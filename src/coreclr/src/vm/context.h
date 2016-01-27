// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#ifndef _H_CONTEXT_
#define _H_CONTEXT_

#include "specialstatics.h"
#include "fcall.h"

#ifdef FEATURE_COMINTEROP
class RCWCache;
#endif // FEATURE_COMINTEROP

typedef DPTR(class Context) PTR_Context;

#ifdef FEATURE_REMOTING

class Context
{
public:
    enum CallbackType
    {
        Wait_callback = 0,
        MonitorWait_callback = 1,
        ADTransition_callback = 2,
        SignalAndWait_callback = 3
    };

    typedef struct
    {
        int     numWaiters;
        HANDLE* waitHandles;
        BOOL    waitAll;
        DWORD   millis;
        BOOL    alertable;
        DWORD*  pResult;    
    } WaitArgs;

    typedef struct
    {
        HANDLE* waitHandles;
        DWORD   millis;
        BOOL    alertable;
        DWORD*  pResult;    
    } SignalAndWaitArgs;

    typedef struct
    {
        INT32           millis;          
        PendingSync*    syncState;     
        BOOL*           pResult;
    } MonitorWaitArgs;


    typedef struct
    {
        enum CallbackType   callbackId;
        void*               callbackData;
    } CallBackInfo;

    typedef void (*ADCallBackFcnType)(LPVOID);

    struct ADCallBackArgs
    {
        ADCallBackFcnType pTarget;
        LPVOID pArguments;
    };

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

friend class Thread;
friend class ThreadNative;
friend class ContextBaseObject;
friend class CRemotingServices;
friend struct PendingSync;

    Context(AppDomain *pDomain);
    ~Context();    
    static void Initialize();
    PTR_AppDomain GetDomain()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_pDomain;
    }

    // Get and Set the exposed System.Runtime.Remoting.Context
    // object which corresponds to this context.
    OBJECTREF   GetExposedObject();
    OBJECTREF   GetExposedObjectRaw();
    PTR_Object  GetExposedObjectRawUnchecked();
    PTR_PTR_Object  GetExposedObjectRawUncheckedPtr();
    void        SetExposedObject(OBJECTREF exposed);
    
    // Query whether the exposed object exists
    BOOL IsExposedObjectSet();

    static LPVOID GetStaticFieldAddress(FieldDesc *pFD);

    PTR_VOID GetStaticFieldAddrNoCreate(FieldDesc *pFD);

    static Context* CreateNewContext(AppDomain *pDomain);

    static void FreeContext(Context* victim)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(victim));
        }
        CONTRACTL_END;

        delete victim;
    }

    static Context* GetExecutionContext(OBJECTREF pObj);
    static void RequestCallBack(ADID appDomain, Context* targetCtxID, void* privateData);    

    // <TODO>Made public to get around the context GC issue </TODO>
    static BOOL ValidateContext(Context *pCtx);  

    inline STATIC_DATA *GetSharedStaticData()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pSharedStaticData;
    }
    
    inline void SetSharedStaticData(STATIC_DATA *pData)
    {
        LIMITED_METHOD_CONTRACT;
        m_pSharedStaticData = PTR_STATIC_DATA(pData);
    }

    inline STATIC_DATA *GetUnsharedStaticData()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pUnsharedStaticData;
    }
    
    inline void SetUnsharedStaticData(STATIC_DATA *pData)
    {
        LIMITED_METHOD_CONTRACT;
        m_pUnsharedStaticData = PTR_STATIC_DATA(pData);
    }

    // Functions called from BCL on a managed context object
    static FCDECL2(void, SetupInternalContext, ContextBaseObject* pThisUNSAFE, CLR_BOOL bDefault);
    static FCDECL1(void, CleanupInternalContext, ContextBaseObject* pThisUNSAFE);
    static FCDECL1(void, ExecuteCallBack, LPVOID privateData);

private:
    // Static helper functions:

    static void ExecuteWaitCallback(WaitArgs* waitArgs);
    static void ExecuteMonitorWaitCallback(MonitorWaitArgs* waitArgs);
    static void ExecuteSignalAndWaitCallback(SignalAndWaitArgs* signalAndWaitArgs);
    void GetStaticFieldAddressSpecial(FieldDesc *pFD, MethodTable *pMT, int *pSlot, LPVOID *ppvAddress);
    PTR_VOID CalculateAddressForManagedStatic(int slot);

    // Static Data Members:

    static CrstStatic s_ContextCrst;
    

    // Non-static Data Members:
    // Pointer to native context static data
    PTR_STATIC_DATA     m_pUnsharedStaticData;
    
    // Pointer to native context static data
    PTR_STATIC_DATA     m_pSharedStaticData;

    typedef SimpleList<OBJECTHANDLE> ObjectHandleList;

    ObjectHandleList    m_PinnedContextStatics;

    // <TODO> CTS. Domains should really be policies on a context and not
    // entry in the context object. When AppDomains become an attribute of
    // a context then add the policy.</TODO>
    PTR_AppDomain       m_pDomain;

    OBJECTHANDLE        m_ExposedObjectHandle;

    DWORD               m_Signature;
    // NOTE: please maintain the signature as the last member field!!!
};

FCDECL0(LPVOID, GetPrivateContextsPerfCountersEx);

#else // FEATURE_REMOTING

// if FEATURE_REMOTING is not defined there will be only the default context for each appdomain
// and contexts will not be exposed to users (so there will be no managed Context class)

class Context
{
    PTR_AppDomain m_pDomain;

public:
#ifndef DACCESS_COMPILE
    Context(AppDomain *pDomain)
    {
        m_pDomain = pDomain;
    }
#endif

    PTR_AppDomain GetDomain()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_pDomain;
    }

    static void Initialize()
    {
    }

    typedef void (*ADCallBackFcnType)(LPVOID);

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif
};

#endif // FEATURE_REMOTING

#endif
