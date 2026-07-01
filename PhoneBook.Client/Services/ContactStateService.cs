namespace PhoneBook.Client.Services;

/// <summary>
/// Singleton state container that survives WASM navigations so Home page
/// doesn't re-fetch contacts/stats on every back-navigation.
/// </summary>
public class ContactStateService
{
    public List<ContactDto>? Contacts { get; set; }
    public StatsDto? Stats { get; set; }
    public string Search { get; set; } = "";
    public string Filter { get; set; } = "";
    public string SortBy { get; set; } = "fav";
    public string ViewMode { get; set; } = "list";
    public int CurrentPage { get; set; } = 1;
    public HashSet<int> Selected { get; set; } = new();
    public int TotalCount { get; set; }
    public int MaxPages { get; set; } = 1;

    public bool HasData => Contacts is not null;

    public void InvalidateContacts() => Contacts = null;
    public void InvalidateStats() => Stats = null;
    public void InvalidateAll() { Contacts = null; Stats = null; }

    public class ContactDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Gender { get; set; }
        public string? Honorific { get; set; }
        public string? Jawatan { get; set; }
        public string? Department { get; set; }
        public string? Bahagian { get; set; }
        public string? Kementerian { get; set; }
        public string? Company { get; set; }
        public string? Building { get; set; }
        public string? Floor { get; set; }
        public string? Room { get; set; }
        public string? PAName { get; set; }
        public string? PAMobile { get; set; }
        public string? PAEmail { get; set; }
        public string? Mobile1 { get; set; }
        public string? Mobile2 { get; set; }
        public string? Mobile3 { get; set; }
        public string? Mobile4 { get; set; }
        public string? Phone1 { get; set; }
        public string? Phone2 { get; set; }
        public string? Phone3 { get; set; }
        public string? Phone4 { get; set; }
        public string? Email1 { get; set; }
        public string? Email2 { get; set; }
        public string? Email3 { get; set; }
        public string? Tags { get; set; }
        public bool IsFavorite { get; set; }
        public string? Notes { get; set; }
        public string? PhotoPath { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class StatsDto
    {
        public int Total { get; set; }
        public int Favorites { get; set; }
        public int MaxPages { get; set; }
        public List<MinistryCount> Ministries { get; set; } = new();
    }

    public class MinistryCount
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
    }
}
