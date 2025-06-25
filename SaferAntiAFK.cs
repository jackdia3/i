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
    static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

    struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    // 鍵盤常數
    const uint KEYEVENTF_KEYUP = 0x0002;
    const byte VK_SHIFT = 0x10;
    const byte VK_CONTROL = 0x11;
    const byte VK_MENU = 0x12; // Alt
    
    // 滑鼠訊息常數
    const uint WM_RBUTTONDOWN = 0x0204;
    const uint WM_RBUTTONUP = 0x0205;
    const uint WM_LBUTTONDOWN = 0x0201;
    const uint WM_LBUTTONUP = 0x0202;

    static Random random = new Random();

    static void Main()
    {
        Console.WriteLine("🛡️ 更安全的暗黑不朽反掛機程式");
        Console.WriteLine("🔒 使用隨機間隔和多種操作模式降低偵測風險");
        Console.WriteLine("按 Ctrl+C 結束程式");
        Console.WriteLine();
        Console.WriteLine("⚠️  風險提醒：");
        Console.WriteLine("   - 任何自動化工具都有被偵測的風險");
        Console.WriteLine("   - 建議搭配手動操作使用");
        Console.WriteLine("   - 使用前請備份遊戲帳號");
        Console.WriteLine();
        
        while (true)
        {
            try
            {
                // 隨機間隔：5-15分鐘之間
                int randomInterval = random.Next(5 * 60, 15 * 60 + 1);
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 等待 {randomInterval / 60.0:F1} 分鐘後執行下一次操作...");
                
                Thread.Sleep(randomInterval * 1000);
                
                IntPtr gameWindow = FindDiabloImmortalWindow();
                
                if (gameWindow != IntPtr.Zero)
                {
                    // 隨機選擇操作類型
                    int actionType = random.Next(1, 4);
                    
                    switch (actionType)
                    {
                        case 1:
                            PerformKeyboardAction();
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 執行鍵盤操作");
                            break;
                        case 2:
                            PerformMouseAction(gameWindow);
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 執行滑鼠操作");
                            break;
                        case 3:
                            PerformMinimalAction();
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 執行最小化操作");
                            break;
                    }
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 未找到遊戲視窗，跳過此次操作");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"錯誤: {ex.Message}");
                Thread.Sleep(5000);
            }
        }
    }

    // 鍵盤操作 - 使用不影響遊戲的按鍵
    static void PerformKeyboardAction()
    {
        byte[] safKeys = { VK_SHIFT, VK_CONTROL, VK_MENU };
        byte selectedKey = safKeys[random.Next(safKeys.Length)];
        
        // 隨機按住時間 50-200毫秒
        int holdTime = random.Next(50, 201);
        
        keybd_event(selectedKey, 0, 0, 0);
        Thread.Sleep(holdTime);
        keybd_event(selectedKey, 0, KEYEVENTF_KEYUP, 0);
    }

    // 滑鼠操作 - 點擊遊戲視窗的隨機安全區域
    static void PerformMouseAction(IntPtr gameWindow)
    {
        if (!GetWindowRect(gameWindow, out RECT rect))
            return;

        int windowWidth = rect.Right - rect.Left;
        int windowHeight = rect.Bottom - rect.Top;
        
        // 避開邊緣，在中心區域的30%-70%範圍內隨機點擊
        int safeZoneStartX = (int)(windowWidth * 0.3);
        int safeZoneEndX = (int)(windowWidth * 0.7);
        int safeZoneStartY = (int)(windowHeight * 0.3);
        int safeZoneEndY = (int)(windowHeight * 0.7);
        
        int randomX = random.Next(safeZoneStartX, safeZoneEndX);
        int randomY = random.Next(safeZoneStartY, safeZoneEndY);
        
        IntPtr lParam = (IntPtr)((randomY << 16) | (randomX & 0xFFFF));
        
        // 隨機選擇左鍵或右鍵
        if (random.Next(2) == 0)
        {
            PostMessage(gameWindow, WM_LBUTTONDOWN, IntPtr.Zero, lParam);
            Thread.Sleep(random.Next(30, 100));
            PostMessage(gameWindow, WM_LBUTTONUP, IntPtr.Zero, lParam);
        }
        else
        {
            PostMessage(gameWindow, WM_RBUTTONDOWN, IntPtr.Zero, lParam);
            Thread.Sleep(random.Next(30, 100));
            PostMessage(gameWindow, WM_RBUTTONUP, IntPtr.Zero, lParam);
        }
    }

    // 最小化操作 - 僅按鍵盤，最不容易被偵測
    static void PerformMinimalAction()
    {
        // 短暫按下Shift鍵
        keybd_event(VK_SHIFT, 0, 0, 0);
        Thread.Sleep(random.Next(20, 50));
        keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, 0);
    }

    static IntPtr FindDiabloImmortalWindow()
    {
        IntPtr window = IntPtr.Zero;
        
        string[] possibleTitles = {
            "Diablo Immortal",
            "暗黑不朽",
            "DiabloImmortal"
        };
        
        foreach (string title in possibleTitles)
        {
            window = FindWindow(null, title);
            if (window != IntPtr.Zero && IsWindowVisible(window))
            {
                return window;
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
                        return process.MainWindowHandle;
                    }
                }
            }
            catch
            {
                continue;
            }
        }
        
        return IntPtr.Zero;
    }
} 