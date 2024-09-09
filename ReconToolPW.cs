using Microsoft.Identity.Client;
using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ReconciliationTool
{
    public partial class MainForm : Form
    {
        // Service Principal Details for OAuth
        private string clientId = "YOUR_CLIENT_ID";         // Application (Client) ID of your service principal
        private string tenantId = "YOUR_TENANT_ID";         // Tenant ID of your Azure AD
        private string clientSecret = "YOUR_CLIENT_SECRET"; // Client Secret generated from Azure AD

        public MainForm()
        {
            InitializeComponent();
        }

        // Main button click to trigger reconciliation
        private async void btnReconcile_Click(object sender, EventArgs e)
        {
            try
            {
                txtLog.Clear();

                // Step 1: User authenticates and gets an access token
                string authorizationCode = await StartUserLoginFlow();

                // Step 2: Exchange the authorization code for an access token
                string userAccessToken = await ExchangeAuthorizationCodeForToken(authorizationCode);

                // Step 3: Service principal exchanges token on behalf of the user
                string onBehalfOfToken = await GetOnBehalfOfTokenAsync(userAccessToken);

                // Step 4: Use the on-behalf-of token to access SharePoint
                using (ClientContext context = GetPnPContextWithUserToken("https://yourtenant.sharepoint.com/sites/SiteName", onBehalfOfToken))
                {
                    var web = context.Web;
                    context.Load(web);
                    context.ExecuteQuery();

                    txtLog.AppendText($"Connected to SharePoint site: {web.Title}.\n");

                    // Example: Load document libraries (Lists) as an example of accessing content on behalf of the user
                    var lists = context.Web.Lists;
                    context.Load(lists);
                    context.ExecuteQuery();

                    foreach (var list in lists)
                    {
                        txtLog.AppendText($"List found: {list.Title}\n");
                    }

                    // Step 5: Compare files between local and SharePoint
                    var localFiles = GetFilesFromLocalDrive(@"C:\path\to\local\folder");
                    var sharePointFiles = GetFilesFromSharePoint(context, "Documents");

                    CompareFiles(localFiles, sharePointFiles);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
                txtLog.AppendText($"Error: {ex.Message}\n");
            }
        }

        // Step 1: Start User Login via Authorization Code Flow
        private async Task<string> StartUserLoginFlow()
        {
            // Step 1.1: Redirect the user to Azure AD login page
            var authorizeUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize?" +
                               $"client_id={clientId}&response_type=code&redirect_uri=https://localhost&response_mode=query" +
                               $"&scope=User.Read%20Sites.ReadWrite.All&state=12345";

            System.Diagnostics.Process.Start(authorizeUrl);

            // Wait for the user to log in and receive the authorization code
            // In production, you would have a server-side endpoint to handle the redirect
            string authorizationCode = "";  // Replace with actual code retrieval logic
            return authorizationCode;
        }

        // Step 2: Exchange the authorization code for an access token
        private async Task<string> ExchangeAuthorizationCodeForToken(string authorizationCode)
        {
            var httpClient = new HttpClient();

            var tokenRequestBody = new Dictionary<string, string>
            {
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "grant_type", "authorization_code" },
                { "code", authorizationCode },
                { "redirect_uri", "https://localhost" },
                { "scope", "User.Read Sites.ReadWrite.All" }
            };

            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token")
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

        // Step 3: Service principal exchanges the user's token for a SharePoint access token (on-behalf-of flow)
        private async Task<string> GetOnBehalfOfTokenAsync(string userToken)
        {
            var confidentialClient = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithClientSecret(clientSecret)  // Use client secret here for the service principal
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}/v2.0"))
                .Build();

            var result = await confidentialClient.AcquireTokenOnBehalfOf(
                new[] { "https://graph.microsoft.com/.default" },
                new UserAssertion(userToken)  // Pass the user's token for OBO
            ).ExecuteAsync();

            return result.AccessToken;  // Return the access token for the service principal
        }

        // Step 4: Use the access token to authenticate with SharePoint
        private ClientContext GetPnPContextWithUserToken(string siteUrl, string accessToken)
        {
            var context = new ClientContext(siteUrl);
            context.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.WebRequest.Headers["Authorization"] = "Bearer " + accessToken;
            };
            return context;
        }

        // Step 5: Compare Files Between Local and SharePoint

        // Get files from a local drive
        private System.IO.FileInfo[] GetFilesFromLocalDrive(string localFolderPath)
        {
            return new DirectoryInfo(localFolderPath).GetFiles("*", SearchOption.AllDirectories);
        }

        // Get files from SharePoint
        private SharePointFileInfo[] GetFilesFromSharePoint(ClientContext context, string documentLibraryTitle)
        {
            List<SharePointFileInfo> files = new List<SharePointFileInfo>();

            var list = context.Web.Lists.GetByTitle(documentLibraryTitle);
            context.Load(list.RootFolder.Files);
            context.ExecuteQuery();

            foreach (var file in list.RootFolder.Files)
            {
                files.Add(new SharePointFileInfo
                {
                    Name = file.Name,
                    Length = file.Length,
                    LastModified = file.TimeLastModified,
                    ServerRelativeUrl = file.ServerRelativeUrl
                });
            }

            return files.ToArray();
        }

        // Compare local files to SharePoint files
        private void CompareFiles(System.IO.FileInfo[] localFiles, SharePointFileInfo[] sharePointFiles)
        {
            foreach (var localFile in localFiles)
            {
                var matchingFile = sharePointFiles.FirstOrDefault(f => f.Name == localFile.Name);
                if (matchingFile == null)
                {
                    txtLog.AppendText($"Missing in SharePoint: {localFile.Name}\n");
                    continue;
                }

                bool isSizeEqual = localFile.Length == matchingFile.Length;
                bool isModifiedEqual = localFile.LastWriteTime == matchingFile.LastModified;

                if (isSizeEqual && isModifiedEqual)
                {
                    txtLog.AppendText($"File matched: {localFile.Name}\n");
                }
                else
                {
                    txtLog.AppendText($"Mismatch found for file: {localFile.Name}\n");
                }
            }

            txtLog.AppendText("File comparison completed.\n");
        }

        // Custom class to represent SharePoint file information
        public class SharePointFileInfo
        {
            public string Name { get; set; }
            public long Length { get; set; }
            public DateTime LastModified { get; set; }
            public string ServerRelativeUrl { get; set; }
        }
    }
}
