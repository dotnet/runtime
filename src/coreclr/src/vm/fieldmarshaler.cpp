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
#ifdef _DEBUG
BOOL IsFixedBuffer(mdFieldDef field, IMDInternalImport *pInternalImport);
#endif


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

    BOOL                fAnsi               = (flags == ParseNativeTypeFlags::IsAnsi);
#ifdef FEATURE_COMINTEROP
    BOOL                fIsWinRT            = (flags == ParseNativeTypeFlags::IsWinRT);
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
            pfwalk->m_managedPlacement.m_size = ((UINT32)CorTypeInfo::Size(corElemType)); // Safe cast - no primitive type is larger than 4gb!
#if defined(_TARGET_X86_) && defined(UNIX_X86_ABI)
            switch (corElemType)
            {
                // The System V ABI for i386 defines different packing for these types.
                case ELEMENT_TYPE_I8:
                case ELEMENT_TYPE_U8:
                case ELEMENT_TYPE_R8:
                {
                    pfwalk->m_managedPlacement.m_alignment = 4;
                    break;
                }

                default:
                {
                    pfwalk->m_managedPlacement.m_alignment = pfwalk->m_managedPlacement.m_size;
                    break;
                }
            }
#else // _TARGET_X86_ && UNIX_X86_ABI
            pfwalk->m_managedPlacement.m_alignment = pfwalk->m_managedPlacement.m_size;
#endif
        }
        else if (corElemType == ELEMENT_TYPE_PTR)
        {
            pfwalk->m_managedPlacement.m_size = TARGET_POINTER_SIZE;
            pfwalk->m_managedPlacement.m_alignment = TARGET_POINTER_SIZE;
        }
        else if (corElemType == ELEMENT_TYPE_VALUETYPE)
        {
            TypeHandle pNestedType = fsig.GetLastTypeHandleThrowing(ClassLoader::LoadTypes,
                                                                    CLASS_LOAD_APPROXPARENTS,
                                                                    TRUE);
            if (pNestedType.GetMethodTable()->IsManagedSequential())
            {
                pfwalk->m_managedPlacement.m_size = (pNestedType.GetMethodTable()->GetNumInstanceFieldBytes());

                _ASSERTE(pNestedType.GetMethodTable()->HasLayout()); // If it is ManagedSequential(), it also has Layout but doesn't hurt to check before we do a cast!
                pfwalk->m_managedPlacement.m_alignment = pNestedType.GetMethodTable()->GetLayoutInfo()->m_ManagedLargestAlignmentRequirementOfAllMembers;
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
                    ReadBestFitCustomAttribute(pModule, cl, &BestFit, &ThrowOnUnmappableChar);
                    INITFIELDMARSHALER(NFT_ANSICHAR, FieldMarshaler_Ansi, (BestFit, ThrowOnUnmappableChar));
                }
                else
                {
                    INITFIELDMARSHALER(NFT_COPY2, FieldMarshaler_Copy2, ());
                }
            }
            else if (ntype == NATIVE_TYPE_I1 || ntype == NATIVE_TYPE_U1)
            {
                ReadBestFitCustomAttribute(pModule, cl, &BestFit, &ThrowOnUnmappableChar);
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
#ifdef _TARGET_64BIT_
                INITFIELDMARSHALER(NFT_COPY8, FieldMarshaler_Copy8, ());
#else // !_TARGET_64BIT_
                INITFIELDMARSHALER(NFT_COPY4, FieldMarshaler_Copy4, ());
#endif // !_TARGET_64BIT_
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
#ifdef _TARGET_64BIT_
                INITFIELDMARSHALER(NFT_COPY8, FieldMarshaler_Copy8, ());
#else // !_TARGET_64BIT_
                INITFIELDMARSHALER(NFT_COPY4, FieldMarshaler_Copy4, ());
#endif // !_TARGET_64BIT_
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
#ifdef _DEBUG
                        INITFIELDMARSHALER(NFT_NESTEDVALUECLASS, FieldMarshaler_NestedValueClass, (thNestedType.GetMethodTable(), IsFixedBuffer(pfwalk->m_MD, pModule->GetMDImport())));
#else
                        INITFIELDMARSHALER(NFT_NESTEDVALUECLASS, FieldMarshaler_NestedValueClass, (thNestedType.GetMethodTable()));
#endif
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
                else if (COMDelegate::IsDelegate(thNestedType.GetMethodTable()))
                {
                    INITFIELDMARSHALER(NFT_ILLEGAL, FieldMarshaler_Illegal, (IDS_EE_BADMARSHAL_DELEGATE_TLB_INTERFACE));
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
                        ReadBestFitCustomAttribute(pModule, cl, &BestFit, &ThrowOnUnmappableChar);
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
                            ReadBestFitCustomAttribute(pModule, cl, &BestFit, &ThrowOnUnmappableChar);
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

                        case NATIVE_TYPE_BSTR:
                            INITFIELDMARSHALER(NFT_BSTR, FieldMarshaler_BSTR, ());
                            break;

#ifdef FEATURE_COMINTEROP
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
                                    ReadBestFitCustomAttribute(pModule, cl, &BestFit, &ThrowOnUnmappableChar);
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
                    INITFIELDMARSHALER(NFT_FIXEDARRAY, FieldMarshaler_FixedArray, (pModule, cl, numElements, VT_BSTR, g_pStringClass));
                }
            }
#endif // FEATURE_CLASSIC_COMINTEROP
            else if (COMDelegate::IsDelegate(thNestedType.GetMethodTable()))
            {
                if (fDefault || ntype == NATIVE_TYPE_FUNC)
                {
                    INITFIELDMARSHALER(NFT_DELEGATE, FieldMarshaler_Delegate, (thNestedType.GetMethodTable()));
                }
#ifdef FEATURE_COMINTEROP
                else if (ntype == NATIVE_TYPE_IDISPATCH)
                {
                    ItfMarshalInfo itfInfo;
                    if (FAILED(MarshalInfo::TryGetItfMarshalInfo(thNestedType, FALSE, FALSE, &itfInfo)))
                        break;

                    INITFIELDMARSHALER(NFT_INTERFACE, FieldMarshaler_Interface, (itfInfo.thClass.GetMethodTable(), itfInfo.thItf.GetMethodTable(), itfInfo.dwFlags));
                }
#endif // FEATURE_COMINTEROP
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
                    ReadBestFitCustomAttribute(pModule, cl, &BestFit, &ThrowOnUnmappableChar);
                    INITFIELDMARSHALER(NFT_FIXEDCHARARRAYANSI, FieldMarshaler_FixedCharArrayAnsi, (numElements, BestFit, ThrowOnUnmappableChar));
                    break;                    
                }
                else
                {
                    VARTYPE elementVT = arrayMarshalInfo.GetElementVT();

                    INITFIELDMARSHALER(NFT_FIXEDARRAY, FieldMarshaler_FixedArray, (pModule, cl, numElements, elementVT, arrayMarshalInfo.GetElementTypeHandle().GetMethodTable()));
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

BOOL IsFieldBlittable(FieldMarshaler* pFM)
{
    // if it's a simple copy...
    if (pFM->GetNStructFieldType() == NFT_COPY1    ||
        pFM->GetNStructFieldType() == NFT_COPY2    ||
        pFM->GetNStructFieldType() == NFT_COPY4    ||
        pFM->GetNStructFieldType() == NFT_COPY8)
    {
        return TRUE;
    }

    // Or if it's a nested value class that is itself blittable...
    if (pFM->GetNStructFieldType() == NFT_NESTEDVALUECLASS)
    {
        _ASSERTE(pFM->IsNestedValueClassMarshaler());

        if (((FieldMarshaler_NestedValueClass *) pFM)->IsBlittable())
            return TRUE;
    }

    return FALSE;
}

#ifdef _DEBUG
BOOL IsFixedBuffer(mdFieldDef field, IMDInternalImport *pInternalImport)
{
    HRESULT hr = pInternalImport->GetCustomAttributeByName(field, g_FixedBufferAttribute, NULL, NULL);

    return hr == S_OK ? TRUE : FALSE;
}
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
                SetObjectReference( (OBJECTREF*) (internalOffset + offsetbias + (BYTE*)(*ppProtectedManagedData)), pCLRValue);
            }

            // The cleanup work list is not used to clean up the native contents. It is used
            // to handle cleanup of any additional resources the FieldMarshalers allocate.

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

    FieldMarshaler* pFM = pMT->GetLayoutInfo()->GetFieldMarshalers();
    UINT  numReferenceFields = pMT->GetLayoutInfo()->GetNumCTMFields();

    struct _gc
    {
        OBJECTREF pCLRValue;
        OBJECTREF pOldCLRValue;
    } gc;

    gc.pCLRValue = NULL;
    gc.pOldCLRValue = NULL;
    LPVOID scalar = NULL;

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
                SetObjectReference( (OBJECTREF*) (internalOffset + offsetbias + (BYTE*)(*ppProtectedManagedData)), gc.pCLRValue );
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
        // BSTR has been created, Delay load will not fail.
        CONTRACT_VIOLATION(ThrowsViolation);
        SysFreeString(pBSTR);
    }
}

#ifdef FEATURE_COMINTEROP
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

    MethodTable* pMT = GetMethodTable();

    // would be better to detect this at class load time (that have a nested value
    // class with no layout) but don't have a way to know
    if (!pMT->GetLayoutInfo())
        COMPlusThrow(kArgumentException, IDS_NOLAYOUT_IN_EMBEDDED_VALUECLASS);

    if (pMT->IsBlittable())
    {
        memcpyNoGCRefs(pNative, (BYTE*)(*ppProtectedCLR) + startoffset, pMT->GetNativeSize());
    }
    else
    {
#ifdef _DEBUG
        _ASSERTE_MSG(!IsFixedBuffer(), "Cannot correctly marshal fixed buffers of non-blittable types");
#endif
        LayoutUpdateNative((LPVOID*)ppProtectedCLR, startoffset, pMT, (BYTE*)pNative, ppCleanupWorkListOnStack);
    }
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

    MethodTable* pMT = GetMethodTable();

    // would be better to detect this at class load time (that have a nested value
    // class with no layout) but don't have a way to know
    if (!pMT->GetLayoutInfo())
        COMPlusThrow(kArgumentException, IDS_NOLAYOUT_IN_EMBEDDED_VALUECLASS);

    if (pMT->IsBlittable())
    {
        memcpyNoGCRefs((BYTE*)(*ppProtectedCLR) + startoffset, pNative, pMT->GetNativeSize());
    }
    else
    {
#ifdef _DEBUG
        _ASSERTE_MSG(!IsFixedBuffer(), "Cannot correctly marshal fixed buffers of non-blittable types");
#endif
        LayoutUpdateCLR((LPVOID*)ppProtectedCLR,
            startoffset,
            pMT,
            (BYTE *)pNative);
    }
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

    MethodTable* pMT = GetMethodTable();

    if (!pMT->IsBlittable())
    {
        LayoutDestroyNative(pNativeValue, pMT);
    }
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
    if (sz)
    {
        MethodDescCallSite convertToManaged(METHOD__CUTF8MARSHALER__CONVERT_TO_MANAGED);
        ARG_SLOT args[] =
        {
            PtrToArgSlot(sz),
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
FieldMarshaler_FixedArray::FieldMarshaler_FixedArray(Module *pModule, mdTypeDef cl, UINT32 numElems, VARTYPE vt, MethodTable* pElementMT)
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
        ReadBestFitCustomAttribute(pModule, cl, &BestFitMap, &ThrowOnUnmappableChar);      
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
    *ppProtectedCLRValue = AllocateSzArray(m_arrayType.GetValue(), (INT32)m_numElems);

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
    MethodTable* pMT = m_pMT.GetValueMaybeNull();

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
    MethodTable* pMT = m_pMT.GetValueMaybeNull();

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
    
    // If there is no CleanupWorkList (i.e. a call from Marshal.StructureToPtr), we don't use it to manage delegate lifetime.
    // In that case, it falls on the user to manage the delegate lifetime. This is the cleanest way to do this since there is no well-defined
    // object lifetime for the unmanaged memory that the structure would be marshalled to in the Marshal.StructureToPtr case.
    if (*pCLRValue != NULL && ppCleanupWorkListOnStack != NULL)
    {
        // Call StubHelpers.AddToCleanupList to ensure the delegate is kept alive across the full native call.
        MethodDescCallSite AddToCleanupList(METHOD__STUBHELPERS__ADD_TO_CLEANUP_LIST_DELEGATE);

        ARG_SLOT args[] =
        {
            (ARG_SLOT)ppCleanupWorkListOnStack,
            ObjToArgSlot(*pCLRValue)
        };

        AddToCleanupList.Call(args);
    }

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
    MethodDescCallSite AddToCleanupList(METHOD__STUBHELPERS__ADD_TO_CLEANUP_LIST_SAFEHANDLE);

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
    VarDecFromCyCanonicalize( *(CURRENCY*)pNative, (DECIMAL *)pCLR );
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

#define FieldMarshaler_VTable(name, rettype, args) \
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
        } 

#else // FEATURE_COMINTEROP


#define FieldMarshaler_VTable(name, rettype, args) \
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
        case NFT_BSTR: rettype ((FieldMarshaler_BSTR*)this)->name##Impl args; break; \
        case NFT_ILLEGAL: rettype ((FieldMarshaler_Illegal*)this)->name##Impl args; break; \
        default: UNREACHABLE_MSG("unexpected type of FieldMarshaler"); break; \
        }

#endif // FEATURE_COMINTEROP


#define IMPLEMENT_FieldMarshaler_METHOD(ret, name, argsdecl, rettype, args) \
    ret FieldMarshaler::name argsdecl { \
        WRAPPER_NO_CONTRACT; \
        FieldMarshaler_VTable(name, rettype, args) \
    }

UINT32 FieldMarshaler::NativeSize() const
{
    WRAPPER_NO_CONTRACT;
    // Use the NFTDataBase to lookup the native size quickly to avoid a vtable call when the result is already known.
    if (NFTDataBase[m_nft].m_cbNativeSize != 0)
    {
        return NFTDataBase[m_nft].m_cbNativeSize;
    }
    else
    {
        FieldMarshaler_VTable(NativeSize, return, ())
    }
}

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

#ifndef DACCESS_COMPILE
IMPLEMENT_FieldMarshaler_METHOD(VOID, CopyTo,
    (VOID *pDest, SIZE_T destSize) const,
    ,
    (pDest, destSize))
#endif // !DACCESS_COMPILE
