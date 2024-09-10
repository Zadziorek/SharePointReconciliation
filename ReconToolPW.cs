using System;
using System.Drawing;
using System.Windows.Forms;

namespace DesktopApp
{
    public partial class MainForm : Form
    {
        private const string localPathPlaceholder = "Enter local folder path";
        private const string sharePointPathPlaceholder = "Enter SharePoint library path";

        public MainForm()
        {
            InitializeComponent();
            SetupPlaceholders();
        }

        private void SetupPlaceholders()
        {
            // Set placeholder for localPathTextBox
            localPathTextBox.Text = localPathPlaceholder;
            localPathTextBox.ForeColor = Color.Gray;
            localPathTextBox.GotFocus += RemovePlaceholderLocal;
            localPathTextBox.LostFocus += SetPlaceholderLocal;

            // Set placeholder for sharePointPathTextBox
            sharePointPathTextBox.Text = sharePointPathPlaceholder;
            sharePointPathTextBox.ForeColor = Color.Gray;
            sharePointPathTextBox.GotFocus += RemovePlaceholderSharePoint;
            sharePointPathTextBox.LostFocus += SetPlaceholderSharePoint;
        }

        private void RemovePlaceholderLocal(object sender, EventArgs e)
        {
            if (localPathTextBox.Text == localPathPlaceholder)
            {
                localPathTextBox.Text = "";
                localPathTextBox.ForeColor = Color.Black;
            }
        }

        private void SetPlaceholderLocal(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(localPathTextBox.Text))
            {
                localPathTextBox.Text = localPathPlaceholder;
                localPathTextBox.ForeColor = Color.Gray;
            }
        }

        private void RemovePlaceholderSharePoint(object sender, EventArgs e)
        {
            if (sharePointPathTextBox.Text == sharePointPathPlaceholder)
            {
                sharePointPathTextBox.Text = "";
                sharePointPathTextBox.ForeColor = Color.Black;
            }
        }

        private void SetPlaceholderSharePoint(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(sharePointPathTextBox.Text))
            {
                sharePointPathTextBox.Text = sharePointPathPlaceholder;
                sharePointPathTextBox.ForeColor = Color.Gray;
            }
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

            this.sharePointPathTextBox.Location = new System.Drawing.Point(20, 70);
            this.sharePointPathTextBox.Size = new System.Drawing.Size(300, 30);

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

            // Check if the text boxes contain the placeholder text, and treat them as empty
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

            await AccessSharePointAndCompareFiles(localPath, sharePointPath);
        }

        // Function to access SharePoint and compare files (same as before)
        private async Task AccessSharePointAndCompareFiles(string localPath, string sharePointPath)
        {
            // CSOM logic for SharePoint access and file comparison goes here...
        }

        private System.Windows.Forms.TextBox localPathTextBox;
        private System.Windows.Forms.TextBox sharePointPathTextBox;
        private System.Windows.Forms.Button compareButton;
        private System.Windows.Forms.TextBox logTextBox;
    }
}
