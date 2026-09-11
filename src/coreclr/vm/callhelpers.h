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
    MethodDesc* _pMD;
public:
    explicit UnmanagedCallersOnlyCaller(BinderMethodID id)
        : _pMD{}
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

        OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);

        GCX_PREEMP();

        PCODE methodEntry = _pMD->GetSingleCallableAddrOfCodeForUnmanagedCallersOnly();
        _ASSERTE(methodEntry != (PCODE)NULL);

        auto fptr = reinterpret_cast<void(*)(Args...)>(methodEntry);
        fptr(args...);
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

        OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);

        GCX_PREEMP();

        PCODE methodEntry = _pMD->GetSingleCallableAddrOfCodeForUnmanagedCallersOnly();
        _ASSERTE(methodEntry != (PCODE)NULL);

        auto fptr = reinterpret_cast<Ret(*)(Args...)>(methodEntry);
        return fptr(args...);
    }
};

#endif //!DACCESS_COMPILE

#endif // __CALLHELPERS_H__
