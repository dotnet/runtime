// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: ComInterfaceMarshaler.cpp
//

#include "common.h"

#include "vars.hpp"
#include "excep.h"
#include "stdinterfaces.h"
#include "interoputil.h"
#include "comcallablewrapper.h"
#include "runtimecallablewrapper.h"
#include "cominterfacemarshaler.h"
#include "interopconverter.h"
#include "notifyexternals.h"
#include "comdelegate.h"
#include "olecontexthelpers.h"


//--------------------------------------------------------------------------------
// COMInterfaceMarshaler::COMInterfaceMarshaler()
// ctor
//--------------------------------------------------------------------------------
COMInterfaceMarshaler::COMInterfaceMarshaler()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_pWrapperCache = RCWCache::GetRCWCache();
    _ASSERTE(m_pWrapperCache);

    m_pUnknown = NULL;
    m_pIdentity = NULL;
    m_flags = RCW::CF_None;
    m_pThread = NULL;
}

//--------------------------------------------------------------------------------
// COMInterfaceMarshaler::~COMInterfaceMarshaler()
// dtor
//--------------------------------------------------------------------------------
COMInterfaceMarshaler::~COMInterfaceMarshaler()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
}

//--------------------------------------------------------------------------------
// VOID COMInterfaceMarshaler::Init(IUnknown* pUnk, MethodTable* pClassMT, Thread *pThread, DWORD flags)
// init
//--------------------------------------------------------------------------------
VOID COMInterfaceMarshaler::Init(IUnknown* pUnk, MethodTable* pClassMT, Thread *pThread, DWORD flags /*= RCW::CF_None*/)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(CheckPointer(pClassMT, NULL_OK));
        PRECONDITION(CheckPointer(pThread));
        PRECONDITION(m_typeHandle.IsNull() && m_pUnknown == NULL && m_pIdentity == NULL);
    }
    CONTRACTL_END;

    // NOTE ** this struct is temporary,
    // so NO ADDREF of the COM Interface pointers
    m_pUnknown = pUnk;

    // for now use the IUnknown as the Identity
    m_pIdentity = pUnk;

    m_typeHandle = TypeHandle(pClassMT);

    m_pThread = pThread;

    m_flags = flags;
}

//--------------------------------------------------------------------------------
// void COMInterfaceMarshaler::CreateObjectRef(BOOL fDuplicate, OBJECTREF *pComObj)
//  Creates an RCW of the proper type.
//--------------------------------------------------------------------------------
void COMInterfaceMarshaler::CreateObjectRef(BOOL fDuplicate, OBJECTREF *pComObj, IUnknown **ppIncomingIP, MethodTable *pIncomingItfMT, bool bIncomingIPAddRefed)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(IsProtectedByGCFrame(pComObj));
        PRECONDITION(!m_typeHandle.IsNull());
        PRECONDITION(m_typeHandle.IsComObjectType());
        PRECONDITION(m_pThread == GetThreadNULLOk());
        PRECONDITION(pIncomingItfMT == NULL || pIncomingItfMT->IsInterface());
    }
    CONTRACTL_END;

    BOOL fExisting = FALSE;

    // instantiate an instance of m_typeHandle
    if (*pComObj != NULL)
    {
        // the instance already exists and was passed in *pComObj
        fExisting = TRUE;
    }
    else if (m_typeHandle.IsComObjectType())
    {
        // ordinary RCW
        *pComObj = ComObject::CreateComObjectRef(m_typeHandle.GetMethodTable());
    }
    else
    {
        _ASSERTE(!"Creating a COM wrapper for WinRT delegates (which do not inherit from __ComObject) is not supported.");
    }

    // make sure we "pin" the syncblock before switching to preemptive mode
    SyncBlock *pSB = (*pComObj)->GetSyncBlock();
    pSB->SetPrecious();
    DWORD dwSyncBlockIndex = pSB->GetSyncBlockIndex();

    NewRCWHolder pNewRCW;
    pNewRCW = RCW::CreateRCW(m_pUnknown, dwSyncBlockIndex, m_flags, m_typeHandle.GetMethodTable());

    if (fDuplicate)
    {
        // let us fix the identity to be the wrapper,
        // so looking up this IUnknown won't return this wrapper
        // this would allow users to call WrapIUnknownWithCOMObject
        // to create duplicate wrappers
        pNewRCW->m_pIdentity = pNewRCW;
        m_pIdentity = (IUnknown*)(LPVOID)pNewRCW;
    }

    // If the class is an extensible RCW (managed class deriving from a ComImport class)
    if (fExisting)
    {
        MethodTable *pClassMT = (*pComObj)->GetMethodTable();
        if (pClassMT != g_pBaseCOMObject && pClassMT->IsExtensibleRCW())
        {
            // WinRT scenario: we're initializing an RCW for a managed object that is
            // already in the process of being constructed (we're at the point of calling
            // to the base class ctor.
            // Just mark the RCW as aggregated (in this scenario we don't go down
            // ComClassFactory::CreateAggregatedInstance)
            pNewRCW->MarkURTAggregated();
        }
    }
    else
    {
        if (m_typeHandle.GetMethodTable() != g_pBaseCOMObject && m_typeHandle.GetMethodTable()->IsExtensibleRCW())
        {
            // Normal COM aggregation case - we're just in the process of allocating the object
            // If the managed class has a default constructor then call it
            MethodDesc *pCtorMD = m_typeHandle.GetMethodTable()->GetDefaultConstructor();
            if (pCtorMD)
            {
                PREPARE_NONVIRTUAL_CALLSITE_USING_METHODDESC(pCtorMD);
                DECLARE_ARGHOLDER_ARRAY(CtorArgs, 1);
                CtorArgs[ARGNUM_0]  = OBJECTREF_TO_ARGHOLDER(*pComObj);

                // Call the ctor...
                CALL_MANAGED_METHOD_NORET(CtorArgs);
            }
        }
    }

    // We expect that, at most, the first entry will already be allocated.
    int nNextFreeIdx = pNewRCW->m_aInterfaceEntries[0].IsFree() ? 0 : 1;

    if (!m_itfTypeHandle.IsNull() && !m_itfTypeHandle.IsTypeDesc())
    {
        MethodTable *pItfMT = m_itfTypeHandle.AsMethodTable();

        // Just in case we've already cached it with pIncomingItfMT
        if (pItfMT != pIncomingItfMT)
        {
            // We know that the object supports pItfMT but we don't have the right interface pointer at this point
            // (*ppIncomingIP is not necessarily the right one) so we'll QI for it. Note that this is not just a
            // perf optimization, we need to store pItfMT in the RCW in case it has variance and/or provide the
            // non-generic IEnumerable::GetEnumerator method.

            IID iid;
            SafeComHolder<IUnknown> pItfIP;

            if (SUCCEEDED(pNewRCW->CallQueryInterface(pItfMT, Instantiation(), &iid, &pItfIP)))
            {
                _ASSERTE(pNewRCW->m_aInterfaceEntries[nNextFreeIdx].IsFree());

                pNewRCW->m_aInterfaceEntries[nNextFreeIdx].Init(pItfMT, pItfIP);

                // Don't hold ref count if RCW is aggregated
                if (!pNewRCW->IsURTAggregated())
                {
                    pItfIP.SuppressRelease();
                }
            }
        }
    }


    {
        // Make sure that RCWHolder is declared before GC is forbidden - its destructor may trigger GC.
        RCWHolder pRCW(m_pThread);
        pRCW.InitNoCheck(pNewRCW);

        // We may get back an RCW from another STA thread and we can only touch the RCW if we hold the lock,
        // otherwise we may AV if the STA thread dies and takes the RCW with it
        RCWCache::LockHolder lh(m_pWrapperCache);

        GCX_FORBID();

        // see if somebody beat us to it..
        BOOL fInserted = m_pWrapperCache->FindOrInsertWrapper_NoLock(m_pIdentity, &pRCW, !fExisting);
        if (!fInserted)
        {
            // somebody beats us in creating a wrapper. Let's determine whether we should insert our
            // wrapper as a duplicate, or use the other wrapper that is already in the cache

            // If the object instance already exists, we have no choice but to insert this wrapper
            // as a duplicate. If we return the one that is already in the cache, we would return
            // a different object!
            BOOL fInsertAsDuplicateWrapper = fExisting;

            if (fInsertAsDuplicateWrapper)
            {
                // we need to keep this wrapper separate so we'll insert it with the alternate identity
                // (just as if fDuplicate was TRUE)
                pNewRCW->m_pIdentity = pNewRCW;
                m_pIdentity = (IUnknown*)(LPVOID)pNewRCW;

                fInserted = m_pWrapperCache->FindOrInsertWrapper_NoLock(m_pIdentity, &pRCW, !fExisting);
                _ASSERTE(fInserted);

                pNewRCW.SuppressRelease();
            }
            else
            {
                // grab the new object
                *pComObj = (OBJECTREF)pRCW->GetExposedObject();
            }
        }
        else
        {
            // If we did insert this wrapper in the table, make sure we don't delete it.
            pNewRCW.SuppressRelease();
        }
    }

    _ASSERTE(*pComObj != NULL);

#ifdef _DEBUG
    if (!m_typeHandle.IsNull() && m_typeHandle.IsComObjectType())
    {
        // make sure this object supports all the COM Interfaces in the class
        EnsureCOMInterfacesSupported(*pComObj, m_typeHandle.GetMethodTable());
    }
#endif
}

//--------------------------------------------------------------------------------
// OBJECTREF COMInterfaceMarshaler::FindOrCreateObjectRef()
// Find the wrapper for this COM IP, might have to create one if not found.
// It will return null for out-of memory scenarios.  It also notices if we have
// an IP that is disguised as an unmanaged object, sitting on top of a
// managed object.
//
// The ppIncomingIP parameter lets COMInterfaceMarshaler call methods on the
// interface pointer that came in from unmanaged code (pUnk could be the result of QI'ing such an IP for IUnknown).
//
// If pIncomingItfMT is not NULL, we'll cache ppIncomingIP into the created RCW, so that
// 1) RCW variance would work if we can't load the right type from RuntimeClassName, but the method returns a interface
// 2) avoid a second QI for the same interface type
//--------------------------------------------------------------------

OBJECTREF COMInterfaceMarshaler::FindOrCreateObjectRef(IUnknown **ppIncomingIP, MethodTable *pIncomingItfMT /* = NULL */)
{
    WRAPPER_NO_CONTRACT;

    return FindOrCreateObjectRefInternal(ppIncomingIP, pIncomingItfMT, /* bIncomingIPAddRefed = */ true);
}

OBJECTREF COMInterfaceMarshaler::FindOrCreateObjectRef(IUnknown *pIncomingIP, MethodTable *pIncomingItfMT /* = NULL */)
{
    WRAPPER_NO_CONTRACT;

    return FindOrCreateObjectRefInternal(&pIncomingIP, pIncomingItfMT, /* bIncomingIPAddRefed = */ false);
}

OBJECTREF COMInterfaceMarshaler::FindOrCreateObjectRefInternal(IUnknown **ppIncomingIP, MethodTable *pIncomingItfMT, bool bIncomingIPAddRefed)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(m_pThread == GetThreadNULLOk());
        PRECONDITION(pIncomingItfMT == NULL || pIncomingItfMT->IsInterface());
    }
    CONTRACTL_END;

    OBJECTREF oref = NULL;

    // (I)
    // Initial check in our cache
    // Skip if we want a unique object.
    if (!NeedUniqueObject())
    {
        // Protect oref as SafeAddRef may trigger GC
        GCPROTECT_BEGIN_THREAD(m_pThread, oref);

        {
            // We may get back an RCW from another STA thread and we can only touch the RCW if we hold the lock,
            // otherwise we may AV if the STA thread dies and takes the RCW with it
            RCWCache::LockHolder lh(m_pWrapperCache);

            RCWHolder pRCW(m_pThread);
            m_pWrapperCache->FindWrapperInCache_NoLock(
                m_pIdentity,
                &pRCW);
            if (!pRCW.IsNull())
            {
                oref = (OBJECTREF)pRCW->GetExposedObject();
            }
        }

        GCPROTECT_END();

        if (oref != NULL)
            return oref;
    }

    // (II)
    // okay let us create a wrapper and an instance for this IUnknown

    // Find a suitable class to instantiate the instance
    if (ppIncomingIP != NULL)
    {
        InitializeObjectClass(*ppIncomingIP);
    }
    else
    {
        InitializeObjectClass(m_pUnknown);
    }


    GCPROTECT_BEGIN_THREAD(m_pThread, oref)
    {
        CreateObjectRef(NeedUniqueObject(), &oref, ppIncomingIP, pIncomingItfMT, bIncomingIPAddRefed);
    }
    GCPROTECT_END();

    return oref;
}

VOID COMInterfaceMarshaler::InitializeExistingComObject(OBJECTREF *pComObj, IUnknown **ppIncomingIP)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(!m_typeHandle.IsNull());
        PRECONDITION(IsProtectedByGCFrame(pComObj));
    }
    CONTRACTL_END;

    CreateObjectRef(NeedUniqueObject(), pComObj, ppIncomingIP, /* pIncomingItfMT = */ NULL, /* bIncomingIPAddRefed = */ true);
}

//--------------------------------------------------------------------------------
// VOID COMInterfaceMarshaler::InitializeObjectClass()
//--------------------------------------------------------------------------------
VOID COMInterfaceMarshaler::InitializeObjectClass(IUnknown *pIncomingIP)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // If the marshaller's type handle is null, compute what it should be.
    if (m_typeHandle.IsNull())
    {
        // We no longer support the mapping from ITypeInfo to Type (i.e. MethodTable*).
        // This was previously provided by IProvideClassinfo. If the type handle isn't
        // set fallback to the opaque __ComObject type.
        m_typeHandle = TypeHandle(g_pBaseCOMObject);
        _ASSERTE(!m_typeHandle.IsNull());
    }
}

// VOID EnsureCOMInterfacesSupported(OBJECTREF oref, MethodTable* pClassMT)
// Make sure the oref supports all the COM interfaces in the class
VOID COMInterfaceMarshaler::EnsureCOMInterfacesSupported(OBJECTREF oref, MethodTable* pClassMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pClassMT));
        PRECONDITION(pClassMT->IsComObjectType());
    }
    CONTRACTL_END;

    // Make sure the COM object supports all the COM imported interfaces that the new
    // wrapper class implements.
    GCPROTECT_BEGIN(oref);
    MethodTable::InterfaceMapIterator it = pClassMT->IterateInterfaceMap();

    while (it.Next())
    {
        MethodTable *pItfMT = it.GetInterface();
        if (!pItfMT)
            COMPlusThrow(kInvalidCastException, IDS_EE_CANNOT_COERCE_COMOBJECT);

        if (pItfMT->IsComImport())
        {
            if (!Object::SupportsInterface(oref, pItfMT))
                COMPlusThrow(kInvalidCastException, IDS_EE_CANNOT_COERCE_COMOBJECT);
        }
    }

    GCPROTECT_END();
}

bool COMInterfaceMarshaler::NeedUniqueObject()
{
    LIMITED_METHOD_CONTRACT;
    return (m_flags & RCW::CF_NeedUniqueObject) != 0;
}
