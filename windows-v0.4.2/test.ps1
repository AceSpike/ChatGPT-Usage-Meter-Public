param([switch]$LiveDesktop,[switch]$LiveForm,[switch]$Skins)
$ErrorActionPreference='Stop'
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$output=Join-Path $PSScriptRoot 'test-output'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$sources=@(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -File -Filter '*.cs' | Select-Object -ExpandProperty FullName)
$manifest=Join-Path $PSScriptRoot 'src\UsageTray.manifest'
$entries=if($LiveDesktop){@('CliDiscoveryChecks')}elseif($LiveForm){@('BilingualChecks')}elseif($Skins){@('SkinChecks')}else{@('CliDiscoveryChecks','CleanTrayTests','BilingualChecks')}
Push-Location $output
try {
 foreach($entry in $entries){
  $exe=Join-Path $output ($entry+'.exe')
  & $compiler /nologo /codepage:65001 /target:exe ("/main:"+$entry) ("/out:"+$exe) ("/win32manifest:"+$manifest) /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll $sources
  if($LASTEXITCODE -ne 0){throw "$entry compilation failed"}
  if($LiveDesktop){& $exe --live-desktop}elseif($LiveForm){& $exe --live}else{& $exe}
  if($LASTEXITCODE -ne 0){throw "$entry failed"}
 }
}finally{Pop-Location}
