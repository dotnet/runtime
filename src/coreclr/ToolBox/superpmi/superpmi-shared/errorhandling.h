// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

//
// Functions and types used by PAL_TRY-related macros.
//

extern LONG FilterSuperPMIExceptions_CatchMC(PEXCEPTION_POINTERS pExceptionPointers, LPVOID lpvParam);

struct FilterSuperPMIExceptionsParam_CaptureException
{
    DWORD exceptionCode;
    char* exceptionMessage; // 'new' memory passed from ThrowException()

    FilterSuperPMIExceptionsParam_CaptureException()
        : exceptionCode(0)
        , exceptionMessage(nullptr)
    {
    }

    // Note: this is called during an SEH filter; the data pointed to by PEXCEPTION_POINTERS is not valid after
    // calling this function, so anything we want to safe must be copied.
    // The exception message string is 'new' memory, allocated in the ThrowException() function.
    void Initialize(PEXCEPTION_POINTERS pExceptionPointers)
    {
        exceptionCode    = pExceptionPointers->ExceptionRecord->ExceptionCode;
        exceptionMessage = (pExceptionPointers->ExceptionRecord->NumberParameters != 1) ? nullptr : (char*)pExceptionPointers->ExceptionRecord->ExceptionInformation[0];
    }
};

extern LONG FilterSuperPMIExceptions_CaptureExceptionAndContinue(PEXCEPTION_POINTERS pExceptionPointers,
                                                                 LPVOID              lpvParam);
extern LONG FilterSuperPMIExceptions_CaptureExceptionAndStop(PEXCEPTION_POINTERS pExceptionPointers, LPVOID lpvParam);

extern bool RunWithErrorTrap(void (*function)(void*), void* param);

class SpmiException
{
private:
    DWORD exCode;
    char* exMessage;

public:
    SpmiException(FilterSuperPMIExceptionsParam_CaptureException* e);
#if 0
    ~SpmiException();
#endif

    char* GetExceptionMessage();
    DWORD GetCode();

    void ShowAndDeleteMessage();
    void DeleteMessage();
};

#endif
