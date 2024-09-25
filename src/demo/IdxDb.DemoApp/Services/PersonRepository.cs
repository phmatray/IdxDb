using IdxDb.DemoApp.Models;
using Microsoft.JSInterop;

namespace IdxDb.DemoApp.Services;

public class PersonRepository(IJSRuntime jsRuntime)
    : IndexedDbRepository<Person>(jsRuntime, DbName, StoreName)
{
    private const string DbName = "demo";
    private const string StoreName = "persons";
}