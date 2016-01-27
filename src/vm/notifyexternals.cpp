// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: EXTERNALS.CPP
// 

// ===========================================================================


#include "common.h"

#include "excep.h"
#include "interoputil.h"
#include "comcache.h"

#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
#include "olecontexthelpers.h"
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

#define INITGUID
#include <guiddef.h>
#include "ctxtcall.h"
#include "notifyexternals.h"
#include "mdaassistants.h"

DEFINE_GUID(CLSID_ComApartmentState, 0x00000349, 0, 0, 0xC0,0,0,0,0,0,0,0x46);
static const GUID IID_ITeardownNotification = { 0xa85e0fb6, 0x8bf4, 0x4614, { 0xb1, 0x64, 0x7b, 0x43, 0xef, 0x43, 0xf5, 0xbe } };
static const GUID IID_IComApartmentState = { 0x7e220139, 0x8dde, 0x47ef, { 0xb1, 0x81, 0x08, 0xbe, 0x60, 0x3e, 0xfd, 0x75 } };

static IComApartmentState* g_pApartmentState = NULL;
static ULONG_PTR      g_TDCookie = 0;
    

// ---------------------------------------------------------------------------
// %%Class EEClassFactory
// IClassFactory implementation for COM+ objects
// ---------------------------------------------------------------------------
class ApartmentTearDownHandler : public ITeardownNotification
{ 
public:
    ApartmentTearDownHandler(HRESULT& hr)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

        GCX_PREEMP();
        
        m_pMarshalerObj = NULL;
        m_cbRefCount = 1;     
        hr = CoCreateFreeThreadedMarshaler(this, &m_pMarshalerObj);
        if (hr == S_OK)
            m_cbRefCount = 0;
        else
            Release();        
    }

    virtual ~ApartmentTearDownHandler()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;
        
        if (m_pMarshalerObj != NULL)
        {
            DWORD cbRef = SafeRelease(m_pMarshalerObj);
            LogInteropRelease(m_pMarshalerObj, cbRef, "pMarshaler object");
        }
    }

    STDMETHODIMP QueryInterface( REFIID iid, void **ppv)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;
        
        if (ppv == NULL)
            return E_POINTER;

        *ppv = NULL;

        if (iid == IID_ITeardownNotification || iid == IID_IUnknown)
        {
            *ppv = (IClassFactory2 *)this;
            AddRef();
        }
        else if (iid == IID_IMarshal || iid == IID_IAgileObject)
        {
            // delegate the IMarshal and IAgileObject Queries
            return SafeQueryInterface(m_pMarshalerObj, iid, (IUnknown**)ppv);
        }

        return (*ppv != NULL) ? S_OK : E_NOINTERFACE;
    }
        
    
    STDMETHODIMP_(ULONG) AddRef()
    {
        LIMITED_METHOD_CONTRACT;
        
        LONG l = FastInterlockIncrement(&m_cbRefCount);
        return l;
    }
    STDMETHODIMP_(ULONG)    Release()
    {
        LIMITED_METHOD_CONTRACT;
        
        LONG l = FastInterlockDecrement(&m_cbRefCount);
        
        if (l == 0)
            delete this;
        
        return l;
    }

    STDMETHODIMP TeardownHint(void)
    {
        WRAPPER_NO_CONTRACT;
        return HandleApartmentShutDown();
    }

    HRESULT HandleApartmentShutDown()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

        Thread* pThread = GetThread();
        if (pThread != NULL)
        {
            _ASSERTE(!"NYI");      
            // reset the apartment state
            pThread->ResetApartment();
        }
        return S_OK;
    }

private:
    LONG                    m_cbRefCount;
    IUnknown*               m_pMarshalerObj;
};

HRESULT SetupTearDownNotifications()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    
    HRESULT hr =  S_OK;
    static BOOL fTearDownCalled = FALSE;
    
    // check if we already have setup a notification
    if (fTearDownCalled == TRUE)
        return S_OK;
        
    fTearDownCalled = TRUE;        

    GCX_PREEMP();
            
    //  instantiate the notifier
    SafeComHolderPreemp<IComApartmentState> pAptState = NULL;
    hr = CoCreateInstance(CLSID_ComApartmentState, NULL, CLSCTX_ALL, IID_IComApartmentState, (VOID **)&pAptState);

    if (hr == S_OK)
    {
        IComApartmentState* pPrevAptState = FastInterlockCompareExchangePointer(&g_pApartmentState, pAptState.GetValue(), NULL);
        
        if (pPrevAptState == NULL)
        {
            _ASSERTE(g_pApartmentState);
            ApartmentTearDownHandler* pTDHandler = new (nothrow) ApartmentTearDownHandler(hr);
            if (hr == S_OK)
            {
                SafeComHolderPreemp<ITeardownNotification> pITD = NULL;
                hr = SafeQueryInterface(pTDHandler, IID_ITeardownNotification, (IUnknown **)&pITD);
                _ASSERTE(hr == S_OK && pITD != NULL);
                g_pApartmentState->RegisterForTeardownHint(pITD, 0, &g_TDCookie);                    
            }         
            else
            {
                // oops we couldn't create our handler
                // release the global apstate pointer
                if (g_pApartmentState != NULL)
                {
                    g_pApartmentState->Release();
                    g_pApartmentState = NULL;
                }
            }

            // We're either keeping the object alive, or we've already freed it.
            pAptState.SuppressRelease();
        }
    }

    return S_OK;
}

VOID RemoveTearDownNotifications()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    
    if (g_pApartmentState != NULL)
    {
        _ASSERTE(g_TDCookie != 0);
        g_pApartmentState->UnregisterForTeardownHint(g_TDCookie);
        g_pApartmentState->Release();
        g_pApartmentState = NULL;
        g_TDCookie = 0;
    }    
}


// On some platforms, we can detect whether the current thread holds the loader
// lock.  It is unsafe to execute managed code when this is the case
BOOL ShouldCheckLoaderLock(BOOL fForMDA /*= TRUE*/)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT;
    }
    CONTRACTL_END;
    
#ifdef FEATURE_CORESYSTEM
    // CoreSystem does not support this.
    return FALSE;
#else
    // Because of how C++ generates code, we must use default initialization to
    // 0 here.  Any explicit initialization will result in thread-safety problems.
    static BOOL fInited;
    static BOOL fShouldCheck;
    static BOOL fShouldCheck_ForMDA;

    if (VolatileLoad(&fInited) == FALSE)
    {
        fShouldCheck_ForMDA = FALSE;

        fShouldCheck = AuxUlibInitialize();      // may fail

#ifdef MDA_SUPPORTED
        if (fShouldCheck)
        {
            MdaLoaderLock* pProbe = MDA_GET_ASSISTANT(LoaderLock);
            if (pProbe)
                fShouldCheck_ForMDA = TRUE;
        }
#endif // MDA_SUPPORTED
        VolatileStore(&fInited, TRUE);
    }
    return (fForMDA ? fShouldCheck_ForMDA : fShouldCheck);
#endif // FEATURE_CORESYSTEM
}
