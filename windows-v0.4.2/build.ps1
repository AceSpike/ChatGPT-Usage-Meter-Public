$ErrorActionPreference='Stop'
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$output=Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Force -Path $output | Out-Null
Push-Location (Join-Path $PSScriptRoot 'src')
try {
 & $compiler /nologo /codepage:65001 /target:winexe /main:FloatingProgram /win32icon:App.ico /win32manifest:UsageTray.manifest ("/out:"+(Join-Path $output 'ChatGPTUsageMeter.exe')) /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll SkinSupport.cs CodexTrayDisplay.cs CodexCleanTray.cs FloatingUsage.cs UsageTray.cs LiveUsage.cs QuotaDisplay.cs DashboardHorizontal.cs RuntimeChecks.cs
 if($LASTEXITCODE -ne 0){throw 'Build failed'}
}finally{Pop-Location}
