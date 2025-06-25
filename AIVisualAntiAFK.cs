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

    struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    const uint WM_LBUTTONDOWN = 0x0201;
    const uint WM_LBUTTONUP = 0x0202;
    const uint WM_RBUTTONDOWN = 0x0204;
    const uint WM_RBUTTONUP = 0x0205;
    const int SRCCOPY = 0x00CC0020;

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
    private ComboBox comboBoxStrategy;
    private ListBox listBoxSafeZones;
    private Timer timerAFK;
    private IntPtr gameWindow;
    private List<Point> safeClickPositions = new List<Point>();
    private bool isAFKRunning = false;
    private Random random = new Random();

    // AI 分析策略
    public enum AnalysisStrategy
    {
        SafeZone,           // 尋找安全空白區域
        AvoidUI,            // 避開UI元素
        CenterFocus,        // 中心區域優先
        EdgeSafe,           // 邊緣安全區域
        ColorAnalysis       // 顏色分析
    }

    public AIVisualAntiAFKForm()
    {
        InitializeComponent();
        timerAFK = new Timer();
        timerAFK.Tick += TimerAFK_Tick;
    }

    private void InitializeComponent()
    {
        this.Text = "🤖 AI智能暗黑不朽反掛機";
        this.Size = new Size(1000, 700);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;

        // 遊戲畫面顯示區域
        pictureBoxGameScreen = new PictureBox();
        pictureBoxGameScreen.Location = new Point(10, 10);
        pictureBoxGameScreen.Size = new Size(600, 400);
        pictureBoxGameScreen.BorderStyle = BorderStyle.FixedSingle;
        pictureBoxGameScreen.SizeMode = PictureBoxSizeMode.Zoom;
        pictureBoxGameScreen.Paint += PictureBoxGameScreen_Paint;
        this.Controls.Add(pictureBoxGameScreen);

        // 控制面板
        GroupBox groupControl = new GroupBox();
        groupControl.Text = "🎮 控制面板";
        groupControl.Location = new Point(620, 10);
        groupControl.Size = new Size(360, 200);
        this.Controls.Add(groupControl);

        // 截取畫面按鈕
        btnCapture = new Button();
        btnCapture.Text = "📷 截取遊戲畫面";
        btnCapture.Location = new Point(10, 20);
        btnCapture.Size = new Size(120, 30);
        btnCapture.Click += BtnCapture_Click;
        groupControl.Controls.Add(btnCapture);

        // AI分析按鈕
        btnAnalyze = new Button();
        btnAnalyze.Text = "🤖 AI分析位置";
        btnAnalyze.Location = new Point(140, 20);
        btnAnalyze.Size = new Size(120, 30);
        btnAnalyze.BackColor = Color.LightBlue;
        btnAnalyze.Click += BtnAnalyze_Click;
        groupControl.Controls.Add(btnAnalyze);

        // 分析策略選擇
        Label labelStrategy = new Label();
        labelStrategy.Text = "AI分析策略:";
        labelStrategy.Location = new Point(10, 60);
        labelStrategy.Size = new Size(80, 20);
        groupControl.Controls.Add(labelStrategy);

        comboBoxStrategy = new ComboBox();
        comboBoxStrategy.Location = new Point(100, 58);
        comboBoxStrategy.Size = new Size(150, 25);
        comboBoxStrategy.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBoxStrategy.Items.AddRange(new string[] {
            "安全空白區域",
            "避開UI元素", 
            "中心區域優先",
            "邊緣安全區域",
            "顏色分析"
        });
        comboBoxStrategy.SelectedIndex = 0;
        groupControl.Controls.Add(comboBoxStrategy);

        // 自動分析選項
        checkBoxAutoAnalyze = new CheckBox();
        checkBoxAutoAnalyze.Text = "每次截圖後自動分析";
        checkBoxAutoAnalyze.Location = new Point(10, 90);
        checkBoxAutoAnalyze.Size = new Size(150, 25);
        checkBoxAutoAnalyze.Checked = true;
        groupControl.Controls.Add(checkBoxAutoAnalyze);

        // 間隔時間設定
        Label labelInterval = new Label();
        labelInterval.Text = "間隔時間(秒):";
        labelInterval.Location = new Point(10, 120);
        labelInterval.Size = new Size(80, 20);
        groupControl.Controls.Add(labelInterval);

        numericInterval = new NumericUpDown();
        numericInterval.Location = new Point(100, 118);
        numericInterval.Size = new Size(80, 25);
        numericInterval.Minimum = 10;
        numericInterval.Maximum = 3600;
        numericInterval.Value = 60;
        groupControl.Controls.Add(numericInterval);

        // 滑鼠按鍵選擇
        checkBoxRightClick = new CheckBox();
        checkBoxRightClick.Text = "使用右鍵點擊";
        checkBoxRightClick.Location = new Point(190, 120);
        checkBoxRightClick.Size = new Size(100, 25);
        checkBoxRightClick.Checked = true;
        groupControl.Controls.Add(checkBoxRightClick);

        // 開始/停止按鈕
        btnStartAFK = new Button();
        btnStartAFK.Text = "▶️ 開始AI反掛機";
        btnStartAFK.Location = new Point(10, 150);
        btnStartAFK.Size = new Size(120, 30);
        btnStartAFK.BackColor = Color.LightGreen;
        btnStartAFK.Click += BtnStartAFK_Click;
        groupControl.Controls.Add(btnStartAFK);

        btnStopAFK = new Button();
        btnStopAFK.Text = "⏸️ 停止反掛機";
        btnStopAFK.Location = new Point(140, 150);
        btnStopAFK.Size = new Size(120, 30);
        btnStopAFK.BackColor = Color.LightCoral;
        btnStopAFK.Enabled = false;
        btnStopAFK.Click += BtnStopAFK_Click;
        groupControl.Controls.Add(btnStopAFK);

        // 分析結果面板
        GroupBox groupAnalysis = new GroupBox();
        groupAnalysis.Text = "🧠 AI分析結果";
        groupAnalysis.Location = new Point(620, 220);
        groupAnalysis.Size = new Size(360, 200);
        this.Controls.Add(groupAnalysis);

        labelAnalysisResult = new Label();
        labelAnalysisResult.Text = "等待AI分析...";
        labelAnalysisResult.Location = new Point(10, 20);
        labelAnalysisResult.Size = new Size(340, 40);
        groupAnalysis.Controls.Add(labelAnalysisResult);

        Label labelSafeZones = new Label();
        labelSafeZones.Text = "AI建議的安全點擊位置:";
        labelSafeZones.Location = new Point(10, 70);
        labelSafeZones.Size = new Size(200, 20);
        groupAnalysis.Controls.Add(labelSafeZones);

        listBoxSafeZones = new ListBox();
        listBoxSafeZones.Location = new Point(10, 95);
        listBoxSafeZones.Size = new Size(340, 90);
        groupAnalysis.Controls.Add(listBoxSafeZones);

        // 狀態面板
        GroupBox groupStatus = new GroupBox();
        groupStatus.Text = "📊 運行狀態";
        groupStatus.Location = new Point(620, 430);
        groupStatus.Size = new Size(360, 100);
        this.Controls.Add(groupStatus);

        labelStatus = new Label();
        labelStatus.Text = "狀態: 待機中";
        labelStatus.Location = new Point(10, 20);
        labelStatus.Size = new Size(340, 60);
        groupStatus.Controls.Add(labelStatus);

        // 說明區域
        GroupBox groupHelp = new GroupBox();
        groupHelp.Text = "💡 使用說明";
        groupHelp.Location = new Point(10, 420);
        groupHelp.Size = new Size(600, 110);
        this.Controls.Add(groupHelp);

        Label labelHelp = new Label();
        labelHelp.Text = "1. 點擊「截取遊戲畫面」獲取當前畫面\n" +
                        "2. 選擇AI分析策略，點擊「AI分析位置」\n" +
                        "3. AI會自動找到最佳點擊位置並標記\n" +
                        "4. 設定間隔時間，點擊「開始AI反掛機」\n" +
                        "5. AI會隨機選擇安全位置進行點擊";
        labelHelp.Location = new Point(10, 20);
        labelHelp.Size = new Size(580, 80);
        groupHelp.Controls.Add(labelHelp);

        // 尋找遊戲視窗
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
            }

            this.Invoke(new Action(() => {
                UpdateAnalysisResults();
            }));
        }
        catch (Exception ex)
        {
            this.Invoke(new Action(() => {
                labelAnalysisResult.Text = $"分析錯誤: {ex.Message}";
            }));
        }
    }

    private void FindSafeZones(Bitmap screenshot)
    {
        // 尋找大面積的相似顏色區域（通常是地面或背景）
        int width = screenshot.Width;
        int height = screenshot.Height;
        
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

        IntPtr hDC = GetDC(hWnd);
        IntPtr hMemDC = CreateCompatibleDC(hDC);
        IntPtr hBitmap = CreateCompatibleBitmap(hDC, width, height);
        IntPtr hOldBitmap = SelectObject(hMemDC, hBitmap);

        BitBlt(hMemDC, 0, 0, width, height, hDC, 0, 0, SRCCOPY);

        Bitmap bitmap = Image.FromHbitmap(hBitmap);

        SelectObject(hMemDC, hOldBitmap);
        DeleteObject(hBitmap);
        DeleteDC(hMemDC);
        ReleaseDC(hWnd, hDC);

        return bitmap;
    }

    private void PictureBoxGameScreen_Paint(object sender, PaintEventArgs e)
    {
        if (safeClickPositions.Count > 0 && pictureBoxGameScreen.Image != null)
        {
            float scaleX = (float)pictureBoxGameScreen.Width / pictureBoxGameScreen.Image.Width;
            float scaleY = (float)pictureBoxGameScreen.Height / pictureBoxGameScreen.Image.Height;

            using (Brush greenBrush = new SolidBrush(Color.FromArgb(150, Color.Green)))
            using (Pen redPen = new Pen(Color.Red, 2))
            {
                for (int i = 0; i < safeClickPositions.Count; i++)
                {
                    Point pos = safeClickPositions[i];
                    int displayX = (int)(pos.X * scaleX);
                    int displayY = (int)(pos.Y * scaleY);

                    // 繪製綠色圓圈
                    e.Graphics.FillEllipse(greenBrush, displayX - 8, displayY - 8, 16, 16);
                    e.Graphics.DrawEllipse(redPen, displayX - 8, displayY - 8, 16, 16);
                    
                    // 繪製數字
                    using (Font font = new Font("Arial", 8, FontStyle.Bold))
                    using (Brush textBrush = new SolidBrush(Color.White))
                    {
                        string text = (i + 1).ToString();
                        SizeF textSize = e.Graphics.MeasureString(text, font);
                        e.Graphics.DrawString(text, font, textBrush, 
                            displayX - textSize.Width / 2, displayY - textSize.Height / 2);
                    }
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