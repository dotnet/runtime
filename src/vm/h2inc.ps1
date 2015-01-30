# ==++==
# 
#  Copyright (c) Microsoft. All rights reserved.
#  Licensed under the MIT license. See LICENSE file in the project root for full license information. 
# 
# ==--==

# C to MASM include file translator
# This is replacement for the deprecated h2inc tool that used to be part of VS.

Function ProcessFile($filePath) {

    Write-Output "// File start: $filePath"

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
                    Write-Output "$name EQU $value"
                } else {
                    Write-Output "$name TEXTEQU <$value>"
                }
            }            
        }
        
        Write-Output $_
    }

    Write-Output "// File end: $filePath"
}

ProcessFile $args[0]
