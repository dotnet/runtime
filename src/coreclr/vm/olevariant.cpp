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

#define NO_MAPPING ((BYTE) -1)

#define GCPROTECT_BEGIN_VARIANTDATA(/*VARIANTDATA*/vd) do {             \
                GCFrame __gcframe(vd.GetObjRefPtr(), 1, FALSE);         \
                /* work around unreachable code warning */              \
                if (true) { DEBUG_ASSURE_NO_RETURN_BEGIN(GCPROTECT);


#define GCPROTECT_END_VARIANTDATA()                                     \
                DEBUG_ASSURE_NO_RETURN_END(GCPROTECT); }                \
                } while(0)


//Mapping from CVType to type handle. Used for conversion between the two internally.
const BinderClassID CVTypeToBinderClassID[] =
{
    CLASS__EMPTY,       //CV_EMPTY
    CLASS__VOID,        //CV_VOID, Changing this to object messes up signature resolution very badly.
    CLASS__BOOLEAN,     //CV_BOOLEAN
    CLASS__CHAR,        //CV_CHAR
    CLASS__SBYTE,       //CV_I1
    CLASS__BYTE,        //CV_U1
    CLASS__INT16,       //CV_I2
    CLASS__UINT16,      //CV_U2
    CLASS__INT32,       //CV_I4
    CLASS__UINT32,      //CV_UI4
    CLASS__INT64,       //CV_I8
    CLASS__UINT64,      //CV_UI8
    CLASS__SINGLE,      //CV_R4
    CLASS__DOUBLE,      //CV_R8
    CLASS__STRING,      //CV_STRING
    CLASS__VOID,        //CV_PTR...We treat this as void
    CLASS__DATE_TIME,   //CV_DATETIME
    CLASS__TIMESPAN,    //CV_TIMESPAN
    CLASS__OBJECT,      //CV_OBJECT
    CLASS__DECIMAL,     //CV_DECIMAL
    CLASS__CURRENCY,    //CV_CURRENCY
    CLASS__OBJECT,      //ENUM...We treat this as OBJECT
    CLASS__MISSING,     //CV_MISSING
    CLASS__NULL,        //CV_NULL
    CLASS__NIL,         //CV_LAST
};

// Use this very carefully.  There is not a direct mapping between
//  CorElementType and CVTypes for a bunch of things.  In this case
//  we return CV_LAST.  You need to check this at the call site.
CVTypes CorElementTypeToCVTypes(CorElementType type)
{
    LIMITED_METHOD_CONTRACT;

    if (type <= ELEMENT_TYPE_STRING)
        return (CVTypes) type;

    if (type == ELEMENT_TYPE_CLASS || type == ELEMENT_TYPE_OBJECT)
        return (CVTypes) ELEMENT_TYPE_CLASS;

    return CV_LAST;
}

/* ------------------------------------------------------------------------- *
 * Mapping routines
 * ------------------------------------------------------------------------- */

VARTYPE OleVariant::GetVarTypeForCVType(CVTypes type)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    static const BYTE map[] =
    {
        VT_EMPTY,           // CV_EMPTY
        VT_VOID,            // CV_VOID
        VT_BOOL,            // CV_BOOLEAN
        VT_UI2,             // CV_CHAR
        VT_I1,              // CV_I1
        VT_UI1,             // CV_U1
        VT_I2,              // CV_I2
        VT_UI2,             // CV_U2
        VT_I4,              // CV_I4
        VT_UI4,             // CV_U4
        VT_I8,              // CV_I8
        VT_UI8,             // CV_U8
        VT_R4,              // CV_R4
        VT_R8,              // CV_R8
        VT_BSTR,            // CV_STRING
        NO_MAPPING,         // CV_PTR
        VT_DATE,            // CV_DATETIME
        NO_MAPPING,         // CV_TIMESPAN
        VT_DISPATCH,        // CV_OBJECT
        VT_DECIMAL,         // CV_DECIMAL
        VT_CY,              // CV_CURRENCY
        VT_I4,              // CV_ENUM
        VT_ERROR,           // CV_MISSING
        VT_NULL             // CV_NULL
    };

    _ASSERTE(type < (CVTypes) (sizeof(map) / sizeof(map[0])));

    VARTYPE vt = VARTYPE(map[type]);

    if (vt == NO_MAPPING)
        COMPlusThrow(kArgumentException, IDS_EE_COM_UNSUPPORTED_SIG);

    return vt;
}

//
// GetCVTypeForVarType returns the COM+ variant type for a given
// VARTYPE.  This is called by the marshaller in the context of
// a function call.
//

CVTypes OleVariant::GetCVTypeForVarType(VARTYPE vt)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    static const BYTE map[] =
    {
        CV_EMPTY,           // VT_EMPTY
        CV_NULL,            // VT_NULL
        CV_I2,              // VT_I2
        CV_I4,              // VT_I4
        CV_R4,              // VT_R4
        CV_R8,              // VT_R8
        CV_DECIMAL,         // VT_CY
        CV_DATETIME,        // VT_DATE
        CV_STRING,          // VT_BSTR
        CV_OBJECT,          // VT_DISPATCH
        CV_I4,              // VT_ERROR
        CV_BOOLEAN,         // VT_BOOL
        NO_MAPPING,         // VT_VARIANT
        CV_OBJECT,          // VT_UNKNOWN
        CV_DECIMAL,         // VT_DECIMAL
        NO_MAPPING,         // unused
        CV_I1,              // VT_I1
        CV_U1,              // VT_UI1
        CV_U2,              // VT_UI2
        CV_U4,              // VT_UI4
        CV_I8,              // VT_I8
        CV_U8,              // VT_UI8
        CV_I4,              // VT_INT
        CV_U4,              // VT_UINT
        CV_VOID,            // VT_VOID
        NO_MAPPING,         // VT_HRESULT
        NO_MAPPING,         // VT_PTR
        NO_MAPPING,         // VT_SAFEARRAY
        NO_MAPPING,         // VT_CARRAY
        NO_MAPPING,         // VT_USERDEFINED
        NO_MAPPING,         // VT_LPSTR
        NO_MAPPING,         // VT_LPWSTR
        NO_MAPPING,         // unused
        NO_MAPPING,         // unused
        NO_MAPPING,         // unused
        NO_MAPPING,         // unused
        CV_OBJECT,          // VT_RECORD
    };

    CVTypes type = CV_LAST;

    // Validate the arguments.
    _ASSERTE((vt & VT_BYREF) == 0);

    // Array's map to CV_OBJECT.
    if (vt & VT_ARRAY)
        return CV_OBJECT;

    // This is prety much a workaround because you cannot cast a CorElementType into a CVTYPE
    if (vt > VT_RECORD || (BYTE)(type = (CVTypes) map[vt]) == NO_MAPPING)
        COMPlusThrow(kArgumentException, IDS_EE_COM_UNSUPPORTED_TYPE);

    return type;
} // CVTypes OleVariant::GetCVTypeForVarType()

#ifdef FEATURE_COMINTEROP

// GetVarTypeForComVariant retusn the VARTYPE for the contents
// of a COM+ variant.
//
VARTYPE OleVariant::GetVarTypeForComVariant(VariantData *pComVariant)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    CVTypes type = pComVariant->GetType();
    VARTYPE vt;

    vt = pComVariant->GetVT();
    if (vt != VT_EMPTY)
    {
        // This variant was originally unmarshaled from unmanaged, and had the original VT recorded in it.
        // We'll always use that over inference.
        return vt;
    }

    if (type == CV_OBJECT)
    {
        OBJECTREF obj = pComVariant->GetObjRef();

        // Null objects will be converted to VT_DISPATCH variants with a null
        // IDispatch pointer.
        if (obj == NULL)
            return VT_DISPATCH;

        // Retrieve the object's method table.
        MethodTable *pMT = obj->GetMethodTable();

        // Handle the value class case.
        if (pMT->IsValueType())
            return VT_RECORD;

        // Handle the array case.
        if (pMT->IsArray())
        {
            vt = GetElementVarTypeForArrayRef((BASEARRAYREF)obj);
            if (vt == VT_ARRAY)
                vt = VT_VARIANT;

            return vt | VT_ARRAY;
        }

#ifdef FEATURE_COMINTEROP
        // SafeHandle's or CriticalHandle's cannot be stored in VARIANT's.
        if (pMT->CanCastToClass(CoreLibBinder::GetClass(CLASS__SAFE_HANDLE)))
            COMPlusThrow(kArgumentException, IDS_EE_SH_IN_VARIANT_NOT_SUPPORTED);
        if (pMT->CanCastToClass(CoreLibBinder::GetClass(CLASS__CRITICAL_HANDLE)))
            COMPlusThrow(kArgumentException, IDS_EE_CH_IN_VARIANT_NOT_SUPPORTED);

        // VariantWrappers cannot be stored in VARIANT's.
        if (CoreLibBinder::IsClass(pMT, CLASS__VARIANT_WRAPPER))
            COMPlusThrow(kArgumentException, IDS_EE_VAR_WRAP_IN_VAR_NOT_SUPPORTED);

        // We are dealing with a normal object (not a wrapper) so we will
        // leave the VT as VT_DISPATCH for now and we will determine the actual
        // VT when we convert the object to a COM IP.
        return VT_DISPATCH;
#else // FEATURE_COMINTEROP
        return VT_UNKNOWN;
#endif  // FEATURE_COMINTEROP
    }

    return GetVarTypeForCVType(type);
}

#endif // FEATURE_COMINTEROP

VARTYPE OleVariant::GetVarTypeForTypeHandle(TypeHandle type)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Handle primitive types.
    CorElementType elemType = type.GetSignatureCorElementType();
    if (elemType <= ELEMENT_TYPE_R8)
        return GetVarTypeForCVType(CorElementTypeToCVTypes(elemType));

    // Types incompatible with interop.
    if (type.IsTypeDesc())
        COMPlusThrow(kArgumentException, IDS_EE_COM_UNSUPPORTED_SIG);

    // Handle objects.
    MethodTable * pMT = type.AsMethodTable();

    if (pMT == g_pStringClass)
        return VT_BSTR;
    if (pMT == g_pObjectClass)
        return VT_VARIANT;

    // We need to make sure the CVClasses table is populated.
    if(CoreLibBinder::IsClass(pMT, CLASS__DATE_TIME))
        return VT_DATE;
    if(CoreLibBinder::IsClass(pMT, CLASS__DECIMAL))
        return VT_DECIMAL;

#ifdef HOST_64BIT
    if (CoreLibBinder::IsClass(pMT, CLASS__INTPTR))
        return VT_I8;
    if (CoreLibBinder::IsClass(pMT, CLASS__UINTPTR))
        return VT_UI8;
#else
    if (CoreLibBinder::IsClass(pMT, CLASS__INTPTR))
        return VT_INT;
    if (CoreLibBinder::IsClass(pMT, CLASS__UINTPTR))
        return VT_UINT;
#endif

#ifdef FEATURE_COMINTEROP
    // The wrapper types are only available when built-in COM is supported.
    if (g_pConfig->IsBuiltInCOMSupported())
    {
        if (CoreLibBinder::IsClass(pMT, CLASS__DISPATCH_WRAPPER))
            return VT_DISPATCH;
        if (CoreLibBinder::IsClass(pMT, CLASS__UNKNOWN_WRAPPER))
            return VT_UNKNOWN;
        if (CoreLibBinder::IsClass(pMT, CLASS__ERROR_WRAPPER))
            return VT_ERROR;
        if (CoreLibBinder::IsClass(pMT, CLASS__CURRENCY_WRAPPER))
            return VT_CY;
        if (CoreLibBinder::IsClass(pMT, CLASS__BSTR_WRAPPER))
            return VT_BSTR;

        // VariantWrappers cannot be stored in VARIANT's.
        if (CoreLibBinder::IsClass(pMT, CLASS__VARIANT_WRAPPER))
            COMPlusThrow(kArgumentException, IDS_EE_COM_UNSUPPORTED_SIG);
    }
#endif // FEATURE_COMINTEROP

    if (pMT->IsEnum())
        return GetVarTypeForCVType((CVTypes)type.GetInternalCorElementType());

    if (pMT->IsValueType())
        return VT_RECORD;

    if (pMT->IsArray())
        return VT_ARRAY;

#ifdef FEATURE_COMINTEROP
    // There is no VT corresponding to SafeHandles as they cannot be stored in
    // VARIANTs or Arrays. The same applies to CriticalHandle.
    if (type.CanCastTo(TypeHandle(CoreLibBinder::GetClass(CLASS__SAFE_HANDLE))))
        COMPlusThrow(kArgumentException, IDS_EE_COM_UNSUPPORTED_SIG);
    if (type.CanCastTo(TypeHandle(CoreLibBinder::GetClass(CLASS__CRITICAL_HANDLE))))
        COMPlusThrow(kArgumentException, IDS_EE_COM_UNSUPPORTED_SIG);

    if (pMT->IsInterface())
    {
        CorIfaceAttr ifaceType = pMT->GetComInterfaceType();
        return static_cast<VARTYPE>(IsDispatchBasedItf(ifaceType) ? VT_DISPATCH : VT_UNKNOWN);
    }

    TypeHandle hndDefItfClass;
    DefaultInterfaceType DefItfType = GetDefaultInterfaceForClassWrapper(type, &hndDefItfClass);
    switch (DefItfType)
    {
        case DefaultInterfaceType_Explicit:
        {
            CorIfaceAttr ifaceType = hndDefItfClass.GetMethodTable()->GetComInterfaceType();
            return static_cast<VARTYPE>(IsDispatchBasedItf(ifaceType) ? VT_DISPATCH : VT_UNKNOWN);
        }

        case DefaultInterfaceType_AutoDual:
        {
            return VT_DISPATCH;
        }

        case DefaultInterfaceType_IUnknown:
        case DefaultInterfaceType_BaseComClass:
        {
            return VT_UNKNOWN;
        }

        case DefaultInterfaceType_AutoDispatch:
        {
            return VT_DISPATCH;
        }

        default:
        {
            _ASSERTE(!"Invalid default interface type!");
        }
    }
#endif // FEATURE_COMINTEROP

    return VT_UNKNOWN;
}

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
    return(GetVarTypeForTypeHandle(elemTypeHnd));
}

#ifdef FEATURE_COMINTEROP

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

#endif // FEATURE_COMINTEROP

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
        case VTHACK_WINBOOL:
        case VTHACK_CBOOL:
            baseElement = ELEMENT_TYPE_BOOLEAN;
            break;

        case VTHACK_ANSICHAR:
            baseElement = ELEMENT_TYPE_CHAR;
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

    // Special cases
    switch (vt)
    {
        case VTHACK_WINBOOL:
            return sizeof(BOOL);
            break;
        case VTHACK_ANSICHAR:
            return GetMaxDBCSCharByteSize();  // Multi byte characters.
            break;
        case VTHACK_CBOOL:
            return sizeof(BYTE);
        default:
            break;
    }

    // VT_ARRAY indicates a safe array which is always sizeof(SAFEARRAY *).
    if (vt & VT_ARRAY)
        return sizeof(SAFEARRAY*);

    if (vt == VTHACK_NONBLITTABLERECORD || vt == VTHACK_BLITTABLERECORD || vt == VT_RECORD)
    {
        PREFIX_ASSUME(pInterfaceMT != NULL);
        return pInterfaceMT->GetNativeSize();
    }
    else if (vt > VT_LPWSTR)
        return 0;
    else
        return map[vt];
}

//
// GetElementSizeForVarType returns the a MethodTable* to a type that it blittable to the native
// element representation, or pManagedMT if vt represents a record (user-defined type).
//

MethodTable* OleVariant::GetNativeMethodTableForVarType(VARTYPE vt, MethodTable* pManagedMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (vt & VT_ARRAY)
    {
        return CoreLibBinder::GetClass(CLASS__INTPTR);
    }

    switch (vt)
    {
        case VT_DATE:
            return CoreLibBinder::GetClass(CLASS__DOUBLE);
        case VT_CY:
            return CoreLibBinder::GetClass(CLASS__CURRENCY);
        case VTHACK_WINBOOL:
            return CoreLibBinder::GetClass(CLASS__INT32);
        case VT_BOOL:
            return CoreLibBinder::GetClass(CLASS__INT16);
        case VTHACK_CBOOL:
            return CoreLibBinder::GetClass(CLASS__BYTE);
        case VT_DISPATCH:
        case VT_UNKNOWN:
        case VT_LPSTR:
        case VT_LPWSTR:
        case VT_BSTR:
        case VT_USERDEFINED:
        case VT_SAFEARRAY:
        case VT_CARRAY:
            return CoreLibBinder::GetClass(CLASS__INTPTR);
        case VT_VARIANT:
            return CoreLibBinder::GetClass(CLASS__NATIVEVARIANT);
        case VTHACK_ANSICHAR:
            return CoreLibBinder::GetClass(CLASS__BYTE);
        case VT_UI2:
            // When CharSet = CharSet.Unicode, System.Char arrays are marshaled as VT_UI2.
            // However, since System.Char itself is CharSet.Ansi, the native size of
            // System.Char is 1 byte instead of 2. So here we explicitly return System.UInt16's
            // MethodTable to ensure the correct size.
            return CoreLibBinder::GetClass(CLASS__UINT16);
        case VT_DECIMAL:
            return CoreLibBinder::GetClass(CLASS__NATIVEDECIMAL);
        default:
            PREFIX_ASSUME(pManagedMT != NULL);
            return pManagedMT;
    }
}

//
// GetMarshalerForVarType returns the marshaler for the
// given VARTYPE.
//

const OleVariant::Marshaler *OleVariant::GetMarshalerForVarType(VARTYPE vt, BOOL fThrow)
{
    CONTRACT (const OleVariant::Marshaler*)
    {
        if (fThrow) THROWS; else NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

#ifdef FEATURE_COMINTEROP

#define RETURN_MARSHALER(OleToCom, ComToOle, OleRefToCom, ArrayOleToCom, ArrayComToOle, ClearArray) \
    { static const Marshaler marshaler = { OleToCom, ComToOle, OleRefToCom, ArrayOleToCom, ArrayComToOle, ClearArray }; RETURN &marshaler; }

#else // FEATURE_COMINTEROP

#define RETURN_MARSHALER(OleToCom, ComToOle, OleRefToCom, ArrayOleToCom, ArrayComToOle, ClearArray) \
    { static const Marshaler marshaler = { ArrayOleToCom, ArrayComToOle, ClearArray }; RETURN &marshaler; }

#endif // FEATURE_COMINTEROP

#ifdef FEATURE_COMINTEROP
    if (vt & VT_ARRAY)
    {
VariantArray:
        RETURN_MARSHALER(
            MarshalArrayVariantOleToCom,
            MarshalArrayVariantComToOle,
            MarshalArrayVariantOleRefToCom,
            NULL,
            NULL,
            ClearVariantArray
        );
    }
#endif // FEATURE_COMINTEROP

    switch (vt)
    {
    case VT_BOOL:
        RETURN_MARSHALER(
            MarshalBoolVariantOleToCom,
            NULL,
            NULL,
            MarshalBoolArrayOleToCom,
            MarshalBoolArrayComToOle,
            NULL
        );

    case VT_DATE:
        RETURN_MARSHALER(
            MarshalDateVariantOleToCom,
            MarshalDateVariantComToOle,
            MarshalDateVariantOleRefToCom,
            MarshalDateArrayOleToCom,
            MarshalDateArrayComToOle,
            NULL
        );

    case VT_DECIMAL:
        RETURN_MARSHALER(
            MarshalDecimalVariantOleToCom,
            MarshalDecimalVariantComToOle,
            MarshalDecimalVariantOleRefToCom,
            NULL, NULL, NULL
        );

#ifdef FEATURE_COMINTEROP
    case VT_CY:
        RETURN_MARSHALER(
            MarshalCurrencyVariantOleToCom,
            MarshalCurrencyVariantComToOle,
            MarshalCurrencyVariantOleRefToCom,
            MarshalCurrencyArrayOleToCom,
            MarshalCurrencyArrayComToOle,
            NULL
        );

    case VT_BSTR:
        RETURN_MARSHALER(
            MarshalBSTRVariantOleToCom,
            MarshalBSTRVariantComToOle,
            NULL,
            MarshalBSTRArrayOleToCom,
            MarshalBSTRArrayComToOle,
            ClearBSTRArray
        );

    case VT_UNKNOWN:
        RETURN_MARSHALER(
            MarshalInterfaceVariantOleToCom,
            MarshalInterfaceVariantComToOle,
            MarshalInterfaceVariantOleRefToCom,
            MarshalInterfaceArrayOleToCom,
            MarshalIUnknownArrayComToOle,
            ClearInterfaceArray
        );

    case VT_DISPATCH:
        RETURN_MARSHALER(
            MarshalInterfaceVariantOleToCom,
            MarshalInterfaceVariantComToOle,
            MarshalInterfaceVariantOleRefToCom,
            MarshalInterfaceArrayOleToCom,
            MarshalIDispatchArrayComToOle,
            ClearInterfaceArray
        );

    case VT_SAFEARRAY:
        goto VariantArray;

    case VT_VARIANT:
        RETURN_MARSHALER(
            NULL, NULL, NULL,
            MarshalVariantArrayOleToCom,
            MarshalVariantArrayComToOle,
            ClearVariantArray
        );

    case VT_ERROR:
        RETURN_MARSHALER(
            MarshalErrorVariantOleToCom,
            MarshalErrorVariantComToOle,
            MarshalErrorVariantOleRefToCom,
            NULL, NULL, NULL
        );
#endif // FEATURE_COMINTEROP

    case VTHACK_NONBLITTABLERECORD:
        RETURN_MARSHALER(
            NULL, NULL, NULL,
            MarshalNonBlittableRecordArrayOleToCom,
            MarshalNonBlittableRecordArrayComToOle,
            ClearNonBlittableRecordArray
        );

    case VTHACK_BLITTABLERECORD:
        RETURN NULL; // Requires no marshaling

    case VTHACK_WINBOOL:
        RETURN_MARSHALER(
            MarshalWinBoolVariantOleToCom,
            MarshalWinBoolVariantComToOle,
            MarshalWinBoolVariantOleRefToCom,
            MarshalWinBoolArrayOleToCom,
            MarshalWinBoolArrayComToOle,
            NULL
        );

    case VTHACK_CBOOL:
        RETURN_MARSHALER(
            MarshalCBoolVariantOleToCom,
            MarshalCBoolVariantComToOle,
            MarshalCBoolVariantOleRefToCom,
            MarshalCBoolArrayOleToCom,
            MarshalCBoolArrayComToOle,
            NULL
        );

    case VTHACK_ANSICHAR:
        RETURN_MARSHALER(
            MarshalAnsiCharVariantOleToCom,
            MarshalAnsiCharVariantComToOle,
            MarshalAnsiCharVariantOleRefToCom,
            MarshalAnsiCharArrayOleToCom,
            MarshalAnsiCharArrayComToOle,
            NULL
        );

    case VT_LPSTR:
        RETURN_MARSHALER(
            NULL, NULL, NULL,
            MarshalLPSTRArrayOleToCom,
            MarshalLPSTRRArrayComToOle,
            ClearLPSTRArray
        );

    case VT_LPWSTR:
        RETURN_MARSHALER(
            NULL, NULL, NULL,
            MarshalLPWSTRArrayOleToCom,
            MarshalLPWSTRRArrayComToOle,
            ClearLPWSTRArray
        );

    case VT_RECORD:
#ifdef FEATURE_COMINTEROP
        RETURN_MARSHALER(
            MarshalRecordVariantOleToCom,
            MarshalRecordVariantComToOle,
            MarshalRecordVariantOleRefToCom,
            MarshalRecordArrayOleToCom,
            MarshalRecordArrayComToOle,
            ClearRecordArray
        );
#else
        RETURN_MARSHALER(
            NULL, NULL, NULL,
            MarshalRecordArrayOleToCom,
            MarshalRecordArrayComToOle,
            ClearRecordArray
        );
#endif // FEATURE_COMINTEROP

    case VT_CARRAY:
    case VT_USERDEFINED:
        if (fThrow)
        {
            COMPlusThrow(kArgumentException, IDS_EE_COM_UNSUPPORTED_SIG);
        }
        else
        {
            RETURN NULL;
        }

    default:
        RETURN NULL;
    }
} // OleVariant::Marshaler *OleVariant::GetMarshalerForVarType()


#ifdef FEATURE_COMINTEROP

/*==================================NewVariant==================================
**N.B.:  This method does a GC Allocation.  Any method calling it is required to
**       GC_PROTECT the OBJECTREF.
**
**Actions:  Allocates a new Variant and fills it with the appropriate data.
**Returns:  A new Variant with all of the appropriate fields filled out.
**Exceptions: OutOfMemoryError if v can't be allocated.
==============================================================================*/
void VariantData::NewVariant(VariantData * const& dest, const CVTypes type, INT64 data
                            DEBUG_ARG(BOOL bDestIsInterior))
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        // Don't pass an object in for Empty.
        PRECONDITION(CheckPointer(dest));
        PRECONDITION((bDestIsInterior && IsProtectedByGCFrame ((OBJECTREF *) &dest))
            || (!bDestIsInterior && IsProtectedByGCFrame (dest->GetObjRefPtr ())));
        PRECONDITION((type == CV_EMPTY) || (type == CV_NULL) || (type == CV_U4) || (type == CV_U8));
    }
    CONTRACTL_END;

    //If both arguments are null or both are specified, we're in an illegal situation.  Bail.
    //If all three are null, we're creating an empty variant
    if ( (type != CV_EMPTY) && (type != CV_NULL) && (type != CV_U4) && (type != CV_U8) )
    {
        COMPlusThrow(kArgumentException);
    }

    //Fill in the data.
    dest->SetType(type);

    switch (type)
    {
        case CV_U4:
            dest->SetObjRef(NULL);
            dest->SetDataAsUInt32((UINT32)data);
            break;

        case CV_U8:
            dest->SetObjRef(NULL);
            dest->SetDataAsInt64(data);
            break;

        case CV_NULL:
        {
            FieldDesc * pFD = CoreLibBinder::GetField(FIELD__NULL__VALUE);
            _ASSERTE(pFD);

            pFD->CheckRunClassInitThrowing();

            OBJECTREF obj = pFD->GetStaticOBJECTREF();
            _ASSERTE(obj!=NULL);

            dest->SetObjRef(obj);
            dest->SetDataAsInt64(0);
            break;
        }

        case CV_EMPTY:
        {
            dest->SetObjRef(NULL);
            break;
        }

        default:
            // Did you add any new CVTypes?
            COMPlusThrow(kNotSupportedException, W("Arg_InvalidOleVariantTypeException"));
    }
}

void SafeVariantClearHelper(_Inout_ VARIANT* pVar)
{
    WRAPPER_NO_CONTRACT;

    VariantClear(pVar);
}

class OutOfMemoryException;

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
        SCAN_EHMARKER();
        PAL_CPP_TRY
        {
            // These are holders to tell the contract system that we're catching all exceptions.
            SCAN_EHMARKER_TRY();
            CLR_TRY_MARKER();

            // Most of time, oleaut32.dll is loaded already when we get here.
            // Sometimes, CLR initializes Variant without loading oleaut32.dll, e.g. VT_BOOL.
            // It is better for performance with EX_TRY than

            SafeVariantClearHelper(pVar);

            SCAN_EHMARKER_END_TRY();
        }
#pragma warning(suppress: 4101)
        PAL_CPP_CATCH_DERIVED(OutOfMemoryException, obj)
        {
            SCAN_EHMARKER_CATCH();

#if defined(STACK_GUARDS_DEBUG)
            // Catching and just swallowing an exception means we need to tell
            // the SO code that it should go back to normal operation, as it
            // currently thinks that the exception is still on the fly.
            GetThread()->GetCurrentStackGuard()->RestoreCurrentGuard();
#endif

            SCAN_EHMARKER_END_CATCH();
        }
        PAL_CPP_ENDTRY;

        FillMemory(pVar, sizeof(VARIANT), 0x00);
    }
}

FORCEINLINE void EmptyVariant(VARIANT* value)
{
    WRAPPER_NO_CONTRACT;
    SafeVariantClear(value);
}

class VariantEmptyHolder : public Wrapper<VARIANT*, ::DoNothing<VARIANT*>, EmptyVariant, NULL>
{
public:
    VariantEmptyHolder(VARIANT* p = NULL) :
        Wrapper<VARIANT*, ::DoNothing<VARIANT*>, EmptyVariant, NULL>(p)
    {
        WRAPPER_NO_CONTRACT;
    }

    FORCEINLINE void operator=(VARIANT* p)
    {
        WRAPPER_NO_CONTRACT;

        Wrapper<VARIANT*, ::DoNothing<VARIANT*>, EmptyVariant, NULL>::operator=(p);
    }
};

FORCEINLINE void RecordVariantRelease(VARIANT* value)
{
    if (value)
    {
        WRAPPER_NO_CONTRACT;

        if (V_RECORD(value))
            V_RECORDINFO(value)->RecordDestroy(V_RECORD(value));
        if (V_RECORDINFO(value))
            V_RECORDINFO(value)->Release();
    }
}

class RecordVariantHolder : public Wrapper<VARIANT*, ::DoNothing<VARIANT*>, RecordVariantRelease, NULL>
{
public:
    RecordVariantHolder(VARIANT* p = NULL)
        : Wrapper<VARIANT*, ::DoNothing<VARIANT*>, RecordVariantRelease, NULL>(p)
    {
        WRAPPER_NO_CONTRACT;
    }

    FORCEINLINE void operator=(VARIANT* p)
    {
        WRAPPER_NO_CONTRACT;
        Wrapper<VARIANT*, ::DoNothing<VARIANT*>, RecordVariantRelease, NULL>::operator=(p);
    }
};
#endif  // FEATURE_COMINTEROP

/* ------------------------------------------------------------------------- *
 * Boolean marshaling routines
 * ------------------------------------------------------------------------- */

#ifdef FEATURE_COMINTEROP

void OleVariant::MarshalBoolVariantOleToCom(VARIANT *pOleVariant,
                                            VariantData *pComVariant)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pComVariant));
        PRECONDITION(CheckPointer(pOleVariant));
    }
    CONTRACTL_END;

    *(INT64*)pComVariant->GetData() = V_BOOL(pOleVariant) ? 1 : 0;
}

#endif // FEATURE_COMINTEROP

void OleVariant::MarshalBoolArrayOleToCom(void *oleArray, BASEARRAYREF *pComArray,
                                          MethodTable *pInterfaceMT, PCODE pManagedMarshalerCode)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(oleArray));
        PRECONDITION(CheckPointer(pComArray));
    }
    CONTRACTL_END;

    ASSERT_PROTECTED(pComArray);

    SIZE_T elementCount = (*pComArray)->GetNumComponents();

    VARIANT_BOOL *pOle = (VARIANT_BOOL *) oleArray;
    VARIANT_BOOL *pOleEnd = pOle + elementCount;

    UCHAR *pCom = (UCHAR *) (*pComArray)->GetDataPtr();

    while (pOle < pOleEnd)
    {
        static_assert_no_msg(sizeof(VARIANT_BOOL) == sizeof(UINT16));
        (*(pCom++)) = MAYBE_UNALIGNED_READ(pOle, 16) ? 1 : 0;
        pOle++;
    }
}

void OleVariant::MarshalBoolArrayComToOle(BASEARRAYREF *pComArray, void *oleArray,
                                          MethodTable *pInterfaceMT, BOOL fBestFitMapping,
                                          BOOL fThrowOnUnmappableChar,
                                          BOOL fOleArrayIsValid, SIZE_T cElements,
                                          PCODE pManagedMarshalerCode)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(oleArray));
        PRECONDITION(CheckPointer(pComArray));
    }
    CONTRACTL_END;

    ASSERT_PROTECTED(pComArray);

    VARIANT_BOOL *pOle = (VARIANT_BOOL *) oleArray;
    VARIANT_BOOL *pOleEnd = pOle + cElements;

    UCHAR *pCom = (UCHAR *) (*pComArray)->GetDataPtr();

    while (pOle < pOleEnd)
    {
        static_assert_no_msg(sizeof(VARIANT_BOOL) == sizeof(UINT16));
        MAYBE_UNALIGNED_WRITE(pOle, 16, *pCom ? VARIANT_TRUE : VARIANT_FALSE);
        pOle++; pCom++;
    }
}

/* ------------------------------------------------------------------------- *
 * WinBoolean marshaling routines
 * ------------------------------------------------------------------------- */

#ifdef FEATURE_COMINTEROP
void OleVariant::MarshalWinBoolVariantOleToCom(VARIANT *pOleVariant,
                                            VariantData *pComVariant)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(!"Not supposed to get here.");
}

void OleVariant::MarshalWinBoolVariantComToOle(VariantData *pComVariant,
                                            VARIANT *pOleVariant)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(!"Not supposed to get here.");
}

void OleVariant::MarshalWinBoolVariantOleRefToCom(VARIANT *pOleVariant,
                                            VariantData *pComVariant)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(!"Not supposed to get here.");
}
#endif // FEATURE_COMINTEROP

void OleVariant::MarshalWinBoolArrayOleToCom(void *oleArray, BASEARRAYREF *pComArray,
                                          MethodTable *pInterfaceMT, PCODE pManagedMarshalerCode)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(oleArray));
        PRECONDITION(CheckPointer(pComArray));
    }
    CONTRACTL_END;

    ASSERT_PROTECTED(pComArray);

    SIZE_T elementCount = (*pComArray)->GetNumComponents();

    BOOL *pOle = (BOOL *) oleArray;
    BOOL *pOleEnd = pOle + elementCount;

    UCHAR *pCom = (UCHAR *) (*pComArray)->GetDataPtr();

    while (pOle < pOleEnd)
    {
        static_assert_no_msg(sizeof(BOOL) == sizeof(UINT32));
        (*(pCom++)) = MAYBE_UNALIGNED_READ(pOle, 32) ? 1 : 0;
        pOle++;
    }
}

void OleVariant::MarshalWinBoolArrayComToOle(BASEARRAYREF *pComArray, void *oleArray,
                                          MethodTable *pInterfaceMT, BOOL fBestFitMapping,
                                          BOOL fThrowOnUnmappableChar,
                                          BOOL fOleArrayIsValid, SIZE_T cElements,
                                          PCODE pManagedMarshalerCode)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(oleArray));
        PRECONDITION(CheckPointer(pComArray));
    }
    CONTRACTL_END;

    ASSERT_PROTECTED(pComArray);

    BOOL *pOle = (BOOL *) oleArray;
    BOOL *pOleEnd = pOle + cElements;

    UCHAR *pCom = (UCHAR *) (*pComArray)->GetDataPtr();

    while (pOle < pOleEnd)
    {
        static_assert_no_msg(sizeof(BOOL) == sizeof(UINT32));
        MAYBE_UNALIGNED_WRITE(pOle, 32, *pCom ? 1 : 0);
        pOle++; pCom++;
    }
}

/* ------------------------------------------------------------------------- *
 * CBool marshaling routines
 * ------------------------------------------------------------------------- */

#ifdef FEATURE_COMINTEROP
void OleVariant::MarshalCBoolVariantOleToCom(VARIANT* pOleVariant, VariantData* pComVariant)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(!"Not supposed to get here.");
}

void OleVariant::MarshalCBoolVariantComToOle(VariantData* pComVariant, VARIANT* pOleVariant)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(!"Not supposed to get here.");
}

void OleVariant::MarshalCBoolVariantOleRefToCom(VARIANT* pOleVariant, VariantData* pComVariant)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(!"Not supposed to get here.");
}
#endif // FEATURE_COMINTEROP

void OleVariant::MarshalCBoolArrayOleToCom(void* oleArray, BASEARRAYREF* pComArray,
                                        MethodTable* pInterfaceMT, PCODE pManagedMarshalerCode)
{
    LIMITED_METHOD_CONTRACT;

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(oleArray));
        PRECONDITION(CheckPointer(pComArray));
    }
    CONTRACTL_END;

    ASSERT_PROTECTED(pComArray);

    _ASSERTE((*pComArray)->GetArrayElementType() == ELEMENT_TYPE_BOOLEAN);

    SIZE_T cbArray = (*pComArray)->GetNumComponents();

    BYTE *pOle = (BYTE *) oleArray;
    BYTE *pOleEnd = pOle + cbArray;

    UCHAR *pCom = (UCHAR *) (*pComArray)->GetDataPtr();

    while (pOle < pOleEnd)
    {
        (*pCom) = (*pOle ? 1 : 0);
        pOle++; pCom++;
    }
}

void OleVariant::MarshalCBoolArrayComToOle(BASEARRAYREF* pComArray, void* oleArray,
                                        MethodTable* pInterfaceMT, BOOL fBestFitMapping,
                                        BOOL fThrowOnUnmappableChar, BOOL fOleArrayIsValid,
                                        SIZE_T cElements,
                                        PCODE pManagedMarshalerCode)
{
    LIMITED_METHOD_CONTRACT;

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(oleArray));
        PRECONDITION(CheckPointer(pComArray));
    }
    CONTRACTL_END;

    ASSERT_PROTECTED(pComArray);

    _ASSERTE((*pComArray)->GetArrayElementType() == ELEMENT_TYPE_BOOLEAN);

    BYTE *pOle = (BYTE *) oleArray;
    BYTE *pOleEnd = pOle + cElements;

    UCHAR *pCom = (UCHAR *) (*pComArray)->GetDataPtr();

    while (pOle < pOleEnd)
    {
        *pOle = (*pCom ? 1 : 0);
        pOle++; pCom++;
    }
}

/* ------------------------------------------------------------------------- *
 * Ansi char marshaling routines
 * ------------------------------------------------------------------------- */

#ifdef FEATURE_COMINTEROP
void OleVariant::MarshalAnsiCharVariantOleToCom(VARIANT *pOleVariant,
                                            VariantData *pComVariant)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(!"Not supposed to get here.");
}

void OleVariant::MarshalAnsiCharVariantComToOle(VariantData *pComVariant,
                                            VARIANT *pOleVariant)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(!"Not supposed to get here.");
}

void OleVariant::MarshalAnsiCharVariantOleRefToCom(VARIANT *pOleVariant,
                                            VariantData *pComVariant)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(!"Not supposed to get here.");
}
#endif // FEATURE_COMINTEROP

void OleVariant::MarshalAnsiCharArrayOleToCom(void *oleArray, BASEARRAYREF *pComArray,
                                          MethodTable *pInterfaceMT, PCODE pManagedMarshalerCode)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(oleArray));
        PRECONDITION(CheckPointer(pComArray));
    }
    CONTRACTL_END;

    ASSERT_PROTECTED(pComArray);

    SIZE_T elementCount = (*pComArray)->GetNumComponents();

    WCHAR *pCom = (WCHAR *) (*pComArray)->GetDataPtr();

    if (0 == elementCount)
    {
        *pCom = '\0';
        return;
    }

    if (0 == MultiByteToWideChar(CP_ACP,
                        MB_PRECOMPOSED,
                        (const CHAR *)oleArray,
                        (int)elementCount,
                        pCom,
                        (int)elementCount))
    {
        COMPlusThrowWin32();
    }
}

void OleVariant::MarshalAnsiCharArrayComToOle(BASEARRAYREF *pComArray, void *oleArray,
                                          MethodTable *pInterfaceMT, BOOL fBestFitMapping,
                                          BOOL fThrowOnUnmappableChar, BOOL fOleArrayIsValid,
                                          SIZE_T cElements, PCODE pManagedMarshalerCode)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(oleArray));
        PRECONDITION(CheckPointer(pComArray));
    }
    CONTRACTL_END;

    const WCHAR *pCom = (const WCHAR *) (*pComArray)->GetDataPtr();

    if (!FitsIn<int>(cElements))
        COMPlusThrowHR(COR_E_OVERFLOW);

    int cchCount = (int)cElements;
    int cbBuffer;

    if (!ClrSafeInt<int>::multiply(cchCount, GetMaxDBCSCharByteSize(), cbBuffer))
        COMPlusThrowHR(COR_E_OVERFLOW);

    InternalWideToAnsi((WCHAR*)pCom, cchCount, (CHAR*)oleArray, cbBuffer,
                        fBestFitMapping, fThrowOnUnmappableChar);
}

/* ------------------------------------------------------------------------- *
 * Interface marshaling routines
 * ------------------------------------------------------------------------- */

#ifdef FEATURE_COMINTEROP
void OleVariant::MarshalInterfaceVariantOleToCom(VARIANT *pOleVariant,
                                                 VariantData *pComVariant)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pOleVariant));
        PRECONDITION(CheckPointer(pComVariant));
    }
    CONTRACTL_END;

    IUnknown *unk = V_UNKNOWN(pOleVariant);

    OBJECTREF obj = NULL;
    if (unk != NULL)
    {
        GCPROTECT_BEGIN(obj);
        GetObjectRefFromComIP(&obj, V_UNKNOWN(pOleVariant));
        GCPROTECT_END();
    }

    pComVariant->SetObjRef(obj);
}

void OleVariant::MarshalInterfaceVariantComToOle(VariantData *pComVariant,
                                                 VARIANT *pOleVariant)

{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pOleVariant));
        PRECONDITION(CheckPointer(pComVariant));
    }
    CONTRACTL_END;

    OBJECTREF *obj = pComVariant->GetObjRefPtr();
    VARTYPE vt = pComVariant->GetVT();

    ASSERT_PROTECTED(obj);

    if (*obj == NULL)
    {
        // If there is no VT set in the managed variant, then default to VT_UNKNOWN.
        if (vt == VT_EMPTY)
            vt = VT_UNKNOWN;

        V_UNKNOWN(pOleVariant) = NULL;
        V_VT(pOleVariant) = vt;
    }
    else
    {
#ifdef FEATURE_COMINTEROP
        ComIpType FetchedIpType = ComIpType_None;
        ComIpType ReqIpType;

        if (vt != VT_EMPTY)
        {
            // We are dealing with an UnknownWrapper or DispatchWrapper.
            // In this case, we need to respect the VT.
            _ASSERTE(vt == VT_DISPATCH || vt == VT_UNKNOWN);
            ReqIpType = vt == VT_DISPATCH ? ComIpType_Dispatch : ComIpType_Unknown;
        }
        else
        {
            // We are dealing with a normal object so we can give either
            // IDispatch or IUnknown out depending on what it supports.
            ReqIpType = ComIpType_Both;
        }

        IUnknown *unk = GetComIPFromObjectRef(obj, ReqIpType, &FetchedIpType);
        BOOL ItfIsDispatch = FetchedIpType == ComIpType_Dispatch;

        V_UNKNOWN(pOleVariant) = unk;
        V_VT(pOleVariant) = static_cast<VARTYPE>(ItfIsDispatch ? VT_DISPATCH : VT_UNKNOWN);
#else // FEATURE_COMINTEROP
        V_UNKNOWN(pOleVariant) = GetComIPFromObjectRef(obj);
        V_VT(pOleVariant) = VT_UNKNOWN;
#endif // FEATURE_COMINTEROP
    }
}

void OleVariant::MarshalInterfaceVariantOleRefToCom(VARIANT *pOleVariant,
                                                 VariantData *pComVariant)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pOleVariant));
        PRECONDITION(CheckPointer(pComVariant));
    }
    CONTRACTL_END;

    IUnknown *unk = V_UNKNOWN(pOleVariant);

    OBJECTREF obj = NULL;
    if (unk != NULL)
    {
        GCPROTECT_BEGIN(obj);
        GetObjectRefFromComIP(&obj, *V_UNKNOWNREF(pOleVariant));
        GCPROTECT_END();
    }

    pComVariant->SetObjRef(obj);
}

void OleVariant::MarshalInterfaceArrayOleToCom(void *oleArray, BASEARRAYREF *pComArray,
                                               MethodTable *pElementMT, PCODE pManagedMarshalerCode)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(oleArray));
        PRECONDITION(CheckPointer(pComArray));
    }
    CONTRACTL_END;

    ASSERT_PROTECTED(pComArray);

    SIZE_T elementCount = (*pComArray)->GetNumComponents();

    IUnknown **pOle = (IUnknown **) oleArray;
    IUnknown **pOleEnd = pOle + elementCount;

    BASEARRAYREF unprotectedArray = *pComArray;
    OBJECTREF *pCom = (OBJECTREF *) unprotectedArray->GetDataPtr();

    OBJECTREF obj = NULL;
    GCPROTECT_BEGIN(obj)
    {
        while (pOle < pOleEnd)
        {
            IUnknown *unk = *pOle++;

            if (unk == NULL)
                obj = NULL;
            else
                GetObjectRefFromComIP(&obj, unk);

            //
            // Make sure the object can be cast to the destination type.
            //

            if (pElementMT != NULL && !CanCastComObject(obj, pElementMT))
            {
                StackSString ssObjClsName;
                StackSString ssDestClsName;
                obj->GetMethodTable()->_GetFullyQualifiedNameForClass(ssObjClsName);
                pElementMT->_GetFullyQualifiedNameForClass(ssDestClsName);
                COMPlusThrow(kInvalidCastException, IDS_EE_CANNOTCAST,
                             ssObjClsName.GetUnicode(), ssDestClsName.GetUnicode());
            }

            //
            // Reset pCom pointer only if array object has moved, rather than
            // recomputing every time through the loop.  Beware implicit calls to
            // ValidateObject inside OBJECTREF methods.
            //

            if (*(void **)&unprotectedArray != *(void **)&*pComArray)
            {
                SIZE_T currentOffset = ((BYTE *)pCom) - (*(Object **) &unprotectedArray)->GetAddress();
                unprotectedArray = *pComArray;
                pCom = (OBJECTREF *) (unprotectedArray->GetAddress() + currentOffset);
            }

            SetObjectReference(pCom++, obj);
        }
    }
    GCPROTECT_END();
}

void OleVariant::MarshalIUnknownArrayComToOle(BASEARRAYREF *pComArray, void *oleArray,
                                              MethodTable *pElementMT, BOOL fBestFitMapping,
                                              BOOL fThrowOnUnmappableChar,
                                              BOOL fOleArrayIsValid, SIZE_T cElements, PCODE pManagedMarshalerCode)
{
    WRAPPER_NO_CONTRACT;

    MarshalInterfaceArrayComToOleHelper(pComArray, oleArray, pElementMT, FALSE, cElements);
}

void OleVariant::ClearInterfaceArray(void *oleArray, SIZE_T cElements, MethodTable *pInterfaceMT, PCODE pManagedMarshalerCode)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(oleArray));
    }
    CONTRACTL_END;

    IUnknown **pOle = (IUnknown **) oleArray;
    IUnknown **pOleEnd = pOle + cElements;

    GCX_PREEMP();
    while (pOle < pOleEnd)
    {
        IUnknown *pUnk = *pOle++;

        if (pUnk != NULL)
        {
            ULONG cbRef = SafeReleasePreemp(pUnk);
            LogInteropRelease(pUnk, cbRef, "VariantClearInterfacArray");
        }
    }
}


/* ------------------------------------------------------------------------- *
 * BSTR marshaling routines
 * ------------------------------------------------------------------------- */

void OleVariant::MarshalBSTRVariantOleToCom(VARIANT *pOleVariant,
                                            VariantData *pComVariant)
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_TRIGGERS);
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pOleVariant));
        PRECONDITION(CheckPointer(pComVariant));
    }
    CONTRACTL_END;

    BSTR bstr = V_BSTR(pOleVariant);

    STRINGREF stringObj = NULL;
    GCPROTECT_BEGIN(stringObj)
    {
        ConvertBSTRToString(bstr, &stringObj);
        pComVariant->SetObjRef((OBJECTREF) stringObj);
    }
    GCPROTECT_END();

    pComVariant->SetObjRef((OBJECTREF) stringObj);
}

void OleVariant::MarshalBSTRVariantComToOle(VariantData *pComVariant,
                                            VARIANT *pOleVariant)
{
    CONTRACTL
    {
        THROWS;
        WRAPPER(GC_TRIGGERS);
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pOleVariant));
        PRECONDITION(CheckPointer(pComVariant));
    }
    CONTRACTL_END;

    STRINGREF stringObj = (STRINGREF) pComVariant->GetObjRef();
    GCPROTECT_BEGIN(stringObj)
    {
        V_BSTR(pOleVariant) = ConvertStringToBSTR(&stringObj);
    }
    GCPROTECT_END();
}

void OleVariant::MarshalBSTRArrayOleToCom(void *oleArray, BASEARRAYREF *pComArray,
                                          MethodTable *pInterfaceMT, PCODE pManagedMarshalerCode)
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_TRIGGERS);
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(oleArray));
        PRECONDITION(CheckPointer(pComArray));
    }
    CONTRACTL_END;

    STRINGREF stringObj = NULL;
    GCPROTECT_BEGIN(stringObj)
    {
    ASSERT_PROTECTED(pComArray);
    SIZE_T elementCount = (*pComArray)->GetNumComponents();

    BSTR *pOle = (BSTR *) oleArray;
    BSTR *pOleEnd = pOle + elementCount;

    BASEARRAYREF unprotectedArray = *pComArray;
    STRINGREF *pCom = (STRINGREF *) unprotectedArray->GetDataPtr();

    while (pOle < pOleEnd)
    {
        BSTR bstr = *pOle++;

            ConvertBSTRToString(bstr, &stringObj);

        //
        // Reset pCom pointer only if array object has moved, rather than
        // recomputing it every time through the loop.  Beware implicit calls to
        // ValidateObject inside OBJECTREF methods.
        //

        if (*(void **)&unprotectedArray != *(void **)&*pComArray)
        {
            SIZE_T currentOffset = ((BYTE *)pCom) - (*(Object **) &unprotectedArray)->GetAddress();
            unprotectedArray = *pComArray;
            pCom = (STRINGREF *) (unprotectedArray->GetAddress() + currentOffset);
        }

            SetObjectReference((OBJECTREF*) pCom++, (OBJECTREF) stringObj);
        }
    }
    GCPROTECT_END();
}

void OleVariant::MarshalBSTRArrayComToOle(BASEARRAYREF *pComArray, void *oleArray,
                                          MethodTable *pInterfaceMT, BOOL fBestFitMapping,
                                          BOOL fThrowOnUnmappableChar,
                                          BOOL fOleArrayIsValid, SIZE_T cElements, PCODE pManagedMarshalerCode)
{
    CONTRACTL
    {
        THROWS;
        WRAPPER(GC_TRIGGERS);
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(oleArray));
        PRECONDITION(CheckPointer(pComArray));
    }
    CONTRACTL_END;

    STRINGREF stringObj = NULL;
    GCPROTECT_BEGIN(stringObj)
    {
    ASSERT_PROTECTED(pComArray);

    BSTR *pOle = (BSTR *) oleArray;
    BSTR *pOleEnd = pOle + cElements;

    STRINGREF *pCom = (STRINGREF *) (*pComArray)->GetDataPtr();

    while (pOle < pOleEnd)
    {
            stringObj = *pCom++;
            BSTR bstr = ConvertStringToBSTR(&stringObj);

        //
        // We aren't calling anything which might cause a GC, so don't worry about
        // the array moving here.
        //

            *pOle++ = bstr;
        }
    }
    GCPROTECT_END();
}

void OleVariant::ClearBSTRArray(void *oleArray, SIZE_T cElements, MethodTable *pInterfaceMT, PCODE pManagedMarshalerCode)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(oleArray));
    }
    CONTRACTL_END;

    BSTR *pOle = (BSTR *) oleArray;
    BSTR *pOleEnd = pOle + cElements;

    while (pOle < pOleEnd)
    {
        BSTR bstr = *pOle++;

        if (bstr != NULL)
            SysFreeString(bstr);
    }
}
#endif // FEATURE_COMINTEROP



/* ------------------------------------------------------------------------- *
 * Structure marshaling routines
 * ------------------------------------------------------------------------- */
void OleVariant::MarshalNonBlittableRecordArrayOleToCom(void *oleArray, BASEARRAYREF *pComArray,
                                                        MethodTable *pInterfaceMT, PCODE pManagedMarshalerCode)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(oleArray));
        PRECONDITION(CheckPointer(pComArray));
        PRECONDITION(CheckPointer(pInterfaceMT));
    }
    CONTRACTL_END;

    ASSERT_PROTECTED(pComArray);

    SIZE_T elementCount = (*pComArray)->GetNumComponents();
    SIZE_T elemSize     = pInterfaceMT->GetNativeSize();

    BYTE *pOle = (BYTE *) oleArray;
    BYTE *pOleEnd = pOle + elemSize * elementCount;

    SIZE_T dstofs = ArrayBase::GetDataPtrOffset( (*pComArray)->GetMethodTable() );
    while (pOle < pOleEnd)
    {
        BYTE* managedData = (BYTE*)(*(LPVOID*)pComArray) + dstofs;

        MarshalStructViaILStubCode(pManagedMarshalerCode, managedData, pOle, StructMarshalStubs::MarshalOperation::Unmarshal);

        dstofs += (*pComArray)->GetComponentSize();
        pOle += elemSize;
    }
}

void OleVariant::MarshalNonBlittableRecordArrayComToOle(BASEARRAYREF *pComArray, void *oleArray,
                                          MethodTable *pInterfaceMT, BOOL fBestFitMapping,
                                          BOOL fThrowOnUnmappableChar,
                                          BOOL fOleArrayIsValid, SIZE_T cElements, PCODE pManagedMarshalerCode)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(oleArray));
        PRECONDITION(CheckPointer(pComArray));
        PRECONDITION(CheckPointer(pInterfaceMT));
    }
    CONTRACTL_END;

    ASSERT_PROTECTED(pComArray);

    SIZE_T elemSize     = pInterfaceMT->GetNativeSize();

    BYTE *pOle = (BYTE *) oleArray;
    BYTE *pOleEnd = pOle + elemSize * cElements;

    if (!fOleArrayIsValid)
    {
        // field marshalers assume that the native structure is valid
        FillMemory(pOle, pOleEnd - pOle, 0);
    }

    const SIZE_T compSize = (*pComArray)->GetComponentSize();
    SIZE_T offset = 0;
    while (pOle < pOleEnd)
    {
        BYTE* managedData = (*pComArray)->GetDataPtr() + offset;
        MarshalStructViaILStubCode(pManagedMarshalerCode, managedData, pOle, StructMarshalStubs::MarshalOperation::Marshal);

        pOle += elemSize;
        offset += compSize;
    }
}

void OleVariant::ClearNonBlittableRecordArray(void *oleArray, SIZE_T cElements, MethodTable *pInterfaceMT, PCODE pManagedMarshalerCode)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(oleArray));
        PRECONDITION(CheckPointer(pInterfaceMT));
    }
    CONTRACTL_END;

    SIZE_T elemSize     = pInterfaceMT->GetNativeSize();
    SIZE_T componentSize = TypeHandle(pInterfaceMT).MakeSZArray().GetMethodTable()->GetComponentSize();
    BYTE *pOle = (BYTE *) oleArray;
    BYTE *pOleEnd = pOle + elemSize * cElements;
    while (pOle < pOleEnd)
    {
        MarshalStructViaILStubCode(pManagedMarshalerCode, nullptr, pOle, StructMarshalStubs::MarshalOperation::Cleanup);

        pOle += elemSize;
    }
}


/* ------------------------------------------------------------------------- *
 * LPWSTR marshaling routines
 * ------------------------------------------------------------------------- */

void OleVariant::MarshalLPWSTRArrayOleToCom(void *oleArray, BASEARRAYREF *pComArray,
                                            MethodTable *pInterfaceMT, PCODE pManagedMarshalerCode)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(oleArray));
        PRECONDITION(CheckPointer(pComArray));
    }
    CONTRACTL_END;

    ASSERT_PROTECTED(pComArray);
    SIZE_T elementCount = (*pComArray)->GetNumComponents();

    LPWSTR *pOle = (LPWSTR *) oleArray;
    LPWSTR *pOleEnd = pOle + elementCount;

    BASEARRAYREF unprotectedArray = *pComArray;
    STRINGREF *pCom = (STRINGREF *) unprotectedArray->GetDataPtr();

    while (pOle < pOleEnd)
    {
        LPWSTR lpwstr = *pOle++;

        STRINGREF string;
        if (lpwstr == NULL)
            string = NULL;
        else
            string = StringObject::NewString(lpwstr);

        //
        // Reset pCom pointer only if array object has moved, rather than
        // recomputing it every time through the loop.  Beware implicit calls to
        // ValidateObject inside OBJECTREF methods.
        //

        if (*(void **)&unprotectedArray != *(void **)&*pComArray)
        {
            SIZE_T currentOffset = ((BYTE *)pCom) - (*(Object **) &unprotectedArray)->GetAddress();
            unprotectedArray = *pComArray;
            pCom = (STRINGREF *) (unprotectedArray->GetAddress() + currentOffset);
        }

        SetObjectReference((OBJECTREF*) pCom++, (OBJECTREF) string);
    }
}

void OleVariant::MarshalLPWSTRRArrayComToOle(BASEARRAYREF *pComArray, void *oleArray,
                                             MethodTable *pInterfaceMT, BOOL fBestFitMapping,
                                             BOOL fThrowOnUnmappableChar,
                                             BOOL fOleArrayIsValid, SIZE_T cElements, PCODE pManagedMarshalerCode)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(oleArray));
        PRECONDITION(CheckPointer(pComArray));
    }
    CONTRACTL_END;

    ASSERT_PROTECTED(pComArray);

    LPWSTR *pOle = (LPWSTR *) oleArray;
    LPWSTR *pOleEnd = pOle + cElements;

    STRINGREF *pCom = (STRINGREF *) (*pComArray)->GetDataPtr();

    while (pOle < pOleEnd)
    {
        //
        // We aren't calling anything which might cause a GC, so don't worry about
        // the array moving here.
        //

        STRINGREF stringRef = *pCom++;

        LPWSTR lpwstr;
        if (stringRef == NULL)
        {
            lpwstr = NULL;
        }
        else
        {
            // Retrieve the length of the string.
            int Length = stringRef->GetStringLength();
            int allocLength = (Length + 1) * sizeof(WCHAR);
            if (allocLength < Length)
                ThrowOutOfMemory();

            // Allocate the string using CoTaskMemAlloc.
            lpwstr = (LPWSTR)CoTaskMemAlloc(allocLength);
            if (lpwstr == NULL)
                ThrowOutOfMemory();

            // Copy the COM+ string into the newly allocated LPWSTR.
            memcpyNoGCRefs(lpwstr, stringRef->GetBuffer(), (Length + 1) * sizeof(WCHAR));
            lpwstr[Length] = 0;
        }

        *pOle++ = lpwstr;
    }
}

void OleVariant::ClearLPWSTRArray(void *oleArray, SIZE_T cElements, MethodTable *pInterfaceMT, PCODE pManagedMarshalerCode)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(oleArray));
    }
    CONTRACTL_END;

    LPWSTR *pOle = (LPWSTR *) oleArray;
    LPWSTR *pOleEnd = pOle + cElements;

    while (pOle < pOleEnd)
    {
        LPWSTR lpwstr = *pOle++;

        if (lpwstr != NULL)
            CoTaskMemFree(lpwstr);
    }
}

/* ------------------------------------------------------------------------- *
 * LPWSTR marshaling routines
 * ------------------------------------------------------------------------- */

void OleVariant::MarshalLPSTRArrayOleToCom(void *oleArray, BASEARRAYREF *pComArray,
                                           MethodTable *pInterfaceMT, PCODE pManagedMarshalerCode)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(oleArray));
        PRECONDITION(CheckPointer(pComArray));
    }
    CONTRACTL_END;

    ASSERT_PROTECTED(pComArray);
    SIZE_T elementCount = (*pComArray)->GetNumComponents();

    LPSTR *pOle = (LPSTR *) oleArray;
    LPSTR *pOleEnd = pOle + elementCount;

    BASEARRAYREF unprotectedArray = *pComArray;
    STRINGREF *pCom = (STRINGREF *) unprotectedArray->GetDataPtr();

    while (pOle < pOleEnd)
    {
        LPSTR lpstr = *pOle++;

        STRINGREF string;
        if (lpstr == NULL)
            string = NULL;
        else
            string = StringObject::NewString(lpstr);

        //
        // Reset pCom pointer only if array object has moved, rather than
        // recomputing it every time through the loop.  Beware implicit calls to
        // ValidateObject inside OBJECTREF methods.
        //

        if (*(void **)&unprotectedArray != *(void **)&*pComArray)
        {
            SIZE_T currentOffset = ((BYTE *)pCom) - (*(Object **) &unprotectedArray)->GetAddress();
            unprotectedArray = *pComArray;
            pCom = (STRINGREF *) (unprotectedArray->GetAddress() + currentOffset);
        }

        SetObjectReference((OBJECTREF*) pCom++, (OBJECTREF) string);
    }
}

void OleVariant::MarshalLPSTRRArrayComToOle(BASEARRAYREF *pComArray, void *oleArray,
                                            MethodTable *pInterfaceMT, BOOL fBestFitMapping,
                                            BOOL fThrowOnUnmappableChar,
                                            BOOL fOleArrayIsValid, SIZE_T cElements, PCODE pManagedMarshalerCode)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(oleArray));
        PRECONDITION(CheckPointer(pComArray));
    }
    CONTRACTL_END;

    ASSERT_PROTECTED(pComArray);

    LPSTR *pOle = (LPSTR *) oleArray;
    LPSTR *pOleEnd = pOle + cElements;

    STRINGREF *pCom = (STRINGREF *) (*pComArray)->GetDataPtr();

    while (pOle < pOleEnd)
    {
        //
        // We aren't calling anything which might cause a GC, so don't worry about
        // the array moving here.
        //
        STRINGREF stringRef = *pCom++;

        CoTaskMemHolder<CHAR> lpstr(NULL);
        if (stringRef == NULL)
        {
            lpstr = NULL;
        }
        else
        {
            // Retrieve the length of the string.
            int Length = stringRef->GetStringLength();
            int allocLength = Length * GetMaxDBCSCharByteSize() + 1;
            if (allocLength < Length)
                ThrowOutOfMemory();

            // Allocate the string using CoTaskMemAlloc.
            lpstr = (LPSTR)CoTaskMemAlloc(allocLength);
            if (lpstr == NULL)
                ThrowOutOfMemory();

            // Convert the unicode string to an ansi string.
            int bytesWritten = InternalWideToAnsi(stringRef->GetBuffer(), Length, lpstr, allocLength, fBestFitMapping, fThrowOnUnmappableChar);
            _ASSERTE(bytesWritten >= 0 && bytesWritten < allocLength);
            lpstr[bytesWritten] = 0;
        }

        *pOle++ = lpstr;
        lpstr.SuppressRelease();
    }
}

void OleVariant::ClearLPSTRArray(void *oleArray, SIZE_T cElements, MethodTable *pInterfaceMT, PCODE pManagedMarshalerCode)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(oleArray));
    }
    CONTRACTL_END;

    LPSTR *pOle = (LPSTR *) oleArray;
    LPSTR *pOleEnd = pOle + cElements;

    while (pOle < pOleEnd)
    {
        LPSTR lpstr = *pOle++;

        if (lpstr != NULL)
            CoTaskMemFree(lpstr);
    }
}

/* ------------------------------------------------------------------------- *
 * Date marshaling routines
 * ------------------------------------------------------------------------- */

#ifdef FEATURE_COMINTEROP
void OleVariant::MarshalDateVariantOleToCom(VARIANT *pOleVariant,
                                            VariantData *pComVariant)
{
    WRAPPER_NO_CONTRACT;

    *(INT64*)pComVariant->GetData() = COMDateTime::DoubleDateToTicks(V_DATE(pOleVariant));
}

void OleVariant::MarshalDateVariantComToOle(VariantData *pComVariant,
                                            VARIANT *pOleVariant)
{
    WRAPPER_NO_CONTRACT;

    V_DATE(pOleVariant) = COMDateTime::TicksToDoubleDate(*(INT64*)pComVariant->GetData());
}

void OleVariant::MarshalDateVariantOleRefToCom(VARIANT *pOleVariant,
                                               VariantData *pComVariant)
{
    WRAPPER_NO_CONTRACT;

    *(INT64*)pComVariant->GetData() = COMDateTime::DoubleDateToTicks(*V_DATEREF(pOleVariant));
}
#endif // FEATURE_COMINTEROP

void OleVariant::MarshalDateArrayOleToCom(void *oleArray, BASEARRAYREF *pComArray,
                                          MethodTable *pInterfaceMT, PCODE pManagedMarshalerCode)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(oleArray));
        PRECONDITION(CheckPointer(pComArray));
    }
    CONTRACTL_END;

    ASSERT_PROTECTED(pComArray);

    SIZE_T elementCount = (*pComArray)->GetNumComponents();

    DATE *pOle = (DATE *) oleArray;
    DATE *pOleEnd = pOle + elementCount;

    INT64 *pCom = (INT64 *) (*pComArray)->GetDataPtr();

    //
    // We aren't calling anything which might cause a GC, so don't worry about
    // the array moving here.
    //

    while (pOle < pOleEnd)
        *pCom++ = COMDateTime::DoubleDateToTicks(*pOle++);
}

void OleVariant::MarshalDateArrayComToOle(BASEARRAYREF *pComArray, void *oleArray,
                                          MethodTable *pInterfaceMT, BOOL fBestFitMapping,
                                          BOOL fThrowOnUnmappableChar,
                                          BOOL fOleArrayIsValid, SIZE_T cElements, PCODE pManagedMarshalerCode)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(oleArray));
        PRECONDITION(CheckPointer(pComArray));
    }
    CONTRACTL_END;

    ASSERT_PROTECTED(pComArray);

    DATE *pOle = (DATE *) oleArray;
    DATE *pOleEnd = pOle + cElements;

    INT64 *pCom = (INT64 *) (*pComArray)->GetDataPtr();

    //
    // We aren't calling anything which might cause a GC, so don't worry about
    // the array moving here.
    //

    while (pOle < pOleEnd)
        *pOle++ = COMDateTime::TicksToDoubleDate(*pCom++);
}

/* ------------------------------------------------------------------------- *
 * Decimal marshaling routines
 * ------------------------------------------------------------------------- */

#ifdef FEATURE_COMINTEROP

void OleVariant::MarshalDecimalVariantOleToCom(VARIANT *pOleVariant,
                                               VariantData *pComVariant)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pOleVariant));
        PRECONDITION(CheckPointer(pComVariant));
    }
    CONTRACTL_END;

    OBJECTREF pDecimalRef = AllocateObject(CoreLibBinder::GetClass(CLASS__DECIMAL));

    DECIMAL* pDecimal = (DECIMAL *) pDecimalRef->UnBox();
    *pDecimal = V_DECIMAL(pOleVariant);
    // Mashaling uses the reserved value to store the variant type, so clear it out when marshaling back
    pDecimal->wReserved = 0;

    pComVariant->SetObjRef(pDecimalRef);
}

void OleVariant::MarshalDecimalVariantComToOle(VariantData *pComVariant,
                                               VARIANT *pOleVariant)
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_TRIGGERS);
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pOleVariant));
        PRECONDITION(CheckPointer(pComVariant));
    }
    CONTRACTL_END;

    VARTYPE vt = V_VT(pOleVariant);
    _ASSERTE(vt == VT_DECIMAL);
    V_DECIMAL(pOleVariant) = * (DECIMAL*) pComVariant->GetObjRef()->UnBox();
    V_VT(pOleVariant) = vt;
}

void OleVariant::MarshalDecimalVariantOleRefToCom(VARIANT *pOleVariant,
                                                  VariantData *pComVariant )
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pOleVariant));
        PRECONDITION(CheckPointer(pComVariant));
    }
    CONTRACTL_END;

    OBJECTREF pDecimalRef = AllocateObject(CoreLibBinder::GetClass(CLASS__DECIMAL));

    DECIMAL* pDecimal = (DECIMAL *) pDecimalRef->UnBox();
    *pDecimal = *V_DECIMALREF(pOleVariant);
    // Mashaling uses the reserved value to store the variant type, so clear it out when marshaling back
    pDecimal->wReserved = 0;

    pComVariant->SetObjRef(pDecimalRef);
}
#endif // FEATURE_COMINTEROP

/* ------------------------------------------------------------------------- *
 * Record marshaling routines
 * ------------------------------------------------------------------------- */

#ifdef FEATURE_COMINTEROP
void OleVariant::MarshalRecordVariantOleToCom(VARIANT *pOleVariant,
                                              VariantData *pComVariant)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pOleVariant));
        PRECONDITION(CheckPointer(pComVariant));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    IRecordInfo *pRecInfo = V_RECORDINFO(pOleVariant);
    if (!pRecInfo)
        COMPlusThrow(kArgumentException, IDS_EE_INVALID_OLE_VARIANT);

    OBJECTREF BoxedValueClass = NULL;
    GCPROTECT_BEGIN(BoxedValueClass)
    {
        LPVOID pvRecord = V_RECORD(pOleVariant);
        if (pvRecord)
        {
            // This value type should have been registered through
            // a TLB. CoreCLR doesn't support dynamic type mapping.
            COMPlusThrow(kArgumentException, IDS_EE_CANNOT_MAP_TO_MANAGED_VC);
        }

        pComVariant->SetObjRef(BoxedValueClass);
    }
    GCPROTECT_END();
}

void OleVariant::MarshalRecordVariantComToOle(VariantData *pComVariant,
                                              VARIANT *pOleVariant)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pOleVariant));
        PRECONDITION(CheckPointer(pComVariant));
    }
    CONTRACTL_END;

    OBJECTREF BoxedValueClass = pComVariant->GetObjRef();
    GCPROTECT_BEGIN(BoxedValueClass)
    {
        _ASSERTE(BoxedValueClass != NULL);
        ConvertValueClassToVariant(&BoxedValueClass, pOleVariant);
    }
    GCPROTECT_END();
}

void OleVariant::MarshalRecordVariantOleRefToCom(VARIANT *pOleVariant,
                                                 VariantData *pComVariant)
{
    WRAPPER_NO_CONTRACT;

    // The representation of a VT_RECORD and a VT_BYREF | VT_RECORD VARIANT are
    // the same so we can simply forward the call to the non byref API.
    MarshalRecordVariantOleToCom(pOleVariant, pComVariant);
}
#endif // FEATURE_COMINTEROP

void OleVariant::MarshalRecordArrayOleToCom(void *oleArray, BASEARRAYREF *pComArray,
                                            MethodTable *pElementMT, PCODE pManagedMarshalerCode)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(oleArray));
        PRECONDITION(CheckPointer(pComArray));
        PRECONDITION(CheckPointer(pElementMT));
    }
    CONTRACTL_END;

    if (pElementMT->IsBlittable())
    {
        // The array is blittable so we can simply copy it.
        _ASSERTE(pComArray);
        SIZE_T elementCount = (*pComArray)->GetNumComponents();
        SIZE_T elemSize     = pElementMT->GetNativeSize();
        memcpyNoGCRefs((*pComArray)->GetDataPtr(), oleArray, elementCount * elemSize);
    }
    else
    {
        // The array is non blittable so we need to marshal the elements.
        _ASSERTE(pElementMT->HasLayout());
        MarshalNonBlittableRecordArrayOleToCom(oleArray, pComArray, pElementMT, pManagedMarshalerCode);
    }
}

void OleVariant::MarshalRecordArrayComToOle(BASEARRAYREF *pComArray, void *oleArray,
                                            MethodTable *pElementMT, BOOL fBestFitMapping,
                                            BOOL fThrowOnUnmappableChar,
                                            BOOL fOleArrayIsValid, SIZE_T cElements, PCODE pManagedMarshalerCode)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(oleArray));
        PRECONDITION(CheckPointer(pComArray));
        PRECONDITION(CheckPointer(pElementMT));
    }
    CONTRACTL_END;

    if (pElementMT->IsBlittable())
    {
        // The array is blittable so we can simply copy it.
        _ASSERTE(pComArray);
        SIZE_T elemSize     = pElementMT->GetNativeSize();
        memcpyNoGCRefs(oleArray, (*pComArray)->GetDataPtr(), cElements * elemSize);
    }
    else
    {
        // The array is non blittable so we need to marshal the elements.
        _ASSERTE(pElementMT->HasLayout());
        MarshalNonBlittableRecordArrayComToOle(pComArray, oleArray, pElementMT, fBestFitMapping, fThrowOnUnmappableChar, fOleArrayIsValid, cElements, pManagedMarshalerCode);
    }
}


void OleVariant::ClearRecordArray(void *oleArray, SIZE_T cElements, MethodTable *pElementMT, PCODE pManagedMarshalerCode)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(oleArray));
        PRECONDITION(CheckPointer(pElementMT));
    }
    CONTRACTL_END;

    if (!pElementMT->IsBlittable())
    {
        _ASSERTE(pElementMT->HasLayout());
        ClearNonBlittableRecordArray(oleArray, cElements, pElementMT, pManagedMarshalerCode);
    }
}

#ifdef FEATURE_COMINTEROP

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
            V_BOOL(pOle) = *(U1*)( (*pObj)->GetData() ) ? VARIANT_TRUE : VARIANT_FALSE;
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
            MethodDescCallSite convertObjectToVariant(METHOD__VARIANT__CONVERT_OBJECT_TO_VARIANT);

            VariantData managedVariant;
            FillMemory(&managedVariant, sizeof(managedVariant), 0);
            GCPROTECT_BEGIN_VARIANTDATA(managedVariant)
            {
                ARG_SLOT args[] = {
                        ObjToArgSlot(*pObj),
                        PtrToArgSlot(&managedVariant),
                        };

                convertObjectToVariant.Call(args);

                OleVariant::MarshalOleVariantForComVariant(&managedVariant, pOle);
            }
            GCPROTECT_END_VARIANTDATA();
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
            MethodDescCallSite castVariant(METHOD__VARIANT__CAST_VARIANT);

            // MarshalOleRefVariantForObjectNoCast has checked that the variant is not an array
            // so we can use the marshal cast helper to coerce the object to the proper type.
            VariantData vd;
            FillMemory(&vd, sizeof(vd), 0);
            VARTYPE vt = V_VT(pOle) & ~VT_BYREF;

            GCPROTECT_BEGIN_VARIANTDATA(vd);
            {
                ARG_SLOT args[3];
                args[0] = ObjToArgSlot(*pObj);
                args[1] = (ARG_SLOT)vt;
                args[2] = PtrToArgSlot(&vd);
                castVariant.Call(args);
                VARIANT vtmp;
                VariantInit(&vtmp);
                OleVariant::MarshalOleVariantForComVariant(&vd, &vtmp);

                // If the variant types are still not the same then call VariantChangeType to
                // try and coerse them.
                if (V_VT(&vtmp) != vt)
                {
                    VARIANT vtmp2;
                    memset(&vtmp2, 0, sizeof(VARIANT));

                    // The type of the variant has changed so attempt to change
                    // the type back.
                    hr = SafeVariantChangeType(&vtmp2, &vtmp, 0, vt);
                    if (FAILED(hr))
                    {
                        if (hr == DISP_E_TYPEMISMATCH)
                            COMPlusThrow(kInvalidCastException, IDS_EE_CANNOT_COERCE_BYREF_VARIANT);
                        else
                            COMPlusThrowHR(hr);
                    }

                    // Copy the converted variant back into the original variant and clear the temp.
                    InsertContentsIntoByrefVariant(&vtmp2, pOle);
                    SafeVariantClear(&vtmp);
                }
                else
                {
                    InsertContentsIntoByrefVariant(&vtmp, pOle);
                }
            }
            GCPROTECT_END_VARIANTDATA();
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

        *(V_BOOLREF(pOle)) =  ( *(U1*)( (*pObj)->GetData() ) ) ? VARIANT_TRUE : VARIANT_FALSE;
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
            InsertContentsIntoByrefVariant(&vtmp, pOle);
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
            InsertContentsIntoByrefVariant(&vtmp, pOle);
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
            InsertContentsIntoByrefVariant(&vtmp, pOle);
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
            {
                MethodDescCallSite convertVariantToObject(METHOD__VARIANT__CONVERT_VARIANT_TO_OBJECT);

                VariantData managedVariant;
                FillMemory(&managedVariant, sizeof(managedVariant), 0);
                GCPROTECT_BEGIN_VARIANTDATA(managedVariant)
                {
                    OleVariant::MarshalComVariantForOleVariant((VARIANT*)pOle, &managedVariant);
                    ARG_SLOT args[] = { PtrToArgSlot(&managedVariant) };
                    SetObjectReference( pObj,
                                        convertVariantToObject.Call_RetOBJECTREF(args) );
                }
                GCPROTECT_END_VARIANTDATA();
            }
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

void OleVariant::InsertContentsIntoByrefVariant(VARIANT *pSrcVar, VARIANT *pByrefVar)
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

void OleVariant::MarshalComVariantForOleVariant(VARIANT *pOle, VariantData *pCom)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pOle));
        PRECONDITION(CheckPointer(pCom));
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

    CVTypes cvt = GetCVTypeForVarType(vt);
    const Marshaler *marshal = GetMarshalerForVarType(vt, TRUE);

    pCom->SetType(cvt);
    pCom->SetVT(vt); // store away VT for return trip.
    if (marshal == NULL || (byref
                            ? marshal->OleRefToComVariant == NULL
                            : marshal->OleToComVariant == NULL))
    {
        if (cvt==CV_EMPTY)
        {
            if (V_ISBYREF(pOle))
            {
                // Must set ObjectRef field of Variant to a specific instance.
#ifdef HOST_64BIT
                VariantData::NewVariant(pCom, CV_U8, (INT64)(size_t)V_BYREF(pOle));
#else // HOST_64BIT
                VariantData::NewVariant(pCom, CV_U4, (INT32)(size_t)V_BYREF(pOle));
#endif // HOST_64BIT
            }
            else
            {
                VariantData::NewVariant(pCom, cvt, NULL);
            }
        }
        else if (cvt==CV_NULL)
        {
            VariantData::NewVariant(pCom, cvt, NULL);
        }
        else
        {
            pCom->SetObjRef(NULL);
            if (byref)
            {
                INT64 data = 0;
                CopyMemory(&data, V_R8REF(pOle), GetElementSizeForVarType(vt, NULL));
                pCom->SetData(&data);
            }
            else
                pCom->SetData(&V_R8(pOle));
        }
    }
    else
    {
        if (byref)
            marshal->OleRefToComVariant(pOle, pCom);
        else
            marshal->OleToComVariant(pOle, pCom);
    }
}

//
// MarshalOleVariantForComVariant copies the contents of the OLE variant from
// the COM variant.
//

void OleVariant::MarshalOleVariantForComVariant(VariantData *pCom, VARIANT *pOle)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pCom));
        PRECONDITION(CheckPointer(pOle));
    }
    CONTRACTL_END;

    SafeVariantClear(pOle);

    VariantEmptyHolder veh;
    veh = pOle;

    VARTYPE vt = GetVarTypeForComVariant(pCom);
    V_VT(pOle) = vt;

    const Marshaler *marshal = GetMarshalerForVarType(vt, TRUE);

    if (marshal == NULL || marshal->ComToOleVariant == NULL)
    {
        *(INT64*)&V_R8(pOle) = *(INT64*)pCom->GetData();
    }
    else
    {
        marshal->ComToOleVariant(pCom, pOle);
    }

    veh.SuppressRelease();
}

void OleVariant::MarshalInterfaceArrayComToOleHelper(BASEARRAYREF *pComArray, void *oleArray,
                                                     MethodTable *pElementMT, BOOL bDefaultIsDispatch,
                                                     SIZE_T cElements)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pComArray));
        PRECONDITION(CheckPointer(oleArray));
    }
    CONTRACTL_END;

    ASSERT_PROTECTED(pComArray);


    BOOL bDispatch = bDefaultIsDispatch;
    BOOL bHeterogenous = (pElementMT == NULL);

    // If the method table is for Object then don't consider it.
    if (pElementMT == g_pObjectClass)
        pElementMT = NULL;

    // If the element MT represents a class, then we need to determine the default
    // interface to use to expose the object out to COM.
    if (pElementMT && !pElementMT->IsInterface())
    {
        pElementMT = GetDefaultInterfaceMTForClass(pElementMT, &bDispatch);
    }

    // Determine the start and the end of the data in the OLE array.
    IUnknown **pOle = (IUnknown **) oleArray;
    IUnknown **pOleEnd = pOle + cElements;

    // Retrieve the start of the data in the managed array.
    BASEARRAYREF unprotectedArray = *pComArray;
    OBJECTREF *pCom = (OBJECTREF *) unprotectedArray->GetDataPtr();

    OBJECTREF TmpObj = NULL;
    GCPROTECT_BEGIN(TmpObj)
    {
        MethodTable *pLastElementMT = NULL;

        while (pOle < pOleEnd)
        {
            TmpObj = *pCom++;

            IUnknown *unk;
            if (TmpObj == NULL)
                unk = NULL;
            else
            {
                if (bHeterogenous)
                {
                    // Inspect the type of each element separately (cache the last type for perf).
                    if (TmpObj->GetMethodTable() != pLastElementMT)
                    {
                        pLastElementMT = TmpObj->GetMethodTable();
                        pElementMT = GetDefaultInterfaceMTForClass(pLastElementMT, &bDispatch);
                    }
                }

                if (pElementMT)
                {
                    // Convert to COM IP based on an interface MT (a specific interface will be exposed).
                    unk = GetComIPFromObjectRef(&TmpObj, pElementMT);
                }
                else
                {
                    // Convert to COM IP exposing either IDispatch or IUnknown.
                    unk = GetComIPFromObjectRef(&TmpObj, (bDispatch ? ComIpType_Dispatch : ComIpType_Unknown), NULL);
                }
            }

            *pOle++ = unk;

            if (*(void **)&unprotectedArray != *(void **)&*pComArray)
            {
                SIZE_T currentOffset = ((BYTE *)pCom) - (*(Object **) &unprotectedArray)->GetAddress();
                unprotectedArray = *pComArray;
                pCom = (OBJECTREF *) (unprotectedArray->GetAddress() + currentOffset);
            }
        }
    }
    GCPROTECT_END();
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
    EX_END_CATCH(SwallowAllExceptions);

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

void OleVariant::MarshalIDispatchArrayComToOle(BASEARRAYREF *pComArray, void *oleArray,
                                               MethodTable *pElementMT, BOOL fBestFitMapping,
                                               BOOL fThrowOnUnmappableChar, BOOL fOleArrayIsValid,
                                               SIZE_T cElements, PCODE pManagedMarshalerCode)
{
    WRAPPER_NO_CONTRACT;

    MarshalInterfaceArrayComToOleHelper(pComArray, oleArray, pElementMT, TRUE, cElements);
}


/* ------------------------------------------------------------------------- *
 * Currency marshaling routines
 * ------------------------------------------------------------------------- */

void OleVariant::MarshalCurrencyVariantOleToCom(VARIANT *pOleVariant,
                                                VariantData *pComVariant)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pOleVariant));
        PRECONDITION(CheckPointer(pComVariant));
    }
    CONTRACTL_END;

    OBJECTREF pDecimalRef = AllocateObject(CoreLibBinder::GetClass(CLASS__DECIMAL));
    DECIMAL DecVal;

    // Convert the currency to a decimal.
    VarDecFromCyCanonicalize(V_CY(pOleVariant), &DecVal);

    // Store the value into the unboxes decimal and store the decimal in the variant.
    *(DECIMAL *) pDecimalRef->UnBox() = DecVal;
    pComVariant->SetObjRef(pDecimalRef);
}

void OleVariant::MarshalCurrencyVariantComToOle(VariantData *pComVariant,
                                                VARIANT *pOleVariant)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pOleVariant));
        PRECONDITION(CheckPointer(pComVariant));
    }
    CONTRACTL_END;

    CURRENCY CyVal;

    // Convert the decimal to a currency.
    HRESULT hr = VarCyFromDec((DECIMAL*)pComVariant->GetObjRef()->UnBox(), &CyVal);
    IfFailThrow(hr);

    // Store the currency in the VARIANT and set the VT.
    V_CY(pOleVariant) = CyVal;
}

void OleVariant::MarshalCurrencyVariantOleRefToCom(VARIANT *pOleVariant,
                                                   VariantData *pComVariant)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pOleVariant));
        PRECONDITION(CheckPointer(pComVariant));
    }
    CONTRACTL_END;

    OBJECTREF pDecimalRef = AllocateObject(CoreLibBinder::GetClass(CLASS__DECIMAL));
    DECIMAL DecVal;

    // Convert the currency to a decimal.
    VarDecFromCyCanonicalize(*V_CYREF(pOleVariant), &DecVal);

    // Store the value into the unboxes decimal and store the decimal in the variant.
    *(DECIMAL *) pDecimalRef->UnBox() = DecVal;
    pComVariant->SetObjRef(pDecimalRef);
}

void OleVariant::MarshalCurrencyArrayOleToCom(void *oleArray, BASEARRAYREF *pComArray,
                                              MethodTable *pInterfaceMT, PCODE pManagedMarshalerCode)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(oleArray));
        PRECONDITION(CheckPointer(pComArray));
    }
    CONTRACTL_END;

    ASSERT_PROTECTED(pComArray);
    SIZE_T elementCount = (*pComArray)->GetNumComponents();

    CURRENCY *pOle = (CURRENCY *) oleArray;
    CURRENCY *pOleEnd = pOle + elementCount;

    DECIMAL *pCom = (DECIMAL *) (*pComArray)->GetDataPtr();

    while (pOle < pOleEnd)
    {
        VarDecFromCyCanonicalize(*pOle++, pCom++);
    }
}

void OleVariant::MarshalCurrencyArrayComToOle(BASEARRAYREF *pComArray, void *oleArray,
                                              MethodTable *pInterfaceMT, BOOL fBestFitMapping,
                                              BOOL fThrowOnUnmappableChar,
                                              BOOL fOleArrayIsValid, SIZE_T cElements, PCODE pManagedMarshalerCode)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(oleArray));
        PRECONDITION(CheckPointer(pComArray));
    }
    CONTRACTL_END;

    ASSERT_PROTECTED(pComArray);

    CURRENCY *pOle = (CURRENCY *) oleArray;
    CURRENCY *pOleEnd = pOle + cElements;

    DECIMAL *pCom = (DECIMAL *) (*pComArray)->GetDataPtr();

    while (pOle < pOleEnd)
        IfFailThrow(VarCyFromDec(pCom++, pOle++));
}


/* ------------------------------------------------------------------------- *
 * Variant marshaling routines
 * ------------------------------------------------------------------------- */

void OleVariant::MarshalVariantArrayOleToCom(void *oleArray, BASEARRAYREF *pComArray,
                                             MethodTable *pInterfaceMT, PCODE pManagedMarshalerCode)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(oleArray));
        PRECONDITION(CheckPointer(pComArray));
    }
    CONTRACTL_END;

    ASSERT_PROTECTED(pComArray);

    SIZE_T elementCount = (*pComArray)->GetNumComponents();

    VARIANT *pOle = (VARIANT *) oleArray;
    VARIANT *pOleEnd = pOle + elementCount;

    BASEARRAYREF unprotectedArray = *pComArray;
    OBJECTREF *pCom = (OBJECTREF *) unprotectedArray->GetDataPtr();

    OBJECTREF TmpObj = NULL;
    GCPROTECT_BEGIN(TmpObj)
    {
        while (pOle < pOleEnd)
        {
            // Marshal the OLE variant into a temp managed variant.
            MarshalObjectForOleVariant(pOle++, &TmpObj);

            // Reset pCom pointer only if array object has moved, rather than
            // recomputing it every time through the loop.  Beware implicit calls to
            // ValidateObject inside OBJECTREF methods.
            if (*(void **)&unprotectedArray != *(void **)&*pComArray)
            {
                SIZE_T currentOffset = ((BYTE *)pCom) - (*(Object **) &unprotectedArray)->GetAddress();
                unprotectedArray = *pComArray;
                pCom = (OBJECTREF *) (unprotectedArray->GetAddress() + currentOffset);
            }
            SetObjectReference(pCom++, TmpObj);
        }
    }
    GCPROTECT_END();
}

void OleVariant::MarshalVariantArrayComToOle(BASEARRAYREF *pComArray, void *oleArray,
                                             MethodTable *pInterfaceMT, BOOL fBestFitMapping,
                                             BOOL fThrowOnUnmappableChar,
                                             BOOL fOleArrayIsValid, SIZE_T cElements, PCODE pManagedMarshalerCode)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(oleArray));
        PRECONDITION(CheckPointer(pComArray));
    }
    CONTRACTL_END;


    MarshalVariantArrayComToOle(pComArray, oleArray, pInterfaceMT, fBestFitMapping, fThrowOnUnmappableChar, FALSE, fOleArrayIsValid);
}


void OleVariant::MarshalVariantArrayComToOle(BASEARRAYREF *pComArray, void *oleArray,
                                             MethodTable *pInterfaceMT, BOOL fBestFitMapping,
                                          BOOL fThrowOnUnmappableChar, BOOL fMarshalByrefArgOnly,
                                          BOOL fOleArrayIsValid, int nOleArrayStepLength)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(oleArray));
        PRECONDITION(CheckPointer(pComArray));
    }
    CONTRACTL_END;

    ASSERT_PROTECTED(pComArray);

    SIZE_T elementCount = (*pComArray)->GetNumComponents();

    VARIANT *pOle = (VARIANT *) oleArray;
    VARIANT *pOleEnd = pOle + elementCount * nOleArrayStepLength;

    BASEARRAYREF unprotectedArray = *pComArray;
    OBJECTREF *pCom = (OBJECTREF *) unprotectedArray->GetDataPtr();

    OBJECTREF TmpObj = NULL;
    GCPROTECT_BEGIN(TmpObj)
    {
        while (pOle != pOleEnd)
        {
            // Reset pCom pointer only if array object has moved, rather than
            // recomputing it every time through the loop.  Beware implicit calls to
            // ValidateObject inside OBJECTREF methods.
            if (*(void **)&unprotectedArray != *(void **)&*pComArray)
            {
                SIZE_T currentOffset = ((BYTE *)pCom) - (*(Object **) &unprotectedArray)->GetAddress();
                unprotectedArray = *pComArray;
                pCom = (OBJECTREF *) (unprotectedArray->GetAddress() + currentOffset);
            }
            TmpObj = *pCom++;

            // Marshal the temp managed variant into the OLE variant.
            if (fOleArrayIsValid)
            {
                // We firstly try MarshalCommonOleRefVariantForObject for VT_BYREF variant because
                // MarshalOleVariantForObject() VariantClear the variant and does not keep the VT_BYREF.
                // For back compating the old behavior(we used MarshalOleVariantForObject in the previous
                //  version) that casts the managed object to Variant based on the object's MethodTable,
                // MarshalCommonOleRefVariantForObject is used instead of MarshalOleRefVariantForObject so
                // that cast will not be done based on the VT of the variant.
                if (!((pOle->vt & VT_BYREF) &&
                       SUCCEEDED(MarshalCommonOleRefVariantForObject(&TmpObj, pOle))))
                    if (pOle->vt & VT_BYREF || !fMarshalByrefArgOnly)
                        MarshalOleVariantForObject(&TmpObj, pOle);
            }
            else
            {
                // The contents of pOle is undefined, don't try to handle byrefs.
                MarshalOleVariantForObject(&TmpObj, pOle);
            }

            pOle += nOleArrayStepLength;
        }
    }
    GCPROTECT_END();
}

void OleVariant::ClearVariantArray(void *oleArray, SIZE_T cElements, MethodTable *pInterfaceMT, PCODE pManagedMarshalerCode)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(oleArray));
    }
    CONTRACTL_END;

    VARIANT *pOle = (VARIANT *) oleArray;
    VARIANT *pOleEnd = pOle + cElements;

    while (pOle < pOleEnd)
        SafeVariantClear(pOle++);
}


/* ------------------------------------------------------------------------- *
 * Array marshaling routines
 * ------------------------------------------------------------------------- */
#ifdef FEATURE_COMINTEROP

void OleVariant::MarshalArrayVariantOleToCom(VARIANT *pOleVariant,
                                             VariantData *pComVariant)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pOleVariant));
        PRECONDITION(CheckPointer(pComVariant));
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

        MethodDesc* pStructMarshalStub = nullptr;
        if (vt == VT_RECORD && !pElemMT->IsBlittable())
        {
            GCX_PREEMP();

            pStructMarshalStub = NDirect::CreateStructMarshalILStub(pElemMT);
        }

        BASEARRAYREF pArrayRef = CreateArrayRefForSafeArray(pSafeArray, vt, pElemMT);
        pComVariant->SetObjRef((OBJECTREF) pArrayRef);
        MarshalArrayRefForSafeArray(pSafeArray, (BASEARRAYREF *) pComVariant->GetObjRefPtr(), vt, pStructMarshalStub != nullptr ? pStructMarshalStub->GetMultiCallableAddrOfCode() : NULL, pElemMT);
    }
    else
    {
        pComVariant->SetObjRef(NULL);
    }
}

void OleVariant::MarshalArrayVariantComToOle(VariantData *pComVariant,
                                             VARIANT *pOleVariant)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pOleVariant));
        PRECONDITION(CheckPointer(pComVariant));
    }
    CONTRACTL_END;

    SafeArrayPtrHolder pSafeArray = NULL;
    BASEARRAYREF *pArrayRef = (BASEARRAYREF *) pComVariant->GetObjRefPtr();
    MethodTable *pElemMT = NULL;

    _ASSERTE(pArrayRef);

    VARTYPE vt = GetElementVarTypeForArrayRef(*pArrayRef);
    if (vt == VT_ARRAY)
        vt = VT_VARIANT;

    pElemMT = GetArrayElementTypeWrapperAware(pArrayRef).GetMethodTable();

    MethodDesc* pStructMarshalStub = nullptr;
    GCPROTECT_BEGIN(*pArrayRef);
    if (vt == VT_RECORD && !pElemMT->IsBlittable())
    {
        GCX_PREEMP();

        pStructMarshalStub = NDirect::CreateStructMarshalILStub(pElemMT);
    }
    GCPROTECT_END();

    if (*pArrayRef != NULL)
    {
        pSafeArray = CreateSafeArrayForArrayRef(pArrayRef, vt, pElemMT);
        MarshalSafeArrayForArrayRef(pArrayRef, pSafeArray, vt, pElemMT, pStructMarshalStub != nullptr ? pStructMarshalStub->GetMultiCallableAddrOfCode() : NULL);
    }
    V_ARRAY(pOleVariant) = pSafeArray;
    pSafeArray.SuppressRelease();
}

void OleVariant::MarshalArrayVariantOleRefToCom(VARIANT *pOleVariant,
                                                VariantData *pComVariant)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pOleVariant));
        PRECONDITION(CheckPointer(pComVariant));
    }
    CONTRACTL_END;

    SAFEARRAY *pSafeArray = *V_ARRAYREF(pOleVariant);

    VARTYPE vt = V_VT(pOleVariant) & ~(VT_ARRAY|VT_BYREF);

    if (pSafeArray)
    {
        MethodTable *pElemMT = NULL;
        if (vt == VT_RECORD)
            pElemMT = GetElementTypeForRecordSafeArray(pSafeArray).GetMethodTable();

        MethodDesc* pStructMarshalStub = nullptr;
        if (vt == VT_RECORD && !pElemMT->IsBlittable())
        {
            GCX_PREEMP();

            pStructMarshalStub = NDirect::CreateStructMarshalILStub(pElemMT);
        }

        BASEARRAYREF pArrayRef = CreateArrayRefForSafeArray(pSafeArray, vt, pElemMT);
        pComVariant->SetObjRef((OBJECTREF) pArrayRef);
        MarshalArrayRefForSafeArray(pSafeArray, (BASEARRAYREF *) pComVariant->GetObjRefPtr(), vt, pStructMarshalStub != nullptr ? pStructMarshalStub->GetMultiCallableAddrOfCode() : NULL, pElemMT);
    }
    else
    {
        pComVariant->SetObjRef(NULL);
    }
}
#endif //FEATURE_COMINTEROP


/* ------------------------------------------------------------------------- *
 * Error marshaling routines
 * ------------------------------------------------------------------------- */

void OleVariant::MarshalErrorVariantOleToCom(VARIANT *pOleVariant,
                                             VariantData *pComVariant)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pOleVariant));
        PRECONDITION(CheckPointer(pComVariant));
    }
    CONTRACTL_END;

    // Check to see if the variant represents a missing argument.
    if (V_ERROR(pOleVariant) == DISP_E_PARAMNOTFOUND)
    {
        pComVariant->SetType(CV_MISSING);
    }
    else
    {
        pComVariant->SetDataAsInt32(V_ERROR(pOleVariant));
    }
}

void OleVariant::MarshalErrorVariantOleRefToCom(VARIANT *pOleVariant,
                                                 VariantData *pComVariant)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pOleVariant));
        PRECONDITION(CheckPointer(pComVariant));
    }
    CONTRACTL_END;

    // Check to see if the variant represents a missing argument.
    if (*V_ERRORREF(pOleVariant) == DISP_E_PARAMNOTFOUND)
    {
        pComVariant->SetType(CV_MISSING);
    }
    else
    {
        pComVariant->SetDataAsInt32(*V_ERRORREF(pOleVariant));
    }
}

void OleVariant::MarshalErrorVariantComToOle(VariantData *pComVariant,
                                             VARIANT *pOleVariant)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pOleVariant));
        PRECONDITION(CheckPointer(pComVariant));
    }
    CONTRACTL_END;

    if (pComVariant->GetType() == CV_MISSING)
    {
        V_ERROR(pOleVariant) = DISP_E_PARAMNOTFOUND;
    }
    else
    {
        V_ERROR(pOleVariant) = pComVariant->GetDataAsInt32();
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

    SafeArrayPtrHolder pSafeArray = NULL;

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

    pSafeArray.SuppressRelease();
    RETURN pSafeArray;
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

//
// MarshalSafeArrayForArrayRef marshals the contents of the array ref into the given
// safe array. It is assumed that the type & dimensions of the arrays are compatible.
//
void OleVariant::MarshalSafeArrayForArrayRef(BASEARRAYREF *pArrayRef,
                                             SAFEARRAY *pSafeArray,
                                             VARTYPE vt,
                                             MethodTable *pInterfaceMT,
                                             PCODE pManagedMarshalerCode,
                                             BOOL fSafeArrayIsValid /*= TRUE*/)
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
        // Retrieve the marshaler to use to convert the contents.
        const Marshaler *marshal = GetMarshalerForVarType(vt, TRUE);

        // If the array is an array of wrappers, then we need to extract the objects
        // being wrapped and create an array of those.
        BOOL bArrayOfInterfaceWrappers;
        if (IsArrayOfWrappers(pArrayRef, &bArrayOfInterfaceWrappers))
        {
            Array = ExtractWrappedObjectsFromArray(pArrayRef);
        }
        else
        {
            Array = *pArrayRef;
        }

        if (marshal == NULL || marshal->ComToOleArray == NULL)
        {
            if (pSafeArray->cDims == 1)
            {
                // If the array is single dimensionnal then we can simply copy it over.
                memcpyNoGCRefs(pSafeArray->pvData, Array->GetDataPtr(), dwNumComponents * dwComponentSize);
            }
            else
            {
                // Copy and transpose the data.
                TransposeArrayData((BYTE*)pSafeArray->pvData, Array->GetDataPtr(), dwNumComponents, dwComponentSize, pSafeArray, FALSE);
            }
        }
        else
        {
            {
                PinningHandleHolder handle = GetAppDomain()->CreatePinningHandle((OBJECTREF)Array);

                if (bArrayOfInterfaceWrappers)
                {
                    _ASSERTE(vt == VT_UNKNOWN || vt == VT_DISPATCH);
                    // Signal to code:OleVariant::MarshalInterfaceArrayComToOleHelper that this was an array
                    // of UnknownWrapper or DispatchWrapper. It shall use a different logic and marshal each
                    // element according to its specific default interface.
                    pInterfaceMT = NULL;
                }
                marshal->ComToOleArray(&Array, pSafeArray->pvData, pInterfaceMT, TRUE, FALSE, fSafeArrayIsValid, dwNumComponents, pManagedMarshalerCode);
            }

            if (pSafeArray->cDims != 1)
            {
                // The array is multidimensionnal we need to transpose it.
                TransposeArrayData((BYTE*)pSafeArray->pvData, (BYTE*)pSafeArray->pvData, dwNumComponents, dwComponentSize, pSafeArray, FALSE);
            }
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
                                             PCODE pManagedMarshalerCode,
                                             MethodTable *pInterfaceMT)
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

    // Retrieve the marshaler to use to convert the contents.
    const Marshaler *marshal = GetMarshalerForVarType(vt, TRUE);

    if (marshal == NULL || marshal->OleToComArray == NULL)
    {
        SIZE_T dwManagedComponentSize = (*pArrayRef)->GetComponentSize();

#ifdef _DEBUG
        {
            // If we're blasting bits, this better be a primitive type.  Currency is
            // an I8 on managed & unmanaged, so it's good enough.
            TypeHandle  th = (*pArrayRef)->GetArrayElementTypeHandle();

            if (!CorTypeInfo::IsPrimitiveType(th.GetInternalCorElementType()))
            {
                _ASSERTE(!strcmp(th.AsMethodTable()->GetDebugClassName(),
                                 "System.Currency"));
            }
        }
#endif
        if (pSafeArray->cDims == 1)
        {
            // If the array is single dimensionnal then we can simply copy it over.
            memcpyNoGCRefs((*pArrayRef)->GetDataPtr(), pSafeArray->pvData, dwNumComponents * dwManagedComponentSize);
        }
        else
        {
            // Copy and transpose the data.
            TransposeArrayData((*pArrayRef)->GetDataPtr(), (BYTE*)pSafeArray->pvData, dwNumComponents, dwManagedComponentSize, pSafeArray, TRUE);
        }
    }
    else
    {
        CQuickArray<BYTE> TmpArray;
        BYTE* pSrcData = NULL;
        SIZE_T dwNativeComponentSize = GetElementSizeForVarType(vt, pInterfaceMT);

        if (pSafeArray->cDims != 1)
        {
            TmpArray.ReSizeThrows(dwNumComponents * dwNativeComponentSize);
            pSrcData = TmpArray.Ptr();
            TransposeArrayData(pSrcData, (BYTE*)pSafeArray->pvData, dwNumComponents, dwNativeComponentSize, pSafeArray, TRUE);
        }
        else
        {
            pSrcData = (BYTE*)pSafeArray->pvData;
        }

        PinningHandleHolder handle = GetAppDomain()->CreatePinningHandle((OBJECTREF)*pArrayRef);

        marshal->OleToComArray(pSrcData, pArrayRef, pInterfaceMT, pManagedMarshalerCode);
    }
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
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    SafeComHolder<ITypeInfo> pTypeInfo = NULL;
    RecordVariantHolder pRecHolder = pOleVariant;

    BOOL bSuccess = FALSE;

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

    // Marshal the contents of the value class into the record.
    MethodDesc* pStructMarshalStub;
    {
        GCPROTECT_BEGIN(*pBoxedValueClass);
        GCX_PREEMP();

        pStructMarshalStub = NDirect::CreateStructMarshalILStub(pValueClassMT);
        GCPROTECT_END();
    }

    MarshalStructViaILStub(pStructMarshalStub, (*pBoxedValueClass)->GetData(), (BYTE*)V_RECORD(pRecHolder), StructMarshalStubs::MarshalOperation::Marshal);

    pRecHolder.SuppressRelease();
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

    // Initalize the indexes for each dimension to 0.
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

#ifdef FEATURE_COMINTEROP
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
#endif //FEATURE_COMINTEROP

void OleVariant::AllocateEmptyStringForBSTR(BSTR bstr, STRINGREF *pStringObj)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(bstr));
        PRECONDITION(CheckPointer(pStringObj));
    }
    CONTRACTL_END;

    // The BSTR isn't null so allocate a managed string of the appropriate length.
    ULONG length = SysStringByteLen(bstr);

    if (length > MAX_SIZE_FOR_INTEROP)
        COMPlusThrow(kMarshalDirectiveException, IDS_EE_STRING_TOOLONG);

    // Check to see if the BSTR has trailing odd byte.
    BOOL bHasTrailByte = ((length%sizeof(WCHAR)) != 0);
    length = length / sizeof(WCHAR);
    SetObjectReference((OBJECTREF*)pStringObj, (OBJECTREF)StringObject::NewString(length, bHasTrailByte));
}

void OleVariant::ConvertContentsBSTRToString(BSTR bstr, STRINGREF *pStringObj)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(bstr));
        PRECONDITION(CheckPointer(pStringObj));
    }
    CONTRACTL_END;

    // this is the right thing to do, but sometimes we
    // end up thinking we're marshaling a BSTR when we're not, because
    // it's the default type.
    ULONG length = SysStringByteLen((BSTR)bstr);
    if (length > MAX_SIZE_FOR_INTEROP)
        COMPlusThrow(kMarshalDirectiveException, IDS_EE_STRING_TOOLONG);

    ULONG charLength = length/sizeof(WCHAR);
    BOOL hasTrailByte = (length%sizeof(WCHAR) != 0);

    memcpyNoGCRefs((*pStringObj)->GetBuffer(), bstr, charLength*sizeof(WCHAR));

    if (hasTrailByte)
    {
        BYTE* buff = (BYTE*)bstr;
        //set the trail byte
        (*pStringObj)->SetTrailByte(buff[length-1]);
    }

    // null terminate the StringRef
    WCHAR* wstr = (WCHAR *)(*pStringObj)->GetBuffer();
    wstr[charLength] = '\0';
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

    AllocateEmptyStringForBSTR(bstr, pStringObj);
    ConvertContentsBSTRToString(bstr, pStringObj);
}

BSTR OleVariant::AllocateEmptyBSTRForString(STRINGREF *pStringObj)
{
    CONTRACT(BSTR)
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pStringObj));
        PRECONDITION(*pStringObj != NULL);
        POSTCONDITION(RETVAL != NULL);
    }
    CONTRACT_END;

    ULONG length = (*pStringObj)->GetStringLength();
    if (length > MAX_SIZE_FOR_INTEROP)
        COMPlusThrow(kMarshalDirectiveException, IDS_EE_STRING_TOOLONG);

    length = length*sizeof(WCHAR);
    if ((*pStringObj)->HasTrailByte())
    {
        length += 1;
    }
    BSTR bstr = SysAllocStringByteLen(NULL, length);
    if (bstr == NULL)
        ThrowOutOfMemory();

    RETURN bstr;
}

void OleVariant::ConvertContentsStringToBSTR(STRINGREF *pStringObj, BSTR bstr)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pStringObj));
        PRECONDITION(*pStringObj != NULL);
        PRECONDITION(CheckPointer(bstr));
    }
    CONTRACTL_END;

    DWORD length = (DWORD)(*pStringObj)->GetStringLength();
    if (length > MAX_SIZE_FOR_INTEROP)
        COMPlusThrow(kMarshalDirectiveException, IDS_EE_STRING_TOOLONG);

    BYTE *buff = (BYTE*)bstr;
    ULONG byteLen = length * sizeof(WCHAR);

    memcpyNoGCRefs(bstr, (*pStringObj)->GetBuffer(), byteLen);

    if ((*pStringObj)->HasTrailByte())
    {
        BYTE b;
        BOOL hasTrailB;
        hasTrailB = (*pStringObj)->GetTrailByte(&b);
        _ASSERTE(hasTrailB);
        buff[byteLen] = b;
    }
    else
    {
        // copy the null terminator
        bstr[length] = W('\0');
    }
}

BSTR OleVariant::ConvertStringToBSTR(STRINGREF *pStringObj)
{
    CONTRACT(BSTR)
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pStringObj));

        // A null BSTR should only be returned if the input string is null.
        POSTCONDITION(RETVAL != NULL || *pStringObj == NULL);
}
    CONTRACT_END;

    // Initiatilize the return BSTR value to null.
    BSTR bstr = NULL;

    // If the string object isn't null then we convert it to a BSTR. Otherwise we will return null.
    if (*pStringObj != NULL)
    {
        bstr = AllocateEmptyBSTRForString(pStringObj);
        ConvertContentsStringToBSTR(pStringObj, bstr);
    }

    RETURN bstr;
}
#endif // FEATURE_COMINTEROP

