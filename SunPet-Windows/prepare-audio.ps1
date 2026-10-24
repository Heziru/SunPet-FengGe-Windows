param([string]$FFmpegPath = 'ffmpeg')
$ErrorActionPreference = 'Stop'
$clips = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Assets\clips.json') -Encoding UTF8 | ConvertFrom-Json
foreach ($clip in $clips) {
    $original = Join-Path $PSScriptRoot ('Assets\Clips\' + $clip.file)
    $wave = [IO.Path]::ChangeExtension($original, '.wav')
    & $FFmpegPath -nostdin -v error -y -i $original -map '0:a:0' -c:a pcm_s16le $wave
    if ($LASTEXITCODE -ne 0) { throw "Audio conversion failed: $original" }
}
Write-Output ('Prepared ' + $clips.Count + ' PCM WAV clips')
