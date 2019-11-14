// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Header: ExtensibleClassFactory.cpp
**
**
** Purpose: Native methods on System.Runtime.InteropServices.ExtensibleClassFactory
**

**
===========================================================*/

#include "common.h"

#include "excep.h"
#include "stackwalk.h"
#include "extensibleclassfactory.h"


// Helper function used to walk stack frames looking for a class initializer.
static StackWalkAction FrameCallback(CrawlFrame *pCF, void *pData)
{
    _ASSERTE(NULL != pCF);
    MethodDesc *pMD = pCF->GetFunction();

    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMD));
        PRECONDITION(CheckPointer(pData, NULL_OK));
        PRECONDITION(pMD->GetMethodTable() != NULL);
    }
    CONTRACTL_END;


    // We use the pData context argument to track the class as we move down the
    // stack and to return the class whose initializer is being called. If
    // *ppMT is NULL we are looking at the caller's initial frame and just
    // record the class that the method belongs to. From that point on the class
    // must remain the same until we hit a class initializer or else we must
    // fail (to prevent other classes called from a class initializer from
    // setting the current classes callback). The very first class we will see
    // belongs to RegisterObjectCreationCallback itself, so skip it (we set
    // *ppMT to an initial value of -1 to detect this).
    MethodTable **ppMT = (MethodTable **)pData;

    if (*ppMT == (MethodTable *)-1)
        *ppMT = NULL;

    else if (*ppMT == NULL)
        *ppMT = pMD->GetMethodTable();

    else if (pMD->GetMethodTable() != *ppMT)
    {
        *ppMT = NULL;
        return SWA_ABORT;
    }

    if (pMD->IsClassConstructor())
        return SWA_ABORT;

    return SWA_CONTINUE;
}


// Register a delegate that will be called whenever an instance of a
// managed type that extends from an unmanaged type needs to allocate
// the aggregated unmanaged object. This delegate is expected to
// allocate and aggregate the unmanaged object and is called in place
// of a CoCreateInstance. This routine must be called in the context
// of the static initializer for the class for which the callbacks
// will be made.
// It is not legal to register this callback from a class that has any
// parents that have already registered a callback.
FCIMPL1(void, RegisterObjectCreationCallback, Object* pDelegateUNSAFE)
{
    FCALL_CONTRACT;

    OBJECTREF orDelegate = (OBJECTREF) pDelegateUNSAFE;
    HELPER_METHOD_FRAME_BEGIN_1(orDelegate);

    // Validate the delegate argument.
    if (orDelegate == 0)
        COMPlusThrowArgumentNull(W("callback"));

    // We should have been called in the context of a class static initializer.
    // Walk back up the stack to verify this and to determine just what class
    // we're registering a callback for.
    MethodTable *pMT = (MethodTable *)-1;
    if (GetThread()->StackWalkFrames(FrameCallback, &pMT, FUNCTIONSONLY, NULL) == SWA_FAILED)
        COMPlusThrow(kInvalidOperationException, IDS_EE_CALLBACK_NOT_CALLED_FROM_CCTOR);

    // If we didn't find a class initializer, we can't continue.
    if (pMT == NULL)
    {
        COMPlusThrow(kInvalidOperationException, IDS_EE_CALLBACK_NOT_CALLED_FROM_CCTOR);
    }

    // The object type must derive at some stage from a COM imported object.
    // Also we must fail the call if some parent class has already registered a
    // callback.
    MethodTable *pParent = pMT;
    do
    {
        pParent = pParent->GetParentMethodTable();
        if (pParent && !pParent->IsComImport() && (pParent->GetObjCreateDelegate() != NULL))
        {
            COMPlusThrow(kInvalidOperationException, IDS_EE_CALLBACK_ALREADY_REGISTERED);
        }
    }
    while (pParent && !pParent->IsComImport());

    // If the class does not have a COM imported base class then fail the call.
    if (pParent == NULL || pParent->IsProjectedFromWinRT())
    {
        COMPlusThrow(kInvalidOperationException, IDS_EE_CALLBACK_NOT_CALLED_FROM_CCTOR);
    }

    // Save the delegate in the MethodTable for the class.
    pMT->SetObjCreateDelegate(orDelegate);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND
