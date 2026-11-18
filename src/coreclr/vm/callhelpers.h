// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
** File:    callhelpers.h
** Purpose: Provides helpers for making managed calls
===========================================================*/

#ifndef __CALLHELPERS_H__
#define __CALLHELPERS_H__

#ifdef TARGET_WASM
#include "wasm/callhelpers.hpp"
#endif

#define NUMBER_RETURNVALUE_SLOTS (ENREGISTERED_RETURNTYPE_MAXSIZE / sizeof(ARG_SLOT))

#if !defined(DACCESS_COMPILE)

void CallDefaultConstructor(OBJECTREF ref);

//
// Helper types for calling managed methods marked with [UnmanagedCallersOnly]
// from native code.
//

// Use CLR_BOOL_ARG to convert a BOOL value to a CLR_BOOL for passing to
// UnmanagedCallersOnlyCaller::InvokeThrowing when the managed parameter is bool.
#define CLR_BOOL_ARG(x) ((CLR_BOOL)(!!(x)))

// Helper class for calling managed methods marked with [UnmanagedCallersOnly]
// using the reverse P/Invoke infrastructure.
// This class assumes the target method signature has a trailing argument for
// returning the exception (that is, Exception* in C#).
//
// Example usage:
//   UnmanagedCallersOnlyCaller caller(BinderMethodID::MyMethod);
//   ...
//   caller.InvokeThrowing(arg1, arg2);
//
// The corresponding C# method would be declared as:
//   [UnmanagedCallersOnly]
//   public static void MyMethod(int arg1, object* arg2, Exception* pException);
//
class UnmanagedCallersOnlyCaller final
{
public:
    // Controls whether the reverse P/Invoke into the target is reported to the
    // debugger as a possible step-in destination (see DebuggerTraceCall).
    //
    // HiddenFromDebugger is for runtime-internal callbacks. Nested reverse
    // P/Invokes, including class constructors, are also hidden unless they
    // explicitly restore visibility. Callbacks that may run user code may need
    // VisibleEvenIfNested, but callers should account for internal-only paths.
    // VisibleEvenIfNested restores visibility for the duration of the call; nested
    // callbacks inherit it, including runtime-internal ones, unless they use
    // HiddenFromDebugger. Only InvokeThrowing and InvokeThrowing_Ret apply non-default
    // visibility; InvokeDirect and InvokeDirect_Ret ignore it (checked builds assert).
    enum DebuggerVisibility
    {
        VisibleToDebugger,
        HiddenFromDebugger,
        VisibleEvenIfNested,
    };

private:
    MethodDesc* _pMD;
    DebuggerVisibility _debuggerVisibility;

#ifdef DEBUGGING_SUPPORTED
    // True while a HiddenFromDebugger invocation is in progress on this thread.
    static thread_local bool t_fDebuggerTraceCallsHidden;

    class DebuggerHiddenHolder final
    {
        bool _fPrevious;
    public:
        explicit DebuggerHiddenHolder(DebuggerVisibility visibility)
            : _fPrevious{ t_fDebuggerTraceCallsHidden }
        {
            LIMITED_METHOD_CONTRACT;
            t_fDebuggerTraceCallsHidden = visibility == HiddenFromDebugger ||
                (visibility == VisibleToDebugger && _fPrevious);
        }

        ~DebuggerHiddenHolder()
        {
            LIMITED_METHOD_CONTRACT;
            t_fDebuggerTraceCallsHidden = _fPrevious;
        }
    };
#endif // DEBUGGING_SUPPORTED

public:
    explicit UnmanagedCallersOnlyCaller(BinderMethodID id, DebuggerVisibility debuggerVisibility = VisibleToDebugger)
        : _pMD{}
        , _debuggerVisibility{ debuggerVisibility }
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;

        _pMD = CoreLibBinder::GetMethod(id);
        _ASSERTE(_pMD != NULL);
        _ASSERTE(_pMD->HasUnmanagedCallersOnlyAttribute());
    }

#ifdef DEBUGGING_SUPPORTED
    // Returns true while a HiddenFromDebugger invocation is in progress on the current thread.
    // This covers the callback itself and any managed code it reaches through further
    // native-to-managed transitions (for example, a class constructor run by the runtime),
    // except callbacks that explicitly restore visibility.
    static bool AreDebuggerTraceCallsHidden()
    {
        LIMITED_METHOD_CONTRACT;
        return t_fDebuggerTraceCallsHidden;
    }
#endif // DEBUGGING_SUPPORTED

    template<typename... Args>
    void InvokeThrowing(Args... args)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;

        // Sanity check - UnmanagedCallersOnly methods must be in CoreLib.
        // See below load level override.
        _ASSERTE(_pMD->GetModule()->IsSystem());

        // We're invoking an CoreLib method, so lift the restriction on type load limits. These calls are
        // limited to CoreLib and only into UnmanagedCallersOnly methods.
        OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);

        struct
        {
            OBJECTREF Exception;
        } gc;
        gc.Exception = NULL;
        GCPROTECT_BEGIN(gc);

        {
            GCX_PREEMP();

            PCODE methodEntry = _pMD->GetSingleCallableAddrOfCodeForUnmanagedCallersOnly();
            _ASSERTE(methodEntry != (PCODE)NULL);

            // Cast the function pointer to the appropriate type.
            // Note that we append the exception handle argument.
            auto fptr = reinterpret_cast<void(*)(Args..., OBJECTREF*)>(methodEntry);

#ifdef DEBUGGING_SUPPORTED
            DebuggerHiddenHolder debuggerHidden(_debuggerVisibility);
#endif // DEBUGGING_SUPPORTED

            // The last argument is the implied exception handle for any exceptions.
            fptr(args..., &gc.Exception);
        }

        // If an exception was thrown, propagate it
        if (gc.Exception != NULL)
            COMPlusThrow(gc.Exception);

        GCPROTECT_END();
    }

    template<typename Ret, typename... Args>
    Ret InvokeThrowing_Ret(Args... args)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;

        // Sanity check - UnmanagedCallersOnly methods must be in CoreLib.
        // See below load level override.
        _ASSERTE(_pMD->GetModule()->IsSystem());

        // We're invoking an CoreLib method, so lift the restriction on type load limits. These calls are
        // limited to CoreLib and only into UnmanagedCallersOnly methods.
        OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);

        Ret ret;

        struct
        {
            OBJECTREF Exception;
        } gc;
        gc.Exception = NULL;
        GCPROTECT_BEGIN(gc);

        {
            GCX_PREEMP();

            PCODE methodEntry = _pMD->GetSingleCallableAddrOfCodeForUnmanagedCallersOnly();
            _ASSERTE(methodEntry != (PCODE)NULL);

            // Cast the function pointer to the appropriate type.
            // Note that we append the exception handle argument.
            auto fptr = reinterpret_cast<Ret(*)(Args..., OBJECTREF*)>(methodEntry);

#ifdef DEBUGGING_SUPPORTED
            DebuggerHiddenHolder debuggerHidden(_debuggerVisibility);
#endif // DEBUGGING_SUPPORTED

            // The last argument is the implied exception handle for any exceptions.
            ret = fptr(args..., &gc.Exception);
        }

        // If an exception was thrown, propagate it
        if (gc.Exception != NULL)
            COMPlusThrow(gc.Exception);

        GCPROTECT_END();

        return ret;
    }

    template<typename... Args>
    void InvokeDirect(Args... args)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;

        _ASSERTE(_pMD->GetModule()->IsSystem());
        // The callee may unwind through this frame, so non-default debugger visibility is not supported here.
        _ASSERTE(_debuggerVisibility == VisibleToDebugger);

        OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);

        // The callee may be Ex::RhThrowEx/RhThrowHwEx/RhRethrow, which raise the managed exception
        // that resumes execution by throwing a native WASM exception tag through this frame. The
        // region form is required so a plain holder destructor does not flip the GC mode as that
        // tag unwinds past this point.
        GCX_PREEMP_REGION_BEGIN();

        PCODE methodEntry = _pMD->GetSingleCallableAddrOfCodeForUnmanagedCallersOnly();
        _ASSERTE(methodEntry != (PCODE)NULL);

        auto fptr = reinterpret_cast<void(*)(Args...)>(methodEntry);
        fptr(args...);

        GCX_PREEMP_REGION_END();
    }

    template<typename Ret, typename... Args>
    Ret InvokeDirect_Ret(Args... args)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;

        _ASSERTE(_pMD->GetModule()->IsSystem());
        _ASSERTE(_debuggerVisibility == VisibleToDebugger);

        OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);

        Ret ret;

        // See the comment in InvokeDirect above for why the region form is required here.
        GCX_PREEMP_REGION_BEGIN();

        PCODE methodEntry = _pMD->GetSingleCallableAddrOfCodeForUnmanagedCallersOnly();
        _ASSERTE(methodEntry != (PCODE)NULL);

        auto fptr = reinterpret_cast<Ret(*)(Args...)>(methodEntry);
        ret = fptr(args...);

        GCX_PREEMP_REGION_END();

        return ret;
    }
};

#endif //!DACCESS_COMPILE

#endif // __CALLHELPERS_H__
