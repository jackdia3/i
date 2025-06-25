using System;
using System.Runtime.InteropServices;
using System.Threading;

class Program
{
    [DllImport("user32.dll")]
    static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    static extern bool GetCursorPos(out POINT lpPoint);

    struct POINT
    {
        public int X;
        public int Y;
    }

    static void Main()
    {
        Console.WriteLine("🌀 暗黑不朽反掛機程式已啟動 - 每3分鐘輕微移動滑鼠...");
        Console.WriteLine("按 Ctrl+C 結束程式");
        
        while (true)
        {
            try
            {
                GetCursorPos(out POINT pos);
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 目前滑鼠位置: ({pos.X}, {pos.Y})");
                
                SetCursorPos(pos.X + 1, pos.Y); // 輕微移動
                Thread.Sleep(100);
                SetCursorPos(pos.X, pos.Y);     // 移回原位
                
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 已執行微移動，等待3分鐘...");
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