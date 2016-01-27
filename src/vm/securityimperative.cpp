// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 


//

#include "common.h"
#include "security.h"

//-----------------------------------------------------------+
// P R I V A T E   H E L P E R S
//-----------------------------------------------------------+

LPVOID GetSecurityObjectForFrameInternal(StackCrawlMark *stackMark, INT32 create, OBJECTREF *pRefSecDesc)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    // This is a package protected method. Assumes correct usage.

    Thread *pThread = GetThread();
    AppDomain * pAppDomain = pThread->GetDomain();

    if (pRefSecDesc == NULL)
    {
        if (!Security::SkipAndFindFunctionInfo(stackMark, NULL, &pRefSecDesc, &pAppDomain))
            return NULL;
    }

    if (pRefSecDesc == NULL)
        return NULL;

    // Is security object frame in a different context?
    bool fSwitchContext;
    fSwitchContext = pAppDomain != pThread->GetDomain();

    if (create && *pRefSecDesc == NULL)
    {
        // If necessary, shift to correct context to allocate security object.
        _ASSERTE(pAppDomain == GetAppDomain());
        MethodTable* pMethFrameSecDesc = MscorlibBinder::GetClass(CLASS__FRAME_SECURITY_DESCRIPTOR);
        *pRefSecDesc = AllocateObject(pMethFrameSecDesc);
    }

    // If we found or created a security object in a different context, make a
    // copy in the current context.
    LPVOID rv;
    if (fSwitchContext && *pRefSecDesc != NULL)
        *((OBJECTREF*)&rv) = AppDomainHelper::CrossContextCopyFrom(pAppDomain, pRefSecDesc);
    else
        *((OBJECTREF*)&rv) = *pRefSecDesc;

    return rv;
}

FCIMPL2(Object*, SecurityRuntime::GetSecurityObjectForFrame, StackCrawlMark* stackMark, CLR_BOOL create)
{
    FCALL_CONTRACT;

    OBJECTREF refRetVal = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_0();

    refRetVal = ObjectToOBJECTREF((Object*)GetSecurityObjectForFrameInternal(stackMark, create, NULL));

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(refRetVal);
}
FCIMPLEND

void SecurityRuntime::CheckBeforeAllocConsole(AppDomain* pDomain, Assembly* pAssembly)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    // Tell the debugger not to start on any managed code that we call in this method    
    FrameWithCookie<DebuggerSecurityCodeMarkFrame> __dbgSecFrame;

    // Check that the assembly is granted unrestricted UIPermission
    IAssemblySecurityDescriptor* pSecDesc = pAssembly->GetSecurityDescriptor(pDomain);
    _ASSERTE(pSecDesc != NULL);
    if (!pSecDesc->HasUnrestrictedUIPermission())
    {
        struct _gc {
            OBJECTREF orDemand;
            OBJECTREF orRefused;
            OBJECTREF orGranted;
        } gc;
        ZeroMemory(&gc, sizeof(_gc));
        GCPROTECT_BEGIN(gc);
        {
            // Get the necessary managed objects
            gc.orGranted = pSecDesc->GetGrantedPermissionSet(&gc.orRefused);
            SecurityDeclarative::_GetSharedPermissionInstance(&gc.orDemand, UI_PERMISSION);

            // Check that the assembly is granted the necessary permission
            SecurityStackWalk sw(SSWT_DEMAND_FROM_NATIVE, NULL);
            sw.m_objects.SetObjects(gc.orDemand, NULL);
            sw.CheckPermissionAgainstGrants(NULL, gc.orGranted, gc.orRefused, pDomain, NULL, pAssembly);
        }
        GCPROTECT_END();
    }

    // Now do a demand against everything on the stack for unrestricted UIPermission
    Security::SpecialDemand(SSWT_DEMAND_FROM_NATIVE, UI_PERMISSION);

    // Pop the debugger frame
    __dbgSecFrame.Pop();
}


