; 暗黑不朽反掛機腳本 - AutoHotkey鍵盤版本
; 每3分鐘按下Alt鍵，避免被判定閒置（適合全螢幕遊戲）

#NoEnv
#SingleInstance Force
#Persistent

; 設定參數
IntervalMinutes := 3  ; 間隔分鐘數

; 建立系統托盤圖示
Menu, Tray, Tip, 暗黑不朽反掛機(鍵盤版) - 運行中
Menu, Tray, Add, 暫停/恢復, TogglePause
Menu, Tray, Add, 設定間隔, SetInterval
Menu, Tray, Add, 退出, ExitApp
Menu, Tray, Default, 暫停/恢復

; 顯示啟動訊息
TrayTip, 反掛機程式, 暗黑不朽反掛機(鍵盤版)已啟動`n間隔: %IntervalMinutes%分鐘`n按鍵: Alt`n右鍵托盤圖示可控制, 3

; 設定定時器
SetTimer, AntiAFK, % IntervalMinutes * 60 * 1000

; 熱鍵設定
F9::Gosub, TogglePause  ; F9 暫停/恢復
F10::ExitApp            ; F10 退出
F11::Gosub, SetInterval ; F11 設定間隔

; 主要反掛機函數
AntiAFK:
    ; 按下Alt鍵
    Send, {Alt}
    
    ; 顯示執行訊息
    FormatTime, CurrentTime, , HH:mm:ss
    TrayTip, 反掛機執行, [%CurrentTime%] 已按下Alt鍵, 1
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

; 設定間隔時間
SetInterval:
    InputBox, NewInterval, 設定間隔, 請輸入新的間隔時間(分鐘):, , 250, 120, , , , , %IntervalMinutes%
    if (ErrorLevel = 0 && NewInterval > 0) {
        IntervalMinutes := NewInterval
        SetTimer, AntiAFK, Off
        SetTimer, AntiAFK, % IntervalMinutes * 60 * 1000
        TrayTip, 設定更新, 新間隔: %IntervalMinutes%分鐘, 2
        Menu, Tray, Tip, 暗黑不朽反掛機(鍵盤版) - 運行中 (%IntervalMinutes%分鐘)
    }
return

; 退出程式
ExitApp:
    TrayTip, 反掛機程式, 程式已退出, 1
    Sleep, 1000
    ExitApp
return 