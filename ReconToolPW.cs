using System;
using System.Windows.Forms;
using Microsoft.SharePoint.Client;
using PnP.Framework;

namespace ReconciliationTool
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        // Use Windows Integrated Authentication (no username/password needed)
        private ClientContext GetPnPContextWithWindowsAuth(string siteUrl)
        {
            var context = new ClientContext(siteUrl);
            context.Credentials = System.Net.CredentialCache.DefaultCredentials; // Use default Windows credentials
            return context;
        }

        // Use this in the btnReconcile_Click method
        private void btnReconcile_Click(object sender, EventArgs e)
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

                logBuilder.Clear();
                txtLog.Clear();

                // Authenticate using Windows Integrated Authentication
                using (ClientContext context = GetPnPContextWithWindowsAuth(sharePointUrl))
                {
                    // Reconciliation logic goes here
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during the reconciliation process: {ex.Message}");
                logBuilder.AppendLine($"Error: {ex.Message}");
                txtLog.Text = logBuilder.ToString();
            }
        }
    }
}
