using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.SharePoint.Client;

namespace DesktopApp
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        // Add UI elements in the form's initialization method
        private void InitializeComponent()
        {
            this.localPathTextBox = new System.Windows.Forms.TextBox();
            this.sharePointPathTextBox = new System.Windows.Forms.TextBox();
            this.compareButton = new System.Windows.Forms.Button();
            this.logTextBox = new System.Windows.Forms.TextBox();

            // Set up the form layout
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

            // Perform file comparison using CSOM and Windows Integrated Authentication
            await AccessSharePointAndCompareFiles(localPath, sharePointPath);
        }

        // Function to access SharePoint using Windows Integrated Authentication and compare files
        private async Task AccessSharePointAndCompareFiles(string localPath, string sharePointPath)
        {
            try
            {
                // Using CSOM to connect to SharePoint Online
                using (ClientContext context = new ClientContext("https://yourtenant.sharepoint.com/sites/SiteName"))
                {
                    // Use the current Windows credentials to authenticate (Windows Integrated Authentication)
                    context.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials;

                    // Access the SharePoint document library (replace with actual path)
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

        private System.Windows.Forms.TextBox localPathTextBox;
        private System.Windows.Forms.TextBox sharePointPathTextBox;
        private System.Windows.Forms.Button compareButton;
        private System.Windows.Forms.TextBox logTextBox;
    }
}
