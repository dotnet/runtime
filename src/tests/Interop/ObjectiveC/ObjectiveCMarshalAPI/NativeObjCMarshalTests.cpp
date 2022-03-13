// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include <cstdlib>
#include <cstdio>
#include <cassert>
#include <atomic>
#include <exception>
#include <platformdefines.h>

using BeginEndCallback = void(STDMETHODCALLTYPE *)(void);
using IsReferencedCallback = int(STDMETHODCALLTYPE *)(void*);
using EnteredFinalizationCallback = void(STDMETHODCALLTYPE *)(void*);

//#define REF_TRACKER_DEBUG_CALLBACKS

namespace
{
    std::atomic_bool state = { false };

    void STDMETHODCALLTYPE BeginEndCb()
    {
#ifdef REF_TRACKER_DEBUG_CALLBACKS
        ::printf("BeginEndCb: %s -> %s\n",
            state ? "true" : "false",
            !state ? "true" : "false");
#endif // REF_TRACKER_DEBUG_CALLBACKS

        state = !state;
    }

    // See contract in managed portion of test.
    struct Contract
    {
        size_t RefCountDown;
        size_t RefCountUp;
    };

    int STDMETHODCALLTYPE IsRefCb(void* mem)
    {
#ifdef REF_TRACKER_DEBUG_CALLBACKS
        ::printf("IsRefCb: %p\n", mem);
#endif // REF_TRACKER_DEBUG_CALLBACKS

        if (!state)
        {
            ::printf("Invalid callback state!\n");
            ::abort();
        }

        assert(mem != nullptr);
        auto cxt = (Contract*)mem;

        if (cxt->RefCountDown == 0)
        {
            // No more references
            return 0;
        }
        else
        {
            cxt->RefCountDown--;
            cxt->RefCountUp++;
            return 1;
        }
    }

    void STDMETHODCALLTYPE EnteredFinalizerCb(void* mem)
    {
#ifdef REF_TRACKER_DEBUG_CALLBACKS
        ::printf("EnteredFinalizerCb: %p\n", mem);
#endif // REF_TRACKER_DEBUG_CALLBACKS

        assert(mem != nullptr);
        auto cxt = (Contract*)mem;

        cxt->RefCountDown = (size_t)-1;
    }
}

extern "C" DLL_EXPORT void GetExports(
    BeginEndCallback* beginEnd,
    IsReferencedCallback* isRef,
    EnteredFinalizationCallback* enteredFinalizer)
{
    assert(beginEnd != nullptr);
    assert(isRef != nullptr);
    assert(enteredFinalizer != nullptr);

    *beginEnd = BeginEndCb;
    *isRef = IsRefCb;
    *enteredFinalizer = EnteredFinalizerCb;
}

using propagation_func_t = void(*)(void*);

namespace
{
    [[noreturn]]
    void ThrowInt(void* cxt)
    {
        int val = (int)(size_t)cxt;
        throw val;
    }

    [[noreturn]]
    void ThrowException(void*)
    {
        throw std::exception{};
    }
}

extern "C" DLL_EXPORT propagation_func_t GetThrowInt()
{
    return ThrowInt;
}

extern "C" DLL_EXPORT propagation_func_t GetThrowException()
{
    return ThrowException;
}

using fptr_t = void(*)(int);

extern "C" DLL_EXPORT int CallAndCatch(fptr_t fptr, int a)
{
    try
    {
        fptr(a);

        std::printf("Function should not have returned.");
        std::abort();
    }
    catch (int e)
    {
        return e;
    }
    catch (const std::exception &e)
    {
        return a;
    }
}
