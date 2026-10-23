@echo off
echo ===================================================
echo   BIEN DICH QLTK_DragonBoy (MA NGUON MO SACH 100%%)
echo ===================================================
echo.

set CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe

if not exist "%CSC%" (
    echo [LOI] Khong tim thay trinh bien dich csc.exe tai %CSC%
    pause
    exit /b
)

echo Dang bien dich QLTK_DragonBoy.cs thanh QLTK_DragonBoy.exe...
"%CSC%" /target:winexe /optimize+ /platform:anycpu /out:QLTK_DragonBoy.exe QLTK_DragonBoy.cs

if %ERRORLEVEL% equ 0 (
    echo.
    echo ===================================================
    echo [THANH CONG] Da tao file QLTK_DragonBoy.exe thanh cong!
    echo ===================================================
) else (
    echo.
    echo [LOI] Qua trinh bien dich gap su co!
)

echo.
pause
