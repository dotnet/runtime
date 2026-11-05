// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
 *    CallHelpers.CPP: helpers to call managed code
 */

#include "common.h"

void CallDefaultConstructor(OBJECTREF ref)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    MethodTable *pMT = ref->GetMethodTable();

    _ASSERTE(pMT != NULL);

    if (!pMT->HasDefaultConstructor())
    {
        SString ctorMethodName(SString::Utf8, COR_CTOR_METHOD_NAME);
        COMPlusThrowNonLocalized(kMissingMethodException, ctorMethodName.GetUnicode());
    }

    GCPROTECT_BEGIN (ref);

    PCODE ctorCode;
    {
        GCX_PREEMP();
        MethodDesc *pMD = pMT->GetDefaultConstructor();
        ctorCode = pMD->GetSingleCallableAddrOfCode();
    }

    UnmanagedCallersOnlyCaller defaultCtorInvoker{METHOD__RUNTIME_HELPERS__CALL_DEFAULT_CONSTRUCTOR};

#ifdef FEATURE_PORTABLE_ENTRYPOINTS
    // CallDefaultConstructor invokes the ctor via the function pointer, so its portable entrypoint
    // must resolve to real code if possible.
    MethodDesc::EnsurePortableEntryPointIsCallableFromR2R(ctorCode);
#endif // FEATURE_PORTABLE_ENTRYPOINTS

    defaultCtorInvoker.InvokeThrowing(&ref, ctorCode);

    GCPROTECT_END ();
}
