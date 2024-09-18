using FormLang.BlazorApp.Models;

namespace FormLang.BlazorApp.Services;

public class PersonManager(IndexedDbInterop indexedDbInterop)
{
    public string DbName { get; } = "demo";
    public string StoreName { get; }  = "persons";
    
    public async Task AddPersonAsync(Person formModel)
    {
        await indexedDbInterop.AddOneAsync(DbName, StoreName, formModel);
    }

    public async Task<Person[]> GetAllPersonsAsync()
    {
        return await indexedDbInterop.GetAllAsync<Person>(DbName, StoreName);
    }

    public async Task<Person?> GetPersonAsync(string id)
    {
        return await indexedDbInterop.GetOneAsync<Person, string>(DbName, StoreName, id);
    }

    public async Task UpdatePersonAsync(Person formModel)
    {
        await indexedDbInterop.UpdateOneAsync(DbName, StoreName, formModel);
    }

    public async Task DeletePersonAsync(string id)
    {
        await indexedDbInterop.DeleteOneAsync(DbName, StoreName, id);
    }
    
    public async ValueTask DisposeAsync()
    {
        await indexedDbInterop.DisposeAsync();
    }
}