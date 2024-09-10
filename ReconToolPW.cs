private async Task AuthenticateAndCompareFiles(string localPath, string sharePointPath)
{
    try
    {
        // Well-known public client ID for Microsoft apps (e.g., Office 365, SharePoint Online)
        var clientId = "d3590ed6-52b3-4102-aeff-aad2292ab01c";  
        var tenantId = "organizations";  // Using "organizations" for multi-tenant authentication
        var authority = $"https://login.microsoftonline.com/{tenantId}";

        // MSAL Public Client Application for user-interactive authentication
        var app = PublicClientApplicationBuilder.Create(clientId)
            .WithAuthority(authority)
            .WithDefaultRedirectUri()
            .Build();

        // Scopes needed for SharePoint access
        string[] scopes = { "https://yourtenant.sharepoint.com/.default" };

        // Acquire token interactively (prompts the user to log in)
        var result = await app.AcquireTokenInteractive(scopes).ExecuteAsync();

        // Access token acquired
        string accessToken = result.AccessToken;

        // Use the token to authenticate SharePoint requests
        using (var context = new ClientContext("https://yourtenant.sharepoint.com/sites/SiteName"))
        {
            context.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.WebRequest.Headers["Authorization"] = "Bearer " + accessToken;
            };

            var folder = context.Web.GetFolderByServerRelativeUrl(sharePointPath);
            context.Load(folder.Files);
            await context.ExecuteQueryAsync();

            var sharePointFiles = folder.Files.ToList();

            // Load local files
            var localFiles = Directory.GetFiles(localPath);

            // Compare files
            foreach (var localFile in localFiles)
            {
                var matchingFile = sharePointFiles.FirstOrDefault(f => f.Name == Path.GetFileName(localFile));
                if (matchingFile != null)
                {
                    logTextBox.AppendText($"Found matching file: {matchingFile.Name}\n");
                }
                else
                {
                    logTextBox.AppendText($"File not found in SharePoint: {Path.GetFileName(localFile)}\n");
                }
            }

            logTextBox.AppendText("File comparison completed.\n");
        }
    }
    catch (Exception ex)
    {
        logTextBox.AppendText($"Error: {ex.Message}\n");
    }
}
