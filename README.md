# **IndexedDb Blazor Library**

A Blazor library that provides seamless interaction with IndexedDB via JavaScript interop. This library simplifies data storage and retrieval in Blazor applications by offering a straightforward API for IndexedDB operations.

![NuGet](https://img.shields.io/nuget/v/IndexedDb) ![License](https://img.shields.io/github/license/phmatray/IndexedDb) ![Build](https://img.shields.io/github/actions/workflow/status/phmatray/IndexedDb/build.yml)

## **Table of Contents**

- [Features](#features)
- [Installation](#installation)
- [Getting Started](#getting-started)
    - [Database Initialization](#database-initialization)
    - [Using the Repository](#using-the-repository)
- [API Reference](#api-reference)
    - [IndexedDbInterop](#indexeddbinterop)
    - [IndexedDbRepository\<TItem>](#indexeddbrepositorytitem)
- [Examples](#examples)
    - [CRUD Operations](#crud-operations)
    - [Transactions](#transactions)
- [Contributing](#contributing)
- [License](#license)
- [Acknowledgments](#acknowledgments)

---

## **Features**

- **Easy Setup**: Simplify the configuration and use of IndexedDB in Blazor applications.
- **Type Safety**: Utilize generics for strong typing and compile-time checks.
- **Async Operations**: All methods are asynchronous, providing non-blocking data operations.
- **Transactions Support**: Execute multiple operations within a single transaction.
- **Indexing**: Create and query indexes for efficient data retrieval.
- **Schema Migration**: Upgrade database schemas with versioning and store schemas.

## **Installation**

Install the package via NuGet Package Manager:

```bash
dotnet add package IndexedDb --version 1.0.0
```

Or via the NuGet Package Manager UI in Visual Studio by searching for **IndexedDb**.

## **Getting Started**

### **Database Initialization**

Before using the library, you need to define your database schema and initialize it.

```csharp
@inject IJSRuntime JSRuntime

@code {
    private IndexedDbInterop _indexedDbInterop;

    protected override async Task OnInitializedAsync()
    {
        _indexedDbInterop = new IndexedDbInterop(JSRuntime);

        var storeSchemas = new[]
        {
            new
            {
                name = "Customers",
                options = new { keyPath = "id", autoIncrement = true },
                indexes = new[]
                {
                    new { name = "NameIndex", keyPath = "name", unique = false }
                }
            }
        };

        await _indexedDbInterop.UpgradeDatabaseAsync("MyDatabase", 1, storeSchemas);
    }

    public async ValueTask DisposeAsync()
    {
        if (_indexedDbInterop != null)
        {
            await _indexedDbInterop.DisposeAsync();
        }
    }
}
```

### **Using the Repository**

Create a repository for your data model to simplify data operations.

```csharp
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
    // Additional properties...
}

@inject IJSRuntime JSRuntime

@code {
    private IndexedDbRepository<Customer> _customerRepository;

    protected override async Task OnInitializedAsync()
    {
        _customerRepository = new IndexedDbRepository<Customer>(JSRuntime, "MyDatabase", "Customers");

        // Add a new customer
        var newCustomer = new Customer { Name = "John Doe" };
        await _customerRepository.AddOneAsync(newCustomer);

        // Get all customers
        var customers = await _customerRepository.GetAllAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_customerRepository != null)
        {
            await _customerRepository.DisposeAsync();
        }
    }
}
```

## **API Reference**

### **IndexedDbInterop**

The `IndexedDbInterop` class provides methods to interact directly with IndexedDB.

#### **Methods**

- `AddOneAsync(string dbName, string storeName, object item)`
- `GetAllAsync<T>(string dbName, string storeName)`
- `GetOneAsync<TRecord, TKey>(string dbName, string storeName, TKey id)`
- `UpdateOneAsync(string dbName, string storeName, object item)`
- `DeleteOneAsync<TKey>(string dbName, string storeName, TKey id)`
- `UpgradeDatabaseAsync(string dbName, int newVersion, object[] storeSchemas)`
- `AddManyAsync(string dbName, string storeName, object[] items)`
- `CreateIndexAsync(string dbName, string storeName, string indexName, string keyPath, bool unique = false)`
- `GetAllByIndexAsync<T>(string dbName, string storeName, string indexName, object query)`
- `ExecuteTransactionAsync(string dbName, string[] storeNames, string mode, Func<Task> transactionBody)`
- `CountAsync(string dbName, string storeName)`
- `ClearStoreAsync(string dbName, string storeName)`

### **IndexedDbRepository\<TItem>**

The `IndexedDbRepository<TItem>` class simplifies data operations by setting the database name, store name, and item type.

#### **Constructor**

```csharp
public IndexedDbRepository(IJSRuntime jsRuntime, string dbName, string storeName)
```

#### **Methods**

- `AddOneAsync(TItem item)`
- `AddManyAsync(TItem[] items)`
- `GetAllAsync()`
- `GetOneAsync<TKey>(TKey id)`
- `UpdateOneAsync(TItem item)`
- `DeleteOneAsync<TKey>(TKey id)`
- `CountAsync()`
- `ClearStoreAsync()`

## **Examples**

### **CRUD Operations**

#### **Adding an Item**

```csharp
var newCustomer = new Customer { Name = "Alice" };
await _customerRepository.AddOneAsync(newCustomer);
```

#### **Retrieving Items**

```csharp
var customers = await _customerRepository.GetAllAsync();
```

#### **Updating an Item**

```csharp
var customer = customers.FirstOrDefault();
if (customer != null)
{
    customer.Name = "Alice Smith";
    await _customerRepository.UpdateOneAsync(customer);
}
```

#### **Deleting an Item**

```csharp
if (customer != null)
{
    await _customerRepository.DeleteOneAsync(customer.Id);
}
```

### **Transactions**

```csharp
await _customerRepository.ExecuteTransactionAsync(async () =>
{
    await _customerRepository.AddOneAsync(new Customer { Name = "Bob" });
    await _customerRepository.AddOneAsync(new Customer { Name = "Carol" });
});
```

## **Contributing**

Contributions are welcome! Please follow these steps:

1. Fork the repository.
2. Create a new branch: `git checkout -b feature/your-feature-name`.
3. Commit your changes: `git commit -am 'Add new feature'`.
4. Push to the branch: `git push origin feature/your-feature-name`.
5. Submit a pull request.

Please ensure all new code is covered by unit tests and adheres to the existing coding standards.

## **License**

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## **Acknowledgments**

- **[Nuke Build](https://nuke.build/)**: For build automation.
- **[MinVer](https://github.com/adamralph/minver)**: For versioning based on Git tags.
- **[IndexedDB API](https://developer.mozilla.org/en-US/docs/Web/API/IndexedDB_API)**: The underlying technology enabling this library.
