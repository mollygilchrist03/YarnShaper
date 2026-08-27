using System.Text.Json;
using Microsoft.JSInterop;
using YarnShaper.Web.Models;

namespace YarnShaper.Web.Services;

/// <summary>
/// Thin wrapper over the browser's localStorage. Calls the Web Storage API
/// directly through JS interop (no custom .js file needed — "localStorage.x"
/// resolves as a path from `window`) and serializes with the source-generated
/// <see cref="ProjectJsonContext"/> so it stays correct under WASM trimming.
/// </summary>
public sealed class LocalStorageService(IJSRuntime js)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        TypeInfoResolver = ProjectJsonContext.Default,
    };

    public async Task<T?> GetItemAsync<T>(string key)
    {
        var json = await js.InvokeAsync<string?>("localStorage.getItem", key);
        return json is null ? default : JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public async Task SetItemAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        await js.InvokeVoidAsync("localStorage.setItem", key, json);
    }

    public async Task RemoveItemAsync(string key) => await js.InvokeVoidAsync("localStorage.removeItem", key);
}
