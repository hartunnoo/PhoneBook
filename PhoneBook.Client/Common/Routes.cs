namespace PhoneBook.Client.Common;

public static class AppRoutes
{
    public const string Home = "/";
    public const string Login = "/login";
    public const string Add = "/add";
    public static string Edit(int id) => $"/edit/{id}";

    // API
    public const string ApiContacts = "api/contacts";
    public const string ApiAuth = "api/auth";
    public const string ApiLogin = "api/auth/login";
    public const string ApiLogout = "api/auth/logout";
    public const string ApiRegister = "api/auth/register";
    public const string ApiStatus = "api/auth/status";

    // Query params
    public const string SortParam = "sort";
    public const string SearchParam = "search";
    public const string PageSizeParam = "pageSize";
}
