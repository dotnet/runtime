// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
#ifdef WIN64EXCEPTIONS
    // Unwind info of the main method body. It will get merged with GC info.
    m_pMainUnwindInfo(NULL),
    m_cbMainUnwindInfo(0),

    m_pUnwindInfo(NULL),
    m_pUnwindInfoFragments(NULL),
#if defined(_TARGET_AMD64_)
    m_pChainedColdUnwindInfo(NULL),
#endif
#endif // WIN64EXCEPTIONS
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
#ifdef WIN64EXCEPTIONS
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

#ifdef WIN64EXCEPTIONS
    delete [] m_pMainUnwindInfo;
    m_pMainUnwindInfo = NULL;

    m_cbMainUnwindInfo = 0;
#endif // WIN64EXCEPTIONS

    // The rest of these pointers are in the ZapWriter's ZapHeap, and will go away when the ZapWriter
    // goes away. That's ok for altjit fallback; we'll use extra memory until the ZapWriter goes away,
    // but we won't write anything to the image. We just zero out the pointers and constants, and we're good.

    m_pCode = NULL;
    m_pColdCode = NULL;
    m_pROData = NULL;

#ifdef WIN64EXCEPTIONS
    m_pUnwindInfoFragments = NULL;
    m_pUnwindInfo = NULL;
#if defined(_TARGET_AMD64_)
    m_pChainedColdUnwindInfo = NULL;
#endif
#endif // WIN64EXCEPTIONS

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

int ZapInfo::ComputeJitFlags(CORINFO_METHOD_HANDLE handle)
{
    int jitFlags = m_zapper->m_pOpt->m_compilerFlags;

    DWORD flags = 0;
    IfFailThrow(m_pEECompileInfo->GetBaseJitFlags(handle, &flags));
    jitFlags |= flags;

    // COMPlus_JitFramed specifies the default fpo setting for jitted and NGened code.
    // You can override the behavior for NGened code using COMPlus_NGenFramed.
    static ConfigDWORD g_NGenFramed;
    DWORD dwNGenFramed = g_NGenFramed.val(CLRConfig::UNSUPPORTED_NGenFramed);
    if (dwNGenFramed == 0) 
    {
        // NGened code should enable fpo
        jitFlags &= ~CORJIT_FLG_FRAMED;
    } 
    else if (dwNGenFramed == 1) 
    {
        // NGened code should disable fpo
        jitFlags |= CORJIT_FLG_FRAMED; 
    }

    if (canSkipMethodVerification(m_currentMethodHandle) == CORINFO_VERIFICATION_CAN_SKIP)
    {
        jitFlags |= CORJIT_FLG_SKIP_VERIFICATION;
    }

    if (m_pImage->m_profileDataSections[MethodBlockCounts].pData && 
        !m_zapper->m_pOpt->m_ignoreProfileData)
    {
        jitFlags |= CORJIT_FLG_BBOPT;
    }

    // 
    // By default we always enable Hot/Cold procedure splitting
    //
    jitFlags |= CORJIT_FLG_PROCSPLIT;

    if (m_zapper->m_pOpt->m_noProcedureSplitting)
        jitFlags &= ~CORJIT_FLG_PROCSPLIT;

    //never emit inlined polls for NGen'd code.  The extra indirection is not optimal.
    if (jitFlags & CORJIT_FLG_GCPOLL_INLINE)
    {
        jitFlags &= ~CORJIT_FLG_GCPOLL_INLINE;
        jitFlags |= CORJIT_FLG_GCPOLL_CALLS;
    }

    // If the method is specified for min-opts then turn everything off
    if (jitFlags & CORJIT_FLG_MIN_OPT)
        jitFlags &= ~(CORJIT_FLG_BBINSTR | CORJIT_FLG_BBOPT | CORJIT_FLG_PROCSPLIT);

    // Rejit is now enabled by default for NGEN'ed code. This costs us
    // some size in exchange for diagnostic functionality, but we've got
    // further work planned that should mitigate the size increase.
    jitFlags |= CORJIT_FLG_PROF_REJIT_NOPS;

#ifdef FEATURE_READYTORUN_COMPILER
    if (IsReadyToRunCompilation())
        jitFlags |= CORJIT_FLG_READYTORUN;
#endif

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

#ifdef WIN64EXCEPTIONS
    return m_pImage->m_pGCInfoTable->GetGCInfo(m_pGCInfo, m_cbGCInfo, m_pMainUnwindInfo, m_cbMainUnwindInfo);
#else
    return m_pImage->m_pGCInfoTable->GetGCInfo(m_pGCInfo, m_cbGCInfo);
#endif // WIN64EXCEPTIONS
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

    // During ngen we look for a hint attribute on the method that indicates
    // the method should be preprocessed for early
    // preparation. This normally happens automatically, but for methods that
    // are prepared explicitly at runtime the needed
    // information is missing from the ngen image, causing costly overheads
    // at runtime. When the author of the method knows about
    // this they can add the hint and reduce the perf cost at runtime.
    m_pImage->m_pPreloader->PrePrepareMethodIfNecessary(m_currentMethodHandle);

    int jitFlags = ComputeJitFlags(m_currentMethodHandle);

#ifdef FEATURE_READYTORUN_COMPILER
    if (IsReadyToRunCompilation())
    {
        // READYTORUN: FUTURE: Producedure spliting
        jitFlags &= ~CORJIT_FLG_PROCSPLIT;

        DWORD methodAttribs = getMethodAttribs(m_currentMethodHandle);
        if (!(methodAttribs & CORINFO_FLG_NOSECURITYWRAP) || (methodAttribs & CORINFO_FLG_SECURITYCHECK))
        {
            m_zapper->Warning(W("ReadyToRun: Methods with security checks not supported\n"));
            ThrowHR(E_NOTIMPL);
        }
    }
#endif

    if ((jitFlags & CORJIT_FLG_SKIP_VERIFICATION) == 0)
    {
        BOOL raiseVerificationException, unverifiableGenericCode;

        jitFlags = GetCompileFlagsIfGenericInstantiation(
                        m_currentMethodHandle,
                        (CorJitFlag)jitFlags,
                        this,
                        &raiseVerificationException,
                        &unverifiableGenericCode);

        // Instead of raising a VerificationException, we will leave the method
        // uncompiled. If it gets called at runtime, we will raise the
        // VerificationException at that time while trying to compile the method.
        if (raiseVerificationException)
            return;
    }

    if (m_pImage->m_stats)
    {
        m_pImage->m_stats->m_methods++;
        m_pImage->m_stats->m_ilCodeSize += m_currentMethodInfo.ILCodeSize;
    }

    CorJitResult res = CORJIT_SKIPPED;
    
    BYTE *pCode;
    ULONG cCode;

#ifdef ALLOW_SXS_JIT_NGEN
    if (m_zapper->m_alternateJit)
    {
        REMOVE_STACK_GUARD;

        res = m_zapper->m_alternateJit->compileMethod( this,
                                                     &m_currentMethodInfo,
                                                     jitFlags,
                                                     &pCode,
                                                     &cCode );
        if (FAILED(res))
        {
            // We will fall back to the "main" JIT on failure.
            ResetForJitRetry();
        }
    }
#endif // ALLOW_SXS_JIT_NGEN

    if (FAILED(res))
    {
        REMOVE_STACK_GUARD;

        ICorJitCompiler * pCompiler = m_zapper->m_pJitCompiler;
        res = pCompiler->compileMethod(this,
                                    &m_currentMethodInfo,
                                    jitFlags,
                                    &pCode,
                                    &cCode);

        if (FAILED(res))
        {
            ThrowExceptionForJitResult(res);
        }
    }
    
    MethodCompileComplete(m_currentMethodInfo.ftn);

#ifdef _TARGET_X86_
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

        for (int i = 0; i < _countof(equivalentNodes); i++)
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

#ifdef WIN64EXCEPTIONS
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
                hash = ((hash << 5) + hash) ^ (COUNT_T)(pTarget);
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

#ifdef WIN64EXCEPTIONS
    pMethod->m_pUnwindInfoFragments = m_pUnwindInfoFragments;

    // Set the combined GCInfo + UnwindInfo blob
    m_pUnwindInfo->SetUnwindData(pMethod->m_pGCInfo);

#if defined(_TARGET_AMD64_)
    if (m_pChainedColdUnwindInfo != NULL)
    {
        // Chain the cold unwind info with the hot unwind info
        m_pChainedColdUnwindInfo->SetUnwindData(m_pUnwindInfo);
    }
#endif // _TARGET_AMD64_

#endif // WIN64EXCEPTIONS

#ifndef FEATURE_FULL_NGEN
    //
    // Method code deduplication
    //
    // For now, the only methods eligible for de-duplication are IL stubs
    //
    if (m_zapper->m_pOpt->m_compilerFlags & CORJIT_FLG_IL_STUB)
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
        if (m_zapper->m_pOpt->m_compilerFlags & CORJIT_FLG_IL_STUB)
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

    return 0;
}

IEEMemoryManager* ZapInfo::getMemoryManager()
{
    return GetEEMemoryManager();
}

HRESULT ZapInfo::allocBBProfileBuffer (
    ULONG                         cBlock,
    ICorJitInfo::ProfileBuffer ** ppBlock
    )
{
    HRESULT hr;

    if (m_zapper->m_pOpt->m_compilerFlags & CORJIT_FLG_IL_STUB)
    {
        *ppBlock = NULL;
        return E_NOTIMPL;
    }

    // @TODO: support generic methods from other assemblies
    if (m_currentMethodModule != m_pImage->m_hModule)
    {
        *ppBlock = NULL;
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

    DWORD totalSize = (DWORD) (cBlock * sizeof(ICorJitInfo::ProfileBuffer)) + sizeof(CORBBTPROF_METHOD_HEADER);
    m_pProfileData = ZapBlobWithRelocs::NewAlignedBlob(m_pImage, NULL, totalSize, sizeof(DWORD));
    CORBBTPROF_METHOD_HEADER * profileData = (CORBBTPROF_METHOD_HEADER *) m_pProfileData->GetData();
    profileData->size           = totalSize;
    profileData->cDetail        = 0;
    profileData->method.token   = md;
    profileData->method.ILSize  = m_currentMethodInfo.ILCodeSize;
    profileData->method.cBlock  = cBlock;

    *ppBlock = (ICorJitInfo::ProfileBuffer *)(&profileData->method.block[0]);

    return S_OK;
}

HRESULT ZapInfo::getBBProfileData (
    CORINFO_METHOD_HANDLE         ftnHnd,
    ULONG *                       pCount,
    ICorJitInfo::ProfileBuffer ** ppBlock,
    ULONG *                       numRuns
    )
{
    _ASSERTE(ppBlock);
    _ASSERTE(pCount);
    _ASSERTE(ftnHnd == m_currentMethodHandle);

    HRESULT hr;

    // Initialize outputs in case we return E_FAIL
    *ppBlock = NULL;
    *pCount = 0;
    if (numRuns)
    {
        *numRuns = 0;
    }

    // For generic instantiations whose IL is in another module,
    // the profile data is in that module
    // @TODO: Fetch the profile data from the other module.
    if ((m_currentMethodModule != m_pImage->m_hModule) ||
        (m_zapper->m_pOpt->m_compilerFlags & CORJIT_FLG_IL_STUB))
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

    if (numRuns)
    {
        *numRuns =  m_pImage->m_profileDataNumRuns;
    }

    const ZapImage::ProfileDataHashEntry * foundEntry = m_pImage->profileDataHashTable.LookupPtr(md);

    if (foundEntry == NULL)
    {
        return E_FAIL;
    }

    // The md must match.
    _ASSERTE(foundEntry->md == md); 

    //
    // We found the md. Let's retrieve the profile data.
    //
    _ASSERTE(foundEntry->pos > 0);                                   // The target position cannot be 0.
    _ASSERTE(foundEntry->size >= sizeof(CORBBTPROF_METHOD_HEADER));   // The size must at least this

    ProfileReader profileReader(DataSection_MethodBlockCounts->pData, DataSection_MethodBlockCounts->dataSize);

    // Locate the method in interest.
    SEEK(foundEntry->pos);     
    CORBBTPROF_METHOD_HEADER *  profileData;
    READ_SIZE(profileData, CORBBTPROF_METHOD_HEADER, foundEntry->size);
    _ASSERTE(profileData->method.token == foundEntry->md);  // We should be looking at the right method
    _ASSERTE(profileData->size == foundEntry->size);        // and the cached size must match

    *ppBlock = (ICorJitInfo::ProfileBuffer *) &profileData->method.block[0];
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
    bool optForSize = ((m_zapper->m_pOpt->m_compilerFlags & CORJIT_FLG_SIZE_OPT) == CORJIT_FLG_SIZE_OPT);

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
            align = sizeof(TADDR);
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

#ifdef _WIN64
    if (size & 0xFFFFFFFF80000000LL)
    {
        IfFailThrow(CORJIT_OUTOFMEM);
    }
#endif // _WIN64

    m_pGCInfo = new BYTE[size];
    m_cbGCInfo = size;

    return m_pGCInfo;
}

void ZapInfo::yieldExecution()
{
    // nothing necessary here
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

        if ((m_zapper->m_pOpt->m_compilerFlags & CORJIT_FLG_IL_STUB) && (clause->ClassToken != 0))
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

            CORINFO_RESOLVED_TOKEN resolvedToken = { 0 };
            resolvedToken.tokenContext = MAKE_METHODCONTEXT(m_currentMethodInfo.ftn);
            resolvedToken.tokenScope = m_currentMethodInfo.scope;
            resolvedToken.token = ilClause->ClassToken;
            resolvedToken.tokenType = CORINFO_TOKENKIND_Class;

            resolveToken(&resolvedToken);

            CORINFO_CLASS_HANDLE systemObjectHandle = getBuiltinClass(CLASSID_SYSTEM_OBJECT);
            _ASSERTE(systemObjectHandle == resolvedToken.hClass);
#endif // _DEBUG

            ilClause->ClassToken = mdTypeRefNil; 
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
#if defined(CROSSGEN_COMPILE) && !defined(FEATURE_CORECLR)
    ThrowHR(COR_E_INVALIDPROGRAM);
#else

#if defined(_DEBUG)
    return(_DbgBreakCheck(szFile, iLine, szExpr));
#else
    return(true);       // break into debugger
#endif

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
#ifdef WIN64EXCEPTIONS
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
#if defined(_TARGET_AMD64_)
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
#endif // WIN64EXCEPTIONS
}

BOOL ZapInfo::logMsg(unsigned level, const char *fmt, va_list args)
{
    if (m_zapper->m_pOpt->m_legacyMode)
        return FALSE;

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

SIZE_T* ZapInfo::getAddrModuleDomainID(CORINFO_MODULE_HANDLE   module)
{
    return m_pEEJitInfo->getAddrModuleDomainID(module);
}

LONG * ZapInfo::getAddrOfCaptureThreadGlobal(void **ppIndirection)
{
    _ASSERTE(ppIndirection != NULL);

    *ppIndirection = (LONG *) m_pImage->GetInnerPtr(m_pImage->m_pEEInfoTable,
        offsetof(CORCOMPILE_EE_INFO_TABLE, addrOfCaptureThreadGlobal));
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
        _ASSERTE(!"embedMethodHandle");
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

CORINFO_LOOKUP_KIND
ZapInfo::getLocationOfThisType(CORINFO_METHOD_HANDLE   context)
{
    return m_pEEJitInfo->getLocationOfThisType(context);
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
        _ASSERTE(!"embedGenericSignature");
        ThrowHR(E_NOTIMPL);
    }

    if (pLookup->runtimeLookup.signature != NULL)
    {
        pLookup->runtimeLookup.signature = m_pImage->GetImportTable()->GetGenericSignature(
            pLookup->runtimeLookup.signature, pLookup->lookupKind.runtimeLookupKind == CORINFO_LOOKUP_METHODPARAM);
    }
}

void* ZapInfo::getTailCallCopyArgsThunk (
                    CORINFO_SIG_INFO       *pSig,
                    CorInfoHelperTailCallSpecialHandling flags)
{
    void * pStub = m_pEEJitInfo->getTailCallCopyArgsThunk(pSig, flags);
    if (pStub == NULL)
        return NULL;
    return m_pImage->GetWrappers()->GetStub(pStub);
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
        *ppIndirection = m_pImage->GetInnerPtr(GetProfilingHandleImport(), kZapProfilingHandleImportValueIndexEnterAddr * sizeof(TADDR));
        return NULL;
    case CORINFO_HELP_PROF_FCN_LEAVE:
        *ppIndirection = m_pImage->GetInnerPtr(GetProfilingHandleImport(), kZapProfilingHandleImportValueIndexLeaveAddr * sizeof(TADDR));
        return NULL;
    case CORINFO_HELP_PROF_FCN_TAILCALL:
        *ppIndirection = m_pImage->GetInnerPtr(GetProfilingHandleImport(), kZapProfilingHandleImportValueIndexTailcallAddr * sizeof(TADDR));
        return NULL;
#ifdef _TARGET_AMD64_
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
#if defined(_TARGET_ARM_)
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

#ifdef _TARGET_ARM_
    pEntryPointOrThunkToEmbed = m_pImage->GetInnerPtr(pEntryPointOrThunkToEmbed, THUMB_CODE);
#endif

    return pEntryPointOrThunkToEmbed;
}

void ZapInfo::getFunctionEntryPoint(
                                CORINFO_METHOD_HANDLE   ftn,                 /* IN  */
                                CORINFO_CONST_LOOKUP *  pResult,             /* OUT */
                                CORINFO_ACCESS_FLAGS    accessFlags/*=CORINFO_ACCESS_ANY*/)
{
    if (IsReadyToRunCompilation())
    {
        // READYTORUN: FUTURE: JIT still calls this for tail. and jmp instructions
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

void * ZapInfo::getPInvokeUnmanagedTarget(CORINFO_METHOD_HANDLE method, void **ppIndirection)
{
    // We will never be able to return this directly in prejit mode.
    _ASSERTE(ppIndirection != NULL);

    *ppIndirection = NULL;
    return NULL;
}

void * ZapInfo::getAddressOfPInvokeFixup(CORINFO_METHOD_HANDLE method,void **ppIndirection)
{
    _ASSERTE(ppIndirection != NULL);

    m_pImage->m_pPreloader->AddMethodToTransitiveClosureOfInstantiations(method);

    CORINFO_MODULE_HANDLE moduleHandle = m_pEECompileInfo->GetLoaderModuleForEmbeddableMethod(method);
    if (moduleHandle == m_pImage->m_hModule 
        && m_pImage->m_pPreloader->CanEmbedMethodHandle(method, m_currentMethodHandle))
    {
        *ppIndirection = NULL;
        return PVOID(m_pImage->GetWrappers()->GetAddrOfPInvokeFixup(method));
    }

    //
    // Note we could a fixup to a direct call site, rather than to
    // the indirection.  This would saves us an extra indirection, but changes the
    // semantics slightly (so that the pinvoke will be bound when the calling
    // method is first run, not at the exact moment of the first pinvoke.)
    //

    ZapImport * pImport = m_pImage->GetImportTable()->GetIndirectPInvokeTargetImport(method);
    AppendConditionalImport(pImport);

    *ppIndirection = pImport;
    return NULL;
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
    *pProfilerHandle = m_pImage->GetInnerPtr(GetProfilingHandleImport(), kZapProfilingHandleImportValueIndexClientData * sizeof(TADDR));

    // All functions get hooked in ngen /Profile
    *pbHookFunction = TRUE;

    //
    // This is the NGEN case, where we always do indirection on the handle so we can fix it up at load time.
    //
    *pbIndirectedHandles = TRUE;
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

#ifdef FEATURE_READYTORUN_COMPILER
    if (IsReadyToRunCompilation())
    {
        if (pResult->sig.isVarArg())
        {
            m_zapper->Warning(W("ReadyToRun: VarArg methods not supported\n"));
            ThrowHR(E_NOTIMPL);
        }

        if (pResult->accessAllowed != CORINFO_ACCESS_ALLOWED)
        {
            m_zapper->Warning(W("ReadyToRun: Runtime method access checks not supported\n"));
            ThrowHR(E_NOTIMPL);
        }

        if (pResult->methodFlags & CORINFO_FLG_SECURITYCHECK)
        {
            m_zapper->Warning(W("ReadyToRun: Methods with security checks not supported\n"));
            ThrowHR(E_NOTIMPL);
        }
    }
#endif

    if (flags & CORINFO_CALLINFO_KINDONLY)
        return;

    if (IsReadyToRunCompilation())
    {
        if (pResult->thisTransform == CORINFO_BOX_THIS)
        {
            // READYTORUN: FUTURE: Optionally create boxing stub at runtime
            m_zapper->Warning(W("ReadyToRun: Implicit boxing for calls to constrained methods not supported\n"));
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
                embedGenericSignature(&pResult->stubLookup);
                return;
            }

#ifdef FEATURE_READYTORUN_COMPILER
            if (IsReadyToRunCompilation())
            {
                ZapImport * pImport = m_pImage->GetImportTable()->GetStubDispatchCell(pResolvedToken);

                pResult->stubLookup.constLookup.accessType   = IAT_PVALUE;
                pResult->stubLookup.constLookup.addr         = pImport;
                break;
            }
#endif

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
        break;


    case CORINFO_CALL_CODE_POINTER:
        _ASSERTE(pResult->codePointerLookup.lookupKind.needsRuntimeLookup);
        embedGenericSignature(&pResult->codePointerLookup);

        // There is no easy way to detect method referenced via generic lookups in generated code.
        // Report this method reference unconditionally.
        m_pImage->m_pPreloader->MethodReferencedByCompiledCode(pResult->hMethod);
        return;

    case CORINFO_CALL:
#ifdef FEATURE_READYTORUN_COMPILER
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
                pImport = m_pImage->GetImportTable()->GetExternalMethodCell(pResult->hMethod, pResolvedToken, pConstrainedResolvedToken);
            }

            // READYTORUN: FUTURE: Direct calls if possible
            pResult->codePointerLookup.constLookup.accessType   = IAT_PVALUE;
            pResult->codePointerLookup.constLookup.addr         = pImport;
        }
#endif
        break;

    case CORINFO_VIRTUALCALL_VTABLE:
        _ASSERTE(!IsReadyToRunCompilation());
        break;

    case CORINFO_VIRTUALCALL_LDVIRTFTN:
#ifdef FEATURE_READYTORUN_COMPILER
        if (IsReadyToRunCompilation())
        {
            DWORD fAtypicalCallsite = (flags & CORINFO_CALLINFO_ATYPICAL_CALLSITE) ? CORINFO_HELP_READYTORUN_ATYPICAL_CALLSITE : 0;

            ZapImport * pImport = m_pImage->GetImportTable()->GetDynamicHelperCell(
                (CORCOMPILE_FIXUP_BLOB_KIND)(ENCODE_VIRTUAL_ENTRY | fAtypicalCallsite), pResult->hMethod, pResolvedToken);

            pResult->codePointerLookup.constLookup.accessType   = IAT_PVALUE;
            pResult->codePointerLookup.constLookup.addr         = pImport;

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

#ifdef FEATURE_READYTORUN_COMPILER
    if (IsReadyToRunCompilation() && pResult->sig.hasTypeArg())
    {
        if (pResult->exactContextNeedsRuntimeLookup)
        {
            // READYTORUN: FUTURE: Generics
            _ASSERTE(!"Generics");
            ThrowHR(E_NOTIMPL);
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
#endif
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

    if(!(m_zapper->m_pOpt->m_compilerFlags & CORJIT_FLG_DEBUG_CODE))
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
    _ASSERTE(ppIndirection != NULL);

    CORINFO_CLASS_HANDLE hClass = m_pEEJitInfo->getFieldClass(field);

    m_pImage->m_pPreloader->AddTypeToTransitiveClosureOfInstantiations(hClass);

    ZapImport * pImport = m_pImage->GetImportTable()->GetStaticFieldAddressImport(field);
    AppendConditionalImport(pImport);

    // Field address is not aligned thus we can not store it in the same location as token.
    *ppIndirection = m_pImage->GetInnerPtr(pImport, sizeof(TADDR));

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
#if defined(_TARGET_X86_) || defined(_TARGET_AMD64_)
    case IMAGE_REL_BASED_REL32:
#endif // _TARGET_X86_ || _TARGET_AMD64_
        location = (PBYTE)location + slotNum;
        break;

#if defined(_TARGET_ARM_)
    case IMAGE_REL_BASED_THUMB_MOV32:
    case IMAGE_REL_BASED_THUMB_BRANCH24:
        break;
#endif

#if defined(_TARGET_ARM64_)
    case IMAGE_REL_ARM64_BRANCH26:
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
#if defined(_TARGET_AMD64_) && !defined(FEATURE_CORECLR)
        _ASSERTE(!"Why we are not using RIP relative address?");
#endif
        *(UNALIGNED TADDR *)location = (TADDR)targetOffset;
        break;

#if defined(_TARGET_X86_) || defined(_TARGET_AMD64_)
    case IMAGE_REL_BASED_REL32:
        *(UNALIGNED INT32 *)location = targetOffset + addlDelta;
        break;
#endif // _TARGET_X86_ || _TARGET_AMD64_

#if defined(_TARGET_ARM_)
    case IMAGE_REL_BASED_THUMB_MOV32:
        PutThumb2Mov32((UINT16 *)location, targetOffset);
        break;

    case IMAGE_REL_BASED_THUMB_BRANCH24:
        if (!FitsInThumb2BlRel24(targetOffset))
            ThrowHR(COR_E_OVERFLOW);
        PutThumb2BlRel24((UINT16 *)location, targetOffset);
        break;
#endif

#if defined(_TARGET_ARM64_)
    case IMAGE_REL_ARM64_BRANCH26:
        if (!FitsInRel28(targetOffset))
            ThrowHR(COR_E_OVERFLOW);
        PutArm64Rel28((UINT32 *)location, targetOffset);
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
        COUNT_T nEstimatedRelocations = (COUNT_T)(totalCodeSize / (2 * sizeof(TADDR)));
        if (nEstimatedRelocations > 1)
            m_CodeRelocations.Preallocate(nEstimatedRelocations);
    }

    m_CodeRelocations.Append(r);
}

WORD ZapInfo::getRelocTypeHint(void * target)
{
#ifdef _TARGET_AMD64_
    // There should be no external pointers
    return IMAGE_REL_BASED_REL32;
#elif defined(_TARGET_ARM_)
    // Use full 32-bit branch targets when retrying compilation on ARM
    if (m_zapper->m_pOpt->m_fNGenLastRetry)
        return (WORD)-1;
    return IMAGE_REL_BASED_THUMB_BRANCH24;
#else
    // No hints
    return (WORD)-1;
#endif
}

void ZapInfo::getModuleNativeEntryPointRange(void** pStart, void** pEnd)
{
    ULONG rvaStart, rvaEnd;

    // Initialize outparams to default range of (0,0).
    *pStart = 0;
    *pEnd = 0;

    // If this is ILONLY, there are no native entry points.
    if (m_pImage->m_ModuleDecoder.IsILOnly())
    {
        return;
    }

    rvaStart = rvaEnd = 0;

    // Walk the section table looking for a section named .nep.

    IMAGE_SECTION_HEADER *section = m_pImage->m_ModuleDecoder.FindFirstSection();
    IMAGE_SECTION_HEADER *sectionEnd = section + m_pImage->m_ModuleDecoder.GetNumberOfSections();
    while (section < sectionEnd)
    {
        if (strncmp((const char *)(section->Name), ".nep", IMAGE_SIZEOF_SHORT_NAME) == 0)
        {
            rvaStart = VAL32(section->VirtualAddress);
            rvaEnd = rvaStart + VAL32(section->Misc.VirtualSize);
            if (rvaStart < rvaEnd)
            {
                // RVA will be fixed up to the actual address at runtime
                CORCOMPILE_EE_INFO_TABLE * pEEInfoTable = (CORCOMPILE_EE_INFO_TABLE *)m_pImage->m_pEEInfoTable->GetData();
                pEEInfoTable->nativeEntryPointStart = (BYTE*)((ULONG_PTR)rvaStart);
                pEEInfoTable->nativeEntryPointEnd = (BYTE*)((ULONG_PTR)rvaEnd);

                *pStart = m_pImage->GetInnerPtr(m_pImage->m_pEEInfoTable,
                    offsetof(CORCOMPILE_EE_INFO_TABLE, nativeEntryPointStart));
                *pEnd = m_pImage->GetInnerPtr(m_pImage->m_pEEInfoTable,
                    offsetof(CORCOMPILE_EE_INFO_TABLE, nativeEntryPointEnd));
            }
            break;
        }
        section++;
    }
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
            // cannot optimize any secure delegate, give up
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

CorInfoType ZapInfo::getHFAType(CORINFO_CLASS_HANDLE hClass)
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

void * ZapInfo::allocateArray(ULONG cBytes)
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

bool ZapInfo::isWriteBarrierHelperRequired(
                        CORINFO_FIELD_HANDLE    field)
{
    return m_pEEJitInfo->isWriteBarrierHelperRequired(field);
}

void ZapInfo::getFieldInfo (CORINFO_RESOLVED_TOKEN * pResolvedToken,
                            CORINFO_METHOD_HANDLE  callerHandle,
                            CORINFO_ACCESS_FLAGS   flags,
                            CORINFO_FIELD_INFO    *pResult)
{
    m_pEEJitInfo->getFieldInfo(pResolvedToken, callerHandle, flags, pResult);

#ifdef FEATURE_READYTORUN_COMPILER
    if (IsReadyToRunCompilation())
    {
        if (pResult->accessAllowed != CORINFO_ACCESS_ALLOWED)
        {
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

                        if (pResult->offset > MAX_UNCHECKED_OFFSET_FOR_NULL_OBJECT / 2)
                        {
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

                        if (pResult->offset > MAX_UNCHECKED_OFFSET_FOR_NULL_OBJECT / 2)
                        {
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
            // READYTORUN: FUTURE: Generics
            _ASSERTE(!"Generics");
            ThrowHR(E_NOTIMPL);
            break;

        case CORINFO_FIELD_STATIC_ADDRESS:           // field at given address
        case CORINFO_FIELD_STATIC_RVA_ADDRESS:       // RVA field at given address
        case CORINFO_FIELD_STATIC_ADDR_HELPER:       // static field accessed using address-of helper (argument is FieldDesc *)
        case CORINFO_FIELD_STATIC_TLS:
            m_zapper->Warning(W("ReadyToRun: Rare kinds of static fields not supported\n"));
            ThrowHR(E_NOTIMPL);
            break;

        case CORINFO_FIELD_INTRINSIC_ZERO:
        case CORINFO_FIELD_INTRINSIC_EMPTY_STRING:
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

BOOL ZapInfo::canInlineTypeCheckWithObjectVTable (CORINFO_CLASS_HANDLE cls)
{
    return m_pEEJitInfo->canInlineTypeCheckWithObjectVTable(cls);
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
            CORINFO_CONTEXT_HANDLE  context,
            BOOL                    speculative)
{
    return m_pEEJitInfo->initClass(field, method, context, speculative);
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

BOOL ZapInfo::canCast(CORINFO_CLASS_HANDLE child,
                                CORINFO_CLASS_HANDLE parent)
{
    return m_pEEJitInfo->canCast(child, parent);
}

BOOL ZapInfo::areTypesEquivalent(CORINFO_CLASS_HANDLE cls1, CORINFO_CLASS_HANDLE cls2)
{
    return m_pEEJitInfo->areTypesEquivalent(cls1, cls2);
}

CORINFO_CLASS_HANDLE ZapInfo::mergeClasses(
                                CORINFO_CLASS_HANDLE cls1,
                                CORINFO_CLASS_HANDLE cls2)
{
    return m_pEEJitInfo->mergeClasses(cls1, cls2);
}

BOOL ZapInfo::shouldEnforceCallvirtRestriction(
        CORINFO_MODULE_HANDLE scopeHnd)
{
    return m_zapper->m_pEEJitInfo->shouldEnforceCallvirtRestriction(scopeHnd);
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

    // FieldDesc::SaveContents() does not save the RVA blob for IJW modules.
    if (!m_pImage->m_ModuleDecoder.IsILOnly())
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

#ifndef FEATURE_CORECLR

        // Is this mscorlib.dll (which has ModuleDomainId of 0 (tagged == 1), then you don't need a fixup
        if (moduleId == (size_t) 1)
        {
            *ppIndirection = NULL;
            return (size_t) 1;
        }
#endif

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


CorInfoHelpFunc ZapInfo::getNewHelper(CORINFO_RESOLVED_TOKEN * pResolvedToken, CORINFO_METHOD_HANDLE callerHandle)
{
    classMustBeLoadedBeforeCodeIsRun(pResolvedToken->hClass);
    return m_pEEJitInfo->getNewHelper(pResolvedToken, callerHandle);
}

CorInfoHelpFunc ZapInfo::getSharedCCtorHelper(CORINFO_CLASS_HANDLE clsHnd)
{
    return m_pEEJitInfo->getSharedCCtorHelper(clsHnd);
}

CorInfoHelpFunc ZapInfo::getSecurityPrologHelper(CORINFO_METHOD_HANDLE ftn)
{
    return m_pEEJitInfo->getSecurityPrologHelper(ftn);
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
    return m_pEEJitInfo->getCastingHelper(pResolvedToken, fThrowing);
}

CorInfoHelpFunc ZapInfo::getNewArrHelper(CORINFO_CLASS_HANDLE arrayCls)
{
    return m_pEEJitInfo->getNewArrHelper(arrayCls);
}

void ZapInfo::getReadyToRunHelper(
        CORINFO_RESOLVED_TOKEN * pResolvedToken,
        CorInfoHelpFunc          id,
        CORINFO_CONST_LOOKUP *   pLookup
        )
{
#ifdef FEATURE_READYTORUN_COMPILER
    _ASSERTE(IsReadyToRunCompilation());

    ZapImport * pImport = NULL;

    DWORD fAtypicalCallsite = (id & CORINFO_HELP_READYTORUN_ATYPICAL_CALLSITE);
    id = (CorInfoHelpFunc)(id & ~CORINFO_HELP_READYTORUN_ATYPICAL_CALLSITE);

    switch (id)
    {
    case CORINFO_HELP_READYTORUN_NEW:
        pImport = m_pImage->GetImportTable()->GetDynamicHelperCell(
            (CORCOMPILE_FIXUP_BLOB_KIND)(ENCODE_NEW_HELPER | fAtypicalCallsite), pResolvedToken->hClass);
        break;

    case CORINFO_HELP_READYTORUN_NEWARR_1:
        pImport = m_pImage->GetImportTable()->GetDynamicHelperCell(
            (CORCOMPILE_FIXUP_BLOB_KIND)(ENCODE_NEW_ARRAY_HELPER | fAtypicalCallsite), pResolvedToken->hClass);
        break;

    case CORINFO_HELP_READYTORUN_ISINSTANCEOF:
        pImport = m_pImage->GetImportTable()->GetDynamicHelperCell(
            (CORCOMPILE_FIXUP_BLOB_KIND)(ENCODE_ISINSTANCEOF_HELPER | fAtypicalCallsite), pResolvedToken->hClass);
        break;

    case CORINFO_HELP_READYTORUN_CHKCAST:
        pImport = m_pImage->GetImportTable()->GetDynamicHelperCell(
            (CORCOMPILE_FIXUP_BLOB_KIND)(ENCODE_CHKCAST_HELPER | fAtypicalCallsite), pResolvedToken->hClass);
        break;

    case CORINFO_HELP_READYTORUN_STATIC_BASE:
        if (m_pImage->GetCompileInfo()->IsInCurrentVersionBubble(m_pEEJitInfo->getClassModule(pResolvedToken->hClass)))
        {
            pImport = m_pImage->GetImportTable()->GetDynamicHelperCell(
                (CORCOMPILE_FIXUP_BLOB_KIND)(ENCODE_CCTOR_TRIGGER | fAtypicalCallsite), pResolvedToken->hClass);
        }
        else
        {
            // READYTORUN: FUTURE: Cross-module static cctor triggers
            m_zapper->Warning(W("ReadyToRun: Cross-module static cctor triggers not supported\n"));
            ThrowHR(E_NOTIMPL);
        }
        break;

    default:
        _ASSERTE(false);
        ThrowHR(E_NOTIMPL);
    }

    pLookup->accessType = IAT_PVALUE;
    pLookup->addr = pImport;
#endif
}

void ZapInfo::getReadyToRunDelegateCtorHelper(
        CORINFO_RESOLVED_TOKEN * pTargetMethod,
        CORINFO_CLASS_HANDLE     delegateType,
        CORINFO_CONST_LOOKUP *   pLookup
        )
{
#ifdef FEATURE_READYTORUN_COMPILER
    _ASSERTE(IsReadyToRunCompilation());

    pLookup->accessType = IAT_PVALUE;
    pLookup->addr = m_pImage->GetImportTable()->GetDynamicHelperCell(
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

CorInfoCanSkipVerificationResult ZapInfo::canSkipVerification (
        CORINFO_MODULE_HANDLE tokenScope)
{
    return m_pEEJitInfo->canSkipVerification(tokenScope);
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


//
// ICorMethodInfo
//

const char* ZapInfo::getMethodName(CORINFO_METHOD_HANDLE ftn, const char **moduleName)
{
    return m_pEEJitInfo->getMethodName(ftn, moduleName);
}

unsigned ZapInfo::getMethodHash(CORINFO_METHOD_HANDLE ftn)
{
    return m_pEEJitInfo->getMethodHash(ftn);
}

DWORD ZapInfo::getMethodAttribs(CORINFO_METHOD_HANDLE ftn)
{
    return m_pEEJitInfo->getMethodAttribs(ftn);
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
    bool result = m_pEEJitInfo->getMethodInfo(ftn, info);
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

#ifndef FEATURE_CORECLR
    if (!dontInline(inlineResult) && inlineeHnd != NULL)
    {
        // We deliberately report  m_currentMethodHandle (not inlinerHnd) as inliner, because
        // if m_currentMethodHandle != inlinerHnd, it simply means that inlinerHnd is intermediate link 
        // in inlining into m_currentMethodHandle, and we have no interest to track those intermediate links now.
        m_pImage->m_pPreloader->ReportInlining(m_currentMethodHandle, inlineeHnd);
    }
#endif //FEATURE_CORECLR

    return m_pEEJitInfo->reportInliningDecision(inlinerHnd, inlineeHnd, inlineResult, reason);
}


CorInfoInstantiationVerification ZapInfo::isInstantiationOfVerifiedGeneric(
        CORINFO_METHOD_HANDLE method)
{
    return m_pEEJitInfo->isInstantiationOfVerifiedGeneric(method);
}


void ZapInfo::initConstraintsForVerification(CORINFO_METHOD_HANDLE method,
                                                            BOOL *pfHasCircularClassConstraints,
                                                            BOOL *pfHasCircularMethodConstraints)
{
     m_pEEJitInfo->
              initConstraintsForVerification(method,pfHasCircularClassConstraints,pfHasCircularMethodConstraints);
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


CorInfoCanSkipVerificationResult ZapInfo::canSkipMethodVerification (
        CORINFO_METHOD_HANDLE ftnHandle)
{
    // ILStubs are generated internally by the CLR. There is no need to
    // verify it, or any of its callees.
    if (m_zapper->m_pOpt->m_compilerFlags & CORJIT_FLG_IL_STUB)
        return CORINFO_VERIFICATION_CAN_SKIP;

    CorInfoCanSkipVerificationResult canSkipVer =
        m_pEEJitInfo->canSkipMethodVerification(ftnHandle);

    if (canSkipVer == CORINFO_VERIFICATION_RUNTIME_CHECK)
    {
        // Transparent code could be partial trust, but we don't know at NGEN time.
        // Since the JIT is not hardened against unverifiable/illegal code, tell it
        // to just not jit the method if it hits unverifiable code, rathern than
        // injecting a runtime callout and continuing trying to JIT the method.
        canSkipVer = CORINFO_VERIFICATION_DONT_JIT;
    }

    return canSkipVer;
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
                                                  unsigned * pOffsetAfterIndirection)
{
    m_pEEJitInfo->getMethodVTableOffset(method, pOffsetOfIndirection, pOffsetAfterIndirection);
}

CorInfoIntrinsics ZapInfo::getIntrinsicID(CORINFO_METHOD_HANDLE method,
                                          bool * pMustExpand)
{
    return m_pEEJitInfo->getIntrinsicID(method, pMustExpand);
}

bool ZapInfo::isInSIMDModule(CORINFO_CLASS_HANDLE classHnd)
{
    return m_pEEJitInfo->isInSIMDModule(classHnd);
}

CorInfoUnmanagedCallConv ZapInfo::getUnmanagedCallConv(CORINFO_METHOD_HANDLE method)
{
    return m_pEEJitInfo->getUnmanagedCallConv(method);
}

BOOL ZapInfo::pInvokeMarshalingRequired(CORINFO_METHOD_HANDLE method,
                                                       CORINFO_SIG_INFO* sig)
{
    // READYTORUN: FUTURE: P/Invoke
    if (IsReadyToRunCompilation())
        return TRUE;

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

BOOL ZapInfo::isDelegateCreationAllowed (
        CORINFO_CLASS_HANDLE        delegateHnd,
        CORINFO_METHOD_HANDLE       calleeHnd)
{
    return m_pEEJitInfo->isDelegateCreationAllowed(delegateHnd, calleeHnd);
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
    ULONG size;
    ICorJitInfo::ProfileBuffer * profileBuffer;
    return SUCCEEDED(getBBProfileData(m_currentMethodHandle, &size, &profileBuffer, NULL));
}
