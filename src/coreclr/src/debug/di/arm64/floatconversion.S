// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>

// Arguments
//     input: (in X0) the _NEON128 value to be converted to a double
//     output: the double corresponding to the _NEON128 input value

LEAF_ENTRY FPFillR8, .TEXT
        LDR     Q0, [X0]
        ret     lr
LEAF_END FPFillR8, .TEXT
