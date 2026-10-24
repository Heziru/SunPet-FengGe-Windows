$ErrorActionPreference = 'Stop'
foreach ($name in @('SunPet', 'FengGe')) {
    & (Join-Path $PSScriptRoot ($name + '-Windows\build.ps1'))
}
