$ErrorActionPreference='Stop'
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$source=Join-Path $PSScriptRoot 'src'
$bin=Join-Path $PSScriptRoot 'bin'
$output=Join-Path $PSScriptRoot 'test-output'
New-Item -ItemType Directory -Path $bin,$output -Force | Out-Null
Push-Location $source
try {
    & $compiler /nologo /codepage:65001 /target:exe /main:BilingualChecks /out:../bin/BilingualChecks.exe /win32manifest:UsageTray.manifest /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll BilingualChecks.cs FloatingUsage.cs UsageTray.cs LiveUsage.cs QuotaDisplay.cs DashboardHorizontal.cs RuntimeChecks.cs
    if($LASTEXITCODE -ne 0){throw 'Test build failed'}
    Push-Location $output
    try { & (Join-Path $bin 'BilingualChecks.exe') } finally { Pop-Location }
    if($LASTEXITCODE -ne 0){throw 'Bilingual UI checks failed'}
} finally { Pop-Location }
