# Standing battery for roadmap.md, the LXR coordination board.
#
# Default LogDir is the app's extension log directory. Parameterised only so the
# subject controls can point at a fabricated log; the banner prints it, per rule 63's
# practice of disclosing every root the run reads.
param([string]$LogDir = "$env:USERPROFILE\.copilot\logs\extensions",
      [string]$HistoryDir = $PSScriptRoot)

#
# Exists because the battery was retyped from memory on each use and drifted three
# times in one sitting: '^\d+\. \*\*' counted 19 of 66 rules (early rules are not
# bold-prefixed); '^- \*\*[A-Za-z ]+\*\*:' counted 0 of 360 fields (the colon sits
# INSIDE the bold); and a staleness grep matched the very rule that documents the
# stale figure. Every one of those was a false alarm about a healthy board. A checker
# you retype is a checker whose patterns are unversioned.
#
# Three axes, after rule 63: a verdict can be wrong in CONTENTS, in EXTENT, or in
# SUBJECT, and count-equality reaches only the first two.
#
#   contents  what the board says
#   extent    how much of it was looked at
#   subject   whether it was this board at all
#
# Structural expectations are asserted, not merely printed: a battery that reports
# numbers without comparing them cannot fail, which is the defect rule 59 found in
# verify-baselines.sh. Update EXPECTED deliberately when the board legitimately grows.

$ErrorActionPreference = 'Stop'

$EXPECTED = @{ Phases = 11; Steps = 54; Fields = 360; Rules = 110 }

$board = Join-Path $PSScriptRoot 'roadmap.md'
$extension = Join-Path $PSScriptRoot 'extension.mjs'
$pass = 0
$fail = 0

function ok   ($m) { $script:pass++; Write-Output "  ok    $m" }
function bad  ($m) { $script:fail++; Write-Output "  FAIL  $m" }
function want ($label, $actual, $expected) {
    if ($actual -eq $expected) { ok "$label`: $actual" } else { bad "$label`: $actual, expected $expected" }
}

Write-Output 'LXR board battery'
Write-Output "  self : $PSScriptRoot"
Write-Output "  board: $board"
Write-Output "  logs : $LogDir"
Write-Output "  hist : $HistoryDir"
Write-Output ''

if (-not (Test-Path $board)) { Write-Output "  FAIL  no board at $board"; exit 2 }
$lines = @(Get-Content $board)

Write-Output '== contents =='
$phases = @($lines | Where-Object { $_ -cmatch '^## P\d+' })
$steps  = @($lines | Where-Object { $_ -cmatch '^### P\d+\.\d+' })
want 'phases' $phases.Count $EXPECTED.Phases
want 'steps'  $steps.Count  $EXPECTED.Steps

$rules = @($lines | Where-Object { $_ -cmatch '^\d+\. ' } |
           ForEach-Object { [int]([regex]::Match($_, '^(\d+)\.')).Groups[1].Value })
$maxRule = ($rules | Measure-Object -Maximum).Maximum
$dups = @($rules | Group-Object | Where-Object Count -gt 1)
$gaps = @(1..$maxRule | Where-Object { $rules -notcontains $_ })
if ($rules.Count -eq $maxRule -and $dups.Count -eq 0 -and $gaps.Count -eq 0) {
    ok "rules: $($rules.Count) numbered 1..$maxRule, no gaps, no duplicates"
} else {
    bad "rules: $($rules.Count) found, max $maxRule, $($dups.Count) duplicated, $($gaps.Count) missing$(if ($gaps.Count) { ' [' + ($gaps -join ',') + ']' })$(if ($dups.Count) { ' dup [' + ($dups -join ',') + ']' })"
}

# Contiguity alone is derived from the artifact, so a truncated rule set stays
# contiguous and passes: delete the highest rule and 1..N-1 is still perfect. That
# is rule 59's defect, reproduced in this file's first draft and caught by its own
# negative control. Shape is derived; extent needs a floor stated from outside.
# Raise this floor deliberately when rules are added.
if ($rules.Count -ge $EXPECTED.Rules) {
    ok "rule count $($rules.Count) meets the floor of $($EXPECTED.Rules)"
} else {
    bad "rule count $($rules.Count) is below the floor of $($EXPECTED.Rules); rules have been lost"
}

$extensionText = if (Test-Path -LiteralPath $extension -PathType Leaf) {
    Get-Content -LiteralPath $extension -Raw
} else {
    ''
}
$hasLxrBase = $extensionText.Contains('base_branch=\"lxr\"')
$hasLxrPrBase = $extensionText.Contains('Every LXR pull request targets the `lxr` branch')
$protectsMain = $extensionText.Contains('Fork `main` tracks `upstream/main`')
if ($hasLxrBase -and $hasLxrPrBase -and $protectsMain) {
    ok 'LXR branch flow: sessions and pull requests use lxr; main tracks upstream'
} else {
    bad "LXR branch flow drifted: base=$hasLxrBase, prBase=$hasLxrPrBase, protectsMain=$protectsMain"
}

Write-Output ''
Write-Output '== numeral boundaries =='
# Rule 93. This board states a count of its own occurrences of a literal, and that count
# is exactly the kind rule 90 warns about: writing the sentence that records it changed it
# from four/three to six/four. A self-referential figure nothing checks is a figure that
# silently goes stale on the next edit that happens to contain the literal -- and the
# literal here, '0 of 69', is a PREFIX of '0 of 693', which is how it went stale the first
# time. Both boundaries are asserted, because the whole point of the rule is that checking
# only one of them is what failed.
$joined = $lines -join "`n"
$whole = @([regex]::Matches($joined, '0 of 69')).Count
$term  = @([regex]::Matches($joined, '0 of 69\b')).Count
if ($whole -eq 6 -and $term -eq 4) {
    ok "rule 93's self-count holds: '0 of 69' matches $whole whole-text and $term terminated"
} else {
    bad "rule 93's self-count is stale: '0 of 69' matches $whole whole-text and $term terminated, where the rule states 6 and 4"
}

Write-Output ''
Write-Output '== extent =='
# The colon is inside the bold: '- **Status:** done'. Getting this wrong reports zero
# fields on a complete board, which reads as catastrophe rather than as a typo.
$fields = @($lines | Where-Object { $_ -cmatch '^- \*\*[A-Za-z ]+:\*\*' })
want 'fields' $fields.Count $EXPECTED.Fields

foreach ($req in 'Status', 'Summary') {
    $empty = @($lines | Where-Object { $_ -cmatch "^- \*\*$req`:\*\*\s*$" })
    if ($empty.Count -eq 0) { ok "every step carries a non-empty $req" }
    else { bad "$($empty.Count) empty $req field(s)" }
}

# Derived rather than literal, so it survives the board growing. Phases carry a
# Status of their own, so the correct expectation is steps + phases, not steps --
# a wrong expectation this battery caught on its first run, which no retyped
# version had ever tested.
$statusCount = @($lines | Where-Object { $_ -cmatch '^- \*\*Status:\*\*' }).Count
want 'one Status per step and per phase' $statusCount ($steps.Count + $phases.Count)

Write-Output ''
Write-Output '== subject =='
# The board resolves relative to this script, which is structurally the $SELF_DIR
# hazard of rule 63. What matters is not how many copies exist but whether the copy
# the host loads is the copy this script audits -- uniqueness was only ever a proxy
# for that, and it is a proxy that goes permanently red the moment the branch merges
# and a second, entirely legitimate copy appears. EXTENSION_PATH in the extension log
# answers the question directly, so the assertion moved to the evidence.
#
# The old form scanned all of C:\ recursively: 36 seconds, and -- until rule 74 --
# blind to hidden directories. A battery slow enough to skip is a battery that gets
# skipped.
$logs = @(Get-ChildItem $LogDir -Filter '*lxr-gc-roadmap*.log' -File -ErrorAction SilentlyContinue |
          Sort-Object LastWriteTime -Descending)
$paths = @($logs | ForEach-Object {
    # The newest log is the live one and the extension host holds it open. A default
    # read (File.ReadLines / Get-Content) throws on it, so the check would fail on
    # exactly the log that carries the current answer. Share ReadWrite.
    $fs = [IO.FileStream]::new($_.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite)
    try {
        $sr = [IO.StreamReader]::new($fs)
        while ($null -ne ($ln = $sr.ReadLine())) {
            $m = [regex]::Match($ln, 'EXTENSION_PATH=(.+?\\extension\.mjs)')
            if ($m.Success) { Split-Path -Parent $m.Groups[1].Value; break }
        }
    } finally { $fs.Dispose() }
})
$distinct = @($paths | Sort-Object -Unique)

if ($logs.Count -eq 0) {
    bad "no extension log under '$LogDir'; the host's board cannot be identified"
} elseif ($paths.Count -eq 0) {
    bad "$($logs.Count) log(s) but none records an EXTENSION_PATH"
} elseif ($paths[0] -eq $PSScriptRoot) {
    ok "the host loads the board this script audits (newest of $($logs.Count) logs)"
} else {
    bad "the host loads '$($paths[0])', not '$PSScriptRoot'"
}

# One distinct path across every log means no host has ever loaded another copy.
# This is the check that keeps working after the merge: two files on disk are fine,
# two loaded boards are not.
want 'distinct board paths across all logs' $distinct.Count 1
if ($distinct.Count -gt 1) { $distinct | ForEach-Object { Write-Output "          $_" } }

Write-Output ''
Write-Output '== rule identity =='
# Rule 103. Rule 22 requires correcting a claim IN PLACE, which means this board's rule
# numbers are mutable by design: a citation of 'rule 51' written before be5bc610e00 still
# resolves afterwards and now points at a different claim. An existence check -- the
# dangling-citation check the P0.5 session proposed -- returns clean on every one of these,
# because the rules were OVERWRITTEN, not deleted. So the guard is not existence but
# disclosure: any rule whose bolded headline was ever replaced must say so in its current
# text. Derived from history, never from a list, per rule 33: a literal set of replaced
# numbers would diverge from the file the moment another rule is corrected.
$replaced = @{}
try {
    $log = & git -C $HistoryDir log -p --unified=0 --format='commit %H' -- roadmap.md 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $log) { throw 'no history' }
    $rm = @{}; $ad = @{}
    function Flush-Commit {
        foreach ($n in $script:rm.Keys) {
            if ($script:ad.ContainsKey($n) -and $script:ad[$n] -ne $script:rm[$n]) { $script:replaced[$n] = $true }
        }
        $script:rm = @{}; $script:ad = @{}
    }
    foreach ($l in $log) {
        if ($l -cmatch '^commit ') { Flush-Commit; continue }
        # Headline claim only: the bolded lead. Body edits are not identity changes.
        if ($l -cmatch '^-(\d+)\. ') {
            $n = [int]$Matches[1]; $h = [regex]::Match($l, '\*\*(.+?)\*\*')
            if ($h.Success) { $rm[$n] = $h.Groups[1].Value }
        } elseif ($l -cmatch '^\+(\d+)\. ') {
            $n = [int]$Matches[1]; $h = [regex]::Match($l, '\*\*(.+?)\*\*')
            if ($h.Success) { $ad[$n] = $h.Groups[1].Value }
        }
    }
    Flush-Commit
} catch { $replaced = $null }

if ($null -eq $replaced) {
    bad 'rule identity: git history unreadable, so replacement disclosure could not be checked'
} else {
    $undisclosed = @()
    foreach ($n in ($replaced.Keys | Sort-Object)) {
        $t = @($lines | Where-Object { $_ -cmatch "^$n\. " })
        if ($t.Count -ne 1 -or $t[0] -notmatch '(?i)withdraw|retract|first draft|first recorded|first version|was wrong|are false') {
            $undisclosed += $n
        }
    }
    if ($undisclosed.Count -eq 0) {
        ok "rule identity: $($replaced.Count) headline(s) replaced in history [$(($replaced.Keys | Sort-Object) -join ',')], all disclosed in place"
    } else {
        bad "rule identity: replaced headlines that do not disclose it: $($undisclosed -join ',')"
    }
}

Write-Output ''
Write-Output '== hygiene =='
want 'trailing whitespace'  @($lines | Where-Object { $_ -cmatch '[ \t]+$' }).Count 0
want 'control characters'   @($lines | Where-Object { $_ -cmatch '[\x00-\x08\x0B\x0C\x0E-\x1F]' }).Count 0
$oddTicks = @($lines | Where-Object { $_ -cmatch '^\d+\. ' -and ([regex]::Matches($_, '`').Count % 2) -ne 0 })
want 'rules with unbalanced backticks' $oddTicks.Count 0

# Doubled apostrophes are a PowerShell artifact, not prose. Inside a regular
# single-quoted string '' escapes an apostrophe, but inside a @'..'@ HERE-STRING it does
# not -- it stays as two characters -- so every board edit written as a here-string
# leaked `rule''s` into rendered markdown. Twenty sites had accumulated before anything
# noticed, because all four instruments read the board for FIGURES and none read it as
# text. Rule 88's precondition is met with room to spare: the predicate fires 0 times on
# a clean board, there is no construction in English prose that wants two apostrophes in
# a row, and it fired on 4 of the 102 commits in this file's history -- so it is a real
# defect with a zero false-alarm rate rather than a style preference.
want 'doubled apostrophes' @($lines | Where-Object { $_ -cmatch "''" }).Count 0

Write-Output ''
if ($fail -eq 0) {
    Write-Output "RESULT: PASS ($pass checks)"
    exit 0
} else {
    Write-Output "RESULT: FAIL ($pass passed, $fail failed)"
    exit 1
}
