; 暗黑不朽反掛機腳本 - AutoHotkey版本
; 每10秒找到暗黑不朽進程並發送滑鼠右鍵點擊

#NoEnv
#SingleInstance Force
#Persistent

; 設定參數
IntervalSeconds := 10  ; 間隔秒數

; 可能的遊戲視窗標題和進程名稱
GameTitles := ["Diablo Immortal", "暗黑不朽", "DiabloImmortal"]
GameProcesses := ["diablo", "immortal"]

; 建立系統托盤圖示
Menu, Tray, Tip, 暗黑不朽反掛機(右鍵版) - 運行中
Menu, Tray, Add, 暫停/恢復, TogglePause
Menu, Tray, Add, 測試右鍵, TestRightClick
Menu, Tray, Add, 設定間隔, SetInterval
Menu, Tray, Add, 退出, ExitApp
Menu, Tray, Default, 暫停/恢復

; 顯示啟動訊息
TrayTip, 反掛機程式, 暗黑不朽反掛機(右鍵版)已啟動`n間隔: %IntervalSeconds%秒`n操作: 滑鼠右鍵點擊, 3

; 設定定時器
SetTimer, AntiAFK, % IntervalSeconds * 1000

; 熱鍵設定
F9::Gosub, TogglePause    ; F9 暫停/恢復
F10::ExitApp              ; F10 退出
F11::Gosub, SetInterval   ; F11 設定間隔
F12::Gosub, TestRightClick ; F12 測試右鍵

; 主要反掛機函數
AntiAFK:
    GameWindow := FindDiabloImmortalWindow()
    
    if (GameWindow) {
        ; 獲取視窗位置和大小
        WinGetPos, WinX, WinY, WinW, WinH, ahk_id %GameWindow%
        
        ; 計算視窗中心點
        CenterX := WinW // 2
        CenterY := WinH // 2
        
        ; 激活視窗並發送右鍵點擊
        WinActivate, ahk_id %GameWindow%
        Sleep, 100
        
        ; 發送右鍵點擊到視窗中心
        ControlClick, , ahk_id %GameWindow%, , Right, 1, x%CenterX% y%CenterY%
        
        ; 顯示執行訊息
        FormatTime, CurrentTime, , HH:mm:ss
        TrayTip, 反掛機執行, [%CurrentTime%] 已向遊戲視窗發送右鍵點擊, 1
    } else {
        FormatTime, CurrentTime, , HH:mm:ss
        TrayTip, 反掛機執行, [%CurrentTime%] 未找到暗黑不朽遊戲視窗, 2
    }
return

; 找到暗黑不朽遊戲視窗
FindDiabloImmortalWindow() {
    ; 先嘗試通過視窗標題找到
    Loop % GameTitles.Length() {
        Title := GameTitles[A_Index]
        WinGet, WindowID, ID, %Title%
        if (WindowID) {
            return WindowID
        }
    }
    
    ; 如果標題找不到，嘗試通過進程名稱找到
    WinGet, AllWindows, List
    Loop %AllWindows% {
        WindowID := AllWindows%A_Index%
        WinGet, ProcessName, ProcessName, ahk_id %WindowID%
        
        Loop % GameProcesses.Length() {
            GameProcess := GameProcesses[A_Index]
            if (InStr(ProcessName, GameProcess) > 0) {
                ; 確認視窗是可見的
                WinGet, WindowState, MinMax, ahk_id %WindowID%
                if (WindowState != -1) {  ; 不是最小化的
                    return WindowID
                }
            }
        }
    }
    
    return 0
}

; 暫停/恢復功能
TogglePause:
    if (A_IsPaused) {
        Pause, Off
        Menu, Tray, Rename, 暫停/恢復, 暫停
        TrayTip, 反掛機程式, 已恢復運行, 2
    } else {
        Pause, On
        Menu, Tray, Rename, 暫停, 恢復
        TrayTip, 反掛機程式, 已暫停, 2
    }
return

; 測試右鍵點擊
TestRightClick:
    Gosub, AntiAFK
return

; 設定間隔時間
SetInterval:
    InputBox, NewInterval, 設定間隔, 請輸入新的間隔時間(秒):, , 250, 120, , , , , %IntervalSeconds%
    if (ErrorLevel = 0 && NewInterval > 0) {
        IntervalSeconds := NewInterval
        SetTimer, AntiAFK, Off
        SetTimer, AntiAFK, % IntervalSeconds * 1000
        TrayTip, 設定更新, 新間隔: %IntervalSeconds%秒, 2
        Menu, Tray, Tip, 暗黑不朽反掛機(右鍵版) - 運行中 (%IntervalSeconds%秒)
    }
return

; 退出程式
ExitApp:
    TrayTip, 反掛機程式, 程式已退出, 1
    Sleep, 1000
    ExitApp
return 