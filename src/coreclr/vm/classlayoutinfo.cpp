// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "class.h"
#include "fieldmarshaler.h"

#ifndef DACCESS_COMPILE

namespace
{
    void SetOffsetsAndSortFields(
        IMDInternalImport* pInternalImport,
        const mdTypeDef cl,
        LayoutRawFieldInfo* pFieldInfoArray,
        const ULONG cInstanceFields,
        const BOOL fExplicitOffsets,
        const UINT32 cbAdjustedParentLayoutNativeSize,
        Module* pModule,
        LayoutRawFieldInfo** pSortArrayOut
    )
    {
        HRESULT hr;
        MD_CLASS_LAYOUT classlayout;
        hr = pInternalImport->GetClassLayoutInit(cl, &classlayout);
        if (FAILED(hr))
        {
            COMPlusThrowHR(hr, BFA_CANT_GET_CLASSLAYOUT);
        }

        LayoutRawFieldInfo* pfwalk = pFieldInfoArray;
        mdFieldDef fd;
        ULONG ulOffset;
        while (SUCCEEDED(hr = pInternalImport->GetClassLayoutNext(
            &classlayout,
            &fd,
            &ulOffset)) &&
            fd != mdFieldDefNil)
        {
            // watch for the last entry: must be mdFieldDefNil
            while ((mdFieldDefNil != pfwalk->m_MD) && (pfwalk->m_MD < fd))
                pfwalk++;

            // if we haven't found a matching token, it must be a static field with layout -- ignore it
            if (pfwalk->m_MD != fd) continue;

            if (!fExplicitOffsets)
            {
                // ulOffset is the sequence
                pfwalk->m_sequence = ulOffset;
            }
            else
            {
                // ulOffset is the explicit offset
                pfwalk->m_placement.m_offset = ulOffset;
                pfwalk->m_sequence = (ULONG)-1;

                // Treat base class as an initial member.
                if (!SafeAddUINT32(&(pfwalk->m_placement.m_offset), cbAdjustedParentLayoutNativeSize))
                    COMPlusThrowOM();
            }
        }
        IfFailThrow(hr);

        LayoutRawFieldInfo** pSortArrayEnd = pSortArrayOut;
        // now sort the array
        if (!fExplicitOffsets)
        {
            // sort sequential by ascending sequence
            for (ULONG i = 0; i < cInstanceFields; i++)
            {
                LayoutRawFieldInfo** pSortWalk = pSortArrayEnd;
                while (pSortWalk != pSortArrayOut)
                {
                    if (pFieldInfoArray[i].m_sequence >= (*(pSortWalk - 1))->m_sequence)
                        break;

                    pSortWalk--;
                }

                // pSortWalk now points to the target location for new LayoutRawFieldInfo*.
                MoveMemory(pSortWalk + 1, pSortWalk, (pSortArrayEnd - pSortWalk) * sizeof(LayoutRawFieldInfo*));
                *pSortWalk = &pFieldInfoArray[i];
                pSortArrayEnd++;
            }
        }
        else // no sorting for explicit layout
        {
            for (ULONG i = 0; i < cInstanceFields; i++)
            {
                if (pFieldInfoArray[i].m_MD != mdFieldDefNil)
                {
                    if (pFieldInfoArray[i].m_placement.m_offset == (UINT32)-1)
                    {
                        LPCUTF8 szFieldName;
                        if (FAILED(pInternalImport->GetNameOfFieldDef(pFieldInfoArray[i].m_MD, &szFieldName)))
                        {
                            szFieldName = "Invalid FieldDef record";
                        }
                        pModule->GetAssembly()->ThrowTypeLoadException(pInternalImport,
                            cl,
                            szFieldName,
                            IDS_CLASSLOAD_NSTRUCT_EXPLICIT_OFFSET);
                    }
                    else if ((INT)pFieldInfoArray[i].m_placement.m_offset < 0)
                    {
                        LPCUTF8 szFieldName;
                        if (FAILED(pInternalImport->GetNameOfFieldDef(pFieldInfoArray[i].m_MD, &szFieldName)))
                        {
                            szFieldName = "Invalid FieldDef record";
                        }
                        pModule->GetAssembly()->ThrowTypeLoadException(pInternalImport,
                            cl,
                            szFieldName,
                            IDS_CLASSLOAD_NSTRUCT_NEGATIVE_OFFSET);
                    }
                }

                *pSortArrayEnd = &pFieldInfoArray[i];
                pSortArrayEnd++;
            }
        }
    }

    void CalculateSizeAndFieldOffsets(
        const UINT32 parentSize,
        ULONG numInstanceFields,
        BOOL fExplicitOffsets,
        LayoutRawFieldInfo* const* pSortedFieldInfoArray, // An array of pointers to LayoutRawFieldInfo's in ascending order when sequential layout.
        ULONG classSizeInMetadata,
        BYTE packingSize,
        BYTE parentAlignmentRequirement,
        BOOL limitToMaxInteropSize,
        BYTE* pLargestAlignmentRequirementOut,
        UINT32* pSizeOut
    )
    {
        UINT32 cbCurOffset = parentSize;
        BYTE LargestAlignmentRequirement = max(1, min(packingSize, parentAlignmentRequirement));

        // Start with the size inherited from the parent (if any).
        uint32_t calcTotalSize = parentSize;

        LayoutRawFieldInfo* const* pSortWalk;
        ULONG i;
        for (pSortWalk = pSortedFieldInfoArray, i = numInstanceFields; i; i--, pSortWalk++)
        {
            LayoutRawFieldInfo* pfwalk = *pSortWalk;
            RawFieldPlacementInfo* placementInfo = &pfwalk->m_placement;

            BYTE alignmentRequirement = placementInfo->m_alignment;

            alignmentRequirement = min(alignmentRequirement, packingSize);

            LargestAlignmentRequirement = max(LargestAlignmentRequirement, alignmentRequirement);

            switch (alignmentRequirement)
            {
            case 1:
            case 2:
            case 4:
            case 8:
            case 16:
            case 32:
                break;
            default:
                COMPlusThrowHR(COR_E_INVALIDPROGRAM, BFA_METADATA_CORRUPT);
            }

            if (!fExplicitOffsets)
            {
                // Insert enough padding to align the current data member.
                while (cbCurOffset % alignmentRequirement)
                {
                    if (!SafeAddUINT32(&cbCurOffset, 1))
                        COMPlusThrowOM();
                }

                // if we overflow we will catch it below
                placementInfo->m_offset = cbCurOffset;
                cbCurOffset += placementInfo->m_size;
            }

            uint32_t fieldEnd = placementInfo->m_offset + placementInfo->m_size;
            if (fieldEnd < placementInfo->m_offset)
                COMPlusThrowOM();

            // size of the structure is the size of the last field.
            if (fieldEnd > calcTotalSize)
                calcTotalSize = fieldEnd;
        }

        if (classSizeInMetadata != 0)
        {
            ULONG classSize = classSizeInMetadata;
            if (!SafeAddULONG(&classSize, (ULONG)parentSize))
                COMPlusThrowOM();

            // size must be large enough to accomodate layout. If not, we use the layout size instead.
            calcTotalSize = max(classSize, calcTotalSize);
        }
        else
        {
            // There was no class size given in metadata, so let's round up to a multiple of the alignment requirement
            // to make array allocations of this structure simple to keep aligned.
            calcTotalSize += (LargestAlignmentRequirement - calcTotalSize % LargestAlignmentRequirement) % LargestAlignmentRequirement;

            if (calcTotalSize % LargestAlignmentRequirement != 0)
            {
                if (!SafeAddUINT32(&calcTotalSize, LargestAlignmentRequirement - (calcTotalSize % LargestAlignmentRequirement)))
                    COMPlusThrowOM();
            }
        }

        // We'll cap the total native size at a (somewhat) arbitrary limit to ensure
        // that we don't expose some overflow bug later on.
        if (calcTotalSize >= MAX_SIZE_FOR_INTEROP && limitToMaxInteropSize)
            COMPlusThrowOM();

        // The packingSize acts as a ceiling on all individual alignment
        // requirements so it follows that the largest alignment requirement
        // is also capped.
        _ASSERTE(LargestAlignmentRequirement <= packingSize);

        *pSizeOut = calcTotalSize;
        *pLargestAlignmentRequirementOut = LargestAlignmentRequirement;
    }

    //=======================================================================
    // This function returns TRUE if the provided corElemType disqualifies
    // the structure from being a managed-sequential structure.
    // The fsig parameter is used when the corElemType doesn't contain enough information
    // to successfully determine if this field disqualifies the type from being
    // managed-sequential.
    // This function also fills in the pManagedPlacementInfo structure for this field.
    //=======================================================================
    BOOL CheckIfDisqualifiedFromManagedSequential(CorElementType corElemType, MetaSig& fsig, RawFieldPlacementInfo* pManagedPlacementInfo)
    {
        pManagedPlacementInfo->m_alignment = TARGET_POINTER_SIZE;
        pManagedPlacementInfo->m_size = TARGET_POINTER_SIZE;
        // This type may qualify for ManagedSequential. Collect managed size and alignment info.
        if (CorTypeInfo::IsPrimitiveType(corElemType))
        {
            // Safe cast - no primitive type is larger than 4gb!
            pManagedPlacementInfo->m_size = ((UINT32)CorTypeInfo::Size(corElemType));
#if defined(TARGET_X86) && defined(UNIX_X86_ABI)
            switch (corElemType)
            {
                // The System V ABI for i386 defines different packing for these types.
            case ELEMENT_TYPE_I8:
            case ELEMENT_TYPE_U8:
            case ELEMENT_TYPE_R8:
            {
                pManagedPlacementInfo->m_alignment = 4;
                break;
            }

            default:
            {
                pManagedPlacementInfo->m_alignment = pManagedPlacementInfo->m_size;
                break;
            }
            }
#else // TARGET_X86 && UNIX_X86_ABI
            pManagedPlacementInfo->m_alignment = pManagedPlacementInfo->m_size;
#endif

            return FALSE;
        }
        else if (corElemType == ELEMENT_TYPE_PTR)
        {
            pManagedPlacementInfo->m_size = TARGET_POINTER_SIZE;
            pManagedPlacementInfo->m_alignment = TARGET_POINTER_SIZE;

            return FALSE;
        }
        else if (corElemType == ELEMENT_TYPE_VALUETYPE)
        {
            TypeHandle pNestedType = fsig.GetLastTypeHandleThrowing(ClassLoader::LoadTypes,
                CLASS_LOAD_APPROXPARENTS,
                TRUE);

            pManagedPlacementInfo->m_size = (pNestedType.GetMethodTable()->GetNumInstanceFieldBytes());

            if (pNestedType.GetMethodTable()->HasLayout())
            {
                pManagedPlacementInfo->m_alignment = pNestedType.GetMethodTable()->GetLayoutInfo()->m_ManagedLargestAlignmentRequirementOfAllMembers;
            }
            else
            {
                pManagedPlacementInfo->m_alignment = TARGET_POINTER_SIZE;
            }

            return !pNestedType.GetMethodTable()->IsManagedSequential();
        }

        // No other type permitted for ManagedSequential.
        return TRUE;
    }

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
            pFieldInfoArrayOut->m_MD = fd;
            pFieldInfoArrayOut->m_placement.m_offset = (UINT32)-1;
            pFieldInfoArrayOut->m_sequence = 0;

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
        pFieldInfoArrayOut->m_MD = mdFieldDefNil;
    }

    void DetermineBlittabilityAndManagedSequential(
        IMDInternalImport* pInternalImport,
        HENUMInternal* phEnumField,
        Module* pModule,
        ParseNativeTypeFlags nativeTypeFlags,
        const SigTypeContext* pTypeContext,
        BOOL* fDisqualifyFromManagedSequential,
        LayoutRawFieldInfo* pFieldInfoArrayOut,
        BOOL* pIsBlittableOut,
        ULONG* cInstanceFields
    #ifdef _DEBUG
        ,
        const ULONG cTotalFields,
        LPCUTF8 szNamespace,
        LPCUTF8 szName
    #endif
    )
    {
        HRESULT hr;
        mdFieldDef fd;
        ULONG maxRid = pInternalImport->GetCountWithTokenKind(mdtFieldDef);
        *pIsBlittableOut = TRUE; // Assume is blittable until proven otherwise.

        ULONG i;
        for (i = 0; pInternalImport->EnumNext(phEnumField, &fd); i++)
        {
            DWORD dwFieldAttrs;
            ULONG rid = RidFromToken(fd);

            if ((rid == 0) || (rid > maxRid))
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
                    {
                        cbNativeType = 0;
                    }
                }
                else
                {
                    cbNativeType = 0;
                }

                IfFailThrow(pInternalImport->GetSigOfFieldDef(fd, &cbCOMSignature, &pCOMSignature));

                IfFailThrow(::validateTokenSig(fd, pCOMSignature, cbCOMSignature, dwFieldAttrs, pInternalImport));

                // fill the appropriate entry in pInfoArray
                pFieldInfoArrayOut->m_MD = fd;
                pFieldInfoArrayOut->m_sequence = 0;

    #ifdef _DEBUG
                LPCUTF8 szFieldName;
                if (FAILED(pInternalImport->GetNameOfFieldDef(fd, &szFieldName)))
                {
                    szFieldName = "Invalid FieldDef record";
                }
    #endif
                MetaSig fsig(pCOMSignature, cbCOMSignature, pModule, pTypeContext, MetaSig::sigField);
                CorElementType corElemType = fsig.NextArgNormalized();
                *fDisqualifyFromManagedSequential |= CheckIfDisqualifiedFromManagedSequential(corElemType, fsig, &pFieldInfoArrayOut->m_placement);

                if (!IsFieldBlittable(pModule, fd, fsig.GetArgProps(), pTypeContext, nativeTypeFlags))
                    *pIsBlittableOut = FALSE;

                (*cInstanceFields)++;
                pFieldInfoArrayOut++;
            }
        }

        _ASSERTE(i == cTotalFields);
        // NULL out the last entry
        pFieldInfoArrayOut->m_MD = mdFieldDefNil;
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
}

//=======================================================================
// Called from the clsloader to load up and summarize the field metadata
// for layout classes.
//
// Warning: This function can load other classes (esp. for nested structs.)
//=======================================================================
VOID EEClassLayoutInfo::CollectLayoutFieldMetadataThrowing(
   mdTypeDef      cl,               // cl of the NStruct being loaded
   BYTE           packingSize,      // packing size (from @dll.struct)
   BYTE           nlType,           // nltype (from @dll.struct)
   BOOL           fExplicitOffsets, // explicit offsets?
   MethodTable   *pParentMT,        // the loaded superclass
   ULONG          cTotalFields,         // total number of fields (instance and static)
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
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pModule));
    }
    CONTRACTL_END;

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

    // Running tote - if anything in this type disqualifies it from being ManagedSequential, somebody will set this to TRUE by the the time
    // function exits.
    BOOL fDisqualifyFromManagedSequential;

    // Check if this type might be ManagedSequential. Only valuetypes marked Sequential can be
    // ManagedSequential. Other issues checked below might also disqualify the type.
    if ( (!fExplicitOffsets) &&    // Is it marked sequential?
         (pParentMT && (pParentMT->IsValueTypeClass() || pParentMT->IsManagedSequential()))  // Is it a valuetype or derived from a qualifying valuetype?
       )
    {
        fDisqualifyFromManagedSequential = FALSE;
    }
    else
    {
        fDisqualifyFromManagedSequential = TRUE;
    }


    BOOL fHasNonTrivialParent = pParentMT &&
                                !pParentMT->IsObjectClass() &&
                                !pParentMT->IsValueTypeClass();


    // Set some defaults based on the parent type of this type (if one exists).
    _ASSERTE(!(fHasNonTrivialParent && !(pParentMT->HasLayout())));

    pEEClassLayoutInfoOut->SetIsZeroSized(FALSE);
    pEEClassLayoutInfoOut->SetHasExplicitSize(FALSE);
    pEEClassLayoutInfoOut->m_cbPackingSize = packingSize;

    BOOL fParentHasLayout = pParentMT && pParentMT->HasLayout();
    UINT32 cbAdjustedParentLayoutSize = 0;
    EEClassLayoutInfo *pParentLayoutInfo = NULL;
    if (fParentHasLayout)
    {
        pParentLayoutInfo = pParentMT->GetLayoutInfo();
        // Treat base class as an initial member.
        // If the parent was originally a zero-sized explicit type but
        // got bumped up to a size of 1 for compatibility reasons, then
        // we need to remove the padding, but ONLY for inheritance situations.
        if (pParentLayoutInfo->IsZeroSized()) {
            cbAdjustedParentLayoutSize = 0;
        }
        else
        {
            cbAdjustedParentLayoutSize = pParentMT->GetNumInstanceFieldBytes();
        }
    }

    ULONG cInstanceFields = 0;

    ParseNativeTypeFlags nativeTypeFlags = ParseNativeTypeFlags::None;
    if (nlType == nltAnsi)
        nativeTypeFlags = ParseNativeTypeFlags::IsAnsi;

    BOOL isBlittable;

    DetermineBlittabilityAndManagedSequential(
        pInternalImport,
        phEnumField,
        pModule,
        nativeTypeFlags,
        pTypeContext,
        &fDisqualifyFromManagedSequential,
        pInfoArrayOut,
        &isBlittable,
        &cInstanceFields
        DEBUGARG(cTotalFields)
        DEBUGARG(szNamespace)
        DEBUGARG(szName)
        );

    // Type is blittable only if parent is also blittable
    isBlittable = isBlittable && (fHasNonTrivialParent ? pParentMT->IsBlittable() : TRUE);
    pEEClassLayoutInfoOut->SetIsBlittable(isBlittable);

    S_UINT32 cbSortArraySize = S_UINT32(cTotalFields) * S_UINT32(sizeof(LayoutRawFieldInfo*));
    if (cbSortArraySize.IsOverflow())
    {
        ThrowHR(COR_E_TYPELOAD);
    }
    CQuickArray<LayoutRawFieldInfo*> pSortArray;
    pSortArray.ReSizeThrows(cbSortArraySize.Value());
    SetOffsetsAndSortFields(pInternalImport, cl, pInfoArrayOut, cInstanceFields, fExplicitOffsets, cbAdjustedParentLayoutSize, pModule, pSortArray.Ptr());

    ULONG classSizeInMetadata = 0;
    if (FAILED(pInternalImport->GetClassTotalSize(cl, &classSizeInMetadata)))
    {
        classSizeInMetadata = 0;
    }
    else
    {
        // If we can get the class size from metadata, that means that the user
        // explicitly provided a value to the StructLayoutAttribute.Size field
        // or explicitly provided the size in IL.
        pEEClassLayoutInfoOut->SetHasExplicitSize(TRUE);
    }

    BYTE parentAlignmentRequirement = 0;
    if (fParentHasLayout)
    {
        parentAlignmentRequirement = pParentLayoutInfo->m_ManagedLargestAlignmentRequirementOfAllMembers;
    }

    BYTE parentManagedAlignmentRequirement = 0;
    if (pParentMT && (pParentMT->IsManagedSequential() || (pParentMT->GetClass()->HasExplicitFieldOffsetLayout() && pParentMT->IsBlittable())))
    {
        parentManagedAlignmentRequirement = pParentLayoutInfo->m_ManagedLargestAlignmentRequirementOfAllMembers;
    }

    CalculateSizeAndFieldOffsets(
        cbAdjustedParentLayoutSize,
        cInstanceFields,
        fExplicitOffsets,
        pSortArray.Ptr(),
        classSizeInMetadata,
        packingSize,
        parentManagedAlignmentRequirement,
        /*limitToMaxInteropSize*/ FALSE,
        &pEEClassLayoutInfoOut->m_ManagedLargestAlignmentRequirementOfAllMembers,
        &pEEClassLayoutInfoOut->m_cbManagedSize);

    if (pEEClassLayoutInfoOut->m_cbManagedSize == 0)
    {
        pEEClassLayoutInfoOut->SetIsZeroSized(TRUE);
        pEEClassLayoutInfoOut->m_cbManagedSize = 1; // Bump the managed size of the structure up to 1.
    }

    pEEClassLayoutInfoOut->SetIsManagedSequential(!fDisqualifyFromManagedSequential);
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
        ListLockHolder nativeTypeLoadLock(pMT->GetDomain()->GetNativeTypeLoadLock());
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

    BOOL fHasNonTrivialParent = pParentMT &&
        !pParentMT->IsObjectClass() &&
        !pParentMT->IsValueTypeClass();

    // Set some defaults based on the parent type of this type (if one exists).
    _ASSERTE(!(fHasNonTrivialParent && !(pParentMT->HasLayout())));

    BOOL fParentHasLayout = pParentMT && pParentMT->HasLayout();
    UINT32 cbAdjustedParentLayoutNativeSize = 0;
    EEClassNativeLayoutInfo const* pParentLayoutInfo = NULL;
    if (fParentHasLayout)
    {
        pParentLayoutInfo = pParentMT->GetNativeLayoutInfo();
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

    ParseNativeTypeFlags nativeTypeFlags = ParseNativeTypeFlags::None;
    if (charSet == nltAnsi)
        nativeTypeFlags = ParseNativeTypeFlags::IsAnsi;

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

    // Now compute the native size of each field
    for (LayoutRawFieldInfo* pfwalk = pInfoArray; pfwalk->m_MD != mdFieldDefNil; pfwalk++)
    {
        pfwalk->m_placement.m_size = pfwalk->m_nfd.NativeSize();
        pfwalk->m_placement.m_alignment = pfwalk->m_nfd.AlignmentRequirement();
    }

    S_UINT32 cbSortArraySize = S_UINT32(cInstanceFields) * S_UINT32(sizeof(LayoutRawFieldInfo*));
    if (cbSortArraySize.IsOverflow())
    {
        ThrowHR(COR_E_TYPELOAD);
    }

    BOOL fExplicitOffsets = pMT->GetClass()->HasExplicitFieldOffsetLayout();

    CQuickArray<LayoutRawFieldInfo*> pSortArray;
    pSortArray.ReSizeThrows(cbSortArraySize.Value());
    SetOffsetsAndSortFields(pInternalImport, pMT->GetCl(), pInfoArray, cInstanceFields, fExplicitOffsets, cbAdjustedParentLayoutNativeSize, pModule, pSortArray.Ptr());

    EEClassLayoutInfo* pEEClassLayoutInfo = pMT->GetLayoutInfo();

    ULONG classSizeInMetadata = 0;
    if (pEEClassLayoutInfo->HasExplicitSize())
    {
        HRESULT hr = pInternalImport->GetClassTotalSize(pMT->GetCl(), &classSizeInMetadata);

        CONSISTENCY_CHECK(hr == S_OK);
    }

    BYTE parentAlignmentRequirement = 0;
    if (fParentHasLayout)
    {
        parentAlignmentRequirement = pParentLayoutInfo->GetLargestAlignmentRequirement();
    }

    CalculateSizeAndFieldOffsets(
        cbAdjustedParentLayoutNativeSize,
        cInstanceFields,
        fExplicitOffsets,
        pSortArray.Ptr(),
        classSizeInMetadata,
        pMT->GetLayoutInfo()->GetPackingSize(),
        parentAlignmentRequirement,
        /*limitToMaxInteropSize*/ TRUE,
        &pNativeLayoutInfo->m_alignmentRequirement,
        &pNativeLayoutInfo->m_size);

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
        // Crossgen scenarios block Vector<T> from even being loaded, so only do this check when not in crossgen.
#ifndef CROSSGEN_COMPILE
        if (pMT->HasSameTypeDefAs(CoreLibBinder::GetClass(CLASS__VECTORT)))
        {
            pNativeLayoutInfo->m_size = pEEClassLayoutInfo->GetManagedSize();
            pNativeLayoutInfo->m_alignmentRequirement = pEEClassLayoutInfo->m_ManagedLargestAlignmentRequirementOfAllMembers;
        }
        else
#endif
        if (pMT->HasSameTypeDefAs(CoreLibBinder::GetClass(CLASS__VECTOR64T)) ||
            pMT->HasSameTypeDefAs(CoreLibBinder::GetClass(CLASS__VECTOR128T)) ||
            pMT->HasSameTypeDefAs(CoreLibBinder::GetClass(CLASS__VECTOR256T)))
        {
            pNativeLayoutInfo->m_alignmentRequirement = pEEClassLayoutInfo->m_ManagedLargestAlignmentRequirementOfAllMembers;
        }
    }

    PTR_NativeFieldDescriptor pNativeFieldDescriptors = pNativeLayoutInfo->GetNativeFieldDescriptors();

    // Bring in the parent's fieldmarshalers
    if (fHasNonTrivialParent)
    {
        CONSISTENCY_CHECK(fParentHasLayout);
        PREFAST_ASSUME(pParentLayoutInfo != NULL);  // See if (fParentHasLayout) branch above

        UINT numChildCTMFields = cInstanceFields;

        NativeFieldDescriptor const* pParentCTMFieldSrcArray = pParentLayoutInfo->GetNativeFieldDescriptors();
        NativeFieldDescriptor* pParentCTMFieldDestArray = pNativeFieldDescriptors + numChildCTMFields;

        for (UINT parentCTMFieldIndex = 0; parentCTMFieldIndex < pParentLayoutInfo->GetNumFields(); parentCTMFieldIndex++)
        {
            pParentCTMFieldDestArray[parentCTMFieldIndex] = pParentCTMFieldSrcArray[parentCTMFieldIndex];
        }
    }

    for (UINT i = 0; i < cInstanceFields; i++)
    {
        pInfoArray[i].m_nfd.SetExternalOffset(pInfoArray[i].m_placement.m_offset);
        pNativeFieldDescriptors[i] = pInfoArray[i].m_nfd;
    }

    bool isMarshalable = true;
    for (UINT i = 0; i < numTotalInstanceFields; i++)
    {
        if (pNativeFieldDescriptors[i].IsUnmarshalable())
        {
            isMarshalable = false;
            break;
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
                _ASSERTE(pNativeFieldDescriptors[i].GetExternalOffset() == pNativeFieldDescriptors[i].GetFieldDesc()->GetOffset_NoLogging());
                _ASSERTE(pNativeFieldDescriptors[i].NativeSize() == pNativeFieldDescriptors[i].GetFieldDesc()->GetSize());
            }
            _ASSERTE(pNativeLayoutInfo->GetSize() == pEEClassLayoutInfo->GetManagedSize());
            _ASSERTE(pNativeLayoutInfo->GetLargestAlignmentRequirement() == pEEClassLayoutInfo->m_ManagedLargestAlignmentRequirementOfAllMembers);
        }

        LOG((LF_INTEROP, LL_INFO100000, "\n\n"));
        LOG((LF_INTEROP, LL_INFO100000, "%s.%s\n", szNamespace, szName));
        LOG((LF_INTEROP, LL_INFO100000, "Packsize      = %lu\n", (ULONG)pEEClassLayoutInfo->GetPackingSize()));
        LOG((LF_INTEROP, LL_INFO100000, "Max align req = %lu\n", (ULONG)(pNativeLayoutInfo->GetLargestAlignmentRequirement())));
        LOG((LF_INTEROP, LL_INFO100000, "----------------------------\n"));
        for (LayoutRawFieldInfo* pfwalk = pInfoArray; pfwalk->m_MD != mdFieldDefNil; pfwalk++)
        {
            LPCUTF8 fieldname;
            if (FAILED(pInternalImport->GetNameOfFieldDef(pfwalk->m_MD, &fieldname)))
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
