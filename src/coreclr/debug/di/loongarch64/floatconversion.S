// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>

// Arguments
//     input: (in A0) the value to be converted to a double
//     output: the double corresponding to the _NEON128 input value
LEAF_ENTRY FPFillR8, .TEXT
        ld.d     $a0, $a0, 0
        jirl   $r0, $ra, 0
LEAF_END FPFillR8, .TEXT
