// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: DispParamMarshaler.cpp
//

//
// Implementation of dispatch parameter marshalers.
//


#include "common.h"

#include "dispparammarshaler.h"
#include "olevariant.h"
#include "dispatchinfo.h"
#include "fieldmarshaler.h"
#include "comdelegate.h"

BOOL DispParamMarshaler::RequiresManagedCleanup()
{
    LIMITED_METHOD_CONTRACT;
    return FALSE;
}

void DispParamMarshaler::MarshalNativeToManaged(VARIANT *pSrcVar, OBJECTREF *pDestObj)
{
    WRAPPER_NO_CONTRACT;
    OleVariant::MarshalObjectForOleVariant(pSrcVar, pDestObj);
}

void DispParamMarshaler::MarshalManagedToNative(OBJECTREF *pSrcObj, VARIANT *pDestVar)
{
    WRAPPER_NO_CONTRACT;
    OleVariant::MarshalOleVariantForObject(pSrcObj, pDestVar);
}

void DispParamMarshaler::MarshalManagedToNativeRef(OBJECTREF *pSrcObj, VARIANT *pRefVar)
{
    WRAPPER_NO_CONTRACT;
    OleVariant::MarshalOleRefVariantForObject(pSrcObj, pRefVar);
}

void DispParamMarshaler::CleanUpManaged(OBJECTREF *pObj)
{
    LIMITED_METHOD_CONTRACT;
}

void DispParamCurrencyMarshaler::MarshalManagedToNative(OBJECTREF *pSrcObj, VARIANT *pDestVar)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pDestVar));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    // Convert the managed decimal to a VARIANT containing a decimal.
    OleVariant::MarshalOleVariantForObject(pSrcObj, pDestVar);
    _ASSERTE(pDestVar->vt == VT_DECIMAL);

    // Coerce the decimal to a currency.
    IfFailThrow(SafeVariantChangeType(pDestVar, pDestVar, 0, VT_CY));
}

void DispParamOleColorMarshaler::MarshalNativeToManaged(VARIANT *pSrcVar, OBJECTREF *pDestObj)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pSrcVar));
    }
    CONTRACTL_END;

    BOOL bByref = FALSE;
    VARTYPE vt = V_VT(pSrcVar);

    // Handle byref VARIANTS
    if (vt & VT_BYREF)
    {
        vt = vt & ~VT_BYREF;
        bByref = TRUE;
    }

    // Validate the OLE variant type.
    if (vt != VT_I4 && vt != VT_UI4)
        COMPlusThrow(kArgumentException, IDS_EE_INVALID_OLE_VARIANT);

    // Retrieve the OLECOLOR.
    OLE_COLOR OleColor = bByref ? *V_UI4REF(pSrcVar) : V_UI4(pSrcVar);

    // Convert the OLECOLOR to a System.Drawing.Color.
    SYSTEMCOLOR MngColor;
    ConvertOleColorToSystemColor(OleColor, &MngColor);

    // Box the System.Drawing.Color value class and give back the boxed object.
    TypeHandle hndColorType =
        GetThread()->GetDomain()->GetLoaderAllocator()->GetMarshalingData()->GetOleColorMarshalingInfo()->GetColorType();

    *pDestObj = hndColorType.GetMethodTable()->Box(&MngColor);
}

void DispParamOleColorMarshaler::MarshalManagedToNative(OBJECTREF *pSrcObj, VARIANT *pDestVar)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pDestVar));
    }
    CONTRACTL_END;

    // Clear the destination VARIANT.
    SafeVariantClear(pDestVar);

    // Convert the System.Drawing.Color to an OLECOLOR.
    V_VT(pDestVar) = VT_UI4;
    V_UI4(pDestVar) = ConvertSystemColorToOleColor(pSrcObj);
}

void DispParamOleColorMarshaler::MarshalManagedToNativeRef(OBJECTREF *pSrcObj, VARIANT *pRefVar)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pRefVar));
    }
    CONTRACTL_END;

    _ASSERTE(V_VT(pRefVar) == (VT_I4 | VT_BYREF) || V_VT(pRefVar) == (VT_UI4 | VT_BYREF));

    // Convert the System.Drawing.Color to an OLECOLOR.
    *V_UI4REF(pRefVar) = ConvertSystemColorToOleColor(pSrcObj);
}

void DispParamErrorMarshaler::MarshalManagedToNative(OBJECTREF *pSrcObj, VARIANT *pDestVar)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pDestVar));
    }
    CONTRACTL_END;

    // Convert the managed decimal to a VARIANT containing a VT_I4 or VT_UI4.
    OleVariant::MarshalOleVariantForObject(pSrcObj, pDestVar);
    _ASSERTE(V_VT(pDestVar) == VT_I4 || V_VT(pDestVar) == VT_UI4);

    // Since VariantChangeType refuses to coerce an I4 or an UI4 to a VT_ERROR, just
    // wack the variant type directly.
    V_VT(pDestVar) = VT_ERROR;
}

void DispParamInterfaceMarshaler::MarshalNativeToManaged(VARIANT *pSrcVar, OBJECTREF *pDestObj)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pSrcVar));
        PRECONDITION(IsProtectedByGCFrame(pDestObj));
    }
    CONTRACTL_END;

    BOOL bByref = FALSE;
    VARTYPE vt = V_VT(pSrcVar);

    // Handle byref VARIANTS
    if (vt & VT_BYREF)
    {
        vt = vt & ~VT_BYREF;
        bByref = TRUE;
    }

    // Validate the OLE variant type.
    if (vt != VT_UNKNOWN && vt != VT_DISPATCH)
        COMPlusThrow(kArgumentException, IDS_EE_INVALID_OLE_VARIANT);

    // Retrieve the IP.
    IUnknown *pUnk = bByref ? *V_UNKNOWNREF(pSrcVar) : V_UNKNOWN(pSrcVar);

    // Convert the IP to an OBJECTREF.
    GetObjectRefFromComIP(pDestObj, pUnk, m_pClassMT, m_bClassIsHint ? ObjFromComIP::CLASS_IS_HINT : ObjFromComIP::NONE);
}

void DispParamInterfaceMarshaler::MarshalManagedToNative(OBJECTREF *pSrcObj, VARIANT *pDestVar)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pDestVar));
    }
    CONTRACTL_END;

    SafeVariantClear(pDestVar);
    if (m_pIntfMT != NULL)
    {
        V_UNKNOWN(pDestVar) = GetComIPFromObjectRef(pSrcObj, m_pIntfMT);
    }
    else
    {
        V_UNKNOWN(pDestVar) = GetComIPFromObjectRef(pSrcObj, m_bDispatch ? ComIpType_Dispatch : ComIpType_Unknown, NULL);
    }

    V_VT(pDestVar) = static_cast<VARTYPE>(m_bDispatch ? VT_DISPATCH : VT_UNKNOWN);
}

void DispParamArrayMarshaler::MarshalNativeToManaged(VARIANT *pSrcVar, OBJECTREF *pDestObj)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pSrcVar));
        PRECONDITION(CheckPointer(pDestObj) && *pDestObj == NULL);
    }
    CONTRACTL_END;

    VARTYPE vt = m_ElementVT;
    MethodTable *pElemMT = m_pElementMT;

    // Validate the OLE variant type.
    if ((V_VT(pSrcVar) & VT_ARRAY) == 0)
        COMPlusThrow(kArgumentException, IDS_EE_INVALID_OLE_VARIANT);

    // Retrieve the SAFEARRAY pointer.
    SAFEARRAY *pSafeArray = V_VT(pSrcVar) & VT_BYREF ? *V_ARRAYREF(pSrcVar) : V_ARRAY(pSrcVar);

    if (!pSafeArray)
    {
        return;
    }

    // Retrieve the variant type if it is not specified for the parameter.
    if (vt == VT_EMPTY)
        vt = V_VT(pSrcVar) & ~VT_ARRAY | VT_BYREF;

    if (!pElemMT && vt == VT_RECORD)
        pElemMT = OleVariant::GetElementTypeForRecordSafeArray(pSafeArray).GetMethodTable();

    PCODE pStructMarshalStubAddress = NULL;
    if (vt == VT_RECORD && !pElemMT->IsBlittable())
    {
        GCX_PREEMP();
        pStructMarshalStubAddress = NDirect::GetEntryPointForStructMarshalStub(pElemMT);
    }

    // Create an array from the SAFEARRAY.
    *(BASEARRAYREF*)pDestObj = OleVariant::CreateArrayRefForSafeArray(pSafeArray, vt, pElemMT);

    // Convert the contents of the SAFEARRAY.
    OleVariant::MarshalArrayRefForSafeArray(pSafeArray, (BASEARRAYREF*)pDestObj, vt, pStructMarshalStubAddress, pElemMT);
}

void DispParamArrayMarshaler::MarshalManagedToNative(OBJECTREF *pSrcObj, VARIANT *pDestVar)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pDestVar));
    }
    CONTRACTL_END;

    SafeArrayPtrHolder pSafeArray = NULL;
    VARTYPE vt = m_ElementVT;
    MethodTable *pElemMT = m_pElementMT;

    // Clear the destination VARIANT.
    SafeVariantClear(pDestVar);

    if (*pSrcObj != NULL)
    {
        // Retrieve the VARTYPE if it is not specified for the parameter.
        if (vt == VT_EMPTY)
            vt = OleVariant::GetElementVarTypeForArrayRef(*((BASEARRAYREF*)pSrcObj));

        // Retrieve the element method table if it is not specified for the parameter.
        if (!pElemMT)
        {
            TypeHandle tempHandle = OleVariant::GetArrayElementTypeWrapperAware((BASEARRAYREF*)pSrcObj);
            pElemMT = tempHandle.GetMethodTable();
        }

        PCODE pStructMarshalStubAddress = NULL;
        GCPROTECT_BEGIN(*pSrcObj);
        if (vt == VT_RECORD && !pElemMT->IsBlittable())
        {
            GCX_PREEMP();
            pStructMarshalStubAddress = NDirect::GetEntryPointForStructMarshalStub(pElemMT);
        }
        GCPROTECT_END();

        // Allocate the safe array based on the source object and the destination VT.
        pSafeArray = OleVariant::CreateSafeArrayForArrayRef((BASEARRAYREF*)pSrcObj, vt, pElemMT);
        _ASSERTE(pSafeArray);

        // Marshal the contents of the SAFEARRAY.
        OleVariant::MarshalSafeArrayForArrayRef((BASEARRAYREF*)pSrcObj, pSafeArray, vt, pElemMT, pStructMarshalStubAddress);
    }

    // Store the resulting SAFEARRAY in the destination VARIANT.
    V_ARRAY(pDestVar) = pSafeArray;
    V_VT(pDestVar) = VT_ARRAY | vt;

    // Don't destroy the safearray.
    pSafeArray.SuppressRelease();
}

void DispParamArrayMarshaler::MarshalManagedToNativeRef(OBJECTREF *pSrcObj, VARIANT *pRefVar)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pRefVar));
    }
    CONTRACTL_END;

    VARIANT vtmp;
    VARTYPE RefVt = V_VT(pRefVar) & ~VT_BYREF;

    // Clear the contents of the original variant.
    OleVariant::ExtractContentsFromByrefVariant(pRefVar, &vtmp);
    SafeVariantClear(&vtmp);

    // Marshal the array to a temp VARIANT.
    memset(&vtmp, 0, sizeof(VARIANT));
    MarshalManagedToNative(pSrcObj, &vtmp);

    // Verify that the type of the temp VARIANT and the destination byref VARIANT
    // are the same.
    if (V_VT(&vtmp) != RefVt)
    {
        SafeVariantClear(&vtmp);
        COMPlusThrow(kInvalidCastException, IDS_EE_CANNOT_COERCE_BYREF_VARIANT);
    }

    // Copy the converted variant back into the byref variant.
    OleVariant::InsertContentsIntoByrefVariant(&vtmp, pRefVar);
}

void DispParamRecordMarshaler::MarshalNativeToManaged(VARIANT *pSrcVar, OBJECTREF *pDestObj)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pSrcVar));
    }
    CONTRACTL_END;

    GUID argGuid;
    GUID paramGuid;
    HRESULT hr = S_OK;
    VARTYPE vt = V_VT(pSrcVar);

    // Handle byref VARIANTS
    if (vt & VT_BYREF)
        vt = vt & ~VT_BYREF;

    // Validate the OLE variant type.
    if (vt != VT_RECORD)
        COMPlusThrow(kArgumentException, IDS_EE_INVALID_OLE_VARIANT);

    // Make sure an IRecordInfo is specified.
    IRecordInfo *pRecInfo = pSrcVar->pRecInfo;
    if (!pRecInfo)
        COMPlusThrow(kArgumentException, IDS_EE_INVALID_OLE_VARIANT);

    // Make sure the GUID of the IRecordInfo matches the guid of the
    // parameter type.
    {
        GCX_PREEMP();
        IfFailThrow(pRecInfo->GetGuid(&argGuid));
    }
    if (argGuid != GUID_NULL)
    {
        m_pRecordMT->GetGuid(&paramGuid, TRUE);
        if (paramGuid != argGuid)
            COMPlusThrow(kArgumentException, IDS_EE_INVALID_OLE_VARIANT);
    }

    OBJECTREF BoxedValueClass = NULL;
    GCPROTECT_BEGIN(BoxedValueClass)
    {
        LPVOID pvRecord = pSrcVar->pvRecord;
        if (pvRecord)
        {
            // Allocate an instance of the boxed value class and copy the contents
            // of the record into it.
            BoxedValueClass = m_pRecordMT->Allocate();

            MethodDesc* pStructMarshalStub;
            {
                GCX_PREEMP();

                pStructMarshalStub = NDirect::CreateStructMarshalILStub(m_pRecordMT);
            }

            MarshalStructViaILStub(pStructMarshalStub, BoxedValueClass->GetData(), pvRecord, StructMarshalStubs::MarshalOperation::Unmarshal);
        }

        *pDestObj = BoxedValueClass;
    }
    GCPROTECT_END();
}

void DispParamRecordMarshaler::MarshalManagedToNative(OBJECTREF *pSrcObj, VARIANT *pDestVar)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pDestVar));
    }
    CONTRACTL_END;

    // Clear the destination VARIANT.
    SafeVariantClear(pDestVar);

    // Convert the value class to a VT_RECORD.
    OleVariant::ConvertValueClassToVariant(pSrcObj, pDestVar);

    // Set the VT in the VARIANT.
    V_VT(pDestVar) = VT_RECORD;
}

void DispParamDelegateMarshaler::MarshalNativeToManaged(VARIANT *pSrcVar, OBJECTREF *pDestObj)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pSrcVar));
    }
    CONTRACTL_END;

    void *pDelegate = NULL;

    switch(V_VT(pSrcVar))
    {
#ifdef HOST_64BIT
        case VT_I8:
            pDelegate = reinterpret_cast<void*>(static_cast<INT_PTR>(V_I8(pSrcVar)));
            break;

        case VT_UI8:
            pDelegate = reinterpret_cast<void*>(static_cast<UINT_PTR>(V_UI8(pSrcVar)));
            break;
#else
        case VT_I4:
            pDelegate = reinterpret_cast<void*>(static_cast<INT_PTR>(V_I4(pSrcVar)));
            break;

        case VT_UI4:
            pDelegate = reinterpret_cast<void*>(static_cast<UINT_PTR>(V_UI4(pSrcVar)));
            break;
#endif
        default :
            COMPlusThrow(kArgumentException, IDS_EE_INVALID_OLE_VARIANT);

    }

    if (pDelegate == NULL)
        SetObjectReference(pDestObj, NULL);
    else
        SetObjectReference(pDestObj, COMDelegate::ConvertToDelegate(pDelegate, m_pDelegateMT));
}

void DispParamDelegateMarshaler::MarshalManagedToNative(OBJECTREF *pSrcObj, VARIANT *pDestVar)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pDestVar));
    }
    CONTRACTL_END;

    // Clear the destination VARIANT.
    SafeVariantClear(pDestVar);

    // Convert to VARIANT
#ifdef HOST_64BIT
    V_VT(pDestVar) = VT_I8;
#else
    V_VT(pDestVar) = VT_I4;
#endif

    // ConvertToCallback automatically takes care of the pSrcObj == NULL case
    void *pDelegate = (void*) COMDelegate::ConvertToCallback(*pSrcObj);

#ifdef HOST_64BIT
    V_I8(pDestVar) = static_cast<INT64>(reinterpret_cast<INT_PTR>(pDelegate));
#else
    V_I4(pDestVar) = static_cast<INT32>(reinterpret_cast<INT_PTR>(pDelegate));
#endif
}

BOOL DispParamCustomMarshaler::RequiresManagedCleanup()
{
    LIMITED_METHOD_CONTRACT;
    return TRUE;
}

void DispParamCustomMarshaler::MarshalNativeToManaged(VARIANT *pSrcVar, OBJECTREF *pDestObj)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pSrcVar));
    }
    CONTRACTL_END;

    BOOL bByref = FALSE;
    VARTYPE vt = V_VT(pSrcVar);

    // Handle byref VARIANTS
    if (vt & VT_BYREF)
    {
        vt = vt & ~VT_BYREF;
        bByref = TRUE;
    }

    // Make sure the source VARIANT is of a valid type.
    if (vt != VT_I4 && vt != VT_UI4 && vt != VT_UNKNOWN && vt != VT_DISPATCH)
        COMPlusThrow(kInvalidCastException, IDS_EE_INVALID_VT_FOR_CUSTOM_MARHALER);

    // Retrieve the IUnknown pointer.
    IUnknown *pUnk = bByref ? *V_UNKNOWNREF(pSrcVar) : V_UNKNOWN(pSrcVar);

    // Marshal the contents of the VARIANT using the custom marshaler.
    *pDestObj = m_pCMHelper->InvokeMarshalNativeToManagedMeth(pUnk);
}

void DispParamCustomMarshaler::MarshalManagedToNative(OBJECTREF *pSrcObj, VARIANT *pDestVar)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pDestVar));
    }
    CONTRACTL_END;

    SafeComHolder<IUnknown> pUnk = NULL;
    SafeComHolder<IDispatch> pDisp = NULL;

    // Convert the object using the custom marshaler.
    SafeVariantClear(pDestVar);

    // Invoke the MarshalManagedToNative method.
    pUnk = (IUnknown*)m_pCMHelper->InvokeMarshalManagedToNativeMeth(*pSrcObj);
    if (!pUnk)
    {
        // Put a null IDispath pointer in the VARIANT.
        V_VT(pDestVar) = VT_DISPATCH;
        V_DISPATCH(pDestVar) = NULL;
    }
    else
    {
        // QI the object for IDispatch.
        HRESULT hr = SafeQueryInterface(pUnk, IID_IDispatch, (IUnknown **)&pDisp);
        LogInteropQI(pUnk, IID_IDispatch, hr, "DispParamCustomMarshaler::MarshalManagedToNative");
        if (SUCCEEDED(hr))
        {
            // Release the IUnknown pointer since we will put the IDispatch pointer in
            // the VARIANT.
            ULONG cbRef = SafeRelease(pUnk);
            pUnk.SuppressRelease();
            LogInteropRelease(pUnk, cbRef, "Release IUnknown");

            // Put the IDispatch pointer into the VARIANT.
            V_VT(pDestVar) = VT_DISPATCH;
            V_DISPATCH(pDestVar) = pDisp;
            pDisp.SuppressRelease();
        }
        else
        {
            // Put the IUnknown pointer into the VARIANT.
            V_VT(pDestVar) = VT_UNKNOWN;
            V_UNKNOWN(pDestVar) = pUnk;
            pUnk.SuppressRelease();
        }
    }
}

void DispParamCustomMarshaler::MarshalManagedToNativeRef(OBJECTREF *pSrcObj, VARIANT *pRefVar)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pRefVar));
    }
    CONTRACTL_END;

    VARTYPE RefVt = V_VT(pRefVar) & ~VT_BYREF;
    VARIANT vtmp;

    // Clear the contents of the original variant.
    OleVariant::ExtractContentsFromByrefVariant(pRefVar, &vtmp);
    SafeVariantClear(&vtmp);

    // Convert the object using the custom marshaler.
    V_UNKNOWN(&vtmp) = (IUnknown*)m_pCMHelper->InvokeMarshalManagedToNativeMeth(*pSrcObj);
    V_VT(&vtmp) = m_vt;

    // Call VariantChangeType if required.
    if (V_VT(&vtmp) != RefVt)
    {
        HRESULT hr = SafeVariantChangeType(&vtmp, &vtmp, 0, RefVt);
        if (FAILED(hr))
        {
            SafeVariantClear(&vtmp);
            if (hr == DISP_E_TYPEMISMATCH)
                COMPlusThrow(kInvalidCastException, IDS_EE_CANNOT_COERCE_BYREF_VARIANT);
            else
                COMPlusThrowHR(hr);
        }
    }

    // Copy the converted variant back into the byref variant.
    OleVariant::InsertContentsIntoByrefVariant(&vtmp, pRefVar);
}

void DispParamCustomMarshaler::CleanUpManaged(OBJECTREF *pObj)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
    m_pCMHelper->InvokeCleanUpManagedMeth(*pObj);
}
