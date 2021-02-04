// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include <cstdlib>
#include <cstdio>
#include <cassert>
#include <platformdefines.h>

using BeginEndCallback = void(STDMETHODCALLTYPE *)(int);
using IsReferencedCallback = int(STDMETHODCALLTYPE *)(void*);
using EnteredFinalizationCallback = void(STDMETHODCALLTYPE *)(void*);

// #define REF_TRACKER_DEBUG_CALLBACKS

namespace
{
    [[noreturn]] void fatal_state_error(int last, int state, const char* msg)
    {
        std::printf("Invalid state:%d -> %d\n%s\n", last, state, msg);
        std::abort();
    }

    const int UnknownBeginState = 0;
    int lastBeginState = UnknownBeginState;

    void STDMETHODCALLTYPE BeginEndCb(int state)
    {
#ifdef REF_TRACKER_DEBUG_CALLBACKS
        ::printf("BeginEndCb: %d\n", state);
#endif // REF_TRACKER_DEBUG_CALLBACKS

        if (state == 0)
        {
            fatal_state_error(lastBeginState, state, "Invalid begin/end state");
        }
        else if (state > 0)
        {
            // Begin
            if (lastBeginState != UnknownBeginState)
                fatal_state_error(lastBeginState, state, "Invalid begin state");

            lastBeginState = state;
        }
        else
        {
            // End
            if (std::abs(state) != lastBeginState)
                fatal_state_error(lastBeginState, state, "Invalid end state");

            lastBeginState = UnknownBeginState;
        }
    }

    // See contract in managed portion of test.
    struct ScratchContract
    {
        size_t RefCountDown;
        size_t RefCountUp;
    };

    int STDMETHODCALLTYPE IsRefCb(void* mem)
    {
#ifdef REF_TRACKER_DEBUG_CALLBACKS
        ::printf("IsRefCb: %p\n", mem);
#endif // REF_TRACKER_DEBUG_CALLBACKS

        assert(mem != nullptr);
        auto cxt = (ScratchContract*)mem;

        cxt->RefCountDown--;
        cxt->RefCountUp++;

        if (cxt->RefCountDown == 0)
        {
            // No more references
            return 0;
        }
        else
        {
            return 1;
        }
    }

    void STDMETHODCALLTYPE EnteredFinalizerCb(void* mem)
    {
#ifdef REF_TRACKER_DEBUG_CALLBACKS
        ::printf("EnteredFinalizerCb: %p\n", mem);
#endif // REF_TRACKER_DEBUG_CALLBACKS

        assert(mem != nullptr);
        auto cxt = (ScratchContract*)mem;

        cxt->RefCountDown = (size_t)-1;
    }
}

extern "C" DLL_EXPORT void GetBridgeExports(
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