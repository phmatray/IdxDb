using FormLang.BlazorApp.Models;

namespace IndexedDb.DemoApp.Services;

public class PersonRepository(IndexedDbInterop indexedDb)
{
    private const string DbName = "demo";
    private const string StoreName = "persons";

    public async Task AddPersonAsync(Person formModel)
        => await indexedDb.AddOneAsync(DbName, StoreName, formModel);

    public async Task<Person[]> GetAllPersonsAsync()
        => await indexedDb.GetAllAsync<Person>(DbName, StoreName);

    public async Task<Person?> GetPersonAsync(string id)
        => await indexedDb.GetOneAsync<Person, string>(DbName, StoreName, id);

    public async Task UpdatePersonAsync(Person formModel)
        => await indexedDb.UpdateOneAsync(DbName, StoreName, formModel);

    public async Task DeletePersonAsync(Guid id)
        => await indexedDb.DeleteOneAsync(DbName, StoreName, id);

    public async Task<Person[]> GetPeopleByAgeAsync(int age)
        => await indexedDb.GetAllByIndexAsync<Person>(DbName, StoreName, "ageIndex", age);

    public async Task<int> CountPersonsAsync()
        => await indexedDb.CountAsync(DbName, StoreName);

    public async Task ClearAllPersonsAsync()
        => await indexedDb.ClearStoreAsync(DbName, StoreName);

    public async ValueTask DisposeAsync()
        => await indexedDb.DisposeAsync();
}