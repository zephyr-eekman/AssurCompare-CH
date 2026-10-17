using System.Text.Json;
using Microsoft.JSInterop;

namespace AssurCompare.Web.Services;

/// <summary>
/// Stockage dans le navigateur (localStorage). Rien n'est envoyé sur un serveur.
/// Si le stockage est bloqué (navigation privée, réglages), l'application continue sans lui.
/// </summary>
public sealed class BrowserStorage(IJSRuntime js)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public bool IsAvailable { get; private set; } = true;

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var json = await js.InvokeAsync<string?>("localStorage.getItem", key);
            return json is null ? default : JsonSerializer.Deserialize<T>(json, Json);
        }
        catch (JsonException)
        {
            return default;
        }
        catch (JSException)
        {
            IsAvailable = false;
            return default;
        }
    }

    public async Task<bool> SetAsync<T>(string key, T value)
    {
        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", key, JsonSerializer.Serialize(value, Json));
            IsAvailable = true;
            return true;
        }
        catch (JSException)
        {
            IsAvailable = false;
            return false;
        }
    }
}
