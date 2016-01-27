// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// precode.cpp
//

//
// Stub that runs before the actual native code
//


#include "common.h"

#ifdef FEATURE_PREJIT
#include "compile.h"
#endif

//==========================================================================================
// class Precode
//==========================================================================================
BOOL Precode::IsValidType(PrecodeType t)
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    switch (t) {
    case PRECODE_STUB:
#ifdef HAS_NDIRECT_IMPORT_PRECODE
    case PRECODE_NDIRECT_IMPORT:
#endif // HAS_NDIRECT_IMPORT_PRECODE
#ifdef HAS_REMOTING_PRECODE
    case PRECODE_REMOTING:
#endif // HAS_REMOTING_PRECODE
#ifdef HAS_FIXUP_PRECODE
    case PRECODE_FIXUP:
#endif // HAS_FIXUP_PRECODE
#ifdef HAS_THISPTR_RETBUF_PRECODE
    case PRECODE_THISPTR_RETBUF:
#endif // HAS_THISPTR_RETBUF_PRECODE
        return TRUE;
    default:
        return FALSE;
    }
}

SIZE_T Precode::SizeOf(PrecodeType t)
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    switch (t)
    {
    case PRECODE_STUB:
        return sizeof(StubPrecode);
#ifdef HAS_NDIRECT_IMPORT_PRECODE
    case PRECODE_NDIRECT_IMPORT:
        return sizeof(NDirectImportPrecode);
#endif // HAS_NDIRECT_IMPORT_PRECODE
#ifdef HAS_REMOTING_PRECODE
    case PRECODE_REMOTING:
        return sizeof(RemotingPrecode);
#endif // HAS_REMOTING_PRECODE
#ifdef HAS_FIXUP_PRECODE
    case PRECODE_FIXUP:
        return sizeof(FixupPrecode);
#endif // HAS_FIXUP_PRECODE
#ifdef HAS_THISPTR_RETBUF_PRECODE
    case PRECODE_THISPTR_RETBUF:
        return sizeof(ThisPtrRetBufPrecode);
#endif // HAS_THISPTR_RETBUF_PRECODE

    default:
        UnexpectedPrecodeType("Precode::SizeOf", t);
        break;
    }
    return 0;
}

// Note: This is immediate target of the precode. It does not follow jump stub if there is one.
PCODE Precode::GetTarget()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    PCODE target = NULL;

    PrecodeType precodeType = GetType();
    switch (precodeType)
    {
    case PRECODE_STUB:
        target = AsStubPrecode()->GetTarget();
        break;
#ifdef HAS_REMOTING_PRECODE
    case PRECODE_REMOTING:
        target = AsRemotingPrecode()->GetTarget();
        break;
#endif // HAS_REMOTING_PRECODE
#ifdef HAS_FIXUP_PRECODE
    case PRECODE_FIXUP:
        target = AsFixupPrecode()->GetTarget();
        break;
#endif // HAS_FIXUP_PRECODE
#ifdef HAS_THISPTR_RETBUF_PRECODE
    case PRECODE_THISPTR_RETBUF:
        target = AsThisPtrRetBufPrecode()->GetTarget();
        break;
#endif // HAS_THISPTR_RETBUF_PRECODE

    default:
        UnexpectedPrecodeType("Precode::GetTarget", precodeType);
        break;
    }
    return target;
}

MethodDesc* Precode::GetMethodDesc(BOOL fSpeculative /*= FALSE*/)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    TADDR pMD = NULL;

    PrecodeType precodeType = GetType();
    switch (precodeType)
    {
    case PRECODE_STUB:
        pMD = AsStubPrecode()->GetMethodDesc();
        break;
#ifdef HAS_NDIRECT_IMPORT_PRECODE
    case PRECODE_NDIRECT_IMPORT:
        pMD = AsNDirectImportPrecode()->GetMethodDesc();
        break;
#endif // HAS_NDIRECT_IMPORT_PRECODE
#ifdef HAS_REMOTING_PRECODE
    case PRECODE_REMOTING:
        pMD = AsRemotingPrecode()->GetMethodDesc();
        break;
#endif // HAS_REMOTING_PRECODE
#ifdef HAS_FIXUP_PRECODE
    case PRECODE_FIXUP:
        pMD = AsFixupPrecode()->GetMethodDesc();
        break;
#endif // HAS_FIXUP_PRECODE
#ifdef HAS_THISPTR_RETBUF_PRECODE
    case PRECODE_THISPTR_RETBUF:
        pMD = AsThisPtrRetBufPrecode()->GetMethodDesc();
        break;
#endif // HAS_THISPTR_RETBUF_PRECODE

    default:
        break;
    }

    if (pMD == NULL)
    {
        if (fSpeculative)
            return NULL;
        else
            UnexpectedPrecodeType("Precode::GetMethodDesc", precodeType);
    }

    // GetMethodDesc() on platform specific precode types returns TADDR. It should return 
    // PTR_MethodDesc instead. It is a workaround to resolve cyclic dependency between headers. 
    // Once we headers factoring of headers cleaned up, we should be able to get rid of it.

    // For speculative calls, pMD can be garbage that causes IBC logging to crash
    if (!fSpeculative)
        g_IBCLogger.LogMethodPrecodeAccess((PTR_MethodDesc)pMD);

    return (PTR_MethodDesc)pMD;
}

BOOL Precode::IsCorrectMethodDesc(MethodDesc *  pMD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;
    MethodDesc * pMDfromPrecode = GetMethodDesc(TRUE);

    if (pMDfromPrecode == pMD)
        return TRUE;

#ifdef HAS_FIXUP_PRECODE_CHUNKS
    if (pMDfromPrecode == NULL)
    {
        PrecodeType precodeType = GetType();

#ifdef HAS_FIXUP_PRECODE_CHUNKS
        // We do not keep track of the MethodDesc in every kind of fixup precode
        if (precodeType == PRECODE_FIXUP)
            return TRUE;
#endif
    }
#endif // HAS_FIXUP_PRECODE_CHUNKS

    return FALSE;
}

BOOL Precode::IsPointingToPrestub(PCODE target)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (IsPointingTo(target, GetPreStubEntryPoint()))
        return TRUE;

#ifdef HAS_FIXUP_PRECODE
    if (IsPointingTo(target, GetEEFuncEntryPoint(PrecodeFixupThunk)))
        return TRUE;
#endif

#ifdef FEATURE_PREJIT
    Module *pZapModule = GetMethodDesc()->GetZapModule();
    if (pZapModule != NULL)
    {
        if (IsPointingTo(target, pZapModule->GetPrestubJumpStub()))
            return TRUE;

#ifdef HAS_FIXUP_PRECODE
        if (IsPointingTo(target, pZapModule->GetPrecodeFixupJumpStub()))
            return TRUE;
#endif
    }
#endif // FEATURE_PREJIT

    return FALSE;
}

// If addr is patched fixup precode, returns address that it points to. Otherwise returns NULL.
PCODE Precode::TryToSkipFixupPrecode(PCODE addr)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    } CONTRACTL_END;

    PCODE pTarget = NULL;

#if defined(FEATURE_PREJIT) && defined(HAS_FIXUP_PRECODE)
    // Early out for common cases
    if (!FixupPrecode::IsFixupPrecodeByASM(addr))
        return NULL;

    // This optimization makes sense in NGened code only.
    Module * pModule = ExecutionManager::FindZapModule(addr);
    if (pModule == NULL)
        return NULL;

    // Verify that the address is in precode section
    if (!pModule->IsZappedPrecode(addr))
        return NULL;

    pTarget = GetPrecodeFromEntryPoint(addr)->GetTarget();

    // Verify that the target is in code section
    if (!pModule->IsZappedCode(pTarget))
        return NULL;

#if defined(_DEBUG)
    MethodDesc * pMD_orig   = MethodTable::GetMethodDescForSlotAddress(addr);
    MethodDesc * pMD_direct = MethodTable::GetMethodDescForSlotAddress(pTarget);

    // Both the original and direct entrypoint should map to same MethodDesc
    // Some FCalls are remapped to private methods (see System.String.CtorCharArrayStartLength)
    _ASSERTE((pMD_orig == pMD_direct) || pMD_orig->IsRuntimeSupplied());
#endif

#endif // defined(FEATURE_PREJIT) && defined(HAS_FIXUP_PRECODE)

    return pTarget;
}

Precode* Precode::GetPrecodeForTemporaryEntryPoint(TADDR temporaryEntryPoints, int index)
{
    WRAPPER_NO_CONTRACT;
    PrecodeType t = PTR_Precode(temporaryEntryPoints)->GetType();
#ifdef HAS_FIXUP_PRECODE_CHUNKS
    if (t == PRECODE_FIXUP)
    {
        return PTR_Precode(temporaryEntryPoints + index * sizeof(FixupPrecode));
    }
#endif
    SIZE_T oneSize = SizeOfTemporaryEntryPoint(t);
    return PTR_Precode(temporaryEntryPoints + index * oneSize);
}

SIZE_T Precode::SizeOfTemporaryEntryPoints(PrecodeType t, int count)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;
#ifdef HAS_FIXUP_PRECODE_CHUNKS
    if (t == PRECODE_FIXUP)
    {
        return count * (sizeof(FixupPrecode) + sizeof(PCODE)) + sizeof(PTR_MethodDesc);
    }
#endif
    SIZE_T oneSize = SizeOfTemporaryEntryPoint(t);
    return count * oneSize;
}

#ifndef DACCESS_COMPILE

Precode* Precode::Allocate(PrecodeType t, MethodDesc* pMD,
                           LoaderAllocator *  pLoaderAllocator, 
                           AllocMemTracker *  pamTracker)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    SIZE_T size;

#ifdef HAS_FIXUP_PRECODE_CHUNKS
    if (t == PRECODE_FIXUP)
    {
        size = sizeof(FixupPrecode) + sizeof(PTR_MethodDesc);
    }
    else
#endif
    {
        size = Precode::SizeOf(t);
    }

    Precode* pPrecode = (Precode*)pamTracker->Track(pLoaderAllocator->GetPrecodeHeap()->AllocAlignedMem(size, AlignOf(t)));
    pPrecode->Init(t, pMD, pLoaderAllocator);

#ifndef CROSSGEN_COMPILE
    ClrFlushInstructionCache(pPrecode, size);
#endif

    return pPrecode;
}

void Precode::Init(PrecodeType t, MethodDesc* pMD, LoaderAllocator *pLoaderAllocator)
{
    LIMITED_METHOD_CONTRACT;

    switch (t) {
    case PRECODE_STUB:
        ((StubPrecode*)this)->Init(pMD, pLoaderAllocator);
        break;
#ifdef HAS_NDIRECT_IMPORT_PRECODE
    case PRECODE_NDIRECT_IMPORT:
        ((NDirectImportPrecode*)this)->Init(pMD, pLoaderAllocator);
        break;
#endif // HAS_NDIRECT_IMPORT_PRECODE
#ifdef HAS_REMOTING_PRECODE
    case PRECODE_REMOTING:
        ((RemotingPrecode*)this)->Init(pMD, pLoaderAllocator);
        break;
#endif // HAS_REMOTING_PRECODE
#ifdef HAS_FIXUP_PRECODE
    case PRECODE_FIXUP:
        ((FixupPrecode*)this)->Init(pMD, pLoaderAllocator);
        break;
#endif // HAS_FIXUP_PRECODE
#ifdef HAS_THISPTR_RETBUF_PRECODE
    case PRECODE_THISPTR_RETBUF:
        ((ThisPtrRetBufPrecode*)this)->Init(pMD, pLoaderAllocator);
        break;
#endif // HAS_THISPTR_RETBUF_PRECODE
    default:
        UnexpectedPrecodeType("Precode::Init", t);
        break;
    }

    _ASSERTE(IsValidType(GetType()));
}

BOOL Precode::SetTargetInterlocked(PCODE target)
{
    WRAPPER_NO_CONTRACT;

    PCODE expected = GetTarget();
    BOOL ret = FALSE;

    if (!IsPointingToPrestub(expected))
        return FALSE;

    g_IBCLogger.LogMethodPrecodeWriteAccess(GetMethodDesc());

    PrecodeType precodeType = GetType();
    switch (precodeType)
    {
    case PRECODE_STUB:
        ret = AsStubPrecode()->SetTargetInterlocked(target, expected);
        break;

#ifdef HAS_REMOTING_PRECODE
    case PRECODE_REMOTING:
        ret = AsRemotingPrecode()->SetTargetInterlocked(target, expected);
        break;
#endif // HAS_REMOTING_PRECODE

#ifdef HAS_FIXUP_PRECODE
    case PRECODE_FIXUP:
        ret = AsFixupPrecode()->SetTargetInterlocked(target, expected);
        break;
#endif // HAS_FIXUP_PRECODE

#ifdef HAS_THISPTR_RETBUF_PRECODE
    case PRECODE_THISPTR_RETBUF:
        ret = AsThisPtrRetBufPrecode()->SetTargetInterlocked(target, expected);
        break;
#endif // HAS_THISPTR_RETBUF_PRECODE

    default:
        UnexpectedPrecodeType("Precode::SetTargetInterlocked", precodeType);
        break;
    }

    //
    // SetTargetInterlocked does not modify code on ARM so the flush instruction cache is
    // not necessary.
    //
#if !defined(_TARGET_ARM_)
    if (ret) {
        FlushInstructionCache(GetCurrentProcess(),this,SizeOf());
    }
#endif

    _ASSERTE(!IsPointingToPrestub());
    return ret;
}

void Precode::Reset()
{
    WRAPPER_NO_CONTRACT;

    MethodDesc* pMD = GetMethodDesc();
    Init(GetType(), pMD, pMD->GetLoaderAllocatorForCode());
    FlushInstructionCache(GetCurrentProcess(),this,SizeOf());
}

/* static */ 
TADDR Precode::AllocateTemporaryEntryPoints(MethodDescChunk *  pChunk,
                                            LoaderAllocator *  pLoaderAllocator, 
                                            AllocMemTracker *  pamTracker)
{
    WRAPPER_NO_CONTRACT;

    MethodDesc* pFirstMD = pChunk->GetFirstMethodDesc();

    int count = pChunk->GetCount();

    PrecodeType t = PRECODE_STUB;

#ifdef HAS_FIXUP_PRECODE
    // Default to faster fixup precode if possible
    if (!pFirstMD->RequiresMethodDescCallingConvention(count > 1))
    {
        t = PRECODE_FIXUP;
    }
#endif // HAS_FIXUP_PRECODE

    SIZE_T totalSize = SizeOfTemporaryEntryPoints(t, count);

#ifdef HAS_COMPACT_ENTRYPOINTS
    // Note that these are just best guesses to save memory. If we guessed wrong,
    // we will allocate a new exact type of precode in GetOrCreatePrecode.
    BOOL fForcedPrecode = pFirstMD->RequiresStableEntryPoint(count > 1);
    if (!fForcedPrecode && (totalSize > MethodDescChunk::SizeOfCompactEntryPoints(count)))
        return NULL;
#endif

    TADDR temporaryEntryPoints = (TADDR)pamTracker->Track(pLoaderAllocator->GetPrecodeHeap()->AllocAlignedMem(totalSize, AlignOf(t)));

#ifdef HAS_FIXUP_PRECODE_CHUNKS
    if (t == PRECODE_FIXUP)
    {
        TADDR entryPoint = temporaryEntryPoints;
        MethodDesc * pMD = pChunk->GetFirstMethodDesc();
        for (int i = 0; i < count; i++)
        {
            ((FixupPrecode *)entryPoint)->Init(pMD, pLoaderAllocator, pMD->GetMethodDescIndex(), (count - 1) - i);

            _ASSERTE((Precode *)entryPoint == GetPrecodeForTemporaryEntryPoint(temporaryEntryPoints, i));
            entryPoint += sizeof(FixupPrecode);
    
            pMD = (MethodDesc *)(dac_cast<TADDR>(pMD) + pMD->SizeOf());
        }

        ClrFlushInstructionCache((LPVOID)temporaryEntryPoints, count * sizeof(FixupPrecode));

        return temporaryEntryPoints;
    }
#endif

    SIZE_T oneSize = SizeOfTemporaryEntryPoint(t);
    TADDR entryPoint = temporaryEntryPoints;
    MethodDesc * pMD = pChunk->GetFirstMethodDesc();
    for (int i = 0; i < count; i++)
    {
        ((Precode *)entryPoint)->Init(t, pMD, pLoaderAllocator);

        _ASSERTE((Precode *)entryPoint == GetPrecodeForTemporaryEntryPoint(temporaryEntryPoints, i));
        entryPoint += oneSize;

        pMD = (MethodDesc *)(dac_cast<TADDR>(pMD) + pMD->SizeOf());
    }

    ClrFlushInstructionCache((LPVOID)temporaryEntryPoints, count * oneSize);

    return temporaryEntryPoints;
}

#ifdef FEATURE_NATIVE_IMAGE_GENERATION

static DataImage::ItemKind GetPrecodeItemKind(DataImage * image, MethodDesc * pMD, BOOL fIsPrebound = FALSE)
{
    STANDARD_VM_CONTRACT;

    DataImage::ItemKind kind = DataImage::ITEM_METHOD_PRECODE_COLD_WRITEABLE;

    DWORD flags = image->GetMethodProfilingFlags(pMD);

    if (flags & (1 << WriteMethodPrecode))
    {
        kind = fIsPrebound ? DataImage::ITEM_METHOD_PRECODE_HOT : DataImage::ITEM_METHOD_PRECODE_HOT_WRITEABLE;
    }
    else
    if (flags & (1 << ReadMethodPrecode))
    {
        kind = DataImage::ITEM_METHOD_PRECODE_HOT;
    }
    else
    if (
        fIsPrebound ||
        // The generic method definitions get precode to make GetMethodDescForSlot work.
        // This precode should not be ever written to.
        pMD->ContainsGenericVariables() ||
        // Interface MDs are run only for remoting and cominterop which is pretty rare. Make them cold.
        pMD->IsInterface()
        )
    {
        kind = DataImage::ITEM_METHOD_PRECODE_COLD;
    }

    return kind;
}

void Precode::Save(DataImage *image)
{
    STANDARD_VM_CONTRACT;

    MethodDesc * pMD = GetMethodDesc();
    PrecodeType t = GetType();

#ifdef HAS_FIXUP_PRECODE_CHUNKS
    _ASSERTE(GetType() != PRECODE_FIXUP);
#endif

#if defined(_TARGET_X86_) || defined(_TARGET_AMD64_)
    // StubPrecode and RemotingPrecode may have straddlers (relocations crossing pages) on x86 and x64. We need 
    // to insert padding to eliminate it. To do that, we need to save these using custom ZapNode that can only
    // be implemented in dataimage.cpp or zapper due to factoring of the header files.
    BOOL fIsPrebound = IsPrebound(image);
    image->SavePrecode(this, 
        pMD, 
        t,  
        GetPrecodeItemKind(image, pMD, fIsPrebound),
        fIsPrebound);
#else
    _ASSERTE(FitsIn<ULONG>(SizeOf(t)));
    image->StoreStructure((void*)GetStart(), 
        static_cast<ULONG>(SizeOf(t)), 
        GetPrecodeItemKind(image, pMD, IsPrebound(image)),
        AlignOf(t));
#endif // _TARGET_X86_ || _TARGET_AMD64_
}

void Precode::Fixup(DataImage *image, MethodDesc * pMD)
{
    STANDARD_VM_CONTRACT;

    PrecodeType precodeType = GetType();

#if defined(_TARGET_X86_) || defined(_TARGET_AMD64_)
#if defined(HAS_FIXUP_PRECODE)
    if (precodeType == PRECODE_FIXUP)
    {
        AsFixupPrecode()->Fixup(image, pMD);
    }
#endif
#else // _TARGET_X86_ || _TARGET_AMD64_
    ZapNode * pCodeNode = NULL;

    if (IsPrebound(image))
    {
        pCodeNode = image->GetCodeAddress(pMD);
    }

    switch (precodeType) {
    case PRECODE_STUB:
        AsStubPrecode()->Fixup(image);
        break;
#ifdef HAS_NDIRECT_IMPORT_PRECODE
    case PRECODE_NDIRECT_IMPORT:
        AsNDirectImportPrecode()->Fixup(image);
        break;
#endif // HAS_NDIRECT_IMPORT_PRECODE
#ifdef HAS_REMOTING_PRECODE
    case PRECODE_REMOTING:
        AsRemotingPrecode()->Fixup(image, pCodeNode);
        break;
#endif // HAS_REMOTING_PRECODE
#ifdef HAS_FIXUP_PRECODE
    case PRECODE_FIXUP:
        AsFixupPrecode()->Fixup(image, pMD);
        break;
#endif // HAS_FIXUP_PRECODE
    default:
        UnexpectedPrecodeType("Precode::Save", precodeType);
        break;
    }
#endif // _TARGET_X86_ || _TARGET_AMD64_
}

BOOL Precode::IsPrebound(DataImage *image)
{
    WRAPPER_NO_CONTRACT;

#ifdef HAS_REMOTING_PRECODE
    // This will make sure that when IBC logging is on, the precode goes thru prestub.
    if (GetAppDomain()->ToCompilationDomain()->m_fForceInstrument)
        return FALSE;

    if (GetType() != PRECODE_REMOTING)
        return FALSE;

    // Prebind the remoting precode if possible
    return image->CanDirectCall(GetMethodDesc(), CORINFO_ACCESS_THIS);
#else
    return FALSE;
#endif
}

void Precode::SaveChunk::Save(DataImage* image, MethodDesc * pMD)
{
    STANDARD_VM_CONTRACT;

    PrecodeType precodeType = pMD->GetPrecodeType();

#ifdef HAS_FIXUP_PRECODE_CHUNKS
    if (precodeType == PRECODE_FIXUP)
    {
        m_rgPendingChunk.Append(pMD);
        return;
    }
#endif // HAS_FIXUP_PRECODE_CHUNKS

    SIZE_T size = Precode::SizeOf(precodeType);
    Precode* pPrecode = (Precode *)new (image->GetHeap()) BYTE[size];
    pPrecode->Init(precodeType, pMD, NULL);
    pPrecode->Save(image);

    // Alias the temporary entrypoint
    image->RegisterSurrogate(pMD, pPrecode);
}

#ifdef HAS_FIXUP_PRECODE_CHUNKS
static void SaveFixupPrecodeChunk(DataImage * image, MethodDesc ** rgMD, COUNT_T count, DataImage::ItemKind kind)
{
    STANDARD_VM_CONTRACT;

    ULONG size = sizeof(FixupPrecode) * count + sizeof(PTR_MethodDesc);
    FixupPrecode * pBase = (FixupPrecode *)new (image->GetHeap()) BYTE[size];

    ZapStoredStructure * pNode = image->StoreStructure(NULL, size, kind,
        Precode::AlignOf(PRECODE_FIXUP));

    for (COUNT_T i = 0; i < count; i++)
    {
        MethodDesc * pMD = rgMD[i];
        FixupPrecode * pPrecode = pBase + i;

        pPrecode->InitForSave((count - 1) - i);

        image->BindPointer(pPrecode, pNode, i * sizeof(FixupPrecode));

        // Alias the temporary entrypoint
        image->RegisterSurrogate(pMD, pPrecode);
    }

    image->CopyData(pNode, pBase, size);
}
#endif // HAS_FIXUP_PRECODE_CHUNKS

void Precode::SaveChunk::Flush(DataImage * image)
{
    STANDARD_VM_CONTRACT;

#ifdef HAS_FIXUP_PRECODE_CHUNKS
    if (m_rgPendingChunk.GetCount() == 0)
        return;

    // Sort MethodDescs using the item kind for hot-cold spliting
    struct SortMethodDesc : CQuickSort< MethodDesc * >
    {
        DataImage * m_image;

        SortMethodDesc(DataImage *image, MethodDesc **pBase, SSIZE_T iCount)
            : CQuickSort< MethodDesc * >(pBase, iCount),
            m_image(image)
        {
        }

        int Compare(MethodDesc ** ppMD1, MethodDesc ** ppMD2)
        {
            MethodDesc * pMD1 = *ppMD1;
            MethodDesc * pMD2 = *ppMD2;

            // Compare item kind
            DataImage::ItemKind kind1 = GetPrecodeItemKind(m_image, pMD1);
            DataImage::ItemKind kind2 = GetPrecodeItemKind(m_image, pMD2);

            return kind1 - kind2;
        }
    };

    SortMethodDesc sort(image, &(m_rgPendingChunk[0]), m_rgPendingChunk.GetCount());
    sort.Sort();

    DataImage::ItemKind pendingKind = DataImage::ITEM_METHOD_PRECODE_COLD_WRITEABLE;
    COUNT_T pendingCount = 0;

    COUNT_T i;
    for (i = 0; i < m_rgPendingChunk.GetCount(); i++)
    {
        MethodDesc * pMD = m_rgPendingChunk[i];

        DataImage::ItemKind kind = GetPrecodeItemKind(image, pMD);
        if (kind != pendingKind)
        {
            if (pendingCount != 0)
                SaveFixupPrecodeChunk(image, &(m_rgPendingChunk[i-pendingCount]), pendingCount, pendingKind);

            pendingKind = kind;
            pendingCount = 0;
        }

        pendingCount++;
    }

    // Flush the remaining items
    SaveFixupPrecodeChunk(image, &(m_rgPendingChunk[i-pendingCount]), pendingCount, pendingKind);
#endif // HAS_FIXUP_PRECODE_CHUNKS
}

#endif // FEATURE_NATIVE_IMAGE_GENERATION

#endif // !DACCESS_COMPILE


#ifdef DACCESS_COMPILE
void Precode::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{   
    SUPPORTS_DAC;
    PrecodeType t = GetType();

#ifdef HAS_FIXUP_PRECODE_CHUNKS
    if (t == PRECODE_FIXUP)
    {
        AsFixupPrecode()->EnumMemoryRegions(flags);
        return;
    }
#endif

    DacEnumMemoryRegion(GetStart(), SizeOf(t));
}
#endif

