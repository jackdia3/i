using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

public partial class AIVisualAntiAFKForm : Form
{
    [DllImport("user32.dll")]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    static extern IntPtr CreateCompatibleDC(IntPtr hDC);

    [DllImport("gdi32.dll")]
    static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    static extern IntPtr SelectObject(IntPtr hDC, IntPtr hGdiObj);

    [DllImport("gdi32.dll")]
    static extern bool BitBlt(IntPtr hDestDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);

    [DllImport("gdi32.dll")]
    static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    static extern bool DeleteDC(IntPtr hDC);

    [DllImport("user32.dll")]
    static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

    [DllImport("user32.dll")]
    static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

    [DllImport("user32.dll")]
    static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    static extern bool PrintWindow(IntPtr hWnd, IntPtr hDC, uint nFlags);

    struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    struct POINT
    {
        public int X;
        public int Y;
    }

    const uint WM_LBUTTONDOWN = 0x0201;
    const uint WM_LBUTTONUP = 0x0202;
    const uint WM_RBUTTONDOWN = 0x0204;
    const uint WM_RBUTTONUP = 0x0205;
    const int SRCCOPY = 0x00CC0020;
    const uint PW_CLIENTONLY = 0x00000001;

    private PictureBox pictureBoxGameScreen;
    private Button btnCapture;
    private Button btnAnalyze;
    private Button btnStartAFK;
    private Button btnStopAFK;
    private Label labelStatus;
    private Label labelAnalysisResult;
    private NumericUpDown numericInterval;
    private CheckBox checkBoxRightClick;
    private CheckBox checkBoxAutoAnalyze;
    private CheckBox checkBoxMouseReference;
    private ComboBox comboBoxStrategy;
    private ListBox listBoxSafeZones;
    private TrackBar trackBarClickChance;
    private Label labelClickChance;
    private Label labelMousePosition;
    private System.Windows.Forms.Timer timerAFK;
    private System.Windows.Forms.Timer timerMouseTracker;
    private IntPtr gameWindow;
    private List<Point> safeClickPositions = new List<Point>();
    private bool isAFKRunning = false;
    private Random random = new Random();
    private Point currentMousePosition = new Point(0, 0);

    // 新增：右下角按鈕檢測相關
    private Button btnDetectButtons;
    private CheckBox checkBoxAvoidButtons;
    private ListBox listBoxDetectedButtons;
    private Label labelButtonDetection;
    private List<Rectangle> detectedButtons = new List<Rectangle>();
    private GroupBox groupButtonDetection;

    // AI 分析策略
    public enum AnalysisStrategy
    {
        SafeZone,           // 尋找安全空白區域
        AvoidUI,            // 避開UI元素
        CenterFocus,        // 中心區域優先
        EdgeSafe,           // 邊緣安全區域
        ColorAnalysis,      // 顏色分析
        AvoidButtons        // 避開按鈕區域 (新增)
    }

    public AIVisualAntiAFKForm()
    {
        InitializeComponent();
        timerAFK = new System.Windows.Forms.Timer();
        timerAFK.Tick += TimerAFK_Tick;
        
        timerMouseTracker = new System.Windows.Forms.Timer();
        timerMouseTracker.Interval = 100; // 100ms更新一次滑鼠位置
        timerMouseTracker.Tick += TimerMouseTracker_Tick;
        timerMouseTracker.Start();
    }

    private void InitializeComponent()
    {
        this.Text = "🤖 AI智能暗黑不朽反掛機 - 增強版 v2.0";
        this.Size = new Size(1600, 1000);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(240, 245, 250); // 淡藍灰色背景

        // 遊戲畫面顯示區域 - 調整為更適合1920x1200的比例
        pictureBoxGameScreen = new PictureBox();
        pictureBoxGameScreen.Location = new Point(15, 15);
        pictureBoxGameScreen.Size = new Size(960, 600); // 1920x1200的一半比例
        pictureBoxGameScreen.BorderStyle = BorderStyle.None;
        pictureBoxGameScreen.SizeMode = PictureBoxSizeMode.Zoom;
        pictureBoxGameScreen.Paint += PictureBoxGameScreen_Paint;
        pictureBoxGameScreen.BackColor = Color.Black;
        
        // 添加邊框效果
        Panel gameBorderPanel = new Panel();
        gameBorderPanel.Location = new Point(12, 12);
        gameBorderPanel.Size = new Size(966, 606);
        gameBorderPanel.BackColor = Color.FromArgb(70, 130, 180);
        gameBorderPanel.Controls.Add(pictureBoxGameScreen);
        this.Controls.Add(gameBorderPanel);

        // 控制面板 - 重新設計
        GroupBox groupControl = new GroupBox();
        groupControl.Text = "🎮 控制面板";
        groupControl.Location = new Point(990, 15);
        groupControl.Size = new Size(590, 200);
        groupControl.BackColor = Color.White;
        groupControl.Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold);
        groupControl.ForeColor = Color.FromArgb(50, 50, 50);
        this.Controls.Add(groupControl);

        // 第一行：截圖和分析按鈕
        btnCapture = new Button();
        btnCapture.Text = "📷 截取完整遊戲畫面";
        btnCapture.Location = new Point(15, 25);
        btnCapture.Size = new Size(180, 35);
        btnCapture.BackColor = Color.FromArgb(52, 152, 219);
        btnCapture.ForeColor = Color.White;
        btnCapture.FlatStyle = FlatStyle.Flat;
        btnCapture.FlatAppearance.BorderSize = 0;
        btnCapture.Font = new Font("Microsoft YaHei UI", 9, FontStyle.Bold);
        btnCapture.Cursor = Cursors.Hand;
        btnCapture.Click += BtnCapture_Click;
        groupControl.Controls.Add(btnCapture);

        btnAnalyze = new Button();
        btnAnalyze.Text = "🤖 AI分析位置";
        btnAnalyze.Location = new Point(205, 25);
        btnAnalyze.Size = new Size(140, 35);
        btnAnalyze.BackColor = Color.FromArgb(155, 89, 182);
        btnAnalyze.ForeColor = Color.White;
        btnAnalyze.FlatStyle = FlatStyle.Flat;
        btnAnalyze.FlatAppearance.BorderSize = 0;
        btnAnalyze.Font = new Font("Microsoft YaHei UI", 9, FontStyle.Bold);
        btnAnalyze.Cursor = Cursors.Hand;
        btnAnalyze.Click += BtnAnalyze_Click;
        groupControl.Controls.Add(btnAnalyze);

        btnDetectButtons = new Button();
        btnDetectButtons.Text = "🔍 檢測UI按鈕";
        btnDetectButtons.Location = new Point(355, 25);
        btnDetectButtons.Size = new Size(140, 35);
        btnDetectButtons.BackColor = Color.FromArgb(39, 174, 96);
        btnDetectButtons.ForeColor = Color.White;
        btnDetectButtons.FlatStyle = FlatStyle.Flat;
        btnDetectButtons.FlatAppearance.BorderSize = 0;
        btnDetectButtons.Font = new Font("Microsoft YaHei UI", 9, FontStyle.Bold);
        btnDetectButtons.Cursor = Cursors.Hand;
        btnDetectButtons.Click += BtnDetectButtons_Click;
        groupControl.Controls.Add(btnDetectButtons);

        // 第二行：策略和選項
        Label labelStrategy = new Label();
        labelStrategy.Text = "AI分析策略:";
        labelStrategy.Location = new Point(15, 75);
        labelStrategy.Size = new Size(90, 20);
        labelStrategy.Font = new Font("Microsoft YaHei UI", 9);
        labelStrategy.ForeColor = Color.FromArgb(70, 70, 70);
        groupControl.Controls.Add(labelStrategy);

        comboBoxStrategy = new ComboBox();
        comboBoxStrategy.Location = new Point(110, 72);
        comboBoxStrategy.Size = new Size(160, 25);
        comboBoxStrategy.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBoxStrategy.Font = new Font("Microsoft YaHei UI", 9);
        comboBoxStrategy.Items.AddRange(new string[] {
            "安全空白區域",
            "避開UI元素", 
            "中心區域優先",
            "邊緣安全區域",
            "顏色分析",
            "避開按鈕區域"
        });
        comboBoxStrategy.SelectedIndex = 0;
        groupControl.Controls.Add(comboBoxStrategy);

        // 智能選項區域
        Panel optionsPanel = new Panel();
        optionsPanel.Location = new Point(280, 70);
        optionsPanel.Size = new Size(325, 30);
        optionsPanel.BackColor = Color.FromArgb(250, 250, 250);
        optionsPanel.BorderStyle = BorderStyle.FixedSingle;
        groupControl.Controls.Add(optionsPanel);

        checkBoxAutoAnalyze = new CheckBox();
        checkBoxAutoAnalyze.Text = "自動分析";
        checkBoxAutoAnalyze.Location = new Point(5, 5);
        checkBoxAutoAnalyze.Size = new Size(80, 20);
        checkBoxAutoAnalyze.Checked = true;
        checkBoxAutoAnalyze.Font = new Font("Microsoft YaHei UI", 8);
        checkBoxAutoAnalyze.ForeColor = Color.FromArgb(70, 70, 70);
        optionsPanel.Controls.Add(checkBoxAutoAnalyze);

        checkBoxAvoidButtons = new CheckBox();
        checkBoxAvoidButtons.Text = "智能避開UI按鈕";
        checkBoxAvoidButtons.Location = new Point(90, 5);
        checkBoxAvoidButtons.Size = new Size(120, 20);
        checkBoxAvoidButtons.Checked = true;
        checkBoxAvoidButtons.Font = new Font("Microsoft YaHei UI", 8);
        checkBoxAvoidButtons.ForeColor = Color.FromArgb(70, 70, 70);
        optionsPanel.Controls.Add(checkBoxAvoidButtons);

        checkBoxMouseReference = new CheckBox();
        checkBoxMouseReference.Text = "以滑鼠位置為參考";
        checkBoxMouseReference.Location = new Point(215, 5);
        checkBoxMouseReference.Size = new Size(105, 20);
        checkBoxMouseReference.Checked = false;
        checkBoxMouseReference.Font = new Font("Microsoft YaHei UI", 8);
        checkBoxMouseReference.ForeColor = Color.FromArgb(70, 70, 70);
        optionsPanel.Controls.Add(checkBoxMouseReference);

        // 第三行：參數設定
        Label labelInterval = new Label();
        labelInterval.Text = "間隔時間(秒):";
        labelInterval.Location = new Point(15, 115);
        labelInterval.Size = new Size(90, 20);
        labelInterval.Font = new Font("Microsoft YaHei UI", 9);
        labelInterval.ForeColor = Color.FromArgb(70, 70, 70);
        groupControl.Controls.Add(labelInterval);

        numericInterval = new NumericUpDown();
        numericInterval.Location = new Point(110, 112);
        numericInterval.Size = new Size(70, 25);
        numericInterval.Minimum = 5;
        numericInterval.Maximum = 3600;
        numericInterval.Value = 30;
        numericInterval.Font = new Font("Microsoft YaHei UI", 9);
        groupControl.Controls.Add(numericInterval);

        Label labelChanceTitle = new Label();
        labelChanceTitle.Text = "點擊機率:";
        labelChanceTitle.Location = new Point(195, 115);
        labelChanceTitle.Size = new Size(70, 20);
        labelChanceTitle.Font = new Font("Microsoft YaHei UI", 9);
        labelChanceTitle.ForeColor = Color.FromArgb(70, 70, 70);
        groupControl.Controls.Add(labelChanceTitle);

        trackBarClickChance = new TrackBar();
        trackBarClickChance.Location = new Point(270, 110);
        trackBarClickChance.Size = new Size(200, 25);
        trackBarClickChance.Minimum = 10;
        trackBarClickChance.Maximum = 100;
        trackBarClickChance.Value = 80;
        trackBarClickChance.TickFrequency = 10;
        trackBarClickChance.ValueChanged += TrackBarClickChance_ValueChanged;
        groupControl.Controls.Add(trackBarClickChance);

        labelClickChance = new Label();
        labelClickChance.Text = "機率: 80%";
        labelClickChance.Location = new Point(480, 115);
        labelClickChance.Size = new Size(80, 20);
        labelClickChance.ForeColor = Color.FromArgb(231, 76, 60);
        labelClickChance.Font = new Font("Microsoft YaHei UI", 9, FontStyle.Bold);
        groupControl.Controls.Add(labelClickChance);

        // 第四行：滑鼠和點擊設定
        labelMousePosition = new Label();
        labelMousePosition.Text = "滑鼠位置: (0, 0)";
        labelMousePosition.Location = new Point(15, 145);
        labelMousePosition.Size = new Size(150, 20);
        labelMousePosition.ForeColor = Color.FromArgb(52, 152, 219);
        labelMousePosition.Font = new Font("Microsoft YaHei UI", 9);
        groupControl.Controls.Add(labelMousePosition);

        checkBoxRightClick = new CheckBox();
        checkBoxRightClick.Text = "使用右鍵點擊";
        checkBoxRightClick.Location = new Point(175, 145);
        checkBoxRightClick.Size = new Size(110, 20);
        checkBoxRightClick.Checked = true;
        checkBoxRightClick.Font = new Font("Microsoft YaHei UI", 9);
        checkBoxRightClick.ForeColor = Color.FromArgb(70, 70, 70);
        groupControl.Controls.Add(checkBoxRightClick);

        // 第五行：開始/停止按鈕
        btnStartAFK = new Button();
        btnStartAFK.Text = "▶️ 開始AI反掛機";
        btnStartAFK.Location = new Point(15, 170);
        btnStartAFK.Size = new Size(180, 25);
        btnStartAFK.BackColor = Color.FromArgb(46, 204, 113);
        btnStartAFK.ForeColor = Color.White;
        btnStartAFK.FlatStyle = FlatStyle.Flat;
        btnStartAFK.FlatAppearance.BorderSize = 0;
        btnStartAFK.Font = new Font("Microsoft YaHei UI", 9, FontStyle.Bold);
        btnStartAFK.Cursor = Cursors.Hand;
        btnStartAFK.Click += BtnStartAFK_Click;
        groupControl.Controls.Add(btnStartAFK);

        btnStopAFK = new Button();
        btnStopAFK.Text = "⏹️ 停止反掛機";
        btnStopAFK.Location = new Point(205, 170);
        btnStopAFK.Size = new Size(140, 25);
        btnStopAFK.BackColor = Color.FromArgb(231, 76, 60);
        btnStopAFK.ForeColor = Color.White;
        btnStopAFK.FlatStyle = FlatStyle.Flat;
        btnStopAFK.FlatAppearance.BorderSize = 0;
        btnStopAFK.Font = new Font("Microsoft YaHei UI", 9, FontStyle.Bold);
        btnStopAFK.Cursor = Cursors.Hand;
        btnStopAFK.Enabled = false;
        btnStopAFK.Click += BtnStopAFK_Click;
        groupControl.Controls.Add(btnStopAFK);

        // AI分析結果面板
        GroupBox groupAnalysis = new GroupBox();
        groupAnalysis.Text = "🤖 AI分析結果";
        groupAnalysis.Location = new Point(990, 225);
        groupAnalysis.Size = new Size(590, 200);
        groupAnalysis.BackColor = Color.White;
        groupAnalysis.Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold);
        groupAnalysis.ForeColor = Color.FromArgb(50, 50, 50);
        this.Controls.Add(groupAnalysis);

        labelAnalysisResult = new Label();
        labelAnalysisResult.Text = "請先截取遊戲畫面進行AI分析";
        labelAnalysisResult.Location = new Point(15, 25);
        labelAnalysisResult.Size = new Size(560, 20);
        labelAnalysisResult.ForeColor = Color.FromArgb(52, 73, 94);
        labelAnalysisResult.Font = new Font("Microsoft YaHei UI", 9);
        groupAnalysis.Controls.Add(labelAnalysisResult);

        Label labelSafeZones = new Label();
        labelSafeZones.Text = "AI建議的安全點擊位置:";
        labelSafeZones.Location = new Point(15, 50);
        labelSafeZones.Size = new Size(200, 20);
        labelSafeZones.Font = new Font("Microsoft YaHei UI", 9, FontStyle.Bold);
        labelSafeZones.ForeColor = Color.FromArgb(39, 174, 96);
        groupAnalysis.Controls.Add(labelSafeZones);

        listBoxSafeZones = new ListBox();
        listBoxSafeZones.Location = new Point(15, 75);
        listBoxSafeZones.Size = new Size(560, 115);
        listBoxSafeZones.Font = new Font("Consolas", 9);
        listBoxSafeZones.BackColor = Color.FromArgb(248, 249, 250);
        listBoxSafeZones.BorderStyle = BorderStyle.FixedSingle;
        groupAnalysis.Controls.Add(listBoxSafeZones);

        // UI按鈕檢測結果面板
        GroupBox groupButtonDetection = new GroupBox();
        groupButtonDetection.Text = "🎯 UI按鈕檢測結果";
        groupButtonDetection.Location = new Point(990, 435);
        groupButtonDetection.Size = new Size(590, 120);
        groupButtonDetection.BackColor = Color.White;
        groupButtonDetection.Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold);
        groupButtonDetection.ForeColor = Color.FromArgb(50, 50, 50);
        this.Controls.Add(groupButtonDetection);

        labelButtonDetection = new Label();
        labelButtonDetection.Text = "檢測到的UI按鈕: 0個 (點擊檢測按鈕開始分析)";
        labelButtonDetection.Location = new Point(15, 25);
        labelButtonDetection.Size = new Size(560, 20);
        labelButtonDetection.Font = new Font("Microsoft YaHei UI", 9, FontStyle.Bold);
        labelButtonDetection.ForeColor = Color.FromArgb(230, 126, 34);
        groupButtonDetection.Controls.Add(labelButtonDetection);

        listBoxDetectedButtons = new ListBox();
        listBoxDetectedButtons.Location = new Point(15, 50);
        listBoxDetectedButtons.Size = new Size(560, 60);
        listBoxDetectedButtons.Font = new Font("Consolas", 8);
        listBoxDetectedButtons.BackColor = Color.FromArgb(248, 249, 250);
        listBoxDetectedButtons.BorderStyle = BorderStyle.FixedSingle;
        listBoxDetectedButtons.ScrollAlwaysVisible = true;
        groupButtonDetection.Controls.Add(listBoxDetectedButtons);

        // 狀態顯示
        GroupBox groupStatus = new GroupBox();
        groupStatus.Text = "📊 運行狀態";
        groupStatus.Location = new Point(990, 565);
        groupStatus.Size = new Size(590, 60);
        groupStatus.BackColor = Color.White;
        groupStatus.Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold);
        groupStatus.ForeColor = Color.FromArgb(50, 50, 50);
        this.Controls.Add(groupStatus);

        labelStatus = new Label();
        labelStatus.Text = "狀態: 等待開始";
        labelStatus.Location = new Point(15, 25);
        labelStatus.Size = new Size(560, 25);
        labelStatus.ForeColor = Color.FromArgb(39, 174, 96);
        labelStatus.Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold);
        groupStatus.Controls.Add(labelStatus);

        // 使用說明
        GroupBox groupInstructions = new GroupBox();
        groupInstructions.Text = "📋 使用說明";
        groupInstructions.Location = new Point(15, 545);
        groupInstructions.Size = new Size(1580, 120);
        groupInstructions.BackColor = Color.FromArgb(250, 250, 250);
        groupInstructions.Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold);
        groupInstructions.ForeColor = Color.FromArgb(50, 50, 50);
        this.Controls.Add(groupInstructions);

        Label labelInstructions = new Label();
        labelInstructions.Text = "1. 點擊「截取完整遊戲畫面」獲取當前畫面\n" +
                                "2. 點擊「檢測UI按鈕」分析遊戲界面按鈕位置\n" +
                                "3. 選擇AI分析策略，點擊「AI分析位置」\n" +
                                "4. 調整點擊機率和間隔時間，開啟「智能避開UI按鈕」\n" +
                                "5. 開啟「以滑鼠位置為參考」可讓AI優先分析滑鼠附近區域\n" +
                                "6. AI會隨機選擇安全位置進行點擊，避開UI元素";
        labelInstructions.Location = new Point(15, 25);
        labelInstructions.Size = new Size(1550, 85);
        labelInstructions.Font = new Font("Microsoft YaHei UI", 9);
        labelInstructions.ForeColor = Color.FromArgb(70, 70, 70);
        groupInstructions.Controls.Add(labelInstructions);

        FindGameWindow();
    }

    private void FindGameWindow()
    {
        string[] possibleTitles = {
            "Diablo Immortal",
            "暗黑不朽",
            "DiabloImmortal"
        };

        foreach (string title in possibleTitles)
        {
            gameWindow = FindWindow(null, title);
            if (gameWindow != IntPtr.Zero && IsWindowVisible(gameWindow))
            {
                labelStatus.Text = $"狀態: 找到遊戲 - {title}";
                return;
            }
        }

        Process[] processes = Process.GetProcesses();
        foreach (Process process in processes)
        {
            try
            {
                if (process.ProcessName.ToLower().Contains("diablo") || 
                    process.ProcessName.ToLower().Contains("immortal"))
                {
                    if (process.MainWindowHandle != IntPtr.Zero && IsWindowVisible(process.MainWindowHandle))
                    {
                        gameWindow = process.MainWindowHandle;
                        labelStatus.Text = $"狀態: 找到遊戲 - {process.ProcessName}";
                        return;
                    }
                }
            }
            catch
            {
                continue;
            }
        }

        labelStatus.Text = "狀態: 未找到遊戲";
        gameWindow = IntPtr.Zero;
    }

    private async void BtnCapture_Click(object sender, EventArgs e)
    {
        if (gameWindow == IntPtr.Zero)
        {
            FindGameWindow();
            if (gameWindow == IntPtr.Zero)
            {
                MessageBox.Show("未找到暗黑不朽遊戲視窗！", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        Bitmap screenshot = CaptureWindow(gameWindow);
        if (screenshot != null)
        {
            pictureBoxGameScreen.Image = screenshot;
            labelStatus.Text = "狀態: 畫面已截取";
            
            if (checkBoxAutoAnalyze.Checked)
            {
                await Task.Delay(500); // 等待畫面更新
                BtnAnalyze_Click(sender, e);
            }
        }
        else
        {
            MessageBox.Show("截取畫面失敗！", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void BtnAnalyze_Click(object sender, EventArgs e)
    {
        if (pictureBoxGameScreen.Image == null)
        {
            MessageBox.Show("請先截取遊戲畫面！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        btnAnalyze.Enabled = false;
        labelAnalysisResult.Text = "🤖 AI正在分析畫面...";
        
        await Task.Run(() => {
            AnalysisStrategy strategy = (AnalysisStrategy)comboBoxStrategy.SelectedIndex;
            AnalyzeGameScreen((Bitmap)pictureBoxGameScreen.Image, strategy);
        });

        btnAnalyze.Enabled = true;
        pictureBoxGameScreen.Invalidate(); // 重繪以顯示分析結果
    }

    private void AnalyzeGameScreen(Bitmap screenshot, AnalysisStrategy strategy)
    {
        safeClickPositions.Clear();
        
        try
        {
            // 如果啟用了智能避開UI按鈕，先檢測按鈕
            if (checkBoxAvoidButtons.Checked && detectedButtons.Count == 0)
            {
                DetectUIButtons(screenshot);
            }

            switch (strategy)
            {
                case AnalysisStrategy.SafeZone:
                    FindSafeZones(screenshot);
                    break;
                case AnalysisStrategy.AvoidUI:
                    AvoidUIElements(screenshot);
                    break;
                case AnalysisStrategy.CenterFocus:
                    FindCenterSafeZones(screenshot);
                    break;
                case AnalysisStrategy.EdgeSafe:
                    FindEdgeSafeZones(screenshot);
                    break;
                case AnalysisStrategy.ColorAnalysis:
                    PerformColorAnalysis(screenshot);
                    break;
                case AnalysisStrategy.AvoidButtons:
                    AvoidButtons(screenshot);
                    break;
            }

            // 如果啟用了智能避開UI按鈕，過濾掉與按鈕重疊的位置
            if (checkBoxAvoidButtons.Checked && detectedButtons.Count > 0 && strategy != AnalysisStrategy.AvoidButtons)
            {
                FilterPositionsAwayFromButtons();
            }

            this.Invoke(new Action(() => {
                UpdateAnalysisResults();
                // 重新繪製以顯示檢測到的按鈕
                pictureBoxGameScreen.Invalidate();
            }));
        }
        catch (Exception ex)
        {
            this.Invoke(new Action(() => {
                labelAnalysisResult.Text = $"分析錯誤: {ex.Message}";
            }));
        }
    }

    private void FilterPositionsAwayFromButtons()
    {
        List<Point> filteredPositions = new List<Point>();
        
        foreach (Point position in safeClickPositions)
        {
            bool isInButtonArea = false;
            
            foreach (Rectangle button in detectedButtons)
            {
                // 為按鈕區域添加安全邊距
                Rectangle safeZone = new Rectangle(
                    button.X - 25, button.Y - 25,
                    button.Width + 50, button.Height + 50
                );

                if (safeZone.Contains(position))
                {
                    isInButtonArea = true;
                    break;
                }
            }

            if (!isInButtonArea)
            {
                filteredPositions.Add(position);
            }
        }

        safeClickPositions = filteredPositions;
    }

    private void FindSafeZones(Bitmap screenshot)
    {
        // 尋找大面積的相似顏色區域（通常是地面或背景）
        int width = screenshot.Width;
        int height = screenshot.Height;
        
        // 如果啟用滑鼠參考模式，優先分析滑鼠附近區域
        if (checkBoxMouseReference.Checked && currentMousePosition != Point.Empty)
        {
            FindSafeZonesByMouseReference(screenshot, currentMousePosition);
            return;
        }
        
        // 分析畫面中心區域 (30%-70%)
        int startX = (int)(width * 0.3);
        int endX = (int)(width * 0.7);
        int startY = (int)(height * 0.3);
        int endY = (int)(height * 0.7);
        
        List<Point> candidates = new List<Point>();
        
        for (int x = startX; x < endX; x += 20)
        {
            for (int y = startY; y < endY; y += 20)
            {
                if (IsSafeArea(screenshot, x, y, 30))
                {
                    candidates.Add(new Point(x, y));
                }
            }
        }
        
        // 選取最佳候選位置
        safeClickPositions = candidates.Take(10).ToList();
    }

    private void FindSafeZonesByMouseReference(Bitmap screenshot, Point mousePos)
    {
        List<Point> candidates = new List<Point>();
        
        // 在滑鼠位置周圍尋找安全區域
        int[] radiuses = { 50, 100, 150, 200 };
        
        foreach (int radius in radiuses)
        {
            for (int angle = 0; angle < 360; angle += 30)
            {
                int x = mousePos.X + (int)(radius * Math.Cos(angle * Math.PI / 180));
                int y = mousePos.Y + (int)(radius * Math.Sin(angle * Math.PI / 180));
                
                if (x >= 30 && x < screenshot.Width - 30 && y >= 30 && y < screenshot.Height - 30)
                {
                    if (IsSafeArea(screenshot, x, y, 25))
                    {
                        candidates.Add(new Point(x, y));
                    }
                }
            }
        }
        
        // 按距離滑鼠位置排序，選擇最近的
        candidates = candidates.OrderBy(p => Math.Sqrt(Math.Pow(p.X - mousePos.X, 2) + Math.Pow(p.Y - mousePos.Y, 2))).Take(10).ToList();
        safeClickPositions = candidates;
    }

    private void AvoidUIElements(Bitmap screenshot)
    {
        // 避開可能的UI元素（邊緣、角落、高對比度區域）
        int width = screenshot.Width;
        int height = screenshot.Height;
        
        // 避開邊緣區域
        int margin = Math.Min(width, height) / 10;
        int startX = margin;
        int endX = width - margin;
        int startY = margin;
        int endY = height - margin;
        
        for (int x = startX; x < endX; x += 25)
        {
            for (int y = startY; y < endY; y += 25)
            {
                if (!IsHighContrastArea(screenshot, x, y, 20))
                {
                    safeClickPositions.Add(new Point(x, y));
                    if (safeClickPositions.Count >= 15) return;
                }
            }
        }
    }

    private void FindCenterSafeZones(Bitmap screenshot)
    {
        // 專注於畫面中心區域
        int centerX = screenshot.Width / 2;
        int centerY = screenshot.Height / 2;
        int radius = Math.Min(screenshot.Width, screenshot.Height) / 4;
        
        for (int angle = 0; angle < 360; angle += 30)
        {
            for (int r = 20; r < radius; r += 30)
            {
                int x = centerX + (int)(r * Math.Cos(angle * Math.PI / 180));
                int y = centerY + (int)(r * Math.Sin(angle * Math.PI / 180));
                
                if (x >= 0 && x < screenshot.Width && y >= 0 && y < screenshot.Height)
                {
                    if (IsSafeArea(screenshot, x, y, 25))
                    {
                        safeClickPositions.Add(new Point(x, y));
                    }
                }
            }
        }
    }

    private void FindEdgeSafeZones(Bitmap screenshot)
    {
        // 尋找邊緣的安全區域，但不太靠近邊界
        int width = screenshot.Width;
        int height = screenshot.Height;
        int margin = 50;
        
        // 上邊緣
        for (int x = margin; x < width - margin; x += 40)
        {
            int y = margin;
            if (IsSafeArea(screenshot, x, y, 20))
                safeClickPositions.Add(new Point(x, y));
        }
        
        // 下邊緣
        for (int x = margin; x < width - margin; x += 40)
        {
            int y = height - margin;
            if (IsSafeArea(screenshot, x, y, 20))
                safeClickPositions.Add(new Point(x, y));
        }
        
        // 左右邊緣
        for (int y = margin; y < height - margin; y += 40)
        {
            int x = margin;
            if (IsSafeArea(screenshot, x, y, 20))
                safeClickPositions.Add(new Point(x, y));
                
            x = width - margin;
            if (IsSafeArea(screenshot, x, y, 20))
                safeClickPositions.Add(new Point(x, y));
        }
    }

    private void PerformColorAnalysis(Bitmap screenshot)
    {
        // 基於顏色分析找到相似的大面積區域
        Dictionary<int, List<Point>> colorGroups = new Dictionary<int, List<Point>>();
        
        for (int x = 20; x < screenshot.Width - 20; x += 15)
        {
            for (int y = 20; y < screenshot.Height - 20; y += 15)
            {
                Color pixel = screenshot.GetPixel(x, y);
                int colorKey = GetColorGroup(pixel);
                
                if (!colorGroups.ContainsKey(colorKey))
                    colorGroups[colorKey] = new List<Point>();
                    
                colorGroups[colorKey].Add(new Point(x, y));
            }
        }
        
        // 選擇最大的顏色群組中的點
        var largestGroup = colorGroups.OrderByDescending(g => g.Value.Count).FirstOrDefault();
        if (largestGroup.Value != null && largestGroup.Value.Count > 10)
        {
            safeClickPositions = largestGroup.Value.Take(12).ToList();
        }
    }

    private bool IsSafeArea(Bitmap screenshot, int centerX, int centerY, int checkRadius)
    {
        if (centerX < checkRadius || centerY < checkRadius || 
            centerX >= screenshot.Width - checkRadius || centerY >= screenshot.Height - checkRadius)
            return false;

        Color centerColor = screenshot.GetPixel(centerX, centerY);
        int similarColorCount = 0;
        int totalChecked = 0;

        for (int x = centerX - checkRadius; x <= centerX + checkRadius; x += 5)
        {
            for (int y = centerY - checkRadius; y <= centerY + checkRadius; y += 5)
            {
                if (x >= 0 && x < screenshot.Width && y >= 0 && y < screenshot.Height)
                {
                    Color pixelColor = screenshot.GetPixel(x, y);
                    if (AreColorsSimilar(centerColor, pixelColor, 30))
                        similarColorCount++;
                    totalChecked++;
                }
            }
        }

        return totalChecked > 0 && (similarColorCount / (double)totalChecked) > 0.7;
    }

    private bool IsHighContrastArea(Bitmap screenshot, int centerX, int centerY, int checkRadius)
    {
        if (centerX < checkRadius || centerY < checkRadius || 
            centerX >= screenshot.Width - checkRadius || centerY >= screenshot.Height - checkRadius)
            return true;

        List<Color> colors = new List<Color>();
        
        for (int x = centerX - checkRadius; x <= centerX + checkRadius; x += 8)
        {
            for (int y = centerY - checkRadius; y <= centerY + checkRadius; y += 8)
            {
                if (x >= 0 && x < screenshot.Width && y >= 0 && y < screenshot.Height)
                {
                    colors.Add(screenshot.GetPixel(x, y));
                }
            }
        }

        if (colors.Count < 4) return true;

        // 檢查顏色變化是否太大
        int maxDifference = 0;
        for (int i = 0; i < colors.Count - 1; i++)
        {
            for (int j = i + 1; j < colors.Count; j++)
            {
                int diff = Math.Abs(colors[i].R - colors[j].R) + 
                          Math.Abs(colors[i].G - colors[j].G) + 
                          Math.Abs(colors[i].B - colors[j].B);
                maxDifference = Math.Max(maxDifference, diff);
            }
        }

        return maxDifference > 150; // 高對比度閾值
    }

    private bool AreColorsSimilar(Color color1, Color color2, int threshold)
    {
        int diff = Math.Abs(color1.R - color2.R) + 
                  Math.Abs(color1.G - color2.G) + 
                  Math.Abs(color1.B - color2.B);
        return diff <= threshold;
    }

    private int GetColorGroup(Color color)
    {
        // 將顏色分組到較大的色系中
        int r = color.R / 32;
        int g = color.G / 32;
        int b = color.B / 32;
        return (r << 10) | (g << 5) | b;
    }

    private void UpdateAnalysisResults()
    {
        listBoxSafeZones.Items.Clear();
        
        if (safeClickPositions.Count > 0)
        {
            labelAnalysisResult.Text = $"✅ AI分析完成！找到 {safeClickPositions.Count} 個安全位置";
            
            for (int i = 0; i < safeClickPositions.Count; i++)
            {
                Point pos = safeClickPositions[i];
                listBoxSafeZones.Items.Add($"位置 {i + 1}: ({pos.X}, {pos.Y})");
            }
        }
        else
        {
            labelAnalysisResult.Text = "⚠️ 未找到合適的點擊位置，請嘗試其他分析策略";
        }
    }

    private Bitmap CaptureWindow(IntPtr hWnd)
    {
        if (!GetWindowRect(hWnd, out RECT rect))
            return null;

        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;

        // 使用PrintWindow方法獲取完整的遊戲畫面，包括被遮擋的部分
        Bitmap bitmap = null;
        try
        {
            bitmap = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                IntPtr hDC = g.GetHdc();
                // 先嘗試使用PrintWindow截取完整內容
                bool success = PrintWindow(hWnd, hDC, PW_CLIENTONLY);
                g.ReleaseHdc(hDC);
                
                if (!success)
                {
                    // 如果PrintWindow失敗，使用傳統方法
                    IntPtr windowDC = GetDC(hWnd);
                    IntPtr memDC = CreateCompatibleDC(windowDC);
                    IntPtr hBitmap = CreateCompatibleBitmap(windowDC, width, height);
                    IntPtr hOldBitmap = SelectObject(memDC, hBitmap);

                    BitBlt(memDC, 0, 0, width, height, windowDC, 0, 0, SRCCOPY);

                    bitmap.Dispose();
                    bitmap = Image.FromHbitmap(hBitmap);

                    SelectObject(memDC, hOldBitmap);
                    DeleteObject(hBitmap);
                    DeleteDC(memDC);
                    ReleaseDC(hWnd, windowDC);
                }
            }
        }
        catch (Exception ex)
        {
            labelStatus.Text = $"截圖錯誤: {ex.Message}";
            return null;
        }

        return bitmap;
    }

    private void PictureBoxGameScreen_Paint(object sender, PaintEventArgs e)
    {
        if (pictureBoxGameScreen.Image != null)
        {
            // 計算縮放比例 - 保持比例的縮放
            float scaleX = (float)pictureBoxGameScreen.Width / pictureBoxGameScreen.Image.Width;
            float scaleY = (float)pictureBoxGameScreen.Height / pictureBoxGameScreen.Image.Height;
            float scale = Math.Min(scaleX, scaleY); // 使用較小的縮放比例保持比例
            
            // 計算居中偏移
            float offsetX = (pictureBoxGameScreen.Width - pictureBoxGameScreen.Image.Width * scale) / 2;
            float offsetY = (pictureBoxGameScreen.Height - pictureBoxGameScreen.Image.Height * scale) / 2;

            // 繪製檢測到的UI按鈕
            if (detectedButtons.Count > 0)
            {
                using (Pen redPen = new Pen(Color.Red, 2))
                using (Brush redBrush = new SolidBrush(Color.FromArgb(80, Color.Red)))
                using (Font buttonFont = new Font("Arial", 8, FontStyle.Bold))
                using (Brush textBrush = new SolidBrush(Color.White))
                {
                    foreach (Rectangle button in detectedButtons)
                    {
                        float displayX = button.X * scale + offsetX;
                        float displayY = button.Y * scale + offsetY;
                        float displayWidth = button.Width * scale;
                        float displayHeight = button.Height * scale;
                        
                        // 繪製紅色邊框和半透明填充
                        e.Graphics.FillRectangle(redBrush, displayX, displayY, displayWidth, displayHeight);
                        e.Graphics.DrawRectangle(redPen, displayX, displayY, displayWidth, displayHeight);
                        
                        // 在按鈕中心繪製 "UI" 標記
                        string buttonText = "UI";
                        SizeF textSize = e.Graphics.MeasureString(buttonText, buttonFont);
                        float textX = displayX + displayWidth/2 - textSize.Width/2;
                        float textY = displayY + displayHeight/2 - textSize.Height/2;
                        e.Graphics.DrawString(buttonText, buttonFont, textBrush, textX, textY);
                    }
                }
            }

            // 繪製安全點擊位置
            if (safeClickPositions.Count > 0)
            {
                using (Brush greenBrush = new SolidBrush(Color.FromArgb(150, Color.Green)))
                using (Pen greenPen = new Pen(Color.DarkGreen, 2))
                using (Font font = new Font("Arial", 8, FontStyle.Bold))
                using (Brush textBrush = new SolidBrush(Color.White))
                {
                    for (int i = 0; i < safeClickPositions.Count; i++)
                    {
                        Point pos = safeClickPositions[i];
                        float displayX = pos.X * scale + offsetX;
                        float displayY = pos.Y * scale + offsetY;

                        // 繪製綠色圓圈
                        e.Graphics.FillEllipse(greenBrush, displayX - 8, displayY - 8, 16, 16);
                        e.Graphics.DrawEllipse(greenPen, displayX - 8, displayY - 8, 16, 16);
                        
                        // 繪製數字
                        string text = (i + 1).ToString();
                        SizeF textSize = e.Graphics.MeasureString(text, font);
                        e.Graphics.DrawString(text, font, textBrush, 
                            displayX - textSize.Width / 2, displayY - textSize.Height / 2);
                    }
                }
            }

            // 繪製滑鼠位置參考點
            if (checkBoxMouseReference.Checked)
            {
                using (Brush mouseBrush = new SolidBrush(Color.FromArgb(180, Color.Blue)))
                using (Pen mousePen = new Pen(Color.DarkBlue, 2))
                using (Font mouseFont = new Font("Arial", 8, FontStyle.Bold))
                using (Brush mouseTextBrush = new SolidBrush(Color.White))
                {
                    float mouseX = currentMousePosition.X * scale + offsetX;
                    float mouseY = currentMousePosition.Y * scale + offsetY;
                    
                    e.Graphics.FillEllipse(mouseBrush, mouseX - 6, mouseY - 6, 12, 12);
                    e.Graphics.DrawEllipse(mousePen, mouseX - 6, mouseY - 6, 12, 12);
                    e.Graphics.DrawString("鼠", mouseFont, mouseTextBrush, mouseX - 6, mouseY - 6);
                }
            }
        }
    }

    private void BtnStartAFK_Click(object sender, EventArgs e)
    {
        if (safeClickPositions.Count == 0)
        {
            MessageBox.Show("請先進行AI分析以找到安全點擊位置！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        isAFKRunning = true;
        timerAFK.Interval = (int)(numericInterval.Value * 1000);
        timerAFK.Start();

        btnStartAFK.Enabled = false;
        btnStopAFK.Enabled = true;
        labelStatus.Text = "狀態: AI反掛機運行中";
    }

    private void BtnStopAFK_Click(object sender, EventArgs e)
    {
        isAFKRunning = false;
        timerAFK.Stop();

        btnStartAFK.Enabled = true;
        btnStopAFK.Enabled = false;
        labelStatus.Text = "狀態: AI反掛機已停止";
    }

    private void TimerAFK_Tick(object sender, EventArgs e)
    {
        if (!isAFKRunning || gameWindow == IntPtr.Zero || safeClickPositions.Count == 0) 
            return;

        PerformAIClick();
    }

    private void PerformAIClick()
    {
        // 根據設定的機率決定是否執行點擊
        int clickChance = trackBarClickChance.Value;
        if (random.Next(100) >= clickChance)
        {
            labelStatus.Text = $"狀態: 跳過點擊 (機率: {clickChance}%) - {DateTime.Now:HH:mm:ss}";
            return;
        }

        // 隨機選擇一個AI分析出的安全位置
        Point clickPos = safeClickPositions[random.Next(safeClickPositions.Count)];
        
        IntPtr lParam = (IntPtr)((clickPos.Y << 16) | (clickPos.X & 0xFFFF));

        if (checkBoxRightClick.Checked)
        {
            PostMessage(gameWindow, WM_RBUTTONDOWN, IntPtr.Zero, lParam);
            Thread.Sleep(50);
            PostMessage(gameWindow, WM_RBUTTONUP, IntPtr.Zero, lParam);
        }
        else
        {
            PostMessage(gameWindow, WM_LBUTTONDOWN, IntPtr.Zero, lParam);
            Thread.Sleep(50);
            PostMessage(gameWindow, WM_LBUTTONUP, IntPtr.Zero, lParam);
        }

        labelStatus.Text = $"狀態: AI點擊位置 ({clickPos.X}, {clickPos.Y}) - {DateTime.Now:HH:mm:ss}";
    }

    private void TimerMouseTracker_Tick(object sender, EventArgs e)
    {
        if (checkBoxMouseReference.Checked && gameWindow != IntPtr.Zero)
        {
            POINT point;
            if (GetCursorPos(out point))
            {
                POINT clientPoint = point;
                if (ScreenToClient(gameWindow, ref clientPoint))
                {
                    currentMousePosition = new Point(clientPoint.X, clientPoint.Y);
                    labelMousePosition.Text = $"滑鼠位置: ({clientPoint.X}, {clientPoint.Y})";
                }
            }
        }
        else
        {
            labelMousePosition.Text = "滑鼠位置: 未啟用參考模式";
        }
    }

    private void TrackBarClickChance_ValueChanged(object sender, EventArgs e)
    {
        labelClickChance.Text = $"機率: {trackBarClickChance.Value}%";
    }

    private async void BtnDetectButtons_Click(object sender, EventArgs e)
    {
        if (gameWindow == IntPtr.Zero)
        {
            labelButtonDetection.Text = "錯誤: 未找到遊戲視窗";
            return;
        }

        labelButtonDetection.Text = "正在檢測UI按鈕...";
        listBoxDetectedButtons.Items.Clear();
        detectedButtons.Clear();

        try
        {
            // 截取遊戲畫面
            Bitmap screenshot = CaptureWindow(gameWindow);
            if (screenshot != null)
            {
                // 執行按鈕檢測
                DetectUIButtons(screenshot);
                
                // 更新顯示
                labelButtonDetection.Text = $"檢測到的UI按鈕: {detectedButtons.Count}個";
                
                // 顯示檢測結果
                foreach (var button in detectedButtons)
                {
                    listBoxDetectedButtons.Items.Add($"按鈕區域: ({button.X}, {button.Y}) 大小: {button.Width}x{button.Height}");
                }

                // 重新繪製畫面以顯示檢測到的按鈕
                pictureBoxGameScreen.Image = screenshot;
                pictureBoxGameScreen.Invalidate();
            }
            else
            {
                labelButtonDetection.Text = "錯誤: 無法截取遊戲畫面";
            }
        }
        catch (Exception ex)
        {
            labelButtonDetection.Text = $"檢測失敗: {ex.Message}";
        }
    }

    private void DetectUIButtons(Bitmap screenshot)
    {
        detectedButtons.Clear();
        int width = screenshot.Width;
        int height = screenshot.Height;

        // 重點檢測右下角區域 (遊戲UI按鈕通常在這裡)
        int rightBottomX = (int)(width * 0.7); // 右邊30%區域
        int rightBottomY = (int)(height * 0.7); // 下邊30%區域

        // 檢測右下角按鈕
        DetectButtonsInRegion(screenshot, rightBottomX, rightBottomY, width, height, "右下角");

        // 檢測右上角區域 (小地圖、設定按鈕等)
        DetectButtonsInRegion(screenshot, rightBottomX, 0, width, (int)(height * 0.3), "右上角");

        // 檢測左下角區域 (技能按鈕等)
        DetectButtonsInRegion(screenshot, 0, rightBottomY, (int)(width * 0.3), height, "左下角");

        // 檢測底部中央區域 (聊天、背包等)
        DetectButtonsInRegion(screenshot, (int)(width * 0.3), (int)(height * 0.85), (int)(width * 0.4), height, "底部中央");
    }

    private void DetectButtonsInRegion(Bitmap screenshot, int startX, int startY, int endX, int endY, string regionName)
    {
        // 使用邊緣檢測和顏色分析來找出可能的按鈕
        for (int y = startY; y < endY - 20; y += 10)
        {
            for (int x = startX; x < endX - 20; x += 10)
            {
                if (IsPossibleButton(screenshot, x, y))
                {
                    // 找到可能的按鈕，確定其邊界
                    Rectangle buttonRect = GetButtonBounds(screenshot, x, y);
                    if (buttonRect.Width > 15 && buttonRect.Height > 15 && 
                        buttonRect.Width < 200 && buttonRect.Height < 200)
                    {
                        // 檢查是否與已檢測的按鈕重疊
                        bool isOverlapping = false;
                        foreach (var existingButton in detectedButtons)
                        {
                            if (buttonRect.IntersectsWith(existingButton))
                            {
                                isOverlapping = true;
                                break;
                            }
                        }

                        if (!isOverlapping)
                        {
                            detectedButtons.Add(buttonRect);
                        }
                    }
                }
            }
        }
    }

    private bool IsPossibleButton(Bitmap screenshot, int x, int y)
    {
        try
        {
            // 檢查是否有按鈕特徵：邊框、圓角、特定顏色等
            Color centerColor = screenshot.GetPixel(x, y);
            
            // 檢查周圍像素是否有邊框特徵
            int edgeCount = 0;
            for (int dy = -2; dy <= 2; dy++)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    if (x + dx >= 0 && x + dx < screenshot.Width && 
                        y + dy >= 0 && y + dy < screenshot.Height)
                    {
                        Color neighborColor = screenshot.GetPixel(x + dx, y + dy);
                        if (IsEdgePixel(centerColor, neighborColor))
                        {
                            edgeCount++;
                        }
                    }
                }
            }

            // 如果有足夠的邊緣像素，可能是按鈕
            return edgeCount >= 3;
        }
        catch
        {
            return false;
        }
    }

    private bool IsEdgePixel(Color center, Color neighbor)
    {
        // 檢查顏色差異是否足夠大，表示邊緣
        int rDiff = Math.Abs(center.R - neighbor.R);
        int gDiff = Math.Abs(center.G - neighbor.G);
        int bDiff = Math.Abs(center.B - neighbor.B);
        
        return (rDiff + gDiff + bDiff) > 100;
    }

    private Rectangle GetButtonBounds(Bitmap screenshot, int startX, int startY)
    {
        // 從起始點擴展找出按鈕的完整邊界
        Color baseColor = screenshot.GetPixel(startX, startY);
        
        int minX = startX, maxX = startX;
        int minY = startY, maxY = startY;

        // 向四個方向擴展找邊界
        for (int x = startX; x >= 0 && x < screenshot.Width; x--)
        {
            if (AreColorsSimilar(screenshot.GetPixel(x, startY), baseColor, 50))
                minX = x;
            else
                break;
        }

        for (int x = startX; x < screenshot.Width; x++)
        {
            if (AreColorsSimilar(screenshot.GetPixel(x, startY), baseColor, 50))
                maxX = x;
            else
                break;
        }

        for (int y = startY; y >= 0 && y < screenshot.Height; y--)
        {
            if (AreColorsSimilar(screenshot.GetPixel(startX, y), baseColor, 50))
                minY = y;
            else
                break;
        }

        for (int y = startY; y < screenshot.Height; y++)
        {
            if (AreColorsSimilar(screenshot.GetPixel(startX, y), baseColor, 50))
                maxY = y;
            else
                break;
        }

        return new Rectangle(minX, minY, maxX - minX, maxY - minY);
    }

    private void AvoidButtons(Bitmap screenshot)
    {
        // 首先檢測按鈕（如果還沒檢測）
        if (detectedButtons.Count == 0)
        {
            DetectUIButtons(screenshot);
        }

        // 基於檢測到的按鈕區域找安全點擊位置
        safeClickPositions.Clear();
        
        int width = screenshot.Width;
        int height = screenshot.Height;
        int attempts = 0;
        int maxAttempts = 500;

        while (safeClickPositions.Count < 10 && attempts < maxAttempts)
        {
            attempts++;
            
            // 隨機選擇位置
            int x = random.Next(50, width - 50);
            int y = random.Next(50, height - 50);

            // 檢查是否與任何檢測到的按鈕重疊
            bool isInButtonArea = false;
            foreach (var button in detectedButtons)
            {
                // 為按鈕區域添加安全邊距
                Rectangle safeZone = new Rectangle(
                    button.X - 20, button.Y - 20,
                    button.Width + 40, button.Height + 40
                );

                if (safeZone.Contains(x, y))
                {
                    isInButtonArea = true;
                    break;
                }
            }

            // 如果不在按鈕區域內，且是安全區域，添加到列表
            if (!isInButtonArea && IsSafeArea(screenshot, x, y, 15))
            {
                safeClickPositions.Add(new Point(x, y));
            }
        }

        UpdateAnalysisResults();
    }
}

class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new AIVisualAntiAFKForm());
    }
} 