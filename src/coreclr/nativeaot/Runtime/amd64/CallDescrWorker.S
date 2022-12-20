// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include <unixasmmacros.inc>

NESTED_ENTRY RhCallDescrWorker, _TEXT, NoHandler

    EXPORT_POINTER_TO_ADDRESS PointerToReturnFromCallDescrThunk

    // UNIXTODO: Implement this function
    int 3
NESTED_END RhCallDescrWorker, _TEXT
