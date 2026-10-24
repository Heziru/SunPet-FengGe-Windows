$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'build.ps1')
$output = Join-Path $PSScriptRoot 'outputs'
$staging = Join-Path $PSScriptRoot ('work\package-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $output,$staging -Force | Out-Null
$hashes = @()
foreach ($name in @('SunPet', 'FengGe')) {
    $folderName = $name + '-Windows'
    $source = Join-Path $PSScriptRoot $folderName
    $target = Join-Path $staging $folderName
    New-Item -ItemType Directory -Path $target -Force | Out-Null
    Get-ChildItem -LiteralPath $source -File | Where-Object { $_.Extension -in @('.cs','.ico','.ps1','.md','.manifest','.exe','.config') -or $_.Name -like 'LICENSE*' } | Copy-Item -Destination $target
    Copy-Item -LiteralPath (Join-Path $source 'Assets') -Destination $target -Recurse
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'THIRD_PARTY_NOTICES.md') -Destination $target
    $archive = Join-Path $output ($folderName + '.zip')
    Compress-Archive -LiteralPath $target -DestinationPath $archive -CompressionLevel Optimal -Force
    $hash = Get-FileHash -LiteralPath $archive -Algorithm SHA256
    $hashes += $hash.Hash.ToLowerInvariant() + '  ' + [IO.Path]::GetFileName($archive)
    Write-Output $archive
}
$hashes | Set-Content -LiteralPath (Join-Path $output 'SHA256SUMS.txt') -Encoding ASCII
