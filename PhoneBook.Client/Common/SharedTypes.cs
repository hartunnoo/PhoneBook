namespace PhoneBook.Client.Common;

// Auth status — used by all pages
public record AuthStatus(bool IsAuthenticated, string? Name, bool IsAdmin = false);

// Toast notification
public class ToastInfo
{
    public string Message { get; set; } = "";
    public bool IsError { get; set; }
}
