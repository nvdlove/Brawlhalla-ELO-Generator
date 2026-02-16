using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace BrawlhallaAutomation
{
    public class EnterpriseKeyDialog : Form
    {
        private TextBox txtKey;
        private Button btnActivate;
        private Button btnClose;
        private Label lblStatus;
        private Label lblAttempts;
        private EnterpriseLicenseManager licenseManager;
        private int remainingAttempts;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        public bool IsLicenseValid { get; private set; } = false;

        public EnterpriseKeyDialog(EnterpriseLicenseManager manager, int attempts)
        {
            licenseManager = manager;
            remainingAttempts = attempts;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // Form settings - Black and Green theme
            this.Text = "";
            this.Size = new Size(450, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.Black;
            this.ForeColor = Color.FromArgb(0, 255, 0); // Matrix green

            // Load icon
            try
            {
                if (System.IO.File.Exists("icon.ico"))
                    this.Icon = new Icon("icon.ico");
            }
            catch { }

            // Title Bar
            Panel titleBar = new Panel
            {
                Height = 60,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(10, 10, 10)
            };

            // Title
            Label lblTitle = new Label
            {
                Text = "> LICENSE ACTIVATION",
                Font = new Font("Consolas", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 255, 0),
                Location = new Point(20, 18),
                AutoSize = true
            };

            // Close Button - Instant exit
            btnClose = new Button
            {
                Text = "X",
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(0, 255, 0),
                BackColor = Color.Black,
                Size = new Size(40, 40),
                Location = new Point(390, 10),
                Font = new Font("Consolas", 14, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(0, 255, 0) }
            };
            btnClose.Click += (s, e) => Application.Exit();

            titleBar.Controls.Add(lblTitle);
            titleBar.Controls.Add(btnClose);

            // Allow dragging
            titleBar.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(this.Handle, WM_NCLBUTTONDOWN, new IntPtr(HT_CAPTION), IntPtr.Zero);
                }
            };

            this.Controls.Add(titleBar);

            // Main Content
            Panel contentPanel = new Panel
            {
                Location = new Point(25, 80),
                Size = new Size(400, 380),
                BackColor = Color.Black
            };

            // Icon
            Label iconLabel = new Label
            {
                Text = "🔒",
                Font = new Font("Consolas", 48),
                ForeColor = Color.FromArgb(0, 255, 0),
                Location = new Point(160, 20),
                AutoSize = true
            };
            contentPanel.Controls.Add(iconLabel);

            // Instruction
            Label lblInstruction = new Label
            {
                Text = "> ENTER LICENSE KEY <",
                Font = new Font("Consolas", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 255, 0),
                Location = new Point(0, 90),
                Size = new Size(400, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };
            contentPanel.Controls.Add(lblInstruction);

            // Key TextBox
            txtKey = new TextBox
            {
                Location = new Point(50, 130),
                Size = new Size(300, 30),
                BackColor = Color.Black,
                ForeColor = Color.FromArgb(0, 255, 0),
                Font = new Font("Consolas", 11),
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = HorizontalAlignment.Center
            };
            contentPanel.Controls.Add(txtKey);

            // Status
            lblStatus = new Label
            {
                Text = "",
                Font = new Font("Consolas", 9),
                ForeColor = Color.FromArgb(0, 255, 0),
                Location = new Point(0, 175),
                Size = new Size(400, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            contentPanel.Controls.Add(lblStatus);

            // Attempts
            lblAttempts = new Label
            {
                Text = $"> ATTEMPTS: {remainingAttempts}/3 <",
                Font = new Font("Consolas", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 200, 0),
                Location = new Point(0, 210),
                Size = new Size(400, 25),
                TextAlign = ContentAlignment.MiddleCenter
            };
            contentPanel.Controls.Add(lblAttempts);

            // Progress bars (simple)
            Panel progressPanel = new Panel
            {
                Location = new Point(100, 245),
                Size = new Size(200, 10),
                BackColor = Color.FromArgb(20, 20, 20)
            };

            for (int i = 0; i < 3; i++)
            {
                Panel bar = new Panel
                {
                    Location = new Point(5 + (i * 65), 0),
                    Size = new Size(60, 10),
                    BackColor = i < remainingAttempts ? Color.FromArgb(0, 255, 0) : Color.FromArgb(40, 40, 40)
                };
                progressPanel.Controls.Add(bar);
            }
            contentPanel.Controls.Add(progressPanel);

            // Activate Button
            btnActivate = new Button
            {
                Text = "[ ACTIVATE ]",
                Location = new Point(125, 280),
                Size = new Size(150, 45),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Consolas", 12, FontStyle.Bold),
                BackColor = Color.Black,
                ForeColor = Color.FromArgb(0, 255, 0),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(0, 255, 0) }
            };
            btnActivate.Click += BtnActivate_Click;
            contentPanel.Controls.Add(btnActivate);

            // Footer
            Label footer = new Label
            {
                Text = "> SYSTEM READY <",
                Font = new Font("Consolas", 8),
                ForeColor = Color.FromArgb(0, 100, 0),
                Location = new Point(0, 350),
                Size = new Size(400, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            contentPanel.Controls.Add(footer);

            this.Controls.Add(contentPanel);
        }

        private async void BtnActivate_Click(object sender, EventArgs e)
        {
            string key = txtKey.Text.Trim();

            if (string.IsNullOrWhiteSpace(key))
            {
                lblStatus.Text = "> ERROR: KEY REQUIRED <";
                return;
            }

            btnActivate.Enabled = false;
            btnActivate.Text = "[ CHECKING ]";

            try
            {
                var result = await licenseManager.ValidateLicenseWithServerAsync(key);

                if (result != null && result.IsValid)
                {
                    lblStatus.Text = "> ACCESS GRANTED <";
                    lblStatus.ForeColor = Color.FromArgb(0, 255, 0);
                    btnActivate.Text = "[ ACTIVATED ]";

                    IsLicenseValid = true;

                    await Task.Delay(1000);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    remainingAttempts--;
                    licenseManager.IncrementAttempt();

                    lblStatus.Text = "> ACCESS DENIED <";
                    lblStatus.ForeColor = Color.Red;
                    lblAttempts.Text = $"> ATTEMPTS: {remainingAttempts}/3 <";

                    btnActivate.Enabled = true;
                    btnActivate.Text = "[ ACTIVATE ]";

                    if (remainingAttempts <= 0)
                    {
                        lblStatus.Text = "> SYSTEM LOCKED <";
                        await Task.Delay(1500);
                        Application.Exit();
                    }
                }
            }
            catch
            {
                lblStatus.Text = "> CONNECTION ERROR <";
                btnActivate.Enabled = true;
                btnActivate.Text = "[ ACTIVATE ]";
            }
        }
    }
}