// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// ZapInfo.cpp
//

//
// JIT-EE interface for zapping
//
// ======================================================================================

#include "common.h"

#include "zapcode.h"
#include "zapimport.h"
#include "zapwrapper.h"
#include "zapinnerptr.h"
#include "zapmetadata.h"

#ifdef FEATURE_READYTORUN_COMPILER
#include "zapreadytorun.h"
#endif

ZapInfo::ZapInfo(ZapImage * pImage, mdMethodDef md, CORINFO_METHOD_HANDLE handle, CORINFO_MODULE_HANDLE module, unsigned methodProfilingDataFlags)
    : m_pImage(pImage),
    m_currentMethodToken(md),
    m_currentMethodHandle(handle),
    m_currentMethodModule(module),
    m_currentMethodProfilingDataFlags(methodProfilingDataFlags),

    m_pNativeVarInfo(NULL),
    m_iNativeVarInfo(0),
    m_pOffsetMapping(NULL),
    m_iOffsetMapping(0),
    m_pGCInfo(NULL),
    m_cbGCInfo(0),
    m_pCode(NULL),
    m_pColdCode(NULL),
    m_pROData(NULL),
#ifdef FEATURE_EH_FUNCLETS
    // Unwind info of the main method body. It will get merged with GC info.
    m_pMainUnwindInfo(NULL),
    m_cbMainUnwindInfo(0),

    m_pUnwindInfo(NULL),
    m_pUnwindInfoFragments(NULL),
#if defined(TARGET_AMD64)
    m_pChainedColdUnwindInfo(NULL),
#endif
#endif // FEATURE_EH_FUNCLETS
    m_pExceptionInfo(NULL),
    m_pProfileData(NULL),
    m_pProfilingHandle(NULL),

    m_ClassLoadTable(pImage),
    m_MethodLoadTable(pImage)
{
    m_zapper = m_pImage->m_zapper;

    m_pEEJitInfo = m_zapper->m_pEEJitInfo;
    m_pEEJitInfo->setOverride(this, handle);

    m_pEECompileInfo = m_zapper->m_pEECompileInfo;
}

ZapInfo::~ZapInfo()
{
    m_pEEJitInfo->setOverride(NULL, NULL);

    delete [] m_pNativeVarInfo;
    delete [] m_pOffsetMapping;

    delete [] m_pGCInfo;
#ifdef FEATURE_EH_FUNCLETS
    delete [] m_pMainUnwindInfo;
#endif
}

#ifdef ALLOW_SXS_JIT_NGEN
// The AltJit failed and we're going to retry. Forget everything the JIT told us and prepare to JIT again.
void ZapInfo::ResetForJitRetry()
{
    delete [] m_pNativeVarInfo;
    m_pNativeVarInfo = NULL;

    m_iNativeVarInfo = 0;

    delete [] m_pOffsetMapping;
    m_pOffsetMapping = NULL;

    m_iOffsetMapping = 0;

    delete [] m_pGCInfo;
    m_pGCInfo = NULL;

    m_cbGCInfo = 0;

#ifdef FEATURE_EH_FUNCLETS
    delete [] m_pMainUnwindInfo;
    m_pMainUnwindInfo = NULL;

    m_cbMainUnwindInfo = 0;
#endif // FEATURE_EH_FUNCLETS

    // The rest of these pointers are in the ZapWriter's ZapHeap, and will go away when the ZapWriter
    // goes away. That's ok for altjit fallback; we'll use extra memory until the ZapWriter goes away,
    // but we won't write anything to the image. We just zero out the pointers and constants, and we're good.

    m_pCode = NULL;
    m_pColdCode = NULL;
    m_pROData = NULL;

#ifdef FEATURE_EH_FUNCLETS
    m_pUnwindInfoFragments = NULL;
    m_pUnwindInfo = NULL;
#if defined(TARGET_AMD64)
    m_pChainedColdUnwindInfo = NULL;
#endif
#endif // FEATURE_EH_FUNCLETS

    m_pExceptionInfo = NULL;
    m_pProfileData = NULL;
    m_pProfilingHandle = NULL;

    m_ImportSet.RemoveAll();
    m_Imports.Clear();
    m_CodeRelocations.Clear();
}
#endif // ALLOW_SXS_JIT_NGEN

void ZapInfo::InitMethodName()
{
    const char* szClsName;
    const char* szMethodName = m_pEEJitInfo->getMethodName(
                                        m_currentMethodHandle,
                                        &szClsName);

    m_currentMethodName.SetUTF8(szClsName);
    m_currentMethodName.AppendUTF8(NAMESPACE_SEPARATOR_STR);
    m_currentMethodName.AppendUTF8(szMethodName);
}

CORJIT_FLAGS ZapInfo::ComputeJitFlags(CORINFO_METHOD_HANDLE handle)
{
    CORJIT_FLAGS jitFlags = m_zapper->m_pOpt->m_compilerFlags;

    CORJIT_FLAGS flags;
    IfFailThrow(m_pEECompileInfo->GetBaseJitFlags(handle, &flags));
    jitFlags.Add(flags);

    // COMPlus_JitFramed specifies the default fpo setting for jitted and NGened code.
    // You can override the behavior for NGened code using COMPlus_NGenFramed.
    static ConfigDWORD g_NGenFramed;
    DWORD dwNGenFramed = g_NGenFramed.val(CLRConfig::UNSUPPORTED_NGenFramed);
    if (dwNGenFramed == 0)
    {
        // NGened code should enable fpo
        jitFlags.Clear(CORJIT_FLAGS::CORJIT_FLAG_FRAMED);
    }
    else if (dwNGenFramed == 1)
    {
        // NGened code should disable fpo
        jitFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_FRAMED);
    }

    jitFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_SKIP_VERIFICATION);

    if (m_pImage->m_profileDataSections[MethodBlockCounts].pData &&
        !m_zapper->m_pOpt->m_ignoreProfileData)
    {
        jitFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_BBOPT);
    }

    //
    // By default we always enable Hot/Cold procedure splitting
    //
    jitFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_PROCSPLIT);

    if (m_zapper->m_pOpt->m_noProcedureSplitting)
        jitFlags.Clear(CORJIT_FLAGS::CORJIT_FLAG_PROCSPLIT);

    //never emit inlined polls for NGen'd code.  The extra indirection is not optimal.
    if (jitFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_GCPOLL_INLINE))
    {
        jitFlags.Clear(CORJIT_FLAGS::CORJIT_FLAG_GCPOLL_INLINE);
        jitFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_GCPOLL_CALLS);
    }

    // If the method is specified for min-opts then turn everything off
    if (jitFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_MIN_OPT))
    {
        jitFlags.Clear(CORJIT_FLAGS::CORJIT_FLAG_BBINSTR);
        jitFlags.Clear(CORJIT_FLAGS::CORJIT_FLAG_BBOPT);
        jitFlags.Clear(CORJIT_FLAGS::CORJIT_FLAG_PROCSPLIT);
    }

#ifdef FEATURE_READYTORUN_COMPILER
    if (IsReadyToRunCompilation())
    {
        jitFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_READYTORUN);
        jitFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_USE_PINVOKE_HELPERS);
    }
#endif  // FEATURE_READYTORUN_COMPILER

    return jitFlags;
}

ZapDebugInfo * ZapInfo::EmitDebugInfo()
{
    if (m_iNativeVarInfo == 0 && m_iOffsetMapping == 0)
    {
        return NULL;
    }

    // We create a temporary buffer which is conservatily estimated to be
    // bigger than we need. We then copy the used portion into the ngen image.

    StackSBuffer debugInfoBuffer;
    m_pEECompileInfo->CompressDebugInfo(
            m_pOffsetMapping, m_iOffsetMapping,
            m_pNativeVarInfo, m_iNativeVarInfo,
            &debugInfoBuffer);

    if (IsReadyToRunCompilation())
        return ZapBlob::NewBlob(m_pImage, &debugInfoBuffer[0], debugInfoBuffer.GetSize());

    return m_pImage->m_pDebugInfoTable->GetDebugInfo(&debugInfoBuffer[0], debugInfoBuffer.GetSize());
}

ZapGCInfo * ZapInfo::EmitGCInfo()
{
    _ASSERTE(m_pGCInfo != NULL);

#ifdef FEATURE_EH_FUNCLETS
    return m_pImage->m_pGCInfoTable->GetGCInfo(m_pGCInfo, m_cbGCInfo, m_pMainUnwindInfo, m_cbMainUnwindInfo);
#else
    return m_pImage->m_pGCInfoTable->GetGCInfo(m_pGCInfo, m_cbGCInfo);
#endif // FEATURE_EH_FUNCLETS
}

ZapImport ** ZapInfo::EmitFixupList()
{
    ZapImport ** pFixupList = NULL;

    if (m_Imports.GetCount() != 0)
    {
        pFixupList = new (m_pImage->GetHeap()) ZapImport * [m_Imports.GetCount() + 1];
        memcpy(pFixupList, &(m_Imports[0]), m_Imports.GetCount() * sizeof(ZapImport *));
    }

    return pFixupList;
}

// Used by qsort
int __cdecl ZapInfo::CompareCodeRelocation(const void * a_, const void * b_)
{
    ZapInfo::CodeRelocation * a = (ZapInfo::CodeRelocation *)a_;
    ZapInfo::CodeRelocation * b = (ZapInfo::CodeRelocation *)b_;

    if (a->m_pNode != b->m_pNode)
    {
        return (a->m_pNode > b->m_pNode) ? 1 : -1;
    }

    return a->m_offset - b->m_offset;
}

void ZapInfo::EmitCodeRelocations()
{
    if (m_CodeRelocations.IsEmpty())
        return;

    qsort(&m_CodeRelocations[0], m_CodeRelocations.GetCount(), sizeof(CodeRelocation), CompareCodeRelocation);

    COUNT_T startIndex = 0;
    while (startIndex < m_CodeRelocations.GetCount())
    {
        ZapBlobWithRelocs * pNode = m_CodeRelocations[startIndex].m_pNode;

        COUNT_T endIndex = startIndex + 1;
        for ( ; endIndex < m_CodeRelocations.GetCount(); endIndex++)
        {
            if (m_CodeRelocations[endIndex].m_pNode != pNode)
                break;
        }

        ZapReloc * pRelocs = (ZapReloc *)
            new (m_pImage->GetHeap()) BYTE[sizeof(ZapReloc) * (endIndex - startIndex) + sizeof(ZapRelocationType)];

        for (COUNT_T i = 0; i < endIndex - startIndex; i++)
            pRelocs[i] = m_CodeRelocations[startIndex + i];

        // Set sentinel
        static_assert_no_msg(offsetof(ZapReloc, m_type) == 0);
        *(ZapRelocationType *)(pRelocs + (endIndex - startIndex)) = IMAGE_REL_INVALID;

        pNode->SetRelocs(pRelocs);

        startIndex = endIndex;
    }
}

void ZapInfo::ProcessReferences()
{
    COUNT_T count = m_CodeRelocations.GetCount();
    for (COUNT_T i = 0; i < count; i++)
    {
        CORINFO_METHOD_HANDLE hMethod = NULL;
        CORINFO_CLASS_HANDLE hClass = NULL;
        bool fMaybeConditionalImport = false;

        ZapNode * pTarget = m_CodeRelocations[i].m_pTargetNode;

        ZapNodeType type = pTarget->GetType();
        if (type == ZapNodeType_InnerPtr)
        {
            pTarget = ((ZapInnerPtr *)pTarget)->GetBase();
            type = pTarget->GetType();
        }

        switch (type)
        {
        case ZapNodeType_MethodEntryPoint:
            hMethod = ((ZapMethodEntryPoint*)pTarget)->GetHandle();

            if (m_pImage->m_pPreloader->DoesMethodNeedRestoringBeforePrestubIsRun(hMethod))
            {
                methodMustBeLoadedBeforeCodeIsRun(hMethod);
            }
            break;

        case ZapNodeType_Import_MethodHandle:
        case ZapNodeType_Import_FunctionEntry:
        case ZapNodeType_Import_IndirectPInvokeTarget:
        case ZapNodeType_Import_PInvokeTarget:
            hMethod = (CORINFO_METHOD_HANDLE)(((ZapImport *)pTarget)->GetHandle());
            fMaybeConditionalImport = true;
            break;
        case ZapNodeType_Import_ClassHandle:
        case ZapNodeType_Import_ClassDomainId:
        case ZapNodeType_Import_SyncLock:
            hClass = (CORINFO_CLASS_HANDLE)((ZapImport *)pTarget)->GetHandle();
            fMaybeConditionalImport = true;
            break;
        case ZapNodeType_Import_FieldHandle:
        case ZapNodeType_Import_StaticFieldAddress:
            hClass = m_pEEJitInfo->getFieldClass((CORINFO_FIELD_HANDLE)(((ZapImport *)pTarget)->GetHandle()));
            fMaybeConditionalImport = true;
            break;
        case ZapNodeType_Import_StringHandle:
        case ZapNodeType_Import_ModuleHandle:
        case ZapNodeType_Import_ModuleDomainId:
        case ZapNodeType_Import_VarArg:
            fMaybeConditionalImport = true;
            break;

        case ZapNodeType_MethodHandle:
            hMethod = (CORINFO_METHOD_HANDLE)(((ZapWrapper *)pTarget)->GetHandle());
            break;

        case ZapNodeType_ExternalMethodThunk:
        case ZapNodeType_ExternalMethodCell:
            hMethod = (CORINFO_METHOD_HANDLE)((ZapImport*)pTarget)->GetHandle();
            break;

        default:
            break;
        }

        if (fMaybeConditionalImport)
        {
            const ImportEntry * pExistingEntry = m_ImportSet.LookupPtr((ZapImport *)pTarget);
            if (pExistingEntry != NULL && pExistingEntry->fConditional)
            {
                const_cast<ImportEntry *>(pExistingEntry)->fConditional = false;
                m_Imports.Append((ZapImport *)pTarget);

                // 'handle' does not have to be added to CORCOMPILE_LOAD_TABLE since we adding
                // it to CORCOMPILE_HANDLE_TABLE
                if (hMethod != NULL)
                    m_MethodLoadTable.Load(hMethod, TRUE);
                else
                if (hClass != NULL)
                    m_ClassLoadTable.Load(hClass, TRUE);
            }
        }

        if (hMethod != NULL)
        {
            m_pImage->m_pPreloader->MethodReferencedByCompiledCode(hMethod);
        }
    }
}

// Compile a method using the JIT or Module compiler, and emit fixups

void ZapInfo::CompileMethod()
{
    PRECONDITION(m_zapper->m_pJitCompiler != NULL);

    InitMethodName();

    if (m_zapper->m_pOpt->m_verbose)
    {
        // The evaluation of m_currentMethodName.GetUnicode() is expensive
        // only do it when we are truely logging
        m_zapper->Info(W("Compiling method %s\n"), m_currentMethodName.GetUnicode());
    }

    m_currentMethodInfo = CORINFO_METHOD_INFO();
    if (!getMethodInfo(m_currentMethodHandle, &m_currentMethodInfo))
    {
        return;
    }

    // Method does not have IL (e.g. an abstract method)
    if (m_currentMethodInfo.ILCodeSize == 0)
        return;

    // If we are doing partial ngen, only compile methods with profile data
    if (!CurrentMethodHasProfileData() && m_zapper->m_pOpt->m_fPartialNGen)
    {
        if (m_zapper->m_pOpt->m_verbose)
            m_zapper->Info(W("Skipped because of no profile data\n"));
        return;
    }

    // During ngen we look for a hint attribute on the method that indicates
    // the method should be preprocessed for early
    // preparation. This normally happens automatically, but for methods that
    // are prepared explicitly at runtime the needed
    // information is missing from the ngen image, causing costly overheads
    // at runtime. When the author of the method knows about
    // this they can add the hint and reduce the perf cost at runtime.
    m_pImage->m_pPreloader->PrePrepareMethodIfNecessary(m_currentMethodHandle);

    // Retrieve method attributes from EEJitInfo - the ZapInfo's version updates
    // some of the flags related to hardware intrinsics but we don't want that.
    DWORD methodAttribs = m_pEEJitInfo->getMethodAttribs(m_currentMethodHandle);

#ifdef FEATURE_READYTORUN_COMPILER
    if (IsReadyToRunCompilation() && (methodAttribs & CORINFO_FLG_AGGRESSIVE_OPT))
    {
        // Skip methods marked with MethodImplOptions.AggressiveOptimization, they will be jitted instead. In the future,
        // consider letting the JIT determine whether aggressively optimized code can/should be pregenerated for the method
        // instead of this check.
        if (m_zapper->m_pOpt->m_verbose)
            m_zapper->Info(W("Skipped because of aggressive optimization flag\n"));
        return;
    }
#endif

#if defined(TARGET_X86) || defined(TARGET_AMD64) || defined(TARGET_ARM64)
    if (methodAttribs & CORINFO_FLG_JIT_INTRINSIC)
    {
        // Skip generating hardware intrinsic method bodies.
        //
        // We don't know what the implementation should do (whether it can do the actual intrinsic thing, or whether
        // it should throw a PlatformNotSupportedException).

        const char* namespaceName;
        getMethodNameFromMetadata(m_currentMethodHandle, nullptr, &namespaceName, nullptr);
        if (strcmp(namespaceName, "System.Runtime.Intrinsics.X86") == 0
            || strcmp(namespaceName, "System.Runtime.Intrinsics.Arm") == 0
            || strcmp(namespaceName, "System.Runtime.Intrinsics") == 0)
        {
            if (m_zapper->m_pOpt->m_verbose)
                m_zapper->Info(W("Skipped due to being a hardware intrinsic\n"));
            return;
        }
    }
#endif

    m_jitFlags = ComputeJitFlags(m_currentMethodHandle);

#ifdef FEATURE_READYTORUN_COMPILER
    if (IsReadyToRunCompilation())
    {
        // READYTORUN: FUTURE: Producedure spliting
        m_jitFlags.Clear(CORJIT_FLAGS::CORJIT_FLAG_PROCSPLIT);
    }
#endif

#ifdef TARGET_X86
    if (GetCompileInfo()->IsUnmanagedCallersOnlyMethod(m_currentMethodHandle))
    {
        if (m_zapper->m_pOpt->m_verbose)
            m_zapper->Warning(W("ReadyToRun:  Methods with UnmanagedCallersOnlyAttribute not implemented\n"));
        ThrowHR(E_NOTIMPL);
    }
#endif // TARGET_X86

    if (m_pImage->m_stats)
    {
        m_pImage->m_stats->m_methods++;
        m_pImage->m_stats->m_ilCodeSize += m_currentMethodInfo.ILCodeSize;
    }

    CorJitResult res = CORJIT_SKIPPED;   // FAILED() returns true for this value

    BYTE *pCode;
    ULONG cCode;
    bool  doNormalCompile = true;

#ifdef ALLOW_SXS_JIT_NGEN

    // Only retry the JIT compilation when we have a different JIT to run
    // Often we see both COMPlus_AltJIT and COMPlus_AltJitNgen set
    // which results in both JIT compilers set to the same altjit
    //
    doNormalCompile = (m_zapper->m_alternateJit != m_zapper->m_pJitCompiler);

    // Compile this method using the AltJitNgen compiler
    //
    if (m_zapper->m_alternateJit)
    {
        res = m_zapper->m_alternateJit->compileMethod( this,
                                                     &m_currentMethodInfo,
                                                     CORJIT_FLAGS::CORJIT_FLAG_CALL_GETJITFLAGS,
                                                     &pCode,
                                                     &cCode);

        // The above compileMethod call will typically return CORJIT_SKIPPED
        if (doNormalCompile && FAILED(res))
        {
            // We will fall back to the "main" JIT on failure.
            ResetForJitRetry();
        }
    }

#endif // ALLOW_SXS_JIT_NGEN

    // Compile this method using the normal JIT compiler
    //
    if (doNormalCompile && FAILED(res))
    {
        ICorJitCompiler * pCompiler = m_zapper->m_pJitCompiler;
        res = pCompiler->compileMethod(this,
                                       &m_currentMethodInfo,
                                       CORJIT_FLAGS::CORJIT_FLAG_CALL_GETJITFLAGS,
                                       &pCode,
                                       &cCode);
        if (FAILED(res))
        {
            ThrowExceptionForJitResult(res);
        }
    }

    MethodCompileComplete(m_currentMethodInfo.ftn);

#ifdef TARGET_X86
    // The x86 JIT over estimates the code size. Trim the blob size down to
    // the actual size.
    // We can do this only for non-split code. Adjusting the code size for split
    // methods would hose offsets in GC info.
    if (m_pColdCode == NULL)
    {
        m_pCode->AdjustBlobSize(cCode);
    }
#endif

    PublishCompiledMethod();
}

#ifndef FEATURE_FULL_NGEN
class MethodCodeComparer
{
    static BOOL NodeEquals(ZapNode * k1, ZapNode * k2)
    {
        return k1 == k2;
    }

    static BOOL BlobEquals(ZapBlob * k1, ZapBlob * k2)
    {
        if (k1 == NULL && k2 == NULL)
            return TRUE;
        if (k1 == NULL || k2 == NULL)
            return FALSE;

        if (k1->GetBlobSize() != k2->GetBlobSize())
            return FALSE;
        if (memcmp(k1->GetData(), k2->GetData(), k1->GetBlobSize()) != 0)
            return FALSE;

        return TRUE;
    }

    typedef ZapNode * EquivalentNodes[4][2];

    static BOOL EquivalentNode(ZapNode * k1, ZapNode * k2, EquivalentNodes & equivalentNodes)
    {
        if (k1 == k2)
            return TRUE;

        for (unsigned int i = 0; i < _countof(equivalentNodes); i++)
        {
            if (k1 == equivalentNodes[i][0] && k2 == equivalentNodes[i][1])
                return TRUE;
        }

        return FALSE;
    }

    static BOOL BlobWithRelocsEquals(ZapBlobWithRelocs * k1, ZapBlobWithRelocs * k2, EquivalentNodes & equivalentNodes)
    {
        if (k1 == NULL && k2 == NULL)
            return TRUE;
        if (k1 == NULL || k2 == NULL)
            return FALSE;

        if (k1->GetBlobSize() != k2->GetBlobSize())
            return FALSE;
        if (memcmp(k1->GetData(), k2->GetData(), k1->GetBlobSize()) != 0)
            return FALSE;

        ZapReloc * pRelocs1 = k1->GetRelocs();
        ZapReloc * pRelocs2 = k2->GetRelocs();

        if (pRelocs1 == NULL && pRelocs2 == NULL)
            return TRUE;
        if (pRelocs1 == NULL || pRelocs2 == NULL)
            return FALSE;

        while (pRelocs1->m_type != IMAGE_REL_INVALID || pRelocs2->m_type != IMAGE_REL_INVALID)
        {
            if (pRelocs1->m_type != pRelocs2->m_type || pRelocs1->m_offset != pRelocs2->m_offset)
                return FALSE;

            if (!EquivalentNode(pRelocs1->m_pTargetNode, pRelocs2->m_pTargetNode, equivalentNodes))
                return FALSE;

            pRelocs1++; pRelocs2++;
        }

        return TRUE;
    }

    static BOOL UnwindInfoEquals(ZapUnwindInfo * k1, ZapUnwindInfo * k2, EquivalentNodes & equivalentNodes)
    {
        if (k1 == NULL && k2 == NULL)
            return TRUE;
        if (k1 == NULL || k2 == NULL)
            return FALSE;

        return (k1->GetStartOffset() == k2->GetStartOffset()) &&
               (k1->GetEndOffset() == k2->GetEndOffset()) &&
               (k1->GetUnwindData() == k2->GetUnwindData()) &&
               EquivalentNode(k1->GetCode(), k2->GetCode(), equivalentNodes);
    }

    static BOOL UnwindInfoFragmentsEquals(ZapUnwindInfo * k1, ZapUnwindInfo * k2, EquivalentNodes & equivalentNodes)
    {
        if (k1 == NULL && k2 == NULL)
            return TRUE;
        if (k1 == NULL || k2 == NULL)
            return FALSE;

        while (k1 != NULL || k2 != NULL)
        {
            if (!UnwindInfoEquals(k1, k2, equivalentNodes))
                return FALSE;

            k1 = k1->GetNextFragment(); k2 = k2->GetNextFragment();
        }

        return TRUE;
    }

    static BOOL FixupListEquals(ZapImport ** k1, ZapImport ** k2)
    {
        if (k1 == NULL && k2 == NULL)
            return TRUE;
        if (k1 == NULL || k2 == NULL)
            return FALSE;

        while (*k1 != NULL || *k2 != NULL)
        {
            if (*k1 != *k2)
                return FALSE;
            k1++; k2++;
        }

        return TRUE;
    }

public:
    static BOOL MethodCodeEquals(ZapMethodHeader * k1, ZapMethodHeader * k2)
    {
        LIMITED_METHOD_CONTRACT;

        EquivalentNodes equivalentNodes =
        {
            { k1->m_pCode, k2->m_pCode },
            { k1->m_pColdCode, k2->m_pColdCode },
            { k1->m_pROData, k2->m_pROData },
            { k1->m_pProfileData, k2->m_pProfileData }
        };

        if (!BlobWithRelocsEquals(k1->m_pCode, k2->m_pCode, equivalentNodes))
            return FALSE;

        if (!BlobWithRelocsEquals(k1->m_pColdCode, k2->m_pColdCode, equivalentNodes))
            return FALSE;

        if (!UnwindInfoEquals(k1->m_pUnwindInfo, k2->m_pUnwindInfo, equivalentNodes))
            return FALSE;

        if (!UnwindInfoEquals(k1->m_pColdUnwindInfo, k2->m_pColdUnwindInfo, equivalentNodes))
            return FALSE;

#ifdef FEATURE_EH_FUNCLETS
        if (!UnwindInfoFragmentsEquals(k1->m_pUnwindInfoFragments, k2->m_pUnwindInfoFragments, equivalentNodes))
            return FALSE;
#endif

        if (!BlobWithRelocsEquals(k1->m_pROData, k2->m_pROData, equivalentNodes))
            return FALSE;

        if (!BlobWithRelocsEquals(k1->m_pProfileData, k2->m_pProfileData, equivalentNodes))
            return FALSE;

        if (!NodeEquals(k1->m_pGCInfo, k2->m_pGCInfo)) // interned
            return FALSE;

        if (!NodeEquals(k1->m_pDebugInfo, k2->m_pDebugInfo)) // interned
            return FALSE;

        if (!FixupListEquals(k1->m_pFixupList, k2->m_pFixupList))
            return FALSE;

        if (!BlobEquals(k1->m_pExceptionInfo, k2->m_pExceptionInfo))
            return FALSE;

        return TRUE;
    }
};

extern BOOL CanDeduplicateCode(CORINFO_METHOD_HANDLE method, CORINFO_METHOD_HANDLE duplicateMethod);

BOOL ZapImage::MethodCodeTraits::Equals(key_t k1, key_t k2)
{
    if (!MethodCodeComparer::MethodCodeEquals(k1, k2))
        return FALSE;

    // Check additional VM conditions that has to be satisfied for deduplication
    if (!CanDeduplicateCode(k1->GetHandle(), k2->GetHandle()))
        return FALSE;

    return TRUE;
}

COUNT_T ZapImage::MethodCodeTraits::Hash(key_t k)
{
    COUNT_T hash = ZapBlob::SHashTraits::Hash(ZapBlob::SHashTraits::GetKey(k->m_pCode));

    ZapReloc * pRelocs = k->m_pCode->GetRelocs();
    if (pRelocs != NULL)
    {
        while (pRelocs->m_type != IMAGE_REL_INVALID)
        {
            ZapNode * pTarget = pRelocs->m_pTargetNode;
            ZapNodeType type = pTarget->GetType();

            if (type == ZapNodeType_InnerPtr)
            {
                pTarget = ((ZapInnerPtr *)pTarget)->GetBase();
                type = pTarget->GetType();
            }

            // The IL stubs code often differs by just a method call or class handle. Include
            // these in the hash code.
            switch (type)
            {
            case ZapNodeType_MethodEntryPoint:
            case ZapNodeType_ExternalMethodThunk:
            case ZapNodeType_ClassHandle:
            case ZapNodeType_Import_ClassHandle:
            case ZapNodeType_MethodHandle:
            case ZapNodeType_Import_MethodHandle:
                hash = ((hash << 5) + hash) ^ (COUNT_T)((SIZE_T)pTarget);
                break;
            default:
                break;
            }

            pRelocs++;
        }
    }

    return hash;
}
#endif

void ZapInfo::PublishCompiledMethod()
{
    EmitCodeRelocations();

    // Go through all references in the code, make sure that we have fixups for them,
    // and ensure that they will be otherwise present in the image if necessary
    ProcessReferences();

    // See if there are load fixups to emit.
    m_ClassLoadTable.EmitLoadFixups(m_currentMethodHandle, this);

    if (!IsReadyToRunCompilation())
        m_MethodLoadTable.EmitLoadFixups(m_currentMethodHandle, this);

    ZapMethodHeader * pMethod = new (m_pImage->GetHeap()) ZapMethodHeader();

    pMethod->m_handle = m_currentMethodHandle;
    pMethod->m_classHandle = getMethodClass(m_currentMethodHandle);

    pMethod->m_pCode = m_pCode;
    pMethod->m_pColdCode = m_pColdCode;
    pMethod->m_pROData = m_pROData;

    pMethod->m_pProfileData = m_pProfileData;

    pMethod->m_pExceptionInfo = m_pExceptionInfo;

    pMethod->m_pFixupList = EmitFixupList();

    pMethod->m_pDebugInfo = EmitDebugInfo();
    pMethod->m_pGCInfo = EmitGCInfo();

#ifdef FEATURE_EH_FUNCLETS
    pMethod->m_pUnwindInfoFragments = m_pUnwindInfoFragments;

    // Set the combined GCInfo + UnwindInfo blob
    m_pUnwindInfo->SetUnwindData(pMethod->m_pGCInfo);

#if defined(TARGET_AMD64)
    if (m_pChainedColdUnwindInfo != NULL)
    {
        // Chain the cold unwind info with the hot unwind info
        m_pChainedColdUnwindInfo->SetUnwindData(m_pUnwindInfo);
    }
#endif // TARGET_AMD64

#endif // FEATURE_EH_FUNCLETS

#ifndef FEATURE_FULL_NGEN
    //
    // Method code deduplication
    //
    // For now, the only methods eligible for de-duplication are IL stubs
    //
    if (m_zapper->m_pOpt->m_compilerFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_IL_STUB))
    {
        ZapMethodHeader * pDuplicateMethod = m_pImage->m_CodeDeduplicator.Lookup(pMethod);
        if (pDuplicateMethod != NULL)
        {
            m_pImage->m_pPreloader->NoteDeduplicatedCode(pMethod->m_handle, pDuplicateMethod->m_handle);
            return;
        }

        m_pImage->m_CodeDeduplicator.Add(pMethod);
    }
#endif

    // Remember the gc info for IL stubs associated with hot methods so they can be packed well.
    // Stubs that have no metadata token cannot be tracked by IBC data.
    if (m_currentMethodProfilingDataFlags & (1 << ReadMethodCode))
    {
        if (m_zapper->m_pOpt->m_compilerFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_IL_STUB))
            m_pImage->m_PrioritizedGCInfo.Append(pMethod->m_pGCInfo);
    }

    pMethod->m_ProfilingDataFlags = m_currentMethodProfilingDataFlags;

    COUNT_T methodCompilationOrder = m_pImage->m_MethodCompilationOrder.GetCount();
    pMethod->m_compilationOrder = methodCompilationOrder;

    // We need to remember the first index into m_MethodCompilationOrder where we saw a method from this class
    m_pImage->InitializeClassLayoutOrder(pMethod->m_classHandle, methodCompilationOrder);

    m_pImage->m_CompiledMethods.Add(pMethod);
    m_pImage->m_MethodCompilationOrder.Append(pMethod);
}

void ZapInfo::getGSCookie(GSCookie * pCookieVal, GSCookie ** ppCookieVal)
{
    *pCookieVal = 0;

#ifdef FEATURE_READYTORUN_COMPILER
    if (IsReadyToRunCompilation())
    {
        *ppCookieVal = (GSCookie *)m_pImage->GetImportTable()->GetHelperImport(READYTORUN_HELPER_GSCookie);
        return;
    }
#endif

    *ppCookieVal = (GSCookie *)m_pImage->GetInnerPtr(m_pImage->m_pEEInfoTable,
        offsetof(CORCOMPILE_EE_INFO_TABLE, gsCookie));
}

DWORD ZapInfo::getJitFlags(CORJIT_FLAGS* jitFlags, DWORD sizeInBytes)
{
    _ASSERTE(jitFlags != NULL);
    _ASSERTE(sizeInBytes >= sizeof(m_jitFlags));

    *jitFlags = m_jitFlags;
    return sizeof(m_jitFlags);
}

bool ZapInfo::runWithErrorTrap(void (*function)(void*), void* param)
{
    return m_pEEJitInfo->runWithErrorTrap(function, param);
}

HRESULT ZapInfo::allocMethodBlockCounts (
    UINT32                        count,           // the count of <ILOffset, ExecutionCount> tuples
    ICorJitInfo::BlockCounts **   pBlockCounts     // pointer to array of <ILOffset, ExecutionCount> tuples
    )
{
    HRESULT hr;

    if (m_zapper->m_pOpt->m_compilerFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_IL_STUB))
    {
        *pBlockCounts = nullptr;
        return E_NOTIMPL;
    }

    // @TODO: support generic methods from other assemblies
    if (m_currentMethodModule != m_pImage->m_hModule)
    {
        *pBlockCounts = nullptr;
        return E_NOTIMPL;
    }

    mdMethodDef md = m_currentMethodToken;

    if (IsNilToken(md))
    {
        // This must be the non-System.Object instantiation of a generic type/method.
        IfFailRet(m_zapper->m_pEECompileInfo->GetMethodDef(m_currentMethodHandle, &md));
    }
#ifdef _DEBUG
    else
    {
        mdMethodDef mdTemp;
        IfFailRet(m_zapper->m_pEECompileInfo->GetMethodDef(m_currentMethodHandle, &mdTemp));
        _ASSERTE(md == mdTemp);
    }
#endif
    if (IsNilToken(md))
    {
        return E_FAIL;
    }

    // If the JIT retries the compilation (especially during JIT stress), it can
    // try to allocate the profiling data multiple times. We will just keep track
    // of the latest copy in this case.
    // _ASSERTE(m_pProfileData == NULL);

    DWORD totalSize = (DWORD) (count * sizeof(ICorJitInfo::BlockCounts)) + sizeof(CORBBTPROF_METHOD_HEADER);
    m_pProfileData = ZapBlobWithRelocs::NewAlignedBlob(m_pImage, NULL, totalSize, sizeof(DWORD));
    CORBBTPROF_METHOD_HEADER * profileData = (CORBBTPROF_METHOD_HEADER *) m_pProfileData->GetData();
    profileData->size           = totalSize;
    profileData->cDetail        = 0;
    profileData->method.token   = md;
    profileData->method.ILSize  = m_currentMethodInfo.ILCodeSize;
    profileData->method.cBlock  = count;

    *pBlockCounts = (ICorJitInfo::BlockCounts *)(&profileData->method.block[0]);

    return S_OK;
}

HRESULT ZapInfo::getMethodBlockCounts (
    CORINFO_METHOD_HANDLE ftnHnd,
    UINT32 *              pCount,          // pointer to the count of <ILOffset, ExecutionCount> tuples
    BlockCounts **        pBlockCounts,    // pointer to array of <ILOffset, ExecutionCount> tuples
    UINT32 *              pNumRuns
    )
{
    _ASSERTE(pBlockCounts != nullptr);
    _ASSERTE(pCount != nullptr);
    _ASSERTE(ftnHnd == m_currentMethodHandle);

    HRESULT hr;

    // Initialize outputs in case we return E_FAIL
    *pBlockCounts = nullptr;
    *pCount = 0;
    if (pNumRuns != nullptr)
    {
        *pNumRuns = 0;
    }

    // For generic instantiations whose IL is in another module,
    // the profile data is in that module
    // @TODO: Fetch the profile data from the other module.
    if ((m_currentMethodModule != m_pImage->m_hModule) ||
        m_zapper->m_pOpt->m_compilerFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_IL_STUB))
    {
        return E_FAIL;
    }

    ZapImage::ProfileDataSection * DataSection_MethodBlockCounts = & m_pImage->m_profileDataSections[MethodBlockCounts];

    if (!DataSection_MethodBlockCounts->pData)
    {
        return E_FAIL;
    }

    mdMethodDef md = m_currentMethodToken;

    if (IsNilToken(md))
    {
        // This must be the non-System.Object instantiation of a generic type/method.
        IfFailRet(m_zapper->m_pEECompileInfo->GetMethodDef(ftnHnd, &md));
    }
#ifdef _DEBUG
    else
    {
        mdMethodDef mdTemp;
        IfFailRet(m_zapper->m_pEECompileInfo->GetMethodDef(ftnHnd, &mdTemp));
        _ASSERTE(md == mdTemp);
    }
#endif
    if (IsNilToken(md))
    {
        return E_FAIL;
    }

    if (pNumRuns != nullptr)
    {
        *pNumRuns =  m_pImage->m_profileDataNumRuns;
    }

    const ZapImage::ProfileDataHashEntry * foundEntry = m_pImage->profileDataHashTable.LookupPtr(md);

    if (foundEntry == NULL)
    {
        return E_FAIL;
    }

    // The md must match.
    _ASSERTE(foundEntry->md == md);

    if (foundEntry->pos == 0)
    {
        // We might not have profile data and instead only have CompileStatus and flags
        assert(foundEntry->size == 0);
        return E_FAIL;
    }

    //
    //
    // We found the md. Let's retrieve the profile data.
    //
    _ASSERTE(foundEntry->size >= sizeof(CORBBTPROF_METHOD_HEADER));   // The size must at least this

    ProfileReader profileReader(DataSection_MethodBlockCounts->pData, DataSection_MethodBlockCounts->dataSize);

    // Locate the method in interest.
    SEEK(foundEntry->pos);
    CORBBTPROF_METHOD_HEADER *  profileData;
    READ_SIZE(profileData, CORBBTPROF_METHOD_HEADER, foundEntry->size);
    _ASSERTE(profileData->method.token == foundEntry->md);  // We should be looking at the right method
    _ASSERTE(profileData->size == foundEntry->size);        // and the cached size must match

    *pBlockCounts = (ICorJitInfo::BlockCounts *) &profileData->method.block[0];
    *pCount  = profileData->method.cBlock;

    // If the ILSize is non-zero the the ILCodeSize also must match
    //
    if ((profileData->method.ILSize != 0) && (profileData->method.ILSize != m_currentMethodInfo.ILCodeSize))
    {
        // IL code for this method does not match the IL code for the method when it was profiled
        // in such cases we tell the JIT to discard the profile data by returning E_FAIL
        //
        return E_FAIL;
    }

    return S_OK;
}

void ZapInfo::allocMem(
    ULONG               hotCodeSize,    /* IN */
    ULONG               coldCodeSize,   /* IN */
    ULONG               roDataSize,     /* IN */
    ULONG               xcptnsCount,    /* IN */
    CorJitAllocMemFlag  flag,           /* IN */
    void **             hotCodeBlock,   /* OUT */
    void **             coldCodeBlock,  /* OUT */
    void **             roDataBlock     /* OUT */
    )
{
    bool optForSize = m_zapper->m_pOpt->m_compilerFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_SIZE_OPT);

    UINT align = DEFAULT_CODE_ALIGN;

    if ((flag & CORJIT_ALLOCMEM_FLG_16BYTE_ALIGN) && !IsReadyToRunCompilation()) align = max(align, 16);

    m_pCode = ZapCodeBlob::NewAlignedBlob(m_pImage, NULL, hotCodeSize, align);
    *hotCodeBlock = m_pCode->GetData();

    if (coldCodeSize != 0)
    {
        align = sizeof(DWORD);

        m_pColdCode = ZapCodeBlob::NewAlignedBlob(m_pImage, NULL, coldCodeSize, align);
        *coldCodeBlock = m_pColdCode->GetData();
    }

    //
    // Allocate data
    //

    if (roDataSize > 0)
    {
        if (flag & CORJIT_ALLOCMEM_FLG_RODATA_16BYTE_ALIGN)
        {
            align = 16;
        }
        else if (optForSize || (roDataSize < 8))
        {
            align = TARGET_POINTER_SIZE;
        }
        else
        {
            align = 8;
        }
        m_pROData = ZapBlobWithRelocs::NewAlignedBlob(m_pImage, NULL, roDataSize, align);
        *roDataBlock = m_pROData->GetData();
    }

    if (m_pImage->m_stats)
    {
        m_pImage->m_stats->m_nativeCodeSize     += hotCodeSize;
        m_pImage->m_stats->m_nativeColdCodeSize += coldCodeSize;
        m_pImage->m_stats->m_nativeRODataSize   += roDataSize;

        BOOL haveProfileData = CurrentMethodHasProfileData();

        if (haveProfileData)
        {
            m_pImage->m_stats->m_nativeCodeSizeInProfiledMethods     += hotCodeSize;
            m_pImage->m_stats->m_nativeColdCodeSizeInProfiledMethods += coldCodeSize;
        }

        if (coldCodeSize)
        {
            m_pImage->m_stats->m_NumHotColdAllocations++;

            m_pImage->m_stats->m_nativeCodeSizeInSplitMethods     += hotCodeSize;
            m_pImage->m_stats->m_nativeColdCodeSizeInSplitMethods += coldCodeSize;

            if (haveProfileData)
            {
                m_pImage->m_stats->m_nativeCodeSizeInSplitProfiledMethods     += hotCodeSize;
                m_pImage->m_stats->m_nativeColdCodeSizeInSplitProfiledMethods += coldCodeSize;
            }
        }
        else
        {
            m_pImage->m_stats->m_NumHotAllocations++;
        }
    }
}

void * ZapInfo::allocGCInfo(size_t size)
{
    _ASSERTE(m_pGCInfo == NULL);

#ifdef HOST_64BIT
    if (size & 0xFFFFFFFF80000000LL)
    {
        IfFailThrow(CORJIT_OUTOFMEM);
    }
#endif // HOST_64BIT

    m_pGCInfo = new BYTE[size];
    m_cbGCInfo = size;

    return m_pGCInfo;
}

void ZapInfo::setEHcount(unsigned cEH)
{
    //
    // Must call after header has been allocated
    //

    if (cEH == 0)
    {
        _ASSERTE(!"Should not be called");
        return;
    }

    ULONG size = (sizeof(CORCOMPILE_EXCEPTION_CLAUSE) * cEH);

    _ASSERTE(m_pExceptionInfo == NULL);
    m_pExceptionInfo = ZapBlob::NewAlignedBlob(m_pImage, NULL, size, sizeof(DWORD));
}

void ZapInfo::setEHinfo(unsigned EHnumber,
                        const CORINFO_EH_CLAUSE *clause)
{
    //
    // Must call after EH info has been allocated
    //

    _ASSERTE(m_pExceptionInfo != NULL);

    CORCOMPILE_EXCEPTION_CLAUSE *ehClauseArray = (CORCOMPILE_EXCEPTION_CLAUSE *)m_pExceptionInfo->GetData();
    CORCOMPILE_EXCEPTION_CLAUSE *ilClause = &ehClauseArray[EHnumber];

    ilClause->TryStartPC    = clause->TryOffset;
    ilClause->TryEndPC      = clause->TryLength;
    ilClause->HandlerStartPC= clause->HandlerOffset;
    ilClause->HandlerEndPC  = clause->HandlerLength;
    ilClause->Flags         = (CorExceptionFlag) clause->Flags;

    if (clause->Flags & CORINFO_EH_CLAUSE_FILTER)
    {
        ilClause->FilterOffset = clause->FilterOffset;
    }
    else
    {
        ilClause->ClassToken = clause->ClassToken;

        if (ilClause->ClassToken != 0)
        {
            CORINFO_RESOLVED_TOKEN resolvedToken = { 0 };
            resolvedToken.tokenContext = MAKE_METHODCONTEXT(m_currentMethodInfo.ftn);
            resolvedToken.tokenScope = m_currentMethodInfo.scope;
            resolvedToken.token = ilClause->ClassToken;
            resolvedToken.tokenType = CORINFO_TOKENKIND_Class;

            resolveToken(&resolvedToken);

            if (m_zapper->m_pOpt->m_compilerFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_IL_STUB))
            {
                // IL stub tokens are 'private' and do not resolve correctly in their parent module's metadata.

                // Currently, the only place we are using a token here is for a COM-to-CLR exception-to-HRESULT
                // mapping catch clause.  We want this catch clause to catch all exceptions, so we override the
                // token to be mdTypeRefNil, which used by the EH system to mean catch(...)
#ifdef _DEBUG
                // The proper way to do this, should we ever want to support arbitrary types here, is to "pre-
                // resolve" the token and store the TypeHandle in the clause.  But this requires additional
                // infrastructure to ensure the TypeHandle is saved and fixed-up properly.  For now, we will
                // simply assert that the original type was System.Object.

                CORINFO_CLASS_HANDLE systemObjectHandle = getBuiltinClass(CLASSID_SYSTEM_OBJECT);
                _ASSERTE(systemObjectHandle == resolvedToken.hClass);
#endif // _DEBUG
                ilClause->ClassToken = mdTypeRefNil;
            }
            else
            {
                // For all clause types add fixup to ensure the types are loaded before the code of the method
                // containing the catch blocks is executed. This ensures that a failure to load the types would
                // not happen when the exception handling is in progress and it is looking for a catch handler.
                // At that point, we could only fail fast.
                classMustBeLoadedBeforeCodeIsRun(resolvedToken.hClass);
            }
        }
    }

    //
    // @TODO: this does not support DynamicMethods
    //
}

int ZapInfo::canHandleException(struct _EXCEPTION_POINTERS *pExceptionPointers)
{
    return (EXCEPTION_EXECUTE_HANDLER);
}

int ZapInfo::doAssert(const char* szFile, int iLine, const char* szExpr)
{

#if defined(_DEBUG)
    return(_DbgBreakCheck(szFile, iLine, szExpr));
#else
    return(true);       // break into debugger
#endif

}
void ZapInfo::reportFatalError(CorJitResult result)
{
    m_zapper->Info(W("Jit reported error 0x%x while compiling %s\n"), (int)result,
                   m_currentMethodName.GetUnicode());
}

// Reserve memory for the method/funclet's unwind information
// Note that this must be called before allocMem, it should be
// called once for the main method, once for every funclet region, and
// once for every non-funclet cold region for which we will call
// allocUnwindinfo.
//
// For prejitted code we need to count how many funclet regions
// we have so that we can allocate and sort a contiguous .rdata block.
//
void ZapInfo::reserveUnwindInfo(BOOL isFunclet, BOOL isColdCode, ULONG unwindSize)
{
    // Nothing to do
}


//
// Allocate and initialize the .xdata and .pdata for this method or
// funclet region and get the block of memory needed for the machine-specific
// unwind information (the info for crawling the stack frame).
// Note that allocMem must be called first.
//
// The pHotCode parameter points at the first byte of the code of the method.
// The startOffset and endOffset are the region (main or funclet) that
// we are to allocate and create .xdata and .pdata for.
// The pUnwindBlock is copied and contains the .xdata unwind info.
//
// Parameters:
//
//    pHotCode        main method code buffer, always filled in
//    pColdCode       cold code buffer, only filled in if this is cold code,
//                      null otherwise
//    startOffset     start of code block, relative to appropriate code buffer
//                      (e.g. pColdCode if cold, pHotCode if hot).
//    endOffset       end of code block, relative to appropriate code buffer
//    unwindSize      size of unwind info pointed to by pUnwindBlock
//    pUnwindBlock    pointer to unwind info
//    funcKind        type of funclet (main method code, handler, filter)
//
void ZapInfo::allocUnwindInfo (
        BYTE *              pHotCode,              /* IN */
        BYTE *              pColdCode,             /* IN */
        ULONG               startOffset,           /* IN */
        ULONG               endOffset,             /* IN */
        ULONG               unwindSize,            /* IN */
        BYTE *              pUnwindBlock,          /* IN */
        CorJitFuncKind      funcKind               /* IN */
        )
{
#ifdef FEATURE_EH_FUNCLETS
    _ASSERTE(pHotCode == m_pCode->GetData());
    _ASSERTE(pColdCode == NULL || pColdCode == m_pColdCode->GetData());

    ZapNode * pCode = (pColdCode != NULL) ? m_pColdCode : m_pCode;

    ZapUnwindInfo * pUnwindInfo = new (m_pImage->GetHeap()) ZapUnwindInfo(pCode, startOffset, endOffset);

    // Prepend the new unwind info to the linked list of all fragments
    pUnwindInfo->SetNextFragment(m_pUnwindInfoFragments);
    m_pUnwindInfoFragments = pUnwindInfo;

    if (funcKind == CORJIT_FUNC_ROOT && pColdCode == NULL && startOffset == 0)
    {
        //
        // Main method unwind data
        //

        _ASSERTE(m_pMainUnwindInfo == NULL);

        m_pMainUnwindInfo = new BYTE[unwindSize];
        m_cbMainUnwindInfo = unwindSize;

        memcpy(m_pMainUnwindInfo, pUnwindBlock, unwindSize);

        // UnwindData Will be set to the combined GCInfo + UnwindInfo blob later as the compiled method is published

        _ASSERTE(m_pUnwindInfo == NULL);
        m_pUnwindInfo = pUnwindInfo;
    }
#if defined(TARGET_AMD64)
    else
    if (funcKind == CORJIT_FUNC_ROOT && pColdCode != NULL)
    {
        //
        // Chained cold code unwind data
        //

        _ASSERTE(unwindSize == 0);

        // UnwindData Will be chained to the parent unwind info later as the compiled method is published

        _ASSERTE(m_pChainedColdUnwindInfo == NULL);
        m_pChainedColdUnwindInfo = pUnwindInfo;
    }
#endif
    else
    {

        //
        // Normal unwind data
        //

        ZapUnwindData * pUnwindData = m_pImage->m_pUnwindDataTable->GetUnwindData(pUnwindBlock, unwindSize, funcKind == CORJIT_FUNC_FILTER);
        pUnwindInfo->SetUnwindData(pUnwindData);
    }
#endif // FEATURE_EH_FUNCLETS
}

BOOL ZapInfo::logMsg(unsigned level, const char *fmt, va_list args)
{
    if (HasSvcLogger())
    {
        if (level <= LL_INFO10)
        {
            StackSString ss;
            ss.VPrintf(fmt,args);
            GetSvcLogger()->Log(ss.GetUnicode(), LogLevel_Success);
            return TRUE;
        }
    }

#ifdef LOGGING
    if (LoggingOn(LF_JIT, level))
    {
        LogSpewValist(LF_JIT, level, (char*) fmt, args);
        return TRUE;
    }
#endif // LOGGING

    return FALSE;
}

//
// ICorDynamicInfo
//

DWORD ZapInfo::getThreadTLSIndex(void **ppIndirection)
{
    _ASSERTE(ppIndirection != NULL);

    *ppIndirection = NULL;
    return (DWORD)-1;
}

const void * ZapInfo::getInlinedCallFrameVptr(void **ppIndirection)
{
    _ASSERTE(ppIndirection != NULL);

    *ppIndirection = m_pImage->GetInnerPtr(m_pImage->m_pEEInfoTable,
        offsetof(CORCOMPILE_EE_INFO_TABLE, inlinedCallFrameVptr));
    return NULL;
}

LONG * ZapInfo::getAddrOfCaptureThreadGlobal(void **ppIndirection)
{
    _ASSERTE(ppIndirection != NULL);

    *ppIndirection = NULL;
    if (IsReadyToRunCompilation())
    {
        *ppIndirection = m_pImage->GetImportTable()->GetHelperImport(READYTORUN_HELPER_IndirectTrapThreads);
    }
    else
    {
        *ppIndirection = m_pImage->GetInnerPtr(m_pImage->m_pEEInfoTable,
            offsetof(CORCOMPILE_EE_INFO_TABLE, addrOfCaptureThreadGlobal));
    }

    return NULL;
}

// Get slow lazy string literal helper to use (CORINFO_HELP_STRCNS*).
// Returns CORINFO_HELP_UNDEF if lazy string literal helper cannot be used.
CorInfoHelpFunc ZapInfo::getLazyStringLiteralHelper(CORINFO_MODULE_HANDLE handle)
{
    if (handle == m_pImage->m_hModule)
        return CORINFO_HELP_STRCNS_CURRENT_MODULE;

    return CORINFO_HELP_STRCNS;
}

CORINFO_MODULE_HANDLE ZapInfo::embedModuleHandle(CORINFO_MODULE_HANDLE handle,
                                                                void **ppIndirection)
{
    _ASSERTE(ppIndirection != NULL);

    if (IsReadyToRunCompilation())
    {
        _ASSERTE(!"embedModuleHandle");
        ThrowHR(E_NOTIMPL);
    }

    BOOL fHardbound = m_pImage->m_pPreloader->CanEmbedModuleHandle(handle);
    if (fHardbound)
    {
        if (handle == m_pImage->m_hModule)
        {
            // If the handle is the module we are currently ngening, we will
            // assume that Module is the very first thing in the preload section
            *ppIndirection = NULL;
            return (CORINFO_MODULE_HANDLE)m_pImage->m_pPreloadSections[CORCOMPILE_SECTION_MODULE];
        }

        *ppIndirection = m_pImage->GetImportTable()->GetModuleHandleImport(handle);
    }
    else
    {
        ZapImport * pImport = m_pImage->GetImportTable()->GetModuleHandleImport(handle);
        AppendConditionalImport(pImport);

        *ppIndirection = pImport;
    }
    return NULL;
}

//
// The following functions indicate whether a handle can be directly embedded into
// the code being compiled, or if it needs to be accessed with a (fixable) indirection.
// Embeddable handles are those that will be persisted in the zap image.
//
// These functions are gradually being all moved across to ceeload.cpp and compile.cpp.
//

CORINFO_CLASS_HANDLE ZapInfo::embedClassHandle(CORINFO_CLASS_HANDLE handle,
                                                         void **ppIndirection)
{
    _ASSERTE(ppIndirection != NULL);

    if (IsReadyToRunCompilation())
    {
        _ASSERTE(!"embedClassHandle");
        ThrowHR(E_NOTIMPL);
    }

    m_pImage->m_pPreloader->AddTypeToTransitiveClosureOfInstantiations(handle);

    BOOL fHardbound = m_pImage->m_pPreloader->CanEmbedClassHandle(handle);
    if (fHardbound)
    {
        CORINFO_MODULE_HANDLE moduleHandle = m_pEECompileInfo->GetLoaderModuleForEmbeddableType(handle);

        if (moduleHandle == m_pImage->m_hModule)
        {
            // If the handle is the module we are currently ngening, we can
            // embed it after its resolved. So use a deferred reloc

            *ppIndirection = NULL;
            return CORINFO_CLASS_HANDLE(m_pImage->GetWrappers()->GetClassHandle(handle));
        }

        *ppIndirection = m_pImage->GetImportTable()->GetClassHandleImport(handle);
    }
    else
    {
        ZapImport * pImport = m_pImage->GetImportTable()->GetClassHandleImport(handle);
        AppendConditionalImport(pImport);

        *ppIndirection = pImport;
    }
    return NULL;
}

CORINFO_FIELD_HANDLE ZapInfo::embedFieldHandle(CORINFO_FIELD_HANDLE handle,
                                               void **ppIndirection)
{
    _ASSERTE(ppIndirection != NULL);

    if (IsReadyToRunCompilation())
    {
        _ASSERTE(!"embedFieldHandle");
        ThrowHR(E_NOTIMPL);
    }

    m_pImage->m_pPreloader->AddTypeToTransitiveClosureOfInstantiations(m_pEEJitInfo->getFieldClass(handle));

    BOOL fHardbound = m_pImage->m_pPreloader->CanEmbedFieldHandle(handle);
    if (fHardbound)
    {
        CORINFO_MODULE_HANDLE moduleHandle = m_pEECompileInfo->GetLoaderModuleForEmbeddableField(handle);

        if (moduleHandle == m_pImage->m_hModule)
        {
            // If the handle is the module we are currently ngening, we can
            // embed it after its resolved. So use a deferred reloc

            *ppIndirection = NULL;
            return CORINFO_FIELD_HANDLE(m_pImage->GetWrappers()->GetFieldHandle(handle));
        }
    }


    ZapImport * pImport = m_pImage->GetImportTable()->GetFieldHandleImport(handle);
    AppendConditionalImport(pImport);

    *ppIndirection = pImport;
    return NULL;
}

CORINFO_METHOD_HANDLE ZapInfo::embedMethodHandle(CORINFO_METHOD_HANDLE handle,
                                                 void **ppIndirection)
{
    _ASSERTE(ppIndirection != NULL);

    if (IsReadyToRunCompilation())
    {
        // READYTORUN FUTURE: Handle this case correctly
        ThrowHR(E_NOTIMPL);
    }

    CORINFO_MODULE_HANDLE moduleHandle = m_pEECompileInfo->GetLoaderModuleForEmbeddableMethod(handle);
    if (moduleHandle == m_pImage->m_hModule
        && m_pImage->m_pPreloader->CanEmbedMethodHandle(handle, m_currentMethodHandle))
    {
        // If the handle is the module we are currently ngening, we can
        // embed it after its resolved. So use a deferred reloc

        *ppIndirection = NULL;
        return CORINFO_METHOD_HANDLE(m_pImage->GetWrappers()->GetMethodHandle(handle));
    }

    ZapImport * pImport = m_pImage->GetImportTable()->GetMethodHandleImport(handle);
    AppendConditionalImport(pImport);

    *ppIndirection = pImport;
    return NULL;
}

CORINFO_CLASS_HANDLE ZapInfo::getTokenTypeAsHandle(CORINFO_RESOLVED_TOKEN * pResolvedToken)
{
    return m_pEEJitInfo->getTokenTypeAsHandle(pResolvedToken);
}

void ZapInfo::getLocationOfThisType(CORINFO_METHOD_HANDLE context, CORINFO_LOOKUP_KIND * pLookupKind)
{
    m_pEEJitInfo->getLocationOfThisType(context, pLookupKind);
}

void
ZapInfo::embedGenericHandle(CORINFO_RESOLVED_TOKEN * pResolvedToken,
                            BOOL                     fEmbedParent,
                            CORINFO_GENERICHANDLE_RESULT *pResult)
{
    _ASSERTE(pResult);

    m_pEEJitInfo->embedGenericHandle( pResolvedToken,
                                      fEmbedParent,
                                      pResult);

    _ASSERTE(pResult->compileTimeHandle);

    if (pResult->lookup.lookupKind.needsRuntimeLookup)
    {
        if (!IsReadyToRunCompilation())
            embedGenericSignature(&pResult->lookup);

        if (pResult->handleType == CORINFO_HANDLETYPE_METHOD)
        {
            // There is no easy way to detect method referenced via generic lookups in generated code.
            // Report this method reference unconditionally.
            m_pImage->m_pPreloader->MethodReferencedByCompiledCode((CORINFO_METHOD_HANDLE)pResult->compileTimeHandle);
        }
    }
    else
    {
        void *pIndirection = 0;
        CORINFO_GENERIC_HANDLE handle = 0;

        switch (pResult->handleType)
        {
        case CORINFO_HANDLETYPE_CLASS:
            if (IsReadyToRunCompilation())
            {
                ZapImport * pImport = m_pImage->GetImportTable()->GetClassImport(ENCODE_TYPE_HANDLE, pResolvedToken);
                AppendConditionalImport(pImport);
                pIndirection = pImport;
                handle = NULL;
            }
            else
            {
                CORINFO_CLASS_HANDLE clsHnd = (CORINFO_CLASS_HANDLE) pResult->compileTimeHandle;
                handle = CORINFO_GENERIC_HANDLE(embedClassHandle(clsHnd, &pIndirection));
            }
            break;

        case CORINFO_HANDLETYPE_METHOD:
            if (IsReadyToRunCompilation())
            {
                ZapImport * pImport = m_pImage->GetImportTable()->GetMethodImport(ENCODE_METHOD_HANDLE, (CORINFO_METHOD_HANDLE)pResult->compileTimeHandle, pResolvedToken);
                AppendConditionalImport(pImport);
                pIndirection = pImport;
                handle = NULL;
            }
            else
            {
                CORINFO_METHOD_HANDLE methHnd = (CORINFO_METHOD_HANDLE) pResult->compileTimeHandle;
                handle =  CORINFO_GENERIC_HANDLE(embedMethodHandle(methHnd, &pIndirection));
            }
            break;

        case CORINFO_HANDLETYPE_FIELD:
            if (IsReadyToRunCompilation())
            {
                ZapImport * pImport = m_pImage->GetImportTable()->GetFieldImport(ENCODE_FIELD_HANDLE, (CORINFO_FIELD_HANDLE)pResult->compileTimeHandle, pResolvedToken);
                AppendConditionalImport(pImport);
                pIndirection = pImport;
                handle = NULL;
            }
            else
            {
                CORINFO_FIELD_HANDLE fldHnd = (CORINFO_FIELD_HANDLE) pResult->compileTimeHandle;
                handle = CORINFO_GENERIC_HANDLE(embedFieldHandle(fldHnd, &pIndirection));
            }
            break;

        default:
            ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_TOKEN_TYPE);
        }

        if (handle)
        {
            pResult->lookup.constLookup.accessType   = IAT_VALUE;
            pResult->lookup.constLookup.handle       = CORINFO_GENERIC_HANDLE(handle);
        }
        else
        {
            pResult->lookup.constLookup.accessType   = IAT_PVALUE;
            pResult->lookup.constLookup.addr         = pIndirection;
        }
    }
}

void ZapInfo::embedGenericSignature(CORINFO_LOOKUP * pLookup)
{
    _ASSERTE(pLookup->lookupKind.needsRuntimeLookup);

    if (IsReadyToRunCompilation())
    {
		UNREACHABLE_MSG("We should never get here for the ReadyToRun compilation.");
        ThrowHR(E_NOTIMPL);
    }

    if (pLookup->runtimeLookup.signature != NULL)
    {
        pLookup->runtimeLookup.signature = m_pImage->GetImportTable()->GetGenericSignature(
            pLookup->runtimeLookup.signature, pLookup->lookupKind.runtimeLookupKind == CORINFO_LOOKUP_METHODPARAM);
    }
}

bool ZapInfo::getTailCallHelpers(
        CORINFO_RESOLVED_TOKEN* callToken,
        CORINFO_SIG_INFO* sig,
        CORINFO_GET_TAILCALL_HELPERS_FLAGS flags,
        CORINFO_TAILCALL_HELPERS* pResult)
{
    ThrowHR(E_NOTIMPL);
    return false;
}

bool ZapInfo::convertPInvokeCalliToCall(
                    CORINFO_RESOLVED_TOKEN * pResolvedToken,
                    bool fMustConvert)
{
    return false;
}

#ifdef FEATURE_READYTORUN_COMPILER
ReadyToRunHelper MapReadyToRunHelper(CorInfoHelpFunc func, bool * pfOptimizeForSize)
{
    switch (func)
    {
#define OPTIMIZEFORSIZE *pfOptimizeForSize = true;
#define HELPER(readyToRunHelper, corInfoHelpFunc, flags) \
    case corInfoHelpFunc: flags return readyToRunHelper;
#include "readytorunhelpers.h"

    case CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPEHANDLE:
        return READYTORUN_HELPER_GetRuntimeTypeHandle;

    case CORINFO_HELP_STRCNS_CURRENT_MODULE:
        *pfOptimizeForSize = true;
        return READYTORUN_HELPER_GetString;

    default:
        return READYTORUN_HELPER_Invalid;
    }
}
#endif // FEATURE_READYTORUN_COMPILER

void * ZapInfo::getHelperFtn (CorInfoHelpFunc ftnNum, void **ppIndirection)
{
    _ASSERTE(ppIndirection != NULL);
    *ppIndirection = NULL;

#ifdef FEATURE_READYTORUN_COMPILER
    if (IsReadyToRunCompilation())
    {
        bool fOptimizeForSize = false;
        ReadyToRunHelper helperNum = MapReadyToRunHelper(ftnNum, &fOptimizeForSize);

        if (helperNum == READYTORUN_HELPER_Invalid)
        {
            if (m_zapper->m_pOpt->m_verbose)
                m_zapper->Warning(W("ReadyToRun: JIT helper not supported: %S\n"), m_pEEJitInfo->getHelperName(ftnNum));
            ThrowHR(E_NOTIMPL);
        }

        if (fOptimizeForSize)
        {
            *ppIndirection = NULL;
            return m_pImage->GetImportTable()->GetIndirectHelperThunk(helperNum);
        }
        else
        {
            *ppIndirection = m_pImage->GetImportTable()->GetHelperImport(helperNum);
            return NULL;
        }
    }
#endif

    DWORD dwHelper = ftnNum;

    switch (ftnNum)
    {
    case CORINFO_HELP_PROF_FCN_ENTER:
        *ppIndirection = m_pImage->GetInnerPtr(GetProfilingHandleImport(), kZapProfilingHandleImportValueIndexEnterAddr * TARGET_POINTER_SIZE);
        return NULL;
    case CORINFO_HELP_PROF_FCN_LEAVE:
        *ppIndirection = m_pImage->GetInnerPtr(GetProfilingHandleImport(), kZapProfilingHandleImportValueIndexLeaveAddr * TARGET_POINTER_SIZE);
        return NULL;
    case CORINFO_HELP_PROF_FCN_TAILCALL:
        *ppIndirection = m_pImage->GetInnerPtr(GetProfilingHandleImport(), kZapProfilingHandleImportValueIndexTailcallAddr * TARGET_POINTER_SIZE);
        return NULL;
#ifdef TARGET_AMD64
    case CORINFO_HELP_STOP_FOR_GC:
        // Force all calls in ngen images for this helper to use an indirect call.
        // We cannot use a jump stub to reach this helper because
        // the RAX register can contain a return value.
        dwHelper |= CORCOMPILE_HELPER_PTR;
   break;
#endif
    default:
        break;
    }

    if (m_pImage->m_pHelperThunks[ftnNum] == NULL)
    {
        ZapNode * pHelperThunk;
        if (ftnNum == CORINFO_HELP_STRCNS_CURRENT_MODULE)
        {
            pHelperThunk = new (m_pImage->GetHeap()) ZapLazyHelperThunk(CORINFO_HELP_STRCNS);
        }
        else
        {
            pHelperThunk = new (m_pImage->GetHeap()) ZapHelperThunk(dwHelper);
        }
#if defined(TARGET_ARM)
        if ((dwHelper & CORCOMPILE_HELPER_PTR) == 0)
            pHelperThunk = m_pImage->GetInnerPtr(pHelperThunk, THUMB_CODE);
#endif
        m_pImage->m_pHelperThunks[ftnNum] = pHelperThunk;
    }

    void * ptr = m_pImage->m_pHelperThunks[ftnNum];

    if (dwHelper & CORCOMPILE_HELPER_PTR)
    {
        *ppIndirection = ptr;
        return NULL;
    }

    return ptr;
}

ULONG ZapInfo::GetNumFixups()
{
    return m_Imports.GetCount();
}

void ZapInfo::AppendConditionalImport(ZapImport * pImport)
{
    if (m_ImportSet.LookupPtr(pImport) != NULL)
        return;

    ImportEntry entry;
    entry.pImport = pImport;
    entry.fConditional = true;
    m_ImportSet.Add(entry);
}

void ZapInfo::AppendImport(ZapImport * pImport)
{
    const ImportEntry * pExistingEntry = m_ImportSet.LookupPtr(pImport);
    if (pExistingEntry != NULL)
    {
        if (!pExistingEntry->fConditional)
            return;
        const_cast<ImportEntry *>(pExistingEntry)->fConditional = false;
    }
    else
    {
        ImportEntry entry;
        entry.pImport = pImport;
        entry.fConditional = false;
        m_ImportSet.Add(entry);
    }

    m_Imports.Append(pImport);
}

//
// This function indicates whether a method entry point be directly embedded into
// the code being compiled, or if we can use a (fixable) cross module thunk.
// If we can't use either of these then we return NULL and we will use an
// (fixable) indirection cell to perform the call.
//
PVOID ZapInfo::embedDirectCall(CORINFO_METHOD_HANDLE ftn,
                               CORINFO_ACCESS_FLAGS accessFlags,
                               BOOL fAllowThunk)
{
    if (!m_pImage->m_pPreloader->CanEmbedFunctionEntryPoint(ftn, m_currentMethodHandle, accessFlags))
    {
        return NULL;
    }

    ZapNode * pEntryPointOrThunkToEmbed = NULL;

    //
    // If it's in the same module then we can call it directly
    //
    CORINFO_MODULE_HANDLE moduleHandle = m_pEECompileInfo->GetLoaderModuleForEmbeddableMethod(ftn);
    if (moduleHandle == m_pImage->m_hModule
        && m_pImage->m_pPreloader->CanEmbedMethodHandle(ftn, m_currentMethodHandle))
    {
        pEntryPointOrThunkToEmbed = m_pImage->m_pMethodEntryPoints->GetMethodEntryPoint(ftn, accessFlags);
    }
    else  // otherwise we are calling into an external module
    {
        if (!fAllowThunk)
        {
            return NULL;
        }

        pEntryPointOrThunkToEmbed = m_pImage->GetImportTable()->GetExternalMethodThunk(ftn);
    }

#ifdef TARGET_ARM
    pEntryPointOrThunkToEmbed = m_pImage->GetInnerPtr(pEntryPointOrThunkToEmbed, THUMB_CODE);
#endif

    return pEntryPointOrThunkToEmbed;
}

void ZapInfo::notifyInstructionSetUsage(CORINFO_InstructionSet instructionSet, bool supportEnabled)
{
    m_pEEJitInfo->notifyInstructionSetUsage(instructionSet, supportEnabled);
}

void ZapInfo::getFunctionEntryPoint(
                                CORINFO_METHOD_HANDLE   ftn,                 /* IN  */
                                CORINFO_CONST_LOOKUP *  pResult,             /* OUT */
                                CORINFO_ACCESS_FLAGS    accessFlags/*=CORINFO_ACCESS_ANY*/)
{
    if (IsReadyToRunCompilation())
    {
        // READYTORUN: FUTURE: JIT still calls this for tail. and jmp instructions
        if (m_zapper->m_pOpt->m_verbose)
            m_zapper->Warning(W("ReadyToRun: Method entrypoint cannot be encoded\n"));
        ThrowHR(E_NOTIMPL);
    }

    // Must deal with methods that are methodImpl'd within their own type.
    ftn = mapMethodDeclToMethodImpl(ftn);

    m_pImage->m_pPreloader->AddMethodToTransitiveClosureOfInstantiations(ftn);

    void * entryPointOrThunkToEmbed = embedDirectCall(ftn, accessFlags, TRUE);
    if (entryPointOrThunkToEmbed != NULL)
    {
        pResult->accessType = IAT_VALUE;
        pResult->addr       = entryPointOrThunkToEmbed;
    }
    else
    {
        ZapImport * pImport = m_pImage->GetImportTable()->GetFunctionEntryImport(ftn);
        AppendConditionalImport(pImport);

        // Tell the JIT to use an indirections
        pResult->accessType   = IAT_PVALUE;
        pResult->addr         = pImport;
    }
}

void ZapInfo::getFunctionFixedEntryPoint(CORINFO_METHOD_HANDLE   ftn,
                                         CORINFO_CONST_LOOKUP *  pResult)
{
    _ASSERTE(pResult);

    m_pImage->m_pPreloader->AddMethodToTransitiveClosureOfInstantiations(ftn);

    // We can only embed entrypoints from the module being NGened since we do not support mapping of external
    // import thunks to MethodDesc. It should be ok since the delegate targets are typically from the same module.
    void * entryPointToEmbed = embedDirectCall(ftn, CORINFO_ACCESS_ANY, FALSE);

    if (entryPointToEmbed != NULL)
    {
        pResult->accessType   = IAT_VALUE;
        pResult->addr         = entryPointToEmbed;
    }
    else
    {
        ZapImport * pImport = m_pImage->GetImportTable()->GetFunctionEntryImport(ftn);
        AppendConditionalImport(pImport);

        pResult->accessType   = IAT_PVALUE;
        pResult->addr         = pImport;
    }
}

void * ZapInfo::getMethodSync(CORINFO_METHOD_HANDLE ftn,
                                            void **ppIndirection)
{
    _ASSERTE(ppIndirection != NULL);

    CORINFO_CLASS_HANDLE classHandle = getMethodClass(ftn);

    ZapImport * pImport = m_pImage->GetImportTable()->GetSyncLockImport(classHandle);
    AppendConditionalImport(pImport);

    *ppIndirection = pImport;
    return NULL;
}

void * ZapInfo::getAddressOfPInvokeFixup(CORINFO_METHOD_HANDLE method,void **ppIndirection)
{
    _ASSERTE(ppIndirection != NULL);
    *ppIndirection = NULL;

    m_pImage->m_pPreloader->AddMethodToTransitiveClosureOfInstantiations(method);

    if (!IsReadyToRunCompilation())
    {
        CORINFO_MODULE_HANDLE moduleHandle = m_pEECompileInfo->GetLoaderModuleForEmbeddableMethod(method);
        if (moduleHandle == m_pImage->m_hModule
            && m_pImage->m_pPreloader->CanEmbedMethodHandle(method, m_currentMethodHandle))
        {
            return PVOID(m_pImage->GetWrappers()->GetAddrOfPInvokeFixup(method));
        }
    }

    //
    // The indirect P/Invoke target enables the traditional semantics of
    // resolving the P/Invoke target at the callsite. Providing a non-indirect
    // fixup indicates the P/Invoke target will be resolved when the enclosing
    // function is compiled. This subtle semantic difference is chosen for
    // scenarios when resolution of the target must occur under a specific GC mode.
    //

    void *fixup = NULL;
    ZapImport *pImport = NULL;
    if (m_pImage->m_pPreloader->ShouldSuppressGCTransition(method))
    {
        pImport = m_pImage->GetImportTable()->GetPInvokeTargetImport(method);
        fixup = pImport;
    }
    else
    {
        pImport = m_pImage->GetImportTable()->GetIndirectPInvokeTargetImport(method);
        *ppIndirection = pImport;
    }

    _ASSERTE(pImport != NULL);
    AppendConditionalImport(pImport);

    return fixup;
}

void ZapInfo::getAddressOfPInvokeTarget(CORINFO_METHOD_HANDLE method, CORINFO_CONST_LOOKUP *pLookup)
{
    _ASSERTE(pLookup != NULL);

    void * pIndirection;
    void * pResult = getAddressOfPInvokeFixup(method, &pIndirection);
    if (pResult != NULL)
    {
        pLookup->accessType = IAT_PVALUE;
        pLookup->addr = pResult;
        return;
    }

    _ASSERTE(pIndirection != NULL);
    pLookup->accessType = IAT_PPVALUE;
    pLookup->addr = pIndirection;
}

CORINFO_JUST_MY_CODE_HANDLE ZapInfo::getJustMyCodeHandle(
    CORINFO_METHOD_HANDLE method,
    CORINFO_JUST_MY_CODE_HANDLE **ppIndirection)
{
    _ASSERTE(ppIndirection != NULL);

    if (IsReadyToRunCompilation())
    {
        *ppIndirection = NULL;
        return NULL;
    }

    *ppIndirection = (CORINFO_JUST_MY_CODE_HANDLE *)m_pImage->GetInnerPtr(m_pImage->m_pEEInfoTable,
        offsetof(CORCOMPILE_EE_INFO_TABLE, addrOfJMCFlag));
    return NULL;
}

ZapImport * ZapInfo::GetProfilingHandleImport()
{
    if (m_pProfilingHandle == NULL)
    {
        ZapImport * pImport = m_pImage->GetImportTable()->GetProfilingHandleImport(m_currentMethodHandle);
        AppendImport(pImport);

        m_pProfilingHandle = pImport;
    }

    return m_pProfilingHandle;
}

void ZapInfo::GetProfilingHandle(BOOL                      *pbHookFunction,
                                 void                     **pProfilerHandle,
                                 BOOL                      *pbIndirectedHandles)
{
    //
    // Return the location within the fixup table
    //
    // Profiling handle is opaque token. It does not have to be aligned thus we can not store it in the same location as token.
    //
    *pProfilerHandle = m_pImage->GetInnerPtr(GetProfilingHandleImport(), kZapProfilingHandleImportValueIndexClientData * TARGET_POINTER_SIZE);

    // All functions get hooked in ngen /Profile
    *pbHookFunction = TRUE;

    //
    // This is the NGEN case, where we always do indirection on the handle so we can fix it up at load time.
    //
    *pbIndirectedHandles = TRUE;
}

//
// This strips the CORINFO_FLG_JIT_INTRINSIC flag from some of the named intrinsic methods.
//
DWORD FilterNamedIntrinsicMethodAttribs(ZapInfo* pZapInfo, DWORD attribs, CORINFO_METHOD_HANDLE ftn, ICorDynamicInfo* pJitInfo)
{
    if (attribs & CORINFO_FLG_JIT_INTRINSIC)
    {
        // Figure out which intrinsic we are dealing with.
        const char* namespaceName;
        const char* className;
        const char* enclosingClassName;
        const char* methodName = pJitInfo->getMethodNameFromMetadata(ftn, &className, &namespaceName, &enclosingClassName);

        // Is this the get_IsSupported method that checks whether intrinsic is supported?
        bool fIsGetIsSupportedMethod   = strcmp(methodName, "get_IsSupported") == 0;
        bool fIsPlatformHWIntrinsic    = false;
        bool fIsHWIntrinsic            = false;
        bool fTreatAsRegularMethodCall = false;

#if defined(TARGET_X86) || defined(TARGET_AMD64)
        fIsPlatformHWIntrinsic = strcmp(namespaceName, "System.Runtime.Intrinsics.X86") == 0;
#elif defined(TARGET_ARM64)
        fIsPlatformHWIntrinsic = strcmp(namespaceName, "System.Runtime.Intrinsics.Arm") == 0;
#endif

        fIsHWIntrinsic = fIsPlatformHWIntrinsic || (strcmp(namespaceName, "System.Runtime.Intrinsics") == 0);

        // By default, we want to treat the get_IsSupported method for platform specific HWIntrinsic ISAs as
        // method calls. This will be modified as needed below based on what ISAs are considered baseline.
        //
        // We also want to treat the non-platform specific hardware intrinsics as regular method calls. This
        // is because they often change the code they emit based on what ISAs are supported by the compiler,
        // but we don't know what the target machine will support.
        //
        // Additionally, we make sure none of the hardware intrinsic method bodies get pregenerated in crossgen
        // (see ZapInfo::CompileMethod) but get JITted instead. The JITted method will have the correct
        // answer for the CPU the code is running on.

        fTreatAsRegularMethodCall = fIsGetIsSupportedMethod && fIsPlatformHWIntrinsic;

#if defined(TARGET_ARM64)
        // On Arm64 AdvSimd ISA is required by CoreCLR, so we can expand Vector64<T> and Vector128<T> generic methods (e.g. Vector64<byte>.get_Zero)
        // as well as Vector64 and Vector128 methods (e.g. Vector128.CreateScalarUnsafe).
        fTreatAsRegularMethodCall |= !fIsPlatformHWIntrinsic && fIsHWIntrinsic
            && (strncmp(className, "Vector64", _countof("Vector64") - 1) != 0)
            && (strncmp(className, "Vector128", _countof("Vector128") - 1) != 0);
#else
        fTreatAsRegularMethodCall |= !fIsPlatformHWIntrinsic && fIsHWIntrinsic;
#endif 

        if (fIsPlatformHWIntrinsic)
        {
            // Simplify the comparison logic by grabbing the name of the ISA
            const char* isaName = (enclosingClassName == nullptr) ? className : enclosingClassName;

            bool fIsPlatformRequiredISA     = false;
            bool fIsPlatformSubArchitecture = false;

#if defined(TARGET_X86) || defined(TARGET_AMD64)
            fIsPlatformRequiredISA     = (strcmp(isaName, "X86Base") == 0) || (strcmp(isaName, "Sse") == 0) || (strcmp(isaName, "Sse2") == 0);
            fIsPlatformSubArchitecture = strcmp(className, "X64") == 0;
#elif defined(TARGET_ARM64)
            fIsPlatformRequiredISA     = (strcmp(isaName, "ArmBase") == 0) || (strcmp(isaName, "AdvSimd") == 0);
            fIsPlatformSubArchitecture = strcmp(className, "Arm64") == 0;
#endif

            if (fIsPlatformRequiredISA)
            {
                if ((enclosingClassName == nullptr) || fIsPlatformSubArchitecture)
                {
                    // If the ISA is required by CoreCLR for the platform, we can expand unconditionally
                    fTreatAsRegularMethodCall = false;
                }
            }
#if defined(TARGET_X86) || defined(TARGET_AMD64)
            else if ((strcmp(isaName, "Avx") == 0) || (strcmp(isaName, "Fma") == 0) || (strcmp(isaName, "Avx2") == 0)
                     || (strcmp(isaName, "Bmi1") == 0) || (strcmp(isaName, "Bmi2") == 0) || (strcmp(isaName, "Lzcnt") == 0))
            {
                if ((enclosingClassName == nullptr) || fIsPlatformSubArchitecture)
                {
                    // If it is the get_IsSupported method for an ISA which is intentionally not enabled
                    // for crossgen, we want to expand unconditionally. This will force those code
                    // paths to be treated as dead code and dropped from the compilation.
                    // See Zapper::InitializeCompilerFlags
                    //
                    // For all of the other intrinsics in an ISA which requires the VEX encoding
                    // we need to treat them as regular method calls. This is done because RyuJIT
                    // doesn't currently support emitting both VEX and non-VEX encoded instructions
                    // for a single method.
                    fTreatAsRegularMethodCall = !fIsGetIsSupportedMethod;
                }
            }
#endif // defined(TARGET_X86) || defined(TARGET_AMD64)
#ifdef TARGET_X86
            else if (fIsPlatformSubArchitecture)
            {
                // For ISAs not handled explicitly above, the IsSupported check will always
                // be treated as a regular method call. If we are evaulating a method in the X64
                // namespace, we know it will never be supported on x86, so we can allow the code
                // to be treated as dead. We treat all non-IsSupported methods as regular method
                // calls so they throw PNSE if used withoug the IsSupported check.
                fTreatAsRegularMethodCall = !fIsGetIsSupportedMethod;
            }
#endif // TARGET_X86
        }
#if defined(TARGET_X86) || defined(TARGET_AMD64)
        else if (strcmp(namespaceName, "System") == 0)
        {
            if (strcmp(className, "Math") == 0 || strcmp(className, "MathF") == 0)
            {
                // These are normally handled via the SSE4.1 instructions ROUNDSS/ROUNDSD.
                // However, we don't know the ISAs the target machine supports so we should
                // fallback to the method call implementation instead.
                fTreatAsRegularMethodCall = strcmp(methodName, "Round") == 0 || strcmp(methodName, "Ceiling") == 0 ||
                    strcmp(methodName, "Floor") == 0;
            }
        }
        else if (strcmp(namespaceName, "System.Numerics") == 0)
        {
            if ((strcmp(className, "Vector3") == 0) || (strcmp(className, "Vector4") == 0))
            {
                // Vector3 and Vector4 have constructors which take a smaller Vector and create bolt on
                // a larger vector. This uses insertps instruction when compiled with SSE4.1 instruction support
                // which must not be generated inline in R2R images that actually support an SSE2 only mode.
                if (strcmp(methodName, ".ctor") == 0)
                {
                    CORINFO_SIG_INFO sig;
                    pZapInfo->getMethodSig(ftn, &sig, NULL);
                    CORINFO_CLASS_HANDLE argClass;
                    if ((CorInfoType)pZapInfo->getArgType(&sig, sig.args, &argClass) == CORINFO_TYPE_VALUECLASS)
                    {
                        fTreatAsRegularMethodCall = TRUE;
                    }
                }
                else if (strcmp(methodName, "Dot") == 0)
                {
                    // The dot product operations uses the dpps instruction when compiled with SSE4.1 instruction
                    // support. This must not be generated inline in R2R images that actually support an SSE2 only mode.
                    fTreatAsRegularMethodCall = TRUE;
                }
            }
            else if ((strcmp(className, "Vector2") == 0) || (strcmp(className, "Vector") == 0) || (strcmp(className, "Vector`1") == 0))
            {
                if (strcmp(methodName, "Dot") == 0)
                {
                    // The dot product operations uses the dpps instruction when compiled with SSE4.1 instruction
                    // support. This must not be generated inline in R2R images that actually support an SSE2 only mode.
                    fTreatAsRegularMethodCall = TRUE;
                }
            }
        }
#endif // defined(TARGET_X86) || defined(TARGET_AMD64)

        if (fTreatAsRegularMethodCall)
        {
            // Treat as a regular method call (into a JITted method).
            attribs = (attribs & ~CORINFO_FLG_JIT_INTRINSIC) | CORINFO_FLG_DONT_INLINE;
        }
    }

    return attribs;
}

//return a callable stub that will do the virtual or interface call


void ZapInfo::getCallInfo(CORINFO_RESOLVED_TOKEN * pResolvedToken,
                          CORINFO_RESOLVED_TOKEN * pConstrainedResolvedToken,
                          CORINFO_METHOD_HANDLE   callerHandle,
                          CORINFO_CALLINFO_FLAGS  flags,
                          CORINFO_CALL_INFO       *pResult)
{
    void * pTarget = NULL;

    _ASSERTE(pResult);

    // Fill in the kind of the virtual call.
    // We set kindOnly=true since we don't want the EE to actually give us
    // a call stub - instead we want to generate an indirection ourselves.
    m_pEEJitInfo->getCallInfo(pResolvedToken,
                              pConstrainedResolvedToken,
                              callerHandle,
                              /* REVISIT_TODO
                               * Addition of this flag.
                               */
                              (CORINFO_CALLINFO_FLAGS)(flags | CORINFO_CALLINFO_KINDONLY),
                              pResult);

    pResult->methodFlags = FilterNamedIntrinsicMethodAttribs(this, pResult->methodFlags, pResult->hMethod, m_pEEJitInfo);

#ifdef FEATURE_READYTORUN_COMPILER
    if (IsReadyToRunCompilation())
    {
        if (pResult->sig.isVarArg())
        {
            if (m_zapper->m_pOpt->m_verbose)
                m_zapper->Warning(W("ReadyToRun: VarArg methods not supported\n"));
            ThrowHR(E_NOTIMPL);
        }

        if (pResult->accessAllowed != CORINFO_ACCESS_ALLOWED)
        {
            if (m_zapper->m_pOpt->m_verbose)
                m_zapper->Warning(W("ReadyToRun: Runtime method access checks not supported\n"));
            ThrowHR(E_NOTIMPL);
        }
    }
#endif

#ifdef TARGET_X86
    if (GetCompileInfo()->IsUnmanagedCallersOnlyMethod(pResult->hMethod))
    {
        if (m_zapper->m_pOpt->m_verbose)
            m_zapper->Warning(W("ReadyToRun: References to methods with UnmanagedCallersOnlyAttribute not implemented\n"));
        ThrowHR(E_NOTIMPL);
    }
#endif // TARGET_X86

    if (flags & CORINFO_CALLINFO_KINDONLY)
        return;

    if (IsReadyToRunCompilation())
    {
        if (pResult->thisTransform == CORINFO_BOX_THIS)
        {
            // READYTORUN: FUTURE: Optionally create boxing stub at runtime
            // We couldn't resolve the constrained call into a valuetype instance method and we're asking the JIT
            // to box and do a virtual dispatch. If we were to allow the boxing to happen now, it could break future code
            // when the user adds a method to the valuetype that makes it possible to avoid boxing (if there is state
            // mutation in the method).

            // We allow this at least for primitives and enums because we control them
            // and we know there's no state mutation.
            CorInfoType constrainedType = getTypeForPrimitiveValueClass(pConstrainedResolvedToken->hClass);
            if (constrainedType == CORINFO_TYPE_UNDEF)
                ThrowHR(E_NOTIMPL);
        }
    }

    // OK, if the EE said we're not doing a stub dispatch then just return the kind to
    // the caller.  No other kinds of virtual calls have extra information attached.
    switch (pResult->kind)
    {
    case CORINFO_VIRTUALCALL_STUB:
        {
            if (pResult->stubLookup.lookupKind.needsRuntimeLookup)
            {
				if (!IsReadyToRunCompilation())
					embedGenericSignature(&pResult->stubLookup);
                return;
            }

            if (IsReadyToRunCompilation())
            {
                ZapImport * pImport = m_pImage->GetImportTable()->GetStubDispatchCell(pResolvedToken);

                pResult->stubLookup.constLookup.accessType   = IAT_PVALUE;
                pResult->stubLookup.constLookup.addr         = pImport;
            }
            else
            {

                CORINFO_CLASS_HANDLE calleeOwner = pResolvedToken->hClass;
                CORINFO_METHOD_HANDLE callee = pResolvedToken->hMethod;
                _ASSERTE(callee == pResult->hMethod);

                //
                // Create the indirection cell
                //
                pTarget = m_pImage->GetImportTable()->GetStubDispatchCell(calleeOwner, callee);

                pResult->stubLookup.constLookup.accessType = IAT_PVALUE;
                pResult->stubLookup.constLookup.addr = pTarget;
            }
        }
        break;


    case CORINFO_CALL_CODE_POINTER:
        _ASSERTE(pResult->codePointerLookup.lookupKind.needsRuntimeLookup);
		if (!IsReadyToRunCompilation())
			embedGenericSignature(&pResult->codePointerLookup);

        // There is no easy way to detect method referenced via generic lookups in generated code.
        // Report this method reference unconditionally.
        m_pImage->m_pPreloader->MethodReferencedByCompiledCode(pResult->hMethod);
        return;

    case CORINFO_CALL:
        if (IsReadyToRunCompilation())
        {
            // Constrained token is not interesting with this transforms
            if (pResult->thisTransform != CORINFO_NO_THIS_TRANSFORM)
                pConstrainedResolvedToken = NULL;

            ZapImport * pImport;

            if (flags & (CORINFO_CALLINFO_LDFTN | CORINFO_CALLINFO_ATYPICAL_CALLSITE))
            {
                pImport = m_pImage->GetImportTable()->GetMethodImport(ENCODE_METHOD_ENTRY, pResult->hMethod, pResolvedToken, pConstrainedResolvedToken);

                AppendConditionalImport(pImport);
            }
            else
            {
                if (pResult->methodFlags & CORINFO_FLG_INTRINSIC)
                {
                    bool unused;
                    CorInfoIntrinsics intrinsic = getIntrinsicID(pResult->hMethod, &unused);
                    if ((intrinsic == CORINFO_INTRINSIC_StubHelpers_GetStubContext)
                     || (intrinsic == CORINFO_INTRINSIC_StubHelpers_GetStubContextAddr)
                     )
                    {
                        // These intrinsics are always expanded directly in the jit and do not correspond to external methods
                        return;
                    }
                }
                pImport = m_pImage->GetImportTable()->GetExternalMethodCell(pResult->hMethod, pResolvedToken, pConstrainedResolvedToken);
            }

            // READYTORUN: FUTURE: Direct calls if possible
            pResult->codePointerLookup.constLookup.accessType   = IAT_PVALUE;
            pResult->codePointerLookup.constLookup.addr         = pImport;
        }
        break;

    case CORINFO_VIRTUALCALL_VTABLE:
        // Only calls within the CoreLib version bubble support fragile NI codegen with vtable based calls, for better performance (because
        // CoreLib and the runtime will always be updated together anyways - this is a special case)
        break;

    case CORINFO_VIRTUALCALL_LDVIRTFTN:
#ifdef FEATURE_READYTORUN_COMPILER
		if (IsReadyToRunCompilation() && !pResult->exactContextNeedsRuntimeLookup)
		{
			DWORD fAtypicalCallsite = (flags & CORINFO_CALLINFO_ATYPICAL_CALLSITE) ? CORINFO_HELP_READYTORUN_ATYPICAL_CALLSITE : 0;

			ZapImport * pImport = m_pImage->GetImportTable()->GetDynamicHelperCell(
				(CORCOMPILE_FIXUP_BLOB_KIND)(ENCODE_VIRTUAL_ENTRY | fAtypicalCallsite), pResult->hMethod, pResolvedToken);

			pResult->codePointerLookup.constLookup.accessType = IAT_PVALUE;
			pResult->codePointerLookup.constLookup.addr = pImport;

			_ASSERTE(!pResult->sig.hasTypeArg());
		}
#endif

        // Include the declaring instantiation of virtual generic methods in the NGen image.
        m_pImage->m_pPreloader->AddMethodToTransitiveClosureOfInstantiations(pResult->hMethod);
        break;

    default:
        _ASSERTE(!"Unknown call type");
        break;
    }

    if (IsReadyToRunCompilation() && pResult->sig.hasTypeArg())
    {
        if (pResult->exactContextNeedsRuntimeLookup)
        {
			// Nothing to do... The generic handle lookup gets embedded in to the codegen
			// during the jitting of the call.
			// (Note: The generic lookup in R2R is performed by a call to a helper at runtime, not by
			// codegen emitted at crossgen time)
        }
        else
        {
            ZapImport * pImport;
            if (((SIZE_T)pResult->contextHandle & CORINFO_CONTEXTFLAGS_MASK) == CORINFO_CONTEXTFLAGS_METHOD)
            {
                CORINFO_METHOD_HANDLE exactMethodHandle = (CORINFO_METHOD_HANDLE)((SIZE_T)pResult->contextHandle & ~CORINFO_CONTEXTFLAGS_MASK);

                pImport = m_pImage->GetImportTable()->GetMethodImport(ENCODE_METHOD_DICTIONARY, exactMethodHandle,
                    pResolvedToken, pConstrainedResolvedToken);
            }
            else
            {
                pImport = m_pImage->GetImportTable()->GetClassImport(ENCODE_TYPE_DICTIONARY,
                    (pConstrainedResolvedToken != NULL) ? pConstrainedResolvedToken : pResolvedToken);
            }

            pResult->instParamLookup.accessType   = IAT_PVALUE;
            pResult->instParamLookup.addr         = pImport;

            AppendConditionalImport(pImport);
        }
    }
}

BOOL ZapInfo::canAccessFamily(CORINFO_METHOD_HANDLE hCaller,
                              CORINFO_CLASS_HANDLE hInstanceType)
{
    return m_pEEJitInfo->canAccessFamily(hCaller, hInstanceType);
}

BOOL ZapInfo::isRIDClassDomainID (CORINFO_CLASS_HANDLE cls)
{
    return m_pEEJitInfo->isRIDClassDomainID(cls);
}

unsigned ZapInfo::getClassDomainID (CORINFO_CLASS_HANDLE cls, void **ppIndirection)
{
    _ASSERTE(ppIndirection != NULL);

    m_pImage->m_pPreloader->AddTypeToTransitiveClosureOfInstantiations(cls);

    if (!m_zapper->m_pOpt->m_compilerFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_CODE))
    {
        if (isRIDClassDomainID(cls))
        {
            // Token is invariant to loading order, so we can go ahead and use it

            // Ensure that 'cls' gets added to CORCOMPILE_LOAD_TABLE, unless
            // someone else adds some other type of fixup for 'cls'.
            m_ClassLoadTable.Load(cls, FALSE);

            return m_pEEJitInfo->getClassDomainID(cls, ppIndirection);
        }

        if (m_pImage->m_pPreloader->CanEmbedClassID(cls))
        {
            // Ensure that 'cls' gets added to CORCOMPILE_LOAD_TABLE, unless
            // someone else adds some other type of fixup for 'cls'.
            m_ClassLoadTable.Load(cls, FALSE);
            return m_pEEJitInfo->getClassDomainID(cls, ppIndirection);
        }
    }

    // We will have to insert a fixup
    ZapImport * pImport = m_pImage->GetImportTable()->GetClassDomainIdImport(cls);
    AppendConditionalImport(pImport);

    *ppIndirection = pImport;
    return NULL;
}

void * ZapInfo::getFieldAddress(CORINFO_FIELD_HANDLE field, void **ppIndirection)
{
    if (IsReadyToRunCompilation())
    {
        void * pAddress = m_pEEJitInfo->getFieldAddress(field, ppIndirection);

        return m_pImage->m_pILMetaData->GetRVAField(pAddress);
    }

    _ASSERTE(ppIndirection != NULL);

    CORINFO_CLASS_HANDLE hClass = m_pEEJitInfo->getFieldClass(field);

    m_pImage->m_pPreloader->AddTypeToTransitiveClosureOfInstantiations(hClass);

    ZapImport * pImport = m_pImage->GetImportTable()->GetStaticFieldAddressImport(field);
    AppendConditionalImport(pImport);

    // Field address is not aligned thus we can not store it in the same location as token.
    *ppIndirection = m_pImage->GetInnerPtr(pImport, TARGET_POINTER_SIZE);

    return NULL;
}

CORINFO_CLASS_HANDLE ZapInfo::getStaticFieldCurrentClass(CORINFO_FIELD_HANDLE field, bool* pIsSpeculative)
{
    if (pIsSpeculative != NULL)
    {
        *pIsSpeculative = true;
    }

    return NULL;
}

DWORD ZapInfo::getFieldThreadLocalStoreID(CORINFO_FIELD_HANDLE field,
                                          void **ppIndirection)
{
    _ASSERTE(ppIndirection != NULL);

    *ppIndirection = m_pImage->GetInnerPtr(m_pImage->m_pEEInfoTable,
        offsetof(CORCOMPILE_EE_INFO_TABLE, rvaStaticTlsIndex));
    return NULL;
}

CORINFO_VARARGS_HANDLE ZapInfo::getVarArgsHandle(CORINFO_SIG_INFO *sig,
                                                 void **ppIndirection)
{
    _ASSERTE(ppIndirection != NULL);

    // Zapper does not support embedding these as they are created dynamically

    if (sig->scope != m_pImage->m_hModule || sig->token == mdTokenNil)
    {
        _ASSERTE(!"Don't have enough info to be able to create a sig token.");

        *ppIndirection = NULL;
        return NULL;
    }

    // @perf: If the sig cookie construction code actually will restore the value types in
    // the sig, we should call LoadClass on all of those types to avoid redundant
    // restore cookies.

    ZapImport * pImport = m_pImage->GetImportTable()->GetVarArgImport(sig->scope, sig->token);
    AppendConditionalImport(pImport);

    *ppIndirection = pImport;
    return NULL;
}

bool ZapInfo::canGetVarArgsHandle(CORINFO_SIG_INFO *sig)
{
    // Zapper does not support embedding these as they are created dynamically
    if (sig->scope != m_pImage->m_hModule || sig->token == mdTokenNil)
    {
        return false;
    }

    return true;
}

void ZapInfo::setOverride(ICorDynamicInfo *pOverride, CORINFO_METHOD_HANDLE currentMethod)
{
    UNREACHABLE();
}

void ZapInfo::addActiveDependency(CORINFO_MODULE_HANDLE moduleFrom, CORINFO_MODULE_HANDLE moduleTo)
{
    if (IsReadyToRunCompilation())
        return;

    _ASSERT(moduleFrom != moduleTo);

    if (m_pImage->m_pPreloader->CanSkipDependencyActivation(m_currentMethodHandle, moduleFrom, moduleTo))
    {
        // No need to add dependency fixup since we will have an unconditional dependency
        // already
    }
    else if (!GetCompileInfo()->IsInCurrentVersionBubble(moduleTo))
    {
    }
    else
    {
        ZapImport * pImport = m_pImage->GetImportTable()->GetActiveDependencyImport(moduleFrom, moduleTo);
        AppendImport(pImport);

        CORINFO_DEPENDENCY dep;
        dep.moduleFrom = moduleFrom;
        dep.moduleTo   = moduleTo;
    }
}


InfoAccessType
    ZapInfo::constructStringLiteral(CORINFO_MODULE_HANDLE tokenScope,
                                         unsigned metaTok, void **ppValue)
{
    if (m_pEECompileInfo->IsEmptyString(metaTok, tokenScope))
    {
        return emptyStringLiteral(ppValue);
    }

    ZapImport * pImport = m_pImage->GetImportTable()->GetStringHandleImport(tokenScope, metaTok);
    AppendConditionalImport(pImport);

    *ppValue = pImport;

    return IAT_PPVALUE;
}

InfoAccessType ZapInfo::emptyStringLiteral(void **ppValue)
{
#ifdef FEATURE_READYTORUN_COMPILER
    if (IsReadyToRunCompilation())
    {
        ZapImport * pImport = m_pImage->GetImportTable()->GetStringHandleImport(m_pImage->m_hModule, mdtString);
        *ppValue = pImport;
        return IAT_PPVALUE;
    }
#endif

    *ppValue = m_pImage->GetInnerPtr(m_pImage->m_pEEInfoTable,
        offsetof(CORCOMPILE_EE_INFO_TABLE, emptyString));

    return IAT_PPVALUE;
}

void ZapInfo::recordCallSite(ULONG instrOffset, CORINFO_SIG_INFO *callSig, CORINFO_METHOD_HANDLE methodHandle)
{
    return;
}

void ZapInfo::recordRelocation(void *location, void *target,
                               WORD fRelocType, WORD slotNum, INT32 addlDelta)
{
    // Factor slotNum into the location address
    switch (fRelocType)
    {
    case IMAGE_REL_BASED_ABSOLUTE:
    case IMAGE_REL_BASED_PTR:
#if defined(TARGET_X86) || defined(TARGET_AMD64)
    case IMAGE_REL_BASED_REL32:
#endif // TARGET_X86 || TARGET_AMD64
        location = (PBYTE)location + slotNum;
        break;

#if defined(TARGET_ARM)
    case IMAGE_REL_BASED_THUMB_MOV32:
    case IMAGE_REL_BASED_REL_THUMB_MOV32_PCREL:
    case IMAGE_REL_BASED_THUMB_BRANCH24:

# ifdef _DEBUG
    {
        CORJIT_FLAGS jitFlags = m_zapper->m_pOpt->m_compilerFlags;

        if (jitFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_RELATIVE_CODE_RELOCS))
        {
            _ASSERTE(fRelocType == IMAGE_REL_BASED_REL_THUMB_MOV32_PCREL
                     || fRelocType == IMAGE_REL_BASED_THUMB_BRANCH24);
        }
        else
        {
            _ASSERTE(fRelocType == IMAGE_REL_BASED_THUMB_MOV32
                     || fRelocType == IMAGE_REL_BASED_THUMB_BRANCH24);
        }
    }
# endif // _DEBUG
        break;
#endif

#if defined(TARGET_ARM64)
    case IMAGE_REL_ARM64_BRANCH26:
    case IMAGE_REL_ARM64_PAGEBASE_REL21:
    case IMAGE_REL_ARM64_PAGEOFFSET_12A:
        break;
#endif

    default:
        _ASSERTE(!"Unknown reloc type");
        break;
    }

    ZapBlobWithRelocs * knownNodes[] =
    {
        m_pCode,
        m_pColdCode,
        m_pROData,
        m_pProfileData
    };

    //
    // The location of the relocation reported by the JIT has to fall into one of the code or data blobs
    //

    CodeRelocation r;

    ZapBlobWithRelocs * pSrcNode = NULL;
    for (size_t i = 0; i < _countof(knownNodes); i++)
    {
        ZapBlobWithRelocs * pNode = knownNodes[i];
        if (pNode == NULL)
            continue;

        if (pNode->GetData() <= location && location < pNode->GetData() + pNode->GetSize())
        {
            pSrcNode = pNode;
            break;
        }
    }
    PREFIX_ASSUME(pSrcNode != NULL);
    r.m_pNode = pSrcNode;
    r.m_offset = (DWORD)((PBYTE)location - (PBYTE)pSrcNode->GetData());

    //
    // The target of the relocation reported by the JIT can be one of:
    //  - Inner pointer into into one of the code or data blobs. We can detect this case by searching
    //    through the blobs.
    //  - Hardbound target. We can detect this case by searching through all hardbound assemblies.
    //  - Otherwise, it has to be ZapNode *.
    //

    ZapNode * pTargetNode = NULL;
    INT32 targetOffset = 0;
    for (size_t i = 0; i < _countof(knownNodes); i++)
    {
        ZapBlobWithRelocs * pNode = knownNodes[i];
        if (pNode == NULL)
            continue;

        if (pNode->GetData() <= target && target < pNode->GetData() + pNode->GetSize())
        {
            pTargetNode = pNode;
            targetOffset = (INT32)((PBYTE)target - (PBYTE)pNode->GetData());
            break;
        }
    }

    if (pTargetNode != NULL)
    {
        r.m_pTargetNode = pTargetNode;
    }
    else
    {
        // Must be ZapNode otherwise
        pTargetNode = (ZapNode *)target;
        _ASSERTE(pTargetNode->GetType() != ZapNodeType_Unknown);
        r.m_pTargetNode = pTargetNode;
    }

    r.m_type = (ZapRelocationType)fRelocType;

    switch (fRelocType)
    {
    case IMAGE_REL_BASED_ABSOLUTE:
        *(UNALIGNED DWORD *)location = (DWORD)targetOffset;
        break;

    case IMAGE_REL_BASED_PTR:
        *(UNALIGNED TARGET_POINTER_TYPE *)location = (TARGET_POINTER_TYPE)targetOffset;
        break;

#if defined(TARGET_X86) || defined(TARGET_AMD64)
    case IMAGE_REL_BASED_REL32:
        *(UNALIGNED INT32 *)location = targetOffset + addlDelta;
        break;
#endif // TARGET_X86 || TARGET_AMD64

#if defined(TARGET_ARM)
    case IMAGE_REL_BASED_THUMB_MOV32:
    case IMAGE_REL_BASED_REL_THUMB_MOV32_PCREL:
        PutThumb2Mov32((UINT16 *)location, targetOffset);
        break;

    case IMAGE_REL_BASED_THUMB_BRANCH24:
        if (!FitsInThumb2BlRel24(targetOffset))
            ThrowHR(COR_E_OVERFLOW);
        PutThumb2BlRel24((UINT16 *)location, targetOffset);
        break;
#endif

#if defined(TARGET_ARM64)
    case IMAGE_REL_ARM64_BRANCH26:
        if (!FitsInRel28(targetOffset))
            ThrowHR(COR_E_OVERFLOW);
        PutArm64Rel28((UINT32 *)location, targetOffset);
        break;
    case IMAGE_REL_ARM64_PAGEBASE_REL21:
        if (!FitsInRel21(targetOffset))
            ThrowHR(COR_E_OVERFLOW);
        PutArm64Rel21((UINT32 *)location, targetOffset);
        break;

    case IMAGE_REL_ARM64_PAGEOFFSET_12A:
        if (!FitsInRel12(targetOffset))
            ThrowHR(COR_E_OVERFLOW);
        PutArm64Rel12((UINT32 *)location, targetOffset);
        break;
#endif

    default:
        _ASSERTE(!"Unknown reloc type");
        break;
    }

    if (m_CodeRelocations.IsEmpty())
    {
        SIZE_T totalCodeSize = m_pCode->GetSize() + ((m_pColdCode != NULL) ? m_pColdCode->GetSize() : 0);

        // Prealocate relocations (assume that every other pointer may need relocation)
        COUNT_T nEstimatedRelocations = (COUNT_T)(totalCodeSize / (2 * TARGET_POINTER_SIZE));
        if (nEstimatedRelocations > 1)
            m_CodeRelocations.Preallocate(nEstimatedRelocations);
    }

    m_CodeRelocations.Append(r);
}

WORD ZapInfo::getRelocTypeHint(void * target)
{
#ifdef TARGET_AMD64
    // There should be no external pointers
    return IMAGE_REL_BASED_REL32;
#elif defined(TARGET_ARM)
    // Use full 32-bit branch targets when retrying compilation on ARM
    if (m_zapper->m_pOpt->m_fNGenLastRetry)
        return (WORD)-1;
    return IMAGE_REL_BASED_THUMB_BRANCH24;
#elif defined(TARGET_ARM64)
    return IMAGE_REL_ARM64_BRANCH26;
#else
    // No hints
    return (WORD)-1;
#endif
}

DWORD ZapInfo::getExpectedTargetArchitecture()
{
    return IMAGE_FILE_MACHINE_NATIVE;
}

CORINFO_METHOD_HANDLE ZapInfo::GetDelegateCtor(CORINFO_METHOD_HANDLE   methHnd,
                                               CORINFO_CLASS_HANDLE    clsHnd,
                                               CORINFO_METHOD_HANDLE   targetMethodHnd,
                                               DelegateCtorArgs *      pCtorData)
{
    // For ReadyToRun, this optimization is done via ZapInfo::getReadyToRunDelegateCtorHelper
    if (IsReadyToRunCompilation())
        return methHnd;

    // forward the call to the standard GetDelegateCtor
    CORINFO_METHOD_HANDLE delegateCtor = m_pEEJitInfo->GetDelegateCtor(methHnd, clsHnd, targetMethodHnd, pCtorData);
    if (delegateCtor != methHnd)
    {
        if (pCtorData->pArg4)
        {
            // cannot optimize any wrapper delegate, give up
            delegateCtor = methHnd;
        }
        else if (pCtorData->pArg3)
        {
            pCtorData->pArg3 = m_pImage->GetWrappers()->GetStub(pCtorData->pArg3);
        }
    }
    return delegateCtor;
}

void ZapInfo::MethodCompileComplete(
            CORINFO_METHOD_HANDLE methHnd)
{
    m_pEEJitInfo->MethodCompileComplete(methHnd);
}


//
// ICorStaticInfo
//

void ZapInfo::getEEInfo(CORINFO_EE_INFO *pEEInfoOut)
{
    m_pEEJitInfo->getEEInfo(pEEInfoOut);
}

LPCWSTR ZapInfo::getJitTimeLogFilename()
{
    return m_pEEJitInfo->getJitTimeLogFilename();
}

//
// ICorArgInfo
//

CORINFO_ARG_LIST_HANDLE ZapInfo::getArgNext(CORINFO_ARG_LIST_HANDLE args)
{
    return m_pEEJitInfo->getArgNext(args);
}

CorInfoTypeWithMod ZapInfo::getArgType(CORINFO_SIG_INFO* sig,
                                               CORINFO_ARG_LIST_HANDLE args,
                                                CORINFO_CLASS_HANDLE *vcTypeRet)
{
    return m_pEEJitInfo->getArgType(sig, args, vcTypeRet);
}

CORINFO_CLASS_HANDLE ZapInfo::getArgClass(CORINFO_SIG_INFO* sig,
                                           CORINFO_ARG_LIST_HANDLE args)
{
    return m_pEEJitInfo->getArgClass(sig, args);
}

CorInfoHFAElemType ZapInfo::getHFAType(CORINFO_CLASS_HANDLE hClass)
{
    return m_pEEJitInfo->getHFAType(hClass);
}

//
// ICorDebugInfo
//

void ZapInfo::getBoundaries(CORINFO_METHOD_HANDLE ftn, unsigned int *cILOffsets,
                             DWORD **pILOffsets, ICorDebugInfo::BoundaryTypes *implicitBoundaries)
{
    m_pEEJitInfo->getBoundaries(ftn, cILOffsets, pILOffsets,
                                              implicitBoundaries);
}

void ZapInfo::setBoundaries(CORINFO_METHOD_HANDLE ftn, ULONG32 cMap,
                                           ICorDebugInfo::OffsetMapping *pMap)
{
    _ASSERTE(ftn == m_currentMethodHandle);

    if (cMap == 0)
        return;

    m_pOffsetMapping = pMap;
    m_iOffsetMapping = cMap;
    return;
}

void ZapInfo::getVars(CORINFO_METHOD_HANDLE ftn,
                                    ULONG32 *cVars,
                                    ICorDebugInfo::ILVarInfo **vars,
                                    bool *extendOthers)
{
    m_pEEJitInfo->getVars(ftn, cVars, vars, extendOthers);
}

void ZapInfo::setVars(CORINFO_METHOD_HANDLE ftn,
                                    ULONG32 cVars,
                                    ICorDebugInfo::NativeVarInfo * vars)
{
    _ASSERTE(ftn == m_currentMethodHandle);

    if (cVars == 0)
        return;

    m_pNativeVarInfo = vars;
    m_iNativeVarInfo = cVars;

    return;
}

void ZapInfo::setPatchpointInfo(PatchpointInfo* patchpointInfo)
{
    // No patchpoint info when prejitting
    UNREACHABLE();
}

PatchpointInfo* ZapInfo::getOSRInfo(unsigned * ilOffset)
{
    // No patchpoint info when prejitting
    UNREACHABLE();
}

void * ZapInfo::allocateArray(size_t cBytes)
{
    return new BYTE[cBytes];
}

void ZapInfo::freeArray(void *array)
{
    delete [] ((BYTE*) array);
}

//
// ICorFieldInfo
//

const char* ZapInfo::getFieldName(CORINFO_FIELD_HANDLE ftn, const char **moduleName)
{
    return m_pEEJitInfo->getFieldName(ftn, moduleName);
}

CORINFO_CLASS_HANDLE ZapInfo::getFieldClass(CORINFO_FIELD_HANDLE field)
{
    return m_pEEJitInfo->getFieldClass(field);
}

CorInfoType ZapInfo::getFieldType(CORINFO_FIELD_HANDLE field,
                                  CORINFO_CLASS_HANDLE *structType,
                                  CORINFO_CLASS_HANDLE memberParent)

{
    return m_pEEJitInfo->getFieldType(field, structType, memberParent);
}

unsigned ZapInfo::getFieldOffset(CORINFO_FIELD_HANDLE field)
{
    return m_pEEJitInfo->getFieldOffset(field);
}

void ZapInfo::getFieldInfo (CORINFO_RESOLVED_TOKEN * pResolvedToken,
                            CORINFO_METHOD_HANDLE  callerHandle,
                            CORINFO_ACCESS_FLAGS   flags,
                            CORINFO_FIELD_INFO    *pResult)
{
    m_pEEJitInfo->getFieldInfo(pResolvedToken, callerHandle, flags, pResult);

#ifdef FEATURE_READYTORUN_COMPILER
    CORINFO_EE_INFO eeInfo;
    m_pEEJitInfo->getEEInfo(&eeInfo);

    if (IsReadyToRunCompilation())
    {
        if (pResult->accessAllowed != CORINFO_ACCESS_ALLOWED)
        {
            if (m_zapper->m_pOpt->m_verbose)
                m_zapper->Warning(W("ReadyToRun: Runtime field access checks not supported\n"));
            ThrowHR(E_NOTIMPL);
        }

        DWORD fAtypicalCallsite = (flags & CORINFO_ACCESS_ATYPICAL_CALLSITE) ? CORINFO_HELP_READYTORUN_ATYPICAL_CALLSITE : 0;

        switch (pResult->fieldAccessor)
        {
        case CORINFO_FIELD_INSTANCE:
            {
                DWORD dwBaseOffset = (DWORD)-1;
                CORCOMPILE_FIXUP_BLOB_KIND fixupKind = m_pImage->GetCompileInfo()->GetFieldBaseOffset(pResolvedToken->hClass, &dwBaseOffset);

                switch (fixupKind)
                {
                case ENCODE_FIELD_OFFSET:
                    {
                        ZapImport * pImport = m_pImage->GetImportTable()->GetFieldImport(ENCODE_FIELD_OFFSET, pResolvedToken->hField, pResolvedToken);

                        if (pResult->offset > eeInfo.maxUncheckedOffsetForNullObject / 2)
                        {
                            if (m_zapper->m_pOpt->m_verbose)
                                m_zapper->Warning(W("ReadyToRun: Cross-module instance fields with large offsets not supported\n"));
                            ThrowHR(E_NOTIMPL);
                        }
                        pResult->offset = 0;

                        pResult->fieldAccessor = CORINFO_FIELD_INSTANCE_WITH_BASE;

                        pResult->fieldLookup.accessType = IAT_PVALUE;
                        pResult->fieldLookup.addr = pImport;

                        AppendImport(pImport);
                    }
                    break;

                case ENCODE_CHECK_FIELD_OFFSET:
                    {
                        ZapImport * pImport = m_pImage->GetImportTable()->GetCheckFieldOffsetImport(pResolvedToken->hField, pResolvedToken, pResult->offset);
                        AppendImport(pImport);
                    }
                    break;

                case ENCODE_FIELD_BASE_OFFSET:
                    {
                        ZapImport * pImport = m_pImage->GetImportTable()->GetClassImport(ENCODE_FIELD_BASE_OFFSET, pResolvedToken);

                        if (pResult->offset > eeInfo.maxUncheckedOffsetForNullObject / 2)
                        {
                            if (m_zapper->m_pOpt->m_verbose)
                                m_zapper->Warning(W("ReadyToRun: Large objects crossing module boundaries not supported\n"));
                            ThrowHR(E_NOTIMPL);
                        }
                        _ASSERTE(pResult->offset >= dwBaseOffset);
                        pResult->offset -= dwBaseOffset;

                        pResult->fieldAccessor = CORINFO_FIELD_INSTANCE_WITH_BASE;

                        pResult->fieldLookup.accessType = IAT_PVALUE;
                        pResult->fieldLookup.addr = pImport;

                        AppendImport(pImport);
                    }
                    break;

                case ENCODE_NONE:
                    break;

                default:
                    UNREACHABLE_MSG("Unexpected field base fixup");
                }
            }
            break;

        case CORINFO_FIELD_INSTANCE_HELPER:
        case CORINFO_FIELD_INSTANCE_ADDR_HELPER:
            if (m_zapper->m_pOpt->m_verbose)
                m_zapper->Warning(W("ReadyToRun: Special instance fields not supported\n"));
            ThrowHR(E_NOTIMPL);
            break;

        case CORINFO_FIELD_STATIC_SHARED_STATIC_HELPER:
            {
                if (m_pImage->GetCompileInfo()->IsInCurrentVersionBubble(m_pEEJitInfo->getClassModule(pResolvedToken->hClass)))
                {
                    CORCOMPILE_FIXUP_BLOB_KIND kind;

                    switch (pResult->helper)
                    {
                    case CORINFO_HELP_GETSHARED_GCSTATIC_BASE:
                    case CORINFO_HELP_GETSHARED_GCSTATIC_BASE_NOCTOR:
                    case CORINFO_HELP_GETSHARED_GCSTATIC_BASE_DYNAMICCLASS:
                        kind = ENCODE_STATIC_BASE_GC_HELPER;
                        break;
                    case CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE:
                    case CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE_NOCTOR:
                    case CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE_DYNAMICCLASS:
                        kind = ENCODE_STATIC_BASE_NONGC_HELPER;
                        break;
                    case CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE:
                    case CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_NOCTOR:
                    case CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_DYNAMICCLASS:
                        kind = ENCODE_THREAD_STATIC_BASE_GC_HELPER;
                        break;
                    case CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE:
                    case CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_NOCTOR:
                    case CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_DYNAMICCLASS:
                        kind = ENCODE_THREAD_STATIC_BASE_NONGC_HELPER;
                        break;
                    default:
                        UNREACHABLE_MSG("Unexpected static helper");
                    }

                    ZapImport * pImport = m_pImage->GetImportTable()->GetDynamicHelperCell(
                        (CORCOMPILE_FIXUP_BLOB_KIND)(kind | fAtypicalCallsite), pResolvedToken->hClass);

                    pResult->fieldLookup.accessType = IAT_PVALUE;
                    pResult->fieldLookup.addr = pImport;

                    pResult->helper = CORINFO_HELP_READYTORUN_STATIC_BASE;
                }
                else
                {
                    ZapImport * pImport = m_pImage->GetImportTable()->GetDynamicHelperCell(
                        (CORCOMPILE_FIXUP_BLOB_KIND)(ENCODE_FIELD_ADDRESS | fAtypicalCallsite), pResolvedToken->hField, pResolvedToken);

                    pResult->fieldLookup.accessType = IAT_PVALUE;
                    pResult->fieldLookup.addr = pImport;

                    pResult->helper = CORINFO_HELP_READYTORUN_STATIC_BASE;

                    pResult->offset = 0;
                    pResult->fieldFlags &= ~CORINFO_FLG_FIELD_STATIC_IN_HEAP; // The dynamic helper takes care of the unboxing
                }
            }
            break;

        case CORINFO_FIELD_STATIC_GENERICS_STATIC_HELPER:
		{
			// Nothing to do... The generic handle lookup gets embedded in to the codegen
			// during the jitting of the field lookup.
			// (Note: The generic lookup in R2R is performed by a call to a helper at runtime, not by
			// codegen emitted at crossgen time)
			// TODO: replace the call to the generic lookup helper and the call to the static helper function
			// with a single call to a R2R cell that performs:
			//      1) Generic handle lookup
			//      2) Computes the statics base address
			//      3) Generates a stub for subsequent lookups that includes dictionary access
			// (For perf reasons)
		}
            break;

        case CORINFO_FIELD_STATIC_RVA_ADDRESS:       // RVA field at given address
            if (m_pEEJitInfo->getClassModule(pResolvedToken->hClass) != m_pImage->m_hModule)
            {
                if (m_zapper->m_pOpt->m_verbose)
                    m_zapper->Warning(W("ReadyToRun: Cross-module RVA static fields not supported\n"));
                ThrowHR(E_NOTIMPL);
            }
            break;

        case CORINFO_FIELD_STATIC_ADDRESS:           // field at given address
        case CORINFO_FIELD_STATIC_ADDR_HELPER:       // static field accessed using address-of helper (argument is FieldDesc *)
        case CORINFO_FIELD_STATIC_TLS:
            if (m_zapper->m_pOpt->m_verbose)
                m_zapper->Warning(W("ReadyToRun: Rare kinds of static fields not supported\n"));
            ThrowHR(E_NOTIMPL);
            break;

        case CORINFO_FIELD_INTRINSIC_ZERO:
        case CORINFO_FIELD_INTRINSIC_EMPTY_STRING:
        case CORINFO_FIELD_INTRINSIC_ISLITTLEENDIAN:
            break;

        default:
            UNREACHABLE_MSG("Unexpected field acccess type");
        }
    }
#endif // FEATURE_READYTORUN_COMPILER
}

bool ZapInfo::isFieldStatic(CORINFO_FIELD_HANDLE fldHnd)
{
    return m_pEEJitInfo->isFieldStatic(fldHnd);
}

//
// ICorClassInfo
//

CorInfoType ZapInfo::asCorInfoType(CORINFO_CLASS_HANDLE cls)
{
    return m_pEEJitInfo->asCorInfoType(cls);
}

const char* ZapInfo::getClassName(CORINFO_CLASS_HANDLE cls)
{
    return m_pEEJitInfo->getClassName(cls);
}

const char* ZapInfo::getClassNameFromMetadata(CORINFO_CLASS_HANDLE cls, const char** namespaceName)
{
    return m_pEEJitInfo->getClassNameFromMetadata(cls, namespaceName);
}

CORINFO_CLASS_HANDLE ZapInfo::getTypeInstantiationArgument(CORINFO_CLASS_HANDLE cls, unsigned index)
{
    return m_pEEJitInfo->getTypeInstantiationArgument(cls, index);
}

const char* ZapInfo::getHelperName(CorInfoHelpFunc func)
{
    return m_pEEJitInfo->getHelperName(func);
}

int ZapInfo::appendClassName(__deref_inout_ecount(*pnBufLen) WCHAR** ppBuf, int* pnBufLen,
                             CORINFO_CLASS_HANDLE    cls,
                             BOOL fNamespace,
                             BOOL fFullInst,
                             BOOL fAssembly)
{
    return m_pEEJitInfo->appendClassName(ppBuf,pnBufLen,cls,fNamespace,fFullInst,fAssembly);
}

BOOL ZapInfo::isValueClass(CORINFO_CLASS_HANDLE cls)
{
    return m_pEEJitInfo->isValueClass(cls);
}

CorInfoInlineTypeCheck ZapInfo::canInlineTypeCheck (CORINFO_CLASS_HANDLE cls, CorInfoInlineTypeCheckSource source)
{
    return m_pEEJitInfo->canInlineTypeCheck(cls, source);
}

DWORD ZapInfo::getClassAttribs(CORINFO_CLASS_HANDLE cls)
{
    return m_pEEJitInfo->getClassAttribs(cls);
}

BOOL ZapInfo::isStructRequiringStackAllocRetBuf(CORINFO_CLASS_HANDLE cls)
{
    return m_pEEJitInfo->isStructRequiringStackAllocRetBuf(cls);
}

CorInfoInitClassResult ZapInfo::initClass(
            CORINFO_FIELD_HANDLE    field,
            CORINFO_METHOD_HANDLE   method,
            CORINFO_CONTEXT_HANDLE  context)
{
    return m_pEEJitInfo->initClass(field, method, context);
}

void ZapInfo::classMustBeLoadedBeforeCodeIsRun(CORINFO_CLASS_HANDLE cls)
{
    // This adds an entry to the table of fixups.  The table gets iterated later
    // to add entries to the delayed fixup list for the code being generated.
    m_ClassLoadTable.Load(cls, FALSE);
}

CORINFO_METHOD_HANDLE ZapInfo::mapMethodDeclToMethodImpl(CORINFO_METHOD_HANDLE methHnd)
{
    return (CORINFO_METHOD_HANDLE)m_pEEJitInfo->mapMethodDeclToMethodImpl(methHnd);
}

void ZapInfo::methodMustBeLoadedBeforeCodeIsRun(CORINFO_METHOD_HANDLE meth)
{
    // This adds an entry to the table of fixups.  The table gets iterated later
    // to add entries to the delayed fixup list for the code being generated.
    m_MethodLoadTable.Load(meth, FALSE);
}

CORINFO_CLASS_HANDLE ZapInfo::getBuiltinClass(CorInfoClassId classId)
{
    return m_pEEJitInfo->getBuiltinClass(classId);
}

CorInfoType ZapInfo::getTypeForPrimitiveValueClass(CORINFO_CLASS_HANDLE cls)
{
    return m_pEEJitInfo->getTypeForPrimitiveValueClass(cls);
}

CorInfoType ZapInfo::getTypeForPrimitiveNumericClass(CORINFO_CLASS_HANDLE cls)
{
    return m_pEEJitInfo->getTypeForPrimitiveNumericClass(cls);
}

BOOL ZapInfo::canCast(CORINFO_CLASS_HANDLE child,
                                CORINFO_CLASS_HANDLE parent)
{
    return m_pEEJitInfo->canCast(child, parent);
}

BOOL ZapInfo::areTypesEquivalent(CORINFO_CLASS_HANDLE cls1, CORINFO_CLASS_HANDLE cls2)
{
    return m_pEEJitInfo->areTypesEquivalent(cls1, cls2);
}

TypeCompareState ZapInfo::compareTypesForCast(CORINFO_CLASS_HANDLE fromClass, CORINFO_CLASS_HANDLE toClass)
{
    return m_pEEJitInfo->compareTypesForCast(fromClass, toClass);
}

TypeCompareState ZapInfo::compareTypesForEquality(CORINFO_CLASS_HANDLE cls1, CORINFO_CLASS_HANDLE cls2)
{
    return m_pEEJitInfo->compareTypesForEquality(cls1, cls2);
}

CORINFO_CLASS_HANDLE ZapInfo::mergeClasses(
                                CORINFO_CLASS_HANDLE cls1,
                                CORINFO_CLASS_HANDLE cls2)
{
    return m_pEEJitInfo->mergeClasses(cls1, cls2);
}

BOOL ZapInfo::isMoreSpecificType(
                CORINFO_CLASS_HANDLE cls1,
                CORINFO_CLASS_HANDLE cls2)
{
    return m_pEEJitInfo->isMoreSpecificType(cls1, cls2);
}

CORINFO_CLASS_HANDLE ZapInfo::getParentType (
                                CORINFO_CLASS_HANDLE       cls)
{
    return m_pEEJitInfo->getParentType(cls);
}

CorInfoType ZapInfo::getChildType (
            CORINFO_CLASS_HANDLE       clsHnd,
            CORINFO_CLASS_HANDLE       *clsRet)
{
    return m_pEEJitInfo->getChildType(clsHnd, clsRet);
}

BOOL ZapInfo::satisfiesClassConstraints(
            CORINFO_CLASS_HANDLE cls)
{
    return m_pEEJitInfo->satisfiesClassConstraints(cls);
}

BOOL ZapInfo::isSDArray(CORINFO_CLASS_HANDLE cls)
{
    return m_pEEJitInfo->isSDArray(cls);
}

unsigned ZapInfo::getArrayRank(CORINFO_CLASS_HANDLE cls)
{
    return m_pEEJitInfo->getArrayRank(cls);
}

void * ZapInfo::getArrayInitializationData(CORINFO_FIELD_HANDLE field, DWORD size)
{
    if (m_pEEJitInfo->getClassModule(m_pEEJitInfo->getFieldClass(field)) != m_pImage->m_hModule)
        return NULL;

    void * arrayData = m_pEEJitInfo->getArrayInitializationData(field, size);
    if (!arrayData)
        return NULL;

#ifdef FEATURE_READYTORUN_COMPILER
    if (IsReadyToRunCompilation())
        return m_pImage->m_pILMetaData->GetRVAField(arrayData);
#endif

    return (void *) m_pImage->GetWrappers()->GetGenericHandle(CORINFO_GENERIC_HANDLE(arrayData));
}

CorInfoIsAccessAllowedResult ZapInfo::canAccessClass( CORINFO_RESOLVED_TOKEN * pResolvedToken,
                                                      CORINFO_METHOD_HANDLE   callerHandle,
                                                      CORINFO_HELPER_DESC    *throwHelper)
{
    CorInfoIsAccessAllowedResult ret = m_pEEJitInfo->canAccessClass(pResolvedToken, callerHandle, throwHelper);

#ifdef FEATURE_READYTORUN_COMPILER
    if (ret != CORINFO_ACCESS_ALLOWED)
    {
        if (m_zapper->m_pOpt->m_verbose)
            m_zapper->Warning(W("ReadyToRun: Runtime access checks not supported\n"));
        ThrowHR(E_NOTIMPL);
    }
#endif

    return ret;
}


CORINFO_MODULE_HANDLE ZapInfo::getClassModule(CORINFO_CLASS_HANDLE cls)
{
    return m_pEEJitInfo->getClassModule(cls);
}

CORINFO_ASSEMBLY_HANDLE ZapInfo::getModuleAssembly(CORINFO_MODULE_HANDLE mod)
{
    return m_pEEJitInfo->getModuleAssembly(mod);
}

const char* ZapInfo::getAssemblyName(CORINFO_ASSEMBLY_HANDLE assem)
{
    return m_pEEJitInfo->getAssemblyName(assem);
}

void* ZapInfo::LongLifetimeMalloc(size_t sz)
{
    return m_pEEJitInfo->LongLifetimeMalloc(sz);
}

void ZapInfo::LongLifetimeFree(void* obj)
{
    return m_pEEJitInfo->LongLifetimeFree(obj);
}

size_t ZapInfo::getClassModuleIdForStatics(CORINFO_CLASS_HANDLE cls, CORINFO_MODULE_HANDLE *pModule, void **ppIndirection)
{
    if (IsReadyToRunCompilation())
    {
        _ASSERTE(!"getClassModuleIdForStatics");
        ThrowHR(E_NOTIMPL);
    }

    _ASSERTE(ppIndirection != NULL);
    _ASSERTE(pModule == NULL);
    CORINFO_MODULE_HANDLE module;
    size_t moduleId = m_pEEJitInfo->getClassModuleIdForStatics(cls, &module, ppIndirection);
    CORINFO_MODULE_HANDLE pzmModule = m_pImage->m_pPreloader->GetPreferredZapModuleForClassHandle(cls);

    if (module == pzmModule)
    {
        // Use the module for the moduleid lookup if we have to do so. This causes us to have fewer fixups than
        // if the fixups were exclusively based on the moduleforstatics lookup
        cls = NULL;


        if (module == m_pImage->m_hModule)
        {
            // If the handle is the module we are currently ngening, we use
            // an indirection to the slot where the module pointer gets
            // stored when the module gets reloaded.

            *ppIndirection = PVOID(m_pImage->GetWrappers()->GetModuleIDHandle(module));
            return NULL;
        }

        // Fall through to regular import
    }
    else
    {
        // Use the class for the moduleid lookup. This causes us to generate a fixup for the ModuleForStatics explicitly.
        module = NULL;
    }

    ZapImport * pImport = m_pImage->GetImportTable()->GetModuleDomainIdImport(module, cls);
    AppendConditionalImport(pImport);

    *ppIndirection = pImport;
    return NULL;
}

unsigned ZapInfo::getClassSize(CORINFO_CLASS_HANDLE cls)
{
    DWORD size = m_pEEJitInfo->getClassSize(cls);

#ifdef FEATURE_READYTORUN_COMPILER
    if (IsReadyToRunCompilation())
    {
        if (m_pEECompileInfo->NeedsTypeLayoutCheck(cls))
        {
            ZapImport * pImport = m_pImage->GetImportTable()->GetCheckTypeLayoutImport(cls);
            AppendImport(pImport);

            m_ClassLoadTable.Load(cls, TRUE);
        }
    }
#endif

    return size;
}

unsigned ZapInfo::getHeapClassSize(CORINFO_CLASS_HANDLE cls)
{
    return m_pEEJitInfo->getHeapClassSize(cls);
}

BOOL ZapInfo::canAllocateOnStack(CORINFO_CLASS_HANDLE cls)
{
    return m_pEEJitInfo->canAllocateOnStack(cls);
}

unsigned ZapInfo::getClassAlignmentRequirement(CORINFO_CLASS_HANDLE cls, BOOL fDoubleAlignHint)
{
    return m_pEEJitInfo->getClassAlignmentRequirement(cls, fDoubleAlignHint);
}

CORINFO_FIELD_HANDLE ZapInfo::getFieldInClass(CORINFO_CLASS_HANDLE clsHnd, INT num)
{
    return m_pEEJitInfo->getFieldInClass(clsHnd,num);
}

mdMethodDef ZapInfo::getMethodDefFromMethod(CORINFO_METHOD_HANDLE hMethod)
{
    return m_pEEJitInfo->getMethodDefFromMethod(hMethod);
}

BOOL ZapInfo::checkMethodModifier(CORINFO_METHOD_HANDLE hMethod, LPCSTR modifier, BOOL fOptional)
{
    return m_pEEJitInfo->checkMethodModifier(hMethod, modifier, fOptional);
}

unsigned ZapInfo::getClassGClayout(CORINFO_CLASS_HANDLE cls, BYTE *gcPtrs)
{
    return m_pEEJitInfo->getClassGClayout(cls, gcPtrs);
}

// returns the enregister info for a struct based on type of fields, alignment, etc..
bool ZapInfo::getSystemVAmd64PassStructInRegisterDescriptor(
    /*IN*/  CORINFO_CLASS_HANDLE _structHnd,
    /*OUT*/ SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR* structPassInRegDescPtr)
{
    return m_pEEJitInfo->getSystemVAmd64PassStructInRegisterDescriptor(_structHnd, structPassInRegDescPtr);
}

unsigned ZapInfo::getClassNumInstanceFields(CORINFO_CLASS_HANDLE cls)
{
    return m_pEEJitInfo->getClassNumInstanceFields(cls);
}

CorInfoHelpFunc ZapInfo::getNewHelper(CORINFO_RESOLVED_TOKEN * pResolvedToken, CORINFO_METHOD_HANDLE callerHandle, bool * pHasSideEffects)
{
    if (!IsReadyToRunCompilation())
    {
        classMustBeLoadedBeforeCodeIsRun(pResolvedToken->hClass);
    }

    CorInfoHelpFunc helper = m_pEEJitInfo->getNewHelper(pResolvedToken, callerHandle, pHasSideEffects);

    return IsReadyToRunCompilation() ? CORINFO_HELP_NEWFAST : helper;
}

CorInfoHelpFunc ZapInfo::getSharedCCtorHelper(CORINFO_CLASS_HANDLE clsHnd)
{
    return m_pEEJitInfo->getSharedCCtorHelper(clsHnd);
}

CORINFO_CLASS_HANDLE  ZapInfo::getTypeForBox(CORINFO_CLASS_HANDLE  cls)
{
    return m_pEEJitInfo->getTypeForBox(cls);
}

CorInfoHelpFunc ZapInfo::getBoxHelper(CORINFO_CLASS_HANDLE cls)
{
    return m_pEEJitInfo->getBoxHelper(cls);
}

CorInfoHelpFunc ZapInfo::getUnBoxHelper(CORINFO_CLASS_HANDLE cls)
{
    return m_pEEJitInfo->getUnBoxHelper(cls);
}

CorInfoHelpFunc ZapInfo::getCastingHelper(CORINFO_RESOLVED_TOKEN * pResolvedToken, bool fThrowing)
{
    if (IsReadyToRunCompilation())
        return (fThrowing ? CORINFO_HELP_CHKCASTANY : CORINFO_HELP_ISINSTANCEOFANY);

    return m_pEEJitInfo->getCastingHelper(pResolvedToken, fThrowing);
}

CorInfoHelpFunc ZapInfo::getNewArrHelper(CORINFO_CLASS_HANDLE arrayCls)
{
    if (IsReadyToRunCompilation())
        return CORINFO_HELP_NEWARR_1_DIRECT;

    return m_pEEJitInfo->getNewArrHelper(arrayCls);
}

bool ZapInfo::getReadyToRunHelper(CORINFO_RESOLVED_TOKEN * pResolvedToken,
    CORINFO_LOOKUP_KIND * pGenericLookupKind,
    CorInfoHelpFunc id,
    CORINFO_CONST_LOOKUP * pLookup)
{
#ifdef FEATURE_READYTORUN_COMPILER
    _ASSERTE(IsReadyToRunCompilation());

    ZapImport * pImport = NULL;

    DWORD fAtypicalCallsite = (id & CORINFO_HELP_READYTORUN_ATYPICAL_CALLSITE);
    id = (CorInfoHelpFunc)(id & ~CORINFO_HELP_READYTORUN_ATYPICAL_CALLSITE);

    switch (id)
    {
    case CORINFO_HELP_READYTORUN_NEW:
        // Call CEEInfo::getNewHelper to validate the request (e.g., check for abstract class).
        m_pEEJitInfo->getNewHelper(pResolvedToken, m_currentMethodHandle);

        if ((getClassAttribs(pResolvedToken->hClass) & CORINFO_FLG_SHAREDINST) != 0)
            return false;   // Requires runtime lookup.
        pImport = m_pImage->GetImportTable()->GetDynamicHelperCell(
            (CORCOMPILE_FIXUP_BLOB_KIND)(ENCODE_NEW_HELPER | fAtypicalCallsite), pResolvedToken->hClass);
        break;

    case CORINFO_HELP_READYTORUN_NEWARR_1:
        if ((getClassAttribs(pResolvedToken->hClass) & CORINFO_FLG_SHAREDINST) != 0)
            return false;   // Requires runtime lookup.
        pImport = m_pImage->GetImportTable()->GetDynamicHelperCell(
            (CORCOMPILE_FIXUP_BLOB_KIND)(ENCODE_NEW_ARRAY_HELPER | fAtypicalCallsite), pResolvedToken->hClass);
        break;

    case CORINFO_HELP_READYTORUN_ISINSTANCEOF:
        if ((getClassAttribs(pResolvedToken->hClass) & CORINFO_FLG_SHAREDINST) != 0)
            return false;   // Requires runtime lookup.
        pImport = m_pImage->GetImportTable()->GetDynamicHelperCell(
            (CORCOMPILE_FIXUP_BLOB_KIND)(ENCODE_ISINSTANCEOF_HELPER | fAtypicalCallsite), pResolvedToken->hClass);
        break;

    case CORINFO_HELP_READYTORUN_CHKCAST:
        if ((getClassAttribs(pResolvedToken->hClass) & CORINFO_FLG_SHAREDINST) != 0)
            return false;   // Requires runtime lookup.
        pImport = m_pImage->GetImportTable()->GetDynamicHelperCell(
            (CORCOMPILE_FIXUP_BLOB_KIND)(ENCODE_CHKCAST_HELPER | fAtypicalCallsite), pResolvedToken->hClass);
        break;

    case CORINFO_HELP_READYTORUN_STATIC_BASE:
        if ((getClassAttribs(pResolvedToken->hClass) & CORINFO_FLG_SHAREDINST) != 0)
            return false;   // Requires runtime lookup.
        if (m_pImage->GetCompileInfo()->IsInCurrentVersionBubble(m_pEEJitInfo->getClassModule(pResolvedToken->hClass)))
        {
            pImport = m_pImage->GetImportTable()->GetDynamicHelperCell(
                (CORCOMPILE_FIXUP_BLOB_KIND)(ENCODE_CCTOR_TRIGGER | fAtypicalCallsite), pResolvedToken->hClass);
        }
        else
        {
            // READYTORUN: FUTURE: Cross-module static cctor triggers
            if (m_zapper->m_pOpt->m_verbose)
                m_zapper->Warning(W("ReadyToRun: Cross-module static cctor triggers not supported\n"));
            ThrowHR(E_NOTIMPL);
        }
        break;

    case CORINFO_HELP_READYTORUN_GENERIC_HANDLE:
        _ASSERTE(pGenericLookupKind != NULL && pGenericLookupKind->needsRuntimeLookup);
        if (pGenericLookupKind->runtimeLookupKind == CORINFO_LOOKUP_METHODPARAM)
        {
            pImport = m_pImage->GetImportTable()->GetDictionaryLookupCell(
                (CORCOMPILE_FIXUP_BLOB_KIND)(ENCODE_DICTIONARY_LOOKUP_METHOD | fAtypicalCallsite), m_currentMethodHandle, pResolvedToken, pGenericLookupKind);
        }
        else if (pGenericLookupKind->runtimeLookupKind == CORINFO_LOOKUP_THISOBJ)
        {
            pImport = m_pImage->GetImportTable()->GetDictionaryLookupCell(
                (CORCOMPILE_FIXUP_BLOB_KIND)(ENCODE_DICTIONARY_LOOKUP_THISOBJ | fAtypicalCallsite), m_currentMethodHandle, pResolvedToken, pGenericLookupKind);
        }
        else
        {
            _ASSERTE(pGenericLookupKind->runtimeLookupKind == CORINFO_LOOKUP_CLASSPARAM);
            pImport = m_pImage->GetImportTable()->GetDictionaryLookupCell(
                (CORCOMPILE_FIXUP_BLOB_KIND)(ENCODE_DICTIONARY_LOOKUP_TYPE | fAtypicalCallsite), m_currentMethodHandle, pResolvedToken, pGenericLookupKind);
        }
        break;

    default:
        _ASSERTE(false);
        ThrowHR(E_NOTIMPL);
    }

    pLookup->accessType = IAT_PVALUE;
    pLookup->addr = pImport;
    return true;
#else
    return false;
#endif
}

void ZapInfo::getReadyToRunDelegateCtorHelper(
        CORINFO_RESOLVED_TOKEN * pTargetMethod,
        CORINFO_CLASS_HANDLE     delegateType,
        CORINFO_LOOKUP *   pLookup
        )
{
#ifdef FEATURE_READYTORUN_COMPILER
    _ASSERTE(IsReadyToRunCompilation());
    pLookup->lookupKind.needsRuntimeLookup = false;
    pLookup->constLookup.accessType = IAT_PVALUE;
    pLookup->constLookup.addr = m_pImage->GetImportTable()->GetDynamicHelperCell(
            (CORCOMPILE_FIXUP_BLOB_KIND)(ENCODE_DELEGATE_CTOR), pTargetMethod->hMethod, pTargetMethod, delegateType);
#endif
}


//
// ICorModuleInfo
//

//-----------------------------------------------------------------------------
void ZapInfo::resolveToken(CORINFO_RESOLVED_TOKEN * pResolvedToken)
{
    m_pEEJitInfo->resolveToken(pResolvedToken);
}

//-----------------------------------------------------------------------------
bool ZapInfo::tryResolveToken(CORINFO_RESOLVED_TOKEN * pResolvedToken)
{
    return m_pEEJitInfo->tryResolveToken(pResolvedToken);
}

//-----------------------------------------------------------------------------
void ZapInfo::findSig(CORINFO_MODULE_HANDLE tokenScope,
                      unsigned sigTOK,
                      CORINFO_CONTEXT_HANDLE tokenContext,
                      CORINFO_SIG_INFO *sig)
{
    m_pEEJitInfo->findSig(tokenScope, sigTOK, tokenContext, sig);
}

void ZapInfo::findCallSiteSig(CORINFO_MODULE_HANDLE tokenScope,
                                           unsigned methTOK,
                                           CORINFO_CONTEXT_HANDLE tokenContext, CORINFO_SIG_INFO *sig)
{
    m_pEEJitInfo->findCallSiteSig(tokenScope, methTOK, tokenContext, sig);
}

size_t ZapInfo::findNameOfToken(CORINFO_MODULE_HANDLE tokenScope,
                                       unsigned token,
                                       __out_ecount (FQNameCapacity) char * szFQName,
                                       size_t FQNameCapacity)
{
    return m_pEEJitInfo->findNameOfToken(tokenScope, token, szFQName, FQNameCapacity);
}

BOOL ZapInfo::isValidToken (
            CORINFO_MODULE_HANDLE       tokenScope,
            unsigned                    token)
{
    return m_pEEJitInfo->isValidToken(tokenScope, token);
}

BOOL ZapInfo::isValidStringRef (
            CORINFO_MODULE_HANDLE       tokenScope,
            unsigned                    token)
{
    return m_pEEJitInfo->isValidStringRef(tokenScope, token);
}

LPCWSTR ZapInfo::getStringLiteral (
            CORINFO_MODULE_HANDLE       tokenScope,
            unsigned                    token,
            int*                        length)
{
    return m_pEEJitInfo->getStringLiteral(tokenScope, token, length);
}

//
// ICorMethodInfo
//

const char* ZapInfo::getMethodName(CORINFO_METHOD_HANDLE ftn, const char **moduleName)
{
    return m_pEEJitInfo->getMethodName(ftn, moduleName);
}

const char* ZapInfo::getMethodNameFromMetadata(CORINFO_METHOD_HANDLE ftn, const char **className, const char** namespaceName, const char **enclosingClassName)
{
    return m_pEEJitInfo->getMethodNameFromMetadata(ftn, className, namespaceName, enclosingClassName);
}

unsigned ZapInfo::getMethodHash(CORINFO_METHOD_HANDLE ftn)
{
    return m_pEEJitInfo->getMethodHash(ftn);
}

DWORD ZapInfo::getMethodAttribs(CORINFO_METHOD_HANDLE ftn)
{
    DWORD result = m_pEEJitInfo->getMethodAttribs(ftn);
    return FilterNamedIntrinsicMethodAttribs(this, result, ftn, m_pEEJitInfo);
}

void ZapInfo::setMethodAttribs(CORINFO_METHOD_HANDLE ftn, CorInfoMethodRuntimeFlags attribs)
{
    m_pEEJitInfo->setMethodAttribs(ftn, attribs);
}

void ZapInfo::getMethodSig(CORINFO_METHOD_HANDLE ftn, CORINFO_SIG_INFO *sig,CORINFO_CLASS_HANDLE memberParent)
{
    m_pEEJitInfo->getMethodSig(ftn, sig, memberParent);
}

bool ZapInfo::getMethodInfo(CORINFO_METHOD_HANDLE ftn,CORINFO_METHOD_INFO* info)
{
    bool result = m_pImage->m_pPreloader->GetMethodInfo(m_currentMethodToken, ftn, info);
    info->regionKind = m_pImage->GetCurrentRegionKind();
    return result;
}

CorInfoInline ZapInfo::canInline(CORINFO_METHOD_HANDLE caller,
                                           CORINFO_METHOD_HANDLE callee,
                                           DWORD* pRestrictions)
{
    return m_pEEJitInfo->canInline(caller, callee, pRestrictions);

}

void ZapInfo::reportInliningDecision (CORINFO_METHOD_HANDLE inlinerHnd,
                                                CORINFO_METHOD_HANDLE inlineeHnd,
                                                CorInfoInline inlineResult,
                                                const char * reason)
{
    if (!dontInline(inlineResult) && inlineeHnd != NULL)
    {
        // We deliberately report  m_currentMethodHandle (not inlinerHnd) as inliner, because
        // if m_currentMethodHandle != inlinerHnd, it simply means that inlinerHnd is intermediate link
        // in inlining into m_currentMethodHandle, and we have no interest to track those intermediate links now.
        m_pImage->m_pPreloader->ReportInlining(m_currentMethodHandle, inlineeHnd);
    }
    return m_pEEJitInfo->reportInliningDecision(inlinerHnd, inlineeHnd, inlineResult, reason);
}

bool ZapInfo::canTailCall(CORINFO_METHOD_HANDLE caller,
                                         CORINFO_METHOD_HANDLE declaredCallee,
                                         CORINFO_METHOD_HANDLE exactCallee,
                                         bool fIsTailPrefix)
{
#ifdef FEATURE_READYTORUN_COMPILER
    // READYTORUN: FUTURE: Delay load fixups for tailcalls
    if (IsReadyToRunCompilation())
    {
        if (fIsTailPrefix)
        {
            if (m_zapper->m_pOpt->m_verbose)
                m_zapper->Warning(W("ReadyToRun: Explicit tailcalls not supported\n"));
            ThrowHR(E_NOTIMPL);
        }

        return false;
    }
#endif

    return m_pEEJitInfo->canTailCall(caller, declaredCallee, exactCallee, fIsTailPrefix);
}

void ZapInfo::reportTailCallDecision(CORINFO_METHOD_HANDLE callerHnd,
                                               CORINFO_METHOD_HANDLE calleeHnd,
                                               bool fIsTailPrefix,
                                               CorInfoTailCall tailCallResult,
                                               const char * reason)
{
    return m_pEEJitInfo->reportTailCallDecision(callerHnd, calleeHnd, fIsTailPrefix, tailCallResult, reason);
}

void ZapInfo::getEHinfo(CORINFO_METHOD_HANDLE ftn,
                         unsigned EHnumber, CORINFO_EH_CLAUSE* clause)
{
    m_pEEJitInfo->getEHinfo(ftn, EHnumber, clause);
}

CORINFO_CLASS_HANDLE ZapInfo::getMethodClass(CORINFO_METHOD_HANDLE method)
{
    return m_pEEJitInfo->getMethodClass(method);
}

CORINFO_MODULE_HANDLE ZapInfo::getMethodModule(CORINFO_METHOD_HANDLE method)
{
    return m_pEEJitInfo->getMethodModule(method);
}

void ZapInfo::getMethodVTableOffset(CORINFO_METHOD_HANDLE method,
                                    unsigned * pOffsetOfIndirection,
                                    unsigned * pOffsetAfterIndirection,
                                    bool * isRelative)
{
    m_pEEJitInfo->getMethodVTableOffset(method, pOffsetOfIndirection, pOffsetAfterIndirection, isRelative);
}

CORINFO_METHOD_HANDLE ZapInfo::resolveVirtualMethod(
        CORINFO_METHOD_HANDLE virtualMethod,
        CORINFO_CLASS_HANDLE implementingClass,
        CORINFO_CONTEXT_HANDLE ownerType)
{
    return m_pEEJitInfo->resolveVirtualMethod(virtualMethod, implementingClass, ownerType);
}

CORINFO_METHOD_HANDLE ZapInfo::getUnboxedEntry(
    CORINFO_METHOD_HANDLE ftn,
    bool* requiresInstMethodTableArg)
{
    return m_pEEJitInfo->getUnboxedEntry(ftn, requiresInstMethodTableArg);
}

CORINFO_CLASS_HANDLE ZapInfo::getDefaultEqualityComparerClass(
    CORINFO_CLASS_HANDLE elemType)
{
    return m_pEEJitInfo->getDefaultEqualityComparerClass(elemType);
}

void ZapInfo::expandRawHandleIntrinsic(
    CORINFO_RESOLVED_TOKEN *        pResolvedToken,
    CORINFO_GENERICHANDLE_RESULT *  pResult)
{
    m_pEEJitInfo->expandRawHandleIntrinsic(pResolvedToken, pResult);
}

CorInfoIntrinsics ZapInfo::getIntrinsicID(CORINFO_METHOD_HANDLE method,
                                          bool * pMustExpand)
{
    return m_pEEJitInfo->getIntrinsicID(method, pMustExpand);
}

bool ZapInfo::isIntrinsicType(CORINFO_CLASS_HANDLE classHnd)
{
    return m_pEEJitInfo->isIntrinsicType(classHnd);
}

CorInfoUnmanagedCallConv ZapInfo::getUnmanagedCallConv(CORINFO_METHOD_HANDLE method)
{
    return m_pEEJitInfo->getUnmanagedCallConv(method);
}

BOOL ZapInfo::pInvokeMarshalingRequired(CORINFO_METHOD_HANDLE method,
                                                       CORINFO_SIG_INFO* sig)
{
#if defined(TARGET_X86) && defined(TARGET_UNIX)
    // FUTURE ReadyToRun: x86 pinvoke stubs on Unix platforms
    if (IsReadyToRunCompilation())
        return TRUE;
#endif

    if (IsReadyToRunCompilation() && method != NULL && !m_pImage->GetCompileInfo()->IsInCurrentVersionBubble(m_pEEJitInfo->getMethodModule(method)))
    {
        // FUTURE: ZapSig::EncodeMethod does not yet handle cross module references for ReadyToRun
        // See zapsig.cpp around line 1217.
        // Once this is implemented, we'll be able to inline pinvokes of extern methods declared in other modules (Ex: PresentationCore.dll)
        return TRUE;
    }

    return m_pEEJitInfo->pInvokeMarshalingRequired(method, sig);
}

LPVOID ZapInfo::GetCookieForPInvokeCalliSig(CORINFO_SIG_INFO* szMetaSig,
                                                 void ** ppIndirection)
{
    return getVarArgsHandle(szMetaSig, ppIndirection);
}

bool ZapInfo::canGetCookieForPInvokeCalliSig(CORINFO_SIG_INFO* szMetaSig)
{
    return canGetVarArgsHandle(szMetaSig);
}

BOOL ZapInfo::satisfiesMethodConstraints(
            CORINFO_CLASS_HANDLE        parent,
            CORINFO_METHOD_HANDLE       method)
{
    return m_pEEJitInfo->satisfiesMethodConstraints(parent, method);
}


BOOL ZapInfo::isCompatibleDelegate(
            CORINFO_CLASS_HANDLE objCls,
            CORINFO_CLASS_HANDLE methodParentCls,
            CORINFO_METHOD_HANDLE method,
            CORINFO_CLASS_HANDLE delegateCls,
            BOOL* pfIsOpenDelegate)
{
    return m_pEEJitInfo->isCompatibleDelegate(objCls, methodParentCls, method, delegateCls, pfIsOpenDelegate);
}

//
// ICorErrorInfo
//

HRESULT ZapInfo::GetErrorHRESULT(struct _EXCEPTION_POINTERS *pExceptionPointers)
{
    return m_pEEJitInfo->GetErrorHRESULT(pExceptionPointers);
}

ULONG ZapInfo::GetErrorMessage(__in_ecount(bufferLength) LPWSTR buffer, ULONG bufferLength)
{
    return m_pEEJitInfo->GetErrorMessage(buffer, bufferLength);
}

int ZapInfo::FilterException(struct _EXCEPTION_POINTERS *pExceptionPointers)
{
    // Continue unwinding if fatal error was hit.
    if (FAILED(g_hrFatalError))
        return EXCEPTION_CONTINUE_SEARCH;

    return m_pEEJitInfo->FilterException(pExceptionPointers);
}

void ZapInfo::HandleException(struct _EXCEPTION_POINTERS *pExceptionPointers)
{
    m_pEEJitInfo->HandleException(pExceptionPointers);
}

void ZapInfo::ThrowExceptionForJitResult(HRESULT result)
{
    m_pEEJitInfo->ThrowExceptionForJitResult(result);
}
void ZapInfo::ThrowExceptionForHelper(const CORINFO_HELPER_DESC * throwHelper)
{
    m_pEEJitInfo->ThrowExceptionForHelper(throwHelper);
}

template<> void LoadTable<CORINFO_CLASS_HANDLE>::EmitLoadFixups(CORINFO_METHOD_HANDLE currentMethodHandle, ZapInfo * pZapInfo)
{
    //
    // Find all of our un-fixed entries, and emit a restore fixup for each of them.
    // Note that we don't need a restore fixups for prerestored classes.
    //

    InlineSArray<LoadEntry, 4> unfixed;

    for (LoadEntryHashTable::Iterator i = m_entries.Begin(), end = m_entries.End(); i != end; i++)
    {
        if (i->order == -1
            || m_pModule->m_pPreloader->CanPrerestoreEmbedClassHandle(i->handle)
            // @TODO: Skip transitive closure of currentMethodHandle (parents, instantiations, etc.)
            || m_pModule->GetJitInfo()->getMethodClass(currentMethodHandle) == i->handle)
            continue;

        unfixed.Append(*i);
    }

    //
    // Now clear the table.
    //

    m_entries.RemoveAll();

    if (unfixed.IsEmpty())
        return;

    // Save the fixups in the order they got emited for determinism
    qsort(&unfixed[0], unfixed.GetCount(), sizeof(LoadEntry), LoadEntryCmp);

    for(COUNT_T j = 0; j < unfixed.GetCount(); j++)
    {
        CORINFO_CLASS_HANDLE handle = unfixed[j].handle;
        m_pModule->m_pPreloader->AddTypeToTransitiveClosureOfInstantiations(handle);
        ZapImport * pImport = m_pModule->GetImportTable()->GetClassHandleImport(handle);
        pZapInfo->AppendImport(pImport);
    }
}


template<> void LoadTable<CORINFO_METHOD_HANDLE>::EmitLoadFixups(CORINFO_METHOD_HANDLE currentMethodHandle, ZapInfo * pZapInfo)
{
    //
    // Find all of our un-fixed entries, and emit a restore fixup for each of them.
    // Note that we don't need a restore fixups for prerestored methods.
    //

    InlineSArray<LoadEntry, 4> unfixed;

    for (LoadEntryHashTable::Iterator i = m_entries.Begin(), end = m_entries.End(); i != end; i++)
    {
        if (i->order == -1
            || m_pModule->m_pPreloader->CanPrerestoreEmbedMethodHandle(i->handle)
            || currentMethodHandle == i->handle)
            continue;

        unfixed.Append(*i);
    }

    //
    // Now clear the table.
    //

    m_entries.RemoveAll();

    if (unfixed.IsEmpty())
        return;

    // Save the fixups in the order they got emited for determinism
    qsort(&unfixed[0], unfixed.GetCount(), sizeof(LoadEntry), LoadEntryCmp);

    for(COUNT_T j = 0; j < unfixed.GetCount(); j++)
    {
        CORINFO_METHOD_HANDLE handle = unfixed[j].handle;
        m_pModule->m_pPreloader->AddMethodToTransitiveClosureOfInstantiations(handle);
        ZapImport * pImport = m_pModule->GetImportTable()->GetMethodHandleImport(handle);
        pZapInfo->AppendImport(pImport);
    }
}

BOOL ZapInfo::CurrentMethodHasProfileData()
{
    WRAPPER_NO_CONTRACT;
    UINT32 size;
    ICorJitInfo::BlockCounts * pBlockCounts;
    return SUCCEEDED(getMethodBlockCounts(m_currentMethodHandle, &size, &pBlockCounts, NULL));
}

