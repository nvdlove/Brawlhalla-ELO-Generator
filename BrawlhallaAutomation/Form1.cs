using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using DrawingPoint = System.Drawing.Point;
using DrawingSize = System.Drawing.Size;

namespace BrawlhallaAutomation
{
    // ==================== WEBHOOK INPUT DIALOG ====================
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
            // Window setup
            this.Text = "Set Discord Webhook URL";
            this.Size = new DrawingSize(600, 220);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.Black;
            this.TopMost = true;

            // Title bar
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
                Location = new DrawingPoint(10, 5),
                AutoSize = true
            };

            // Close button
            var btnClose = CreateTransparentButton("✕", new DrawingPoint(570, 0),
                (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); });

            titleBar.Controls.Add(titleLabel);
            titleBar.Controls.Add(btnClose);

            // Main content
            var lblInstruction = new Label
            {
                Text = "Enter Discord Webhook URL (leave empty to disable):",
                ForeColor = Color.Cyan,
                Font = new Font("Consolas", 9),
                Location = new DrawingPoint(20, 40),
                AutoSize = true
            };

            txtWebhook = new TextBox
            {
                Location = new DrawingPoint(20, 70),
                Size = new DrawingSize(540, 25),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 9)
            };

            if (!string.IsNullOrEmpty(currentUrl))
                txtWebhook.Text = currentUrl;

            // Help text
            var lblHelp = new Label
            {
                Text = "Get webhook from Discord: Server Settings → Integrations → Webhooks",
                ForeColor = Color.Gray,
                Font = new Font("Consolas", 7),
                Location = new DrawingPoint(20, 100),
                AutoSize = true
            };

            // Buttons
            btnTest = new Button
            {
                Text = "TEST",
                Location = new DrawingPoint(300, 140),
                Size = new DrawingSize(80, 30),
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
                Location = new DrawingPoint(400, 140),
                Size = new DrawingSize(80, 30),
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
                Location = new DrawingPoint(490, 140),
                Size = new DrawingSize(80, 30),
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

            // Add controls
            this.Controls.Add(titleBar);
            this.Controls.Add(lblInstruction);
            this.Controls.Add(txtWebhook);
            this.Controls.Add(lblHelp);
            this.Controls.Add(btnTest);
            this.Controls.Add(btnSave);
            this.Controls.Add(btnCancel);

            // Make draggable
            titleBar.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(this.Handle, WM_NCLBUTTONDOWN, new IntPtr(HT_CAPTION), IntPtr.Zero);
                }
            };
        }

        private Button CreateTransparentButton(string text, DrawingPoint location, EventHandler clickHandler)
        {
            var btn = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(180, 180, 180),
                BackColor = Color.Transparent,
                Size = new DrawingSize(30, 30),
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
    }

    // ==================== MAIN APPLICATION ====================

    // Configuration class for settings
    public class AppSettings
    {
        public double MatchThreshold { get; set; } = 0.68;
        public int WindowWidth { get; set; } = 1254;
        public int WindowHeight { get; set; } = 657;
        public int CheckInterval { get; set; } = 600;
        public int CooldownSeconds { get; set; } = 25;
        public int StartupDelay { get; set; } = 35; // 35-second delay after game launch
        public string DiscordWebhookUrl { get; set; } = "";
    }

    public partial class Form1 : Form
    {
        // ================= UI Components =================
        private TextBox txtLog;
        private Panel titleBar;
        private Button btnClose;
        private Button btnMinimize;

        // ================= State =================
        private bool running = false;
        private int frameCount = 0;
        private bool matchFound = false;
        private DateTime lastMatchTime = DateTime.MinValue;
        private int queueCount = 0;
        private bool autoStart = false;
        private bool isRunningSequence = false;
        private readonly object sequenceLock = new object();
        private bool isPaused = false;

        // ================= Configuration =================
        private double MATCH_THRESHOLD = 0.68;
        private readonly string searchingTemplatePath = @"Templates\searching.png";
        private readonly string matchTemplatePath = @"Templates\match.png";
        private readonly string reconnectTemplatePath = @"Templates\reconnect_popup.png";
        private DrawingSize targetWindowSize = new DrawingSize(1254, 657);
        private AppSettings settings = new AppSettings();

        // ================= Template Cache =================
        private Mat searchingTpl = null;
        private Mat matchTpl = null;
        private Mat reconnectTpl = null;

        // ================= Win32 API =================
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

        // Keyboard constants
        private const int KEYEVENTF_KEYDOWN = 0x0000;
        private const int KEYEVENTF_KEYUP = 0x0002;
        private const byte VK_S = 0x53;
        private const byte VK_C = 0x43;
        private const byte VK_ESCAPE = 0x1B;

        // Window dragging
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        // Scrolling
        private const int WM_VSCROLL = 0x0115;
        private const int SB_LINEUP = 0;
        private const int SB_LINEDOWN = 1;

        public Form1()
        {
            InitializeUI();
            LoadSettings();

            if (ValidateSettings())
            {
                autoStart = true;
                _ = Task.Run(async () => await StartMonitoringLoop());
            }
            else
            {
                Log("❌ Invalid settings detected. Please check configuration.");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Additional initialization
        }

        private Button CreateTransparentButton(string text, DrawingPoint location, EventHandler clickHandler)
        {
            var btn = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(180, 180, 180),
                BackColor = Color.Transparent,
                Size = new DrawingSize(30, 30),
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

        // ================= UI Setup =================
        private void InitializeUI()
        {
            Text = "Brawlhalla ELO Generator v1.0";
            this.Size = new DrawingSize(800, 500);
            StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.Black;

            try
            {
                this.Icon = new Icon("icon.ico");
            }
            catch
            {
                this.Icon = SystemIcons.Application;
            }

            this.TopMost = true;

            // Title bar
            titleBar = new Panel
            {
                Height = 30,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(30, 30, 30)
            };

            var titleLabel = new Label
            {
                Text = "Brawlhalla ELO Generator v1.0",
                ForeColor = Color.White,
                Font = new Font("Consolas", 9),
                Location = new DrawingPoint(10, 5),
                AutoSize = true
            };

            btnClose = CreateTransparentButton("✕", new DrawingPoint(770, 0), (s, e) => Application.Exit());
            btnMinimize = CreateTransparentButton("─", new DrawingPoint(740, 0), (s, e) => this.WindowState = FormWindowState.Minimized);

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

            // Log textbox
            txtLog = new TextBox
            {
                Multiline = true,
                Location = new DrawingPoint(0, 30),
                Size = new DrawingSize(760, 445),
                BackColor = Color.Black,
                ForeColor = Color.Lime,
                Font = new Font("Consolas", 10),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                ScrollBars = ScrollBars.None,
                WordWrap = true
            };

            // Mouse wheel scrolling
            this.MouseWheel += (s, e) =>
            {
                DrawingPoint mousePos = txtLog.PointToClient(Cursor.Position);
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

            // Status bar
            var statusBar = new Panel
            {
                Height = 25,
                Dock = DockStyle.Bottom,
                BackColor = Color.FromArgb(30, 30, 30)
            };

            var statusLabel = new Label
            {
                Text = "STATUS: INITIALIZING...",
                ForeColor = Color.Cyan,
                Font = new Font("Consolas", 9),
                Location = new DrawingPoint(10, 3),
                AutoSize = true,
                Name = "statusLabel"
            };

            var queueLabel = new Label
            {
                Text = "QUEUES: 0",
                ForeColor = Color.Yellow,
                Font = new Font("Consolas", 9),
                Location = new DrawingPoint(200, 3),
                AutoSize = true,
                Name = "queueLabel"
            };

            var pauseLabel = new Label
            {
                Text = "PAUSED: NO",
                ForeColor = Color.Orange,
                Font = new Font("Consolas", 9),
                Location = new DrawingPoint(350, 3),
                AutoSize = true,
                Name = "pauseLabel"
            };

            var timeLabel = new Label
            {
                Text = DateTime.Now.ToString("HH:mm:ss"),
                ForeColor = Color.White,
                Font = new Font("Consolas", 9),
                Location = new DrawingPoint(700, 3),
                AutoSize = true,
                Name = "timeLabel"
            };

            statusBar.Controls.Add(statusLabel);
            statusBar.Controls.Add(queueLabel);
            statusBar.Controls.Add(pauseLabel);
            statusBar.Controls.Add(timeLabel);

            // Time update timer
            var timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000;
            timer.Tick += (s, e) => timeLabel.Text = DateTime.Now.ToString("HH:mm:ss");
            timer.Start();

            // Hotkey labels
            var hotkeyLabel = new Label
            {
                Text = "[F1] Stop/Restart [F2] Webhook URL [F3] Manual Nav [F4] Screenshot [F5] Pause",
                ForeColor = Color.Gray,
                Font = new Font("Consolas", 8),
                Location = new DrawingPoint(330, 460),
                AutoSize = true
            };

            // Add controls
            this.Controls.Add(txtLog);
            this.Controls.Add(titleBar);
            this.Controls.Add(statusBar);
            this.Controls.Add(hotkeyLabel);

            // Z-order
            titleBar.BringToFront();
            statusBar.BringToFront();
            hotkeyLabel.BringToFront();

            // Hotkeys
            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;

            // Initial log
            Log("  Brawlhalla ELO Generator v1.0 nvdlove & antireplay");
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

        // ================= Settings Management =================
        private void LoadSettings()
        {
            try
            {
                if (File.Exists("settings.json"))
                {
                    var json = File.ReadAllText("settings.json");

                    double mt = ParseDoubleFromJson(json, "MatchThreshold", settings.MatchThreshold);
                    int ww = ParseIntFromJson(json, "WindowWidth", settings.WindowWidth);
                    int wh = ParseIntFromJson(json, "WindowHeight", settings.WindowHeight);
                    int ci = ParseIntFromJson(json, "CheckInterval", settings.CheckInterval);
                    int cs = ParseIntFromJson(json, "CooldownSeconds", settings.CooldownSeconds);
                    int sd = ParseIntFromJson(json, "StartupDelay", settings.StartupDelay);
                    string webhook = ParseStringFromJson(json, "DiscordWebhookUrl", settings.DiscordWebhookUrl);

                    settings.MatchThreshold = mt;
                    settings.WindowWidth = ww;
                    settings.WindowHeight = wh;
                    settings.CheckInterval = ci;
                    settings.CooldownSeconds = cs;
                    settings.StartupDelay = sd;
                    settings.DiscordWebhookUrl = webhook;

                    MATCH_THRESHOLD = settings.MatchThreshold;
                    targetWindowSize = new DrawingSize(settings.WindowWidth, settings.WindowHeight);

                    Log($" Settings loaded: {MATCH_THRESHOLD * 100}% threshold");
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

                var sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine($"  \"MatchThreshold\": {settings.MatchThreshold.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
                sb.AppendLine($"  \"WindowWidth\": {settings.WindowWidth},");
                sb.AppendLine($"  \"WindowHeight\": {settings.WindowHeight},");
                sb.AppendLine($"  \"CheckInterval\": {settings.CheckInterval},");
                sb.AppendLine($"  \"CooldownSeconds\": {settings.CooldownSeconds},");
                sb.AppendLine($"  \"StartupDelay\": 35,"); // FORCE 35 SECONDS
                sb.AppendLine($"  \"DiscordWebhookUrl\": \"{EscapeJsonString(settings.DiscordWebhookUrl)}\"");
                sb.AppendLine("}");

                File.WriteAllText("settings.json", sb.ToString(), Encoding.UTF8);
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

            // FORCE 35 SECONDS NO MATTER WHAT
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

        private double ParseDoubleFromJson(string json, string key, double defaultValue)
        {
            try
            {
                var idx = json.IndexOf($"\"{key}\"", StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    var colon = json.IndexOf(':', idx);
                    if (colon >= 0)
                    {
                        var end = json.IndexOfAny(new[] { ',', '\n', '\r', '}' }, colon + 1);
                        if (end < 0) end = json.Length;
                        var token = json.Substring(colon + 1, end - colon - 1).Trim().Trim('"');
                        if (double.TryParse(token, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double v))
                            return v;
                    }
                }
            }
            catch { }
            return defaultValue;
        }

        private int ParseIntFromJson(string json, string key, int defaultValue)
        {
            try
            {
                var d = ParseDoubleFromJson(json, key, defaultValue);
                return (int)d;
            }
            catch { }
            return defaultValue;
        }

        private string ParseStringFromJson(string json, string key, string defaultValue)
        {
            try
            {
                var idx = json.IndexOf($"\"{key}\"", StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    var colon = json.IndexOf(':', idx);
                    if (colon >= 0)
                    {
                        var start = json.IndexOf('"', colon + 1);
                        if (start >= 0)
                        {
                            int end = start + 1;
                            while (end < json.Length)
                            {
                                if (json[end] == '"' && (end == 0 || json[end - 1] != '\\'))
                                    break;
                                end++;
                            }

                            if (end > start && end < json.Length)
                            {
                                var value = json.Substring(start + 1, end - start - 1);
                                return value.Replace("\\\"", "\"")
                                            .Replace("\\\\", "\\")
                                            .Replace("\\n", "\n")
                                            .Replace("\\r", "\r")
                                            .Replace("\\t", "\t");
                            }
                        }
                    }
                }
            }
            catch { }
            return defaultValue;
        }

        // ================= Hotkey Handler =================
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.F1:
                    if (running)
                    {
                        Log("HOTKEY: Stopping monitor...");
                        StopMonitoringLoop();
                    }
                    else
                    {
                        Log("HOTKEY: Starting monitor...");
                        _ = Task.Run(async () => await StartMonitoringLoop());
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
                    Log("HOTKEY: Capturing debug screenshot...");
                    CaptureDebugScreenshot();
                    break;

                case Keys.F5:
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

        // ================= Pause Functionality =================
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

        // ================= Webhook Configuration =================
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

        // ================= Keyboard Navigation =================
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

        // ================= FIXED: Reconnect Popup Detection =================
        private async Task<bool> IsReconnectPopupVisible()
        {
            try
            {
                IntPtr hwnd = FindWindow(null, "Brawlhalla");
                if (hwnd == IntPtr.Zero)
                {
                    Log("   ⚠️  Brawlhalla window not found for reconnect check");
                    return false; // Return false if window not found
                }

                using (Bitmap screenshot = CaptureWindow(hwnd))
                {
                    if (screenshot == null)
                        return false;

                    using (Mat frame = BitmapConverter.ToMat(screenshot))
                    using (Mat gray = new Mat())
                    {
                        Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

                        if (File.Exists(reconnectTemplatePath))
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

                                    Log($"   • Reconnect template match score: {bestScore:F3}");

                                    // FIXED: Use higher threshold (0.75) to avoid false positives
                                    if (bestScore > 0.75) // CHANGED FROM 0.6 TO 0.75
                                    {
                                        string debugFile = $"debug_reconnect_{DateTime.Now:HHmmss}_{bestScore:F3}.png";
                                        screenshot.Save(debugFile, ImageFormat.Png);

                                        if (bestScore > 0.85)
                                            Log($"   ✓ Reconnect popup detected (STRONG match: {bestScore:F3})");
                                        else if (bestScore > 0.75)
                                            Log($"   ⚠️  Reconnect popup detected (MEDIUM match: {bestScore:F3})");

                                        return true;
                                    }
                                    else
                                    {
                                        Log($"   • No popup detected (score too low: {bestScore:F3} < 0.75)");
                                        return false;
                                    }
                                }
                                else
                                {
                                    Log($"   • Template file exists but is empty/corrupted");
                                    return false;
                                }
                            }
                        }
                        else
                        {
                            Log($"   • No reconnect template found at: {reconnectTemplatePath}");
                            Log($"   • Assuming no reconnect popup (safe to skip ESC)");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"   ⚠️  Reconnect check error: {ex.Message}");
                // FIXED: Return FALSE when there's an error, not TRUE
                // We don't want to press ESC if we can't check properly
                return false; // CHANGED FROM true TO false
            }
        }

        private double MatchTemplateMultipleMethods(Mat source, Mat template)
        {
            double bestScore = 0;

            var methods = new (TemplateMatchModes Method, bool InvertScore)[]
            {
                (TemplateMatchModes.CCoeffNormed, false),
                (TemplateMatchModes.CCorrNormed, false),
                (TemplateMatchModes.SqDiffNormed, true)
            };

            foreach (var (method, invert) in methods)
            {
                using (Mat result = new Mat())
                {
                    Cv2.MatchTemplate(source, template, result, method);
                    Cv2.MinMaxLoc(result, out double minVal, out double maxVal);

                    double score = invert ? 1 - minVal : maxVal;
                    if (score > bestScore) bestScore = score;
                }
            }

            return bestScore;
        }

        // ================= FIXED: Navigation Method =================
        private async Task NavigateToQueue()
        {
            Log("   ▷ STARTING AUTO-NAVIGATION");

            try
            {
                // FIXED: Better reconnect popup check with timeout
                Log("   • Checking for reconnect popup...");

                // Add a small delay before checking to ensure window is ready
                await Task.Delay(1000);

                bool hasReconnect = await IsReconnectPopupVisible();

                if (hasReconnect)
                {
                    Log("   • Step 1: Pressing ESC (closing reconnect popup)");
                    PressKey(VK_ESCAPE, 1000, 500);
                    await Task.Delay(2000);

                    // Check one more time but don't get stuck
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
                    // Small delay before continuing to match timing with ESC press scenario
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

        // ================= Discord Methods =================
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
                Log("   ⚠️  Discord not configured (press F2 to set webhook)");
                return;
            }

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string json = $"{{\"content\": \"{EscapeJsonString(message)}\"}}";
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(settings.DiscordWebhookUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        Log("   ✓  Discord notification sent");
                    }
                    else
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        Log($"   ✗ Discord failed: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"   ✗ Discord error: {ex.Message}");
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

        // ================= Template Management =================
        private void LoadTemplates()
        {
            try
            {
                ReloadTemplates();

                if (searchingTpl?.Empty() != false || matchTpl?.Empty() != false)
                {
                    throw new Exception("Failed to load templates");
                }

                Log($"   • Templates loaded");
                Log($"   • Searching: {searchingTpl.Width}x{searchingTpl.Height}");
                Log($"   • Match: {matchTpl.Width}x{matchTpl.Height}");
            }
            catch (Exception ex)
            {
                Log($"❌ Template error: {ex.Message}");
                throw;
            }
        }

        private void ReloadTemplates()
        {
            // Dispose existing templates
            searchingTpl?.Dispose();
            matchTpl?.Dispose();
            reconnectTpl?.Dispose();

            searchingTpl = null;
            matchTpl = null;
            reconnectTpl = null;

            // Load new templates
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

        // ================= Core Monitoring Loop =================
        private async Task StartMonitoringLoop()
        {
            if (running) return;

            if (!File.Exists(searchingTemplatePath) || !File.Exists(matchTemplatePath))
            {
                Log("❌ ERROR: Missing templates in Templates folder!");
                return;
            }

            running = true;
            matchFound = false;
            frameCount = 0;
            UpdateStatus("MONITORING");

            Log("======================================================");
            Log(" STARTING 68% FAST MONITORING");
            Log("======================================================");
            Log($"   • Threshold: {MATCH_THRESHOLD * 100:F0}%");
            Log($"   • Window Size: {targetWindowSize.Width}x{targetWindowSize.Height}");
            Log($"   • Check Interval: {settings.CheckInterval}ms");
            Log($"   • Cooldown: {settings.CooldownSeconds}s");
            Log($"   • Startup Delay: 35s"); // Hardcoded 35 seconds
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

                Log("======================================================");
                Log(" SCANNING FOR MATCHES...");

                while (running && !this.IsDisposed)
                {
                    if (!isPaused && !isRunningSequence) // ADDED: Check for running sequence
                    {
                        await ProcessFrame();
                    }
                    await Task.Delay(settings.CheckInterval);
                }
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

        // ================= Process Frame - FIXED =================
        private async Task ProcessFrame()
        {
            // FIX: STOP PROCESSING IF SEQUENCE IS RUNNING
            if (isRunningSequence || !running) return;

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
                        await Task.Delay(100);
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

                        OpenCvSharp.Rect[] testROIs = new[]
                        {
                            new OpenCvSharp.Rect(50, 40, 400, 100),
                            new OpenCvSharp.Rect(gray.Width/2 - 200, 40, 400, 100),
                            new OpenCvSharp.Rect(0, 30, gray.Width, 120)
                        };

                        bool foundMatch = false;

                        foreach (var roi in testROIs)
                        {
                            var clampedRoi = ClampRect(roi, gray.Width, gray.Height);
                            if (clampedRoi.Width <= 10 || clampedRoi.Height <= 10)
                                continue;

                            using (Mat region = new Mat(gray, clampedRoi))
                            {
                                if (region.Width < searchingTpl.Width || region.Height < searchingTpl.Height)
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
                                    SaveDebugImage(screenshot, "MATCH_FOUND", matchScore);

                                    matchFound = true;
                                    lastMatchTime = DateTime.Now;

                                    _ = Task.Run(async () => await SendMatchFoundNotification(matchScore));

                                    // STOP MONITORING AND START SEQUENCE
                                    running = false;
                                    await FastMatchSequence();

                                    foundMatch = true;
                                    break;
                                }

                                if (matchScore < MATCH_THRESHOLD)
                                {
                                    double searchScore = MatchTemplate(region, searchingTpl);

                                    if (searchScore >= MATCH_THRESHOLD && frameCount % 10 == 0)
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
            catch (Exception ex)
            {
                if (frameCount % 50 == 0)
                    Log($"   ⚠️  Error: {ex.Message}");
            }
        }

        // ================= FIXED: FAST MATCH SEQUENCE WITH EXACT 35-SECOND DELAY =================
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

                // FIXED: EXACT 35-SECOND DELAY - NO INTERFERENCE
                Log($"   • Step 3: Waiting 35 seconds for game load...");

                // Clean 35-second countdown
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

                // AUTO-RESTART MONITORING
                Log("   • Auto-restarting monitoring...");
                await Task.Delay(1000);
                _ = Task.Run(async () => await StartMonitoringLoop());
            }
        }

        // ================= Window Resize Methods =================
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
                else
                {
                    Log($"   • Window already correct: {currentWidth}x{currentHeight}");
                }
            }
            catch (Exception ex)
            {
                Log($"   ✗ Resize error: {ex.Message}");
            }
        }

        // ================= Helper Methods =================
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

        private double MatchTemplate(Mat src, Mat template)
        {
            if (src.Width < template.Width || src.Height < template.Height)
                return 0;

            using (Mat result = new Mat())
            {
                Cv2.MatchTemplate(src, template, result, TemplateMatchModes.CCoeffNormed);
                Cv2.MinMaxLoc(result, out _, out double maxVal);
                return maxVal;
            }
        }

        private OpenCvSharp.Rect ClampRect(OpenCvSharp.Rect r, int maxWidth, int maxHeight)
        {
            int x = Math.Max(0, r.X);
            int y = Math.Max(0, r.Y);
            int w = Math.Min(r.Width, maxWidth - x);
            int h = Math.Min(r.Height, maxHeight - y);
            return new OpenCvSharp.Rect(x, y, w, h);
        }

        // ================= Debug Functions =================
        private void CaptureDebugScreenshot()
        {
            try
            {
                IntPtr hwnd = FindWindow(null, "Brawlhalla");
                if (hwnd == IntPtr.Zero)
                {
                    Log("   ⚠️  Brawlhalla not found");
                    return;
                }

                using (Bitmap screenshot = CaptureWindow(hwnd))
                {
                    if (screenshot != null)
                    {
                        string filename = $"Debug_{DateTime.Now:HHmmss}.png";
                        screenshot.Save(filename, ImageFormat.Png);
                        Log($"   📸 Screenshot saved: {filename}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"   ✗ Screenshot error: {ex.Message}");
            }
        }

        private void SaveDebugImage(Bitmap image, string state, double score)
        {
            try
            {
                string filename = $"Match_{state}_{DateTime.Now:HHmmss}_{score:F2}.png";
                image.Save(filename, ImageFormat.Png);
            }
            catch { }
        }

        // ================= UI Update Methods =================
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

        // ================= Logging =================
        private void Log(string message)
        {
            if (this.IsDisposed || txtLog.IsDisposed || txtLog.Disposing)
                return;

            if (txtLog.InvokeRequired)
            {
                if (!this.IsDisposed && !this.Disposing && !txtLog.IsDisposed && !txtLog.Disposing)
                {
                    txtLog.Invoke(new Action(() => Log(message)));
                }
                return;
            }

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

        // ================= Cleanup =================
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveSettings();

            searchingTpl?.Dispose();
            matchTpl?.Dispose();
            reconnectTpl?.Dispose();

            base.OnFormClosing(e);
        }
    }
}
