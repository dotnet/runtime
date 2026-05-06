// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
#include "comcallablewrapper.h"

#if !defined(DACCESS_COMPILE)
inline BOOL ComInterfaceSlotIs(IUnknown* pUnk, int slot, LPVOID pvFunction)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        MODE_ANY;
        PRECONDITION(CheckPointer(pUnk));
    }
    CONTRACTL_END;

    LPVOID pvRetVal = (*((LPVOID**)pUnk))[slot];

    return (pvRetVal == (LPVOID)GetEEFuncEntryPoint(pvFunction));
}

//Helpers
// Is the tear-off a CLR created tear-off
inline BOOL IsInProcCCWTearOff(IUnknown* pUnk)
{
    WRAPPER_NO_CONTRACT;
    return ComInterfaceSlotIs(pUnk, 0, Unknown_QueryInterface) ||
           ComInterfaceSlotIs(pUnk, 0, Unknown_QueryInterface_IErrorInfo);
}

// is the tear-off represent one of the standard interfaces such as IProvideClassInfo, IErrorInfo etc.
inline BOOL IsSimpleTearOff(IUnknown* pUnk)
{
    WRAPPER_NO_CONTRACT;
    return ComInterfaceSlotIs(pUnk, TEAR_OFF_SLOT, TEAR_OFF_SIMPLE);
}

// Is the tear-off represent the inner unknown or the original unknown for the object
inline BOOL IsInnerUnknown(IUnknown* pUnk)
{
    WRAPPER_NO_CONTRACT;
    return ComInterfaceSlotIs(pUnk, TEAR_OFF_SLOT, TEAR_OFF_SIMPLE_INNER);
}

// Is this one of our "standard" ComCallWrappers
inline BOOL IsStandardTearOff(IUnknown* pUnk)
{
    WRAPPER_NO_CONTRACT;
    return ComInterfaceSlotIs(pUnk, TEAR_OFF_SLOT, TEAR_OFF_STANDARD);
}

// Convert an IUnknown to CCW, does not handle aggregation and ICustomQI.
FORCEINLINE ComCallWrapper* MapIUnknownToWrapper(IUnknown* pUnk)
{
    CONTRACT (ComCallWrapper*)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pUnk, NULL_OK));
    }
    CONTRACT_END;

    if (IsStandardTearOff(pUnk))
        RETURN ComCallWrapper::GetWrapperFromIP(pUnk);

    if (IsSimpleTearOff(pUnk) || IsInnerUnknown(pUnk))
        RETURN SimpleComCallWrapper::GetWrapperFromIP(pUnk)->GetMainWrapper();

    RETURN NULL;
}
#endif // !DACCESS_COMPILE
