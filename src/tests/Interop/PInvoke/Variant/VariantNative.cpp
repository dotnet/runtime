// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <xplatform.h>
#include "platformdefines.h"

#define LCID_ENGLISH MAKELCID(MAKELANGID(0x09, 0x01), SORT_DEFAULT)

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByValue_Byte(VARIANT value, uint8_t expected)
{
    if (value.vt != VT_UI1)
    {
        printf("Invalid format. Expected VT_UI1.\n");
        return FALSE;
    }

    return value.bVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByValue_SByte(VARIANT value, CHAR expected)
{
    if (value.vt != VT_I1)
    {
        printf("Invalid format. Expected VT_I1.\n");
        return FALSE;
    }

    return value.cVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByValue_Int16(VARIANT value, int16_t expected)
{
    if (value.vt != VT_I2)
    {
        printf("Invalid format. Expected VT_I2.\n");
        return FALSE;
    }

    return value.iVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByValue_UInt16(VARIANT value, uint16_t expected)
{
    if (value.vt != VT_UI2)
    {
        printf("Invalid format. Expected VT_UI2.\n");
        return FALSE;
    }

    return value.uiVal == expected ? TRUE : FALSE;
}
extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByValue_Int32(VARIANT value, int32_t expected)
{
    if (value.vt != VT_I4)
    {
        printf("Invalid format. Expected VT_I4.\n");
        return FALSE;
    }

    return value.lVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByValue_UInt32(VARIANT value, uint32_t expected)
{
    if (value.vt != VT_UI4)
    {
        printf("Invalid format. Expected VT_UI4.\n");
        return FALSE;
    }

    return value.ulVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByValue_Int64(VARIANT value, int64_t expected)
{
    if (value.vt != VT_I8)
    {
        printf("Invalid format. Expected VT_I8.\n");
        return FALSE;
    }

    return value.llVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByValue_UInt64(VARIANT value, uint64_t expected)
{
    if (value.vt != VT_UI8)
    {
        printf("Invalid format. Expected VT_UI8.\n");
        return FALSE;
    }

    return value.ullVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByValue_Single(VARIANT value, FLOAT expected)
{
    if (value.vt != VT_R4)
    {
        printf("Invalid format. Expected VT_R4.\n");
        return FALSE;
    }

    return value.fltVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByValue_Double(VARIANT value, DOUBLE expected)
{
    if (value.vt != VT_R8)
    {
        printf("Invalid format. Expected VT_R8.\n");
        return FALSE;
    }

    return value.dblVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByValue_Char(VARIANT value, WCHAR expected)
{
    if (value.vt != VT_UI2)
    {
        printf("Invalid format. Expected VT_UI2.\n");
        return FALSE;
    }

    return value.uiVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByValue_String(VARIANT value, BSTR expected)
{
    if (value.vt != VT_BSTR)
    {
        printf("Invalid format. Expected VT_BSTR.\n");
        return FALSE;
    }

    if (value.bstrVal == NULL || expected == NULL)
    {
        return value.bstrVal == NULL && expected == NULL;
    }

    size_t len = TP_SysStringByteLen(value.bstrVal);

    return len == TP_SysStringByteLen(expected) && memcmp(value.bstrVal, expected, len) == 0;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByValue_Object(VARIANT value)
{

    if (value.vt != VT_DISPATCH)
    {
        printf("Invalid format. Expected VT_DISPATCH.\n");
        return FALSE;
    }


    IDispatch* obj = value.pdispVal;

    if (obj == NULL)
    {
        printf("Marshal_ByValue (Native side) received an invalid IDispatch pointer\n");
        return FALSE;
    }

    obj->AddRef();

    obj->Release();

    return TRUE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByValue_Object_IUnknown(VARIANT value)
{

    if (value.vt != VT_UNKNOWN)
    {
        printf("Invalid format. Expected VT_UNKNOWN.\n");
        return FALSE;
    }


    IUnknown* obj = value.punkVal;

    if (obj == NULL)
    {
        printf("Marshal_ByValue (Native side) received an invalid IUnknown pointer\n");
        return FALSE;
    }

    obj->AddRef();

    obj->Release();

    return TRUE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByValue_Missing(VARIANT value)
{
    if (value.vt != VT_ERROR)
    {
        printf("Invalid format. Expected VT_ERROR.\n");
        return FALSE;
    }

    return value.scode == DISP_E_PARAMNOTFOUND ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByValue_Empty(VARIANT value)
{
    if (value.vt != VT_EMPTY)
    {
        printf("Invalid format. Expected VT_EMPTY. \n");
        return FALSE;
    }

    return TRUE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByValue_Boolean(VARIANT value, VARIANT_BOOL expected)
{
    if (value.vt != VT_BOOL)
    {
        printf("Invalid format. Expected VT_BOOL.\n");
        return FALSE;
    }

    return value.boolVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByValue_DateTime(VARIANT value, DATE expected)
{
    if (value.vt != VT_DATE)
    {
        printf("Invalid format. Expected VT_BYREF.\n");
        return FALSE;
    }

    return value.date == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByValue_Decimal(VARIANT value, DECIMAL expected)
{
    if (value.vt != VT_DECIMAL)
    {
        printf("Invalid format. Expected VT_DECIMAL.\n");
        return FALSE;
    }

    expected.wReserved = VT_DECIMAL; // The wReserved field in DECIMAL overlaps with the vt field in VARIANT

    return memcmp(&value.decVal, &expected, sizeof(DECIMAL)) == 0;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByValue_Currency(VARIANT value, CY expected)
{
    if (value.vt != VT_CY)
    {
        printf("Invalid format. Expected VT_CY.\n");
        return FALSE;
    }

    return memcmp(&value.cyVal, &expected, sizeof(CY)) == 0;
}


extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByValue_Null(VARIANT value)
{
    if (value.vt != VT_NULL)
    {
        printf("Invalid format. Expected VT_NULL. \n");
        return FALSE;
    }

    return TRUE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByValue_Invalid(VARIANT value)
{
    return FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByRef_Byte(VARIANT* value, uint8_t expected)
{
    if (value->vt != VT_UI1)
    {
        printf("Invalid format. Expected VT_UI1.\n");
        return FALSE;
    }

    return value->bVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByRef_SByte(VARIANT* value, CHAR expected)
{
    if (value->vt != VT_I1)
    {
        printf("Invalid format. Expected VT_I1.\n");
        return FALSE;
    }

    return value->cVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByRef_Int16(VARIANT* value, int16_t expected)
{
    if (value->vt != VT_I2)
    {
        printf("Invalid format. Expected VT_I2.\n");
        return FALSE;
    }

    return value->iVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByRef_UInt16(VARIANT* value, uint16_t expected)
{
    if (value->vt != VT_UI2)
    {
        printf("Invalid format. Expected VT_UI2.\n");
        return FALSE;
    }

    return value->uiVal == expected ? TRUE : FALSE;
}
extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByRef_Int32(VARIANT* value, int32_t expected)
{
    if (value->vt != VT_I4)
    {
        printf("Invalid format. Expected VT_I4.\n");
        return FALSE;
    }

    return value->lVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByRef_UInt32(VARIANT* value, uint32_t expected)
{
    if (value->vt != VT_UI4)
    {
        printf("Invalid format. Expected VT_UI4.\n");
        return FALSE;
    }

    return value->ulVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByRef_Int64(VARIANT* value, int64_t expected)
{
    if (value->vt != VT_I8)
    {
        printf("Invalid format. Expected VT_I8.\n");
        return FALSE;
    }

    return value->llVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByRef_UInt64(VARIANT* value, uint64_t expected)
{
    if (value->vt != VT_UI8)
    {
        printf("Invalid format. Expected VT_UI8.\n");
        return FALSE;
    }

    return value->ullVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByRef_Single(VARIANT* value, FLOAT expected)
{
    if (value->vt != VT_R4)
    {
        printf("Invalid format. Expected VT_R4.\n");
        return FALSE;
    }

    return value->fltVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByRef_Double(VARIANT* value, DOUBLE expected)
{
    if (value->vt != VT_R8)
    {
        printf("Invalid format. Expected VT_R8.\n");
        return FALSE;
    }

    return value->dblVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByRef_Char(VARIANT* value, WCHAR expected)
{
    if (value->vt != VT_UI2)
    {
        printf("Invalid format. Expected VT_UI2.\n");
        return FALSE;
    }

    return value->uiVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByRef_String(VARIANT* value, BSTR expected)
{
    if (value->vt != VT_BSTR)
    {
        printf("Invalid format. Expected VT_BSTR.\n");
        return FALSE;
    }

    if (value->bstrVal == NULL || expected == NULL)
    {
        return value->bstrVal == NULL && expected == NULL;
    }

    size_t len = TP_SysStringByteLen(value->bstrVal);

    return len == TP_SysStringByteLen(expected) && memcmp(value->bstrVal, expected, len) == 0;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByRef_Object(VARIANT* value)
{

    if (value->vt != VT_DISPATCH)
    {
        printf("Invalid format. Expected VT_DISPATCH.\n");
        return FALSE;
    }


    IDispatch* obj = value->pdispVal;

    if (obj == NULL)
    {
        printf("Marshal_ByRef (Native side) received an invalid IDispatch pointer\n");
        return FALSE;
    }

    obj->AddRef();

    obj->Release();

    return TRUE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByRef_Object_IUnknown(VARIANT* value)
{

    if (value->vt != VT_UNKNOWN)
    {
        printf("Invalid format. Expected VT_UNKNOWN.\n");
        return FALSE;
    }


    IUnknown* obj = value->punkVal;

    if (obj == NULL)
    {
        printf("Marshal_ByRef (Native side) received an invalid IUnknown pointer\n");
        return FALSE;
    }

    obj->AddRef();

    obj->Release();

    return TRUE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByRef_Missing(VARIANT* value)
{
    if (value->vt != VT_ERROR)
    {
        printf("Invalid format. Expected VT_ERROR.\n");
        return FALSE;
    }

    return value->scode == DISP_E_PARAMNOTFOUND ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByRef_Empty(VARIANT* value)
{
    if (value->vt != VT_EMPTY)
    {
        printf("Invalid format. Expected VT_EMPTY. \n");
        return FALSE;
    }

    return TRUE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByRef_Boolean(VARIANT* value, VARIANT_BOOL expected)
{
    if (value->vt != VT_BOOL)
    {
        printf("Invalid format. Expected VT_BOOL.\n");
        return FALSE;
    }

    return value->boolVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByRef_DateTime(VARIANT* value, DATE expected)
{
    if (value->vt != VT_DATE)
    {
        printf("Invalid format. Expected VT_BYREF.\n");
        return FALSE;
    }

    return value->date == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByRef_Decimal(VARIANT* value, DECIMAL expected)
{
    if (value->vt != VT_DECIMAL)
    {
        printf("Invalid format. Expected VT_DECIMAL.\n");
        return FALSE;
    }

    expected.wReserved = VT_DECIMAL; // The wReserved field in DECIMAL overlaps with the vt field in VARIANT*

    return memcmp(&value->decVal, &expected, sizeof(DECIMAL)) == 0;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByRef_Currency(VARIANT* value, CY expected)
{
    if (value->vt != VT_CY)
    {
        printf("Invalid format. Expected VT_CY.\n");
        return FALSE;
    }

    return memcmp(&value->cyVal, &expected, sizeof(CY)) == 0;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ByRef_Null(VARIANT* value)
{
    if (value->vt != VT_NULL)
    {
        printf("Invalid format. Expected VT_NULL. \n");
        return FALSE;
    }

    return TRUE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Out(VARIANT* pValue, int32_t expected)
{
    if (FAILED(VariantClear(pValue)))
    {
        printf("Failed to clear pValue.\n");
        return FALSE;
    }
    pValue->vt = VT_I4;
    pValue->lVal = expected;

    return TRUE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_ChangeVariantType(VARIANT* pValue, int32_t expected)
{
    if (FAILED(VariantClear(pValue)))
    {
        printf("Failed to clear pValue.\n");
        return FALSE;
    }
    pValue->vt = VT_I4;
    pValue->lVal = expected;

    return TRUE;
}

struct VariantWrapper
{
    VARIANT value;
};

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByValue_Byte(VariantWrapper wrapper, uint8_t expected)
{
    if (wrapper.value.vt != VT_UI1)
    {
        printf("Invalid format. Expected VT_UI1.\n");
        return FALSE;
    }

    return wrapper.value.bVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByValue_SByte(VariantWrapper wrapper, CHAR expected)
{
    if (wrapper.value.vt != VT_I1)
    {
        printf("Invalid format. Expected VT_I1.\n");
        return FALSE;
    }

    return wrapper.value.cVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByValue_Int16(VariantWrapper wrapper, int16_t expected)
{
    if (wrapper.value.vt != VT_I2)
    {
        printf("Invalid format. Expected VT_I2.\n");
        return FALSE;
    }

    return wrapper.value.iVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByValue_UInt16(VariantWrapper wrapper, uint16_t expected)
{
    if (wrapper.value.vt != VT_UI2)
    {
        printf("Invalid format. Expected VT_UI2.\n");
        return FALSE;
    }

    return wrapper.value.uiVal == expected ? TRUE : FALSE;
}
extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByValue_Int32(VariantWrapper wrapper, int32_t expected)
{
    if (wrapper.value.vt != VT_I4)
    {
        printf("Invalid format. Expected VT_I4.\n");
        return FALSE;
    }

    return wrapper.value.lVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByValue_UInt32(VariantWrapper wrapper, uint32_t expected)
{
    if (wrapper.value.vt != VT_UI4)
    {
        printf("Invalid format. Expected VT_UI4.\n");
        return FALSE;
    }

    return wrapper.value.ulVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByValue_Int64(VariantWrapper wrapper, int64_t expected)
{
    if (wrapper.value.vt != VT_I8)
    {
        printf("Invalid format. Expected VT_I8.\n");
        return FALSE;
    }

    return wrapper.value.llVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByValue_UInt64(VariantWrapper wrapper, uint64_t expected)
{
    if (wrapper.value.vt != VT_UI8)
    {
        printf("Invalid format. Expected VT_UI8.\n");
        return FALSE;
    }

    return wrapper.value.ullVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByValue_Single(VariantWrapper wrapper, FLOAT expected)
{
    if (wrapper.value.vt != VT_R4)
    {
        printf("Invalid format. Expected VT_R4.\n");
        return FALSE;
    }

    return wrapper.value.fltVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByValue_Double(VariantWrapper wrapper, DOUBLE expected)
{
    if (wrapper.value.vt != VT_R8)
    {
        printf("Invalid format. Expected VT_R8.\n");
        return FALSE;
    }

    return wrapper.value.dblVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByValue_Char(VariantWrapper wrapper, WCHAR expected)
{
    if (wrapper.value.vt != VT_UI2)
    {
        printf("Invalid format. Expected VT_UI2.\n");
        return FALSE;
    }

    return wrapper.value.uiVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByValue_String(VariantWrapper wrapper, BSTR expected)
{
    if (wrapper.value.vt != VT_BSTR)
    {
        printf("Invalid format. Expected VT_BSTR.\n");
        return FALSE;
    }

    if (wrapper.value.bstrVal == NULL || expected == NULL)
    {
        return wrapper.value.bstrVal == NULL && expected == NULL;
    }

    size_t len = TP_SysStringByteLen(wrapper.value.bstrVal);

    return len == TP_SysStringByteLen(expected) && memcmp(wrapper.value.bstrVal, expected, len) == 0;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByValue_Object(VariantWrapper wrapper)
{

    if (wrapper.value.vt != VT_DISPATCH)
    {
        printf("Invalid format. Expected VT_DISPATCH.\n");
        return FALSE;
    }


    IDispatch* obj = wrapper.value.pdispVal;

    if (obj == NULL)
    {
        printf("Marshal_Struct_ByValue (Native side) received an invalid IDispatch pointer\n");
        return FALSE;
    }

    obj->AddRef();

    obj->Release();

    return TRUE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByValue_Object_IUnknown(VariantWrapper wrapper)
{

    if (wrapper.value.vt != VT_UNKNOWN)
    {
        printf("Invalid format. Expected VT_UNKNOWN.\n");
        return FALSE;
    }


    IUnknown* obj = wrapper.value.punkVal;

    if (obj == NULL)
    {
        printf("Marshal_Struct_ByValue (Native side) received an invalid IUnknown pointer\n");
        return FALSE;
    }

    obj->AddRef();

    obj->Release();

    return TRUE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByValue_Missing(VariantWrapper wrapper)
{
    if (wrapper.value.vt != VT_ERROR)
    {
        printf("Invalid format. Expected VT_ERROR.\n");
        return FALSE;
    }

    return wrapper.value.scode == DISP_E_PARAMNOTFOUND ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByValue_Empty(VariantWrapper wrapper)
{
    if (wrapper.value.vt != VT_EMPTY)
    {
        printf("Invalid format. Expected VT_EMPTY. \n");
        return FALSE;
    }

    return TRUE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByValue_Boolean(VariantWrapper wrapper, VARIANT_BOOL expected)
{
    if (wrapper.value.vt != VT_BOOL)
    {
        printf("Invalid format. Expected VT_BOOL.\n");
        return FALSE;
    }

    return wrapper.value.boolVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByValue_DateTime(VariantWrapper wrapper, DATE expected)
{
    if (wrapper.value.vt != VT_DATE)
    {
        printf("Invalid format. Expected VT_Struct_ByValue.\n");
        return FALSE;
    }

    return wrapper.value.date == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByValue_Decimal(VariantWrapper wrapper, DECIMAL expected)
{
    if (wrapper.value.vt != VT_DECIMAL)
    {
        printf("Invalid format. Expected VT_DECIMAL.\n");
        return FALSE;
    }

    expected.wReserved = VT_DECIMAL; // The wReserved field in DECIMAL overlaps with the vt field in VARIANT*

    return memcmp(&wrapper.value.decVal, &expected, sizeof(DECIMAL)) == 0;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByValue_Currency(VariantWrapper wrapper, CY expected)
{
    if (wrapper.value.vt != VT_CY)
    {
        printf("Invalid format. Expected VT_CY.\n");
        return FALSE;
    }

    return memcmp(&wrapper.value.cyVal, &expected, sizeof(CY)) == 0;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByValue_Null(VariantWrapper wrapper)
{
    if (wrapper.value.vt != VT_NULL)
    {
        printf("Invalid format. Expected VT_NULL. \n");
        return FALSE;
    }

    return TRUE;
}


extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByRef_Byte(VariantWrapper* pWrapper, uint8_t expected)
{
    if (pWrapper->value.vt != VT_UI1)
    {
        printf("Invalid format. Expected VT_UI1.\n");
        return FALSE;
    }

    return pWrapper->value.bVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByRef_SByte(VariantWrapper* pWrapper, CHAR expected)
{
    if (pWrapper->value.vt != VT_I1)
    {
        printf("Invalid format. Expected VT_I1.\n");
        return FALSE;
    }

    return pWrapper->value.cVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByRef_Int16(VariantWrapper* pWrapper, int16_t expected)
{
    if (pWrapper->value.vt != VT_I2)
    {
        printf("Invalid format. Expected VT_I2.\n");
        return FALSE;
    }

    return pWrapper->value.iVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByRef_UInt16(VariantWrapper* pWrapper, uint16_t expected)
{
    if (pWrapper->value.vt != VT_UI2)
    {
        printf("Invalid format. Expected VT_UI2.\n");
        return FALSE;
    }

    return pWrapper->value.uiVal == expected ? TRUE : FALSE;
}
extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByRef_Int32(VariantWrapper* pWrapper, int32_t expected)
{
    if (pWrapper->value.vt != VT_I4)
    {
        printf("Invalid format. Expected VT_I4.\n");
        return FALSE;
    }

    return pWrapper->value.lVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByRef_UInt32(VariantWrapper* pWrapper, uint32_t expected)
{
    if (pWrapper->value.vt != VT_UI4)
    {
        printf("Invalid format. Expected VT_UI4.\n");
        return FALSE;
    }

    return pWrapper->value.ulVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByRef_Int64(VariantWrapper* pWrapper, int64_t expected)
{
    if (pWrapper->value.vt != VT_I8)
    {
        printf("Invalid format. Expected VT_I8.\n");
        return FALSE;
    }

    return pWrapper->value.llVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByRef_UInt64(VariantWrapper* pWrapper, uint64_t expected)
{
    if (pWrapper->value.vt != VT_UI8)
    {
        printf("Invalid format. Expected VT_UI8.\n");
        return FALSE;
    }

    return pWrapper->value.ullVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByRef_Single(VariantWrapper* pWrapper, FLOAT expected)
{
    if (pWrapper->value.vt != VT_R4)
    {
        printf("Invalid format. Expected VT_R4.\n");
        return FALSE;
    }

    return pWrapper->value.fltVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByRef_Double(VariantWrapper* pWrapper, DOUBLE expected)
{
    if (pWrapper->value.vt != VT_R8)
    {
        printf("Invalid format. Expected VT_R8.\n");
        return FALSE;
    }

    return pWrapper->value.dblVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByRef_Char(VariantWrapper* pWrapper, WCHAR expected)
{
    if (pWrapper->value.vt != VT_UI2)
    {
        printf("Invalid format. Expected VT_UI2.\n");
        return FALSE;
    }

    return pWrapper->value.uiVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByRef_String(VariantWrapper* pWrapper, BSTR expected)
{
    if (pWrapper->value.vt != VT_BSTR)
    {
        printf("Invalid format. Expected VT_BSTR.\n");
        return FALSE;
    }

    if (pWrapper->value.bstrVal == NULL || expected == NULL)
    {
        return pWrapper->value.bstrVal == NULL && expected == NULL;
    }

    size_t len = TP_SysStringByteLen(pWrapper->value.bstrVal);

    return len == TP_SysStringByteLen(expected) && memcmp(pWrapper->value.bstrVal, expected, len) == 0;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByRef_Object(VariantWrapper* pWrapper)
{

    if (pWrapper->value.vt != VT_DISPATCH)
    {
        printf("Invalid format. Expected VT_DISPATCH.\n");
        return FALSE;
    }


    IDispatch* obj = pWrapper->value.pdispVal;

    if (obj == NULL)
    {
        printf("Marshal_Struct_ByRef (Native side) received an invalid IDispatch pointer\n");
        return FALSE;
    }

    obj->AddRef();

    obj->Release();

    return TRUE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByRef_Object_IUnknown(VariantWrapper* pWrapper)
{

    if (pWrapper->value.vt != VT_UNKNOWN)
    {
        printf("Invalid format. Expected VT_UNKNOWN.\n");
        return FALSE;
    }


    IUnknown* obj = pWrapper->value.punkVal;

    if (obj == NULL)
    {
        printf("Marshal_Struct_ByRef (Native side) received an invalid IUnknown pointer\n");
        return FALSE;
    }

    obj->AddRef();

    obj->Release();

    return TRUE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByRef_Missing(VariantWrapper* pWrapper)
{
    if (pWrapper->value.vt != VT_ERROR)
    {
        printf("Invalid format. Expected VT_ERROR.\n");
        return FALSE;
    }

    return pWrapper->value.scode == DISP_E_PARAMNOTFOUND ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByRef_Empty(VariantWrapper* pWrapper)
{
    if (pWrapper->value.vt != VT_EMPTY)
    {
        printf("Invalid format. Expected VT_EMPTY. \n");
        return FALSE;
    }

    return TRUE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByRef_Boolean(VariantWrapper* pWrapper, VARIANT_BOOL expected)
{
    if (pWrapper->value.vt != VT_BOOL)
    {
        printf("Invalid format. Expected VT_BOOL.\n");
        return FALSE;
    }

    return pWrapper->value.boolVal == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByRef_DateTime(VariantWrapper* pWrapper, DATE expected)
{
    if (pWrapper->value.vt != VT_DATE)
    {
        printf("Invalid format. Expected VT_Struct_ByRef.\n");
        return FALSE;
    }

    return pWrapper->value.date == expected ? TRUE : FALSE;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByRef_Decimal(VariantWrapper* pWrapper, DECIMAL expected)
{
    if (pWrapper->value.vt != VT_DECIMAL)
    {
        printf("Invalid format. Expected VT_DECIMAL.\n");
        return FALSE;
    }

    expected.wReserved = VT_DECIMAL; // The wReserved field in DECIMAL overlaps with the vt field in VARIANT*

    return memcmp(&pWrapper->value.decVal, &expected, sizeof(DECIMAL)) == 0;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByRef_Currency(VariantWrapper* pWrapper, CY expected)
{
    if (pWrapper->value.vt != VT_CY)
    {
        printf("Invalid format. Expected VT_CY.\n");
        return FALSE;
    }

    return memcmp(&pWrapper->value.cyVal, &expected, sizeof(CY)) == 0;
}

extern "C" BOOL DLL_EXPORT STDMETHODCALLTYPE Marshal_Struct_ByRef_Null(VariantWrapper* pWrapper)
{
    if (pWrapper->value.vt != VT_NULL)
    {
        printf("Invalid format. Expected VT_NULL. \n");
        return FALSE;
    }

    return TRUE;
}

