// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "common.h"
#include "vars.hpp"
#include "excep.h"
#include "interoputil.h"
#include "interopconverter.h"
#include "olevariant.h"
#include "comcallablewrapper.h"

#ifdef FEATURE_COMINTEROP

#include "stdinterfaces.h"
#include "runtimecallablewrapper.h"
#include "cominterfacemarshaler.h"
#include "binder.h"
#include "winrttypenameconverter.h"
#include "typestring.h"

//--------------------------------------------------------------------------------
// IUnknown *GetComIPFromObjectRef(OBJECTREF *poref, MethodTable *pMT, ...)
// Convert ObjectRef to a COM IP, based on MethodTable* pMT.
IUnknown *GetComIPFromObjectRef(OBJECTREF *poref, MethodTable *pMT, BOOL bSecurityCheck, BOOL bEnableCustomizedQueryInterface)
{
    CONTRACT (IUnknown*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(poref));
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(g_fComStarted && "COM has not been started up, make sure EnsureComStarted is called before any COM objects are used!");
        POSTCONDITION((*poref) != NULL ? CheckPointer(RETVAL) : CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    BOOL        fReleaseWrapper     = false;
    HRESULT     hr                  = E_NOINTERFACE;
    SafeComHolder<IUnknown> pUnk    = NULL;
    size_t      ul                  = 0;

    if (*poref == NULL)
        RETURN NULL;

    
    SyncBlock* pBlock = (*poref)->GetSyncBlock();
    
    InteropSyncBlockInfo* pInteropInfo = pBlock->GetInteropInfo();

    // If we have a CCW, or a NULL CCW but the RCW field was never used,
    //  get the pUnk from the ComCallWrapper, otherwise from the RCW
    if ((NULL != pInteropInfo->GetCCW()) || (!pInteropInfo->RCWWasUsed()))
    {
        CCWHolder pCCWHold = ComCallWrapper::InlineGetWrapper(poref);

        GetComIPFromCCW::flags flags = GetComIPFromCCW::None;
        if (!bSecurityCheck)                    { flags |= GetComIPFromCCW::SuppressSecurityCheck; }
        if (!bEnableCustomizedQueryInterface)   { flags |= GetComIPFromCCW::SuppressCustomizedQueryInterface; }

        pUnk = ComCallWrapper::GetComIPFromCCW(pCCWHold, GUID_NULL, pMT, flags);
    }
    else
    {
        RCWHolder pRCW(GetThread());
        RCWPROTECT_BEGIN(pRCW, pBlock);
        
        // The interface will be returned addref'ed.
        pUnk = pRCW->GetComIPFromRCW(pMT);

        RCWPROTECT_END(pRCW);
    }

    // If we failed to retrieve an IP then throw an exception.
    if (pUnk == NULL)
        COMPlusThrowHR(hr);

    pUnk.SuppressRelease();
    RETURN pUnk;
}


//--------------------------------------------------------------------------------
// IUnknown *GetComIPFromObjectRef(OBJECTREF *poref, ComIpType ReqIpType, ComIpType *pFetchedIpType);
// Convert ObjectRef to a COM IP of the requested type.
IUnknown *GetComIPFromObjectRef(OBJECTREF *poref, ComIpType ReqIpType, ComIpType *pFetchedIpType)
{
    CONTRACT (IUnknown*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION((ReqIpType & (ComIpType_Dispatch | ComIpType_Unknown | ComIpType_Inspectable)) != 0);
        PRECONDITION(CheckPointer(poref));
        PRECONDITION(ReqIpType != 0);
        POSTCONDITION((*poref) != NULL ? CheckPointer(RETVAL) : CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    // COM had better be started up at this point.
    _ASSERTE(g_fComStarted && "COM has not been started up, make sure EnsureComStarted is called before any COM objects are used!");

    BOOL        fReleaseWrapper = false;
    HRESULT     hr              = E_NOINTERFACE;
    IUnknown*   pUnk            = NULL;
    size_t      ul              = 0;
    ComIpType   FetchedIpType   = ComIpType_None;

    if (*poref == NULL)
        RETURN NULL;

    MethodTable *pMT = (*poref)->GetMethodTable();

    SyncBlock* pBlock = (*poref)->GetSyncBlock();

    InteropSyncBlockInfo* pInteropInfo = pBlock->GetInteropInfo();
    
    if ( (NULL != pInteropInfo->GetCCW()) || (!pInteropInfo->RCWWasUsed()) )
    {
        CCWHolder pCCWHold = ComCallWrapper::InlineGetWrapper(poref);
        
        // If the user requested IDispatch, then check for IDispatch first.
        if (ReqIpType & ComIpType_Dispatch)
        {
            pUnk = ComCallWrapper::GetComIPFromCCW(pCCWHold, IID_IDispatch, NULL);
            if (pUnk)
                FetchedIpType = ComIpType_Dispatch;
        }
        
        if (ReqIpType & ComIpType_Inspectable)
        {
            WinMDAdapter::RedirectedTypeIndex redirectedTypeIndex;

            MethodTable * pMT = (*poref)->GetMethodTable();

            //
            // Check whether this object is of a legal WinRT type (including array)
            //
            // Note that System.RuntimeType is a weird case - we only redirect System.Type at type
            // level, but when we boxing the actual instance, we expect it to be a System.RuntimeType 
            // instance, which is not redirected and not a legal WinRT type
            // 
            // Therefore, special case for System.RuntimeType and treat it as a legal WinRT type
            // only for boxing
            //
            if (pMT->IsLegalWinRTType(poref) || 
                MscorlibBinder::IsClass(pMT, CLASS__CLASS))
            {
                // The managed signature contains Object, and native signature is IInspectable.
                // "Box" value types by allocating an IReference<T> and storing them inside it.
                // Similarly, String must be an IReference<HSTRING>.  Delegates get wrapped too.
                // Arrays must be stored in an IReferenceArray<T>.
                // System.Type is in fact internal type System.RuntimeType (CLASS__CLASS) that inherits from it.
                //   Note: We do not allow System.ReflectionOnlyType that inherits from System.RuntimeType.
                // KeyValuePair`2 must be exposed as CLRIKeyValuePair.
                if (pMT->HasInstantiation() && pMT->HasSameTypeDefAs(MscorlibBinder::GetClass(CLASS__KEYVALUEPAIRGENERIC)))
                {
                    TypeHandle th = TypeHandle(MscorlibBinder::GetClass(CLASS__CLRIKEYVALUEPAIRIMPL)).Instantiate(pMT->GetInstantiation());

                    MethodDesc *method = MethodDesc::FindOrCreateAssociatedMethodDesc(
                         MscorlibBinder::GetMethod(METHOD__CLRIKEYVALUEPAIRIMPL__BOXHELPER),
                         th.GetMethodTable(),
                         FALSE,
                         Instantiation(),
                         FALSE);
                    _ASSERTE(method != NULL);

                    MethodDescCallSite boxHelper(method);

                    ARG_SLOT Args[] =
                    { 
                        ObjToArgSlot(*poref),
                    };
                    OBJECTREF orCLRKeyValuePair = boxHelper.Call_RetOBJECTREF(Args);

                    GCPROTECT_BEGIN(orCLRKeyValuePair);
                    CCWHolder pCCWHoldBoxed = ComCallWrapper::InlineGetWrapper(&orCLRKeyValuePair);
                    pUnk = ComCallWrapper::GetComIPFromCCW(pCCWHoldBoxed, IID_IInspectable, NULL);
                    GCPROTECT_END();
                }
                else if ((pMT->IsValueType() || 
                     pMT->IsStringOrArray() ||
                     pMT->IsDelegate() || 
                     MscorlibBinder::IsClass(pMT, CLASS__CLASS)))
                {
                    OBJECTREF orBoxedIReference = NULL;
                    MethodDescCallSite createIReference(METHOD__FACTORYFORIREFERENCE__CREATE_IREFERENCE);

                    ARG_SLOT Args[] =
                    { 
                        ObjToArgSlot(*poref),
                    };

                    // Call FactoryForIReference::CreateIReference(Object) for an IReference<T> or IReferenceArray<T>.
                    orBoxedIReference = createIReference.Call_RetOBJECTREF(Args);

                    GCPROTECT_BEGIN(orBoxedIReference);
                    CCWHolder pCCWHoldBoxed = ComCallWrapper::InlineGetWrapper(&orBoxedIReference);
                    pUnk = ComCallWrapper::GetComIPFromCCW(pCCWHoldBoxed, IID_IInspectable, NULL);
                    GCPROTECT_END();
                }
                else if (WinRTTypeNameConverter::ResolveRedirectedType(pMT, &redirectedTypeIndex))
                {
                    // This is a redirected type - see if we need to manually marshal it                    
                    if (redirectedTypeIndex == WinMDAdapter::RedirectedTypeIndex_System_Uri)
                    {
                        UriMarshalingInfo *pUriMarshalInfo = GetAppDomain()->GetLoaderAllocator()->GetMarshalingData()->GetUriMarshalingInfo();
                        struct
                        {
                            OBJECTREF ref;
                            STRINGREF refRawUri;
                        }
                        gc;
                        ZeroMemory(&gc, sizeof(gc));
                        GCPROTECT_BEGIN(gc);

                        gc.ref = *poref;

                        MethodDescCallSite getRawURI(pUriMarshalInfo->GetSystemUriOriginalStringMD());
                        ARG_SLOT getRawURIArgs[] =
                        {
                            ObjToArgSlot(gc.ref)
                        };

                        gc.refRawUri = (STRINGREF)getRawURI.Call_RetOBJECTREF(getRawURIArgs);

                        DWORD cchRawUri = gc.refRawUri->GetStringLength();
                        LPCWSTR wszRawUri = gc.refRawUri->GetBuffer();

                        {
                            GCX_PREEMP();                
                            pUnk = CreateWinRTUri(wszRawUri, static_cast<INT32>(cchRawUri));
                        }

                        GCPROTECT_END();
                    }
                    else if (redirectedTypeIndex == WinMDAdapter::RedirectedTypeIndex_System_Collections_Specialized_NotifyCollectionChangedEventArgs ||
                             redirectedTypeIndex == WinMDAdapter::RedirectedTypeIndex_System_ComponentModel_PropertyChangedEventArgs)
                    {
                        MethodDesc *pMD;
                        EventArgsMarshalingInfo *pInfo = GetAppDomain()->GetLoaderAllocator()->GetMarshalingData()->GetEventArgsMarshalingInfo();

                        if (redirectedTypeIndex == WinMDAdapter::RedirectedTypeIndex_System_Collections_Specialized_NotifyCollectionChangedEventArgs)
                            pMD = pInfo->GetSystemNCCEventArgsToWinRTNCCEventArgsMD();
                        else
                            pMD = pInfo->GetSystemPCEventArgsToWinRTPCEventArgsMD();

                        MethodDescCallSite marshalMethod(pMD);
                        ARG_SLOT methodArgs[] =
                        {
                            ObjToArgSlot(*poref)
                        };
                        pUnk = (IUnknown *)marshalMethod.Call_RetLPVOID(methodArgs);
                    }
                    else
                    {
                        _ASSERTE(!W("Unexpected redirected type seen in GetComIPFromObjectRef"));
                    }
                }
                else
                {
                    //
                    // WinRT reference type - marshal as IInspectable
                    //
                    pUnk = ComCallWrapper::GetComIPFromCCW(pCCWHold, IID_IInspectable, /* pIntfMT = */ NULL);
                }
            }
            else
            {
                //
                // Marshal non-WinRT types as IInspectable* to enable round-tripping (for example, TextBox.Tag property)                
                // By default, this returns ICustomPropertyProvider;
                //
                pUnk = ComCallWrapper::GetComIPFromCCW(pCCWHold, IID_IInspectable, /* pIntfMT = */ NULL);
            }
            
            if (pUnk)
                FetchedIpType = ComIpType_Inspectable;
        }

        // If the ObjectRef doesn't support IDispatch and the caller also accepts
        // an IUnknown pointer, then check for IUnknown.
        if (!pUnk && (ReqIpType & ComIpType_Unknown))
        {
            if (ReqIpType & ComIpType_OuterUnknown)
            {
                // check if the object is aggregated
                SimpleComCallWrapper* pSimpleWrap = pCCWHold->GetSimpleWrapper();
                if (pSimpleWrap)
                {
                    pUnk = pSimpleWrap->GetOuter();
                    if (pUnk)
                        SafeAddRef(pUnk);
                }
            }
            if (!pUnk)
                pUnk = ComCallWrapper::GetComIPFromCCW(pCCWHold, IID_IUnknown, NULL);
            if (pUnk)
                FetchedIpType = ComIpType_Unknown;
        }
    }
    else
    {
        RCWHolder pRCW(GetThread());

        // This code is hot, use a simple RCWHolder check (i.e. don't increment the use count on the RCW).
        // @TODO: Cache IInspectable & IDispatch so we don't have to QI every time we come here.
        pRCW.InitFastCheck(pBlock);

        // If the user requested IDispatch, then check for IDispatch first.
        if (ReqIpType & ComIpType_Dispatch)
        {
            pUnk = pRCW->GetIDispatch();
            if (pUnk)
                FetchedIpType = ComIpType_Dispatch;
        }
        
        if (ReqIpType & ComIpType_Inspectable)
        {
            pUnk = pRCW->GetIInspectable();
            if (pUnk)
                FetchedIpType = ComIpType_Inspectable;
        }

        // If the ObjectRef doesn't support IDispatch and the caller also accepts
        // an IUnknown pointer, then check for IUnknown.
        if (!pUnk && (ReqIpType & ComIpType_Unknown))
        {
            pUnk = pRCW->GetIUnknown();
            if (pUnk)
                FetchedIpType = ComIpType_Unknown;
        }
    }
    
    // If we failed to retrieve an IP then throw an exception.
    if (pUnk == NULL)
        COMPlusThrowHR(hr);

    // If the caller wants to know the fetched IP type, then set pFetchedIpType
    // to the type of the IP.
    if (pFetchedIpType)
        *pFetchedIpType = FetchedIpType;

    RETURN pUnk;
}


//+----------------------------------------------------------------------------
// IUnknown *GetComIPFromObjectRef(OBJECTREF *poref, REFIID iid);
// convert ComIP to an ObjectRef, based on riid
//+----------------------------------------------------------------------------
IUnknown *GetComIPFromObjectRef(OBJECTREF *poref, REFIID iid, bool throwIfNoComIP /* = true */)
{
    CONTRACT (IUnknown*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(poref));
        POSTCONDITION((*poref) != NULL ? CheckPointer(RETVAL, throwIfNoComIP ? NULL_NOT_OK : NULL_OK) : CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;
    
    ASSERT_PROTECTED(poref);

    // COM had better be started up at this point.
    _ASSERTE(g_fComStarted && "COM has not been started up, make sure EnsureComStarted is called before any COM objects are used!");

    BOOL        fReleaseWrapper = false;
    HRESULT     hr              = E_NOINTERFACE;
    IUnknown*   pUnk            = NULL;
    size_t      ul              = 0;
    
    if (*poref == NULL)
        RETURN NULL;

    MethodTable *pMT = (*poref)->GetMethodTable();

    SyncBlock* pBlock = (*poref)->GetSyncBlock();

    InteropSyncBlockInfo* pInteropInfo = pBlock->GetInteropInfo();

    if ((NULL != pInteropInfo->GetCCW()) || (!pInteropInfo->RCWWasUsed()))
    {
        CCWHolder pCCWHold = ComCallWrapper::InlineGetWrapper(poref);
        pUnk = ComCallWrapper::GetComIPFromCCW(pCCWHold, iid, NULL);
    }
    else
    {
        SafeComHolder<IUnknown> pUnkHolder;

        RCWHolder pRCW(GetThread());
        RCWPROTECT_BEGIN(pRCW, pBlock);
                
        // The interface will be returned addref'ed.
        pUnkHolder = pRCW->GetComIPFromRCW(iid);

        RCWPROTECT_END(pRCW);

        pUnk = pUnkHolder.Extract();
    }
    
    if (throwIfNoComIP && pUnk == NULL)
        COMPlusThrowHR(hr);

    RETURN pUnk;
}


//+----------------------------------------------------------------------------
// GetObjectRefFromComIP
// pUnk : input IUnknown
// pMTClass : specifies the type of instance to be returned
// NOTE:**  As per COM Rules, the IUnknown passed in shouldn't be AddRef'ed
//+----------------------------------------------------------------------------
void GetObjectRefFromComIP(OBJECTREF* pObjOut, IUnknown **ppUnk, MethodTable *pMTClass, MethodTable *pItfMT, DWORD dwFlags)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(ppUnk));
        PRECONDITION(CheckPointer(*ppUnk, NULL_OK));
        PRECONDITION(CheckPointer(pMTClass, NULL_OK));
        PRECONDITION(IsProtectedByGCFrame(pObjOut));
        PRECONDITION(pItfMT == NULL || pItfMT->IsInterface());
    }
    CONTRACTL_END;

    // COM had better be started up at this point.
    _ASSERTE(g_fComStarted && "COM has not been started up, make sure EnsureComStarted is called before any COM objects are used!");

    IUnknown *pUnk = *ppUnk;
    Thread * pThread = GetThread();

    *pObjOut = NULL;
    IUnknown* pOuter = pUnk;
    SafeComHolder<IUnknown> pAutoOuterUnk = NULL;
    
    if (pUnk != NULL)
    {
        // get CCW for IUnknown
        ComCallWrapper *ccw = GetCCWFromIUnknown(pUnk);
        if (ccw == NULL)
        {
            // could be aggregated scenario
            HRESULT hr = SafeQueryInterface(pUnk, IID_IUnknown, &pOuter);
            LogInteropQI(pUnk, IID_IUnknown, hr, "GetObjectRefFromComIP: QI for Outer");
            IfFailThrow(hr);

            // store the outer in the auto pointer
            pAutoOuterUnk = pOuter; 
            ccw = GetCCWFromIUnknown(pOuter);
        }

        // If the CCW was activated via COM, do not unwrap it.
        // Unwrapping a CCW would deliver the underlying OBJECTREF,
        // but when a managed class is activated via COM it should
        // remain a COM object and adhere to COM rules.
        if (ccw != NULL
            && !ccw->IsComActivated())
        {
            *pObjOut = ccw->GetObjectRef();
        }

        if (*pObjOut != NULL)
        {
            if (!(dwFlags & ObjFromComIP::IGNORE_WINRT_AND_SKIP_UNBOXING))
            {
                // Unbox objects from a CLRIReferenceImpl<T> or CLRIReferenceArrayImpl<T>.  
                MethodTable *pMT = (*pObjOut)->GetMethodTable();
                if (pMT->HasInstantiation())
                {
                    DWORD nGenericArgs = pMT->GetNumGenericArgs();
                    if (nGenericArgs == 1)
                    {
                        // See if this type C<SomeType> is a G<T>.
                        if (pMT->HasSameTypeDefAs(MscorlibBinder::GetClass(CLASS__CLRIREFERENCEIMPL)))
                        {
                            TypeHandle thType = pMT->GetInstantiation()[0];
                            COMInterfaceMarshaler::IReferenceOrIReferenceArrayUnboxWorker(*pObjOut, thType, FALSE, pObjOut);
                        }
                        else if (pMT->HasSameTypeDefAs(MscorlibBinder::GetClass(CLASS__CLRIREFERENCEARRAYIMPL)))
                        {
                            TypeHandle thArrayElementType = pMT->GetInstantiation()[0];
                            COMInterfaceMarshaler::IReferenceOrIReferenceArrayUnboxWorker(*pObjOut, thArrayElementType, TRUE, pObjOut);
                        }
                    }
                    else if ((nGenericArgs == 2) && pMT->HasSameTypeDefAs(MscorlibBinder::GetClass(CLASS__CLRIKEYVALUEPAIRIMPL)))
                    {
                        // Unbox IKeyValuePair from CLRIKeyValuePairImpl
                        COMInterfaceMarshaler::IKeyValuePairUnboxWorker(*pObjOut, pObjOut);
                    }
                }
            }
        }
        else
        {
            // Only pass in the class method table to the interface marshaler if 
            // it is a COM import (or COM import derived) class or a WinRT delegate.
            MethodTable *pComClassMT = NULL;
            if (pMTClass)
            {
                if (pMTClass->IsComObjectType() ||
                    (pMTClass->IsDelegate() && (pMTClass->IsProjectedFromWinRT() || WinRTTypeNameConverter::IsRedirectedType(pMTClass))))
                {
                    pComClassMT = pMTClass;
                }
            }

            DWORD flags = RCW::CreationFlagsFromObjForComIPFlags((ObjFromComIP::flags)dwFlags);

            // Convert the IP to an OBJECTREF.
            COMInterfaceMarshaler marshaler;

            marshaler.Init(pOuter, pComClassMT, pThread, flags);

            if (flags & ObjFromComIP::SUPPRESS_ADDREF)
            {
                // We can swallow the reference in ppUnk
                // This only happens in WinRT
                *pObjOut = marshaler.FindOrCreateObjectRef(ppUnk, pItfMT);
            }
            else
            {
                *pObjOut = marshaler.FindOrCreateObjectRef(pUnk, pItfMT);
            }
        }
    }


    if ((0 == (dwFlags & ObjFromComIP::CLASS_IS_HINT)) && (*pObjOut != NULL))
    {
        // make sure we can cast to the specified class
        if (pMTClass != NULL)
        {
            FAULT_NOT_FATAL();

            // Bad format exception thrown for backward compatibility
            THROW_BAD_FORMAT_MAYBE(pMTClass->IsArray() == FALSE, BFA_UNEXPECTED_ARRAY_TYPE, pMTClass);

            if (!CanCastComObject(*pObjOut, pMTClass))
            {
                StackSString ssObjClsName;
                StackSString ssDestClsName;

                (*pObjOut)->GetMethodTable()->_GetFullyQualifiedNameForClass(ssObjClsName);
                pMTClass->_GetFullyQualifiedNameForClass(ssDestClsName);

                COMPlusThrow(kInvalidCastException, IDS_EE_CANNOTCAST,
                                ssObjClsName.GetUnicode(), ssDestClsName.GetUnicode());
            }
        }
        else if (dwFlags & ObjFromComIP::REQUIRE_IINSPECTABLE)
        {
            MethodTable *pMT = (*pObjOut)->GetMethodTable();
            if (pMT->IsDelegate() && pMT->IsProjectedFromWinRT())
            {
                // This is a WinRT delegate - WinRT delegate doesn't implement IInspectable but we allow unboxing a WinRT delegate
                // from a IReference<T>
            }
            else
            {
                // Just call GetComIPFromObjectRef. We could be more efficient here but the code would get complicated
                // which doesn't seem to be worth it. The function throws an exception if the QI/cast fails.
                SafeComHolder<IUnknown> pInsp = GetComIPFromObjectRef(pObjOut, ComIpType_Inspectable, NULL);
                _ASSERTE(pInsp != NULL);
            }
        }
    }
}
#endif // FEATURE_COMINTEROP


