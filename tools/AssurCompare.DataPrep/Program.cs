// Convertit les fichiers open data de l'OFSP (primes LAMal) en petits fichiers JSON par canton
// pour l'application web.
//
// Usage : dotnet run --project tools/AssurCompare.DataPrep -- <dossier source OFSP> <dossier de sortie>
// Source : https://opendata.swiss/fr/dataset/health-insurance-premiums

using System.Globalization;
using System.Text;
using System.Text.Json;
using AssurCompare.Core.Lamal;

var sourceDir = args.Length > 0 ? args[0] : Path.Combine("data-source", "ofsp");
var outputDir = args.Length > 1 ? args[1] : Path.Combine("src", "AssurCompare.Web", "wwwroot", "data");
Directory.CreateDirectory(outputDir);

var cantonNames = new Dictionary<string, string>
{
    ["AG"] = "Argovie", ["AI"] = "Appenzell Rhodes-Intérieures", ["AR"] = "Appenzell Rhodes-Extérieures",
    ["BE"] = "Berne", ["BL"] = "Bâle-Campagne", ["BS"] = "Bâle-Ville", ["FR"] = "Fribourg", ["GE"] = "Genève",
    ["GL"] = "Glaris", ["GR"] = "Grisons", ["JU"] = "Jura", ["LU"] = "Lucerne", ["NE"] = "Neuchâtel",
    ["NW"] = "Nidwald", ["OW"] = "Obwald", ["SG"] = "Saint-Gall", ["SH"] = "Schaffhouse", ["SO"] = "Soleure",
    ["SZ"] = "Schwytz", ["TG"] = "Thurgovie", ["TI"] = "Tessin", ["UR"] = "Uri", ["VD"] = "Vaud",
    ["VS"] = "Valais", ["ZG"] = "Zoug", ["ZH"] = "Zurich",
};

// Seules les primes « standard » par classe d'âge sont gardées (pas les rabais dès le 2e/3e enfant).
var ageGroups = new Dictionary<string, AgeGroup>
{
    ["K1"] = AgeGroup.Enfant, ["J1"] = AgeGroup.JeuneAdulte, ["E1"] = AgeGroup.Adulte,
};

var insurers = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "insurers.csv"), Encoding.UTF8)
    .Skip(1)
    .Where(l => l.Length > 0)
    .Select(l => l.Split(';'))
    .Select(c => new InsurerInfo(int.Parse(c[0]), c[1], c[2].Length > 0 ? c[2] : null))
    .ToDictionary(i => i.Id);

// Noms de tarifs en français (ex. « Assurance du médecin de famille Profit »).
var tariffNamesFr = ReadCsv(Path.Combine(sourceDir, "Tarifs.csv"))
    .Where(r => r["Kategorie"] == "MOD")
    .GroupBy(r => (r["Versicherer"], r["Tarif"]))
    .ToDictionary(g => g.Key, g => g.First()["Name_FR"]);

var perCanton = new SortedDictionary<string, CantonBuilder>();
int year = 0, kept = 0, skipped = 0;

foreach (var row in ReadCsv(Path.Combine(sourceDir, "Primes_CH.csv")))
{
    var canton = row["Kanton"];
    if (!cantonNames.ContainsKey(canton) || !ageGroups.TryGetValue(row["Altersuntergruppe"], out var age))
    {
        skipped++;
        continue;
    }

    var insurerId = int.Parse(row["Versicherer"]);
    if (!insurers.ContainsKey(insurerId))
        throw new InvalidDataException($"Assureur {insurerId} absent de insurers.csv : mettre la liste à jour.");

    year = int.Parse(row["Geschäftsjahr"]);
    var tariffCode = row["Tarif"];
    var name = tariffNamesFr.GetValueOrDefault((row["Versicherer"], tariffCode)) ?? row["Tarifbezeichnung"];
    var tariff = new TariffInfo(insurerId, tariffCode, TariffTypeExtensions.FromOfspCode(row["Tariftyp"]), name);

    var builder = perCanton.TryGetValue(canton, out var b) ? b : perCanton[canton] = new CantonBuilder(canton);
    builder.Add(tariff,
        region: int.Parse(row["Region"]["PR_REG_".Length..]),
        age,
        withAccident: row["Unfalleinschluss"] == "MIT_UNF",
        franchise: int.Parse(row["Franchise"][^4..]),
        monthlyPremium: decimal.Parse(row["Prämie"], CultureInfo.InvariantCulture));
    kept++;
}

var json = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

foreach (var builder in perCanton.Values)
{
    var data = builder.Build(year);
    await File.WriteAllTextAsync(Path.Combine(outputDir, $"{builder.Canton}.json"), JsonSerializer.Serialize(data, json));
}

var index = new PremiumIndex
{
    Year = year,
    Source = "Office fédéral de la santé publique (OFSP), primes de l'assurance obligatoire des soins, opendata.swiss",
    GeneratedOn = DateOnly.FromDateTime(DateTime.Today),
    Cantons = perCanton.Values
        .Select(b => new CantonInfo(b.Canton, cantonNames[b.Canton], b.Regions.Order().ToArray()))
        .OrderBy(c => c.Name, StringComparer.Create(CultureInfo.GetCultureInfo("fr-CH"), false))
        .ToList(),
    Insurers = insurers.Values.OrderBy(i => i.Name).ToList(),
};
await File.WriteAllTextAsync(Path.Combine(outputDir, "index.json"), JsonSerializer.Serialize(index, json));

Console.WriteLine($"Primes {year} : {kept} lignes gardées, {skipped} ignorées, {perCanton.Count} cantons -> {Path.GetFullPath(outputDir)}");

// Lecteur CSV minimal : virgules comme séparateur, champs entre guillemets possibles.
static IEnumerable<Dictionary<string, string>> ReadCsv(string path)
{
    using var reader = new StreamReader(path, Encoding.UTF8);
    var header = SplitCsvLine(reader.ReadLine() ?? throw new InvalidDataException($"{path} est vide."));
    while (reader.ReadLine() is { } line)
    {
        if (line.Length == 0) continue;
        var cells = SplitCsvLine(line);
        var row = new Dictionary<string, string>(header.Count);
        for (var i = 0; i < header.Count; i++)
            row[header[i]] = i < cells.Count ? cells[i] : "";
        yield return row;
    }
}

static List<string> SplitCsvLine(string line)
{
    var cells = new List<string>();
    var cell = new StringBuilder();
    var inQuotes = false;
    for (var i = 0; i < line.Length; i++)
    {
        var c = line[i];
        if (inQuotes)
        {
            if (c == '"' && i + 1 < line.Length && line[i + 1] == '"') { cell.Append('"'); i++; }
            else if (c == '"') inQuotes = false;
            else cell.Append(c);
        }
        else if (c == '"') inQuotes = true;
        else if (c == ',') { cells.Add(cell.ToString()); cell.Clear(); }
        else cell.Append(c);
    }
    cells.Add(cell.ToString());
    return cells;
}

sealed class CantonBuilder(string canton)
{
    private readonly Dictionary<TariffInfo, int> _tariffIndex = [];
    private readonly List<TariffInfo> _tariffs = [];
    private readonly List<decimal[]> _rows = [];

    public string Canton { get; } = canton;
    public HashSet<int> Regions { get; } = [];

    public void Add(TariffInfo tariff, int region, AgeGroup age, bool withAccident, int franchise, decimal monthlyPremium)
    {
        if (!_tariffIndex.TryGetValue(tariff, out var index))
        {
            index = _tariffs.Count;
            _tariffIndex[tariff] = index;
            _tariffs.Add(tariff);
        }
        Regions.Add(region);
        _rows.Add([index, region, (int)age, withAccident ? 1 : 0, franchise, monthlyPremium]);
    }

    public CantonPremiums Build(int year) => new()
    {
        Canton = Canton,
        Year = year,
        Tariffs = _tariffs,
        Rows = _rows,
    };
}
