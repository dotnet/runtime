// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <unixasmmacros.inc>

// Arguments
//     input: (in R0) the adress of the ULONGLONG to be converted to a double
//     output: the double corresponding to the ULONGLONG input value

LEAF_ENTRY FPFillR8, .TEXT
        .thumb
        vldr  D0, [R0]
        bx   lr  
LEAF_END FPFillR8, .TEXT