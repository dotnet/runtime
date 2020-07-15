# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

# C to MASM include file translator
# This is replacement for the deprecated h2inc tool that used to be part of VS.

#
# The use of [console]::WriteLine (instead of Write-Output) is intentional.
# PowerShell 2.0 (installed by default on Windows 7) wraps lines written with
# Write-Output at whatever column width is being used by the current terminal,
# even when output is being redirected to a file. We can't have this behavior
# because it will cause the generated file to be malformed.
#

Function ProcessFile($filePath) {

    [console]::WriteLine("// File start: $filePath")

    Get-Content $filePath | ForEach-Object {

        if ($_ -match "^\s*#\spragma") {
            # Ignore pragmas
            return
        }

        if ($_ -match "^\s*#\s*include\s*`"(.*)`"")
        {
            # Expand includes.
            ProcessFile(Join-Path (Split-Path -Parent $filePath) $Matches[1])
            return
        }

        if ($_ -match "^\s*#define\s+(\S+)\s*(.*)")
        {
            # Augment #defines with their MASM equivalent
            $name = $Matches[1]
            $value = $Matches[2]

            # Note that we do not handle multiline constants

            # Strip comments from value
            $value = $value -replace "//.*", ""
            $value = $value -replace "/\*.*\*/", ""

            # Strip whitespaces from value
            $value = $value -replace "\s+$", ""

            # ignore #defines with arguments
            if ($name -notmatch "\(") {
                $HEX_NUMBER_PATTERN = "\b0x(\w+)\b"
                $DECIMAL_NUMBER_PATTERN = "(-?\b\d+\b)"

                if ($value -match $HEX_NUMBER_PATTERN -or $value -match $DECIMAL_NUMBER_PATTERN) {
                    $value = $value -replace $HEX_NUMBER_PATTERN, "0`$1h"    # Convert hex constants
                    $value = $value -replace $DECIMAL_NUMBER_PATTERN, "`$1t" # Convert dec constants
                    [console]::WriteLine("$name EQU $value")
                } else {
                    [console]::WriteLine("$name TEXTEQU <$value>")
                }
            }
        }

        [console]::WriteLine("$_")
    }

    [console]::WriteLine("// File end: $filePath")
}

ProcessFile $args[0]
