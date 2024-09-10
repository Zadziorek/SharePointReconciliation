var builder = WebApplication.CreateBuilder(args);

// Configure Azure AD Authentication
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

var app = builder.Build();

// Use Authentication and Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/get-token", async (HttpContext httpContext) =>
{
    // Get the access token for the signed-in user
    var tokenAcquisition = httpContext.RequestServices.GetRequiredService<ITokenAcquisition>();
    string accessToken = await tokenAcquisition.GetAccessTokenForUserAsync(new[] { "User.Read", "Sites.ReadWrite.All" });

    // Redirect to a local URI or custom scheme to pass token to desktop app
    httpContext.Response.Redirect($"yourdesktopapp://token?access_token={accessToken}");
});

app.Run();
