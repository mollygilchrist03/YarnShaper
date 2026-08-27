using YarnShaper.Web.Models;

namespace YarnShaper.Web.Services;

/// <summary>
/// Saves and loads named projects to localStorage: a lightweight shared
/// index (name, calculator, saved date) for the "My Projects" list, plus
/// one full payload per project stored under its own key so listing
/// projects never has to load every payload just to show their names.
/// </summary>
public sealed class ProjectStore(LocalStorageService storage)
{
    private const string IndexKey = "yarnshaper.projects.index";

    public async Task<List<ProjectIndexEntry>> ListAsync() =>
        await storage.GetItemAsync<List<ProjectIndexEntry>>(IndexKey) ?? [];

    public async Task<string> SaveAsync<T>(string name, string calculatorKind, T payload)
    {
        var id = Guid.NewGuid().ToString("n");
        await storage.SetItemAsync($"yarnshaper.project.{id}", payload);

        var index = await ListAsync();
        index.Insert(0, new ProjectIndexEntry(id, name, calculatorKind, DateTimeOffset.UtcNow));
        await storage.SetItemAsync(IndexKey, index);

        return id;
    }

    public Task<T?> LoadPayloadAsync<T>(string id) => storage.GetItemAsync<T>($"yarnshaper.project.{id}");

    public async Task DeleteAsync(string id)
    {
        await storage.RemoveItemAsync($"yarnshaper.project.{id}");

        var index = await ListAsync();
        index.RemoveAll(e => e.Id == id);
        await storage.SetItemAsync(IndexKey, index);
    }
}
