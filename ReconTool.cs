using System;
using System.IO;
using System.Linq;
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

        private void btnReconcile_Click(object sender, EventArgs e)
        {
            try
            {
                string sharedDrivePath = txtSharedDrivePath.Text;
                string sharePointPath = txtSharePointPath.Text;

                if (string.IsNullOrWhiteSpace(sharedDrivePath) || string.IsNullOrWhiteSpace(sharePointPath))
                {
                    MessageBox.Show("Please provide both paths.");
                    return;
                }

                logBuilder.Clear();
                txtLog.Clear();

                var sharedDriveFiles = GetFilesFromSharedDrive(sharedDrivePath);
                var sharePointFiles = GetFilesFromSharePoint(sharePointPath);

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

        private void CompareFiles(FileInfo[] sharedDriveFiles, FileInfo[] sharePointFiles)
        {
            try
            {
                foreach (var file in sharedDriveFiles)
                {
                    progressBar.Value += 1;

                    var matchingFile = sharePointFiles.FirstOrDefault(spf => spf.Name == file.Name);
                    if (matchingFile == null)
                    {
                        logBuilder.AppendLine($"Missing in SharePoint: {file.Name}");
                        continue;
                    }

                    bool isSizeEqual = file.Length == matchingFile.Length;
                    bool isDateEqual = file.LastWriteTime == matchingFile.LastWriteTime;
                    bool isContentEqual = CompareFileContent(file.FullName, matchingFile.FullName);

                    if (isSizeEqual && isDateEqual && isContentEqual)
                    {
                        logBuilder.AppendLine($"Match found: {file.Name}");
                    }
                    else
                    {
                        logBuilder.AppendLine($"Mismatch found: {file.Name}");
                    }
                }

                txtLog.Text = logBuilder.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during file comparison: {ex.Message}");
                logBuilder.AppendLine($"Error during comparison: {ex.Message}");
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

        private FileInfo[] GetFilesFromSharePoint(string sharePointUrl)
        {
            try
            {
                var clientContext = new ClientContext(sharePointUrl);
                // Add authentication if required
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
    }

    public class FileInfo
    {
        public string Name { get; set; }
        public long Length { get; set; }
        public DateTime LastWriteTime { get; set; }
        public string FullName { get; set; }
    }
}
