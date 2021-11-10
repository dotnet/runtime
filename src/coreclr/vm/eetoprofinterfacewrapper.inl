// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// EEToProfInterfaceWrapper.inl
//

//
// Inline implementation of wrappers around code that calls into some of the profiler's
// callback methods.
//

// ======================================================================================

#ifndef _EETOPROFEXCEPTIONINTERFACEWRAPPER_INL_
#define _EETOPROFEXCEPTIONINTERFACEWRAPPER_INL_

#include "common.h"


// A wrapper for the profiler.  Various events to signal different phases of exception
// handling.
class EEToProfilerExceptionInterfaceWrapper
{
  public:

#if defined(PROFILING_SUPPORTED)
    //
    // Exception creation
    //

    static inline void ExceptionThrown(Thread * pThread)
    {
        WRAPPER_NO_CONTRACT;
        {
            BEGIN_PROFILER_CALLBACK(CORProfilerTrackExceptions());
            _ASSERTE(pThread->PreemptiveGCDisabled());

            // Get a reference to the object that won't move
            OBJECTREF thrown = pThread->GetThrowable();

            (&g_profControlBlock)->ExceptionThrown(
                reinterpret_cast<ObjectID>((*(BYTE **)&thrown)));
            END_PROFILER_CALLBACK();
        }
    }

    //
    // Search phase
    //

    static inline void ExceptionSearchFunctionEnter(MethodDesc * pFunction)
    {
        WRAPPER_NO_CONTRACT;
        // Notify the profiler of the function being searched for a handler.
        {
            BEGIN_PROFILER_CALLBACK(CORProfilerTrackExceptions());
            if (!pFunction->IsNoMetadata())
            {
                GCX_PREEMP();
                (&g_profControlBlock)->ExceptionSearchFunctionEnter(
                    (FunctionID) pFunction);
            }
            END_PROFILER_CALLBACK();
        }
    }

    static inline void ExceptionSearchFunctionLeave(MethodDesc * pFunction)
    {
        WRAPPER_NO_CONTRACT;
        // Notify the profiler of the function being searched for a handler.
        {
            BEGIN_PROFILER_CALLBACK(CORProfilerTrackExceptions());
            if (!pFunction->IsNoMetadata())
            {
                GCX_PREEMP();
                (&g_profControlBlock)->ExceptionSearchFunctionLeave();
            }
            END_PROFILER_CALLBACK();
        }
    }

    static inline void ExceptionSearchFilterEnter(MethodDesc * pFunc)
    {
        WRAPPER_NO_CONTRACT;
        // Notify the profiler of the filter.
        {
            BEGIN_PROFILER_CALLBACK(CORProfilerTrackExceptions());
            if (!pFunc->IsNoMetadata())
            {
                GCX_PREEMP();
                (&g_profControlBlock)->ExceptionSearchFilterEnter(
                    (FunctionID) pFunc);
            }
            END_PROFILER_CALLBACK();
        }
    }

    static inline void ExceptionSearchFilterLeave()
    {
        WRAPPER_NO_CONTRACT;
        // Notify the profiler of the filter.
        {
            BEGIN_PROFILER_CALLBACK(CORProfilerTrackExceptions());
            GCX_PREEMP();
            (&g_profControlBlock)->ExceptionSearchFilterLeave();
            END_PROFILER_CALLBACK();
        }
    }

    static inline void ExceptionSearchCatcherFound(MethodDesc * pFunc)
    {
        WRAPPER_NO_CONTRACT;
        {
            BEGIN_PROFILER_CALLBACK(CORProfilerTrackExceptions());
            if (!pFunc->IsNoMetadata())
            {
                GCX_PREEMP();
                (&g_profControlBlock)->ExceptionSearchCatcherFound(
                    (FunctionID) pFunc);
            }
            END_PROFILER_CALLBACK();
        }
    }

    //
    // Unwind phase
    //
    static inline void ExceptionUnwindFunctionEnter(MethodDesc * pFunc)
    {
        WRAPPER_NO_CONTRACT;
        // Notify the profiler of the function being searched for a handler.
        {
            BEGIN_PROFILER_CALLBACK(CORProfilerTrackExceptions());
            if (!pFunc->IsNoMetadata())
            {
                (&g_profControlBlock)->ExceptionUnwindFunctionEnter(
                    (FunctionID) pFunc);
            }
            END_PROFILER_CALLBACK();
        }
    }

    static inline void ExceptionUnwindFunctionLeave(MethodDesc * pFunction)
    {
        WRAPPER_NO_CONTRACT;
        // Notify the profiler that searching this function is over.
        {
            BEGIN_PROFILER_CALLBACK(CORProfilerTrackExceptions());
            if (!pFunction->IsNoMetadata())
            {
                (&g_profControlBlock)->ExceptionUnwindFunctionLeave();
            }
            END_PROFILER_CALLBACK();
        }
    }

    static inline void ExceptionUnwindFinallyEnter(MethodDesc * pFunc)
    {
        WRAPPER_NO_CONTRACT;
        // Notify the profiler of the function being searched for a handler.
        {
            BEGIN_PROFILER_CALLBACK(CORProfilerTrackExceptions());
            if (!pFunc->IsNoMetadata())
            {
                (&g_profControlBlock)->ExceptionUnwindFinallyEnter(
                    (FunctionID) pFunc);
            }
            END_PROFILER_CALLBACK();
        }
    }

    static inline void ExceptionUnwindFinallyLeave()
    {
        WRAPPER_NO_CONTRACT;
        // Notify the profiler of the function being searched for a handler.
        {
            BEGIN_PROFILER_CALLBACK(CORProfilerTrackExceptions());
            (&g_profControlBlock)->ExceptionUnwindFinallyLeave();
            END_PROFILER_CALLBACK();
        }
    }

    static inline void ExceptionCatcherEnter(Thread * pThread, MethodDesc * pFunc)
    {
        WRAPPER_NO_CONTRACT;
        // Notify the profiler.
        {
            BEGIN_PROFILER_CALLBACK(CORProfilerTrackExceptions());
            if (!pFunc->IsNoMetadata())
            {
                // <TODO>Remove the thrown variable as well as the
                // gcprotect they are pointless.</TODO>

                // Note that the callee must be aware that the ObjectID
                // passed CAN change when gc happens.
                OBJECTREF thrown = NULL;
                GCPROTECT_BEGIN(thrown);
                thrown = pThread->GetThrowable();
                {
                    (&g_profControlBlock)->ExceptionCatcherEnter(
                        (FunctionID) pFunc,
                        reinterpret_cast<ObjectID>((*(BYTE **)&thrown)));
                }
                GCPROTECT_END();
            }
            END_PROFILER_CALLBACK();
        }
    }

    static inline void ExceptionCatcherLeave()
    {
        WRAPPER_NO_CONTRACT;
        // Notify the profiler of the function being searched for a handler.
        {
            BEGIN_PROFILER_CALLBACK(CORProfilerTrackExceptions());
            (&g_profControlBlock)->ExceptionCatcherLeave();
            END_PROFILER_CALLBACK();
        }
    }


#else // !PROFILING_SUPPORTED
    static inline void ExceptionThrown(Thread * pThread) { LIMITED_METHOD_CONTRACT;}
    static inline void ExceptionSearchFunctionEnter(MethodDesc * pFunction) { LIMITED_METHOD_CONTRACT;}
    static inline void ExceptionSearchFunctionLeave(MethodDesc * pFunction) { LIMITED_METHOD_CONTRACT;}
    static inline void ExceptionSearchFilterEnter(MethodDesc * pFunc) { LIMITED_METHOD_CONTRACT;}
    static inline void ExceptionSearchFilterLeave() { LIMITED_METHOD_CONTRACT;}
    static inline void ExceptionSearchCatcherFound(MethodDesc * pFunc) { LIMITED_METHOD_CONTRACT;}
    static inline void ExceptionOSHandlerEnter(MethodDesc ** ppNotify, MethodDesc * pFunc) { LIMITED_METHOD_CONTRACT;}
    static inline void ExceptionOSHandlerLeave(MethodDesc * pNotify) { LIMITED_METHOD_CONTRACT;}
    static inline void ExceptionUnwindFunctionEnter(MethodDesc * pFunc) { LIMITED_METHOD_CONTRACT;}
    static inline void ExceptionUnwindFunctionLeave(MethodDesc * pFunction) { LIMITED_METHOD_CONTRACT;}
    static inline void ExceptionUnwindFinallyEnter(MethodDesc * pFunc) { LIMITED_METHOD_CONTRACT;}
    static inline void ExceptionUnwindFinallyLeave() { LIMITED_METHOD_CONTRACT;}
    static inline void ExceptionCatcherEnter(Thread * pThread, MethodDesc * pFunc) { LIMITED_METHOD_CONTRACT;}
    static inline void ExceptionCatcherLeave() { LIMITED_METHOD_CONTRACT;}
#endif // !PROFILING_SUPPORTED
};


#endif // _EETOPROFEXCEPTIONINTERFACEWRAPPER_INL_
