// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// Methods to support the implementation of Constrained Execution Regions (CERs). This includes logic to walk the IL of methods to
// determine the statically determinable call graph and prepare each submethod (jit, prepopulate generic dictionaries etc.,
// everything needed to ensure that the runtime won't generate implicit failure points during the execution of said call graph).
//

//


#include "common.h"
#include <openum.h>
#include <mdaassistants.h>
#include <constrainedexecutionregion.h>
#include <ecmakey.h>
#include <typestring.h>
#include <jitinterface.h>

#ifdef FEATURE_PREJIT
#include <compile.h>
#endif


// Internal debugging support. Would be nice to use the common logging code but we've run out of unique facility codes and the debug
// info we spew out is of interest to a limited audience anyhow.
#ifdef _DEBUG

#define CER_NOISY_PREPARE       0x00000001
#define CER_NOISY_RESTORE       0x00000002
#define CER_NOISY_CONTRACTS     0x00000004
#define CER_NOISY_WARNINGS      0x00000008
#define CER_NOISY_NGEN_STATS    0x00000010

DWORD g_dwCerLogActions = 0xffffffff;
DWORD GetCerLoggingOptions()
{
    WRAPPER_NO_CONTRACT;
    if (g_dwCerLogActions != 0xffffffff)
        return g_dwCerLogActions;
    return g_dwCerLogActions = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_CerLogging);
}

#define CER_LOG(_reason, _msg) do { if (GetCerLoggingOptions() & CER_NOISY_##_reason) printf _msg; } while (false)
#else
#define CER_LOG(_reason, _msg)
#endif


// Enumeration used to determine the number of inline data bytes included inside a given IL instruction (except for the case of a
// SWITCH instruction, where a dynamic calculation is required).
enum
{
        ArgBytes_InlineNone             = 0,    // no inline args       
        ArgBytes_InlineVar              = 2,    // local variable       (U2 (U1 if Short on))
        ArgBytes_InlineI                = 4,    // an signed integer    (I4 (I1 if Short on))
        ArgBytes_InlineR                = 8,    // a real number        (R8 (R4 if Short on))
        ArgBytes_InlineBrTarget         = 4,    // branch target        (I4 (I1 if Short on))
        ArgBytes_InlineI8               = 8,
        ArgBytes_InlineMethod           = 4,    // method token (U4)
        ArgBytes_InlineField            = 4,    // field token  (U4)
        ArgBytes_InlineType             = 4,    // type token   (U4)
        ArgBytes_InlineString           = 4,    // string TOKEN (U4)
        ArgBytes_InlineSig              = 4,    // signature tok (U4)
        ArgBytes_InlineRVA              = 4,    // ldptr token  (U4)
        ArgBytes_InlineTok              = 4,    // a meta-data token of unknown type (U4)
        ArgBytes_InlineSwitch           = 4,    // count (U4), pcrel1 (U4) .... pcrelN (U4)
        ArgBytes_ShortInlineVar         = 1,
        ArgBytes_ShortInlineI           = 1,
        ArgBytes_ShortInlineR           = 4,
        ArgBytes_ShortInlineBrTarget    = 1
};

// Build an array of argument byte counts as described above by extracting the 'args' field of each entry in opcode.def.
#define OPDEF(c, s, pop, push, args, type, l, s1, s2, ctrl) ArgBytes_##args,
const BYTE g_rOpArgs[] = {
#include <opcode.def>
};
#undef OPDEF


// Global cache of methods and their reliability contract state.
PtrHashCache *g_pMethodContractCache = NULL;


// Private method forward references.
bool IsPcrReference(Module *pModule, mdToken tkMethod);
MethodContext *TokenToMethodDesc(Module *pModule, mdToken tokMethod, SigTypeContext *pTypeContext);
TypeHandle GetTypeFromMemberDefOrRefOrSpecThrowing(Module         *pModule,
                                                   mdToken         tokMethod,
                                                   SigTypeContext *pTypeContext);

bool MethodCallGraphPreparer::ShouldGatherExplicitCERCallInfo()
{
    LIMITED_METHOD_CONTRACT;

    // If we're partially processing a method body (at the top of the call graph), we need to fetch exception handling
    // information to determine possible ranges of interesting IL (potentially each finally and catch clause).
    // 
    // And if we are probing for stack overflow, we need to know if the explicit CER region contains any calls out, in 
    // which case we want to probe in the call to PrepareConstrainedExecutionRegions.  This will ensure that we don't
    // take an SO in boundary code and not be able to call the CER.  When stack probing is disabled, we rip the process
    // if we take an SO anywhere but managed, or if we take an SO with a CER on the stack.  For NGEN images, we need 
    // to always probe because stack probing may be enabled in the runtime, but if we haven't probed in the NGEN image
    // then we could take an SO in boundary code and not be able to crawl the stack to know that we've skipped a CER and
    // need to tear the process.
    //
    // Additionally, if the MDA for illegal PrepareConstrainedRegions call positioning is enabled we gather this information for
    // all methods in the graph.
    return !m_fEntireMethod 
#ifdef MDA_SUPPORTED
        || MDA_GET_ASSISTANT(IllegalPrepareConstrainedRegion)
#endif
#ifdef FEATURE_NATIVE_IMAGE_GENERATION
        || m_fNgen
#endif
        || g_pConfig->ProbeForStackOverflow();
}

MethodCallGraphPreparer::MethodCallGraphPreparer(MethodDesc *pRootMD, SigTypeContext *pRootTypeContext, bool fEntireMethod, bool fExactTypeContext, bool fIgnoreVirtualCERCallMDA)
{
    CONTRACTL {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pRootMD));
        PRECONDITION(CheckPointer(pRootTypeContext));
    } CONTRACTL_END;

    // Canonicalize value type unboxing stubs into their underlying method desc.
    if (pRootMD->IsUnboxingStub())
        pRootMD = pRootMD->GetWrappedMethodDesc();

    m_pRootMD = pRootMD;
    m_pRootTypeContext = pRootTypeContext;
    m_fEntireMethod = fEntireMethod;
    m_fExactTypeContext = fExactTypeContext;
    m_fIgnoreVirtualCERCallMDA = fIgnoreVirtualCERCallMDA;
    
    m_pEHClauses = NULL;
    m_cEHClauses = 0;
    m_pCerPrepInfo = NULL;  
    m_pMethodDecoder = NULL;

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
    m_fNgen = false; 
#endif

    m_pThread = GetThread(); 
    m_fPartialPreparation = false;
    m_fMethodHasCallsWithinExplicitCer = false;
}

// Walk the call graph of the method given by pRootMD (and type context in pRootTypeContext which provides instantiation information
// for generic methods/classes).
//
// If fEntireMethod is true then the entire body of pRootMD is scanned for callsites, otherwise we assume that one or more CER
// exception handlers exist in the method and only the finally and catch blocks of such handlers are scanned for graph roots.
//
// Each method we come across in the call graph (excluding late bound invocation destinations precipitated by virtual or interface
// calls) is jitted and has any generic dictionary information we can determine at jit time prepopulated. This includes implicit
// cctor invocations. If this method is called at ngen time we will attach extra fixup information to the affected method to ensure
// that fixing up the root method of the graph will cause all methods in the graph to be fixed up at that point also.
//
// Some generic dictionary entries may not be prepopulated if unbound type variables exist at the root of the call tree. Such cases
// will be ignored (as for the virtual/interface dispatch case we assume the caller will use an out-of-band mechanism to pre-prepare
// these entries explicitly).
bool MethodCallGraphPreparer::Run()
{
    STANDARD_VM_CONTRACT;

    // Avoid recursion while jitting methods for another preparation.
    if (!m_pThread->GetCerPreparationState()->CanPreparationProceed(m_pRootMD, m_pRootTypeContext))
        return TRUE;   // Assume the worst

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
    // Determine if we're being called in order to provide an ngen image. This impacts whether we actually prepare methods and the
    // type of tracking data we produce. Ideally we'd call GetAppDomain()->IsCompilationDomain() here, but we have to deal with the
    // problem of ngen'ing mscorlib. Mscorlib code is always shared and some of it is run before the compilation domain is fully
    // created (so we'd end up with some methods being prepared without saving any ngen metadata to that effect). So instead we
    // check to see whether this is an ngen process. This will catch those first few mscorlib methods.
    m_fNgen = IsCompilationProcess() != FALSE;

    // We keep a hash table of CERs we've processed on the module object of the root method. See if any work has been done on this
    // CER before. We store different data for ngen and non-ngen cases.
    if (m_fNgen) {

        // Pretty simple in ngen case -- if we've stored a context record for this method at all then we've already processed it.
        if (m_pRootMD->GetModule()->IsNgenCerRootMethod(m_pRootMD)) {
            m_pCerPrepInfo = m_pRootMD->GetModule()->GetCerPrepInfo(m_pRootMD);

            // We always store CerPrepInfo if the method has calls, so if we haven't stored
            // anything then we know it doesn't have any calls
            return (m_pCerPrepInfo && m_pCerPrepInfo->m_fMethodHasCallsWithinExplicitCer);
        }
    } else 
#endif
    {
        // The non-ngen case (normal jit, call to PrepareMethod etc).
        m_pCerPrepInfo = m_pRootMD->GetModule()->GetCerPrepInfo(m_pRootMD);
        if (m_pCerPrepInfo) {

            // Check for the "everything's done" case.
            if (m_pCerPrepInfo->m_fFullyPrepared)
                return m_pCerPrepInfo->m_fMethodHasCallsWithinExplicitCer;

            // Check for the "we can't do anything" case (see below for descriptions of that).
            if (m_pCerPrepInfo->m_fRequiresInstantiation && !m_fExactTypeContext)
                return m_pCerPrepInfo->m_fMethodHasCallsWithinExplicitCer;

            // Check for the "need to prepare per-instantiation, but we've already done this one" case.
            if (m_pCerPrepInfo->m_fRequiresInstantiation) {
                HashDatum sDatum;
                if (m_pCerPrepInfo->m_sIsInitAtInstHash.GetValue(m_pRootTypeContext, &sDatum))
                    return m_pCerPrepInfo->m_fMethodHasCallsWithinExplicitCer;
            }
        }
    }

    // We can't deal with generic methods or methods on generic types that may have some representative type parameters in their
    // instantiation (i.e. some reference types indicated by Object rather than the exact type). The jit will tend to pass us these
    // since it shares code between instantiations over reference types. We can't prepare methods like these completely -- even
    // though we can jit all the method bodies the code might require generic dictionary information at the class or method level
    // that is populated at runtime and can introduce failure points. So we reject such methods immediately (they will need to be
    // prepared at non-jit time by an explicit call to PrepareMethod with a fully instantiated method).
    //
    // In the case where the type context is marked as suspect (m_fExactTypeContext == false) there are a number of possibilites for
    // bad methods the jit will pass us:
    //  1) We're passed a MethodDesc that shared between instantiations (bogus because exact method descs are never shared).
    //  2) We're passed a MethodDesc that's an instantiating stub (bogus because non-shared methods don't need this).
    //  3) We're passed a MethodDesc that has generic variables in its instantiations (I've seen this during ngen).
    //
    // Technically we could do a little better than this -- we could determine whether any of the representative type parameters are
    // actually used within the CER call graph itself. But this would require us to understand the IL at a much deeper level (i.e.
    // parse every instruction that could take a type or member spec and pull apart those specs to see if a type var is used). Plus
    // we couldn't make this determination until we've prepared the entire region and the result is rather brittle from the code
    // author's point of view (i.e. we might prepare a CER automatically one day but stop doing after some relatively subtle changes
    // in the source code).
    m_fPartialPreparation = m_pRootMD->IsSharedByGenericInstantiations() || m_pRootMD->IsInstantiatingStub() || m_pRootMD->ContainsGenericVariables();
    if (!m_fExactTypeContext && m_fPartialPreparation) {
#ifdef MDA_SUPPORTED
        MDA_TRIGGER_ASSISTANT(OpenGenericCERCall, ReportViolation(m_pRootMD));
#endif
        CER_LOG(WARNINGS, ("CER: %s has open type parameters and can't be pre-prepared\n", m_pRootMD->m_pszDebugMethodName));


#ifdef FEATURE_NATIVE_IMAGE_GENERATION
        if (!m_fNgen) 
#endif
        {
            // Set up a prep info structure for this method if it's not there already (the create method takes care of races).
            if (m_pCerPrepInfo == NULL)
                m_pCerPrepInfo = m_pRootMD->GetModule()->CreateCerPrepInfo(m_pRootMD);

            // We may be racing to update the structure at this point but that's OK since the flag we're setting is never cleared once
            // it's set and is always guaranteed to be set before we rely on its value (setting it here is just a performance thing,
            // letting us early-out on multiple attempts to prepare this CER from the jit).
            m_pCerPrepInfo->m_fRequiresInstantiation = true;
        }

        if (! g_pConfig->ProbeForStackOverflow())
        {
            return FALSE;
        }
        m_pCerPrepInfo = m_pRootMD->GetModule()->GetCerPrepInfo(m_pRootMD);

        // We always store CerPrepInfo if the method has calls, so if we haven't stored
        // anything then we know it doesn't have any calls
        return (m_pCerPrepInfo && m_pCerPrepInfo->m_fMethodHasCallsWithinExplicitCer);
        
    }

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
    // If we've been called for a shared generic root method and the exact instantiation (this can happen because ngen lets code
    // execute under some circumstances) we don't currently support saving this information in the ngen image (we don't have a
    // format for the instantiation info). We just ignore the preparation in this case (it will be prepared at runtime).
    if (m_fNgen && m_fPartialPreparation)
        return TRUE;
#endif

    // Prevent inlining of the root method (otherwise it's hard to tell where ThreadAbort exceptions should be delayed). Note that
    // MethodDesc::SetNotInline is thread safe.
    m_pRootMD->SetNotInline(true);

    // Remember the checkpoint for all of our allocations. Keep it in a holder so they'll be unwound if we throw an exception past
    // here.
    CheckPointHolder sCheckpoint(m_pThread->m_MarshalAlloc.GetCheckpoint());

    // Push the current method as the one and only method to process so far.
    m_sLeftToProcess.Push(MethodContext::PerThreadAllocate(m_pRootMD, m_pRootTypeContext));

    MethodContext      *pContext = NULL;               // The current MethodContext we're processing
    
    // Iterate until we run out of methods to process.
    while ((pContext = m_sLeftToProcess.Pop()) != NULL) {

        // Restore the MD if necessary. In particular, if this is an instantiating stub and the wrapped MethodDesc could
        // not be hard bound, then we'll need to restore that pointer before getting it.
        pContext->m_pMethodDesc->CheckRestore();

        // Transfer the method to the already seen stack immediately (we don't want to loop infinitely in the case of method
        // recursion).
        m_sAlreadySeen.Push(pContext);

        // Check if the enclosing class requires a static class constructor to be run. If so, we need to prepare that method as
        // though it were any other call.
        if (pContext->m_pMethodDesc->GetMethodTable()->HasClassConstructor()) {

            // Decode target method into MethodDesc and new SigTypeContext.
            // The type context is easy to derive here : .cctors never have any method type parameters and the class instantiations
            // are those of the method we're currently parsing, so can be simply copied down.
            MethodDesc *pCctor = pContext->m_pMethodDesc->GetCanonicalMethodTable()->GetClassConstructor();
            SigTypeContext sCctorTypeContext(pCctor, pContext->m_sTypeContext.m_classInst, Instantiation());
            MethodContext *pCctorContext = MethodContext::PerThreadAllocate(pCctor, &sCctorTypeContext);

            // Only process this cctor the first time we find it in this call graph.
            if (!m_sAlreadySeen.IsInStack(pCctorContext) && !m_sLeftToProcess.IsInStack(pCctorContext))
                m_sLeftToProcess.Push(pCctorContext);
        }

        // Skip further processing if this method doesn't have an IL body (note that we assume the method we entered with was IL, so
        // we don't need to bother with partial method processing).
        if (!pContext->m_pMethodDesc->IsIL()) {
            _ASSERTE(m_fEntireMethod);
            continue;
        }

        // Locate the IL body of the current method. May have to account for the fact that the current method desc is an
        // instantiating stub and burrow down for the real method desc.
        MethodDesc *pRealMethod = pContext->m_pMethodDesc;
        if (pRealMethod->IsInstantiatingStub()) {
            _ASSERTE(!pRealMethod->ContainsGenericVariables());
            pRealMethod = pRealMethod->GetWrappedMethodDesc();
        }

        COR_ILMETHOD* pILHeader = pRealMethod->GetILHeader();

        // Skip malformed methods. (We should always have method with IL for well-formed images here.)
        if (pILHeader == NULL) {
            continue;
        }

        COR_ILMETHOD_DECODER method(pILHeader);
        m_pMethodDecoder = &method;

        // We want to reget the EH clauses for the current method so that we can scan its handlers
        GetEHClauses();

        LookForInterestingCallsites(pContext);

        // Whatever we've done, we're definitely not processing the top-level method at this point (so we'll be processing full
        // method bodies from now on).
        m_fEntireMethod = true;
    }

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
    if (!m_fNgen) 
#endif
    {
        // Set up a prep info structure for this method if it's not there already (the create method takes care of races).
        // This needs to happen before we start JITing the methods as part of preparation. The JIT needs to know
        // about the CER root in CEEInfo::canTailCall.
        if (m_pCerPrepInfo == NULL)
            m_pCerPrepInfo = m_pRootMD->GetModule()->CreateCerPrepInfo(m_pRootMD);
    }

    // Prevent infinite recursion by recording on the thread which roots we're currently preparing.
    ThreadPreparingCerHolder sCerHolder(this);

    // Once we get here we've run out of methods to process and have recorded each method we visited in the m_sAlreadySeen stack. Now
    // it's time to prepare each of these methods (jit, prepopulate generic dictionaries etc.).
    PrepareMethods();

    return RecordResults();
}


// Determine whether a CER preparation for the given root method (with type context for generic instantiation
// if necessary) can go ahead given any current preparation already being performed on the current thread.
BOOL MethodCallGraphPreparer::CanPreparationProceed(MethodDesc * pMD, SigTypeContext * pTypeContext)
{
    WRAPPER_NO_CONTRACT;
    MethodCallGraphPreparer * pCurrPrep = this;
    while (pCurrPrep)
    {
        // Is the prepartion request for the root method of the current preparer?
        if (pMD == pCurrPrep->m_pRootMD && SigTypeContext::Equal(pTypeContext, pCurrPrep->m_pRootTypeContext))
        {
            // We're already preparing this root, return FALSE to turn the request into a no-op and avoid
            // infinite recursion.
            return FALSE;
        }

        pCurrPrep = pCurrPrep->m_pNext;
    }

    // We found no previous preparation for the same root, so the request can proceed.
    return TRUE;
}

// Methods that push and pop thread local state used to determine if a re-entrant preparation request should
// complete immediately as a no-op (because it would lead to an infinite recursion) or should proceed
// recursively.

//static
void MethodCallGraphPreparer::BeginPrepareCerForHolder(MethodCallGraphPreparer * pPrepState)
{
    LIMITED_METHOD_CONTRACT;

    Thread * pThread = pPrepState->m_pThread;
    pPrepState->m_pNext = pThread->GetCerPreparationState();
    pThread->SetCerPreparationState(pPrepState);
}

//static
void MethodCallGraphPreparer::EndPrepareCerForHolder(MethodCallGraphPreparer * pPrepState)
{
    LIMITED_METHOD_CONTRACT;

    Thread * pThread = pPrepState->m_pThread;
    _ASSERTE(pThread && pThread->GetCerPreparationState() == pPrepState);
    pThread->SetCerPreparationState(pPrepState->m_pNext);
}


void MethodCallGraphPreparer::GetEHClauses()
{
    STANDARD_VM_CONTRACT;

    if (! ShouldGatherExplicitCERCallInfo())
    {
        return;
    }

    m_cEHClauses = 0;
    m_pEHClauses = NULL;    // we use the StackingAllocator, so don't have to delete the previous storage
    
    COR_ILMETHOD_SECT_EH const * pEH = m_pMethodDecoder->EH;
    if (pEH == NULL ||pEH->EHCount() == 0) 
    {
        return;
    }

    m_cEHClauses = pEH->EHCount();
    m_pEHClauses = new (&m_pThread->m_MarshalAlloc) EHClauseRange[m_cEHClauses];

    for (DWORD i = 0; i < m_cEHClauses; i++) 
    {
        COR_ILMETHOD_SECT_EH_CLAUSE_FAT         sEHClauseBuffer;
        const COR_ILMETHOD_SECT_EH_CLAUSE_FAT   *pEHClause;

        pEHClause = (COR_ILMETHOD_SECT_EH_CLAUSE_FAT*)pEH->EHClause(i, &sEHClauseBuffer);

        // The algorithm below assumes handlers are located after their associated try blocks. If this turns out to be a
        // false assumption we need to move to a two pass technique (or defer callsite handling in some other fashion until
        // we've scanned the IL for all calls to our preparation marker method).
        if (!(pEHClause->GetTryOffset() < pEHClause->GetHandlerOffset()))
        {
            COMPlusThrowHR(COR_E_NOTSUPPORTED, IDS_EE_NOTSUPPORTED_CATCHBEFORETRY);
        }

        m_pEHClauses[i].m_dwTryOffset = pEHClause->GetTryOffset();
        m_pEHClauses[i].m_dwHandlerOffset = pEHClause->GetHandlerOffset();
        m_pEHClauses[i].m_dwHandlerLength = pEHClause->GetHandlerLength();
        m_pEHClauses[i].m_fActive = false;

        //printf("Try: %u Handler: %u -> %u\n", pEHClause->GetTryOffset(), pEHClause->GetHandlerOffset(), pEHClause->GetHandlerOffset() + pEHClause->GetHandlerLength() - 1);
    }
 
}

void MethodCallGraphPreparer::MarkEHClauseActivatedByCERCall(MethodContext *pContext, BYTE *pbIL, DWORD cbIL)
{
    STANDARD_VM_CONTRACT;

    DWORD   dwOffset = (DWORD)(SIZE_T)((pbIL + ArgBytes_InlineTok) - (BYTE*)m_pMethodDecoder->Code);

    // Additionally we need to cope with the fact that VB and C# (for debug builds) can generate NOP instructions
    // between the PCR call and the beginning of the try block. So we're potentially looking for the
    // intersection of the try with a range of instructions. Count the number of consecutive NOP instructions
    // which follow the call.
    DWORD   dwLength = 0;
    BYTE   *pbTmpIL = pbIL + ArgBytes_InlineTok;
    while (pbTmpIL < (pbIL + cbIL) && *pbTmpIL++ == CEE_NOP)
    {
        dwLength++;
    }

    bool    fMatched = false;
    for (DWORD i = 0; i < m_cEHClauses; i++)
    {
        if (m_pEHClauses[i].m_dwTryOffset >= dwOffset &&
            m_pEHClauses[i].m_dwTryOffset <= (dwOffset + dwLength)) 
        {
            fMatched = true;
            m_pEHClauses[i].m_fActive = true;
        }
    }
    if (!fMatched) 
    {
#if defined(_DEBUG) || defined(MDA_SUPPORTED)
        DWORD dwPCROffset = (DWORD)(SIZE_T)((pbIL - 1) - (BYTE*)m_pMethodDecoder->Code);
#endif // defined(_DEBUG) || defined(MDA_SUPPORTED)
#ifdef MDA_SUPPORTED
        MDA_TRIGGER_ASSISTANT(IllegalPrepareConstrainedRegion, ReportViolation(pContext->m_pMethodDesc, dwPCROffset));
#endif
        CER_LOG(WARNINGS, ("CER: %s: Invalid call to PrepareConstrainedRegions() at IL +%04X\n",
                           pContext->m_pMethodDesc->m_pszDebugMethodName, dwPCROffset));
    }
}

bool MethodCallGraphPreparer::CheckIfCallsiteWithinCER(DWORD dwOffset)
{
    STANDARD_VM_CONTRACT;

    //printf("Found: %s at %u\n", pCallTarget->m_pMethodDesc->m_pszDebugMethodName, dwOffset);

    // Search all the EH regions we know about.
    for (DWORD i = 0; i < m_cEHClauses; i++) 
    {
        bool fCallsiteWithinCER = false;
        if (! m_pEHClauses[i].m_fActive) 
        {
            // clause not CER-active so skip it
            continue;
        }
        if (dwOffset >= (m_pEHClauses[i].m_dwHandlerOffset + m_pEHClauses[i].m_dwHandlerLength)) 
        {
            // offset beyond clause, so skip it
            continue;
        }
        if (dwOffset >= m_pEHClauses[i].m_dwTryOffset)
        {   
            // For stack probing optimization, we care if either the try or the handler has a call.  If neither
            // does, then we can optimize the probe out.
            m_fMethodHasCallsWithinExplicitCer = true;
            if (dwOffset >= m_pEHClauses[i].m_dwHandlerOffset)
            {
                fCallsiteWithinCER = true;
            }
        }
        // Only terminate if we got a positive result (i.e. the calliste is within a hardened clause).
        // We can't terminate early in the negative case because the callsite could be nested
        // in another EH region which may be hardened.
        if (fCallsiteWithinCER == true)
        {
            return true;
        }
    }
    
    return false;
}


// Iterate through the body of the method looking for interesting call sites.
void MethodCallGraphPreparer::LookForInterestingCallsites(MethodContext *pContext)
{
    STANDARD_VM_CONTRACT;

    BYTE *pbIL = (BYTE*)m_pMethodDecoder->Code;
    DWORD cbIL = m_pMethodDecoder->GetCodeSize();

    while (cbIL) {

        // Read the IL op.
        DWORD dwOp = *pbIL++; cbIL--;

        // Handle prefix codes (only CEE_PREFIX1 is legal so far).
        if (dwOp == CEE_PREFIX1) {
            if (!cbIL)
                COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
            dwOp = 256 + *pbIL++; cbIL--;
        } else if (dwOp >= CEE_PREFIX7)
            COMPlusThrowHR(COR_E_BADIMAGEFORMAT);

        // We're interested in NEWOBJ, JMP, CALL and CALLVIRT (can't trace through CALLI). We include CALLVIRT becase C#
        // routinely calls non-virtual instance methods this way in order to get this pointer null checking. We prepare NEWOBJ
        // because that covers the corner case of value types which can be constructed with no failure path.
        if (dwOp == CEE_CALL || dwOp == CEE_CALLVIRT || dwOp == CEE_NEWOBJ || dwOp == CEE_JMP) {

            if (cbIL < sizeof(DWORD))
                COMPlusThrowHR(COR_E_BADIMAGEFORMAT);

            // Decode target method into MethodDesc and new SigTypeContext.
            mdToken tkCallTarget = (mdToken)GET_UNALIGNED_VAL32(pbIL);
            MethodContext *pCallTarget = TokenToMethodDesc(pContext->m_pMethodDesc->GetModule(), tkCallTarget, &pContext->m_sTypeContext);

            // Check whether we've found a call to our own preparation marker method.
            if (pCallTarget->m_pMethodDesc == g_pPrepareConstrainedRegionsMethod) {

                if (ShouldGatherExplicitCERCallInfo()) {
                    // If we're preparing a partial method these callsites are significant (we mark which EH clauses are now
                    // 'activated' by proximity to this marker method call). Look for EH regions that are 'activated' by the call to
                    // PrepareConstrainedRegions by comparing the IL offset of the start of the try to the offset immediately after
                    // the callsite (remember to account for the rest of the CALLVIRT instruction we haven't skipped yet).
                    MarkEHClauseActivatedByCERCall(pContext, pbIL, cbIL);
                }

                // Record the fact that we found a method in the CER which is the root of a sub-CER. This is important since the
                // rude thread abort protection algorithm relies on root CER methods being marked as such.
                pContext->m_fRoot = true;
            }

            // Determine if this was really a virtual call (we discard those since we can't reliably determine the call target).
            bool fNonVirtualCall = dwOp == CEE_CALL || !pCallTarget->m_pMethodDesc->IsVirtual() || pCallTarget->m_pMethodDesc->IsFinal();

            // When we're only processing qualified catch / finally handlers then we need to compute whether this call site
            // lands in one of them.  The callsite is always within a cer if we are processing the full method.
            // If we have stackoverflow probing on, also call to determine if the CER try or finally makes any calls
            bool fCallsiteWithinCerInThisFunction = false;
            if (!m_fEntireMethod || g_pConfig->ProbeForStackOverflow()) {
                DWORD dwOffset = (DWORD)(SIZE_T)((pbIL - 1) - (BYTE*)m_pMethodDecoder->Code);
                fCallsiteWithinCerInThisFunction = CheckIfCallsiteWithinCER(dwOffset);
            }
            bool fCallsiteWithinCer = m_fEntireMethod || fCallsiteWithinCerInThisFunction;

            // Check for the presence of some sort of reliability contract (on the method, class or assembly). This will
            // determine whether we log an error, ignore the method or treat it as part of the prepared call graph.
            ReliabilityContractLevel eLevel = RCL_UNKNOWN;
            if (fNonVirtualCall &&                          // Ignore virtual calls
                fCallsiteWithinCer &&                       // And calls outside CERs
                !m_sAlreadySeen.IsInStack(pCallTarget) &&     // And methods we've seen before
                !m_sLeftToProcess.IsInStack(pCallTarget) &&   // And methods we've already queued for processing
                (eLevel = CheckForReliabilityContract(pCallTarget->m_pMethodDesc)) >= RCL_PREPARE_CONTRACT) // And unreliable methods
                m_sLeftToProcess.Push(pCallTarget);           // Otherwise add this method to the list to process
            else if (fCallsiteWithinCer) { 
#if defined(_DEBUG) || defined(MDA_SUPPORTED)
                DWORD dwOffset = (DWORD)(SIZE_T)((pbIL - 1) - (BYTE*)m_pMethodDecoder->Code);
#endif // defined(_DEBUG) || defined(MDA_SUPPORTED)
                if (eLevel == RCL_NO_CONTRACT) {
                    // Method was sufficiently unreliable for us to warn interested users that something may be amiss. Do this
                    // through MDA logging.
#ifdef MDA_SUPPORTED
                    MDA_TRIGGER_ASSISTANT(InvalidCERCall, ReportViolation(pContext->m_pMethodDesc, pCallTarget->m_pMethodDesc, dwOffset));
#endif
                    CER_LOG(WARNINGS, ("CER: %s +%04X -> %s: weak contract\n", pContext->ToString(), dwOffset, pCallTarget->ToString()));
                } else if (!fNonVirtualCall && !m_fIgnoreVirtualCERCallMDA) {
                    // Warn users about virtual calls in CERs (so they can go back and consider which target methods need to be
                    // prepared ahead of time).
#ifdef MDA_SUPPORTED
                    MDA_TRIGGER_ASSISTANT(VirtualCERCall, ReportViolation(pContext->m_pMethodDesc, pCallTarget->m_pMethodDesc, dwOffset));
#endif
                    CER_LOG(WARNINGS, ("CER: %s +%04X -> %s: virtual call\n", pContext->ToString(), dwOffset, pCallTarget->ToString()));
                }
            }
        }

        // Skip the rest of the current IL instruction. Look up the table built statically at the top of this module for most
        // instructions, but CEE_SWITCH requires special processing (the length of that instruction depends on a count DWORD
        // embedded right after the opcode).
        if (dwOp == CEE_SWITCH) {
            DWORD dwTargets = GET_UNALIGNED_VAL32(pbIL);
            if (dwTargets >= (MAXDWORD / sizeof(DWORD)))
                COMPlusThrowHR(COR_E_BADIMAGEFORMAT); // multiplication below would overflow
            DWORD cbSwitch = (1 + dwTargets) * sizeof(DWORD);
            if (cbIL < cbSwitch)
                COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
            pbIL += cbSwitch;
            cbIL -= cbSwitch;
        } else {
            if (dwOp >= _countof(g_rOpArgs))
                COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
            DWORD cbOp = g_rOpArgs[dwOp];
            if (cbIL < cbOp)
                COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
            pbIL += cbOp;
            cbIL -= cbOp;
        }

    } // End of IL parsing loop
}

void MethodCallGraphPreparer::PrepareMethods()
{
    STANDARD_VM_CONTRACT;

#ifdef _DEBUG
    DWORD dwCount = 0;
    if (GetCerLoggingOptions())
    {
        CER_LOG(PREPARE, ("---------------------------------------------------------------\n"));
        SString ssMethod;
        TypeString::AppendMethodInternal(ssMethod, m_pRootMD, TypeString::FormatNamespace | TypeString::FormatStubInfo);
        CER_LOG(PREPARE, ("Preparing from %S\n", ssMethod.GetUnicode()));
    }
#endif

    MethodContext      *pContext;               // The current MethodContext we're processing

    while ((pContext = m_sAlreadySeen.Pop()) != NULL) {
        MethodDesc *pMD = pContext->m_pMethodDesc;

#ifndef CROSSGEN_COMPILE
        // Jitting. Don't need to do this for the ngen case.
#ifdef FEATURE_NATIVE_IMAGE_GENERATION
        if (!m_fNgen)
#endif
        {
            // Also skip the jit for the root method in the activated from jit case (where this would result in a recursive
            // jit). We'd cope with this just fine, the main reason for this logic is to avoid unbalancing some profiler event
            // counts that upset some of our test cases. This is safe in the face of multiple instantiations of the same method
            // because in the jit activated case (where we're told the root type context is not exact) we early exit if the root
            // method desc isn't 'unique' (i.e. independent of the type context).
            if (m_fExactTypeContext || pMD != m_pRootMD) {

                // Jit the method we traced.
                if (pMD->IsPointingToPrestub())
                {
                    pMD->EnsureActive();
                    pMD->DoPrestub(NULL);
                }

                // If we traced an instantiating stub we need to jit the wrapped (real) method as well.
                if (pMD->IsInstantiatingStub()) {
                    _ASSERTE(!pMD->ContainsGenericVariables());
                    MethodDesc *pRealMD = pMD->GetWrappedMethodDesc();
                    if (pRealMD->IsPointingToPrestub())
                    {
                        pMD->EnsureActive();
                        pRealMD->DoPrestub(NULL);
                    }
                }
            }

            // Remember sub-CER root methods for further processing in RecordResults. We need to build CerPrepInfo structures for
            // these just the same as top-level CERs since we may wander in to them by a route that doesn't include the top-level CER
            // and the thread abort deflection algorithm relies on each CER root method being marked by a CerPrepInfo. Defer this
            // processing to RecordResults since we aren't guaranteed to have prepared all the methods of the sub-graph at this
            // point.
            if (pContext->m_fRoot && pMD != m_pRootMD)
                m_sPersist.Push(pContext);
        }
#endif // CROSSGEN_COMPILE

        // Prepare generic dictionaries (both class and method as needed). We do this even in the ngen scenario, trying to get
        // as many slots filled as possible. By the looks of it, it's possible that not all of these entries will make it across
        // to runtime (the fixup code seems to give up on some of the more complex entries, not sure of the details). But we'll
        // do as best we can here to hopefully minimize any real work on the other side.

        // Don't use the direct PrepopulateDictionary method on MethodTable here, it takes binding considerations into account
        // (which we don't care about).
        DictionaryLayout *pClassDictLayout = pMD->GetClass()->GetDictionaryLayout();
        if (pClassDictLayout) {
            // Translate the representative method table we can find from our method desc into an exact instantiation using the
            // type context we have.
            MethodTable *pMT = TypeHandle(pMD->GetMethodTable()).Instantiate(pContext->m_sTypeContext.m_classInst).AsMethodTable();

            pMT->GetDictionary()->PrepopulateDictionary(NULL, pMT, false);

            // The dictionary may have overflowed in which case we need to prepopulate the jit's lookup cache as well.
            PrepopulateGenericHandleCache(pClassDictLayout, NULL, pMT);
        }

        // Don't use the direct PrepopulateDictionary method on MethodDesc here, it appears to use a representative class
        // instantiation (and we have the exact one handy).
        DictionaryLayout *pMethDictLayout = pMD->GetDictionaryLayout();
        if (pMethDictLayout) {
            pMD->GetMethodDictionary()->PrepopulateDictionary(pMD, NULL, false);

            // The dictionary may have overflowed in which case we need to prepopulate the jit's lookup cache as well.
            PrepopulateGenericHandleCache(pMethDictLayout, pMD, NULL);
        }

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
        // Keep some of the method contexts around for the ngen case (the ones that might still need fixup at runtime). We'll
        // write them into a persisted data structure in the next step.
        // @todo: We use a horrible workaround here to get round the fact that while ngen'ing mscorlib we may prepare some of its
        // methods before we've had a chance to start up the compilation domain (mscorlib code is shared and used by the ngen
        // process itself). So we can't blindly call NeedsRestore() on an mscorlib method since that code asserts we're in the
        // compilation domain. Instead, if we're in the ngen process and we're outside the compilation domain we're going to
        // assume that the method doesn't need restoration. This only affects a handful of methods (six at last count, all to do
        // with security safe handles or some CERs in remoting).
        if (m_fNgen) {
            if (GetAppDomain() == NULL ||
                !GetAppDomain()->IsCompilationDomain() ||
                !(GetAppDomain()->ToCompilationDomain()->canCallNeedsRestore()) ||
                !(GetAppDomain()->ToCompilationDomain()->GetTargetImage()->CanPrerestoreEagerBindToMethodDesc(pMD, NULL))||
                pMD->HasClassOrMethodInstantiation() ||
                pMD->IsNDirect() ||
                pMD->IsComPlusCall() ||
                pMD->IsFCall() ||
                pContext->m_fRoot)
                m_sPersist.Push(pContext);
        }
#endif

#ifdef _DEBUG
        CER_LOG(PREPARE, ("  %s\n", pContext->ToString()));
        dwCount++;
#endif
    }

#ifdef _DEBUG
    CER_LOG(PREPARE, ("Prepared a total of %u methods\n", dwCount));
#ifdef FEATURE_NATIVE_IMAGE_GENERATION
    if (m_fNgen)
        CER_LOG(PREPARE, ("Saved data for %u of them in the ngen image\n", m_sPersist.GetCount()));
#endif
    CER_LOG(PREPARE, ("---------------------------------------------------------------\n"));
#endif
}

// Common code used in creating/looking up a CerPrepInfo and initializing/updating it.
void InitPrepInfo(MethodDesc *pMD, SigTypeContext *pTypeContext, bool fMethodHasCallsWithinExplicitCer)
{
    CONTRACTL {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pMD));
    } CONTRACTL_END;

    // Lookup or allocate the CerPrepInfo.
    CerPrepInfo *pInfo = pMD->GetModule()->CreateCerPrepInfo(pMD);

    pInfo->m_fMethodHasCallsWithinExplicitCer = fMethodHasCallsWithinExplicitCer;

    // Work out if this was a partial preparation.
    bool fPartialPreparation = pMD->IsSharedByGenericInstantiations() ||
                               pMD->IsInstantiatingStub() ||
                               pMD->ContainsGenericVariables();

    // Simple case first: if this isn't a partial preparation (no pesky unbound type vars to worry about), then the method is
    // now fully prepared.
    if (!fPartialPreparation) {
        pInfo->m_fFullyPrepared = true;
        return;
    }

    // Else we know we require per-instantiation initialization. We need to update a hash table to record the preparation we did
    // in this case, and that requires taking a mutex. We could check that nobody beat us to it first, but that will hardly ever
    // happen, so it's not really worth it. So just acquire the mutex right away.
    CrstHolder sHolder(pMD->GetModule()->GetCerCrst());

    pInfo->m_fRequiresInstantiation = true;

    // Add an entry to a hash that records which instantiations we've prep'd for (again, only if someone hasn't beaten us).
    HashDatum sDatum;
    if (!pInfo->m_sIsInitAtInstHash.GetValue(pTypeContext, &sDatum))
    {
        pInfo->m_sIsInitAtInstHash.InsertKeyAsValue(pTypeContext);
    }
}

bool  MethodCallGraphPreparer::RecordResults()
{
    STANDARD_VM_CONTRACT;

    // Preparation has been successful, record what we've done in a manner consistent with whether we're ngen'ing or running for
    // real.

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
    // If we're ngen'ing an image we save our progess as a list of method contexts that might need restoration at runtime (since
    // even with prejitting there are some things that need to be prepared at runtime). This list goes into a per module table (the
    // module in question being that of the root method in the CER).
    if (m_fNgen) {

        // We have the list of MethodContexts ready, but they're in cheap storage that will go away once we exit this method.
        // Not only do we have to copy them to heap memory, but we also know exactly how many there are. So we can allocate a
        // single array with a more compact form of MethodContext for each element. We allocate an extra sentinel value for the end
        // of the list. This means we can store just a pointer to the list without a count (the code that accesses this list cares
        // about keeping the list heads compact and densely packed and doesn't care about counting the elements in the list).
        DWORD cContexts = m_sPersist.GetCount();
        LoaderHeap *pHeap = m_pRootMD->GetAssembly()->GetLowFrequencyHeap();
        MethodContextElement *pContexts = (MethodContextElement*)(void*)pHeap->AllocMem(S_SIZE_T(sizeof(MethodContextElement)) * (S_SIZE_T(cContexts) + S_SIZE_T(1)));
        DWORD i = 0;

        MethodContext      *pContext;               // The current MethodContext we're processing
        while ((pContext = m_sPersist.Pop()) != NULL) {
            pContexts[i].m_pMethodDesc.SetValue(pContext->m_pMethodDesc);

            MethodTable * pExactMT = NULL;
            if (!pContext->m_sTypeContext.m_classInst.IsEmpty())
            {
                pExactMT = TypeHandle(pContext->m_pMethodDesc->GetMethodTable()).Instantiate(pContext->m_sTypeContext.m_classInst).AsMethodTable();
                _ASSERTE(pExactMT->HasInstantiation());
            }
            else
            {
                _ASSERTE(!pContext->m_pMethodDesc->GetMethodTable()->HasInstantiation());
            }
            pContexts[i].m_pExactMT.SetValue(pExactMT);

            i++;

            // Keep the context round for a little longer if the method in question was the root of a sub-CER.
            if (pContext->m_fRoot)
                m_sRootMethods.Push(pContext);
        }

        // Write sentinel entry terminating list.
        _ASSERTE(i == cContexts);

        // Add list representing this CER to the per-module table (keyed by root method desc).
        m_pRootMD->GetModule()->AddCerListToRootTable(m_pRootMD, pContexts);

        // If this did have an call from an explicit PCER range, create a PrepInfo for it so that we can
        // quickly grab that information later when we jit that method.  This allows us to optimize the probe
        // away if there are no calls from the PCER range.  This is an issue when we've prepared a method
        // as part of a CER call from another method, but haven't ngened that method yet.  When we get
        // around to finally ngening that method, we want to be able to optimize the CER probe out if
        // we can, but don't want to reprepare the method.
        if (g_pConfig->ProbeForStackOverflow() && m_fMethodHasCallsWithinExplicitCer)
        {
            if (m_pCerPrepInfo == NULL)
                m_pCerPrepInfo = m_pRootMD->GetModule()->CreateCerPrepInfo(m_pRootMD);
            m_pCerPrepInfo->m_fMethodHasCallsWithinExplicitCer = TRUE;
        }


        // We need to be careful with sub-CERs in the ngen case. In the jit case they're dealt with automatically (preparing a
        // super-CER always completely prepares a sub-CER). But in the ngen case we potentially need to run further preparation
        // steps at the point that a CER root is executed for the first time. If the sub-root is encountered before the super-root
        // then the sub-CER won't have been prepared correctly.
        // We solve this simply by recursively running this routine over the methods we noted were sub-roots earlier (this list
        // doesn't include the main root). We could potentially do a little better than this given that we've calculated the
        // super-graph, but this is complicated somewhat by the fact that we don't retain the graph structure (i.e. we can't extract
        // sub-graphs easily) and the effort seems wasted just to avoid a little CPU time and stack space just for the ngen creation
        // scenario.
        while ((pContext = m_sRootMethods.Pop()) != NULL)
        {
            MethodCallGraphPreparer mgcp(pContext->m_pMethodDesc, &pContext->m_sTypeContext, false, false);
            mgcp.Run();
        }

        return m_fMethodHasCallsWithinExplicitCer;
    }
#endif // FEATURE_NATIVE_IMAGE_GENERATION

    // This is the runtime (non-ngen case). Record our progress in an info structure placed in a hash table hung off the module
    // which owns the root method desc in the CER. The methods which create this info structure handle race conditions (to
    // ensure we don't leak memory or data), but the updates to the info structure itself might not require any serialization
    // (the values are 'latched' -- recomputation should yield the same result). The exception is any update to a more complex
    // data fields (lists and hash tables) that require serialization to prevent corruption of the basic data structure.

    // Process sub-CER roots first. We need to build CerPrepInfo structures for these just as same as top-level CERs since we may
    // wander in to them by a route that doesn't include the top-level CER and the thread abort deflection algorithm relies on each
    // CER root method being marked by a CerPrepInfo.
    MethodContext *pContext;
    while ((pContext = m_sPersist.Pop()) != NULL) {
        _ASSERTE(pContext->m_fRoot);

        // @todo: need to flow fMethodHasCallsWithinExplicitCer information through method contexts. For now just make a
        // conservative, safe choice.
        InitPrepInfo(pContext->m_pMethodDesc, &pContext->m_sTypeContext, true);
    }

    // Now process the top-level CER.
    InitPrepInfo(m_pRootMD, m_pRootTypeContext, m_fMethodHasCallsWithinExplicitCer);

    return m_fMethodHasCallsWithinExplicitCer;
}

// Determines whether the given method contains a CER root that can be pre-prepared (i.e. prepared at jit time).
bool ContainsPrePreparableCerRoot(MethodDesc *pMD)
{
    CONTRACTL {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pMD));
    } CONTRACTL_END;

    // Deal with exotic cases (non-IL methods and the like).
    if (!pMD->IsIL() || pMD->IsAbstract())
        return false;

    // And cases where we can't jit prepare (because the code is shared between instantiations).
    if (pMD->IsSharedByGenericInstantiations() || pMD->IsInstantiatingStub() || pMD->ContainsGenericVariables())
        return false;

    // Otherwise we have a trickier calculation. We don't want to force the jit of the method at this point (may cause infinite
    // recursion problems when we're called from the jit in the presence of call cycles). Instead we walk the top-level of the
    // method IL using the same algorithm as PrepareMethodCallGraph.

    // Locate the IL body of the current method. May have to account for the fact that the current method desc is an
    // instantiating stub and burrow down for the real method desc.
    MethodDesc *pRealMethod = pMD;
    if (pRealMethod->IsInstantiatingStub()) {
        _ASSERTE(!pRealMethod->ContainsGenericVariables());
        pRealMethod = pRealMethod->GetWrappedMethodDesc();
    }
    COR_ILMETHOD_DECODER method(pRealMethod->GetILHeader());
    BYTE *pbIL = (BYTE*)method.Code;
    DWORD cbIL = method.GetCodeSize();

    // Look for exception handling information for the method. If there isn't any then we know there can't be a CER rooted here.
    COR_ILMETHOD_SECT_EH const * pEH = method.EH;
    if (pEH == NULL || pEH->EHCount() == 0)
        return false;

    // Iterate through the body of the method looking for interesting call sites.
    while (cbIL) {

        // Read the IL op.
        DWORD dwOp = *pbIL++; cbIL--;

        // Handle prefix codes (only CEE_PREFIX1 is legal so far).
        if (dwOp == CEE_PREFIX1) {
            if (!cbIL)
                COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
            dwOp = 256 + *pbIL++; cbIL--;
            if (dwOp >= CEE_ILLEGAL)
                COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
        } else if (dwOp >= CEE_PREFIX7)
            COMPlusThrowHR(COR_E_BADIMAGEFORMAT);

        // We'll only ever see CALL instructions targeting PrepareConstrainedRegions (well those are the ones we're interested in
        // anyway).
        if (dwOp == CEE_CALL)
        {
            if (cbIL < sizeof(DWORD))
                COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
            if (IsPcrReference(pMD->GetModule(), (mdToken)GET_UNALIGNED_VAL32(pbIL)))
                return true;
        }

        // Skip the rest of the current IL instruction. Look up the table built statically at the top of this module for most
        // instructions, but CEE_SWITCH requires special processing (the length of that instruction depends on a count DWORD
        // embedded right after the opcode).
        if (dwOp == CEE_SWITCH) {
            if (cbIL < sizeof(DWORD))
                COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
            DWORD dwTargets = GET_UNALIGNED_VAL32(pbIL);
            if (dwTargets >= (MAXDWORD / sizeof(DWORD)))
                COMPlusThrowHR(COR_E_BADIMAGEFORMAT); // multiplication below would overflow
            DWORD cbSwitch = (1 + dwTargets) * sizeof(DWORD);
            if (cbIL < cbSwitch)
                COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
            pbIL += cbSwitch;
            cbIL -= cbSwitch;
        } else {
            if (dwOp >= _countof(g_rOpArgs))
                COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
            DWORD cbOp = g_rOpArgs[dwOp];
            if (cbIL < cbOp)
                COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
            pbIL += cbOp;
            cbIL -= cbOp;
        }

    } // End of IL parsing loop

    // If we get here then there was no CER-root.
    return false;
}

// The name of the PrepareConstrainedRegions method, broken down into its components (the code below scans for these directly in the
// metadata).
#define PCR_METHOD      "PrepareConstrainedRegions"
#define PCR_TYPE        "RuntimeHelpers"
#define PCR_NAMESPACE   "System.Runtime.CompilerServices"

// Given a token and a module scoping it, determine if that token is a reference to PrepareConstrainedRegions. We want to do this
// without loading any random types since we're called in a context where type loading is prohibited.
bool IsPcrReference(Module *pModule, mdToken tkMethod)
{
    CONTRACTL {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pModule));
    } CONTRACTL_END;

    IMDInternalImport *pImport = pModule->GetMDImport();

    // Validate that the token is one that we can handle.
    if (!pImport->IsValidToken(tkMethod) || (TypeFromToken(tkMethod) != mdtMethodDef &&
                                             TypeFromToken(tkMethod) != mdtMethodSpec &&
                                             TypeFromToken(tkMethod) != mdtMemberRef))
        COMPlusThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_METHOD_TOKEN);

    // No reason to see a method spec for a call to something as simple as PrepareConstrainedRegions.
    if (TypeFromToken(tkMethod) == mdtMethodSpec)
        return false;

    // If it's a method def then the module had better be mscorlib.
    if (TypeFromToken(tkMethod) == mdtMethodDef) {
        if (pModule->GetAssembly()->GetManifestModule() == SystemDomain::SystemAssembly()->GetManifestModule())
            return tkMethod == g_pPrepareConstrainedRegionsMethod->GetMemberDef();
        else
            return false;
    }

    // That leaves the cross module reference case.
    _ASSERTE(TypeFromToken(tkMethod) == mdtMemberRef);

    // First get the method name and signature and validate it.
    PCCOR_SIGNATURE pSig;
    DWORD cbSig;
    LPCSTR szMethod;
    IfFailThrow(pImport->GetNameAndSigOfMemberRef(tkMethod, &pSig, &cbSig, &szMethod));
    
    {
        SigParser sig(pSig, cbSig);
        ULONG nCallingConvention;
        ULONG nArgumentsCount;
        BYTE  bReturnType;
        
        // Signature is easy: void PCR().
        // Must be a static method signature.
        if (FAILED(sig.GetCallingConvInfo(&nCallingConvention)))
            return false;
        if (nCallingConvention != IMAGE_CEE_CS_CALLCONV_DEFAULT)
            return false;
        // With no arguments.
        if (FAILED(sig.GetData(&nArgumentsCount)))
            return false;
        if (nArgumentsCount != 0)
            return false;
        // And a void return type.
        if (FAILED(sig.GetByte(&bReturnType)))
            return false;
        if (bReturnType != (BYTE)ELEMENT_TYPE_VOID)
            return false;
    }

    // Validate the name.
    if (strcmp(szMethod, PCR_METHOD) != 0)
        return false;

    // The method looks OK, move up to the type and validate that.
    mdToken tkType;
    IfFailThrow(pImport->GetParentOfMemberRef(tkMethod, &tkType));
    
    if (!pImport->IsValidToken(tkType))
        COMPlusThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_TOKEN);
    
    // If the parent is not a type ref then this isn't our target (we assume that mscorlib never uses a member ref to target
    // PrepareConstrainedRegions, check that with the assert below, if it ever fails we need to add some additional logic below).
    _ASSERTE(TypeFromToken(tkType) != mdtTypeDef ||
             pModule->GetAssembly()->GetManifestModule() != SystemDomain::SystemAssembly()->GetManifestModule());
    if (TypeFromToken(tkType) != mdtTypeRef)
        return false;

    // Get the type name and validate it.
    LPCSTR szNamespace;
    LPCSTR szType;
    IfFailThrow(pImport->GetNameOfTypeRef(tkType, &szNamespace, &szType));
    
    if (strcmp(szType, PCR_TYPE) != 0)
        return false;
    if (strcmp(szNamespace, PCR_NAMESPACE) != 0)
        return false;
    
    // Type is OK as well. Check the assembly reference.
    mdToken tkScope;
    IfFailThrow(pImport->GetResolutionScopeOfTypeRef(tkType, &tkScope));
    
    if (TypeFromToken(tkScope) != mdtAssemblyRef)
        return false;
    if (!pImport->IsValidToken(tkScope))
    {
        COMPlusThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_TOKEN);
    }
    
    // Fetch the name and public key or public key token.
    BYTE *pbPublicKeyOrToken;
    DWORD cbPublicKeyOrToken;
    LPCSTR szAssembly;
    DWORD dwAssemblyFlags;
    IfFailThrow(pImport->GetAssemblyRefProps(
        tkScope, 
        (const void**)&pbPublicKeyOrToken, 
        &cbPublicKeyOrToken, 
        &szAssembly, 
        NULL,   // AssemblyMetaDataInternal: we don't care about version, culture etc.
        NULL,   // Hash value pointer, obsolete information
        NULL,   // Byte count for above
        &dwAssemblyFlags));
    
    // Validate the name.
    if (stricmpUTF8(szAssembly, g_psBaseLibraryName) != 0)
        return false;
    
    // And the public key or token, which ever was burned into the reference by the compiler. For mscorlib this is the ECMA key or
    // token.
    if (IsAfPublicKeyToken(dwAssemblyFlags)) {
        if (cbPublicKeyOrToken != sizeof(g_rbNeutralPublicKeyToken) ||
            memcmp(pbPublicKeyOrToken, g_rbNeutralPublicKeyToken, cbPublicKeyOrToken) != 0)
            return false;
    } else {
        if (cbPublicKeyOrToken != sizeof(g_rbNeutralPublicKey) ||
            memcmp(pbPublicKeyOrToken, g_rbNeutralPublicKey, cbPublicKeyOrToken) != 0)
            return false;
    }

    // If we get here we've finally proved the call target was indeed PrepareConstrainedRegions. Whew.
    return true;
}

// Prepares a method as a CER root. In some scenarios we set
// fIgnoreVirtualCERCallMDA=TRUE, this happens when we want to ignore firing a
// VirtualCERCall MDA because we know for sure that the virtual methods are
// already prepared. A good example of this case is preparing
// g_pExecuteBackoutCodeHelperMethod method. 
void PrepareMethodDesc(MethodDesc* pMD, Instantiation classInst, Instantiation methodInst, BOOL onlyContractedMethod, BOOL fIgnoreVirtualCERCallMDA)
{
     CONTRACTL 
     {
        THROWS;
        DISABLED(GC_TRIGGERS);
        MODE_ANY;
    }
    CONTRACTL_END;

    GCX_PREEMP();

#ifdef FEATURE_PREJIT
    // This method may have some ngen fixup information provided, in which case we just check that it's been restored and can
    // dispense with the preparation altogether.
    Module *pModule = pMD->GetModule();
    if (pModule->IsNgenCerRootMethod(pMD))
    {
        pMD->CheckRestore();
        pModule->RestoreCer(pMD);
        return;
    }
#endif

    // If we are only going to prepare contracted methods and this method does
    // not have a contract then we just return.
    if (onlyContractedMethod && CheckForReliabilityContract(pMD) < RCL_BASIC_CONTRACT)
    {
        return;
    }

    SigTypeContext sTypeContext(pMD, classInst, methodInst);
    MethodCallGraphPreparer mcgp(pMD, &sTypeContext, true, true, fIgnoreVirtualCERCallMDA == TRUE);
    mcgp.Run();
}

// Prepares the critical finalizer call graph for the given object type (which
// must derive from CriticalFinalizerObject). This involves preparing at least
// the finalizer method and possibly some others (for SafeHandle and
// CriticalHandle derivations). If a module pointer is supplied then only the
// critical methods introduced in that module are prepared (this is used at
// ngen time to ensure that we're only generating ngen preparation info for the
// targetted module). 
void PrepareCriticalFinalizerObject(MethodTable *pMT, Module *pModule)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMT));
    } CONTRACTL_END;

    // Have we prepared this type before?
    if (pMT->CriticalTypeHasBeenPrepared())
        return;

    GCX_PREEMP();

    // Restore the method table if necessary.
    pMT->CheckRestore();

    // Determine the interesting parent class (either SafeHandle, CriticalHandle or CriticalFinalizerObject).
    MethodTable *pSafeHandleClass = MscorlibBinder::GetClass(CLASS__SAFE_HANDLE);
    MethodTable *pCriticalHandleClass = MscorlibBinder::GetClass(CLASS__CRITICAL_HANDLE);
    MethodTable *pParent = pMT;
    while (pParent) {
        if (pParent == g_pCriticalFinalizerObjectClass ||
            pParent == pSafeHandleClass ||
            pParent == pCriticalHandleClass) {
            break;
        }
        pParent = pParent->GetParentMethodTable();
    }
    _ASSERTE(pParent != NULL);

    BinderMethodID rgMethods[5];
    int nMethods;

    // Prepare the method or methods based on the parent class.
    if (pParent == pSafeHandleClass) {
        rgMethods[0] = METHOD__CRITICAL_FINALIZER_OBJECT__FINALIZE;
        rgMethods[1] = METHOD__SAFE_HANDLE__RELEASE_HANDLE;
        rgMethods[2] = METHOD__SAFE_HANDLE__GET_IS_INVALID;
        rgMethods[3] = METHOD__SAFE_HANDLE__DISPOSE;
        rgMethods[4] = METHOD__SAFE_HANDLE__DISPOSE_BOOL;
        nMethods = 5;
    } else if (pParent == pCriticalHandleClass) {
        rgMethods[0] = METHOD__CRITICAL_FINALIZER_OBJECT__FINALIZE;
        rgMethods[1] = METHOD__CRITICAL_HANDLE__RELEASE_HANDLE;
        rgMethods[2] = METHOD__CRITICAL_HANDLE__GET_IS_INVALID;
        rgMethods[3] = METHOD__CRITICAL_HANDLE__DISPOSE;
        rgMethods[4] = METHOD__CRITICAL_HANDLE__DISPOSE_BOOL;
        nMethods = 5;
    } else {
        _ASSERTE(pParent == g_pCriticalFinalizerObjectClass);
        rgMethods[0] = METHOD__CRITICAL_FINALIZER_OBJECT__FINALIZE;
        nMethods = 1;
    }

    for (int iMethod = 0; iMethod < nMethods; iMethod++)
    {
        // Prepare a (possibly virtual) method on an instance. The method is identified via a binder ID, so the initial
        // declaration of the method must reside within mscorlib. We might have ngen fixup information for the method and can avoid direct
        // preparation as well.

        MethodDesc *pPrepMethod = pMT->GetMethodDescForSlot(MscorlibBinder::GetMethod(rgMethods[iMethod])->GetSlot());
#ifdef FEATURE_PREJIT
        if (pPrepMethod->GetModule()->IsNgenCerRootMethod(pPrepMethod)) {
            pPrepMethod->GetModule()->RestoreCer(pPrepMethod);
        }
        else
        if (IsCompilationProcess() && pPrepMethod->IsAbstract()) {
            // Skip abstract methods during NGen (we should not ever get abstract methods here at runtime)
        }
        else
#endif
        {
            if (pModule == NULL || pPrepMethod->GetModule() == pModule) {
                SigTypeContext _sTypeContext(pPrepMethod, TypeHandle(pMT));
                MethodCallGraphPreparer mcgp(pPrepMethod, &_sTypeContext, true, true);
                mcgp.Run();
            }
        }
    }

    // Note the fact that we've prepared this type before to prevent repetition of the work above. (Though repetition is harmless in
    // all other respects, so there's no need to worry about the race setting this flag).
    pMT->SetCriticalTypeHasBeenPrepared();
}

#ifdef _DEBUG

static const char * const g_rszContractNames[] = { "RCL_NO_CONTRACT", "RCL_BASIC_CONTRACT", "RCL_PREPARE_CONTRACT" };
static DWORD g_dwContractChecks = 0;

#define ReturnContractLevel(_level) do {                                                        \
    g_dwContractChecks++;                                                                       \
    if ((g_dwContractChecks % 100) == 0 && g_pMethodContractCache)                              \
        g_pMethodContractCache->DbgDumpStats();                                                 \
    ReliabilityContractLevel __level = (_level);                                                \
    CER_LOG(CONTRACTS, ("%s -- %s\n", pMD->m_pszDebugMethodName, g_rszContractNames[__level])); \
    return __level;                                                                             \
} while (false)
#else
#define ReturnContractLevel(_level) return (_level)
#endif

// Look for reliability contracts at the method, class and assembly level and parse them to extract the information we're interested
// in from a runtime preparation viewpoint. This information is abstracted in the form of the ReliabilityContractLevel enumeration.
ReliabilityContractLevel CheckForReliabilityContract(MethodDesc *pMD)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMD));
    } CONTRACTL_END;

    // We are attempting to abstract reliability contracts for the given method into three different buckets: those methods that
    // will cause an error (or a MDA report at least) during preparation (RCL_NO_CONTRACT), those we allow but don't prepare
    // (RCL_BASIC_CONTRACT) and those we allow and prepare (RCL_PREPARE_CONTRACT).
    //
    // We place methods into the first bucket below that matches:
    //  RCL_NO_CONTRACT      --  Methods with no consistency or whose consistency states they may corrupt the appdomain or process.
    //  RCL_BASIC_CONTRACT   --  Methods that state CER.None (or don't specify a CER attribute)
    //  RCL_PREPARE_CONTRACT --  Methods that state CER.MayFail or CER.Success
    //
    // We look for reliability contracts at three levels: method, class and assembly. Definitions found at the method level override
    // those at the class and assembly level and those at the class level override assembly settings.
    //
    // In the interests of efficiency we cache contract information in a number of ways. Firstly we look at a hash of recently
    // queried MethodDescs. This contains authoritative answers (assembly/class/method information has already been composed so on a
    // hit we don't need to look anywhere else). This cache is allocated lazily, never grows (newer items eventually displace older
    // ones), is global, requires no locks and is never freed. The idea is to limit the amount of working set we ever occupy while
    // keeping the CPU usage as low as possible. Typical usages of this method involve querying a small number of methods in a stack
    // walk, possibly multiple times, so a small hash cache should work reasonably well here.
    //
    // On a miss we're going to have to bite the bullet and look at the assembly, class and method. The assembly and class cache
    // this information at load (ngen) time though, so they're not so expensive (class level data is cached on the EEClass, so it's
    // cold data, but the most performance sensitive scenario in which we're called here, ThreadAbort, isn't all that hot).

    // Check the cache first, it contains a raw contract level.
    ReliabilityContractLevel eLevel;
    if (g_pMethodContractCache && g_pMethodContractCache->Lookup(pMD, (DWORD*)&eLevel))
        ReturnContractLevel(eLevel);

    // Start at the method level and work up until we've found enough information to make a decision. The contract level is composed
    // in an encoded DWORD form that lets us track both parts of the state (consistency and cer) and whether each has been supplied
    // yet. See the RC_* macros for encoding details.
    DWORD dwMethodContractInfo = GetReliabilityContract(pMD->GetMDImport(), pMD->GetMemberDef());
    if (RC_INCOMPLETE(dwMethodContractInfo)) {
        DWORD dwClassContractInfo = pMD->GetClass()->GetReliabilityContract();
        dwMethodContractInfo = RC_MERGE(dwMethodContractInfo, dwClassContractInfo);
        if (RC_INCOMPLETE(dwMethodContractInfo)) {
            DWORD dwAssemblyContractInfo = pMD->GetModule()->GetReliabilityContract();
            dwMethodContractInfo = RC_MERGE(dwMethodContractInfo, dwAssemblyContractInfo);
        }
    }

    // We've got an answer, so attempt to cache it for the next time.

    // First check we have a cache (we allocate it lazily).
    if (g_pMethodContractCache == NULL) {
        PtrHashCache *pCache = new (nothrow) PtrHashCache();
        if (pCache)
            if (FastInterlockCompareExchangePointer(&g_pMethodContractCache, pCache, NULL) != NULL)
                delete pCache;
    }

    // We still might not have a cache in low memory situations. That's OK.
    if (g_pMethodContractCache)
        g_pMethodContractCache->Add(pMD, RC_ENCODED_TO_LEVEL(dwMethodContractInfo));

    ReturnContractLevel(RC_ENCODED_TO_LEVEL(dwMethodContractInfo));
}


// Macro used to handle failures in the routine below.
#define IfFailRetRcNull(_hr) do { if (FAILED(_hr)) return RC_NULL; } while (false)

// Look for a reliability contract attached to the given metadata token in the given scope. Return the result as an encoded DWORD
// (see the RC_ENCODE macro).
DWORD GetReliabilityContract(IMDInternalImport *pImport, mdToken tkParent)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pImport));
    } CONTRACTL_END;

    HRESULT hr;
    DWORD   dwResult = RC_NULL;

    // Sadly we only have two unmanaged APIs available to us for looking at custom attributes. One looks up attributes by name but
    // only returns the byte blob, not the attribute ctor information (which we need to parse the blob) while the other returns
    // everything but requires us to enumerate all attributes on a given token looking for the one we're interested in. To keep the
    // cost down we probe for the existence of the attribute using the first API and then use the enumeration method if we get a
    // hit.
    hr = pImport->GetCustomAttributeByName(tkParent, RELIABILITY_CONTRACT_NAME, NULL, NULL);
    if (hr == S_FALSE)
        return RC_NULL;

    IfFailRetRcNull(hr);

    // Got at least one contract against this parent. Enumerate them all (filtering by name).
    MDEnumHolder hEnum(pImport);
    hr = pImport->SafeAndSlowEnumCustomAttributeByNameInit(tkParent, RELIABILITY_CONTRACT_NAME, &hEnum);
    _ASSERTE(hr != S_FALSE);
    IfFailRetRcNull(hr);

    // Enumerate over all the contracts.
    mdToken tkContract;
    while (S_OK == pImport->SafeAndSlowEnumCustomAttributeByNameNext(tkParent, RELIABILITY_CONTRACT_NAME, &hEnum, &tkContract)) {

        // Get the attribute type (token of the ctor used) since we need this information in order to parse the blob we'll find
        // next.
        mdToken tkAttrType;
        IfFailRetRcNull(pImport->GetCustomAttributeProps(tkContract, &tkAttrType));
        if (!pImport->IsValidToken(tkAttrType))
            continue;

        // The token should be a member ref or method def.
        // Get the signature of the ctor so we know which version has been called.
        PCCOR_SIGNATURE pSig;
        DWORD           cbSig;
        LPCSTR          szName_Ignore;
        if (TypeFromToken(tkAttrType) == mdtMemberRef)
        {
            IfFailRetRcNull(pImport->GetNameAndSigOfMemberRef(tkAttrType, &pSig, &cbSig, &szName_Ignore));
        }
        else
        {
            if (TypeFromToken(tkAttrType) != mdtMethodDef)
                continue;
            IfFailRetRcNull(pImport->GetNameAndSigOfMethodDef(tkAttrType, &pSig, &cbSig, &szName_Ignore));
        }

        // Only two signatures are supported: the null sig '()' and the full sig '(Consistency, CER)'.
        // Set a boolean based on which one was provided.
        bool                    fNullCtor;
        ULONG                   eCallConv;

        SigPointer sig(pSig, cbSig);

        // Check the calling convention is what we expect (default convention on an instance method).
        IfFailRetRcNull(sig.GetCallingConvInfo(&eCallConv));
        _ASSERTE(eCallConv == (IMAGE_CEE_CS_CALLCONV_DEFAULT | IMAGE_CEE_CS_CALLCONV_HASTHIS));
        if (eCallConv != (IMAGE_CEE_CS_CALLCONV_DEFAULT | IMAGE_CEE_CS_CALLCONV_HASTHIS))
            IfFailRetRcNull(COR_E_BADIMAGEFORMAT);

        // If so, the next datum is the count of arguments, and this is all we need to determine which ctor has been used.
        ULONG dwArgs;
        IfFailRetRcNull(sig.GetData(&dwArgs));
        _ASSERTE(dwArgs == 0 || dwArgs == 2);
        if (dwArgs != 0 && dwArgs != 2)
            IfFailRetRcNull(COR_E_BADIMAGEFORMAT);

        fNullCtor = dwArgs == 0;

        // Now we know how to parse the blob, let's fetch a pointer to it.
        BYTE const *pbData;
        DWORD       cbData;
        IfFailRetRcNull(pImport->GetCustomAttributeAsBlob(tkContract, (const void **)&pbData, &cbData));
        
        // Check serialization format (we support version 1 only).
        if (cbData < sizeof(WORD) || GET_UNALIGNED_VAL16(pbData) != 1)
            IfFailRetRcNull(COR_E_BADIMAGEFORMAT);
        pbData += sizeof(WORD);
        cbData -= sizeof(WORD);

        // Parse ctor arguments if we have any.
        if (!fNullCtor) {

            // We assume the enums are based on DWORDS.
            if (cbData < (sizeof(DWORD) * 2))
                IfFailRetRcNull(COR_E_BADIMAGEFORMAT);

            // Consistency first.
            DWORD dwConsistency = GET_UNALIGNED_VAL32(pbData);
            pbData += sizeof(DWORD);
            cbData -= sizeof(DWORD);
            if (dwConsistency > RC_CONSISTENCY_CORRUPT_NOTHING)
                IfFailRetRcNull(COR_E_BADIMAGEFORMAT);

            // Followed by Cer.
            DWORD dwCer = GET_UNALIGNED_VAL32(pbData);
            pbData += sizeof(DWORD);
            cbData -= sizeof(DWORD);
            if (dwCer > RC_CER_SUCCESS)
                IfFailRetRcNull(COR_E_BADIMAGEFORMAT);

            dwResult = RC_MERGE(dwResult, RC_ENCODE(dwConsistency, dwCer));
        }

        // Get the count of field/property, value pairs.
        if (cbData < sizeof(WORD))
            IfFailRetRcNull(COR_E_BADIMAGEFORMAT);
        WORD cPairs = GET_UNALIGNED_VAL16(pbData);
        pbData += sizeof(WORD);
        cbData -= sizeof(WORD);

        // Iterate over any such pairs, looking for values we haven't picked up yet.
        for (DWORD i = 0 ; i < cPairs; i++) {

            // First is a field vs property selector. We expect only properties.
            if (cbData < sizeof(BYTE) || *(BYTE*)pbData != SERIALIZATION_TYPE_PROPERTY)
                IfFailRetRcNull(COR_E_BADIMAGEFORMAT);
            pbData += sizeof(BYTE);
            cbData -= sizeof(BYTE);

            // Next is the type of the property. It had better be an enum.
            if (cbData < sizeof(BYTE) || *(BYTE*)pbData != SERIALIZATION_TYPE_ENUM)
                IfFailRetRcNull(COR_E_BADIMAGEFORMAT);
            pbData += sizeof(BYTE);
            cbData -= sizeof(BYTE);

            // Next we have the assembly qualified enum type name. This is preceded by a metadata style packed byte length (the
            // string itself is 8-bit and not null terminated). Ignore it (just skip across) and we'll key off the property name
            // (coming up) instead.
            DWORD cbName;
            BYTE const * pbPostEncodedLength;
            IfFailRetRcNull(CPackedLen::SafeGetData(pbData, cbData, &cbName, &pbPostEncodedLength));
            DWORD cbEncodedLength = static_cast<DWORD>(pbPostEncodedLength - pbData);
            pbData += cbEncodedLength + cbName;
            cbData -= cbEncodedLength + cbName;

            // Now we have the name of the property (in a similar format to above).
            IfFailRetRcNull(CPackedLen::SafeGetData(pbData, cbData, &cbName, &pbPostEncodedLength));
            cbEncodedLength = static_cast<DWORD>(pbPostEncodedLength - pbData);
            pbData += cbEncodedLength;
            cbData -= cbEncodedLength;

            bool fConsistencyProp = false;
            if (cbName == strlen(RC_CONSISTENCY_PROP_NAME) && strncmp((const char*)pbData, RC_CONSISTENCY_PROP_NAME, cbName) == 0)
                fConsistencyProp = true;
            else if (cbName == strlen(RC_CER_PROP_NAME) && strncmp((const char*)pbData, RC_CER_PROP_NAME, cbName) == 0)
                fConsistencyProp = false;
            else
                IfFailRetRcNull(COR_E_BADIMAGEFORMAT);
            pbData += cbName;
            cbData -= cbName;

            // And finally the actual enum value (again, we assume the underlying type is a DWORD).
            if (cbData < sizeof(DWORD))
                IfFailRetRcNull(COR_E_BADIMAGEFORMAT);
            DWORD dwValue = GET_UNALIGNED_VAL32(pbData);
            pbData += sizeof(DWORD);
            cbData -= sizeof(DWORD);

            if (fConsistencyProp) {
                if (dwValue > RC_CONSISTENCY_CORRUPT_NOTHING)
                    IfFailRetRcNull(COR_E_BADIMAGEFORMAT);
                dwResult = RC_MERGE(dwResult, RC_ENCODE(dwValue, RC_CER_UNDEFINED));
            } else {
                if (dwValue > RC_CER_SUCCESS)
                    IfFailRetRcNull(COR_E_BADIMAGEFORMAT);
                dwResult = RC_MERGE(dwResult, RC_ENCODE(RC_CONSISTENCY_UNDEFINED, dwValue));
            }
        }

        // Shouldn't have any bytes left in the blob at this stage.
        _ASSERTE(cbData == 0);
    }

    // Return the result encoded and packed into the 2 low order bits of a DWORD.
    return dwResult;
}

// Given a metadata token, a scoping module and a type context, look up the appropriate MethodDesc (and recomputed accompanying type
// context).
MethodContext *TokenToMethodDesc(Module *pModule, mdToken tokMethod, SigTypeContext *pTypeContext)
{
    STANDARD_VM_CONTRACT;

    // Validate that the token is one that we can handle.
    if (!pModule->GetMDImport()->IsValidToken(tokMethod) || (TypeFromToken(tokMethod) != mdtMethodDef &&
                                                             TypeFromToken(tokMethod) != mdtMethodSpec &&
                                                             TypeFromToken(tokMethod) != mdtMemberRef)) {
        COMPlusThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_METHOD_TOKEN);
    }

    // Look up the MethodDesc based on the token and type context.
    MethodDesc *pMD = MemberLoader::GetMethodDescFromMemberDefOrRefOrSpec(pModule,
                                                                          tokMethod,
                                                                          pTypeContext,
                                                                          TRUE,
                                                                          FALSE);

    // The MethodDesc we get might be shared between several types. If so we'll need to do extra work to locate the exact
    // class instantiation instead of the default representative one.
    SigTypeContext sNewTypeContext;
    if (pMD->IsSharedByGenericInstantiations()) {
        TypeHandle th = GetTypeFromMemberDefOrRefOrSpecThrowing(pModule,
                                                                tokMethod,
                                                                pTypeContext);
        SigTypeContext::InitTypeContext(pMD, th,&sNewTypeContext);
    } else
        SigTypeContext::InitTypeContext(pMD, pMD->GetClassInstantiation(), pMD->GetMethodInstantiation(),&sNewTypeContext);

    return MethodContext::PerThreadAllocate(pMD, &sNewTypeContext);
}

// Locate an exact type definition given a method token and the type context in which it can be resolved.
TypeHandle GetTypeFromMemberDefOrRefOrSpecThrowing(Module         *pModule,
                                                   mdToken         tokMethod,
                                                   SigTypeContext *pTypeContext)
{
    STANDARD_VM_CONTRACT;

    IMDInternalImport *pImport = pModule->GetMDImport();

    // Convert method specs into the underlying member ref.
    if (TypeFromToken(tokMethod) == mdtMethodSpec)
    {
        PCCOR_SIGNATURE   pSig;
        ULONG             cSig;
        mdMemberRef       tkGenericMemberRef;
        
        IfFailThrow(pImport->GetMethodSpecProps(tokMethod, &tkGenericMemberRef, &pSig, &cSig));
        
        if (!pImport->IsValidToken(tkGenericMemberRef) || 
            (TypeFromToken(tkGenericMemberRef) != mdtMethodDef &&
             TypeFromToken(tkGenericMemberRef) != mdtMemberRef))
        {
            COMPlusThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_TOKEN_TYPE);
        }
        
        tokMethod = tkGenericMemberRef;
    }
    
    // Follow the member ref/def back up to the type def/ref/spec or module (for global methods).
    if (TypeFromToken(tokMethod) == mdtMemberRef)
    {
        IfFailThrow(pImport->GetParentOfMemberRef(tokMethod, &tokMethod));
        
        // For varargs, a memberref can point to a methodDef
        if (TypeFromToken(tokMethod) == mdtMethodDef)
        {
            if (!pImport->IsValidToken(tokMethod))
            {
                COMPlusThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_TOKEN);
            }
            IfFailThrow(pImport->GetParentToken(tokMethod, &tokMethod));
        }
    }
    else if (TypeFromToken(tokMethod) == mdtMethodDef)
    {
        IfFailThrow(pImport->GetParentToken(tokMethod, &tokMethod));
    }
    
    if (!pImport->IsValidToken(tokMethod) || (TypeFromToken(tokMethod) != mdtTypeDef  &&
                                              TypeFromToken(tokMethod) != mdtTypeRef  &&
                                              TypeFromToken(tokMethod) != mdtTypeSpec &&
                                              TypeFromToken(tokMethod) != mdtModuleRef))
    {
        COMPlusThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_TOKEN);
    }
    
    // Load the type in question, using a type context if necessary to get an exact representation.
    TypeHandle th;
    if (TypeFromToken(tokMethod) == mdtModuleRef) {
        DomainFile *pNewModule = pModule->LoadModule(GetAppDomain(), tokMethod, FALSE);
        if (pNewModule != NULL)
            th = TypeHandle(pNewModule->GetModule()->GetGlobalMethodTable());
    } else {
        th = ClassLoader::LoadTypeDefOrRefOrSpecThrowing(pModule,
                                                         tokMethod,
                                                         pTypeContext);
    }

    if (th.IsNull())
        COMPlusThrowHR(COR_E_BADIMAGEFORMAT);

    return th;
}

// Determine whether the method given as a parameter is the root of a CER.
// @todo: Need an x86 offset as well and logic to determine whether we're actually in a root-CER portion of the method (if the whole
// thing isn't the root).
bool IsCerRootMethod(MethodDesc *pMD)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMD));
        SO_TOLERANT;
    } CONTRACTL_END;

    // Treat IL stubs as CER roots (marshaling code needs to string together operations without being interruped by thread aborts).
    if (pMD->IsILStub())
        return true;

    // There are some well defined root methods defined by the system.
    if (pMD == g_pExecuteBackoutCodeHelperMethod)
        return true;

    // For now we just look to see whether there is some prep or fixup info stored for this method.
    Module *pModule = pMD->GetModule();

    if (pModule->GetCerPrepInfo(pMD) != NULL)
        return true;

#ifdef FEATURE_PREJIT
    if (pModule->IsNgenCerRootMethod(pMD))
        return true;
#endif

    return false;
}

// Fill the cache of overflowed generic dictionary entries that the jit maintains with all the overflow slots stored so far in the
// dictionary layout.
void PrepopulateGenericHandleCache(DictionaryLayout  *pDictionaryLayout,
                                   MethodDesc        *pMD,
                                   MethodTable       *pMT)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    // Dictionary overflow entries are recorded starting in the second bucket of the dictionary layout.
    DictionaryLayout *pOverflows = pDictionaryLayout->GetNextLayout();

    while (pOverflows) {
        for (DWORD i = 0; i < pOverflows->GetMaxSlots(); i++) {
            DictionaryEntryLayout *pEntry = pOverflows->GetEntryLayout(i);

            // We've finished as soon as we find the first unused slot.
            if (!pEntry->m_signature)
                return;

            // We have a valid overflow entry. Determine the handle value given the type context we have and push it into the JIT's
            // cache.
            JIT_GenericHandleWorker(pMD, pMT, pEntry->m_signature);
        }
        pOverflows = pOverflows->GetNextLayout();
    }
}

#ifdef FEATURE_PREJIT

// Prepare the CER rooted at the given method (it's OK to pass a MethodDesc* that doesn't root a CER, in which case the method
// is a no-op).
void CerNgenRootTable::Restore(MethodDesc *pRootMD)
{
#ifndef CROSSGEN_COMPILE
    STANDARD_VM_CONTRACT;

    // We don't have a restoration bitmap at ngen time. No matter, we just always claim everything is restored.
    if (m_pRestoreBitmap == NULL)
        return;

    // Locate the root index from the table. Failure indicates there's no work to do.
    DWORD dwIndex = FindIndex(pRootMD);
    if (dwIndex == NoSuchRoot)
        return;

    _ASSERTE(m_pRoots[dwIndex].m_pRootMD == pRootMD);

    // Check then mark the fact that we're preparing (to prevent potential recursion).
    SigTypeContext typeContext;
    if (!GetThread()->GetCerPreparationState()->CanPreparationProceed(pRootMD, &typeContext))
        return;

    MethodCallGraphPreparer sPrep(pRootMD, &typeContext, true, true);
    MethodCallGraphPreparer::ThreadPreparingCerHolder sCerHolder(&sPrep);

#ifdef _DEBUG
    if (GetCerLoggingOptions())
    {
        CER_LOG(RESTORE, ("---------------------------------------------------------------\n"));
        SString ssRootMethod;
        TypeString::AppendMethodInternal(ssRootMethod, pRootMD, TypeString::FormatNamespace | TypeString::FormatStubInfo);
        CER_LOG(RESTORE, ("Restoring CER graph from %S\n", ssRootMethod.GetUnicode()));
    }
#endif

    g_IBCLogger.LogCerMethodListReadAccess(pRootMD);

    // Retrieve the CerRoot structure.
    CerRoot *pRoot = &m_pRoots[dwIndex];
    _ASSERTE(pRoot->m_pRootMD == pRootMD);

    // Scan the list of methods in the CER (the last one is a sentinel with a NULL MethodDesc*). Restore each method as we go.
    MethodContextElement *pEntry = pRoot->m_pList;
    while (pEntry->GetMethodDesc())
    {
        // Method desc and type handle pointers may still be tokenized at this point.
        Module::RestoreMethodDescPointer(&pEntry->m_pMethodDesc);
        Module::RestoreMethodTablePointer(&pEntry->m_pExactMT);

        g_IBCLogger.LogCerMethodListReadAccess(pEntry->GetMethodDesc());

        MethodDesc * pMD = pEntry->GetMethodDesc();

        // Check whether there are generic dictionaries that need to be pre-populated.

        // Don't use the direct PrepopulateDictionary method here for MethodTable/MethodDesc
        //  - MethodTable: Takes binding considerations into account (which we don't care about)
        //  - MethodDesc: Appears to use a representative class instantiation (and we have the exact one handy)
        //
        // Additionally, avoid touching EE Class if we don't need to
        MethodTable * pMT = pEntry->GetExactMT();
        if (pMT != NULL)
        {
            // MethodTable
            DictionaryLayout *pClassDictLayout = pMT->GetClass()->GetDictionaryLayout();
            if (pClassDictLayout)
            {
                pMT->GetDictionary()->PrepopulateDictionary(NULL, pMT, false);

                // The dictionary may have overflowed in which case we need to prepopulate the jit's lookup cache as well.
                PrepopulateGenericHandleCache(pClassDictLayout, NULL, pMT);
            }

            // MethodDesc
            DictionaryLayout *pMethDictLayout = pMD->GetDictionaryLayout();
            if (pMethDictLayout)
            {
                pMD->GetMethodDictionary()->PrepopulateDictionary(pMD, NULL, false);

                // The dictionary may have overflowed in which case we need to prepopulate the jit's lookup cache as well.
                PrepopulateGenericHandleCache(pMethDictLayout, pMD, NULL);
            }
        }

        // Recreate stubs used by P/Invoke, COM calls, or FCalls by exercising the prestub.
        if (pMD->IsPointingToPrestub() && (pMD->IsNDirect() || pMD->IsComPlusCall() || pMD->IsFCall()))
        {
            pMD->EnsureActive();
            pMD->DoPrestub(NULL);
        }

#ifdef _DEBUG
        if (GetCerLoggingOptions())
        {
            SString ssMethod;
            TypeString::AppendMethodInternal(ssMethod, pMD, TypeString::FormatNamespace | TypeString::FormatStubInfo);
            CER_LOG(RESTORE, ("  %S\n", ssMethod.GetUnicode()));
        }
#endif

        // Move to next entry.
        pEntry++;
    }

    CER_LOG(RESTORE, ("---------------------------------------------------------------\n"));

    // Mark this whole CER region as fixed up by setting a flag in the restore bitmap (kept separate so we can cluster all our page
    // writes).
    // Compute the DWORD offset into the flag array and then the mask for the specific bit in that DWORD.
    DWORD dwOffset = dwIndex / (sizeof(DWORD) * 8);
    DWORD dwMask = 1 << (dwIndex % (sizeof(DWORD) * 8));
    EnsureWritablePages(m_pRestoreBitmap, sizeof(DWORD) * SizeOfRestoreBitmap());
    FastInterlockOr(&m_pRestoreBitmap[dwOffset], dwMask);

    // If we fixed up any methods with their own CERs then we will have implicitly fixed up those too. Mark their fixup records as
    // completed as well to avoid further unecessary work.
    pEntry = pRoot->m_pList;
    while (pEntry->GetMethodDesc()) {
        dwIndex = FindIndex(pEntry->GetMethodDesc());
        if (dwIndex != NoSuchRoot) {
            dwOffset = dwIndex / (sizeof(DWORD) * 8);
            dwMask = 1 << (dwIndex % (sizeof(DWORD) * 8));
            FastInterlockOr(&m_pRestoreBitmap[dwOffset], dwMask);
        }
        pEntry++;
    }
#endif // CROSSGEN_COMPILE
}

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
// Add a new root to the table, expanding it as necessary. Note that this method must be called with the CerCrst already held.
void CerNgenRootTable::AddRoot(MethodDesc *pRootMD, MethodContextElement *pList)
{
    CONTRACTL {
        STANDARD_VM_CHECK;
        PRECONDITION(IsOwnerOfCrst(pRootMD->GetModule()->GetCerCrst()));
    } CONTRACTL_END;

    // Ensure we have enough space first.
    if (m_cRoots == m_cSlots) {
        DWORD cNewSize = m_cSlots + 16;
        CerRoot *pNewArray = new CerRoot[cNewSize];
        memcpyNoGCRefs(pNewArray, m_pRoots, m_cRoots * sizeof(CerRoot));
        MethodContextElement **pNewRootsInCompilationOrder = new MethodContextElement*[cNewSize];
        memcpyNoGCRefs(pNewRootsInCompilationOrder, m_pRootsInCompilationOrder, m_cRoots * sizeof(MethodContextElement*) );
        m_cSlots = cNewSize;
        delete m_pRoots;
        m_pRoots = pNewArray;
        delete m_pRootsInCompilationOrder;
        m_pRootsInCompilationOrder = pNewRootsInCompilationOrder;
    }

    // Fill in the new entry in sorted order.
    DWORD i;
    for (i = 0; i < m_cRoots; i++)
        if ((UPTR) m_pRoots[i].m_pRootMD > (UPTR) pRootMD)
            break;
    if (i < m_cRoots)
        memmove(&m_pRoots[i + 1], &m_pRoots[i], (m_cRoots - i) * sizeof(CerRoot));
    m_pRoots[i].m_pRootMD = pRootMD;
    m_pRoots[i].m_pList = pList;

    m_pRootsInCompilationOrder[m_cRoots] = pList;

    m_cRoots++;
}

// Ngen callouts to help serialize this structure and its children to storage.
void CerNgenRootTable::Save(DataImage *image, CorProfileData *profileData)
{
    STANDARD_VM_CONTRACT;

#ifdef _DEBUG
    DWORD dwMaxEntries = 0;
    DWORD dwTotalEntries = 0;
#endif

    image->StoreStructure(this, sizeof(CerNgenRootTable), DataImage::ITEM_CER_ROOT_TABLE);
    image->StoreStructure(m_pRoots, m_cRoots * sizeof(CerRoot), DataImage::ITEM_CER_ROOT_TABLE);

    // Create a bitmap of boolean flags (1 bit per flag) indicating whether the CER at a given index in the array has been restored.
    // This is initially all zero and only filled in at runtime (keep all the flags together this way because they're the only
    // things we have to write at runtime and we want to keep them as dense as possible).
    _ASSERTE((SizeOfRestoreBitmap() % sizeof(DWORD)) == 0);
    m_pRestoreBitmap = new DWORD[SizeOfRestoreBitmap() / sizeof(DWORD)];
    memset(m_pRestoreBitmap, 0xff, SizeOfRestoreBitmap());

    image->StoreStructure(m_pRestoreBitmap,
                          SizeOfRestoreBitmap(),
                          DataImage::ITEM_CER_RESTORE_FLAGS);

    // Next save off the list of MethodContextElements associated with each root.
    for (DWORD i = 0; i < m_cRoots; i++) {
        MethodContextElement *pEntry = m_pRootsInCompilationOrder[i];
        
        // Count entries in list.
        DWORD cEntries = 0;
        while (pEntry->GetMethodDesc()) {
            cEntries++;
            pEntry++;
        }

        // Plus one for the sentinel value.
        cEntries++;

#ifdef _DEBUG
        dwTotalEntries += cEntries;
        if (cEntries > dwMaxEntries)
            dwMaxEntries = cEntries;
#endif

        // Store this list.
        image->StoreStructure(m_pRootsInCompilationOrder[i],
                              cEntries * sizeof(MethodContextElement),
                              DataImage::ITEM_CER_METHOD_LIST);
    }

#ifdef _DEBUG
    if (m_cRoots > 0) {
        CER_LOG(NGEN_STATS, ("Saving %u CER roots in ngen image\n", m_cRoots));
        CER_LOG(NGEN_STATS, ("  Max methods in CER: %u\n", dwMaxEntries));
        CER_LOG(NGEN_STATS, ("  Avg methods in CER: %.1f\n", (float)((float)dwTotalEntries / (float)m_cRoots)));
    } else
        CER_LOG(NGEN_STATS, ("No CER roots in ngen image\n"));
#endif
}

void CerNgenRootTable::Fixup(DataImage *image)
{
    STANDARD_VM_CONTRACT;

    DWORD i;

    // We still use the point to the root array even though at runtime the two structures will be adjacent.
    image->FixupPointerField(this, offsetof(CerNgenRootTable, m_pRoots));

    // Restoration flags are used only at runtime and must start off zeroed.
    image->FixupPointerField(this, offsetof(CerNgenRootTable, m_pRestoreBitmap));
    image->ZeroField(m_pRestoreBitmap, 0, SizeOfRestoreBitmap());

    // The root list in compilation order is only used at ngen time, and is not written into native image.
    image->ZeroPointerField(this, offsetof(CerNgenRootTable, m_pRootsInCompilationOrder));

    // Fixup all the pointers in the individual CERs.
    for (i = 0; i < m_cRoots; i++) {

        // For each MethodContextElement in the list we need to fixup a pointer to a MethodDesc and two array pointers (one for any
        // class instantiation and one for any method instantiation). The actual MethodDescs and TypeHandles themselves are already
        // fixed up as are the instantiation arrays we point to (they're the ones inside the generic dictionaries of the class/method
        // concerned).
        MethodContextElement *pList = m_pRootsInCompilationOrder[i];
        MethodContextElement *pEntry = pList;
        while (pEntry->GetMethodDesc()) {
            image->FixupMethodDescPointer(pList, &pEntry->m_pMethodDesc);
            image->FixupMethodTablePointer(pList, &pEntry->m_pExactMT);
            pEntry++;
        }
    }
}

void CerNgenRootTable::FixupRVAs(DataImage *image)
{
    STANDARD_VM_CONTRACT;

    DWORD i, j;

    // Now we go back through the root table and sort the entries based on the locations of the root method descs in the new image
    // (they may be rearranged due to IBC profiling).
    CerRoot *pNewRoots = (CerRoot*)image->GetImagePointer(m_pRoots);
    PREFIX_ASSUME(pNewRoots != NULL);

    // Simple insertion sort. Starting at the second element insert a candidate into its correct location in the sub-list
    // preceding it (which by definition will already be sorted).
    for (i = 1; i < m_cRoots; i++)
    {
        // Look at all of the preceding elements for the first that is larger than the candidate (i.e. should succeed the
        // candidate in sorted order). If we don't find one then the candidate is already in place and we can proceed to the
        // next candidate.
        for (j = 0; j < i; j++)
            if (image->GetRVA(pNewRoots[j].m_pRootMD) > image->GetRVA(pNewRoots[i].m_pRootMD)) {

                // Need to move candidate element up. Cache its value because we're about to overwrite it.
                MethodDesc *pTmpRootMD = pNewRoots[i].m_pRootMD;
                MethodContextElement *pTmpList = pNewRoots[i].m_pList;

                // Shuffle the sorted list one up to make room for the candidate.
                memmove(&pNewRoots[j + 1], &pNewRoots[j], (i - j) * sizeof(CerRoot));

                // Insert the candidate into position.
                pNewRoots[j].m_pRootMD = pTmpRootMD;
                pNewRoots[j].m_pList = pTmpList;

                // Sorted the candidate, move onto the next.
                break;
            }
    }

    // Fixup all the pointers in the individual CERs.
    for (i = 0; i < m_cRoots; i++) {
        // Fix up the pointer to the root method and the list of methods in the CER.
        image->FixupField(m_pRoots, sizeof(CerRoot) * i + offsetof(CerRoot, m_pRootMD),
            pNewRoots[i].m_pRootMD);
        image->FixupField(m_pRoots, sizeof(CerRoot) * i + offsetof(CerRoot, m_pList),
            pNewRoots[i].m_pList);
    }
}
#endif // FEATURE_NATIVE_IMAGE_GENERATION

// Locate the index of a given CerRoot record in the array given the root method. This is used to access the array and to locate the
// restored flag for the entry in the restored bitmap. NoSuchRoot is returned if the root cannot be found.
DWORD CerNgenRootTable::FindIndex(MethodDesc *pRootMD)
{
    CONTRACTL {
        NOTHROW;
        MODE_ANY;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pRootMD));
        SO_TOLERANT;
    } CONTRACTL_END;

    // The table is guaranteed to be sorted, so we can lookup our target with a binary search.
    DWORD dwLow = 0;
    DWORD dwHigh = m_cRoots - 1;
    while (true) {

        // Take out the simple cases first.

        // The range has only one entry.
        if (dwLow == dwHigh) {
            if (m_pRoots[dwLow].m_pRootMD == pRootMD)
                return dwLow;
#ifdef _DEBUG
            for (DWORD i = 0; i < m_cRoots; i++)
                _ASSERTE(m_pRoots[i].m_pRootMD != pRootMD);
#endif
            return NoSuchRoot;
        }

        // The range has only two entries.
        if (dwLow == dwHigh - 1) {
            if (m_pRoots[dwLow].m_pRootMD == pRootMD)
                return dwLow;
            if (m_pRoots[dwHigh].m_pRootMD == pRootMD)
                return dwHigh;
#ifdef _DEBUG
            for (DWORD i = 0; i < m_cRoots; i++)
                _ASSERTE(m_pRoots[i].m_pRootMD != pRootMD);
#endif
            return NoSuchRoot;
        }

        // Now we can compute a midpoint that is definitely distinct and in-between the endpoints.
        DWORD dwMid = dwLow + ((dwHigh - dwLow) / 2);

        // Did we nail it?
        if (m_pRoots[dwMid].m_pRootMD == pRootMD)
            return dwMid;

        // Otherwise adjust our range to be the bit we haven't looked at and iterate.
        if ((UPTR)m_pRoots[dwMid].m_pRootMD < (UPTR)pRootMD)
            dwLow = dwMid + 1;
        else
            dwHigh = dwMid - 1;
    }
}

// Prepare the class if it is derived from CriticalFinalizerObject. This is used at ngen time since such classes are normally 
// prepared at runtime (at instantiation) and would therefore miss the ngen image.
void PrepareCriticalType(MethodTable * pMT)
{
    STANDARD_VM_CONTRACT;

    // Prepare any class that satisfies the criteria. Pass a pointer to this module so that we'll only prepare any overrides of
    // the critical methods that were actually introduced here.
    if (pMT->HasCriticalFinalizer())
        PrepareCriticalFinalizerObject(pMT, pMT->GetLoaderModule());
}

// Prepare a method and its statically determinable call graph if a hint attribute has been applied. This is only called at ngen
// time to save additional preparation information into the ngen image that wouldn't normally be there (and thus lower runtime
// overheads).
void PrePrepareMethodIfNecessary(CORINFO_METHOD_HANDLE hMethod)
{
    STANDARD_VM_CONTRACT;

    EX_TRY {

        // Translate jit-style method handle into method desc.
        MethodDesc *pMD = GetMethod(hMethod);

        // Check for the existance of the attribute.
        IMDInternalImport  *pImport = pMD->GetMDImport();
        mdToken             tkMethod = pMD->GetMemberDef();
        HRESULT             hr = pImport->GetCustomAttributeByName(tkMethod,
                                                                   "System.Runtime.ConstrainedExecution.PrePrepareMethodAttribute",
                                                                   NULL, NULL);

        // TODO: We should add IBC probes which indicate that methods need to be preprepared
        //                which can then be reflected in the IBC data, we can add an additional check
        //                here to cover that case, then we can get around this problem with profiling
        //                instead of manual programmer effort.

        // Only prepare if we definitely saw the attribute.
        if (hr == S_OK) {
            // Prepare the method and its graph. There should never be any open type parameters (we can't do much at ngen time with these),
            // so we can pass a null type context.
            SigTypeContext sTypeContext;
            MethodCallGraphPreparer mcgp(pMD, &sTypeContext, true, true);
            mcgp.Run();
        }

    } EX_CATCH {
    } EX_END_CATCH(SwallowAllExceptions);
}

#endif // FEATURE_PREJIT

PtrHashCache::PtrHashCache()
{
    LIMITED_METHOD_CONTRACT;
    ZeroMemory(this, sizeof(*this));

    // First entry in each bucket is a chain index used to evenly distribute inserts within a bucket.
    _ASSERTE(PHC_CHAIN > 1);
}

bool PtrHashCache::Lookup(void *pKey, DWORD *pdwValue)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(((UINT_PTR)pKey & PHC_DATA_MASK) == 0);

    DWORD dwBucket = GetHash(pKey);

    // Skip first entry in bucket, it's a sequence number used for insertions.
    for (DWORD i = 1; i < PHC_CHAIN; i++) {
        UINT_PTR uipEntry = VolatileLoad<UINT_PTR>(&m_rEntries[(dwBucket * PHC_CHAIN) + i]);
        if ((uipEntry & ~PHC_DATA_MASK) == (UINT_PTR)pKey) {
#ifdef _DEBUG
            FastInterlockIncrement((LONG*)&m_dwHits);
#endif
            *pdwValue = uipEntry & PHC_DATA_MASK;
            return true;
        }
    }

#ifdef _DEBUG
    FastInterlockIncrement((LONG*)&m_dwMisses);
#endif
    return false;
}

void PtrHashCache::Add(void *pKey, DWORD dwValue)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(((UINT_PTR)pKey & PHC_DATA_MASK) == 0);
    _ASSERTE((dwValue & ~PHC_DATA_MASK) == 0);

    DWORD dwBucket = GetHash(pKey);

    // We keep a sequence number in the first entry of the bucket so that we distribute insertions within the bucket evenly. We're
    // racing when we update this value, but it doesn't matter if we lose an update (we're a cache after all). We don't bother being
    // careful to avoid overflowing the value here (we just keep incrementing); we'll do the modulo logic when we insert our value
    // instead.
    DWORD dwIndex = static_cast<DWORD>(m_rEntries[dwBucket * PHC_CHAIN]++);
    dwIndex = (dwIndex % (PHC_CHAIN - 1)) + 1;
    m_rEntries[(dwBucket * PHC_CHAIN) + dwIndex] = ((UINT_PTR)pKey & ~PHC_DATA_MASK) | dwValue;
}

DWORD PtrHashCache::GetHash(void *pKey)
{
    LIMITED_METHOD_CONTRACT;

    return (DWORD)(((UINT_PTR)pKey >> 4) % PHC_BUCKETS);
}

#ifdef _DEBUG
void PtrHashCache::DbgDumpStats()
{
#if 0
    if ((m_dwHits + m_dwMisses) == 0)
        return;

    printf("Dumping stats for PtrHashCache %08X\n", this);
    printf("  %u hits, %u misses (%u%% hit rate)\n", m_dwHits, m_dwMisses, (m_dwHits * 100) / (m_dwHits + m_dwMisses));
    for (DWORD i = 0; i < PHC_BUCKETS; i++)
        printf("    [%2u] : %u insertions\n", i, m_rEntries[i * PHC_CHAIN]);
    printf("\n");
#endif
}
#endif
