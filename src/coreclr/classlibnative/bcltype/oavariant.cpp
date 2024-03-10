// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: OAVariant.cpp
//

#include <common.h>

#ifdef FEATURE_COMINTEROP

#include <oleauto.h>
#include "excep.h"
#include "oavariant.h"
#include "comdatetime.h"   // DateTime <-> OleAut date conversions
#include "interoputil.h"
#include "interopconverter.h"
#include "excep.h"
#include "string.h"
#include "comutilnative.h" // for COMDate

#define INVALID_MAPPING (BYTE)(-1)

static const BYTE CVtoVTTable [] =
{
    VT_EMPTY,   // CV_EMPTY
    VT_VOID,    // CV_VOID
    VT_BOOL,    // CV_BOOLEAN
    VT_UI2,     // CV_CHAR
    VT_I1,      // CV_I1
    VT_UI1,     // CV_U1
    VT_I2,      // CV_I2
    VT_UI2,     // CV_U2
    VT_I4,      // CV_I4
    VT_UI4,     // CV_U4
    VT_I8,      // CV_I8
    VT_UI8,     // CV_U8
    VT_R4,      // CV_R4
    VT_R8,      // CV_R8
    VT_BSTR,    // CV_STRING
    INVALID_MAPPING,    // CV_PTR
    VT_DATE,    // CV_DATETIME
    INVALID_MAPPING, // CV_TIMESPAN
    VT_UNKNOWN, // CV_OBJECT
    VT_DECIMAL, // CV_DECIMAL
    VT_CY,      // CV_CURRENCY
    INVALID_MAPPING, // CV_ENUM
    INVALID_MAPPING, // CV_MISSING
    VT_NULL,    // CV_NULL
    INVALID_MAPPING  // CV_LAST
};

static const BYTE VTtoCVTable[] =
{
    CV_EMPTY,   // VT_EMPTY
    CV_NULL,    // VT_NULL
    CV_I2,      // VT_I2
    CV_I4,      // VT_I4
    CV_R4,      // VT_R4
    CV_R8,      // VT_R8
    CV_CURRENCY,// VT_CY
    CV_DATETIME,// VT_DATE
    CV_STRING,  // VT_BSTR
    INVALID_MAPPING, // VT_DISPATCH
    INVALID_MAPPING, // VT_ERROR
    CV_BOOLEAN, // VT_BOOL
    CV_OBJECT,  // VT_VARIANT
    CV_OBJECT,  // VT_UNKNOWN
    CV_DECIMAL, // VT_DECIMAL
    INVALID_MAPPING, // An unused enum table entry
    CV_I1,      // VT_I1
    CV_U1,      // VT_UI1
    CV_U2,      // VT_UI2
    CV_U4,      // VT_UI4
    CV_I8,      // VT_I8
    CV_U8,      // VT_UI8
    CV_I4,      // VT_INT
    CV_U4,      // VT_UINT
    CV_VOID     // VT_VOID
};

// Need translations from CVType to VARENUM and vice versa.  CVTypes
// is defined in olevariant.h.  VARENUM is defined in OleAut's variant.h
// Assumption here is we will only deal with VARIANTs and not other OLE
// constructs such as property sets or safe arrays.
static VARENUM CVtoVT(const CVTypes cv)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(cv >= 0 && cv < CV_LAST);
    }
    CONTRACTL_END;

    if (CVtoVTTable[cv] == INVALID_MAPPING)
        COMPlusThrow(kNotSupportedException, W("NotSupported_ChangeType"));

    return (VARENUM) CVtoVTTable[cv];
}

// Need translations from CVType to VARENUM and vice versa.  CVTypes
// is defined in olevariant.h.  VARENUM is defined in OleAut's variant.h
static CVTypes VTtoCV(const VARENUM vt)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(vt < VT_VOID);
    }
    CONTRACTL_END;

    if (vt <0 || vt > VT_VOID || VTtoCVTable[vt]==INVALID_MAPPING)
        COMPlusThrow(kNotSupportedException, W("NotSupported_ChangeType"));

    return (CVTypes) VTtoCVTable[vt];
}


// Converts a COM+ Variant to an OleAut Variant.  Returns true if
// there was a native object allocated by this method that must be freed,
// else false.
static bool ToOAVariant(VariantData const* src, VARIANT* oa)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(src));
        PRECONDITION(CheckPointer(oa));
    }
    CONTRACTL_END;

    SafeVariantInit(oa);
    UINT64 * dest = (UINT64*) &V_UI1(oa);
    *dest = 0;

    WCHAR * chars;
    int strLen;

    // Set the data field of the OA Variant to be either the object reference
    // or the data (ie int) that it needs.

    switch (src->GetType())
    {
        case CV_STRING:
            if (src->GetObjRef() == NULL)
            {
                V_BSTR(oa) = NULL;
                V_VT(oa) = static_cast<VARTYPE>(CVtoVT(src->GetType()));

                // OA perf feature: VarClear calls SysFreeString(null), which access violates.
                return false;
            }

            ((STRINGREF) (src->GetObjRef()))->RefInterpretGetStringValuesDangerousForGC(&chars, &strLen);
            V_BSTR(oa) = SysAllocStringLen(chars, strLen);
            if (V_BSTR(oa) == NULL)
                COMPlusThrowOM();

            V_VT(oa) = static_cast<VARTYPE>(CVtoVT(src->GetType()));

            return true;

        case CV_CHAR:
            chars = (WCHAR*)src->GetData();
            V_BSTR(oa) = SysAllocStringLen(chars, 1);
            if (V_BSTR(oa) == NULL)
                COMPlusThrowOM();

            // We should override the VTtoVT default of VT_UI2 for this case.
            V_VT(oa) = VT_BSTR;

            return true;

        case CV_DATETIME:
            V_DATE(oa) = COMDateTime::TicksToDoubleDate(src->GetDataAsInt64());
            V_VT(oa) = static_cast<VARTYPE>(CVtoVT(src->GetType()));
           return false;

        case CV_BOOLEAN:
            V_BOOL(oa) = (src->GetDataAsInt64()==0 ? VARIANT_FALSE : VARIANT_TRUE);
            V_VT(oa) = static_cast<VARTYPE>(CVtoVT(src->GetType()));
            return false;

        case CV_DECIMAL:
        {
            OBJECTREF obj = src->GetObjRef();
            DECIMAL * d = (DECIMAL*) obj->GetData();
            // DECIMALs and Variants are the same size.  Variants are a union between
            // all the normal Variant fields (vt, bval, etc) and a Decimal.  Decimals
            // also have the first 2 bytes reserved, for a VT field.

            V_DECIMAL(oa) = *d;
            V_VT(oa) = VT_DECIMAL;
            return false;
        }

        case CV_OBJECT:
        {
            OBJECTREF obj = src->GetObjRef();
            GCPROTECT_BEGIN(obj)
            {
                IUnknown *pUnk = NULL;

                // Convert the object to an IDispatch/IUnknown pointer.
                ComIpType FetchedIpType = ComIpType_None;
                pUnk = GetComIPFromObjectRef(&obj, ComIpType_Both, &FetchedIpType);
                V_UNKNOWN(oa) = pUnk;
                V_VT(oa) = static_cast<VARTYPE>(FetchedIpType == ComIpType_Dispatch ? VT_DISPATCH : VT_UNKNOWN);
            }
            GCPROTECT_END();
            return true;
        }

        default:
            *dest = src->GetDataAsInt64();
            V_VT(oa) = static_cast<VARTYPE>(CVtoVT(src->GetType()));
            return false;
    }
}

// Converts an OleAut Variant into a COM+ Variant.
// Note that we pass the VariantData Byref so that if GC happens, 'var' gets updated
static void FromOAVariant(VARIANT const* src, VariantData*& var)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(src));
    }
    CONTRACTL_END;

    // Clear the return variant value.  It's allocated on
    // the stack and we only want valid state data in there.
    memset(var, 0, sizeof(VariantData));

    CVTypes type = VTtoCV((VARENUM) V_VT(src));
    var->SetType(type);

    switch (type)
    {
        case CV_STRING:
        {
            // BSTRs have an int with the string buffer length (not the string length)
            // followed by the data.  The pointer to the BSTR points to the start of the
            // characters, NOT the start of the BSTR.
            WCHAR* chars = V_BSTR(src);
            int strLen = SysStringLen(V_BSTR(src));
            STRINGREF str = StringObject::NewString(chars, strLen);
            var->SetObjRef((OBJECTREF)str);
            break;
        }
        case CV_DATETIME:
            var->SetDataAsInt64(COMDateTime::DoubleDateToTicks(V_DATE(src)));
            break;

        case CV_BOOLEAN:
            var->SetDataAsInt64(V_BOOL(src) == VARIANT_FALSE ? FALSE : TRUE);
            break;

        case CV_DECIMAL:
        {
            MethodTable * pDecimalMT = GetTypeHandleForCVType(CV_DECIMAL).GetMethodTable();
            _ASSERTE(pDecimalMT);
            OBJECTREF pDecimalRef = AllocateObject(pDecimalMT);

            *(DECIMAL *) pDecimalRef->GetData() = V_DECIMAL(src);
            var->SetObjRef(pDecimalRef);
            break;
        }

        // All types less than 4 bytes need an explicit cast from their original
        // type to be sign extended to 8 bytes.  This makes Variant's ToInt32
        // function simpler for these types.
        case CV_I1:
            var->SetDataAsInt64(V_I1(src));
            break;

        case CV_U1:
            var->SetDataAsInt64(V_UI1(src));
            break;

        case CV_I2:
            var->SetDataAsInt64(V_I2(src));
            break;

        case CV_U2:
            var->SetDataAsInt64(V_UI2(src));
            break;

        case CV_EMPTY:
        case CV_NULL:
            // Must set up the Variant's m_or to the appropriate classes.
            // Note that OleAut doesn't have any VT_MISSING.
            VariantData::NewVariant(var, type, NULL DEBUG_ARG(TRUE));
            break;

        case CV_OBJECT:
        {
            // Convert the IUnknown pointer to an OBJECTREF.
            OBJECTREF oref = NULL;
            GCPROTECT_BEGIN(oref);
            GetObjectRefFromComIP(&oref, V_UNKNOWN(src));
            var->SetObjRef(oref);
            GCPROTECT_END();
            break;
        }
        default:
            // Copy all the bits there, and make sure we don't do any float to int conversions.
            void* data = (void*)&(V_UI1(src));
            var->SetData(data);
            break;
    }
}

static void OAFailed(HRESULT hr)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(FAILED(hr));
    }
    CONTRACTL_END;

    switch (hr)
    {
        case E_OUTOFMEMORY:
            COMPlusThrowOM();

        case DISP_E_BADVARTYPE:
            COMPlusThrow(kNotSupportedException, W("NotSupported_OleAutBadVarType"));

        case DISP_E_DIVBYZERO:
            COMPlusThrow(kDivideByZeroException);

        case DISP_E_OVERFLOW:
            COMPlusThrow(kOverflowException);

        case DISP_E_TYPEMISMATCH:
            COMPlusThrow(kInvalidCastException, W("InvalidCast_OATypeMismatch"));

        case E_INVALIDARG:
            COMPlusThrow(kArgumentException);

        default:
            _ASSERTE(!"Unrecognized HResult - OAVariantLib routine failed in an unexpected way!");
            COMPlusThrowHR(hr);
    }
}

extern "C" void QCALLTYPE OAVariant_ChangeType(VariantData* result, VariantData* source, LCID lcid, void* targetType, int cvType, INT16 flags)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(result));
    }
    CONTRACTL_END;

    BEGIN_QCALL;

    GCX_COOP();

    bool converted = false;

    TypeHandle thTarget = TypeHandle::FromPtr(targetType);
    if (cvType == CV_OBJECT && IsTypeRefOrDef(g_ColorClassName, thTarget.GetModule(), thTarget.GetCl()))
    {
        CVTypes sourceType = source->GetType();
        if (sourceType == CV_I4 || sourceType == CV_U4)
        {
            // Int32/UInt32 can be converted to System.Drawing.Color
            SYSTEMCOLOR SystemColor;
            ConvertOleColorToSystemColor(source->GetDataAsUInt32(), &SystemColor);

            result->SetObjRef(thTarget.AsMethodTable()->Box(&SystemColor));
            result->SetType(CV_OBJECT);

            converted = true;
        }
    }

    if (!converted)
    {
        VariantHolder ret;
        VariantHolder vOp;

        VARENUM vt = CVtoVT((CVTypes) cvType);
        ToOAVariant(source, &vOp);

        HRESULT hr = SafeVariantChangeTypeEx(&ret, &vOp, lcid, flags, static_cast<VARTYPE>(vt));

        if (FAILED(hr))
            OAFailed(hr);

        if ((CVTypes) cvType == CV_CHAR)
        {
            result->SetType(CV_CHAR);
            result->SetDataAsUInt16(V_UI2(&ret));
        }
        else
        {
            FromOAVariant(&ret, result);
        }
    }

    END_QCALL;
}

#endif // FEATURE_COMINTEROP
