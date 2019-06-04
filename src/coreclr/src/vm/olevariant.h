// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
#define VTHACK_REDIRECTEDTYPE  249
#define VTHACK_CBOOL           250          
#define VTHACK_NONBLITTABLERECORD 251
#define VTHACK_BLITTABLERECORD 252
#define VTHACK_ANSICHAR        253
#define VTHACK_WINBOOL         254


//These types must be kept in sync with the CorElementTypes defined in cor.h
//NOTE: If you add values to this enum you need to look at COMOAVariant.cpp.  There is
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
    
    RETURN TypeHandle(MscorlibBinder::GetClass(CVTypeToBinderClassID[elemType]));
}

// Use this very carefully.  There is not a direct mapping between
//  CorElementType and CVTypes for a bunch of things.  In this case
//  we return CV_LAST.  You need to check this at the call site.
extern CVTypes CorElementTypeToCVTypes(CorElementType type);


#ifdef FEATURE_COMINTEROP

#include <pshpack1.h>


/***  Variant Design Restrictions  (ie, decisions we've had to re-do differently):
      1)  A Variant containing all zeros should be a valid Variant of type empty.
      2)  Variant must contain an OBJECTREF field for Objects, etc.  Since we
          have no way of expressing a union between an OBJECTREF and an int, we
          always box Decimals in a Variant.
      3)  The m_flags field is not a CVType and will contain extra bits.  People
          should use VariantData::GetType() to get the CVType.
      4)  You should use SetObjRef and GetObjRef to manipulate the OBJECTREF field.
          These will handle write barriers correctly, as well as CV_EMPTY.
      

   Empty, Missing & Null:
      Variants of type CV_EMPTY will be all zero's.  This forces us to add in
   special cases for all functions that convert a Variant into an object (such
   as copying a Variant into an Object[]).  

      Variants of type Missing and Null will have their objectref field set to 
   Missing.Value and Null.Value respectively.  This simplifies the code in 
   Variant.cs and strewn throughout the EE.  
*/

#define VARIANT_TYPE_MASK  0xFFFF
#define VT_MASK            0xFF000000
#define VT_BITSHIFT        24

struct VariantData
{
    friend class MscorlibBinder;

public:        
    static void NewVariant(VariantData * const& dest, const CVTypes type, INT64 data
                                            DEBUG_ARG(BOOL bDestIsInterior = FALSE));

    FORCEINLINE CVTypes GetType() const
    {
        LIMITED_METHOD_CONTRACT;

        return (CVTypes)(m_flags & VARIANT_TYPE_MASK);
    }

    FORCEINLINE void SetType(INT32 in)
    {
        LIMITED_METHOD_CONTRACT;
        m_flags = in;
    }

    FORCEINLINE VARTYPE GetVT() const
    {
        LIMITED_METHOD_CONTRACT;

        VARTYPE vt = (m_flags & VT_MASK) >> VT_BITSHIFT;
        if (vt & 0x80)
        {
            vt &= ~0x80;
            vt |= VT_ARRAY;
        }
        return vt;
    }

    FORCEINLINE void SetVT(VARTYPE vt)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION( !(vt & VT_BYREF) );
            PRECONDITION( (vt & ~VT_ARRAY) < 128 );
        }
        CONTRACTL_END;
            
        if (vt & VT_ARRAY)
        {
            vt &= ~VT_ARRAY;
            vt |= 0x80;
        }
        m_flags = (m_flags & ~((INT32)VT_MASK)) | (vt << VT_BITSHIFT);
    }


    FORCEINLINE OBJECTREF GetObjRef() const
    {
        WRAPPER_NO_CONTRACT;
        
        return (OBJECTREF)m_objref;
    }

    OBJECTREF* GetObjRefPtr()
    {
        CONTRACT (OBJECTREF*)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
            POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        }
        CONTRACT_END;

        RETURN (OBJECTREF*)&m_objref;
    }

    void SetObjRef(OBJECTREF objRef)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;
        
        if (objRef!=NULL)
        {
            SetObjectReference((OBJECTREF*)&m_objref, objRef);
        }
        else
        {
            // Casting trick to avoid going thru overloaded operator= (which
            // in this case would trigger a false write barrier violation assert.)
            *(LPVOID*)(OBJECTREF*)&m_objref=NULL;
        }
    }

    FORCEINLINE void* GetData() const
    {
        LIMITED_METHOD_CONTRACT;
        return (void *)(&m_data);
    }

    FORCEINLINE INT8 GetDataAsInt8() const
    {
        LIMITED_METHOD_CONTRACT;
        return (INT8)m_data;
    }

    FORCEINLINE UINT8 GetDataAsUInt8() const
    {
        LIMITED_METHOD_CONTRACT;
        return (UINT8)m_data;
    }

    FORCEINLINE INT16 GetDataAsInt16() const
    {
        LIMITED_METHOD_CONTRACT;
        return (INT16)m_data;
    }

    FORCEINLINE UINT16 GetDataAsUInt16() const
    {
        LIMITED_METHOD_CONTRACT;
        return (UINT16)m_data;
    }

    FORCEINLINE INT32 GetDataAsInt32() const
    {
        LIMITED_METHOD_CONTRACT;
        return (INT32)m_data;
    }

    FORCEINLINE UINT32 GetDataAsUInt32() const
    {
        LIMITED_METHOD_CONTRACT;
        return (UINT32)m_data;
    }

    FORCEINLINE INT64 GetDataAsInt64() const
    {
        LIMITED_METHOD_CONTRACT;
        return (INT64)m_data;
    }

    FORCEINLINE UINT64 GetDataAsUInt64() const
    {
        LIMITED_METHOD_CONTRACT;
        return (UINT64)m_data;
    }

    FORCEINLINE void SetData(void *in)
    {
        LIMITED_METHOD_CONTRACT;

        if (!in)
            m_data=0;
        else
            m_data = *(INT64 *)in;
    }

    // When possible, please use the most specific SetDataAsXxx function.
    // This is necessary to guarantee we do sign extension correctly
    // for all types smaller than 32 bits.  R4's, R8's, U8's, DateTimes,
    // Currencies, and TimeSpans can all be treated as ints of the appropriate 
    // size - sign extension is irrelevant in those cases.
    FORCEINLINE void SetDataAsInt8(INT8 data)
    {
        LIMITED_METHOD_CONTRACT;
        m_data=data;
    }

    FORCEINLINE void SetDataAsUInt8(UINT8 data)
    {
        LIMITED_METHOD_CONTRACT;
        m_data=data;
    }

    FORCEINLINE void SetDataAsInt16(INT16 data)
    {
        LIMITED_METHOD_CONTRACT;
        m_data=data;
    }

    FORCEINLINE void SetDataAsUInt16(UINT16 data)
    {
        LIMITED_METHOD_CONTRACT;
        m_data=data;
    }

    FORCEINLINE void SetDataAsInt32(INT32 data)
    {
        LIMITED_METHOD_CONTRACT;
        m_data=data;
    }

    FORCEINLINE void SetDataAsUInt32(UINT32 data)
    {
        LIMITED_METHOD_CONTRACT;
        m_data=data;
    }

    FORCEINLINE void SetDataAsInt64(INT64 data)
    {
        LIMITED_METHOD_CONTRACT;
        m_data=data;
    }

private:
    // Typeloader reorders fields of non-blitable types. This reordering differs between 32-bit and 64-bit platforms.
#ifdef _TARGET_64BIT_
    Object*     m_objref;
    INT64       m_data;
    INT32       m_flags;
    INT32       m_padding;
#else
    INT64       m_data;
    Object*     m_objref;
    INT32       m_flags;
#endif
};

#include <poppack.h>

#endif // FEATURE_COMINTEROP


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
    static void MarshalComVariantForOleVariant(VARIANT *pOle, VariantData *pCom);
    static void MarshalOleVariantForComVariant(VariantData *pCom, VARIANT *pOle);
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
                                            BOOL fSafeArrayIsValid = TRUE);
    
    static void MarshalArrayRefForSafeArray(SAFEARRAY* pSafeArray, 
                                            BASEARRAYREF* pArrayRef,
                                            VARTYPE vt,
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

    static VARTYPE GetVarTypeForComVariant(VariantData* pComVariant);
#endif // FEATURE_COMINTEROP

    static CVTypes GetCVTypeForVarType(VARTYPE vt);
    static VARTYPE GetVarTypeForCVType(CVTypes);
    static VARTYPE GetVarTypeForTypeHandle(TypeHandle typeHnd);

    static VARTYPE GetVarTypeForValueClassArrayName(LPCUTF8 pArrayClassName);
    static VARTYPE GetElementVarTypeForArrayRef(BASEARRAYREF pArrayRef);

    // Note that Rank == 0 means SZARRAY (that is rank 1, no lower bounds)
    static TypeHandle GetArrayForVarType(VARTYPE vt, TypeHandle elemType, unsigned rank=0);
    static UINT GetElementSizeForVarType(VARTYPE vt, MethodTable* pInterfaceMT);

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
#ifdef FEATURE_COMINTEROP
        void (*OleToComVariant)(VARIANT* pOleVariant, VariantData* pComVariant);
        void (*ComToOleVariant)(VariantData* pComVariant, VARIANT* pOleVariant);
        void (*OleRefToComVariant)(VARIANT* pOleVariant, VariantData* pComVariant);
#endif // FEATURE_COMINTEROP
        void (*OleToComArray)(void* oleArray, BASEARRAYREF* pComArray, MethodTable* pInterfaceMT);
        void (*ComToOleArray)(BASEARRAYREF* pComArray, void* oleArray, MethodTable* pInterfaceMT,
        	                  BOOL fBestFitMapping, BOOL fThrowOnUnmappableChar, 
                              BOOL fOleArrayIsValid,SIZE_T cElements);
        void (*ClearOleArray)(void* oleArray, SIZE_T cElements, MethodTable* pInterfaceMT);
    };

    static const Marshaler* GetMarshalerForVarType(VARTYPE vt, BOOL fThrow);

    static void MarshalVariantArrayComToOle(BASEARRAYREF* pComArray, void* oleArray,
                                            MethodTable* pInterfaceMT, BOOL fBestFitMapping,
                                            BOOL fThrowOnUnmappableChar, BOOL fMarshalByrefArgOnly,
                                            BOOL fOleArrayIsValid, int nOleArrayStepLength = 1);

private:


    // Specific marshaler functions

    static void MarshalBoolArrayOleToCom(void *oleArray, BASEARRAYREF* pComArray,
                                            MethodTable* pInterfaceMT);
    static void MarshalBoolArrayComToOle(BASEARRAYREF* pComArray, void* oleArray,
                                         MethodTable* pInterfaceMT, BOOL fBestFitMapping,
                                         BOOL fThrowOnUnmappableChar, BOOL fOleArrayIsValid,
                                         SIZE_T cElements);

    static void MarshalWinBoolArrayOleToCom(void* oleArray, BASEARRAYREF* pComArray,
                                            MethodTable* pInterfaceMT);
    static void MarshalWinBoolArrayComToOle(BASEARRAYREF* pComArray, void* oleArray,
                                            MethodTable* pInterfaceMT, BOOL fBestFitMapping,
                                            BOOL fThrowOnUnmappableChar, BOOL fOleArrayValid, 
                                            SIZE_T cElements);
    static void MarshalCBoolVariantOleToCom(VARIANT* pOleVariant, VariantData* pComVariant);
    static void MarshalCBoolVariantComToOle(VariantData* pComVariant, VARIANT* pOleVariant);
    static void MarshalCBoolVariantOleRefToCom(VARIANT* pOleVariant, VariantData* pComVariant);
    static void MarshalCBoolArrayOleToCom(void* oleArray, BASEARRAYREF* pComArray,
                                            MethodTable* pInterfaceMT);
    static void MarshalCBoolArrayComToOle(BASEARRAYREF* pComArray, void* oleArray,
                                          MethodTable* pInterfaceMT, BOOL fBestFitMapping,
                                          BOOL fThrowOnUnmappableChar, BOOL fOleArrayValid,
                                          SIZE_T cElements);

    static void MarshalAnsiCharArrayOleToCom(void* oleArray, BASEARRAYREF* pComArray,
                                            MethodTable* pInterfaceMT);
    static void MarshalAnsiCharArrayComToOle(BASEARRAYREF* pComArray, void* oleArray,
                                            MethodTable* pInterfaceMT, BOOL fBestFitMapping,
                                            BOOL fThrowOnUnmappableChar, BOOL fOleArrayValid,
                                            SIZE_T cElements);

#ifdef FEATURE_COMINTEROP
    static void MarshalIDispatchArrayComToOle(BASEARRAYREF* pComArray, void* oleArray,
                                            MethodTable* pInterfaceMT, BOOL fBestFitMapping,
                                            BOOL fThrowOnUnmappableChar, BOOL fOleArrayValid,
                                            SIZE_T cElements);
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_COMINTEROP
    static void MarshalBSTRArrayOleToCom(void* oleArray, BASEARRAYREF* pComArray,
                                            MethodTable* pInterfaceMT);
    static void MarshalBSTRArrayComToOle(BASEARRAYREF* pComArray, void* oleArray,
                                            MethodTable* pInterfaceMT, BOOL fBestFitMapping,
                                            BOOL fThrowOnUnmappableChar, BOOL fOleArrayValid,
                                            SIZE_T cElements);
    static void ClearBSTRArray(void* oleArray, SIZE_T cElements, MethodTable* pInterfaceMT);
#endif // FEATURE_COMINTEROP

    static void MarshalNonBlittableRecordArrayOleToCom(void* oleArray, BASEARRAYREF* pComArray,
                                            MethodTable* pInterfaceMT);
    static void MarshalNonBlittableRecordArrayComToOle(BASEARRAYREF* pComArray, void* oleArray,
                                            MethodTable* pInterfaceMT, BOOL fBestFitMapping,
                                            BOOL fThrowOnUnmappableChar, BOOL fOleArrayValid,
                                            SIZE_T cElements);
    static void ClearNonBlittableRecordArray(void* oleArray, SIZE_T cElements, MethodTable* pInterfaceMT);

    static void MarshalLPWSTRArrayOleToCom(void* oleArray, BASEARRAYREF* pComArray,
                                            MethodTable* pInterfaceMT);
    static void MarshalLPWSTRRArrayComToOle(BASEARRAYREF* pComArray, void* oleArray,
                                            MethodTable* pInterfaceMT, BOOL fBestFitMapping,
                                            BOOL fThrowOnUnmappableChar, BOOL fOleArrayValid,
                                            SIZE_T cElements);
    static void ClearLPWSTRArray(void* oleArray, SIZE_T cElements, MethodTable* pInterfaceMT);

    static void MarshalLPSTRArrayOleToCom(void* oleArray, BASEARRAYREF* pComArray,
                                            MethodTable* pInterfaceMT);
    static void MarshalLPSTRRArrayComToOle(BASEARRAYREF* pComArray, void* oleArray,
                                            MethodTable* pInterfaceMT, BOOL fBestFitMapping,
                                            BOOL fThrowOnUnmappableChar, BOOL fOleArrayValid,
                                            SIZE_T cElements);
    static void ClearLPSTRArray(void* oleArray, SIZE_T cElements, MethodTable* pInterfaceMT);

    static void MarshalDateArrayOleToCom(void* oleArray, BASEARRAYREF* pComArray,
                                            MethodTable* pInterfaceMT);
    static void MarshalDateArrayComToOle(BASEARRAYREF* pComArray, void* oleArray,
                                            MethodTable* pInterfaceMT, BOOL fBestFitMapping,
                                            BOOL fThrowOnUnmappableChar, BOOL fOleArrayValid,
                                            SIZE_T cElements);

    static void MarshalRecordArrayOleToCom(void* oleArray, BASEARRAYREF* pComArray, MethodTable* pElementMT);
    static void MarshalRecordArrayComToOle(BASEARRAYREF* pComArray, void* oleArray, MethodTable* pElementMT,
                                           BOOL fBestFitMapping, BOOL fThrowOnUnmappableChar, 
                                           BOOL fOleArrayValid,
                                           SIZE_T cElements);
    static void ClearRecordArray(void* oleArray, SIZE_T cElements, MethodTable* pElementMT);

#ifdef FEATURE_COMINTEROP
    static HRESULT MarshalCommonOleRefVariantForObject(OBJECTREF *pObj, VARIANT *pOle);
    static void MarshalInterfaceArrayOleToCom(void* oleArray, BASEARRAYREF* pComArray,
                                            MethodTable* pInterfaceMT);
    static void MarshalIUnknownArrayComToOle(BASEARRAYREF* pComArray, void* oleArray,
                                            MethodTable* pInterfaceMT, BOOL fBestFitMapping,
                                            BOOL fThrowOnUnmappableChar, BOOL fOleArrayValid,
                                            SIZE_T cElements);
    static void ClearInterfaceArray(void* oleArray, SIZE_T cElements, MethodTable* pInterfaceMT);

    static void MarshalBoolVariantOleToCom(VARIANT* pOleVariant, VariantData* pComVariant);

    static void MarshalWinBoolVariantOleToCom(VARIANT* pOleVariant, VariantData* pComVariant);
    static void MarshalWinBoolVariantComToOle(VariantData* pComVariant, VARIANT* pOleVariant);
    static void MarshalWinBoolVariantOleRefToCom(VARIANT* pOleVariant, VariantData* pComVariant);

    static void MarshalAnsiCharVariantOleToCom(VARIANT* pOleVariant, VariantData* pComVariant);
    static void MarshalAnsiCharVariantComToOle(VariantData* pComVariant, VARIANT* pOleVariant);
    static void MarshalAnsiCharVariantOleRefToCom(VARIANT* pOleVariant, VariantData* pComVariant);

    static void MarshalInterfaceVariantOleToCom(VARIANT* pOleVariant, VariantData* pComVariant);
    static void MarshalInterfaceVariantComToOle(VariantData* pComVariant, VARIANT* pOleVariant);
    static void MarshalInterfaceVariantOleRefToCom(VARIANT* pOleVariant, VariantData* pComVariant);

    static void MarshalBSTRVariantOleToCom(VARIANT* pOleVariant, VariantData* pComVariant);
    static void MarshalBSTRVariantComToOle(VariantData* pComVariant, VARIANT* pOleVariant);

    static void MarshalDateVariantOleToCom(VARIANT* pOleVariant, VariantData* pComVariant);
    static void MarshalDateVariantComToOle(VariantData* pComVariant, VARIANT* pOleVariant);
    static void MarshalDateVariantOleRefToCom(VARIANT* pOleVariant, VariantData* pComVariant);

    static void MarshalDecimalVariantOleToCom(VARIANT* pOleVariant, VariantData* pComVariant);
    static void MarshalDecimalVariantComToOle(VariantData* pComVariant, VARIANT* pOleVariant);
    static void MarshalDecimalVariantOleRefToCom(VARIANT* pOleVariant, VariantData* pComVariant);

#ifdef FEATURE_CLASSIC_COMINTEROP
    static void MarshalRecordVariantOleToCom(VARIANT* pOleVariant, VariantData* pComVariant);
    static void MarshalRecordVariantComToOle(VariantData* pComVariant, VARIANT* pOleVariant);
    static void MarshalRecordVariantOleRefToCom(VARIANT* pOleVariant, VariantData* pComVariant);
#endif

    static void MarshalCurrencyVariantOleToCom(VARIANT* pOleVariant, VariantData* pComVariant);
    static void MarshalCurrencyVariantComToOle(VariantData* pComVariant, VARIANT* pOleVariant);
    static void MarshalCurrencyVariantOleRefToCom(VARIANT* pOleVariant, VariantData* pComVariant);
    static void MarshalCurrencyArrayOleToCom(void* oleArray, BASEARRAYREF* pComArray,
                                            MethodTable* pInterfaceMT);
    static void MarshalCurrencyArrayComToOle(BASEARRAYREF* pComArray, void* oleArray,
                                            MethodTable* pInterfaceMT, BOOL fBestFitMapping,
                                            BOOL fThrowOnUnmappableChar, BOOL fOleArrayValid,
                                            SIZE_T cElements);

    static void MarshalVariantArrayOleToCom(void* oleArray, BASEARRAYREF* pComArray,
                                            MethodTable* pInterfaceMT);
    static void MarshalVariantArrayComToOle(BASEARRAYREF* pComArray, void* oleArray,
                                            MethodTable* pInterfaceMT, BOOL fBestFitMapping,
                                            BOOL fThrowOnUnmappableChar, BOOL fOleArrayValid,
                                            SIZE_T cElements);
    static void ClearVariantArray(void* oleArray, SIZE_T cElements, MethodTable* pInterfaceMT);

#ifdef FEATURE_CLASSIC_COMINTEROP
    static void MarshalArrayVariantOleToCom(VARIANT* pOleVariant, VariantData* pComVariant);
    static void MarshalArrayVariantComToOle(VariantData* pComVariant, VARIANT* pOleVariant);
    static void MarshalArrayVariantOleRefToCom(VARIANT* pOleVariant, VariantData* pComVariant);
#endif

    static void MarshalErrorVariantOleToCom(VARIANT* pOleVariant, VariantData* pComVariant);
    static void MarshalErrorVariantOleRefToCom(VARIANT* pOleVariant, VariantData* pComVariant);
    static void MarshalErrorVariantComToOle(VariantData* pComVariant, VARIANT* pOleVariant);
#endif // FEATURE_COMINTEROP
};

#endif
