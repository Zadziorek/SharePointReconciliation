using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

public async Task<string> ExchangeAuthorizationCodeForToken(string authorizationCode)
{
    var httpClient = new HttpClient();
    
    var tokenRequestBody = new Dictionary<string, string>
    {
        { "client_id", "your-client-id" },
        { "client_secret", "your-client-secret" },
        { "grant_type", "authorization_code" },
        { "code", authorizationCode },
        { "redirect_uri", "https://yourapp.com/redirect" },
        { "scope", "User.Read Sites.ReadWrite.All" }
    };

    var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://login.microsoftonline.com/your-tenant-id/oauth2/v2.0/token")
    {
        Content = new FormUrlEncodedContent(tokenRequestBody)
    };

    var response = await httpClient.SendAsync(tokenRequest);
    var responseContent = await response.Content.ReadAsStringAsync();

    if (response.IsSuccessStatusCode)
    {
        var tokenResult = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent);
        return tokenResult["access_token"];  // Return the access token
    }
    else
    {
        throw new Exception($"Failed to exchange authorization code for access token: {responseContent}");
    }
}
