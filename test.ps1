$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'build.ps1')
$reports = Join-Path $PSScriptRoot 'work\tests'
New-Item -ItemType Directory -Path $reports -Force | Out-Null
foreach ($name in @('SunPet', 'FengGe')) {
    $exe = Join-Path $PSScriptRoot ($name + '-Windows\' + $name + '.exe')
    $report = Join-Path $reports ($name + '.json')
    $process = Start-Process -FilePath $exe -ArgumentList ('--self-test "' + $report + '"') -PassThru -Wait -WindowStyle Hidden
    if ($process.ExitCode -ne 0) { throw "$name self-test failed. See $report" }
    Write-Output "$name self-test passed"
}
