// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <palsuite.h>

enum class SignalableObjectType
{
    First = 0,

    Invalid = First,
    ManualResetEvent,
    AutoResetEvent,
    Semaphore,
    FullSemaphore,
    Mutex,
    UnlockedMutex,

    Last = UnlockedMutex
};

enum class WaitableObjectType
{
    First = 0,

    Invalid = First,
    ManualResetEvent,
    UnsignaledManualResetEvent,
    AutoResetEvent,
    UnsignaledAutoResetEvent,
    Semaphore,
    EmptySemaphore,
    Mutex,
    LockedMutex,

    Last = LockedMutex
};

void operator ++(SignalableObjectType &objectType)
{
    ++(int &)objectType;
}

void operator ++(WaitableObjectType &objectType)
{
    ++(int &)objectType;
}

struct AssertionFailureException
{
    const int lineNumber;
    const char *const expression;
    SignalableObjectType signalableObjectType;
    WaitableObjectType waitableObjectType;
    DWORD waitResult;
    DWORD errorCode;

    AssertionFailureException(int lineNumber, const char *expression)
        : lineNumber(lineNumber),
        expression(expression),
        signalableObjectType(SignalableObjectType::Invalid),
        waitableObjectType(WaitableObjectType::Invalid),
        waitResult(WAIT_OBJECT_0),
        errorCode(ERROR_SUCCESS)
    {
    }
};

#define TestAssert(expression) \
    do \
    { \
        if (!(expression)) \
        { \
            throw AssertionFailureException(__LINE__, "" #expression ""); \
        } \
    } while (false)

HANDLE CreateObjectToSignal(SignalableObjectType objectType)
{
    switch (objectType)
    {
        case SignalableObjectType::Invalid:
            return nullptr;

        case SignalableObjectType::ManualResetEvent:
            return CreateEvent(nullptr, true, false, nullptr);

        case SignalableObjectType::AutoResetEvent:
            return CreateEvent(nullptr, false, false, nullptr);

        case SignalableObjectType::Semaphore:
            return CreateSemaphoreExW(nullptr, 0, 1, nullptr, 0, 0);

        case SignalableObjectType::FullSemaphore:
            return CreateSemaphoreExW(nullptr, 1, 1, nullptr, 0, 0);

        case SignalableObjectType::Mutex:
            return CreateMutex(nullptr, true, nullptr);

        case SignalableObjectType::UnlockedMutex:
            return CreateMutex(nullptr, false, nullptr);

        default:
            TestAssert(false);
    }
}

void VerifySignal(HANDLE h, SignalableObjectType objectType)
{
    switch (objectType)
    {
        case SignalableObjectType::ManualResetEvent:
            TestAssert(WaitForSingleObject(h, 0) == WAIT_OBJECT_0);
            break;

        case SignalableObjectType::AutoResetEvent:
            TestAssert(WaitForSingleObject(h, 0) == WAIT_OBJECT_0);
            SetEvent(h);
            break;

        case SignalableObjectType::Semaphore:
            TestAssert(!ReleaseSemaphore(h, 1, nullptr));
            break;

        case SignalableObjectType::Mutex:
            TestAssert(!ReleaseMutex(h));
            break;

        default:
            TestAssert(false);
    }
}

void CloseObjectToSignal(HANDLE h, SignalableObjectType objectType)
{
    if (objectType != SignalableObjectType::Invalid)
    {
        CloseHandle(h);
    }
}

HANDLE CreateObjectToWaitOn(WaitableObjectType objectType)
{
    switch (objectType)
    {
        case WaitableObjectType::Invalid:
            return nullptr;

        case WaitableObjectType::ManualResetEvent:
            return CreateEvent(nullptr, true, true, nullptr);

        case WaitableObjectType::UnsignaledManualResetEvent:
            return CreateEvent(nullptr, true, false, nullptr);

        case WaitableObjectType::AutoResetEvent:
            return CreateEvent(nullptr, false, true, nullptr);

        case WaitableObjectType::UnsignaledAutoResetEvent:
            return CreateEvent(nullptr, false, false, nullptr);

        case WaitableObjectType::Semaphore:
            return CreateSemaphoreExW(nullptr, 1, 1, nullptr, 0, 0);

        case WaitableObjectType::EmptySemaphore:
            return CreateSemaphoreExW(nullptr, 0, 1, nullptr, 0, 0);

        case WaitableObjectType::Mutex:
            return CreateMutex(nullptr, false, nullptr);

        case WaitableObjectType::LockedMutex:
            return CreateMutex(nullptr, true, nullptr);

        default:
            TestAssert(false);
    }
}

void VerifyWait(HANDLE h, WaitableObjectType objectType)
{
    switch (objectType)
    {
        case WaitableObjectType::ManualResetEvent:
        case WaitableObjectType::UnsignaledManualResetEvent:
            break;

        case WaitableObjectType::AutoResetEvent:
        case WaitableObjectType::UnsignaledAutoResetEvent:
        case WaitableObjectType::Semaphore:
        case WaitableObjectType::EmptySemaphore:
            TestAssert(WaitForSingleObject(h, 0) == WAIT_TIMEOUT);
            break;

        case WaitableObjectType::Mutex:
            TestAssert(ReleaseMutex(h));
            TestAssert(!ReleaseMutex(h));
            TestAssert(WaitForSingleObject(h, 0) == WAIT_OBJECT_0);
            break;

        case WaitableObjectType::LockedMutex:
            TestAssert(ReleaseMutex(h));
            TestAssert(ReleaseMutex(h));
            TestAssert(!ReleaseMutex(h));
            TestAssert(WaitForSingleObject(h, 0) == WAIT_OBJECT_0);
            TestAssert(WaitForSingleObject(h, 0) == WAIT_OBJECT_0);
            break;

        default:
            TestAssert(false);
    }
}

void CloseObjectToWaitOn(HANDLE h, WaitableObjectType objectType)
{
    switch (objectType)
    {
        case WaitableObjectType::ManualResetEvent:
        case WaitableObjectType::UnsignaledManualResetEvent:
        case WaitableObjectType::AutoResetEvent:
        case WaitableObjectType::UnsignaledAutoResetEvent:
            CloseHandle(h);
            break;

        case WaitableObjectType::Semaphore:
        case WaitableObjectType::EmptySemaphore:
            ReleaseSemaphore(h, 1, nullptr);
            CloseHandle(h);
            break;

        case WaitableObjectType::Mutex:
            ReleaseMutex(h);
            CloseHandle(h);
            break;

        case WaitableObjectType::LockedMutex:
            ReleaseMutex(h);
            ReleaseMutex(h);
            CloseHandle(h);
            break;

        default:
            break;
    }
}

bool Verify(SignalableObjectType signalableObjectType, WaitableObjectType waitableObjectType, DWORD waitResult, DWORD errorCode)
{
    if (signalableObjectType == SignalableObjectType::Invalid || waitableObjectType == WaitableObjectType::Invalid)
    {
        TestAssert(waitResult == WAIT_FAILED);
        TestAssert(errorCode == ERROR_INVALID_HANDLE);
        return false;
    }

    switch (signalableObjectType)
    {
        case SignalableObjectType::FullSemaphore:
            TestAssert(waitResult == WAIT_FAILED);
            TestAssert(errorCode == ERROR_TOO_MANY_POSTS);
            return false;

        case SignalableObjectType::UnlockedMutex:
            TestAssert(waitResult == WAIT_FAILED);
            TestAssert(errorCode == ERROR_NOT_OWNER);
            return false;

        default:
            break;
    }

    switch (waitableObjectType)
    {
        case WaitableObjectType::UnsignaledManualResetEvent:
        case WaitableObjectType::UnsignaledAutoResetEvent:
        case WaitableObjectType::EmptySemaphore:
            TestAssert(waitResult == WAIT_TIMEOUT);
            break;

        default:
            TestAssert(waitResult == WAIT_OBJECT_0);
            break;
    }
    TestAssert(errorCode == ERROR_SUCCESS);
    return true;
}

void Run(SignalableObjectType signalableObjectType, WaitableObjectType waitableObjectType)
{
    HANDLE objectToSignal = CreateObjectToSignal(signalableObjectType);
    TestAssert(signalableObjectType == SignalableObjectType::Invalid || objectToSignal != nullptr);
    HANDLE objectToWaitOn = CreateObjectToWaitOn(waitableObjectType);
    TestAssert(waitableObjectType == WaitableObjectType::Invalid || objectToWaitOn != nullptr);
    DWORD waitResult = SignalObjectAndWait(objectToSignal, objectToWaitOn, 0, true);
    DWORD errorCode = waitResult == WAIT_FAILED ? GetLastError() : ERROR_SUCCESS;

    try
    {
        if (Verify(signalableObjectType, waitableObjectType, waitResult, errorCode))
        {
            VerifySignal(objectToSignal, signalableObjectType);
            VerifyWait(objectToWaitOn, waitableObjectType);
        }
    }
    catch (AssertionFailureException ex)
    {
        ex.signalableObjectType = signalableObjectType;
        ex.waitableObjectType = waitableObjectType;
        ex.waitResult = waitResult;
        ex.errorCode = errorCode;
        throw ex;
    }
}

static bool s_apcCalled = false;

void CALLBACK ApcCallback(ULONG_PTR dwParam)
{
    s_apcCalled = true;
    HANDLE *objects = (HANDLE *)dwParam;
    HANDLE objectToSignal = objects[0];
    HANDLE objectToWaitOn = objects[1];
    TestAssert(WaitForSingleObject(objectToSignal, 0) == WAIT_OBJECT_0); // signal has occurred
    TestAssert(WaitForSingleObject(objectToWaitOn, 0) == WAIT_OBJECT_0); // wait has not occurred yet
    SetEvent(objectToWaitOn);
}

void Run()
{
    for (SignalableObjectType signalableObjectType = SignalableObjectType::First;
        signalableObjectType <= SignalableObjectType::Last;
        ++signalableObjectType)
    {
        for (WaitableObjectType waitableObjectType = WaitableObjectType::First;
            waitableObjectType <= WaitableObjectType::Last;
            ++waitableObjectType)
        {
            Run(signalableObjectType, waitableObjectType);
        }
    }

    DWORD waitResult = WAIT_FAILED;
    try
    {
        HANDLE objectToSignal = CreateObjectToSignal(SignalableObjectType::ManualResetEvent);
        TestAssert(objectToSignal != nullptr);
        HANDLE objectToWaitOn = CreateObjectToWaitOn(WaitableObjectType::AutoResetEvent);
        TestAssert(objectToWaitOn != nullptr);
        HANDLE objects[] = {objectToSignal, objectToWaitOn};

        // Verify that a queued APC is not called if the wait is not alertable
        QueueUserAPC(&ApcCallback, GetCurrentThread(), (ULONG_PTR)&objects);
        waitResult = SignalObjectAndWait(objectToSignal, objectToWaitOn, 0, false);
        TestAssert(waitResult == WAIT_OBJECT_0);
        TestAssert(!s_apcCalled);
        TestAssert(WaitForSingleObject(objectToSignal, 0) == WAIT_OBJECT_0); // signal has occurred
        TestAssert(WaitForSingleObject(objectToWaitOn, 0) == WAIT_TIMEOUT); // wait has occurred

        // Verify that signal, call APC, wait, occur in that order
        ResetEvent(objectToSignal);
        SetEvent(objectToWaitOn);
        waitResult = SignalObjectAndWait(objectToSignal, objectToWaitOn, 0, true);
        TestAssert(waitResult == WAIT_IO_COMPLETION);
        TestAssert(s_apcCalled);
        TestAssert(WaitForSingleObject(objectToSignal, 0) == WAIT_OBJECT_0); // signal has occurred
        TestAssert(WaitForSingleObject(objectToWaitOn, 0) == WAIT_OBJECT_0); // wait has not occurred yet
        s_apcCalled = false;
        ResetEvent(objectToSignal);
        SetEvent(objectToWaitOn);
        waitResult = SignalObjectAndWait(objectToSignal, objectToWaitOn, 0, true);
        TestAssert(waitResult == WAIT_OBJECT_0);
        TestAssert(!s_apcCalled);
        TestAssert(WaitForSingleObject(objectToSignal, 0) == WAIT_OBJECT_0); // signal has occurred
        TestAssert(WaitForSingleObject(objectToWaitOn, 0) == WAIT_TIMEOUT); // wait has occurred

        CloseHandle(objectToSignal);
        CloseHandle(objectToWaitOn);
    }
    catch (AssertionFailureException ex)
    {
        ex.signalableObjectType = SignalableObjectType::ManualResetEvent;
        ex.waitableObjectType = WaitableObjectType::AutoResetEvent;
        ex.waitResult = waitResult;
        throw ex;
    }
}

PALTEST(threading_SignalObjectAndWait_paltest_signalobjectandwaittest, "threading/SignalObjectAndWait/paltest_signalobjectandwaittest")
{
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    int testReturnCode = PASS;
    try
    {
        Run();
    }
    catch (AssertionFailureException ex)
    {
        printf(
            "SignalObjectAndWaitTest - Assertion failure (line %d, signalable object type %d, waitable object type %d, wait result 0x%x, error code %u): '%s'\n",
            ex.lineNumber,
            (int)ex.signalableObjectType,
            (int)ex.waitableObjectType,
            ex.waitResult,
            ex.errorCode,
            ex.expression);
        fflush(stdout);
        testReturnCode = FAIL;
    }

    PAL_TerminateEx(testReturnCode);
    return testReturnCode;
}
