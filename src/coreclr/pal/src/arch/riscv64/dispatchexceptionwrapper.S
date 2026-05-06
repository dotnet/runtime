// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Implementation of the PAL_DispatchExceptionWrapper that is
// interposed between a function that caused a hardware fault
// and PAL_DispatchException that throws an SEH exception for
// the fault, to make the stack unwindable.
//

#include "unixasmmacros.inc"

// #error "TODO-RISCV64: missing implementation"
