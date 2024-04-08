// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "common.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "PalRedhawkCommon.h"
#include "PalRedhawk.h"
#include "rhassert.h"

#ifdef _DEBUG

void Assert(const char * expr, const char * file, uint32_t line_num, const char * message)
{
    printf(
        "--------------------------------------------------\n"
        "Debug Assertion Violation\n\n"
        "%s%s%s"
        "Expression: '%s'\n\n"
        "File: %s, Line: %u\n"
        "--------------------------------------------------\n",
        message ? ("Message: ") : (""),
        message ? (message) : (""),
        message ? ("\n\n") : (""),
        expr, file, line_num);

    // Flush standard output before failing fast to make sure the assertion failure message
    // is retained when tests are being run with redirected stdout.
    fflush(stdout);

    // If there's no debugger attached, we just FailFast
    if (!PalIsDebuggerPresent())
        PalRaiseFailFastException(NULL, NULL, FAIL_FAST_GENERATE_EXCEPTION_ADDRESS);

    // If there is a debugger attached, we break and then allow continuation.
    PalDebugBreak();
}

#endif // _DEBUG
