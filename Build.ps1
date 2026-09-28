param([string]$OutputDirectory = '')
$ErrorActionPreference = 'Stop'
$destination = if ($OutputDirectory) { $OutputDirectory } else { Join-Path $PSScriptRoot 'build' }
& (Join-Path $PSScriptRoot 'src/Build.ps1') -OutputDirectory $destination
