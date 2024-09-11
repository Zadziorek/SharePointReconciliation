using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.SharePoint.Client;
using System.Net;

namespace DesktopApp
{
    public partial class MainForm : Form
    {
        private const string localPathPlaceholder = "Enter local/shared drive folder path";
        private const string sharePointPathPlaceholder = "Enter SharePoint library path";
        private ProgressBar progressBar;
        private int totalFilesAndFolders = 0;
        private int currentProgress = 0;

        // Declare the UI controls
        private TextBox localPathTextBox;
        private TextBox sharePointPathTextBox;
        private Button compareButton;
        private TextBox logTextBox;

        public MainForm()
        {
            InitializeComponent();
            SetupPlaceholders();
        }

        // Initialize UI components
        private void InitializeComponent()
        {
            this.localPathTextBox = new System.Windows.Forms.TextBox();
            this.sharePointPathTextBox = new System.Windows.Forms.TextBox();
            this.compareButton = new System.Windows.Forms.Button();
            this.logTextBox = new System.Windows.Forms.TextBox();
            this.progressBar = new System.Windows.Forms.ProgressBar();

            // Local folder path TextBox
            this.localPathTextBox.Location = new System.Drawing.Point(20, 20);
            this.localPathTextBox.Size = new System.Drawing.Size(300, 30);

            // SharePoint folder path TextBox
            this.sharePointPathTextBox.Location = new System.Drawing.Point(20, 70);
            this.sharePointPathTextBox.Size = new System.Drawing.Size(300, 30);

            // Compare button
            this.compareButton.Location = new System.Drawing.Point(20, 120);
            this.compareButton.Size = new System.Drawing.Size(150, 30);
            this.compareButton.Text = "Compare Folders";
            this.compareButton.Click += new System.EventHandler(this.CompareFolders_Click);

            // Log TextBox
            this.logTextBox.Location = new System.Drawing.Point(20, 170);
            this.logTextBox.Size = new System.Drawing.Size(400, 200);
            this.logTextBox.Multiline = true;

            // Progress bar
            this.progressBar.Location = new System.Drawing.Point(20, 380);
            this.progressBar.Size = new System.Drawing.Size(400, 30);

            // Add controls to the form
            this.Controls.Add(this.localPathTextBox);
            this.Controls.Add(this.sharePointPathTextBox);
            this.Controls.Add(this.compareButton);
            this.Controls.Add(this.logTextBox);
            this.Controls.Add(this.progressBar);

            // Set form properties
            this.Text = "Folder Comparison Tool";
            this.Size = new System.Drawing.Size(500, 450);
        }

        // Set up placeholder texts for the text boxes
        private void SetupPlaceholders()
        {
            localPathTextBox.Text = localPathPlaceholder;
            localPathTextBox.ForeColor = System.Drawing.Color.Gray;
            localPathTextBox.GotFocus += RemovePlaceholderLocal;
            localPathTextBox.LostFocus += SetPlaceholderLocal;

            sharePointPathTextBox.Text = sharePointPathPlaceholder;
            sharePointPathTextBox.ForeColor = System.Drawing.Color.Gray;
            sharePointPathTextBox.GotFocus += RemovePlaceholderSharePoint;
            sharePointPathTextBox.LostFocus += SetPlaceholderSharePoint;
        }

        private void RemovePlaceholderLocal(object sender, EventArgs e)
        {
            if (localPath
            if (localPathTextBox.Text == localPathPlaceholder)
            {
                localPathTextBox.Text = "";
                localPathTextBox.ForeColor = System.Drawing.Color.Black;
            }
        }

        private void SetPlaceholderLocal(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(localPathTextBox.Text))
            {
                localPathTextBox.Text = localPathPlaceholder;
                localPathTextBox.ForeColor = System.Drawing.Color.Gray;
            }
        }

        private void RemovePlaceholderSharePoint(object sender, EventArgs e)
        {
            if (sharePointPathTextBox.Text == sharePointPathPlaceholder)
            {
                sharePointPathTextBox.Text = "";
                sharePointPathTextBox.ForeColor = System.Drawing.Color.Black;
            }
        }

        private void SetPlaceholderSharePoint(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(sharePointPathTextBox.Text))
            {
                sharePointPathTextBox.Text = sharePointPathPlaceholder;
                sharePointPathTextBox.ForeColor = System.Drawing.Color.Gray;
            }
        }

        // Triggered when the Compare button is clicked
        private async void CompareFolders_Click(object sender, EventArgs e)
        {
            string localPath = localPathTextBox.Text;
            string sharePointPath = sharePointPathTextBox.Text;

            // Reset progress bar and logs
            progressBar.Value = 0;
            logTextBox.Clear();
            totalFilesAndFolders = 0;
            currentProgress = 0;

            if (localPath == localPathPlaceholder) localPath = string.Empty;
            if (sharePointPath == sharePointPathPlaceholder) sharePointPath = string.Empty;

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

            try
            {
                logTextBox.AppendText("Starting comparison...\n");

                // Authenticate with SharePoint using the SharePoint path from the text box
                using (var context = AuthenticateToSharePoint(sharePointPath))
                {
                    if (context == null)
                    {
                        logTextBox.AppendText("Authentication failed.\n");
                        return;
                    }

                    // Get folder information from SharePoint
                    var folder = context.Web.GetFolderByServerRelativeUrl(sharePointPath);
                    context.Load(folder);
                    context.Load(folder.Folders);
                    context.Load(folder.Files);
                    await context.ExecuteQueryAsync();

                    // Count total files and folders for progress bar
                    totalFilesAndFolders = Directory.GetFiles(localPath, "*", SearchOption.AllDirectories).Length +
                                           Directory.GetDirectories(localPath, "*", SearchOption.AllDirectories).Length;

                    totalFilesAndFolders += folder.Files.Count + folder.Folders.Count;

                    // Set progress bar max value
                    progressBar.Maximum = totalFilesAndFolders;

                    // Compare folders and files
                    await CompareLocalAndSharePointFolders(localPath, folder, context);
                }

                logTextBox.AppendText("Comparison completed.\n");
            }
            catch (Exception ex)
            {
                logTextBox.AppendText($"Error: {ex.Message}\n");
            }
        }

        // Authenticate to SharePoint using Integrated Windows Authentication (SSO)
        private ClientContext AuthenticateToSharePoint(string siteUrl)
        {
            try
            {
                var context = new ClientContext(siteUrl)
                {
                    Credentials = CredentialCache.DefaultNetworkCredentials
                };

                return context;
            }
            catch (Exception ex)
            {
                logTextBox.AppendText($"Authentication error: {ex.Message}\n");
                return null;
            }
        }

        // Recursive function to compare local and SharePoint folder structures
        private async Task CompareLocalAndSharePointFolders(string localPath, Folder sharePointFolder, ClientContext context)
        {
            // Compare files in current folder
            var localFiles = Directory.GetFiles(localPath);
            foreach (var localFile in localFiles)
            {
                var fileName = Path.GetFileName(localFile);
                var matchingFile = sharePointFolder.Files.FirstOrDefault(f => f.Name == fileName);
                if (matchingFile != null)
                {
                    logTextBox.AppendText($"File exists in both: {fileName}\n");
                }
                else
                {
                    logTextBox.AppendText($"File missing in SharePoint: {fileName}\n");
                }

                UpdateProgress();
            }

            // Compare folders in current folder
            var localFolders = Directory.GetDirectories(localPath);
            foreach (var localFolder in localFolders)
            {
                var folderName = Path.GetFileName(localFolder);
                var matchingFolder = sharePointFolder.Folders.FirstOrDefault(f => f.Name == folderName);
                if (matchingFolder != null)
                {
                    logTextBox.AppendText($"Folder exists in both: {folderName}\n");

                    // Recursive call for sub-folders
                    await CompareLocalAndSharePointFolders(localFolder, matchingFolder, context);
                }
                else
                {
                    logTextBox.AppendText($"Folder missing in SharePoint: {folderName}\n");
                }

                UpdateProgress();
            }
        }

        // Update progress bar as the comparison progresses
        private void UpdateProgress()
        {
            currentProgress++;
            progressBar.Value = Math.Min(currentProgress, totalFilesAndFolders);
        }
    }
}
