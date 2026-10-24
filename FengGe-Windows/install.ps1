param([switch]$Launch)
$ErrorActionPreference = 'Stop'
$destination = Join-Path $env:LOCALAPPDATA 'Programs\FengGe-Windows'
if (-not (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'FengGe.exe'))) { throw 'Build FengGe.exe first.' }
if (([IO.Path]::GetFullPath($PSScriptRoot)).TrimEnd('\') -ne ([IO.Path]::GetFullPath($destination)).TrimEnd('\')) {
    $running = Get-Process -Name FengGe -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq (Join-Path $destination 'FengGe.exe') }
    if ($running) { throw 'Exit the installed FengGe from its right-click menu before updating.' }
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    Get-ChildItem -LiteralPath $PSScriptRoot | Copy-Item -Destination $destination -Recurse -Force
}
$shell = New-Object -ComObject WScript.Shell
$locations = @([Environment]::GetFolderPath('Desktop'), [Environment]::GetFolderPath('Programs'))
foreach ($location in $locations) {
    $shortcut = $shell.CreateShortcut((Join-Path $location 'FengGe Windows.lnk'))
    $shortcut.TargetPath = Join-Path $destination 'FengGe.exe'
    $shortcut.WorkingDirectory = $destination
    $shortcut.IconLocation = (Join-Path $destination 'FengGe.ico') + ',0'
    $shortcut.Description = 'FengGe - click to play, drag to move, right-click to exit'
    $shortcut.Save()
}
Write-Output "Installed: $destination"
if ($Launch) { Start-Process -FilePath (Join-Path $destination 'FengGe.exe') -WorkingDirectory $destination -WindowStyle Normal }
