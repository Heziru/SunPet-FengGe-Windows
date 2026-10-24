$ErrorActionPreference = 'Stop'
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw '.NET Framework C# compiler not found.' }
$wpf = Join-Path $framework 'WPF'
$refs = @('PresentationFramework.dll', 'PresentationCore.dll', 'WindowsBase.dll') | ForEach-Object { '/reference:' + (Join-Path $wpf $_) }
$refs += @('System.Xaml.dll', 'System.Web.Extensions.dll', 'System.Windows.Forms.dll', 'System.Drawing.dll') | ForEach-Object { '/reference:' + (Join-Path $framework $_) }
& $compiler /nologo /optimize+ /target:winexe /platform:anycpu /utf8output "/out:$PSScriptRoot\SunPet.exe" "/win32manifest:$PSScriptRoot\app.manifest" "/win32icon:$PSScriptRoot\SunPet.ico" @refs "$PSScriptRoot\SunPet.cs"
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
@'
<?xml version="1.0" encoding="utf-8"?>
<configuration><startup><supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.8" /></startup></configuration>
'@ | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'SunPet.exe.config') -Encoding UTF8
Write-Output "Built: $PSScriptRoot\SunPet.exe"
