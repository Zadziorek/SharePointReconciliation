using Microsoft.Identity.Client;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.SharePoint.Client;
using PnP.Framework;

namespace ReconciliationTool
{
    public partial class MainForm : Form
    {
        // Service Principal Details for OAuth
        private string clientId = "YOUR_CLIENT_ID";         // Replace with your Service Principal Application (Client) ID
        private string tenantId = "YOUR_TENANT_ID";         // Replace with your Tenant ID
        private string clientSecret = "YOUR_CLIENT_SECRET"; // Replace with your Client Secret from Azure AD

        // Private Azure Cloud Authority URL (e.g., for Azure Government or Azure China)
        private string authority = "https://login.microsoftonline.us/{0}/v2.0"; // Adjust for private cloud, replace {0} with your tenantId

        private string[] scopes = new[] { "https://graph.microsoft.com/.default" };  // Default scope for Microsoft Graph

        // UI Controls
        private TextBox txtSharedDrivePath;
        private TextBox txtSharePointPath;
        private Button btnReconcile;
        private Button btnSaveLog;
        private RichTextBox txtLog;
        private ProgressBar progressBar;
        private Label lblSharedDrivePath;
        private Label lblSharePointPath;

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "File Reconciliation Tool";
            this.Size = new System.Drawing.Size(600, 600);  // Set form size

            // Label for Shared Drive Path
            lblSharedDrivePath = new Label();
            lblSharedDrivePath.Text = "Shared Drive Path:";
            lblSharedDrivePath.Location = new System.Drawing.Point(20, 20);
            lblSharedDrivePath.Size = new System.Drawing.Size(150, 20);

            // Shared Drive Path TextBox
            txtSharedDrivePath = new TextBox();
            txtSharedDrivePath.Location = new System.Drawing.Point(180, 20);
            txtSharedDrivePath.Size = new System.Drawing.Size(350, 20);

            // Label for SharePoint Path
            lblSharePointPath = new Label();
            lblSharePointPath.Text = "SharePoint Path:";
            lblSharePointPath.Location = new System.Drawing.Point(20, 60);
            lblSharePointPath.Size = new System.Drawing.Size(150, 20);

            // SharePoint Path TextBox
            txtSharePointPath = new TextBox();
            txtSharePointPath.Location = new System.Drawing.Point(180, 60);
            txtSharePointPath.Size = new System.Drawing.Size(350, 20);

            // Reconcile Button
            btnReconcile = new Button();
            btnReconcile.Text = "Reconcile";
            btnReconcile.Location = new System.Drawing.Point(180, 100);
            btnReconcile.Size = new System.Drawing.Size(100, 30);
            btnReconcile.Click += new EventHandler(this.btnReconcile_Click);

            // Save Log Button
            btnSaveLog = new Button();
            btnSaveLog.Text = "Save Log";
            btnSaveLog.Location = new System.Drawing.Point(300, 100);
            btnSaveLog.Size = new System.Drawing.Size(100, 30);
            btnSaveLog.Click += new EventHandler(this.btnSaveLog_Click);

            // Log RichTextBox
            txtLog = new RichTextBox();
            txtLog.Location = new System.Drawing.Point(20, 150);
            txtLog.Size = new System.Drawing.Size(510, 200);

            // Progress Bar
            progressBar = new ProgressBar();
            progressBar.Location = new System.Drawing.Point(20, 370);
            progressBar.Size = new System.Drawing.Size(510, 30);

            // Add all controls to the form
            this.Controls.Add(lblSharedDrivePath);
            this.Controls.Add(txtSharedDrivePath);
            this.Controls.Add(lblSharePointPath);
            this.Controls.Add(txtSharePointPath);
            this.Controls.Add(btnReconcile);
            this.Controls.Add(btnSaveLog);
            this.Controls.Add(txtLog);
            this.Controls.Add(progressBar);
        }

        // Reconcile button click event
        private async void btnReconcile_Click(object sender, EventArgs e)
        {
            try
            {
                string sharedDrivePath = txtSharedDrivePath.Text;
                string sharePointUrl = txtSharePointPath.Text;

                if (string.IsNullOrWhiteSpace(sharedDrivePath) || string.IsNullOrWhiteSpace(sharePointUrl))
                {
                    MessageBox.Show("Please provide both paths (Shared Drive and SharePoint).");
                    return;
                }

                txtLog.Clear();

                // Authenticate using Service Principal (Client Credentials Flow)
                string accessToken = await GetOAuthAccessTokenServicePrincipalAsync();
                using (ClientContext context = GetPnPContextWithServicePrincipal(sharePointUrl, accessToken))
                {
                    // Reconciliation logic goes here, e.g., getting files and comparing
                    var sharedDriveFiles = GetFilesFromSharedDrive(sharedDrivePath);
                    var sharePointFiles = GetFilesFromSharePoint(context);

                    progressBar.Maximum = sharedDriveFiles.Length;
                    progressBar.Value = 0;

                    CompareFiles(sharedDriveFiles, sharePointFiles);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during the reconciliation process: {ex.Message}");
                txtLog.AppendText($"Error: {ex.Message}\n");
            }
        }

        // Save log button click event
        private void btnSaveLog_Click(object sender, EventArgs e)
        {
            try
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "Text Files (*.txt)|*.txt";
                    saveFileDialog.DefaultExt = "txt";
                    saveFileDialog.AddExtension = true;
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllText(saveFileDialog.FileName, txtLog.Text);
                        MessageBox.Show("Log saved successfully.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving log file: {ex.Message}");
            }
        }

        // Function to authenticate and get OAuth Access Token using service principal
        private async Task<string> GetOAuthAccessTokenServicePrincipalAsync()
        {
            var app = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithClientSecret(clientSecret)
                .WithAuthority(new Uri(string.Format(authority, tenantId))) // Private Azure Cloud Authority
                .Build();

            var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
            return result.AccessToken;
        }

        // Function to get PnP ClientContext using the Access Token
        private ClientContext GetPnPContextWithServicePrincipal(string siteUrl, string accessToken)
        {
            var context = new ClientContext(siteUrl);
            context.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.WebRequest.Headers["Authorization"] = "Bearer " + accessToken;
            };
            return context;
        }

        // Use System.IO.FileInfo explicitly for local files
        private System.IO.FileInfo[] GetFilesFromSharedDrive(string path)
        {
            try
            {
                return new DirectoryInfo(path).GetFiles("*", SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading shared drive: {ex.Message}");
                txtLog.AppendText($"Error reading shared drive: {ex.Message}\n");
                return Array.Empty<System.IO.FileInfo>();
            }
        }

        // SharePoint-specific file retrieval using PnP
        private SharePointFileInfo[] GetFilesFromSharePoint(ClientContext context)
        {
            try
            {
                var web = context.Web;
                var list = web.Lists.GetByTitle("Documents"); // Assuming a default 'Documents' library
                context.Load(list.RootFolder.Files);
                context.ExecuteQuery();

                return list.RootFolder.Files.ToList().Select(f => new SharePointFileInfo
                {
                    Name = f.Name,
                    Length = f.Length,
                    LastWriteTime = f.TimeLastModified,
                    FullName = context.Url + f.ServerRelativeUrl
                }).ToArray();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading SharePoint: {ex.Message}");
                txtLog.AppendText($"Error reading SharePoint: {ex.Message}\n");
                return Array.Empty<SharePointFileInfo>();
            }
        }

        // Compare files between shared drive and SharePoint
        private void CompareFiles(System.IO.FileInfo[] sharedDriveFiles, SharePointFileInfo[] sharePointFiles)
        {
            foreach (var localFile in sharedDriveFiles)
            {
                progressBar.Value += 1;

                var matchingFile = sharePointFiles.FirstOrDefault(spFile => spFile.Name == localFile.Name);
                if (matchingFile == null)
                {
                    txtLog.AppendText($"Missing in SharePoint: {localFile.Name}\n");
                    continue;
                }

                bool isSizeEqual = localFile.Length == matchingFile.Length;
                bool isDateEqual = localFile.LastWriteTime == matchingFile.LastWriteTime;
                bool isContentEqual = CompareFileContent(localFile.FullName, matchingFile.FullName);

                if (isSizeEqual && isDateEqual && isContentEqual)
                {
                    txtLog.AppendText($"Match found: {localFile.Name}\n");
                }
                else
                {
                    txtLog.AppendText($"Mismatch found: {localFile.Name}\n");
                }
            }

            txtLog.AppendText("File comparison completed.\n");
        }

        // Compare file content by computing SHA256 hash
        private bool CompareFileContent(string filePath1, string filePath2)
        {
            try
            {
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    var hash1 = ComputeFileHash(sha256, filePath1);
                    var hash2 = ComputeFileHash(sha256, filePath2);

                    return hash1.SequenceEqual(hash2);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error comparing file content: {ex.Message}");
                txtLog.AppendText($"Error comparing file content: {ex.Message}\n");
                return false;
            }
        }

        // Compute the SHA256 hash of a file
        private byte[] ComputeFileHash(System.Security.Cryptography.SHA256 sha256, string filePath)
        {
            try
            {
                using (var stream = File.OpenRead(filePath))
                {
                    return sha256.ComputeHash(stream);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error computing file hash: {ex.Message}");
                txtLog.AppendText($"Error computing file hash: {ex.Message}\n");
                return Array.Empty<byte>();
            }
        }
    }

    // Custom class to represent SharePoint file information
    public class SharePointFileInfo
    {
        public string Name { get; set; }
        public long Length { get; set; }
        public DateTime LastWriteTime { get; set; }
        public string FullName { get; set; }
    }
}
