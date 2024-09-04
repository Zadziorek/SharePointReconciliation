using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Microsoft.SharePoint.Client;

namespace ReconciliationTool
{
    public partial class MainForm : Form
    {
        private StringBuilder logBuilder = new StringBuilder();

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.txtSharedDrivePath = new TextBox();
            this.txtSharePointPath = new TextBox();
            this.txtUserName = new TextBox();
            this.txtPassword = new TextBox();
            this.btnReconcile = new Button();
            this.btnSaveLog = new Button();
            this.txtLog = new RichTextBox();
            this.progressBar = new ProgressBar();

            this.SuspendLayout();

            // Shared Drive Path TextBox
            this.txtSharedDrivePath.Location = new System.Drawing.Point(30, 30);
            this.txtSharedDrivePath.Name = "txtSharedDrivePath";
            this.txtSharedDrivePath.Size = new System.Drawing.Size(300, 20);
            this.txtSharedDrivePath.PlaceholderText = "Enter Shared Drive Path";

            // SharePoint Path TextBox
            this.txtSharePointPath.Location = new System.Drawing.Point(30, 70);
            this.txtSharePointPath.Name = "txtSharePointPath";
            this.txtSharePointPath.Size = new System.Drawing.Size(300, 20);
            this.txtSharePointPath.PlaceholderText = "Enter SharePoint URL";

            // Username TextBox
            this.txtUserName.Location = new System.Drawing.Point(30, 110);
            this.txtUserName.Name = "txtUserName";
            this.txtUserName.Size = new System.Drawing.Size(300, 20);
            this.txtUserName.PlaceholderText = "Enter SharePoint Username";

            // Password TextBox
            this.txtPassword.Location = new System.Drawing.Point(30, 150);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(300, 20);
            this.txtPassword.UseSystemPasswordChar = true; // To hide password input
            this.txtPassword.PlaceholderText = "Enter SharePoint Password";

            // Reconcile Button
            this.btnReconcile.Location = new System.Drawing.Point(30, 190);
            this.btnReconcile.Name = "btnReconcile";
            this.btnReconcile.Size = new System.Drawing.Size(100, 30);
            this.btnReconcile.Text = "Reconcile";
            this.btnReconcile.Click += new EventHandler(this.btnReconcile_Click);

            // Save Log Button
            this.btnSaveLog.Location = new System.Drawing.Point(150, 190);
            this.btnSaveLog.Name = "btnSaveLog";
            this.btnSaveLog.Size = new System.Drawing.Size(100, 30);
            this.btnSaveLog.Text = "Save Log";
            this.btnSaveLog.Click += new EventHandler(this.btnSaveLog_Click);

            // Log TextBox
            this.txtLog.Location = new System.Drawing.Point(30, 240);
            this.txtLog.Name = "txtLog";
            this.txtLog.Size = new System.Drawing.Size(300, 150);
            this.txtLog.ReadOnly = true;

            // ProgressBar
            this.progressBar.Location = new System.Drawing.Point(30, 410);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(300, 20);

            // MainForm Settings
            this.ClientSize = new System.Drawing.Size(380, 450);
            this.Controls.Add(this.txtSharedDrivePath);
            this.Controls.Add(this.txtSharePointPath);
            this.Controls.Add(this.txtUserName);
            this.Controls.Add(this.txtPassword);
            this.Controls.Add(this.btnReconcile);
            this.Controls.Add(this.btnSaveLog);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.progressBar);
            this.Name = "MainForm";
            this.Text = "File Reconciliation Tool";
            this.ResumeLayout(false);
            this.PerformLayout();
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

        private FileInfo[] GetFilesFromSharedDrive(string path)
        {
            try
            {
                return new DirectoryInfo(path).GetFiles("*", SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading shared drive: {ex.Message}");
                logBuilder.AppendLine($"Error reading shared drive: {ex.Message}");
                return Array.Empty<FileInfo>();
            }
        }

        private FileInfo[] GetFilesFromSharePoint(string sharePointUrl, string userName, string password)
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

                    return list.RootFolder.Files.ToList().Select(f => new FileInfo
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
                return Array.Empty<FileInfo>();
            }
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

    public class FileInfo
    {
        public string Name { get; set; }
        public long Length { get; set; }
        public DateTime LastWriteTime { get; set; }
        public string FullName { get; set; }
    }
}
