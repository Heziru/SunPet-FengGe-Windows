param([switch]$Launch)
$ErrorActionPreference = 'Stop'
$destination = Join-Path $env:LOCALAPPDATA 'Programs\SunPet-Windows'
if (-not (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'SunPet.exe'))) { throw 'Build SunPet.exe first.' }
if (([IO.Path]::GetFullPath($PSScriptRoot)).TrimEnd('\') -ne ([IO.Path]::GetFullPath($destination)).TrimEnd('\')) {
    $running = Get-Process -Name SunPet -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq (Join-Path $destination 'SunPet.exe') }
    if ($running) { throw 'Exit the installed SunPet from its right-click menu before updating.' }
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    Get-ChildItem -LiteralPath $PSScriptRoot | Copy-Item -Destination $destination -Recurse -Force
}
$shell = New-Object -ComObject WScript.Shell
$locations = @([Environment]::GetFolderPath('Desktop'), [Environment]::GetFolderPath('Programs'))
foreach ($location in $locations) {
    $shortcut = $shell.CreateShortcut((Join-Path $location 'SunPet Windows.lnk'))
    $shortcut.TargetPath = Join-Path $destination 'SunPet.exe'
    $shortcut.WorkingDirectory = $destination
    $shortcut.IconLocation = (Join-Path $destination 'SunPet.ico') + ',0'
    $shortcut.Description = 'SunPet - click to play, drag to move, right-click to exit'
    $shortcut.Save()
}
Write-Output "Installed: $destination"
if ($Launch) { Start-Process -FilePath (Join-Path $destination 'SunPet.exe') -WorkingDirectory $destination -WindowStyle Normal }
