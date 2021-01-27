//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//----------------------------------------------------------
// ErrorHandling.h - Helpers & whatnot for using SEH for errors
//----------------------------------------------------------
#ifndef _ErrorHandling
#define _ErrorHandling

#include "logging.h"

// EXCEPTIONCODE_DebugBreakorAV is just the base exception number; calls to DebugBreakorAV()
// pass a unique number to add to this. EXCEPTIONCODE_DebugBreakorAV_MAX is the maximum number
// of this exception range.
#define EXCEPTIONCODE_DebugBreakorAV 0xe0421000
#define EXCEPTIONCODE_DebugBreakorAV_MAX 0xe0422000

#define EXCEPTIONCODE_MC 0xe0422000
#define EXCEPTIONCODE_LWM 0xe0423000
#define EXCEPTIONCODE_CALLUTILS 0xe0426000
#define EXCEPTIONCODE_TYPEUTILS 0xe0427000
#define EXCEPTIONCODE_ASSERT 0xe0440000

// RaiseException wrappers
void MSC_ONLY(__declspec(noreturn)) ThrowException(DWORD exceptionCode);
void MSC_ONLY(__declspec(noreturn)) ThrowException(DWORD exceptionCode, const char* message, ...);

// Assert stuff
#define AssertCodeMsg(expr, exCode, msg, ...)                                                                          \
    do                                                                                                                 \
    {                                                                                                                  \
        if (!(expr))                                                                                                   \
            LogException(exCode, "SuperPMI assertion '%s' failed (" #msg ")", #expr, ##__VA_ARGS__);                   \
    } while (0)

#define AssertCode(expr, exCode)                                                                                       \
    do                                                                                                                 \
    {                                                                                                                  \
        if (!(expr))                                                                                                   \
            LogException(exCode, "SuperPMI assertion '%s' failed", #expr);                                             \
    } while (0)

#define AssertMsg(expr, msg, ...) AssertCodeMsg(expr, EXCEPTIONCODE_ASSERT, msg, ##__VA_ARGS__)
#define Assert(expr) AssertCode(expr, EXCEPTIONCODE_ASSERT)

class SpmiException
{
private:
    DWORD exCode;
    char* exMessage;

public:
    SpmiException(PEXCEPTION_POINTERS exp);
    SpmiException(DWORD exceptionCode, char* exceptionMessage);
#if 0
    ~SpmiException();
#endif

    char* GetExceptionMessage();
    DWORD GetCode();

    void ShowAndDeleteMessage();
    void DeleteMessage();
};

//
// Functions and types used by PAL_TRY-related macros.
//

extern LONG FilterSuperPMIExceptions_CatchMC(PEXCEPTION_POINTERS pExceptionPointers, LPVOID lpvParam);

struct FilterSuperPMIExceptionsParam_CaptureException
{
    EXCEPTION_POINTERS exceptionPointers;
    DWORD              exceptionCode;

    FilterSuperPMIExceptionsParam_CaptureException() : exceptionCode(0)
    {
        exceptionPointers.ExceptionRecord = nullptr;
        exceptionPointers.ContextRecord   = nullptr;
    }
};

extern LONG FilterSuperPMIExceptions_CaptureExceptionAndContinue(PEXCEPTION_POINTERS pExceptionPointers,
                                                                 LPVOID              lpvParam);
extern LONG FilterSuperPMIExceptions_CaptureExceptionAndStop(PEXCEPTION_POINTERS pExceptionPointers, LPVOID lpvParam);

extern bool RunWithErrorTrap(void (*function)(void*), void* param);

#endif
