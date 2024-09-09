using Microsoft.Identity.Client;
using Microsoft.SharePoint.Client;
using System;
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
                string userAccessToken = await GetUserAccessTokenAsync();

                // Step 2: Service principal exchanges token on behalf of the user
                string onBehalfOfToken = await GetOnBehalfOfTokenAsync(userAccessToken);

                // Step 3: Use the on-behalf-of token to access SharePoint
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
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
                txtLog.AppendText($"Error: {ex.Message}\n");
            }
        }

        // Step 1: User authenticates and gets an access token
        private async Task<string> GetUserAccessTokenAsync()
        {
            var app = PublicClientApplicationBuilder.Create(clientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, tenantId)
                .WithRedirectUri("http://localhost") // Redirect for interactive user login
                .Build();

            var result = await app.AcquireTokenInteractive(new[] { "User.Read", "Sites.ReadWrite.All" }).ExecuteAsync();
            return result.AccessToken;
        }

        // Step 2: Service principal exchanges the user's token for a SharePoint access token (on-behalf-of flow)
        private async Task<string> GetOnBehalfOfTokenAsync(string userToken)
        {
            try
            {
                // Create the confidential client with client ID, client secret, and tenant authority
                var confidentialClient = ConfidentialClientApplicationBuilder.Create(clientId)
                    .WithClientSecret(clientSecret) // Pass the client secret here
                    .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}/v2.0"))  // Correct authority URL with tenant
                    .Build();

                // Use On-Behalf-Of (OBO) flow to exchange the user's token for a new token
                var result = await confidentialClient.AcquireTokenOnBehalfOf(
                    new[] { "https://graph.microsoft.com/.default" },  // Scope for Microsoft Graph or SharePoint API
                    new UserAssertion(userToken)  // User token obtained from interactive login
                ).ExecuteAsync();

                return result.AccessToken;
            }
            catch (MsalServiceException ex)
            {
                MessageBox.Show($"MSAL error: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error acquiring token: {ex.Message}");
                throw;
            }
        }

        // Step 3: Use the access token to authenticate with SharePoint
        private ClientContext GetPnPContextWithUserToken(string siteUrl, string accessToken)
        {
            var context = new ClientContext(siteUrl);
            context.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.WebRequest.Headers["Authorization"] = "Bearer " + accessToken;
            };
            return context;
        }
    }
}
