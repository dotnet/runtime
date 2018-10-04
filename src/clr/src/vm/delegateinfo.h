// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Header: DelegateInfo.h
**
**
** Purpose: Native methods on System.ThreadPool
**          and its inner classes
**

** 
===========================================================*/
#ifndef DELEGATE_INFO
#define DELEGATE_INFO

struct DelegateInfo;
typedef DelegateInfo* DelegateInfoPtr;

struct DelegateInfo
{
    ADID            m_appDomainId;
    OBJECTHANDLE    m_stateHandle;
    OBJECTHANDLE    m_eventHandle;
    OBJECTHANDLE    m_registeredWaitHandle;

#ifndef DACCESS_COMPILE
    void Release()
    {
        CONTRACTL {
            // m_compressedStack->Release() can actually throw today because it has got a call
            // to new down the stack. However that is recent and the semantic of that api is such
            // it should not throw. I am expecting clenup of that function to take care of that
            // so I am adding this comment to make sure the issue is document.
            // Remove this comment once that work is done
            NOTHROW;
            GC_TRIGGERS;
            MODE_COOPERATIVE; 
            FORBID_FAULT;
        }
        CONTRACTL_END;


        if (m_stateHandle)
            DestroyHandle(m_stateHandle);
        if (m_eventHandle)
                DestroyHandle(m_eventHandle);
        if (m_registeredWaitHandle)
            DestroyHandle(m_registeredWaitHandle);
    }
#endif

    static DelegateInfo  *MakeDelegateInfo(AppDomain *pAppDomain,
                                           OBJECTREF *state,
                                           OBJECTREF *waitEvent,
                                           OBJECTREF *registeredWaitObject);
};





#endif // DELEGATE_INFO
