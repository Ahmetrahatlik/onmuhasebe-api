using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.BearerToken;
using OnMuhasebe.Api;
using OnMuhasebe.Api.Auth;
using OnMuhasebe.Api.Endpoints;
using OnMuhasebe.Infrastructure;

// Kullanıcı şifresi için hash üretmek: dotnet run --project src/OnMuhasebe.Api -- hash-sifre "Sifrem123!"
if (args is ["hash-sifre", var sifre])
{
    Console.WriteLine(SifreHasher.Hashle(sifre));
    return;
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOnMuhasebe();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services
    .AddAuthentication(BearerTokenDefaults.AuthenticationScheme)
    .AddBearerToken(o => o.BearerTokenExpiration = TimeSpan.FromHours(8));
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/saglik", () => Results.Ok(new { durum = "ok" })).AllowAnonymous();
app.MapAuth();
app.MapOnMuhasebe();

app.Run();

public partial class Program;
