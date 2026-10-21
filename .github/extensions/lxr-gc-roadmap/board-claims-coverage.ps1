# Negative control for board-claims-check.ps1.
#
# A checker that has only ever printed PASS is indistinguishable from a checker that cannot
# fail. That is rule 59's defect, and board-check.ps1 reproduced it in its own first draft --
# contiguity was derived from the artifact, so deleting the highest rule left 1..N-1 perfect
# and the check green. This control perturbs the board and requires each claim to go red.
#
# Rule 73: every claim is perturbed with TWO DISTINCT payloads, never the same one twice. A
# single payload proves only that something failed; two distinguishable ones prove the check
# READ THE VALUE IT NAMES, because the output has to quote back the payload that arrived. A
# checker that failed everything unconditionally would pass a one-payload control.
#
# Rule 74: a control that observes PASS on a perturbed input cannot tell "the check passed"
# from "the perturbation never arrived". So each case asserts the perturbation landed in the
# fabricated board before the checker runs, and asserts the checker's output quotes it.
#
# The patterns are EXTRACTED FROM THE CHECKER rather than restated here. A control holding
# its own copy of the locators would drift from the checker it audits and would keep passing
# after the checker stopped matching anything -- the same objection check-prose-claims.py:11
# raises against a checker holding its own copy of a number.
#
# Hermetic: every run works on a copy under a fresh temp directory. The real board is opened
# read-only and never written.

param(
    [string]$Checker = (Join-Path $PSScriptRoot 'board-claims-check.ps1'),
    [string]$Board   = (Join-Path $PSScriptRoot 'roadmap.md')
)

$ErrorActionPreference = 'Stop'

$cases = 0
$fail  = 0

function ok  ($m) { $script:cases++; Write-Output "  ok    $m" }
function bad ($m) { $script:cases++; $script:fail++; Write-Output "  FAIL  $m" }

Write-Output 'LXR board claim-check coverage'
Write-Output "  checker: $Checker"
Write-Output "  board  : $Board"

if (-not (Test-Path $Checker)) { Write-Output "  FAIL  no checker at $Checker"; exit 2 }
if (-not (Test-Path $Board))   { Write-Output "  FAIL  no board at $Board";     exit 2 }

$src      = Get-Content $Checker -Raw
$pristine = Get-Content $Board -Raw

# ---- extract the claim table from the checker ----------------------------------------

$defRx = "name = '(?<n>[^']+)'\s*\r?\n\s*re\s+= (?:'(?<r1>[^']*)'|`"(?<r2>[^`"]*)`")\s*\r?\n\s*sites = (?<s>\d+)"
$defs = @()
foreach ($x in [regex]::Matches($src, $defRx)) {
    $pattern = if ($x.Groups['r1'].Success) { $x.Groups['r1'].Value } else { $x.Groups['r2'].Value }
    $defs += [pscustomobject]@{ Name = $x.Groups['n'].Value; Pattern = $pattern; Sites = [int]$x.Groups['s'].Value }
}
Write-Output "  claims : $($defs.Count) extracted from the checker"
Write-Output ''

if ($defs.Count -eq 0) {
    Write-Output '  FAIL  extracted no claim definitions; every case below would be vacuous'
    exit 2
}

$work = Join-Path ([IO.Path]::GetTempPath()) ("lxr-claims-cov-" + [Guid]::NewGuid().ToString('N').Substring(0, 8))
New-Item -ItemType Directory -Path $work -Force | Out-Null

function Invoke-Checker ($boardPath) {
    $out = & $Checker -Board $boardPath 2>&1 | Out-String
    return [pscustomobject]@{ Out = $out; Code = $LASTEXITCODE }
}

try {
    # ---- control: the unperturbed board must pass --------------------------------------
    Write-Output '== pristine control =='
    $clean = Join-Path $work 'clean.md'
    Set-Content -LiteralPath $clean -Value $pristine -NoNewline
    $r = Invoke-Checker $clean
    if ($r.Code -eq 0 -and $r.Out -match 'RESULT: PASS \((\d+) claims') {
        $n = [int]$Matches[1]
        if ($n -eq $defs.Count) { ok "unperturbed board passes, all $n claims verified" }
        else { bad "unperturbed board passes but reports $n claims where $($defs.Count) were extracted" }
    } else {
        bad "unperturbed board did not pass (exit $($r.Code)); every perturbation below is uninterpretable"
    }

    # ---- each claim, two distinct payloads ---------------------------------------------
    Write-Output ''
    Write-Output '== per-claim perturbation, two distinct payloads each =='
    $WORDPAY = @{ 'nine' = @('three', 'seven'); 'ten' = @('four', 'eight') }

    foreach ($d in $defs) {
        $m = [regex]::Match($pristine, $d.Pattern)
        if (-not $m.Success) {
            bad "$($d.Name): the checker's own pattern matches nothing in the board"
            continue
        }
        $g = $m.Groups[1]
        $orig = $g.Value

        $payloads = @()
        if ($WORDPAY.ContainsKey($orig)) {
            $payloads = $WORDPAY[$orig]
        } elseif ($orig -match '^\d+$') {
            $payloads = @([string]([int]$orig + 1), [string]([int]$orig + 13))
        } elseif ($orig -cmatch '^[0-9a-f]{7,40}$') {
            # A gated literal is not always a quantity: rule 90 pins a population to a
            # COMMIT HASH, and [double]'5d3246b1163' throws, which took the whole battery
            # down at the first such claim. The payload must stay inside the capture's own
            # character class -- a perturbation that stops matching the pattern reports
            # "matched nothing", which is the wrong failure and hides the real one.
            $rot = { param($c) if ($c -eq 'f') { '0' } else { ([string]('0123456789abcdef'[('0123456789abcdef'.IndexOf($c) + 1)])) } }
            $p1 = $orig.Substring(0, $orig.Length - 1) + (& $rot $orig[-1])
            $p2 = $orig.Substring(0, $orig.Length - 2) + (& $rot $orig[-2]) + (& $rot $orig[-1])
            $payloads = @($p1, $p2)
        } else {
            $payloads = @(([double]$orig + 1.11).ToString('0.00'), ([double]$orig + 2.22).ToString('0.00'))
        }

        $seen = @()
        foreach ($p in $payloads) {
            $fake = $pristine.Substring(0, $g.Index) + $p + $pristine.Substring($g.Index + $g.Length)
            $path = Join-Path $work ("case-" + [Guid]::NewGuid().ToString('N').Substring(0, 6) + '.md')
            Set-Content -LiteralPath $path -Value $fake -NoNewline

            # Rule 74: prove the perturbation arrived before drawing any conclusion from the
            # verdict. A fabricated file that never changed produces a PASS that reads as a
            # coverage gap and is really a harness bug.
            $back = Get-Content $path -Raw
            if ($back -eq $pristine) {
                bad "$($d.Name) [$p]: fabricated board is byte-identical to the original; perturbation never arrived"
                continue
            }

            $r = Invoke-Checker $path
            $named = $r.Out -match ('FAIL\s+' + [regex]::Escape($d.Name))
            $quoted = $r.Out.Contains("says $p ")
            if ($r.Code -eq 1 -and $named -and $quoted) {
                $seen += $p
            } elseif ($r.Code -ne 1) {
                bad "$($d.Name) [$p]: exit $($r.Code), expected 1"
            } elseif (-not $named) {
                bad "$($d.Name) [$p]: the run failed but did not name this claim"
            } else {
                bad "$($d.Name) [$p]: named the claim but did not quote the payload back"
            }
        }
        if ($seen.Count -eq 2) {
            ok "$($d.Name): fired on both '$($seen[0])' and '$($seen[1])', each quoted back"
        }
    }

    # ---- rewording must be loud, not silent --------------------------------------------
    Write-Output ''
    Write-Output '== reworded sentence retires its own check, and must say so =='
    foreach ($d in $defs[0], $defs[5]) {
        $m = [regex]::Match($pristine, $d.Pattern)
        $fake = $pristine.Remove($m.Index, $m.Length).Insert($m.Index, '<reworded>')
        $path = Join-Path $work ("reword-" + [Guid]::NewGuid().ToString('N').Substring(0, 6) + '.md')
        Set-Content -LiteralPath $path -Value $fake -NoNewline
        $r = Invoke-Checker $path
        if ($r.Code -eq 1 -and $r.Out -match ([regex]::Escape($d.Name) + '.*matched nothing')) {
            ok "$($d.Name): rewording reports the check as retired rather than passing"
        } else {
            bad "$($d.Name): rewording gave exit $($r.Code) without reporting a retired check"
        }
    }

    # ---- a site dropping out of its own pattern must be loud ----------------------------
    # The defect this control found on its first run: with a closed alternation, corrupting
    # one of two sites moved it outside the pattern, the remaining site agreed, and the
    # checker reported "1 occurrence(s), all agree" over a corrupted board. The fix was a
    # declared site count; this is the case that proves the fix fires. The two payloads are
    # the two DIFFERENT sites, so the output has to identify which one left.
    Write-Output ''
    Write-Output '== a site reworded out of its own pattern must be loud =='
    foreach ($d in @($defs | Where-Object { $_.Sites -gt 1 })) {
        $hits = [regex]::Matches($pristine, $d.Pattern)
        $fired = 0
        for ($i = 0; $i -lt $hits.Count; $i++) {
            $h = $hits[$i]
            $fake = $pristine.Remove($h.Index, $h.Length).Insert($h.Index, "<site$i gone>")
            $path = Join-Path $work ("site-" + [Guid]::NewGuid().ToString('N').Substring(0, 6) + '.md')
            Set-Content -LiteralPath $path -Value $fake -NoNewline
            $r = Invoke-Checker $path
            $want = "matched $($hits.Count - 1) site(s) where the board declares $($d.Sites)"
            if ($r.Code -eq 1 -and $r.Out.Contains($want)) { $fired++ }
            else { bad "$($d.Name) [site $i removed]: exit $($r.Code), did not report the site count dropping" }
        }
        if ($fired -eq $hits.Count) {
            ok "$($d.Name): each of its $($hits.Count) sites is individually load-bearing"
        }
    }
    # ---- subject axis -------------------------------------------------------------------
    Write-Output ''
    Write-Output '== subject =='
    $r = & $Checker -Board $clean -Ref 'deadbeef000' 2>&1 | Out-String
    $code = $LASTEXITCODE
    if ($code -eq 2 -and $r -match 'does not resolve') { ok 'an unresolvable ref aborts rather than checking a default tree' }
    else { bad "unresolvable ref gave exit $code" }

    $missing = Join-Path $work 'no-such-board.md'
    $r = & $Checker -Board $missing 2>&1 | Out-String
    $code = $LASTEXITCODE
    if ($code -eq 2 -and $r -match 'no board at') { ok 'a missing board aborts rather than reporting zero claims' }
    else { bad "missing board gave exit $code" }
}
finally {
    Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Output ''
if ($fail -eq 0) {
    Write-Output "COVERAGE: every claim fired on both of its distinct payloads ($cases cases)"
    exit 0
} else {
    Write-Output "COVERAGE: $fail of $cases cases failed"
    exit 1
}
