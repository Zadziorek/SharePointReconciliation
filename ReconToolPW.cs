using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Windows.Forms;
using Microsoft.SharePoint.Client;
using PnP.Framework;

namespace DesktopApp
{
    public partial class MainForm : Form
    {
        private string accessToken;

        public MainForm()
        {
            InitializeComponent();

            // Listen for the URI activation (e.g., custom URI scheme)
            AppDomain.CurrentDomain.ProcessExit += HandleRedirectUri;
        }

        private void HandleRedirectUri(object sender, EventArgs e)
        {
            string[] args = Environment.GetCommandLineArgs();
            foreach (var arg in args)
            {
                if (arg.StartsWith("yourdesktopapp://token"))
                {
                    var queryParams = new Uri(arg).Query.TrimStart('?').Split('&');
                    foreach (var param in queryParams)
                    {
                        var keyValue = param.Split('=');
                        if (keyValue[0] == "access_token")
                        {
                            accessToken = keyValue[1];
                            MessageBox.Show("Access Token Received");
                        }
                    }
                }
            }
        }

        // Add UI elements in the form's initialization method
        private void InitializeComponent()
        {
            this.localPathTextBox = new System.Windows.Forms.TextBox();
            this.sharePointPathTextBox = new System.Windows.Forms.TextBox();
            this.compareButton = new System.Windows.Forms.Button();
            this.logTextBox = new System.Windows.Forms.TextBox();

            // Set up the form layout (simplified)
            this.localPathTextBox.Location = new System.Drawing.Point(20, 20);
            this.localPathTextBox.Size = new System.Drawing.Size(300, 30);
            this.localPathTextBox.PlaceholderText = "Enter local folder path";

            this.sharePointPathTextBox.Location = new System.Drawing.Point(20, 70);
            this.sharePointPathTextBox.Size = new System.Drawing.Size(300, 30);
            this.sharePointPathTextBox.PlaceholderText = "Enter SharePoint library path";

            this.compareButton.Location = new System.Drawing.Point(20, 120);
            this.compareButton.Size = new System.Drawing.Size(150, 30);
            this.compareButton.Text = "Compare Files";
            this.compareButton.Click += new System.EventHandler(this.CompareFiles_Click);

            this.logTextBox.Location = new System.Drawing.Point(20, 170);
            this.logTextBox.Size = new System.Drawing.Size(400, 200);
            this.logTextBox.Multiline = true;

            this.Controls.Add(this.localPathTextBox);
            this.Controls.Add(this.sharePointPathTextBox);
            this.Controls.Add(this.compareButton);
            this.Controls.Add(this.logTextBox);

            this.Text = "File Comparison Tool";
            this.Size = new System.Drawing.Size(500, 450);
        }

        // Button click event to start file comparison
        private async void CompareFiles_Click(object sender, EventArgs e)
        {
            string localPath = localPathTextBox.Text;
            string sharePointPath = sharePointPathTextBox.Text;

            if (string.IsNullOrEmpty(localPath) || string.IsNullOrEmpty(sharePointPath))
            {
                logTextBox.AppendText("Both paths are required.\n");
                return;
            }

            // Validate local folder path
            if (!Directory.Exists(localPath))
            {
                logTextBox.AppendText("Invalid local folder path.\n");
                return;
            }

            await AccessSharePointAndCompareFiles(localPath, sharePointPath);
        }

        // Function to access SharePoint and compare files
        private async Task AccessSharePointAndCompareFiles(string localPath, string sharePointPath)
        {
            if (accessToken == null)
            {
                MessageBox.Show("Access token not available.");
                return;
            }

            using (ClientContext context = new ClientContext("https://yourtenant.sharepoint.com/sites/SiteName"))
            {
                context.ExecutingWebRequest += (sender, e) =>
                {
                    e.WebRequestExecutor.WebRequest.Headers["Authorization"] = "Bearer " + accessToken;
                };

                // Access the SharePoint document library (replace with actual path)
                var files = context.Web.GetFolderByServerRelativeUrl(sharePointPath).Files;
                context.Load(files);
                await context.ExecuteQueryAsync();

                // Load local files
                var localFiles = Directory.GetFiles(localPath);
                
                // Compare files
                foreach (var localFile in localFiles)
                {
                    var matchingFile = files.FirstOrDefault(f => f.Name == Path.GetFileName(localFile));
                    if (matchingFile != null)
                    {
                        // Compare file properties (size, modified date, etc.)
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

        private System.Windows.Forms.TextBox localPathTextBox;
        private System.Windows.Forms.TextBox sharePointPathTextBox;
        private System.Windows.Forms.Button compareButton;
        private System.Windows.Forms.TextBox logTextBox;
    }
}
