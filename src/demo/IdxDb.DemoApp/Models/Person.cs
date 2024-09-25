namespace IdxDb.DemoApp.Models;

public class Person
{
    [IndexedDbKeyPath(AutoIncrement = true)]
    public required int Id { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    [IndexedDbIndex("EmailIndex", Unique = true)]
    public required string Email { get; set; }
}