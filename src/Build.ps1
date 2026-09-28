param([string]$OutputDirectory = '')
$ErrorActionPreference = 'Stop'
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$target = if ($OutputDirectory) { $OutputDirectory } else { Join-Path $PSScriptRoot 'bin' }
New-Item -ItemType Directory -Force $target | Out-Null
$references = @('System.dll','System.Core.dll','System.Web.Extensions.dll','System.Xaml.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll') | ForEach-Object { '/reference:' + (Join-Path $framework $_) }
$sources = Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' | ForEach-Object FullName
$assets = Get-ChildItem (Join-Path $PSScriptRoot 'assets') -Filter '*.png' | ForEach-Object { '/resource:' + $_.FullName + ',' + $_.Name }
& (Join-Path $framework 'csc.exe') /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 ('/win32manifest:' + (Join-Path $PSScriptRoot 'app.manifest')) ('/out:' + (Join-Path $target 'ArcanaDuel.exe')) @references @assets @sources
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
Write-Output (Join-Path $target 'ArcanaDuel.exe')
