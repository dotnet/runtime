// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "common.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "PalRedhawkCommon.h"
#include "PalRedhawk.h"
#include "rhassert.h"


#include "RhConfig.h"

#ifdef _DEBUG

#define MB_ABORTRETRYIGNORE         0x00000002L
#define IDABORT             3
#define IDRETRY             4
#define IDIGNORE            5

void Assert(const char * expr, const char * file, uint32_t line_num, const char * message)
{
#ifndef DACCESS_COMPILE
#ifdef NO_UI_ASSERT
    PalDebugBreak();
#else
    if (g_pRhConfig->GetBreakOnAssert())
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
        return;
    }

    char buffer[4096];

    sprintf_s(buffer, ARRAY_SIZE(buffer),
           "--------------------------------------------------\n"
           "Debug Assertion Violation\n\n"
           "%s%s%s"
           "Expression: '%s'\n\n"
           "File: %s, Line: %u\n"
           "--------------------------------------------------\n"
           "Abort: Exit Immediately\n"
           "Retry: DebugBreak()\n"
           "Ignore: Keep Going\n"
           "--------------------------------------------------\n",
           message ? ("Message: ") : (""),
           message ? (message) : (""),
           message ? ("\n\n") : (""),
           expr, file, line_num);

    HANDLE hMod = PalLoadLibraryExW(L"user32.dll", NULL, 0);
    int32_t (* pfn)(HANDLE, char *, const char *, uint32_t) =
        (int32_t (*)(HANDLE, char *, const char *, uint32_t))PalGetProcAddress(hMod, "MessageBoxA");

    int32_t result = pfn(NULL, buffer, "Redhawk Assert", MB_ABORTRETRYIGNORE);

    switch (result)
    {
    case IDABORT:
        PalTerminateCurrentProcess(42);
        break;
    case IDRETRY:
        PalDebugBreak();
        break;
    case IDIGNORE:
        break;
    }
#endif
#else
    UNREFERENCED_PARAMETER(expr);
    UNREFERENCED_PARAMETER(file);
    UNREFERENCED_PARAMETER(line_num);
    UNREFERENCED_PARAMETER(message);
#endif //!DACCESS_COMPILE
}

#endif // _DEBUG
