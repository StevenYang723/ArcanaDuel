param([switch]$RulesOnly)
$ErrorActionPreference = 'Stop'
$destination = Join-Path $PSScriptRoot 'build'
& (Join-Path $PSScriptRoot 'Build.ps1') -OutputDirectory $destination
$exe = Join-Path $destination 'ArcanaDuel.exe'
$process = Start-Process -FilePath $exe -ArgumentList '--test' -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) { throw "Rule checks failed; inspect $destination/test-results.txt" }
Get-Content (Join-Path $destination 'test-results.txt') -Tail 1
if (!$RulesOnly) {
    $screenshots = Join-Path $destination 'screenshots'
    $process = Start-Process -FilePath $exe -ArgumentList @('--playback-test', ('"' + $screenshots + '"')) -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw "Presentation checks failed; inspect $destination/playback-error.txt" }
    Get-Content (Join-Path $screenshots 'playback-test-results.txt') -Tail 1
}
