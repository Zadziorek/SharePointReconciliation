using System;
using System.IO; // For file operations on the local system
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Microsoft.SharePoint.Client; // For SharePoint operations

namespace ReconciliationTool
{
    public partial class MainForm : Form
    {
        private StringBuilder logBuilder = new StringBuilder();

        public MainForm()
        {
            InitializeComponent();
        }

        private void btnReconcile_Click(object sender, EventArgs e)
        {
            try
            {
                string sharedDrivePath = txtSharedDrivePath.Text;
                string sharePointUrl = txtSharePointPath.Text;
                string userName = txtUserName.Text;
                string password = txtPassword.Text;

                if (string.IsNullOrWhiteSpace(sharedDrivePath) || string.IsNullOrWhiteSpace(sharePointUrl) ||
                    string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show("Please provide all required fields (paths, username, and password).");
                    return;
                }

                logBuilder.Clear();
                txtLog.Clear();

                // Use System.IO.FileInfo explicitly for local file system operations
                var sharedDriveFiles = GetFilesFromSharedDrive(sharedDrivePath);
                var sharePointFiles = GetFilesFromSharePoint(sharePointUrl, userName, password);

                progressBar.Maximum = sharedDriveFiles.Length;
                progressBar.Value = 0;

                CompareFiles(sharedDriveFiles, sharePointFiles);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during the reconciliation process: {ex.Message}");
                logBuilder.AppendLine($"Error: {ex.Message}");
                txtLog.Text = logBuilder.ToString();
            }
        }

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
                logBuilder.AppendLine($"Error reading shared drive: {ex.Message}");
                return Array.Empty<System.IO.FileInfo>();
            }
        }

        // SharePoint-specific file retrieval
        private SharePointFileInfo[] GetFilesFromSharePoint(string sharePointUrl, string userName, string password)
        {
            try
            {
                using (var clientContext = new ClientContext(sharePointUrl))
                {
                    SecureString securePassword = new SecureString();
                    foreach (char c in password)
                    {
                        securePassword.AppendChar(c);
                    }

                    clientContext.Credentials = new SharePointOnlineCredentials(userName, securePassword);

                    var web = clientContext.Web;
                    var list = web.Lists.GetByTitle("Documents"); // Assuming a default 'Documents' library
                    clientContext.Load(list.RootFolder.Files);
                    clientContext.ExecuteQuery();

                    return list.RootFolder.Files.ToList().Select(f => new SharePointFileInfo
                    {
                        Name = f.Name,
                        Length = f.Length,
                        LastWriteTime = f.TimeLastModified,
                        FullName = sharePointUrl + f.ServerRelativeUrl
                    }).ToArray();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading SharePoint: {ex.Message}");
                logBuilder.AppendLine($"Error reading SharePoint: {ex.Message}");
                return Array.Empty<SharePointFileInfo>();
            }
        }

        // Definition for CompareFiles function
        private void CompareFiles(System.IO.FileInfo[] sharedDriveFiles, SharePointFileInfo[] sharePointFiles)
        {
            foreach (var localFile in sharedDriveFiles)
            {
                progressBar.Value += 1;

                var matchingFile = sharePointFiles.FirstOrDefault(spFile => spFile.Name == localFile.Name);
                if (matchingFile == null)
                {
                    logBuilder.AppendLine($"Missing in SharePoint: {localFile.Name}");
                    continue;
                }

                bool isSizeEqual = localFile.Length == matchingFile.Length;
                bool isDateEqual = localFile.LastWriteTime == matchingFile.LastWriteTime;
                bool isContentEqual = CompareFileContent(localFile.FullName, matchingFile.FullName);

                if (isSizeEqual && isDateEqual && isContentEqual)
                {
                    logBuilder.AppendLine($"Match found: {localFile.Name}");
                }
                else
                {
                    logBuilder.AppendLine($"Mismatch found: {localFile.Name}");
                }
            }

            txtLog.Text = logBuilder.ToString();
        }

        private bool CompareFileContent(string filePath1, string filePath2)
        {
            try
            {
                using (var sha256 = SHA256.Create())
                {
                    var hash1 = ComputeFileHash(sha256, filePath1);
                    var hash2 = ComputeFileHash(sha256, filePath2);

                    return hash1.SequenceEqual(hash2);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error comparing file content: {ex.Message}");
                logBuilder.AppendLine($"Error comparing file content: {ex.Message}");
                return false;
            }
        }

        private byte[] ComputeFileHash(SHA256 sha256, string filePath)
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
                logBuilder.AppendLine($"Error computing file hash: {ex.Message}");
                return Array.Empty<byte>();
            }
        }

        // Form Fields
        private TextBox txtSharedDrivePath;
        private TextBox txtSharePointPath;
        private TextBox txtUserName;
        private TextBox txtPassword;
        private Button btnReconcile;
        private Button btnSaveLog;
        private RichTextBox txtLog;
        private ProgressBar progressBar;
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
