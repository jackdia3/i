using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

class Program
{
    [DllImport("user32.dll")]
    static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

    const uint KEYEVENTF_KEYUP = 0x0002;
    const byte VK_MENU = 0x12; // Alt鍵

    static void Main()
    {
        Console.WriteLine("🎮 暗黑不朽反掛機程式已啟動 - 每3分鐘按下Alt鍵...");
        Console.WriteLine("按 Ctrl+C 結束程式");
        Console.WriteLine("此版本較適合全螢幕遊戲，不會移動滑鼠游標");
        
        while (true)
        {
            try
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 按下Alt鍵...");
                
                // 按下Alt鍵
                keybd_event(VK_MENU, 0, 0, 0);
                Thread.Sleep(50);
                // 放開Alt鍵
                keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, 0);
                
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 已執行按鍵操作，等待3分鐘...");
                Thread.Sleep(3 * 60 * 1000);    // 等待3分鐘
            }
            catch (Exception ex)
            {
                Console.WriteLine($"錯誤: {ex.Message}");
                Thread.Sleep(1000);
            }
        }
    }
} 