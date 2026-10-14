@echo off
echo ==========================================
echo DANG BUILD MOD NGOC RONG 2.5.0...
echo ==========================================
cd /d "%~dp0src"
dotnet build Assembly-CSharp.csproj -c Debug
if %ERRORLEVEL% EQU 0 (
    echo ==========================================
    echo BUILD THANH CONG! DANG COPY DLL VAO GAME...
    echo ==========================================
    copy /Y "bin\Debug\net35\Assembly-CSharp.dll" "..\DragonBoy250_pc\DragonBoy250_Data\Managed\Assembly-CSharp.dll"
    echo DA CAP NHAT BAN MOD VAO GAME XONG!
) else (
    echo BUILD THAT BAI! VUI LONG KIEM TRA LOI.
)
pause
