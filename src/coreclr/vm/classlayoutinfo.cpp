// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "class.h"
#include "fieldmarshaler.h"
#include "enum_class_flags.h"

#ifndef DACCESS_COMPILE

struct LayoutRawFieldInfo final
{
    mdFieldDef  m_token;             // mdMemberDefNil for end of array
    RawFieldPlacementInfo m_placement;
    NativeFieldDescriptor m_nfd;
};

namespace
{
    bool TryGetParentLayoutInfo(MethodTable* pParentMT, UINT32* pSize, BYTE* pAlignment)
    {
        if (!pParentMT || !pParentMT->HasLayout())
            return false;

        EEClassLayoutInfo* pLayoutInfo = pParentMT->GetLayoutInfo();
        // Treat base class as an initial member.
        // If the parent was originally a zero-sized explicit type but
        // got bumped up to a size of 1 for compatibility reasons, then
        // we need to remove the padding, but ONLY for inheritance situations.
        UINT32 size;
        if (pLayoutInfo->IsZeroSized()) {
            size = 0;
        }
        else
        {
            size = pParentMT->GetNumInstanceFieldBytes();
        }
        *pSize = size;

        if (pParentMT->IsManagedSequential() || (pParentMT->GetClass()->HasExplicitFieldOffsetLayout() && pParentMT->IsBlittable()))
        {
            *pAlignment = pLayoutInfo->GetAlignmentRequirement();
        }
        return true;
    }

    RawFieldPlacementInfo GetFieldPlacementInfo(CorElementType corElemType, TypeHandle pNestedType)
    {
        RawFieldPlacementInfo placementInfo;
        // Initialize offset to a dummy value as we set it to the correct value later.
        placementInfo.m_offset = (UINT32)-1;
        placementInfo.m_size = TARGET_POINTER_SIZE;
        placementInfo.m_alignment = TARGET_POINTER_SIZE;
        // This type may qualify for ManagedSequential. Collect managed size and alignment info.
        if (CorTypeInfo::IsPrimitiveType(corElemType))
        {
            // Safe cast - no primitive type is larger than 4gb!
            placementInfo.m_size = ((UINT32)CorTypeInfo::Size(corElemType));
#if defined(TARGET_X86) && defined(UNIX_X86_ABI)
            switch (corElemType)
            {
                // The System V ABI for i386 defines different packing for these types.
            case ELEMENT_TYPE_I8:
            case ELEMENT_TYPE_U8:
            case ELEMENT_TYPE_R8:
            {
                placementInfo.m_alignment = 4;
                break;
            }

            default:
            {
                placementInfo.m_alignment = placementInfo.m_size;
                break;
            }
            }
#else // TARGET_X86 && UNIX_X86_ABI
            placementInfo.m_alignment = placementInfo.m_size;
#endif
        }
        else if (corElemType == ELEMENT_TYPE_PTR || corElemType == ELEMENT_TYPE_FNPTR)
        {
            placementInfo.m_size = TARGET_POINTER_SIZE;
            placementInfo.m_alignment = TARGET_POINTER_SIZE;
        }
        else if (corElemType == ELEMENT_TYPE_VALUETYPE)
        {
            _ASSERTE(!pNestedType.IsNull());

            placementInfo.m_size = (pNestedType.GetMethodTable()->GetNumInstanceFieldBytes());

#if !defined(TARGET_64BIT) && (DATA_ALIGNMENT > 4)
            if (placementInfo.m_size >= DATA_ALIGNMENT)
            {
                placementInfo.m_alignment = DATA_ALIGNMENT;
            }
            else
#elif defined(FEATURE_64BIT_ALIGNMENT)
            if (pNestedType.RequiresAlign8())
            {
                placementInfo.m_alignment = 8;
            }
            else
#endif // FEATURE_64BIT_ALIGNMENT
            if (pNestedType.GetMethodTable()->ContainsGCPointers())
            {
                // this field type has GC pointers in it, which need to be pointer-size aligned
                placementInfo.m_alignment = TARGET_POINTER_SIZE;
            }
            else
            {
                placementInfo.m_alignment = pNestedType.GetMethodTable()->GetFieldAlignmentRequirement();
            }
        }

        // No other type permitted for ManagedSequential.
        return placementInfo;
    }

    void InitializeLayoutFieldInfoArray(FieldDesc* pFields, ULONG cFields, MethodTable** pByValueClassCache, BYTE packingSize, LayoutRawFieldInfo* pInfoArray, UINT32* pNumInstanceFields, BYTE* pAlignmentRequirement)
    {
        ULONG cInstanceFields = 0;
        BYTE alignmentRequirement = 0;
        for (ULONG i = 0; i < cFields; i++)
        {
            FieldDesc* pField = &pFields[i];
            if (pField->IsStatic())
                continue;

            cInstanceFields++;
            CorElementType corElemType = pField->GetFieldType();
            TypeHandle typeHandleMaybe{};

            if (corElemType == ELEMENT_TYPE_VALUETYPE)
            {
                typeHandleMaybe = pByValueClassCache[i];

                corElemType = typeHandleMaybe.AsMethodTable()->GetInternalCorElementType();
                if (corElemType != ELEMENT_TYPE_VALUETYPE)
                    typeHandleMaybe = TypeHandle{};
            }

            pInfoArray[i].m_token = pField->GetMemberDef();
            pInfoArray[i].m_placement = GetFieldPlacementInfo(corElemType, typeHandleMaybe);

            BYTE fieldAlignmentRequirement = (BYTE)pInfoArray[i].m_placement.m_alignment;

            fieldAlignmentRequirement = min(fieldAlignmentRequirement, packingSize);

            alignmentRequirement = max(alignmentRequirement, fieldAlignmentRequirement);

            switch (fieldAlignmentRequirement)
            {
            case 1:
            case 2:
            case 4:
            case 8:
            case 16:
            case 32:
            case 64:
                break;
            default:
                COMPlusThrowHR(COR_E_INVALIDPROGRAM, BFA_METADATA_CORRUPT);
            }
        }

        *pNumInstanceFields = cInstanceFields;
        *pAlignmentRequirement = alignmentRequirement;
    }

    void SetFieldOffsets(FieldDesc* pFields, ULONG cFields, LayoutRawFieldInfo* pInfoArray, ULONG cInstanceFields)
    {
        for (ULONG i = 0, iInstanceFieldInfo = 0; i < cFields; i++)
        {
            FieldDesc* pField = &pFields[i];
            if (pField->IsStatic())
                continue;

            // We should only be placing unplaced fields at this point.
            _ASSERTE(pField->GetOffset() == FIELD_OFFSET_UNPLACED
                || pField->GetOffset() == FIELD_OFFSET_UNPLACED_GC_PTR
                || pField->GetOffset() == FIELD_OFFSET_VALUE_CLASS);

            _ASSERTE(iInstanceFieldInfo < cInstanceFields);
            IfFailThrow(pField->SetOffset(pInfoArray[iInstanceFieldInfo++].m_placement.m_offset));
        }
    }

    /// @brief Read the offsets for a type's fields from metadata for explicit layout.
    /// @param pModule The module containing the type.
    /// @param cl The metadata token of the type.
    /// @param pFieldInfoArray The information about the instance fields of the type.
    /// @param cInstanceFields The numer of instance fields in the type.
    /// @param parentSize The size of the parent type's layout.
    /// @return The end of the last field in this layout
    UINT32 ReadOffsetsForExplicitLayout(
        Module* pModule,
        const mdTypeDef cl,
        LayoutRawFieldInfo* pFieldInfoArray,
        const ULONG cInstanceFields,
        const UINT32 parentSize
    )
    {
        HRESULT hr;
        MD_CLASS_LAYOUT classlayout;
        IMDInternalImport* pInternalImport = pModule->GetMDImport();
        hr = pInternalImport->GetClassLayoutInit(cl, &classlayout);
        if (FAILED(hr))
        {
            COMPlusThrowHR(hr, BFA_CANT_GET_CLASSLAYOUT);
        }

        LayoutRawFieldInfo* pfwalk = pFieldInfoArray;
        mdFieldDef fd;
        ULONG ulOffset;
        UINT32 calcTotalSize = 0;
        while (SUCCEEDED(hr = pInternalImport->GetClassLayoutNext(
            &classlayout,
            &fd,
            &ulOffset)) &&
            fd != mdFieldDefNil)
        {
            // watch for the last entry: must be mdFieldDefNil
            while ((mdFieldDefNil != pfwalk->m_token) && (pfwalk->m_token < fd))
                pfwalk++;

            // if we haven't found a matching token, either we have invalid metadata
            // or the field doesn't have an entry. We'll error out in the next loop.
            if (pfwalk->m_token != fd) continue;

            // ulOffset is the explicit offset
            pfwalk->m_placement.m_offset = ulOffset;

            // Treat base class as an initial member.
            if (!ClrSafeInt<UINT32>::addition(pfwalk->m_placement.m_offset, parentSize, pfwalk->m_placement.m_offset))
                COMPlusThrowOM();

            uint32_t fieldEnd;
            if (!ClrSafeInt<uint32_t>::addition(pfwalk->m_placement.m_offset, pfwalk->m_placement.m_size, fieldEnd))
                COMPlusThrowOM();

            // size of the structure is the size of the last field.
            if (fieldEnd > calcTotalSize)
                calcTotalSize = fieldEnd;
        }
        IfFailThrow(hr);

        for (ULONG i = 0; i < cInstanceFields; i++)
        {
            if (pFieldInfoArray[i].m_token != mdFieldDefNil)
            {
                if (pFieldInfoArray[i].m_placement.m_offset == (UINT32)-1)
                {
                    LPCUTF8 szFieldName;
                    if (FAILED(pInternalImport->GetNameOfFieldDef(pFieldInfoArray[i].m_token, &szFieldName)))
                    {
                        szFieldName = "Invalid FieldDef record";
                    }
                    pModule->GetAssembly()->ThrowTypeLoadException(pInternalImport,
                        cl,
                        szFieldName,
                        IDS_CLASSLOAD_NSTRUCT_EXPLICIT_OFFSET);
                }
                else if (pFieldInfoArray[i].m_placement.m_offset > INT32_MAX)
                {
                    LPCUTF8 szFieldName;
                    if (FAILED(pInternalImport->GetNameOfFieldDef(pFieldInfoArray[i].m_token, &szFieldName)))
                    {
                        szFieldName = "Invalid FieldDef record";
                    }
                    pModule->GetAssembly()->ThrowTypeLoadException(pInternalImport,
                        cl,
                        szFieldName,
                        IDS_CLASSLOAD_NSTRUCT_NEGATIVE_OFFSET);
                }
            }
        }

        return calcTotalSize;
    }

    /// @brief Calculate the offsets of the fields if they were to be laid out in sequential order at their alignment requirements.
    /// @param pFieldInfoArray The information about the instance fields of the type.
    /// @param cInstanceFields The numer of instance fields in the type.
    /// @param parentSize The size of the parent type's layout.
    /// @param packingSize The packing size of the type.
    /// @return The end of the last field in this layout
    ULONG CalculateOffsetsForSequentialLayout(
        LayoutRawFieldInfo* pFieldInfoArray,
        const ULONG numInstanceFields,
        const UINT32 parentSize,
        const BYTE packingSize
    )
    {
        _ASSERTE(packingSize != 0);
        UINT32 cbCurOffset = parentSize;

        // Start with the size inherited from the parent (if any).
        uint32_t calcTotalSize = parentSize;

        for (UINT32 i = 0; i < numInstanceFields; i++)
        {
            RawFieldPlacementInfo& placementInfo = pFieldInfoArray[i].m_placement;

            BYTE alignmentRequirement = min((BYTE)placementInfo.m_alignment, packingSize);

            // Insert enough padding to align the current data member.
            if (!ClrSafeInt<uint32_t>::addition(cbCurOffset, (alignmentRequirement - (cbCurOffset % alignmentRequirement)) % alignmentRequirement, cbCurOffset))
                COMPlusThrowOM();

            placementInfo.m_offset = cbCurOffset;

            if (!ClrSafeInt<uint32_t>::addition(cbCurOffset, placementInfo.m_size, cbCurOffset))
            {
                COMPlusThrowOM();
            }

            // size of the structure is the size of the last field.
            if (cbCurOffset > calcTotalSize)
                calcTotalSize = cbCurOffset;
        }

        return calcTotalSize;
    }

    ULONG CalculateSizeWithMetadataSize(
        const ULONG parentSize,
        const UINT32 lastFieldEnd,
        const ULONG classSizeInMetadata
    )
    {
        // If we have successfully fetched the class size from metadata,
        // we'll try to use it. Add the parent size to the metadata size,
        // so it represents the full size of the class.
        ULONG classSize;
        if (!ClrSafeInt<ULONG>::addition(classSizeInMetadata, (ULONG)parentSize, classSize))
            COMPlusThrowOM();

        // size must be large enough to accommodate layout. If not, we use the layout size instead.
        return max((uint32_t)classSize, lastFieldEnd);
    }

    UINT32 AlignSize(
        const UINT32 lastFieldEnd,
        BYTE alignmentRequirement
    )
    {
        ULONG calcTotalSize = lastFieldEnd;

        // There was no class size given in metadata, so let's round up to a multiple of the alignment requirement
        // to make array allocations of this structure simple to keep aligned.
        if (calcTotalSize % alignmentRequirement != 0)
        {
            if (!ClrSafeInt<ULONG>::addition(calcTotalSize, (alignmentRequirement - (calcTotalSize % alignmentRequirement)) % alignmentRequirement, calcTotalSize))
                COMPlusThrowOM();
        }

        return calcTotalSize;
    }

    BOOL TypeHasGCPointers(CorElementType corElemType, TypeHandle pNestedType)
    {
        if (CorTypeInfo::IsPrimitiveType(corElemType) || corElemType == ELEMENT_TYPE_PTR || corElemType == ELEMENT_TYPE_FNPTR ||
            corElemType == ELEMENT_TYPE_BYREF)
        {
            return FALSE;
        }
        if (corElemType == ELEMENT_TYPE_VALUETYPE)
        {
            _ASSERTE(!pNestedType.IsNull());
            return pNestedType.GetMethodTable()->ContainsGCPointers() != FALSE;
        }
        return TRUE;
    }

    BOOL TypeHasAutoLayoutField(CorElementType corElemType, TypeHandle pNestedType)
    {
        if (CorTypeInfo::IsPrimitiveType(corElemType) || corElemType == ELEMENT_TYPE_PTR || corElemType == ELEMENT_TYPE_FNPTR)
        {
            return FALSE;
        }
        if (corElemType == ELEMENT_TYPE_VALUETYPE)
        {
            _ASSERTE(!pNestedType.IsNull());
            return pNestedType.IsEnum() || pNestedType.GetMethodTable()->IsAutoLayoutOrHasAutoLayoutField();
        }
        return FALSE;
    }

    BOOL TypeHasInt128Field(CorElementType corElemType, TypeHandle pNestedType)
    {
        if (corElemType == ELEMENT_TYPE_VALUETYPE)
        {
            _ASSERTE(!pNestedType.IsNull());
            return pNestedType.GetMethodTable()->IsInt128OrHasInt128Fields();
        }
        return FALSE;
    }

    ParseNativeTypeFlags NlTypeToNativeTypeFlags(CorNativeLinkType nlType)
    {
        ParseNativeTypeFlags nativeTypeFlags = ParseNativeTypeFlags::None;
        if (nlType == nltAnsi)
            nativeTypeFlags = ParseNativeTypeFlags::IsAnsi;

        return nativeTypeFlags;
    }

#ifdef FEATURE_DOUBLE_ALIGNMENT_HINT
    //*******************************************************************************
    //
    // Heuristic to determine if we should have instances of this class 8 byte aligned
    //
    bool ShouldAlign8(ULONG dwR8Fields, ULONG dwTotalFields)
    {
        LIMITED_METHOD_CONTRACT;

        return dwR8Fields*2>dwTotalFields && dwR8Fields>=2;
    }
#endif
}

auto EEClassLayoutInfo::GetNestedFieldFlags(Module* pModule, FieldDesc *pFields, ULONG cFields, CorNativeLinkType nlType, MethodTable** pByValueClassCache) -> NestedFieldFlags
{
    STANDARD_VM_CONTRACT;

    NestedFieldFlags flags = NestedFieldFlags::None;
    const ParseNativeTypeFlags nativeTypeFlags = NlTypeToNativeTypeFlags(nlType);

    ULONG numR8Fields = 0;
    ULONG numInstanceFields = 0;

    for (ULONG i = 0; i < cFields; i++)
    {
        FieldDesc* pField = &pFields[i];
        if (pField->IsStatic())
            continue;

        numInstanceFields++;
        CorElementType corElemType = pField->GetFieldType();
        TypeHandle typeHandleMaybe{};

        if (corElemType == ELEMENT_TYPE_VALUETYPE)
        {
            typeHandleMaybe = pByValueClassCache[i];

            corElemType = typeHandleMaybe.AsMethodTable()->GetInternalCorElementType();
            if (corElemType != ELEMENT_TYPE_VALUETYPE)
                typeHandleMaybe = TypeHandle();
        }

        if (corElemType == ELEMENT_TYPE_R8)
        {
            numR8Fields++;
        }

#ifdef FEATURE_64BIT_ALIGNMENT
        if (!typeHandleMaybe.IsNull() && typeHandleMaybe.GetMethodTable()->GetClass()->IsAlign8Candidate())
        {
            flags |= NestedFieldFlags::Align8;
        }

        if (corElemType == ELEMENT_TYPE_I8
            || corElemType == ELEMENT_TYPE_U8
            || corElemType == ELEMENT_TYPE_R8
            IN_TARGET_64BIT(|| corElemType == ELEMENT_TYPE_I || corElemType == ELEMENT_TYPE_U))
        {
            flags |= NestedFieldFlags::Align8;
        }
#endif

        if (!IsFieldBlittable(pModule, pField->GetMemberDef(), corElemType, typeHandleMaybe, nativeTypeFlags))
        {
            flags |= NestedFieldFlags::NonBlittable;
        }

        if (TypeHasGCPointers(corElemType, typeHandleMaybe))
        {
            flags |= NestedFieldFlags::GCPointer;
        }

        if (TypeHasAutoLayoutField(corElemType, typeHandleMaybe))
        {
            flags |= NestedFieldFlags::AutoLayout;
        }

        if (TypeHasInt128Field(corElemType, typeHandleMaybe))
        {
            flags |= NestedFieldFlags::Int128;
        }
    }

#ifdef FEATURE_DOUBLE_ALIGNMENT_HINT
    if (ShouldAlign8(numR8Fields, numInstanceFields))
    {
        flags |= NestedFieldFlags::Align8;
    }
#endif

    return flags;
}

ULONG EEClassLayoutInfo::InitializeSequentialFieldLayout(
    FieldDesc* pFields,
    MethodTable** pByValueClassCache,
    ULONG cFields,
    BYTE packingSize,
    ULONG classSizeInMetadata,
    MethodTable* pParentMT
)
{
    STANDARD_VM_CONTRACT;

    SetLayoutType(LayoutType::Sequential);

    UINT32 cbAdjustedParentLayoutSize;
    BYTE parentAlignmentRequirement;
    if (!TryGetParentLayoutInfo(pParentMT, &cbAdjustedParentLayoutSize, &parentAlignmentRequirement))
    {
        cbAdjustedParentLayoutSize = 0;
        parentAlignmentRequirement = 0;
    }

    NewArrayHolder<LayoutRawFieldInfo> pInfoArray = new LayoutRawFieldInfo[cFields + 1];
    UINT32 numInstanceFields;
    BYTE fieldsAlignmentRequirement;
    InitializeLayoutFieldInfoArray(pFields, cFields, pByValueClassCache, packingSize, pInfoArray, &numInstanceFields, &fieldsAlignmentRequirement);

    BYTE alignmentRequirement = max(max<BYTE>(1, min(packingSize, parentAlignmentRequirement)), fieldsAlignmentRequirement);

    // The packingSize acts as a ceiling on all individual alignment
    // requirements so it follows that the largest alignment requirement
    // is also capped.
    _ASSERTE(alignmentRequirement <= packingSize);
    SetAlignmentRequirement(alignmentRequirement);
    SetPackingSize(packingSize);

    UINT32 lastFieldEnd = CalculateOffsetsForSequentialLayout(pInfoArray, numInstanceFields, cbAdjustedParentLayoutSize, packingSize);

    SetFieldOffsets(pFields, cFields, pInfoArray, numInstanceFields);

    UINT32 managedSize;
    if (classSizeInMetadata != 0)
    {
        managedSize = CalculateSizeWithMetadataSize(cbAdjustedParentLayoutSize, lastFieldEnd, classSizeInMetadata);
    }
    else
    {
        managedSize = AlignSize(lastFieldEnd, alignmentRequirement);
    }

    return SetInstanceBytesSize(managedSize);
}

ULONG EEClassLayoutInfo::InitializeExplicitFieldLayout(
    FieldDesc* pFields,
    MethodTable** pByValueClassCache,
    ULONG cFields,
    BYTE packingSize,
    ULONG classSizeInMetadata,
    MethodTable* pParentMT,
    Module* pModule,
    mdTypeDef cl
)
{
    STANDARD_VM_CONTRACT;

    SetLayoutType(LayoutType::Explicit);

    UINT32 cbAdjustedParentLayoutSize;
    BYTE parentAlignmentRequirement;
    if (!TryGetParentLayoutInfo(pParentMT, &cbAdjustedParentLayoutSize, &parentAlignmentRequirement))
    {
        cbAdjustedParentLayoutSize = 0;
        parentAlignmentRequirement = 0;
    }

    NewArrayHolder<LayoutRawFieldInfo> pInfoArray = new LayoutRawFieldInfo[cFields + 1];
    UINT32 numInstanceFields;
    BYTE fieldsAlignmentRequirement;
    InitializeLayoutFieldInfoArray(pFields, cFields, pByValueClassCache, packingSize, pInfoArray, &numInstanceFields, &fieldsAlignmentRequirement);

    BYTE alignmentRequirement = max(max<BYTE>(1, min(packingSize, parentAlignmentRequirement)), fieldsAlignmentRequirement);

    // The packingSize acts as a ceiling on all individual alignment
    // requirements so it follows that the largest alignment requirement
    // is also capped.
    _ASSERTE(alignmentRequirement <= packingSize);
    SetAlignmentRequirement(alignmentRequirement);
    SetPackingSize(packingSize);

    UINT32 lastFieldEnd = 0;
    lastFieldEnd = ReadOffsetsForExplicitLayout(pModule, cl, pInfoArray, numInstanceFields, cbAdjustedParentLayoutSize);

    SetFieldOffsets(pFields, cFields, pInfoArray, numInstanceFields);

    UINT32 managedSize;
    if (classSizeInMetadata != 0)
    {
        managedSize = CalculateSizeWithMetadataSize(cbAdjustedParentLayoutSize, lastFieldEnd, classSizeInMetadata);
    }
    else
    {
        managedSize = AlignSize(lastFieldEnd, alignmentRequirement);
    }

    return SetInstanceBytesSize(managedSize);
}

namespace
{
    #ifdef UNIX_AMD64_ABI
    void SystemVAmd64CheckForPassNativeStructInRegister(MethodTable* pMT, EEClassNativeLayoutInfo* pNativeLayoutInfo)
    {
        STANDARD_VM_CONTRACT;
        DWORD totalStructSize = 0;

        // If not a native value type, return.
        if (!pMT->IsValueType())
        {
            return;
        }

        totalStructSize = pNativeLayoutInfo->GetSize();

        // If num of bytes for the fields is bigger than CLR_SYSTEMV_MAX_STRUCT_BYTES_TO_PASS_IN_REGISTERS
        // pass through stack
        if (totalStructSize > CLR_SYSTEMV_MAX_STRUCT_BYTES_TO_PASS_IN_REGISTERS)
        {
            LOG((LF_JIT, LL_EVERYTHING, "**** SystemVAmd64CheckForPassNativeStructInRegister: struct %s is too big to pass in registers (%d bytes)\n",
                pMT->GetDebugClassName(), totalStructSize));
            return;
        }

        _ASSERTE(pMT->HasLayout());

        // Iterate through the fields and make sure they meet requirements to pass in registers
        SystemVStructRegisterPassingHelper helper((unsigned int)totalStructSize);
        if (pMT->ClassifyEightBytesWithNativeLayout(&helper, 0, 0, pNativeLayoutInfo))
        {
            pNativeLayoutInfo->SetNativeStructPassedInRegisters();
        }
    }
#endif // UNIX_AMD64_ABI

    //=====================================================================
    // ParseNativeFieldTypes:
    // Figure out the native field type of each field based on both the CLR
    // signature of the field and the FieldMarshaler metadata.
    //=====================================================================
    void ParseFieldNativeTypes(
        IMDInternalImport* pInternalImport,
        const mdTypeDef cl,
        ApproxFieldDescIterator fieldDescs,
        Module* pModule,
        ParseNativeTypeFlags nativeTypeFlags,
        const SigTypeContext* pTypeContext,
        LayoutRawFieldInfo* pFieldInfoArrayOut
    #ifdef _DEBUG
        ,
        LPCUTF8 szNamespace,
        LPCUTF8 szName
    #endif
    )
    {
        HRESULT hr;

        for (int i = 0; i < fieldDescs.Count(); i++, pFieldInfoArrayOut++)
        {
            DWORD dwFieldAttrs;

            FieldDesc* pFieldDesc = fieldDescs.Next();
            mdFieldDef fd = pFieldDesc->GetMemberDef();

            IfFailThrow(pInternalImport->GetFieldDefProps(fd, &dwFieldAttrs));

            PCCOR_SIGNATURE pNativeType = NULL;
            ULONG cbNativeType;
            if (IsFdHasFieldMarshal(dwFieldAttrs))
            {
                hr = pInternalImport->GetFieldMarshal(fd, &pNativeType, &cbNativeType);
                if (FAILED(hr))
                {
                    cbNativeType = 0;
                }
            }
            else
            {
                cbNativeType = 0;
            }

            PCCOR_SIGNATURE pCOMSignature;
            ULONG cbCOMSignature;
            pFieldDesc->GetSig(&pCOMSignature, &cbCOMSignature);

            // fill the appropriate entry in pInfoArray
            pFieldInfoArrayOut->m_token = fd;
            pFieldInfoArrayOut->m_placement.m_offset = (UINT32)-1;

    #ifdef _DEBUG
            LPCUTF8 szFieldName;
            if (FAILED(pInternalImport->GetNameOfFieldDef(fd, &szFieldName)))
            {
                szFieldName = "Invalid FieldDef record";
            }
    #endif
            MetaSig fsig(pCOMSignature, cbCOMSignature, pModule, pTypeContext, MetaSig::sigField);
            fsig.NextArg();

            ParseNativeType(pModule,
                fsig.GetArgProps(),
                pFieldDesc,
                nativeTypeFlags,
                &pFieldInfoArrayOut->m_nfd,
                pTypeContext
    #ifdef _DEBUG
                ,
                szNamespace,
                szName,
                szFieldName
    #endif
            );
        }

        // NULL out the last entry
        pFieldInfoArrayOut->m_token = mdFieldDefNil;
    }

#ifdef FEATURE_HFA
    //
    // The managed and unmanaged views of the types can differ for non-blitable types. This method
    // mirrors the HFA type computation for the unmanaged view.
    //
    void CheckForNativeHFA(MethodTable* pMT, EEClassNativeLayoutInfo* pNativeLayoutInfo)
    {
        STANDARD_VM_CONTRACT;

        // No HFAs with inheritance
        if (!(pMT->IsValueType() || (pMT->GetParentMethodTable() == g_pObjectClass)))
            return;

        // No HFAs with explicit layout. There may be cases where explicit layout may be still
        // eligible for HFA, but it is hard to tell the real intent. Make it simple and just
        // unconditionally disable HFAs for explicit layout.
        if (pMT->GetClass()->HasExplicitFieldOffsetLayout())
            return;

        CorInfoHFAElemType hfaType = pNativeLayoutInfo->GetNativeHFATypeRaw();
        if (hfaType == CORINFO_HFA_ELEM_NONE)
        {
            return;
        }

        // All the above tests passed. It's HFA!
        pNativeLayoutInfo->SetHFAType(hfaType);
    }
#endif // FEATURE_HFA

    EEClassNativeLayoutInfo const* FindParentNativeLayoutInfo(MethodTable* pParentMT)
    {
        STANDARD_VM_CONTRACT;

        if (!pParentMT || !pParentMT->HasLayout())
            return nullptr;

        bool fHasNonTrivialParent = pParentMT &&
            !pParentMT->IsObjectClass() &&
            !pParentMT->IsValueTypeClass();

        // Set some defaults based on the parent type of this type (if one exists).
        _ASSERTE(!(fHasNonTrivialParent && !(pParentMT->HasLayout())));

        return pParentMT->GetNativeLayoutInfo();
    }
}

void EEClassNativeLayoutInfo::InitializeNativeLayoutFieldMetadataThrowing(MethodTable* pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(pMT->HasLayout());
    }
    CONTRACTL_END;

    EEClass* pClass = pMT->GetClass();
    EEClassLayoutInfo* pLayoutInfo = pClass->GetLayoutInfo();

    if (pClass->GetNativeLayoutInfo() == nullptr)
    {
        GCX_PREEMP();
        ListLockHolder nativeTypeLoadLock(AppDomain::GetCurrentDomain()->GetNativeTypeLoadLock());
        ListLockEntryHolder entry(ListLockEntry::Find(nativeTypeLoadLock, pMT->GetClass()));
        ListLockEntryLockHolder pEntryLock(entry, FALSE);
        nativeTypeLoadLock.Release();
        if (!pEntryLock.DeadlockAwareAcquire())
        {
            DefineFullyQualifiedNameForClassW()
            COMPlusThrow(kTypeLoadException, IDS_CANNOT_MARSHAL_RECURSIVE_DEF, GetFullyQualifiedNameForClassW(pMT));
        }

        if (pClass->GetNativeLayoutInfo() == nullptr)
        {
            EEClassNativeLayoutInfo* pNativeLayoutInfo = CollectNativeLayoutFieldMetadataThrowing(pMT);
#ifdef FEATURE_HFA
            CheckForNativeHFA(pMT, pNativeLayoutInfo);
#endif
#ifdef UNIX_AMD64_ABI
            SystemVAmd64CheckForPassNativeStructInRegister(pMT, pNativeLayoutInfo);
#endif
            ((LayoutEEClass*)pClass)->m_nativeLayoutInfo = pNativeLayoutInfo;
        }
    }
}

EEClassNativeLayoutInfo* EEClassNativeLayoutInfo::CollectNativeLayoutFieldMetadataThrowing(MethodTable* pMT)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(pMT->HasLayout());
    }
    CONTRACTL_END;

    Module* pModule = pMT->GetModule();
    // Internal interface for the NStruct being loaded.
    IMDInternalImport* pInternalImport = pModule->GetMDImport();

#ifdef _DEBUG
    LPCUTF8 szName;
    LPCUTF8 szNamespace;
    if (FAILED(pInternalImport->GetNameOfTypeDef(pMT->GetCl(), &szName, &szNamespace)))
    {
        szName = szNamespace = "Invalid TypeDef record";
    }

    if (g_pConfig->ShouldBreakOnStructMarshalSetup(szName))
        CONSISTENCY_CHECK_MSGF(false, ("BreakOnStructMarshalSetup: '%s' ", szName));
#endif

    MethodTable* pParentMT = pMT->GetParentMethodTable();

    UINT32 cbAdjustedParentLayoutNativeSize = 0;
    EEClassNativeLayoutInfo const* pParentLayoutInfo = FindParentNativeLayoutInfo(pParentMT);
    if (pParentLayoutInfo != nullptr)
    {
        // Treat base class as an initial member.
        cbAdjustedParentLayoutNativeSize = pParentLayoutInfo->GetSize();
        // If the parent was originally a zero-sized explicit type but
        // got bumped up to a size of 1 for compatibility reasons, then
        // we need to remove the padding, but ONLY for inheritance situations.
        if (pParentMT->GetLayoutInfo()->IsZeroSized()) {
            CONSISTENCY_CHECK(cbAdjustedParentLayoutNativeSize == 1);
            cbAdjustedParentLayoutNativeSize = 0;
        }
    }

    CorNativeLinkType charSet = pMT->GetCharSet();

    ParseNativeTypeFlags nativeTypeFlags = NlTypeToNativeTypeFlags(charSet);

    ApproxFieldDescIterator fieldDescs(pMT, ApproxFieldDescIterator::INSTANCE_FIELDS);

    ULONG cInstanceFields = fieldDescs.Count();

    NewArrayHolder<LayoutRawFieldInfo> pInfoArray = new LayoutRawFieldInfo[cInstanceFields + 1];

    SigTypeContext context(pMT);

    ParseFieldNativeTypes(
        pInternalImport,
        pMT->GetCl(),
        fieldDescs,
        pModule,
        nativeTypeFlags,
        &context,
        pInfoArray
        DEBUGARG(szNamespace)
        DEBUGARG(szName)
    );

    uint32_t numTotalInstanceFields = cInstanceFields + (pParentLayoutInfo != nullptr ? pParentLayoutInfo->GetNumFields() : 0);
    LoaderAllocator* pAllocator = pMT->GetLoaderAllocator();
    AllocMemHolder<EEClassNativeLayoutInfo> pNativeLayoutInfo(
        pAllocator->GetLowFrequencyHeap()->AllocMem(
            S_SIZE_T(sizeof(EEClassNativeLayoutInfo)) + S_SIZE_T(sizeof(NativeFieldDescriptor)) * S_SIZE_T(numTotalInstanceFields)));

    pNativeLayoutInfo->m_numFields = numTotalInstanceFields;

    // If there's no parent, pretend that the parent alignment requirement is 1.
    BYTE parentAlignmentRequirement = 1;
    if (pParentLayoutInfo != nullptr)
    {
        parentAlignmentRequirement = pParentLayoutInfo->GetLargestAlignmentRequirement();
    }

    BYTE packingSize = pMT->GetLayoutInfo()->GetPackingSize();
    if (packingSize == 0)
    {
        packingSize = DEFAULT_PACKING_SIZE;
    }

    BYTE fieldAlignmentRequirement = 0;
    // Now compute the native size of each field
    for (LayoutRawFieldInfo* pfwalk = pInfoArray; pfwalk->m_token != mdFieldDefNil; pfwalk++)
    {
        pfwalk->m_placement.m_size = pfwalk->m_nfd.NativeSize();
        // Allow the packing size to override a looser alignment requirement.
        pfwalk->m_placement.m_alignment = min(packingSize, (BYTE)pfwalk->m_nfd.AlignmentRequirement());
        if (pfwalk->m_placement.m_alignment > fieldAlignmentRequirement)
        {
            fieldAlignmentRequirement = (BYTE)pfwalk->m_placement.m_alignment;
        }
    }

    // Allow the packing size to require less alignment than the parent's alignment requirement.
    BYTE initialAlignmentRequirement = min(packingSize, parentAlignmentRequirement);

    // The alignment of the native layout is the stricter of the initial alignment requirement or the alignment requirements of the fields.
    pNativeLayoutInfo->m_alignmentRequirement = max(initialAlignmentRequirement, fieldAlignmentRequirement);

    BOOL fExplicitOffsets = pMT->GetClass()->HasExplicitFieldOffsetLayout();

    ULONG lastFieldEnd = 0;
    if (fExplicitOffsets)
    {
        lastFieldEnd = ReadOffsetsForExplicitLayout(pModule, pMT->GetCl(), pInfoArray, cInstanceFields, cbAdjustedParentLayoutNativeSize);
    }
    else
    {
        lastFieldEnd = CalculateOffsetsForSequentialLayout(pInfoArray, cInstanceFields, cbAdjustedParentLayoutNativeSize, packingSize);
    }

    EEClassLayoutInfo* pEEClassLayoutInfo = pMT->GetLayoutInfo();

    if (pEEClassLayoutInfo->HasExplicitSize())
    {
        ULONG classSizeInMetadata = 0;
        HRESULT hr = pInternalImport->GetClassTotalSize(pMT->GetCl(), &classSizeInMetadata);

        CONSISTENCY_CHECK(hr == S_OK);

        pNativeLayoutInfo->m_size = CalculateSizeWithMetadataSize(cbAdjustedParentLayoutNativeSize, lastFieldEnd, classSizeInMetadata);
    }
    else if (pMT->GetClass()->IsInlineArray())
    {
        // If the type is an inline array, we need to calculate the size based on the number of elements.
        const void* pVal;                  // The custom value.
        ULONG       cbVal;                 // Size of the custom value.
        HRESULT hr = pMT->GetCustomAttribute(
            WellKnownAttribute::InlineArrayAttribute,
            &pVal, &cbVal);

        if (hr != S_FALSE)
        {
            // Validity of the InlineArray attribute is checked at type-load time,
            // so we only assert here as we should have already checked this and failed
            // type load if this condition is false.
            _ASSERTE(cbVal >= (sizeof(INT32) + 2));
            if (cbVal >= (sizeof(INT32) + 2))
            {
                INT32 repeat = GET_UNALIGNED_VAL32((byte*)pVal + 2);
                if (repeat > 0)
                {
                    pNativeLayoutInfo->m_size = repeat * pInfoArray[0].m_nfd.NativeSize();
                }
            }
        }
    }
    else
    {
        pNativeLayoutInfo->m_size = AlignSize(lastFieldEnd, pNativeLayoutInfo->GetLargestAlignmentRequirement());
    }

    // We'll cap the total native size at a (somewhat) arbitrary limit to ensure
    // that we don't expose some overflow bug later on.
    if (pNativeLayoutInfo->m_size >= MAX_SIZE_FOR_INTEROP)
        COMPlusThrowOM();

    if (pNativeLayoutInfo->m_size == 0)
    {
        _ASSERTE(pEEClassLayoutInfo->IsZeroSized());
        pNativeLayoutInfo->m_size = 1; // Bump the managed size of the structure up to 1.
    }

    // The intrinsic Vector types have specialized alignment requirements. For these types,
    // copy the managed alignment requirement as the native alignment requirement.
    if (pMT->IsIntrinsicType())
    {
        // The intrinsic Vector<T> type has a special size. Copy the native size and alignment
        // from the managed size and alignment.
        if (pMT->HasSameTypeDefAs(CoreLibBinder::GetClass(CLASS__VECTORT)))
        {
            pNativeLayoutInfo->m_size = pMT->GetNumInstanceFieldBytes();
            pNativeLayoutInfo->m_alignmentRequirement = pEEClassLayoutInfo->GetAlignmentRequirement();
        }
        else
        if (pMT->HasSameTypeDefAs(CoreLibBinder::GetClass(CLASS__INT128)) ||
            pMT->HasSameTypeDefAs(CoreLibBinder::GetClass(CLASS__UINT128)) ||
            pMT->HasSameTypeDefAs(CoreLibBinder::GetClass(CLASS__VECTOR64T)) ||
            pMT->HasSameTypeDefAs(CoreLibBinder::GetClass(CLASS__VECTOR128T)) ||
            pMT->HasSameTypeDefAs(CoreLibBinder::GetClass(CLASS__VECTOR256T)) ||
            pMT->HasSameTypeDefAs(CoreLibBinder::GetClass(CLASS__VECTOR512T)))
        {
            pNativeLayoutInfo->m_alignmentRequirement = pEEClassLayoutInfo->GetAlignmentRequirement();
        }
    }

    PTR_NativeFieldDescriptor pNativeFieldDescriptors = pNativeLayoutInfo->GetNativeFieldDescriptors();

    // Bring in the parent's fieldmarshalers
    if (pParentLayoutInfo != nullptr)
    {
        UINT numChildCTMFields = cInstanceFields;

        NativeFieldDescriptor const* pParentCTMFieldSrcArray = pParentLayoutInfo->GetNativeFieldDescriptors();
        NativeFieldDescriptor* pParentCTMFieldDestArray = pNativeFieldDescriptors + numChildCTMFields;

        for (UINT parentCTMFieldIndex = 0; parentCTMFieldIndex < pParentLayoutInfo->GetNumFields(); parentCTMFieldIndex++)
        {
            pParentCTMFieldDestArray[parentCTMFieldIndex] = pParentCTMFieldSrcArray[parentCTMFieldIndex];
        }
    }

    bool isMarshalable = pParentLayoutInfo != nullptr ? pParentLayoutInfo->IsMarshalable() : true;
    for (UINT i = 0; i < cInstanceFields; i++)
    {
        pInfoArray[i].m_nfd.SetExternalOffset(pInfoArray[i].m_placement.m_offset);
        pNativeFieldDescriptors[i] = pInfoArray[i].m_nfd;
        if (pNativeFieldDescriptors[i].IsUnmarshalable())
        {
            isMarshalable = false;
        }
    }

    pNativeLayoutInfo->SetIsMarshalable(isMarshalable);

    pNativeLayoutInfo.SuppressRelease();

#ifdef _DEBUG
    {
        if (pEEClassLayoutInfo->IsBlittable())
        {
            for (UINT i = 0; i < cInstanceFields; i++)
            {
                _ASSERTE(pNativeFieldDescriptors[i].GetExternalOffset() == pNativeFieldDescriptors[i].GetFieldDesc()->GetOffset());
                _ASSERTE(pNativeFieldDescriptors[i].NativeSize() == pNativeFieldDescriptors[i].GetFieldDesc()->GetSize());
            }
            _ASSERTE(pNativeLayoutInfo->GetSize() == pMT->GetNumInstanceFieldBytes());
            _ASSERTE(pNativeLayoutInfo->GetLargestAlignmentRequirement() == pEEClassLayoutInfo->GetAlignmentRequirement());
        }

        LOG((LF_INTEROP, LL_INFO100000, "\n\n"));
        LOG((LF_INTEROP, LL_INFO100000, "%s.%s\n", szNamespace, szName));
        LOG((LF_INTEROP, LL_INFO100000, "Packsize      = %lu\n", (ULONG)pEEClassLayoutInfo->GetPackingSize()));
        LOG((LF_INTEROP, LL_INFO100000, "Max align req = %lu\n", (ULONG)(pNativeLayoutInfo->GetLargestAlignmentRequirement())));
        LOG((LF_INTEROP, LL_INFO100000, "----------------------------\n"));
        for (LayoutRawFieldInfo* pfwalk = pInfoArray; pfwalk->m_token != mdFieldDefNil; pfwalk++)
        {
            LPCUTF8 fieldname;
            if (FAILED(pInternalImport->GetNameOfFieldDef(pfwalk->m_token, &fieldname)))
            {
                fieldname = "??";
            }
            LOG((LF_INTEROP, LL_INFO100000, "+%-5lu  ", (ULONG)(pfwalk->m_placement.m_offset)));
            LOG((LF_INTEROP, LL_INFO100000, "%s", fieldname));
            LOG((LF_INTEROP, LL_INFO100000, "\n"));
        }

        LOG((LF_INTEROP, LL_INFO100000, "+%-5lu   EOS\n", (ULONG)(pNativeLayoutInfo->GetSize())));
        LOG((LF_INTEROP, LL_INFO100000, "Allocated %d %s field marshallers for %s.%s\n", numTotalInstanceFields, (!isMarshalable ? "pointless" : "usable"), szNamespace, szName));
    }
#endif
    return pNativeLayoutInfo;
}

#endif // DACCESS_COMPILE

CorInfoHFAElemType EEClassNativeLayoutInfo::GetNativeHFATypeRaw() const
{
    LIMITED_METHOD_CONTRACT;

    uint32_t numReferenceFields = GetNumFields();

    CorInfoHFAElemType hfaType = CORINFO_HFA_ELEM_NONE;

#ifndef DACCESS_COMPILE
    const NativeFieldDescriptor* pNativeFieldDescriptorsBegin = GetNativeFieldDescriptors();
    const NativeFieldDescriptor* pNativeFieldDescriptorsEnd = pNativeFieldDescriptorsBegin + numReferenceFields;
    for (const NativeFieldDescriptor* pCurrNFD = pNativeFieldDescriptorsBegin; pCurrNFD < pNativeFieldDescriptorsEnd; ++pCurrNFD)
    {
        CorInfoHFAElemType fieldType = CORINFO_HFA_ELEM_NONE;

        NativeFieldCategory category = pCurrNFD->GetCategory();

        if (category == NativeFieldCategory::FLOAT)
        {
            if (pCurrNFD->NativeSize() == 4)
            {
                fieldType = CORINFO_HFA_ELEM_FLOAT;
            }
            else if (pCurrNFD->NativeSize() == 8)
            {
                fieldType = CORINFO_HFA_ELEM_DOUBLE;
            }
            else
            {
                UNREACHABLE_MSG("Invalid NativeFieldCategory.");
                fieldType = CORINFO_HFA_ELEM_NONE;
            }

            // An HFA can only have aligned float and double fields.
            if (pCurrNFD->GetExternalOffset() % pCurrNFD->AlignmentRequirement() != 0)
            {
                fieldType = CORINFO_HFA_ELEM_NONE;
            }
        }
        else if (category == NativeFieldCategory::NESTED)
        {
            fieldType = pCurrNFD->GetNestedNativeMethodTable()->GetNativeHFAType();
        }
        else
        {
            return CORINFO_HFA_ELEM_NONE;
        }

        // Field type should be a valid HFA type.
        if (fieldType == CORINFO_HFA_ELEM_NONE)
        {
            return CORINFO_HFA_ELEM_NONE;
        }

        // Initialize with a valid HFA type.
        if (hfaType == CORINFO_HFA_ELEM_NONE)
        {
            hfaType = fieldType;
        }
        // All field types should be equal.
        else if (fieldType != hfaType)
        {
            return CORINFO_HFA_ELEM_NONE;
        }
    }

    if (hfaType == CORINFO_HFA_ELEM_NONE)
        return CORINFO_HFA_ELEM_NONE;

    int elemSize = 1;
    switch (hfaType)
    {
    case CORINFO_HFA_ELEM_FLOAT: elemSize = sizeof(float); break;
    case CORINFO_HFA_ELEM_DOUBLE: elemSize = sizeof(double); break;
#ifdef TARGET_ARM64
    case CORINFO_HFA_ELEM_VECTOR64: elemSize = 8; break;
    case CORINFO_HFA_ELEM_VECTOR128: elemSize = 16; break;
#endif
    default: _ASSERTE(!"Invalid HFA Type");
    }

    // Note that we check the total size, but do not perform any checks on number of fields:
    // - Type of fields can be HFA valuetype itself
    // - Managed C++ HFA valuetypes have just one <alignment member> of type float to signal that
    //   the valuetype is HFA and explicitly specified size

    DWORD totalSize = GetSize();

    if (totalSize % elemSize != 0)
        return CORINFO_HFA_ELEM_NONE;

    // On ARM, HFAs can have a maximum of four fields regardless of whether those are float or double.
    if (totalSize / elemSize > 4)
        return CORINFO_HFA_ELEM_NONE;

#endif // !DACCESS_COMPILE

    return hfaType;
}
