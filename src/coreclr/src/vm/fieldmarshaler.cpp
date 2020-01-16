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

VOID ParseNativeType(Module*                     pModule,
                     SigPointer                  sig,
                     mdFieldDef                  fd,
                     ParseNativeTypeFlags        flags,
                     NativeFieldDescriptor*      pNFD,
                     const SigTypeContext *      pTypeContext
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
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pNFD));
    }
    CONTRACTL_END;

    BOOL fAnsi = (flags == ParseNativeTypeFlags::IsAnsi);
#ifdef FEATURE_COMINTEROP
    BOOL fIsWinRT = (flags == ParseNativeTypeFlags::IsWinRT);
#endif // FEATURE_COMINTEROP

    MarshalInfo mlInfo(
        pModule,
        sig,
        pTypeContext,
        fd,
#if FEATURE_COMINTEROP
        fIsWinRT ? MarshalInfo::MARSHAL_SCENARIO_WINRT_FIELD : MarshalInfo::MARSHAL_SCENARIO_FIELD,
#else
        MarshalInfo::MARSHAL_SCENARIO_FIELD,
#endif
        fAnsi ? nltAnsi : nltUnicode,
        nlfNone,
        FALSE,
        0,
        0,
        FALSE, // We only need validation of the native signature and the MARSHAL_TYPE_*
        FALSE, // so we don't need to accurately get the BestFitCustomAttribute data for this construction.
        FALSE, /* fEmitsIL */
        FALSE, /* onInstanceMethod */
        nullptr,
        FALSE /* fUseCustomMarshal */
#ifdef _DEBUG
        ,
        szFieldName,
        szClassName,
        -1 /* field */
#endif
    );

    OverrideProcArgs const* const pargs = mlInfo.GetOverrideProcArgs();

    switch (mlInfo.GetMarshalType())
    {
        case MarshalInfo::MARSHAL_TYPE_GENERIC_1:
        case MarshalInfo::MARSHAL_TYPE_GENERIC_U1:
            *pNFD = NativeFieldDescriptor(NativeFieldCategory::INTEGER, sizeof(INT8), sizeof(INT8));
            break;
        case MarshalInfo::MARSHAL_TYPE_GENERIC_2:
        case MarshalInfo::MARSHAL_TYPE_GENERIC_U2:
            *pNFD = NativeFieldDescriptor(NativeFieldCategory::INTEGER, sizeof(INT16), sizeof(INT16));
            break;
        case MarshalInfo::MARSHAL_TYPE_GENERIC_4:
        case MarshalInfo::MARSHAL_TYPE_GENERIC_U4:
            *pNFD = NativeFieldDescriptor(NativeFieldCategory::INTEGER, sizeof(INT32), sizeof(INT32));
            break;
        case MarshalInfo::MARSHAL_TYPE_GENERIC_8:
#if defined(_TARGET_X86_) && defined(UNIX_X86_ABI)
            *pNFD = NativeFieldDescriptor(NativeFieldCategory::INTEGER, sizeof(INT64), 4);
#else
            *pNFD = NativeFieldDescriptor(NativeFieldCategory::INTEGER, sizeof(INT64), sizeof(INT64));
#endif
            break;
        case MarshalInfo::MARSHAL_TYPE_ANSICHAR:
            *pNFD = NativeFieldDescriptor(NativeFieldCategory::INTEGER, sizeof(CHAR), sizeof(CHAR));
            break;
        case MarshalInfo::MARSHAL_TYPE_WINBOOL:
            *pNFD = NativeFieldDescriptor(NativeFieldCategory::INTEGER, sizeof(BOOL), sizeof(BOOL));
            break;
        case MarshalInfo::MARSHAL_TYPE_CBOOL:
            *pNFD = NativeFieldDescriptor(NativeFieldCategory::INTEGER, sizeof(bool), sizeof(bool));
            break;
#ifdef FEATURE_COMINTEROP
        case MarshalInfo::MARSHAL_TYPE_VTBOOL:
            *pNFD = NativeFieldDescriptor(NativeFieldCategory::INTEGER, sizeof(VARIANT_BOOL), sizeof(VARIANT_BOOL));
            break;
#endif
        case MarshalInfo::MARSHAL_TYPE_FLOAT:
            *pNFD = NativeFieldDescriptor(NativeFieldCategory::FLOAT, sizeof(float), sizeof(float));
            break;
        case MarshalInfo::MARSHAL_TYPE_DOUBLE:
#if defined(_TARGET_X86_) && defined(UNIX_X86_ABI)
            *pNFD = NativeFieldDescriptor(NativeFieldCategory::FLOAT, sizeof(double), 4);
#else
            *pNFD = NativeFieldDescriptor(NativeFieldCategory::FLOAT, sizeof(double), sizeof(double));
#endif
            break;
        case MarshalInfo::MARSHAL_TYPE_CURRENCY:
            *pNFD = NativeFieldDescriptor(MscorlibBinder::GetClass(CLASS__CURRENCY));
            break;
        case MarshalInfo::MARSHAL_TYPE_DECIMAL:
            // The decimal type can't be blittable since the managed and native alignment requirements differ.
            // Native needs 8-byte alignment since one field is a 64-bit integer, but managed only needs 4-byte alignment since all fields are ints.
            *pNFD = NativeFieldDescriptor(MscorlibBinder::GetClass(CLASS__NATIVEDECIMAL));
            break;
        case MarshalInfo::MARSHAL_TYPE_GUID:
            *pNFD = NativeFieldDescriptor(MscorlibBinder::GetClass(CLASS__GUID));
            break;
        case MarshalInfo::MARSHAL_TYPE_DATE:
            *pNFD = NativeFieldDescriptor(NativeFieldCategory::FLOAT, sizeof(double), sizeof(double));
            break;
        case MarshalInfo::MARSHAL_TYPE_LPWSTR:
        case MarshalInfo::MARSHAL_TYPE_LPSTR:
        case MarshalInfo::MARSHAL_TYPE_LPUTF8STR:
        case MarshalInfo::MARSHAL_TYPE_BSTR:
        case MarshalInfo::MARSHAL_TYPE_ANSIBSTR:
            *pNFD = NativeFieldDescriptor(NativeFieldCategory::INTEGER, sizeof(void*), sizeof(void*));
            break;      
#ifdef FEATURE_COMINTEROP
        case MarshalInfo::MARSHAL_TYPE_HSTRING:
            *pNFD = NativeFieldDescriptor(NativeFieldCategory::INTEGER, sizeof(HSTRING), sizeof(HSTRING));
            break;
        case MarshalInfo::MARSHAL_TYPE_DATETIME:
            *pNFD = NativeFieldDescriptor(NativeFieldCategory::INTEGER, sizeof(INT64), sizeof(INT64));
            break;
        case MarshalInfo::MARSHAL_TYPE_INTERFACE:
            *pNFD = NativeFieldDescriptor(NativeFieldCategory::INTEGER, sizeof(IUnknown*), sizeof(IUnknown*));
            break;
        case MarshalInfo::MARSHAL_TYPE_SAFEARRAY:
            *pNFD = NativeFieldDescriptor(NativeFieldCategory::INTEGER, sizeof(SAFEARRAY*), sizeof(SAFEARRAY*));
            break;
#endif
        case MarshalInfo::MARSHAL_TYPE_DELEGATE:
            *pNFD = NativeFieldDescriptor(NativeFieldCategory::INTEGER, sizeof(void*), sizeof(void*));
            break;
        case MarshalInfo::MARSHAL_TYPE_BLITTABLEVALUECLASS:
        case MarshalInfo::MARSHAL_TYPE_VALUECLASS:
        case MarshalInfo::MARSHAL_TYPE_LAYOUTCLASS:
        case MarshalInfo::MARSHAL_TYPE_BLITTABLE_LAYOUTCLASS:
            *pNFD = NativeFieldDescriptor(pargs->m_pMT);
            break;
#ifdef FEATURE_COMINTEROP
        case MarshalInfo::MARSHAL_TYPE_OBJECT:
            *pNFD = NativeFieldDescriptor(MscorlibBinder::GetClass(CLASS__NATIVEVARIANT));
            break;
#endif
        case MarshalInfo::MARSHAL_TYPE_SAFEHANDLE:
        case MarshalInfo::MARSHAL_TYPE_CRITICALHANDLE:
            *pNFD = NativeFieldDescriptor(NativeFieldCategory::INTEGER, sizeof(void*), sizeof(void*));
            break;
        case MarshalInfo::MARSHAL_TYPE_FIXED_ARRAY:
        {
            CREATE_MARSHALER_CARRAY_OPERANDS mops;
            mlInfo.GetMops(&mops);

            MethodTable *pMT = mops.methodTable;

            if (pMT->IsEnum())
            {
                pMT = MscorlibBinder::GetElementType(pMT->GetInternalCorElementType());
            }

            *pNFD = NativeFieldDescriptor(OleVariant::GetNativeMethodTableForVarType(mops.elementType, pMT), mops.additive);
            break;
        }
        case MarshalInfo::MARSHAL_TYPE_FIXED_CSTR:
            *pNFD = NativeFieldDescriptor(MscorlibBinder::GetClass(CLASS__BYTE), pargs->fs.fixedStringLength);
            break;
        case MarshalInfo::MARSHAL_TYPE_FIXED_WSTR:
            *pNFD = NativeFieldDescriptor(MscorlibBinder::GetClass(CLASS__UINT16), pargs->fs.fixedStringLength);
            break;
#ifdef FEATURE_COMINTEROP
        case MarshalInfo::MARSHAL_TYPE_SYSTEMTYPE:
            *pNFD = NativeFieldDescriptor(MscorlibBinder::GetClass(CLASS__TYPENAMENATIVE));
            break;
        case MarshalInfo::MARSHAL_TYPE_EXCEPTION:
            *pNFD = NativeFieldDescriptor(NativeFieldCategory::INTEGER, sizeof(int), sizeof(int));
            break;
        case MarshalInfo::MARSHAL_TYPE_NULLABLE:
            *pNFD = NativeFieldDescriptor(NativeFieldCategory::INTEGER, sizeof(IUnknown*), sizeof(IUnknown*));
            break;
#endif
        case MarshalInfo::MARSHAL_TYPE_UNKNOWN:
        default:
            *pNFD = NativeFieldDescriptor();
            break;
    }
}

bool IsFieldBlittable(
    Module* pModule,
    mdFieldDef fd,
    SigPointer fieldSig,
    const SigTypeContext* pTypeContext,
    ParseNativeTypeFlags flags
)
{
    PCCOR_SIGNATURE marshalInfoSig;
    ULONG marshalInfoSigLength;

    CorNativeType nativeType = NATIVE_TYPE_DEFAULT;

    if (pModule->GetMDImport()->GetFieldMarshal(fd, &marshalInfoSig, &marshalInfoSigLength) == S_OK && marshalInfoSigLength > 0)
    {
        nativeType = (CorNativeType)*marshalInfoSig;
    }

    if (nativeType != NATIVE_TYPE_DEFAULT && flags == ParseNativeTypeFlags::IsWinRT)
    {
        return false;
    }

    bool isBlittable = false;

    EX_TRY
    {
        TypeHandle valueTypeHandle;
        CorElementType corElemType = fieldSig.PeekElemTypeNormalized(pModule, pTypeContext, &valueTypeHandle);

        switch (corElemType)
        {
        case ELEMENT_TYPE_CHAR:
            isBlittable = (nativeType == NATIVE_TYPE_DEFAULT && flags != ParseNativeTypeFlags::IsAnsi) || (nativeType == NATIVE_TYPE_I2) || (nativeType == NATIVE_TYPE_U2);
            break;
        case ELEMENT_TYPE_I1:
        case ELEMENT_TYPE_U1:
            isBlittable = (nativeType == NATIVE_TYPE_DEFAULT) || (nativeType == NATIVE_TYPE_I1) || (nativeType == NATIVE_TYPE_U1);
            break;
        case ELEMENT_TYPE_I2:
        case ELEMENT_TYPE_U2:
            isBlittable = (nativeType == NATIVE_TYPE_DEFAULT) || (nativeType == NATIVE_TYPE_I2) || (nativeType == NATIVE_TYPE_U2);
            break;
        case ELEMENT_TYPE_I4:
        case ELEMENT_TYPE_U4:
            isBlittable = (nativeType == NATIVE_TYPE_DEFAULT) || (nativeType == NATIVE_TYPE_I4) || (nativeType == NATIVE_TYPE_U4) || (nativeType == NATIVE_TYPE_ERROR);
            break;
        case ELEMENT_TYPE_I8:
        case ELEMENT_TYPE_U8:
            isBlittable = (nativeType == NATIVE_TYPE_DEFAULT) || (nativeType == NATIVE_TYPE_I8) || (nativeType == NATIVE_TYPE_U8);
            break;
        case ELEMENT_TYPE_R4:
            isBlittable = (nativeType == NATIVE_TYPE_DEFAULT) || (nativeType == NATIVE_TYPE_R4);
        case ELEMENT_TYPE_R8:
            isBlittable = (nativeType == NATIVE_TYPE_DEFAULT) || (nativeType == NATIVE_TYPE_R8);
            break;
        case ELEMENT_TYPE_I:
        case ELEMENT_TYPE_U:
            isBlittable = (nativeType == NATIVE_TYPE_DEFAULT) || (nativeType == NATIVE_TYPE_INT) || (nativeType == NATIVE_TYPE_UINT);
            break;
        case ELEMENT_TYPE_PTR:
            isBlittable = nativeType == NATIVE_TYPE_DEFAULT && flags != ParseNativeTypeFlags::IsWinRT;
            break;
        case ELEMENT_TYPE_FNPTR:
            isBlittable = flags != ParseNativeTypeFlags::IsWinRT && (nativeType == NATIVE_TYPE_DEFAULT || nativeType == NATIVE_TYPE_FUNC);
            break;
        case ELEMENT_TYPE_VALUETYPE:
            if (nativeType != NATIVE_TYPE_DEFAULT && nativeType != NATIVE_TYPE_STRUCT)
            {
                isBlittable = false;
            }
            else if (valueTypeHandle.GetMethodTable() == MscorlibBinder::GetClass(CLASS__DECIMAL))
            {
                // The alignment requirements of the managed System.Decimal type do not match the native DECIMAL type.
                // As a result, a field of type System.Decimal can't be blittable.
                isBlittable = false;
            }
#if FEATURE_COMINTEROP
            else if (flags == ParseNativeTypeFlags::IsWinRT)
            {
                if (valueTypeHandle.GetMethodTable()->HasSameTypeDefAs(g_pNullableClass))
                {
                    isBlittable = false;
                }
                else if (valueTypeHandle.GetMethodTable()->HasSameTypeDefAs(MscorlibBinder::GetClass(CLASS__KEYVALUEPAIRGENERIC)))
                {
                    isBlittable = false;
                }
            }
#endif
            else
            {
                isBlittable = !valueTypeHandle.GetMethodTable()->HasInstantiation() && valueTypeHandle.GetMethodTable()->IsBlittable();
            }
            break;
        default:
            isBlittable = false;
            break;
        }
    }
    EX_CATCH
    {
        // We were unable to determine the native type, likely because there is a mutually recursive type reference
        // in this field's type. A mutually recursive object would never be blittable, so we don't need to do anything.
    }
    EX_END_CATCH(RethrowTerminalExceptions);
    return isBlittable;
}

//=======================================================================
// This function returns TRUE if the type passed in is either a value class or a class and if it has layout information
// and is marshalable. In all other cases it will return FALSE.
//=======================================================================
BOOL IsStructMarshalable(TypeHandle th)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
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

    return pMT->GetNativeLayoutInfo()->IsMarshalable() ? TRUE : FALSE;
}

NativeFieldDescriptor::NativeFieldDescriptor()
    :m_offset(0),
    m_category(NativeFieldCategory::ILLEGAL),
    m_isNestedType(false)
{
    nativeSizeAndAlignment.m_nativeSize = 1;
    nativeSizeAndAlignment.m_alignmentRequirement = 1;
    m_pFD.SetValueMaybeNull(nullptr);
}

NativeFieldDescriptor::NativeFieldDescriptor(NativeFieldCategory flags, ULONG nativeSize, ULONG alignment)
    :m_offset(0),
    m_category(flags),
    m_isNestedType(false)
{
    nativeSizeAndAlignment.m_nativeSize = nativeSize;
    nativeSizeAndAlignment.m_alignmentRequirement = alignment;
    m_pFD.SetValueMaybeNull(nullptr);
}

NativeFieldDescriptor::NativeFieldDescriptor(PTR_MethodTable pMT, int numElements)
    :m_isNestedType(true),
    m_category(NativeFieldCategory::NESTED)
{
    CONTRACTL
    {
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(pMT->HasLayout());
    }
    CONTRACTL_END;

    m_pFD.SetValueMaybeNull(nullptr);
    nestedTypeAndCount.m_pNestedType.SetValue(pMT);
    nestedTypeAndCount.m_numElements = numElements;
}

NativeFieldDescriptor::NativeFieldDescriptor(const NativeFieldDescriptor& other)
    :m_offset(other.m_offset),
    m_category(other.m_category),
    m_isNestedType(other.m_isNestedType)
{
    m_pFD.SetValueMaybeNull(other.m_pFD.GetValueMaybeNull());
    if (m_isNestedType)
    {
        nestedTypeAndCount.m_pNestedType.SetValueMaybeNull(other.nestedTypeAndCount.m_pNestedType.GetValueMaybeNull());
        nestedTypeAndCount.m_numElements = other.nestedTypeAndCount.m_numElements;
    }
    else
    {
        nativeSizeAndAlignment.m_nativeSize = other.nativeSizeAndAlignment.m_nativeSize;
        nativeSizeAndAlignment.m_alignmentRequirement = other.nativeSizeAndAlignment.m_alignmentRequirement;
    }
}

NativeFieldDescriptor& NativeFieldDescriptor::operator=(const NativeFieldDescriptor& other)
{
    m_offset = other.m_offset;
    m_category = other.m_category;
    m_isNestedType = other.m_isNestedType;
    m_pFD.SetValueMaybeNull(other.m_pFD.GetValueMaybeNull());

    if (m_isNestedType)
    {
        nestedTypeAndCount.m_pNestedType.SetValueMaybeNull(other.nestedTypeAndCount.m_pNestedType.GetValueMaybeNull());
        nestedTypeAndCount.m_numElements = other.nestedTypeAndCount.m_numElements;
    }
    else
    {
        nativeSizeAndAlignment.m_nativeSize = other.nativeSizeAndAlignment.m_nativeSize;
        nativeSizeAndAlignment.m_alignmentRequirement = other.nativeSizeAndAlignment.m_alignmentRequirement;
    }

    return *this;
}

UINT32 NativeFieldDescriptor::AlignmentRequirement() const
{
    if (m_isNestedType)
    {
        MethodTable* pMT = GetNestedNativeMethodTable();
        if (pMT->IsBlittable())
        {
            return pMT->GetLayoutInfo()->m_ManagedLargestAlignmentRequirementOfAllMembers;
        }
        return pMT->GetNativeLayoutInfo()->GetLargestAlignmentRequirement();
    }
    else
    {
        return nativeSizeAndAlignment.m_alignmentRequirement;
    }
}

PTR_MethodTable NativeFieldDescriptor::GetNestedNativeMethodTable() const
{
    CONTRACT(PTR_MethodTable)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(IsRestored());
        PRECONDITION(m_isNestedType);
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    RETURN nestedTypeAndCount.m_pNestedType.GetValue();
}

PTR_FieldDesc NativeFieldDescriptor::GetFieldDesc() const
{
    CONTRACT(PTR_FieldDesc)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    RETURN m_pFD.GetValueMaybeNull();
}

#ifdef _DEBUG
BOOL NativeFieldDescriptor::IsRestored() const
{
    WRAPPER_NO_CONTRACT;

#ifdef FEATURE_PREJIT
    return nestedTypeAndCount.m_pNestedType.IsNull() || (!nestedTypeAndCount.m_pNestedType.IsTagged() && nestedTypeAndCount.m_pNestedType.GetValue()->IsRestored());
#else // FEATURE_PREJIT
    // putting the IsFullyLoaded check here is tempting but incorrect
    return TRUE;
#endif // FEATURE_PREJIT
}
#endif

void NativeFieldDescriptor::Restore()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(!m_isNestedType || CheckPointer(nestedTypeAndCount.m_pNestedType.GetValue()));
    }
    CONTRACTL_END;


#ifdef FEATURE_PREJIT
    Module::RestoreFieldDescPointer(&m_pFD);
#endif // FEATURE_PREJIT

    if (m_isNestedType)
    {

#ifdef FEATURE_PREJIT
        Module::RestoreMethodTablePointer(&nestedTypeAndCount.m_pNestedType);
#else // FEATURE_PREJIT
        // without NGEN we only have to make sure that the type is fully loaded
        PTR_MethodTable pMT = nestedTypeAndCount.m_pNestedType.GetValue();
        if (pMT != NULL)
            ClassLoader::EnsureLoaded(pMT);
#endif // FEATURE_PREJIT
    }
}

void NativeFieldDescriptor::SetFieldDesc(PTR_FieldDesc pFD)
{
    LIMITED_METHOD_CONTRACT;
    m_pFD.SetValueMaybeNull(pFD);
}
