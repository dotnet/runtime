// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: OleVariant.h
//

#ifndef _H_OLEVARIANT_
#define _H_OLEVARIANT_

#ifndef FEATURE_COMINTEROP
#error FEATURE_COMINTEROP is required for this file
#endif // FEATURE_COMINTEROP

class OleVariant
{
  public:

    // New variant conversion
    static void MarshalOleVariantForObject(OBJECTREF * const & pObj, VARIANT *pOle);
    static void MarshalObjectForOleVariant(const VARIANT *pOle, OBJECTREF * const & pObj);
    static void MarshalOleRefVariantForObject(OBJECTREF *pObj, VARIANT *pOle);

    static void ConvertBSTRToString(BSTR bstr, STRINGREF *pStringObj);
    static BSTR ConvertStringToBSTR(STRINGREF *pStringObj);

    static void MarshalObjectForOleVariantUncommon(const VARIANT *pOle, OBJECTREF * const & pObj);
    static void MarshalOleVariantForObjectUncommon(OBJECTREF * const & pObj, VARIANT *pOle);

    // Safearray conversion

    static SAFEARRAY* CreateSafeArrayDescriptorForArrayRef(BASEARRAYREF* pArrayRef, VARTYPE vt,
                                                MethodTable* pInterfaceMT = NULL);

    static SAFEARRAY* CreateSafeArrayForArrayRef(BASEARRAYREF* pArrayRef, VARTYPE vt,
                                                MethodTable* pInterfaceMT = NULL);

    static BASEARRAYREF CreateArrayRefForSafeArray(SAFEARRAY* pSafeArray, VARTYPE vt,
                                                MethodTable* pElementMT);

    static void MarshalSafeArrayForArrayRef(BASEARRAYREF* pArrayRef,
                                            SAFEARRAY* pSafeArray,
                                            VARTYPE vt,
                                            MethodTable* pInterfaceMT,
                                            PCODE pConvertContentsCode);

    static void MarshalArrayRefForSafeArray(SAFEARRAY* pSafeArray,
                                            BASEARRAYREF* pArrayRef,
                                            VARTYPE vt,
                                            MethodTable* pInterfaceMT,
                                            PCODE pConvertContentsCode);

    // Helper function to convert a boxed value class to an OLE variant.
    static void ConvertValueClassToVariant(OBJECTREF *pBoxedValueClass, VARIANT *pOleVariant);

    // Helper function to transpose the data in a multidimensionnal array.
    static void TransposeArrayData(BYTE *pDestData, BYTE *pSrcData, SIZE_T dwNumComponents, SIZE_T dwComponentSize, SAFEARRAY *pSafeArray, BOOL bSafeArrayToMngArray);

    // Helper to determine if an array is an array of wrappers.
    static BOOL IsArrayOfWrappers(BASEARRAYREF *pArray, BOOL *pbOfInterfaceWrappers);

    // Helper to extract the wrapped objects from an array.
    static BASEARRAYREF ExtractWrappedObjectsFromArray(BASEARRAYREF *pArray);

    static HRESULT ClearAndInsertContentsIntoByrefRecordVariant(VARIANT* pOle, OBJECTREF* pObj);

    static BOOL IsValidArrayForSafeArrayElementType(BASEARRAYREF* pArrayRef, VARTYPE vtExpected);

    static BOOL CheckVariant(VARIANT *pOle);

    // Type conversion utilities
    static void ExtractContentsFromByrefVariant(VARIANT* pByrefVar, VARIANT* pDestVar);
    static void InsertContentsIntoByRefVariant(VARIANT* pSrcVar, VARIANT* pByrefVar);
    static void CreateByrefVariantForVariant(VARIANT* pSrcVar, VARIANT* pByrefVar);

    static VARTYPE GetElementVarTypeForArrayRef(BASEARRAYREF pArrayRef);

    // Note that Rank == 0 means SZARRAY (that is rank 1, no lower bounds)
    static TypeHandle GetArrayForVarType(VARTYPE vt, TypeHandle elemType, unsigned rank=0);
    static UINT GetElementSizeForVarType(VARTYPE vt, MethodTable* pInterfaceMT);

    // Determine the element type of the objects being wrapped by an array of wrappers.
    static TypeHandle GetWrappedArrayElementType(BASEARRAYREF* pArray);

    // Determines the element type of an array taking wrappers into account. This means
    // that is an array of wrappers is passed in, the returned element type will be that
    // of the wrapped objects, not of the wrappers.
    static TypeHandle GetArrayElementTypeWrapperAware(BASEARRAYREF* pArray);

    // Determine the type of the elements for a safe array of records.
    static TypeHandle GetElementTypeForRecordSafeArray(SAFEARRAY* pSafeArray);

    static void MarshalVarArgVariantArrayToOle(PTRARRAYREF* pComArray, VARIANT* oleArray);

private:

    static HRESULT MarshalCommonOleRefVariantForObject(OBJECTREF *pObj, VARIANT *pOle);

    static void MarshalRecordVariantOleToObject(const VARIANT* pOleVariant, OBJECTREF * const & pComVariant);

    static void MarshalArrayVariantOleToObject(const VARIANT* pOleVariant, OBJECTREF * const & pObj);
    static void MarshalArrayVariantOleRefToObject(const VARIANT* pOleVariant, OBJECTREF * const & pObj);
    static void MarshalArrayVariantObjectToOle(OBJECTREF * const & pObj, VARIANT* pOleVariant);
};

// Returns the instantiated MethodDesc for a StubHelpers array marshalling method
// (e.g. ConvertArrayContentsToUnmanaged/ConvertArrayContentsToManaged) for a given
// SAFEARRAY VARTYPE and element MethodTable.
MethodDesc* GetInstantiatedSafeArrayMethod(BinderMethodID methodId, VARTYPE vt, MethodTable* pElementMT, BOOL bHeterogeneous, BOOL bNativeDataValid = FALSE);

extern "C" void QCALLTYPE Variant_ConvertValueTypeToRecord(QCall::ObjectHandleOnStack obj, VARIANT* pOle);

#endif
