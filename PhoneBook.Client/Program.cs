using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PhoneBook.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

builder.Services.AddSingleton<ContactStateService>();

await builder.Build().RunAsync();
