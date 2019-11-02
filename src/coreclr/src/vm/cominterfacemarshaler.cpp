// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
#include "winrttypenameconverter.h"
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

    m_fIReference = false;
    m_fIReferenceArray = false;
    m_fNonRCWType = false;
    m_flags = RCW::CF_None;
    m_pCallback = NULL;
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

    if (!SupportsIInspectable())
    {
        if (!m_typeHandle.IsNull() && m_typeHandle.IsProjectedFromWinRT())
            m_flags |= RCW::CF_SupportsIInspectable;
    }
}

// Returns true if the type is WinRT-redirected and requires special marshaler functionality
// to convert an interface pointer to its corresponding managed instance.
static bool IsRedirectedToNonRCWType(MethodTable *pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (pMT == nullptr)
    {
        return false;
    }

    WinMDAdapter::RedirectedTypeIndex index;
    if (!WinRTTypeNameConverter::ResolveRedirectedType(pMT, &index))
    {
        return false;
    }

    if (index == WinMDAdapter::RedirectedTypeIndex_System_Collections_Generic_KeyValuePair)
    {
        // we need to convert IKeyValuePair to boxed KeyValuePair
        return true;
    }

    // redirected runtime classes are not RCWs
    WinMDAdapter::WinMDTypeKind kind;
    WinMDAdapter::GetRedirectedTypeInfo(index, nullptr, nullptr, nullptr, nullptr, nullptr, &kind);

    return kind == WinMDAdapter::WinMDTypeKind_Runtimeclass;
}

//--------------------------------------------------------------------------------
// VOID COMInterfaceMarshaler::InitializeObjectClass()
//--------------------------------------------------------------------------------
VOID COMInterfaceMarshaler::InitializeObjectClass(IUnknown *pIncomingIP)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!DontResolveClass())
    {

        // If we are not in an APPX process, and an object could have a strongly typed RCW as a COM CoClass,
        // we prefer that to the WinRT class.This preserves compatibility for exisitng code.
        // If we are in an APPX process we do not check for IProvideClassInfo.
        if (m_typeHandle.IsNull() && !AppX::IsAppXProcess())
        {
            EX_TRY
            {
                m_typeHandle = GetClassFromIProvideClassInfo(m_pUnknown);

                if (!m_typeHandle.IsNull() && !m_typeHandle.IsComObjectType())
                {
                    m_typeHandle = TypeHandle();  // Clear the existing one.
                }
            }
            EX_CATCH
            {
            }
            EX_END_CATCH(RethrowTerminalExceptions);
            if(!m_typeHandle.IsNull())
                return;
        }

        // Note that the actual type may be a subtype of m_typeHandle if it's not sealed.
        if ((m_typeHandle.IsNull() || !m_typeHandle.GetMethodTable()->IsSealed()) && WinRTSupported())
        {
            bool fInspectable = SupportsIInspectable();
            EX_TRY
            {
                // QI for IInspectable first. m_fInspectable at this point contains information about the interface
                // pointer that we could gather from the signature or API call. But, since an object can be acquired
                // as a plain IUnknown and later started being treated as a WinRT object, we always eagerly QI for
                // IInspectable as part of the IInspectable::GetRuntimeClassName call.  Also note that we may discover
                // this IInspectable is really a IReference<T> or IReferenceArray<T> for WinRT-compatible T's.
                TypeHandle typeHandle = GetClassFromIInspectable(pIncomingIP, &fInspectable, &m_fIReference, &m_fIReferenceArray);

                if (!typeHandle.IsNull())
                {
                    // GetRuntimeClassName could return a interface or projected value type name
                    if (m_fIReference || m_fIReferenceArray)
                    {
                        // this has already been pre-processed - it is the IReference/IReferenceArray generic argument
                        m_typeHandle = typeHandle;
                    }
                    if (typeHandle.IsInterface())
                    {
                        m_itfTypeHandle = typeHandle;
                    }
                    else if (IsRedirectedToNonRCWType(typeHandle.GetMethodTable()))
                    {
                        m_typeHandle = typeHandle;
                        m_fNonRCWType = true;
                    }
                    else if (!typeHandle.IsValueType())
                    {
                        // if the type returned from GetRuntimeClassName is a class, it must be derived from __ComObject
                        // or be a WinRT delegate for us to be able to build an RCW for it
                        if (typeHandle.IsComObjectType() ||
                            (!typeHandle.IsTypeDesc() && typeHandle.GetMethodTable()->IsDelegate() && (typeHandle.IsProjectedFromWinRT() || WinRTTypeNameConverter::IsRedirectedType(typeHandle.GetMethodTable()))))
                        {
                            m_typeHandle = typeHandle;
                        }
                    }
                }
            }
            EX_CATCH
            {
            }
            EX_END_CATCH(RethrowTerminalExceptions);

            if (fInspectable)
            {
                m_flags |= RCW::CF_SupportsIInspectable;
            }
            else
            {
                _ASSERTE_MSG(m_typeHandle.IsNull() || !SupportsIInspectable(),
                    "Acquired an object which should be IInspectable according to metadata but the QI failed.");
            }
        }
    }

    if (m_typeHandle.IsNull())
        m_typeHandle = TypeHandle(g_pBaseCOMObject);
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
        PRECONDITION(m_typeHandle.IsComObjectType() || (m_typeHandle.GetMethodTable()->IsDelegate() && (m_typeHandle.GetMethodTable()->IsProjectedFromWinRT() || WinRTTypeNameConverter::IsRedirectedType(m_typeHandle.GetMethodTable()))));
        PRECONDITION(m_pThread == GetThread());
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
        // If delegates were to take this path, we need to fix the identity in MethodPtrAux later
        _ASSERTE(!(m_flags & RCW::CF_QueryForIdentity));

        // delegate backed by a WinRT interface pointer
        *pComObj = COMDelegate::ConvertWinRTInterfaceToDelegate(m_pIdentity, m_typeHandle.GetMethodTable());
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
    else if (m_flags & RCW::CF_QueryForIdentity)
    {
        // pNewRCW has the real Identity in this case and we need to use it to insert into RCW cache
        m_pIdentity = (IUnknown *)pNewRCW->m_pIdentity;
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
    // (SetJupiterObject gets the first shot at this.)
    int nNextFreeIdx = pNewRCW->m_aInterfaceEntries[0].IsFree() ? 0 : 1;

    // Only cache WinRT interfaces
    // Note that we can't use SupportsIInspectable here because we could be talking to a CCW
    // which supports IInspectable by default
    if (ppIncomingIP != NULL &&
        *ppIncomingIP != NULL &&
        pIncomingItfMT != NULL &&
        pIncomingItfMT->IsLegalNonArrayWinRTType())
    {
        _ASSERTE(pIncomingItfMT->IsInterface());
        _ASSERTE(pNewRCW->m_aInterfaceEntries[nNextFreeIdx].IsFree());

        //
        // The incoming interface pointer is of type m_pItfMT
        // Cache the result into RCW for better performance and for variance support
        // For example, GetFilesAsync() returns Windows.Storage.StorageFileView and this type
        // is not in any WinMD. Because GetFilesAsync actually returns IVectorView<StorageFile>,
        // we know this RCW supports this interface, and putting it into the cache would make sure
        // casting this RCW to IVectorView<object> works
        //
        pNewRCW->m_aInterfaceEntries[nNextFreeIdx++].Init(pIncomingItfMT, *ppIncomingIP);

        // Don't hold ref count if RCW is aggregated
        if (!pNewRCW->IsURTAggregated())
        {
            if (bIncomingIPAddRefed)
            {
                // Transfer the ref from ppIncomingIP to internal cache
                // This will only happen in WinRT scenarios to reduce risk of this change
                *ppIncomingIP = NULL;
            }
            else
            {
                // Otherwise AddRef by ourselves
                RCW_VTABLEPTR(pNewRCW);
                SafeAddRef(*ppIncomingIP);
            }

            RCWWalker::AfterInterfaceAddRef(pNewRCW);
        }

        // Save GetEnumerator method if necessary
        // Do this after we "AddRef" on ppIncomingIP otherwise we would call Release on it
        // without a AddRef
        pNewRCW->SetGetEnumeratorMethod(pIncomingItfMT);
    }

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

                    RCWWalker::AfterInterfaceAddRef(pNewRCW);
                }
            }
        }
    }


    {
        // Make sure that RCWHolder is declared before GC is forbidden - its destructor may trigger GC.
        RCWHolder pRCW(m_pThread);
        pRCW.InitNoCheck(pNewRCW);

        // We may get back an RCW from another STA thread (mainly in WinRT factory cache scenario,
        // as those factories are typically singleton), and we can only touch the RCW if we hold the lock,
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

            if (!fInsertAsDuplicateWrapper)
            {
                // Shall we use the RCW that is already in the cache?
                if (m_pCallback && !m_pCallback->ShouldUseThisRCW(pRCW.GetRawRCWUnsafe()))
                {
                    // No - let's insert our wrapper as a duplicate instead
                    fInsertAsDuplicateWrapper = TRUE;

                    // Initialize pRCW again and make sure sure pRCW is indeed our new wrapper
                    pRCW.UnInit();
                    pRCW.InitNoCheck(pNewRCW);
                }
            }

            if (fInsertAsDuplicateWrapper)
            {
                // we need to keep this wrapper separate so we'll insert it with the alternate identity
                // (just as if fDuplicate was TRUE)
                pNewRCW->m_pIdentity = pNewRCW;
                m_pIdentity = (IUnknown*)(LPVOID)pNewRCW;

                fInserted = m_pWrapperCache->FindOrInsertWrapper_NoLock(m_pIdentity, &pRCW, !fExisting);
                _ASSERTE(fInserted);

                pNewRCW.SuppressRelease();

                if (m_pCallback)
                    m_pCallback->OnRCWCreated(pRCW.GetRawRCWUnsafe());
            }
            else
            {
                // Somebody beat us in creating the wrapper. Let's use that
                if (m_pCallback)
                    m_pCallback->OnRCWCacheHit(pRCW.GetRawRCWUnsafe());

                // grab the new object
                *pComObj = (OBJECTREF)pRCW->GetExposedObject();
            }
        }
        else
        {
            // If we did insert this wrapper in the table, make sure we don't delete it.
            pNewRCW.SuppressRelease();

            if (m_pCallback)
                m_pCallback->OnRCWCreated(pRCW.GetRawRCWUnsafe());
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


// OBJECTREF COMInterfaceMarshaler::IReferenceUnbox()
//--------------------------------------------------------------------------------

void COMInterfaceMarshaler::IReferenceUnbox(IUnknown **ppIncomingIP, OBJECTREF *poref, bool bIncomingIPAddRefed)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(m_fIReference);
        PRECONDITION(m_pThread == GetThread());
    }
    CONTRACTL_END;

    OBJECTREF unboxed = NULL;
    _ASSERTE(m_typeHandle.AsMethodTable()->IsLegalNonArrayWinRTType());

    // Create a temporary RCW.  Call into managed.  Let managed query for a closed generic instantiation
    // like IReference<Int32> (including the GUID calculation & QI) then call the Value property.  That
    // will use the existing interop code to safely marshal the value.
    // Also, make sure we create a duplicate RCW in this case so that next time we won't end up getting
    // this RCW from cache
    COMInterfaceMarshaler marshaler;

    DWORD flags = RCW::CF_DontResolveClass | RCW::CF_NeedUniqueObject;

    marshaler.Init(m_pUnknown, g_pBaseCOMObject, m_pThread, flags);

    if (m_pCallback)
        marshaler.SetCallback(m_pCallback);

    OBJECTREF oref = marshaler.FindOrCreateObjectRefInternal(ppIncomingIP, /* pIncomingItfMT = */ NULL, bIncomingIPAddRefed);

    IReferenceOrIReferenceArrayUnboxWorker(oref, m_typeHandle, FALSE, poref);
}

// void COMInterfaceMarshaler::IReferenceOrIReferenceArrayUnboxWorker()
//--------------------------------------------------------------------------------

// static
void COMInterfaceMarshaler::IReferenceOrIReferenceArrayUnboxWorker(OBJECTREF oref, TypeHandle thT, BOOL fIsIReferenceArray, OBJECTREF *porefResult)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    GCPROTECT_BEGIN(oref);

    // Get IReference<SomeType> or IReferenceArray<SomeType>
    Instantiation inst(&thT, 1);
    TypeHandle openType;
    MethodDesc* pMD = NULL;
    if (fIsIReferenceArray)
    {
        openType = TypeHandle(MscorlibBinder::GetClass(CLASS__CLRIREFERENCEARRAYIMPL));
        pMD = MscorlibBinder::GetMethod(METHOD__CLRIREFERENCEARRAYIMPL__UNBOXHELPER);
    }
    else
    {
        openType = TypeHandle(MscorlibBinder::GetClass(CLASS__CLRIREFERENCEIMPL));
        pMD = MscorlibBinder::GetMethod(METHOD__CLRIREFERENCEIMPL__UNBOXHELPER);
    }
    TypeHandle closedType = openType.Instantiate(inst);

    // Call managed helper to get the real unboxed object now
    MethodDesc* method = MethodDesc::FindOrCreateAssociatedMethodDesc(
         pMD,
         closedType.AsMethodTable(),
         FALSE,
         Instantiation(),
         FALSE);
    _ASSERTE(method != NULL);

    MethodDescCallSite unboxHelper(method);
    ARG_SLOT args[] =
    {
        ObjToArgSlot(oref),
    };

    // Call CLRIReferenceImpl::UnboxHelper(Object) or CLRIReferenceArrayImpl::UnboxHelper(Object)
    *porefResult = unboxHelper.Call_RetOBJECTREF(args);
    GCPROTECT_END();
}

// void COMInterfaceMarshaler::IKeyValuePairUnboxWorker()
//--------------------------------------------------------------------------------

// static
void COMInterfaceMarshaler::IKeyValuePairUnboxWorker(OBJECTREF oref, OBJECTREF *porefResult)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    GCPROTECT_BEGIN(oref);

    _ASSERTE(oref->GetMethodTable()->HasSameTypeDefAs(MscorlibBinder::GetClass(CLASS__CLRIKEYVALUEPAIRIMPL)));

    MethodDesc *method = MethodDesc::FindOrCreateAssociatedMethodDesc(
         MscorlibBinder::GetMethod(METHOD__CLRIKEYVALUEPAIRIMPL__UNBOXHELPER),
         oref->GetMethodTable(),
         FALSE,
         Instantiation(),
         FALSE);
    _ASSERTE(method != NULL);

    MethodDescCallSite unboxHelper(method);
    ARG_SLOT args[] =
    {
        ObjToArgSlot(oref),
    };

    // Call CLRIKeyValuePair::UnboxHelper(Object)
    *porefResult = unboxHelper.Call_RetOBJECTREF(args);
    GCPROTECT_END();
}

// OBJECTREF COMInterfaceMarshaler::IReferenceArrayUnbox()
//--------------------------------------------------------------------------------

void COMInterfaceMarshaler::IReferenceArrayUnbox(IUnknown **ppIncomingIP, OBJECTREF *poref, bool bIncomingIPAddRefed)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(m_fIReferenceArray);
        PRECONDITION(m_pThread == GetThread());
    }
    CONTRACTL_END;

    OBJECTREF unboxed = NULL;
    // Remember all reference type array method tables are shared.
    TypeHandle elementType = m_typeHandle.GetElementType();
    _ASSERTE(elementType.AsMethodTable()->IsLegalNonArrayWinRTType());

    // Create a temporary RCW.  Call into managed.  Let managed query for a closed generic instantiation
    // like IReferenceArray<Int32> (including the GUID calculation & QI) then call the Value property.  That
    // will use the existing interop code to safely marshal the value.
    // Also, make sure we create a duplicate RCW in this case so that next time we won't end up getting
    // this RCW from cache
    COMInterfaceMarshaler marshaler;

    DWORD flags = RCW::CF_DontResolveClass | RCW::CF_NeedUniqueObject;

    marshaler.Init(m_pUnknown, g_pBaseCOMObject, m_pThread, flags);

    if (m_pCallback)
        marshaler.SetCallback(m_pCallback);

    OBJECTREF oref = marshaler.FindOrCreateObjectRefInternal(ppIncomingIP, /* pIncomingItfMT = */ NULL, bIncomingIPAddRefed);

    IReferenceOrIReferenceArrayUnboxWorker(oref, elementType, TRUE, poref);
}

void COMInterfaceMarshaler::MarshalToNonRCWType(OBJECTREF *poref)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(m_fNonRCWType);
    }
    CONTRACTL_END;

    _ASSERTE(IsRedirectedToNonRCWType(m_typeHandle.GetMethodTable()));

    struct
    {
        OBJECTREF refMarshaled;
        STRINGREF refRawURI;
    }
    gc;
    ZeroMemory(&gc, sizeof(gc));

    WinMDAdapter::RedirectedTypeIndex index = static_cast<WinMDAdapter::RedirectedTypeIndex>(-1);
    WinRTTypeNameConverter::ResolveRedirectedType(m_typeHandle.GetMethodTable(), &index);
    _ASSERTE(index != -1);

    GCPROTECT_BEGIN(gc)

    switch (index)
    {
        case WinMDAdapter::RedirectedTypeIndex_System_Uri:
        {
            WinRtString hsRawUri;
            {
                GCX_PREEMP();

                SafeComHolderPreemp<ABI::Windows::Foundation::IUriRuntimeClass> pUriRuntimeClass;
                HRESULT hr = SafeQueryInterfacePreemp(m_pUnknown, ABI::Windows::Foundation::IID_IUriRuntimeClass, (IUnknown **) &pUriRuntimeClass);
                LogInteropQI(m_pUnknown, ABI::Windows::Foundation::IID_IUriRuntimeClass, hr, "IUriRuntimeClass");
                IfFailThrow(hr);

                IfFailThrow(pUriRuntimeClass->get_RawUri(hsRawUri.Address()));
            }

            UINT32 cchRawUri;
            LPCWSTR pwszRawUri = hsRawUri.GetRawBuffer(&cchRawUri);
            gc.refRawURI = StringObject::NewString(pwszRawUri, cchRawUri);

            UriMarshalingInfo *pUriMarshalingInfo = GetAppDomain()->GetLoaderAllocator()->GetMarshalingData()->GetUriMarshalingInfo();
            MethodDesc* pSystemUriCtorMD = pUriMarshalingInfo->GetSystemUriCtorMD();

            MethodTable *pMTSystemUri = pUriMarshalingInfo->GetSystemUriType().AsMethodTable();
            pMTSystemUri->EnsureInstanceActive();
            gc.refMarshaled = AllocateObject(pMTSystemUri, false);

            MethodDescCallSite uriCtor(pSystemUriCtorMD);
            ARG_SLOT ctorArgs[] =
            {
                ObjToArgSlot(gc.refMarshaled),
                ObjToArgSlot(gc.refRawURI)
            };
            uriCtor.Call(ctorArgs);
        }
        break;

        case WinMDAdapter::RedirectedTypeIndex_System_Collections_Generic_KeyValuePair:
        {
            MethodDesc *pMD = MscorlibBinder::GetMethod(METHOD__KEYVALUEPAIRMARSHALER__CONVERT_TO_MANAGED_BOX);

            pMD = MethodDesc::FindOrCreateAssociatedMethodDesc(
                pMD,
                pMD->GetMethodTable(),
                FALSE,                           // forceBoxedEntryPoint
                m_typeHandle.GetInstantiation(), // methodInst
                FALSE,                           // allowInstParam
                TRUE);                           // forceRemotableMethod

            MethodDescCallSite marshalMethod(pMD);
            ARG_SLOT methodArgs[] =
            {
                PtrToArgSlot(m_pUnknown)
            };
            gc.refMarshaled = marshalMethod.Call_RetOBJECTREF(methodArgs);
        }
        break;

        case WinMDAdapter::RedirectedTypeIndex_System_Collections_Specialized_NotifyCollectionChangedEventArgs:
        case WinMDAdapter::RedirectedTypeIndex_System_ComponentModel_PropertyChangedEventArgs:
        {
            MethodDesc *pMD;
            EventArgsMarshalingInfo *pInfo = GetAppDomain()->GetLoaderAllocator()->GetMarshalingData()->GetEventArgsMarshalingInfo();

            if (index == WinMDAdapter::RedirectedTypeIndex_System_Collections_Specialized_NotifyCollectionChangedEventArgs)
                pMD = pInfo->GetWinRTNCCEventArgsToSystemNCCEventArgsMD();
            else
                pMD = pInfo->GetWinRTPCEventArgsToSystemPCEventArgsMD();

            MethodDescCallSite marshalMethod(pMD);
            ARG_SLOT methodArgs[] =
            {
                PtrToArgSlot(m_pUnknown)
            };
            gc.refMarshaled = marshalMethod.Call_RetOBJECTREF(methodArgs);
        }
        break;

        default:
        {
            // If we get here then there is a new redirected type being introduced to the system.  You must
            // add code to marshal that type above.  Additionally, code may need to be added to GetComIPFromObjectRef,
            // in order to handle the reverse case.
            UNREACHABLE();
        }
    }

    *poref = gc.refMarshaled;

    GCPROTECT_END();
}

//--------------------------------------------------------------------------------
// OBJECTREF COMInterfaceMarshaler::FindOrCreateObjectRef()
// Find the wrapper for this COM IP, might have to create one if not found.
// It will return null for out-of memory scenarios.  It also notices if we have
// an IP that is disguised as an unmanaged object, sitting on top of a
// managed object.
//
// The ppIncomingIP parameter serves two purposes - it lets COMInterfaceMarshaler call methods on the
// interface pointer that came in from unmanaged code (pUnk could be the result of QI'ing such an IP for IUnknown),
// and it also implements the CF_SuppressAddRef flag in a reliable way by assigning NULL to *ppIncomingIP if and
// only if COMInterfaceMarshaler ended up creating a new RCW which took ownership of the interface pointer.
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
        PRECONDITION(m_pThread == GetThread());
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
            // We may get back an RCW from another STA thread (mainly in WinRT factory cache scenario,
            // as those factories are typically singleton), and we can only touch the RCW if we hold the lock,
            // otherwise we may AV if the STA thread dies and takes the RCW with it
            RCWCache::LockHolder lh(m_pWrapperCache);

            RCWHolder pRCW(m_pThread);
            m_pWrapperCache->FindWrapperInCache_NoLock(
                m_pIdentity,
                &pRCW);
            if (!pRCW.IsNull())
            {
                bool bShouldUseThisRCW = true;

                if (m_pCallback)
                    bShouldUseThisRCW = m_pCallback->ShouldUseThisRCW(pRCW.GetRawRCWUnsafe());

                if (bShouldUseThisRCW)
                {
                    oref = (OBJECTREF)pRCW->GetExposedObject();
                    if (m_pCallback)
                        m_pCallback->OnRCWCacheHit(pRCW.GetRawRCWUnsafe());
                }
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
        if (m_fIReference)
            IReferenceUnbox(ppIncomingIP, &oref, bIncomingIPAddRefed);
        else if (m_fIReferenceArray)
            IReferenceArrayUnbox(ppIncomingIP, &oref, bIncomingIPAddRefed);
        else if (m_fNonRCWType)
            MarshalToNonRCWType(&oref);
        else
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
// Helper to wrap an IUnknown with COM object
//--------------------------------------------------------------------------------
OBJECTREF COMInterfaceMarshaler::WrapWithComObject()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    OBJECTREF oref = NULL;
    GCPROTECT_BEGIN(oref)
    {
        CreateObjectRef(
            TRUE,       // fDuplicate
            &oref,      // pComObj
            NULL,       // ppIncomingIP
            NULL,       // pIncomingItfMT
            false       // bIncomingIPAddRefed
            );
    }
    GCPROTECT_END();

    return oref;
}

//--------------------------------------------------------------------------------
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

bool COMInterfaceMarshaler::SupportsIInspectable()
{
    LIMITED_METHOD_CONTRACT;
    return (m_flags & RCW::CF_SupportsIInspectable) != 0;
}

bool COMInterfaceMarshaler::DontResolveClass()
{
    LIMITED_METHOD_CONTRACT;
    return (m_flags & RCW::CF_DontResolveClass) != 0;
}

bool COMInterfaceMarshaler::NeedUniqueObject()
{
    LIMITED_METHOD_CONTRACT;
    return (m_flags & RCW::CF_NeedUniqueObject) != 0;
}
