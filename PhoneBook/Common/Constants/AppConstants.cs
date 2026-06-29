namespace PhoneBook.Common.Constants;

public static class AppConstants
{
    // Sort
    public const string SortByName = "name";
    public const string SortByDate = "date";
    public const string SortByDept = "dept";
    public const string SortByFav = "fav";

    // Content types
    public const string ContentCsv = "text/csv";
    public const string ContentVCard = "text/vcard";

    // Files
    public const string CsvFileName = "kenalan.csv";
    public const string PhotosDir = "photos";
    public const string WwwRoot = "wwwroot";
    public const string DefaultPhoto = "default.png";

    // Auth
    public const string CookieName = ".AspNetCore.Identity.Application";
    public const string DefaultPassword = "SuperAdmin@123";

    // Pagination
    public const int DefaultPageSize = 200;
}

public static class DateTimeFormat
{
    public const string RoundTrip = "o";
    public const string DateOnly = "yyyy-MM-dd";
    public const string DateTimeDisplay = "dd MMM yyyy HH:mm";
}
