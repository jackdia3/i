using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

public partial class VisualAntiAFKForm : Form
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
    private Button btnStartAFK;
    private Button btnStopAFK;
    private Label labelStatus;
    private Label labelClickPosition;
    private NumericUpDown numericInterval;
    private CheckBox checkBoxRightClick;
    private Timer timerAFK;
    private IntPtr gameWindow;
    private Point clickPosition = new Point(-1, -1);
    private bool isAFKRunning = false;

    public VisualAntiAFKForm()
    {
        InitializeComponent();
        timerAFK = new Timer();
        timerAFK.Tick += TimerAFK_Tick;
    }

    private void InitializeComponent()
    {
        this.Text = "🎮 視覺化暗黑不朽反掛機";
        this.Size = new Size(800, 600);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;

        // 遊戲畫面顯示區域
        pictureBoxGameScreen = new PictureBox();
        pictureBoxGameScreen.Location = new Point(10, 10);
        pictureBoxGameScreen.Size = new Size(600, 400);
        pictureBoxGameScreen.BorderStyle = BorderStyle.FixedSingle;
        pictureBoxGameScreen.SizeMode = PictureBoxSizeMode.Zoom;
        pictureBoxGameScreen.MouseClick += PictureBoxGameScreen_MouseClick;
        pictureBoxGameScreen.Paint += PictureBoxGameScreen_Paint;
        this.Controls.Add(pictureBoxGameScreen);

        // 截取畫面按鈕
        btnCapture = new Button();
        btnCapture.Text = "📷 截取遊戲畫面";
        btnCapture.Location = new Point(620, 10);
        btnCapture.Size = new Size(120, 30);
        btnCapture.Click += BtnCapture_Click;
        this.Controls.Add(btnCapture);

        // 間隔時間設定
        Label labelInterval = new Label();
        labelInterval.Text = "間隔時間(秒):";
        labelInterval.Location = new Point(620, 50);
        labelInterval.Size = new Size(100, 20);
        this.Controls.Add(labelInterval);

        numericInterval = new NumericUpDown();
        numericInterval.Location = new Point(620, 70);
        numericInterval.Size = new Size(120, 25);
        numericInterval.Minimum = 5;
        numericInterval.Maximum = 3600;
        numericInterval.Value = 30;
        this.Controls.Add(numericInterval);

        // 滑鼠按鍵選擇
        checkBoxRightClick = new CheckBox();
        checkBoxRightClick.Text = "使用右鍵點擊";
        checkBoxRightClick.Location = new Point(620, 100);
        checkBoxRightClick.Size = new Size(120, 25);
        checkBoxRightClick.Checked = true;
        this.Controls.Add(checkBoxRightClick);

        // 開始反掛機按鈕
        btnStartAFK = new Button();
        btnStartAFK.Text = "▶️ 開始反掛機";
        btnStartAFK.Location = new Point(620, 130);
        btnStartAFK.Size = new Size(120, 30);
        btnStartAFK.BackColor = Color.LightGreen;
        btnStartAFK.Click += BtnStartAFK_Click;
        this.Controls.Add(btnStartAFK);

        // 停止反掛機按鈕
        btnStopAFK = new Button();
        btnStopAFK.Text = "⏸️ 停止反掛機";
        btnStopAFK.Location = new Point(620, 170);
        btnStopAFK.Size = new Size(120, 30);
        btnStopAFK.BackColor = Color.LightCoral;
        btnStopAFK.Enabled = false;
        btnStopAFK.Click += BtnStopAFK_Click;
        this.Controls.Add(btnStopAFK);

        // 狀態標籤
        labelStatus = new Label();
        labelStatus.Text = "狀態: 待機中";
        labelStatus.Location = new Point(620, 210);
        labelStatus.Size = new Size(120, 20);
        this.Controls.Add(labelStatus);

        // 點擊位置標籤
        labelClickPosition = new Label();
        labelClickPosition.Text = "點擊位置: 未設定";
        labelClickPosition.Location = new Point(620, 240);
        labelClickPosition.Size = new Size(120, 40);
        this.Controls.Add(labelClickPosition);

        // 說明標籤
        Label labelInstructions = new Label();
        labelInstructions.Text = "使用說明:\n1. 點擊「截取遊戲畫面」\n2. 在圖片上點擊選擇位置\n3. 設定間隔時間\n4. 開始反掛機";
        labelInstructions.Location = new Point(620, 290);
        labelInstructions.Size = new Size(150, 100);
        this.Controls.Add(labelInstructions);

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

    private void BtnCapture_Click(object sender, EventArgs e)
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
        }
        else
        {
            MessageBox.Show("截取畫面失敗！", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

    private void PictureBoxGameScreen_MouseClick(object sender, MouseEventArgs e)
    {
        if (pictureBoxGameScreen.Image == null) return;

        // 計算實際遊戲座標
        float scaleX = (float)pictureBoxGameScreen.Image.Width / pictureBoxGameScreen.Width;
        float scaleY = (float)pictureBoxGameScreen.Image.Height / pictureBoxGameScreen.Height;

        clickPosition = new Point(
            (int)(e.X * scaleX),
            (int)(e.Y * scaleY)
        );

        labelClickPosition.Text = $"點擊位置:\nX: {clickPosition.X}\nY: {clickPosition.Y}";
        pictureBoxGameScreen.Invalidate(); // 重繪以顯示點擊標記
    }

    private void PictureBoxGameScreen_Paint(object sender, PaintEventArgs e)
    {
        if (clickPosition.X >= 0 && clickPosition.Y >= 0 && pictureBoxGameScreen.Image != null)
        {
            // 計算顯示座標
            float scaleX = (float)pictureBoxGameScreen.Width / pictureBoxGameScreen.Image.Width;
            float scaleY = (float)pictureBoxGameScreen.Height / pictureBoxGameScreen.Image.Height;

            int displayX = (int)(clickPosition.X * scaleX);
            int displayY = (int)(clickPosition.Y * scaleY);

            // 繪製紅色十字標記
            using (Pen pen = new Pen(Color.Red, 2))
            {
                e.Graphics.DrawLine(pen, displayX - 10, displayY, displayX + 10, displayY);
                e.Graphics.DrawLine(pen, displayX, displayY - 10, displayX, displayY + 10);
            }
        }
    }

    private void BtnStartAFK_Click(object sender, EventArgs e)
    {
        if (clickPosition.X < 0 || clickPosition.Y < 0)
        {
            MessageBox.Show("請先截取畫面並選擇點擊位置！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        isAFKRunning = true;
        timerAFK.Interval = (int)(numericInterval.Value * 1000);
        timerAFK.Start();

        btnStartAFK.Enabled = false;
        btnStopAFK.Enabled = true;
        labelStatus.Text = "狀態: 反掛機運行中";
    }

    private void BtnStopAFK_Click(object sender, EventArgs e)
    {
        isAFKRunning = false;
        timerAFK.Stop();

        btnStartAFK.Enabled = true;
        btnStopAFK.Enabled = false;
        labelStatus.Text = "狀態: 反掛機已停止";
    }

    private void TimerAFK_Tick(object sender, EventArgs e)
    {
        if (!isAFKRunning || gameWindow == IntPtr.Zero) return;

        PerformClick();
    }

    private void PerformClick()
    {
        IntPtr lParam = (IntPtr)((clickPosition.Y << 16) | (clickPosition.X & 0xFFFF));

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

        labelStatus.Text = $"狀態: 已點擊 ({DateTime.Now:HH:mm:ss})";
    }
}

class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new VisualAntiAFKForm());
    }
} 