namespace PhoneBook.Common.Constants;

public static class CacheKeys
{
    public const string AllContacts = "contacts:all";
    public static string ContactById(int id) => $"contacts:{id}";
}
