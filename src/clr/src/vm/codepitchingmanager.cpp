// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: CodePitchingManager.cpp
//

// ===========================================================================
// This file contains the implementation for code pitching.
// Its distinctive features and algorithm are:
//
// 1. All its code is under #if defined(FEATURE_JIT_PITCHING) and doesn't mess up with other code
// 2. This feature is working only if the options INTERNAL_JitPitchEnabled != 0 and INTERNAL_JitPitchMemThreshold > 0
// 3. Jitted code can be pitched only for methods that are not Dynamic, FCall or Virtual
// 4. If the size of the generated native code exceeds the value of INTERNAL_JitDPitchMethodSizeThreshold this code is
//    placed in the special heap code list. Each heap block in this list stores the code for only one method and has the
//    sufficient size for the code of a method aligned to 4K. The pointers to such methods are stored in the
//    "PitchingCandidateMethods" hash map.
// 5. If the entrypoint of a method is backpatched this method is excluded from the "PitchingCandidateMethods" hash map
//    and stored in "NotForPitchingMethods" hashmap.
// 6. When the total size of the generated native code exceeds the value of INTERNAL_JitPitchMemThreshold option, the
//    execution of the program is stopped and stack frames for all the threads are inspected and pointers to methods
//    being executed are stored in the "ExecutedMethods" hash map
// 7. The code for all the methods from the "PitchingCandidateMethods" that are not in the "ExecutedMethods" is pitched.
//    (All heap blocks for these methods are set in the initial state and can be reused for newly compiled methods, pointers
//     to the code for non-executed methods are set to nullptr).
// 8. If the code for the given method is pitched once, this method is stored in the "NotForPitchingMethods" hashmap. Thus,
//    if this method is compiled the second time, it is considered as called repeatedly, therefore, pitching for it is inexpedient,
//    and the newly compiled code stored in the usual heap.
// 9. The coreclr code with this feature is built by the option
//     ./build.sh cmakeargs -DFEATURE_JIT_PITCHING=true
// ===========================================================================

#include "common.h"

#ifndef DACCESS_COMPILE

#if defined(FEATURE_JIT_PITCHING)

#include "nibblemapmacros.h"
#include "threadsuspend.h"

static PtrHashMap* s_pPitchingCandidateMethods = nullptr;
static PtrHashMap* s_pPitchingCandidateSizes = nullptr;
static SimpleRWLock* s_pPitchingCandidateMethodsLock = nullptr;

static PtrHashMap* s_pExecutedMethods = nullptr;
static SimpleRWLock* s_pExecutedMethodsLock = nullptr;

static PtrHashMap* s_pNotForPitchingMethods = nullptr;
static SimpleRWLock* s_pNotForPitchingMethodsLock = nullptr;

#ifdef _DEBUG
static PtrHashMap* s_pPitchedMethods = nullptr;
static SimpleRWLock* s_pPitchedMethodsLock = nullptr;
#endif

static ULONG s_totalNCSize = 0;
static SimpleRWLock* s_totalNCSizeLock = nullptr;

static ULONG s_jitPitchedBytes = 0;

static INT64 s_JitPitchLastTick = 0;

static bool s_JitPitchInitialized = false;


static BOOL IsOwnerOfRWLock(LPVOID lock)
{
    // @TODO - SimpleRWLock does not have knowledge of which thread gets the writer
    // lock, so no way to verify
    return TRUE;
}

static void CreateRWLock(SimpleRWLock** lock)
{
    if (*lock == nullptr)
    {
        void *pLockSpace = SystemDomain::GetGlobalLoaderAllocator()->GetLowFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(SimpleRWLock)));
        SimpleRWLock *pLock = new (pLockSpace) SimpleRWLock(COOPERATIVE_OR_PREEMPTIVE, LOCK_TYPE_DEFAULT);

        if (FastInterlockCompareExchangePointer(lock, pLock, NULL) != NULL)
            SystemDomain::GetGlobalLoaderAllocator()->GetLowFrequencyHeap()->BackoutMem(pLockSpace, sizeof(SimpleRWLock));
    }
}

static PtrHashMap* CreateHashMap(SimpleRWLock* rwLock)
{
    PtrHashMap *pMap = new (SystemDomain::GetGlobalLoaderAllocator()->GetLowFrequencyHeap()) PtrHashMap();
    LockOwner lock = {rwLock, IsOwnerOfRWLock};
    pMap->Init(32, nullptr, FALSE, &lock);
    return pMap;
}

static void InitializeJitPitching()
{
    if (!s_JitPitchInitialized)
    {
        CreateRWLock(&s_pNotForPitchingMethodsLock);
        CreateRWLock(&s_pPitchingCandidateMethodsLock);
        CreateRWLock(&s_totalNCSizeLock);

        {
            SimpleReadLockHolder srlh(s_pNotForPitchingMethodsLock);
            if (s_pNotForPitchingMethods == nullptr)
            {
                s_pNotForPitchingMethods = CreateHashMap(s_pNotForPitchingMethodsLock);
            }
        }

        {
            SimpleReadLockHolder srlh(s_pPitchingCandidateMethodsLock);
            if (s_pPitchingCandidateMethods == nullptr)
            {
                s_pPitchingCandidateMethods = CreateHashMap(s_pPitchingCandidateMethodsLock);
                s_pPitchingCandidateSizes = CreateHashMap(s_pPitchingCandidateMethodsLock);
            }
        }

        s_JitPitchInitialized = true;
    }
}

static COUNT_T GetFullHash(MethodDesc* pMD)
{
    const char *moduleName = pMD->GetModule()->GetSimpleName();

    COUNT_T hash = HashStringA(moduleName);         // Start the hash with the Module name

    SString className, methodName, methodSig;

    pMD->GetMethodInfo(className, methodName, methodSig);

    hash = HashCOUNT_T(hash, className.Hash());   // Hash in the name of the Class name
    hash = HashCOUNT_T(hash, methodName.Hash());  // Hash in the name of the Method name
    hash = HashCOUNT_T(hash, 0xffffffff & (ULONGLONG)pMD);

    return hash;
}

bool MethodDesc::IsPitchable()
{
    if ((CLRConfig::GetConfigValue(CLRConfig::INTERNAL_JitPitchEnabled) == 0) ||
        (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_JitPitchMemThreshold) == 0))
        return FALSE;

    InitializeJitPitching();

    if (IsLCGMethod() || IsVtableMethod() || IsInterface() || IsVirtual())
        return FALSE;

    _ASSERTE(s_pNotForPitchingMethodsLock != nullptr && s_pNotForPitchingMethods != nullptr);

    {
        SimpleReadLockHolder srlh(s_pNotForPitchingMethodsLock);
        UPTR key = (UPTR)GetFullHash(this);
        MethodDesc *pFound = (MethodDesc *)s_pNotForPitchingMethods->LookupValue(key, (LPVOID)this);
        if (pFound != (MethodDesc *)INVALIDENTRY)
        {
            return FALSE;
        }
    }
    return TRUE;
}

EXTERN_C bool LookupOrCreateInNotForPitching(MethodDesc* pMD)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        THROWS;
    }
    CONTRACTL_END;

    if (pMD != nullptr && pMD->IsPitchable())
    {
        UPTR key = (UPTR)GetFullHash(pMD);

        _ASSERTE(s_pNotForPitchingMethodsLock != nullptr && s_pNotForPitchingMethods != nullptr);

        {
             SimpleReadLockHolder srlh(s_pNotForPitchingMethodsLock);
             MethodDesc *pFound = (MethodDesc *)s_pNotForPitchingMethods->LookupValue(key, (LPVOID)pMD);
             if (pFound != (MethodDesc *)INVALIDENTRY)
                 return TRUE;
        }

        {
             SimpleWriteLockHolder swlh(s_pNotForPitchingMethodsLock);
             s_pNotForPitchingMethods->InsertValue(key, (LPVOID)pMD);
        }
    }
    return FALSE;
}

static void LookupOrCreateInPitchingCandidate(MethodDesc* pMD, ULONG sizeOfCode)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        THROWS;
    }
    CONTRACTL_END;

    if (pMD == nullptr || !pMD->IsPitchable())
        return;

    PCODE prCode = pMD->GetPreImplementedCode();
    if (prCode)
        return;

    if (!pMD->HasPrecode())
        return;

    UPTR key = (UPTR)GetFullHash(pMD);

    _ASSERTE(s_pPitchingCandidateMethodsLock != nullptr && s_pPitchingCandidateMethods != nullptr);
    _ASSERTE(s_pPitchingCandidateSizes);

    {
        // Try getting an existing value first.
        SimpleReadLockHolder srlh(s_pPitchingCandidateMethodsLock);
        MethodDesc *pFound = (MethodDesc *)s_pPitchingCandidateMethods->LookupValue(key, (LPVOID)pMD);
        if (pFound != (MethodDesc *)INVALIDENTRY)
            return;
    }

    {
        SimpleWriteLockHolder swlh(s_pPitchingCandidateMethodsLock);
        s_pPitchingCandidateMethods->InsertValue(key, (LPVOID)pMD);
        s_pPitchingCandidateSizes->InsertValue(key, (LPVOID)((ULONGLONG)(sizeOfCode << 1)));
#ifdef _DEBUG
        if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_JitPitchPrintStat) != 0)
        {
            SString className, methodName, methodSig;
            pMD->GetMethodInfo(className, methodName, methodSig);

            StackScratchBuffer scratch;
            const char* szClassName = className.GetUTF8(scratch);
            const char* szMethodSig = methodSig.GetUTF8(scratch);

            printf("Candidate %lld %s :: %s %s\n",
                   sizeOfCode, szClassName, pMD->GetName(), szMethodSig);
        }
#endif
    }
}

EXTERN_C void DeleteFromPitchingCandidate(MethodDesc* pMD)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        THROWS;
    }
    CONTRACTL_END;

    if (pMD != nullptr && pMD->IsPitchable())
    {
        PCODE pCode = pMD->GetPreImplementedCode();

        if (pCode)
           return;

        _ASSERTE(s_pPitchingCandidateMethodsLock != nullptr && s_pPitchingCandidateMethods != nullptr);
        _ASSERTE(s_pPitchingCandidateSizes != nullptr);

        UPTR key = (UPTR)GetFullHash(pMD);
        {
            SimpleReadLockHolder srlh(s_pPitchingCandidateMethodsLock);
            MethodDesc *pFound = (MethodDesc *)s_pPitchingCandidateMethods->LookupValue(key, (LPVOID)pMD);
            if (pFound == (MethodDesc *)INVALIDENTRY)
                return;
        }

        {
            SimpleWriteLockHolder swlh(s_pPitchingCandidateMethodsLock);
            s_pPitchingCandidateMethods->DeleteValue(key, (LPVOID)pMD);
        }

        LPVOID pitchedBytes;
        {
            SimpleReadLockHolder srlh(s_pPitchingCandidateMethodsLock);
            pitchedBytes = s_pPitchingCandidateSizes->LookupValue(key, nullptr);
            _ASSERTE(pitchedBytes != (LPVOID)INVALIDENTRY);
        }
        {
            SimpleWriteLockHolder swlh(s_pPitchingCandidateMethodsLock);
            s_pPitchingCandidateSizes->DeleteValue(key, pitchedBytes);
        }
    }
}

EXTERN_C void MarkMethodNotPitchingCandidate(MethodDesc* pMD)
{

    DeleteFromPitchingCandidate(pMD);
    (void)LookupOrCreateInNotForPitching(pMD);
}

StackWalkAction CrawlFrameVisitor(CrawlFrame* pCf, Thread* pMdThread)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;

    MethodDesc* pMD = pCf->GetFunction();

    // Filter out methods we don't care about
    if (pMD == nullptr || !pMD->IsPitchable())
    {
        return SWA_CONTINUE;
    }

    if (s_pExecutedMethods == nullptr)
    {
        PtrHashMap *pMap = new (SystemDomain::GetGlobalLoaderAllocator()->GetLowFrequencyHeap()) PtrHashMap();
        pMap->Init(TRUE, nullptr);
        s_pExecutedMethods = pMap;
    }

    UPTR key = (UPTR)GetFullHash(pMD);
    MethodDesc *pFound = (MethodDesc *)s_pExecutedMethods->LookupValue(key, (LPVOID)pMD);
    if (pFound == (MethodDesc *)INVALIDENTRY)
    {
        s_pExecutedMethods->InsertValue(key, (LPVOID)pMD);
    }

    return SWA_CONTINUE;
}

// Visitor for stack walk callback.
StackWalkAction StackWalkCallback(CrawlFrame* pCf, VOID* data)
{
    WRAPPER_NO_CONTRACT;

    // WalkInfo* info = (WalkInfo*) data;
    return CrawlFrameVisitor(pCf, (Thread *)data);
}

static ULONGLONG s_PitchedMethodCounter = 0;
void MethodDesc::PitchNativeCode()
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    g_IBCLogger.LogMethodDescAccess(this);

    if (!IsPitchable())
        return;

    PCODE pCode = GetNativeCode();

    if (!pCode)
        return;

    _ASSERTE(HasPrecode());

    _ASSERTE(HasNativeCode());

    ++s_PitchedMethodCounter;

    if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_JitPitchMinVal) > s_PitchedMethodCounter)
    {
        return;
    }
    if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_JitPitchMaxVal) < s_PitchedMethodCounter)
    {
        return;
    }

    if (LookupOrCreateInNotForPitching(this))
        return;

    MethodTable * pMT = GetMethodTable();
    _ASSERTE(pMT != nullptr);

    CodeHeader* pCH = ((CodeHeader*)(pCode & ~1)) - 1;
    _ASSERTE(pCH->GetMethodDesc() == this);

    HostCodeHeap* pHeap = HostCodeHeap::GetCodeHeap((TADDR)pCode);
    pHeap->GetJitManager()->FreeCodeMemory(pHeap, (void*)pCode);

    ClearFlagsOnUpdate();

    _ASSERTE(HasPrecode());
    GetPrecode()->Reset();

    if (HasNativeCodeSlot())
    {
        RelativePointer<TADDR> *pRelPtr = (RelativePointer<TADDR> *)GetAddrOfNativeCodeSlot();
        pRelPtr->SetValueMaybeNull(NULL);
    }
    else
    {
#ifdef FEATURE_INTERPRETER
        SetNativeCodeInterlocked(NULL, NULL, FALSE);
#else
        SetNativeCodeInterlocked(NULL, NULL);
#endif
    }

    _ASSERTE(!HasNativeCode());

    UPTR key = (UPTR)GetFullHash(this);
    ULONGLONG pitchedBytes;
    {
        SimpleReadLockHolder srlh(s_pPitchingCandidateMethodsLock);
        pitchedBytes = (ULONGLONG)s_pPitchingCandidateSizes->LookupValue(key, nullptr);
        _ASSERTE(pitchedBytes != (ULONGLONG)INVALIDENTRY);
        if (pitchedBytes == (ULONGLONG)INVALIDENTRY)
            pitchedBytes = 0;
        s_jitPitchedBytes += (pitchedBytes >> 1);
    }
    {
        SimpleWriteLockHolder swlh(s_pPitchingCandidateMethodsLock);
        s_pPitchingCandidateMethods->DeleteValue(key, (LPVOID)this);
        if (pitchedBytes != 0)
            s_pPitchingCandidateSizes->DeleteValue(key, (LPVOID)pitchedBytes);
    }

    if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_JitPitchPrintStat) != 0)
    {
        SString className, methodName, methodSig;
        GetMethodInfo(className, methodName, methodSig);

        StackScratchBuffer scratch;
        const char* szClassName = className.GetUTF8(scratch);
        const char* szMethodSig = methodSig.GetUTF8(scratch);

        if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_JitPitchPrintStat) != 0)
        {
            printf("Pitched %lld %lld %s :: %s %s\n",
                   s_PitchedMethodCounter, pitchedBytes, szClassName, GetName(), szMethodSig);
        }
    }

    DACNotify::DoJITPitchingNotification(this);
}

EXTERN_C void CheckStacksAndPitch()
{
    if ((CLRConfig::GetConfigValue(CLRConfig::INTERNAL_JitPitchEnabled) != 0) &&
        (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_JitPitchMemThreshold) != 0) &&
        (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_JitPitchTimeInterval) == 0 ||
         ((::GetTickCount64() - s_JitPitchLastTick) > CLRConfig::GetConfigValue(CLRConfig::INTERNAL_JitPitchTimeInterval))))
    {
        SimpleReadLockHolder srlh(s_totalNCSizeLock);

        if ((s_totalNCSize - s_jitPitchedBytes) > CLRConfig::GetConfigValue(CLRConfig::INTERNAL_JitPitchMemThreshold) &&
            s_pPitchingCandidateMethods != nullptr)
        {
            EX_TRY
            {
                // Suspend the runtime.
                ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_OTHER);

                // Walk all other threads.
                Thread* pThread = nullptr;
                while ((pThread = ThreadStore::GetThreadList(pThread)) != nullptr)
                {
                    pThread->StackWalkFrames(StackWalkCallback, (VOID *)pThread, ALLOW_ASYNC_STACK_WALK);
                }

                if (s_pExecutedMethods)
                {
                    PtrHashMap::PtrIterator i = s_pPitchingCandidateMethods->begin();
                    while (!i.end())
                    {
                        MethodDesc *pMD = (MethodDesc *) i.GetValue();
                        UPTR key = (UPTR)GetFullHash(pMD);
                        MethodDesc *pFound = (MethodDesc *)s_pExecutedMethods->LookupValue(key, (LPVOID)pMD);
                        ++i;
                        if (pFound == (MethodDesc *)INVALIDENTRY)
                        {
                            pMD->PitchNativeCode();
                        }
                    }
                    s_pExecutedMethods->Clear();
                    delete s_pExecutedMethods;
                    s_pExecutedMethods = nullptr;
                    s_pPitchingCandidateMethods->Compact();
                    s_pPitchingCandidateSizes->Compact();
                }

                s_JitPitchLastTick = ::GetTickCount64();

                ThreadSuspend::RestartEE(FALSE, TRUE);
            }
            EX_CATCH
            {
            }
            EX_END_CATCH(SwallowAllExceptions);
        }
    }
}

EXTERN_C void SavePitchingCandidate(MethodDesc* pMD, ULONG sizeOfCode)
{
    if (pMD && pMD->IsPitchable() && CLRConfig::GetConfigValue(CLRConfig::INTERNAL_JitPitchMethodSizeThreshold) < sizeOfCode)
    {
        LookupOrCreateInPitchingCandidate(pMD, sizeOfCode);
    }
    if (sizeOfCode > 0)
    {
        SimpleWriteLockHolder swlh(s_totalNCSizeLock);
        s_totalNCSize += sizeOfCode;
        if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_JitPitchPrintStat) != 0)
            printf("jitted %lld (bytes) pitched %lld (bytes)\n", s_totalNCSize, s_jitPitchedBytes);
    }
}
#endif

#endif
