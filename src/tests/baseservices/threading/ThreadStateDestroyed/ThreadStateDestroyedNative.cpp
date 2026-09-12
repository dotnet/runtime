// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <platformdefines.h>

typedef void (*CallbackFn)();

static CallbackFn s_callback;

static void RunCallbackDuringThreadDestruction()
{
    printf("[native] invoking managed callback from the thread destruction callback\n");
    fflush(stdout);

    s_callback();

    printf("[native] managed callback returned; the runtime did not detect the re-initialization\n");
    fflush(stdout);
}

#ifdef _WIN32

// FLS rather than TLS: FlsAlloc takes a destruction callback, which is also the mechanism the
// runtime itself uses to learn that a thread is terminating.

static DWORD s_flsIndex = FLS_OUT_OF_INDEXES;

static VOID WINAPI FlsCallback(PVOID)
{
    RunCallbackDuringThreadDestruction();
}

static DWORD WINAPI ThreadProc(LPVOID)
{
    s_callback();

    // Arms FlsCallback, which the OS invokes while this thread is being destroyed.
    if (!FlsSetValue(s_flsIndex, reinterpret_cast<PVOID>(1)))
    {
        abort();
    }

    return 0;
}

extern "C" DLL_EXPORT void RunCallbackOnThreadAndDuringItsDestruction(CallbackFn callback)
{
    s_callback = callback;

    s_flsIndex = FlsAlloc(FlsCallback);
    if (s_flsIndex == FLS_OUT_OF_INDEXES)
    {
        abort();
    }

    HANDLE thread = CreateThread(nullptr, 0, ThreadProc, nullptr, 0, nullptr);
    if (thread == nullptr)
    {
        abort();
    }

    if (WaitForSingleObject(thread, INFINITE) != WAIT_OBJECT_0)
    {
        abort();
    }

    CloseHandle(thread);
    FlsFree(s_flsIndex);
}

#else // _WIN32

#define AbortIfFail(st) if (st != 0) abort()

static pthread_key_t s_key;

static void KeyDestructor(void*)
{
    RunCallbackDuringThreadDestruction();
}

static void* ThreadProc(void*)
{
    s_callback();

    // Arms KeyDestructor. pthread key destructors run after the C++ thread_local destructors
    // that the runtime uses to tear down its per-thread state.
    // The callback below observes a thread whose runtime thread state is already gone.
    int st = pthread_setspecific(s_key, reinterpret_cast<void*>(1));
    AbortIfFail(st);

    return nullptr;
}

extern "C" DLL_EXPORT void RunCallbackOnThreadAndDuringItsDestruction(CallbackFn callback)
{
    s_callback = callback;

    int st = pthread_key_create(&s_key, KeyDestructor);
    AbortIfFail(st);

    pthread_attr_t attr;
    st = pthread_attr_init(&attr);
    AbortIfFail(st);

    // We need to set the stack size due to the very small (80kB) default stack size on MUSL
    // based Linux distros.
    st = pthread_attr_setstacksize(&attr, 0x180000); // 1.5MB
    AbortIfFail(st);

    pthread_t thread;
    st = pthread_create(&thread, &attr, ThreadProc, nullptr);
    AbortIfFail(st);

    pthread_attr_destroy(&attr);

    st = pthread_join(thread, nullptr);
    AbortIfFail(st);
}

#endif // _WIN32
