// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "common.h"

//#include "ClassFactory3.h"
#include "winwrap.h"
#include "comcallablewrapper.h"
#include "frames.h"
#include "excep.h"
#include "registration.h"
#include "typeparse.h"
#include "mdaassistants.h"


#ifdef FEATURE_COMINTEROP_MANAGED_ACTIVATION

// Allocate a managed object given the method table pointer.
HRESULT STDMETHODCALLTYPE EEAllocateInstance(LPUNKNOWN pOuter, MethodTable* pMT, BOOL fHasLicensing, REFIID riid, BOOL fDesignTime, BSTR bstrKey, void** ppv);
extern BOOL g_fEEComActivatedStartup;
extern BOOL g_fEEHostedStartup; 
extern GUID g_EEComObjectGuid;

// ---------------------------------------------------------------------------
// %%Class EEClassFactory
// IClassFactory implementation for COM+ objects
// ---------------------------------------------------------------------------
class EEClassFactory : public IClassFactory2
{
public:
    EEClassFactory(CLSID* pClsId, MethodTable* pTable)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
            PRECONDITION(CheckPointer(pTable));
            PRECONDITION(CheckPointer(pClsId));
        }
        CONTRACTL_END;
        
        LOG((LF_INTEROP, LL_INFO100, "EEClassFactory::EEClassFactory for class %s\n", pTable->GetDebugClassName()));
        m_pMethodTable = pTable;
        m_cbRefCount = 0;
        memcpy(&m_ClsId, pClsId, sizeof(GUID));
        m_hasLicensing = FALSE;

        while (pTable != NULL && pTable != g_pObjectClass)
        {
            if (pTable->GetMDImport()->GetCustomAttributeByName(pTable->GetCl(), "System.ComponentModel.LicenseProviderAttribute", 0,0) == S_OK)
            {
                m_hasLicensing = TRUE;
                break;
            }
            pTable = pTable->GetParentMethodTable();
        }
    }

    ~EEClassFactory()
    {
        WRAPPER_NO_CONTRACT;
        
        LOG((LF_INTEROP, LL_INFO100, "EEClassFactory::~ for class %s\n", m_pMethodTable->GetDebugClassName()));
    }

    STDMETHODIMP QueryInterface( REFIID iid, void **ppv)
    {
        SetupForComCallDWORDNoHostNotif();

        CONTRACTL
        {
            NOTHROW;
            GC_TRIGGERS;
            MODE_PREEMPTIVE;
            SO_TOLERANT;
            PRECONDITION(CheckPointer(ppv, NULL_OK));
        }
        CONTRACTL_END;

        if (ppv == NULL)
            return E_POINTER;

        *ppv = NULL;

        if (iid == IID_IClassFactory || ((iid == IID_IClassFactory2) && m_hasLicensing ) || iid == IID_IUnknown)
        {
            *ppv = (IClassFactory2 *)this;
            AddRef();
        }

        return (*ppv != NULL) ? S_OK : E_NOINTERFACE;
    }

    STDMETHODIMP_(ULONG) AddRef()
{       
        SetupForComCallDWORDNoHostNotif();

        CONTRACTL
        {
            NOTHROW;
            GC_TRIGGERS;
            MODE_PREEMPTIVE;
            SO_TOLERANT;
        }
        CONTRACTL_END;
        
        ULONG l = FastInterlockIncrement(&m_cbRefCount);
        return l;
    }
    
    STDMETHODIMP_(ULONG) Release()
    {
        SetupForComCallDWORD();

        CONTRACTL
        {
            NOTHROW;
            GC_TRIGGERS;
            MODE_PREEMPTIVE;
            SO_TOLERANT;
            PRECONDITION(m_cbRefCount > 0);
        }
        CONTRACTL_END;

        HRESULT hr = S_OK;
        ULONG l = -1;
        
        BEGIN_EXTERNAL_ENTRYPOINT(&hr)
        {
            l = FastInterlockDecrement(&m_cbRefCount);
            if (l == 0)
                delete this;
        }
        END_EXTERNAL_ENTRYPOINT;
        
        return l;
    }

    STDMETHODIMP CreateInstance(LPUNKNOWN punkOuter, REFIID riid, void** ppv)
    {
        HRESULT hr = S_OK;
#ifdef FEATURE_CORRUPTING_EXCEPTIONS
        // SetupForComCallHR uses "SO_INTOLERANT_CODE_NOTHROW" to setup the SO-Intolerant transition
        // for COM Interop. However, "SO_INTOLERANT_CODE_NOTHROW" expects that no exception can escape
        // through this boundary but all it does is (in addition to checking that no exception has escaped it)
        // do stack probing.
        //
        // However, Corrupting Exceptions [CE] can escape the COM Interop boundary. Thus, to address that scenario,
        // we use the macro below that uses BEGIN_SO_INTOLERANT_CODE_NOTHROW to do the equivalent of 
        // SO_INTOLERANT_CODE_NOTHROW and yet allow for CEs to escape through. Since there will be a corresponding
        // END_SO_INTOLERANT_CODE, the call is splitted into two parts: the Begin and End (see below).
        BeginSetupForComCallHRWithEscapingCorruptingExceptions();
#else // !FEATURE_CORRUPTING_EXCEPTIONS
        SetupForComCallHR();
#endif // FEATURE_CORRUPTING_EXCEPTIONS

        CONTRACTL
        {
#ifdef FEATURE_CORRUPTING_EXCEPTIONS
            THROWS; // CSE can escape out of this function
#else // !FEATURE_CORRUPTING_EXCEPTIONS
            NOTHROW;
#endif // FEATURE_CORRUPTING_EXCEPTIONS
            GC_TRIGGERS;
            MODE_PREEMPTIVE;
            SO_TOLERANT;
        }
        CONTRACTL_END;

        hr = UpdateMethodTable();

        // allocate a com+ object
        // this will allocate the object in the correct context
        // we might end up with a tear-off on our COM+ context proxy
        if (SUCCEEDED(hr))
        {
            hr = EEAllocateInstance(punkOuter, m_pMethodTable, m_hasLicensing, riid, TRUE, NULL, ppv);
        }

#ifdef FEATURE_CORRUPTING_EXCEPTIONS
        EndSetupForComCallHRWithEscapingCorruptingExceptions();
#endif // FEATURE_CORRUPTING_EXCEPTIONS

        return hr;
    }

    STDMETHODIMP LockServer(BOOL fLock)
    {
        SetupForComCallHR();

        CONTRACTL
        {
            NOTHROW;
            GC_TRIGGERS;
            MODE_PREEMPTIVE;
            SO_TOLERANT;
        }
        CONTRACTL_END;

        return S_OK;
    }

    // The implementation of these two functions is provided below. Prefast chocks if their implementation is here.
    STDMETHODIMP GetLicInfo(LPLICINFO pLicInfo);
    STDMETHODIMP RequestLicKey(DWORD dwReserved, BSTR * pbstrKey);
    
    STDMETHODIMP CreateInstanceLic(IUnknown *punkOuter, IUnknown* pUnkReserved, REFIID riid, BSTR bstrKey, void **ppUnk)
    {
        HRESULT hr = S_OK;

#ifdef FEATURE_CORRUPTING_EXCEPTIONS
        // SetupForComCallHR uses "SO_INTOLERANT_CODE_NOTHROW" to setup the SO-Intolerant transition
        // for COM Interop. However, "SO_INTOLERANT_CODE_NOTHROW" expects that no exception can escape
        // through this boundary but all it does is (in addition to checking that no exception has escaped it)
        // do stack probing.
        //
        // However, Corrupting Exceptions [CE] can escape the COM Interop boundary. Thus, to address that scenario,
        // we use the macro below that uses BEGIN_SO_INTOLERANT_CODE_NOTHROW to do the equivalent of 
        // SO_INTOLERANT_CODE_NOTHROW and yet allow for CEs to escape through. Since there will be a corresponding
        // END_SO_INTOLERANT_CODE, the call is splitted into two parts: the Begin and End (see below).
        BeginSetupForComCallHRWithEscapingCorruptingExceptions();
#else // !FEATURE_CORRUPTING_EXCEPTIONS
        SetupForComCallHR();
#endif // FEATURE_CORRUPTING_EXCEPTIONS

        CONTRACTL
        {
#ifdef FEATURE_CORRUPTING_EXCEPTIONS
            THROWS; // CSE can escape out of this function
#else // !FEATURE_CORRUPTING_EXCEPTIONS
            NOTHROW;
#endif // FEATURE_CORRUPTING_EXCEPTIONS

            GC_TRIGGERS;
            MODE_PREEMPTIVE;
            SO_TOLERANT;
        }
        CONTRACTL_END;
        
        if (!ppUnk)
        {
            hr = E_POINTER;
            goto done;
        }

        *ppUnk = NULL;

        if (pUnkReserved != NULL)
        {
            hr = E_INVALIDARG;
            goto done;
        }

        if (bstrKey == NULL)
        {
            hr = E_POINTER;
            goto done;
        }

        hr = UpdateMethodTable();

        // allocate a com+ object
        // this will allocate the object in the correct context
        // we might end up with a tear-off on our COM+ context proxy
        if (SUCCEEDED(hr))
        {
            hr = EEAllocateInstance(punkOuter, m_pMethodTable, m_hasLicensing, riid, /*fDesignTime=*/FALSE, bstrKey, ppUnk);
        }

done: ;
#ifdef FEATURE_CORRUPTING_EXCEPTIONS
        EndSetupForComCallHRWithEscapingCorruptingExceptions();
#endif // FEATURE_CORRUPTING_EXCEPTIONS

        return hr;
    }
    
    STDMETHODIMP CreateInstanceWithContext(LPUNKNOWN punkContext, LPUNKNOWN punkOuter, REFIID riid, void** ppv)
    {
        HRESULT hr = S_OK;

#ifdef FEATURE_CORRUPTING_EXCEPTIONS
        // SetupForComCallHR uses "SO_INTOLERANT_CODE_NOTHROW" to setup the SO-Intolerant transition
        // for COM Interop. However, "SO_INTOLERANT_CODE_NOTHROW" expects that no exception can escape
        // through this boundary but all it does is (in addition to checking that no exception has escaped it)
        // do stack probing.
        //
        // However, Corrupting Exceptions [CE] can escape the COM Interop boundary. Thus, to address that scenario,
        // we use the macro below that uses BEGIN_SO_INTOLERANT_CODE_NOTHROW to do the equivalent of 
        // SO_INTOLERANT_CODE_NOTHROW and yet allow for CEs to escape through. Since there will be a corresponding
        // END_SO_INTOLERANT_CODE, the call is splitted into two parts: the Begin and End (see below).
        BeginSetupForComCallHRWithEscapingCorruptingExceptions();
#else // !FEATURE_CORRUPTING_EXCEPTIONS
        SetupForComCallHR();
#endif // FEATURE_CORRUPTING_EXCEPTIONS

        CONTRACTL
        {
#ifdef FEATURE_CORRUPTING_EXCEPTIONS
            THROWS; // CSE can escape out of this function
#else // !FEATURE_CORRUPTING_EXCEPTIONS
            NOTHROW;
#endif // FEATURE_CORRUPTING_EXCEPTIONS
            GC_TRIGGERS;
            MODE_PREEMPTIVE;
            SO_TOLERANT;
        }
        CONTRACTL_END;

        hr = UpdateMethodTable();
        
        if (SUCCEEDED(hr))
        {
            hr = EEAllocateInstance(punkOuter, m_pMethodTable, m_hasLicensing, riid, TRUE, NULL, ppv);
        }

#ifdef FEATURE_CORRUPTING_EXCEPTIONS
        EndSetupForComCallHRWithEscapingCorruptingExceptions();
#endif // FEATURE_CORRUPTING_EXCEPTIONS

        return hr;
    }

private:
    // If we happen to be called from the same AD last time, we can use a cached MT
    // If not, we need to use the CLSID to find the correct MT for the current domain.
    CLSID                   m_ClsId;
    MethodTable*            m_pMethodTable;  // most recently used MT
    ADID                    m_dwDomainId;    // the AD we were in when this ClassFact was last used
    LONG                    m_cbRefCount;
    BOOL                    m_hasLicensing;

    // Ensure that m_pMethodTable and m_dwDomainId are valid for the current AppDomain.  If we're
    // not in the AD m_dwDomainId use the CLSID to get the MT.
    STDMETHODIMP UpdateMethodTable()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_TRIGGERS;
            MODE_ANY;
            INJECT_FAULT(COMPlusThrowOM(););
            SO_TOLERANT;        
        }
        CONTRACTL_END;

        HRESULT hr = S_OK;
        Thread *pThread = GetThread();
        ADID adid = pThread->GetDomain()->GetId();
        if (adid != m_dwDomainId) 
        {
            MethodTable* tempMT = NULL;
            EX_TRY
            { 
                BEGIN_SO_INTOLERANT_CODE(pThread);
                GCX_COOP();
                tempMT = GetTypeForCLSID(m_ClsId);
                END_SO_INTOLERANT_CODE;
            }
            EX_CATCH 
            {
                hr = E_FAIL;
            } 
            EX_END_CATCH(SwallowAllExceptions);

            if (tempMT != NULL && SUCCEEDED(hr))
            {
                m_pMethodTable = tempMT;
                m_dwDomainId = adid;
            }
        }
        return hr;
    }
    
};


STDMETHODIMP EEClassFactory::GetLicInfo(LPLICINFO pLicInfo)
{
    SetupForComCallHR();
    
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        SO_TOLERANT;        
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    if (!pLicInfo)
        return E_POINTER;

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        GCX_COOP_THREAD_EXISTS(GET_THREAD());

        Thread *pThread = GET_THREAD();
        hr = UpdateMethodTable();
        if (SUCCEEDED(hr))
        {
            MethodTable *pHelperMT = pThread->GetDomain()->GetLicenseInteropHelperMethodTable();
            MethodDesc *pMD = MemberLoader::FindMethod(pHelperMT, "GetLicInfo", &gsig_IM_LicenseInteropHelper_GetLicInfo);
            MethodDescCallSite getLicInfo(pMD);

            struct _gc {
                OBJECTREF pHelper;
                OBJECTREF pType;
            } gc;
            gc.pHelper = NULL; // LicenseInteropHelper
            gc.pType   = NULL;

            GCPROTECT_BEGIN(gc);

            gc.pHelper = pHelperMT->Allocate();
            gc.pType = m_pMethodTable->GetManagedClassObject();

            {
                INT32 fRuntimeKeyAvail = 0;
                INT32 fLicVerified     = 0;
    
                ARG_SLOT args[4];
                args[0] = ObjToArgSlot(gc.pHelper);
                args[1] = ObjToArgSlot(gc.pType);
                args[2] = (ARG_SLOT)&fRuntimeKeyAvail;
                args[3] = (ARG_SLOT)&fLicVerified;
                getLicInfo.Call(args);
        
                pLicInfo->cbLicInfo = sizeof(LICINFO);
                pLicInfo->fRuntimeKeyAvail = fRuntimeKeyAvail;
                pLicInfo->fLicVerified     = fLicVerified;
            }
            GCPROTECT_END();
        }
    } 
    END_EXTERNAL_ENTRYPOINT;

    return hr;
}

STDMETHODIMP EEClassFactory::RequestLicKey(DWORD dwReserved, BSTR * pbstrKey)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        SO_TOLERANT;        
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    if (dwReserved != 0)
        return E_INVALIDARG;

    if (!pbstrKey)
        return E_POINTER;

    *pbstrKey = NULL;

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        GCX_COOP_THREAD_EXISTS(GET_THREAD());

        Thread *pThread = GET_THREAD();
        hr = UpdateMethodTable();
        if (SUCCEEDED(hr))
        {
            MethodTable *pHelperMT = pThread->GetDomain()->GetLicenseInteropHelperMethodTable();
            MethodDesc *pMD = MemberLoader::FindMethod(pHelperMT, "RequestLicKey", &gsig_SM_LicenseInteropHelper_RequestLicKey);
            MethodDescCallSite requestLicKey(pMD);

            OBJECTREF pType = NULL;

            GCPROTECT_BEGIN(pType);

            pType = m_pMethodTable->GetManagedClassObject();
            ARG_SLOT args[2];
            args[0] = ObjToArgSlot(pType);
            args[1] = (ARG_SLOT)pbstrKey;
            hr = requestLicKey.Call_RetHR(args);
            GCPROTECT_END();
        }
    } 
    END_EXTERNAL_ENTRYPOINT;

    return hr;
}

void EEAllocateInstanceWorker(LPUNKNOWN pOuter, MethodTable* pMT, BOOL fHasLicensing, REFIID riid, BOOL fDesignTime, BSTR bstrKey, void** ppv)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(!pMT->IsComImport());
    }
    CONTRACTL_END;
    
    *ppv                            = NULL;
    OBJECTREF   newobj;    
    CCWHolder   pWrap               = NULL;
    BOOL        fCtorAlreadyCalled  = FALSE;
    Thread*     pThread             = GetThread();

    // classes that extend COM Imported class are special
    if (ExtendsComImport(pMT))
    {
        pMT->EnsureInstanceActive();
        newobj = AllocateObject(pMT);
    }
    else if (CRemotingServices::RequiresManagedActivation(pMT) != NoManagedActivation)
    {
        fCtorAlreadyCalled = TRUE;
        newobj = CRemotingServices::CreateProxyOrObject(pMT, TRUE);
    }
    else
    {
        // If the class doesn't have a LicenseProviderAttribute, let's not
        // pull in the LicenseManager class and friends.
        if (!fHasLicensing)
        {
            pMT->EnsureInstanceActive();
            newobj = AllocateObject( pMT, false );
        }
        else
        {
            MethodTable *pHelperMT = pThread->GetDomain()->GetLicenseInteropHelperMethodTable();
            MethodDesc *pMD = MemberLoader::FindMethod(pHelperMT, "AllocateAndValidateLicense", &gsig_SM_LicenseInteropHelper_AllocateAndValidateLicense);
            MethodDescCallSite allocateAndValidateLicense(pMD);

            pHelperMT->EnsureInstanceActive();

            OBJECTREF pType = NULL;

            GCPROTECT_BEGIN(pType);

            pType = pMT->GetManagedClassObject();

            ARG_SLOT args[3];
            args[0] = ObjToArgSlot(pType);
            args[1] = (ARG_SLOT)bstrKey;
            args[2] = fDesignTime ? 1 : 0;
            newobj = allocateAndValidateLicense.Call_RetOBJECTREF(args);
            fCtorAlreadyCalled = TRUE;

            GCPROTECT_END();
        }
    }
    
    GCPROTECT_BEGIN(newobj);
    {
        //get wrapper for the object, this could enable GC
        pWrap =  ComCallWrapper::InlineGetWrapper(&newobj); 

        // don't call any constructors if we already have called them
        if (!fCtorAlreadyCalled && !pMT->IsValueType())
            CallDefaultConstructor(newobj);
    }        
    GCPROTECT_END();            

    if (pOuter == NULL)
    {
        // Return the tear-off
        *ppv = ComCallWrapper::GetComIPFromCCW(pWrap, riid, NULL, GetComIPFromCCW::CheckVisibility);
        if (!*ppv)
            COMPlusThrowHR(E_NOINTERFACE);
    }
    else
    {
        // Aggregation support, 
        pWrap->InitializeOuter(pOuter);                                             
        IfFailThrow(pWrap->GetInnerUnknown(ppv));           
    }
}

// Allocate a managed object given the method table pointer
HRESULT STDMETHODCALLTYPE EEAllocateInstance(LPUNKNOWN pOuter, MethodTable* pMT, BOOL fHasLicensing, REFIID riid, BOOL fDesignTime, BSTR bstrKey, void** ppv)
{
    CONTRACTL
    {
#ifdef FEATURE_CORRUPTING_EXCEPTIONS
        THROWS; // CSE can escape out of this function
#else // !FEATURE_CORRUPTING_EXCEPTIONS
        NOTHROW;
#endif // FEATURE_CORRUPTING_EXCEPTIONS
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        SO_TOLERANT;        
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(CheckPointer(ppv, NULL_OK));
    }
    CONTRACTL_END;

    if (ppv == NULL)
        return E_POINTER;
    *ppv = NULL;

    if ((!fDesignTime) && bstrKey == NULL)
        return E_POINTER;

    // aggregating objects should QI for IUnknown
    if (pOuter != NULL && !IsEqualIID(riid, IID_IUnknown))
        return E_INVALIDARG;

    HRESULT hr = S_OK;

#ifdef FEATURE_CORRUPTING_EXCEPTIONS
    // Get the MethodDesc of the type being instantiated. Based upon it,
    // we will decide whether to rethrow a CSE or not in 
    // END_EXTERNAL_ENTRYPOINT_RETHROW_CORRUPTING_EXCEPTIONS_EX below.
    PTR_MethodDesc pMDDefConst = NULL;
    BOOL fHasConstructor = FALSE;
#endif // FEATURE_CORRUPTING_EXCEPTIONS

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        GCX_COOP_THREAD_EXISTS(GET_THREAD());

#ifdef FEATURE_CORRUPTING_EXCEPTIONS
        // Get the MethodDesc of the type being instantiated. Based upon it,
        // we will decide whether to rethrow a CSE or not in 
        // END_EXTERNAL_ENTRYPOINT_RETHROW_CORRUPTING_EXCEPTIONS_EX below.
        if (pMT->HasDefaultConstructor())
        {
            pMDDefConst = pMT->GetDefaultConstructor();
            fHasConstructor = (pMDDefConst != NULL);
        }
#endif // FEATURE_CORRUPTING_EXCEPTIONS

        EEAllocateInstanceWorker(pOuter, pMT, fHasLicensing, riid, fDesignTime, bstrKey, ppv);
    } 
    END_EXTERNAL_ENTRYPOINT_RETHROW_CORRUPTING_EXCEPTIONS_EX((fHasConstructor && (!CEHelper::CanMethodHandleException(UseLast,pMDDefConst))) || 
                                                             (!fHasConstructor));

    LOG((LF_INTEROP, LL_INFO100, "EEAllocateInstance for class %s object %8.8x\n", pMT->GetDebugClassName(), *ppv));

    return hr;
}

IUnknown *AllocateEEClassFactoryHelper(CLSID *pClsId, MethodTable *pMT)
{
    CONTRACT (IUnknown*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(CheckPointer(pClsId));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;
    
    RETURN ((IUnknown*)new EEClassFactory(pClsId, pMT));
}

void InitializeClass(TypeHandle th)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(!th.IsNull());
    }
    CONTRACTL_END;

    // Make sure the type isn't an interface or an abstract class.
    if (th.IsAbstract() || th.IsInterface())
        COMPlusThrowHR(COR_E_MEMBERACCESS);

    // Unless we are dealing with a value class, the type must have a public
    // default constructor.
    if (!th.GetMethodTable()->HasExplicitOrImplicitPublicDefaultConstructor())
    {
        COMPlusThrowHR(COR_E_MEMBERACCESS);
    }

    // Call class init if necessary
    th.GetMethodTable()->EnsureInstanceActive();
    th.GetMethodTable()->CheckRunClassInitThrowing(); 
}

// Try to load a managed class and give out an IClassFactory
void EEDllGetClassObjectHelper(REFCLSID rclsid, MethodTable* pMT, REFIID riid, LPVOID FAR *ppv)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(CheckPointer(ppv));
    }
    CONTRACTL_END;
    
    HRESULT hr = S_OK;
    CLSID clsId;
    SafeComHolder<IUnknown> pUnk = NULL;

    memcpy(&clsId, &rclsid, sizeof(GUID));
    pUnk = AllocateEEClassFactoryHelper(&clsId, pMT);

    // Bump up the count to protect the object
    ULONG cbRef = SafeAddRef(pUnk);
    LogInteropAddRef(pUnk, cbRef, "EEDllGetClassObjectHelper: Bump up refcount to protect object during call");

    // Query for the requested interface.
    hr = SafeQueryInterface(pUnk, riid, (IUnknown**)ppv);
    LogInteropQI(pUnk, riid, hr, "EDllGetClassObjectHelper: QI for requested interface");
    IfFailThrow(hr);
}

HRESULT STDMETHODCALLTYPE EEDllGetClassObject(REFCLSID rclsid, REFIID riid, LPVOID FAR *ppv)
{
    HRESULT hr = S_OK;
    g_fEEComActivatedStartup = TRUE;
    g_EEComObjectGuid = rclsid;

    // The EE must be started before SetupForComCallHR is called and the contract is set up.
    if (FAILED(hr = EnsureEEStarted(COINITEE_DEFAULT)))
        return hr;

    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        SO_TOLERANT;        
        PRECONDITION(CheckPointer(ppv, NULL_OK));
    }
    CONTRACTL_END;

    if (ppv == NULL)
        return  E_POINTER;

    BEGIN_EXTERNAL_ENTRYPOINT(&hr);
    {
        GCX_COOP_THREAD_EXISTS(GET_THREAD());

        // We are about to use COM IPs so make sure COM is started up.
        EnsureComStarted();

        MethodTable *pMT;

        {
            Thread *pThread = GetThread();
            BEGIN_SO_INTOLERANT_CODE(pThread);
            GCX_COOP();
            pMT = GetTypeForCLSID(rclsid);
            END_SO_INTOLERANT_CODE;
        }

        // If we can't find the class based on the CLSID or if the registered managed
        // class is ComImport class then fail the call. Also, if the type is a generic
        // type (either opened or closed) then fail the call.
        if (!pMT || pMT->IsComImport() || (pMT->GetNumGenericArgs() != 0))
        {
            COMPlusThrowHR(REGDB_E_CLASSNOTREG);
        }

        // Verify that the class is indeed creatable and run it's .cctor if it
        // hasn't been run yet.
        InitializeClass(TypeHandle(pMT));

        // Allocate the IClassFactory for the type.
        EEDllGetClassObjectHelper(rclsid, pMT, riid, ppv);
    }
    END_EXTERNAL_ENTRYPOINT;

    return hr;
} //EEDllGetClassObject

// Helper Functions to get a object based on a name
void ClrCreateManagedInstanceHelper(MethodTable* pMT, REFIID riid, LPVOID FAR *ppv)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(CheckPointer(ppv));
    }
    CONTRACTL_END;

    GCX_PREEMP();

    HRESULT hr = S_OK;
    SafeComHolderPreemp<IUnknown> pUnk = NULL;
    SafeComHolderPreemp<IClassFactory> pFactory = NULL;

    GUID guid;
    pMT->GetGuid(&guid, TRUE);
    pUnk = AllocateEEClassFactoryHelper(&guid, pMT);

    // Bump up the count to protect the object for the duration of this function
    ULONG cbRef = SafeAddRef(pUnk);
    LogInteropAddRef(pUnk, cbRef, "ClrCreateManagedInstanceHelper: Bumping refcount to protect object during call");

    // Query the factory for the IClassFactory interface.
    hr = SafeQueryInterface(pUnk, IID_IClassFactory, (IUnknown**) &pFactory);
    LogInteropQI(pUnk, IID_IClassFactory, hr, "ClrCreateManagedInstanceHelper: QI for IID_IClassFactory");
    IfFailThrow(hr);

    // Create an instance of the type.
    IfFailThrow(pFactory->CreateInstance(NULL, riid, ppv));
}

STDAPI ClrCreateManagedInstance(LPCWSTR typeName, REFIID riid, LPVOID FAR *ppv)
{
    HRESULT hr = S_OK;

    // The EE must be started before SetupForComCallHR is called and the contract is set up.
    g_fEEHostedStartup = TRUE;
    if (FAILED(hr = EnsureEEStarted(COINITEE_DEFAULT)))
        return hr;

    SetupForComCallHR();

    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        SO_TOLERANT;
        PRECONDITION(CheckPointer(typeName, NULL_OK));
        PRECONDITION(CheckPointer(ppv, NULL_OK));
    }
    CONTRACTL_END;

    if (ppv == NULL)
        return  E_POINTER;

    if (typeName == NULL)
        return E_INVALIDARG;

    BEGIN_EXTERNAL_ENTRYPOINT(&hr);
    {
        GCX_COOP_THREAD_EXISTS(GET_THREAD());

        // We are about to use COM IPs so make sure COM is started up.
        EnsureComStarted();

        MAKE_UTF8PTR_FROMWIDE(pName, typeName);

        AppDomain* pDomain = SystemDomain::GetCurrentDomain();       
        MethodTable *pMT = TypeName::GetTypeUsingCASearchRules(pName, NULL).GetMethodTable();
        if (!pMT || pMT->IsComImport())
            COMPlusThrowHR(REGDB_E_CLASSNOTREG);

        // Verify that the class is indeed creatable and run it's .cctor if it
        // hasn't been run yet.
        InitializeClass(TypeHandle(pMT));

        // Allocate the instance of the type.
        ClrCreateManagedInstanceHelper(pMT, riid, ppv);
    }
    END_EXTERNAL_ENTRYPOINT;

    return hr;
}

DWORD RegisterTypeForComClientsHelper(MethodTable *pMT, GUID *pGuid, CLSCTX clsContext, REGCLS flags)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(pMT != NULL);
        PRECONDITION(pGuid != NULL);
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    DWORD dwCookie = 0;
    SafeComHolder<IUnknown> pUnk = NULL;

    // We are about to perform COM operations so ensure COM is started up.
    EnsureComStarted();

    // Allocate an EE class factory for the type.
    pUnk = AllocateEEClassFactoryHelper(pGuid, pMT);

    // bump up the count to protect the object
    ULONG cbRef = SafeAddRef(pUnk);
    LogInteropAddRef(pUnk, cbRef, "RegisterTypeForComClientsNative: Bumping refcount to protect class factory");

    {
        // Enable GC
        GCX_PREEMP();

        // Call CoRegisterClassObject.
        IfFailThrow(CoRegisterClassObject(*(pGuid), pUnk, clsContext, flags, &dwCookie));
    }

    // CoRegisterClassObject will bump up the ref count so we must release
    // the extra ref count we added above.
    return dwCookie;
}

//+----------------------------------------------------------------------------
//
//  Method:     RegisterTypeForComClientsNative    
//
//  Synopsis:   Registers a class factory with COM classic for a given type 
//              and CLSID. Later we can receive activations on this factory
//              and we return a CCW.
//

//
//+----------------------------------------------------------------------------
FCIMPL2(VOID, RegisterTypeForComClientsNative, ReflectClassBaseObject* pTypeUNSAFE, GUID* pGuid)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(pTypeUNSAFE != NULL);
        PRECONDITION(pGuid != NULL);
    }
    CONTRACTL_END;

    REFLECTCLASSBASEREF pType = (REFLECTCLASSBASEREF) pTypeUNSAFE;
    HELPER_METHOD_FRAME_BEGIN_1(pType);

    // Retrieve the method table from the type.
    MethodTable *pMT = pType->GetType().GetMethodTable();

    // Call the helper to perform the registration.
    RegisterTypeForComClientsHelper(pMT, pGuid, (CLSCTX)(CLSCTX_INPROC_SERVER | CLSCTX_LOCAL_SERVER), REGCLS_MULTIPLEUSE);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

//+----------------------------------------------------------------------------
//
//  Method:     RegisterTypeForComClientsExNative    
//
//  Synopsis:   Registers a class factory with COM classic for a given type. 
//
//+----------------------------------------------------------------------------
FCIMPL3(DWORD, RegisterTypeForComClientsExNative, ReflectClassBaseObject* pTypeUNSAFE, CLSCTX clsContext, REGCLS flags)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(pTypeUNSAFE != NULL);
    }
    CONTRACTL_END;

    DWORD dwCookie = 0;
    GUID clsid;

    REFLECTCLASSBASEREF pType = (REFLECTCLASSBASEREF) pTypeUNSAFE;
    HELPER_METHOD_FRAME_BEGIN_RET_1(pType);

    // Retrieve the method table from the type.
    MethodTable *pMT = pType->GetType().GetMethodTable();

    // Retrieve the CLSID from the type.
    pMT->GetGuid(&clsid, TRUE);

    // Call the helper to perform the registration.
    dwCookie = RegisterTypeForComClientsHelper(pMT, &clsid, clsContext, flags);

    HELPER_METHOD_FRAME_END();

    return dwCookie;
}
FCIMPLEND

#else // FEATURE_COMINTEROP_MANAGED_ACTIVATION

STDAPI ClrCreateManagedInstance(LPCWSTR typeName, REFIID riid, LPVOID FAR *ppv)
{

    return E_NOTIMPL; // @TODO: CoreCLR_REMOVED: completely remove this function
}

#endif // FEATURE_COMINTEROP_MANAGED_ACTIVATION

