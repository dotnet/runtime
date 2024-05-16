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


//These types must be kept in sync with the CorElementTypes defined in cor.h
//NOTE: If you add values to this enum you need to look at OAVariant.cpp.  There is
//      a mapping between CV type and VT types found there.
//NOTE: This is also found in a table in OleVariant.cpp.
//NOTE: These are also found in Variant.cs
typedef enum
{
    CV_EMPTY               = 0x0,                   // CV_EMPTY
    CV_VOID                = ELEMENT_TYPE_VOID,
    CV_BOOLEAN             = ELEMENT_TYPE_BOOLEAN,
    CV_CHAR                = ELEMENT_TYPE_CHAR,
    CV_I1                  = ELEMENT_TYPE_I1,
    CV_U1                  = ELEMENT_TYPE_U1,
    CV_I2                  = ELEMENT_TYPE_I2,
    CV_U2                  = ELEMENT_TYPE_U2,
    CV_I4                  = ELEMENT_TYPE_I4,
    CV_U4                  = ELEMENT_TYPE_U4,
    CV_I8                  = ELEMENT_TYPE_I8,
    CV_U8                  = ELEMENT_TYPE_U8,
    CV_R4                  = ELEMENT_TYPE_R4,
    CV_R8                  = ELEMENT_TYPE_R8,
    CV_STRING              = ELEMENT_TYPE_STRING,

    // For the rest, we map directly if it is defined in CorHdr.h and fill
    //  in holes for the rest.
    CV_PTR                 = ELEMENT_TYPE_PTR,
    CV_DATETIME            = 0x10,      // ELEMENT_TYPE_BYREF
    CV_TIMESPAN            = 0x11,      // ELEMENT_TYPE_VALUETYPE
    CV_OBJECT              = ELEMENT_TYPE_CLASS,
    CV_DECIMAL             = 0x13,      // ELEMENT_TYPE_UNUSED1
    CV_CURRENCY            = 0x14,      // ELEMENT_TYPE_ARRAY
    CV_ENUM                = 0x15,      //
    CV_MISSING             = 0x16,      //
    CV_NULL                = 0x17,      //
    CV_LAST                = 0x18,      //
} CVTypes;

//Mapping from CVType to type handle. Used for conversion between the two internally.
extern const BinderClassID CVTypeToBinderClassID[];

inline TypeHandle GetTypeHandleForCVType(CVTypes elemType)
{
    CONTRACT (TypeHandle)
    {
        WRAPPER(THROWS);
        WRAPPER(GC_TRIGGERS);
        MODE_ANY;
        PRECONDITION(elemType < CV_LAST);
    }
    CONTRACT_END;

    RETURN TypeHandle(CoreLibBinder::GetClass(CVTypeToBinderClassID[elemType]));
}

// Use this very carefully.  There is not a direct mapping between
//  CorElementType and CVTypes for a bunch of things.  In this case
//  we return CV_LAST.  You need to check this at the call site.
extern CVTypes CorElementTypeToCVTypes(CorElementType type);


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
    static void InsertContentsIntoByrefVariant(VARIANT* pSrcVar, VARIANT* pByrefVar);
    static void CreateByrefVariantForVariant(VARIANT* pSrcVar, VARIANT* pByrefVar);
#endif // FEATURE_COMINTEROP

    static CVTypes GetCVTypeForVarType(VARTYPE vt);
    static VARTYPE GetVarTypeForCVType(CVTypes);
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
