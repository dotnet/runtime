// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"
#include "asmconstants.h"

//
// METHODDESC_REGISTER: UMEntryThunk*
//
NESTED_ENTRY TheUMEntryPrestub, _TEXT, UnhandledExceptionHandlerUnix
// NOT YET IMPLEMENTED
    bl abort 
    nop
NESTED_END TheUMEntryPrestub, _TEXT
