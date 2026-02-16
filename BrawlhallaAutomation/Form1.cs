using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;
using Rect = OpenCvSharp.Rect;

namespace BrawlhallaAutomation
{
    public class WebhookInputDialog : Form
    {
        private TextBox txtWebhook;
        private Button btnSave;
        private Button btnCancel;
        private Button btnTest;

        public string WebhookUrl { get; private set; }

        public WebhookInputDialog(string currentUrl = "")
        {
            InitializeUI(currentUrl);
        }

        private void InitializeUI(string currentUrl)
        {
            this.Text = "Set Discord Webhook URL";
            this.Size = new Size(600, 220);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.Black;
            this.TopMost = true;

            var titleBar = new Panel
            {
                Height = 30,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(30, 30, 30)
            };

            var titleLabel = new Label
            {
                Text = "Discord Webhook Configuration",
                ForeColor = Color.White,
                Font = new Font("Consolas", 9),
                Location = new Point(10, 5),
                AutoSize = true
            };

            var btnClose = CreateTransparentButton("✕", new Point(570, 0),
                (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); });

            titleBar.Controls.Add(titleLabel);
            titleBar.Controls.Add(btnClose);

            var lblInstruction = new Label
            {
                Text = "Enter Discord Webhook URL (leave empty to disable):",
                ForeColor = Color.Cyan,
                Font = new Font("Consolas", 9),
                Location = new Point(20, 40),
                AutoSize = true
            };

            txtWebhook = new TextBox
            {
                Location = new Point(20, 70),
                Size = new Size(540, 25),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 9)
            };

            if (!string.IsNullOrEmpty(currentUrl))
                txtWebhook.Text = currentUrl;

            var lblHelp = new Label
            {
                Text = "Get webhook from Discord: Server Settings → Integrations → Webhooks",
                ForeColor = Color.Gray,
                Font = new Font("Consolas", 7),
                Location = new Point(20, 100),
                AutoSize = true
            };

            btnTest = new Button
            {
                Text = "TEST",
                Location = new Point(300, 140),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(60, 60, 120),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Consolas", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btnTest.FlatAppearance.BorderSize = 0;
            btnTest.Click += async (s, e) => await TestWebhook();

            btnSave = new Button
            {
                Text = "SAVE",
                Location = new Point(400, 140),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(0, 120, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Consolas", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) =>
            {
                WebhookUrl = txtWebhook.Text.Trim();
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            btnCancel = new Button
            {
                Text = "CANCEL",
                Location = new Point(490, 140),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(120, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Consolas", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            this.Controls.Add(titleBar);
            this.Controls.Add(lblInstruction);
            this.Controls.Add(txtWebhook);
            this.Controls.Add(lblHelp);
            this.Controls.Add(btnTest);
            this.Controls.Add(btnSave);
            this.Controls.Add(btnCancel);

            titleBar.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(this.Handle, WM_NCLBUTTONDOWN, new IntPtr(HT_CAPTION), IntPtr.Zero);
                }
            };
        }

        private Button CreateTransparentButton(string text, Point location, EventHandler clickHandler)
        {
            var btn = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(180, 180, 180),
                BackColor = Color.Transparent,
                Size = new Size(30, 30),
                Location = location,
                Font = new Font("Segoe UI", 11, FontStyle.Regular),
                Cursor = Cursors.Hand,
                TabStop = false
            };

            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btn.FlatAppearance.MouseOverBackColor = Color.Transparent;

            btn.MouseEnter += (s, e) => btn.ForeColor = Color.White;
            btn.MouseLeave += (s, e) => btn.ForeColor = Color.FromArgb(180, 180, 180);

            if (clickHandler != null)
                btn.Click += clickHandler;

            return btn;
        }

        private async Task TestWebhook()
        {
            string url = txtWebhook.Text.Trim();

            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("Please enter a webhook URL first", "Test Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!IsValidWebhookUrl(url))
            {
                MessageBox.Show("URL doesn't look like a valid Discord webhook.\n" +
                              "Format: https://discord.com/api/webhooks/...", "Warning",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnTest.Enabled = false;
            btnTest.Text = "TESTING...";
            btnTest.BackColor = Color.FromArgb(90, 90, 150);

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string json = $"{{\"content\": \"✅ Webhook test from Brawlhalla Auto Queue\\nTime: {DateTime.Now:HH:mm:ss}\"}}";
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("✅ Webhook test successful!\nCheck your Discord channel.", "Test Passed",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"❌ Webhook test failed: {response.StatusCode}\nCheck the URL and try again.", "Test Failed",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Webhook test error: {ex.Message}", "Test Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnTest.Enabled = true;
                btnTest.Text = "TEST";
                btnTest.BackColor = Color.FromArgb(60, 60, 120);
            }
        }

        private bool IsValidWebhookUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            return Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult)
                   && (uriResult.Scheme == Uri.UriSchemeHttps)
                   && uriResult.Host.Contains("discord.com")
                   && uriResult.AbsolutePath.Contains("/api/webhooks/");
        }

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // WebhookInputDialog
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Name = "WebhookInputDialog";
            this.Load += new System.EventHandler(this.WebhookInputDialog_Load);
            this.ResumeLayout(false);

        }

        private void WebhookInputDialog_Load(object sender, EventArgs e)
        {

        }
    }

    public class AppSettings
    {
        public double MatchThreshold { get; set; } = 0.68;
        public int WindowWidth { get; set; } = 1254;
        public int WindowHeight { get; set; } = 657;
        public int CheckInterval { get; set; } = 600;
        public int CooldownSeconds { get; set; } = 25;
        public int StartupDelay { get; set; } = 35;
        public string DiscordWebhookUrl { get; set; } = "";
    }

    public partial class Form1 : Form
    {
        private TextBox txtLog;
        private Panel titleBar;
        private Button btnClose;
        private Button btnMinimize;

        private bool running = false;
        private int frameCount = 0;
        private bool matchFound = false;
        private DateTime lastMatchTime = DateTime.MinValue;
        private int queueCount = 0;
        private bool autoStart = false;
        private bool isRunningSequence = false;
        private readonly object sequenceLock = new object();
        private bool isPaused = false;
        private CancellationTokenSource monitoringCts;

        private double MATCH_THRESHOLD = 0.68;
        private readonly string searchingTemplatePath = @"Templates\searching.png";
        private readonly string matchTemplatePath = @"Templates\match.png";
        private readonly string reconnectTemplatePath = @"Templates\reconnect_popup.png";
        private Size targetWindowSize = new Size(1254, 657);
        private AppSettings settings = new AppSettings();
        private EnterpriseLicenseManager licenseManager;

        private Mat searchingTpl = null;
        private Mat matchTpl = null;
        private Mat reconnectTpl = null;

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref RECT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        private const int KEYEVENTF_KEYDOWN = 0x0000;
        private const int KEYEVENTF_KEYUP = 0x0002;
        private const byte VK_S = 0x53;
        private const byte VK_C = 0x43;
        private const byte VK_ESCAPE = 0x1B;

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;
        private const int WM_VSCROLL = 0x0115;
        private const int SB_LINEUP = 0;
        private const int SB_LINEDOWN = 1;

        public Form1()
        {
            licenseManager = new EnterpriseLicenseManager();

            if (!CheckLicense())
            {
                Environment.Exit(0);
                return;
            }

            InitializeUI();
            EnsureTemplateDirectory();
            LoadSettings();

            if (ValidateSettings())
            {
                autoStart = true;
                monitoringCts = new CancellationTokenSource();
                _ = Task.Run(async () => await StartMonitoringLoop(monitoringCts.Token));
            }
            else
            {
                Log("❌ Invalid settings detected. Please check configuration.");
            }
        }

        private bool CheckLicense()
        {
            try
            {
                if (licenseManager.IsLicenseValid)
                {
                    return true;
                }

                int attempts = 0;
                while (attempts < 3)
                {
                    using (var licenseDialog = new EnterpriseKeyDialog(licenseManager, 3 - attempts))
                    {
                        var result = licenseDialog.ShowDialog();

                        if (result == DialogResult.OK && licenseDialog.IsLicenseValid)
                        {
                            Log("✓ License validated successfully");
                            return true;
                        }

                        attempts++;

                        if (attempts < 3)
                        {
                            if (MessageBox.Show($"License validation failed. {3 - attempts} attempts remaining.\n\nTry again?",
                                "License Error", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                            {
                                break;
                            }
                        }
                    }
                }

                MessageBox.Show("License validation failed. Application will exit.",
                    "License Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"License error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void EnsureTemplateDirectory()
        {
            try
            {
                if (!Directory.Exists("Templates"))
                {
                    Directory.CreateDirectory("Templates");
                    Log("📁 Created Templates folder");
                    Log("   Please add template images:");
                    Log("   - Templates/searching.png");
                    Log("   - Templates/match.png");
                    Log("   - Templates/reconnect_popup.png");
                }

                bool hasSearching = File.Exists(searchingTemplatePath);
                bool hasMatch = File.Exists(matchTemplatePath);

                if (!hasSearching || !hasMatch)
                {
                    Log("⚠️  WARNING: Template images missing!");
                    if (!hasSearching) Log($"   Missing: {searchingTemplatePath}");
                    if (!hasMatch) Log($"   Missing: {matchTemplatePath}");
                }
            }
            catch (Exception ex)
            {
                Log($"⚠️  Template check error: {ex.Message}");
            }
        }

        private Button CreateTransparentButton(string text, Point location, EventHandler clickHandler)
        {
            var btn = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(180, 180, 180),
                BackColor = Color.Transparent,
                Size = new Size(30, 30),
                Location = location,
                Font = new Font("Segoe UI", 11, FontStyle.Regular),
                Cursor = Cursors.Hand,
                TabStop = false
            };

            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btn.FlatAppearance.MouseOverBackColor = Color.Transparent;

            btn.MouseEnter += (s, e) =>
            {
                btn.ForeColor = Color.White;
                btn.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            };

            btn.MouseLeave += (s, e) =>
            {
                btn.ForeColor = Color.FromArgb(180, 180, 180);
                btn.Font = new Font("Segoe UI", 11, FontStyle.Regular);
            };

            btn.MouseDown += (s, e) => btn.ForeColor = Color.FromArgb(150, 150, 150);
            btn.MouseUp += (s, e) => btn.ForeColor = Color.White;

            if (clickHandler != null)
                btn.Click += clickHandler;

            return btn;
        }

        private void InitializeUI()
        {
            Text = "Brawlhalla ELO Generator v1.0";
            this.Size = new Size(800, 500);
            StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.Black;

            try
            {
                if (File.Exists("icon.ico"))
                    this.Icon = new Icon("icon.ico");
            }
            catch
            {
                // Use default icon
            }

            this.TopMost = true;

            titleBar = new Panel
            {
                Height = 25,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(30, 30, 30)
            };

            var titleLabel = new Label
            {
                Text = "Brawlhalla ELO Generator v1.0",
                ForeColor = Color.White,
                Font = new Font("Consolas", 9),
                Location = new Point(10, 5),
                AutoSize = true
            };

            btnClose = CreateTransparentButton("✕", new Point(770, 0), (s, e) => Application.Exit());
            btnMinimize = CreateTransparentButton("─", new Point(740, 0), (s, e) => this.WindowState = FormWindowState.Minimized);

            titleBar.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(this.Handle, WM_NCLBUTTONDOWN, new IntPtr(HT_CAPTION), IntPtr.Zero);
                }
            };

            titleBar.Controls.Add(titleLabel);
            titleBar.Controls.Add(btnMinimize);
            titleBar.Controls.Add(btnClose);

            txtLog = new TextBox
            {
                Multiline = true,
                Location = new Point(0, 30),
                Size = new Size(760, 445),
                BackColor = Color.Black,
                ForeColor = Color.Lime,
                Font = new Font("Consolas", 10),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                ScrollBars = ScrollBars.None,
                WordWrap = true
            };

            this.MouseWheel += (s, e) =>
            {
                Point mousePos = txtLog.PointToClient(Cursor.Position);
                if (txtLog.ClientRectangle.Contains(mousePos))
                {
                    int lines = Math.Abs(e.Delta) / 120 * 3;

                    for (int i = 0; i < lines; i++)
                    {
                        if (e.Delta > 0)
                        {
                            SendMessage(txtLog.Handle, WM_VSCROLL, (IntPtr)SB_LINEUP, IntPtr.Zero);
                        }
                        else
                        {
                            SendMessage(txtLog.Handle, WM_VSCROLL, (IntPtr)SB_LINEDOWN, IntPtr.Zero);
                        }
                    }
                }
            };

            txtLog.MouseEnter += (s, e) => txtLog.Focus();

            var statusBar = new Panel
            {
                Height = 20,
                Dock = DockStyle.Bottom,
                BackColor = Color.FromArgb(30, 30, 30)
            };

            var statusLabel = new Label
            {
                Text = "STATUS: INITIALIZING...",
                ForeColor = Color.Cyan,
                Font = new Font("Consolas", 9),
                Location = new Point(10, 3),
                AutoSize = true,
                Name = "statusLabel"
            };

            var queueLabel = new Label
            {
                Text = "QUEUES: 0",
                ForeColor = Color.Yellow,
                Font = new Font("Consolas", 9),
                Location = new Point(200, 3),
                AutoSize = true,
                Name = "queueLabel"
            };

            var pauseLabel = new Label
            {
                Text = "PAUSED: NO",
                ForeColor = Color.Orange,
                Font = new Font("Consolas", 9),
                Location = new Point(350, 3),
                AutoSize = true,
                Name = "pauseLabel"
            };

            var timeLabel = new Label
            {
                Text = DateTime.Now.ToString("HH:mm:ss"),
                ForeColor = Color.White,
                Font = new Font("Consolas", 9),
                Location = new Point(735, 3),
                AutoSize = true,
                Name = "timeLabel"
            };

            statusBar.Controls.Add(statusLabel);
            statusBar.Controls.Add(queueLabel);
            statusBar.Controls.Add(pauseLabel);
            statusBar.Controls.Add(timeLabel);

            var timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000;
            timer.Tick += (s, e) => timeLabel.Text = DateTime.Now.ToString("HH:mm:ss");
            timer.Start();

            var hotkeyLabel = new Label
            {
                Text = "[F1] Stop/Restart [F2] Webhook URL [F3] Manual Nav [F4] Pause",
                ForeColor = Color.Gray,
                Font = new Font("Consolas", 8),
                Location = new Point(425, 465),
                AutoSize = true
            };

            this.Controls.Add(txtLog);
            this.Controls.Add(titleBar);
            this.Controls.Add(statusBar);
            this.Controls.Add(hotkeyLabel);

            titleBar.BringToFront();
            statusBar.BringToFront();
            hotkeyLabel.BringToFront();

            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;

            Log("  Brawlhalla ELO Generator v1.0 nvdlove & antireplay  ");
            Log("======================================================");
            Log("                    CONFIG");
            Log($"   • Threshold: {MATCH_THRESHOLD * 100}%");
            Log($"   • Window Size: {targetWindowSize.Width}x{targetWindowSize.Height}");
            Log($"   • Discord: {(string.IsNullOrEmpty(settings.DiscordWebhookUrl) ? "DISABLED (press F2)" : "ENABLED")}");
            Log($"   • Auto-Nav: ESC → S → C → C → C (35s delay) ");
            Log("======================================================");

            if (autoStart)
            {
                Log(" AUTO-STARTING MONITORING...");
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists("settings.json"))
                {
                    var json = File.ReadAllText("settings.json");
                    var loadedSettings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (loadedSettings != null)
                    {
                        settings = loadedSettings;
                        MATCH_THRESHOLD = settings.MatchThreshold;
                        targetWindowSize = new Size(settings.WindowWidth, settings.WindowHeight);
                        Log($" Settings loaded: {MATCH_THRESHOLD * 100}% threshold");
                    }
                }
                else
                {
                    SaveSettings();
                }
            }
            catch (Exception ex)
            {
                Log($"   ⚠️  Settings error: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                settings.MatchThreshold = MATCH_THRESHOLD;
                settings.WindowWidth = targetWindowSize.Width;
                settings.WindowHeight = targetWindowSize.Height;

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText("settings.json", json, Encoding.UTF8);
                Log("   ✓ Settings saved");
            }
            catch (Exception ex)
            {
                Log($"   ✗ Save settings error: {ex.Message}");
            }
        }

        private bool ValidateSettings()
        {
            bool isValid = true;

            if (settings.MatchThreshold < 0.1 || settings.MatchThreshold > 1.0)
            {
                Log("❌ Invalid threshold (must be between 10% and 100%)");
                settings.MatchThreshold = 0.68;
                isValid = false;
            }

            if (settings.WindowWidth < 800 || settings.WindowHeight < 600)
            {
                Log("❌ Window size too small (minimum 800x600)");
                settings.WindowWidth = 1254;
                settings.WindowHeight = 657;
                isValid = false;
            }

            if (settings.StartupDelay != 35)
            {
                Log($"⚠️  Startup delay changed from {settings.StartupDelay} to 35 seconds");
                settings.StartupDelay = 35;
            }

            if (isValid)
            {
                SaveSettings();
            }

            return isValid;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.F1:
                    if (running)
                    {
                        Log("HOTKEY: Stopping monitor...");
                        monitoringCts?.Cancel();
                        StopMonitoringLoop();
                    }
                    else
                    {
                        Log("HOTKEY: Starting monitor...");
                        monitoringCts = new CancellationTokenSource();
                        _ = Task.Run(async () => await StartMonitoringLoop(monitoringCts.Token));
                    }
                    break;

                case Keys.F2:
                    Log("HOTKEY: Setting Discord webhook URL...");
                    SetDiscordWebhook();
                    break;

                case Keys.F3:
                    Log("HOTKEY: Testing navigation...");
                    _ = Task.Run(async () => await TestNavigation());
                    break;

                case Keys.F4:
                    TogglePause();
                    break;

                case Keys.Escape:
                    if (MessageBox.Show("Exit Brawlhalla Auto Queue?", "Exit",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        Application.Exit();
                    }
                    break;
            }
        }

        private void TogglePause()
        {
            isPaused = !isPaused;
            UpdatePauseLabel();

            if (isPaused)
            {
                Log("⏸️  Monitoring PAUSED");
                UpdateStatus("PAUSED");
            }
            else
            {
                Log("▶️  Monitoring RESUMED");
                UpdateStatus("MONITORING");
            }
        }

        private void UpdatePauseLabel()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdatePauseLabel()));
                return;
            }

            var pauseLabel = Controls.Find("pauseLabel", true).FirstOrDefault() as Label;
            if (pauseLabel != null)
            {
                pauseLabel.Text = $"PAUSED: {(isPaused ? "YES" : "NO")}";
                pauseLabel.ForeColor = isPaused ? Color.Red : Color.Green;
            }
        }

        private void SetDiscordWebhook()
        {
            try
            {
                using (var dialog = new WebhookInputDialog(settings.DiscordWebhookUrl))
                {
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        string newUrl = dialog.WebhookUrl;

                        if (!string.IsNullOrEmpty(newUrl) && !IsValidWebhookUrl(newUrl))
                        {
                            if (MessageBox.Show("URL doesn't look like a Discord webhook.\n" +
                                               "Format should be: https://discord.com/api/webhooks/...\n\n" +
                                               "Save anyway?", "Warning",
                                               MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                            {
                                return;
                            }
                        }

                        settings.DiscordWebhookUrl = newUrl;
                        SaveSettings();

                        if (string.IsNullOrEmpty(newUrl))
                        {
                            Log("   ✓ Discord notifications DISABLED");
                        }
                        else
                        {
                            Log("   ✓ Discord webhook URL saved");
                        }

                        UpdateDiscordStatus();
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"   ✗ Webhook dialog error: {ex.Message}");
            }
        }

        private bool IsValidWebhookUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            return Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult)
                   && (uriResult.Scheme == Uri.UriSchemeHttps)
                   && uriResult.Host.Contains("discord.com")
                   && uriResult.AbsolutePath.Contains("/api/webhooks/");
        }

        private void UpdateDiscordStatus()
        {
            Log($"   • Discord: {(string.IsNullOrEmpty(settings.DiscordWebhookUrl) ? "DISABLED (press F2)" : "ENABLED")}");
        }

        private void PressKey(byte keyCode, int delayBefore = 100, int delayAfter = 200)
        {
            try
            {
                Thread.Sleep(delayBefore);
                keybd_event(keyCode, 0, KEYEVENTF_KEYDOWN, 0);
                Thread.Sleep(50);
                keybd_event(keyCode, 0, KEYEVENTF_KEYUP, 0);
                Thread.Sleep(delayAfter);
            }
            catch { }
        }

        private async Task TestNavigation()
        {
            Log("🎮 Testing navigation sequence...");
            Log("   Make sure Brawlhalla window is focused!");

            for (int i = 3; i > 0; i--)
            {
                Log($"   Starting in {i}...");
                await Task.Delay(1000);
            }

            await NavigateToQueue();
        }

        private async Task<bool> IsReconnectPopupVisible()
        {
            try
            {
                IntPtr hwnd = FindWindow(null, "Brawlhalla");
                if (hwnd == IntPtr.Zero)
                {
                    return false;
                }

                using (Bitmap screenshot = CaptureWindow(hwnd))
                {
                    if (screenshot == null)
                        return false;

                    using (Mat frame = BitmapConverter.ToMat(screenshot))
                    using (Mat gray = new Mat())
                    {
                        Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

                        if (reconnectTpl != null && !reconnectTpl.Empty() && File.Exists(reconnectTemplatePath))
                        {
                            using (Mat template = Cv2.ImRead(reconnectTemplatePath, ImreadModes.Grayscale))
                            {
                                if (!template.Empty())
                                {
                                    double bestScore = 0;
                                    var methods = new TemplateMatchModes[]
                                    {
                                        TemplateMatchModes.CCoeffNormed,
                                        TemplateMatchModes.CCorrNormed,
                                        TemplateMatchModes.SqDiffNormed
                                    };

                                    foreach (var method in methods)
                                    {
                                        using (Mat result = new Mat())
                                        {
                                            Cv2.MatchTemplate(gray, template, result, method);
                                            Cv2.MinMaxLoc(result, out double minVal, out double maxVal);

                                            double score = method == TemplateMatchModes.SqDiffNormed ? 1 - minVal : maxVal;
                                            if (score > bestScore) bestScore = score;
                                        }
                                    }

                                    if (bestScore > 0.75)
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private async Task NavigateToQueue()
        {
            Log("   ▷ STARTING AUTO-NAVIGATION");

            try
            {
                await Task.Delay(1000);

                bool hasReconnect = await IsReconnectPopupVisible();

                if (hasReconnect)
                {
                    Log("   • Step 1: Pressing ESC (closing reconnect popup)");
                    PressKey(VK_ESCAPE, 1000, 500);
                    await Task.Delay(2000);

                    bool stillHasReconnect = await IsReconnectPopupVisible();
                    if (stillHasReconnect)
                    {
                        Log("   • WARNING: Popup still detected, pressing ESC one more time");
                        PressKey(VK_ESCAPE, 1000, 500);
                        await Task.Delay(2000);
                    }
                    else
                    {
                        Log("   • Popup cleared successfully");
                    }
                }
                else
                {
                    Log("   • Step 1: No reconnect popup found, skipping ESC");
                    await Task.Delay(1000);
                }

                Log("   • Step 2: Pressing S (Ranked)");
                PressKey(VK_S, 500, 1000);

                Log("   • Step 3: Pressing C (Confirm Ranked)");
                PressKey(VK_C, 500, 1500);

                Log("   • Step 4: Pressing C (Confirm Gamemode)");
                PressKey(VK_C, 500, 1000);

                Log("   • Step 5: Selecting legend (C x3)");
                for (int i = 1; i <= 3; i++)
                {
                    Log($"     • Legend {i}/3");
                    PressKey(VK_C, 300, 400);
                }

                Log("   ✓ Navigation complete!");

                if (!string.IsNullOrEmpty(settings.DiscordWebhookUrl))
                {
                    _ = Task.Run(async () => await SendDiscordNotification(
                        "Auto-navigation complete! Ready for queue."
                    ));
                }
            }
            catch (Exception ex)
            {
                Log($"   ✗ Navigation error: {ex.Message}");
            }
        }

        private string EscapeJsonString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return input.Replace("\\", "\\\\")
                        .Replace("\"", "\\\"")
                        .Replace("\n", "\\n")
                        .Replace("\r", "\\r")
                        .Replace("\t", "\\t");
        }

        private async Task SendDiscordNotification(string message)
        {
            if (string.IsNullOrEmpty(settings.DiscordWebhookUrl))
            {
                return;
            }

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string json = $"{{\"content\": \"{EscapeJsonString(message)}\"}}";
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(settings.DiscordWebhookUrl, content);
                }
            }
            catch
            {
                // Silently fail
            }
        }

        private async Task SendMatchFoundNotification(double matchScore)
        {
            queueCount++;
            UpdateQueueLabel();

            string message = $"Queue #{queueCount} COMPLETED! " +
                            $"Time: {DateTime.Now:HH:mm:ss} " +
                            $"Score: {matchScore:F3} " +
                            $"Threshold: {MATCH_THRESHOLD * 100:F0}%";

            await SendDiscordNotification(message);
        }

        private void LoadTemplates()
        {
            try
            {
                ReloadTemplates();

                if (searchingTpl == null || searchingTpl.Empty() || matchTpl == null || matchTpl.Empty())
                {
                    Log("⚠️  Templates not loaded - will use fallback detection");
                }
            }
            catch (Exception ex)
            {
                Log($"⚠️  Template error: {ex.Message}");
            }
        }

        private void ReloadTemplates()
        {
            try
            {
                searchingTpl?.Dispose();
                matchTpl?.Dispose();
                reconnectTpl?.Dispose();

                searchingTpl = null;
                matchTpl = null;
                reconnectTpl = null;

                if (File.Exists(searchingTemplatePath))
                {
                    searchingTpl = Cv2.ImRead(searchingTemplatePath, ImreadModes.Grayscale);
                }

                if (File.Exists(matchTemplatePath))
                {
                    matchTpl = Cv2.ImRead(matchTemplatePath, ImreadModes.Grayscale);
                }

                if (File.Exists(reconnectTemplatePath))
                {
                    reconnectTpl = Cv2.ImRead(reconnectTemplatePath, ImreadModes.Grayscale);
                }
            }
            catch (Exception ex)
            {
                Log($"⚠️  Reload templates error: {ex.Message}");
            }
        }

        private async Task StartMonitoringLoop(CancellationToken cancellationToken)
        {
            if (running) return;

            running = true;
            matchFound = false;
            frameCount = 0;
            UpdateStatus("MONITORING");

            Log("======================================================");
            Log(" STARTING MONITORING");
            Log("======================================================");
            Log($"   • Threshold: {MATCH_THRESHOLD * 100:F0}%");
            Log($"   • Window Size: {targetWindowSize.Width}x{targetWindowSize.Height}");
            Log($"   • Check Interval: {settings.CheckInterval}ms");
            Log($"   • Cooldown: {settings.CooldownSeconds}s");
            Log("======================================================");

            if (!string.IsNullOrEmpty(settings.DiscordWebhookUrl))
            {
                _ = Task.Run(async () => await SendDiscordNotification(
                    $"Brawlhalla Auto Queue STARTED | Threshold: {MATCH_THRESHOLD * 100:F0}% | Time: {DateTime.Now:HH:mm:ss}"
                ));
            }

            try
            {
                IntPtr hwnd = FindWindow(null, "Brawlhalla");
                if (hwnd != IntPtr.Zero)
                {
                    FastResizeWindow(hwnd);
                }

                LoadTemplates();

                Log(" SCANNING FOR MATCHES...");

                while (running && !this.IsDisposed && !cancellationToken.IsCancellationRequested)
                {
                    if (!isPaused && !isRunningSequence)
                    {
                        await ProcessFrame(cancellationToken);
                    }
                    await Task.Delay(settings.CheckInterval, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                Log(" Monitoring cancelled.");
            }
            catch (Exception ex)
            {
                Log($"❌ ERROR: {ex.Message}");
            }
            finally
            {
                StopMonitoringLoop();
            }
        }

        private void StopMonitoringLoop()
        {
            if (!running) return;

            running = false;
            matchFound = false;

            searchingTpl?.Dispose();
            matchTpl?.Dispose();
            reconnectTpl?.Dispose();
            searchingTpl = null;
            matchTpl = null;
            reconnectTpl = null;

            UpdateStatus("STOPPED");
            Log("======================================================");
            Log(" MONITORING STOPPED");
            Log($"   • Total Queues: {queueCount}");
            Log("======================================================");

            if (!string.IsNullOrEmpty(settings.DiscordWebhookUrl))
            {
                _ = Task.Run(async () => await SendDiscordNotification(
                    $"Brawlhalla Auto Queue STOPPED | Total Queues: {queueCount} | Time: {DateTime.Now:HH:mm:ss}"
                ));
            }
        }

        private async Task ProcessFrame(CancellationToken cancellationToken)
        {
            if (isRunningSequence || !running || cancellationToken.IsCancellationRequested) return;

            try
            {
                frameCount++;

                IntPtr hwnd = FindWindow(null, "Brawlhalla");
                if (hwnd == IntPtr.Zero)
                {
                    if (frameCount % 10 == 0 && !matchFound)
                        Log("   ⚠️  Brawlhalla window not found");
                    return;
                }

                if (IsIconic(hwnd))
                {
                    if (!matchFound)
                    {
                        SetForegroundWindow(hwnd);
                        await Task.Delay(100, cancellationToken);
                    }
                    else
                    {
                        return;
                    }
                }

                using (Bitmap screenshot = CaptureWindow(hwnd))
                {
                    if (screenshot == null)
                        return;

                    using (Mat frame = BitmapConverter.ToMat(screenshot))
                    using (Mat gray = new Mat())
                    {
                        Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

                        if (matchTpl == null || matchTpl.Empty() || searchingTpl == null || searchingTpl.Empty())
                        {
                            if (frameCount % 30 == 0)
                                Log("   ⚠️ Templates not loaded - attempting reload");
                            ReloadTemplates();

                            if (matchTpl == null || matchTpl.Empty())
                            {
                                await Task.Delay(1000, cancellationToken);
                                return;
                            }
                        }

                        Rect[] testROIs = new[]
                        {
                            new Rect(50, 40, 400, 100),
                            new Rect(gray.Width/2 - 200, 40, 400, 100),
                            new Rect(0, 30, gray.Width, 120)
                        };

                        bool foundMatch = false;

                        foreach (var roi in testROIs)
                        {
                            if (cancellationToken.IsCancellationRequested) return;

                            var clampedRoi = ClampRect(roi, gray.Width, gray.Height);
                            if (clampedRoi.Width <= 10 || clampedRoi.Height <= 10)
                                continue;

                            using (Mat region = new Mat(gray, clampedRoi))
                            {
                                if (region.Width < matchTpl.Width || region.Height < matchTpl.Height)
                                    continue;

                                double matchScore = MatchTemplate(region, matchTpl);

                                if (matchScore >= MATCH_THRESHOLD && !matchFound)
                                {
                                    if ((DateTime.Now - lastMatchTime).TotalSeconds < settings.CooldownSeconds)
                                    {
                                        Log($"   ⏳ Match detected (cooldown: {(int)(DateTime.Now - lastMatchTime).TotalSeconds}/{settings.CooldownSeconds}s)");
                                        continue;
                                    }

                                    Log("======================================================");
                                    Log($"🎯🎯🎯 MATCH FOUND!");
                                    Log($"   • Score: {matchScore:F3}");
                                    Log($"   • Threshold: {MATCH_THRESHOLD * 100:F0}%");
                                    Log($"   • Queue #{queueCount + 1}");
                                    Log("======================================================");

                                    System.Media.SystemSounds.Exclamation.Play();

                                    matchFound = true;
                                    lastMatchTime = DateTime.Now;

                                    _ = Task.Run(async () => await SendMatchFoundNotification(matchScore));

                                    running = false;
                                    await FastMatchSequence();

                                    foundMatch = true;
                                    break;
                                }

                                if (matchScore < MATCH_THRESHOLD && frameCount % 10 == 0)
                                {
                                    double searchScore = MatchTemplate(region, searchingTpl);
                                    if (searchScore >= MATCH_THRESHOLD)
                                    {
                                        Log($"   ⌛ Searching... Score: {searchScore:F3}");
                                    }
                                }
                            }

                            if (foundMatch) break;
                        }

                        if (!foundMatch && frameCount % 30 == 0)
                        {
                            Log($"   🔍 Scanning... Frame: {frameCount}");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (frameCount % 50 == 0)
                    Log($"   ⚠️  Error: {ex.Message}");
            }
        }

        private double MatchTemplate(Mat src, Mat template)
        {
            if (src == null || template == null || src.Empty() || template.Empty())
                return 0;

            if (src.Width < template.Width || src.Height < template.Height)
                return 0;

            try
            {
                using (Mat result = new Mat())
                {
                    Cv2.MatchTemplate(src, template, result, TemplateMatchModes.CCoeffNormed);
                    Cv2.MinMaxLoc(result, out _, out double maxVal);
                    return maxVal;
                }
            }
            catch
            {
                return 0;
            }
        }

        private Rect ClampRect(Rect r, int maxWidth, int maxHeight)
        {
            int x = Math.Max(0, r.X);
            int y = Math.Max(0, r.Y);
            int w = Math.Min(r.Width, maxWidth - x);
            int h = Math.Min(r.Height, maxHeight - y);
            return new Rect(x, y, w, h);
        }

        private async Task FastMatchSequence()
        {
            lock (sequenceLock)
            {
                if (isRunningSequence) return;
                isRunningSequence = true;
            }

            try
            {
                UpdateStatus("PROCESSING MATCH");

                Log("======================================================");
                Log("🔄 EXECUTING MATCH SEQUENCE");
                Log("======================================================");
                Log("   • Step 1: Closing Brawlhalla...");

                CloseBrawlhalla();
                await Task.Delay(2000);

                Log("   • Step 2: Launching Brawlhalla...");
                LaunchBrawlhalla();

                Log($"   • Step 3: Waiting 35 seconds for game load...");

                for (int i = 35; i > 0; i--)
                {
                    if (i % 5 == 0 || i <= 3)
                    {
                        Log($"      {i} seconds remaining");
                    }
                    await Task.Delay(1000);
                }

                Log($"   • Step 3 complete: 35 seconds elapsed");

                Log("   • Step 4: Finding window...");
                IntPtr newHwnd = IntPtr.Zero;
                for (int i = 0; i < 20; i++)
                {
                    newHwnd = FindWindow(null, "Brawlhalla");
                    if (newHwnd != IntPtr.Zero)
                    {
                        SetForegroundWindow(newHwnd);
                        await Task.Delay(2000);

                        FastResizeWindow(newHwnd);
                        Log($"   • Window resized: {targetWindowSize.Width}x{targetWindowSize.Height}");

                        await NavigateToQueue();
                        break;
                    }
                    if (i % 5 == 0)
                        Log($"      Attempt {i + 1}/20");
                    await Task.Delay(1000);
                }

                Log("======================================================");
                Log("✓ SEQUENCE COMPLETE - READY FOR NEXT QUEUE");
                Log("======================================================");

                if (!string.IsNullOrEmpty(settings.DiscordWebhookUrl))
                {
                    _ = Task.Run(async () => await SendDiscordNotification(
                        $"Queue #{queueCount} restarted! Ready for next match."
                    ));
                }
            }
            catch (Exception ex)
            {
                Log($"   ✗ Sequence error: {ex.Message}");
            }
            finally
            {
                isRunningSequence = false;
                matchFound = false;

                Log("   • Auto-restarting monitoring...");
                await Task.Delay(1000);

                if (!this.IsDisposed)
                {
                    monitoringCts = new CancellationTokenSource();
                    _ = Task.Run(async () => await StartMonitoringLoop(monitoringCts.Token));
                }
            }
        }

        private void FastResizeWindow(IntPtr hwnd)
        {
            try
            {
                GetWindowRect(hwnd, out RECT rect);
                int currentWidth = rect.Right - rect.Left;
                int currentHeight = rect.Bottom - rect.Top;

                if (Math.Abs(currentWidth - targetWindowSize.Width) > 20 ||
                    Math.Abs(currentHeight - targetWindowSize.Height) > 20)
                {
                    Screen screen = Screen.PrimaryScreen;
                    int x = (screen.WorkingArea.Width - targetWindowSize.Width) / 2;
                    int y = (screen.WorkingArea.Height - targetWindowSize.Height) / 2;

                    MoveWindow(hwnd, x, y, targetWindowSize.Width, targetWindowSize.Height, true);
                    Log($"   • Window resized: {currentWidth}x{currentHeight} → {targetWindowSize.Width}x{targetWindowSize.Height}");
                }
            }
            catch (Exception ex)
            {
                Log($"   ✗ Resize error: {ex.Message}");
            }
        }

        private void CloseBrawlhalla()
        {
            try
            {
                foreach (var process in Process.GetProcessesByName("Brawlhalla"))
                {
                    process.CloseMainWindow();
                    if (!process.WaitForExit(1500))
                        process.Kill();
                    process.Dispose();
                }
                Log("   • Brawlhalla closed");
            }
            catch (Exception ex)
            {
                Log($"   ✗ Close error: {ex.Message}");
            }
        }

        private void LaunchBrawlhalla()
        {
            try
            {
                if (Process.GetProcessesByName("Brawlhalla").Length > 0)
                {
                    Log("   ⚠️  Brawlhalla already running");
                    return;
                }

                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "steam://rungameid/291550",
                        UseShellExecute = true
                    });
                }
                catch
                {
                    var steamPath = @"C:\Program Files (x86)\Steam\steam.exe";
                    if (File.Exists(steamPath))
                    {
                        Process.Start(steamPath, "-applaunch 291550");
                    }
                    else
                    {
                        Process.Start("steam://rungameid/291550");
                    }
                }

                Log("   • Launching Brawlhalla...");
            }
            catch (Exception ex)
            {
                Log($"   ✗ Launch error: {ex.Message}");
            }
        }

        private Bitmap CaptureWindow(IntPtr hwnd)
        {
            try
            {
                GetWindowRect(hwnd, out RECT rect);
                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;

                if (width <= 0 || height <= 0 || width > 5000 || height > 5000)
                    return null;

                Bitmap bmp = new Bitmap(width, height);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);
                }
                return bmp;
            }
            catch
            {
                return null;
            }
        }

        private void UpdateStatus(string status)
        {
            if (this.IsDisposed || this.Disposing)
                return;

            if (this.InvokeRequired)
            {
                if (!this.IsDisposed && !this.Disposing)
                {
                    this.Invoke(new Action(() => UpdateStatus(status)));
                }
                return;
            }

            var statusLabel = Controls.Find("statusLabel", true).FirstOrDefault() as Label;
            if (statusLabel != null)
            {
                statusLabel.Text = $"STATUS: {status}";
                statusLabel.ForeColor = status == "MONITORING" ? Color.Lime :
                                       status == "PAUSED" ? Color.Orange :
                                       status == "PROCESSING MATCH" ? Color.Magenta :
                                       status == "STOPPED" ? Color.Red : Color.Cyan;
            }
        }

        private void UpdateQueueLabel()
        {
            if (this.IsDisposed || this.Disposing)
                return;

            if (this.InvokeRequired)
            {
                if (!this.IsDisposed && !this.Disposing)
                {
                    this.Invoke(new Action(() => UpdateQueueLabel()));
                }
                return;
            }

            var queueLabel = Controls.Find("queueLabel", true).FirstOrDefault() as Label;
            if (queueLabel != null)
            {
                queueLabel.Text = $"QUEUES: {queueCount}";
            }
        }

        private void Log(string message)
        {
            if (this.IsDisposed || txtLog == null || txtLog.IsDisposed)
                return;

            if (txtLog.InvokeRequired)
            {
                if (!this.IsDisposed && !this.Disposing && txtLog != null && !txtLog.IsDisposed)
                {
                    try
                    {
                        txtLog.Invoke(new Action(() => Log(message)));
                    }
                    catch
                    {
                        // Ignore invocation errors during shutdown
                    }
                }
                return;
            }

            try
            {
                txtLog.AppendText($"{message}\r\n");
                txtLog.SelectionStart = txtLog.Text.Length;
                txtLog.ScrollToCaret();

                if (txtLog.Lines.Length > 200)
                {
                    var lines = txtLog.Lines;
                    var newLines = new string[100];
                    Array.Copy(lines, lines.Length - 100, newLines, 0, 100);
                    txtLog.Lines = newLines;
                    txtLog.SelectionStart = txtLog.Text.Length;
                    txtLog.ScrollToCaret();
                }
            }
            catch
            {
                // Ignore logging errors during shutdown
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            monitoringCts?.Cancel();
            monitoringCts?.Dispose();

            SaveSettings();

            searchingTpl?.Dispose();
            matchTpl?.Dispose();
            reconnectTpl?.Dispose();

            base.OnFormClosing(e);
        }
    }
}