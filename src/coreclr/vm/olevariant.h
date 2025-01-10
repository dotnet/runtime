// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: OleVariant.h
//

//


#ifndef _H_OLEVARIANT_
#define _H_OLEVARIANT_


// The COM interop native array marshaler is built on top of VT_* types.
// The P/Invoke marshaler supports marshaling to WINBOOL's and ANSICHAR's.
// This is an annoying workaround to shoehorn these non-OleAut types into
// the COM interop marshaler.
#define VTHACK_INSPECTABLE     247
#define VTHACK_HSTRING         248
#define VTHACK_CBOOL           250
#define VTHACK_NONBLITTABLERECORD 251
#define VTHACK_BLITTABLERECORD 252
#define VTHACK_ANSICHAR        253
#define VTHACK_WINBOOL         254


class OleVariant
{
  public:

#ifdef FEATURE_COMINTEROP
    // New variant conversion
    static void MarshalOleVariantForObject(OBJECTREF * const & pObj, VARIANT *pOle);
    static void MarshalObjectForOleVariant(const VARIANT *pOle, OBJECTREF * const & pObj);
    static void MarshalOleRefVariantForObject(OBJECTREF *pObj, VARIANT *pOle);

    // Helper functions to convert BSTR to managed strings.
    static void AllocateEmptyStringForBSTR(BSTR bstr, STRINGREF *pStringObj);
    static void ConvertContentsBSTRToString(BSTR bstr, STRINGREF *pStringObj);
    static void ConvertBSTRToString(BSTR bstr, STRINGREF *pStringObj);

    // Helper functions to convert managed strings to BSTRs.
    static BSTR AllocateEmptyBSTRForString(STRINGREF *pStringObj);
    static void ConvertContentsStringToBSTR(STRINGREF *pStringObj, BSTR bstr);
    static BSTR ConvertStringToBSTR(STRINGREF *pStringObj);
    static void MarshalObjectForOleVariantUncommon(const VARIANT *pOle, OBJECTREF * const & pObj);
    static void MarshalOleVariantForObjectUncommon(OBJECTREF * const & pObj, VARIANT *pOle);
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_COMINTEROP
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
                                            PCODE pManagedMarshalerCode,
                                            BOOL fSafeArrayIsValid = TRUE);

    static void MarshalArrayRefForSafeArray(SAFEARRAY* pSafeArray,
                                            BASEARRAYREF* pArrayRef,
                                            VARTYPE vt,
                                            PCODE pManagedMarshalerCode,
                                            MethodTable* pInterfaceMT);

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
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_COMINTEROP
    static BOOL CheckVariant(VARIANT *pOle);

    // Type conversion utilities
    static void ExtractContentsFromByrefVariant(VARIANT* pByrefVar, VARIANT* pDestVar);
    static void InsertContentsIntoByRefVariant(VARIANT* pSrcVar, VARIANT* pByrefVar);
    static void CreateByrefVariantForVariant(VARIANT* pSrcVar, VARIANT* pByrefVar);
#endif // FEATURE_COMINTEROP

    static TypeHandle GetTypeHandleForVarType(VARTYPE vt);
    static VARTYPE GetVarTypeForTypeHandle(TypeHandle typeHnd);

    static VARTYPE GetVarTypeForValueClassArrayName(LPCUTF8 pArrayClassName);
    static VARTYPE GetElementVarTypeForArrayRef(BASEARRAYREF pArrayRef);

    // Note that Rank == 0 means SZARRAY (that is rank 1, no lower bounds)
    static TypeHandle GetArrayForVarType(VARTYPE vt, TypeHandle elemType, unsigned rank=0);
    static UINT GetElementSizeForVarType(VARTYPE vt, MethodTable* pInterfaceMT);
    static MethodTable* GetNativeMethodTableForVarType(VARTYPE vt, MethodTable* pManagedMT);

#ifdef FEATURE_COMINTEROP
    // Determine the element type of the objects being wrapped by an array of wrappers.
    static TypeHandle GetWrappedArrayElementType(BASEARRAYREF* pArray);

    // Determines the element type of an array taking wrappers into account. This means
    // that is an array of wrappers is passed in, the returned element type will be that
    // of the wrapped objects, not of the wrappers.
    static TypeHandle GetArrayElementTypeWrapperAware(BASEARRAYREF* pArray);

    // Determine the type of the elements for a safe array of records.
    static TypeHandle GetElementTypeForRecordSafeArray(SAFEARRAY* pSafeArray);

    // Helper called from MarshalIUnknownArrayComToOle and MarshalIDispatchArrayComToOle.
    static void MarshalInterfaceArrayComToOleHelper(BASEARRAYREF* pComArray, void* oleArray,
                                                    MethodTable* pElementMT, BOOL bDefaultIsDispatch,
                                                    SIZE_T cElements);
#endif // FEATURE_COMINTEROP

    struct Marshaler
    {
        void (*OleToComArray)(void* oleArray, BASEARRAYREF* pComArray, MethodTable* pInterfaceMT, PCODE pManagedMarshalerCode);
        void (*ComToOleArray)(BASEARRAYREF* pComArray, void* oleArray, MethodTable* pInterfaceMT,
        	                  BOOL fBestFitMapping, BOOL fThrowOnUnmappableChar,
                              BOOL fOleArrayIsValid,SIZE_T cElements,
                              PCODE pManagedMarshalerCode);
        void (*ClearOleArray)(void* oleArray, SIZE_T cElements, MethodTable* pInterfaceMT, PCODE pManagedMarshalerCode);
    };

    static const Marshaler* GetMarshalerForVarType(VARTYPE vt, BOOL fThrow);

    static void MarshalVariantArrayComToOle(BASEARRAYREF* pComArray, void* oleArray,
                                            MethodTable* pInterfaceMT, BOOL fBestFitMapping,
                                            BOOL fThrowOnUnmappableChar, BOOL fMarshalByrefArgOnly,
                                            BOOL fOleArrayIsValid, int nOleArrayStepLength = 1);

private:


    // Specific marshaler functions

    static void MarshalBoolArrayOleToCom(void *oleArray, BASEARRAYREF* pComArray,
                                            MethodTable* pInterfaceMT, PCODE pManagedMarshalerCode);
    static void MarshalBoolArrayComToOle(BASEARRAYREF* pComArray, void* oleArray,
                                         MethodTable* pInterfaceMT, BOOL fBestFitMapping,
                                         BOOL fThrowOnUnmappableChar, BOOL fOleArrayIsValid,
                                         SIZE_T cElements, PCODE pManagedMarshalerCode);

    static void MarshalWinBoolArrayOleToCom(void* oleArray, BASEARRAYREF* pComArray,
                                            MethodTable* pInterfaceMT, PCODE pManagedMarshalerCode);
    static void MarshalWinBoolArrayComToOle(BASEARRAYREF* pComArray, void* oleArray,
                                            MethodTable* pInterfaceMT, BOOL fBestFitMapping,
                                            BOOL fThrowOnUnmappableChar, BOOL fOleArrayValid,
                                            SIZE_T cElements, PCODE pManagedMarshalerCode);
    static void MarshalCBoolArrayOleToCom(void* oleArray, BASEARRAYREF* pComArray,
                                            MethodTable* pInterfaceMT, PCODE pManagedMarshalerCode);
    static void MarshalCBoolArrayComToOle(BASEARRAYREF* pComArray, void* oleArray,
                                          MethodTable* pInterfaceMT, BOOL fBestFitMapping,
                                          BOOL fThrowOnUnmappableChar, BOOL fOleArrayValid,
                                          SIZE_T cElements, PCODE pManagedMarshalerCode);

    static void MarshalAnsiCharArrayOleToCom(void* oleArray, BASEARRAYREF* pComArray,
                                            MethodTable* pInterfaceMT, PCODE pManagedMarshalerCode);
    static void MarshalAnsiCharArrayComToOle(BASEARRAYREF* pComArray, void* oleArray,
                                            MethodTable* pInterfaceMT, BOOL fBestFitMapping,
                                            BOOL fThrowOnUnmappableChar, BOOL fOleArrayValid,
                                            SIZE_T cElements, PCODE pManagedMarshalerCode);

#ifdef FEATURE_COMINTEROP
    static void MarshalIDispatchArrayComToOle(BASEARRAYREF* pComArray, void* oleArray,
                                            MethodTable* pInterfaceMT, BOOL fBestFitMapping,
                                            BOOL fThrowOnUnmappableChar, BOOL fOleArrayValid,
                                            SIZE_T cElements, PCODE pManagedMarshalerCode);
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_COMINTEROP
    static void MarshalBSTRArrayOleToCom(void* oleArray, BASEARRAYREF* pComArray,
                                            MethodTable* pInterfaceMT, PCODE pManagedMarshalerCode);
    static void MarshalBSTRArrayComToOle(BASEARRAYREF* pComArray, void* oleArray,
                                            MethodTable* pInterfaceMT, BOOL fBestFitMapping,
                                            BOOL fThrowOnUnmappableChar, BOOL fOleArrayValid,
                                            SIZE_T cElements, PCODE pManagedMarshalerCode);
    static void ClearBSTRArray(void* oleArray, SIZE_T cElements, MethodTable* pInterfaceMT, PCODE pManagedMarshalerCode);
#endif // FEATURE_COMINTEROP

    static void MarshalNonBlittableRecordArrayOleToCom(void* oleArray, BASEARRAYREF* pComArray,
                                            MethodTable* pInterfaceMT, PCODE pManagedMarshalerCode);
    static void MarshalNonBlittableRecordArrayComToOle(BASEARRAYREF* pComArray, void* oleArray,
                                            MethodTable* pInterfaceMT, BOOL fBestFitMapping,
                                            BOOL fThrowOnUnmappableChar, BOOL fOleArrayValid,
                                            SIZE_T cElements, PCODE pManagedMarshalerCode);
    static void ClearNonBlittableRecordArray(void* oleArray,
                                             SIZE_T cElements, MethodTable* pInterfaceMT,
                                             PCODE pManagedMarshalerCode);

    static void MarshalLPWSTRArrayOleToCom(void* oleArray, BASEARRAYREF* pComArray,
                                            MethodTable* pInterfaceMT, PCODE pManagedMarshalerCode);
    static void MarshalLPWSTRRArrayComToOle(BASEARRAYREF* pComArray, void* oleArray,
                                            MethodTable* pInterfaceMT, BOOL fBestFitMapping,
                                            BOOL fThrowOnUnmappableChar, BOOL fOleArrayValid,
                                            SIZE_T cElements, PCODE pManagedMarshalerCode);
    static void ClearLPWSTRArray(void* oleArray,
                                 SIZE_T cElements, MethodTable* pInterfaceMT, PCODE pManagedMarshalerCode);

    static void MarshalLPSTRArrayOleToCom(void* oleArray, BASEARRAYREF* pComArray,
                                            MethodTable* pInterfaceMT, PCODE pManagedMarshalerCode);
    static void MarshalLPSTRRArrayComToOle(BASEARRAYREF* pComArray, void* oleArray,
                                            MethodTable* pInterfaceMT, BOOL fBestFitMapping,
                                            BOOL fThrowOnUnmappableChar, BOOL fOleArrayValid,
                                            SIZE_T cElements, PCODE pManagedMarshalerCode);
    static void ClearLPSTRArray(void* oleArray,
                                SIZE_T cElements, MethodTable* pInterfaceMT, PCODE pManagedMarshalerCode);

    static void MarshalDateArrayOleToCom(void* oleArray, BASEARRAYREF* pComArray,
                                            MethodTable* pInterfaceMT, PCODE pManagedMarshalerCode);
    static void MarshalDateArrayComToOle(BASEARRAYREF* pComArray, void* oleArray,
                                            MethodTable* pInterfaceMT, BOOL fBestFitMapping,
                                            BOOL fThrowOnUnmappableChar, BOOL fOleArrayValid,
                                            SIZE_T cElements, PCODE pManagedMarshalerCode);

    static void MarshalRecordArrayOleToCom(void* oleArray, BASEARRAYREF* pComArray, MethodTable* pElementMT, PCODE pManagedMarshalerCode);
    static void MarshalRecordArrayComToOle(BASEARRAYREF* pComArray, void* oleArray, MethodTable* pElementMT,
                                           BOOL fBestFitMapping, BOOL fThrowOnUnmappableChar,
                                           BOOL fOleArrayValid,
                                           SIZE_T cElements, PCODE pManagedMarshalerCode);
    static void ClearRecordArray(void* oleArray, SIZE_T cElements, MethodTable* pElementMT, PCODE pManagedMarshalerCode);

#ifdef FEATURE_COMINTEROP
    static HRESULT MarshalCommonOleRefVariantForObject(OBJECTREF *pObj, VARIANT *pOle);
    static void MarshalInterfaceArrayOleToCom(void* oleArray, BASEARRAYREF* pComArray,
                                            MethodTable* pInterfaceMT, PCODE pManagedMarshalerCode);
    static void MarshalIUnknownArrayComToOle(BASEARRAYREF* pComArray, void* oleArray,
                                            MethodTable* pInterfaceMT, BOOL fBestFitMapping,
                                            BOOL fThrowOnUnmappableChar, BOOL fOleArrayValid,
                                            SIZE_T cElements, PCODE pManagedMarshalerCode);
    static void ClearInterfaceArray(void* oleArray, SIZE_T cElements, MethodTable* pInterfaceMT, PCODE pManagedMarshalerCode);

#ifdef FEATURE_COMINTEROP
    static void MarshalRecordVariantOleToObject(const VARIANT* pOleVariant, OBJECTREF * const & pComVariant);
#endif

    static void MarshalCurrencyArrayOleToCom(void* oleArray, BASEARRAYREF* pComArray,
                                            MethodTable* pInterfaceMT, PCODE pManagedMarshalerCode);
    static void MarshalCurrencyArrayComToOle(BASEARRAYREF* pComArray, void* oleArray,
                                            MethodTable* pInterfaceMT, BOOL fBestFitMapping,
                                            BOOL fThrowOnUnmappableChar, BOOL fOleArrayValid,
                                            SIZE_T cElements, PCODE pManagedMarshalerCode);

    static void MarshalVariantArrayOleToCom(void* oleArray, BASEARRAYREF* pComArray,
                                            MethodTable* pInterfaceMT, PCODE pManagedMarshalerCode);
    static void MarshalVariantArrayComToOle(BASEARRAYREF* pComArray, void* oleArray,
                                            MethodTable* pInterfaceMT, BOOL fBestFitMapping,
                                            BOOL fThrowOnUnmappableChar, BOOL fOleArrayValid,
                                            SIZE_T cElements, PCODE pManagedMarshalerCode);
    static void ClearVariantArray(void* oleArray, SIZE_T cElements, MethodTable* pInterfaceMT, PCODE pManagedMarshalerCode);

#ifdef FEATURE_COMINTEROP
    static void MarshalArrayVariantOleToObject(const VARIANT* pOleVariant, OBJECTREF * const & pObj);
    static void MarshalArrayVariantOleRefToObject(const VARIANT* pOleVariant, OBJECTREF * const & pObj);
    static void MarshalArrayVariantObjectToOle(OBJECTREF * const & pObj, VARIANT* pOleVariant);
#endif
#endif // FEATURE_COMINTEROP
};

#endif
