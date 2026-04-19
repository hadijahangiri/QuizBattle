using Microsoft.JSInterop;
using System.Text.Json;

namespace QuizBattle.PWA.Services;

public interface IBrowserStorageService
{
    ValueTask<T?> GetAsync<T>(string key);
    ValueTask SetAsync<T>(string key, T value);
    ValueTask RemoveAsync(string key);
    ValueTask ClearAsync();
}

public class BrowserStorageService : IBrowserStorageService
{
    private readonly IJSRuntime _jsRuntime;

    public BrowserStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async ValueTask<T?> GetAsync<T>(string key)
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("browserStorage.get", key);
            if (string.IsNullOrEmpty(json))
                return default;

            return JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return default;
        }
    }

    public async ValueTask SetAsync<T>(string key, T value)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            await _jsRuntime.InvokeVoidAsync("browserStorage.set", key, json);
        }
        catch
        {
            // Ignore storage errors
        }
    }

    public async ValueTask RemoveAsync(string key)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("browserStorage.remove", key);
        }
        catch
        {
            // Ignore storage errors
        }
    }

    public async ValueTask ClearAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("browserStorage.clear");
        }
        catch
        {
            // Ignore storage errors
        }
    }
}
