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
            var confidentialClient = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithClientSecret(clientSecret)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}/v2.0"))
                .Build();

            var result = await confidentialClient.AcquireTokenOnBehalfOf(new[] { "https://graph.microsoft.com/.default" }, new UserAssertion(userToken))
                .ExecuteAsync();

            return result.AccessToken;
        }


        // Step 3: Access SharePoint using the token
        private ClientContext GetPnPContextWithUserToken(string siteUrl, string accessToken)
        {
            var context = new ClientContext(siteUrl);
            context.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.WebRequest.Headers["Authorization"] = "Bearer " + accessToken;
            };
            return context;
        }


private async void btnReconcile_Click(object sender, EventArgs e)
        {
            try
            {
                string sharedDrivePath = txtSharedDrivePath.Text;
                string sharePointUrl = txtSharePointPath.Text;

                if (string.IsNullOrWhiteSpace(sharedDrivePath) || string.IsNullOrWhiteSpace(sharePointUrl))
                {
                    MessageBox.Show("Please provide both paths.");
                    return;
                }

                txtLog.Clear();

                // Step 1: User authenticates
                string userAccessToken = await GetUserAccessTokenAsync();

                // Step 2: Service principal exchanges token on behalf of the user
                string onBehalfOfToken = await GetOnBehalfOfTokenAsync(userAccessToken);

                // Step 3: Use the token to access SharePoint
                using (ClientContext context = GetPnPContextWithUserToken(sharePointUrl, onBehalfOfToken))
                {
                    // Now you can access SharePoint on behalf of the user
                    var web = context.Web;
                    context.Load(web);
                    context.ExecuteQuery();

                    txtLog.AppendText($"Connected to {web.Title}.\n");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
                txtLog.AppendText($"Error: {ex.Message}\n");
            }
        }
