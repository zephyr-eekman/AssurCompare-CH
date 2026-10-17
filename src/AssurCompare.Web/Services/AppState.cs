using AssurCompare.Core.Analyses;
using AssurCompare.Core.Besoins;
using AssurCompare.Core.Lamal;
using AssurCompare.Core.Parametres;

namespace AssurCompare.Web.Services;

public enum AppMode { Particulier, Conseiller }

/// <summary>Mode d'utilisation (particulier ou conseiller) et mode présentation.</summary>
public sealed class AppModeState(BrowserStorage storage)
{
    private const string Key = "assurcompare.mode.v1";

    public AppMode Mode { get; private set; } = AppMode.Particulier;
    public bool IsAdvisor => Mode == AppMode.Conseiller;
    public bool Presenting { get; private set; }

    public event Action? Changed;

    public async Task LoadAsync() => Mode = await storage.GetAsync<AppMode?>(Key) ?? AppMode.Particulier;

    public async Task SetModeAsync(AppMode mode)
    {
        Mode = mode;
        if (mode == AppMode.Particulier) Presenting = false;
        Changed?.Invoke();
        await storage.SetAsync(Key, mode);
    }

    public void SetPresenting(bool presenting)
    {
        Presenting = presenting;
        Changed?.Invoke();
    }
}

/// <summary>
/// Le profil du client, saisi une seule fois et lu par tous les outils.
/// Il est gardé dans le navigateur pour survivre à un rechargement de page.
/// </summary>
public sealed class ClientProfileState(BrowserStorage storage)
{
    private const string Key = "assurcompare.profile.v1";

    public ClientProfile Profile { get; private set; } = new();

    public AgeGroup AgeGroup => LamalRules.AgeGroupFor(Profile.Age);

    public event Action? Changed;

    public async Task LoadAsync() => Profile = await storage.GetAsync<ClientProfile>(Key) ?? new ClientProfile();

    /// <summary>À appeler après chaque modification du profil.</summary>
    public async Task NotifyChangedAsync()
    {
        Changed?.Invoke();
        await storage.SetAsync(Key, Profile);
    }

    public async Task ReplaceAsync(ClientProfile profile)
    {
        Profile = profile.Clone();
        await NotifyChangedAsync();
    }

    public Task ResetAsync() => ReplaceAsync(new ClientProfile());
}

/// <summary>Analyses sauvegardées par le conseiller, dans le navigateur uniquement.</summary>
public sealed class AnalysisStore(BrowserStorage storage)
{
    private const string Key = "assurcompare.analyses.v1";

    private List<SavedAnalysis> _items = [];

    public IReadOnlyList<SavedAnalysis> Items => _items;
    public bool StorageAvailable => storage.IsAvailable;

    public event Action? Changed;

    public async Task LoadAsync() => _items = await storage.GetAsync<List<SavedAnalysis>>(Key) ?? [];

    public SavedAnalysis? Get(Guid id) => _items.FirstOrDefault(a => a.Id == id);

    public async Task<bool> SaveAsync(SavedAnalysis analysis)
    {
        _items.RemoveAll(a => a.Id == analysis.Id);
        _items.Insert(0, analysis);
        return await PersistAsync();
    }

    public async Task<SavedAnalysis?> DuplicateAsync(Guid id)
    {
        if (Get(id) is not { } source) return null;
        var copy = new SavedAnalysis
        {
            ClientName = $"{source.ClientName} (copie)",
            Profile = source.Profile.Clone(),
            Summary = source.Summary,
        };
        await SaveAsync(copy);
        return copy;
    }

    public async Task DeleteAsync(Guid id)
    {
        _items.RemoveAll(a => a.Id == id);
        await PersistAsync();
    }

    private async Task<bool> PersistAsync()
    {
        Changed?.Invoke();
        return await storage.SetAsync(Key, _items);
    }
}

/// <summary>Construit l'analyse complète du profil courant à partir des données officielles.</summary>
public sealed class AnalysisService(PremiumDataService data, ClientProfileState state)
{
    public async Task<AnalysisSummary> BuildCurrentAsync()
    {
        var index = await data.GetIndexAsync();
        var offers = await data.GetOffersAsync(state.Profile.Canton);
        return ClientAnalysisBuilder.Build(state.Profile, offers, index.Year, SwissParameters.Current);
    }
}
