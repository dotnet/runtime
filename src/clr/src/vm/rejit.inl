// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: REJIT.INL
//

// 
// Inline definitions of various items declared in REJIT.H\
// ===========================================================================
#ifndef _REJIT_INL_
#define _REJIT_INL_

#ifdef FEATURE_REJIT

inline SharedReJitInfo::InternalFlags SharedReJitInfo::GetState()
{
    LIMITED_METHOD_CONTRACT;

    return (InternalFlags)(m_dwInternalFlags & kStateMask); 
}

inline ReJitInfo::ReJitInfo(PTR_MethodDesc pMD, SharedReJitInfo * pShared) :
    m_key(pMD),
    m_pShared(pShared)
{
    LIMITED_METHOD_CONTRACT;

    CommonInit();
}

inline ReJitInfo::ReJitInfo(PTR_Module pModule, mdMethodDef methodDef, SharedReJitInfo * pShared) :
    m_key(pModule, methodDef),
    m_pShared(pShared)
{
    LIMITED_METHOD_CONTRACT;

    CommonInit();
}

inline ReJitInfo::Key::Key() :
    m_keyType(kUninitialized), 
    m_pMD(NULL),
    m_methodDef(mdTokenNil)
{
    LIMITED_METHOD_CONTRACT;
}

inline ReJitInfo::Key::Key(PTR_MethodDesc pMD) :
    m_keyType(kMethodDesc), 
    m_pMD(dac_cast<TADDR>(pMD)),
    m_methodDef(mdTokenNil)
{
    LIMITED_METHOD_CONTRACT;
}

inline ReJitInfo::Key::Key(PTR_Module pModule, mdMethodDef methodDef) :
    m_keyType(kMetadataToken), 
    m_pModule(dac_cast<TADDR>(pModule)),
    m_methodDef(methodDef)
{
    LIMITED_METHOD_CONTRACT;
}

inline ReJitInfo::Key ReJitInfo::GetKey()
{
    LIMITED_METHOD_CONTRACT;

    return m_key; 
}

inline ReJitInfo::InternalFlags ReJitInfo::GetState()
{
    LIMITED_METHOD_CONTRACT;

    return (InternalFlags)(m_dwInternalFlags & kStateMask); 
}

inline PTR_MethodDesc ReJitInfo::GetMethodDesc()
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(m_key.m_keyType == Key::kMethodDesc);
    return PTR_MethodDesc(m_key.m_pMD);
}

inline void ReJitInfo::GetModuleAndToken(Module ** ppModule, mdMethodDef * pMethodDef)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(ppModule != NULL);
    _ASSERTE(pMethodDef != NULL);
    _ASSERTE(m_key.m_keyType == Key::kMetadataToken);

    *ppModule = PTR_Module(m_key.m_pModule);
    *pMethodDef = (mdMethodDef) m_key.m_methodDef;
}

#ifdef _DEBUG
inline BOOL ReJitInfo::CodeIsSaved()
{
    LIMITED_METHOD_CONTRACT;

    for (size_t i=0; i < sizeof(m_rgSavedCode); i++)
    {
        if (m_rgSavedCode[i] != 0)
            return TRUE;
    }
    return FALSE;
}
#endif //_DEBUG

// static
inline ReJitInfoTraits::key_t ReJitInfoTraits::GetKey(const element_t &e) 
{ 
    LIMITED_METHOD_CONTRACT;

    return e->GetKey(); 
}

// static 
inline BOOL ReJitInfoTraits::Equals(key_t k1, key_t k2) 
{ 
    LIMITED_METHOD_CONTRACT;
    
    // Always use the values of the TADDRs of the MethodDesc * and Module * when treating
    // them as lookup keys into the SHash.

    if (k1.m_keyType == ReJitInfo::Key::kMethodDesc)
    {
        return ((k2.m_keyType == ReJitInfo::Key::kMethodDesc) &&
            (dac_cast<TADDR>(PTR_MethodDesc(k1.m_pMD)) == 
             dac_cast<TADDR>(PTR_MethodDesc(k2.m_pMD))));
    }

    _ASSERTE(k1.m_keyType == ReJitInfo::Key::kMetadataToken);
    return ((k2.m_keyType == ReJitInfo::Key::kMetadataToken) &&
        (dac_cast<TADDR>(PTR_Module(k1.m_pModule)) == 
         dac_cast<TADDR>(PTR_Module(k2.m_pModule))) &&
        (k1.m_methodDef == k2.m_methodDef));
}

// static 
inline ReJitInfoTraits::count_t ReJitInfoTraits::Hash(key_t k) 
{ 
    LIMITED_METHOD_CONTRACT;

    return ReJitInfo::Hash(k);
}

// static
inline bool ReJitInfoTraits::IsNull(const element_t &e)
{
    LIMITED_METHOD_CONTRACT;

    return e == NULL; 
}

// static
inline void ReJitManager::InitStatic()
{
    STANDARD_VM_CONTRACT;

    s_csGlobalRequest.Init(CrstReJITGlobalRequest);
}

// static
inline BOOL ReJitManager::IsReJITEnabled()
{
    LIMITED_METHOD_CONTRACT;

    return CORProfilerEnableRejit();
}

inline ReJitManager::ReJitInfoHash::KeyIterator ReJitManager::GetBeginIterator(PTR_MethodDesc pMD) 
{
    LIMITED_METHOD_CONTRACT;
#ifndef DACCESS_COMPILE
    _ASSERTE(m_crstTable.OwnedByCurrentThread());
#endif
    return m_table.Begin(ReJitInfo::Key(pMD)); 
}

inline ReJitManager::ReJitInfoHash::KeyIterator ReJitManager::GetEndIterator(PTR_MethodDesc pMD)
{
    LIMITED_METHOD_CONTRACT;
#ifndef DACCESS_COMPILE
    _ASSERTE(m_crstTable.OwnedByCurrentThread());
#endif
    return m_table.End(ReJitInfo::Key(pMD)); 
}

inline ReJitManager::ReJitInfoHash::KeyIterator ReJitManager::GetBeginIterator(PTR_Module pModule, mdMethodDef methodDef)
{
    LIMITED_METHOD_CONTRACT;
#ifndef DACCESS_COMPILE
    _ASSERTE(m_crstTable.OwnedByCurrentThread());
#endif
    return m_table.Begin(ReJitInfo::Key(pModule, methodDef)); 
}

inline ReJitManager::ReJitInfoHash::KeyIterator ReJitManager::GetEndIterator(PTR_Module pModule, mdMethodDef methodDef) 
{
    LIMITED_METHOD_CONTRACT;
#ifndef DACCESS_COMPILE
    _ASSERTE(m_crstTable.OwnedByCurrentThread());
#endif
    return m_table.End(ReJitInfo::Key(pModule, methodDef)); 
}

#ifdef _DEBUG
inline BOOL ReJitManager::IsTableCrstOwnedByCurrentThread()
{
    LIMITED_METHOD_CONTRACT;

    return m_crstTable.OwnedByCurrentThread();
}
#endif //_DEBUG


inline HRESULT ReJitManager::MarkForReJit(
    PTR_MethodDesc pMD, 
    SharedReJitInfo * pSharedToReuse,
    ReJitManagerJumpStampBatch* pJumpStampBatch,
    CDynArray<ReJitReportErrorWorkItem> * pRejitErrors,
    /* out */ SharedReJitInfo ** ppSharedUsed)
{
    WRAPPER_NO_CONTRACT;

    return MarkForReJitHelper(pMD, NULL, mdTokenNil, pSharedToReuse, pJumpStampBatch, pRejitErrors, ppSharedUsed);
}

inline HRESULT ReJitManager::MarkForReJit(
    PTR_Module pModule, 
    mdMethodDef methodDef,
    ReJitManagerJumpStampBatch* pJumpStampBatch,
    CDynArray<ReJitReportErrorWorkItem> * pRejitErrors,
    /* out */ SharedReJitInfo ** ppSharedUsed)
{
    WRAPPER_NO_CONTRACT;

    return MarkForReJitHelper(NULL, pModule, methodDef, NULL, pJumpStampBatch, pRejitErrors, ppSharedUsed);
}

inline PTR_ReJitInfo ReJitManager::FindNonRevertedReJitInfo(PTR_Module pModule, mdMethodDef methodDef)
{
    WRAPPER_NO_CONTRACT;

    return FindNonRevertedReJitInfoHelper(NULL, pModule, methodDef);
}

inline PTR_ReJitInfo ReJitManager::FindNonRevertedReJitInfo(PTR_MethodDesc pMD)
{
    WRAPPER_NO_CONTRACT;

    return FindNonRevertedReJitInfoHelper(pMD, NULL, NULL);
}

//static
inline void ReJitManager::ReportReJITError(ReJitReportErrorWorkItem* pErrorRecord)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        CAN_TAKE_LOCK;
        MODE_ANY;
    }
    CONTRACTL_END;
    ReportReJITError(pErrorRecord->pModule, pErrorRecord->methodDef, pErrorRecord->pMethodDesc, pErrorRecord->hrStatus);
}

// static
inline void ReJitManager::ReportReJITError(Module* pModule, mdMethodDef methodDef, MethodDesc* pMD, HRESULT hrStatus)
{
#ifdef PROFILING_SUPPORTED
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        CAN_TAKE_LOCK;
        MODE_ANY;
    }
    CONTRACTL_END;

    {
        BEGIN_PIN_PROFILER(CORProfilerPresent());
        _ASSERTE(CORProfilerEnableRejit());
        {
            GCX_PREEMP();
            g_profControlBlock.pProfInterface->ReJITError(
                reinterpret_cast< ModuleID > (pModule),
                methodDef,
                reinterpret_cast< FunctionID > (pMD),
                hrStatus);
        }
        END_PIN_PROFILER();
    }
#endif // PROFILING_SUPPORTED
}

inline ReJitManager::TableLockHolder::TableLockHolder(ReJitManager * pReJitManager)
#ifdef FEATURE_REJIT
    : CrstHolder(&pReJitManager->m_crstTable)
#endif // FEATURE_REJIT
{
    WRAPPER_NO_CONTRACT;
}

#else // FEATURE_REJIT

// On architectures that don't support rejit, just keep around some do-nothing
// stubs so the rest of the VM doesn't have to be littered with #ifdef FEATURE_REJIT

// static
inline PCODE ReJitManager::DoReJitIfNecessary(PTR_MethodDesc)
{
    return NULL;
}

// static
inline BOOL ReJitManager::IsReJITEnabled()
{
    return FALSE;
}

// static
inline DWORD ReJitManager::GetCurrentReJitFlags(PTR_MethodDesc)
{
    return 0;
}

// static 
inline void ReJitManager::InitStatic()
{
}

inline ReJitManager::TableLockHolder::TableLockHolder(ReJitManager *)
{
}

#endif // FEATURE_REJIT


#endif // _REJIT_INL_
