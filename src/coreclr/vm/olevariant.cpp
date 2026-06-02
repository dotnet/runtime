// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: OleVariant.cpp
//

//


#include "common.h"

#include "object.h"
#include "excep.h"
#include "frames.h"
#include "vars.hpp"
#include "olevariant.h"
#include "comdatetime.h"
#include "fieldmarshaler.h"
#include "dllimport.h"

/* ------------------------------------------------------------------------- *
 * Local constants
 * ------------------------------------------------------------------------- */


//
// GetElementVarTypeForArrayRef returns the safearray variant type for the
// underlying elements in the array.
//

VARTYPE OleVariant::GetElementVarTypeForArrayRef(BASEARRAYREF pArrayRef)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    TypeHandle elemTypeHnd = pArrayRef->GetArrayElementTypeHandle();
    return(::GetVarTypeForTypeHandle(elemTypeHnd));
}

BOOL OleVariant::IsValidArrayForSafeArrayElementType(BASEARRAYREF *pArrayRef, VARTYPE vtExpected)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // Retrieve the VARTYPE for the managed array.
    VARTYPE vtActual = GetElementVarTypeForArrayRef(*pArrayRef);

    // If the actual type is the same as the expected type, then the array is valid.
    if (vtActual == vtExpected)
        return TRUE;

    // Check for additional supported VARTYPES.
    switch (vtExpected)
    {
        case VT_I4:
            return vtActual == VT_INT;

        case VT_INT:
            return vtActual == VT_I4;

        case VT_UI4:
            return vtActual == VT_UINT;

        case VT_UINT:
            return vtActual == VT_UI4;

        case VT_UNKNOWN:
            return vtActual == VT_VARIANT || vtActual == VT_DISPATCH;

        case VT_DISPATCH:
            return vtActual == VT_VARIANT;

        case VT_CY:
            return vtActual == VT_DECIMAL;

        case VT_LPSTR:
        case VT_LPWSTR:
            return vtActual == VT_BSTR;

        default:
            return FALSE;
    }
}

//
// GetArrayClassForVarType returns the element class name and underlying method table
// to use to represent an array with the given variant type.
//

TypeHandle OleVariant::GetArrayForVarType(VARTYPE vt, TypeHandle elemType, unsigned rank)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    CorElementType baseElement = ELEMENT_TYPE_END;
    TypeHandle baseType;

    if (!elemType.IsNull() && elemType.IsEnum())
    {
        baseType = elemType;
    }
    else
    {
        switch (vt)
        {
        case VT_BOOL:
            baseElement = ELEMENT_TYPE_BOOLEAN;
            break;

        case VT_UI1:
            baseElement = ELEMENT_TYPE_U1;
            break;

        case VT_I1:
            baseElement = ELEMENT_TYPE_I1;
            break;

        case VT_UI2:
            baseElement = ELEMENT_TYPE_U2;
            break;

        case VT_I2:
            baseElement = ELEMENT_TYPE_I2;
            break;

        case VT_UI4:
        case VT_UINT:
        case VT_ERROR:
            if (vt == VT_UI4)
            {
                if (elemType.IsNull() || elemType == TypeHandle(g_pObjectClass))
                {
                baseElement = ELEMENT_TYPE_U4;
                }
                else
                {
                    switch (elemType.AsMethodTable()->GetInternalCorElementType())
                    {
                        case ELEMENT_TYPE_U4:
                            baseElement = ELEMENT_TYPE_U4;
                            break;
                        case ELEMENT_TYPE_U:
                            baseElement = ELEMENT_TYPE_U;
                            break;
                        default:
                            _ASSERTE(0);
                    }
                }
            }
            else
            {
                baseElement = ELEMENT_TYPE_U4;
            }
            break;

        case VT_I4:
        case VT_INT:
            if (vt == VT_I4)
            {
                if (elemType.IsNull() || elemType == TypeHandle(g_pObjectClass))
                {
                    baseElement = ELEMENT_TYPE_I4;
                }
                else
                {
                    switch (elemType.AsMethodTable()->GetInternalCorElementType())
                    {
                        case ELEMENT_TYPE_I4:
                            baseElement = ELEMENT_TYPE_I4;
                            break;
                        case ELEMENT_TYPE_I:
                            baseElement = ELEMENT_TYPE_I;
                            break;
                        default:
                            _ASSERTE(0);
                    }
                }
            }
            else
            {
                baseElement = ELEMENT_TYPE_I4;
            }
            break;

        case VT_I8:
            if (elemType.IsNull() || elemType == TypeHandle(g_pObjectClass))
            {
                baseElement = ELEMENT_TYPE_I8;
            }
            else
            {
                switch (elemType.AsMethodTable()->GetInternalCorElementType())
                {
                    case ELEMENT_TYPE_I8:
                        baseElement = ELEMENT_TYPE_I8;
                        break;
                    case ELEMENT_TYPE_I:
                        baseElement = ELEMENT_TYPE_I;
                        break;
                    default:
                        _ASSERTE(0);
                }
            }
            break;

        case VT_UI8:
            if (elemType.IsNull() || elemType == TypeHandle(g_pObjectClass))
            {
                baseElement = ELEMENT_TYPE_U8;
            }
            else
            {
                switch (elemType.AsMethodTable()->GetInternalCorElementType())
                {
                    case ELEMENT_TYPE_U8:
                        baseElement = ELEMENT_TYPE_U8;
                        break;
                    case ELEMENT_TYPE_U:
                        baseElement = ELEMENT_TYPE_U;
                        break;
                    default:
                        _ASSERTE(0);
                }
            }
            break;

        case VT_R4:
            baseElement = ELEMENT_TYPE_R4;
            break;

        case VT_R8:
            baseElement = ELEMENT_TYPE_R8;
            break;

        case VT_CY:
            baseType = TypeHandle(CoreLibBinder::GetClass(CLASS__DECIMAL));
            break;

        case VT_DATE:
            baseType = TypeHandle(CoreLibBinder::GetClass(CLASS__DATE_TIME));
            break;

        case VT_DECIMAL:
            baseType = TypeHandle(CoreLibBinder::GetClass(CLASS__DECIMAL));
            break;

        case VT_VARIANT:

            //
            // It would be nice if our conversion between SAFEARRAY and
            // array ref were symmetric.  Right now it is not, because a
            // jagged array converted to a SAFEARRAY and back will come
            // back as an array of variants.
            //
            // We could try to detect the case where we can turn a
            // safearray of variants into a jagged array.  Basically we
            // needs to make sure that all of the variants in the array
            // have the same array type.  (And if that is array of
            // variant, we need to look recursively for another layer.)
            //
            // We also needs to check the dimensions of each array stored
            // in the variant to make sure they have the same rank, and
            // this rank is needed to build the correct array class name.
            // (Note that it will be impossible to tell the rank if all
            // elements in the array are NULL.)
            //

            // <TODO>@nice: implement this functionality if we decide it really makes sense
            // For now, just live with the asymmetry</TODO>

            baseType = TypeHandle(g_pObjectClass);
            break;

        case VT_BSTR:
        case VT_LPWSTR:
        case VT_LPSTR:
            baseElement = ELEMENT_TYPE_STRING;
            break;

        case VT_DISPATCH:
        case VT_UNKNOWN:
            if (elemType.IsNull())
                baseType = TypeHandle(g_pObjectClass);
            else
                baseType = elemType;
            break;

        case VT_RECORD:
            _ASSERTE(!elemType.IsNull());
            baseType = elemType;
            break;

        default:
            COMPlusThrow(kArgumentException, IDS_EE_COM_UNSUPPORTED_SIG);
        }
    }

    if (baseType.IsNull())
        baseType = TypeHandle(CoreLibBinder::GetElementType(baseElement));

    _ASSERTE(!baseType.IsNull());

    return ClassLoader::LoadArrayTypeThrowing(baseType, rank == 0 ? ELEMENT_TYPE_SZARRAY : ELEMENT_TYPE_ARRAY, rank == 0 ? 1 : rank);
}

//
// GetElementSizeForVarType returns the array element size for the given variant type.
//

UINT OleVariant::GetElementSizeForVarType(VARTYPE vt, MethodTable *pInterfaceMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    static const BYTE map[] =
    {
        0,                      // VT_EMPTY
        0,                      // VT_NULL
        2,                      // VT_I2
        4,                      // VT_I4
        4,                      // VT_R4
        8,                      // VT_R8
        sizeof(CURRENCY),       // VT_CY
        sizeof(DATE),           // VT_DATE
        sizeof(BSTR),           // VT_BSTR
        sizeof(IDispatch*),     // VT_DISPATCH
        sizeof(SCODE),          // VT_ERROR
        sizeof(VARIANT_BOOL),   // VT_BOOL
        sizeof(VARIANT),        // VT_VARIANT
        sizeof(IUnknown*),      // VT_UNKNOWN
        sizeof(DECIMAL),        // VT_DECIMAL
        0,                      // unused
        1,                      // VT_I1
        1,                      // VT_UI1
        2,                      // VT_UI2
        4,                      // VT_UI4
        8,                      // VT_I8
        8,                      // VT_UI8
        4,                      // VT_INT
        4,                      // VT_UINT
        0,                      // VT_VOID
        sizeof(HRESULT),        // VT_HRESULT
        sizeof(void*),          // VT_PTR
        sizeof(SAFEARRAY*),     // VT_SAFEARRAY
        sizeof(void*),          // VT_CARRAY
        sizeof(void*),          // VT_USERDEFINED
        sizeof(LPSTR),          // VT_LPSTR
        sizeof(LPWSTR),         // VT_LPWSTR
    };


    // VT_ARRAY indicates a safe array which is always sizeof(SAFEARRAY *).
    if (vt & VT_ARRAY)
        return sizeof(SAFEARRAY*);

    if (vt == VT_RECORD)
    {
        _ASSERTE(pInterfaceMT != NULL);
        return pInterfaceMT->GetNativeSize();
    }
    else if (vt > VT_LPWSTR)
        return 0;
    else
        return map[vt];
}

//
void SafeVariantClear(VARIANT* pVar)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (pVar)
    {
        GCX_PREEMP();
        VariantClear(pVar);

        // VariantClear resets the instance to VT_EMPTY (0)
        // COMPAT: Clear the remaining memory for compat. The instance remains set to VT_EMPTY (0).
        ZeroMemory(pVar, sizeof(VARIANT));
    }
}

struct VariantEmptyHolderTraits final
{
    using Type = VARIANT*;
    static constexpr Type Default() { return NULL; }
    static void Free(Type value)
    {
        WRAPPER_NO_CONTRACT;
        SafeVariantClear(value);
    }
};

using VariantEmptyHolder = LifetimeHolder<VariantEmptyHolderTraits>;

struct RecordVariantHolderTraits final
{
    using Type = VARIANT*;
    static constexpr Type Default() { return NULL; }
    static void Free(Type value)
    {
        LIMITED_METHOD_CONTRACT;

        if (value != NULL)
        {
            if (V_RECORD(value))
                V_RECORDINFO(value)->RecordDestroy(V_RECORD(value));
            if (V_RECORDINFO(value))
                V_RECORDINFO(value)->Release();
        }
    }
};

using RecordVariantHolder = LifetimeHolder<RecordVariantHolderTraits>;

/* ------------------------------------------------------------------------- *
 * Record marshaling routines
 * ------------------------------------------------------------------------- */

void OleVariant::MarshalRecordVariantOleToObject(const VARIANT *pOleVariant,
                                                 OBJECTREF * const & pObj)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pOleVariant));
        PRECONDITION(CheckPointer(pObj));
        PRECONDITION(*pObj == NULL || (IsProtectedByGCFrame (pObj)));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    IRecordInfo *pRecInfo = V_RECORDINFO(pOleVariant);
    if (!pRecInfo)
        COMPlusThrow(kArgumentException, IDS_EE_INVALID_OLE_VARIANT);

    LPVOID pvRecord = V_RECORD(pOleVariant);
    if (pvRecord == NULL)
    {
        SetObjectReference(pObj, NULL);
        return;
    }

    MethodTable* pValueClass = NULL;
    {
        GCX_PREEMP();
        pValueClass = GetMethodTableForRecordInfo(pRecInfo);
    }

    if (pValueClass == NULL)
    {
        // This value type should have been registered through
        // a TLB. CoreCLR doesn't support dynamic type mapping.
        COMPlusThrow(kArgumentException, IDS_EE_CANNOT_MAP_TO_MANAGED_VC);
    }
    _ASSERTE(pValueClass->IsBlittable());

    OBJECTREF BoxedValueClass = NULL;
    GCPROTECT_BEGIN(BoxedValueClass)
    {
        // Now that we have a blittable value class, allocate an instance of the
        // boxed value class and copy the contents of the record into it.
        BoxedValueClass = AllocateObject(pValueClass);
        memcpyNoGCRefs(BoxedValueClass->GetData(), (BYTE*)pvRecord, pValueClass->GetNativeSize());
        SetObjectReference(pObj, BoxedValueClass);
    }
    GCPROTECT_END();
}

// Warning! VariantClear's previous contents of pVarOut.
void OleVariant::MarshalOleVariantForObject(OBJECTREF * const & pObj, VARIANT *pOle)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pObj));
        PRECONDITION(*pObj == NULL || (IsProtectedByGCFrame (pObj)));

        PRECONDITION(CheckPointer(pOle));
    }
    CONTRACTL_END;

    SafeVariantClear(pOle);

#ifdef _DEBUG
    FillMemory(pOle, sizeof(VARIANT),0xdd);
    V_VT(pOle) = VT_EMPTY;
#endif

    // For perf reasons, let's handle the more common and easy cases
    // without transitioning to managed code.
    if (*pObj == NULL)
    {
        // null maps to VT_EMPTY - nothing to do here.
    }
    else
    {
        MethodTable *pMT = (*pObj)->GetMethodTable();
        if (pMT == CoreLibBinder::GetElementType(ELEMENT_TYPE_I4))
        {
            V_I4(pOle) = *(LONG*)( (*pObj)->GetData() );
            V_VT(pOle) = VT_I4;
        }
        else if (pMT == g_pStringClass)
        {
            if (*(pObj) == NULL)
            {
                V_BSTR(pOle) = NULL;
            }
            else
            {
                STRINGREF stringRef = (STRINGREF)(*pObj);
                V_BSTR(pOle) = SysAllocStringLen(stringRef->GetBuffer(), stringRef->GetStringLength());
                if (NULL == V_BSTR(pOle))
                    COMPlusThrowOM();
            }

            V_VT(pOle) = VT_BSTR;
        }
        else if (pMT == CoreLibBinder::GetElementType(ELEMENT_TYPE_I2))
        {
            V_I2(pOle) = *(SHORT*)( (*pObj)->GetData() );
            V_VT(pOle) = VT_I2;
        }
        else if (pMT == CoreLibBinder::GetElementType(ELEMENT_TYPE_I1))
        {
            V_I1(pOle) = *(CHAR*)( (*pObj)->GetData() );
            V_VT(pOle) = VT_I1;
        }
        else if (pMT == CoreLibBinder::GetElementType(ELEMENT_TYPE_I8))
        {
            V_I8(pOle) = *(INT64*)( (*pObj)->GetData() );
            V_VT(pOle) = VT_I8;
        }
        else if (pMT == CoreLibBinder::GetElementType(ELEMENT_TYPE_U4))
        {
            V_UI4(pOle) = *(ULONG*)( (*pObj)->GetData() );
            V_VT(pOle) = VT_UI4;
        }
        else if (pMT == CoreLibBinder::GetElementType(ELEMENT_TYPE_U2))
        {
            V_UI2(pOle) = *(USHORT*)( (*pObj)->GetData() );
            V_VT(pOle) = VT_UI2;
        }
        else if (pMT == CoreLibBinder::GetElementType(ELEMENT_TYPE_U1))
        {
            V_UI1(pOle) = *(BYTE*)( (*pObj)->GetData() );
            V_VT(pOle) = VT_UI1;
        }
        else if (pMT == CoreLibBinder::GetElementType(ELEMENT_TYPE_U8))
        {
            V_UI8(pOle) = *(UINT64*)( (*pObj)->GetData() );
            V_VT(pOle) = VT_UI8;
        }
        else if (pMT == CoreLibBinder::GetElementType(ELEMENT_TYPE_R4))
        {
            V_R4(pOle) = *(FLOAT*)( (*pObj)->GetData() );
            V_VT(pOle) = VT_R4;
        }
        else if (pMT == CoreLibBinder::GetElementType(ELEMENT_TYPE_R8))
        {
            V_R8(pOle) = *(DOUBLE*)( (*pObj)->GetData() );
            V_VT(pOle) = VT_R8;
        }
        else if (pMT == CoreLibBinder::GetElementType(ELEMENT_TYPE_BOOLEAN))
        {
            V_BOOL(pOle) = *(CLR_BOOL*)( (*pObj)->GetData() ) ? VARIANT_TRUE : VARIANT_FALSE;
            V_VT(pOle) = VT_BOOL;
        }
        else if (pMT == CoreLibBinder::GetElementType(ELEMENT_TYPE_I))
        {
            *(LPVOID*)&(V_INT(pOle)) = *(LPVOID*)( (*pObj)->GetData() );
            V_VT(pOle) = VT_INT;
        }
        else if (pMT == CoreLibBinder::GetElementType(ELEMENT_TYPE_U))
        {
            *(LPVOID*)&(V_UINT(pOle)) = *(LPVOID*)( (*pObj)->GetData() );
            V_VT(pOle) = VT_UINT;
        }
        else
        {
            OleVariant::MarshalOleVariantForObjectUncommon(pObj, pOle);
        }
    }
}

void OleVariant::MarshalOleRefVariantForObject(OBJECTREF *pObj, VARIANT *pOle)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pObj));
        PRECONDITION(IsProtectedByGCFrame (pObj));
        PRECONDITION(CheckPointer(pOle));
        PRECONDITION(V_VT(pOle) & VT_BYREF);
    }
    CONTRACTL_END;

    HRESULT hr = MarshalCommonOleRefVariantForObject(pObj, pOle);

    if (FAILED(hr))
    {
        if (hr == DISP_E_BADVARTYPE)
        {
            COMPlusThrow(kInvalidOleVariantTypeException, IDS_EE_INVALID_OLE_VARIANT);
        }
        else if (hr == DISP_E_TYPEMISMATCH)
        {
            COMPlusThrow(kInvalidCastException, IDS_EE_CANNOT_COERCE_BYREF_VARIANT);
        }
        else
        {
            UnmanagedCallersOnlyCaller castVariant(METHOD__VARIANT__CAST_VARIANT);

            // MarshalOleRefVariantForObjectNoCast has checked that the variant is not an array
            // so we can use the marshal cast helper to coerce the object to the proper type.
            VARIANT vtmp;
            VariantInit(&vtmp);
            VARTYPE vt = V_VT(pOle) & ~VT_BYREF;

            castVariant.InvokeThrowing(pObj, (INT32)vt, &vtmp);

            // Managed implementation of CastVariant should either return correct type or throw.
            _ASSERTE(V_VT(&vtmp) == vt);
            InsertContentsIntoByRefVariant(&vtmp, pOle);
        }
    }
}

HRESULT OleVariant::MarshalCommonOleRefVariantForObject(OBJECTREF *pObj, VARIANT *pOle)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pObj));
        PRECONDITION(IsProtectedByGCFrame (pObj));
        PRECONDITION(CheckPointer(pOle));
        PRECONDITION(V_VT(pOle) & VT_BYREF);
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    // Let's try to handle the common trivial cases quickly first before
    // running the generalized stuff.
    MethodTable *pMT = (*pObj) == NULL ? NULL : (*pObj)->GetMethodTable();
    if ( (V_VT(pOle) == (VT_BYREF | VT_I4) || V_VT(pOle) == (VT_BYREF | VT_UI4)) && (pMT == CoreLibBinder::GetElementType(ELEMENT_TYPE_I4) || pMT == CoreLibBinder::GetElementType(ELEMENT_TYPE_U4)) )
    {
        // deallocation of old value optimized away since there's nothing to
        // deallocate for this vartype.

        *(V_I4REF(pOle)) = *(LONG*)( (*pObj)->GetData() );
    }
    else if ( (V_VT(pOle) == (VT_BYREF | VT_I2) || V_VT(pOle) == (VT_BYREF | VT_UI2)) && (pMT == CoreLibBinder::GetElementType(ELEMENT_TYPE_I2) || pMT == CoreLibBinder::GetElementType(ELEMENT_TYPE_U2)) )
    {
        // deallocation of old value optimized away since there's nothing to
        // deallocate for this vartype.

        *(V_I2REF(pOle)) = *(SHORT*)( (*pObj)->GetData() );
    }
    else if ( (V_VT(pOle) == (VT_BYREF | VT_I1) || V_VT(pOle) == (VT_BYREF | VT_UI1)) && (pMT == CoreLibBinder::GetElementType(ELEMENT_TYPE_I1) || pMT == CoreLibBinder::GetElementType(ELEMENT_TYPE_U1)) )
    {
        // deallocation of old value optimized away since there's nothing to
        // deallocate for this vartype.

        *(V_I1REF(pOle)) = *(CHAR*)( (*pObj)->GetData() );
    }
    else if ( (V_VT(pOle) == (VT_BYREF | VT_I8) || V_VT(pOle) == (VT_BYREF | VT_UI8)) && (pMT == CoreLibBinder::GetElementType(ELEMENT_TYPE_I8) || pMT == CoreLibBinder::GetElementType(ELEMENT_TYPE_U8)) )
    {
        // deallocation of old value optimized away since there's nothing to
        // deallocate for this vartype.

        *(V_I8REF(pOle)) = *(INT64*)( (*pObj)->GetData() );
    }
    else if ( V_VT(pOle) == (VT_BYREF | VT_R4) && pMT == CoreLibBinder::GetElementType(ELEMENT_TYPE_R4) )
    {
        // deallocation of old value optimized away since there's nothing to
        // deallocate for this vartype.

        *(V_R4REF(pOle)) = *(FLOAT*)( (*pObj)->GetData() );
    }
    else if ( V_VT(pOle) == (VT_BYREF | VT_R8) && pMT == CoreLibBinder::GetElementType(ELEMENT_TYPE_R8) )
    {
        // deallocation of old value optimized away since there's nothing to
        // deallocate for this vartype.

        *(V_R8REF(pOle)) = *(DOUBLE*)( (*pObj)->GetData() );
    }
    else if ( V_VT(pOle) == (VT_BYREF | VT_BOOL) && pMT == CoreLibBinder::GetElementType(ELEMENT_TYPE_BOOLEAN) )
    {
        // deallocation of old value optimized away since there's nothing to
        // deallocate for this vartype.

        *(V_BOOLREF(pOle)) =  ( *(CLR_BOOL*)( (*pObj)->GetData() ) ) ? VARIANT_TRUE : VARIANT_FALSE;
    }
    else if ( (V_VT(pOle) == (VT_BYREF | VT_INT) || V_VT(pOle) == (VT_BYREF | VT_UINT)) && (pMT == CoreLibBinder::GetElementType(ELEMENT_TYPE_I4) || pMT == CoreLibBinder::GetElementType(ELEMENT_TYPE_U4)) )
    {
        // deallocation of old value optimized away since there's nothing to
        // deallocate for this vartype.

        *(V_INTREF(pOle)) = *(LONG*)( (*pObj)->GetData() );
    }
    else if ( V_VT(pOle) == (VT_BYREF | VT_BSTR) && pMT == g_pStringClass )
    {
        if (*(V_BSTRREF(pOle)))
        {
            SysFreeString(*(V_BSTRREF(pOle)));
            *(V_BSTRREF(pOle)) = NULL;
        }

        *(V_BSTRREF(pOle)) = ConvertStringToBSTR((STRINGREF*)pObj);
    }
    // Special case VT_BYREF|VT_RECORD
    else if (V_VT(pOle) == (VT_BYREF | VT_RECORD))
    {
        // We have a special BYREF RECORD - we cannot call VariantClear on this one, because the caller owns the memory,
        //  so we will call RecordClear, then write our data into the same location.
        hr = ClearAndInsertContentsIntoByrefRecordVariant(pOle, pObj);
        goto Exit;
    }
    else
    {
        VARIANT vtmp;
        VARTYPE vt = V_VT(pOle) & ~VT_BYREF;

        ExtractContentsFromByrefVariant(pOle, &vtmp);
        SafeVariantClear(&vtmp);

        if (vt == VT_VARIANT)
        {
            // Since variants can contain any VARTYPE we simply convert the object to
            // a variant and stuff it back into the byref variant.
            MarshalOleVariantForObject(pObj, &vtmp);
            InsertContentsIntoByRefVariant(&vtmp, pOle);
        }
        else if (vt & VT_ARRAY)
        {
            // Since the marshal cast helper does not support array's the best we can do
            // is marshal the object back to a variant and hope it is of the right type.
            // If it is not then we must throw an exception.
            MarshalOleVariantForObject(pObj, &vtmp);
            if (V_VT(&vtmp) != vt)
            {
                hr = DISP_E_TYPEMISMATCH;
                goto Exit;
            }
            InsertContentsIntoByRefVariant(&vtmp, pOle);
        }
        else if ( (*pObj) == NULL &&
                 (vt == VT_BSTR ||
                  vt == VT_DISPATCH ||
                  vt == VT_UNKNOWN ||
                  vt == VT_PTR ||
                  vt == VT_CARRAY ||
                  vt == VT_SAFEARRAY ||
                  vt == VT_LPSTR ||
                  vt == VT_LPWSTR) )
        {
            // Have to handle this specially since the managed variant
            // conversion will return a VT_EMPTY which isn't what we want.
            V_VT(&vtmp) = vt;
            V_UNKNOWN(&vtmp) = NULL;
            InsertContentsIntoByRefVariant(&vtmp, pOle);
        }
        else
        {
            hr = E_FAIL;
        }
    }
Exit:
    return hr;
}

void OleVariant::MarshalObjectForOleVariant(const VARIANT * pOle, OBJECTREF * const & pObj)
{
    CONTRACT_VOID
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pOle));
        PRECONDITION(CheckPointer(pObj));
        PRECONDITION(*pObj == NULL || (IsProtectedByGCFrame (pObj)));
    }
    CONTRACT_END;

    // if V_ISBYREF(pOle) and V_BYREF(pOle) is null then we have a problem,
    // unless we're dealing with VT_EMPTY or VT_NULL in which case that is ok??
    VARTYPE vt = V_VT(pOle) & ~VT_BYREF;
    if (V_ISBYREF(pOle) && !V_BYREF(pOle) && !(vt == VT_EMPTY || vt == VT_NULL))
        COMPlusThrow(kArgumentException, IDS_EE_INVALID_OLE_VARIANT);

    switch (V_VT(pOle))
    {
        case VT_EMPTY:
            SetObjectReference( pObj,
                                NULL );
            break;

        case VT_I4:
        case VT_INT:
            SetObjectReference( pObj,
                                AllocateObject(CoreLibBinder::GetElementType(ELEMENT_TYPE_I4)) );
            *(LONG*)((*pObj)->GetData()) = V_I4(pOle);
            break;

        case VT_BYREF|VT_I4:
        case VT_BYREF|VT_INT:
            SetObjectReference( pObj,
                                AllocateObject(CoreLibBinder::GetElementType(ELEMENT_TYPE_I4)) );
            *(LONG*)((*pObj)->GetData()) = *(V_I4REF(pOle));
            break;

        case VT_UI4:
        case VT_UINT:
            SetObjectReference( pObj,
                                AllocateObject(CoreLibBinder::GetElementType(ELEMENT_TYPE_U4)) );
            *(ULONG*)((*pObj)->GetData()) = V_UI4(pOle);
            break;

        case VT_BYREF|VT_UI4:
        case VT_BYREF|VT_UINT:
            SetObjectReference( pObj,
                                AllocateObject(CoreLibBinder::GetElementType(ELEMENT_TYPE_U4)) );
            *(ULONG*)((*pObj)->GetData()) = *(V_UI4REF(pOle));
            break;

        case VT_I2:
            SetObjectReference( pObj,
                                AllocateObject(CoreLibBinder::GetElementType(ELEMENT_TYPE_I2)) );
            (*(SHORT*)((*pObj)->GetData())) = V_I2(pOle);
            break;

        case VT_BYREF|VT_I2:
            SetObjectReference( pObj,
                                AllocateObject(CoreLibBinder::GetElementType(ELEMENT_TYPE_I2)) );
            *(SHORT*)((*pObj)->GetData()) = *(V_I2REF(pOle));
            break;

        case VT_UI2:
            SetObjectReference( pObj,
                                AllocateObject(CoreLibBinder::GetElementType(ELEMENT_TYPE_U2)) );
            *(USHORT*)((*pObj)->GetData()) = V_UI2(pOle);
            break;

        case VT_BYREF|VT_UI2:
            SetObjectReference( pObj,
                                AllocateObject(CoreLibBinder::GetElementType(ELEMENT_TYPE_U2)) );
            *(USHORT*)((*pObj)->GetData()) = *(V_UI2REF(pOle));
            break;

        case VT_I1:
            SetObjectReference( pObj,
                                AllocateObject(CoreLibBinder::GetElementType(ELEMENT_TYPE_I1)) );
            *(CHAR*)((*pObj)->GetData()) = V_I1(pOle);
            break;

        case VT_BYREF|VT_I1:
            SetObjectReference( pObj,
                                AllocateObject(CoreLibBinder::GetElementType(ELEMENT_TYPE_I1)) );
            *(CHAR*)((*pObj)->GetData()) = *(V_I1REF(pOle));
            break;

        case VT_UI1:
            SetObjectReference( pObj,
                                AllocateObject(CoreLibBinder::GetElementType(ELEMENT_TYPE_U1)) );
            *(BYTE*)((*pObj)->GetData()) = V_UI1(pOle);
            break;

        case VT_BYREF|VT_UI1:
            SetObjectReference( pObj,
                                AllocateObject(CoreLibBinder::GetElementType(ELEMENT_TYPE_U1)) );
            *(BYTE*)((*pObj)->GetData()) = *(V_UI1REF(pOle));
            break;

        case VT_I8:
            SetObjectReference( pObj,
                                AllocateObject(CoreLibBinder::GetElementType(ELEMENT_TYPE_I8)) );
            *(INT64*)((*pObj)->GetData()) = V_I8(pOle);
            break;

        case VT_BYREF|VT_I8:
            SetObjectReference( pObj,
                                AllocateObject(CoreLibBinder::GetElementType(ELEMENT_TYPE_I8)) );
            *(INT64*)((*pObj)->GetData()) = *(V_I8REF(pOle));
            break;

        case VT_UI8:
            SetObjectReference( pObj,
                                AllocateObject(CoreLibBinder::GetElementType(ELEMENT_TYPE_U8)) );
            *(UINT64*)((*pObj)->GetData()) = V_UI8(pOle);
            break;

        case VT_BYREF|VT_UI8:
            SetObjectReference( pObj,
                                AllocateObject(CoreLibBinder::GetElementType(ELEMENT_TYPE_U8)) );
            *(UINT64*)((*pObj)->GetData()) = *(V_UI8REF(pOle));
            break;

        case VT_R4:
            SetObjectReference( pObj,
                                AllocateObject(CoreLibBinder::GetElementType(ELEMENT_TYPE_R4)) );
            *(FLOAT*)((*pObj)->GetData()) = V_R4(pOle);
            break;

        case VT_BYREF|VT_R4:
            SetObjectReference( pObj,
                                AllocateObject(CoreLibBinder::GetElementType(ELEMENT_TYPE_R4)) );
            *(FLOAT*)((*pObj)->GetData()) = *(V_R4REF(pOle));
            break;

        case VT_R8:
            SetObjectReference( pObj,
                                AllocateObject(CoreLibBinder::GetElementType(ELEMENT_TYPE_R8)) );
            *(DOUBLE*)((*pObj)->GetData()) = V_R8(pOle);
            break;

        case VT_BYREF|VT_R8:
            SetObjectReference( pObj,
                                AllocateObject(CoreLibBinder::GetElementType(ELEMENT_TYPE_R8)) );
            *(DOUBLE*)((*pObj)->GetData()) = *(V_R8REF(pOle));
            break;

        case VT_BOOL:
            SetObjectReference( pObj,
                                AllocateObject(CoreLibBinder::GetElementType(ELEMENT_TYPE_BOOLEAN)) );
            *(VARIANT_BOOL*)((*pObj)->GetData()) = V_BOOL(pOle) ? 1 : 0;
            break;

        case VT_BYREF|VT_BOOL:
            SetObjectReference( pObj,
                                AllocateObject(CoreLibBinder::GetElementType(ELEMENT_TYPE_BOOLEAN)) );
            *(VARIANT_BOOL*)((*pObj)->GetData()) = *(V_BOOLREF(pOle)) ? 1 : 0;
            break;

        case VT_BSTR:
            ConvertBSTRToString(V_BSTR(pOle), (STRINGREF*)pObj);
            break;

        case VT_BYREF|VT_BSTR:
            ConvertBSTRToString(*(V_BSTRREF(pOle)), (STRINGREF*)pObj);
            break;

        default:
            MarshalObjectForOleVariantUncommon(pOle, pObj);
        }
        RETURN;
    }

/* ------------------------------------------------------------------------- *
 * Byref variant manipulation helpers.
 * ------------------------------------------------------------------------- */

void OleVariant::ExtractContentsFromByrefVariant(VARIANT *pByrefVar, VARIANT *pDestVar)
{
    CONTRACT_VOID
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pByrefVar));
        PRECONDITION(CheckPointer(pDestVar));
    }
    CONTRACT_END;

    VARTYPE vt = V_VT(pByrefVar) & ~VT_BYREF;

    // VT_BYREF | VT_EMPTY is not a valid combination.
    if (vt == 0 || vt == VT_EMPTY)
        COMPlusThrow(kInvalidOleVariantTypeException, IDS_EE_INVALID_OLE_VARIANT);

    switch (vt)
    {
        case VT_RECORD:
        {
            // VT_RECORD's are weird in that regardless of is the VT_BYREF flag is set or not
            // they have the same internal representation.
            V_RECORD(pDestVar) = V_RECORD(pByrefVar);
            V_RECORDINFO(pDestVar) = V_RECORDINFO(pByrefVar);

            // Set the variant type of the destination variant.
            V_VT(pDestVar) = vt;

            break;
        }

        case VT_VARIANT:
        {
            // A byref variant is not allowed to contain a byref variant.
            if (V_ISBYREF(V_VARIANTREF(pByrefVar)))
                COMPlusThrow(kInvalidOleVariantTypeException, IDS_EE_INVALID_OLE_VARIANT);

            // Copy the variant that the byref variant points to into the destination variant.
            // This will replace the VARTYPE of pDestVar with the VARTYPE of the VARIANT being
            // pointed to.
            memcpyNoGCRefs(pDestVar, V_VARIANTREF(pByrefVar), sizeof(VARIANT));
            break;
        }

        case VT_DECIMAL:
        {
            // Copy the value that the byref variant points to into the destination variant.
            // Decimal's are special in that they occupy the 16 bits of padding between the
            // VARTYPE and the intVal field.
            memcpyNoGCRefs(&V_DECIMAL(pDestVar), V_DECIMALREF(pByrefVar), sizeof(DECIMAL));

            // Set the variant type of the destination variant.
            V_VT(pDestVar) = vt;

            break;
        }

        default:
        {
            // Copy the value that the byref variant points to into the destination variant.
            SIZE_T sz = OleVariant::GetElementSizeForVarType(vt, NULL);
            memcpyNoGCRefs(&V_INT(pDestVar), V_INTREF(pByrefVar), sz);

            // Set the variant type of the destination variant.
            V_VT(pDestVar) = vt;

            break;
        }
    }

    RETURN;
}

void OleVariant::InsertContentsIntoByRefVariant(VARIANT *pSrcVar, VARIANT *pByrefVar)
{
    CONTRACT_VOID
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pByrefVar));
        PRECONDITION(CheckPointer(pSrcVar));
    }
    CONTRACT_END;

    _ASSERTE(V_VT(pSrcVar) == (V_VT(pByrefVar) & ~VT_BYREF) || V_VT(pByrefVar) == (VT_BYREF | VT_VARIANT));


    VARTYPE vt = V_VT(pByrefVar) & ~VT_BYREF;

    // VT_BYREF | VT_EMPTY is not a valid combination.
    if (vt == 0 || vt == VT_EMPTY)
        COMPlusThrow(kInvalidOleVariantTypeException, IDS_EE_INVALID_OLE_VARIANT);

    switch (vt)
    {
        case VT_RECORD:
        {
            // VT_RECORD's are weird in that regardless of is the VT_BYREF flag is set or not
            // they have the same internal representation.
            V_RECORD(pByrefVar) = V_RECORD(pSrcVar);
            V_RECORDINFO(pByrefVar) = V_RECORDINFO(pSrcVar);
            break;
        }

        case VT_VARIANT:
        {
            // Copy the variant that the byref variant points to into the destination variant.
            memcpyNoGCRefs(V_VARIANTREF(pByrefVar), pSrcVar, sizeof(VARIANT));
            break;
        }

        case VT_DECIMAL:
        {
            // Copy the value inside the source variant into the location pointed to by the byref variant.
            memcpyNoGCRefs(V_DECIMALREF(pByrefVar), &V_DECIMAL(pSrcVar), sizeof(DECIMAL));
            break;
        }

        default:
        {
            // Copy the value inside the source variant into the location pointed to by the byref variant.

            SIZE_T sz = OleVariant::GetElementSizeForVarType(vt, NULL);
            memcpyNoGCRefs(V_INTREF(pByrefVar), &V_INT(pSrcVar), sz);
            break;
        }
    }
    RETURN;
}

void OleVariant::CreateByrefVariantForVariant(VARIANT *pSrcVar, VARIANT *pByrefVar)
{
    CONTRACT_VOID
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pByrefVar));
        PRECONDITION(CheckPointer(pSrcVar));
    }
    CONTRACT_END;

    // Set the type of the byref variant based on the type of the source variant.
    VARTYPE vt = V_VT(pSrcVar);

    // VT_BYREF | VT_EMPTY is not a valid combination.
    if (vt == VT_EMPTY)
        COMPlusThrow(kInvalidOleVariantTypeException, IDS_EE_INVALID_OLE_VARIANT);

    if (vt == VT_NULL)
    {
        // VT_BYREF | VT_NULL is not a valid combination either but we'll allow VT_NULL
        // to be passed this way (meaning that the callee can change the type and return
        // data), note that the VT_BYREF flag is not added
        V_VT(pByrefVar) = vt;
    }
    else
    {
        switch (vt)
        {
            case VT_RECORD:
            {
                // VT_RECORD's are weird in that regardless of is the VT_BYREF flag is set or not
                // they have the same internal representation.
                V_RECORD(pByrefVar) = V_RECORD(pSrcVar);
                V_RECORDINFO(pByrefVar) = V_RECORDINFO(pSrcVar);
                break;
            }

            case VT_VARIANT:
            {
                V_VARIANTREF(pByrefVar) = pSrcVar;
                break;
            }

            case VT_DECIMAL:
            {
                V_DECIMALREF(pByrefVar) = &V_DECIMAL(pSrcVar);
                break;
            }

            default:
            {
                V_INTREF(pByrefVar) = &V_INT(pSrcVar);
                break;
            }
        }

        V_VT(pByrefVar) = vt | VT_BYREF;
    }

    RETURN;
}

/* ------------------------------------------------------------------------- *
 * Variant marshaling
 * ------------------------------------------------------------------------- */

//
// MarshalComVariantForOleVariant copies the contents of the OLE variant from
// the COM variant.
//

void OleVariant::MarshalObjectForOleVariantUncommon(const VARIANT *pOle, OBJECTREF * const & pObj)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pOle));
        PRECONDITION(CheckPointer(pObj));
        PRECONDITION(*pObj == NULL || (IsProtectedByGCFrame (pObj)));
    }
    CONTRACTL_END;

    BOOL byref = V_ISBYREF(pOle);
    VARTYPE vt = V_VT(pOle) & ~VT_BYREF;

    // Note that the following check also covers VT_ILLEGAL.
    if ((vt & ~VT_ARRAY) >= 128 )
        COMPlusThrow(kInvalidOleVariantTypeException, IDS_EE_INVALID_OLE_VARIANT);

    if (byref && !V_BYREF(pOle) && !(vt == VT_EMPTY || vt == VT_NULL))
        COMPlusThrow(kArgumentException, IDS_EE_INVALID_OLE_VARIANT);

    if (byref && vt == VT_VARIANT)
    {
        pOle = V_VARIANTREF(pOle);
        byref = V_ISBYREF(pOle);
        vt = V_VT(pOle) & ~VT_BYREF;

        // Byref VARIANTS are not allowed to be nested.
        if (byref)
            COMPlusThrow(kInvalidOleVariantTypeException, IDS_EE_INVALID_OLE_VARIANT);
    }

    if ((vt & VT_ARRAY))
    {
        if (byref)
            MarshalArrayVariantOleRefToObject(pOle, pObj);
        else
            MarshalArrayVariantOleToObject(pOle, pObj);
    }
    else if (vt == VT_RECORD)
    {
        // The representation of a VT_RECORD and a VT_BYREF | VT_RECORD VARIANT are the same
        MarshalRecordVariantOleToObject(pOle, pObj);
    }
    else
    {
        UnmanagedCallersOnlyCaller convertVariantToObject(METHOD__VARIANT__CONVERT_VARIANT_TO_OBJECT);
        convertVariantToObject.InvokeThrowing(pOle, pObj);
    }
}

//
// MarshalOleVariantForComVariant copies the contents of the OLE variant from
// the COM variant.
//

void OleVariant::MarshalOleVariantForObjectUncommon(OBJECTREF * const & pObj, VARIANT *pOle)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pObj));
        PRECONDITION(*pObj == NULL || (IsProtectedByGCFrame (pObj)));
        PRECONDITION(CheckPointer(pOle));
    }
    CONTRACTL_END;

    SafeVariantClear(pOle);

    VariantEmptyHolder veh;
    veh = pOle;

    if ((*pObj)->GetMethodTable()->IsArray())
    {
        // Get VarType for array
        VARTYPE vt = GetElementVarTypeForArrayRef((BASEARRAYREF)*pObj);
        if (vt == VT_ARRAY)
            vt = VT_VARIANT;

        V_VT(pOle) = vt | VT_ARRAY;
        MarshalArrayVariantObjectToOle(pObj, pOle);
    }
    else
    {
        UnmanagedCallersOnlyCaller convertObjectToVariant(METHOD__VARIANT__CONVERT_OBJECT_TO_VARIANT);
        convertObjectToVariant.InvokeThrowing(pObj, pOle);
    }

    veh.Detach();
}

// Used by customer checked build to test validity of VARIANT

BOOL OleVariant::CheckVariant(VARIANT* pOle)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pOle));
    }
    CONTRACTL_END;

    BOOL bValidVariant = FALSE;

    // We need a try/catch here since VariantCopy could cause an AV if the VARIANT isn't valid.
    EX_TRY
    {
        VARIANT pOleCopy;
        SafeVariantInit(&pOleCopy);

        GCX_PREEMP();
        if (SUCCEEDED(VariantCopy(&pOleCopy, pOle)))
        {
            SafeVariantClear(&pOleCopy);
            bValidVariant = TRUE;
        }
    }
    EX_CATCH
    {
    }
    EX_END_CATCH

    return bValidVariant;
}

HRESULT OleVariant::ClearAndInsertContentsIntoByrefRecordVariant(VARIANT* pOle, OBJECTREF* pObj)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (V_VT(pOle) != (VT_BYREF | VT_RECORD))
        return DISP_E_BADVARTYPE;

    // Clear the current contents of the record.
    {
        GCX_PREEMP();
        V_RECORDINFO(pOle)->RecordClear(V_RECORD(pOle));
    }

    // Ok - let's marshal the returning object into a VT_RECORD.
    if ((*pObj) != NULL)
    {
        VARIANT vtmp;
        SafeVariantInit(&vtmp);

        MarshalOleVariantForObject(pObj, &vtmp);

        {
            GCX_PREEMP();

            // Verify that we have a VT_RECORD.
            if (V_VT(&vtmp) != VT_RECORD)
            {
                SafeVariantClear(&vtmp);
                return DISP_E_TYPEMISMATCH;
            }

            // Verify that we have the same type of record.
            if (! V_RECORDINFO(pOle)->IsMatchingType(V_RECORDINFO(&vtmp)))
            {
                SafeVariantClear(&vtmp);
                return DISP_E_TYPEMISMATCH;
            }

            // Now copy the contents of the new variant back into the old variant.
            HRESULT hr = V_RECORDINFO(pOle)->RecordCopy(V_RECORD(&vtmp), V_RECORD(pOle));
            if (hr != S_OK)
            {
                SafeVariantClear(&vtmp);
                return DISP_E_TYPEMISMATCH;
            }
        }
    }
    return S_OK;
}

void OleVariant::MarshalVarArgVariantArrayToOle(PTRARRAYREF *pClrArray, VARIANT *oleArray)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(oleArray));
        PRECONDITION(CheckPointer(pClrArray));
    }
    CONTRACTL_END;

    ASSERT_PROTECTED(pClrArray);

    SIZE_T elementCount = (*pClrArray)->GetNumComponents();

    VARIANT *pOle = (VARIANT *) oleArray;

    OBJECTREF *pClr = (*pClrArray)->GetDataPtr();

    OBJECTREF TmpObj = NULL;
    GCPROTECT_BEGIN(TmpObj)
    GCPROTECT_BEGININTERIOR(pClr)
    for (SIZE_T i = 0; i < elementCount; i++)
    {
        TmpObj = pClr[i];
        VARIANT *pCurrent = pOle - ((SSIZE_T)i);

        // Marshal the temp managed variant into the OLE variant.
        // We firstly try MarshalCommonOleRefVariantForObject for VT_BYREF variant because
        // MarshalOleVariantForObject() VariantClear the variant and does not keep the VT_BYREF.
        // MarshalCommonOleRefVariantForObject is used instead of MarshalOleRefVariantForObject so
        // that cast will not be done based on the VT of the variant.
        if (!((pCurrent->vt & VT_BYREF) &&
                SUCCEEDED(MarshalCommonOleRefVariantForObject(&TmpObj, pCurrent))))
        {
            if (pCurrent->vt & VT_BYREF)
                MarshalOleVariantForObject(&TmpObj, pCurrent);
        }
    }
    GCPROTECT_END();
    GCPROTECT_END();
}


/* ------------------------------------------------------------------------- *
 * Array marshaling routines
 * ------------------------------------------------------------------------- */

void OleVariant::MarshalArrayVariantOleToObject(const VARIANT* pOleVariant,
                                                OBJECTREF * const & pObj)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pOleVariant));
        PRECONDITION(CheckPointer(pObj));
        PRECONDITION(*pObj == NULL || (IsProtectedByGCFrame (pObj)));
    }
    CONTRACTL_END;

    SAFEARRAY *pSafeArray = V_ARRAY(pOleVariant);

    VARTYPE vt = V_VT(pOleVariant) & ~VT_ARRAY;

    if (pSafeArray)
    {
        if (vt == VT_EMPTY)
            COMPlusThrow(kInvalidOleVariantTypeException, IDS_EE_INVALID_OLE_VARIANT);

        MethodTable *pElemMT = NULL;
        if (vt == VT_RECORD)
            pElemMT = GetElementTypeForRecordSafeArray(pSafeArray).GetMethodTable();

        PCODE pConvertCode;
        {
            GCX_PREEMP();
            pConvertCode = GetInstantiatedSafeArrayMethod(METHOD__STUBHELPERS__CONVERT_ARRAY_CONTENTS_TO_MANAGED, vt, pElemMT, FALSE)->GetMultiCallableAddrOfCode();
        }

        BASEARRAYREF pArrayRef = CreateArrayRefForSafeArray(pSafeArray, vt, pElemMT);
        SetObjectReference(pObj, pArrayRef);
        MarshalArrayRefForSafeArray(pSafeArray, (BASEARRAYREF *) pObj, vt, pElemMT, pConvertCode);
    }
    else
    {
        SetObjectReference(pObj, NULL);
    }
}

void OleVariant::MarshalArrayVariantObjectToOle(OBJECTREF * const & pObj,
                                                VARIANT* pOleVariant)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pOleVariant));
        PRECONDITION(CheckPointer(pObj));
        PRECONDITION(*pObj == NULL || (IsProtectedByGCFrame (pObj)));
    }
    CONTRACTL_END;

    SafeArrayPtrHolder pSafeArray;
    BASEARRAYREF *pArrayRef = (BASEARRAYREF *) pObj;
    MethodTable *pElemMT = NULL;

    _ASSERTE(pArrayRef);

    VARTYPE vt = GetElementVarTypeForArrayRef(*pArrayRef);
    if (vt == VT_ARRAY)
        vt = VT_VARIANT;

    pElemMT = GetArrayElementTypeWrapperAware(pArrayRef).GetMethodTable();

    if (*pArrayRef != NULL)
    {
        pSafeArray = CreateSafeArrayForArrayRef(pArrayRef, vt, pElemMT);

        PCODE pConvertCode;
        {
            GCX_PREEMP();
            pConvertCode = GetInstantiatedSafeArrayMethod(METHOD__STUBHELPERS__CONVERT_ARRAY_CONTENTS_TO_UNMANAGED, vt, pElemMT, FALSE)->GetMultiCallableAddrOfCode();
        }

        MarshalSafeArrayForArrayRef(pArrayRef, pSafeArray, vt, pElemMT, pConvertCode);
    }
    V_ARRAY(pOleVariant) = pSafeArray.Detach();
}

void OleVariant::MarshalArrayVariantOleRefToObject(const VARIANT *pOleVariant,
                                                   OBJECTREF * const & pObj)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pOleVariant));
        PRECONDITION(CheckPointer(pObj));
        PRECONDITION(*pObj == NULL || (IsProtectedByGCFrame (pObj)));
    }
    CONTRACTL_END;

    SAFEARRAY *pSafeArray = *V_ARRAYREF(pOleVariant);

    VARTYPE vt = V_VT(pOleVariant) & ~(VT_ARRAY|VT_BYREF);

    if (pSafeArray)
    {
        MethodTable *pElemMT = NULL;
        if (vt == VT_RECORD)
            pElemMT = GetElementTypeForRecordSafeArray(pSafeArray).GetMethodTable();

        PCODE pConvertCode;
        {
            GCX_PREEMP();
            pConvertCode = GetInstantiatedSafeArrayMethod(METHOD__STUBHELPERS__CONVERT_ARRAY_CONTENTS_TO_MANAGED, vt, pElemMT, FALSE)->GetMultiCallableAddrOfCode();
        }

        BASEARRAYREF pArrayRef = CreateArrayRefForSafeArray(pSafeArray, vt, pElemMT);
        SetObjectReference(pObj, pArrayRef);
        MarshalArrayRefForSafeArray(pSafeArray, (BASEARRAYREF *) pObj, vt, pElemMT, pConvertCode);
    }
    else
    {
        SetObjectReference(pObj, NULL);
    }
}


/* ------------------------------------------------------------------------- *
 * Safearray allocation & conversion
 * ------------------------------------------------------------------------- */

//
// CreateSafeArrayDescriptorForArrayRef creates a SAFEARRAY descriptor with the
// appropriate type & dimensions for the given array ref.  No memory is
// allocated.
//
// This function is useful when you want to allocate the data specially using
// a fixed buffer or pinning.
//

SAFEARRAY *OleVariant::CreateSafeArrayDescriptorForArrayRef(BASEARRAYREF *pArrayRef, VARTYPE vt,
                                                            MethodTable *pInterfaceMT)
{
    CONTRACT (SAFEARRAY*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pArrayRef));
        PRECONDITION(!(vt & VT_ARRAY));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    ASSERT_PROTECTED(pArrayRef);

    ULONG nElem = (*pArrayRef)->GetNumComponents();
    ULONG nRank = (*pArrayRef)->GetRank();

    SafeArrayPtrHolder pSafeArray;

    IfFailThrow(SafeArrayAllocDescriptorEx(vt, nRank, &pSafeArray));

    switch (vt)
    {
        case VT_VARIANT:
        {
            // OleAut32.dll only sets FADF_HASVARTYPE, but VB says we also need to set
            // the FADF_VARIANT bit for this safearray to destruct properly.  OleAut32
            // doesn't want to change their code unless there's a strong reason, since
            // it's all "black magic" anyway.
            pSafeArray->fFeatures |= FADF_VARIANT;
            break;
        }

        case VT_BSTR:
        {
            pSafeArray->fFeatures |= FADF_BSTR;
            break;
        }

        case VT_UNKNOWN:
        {
            pSafeArray->fFeatures |= FADF_UNKNOWN;
            break;
        }

        case VT_DISPATCH:
        {
            pSafeArray->fFeatures |= FADF_DISPATCH;
            break;
        }

        case VT_RECORD:
        {
            pSafeArray->fFeatures |= FADF_RECORD;
            break;
        }
    }

    //
    // Fill in bounds
    //

    SAFEARRAYBOUND *bounds = pSafeArray->rgsabound;
    SAFEARRAYBOUND *boundsEnd = bounds + nRank;
    SIZE_T cElements;

    if (!(*pArrayRef)->IsMultiDimArray())
    {
        bounds[0].cElements = nElem;
        bounds[0].lLbound = 0;
        cElements = nElem;
    }
    else
    {
        const INT32 *count = (*pArrayRef)->GetBoundsPtr()      + nRank - 1;
        const INT32 *lower = (*pArrayRef)->GetLowerBoundsPtr() + nRank - 1;

        cElements = 1;
        while (bounds < boundsEnd)
        {
            bounds->lLbound = *lower--;
            bounds->cElements = *count--;
            cElements *= bounds->cElements;
            bounds++;
        }
    }

    pSafeArray->cbElements = (unsigned)GetElementSizeForVarType(vt, pInterfaceMT);

    // If the SAFEARRAY contains VT_RECORD's, then we need to set the
    // IRecordInfo.
    if (vt == VT_RECORD)
    {
        GCX_PREEMP();

        SafeComHolder<ITypeInfo> pITI;
        SafeComHolder<IRecordInfo> pRecInfo;
        IfFailThrow(GetITypeInfoForEEClass(pInterfaceMT, &pITI));
        IfFailThrow(GetRecordInfoFromTypeInfo(pITI, &pRecInfo));
        IfFailThrow(SafeArraySetRecordInfo(pSafeArray, pRecInfo));
    }

    RETURN pSafeArray.Detach();
}

//
// CreateSafeArrayDescriptorForArrayRef creates a SAFEARRAY with the appropriate
// type & dimensions & data for the given array ref.  The data is initialized to
// zero if necessary for safe destruction.
//

SAFEARRAY *OleVariant::CreateSafeArrayForArrayRef(BASEARRAYREF *pArrayRef, VARTYPE vt,
                                                  MethodTable *pInterfaceMT)
{
    CONTRACT (SAFEARRAY*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pArrayRef));
//        PRECONDITION(CheckPointer(*pArrayRef));
        PRECONDITION(vt != VT_EMPTY);
    }
    CONTRACT_END;
    ASSERT_PROTECTED(pArrayRef);

    // Validate that the type of the managed array is the expected type.
    if (!IsValidArrayForSafeArrayElementType(pArrayRef, vt))
        COMPlusThrow(kSafeArrayTypeMismatchException);

    // For structs and interfaces, verify that the array is of the valid type.
    if (vt == VT_RECORD || vt == VT_UNKNOWN || vt == VT_DISPATCH)
    {
        if (pInterfaceMT && !GetArrayElementTypeWrapperAware(pArrayRef).CanCastTo(TypeHandle(pInterfaceMT)))
            COMPlusThrow(kSafeArrayTypeMismatchException);
    }

    SAFEARRAY *pSafeArray = CreateSafeArrayDescriptorForArrayRef(pArrayRef, vt, pInterfaceMT);

    HRESULT hr = SafeArrayAllocData(pSafeArray);
    if (FAILED(hr))
    {
        SafeArrayDestroy(pSafeArray);
        COMPlusThrowHR(hr);
    }

    RETURN pSafeArray;
}

//
// CreateArrayRefForSafeArray creates an array object with the same layout and type
// as the given safearray.  The variant type of the safearray must be passed in.
// The underlying element method table may also be specified (or NULL may be passed in
// to use the base class method table for the VARTYPE.
//

BASEARRAYREF OleVariant::CreateArrayRefForSafeArray(SAFEARRAY *pSafeArray, VARTYPE vt,
                                                    MethodTable *pElementMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pSafeArray));
        PRECONDITION(vt != VT_EMPTY);
    }
    CONTRACTL_END;

    TypeHandle arrayType;
    INT32 *pAllocateArrayArgs;
    int cAllocateArrayArgs;
    int Rank;
    VARTYPE SafeArrayVT;

    // Validate that the type of the SAFEARRAY is the expected type.
    if (SUCCEEDED(ClrSafeArrayGetVartype(pSafeArray, &SafeArrayVT)) && (SafeArrayVT != VT_EMPTY))
    {
        if ((SafeArrayVT != vt) &&
            !(vt == VT_INT && SafeArrayVT == VT_I4) &&
            !(vt == VT_UINT && SafeArrayVT == VT_UI4) &&
            !(vt == VT_I4 && SafeArrayVT == VT_INT) &&
            !(vt == VT_UI4 && SafeArrayVT == VT_UINT) &&
            !(vt == VT_UNKNOWN && SafeArrayVT == VT_DISPATCH) &&
            !(SafeArrayVT == VT_RECORD))        // Add this to allowed values as a VT_RECORD might represent a
                                                // valuetype with a single field that we'll just treat as a primitive type if possible.
        {
            COMPlusThrow(kSafeArrayTypeMismatchException);
        }
    }
    else
    {
        UINT ArrayElemSize = SafeArrayGetElemsize(pSafeArray);
        if (ArrayElemSize != GetElementSizeForVarType(vt, NULL))
        {
            COMPlusThrow(kSafeArrayTypeMismatchException, IDS_EE_SAFEARRAYTYPEMISMATCH);
        }
    }

    // Determine if the input SAFEARRAY can be converted to an SZARRAY.
    if ((pSafeArray->cDims == 1) && (pSafeArray->rgsabound->lLbound == 0))
    {
        // The SAFEARRAY maps to an SZARRAY. For SZARRAY's AllocateArrayEx()
        // expects the arguments to be a pointer to the cound of elements in the array
        // and the size of the args must be set to 1.
        Rank = 1;
        cAllocateArrayArgs = 1;
        pAllocateArrayArgs = (INT32 *) &pSafeArray->rgsabound[0].cElements;
    }
    else
    {
        // The SAFEARRAY maps to an general array. For general arrays AllocateArrayEx()
        // expects the arguments to be composed of the lower bounds / element count pairs
        // for each of the dimensions. We need to reverse the order that the lower bounds
        // and element pairs are presented before we call AllocateArrayEx().
        Rank = pSafeArray->cDims;
        cAllocateArrayArgs = Rank * 2;
        pAllocateArrayArgs = (INT32*)_alloca(sizeof(INT32) * Rank * 2);
        INT32 * pBoundsPtr = pAllocateArrayArgs;

        // Copy the lower bounds and count of elements for the dimensions. These
        // need to copied in reverse order.
        for (int i = Rank - 1; i >= 0; i--)
        {
            *pBoundsPtr++ = pSafeArray->rgsabound[i].lLbound;
            *pBoundsPtr++ = pSafeArray->rgsabound[i].cElements;
        }
    }

    // Retrieve the type of the array.
    arrayType = GetArrayForVarType(vt, pElementMT, Rank);

    // Allocate the array.
    return (BASEARRAYREF) AllocateArrayEx(arrayType, pAllocateArrayArgs, cAllocateArrayArgs);
}

/* ------------------------------------------------------------------------- *
 * Safearray marshaling
 * ------------------------------------------------------------------------- */

namespace
{
    // Returns the managed IArrayMarshaler<T> MethodTable for a given VARTYPE.
    // This mirrors the logic in GetMarshalerAndElementTypes for SAFEARRAY-compatible types.
    MethodTable* GetMarshalerMTForSafeArrayVarType(VARTYPE vt, MethodTable* pElementMT, BOOL bHeterogeneous, BOOL bNativeDataValid)
    {
        STANDARD_VM_CONTRACT;

        switch (vt)
        {
        case VT_I1:
        {
            TypeHandle th = CoreLibBinder::GetClass(CLASS__SBYTE);
            return TypeHandle(CoreLibBinder::GetClass(CLASS__BLITTABLE_ARRAY_MARSHALER)).Instantiate(Instantiation(&th, 1)).AsMethodTable();
        }
        case VT_UI1:
        {
            TypeHandle th = CoreLibBinder::GetClass(CLASS__BYTE);
            return TypeHandle(CoreLibBinder::GetClass(CLASS__BLITTABLE_ARRAY_MARSHALER)).Instantiate(Instantiation(&th, 1)).AsMethodTable();
        }
        case VT_I2:
        {
            TypeHandle th = CoreLibBinder::GetClass(CLASS__INT16);
            return TypeHandle(CoreLibBinder::GetClass(CLASS__BLITTABLE_ARRAY_MARSHALER)).Instantiate(Instantiation(&th, 1)).AsMethodTable();
        }
        case VT_UI2:
        {
            if (pElementMT == CoreLibBinder::GetClass(CLASS__CHAR))
            {
                TypeHandle th = CoreLibBinder::GetClass(CLASS__CHAR);
                return TypeHandle(CoreLibBinder::GetClass(CLASS__BLITTABLE_ARRAY_MARSHALER)).Instantiate(Instantiation(&th, 1)).AsMethodTable();
            }
            TypeHandle th = CoreLibBinder::GetClass(CLASS__UINT16);
            return TypeHandle(CoreLibBinder::GetClass(CLASS__BLITTABLE_ARRAY_MARSHALER)).Instantiate(Instantiation(&th, 1)).AsMethodTable();
        }
        case VT_I4:
        case VT_INT:
        case VT_ERROR:
        {
            TypeHandle th = (pElementMT == CoreLibBinder::GetClass(CLASS__INTPTR))
                ? CoreLibBinder::GetClass(CLASS__INTPTR)
                : CoreLibBinder::GetClass(CLASS__INT32);
            return TypeHandle(CoreLibBinder::GetClass(CLASS__BLITTABLE_ARRAY_MARSHALER)).Instantiate(Instantiation(&th, 1)).AsMethodTable();
        }
        case VT_UI4:
        case VT_UINT:
        {
            TypeHandle th = (pElementMT == CoreLibBinder::GetClass(CLASS__UINTPTR))
                ? CoreLibBinder::GetClass(CLASS__UINTPTR)
                : CoreLibBinder::GetClass(CLASS__UINT32);
            return TypeHandle(CoreLibBinder::GetClass(CLASS__BLITTABLE_ARRAY_MARSHALER)).Instantiate(Instantiation(&th, 1)).AsMethodTable();
        }
        case VT_I8:
        {
            TypeHandle th = (pElementMT == CoreLibBinder::GetClass(CLASS__INTPTR))
                ? CoreLibBinder::GetClass(CLASS__INTPTR)
                : CoreLibBinder::GetClass(CLASS__INT64);
            return TypeHandle(CoreLibBinder::GetClass(CLASS__BLITTABLE_ARRAY_MARSHALER)).Instantiate(Instantiation(&th, 1)).AsMethodTable();
        }
        case VT_UI8:
        {
            TypeHandle th = (pElementMT == CoreLibBinder::GetClass(CLASS__UINTPTR))
                ? CoreLibBinder::GetClass(CLASS__UINTPTR)
                : CoreLibBinder::GetClass(CLASS__UINT64);
            return TypeHandle(CoreLibBinder::GetClass(CLASS__BLITTABLE_ARRAY_MARSHALER)).Instantiate(Instantiation(&th, 1)).AsMethodTable();
        }
        case VT_R4:
        {
            TypeHandle th = CoreLibBinder::GetClass(CLASS__SINGLE);
            return TypeHandle(CoreLibBinder::GetClass(CLASS__BLITTABLE_ARRAY_MARSHALER)).Instantiate(Instantiation(&th, 1)).AsMethodTable();
        }
        case VT_R8:
        {
            TypeHandle th = CoreLibBinder::GetClass(CLASS__DOUBLE);
            return TypeHandle(CoreLibBinder::GetClass(CLASS__BLITTABLE_ARRAY_MARSHALER)).Instantiate(Instantiation(&th, 1)).AsMethodTable();
        }
        case VT_DECIMAL:
        {
            TypeHandle th = CoreLibBinder::GetClass(CLASS__DECIMAL);
            return TypeHandle(CoreLibBinder::GetClass(CLASS__BLITTABLE_ARRAY_MARSHALER)).Instantiate(Instantiation(&th, 1)).AsMethodTable();
        }
        case VT_BOOL:
            return CoreLibBinder::GetClass(CLASS__VARIANT_BOOL_MARSHALER);

        case VT_DATE:
            return CoreLibBinder::GetClass(CLASS__DATEMARSHALER);

        case VT_LPWSTR:
            return CoreLibBinder::GetClass(CLASS__LPWSTR_MARSHALER);

        case VT_LPSTR:
        {
            // SAFEARRAY LPSTR marshalling always uses default best-fit/throw-on-unmappable.
            MethodTable* pBestFitEnabledMT = CoreLibBinder::GetClass(CLASS__MARSHALER_OPTION_ENABLED);
            MethodTable* pThrowOnUnmappableDisabledMT = CoreLibBinder::GetClass(CLASS__MARSHALER_OPTION_DISABLED);
            TypeHandle thArgs[2] = { TypeHandle(pBestFitEnabledMT), TypeHandle(pThrowOnUnmappableDisabledMT) };
            return TypeHandle(CoreLibBinder::GetClass(CLASS__LPSTR_ARRAY_ELEMENT_MARSHALER)).Instantiate(Instantiation(thArgs, 2)).AsMethodTable();
        }

        case VT_CY:
            return CoreLibBinder::GetClass(CLASS__CURRENCY_ARRAY_ELEMENT_MARSHALER);

        case VT_BSTR:
            return CoreLibBinder::GetClass(CLASS__BSTR_ARRAY_ELEMENT_MARSHALER);

        case VT_UNKNOWN:
        case VT_DISPATCH:
        {
            if (pElementMT == NULL || pElementMT == g_pObjectClass)
            {
                if (bHeterogeneous)
                {
                    return CoreLibBinder::GetClass(CLASS__HETEROGENEOUS_INTERFACE_ARRAY_ELEMENT_MARSHALER);
                }
                MethodTable* pEnabledMT = CoreLibBinder::GetClass(CLASS__MARSHALER_OPTION_ENABLED);
                MethodTable* pDisabledMT = CoreLibBinder::GetClass(CLASS__MARSHALER_OPTION_DISABLED);
                TypeHandle thDispatch(vt == VT_DISPATCH ? pEnabledMT : pDisabledMT);
                return TypeHandle(CoreLibBinder::GetClass(CLASS__INTERFACE_ARRAY_ELEMENT_MARSHALER)).Instantiate(Instantiation(&thDispatch, 1)).AsMethodTable();
            }
            else if (!pElementMT->IsInterface())
            {
                // For class types, resolve the default COM interface.
                BOOL bDispatch = FALSE;
                MethodTable* pDefaultItfMT = GetDefaultInterfaceMTForClass(pElementMT, &bDispatch);
                if (pDefaultItfMT != NULL)
                {
                    // Use the resolved interface type.
                    TypeHandle thElement(pDefaultItfMT);
                    return TypeHandle(CoreLibBinder::GetClass(CLASS__TYPED_INTERFACE_ARRAY_ELEMENT_MARSHALER)).Instantiate(Instantiation(&thElement, 1)).AsMethodTable();
                }
                else
                {
                    // No specific interface — use untyped IDispatch or IUnknown.
                    MethodTable* pEnabledMT = CoreLibBinder::GetClass(CLASS__MARSHALER_OPTION_ENABLED);
                    MethodTable* pDisabledMT = CoreLibBinder::GetClass(CLASS__MARSHALER_OPTION_DISABLED);
                    TypeHandle thDispatch(bDispatch ? pEnabledMT : pDisabledMT);
                    return TypeHandle(CoreLibBinder::GetClass(CLASS__INTERFACE_ARRAY_ELEMENT_MARSHALER)).Instantiate(Instantiation(&thDispatch, 1)).AsMethodTable();
                }
            }
            else
            {
                TypeHandle thElement(pElementMT);
                return TypeHandle(CoreLibBinder::GetClass(CLASS__TYPED_INTERFACE_ARRAY_ELEMENT_MARSHALER)).Instantiate(Instantiation(&thElement, 1)).AsMethodTable();
            }
        }

        case VT_VARIANT:
        {
            MethodTable* pOptionMT = bNativeDataValid
                ? CoreLibBinder::GetClass(CLASS__MARSHALER_OPTION_ENABLED)
                : CoreLibBinder::GetClass(CLASS__MARSHALER_OPTION_DISABLED);
            TypeHandle thOption(pOptionMT);
            return TypeHandle(CoreLibBinder::GetClass(CLASS__VARIANT_ARRAY_ELEMENT_MARSHALER)).Instantiate(Instantiation(&thOption, 1)).AsMethodTable();
        }

        case VT_RECORD:
        {
            _ASSERTE(pElementMT != NULL);
            TypeHandle thElement(pElementMT);
            if (thElement.IsBlittable())
            {
                return TypeHandle(CoreLibBinder::GetClass(CLASS__BLITTABLE_ARRAY_MARSHALER)).Instantiate(Instantiation(&thElement, 1)).AsMethodTable();
            }
            else
            {
                return TypeHandle(CoreLibBinder::GetClass(CLASS__STRUCTURE_MARSHALER)).Instantiate(Instantiation(&thElement, 1)).AsMethodTable();
            }
        }

        default:
            _ASSERTE(!"Unsupported VT for SafeArray marshaler");
            COMPlusThrow(kArgumentException, IDS_EE_COM_UNSUPPORTED_SIG);
            return NULL;
        }
    }

    // Returns the element TypeHandle for a given VARTYPE and element MethodTable.
    TypeHandle GetElementTypeForSafeArrayVarType(VARTYPE vt, MethodTable* pElementMT)
    {
        STANDARD_VM_CONTRACT;

        switch (vt)
        {
        case VT_BOOL:       return TypeHandle(CoreLibBinder::GetClass(CLASS__BOOLEAN));
        case VT_I1:         return TypeHandle(CoreLibBinder::GetClass(CLASS__SBYTE));
        case VT_UI1:        return TypeHandle(CoreLibBinder::GetClass(CLASS__BYTE));
        case VT_I2:         return TypeHandle(CoreLibBinder::GetClass(CLASS__INT16));
        case VT_UI2:
            if (pElementMT == CoreLibBinder::GetClass(CLASS__CHAR))
                return TypeHandle(CoreLibBinder::GetClass(CLASS__CHAR));
            return TypeHandle(CoreLibBinder::GetClass(CLASS__UINT16));
        case VT_I4:
        case VT_INT:
        case VT_ERROR:
            if (pElementMT == CoreLibBinder::GetClass(CLASS__INTPTR))
                return TypeHandle(CoreLibBinder::GetClass(CLASS__INTPTR));
            return TypeHandle(CoreLibBinder::GetClass(CLASS__INT32));
        case VT_UI4:
        case VT_UINT:
            if (pElementMT == CoreLibBinder::GetClass(CLASS__UINTPTR))
                return TypeHandle(CoreLibBinder::GetClass(CLASS__UINTPTR));
            return TypeHandle(CoreLibBinder::GetClass(CLASS__UINT32));
        case VT_I8:
            if (pElementMT == CoreLibBinder::GetClass(CLASS__INTPTR))
                return TypeHandle(CoreLibBinder::GetClass(CLASS__INTPTR));
            return TypeHandle(CoreLibBinder::GetClass(CLASS__INT64));
        case VT_UI8:
            if (pElementMT == CoreLibBinder::GetClass(CLASS__UINTPTR))
                return TypeHandle(CoreLibBinder::GetClass(CLASS__UINTPTR));
            return TypeHandle(CoreLibBinder::GetClass(CLASS__UINT64));
        case VT_R4:         return TypeHandle(CoreLibBinder::GetClass(CLASS__SINGLE));
        case VT_R8:         return TypeHandle(CoreLibBinder::GetClass(CLASS__DOUBLE));
        case VT_DECIMAL:    return TypeHandle(CoreLibBinder::GetClass(CLASS__DECIMAL));
        case VT_DATE:       return TypeHandle(CoreLibBinder::GetClass(CLASS__DATE_TIME));
        case VT_BSTR:
        case VT_LPWSTR:
        case VT_LPSTR:      return TypeHandle(g_pStringClass);
        case VT_CY:         return TypeHandle(CoreLibBinder::GetClass(CLASS__DECIMAL));
        case VT_VARIANT:    return TypeHandle(g_pObjectClass);
        case VT_UNKNOWN:
        case VT_DISPATCH:
            if (pElementMT == NULL || pElementMT == g_pObjectClass)
                return TypeHandle(g_pObjectClass);
            if (pElementMT->IsInterface())
                return TypeHandle(pElementMT);
            {
                // For class types, resolve to the default interface type.
                BOOL bDispatch = FALSE;
                MethodTable* pDefaultItfMT = GetDefaultInterfaceMTForClass(pElementMT, &bDispatch);
                if (pDefaultItfMT != NULL)
                    return TypeHandle(pDefaultItfMT);
                return TypeHandle(g_pObjectClass);
            }
        case VT_RECORD:
            _ASSERTE(pElementMT != NULL);
            return TypeHandle(pElementMT);
        default:
            COMPlusThrow(kArgumentException, IDS_EE_COM_UNSUPPORTED_SIG);
            return TypeHandle();
        }
    }
}

MethodDesc* GetInstantiatedSafeArrayMethod(BinderMethodID methodId, VARTYPE vt, MethodTable* pElementMT, BOOL bHeterogeneous, BOOL bNativeDataValid)
{
    STANDARD_VM_CONTRACT;

    MethodDesc* pGenericMD = CoreLibBinder::GetMethod(methodId);

    TypeHandle thElementType = GetElementTypeForSafeArrayVarType(vt, pElementMT);
    TypeHandle thMarshalerType(GetMarshalerMTForSafeArrayVarType(vt, pElementMT, bHeterogeneous, bNativeDataValid));

    TypeHandle thArgs[2] = { thElementType, thMarshalerType };

    return MethodDesc::FindOrCreateAssociatedMethodDesc(
        pGenericMD,
        pGenericMD->GetMethodTable(),
        FALSE,
        Instantiation(thArgs, 2),
        FALSE);
}

//
// MarshalSafeArrayForArrayRef marshals the contents of the array ref into the given
// safe array. It is assumed that the type & dimensions of the arrays are compatible.
//
void OleVariant::MarshalSafeArrayForArrayRef(BASEARRAYREF *pArrayRef,
                                             SAFEARRAY *pSafeArray,
                                             VARTYPE vt,
                                             MethodTable *pInterfaceMT,
                                             PCODE pConvertContentsCode)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pSafeArray));
        PRECONDITION(CheckPointer(pArrayRef));
//        PRECONDITION(CheckPointer(*pArrayRef));
        PRECONDITION(vt != VT_EMPTY);
    }
    CONTRACTL_END;

    ASSERT_PROTECTED(pArrayRef);

    // Retrieve the size and number of components.
    SIZE_T dwComponentSize = GetElementSizeForVarType(vt, pInterfaceMT);
    SIZE_T dwNumComponents = (*pArrayRef)->GetNumComponents();
    BASEARRAYREF Array = NULL;

    GCPROTECT_BEGIN(Array)
    {
        // If the array is an array of wrappers, then we need to extract the objects
        // being wrapped and create an array of those.
        BOOL bArrayOfInterfaceWrappers = FALSE;
        if (IsArrayOfWrappers(pArrayRef, &bArrayOfInterfaceWrappers))
        {
            Array = ExtractWrappedObjectsFromArray(pArrayRef);
        }
        else
        {
            Array = *pArrayRef;
        }

        // Use managed IArrayMarshaler<T> implementations for content conversion.
        UnmanagedCallersOnlyCaller invoker(METHOD__STUBHELPERS__INVOKE_ARRAY_CONTENTS_CONVERTER);
        invoker.InvokeThrowing(&Array, pSafeArray->pvData, (INT32)dwNumComponents, (void*)pConvertContentsCode);

        if (pSafeArray->cDims != 1)
        {
            // The array is multidimensional - transpose the data in place.
            TransposeArrayData((BYTE*)pSafeArray->pvData, (BYTE*)pSafeArray->pvData, dwNumComponents, dwComponentSize, pSafeArray, FALSE);
        }
    }
    GCPROTECT_END();
}

//
// MarshalArrayRefForSafeArray marshals the contents of the safe array into the given
// array ref. It is assumed that the type & dimensions of the arrays are compatible.
//

void OleVariant::MarshalArrayRefForSafeArray(SAFEARRAY *pSafeArray,
                                             BASEARRAYREF *pArrayRef,
                                             VARTYPE vt,
                                             MethodTable *pInterfaceMT,
                                             PCODE pConvertContentsCode)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pSafeArray));
        PRECONDITION(CheckPointer(pArrayRef));
        PRECONDITION(*pArrayRef != NULL);
        PRECONDITION(vt != VT_EMPTY);
    }
    CONTRACTL_END;

    ASSERT_PROTECTED(pArrayRef);

    // Retrieve the number of components.
    SIZE_T dwNumComponents = (*pArrayRef)->GetNumComponents();
    SIZE_T dwNativeComponentSize = GetElementSizeForVarType(vt, pInterfaceMT);

    CQuickArray<BYTE> TmpArray;
    BYTE* pSrcData = NULL;

    if (pSafeArray->cDims != 1)
    {
        // Multi-dimensional arrays need transposition before content conversion.
        TmpArray.ReSizeThrows(dwNumComponents * dwNativeComponentSize);
        pSrcData = TmpArray.Ptr();
        TransposeArrayData(pSrcData, (BYTE*)pSafeArray->pvData, dwNumComponents, dwNativeComponentSize, pSafeArray, TRUE);
    }
    else
    {
        pSrcData = (BYTE*)pSafeArray->pvData;
    }

    // Use managed IArrayMarshaler<T> implementations for content conversion.
    UnmanagedCallersOnlyCaller invoker(METHOD__STUBHELPERS__INVOKE_ARRAY_CONTENTS_CONVERTER);
    invoker.InvokeThrowing(pArrayRef, pSrcData, (INT32)dwNumComponents, (void*)pConvertContentsCode);
}

void OleVariant::ConvertValueClassToVariant(OBJECTREF *pBoxedValueClass, VARIANT *pOleVariant)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pBoxedValueClass));
        PRECONDITION(CheckPointer(pOleVariant));
        PRECONDITION(*pBoxedValueClass == NULL || (IsProtectedByGCFrame (pBoxedValueClass)));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    SafeComHolder<ITypeInfo> pTypeInfo = NULL;
    RecordVariantHolder pRecHolder(pOleVariant);

    // Initialize the OLE variant's VT_RECORD fields to NULL.
    V_RECORDINFO(pRecHolder) = NULL;
    V_RECORD(pRecHolder) = NULL;

    // Retrieve the ITypeInfo for the value class.
    MethodTable *pValueClassMT = (*pBoxedValueClass)->GetMethodTable();
    hr = GetITypeInfoForEEClass(pValueClassMT, &pTypeInfo, true /* bClassInfo */);
    if (FAILED(hr))
    {
        if (hr == TLBX_E_LIBNOTREGISTERED)
        {
            // Indicate that conversion of the class to variant without a registered type lib is not supported
            StackSString className;
            pValueClassMT->_GetFullyQualifiedNameForClass(className);
            COMPlusThrow(kNotSupportedException, IDS_EE_CLASS_TO_VARIANT_TLB_NOT_REG, className.GetUnicode());
        }
        else
        {
            COMPlusThrowHR(hr);
        }
    }

    // Convert the ITypeInfo to an IRecordInfo.
    hr = GetRecordInfoFromTypeInfo(pTypeInfo, &V_RECORDINFO(pRecHolder));
    if (FAILED(hr))
    {
        // An HRESULT of TYPE_E_UNSUPFORMAT really means that the struct contains
        // fields that aren't supported inside a OLEAUT record.
        if (TYPE_E_UNSUPFORMAT == hr)
            COMPlusThrow(kArgumentException, IDS_EE_RECORD_NON_SUPPORTED_FIELDS);
        else
            COMPlusThrowHR(hr);
    }

    // Allocate an instance of the record.
    V_RECORD(pRecHolder) = V_RECORDINFO(pRecHolder)->RecordCreate();
    IfNullThrow(V_RECORD(pRecHolder));

    if (pValueClassMT->IsBlittable())
    {
        // If the value class is blittable, then we can just copy the bits over.
        memcpyNoGCRefs(V_RECORD(pRecHolder), (*pBoxedValueClass)->GetData(), pValueClassMT->GetNativeSize());
    }
    else
    {
        UnmanagedCallersOnlyCaller convertToUnmanaged(METHOD__STUBHELPERS__LAYOUT_TYPE_CONVERT_TO_UNMANAGED);
        convertToUnmanaged.InvokeThrowing(
            pBoxedValueClass,
            V_RECORD(pRecHolder));
    }

    pRecHolder.Detach();
}

void OleVariant::TransposeArrayData(BYTE *pDestData, BYTE *pSrcData, SIZE_T dwNumComponents, SIZE_T dwComponentSize, SAFEARRAY *pSafeArray, BOOL bSafeArrayToMngArray)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pDestData));
        PRECONDITION(CheckPointer(pSrcData));
        PRECONDITION(CheckPointer(pSafeArray));
    }
    CONTRACTL_END;

    int iDims;
    DWORD *aDestElemCount = (DWORD*)_alloca(pSafeArray->cDims * sizeof(DWORD));
    DWORD *aDestIndex = (DWORD*)_alloca(pSafeArray->cDims * sizeof(DWORD));
    BYTE **aDestDataPos = (BYTE **)_alloca(pSafeArray->cDims * sizeof(BYTE *));
    SIZE_T *aDestDelta = (SIZE_T*)_alloca(pSafeArray->cDims * sizeof(SIZE_T));
    CQuickArray<BYTE> TmpArray;

    // If there are no components, then there we are done.
    if (dwNumComponents == 0)
        return;

    // Check to see if we are transposing in place or copying and transposing.
    if (pSrcData == pDestData)
    {
        TmpArray.ReSizeThrows(dwNumComponents * dwComponentSize);
        memcpyNoGCRefs(TmpArray.Ptr(), pSrcData, dwNumComponents * dwComponentSize);
        pSrcData = TmpArray.Ptr();
    }

    // Copy the element count in reverse order if we are copying from a safe array to
    // a managed array and in direct order otherwise.
    if (bSafeArrayToMngArray)
    {
        for (iDims = 0; iDims < pSafeArray->cDims; iDims++)
            aDestElemCount[iDims] = pSafeArray->rgsabound[pSafeArray->cDims - iDims - 1].cElements;
    }
    else
    {
        for (iDims = 0; iDims < pSafeArray->cDims; iDims++)
            aDestElemCount[iDims] = pSafeArray->rgsabound[iDims].cElements;
    }

    // Initialize the indexes for each dimension to 0.
    memset(aDestIndex, 0, pSafeArray->cDims * sizeof(int));

    // Set all the destination data positions to the start of the array.
    for (iDims = 0; iDims < pSafeArray->cDims; iDims++)
        aDestDataPos[iDims] = (BYTE*)pDestData;

    // Calculate the destination delta for each of the dimensions.
    aDestDelta[pSafeArray->cDims - 1] = dwComponentSize;
    for (iDims = pSafeArray->cDims - 2; iDims >= 0; iDims--)
        aDestDelta[iDims] = aDestDelta[iDims + 1] * aDestElemCount[iDims + 1];

    // Calculate the source data end pointer.
    BYTE *pSrcDataEnd = pSrcData + dwNumComponents * dwComponentSize;
    _ASSERTE(pDestData < pSrcData || pDestData >= pSrcDataEnd);

    // Copy and transpose the data.
    while (TRUE)
    {
        // Copy one component.
        memcpyNoGCRefs(aDestDataPos[0], pSrcData, dwComponentSize);

        // Update the source position.
        pSrcData += dwComponentSize;

        // Check to see if we have reached the end of the array.
        if (pSrcData >= pSrcDataEnd)
            break;

        // Update the destination position.
        for (iDims = 0; aDestIndex[iDims] >= aDestElemCount[iDims] - 1; iDims++);

        _ASSERTE(iDims < pSafeArray->cDims);

        aDestIndex[iDims]++;
        aDestDataPos[iDims] += aDestDelta[iDims];
        for (--iDims; iDims >= 0; iDims--)
        {
            aDestIndex[iDims] = 0;
            aDestDataPos[iDims] = aDestDataPos[iDims + 1];
        }
    }
}

BOOL OleVariant::IsArrayOfWrappers(_In_ BASEARRAYREF *pArray, _Out_opt_ BOOL *pbOfInterfaceWrappers)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!g_pConfig->IsBuiltInCOMSupported())
    {
        return FALSE;
    }

    TypeHandle hndElemType = (*pArray)->GetArrayElementTypeHandle();

    if (!hndElemType.IsTypeDesc())
    {
        if (hndElemType == TypeHandle(CoreLibBinder::GetClass(CLASS__DISPATCH_WRAPPER)) ||
            hndElemType == TypeHandle(CoreLibBinder::GetClass(CLASS__UNKNOWN_WRAPPER)))
        {
            if (pbOfInterfaceWrappers)
            {
                *pbOfInterfaceWrappers = TRUE;
            }
            return TRUE;
        }

        if (hndElemType == TypeHandle(CoreLibBinder::GetClass(CLASS__ERROR_WRAPPER)) ||
            hndElemType == TypeHandle(CoreLibBinder::GetClass(CLASS__CURRENCY_WRAPPER)) ||
            hndElemType == TypeHandle(CoreLibBinder::GetClass(CLASS__BSTR_WRAPPER)))
        {
            if (pbOfInterfaceWrappers)
            {
                *pbOfInterfaceWrappers = FALSE;
            }
            return TRUE;
        }
    }

    if (pbOfInterfaceWrappers)
        *pbOfInterfaceWrappers = FALSE;

    return FALSE;
}

BASEARRAYREF OleVariant::ExtractWrappedObjectsFromArray(BASEARRAYREF *pArray)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pArray));
        PRECONDITION(IsArrayOfWrappers(pArray, NULL));
    }
    CONTRACTL_END;

    TypeHandle hndWrapperType = (*pArray)->GetArrayElementTypeHandle();
    TypeHandle hndElemType;
    TypeHandle hndArrayType;
    BOOL bIsMDArray = (*pArray)->IsMultiDimArray();
    unsigned rank = (*pArray)->GetRank();
    BASEARRAYREF RetArray = NULL;

    // Retrieve the element type handle for the array to create.
    if (hndWrapperType == TypeHandle(CoreLibBinder::GetClass(CLASS__DISPATCH_WRAPPER)))
        hndElemType = TypeHandle(g_pObjectClass);

    else if (hndWrapperType == TypeHandle(CoreLibBinder::GetClass(CLASS__UNKNOWN_WRAPPER)))
        hndElemType = TypeHandle(g_pObjectClass);

    else if (hndWrapperType == TypeHandle(CoreLibBinder::GetClass(CLASS__BSTR_WRAPPER)))
        hndElemType = TypeHandle(g_pStringClass);

    else if (hndWrapperType == TypeHandle(CoreLibBinder::GetClass(CLASS__ERROR_WRAPPER)))
        hndElemType = TypeHandle(CoreLibBinder::GetClass(CLASS__INT32));

    else if (hndWrapperType == TypeHandle(CoreLibBinder::GetClass(CLASS__CURRENCY_WRAPPER)))
        hndElemType = TypeHandle(CoreLibBinder::GetClass(CLASS__DECIMAL));

    else
        _ASSERTE(!"Invalid wrapper type");

    // Retrieve the type handle that represents the array.
    if (bIsMDArray)
    {
        hndArrayType = ClassLoader::LoadArrayTypeThrowing(hndElemType, ELEMENT_TYPE_ARRAY, rank);
    }
    else
    {
        hndArrayType = ClassLoader::LoadArrayTypeThrowing(hndElemType, ELEMENT_TYPE_SZARRAY);
    }
    _ASSERTE(!hndArrayType.IsNull());

    // Set up the bounds arguments.
    DWORD numArgs =  rank*2;
        INT32* args = (INT32*) _alloca(sizeof(INT32)*numArgs);

    if (bIsMDArray)
    {
        const INT32* bounds = (*pArray)->GetBoundsPtr();
        const INT32* lowerBounds = (*pArray)->GetLowerBoundsPtr();
        for(unsigned int i=0; i < rank; i++)
        {
            args[2*i]   = lowerBounds[i];
            args[2*i+1] = bounds[i];
        }
    }
    else
    {
        numArgs = 1;
        args[0] = (*pArray)->GetNumComponents();
    }

    // Extract the values from the source array and copy them into the destination array.
    BASEARRAYREF DestArray = (BASEARRAYREF)AllocateArrayEx(hndArrayType, args, numArgs);
    GCPROTECT_BEGIN(DestArray)
    {
        SIZE_T NumComponents = (*pArray)->GetNumComponents();

        if (hndWrapperType == TypeHandle(CoreLibBinder::GetClass(CLASS__DISPATCH_WRAPPER)))
        {
            DISPATCHWRAPPEROBJECTREF *pSrc = (DISPATCHWRAPPEROBJECTREF *)(*pArray)->GetDataPtr();
            DISPATCHWRAPPEROBJECTREF *pSrcEnd = pSrc + NumComponents;
            OBJECTREF *pDest = (OBJECTREF *)DestArray->GetDataPtr();
            for (; pSrc < pSrcEnd; pSrc++, pDest++)
                SetObjectReference(pDest, (*pSrc) != NULL ? (*pSrc)->GetWrappedObject() : NULL);
        }
        else if (hndWrapperType == TypeHandle(CoreLibBinder::GetClass(CLASS__UNKNOWN_WRAPPER)))
        {
            UNKNOWNWRAPPEROBJECTREF *pSrc = (UNKNOWNWRAPPEROBJECTREF *)(*pArray)->GetDataPtr();
            UNKNOWNWRAPPEROBJECTREF *pSrcEnd = pSrc + NumComponents;
            OBJECTREF *pDest = (OBJECTREF *)DestArray->GetDataPtr();
            for (; pSrc < pSrcEnd; pSrc++, pDest++)
                SetObjectReference(pDest, (*pSrc) != NULL ? (*pSrc)->GetWrappedObject() : NULL);
        }
        else if (hndWrapperType == TypeHandle(CoreLibBinder::GetClass(CLASS__ERROR_WRAPPER)))
        {
            ERRORWRAPPEROBJECTREF *pSrc = (ERRORWRAPPEROBJECTREF *)(*pArray)->GetDataPtr();
            ERRORWRAPPEROBJECTREF *pSrcEnd = pSrc + NumComponents;
            INT32 *pDest = (INT32 *)DestArray->GetDataPtr();
            for (; pSrc < pSrcEnd; pSrc++, pDest++)
                *pDest = (*pSrc) != NULL ? (*pSrc)->GetErrorCode() : NULL;
        }
        else if (hndWrapperType == TypeHandle(CoreLibBinder::GetClass(CLASS__CURRENCY_WRAPPER)))
        {
            CURRENCYWRAPPEROBJECTREF *pSrc = (CURRENCYWRAPPEROBJECTREF *)(*pArray)->GetDataPtr();
            CURRENCYWRAPPEROBJECTREF *pSrcEnd = pSrc + NumComponents;
            DECIMAL *pDest = (DECIMAL *)DestArray->GetDataPtr();
            for (; pSrc < pSrcEnd; pSrc++, pDest++)
            {
                if (*pSrc != NULL)
                    memcpyNoGCRefs(pDest, &(*pSrc)->GetWrappedObject(), sizeof(DECIMAL));
                else
                    memset(pDest, 0, sizeof(DECIMAL));
            }
        }
        else if (hndWrapperType == TypeHandle(CoreLibBinder::GetClass(CLASS__BSTR_WRAPPER)))
        {
            BSTRWRAPPEROBJECTREF *pSrc = (BSTRWRAPPEROBJECTREF *)(*pArray)->GetDataPtr();
            BSTRWRAPPEROBJECTREF *pSrcEnd = pSrc + NumComponents;
            OBJECTREF *pDest = (OBJECTREF *)DestArray->GetDataPtr();
            for (; pSrc < pSrcEnd; pSrc++, pDest++)
                SetObjectReference(pDest, (*pSrc) != NULL ? (*pSrc)->GetWrappedObject() : NULL);
        }
        else
        {
            _ASSERTE(!"Invalid wrapper type");
        }

        // GCPROTECT_END() will wack NewArray so we need to copy the OBJECTREF into
        // a temp to be able to return it.
        RetArray = DestArray;
    }
    GCPROTECT_END();

    return RetArray;
}

TypeHandle OleVariant::GetWrappedArrayElementType(BASEARRAYREF *pArray)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pArray));
        PRECONDITION(IsArrayOfWrappers(pArray, NULL));
    }
    CONTRACTL_END;

    TypeHandle hndWrapperType = (*pArray)->GetArrayElementTypeHandle();
    TypeHandle pWrappedObjType;

    if (hndWrapperType == TypeHandle(CoreLibBinder::GetClass(CLASS__DISPATCH_WRAPPER)) ||
        hndWrapperType == TypeHandle(CoreLibBinder::GetClass(CLASS__UNKNOWN_WRAPPER)))
    {
        // There's no need to traverse the array up front. We'll use the default interface
        // for each element in code:OleVariant::MarshalInterfaceArrayComToOleHelper.
        pWrappedObjType = TypeHandle(g_pObjectClass);
    }
    else if (hndWrapperType == TypeHandle(CoreLibBinder::GetClass(CLASS__ERROR_WRAPPER)))
    {
        pWrappedObjType = TypeHandle(CoreLibBinder::GetClass(CLASS__INT32));
    }
    else if (hndWrapperType == TypeHandle(CoreLibBinder::GetClass(CLASS__CURRENCY_WRAPPER)))
    {
        pWrappedObjType = TypeHandle(CoreLibBinder::GetClass(CLASS__DECIMAL));
    }
    else if (hndWrapperType == TypeHandle(CoreLibBinder::GetClass(CLASS__BSTR_WRAPPER)))
    {
        pWrappedObjType = TypeHandle(g_pStringClass);
    }
    else
    {
        _ASSERTE(!"Invalid wrapper type");
    }

    return pWrappedObjType;
}


TypeHandle OleVariant::GetArrayElementTypeWrapperAware(BASEARRAYREF *pArray)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pArray));
    }
    CONTRACTL_END;

    if (IsArrayOfWrappers(pArray, nullptr))
    {
        return GetWrappedArrayElementType(pArray);
    }
    else
    {
        return (*pArray)->GetArrayElementTypeHandle();
    }
}

TypeHandle OleVariant::GetElementTypeForRecordSafeArray(SAFEARRAY* pSafeArray)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pSafeArray));
    }
    CONTRACTL_END;

    // CoreCLR doesn't support dynamic type mapping.
    COMPlusThrow(kArgumentException, IDS_EE_CANNOT_MAP_TO_MANAGED_VC);
    return TypeHandle(); // Unreachable
}

void OleVariant::ConvertBSTRToString(BSTR bstr, STRINGREF *pStringObj)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(bstr, NULL_OK));
        PRECONDITION(CheckPointer(pStringObj));
    }
    CONTRACTL_END;

    // Initialize the output string object to null to start.
    *pStringObj = NULL;

    // If the BSTR is null then we leave the output string object set to null.
    if (bstr == NULL)
        return;

    UnmanagedCallersOnlyCaller convertToManaged(METHOD__BSTRMARSHALER__CONVERT_TO_MANAGED_UCO);
    convertToManaged.InvokeThrowing((INT_PTR)bstr, pStringObj);
}

BSTR OleVariant::ConvertStringToBSTR(STRINGREF *pStringObj)
{
    CONTRACT(BSTR)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pStringObj));

        // A null BSTR should only be returned if the input string is null.
        POSTCONDITION(RETVAL != NULL || *pStringObj == NULL);
    }
    CONTRACT_END;

    if (*pStringObj == NULL)
    {
        RETURN NULL;
    }

    UnmanagedCallersOnlyCaller convertToNative(METHOD__BSTRMARSHALER__CONVERT_TO_NATIVE_UCO);
    RETURN (BSTR)convertToNative.InvokeThrowing_Ret<INT_PTR>(pStringObj);
}

extern "C" void QCALLTYPE Variant_ConvertValueTypeToRecord(QCall::ObjectHandleOnStack obj, VARIANT * pOle)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;
    GCX_COOP();

    OBJECTREF objRef = obj.Get();
    GCPROTECT_BEGIN(objRef);
    V_VT(pOle) = VT_RECORD;
    OleVariant::ConvertValueClassToVariant(&objRef, pOle);
    GCPROTECT_END();

    END_QCALL;
}

