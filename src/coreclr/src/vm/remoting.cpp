// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// File: remoting.cpp
// 

//
// Purpose: Defines various remoting related objects such as
//          proxies
//

// 


#include "common.h"

#ifdef FEATURE_REMOTING
#include "virtualcallstub.h"
#include "excep.h"
#include "comdelegate.h"
#include "remoting.h"
#include "field.h"
#include "siginfo.hpp"
#include "stackbuildersink.h"
#include "eehash.h"
#include "profilepriv.h"
#include "message.h"
#include "eeconfig.h"
#include "comcallablewrapper.h"
#include "interopconverter.h"
#include "asmconstants.h"
#include "crossdomaincalls.h"
#include "contractimpl.h"
#include "typestring.h"
#include "generics.h"
#include "appdomain.inl"
#include "dbginterface.h"

#ifndef DACCESS_COMPILE

// These hold label offsets into non-virtual thunks. They are used by
// CNonVirtualThunkMgr::DoTraceStub and ::TraceManager to help the
// debugger figure out where the thunk is going to go.
DWORD g_dwNonVirtualThunkRemotingLabelOffset = 0;
DWORD g_dwNonVirtualThunkReCheckLabelOffset = 0;

// Statics

MethodTable *CRemotingServices::s_pMarshalByRefObjectClass;    
MethodTable *CRemotingServices::s_pServerIdentityClass;

MethodDesc *CRemotingServices::s_pRPPrivateInvoke;
MethodDesc *CRemotingServices::s_pRPInvokeStatic;
MethodDesc *CRemotingServices::s_pWrapMethodDesc;
MethodDesc *CRemotingServices::s_pIsCurrentContextOK;
MethodDesc *CRemotingServices::s_pCheckCast;
MethodDesc *CRemotingServices::s_pFieldSetterDesc;
MethodDesc *CRemotingServices::s_pFieldGetterDesc;
MethodDesc *CRemotingServices::s_pObjectGetTypeDesc;
MethodDesc *CRemotingServices::s_pGetTypeDesc;
MethodDesc *CRemotingServices::s_pProxyForDomainDesc;
MethodDesc *CRemotingServices::s_pServerContextForProxyDesc;
MethodDesc *CRemotingServices::s_pServerDomainIdForProxyDesc;
DWORD CRemotingServices::s_dwServerOffsetInRealProxy;
DWORD CRemotingServices::s_dwSrvIdentityOffsetInRealProxy;
DWORD CRemotingServices::s_dwIdOffset;
DWORD CRemotingServices::s_dwTPOrObjOffsetInIdentity;
DWORD CRemotingServices::s_dwMBRIDOffset;
DWORD CRemotingServices::s_dwLeaseOffsetInIdentity;
DWORD CRemotingServices::s_dwURIOffsetInIdentity;
CrstStatic CRemotingServices::s_RemotingCrst;
BOOL CRemotingServices::s_fRemotingStarted;
MethodDesc *CRemotingServices::s_pRenewLeaseOnCallDesc;


#ifdef FEATURE_COMINTEROP
MethodDesc *CRemotingServices::s_pCreateObjectForCom;
#endif

// CTPMethodTable Statics
DWORD CTPMethodTable::s_dwCommitedTPSlots;
DWORD CTPMethodTable::s_dwReservedTPSlots;
DWORD CTPMethodTable::s_dwReservedTPIndirectionSlotSize;
DWORD CTPMethodTable::s_dwGCInfoBytes;
DWORD CTPMethodTable::s_dwMTDataSlots;
MethodTable *CTPMethodTable::s_pRemotingProxyClass;
CrstStatic CTPMethodTable::s_TPMethodTableCrst;
EEThunkHashTable *CTPMethodTable::s_pThunkHashTable;
BOOL CTPMethodTable::s_fTPTableFieldsInitialized;

#endif // !DACCESS_COMPILE


SPTR_IMPL(MethodTable, CTPMethodTable, s_pThunkTable);

#ifndef DACCESS_COMPILE

// CVirtualThunks statics
CVirtualThunks *CVirtualThunks::s_pVirtualThunks;

// CVirtualThunkMgr statics                                                     
CVirtualThunkMgr *CVirtualThunkMgr::s_pVirtualThunkMgr;

#ifndef HAS_REMOTING_PRECODE
// CNonVirtualThunk statics
CNonVirtualThunk *CNonVirtualThunk::s_pNonVirtualThunks;
SimpleRWLock* CNonVirtualThunk::s_pNonVirtualThunksListLock;

// CNonVirtualThunkMgr statics
CNonVirtualThunkMgr *CNonVirtualThunkMgr::s_pNonVirtualThunkMgr;
#endif

//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::Initialize    public
//
//  Synopsis:   Initialized remoting state
//  
//+----------------------------------------------------------------------------
VOID CRemotingServices::Initialize()
{
    STANDARD_VM_CONTRACT;

    // Initialize the remoting services critical section
    s_RemotingCrst.Init(CrstRemoting, CrstFlags(CRST_REENTRANCY|CRST_HOST_BREAKABLE));

    CTPMethodTable::Initialize();
}

INT32 CRemotingServices::IsTransparentProxy(Object* orTP)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        SO_TOLERANT;
    }
    CONTRACTL_END;
    
    INT32 fIsTPMT = FALSE;

    if(orTP != NULL)
    {
        // Check if the supplied object has transparent proxy method table
        MethodTable *pMT = orTP->GetMethodTable();
        fIsTPMT = pMT->IsTransparentProxy() ? TRUE : FALSE;
    }

    LOG((LF_REMOTING, LL_EVERYTHING, "!IsTransparentProxyEx(0x%x) returning %s",
         orTP, fIsTPMT ? "TRUE" : "FALSE"));

    return(fIsTPMT);
}


Object* CRemotingServices::GetRealProxy(Object* objTP)
{   
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        SO_TOLERANT;
    }
    CONTRACTL_END;
    
    OBJECTREF rv = NULL;

    if ((objTP != NULL) && (IsTransparentProxy(objTP)))
    {
        _ASSERTE(s_fRemotingStarted);
        rv = CTPMethodTable::GetRP(OBJECTREF(objTP));
    }

    LOG((LF_REMOTING, LL_INFO100, "!GetRealProxy(0x%x) returning 0x%x\n", objTP, OBJECTREFToObject(rv)));

    return OBJECTREFToObject(rv);
}


//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::EnsureRemotingStarted
//
//  Synopsis:   Startup the remoting services.
//
// 
//+----------------------------------------------------------------------------
VOID CRemotingServices::EnsureRemotingStarted()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    
    if (!CRemotingServices::s_fRemotingStarted)
        CRemotingServices::StartRemoting();

    if (!CTPMethodTable::s_fTPTableFieldsInitialized)
        CTPMethodTable::EnsureFieldsInitialized();
}

//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::StartRemoting    private
//
//  Synopsis:   Initialize the static fields of CRemotingServices class
//
// 
//+----------------------------------------------------------------------------
VOID CRemotingServices::StartRemoting()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Acquire the remoting lock before initializing fields
    GCX_PREEMP();

    CrstHolder ch(&s_RemotingCrst);

    // Make sure that no other thread has initialized the fields
    if (!s_fRemotingStarted)
    {
        InitActivationServicesClass();
        InitRealProxyClass();
        InitRemotingProxyClass();
        InitIdentityClass();
        InitServerIdentityClass();
        InitMarshalByRefObjectClass();
        InitRemotingServicesClass();
        InitObjectClass();
        InitLeaseClass();

        // *********   NOTE   ************ 
        // This must always be the last statement in this block to prevent races
        // 
        VolatileStore(&s_fRemotingStarted, TRUE);
        // ********* END NOTE ************        
    }
}

//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::InitActivationServicesClass    private
//
//  Synopsis:   Extract the method descriptors and fields of ActivationServices class
//
// 
//+----------------------------------------------------------------------------
VOID CRemotingServices::InitActivationServicesClass()
{
    STANDARD_VM_CONTRACT;
    
    s_pIsCurrentContextOK = MscorlibBinder::GetMethod(METHOD__ACTIVATION_SERVICES__IS_CURRENT_CONTEXT_OK);
#ifdef FEATURE_COMINTEROP
    s_pCreateObjectForCom = MscorlibBinder::GetMethod(METHOD__ACTIVATION_SERVICES__CREATE_OBJECT_FOR_COM);
#endif
}

//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::InitRealProxyClass    private
//
//  Synopsis:   Extract the method descriptors and fields of Real Proxy class
//
// 
//+----------------------------------------------------------------------------
VOID CRemotingServices::InitRealProxyClass()
{
    STANDARD_VM_CONTRACT;
    
    // Now store the methoddesc of the PrivateInvoke method on the RealProxy class
    s_pRPPrivateInvoke = MscorlibBinder::GetMethod(METHOD__REAL_PROXY__PRIVATE_INVOKE);

    // Now find the offset to the _identity field inside the 
    // RealProxy  class
    s_dwIdOffset = RealProxyObject::GetOffsetOfIdentity() - Object::GetOffsetOfFirstField();

    s_dwServerOffsetInRealProxy = RealProxyObject::GetOffsetOfServerObject() - Object::GetOffsetOfFirstField();

    s_dwSrvIdentityOffsetInRealProxy = RealProxyObject::GetOffsetOfServerIdentity() - Object::GetOffsetOfFirstField();
    
    return;
}

//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::InitRemotingProxyClass    private
//
//  Synopsis:   Extract the method descriptors and fields of RemotingProxy class
//
// 
//+----------------------------------------------------------------------------
VOID CRemotingServices::InitRemotingProxyClass()
{
    STANDARD_VM_CONTRACT;
    
    s_pRPInvokeStatic = MscorlibBinder::GetMethod(METHOD__REMOTING_PROXY__INVOKE);

    // Note: We cannot do this inside TPMethodTable::InitializeFields ..
    // that causes recursions if in some situation only the latter is called
    // If you do this you will see Asserts when running any process under CorDbg
    // This is because jitting of NV methods on MBR objects calls 
    // InitializeFields and when actually doing that we should not need to
    // JIT another NV method on some MBR object.
    CTPMethodTable::s_pRemotingProxyClass = MscorlibBinder::GetClass(CLASS__REMOTING_PROXY);
    _ASSERTE(CTPMethodTable::s_pRemotingProxyClass);
}

//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::InitServerIdentityClass    private
//
//  Synopsis:   Extract the method descriptors and fields of ServerIdentity class
//
// 
//+----------------------------------------------------------------------------
VOID CRemotingServices::InitServerIdentityClass()
{
    STANDARD_VM_CONTRACT;
    
    s_pServerIdentityClass = MscorlibBinder::GetClass(CLASS__SERVER_IDENTITY);
}

//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::InitIdentityClass    private
//
//  Synopsis:   Extract the method descriptors and fields of Identity class
//
// 
//+----------------------------------------------------------------------------
VOID CRemotingServices::InitIdentityClass()
{
    STANDARD_VM_CONTRACT;
    
    s_dwTPOrObjOffsetInIdentity = MscorlibBinder::GetFieldOffset(FIELD__IDENTITY__TP_OR_OBJECT);

    s_dwLeaseOffsetInIdentity = MscorlibBinder::GetFieldOffset(FIELD__IDENTITY__LEASE);

    s_dwURIOffsetInIdentity = MscorlibBinder::GetFieldOffset(FIELD__IDENTITY__OBJURI);
}

//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::InitMarshalByRefObjectClass    private
//
//  Synopsis:   Extract the method descriptors and fields of MarshalByRefObject class
//
// 
//+----------------------------------------------------------------------------
VOID CRemotingServices::InitMarshalByRefObjectClass()
{
    STANDARD_VM_CONTRACT;
    
    s_pMarshalByRefObjectClass = MscorlibBinder::GetClass(CLASS__MARSHAL_BY_REF_OBJECT);
    s_dwMBRIDOffset = MarshalByRefObjectBaseObject::GetOffsetOfServerIdentity() - Object::GetOffsetOfFirstField();
}

//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::InitRemotingServicesClass    private
//
//  Synopsis:   Extract the method descriptors and fields of RemotingServices class
//
// 
//+----------------------------------------------------------------------------
VOID CRemotingServices::InitRemotingServicesClass()
{
    STANDARD_VM_CONTRACT;
    
    s_pCheckCast = MscorlibBinder::GetMethod(METHOD__REMOTING_SERVICES__CHECK_CAST);

    // Need these to call wrap/unwrap from the VM (message.cpp).
    // Also used by JIT helpers to wrap/unwrap
    s_pWrapMethodDesc = MscorlibBinder::GetMethod(METHOD__REMOTING_SERVICES__WRAP);
    s_pProxyForDomainDesc = MscorlibBinder::GetMethod(METHOD__REMOTING_SERVICES__CREATE_PROXY_FOR_DOMAIN);
    s_pServerContextForProxyDesc = MscorlibBinder::GetMethod(METHOD__REMOTING_SERVICES__GET_SERVER_CONTEXT_FOR_PROXY);
    s_pServerDomainIdForProxyDesc = MscorlibBinder::GetMethod(METHOD__REMOTING_SERVICES__GET_SERVER_DOMAIN_ID_FOR_PROXY);
    s_pGetTypeDesc = MscorlibBinder::GetMethod(METHOD__REMOTING_SERVICES__GET_TYPE);
}

//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::InitObjectClass    private
//
//  Synopsis:   Extract the method descriptors and fields of Object class
//
// 
//+----------------------------------------------------------------------------
VOID CRemotingServices::InitObjectClass()
{
    STANDARD_VM_CONTRACT;
    
    s_pFieldSetterDesc = MscorlibBinder::GetMethod(METHOD__OBJECT__FIELD_SETTER);
    s_pFieldGetterDesc = MscorlibBinder::GetMethod(METHOD__OBJECT__FIELD_GETTER);
    s_pObjectGetTypeDesc = MscorlibBinder::GetMethod(METHOD__OBJECT__GET_TYPE);
}

VOID CRemotingServices::InitLeaseClass()
{
    STANDARD_VM_CONTRACT;

    s_pRenewLeaseOnCallDesc = MscorlibBinder::GetMethod(METHOD__LEASE__RENEW_ON_CALL);
}

//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::RequiresManagedActivation    private
//
//  Synopsis:   Determine if a config file has been parsed or if there
//              are any attributes on the class that would require us
//              to go into the managed activation codepath.
//              
//
//  Note:       Called by CreateProxyOrObject (JIT_NewCrossContext)
// 
//+----------------------------------------------------------------------------
ManagedActivationType __stdcall CRemotingServices::RequiresManagedActivation(TypeHandle ty)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT;
        PRECONDITION(!ty.IsNull());
    }
    CONTRACTL_END;
    
    MethodTable* pMT = ty.GetMethodTable();

    PREFIX_ASSUME(pMT != NULL);
    if (!pMT->MayRequireManagedActivation())
        return NoManagedActivation;

#ifdef _DEBUG
   
    ManagedActivationType bManaged = NoManagedActivation;
    if (pMT->IsRemotingConfigChecked())
    {
        // We have done work to figure this out in the past ... 
        // use the cached result
        bManaged = pMT->RequiresManagedActivation() ? ManagedActivation : NoManagedActivation;
    } 
    else if (pMT->IsContextful() || pMT->GetClass()->HasRemotingProxyAttribute()) 
    {
        // Contextful and classes that have a remoting proxy attribute 
        // (whether they are MarshalByRef or ContextFul) always take the slow 
        // path of managed activation
        bManaged = ManagedActivation;
    }
    else
    {
        // If we have parsed a config file that might have configured
        // this Type to be activated remotely 
        if (GetAppDomain()->IsRemotingConfigured())
        {
            bManaged = ManagedActivation;
            // We will remember if the activation is actually going
            // remote based on if the managed call to IsContextOK returned us
            // a proxy or not
        }

#ifdef FEATURE_COMINTEROP
        else if (pMT->IsComObjectType())
        {
            bManaged = ComObjectType;
        }
#endif // FEATURE_COMINTEROP

    }

#endif // _DEBUG

    if (pMT->RequiresManagedActivation()) 
    {
        // Contextful and classes that have a remoting proxy attribute 
        // (whether they are MarshalByRef or ContextFul) always take the slow 
        // path of managed activation
        _ASSERTE(bManaged == ManagedActivation);
        return ManagedActivation;
    }
    
    ManagedActivationType bMng = NoManagedActivation;
    if (!pMT->IsRemotingConfigChecked())
    {
        g_IBCLogger.LogMethodTableAccess(pMT);

        // If we have parsed a config file that might have configured
        // this Type to be activated remotely   
        if (GetAppDomain()->IsRemotingConfigured())
        {
            bMng = ManagedActivation;
            // We will remember if the activation is actually going
            // remote based on if the managed call to IsContextOK returned us
            // a proxy or not
        }
        
#ifdef FEATURE_COMINTEROP
        else if (pMT->IsComObjectType())
        {
            bMng = ComObjectType;
        }
#endif // FEATURE_COMINTEROP
        
        if (bMng == NoManagedActivation)
        {
            pMT->TrySetRemotingConfigChecked();
        }
    }

    _ASSERTE(bManaged == bMng);
    return bMng;
}

//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::CreateProxyOrObject    public
//
//  Synopsis:   Determine if the current context is appropriate
//              for activation. If the current context is OK then it creates 
//              an object else it creates a proxy.
//              
//
//  Note:       Called by JIT_NewCrossContext 
// 
//+----------------------------------------------------------------------------
OBJECTREF CRemotingServices::CreateProxyOrObject(MethodTable* pMT, 
    BOOL fIsCom /*default:FALSE*/, BOOL fIsNewObj /*default:FALSE*/)
    /* fIsCom == Did we come here through CoCreateInstance */
    /* fIsNewObj == Did we come here through Jit_NewCrossContext (newObj) */
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(!pMT->IsTransparentProxy());

        // By the time we reach here, we have already checked that the class may require
        // managed activation. This check is made either through the JIT_NewCrossContext helper
        // or Activator.CreateInstance codepath.
        PRECONDITION(pMT->MayRequireManagedActivation());
    }
    CONTRACTL_END;
    
    // Ensure remoting has been started.
    EnsureRemotingStarted();

    // Get the address of IsCurrentContextOK in managed code
    MethodDesc* pTargetMD = NULL;
    Object *pServer = NULL;

#ifdef FEATURE_COMINTEROP
    if(fIsCom)
    {
        pTargetMD = CRemotingServices::MDofCreateObjectForCom();
    }
    else
#endif // FEATURE_COMINTEROP
    {
        pTargetMD = CRemotingServices::MDofIsCurrentContextOK();
    }

    // Arrays are not created by JIT_NewCrossContext
    _ASSERTE(!pMT->IsArray());

    // Get the type seen by reflection
    REFLECTCLASSBASEREF reflectType = (REFLECTCLASSBASEREF) pMT->GetManagedClassObject();
    LPVOID pvType = NULL;
    *(REFLECTCLASSBASEREF *)&pvType = reflectType;

    // This will return either an uninitialized object or a proxy
    pServer = (Object *)CTPMethodTable::CallTarget(pTargetMD, pvType, NULL, (LPVOID)(size_t)(fIsNewObj?1:0));

    if (!pMT->IsContextful() && !pMT->IsComObjectType())
    {   
        // Cache the result of the activation attempt ... 
        // if a strictly MBR class is not configured for remote 
        // activation we will not go 
        // through this slow path next time! 
        // (see RequiresManagedActivation)
        if (IsTransparentProxy(pServer))
        {
            // Set the flag that this class is remote activate
            // which means activation will go to managed code.
            pMT->SetRequiresManagedActivation();
        }
        else
        {
            // Set only the flag that no managed checks are required
            // for this class next time.
            pMT->SetRemotingConfigChecked();
        }
    }

    LOG((LF_REMOTING, LL_INFO1000, "CreateProxyOrObject returning 0x%p\n", pServer));
    if (pMT->IsContextful())
    {
        COUNTER_ONLY(GetPerfCounters().m_Context.cObjAlloc++);
    }
    return ObjectToOBJECTREF(pServer);
}


#ifndef HAS_REMOTING_PRECODE
//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::GetStubForNonVirtualMethod   public
//
//  Synopsis:   Get a stub for a non virtual method. 
//
// 
//+----------------------------------------------------------------------------
Stub* CRemotingServices::GetStubForNonVirtualMethod(MethodDesc* pMD, LPVOID pvAddrOfCode, Stub* pInnerStub)
{
    CONTRACT (Stub*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMD));
        PRECONDITION(CheckPointer(pvAddrOfCode));
        PRECONDITION(CheckPointer(pInnerStub, NULL_OK));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;
    
    CPUSTUBLINKER sl;
    Stub* pStub = CTPMethodTable::CreateStubForNonVirtualMethod(pMD, &sl, pvAddrOfCode, pInnerStub);
    
    RETURN pStub;
}
#endif // HAS_REMOTING_PRECODE

//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::GetNonVirtualEntryPointForVirtualMethod   public
//
//  Synopsis:   Get a thunk for a non-virtual call to a virtual method.
//              Virtual methods do not normally get thunked in the vtable. This
//              is because virtual calls use the object's vtable, and proxied objects
//              would use the proxy's vtable. Hence local object (which would
//              have the real vtable) can make virtual calls without going through
//              the thunk.
//              However, if the virtual function is called non-virtually, we have
//              a problem (since this would bypass the proxy's vtable). Since this
//              is not a common case, we fix it by using a stub in such cases.
//
// 
//+----------------------------------------------------------------------------
PCODE CRemotingServices::GetNonVirtualEntryPointForVirtualMethod(MethodDesc* pMD)
{
    CONTRACT (PCODE)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMD));
        PRECONDITION(pMD->IsRemotingInterceptedViaVirtualDispatch());
        POSTCONDITION(RETVAL != NULL);
    }
    CONTRACT_END;

#ifdef HAS_REMOTING_PRECODE
    RETURN pMD->GetLoaderAllocator()->GetFuncPtrStubs()->GetFuncPtrStub(pMD, PRECODE_REMOTING);
#else
    GCX_PREEMP();
    RETURN *CTPMethodTable::GetOrCreateNonVirtualSlotForVirtualMethod(pMD);
#endif
}

#ifndef HAS_REMOTING_PRECODE
//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::DestroyThunk   public
//
//  Synopsis:   Destroy the thunk for the non virtual method. 
//
// 
//+----------------------------------------------------------------------------
void CRemotingServices::DestroyThunk(MethodDesc* pMD)
{
    WRAPPER_NO_CONTRACT;
    
    // Delegate to a helper routine
    CTPMethodTable::DestroyThunk(pMD);
} 
#endif // HAS_REMOTING_PRECODE

//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::GetDispatchInterfaceHelper   public
//
//  Synopsis:   Returns helper for dispatching interface call into the remoting system
//              with exact MethodDesc. Used for remoting of calls on generic interfaces.
//              The returned helper has MethodDesc calling convention
//+----------------------------------------------------------------------------
PCODE CRemotingServices::GetDispatchInterfaceHelper(MethodDesc* pMD)
{
    WRAPPER_NO_CONTRACT;

    return GetEEFuncEntryPoint(CRemotingServices__DispatchInterfaceCall);
}

//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::CheckCast   public
//
//  Synopsis:   Checks either 
//              (1) If the object type supports the given interface OR
//              (2) If the given type is present in the hierarchy of the 
//              object type
// 
//+----------------------------------------------------------------------------
BOOL CRemotingServices::CheckCast(OBJECTREF orTP, TypeHandle objTy, TypeHandle ty)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(orTP != NULL);
        PRECONDITION(!objTy.IsNull());
        PRECONDITION(!ty.IsNull());

        // Object class can never be an interface. We use a separate cached
        // entry for storing interfaces that the proxy supports.
        PRECONDITION(!objTy.IsInterface());
    }
    CONTRACTL_END;
    
    // Early out if someone's trying to cast us to a type desc (such as a byref,
    // array or function pointer).
    if (ty.IsTypeDesc())
        return FALSE;

    BOOL fCastOK = FALSE;    

    // (1) We are trying to cast to an interface 
    if (ty.IsInterface())
    {
        // Do a quick check for interface cast by comparing it against the
        // cached entry
        MethodTable *pItfMT = ((TRANSPARENTPROXYREF)orTP)->GetInterfaceMethodTable();
        if (NULL != pItfMT)
        {
            if(pItfMT == ty.GetMethodTable())
                fCastOK = TRUE;
            else
                fCastOK = pItfMT->CanCastToInterface(ty.GetMethodTable());
        }

        if(!fCastOK)
            fCastOK = objTy.GetMethodTable()->CanCastToInterface(ty.GetMethodTable());        
    }
    // (2) Everything else...
    else
    {
        // Walk up the class hierarchy and find a matching class
        while (ty != objTy)
        {
            if (objTy.IsNull())
            {
                // Oh-oh, the cast did not succeed. Maybe we have to refine
                // the proxy to match the clients view
                break;
            }            

            // Continue searching
            objTy = objTy.GetParent();
        }

        if(objTy == ty)
            fCastOK = TRUE;
    }

    return fCastOK;
}

//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::CheckCast   public
//
//  Synopsis:   Refine the type hierarchy that the proxy represents to match
//              the client view. If the client is trying to cast the proxy
//              to a type not supported by the server object then we 
//              return NULL
//
// 
//+----------------------------------------------------------------------------
BOOL CRemotingServices::CheckCast(OBJECTREF orTP, TypeHandle ty)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(orTP != NULL);
        PRECONDITION(!ty.IsNull());
    }
    CONTRACTL_END;

    BOOL fCastOK = FALSE;
    
    GCPROTECT_BEGIN(orTP);

    // Make sure the type being cast to has been restored.
    ty.CheckRestore();
    
    MethodTable *pMT = orTP->GetMethodTable();

    // Make sure that we have a transparent proxy
    _ASSERTE(pMT->IsTransparentProxy());

    pMT = orTP->GetTrueMethodTable();

    // Do a cast check without taking a lock
    fCastOK = CheckCast(orTP, TypeHandle(pMT), ty);

    if (!fCastOK && !ty.IsTypeDesc())
    {
        // We reach here only if any of the types in the current type hierarchy
        // represented by the proxy does not match the given type.     
        // Call a helper routine in managed RemotingServices to find out 
        // whether the server object supports the given type
        MethodDesc* pTargetMD = MDofCheckCast();
        fCastOK = CTPMethodTable::CheckCast(pTargetMD, (TRANSPARENTPROXYREF)orTP, ty);
    }

    if (fCastOK)
    {
        // Do the type equivalence tests
        CRealProxy::UpdateOptFlags(orTP);
    }

    GCPROTECT_END();
    
    LOG((LF_REMOTING, LL_INFO100, "CheckCast returning %s for object 0x%x and class 0x%x \n", (fCastOK ? "TRUE" : "FALSE")));

    return (fCastOK);
}

//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::FieldAccessor   public
//
//  Synopsis:   Sets/Gets the value of the field given an instance or a proxy
// 
//+----------------------------------------------------------------------------
void CRemotingServices::FieldAccessor(FieldDesc* pFD, OBJECTREF o, LPVOID pVal, BOOL fIsGetter)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pFD));
        PRECONDITION(o != NULL);
        PRECONDITION(CheckPointer(pVal, NULL_OK));
        PRECONDITION(o->IsTransparentProxy() || o->GetMethodTable()->IsMarshaledByRef());
    }
    CONTRACTL_END;

    MethodTable *pMT = o->GetMethodTable();
    TypeHandle fldClass;
    TypeHandle thRealObjectType;

    GCPROTECT_BEGIN(o);
    GCPROTECT_BEGININTERIOR(pVal);

    // If the field descriptor type is not exact (i.e. it's a representative
    // descriptor for a generic field) then we need to be more careful
    // determining the properties of the field.
    if (pFD->IsSharedByGenericInstantiations())
    {
        // We need to resolve the field type in the context of the actual object
        // it belongs to. If we've been handed a proxy we have to go grab the
        // proxied type for this to work.
        thRealObjectType = o->GetTrueTypeHandle();
        
        // Evaluate the field signature in the type context of the parent object.
        MetaSig sig(pFD, thRealObjectType);
        sig.NextArg();
        fldClass = sig.GetLastTypeHandleThrowing();
    }
    else
    {
        fldClass = pFD->GetFieldTypeHandleThrowing();
    }

    GCPROTECT_END();
    GCPROTECT_END();

    CorElementType fieldType = fldClass.GetSignatureCorElementType();
    UINT cbSize = GetSizeForCorElementType(fieldType);
    BOOL fIsGCRef = CorTypeInfo::IsObjRef(fieldType);
    BOOL fIsByValue = fieldType == ELEMENT_TYPE_VALUETYPE;

    if(pMT->IsMarshaledByRef())
    {
        GCX_FORBID();
        
        _ASSERTE(!o->IsTransparentProxy());
    
        // This is a reference to a real object. Get/Set the field value
        // and return
        LPVOID pFieldAddress = pFD->GetAddress((LPVOID)OBJECTREFToObject(o));
        LPVOID pDest = (fIsGetter ? pVal : pFieldAddress);
        LPVOID pSrc  = (fIsGetter ? pFieldAddress : pVal);
        if(fIsGCRef && !fIsGetter)
        {
            SetObjectReference((OBJECTREF*)pDest, ObjectToOBJECTREF(*(Object **)pSrc), o->GetAppDomain());
        }
        else if(fIsByValue) 
        {
            CopyValueClass(pDest, pSrc, fldClass.AsMethodTable(), o->GetAppDomain());
        }
        else
        {    
            CopyDestToSrc(pDest, pSrc, cbSize);
        }
    }
    else
    {
        // Call the managed code to start the field access call
        CallFieldAccessor(pFD, o, pVal, fIsGetter, fIsByValue, fIsGCRef, thRealObjectType, fldClass, fieldType, cbSize);        
    }
}

//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::CopyDestToSrc   private
//
//  Synopsis:   Copies the specified number of bytes from the src to dest
//
// 
//+----------------------------------------------------------------------------
VOID CRemotingServices::CopyDestToSrc(LPVOID pDest, LPVOID pSrc, UINT cbSize)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pDest));
        PRECONDITION(CheckPointer(pSrc));
    }
    CONTRACTL_END;

    switch (cbSize)
    {
        case 1:
            VolatileStore((INT8*)pDest, *(INT8*)pSrc);
            break;
    
        case 2:
            VolatileStore((INT16*)pDest, *(INT16*)pSrc);
            break;
    
        case 4:
            VolatileStore((INT32*)pDest, *(INT32*)pSrc);
            break;
    
        case 8:
            VolatileStore((INT64*)pDest, *(INT64*)pSrc);
            break;
    
        default:
            UNREACHABLE();
            break;
    }
}

//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::CallFieldAccessor   private
//
//  Synopsis:   Sets up the arguments and calls RealProxy::FieldAccessor
//
// 
//+----------------------------------------------------------------------------
VOID CRemotingServices::CallFieldAccessor(FieldDesc* pFD,
                                          OBJECTREF o, 
                                          VOID* pVal, 
                                          BOOL fIsGetter, 
                                          BOOL fIsByValue, 
                                          BOOL fIsGCRef,
                                          TypeHandle ty,
                                          TypeHandle fldTy,
                                          CorElementType fieldType, 
                                          UINT cbSize)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pFD));
        PRECONDITION(o != NULL);
        PRECONDITION(CheckPointer(pVal));
    }
    CONTRACTL_END;
    
    //****************************WARNING******************************
    // GC Protect all non-primitive variables
    //*****************************************************************
    
    FieldArgs fieldArgs;
    fieldArgs.obj = NULL;
    fieldArgs.val = NULL;
    fieldArgs.typeName = NULL;
    fieldArgs.fieldName = NULL;    

    GCPROTECT_BEGIN(fieldArgs);
    GCPROTECT_BEGININTERIOR(pVal);

    fieldArgs.obj = o;

    // protect the field value if it is a gc-ref type
    if(fIsGCRef)
        fieldArgs.val = ObjectToOBJECTREF(*(Object **)pVal);


    // Set up the arguments
    
    // Argument 1: String typeName
    // Argument 2: String fieldName
    // Get the type name and field name strings
    GetTypeAndFieldName(&fieldArgs, pFD, ty);
    
    // Argument 3: Object val
    OBJECTREF val = NULL;
    if(!fIsGetter)
    {
        // If we are setting a field value then we create a variant data 
        // structure to hold the field value        
        // Extract the field from the gc protected structure if it is an object
        // else use the value passed to the function
        LPVOID pvFieldVal = (fIsGCRef ? (LPVOID)&(fieldArgs.val) : pVal);
        // <REVISIT_TODO>: This can cause a GC. We need some way to protect the variant
        // data</REVISIT_TODO>
        OBJECTREF *lpVal = &val;
        GCPROTECT_BEGININTERIOR (pvFieldVal);
        CMessage::GetObjectFromStack(lpVal, &pvFieldVal, fieldType, fldTy, TRUE); 
        GCPROTECT_END ();
    }
        
    // Get the method descriptor of the call
    MethodDesc *pMD = (fIsGetter ? MDofFieldGetter() : MDofFieldSetter());
            
    // Call the field accessor function 
    //////////////////////////////// GETTER ///////////////////////////////////
    if(fIsGetter)
    {       
        // Set up the return value
        OBJECTREF oRet = NULL;

        GCPROTECT_BEGIN (oRet);
        CRemotingServices__CallFieldGetter(pMD, 
                             (LPVOID)OBJECTREFToObject(fieldArgs.obj),
                             (LPVOID)OBJECTREFToObject(fieldArgs.typeName),
                             (LPVOID)OBJECTREFToObject(fieldArgs.fieldName),
                             (LPVOID)&(oRet));

        // If we are getting a field value then extract the field value
        // based on the type of the field    
        if(fIsGCRef)
        {
            // Do a check cast to ensure that the field type and the 
            // return value are compatible
            OBJECTREF orRet = oRet;
            OBJECTREF orSaved = orRet;
            if(IsTransparentProxy(OBJECTREFToObject(orRet)))
            {
                GCPROTECT_BEGIN(orRet);

                if(!CheckCast(orRet, fldTy))
                    COMPlusThrow(kInvalidCastException, W("Arg_ObjObj"));

                orSaved = orRet;

                GCPROTECT_END();
            }

            *(OBJECTREF *)pVal = orSaved;
        }
        else if (fIsByValue) 
        {       
            // Copy from the source to the destination
            if (oRet != NULL) 
            {
                fldTy.GetMethodTable()->UnBoxIntoUnchecked(pVal, oRet);
            }
        }
        else
        {
            if (oRet != NULL)
                CopyDestToSrc(pVal, oRet->UnBox(), cbSize);
        }    
        GCPROTECT_END ();
    }
    ///////////////////////// SETTER //////////////////////////////////////////
    else
    {    
        CRemotingServices__CallFieldSetter(pMD,
                             (LPVOID)OBJECTREFToObject(fieldArgs.obj), 
                             (LPVOID)OBJECTREFToObject(fieldArgs.typeName), 
                             (LPVOID)OBJECTREFToObject(fieldArgs.fieldName),
                             (LPVOID)OBJECTREFToObject(val));
    }

    GCPROTECT_END(); // pVal
    GCPROTECT_END(); // fieldArgs
}
  
//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::GetTypeAndFieldName   private
//
//  Synopsis:   Get the type name and field name of the 
//
// 
//+----------------------------------------------------------------------------
VOID CRemotingServices::GetTypeAndFieldName(FieldArgs *pArgs, FieldDesc *pFD, TypeHandle thEnclosingClass)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pArgs));
        PRECONDITION(CheckPointer(pFD));
    }
    CONTRACTL_END;

    TypeHandle thDeclaringType = !thEnclosingClass.IsNull() ?
        pFD->GetExactDeclaringType(thEnclosingClass.AsMethodTable()) : pFD->GetEnclosingMethodTable();
    _ASSERTE(!thDeclaringType.IsNull());

    // Extract the type name and field name string
    // <REVISIT_TODO>FUTURE: Put this in the reflection data structure cache TarunA 11/26/00</REVISIT_TODO>
    StackSString ss;    
    TypeString::AppendType(ss, thDeclaringType, TypeString::FormatNamespace | TypeString::FormatFullInst);
    pArgs->typeName = StringObject::NewString(ss);

    pArgs->fieldName = StringObject::NewString(pFD->GetName());
}

//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::MatchField   private
//
//  Synopsis:   Find out whether the given field name is the same as the name
//              of the field descriptor field name.
//
// 
//+----------------------------------------------------------------------------
BOOL CRemotingServices::MatchField(FieldDesc* pCurField, LPCUTF8 szFieldName)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pCurField));
        PRECONDITION(CheckPointer(szFieldName));
    }
    CONTRACTL_END;
    
    // Get the name of the field
    LPCUTF8 szCurFieldName;
    if (FAILED(pCurField->GetName_NoThrow(&szCurFieldName)))
    {
        return FALSE;
    }
    
    return strcmp(szCurFieldName, szFieldName) == 0;
}

//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::Wrap   public
//
//  Synopsis:   Wrap a contextful object to create a proxy
//              Delegates to a helper method to do the actual work
//
// 
//+----------------------------------------------------------------------------
OBJECTREF CRemotingServices::Wrap(OBJECTREF obj)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // Basic sanity check
    VALIDATEOBJECTREF(obj);

    // ******************* WARNING ********************************************
    // Do not throw any exceptions or provoke GC without setting up a frame.
    // At present its the callers responsibility to setup a frame that can 
    // handle exceptions.
    // ************************************************************************    
    OBJECTREF orProxy = obj;
    if(obj != NULL && (obj->GetMethodTable()->IsContextful()))       
    {
        if(!IsTransparentProxy(OBJECTREFToObject(obj)))
        {
            // See if we can extract the proxy from the object
            orProxy = GetProxyFromObject(obj);
            if(orProxy == NULL)
            {
                // ask the remoting services to wrap the object
                orProxy = CRemotingServices::WrapHelper(obj);
            }
        }
    }

    return orProxy;
}

//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::WrapHelper   public
//
//  Synopsis:   Wrap an object to return a proxy. This function assumes that 
//              a fcall frame is already setup.
// 
//+----------------------------------------------------------------------------
OBJECTREF CRemotingServices::WrapHelper(OBJECTREF obj)
{
    // Basic sanity check
    VALIDATEOBJECTREF(obj);
    
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(obj != NULL);
        PRECONDITION(!IsTransparentProxy(OBJECTREFToObject(obj)));
        PRECONDITION(obj->GetMethodTable()->IsContextful());
    }
    CONTRACTL_END;
    

    // Default return value indicates an error
    OBJECTREF newobj = NULL;
    MethodDesc* pTargetMD = NULL;
    
    // Ensure remoting has been started.
    EnsureRemotingStarted();

    // Get the address of wrap in managed code        
    pTargetMD = CRemotingServices::MDofWrap();

    // call the managed method to wrap
    newobj = ObjectToOBJECTREF( (Object *)CTPMethodTable::CallTarget(pTargetMD,
                                            (LPVOID)OBJECTREFToObject(obj),
                                            NULL));    

    return newobj;
}

//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::GetProxyFromObject   public
//
//  Synopsis:   Extract the proxy from the field in the 
//              ContextBoundObject class
//              
// 
//+----------------------------------------------------------------------------
OBJECTREF CRemotingServices::GetProxyFromObject(OBJECTREF obj)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(obj != NULL);
    }
    CONTRACTL_END;

    // Basic sanity check
    VALIDATEOBJECTREF(obj);

    // We can derive a proxy for contextful types only.
    _ASSERTE(obj->GetMethodTable()->IsContextful());

    OBJECTREF srvID = (OBJECTREF)(Object*)obj->GetPtrOffset(s_dwMBRIDOffset);
    OBJECTREF orProxy = NULL;
    
    if (srvID != NULL)
        orProxy = (OBJECTREF)(Object*)srvID->GetPtrOffset(s_dwTPOrObjOffsetInIdentity);

    // This should either be null or a proxy type
    _ASSERTE((orProxy == NULL) || IsTransparentProxy(OBJECTREFToObject(orProxy)));

    return orProxy;
}

//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::IsProxyToRemoteObject   public
//
//  Synopsis:   Check if the proxy is to a remote object
//              (1) TRUE : if object is non local (ie outside this PROCESS) otherwise
//              (2) FALSE 
// 
//+----------------------------------------------------------------------------
BOOL CRemotingServices::IsProxyToRemoteObject(OBJECTREF obj)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(obj != NULL);
    }
    CONTRACTL_END;
    
    // Basic sanity check
    VALIDATEOBJECTREF(obj);
 
    // If remoting is not started, for now let us just return FALSE
    if(!s_fRemotingStarted)
        return FALSE;
 
    if(!obj->IsTransparentProxy())
        return FALSE;
    
    // so it is a transparent proxy
    AppDomain *pDomain = GetServerDomainForProxy(obj);
    if(pDomain != NULL)
        return TRUE;

    return FALSE;
}

//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::GetObjectFromProxy   public
//
//  Synopsis:   Extract the object given a proxy. 
//
// 
//+----------------------------------------------------------------------------
OBJECTREF CRemotingServices::GetObjectFromProxy(OBJECTREF obj)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(obj != NULL);
        PRECONDITION(s_fRemotingStarted);
        PRECONDITION(IsTransparentProxy(OBJECTREFToObject(obj)));
        SO_TOLERANT;
    }
    CONTRACTL_END;

    // Basic sanity check
    VALIDATEOBJECTREF(obj);

    OBJECTREF oref = NULL;
    if (CTPMethodTable__GenericCheckForContextMatch(OBJECTREFToObject(obj)))
    {
        OBJECTREF objRef = ObjectToOBJECTREF(GetRealProxy(OBJECTREFToObject(obj)));
        oref = (OBJECTREF)(Object*)objRef->GetPtrOffset(s_dwServerOffsetInRealProxy);
        if (oref != NULL)
            obj = oref; 
    }

    return obj;
}

//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::GetServerIdentityFromProxy   private
//
//  Synopsis:   Gets the server identity (if one exists) from a proxy
//              
//              
//              
// 
//+----------------------------------------------------------------------------
OBJECTREF CRemotingServices::GetServerIdentityFromProxy(OBJECTREF obj)
{    
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(obj != NULL);
        PRECONDITION(IsTransparentProxy(OBJECTREFToObject(obj)));
    }
    CONTRACTL_END;


    // Extract the real proxy underlying the transparent proxy
    OBJECTREF pObj = ObjectToOBJECTREF(GetRealProxy(OBJECTREFToObject(obj)));

    OBJECTREF id = NULL;
        
    // Extract the identity object
    pObj = (OBJECTREF)(Object*)pObj->GetPtrOffset(s_dwIdOffset);

    // Extract the _identity from the real proxy only if it is an instance of 
    // remoting proxy
    if((pObj != NULL) && IsInstanceOfServerIdentity(pObj->GetMethodTable()))
        id = pObj;

    return id;
}

//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::GetServerDomainForProxy public
//
//  Synopsis:   Returns the AppDomain corresponding to the server
//              if the proxy and the server are in the same process.
//              
// 
//+----------------------------------------------------------------------------
AppDomain *CRemotingServices::GetServerDomainForProxy(OBJECTREF proxy)
{
    CONTRACT (AppDomain*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(proxy != NULL);
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;
    
    // call the managed method 
    Context *pContext = (Context *)GetServerContextForProxy(proxy);
    if (pContext)
        RETURN pContext->GetDomain();
    else 
        RETURN NULL; 
}

//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::GetServerDomainIdForProxy public
//
//  Synopsis:   Returns the AppDomain ID corresponding to the server
//              if the proxy and the server are in the same process.
//              Returns 0 if it cannot determine.
//              
// 
//+----------------------------------------------------------------------------
int CRemotingServices::GetServerDomainIdForProxy(OBJECTREF proxy)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(proxy != NULL);
        PRECONDITION(IsTransparentProxy(OBJECTREFToObject(proxy)));
    }
    CONTRACTL_END;

    // Get the address of GetDomainIdForProxy in managed code
    MethodDesc* pTargetMD = CRemotingServices::MDofGetServerDomainIdForProxy();

    // This will just read the appDomain ID from the marshaled data
    // for the proxy. It returns 0 if the proxy is to a server in another
    // process. It may also return 0 if it cannot determine the server
    // domain ID (eg. for Well Known Object proxies).

    // call the managed method
    // <REVISIT_TODO>This cast to Int32 actually causes a potential loss
    // of data.</REVISIT_TODO>
    return (int)(INT_PTR)CTPMethodTable::CallTarget(
                pTargetMD,
                (LPVOID)OBJECTREFToObject(proxy),
                NULL);
}


//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::GetServerContextForProxy public
//
//  Synopsis:   Returns the AppDomain corresponding to the server
//              if the proxy and the server are in the same process.
//              
// 
//+----------------------------------------------------------------------------
Context *CRemotingServices::GetServerContextForProxy(OBJECTREF proxy)
{
    CONTRACT (Context*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(proxy != NULL);
        PRECONDITION(IsTransparentProxy(OBJECTREFToObject(proxy)));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;
 
    // Get the address of GetAppDomainForProxy in managed code        
    MethodDesc* pTargetMD = CRemotingServices::MDofGetServerContextForProxy();
    
    // This will return the correct VM Context object for the server if 
    // the proxy is true cross domain proxy to a server in another domain 
    // in the same process. The managed method will Assert if called on a proxy
    // which is either half-built or does not have an ObjRef ... which may
    // happen for eg. if the proxy and the server are in the same appdomain.

    // we return NULL if the server object for the proxy is in another 
    // process or if the appDomain for the server is invalid or if we cannot
    // determine the context (eg. well known object proxies).

    // call the managed method 
    RETURN (Context *)CTPMethodTable::CallTarget(
                            pTargetMD,
                            (LPVOID)OBJECTREFToObject(proxy),
                            NULL);    
}

//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::CreateProxyForDomain   public
//
//  Synopsis:   Create a proxy for the app domain object by calling marshal
//              inside the newly created domain and unmarshaling in the old
//              domain
//              
// 
//+----------------------------------------------------------------------------
OBJECTREF CRemotingServices::CreateProxyForDomain(AppDomain* pDomain)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pDomain));
    }
    CONTRACTL_END;

    // Ensure remoting has been started.
    EnsureRemotingStarted();

    MethodDesc* pTargetMD = MDOfCreateProxyForDomain();

    // Call the managed method which will marshal and unmarshal the 
    // appdomain object to create the proxy

    // We pass the ContextID of the default context of the new appDomain
    // object. This helps the boot-strapping! (i.e. entering the new domain
    // to marshal itself out).

    Object *proxy = (Object *)CTPMethodTable::CallTarget(
                                    pTargetMD, 
                                    (LPVOID)(DWORD_PTR)pDomain->GetId().m_dwId,
                                    (LPVOID)pDomain->GetDefaultContext());
    return ObjectToOBJECTREF(proxy);
}

//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::GetClass   public
//
//  Synopsis:   Extract the true class of the object whose proxy is given.
//              
//              
// 
//+----------------------------------------------------------------------------
REFLECTCLASSBASEREF CRemotingServices::GetClass(OBJECTREF pThis)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(pThis != NULL);
    }
    CONTRACTL_END;
    
    REFLECTCLASSBASEREF refClass = NULL;
    MethodTable *pMT = NULL;

    GCPROTECT_BEGIN(pThis);

    // For proxies to objects in the same appdomain, we always know the
    // correct type
    if(GetServerIdentityFromProxy(pThis) != NULL)
    {
        pMT = pThis->GetTrueMethodTable();
    }
    else
    {   
        // For everything else either we have refined the proxy to its correct type
        // or we have to consult the objref to get the true type

        MethodDesc* pTargetMD = CRemotingServices::MDofGetType();

        refClass = (REFLECTCLASSBASEREF)(ObjectToOBJECTREF((Object *)CTPMethodTable::CallTarget(pTargetMD, 
            (LPVOID)OBJECTREFToObject(pThis), NULL)));

        if(refClass == NULL)
        {
            // There was no objref associated with the proxy or it is a proxy
            // that we do not understand. 
            // In this case, we return the class that is stored in the proxy
            pMT = pThis->GetTrueMethodTable();
        }

        _ASSERTE(refClass != NULL || pMT != NULL);

        // Refine the proxy to the class just retrieved
        if(refClass != NULL)
        {
            CTPMethodTable::RefineProxy((TRANSPARENTPROXYREF)pThis, refClass->GetType());
        }
    }    

    if (refClass == NULL)
    {
        PREFIX_ASSUME(pMT != NULL);
        refClass = (REFLECTCLASSBASEREF)pMT->GetManagedClassObject();
    }

    GCPROTECT_END();

    _ASSERTE(refClass != NULL);
    return refClass;
}

//+----------------------------------------------------------------------------
//
//  Method:     CRealProxy::SetStubData   public
//
//  Synopsis:   Set the stub data in the transparent proxy
// 
//+----------------------------------------------------------------------------
FCIMPL2(VOID, CRealProxy::SetStubData, Object* orRPUNSAFE, Object* orStubDataUNSAFE)
{
    CONTRACTL
    {
        FCALL_CHECK;
    }
    CONTRACTL_END;
    
    BOOL fThrow = FALSE;
    REALPROXYREF orRP = (REALPROXYREF)ObjectToOBJECTREF(orRPUNSAFE);
    OBJECTREF orStubData = ObjectToOBJECTREF(orStubDataUNSAFE);

    if (orRP != NULL && orStubData != NULL)
    {
        TRANSPARENTPROXYREF orTP = orRP->GetTransparentProxy();
        if (orTP != NULL)
        {
            orTP->SetStubData(orStubData);
        }
        else
        {
            fThrow = TRUE;
        }
    }
    else
    {
        fThrow = TRUE;
    }
    
    if(fThrow)
        FCThrowVoid(kArgumentNullException);
}
FCIMPLEND

//+----------------------------------------------------------------------------
//
//  Method:     CRealProxy::GetStubData   public
//
//  Synopsis:   Get the stub data in the transparent proxy
// 
//+----------------------------------------------------------------------------
FCIMPL1(Object*, CRealProxy::GetStubData, Object* orRPUNSAFE)
{
    CONTRACTL
    {
        FCALL_CHECK;
    }
    CONTRACTL_END;
        
    BOOL fThrow = FALSE;
    REALPROXYREF orRP = (REALPROXYREF)ObjectToOBJECTREF(orRPUNSAFE);
    OBJECTREF orRet = NULL;

    if (orRP != NULL)
    {
        TRANSPARENTPROXYREF orTP = orRP->GetTransparentProxy();
        if (orTP != NULL)
            orRet = orTP->GetStubData();
        else
            fThrow = TRUE;
    }
    else
    {
        fThrow = TRUE;
    }
    
    if(fThrow)
        FCThrow(kArgumentNullException);

    return OBJECTREFToObject(orRet);
}
FCIMPLEND

//+----------------------------------------------------------------------------
//
//  Method:     CRealProxy::GetDefaultStub   public
//
//  Synopsis:   Get the default stub implemented by us which matches contexts
// 
//+----------------------------------------------------------------------------
FCIMPL0(LPVOID, CRealProxy::GetDefaultStub)
{
    FCALL_CONTRACT;

    return (LPVOID)CRemotingServices__CheckForContextMatch;
}
FCIMPLEND

//+----------------------------------------------------------------------------
//
//  Method:     CRealProxy::GetStub   public
//
//  Synopsis:   Get the stub pointer in the transparent proxy 
// 
//+----------------------------------------------------------------------------
FCIMPL1(LPVOID, CRealProxy::GetStub, Object* orRPUNSAFE)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(orRPUNSAFE));
    }
    CONTRACTL_END;
    
    REALPROXYREF orRP = (REALPROXYREF)ObjectToOBJECTREF(orRPUNSAFE);    
    TRANSPARENTPROXYREF orTP = orRP->GetTransparentProxy();

    return orTP->GetStub();
}
FCIMPLEND

//+----------------------------------------------------------------------------
//
//  Method:     CRealProxy::GetProxiedType   public
//
//  Synopsis:   Get the type that is represented by the transparent proxy 
// 
//+----------------------------------------------------------------------------
FCIMPL1(Object*, CRealProxy::GetProxiedType, Object* orRPUNSAFE)
{
    FCALL_CONTRACT;

    REFLECTCLASSBASEREF refClass = NULL;
    REALPROXYREF orRP = (REALPROXYREF)ObjectToOBJECTREF(orRPUNSAFE);
    HELPER_METHOD_FRAME_BEGIN_RET_1(orRP);

    TRANSPARENTPROXYREF orTP = orRP->GetTransparentProxy();

    refClass = CRemotingServices::GetClass(orTP);
    _ASSERTE(refClass != NULL);

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(refClass);
}
FCIMPLEND

//+----------------------------------------------------------------------------
//
//  Method:     CTPMethodTable::Initialize   public
//
//  Synopsis:   Initialized data structures needed for managing tranparent
//              proxies
// 
//+----------------------------------------------------------------------------
VOID CTPMethodTable::Initialize()
{
    STANDARD_VM_CONTRACT;

    s_TPMethodTableCrst.Init(CrstTPMethodTable);
}

//+----------------------------------------------------------------------------

PCODE CTPMethodTable::GetTPStubEntryPoint()
{
    LIMITED_METHOD_CONTRACT;
    return GetEEFuncEntryPoint(TransparentProxyStub);
}

PCODE CTPMethodTable::GetDelegateStubEntryPoint()
{
    LIMITED_METHOD_CONTRACT;
    return GetEEFuncEntryPoint(TransparentProxyStub_CrossContext);
}

//+----------------------------------------------------------------------------
//
//  Method:     CTPMethodTable::EnsureFieldsInitialized    private
//
//  Synopsis:   Initialize the static fields of CTPMethodTable class
//              and the thunk manager classes
//
// 
//+----------------------------------------------------------------------------
void CTPMethodTable::EnsureFieldsInitialized()
{
    CONTRACT_VOID
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(s_fTPTableFieldsInitialized);
    }
    CONTRACT_END;

    if (!s_fTPTableFieldsInitialized)
    {
        GCX_PREEMP();

        // Load Tranparent proxy class (do this before we enter the critical section)
        MethodTable* pTPMT = MscorlibBinder::GetClass(CLASS__TRANSPARENT_PROXY);
        _ASSERTE(pTPMT->IsTransparentProxy());

        CrstHolder ch(&s_TPMethodTableCrst);

        if(!s_fTPTableFieldsInitialized)
        {
            // Obtain size of GCInfo stored above the method table
            CGCDesc *pGCDesc = CGCDesc::GetCGCDescFromMT(pTPMT);
            BYTE *pGCTop = (BYTE *) pGCDesc->GetLowestSeries();
            s_dwGCInfoBytes = (DWORD)(((BYTE *) pTPMT) - pGCTop);
            _ASSERTE((s_dwGCInfoBytes & 3) == 0);

            // Obtain the number of bytes to be copied for creating the TP
            // method tables containing thunks
            _ASSERTE(((s_dwGCInfoBytes + sizeof(MethodTable)) & (sizeof(PCODE)-1)) == 0);
            s_dwMTDataSlots = ((s_dwGCInfoBytes + sizeof(MethodTable)) / sizeof(PCODE));
            _ASSERTE(sizeof(MethodTable) == MethodTable::GetVtableOffset());

            // We rely on the number of interfaces implemented by the
            // Transparent proxy being 0, so that InterfaceInvoke hints
            // fail and trap to InnerFailStub which also fails and
            // in turn traps to FailStubWorker. In FailStubWorker, we
            // determine the class being proxied and return correct slot.
            _ASSERTE(pTPMT->GetNumInterfaces() == 0);

            CVirtualThunkMgr::InitVirtualThunkManager();

            // Create the global thunk table and set the cycle between
            // the transparent proxy class and the global thunk table
            CreateTPMethodTable(pTPMT);

#ifdef HAS_REMOTING_PRECODE
            // Activate the remoting precode helper
            ActivatePrecodeRemotingThunk();
#endif // HAS_REMOTING_PRECODE

            // NOTE: This must always be the last statement in this block
            // to prevent races
            // Load Tranparent proxy class
            s_fTPTableFieldsInitialized = TRUE;
        }
    }
    
    RETURN;
}

//+----------------------------------------------------------------------------
//
//  Method:     CTPMethodTable::GetRP       public
//
//  Synopsis:   Get the real proxy backing the transparent proxy
//
//+----------------------------------------------------------------------------
REALPROXYREF CTPMethodTable::GetRP(OBJECTREF orTP)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(orTP != NULL);
        PRECONDITION(orTP->IsTransparentProxy());
        SO_TOLERANT;
    }
    CONTRACTL_END;

    return (REALPROXYREF)(((TRANSPARENTPROXYREF)orTP)->GetRealProxy());
}

//+----------------------------------------------------------------------------
//
//  Method:     CTPMethodTable::GetMethodTableBeingProxied       public
//
//  Synopsis:   Get the real type backing the transparent proxy
//
//+----------------------------------------------------------------------------
MethodTable * CTPMethodTable::GetMethodTableBeingProxied(OBJECTREF orTP)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        SO_TOLERANT;
        PRECONDITION(orTP != NULL);
        PRECONDITION(orTP->IsTransparentProxy());
    }
    CONTRACTL_END;

    return ((TRANSPARENTPROXYREF)orTP)->GetMethodTableBeingProxied();
}

#define PAGE_ROUND_UP(cb) (((cb) + g_SystemInfo.dwAllocationGranularity) & ~(g_SystemInfo.dwAllocationGranularity - 1))

//+----------------------------------------------------------------------------
//
//  Method:     CTPMethodTable::CreateTPMethodTable   private
//
//  Synopsis:   (1) Reserves a transparent proxy method table that is large 
//              enough to support the largest vtable
//              (2) Commits memory for the GC info of the global thunk table and
//              sets the cycle between the transparent proxy class and the 
//              globale thunk table.
// 
//+----------------------------------------------------------------------------

void CTPMethodTable::CreateTPMethodTable(MethodTable* pTPMT)
{
    CONTRACT_VOID {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        POSTCONDITION(CheckPointer(s_pThunkTable));
    } CONTRACT_END;

    // The largest possible vtable size 64K
    DWORD dwMaxSlots = 64*1024;

    // Allocate virtual memory that is big enough to hold a method table
    // of the maximum possible size
    DWORD dwReserveSize = 0;
    DWORD dwMethodTableReserveSize = (DWORD)(s_dwMTDataSlots * sizeof(PCODE));
    s_dwReservedTPIndirectionSlotSize = MethodTable::GetNumVtableIndirections(dwMaxSlots) * sizeof(PTR_PCODE);
    dwMethodTableReserveSize += s_dwReservedTPIndirectionSlotSize;
    
    dwMethodTableReserveSize += (DWORD)(dwMaxSlots * sizeof(PCODE));
    dwReserveSize = PAGE_ROUND_UP(dwMethodTableReserveSize);

    void *pAlloc = ::ClrVirtualAlloc(0, dwReserveSize, MEM_RESERVE | MEM_TOP_DOWN, PAGE_EXECUTE_READWRITE);
    
    if (pAlloc)
    {
        BOOL bFailed = TRUE;

        // Make sure that we have not created the one and only
        // transparent proxy method table before
        _ASSERTE(NULL == s_pThunkTable);

        // Commit the required amount of memory
        DWORD dwCommitSize = 0;

        // MethodTable memory
        DWORD dwMethodTableCommitSize = (s_dwMTDataSlots) * sizeof(PCODE);
        if (!ClrSafeInt<DWORD>::addition(0, dwMethodTableCommitSize, dwCommitSize))
        {
           COMPlusThrowHR(COR_E_OVERFLOW);
        }

        if (::ClrVirtualAlloc(pAlloc, dwCommitSize, MEM_COMMIT, PAGE_EXECUTE_READWRITE))
        {
            // Copy the fixed portion from the true TP Method Table
            memcpy(pAlloc,MTToAlloc(pTPMT, s_dwGCInfoBytes), (dwMethodTableCommitSize));

            // Initialize the transparent proxy method table
            InitThunkTable(0, dwMaxSlots, AllocToMT((BYTE *) pAlloc, s_dwGCInfoBytes));

            // At this point the transparent proxy class points to the
            // the true TP Method Table and not the transparent 
            // proxy method table. We do not use the true method table
            // any more. Instead we use the transparent proxy method table
            // for allocating transparent proxies. So, we have to make the
            // transparent proxy class point to the one and only transparent 
            // proxy method table
            pTPMT->GetClass()->SetMethodTableForTransparentProxy(s_pThunkTable);

            // Allocate the slots of the Object class method table because
            // we can reflect on the __Transparent proxy class even though 
            // we never intend to use remoting.
            _ASSERTE(NULL != g_pObjectClass);
            _ASSERTE(0 == GetCommitedTPSlots());
            if(ExtendCommitedSlots(g_pObjectClass->GetNumMethods()))
                bFailed = FALSE;
        }
        else
        {
            ClrVirtualFree(pAlloc, 0, MEM_RELEASE);
        }
        
        if(bFailed)
            DestroyThunkTable();
    }
    else {
        if (pAlloc != NULL)
            ::ClrVirtualFree(pAlloc, 0, MEM_RELEASE);
    }

    // Note that the thunk table is set to null on any failure path
    // via DestroyThunkTable
    if (!s_pThunkTable)
        COMPlusThrowOM();

    RETURN;
}

//+----------------------------------------------------------------------------
//
//  Method:     CTPMethodTable::ExtendCommitedSlots   private
//
//  Synopsis:   Extends the commited slots of transparent proxy method table to
//              the desired number
// 
//+----------------------------------------------------------------------------
BOOL CTPMethodTable::ExtendCommitedSlots(_In_range_(1,64*1024) DWORD dwSlots)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(return FALSE);
        PRECONDITION(s_dwCommitedTPSlots <= dwSlots);
        PRECONDITION(dwSlots <= s_dwReservedTPSlots);
        PRECONDITION((CVirtualThunks::GetVirtualThunks() == NULL) || 
                (s_dwCommitedTPSlots == CVirtualThunks::GetVirtualThunks()->_dwCurrentThunk));
        
        // Either we have initialized everything or we are asked to allocate
        // some slots during initialization
        PRECONDITION(s_fTPTableFieldsInitialized || (0 == s_dwCommitedTPSlots));
    }
    CONTRACTL_END;
    
    // Commit memory for TPMethodTable
    BOOL bAlloc = FALSE;
    void *pAlloc = MTToAlloc(s_pThunkTable, s_dwGCInfoBytes);
    ClrSafeInt<DWORD> dwCommitSize;
    dwCommitSize += s_dwMTDataSlots * sizeof(PCODE);
    dwCommitSize += MethodTable::GetNumVtableIndirections(dwSlots) * sizeof(PTR_PCODE);

    DWORD dwLastIndirectionSlot = s_pThunkTable->GetIndexOfVtableIndirection(s_pThunkTable->GetNumVirtuals() - 1);
    DWORD dwSlotsCommitSize = dwSlots * sizeof(PCODE);
    PCODE *pAllocSlots = (PCODE*)(((BYTE*)s_pThunkTable) + s_dwMTDataSlots * sizeof(PCODE) + s_dwReservedTPIndirectionSlotSize);

    if (dwCommitSize.IsOverflow())
    {
       return FALSE; // error condition
    }

    if (::ClrVirtualAlloc(pAlloc, dwCommitSize.Value(), MEM_COMMIT, PAGE_EXECUTE_READWRITE) && 
        ::ClrVirtualAlloc(pAllocSlots, dwSlotsCommitSize, MEM_COMMIT, PAGE_EXECUTE_READWRITE))
    {
        _ASSERTE(FitsIn<WORD>(dwSlots));
        s_pThunkTable->SetNumVirtuals((WORD)dwSlots);

        MethodTable::VtableIndirectionSlotIterator it = s_pThunkTable->IterateVtableIndirectionSlotsFrom(dwLastIndirectionSlot);
        do
        {
            it.SetIndirectionSlot(&pAllocSlots[it.GetStartSlot()]);
        }
        while (it.Next());

        bAlloc = AllocateThunks(dwSlots, dwCommitSize.Value());
    }

    return bAlloc;
}

//+----------------------------------------------------------------------------
//
//  Method:     CTPMethodTable::AllocateThunks   private
//
//  Synopsis:   Allocates the desired number of thunks for virtual methods
// 
//+----------------------------------------------------------------------------
BOOL CTPMethodTable::AllocateThunks(DWORD dwSlots, DWORD dwCommitSize)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    // Check for existing thunks
    DWORD dwCommitThunks = 0;
    DWORD dwAllocThunks = dwSlots;
    MethodTable *pThunkTable = s_pThunkTable;
    
    CVirtualThunks* pThunks = CVirtualThunks::GetVirtualThunks();
    if (pThunks)
    {
        // Compute the sizes of memory to be commited and allocated
        BOOL fCommit;
        if (dwSlots < pThunks->_dwReservedThunks)
        {
            fCommit = TRUE;
            dwCommitThunks = dwSlots;
            dwAllocThunks = 0;
        } 
        else
        {
            fCommit = (pThunks->_dwCurrentThunk != pThunks->_dwReservedThunks);
            dwCommitThunks = pThunks->_dwReservedThunks;
            dwAllocThunks = dwSlots - pThunks->_dwReservedThunks;
        }

        // Commit memory if needed
        if (fCommit)
        {
            DWORD dwCommitSizeTmp = (sizeof(CVirtualThunks) - ConstVirtualThunkSize) +
                                 ((dwCommitThunks - pThunks->_dwStartThunk) * ConstVirtualThunkSize);
            
            if (!::ClrVirtualAlloc(pThunks, dwCommitSizeTmp, MEM_COMMIT, PAGE_EXECUTE_READWRITE))
                return(NULL);

            // Generate thunks that push slot number and jump to TP stub
            DWORD dwStartSlot = pThunks->_dwStartThunk;
            DWORD dwCurrentSlot = pThunks->_dwCurrentThunk;
            while (dwCurrentSlot < dwCommitThunks)
            {
                PCODE pCode = CreateThunkForVirtualMethod(dwCurrentSlot, (BYTE *)&pThunks->ThunkCode[dwCurrentSlot-dwStartSlot]);
                pThunkTable->SetSlot(dwCurrentSlot, pCode);
                ++dwCurrentSlot;
            }

            ClrFlushInstructionCache(&pThunks->ThunkCode[pThunks->_dwCurrentThunk-dwStartSlot], 
                                     (dwCommitThunks-pThunks->_dwCurrentThunk)*ConstVirtualThunkSize);

            s_dwCommitedTPSlots = dwCommitThunks;
            pThunks->_dwCurrentThunk = dwCommitThunks;
        }
    }

    // <REVISIT_TODO>
    // Check for the avialability of a TP method table that is no longer being
    // reused </REVISIT_TODO>

    // Allocate memory if necessary
    if (dwAllocThunks)
    {
        DWORD dwReserveSize = ((sizeof(CVirtualThunks) - ConstVirtualThunkSize) +
                               ((dwAllocThunks << 1) * ConstVirtualThunkSize) +
                               g_SystemInfo.dwAllocationGranularity) & ~((size_t) g_SystemInfo.dwAllocationGranularity - 1);
        
        void *pAlloc = ::ClrVirtualAlloc(0, dwReserveSize,
                                      MEM_RESERVE | MEM_TOP_DOWN,
                                      PAGE_EXECUTE_READWRITE);
        if (pAlloc)
        {
            // Commit the required amount of memory
            DWORD dwCommitSizeTmp = (sizeof(CVirtualThunks) - ConstVirtualThunkSize) +
                                 (dwAllocThunks * ConstVirtualThunkSize);
            
            if (::ClrVirtualAlloc(pAlloc, dwCommitSizeTmp, MEM_COMMIT, PAGE_EXECUTE_READWRITE))
            {
                ((CVirtualThunks *) pAlloc)->_pNext = pThunks;
                pThunks = CVirtualThunks::SetVirtualThunks((CVirtualThunks *) pAlloc);
                pThunks->_dwReservedThunks = (dwReserveSize -
                                             (sizeof(CVirtualThunks) - ConstVirtualThunkSize)) /
                                                 ConstVirtualThunkSize;
                pThunks->_dwStartThunk = dwCommitThunks;
                pThunks->_dwCurrentThunk = dwCommitThunks;

                // Generate thunks that push slot number and jump to TP stub
                DWORD dwStartSlot = pThunks->_dwStartThunk;
                DWORD dwCurrentSlot = pThunks->_dwCurrentThunk;
                while (dwCurrentSlot < dwSlots)
                {
                    PCODE pCode = CreateThunkForVirtualMethod(dwCurrentSlot, (BYTE *)&pThunks->ThunkCode[dwCurrentSlot-dwStartSlot]);
                    pThunkTable->SetSlot(dwCurrentSlot, pCode);
                    ++dwCurrentSlot;
                }

                ClrFlushInstructionCache(&pThunks->ThunkCode[pThunks->_dwCurrentThunk-dwStartSlot], 
                                         (dwSlots-pThunks->_dwCurrentThunk)*ConstVirtualThunkSize);

                s_dwCommitedTPSlots = dwSlots;
                pThunks->_dwCurrentThunk = dwSlots;
            }
            else
            {
                ::ClrVirtualFree(pAlloc, 0, MEM_RELEASE);
                return FALSE;
            }
        }
        else
        {
            return FALSE;
        }
    }

    return TRUE;
}

//+----------------------------------------------------------------------------
//
//  Method:     CTPMethodTable::CreateTPOfClassForRP   private
//
//  Synopsis:   Creates a transparent proxy that behaves as an object of the
//              supplied class
// 
//+----------------------------------------------------------------------------
void CTPMethodTable::CreateTPOfClassForRP(TypeHandle ty, REALPROXYREF *pRP, TRANSPARENTPROXYREF *pTP)
{
    CONTRACT_VOID
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(!ty.IsNull());
        PRECONDITION(pRP != NULL);
        PRECONDITION(*pRP != NULL);
        PRECONDITION(pTP != NULL);
        POSTCONDITION(*pTP != NULL);
    }
    CONTRACT_END;

    // Ensure remoting is started.
    EnsureFieldsInitialized();

    MethodTable * pMT = ty.GetMethodTable();

    // Get the size of the VTable for the class to proxy
    DWORD dwSlots = pMT->GetNumVirtuals();

    if (dwSlots == 0)
        dwSlots = 1;

    // The global thunk table must have been initialized
    _ASSERTE(s_pThunkTable != NULL);

    // Check for the need to extend existing TP method table
    if (dwSlots > GetCommitedTPSlots())
    {
        CrstHolder ch(&s_TPMethodTableCrst);

        if (dwSlots > GetCommitedTPSlots())
        {
            if (!ExtendCommitedSlots(dwSlots))
                COMPlusThrowOM();
        }
    }

    // Create a TP Object
    IfNullThrow(*pTP = (TRANSPARENTPROXYREF) AllocateObject(GetMethodTable()));

    // Create the cycle between TP and RP
    (*pRP)->SetTransparentProxy(*pTP);

    // Make the TP behave as an object of supplied class
    (*pTP)->SetRealProxy(*pRP);

    // If we are creating a proxy for an interface then the class
    // is the object class else it is the class supplied
    if (pMT->IsInterface())
    {
        _ASSERTE(NULL != g_pObjectClass);

        (*pTP)->SetMethodTableBeingProxied(CRemotingServices::GetMarshalByRefClass());

        // Set the cached interface method table to the given interface
        // method table
        (*pTP)->SetInterfaceMethodTable(pMT);
    }
    else
    {
        (*pTP)->SetMethodTableBeingProxied(pMT);
    }

    RETURN;
}

Signature InitMessageData(messageData *msgData, 
                          FramedMethodFrame *pFrame, 
                          Module **ppModule, 
                          SigTypeContext *pTypeContext)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(msgData));
        PRECONDITION(CheckPointer(pFrame));
        PRECONDITION(CheckPointer(ppModule));
        PRECONDITION(CheckPointer(pTypeContext));
    }
    CONTRACTL_END;
    
    msgData->pFrame = pFrame;
    msgData->iFlags = 0;

    MethodDesc *pMD = pFrame->GetFunction();
    _ASSERTE(!pMD->ContainsGenericVariables());
    _ASSERTE(pMD->IsRuntimeMethodHandle());

    TypeHandle thGoverningType;
    BOOL fIsDelegate = pMD->GetMethodTable()->IsDelegate();

    // We want to calculate and store a governing type for the method since
    // sometimes the parent method table might be representative. We get the
    // exact type context from the this reference we're calling on (adjusting
    // for the fact it's a TP).

    // But cope with the common cases first for speed:
    // * If the method is not on a generic type and this is not the async
    //   delegate case (which requires us to unwrap the delegate and have a
    //   look) then we know the method desc's parent method table will be exact.
    // * We require method descs to be exact for the interface case as well (since
    //   the target object doesn't help us resolve the interface type at all).
    // * COM interop can use this code path, but that doesn't support generics so
    //   we can use the quick logic for that too.
    if ((!pMD->HasClassInstantiation() && !fIsDelegate) ||
        pMD->IsInterface() ||
        pMD->IsComPlusCall())
    {
        thGoverningType = TypeHandle(pMD->GetMethodTable());
    }
    else
    {
        MethodDesc *pTargetMD;
        MethodTable *pTargetMT;
        if (fIsDelegate)
        {
            // Async delegates are also handled differently in that the method and the
            // this are delegate wrappers round the real method and target.
            pTargetMD = COMDelegate::GetMethodDesc(pFrame->GetThis());

            // Delegates on static methods don't have a useful target instance.
            // But in that case the target method is guaranteed to have exact
            // type information.
            if (pTargetMD->IsStatic())
                pTargetMT = pTargetMD->GetMethodTable();
            else
            {
                OBJECTREF refDelegateTarget = COMDelegate::GetTargetObject(pFrame->GetThis());
                pTargetMT = refDelegateTarget->GetTrueMethodTable();
            }
        }
        else
        {
            pTargetMD = pMD;
            pTargetMT = CTPMethodTable::GetMethodTableBeingProxied(pFrame->GetThis());
        }

        // One last check to see if we can optimize the delegate case now we've
        // unwrapped it.
        if (fIsDelegate && !pTargetMD->HasClassInstantiation() && !pTargetMT->IsDelegate())
        {
            thGoverningType = TypeHandle(pTargetMD->GetMethodTable());
        }
        else
        {
            // Not quite done yet, we need to get the type that declares the method,
            // which may be a superclass of the type we're calling on.
            MethodTable *pDeclaringMT = pTargetMD->GetMethodTable();
            thGoverningType = ClassLoader::LoadGenericInstantiationThrowing(pDeclaringMT->GetModule(),
                                                                            pDeclaringMT->GetCl(),
                                                                            pTargetMD->GetExactClassInstantiation(TypeHandle(pTargetMT)));
        }
    }

    msgData->thGoverningType = thGoverningType;

    if (fIsDelegate)
    {
        DelegateEEClass* delegateCls = (DelegateEEClass*) pMD->GetMethodTable()->GetClass();

        _ASSERTE(pFrame->GetThis()->GetMethodTable()->IsDelegate());

        msgData->pDelegateMD = pMD;
        msgData->pMethodDesc = COMDelegate::GetMethodDesc(pFrame->GetThis());
        
        _ASSERTE(msgData->pMethodDesc != NULL);
        _ASSERTE(!msgData->pMethodDesc->ContainsGenericVariables());
        _ASSERTE(msgData->pMethodDesc->IsRuntimeMethodHandle());

        if (pMD == delegateCls->m_pBeginInvokeMethod)
        {
            msgData->iFlags |= MSGFLG_BEGININVOKE;
        }
        else
        {
            _ASSERTE(pMD == delegateCls->m_pEndInvokeMethod);
            msgData->iFlags |= MSGFLG_ENDINVOKE;
        }
    }
    else
    {
        msgData->pDelegateMD = NULL;
        msgData->pMethodDesc = pMD;
        _ASSERTE(msgData->pMethodDesc->IsRuntimeMethodHandle());
    }

    if (msgData->pMethodDesc->IsOneWay())
    {
        msgData->iFlags |= MSGFLG_ONEWAY;
    }

    if (msgData->pMethodDesc->IsCtor())
    {
        msgData->iFlags |= MSGFLG_CTOR;
    }

    Signature signature;
    Module *pModule;

    if (msgData->pDelegateMD)
    {
        signature = msgData->pDelegateMD->GetSignature();
        pModule = msgData->pDelegateMD->GetModule();

        // If the delegate is generic, pDelegateMD may not represent the exact instantiation so we recover it from 'this'.
        SigTypeContext::InitTypeContext(pFrame->GetThis()->GetMethodTable()->GetInstantiation(), Instantiation(), pTypeContext);
    }
    else if (msgData->pMethodDesc->IsVarArg()) 
    {
        VASigCookie *pVACookie = pFrame->GetVASigCookie();
        signature = pVACookie->signature;
        pModule = pVACookie->pModule;
        SigTypeContext::InitTypeContext(pTypeContext);

    }
    else 
    {
        signature = msgData->pMethodDesc->GetSignature();
        pModule = msgData->pMethodDesc->GetModule();
        SigTypeContext::InitTypeContext(msgData->pMethodDesc, thGoverningType, pTypeContext);
    }

    *ppModule = pModule;
    return signature;
}

VOID CRealProxy::UpdateOptFlags(OBJECTREF refTP)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        THROWS;
    }
    CONTRACTL_END
        
    DWORD hierarchyDepth = 0;
    REALPROXYREF refRP = CTPMethodTable::GetRP(refTP);
   
    OBJECTHANDLE hServerIdentity = (OBJECTHANDLE)refRP->GetPtrOffset(CRemotingServices::GetOffsetOfSrvIdentityInRP());
    if (hServerIdentity == NULL)
        return;

    // Check if the proxy has already been marked as not equivalent.
    // In which case, it can never get marked as anything else
    RealProxyObject *rpTemp = (RealProxyObject *)OBJECTREFToObject(refRP);
    
    DWORD domainID = rpTemp->GetDomainID();
    AppDomainFromIDHolder ad((ADID)domainID, TRUE);
    if (domainID == 0 || ad.IsUnloaded()) //we do not use ptr
        return;  // The appdomain the server belongs to, has been unloaded
    ad.Release();
    DWORD optFlag = rpTemp->GetOptFlags();
    if ((optFlag & OPTIMIZATION_FLAG_INITTED) &&
        !(optFlag & OPTIMIZATION_FLAG_PROXY_EQUIVALENT))
        return;
    
    OBJECTREF refSrvIdentity = ObjectFromHandle(hServerIdentity);
    // Is this a disconnected proxy ?
    if (refSrvIdentity == NULL)
        return;
    
    OBJECTREF refSrvObject = ObjectToOBJECTREF((Object *)refSrvIdentity->GetPtrOffset(CRemotingServices::GetOffsetOfTPOrObjInIdentity()));

    MethodTable *pCliMT = CTPMethodTable::GetMethodTableBeingProxied(refTP);

    BOOL bProxyQualifies = FALSE;
    BOOL bCastToSharedType = FALSE;

    // Check if modules are physically the same

    // Check the inheritance hierarchy of the server object, to find the type
    // that corresponds to the type the proxy is being cast to
    // @TODO - If being cast to an interface, currently the proxy doesnt get marked equivalent
    // @TODO - Need to check equivalency of the interface being cast to, and then reuse interface slot # on other side
    LPCUTF8 szCliTypeName, szCliNameSpace;
    szCliTypeName = pCliMT->GetFullyQualifiedNameInfo(&szCliNameSpace);
    PREFIX_ASSUME(szCliTypeName != NULL);

    MethodTable *pSrvHierarchy = refSrvObject->GetMethodTable();

    GCPROTECT_BEGIN(refRP);
    while (pSrvHierarchy)
    {
        LPCUTF8 szSrvTypeName, szSrvNameSpace;
        szSrvTypeName = pSrvHierarchy->GetFullyQualifiedNameInfo(&szSrvNameSpace);
        PREFIX_ASSUME(szSrvNameSpace != NULL);

        if (!strcmp(szCliTypeName, szSrvTypeName) && !strcmp(szCliNameSpace, szSrvNameSpace))
        {
            // Check if the types are shared. If they are, no further check neccesary
            if (pSrvHierarchy == pCliMT)
            {
                bProxyQualifies = TRUE;
                bCastToSharedType = TRUE;
            }
            else
            {
                bProxyQualifies = CRealProxy::ProxyTypeIdentityCheck(pCliMT, pSrvHierarchy);
            }
            break;
        }

        pSrvHierarchy = pSrvHierarchy->GetParentMethodTable();
        hierarchyDepth++;
    }
    GCPROTECT_END();

    optFlag = 0;
    if (bProxyQualifies && hierarchyDepth < OPTIMIZATION_FLAG_DEPTH_MASK)
    {
        optFlag = OPTIMIZATION_FLAG_INITTED | OPTIMIZATION_FLAG_PROXY_EQUIVALENT;
        if (bCastToSharedType)
            optFlag |= OPTIMIZATION_FLAG_PROXY_SHARED_TYPE;
        optFlag |= (hierarchyDepth & OPTIMIZATION_FLAG_DEPTH_MASK);
    }
    else
        optFlag = OPTIMIZATION_FLAG_INITTED;

    RealProxyObject *rpUNSAFE = (RealProxyObject *)OBJECTREFToObject(refRP);
    rpUNSAFE->SetOptFlags(optFlag);
}

BOOL CRealProxy::ProxyTypeIdentityCheck(MethodTable *pCliHierarchy, MethodTable *pSrvHierarchy)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        THROWS;
    }
    CONTRACTL_END
    // We have found the server side type that corresponds to the most derived type
    // on client side, that the proxy is cast to
    // Now do identity check on the server type hierarchy to see if there is an exact match

    BOOL bProxyQualifies = FALSE;
    do
    {
        LPCUTF8 szCliTypeName, szCliNameSpace;
        LPCUTF8 szSrvTypeName, szSrvNameSpace;
        szCliTypeName = pCliHierarchy->GetFullyQualifiedNameInfo(&szCliNameSpace);
        szSrvTypeName = pSrvHierarchy->GetFullyQualifiedNameInfo(&szSrvNameSpace);
        PREFIX_ASSUME(szCliTypeName != NULL);
        PREFIX_ASSUME(szSrvNameSpace != NULL);
    
        // If type names are different, there is no match
        if (strcmp(szCliTypeName, szSrvTypeName) ||
            strcmp(szCliNameSpace, szSrvNameSpace))
        {
            bProxyQualifies = FALSE;
            return bProxyQualifies;
        }

        PEAssembly *pClientPE = pCliHierarchy->GetAssembly()->GetManifestFile();
        PEAssembly *pServerPE = pSrvHierarchy->GetAssembly()->GetManifestFile();
        // If the PE files are different, there is no match
        if (!pClientPE->Equals(pServerPE))
        {
            bProxyQualifies = FALSE;
            return bProxyQualifies;
        }

        // If the number of interfaces implemented are different, there is no match
        if (pSrvHierarchy->GetNumInterfaces() != pCliHierarchy->GetNumInterfaces())
        {
            bProxyQualifies = FALSE;
            return bProxyQualifies;
        }

        MethodTable::InterfaceMapIterator srvItfIt = pSrvHierarchy->IterateInterfaceMap();
        MethodTable::InterfaceMapIterator cliItfIt = pCliHierarchy->IterateInterfaceMap();
        while (srvItfIt.Next())
        {
            BOOL succeeded;
            succeeded = cliItfIt.Next();
            CONSISTENCY_CHECK(succeeded);
            if (!ProxyTypeIdentityCheck(srvItfIt.GetInterface(), cliItfIt.GetInterface()))
            {
                bProxyQualifies = FALSE;
                return bProxyQualifies;
            }
        }
        
        pSrvHierarchy = pSrvHierarchy->GetParentMethodTable();
        pCliHierarchy = pCliHierarchy->GetParentMethodTable();
    }
    while (pSrvHierarchy && pCliHierarchy);

    if (pSrvHierarchy || pCliHierarchy)
    {
        bProxyQualifies = FALSE;
        return bProxyQualifies;
    }
    
    bProxyQualifies = TRUE;
    return bProxyQualifies;

}

ProfilerRemotingClientCallbackHolder::ProfilerRemotingClientCallbackHolder()
{
#ifdef PROFILING_SUPPORTED
    // If profiling is active, notify it that remoting stuff is kicking in
    BEGIN_PIN_PROFILER(CORProfilerTrackRemoting());
    GCX_PREEMP();
    g_profControlBlock.pProfInterface->RemotingClientInvocationStarted();
    END_PIN_PROFILER();
#endif // PROFILING_SUPPORTED
}

ProfilerRemotingClientCallbackHolder::~ProfilerRemotingClientCallbackHolder()
{
#ifdef PROFILING_SUPPORTED
    // If profiling is active, tell profiler we've made the call, received the
    // return value, done any processing necessary, and now remoting is done.
    BEGIN_PIN_PROFILER(CORProfilerTrackRemoting());
    GCX_PREEMP();
    g_profControlBlock.pProfInterface->RemotingClientInvocationFinished();
    END_PIN_PROFILER();
#endif // PROFILING_SUPPORTED
}

enum
{
    CALLTYPE_INVALIDCALL        = 0x0,          // Important:: sync this with RealProxy.cs
    CALLTYPE_METHODCALL         = 0x1,          // Important:: sync this with RealProxy.cs
    CALLTYPE_CONSTRUCTORCALL    = 0x2           // Important:: sync this with RealProxy.cs
};

extern "C" void STDCALL TransparentProxyStubPatch();

//+----------------------------------------------------------------------------
//
//  Method:     TransparentProxyStubWorker
//
//  Synopsis:   This function gets control in two situations
//              (1) When a call is made on the transparent proxy it delegates to              
//              PrivateInvoke method on the real proxy
//              (2) When a call is made on the constructor it again delegates to the 
//              PrivateInvoke method on the real proxy.
//              
// 
//+----------------------------------------------------------------------------
extern "C" UINT32 STDCALL TransparentProxyStubWorker(TransitionBlock * pTransitionBlock, TADDR pMethodDescOrSlot)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        ENTRY_POINT;
        PRECONDITION(CheckPointer(pTransitionBlock));
    }
    CONTRACTL_END;

    UINT fpRetSize = 0;

    FrameWithCookie<TPMethodFrame> frame(pTransitionBlock);
    TPMethodFrame * pFrame = &frame;

    //we need to zero out the return value buffer because we will report it during GC
#ifdef ENREGISTERED_RETURNTYPE_MAXSIZE
    ZeroMemory (pFrame->GetReturnValuePtr(), ENREGISTERED_RETURNTYPE_MAXSIZE);
#else
    *(ARG_SLOT *)pFrame->GetReturnValuePtr() = 0;
#endif

    // For virtual calls the slot number is pushed but for 
    // non virtual calls/interface invoke the method descriptor is already 
    // pushed
    MethodDesc * pMD;
    if ((pMethodDescOrSlot >> 16) == 0)
    {
        // The frame is not completly setup at this point.
        // Do not throw exceptions or provoke GC
        MethodTable* pMT = CTPMethodTable::GetMethodTableBeingProxied(pFrame->GetThis());
        _ASSERTE(pMT);
        
        // Replace the slot number with the method descriptor on the stack
        pMD = pMT->GetMethodDescForSlot((WORD)pMethodDescOrSlot);
    }
    else
    {
        pMD = dac_cast<PTR_MethodDesc>(pMethodDescOrSlot);
    }
    pFrame->SetFunction(pMD);

    pFrame->Push();

    // Give debugger opportunity to stop here now that we know the MethodDesc *
    TransparentProxyStubPatch();

    INSTALL_UNWIND_AND_CONTINUE_HANDLER;

    if (g_pConfig->UseNewCrossDomainRemoting())
    {
        BOOL bOptSuccess = FALSE;
        CrossDomainChannel cdc;
        bOptSuccess = cdc.CheckCrossDomainCall(pFrame); 
        if (bOptSuccess)
        {
            fpRetSize = cdc.GetFPReturnSize();
            goto Done;
        }
    }

    {
        messageData msgData;
        Module *pModule = NULL;
        SigTypeContext inst;
        Signature signature = InitMessageData(&msgData, pFrame, &pModule, &inst);

        _ASSERTE(!signature.IsEmpty() && pModule);

        // Allocate metasig on the stack
        MetaSig mSig(signature, pModule, &inst);
        msgData.pSig = &mSig; 

        MethodDesc *pMD = pFrame->GetFunction();    
        if (pMD->GetMethodTable()->IsDelegate())
        {
            // check that there is only one target
            if (COMDelegate::IsTrueMulticastDelegate(pFrame->GetThis()))
            {
                COMPlusThrow(kArgumentException, W("Remoting_Delegate_TooManyTargets"));
            }
        }

        {
            ProfilerRemotingClientCallbackHolder profilerHolder;

            OBJECTREF pThisPointer = NULL;

            if (pMD->GetMethodTable()->IsDelegate())
            {
                // this is an async call
                _ASSERTE(pFrame->GetThis()->GetMethodTable()->IsDelegate());

                pThisPointer = COMDelegate::GetTargetObject(pFrame->GetThis());
            }
            else
            {
                pThisPointer = pFrame->GetThis();
            }

            OBJECTREF firstParameter;
            MethodDesc* pTargetMD = NULL;
            size_t callType = CALLTYPE_INVALIDCALL;
            
            // We are invoking either the constructor or a method on the object
            if(pMD->IsCtor())
            {
                // Get the address of PrivateInvoke in managed code
                pTargetMD = CRemotingServices::MDofPrivateInvoke();
                _ASSERTE(pThisPointer->IsTransparentProxy());
                
                firstParameter = CTPMethodTable::GetRP(pThisPointer);

                // Set a field to indicate that it is a constructor call
                callType = CALLTYPE_CONSTRUCTORCALL;
            }
            else
            {
                // Set a field to indicate that it is a method call
                callType = CALLTYPE_METHODCALL;

                if (pThisPointer->IsTransparentProxy())
                {
                    // Extract the real proxy underlying the transparent proxy
                    firstParameter = CTPMethodTable::GetRP(pThisPointer);

                    // Get the address of PrivateInvoke in managed code
                    pTargetMD = CRemotingServices::MDofPrivateInvoke();
                    _ASSERTE(pTargetMD);
                }
                else 
                {
                    // must be async if this is not a TP 
                    _ASSERTE(pMD->GetMethodTable()->IsDelegate());
                    firstParameter = NULL;
                    
                    // Get the address of PrivateInvoke in managed code
                    pTargetMD = CRemotingServices::MDofInvokeStatic();
                }

                // Go ahead and call PrivateInvoke on Real proxy. There is no need to 
                // catch exceptions thrown by it
                // See RealProxy.cs
            }

            _ASSERTE(pTargetMD);

            // Call the appropriate target
            CTPMethodTable::CallTarget(pTargetMD, (LPVOID)OBJECTREFToObject(firstParameter), (LPVOID)&msgData, (LPVOID)callType);

            // Check for the need to trip thread
            if (GetThread()->CatchAtSafePointOpportunistic())
            {
                // There is no need to GC protect the return object as
                // TPFrame is GC protecting it
                CommonTripThread();
            }
        }  // ProfilerClientCallbackHolder

        {
            mSig.Reset();

            ArgIterator argit(&mSig);

#ifdef _TARGET_X86_
            // Set the number of bytes to pop for x86
            pFrame->SetCbStackPop(argit.CbStackPop());
#endif // _TARGET_X86_
        
            fpRetSize = argit.GetFPReturnSize();
        }
    }

Done: ;

    pFrame->Pop();

    UNINSTALL_UNWIND_AND_CONTINUE_HANDLER;

    return fpRetSize;
}


// Helper due to inability to combine SEH with anything interesting.
BOOL CTPMethodTable::CheckCastHelper(MethodDesc* pTargetMD, LPVOID pFirst, LPVOID pSecond)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pTargetMD));
        PRECONDITION(CheckPointer(pFirst, NULL_OK));
        PRECONDITION(CheckPointer(pSecond, NULL_OK));
    }
    CONTRACTL_END;

    // Actual return type is a managed 'bool', so only look at a CLR_BOOL-sized
    // result.  The high bits are undefined on AMD64.  (Note that a narrowing
    // cast to CLR_BOOL will not work since it is the same as checking the
    // size_t result != 0.)
    LPVOID ret = CallTarget(pTargetMD, pFirst, pSecond);
    return *(CLR_BOOL*)StackElemEndianessFixup(&ret, sizeof(CLR_BOOL));
}



//+----------------------------------------------------------------------------
//
//  Method:     CTPMethodTable::CheckCast   private
//
//  Synopsis:   Call the managed checkcast method to determine whether the 
//              server type can be cast to the given type
//              
//              
// 
//+----------------------------------------------------------------------------
BOOL CTPMethodTable::CheckCast(MethodDesc* pTargetMD, TRANSPARENTPROXYREF orTP, TypeHandle ty)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pTargetMD));
        PRECONDITION(orTP != NULL);
        PRECONDITION(!ty.IsNull());
    }
    CONTRACTL_END;
    
    REFLECTCLASSBASEREF reflectType = NULL;
    LPVOID pvType = NULL;    
    BOOL fCastOK = FALSE;
    
    typedef struct _GCStruct
    {
        TRANSPARENTPROXYREF orTP;
        REALPROXYREF orRP;
    } GCStruct;

    GCStruct gcValues;
    gcValues.orTP = orTP;
    gcValues.orRP = GetRP(orTP);

    GCPROTECT_BEGIN (gcValues);

    reflectType = (REFLECTCLASSBASEREF) ty.GetMethodTable()->GetManagedClassObject();
    *(REFLECTCLASSBASEREF *)&pvType = reflectType;

    fCastOK = CheckCastHelper(pTargetMD, 
                              (LPVOID)OBJECTREFToObject(gcValues.orRP),
                              pvType);    

    if (fCastOK)
    {
        _ASSERTE(s_fTPTableFieldsInitialized);

        // The cast succeeded. Replace the current type in the proxy
        // with the given type. 

        CrstHolder ch(&s_TPMethodTableCrst);
        
        if (ty.IsInterface())
        {
            // We replace the cached interface method table with the interface
            // method table that we are trying to cast to. This will ensure that
            // casts to this interface, which are likely to happen, will succeed.
            gcValues.orTP->SetInterfaceMethodTable(ty.GetMethodTable());
        }
        else
        {
            MethodTable *pCurrent = gcValues.orTP->GetMethodTableBeingProxied();

            BOOL fDerivedClass = FALSE;
            // Check whether this class derives from the current class
            fDerivedClass = CRemotingServices::CheckCast(gcValues.orTP, ty,
                                                         TypeHandle(pCurrent));
            // We replace the current method table only if we cast to a more 
            // derived class
            if (fDerivedClass)
            {
                // Set the method table in the proxy to the given method table
                RefineProxy(gcValues.orTP, ty);
            }
        }
    }

    GCPROTECT_END();
    return fCastOK;
}

//+----------------------------------------------------------------------------
//
//  Method:     CTPMethodTable::RefineProxy   public
//
//  Synopsis:   Set the method table in the proxy to the given class' method table.
//              Additionally, expand the TP method table to the required number of slots.
//              
// 
//+----------------------------------------------------------------------------
void CTPMethodTable::RefineProxy(TRANSPARENTPROXYREF orTP, TypeHandle ty)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(orTP != NULL);
        PRECONDITION(!ty.IsNull());
    }
    CONTRACTL_END;
    
    // Do the expansion only if necessary
    MethodTable *pMT = ty.GetMethodTable();
    
    if (pMT != orTP->GetMethodTableBeingProxied())
    {
        orTP->SetMethodTableBeingProxied(pMT);

        // Extend the vtable if necessary
        DWORD dwSlots = pMT->GetNumVirtuals();

        if (dwSlots == 0)
            dwSlots = 1;
    
        if((dwSlots > GetCommitedTPSlots()) && !ExtendCommitedSlots(dwSlots))
        {
            // We failed to extend the committed slots. Out of memory.
            COMPlusThrowOM();
        }

    }
}

#ifndef HAS_REMOTING_PRECODE
//+----------------------------------------------------------------------------
//
//  Method:     CTPMethodTable::GetOrCreateNonVirtualSlotForVirtualMethod private
//
//  Synopsis:   Get a slot for a non-virtual call to a virtual method.
//
//+----------------------------------------------------------------------------
PTR_PCODE CTPMethodTable::GetOrCreateNonVirtualSlotForVirtualMethod(MethodDesc* pMD)
{
    CONTRACT (PTR_PCODE)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMD));
        PRECONDITION(pMD->IsRemotingInterceptedViaVirtualDispatch());
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    // Ensure the TP MethodTable's fields have been initialized.
    EnsureFieldsInitialized();

    PTR_PCODE pSlot;

    {
        // Create the thunk in a thread safe manner
        CrstHolder ch(&s_TPMethodTableCrst);

        // NOTE: CNonVirtualThunk::SetNonVirtualThunks() depends on the lock being initialized
        CNonVirtualThunk::InitializeListLock();

        // Create hash table if we do not have one yet
        if (s_pThunkHashTable == NULL)
        {
            NewHolder <EEThunkHashTable> pTempHash(new EEThunkHashTable());

            LockOwner lock = {&s_TPMethodTableCrst, IsOwnerOfCrst};
            IfNullThrow(pTempHash->Init(23,&lock));

            s_pThunkHashTable = pTempHash.Extract();
        }

        if (!s_pThunkHashTable->GetValue(pMD, (HashDatum *)&pSlot))
        {
            PCODE pThunkCode = CreateNonVirtualThunkForVirtualMethod(pMD);

            _ASSERTE(CNonVirtualThunkMgr::IsThunkByASM(pThunkCode));
            _ASSERTE(CNonVirtualThunkMgr::GetMethodDescByASM(pThunkCode));

            // Set the generated thunk once and for all..            
            CNonVirtualThunk *pThunk = CNonVirtualThunk::SetNonVirtualThunks((BYTE*)pThunkCode);

            // Remember the thunk address in a hash table 
            // so that we dont generate it again
            pSlot = (PTR_PCODE)pThunk->GetAddrOfCode();
            s_pThunkHashTable->InsertValue(pMD, (HashDatum)pSlot);
        }
    }

    RETURN pSlot;
}

//+----------------------------------------------------------------------------
//
//  Method:     CTPMethodTable::DestroyThunk   public
//
//  Synopsis:   Destroy the thunk for the non virtual method. 
//
// 
//+----------------------------------------------------------------------------
void CTPMethodTable::DestroyThunk(MethodDesc* pMD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END;
    
    if(s_pThunkHashTable)
    {
        CrstHolder ch(&s_TPMethodTableCrst);

        LPVOID pvCode = NULL;
        s_pThunkHashTable->GetValue(pMD, (HashDatum *)&pvCode);
        CNonVirtualThunk *pThunk = NULL;
        if(NULL != pvCode)
        {
            pThunk = CNonVirtualThunk::AddrToThunk(pvCode);
            delete pThunk;
            s_pThunkHashTable->DeleteValue(pMD);
        }
    }
} 
#endif // HAS_REMOTING_PRECODE

static LPVOID CallTargetWorker1(MethodDesc* pTargetMD,
                                            LPVOID pvFirst,
                                            LPVOID pvSecond)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;
    STATIC_CONTRACT_SO_INTOLERANT;

    LPVOID ret = NULL;
    PCODE pTarget = pTargetMD->GetSingleCallableAddrOfCode();

#if defined(DEBUGGING_SUPPORTED)
    if (CORDebuggerTraceCall())
    {
        g_pDebugInterface->TraceCall((const BYTE*)pTarget);
    }
#endif // DEBUGGING_SUPPORTED


    BEGIN_CALL_TO_MANAGED();

    ret = CTPMethodTable__CallTargetHelper2((const BYTE*)pTarget, pvFirst, pvSecond);

    END_CALL_TO_MANAGED();

    return ret;
}  


//+----------------------------------------------------------------------------
//
//  Method:     CTPMethodTable::CallTarget   private
//
//  Synopsis:   Calls the target method on the given object
// 
//+----------------------------------------------------------------------------
LPVOID __stdcall CTPMethodTable::CallTarget (MethodDesc* pTargetMD,
                                            LPVOID pvFirst,
                                            LPVOID pvSecond)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        SO_INTOLERANT;
        PRECONDITION(CheckPointer(pTargetMD));
        PRECONDITION(CheckPointer(pvFirst, NULL_OK));
        PRECONDITION(CheckPointer(pvSecond, NULL_OK));
    }
    CONTRACTL_END;
    
#ifdef _DEBUG

    Thread* curThread = GetThread();
    
    Object* ObjRefTable[OBJREF_TABSIZE];

    if (curThread)
        memcpy(ObjRefTable, curThread->dangerousObjRefs, sizeof(curThread->dangerousObjRefs));
    
#endif // _DEBUG

    LPVOID ret = CallTargetWorker1(pTargetMD, pvFirst, pvSecond);
    
#ifdef _DEBUG
    // Restore dangerousObjRefs when we return back to EE after call
    if (curThread)
        memcpy(curThread->dangerousObjRefs, ObjRefTable, sizeof(curThread->dangerousObjRefs));

    ENABLESTRESSHEAP ();
#endif // _DEBUG

    return ret;
}


static LPVOID CallTargetWorker2(MethodDesc* pTargetMD,
                                    LPVOID pvFirst,
                                    LPVOID pvSecond,
                                    LPVOID pvThird)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;
    STATIC_CONTRACT_SO_INTOLERANT;

    LPVOID ret = NULL;
    PCODE pTarget = pTargetMD->GetSingleCallableAddrOfCode();

#if defined(DEBUGGING_SUPPORTED)
    if (CORDebuggerTraceCall())
    {
        g_pDebugInterface->TraceCall((const BYTE*)pTarget);
    }
#endif // DEBUGGING_SUPPORTED

    BEGIN_CALL_TO_MANAGED();

    ret = CTPMethodTable__CallTargetHelper3((const BYTE*)pTarget, pvFirst, pvSecond, pvThird);

    END_CALL_TO_MANAGED();
    return ret;

}

LPVOID __stdcall CTPMethodTable::CallTarget (MethodDesc* pTargetMD,
                                            LPVOID pvFirst,
                                            LPVOID pvSecond,
                                            LPVOID pvThird)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        SO_INTOLERANT;
        PRECONDITION(CheckPointer(pTargetMD));
        PRECONDITION(CheckPointer(pvFirst, NULL_OK));
        PRECONDITION(CheckPointer(pvSecond, NULL_OK));
        PRECONDITION(CheckPointer(pvThird, NULL_OK));
    }
    CONTRACTL_END;
    
#ifdef _DEBUG
    Thread* curThread = GetThread();
    
    Object* ObjRefTable[OBJREF_TABSIZE];
    if (curThread)
        memcpy(ObjRefTable, curThread->dangerousObjRefs, sizeof(curThread->dangerousObjRefs));
    
#endif // _DEBUG

    LPVOID ret = CallTargetWorker2(pTargetMD, pvFirst, pvSecond, pvThird);
    
#ifdef _DEBUG
    // Restore dangerousObjRefs when we return back to EE after call
    if (curThread)
        memcpy(curThread->dangerousObjRefs, ObjRefTable, sizeof(curThread->dangerousObjRefs));

    ENABLESTRESSHEAP ();
#endif // _DEBUG
    
    return ret;
}


#ifndef HAS_REMOTING_PRECODE
//+----------------------------------------------------------------------------
//
//  Method:     CNonVirtualThunk::SetNextThunk   public
//
//  Synopsis:   Creates a thunk for the given address and adds it to the global
//              list
// 
//+----------------------------------------------------------------------------
CNonVirtualThunk* CNonVirtualThunk::SetNonVirtualThunks(const BYTE* pbCode)
{    
    CONTRACT (CNonVirtualThunk*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pbCode));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;
    
    CNonVirtualThunk *pThunk = new CNonVirtualThunk(pbCode);            

    // Put the generated thunk in a global list
    // Note: this is called when a NV thunk is being created ..
    // The TPMethodTable critsec is held at this point
    pThunk->SetNextThunk();

    // Set up the stub manager if necessary
    CNonVirtualThunkMgr::InitNonVirtualThunkManager();

    RETURN pThunk;
}

//+----------------------------------------------------------------------------
//
//  Method:     CNonVirtualThunk::~CNonVirtualThunk   public
//
//  Synopsis:   Deletes the thunk from the global list of thunks
//              
// 
//+----------------------------------------------------------------------------
CNonVirtualThunk::~CNonVirtualThunk()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(s_pNonVirtualThunks));
    }
    CONTRACTL_END;

    CNonVirtualThunk* pCurr = s_pNonVirtualThunks;
    CNonVirtualThunk* pPrev = NULL;
    BOOL found = FALSE;

    // Note: This is called with the TPMethodTable critsec held
    while(!found && (NULL != pCurr))
    {
        if(pCurr == this)
        {
            found = TRUE;
            SimpleRWLock::SimpleWriteLockHolder swlh(s_pNonVirtualThunksListLock);
            
            // Unlink from the chain 
            if(NULL != pPrev)
            {                    
                pPrev->_pNext = pCurr->_pNext;
            }
            else
            {
               // First entry needs to be deleted
                s_pNonVirtualThunks = pCurr->_pNext;
            }
        }
        pPrev = pCurr;
        pCurr = pCurr->_pNext;
    }

    _ASSERTE(found);
}
#endif // HAS_REMOTING_PRECODE

//+----------------------------------------------------------------------------
//
//  Method:     CVirtualThunkMgr::InitVirtualThunkManager   public
//
//  Synopsis:   Adds the stub manager to aid debugger in stepping into calls
//              
// 
//+----------------------------------------------------------------------------
void CVirtualThunkMgr::InitVirtualThunkManager()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;
    
    // This is function is already threadsafe since this method is called from within a 
    // critical section  
    if(NULL == s_pVirtualThunkMgr)
    {
        // Add the stub manager for vtable calls
        s_pVirtualThunkMgr =  new CVirtualThunkMgr();
    
        StubManager::AddStubManager(s_pVirtualThunkMgr);
    }

}

#endif // !DACCESS_COMPILE

//+----------------------------------------------------------------------------
//
//  Method:     CVirtualThunkMgr::CheckIsStub_Internal   public
//
//  Synopsis:   Returns TRUE if the given address is the starting address of
//              the transparent proxy stub
// 
//+----------------------------------------------------------------------------
BOOL CVirtualThunkMgr::CheckIsStub_Internal(PCODE stubStartAddress)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    BOOL bIsStub = FALSE;

#ifndef DACCESS_COMPILE
    if (!IsThunkByASM(stubStartAddress))
        return FALSE;
    if(NULL != FindThunk((const BYTE *) stubStartAddress))
        bIsStub = TRUE;
#endif // !DACCESS_COMPILE

    return bIsStub;
}

#ifndef DACCESS_COMPILE

//+----------------------------------------------------------------------------
//
//  Method:     CVirtualThunkMgr::Entry2MethodDesc   public
//
//  Synopsis:   Convert a starting address to a MethodDesc
// 
//+----------------------------------------------------------------------------
MethodDesc *CVirtualThunkMgr::Entry2MethodDesc(PCODE StubStartAddress, MethodTable *pMT)
{
    CONTRACT (MethodDesc*)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMT, NULL_OK));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;
    
    if (s_pVirtualThunkMgr == NULL)
        RETURN NULL;

    if (!pMT)
        RETURN NULL;

    if (!s_pVirtualThunkMgr->CheckIsStub_Internal(StubStartAddress))
        RETURN NULL;

    RETURN GetMethodDescByASM(StubStartAddress, pMT);
}

//+----------------------------------------------------------------------------
//
//  Method:     CVirtualThunkMgr::FindThunk   private
//
//  Synopsis:   Finds a thunk that matches the given starting address
// 
//+----------------------------------------------------------------------------
LPBYTE CVirtualThunkMgr::FindThunk(const BYTE *stubStartAddress)
{
    CONTRACT (LPBYTE)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(stubStartAddress, NULL_OK));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        SO_TOLERANT;
    }
    CONTRACT_END;

    CVirtualThunks* pThunks = CVirtualThunks::GetVirtualThunks();
    LPBYTE pThunkAddr = NULL;

    while(NULL != pThunks)
    {
        DWORD dwStartSlot = pThunks->_dwStartThunk;
        DWORD dwCurrSlot = pThunks->_dwStartThunk;
        DWORD dwMaxSlot = pThunks->_dwCurrentThunk;        
        while (dwCurrSlot < dwMaxSlot)
        {
            LPBYTE pStartAddr =  pThunks->ThunkCode[dwCurrSlot-dwStartSlot].pCode;
            if((stubStartAddress >= pStartAddr) &&
               (stubStartAddress <  (pStartAddr + ConstVirtualThunkSize)))
            {
                pThunkAddr = pStartAddr;
                break;
            }            
            ++dwCurrSlot;
        }

        pThunks = pThunks->GetNextThunk();            
     }

     RETURN pThunkAddr;
}

#endif // !DACCESS_COMPILE

#ifndef HAS_REMOTING_PRECODE

#ifndef DACCESS_COMPILE

//+----------------------------------------------------------------------------
//
//  Method:     CNonVirtualThunkMgr::InitNonVirtualThunkManager   public
//
//  Synopsis:   Adds the stub manager to aid debugger in stepping into calls
//              
// 
//+----------------------------------------------------------------------------
void CNonVirtualThunkMgr::InitNonVirtualThunkManager()
{   
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    // This function is already thread safe since this method is called from within a 
    // critical section
    if(NULL == s_pNonVirtualThunkMgr)
    {
        // Add the stub manager for non vtable calls
        s_pNonVirtualThunkMgr = new CNonVirtualThunkMgr();
        
        StubManager::AddStubManager(s_pNonVirtualThunkMgr);
    }
}

#endif // !DACCESS_COMPILE

//+----------------------------------------------------------------------------
//
//  Method:     CNonVirtualThunkMgr::CheckIsStub_Internal   public
//
//  Synopsis:   Returns TRUE if the given address is the starting address of
//              one of our thunks
// 
//+----------------------------------------------------------------------------
BOOL CNonVirtualThunkMgr::CheckIsStub_Internal(PCODE stubStartAddress)
{
    WRAPPER_NO_CONTRACT;
    
    BOOL bIsStub = FALSE;

#ifndef DACCESS_COMPILE
    if (!IsThunkByASM(stubStartAddress))
        return FALSE;
    if(NULL != FindThunk((const BYTE *) stubStartAddress))
        bIsStub = TRUE;       
#endif // !DACCESS_COMPILE

    return bIsStub;
}

#ifndef DACCESS_COMPILE

//+----------------------------------------------------------------------------
//
//  Method:     CNonVirtualThunkMgr::Entry2MethodDesc   public
//
//  Synopsis:   Convert a starting address to a MethodDesc
// 
//+----------------------------------------------------------------------------
MethodDesc *CNonVirtualThunkMgr::Entry2MethodDesc(PCODE StubStartAddress, MethodTable *pMT)
{
    CONTRACT (MethodDesc*)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMT, NULL_OK));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    if (s_pNonVirtualThunkMgr == NULL)
        RETURN NULL;
    
    if (!s_pNonVirtualThunkMgr->CheckIsStub_Internal(StubStartAddress))
        RETURN NULL;

    RETURN GetMethodDescByASM(StubStartAddress);
}

//+----------------------------------------------------------------------------
//
//  Method:     CNonVirtualThunkMgr::FindThunk   private
//
//  Synopsis:   Finds a thunk that matches the given starting address
// 
//+----------------------------------------------------------------------------
CNonVirtualThunk* CNonVirtualThunkMgr::FindThunk(const BYTE *stubStartAddress)
{
    CONTRACT (CNonVirtualThunk*)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(stubStartAddress, NULL_OK));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        SO_TOLERANT;
    }
    CONTRACT_END;
    
    SimpleRWLock::SimpleReadLockHolder srlh(CNonVirtualThunk::GetThunksListLock());
    CNonVirtualThunk* pThunk = CNonVirtualThunk::GetNonVirtualThunks();

    while(NULL != pThunk)
    {
        if(stubStartAddress == pThunk->GetThunkCode())           
            break;

        pThunk = pThunk->GetNextThunk();            
    }

    RETURN pThunk;
}

#endif // !DACCESS_COMPILE

#endif // HAS_REMOTING_PRECODE


#ifndef DACCESS_COMPILE

//+----------------------------------------------------------------------------
//+- HRESULT MethodDescDispatchHelper(MethodDesc* pMD, ARG_SLOT[] args, ARG_SLOT *pret)
//+----------------------------------------------------------------------------
HRESULT MethodDescDispatchHelper(MethodDescCallSite* pMethodCallSite, ARG_SLOT args[], ARG_SLOT *pret)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pMethodCallSite));
        PRECONDITION(CheckPointer(args));
        PRECONDITION(CheckPointer(pret));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    EX_TRY
    {
        *pret = pMethodCallSite->Call_RetArgSlot(args);
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}


#ifdef FEATURE_COMINTEROP

//+----------------------------------------------------------------------------
//
//  Method:     VOID  CRemotingServices::CallSetDCOMProxy(OBJECTREF realProxy, IUnknown* pUnk)
//
//+----------------------------------------------------------------------------

VOID CRemotingServices::CallSetDCOMProxy(OBJECTREF realProxy, IUnknown* pUnk)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(realProxy != NULL);
        PRECONDITION(CheckPointer(pUnk, NULL_OK));
    }
    CONTRACTL_END;

    GCPROTECT_BEGIN(realProxy);

    MethodDescCallSite setDCOMProxy(METHOD__REAL_PROXY__SETDCOMPROXY, &realProxy);
    
    ARG_SLOT args[] =
    {
        ObjToArgSlot(realProxy),
        (ARG_SLOT)pUnk
    };

    ARG_SLOT ret;
    MethodDescDispatchHelper(&setDCOMProxy, args, &ret);
    
    GCPROTECT_END();
}


BOOL CRemotingServices::CallSupportsInterface(OBJECTREF realProxy, REFIID iid, ARG_SLOT* pret)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(realProxy != NULL);
        PRECONDITION(CheckPointer(pret));
    }
    CONTRACTL_END;
    
    BOOL    fResult = TRUE;
    
    GCPROTECT_BEGIN(realProxy);
    
    MethodDescCallSite supportsInterface(METHOD__REAL_PROXY__SUPPORTSINTERFACE, &realProxy);

    ARG_SLOT args[] =
    {
        ObjToArgSlot(realProxy),
        (ARG_SLOT)&iid
    };

    HRESULT hr = MethodDescDispatchHelper(&supportsInterface, args, pret);

    // It is allowed for the managed code to return a NULL interface pointer without returning
    // a failure HRESULT. This is done for performance to avoid having to throw an exception.
    // If this occurs, we need to return E_NOINTERFACE.
    if ((*(IUnknown**)pret) == NULL)
        hr = E_NOINTERFACE;

    if (FAILED(hr))
        fResult = FALSE;

    GCPROTECT_END();
    return fResult;
}
#endif // FEATURE_COMINTEROP

//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::GetStubForInterfaceMethod
//
//  Synopsis:   Given the exact interface method we wish to invoke on, return
//              the entry point of a stub that will correctly transition into
//              the remoting system passing it this method.
//              The stubs is just another kind of precode. They are cached
//              in per appdomain hash.
//
// 
//+----------------------------------------------------------------------------
PCODE CRemotingServices::GetStubForInterfaceMethod(MethodDesc *pItfMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pItfMD));
        PRECONDITION(pItfMD->IsInterface() && !pItfMD->IsStatic());
    }
    CONTRACTL_END;

    return pItfMD->GetLoaderAllocator()->GetFuncPtrStubs()->GetFuncPtrStub(pItfMD, PRECODE_STUB);
}

#endif // !DACCESS_COMPILE
#endif // FEATURE_REMOTING
