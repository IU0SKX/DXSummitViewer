@echo off
rem Compila DXSummitViewer.exe con il compilatore C# incluso in .NET Framework 4.x
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
"%CSC%" /nologo /target:winexe /optimize+ /out:DXSummitViewer.exe ^
  /win32icon:app.ico /resource:app.ico,DXSummitViewer.app.ico ^
  /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Web.Extensions.dll ^
  DXSummitViewer.cs UiControls.cs
