@echo off
if exist "%~dp0lib\DragonCore.dll" (
    if not exist "%~dp0DragonBoy250_pc\DragonBoy250_Data\Managed\DragonCore.dll" (
        copy /Y "%~dp0lib\DragonCore.dll" "%~dp0DragonBoy250_pc\DragonBoy250_Data\Managed\DragonCore.dll" >nul 2>&1
    )
)
start "" "%~dp0DragonBoy250_pc\DragonBoy250.exe"
