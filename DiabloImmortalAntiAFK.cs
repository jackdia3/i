using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

class Program
{
    [DllImport("user32.dll")]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    static extern bool SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    static extern int GetWindowTextLength(IntPtr hWnd);

    struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    // 滑鼠訊息常數
    const uint WM_RBUTTONDOWN = 0x0204;
    const uint WM_RBUTTONUP = 0x0205;
    const uint MK_RBUTTON = 0x0002;

    static void Main()
    {
        Console.WriteLine("🎮 暗黑不朽反掛機程式已啟動 - 每10秒對遊戲視窗發送右鍵點擊...");
        Console.WriteLine("按 Ctrl+C 結束程式");
        Console.WriteLine("支援的遊戲進程名稱: DiabloImmortal, Diablo Immortal");
        
        while (true)
        {
            try
            {
                IntPtr gameWindow = FindDiabloImmortalWindow();
                
                if (gameWindow != IntPtr.Zero)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 找到暗黑不朽視窗");
                    
                    // 獲取視窗矩形
                    if (GetWindowRect(gameWindow, out RECT rect))
                    {
                        // 計算視窗中心點
                        int centerX = (rect.Left + rect.Right) / 2;
                        int centerY = (rect.Top + rect.Bottom) / 2;
                        
                        // 將螢幕座標轉換為視窗內部座標
                        int windowX = centerX - rect.Left;
                        int windowY = centerY - rect.Top;
                        
                        // 發送滑鼠右鍵點擊
                        IntPtr lParam = (IntPtr)((windowY << 16) | (windowX & 0xFFFF));
                        
                        PostMessage(gameWindow, WM_RBUTTONDOWN, (IntPtr)MK_RBUTTON, lParam);
                        Thread.Sleep(50);
                        PostMessage(gameWindow, WM_RBUTTONUP, IntPtr.Zero, lParam);
                        
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 已向遊戲視窗發送右鍵點擊 ({windowX}, {windowY})");
                    }
                    else
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 無法獲取視窗矩形");
                    }
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 未找到暗黑不朽遊戲視窗");
                }
                
                Thread.Sleep(10 * 1000); // 等待10秒
            }
            catch (Exception ex)
            {
                Console.WriteLine($"錯誤: {ex.Message}");
                Thread.Sleep(1000);
            }
        }
    }

    static IntPtr FindDiabloImmortalWindow()
    {
        IntPtr window = IntPtr.Zero;
        
        // 可能的遊戲視窗標題
        string[] possibleTitles = {
            "Diablo Immortal",
            "暗黑不朽",
            "DiabloImmortal"
        };
        
        // 嘗試通過視窗標題找到遊戲
        foreach (string title in possibleTitles)
        {
            window = FindWindow(null, title);
            if (window != IntPtr.Zero && IsWindowVisible(window))
            {
                return window;
            }
        }
        
        // 如果通過標題找不到，嘗試通過進程名稱找到
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
                        // 獲取視窗標題確認
                        int length = GetWindowTextLength(process.MainWindowHandle);
                        if (length > 0)
                        {
                            System.Text.StringBuilder sb = new System.Text.StringBuilder(length + 1);
                            GetWindowText(process.MainWindowHandle, sb, sb.Capacity);
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 找到進程: {process.ProcessName}, 視窗標題: {sb.ToString()}");
                        }
                        return process.MainWindowHandle;
                    }
                }
            }
            catch (Exception ex)
            {
                // 忽略無法存取的進程
                continue;
            }
        }
        
        return IntPtr.Zero;
    }
} 