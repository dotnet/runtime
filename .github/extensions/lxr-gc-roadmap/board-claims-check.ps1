# Verifies the board's quantified prose claims against the P0.5 artifacts they describe.
#
# Why this exists. board-check.ps1 has thirteen checks across contents, extent, subject and
# hygiene, and not one of them compares a SENTENCE to a NUMBER. It would pass a board whose
# every structural property was perfect and whose P0.6 directive told an implementer to code
# against a count that no longer held. That is not hypothetical here: the directive shipped
# "0 of 6 throughput-phase rows" where the set actually claimed was 69, and it was caught by
# re-reading rather than by any instrument. P0.5-baselines.md:941 names the same gap on the
# document side -- "Not one compared a *sentence* to a number" -- and check-prose-claims.py
# was written to close it there. This is that script's counterpart for the board.
#
# Design constraint, taken from check-prose-claims.py:11-14. Every expected value is DERIVED
# from the artifacts at run time and never written here as a constant. This file holds only
# the locator patterns, the predicates, and the paths. A checker holding its own copy of a
# number is a second place to be wrong, and it agrees with a stale board as readily as with
# a correct one.
#
# Each claim is checked at EVERY occurrence, not at one located sentence. The board repeats
# these figures between two and four times, which is the multi-site literal drift that
# verify-baselines.sh:10-11 forbids outright -- "never repeated as literals at several
# assertion sites, so two cannot drift from a third unnoticed". Prose cannot be de-duplicated
# the way a script's constants can, so the next best thing is to require every copy to agree
# with the derived value.
#
# A pattern that matches NOTHING fails. Rule 74: observing a pass on a perturbed input cannot
# distinguish "the check passed" from "the check never arrived", and a reworded sentence
# silently retires its own regex. The same guard applies to the populations -- an empty row
# set makes "all rows carry X" vacuously true, which this script's own first draft printed as
# "0 of 0" after reading `results` at the top level when the rows live under
# `checkpoints[].results`. A defaulted read is a non-arrival that returns a plausible value.
#
# Every claim prints the derived value, the located text and the comparison, per rule 26: a
# bare verdict hides a check that ran against the wrong thing.

param(
    [string]$Repo = (Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))),
    # The artifacts live on the P0.5 branch, not this one. Pinned to the reviewed tree by
    # hash rather than to a branch name: a moving ref would silently change the subject of
    # every claim below. After the pull request merges this becomes the merge commit.
    [string]$Ref = 'd272dabb7b4',
    # Parameterised only so the negative control can point this at a fabricated board, the
    # same reason board-check.ps1 parameterises $LogDir. The banner prints it, per rule 63.
    [string]$Board = (Join-Path $PSScriptRoot 'roadmap.md')
)

$ErrorActionPreference = 'Stop'

$BASE = 'docs/design/lxr-port/P0.5-baselines/'
$board = $Board
$pass = 0
$fail = 0

function ok  ($m) { $script:pass++; Write-Output "  ok    $m" }
function bad ($m) { $script:fail++; Write-Output "  FAIL  $m" }

function Show-Blob ($path) {
    $t = & git -C $Repo show "${Ref}:$path" 2>$null
    if ($LASTEXITCODE -ne 0) { throw "cannot read '$path' at $Ref" }
    return ($t -join "`n")
}

Write-Output 'LXR board claim check'
Write-Output "  board : $board"
Write-Output "  repo  : $Repo"
Write-Output "  ref   : $Ref"

$resolved = & git -C $Repo rev-parse --verify "$Ref^{commit}" 2>$null
if ($LASTEXITCODE -ne 0) { Write-Output "  FAIL  ref '$Ref' does not resolve in $Repo"; exit 2 }
Write-Output "  tree  : $(& git -C $Repo rev-parse "$Ref^{tree}")"
Write-Output ''

if (-not (Test-Path $board)) { Write-Output "  FAIL  no board at $board"; exit 2 }
$text = Get-Content $board -Raw

# ---- derive, from the artifacts only -------------------------------------------------

Write-Output '== derived from the artifacts =='

$rows = @()
foreach ($s in 's2', 's3', 's4sdk') {
    $doc = Show-Blob "${BASE}p0-5-baselines-$s.json" | ConvertFrom-Json
    foreach ($cp in $doc.checkpoints) { $rows += $cp.results }
}
if ($rows.Count -eq 0) { Write-Output '  FAIL  no result rows parsed; every count below would be vacuous'; exit 2 }

$csvRows = @()
$csvCols = $null
foreach ($s in 's2', 's3', 's4sdk') {
    $parsed = Show-Blob "${BASE}raw/p0-5-baselines-$s-invocations.csv" | ConvertFrom-Csv
    if ($null -eq $csvCols) { $csvCols = @($parsed[0].PSObject.Properties.Name) }
    $csvRows += $parsed
}
if ($csvRows.Count -eq 0) { Write-Output '  FAIL  no CSV rows parsed'; exit 2 }

$valid   = @($rows | Where-Object { $_.status -ne 'crashed' })
$lat     = @($valid | Where-Object { $_.notes -like '*testhost.latency*' })
$thr     = @($valid | Where-Object { $_.notes -notlike '*testhost.latency*' })
$thrEx   = @($thr | Where-Object { $_.scenario -ne 'aspnet-request-load' })
$latP99  = @($lat   | Where-Object { $null -ne $_.latencyP99Ms }).Count
$thrP99  = @($thrEx | Where-Object { $null -ne $_.latencyP99Ms }).Count
$fallback = @($rows | Where-Object { $_.pauseSource -like 'total-pause-duration-only*' }).Count

$csvLat  = @($csvRows | Where-Object { $_.runId -like '*.latency*' })
$csvThr  = @($csvRows | Where-Object { $_.runId -notlike '*.latency*' })
function HasVal ($v) { $v -and $v -ne 'NA' -and $v -ne 'null' }
$csvLatP99 = @($csvLat | Where-Object { HasVal $_.latencyP99Ms }).Count
$csvThrP99 = @($csvThr | Where-Object { HasVal $_.latencyP99Ms }).Count

$jsonKeys = @($rows | ForEach-Object { $_.PSObject.Properties.Name } | Sort-Object -Unique)
$shared   = @($csvCols | Where-Object { $jsonKeys -contains $_ }).Count

# Per-scenario worst latency-phase departure from a ratio of 1.000, in percent.
$worst = @{}
foreach ($r in $lat) {
    if ($null -ne $r.ratioVsBaseline) {
        $d = [Math]::Abs([double]$r.ratioVsBaseline - 1.0) * 100
        if (-not $worst.ContainsKey($r.scenario) -or $d -gt $worst[$r.scenario]) { $worst[$r.scenario] = $d }
    }
}
$within  = @($worst.Values | Where-Object { $_ -lt 0.16 }).Count
$maxNine = ($worst.GetEnumerator() | Where-Object { $_.Key -ne 'aspnet-request-load' } |
            ForEach-Object { $_.Value } | Measure-Object -Maximum).Maximum
$aspnet  = $worst['aspnet-request-load']

# The pause signal no ratio basis reaches. Derived from the per-invocation CSV, never from
# the aggregate row: Aggregator.cs:301-305 states the published pauseP99Ms is copied out of
# a SINGLE invocation, so the aggregate and the mean of five are different estimators and
# disagree by design (6.98x against 4.05x at 1.3x). Both are checked here, separately.
$phCsv = @($csvRows | Where-Object { $_.scenario -eq 'pinning-heavy-io' -and $_.runId -notlike '*.latency*' })
if ($phCsv.Count -eq 0) { Write-Output '  FAIL  no pinning-heavy-io throughput rows in the CSV'; exit 2 }
$phHeaps = @($phCsv | ForEach-Object { [double]$_.heapFactor } | Sort-Object -Unique)
$phJson  = @($valid | Where-Object { $_.scenario -eq 'pinning-heavy-io' -and $_.notes -notlike '*testhost.latency*' })

$phRatio = @(); $phPnum = @(); $phPden = 0; $phAgg = @(); $phPublished = @()
foreach ($h in $phHeaps) {
    $w = @($phCsv | Where-Object { [double]$_.heapFactor -eq $h -and $_.collector -eq 'wks' } | ForEach-Object { [double]$_.pauseP99Ms })
    $s = @($phCsv | Where-Object { [double]$_.heapFactor -eq $h -and $_.collector -eq 'srv' } | ForEach-Object { [double]$_.pauseP99Ms })
    if ($w.Count -eq 0 -or $s.Count -eq 0) { Write-Output "  FAIL  missing an arm at heap $h"; exit 2 }
    $mw = ($w | Measure-Object -Average).Average
    $ms = ($s | Measure-Object -Average).Average
    $phRatio += [Math]::Round($ms / $mw, 2)

    # Exact one-sided permutation test: every split of the pooled samples is enumerated, so
    # there is no seed and no resampling. A bootstrap interval cannot be re-derived by a
    # checker that does not share its RNG, which would make it a claim nothing is pointed at.
    $pool = @($w + $s); $n = $pool.Count; $k = $w.Count; $obs = $ms - $mw; $ge = 0; $tot = 0
    for ($m = 0; $m -lt (1 -shl $n); $m++) {
        $idx = @(0..($n - 1) | Where-Object { $m -band (1 -shl $_) })
        if ($idx.Count -ne $k) { continue }
        $a = @($idx | ForEach-Object { $pool[$_] })
        $b = @(0..($n - 1) | Where-Object { $idx -notcontains $_ } | ForEach-Object { $pool[$_] })
        $tot++
        if ((($b | Measure-Object -Average).Average - ($a | Measure-Object -Average).Average) -ge $obs) { $ge++ }
    }
    $phPnum += $ge
    $phPden = $tot

    $jw = @($phJson | Where-Object { [double]$_.heapFactor -eq $h -and $_.collector -eq 'wks' })
    $js = @($phJson | Where-Object { [double]$_.heapFactor -eq $h -and $_.collector -eq 'srv' })
    $phAgg += [Math]::Round([double]$js[0].pauseP99Ms / [double]$jw[0].pauseP99Ms, 2)
    $phPublished += ([double]$js[0].ratioVsBaseline).ToString('0.000000')
}
# The one heap factor where the two samples overlap, so the board cannot claim separation.
$hi = $phHeaps[-1]
$ovSrv = (@($phCsv | Where-Object { [double]$_.heapFactor -eq $hi -and $_.collector -eq 'srv' } | ForEach-Object { [double]$_.pauseP99Ms }) | Measure-Object -Minimum).Minimum
$ovWks = (@($phCsv | Where-Object { [double]$_.heapFactor -eq $hi -and $_.collector -eq 'wks' } | ForEach-Object { [double]$_.pauseP99Ms }) | Measure-Object -Maximum).Maximum

# aspnet-request-load is the matrix's only latency-primary scenario, and the column carrying
# its statistics is not the column carrying its signal. Both p-values are EXACT: every one of
# the 252 arrangements is enumerated, so there is no seed to share and any language reproduces
# them. Two-sided here, because the direction is not predicted in advance.
$aspJson = @($valid | Where-Object { $_.scenario -eq 'aspnet-request-load' -and $_.notes -like '*testhost.latency*' })
# The share must come from the per-invocation CSV, not the published JSON row. Nine columns
# including pauseP99Ms are copied from ONE invocation (Aggregator.cs:262), so a JSON-derived
# share is a ratio of two single draws. Same quantity, two estimators, and the sentence has to
# say which -- rule 84 applied to estimators rather than to artifacts or file paths.
$aspCsv = @($csvRows | Where-Object { $_.scenario -eq 'aspnet-request-load' -and
                                      $_.runId -like '*.latency' -and
                                      "$($_.valid)".Trim().ToLower() -eq 'true' })
$shares = @()
foreach ($grp in ($aspCsv | Group-Object { "$($_.collector)|$([double]$_.heapFactor)" })) {
    $mp = (@($grp.Group | ForEach-Object { [double]$_.pauseP99Ms })  | Measure-Object -Average).Average
    $ml = (@($grp.Group | ForEach-Object { [double]$_.latencyP99Ms }) | Measure-Object -Average).Average
    $shares += 100 * $mp / $ml
}
$aspShareLo = [Math]::Round(($shares | Measure-Object -Minimum).Minimum, 2)
$aspShareHi = [Math]::Round(($shares | Measure-Object -Maximum).Maximum, 2)

$aspCsv = @($csvRows | Where-Object { $_.scenario -eq 'aspnet-request-load' -and $_.runId -like '*.latency*' })
if ($aspCsv.Count -eq 0) { Write-Output '  FAIL  no aspnet latency-phase rows in the CSV'; exit 2 }
$aspHeaps = @($aspCsv | ForEach-Object { [double]$_.heapFactor } | Sort-Object -Unique)
$aspLatP = @(); $aspPauP = @()
foreach ($field in 'latencyP99Ms', 'pauseP99Ms') {
    $out = @()
    foreach ($h in $aspHeaps) {
        $w = @($aspCsv | Where-Object { [double]$_.heapFactor -eq $h -and $_.collector -eq 'wks' -and (HasVal $_.$field) } | ForEach-Object { [double]$_.$field })
        $s = @($aspCsv | Where-Object { [double]$_.heapFactor -eq $h -and $_.collector -eq 'srv' -and (HasVal $_.$field) } | ForEach-Object { [double]$_.$field })
        $obs = [Math]::Abs((($s | Measure-Object -Average).Average) - (($w | Measure-Object -Average).Average))
        $pool = @($w + $s); $n = $pool.Count; $k = $w.Count; $ge = 0; $tot = 0
        for ($m = 0; $m -lt (1 -shl $n); $m++) {
            $idx = @(0..($n - 1) | Where-Object { $m -band (1 -shl $_) })
            if ($idx.Count -ne $k) { continue }
            $a = @($idx | ForEach-Object { $pool[$_] })
            $bb = @(0..($n - 1) | Where-Object { $idx -notcontains $_ } | ForEach-Object { $pool[$_] })
            $tot++
            if ([Math]::Abs((($bb | Measure-Object -Average).Average) - (($a | Measure-Object -Average).Average)) -ge $obs) { $ge++ }
        }
        $out += [Math]::Round($ge / $tot, 3)
    }
    if ($field -eq 'latencyP99Ms') { $aspLatP = $out } else { $aspPauP = $out }
}
Write-Output "  aspnet latency phase: pause is $aspShareLo-$aspShareHi% of latency p99; exact p latency $($aspLatP -join ' / '), pause $($aspPauP -join ' / ')"
Write-Output "  pinning-heavy-io pause srv/wks: csv $($phRatio -join ' / '), aggregate $($phAgg -join ' / '), exact p $($phPnum -join ' / ') of $phPden"
Write-Output "  json rows $($rows.Count), valid $($valid.Count), latency $($lat.Count), throughput $($thr.Count)"
Write-Output "  csv rows $($csvRows.Count), columns $($csvCols.Count), shared with json $shared"
Write-Output "  scenarios within 0.16%: $within of $($worst.Count); largest of the nine $([Math]::Round($maxNine,3))%; aspnet $([Math]::Round($aspnet,2))%"
Write-Output ''

# ---- section 6.4 variance table: reproduce the published rows, then extend to pause ----
# Rule 87. derive-variance-table.py takes ONE csv (load_cells(args.csv)). Unioning the three
# session CSVs charges cross-session drift to within-cell variance: the cell COUNTS still come
# out right because the partition is right, while every statistic inflates. So the control is
# not optional decoration -- it is the only thing that distinguishes this derivation from a
# plausible guess, and it is checked against the deliverable rather than against literals.
$FLOOR = 8.35
$s2Csv = @(Show-Blob "${BASE}raw/p0-5-baselines-s2-invocations.csv" | ConvertFrom-Csv)
function PhaseOf ($runId) {
    if ($runId -like '*.throughput') { return 'throughput' }
    if ($runId -like '*.latency')    { return 'latency' }
    return $null
}
function CvCells ($rowsIn, $metric, $phases) {
    $g = @{}
    foreach ($r in $rowsIn) {
        $ph = PhaseOf $r.runId
        if ($null -eq $ph -or $phases -notcontains $ph) { continue }
        if ("$($r.valid)".Trim().ToLower() -ne 'true') { continue }
        if (-not (HasVal $r.$metric)) { continue }
        $k = "$($r.scenario)|$($r.collector)|$($r.heapFactor)|$ph"
        if (-not $g.ContainsKey($k)) { $g[$k] = @() }
        $g[$k] += [double]$r.$metric
    }
    $cvs = @()
    foreach ($v in $g.Values) {
        if ($v.Count -lt 2) { continue }
        $mean = ($v | Measure-Object -Average).Average
        if ($mean -le 0) { continue }
        $ss = ($v | ForEach-Object { ($_ - $mean) * ($_ - $mean) } | Measure-Object -Sum).Sum
        $cvs += 100 * [Math]::Sqrt($ss / ($v.Count - 1)) / $mean
    }
    $s = @($cvs | Sort-Object); $n = $s.Count
    $med = if ($n % 2) { $s[[int](($n - 1) / 2)] } else { ($s[$n / 2 - 1] + $s[$n / 2]) / 2 }
    return [pscustomobject]@{ Cells = $n
                              Median = [Math]::Round($med, 2)
                              Max = [Math]::Round($s[-1], 2)
                              Above = @($cvs | Where-Object { $_ -gt $FLOOR }).Count }
}
$ctlThr = CvCells $s2Csv 'operationsPerSecond' @('throughput')
$ctlLat = CvCells $s2Csv 'latencyP99Ms'        @('latency')
$vPause = CvCells $s2Csv 'pauseP99Ms'          @('throughput', 'latency')
$badPop = CvCells $csvRows 'operationsPerSecond' @('throughput')

# ---- rule 32: the mode/runId substitution is asymmetric, so counting defends one section ----
# Both keys are derived here rather than one being assumed, because the rule's whole content
# is the DIFFERENCE between them. Keyed without the phase term: the phase IS the thing being
# substituted, so including it in the key would make every cell agree by construction -- the
# rule 108 defect, committed inside the check for the rule that motivated it.
function AsymCells ($rowsIn, $phase, $byMode) {
    $g = @{}
    foreach ($r in $rowsIn) {
        if ("$($r.valid)".Trim().ToLower() -ne 'true') { continue }
        $ph = if ($byMode) { "$($r.mode)".Trim() } else { PhaseOf $r.runId }
        if ($ph -ne $phase) { continue }
        $k = "$($r.scenario)|$($r.collector)|$($r.heapFactor)"
        if (-not $g.ContainsKey($k)) { $g[$k] = @() }
        if (HasVal $r.latencyP99Ms) { $g[$k] += [double]$r.latencyP99Ms }
    }
    $cvs = @()
    foreach ($v in $g.Values) {
        if ($v.Count -lt 2) { continue }
        $mean = ($v | Measure-Object -Average).Average
        if ($mean -le 0) { continue }
        $ss = ($v | ForEach-Object { ($_ - $mean) * ($_ - $mean) } | Measure-Object -Sum).Sum
        $cvs += 100 * [Math]::Sqrt($ss / ($v.Count - 1)) / $mean
    }
    $s = @($cvs | Sort-Object); $n = $s.Count
    $med = if ($n -eq 0) { $null } elseif ($n % 2) { $s[[int](($n - 1) / 2)] } else { ($s[$n / 2 - 1] + $s[$n / 2]) / 2 }
    return [pscustomobject]@{ Cells = $g.Keys.Count; CvCells = $n; Median = $med }
}
$s2Valid   = @($s2Csv | Where-Object { "$($_.valid)".Trim().ToLower() -eq 'true' })
$asymAgree = @($s2Valid | Where-Object { (PhaseOf $_.runId) -eq "$($_.mode)".Trim() }).Count
$asymDiv   = $s2Valid.Count - $asymAgree
$asymLatRun  = AsymCells $s2Csv 'latency'    $false
$asymLatMode = AsymCells $s2Csv 'latency'    $true
$asymThrRun  = AsymCells $s2Csv 'throughput' $false
$asymThrMode = AsymCells $s2Csv 'throughput' $true

# ---- rule 50: rot has three mechanisms, and the two git-derivable ones are derived ----
# This check is available HERE and is not available in the P0.5 gate, which runs from a
# `git archive` extract carrying no history. The property that makes that gate honest
# about committed bytes is exactly what blinds it to what has moved since -- so the
# instrument that can see rot has to be a different instrument, and this is it.
# Every line number below is LOCATED BY CONTENT in each revision, never assumed at the
# number the board publishes; otherwise the claim would confirm the board from the board.
$ROT_REF = '7c725de8fc8'
$CPC     = 'docs/design/lxr-port/P0.5-baselines/scripts/check-prose-claims.py'
$GOG     = 'docs/design/lxr-port/P0.5-baselines/scripts/verify-gate-of-the-gate.sh'

function FindLine ($ref, $path, $needle) {
    $ls = @(& git -C $Repo show "${ref}:${path}" 2>$null)
    if ($LASTEXITCODE -ne 0 -or $ls.Count -eq 0) { return 0 }
    for ($i = 0; $i -lt $ls.Count; $i++) { if ("$($ls[$i])".Contains($needle)) { return $i + 1 } }
    return 0
}

$ROT_NEEDLE = 'affected.add((r["scenario"], r["collector"], r["mode"]'
$ROT_OLD    = FindLine "$ROT_REF^" $CPC $ROT_NEEDLE
$ROT_NEW    = FindLine  $ROT_REF   $CPC $ROT_NEEDLE
$ROT_SHIFT  = $ROT_NEW - $ROT_OLD
$ROT_GOG    = FindLine  $ROT_REF   $GOG "grep -c '^RESULT: PASS'"
if ($ROT_OLD -eq 0 -or $ROT_NEW -eq 0 -or $ROT_GOG -eq 0) {
    Write-Output '  FAIL  a rule 50 rot target was not found by content; every figure below would be vacuous'
    exit 2
}

# The citation sweep, re-rooted at the port tree. The first run of this was rooted at the
# baselines directory while the harness sits a level up, and reported 9 of 13 unresolvable
# -- a resolver rooted below its targets reports the DOCUMENT as broken and makes the
# auditor confident. Root at the port tree and assert the resolvable count, so a future
# re-rooting fails here instead of producing a finding.
$rotDoc  = (@(& git -C $Repo show "${ROT_REF}:docs/design/lxr-port/P0.5-baselines.md")) -join "`n"
$rotTree = @(& git -C $Repo ls-tree -r --name-only $ROT_REF 'docs/design/lxr-port') |
           ForEach-Object { "$_".Trim() } | Where-Object { $_ }
$rotHits = [regex]::Matches($rotDoc, '`([^`\s]*\.[A-Za-z0-9]+):(\d+)`')
$ROT_OCC = $rotHits.Count
$rotPairs = @($rotHits | ForEach-Object { "$($_.Groups[1].Value)|$($_.Groups[2].Value)" } |
                         Sort-Object -Unique)
$ROT_DISTINCT = $rotPairs.Count
$ROT_UNRESOLVED = 0
foreach ($pair in $rotPairs) {
    $f = $pair.Split('|')[0]
    $c = @($rotTree | Where-Object { $_ -eq $f -or $_.EndsWith("/$f") })
    if ($c.Count -eq 0) { $ROT_UNRESOLVED++ }
}

# ---- rule 84 / P0.6: the boundary count is C(z,n), and the exact test is one-sided ----
# The published figure here was "exactly 1 of 252", hardcoded as 1 in this file's own want
# list. It is right at z=n and wrong for every larger z the quantifier "at least n" admits,
# so the series is DERIVED now and the claim asserts four values instead of one -- a single
# value cannot distinguish "correct at the boundary" from "correct on the interval".
function Comb ($nn, $kk) {
    if ($kk -lt 0 -or $kk -gt $nn) { return 0 }
    $r = 1.0
    for ($i = 1; $i -le $kk; $i++) { $r = $r * ($nn - $kk + $i) / $i }
    return [int][Math]::Round($r)
}
$UNDEF_Z = @(5, 6, 7, 8) | ForEach-Object { Comb $_ 5 }

# Exact permutation p over all C(2n,n) splits. Sidedness is a PARAMETER rather than a
# convention, because the whole finding is that the ratio/difference equivalence holds
# one-sided and fails two-sided.
function ExactP ($a, $b, $kind, $twoSided) {
    $pool = @($a) + @($b)
    $na = $a.Count
    $N = $pool.Count
    $idx = @(0..($N - 1))
    $stat = {
        param($A, $B)
        $ma = ($A | Measure-Object -Average).Average
        $mb = ($B | Measure-Object -Average).Average
        switch ($kind) {
            'ratio' { if ($mb -eq 0) { return $null }; if ($twoSided) { return ($ma / $mb) - 1.0 } else { return $ma / $mb } }
            default { return $ma - $mb }
        }
    }
    $obs = & $stat $a $b
    if ($null -eq $obs) { return $null }
    $cnt = 0; $tot = 0
    $combos = New-Object System.Collections.ArrayList
    function Rec ($start, $chosen) {
        if ($chosen.Count -eq $na) { [void]$combos.Add(@($chosen)); return }
        for ($i = $start; $i -lt $N; $i++) { Rec ($i + 1) (@($chosen) + $i) }
    }
    Rec 0 @()
    foreach ($c in $combos) {
        $A = @($c | ForEach-Object { $pool[$_] })
        $B = @($idx | Where-Object { $c -notcontains $_ } | ForEach-Object { $pool[$_] })
        $s = & $stat $A $B
        if ($null -eq $s) { continue }
        $tot++
        if ($twoSided) { if ([Math]::Abs($s) -ge [Math]::Abs($obs) - 1e-12) { $cnt++ } }
        else { if ($s -ge $obs - 1e-12) { $cnt++ } }
    }
    return [pscustomobject]@{ Count = $cnt; Total = $tot }
}

function PinArms ($phase, $heap) {
    $out = @{}
    foreach ($c in @('wks', 'srv')) {
        $v = @()
        foreach ($r in $s2Csv) {
            if ("$($r.valid)".Trim().ToLower() -ne 'true') { continue }
            if ("$($r.scenario)" -ne 'pinning-heavy-io' -or "$($r.collector)" -ne $c) { continue }
            if ((PhaseOf $r.runId) -ne $phase) { continue }
            if ([Math]::Abs([double]$r.heapFactor - $heap) -gt 1e-9) { continue }
            if (-not (HasVal $r.pauseP99Ms)) { continue }
            $v += [double]$r.pauseP99Ms
        }
        $out[$c] = $v
    }
    return $out
}

$PIN = @{}
foreach ($ph in @('throughput', 'latency')) {
    foreach ($h in @(1.3, 2.0, 6.0)) {
        $arm = PinArms $ph $h
        $r1 = ExactP $arm['srv'] $arm['wks'] 'ratio' $false
        $d1 = ExactP $arm['srv'] $arm['wks'] 'diff'  $false
        $r2 = ExactP $arm['srv'] $arm['wks'] 'ratio' $true
        $d2 = ExactP $arm['srv'] $arm['wks'] 'diff'  $true
        $ma = ($arm['srv'] | Measure-Object -Average).Average
        $mb = ($arm['wks'] | Measure-Object -Average).Average
        $PIN["$ph|$h"] = [pscustomobject]@{
            Ratio = [Math]::Round($ma / $mb, 2)
            OneRatio = $r1.Count; OneDiff = $d1.Count
            TwoRatio = $r2.Count; TwoDiff = $d2.Count
        }
    }
}
if ($PIN.Keys.Count -ne 6) {
    Write-Output '  FAIL  the pinning-heavy-io arm table did not populate; its figures would be vacuous'
    exit 2
}

$delivText = (Show-Blob ($BASE.TrimEnd('/') + '.md')) -join "`n"
$ctrl = 0
$ctlOk = $true
$controls = @(
    [pscustomobject]@{ Name = 'operationsPerSecond'; Got = $ctlThr }
    [pscustomobject]@{ Name = 'latencyP99Ms';        Got = $ctlLat }
)
foreach ($pair in $controls) {
    $name = $pair.Name; $got = $pair.Got
    $rx = '\| `' + $name + '` \| (\d+) \| \*\*(\d+\.\d+)%\*\* \| (\d+\.\d+)% \| \*\*(\d+) of (\d+)\*\* \|'
    $mm = [regex]::Match($delivText, $rx)
    if (-not $mm.Success) { bad "section 6.4 control: no published row for ``$name`` in the deliverable"; $ctlOk = $false; continue }
    $exp = @([int]$mm.Groups[1].Value, [double]$mm.Groups[2].Value, [double]$mm.Groups[3].Value, [int]$mm.Groups[4].Value)
    $gotA = @($got.Cells, $got.Median, $got.Max, $got.Above)
    $diff = @()
    for ($i = 0; $i -lt 4; $i++) { if ([Math]::Abs([double]$gotA[$i] - [double]$exp[$i]) -gt 0.001) { $diff += "$($gotA[$i]) vs published $($exp[$i])" } }
    if ($diff.Count) { bad "section 6.4 control: ``$name`` does not reproduce -- $($diff -join '; ')"; $ctlOk = $false }
    else {
        # A control is not a board claim: it compares two artifacts to each other and no
        # sentence anywhere states it. Counting it as a claim would break the coverage
        # script's invariant that the checker reports exactly as many claims as it defines.
        $ctrl++
        Write-Output "  ok    section 6.4 control: ``$name`` reproduces the published row exactly ($($gotA -join ' / '))"
    }
}
if (-not $ctlOk) { Write-Output '  the pause row below is therefore a guess and is not reported'; exit 1 }

# ---- the published pause column is one invocation, not a mean (Aggregator.cs:262) ----
$byCell = @{}
foreach ($r in $csvRows) {
    if (-not (HasVal $r.pauseP99Ms)) { continue }
    $k = "$($r.scenario)|$($r.collector)|$([double]$r.heapFactor)|$($r.runId)"
    if (-not $byCell.ContainsKey($k)) { $byCell[$k] = @() }
    $byCell[$k] += [pscustomobject]@{ Inv = [int]$r.invocation; V = [double]$r.pauseP99Ms }
}
$eqLast = 0; $eqMean = 0; $totPause = 0
foreach ($j in $rows) {
    if ($null -eq $j.pauseP99Ms) { continue }
    if ("$($j.notes)" -match 'runId=(\S+)') { $rid = $Matches[1] } else { continue }
    $k = "$($j.scenario)|$($j.collector)|$([double]$j.heapFactor)|$rid"
    if (-not $byCell.ContainsKey($k)) { continue }
    $vec = @($byCell[$k] | Sort-Object Inv)
    $totPause++
    if ([Math]::Abs([double]$j.pauseP99Ms - $vec[-1].V) -lt 1e-9) { $eqLast++ }
    if ([Math]::Abs([double]$j.pauseP99Ms - (($vec | Measure-Object -Property V -Average).Average)) -lt 1e-9) { $eqMean++ }
}
Write-Output "  pauseP99Ms variance (s2, published convention): $($vPause.Cells) cells, median $($vPause.Median)%, max $($vPause.Max)%, $($vPause.Above) above $FLOOR%"
Write-Output "  published pauseP99Ms == last invocation: $eqLast of $totPause; == column mean: $eqMean of $totPause"

# ---- the Kestrel attribution: presence is not causation, and no file can settle a rank ----
# Kestrel is constant across every row below, so anything that moves across them is not
# explained by the socket path. AspNetRequestLoadScenario.cs:87 establishes presence only.
$aspCell = @{}
foreach ($r in $csvRows) {
    if ($r.scenario -ne 'aspnet-request-load') { continue }
    if ("$($r.valid)".Trim().ToLower() -ne 'true' -or -not (HasVal $r.latencyP99Ms)) { continue }
    $k = "$(PhaseOf $r.runId)|$($r.collector)|$([double]$r.heapFactor)"
    if (-not $aspCell.ContainsKey($k)) { $aspCell[$k] = @() }
    $aspCell[$k] += [double]$r.latencyP99Ms
}
$aspLatM = @(); $aspThrM = @()
foreach ($k in $aspCell.Keys) {
    $m = ($aspCell[$k] | Measure-Object -Average).Average
    if ($k.StartsWith('latency|')) { $aspLatM += $m } else { $aspThrM += $m }
}
$latLo = ($aspLatM | Measure-Object -Minimum).Minimum
$latHi = ($aspLatM | Measure-Object -Maximum).Maximum
$thrLo = ($aspThrM | Measure-Object -Minimum).Minimum
$thrHi = ($aspThrM | Measure-Object -Maximum).Maximum
$aspLatLo = [Math]::Round($latLo, 1); $aspLatHi = [Math]::Round($latHi, 1)
$aspThrLo = [Math]::Round($thrLo, 2); $aspThrHi = [Math]::Round($thrHi, 2)
$kesLo = [Math]::Round($latLo / $thrHi, 0); $kesHi = [Math]::Round($latHi / $thrLo, 0)

# The rank the superlative was never checked against. Section 6.3 declares its basis as the
# arithmetic mean over invocations within a cell, so a scenario figure is the mean of its cell
# means. Pooling raw invocations instead double-weights whichever cells a later session re-ran,
# which is the estimator half of rule 102 and is what this block did until it was measured.
$cellAcc = @{}
foreach ($r in $csvRows) {
    if ("$($r.valid)".Trim().ToLower() -ne 'true' -or (PhaseOf $r.runId) -ne 'latency') { continue }
    if (-not (HasVal $r.latencyP99Ms)) { continue }
    $ck = "$($r.scenario)|$($r.collector)|$($r.heapFactor)"
    if (-not $cellAcc.ContainsKey($ck)) { $cellAcc[$ck] = @() }
    $cellAcc[$ck] += [double]$r.latencyP99Ms
}
$scn = @{}
foreach ($ck in $cellAcc.Keys) {
    $sc = $ck.Split('|')[0]
    if (-not $scn.ContainsKey($sc)) { $scn[$sc] = @() }
    $scn[$sc] += ($cellAcc[$ck] | Measure-Object -Average).Average
}
$scnAvg = @{}
foreach ($k in $scn.Keys) { $scnAvg[$k] = ($scn[$k] | Measure-Object -Average).Average }
$aspAvg  = $scnAvg['aspnet-request-load']
$others  = @($scnAvg.Keys | Where-Object { $_ -ne 'aspnet-request-load' } | ForEach-Object { $scnAvg[$_] })
$nearest = ($others | Measure-Object -Maximum).Maximum
$nearestR = [Math]::Round($nearest, 2)
$gapNear  = [Math]::Round($aspAvg / $nearest, 1)
$othLo    = [Math]::Round(($others | Measure-Object -Minimum).Minimum, 3)
$othHi    = [Math]::Round($nearest, 1)
$twoOrd   = @($others | Where-Object { ($aspAvg / $_) -ge 100 -and ($aspAvg / $_) -le 1000 }).Count
$notTwo   = $others.Count - $twoOrd
$aboveAsp = @($others | Where-Object { $_ -gt $thrHi }).Count
$belowAsp = @($others | Where-Object { $_ -lt $thrLo }).Count
Write-Output "  aspnet p99 with Kestrel held constant: latency $aspLatLo-$aspLatHi ms vs throughput $aspThrLo-$aspThrHi ms = ${kesLo}x-${kesHi}x"
Write-Output "  superlative rank: nearest other scenario $nearestR ms (${gapNear}x); two orders holds against $twoOrd of $($others.Count), fails against $notTwo"

# ---- compare every occurrence against the derived value ------------------------------

Write-Output '== board prose vs derived =='

# Each claim: a regex whose capture groups are numbers the board asserts, and the derived
# values they must equal. Patterns are anchored on the field or artifact they describe so
# that historical narration of a superseded figure cannot match one -- the board's rules
# quote past wrong counts deliberately, and a staleness grep that flagged those would be
# the false alarm this battery's header already records once.
# Each claim: a regex locating one SENTENCE, whose capture groups are the numbers that
# sentence asserts, paired with the derived values they must equal. Every pattern names the
# artifact it is about -- "the published JSON", "the per-invocation CSV" -- because the first
# draft of this list did not, and its JSON patterns matched the CSV's sentences and vice
# versa, reporting four failures that were entirely the checker's. Rule 84 turns out not to
# be a wording preference: a claim about a field present in both files cannot be located,
# let alone verified, until the sentence names which file it means.
#
# Patterns are anchored on surrounding prose rather than on the bare figures so that the
# board's deliberate narration of SUPERSEDED counts cannot match one. Rule 83 quotes its own
# retracted "0 of 6" on purpose; a staleness grep flagging that quotation would be the false
# alarm board-check.ps1's header already records once.
# ---------------------------------------------------------------------------
# Rule 88 -- the two retired citation predicates, and the decidable residue.
#
# These derive from artifacts the rest of this script never touches: the board's own
# git history, and the deliverable's `rule N` citations resolved against the board.
# Both figures in rule 88 are false-alarm rates measured on a HEALTHY artifact, so a
# regression here means either a citation broke or the board grew an edit whose shape
# the measurement did not anticipate -- both worth a failure.
$repoRoot = (& git -C $PSScriptRoot rev-parse --show-toplevel 2>$null)
$boardRel = '.github/extensions/lxr-gc-roadmap/roadmap.md'

# Board rules, first definition wins (a rule quoted inside another rule must not shadow it).
$ruleText = @{}
foreach ($ln in ($text -split "`r?`n")) {
    if ($ln -match '^(\d+)\. (.+)$' -and -not $ruleText.ContainsKey([int]$Matches[1])) {
        $ruleText[[int]$Matches[1]] = ($Matches[2] -replace '\s+', ' ')
    }
}

function WordRun ($a, $b) {
    $A = @(($a.ToLower() -replace '[^a-z0-9 ]', ' ') -split ' +' | Where-Object { $_ })
    $B = @(($b.ToLower() -replace '[^a-z0-9 ]', ' ') -split ' +' | Where-Object { $_ })
    $best = 0
    for ($i = 0; $i -lt $A.Count; $i++) {
        for ($j = 0; $j -lt $B.Count; $j++) {
            $k = 0
            while (($i + $k) -lt $A.Count -and ($j + $k) -lt $B.Count -and $A[$i + $k] -eq $B[$j + $k]) { $k++ }
            if ($k -gt $best) { $best = $k }
        }
    }
    return $best
}

$delivLines = (Show-Blob 'docs/design/lxr-port/P0.5-baselines.md') -split "`r?`n"
$citeOccur = 0; $citeQuote = 0; $citeDangling = 0; $citeRules = @{}
foreach ($ln in $delivLines) {
    foreach ($m in [regex]::Matches($ln, '(?i)\brules?\s+(\d+)\b')) {
        $n = [int]$m.Groups[1].Value
        $citeOccur++; $citeRules[$n] = $true
        if (-not $ruleText.ContainsKey($n)) { $citeDangling++; continue }
        if ((WordRun $ln $ruleText[$n]) -ge 5) { $citeQuote++ }
    }
}
$citeDistinct = $citeRules.Keys.Count

# Board history: an edit either extends a rule (previous text is a prefix) or replaces it.
#
# PINNED, and the pin is the whole point. This walk measures the board's own history, so
# every commit that states the result also joins the population it is a result about --
# and the commit after this measurement (5d3246b1163, which appended rule 84's fourth
# axis) pushed $extends from 22 to 23 and falsified a figure that was true when written.
# Rule 90. The pin is the state the measurement was actually taken over; leaving it
# unpinned makes the claim decay on a schedule set by unrelated future edits.
$HIST_PIN = 'a4eb4dbaedb'
$hist = @(& git -C $repoRoot log --format=%h --reverse $HIST_PIN -- $boardRel)
$prevRules = $null; $extends = 0; $replaces = 0; $replacedAt = @{}
foreach ($h in $hist) {
    $blobLines = (& git -C $repoRoot show "${h}:$boardRel" 2>$null) -split "`r?`n"
    $cur = @{}
    foreach ($ln in $blobLines) {
        if ($ln -match '^(\d+)\. (.+)$' -and -not $cur.ContainsKey([int]$Matches[1])) {
            $cur[[int]$Matches[1]] = ($Matches[2] -replace '\s+', ' ')
        }
    }
    if ($null -ne $prevRules) {
        foreach ($n in $prevRules.Keys) {
            if (-not $cur.ContainsKey($n)) { $replaces++; continue }
            $old = $prevRules[$n]; $new = $cur[$n]
            if ($new -eq $old) { continue }
            if ($new.StartsWith($old)) { $extends++ } else { $replaces++; $replacedAt["$h/$n"] = $true }
        }
    }
    $prevRules = $cur
}
# Rule 90's own evidence, derived: the same walk carried one commit past the pin.
$DRIFT_COMMIT = '5d3246b1163'
$prevRules = $null; $extendsAfter = 0
foreach ($h in @(& git -C $repoRoot log --format=%h --reverse $DRIFT_COMMIT -- $boardRel)) {
    $blobLines = (& git -C $repoRoot show "${h}:$boardRel" 2>$null) -split "`r?`n"
    $cur = @{}
    foreach ($ln in $blobLines) {
        if ($ln -match '^(\d+)\. (.+)$' -and -not $cur.ContainsKey([int]$Matches[1])) {
            $cur[[int]$Matches[1]] = ($Matches[2] -replace '\s+', ' ')
        }
    }
    if ($null -ne $prevRules) {
        foreach ($n in $prevRules.Keys) {
            if (-not $cur.ContainsKey($n)) { continue }
            $old = $prevRules[$n]; $new = $cur[$n]
            if ($new -ne $old -and $new.StartsWith($old)) { $extendsAfter++ }
        }
    }
    $prevRules = $cur
}

$replaced5051 = @(($replacedAt.Keys | Where-Object { $_ -match '/(50|51)$' })).Count
$live5051 = @([regex]::Matches($text, '(?i)\brules?\s+5[01]\b')).Count

# Identity basis, and unpinned on purpose. The walk above is pinned to $HIST_PIN because
# rule 90's extends/replaces figures are a measurement of a fixed state; this claim is
# about the board as it stands, so it must see replacements made after the pin -- rule
# 98's at 0f13853a89b is exactly one of those, and a pinned walk reports it as never
# having happened.
#
# The basis is the bolded HEADLINE, matching board-check.ps1, because the identity of a
# rule is the claim it asserts. The whole-text basis used above yields 22 numbers here
# against 3: it counts every mid-line repair, rule 13's `:317` -> `:315-319` among them,
# which is rot rather than a change of claim. Two instruments were using one word for two
# predicates that differ by more than 7x; rule 95 applied to a predicate instead of a field.
#
# One `git log -p` rather than 107 `git show` calls, which is why this is affordable here.
$replacedHead = @{}; $rmH = @{}; $adH = @{}
foreach ($l in @(& git -C $repoRoot log -p --unified=0 --format='commit %H' -- $boardRel)) {
    if ($l -cmatch '^commit ') {
        foreach ($k in @($rmH.Keys)) { if ($adH.ContainsKey($k) -and $adH[$k] -ne $rmH[$k]) { $replacedHead[$k] = $true } }
        $rmH = @{}; $adH = @{}
        continue
    }
    if ($l -cmatch '^-(\d+)\. ') {
        $nn = [int]$Matches[1]; $hm = [regex]::Match($l, '\*\*(.+?)\*\*')
        if ($hm.Success) { $rmH[$nn] = $hm.Groups[1].Value }
    } elseif ($l -cmatch '^\+(\d+)\. ') {
        $nn = [int]$Matches[1]; $hm = [regex]::Match($l, '\*\*(.+?)\*\*')
        if ($hm.Success) { $adH[$nn] = $hm.Groups[1].Value }
    }
}
foreach ($k in @($rmH.Keys)) { if ($adH.ContainsKey($k) -and $adH[$k] -ne $rmH[$k]) { $replacedHead[$k] = $true } }
$replacedNums = @($replacedHead.Keys | Sort-Object)
$liveReplaced = 0
foreach ($n in $replacedNums) { $liveReplaced += @([regex]::Matches($text, "(?i)\brules?\s+$n\b")).Count }

# ---------------------------------------------------------------------------
# The cost of "compute a ratio for every comparable numeric column", measured.
#
# A comparison cell here is (scenario, heapFactor, phase) -- collector is the axis being
# compared, so it cannot also be part of the key. Both arms must carry >=3 valid
# invocations, and a (cell, column) pair exists only where the column itself has >=3
# parseable values on BOTH arms; a column present on one arm compares against nothing.
#
# Population matters and is asserted in both directions: the pair and undefined counts are
# invariant, the degenerate count is not, and the union of subset re-runs understates it.
$METRIC = @(
    'operationsPerSecond','latencyP50Ms','latencyP99Ms','latencyP999Ms','latencyP9999Ms',
    'latencyMaxMs','serviceTimeP99Ms','arrivalRatePerSecond','achievedRatePerSecond',
    'lateFraction','dispatchLagP99Ms','pauseAverageMs','pauseP99Ms','pauseMaxMs',
    'gen0Collections','gen1Collections','gen2Collections','inducedCollections',
    'workingSetMb','committedMb')

function RatioDomain ($sessions) {
    $rows = @()
    foreach ($s in $sessions) {
        $csvText = Show-Blob "docs/design/lxr-port/P0.5-baselines/raw/p0-5-baselines-$s-invocations.csv"
        $rows += @($csvText | ConvertFrom-Csv | Where-Object { $_.valid -eq 'true' })
    }
    $arm = @{}
    foreach ($r in $rows) {
        # Rule 91. The phase is (runId, mode), not either alone. A run executes both
        # passes, so 30 of the 699 valid rows carry mode=latency under a .throughput
        # runId; keying on mode pools those two phases, and keying on runId alone pools
        # every session into one arm. Both wrong keys are detected below rather than
        # argued about: they produce arms of 10 and 13 where the design produces 5.
        $k  = '{0}|{1:N1}|{2}|{3}|{4}' -f $r.scenario, [double]$r.heapFactor, $r.runId, $r.mode, $r.collector
        if (-not $arm.ContainsKey($k)) { $arm[$k] = New-Object System.Collections.ArrayList }
        [void]$arm[$k].Add($r)
    }
    $armSizes = @($arm.Keys | ForEach-Object { $arm[$_].Count } | Sort-Object -Unique)
    $cells = @{}
    foreach ($k in $arm.Keys) { $cells[($k -replace '\|(wks|srv)$', '')] = $true }
    $pairs = 0; $undef = 0; $degen = 0; $well = 0; $byCol = @{}
    foreach ($c in $cells.Keys) {
        $wRows = $arm["$c|wks"]; $sRows = $arm["$c|srv"]
        if (-not $wRows -or -not $sRows -or $wRows.Count -lt 3 -or $sRows.Count -lt 3) { continue }
        foreach ($m in $METRIC) {
            $w = @(); $sv = @()
            foreach ($r in $wRows) { $x = $r.$m; if ($x -and $x.Trim() -and $x -ne 'NA') { $w  += [double]$x } }
            foreach ($r in $sRows) { $x = $r.$m; if ($x -and $x.Trim() -and $x -ne 'NA') { $sv += [double]$x } }
            if ($w.Count -lt 3 -or $sv.Count -lt 3) { continue }
            $pairs++
            $mw = ($w | Measure-Object -Average).Average
            if ($mw -eq 0) { $undef++; if ($m -eq 'inducedCollections') { $byCol['induced']++ }; continue }
            if ((($w | Sort-Object -Unique).Count -eq 1) -and (($sv | Sort-Object -Unique).Count -eq 1)) {
                $degen++
                if ($m -eq 'arrivalRatePerSecond') { $byCol['arrival']++ }
                continue
            }
            $well++
        }
    }
    return @{ pairs = $pairs; undef = $undef; degen = $degen; well = $well
              induced = [int]$byCol['induced']; arrival = [int]$byCol['arrival']
              armSizes = $armSizes }
}

# Rule 104, measurement form. The board now asserts that the curated metric list excludes
# configuration echoed into the metrics file. That is a property of this file, so it is
# checked here as a guard rather than argued in prose -- adding one of these columns to
# $METRIC would silently falsify the board sentence and change 870/92/693 underneath it.
$CFG_IN_METRIC = @('heapLimitMb', 'warmupSeconds', 'steadyStateSeconds') | Where-Object { $METRIC -contains $_ }
if ($CFG_IN_METRIC.Count -ne 0) {
    Write-Output ('  FAIL  $METRIC now contains configuration columns: ' + ($CFG_IN_METRIC -join ', '))
    exit 2
}
# Rule 107, second instance. Both endpoints are pinned so these counts cannot rot; the
# ancestor test is run on each because the whole defect being recorded is a count reported
# in a frame where it was not a distance. A count whose frame is unverified is not gated.
$FRAME_BASE = 'df9c6988c20'
$FRAME_TIP  = 'dc01449ffc1'
$FRAME_SEG0 = 'b4418bb9bf2'
foreach ($pair in @(@($FRAME_BASE, $FRAME_TIP), @($FRAME_SEG0, $FRAME_TIP))) {
    & git merge-base --is-ancestor $pair[0] $pair[1] 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Output ('  FAIL  ' + $pair[0] + ' is not an ancestor of ' + $pair[1] + '; its commit count would be a set difference, not a distance')
        exit 2
    }
}
$FRAME_TOTAL = [int](& git rev-list --count "$FRAME_BASE..$FRAME_TIP")
$FRAME_SEG   = [int](& git rev-list --count "$FRAME_SEG0^..$FRAME_TIP")

# Rule 109. Two falsifiable claims sit in that rule and both are checked here rather than
# asserted. The first is a count of the terms the user could not parse; the second is that
# "fine-arm" -- a term that reached a PR description -- denotes nothing, which is decidable
# by looking for it in the document the description summarises.
$P05DOC   = Show-Blob 'docs/design/lxr-port/P0.5-baselines.md'
$FINE_ARM = ([regex]::Matches($P05DOC, '(?i)fine[- ]arm')).Count
$r109 = ($text -split "`r?`n" | Where-Object { $_ -match '^109\. ' }) -join ' '
$listStart = $r109.IndexOf('could not parse ')
$listEnd   = $r109.IndexOf('unexplained terms in one description')
if ($listStart -lt 0 -or $listEnd -le $listStart) {
    Write-Output '  FAIL  rule 109 term list could not be located; its count would be ungated'
    exit 2
}
$TERMS_UNEXPLAINED = ([regex]::Matches($r109.Substring($listStart, $listEnd - $listStart), '(?<!\*)\*[^*]+\*(?!\*)')).Count


# The guard that stood here asserted the term must never appear in P0.5-baselines.md. That
# encoded a false lesson: the term is real, and the correct repair was to DEFINE it in the
# document, which the P0.5 session then did. Keeping the guard would have failed the board for
# the fix. What is gated instead is the true shape of the defect -- absent from the write-up,
# present in the source -- with both figures derived at the pinned ref.
$FA_TREE_OCC   = @(& git -C $repoRoot grep -o -i -E 'fine[- ]arm' $Ref -- docs/design/lxr-port 2>$null).Count
$FA_TREE_FILES = @(& git -C $repoRoot grep -l -i -E 'fine[- ]arm' $Ref -- docs/design/lxr-port 2>$null).Count
if ($FA_TREE_OCC -eq 0 -or $FA_TREE_FILES -eq 0) {
    Write-Output '  FAIL  the fine-arm corpus search returned nothing; its figures would be vacuous'
    exit 2
}

$domS2  = RatioDomain @('s2')
$domAll = RatioDomain @('s2', 's3', 's4sdk')

# Rule 91, as an instrument rather than an assertion. The design produces arms of five
# invocations, and eight of three -- which are not scattered damage but a single truncated
# run, p05.s4sdk.sdk.throughput, covering four scenarios at 2.0x, and BALANCED across the
# two collectors, which is why the short arms never produce an unequal comparison. Every
# unequal-arm cell in the artifacts comes from invalidated invocations instead. Any size
# other than five or three means
# the key is pooling something -- two phases, or two sessions -- and every figure derived
# from it is a figure about a population that was never run. This is not hypothetical: the
# keying this function used until now produced 8 arms of 10 and 8 of 13 on the union, and
# with them a well-formed count of 699 that collided with the 699 valid invocations and was
# recorded on this board as a finding. The collision was manufactured by the key.
$DESIGN_ARM_SIZES = @(3, 5)
$guard = 0
foreach ($pop in @(@{ n = 's2'; d = $domS2 }, @{ n = 'union'; d = $domAll })) {
    $badArms = @($pop.d.armSizes | Where-Object { $DESIGN_ARM_SIZES -notcontains $_ })
    $seen = ($pop.d.armSizes | Sort-Object) -join ','
    if ($badArms.Count -eq 0) {
        # Same reason the section 6.4 controls do not call ok: this is a property of the
        # artifacts, not a sentence on the board, and counting it as a claim would break the
        # coverage script's invariant that the checker reports as many claims as it defines.
        $guard++
        Write-Output "  ok    rule 91 guard: $($pop.n) arm sizes [$seen] are all ones the design can produce"
    } else {
        bad "rule 91 guard: $($pop.n) arm sizes [$seen] include $($badArms -join ',') which the design cannot produce; the key is pooling"
    }
}

# inducedCollections is an assertion, not a measurement: one distinct value over every
# valid invocation in every session. Counted as rows, which is NOT the same 699 as the
# well-formed pair count -- the collision rule 84 now carries as its fourth axis.
$allInvRows = @()
foreach ($s in @('s2', 's3', 's4sdk')) {
    $allInvRows += @((Show-Blob "docs/design/lxr-port/P0.5-baselines/raw/p0-5-baselines-$s-invocations.csv") |
                     ConvertFrom-Csv | Where-Object { $_.valid -eq 'true' })
}
$inducedDistinct = @($allInvRows | ForEach-Object { $_.inducedCollections } | Sort-Object -Unique)
$validInvocations = $allInvRows.Count

# ---------------------------------------------------------------------------
# Rule 89 -- the attainable range of the instrument, computed rather than asserted.
#
# Everything here is a property of the DESIGN (n per arm, alpha, the correction), not of
# the measurements, so it is derived from first principles with no artifact read. That is
# the point of the rule: the floor is knowable before any data exists.
function Choose ([int]$n, [int]$k) {
    $r = [double]1
    for ($i = 1; $i -le $k; $i++) { $r = $r * ($n - $k + $i) / $i }
    return [int][Math]::Round($r)
}
$ARRANGEMENTS_5v5 = Choose 10 5                       # 252
$EXACT_FLOOR      = 1.0 / $ARRANGEMENTS_5v5           # smallest attainable one-sided p
$LARGEST_USABLE_K = 0
for ($k = 1; $k -le $ARRANGEMENTS_5v5; $k++) { if (($k / $ARRANGEMENTS_5v5) -le 0.05) { $LARGEST_USABLE_K = $k } }
# Largest Bonferroni family in which the floor is still reachable.
$FAMILY_5 = 0
for ($m = 1; $m -le 5000; $m++) { if ($EXACT_FLOOR -le (0.05 / $m)) { $FAMILY_5 = $m } }
$ARRANGEMENTS_6v6 = Choose 12 6                       # 924
$FLOOR_6          = 1.0 / $ARRANGEMENTS_6v6
$FAMILY_6 = 0
for ($m = 1; $m -le 5000; $m++) { if ($FLOOR_6 -le (0.05 / $m)) { $FAMILY_6 = $m } }
# How far the literal directive sits below the floor, in multiples.
$SHORTFALL_693 = [int][Math]::Round($EXACT_FLOOR / (0.05 / $domS2.well))

# ---------------------------------------------------------------------------
# Rule 91 -- the keying defect, and what it would have cost.
#
# The tell is an arm size the design cannot produce. The consequence is that rule 89's
# floor is a function of n, so a pooled arm size makes the floor look reachable when it
# is not: at 10-versus-10 a 693-wide family clears, at the design 5-versus-5 it is short
# by a factor of 55. Both floors are derived, not quoted.
$CROSSING_ROWS = @($allInvRows | Where-Object {
    $_.runId -like '*throughput*' -and $_.mode -eq 'latency' }).Count
$armHist = @{}
foreach ($r in $allInvRows) {
    $k = '{0}|{1:N1}|{2}|{3}|{4}' -f $r.scenario, [double]$r.heapFactor, $r.runId, $r.mode, $r.collector
    $armHist[$k] = [int]$armHist[$k] + 1
}
$ARMS_OF_5 = @($armHist.Values | Where-Object { $_ -eq 5 }).Count
$ARMS_OF_3 = @($armHist.Values | Where-Object { $_ -eq 3 }).Count
$FLOOR_10v10   = 1.0 / (Choose 20 10)
$NEED_693      = 0.05 / $domS2.well

# Rule 92 -- the rank is basis-invariant, the values ranked are not.
function ArmSeries ([string]$col, [string]$basis, [bool]$designKey) {
    $ser = @{}
    foreach ($r in $allInvRows) {
        if ($r.mode -ne 'latency') { continue }
        if ($designKey -and ($r.runId -notlike '*.latency')) { continue }
        $v = $r.$col
        if (-not $v -or -not $v.Trim() -or $v -eq 'NA') { continue }
        $k = '{0}|{1}|{2:N1}' -f $r.scenario, $r.collector, [double]$r.heapFactor
        if (-not $ser.ContainsKey($k)) { $ser[$k] = New-Object System.Collections.ArrayList }
        [void]$ser[$k].Add(@{ i = [int]$r.invocation; v = [double]$v })
    }
    $out = @()
    foreach ($k in @($ser.Keys)) {
        if ($k -notlike '*|1.3') { continue }
        $hi = $k -replace '\|1\.3$', '|6.0'
        if (-not $ser.ContainsKey($hi)) { continue }
        $pair = @()
        foreach ($kk in @($k, $hi)) {
            $s = @($ser[$kk] | Sort-Object { $_.i })
            $pair += if ($basis -eq 'last') { $s[-1].v } else { ($s | ForEach-Object { $_.v } | Measure-Object -Average).Average }
        }
        if ($pair[0] -eq 0) { continue }
        $pct = ($pair[1] / $pair[0] - 1) * 100
        $out += @{ key = ($k -replace '\|1\.3$', ''); pct = $pct; mag = [Math]::Abs($pct) }
    }
    return @($out | Sort-Object { -$_.mag })
}
$rankAspnet = @()
foreach ($basis in @('last', 'mean')) {
    foreach ($dk in @($true, $false)) {
        $s = ArmSeries 'latencyP99Ms' $basis $dk
        $rankAspnet += 1 + [array]::IndexOf(@($s | ForEach-Object { $_.key }), 'aspnet-request-load|wks')
    }
}
$RANK_ASPNET   = @($rankAspnet | Sort-Object -Unique)
$RANK_SERIES_N = (ArmSeries 'latencyP99Ms' 'last' $true).Count
$lopLast = ArmSeries 'latencyP99Ms' 'last' $true
$lopMean = ArmSeries 'latencyP99Ms' 'mean' $true
$LOP_LAST_PCT  = @($lopLast | Where-Object { $_.key -eq 'large-object-pressure|wks' })[0].pct
$LOP_MEAN_PCT  = @($lopMean | Where-Object { $_.key -eq 'large-object-pressure|wks' })[0].pct
$LOP_LAST_RANK = 1 + [array]::IndexOf(@($lopLast | ForEach-Object { $_.key }), 'large-object-pressure|wks')
$LOP_MEAN_RANK = 1 + [array]::IndexOf(@($lopMean | ForEach-Object { $_.key }), 'large-object-pressure|wks')

$claims = @(
    @{ name = 'rule 32: the mode/runId divergence, per row'
       re   = "agree in \*\*(\d+)\*\* of s2's (\d+) valid rows and diverge in \*\*(\d+)\*\*"
       sites = 1
       want = @($asymAgree, $s2Valid.Count, $asymDiv) }

    @{ name = 'rule 32: the throughput side, where cardinality detects the substitution'
       re   = '\*\*(\d+) cells against (\d+)\*\*'
       sites = 1
       want = @($asymThrMode.Cells, $asymThrRun.Cells) }

    @{ name = 'rule 32: the latency side, where only the statistic moves'
       re   = 'cell count stays \*\*(\d+) either way\*\*.*?median CV \*\*(\d+\.\d+)% to (\d+\.\d+)%\*\*'
       sites = 1
       want = @($asymLatRun.Cells, [Math]::Round($asymLatRun.Median, 3), [Math]::Round($asymLatMode.Median, 3)) }

    @{ name = 'rule 50: the citation that rotted inside the commit that introduced it'
       re   = 'is at `:(\d+)` in the parent and `:(\d+)` in the commit'
       sites = 1
       want = @($ROT_OLD, $ROT_NEW) }

    @{ name = 'rule 50: the same-commit insertion, and the arithmetic that closes it'
       re   = '\*\*\+(\d+) above old line (\d+), and (\d+)\+(\d+)=(\d+) exactly\.\*\*'
       sites = 1
       want = @($ROT_SHIFT, $ROT_OLD, $ROT_OLD, $ROT_SHIFT, $ROT_NEW) }

    @{ name = 'rule 50: where the hand-corrected anchor actually sits, two corrections later'
       re   = 'the anchored `grep -c \x27\^RESULT: PASS\x27` it cites sits at `:(\d+)`'
       sites = 1
       want = @($ROT_GOG) }

    @{ name = 'rule 50: the citation sweep, re-rooted at the port tree'
       re   = '\*\*(\d+) occurrences, (\d+) distinct, (\d+) unresolvable'
       sites = 1
       want = @($ROT_OCC, $ROT_DISTINCT, $ROT_UNRESOLVED) }

    @{ name = 'rule 84: the boundary count is C(z,n), not one arrangement'
       re   = 'that is \*\*(\d+), (\d+), (\d+) and (\d+) of (\d+)\*\* for z = 5, 6, 7 and 8'
       sites = 1
       want = @($UNDEF_Z[0], $UNDEF_Z[1], $UNDEF_Z[2], $UNDEF_Z[3], $ARRANGEMENTS_5v5) }

    @{ name = 'rule 84: the equivalence is one-sided, and two-sided disagrees by exactly 2x'
       re   = 'exactly 2x on every heap: \*\*(\d+) vs (\d+), (\d+) vs (\d+), (\d+) vs (\d+) of (\d+)\*\*'
       sites = 1
       want = @($PIN['throughput|1.3'].TwoRatio, $PIN['throughput|1.3'].TwoDiff,
                $PIN['throughput|2'].TwoRatio,   $PIN['throughput|2'].TwoDiff,
                $PIN['throughput|6'].TwoRatio,   $PIN['throughput|6'].TwoDiff,
                $ARRANGEMENTS_5v5) }

    @{ name = 'rule 84: the phase-free table, and what it reads as under each phase'
       re   = 'throughput gives ratios \*\*([\d.]+) / ([\d.]+) / ([\d.]+)\*\* at one-sided p of \*\*(\d+), (\d+), (\d+) of 252\*\*, while latency gives \*\*([\d.]+) / ([\d.]+) / ([\d.]+)\*\* at \*\*(\d+), (\d+), (\d+) of 252\*\*'
       sites = 1
       want = @($PIN['throughput|1.3'].Ratio, $PIN['throughput|2'].Ratio, $PIN['throughput|6'].Ratio,
                $PIN['throughput|1.3'].OneRatio, $PIN['throughput|2'].OneRatio, $PIN['throughput|6'].OneRatio,
                $PIN['latency|1.3'].Ratio, $PIN['latency|2'].Ratio, $PIN['latency|6'].Ratio,
                $PIN['latency|1.3'].OneRatio, $PIN['latency|2'].OneRatio, $PIN['latency|6'].OneRatio) }

    @{ name = 'rule 104 measurement form: the curated metric list and the figures it was already gating'
       re   = 'curated `\$METRIC` list of \*\*(\d+)\*\* columns, and two claims have been gating \*\*(\d+), (\d+) and (\d+)\*\*'
       sites = 1
       want = @($METRIC.Count, $domS2.pairs, $domS2.undef, $domS2.well) }

    @{ name = 'rule 107 second instance: the two counts that are distances, and the one that is not'
       re   = '`df9c6988c20` to `dc01449ffc1` is \*\*(\d+)\*\*, and this session\u2019s own segment through that same commit is \*\*(\d+)\*\*'
       sites = 1
       want = @($FRAME_TOTAL, $FRAME_SEG) }

    @{ name = 'rule 109: the count of terms the user could not parse'
       re   = '\*\*(\d+)\*\* unexplained terms in one description'
       sites = 1
       want = @($TERMS_UNEXPLAINED) }

    @{ name = 'rule 109: where the borrowed term actually lives, and where it does not'
       re   = 'finding \*\*(\d+)\*\* occurrences'
       sites = 1
       want = @($FINE_ARM) }

    @{ name = 'rule 109: the corpus the term does live in'
       re   = 'the term appears \*\*(\d+)\*\* times across \*\*(\d+)\*\* files'
       sites = 1
       want = @($FA_TREE_OCC, $FA_TREE_FILES) }

    @{ name = 'rule 89: the floor of the exact test at five against five'
       re   = 'enumerates `C\(10,5\) = (\d+(?:\.\d+)?)` arrangements, so the smallest attainable p is \*\*1/(\d+(?:\.\d+)?) = ([\d.]+)\*\* and the largest usable one is (\d+(?:\.\d+)?)/'
       sites = 1
       want = @($ARRANGEMENTS_5v5, $ARRANGEMENTS_5v5, [Math]::Round($EXACT_FLOOR,6), $LARGEST_USABLE_K) }

    @{ name = 'rule 89: the largest interpreted family the design admits'
       re   = 'at \*\*m = (\d+(?:\.\d+)?)\*\* that is ([\d.e-]+) and the floor clears it; at \*\*m = (\d+(?:\.\d+)?)\*\* it is ([\d.e-]+) and the floor does not'
       sites = 1
       want = @($FAMILY_5, (0.05 / $FAMILY_5), ($FAMILY_5 + 1), (0.05 / ($FAMILY_5 + 1))) }

    @{ name = 'rule 89: how far the literal directive sits below the floor'
       re   = 'over (\d+(?:\.\d+)?) well-formed pairs would need ([\d.e-]+), which is \*\*(\d+(?:\.\d+)?)\*\* times below the floor'
       sites = 1
       want = @($domS2.well, (0.05 / $domS2.well), $SHORTFALL_693) }

    @{ name = 'rule 89: the escape at six invocations per arm'
       re   = '`C\(12,6\) = (\d+(?:\.\d+)?)` moves the floor to ([\d.e-]+) and the budget to \*\*(\d+(?:\.\d+)?)\*\*'
       sites = 1
       want = @($ARRANGEMENTS_6v6, $FLOOR_6, $FAMILY_6) }

    @{ name = 'rule 91: the rows on which mode and runId disagree'
       re   = '\*\*(\d+(?:\.\d+)?) of the (\d+(?:\.\d+)?) valid rows carry `mode=latency` under a `\.throughput` runId\*\*'
       sites = 1
       want = @($CROSSING_ROWS, $validInvocations) }

    @{ name = 'rule 91: the design arm sizes, and the pooled sizes that betray the key'
       re   = 'arms of \*\*10 and 13\*\* where the design produces \*\*(\d+(?:\.\d+)?)\*\*.*?the population is \*\*(\d+(?:\.\d+)?) arms of 5 and (\d+(?:\.\d+)?) of 3'
       sites = 1
       want = @(5, $ARMS_OF_5, $ARMS_OF_3) }

    # The whole point of rule 91: the pooled key does not perturb the conclusion, it
    # REVERSES it. Both floors are computed from Choose, so this claim fails if either the
    # arithmetic or the board drifts.
    @{ name = 'rule 91: the pooled arm size would have made rule 86 look achievable'
       re   = 'the floor reads \*\*([\d.e+-]+)\*\* against the \*\*([\d.e+-]+)\*\* a (\d+(?:\.\d+)?)-wide family needs.*?it is \*\*([\d.e+-]+)\*\*, \*\*([\d.]+)x\*\* short'
       sites = 1
       want = @($FLOOR_10v10, $NEED_693, $domS2.well, $EXACT_FLOOR, $SHORTFALL_693) }

    @{ name = 'rule 92: the rank that survives every basis'
       re   = 'ranking `aspnet-request-load`/wks \*\*(\d+(?:\.\d+)?)th of (\d+(?:\.\d+)?)\*\* arm-series.*?that rank is \*\*(\d+(?:\.\d+)?) of (\d+(?:\.\d+)?) under all four\*\*'
       sites = 1
       want = @($RANK_ASPNET[0], $RANK_SERIES_N, $RANK_ASPNET[0], $RANK_SERIES_N) }

    @{ name = 'rule 92: the illustration that inverts under the published basis'
       re   = 'quoted at \*\*(-[\d.]+)%\*\*, its value over means, where it ranks \*\*(\d+(?:\.\d+)?)rd of (\d+(?:\.\d+)?)\*\*.*?it is \*\*\+([\d.]+)%\*\*, ranking \*\*(\d+(?:\.\d+)?)th of (\d+(?:\.\d+)?)\*\*'
       sites = 1
       want = @($LOP_MEAN_PCT, $LOP_MEAN_RANK, $RANK_SERIES_N,
                $LOP_LAST_PCT, $LOP_LAST_RANK, $RANK_SERIES_N) }

    @{ name = 'P0.6 amendment: the union under the design key is a different instrument'
       re   = 'it is \*\*([\d,]+(?:\.\d+)?) pairs, (\d+(?:\.\d+)?) undefined and (\d+(?:\.\d+)?) well-formed\*\* against s2\u2019s \*\*(\d+(?:\.\d+)?), (\d+(?:\.\d+)?) and (\d+(?:\.\d+)?)\*\*'
       sites = 1
       want = @($domAll.pairs, $domAll.undef, $domAll.well,
                $domS2.pairs, $domS2.undef, $domS2.well) }

    # "Three orders of magnitude past the budget" was written, not computed; it is 1.76.
    # Gating the multiple rather than a vague magnitude is the only version of this
    # sentence a checker can hold, and it is the one the argument actually needs.
    @{ name = 'P0.6 amendment: how far each candidate population sits past the budget'
       re   = 'sit at \*\*([\d.]+)x\*\* and \*\*([\d.]+)x\*\* the budget of twelve'
       sites = 1
       want = @(($domS2.well / $FAMILY_5), ($domAll.well / $FAMILY_5)) }

    @{ name = 'P0.6 amendment: the budget the design must fit, restated in the entry'
       re   = 'can hold at most \*\*(\d+(?:\.\d+)?) comparisons\*\* before no cell can be significant'
       sites = 1
       want = @($FAMILY_5) }

    @{ name = 'P0.6 amendment: the arrangement-level hazard is real and currently vacuous'
       re   = 'coincide on the committed data at \*\*(\d+(?:\.\d+)?) of (\d+(?:\.\d+)?)\*\*'
       sites = 1
       want = @(0, $domS2.well) }

    @{ name = 'P0.6 amendment: the directive applied literally, s2 population'
       re   = 'yields \*\*(\d+(?:\.\d+)?) \(cell, column\) pairs, of which (\d+(?:\.\d+)?) are undefined and (\d+(?:\.\d+)?) are well-formed\*\*'
       sites = 1
       want = @($domS2.pairs, $domS2.undef, $domS2.well) }

    @{ name = 'P0.6 amendment: inducedCollections is an assertion, not a measurement'
       re   = 'undefined in (\d+(?:\.\d+)?) of (\d+(?:\.\d+)?) cells\*\* because the column is the string `0` in \*\*all (\d+(?:\.\d+)?) valid invocations'
       sites = 1
       want = @($domS2.induced, $domS2.induced, $validInvocations) }

    @{ name = 'P0.6 amendment: the pinned field the directive re-admits'
       re   = 'contributes (\d+(?:\.\d+)?) zero-width intervals\*\*.*?re-admits it (\d+(?:\.\d+)?) times'
       sites = 1
       want = @($domS2.arrival, $domS2.arrival) }

    @{ name = 'P0.6 amendment: family size against the well-formed count'
       re   = 'at (\d+(?:\.\d+)?) well-formed pairs, roughly (\d+(?:\.\d+)?) exclude 1\.000 by chance'
       sites = 1
       want = @($domS2.well, [int][Math]::Round($domS2.well * 0.05)) }

    @{ name = 'P0.6 amendment: the union understates the degeneracy it demonstrates'
       re   = 'the zero-width count is (\d+(?:\.\d+)?) and `arrivalRatePerSecond` contributes (\d+(?:\.\d+)?); derived over s2 alone.*?they are \*\*(\d+(?:\.\d+)?) and (\d+(?:\.\d+)?)\*\*'
       sites = 1
       want = @($domAll.degen, $domAll.arrival, $domS2.degen, $domS2.arrival) }

    @{ name = 'rule 84 fourth axis: the two quantities that collided on one integer'
       re   = 'constant across \*\*(\d+(?:\.\d+)?) valid invocations\*\* and, in the same message.*?\*\*(\d+(?:\.\d+)?) well-formed pairs\*\*'
       sites = 1
       want = @($validInvocations, $domAll.well) }

    @{ name = 'rule 88: quote-overlap predicate, false alarms on a healthy document'
       re   = 'only \*\*(\d+(?:\.\d+)?) of (\d+(?:\.\d+)?)\*\* carry a 5-or-more word verbatim run'
       sites = 1
       want = @($citeQuote, $citeOccur) }

    @{ name = 'rule 88: the citations the predicate would have flagged are correct'
       re   = 'independently verified as \*\*(\d+(?:\.\d+)?) of (\d+(?:\.\d+)?) distinct rules correct\*\*'
       sites = 1
       want = @($citeDistinct, $citeDistinct) }

    @{ name = 'rule 88: append-only predicate, extensions against replacements'
       re   = 'across the \*\*(\d+(?:\.\d+)?)\*\* commits touching this board up to `([0-9a-f]+)`, \*\*(\d+(?:\.\d+)?)\*\* edits extend a rule and \*\*(\d+(?:\.\d+)?)\*\* replace text in place'
       sites = 1
       want = @($hist.Count, $HIST_PIN, $extends, $replaces) }

    # Rule 90 states the drift that made the pin necessary, so the drift itself is derived
    # rather than remembered: walk the SAME history one commit further and show the count move.
    @{ name = 'rule 90: the unrelated commit that falsified a correct figure'
       re   = 'the one after it, `([0-9a-f]+)`, appended rule 84.s fourth axis in place, which is an extension, and the count became (\d+(?:\.\d+)?)'
       sites = 1
       want = @($DRIFT_COMMIT, $extendsAfter) }

    @{ name = 'P0.6 post-merge: the one decidable residue, measured'
       re   = 'every cited rule number must exist on the board\*\*.{0,6}currently (\d+(?:\.\d+)?) dangling'
       sites = 1
       want = @($citeDangling) }

    @{ name = 'P0.6 post-merge: the identity changes and their blast radius'
       re   = 'rules \*\*(\d+(?:\.\d+)?), (\d+(?:\.\d+)?) and (\d+(?:\.\d+)?) were replaced\*\* across `be5bc610e00` and `0f13853a89b`.*?with \*\*(\d+(?:\.\d+)?)\*\* live citations'
       sites = 1
       want = @($replacedNums[0], $replacedNums[1], $replacedNums[2], $liveReplaced) }

    @{ name = 'rule 83: latencyP99Ms coverage by phase, published JSON'
       re   = 'is populated in \*\*(\d+(?:\.\d+)?) of (\d+(?:\.\d+)?)\*\* latency-phase rows of the published JSON, against \*\*(\d+(?:\.\d+)?) of (\d+(?:\.\d+)?)\*\*'
       sites = 1
       want = @($latP99, $lat.Count, $thrP99, $thrEx.Count) }

    @{ name = 'rule 84: the figure the weak claim rested on, published JSON'
       re   = 'being \*\*(\d+(?:\.\d+)?) of (\d+(?:\.\d+)?)\*\* in the published JSON'
       sites = 1
       want = @($latP99, $lat.Count) }

    @{ name = 'rule 84: the figure that settles it, per-invocation CSV'
       re   = 'CSV settles it properly at \*\*(\d+(?:\.\d+)?) of (\d+(?:\.\d+)?)\*\* latency-phase against \*\*(\d+(?:\.\d+)?) of (\d+(?:\.\d+)?)\*\* throughput-phase'
       sites = 1
       want = @($csvLatP99, $csvLat.Count, $csvThrP99, $csvThr.Count) }

    @{ name = 'P0.6 directive: published JSON coverage'
       re   = 'present in \*\*(\d+(?:\.\d+)?) of (\d+(?:\.\d+)?)\*\* latency-phase, \*\*(\d+(?:\.\d+)?) of (\d+(?:\.\d+)?)\*\* throughput-phase excluding'
       sites = 1
       want = @($latP99, $lat.Count, $thrP99, $thrEx.Count) }

    @{ name = 'P0.6 directive: per-invocation CSV coverage'
       re   = 'settles it:\*\* \*\*(\d+(?:\.\d+)?) of (\d+(?:\.\d+)?)\*\* latency-phase rows, \*\*(\d+(?:\.\d+)?) of (\d+(?:\.\d+)?)\*\* throughput-phase'
       sites = 1
       want = @($csvLatP99, $csvLat.Count, $csvThrP99, $csvThr.Count) }

    @{ name = 'rule 84: the ambiguity surface between the two artifacts'
       re   = "of the CSV's (\d+(?:\.\d+)?) columns, \*\*(\d+(?:\.\d+)?) appear in the JSON"
       sites = 1
       want = @($csvCols.Count, $shared) }

    @{ name = 'F24 generalisation: scenarios inside the 0.16% band'
       re   = '\*\*([A-Za-z]+|\d+) of ([A-Za-z]+|\d+) scenarios publish a latency-phase'
       sites = 2
       want = @($within, $worst.Count) }

    @{ name = 'F24 generalisation: the sole exception and its magnitude'
       re   = '`aspnet-request-load` at (\d+\.\d+)%'
       sites = 2
       want = @([Math]::Round($aspnet, 2)) }

    @{ name = 'F24 generalisation: the margin by which the 0.16% bound clears'
       re   = 'the largest of the nine is `long-lived-cache` at \*\*(\d+\.\d+)%\*\*'
       sites = 1
       want = @([Math]::Round($maxNine, 3)) }
    @{ name = 'rule 80: rows carrying the fallback pauseSource label'
       re   = '(\d+(?:\.\d+)?) of (\d+(?:\.\d+)?) rows are \*labelled\*'
       sites = 1
       want = @($fallback, $rows.Count) }

    @{ name = 'P0.6 amendment: pinning-heavy-io aggregate pause ratios, published JSON'
       re   = 'its `pauseP99Ms` differs srv/wks by \*\*(\d+\.\d+)x / (\d+\.\d+)x / (\d+\.\d+)x\*\*'
       sites = 1
       want = @($phAgg[0], $phAgg[1], $phAgg[2]) }

    @{ name = 'P0.6 amendment: the ratios published beside that pause difference'
       re   = 'publishes `ratioVsBaseline` of (\d+\.\d+) / (\d+\.\d+) / (\d+\.\d+) in the throughput phase'
       sites = 1
       want = @($phPublished[0], $phPublished[1], $phPublished[2]) }

    @{ name = 'P0.6 amendment: exact permutation test over the per-invocation CSV'
       re   = 'point ratios of \*\*(\d+\.\d+)x\*\*, \*\*(\d+\.\d+)x\*\* and \*\*(\d+\.\d+)x\*\* at p = \*\*(\d+(?:\.\d+)?)/(\d+(?:\.\d+)?)\*\*, \*\*(\d+(?:\.\d+)?)/(\d+(?:\.\d+)?)\*\* and \*\*(\d+(?:\.\d+)?)/(\d+(?:\.\d+)?)\*\*'
       sites = 1
       want = @($phRatio[0], $phRatio[1], $phRatio[2], $phPnum[0], $phPden, $phPnum[1], $phPden, $phPnum[2], $phPden) }

    @{ name = 'P0.6 amendment: the overlap that forbids a separation claim'
       re   = "srv's minimum of (\d+\.\d+) falls below wks's maximum of (\d+\.\d+)"
       sites = 1
       want = @($ovSrv, $ovWks) }

    @{ name = 'P0.6 correction: aspnet pause as a share of its latency p99'
       re   = '`pauseP99Ms` is \*\*(\d+\.\d+)% to (\d+\.\d+)%\*\* of `latencyP99Ms`'
       sites = 1
       want = @($aspShareLo, $aspShareHi) }

    @{ name = 'P0.6 correction: exact p for the column that HAS the statistics'
       re   = 'gives p = \*\*(\d+\.\d+) / (\d+\.\d+) / (\d+\.\d+)\*\*, significant at no heap factor'
       sites = 1
       want = @($aspLatP[0], $aspLatP[1], $aspLatP[2]) }

    @{ name = 'P0.6 correction: exact p for the column that has NONE of them'
       re   = 'gives p = \*\*(\d+\.\d+) / (\d+\.\d+) / (\d+\.\d+)\*\*, significant at all of them'
       sites = 1
       want = @($aspPauP[0], $aspPauP[1], $aspPauP[2]) }

    @{ name = 'rule 87: the control rows the extension had to reproduce, s2 CSV'
       re   = '`operationsPerSecond` (\d+(?:\.\d+)?) / (\d+\.\d+)% / (\d+\.\d+)% / (\d+(?:\.\d+)?) of (\d+(?:\.\d+)?) and `latencyP99Ms` (\d+(?:\.\d+)?) / (\d+\.\d+)% / (\d+\.\d+)% / (\d+(?:\.\d+)?) of (\d+(?:\.\d+)?)'
       sites = 1
       want = @($ctlThr.Cells, $ctlThr.Median, $ctlThr.Max, $ctlThr.Above, $ctlThr.Cells,
                $ctlLat.Cells, $ctlLat.Median, $ctlLat.Max, $ctlLat.Above, $ctlLat.Cells) }

    @{ name = 'rule 87: what the WRONG population produced, three sessions unioned'
       re   = '\(median (\d+\.\d+)%, max (\d+\.\d+)%, (\d+(?:\.\d+)?) above the floor instead of 0\)'
       sites = 1
       want = @($badPop.Median, $badPop.Max, $badPop.Above) }

    @{ name = 'P0.6 gap: the section 6.4 row that does not exist, derived under its convention'
       re   = '`pauseP99Ms` is \*\*(\d+(?:\.\d+)?) cells, median within-cell CV (\d+\.\d+)%, max (\d+\.\d+)%, (\d+(?:\.\d+)?) of (\d+(?:\.\d+)?)'
       sites = 1
       want = @($vPause.Cells, $vPause.Median, $vPause.Max, $vPause.Above, $vPause.Cells) }

    @{ name = 'P0.6 gap: the published pause column is one invocation, not a mean'
       re   = 'equals the last valid invocation in \*\*(\d+(?:\.\d+)?) of (\d+(?:\.\d+)?)\*\* cells and the column mean in \*\*(\d+(?:\.\d+)?) of (\d+(?:\.\d+)?)\*\*'
       sites = 1
       want = @($eqLast, $totPause, $eqMean, $totPause) }

    @{ name = 'P0.6 post-merge: aspnet p99 with the Kestrel socket path held constant'
       re   = 'mean `latencyP99Ms` of \*\*(\d+\.\d+)-(\d+\.\d+) ms\*\* in the latency phase at 7,996 op/s and \*\*(\d+\.\d+)-(\d+\.\d+) ms\*\* in the throughput phase'
       sites = 1
       want = @($aspLatLo, $aspLatHi, $aspThrLo, $aspThrHi) }

    @{ name = 'P0.6 post-merge: the factor Kestrel cannot explain because it is constant'
       re   = 'a factor of \*\*(\d+(?:\.\d+)?)x to (\d+(?:\.\d+)?)x\*\* with Kestrel unchanged'
       sites = 1
       want = @($kesLo, $kesHi) }

    @{ name = 'P0.6 post-merge: where the low-rate p99 sits among the non-Kestrel scenarios'
       re   = 'the (\d+\.\d+)-(\d+\.\d+) ms range of the nine non-Kestrel scenarios, below (\w+) of them and above (\w+)'
       sites = 1
       want = @($othLo, $othHi, $aboveAsp, $belowAsp) }

    @{ name = 'P0.6 post-merge: the superlative ranked against its nearest member'
       re   = '`lifecycle-semantics` at (\d+\.\d+) ms, the gap is \*\*(\d+\.\d+)x\*\*'
       sites = 1
       want = @($nearestR, $gapNear) }

    @{ name = 'P0.6 post-merge: how far the superlative actually reaches'
       re   = 'holds against (\d+(?:\.\d+)?) of the (\d+(?:\.\d+)?) other scenarios and fails against (\d+(?:\.\d+)?)'
       sites = 1
       want = @($twoOrd, $others.Count, $notTwo) }
)
$WORDS = @{ 'nine' = 9; 'ten' = 10; 'one' = 1; 'two' = 2; 'three' = 3; 'four' = 4;
            'five' = 5; 'six' = 6; 'seven' = 7; 'eight' = 8; 'eleven' = 11; 'twelve' = 12;
            'thirteen' = 13; 'twenty' = 20 }

foreach ($c in $claims) {
    $m = [regex]::Matches($text, $c.re)
    if ($m.Count -eq 0) {
        # Not a pass. The claim may still be in the board under different wording, in which
        # case nothing is checking it, which is the state this whole script exists to end.
        bad "$($c.name): pattern matched nothing -- the sentence was reworded and its check silently retired"
        continue
    }
    # Zero matches is the loud failure; FEWER matches than the board is known to contain is
    # the quiet one, and it was real. Perturbing "nine of ten" to "three of ten" moved that
    # site outside a closed `(nine|\d+)` alternation, so the site stopped matching, the
    # surviving correct site still agreed, and the checker printed "1 occurrence(s), all
    # agree" over a corrupted board. Rule 74 one level up: not zero arrival but PARTIAL
    # arrival, which no non-empty guard can see. The site count is a property of how many
    # times the board chooses to state a fact, so it cannot be derived from the artifacts
    # the way every value below is; it is declared here and the coverage control proves it
    # fires.
    if ($m.Count -ne $c.sites) {
        bad "$($c.name): matched $($m.Count) site(s) where the board declares $($c.sites); a site was reworded out of its own pattern"
        continue
    }
    $badOnes = @()
    foreach ($x in $m) {
        for ($g = 1; $g -lt $x.Groups.Count; $g++) {
            $gr = $x.Groups[$g]
            $litRaw = $gr.Value
            # A capture group is a WINDOW, and a corrupted board can carry a value wider
            # than the window the pattern opened for it. `([\d,]+)` reading a board that
            # says "1019.11" captures "1019", and the checker then reports the truncation
            # as though it were the board's figure -- so a corruption that changed the
            # meaning would be quoted back as something else, and "1,018.5 pairs" would
            # have passed outright. This is the third instance of one class: a capture
            # narrower than the space of values the text could hold (negatives excluded by
            # a `^\d+` shape test; thousands separators; now decimals). Widening 51
            # patterns would fix the instances and leave the class, so instead every
            # numeric capture is expanded here to the full numeral it sits inside, and the
            # comparator is handed what the board actually says. Expansion is asymmetric on
            # purpose: leftward over digits and separators, but rightward only over digits
            # and a following decimal fraction -- never over a comma, because "870, 92" is
            # two figures and must not merge into one.
            if ($litRaw -match '^-?[\d,]*\d$') {
                $s = $gr.Index
                $e = $gr.Index + $gr.Length
                while ($s -gt 0 -and $text[$s - 1] -match '[\d,]') { $s-- }
                if ($s -gt 0 -and $text[$s - 1] -eq '-') { $s-- }
                while ($e -lt $text.Length -and $text[$e] -match '\d') { $e++ }
                if (($e + 1) -lt $text.Length -and $text[$e] -eq '.' -and $text[$e + 1] -match '\d') {
                    $e++
                    while ($e -lt $text.Length -and $text[$e] -match '\d') { $e++ }
                }
                $litRaw = $text.Substring($s, $e - $s)
            }
            $exp = $c.want[$g - 1]
            # Not every gated literal is a quantity. Rule 90's whole content is that a
            # self-referential count is only meaningful beside the revision it was taken
            # over, so the pin itself has to be gated -- otherwise the hash in the prose
            # and the hash the walk actually used could drift apart and every number below
            # would still agree. A non-numeric expectation is compared verbatim.
            if (([string]$exp) -notmatch '^-?\d+(\.\d+)?([eE][+-]?\d+)?$') {
                # Same SHAPE as the numeric message below, deliberately: the coverage
                # battery proves a perturbation arrived by looking for "says <payload> "
                # in this output, so a message that quotes the literal differently would
                # make every non-numeric claim un-coverable while still reading correctly
                # to a human.
                if ($litRaw -cne ([string]$exp)) {
                    $badOnes += "'$($x.Value.Trim())' says $litRaw where the artifacts give $exp"
                }
                continue
            }
            if ($WORDS.ContainsKey($litRaw)) {
                $lit = $WORDS[$litRaw]
            } elseif ($litRaw -match '^-?\d+(\.\d+)?([eE][+-]?\d+)?$') {
                $lit = [double]$litRaw
            } elseif ($litRaw -match '^\d{1,3}(,\d{3})+$') {
                # The board writes four-digit counts with a thousands separator, and until a
                # gated figure crossed 1,000 nothing here had to read one. It would not have
                # failed loudly: "1,018" is neither a number nor a count word, so it lands in
                # the branch below and reports corruption. Only the grouped shape is
                # normalised, so "1,2,3" still fails rather than silently becoming 123.
                $lit = [double]($litRaw -replace ',', '')
            } else {
                # An unrecognised count word must fail rather than throw or coerce. Widening
                # the alternation to catch corruption is only useful if what it catches is
                # then rejected.
                $badOnes += "'$($x.Value.Trim())' says '$litRaw ', which is not a number or a count word this script knows"
                continue
            }
            # An ABSOLUTE tolerance alone cannot discriminate small-magnitude figures: at
            # 0.001 absolute, a board claiming 9.9e-5 would agree with an artifact giving
            # 7.2e-5, a 37% error passing as a match. Rule 89's alpha values live exactly
            # there. But a fixed absolute bound is also wrong in the other direction: a
            # percentage the board states to one decimal can never be within 0.001 of a
            # derived -82.2738, so a correctly-rounded figure failed. The test that is right
            # at both ends is a ROUND TRIP -- the stated figure must be what the derived
            # value rounds to at the precision the board chose to state it -- kept honest by
            # the relative bound, which is what rejects the 9.9e-5 case (37% > 0.5%) since
            # scientific notation carries no decimal count to round to.
            $rel = if ([double]$exp -eq 0) { if ([double]$lit -eq 0) { 0 } else { [double]::PositiveInfinity } }
                   else { [Math]::Abs([double]$lit - [double]$exp) / [Math]::Abs([double]$exp) }
            $plain = ($litRaw -replace ',', '')
            if ($plain -match '^-?\d+(\.(\d+))?$') {
                $dp = if ($Matches[2]) { $Matches[2].Length } else { 0 }
                $agrees = ([Math]::Round([double]$exp, $dp) -eq [double]$lit) -and ($rel -le 0.005)
            } else {
                $diff = [Math]::Abs([double]$lit - [double]$exp)
                $agrees = ($diff -le 0.001) -and ($rel -le 0.005)
            }
            if (-not $agrees) {
                $badOnes += "'$($x.Value.Trim())' says $litRaw where the artifacts give $exp"
            }
        }
    }
    if ($badOnes.Count -eq 0) {
        ok "$($c.name): $($m.Count) occurrence(s), all agree with $($c.want -join ' / ')"
    } else {
        bad "$($c.name): $($badOnes.Count) of $($m.Count) occurrence(s) disagree"
        $badOnes | ForEach-Object { Write-Output "          $_" }
    }
}

Write-Output ''
if ($fail -eq 0) {
    Write-Output "RESULT: PASS ($pass claims verified against $Ref; $ctrl section 6.4 controls reproduced; $guard rule 91 arm-size guards)"
    exit 0
} else {
    Write-Output "RESULT: FAIL ($pass verified, $fail failed)"
    exit 1
}
