using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using QuizBattle.PWA;
using QuizBattle.PWA.Services;
using QuizBattle.Shared;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<AppState>();
builder.Services.AddScoped<IBrowserStorageService, BrowserStorageService>();
builder.Services.AddScoped<UserStateService>();

// HttpClient با handler برای اضافه کردن توکن احراز هویت
builder.Services.AddScoped<AuthTokenHandler>();
builder.Services.AddScoped(sp =>
{
    var appState = sp.GetRequiredService<AppState>();
    var handler = new AuthTokenHandler(appState);
    return new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5101/api/") };
});

await builder.Build().RunAsync();
