param(
    [string]$SunLog = (Join-Path $PSScriptRoot 'chain-SunPet.jsonl'),
    [string]$FengLog = (Join-Path $PSScriptRoot 'chain-FengGe.jsonl'),
    [string]$Report = (Join-Path $PSScriptRoot 'chain-test.json')
)
$ErrorActionPreference = 'Stop'
function Require($condition, $message) { if (-not $condition) { throw $message } }
$events = @()
foreach ($pair in @(@('SunPet', $SunLog), @('FengGe', $FengLog))) {
    foreach ($line in Get-Content -LiteralPath $pair[1] -Encoding UTF8) {
        if (-not $line.Trim()) { continue }
        $event = $line | ConvertFrom-Json
        $event | Add-Member -NotePropertyName pet -NotePropertyValue $pair[0]
        $event | Add-Member -NotePropertyName time -NotePropertyValue ([DateTimeOffset]::Parse($event.utc))
        $events += $event
    }
}
$events = @($events | Sort-Object time)
$finishes = @($events | Where-Object { $_.pet -eq 'SunPet' -and $_.action -eq 'session-finished' })
Require ($finishes.Count -gt 0) 'No completed four-round session was recorded.'
$cycles = @()
foreach ($finish in $finishes) {
    $start = $events | Where-Object { $_.pet -eq 'FengGe' -and $_.action -eq 'play-requested' -and $_.time -lt $finish.time } | Select-Object -Last 1
    Require ($null -ne $start) 'Missing FengGe click before the final reply.'
    $cycle = @($events | Where-Object { $_.time -ge $start.time -and $_.time -le $finish.time })
    $requests = @($cycle | Where-Object { $_.action -in @('play-requested', 'reply-requested') })
    Require (($requests.pet -join ',') -eq ((@('FengGe','SunPet') * 8) -join ',')) 'Wrong actor order or extra playback.'
    Require (($requests.turn -join ',') -eq ((0..15) -join ',')) 'Expected exactly 16 turns.'
    $ended = @($cycle | Where-Object { $_.action -eq 'audio-ended' })
    Require ($ended.Count -eq 16) 'All 16 clips must finish.'
    for ($i = 0; $i -lt 16; $i++) {
        Require ($ended[$i].pet -eq $requests[$i].pet -and $ended[$i].clip -eq $requests[$i].clip) 'Completion does not match playback.'
        if ($i -lt 15) { Require ($ended[$i].time -le $requests[$i + 1].time) 'Replies overlap their predecessor.' }
    }
    $sent = @($cycle | Where-Object { $_.action -eq 'bridge-sent' })
    Require (($sent.signalTurn -join ',') -eq ((1..15) -join ',')) 'Expected exactly 15 handoffs.'
    $rounds = @($cycle | Where-Object { $_.action -eq 'round-finished' })
    Require (($rounds.turn -join ',') -eq '3,7,11,15') 'Expected exactly four completed rounds.'
    for ($i = 2; $i -lt 16; $i++) { Require ($requests[$i].clip -ne $requests[$i - 2].clip) 'An actor repeated its previous random clip.' }
    $nextManual = $events | Where-Object { $_.time -gt $finish.time -and $_.action -eq 'play-requested' } | Select-Object -First 1
    $extra = @($events | Where-Object { $_.time -gt $finish.time -and $_.action -eq 'reply-requested' -and ($null -eq $nextManual -or $_.time -lt $nextManual.time) })
    Require ($extra.Count -eq 0) 'The chain continued after its final reply.'
    $cycles += [PSCustomObject]@{started=$start.utc;finished=$finish.utc;actors=$requests.pet;turns=$requests.turn;clips=$requests.clip}
}
$result = [PSCustomObject]@{pass=$true;completedSessions=$cycles.Count;roundsPerSession=4;sentencesPerRound=4;sentencesPerSession=16;noOverlap=$true;noImmediateRandomRepeat=$true;stoppedAfterLastRound=$true;cycles=$cycles}
$result | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $Report -Encoding UTF8
$result | ConvertTo-Json -Depth 5
