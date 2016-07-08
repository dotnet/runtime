// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: FieldMarshaler.cpp
//

//


#include "common.h"
#include "vars.hpp"
#include "class.h"
#include "ceeload.h"
#include "excep.h"
#include "fieldmarshaler.h"
#include "field.h"
#include "frames.h"
#include "dllimport.h"
#include "comdelegate.h"
#include "eeconfig.h"
#include "comdatetime.h"
#include "olevariant.h"
#include <cor.h>
#include <corpriv.h>
#include <corerror.h>
#include "sigformat.h"
#include "marshalnative.h"
#include "typeparse.h"
#ifdef FEATURE_COMINTEROP
#include <winstring.h>
#endif // FEATURE_COMINTEROP

// forward declaration
BOOL CheckForPrimitiveType(CorElementType elemType, CQuickArray<WCHAR> *pStrPrimitiveType);
TypeHandle ArraySubTypeLoadWorker(const SString &strUserDefTypeName, Assembly* pAssembly);
TypeHandle GetFieldTypeHandleWorker(MetaSig *pFieldSig);  


//=======================================================================
// A database of NFT types.
//=======================================================================
struct NFTDataBaseEntry
{
    UINT32            m_cbNativeSize;     // native size of field (0 if not constant)
    bool              m_fWinRTSupported;  // true if the field marshaler is supported for WinRT
};

static const NFTDataBaseEntry NFTDataBase[] =
{
    #undef DEFINE_NFT
    #define DEFINE_NFT(name, nativesize, fWinRTSupported) { nativesize, fWinRTSupported },
    #include "nsenums.h"
};


//=======================================================================
// This is invoked from the class loader while building the internal structures for a type
// This function should check if explicit layout metadata exists.
//
// Returns:
//  TRUE    - yes, there's layout metadata
//  FALSE   - no, there's no layout.
//  fail    - throws a typeload exception
//
// If TRUE,
//   *pNLType            gets set to nltAnsi or nltUnicode
//   *pPackingSize       declared packing size
//   *pfExplicitoffsets  offsets explicit in metadata or computed?
//=======================================================================
BOOL HasLayoutMetadata(Assembly* pAssembly, IMDInternalImport *pInternalImport, mdTypeDef cl, MethodTable*pParentMT, BYTE *pPackingSize, BYTE *pNLTType, BOOL *pfExplicitOffsets)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pInternalImport));
        PRECONDITION(CheckPointer(pPackingSize));
        PRECONDITION(CheckPointer(pNLTType));
        PRECONDITION(CheckPointer(pfExplicitOffsets));
    }
    CONTRACTL_END;
    
    HRESULT hr;
    ULONG clFlags;
#ifdef _DEBUG
    clFlags = 0xcccccccc;
#endif
    
    if (FAILED(pInternalImport->GetTypeDefProps(cl, &clFlags, NULL)))
    {
        pAssembly->ThrowTypeLoadException(pInternalImport, cl, IDS_CLASSLOAD_BADFORMAT);
    }
    
    if (IsTdAutoLayout(clFlags))
    {
        // <BUGNUM>workaround for B#104780 - VC fails to set SequentialLayout on some classes
        // with ClassSize. Too late to fix compiler for V1.
        //
        // To compensate, we treat AutoLayout classes as Sequential if they
        // meet all of the following criteria:
        //
        //    - ClassSize present and nonzero.
        //    - No instance fields declared
        //    - Base class is System.ValueType.
        //</BUGNUM>
        ULONG cbTotalSize = 0;
        if (SUCCEEDED(pInternalImport->GetClassTotalSize(cl, &cbTotalSize)) && cbTotalSize != 0)
        {
            if (pParentMT && pParentMT->IsValueTypeClass())
            {
                MDEnumHolder hEnumField(pInternalImport);
                if (SUCCEEDED(pInternalImport->EnumInit(mdtFieldDef, cl, &hEnumField)))
                {
                    ULONG numFields = pInternalImport->EnumGetCount(&hEnumField);
                    if (numFields == 0)
                    {
                        *pfExplicitOffsets = FALSE;
                        *pNLTType = nltAnsi;
                        *pPackingSize = 1;
                        return TRUE;
                    }
                }
            }
        }
        
        return FALSE;
    }
    else if (IsTdSequentialLayout(clFlags))
    {
        *pfExplicitOffsets = FALSE;
    }
    else if (IsTdExplicitLayout(clFlags))
    {
        *pfExplicitOffsets = TRUE;
    }
    else
    {
        pAssembly->ThrowTypeLoadException(pInternalImport, cl, IDS_CLASSLOAD_BADFORMAT);
    }
    
    // We now know this class has seq. or explicit layout. Ensure the parent does too.
    if (pParentMT && !(pParentMT->IsObjectClass() || pParentMT->IsValueTypeClass()) && !(pParentMT->HasLayout()))
        pAssembly->ThrowTypeLoadException(pInternalImport, cl, IDS_CLASSLOAD_BADFORMAT);

    if (IsTdAnsiClass(clFlags))
    {
        *pNLTType = nltAnsi;
    }
    else if (IsTdUnicodeClass(clFlags))
    {
        *pNLTType = nltUnicode;
    }
    else if (IsTdAutoClass(clFlags))
    {
        // We no longer support Win9x so TdAuto always maps to Unicode.
        *pNLTType = nltUnicode;
    }
    else
    {
        pAssembly->ThrowTypeLoadException(pInternalImport, cl, IDS_CLASSLOAD_BADFORMAT);
    }

    DWORD dwPackSize;
    hr = pInternalImport->GetClassPackSize(cl, &dwPackSize);
    if (FAILED(hr) || dwPackSize == 0) 
        dwPackSize = DEFAULT_PACKING_SIZE;

    // This has to be reduced to a BYTE value, so we had better make sure it fits. If
    // not, we'll throw an exception instead of trying to munge the value to what we
    // think the user might want.
    if (!FitsInU1((UINT64)(dwPackSize)))
    {
        pAssembly->ThrowTypeLoadException(pInternalImport, cl, IDS_CLASSLOAD_BADFORMAT);
    }

    *pPackingSize = (BYTE)dwPackSize;
    
    return TRUE;
}

typedef enum
{
    ParseNativeTypeFlag_None    = 0x00,
    ParseNativeTypeFlag_IsAnsi  = 0x01,

#ifdef FEATURE_COMINTEROP
    ParseNativeTypeFlag_IsWinRT = 0x02,
#endif // FEATURE_COMINTEROP
}
ParseNativeTypeFlags;

inline ParseNativeTypeFlags operator|=(ParseNativeTypeFlags& lhs, ParseNativeTypeFlags rhs)
{
    LIMITED_METHOD_CONTRACT;
    lhs = static_cast<ParseNativeTypeFlags>(lhs | rhs);
    return lhs;
}

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
VOID ParseNativeType(Module*                     pModule,
                         PCCOR_SIGNATURE             pCOMSignature,
                         DWORD                       cbCOMSignature,
                         ParseNativeTypeFlags        flags,
                         LayoutRawFieldInfo*         pfwalk,
                         PCCOR_SIGNATURE             pNativeType,
                         ULONG                       cbNativeType,
                         IMDInternalImport*          pInternalImport,
                         mdTypeDef                   cl,
                         const SigTypeContext *      pTypeContext,
                         BOOL                       *pfDisqualifyFromManagedSequential  // set to TRUE if needed (never set to FALSE, it may come in as TRUE!)
#ifdef _DEBUG
                         ,
                         LPCUTF8                     szNamespace,
                         LPCUTF8                     szClassName,
                         LPCUTF8                     szFieldName
#endif
                        )
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pfwalk));
    }
    CONTRACTL_END;

    // Make sure that there is no junk in the unused part of the field marshaler space (ngen image determinism)
    ZeroMemory(&pfwalk->m_FieldMarshaler, MAXFIELDMARSHALERSIZE);

#define INITFIELDMARSHALER(nfttype, fmtype, args)       \
do                                                      \
{                                                       \
    static_assert_no_msg(sizeof(fmtype) <= MAXFIELDMARSHALERSIZE);  \
    pfwalk->m_nft = (nfttype);                          \
    new ( &(pfwalk->m_FieldMarshaler) ) fmtype args;    \
    ((FieldMarshaler*)&(pfwalk->m_FieldMarshaler))->SetNStructFieldType(nfttype); \
} while(0)

    BOOL                fAnsi               = (flags & ParseNativeTypeFlag_IsAnsi);
#ifdef FEATURE_COMINTEROP
    BOOL                fIsWinRT            = (flags & ParseNativeTypeFlag_IsWinRT);
#endif // FEATURE_COMINTEROP
    CorElementType      corElemType         = ELEMENT_TYPE_END;
    PCCOR_SIGNATURE     pNativeTypeStart    = pNativeType;    
    ULONG               cbNativeTypeStart   = cbNativeType;
    CorNativeType       ntype;
    BOOL                fDefault;
    BOOL                BestFit;
    BOOL                ThrowOnUnmappableChar;
    
    pfwalk->m_nft = NFT_NONE;

    if (cbNativeType == 0)
    {
        ntype = NATIVE_TYPE_DEFAULT;
        fDefault = TRUE;
    }
    else
    {
        ntype = (CorNativeType) *( ((BYTE*&)pNativeType)++ );
        cbNativeType--;
        fDefault = (ntype == NATIVE_TYPE_DEFAULT);
    }

#ifdef FEATURE_COMINTEROP
    if (fIsWinRT && !fDefault)
    {
        // Do not allow any MarshalAs in WinRT scenarios - marshaling is fully described by the field type.
        INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_WINRT_MARSHAL_AS));
    }
#endif // FEATURE_COMINTEROP

    // Setup the signature and normalize
    MetaSig fsig(pCOMSignature, cbCOMSignature, pModule, pTypeContext, MetaSig::sigField);
    corElemType = fsig.NextArgNormalized();


    if (!(*pfDisqualifyFromManagedSequential))
    {
        // This type may qualify for ManagedSequential. Collect managed size and alignment info.
        if (CorTypeInfo::IsPrimitiveType(corElemType))
        {
            pfwalk->m_managedSize = ((UINT32)CorTypeInfo::Size(corElemType)); // Safe cast - no primitive type is larger than 4gb!
            pfwalk->m_managedAlignmentReq = pfwalk->m_managedSize;
        }
        else if (corElemType == ELEMENT_TYPE_PTR)
        {
            pfwalk->m_managedSize = sizeof(LPVOID);
            pfwalk->m_managedAlignmentReq = sizeof(LPVOID);
        }
        else if (corElemType == ELEMENT_TYPE_VALUETYPE)
        {
            TypeHandle pNestedType = fsig.GetLastTypeHandleThrowing(ClassLoader::LoadTypes,
                                                                    CLASS_LOAD_APPROXPARENTS,
                                                                    TRUE);
            if (pNestedType.GetMethodTable()->IsManagedSequential())
            {
                pfwalk->m_managedSize = (pNestedType.GetMethodTable()->GetNumInstanceFieldBytes());

                _ASSERTE(pNestedType.GetMethodTable()->HasLayout()); // If it is ManagedSequential(), it also has Layout but doesn't hurt to check before we do a cast!
                pfwalk->m_managedAlignmentReq = pNestedType.GetMethodTable()->GetLayoutInfo()->m_ManagedLargestAlignmentRequirementOfAllMembers;
            }
            else
            {
                *pfDisqualifyFromManagedSequential = TRUE;
            }
        }
        else
        {
            // No other type permitted for ManagedSequential.
            *pfDisqualifyFromManagedSequential = TRUE;
        }
    }

#ifdef _TARGET_X86_
    // Normalization might have put corElementType and ntype out of sync which can
    // result in problems with non-default ntype being validated against the 
    // normalized primitive corElemType.
    //
    VerifyAndAdjustNormalizedType(pModule, fsig.GetArgProps(), fsig.GetSigTypeContext(), &corElemType, &ntype);

    fDefault = (ntype == NATIVE_TYPE_DEFAULT);
#endif // _TARGET_X86_

    CorElementType sigElemType;
    IfFailThrow(fsig.GetArgProps().PeekElemType(&sigElemType));
    if ((sigElemType == ELEMENT_TYPE_GENERICINST || sigElemType == ELEMENT_TYPE_VAR) && corElemType == ELEMENT_TYPE_CLASS)
    {
        INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_GENERICS_RESTRICTION));
    }
    else switch (corElemType)
    {
        case ELEMENT_TYPE_CHAR:
            if (fDefault)
            {
                if (fAnsi)
                {
                    ReadBestFitCustomAttribute(pInternalImport, cl, &BestFit, &ThrowOnUnmappableChar);
                    INITFIELDMARSHALER(NFT_ANSICHAR, FieldMarshaler_Ansi, (BestFit, ThrowOnUnmappableChar));
                }
                else
                {
                    INITFIELDMARSHALER(NFT_COPY2, FieldMarshaler_Copy2, ());
                }
            }
            else if (ntype == NATIVE_TYPE_I1 || ntype == NATIVE_TYPE_U1)
            {
                ReadBestFitCustomAttribute(pInternalImport, cl, &BestFit, &ThrowOnUnmappableChar);
                INITFIELDMARSHALER(NFT_ANSICHAR, FieldMarshaler_Ansi, (BestFit, ThrowOnUnmappableChar));
            }
            else if (ntype == NATIVE_TYPE_I2 || ntype == NATIVE_TYPE_U2)
            {
                INITFIELDMARSHALER(NFT_COPY2, FieldMarshaler_Copy2, ());
            }
            else
            {
                INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_CHAR));
            }
            break;

        case ELEMENT_TYPE_BOOLEAN:
            if (fDefault)
            {
#ifdef FEATURE_COMINTEROP
                if (fIsWinRT)
                {
                    INITFIELDMARSHALER(NFT_CBOOL, FieldMarshaler_CBool, ());
                }
                else
#endif // FEATURE_COMINTEROP
                {
                    INITFIELDMARSHALER(NFT_WINBOOL, FieldMarshaler_WinBool, ());
                }
            }
            else if (ntype == NATIVE_TYPE_BOOLEAN)
            {
                INITFIELDMARSHALER(NFT_WINBOOL, FieldMarshaler_WinBool, ());
            }
#ifdef FEATURE_COMINTEROP
            else if (ntype == NATIVE_TYPE_VARIANTBOOL)
            {
                INITFIELDMARSHALER(NFT_VARIANTBOOL, FieldMarshaler_VariantBool, ());
            }
#endif // FEATURE_COMINTEROP
            else if (ntype == NATIVE_TYPE_U1 || ntype == NATIVE_TYPE_I1)
            {
                INITFIELDMARSHALER(NFT_CBOOL, FieldMarshaler_CBool, ());
            }
            else
            {
                INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_BOOLEAN));
            }
            break;


        case ELEMENT_TYPE_I1:
            if (fDefault || ntype == NATIVE_TYPE_I1 || ntype == NATIVE_TYPE_U1)
            {
                INITFIELDMARSHALER(NFT_COPY1, FieldMarshaler_Copy1, ());
            }
            else
            {
                INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_I1));
            }
            break;

        case ELEMENT_TYPE_U1:
            if (fDefault || ntype == NATIVE_TYPE_U1 || ntype == NATIVE_TYPE_I1)
            {
                INITFIELDMARSHALER(NFT_COPY1, FieldMarshaler_Copy1, ());
            }
            else
            {
                INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_I1));
            }
            break;

        case ELEMENT_TYPE_I2:
            if (fDefault || ntype == NATIVE_TYPE_I2 || ntype == NATIVE_TYPE_U2)
            {
                INITFIELDMARSHALER(NFT_COPY2, FieldMarshaler_Copy2, ());
            }
            else
            {
                INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_I2));
            }
            break;

        case ELEMENT_TYPE_U2:
            if (fDefault || ntype == NATIVE_TYPE_U2 || ntype == NATIVE_TYPE_I2)
            {
                INITFIELDMARSHALER(NFT_COPY2, FieldMarshaler_Copy2, ());
            }
            else
            {
                INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_I2));
            }
            break;

        case ELEMENT_TYPE_I4:
            if (fDefault || ntype == NATIVE_TYPE_I4 || ntype == NATIVE_TYPE_U4 || ntype == NATIVE_TYPE_ERROR)
            {
                INITFIELDMARSHALER(NFT_COPY4, FieldMarshaler_Copy4, ());
            }
            else
            {
                INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_I4));
            }
            break;
            
        case ELEMENT_TYPE_U4:
            if (fDefault || ntype == NATIVE_TYPE_U4 || ntype == NATIVE_TYPE_I4 || ntype == NATIVE_TYPE_ERROR)
            {
                INITFIELDMARSHALER(NFT_COPY4, FieldMarshaler_Copy4, ());
            }
            else
            {
                INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_I4));
            }
            break;

        case ELEMENT_TYPE_I8:
            if (fDefault || ntype == NATIVE_TYPE_I8 || ntype == NATIVE_TYPE_U8)
            {
                INITFIELDMARSHALER(NFT_COPY8, FieldMarshaler_Copy8, ());
            }
            else
            {
                INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_I8));
            }
            break;

        case ELEMENT_TYPE_U8:
            if (fDefault || ntype == NATIVE_TYPE_U8 || ntype == NATIVE_TYPE_I8)
            {
                INITFIELDMARSHALER(NFT_COPY8, FieldMarshaler_Copy8, ());
            }
            else
            {
                INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_I8));
            }
            break;

        case ELEMENT_TYPE_I: //fallthru
        case ELEMENT_TYPE_U:
#ifdef FEATURE_COMINTEROP
            if (fIsWinRT)
            {
                INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_WINRT_ILLEGAL_TYPE));
            }
            else
#endif // FEATURE_COMINTEROP
            if (fDefault || ntype == NATIVE_TYPE_INT || ntype == NATIVE_TYPE_UINT)
            {
                if (sizeof(LPVOID)==4)
                {
                    INITFIELDMARSHALER(NFT_COPY4, FieldMarshaler_Copy4, ());
                }
                else
                {
                    INITFIELDMARSHALER(NFT_COPY8, FieldMarshaler_Copy8, ());
                }
            }
            else
            {
                INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_I));
            }
            break;

        case ELEMENT_TYPE_R4:
            if (fDefault || ntype == NATIVE_TYPE_R4)
            {
                INITFIELDMARSHALER(NFT_COPY4, FieldMarshaler_Copy4, ());
            }
            else
            {
                INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_R4));
            }
            break;
            
        case ELEMENT_TYPE_R8:
            if (fDefault || ntype == NATIVE_TYPE_R8)
            {
                INITFIELDMARSHALER(NFT_COPY8, FieldMarshaler_Copy8, ());
            }
            else
            {
                INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_R8));
            }
            break;

        case ELEMENT_TYPE_PTR:
#ifdef FEATURE_COMINTEROP
            if (fIsWinRT)
            {
                INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_WINRT_ILLEGAL_TYPE));
            }
            else
#endif // FEATURE_COMINTEROP
            if (fDefault)
            {
                switch (sizeof(LPVOID))
                {
                    case 4:
                        INITFIELDMARSHALER(NFT_COPY4, FieldMarshaler_Copy4, ());
                        break;
                        
                    case 8:
                        INITFIELDMARSHALER(NFT_COPY8, FieldMarshaler_Copy8, ());
                        break;

                    default:
                        INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_BADMANAGED));
                        break;
                }
            }
            else
            {
                INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_PTR));
            }
            break;

        case ELEMENT_TYPE_VALUETYPE:
        {
            // This may cause a TypeLoadException, which we currently seem to have to swallow.
            // This happens with structs that contain fields of class type where the class itself
            // refers to the struct in a field.
            TypeHandle thNestedType = GetFieldTypeHandleWorker(&fsig);
            if (!thNestedType.GetMethodTable())
                break;
#ifdef FEATURE_COMINTEROP
            if (fIsWinRT && sigElemType == ELEMENT_TYPE_GENERICINST)
            {
                // If this is a generic value type, lets see whether it is a Nullable<T>
                TypeHandle genType = fsig.GetLastTypeHandleThrowing();
                if(genType != NULL && genType.GetMethodTable()->HasSameTypeDefAs(g_pNullableClass))
                {
                    // The generic type is System.Nullable<T>. 
                    // Lets extract the typeArg and check if the typeArg is valid.
                    // typeArg is invalid if
                    // 1. It is not a value type.
                    // 2. It is string
                    // 3. We have an open type with us.
                    Instantiation inst = genType.GetMethodTable()->GetInstantiation();
                    MethodTable* typeArgMT = inst[0].GetMethodTable();
                    if (!typeArgMT->IsLegalNonArrayWinRTType())
                    {
                        // Type is not a valid WinRT value type.
                        INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_NULLABLE_RESTRICTION));
                    }
                    else
                    {
                        INITFIELDMARSHALER(NFT_WINDOWSFOUNDATIONIREFERENCE, FieldMarshaler_Nullable, (genType.GetMethodTable()));
                    }
                    break;
                }
            }
#endif
           if (fsig.IsClass(g_DateClassName))
            {
                if (fDefault || ntype == NATIVE_TYPE_STRUCT)
                {
                    INITFIELDMARSHALER(NFT_DATE, FieldMarshaler_Date, ());
                }
                else
                {
                    INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_DATETIME));
                }
            }
            else if (fsig.IsClass(g_DecimalClassName))
            {
                if (fDefault || ntype == NATIVE_TYPE_STRUCT)
                {
                    INITFIELDMARSHALER(NFT_DECIMAL, FieldMarshaler_Decimal, ());
                }
#ifdef FEATURE_COMINTEROP
                else if (ntype == NATIVE_TYPE_CURRENCY)
                {
                    INITFIELDMARSHALER(NFT_CURRENCY, FieldMarshaler_Currency, ());
                }
#endif // FEATURE_COMINTEROP
                else
                {
                    INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHALFIELD_DECIMAL));                    
                }
            }
#ifdef FEATURE_COMINTEROP
            else if (fsig.IsClass(g_DateTimeOffsetClassName))
            {
                if (fDefault || ntype == NATIVE_TYPE_STRUCT)
                {
                    INITFIELDMARSHALER(NFT_DATETIMEOFFSET, FieldMarshaler_DateTimeOffset, ());
                }
                else
                {
                    INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_DATETIMEOFFSET));
                }
            }
            else if (fIsWinRT && !thNestedType.GetMethodTable()->IsLegalNonArrayWinRTType())
            {
                INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_WINRT_ILLEGAL_TYPE));
            }
#endif // FEATURE_COMINTEROP
            else if (thNestedType.GetMethodTable()->HasLayout())
            {
                if (fDefault || ntype == NATIVE_TYPE_STRUCT)
                {
                    if (IsStructMarshalable(thNestedType))
                    {
                        INITFIELDMARSHALER(NFT_NESTEDVALUECLASS, FieldMarshaler_NestedValueClass, (thNestedType.GetMethodTable()));
                    }
                    else
                    {
                        INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_NOTMARSHALABLE));
                    }
                }
                else
                {
                    INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_VALUETYPE));                                        
                }
            }
            else
            {
                INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_NOTMARSHALABLE));
            }
            break;
        }

        case ELEMENT_TYPE_CLASS:
        {
            // This may cause a TypeLoadException, which we currently seem to have to swallow.
            // This happens with structs that contain fields of class type where the class itself
            // refers to the struct in a field.
            TypeHandle thNestedType = GetFieldTypeHandleWorker(&fsig);
            if (!thNestedType.GetMethodTable())
                break;
                       
            if (thNestedType.GetMethodTable()->IsObjectClass())
            {
#ifdef FEATURE_COMINTEROP                
                if (fDefault || ntype == NATIVE_TYPE_IUNKNOWN || ntype == NATIVE_TYPE_IDISPATCH || ntype == NATIVE_TYPE_INTF)
                {
                    // Only NATIVE_TYPE_IDISPATCH maps to an IDispatch based interface pointer.
                    DWORD dwFlags = ItfMarshalInfo::ITF_MARSHAL_USE_BASIC_ITF;
                    if (ntype == NATIVE_TYPE_IDISPATCH)
                    {
                        dwFlags |= ItfMarshalInfo::ITF_MARSHAL_DISP_ITF;
                    }
                    INITFIELDMARSHALER(NFT_INTERFACE, FieldMarshaler_Interface, (NULL, NULL, dwFlags));
                }
                else if (ntype == NATIVE_TYPE_STRUCT)
                {
                    INITFIELDMARSHALER(NFT_VARIANT, FieldMarshaler_Variant, ());
                }
#else // FEATURE_COMINTEROP
                if (fDefault || ntype == NATIVE_TYPE_IUNKNOWN || ntype == NATIVE_TYPE_IDISPATCH || ntype == NATIVE_TYPE_INTF)
                {
                    INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_OBJECT_TO_ITF_NOT_SUPPORTED));
                }
                else if (ntype == NATIVE_TYPE_STRUCT)
                {
                    INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_OBJECT_TO_VARIANT_NOT_SUPPORTED));
                }
#endif // FEATURE_COMINTEROP
                else
                {
                    INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHALFIELD_OBJECT));
                }
            }
#ifdef FEATURE_COMINTEROP                
            else if (ntype == NATIVE_TYPE_INTF || thNestedType.IsInterface())
            {
                if (fIsWinRT && !thNestedType.GetMethodTable()->IsLegalNonArrayWinRTType())
                {
                    INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_WINRT_ILLEGAL_TYPE));
                }
                else
                {
                    ItfMarshalInfo itfInfo;
                    if (FAILED(MarshalInfo::TryGetItfMarshalInfo(thNestedType, FALSE, FALSE, &itfInfo)))
                        break;

                    INITFIELDMARSHALER(NFT_INTERFACE, FieldMarshaler_Interface, (itfInfo.thClass.GetMethodTable(), itfInfo.thItf.GetMethodTable(), itfInfo.dwFlags));
                }
            }
#else  // FEATURE_COMINTEROP
            else if (ntype == NATIVE_TYPE_INTF || thNestedType.IsInterface())
            {
                INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_OBJECT_TO_ITF_NOT_SUPPORTED));
            }
#endif // FEATURE_COMINTEROP
            else if (ntype == NATIVE_TYPE_CUSTOMMARSHALER)
            {
                INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHALFIELD_NOCUSTOMMARSH));
            }
            else if (thNestedType == TypeHandle(g_pStringClass))
            {
                if (fDefault)
                {
#ifdef FEATURE_COMINTEROP
                    if (fIsWinRT)
                    {
                        INITFIELDMARSHALER(NFT_HSTRING, FieldMarshaler_HSTRING, ());
                    }
                    else
#endif // FEATURE_COMINTEROP
                    if (fAnsi)
                    {
                        ReadBestFitCustomAttribute(pInternalImport, cl, &BestFit, &ThrowOnUnmappableChar);
                        INITFIELDMARSHALER(NFT_STRINGANSI, FieldMarshaler_StringAnsi, (BestFit, ThrowOnUnmappableChar));
                    }
                    else
                    {
                        INITFIELDMARSHALER(NFT_STRINGUNI, FieldMarshaler_StringUni, ());
                    }
                }
                else
                {
                    switch (ntype)
                    {
                        case NATIVE_TYPE_LPSTR:
                            ReadBestFitCustomAttribute(pInternalImport, cl, &BestFit, &ThrowOnUnmappableChar);
                            INITFIELDMARSHALER(NFT_STRINGANSI, FieldMarshaler_StringAnsi, (BestFit, ThrowOnUnmappableChar));
                            break;

                        case NATIVE_TYPE_LPWSTR:
                            INITFIELDMARSHALER(NFT_STRINGUNI, FieldMarshaler_StringUni, ());
                            break;
                        
                        case NATIVE_TYPE_LPUTF8STR:
							INITFIELDMARSHALER(NFT_STRINGUTF8, FieldMarshaler_StringUtf8, ());
							break;

                        case NATIVE_TYPE_LPTSTR:
                            // We no longer support Win9x so LPTSTR always maps to a Unicode string.
                            INITFIELDMARSHALER(NFT_STRINGUNI, FieldMarshaler_StringUni, ());
                            break;
#ifdef FEATURE_COMINTEROP
                        case NATIVE_TYPE_BSTR:
                            INITFIELDMARSHALER(NFT_BSTR, FieldMarshaler_BSTR, ());
                            break;

                        case NATIVE_TYPE_HSTRING:
                            INITFIELDMARSHALER(NFT_HSTRING, FieldMarshaler_HSTRING, ());
                            break;
#endif // FEATURE_COMINTEROP
                        case NATIVE_TYPE_FIXEDSYSSTRING:
                            {
                                ULONG nchars;
                                ULONG udatasize = CorSigUncompressedDataSize(pNativeType);

                                if (cbNativeType < udatasize)
                                    break;

                                nchars = CorSigUncompressData(pNativeType);
                                cbNativeType -= udatasize;

                                if (nchars == 0)
                                {
                                    INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHALFIELD_ZEROLENGTHFIXEDSTRING));
                                    break;  
                                }

                                if (fAnsi)
                                {
                                    ReadBestFitCustomAttribute(pInternalImport, cl, &BestFit, &ThrowOnUnmappableChar);
                                    INITFIELDMARSHALER(NFT_FIXEDSTRINGANSI, FieldMarshaler_FixedStringAnsi, (nchars, BestFit, ThrowOnUnmappableChar));
                                }
                                else
                                {
                                    INITFIELDMARSHALER(NFT_FIXEDSTRINGUNI, FieldMarshaler_FixedStringUni, (nchars));
                                }
                            }
                        break;

                        default:
                            INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHALFIELD_STRING));
                            break;
                    }
                }
            }
#ifdef FEATURE_COMINTEROP
            else if (fIsWinRT && fsig.IsClass(g_TypeClassName))
            {   // Note: If the System.Type field is in non-WinRT struct, do not change the previously shipped behavior
                INITFIELDMARSHALER(NFT_SYSTEMTYPE, FieldMarshaler_SystemType, ());
            }
            else if (fIsWinRT && fsig.IsClass(g_ExceptionClassName))  // Marshal Windows.Foundation.HResult as System.Exception for WinRT.
            {
                INITFIELDMARSHALER(NFT_WINDOWSFOUNDATIONHRESULT, FieldMarshaler_Exception, ());
            }
#endif //FEATURE_COMINTEROP
#ifdef FEATURE_CLASSIC_COMINTEROP
            else if (thNestedType.GetMethodTable() == g_pArrayClass)
            {
                if (ntype == NATIVE_TYPE_SAFEARRAY)
                {
                    NativeTypeParamInfo ParamInfo;
                    CorElementType etyp = ELEMENT_TYPE_OBJECT;
                    MethodTable* pMT = NULL;
                    VARTYPE vtElement = VT_EMPTY;

                    // Compat: If no safe array used def subtype was specified, we assume TypeOf(Object).                            
                    TypeHandle thElement = TypeHandle(g_pObjectClass);

                    // If we have no native type data, assume default behavior
                    if (S_OK != CheckForCompressedData(pNativeTypeStart, pNativeType, cbNativeTypeStart))
                    {
                        INITFIELDMARSHALER(NFT_SAFEARRAY, FieldMarshaler_SafeArray, (VT_EMPTY, NULL));
                        break;
                    }
      
                    vtElement = (VARTYPE) (CorSigUncompressData(/*modifies*/pNativeType));

                    // Extract the name of the record type's.
                    if (S_OK == CheckForCompressedData(pNativeTypeStart, pNativeType, cbNativeTypeStart))
                    {
                        ULONG strLen;
                        if (FAILED(CPackedLen::SafeGetData(pNativeType, pNativeTypeStart + cbNativeTypeStart, &strLen, &pNativeType)))
                        {
                            INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_BADMETADATA)); 
                            break;
                        }
                        if (strLen > 0)
                        {
                            // Load the type. Use a SString for the string since we need to NULL terminate the string
                            // that comes from the metadata.
                            StackSString safeArrayUserDefTypeName(SString::Utf8, (LPCUTF8)pNativeType, strLen);
                            _ASSERTE((ULONG)(pNativeType + strLen - pNativeTypeStart) == cbNativeTypeStart);

                            // Sadly this may cause a TypeLoadException, which we currently have to swallow.
                            // This happens with structs that contain fields of class type where the class itself
                            // refers to the struct in a field.
                            thElement = ArraySubTypeLoadWorker(safeArrayUserDefTypeName, pModule->GetAssembly());
                            if (thElement.IsNull())
                                break;
                        }                           
                    }

                    ArrayMarshalInfo arrayMarshalInfo(amiRuntime);
                    arrayMarshalInfo.InitForSafeArray(MarshalInfo::MARSHAL_SCENARIO_FIELD, thElement, vtElement, fAnsi);

                    if (!arrayMarshalInfo.IsValid())
                    {
                        INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (arrayMarshalInfo.GetErrorResourceId())); 
                        break;
                    }

                    INITFIELDMARSHALER(NFT_SAFEARRAY, FieldMarshaler_SafeArray, (arrayMarshalInfo.GetElementVT(), arrayMarshalInfo.GetElementTypeHandle().GetMethodTable()));
                }
                else if (ntype == NATIVE_TYPE_FIXEDARRAY)
                {
                    // Check for the number of elements. This is required, if not present fail.
                    if (S_OK != CheckForCompressedData(pNativeTypeStart, pNativeType, cbNativeTypeStart))
                    {
                        INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHALFIELD_FIXEDARRAY_NOSIZE));                            
                        break;
                    }
                            
                    ULONG numElements = CorSigUncompressData(/*modifies*/pNativeType);

                    if (numElements == 0)
                    {
                        INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHALFIELD_FIXEDARRAY_ZEROSIZE));                            
                        break;
                    }
                           
                    // Since these always export to arrays of BSTRs, we don't need to fetch the native type.

                    // Compat: FixedArrays of System.Arrays map to fixed arrays of BSTRs.
                    INITFIELDMARSHALER(NFT_FIXEDARRAY, FieldMarshaler_FixedArray, (pInternalImport, cl, numElements, VT_BSTR, g_pStringClass));
                }
            }
#endif // FEATURE_CLASSIC_COMINTEROP
            else if (COMDelegate::IsDelegate(thNestedType.GetMethodTable()))
            {
                if (fDefault || ntype == NATIVE_TYPE_FUNC)
                {
                    INITFIELDMARSHALER(NFT_DELEGATE, FieldMarshaler_Delegate, (thNestedType.GetMethodTable()));
                }
                else
                {
                    INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_DELEGATE));
                }
            } 
            else if (thNestedType.CanCastTo(TypeHandle(MscorlibBinder::GetClass(CLASS__SAFE_HANDLE))))
            {
                if (fDefault) 
                {
                    INITFIELDMARSHALER(NFT_SAFEHANDLE, FieldMarshaler_SafeHandle, ());
                }
                else 
                {
                    INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_SAFEHANDLE));
                }
            }
            else if (thNestedType.CanCastTo(TypeHandle(MscorlibBinder::GetClass(CLASS__CRITICAL_HANDLE))))
            {
                if (fDefault) 
                {
                    INITFIELDMARSHALER(NFT_CRITICALHANDLE, FieldMarshaler_CriticalHandle, ());
                }
                else 
                {
                    INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_CRITICALHANDLE));
                }
            }
            else if (fsig.IsClass(g_StringBufferClassName))
            {
                INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHALFIELD_NOSTRINGBUILDER));
            }
            else if (IsStructMarshalable(thNestedType))
            {
                if (fDefault || ntype == NATIVE_TYPE_STRUCT)
                {
                    INITFIELDMARSHALER(NFT_NESTEDLAYOUTCLASS, FieldMarshaler_NestedLayoutClass, (thNestedType.GetMethodTable()));
                }
                else
                {
                    INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHALFIELD_LAYOUTCLASS));
                }
            }
#ifdef FEATURE_COMINTEROP
            else if (fIsWinRT)
            {
                // no other reference types are allowed as field types in WinRT
                INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_WINRT_ILLEGAL_TYPE));
            }
            else if (fDefault)
            {
                ItfMarshalInfo itfInfo;
                if (FAILED(MarshalInfo::TryGetItfMarshalInfo(thNestedType, FALSE, FALSE, &itfInfo)))
                {
                    INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_BADMANAGED));
                }
                else
                {
                    INITFIELDMARSHALER(NFT_INTERFACE, FieldMarshaler_Interface, (itfInfo.thClass.GetMethodTable(), itfInfo.thItf.GetMethodTable(), itfInfo.dwFlags));
                }
            }
#endif  // FEATURE_COMINTEROP
            break;
        }

        case ELEMENT_TYPE_SZARRAY:
        case ELEMENT_TYPE_ARRAY:
        {
#ifdef FEATURE_COMINTEROP
            if (fIsWinRT)
            {
                INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_WINRT_ILLEGAL_TYPE));
                break;
            }
#endif // FEATURE_COMINTEROP

            // This may cause a TypeLoadException, which we currently seem to have to swallow.
            // This happens with structs that contain fields of class type where the class itself
            // refers to the struct in a field.
            TypeHandle thArray = GetFieldTypeHandleWorker(&fsig);
            if (thArray.IsNull() || !thArray.IsArray())
                break;

            TypeHandle thElement = thArray.AsArray()->GetArrayElementTypeHandle();
            if (thElement.IsNull())
                break;

            if (ntype == NATIVE_TYPE_FIXEDARRAY)
            {
                CorNativeType elementNativeType = NATIVE_TYPE_DEFAULT;
                
                // The size constant must be specified, if it isn't then the struct can't be marshalled.
                if (S_OK != CheckForCompressedData(pNativeTypeStart, pNativeType, cbNativeTypeStart))
                {
                    INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHALFIELD_FIXEDARRAY_NOSIZE));                            
                    break;
                }

                // Read the size const, if it's 0, then the struct can't be marshalled.
                ULONG numElements = CorSigUncompressData(pNativeType);            
                if (numElements == 0)
                {
                    INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHALFIELD_FIXEDARRAY_ZEROSIZE));                            
                    break;
                }

                // The array sub type is optional so extract it if specified.
                if (S_OK == CheckForCompressedData(pNativeTypeStart, pNativeType, cbNativeTypeStart))
                    elementNativeType = (CorNativeType)CorSigUncompressData(pNativeType);

                ArrayMarshalInfo arrayMarshalInfo(amiRuntime);
                arrayMarshalInfo.InitForFixedArray(thElement, elementNativeType, fAnsi);

                if (!arrayMarshalInfo.IsValid())
                {
                    INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (arrayMarshalInfo.GetErrorResourceId())); 
                    break;
                }

                if (arrayMarshalInfo.GetElementVT() == VTHACK_ANSICHAR)
                {
                    // We need to special case fixed sized arrays of ANSI chars since the OleVariant code
                    // that is used by the generic fixed size array marshaller doesn't support them
                    // properly. 
                    ReadBestFitCustomAttribute(pInternalImport, cl, &BestFit, &ThrowOnUnmappableChar);
                    INITFIELDMARSHALER(NFT_FIXEDCHARARRAYANSI, FieldMarshaler_FixedCharArrayAnsi, (numElements, BestFit, ThrowOnUnmappableChar));
                    break;                    
                }
                else
                {
                    VARTYPE elementVT = arrayMarshalInfo.GetElementVT();

                    INITFIELDMARSHALER(NFT_FIXEDARRAY, FieldMarshaler_FixedArray, (pInternalImport, cl, numElements, elementVT, arrayMarshalInfo.GetElementTypeHandle().GetMethodTable()));
                    break;
                }
            }
#ifdef FEATURE_CLASSIC_COMINTEROP
            else if (fDefault || ntype == NATIVE_TYPE_SAFEARRAY)
            {
                VARTYPE vtElement = VT_EMPTY;

                // Check for data remaining in the signature before we attempt to grab some.
                if (S_OK == CheckForCompressedData(pNativeTypeStart, pNativeType, cbNativeTypeStart))
                    vtElement = (VARTYPE) (CorSigUncompressData(/*modifies*/pNativeType));

                ArrayMarshalInfo arrayMarshalInfo(amiRuntime);
                arrayMarshalInfo.InitForSafeArray(MarshalInfo::MARSHAL_SCENARIO_FIELD, thElement, vtElement, fAnsi);

                if (!arrayMarshalInfo.IsValid())
                {
                    INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (arrayMarshalInfo.GetErrorResourceId())); 
                    break;
                }
                    
                INITFIELDMARSHALER(NFT_SAFEARRAY, FieldMarshaler_SafeArray, (arrayMarshalInfo.GetElementVT(), arrayMarshalInfo.GetElementTypeHandle().GetMethodTable()));
            }
#endif //FEATURE_CLASSIC_COMINTEROP
            else
            {
                INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHALFIELD_ARRAY));                     
            }
            break;
        }            

        case ELEMENT_TYPE_OBJECT:
        case ELEMENT_TYPE_STRING:
            break;

        default:
            // let it fall thru as NFT_NONE
            break;
    }

    if (pfwalk->m_nft == NFT_NONE)
    {
        INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_BADMANAGED));
    }
#ifdef FEATURE_COMINTEROP
    else if (fIsWinRT && !NFTDataBase[pfwalk->m_nft].m_fWinRTSupported)
    {
        // the field marshaler we came up with is not supported in WinRT scenarios
        ZeroMemory(&pfwalk->m_FieldMarshaler, MAXFIELDMARSHALERSIZE);
        INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_WINRT_ILLEGAL_TYPE));
    }
#endif // FEATURE_COMINTEROP
#undef INITFIELDMARSHALER
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif


TypeHandle ArraySubTypeLoadWorker(const SString &strUserDefTypeName, Assembly* pAssembly)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pAssembly));
    }
    CONTRACTL_END;
    
    TypeHandle th; 

    EX_TRY
    {
        // Load the user defined type.
        StackScratchBuffer utf8Name;
        th = TypeName::GetTypeUsingCASearchRules(strUserDefTypeName.GetUTF8(utf8Name), pAssembly);
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(RethrowTerminalExceptions)
    
    return th;
}


TypeHandle GetFieldTypeHandleWorker(MetaSig *pFieldSig)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pFieldSig));
    }
    CONTRACTL_END;

    TypeHandle th; 

    EX_TRY
    {
        // Load the user defined type.
        th = pFieldSig->GetLastTypeHandleThrowing(ClassLoader::LoadTypes, 
                                                  CLASS_LOAD_APPROXPARENTS,
                                                  TRUE /*dropGenericArgumentLevel*/);
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(RethrowTerminalExceptions)

    return th;
}


//=======================================================================
// This function returns TRUE if the type passed in is either a value class or a class and if it has layout information 
// and is marshalable. In all other cases it will return FALSE. 
//=======================================================================
BOOL IsStructMarshalable(TypeHandle th)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT;
        PRECONDITION(!th.IsNull());
    }
    CONTRACTL_END;
    
    if (th.IsBlittable())
    {
        // th.IsBlittable will return true for arrays of blittable types, however since IsStructMarshalable
        // is only supposed to return true for value classes or classes with layout that are marshallable
        // we need to return false if the type is an array.
        if (th.IsArray())
            return FALSE;
        else
            return TRUE;
    }

    // Check to see if the type has layout.
    if (!th.HasLayout())
        return FALSE;

    MethodTable *pMT= th.GetMethodTable();
    PREFIX_ASSUME(pMT != NULL);
    
    if (pMT->IsStructMarshalable())
        return TRUE;

    const FieldMarshaler *pFieldMarshaler = pMT->GetLayoutInfo()->GetFieldMarshalers();
    UINT  numReferenceFields              = pMT->GetLayoutInfo()->GetNumCTMFields();

    while (numReferenceFields--)
    {
        if (pFieldMarshaler->GetNStructFieldType() == NFT_ILLEGAL)
            return FALSE;

        ((BYTE*&)pFieldMarshaler) += MAXFIELDMARSHALERSIZE;
    }

    return TRUE;
}


//=======================================================================
// Called from the clsloader to load up and summarize the field metadata
// for layout classes.
//
// Warning: This function can load other classes (esp. for nested structs.)
//=======================================================================
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
VOID EEClassLayoutInfo::CollectLayoutFieldMetadataThrowing(
   mdTypeDef      cl,               // cl of the NStruct being loaded
   BYTE           packingSize,      // packing size (from @dll.struct)
   BYTE           nlType,           // nltype (from @dll.struct)
#ifdef FEATURE_COMINTEROP
   BOOL           isWinRT,          // Is the type a WinRT type
#endif // FEATURE_COMINTEROP
   BOOL           fExplicitOffsets, // explicit offsets?
   MethodTable   *pParentMT,        // the loaded superclass
   ULONG          cMembers,         // total number of members (methods + fields)
   HENUMInternal *phEnumField,      // enumerator for field
   Module        *pModule,          // Module that defines the scope, loader and heap (for allocate FieldMarshalers)
   const SigTypeContext *pTypeContext,          // Type parameters for NStruct being loaded
   EEClassLayoutInfo    *pEEClassLayoutInfoOut, // caller-allocated structure to fill in.
   LayoutRawFieldInfo   *pInfoArrayOut,         // caller-allocated array to fill in.  Needs room for cMember+1 elements
   LoaderAllocator      *pAllocator,
   AllocMemTracker      *pamTracker
)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pModule));
    }
    CONTRACTL_END;

    HRESULT             hr;
    MD_CLASS_LAYOUT     classlayout;
    mdFieldDef          fd;
    ULONG               ulOffset;
    ULONG cFields = 0;

    // Running tote - if anything in this type disqualifies it from being ManagedSequential, somebody will set this to TRUE by the the time
    // function exits.
    BOOL                fDisqualifyFromManagedSequential = FALSE; 

    // Internal interface for the NStruct being loaded.
    IMDInternalImport *pInternalImport = pModule->GetMDImport();


#ifdef _DEBUG
    LPCUTF8 szName; 
    LPCUTF8 szNamespace; 
    if (FAILED(pInternalImport->GetNameOfTypeDef(cl, &szName, &szNamespace)))
    {
        szName = szNamespace = "Invalid TypeDef record";
    }
    
    if (g_pConfig->ShouldBreakOnStructMarshalSetup(szName))
        CONSISTENCY_CHECK_MSGF(false, ("BreakOnStructMarshalSetup: '%s' ", szName));
#endif


    // Check if this type might be ManagedSequential. Only valuetypes marked Sequential can be
    // ManagedSequential. Other issues checked below might also disqualify the type.
    if ( (!fExplicitOffsets) &&    // Is it marked sequential?
         (pParentMT && (pParentMT->IsValueTypeClass() || pParentMT->IsManagedSequential()))  // Is it a valuetype or derived from a qualifying valuetype?
       )
    {
        // Type qualifies so far... need do nothing.
    }
    else
    {
        fDisqualifyFromManagedSequential = TRUE;
    }


    BOOL fHasNonTrivialParent = pParentMT &&
                                !pParentMT->IsObjectClass() &&
                                !pParentMT->IsValueTypeClass();


    //====================================================================
    // First, some validation checks.
    //====================================================================
    _ASSERTE(!(fHasNonTrivialParent && !(pParentMT->HasLayout())));

    hr = pInternalImport->GetClassLayoutInit(cl, &classlayout);
    if (FAILED(hr))
    {
        COMPlusThrowHR(hr, BFA_CANT_GET_CLASSLAYOUT);
    }

    pEEClassLayoutInfoOut->m_numCTMFields        = fHasNonTrivialParent ? pParentMT->GetLayoutInfo()->m_numCTMFields : 0;
    pEEClassLayoutInfoOut->m_pFieldMarshalers    = NULL;
    pEEClassLayoutInfoOut->SetIsBlittable(TRUE);
    if (fHasNonTrivialParent)
        pEEClassLayoutInfoOut->SetIsBlittable(pParentMT->IsBlittable());
    pEEClassLayoutInfoOut->SetIsZeroSized(FALSE);    
    pEEClassLayoutInfoOut->SetHasExplicitSize(FALSE);
    pEEClassLayoutInfoOut->m_cbPackingSize = packingSize;

    LayoutRawFieldInfo *pfwalk = pInfoArrayOut;
    
    S_UINT32 cbSortArraySize = S_UINT32(cMembers) * S_UINT32(sizeof(LayoutRawFieldInfo *));
    if (cbSortArraySize.IsOverflow())
    {
        ThrowHR(COR_E_TYPELOAD);
    }
    LayoutRawFieldInfo **pSortArray = (LayoutRawFieldInfo **)_alloca(cbSortArraySize.Value());
    LayoutRawFieldInfo **pSortArrayEnd = pSortArray;
    
    ULONG maxRid = pInternalImport->GetCountWithTokenKind(mdtFieldDef);
    
    
    //=====================================================================
    // Phase 1: Figure out the NFT of each field based on both the CLR
    // signature of the field and the FieldMarshaler metadata. 
    //=====================================================================
    BOOL fParentHasLayout = pParentMT && pParentMT->HasLayout();
    UINT32 cbAdjustedParentLayoutNativeSize = 0;
    EEClassLayoutInfo *pParentLayoutInfo = NULL;;
    if (fParentHasLayout)
    {
        pParentLayoutInfo = pParentMT->GetLayoutInfo();
        // Treat base class as an initial member.
        cbAdjustedParentLayoutNativeSize = pParentLayoutInfo->GetNativeSize();
        // If the parent was originally a zero-sized explicit type but
        // got bumped up to a size of 1 for compatibility reasons, then
        // we need to remove the padding, but ONLY for inheritance situations.
        if (pParentLayoutInfo->IsZeroSized()) {
            CONSISTENCY_CHECK(cbAdjustedParentLayoutNativeSize == 1);
            cbAdjustedParentLayoutNativeSize = 0;
        }
    }

    ULONG i;
    for (i = 0; pInternalImport->EnumNext(phEnumField, &fd); i++)
    {
        DWORD dwFieldAttrs;
        ULONG rid = RidFromToken(fd);

        if((rid == 0)||(rid > maxRid))
        {
            COMPlusThrowHR(COR_E_TYPELOAD, BFA_BAD_FIELD_TOKEN);
        }

        IfFailThrow(pInternalImport->GetFieldDefProps(fd, &dwFieldAttrs));
        
        PCCOR_SIGNATURE pNativeType = NULL;
        ULONG cbNativeType;
        // We ignore marshaling data attached to statics and literals,
        // since these do not contribute to instance data.
        if (!IsFdStatic(dwFieldAttrs) && !IsFdLiteral(dwFieldAttrs))
        {
            PCCOR_SIGNATURE pCOMSignature;
            ULONG       cbCOMSignature;

            if (IsFdHasFieldMarshal(dwFieldAttrs))
            {
                hr = pInternalImport->GetFieldMarshal(fd, &pNativeType, &cbNativeType);
                if (FAILED(hr))
                    cbNativeType = 0;
            }
            else
                cbNativeType = 0;
            
            IfFailThrow(pInternalImport->GetSigOfFieldDef(fd,&cbCOMSignature, &pCOMSignature));
            
            IfFailThrow(::validateTokenSig(fd,pCOMSignature,cbCOMSignature,dwFieldAttrs,pInternalImport));
            
            // fill the appropriate entry in pInfoArrayOut
            pfwalk->m_MD = fd;
            pfwalk->m_nft = NULL;
            pfwalk->m_offset = (UINT32) -1;
            pfwalk->m_sequence = 0;

#ifdef _DEBUG
            LPCUTF8 szFieldName;
            if (FAILED(pInternalImport->GetNameOfFieldDef(fd, &szFieldName)))
            {
                szFieldName = "Invalid FieldDef record";
            }
#endif

            ParseNativeTypeFlags flags = ParseNativeTypeFlag_None;
#ifdef FEATURE_COMINTEROP
            if (isWinRT)
                flags |= ParseNativeTypeFlag_IsWinRT;
            else // WinRT types have nlType == nltAnsi but should be treated as Unicode
#endif // FEATURE_COMINTEROP
            if (nlType == nltAnsi)
                flags |=  ParseNativeTypeFlag_IsAnsi;

            ParseNativeType(pModule,
                            pCOMSignature,
                            cbCOMSignature,
                            flags,
                            pfwalk,
                            pNativeType,
                            cbNativeType,
                            pInternalImport,
                            cl,
                            pTypeContext,
                            &fDisqualifyFromManagedSequential
#ifdef _DEBUG
                            ,
                            szNamespace,
                            szName,
                            szFieldName
#endif
                                );


            //<TODO>@nice: This is obviously not the place to bury this logic.
            // We're replacing NFT's with MARSHAL_TYPES_* in the near future
            // so this isn't worth perfecting.</TODO>

            BOOL    resetBlittable = TRUE;

            // if it's a simple copy...
            if (pfwalk->m_nft == NFT_COPY1    ||
                pfwalk->m_nft == NFT_COPY2    ||
                pfwalk->m_nft == NFT_COPY4    ||
                pfwalk->m_nft == NFT_COPY8)
            {
                resetBlittable = FALSE;
            }

            // Or if it's a nested value class that is itself blittable...
            if (pfwalk->m_nft == NFT_NESTEDVALUECLASS)
            {
                FieldMarshaler *pFM = (FieldMarshaler*)&(pfwalk->m_FieldMarshaler);
                _ASSERTE(pFM->IsNestedValueClassMarshaler());

                if (((FieldMarshaler_NestedValueClass *) pFM)->IsBlittable())
                    resetBlittable = FALSE;
            }

            // ...Otherwise, this field prevents blitting
            if (resetBlittable)
                pEEClassLayoutInfoOut->SetIsBlittable(FALSE);

            cFields++;
            pfwalk++;
        }
    }

    _ASSERTE(i == cMembers);

    // NULL out the last entry
    pfwalk->m_MD = mdFieldDefNil;
    
    
    //
    // fill in the layout information 
    //
    
    // pfwalk points to the beginging of the array
    pfwalk = pInfoArrayOut;

    while (SUCCEEDED(hr = pInternalImport->GetClassLayoutNext(
                                     &classlayout,
                                     &fd,
                                     &ulOffset)) &&
                                     fd != mdFieldDefNil)
    {
        // watch for the last entry: must be mdFieldDefNil
        while ((mdFieldDefNil != pfwalk->m_MD)&&(pfwalk->m_MD < fd))
            pfwalk++;

        // if we haven't found a matching token, it must be a static field with layout -- ignore it
        if(pfwalk->m_MD != fd) continue;

        if (!fExplicitOffsets)
        {
            // ulOffset is the sequence
            pfwalk->m_sequence = ulOffset;
        }
        else
        {
            // ulOffset is the explicit offset
            pfwalk->m_offset = ulOffset;
            pfwalk->m_sequence = (ULONG) -1;

            // Treat base class as an initial member.
            if (!SafeAddUINT32(&(pfwalk->m_offset), cbAdjustedParentLayoutNativeSize))
                COMPlusThrowOM();
        }
    }
    IfFailThrow(hr);

    // now sort the array
    if (!fExplicitOffsets)
    { 
        // sort sequential by ascending sequence
        for (i = 0; i < cFields; i++)
        {
            LayoutRawFieldInfo**pSortWalk = pSortArrayEnd;
            while (pSortWalk != pSortArray)
            {
                if (pInfoArrayOut[i].m_sequence >= (*(pSortWalk-1))->m_sequence)
                    break;

                pSortWalk--;
            }

            // pSortWalk now points to the target location for new FieldInfo.
            MoveMemory(pSortWalk + 1, pSortWalk, (pSortArrayEnd - pSortWalk) * sizeof(LayoutRawFieldInfo*));
            *pSortWalk = &pInfoArrayOut[i];
            pSortArrayEnd++;
        }
    }
    else // no sorting for explicit layout
    {
        for (i = 0; i < cFields; i++)
        {
            if(pInfoArrayOut[i].m_MD != mdFieldDefNil)
            {
                if (pInfoArrayOut[i].m_offset == (UINT32)-1)
                {
                    LPCUTF8 szFieldName;
                    if (FAILED(pInternalImport->GetNameOfFieldDef(pInfoArrayOut[i].m_MD, &szFieldName)))
                    {
                        szFieldName = "Invalid FieldDef record";
                    }
                    pModule->GetAssembly()->ThrowTypeLoadException(pInternalImport, 
                                                                   cl,
                                                                   szFieldName,
                                                                   IDS_CLASSLOAD_NSTRUCT_EXPLICIT_OFFSET);
                }
                else if ((INT)pInfoArrayOut[i].m_offset < 0)
                {
                    LPCUTF8 szFieldName;
                    if (FAILED(pInternalImport->GetNameOfFieldDef(pInfoArrayOut[i].m_MD, &szFieldName)))
                    {
                        szFieldName = "Invalid FieldDef record";
                    }
                    pModule->GetAssembly()->ThrowTypeLoadException(pInternalImport, 
                                                                   cl,
                                                                   szFieldName,
                                                                   IDS_CLASSLOAD_NSTRUCT_NEGATIVE_OFFSET);
                }
            }
                
            *pSortArrayEnd = &pInfoArrayOut[i];
            pSortArrayEnd++;
        }
    }

    //=====================================================================
    // Phase 2: Compute the native size (in bytes) of each field.
    // Store this in pInfoArrayOut[].cbNativeSize;
    //=====================================================================

    // Now compute the native size of each field
    for (pfwalk = pInfoArrayOut; pfwalk->m_MD != mdFieldDefNil; pfwalk++)
    {
        UINT8 nft = pfwalk->m_nft;
        pEEClassLayoutInfoOut->m_numCTMFields++;

        // If the NFT's size never changes, it is stored in the database.
        UINT32 cbNativeSize = NFTDataBase[nft].m_cbNativeSize;

        if (cbNativeSize == 0)
        {
            // Size of 0 means NFT's size is variable, so we have to figure it
            // out case by case.
            cbNativeSize = ((FieldMarshaler*)&(pfwalk->m_FieldMarshaler))->NativeSize();
        }
        pfwalk->m_cbNativeSize = cbNativeSize;
    }

    if (pEEClassLayoutInfoOut->m_numCTMFields)
    {
        pEEClassLayoutInfoOut->m_pFieldMarshalers = (FieldMarshaler*)(pamTracker->Track(pAllocator->GetLowFrequencyHeap()->AllocMem(S_SIZE_T(MAXFIELDMARSHALERSIZE) * S_SIZE_T(pEEClassLayoutInfoOut->m_numCTMFields))));

        // Bring in the parent's fieldmarshalers
        if (fHasNonTrivialParent)
        {
            CONSISTENCY_CHECK(fParentHasLayout);
            PREFAST_ASSUME(pParentLayoutInfo != NULL);  // See if (fParentHasLayout) branch above
            
            UINT numChildCTMFields = pEEClassLayoutInfoOut->m_numCTMFields - pParentLayoutInfo->m_numCTMFields;
            memcpyNoGCRefs( ((BYTE*)pEEClassLayoutInfoOut->m_pFieldMarshalers) + MAXFIELDMARSHALERSIZE*numChildCTMFields,
                            pParentLayoutInfo->m_pFieldMarshalers,
                            MAXFIELDMARSHALERSIZE * (pParentLayoutInfo->m_numCTMFields) );
        }

    }


    //=====================================================================
    // Phase 3: If FieldMarshaler requires autooffsetting, compute the offset
    // of each field and the size of the total structure. We do the layout
    // according to standard VC layout rules:
    //
    //   Each field has an alignment requirement. The alignment-requirement
    //   of a scalar field is the smaller of its size and the declared packsize.
    //   The alighnment-requirement of a struct field is the smaller of the
    //   declared packsize and the largest of the alignment-requirement
    //   of its fields. The alignment requirement of an array is that
    //   of one of its elements.
    //
    //   In addition, each struct gets padding at the end to ensure
    //   that an array of such structs contain no unused space between
    //   elements.
    //=====================================================================
    {
        BYTE   LargestAlignmentRequirement = 1;
        UINT32 cbCurOffset = 0;

        // Treat base class as an initial member.
        if (!SafeAddUINT32(&cbCurOffset, cbAdjustedParentLayoutNativeSize))
            COMPlusThrowOM();

        if (fParentHasLayout)
        {
            BYTE alignmentRequirement;
            
            alignmentRequirement = min(packingSize, pParentLayoutInfo->GetLargestAlignmentRequirementOfAllMembers());
    
            LargestAlignmentRequirement = max(LargestAlignmentRequirement, alignmentRequirement);                                          
        }

        // Start with the size inherited from the parent (if any).
        unsigned calcTotalSize = cbAdjustedParentLayoutNativeSize;
     
        LayoutRawFieldInfo **pSortWalk;
        for (pSortWalk = pSortArray, i=cFields; i; i--, pSortWalk++)
        {
            pfwalk = *pSortWalk;
    
            BYTE alignmentRequirement = static_cast<BYTE>(((FieldMarshaler*)&(pfwalk->m_FieldMarshaler))->AlignmentRequirement());
            if (!(alignmentRequirement == 1 ||
                     alignmentRequirement == 2 ||
                     alignmentRequirement == 4 ||
                  alignmentRequirement == 8))
            {
                COMPlusThrowHR(COR_E_INVALIDPROGRAM, BFA_METADATA_CORRUPT);
            }
    
            alignmentRequirement = min(alignmentRequirement, packingSize);
    
            LargestAlignmentRequirement = max(LargestAlignmentRequirement, alignmentRequirement);
    
            // This assert means I forgot to special-case some NFT in the
            // above switch.
            _ASSERTE(alignmentRequirement <= 8);
    
            // Check if this field is overlapped with other(s)
            pfwalk->m_fIsOverlapped = FALSE;
            if (fExplicitOffsets) {
                LayoutRawFieldInfo *pfwalk1;
                DWORD dwBegin = pfwalk->m_offset;
                DWORD dwEnd = dwBegin+pfwalk->m_cbNativeSize;
                for (pfwalk1 = pInfoArrayOut; pfwalk1 < pfwalk; pfwalk1++)
                {
                    if((pfwalk1->m_offset >= dwEnd) || (pfwalk1->m_offset+pfwalk1->m_cbNativeSize <= dwBegin)) continue;
                    pfwalk->m_fIsOverlapped = TRUE;
                    pfwalk1->m_fIsOverlapped = TRUE;
                }
            }
            else
            {
                // Insert enough padding to align the current data member.
                while (cbCurOffset % alignmentRequirement)
                {
                    if (!SafeAddUINT32(&cbCurOffset, 1))
                        COMPlusThrowOM();
                }
    
                // Insert current data member.
                pfwalk->m_offset = cbCurOffset;
    
                // if we overflow we will catch it below
                cbCurOffset += pfwalk->m_cbNativeSize;
            } 
    
            unsigned fieldEnd = pfwalk->m_offset + pfwalk->m_cbNativeSize;
            if (fieldEnd < pfwalk->m_offset)
                COMPlusThrowOM();
    
                // size of the structure is the size of the last field.  
            if (fieldEnd > calcTotalSize)
                calcTotalSize = fieldEnd;
        }
    
        ULONG clstotalsize = 0;
        if (FAILED(pInternalImport->GetClassTotalSize(cl, &clstotalsize)))
        {
            clstotalsize = 0;
        }
        
        if (clstotalsize != 0)
        {
            if (!SafeAddULONG(&clstotalsize, (ULONG)cbAdjustedParentLayoutNativeSize))
                COMPlusThrowOM();
    
            // size must be large enough to accomodate layout. If not, we use the layout size instead.
            if (clstotalsize < calcTotalSize)
            {
                clstotalsize = calcTotalSize;
            }
            calcTotalSize = clstotalsize;   // use the size they told us 
        } 
        else
        {
            // The did not give us an explict size, so lets round up to a good size (for arrays) 
            while (calcTotalSize % LargestAlignmentRequirement != 0)
            {
                if (!SafeAddUINT32(&calcTotalSize, 1))
                    COMPlusThrowOM();
            }
        }
        
        // We'll cap the total native size at a (somewhat) arbitrary limit to ensure
        // that we don't expose some overflow bug later on.
        if (calcTotalSize >= MAX_SIZE_FOR_INTEROP)
            COMPlusThrowOM();

        // This is a zero-sized struct - need to record the fact and bump it up to 1.
        if (calcTotalSize == 0)
        {
            pEEClassLayoutInfoOut->SetIsZeroSized(TRUE);
            calcTotalSize = 1;
        }
    
        pEEClassLayoutInfoOut->m_cbNativeSize = calcTotalSize;
    
        // The packingSize acts as a ceiling on all individual alignment
        // requirements so it follows that the largest alignment requirement
        // is also capped.
        _ASSERTE(LargestAlignmentRequirement <= packingSize);
        pEEClassLayoutInfoOut->m_LargestAlignmentRequirementOfAllMembers = LargestAlignmentRequirement;
    }



    //=====================================================================
    // Phase 4: Now we do the same thing again for managedsequential layout.
    //=====================================================================
    if (!fDisqualifyFromManagedSequential)
    {
        BYTE   LargestAlignmentRequirement = 1;
        UINT32 cbCurOffset = 0;
    
        if (pParentMT && pParentMT->IsManagedSequential())
        {
            // Treat base class as an initial member.
            if (!SafeAddUINT32(&cbCurOffset, pParentMT->GetNumInstanceFieldBytes()))
                COMPlusThrowOM();
    
            BYTE alignmentRequirement = 0;
                
            alignmentRequirement = min(packingSize, pParentLayoutInfo->m_ManagedLargestAlignmentRequirementOfAllMembers);
    
            LargestAlignmentRequirement = max(LargestAlignmentRequirement, alignmentRequirement);                                          
        }
    
        // The current size of the structure as a whole, we start at 1, because we disallow 0 sized structures.
        // NOTE: We do not need to do the same checking for zero-sized types as phase 3 because only ValueTypes
        //       can be ManagedSequential and ValueTypes can not be inherited from.
        unsigned calcTotalSize = 1;
     
        LayoutRawFieldInfo **pSortWalk;
        for (pSortWalk = pSortArray, i=cFields; i; i--, pSortWalk++)
        {
            pfwalk = *pSortWalk;
    
            BYTE alignmentRequirement = ((BYTE)(pfwalk->m_managedAlignmentReq));
            if (!(alignmentRequirement == 1 ||
                     alignmentRequirement == 2 ||
                     alignmentRequirement == 4 ||
                  alignmentRequirement == 8))
            {
                COMPlusThrowHR(COR_E_INVALIDPROGRAM, BFA_METADATA_CORRUPT);
            }
            
            alignmentRequirement = min(alignmentRequirement, packingSize);
            
            LargestAlignmentRequirement = max(LargestAlignmentRequirement, alignmentRequirement);
            
            _ASSERTE(alignmentRequirement <= 8);
            
            // Insert enough padding to align the current data member.
            while (cbCurOffset % alignmentRequirement)
            {
                if (!SafeAddUINT32(&cbCurOffset, 1))
                    COMPlusThrowOM();
            }
            
            // Insert current data member.
            pfwalk->m_managedOffset = cbCurOffset;
            
            // if we overflow we will catch it below
            cbCurOffset += pfwalk->m_managedSize;
            
            unsigned fieldEnd = pfwalk->m_managedOffset + pfwalk->m_managedSize;
            if (fieldEnd < pfwalk->m_managedOffset)
                COMPlusThrowOM();
            
                // size of the structure is the size of the last field.  
            if (fieldEnd > calcTotalSize)
                calcTotalSize = fieldEnd;
            
#ifdef _DEBUG
            // @perf: If the type is blittable, the managed and native layouts have to be identical
            // so they really shouldn't be calculated twice. Until this code has been well tested and
            // stabilized, however, it is useful to compute both and assert that they are equal in the blittable
            // case.
            if (pEEClassLayoutInfoOut->IsBlittable())
            {
                _ASSERTE(pfwalk->m_managedOffset == pfwalk->m_offset);
                _ASSERTE(pfwalk->m_managedSize   == pfwalk->m_cbNativeSize);
            }
#endif
        } //for
        
        ULONG clstotalsize = 0;
        if (FAILED(pInternalImport->GetClassTotalSize(cl, &clstotalsize)))
        {
            clstotalsize = 0;
        }
        
        if (clstotalsize != 0)
        {
            pEEClassLayoutInfoOut->SetHasExplicitSize(TRUE);
            
            if (pParentMT && pParentMT->IsManagedSequential())
            {
                // Treat base class as an initial member.
                UINT32 parentSize = pParentMT->GetNumInstanceFieldBytes();
                if (!SafeAddULONG(&clstotalsize, parentSize))
                    COMPlusThrowOM();
            }
    
            // size must be large enough to accomodate layout. If not, we use the layout size instead.
            if (clstotalsize < calcTotalSize)
            {
                clstotalsize = calcTotalSize;
            }
            calcTotalSize = clstotalsize;   // use the size they told us 
        } 
        else
        {
            // The did not give us an explict size, so lets round up to a good size (for arrays) 
            while (calcTotalSize % LargestAlignmentRequirement != 0)
            {
                if (!SafeAddUINT32(&calcTotalSize, 1))
                    COMPlusThrowOM();
            }
        } 
    
        pEEClassLayoutInfoOut->m_cbManagedSize = calcTotalSize;

        // The packingSize acts as a ceiling on all individual alignment
        // requirements so it follows that the largest alignment requirement
        // is also capped.
        _ASSERTE(LargestAlignmentRequirement <= packingSize);
        pEEClassLayoutInfoOut->m_ManagedLargestAlignmentRequirementOfAllMembers = LargestAlignmentRequirement;

#ifdef _DEBUG
            // @perf: If the type is blittable, the managed and native layouts have to be identical
            // so they really shouldn't be calculated twice. Until this code has been well tested and
            // stabilized, however, it is useful to compute both and assert that they are equal in the blittable
            // case.
            if (pEEClassLayoutInfoOut->IsBlittable())
            {
                _ASSERTE(pEEClassLayoutInfoOut->m_cbManagedSize == pEEClassLayoutInfoOut->m_cbNativeSize);
                _ASSERTE(pEEClassLayoutInfoOut->m_ManagedLargestAlignmentRequirementOfAllMembers == pEEClassLayoutInfoOut->m_LargestAlignmentRequirementOfAllMembers);
            }
#endif
    } //if

    pEEClassLayoutInfoOut->SetIsManagedSequential(!fDisqualifyFromManagedSequential);

#ifdef _DEBUG
    {
        BOOL illegalMarshaler = FALSE;
        
        LOG((LF_INTEROP, LL_INFO100000, "\n\n"));
        LOG((LF_INTEROP, LL_INFO100000, "%s.%s\n", szNamespace, szName));
        LOG((LF_INTEROP, LL_INFO100000, "Packsize      = %lu\n", (ULONG)packingSize));
        LOG((LF_INTEROP, LL_INFO100000, "Max align req = %lu\n", (ULONG)(pEEClassLayoutInfoOut->m_LargestAlignmentRequirementOfAllMembers)));
        LOG((LF_INTEROP, LL_INFO100000, "----------------------------\n"));
        for (pfwalk = pInfoArrayOut; pfwalk->m_MD != mdFieldDefNil; pfwalk++)
        {
            LPCUTF8 fieldname;
            if (FAILED(pInternalImport->GetNameOfFieldDef(pfwalk->m_MD, &fieldname)))
            {
                fieldname = "??";
            }
            LOG((LF_INTEROP, LL_INFO100000, "+%-5lu  ", (ULONG)(pfwalk->m_offset)));
            LOG((LF_INTEROP, LL_INFO100000, "%s", fieldname));
            LOG((LF_INTEROP, LL_INFO100000, "\n"));

            if (((FieldMarshaler*)&pfwalk->m_FieldMarshaler)->GetNStructFieldType() == NFT_ILLEGAL)
                illegalMarshaler = TRUE;             
        }

        // If we are dealing with a non trivial parent, determine if it has any illegal marshallers.
        if (fHasNonTrivialParent)
        {
            FieldMarshaler *pParentFM = pParentMT->GetLayoutInfo()->GetFieldMarshalers();
            for (i = 0; i < pParentMT->GetLayoutInfo()->m_numCTMFields; i++)
            {
                if (pParentFM->GetNStructFieldType() == NFT_ILLEGAL)
                    illegalMarshaler = TRUE;                                 
                ((BYTE*&)pParentFM) += MAXFIELDMARSHALERSIZE;
            }
        }
        
        LOG((LF_INTEROP, LL_INFO100000, "+%-5lu   EOS\n", (ULONG)(pEEClassLayoutInfoOut->m_cbNativeSize)));
        LOG((LF_INTEROP, LL_INFO100000, "Allocated %d %s field marshallers for %s.%s\n", pEEClassLayoutInfoOut->m_numCTMFields, (illegalMarshaler ? "pointless" : "usable"), szNamespace, szName));
    }
#endif
    return;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif


#ifndef CROSSGEN_COMPILE

//=======================================================================
// For each reference-typed FieldMarshaler field, marshals the current CLR value
// to a new native instance and stores it in the fixed portion of the FieldMarshaler.
//
// This function does not attempt to delete the native value that it overwrites.
//
// If there is a SafeHandle field, ppCleanupWorkListOnStack must be non-null, otherwise
// InvalidOperationException is thrown.
//=======================================================================
VOID LayoutUpdateNative(LPVOID *ppProtectedManagedData, SIZE_T offsetbias, MethodTable *pMT, BYTE* pNativeData, OBJECTREF *ppCleanupWorkListOnStack)
{       
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;
    
    FieldMarshaler* pFM                   = pMT->GetLayoutInfo()->GetFieldMarshalers();
    UINT  numReferenceFields              = pMT->GetLayoutInfo()->GetNumCTMFields();
    
    OBJECTREF pCLRValue    = NULL;
    LPVOID scalar           = NULL;
    
    GCPROTECT_BEGIN(pCLRValue)
    GCPROTECT_BEGININTERIOR(scalar)
    {
        g_IBCLogger.LogFieldMarshalersReadAccess(pMT);

        while (numReferenceFields--)
        {
            pFM->Restore();

            DWORD internalOffset = pFM->GetFieldDesc()->GetOffset();

            if (pFM->IsScalarMarshaler())
            {
                scalar = (LPVOID)(internalOffset + offsetbias + (BYTE*)(*ppProtectedManagedData));
                // Note this will throw for FieldMarshaler_Illegal
                pFM->ScalarUpdateNative(scalar, pNativeData + pFM->GetExternalOffset() );
                
            }
            else if (pFM->IsNestedValueClassMarshaler())
            {
                pFM->NestedValueClassUpdateNative((const VOID **)ppProtectedManagedData, internalOffset + offsetbias, pNativeData + pFM->GetExternalOffset(),
                    ppCleanupWorkListOnStack);
            }
            else
            {
                pCLRValue = *(OBJECTREF*)(internalOffset + offsetbias + (BYTE*)(*ppProtectedManagedData));
                pFM->UpdateNative(&pCLRValue, pNativeData + pFM->GetExternalOffset(), ppCleanupWorkListOnStack);
                SetObjectReferenceUnchecked( (OBJECTREF*) (internalOffset + offsetbias + (BYTE*)(*ppProtectedManagedData)), pCLRValue);
            }

            // The cleanup work list is not used to clean up the native contents. It is used
            // to handle cleanup of any additionnal resources the FieldMarshalers allocate.

            ((BYTE*&)pFM) += MAXFIELDMARSHALERSIZE;
        }
    }
    GCPROTECT_END();
    GCPROTECT_END();
}


VOID FmtClassUpdateNative(OBJECTREF *ppProtectedManagedData, BYTE *pNativeData, OBJECTREF *ppCleanupWorkListOnStack)
{        
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(ppProtectedManagedData));
    }
    CONTRACTL_END;

    MethodTable *pMT = (*ppProtectedManagedData)->GetMethodTable();
    _ASSERTE(pMT->IsBlittable() || pMT->HasLayout());
    UINT32   cbsize = pMT->GetNativeSize();

    if (pMT->IsBlittable())
    {
        memcpyNoGCRefs(pNativeData, (*ppProtectedManagedData)->GetData(), cbsize);
    }
    else
    {
        // This allows us to do a partial LayoutDestroyNative in the case of
        // a marshaling error on one of the fields.
        FillMemory(pNativeData, cbsize, 0);
        NativeLayoutDestroyer nld(pNativeData, pMT, cbsize);
        
        LayoutUpdateNative( (VOID**)ppProtectedManagedData,
                                Object::GetOffsetOfFirstField(),
                                pMT,
                                pNativeData,
                                ppCleanupWorkListOnStack);
        
        nld.SuppressRelease();
    }

}


VOID FmtClassUpdateCLR(OBJECTREF *ppProtectedManagedData, BYTE *pNativeData)
{       
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
    
    MethodTable *pMT = (*ppProtectedManagedData)->GetMethodTable();
    _ASSERTE(pMT->IsBlittable() || pMT->HasLayout());
    UINT32   cbsize = pMT->GetNativeSize();

    if (pMT->IsBlittable())
    {
        memcpyNoGCRefs((*ppProtectedManagedData)->GetData(), pNativeData, cbsize);
    }
    else
    {
        LayoutUpdateCLR((VOID**)ppProtectedManagedData,
                            Object::GetOffsetOfFirstField(),
                            pMT,
                            (BYTE*)pNativeData
                           );
    }
}



//=======================================================================
// For each reference-typed FieldMarshaler field, marshals the current CLR value
// to a new CLR instance and stores it in the GC portion of the FieldMarshaler.
//
// If fDeleteNativeCopies is true, it will also destroy the native version.
//
// NOTE: To avoid error-path leaks, this function attempts to destroy
// all of the native fields even if one or more of the conversions fail.
//=======================================================================
VOID LayoutUpdateCLR(LPVOID *ppProtectedManagedData, SIZE_T offsetbias, MethodTable *pMT, BYTE *pNativeData)
{        
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;

    // Don't try to destroy/free native the structure on exception, we may not own it. If we do own it and
    // are supposed to destroy/free it, we do it upstack (e.g. in a helper called from the marshaling stub).

    FieldMarshaler* pFM                   = pMT->GetLayoutInfo()->GetFieldMarshalers();
    UINT  numReferenceFields              = pMT->GetLayoutInfo()->GetNumCTMFields(); 

    struct _gc
    {
        OBJECTREF pCLRValue;
        OBJECTREF pOldCLRValue;
    } gc;

    gc.pCLRValue    = NULL;
    gc.pOldCLRValue = NULL;
    LPVOID scalar    = NULL;
    
    GCPROTECT_BEGIN(gc)
    GCPROTECT_BEGININTERIOR(scalar)
    {
        g_IBCLogger.LogFieldMarshalersReadAccess(pMT);

        while (numReferenceFields--)
        {
            pFM->Restore();

            DWORD internalOffset = pFM->GetFieldDesc()->GetOffset();

            if (pFM->IsScalarMarshaler())
            {
                scalar = (LPVOID)(internalOffset + offsetbias + (BYTE*)(*ppProtectedManagedData));
                // Note this will throw for FieldMarshaler_Illegal
                pFM->ScalarUpdateCLR( pNativeData + pFM->GetExternalOffset(), scalar);
            }
            else if (pFM->IsNestedValueClassMarshaler())
            {
                pFM->NestedValueClassUpdateCLR(pNativeData + pFM->GetExternalOffset(), ppProtectedManagedData, internalOffset + offsetbias);
            }
            else
            {
                gc.pOldCLRValue = *(OBJECTREF*)(internalOffset + offsetbias + (BYTE*)(*ppProtectedManagedData));
                pFM->UpdateCLR( pNativeData + pFM->GetExternalOffset(), &gc.pCLRValue, &gc.pOldCLRValue );
                SetObjectReferenceUnchecked( (OBJECTREF*) (internalOffset + offsetbias + (BYTE*)(*ppProtectedManagedData)), gc.pCLRValue );
            }

            ((BYTE*&)pFM) += MAXFIELDMARSHALERSIZE;
        }
    }
    GCPROTECT_END();
    GCPROTECT_END();
}


VOID LayoutDestroyNative(LPVOID pNative, MethodTable *pMT)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;
    
    FieldMarshaler *pFM                   = pMT->GetLayoutInfo()->GetFieldMarshalers();
    UINT  numReferenceFields              = pMT->GetLayoutInfo()->GetNumCTMFields();
    BYTE *pNativeData                     = (BYTE*)pNative;

    while (numReferenceFields--)
    {
        pFM->DestroyNative( pNativeData + pFM->GetExternalOffset() );
        ((BYTE*&)pFM) += MAXFIELDMARSHALERSIZE;
    }
}

VOID FmtClassDestroyNative(LPVOID pNative, MethodTable *pMT)
{       
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;
    
    if (pNative)
    {
        if (!(pMT->IsBlittable()))
        {
            _ASSERTE(pMT->HasLayout());
            LayoutDestroyNative(pNative, pMT);
        }
    }
}

VOID FmtValueTypeUpdateNative(LPVOID pProtectedManagedData, MethodTable *pMT, BYTE *pNativeData, OBJECTREF *ppCleanupWorkListOnStack)
{        
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;
    
    _ASSERTE(pMT->IsValueType() && (pMT->IsBlittable() || pMT->HasLayout()));
    UINT32   cbsize = pMT->GetNativeSize();

    if (pMT->IsBlittable())
    {
        memcpyNoGCRefs(pNativeData, pProtectedManagedData, cbsize);
    }
    else
    {
        // This allows us to do a partial LayoutDestroyNative in the case of
        // a marshaling error on one of the fields.
        FillMemory(pNativeData, cbsize, 0);
        
        NativeLayoutDestroyer nld(pNativeData, pMT, cbsize);
        
        LayoutUpdateNative( (VOID**)pProtectedManagedData,
                                0,
                                pMT,
                                pNativeData,
                                ppCleanupWorkListOnStack);
        
        nld.SuppressRelease();
    }
}

VOID FmtValueTypeUpdateCLR(LPVOID pProtectedManagedData, MethodTable *pMT, BYTE *pNativeData)
{       
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;

    _ASSERTE(pMT->IsValueType() && (pMT->IsBlittable() || pMT->HasLayout()));
    UINT32   cbsize = pMT->GetNativeSize();

    if (pMT->IsBlittable())
    {
        memcpyNoGCRefs(pProtectedManagedData, pNativeData, cbsize);
    }
    else
    {
        LayoutUpdateCLR((VOID**)pProtectedManagedData,
                            0,
                            pMT,
                            (BYTE*)pNativeData);
    }
}


#ifdef FEATURE_COMINTEROP

//=======================================================================
// BSTR <--> System.String
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_BSTR::UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const 
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pNativeValue));
    }
    CONTRACTL_END;

    STRINGREF pString;
    *((OBJECTREF*)&pString) = *pCLRValue;
    
    if (pString == NULL)
        MAYBE_UNALIGNED_WRITE(pNativeValue, _PTR, NULL);
    else
    {
        BSTR pBSTR = SysAllocStringLen(pString->GetBuffer(), pString->GetStringLength());
        if (!pBSTR)
            COMPlusThrowOM();

        MAYBE_UNALIGNED_WRITE(pNativeValue, _PTR, pBSTR);        
    }
}


//=======================================================================
// BSTR <--> System.String
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_BSTR::UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const 
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    _ASSERTE(NULL != pNativeValue);
    _ASSERTE(NULL != ppProtectedCLRValue);

    STRINGREF pString;
    BSTR pBSTR = (BSTR)MAYBE_UNALIGNED_READ(pNativeValue, _PTR);
    
    if (!pBSTR)
        pString = NULL;
    else
    {
        struct Param : CallOutFilterParam {
            int             length;
            BSTR            pBSTR;
        }; Param param;

        param.OneShot = TRUE;
        param.length = 0;
        param.pBSTR = pBSTR;

        PAL_TRY(Param *, pParam, &param)
        {
            pParam->length = SysStringLen(pParam->pBSTR);
        }
        PAL_EXCEPT_FILTER(CallOutFilter)
        {
            _ASSERTE(!"CallOutFilter returned EXECUTE_HANDLER.");
        }
        PAL_ENDTRY;
        
        pString = StringObject::NewString(pBSTR, param.length);
    }

    *((STRINGREF*)ppProtectedCLRValue) = pString;
}


//=======================================================================
// BSTR <--> System.String
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_BSTR::DestroyNativeImpl(LPVOID pNativeValue) const 
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pNativeValue));
    }
    CONTRACTL_END;

    BSTR pBSTR = (BSTR)MAYBE_UNALIGNED_READ(pNativeValue, _PTR);
    MAYBE_UNALIGNED_WRITE(pNativeValue, _PTR, NULL);
    
    if (pBSTR)
    {
        _ASSERTE (GetModuleHandleA("oleaut32.dll") != NULL);
        // BSTR has been created, which means oleaut32 should have been loaded.
        // Delay load will not fail.
        CONTRACT_VIOLATION(ThrowsViolation);
        SysFreeString(pBSTR);
    }
}

//===========================================================================================
// Windows.Foundation.IReference'1<-- System.Nullable'1
//
VOID FieldMarshaler_Nullable::ScalarUpdateNativeImpl(LPVOID pCLR, LPVOID pNative) const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pNative));
        PRECONDITION(CheckPointer(pCLR));
    }
    CONTRACTL_END;

    IUnknown *pUnk = NULL;

    // ConvertToNative<T>(ref Nullable<T> pManaged) where T : struct
    MethodDescCallSite convertToNative(GetMethodDescForGenericInstantiation(MscorlibBinder::GetMethod(METHOD__NULLABLEMARSHALER__CONVERT_TO_NATIVE)));
    ARG_SLOT args[] =
    {
        PtrToArgSlot(pCLR)
    };

    pUnk = (IUnknown*) convertToNative.Call_RetLPVOID(args);

    MAYBE_UNALIGNED_WRITE(pNative, _PTR, pUnk);
}

//===========================================================================================
// Windows.Foundation.IReference'1--> System.Nullable'1
//
VOID FieldMarshaler_Nullable::ScalarUpdateCLRImpl(const VOID *pNative, LPVOID pCLR) const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pNative));
        PRECONDITION(CheckPointer(pCLR));
    }
    CONTRACTL_END;

    IUnknown *pUnk = (IUnknown*)MAYBE_UNALIGNED_READ(pNative, _PTR);

    MethodDescCallSite convertToManaged(GetMethodDescForGenericInstantiation(MscorlibBinder::GetMethod(METHOD__NULLABLEMARSHALER__CONVERT_TO_MANAGED_RET_VOID)));

    ARG_SLOT args[] =
    {
        PtrToArgSlot(pUnk),
        PtrToArgSlot(pCLR)
    };

    //ConvertToManaged<T>(Intptr pNative, ref Nullable<T> retObj) where T : struct;
    convertToManaged.Call(args);
}

//===========================================================================================
// Windows.Foundation.IReference'1<--> System.Nullable'1
//
VOID FieldMarshaler_Nullable::DestroyNativeImpl(const VOID* pNative) const
{ 
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pNative));
    }
    CONTRACTL_END;

    IUnknown *pUnk = (IUnknown*)MAYBE_UNALIGNED_READ(pNative, _PTR);
    MAYBE_UNALIGNED_WRITE(pNative, _PTR, NULL);

    if (pUnk != NULL)
    {
        ULONG cbRef = SafeRelease(pUnk);
        LogInteropRelease(pUnk, cbRef, "Field marshaler destroy native");
    }
}

//=======================================================================
// HSTRING <--> System.String
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_HSTRING::UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pCLRValue));
        PRECONDITION(CheckPointer(pNativeValue));
    }
    CONTRACTL_END;

    if (!WinRTSupported())
    {
        COMPlusThrow(kPlatformNotSupportedException, W("PlatformNotSupported_WinRT"));
    }

    STRINGREF stringref = (STRINGREF)(*pCLRValue);

    if (stringref == NULL)
    {
        DefineFullyQualifiedNameForClassW();
        StackSString ssFieldName(SString::Utf8, GetFieldDesc()->GetName());

        SString errorString;
        errorString.LoadResource(CCompRC::Error, IDS_EE_BADMARSHALFIELD_NULL_HSTRING);

        COMPlusThrow(kMarshalDirectiveException,
                     IDS_EE_BADMARSHALFIELD_ERROR_MSG,
                     GetFullyQualifiedNameForClassW(GetFieldDesc()->GetEnclosingMethodTable()),
                     ssFieldName.GetUnicode(),
                     errorString.GetUnicode());
    }

    HSTRING hstring;
    IfFailThrow(WindowsCreateString(stringref->GetBuffer(), stringref->GetStringLength(), &hstring));

    MAYBE_UNALIGNED_WRITE(pNativeValue, _PTR, hstring);
}

//=======================================================================
// HSTRING <--> System.String
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_HSTRING::UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pNativeValue));
        PRECONDITION(CheckPointer(ppProtectedCLRValue));
    }
    CONTRACTL_END;

    if (!WinRTSupported())
    {
        COMPlusThrow(kPlatformNotSupportedException, W("PlatformNotSupported_WinRT"));
    }

    // NULL HSTRINGS are equivilent to empty strings
    UINT32 cchString = 0;
    LPCWSTR pwszString = W("");

    HSTRING hstring = (HSTRING)MAYBE_UNALIGNED_READ(pNativeValue, _PTR);
    if (hstring != NULL)
    {
        pwszString = WindowsGetStringRawBuffer(hstring, &cchString);
    }

    STRINGREF stringref = StringObject::NewString(pwszString, cchString);
    *((STRINGREF *)ppProtectedCLRValue) = stringref;
}

//=======================================================================
// HSTRING <--> System.String
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_HSTRING::DestroyNativeImpl(LPVOID pNativeValue) const
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pNativeValue));
    }
    CONTRACTL_END;

    HSTRING hstring = (HSTRING)MAYBE_UNALIGNED_READ(pNativeValue, _PTR);
    MAYBE_UNALIGNED_WRITE(pNativeValue, _PTR, NULL);

    if (hstring != NULL)
    {
        // We need this for code:System.Runtime.InteropServices.Marshal.DestroyStructure (user can explicitly call it)
        if (WinRTSupported())
        {
            // If WinRT is supported we've already loaded combase.dll, which means
            // this delay load will succeed
            CONTRACT_VIOLATION(ThrowsViolation);
            WindowsDeleteString(hstring);
        }
    }
}

//=======================================================================================
// Windows.UI.Xaml.Interop.TypeName <--> System.Type
// 
VOID FieldMarshaler_SystemType::UpdateNativeImpl(OBJECTREF * pCLRValue, LPVOID pNativeValue, OBJECTREF * ppCleanupWorkListOnStack) const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pCLRValue));
        PRECONDITION(CheckPointer(pNativeValue));
    }
    CONTRACTL_END;
    
    // ConvertToNative(System.Type managedType, TypeName *pTypeName)
    MethodDescCallSite convertToNative(METHOD__SYSTEMTYPEMARSHALER__CONVERT_TO_NATIVE);
    ARG_SLOT args[] =
    {
        ObjToArgSlot(*pCLRValue),
        PtrToArgSlot(pNativeValue)
    };
    convertToNative.Call(args);
}

//=======================================================================================
// Windows.UI.Xaml.Interop.TypeName <--> System.Type
// 
VOID FieldMarshaler_SystemType::UpdateCLRImpl(const VOID * pNativeValue, OBJECTREF * ppProtectedCLRValue, OBJECTREF * ppProtectedOldCLRValue) const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pNativeValue));
        PRECONDITION(CheckPointer(ppProtectedCLRValue));
    }
    CONTRACTL_END;
    
    // ConvertToManaged(TypeName *pTypeName, out System.Type)
    MethodDescCallSite convertToManaged(METHOD__SYSTEMTYPEMARSHALER__CONVERT_TO_MANAGED);
    ARG_SLOT args[] =
    {
        PtrToArgSlot(pNativeValue),
        PtrToArgSlot(ppProtectedCLRValue)
    };
    
    convertToManaged.Call(args);
}

//=======================================================================================
// Windows.UI.Xaml.Interop.TypeName <--> System.Type
// Clear the HSTRING field
// 
VOID FieldMarshaler_SystemType::DestroyNativeImpl(LPVOID pNativeValue) const
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pNativeValue));
        PRECONDITION(WinRTSupported());
    }
    CONTRACTL_END;

    //
    // Call WindowsDeleteString instead of SystemTypeMarshaler.ClearNative
    // because WindowsDeleteString does not throw and is much faster
    //    
    size_t offset = offsetof(TypeNameNative, typeName);
    HSTRING hstring = (HSTRING)MAYBE_UNALIGNED_READ((LPBYTE) pNativeValue + offset , _PTR);
    MAYBE_UNALIGNED_WRITE((LPBYTE) pNativeValue + offset, _PTR, NULL);
    
    if (hstring != NULL)
    {
        // Note: we've already loaded combase.dll, which means this delay load will succeed
        CONTRACT_VIOLATION(ThrowsViolation);
        WindowsDeleteString(hstring);
    }
}

//=======================================================================================
// Windows.Foundation.HResult <--> System.Exception
// Note: The WinRT struct has exactly 1 field, Value (an HRESULT)
// 
VOID FieldMarshaler_Exception::UpdateNativeImpl(OBJECTREF * pCLRValue, LPVOID pNativeValue, OBJECTREF * ppCleanupWorkListOnStack) const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pCLRValue));
        PRECONDITION(CheckPointer(pNativeValue));
    }
    CONTRACTL_END;
    
    // int ConvertToNative(Exception ex)
    MethodDescCallSite convertToNative(METHOD__HRESULTEXCEPTIONMARSHALER__CONVERT_TO_NATIVE);
    ARG_SLOT args[] =
    {
        ObjToArgSlot(*pCLRValue)
    };
    int iReturnedValue = convertToNative.Call_RetI4(args);
    MAYBE_UNALIGNED_WRITE(pNativeValue, 32, iReturnedValue);
}

//=======================================================================================
// Windows.Foundation.HResult <--> System.Exception
// Note: The WinRT struct has exactly 1 field, Value (an HRESULT)
// 
VOID FieldMarshaler_Exception::UpdateCLRImpl(const VOID * pNativeValue, OBJECTREF * ppProtectedCLRValue, OBJECTREF * ppProtectedOldCLRValue) const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pNativeValue));
        PRECONDITION(CheckPointer(ppProtectedCLRValue));
    }
    CONTRACTL_END;
    
    // Exception ConvertToManaged(int hr)
    MethodDescCallSite convertToManaged(METHOD__HRESULTEXCEPTIONMARSHALER__CONVERT_TO_MANAGED);
    ARG_SLOT args[] =
    {
        (ARG_SLOT)MAYBE_UNALIGNED_READ(pNativeValue, 32)
    };
    *ppProtectedCLRValue = convertToManaged.Call_RetOBJECTREF(args);
}

#endif // FEATURE_COMINTEROP


//=======================================================================
// Nested structure conversion
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_NestedLayoutClass::UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const 
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pNativeValue));
    }
    CONTRACTL_END;

    UINT32 cbNativeSize = GetMethodTable()->GetNativeSize();

    if (*pCLRValue == NULL)
    {
        ZeroMemoryInGCHeap(pNativeValue, cbNativeSize);
    }
    else
    {
        LayoutUpdateNative((LPVOID*)pCLRValue, Object::GetOffsetOfFirstField(), 
                           GetMethodTable(), (BYTE*)pNativeValue, ppCleanupWorkListOnStack);
    }

}


//=======================================================================
// Nested structure conversion
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_NestedLayoutClass::UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const 
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pNativeValue));
        PRECONDITION(CheckPointer(ppProtectedCLRValue));
    }
    CONTRACTL_END;

    *ppProtectedCLRValue = GetMethodTable()->Allocate();

    LayoutUpdateCLR( (LPVOID*)ppProtectedCLRValue,
                         Object::GetOffsetOfFirstField(),
                         GetMethodTable(),
                         (BYTE *)pNativeValue);

}


//=======================================================================
// Nested structure conversion
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_NestedLayoutClass::DestroyNativeImpl(LPVOID pNativeValue) const 
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pNativeValue));
    }
    CONTRACTL_END;

    LayoutDestroyNative(pNativeValue, GetMethodTable());
}

#endif // CROSSGEN_COMPILE


//=======================================================================
// Nested structure conversion
// See FieldMarshaler for details.
//=======================================================================
UINT32 FieldMarshaler_NestedLayoutClass::NativeSizeImpl() const
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    return GetMethodTable()->GetLayoutInfo()->GetNativeSize();
}

//=======================================================================
// Nested structure conversion
// See FieldMarshaler for details.
//=======================================================================
UINT32 FieldMarshaler_NestedLayoutClass::AlignmentRequirementImpl() const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    return GetMethodTable()->GetLayoutInfo()->GetLargestAlignmentRequirementOfAllMembers();
}

#if FEATURE_COMINTEROP
MethodDesc* FieldMarshaler_Nullable::GetMethodDescForGenericInstantiation(MethodDesc* pMD) const
{
    MethodDesc *pMethodInstantiation;

    pMethodInstantiation = MethodDesc::FindOrCreateAssociatedMethodDesc(
        pMD,
        pMD->GetMethodTable(),
        FALSE,
        GetMethodTable()->GetInstantiation(),
        FALSE,
        TRUE);

    _ASSERTE(pMethodInstantiation != NULL);

    return pMethodInstantiation;
}
#endif //FEATURE_COMINTEROP

#ifndef CROSSGEN_COMPILE

//=======================================================================
// Nested structure conversion
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_NestedValueClass::NestedValueClassUpdateNativeImpl(const VOID **ppProtectedCLR, SIZE_T startoffset, LPVOID pNative, OBJECTREF *ppCleanupWorkListOnStack) const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(ppProtectedCLR));
        PRECONDITION(CheckPointer(pNative));
    }
    CONTRACTL_END;

    // would be better to detect this at class load time (that have a nested value
    // class with no layout) but don't have a way to know
    if (! GetMethodTable()->GetLayoutInfo())
        COMPlusThrow(kArgumentException, IDS_NOLAYOUT_IN_EMBEDDED_VALUECLASS);

    LayoutUpdateNative((LPVOID*)ppProtectedCLR, startoffset, GetMethodTable(), (BYTE*)pNative, ppCleanupWorkListOnStack);
}


//=======================================================================
// Nested structure conversion
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_NestedValueClass::NestedValueClassUpdateCLRImpl(const VOID *pNative, LPVOID *ppProtectedCLR, SIZE_T startoffset) const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pNative));
        PRECONDITION(CheckPointer(ppProtectedCLR));
    }
    CONTRACTL_END;

    // would be better to detect this at class load time (that have a nested value
    // class with no layout) but don't have a way to know
    if (! GetMethodTable()->GetLayoutInfo())
        COMPlusThrow(kArgumentException, IDS_NOLAYOUT_IN_EMBEDDED_VALUECLASS);

    LayoutUpdateCLR( (LPVOID*)ppProtectedCLR,
                         startoffset,
                         GetMethodTable(),
                         (BYTE *)pNative);
    

}


//=======================================================================
// Nested structure conversion
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_NestedValueClass::DestroyNativeImpl(LPVOID pNativeValue) const 
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pNativeValue));
    }
    CONTRACTL_END;

    LayoutDestroyNative(pNativeValue, GetMethodTable());
}

#endif // CROSSGEN_COMPILE


//=======================================================================
// Nested structure conversion
// See FieldMarshaler for details.
//=======================================================================
UINT32 FieldMarshaler_NestedValueClass::NativeSizeImpl() const
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // this can't be marshalled as native type if no layout, so we allow the 
    // native size info to be created if available, but the size will only
    // be valid for native, not unions. Marshaller will throw exception if
    // try to marshall a value class with no layout
    if (GetMethodTable()->HasLayout())
        return GetMethodTable()->GetLayoutInfo()->GetNativeSize();
    
    return 0;
}


//=======================================================================
// Nested structure conversion
// See FieldMarshaler for details.
//=======================================================================
UINT32 FieldMarshaler_NestedValueClass::AlignmentRequirementImpl() const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    
    // this can't be marshalled as native type if no layout, so we allow the 
    // native size info to be created if available, but the alignment will only
    // be valid for native, not unions. Marshaller will throw exception if
    // try to marshall a value class with no layout
    if (GetMethodTable()->HasLayout())
    {
        UINT32  uAlignmentReq = GetMethodTable()->GetLayoutInfo()->GetLargestAlignmentRequirementOfAllMembers();
        return uAlignmentReq;
    }
    return 1;
}


#ifndef CROSSGEN_COMPILE

//=======================================================================
// CoTask Uni <--> System.String
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_StringUni::UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const 
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pNativeValue));
    }
    CONTRACTL_END;

    STRINGREF pString;
    *((OBJECTREF*)&pString) = *pCLRValue;
    
    if (pString == NULL)
    {
        MAYBE_UNALIGNED_WRITE(pNativeValue, _PTR, NULL);
    }
    else
    {
        DWORD nc = pString->GetStringLength();
        if (nc > MAX_SIZE_FOR_INTEROP)
            COMPlusThrow(kMarshalDirectiveException, IDS_EE_STRING_TOOLONG);

        LPWSTR wsz = (LPWSTR)CoTaskMemAlloc( (nc + 1) * sizeof(WCHAR) );
        if (!wsz)
            COMPlusThrowOM();

        memcpyNoGCRefs(wsz, pString->GetBuffer(), nc*sizeof(WCHAR));
        wsz[nc] = W('\0');
        MAYBE_UNALIGNED_WRITE(pNativeValue, _PTR, wsz);
    }
}


//=======================================================================
// CoTask Uni <--> System.String
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_StringUni::UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const 
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pNativeValue));
        PRECONDITION(CheckPointer(ppProtectedCLRValue));
    }
    CONTRACTL_END;

    STRINGREF pString;
    LPCWSTR wsz = (LPCWSTR)MAYBE_UNALIGNED_READ(pNativeValue, _PTR);
    
    if (!wsz)
        pString = NULL;
    else
    {
        SIZE_T length = wcslen(wsz);
        if (length > MAX_SIZE_FOR_INTEROP)
            COMPlusThrow(kMarshalDirectiveException, IDS_EE_STRING_TOOLONG);

        pString = StringObject::NewString(wsz, (DWORD)length);
    }
    
    *((STRINGREF*)ppProtectedCLRValue) = pString;
}


//=======================================================================
// CoTask Uni <--> System.String
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_StringUni::DestroyNativeImpl(LPVOID pNativeValue) const 
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pNativeValue));        
    }
    CONTRACTL_END;
    
    LPWSTR wsz = (LPWSTR)MAYBE_UNALIGNED_READ(pNativeValue, _PTR);
    MAYBE_UNALIGNED_WRITE(pNativeValue, _PTR, NULL);
    if (wsz)
        CoTaskMemFree(wsz);
}



//=======================================================================
// CoTask Ansi <--> System.String
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_StringAnsi::UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const 
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pNativeValue));
    }
    CONTRACTL_END;

    STRINGREF pString;
    *((OBJECTREF*)&pString) = *pCLRValue;
    
    if (pString == NULL)
    {
        MAYBE_UNALIGNED_WRITE(pNativeValue, _PTR, NULL);
    }
    else
    {
        DWORD nc = pString->GetStringLength();
        if (nc > MAX_SIZE_FOR_INTEROP)
            COMPlusThrow(kMarshalDirectiveException, IDS_EE_STRING_TOOLONG);
 
        LPSTR sz = (LPSTR)CoTaskMemAlloc( (nc + 1) * 2 /* 2 for MBCS */ );
        if (!sz)
            COMPlusThrowOM(); 

        int nbytes = InternalWideToAnsi(pString->GetBuffer(),
                                        nc,
                                        sz,
                                        nc*2,
                                        m_BestFitMap,
                                        m_ThrowOnUnmappableChar);
        sz[nbytes] = '\0';

        MAYBE_UNALIGNED_WRITE(pNativeValue, _PTR, sz);
     }
}


//=======================================================================
// CoTask Ansi <--> System.String
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_StringAnsi::UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const 
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pNativeValue));
        PRECONDITION(CheckPointer(ppProtectedCLRValue));
    }
    CONTRACTL_END;

    STRINGREF pString = NULL;
    LPCSTR sz = (LPCSTR)MAYBE_UNALIGNED_READ(pNativeValue, _PTR);
    if (!sz) 
        pString = NULL;
    else 
    {
        MAKE_WIDEPTR_FROMANSI(wsztemp, sz);
        pString = StringObject::NewString(wsztemp, __lwsztemp - 1);
    }
    
    *((STRINGREF*)ppProtectedCLRValue) = pString;
}


//=======================================================================
// CoTask Ansi <--> System.String
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_StringAnsi::DestroyNativeImpl(LPVOID pNativeValue) const 
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pNativeValue));        
    }
    CONTRACTL_END;
    
    LPSTR sz = (LPSTR)MAYBE_UNALIGNED_READ(pNativeValue, _PTR);
    MAYBE_UNALIGNED_WRITE(pNativeValue, _PTR, NULL);
    if (sz)
        CoTaskMemFree(sz);
}

//=======================================================================
// CoTask Utf8 <--> System.String
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_StringUtf8::UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pNativeValue));
    }
    CONTRACTL_END;

    STRINGREF pString = (STRINGREF)(*pCLRValue);
    if (pString == NULL)
    {
        MAYBE_UNALIGNED_WRITE(pNativeValue, _PTR, NULL);
    }
    else
    {
        DWORD nc = pString->GetStringLength();
        if (nc > MAX_SIZE_FOR_INTEROP)
            COMPlusThrow(kMarshalDirectiveException, IDS_EE_STRING_TOOLONG);

        // Characters would be # of characters + 1 in case left over high surrogate is ?
        // Max 3 bytes per char for basic multi-lingual plane.
        nc = (nc + 1) * MAX_UTF8_CHAR_SIZE;
        // +1 for '\0'
        LPUTF8  lpBuffer = (LPUTF8)CoTaskMemAlloc(nc + 1);
        if (!lpBuffer)
        {
            COMPlusThrowOM();
        }

        // UTF8Marshaler.ConvertToNative
        MethodDescCallSite convertToNative(METHOD__CUTF8MARSHALER__CONVERT_TO_NATIVE);
        
        ARG_SLOT args[] =
        {
            ((ARG_SLOT)(CLR_I4)0),
            ObjToArgSlot(*pCLRValue),
            PtrToArgSlot(lpBuffer)
        };
        convertToNative.Call(args);
        MAYBE_UNALIGNED_WRITE(pNativeValue, _PTR, lpBuffer);
    }
}


//=======================================================================
// CoTask Utf8 <--> System.String
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_StringUtf8::UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pNativeValue));
        PRECONDITION(CheckPointer(ppProtectedCLRValue));
    }
    CONTRACTL_END;

    STRINGREF pString = NULL;
    LPCUTF8  sz = (LPCUTF8)MAYBE_UNALIGNED_READ(pNativeValue, _PTR);
    if (!sz)
    {
        pString = NULL;
    }
    else
    {
        MethodDescCallSite convertToManaged(METHOD__CUTF8MARSHALER__CONVERT_TO_MANAGED);
        ARG_SLOT args[] =
        {
            PtrToArgSlot(pNativeValue),
        };
        pString = convertToManaged.Call_RetSTRINGREF(args);
    }
    *((STRINGREF*)ppProtectedCLRValue) = pString;
}

//=======================================================================
// CoTask Utf8 <--> System.String
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_StringUtf8::DestroyNativeImpl(LPVOID pNativeValue) const
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pNativeValue));
    }
    CONTRACTL_END;

    LPCUTF8 lpBuffer = (LPCUTF8)MAYBE_UNALIGNED_READ(pNativeValue, _PTR);
    MAYBE_UNALIGNED_WRITE(pNativeValue, _PTR, NULL);
    if (lpBuffer)
        CoTaskMemFree((LPVOID)lpBuffer);
}

//=======================================================================
// FixedString <--> System.String
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_FixedStringUni::UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const 
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pNativeValue));
    }
    CONTRACTL_END;

    STRINGREF pString;
    *((OBJECTREF*)&pString) = *pCLRValue;
    
    if (pString == NULL)
    {
        MAYBE_UNALIGNED_WRITE(pNativeValue, 16, W('\0'));
    }
    else
    {
        DWORD nc = pString->GetStringLength();
        if (nc >= m_numchar)
            nc = m_numchar - 1;

        memcpyNoGCRefs(pNativeValue, pString->GetBuffer(), nc*sizeof(WCHAR));
        MAYBE_UNALIGNED_WRITE(&(((WCHAR*)pNativeValue)[nc]), 16, W('\0'));
    }

}


//=======================================================================
// FixedString <--> System.String
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_FixedStringUni::UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const 
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pNativeValue));
        PRECONDITION(CheckPointer(ppProtectedCLRValue));
    }
    CONTRACTL_END;

    STRINGREF pString;
    SIZE_T    ncActual = wcsnlen((const WCHAR *)pNativeValue, m_numchar);

    if (!FitsIn<int>(ncActual))
        COMPlusThrowHR(COR_E_OVERFLOW);

    pString = StringObject::NewString((const WCHAR *)pNativeValue, (int)ncActual);
    *((STRINGREF*)ppProtectedCLRValue) = pString;
}







//=======================================================================
// FixedString <--> System.String
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_FixedStringAnsi::UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const 
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pNativeValue));
    }
    CONTRACTL_END;

    STRINGREF pString;
    *((OBJECTREF*)&pString) = *pCLRValue;
    
    if (pString == NULL)
        *((CHAR*)pNativeValue) = W('\0');
    else
    {
        DWORD nc = pString->GetStringLength();
        if (nc >= m_numchar)
            nc = m_numchar - 1;

        int cbwritten = InternalWideToAnsi(pString->GetBuffer(),
            nc,
            (CHAR*)pNativeValue,
            m_numchar,
            m_BestFitMap,
            m_ThrowOnUnmappableChar);

        // Handle the case where SizeConst == Number of bytes.For single byte chars 
        // this will never be the case since nc >= m_numchar check will truncate the last 
        // character, but for multibyte chars nc>= m_numchar check won't truncate since GetStringLength
        // gives number of characters but not the actual number of bytes. For such cases need to make 
        // sure that we dont write one past the buffer.
        if (cbwritten == (int) m_numchar)
            --cbwritten;

        ((CHAR*)pNativeValue)[cbwritten] = '\0';
    }
}


//=======================================================================
// FixedString <--> System.String
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_FixedStringAnsi::UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const 
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pNativeValue));
        PRECONDITION(CheckPointer(ppProtectedCLRValue));

        // should not have slipped past the metadata
        PRECONDITION(m_numchar != 0);
    }
    CONTRACTL_END;

    STRINGREF pString;
    if (m_numchar == 0)
    {
        // but if it does, better to throw an exception tardily rather than
        // allow a memory corrupt.
        COMPlusThrow(kMarshalDirectiveException);
    }

    UINT32 allocSize = m_numchar + 2;
    if (allocSize < m_numchar)
        ThrowOutOfMemory();
    
    LPSTR tempbuf = (LPSTR)(_alloca((size_t)allocSize));
    if (!tempbuf)
        ThrowOutOfMemory();

    memcpyNoGCRefs(tempbuf, pNativeValue, m_numchar);
    tempbuf[m_numchar-1] = '\0';
    tempbuf[m_numchar] = '\0';
    tempbuf[m_numchar+1] = '\0';

    allocSize = m_numchar * sizeof(WCHAR);
    if (allocSize < m_numchar)
        ThrowOutOfMemory();
    
    LPWSTR    wsztemp = (LPWSTR)_alloca( (size_t)allocSize );
    int ncwritten = MultiByteToWideChar(CP_ACP,
                                        MB_PRECOMPOSED,
                                        tempbuf,
                                        -1,  // # of CHAR's in inbuffer
                                        wsztemp,
                                        m_numchar                       // size (in WCHAR) of outbuffer
                                        );

    if (!ncwritten)
    {
        // intentionally not throwing for MB2WC failure. We don't always know
        // whether to expect a valid string in the buffer and we don't want
        // to throw exceptions randomly.
        ncwritten++;
    }

    pString = StringObject::NewString((const WCHAR *)wsztemp, ncwritten-1);
    *((STRINGREF*)ppProtectedCLRValue) = pString;
}


//=======================================================================
// CHAR[] <--> char[]
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_FixedCharArrayAnsi::UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const 
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pNativeValue));
    }
    CONTRACTL_END;

    I2ARRAYREF pArray;
    *((OBJECTREF*)&pArray) = *pCLRValue;

    if (pArray == NULL)
        FillMemory(pNativeValue, m_numElems * sizeof(CHAR), 0);
    else
    {
        if (pArray->GetNumComponents() < m_numElems)
            COMPlusThrow(kArgumentException, IDS_WRONGSIZEARRAY_IN_NSTRUCT);
        else
        {
            InternalWideToAnsi((const WCHAR*) pArray->GetDataPtr(),
                               m_numElems,
                              (CHAR*)pNativeValue,
                               m_numElems * sizeof(CHAR),
                               m_BestFitMap,
                               m_ThrowOnUnmappableChar);
        }
    }
}


//=======================================================================
// CHAR[] <--> char[]
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_FixedCharArrayAnsi::UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const 
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pNativeValue));
        PRECONDITION(CheckPointer(ppProtectedCLRValue));
    }
    CONTRACTL_END;

    *ppProtectedCLRValue = AllocatePrimitiveArray(ELEMENT_TYPE_CHAR, m_numElems);

    MultiByteToWideChar(CP_ACP,
                        MB_PRECOMPOSED,
                        (const CHAR *)pNativeValue,
                        m_numElems * sizeof(CHAR), // size, in bytes, of in buffer
                        (WCHAR*) ((*((I2ARRAYREF*)ppProtectedCLRValue))->GetDirectPointerToNonObjectElements()),
                        m_numElems);               // size, in WCHAR's of outbuffer                       
}

#endif // CROSSGEN_COMPILE


//=======================================================================
// Embedded array
// See FieldMarshaler for details.
//=======================================================================
FieldMarshaler_FixedArray::FieldMarshaler_FixedArray(IMDInternalImport *pMDImport, mdTypeDef cl, UINT32 numElems, VARTYPE vt, MethodTable* pElementMT)
: m_numElems(numElems)
, m_vt(vt)
, m_BestFitMap(FALSE)
, m_ThrowOnUnmappableChar(FALSE)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pElementMT));
        PRECONDITION(vt != VTHACK_ANSICHAR);        // This must be handled by the FixedCharArrayAnsi marshaler.
    }
    CONTRACTL_END;

    // Only attempt to read the best fit mapping attribute if required to minimize
    // custom attribute accesses.
    if (vt == VT_LPSTR || vt == VT_RECORD)
    {
        BOOL BestFitMap = FALSE;
        BOOL ThrowOnUnmappableChar = FALSE;
        ReadBestFitCustomAttribute(pMDImport, cl, &BestFitMap, &ThrowOnUnmappableChar);      
        m_BestFitMap = !!BestFitMap;
        m_ThrowOnUnmappableChar = !!ThrowOnUnmappableChar;
    }

    m_arrayType.SetValue(ClassLoader::LoadArrayTypeThrowing(TypeHandle(pElementMT),
                                                     ELEMENT_TYPE_SZARRAY,
                                                     0,
                                                     ClassLoader::LoadTypes,
                                                     pElementMT->GetLoadLevel()));
}


#ifndef CROSSGEN_COMPILE

//=======================================================================
// Embedded array
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_FixedArray::UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const 
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pNativeValue));
    }
    CONTRACTL_END;

    if (*pCLRValue == NULL)
    {
        FillMemory(pNativeValue, NativeSize(), 0);
    }
    else
    {
        // Make sure the size of the array is >= as specified in the MarshalAs attribute (via the SizeConst field).
        if ((*pCLRValue)->GetNumComponents() < m_numElems)
            COMPlusThrow(kArgumentException, IDS_WRONGSIZEARRAY_IN_NSTRUCT);
  
        // Marshal the contents from the managed array to the native array.
        const OleVariant::Marshaler *pMarshaler = OleVariant::GetMarshalerForVarType(m_vt, TRUE);  
        if (pMarshaler == NULL || pMarshaler->ComToOleArray == NULL)
        {
            memcpyNoGCRefs(pNativeValue, (*(BASEARRAYREF*)pCLRValue)->GetDataPtr(), NativeSize());
        }
        else
        {
            MethodTable *pElementMT = m_arrayType.GetValue().AsArray()->GetArrayElementTypeHandle().GetMethodTable();

            // We never operate on an uninitialized native layout here, we have zero'ed it if needed.
            // Therefore fOleArrayIsValid is always TRUE.
            pMarshaler->ComToOleArray((BASEARRAYREF*)pCLRValue, pNativeValue, pElementMT, m_BestFitMap, m_ThrowOnUnmappableChar, TRUE, m_numElems);
        }
    }
}


//=======================================================================
// Embedded array
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_FixedArray::UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const 
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pNativeValue));
        PRECONDITION(CheckPointer(ppProtectedCLRValue));
    }
    CONTRACTL_END;

    // Allocate the value class array.
    *ppProtectedCLRValue = AllocateArrayEx(m_arrayType.GetValue(), (INT32*)&m_numElems, 1);

    // Marshal the contents from the native array to the managed array.
    const OleVariant::Marshaler *pMarshaler = OleVariant::GetMarshalerForVarType(m_vt, TRUE);        
    if (pMarshaler == NULL || pMarshaler->OleToComArray == NULL)
    {
        memcpyNoGCRefs((*(BASEARRAYREF*)ppProtectedCLRValue)->GetDataPtr(), pNativeValue, NativeSize());
    }
    else
    {
        MethodTable *pElementMT = m_arrayType.GetValue().AsArray()->GetArrayElementTypeHandle().GetMethodTable();
        pMarshaler->OleToComArray((VOID *)pNativeValue, (BASEARRAYREF*)ppProtectedCLRValue, pElementMT);
    }
}

//=======================================================================
// Embedded array
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_FixedArray::DestroyNativeImpl(LPVOID pNativeValue) const
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pNativeValue));
    }
    CONTRACTL_END;

    const OleVariant::Marshaler *pMarshaler = OleVariant::GetMarshalerForVarType(m_vt, FALSE);

    if (pMarshaler != NULL && pMarshaler->ClearOleArray != NULL)
    {
        MethodTable *pElementMT = m_arrayType.GetValue().AsArray()->GetArrayElementTypeHandle().GetMethodTable();
        pMarshaler->ClearOleArray(pNativeValue, m_numElems, pElementMT);
    }
}

#endif // CROSSGEN_COMPILE


//=======================================================================
// Embedded array
// See FieldMarshaler for details.
//=======================================================================
UINT32 FieldMarshaler_FixedArray::AlignmentRequirementImpl() const
{
    WRAPPER_NO_CONTRACT;

    UINT32 alignment = 0;
    TypeHandle elementType = m_arrayType.GetValue().AsArray()->GetArrayElementTypeHandle();

    switch (m_vt)
    {
        case VT_DECIMAL:
            alignment = 8;
            break;

        case VT_VARIANT:
            alignment = 8;
            break;

        case VT_RECORD:
            alignment = elementType.GetMethodTable()->GetLayoutInfo()->GetLargestAlignmentRequirementOfAllMembers();    
            break;

        default:
            alignment = OleVariant::GetElementSizeForVarType(m_vt, elementType.GetMethodTable());
            break;
    }

    return alignment;
}
  
#ifndef CROSSGEN_COMPILE

#ifdef FEATURE_CLASSIC_COMINTEROP
//=======================================================================
// SafeArray
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_SafeArray::UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const 
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pNativeValue));
    }
    CONTRACTL_END;

    BASEARRAYREF pArray;
    *((OBJECTREF*)&pArray) = *pCLRValue;
    if ((pArray == NULL) || (OBJECTREFToObject(pArray) == NULL))
    {
        FillMemory(pNativeValue, sizeof(LPSAFEARRAY*), 0);
        return;
    }
    
    LPSAFEARRAY* pSafeArray;
    pSafeArray = (LPSAFEARRAY*)pNativeValue;

    VARTYPE vt = m_vt;
    MethodTable* pMT = m_pMT.GetValue();

    GCPROTECT_BEGIN(pArray)
    {
        if (vt == VT_EMPTY)
            vt = OleVariant::GetElementVarTypeForArrayRef(pArray);

        if (!pMT)
            pMT = OleVariant::GetArrayElementTypeWrapperAware(&pArray).GetMethodTable();

        // OleVariant calls throw on error.
        *pSafeArray = OleVariant::CreateSafeArrayForArrayRef(&pArray, vt, pMT);
        OleVariant::MarshalSafeArrayForArrayRef(&pArray, *pSafeArray, vt, pMT);
    }
    GCPROTECT_END();
}


//=======================================================================
// SafeArray
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_SafeArray::UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const 
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pNativeValue));
        PRECONDITION(CheckPointer(ppProtectedCLRValue));
    }
    CONTRACTL_END;

    LPSAFEARRAY* pSafeArray;
    pSafeArray = (LPSAFEARRAY*)pNativeValue;

    if ((pSafeArray == NULL) || (*pSafeArray == NULL))
    {
        *ppProtectedCLRValue = NULL;
        return;
    }

    VARTYPE vt = m_vt;
    MethodTable* pMT = m_pMT.GetValue();

    // If we have an empty vartype, get it from the safearray vartype
    if (vt == VT_EMPTY)
    {
        if (FAILED(ClrSafeArrayGetVartype(*pSafeArray, &vt)))
            COMPlusThrow(kArgumentException, IDS_EE_INVALID_SAFEARRAY);
    }

    // Get the method table if we need to.
    if ((vt == VT_RECORD) && (!pMT))
        pMT = OleVariant::GetElementTypeForRecordSafeArray(*pSafeArray).GetMethodTable();

    // If we have a single dimension safearray, it will be converted into a SZArray.
    // SZArray must have a lower bound of zero.
    LONG LowerBound = -1;
    UINT Dimensions = SafeArrayGetDim( (SAFEARRAY*)*pSafeArray );    

    if (Dimensions == 1)
    {
        HRESULT hr = SafeArrayGetLBound((SAFEARRAY*)*pSafeArray, 1, &LowerBound);
        if ( FAILED(hr) || LowerBound != 0)
        COMPlusThrow(kSafeArrayRankMismatchException, IDS_EE_SAFEARRAYSZARRAYMISMATCH);
    }
    
    // OleVariant calls throw on error.
    *ppProtectedCLRValue = OleVariant::CreateArrayRefForSafeArray(*pSafeArray, vt, pMT);
    OleVariant::MarshalArrayRefForSafeArray(*pSafeArray, (BASEARRAYREF*)ppProtectedCLRValue, vt, pMT);
}


//=======================================================================
// SafeArray
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_SafeArray::DestroyNativeImpl(LPVOID pNativeValue) const 
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pNativeValue));
    }
    CONTRACTL_END;

    HRESULT hr;
    GCX_PREEMP();

    LPSAFEARRAY psa = (LPSAFEARRAY)MAYBE_UNALIGNED_READ(pNativeValue, _PTR);
    MAYBE_UNALIGNED_WRITE(pNativeValue, _PTR, NULL);
    
    if (psa)
    {
        _ASSERTE (GetModuleHandleA("oleaut32.dll") != NULL);
        // SafeArray has been created, which means oleaut32 should have been loaded.
        // Delay load will not fail.
        CONTRACT_VIOLATION(ThrowsViolation);
        hr = SafeArrayDestroy(psa);
        _ASSERTE(!FAILED(hr));        
    }
}
#endif //FEATURE_CLASSIC_COMINTEROP


//=======================================================================
// function ptr <--> Delegate
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_Delegate::UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const 
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pNativeValue));
    }
    CONTRACTL_END;

    LPVOID fnPtr = COMDelegate::ConvertToCallback(*pCLRValue);
    MAYBE_UNALIGNED_WRITE(pNativeValue, _PTR, fnPtr);
}


//=======================================================================
// function ptr <--> Delegate
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_Delegate::UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const 
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pNativeValue));
        PRECONDITION(CheckPointer(ppProtectedCLRValue));
    }
    CONTRACTL_END;

    *ppProtectedCLRValue = COMDelegate::ConvertToDelegate((LPVOID)MAYBE_UNALIGNED_READ(pNativeValue, _PTR), GetMethodTable());
}


//=======================================================================
// SafeHandle <--> Handle
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_SafeHandle::UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const 
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pNativeValue));
        PRECONDITION(CheckPointer(ppCleanupWorkListOnStack, NULL_OK));
    }
    CONTRACTL_END;

    SAFEHANDLE *pSafeHandleObj = ((SAFEHANDLE *)pCLRValue);

    // A cleanup list MUST be specified in order for us to be able to marshal
    // the SafeHandle.
    if (ppCleanupWorkListOnStack == NULL)
        COMPlusThrow(kInvalidOperationException, IDS_EE_SH_FIELD_INVALID_OPERATION);

    if (*pSafeHandleObj == NULL)
        COMPlusThrow(kArgumentNullException, W("ArgumentNull_SafeHandle"));

    // Call StubHelpers.AddToCleanupList to AddRef and schedule Release on this SafeHandle
    // This is realiable, i.e. the cleanup will happen if and only if the SH was actually AddRef'ed.
    MethodDescCallSite AddToCleanupList(METHOD__STUBHELPERS__ADD_TO_CLEANUP_LIST);

    ARG_SLOT args[] =
    {
        (ARG_SLOT)ppCleanupWorkListOnStack,
        ObjToArgSlot(*pSafeHandleObj)
    };

    LPVOID handle = AddToCleanupList.Call_RetLPVOID(args);

    MAYBE_UNALIGNED_WRITE(pNativeValue, _PTR, handle);
}


//=======================================================================
// SafeHandle <--> Handle
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_SafeHandle::UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const 
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pNativeValue));
        PRECONDITION(CheckPointer(ppProtectedCLRValue));
        PRECONDITION(CheckPointer(ppProtectedOldCLRValue));
    }
    CONTRACTL_END;

    // Since we dissallow marshaling SafeHandle fields from unmanaged to managed, check
    // to see if this handle was obtained from a SafeHandle and if it was that the
    // handle value hasn't changed.
    SAFEHANDLE *pSafeHandleObj = (SAFEHANDLE *)ppProtectedOldCLRValue;
    if (!*pSafeHandleObj || (*pSafeHandleObj)->GetHandle() != (LPVOID)MAYBE_UNALIGNED_READ(pNativeValue, _PTR))
        COMPlusThrow(kNotSupportedException, IDS_EE_CANNOT_CREATE_SAFEHANDLE_FIELD);

    // Now that we know the handle hasn't changed we just copy set the new SafeHandle
    // to the old one.
    *ppProtectedCLRValue = *ppProtectedOldCLRValue;
}


//=======================================================================
// CriticalHandle <--> Handle
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_CriticalHandle::UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const 
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pNativeValue));
    }
    CONTRACTL_END;

    LPVOID handle = ((CRITICALHANDLE)*pCLRValue)->GetHandle();
    MAYBE_UNALIGNED_WRITE(pNativeValue, _PTR, handle);
}


//=======================================================================
// CriticalHandle <--> Handle
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_CriticalHandle::UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const 
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pNativeValue));
        PRECONDITION(CheckPointer(ppProtectedCLRValue));
        PRECONDITION(CheckPointer(ppProtectedOldCLRValue));
    }
    CONTRACTL_END;

    // Since we dissallow marshaling CriticalHandle fields from unmanaged to managed, check
    // to see if this handle was obtained from a CriticalHandle and if it was that the
    // handle value hasn't changed.
    CRITICALHANDLE *pCriticalHandleObj = (CRITICALHANDLE *)ppProtectedOldCLRValue;
    if (!*pCriticalHandleObj || (*pCriticalHandleObj)->GetHandle() != (LPVOID)MAYBE_UNALIGNED_READ(pNativeValue, _PTR))
        COMPlusThrow(kNotSupportedException, IDS_EE_CANNOT_CREATE_CRITICALHANDLE_FIELD);

    // Now that we know the handle hasn't changed we just copy set the new CriticalHandle
    // to the old one.
    *ppProtectedCLRValue = *ppProtectedOldCLRValue;
}

#ifdef FEATURE_COMINTEROP

//=======================================================================
// COM IP <--> interface
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_Interface::UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const 
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pNativeValue));
    }
    CONTRACTL_END;

    IUnknown *pUnk = NULL;

    if (!m_pItfMT.IsNull())
    {
        pUnk = GetComIPFromObjectRef(pCLRValue, GetInterfaceMethodTable());
    }
    else if (!(m_dwFlags & ItfMarshalInfo::ITF_MARSHAL_USE_BASIC_ITF))
    {
        pUnk = GetComIPFromObjectRef(pCLRValue, GetMethodTable());
    }
    else
    {
        ComIpType ReqIpType = !!(m_dwFlags & ItfMarshalInfo::ITF_MARSHAL_DISP_ITF) ? ComIpType_Dispatch : ComIpType_Unknown;
        pUnk = GetComIPFromObjectRef(pCLRValue, ReqIpType, NULL);
    }

    MAYBE_UNALIGNED_WRITE(pNativeValue, _PTR, pUnk);    
}


//=======================================================================
// COM IP <--> interface
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_Interface::UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const 
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pNativeValue));
        PRECONDITION(CheckPointer(ppProtectedCLRValue));
        PRECONDITION(IsProtectedByGCFrame(ppProtectedCLRValue));
    }
    CONTRACTL_END;

    IUnknown *pUnk = (IUnknown*)MAYBE_UNALIGNED_READ(pNativeValue, _PTR);

    MethodTable *pItfMT = GetInterfaceMethodTable();
    if (pItfMT != NULL && !pItfMT->IsInterface())
        pItfMT = NULL;

    GetObjectRefFromComIP(
        ppProtectedCLRValue,                                    // Created object
        pUnk,                                                   // Interface pointer
        GetMethodTable(),                                       // Class MT
        pItfMT,                                                 // Interface MT
        (m_dwFlags & ItfMarshalInfo::ITF_MARSHAL_CLASS_IS_HINT) // Flags
    );
}


//=======================================================================
// COM IP <--> interface
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_Interface::DestroyNativeImpl(LPVOID pNativeValue) const 
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pNativeValue));
    }
    CONTRACTL_END;

    IUnknown *pUnk = (IUnknown*)MAYBE_UNALIGNED_READ(pNativeValue, _PTR);
    MAYBE_UNALIGNED_WRITE(pNativeValue, _PTR, NULL);

    if (pUnk != NULL)
    {
        ULONG cbRef = SafeRelease(pUnk);
        LogInteropRelease(pUnk, cbRef, "Field marshaler destroy native");
    }
}

#endif // FEATURE_COMINTEROP


//=======================================================================
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_Date::ScalarUpdateNativeImpl(LPVOID pCLR, LPVOID pNative) const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pCLR));
        PRECONDITION(CheckPointer(pNative));
    }
    CONTRACTL_END;

    // <TODO> Handle unaligned native fields </TODO>
    *((DATE*)pNative) =  COMDateTime::TicksToDoubleDate(*((INT64*)pCLR));
}


//=======================================================================
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_Date::ScalarUpdateCLRImpl(const VOID *pNative, LPVOID pCLR) const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pNative));
        PRECONDITION(CheckPointer(pCLR));
    }
    CONTRACTL_END;

    // <TODO> Handle unaligned native fields </TODO>
    *((INT64*)pCLR) = COMDateTime::DoubleDateToTicks(*((DATE*)pNative));
}


#ifdef FEATURE_COMINTEROP

//=======================================================================
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_Currency::ScalarUpdateNativeImpl(LPVOID pCLR, LPVOID pNative) const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pCLR));
        PRECONDITION(CheckPointer(pNative));
    }
    CONTRACTL_END;

    // no need to switch to preemptive mode because it's very primitive operaion, doesn't take 
    // long and is guaranteed not to call 3rd party code. 
    // But if we do need to switch to preemptive mode, we can't pass the managed pointer to native code directly
    HRESULT hr = VarCyFromDec( (DECIMAL *)pCLR, (CURRENCY*)pNative);
    if (FAILED(hr))
        COMPlusThrowHR(hr);

}


//=======================================================================
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_Currency::ScalarUpdateCLRImpl(const VOID *pNative, LPVOID pCLR) const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pNative));
        PRECONDITION(CheckPointer(pCLR));
    }
    CONTRACTL_END;

    // no need to switch to preemptive mode because it's very primitive operaion, doesn't take 
    // long and is guaranteed not to call 3rd party code. 
    // But if we do need to switch to preemptive mode, we can't pass the managed pointer to native code directly
    HRESULT hr = VarDecFromCy( *(CURRENCY*)pNative, (DECIMAL *)pCLR );
    if (FAILED(hr))
        COMPlusThrowHR(hr);

    if (FAILED(DecimalCanonicalize((DECIMAL*)pCLR)))
        COMPlusThrow(kOverflowException, W("Overflow_Currency"));
}

VOID FieldMarshaler_DateTimeOffset::ScalarUpdateNativeImpl(LPVOID pCLR, LPVOID pNative) const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pCLR));
        PRECONDITION(CheckPointer(pNative));
    }
    CONTRACTL_END;

    MethodDescCallSite convertToNative(METHOD__DATETIMEOFFSETMARSHALER__CONVERT_TO_NATIVE);
    ARG_SLOT args[] =
    {
        PtrToArgSlot(pCLR),
        PtrToArgSlot(pNative)
    };
    convertToNative.Call(args);
}

VOID FieldMarshaler_DateTimeOffset::ScalarUpdateCLRImpl(const VOID *pNative, LPVOID pCLR) const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pNative));
        PRECONDITION(CheckPointer(pCLR));
    }
    CONTRACTL_END;

    MethodDescCallSite convertToManaged(METHOD__DATETIMEOFFSETMARSHALER__CONVERT_TO_MANAGED);
    ARG_SLOT args[] =
    {
        PtrToArgSlot(pCLR),
        PtrToArgSlot(pNative)
    };
    convertToManaged.Call(args);
}

#endif // FEATURE_COMINTEROP


//=======================================================================
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_Illegal::ScalarUpdateNativeImpl(LPVOID pCLR, LPVOID pNative) const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pCLR));
        PRECONDITION(CheckPointer(pNative));
    }
    CONTRACTL_END;

    DefineFullyQualifiedNameForClassW();

    StackSString ssFieldName(SString::Utf8, GetFieldDesc()->GetName());

    StackSString errorString(W("Unknown error."));
    errorString.LoadResource(CCompRC::Error, m_resIDWhy);

    COMPlusThrow(kTypeLoadException, IDS_EE_BADMARSHALFIELD_ERROR_MSG,
                 GetFullyQualifiedNameForClassW(GetFieldDesc()->GetEnclosingMethodTable()),
                 ssFieldName.GetUnicode(), errorString.GetUnicode());
}


//=======================================================================
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_Illegal::ScalarUpdateCLRImpl(const VOID *pNative, LPVOID pCLR) const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pNative));
        PRECONDITION(CheckPointer(pCLR));
    }
    CONTRACTL_END;

    DefineFullyQualifiedNameForClassW();

    StackSString ssFieldName(SString::Utf8, GetFieldDesc()->GetName());

    StackSString errorString(W("Unknown error."));
    errorString.LoadResource(CCompRC::Error,m_resIDWhy);

    COMPlusThrow(kTypeLoadException, IDS_EE_BADMARSHALFIELD_ERROR_MSG, 
                 GetFullyQualifiedNameForClassW(GetFieldDesc()->GetEnclosingMethodTable()),
                 ssFieldName.GetUnicode(), errorString.GetUnicode());
}

#ifdef FEATURE_COMINTEROP


//=======================================================================
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_Variant::UpdateNativeImpl(OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const 
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pNativeValue));
    }
    CONTRACTL_END;

    OleVariant::MarshalOleVariantForObject(pCLRValue, (VARIANT*)pNativeValue);

}


//=======================================================================
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_Variant::UpdateCLRImpl(const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const 
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pNativeValue));
        PRECONDITION(CheckPointer(ppProtectedCLRValue));
    }
    CONTRACTL_END;

    OleVariant::MarshalObjectForOleVariant((VARIANT*)pNativeValue, ppProtectedCLRValue);
}


//=======================================================================
// See FieldMarshaler for details.
//=======================================================================
VOID FieldMarshaler_Variant::DestroyNativeImpl(LPVOID pNativeValue) const 
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pNativeValue));
    }
    CONTRACTL_END;

    SafeVariantClear( (VARIANT*)pNativeValue );
}

#endif // FEATURE_COMINTEROP


#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
VOID NStructFieldTypeToString(FieldMarshaler* pFM, SString& strNStructFieldType)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pFM));
    }
    CONTRACTL_END;

    NStructFieldType cls = pFM->GetNStructFieldType();
    LPCWSTR  strRetVal;
    CorElementType elemType = pFM->GetFieldDesc()->GetFieldType();

    // Some NStruct Field Types have extra information and require special handling.
    if (cls == NFT_FIXEDCHARARRAYANSI)
    {
        strNStructFieldType.Printf(W("fixed array of ANSI char (size = %i bytes)"), pFM->NativeSize());
        return;
    }
    else if (cls == NFT_FIXEDARRAY)
    {
        VARTYPE vtElement = ((FieldMarshaler_FixedArray*)pFM)->GetElementVT();
        TypeHandle thElement = ((FieldMarshaler_FixedArray*)pFM)->GetElementTypeHandle();
        BOOL fElementTypeUserDefined = FALSE;

        // Determine if the array type is a user defined type.
        if (vtElement == VT_RECORD)
        {
            fElementTypeUserDefined = TRUE;
        }
        else if (vtElement == VT_UNKNOWN || vtElement == VT_DISPATCH)
        {
            fElementTypeUserDefined = !thElement.IsObjectType();
        }

        // Retrieve the string representation for the VARTYPE.
        StackSString strVarType;
        MarshalInfo::VarTypeToString(vtElement, strVarType);

        MethodTable *pMT = ((FieldMarshaler_FixedArray*)pFM)->GetElementTypeHandle().GetMethodTable();
        DefineFullyQualifiedNameForClassW();
        WCHAR* szClassName = (WCHAR*)GetFullyQualifiedNameForClassW(pMT);

        if (fElementTypeUserDefined)
        {
            strNStructFieldType.Printf(W("fixed array of %s exposed as %s elements (array size = %i bytes)"),
                                       szClassName,
                                       strVarType.GetUnicode(), pFM->NativeSize());
        }
        else
        {
            strNStructFieldType.Printf(W("fixed array of %s (array size = %i bytes)"), 
                szClassName, pFM->NativeSize());
        }

        return;
    }
#ifdef FEATURE_COMINTEROP
    else if (cls == NFT_INTERFACE)
    {
        MethodTable *pItfMT     = NULL;
        DWORD       dwFlags     = 0;

        ((FieldMarshaler_Interface*)pFM)->GetInterfaceInfo(&pItfMT, &dwFlags);

        if (dwFlags & ItfMarshalInfo::ITF_MARSHAL_DISP_ITF)
        {
            strNStructFieldType.Set(W("IDispatch "));
        }
        else
        {
            strNStructFieldType.Set(W("IUnknown "));
        }

        if (dwFlags & ItfMarshalInfo::ITF_MARSHAL_USE_BASIC_ITF)
        {
            strNStructFieldType.Append(W("(basic) "));
        }
            

        if (pItfMT)
        {
            DefineFullyQualifiedNameForClassW();
            GetFullyQualifiedNameForClassW(pItfMT);

            strNStructFieldType.Append(GetFullyQualifiedNameForClassW(pItfMT));
        }

        return;
    }
#ifdef FEATURE_CLASSIC_COMINTEROP
    else if (cls == NFT_SAFEARRAY)
    {
        VARTYPE vtElement = ((FieldMarshaler_SafeArray*)pFM)->GetElementVT();
        TypeHandle thElement = ((FieldMarshaler_SafeArray*)pFM)->GetElementTypeHandle();
        BOOL fElementTypeUserDefined = FALSE;

        // Determine if the array type is a user defined type.
        if (vtElement == VT_RECORD)
        {
            fElementTypeUserDefined = TRUE;
        }
        else if (vtElement == VT_UNKNOWN || vtElement == VT_DISPATCH)
        {
            fElementTypeUserDefined = !thElement.IsObjectType();
        }

        // Retrieve the string representation for the VARTYPE.
        StackSString strVarType;
        MarshalInfo::VarTypeToString(vtElement, strVarType);


        StackSString strClassName;
        if (!thElement.IsNull())
        {
            DefineFullyQualifiedNameForClassW();
            MethodTable *pMT = ((FieldMarshaler_SafeArray*)pFM)->GetElementTypeHandle().GetMethodTable();
            strClassName.Set((WCHAR*)GetFullyQualifiedNameForClassW(pMT));
        }
        else
        {
            strClassName.Set(W("object"));
        }
        
        if (fElementTypeUserDefined)
        {
            strNStructFieldType.Printf(W("safe array of %s exposed as %s elements (array size = %i bytes)"),
                                       strClassName.GetUnicode(),
                                       strVarType.GetUnicode(), pFM->NativeSize());
        }
        else
        {
            strNStructFieldType.Printf(W("safearray of %s (array size = %i bytes)"), 
                strClassName.GetUnicode(), pFM->NativeSize());
        }
        
        return;            
    }
#endif // FEATURE_CLASSIC_COMINTEROP
#endif // FEATURE_COMINTEROP
    else if (cls == NFT_NESTEDLAYOUTCLASS)
    {
        MethodTable *pMT = ((FieldMarshaler_NestedLayoutClass*)pFM)->GetMethodTable();
        DefineFullyQualifiedNameForClassW();
        strNStructFieldType.Printf(W("nested layout class %s"),
                                   GetFullyQualifiedNameForClassW(pMT));
        return;
    }
    else if (cls == NFT_NESTEDVALUECLASS)
    {
        MethodTable     *pMT                = ((FieldMarshaler_NestedValueClass*)pFM)->GetMethodTable();
        DefineFullyQualifiedNameForClassW();
        strNStructFieldType.Printf(W("nested value class %s"),
                                   GetFullyQualifiedNameForClassW(pMT));
        return;
    }
    else if (cls == NFT_COPY1)
    {
        // The following CorElementTypes are the only ones handled with FieldMarshaler_Copy1. 
        switch (elemType)
        {
            case ELEMENT_TYPE_I1:
                strRetVal = W("SByte");
                break;

            case ELEMENT_TYPE_U1:
                strRetVal = W("Byte");
                break;

            default:
                strRetVal = W("Unknown");
                break;
        }
    }
    else if (cls == NFT_COPY2)
    {
        // The following CorElementTypes are the only ones handled with FieldMarshaler_Copy2. 
        switch (elemType)
        {
            case ELEMENT_TYPE_CHAR:
                strRetVal = W("Unicode char");
                break;

            case ELEMENT_TYPE_I2:
                strRetVal = W("Int16");
                break;

            case ELEMENT_TYPE_U2:
                strRetVal = W("UInt16");
                break;

            default:
                strRetVal = W("Unknown");
                break;
        }
    }
    else if (cls == NFT_COPY4)
    {
        // The following CorElementTypes are the only ones handled with FieldMarshaler_Copy4. 
        switch (elemType)
        {
            // At this point, ELEMENT_TYPE_I must be 4 bytes long.  Same for ELEMENT_TYPE_U.
            case ELEMENT_TYPE_I:
            case ELEMENT_TYPE_I4:
                strRetVal = W("Int32");
                break;

            case ELEMENT_TYPE_U:
            case ELEMENT_TYPE_U4:
                strRetVal = W("UInt32");
                break;

            case ELEMENT_TYPE_R4:
                strRetVal = W("Single");
                break;

            case ELEMENT_TYPE_PTR:
                strRetVal = W("4-byte pointer");
                break;

            default:
                strRetVal = W("Unknown");
                break;
        }
    }
    else if (cls == NFT_COPY8)
    {
        // The following CorElementTypes are the only ones handled with FieldMarshaler_Copy8. 
        switch (elemType)
        {
            // At this point, ELEMENT_TYPE_I must be 8 bytes long.  Same for ELEMENT_TYPE_U.
            case ELEMENT_TYPE_I:
            case ELEMENT_TYPE_I8:
                strRetVal = W("Int64");
                break;

            case ELEMENT_TYPE_U:
            case ELEMENT_TYPE_U8:
                strRetVal = W("UInt64");
                break;

            case ELEMENT_TYPE_R8:
                strRetVal = W("Double");
                break;

            case ELEMENT_TYPE_PTR:
                strRetVal = W("8-byte pointer");
                break;

            default:
                strRetVal = W("Unknown");
                break;
        }
    }
    else if (cls == NFT_FIXEDSTRINGUNI)
    {
        int nativeSize = pFM->NativeSize();
        int strLength = nativeSize / sizeof(WCHAR);

        strNStructFieldType.Printf(W("embedded LPWSTR (length %d)"), strLength);
        
        return;
    }
    else if (cls == NFT_FIXEDSTRINGANSI)
    {
        int nativeSize = pFM->NativeSize();
        int strLength = nativeSize / sizeof(CHAR);

        strNStructFieldType.Printf(W("embedded LPSTR (length %d)"), strLength);

        return;
    }
    else
    {
        // All other NStruct Field Types which do not require special handling.
        switch (cls)
        {
#ifdef FEATURE_COMINTEROP
            case NFT_BSTR:
                strRetVal = W("BSTR");
                break;
            case NFT_HSTRING:
                strRetVal = W("HSTRING");
                break;
#endif  // FEATURE_COMINTEROP
            case NFT_STRINGUNI:
                strRetVal = W("LPWSTR");
                break;
            case NFT_STRINGANSI:
                strRetVal = W("LPSTR");
                break;
            case NFT_DELEGATE:
                strRetVal = W("Delegate");
                break;
#ifdef FEATURE_COMINTEROP
            case NFT_VARIANT:
                strRetVal = W("VARIANT");
                break;
#endif  // FEATURE_COMINTEROP
            case NFT_ANSICHAR:
                strRetVal = W("ANSI char");
                break;
            case NFT_WINBOOL:
                strRetVal = W("Windows Bool");
                break;
            case NFT_CBOOL:
                strRetVal = W("CBool");
                break;
            case NFT_DECIMAL:
                strRetVal = W("DECIMAL");
                break;
            case NFT_DATE:
                strRetVal = W("DATE");
                break;
#ifdef FEATURE_COMINTEROP
            case NFT_VARIANTBOOL:
                strRetVal = W("VARIANT Bool");
                break;
            case NFT_CURRENCY:
                strRetVal = W("CURRENCY");
                break;
#endif  // FEATURE_COMINTEROP
            case NFT_ILLEGAL:
                strRetVal = W("illegal type");
                break;
            case NFT_SAFEHANDLE:
                strRetVal = W("SafeHandle");
                break;
            case NFT_CRITICALHANDLE:
                strRetVal = W("CriticalHandle");
                break;
            default:
                strRetVal = W("<UNKNOWN>");
                break;
        }
    }

    strNStructFieldType.Set(strRetVal);

    return;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

#endif // CROSSGEN_COMPILE


//
// Implementation of the virtual functions using switch statements.
//
// We are not able bake pointers to the FieldMarshaller vtables into NGen images. We store
// the field marshaller id instead, and implement the virtualization by switch based on the id.
//

#ifdef FEATURE_CLASSIC_COMINTEROP
#define FieldMarshaler_SafeArray_Case(rettype, name, args) case NFT_SAFEARRAY: rettype ((FieldMarshaler_SafeArray*)this)->name##Impl args; break;
#else
#define FieldMarshaler_SafeArray_Case(rettype, name, args)
#endif

#ifdef FEATURE_COMINTEROP

#define IMPLEMENT_FieldMarshaler_METHOD(ret, name, argsdecl, rettype, args) \
    ret FieldMarshaler::name argsdecl { \
        WRAPPER_NO_CONTRACT; \
        switch (GetNStructFieldType()) { \
        case NFT_STRINGUNI: rettype ((FieldMarshaler_StringUni*)this)->name##Impl args; break; \
        case NFT_STRINGANSI: rettype ((FieldMarshaler_StringAnsi*)this)->name##Impl args; break; \
        case NFT_STRINGUTF8: rettype ((FieldMarshaler_StringUtf8*)this)->name##Impl args; break; \
        case NFT_FIXEDSTRINGUNI: rettype ((FieldMarshaler_FixedStringUni*)this)->name##Impl args; break; \
        case NFT_FIXEDSTRINGANSI: rettype ((FieldMarshaler_FixedStringAnsi*)this)->name##Impl args; break; \
        case NFT_FIXEDCHARARRAYANSI: rettype ((FieldMarshaler_FixedCharArrayAnsi*)this)->name##Impl args; break; \
        case NFT_FIXEDARRAY: rettype ((FieldMarshaler_FixedArray*)this)->name##Impl args; break; \
        case NFT_DELEGATE: rettype ((FieldMarshaler_Delegate*)this)->name##Impl args; break; \
        case NFT_COPY1: rettype ((FieldMarshaler_Copy1*)this)->name##Impl args; break; \
        case NFT_COPY2: rettype ((FieldMarshaler_Copy2*)this)->name##Impl args; break; \
        case NFT_COPY4: rettype ((FieldMarshaler_Copy4*)this)->name##Impl args; break; \
        case NFT_COPY8: rettype ((FieldMarshaler_Copy8*)this)->name##Impl args; break; \
        case NFT_ANSICHAR: rettype ((FieldMarshaler_Ansi*)this)->name##Impl args; break; \
        case NFT_WINBOOL: rettype ((FieldMarshaler_WinBool*)this)->name##Impl args; break; \
        case NFT_NESTEDLAYOUTCLASS: rettype ((FieldMarshaler_NestedLayoutClass*)this)->name##Impl args; break; \
        case NFT_NESTEDVALUECLASS: rettype ((FieldMarshaler_NestedValueClass*)this)->name##Impl args; break; \
        case NFT_CBOOL: rettype ((FieldMarshaler_CBool*)this)->name##Impl args; break; \
        case NFT_DATE: rettype ((FieldMarshaler_Date*)this)->name##Impl args; break; \
        case NFT_DECIMAL: rettype ((FieldMarshaler_Decimal*)this)->name##Impl args; break; \
        case NFT_INTERFACE: rettype ((FieldMarshaler_Interface*)this)->name##Impl args; break; \
        case NFT_SAFEHANDLE: rettype ((FieldMarshaler_SafeHandle*)this)->name##Impl args; break; \
        case NFT_CRITICALHANDLE: rettype ((FieldMarshaler_CriticalHandle*)this)->name##Impl args; break; \
        FieldMarshaler_SafeArray_Case(rettype, name, args) \
        case NFT_BSTR: rettype ((FieldMarshaler_BSTR*)this)->name##Impl args; break; \
        case NFT_HSTRING: rettype ((FieldMarshaler_HSTRING*)this)->name##Impl args; break; \
        case NFT_VARIANT: rettype ((FieldMarshaler_Variant*)this)->name##Impl args; break; \
        case NFT_VARIANTBOOL: rettype ((FieldMarshaler_VariantBool*)this)->name##Impl args; break; \
        case NFT_CURRENCY: rettype ((FieldMarshaler_Currency*)this)->name##Impl args; break; \
        case NFT_DATETIMEOFFSET: rettype ((FieldMarshaler_DateTimeOffset*)this)->name##Impl args; break; \
        case NFT_SYSTEMTYPE: rettype ((FieldMarshaler_SystemType *)this)->name##Impl args; break; \
        case NFT_WINDOWSFOUNDATIONHRESULT: rettype ((FieldMarshaler_Exception*)this)->name##Impl args; break; \
        case NFT_WINDOWSFOUNDATIONIREFERENCE: rettype ((FieldMarshaler_Nullable*)this)->name##Impl args; break; \
        case NFT_ILLEGAL: rettype ((FieldMarshaler_Illegal*)this)->name##Impl args; break; \
        default: UNREACHABLE_MSG("unexpected type of FieldMarshaler"); break; \
        } \
    }

#else // FEATURE_COMINTEROP

#define IMPLEMENT_FieldMarshaler_METHOD(ret, name, argsdecl, rettype, args) \
    ret FieldMarshaler::name argsdecl { \
        WRAPPER_NO_CONTRACT; \
        switch (GetNStructFieldType()) { \
        case NFT_STRINGUNI: rettype ((FieldMarshaler_StringUni*)this)->name##Impl args; break; \
        case NFT_STRINGANSI: rettype ((FieldMarshaler_StringAnsi*)this)->name##Impl args; break; \
        case NFT_STRINGUTF8: rettype ((FieldMarshaler_StringUtf8*)this)->name##Impl args; break; \
        case NFT_FIXEDSTRINGUNI: rettype ((FieldMarshaler_FixedStringUni*)this)->name##Impl args; break; \
        case NFT_FIXEDSTRINGANSI: rettype ((FieldMarshaler_FixedStringAnsi*)this)->name##Impl args; break; \
        case NFT_FIXEDCHARARRAYANSI: rettype ((FieldMarshaler_FixedCharArrayAnsi*)this)->name##Impl args; break; \
        case NFT_FIXEDARRAY: rettype ((FieldMarshaler_FixedArray*)this)->name##Impl args; break; \
        case NFT_DELEGATE: rettype ((FieldMarshaler_Delegate*)this)->name##Impl args; break; \
        case NFT_COPY1: rettype ((FieldMarshaler_Copy1*)this)->name##Impl args; break; \
        case NFT_COPY2: rettype ((FieldMarshaler_Copy2*)this)->name##Impl args; break; \
        case NFT_COPY4: rettype ((FieldMarshaler_Copy4*)this)->name##Impl args; break; \
        case NFT_COPY8: rettype ((FieldMarshaler_Copy8*)this)->name##Impl args; break; \
        case NFT_ANSICHAR: rettype ((FieldMarshaler_Ansi*)this)->name##Impl args; break; \
        case NFT_WINBOOL: rettype ((FieldMarshaler_WinBool*)this)->name##Impl args; break; \
        case NFT_NESTEDLAYOUTCLASS: rettype ((FieldMarshaler_NestedLayoutClass*)this)->name##Impl args; break; \
        case NFT_NESTEDVALUECLASS: rettype ((FieldMarshaler_NestedValueClass*)this)->name##Impl args; break; \
        case NFT_CBOOL: rettype ((FieldMarshaler_CBool*)this)->name##Impl args; break; \
        case NFT_DATE: rettype ((FieldMarshaler_Date*)this)->name##Impl args; break; \
        case NFT_DECIMAL: rettype ((FieldMarshaler_Decimal*)this)->name##Impl args; break; \
        case NFT_SAFEHANDLE: rettype ((FieldMarshaler_SafeHandle*)this)->name##Impl args; break; \
        case NFT_CRITICALHANDLE: rettype ((FieldMarshaler_CriticalHandle*)this)->name##Impl args; break; \
        case NFT_ILLEGAL: rettype ((FieldMarshaler_Illegal*)this)->name##Impl args; break; \
        default: UNREACHABLE_MSG("unexpected type of FieldMarshaler"); break; \
        } \
    }

#endif // FEATURE_COMINTEROP


IMPLEMENT_FieldMarshaler_METHOD(UINT32, NativeSize,
    () const,
    return,
    ())

IMPLEMENT_FieldMarshaler_METHOD(UINT32, AlignmentRequirement,
    () const,
    return,
    ())

IMPLEMENT_FieldMarshaler_METHOD(BOOL, IsScalarMarshaler,
    () const,
    return,
    ())

IMPLEMENT_FieldMarshaler_METHOD(BOOL, IsNestedValueClassMarshaler,
    () const,
    return,
    ())

#ifndef CROSSGEN_COMPILE
IMPLEMENT_FieldMarshaler_METHOD(VOID, UpdateNative,
    (OBJECTREF* pCLRValue, LPVOID pNativeValue, OBJECTREF *ppCleanupWorkListOnStack) const,
    ,
    (pCLRValue, pNativeValue, ppCleanupWorkListOnStack))

IMPLEMENT_FieldMarshaler_METHOD(VOID, UpdateCLR,
    (const VOID *pNativeValue, OBJECTREF *ppProtectedCLRValue, OBJECTREF *ppProtectedOldCLRValue) const,
    ,
    (pNativeValue, ppProtectedCLRValue, ppProtectedOldCLRValue))

IMPLEMENT_FieldMarshaler_METHOD(VOID, DestroyNative,
    (LPVOID pNativeValue) const,
    ,
    (pNativeValue))

IMPLEMENT_FieldMarshaler_METHOD(VOID, ScalarUpdateNative,
    (LPVOID pCLR, LPVOID pNative) const,
    return,
    (pCLR, pNative))

IMPLEMENT_FieldMarshaler_METHOD(VOID, ScalarUpdateCLR,
    (const VOID *pNative, LPVOID pCLR) const,
    return,
    (pNative, pCLR))

IMPLEMENT_FieldMarshaler_METHOD(VOID, NestedValueClassUpdateNative,
    (const VOID **ppProtectedCLR, SIZE_T startoffset, LPVOID pNative, OBJECTREF *ppCleanupWorkListOnStack) const,
    ,
    (ppProtectedCLR, startoffset, pNative, ppCleanupWorkListOnStack))

IMPLEMENT_FieldMarshaler_METHOD(VOID, NestedValueClassUpdateCLR,
    (const VOID *pNative, LPVOID *ppProtectedCLR, SIZE_T startoffset) const,
    ,
    (pNative, ppProtectedCLR, startoffset))
#endif // CROSSGEN_COMPILE

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
IMPLEMENT_FieldMarshaler_METHOD(void, Save,
    (DataImage *image),
    ,
    (image))

IMPLEMENT_FieldMarshaler_METHOD(void, Fixup,
    (DataImage *image),
    ,
    (image))
#endif // FEATURE_NATIVE_IMAGE_GENERATION

IMPLEMENT_FieldMarshaler_METHOD(void, Restore,
    (),
    ,
    ())
