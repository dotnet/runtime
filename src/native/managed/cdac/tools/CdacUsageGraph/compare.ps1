#!/usr/bin/env pwsh
# Compare the tool's contract->Data usage graph against the hand-written
# docs/design/datacontracts/*.md data-descriptor tables.
# Paths default relative to this script's location within the repo:
#   src/native/managed/cdac/tools/CdacUsageGraph -> repo root is 6 levels up.
param(
    [string]$DocsDir = (Join-Path $PSScriptRoot "../../../../../../docs/design/datacontracts"),
    [string]$Json    = (Join-Path $PSScriptRoot "output/contract-usage.json"),
    [string]$OutFile = (Join-Path $PSScriptRoot "output/doc-comparison.md")
)

# ---- 1. Load tool output, union types + (type,field) per contract interface ----
$data = Get-Content $Json -Raw | ConvertFrom-Json
$toolTypes  = @{}   # contract -> set of data type names
$toolFields = @{}   # contract -> set of "Type.Field"
foreach ($e in $data) {
    $c = $e.contract
    if (-not $toolTypes.ContainsKey($c))  { $toolTypes[$c]  = [System.Collections.Generic.HashSet[string]]::new() }
    if (-not $toolFields.ContainsKey($c)) { $toolFields[$c] = [System.Collections.Generic.HashSet[string]]::new() }
    foreach ($t in $e.dataTypes) { [void]$toolTypes[$c].Add(($t -replace '^Data\.','')) }
    if ($e.fieldUsage) {
        foreach ($p in $e.fieldUsage.PSObject.Properties) {
            $tn = $p.Name -replace '^Data\.',''
            foreach ($f in $p.Value.PSObject.Properties) { [void]$toolFields[$c].Add("$tn.$($f.Name)") }
        }
    }
}

# ---- 2. Parse each doc's data-descriptor (Type, Field) rows ----
# Header-driven: find a "Data Descriptor Name | Field" header, then read the
# following table rows. Type/Field cells may or may not be backtick-wrapped,
# and the table may have extra columns (e.g. GC.md adds a Source column).
$skip = @('contract-descriptor','data_descriptor','datacontracts_design','debug_interface_globals','enums','GCHandle')

function Clean($s) { return ($s -replace '`','').Trim() }

$docTypes  = @{}
$docFields = @{}
foreach ($md in Get-ChildItem $DocsDir -Filter *.md) {
    $base = [System.IO.Path]::GetFileNameWithoutExtension($md.Name)
    if ($skip -contains $base) { continue }
    $contract = "I$base"
    $dt = [System.Collections.Generic.HashSet[string]]::new()
    $df = [System.Collections.Generic.HashSet[string]]::new()
    $lines = Get-Content $md.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match 'Data Descriptor Name' -and $lines[$i] -match 'Field') {
            $j = $i + 2   # skip header + separator row
            while ($j -lt $lines.Count -and $lines[$j].Trim().StartsWith('|')) {
                $cells = $lines[$j].Trim().Trim('|').Split('|')
                $type  = Clean $cells[0]
                $field = if ($cells.Count -ge 2) { Clean $cells[1] } else { '' }
                if ($type -cmatch '^[A-Z][A-Za-z0-9_]*$' -and $type -ne '---') {
                    [void]$dt.Add($type)
                    if ($field -match '^[A-Za-z_][A-Za-z0-9_]*$') { [void]$df.Add("$type.$field") }
                }
                $j++
            }
            $i = $j
        }
    }
    $docTypes[$contract]  = $dt
    $docFields[$contract] = $df
}

# ---- 3. Diff and emit ----
# Normalization so naming-only differences collapse:
#  - type aliases (doc conceptual name vs cDAC class name)
#  - strip trailing _<version> on types (StubPrecodeData_1 -> StubPrecodeData)
#  - field names: drop m_/_ prefixes and underscores, lowercase (m_NumComponents == NumComponents)
$typeAlias = @{
    'GCHeapSVR'      = 'GCHeap'
    'StressMsgHeader'= 'StressMsg'
    'ArrayListBlock' = 'ArrayListBase'
}
function NormType($t) {
    if ($typeAlias.ContainsKey($t)) { $t = $typeAlias[$t] }
    $t = [regex]::Replace($t, '_\d+$', '')
    return $t.ToLowerInvariant()
}
function NormField($f) {
    $x = $f -replace '^m_','' -replace '^_','' -replace '_',''
    return $x.ToLowerInvariant()
}
function NormKey($x) {
    $dot = $x.LastIndexOf('.')
    if ($dot -lt 0) { return (NormType $x) }
    return (NormType $x.Substring(0,$dot)) + '.' + (NormField $x.Substring($dot+1))
}
function DiffSet($a, $b) {
    if ($null -eq $a) { $a = [System.Collections.Generic.HashSet[string]]::new() }
    if ($null -eq $b) { $b = [System.Collections.Generic.HashSet[string]]::new() }
    $am = [ordered]@{}; foreach ($x in $a) { $k = NormKey $x; if (-not $am.Contains($k)) { $am[$k] = $x } }
    $bm = [ordered]@{}; foreach ($x in $b) { $k = NormKey $x; if (-not $bm.Contains($k)) { $bm[$k] = $x } }
    $both = [System.Collections.Generic.List[string]]::new()
    $aOnly = [System.Collections.Generic.List[string]]::new()
    $bOnly = [System.Collections.Generic.List[string]]::new()
    foreach ($k in $am.Keys) { if ($bm.Contains($k)) { $both.Add($am[$k]) } else { $aOnly.Add($am[$k]) } }
    foreach ($k in $bm.Keys) { if (-not $am.Contains($k)) { $bOnly.Add($bm[$k]) } }
    return @{ Both = ($both | Sort-Object); DocOnly = ($aOnly | Sort-Object); ToolOnly = ($bOnly | Sort-Object) }
}

$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine("# Tool Output vs. docs/design/datacontracts Comparison")
[void]$sb.AppendLine()
[void]$sb.AppendLine("- **DocOnly** = documented as a dependency but the tool did not detect it. Raw-string ``Fields[""..""]``/constant lookups are now captured (inline and via correlated locals/fields), so remaining DocOnly are genuine drift, cross-contract dependencies (attributed to the owning contract), naming variants, or field access via indirect TypeInfo flows the tool doesn't trace.")
[void]$sb.AppendLine("- **ToolOnly** = the tool found usage that the docs do not list (doc drift/under-documentation, or tool over-approximation via helper walking).")
[void]$sb.AppendLine("- Names are normalized before diffing: type aliases (e.g. ``GCHeapSVR``=``GCHeap``), trailing ``_<version>`` stripped (``StubPrecodeData_1``=``StubPrecodeData``), and field names compared case-insensitively without ``m_``/``_`` prefixes (``m_NumComponents``=``NumComponents``). Remaining diffs are genuine, apart from a few irregular naming variants (e.g. ``ThrownObjectHandle`` vs ``ThrownObject``).")
[void]$sb.AppendLine()

# Type-level summary
[void]$sb.AppendLine("## Data-type level")
[void]$sb.AppendLine()
[void]$sb.AppendLine("| Contract | Match | DocOnly | ToolOnly | DocOnly types | ToolOnly types |")
[void]$sb.AppendLine("|---|--:|--:|--:|---|---|")
$allContracts = ($docTypes.Keys + $toolTypes.Keys | Sort-Object -Unique)
foreach ($c in $allContracts) {
    $d = if ($docTypes.ContainsKey($c))  { $docTypes[$c] }  else { [System.Collections.Generic.HashSet[string]]::new() }
    $t = if ($toolTypes.ContainsKey($c)) { $toolTypes[$c] } else { [System.Collections.Generic.HashSet[string]]::new() }
    $r = DiffSet $d $t
    $docHas = $docTypes.ContainsKey($c); $toolHas = $toolTypes.ContainsKey($c)
    $note = ""
    if (-not $docHas)  { $note = " (no doc)" }
    if (-not $toolHas) { $note = " (not registered)" }
    [void]$sb.AppendLine("| $c$note | $($r.Both.Count) | $($r.DocOnly.Count) | $($r.ToolOnly.Count) | $($r.DocOnly -join ', ') | $($r.ToolOnly -join ', ') |")
}

# Field-level summary
[void]$sb.AppendLine()
[void]$sb.AppendLine("## Field level (Type.Field)")
[void]$sb.AppendLine()
[void]$sb.AppendLine("| Contract | Match | DocOnly | ToolOnly |")
[void]$sb.AppendLine("|---|--:|--:|--:|")
foreach ($c in $allContracts) {
    $d = if ($docFields.ContainsKey($c))  { $docFields[$c] }  else { [System.Collections.Generic.HashSet[string]]::new() }
    $t = if ($toolFields.ContainsKey($c)) { $toolFields[$c] } else { [System.Collections.Generic.HashSet[string]]::new() }
    if ($d.Count -eq 0 -and $t.Count -eq 0) { continue }
    $r = DiffSet $d $t
    [void]$sb.AppendLine("| $c | $($r.Both.Count) | $($r.DocOnly.Count) | $($r.ToolOnly.Count) |")
}

# Field-level detail (only where there are differences)
[void]$sb.AppendLine()
[void]$sb.AppendLine("## Field-level differences (detail)")
foreach ($c in $allContracts) {
    $d = if ($docFields.ContainsKey($c))  { $docFields[$c] }  else { [System.Collections.Generic.HashSet[string]]::new() }
    $t = if ($toolFields.ContainsKey($c)) { $toolFields[$c] } else { [System.Collections.Generic.HashSet[string]]::new() }
    $r = DiffSet $d $t
    if ($r.DocOnly.Count -eq 0 -and $r.ToolOnly.Count -eq 0) { continue }
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("### $c")
    if ($r.DocOnly.Count)  { [void]$sb.AppendLine("- DocOnly: $($r.DocOnly -join ', ')") }
    if ($r.ToolOnly.Count) { [void]$sb.AppendLine("- ToolOnly: $($r.ToolOnly -join ', ')") }
}

Set-Content -Path $OutFile -Value $sb.ToString()
Write-Output "Wrote $OutFile"
Write-Output ""
# console summary
Write-Output "Data-type level (Match / DocOnly / ToolOnly):"
foreach ($c in $allContracts) {
    $d = if ($docTypes.ContainsKey($c))  { $docTypes[$c] }  else { [System.Collections.Generic.HashSet[string]]::new() }
    $t = if ($toolTypes.ContainsKey($c)) { $toolTypes[$c] } else { [System.Collections.Generic.HashSet[string]]::new() }
    $r = DiffSet $d $t
    "{0,-28} {1,3} / {2,3} / {3,3}" -f $c, $r.Both.Count, $r.DocOnly.Count, $r.ToolOnly.Count
}
