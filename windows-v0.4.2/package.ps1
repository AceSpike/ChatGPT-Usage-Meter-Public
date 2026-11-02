$ErrorActionPreference='Stop'
& (Join-Path $PSScriptRoot 'build.ps1')
$exe=Join-Path $PSScriptRoot 'bin\ChatGPTUsageMeter.exe'
if((Get-Item -LiteralPath $exe).VersionInfo.FileVersion -ne '0.4.2.0'){throw 'Unexpected EXE version'}
$stage=Join-Path $PSScriptRoot 'package-staging'
$downloads=Join-Path $PSScriptRoot 'downloads'
New-Item -ItemType Directory -Force -Path $stage,$downloads | Out-Null
Copy-Item -LiteralPath $exe -Destination (Join-Path $stage 'ChatGPTUsageMeter.exe') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'PACKAGE-README.txt') -Destination (Join-Path $stage 'README.txt') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '..\LICENSE') -Destination (Join-Path $stage 'LICENSE') -Force
$exeLine=(Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant()+'  ChatGPTUsageMeter.exe'
[IO.File]::WriteAllText((Join-Path $stage 'SHA256SUMS.txt'),$exeLine+"`n",[Text.UTF8Encoding]::new($false))
$zip=Join-Path $downloads 'ChatGPTUsageMeter-v0.4.2-Windows.zip'
Compress-Archive -LiteralPath @((Join-Path $stage 'ChatGPTUsageMeter.exe'),(Join-Path $stage 'README.txt'),(Join-Path $stage 'LICENSE'),(Join-Path $stage 'SHA256SUMS.txt')) -DestinationPath $zip -Force
$zipLine=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()+'  ChatGPTUsageMeter-v0.4.2-Windows.zip'
[IO.File]::WriteAllText((Join-Path $downloads 'SHA256SUMS.txt'),$zipLine+"`n",[Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText((Join-Path $downloads 'EXE-SHA256SUMS.txt'),$exeLine+"`n",[Text.UTF8Encoding]::new($false))
Write-Output $exeLine
Write-Output $zipLine
