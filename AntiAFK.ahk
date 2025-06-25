; 暗黑不朽反掛機腳本 - AutoHotkey版本
; 每3分鐘輕微移動滑鼠，避免被判定閒置

#NoEnv
#SingleInstance Force
#Persistent

; 設定參數
IntervalMinutes := 3  ; 間隔分鐘數
MovePixels := 1       ; 移動像素數

; 建立系統托盤圖示
Menu, Tray, Tip, 暗黑不朽反掛機 - 運行中
Menu, Tray, Add, 暫停/恢復, TogglePause
Menu, Tray, Add, 退出, ExitApp
Menu, Tray, Default, 暫停/恢復

; 顯示啟動訊息
TrayTip, 反掛機程式, 暗黑不朽反掛機已啟動`n間隔: %IntervalMinutes%分鐘`n右鍵托盤圖示可控制, 3

; 設定定時器
SetTimer, AntiAFK, % IntervalMinutes * 60 * 1000

; 熱鍵設定 (可選)
F9::Gosub, TogglePause  ; F9 暫停/恢復
F10::ExitApp            ; F10 退出

; 主要反掛機函數
AntiAFK:
    ; 獲取目前滑鼠位置
    MouseGetPos, CurrentX, CurrentY
    
    ; 輕微移動滑鼠
    MouseMove, %CurrentX%+%MovePixels%, %CurrentY%, 0
    Sleep, 100
    MouseMove, %CurrentX%, %CurrentY%, 0
    
    ; 顯示執行訊息
    FormatTime, CurrentTime, , HH:mm:ss
    TrayTip, 反掛機執行, [%CurrentTime%] 已執行微移動, 1
return

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

; 退出程式
ExitApp:
    TrayTip, 反掛機程式, 程式已退出, 1
    Sleep, 1000
    ExitApp
return 