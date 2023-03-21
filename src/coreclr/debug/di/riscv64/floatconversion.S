// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>

// Arguments
//     input: (in A0) the address of the ULONGLONG to be converted to a double
//     output: the double corresponding to the ULONGLONG input value
LEAF_ENTRY FPFillR8, .TEXT
        fld  fa0, 0(a0)
        ret
LEAF_END FPFillR8, .TEXT
