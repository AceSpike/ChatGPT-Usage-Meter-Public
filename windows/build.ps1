$ErrorActionPreference='Stop'
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$source=Join-Path $PSScriptRoot 'src'
$bin=Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Path $bin -Force | Out-Null
Push-Location $source
try {
    & $compiler /nologo /codepage:65001 /target:winexe /main:FloatingProgram /out:../bin/ChatGPTUsageMeter.exe /win32manifest:UsageTray.manifest /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll FloatingUsage.cs UsageTray.cs LiveUsage.cs QuotaDisplay.cs DashboardHorizontal.cs RuntimeChecks.cs
    if($LASTEXITCODE -ne 0){throw 'Floating build failed'}
} finally { Pop-Location }
