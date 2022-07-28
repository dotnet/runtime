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

#define AssertMapExists(map, keymsg, ...)                                                                              \
    do                                                                                                                 \
    {                                                                                                                  \
        if (map == nullptr)                                                                                            \
            LogException(EXCEPTIONCODE_MC, "SuperPMI assertion failed (missing map " #map ")" keymsg, ##__VA_ARGS__);  \
    } while (0)

#define AssertKeyExists(map, key, keymsg, ...)                                                                         \
    do                                                                                                                 \
    {                                                                                                                  \
        if (map->GetIndex(key) == -1)                                                                                  \
            LogException(EXCEPTIONCODE_MC, "SuperPMI assertion failed (missing key \"" #key "\" in map " #map ")" keymsg, ##__VA_ARGS__); \
    } while (0)

#define AssertMapAndKeyExist(map, key, keymsg, ...)                                                                    \
    do                                                                                                                 \
    {                                                                                                                  \
        if (map == nullptr)                                                                                            \
            LogException(EXCEPTIONCODE_MC, "SuperPMI assertion failed (missing map " #map ")" keymsg, ##__VA_ARGS__);  \
        if (map->GetIndex(key) == -1)                                                                                  \
            LogException(EXCEPTIONCODE_MC, "SuperPMI assertion failed (missing key \"" #key "\" in map " #map ")" keymsg, ##__VA_ARGS__); \
    } while (0)

// clang doesn't allow for an empty __VA_ARGS__, so we need to pass something non-empty to `LogException`, below.

#define AssertMapExistsNoMessage(map)                                                                                  \
    do                                                                                                                 \
    {                                                                                                                  \
        if (map == nullptr)                                                                                            \
            LogException(EXCEPTIONCODE_MC, "SuperPMI assertion failed (missing map " #map ")", "");                    \
    } while (0)

#define AssertKeyExistsNoMessage(map, key)                                                                             \
    do                                                                                                                 \
    {                                                                                                                  \
        if (map->GetIndex(key) == -1)                                                                                  \
            LogException(EXCEPTIONCODE_MC, "SuperPMI assertion failed (missing key \"" #key "\" in map " #map ")", "");\
    } while (0)

#define AssertMapAndKeyExistNoMessage(map, key)                                                                        \
    do                                                                                                                 \
    {                                                                                                                  \
        if (map == nullptr)                                                                                            \
            LogException(EXCEPTIONCODE_MC, "SuperPMI assertion failed (missing map " #map ")", "");                    \
        if (map->GetIndex(key) == -1)                                                                                  \
            LogException(EXCEPTIONCODE_MC, "SuperPMI assertion failed (missing key \"" #key "\" in map " #map ")", "");\
    } while (0)

#define AssertMsg(expr, msg, ...) AssertCodeMsg(expr, EXCEPTIONCODE_ASSERT, msg, ##__VA_ARGS__)
#define Assert(expr) AssertCode(expr, EXCEPTIONCODE_ASSERT)

//
// Functions and types used by PAL_TRY-related macros.
//

extern bool IsSuperPMIException(unsigned code);

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
        exceptionMessage = nullptr;
        if (IsSuperPMIException(exceptionCode) && (pExceptionPointers->ExceptionRecord->NumberParameters >= 1))
        {
            exceptionMessage = (char*)pExceptionPointers->ExceptionRecord->ExceptionInformation[0];
        }
    }
};

extern LONG FilterSuperPMIExceptions_CaptureExceptionAndStop(PEXCEPTION_POINTERS pExceptionPointers, LPVOID lpvParam);
extern bool RunWithErrorTrap(void (*function)(void*), void* param);
extern bool RunWithSPMIErrorTrap(void (*function)(void*), void* param);
extern void RunWithErrorExceptionCodeCaptureAndContinueImp(void* param, void (*function)(void*), void (*finallyFunction)(void*, DWORD));

template <typename LambdaType>
class LambdaExecutor
{
public:
    LambdaType& _lambda;

    LambdaExecutor(LambdaType& lambda) : _lambda(lambda) {}
};

template <typename LambdaTry, typename LambdaFinally>
void RunWithErrorExceptionCodeCaptureAndContinue(LambdaTry function, LambdaFinally finally)
{
    struct LambdaArguments
    {
        LambdaExecutor<LambdaTry> *pTryLambda;
        LambdaExecutor<LambdaFinally> *pFinallyLambda;
    } lambdaArgs;

    LambdaExecutor<LambdaTry> tryStorage(function);
    LambdaExecutor<LambdaFinally> finallyStorage(finally);

    lambdaArgs.pTryLambda = &tryStorage;
    lambdaArgs.pFinallyLambda = &finallyStorage;
    
    RunWithErrorExceptionCodeCaptureAndContinueImp(&lambdaArgs,
        [](void* pParam)
        {
            ((LambdaArguments*)pParam)->pTryLambda->_lambda();
        },
        [](void* pParam, DWORD exceptionCode)
        {
            ((LambdaArguments*)pParam)->pFinallyLambda->_lambda(exceptionCode);
        });
}

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
