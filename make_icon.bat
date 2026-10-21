@echo off
rem Rigenera app.ico da MakeIcon.cs (serve solo se si vuole cambiare l'icona)
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
"%CSC%" /nologo /out:MakeIcon.exe /r:System.Drawing.dll MakeIcon.cs && .\MakeIcon.exe app.ico && del MakeIcon.exe
