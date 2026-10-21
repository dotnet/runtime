# Coverage audit for board-check.ps1 -- the control for the controller.
#
# Rule 70: an instrument ships with a paired control. Rule 73: a control is N>=2
# instances with DISTINCT payloads. Rule 74: a control must prove its perturbation is
# visible to the check's own method before its verdict means anything.
#
# This drives every check in the battery with two distinct perturbations and records
# WHICH check fired, not merely that the battery failed -- a perturbation that trips a
# different check than the intended one is coverage that is not there. That distinction
# is the only reason the subject gap was ever found: the battery went red, and red for
# the wrong reason reads exactly like red for the right one.
#
# It lives beside board-check.ps1 deliberately (rule 76). Its previous home was a
# session scratch directory, where a future session reading the battery would have had
# no way to learn its checks had ever been controlled.
#
# HERMETIC. Operates on a copy under $env:TEMP and a fabricated log directory. It never
# writes the real board and never reads the real extension logs, so it cannot be
# perturbed by -- or perturb -- a running extension host.

$ErrorActionPreference = 'Stop'

$src  = $PSScriptRoot
$work = Join-Path $env:TEMP 'bcaudit'
$logs = Join-Path $work 'logs'

if (Test-Path $work) { Remove-Item $work -Recurse -Force }
New-Item -ItemType Directory -Path $work | Out-Null
New-Item -ItemType Directory -Path $logs | Out-Null
Copy-Item (Join-Path $src 'board-check.ps1') $work
Copy-Item (Join-Path $src 'roadmap.md') (Join-Path $work 'pristine.md')
Copy-Item (Join-Path $src 'extension.mjs') (Join-Path $work 'pristine-extension.mjs')

$board = Join-Path $work 'roadmap.md'
$bc    = Join-Path $work 'board-check.ps1'
$extension = Join-Path $work 'extension.mjs'

# Ask the battery what it considers its own root rather than deriving it here. $env:TEMP
# yields the 8.3 short form (KONRAD~1) while $PSScriptRoot resolves long, so a locally
# constructed path fails the identity comparison on every case -- including pristine,
# which is how this was found. The banner is the battery's own answer, so no
# normalization rule has to be guessed at or kept in sync.
$selfPath = (@(& $bc -LogDir $logs -HistoryDir $src 2>&1) |
             ForEach-Object { $m = [regex]::Match($_, '^\s*self\s*:\s*(.+?)\s*$'); if ($m.Success) { $m.Groups[1].Value } } |
             Select-Object -First 1)
if (-not $selfPath) { throw 'could not read the battery''s self path from its banner' }

# The battery resolves its board from $PSScriptRoot, so the copy under $work is the
# subject here. Its subject checks compare against EXTENSION_PATH, which in the real
# logs names the real extension directory -- so without a fabricated log naming $work,
# the identity check would fail on every case and drown the signal.
function Write-ExtLog([string]$file, [string]$path, [datetime]$when) {
    # The name must contain 'lxr-gc-roadmap': the battery selects logs with
    # -Filter '*lxr-gc-roadmap*.log'. Fabricating 'ext-1.log' produced a directory the
    # battery read and found empty, which reports as "no extension log" -- a failure
    # that looks like the check working and is actually the control missing.
    $f = Join-Path $logs ('project-lxr-gc-roadmap-' + $file)
    Set-Content -LiteralPath $f -Encoding utf8 -Value @(
        '[extension-bootstrap] starting: pid=0, EXTENSION_PATH=' + $path + '\extension.mjs, SESSION_ID=audit'
        '[ready]'
    )
    (Get-Item $f).LastWriteTime = $when
}

# Derived, never hardcoded: an audit carrying a literal high-water rule number drifts
# silently the moment a rule is added, and then floor-N deletes a MID rule -- which
# trips the contiguity check instead, still goes red, and still reads as coverage.
# That defect was live in the previous version of this file at $hi = 73 against 76.
$pristine = @(Get-Content (Join-Path $work 'pristine.md'))
$hi = ($pristine | ForEach-Object { $m = [regex]::Match($_, '^(\d+)\. '); if ($m.Success) { [int]$m.Groups[1].Value } } |
       Measure-Object -Maximum).Maximum

function Apply($name, $lines) {
    $out = New-Object System.Collections.Generic.List[string]
    switch -Regex ($name) {
        '^phases-(\d)$' {
            $n = [int]$Matches[1]; $c = 0
            foreach ($x in $lines) { if ($x -cmatch '^## P\d+' -and $c -lt $n) { $c++; continue }; $out.Add($x) }
        }
        '^steps-(\d)$' {
            $n = [int]$Matches[1]; $c = 0
            foreach ($x in $lines) { if ($x -cmatch '^### P\d+\.\d+' -and $c -lt $n) { $c++; continue }; $out.Add($x) }
        }
        '^rulemid-(\d+)$' {
            $r = $Matches[1]; $d = $false
            foreach ($x in $lines) { if ($x -cmatch ('^' + $r + '\. ') -and -not $d) { $d = $true; continue }; $out.Add($x) }
        }
        '^selfcount-(\d)$' {
            # Two payloads that move the counts in DIFFERENT directions, because the whole
            # content of rule 93 is that the two boundaries fail independently: payload 1
            # shortens the longer figure so only the whole-text count drops, payload 2
            # rewrites the literal itself so both drop. A pair that moved them together
            # would not distinguish the terminated count from the unterminated one, and
            # this check exists precisely to hold both.
            $n = [int]$Matches[1]
            foreach ($x in $lines) {
                # The extra parentheses are load-bearing: `$out.Add($x -replace 'a', 'b')`
                # parses as Add() with TWO arguments, not as one replaced string.
                if ($n -eq 1) { $out.Add(($x -replace '0 of 693', '0 of 700')) }
                else          { $out.Add(($x -replace '0 of 69',  '0 of 42')) }
            }
        }
        '^floor-(\d)$' {
            $n = [int]$Matches[1]
            $drop = @(); for ($i = 0; $i -lt $n; $i++) { $drop += ($hi - $i) }
            foreach ($x in $lines) {
                $m = [regex]::Match($x, '^(\d+)\. ')
                if ($m.Success -and $drop -contains [int]$m.Groups[1].Value) { continue }
                $out.Add($x)
            }
        }
        '^fields-(\d)$' {
            $n = [int]$Matches[1]; $c = 0
            foreach ($x in $lines) { if ($x -cmatch '^- \*\*References:\*\*' -and $c -lt $n) { $c++; continue }; $out.Add($x) }
        }
        '^empty(Status|Summary)-(\d)$' {
            $f = $Matches[1]; $n = [int]$Matches[2]; $c = 0
            foreach ($x in $lines) {
                if ($x -cmatch ('^- \*\*' + $f + ':\*\* .') -and $c -lt $n) { $c++; $out.Add('- **' + $f + ':**'); continue }
                $out.Add($x)
            }
        }
        '^statusline-(\d)$' {
            $n = [int]$Matches[1]; $c = 0
            foreach ($x in $lines) { if ($x -cmatch '^- \*\*Status:\*\*' -and $c -lt $n) { $c++; continue }; $out.Add($x) }
        }
        '^tws-(\d)$' {
            $n = [int]$Matches[1]; $c = 0
            foreach ($x in $lines) { if ($x -cmatch '^## P' -and $c -lt $n) { $c++; $out.Add($x + ' '); continue }; $out.Add($x) }
        }
        '^ctrl-(\d)$' {
            $n = [int]$Matches[1]; $c = 0
            foreach ($x in $lines) { if ($x -cmatch '^## P' -and $c -lt $n) { $c++; $out.Add($x + [char]1); continue }; $out.Add($x) }
        }
        '^ticks-(\d)$' {
            $n = [int]$Matches[1]; $c = 0
            foreach ($x in $lines) { if ($x -cmatch '^\d+\. ' -and $c -lt $n) { $c++; $out.Add($x + ' `'); continue }; $out.Add($x) }
        }
        '^apos-(\d)$' {
            # Rule 73: the two payloads must differ, and they must differ in the way the
            # real defect did. apos-1 reintroduces the possessive form that leaked from
            # every here-string edit; apos-2 puts the pair somewhere no possessive can be,
            # so a guard narrowed to `''s` -- which is how the FIX was written -- fails
            # here even though it would pass apos-1.
            $n = [int]$Matches[1]; $c = 0
            foreach ($x in $lines) {
                if ($x -cmatch '^\d+\. ' -and $c -lt $n) {
                    $c++
                    if ($n -eq 1) { $s = $x -replace '^(\d+)\. ', '$1. The rule''''s own point. ' }
                    else           { $s = $x + ' See '''' above.' }
                    $out.Add($s)
                    continue
                }
                $out.Add($x)
            }
        }
        '^ridentity-(\d)$' {
            # Rule 73: the guard must fire on ANY replaced rule, not only the one that
            # motivated it. ridentity-1 strips rule 50's markers ('withdraw', 'first
            # recorded', 'was wrong'); ridentity-2 strips rule 98's, which are a different
            # vocabulary ('first draft', 'are false'). A guard hardcoded to the word
            # 'withdraw' -- the obvious way to write it -- passes ridentity-2 and fails
            # only ridentity-1, so identical payloads here would hide a half-blind check.
            $n = [int]$Matches[1]
            $target = if ($n -eq 1) { 50 } else { 98 }
            foreach ($x in $lines) {
                if ($x -cmatch "^$target\. ") {
                    $s = $x -replace '(?i)withdraw\w*', 'note' -replace '(?i)retract\w*', 'note'
                    $s = $s -replace '(?i)first draft', 'earlier text' -replace '(?i)first recorded', 'earlier noted'
                    $s = $s -replace '(?i)first version', 'earlier text' -replace '(?i)was wrong', 'was imprecise'
                    $s = $s -replace '(?i)are false', 'are imprecise'
                    $out.Add($s)
                    continue
                }
                $out.Add($x)
            }
        }
        default { foreach ($x in $lines) { $out.Add($x) } }
    }
    return $out
}

# Distinct payloads per pair, per rule 73: identical payloads cannot detect a check
# that reports the right verdict against the wrong instance.
$cases = @(
    @{ n = 'phases-1';        want = 'phases' },
    @{ n = 'phases-2';        want = 'phases' },
    @{ n = 'steps-1';         want = 'steps' },
    @{ n = 'steps-2';         want = 'steps' },
    @{ n = 'rulemid-40';      want = 'rules' },
    @{ n = 'rulemid-17';      want = 'rules' },
    @{ n = 'floor-1';         want = 'rule count' },
    @{ n = 'floor-2';         want = 'rule count' },
    @{ n = 'fields-1';        want = 'fields' },
    @{ n = 'fields-2';        want = 'fields' },
    @{ n = 'emptyStatus-1';   want = 'empty Status' },
    @{ n = 'emptyStatus-2';   want = 'empty Status' },
    @{ n = 'emptySummary-1';  want = 'empty Summary' },
    @{ n = 'emptySummary-2';  want = 'empty Summary' },
    @{ n = 'statusline-1';    want = 'one Status per step' },
    @{ n = 'statusline-2';    want = 'one Status per step' },
    @{ n = 'identity-alpha';  want = 'the host loads' },
    @{ n = 'identity-beta';   want = 'the host loads' },
    @{ n = 'distinct-gamma';  want = 'distinct board paths' },
    @{ n = 'distinct-delta';  want = 'distinct board paths' },
    @{ n = 'tws-1';           want = 'trailing whitespace' },
    @{ n = 'tws-2';           want = 'trailing whitespace' },
    @{ n = 'ctrl-1';          want = 'control characters' },
    @{ n = 'ctrl-2';          want = 'control characters' },
    @{ n = 'ticks-1';         want = 'unbalanced backticks' },
    @{ n = 'ticks-2';         want = 'unbalanced backticks' },
    @{ n = 'apos-1';          want = 'doubled apostrophes' },
    @{ n = 'apos-2';          want = 'doubled apostrophes' },
    @{ n = 'selfcount-1';     want = "rule 93's self-count" },
    @{ n = 'selfcount-2';     want = "rule 93's self-count" },
    @{ n = 'ridentity-1';     want = 'rule identity' },
    @{ n = 'ridentity-2';     want = 'rule identity' },
    @{ n = 'branchflow-1';    want = 'LXR branch flow' },
    @{ n = 'branchflow-2';    want = 'LXR branch flow' },
    @{ n = 'pristine';        want = '<none>' }
)

Write-Output "coverage audit for board-check.ps1"
Write-Output "  battery : $bc"
Write-Output "  rules   : 1..$hi (derived)"
Write-Output ''
Write-Output ("payload            exit  fired")
Write-Output ("-----------------  ----  ---------------------------------------------")

$blind = @()
$unreached = @()
foreach ($c in $cases) {
    Get-ChildItem $logs -File | Remove-Item -Force
    $pristine | Set-Content $board
    Copy-Item (Join-Path $work 'pristine-extension.mjs') $extension -Force

    switch -Regex ($c.n) {
        '^identity-alpha$' { Write-ExtLog 'ext-1.log' 'D:\decoy-alpha' (Get-Date) }
        '^identity-beta$'  { Write-ExtLog 'ext-1.log' 'E:\decoy-beta'  (Get-Date) }
        '^distinct-gamma$' {
            Write-ExtLog 'ext-old.log' 'D:\other-gamma' (Get-Date).AddHours(-2)
            Write-ExtLog 'ext-new.log' $selfPath        (Get-Date)
        }
        '^distinct-delta$' {
            Write-ExtLog 'ext-old.log' 'E:\other-delta' (Get-Date).AddHours(-2)
            Write-ExtLog 'ext-new.log' $selfPath        (Get-Date)
        }
        '^branchflow-1$' {
            $text = (Get-Content -LiteralPath $extension -Raw).Replace(
                'base_branch=\"lxr\"',
                'base_branch=\"main\"')
            Set-Content -LiteralPath $extension -Value $text -NoNewline
            Write-ExtLog 'ext-new.log' $selfPath (Get-Date)
        }
        '^branchflow-2$' {
            $text = (Get-Content -LiteralPath $extension -Raw).Replace(
                'Every LXR pull request targets the `lxr` branch',
                'Every LXR pull request targets the `main` branch')
            Set-Content -LiteralPath $extension -Value $text -NoNewline
            Write-ExtLog 'ext-new.log' $selfPath (Get-Date)
        }
        default {
            Write-ExtLog 'ext-1.log' $selfPath (Get-Date)
            (Apply $c.n $pristine) | Set-Content $board
        }
    }

    # The board under audit is the copy in $work, but git history lives in the real
    # extension directory, so the history root is passed explicitly rather than derived
    # from $PSScriptRoot -- the same subject split the fabricated logs already use, and
    # per rule 63 the battery prints it. Letting it fall back would make the check report
    # "history unreadable" on every case, pristine included: a control that fails
    # everywhere looks exactly like a check that works everywhere.
    $out  = & $bc -LogDir $logs -HistoryDir $src 2>&1
    $code = $LASTEXITCODE

    # Rule 74: confirm the perturbation was CONSUMED, not merely that its directory was
    # read. The first version of this check asserted the log path appeared in the output
    # -- and it passed on every case while the fabricated logs were misnamed and
    # invisible to the battery's filter, because the path appears in the very message
    # that reports them missing. "The check mentioned my input" is not evidence the
    # check saw my input.
    if (@($out) -match 'no extension log under') { $unreached += $c.n }

    $fired = @($out | Where-Object { $_ -cmatch '^  FAIL' } |
               ForEach-Object { ($_ -replace '^  FAIL\s+', '').Trim() })
    $labels = @($fired | ForEach-Object { ($_ -split ':')[0] })

    $hit = if ($c.want -eq '<none>') { $fired.Count -eq 0 } else { @($labels | Where-Object { $_ -like "*$($c.want)*" }).Count -gt 0 }
    if (-not $hit) { $blind += $c.n }

    $mark = if ($hit) { ' ' } else { '<<' }
    Write-Output ("{0,-17}  {1,-4}  {2} {3}" -f $c.n, $code, (($labels | Select-Object -Unique) -join ' + '), $mark)
    foreach ($f in $fired) { Write-Output ("                         . $f") }
}

Write-Output ''
if ($unreached.Count -gt 0) { Write-Output ("PERTURBATION NEVER REACHED THE CHECK: " + ($unreached -join ', ')) }

# The case list above is HAND-MAINTAINED, so until now this battery could only report on
# checks it already knew about: adding a fifteenth check to board-check.ps1 left it
# uncovered and this script still printed "every check fired", which is the overclaim its
# sibling board-claims-coverage.ps1 avoids by extracting its patterns from the checker
# instead of restating them. Restating cannot be removed here -- a perturbation has to be
# written by hand for each check -- so the count is asserted against the subject instead:
# board-check reports how many checks it ran, and every one of them must own a distinct
# label in $cases. This is rule 85 aimed at the battery rather than at the board, and it
# is what makes "every check" a claim about board-check.ps1 rather than about this list.
$declared = @($cases | Where-Object { $_.want -ne '<none>' } | ForEach-Object { $_.want } | Select-Object -Unique).Count
$reported = 0
$pristineOut = & $bc -LogDir $logs -HistoryDir $src 2>&1
foreach ($ln in $pristineOut) {
    $m = [regex]::Match([string]$ln, 'RESULT: \w+ \((\d+) checks\)')
    if ($m.Success) { $reported = [int]$m.Groups[1].Value }
}
$countOk = ($reported -gt 0 -and $declared -eq $reported)
if (-not $countOk) {
    Write-Output ("COVERAGE INCOMPLETE: board-check runs $reported check(s) but this battery declares payloads for $declared; a check was added without a case")
}

if ($blind.Count -eq 0 -and $countOk) { Write-Output ("COVERAGE: every check fired on both of its distinct payloads (" + $cases.Count + " cases, " + $declared + " checks)") }
elseif ($blind.Count -gt 0) { Write-Output ("COVERAGE GAP: " + ($blind -join ', ')) }

Remove-Item $work -Recurse -Force
if ($blind.Count -gt 0 -or $unreached.Count -gt 0 -or -not $countOk) { exit 1 }
exit 0
