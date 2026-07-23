#!/usr/bin/env pwsh
# One-time helper: seed data-descriptor-meanings.json by porting meanings from existing
# hand-written dependency tables. Unmatched analyzed fields and globals receive TODO placeholders
# so the mechanical migration can be reviewed before new descriptions are authored.
param(
    [string]$Contract,
    [switch]$All,
    [string]$DocsDir = (Join-Path $PSScriptRoot "../../../../../../docs/design/datacontracts"),
    [string]$Json = (Join-Path $PSScriptRoot "output/contract-usage.json"),
    [string]$Out = (Join-Path $PSScriptRoot "../../../../../../docs/design/datacontracts/data-descriptor-meanings.json")
)

if ($All -ne [string]::IsNullOrEmpty($Contract)) {
    throw "Specify exactly one of -Contract or -All."
}

function Normalize-Key($type, $field) {
    $typeAliases = @{
        'ArrayListBlock' = 'ArrayListBase'
        'GCHeapSVR' = 'GCHeap'
        'StressMsgHeader' = 'StressMsg'
    }
    if ($typeAliases.ContainsKey($type)) {
        $type = $typeAliases[$type]
    }
    $type = [regex]::Replace($type, '_\d+$', '').ToLowerInvariant()
    $field = ($field -replace '^m_', '' -replace '^_', '' -replace '_', '').ToLowerInvariant()
    return "$type.$field"
}

function Get-Cells($line) {
    return @($line.Trim().Trim('|').Split('|') | ForEach-Object { ($_ -replace '`', '').Trim() })
}

function Find-CellIndex($cells, $names) {
    for ($i = 0; $i -lt $cells.Count; $i++) {
        if ($cells[$i] -in $names) {
            return $i
        }
    }
    return -1
}

function Read-Existing-Meanings($path) {
    $descriptorMeanings = @{}
    $globalMeanings = @{}
    $lines = Get-Content $path

    for ($i = 0; $i -lt $lines.Count - 1; $i++) {
        if (-not $lines[$i].Trim().StartsWith('|') -or
            -not $lines[$i + 1].Trim().StartsWith('|')) {
            continue
        }

        $header = Get-Cells $lines[$i]
        $descriptorIndex = Find-CellIndex $header @('Data Descriptor Name')
        $globalIndex = Find-CellIndex $header @('Global Name', 'Global name')
        $meaningIndex = Find-CellIndex $header @('Meaning', 'Purpose', 'Description')
        if ($meaningIndex -lt 0 -or ($descriptorIndex -lt 0 -and $globalIndex -lt 0)) {
            continue
        }

        $fieldIndex = Find-CellIndex $header @('Field')
        for ($j = $i + 2; $j -lt $lines.Count -and $lines[$j].Trim().StartsWith('|'); $j++) {
            $cells = Get-Cells $lines[$j]
            if ($cells.Count -le $meaningIndex) {
                continue
            }

            $meaning = $cells[$meaningIndex]
            if ($descriptorIndex -ge 0 -and $cells.Count -gt $descriptorIndex) {
                $type = $cells[$descriptorIndex]
                $field = if ($fieldIndex -ge 0 -and $cells.Count -gt $fieldIndex) {
                    $cells[$fieldIndex]
                } else {
                    'Size'
                }
                if ($type -cmatch '^[A-Z]' -and $field -match '^[A-Za-z_][A-Za-z0-9_]*$') {
                    $key = Normalize-Key $type $field
                    if (-not $descriptorMeanings.ContainsKey($key)) {
                        $descriptorMeanings[$key] = $meaning
                    }
                }
            } elseif ($globalIndex -ge 0 -and $cells.Count -gt $globalIndex) {
                $name = $cells[$globalIndex]
                if ($name -match '^[A-Za-z_][A-Za-z0-9_]*$' -and
                    -not $globalMeanings.ContainsKey($name)) {
                    $globalMeanings[$name] = $meaning
                }
            }
        }
    }

    return @{
        Descriptors = $descriptorMeanings
        Globals = $globalMeanings
    }
}

$graph = Get-Content $Json -Raw | ConvertFrom-Json
$groups = @($graph | Group-Object contract | Sort-Object Name)
if (-not $All) {
    $groups = @($groups | Where-Object { $_.Name -eq "I$Contract" })
    if ($groups.Count -ne 1) {
        throw "No tool data for I$Contract."
    }
}

$root = [ordered]@{}
if (Test-Path $Out) {
    $root = Get-Content $Out -Raw | ConvertFrom-Json -AsHashtable
}
if (-not $root.Contains('_fields')) {
    $root['_fields'] = [ordered]@{}
}
if (-not $root.Contains('_globals')) {
    $root['_globals'] = [ordered]@{}
}

$todoKeys = [System.Collections.Generic.List[string]]::new()
foreach ($group in $groups) {
    $contractName = $group.Name.Substring(1)
    $docPath = Join-Path $DocsDir "$contractName.md"
    if (-not (Test-Path $docPath)) {
        throw "Missing contract document '$docPath'."
    }
    if ($All -and (Select-String -Path $docPath -Pattern 'BEGIN GENERATED: usage' -Quiet)) {
        continue
    }
    $existing = Read-Existing-Meanings $docPath
    $existing = Read-Existing-Meanings $docPath
    $fieldKeys = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $globalNames = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)

    foreach ($entry in $group.Group) {
        foreach ($typeProperty in $entry.fieldUsage.PSObject.Properties) {
            $type = $typeProperty.Name -replace '^Data\.', ''
            foreach ($field in $typeProperty.Value.PSObject.Properties.Name) {
                [void]$fieldKeys.Add("$type.$field")
            }
        }
        foreach ($global in $entry.globalsUsed.PSObject.Properties.Name) {
            [void]$globalNames.Add($global)
        }
    }

    foreach ($key in ($fieldKeys | Sort-Object)) {
        if ($root['_fields'].Contains($key)) {
            continue
        }

        $dot = $key.LastIndexOf('.')
        $meaning = $existing.Descriptors[
            (Normalize-Key $key.Substring(0, $dot) $key.Substring($dot + 1))]
        if ([string]::IsNullOrEmpty($meaning)) {
            $meaning = '_TODO: describe_'
            $todoKeys.Add("$contractName descriptor $key")
        }
        $root['_fields'][$key] = $meaning
    }

    foreach ($name in ($globalNames | Sort-Object)) {
        if ($root['_globals'].Contains($name)) {
            continue
        }

        $meaning = $existing.Globals[$name]
        if ([string]::IsNullOrEmpty($meaning)) {
            $meaning = '_TODO: describe_'
            $todoKeys.Add("$contractName global $name")
        }
        $root['_globals'][$name] = $meaning
    }

    Write-Output (
        "Scanned {0}: {1} descriptors, {2} globals." -f
        $contractName,
        $fieldKeys.Count,
        $globalNames.Count)
}

$orderedRoot = [ordered]@{}
foreach ($key in ($root.Keys | Where-Object { $_.StartsWith('_') } | Sort-Object)) {
    if ($key -in @('_fields', '_globals')) {
        $orderedValues = [ordered]@{}
        foreach ($name in ($root[$key].Keys | Sort-Object)) {
            $orderedValues[$name] = $root[$key][$name]
        }
        $orderedRoot[$key] = $orderedValues
    } else {
        $orderedRoot[$key] = $root[$key]
    }
}

$jsonText = $orderedRoot | ConvertTo-Json -Depth 8
$outPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Out)
[IO.File]::WriteAllText($outPath, $jsonText + [Environment]::NewLine)
Write-Output "Wrote $Out with $($todoKeys.Count) TODO meaning(s)."
foreach ($key in $todoKeys) {
    Write-Output "  TODO $key"
}
