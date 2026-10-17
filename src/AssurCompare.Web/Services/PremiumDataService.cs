using System.Net.Http.Json;
using System.Text.Json;
using AssurCompare.Core.Lamal;

namespace AssurCompare.Web.Services;

/// <summary>
/// Charge les primes officielles depuis les fichiers JSON du site (wwwroot/data).
/// Chaque canton n'est téléchargé qu'une fois, puis gardé en mémoire.
/// </summary>
public sealed class PremiumDataService(HttpClient http)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private PremiumIndex? _index;
    private Dictionary<int, InsurerInfo> _insurers = [];
    private readonly Dictionary<string, IReadOnlyList<PremiumOffer>> _offersByCanton = [];

    public async Task<PremiumIndex> GetIndexAsync()
    {
        if (_index is null)
        {
            _index = await http.GetFromJsonAsync<PremiumIndex>("data/index.json", Json)
                ?? throw new InvalidOperationException("Index des primes introuvable.");
            _insurers = _index.Insurers.ToDictionary(i => i.Id);
        }
        return _index;
    }

    public async Task<IReadOnlyList<PremiumOffer>> GetOffersAsync(string canton)
    {
        if (_offersByCanton.TryGetValue(canton, out var cached)) return cached;

        await GetIndexAsync();
        var data = await http.GetFromJsonAsync<CantonPremiums>($"data/{canton}.json", Json)
            ?? throw new InvalidOperationException($"Primes du canton {canton} introuvables.");

        var offers = data.ToOffers(_insurers).ToList();
        _offersByCanton[canton] = offers;
        return offers;
    }
}
