//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include "standardpch.h"
#include "errorhandling.h"
#include "logging.h"
#include "runtimedetails.h"

void MSC_ONLY(__declspec(noreturn)) ThrowException(DWORD exceptionCode)
{
    RaiseException(exceptionCode, 0, 0, nullptr);
}

// Allocating memory here seems moderately dangerous: we'll probably leak like a sieve...
void MSC_ONLY(__declspec(noreturn)) ThrowException(DWORD exceptionCode, va_list args, const char* message)
{
    char*      buffer = new char[8192];
    ULONG_PTR* ptr    = new ULONG_PTR();
    *ptr              = (ULONG_PTR)buffer;
    _vsnprintf_s(buffer, 8192, 8191, message, args);

    RaiseException(exceptionCode, 0, 1, ptr);
}

void MSC_ONLY(__declspec(noreturn)) ThrowException(DWORD exceptionCode, const char* msg, ...)
{
    va_list ap;
    va_start(ap, msg);
    ThrowException(exceptionCode, ap, msg);
}

SpmiException::SpmiException(PEXCEPTION_POINTERS exp) : exCode(exp->ExceptionRecord->ExceptionCode)
{
    exMessage =
        (exp->ExceptionRecord->NumberParameters != 1) ? nullptr : (char*)exp->ExceptionRecord->ExceptionInformation[0];
}

SpmiException::SpmiException(DWORD exceptionCode, char* exceptionMessage)
    : exCode(exceptionCode), exMessage(exceptionMessage)
{
}

#if 0
SpmiException::~SpmiException()
{
    delete[] exMessage;
    exMessage = nullptr;
}
#endif

char* SpmiException::GetExceptionMessage()
{
    return exMessage;
}

void SpmiException::ShowAndDeleteMessage()
{
    if (exMessage != nullptr)
    {
        LogError("Exception thrown: %s", exMessage);
        delete[] exMessage;
        exMessage = nullptr;
    }
    else
    {
        LogError("Unexpected exception was thrown.");
    }
}

void SpmiException::DeleteMessage()
{
    delete[] exMessage;
    exMessage = nullptr;
}

DWORD SpmiException::GetCode()
{
    return exCode;
}

// This filter function executes the handler only for EXCEPTIONCODE_MC, otherwise it continues the handler search.
LONG FilterSuperPMIExceptions_CatchMC(PEXCEPTION_POINTERS pExceptionPointers, LPVOID lpvParam)
{
    return (pExceptionPointers->ExceptionRecord->ExceptionCode == EXCEPTIONCODE_MC) ? EXCEPTION_EXECUTE_HANDLER
                                                                                    : EXCEPTION_CONTINUE_SEARCH;
}

// This filter function captures the exception pointers and continues searching.
LONG FilterSuperPMIExceptions_CaptureExceptionAndContinue(PEXCEPTION_POINTERS pExceptionPointers, LPVOID lpvParam)
{
    FilterSuperPMIExceptionsParam_CaptureException* pSPMIEParam =
        (FilterSuperPMIExceptionsParam_CaptureException*)lpvParam;
    pSPMIEParam->exceptionPointers = *pExceptionPointers; // Capture the exception pointers for use later
    pSPMIEParam->exceptionCode     = pSPMIEParam->exceptionPointers.ExceptionRecord->ExceptionCode;
    return EXCEPTION_CONTINUE_SEARCH;
}

LONG FilterSuperPMIExceptions_CaptureExceptionAndStop(PEXCEPTION_POINTERS pExceptionPointers, LPVOID lpvParam)
{
    FilterSuperPMIExceptionsParam_CaptureException* pSPMIEParam =
        (FilterSuperPMIExceptionsParam_CaptureException*)lpvParam;
    pSPMIEParam->exceptionPointers = *pExceptionPointers; // Capture the exception pointers for use later
    pSPMIEParam->exceptionCode     = pSPMIEParam->exceptionPointers.ExceptionRecord->ExceptionCode;
    return EXCEPTION_EXECUTE_HANDLER;
}

bool IsSuperPMIException(unsigned code)
{
    switch (code)
    {
        case EXCEPTIONCODE_MC:
        case EXCEPTIONCODE_LWM:
        case EXCEPTIONCODE_CALLUTILS:
        case EXCEPTIONCODE_TYPEUTILS:
        case EXCEPTIONCODE_ASSERT:
            return true;
        default:
            if ((EXCEPTIONCODE_DebugBreakorAV <= code) && (code < EXCEPTIONCODE_DebugBreakorAV_MAX))
            {
                return true;
            }
            return false;
    }
}

// This filter function executes the handler only for non-SuperPMI generated exceptions, otherwise it continues the
// handler search. This allows for SuperPMI-thrown exceptions to pass through the JIT and be caught by the outer
// SuperPMI handler.
LONG FilterSuperPMIExceptions_CatchNonSuperPMIException(PEXCEPTION_POINTERS pExceptionPointers, LPVOID lpvParam)
{
    return !IsSuperPMIException(pExceptionPointers->ExceptionRecord->ExceptionCode);
}

bool RunWithErrorTrap(void (*function)(void*), void* param)
{
    bool success = true;

    struct TrapParam
    {
        void (*function)(void*);
        void* param;
    } trapParam;
    trapParam.function = function;
    trapParam.param    = param;

    PAL_TRY(TrapParam*, pTrapParam, &trapParam)
    {
        pTrapParam->function(pTrapParam->param);
    }
    PAL_EXCEPT_FILTER(FilterSuperPMIExceptions_CatchNonSuperPMIException)
    {
        success = false;
    }
    PAL_ENDTRY

    return success;
}
