// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: CodeVersion.cpp
//
// ===========================================================================

#include "common.h"
#include "codeversion.h"

#ifdef FEATURE_CODE_VERSIONING
#include "threadsuspend.h"
#include "methoditer.h"
#include "../debug/ee/debugger.h"
#include "../debug/ee/walker.h"
#include "../debug/ee/controller.h"
#endif // FEATURE_CODE_VERSIONING

#ifndef FEATURE_CODE_VERSIONING

//
// When not using code versioning we've got a minimal implementation of 
// NativeCodeVersion that simply wraps a MethodDesc* with no additional
// versioning information
//

NativeCodeVersion::NativeCodeVersion(const NativeCodeVersion & rhs) : m_pMethodDesc(rhs.m_pMethodDesc) {}
NativeCodeVersion::NativeCodeVersion(PTR_MethodDesc pMethod) : m_pMethodDesc(pMethod) {}
BOOL NativeCodeVersion::IsNull() const { return m_pMethodDesc == NULL; }
PTR_MethodDesc NativeCodeVersion::GetMethodDesc() const { return m_pMethodDesc; }
PCODE NativeCodeVersion::GetNativeCode() const { return m_pMethodDesc->GetNativeCode(); }
NativeCodeVersionId NativeCodeVersion::GetVersionId() const { return 0; }
// ReJITID NativeCodeVersion::GetILCodeVersionId() const; { return 0; }
// ILCodeVersion NativeCodeVersion::GetILCodeVersion() const { return ILCodeVersion(m_pMethodDesc); }
#ifndef DACCESS_COMPILE
BOOL NativeCodeVersion::SetNativeCodeInterlocked(PCODE pCode, PCODE pExpected) { return m_pMethodDesc->SetNativeCodeInterlocked(pCode, pExpected); }
#endif
bool NativeCodeVersion::operator==(const NativeCodeVersion & rhs) const { return m_pMethodDesc == rhs.m_pMethodDesc; }
bool NativeCodeVersion::operator!=(const NativeCodeVersion & rhs) const { return !operator==(rhs); }


#else // FEATURE_CODE_VERSIONING


// This HRESULT is only used as a private implementation detail. If it escapes through public APIS
// it is a bug. Corerror.xml has a comment in it reserving this value for our use but it doesn't
// appear in the public headers.

#define CORPROF_E_RUNTIME_SUSPEND_REQUIRED _HRESULT_TYPEDEF_(0x80131381L)

#ifndef DACCESS_COMPILE
NativeCodeVersionNode::NativeCodeVersionNode(
    NativeCodeVersionId id,
    MethodDesc* pMethodDesc,
    ReJITID parentId,
    NativeCodeVersion::OptimizationTier optimizationTier)
    :
    m_pNativeCode(NULL),
    m_pMethodDesc(pMethodDesc),
    m_parentId(parentId),
    m_pNextMethodDescSibling(NULL),
    m_id(id),
#ifdef FEATURE_TIERED_COMPILATION
    m_optTier(optimizationTier),
#endif
    m_flags(0)
{}
#endif

#ifdef DEBUG
BOOL NativeCodeVersionNode::LockOwnedByCurrentThread() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return GetMethodDesc()->GetCodeVersionManager()->LockOwnedByCurrentThread();
}
#endif //DEBUG

PTR_MethodDesc NativeCodeVersionNode::GetMethodDesc() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_pMethodDesc;
}

PCODE NativeCodeVersionNode::GetNativeCode() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_pNativeCode;
}

ReJITID NativeCodeVersionNode::GetILVersionId() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_parentId;
}

ILCodeVersion NativeCodeVersionNode::GetILCodeVersion() const
{
    LIMITED_METHOD_DAC_CONTRACT;
#ifdef DEBUG
    if (GetILVersionId() != 0)
    {
        _ASSERTE(LockOwnedByCurrentThread());
    }
#endif
    PTR_MethodDesc pMD = GetMethodDesc();
    return pMD->GetCodeVersionManager()->GetILCodeVersion(pMD, GetILVersionId());
}

NativeCodeVersionId NativeCodeVersionNode::GetVersionId() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_id;
}

#ifndef DACCESS_COMPILE
BOOL NativeCodeVersionNode::SetNativeCodeInterlocked(PCODE pCode, PCODE pExpected)
{
    LIMITED_METHOD_CONTRACT;
    return FastInterlockCompareExchangePointer(&m_pNativeCode,
        (TADDR&)pCode, (TADDR&)pExpected) == (TADDR&)pExpected;
}
#endif

BOOL NativeCodeVersionNode::IsActiveChildVersion() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    _ASSERTE(LockOwnedByCurrentThread());
    return (m_flags & IsActiveChildFlag) != 0;
}

#ifndef DACCESS_COMPILE
void NativeCodeVersionNode::SetActiveChildFlag(BOOL isActive)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(LockOwnedByCurrentThread());
    if (isActive)
    {
        m_flags |= IsActiveChildFlag;
    }
    else
    {
        m_flags &= ~IsActiveChildFlag;
    }
}
#endif


#ifdef FEATURE_TIERED_COMPILATION

NativeCodeVersion::OptimizationTier NativeCodeVersionNode::GetOptimizationTier() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_optTier;
}

#ifndef DACCESS_COMPILE
void NativeCodeVersionNode::SetOptimizationTier(NativeCodeVersion::OptimizationTier tier)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(tier >= m_optTier);

    m_optTier = tier;
}
#endif

#endif // FEATURE_TIERED_COMPILATION

NativeCodeVersion::NativeCodeVersion() :
    m_storageKind(StorageKind::Unknown)
{}

NativeCodeVersion::NativeCodeVersion(const NativeCodeVersion & rhs) :
    m_storageKind(rhs.m_storageKind)
{
    if(m_storageKind == StorageKind::Explicit)
    {
        m_pVersionNode = rhs.m_pVersionNode; 
    }
    else if(m_storageKind == StorageKind::Synthetic)
    {
        m_synthetic = rhs.m_synthetic;
    }
}

NativeCodeVersion::NativeCodeVersion(PTR_NativeCodeVersionNode pVersionNode) :
    m_storageKind(pVersionNode != NULL ? StorageKind::Explicit : StorageKind::Unknown),
    m_pVersionNode(pVersionNode)
{}

NativeCodeVersion::NativeCodeVersion(PTR_MethodDesc pMethod) :
    m_storageKind(pMethod != NULL ? StorageKind::Synthetic : StorageKind::Unknown)
{
    LIMITED_METHOD_DAC_CONTRACT;
    m_synthetic.m_pMethodDesc = pMethod;
}

BOOL NativeCodeVersion::IsNull() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_storageKind == StorageKind::Unknown;
}

BOOL NativeCodeVersion::IsDefaultVersion() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_storageKind == StorageKind::Synthetic;
}

PTR_MethodDesc NativeCodeVersion::GetMethodDesc() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    if (m_storageKind == StorageKind::Explicit)
    {
        return AsNode()->GetMethodDesc();
    }
    else
    {
        return m_synthetic.m_pMethodDesc;
    }
}

PCODE NativeCodeVersion::GetNativeCode() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    if (m_storageKind == StorageKind::Explicit)
    {
        return AsNode()->GetNativeCode();
    }
    else
    {
        return GetMethodDesc()->GetNativeCode();
    }
}

ReJITID NativeCodeVersion::GetILCodeVersionId() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    if (m_storageKind == StorageKind::Explicit)
    {
        return AsNode()->GetILVersionId();
    }
    else
    {
        return 0;
    }
}

ILCodeVersion NativeCodeVersion::GetILCodeVersion() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    if (m_storageKind == StorageKind::Explicit)
    {
        return AsNode()->GetILCodeVersion();
    }
    else
    {
        PTR_MethodDesc pMethod = GetMethodDesc();
        return ILCodeVersion(dac_cast<PTR_Module>(pMethod->GetModule()), pMethod->GetMemberDef());
    }
}

NativeCodeVersionId NativeCodeVersion::GetVersionId() const 
{
    LIMITED_METHOD_DAC_CONTRACT;
    if (m_storageKind == StorageKind::Explicit)
    {
        return AsNode()->GetVersionId();
    }
    else
    {
        return 0;
    }
}

#ifndef DACCESS_COMPILE
BOOL NativeCodeVersion::SetNativeCodeInterlocked(PCODE pCode, PCODE pExpected)
{
    LIMITED_METHOD_CONTRACT;
    if (m_storageKind == StorageKind::Explicit)
    {
        return AsNode()->SetNativeCodeInterlocked(pCode, pExpected);
    }
    else
    {
        return GetMethodDesc()->SetNativeCodeInterlocked(pCode, pExpected);
    }
}
#endif

BOOL NativeCodeVersion::IsActiveChildVersion() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    if (m_storageKind == StorageKind::Explicit)
    {
        return AsNode()->IsActiveChildVersion();
    }
    else
    {
        MethodDescVersioningState* pMethodVersioningState = GetMethodDescVersioningState();
        if (pMethodVersioningState == NULL)
        {
            return TRUE;
        }
        return pMethodVersioningState->IsDefaultVersionActiveChild();
    }
}

PTR_MethodDescVersioningState NativeCodeVersion::GetMethodDescVersioningState() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    PTR_MethodDesc pMethodDesc = GetMethodDesc();
    CodeVersionManager* pCodeVersionManager = pMethodDesc->GetCodeVersionManager();
    return pCodeVersionManager->GetMethodDescVersioningState(pMethodDesc);
}

#ifndef DACCESS_COMPILE
void NativeCodeVersion::SetActiveChildFlag(BOOL isActive)
{
    LIMITED_METHOD_DAC_CONTRACT;
    if (m_storageKind == StorageKind::Explicit)
    {
        AsNode()->SetActiveChildFlag(isActive);
    }
    else
    {
        MethodDescVersioningState* pMethodVersioningState = GetMethodDescVersioningState();
        pMethodVersioningState->SetDefaultVersionActiveChildFlag(isActive);
    }
}

MethodDescVersioningState* NativeCodeVersion::GetMethodDescVersioningState()
{
    LIMITED_METHOD_DAC_CONTRACT;
    MethodDesc* pMethodDesc = GetMethodDesc();
    CodeVersionManager* pCodeVersionManager = pMethodDesc->GetCodeVersionManager();
    return pCodeVersionManager->GetMethodDescVersioningState(pMethodDesc);
}
#endif

#ifdef FEATURE_TIERED_COMPILATION

NativeCodeVersion::OptimizationTier NativeCodeVersion::GetOptimizationTier() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    if (m_storageKind == StorageKind::Explicit)
    {
        return AsNode()->GetOptimizationTier();
    }
    else
    {
        return TieredCompilationManager::GetInitialOptimizationTier(GetMethodDesc());
    }
}

#ifndef DACCESS_COMPILE
void NativeCodeVersion::SetOptimizationTier(OptimizationTier tier)
{
    WRAPPER_NO_CONTRACT;
    if (m_storageKind == StorageKind::Explicit)
    {
        AsNode()->SetOptimizationTier(tier);
    }
    else
    {
        // State changes should have been made previously such that the initial tier is the new tier
        _ASSERTE(TieredCompilationManager::GetInitialOptimizationTier(GetMethodDesc()) == tier);
    }
}
#endif

#endif

PTR_NativeCodeVersionNode NativeCodeVersion::AsNode() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    if (m_storageKind == StorageKind::Explicit)
    {
        return m_pVersionNode;
    }
    else
    {
        return NULL;
    }
}

#ifndef DACCESS_COMPILE
PTR_NativeCodeVersionNode NativeCodeVersion::AsNode()
{
    LIMITED_METHOD_CONTRACT;
    if (m_storageKind == StorageKind::Explicit)
    {
        return m_pVersionNode;
    }
    else
    {
        return NULL;
    }
}
#endif

bool NativeCodeVersion::operator==(const NativeCodeVersion & rhs) const
{
    LIMITED_METHOD_DAC_CONTRACT;
    if (m_storageKind == StorageKind::Explicit)
    {
        return (rhs.m_storageKind == StorageKind::Explicit) &&
            (rhs.AsNode() == AsNode());
    }
    else if (m_storageKind == StorageKind::Synthetic)
    {
        return (rhs.m_storageKind == StorageKind::Synthetic) &&
            (m_synthetic.m_pMethodDesc == rhs.m_synthetic.m_pMethodDesc);
    }
    else
    {
        return rhs.m_storageKind == StorageKind::Unknown;
    }
}
bool NativeCodeVersion::operator!=(const NativeCodeVersion & rhs) const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return !operator==(rhs);
}

NativeCodeVersionCollection::NativeCodeVersionCollection(PTR_MethodDesc pMethodDescFilter, ILCodeVersion ilCodeFilter) :
    m_pMethodDescFilter(pMethodDescFilter),
    m_ilCodeFilter(ilCodeFilter)
{
}

NativeCodeVersionIterator NativeCodeVersionCollection::Begin()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return NativeCodeVersionIterator(this);
}
NativeCodeVersionIterator NativeCodeVersionCollection::End()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return NativeCodeVersionIterator(NULL);
}

NativeCodeVersionIterator::NativeCodeVersionIterator(NativeCodeVersionCollection* pNativeCodeVersionCollection) :
    m_stage(IterationStage::Initial),
    m_pCollection(pNativeCodeVersionCollection),
    m_pLinkedListCur(dac_cast<PTR_NativeCodeVersionNode>(nullptr))
{
    LIMITED_METHOD_DAC_CONTRACT;
    First();
}
void NativeCodeVersionIterator::First()
{
    LIMITED_METHOD_DAC_CONTRACT;
    if (m_pCollection == NULL)
    {
        m_stage = IterationStage::End;
    }
    Next();
}
void NativeCodeVersionIterator::Next()
{
    LIMITED_METHOD_DAC_CONTRACT;
    if (m_stage == IterationStage::Initial)
    {
        ILCodeVersion ilCodeFilter = m_pCollection->m_ilCodeFilter;
        m_stage = IterationStage::ImplicitCodeVersion;
        if (ilCodeFilter.IsNull() || ilCodeFilter.IsDefaultVersion())
        {
            m_cur = NativeCodeVersion(m_pCollection->m_pMethodDescFilter);
            return;
        }
    }
    if (m_stage == IterationStage::ImplicitCodeVersion)
    {
        m_stage = IterationStage::LinkedList;
        CodeVersionManager* pCodeVersionManager = m_pCollection->m_pMethodDescFilter->GetCodeVersionManager();
        MethodDescVersioningState* pMethodDescVersioningState = pCodeVersionManager->GetMethodDescVersioningState(m_pCollection->m_pMethodDescFilter);
        if (pMethodDescVersioningState == NULL)
        {
            m_pLinkedListCur = NULL;
        }
        else
        {
            ILCodeVersion ilCodeFilter = m_pCollection->m_ilCodeFilter;
            m_pLinkedListCur = pMethodDescVersioningState->GetFirstVersionNode();
            while (m_pLinkedListCur != NULL && !ilCodeFilter.IsNull() && ilCodeFilter.GetVersionId() != m_pLinkedListCur->GetILVersionId())
            {
                m_pLinkedListCur = m_pLinkedListCur->m_pNextMethodDescSibling;
            }
        }
        if (m_pLinkedListCur != NULL)
        {
            m_cur = NativeCodeVersion(m_pLinkedListCur);
            return;
        }
    }
    if (m_stage == IterationStage::LinkedList)
    {
        if (m_pLinkedListCur != NULL)
        {
            ILCodeVersion ilCodeFilter = m_pCollection->m_ilCodeFilter;
            do
            {
                m_pLinkedListCur = m_pLinkedListCur->m_pNextMethodDescSibling;
            } while (m_pLinkedListCur != NULL && !ilCodeFilter.IsNull() && ilCodeFilter.GetVersionId() != m_pLinkedListCur->GetILVersionId());
        }
        if (m_pLinkedListCur != NULL)
        {
            m_cur = NativeCodeVersion(m_pLinkedListCur);
            return;
        }
        else
        {
            m_stage = IterationStage::End;
            m_cur = NativeCodeVersion();
        }
    }
}
const NativeCodeVersion & NativeCodeVersionIterator::Get() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_cur;
}
bool NativeCodeVersionIterator::Equal(const NativeCodeVersionIterator &i) const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_cur == i.m_cur;
}

ILCodeVersionNode::ILCodeVersionNode() :
    m_pModule(dac_cast<PTR_Module>(nullptr)),
    m_methodDef(0),
    m_rejitId(0),
    m_pNextILVersionNode(dac_cast<PTR_ILCodeVersionNode>(nullptr)),
    m_rejitState(ILCodeVersion::kStateRequested),
    m_pIL(),
    m_jitFlags(0)
{
    m_pIL.Store(dac_cast<PTR_COR_ILMETHOD>(nullptr));
}

#ifndef DACCESS_COMPILE
ILCodeVersionNode::ILCodeVersionNode(Module* pModule, mdMethodDef methodDef, ReJITID id) :
    m_pModule(pModule),
    m_methodDef(methodDef),
    m_rejitId(id),
    m_pNextILVersionNode(dac_cast<PTR_ILCodeVersionNode>(nullptr)),
    m_rejitState(ILCodeVersion::kStateRequested),
    m_pIL(nullptr),
    m_jitFlags(0)
{}
#endif

#ifdef DEBUG
BOOL ILCodeVersionNode::LockOwnedByCurrentThread() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return GetModule()->GetCodeVersionManager()->LockOwnedByCurrentThread();
}
#endif //DEBUG

PTR_Module ILCodeVersionNode::GetModule() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_pModule;
}

mdMethodDef ILCodeVersionNode::GetMethodDef() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_methodDef;
}

ReJITID ILCodeVersionNode::GetVersionId() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_rejitId;
}

ILCodeVersion::RejitFlags ILCodeVersionNode::GetRejitState() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return static_cast<ILCodeVersion::RejitFlags>(m_rejitState.Load() & ILCodeVersion::kStateMask);
}

BOOL ILCodeVersionNode::GetEnableReJITCallback() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    return (m_rejitState.Load() & ILCodeVersion::kSuppressParams) == ILCodeVersion::kSuppressParams;
}

PTR_COR_ILMETHOD ILCodeVersionNode::GetIL() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return dac_cast<PTR_COR_ILMETHOD>(m_pIL.Load());
}

DWORD ILCodeVersionNode::GetJitFlags() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_jitFlags.Load();
}

const InstrumentedILOffsetMapping* ILCodeVersionNode::GetInstrumentedILMap() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    _ASSERTE(LockOwnedByCurrentThread());
    return &m_instrumentedILMap;
}

PTR_ILCodeVersionNode ILCodeVersionNode::GetNextILVersionNode() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    _ASSERTE(LockOwnedByCurrentThread());
    return m_pNextILVersionNode;
}

#ifndef DACCESS_COMPILE
void ILCodeVersionNode::SetRejitState(ILCodeVersion::RejitFlags newState)
{
    LIMITED_METHOD_CONTRACT;
    // We're doing a non thread safe modification to m_rejitState
    _ASSERTE(LockOwnedByCurrentThread());

    ILCodeVersion::RejitFlags oldNonMaskFlags = 
        static_cast<ILCodeVersion::RejitFlags>(m_rejitState.Load() & ~ILCodeVersion::kStateMask);
    m_rejitState.Store(static_cast<ILCodeVersion::RejitFlags>(newState | oldNonMaskFlags));
}

void ILCodeVersionNode::SetEnableReJITCallback(BOOL state)
{
    LIMITED_METHOD_CONTRACT;
    // We're doing a non thread safe modification to m_rejitState
    _ASSERTE(LockOwnedByCurrentThread());

    ILCodeVersion::RejitFlags oldFlags = m_rejitState.Load();
    if (state)
    {
        m_rejitState.Store(static_cast<ILCodeVersion::RejitFlags>(oldFlags | ILCodeVersion::kSuppressParams));
    }
    else
    {
        m_rejitState.Store(static_cast<ILCodeVersion::RejitFlags>(oldFlags & ~ILCodeVersion::kSuppressParams));
    }
}

void ILCodeVersionNode::SetIL(COR_ILMETHOD* pIL)
{
    LIMITED_METHOD_CONTRACT;
    m_pIL.Store(pIL);
}

void ILCodeVersionNode::SetJitFlags(DWORD flags)
{
    LIMITED_METHOD_CONTRACT;
    m_jitFlags.Store(flags);
}

void ILCodeVersionNode::SetInstrumentedILMap(SIZE_T cMap, COR_IL_MAP * rgMap)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(LockOwnedByCurrentThread());
    m_instrumentedILMap.SetMappingInfo(cMap, rgMap);
}

void ILCodeVersionNode::SetNextILVersionNode(ILCodeVersionNode* pNextILVersionNode)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(LockOwnedByCurrentThread());
    m_pNextILVersionNode = pNextILVersionNode;
}
#endif

ILCodeVersion::ILCodeVersion() :
    m_storageKind(StorageKind::Unknown)
{}

ILCodeVersion::ILCodeVersion(const ILCodeVersion & ilCodeVersion) :
    m_storageKind(ilCodeVersion.m_storageKind)
{
    if(m_storageKind == StorageKind::Explicit)
    {
        m_pVersionNode = ilCodeVersion.m_pVersionNode;
    }
    else if(m_storageKind == StorageKind::Synthetic)
    {
        m_synthetic = ilCodeVersion.m_synthetic;
    }
}

ILCodeVersion::ILCodeVersion(PTR_ILCodeVersionNode pILCodeVersionNode) :
    m_storageKind(pILCodeVersionNode != NULL ? StorageKind::Explicit : StorageKind::Unknown),
    m_pVersionNode(pILCodeVersionNode)
{}

ILCodeVersion::ILCodeVersion(PTR_Module pModule, mdMethodDef methodDef) :
    m_storageKind(pModule != NULL ? StorageKind::Synthetic : StorageKind::Unknown)
{
    LIMITED_METHOD_DAC_CONTRACT;
    m_synthetic.m_pModule = pModule;
    m_synthetic.m_methodDef = methodDef;
}

bool ILCodeVersion::operator==(const ILCodeVersion & rhs) const
{
    LIMITED_METHOD_DAC_CONTRACT;
    if (m_storageKind == StorageKind::Explicit)
    {
        return (rhs.m_storageKind == StorageKind::Explicit) &&
            (AsNode() == rhs.AsNode());
    }
    else if (m_storageKind == StorageKind::Synthetic)
    {
        return (rhs.m_storageKind == StorageKind::Synthetic) &&
            (m_synthetic.m_pModule == rhs.m_synthetic.m_pModule) &&
            (m_synthetic.m_methodDef == rhs.m_synthetic.m_methodDef);
    }
    else
    {
        return rhs.m_storageKind == StorageKind::Unknown;
    }
}

BOOL ILCodeVersion::HasDefaultIL() const
{
    LIMITED_METHOD_CONTRACT;

    return (m_storageKind == StorageKind::Synthetic) || (AsNode()->GetIL() == NULL);
}

BOOL ILCodeVersion::IsNull() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_storageKind == StorageKind::Unknown;
}

BOOL ILCodeVersion::IsDefaultVersion() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_storageKind == StorageKind::Synthetic;
}

PTR_Module ILCodeVersion::GetModule() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    if (m_storageKind == StorageKind::Explicit)
    {
        return AsNode()->GetModule();
    }
    else
    {
        return m_synthetic.m_pModule;
    }
}

mdMethodDef ILCodeVersion::GetMethodDef() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    if (m_storageKind == StorageKind::Explicit)
    {
        return AsNode()->GetMethodDef();
    }
    else
    {
        return m_synthetic.m_methodDef;
    }
}

ReJITID ILCodeVersion::GetVersionId() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    if (m_storageKind == StorageKind::Explicit)
    {
        return AsNode()->GetVersionId();
    }
    else
    {
        return 0;
    }
}

NativeCodeVersionCollection ILCodeVersion::GetNativeCodeVersions(PTR_MethodDesc pClosedMethodDesc) const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return NativeCodeVersionCollection(pClosedMethodDesc, *this);
}

NativeCodeVersion ILCodeVersion::GetActiveNativeCodeVersion(PTR_MethodDesc pClosedMethodDesc) const
{
    LIMITED_METHOD_DAC_CONTRACT;
    NativeCodeVersionCollection versions = GetNativeCodeVersions(pClosedMethodDesc);
    for (NativeCodeVersionIterator cur = versions.Begin(), end = versions.End(); cur != end; cur++)
    {
        if (cur->IsActiveChildVersion())
        {
            return *cur;
        }
    }
    return NativeCodeVersion();
}

ILCodeVersion::RejitFlags ILCodeVersion::GetRejitState() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    if (m_storageKind == StorageKind::Explicit)
    {
        return AsNode()->GetRejitState();
    }
    else
    {
        return ILCodeVersion::kStateActive;
    }
}

BOOL ILCodeVersion::GetEnableReJITCallback() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    if (m_storageKind == StorageKind::Explicit)
    {
        return AsNode()->GetEnableReJITCallback();
    }
    else
    {
        return FALSE;
    }
}

PTR_COR_ILMETHOD ILCodeVersion::GetIL() const
{
    CONTRACTL
    {
        THROWS; //GetILHeader throws
        GC_NOTRIGGER;
        FORBID_FAULT;
        MODE_ANY;
    }
    CONTRACTL_END

    PTR_COR_ILMETHOD pIL = NULL;
    if (m_storageKind == StorageKind::Explicit)
    {
        pIL = AsNode()->GetIL();
    }
    
    // For the default code version we always fetch the globally stored default IL for a method
    //
    // In the non-default code version we assume NULL is the equivalent of explicitly requesting to
    // re-use the default IL. Ideally there would be no reason to create a new version that re-uses
    // the default IL (just use the default code version for that) but we do it here for compat. We've 
    // got some profilers that use ReJIT to create a new code version and then instead of calling
    // ICorProfilerFunctionControl::SetILFunctionBody they call ICorProfilerInfo::SetILFunctionBody. 
    // This mutates the default IL so that it is now correct for their new code version. Of course this
    // also overwrote the previous default IL so now the default code version GetIL() is out of sync
    // with the jitted code. In the majority of cases we never re-read the IL after the initial
    // jitting so this issue goes unnoticed.
    //
    // If changing the default IL after it is in use becomes more problematic in the future we would
    // need to add enforcement that prevents profilers from using ICorProfilerInfo::SetILFunctionBody
    // that way + coordinate with them because it is a breaking change for any profiler currently doing it.
    if(pIL == NULL)
    {
        PTR_Module pModule = GetModule();
        PTR_MethodDesc pMethodDesc = dac_cast<PTR_MethodDesc>(pModule->LookupMethodDef(GetMethodDef()));
        if (pMethodDesc != NULL)
        {
            pIL = dac_cast<PTR_COR_ILMETHOD>(pMethodDesc->GetILHeader(TRUE));
        }
    }

    return pIL;
}

PTR_COR_ILMETHOD ILCodeVersion::GetILNoThrow() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    PTR_COR_ILMETHOD ret;
    EX_TRY
    {
        ret = GetIL();
    }
    EX_CATCH
    {
        ret = NULL;
    }
    EX_END_CATCH(RethrowTerminalExceptions);
    return ret;
}

DWORD ILCodeVersion::GetJitFlags() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    if (m_storageKind == StorageKind::Explicit)
    {
        return AsNode()->GetJitFlags();
    }
    else
    {
        return 0;
    }
}

const InstrumentedILOffsetMapping* ILCodeVersion::GetInstrumentedILMap() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    if (m_storageKind == StorageKind::Explicit)
    {
        return AsNode()->GetInstrumentedILMap();
    }
    else
    {
        return NULL;
    }
}

#ifndef DACCESS_COMPILE
void ILCodeVersion::SetRejitState(RejitFlags newState)
{
    LIMITED_METHOD_CONTRACT;
    AsNode()->SetRejitState(newState);
}

void ILCodeVersion::SetEnableReJITCallback(BOOL state)
{
    LIMITED_METHOD_CONTRACT;
    return AsNode()->SetEnableReJITCallback(state);
}

void ILCodeVersion::SetIL(COR_ILMETHOD* pIL)
{
    LIMITED_METHOD_CONTRACT;
    AsNode()->SetIL(pIL);
}

void ILCodeVersion::SetJitFlags(DWORD flags)
{
    LIMITED_METHOD_CONTRACT;
    AsNode()->SetJitFlags(flags);
}

void ILCodeVersion::SetInstrumentedILMap(SIZE_T cMap, COR_IL_MAP * rgMap)
{
    LIMITED_METHOD_CONTRACT;
    AsNode()->SetInstrumentedILMap(cMap, rgMap);
}

HRESULT ILCodeVersion::AddNativeCodeVersion(
    MethodDesc* pClosedMethodDesc,
    NativeCodeVersion::OptimizationTier optimizationTier,
    NativeCodeVersion* pNativeCodeVersion)
{
    LIMITED_METHOD_CONTRACT;
    CodeVersionManager* pManager = GetModule()->GetCodeVersionManager();
    HRESULT hr = pManager->AddNativeCodeVersion(*this, pClosedMethodDesc, optimizationTier, pNativeCodeVersion);
    if (FAILED(hr))
    {
        _ASSERTE(hr == E_OUTOFMEMORY);
        return hr;
    }
    return S_OK;
}

HRESULT ILCodeVersion::GetOrCreateActiveNativeCodeVersion(MethodDesc* pClosedMethodDesc, NativeCodeVersion* pActiveNativeCodeVersion)
{
    LIMITED_METHOD_CONTRACT;
    HRESULT hr = S_OK;
    NativeCodeVersion activeNativeChild = GetActiveNativeCodeVersion(pClosedMethodDesc);
    if (activeNativeChild.IsNull())
    {
        NativeCodeVersion::OptimizationTier optimizationTier =
            TieredCompilationManager::GetInitialOptimizationTier(pClosedMethodDesc);
        if (FAILED(hr = AddNativeCodeVersion(pClosedMethodDesc, optimizationTier, &activeNativeChild)))
        {
            _ASSERTE(hr == E_OUTOFMEMORY);
            return hr;
        }
    }
    // The first added child should automatically become active
    _ASSERTE(GetActiveNativeCodeVersion(pClosedMethodDesc) == activeNativeChild);
    *pActiveNativeCodeVersion = activeNativeChild;
    return S_OK;
}

HRESULT ILCodeVersion::SetActiveNativeCodeVersion(NativeCodeVersion activeNativeCodeVersion, BOOL fEESuspended)
{
    LIMITED_METHOD_CONTRACT;
    HRESULT hr = S_OK;
    MethodDesc* pMethodDesc = activeNativeCodeVersion.GetMethodDesc();
    NativeCodeVersion prevActiveVersion = GetActiveNativeCodeVersion(pMethodDesc);
    if (prevActiveVersion == activeNativeCodeVersion)
    {
        //nothing to do, this version is already active
        return S_OK;
    }

    if (!prevActiveVersion.IsNull())
    {
        prevActiveVersion.SetActiveChildFlag(FALSE);
    }
    activeNativeCodeVersion.SetActiveChildFlag(TRUE);

    // If needed update the published code body for this method
    CodeVersionManager* pCodeVersionManager = GetModule()->GetCodeVersionManager();
    if (pCodeVersionManager->GetActiveILCodeVersion(GetModule(), GetMethodDef()) == *this)
    {
        if (FAILED(hr = pCodeVersionManager->PublishNativeCodeVersion(pMethodDesc, activeNativeCodeVersion, fEESuspended)))
        {
            return hr;
        }
    }

    return S_OK;
}

ILCodeVersionNode* ILCodeVersion::AsNode()
{
    LIMITED_METHOD_CONTRACT;
    //This is dangerous - NativeCodeVersion coerces non-explicit versions to NULL but ILCodeVersion assumes the caller
    //will never invoke AsNode() on a non-explicit node. Asserting for now as a minimal fix, but we should revisit this.
    _ASSERTE(m_storageKind == StorageKind::Explicit);
    return m_pVersionNode;
}
#endif //DACCESS_COMPILE

PTR_ILCodeVersionNode ILCodeVersion::AsNode() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    //This is dangerous - NativeCodeVersion coerces non-explicit versions to NULL but ILCodeVersion assumes the caller
    //will never invoke AsNode() on a non-explicit node. Asserting for now as a minimal fix, but we should revisit this.
    _ASSERTE(m_storageKind == StorageKind::Explicit);
    return m_pVersionNode;
}

ILCodeVersionCollection::ILCodeVersionCollection(PTR_Module pModule, mdMethodDef methodDef) :
    m_pModule(pModule),
    m_methodDef(methodDef)
{}

ILCodeVersionIterator ILCodeVersionCollection::Begin()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return ILCodeVersionIterator(this);
}

ILCodeVersionIterator ILCodeVersionCollection::End()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return ILCodeVersionIterator(NULL);
}

ILCodeVersionIterator::ILCodeVersionIterator(const ILCodeVersionIterator & iter) :
    m_stage(iter.m_stage),
    m_cur(iter.m_cur),
    m_pLinkedListCur(iter.m_pLinkedListCur),
    m_pCollection(iter.m_pCollection)
{}

ILCodeVersionIterator::ILCodeVersionIterator(ILCodeVersionCollection* pCollection) :
    m_stage(pCollection != NULL ? IterationStage::Initial : IterationStage::End),
    m_pLinkedListCur(dac_cast<PTR_ILCodeVersionNode>(nullptr)),
    m_pCollection(pCollection)
{
    LIMITED_METHOD_DAC_CONTRACT;
    First();
}

const ILCodeVersion & ILCodeVersionIterator::Get() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_cur;
}

void ILCodeVersionIterator::First()
{
    LIMITED_METHOD_DAC_CONTRACT;
    Next();
}

void ILCodeVersionIterator::Next()
{
    LIMITED_METHOD_DAC_CONTRACT;
    if (m_stage == IterationStage::Initial)
    {
        m_stage = IterationStage::ImplicitCodeVersion;
        m_cur = ILCodeVersion(m_pCollection->m_pModule, m_pCollection->m_methodDef);
        return;
    }
    if (m_stage == IterationStage::ImplicitCodeVersion)
    {
        CodeVersionManager* pCodeVersionManager = m_pCollection->m_pModule->GetCodeVersionManager();
        _ASSERTE(pCodeVersionManager->LockOwnedByCurrentThread());
        PTR_ILCodeVersioningState pILCodeVersioningState = pCodeVersionManager->GetILCodeVersioningState(m_pCollection->m_pModule, m_pCollection->m_methodDef);
        if (pILCodeVersioningState != NULL)
        {
            m_pLinkedListCur = pILCodeVersioningState->GetFirstVersionNode();
        }
        m_stage = IterationStage::LinkedList;
        if (m_pLinkedListCur != NULL)
        {
            m_cur = ILCodeVersion(m_pLinkedListCur);
            return;
        }
    }
    if (m_stage == IterationStage::LinkedList)
    {
        if (m_pLinkedListCur != NULL)
        {
            m_pLinkedListCur = m_pLinkedListCur->GetNextILVersionNode();
        }
        if (m_pLinkedListCur != NULL)
        {
            m_cur = ILCodeVersion(m_pLinkedListCur);
            return;
        }
        else
        {
            m_stage = IterationStage::End;
            m_cur = ILCodeVersion();
            return;
        }
    }
}

bool ILCodeVersionIterator::Equal(const ILCodeVersionIterator &i) const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_cur == i.m_cur;
}

MethodDescVersioningState::MethodDescVersioningState(PTR_MethodDesc pMethodDesc) :
    m_pMethodDesc(pMethodDesc),
    m_flags(IsDefaultVersionActiveChildFlag),
    m_nextId(1),
    m_pFirstVersionNode(dac_cast<PTR_NativeCodeVersionNode>(nullptr))
{
    LIMITED_METHOD_DAC_CONTRACT;
#ifdef FEATURE_JUMPSTAMP
    ZeroMemory(m_rgSavedCode, JumpStubSize);
#endif
}

PTR_MethodDesc MethodDescVersioningState::GetMethodDesc() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_pMethodDesc;
}

#ifndef DACCESS_COMPILE
NativeCodeVersionId MethodDescVersioningState::AllocateVersionId()
{
    LIMITED_METHOD_CONTRACT;
    return m_nextId++;
}
#endif

PTR_NativeCodeVersionNode MethodDescVersioningState::GetFirstVersionNode() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_pFirstVersionNode;
}

#ifdef FEATURE_JUMPSTAMP
MethodDescVersioningState::JumpStampFlags MethodDescVersioningState::GetJumpStampState()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return (JumpStampFlags)(m_flags & JumpStampMask);
}

#ifndef DACCESS_COMPILE
void MethodDescVersioningState::SetJumpStampState(JumpStampFlags newState)
{
    LIMITED_METHOD_CONTRACT;
    m_flags = (m_flags & ~JumpStampMask) | (BYTE)newState;
}
#endif // DACCESS_COMPILE

#ifndef DACCESS_COMPILE
HRESULT MethodDescVersioningState::SyncJumpStamp(NativeCodeVersion nativeCodeVersion, BOOL fEESuspended)
 {
    LIMITED_METHOD_CONTRACT;
    HRESULT hr = S_OK;
    PCODE pCode = nativeCodeVersion.IsNull() ? NULL : nativeCodeVersion.GetNativeCode();
    MethodDesc* pMethod = GetMethodDesc();
    _ASSERTE(pMethod->IsVersionableWithJumpStamp());

    if (!pMethod->HasNativeCode())
    {
        //we'll set up the jump-stamp when the default native code is created
        return S_OK;
    }

    if (!nativeCodeVersion.IsNull() && nativeCodeVersion.IsDefaultVersion())
    {
        return UndoJumpStampNativeCode(fEESuspended);
    }
    else
    {
        // We don't have new code ready yet, jumpstamp back to the prestub to let us generate it the next time
        // the method is called
        if (pCode == NULL)
        {
            if (!fEESuspended)
            {
                return CORPROF_E_RUNTIME_SUSPEND_REQUIRED;
            }
            return JumpStampNativeCode();
        }
        // We do know the new code body, install the jump stamp now
        else
        {
            return UpdateJumpTarget(fEESuspended, pCode);
        }
    }
}
#endif // DACCESS_COMPILE

//---------------------------------------------------------------------------------------
//
// Simple, thin abstraction of debugger breakpoint patching. Given an address and a
// previously procured DebuggerControllerPatch governing the code address, this decides
// whether the code address is patched. If so, it returns a pointer to the debugger's
// buffer (of what's "underneath" the int 3 patch); otherwise, it returns the code
// address itself.
//
// Arguments:
//      * pbCode - Code address to return if unpatched
//      * dbgpatch - DebuggerControllerPatch to test
//
// Return Value:
//      Either pbCode or the debugger's patch buffer, as per description above.
//
// Assumptions:
//      Caller must manually grab (and hold) the ControllerLockHolder and get the
//      DebuggerControllerPatch before calling this helper.
//      
// Notes:
//     pbCode need not equal the code address governed by dbgpatch, but is always
//     "related" (and sometimes really is equal). For example, this helper may be used
//     when writing a code byte to an internal rejit buffer (e.g., in preparation for an
//     eventual 64-bit interlocked write into the code stream), and thus pbCode would
//     point into the internal rejit buffer whereas dbgpatch governs the corresponding
//     code byte in the live code stream. This function would then be used to determine
//     whether a byte should be written into the internal rejit buffer OR into the
//     debugger controller's breakpoint buffer.
//

LPBYTE FirstCodeByteAddr(LPBYTE pbCode, DebuggerControllerPatch * dbgpatch)
{
    LIMITED_METHOD_CONTRACT;

    if (dbgpatch != NULL && dbgpatch->IsActivated())
    {
        // Debugger has patched the code, so return the address of the buffer
        return LPBYTE(&(dbgpatch->opcode));
    }

    // no active patch, just return the direct code address
    return pbCode;
}


#ifdef _DEBUG
#ifndef DACCESS_COMPILE
BOOL MethodDescVersioningState::CodeIsSaved()
{
    LIMITED_METHOD_CONTRACT;

    for (size_t i = 0; i < sizeof(m_rgSavedCode); i++)
    {
        if (m_rgSavedCode[i] != 0)
            return TRUE;
    }
    return FALSE;
}
#endif //DACCESS_COMPILE
#endif //_DEBUG

//---------------------------------------------------------------------------------------
//
// Do the actual work of stamping the top of originally-jitted-code with a jmp that goes
// to the prestub. This can be called in one of three ways:
//     * Case 1: By RequestReJIT against an already-jitted function, in which case the
//         PCODE may be inferred by the MethodDesc, and our caller will have suspended
//         the EE for us, OR
//     * Case 2: By the prestub worker after jitting the original code of a function
//         (i.e., the "pre-rejit" scenario). In this case, the EE is not suspended. But
//         that's ok, because the PCODE has not yet been published to the MethodDesc, and
//         no thread can be executing inside the originally JITted function yet.
//     * Case 3: At type/method restore time for an NGEN'ed assembly. This is also the pre-rejit
//         scenario because we are guaranteed to do this before the code in the module
//         is executable. EE suspend is not required.
//
// Arguments:
//    * pCode - Case 1 (above): will be NULL, and we can infer the PCODE from the
//        MethodDesc; Case 2+3 (above, pre-rejit): will be non-NULL, and we'll need to use
//        this to find the code to stamp on top of.
//
// Return Value:
//    * S_OK: Either we successfully did the jmp-stamp, or a racing thread took care of
//        it for us.
//    * Else, HRESULT indicating failure.
//
// Assumptions:
//     The caller will have suspended the EE if necessary (case 1), before this is
//     called.
//
#ifndef DACCESS_COMPILE
HRESULT MethodDescVersioningState::JumpStampNativeCode(PCODE pCode /* = NULL */)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        // It may seem dangerous to be stamping jumps over code while a GC is going on,
        // but we're actually safe. As we assert below, either we're holding the thread
        // store lock (and thus preventing a GC) OR we're stamping code that has not yet
        // been published (and will thus not be executed by managed therads or examined
        // by the GC).
        MODE_ANY;
    }
    CONTRACTL_END;

    PCODE pCodePublished = GetMethodDesc()->GetNativeCode();

    _ASSERTE((pCode != NULL) || (pCodePublished != NULL));
    _ASSERTE(GetMethodDesc()->GetCodeVersionManager()->LockOwnedByCurrentThread());

    HRESULT hr = S_OK;

    // We'll jump-stamp over pCode, or if pCode is NULL, jump-stamp over the published
    // code for this's MethodDesc.
    LPBYTE pbCode = (LPBYTE)pCode;
    if (pbCode == NULL)
    {
        // If caller didn't specify a pCode, just use the one that was published after
        // the original JIT.  (A specific pCode would be passed in the pre-rejit case,
        // to jump-stamp the original code BEFORE the PCODE gets published.)
        pbCode = (LPBYTE)pCodePublished;
    }
    _ASSERTE(pbCode != NULL);

    // The debugging API may also try to write to the very top of this function (though
    // with an int 3 for breakpoint purposes). Coordinate with the debugger so we know
    // whether we can safely patch the actual code, or instead write to the debugger's
    // buffer.
    DebuggerController::ControllerLockHolder lockController;

    if (GetJumpStampState() == JumpStampToPrestub)
    {
        // The method has already been jump stamped so nothing left to do
        _ASSERTE(CodeIsSaved());
        return S_OK;
    }

    // Remember what we're stamping our jump on top of, so we can replace it during a
    // revert.
    if (GetJumpStampState() == JumpStampNone)
    {
        for (unsigned int i = 0; i < sizeof(m_rgSavedCode); i++)
        {
            m_rgSavedCode[i] = *FirstCodeByteAddr(pbCode + i, DebuggerController::GetPatchTable()->GetPatch((CORDB_ADDRESS_TYPE *)(pbCode + i)));
        }
    }

    EX_TRY
    {
        AllocMemTracker amt;

        // This guy might throw on out-of-memory, so rely on the tracker to clean-up
        Precode * pPrecode = Precode::Allocate(PRECODE_STUB, GetMethodDesc(), GetMethodDesc()->GetLoaderAllocator(), &amt);
        PCODE target = pPrecode->GetEntryPoint();

#if defined(_X86_) || defined(_AMD64_)

        // Normal unpatched code never starts with a jump
        _ASSERTE(GetJumpStampState() == JumpStampToActiveVersion ||
            *FirstCodeByteAddr(pbCode, DebuggerController::GetPatchTable()->GetPatch((CORDB_ADDRESS_TYPE *)pbCode)) != X86_INSTR_JMP_REL32);

        INT64 i64OldCode = *(INT64*)pbCode;
        INT64 i64NewCode = i64OldCode;
        LPBYTE pbNewValue = (LPBYTE)&i64NewCode;
        *pbNewValue = X86_INSTR_JMP_REL32;
        INT32 UNALIGNED * pOffset = reinterpret_cast<INT32 UNALIGNED *>(&pbNewValue[1]);
        // This will throw for out-of-memory, so don't write anything until
        // after he succeeds
        // This guy will leak/cache/reuse the jumpstub
        *pOffset = rel32UsingJumpStub(reinterpret_cast<INT32 UNALIGNED *>(pbCode + 1), target, GetMethodDesc(), GetMethodDesc()->GetLoaderAllocator());

        // If we have the EE suspended or the code is unpublished there won't be contention on this code
        hr = UpdateJumpStampHelper(pbCode, i64OldCode, i64NewCode, FALSE);
        if (FAILED(hr))
        {
            ThrowHR(hr);
        }

        //
        // No failure point after this!
        //
        amt.SuppressRelease();

#else // _X86_ || _AMD64_
#error "Need to define a way to jump-stamp the prolog in a safe way for this platform"
#endif // _X86_ || _AMD64_

        SetJumpStampState(JumpStampToPrestub);
    }
    EX_CATCH_HRESULT(hr);
    _ASSERT(hr == S_OK || hr == E_OUTOFMEMORY);

    if (SUCCEEDED(hr))
    {
        _ASSERTE(GetJumpStampState() == JumpStampToPrestub);
        _ASSERTE(m_rgSavedCode[0] != 0); // saved code should not start with 0
    }

    return hr;
}


//---------------------------------------------------------------------------------------
//
// After code has been rejitted, this is called to update the jump-stamp to go from
// pointing to the prestub, to pointing to the newly rejitted code.
//
// Arguments:
//     fEESuspended - TRUE if the caller keeps the EE suspended during this call
//     pRejittedCode - jitted code for the updated IL this method should execute
//
// Assumptions:
//      This rejit manager's table crst should be held by the caller
//
// Returns - S_OK if the jump target is updated
//           CORPROF_E_RUNTIME_SUSPEND_REQUIRED if the ee isn't suspended and it
//             will need to be in order to do the update safely
HRESULT MethodDescVersioningState::UpdateJumpTarget(BOOL fEESuspended, PCODE pRejittedCode)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    MethodDesc * pMD = GetMethodDesc();
    _ASSERTE(pMD->GetCodeVersionManager()->LockOwnedByCurrentThread());

    // It isn't safe to overwrite the original method prolog with a jmp because threads might
    // be at an IP in the middle of the jump stamp already. However converting between different
    // jump stamps is OK (when done atomically) because this only changes the jmp target, not
    // instruction boundaries.
    if (GetJumpStampState() == JumpStampNone && !fEESuspended)
    {
        return CORPROF_E_RUNTIME_SUSPEND_REQUIRED;
    }

    // Beginning of originally JITted code containing the jmp that we will redirect.
    BYTE * pbCode = (BYTE*)pMD->GetNativeCode();

    // Remember what we're stamping our jump on top of, so we can replace it during a
    // revert.
    if (GetJumpStampState() == JumpStampNone)
    {
        for (unsigned int i = 0; i < sizeof(m_rgSavedCode); i++)
        {
            m_rgSavedCode[i] = *FirstCodeByteAddr(pbCode + i, DebuggerController::GetPatchTable()->GetPatch((CORDB_ADDRESS_TYPE *)(pbCode + i)));
        }
    }

#if defined(_X86_) || defined(_AMD64_)

    HRESULT hr = S_OK;
    {
        DebuggerController::ControllerLockHolder lockController;

        // This will throw for out-of-memory, so don't write anything until
        // after he succeeds
        // This guy will leak/cache/reuse the jumpstub
        INT32 offset = 0;
        EX_TRY
        {
            offset = rel32UsingJumpStub(
            reinterpret_cast<INT32 UNALIGNED *>(&pbCode[1]),    // base of offset
            pRejittedCode,                                      // target of jump
            pMD,
            pMD->GetLoaderAllocator());
        }
        EX_CATCH_HRESULT(hr);
        _ASSERT(hr == S_OK || hr == E_OUTOFMEMORY);
        if (FAILED(hr))
        {
            return hr;
        }
        // For validation later, remember what pbCode is right now
        INT64 i64OldValue = *(INT64 *)pbCode;

        // Assemble the INT64 of the new code bytes to write.  Start with what's there now
        INT64 i64NewValue = i64OldValue;
        LPBYTE pbNewValue = (LPBYTE)&i64NewValue;

        // First byte becomes a rel32 jmp instruction (if it wasn't already)
        *pbNewValue = X86_INSTR_JMP_REL32;
        // Next 4 bytes are the jmp target (offset to jmp stub)
        INT32 UNALIGNED * pnOffset = reinterpret_cast<INT32 UNALIGNED *>(&pbNewValue[1]);
        *pnOffset = offset;

        hr = UpdateJumpStampHelper(pbCode, i64OldValue, i64NewValue, !fEESuspended);
        _ASSERTE(hr == S_OK || (hr == CORPROF_E_RUNTIME_SUSPEND_REQUIRED && !fEESuspended));
    }
    if (FAILED(hr))
    {
        return hr;
    }

#else // _X86_ || _AMD64_
#error "Need to define a way to jump-stamp the prolog in a safe way for this platform"
#endif // _X86_ || _AMD64_

    // State transition
    SetJumpStampState(JumpStampToActiveVersion);
    return S_OK;
}


//---------------------------------------------------------------------------------------
//
// Poke the JITted code to satsify a revert request (or to perform an implicit revert as
// part of a second, third, etc. rejit request). Reinstates the originally JITted code
// that had been jump-stamped over to perform a prior rejit.
//
// Arguments
//     fEESuspended - TRUE if the caller keeps the EE suspended during this call
//
//
// Return Value:
//     S_OK to indicate the revert succeeded,
//     CORPROF_E_RUNTIME_SUSPEND_REQUIRED to indicate the jumpstamp hasn't been reverted
//       and EE suspension will be needed for success
//     other failure HRESULT indicating what went wrong.
//
// Assumptions:
//     Caller must be holding the owning ReJitManager's table crst.
//
HRESULT MethodDescVersioningState::UndoJumpStampNativeCode(BOOL fEESuspended)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(GetMethodDesc()->GetCodeVersionManager()->LockOwnedByCurrentThread());
    if (GetJumpStampState() == JumpStampNone)
    {
        return S_OK;
    }

    _ASSERTE(m_rgSavedCode[0] != 0); // saved code should not start with 0

    BYTE * pbCode = (BYTE*)GetMethodDesc()->GetNativeCode();
    DebuggerController::ControllerLockHolder lockController;

#if defined(_X86_) || defined(_AMD64_)
    _ASSERTE(m_rgSavedCode[0] != X86_INSTR_JMP_REL32);
    _ASSERTE(*FirstCodeByteAddr(pbCode, DebuggerController::GetPatchTable()->GetPatch((CORDB_ADDRESS_TYPE *)pbCode)) == X86_INSTR_JMP_REL32);
#else
#error "Need to define a way to jump-stamp the prolog in a safe way for this platform"
#endif // _X86_ || _AMD64_

    // For the interlocked compare, remember what pbCode is right now
    INT64 i64OldValue = *(INT64 *)pbCode;
    // Assemble the INT64 of the new code bytes to write.  Start with what's there now
    INT64 i64NewValue = i64OldValue;
    memcpy(LPBYTE(&i64NewValue), m_rgSavedCode, sizeof(m_rgSavedCode));
    HRESULT hr = UpdateJumpStampHelper(pbCode, i64OldValue, i64NewValue, !fEESuspended);
    _ASSERTE(hr == S_OK || (hr == CORPROF_E_RUNTIME_SUSPEND_REQUIRED && !fEESuspended));
    if (hr != S_OK)
        return hr;

    // Transition state of this ReJitInfo to indicate the MD no longer has any jump stamp
    SetJumpStampState(JumpStampNone);
    return S_OK;
}
#endif

//---------------------------------------------------------------------------------------
//
// This is called to modify the jump-stamp area, the first ReJitInfo::JumpStubSize bytes
// in the method's code. 
//
// Notes:
//      Callers use this method in a variety of circumstances:
//      a) when the code is unpublished (fContentionPossible == FALSE)
//      b) when the caller has taken the ThreadStoreLock and suspended the EE 
//         (fContentionPossible == FALSE)
//      c) when the code is published, the EE isn't suspended, and the jumpstamp
//         area consists of a single 5 byte long jump instruction
//         (fContentionPossible == TRUE)
//      This method will attempt to alter the jump-stamp even if the caller has not prevented
//      contention, but there is no guarantee it will be succesful. When the caller has prevented
//      contention, then success is assured. Callers may opportunistically try without
//      EE suspension, and then upgrade to EE suspension if the first attempt fails. 
//
// Assumptions:
//      This rejit manager's table crst should be held by the caller or fContentionPossible==FALSE
//      The debugger patch table lock should be held by the caller
//
// Arguments:
//      pbCode - pointer to the code where the jump stamp is placed
//      i64OldValue - the bytes which should currently be at the start of the method code
//      i64NewValue - the new bytes which should be written at the start of the method code
//      fContentionPossible - See the Notes section above.
//
// Returns:
//      S_OK => the jumpstamp has been succesfully updated.
//      CORPROF_E_RUNTIME_SUSPEND_REQUIRED => the jumpstamp remains unchanged (preventing contention will be necessary)
//      other failing HR => VirtualProtect failed, the jumpstamp remains unchanged
//
#ifndef DACCESS_COMPILE
HRESULT MethodDescVersioningState::UpdateJumpStampHelper(BYTE* pbCode, INT64 i64OldValue, INT64 i64NewValue, BOOL fContentionPossible)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    MethodDesc * pMD = GetMethodDesc();
    _ASSERTE(pMD->GetCodeVersionManager()->LockOwnedByCurrentThread() || !fContentionPossible);

    // When ReJIT is enabled, method entrypoints are always at least 8-byte aligned (see
    // code:EEJitManager::allocCode), so we can do a single 64-bit interlocked operation
    // to update the jump target.  However, some code may have gotten compiled before
    // the profiler had a chance to enable ReJIT (e.g., NGENd code, or code JITted
    // before a profiler attaches).  In such cases, we cannot rely on a simple
    // interlocked operation, and instead must suspend the runtime to ensure we can
    // safely update the jmp instruction.
    //
    // This method doesn't verify that the method is actually safe to rejit, we expect
    // callers to do that. At the moment NGEN'ed code is safe to rejit even if
    // it is unaligned, but code generated before the profiler attaches is not.
    if (fContentionPossible && !(IS_ALIGNED(pbCode, sizeof(INT64))))
    {
        return CORPROF_E_RUNTIME_SUSPEND_REQUIRED;
    }

    // The debugging API may also try to write to this function (though
    // with an int 3 for breakpoint purposes). Coordinate with the debugger so we know
    // whether we can safely patch the actual code, or instead write to the debugger's
    // buffer.
    if (fContentionPossible)
    {
        for (CORDB_ADDRESS_TYPE* pbProbeAddr = pbCode; pbProbeAddr < pbCode + MethodDescVersioningState::JumpStubSize; pbProbeAddr++)
        {
            if (NULL != DebuggerController::GetPatchTable()->GetPatch(pbProbeAddr))
            {
                return CORPROF_E_RUNTIME_SUSPEND_REQUIRED;
            }
        }
    }

#if defined(_X86_) || defined(_AMD64_)

    DWORD oldProt;
    if (!ClrVirtualProtect((LPVOID)pbCode, 8, PAGE_EXECUTE_READWRITE, &oldProt))
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    if (fContentionPossible)
    {
        INT64 i64InterlockReportedOldValue = FastInterlockCompareExchangeLong((INT64 *)pbCode, i64NewValue, i64OldValue);
        // Since changes to these bytes are protected by this rejitmgr's m_crstTable, we
        // shouldn't have two writers conflicting.
        _ASSERTE(i64InterlockReportedOldValue == i64OldValue);
    }
    else
    {
        // In this path the caller ensures:
        //   a) no thread will execute through the prologue area we are modifying
        //   b) no thread is stopped in a prologue such that it resumes in the middle of code we are modifying
        //   c) no thread is doing a debugger patch skip operation in which an unmodified copy of the method's 
        //      code could be executed from a patch skip buffer.

        // PERF: we might still want a faster path through here if we aren't debugging that doesn't do
        // all the patch checks
        for (unsigned int i = 0; i < MethodDescVersioningState::JumpStubSize; i++)
        {
            *FirstCodeByteAddr(pbCode + i, DebuggerController::GetPatchTable()->GetPatch(pbCode + i)) = ((BYTE*)&i64NewValue)[i];
        }
    }

    if (oldProt != PAGE_EXECUTE_READWRITE)
    {
        // The CLR codebase in many locations simply ignores failures to restore the page protections
        // Its true that it isn't a problem functionally, but it seems a bit sketchy?
        // I am following the convention for now.
        ClrVirtualProtect((LPVOID)pbCode, 8, oldProt, &oldProt);
    }

    FlushInstructionCache(GetCurrentProcess(), pbCode, MethodDescVersioningState::JumpStubSize);
    return S_OK;

#else // _X86_ || _AMD64_
#error "Need to define a way to jump-stamp the prolog in a safe way for this platform"
#endif // _X86_ || _AMD64_
}
#endif
#endif // FEATURE_JUMPSTAMP

BOOL MethodDescVersioningState::IsDefaultVersionActiveChild() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return (m_flags & IsDefaultVersionActiveChildFlag) != 0;
}
#ifndef DACCESS_COMPILE
void MethodDescVersioningState::SetDefaultVersionActiveChildFlag(BOOL isActive)
{
    LIMITED_METHOD_CONTRACT;
    if (isActive)
    {
        m_flags |= IsDefaultVersionActiveChildFlag;
    }
    else
    {
        m_flags &= ~IsDefaultVersionActiveChildFlag;
    }
}

void MethodDescVersioningState::LinkNativeCodeVersionNode(NativeCodeVersionNode* pNativeCodeVersionNode)
{
    LIMITED_METHOD_CONTRACT;
    pNativeCodeVersionNode->m_pNextMethodDescSibling = m_pFirstVersionNode;
    m_pFirstVersionNode = pNativeCodeVersionNode;
}
#endif

ILCodeVersioningState::ILCodeVersioningState(PTR_Module pModule, mdMethodDef methodDef) :
    m_activeVersion(ILCodeVersion(pModule,methodDef)),
    m_pFirstVersionNode(dac_cast<PTR_ILCodeVersionNode>(nullptr)),
    m_pModule(pModule),
    m_methodDef(methodDef)
{}


ILCodeVersioningState::Key::Key() :
    m_pModule(dac_cast<PTR_Module>(nullptr)),
    m_methodDef(0)
{}

ILCodeVersioningState::Key::Key(PTR_Module pModule, mdMethodDef methodDef) :
    m_pModule(pModule),
    m_methodDef(methodDef)
{}

size_t ILCodeVersioningState::Key::Hash() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return (size_t)(dac_cast<TADDR>(m_pModule) ^ m_methodDef);
}

bool ILCodeVersioningState::Key::operator==(const Key & rhs) const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return (m_pModule == rhs.m_pModule) && (m_methodDef == rhs.m_methodDef);
}

ILCodeVersioningState::Key ILCodeVersioningState::GetKey() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return Key(m_pModule, m_methodDef);
}

ILCodeVersion ILCodeVersioningState::GetActiveVersion() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_activeVersion;
}

PTR_ILCodeVersionNode ILCodeVersioningState::GetFirstVersionNode() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_pFirstVersionNode;
}

#ifndef DACCESS_COMPILE
void ILCodeVersioningState::SetActiveVersion(ILCodeVersion ilActiveCodeVersion)
{
    LIMITED_METHOD_CONTRACT;
    m_activeVersion = ilActiveCodeVersion;
}

void ILCodeVersioningState::LinkILCodeVersionNode(ILCodeVersionNode* pILCodeVersionNode)
{
    LIMITED_METHOD_CONTRACT;
    pILCodeVersionNode->SetNextILVersionNode(m_pFirstVersionNode);
    m_pFirstVersionNode = pILCodeVersionNode;
}
#endif

CodeVersionManager::CodeVersionManager()
{}

//---------------------------------------------------------------------------------------
//
// Called from BaseDomain::BaseDomain to do any constructor-time initialization.
// Presently, this takes care of initializing the Crst.
//

void CodeVersionManager::PreInit()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        CAN_TAKE_LOCK;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifndef DACCESS_COMPILE
    m_crstTable.Init(
        CrstReJITDomainTable,
        CrstFlags(CRST_UNSAFE_ANYMODE | CRST_DEBUGGER_THREAD | CRST_REENTRANCY | CRST_TAKEN_DURING_SHUTDOWN));
#endif // DACCESS_COMPILE
}

CodeVersionManager::TableLockHolder::TableLockHolder(CodeVersionManager* pCodeVersionManager) :
    CrstHolder(&pCodeVersionManager->m_crstTable)
{
}
#ifndef DACCESS_COMPILE
void CodeVersionManager::EnterLock()
{
    m_crstTable.Enter();
}
void CodeVersionManager::LeaveLock()
{
    m_crstTable.Leave();
}
#endif

#ifdef DEBUG
BOOL CodeVersionManager::LockOwnedByCurrentThread() const
{
    LIMITED_METHOD_DAC_CONTRACT;
#ifdef DACCESS_COMPILE
    return TRUE;
#else
    return const_cast<CrstExplicitInit &>(m_crstTable).OwnedByCurrentThread();
#endif
}
#endif

PTR_ILCodeVersioningState CodeVersionManager::GetILCodeVersioningState(PTR_Module pModule, mdMethodDef methodDef) const
{
    LIMITED_METHOD_DAC_CONTRACT;
    ILCodeVersioningState::Key key = ILCodeVersioningState::Key(pModule, methodDef);
    return m_ilCodeVersioningStateMap.Lookup(key);
}

PTR_MethodDescVersioningState CodeVersionManager::GetMethodDescVersioningState(PTR_MethodDesc pClosedMethodDesc) const
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_methodDescVersioningStateMap.Lookup(pClosedMethodDesc);
}

#ifndef DACCESS_COMPILE
HRESULT CodeVersionManager::GetOrCreateILCodeVersioningState(Module* pModule, mdMethodDef methodDef, ILCodeVersioningState** ppILCodeVersioningState)
{
    LIMITED_METHOD_CONTRACT;
    HRESULT hr = S_OK;
    ILCodeVersioningState* pILCodeVersioningState = GetILCodeVersioningState(pModule, methodDef);
    if (pILCodeVersioningState == NULL)
    {
        pILCodeVersioningState = new (nothrow) ILCodeVersioningState(pModule, methodDef);
        if (pILCodeVersioningState == NULL)
        {
            return E_OUTOFMEMORY;
        }
        EX_TRY
        {
            // This throws when out of memory, but remains internally
            // consistent (without adding the new element)
            m_ilCodeVersioningStateMap.Add(pILCodeVersioningState);
        }
        EX_CATCH_HRESULT(hr);
        if (FAILED(hr))
        {
            delete pILCodeVersioningState;
            return hr;
        }
    }
    *ppILCodeVersioningState = pILCodeVersioningState;
    return S_OK;
}

HRESULT CodeVersionManager::GetOrCreateMethodDescVersioningState(MethodDesc* pMethod, MethodDescVersioningState** ppMethodVersioningState)
{
    LIMITED_METHOD_CONTRACT;
    HRESULT hr = S_OK;
    MethodDescVersioningState* pMethodVersioningState = m_methodDescVersioningStateMap.Lookup(pMethod);
    if (pMethodVersioningState == NULL)
    {
        pMethodVersioningState = new (nothrow) MethodDescVersioningState(pMethod);
        if (pMethodVersioningState == NULL)
        {
            return E_OUTOFMEMORY;
        }
        EX_TRY
        {
            // This throws when out of memory, but remains internally
            // consistent (without adding the new element)
            m_methodDescVersioningStateMap.Add(pMethodVersioningState);
        }
        EX_CATCH_HRESULT(hr);
        if (FAILED(hr))
        {
            delete pMethodVersioningState;
            return hr;
        }
    }
    *ppMethodVersioningState = pMethodVersioningState;
    return S_OK;
}
#endif // DACCESS_COMPILE

DWORD CodeVersionManager::GetNonDefaultILVersionCount()
{
    LIMITED_METHOD_DAC_CONTRACT;

    //This function is legal to call WITHOUT taking the lock
    //It is used to do a quick check if work might be needed without paying the overhead
    //of acquiring the lock and doing dictionary lookups
    return m_ilCodeVersioningStateMap.GetCount();
}

ILCodeVersionCollection CodeVersionManager::GetILCodeVersions(PTR_MethodDesc pMethod)
{
    LIMITED_METHOD_DAC_CONTRACT;
    _ASSERTE(LockOwnedByCurrentThread());
    return GetILCodeVersions(dac_cast<PTR_Module>(pMethod->GetModule()), pMethod->GetMemberDef());
}

ILCodeVersionCollection CodeVersionManager::GetILCodeVersions(PTR_Module pModule, mdMethodDef methodDef)
{
    LIMITED_METHOD_DAC_CONTRACT;
    _ASSERTE(LockOwnedByCurrentThread());
    return ILCodeVersionCollection(pModule, methodDef);
}

ILCodeVersion CodeVersionManager::GetActiveILCodeVersion(PTR_MethodDesc pMethod)
{
    LIMITED_METHOD_DAC_CONTRACT;
    _ASSERTE(LockOwnedByCurrentThread());
    return GetActiveILCodeVersion(dac_cast<PTR_Module>(pMethod->GetModule()), pMethod->GetMemberDef());
}

ILCodeVersion CodeVersionManager::GetActiveILCodeVersion(PTR_Module pModule, mdMethodDef methodDef)
{
    LIMITED_METHOD_DAC_CONTRACT;
    _ASSERTE(LockOwnedByCurrentThread());
    ILCodeVersioningState* pILCodeVersioningState = GetILCodeVersioningState(pModule, methodDef);
    if (pILCodeVersioningState == NULL)
    {
        return ILCodeVersion(pModule, methodDef);
    }
    else
    {
        return pILCodeVersioningState->GetActiveVersion();
    }
}

ILCodeVersion CodeVersionManager::GetILCodeVersion(PTR_MethodDesc pMethod, ReJITID rejitId)
{
    LIMITED_METHOD_DAC_CONTRACT;
    _ASSERTE(LockOwnedByCurrentThread());

#ifdef FEATURE_REJIT
    ILCodeVersionCollection collection = GetILCodeVersions(pMethod);
    for (ILCodeVersionIterator cur = collection.Begin(), end = collection.End(); cur != end; cur++)
    {
        if (cur->GetVersionId() == rejitId)
        {
            return *cur;
        }
    }
    return ILCodeVersion();
#else // FEATURE_REJIT
    _ASSERTE(rejitId == 0);
    return ILCodeVersion(dac_cast<PTR_Module>(pMethod->GetModule()), pMethod->GetMemberDef());
#endif // FEATURE_REJIT
}

NativeCodeVersionCollection CodeVersionManager::GetNativeCodeVersions(PTR_MethodDesc pMethod) const
{
    LIMITED_METHOD_DAC_CONTRACT;
    _ASSERTE(LockOwnedByCurrentThread());
    return NativeCodeVersionCollection(pMethod, ILCodeVersion());
}

NativeCodeVersion CodeVersionManager::GetNativeCodeVersion(PTR_MethodDesc pMethod, PCODE codeStartAddress) const
{
    LIMITED_METHOD_DAC_CONTRACT;
    _ASSERTE(LockOwnedByCurrentThread());

    NativeCodeVersionCollection nativeCodeVersions = GetNativeCodeVersions(pMethod);
    for (NativeCodeVersionIterator cur = nativeCodeVersions.Begin(), end = nativeCodeVersions.End(); cur != end; cur++)
    {
        if (cur->GetNativeCode() == codeStartAddress)
        {
            return *cur;
        }
    }
    return NativeCodeVersion();
}

#ifndef DACCESS_COMPILE
HRESULT CodeVersionManager::AddILCodeVersion(Module* pModule, mdMethodDef methodDef, ReJITID rejitId, ILCodeVersion* pILCodeVersion)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(LockOwnedByCurrentThread());

    ILCodeVersioningState* pILCodeVersioningState;
    HRESULT hr = GetOrCreateILCodeVersioningState(pModule, methodDef, &pILCodeVersioningState);
    if (FAILED(hr))
    {
        _ASSERTE(hr == E_OUTOFMEMORY);
        return hr;
    }

    ILCodeVersionNode* pILCodeVersionNode = new (nothrow) ILCodeVersionNode(pModule, methodDef, rejitId);
    if (pILCodeVersionNode == NULL)
    {
        return E_OUTOFMEMORY;
    }
    pILCodeVersioningState->LinkILCodeVersionNode(pILCodeVersionNode);
    *pILCodeVersion = ILCodeVersion(pILCodeVersionNode);
    return S_OK;
}

HRESULT CodeVersionManager::SetActiveILCodeVersions(ILCodeVersion* pActiveVersions, DWORD cActiveVersions, BOOL fEESuspended, CDynArray<CodePublishError> * pErrors)
{
    // If the IL version is in the shared domain we need to iterate all domains
    // looking for instantiations. The domain iterator lock is bigger than
    // the code version manager lock so we can't do this atomically. In one atomic
    // update the bookkeeping for IL versioning will happen and then in a second
    // update the active native code versions will change/code jumpstamps+precodes
    // will update.
    //
    // Note: For all domains other than the shared AppDomain we could do this
    // atomically, but for now we use the lowest common denominator for all
    // domains.
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
        PRECONDITION(CheckPointer(pActiveVersions));
        PRECONDITION(CheckPointer(pErrors, NULL_OK));
    }
    CONTRACTL_END;
    _ASSERTE(!LockOwnedByCurrentThread());
    HRESULT hr = S_OK;

#if DEBUG
    for (DWORD i = 0; i < cActiveVersions; i++)
    {
        ILCodeVersion activeVersion = pActiveVersions[i];
        if (activeVersion.IsNull())
        {
            _ASSERTE(!"The active IL version can't be NULL");
        }
    }
#endif

    // step 1 - mark the IL versions as being active, this ensures that
    // any new method instantiations added after this point will bind to
    // the correct version
    {
        TableLockHolder(this);
        for (DWORD i = 0; i < cActiveVersions; i++)
        {
            ILCodeVersion activeVersion = pActiveVersions[i];
            ILCodeVersioningState* pILCodeVersioningState = NULL;
            if (FAILED(hr = GetOrCreateILCodeVersioningState(activeVersion.GetModule(), activeVersion.GetMethodDef(), &pILCodeVersioningState)))
            {
                _ASSERTE(hr == E_OUTOFMEMORY);
                return hr;
            }
            pILCodeVersioningState->SetActiveVersion(activeVersion);
        }
    }

    // step 2 - determine the set of pre-existing method instantiations

    // a parallel array to activeVersions
    // for each ILCodeVersion in activeVersions, this lists the set
    // MethodDescs that will need to be updated
    CDynArray<CDynArray<MethodDesc*>> methodDescsToUpdate;
    CDynArray<CodePublishError> errorRecords;
    for (DWORD i = 0; i < cActiveVersions; i++)
    {
        CDynArray<MethodDesc*>* pMethodDescs = methodDescsToUpdate.Append();
        if (pMethodDescs == NULL)
        {
            return E_OUTOFMEMORY;
        }
        *pMethodDescs = CDynArray<MethodDesc*>();

        MethodDesc* pLoadedMethodDesc = pActiveVersions[i].GetModule()->LookupMethodDef(pActiveVersions[i].GetMethodDef());
        if (FAILED(hr = CodeVersionManager::EnumerateClosedMethodDescs(pLoadedMethodDesc, pMethodDescs, &errorRecords)))
        {
            _ASSERTE(hr == E_OUTOFMEMORY);
            return hr;
        }
    }

    // step 3 - update each pre-existing method instantiation
    {
        TableLockHolder lock(this);
        for (DWORD i = 0; i < cActiveVersions; i++)
        {
            // Its possible the active IL version has changed if
            // another caller made an update while this method wasn't
            // holding the lock. We will ensure that we synchronize
            // publishing to whatever version is currently active, even
            // if that isn't the IL version we set above.
            //
            // Note: Although we attempt to handle this case gracefully
            // it isn't recommended for callers to do this. Racing two calls
            // that set the IL version to different results means it will be
            // completely arbitrary which version wins.
            ILCodeVersion requestedActiveILVersion = pActiveVersions[i];
            ILCodeVersion activeILVersion = GetActiveILCodeVersion(requestedActiveILVersion.GetModule(), requestedActiveILVersion.GetMethodDef());

            CDynArray<MethodDesc*> methodDescs = methodDescsToUpdate[i];
            for (int j = 0; j < methodDescs.Count(); j++)
            {
                // Get an the active child code version for this method instantiation (it might be NULL, that is OK)
                NativeCodeVersion activeNativeChild = activeILVersion.GetActiveNativeCodeVersion(methodDescs[j]);

                // Publish that child version, because it is the active native child of the active IL version
                // Failing to publish is non-fatal, but we do record it so the caller is aware
                if (FAILED(hr = PublishNativeCodeVersion(methodDescs[j], activeNativeChild, fEESuspended)))
                {
                    if (FAILED(hr = AddCodePublishError(activeILVersion.GetModule(), activeILVersion.GetMethodDef(), methodDescs[j], hr, &errorRecords)))
                    {
                        _ASSERTE(hr == E_OUTOFMEMORY);
                        return hr;
                    }
                }
            }
        }
    }

    return S_OK;
}

HRESULT CodeVersionManager::AddNativeCodeVersion(
    ILCodeVersion ilCodeVersion,
    MethodDesc* pClosedMethodDesc,
    NativeCodeVersion::OptimizationTier optimizationTier,
    NativeCodeVersion* pNativeCodeVersion)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(LockOwnedByCurrentThread());

    MethodDescVersioningState* pMethodVersioningState;
    HRESULT hr = GetOrCreateMethodDescVersioningState(pClosedMethodDesc, &pMethodVersioningState);
    if (FAILED(hr))
    {
        _ASSERTE(hr == E_OUTOFMEMORY);
        return hr;
    }

    NativeCodeVersionId newId = pMethodVersioningState->AllocateVersionId();
    NativeCodeVersionNode* pNativeCodeVersionNode = new (nothrow) NativeCodeVersionNode(newId, pClosedMethodDesc, ilCodeVersion.GetVersionId(), optimizationTier);
    if (pNativeCodeVersionNode == NULL)
    {
        return E_OUTOFMEMORY;
    }

    pMethodVersioningState->LinkNativeCodeVersionNode(pNativeCodeVersionNode);

    // the first child added is automatically considered the active one.
    if (ilCodeVersion.GetActiveNativeCodeVersion(pClosedMethodDesc).IsNull())
    {
        pNativeCodeVersionNode->SetActiveChildFlag(TRUE);
        _ASSERTE(!ilCodeVersion.GetActiveNativeCodeVersion(pClosedMethodDesc).IsNull());

        // the new child shouldn't have any native code. If it did we might need to
        // publish that code as part of adding the node which would require callers
        // to pay attention to GC suspension and we'd need to report publishing errors
        // back to them.
        _ASSERTE(pNativeCodeVersionNode->GetNativeCode() == NULL);
    }
    *pNativeCodeVersion = NativeCodeVersion(pNativeCodeVersionNode);
    return S_OK;
}

PCODE CodeVersionManager::PublishVersionableCodeIfNecessary(MethodDesc* pMethodDesc, BOOL fCanBackpatchPrestub)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(!LockOwnedByCurrentThread());
    _ASSERTE(pMethodDesc->IsVersionable());
    _ASSERTE(!pMethodDesc->IsPointingToPrestub() || !pMethodDesc->IsVersionableWithJumpStamp());

    HRESULT hr = S_OK;
    PCODE pCode = NULL;

    NativeCodeVersion activeVersion;
    {
        TableLockHolder lock(this);
        if (FAILED(hr = GetActiveILCodeVersion(pMethodDesc).GetOrCreateActiveNativeCodeVersion(pMethodDesc, &activeVersion)))
        {
            _ASSERTE(hr == E_OUTOFMEMORY);
            ReportCodePublishError(pMethodDesc->GetModule(), pMethodDesc->GetMemberDef(), pMethodDesc, hr);
            return NULL;
        }
    }

    BOOL fEESuspend = FALSE;
    while (true)
    {
        // compile the code if needed
        pCode = activeVersion.GetNativeCode();
        if (pCode == NULL)
        {
            pCode = pMethodDesc->PrepareCode(activeVersion);
        }

        MethodDescBackpatchInfoTracker::ConditionalLockHolder lockHolder(pMethodDesc->MayHaveEntryPointSlotsToBackpatch());

        // suspend in preparation for publishing if needed
        if (fEESuspend)
        {
            ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_FOR_REJIT);
        }

        {
            TableLockHolder lock(this);
            // The common case is that newActiveCode == activeCode, however we did leave the lock so there is
            // possibility that the active version has changed. If it has we need to restart the compilation
            // and publishing process with the new active version instead.
            //
            // In theory it should be legitimate to break out of this loop and run the less recent active version,
            // because ultimately this is a race between one thread that is updating the version and another thread
            // trying to run the current version. However for back-compat with ReJIT we need to guarantee that
            // a versioning update at least as late as the profiler JitCompilationFinished callback wins the race.
            NativeCodeVersion newActiveVersion;
            if (FAILED(hr = GetActiveILCodeVersion(pMethodDesc).GetOrCreateActiveNativeCodeVersion(pMethodDesc, &newActiveVersion)))
            {
                _ASSERTE(hr == E_OUTOFMEMORY);
                ReportCodePublishError(pMethodDesc->GetModule(), pMethodDesc->GetMemberDef(), pMethodDesc, hr);
                pCode = NULL;
                break;
            }
            if (newActiveVersion != activeVersion)
            {
                activeVersion = newActiveVersion;
            }
            else
            {
                // if we aren't allowed to backpatch we are done
                if (!fCanBackpatchPrestub)
                {
                    break;
                }

                // attempt to publish the active version still under the lock
                if (FAILED(hr = PublishNativeCodeVersion(pMethodDesc, activeVersion, fEESuspend)))
                {
                    // If we need an EESuspend to publish then start over. We have to leave the lock in order to suspend,
                    // and when we leave the lock the active version might change again. However now we know that suspend is
                    // necessary.
                    if (hr == CORPROF_E_RUNTIME_SUSPEND_REQUIRED)
                    {
                        _ASSERTE(!fEESuspend);
                        fEESuspend = true;
                        continue; // skip RestartEE() below since SuspendEE() has not been called yet
                    }
                    else
                    {
                        ReportCodePublishError(pMethodDesc->GetModule(), pMethodDesc->GetMemberDef(), pMethodDesc, hr);
                        pCode = NULL;
                        break;
                    }
                }
                else
                {
                    //success
                    break;
                }
            }
        } // exit lock

        if (fEESuspend)
        {
            ThreadSuspend::RestartEE(FALSE, TRUE);
        }
    }
    
    // if the EE is still suspended from breaking in the middle of the loop, resume it
    if (fEESuspend)
    {
        ThreadSuspend::RestartEE(FALSE, TRUE);
    }
    return pCode;
}

HRESULT CodeVersionManager::PublishNativeCodeVersion(MethodDesc* pMethod, NativeCodeVersion nativeCodeVersion, BOOL fEESuspended)
{
    // TODO: This function needs to make sure it does not change the precode's target if call counting is in progress. Track
    // whether call counting is currently being done for the method, and use a lock to ensure the expected precode target.
    WRAPPER_NO_CONTRACT;
    _ASSERTE(LockOwnedByCurrentThread());
    _ASSERTE(pMethod->IsVersionable());
    HRESULT hr = S_OK;
    PCODE pCode = nativeCodeVersion.IsNull() ? NULL : nativeCodeVersion.GetNativeCode();
    if (pMethod->IsVersionableWithoutJumpStamp())
    {
        EX_TRY
        {
            if (pCode == NULL)
            {
                pMethod->ResetCodeEntryPoint();
            }
            else
            {
                pMethod->SetCodeEntryPoint(pCode);
            }
        }
        EX_CATCH_HRESULT(hr);
        return hr;
    }
    else
    {
#ifndef FEATURE_JUMPSTAMP
        _ASSERTE(!"This platform doesn't support JumpStamp but this method doesn't version with Precode,"
            " this method can't be updated");
        return E_FAIL;
#else
        MethodDescVersioningState* pVersioningState;
        if (FAILED(hr = GetOrCreateMethodDescVersioningState(pMethod, &pVersioningState)))
        {
            _ASSERTE(hr == E_OUTOFMEMORY);
            return hr;
        }
        return pVersioningState->SyncJumpStamp(nativeCodeVersion, fEESuspended);
#endif
    }
}

// static
HRESULT CodeVersionManager::EnumerateClosedMethodDescs(
    MethodDesc* pMD,
    CDynArray<MethodDesc*> * pClosedMethodDescs,
    CDynArray<CodePublishError> * pUnsupportedMethodErrors)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
        PRECONDITION(CheckPointer(pMD, NULL_OK));
        PRECONDITION(CheckPointer(pClosedMethodDescs));
        PRECONDITION(CheckPointer(pUnsupportedMethodErrors));
    }
    CONTRACTL_END;
    HRESULT hr = S_OK;
    if (pMD == NULL)
    {
        // nothing is loaded yet so we're done for this method.
        return S_OK;
    }

    if (!pMD->HasClassOrMethodInstantiation())
    {
        // We have a JITted non-generic.
        MethodDesc ** ppMD = pClosedMethodDescs->Append();
        if (ppMD == NULL)
        {
            return E_OUTOFMEMORY;
        }
        *ppMD = pMD;
    }

    if (!pMD->HasClassOrMethodInstantiation())
    {
        // not generic, we're done for this method
        return S_OK;
    }

    // Ok, now the case of a generic function (or function on generic class), which
    // is loaded, and may thus have compiled instantiations.
    // It's impossible to get to any other kind of domain from the profiling API
    Module* pModule = pMD->GetModule();
    mdMethodDef methodDef = pMD->GetMemberDef();
    BaseDomain * pBaseDomainFromModule = pModule->GetDomain();
    _ASSERTE(pBaseDomainFromModule->IsAppDomain() ||
        pBaseDomainFromModule->IsSharedDomain());

    if (pBaseDomainFromModule->IsSharedDomain())
    {
        // Iterate through all modules loaded into the shared domain, to
        // find all instantiations living in the shared domain. This will
        // include orphaned code (i.e., shared code used by ADs that have
        // all unloaded), which is good, because orphaned code could get
        // re-adopted if a new AD is created that can use that shared code
        hr = EnumerateDomainClosedMethodDescs(
            NULL,  // NULL means to search SharedDomain instead of an AD
            pModule,
            methodDef,
            pClosedMethodDescs,
            pUnsupportedMethodErrors);
    }
    else
    {
        // Module is unshared, so just use the module's domain to find instantiations.
        hr = EnumerateDomainClosedMethodDescs(
            pBaseDomainFromModule->AsAppDomain(),
            pModule,
            methodDef,
            pClosedMethodDescs,
            pUnsupportedMethodErrors);
    }
    if (FAILED(hr))
    {
        _ASSERTE(hr == E_OUTOFMEMORY);
        return hr;
    }

    // We want to iterate through all compilations of existing instantiations to
    // ensure they get marked for rejit.  Note: There may be zero instantiations,
    // but we won't know until we try.
    if (pBaseDomainFromModule->IsSharedDomain())
    {
        // Iterate through all real domains, to find shared instantiations.
        AppDomainIterator appDomainIterator(TRUE);
        while (appDomainIterator.Next())
        {
            AppDomain * pAppDomain = appDomainIterator.GetDomain();
            hr = EnumerateDomainClosedMethodDescs(
                pAppDomain,
                pModule,
                methodDef,
                pClosedMethodDescs,
                pUnsupportedMethodErrors);
            if (FAILED(hr))
            {
                _ASSERTE(hr == E_OUTOFMEMORY);
                return hr;
            }
        }
    }
    return S_OK;
}

// static
HRESULT CodeVersionManager::EnumerateDomainClosedMethodDescs(
    AppDomain * pAppDomainToSearch,
    Module* pModuleContainingMethodDef,
    mdMethodDef methodDef,
    CDynArray<MethodDesc*> * pClosedMethodDescs,
    CDynArray<CodePublishError> * pUnsupportedMethodErrors)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
        PRECONDITION(CheckPointer(pAppDomainToSearch, NULL_OK));
        PRECONDITION(CheckPointer(pModuleContainingMethodDef));
        PRECONDITION(CheckPointer(pClosedMethodDescs));
        PRECONDITION(CheckPointer(pUnsupportedMethodErrors));
    }
    CONTRACTL_END;

    _ASSERTE(methodDef != mdTokenNil);

    HRESULT hr;

    BaseDomain * pDomainContainingGenericDefinition = pModuleContainingMethodDef->GetDomain();

#ifdef _DEBUG
    // If the generic definition is not loaded domain-neutral, then all its
    // instantiations will also be non-domain-neutral and loaded into the same
    // domain as the generic definition.  So the caller may only pass the
    // domain containing the generic definition as pAppDomainToSearch
    if (!pDomainContainingGenericDefinition->IsSharedDomain())
    {
        _ASSERTE(pDomainContainingGenericDefinition == pAppDomainToSearch);
    }
#endif //_DEBUG

    // these are the default flags which won't actually be used in shared mode other than
    // asserting they were specified with their default values
    AssemblyIterationFlags assemFlags = (AssemblyIterationFlags)(kIncludeLoaded | kIncludeExecution);
    ModuleIterationOption moduleFlags = (ModuleIterationOption)kModIterIncludeLoaded;
    if (pAppDomainToSearch != NULL)
    {
        assemFlags = (AssemblyIterationFlags)(kIncludeAvailableToProfilers | kIncludeExecution);
        moduleFlags = (ModuleIterationOption)kModIterIncludeAvailableToProfilers;
    }
    LoadedMethodDescIterator it(
        pAppDomainToSearch,
        pModuleContainingMethodDef,
        methodDef,
        assemFlags,
        moduleFlags);
    CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;
    while (it.Next(pDomainAssembly.This()))
    {
        MethodDesc * pLoadedMD = it.Current();

        if (!pLoadedMD->IsVersionable())
        {
            // For compatibility with the rejit APIs we ensure certain errors are detected and reported using their
            // original HRESULTS
            HRESULT errorHR = GetNonVersionableError(pLoadedMD);
            if (FAILED(errorHR))
            {
                if (FAILED(hr = CodeVersionManager::AddCodePublishError(pModuleContainingMethodDef, methodDef, pLoadedMD, CORPROF_E_FUNCTION_IS_COLLECTIBLE, pUnsupportedMethodErrors)))
                {
                    _ASSERTE(hr == E_OUTOFMEMORY);
                    return hr;
                }
            }
            continue;
        }
        
#ifdef _DEBUG
        if (!pDomainContainingGenericDefinition->IsSharedDomain())
        {
            // Method is defined outside of the shared domain, so its instantiation must
            // be defined in the AD we're iterating over (pAppDomainToSearch, which, as
            // asserted above, must be the same domain as the generic's definition)
            _ASSERTE(pLoadedMD->GetDomain() == pAppDomainToSearch);
        }
#endif // _DEBUG

        MethodDesc ** ppMD = pClosedMethodDescs->Append();
        if (ppMD == NULL)
        {
            return E_OUTOFMEMORY;
        }
        *ppMD = pLoadedMD;
    }
    return S_OK;
}
#endif // DACCESS_COMPILE


//---------------------------------------------------------------------------------------
//
// Given the default version code for a MethodDesc that is about to published, add 
// a jumpstamp pointing back to the prestub if the currently active version isn't
// the default one. This called from the PublishMethodHolder.
//
// Arguments:
//    * pMD - MethodDesc to jmp-stamp
//    * pCode - Top of the code that was just jitted (using original IL).
//
//
// Return value:
//    * S_OK: Either we successfully did the jmp-stamp, or we didn't have to
//    * Else, HRESULT indicating failure.

// Assumptions:
//     The caller has not yet published pCode to the MethodDesc, so no threads can be
//     executing inside pMD's code yet. Thus, we don't need to suspend the runtime while
//     applying the jump-stamp like we usually do for rejit requests that are made after
//     a function has been JITted.
//
#ifndef DACCESS_COMPILE
HRESULT CodeVersionManager::DoJumpStampIfNecessary(MethodDesc* pMD, PCODE pCode)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CAN_TAKE_LOCK;
        PRECONDITION(CheckPointer(pMD));
        PRECONDITION(pCode != NULL);
    }
    CONTRACTL_END;

    _ASSERTE(LockOwnedByCurrentThread());

    NativeCodeVersion activeCodeVersion = GetActiveILCodeVersion(pMD).GetActiveNativeCodeVersion(pMD);
    if (activeCodeVersion.IsDefaultVersion())
    {
        //Method not requested to be rejitted, nothing to do
        return S_OK;
    }

    if (!pMD->IsVersionableWithJumpStamp())
    {
        return GetNonVersionableError(pMD);
    }

#ifndef FEATURE_JUMPSTAMP
    _ASSERTE(!"How did we get here? IsVersionableWithJumpStamp() should have been false above");
    return S_OK;
#else
    HRESULT hr;
    MethodDescVersioningState* pVersioningState;
    if (FAILED(hr = GetOrCreateMethodDescVersioningState(pMD, &pVersioningState)))
    {
        _ASSERTE(hr == E_OUTOFMEMORY);
        return hr;
    }
    if (pVersioningState->GetJumpStampState() != MethodDescVersioningState::JumpStampNone)
    {
        //JumpStamp already in place
        return S_OK;
    }
    return pVersioningState->JumpStampNativeCode(pCode);
#endif // FEATURE_JUMPSTAMP

}
#endif // DACCESS_COMPILE

#ifndef DACCESS_COMPILE
//static
void CodeVersionManager::OnAppDomainExit(AppDomain * pAppDomain)
{
    LIMITED_METHOD_CONTRACT;
    // This would clean up all the allocations we have done and synchronize with any threads that might
    // still be using the data
    _ASSERTE(!".NET Core shouldn't be doing app domain shutdown - if we start doing so this needs to be implemented");
}
#endif

// Returns true if CodeVersionManager is capable of versioning this method. There may be other reasons that the runtime elects
// not to version a method even if CodeVersionManager could support it. Use the MethodDesc::IsVersionableWith*() accessors to
// get the final determination of versioning support for a given method.
//
//static
bool CodeVersionManager::IsMethodSupported(PTR_MethodDesc pMethodDesc)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(pMethodDesc != NULL);

    return
        // CodeVersionManager data structures don't properly handle the lifetime semantics of dynamic code at this point
        !pMethodDesc->IsDynamicMethod() &&

        // CodeVersionManager data structures don't properly handle the lifetime semantics of collectible code at this point
        !pMethodDesc->GetLoaderAllocator()->IsCollectible() &&

        // EnC has its own way of versioning
        !pMethodDesc->IsEnCMethod();
}

//---------------------------------------------------------------------------------------
//
// Small helper to determine whether a given (possibly instantiated generic) MethodDesc
// is safe to rejit.
//
// Arguments:
//      pMD - MethodDesc to test
// Return Value:
//      S_OK iff pMD is safe to rejit
//      CORPROF_E_FUNCTION_IS_COLLECTIBLE - function can't be rejitted because it is collectible
//      

// static
#ifndef DACCESS_COMPILE
HRESULT CodeVersionManager::GetNonVersionableError(MethodDesc* pMD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CAN_TAKE_LOCK;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(pMD != NULL);

    // Weird, non-user functions were already weeded out in RequestReJIT(), and will
    // also never be passed to us by the prestub worker (for the pre-rejit case).
    _ASSERTE(pMD->IsIL());

    // Any MethodDescs that could be collected are not currently supported.  Although we
    // rule out all Ref.Emit modules in RequestReJIT(), there can still exist types defined
    // in a non-reflection module and instantiated into a collectible assembly
    // (e.g., List<MyCollectibleStruct>).  In the future we may lift this
    // restriction by updating the ReJitManager when the collectible assemblies
    // owning the instantiations get collected.
    if (pMD->GetLoaderAllocator()->IsCollectible())
    {
        return CORPROF_E_FUNCTION_IS_COLLECTIBLE;
    }

    return S_OK;
}
#endif

//---------------------------------------------------------------------------------------
//
// Helper that inits a new CodePublishError and adds it to the pErrors array
//
// Arguments:
//      * pModule - The module in the module/MethodDef identifier pair for the method which
//                  had an error during rejit
//      * methodDef - The MethodDef in the module/MethodDef identifier pair for the method which
//                  had an error during rejit
//      * pMD - If available, the specific method instance which had an error during rejit
//      * hrStatus - HRESULT for the rejit error that occurred
//      * pErrors - the list of error records that this method will append to
//
// Return Value:
//      * S_OK: error was appended
//      * E_OUTOFMEMORY: Not enough memory to create the new error item. The array is unchanged.
//

//static
#ifndef DACCESS_COMPILE
HRESULT CodeVersionManager::AddCodePublishError(Module* pModule, mdMethodDef methodDef, MethodDesc* pMD, HRESULT hrStatus, CDynArray<CodePublishError> * pErrors)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (pErrors == NULL)
    {
        return S_OK;
    }

    CodePublishError* pError = pErrors->Append();
    if (pError == NULL)
    {
        return E_OUTOFMEMORY;
    }
    pError->pModule = pModule;
    pError->methodDef = methodDef;
    pError->pMethodDesc = pMD;
    pError->hrStatus = hrStatus;
    return S_OK;
}
#endif

#ifndef DACCESS_COMPILE
void CodeVersionManager::ReportCodePublishError(CodePublishError* pErrorRecord)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        CAN_TAKE_LOCK;
        MODE_ANY;
    }
    CONTRACTL_END;

    ReportCodePublishError(pErrorRecord->pModule, pErrorRecord->methodDef, pErrorRecord->pMethodDesc, pErrorRecord->hrStatus);
}

void CodeVersionManager::ReportCodePublishError(Module* pModule, mdMethodDef methodDef, MethodDesc* pMD, HRESULT hrStatus)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        CAN_TAKE_LOCK;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifdef FEATURE_REJIT
    BOOL isRejitted = FALSE;
    {
        TableLockHolder(this);
        isRejitted = !GetActiveILCodeVersion(pModule, methodDef).IsDefaultVersion();
    }

    // this isn't perfect, we might be activating a tiered jitting variation of a rejitted
    // method for example. If it proves to be an issue we can revisit.
    if (isRejitted)
    {
        ReJitManager::ReportReJITError(pModule, methodDef, pMD, hrStatus);
    }
#endif
}
#endif // DACCESS_COMPILE

//---------------------------------------------------------------------------------------
//
// PrepareCodeConfig::SetNativeCode() calls this to determine if there's a non-default code
// version requested for a MethodDesc that has just been jitted for the first time.
// This is also called when methods are being restored in NGEN images. The sequence looks like:
// *Enter holder
//   Enter code version manager lock
//   DoJumpStampIfNecessary
// *Runtime code publishes/restores method
// *Exit holder
//   Leave code version manager lock
//   Send rejit error callbacks if needed
// 
//
// #PublishCode:
// Note that the runtime needs to publish/restore the PCODE while this holder is
// on the stack, so it can happen under the code version manager's lock.
// This prevents a race with a profiler that calls
// RequestReJIT just as the method finishes compiling. In particular, the locking ensures
// atomicity between this set of steps (performed in DoJumpStampIfNecessary):
//     * (1) Checking whether there is a non-default version for this MD
//     * (2) If not, skip doing the jmp-stamp
//     * (3) Publishing the PCODE
//     
// with respect to these steps performed in RequestReJIT:
//     * (a) Is PCODE published yet?
//     * (b) Create non-default ILCodeVersion which the prestub will
//         consult when it JITs the original IL
//         
// Without this atomicity, we could get the ordering (1), (2), (a), (b), (3), resulting
// in the rejit request getting completely ignored (i.e., we file away the new ILCodeVersion
// AFTER the prestub checks for it).
//
// A similar race is possible for code being restored. In that case the restoring thread
// does:
//      * (1) Check if there is a non-default ILCodeVersion for this MD
//      * (2) If not, no need to jmp-stamp
//      * (3) Restore the MD

// And RequestRejit does:
//      * (a) [In LoadedMethodDescIterator] Is a potential MD restored yet?
//      * (b) [In EnumerateDomainClosedMethodDescs] If not, don't queue it for jump-stamping
//
// Same ordering (1), (2), (a), (b), (3) results in missing both opportunities to jump
// stamp.

#if !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
PublishMethodHolder::PublishMethodHolder(MethodDesc* pMethodDesc, PCODE pCode) :
    m_pMD(NULL), m_hr(S_OK)
{
    // This method can't have a contract because entering the table lock
    // below increments GCNoTrigger count. Contracts always revert these changes
    // at the end of the method but we need the incremented count to flow out of the
    // method. The balancing decrement occurs in the destructor.
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_CAN_TAKE_LOCK;
    STATIC_CONTRACT_MODE_ANY;

    // We come here from the PreStub and from MethodDesc::CheckRestore
    // The method should be effectively restored, but we haven't yet
    // cleared the unrestored bit so we can't assert pMethodDesc->IsRestored()
    // We can assert:
    _ASSERTE(pMethodDesc->GetMethodTable()->IsRestored());

    if (pCode != NULL)
    {
        m_pMD = pMethodDesc;
        CodeVersionManager* pCodeVersionManager = pMethodDesc->GetCodeVersionManager();
        pCodeVersionManager->EnterLock();
        m_hr = pCodeVersionManager->DoJumpStampIfNecessary(pMethodDesc, pCode);
    }
}


PublishMethodHolder::~PublishMethodHolder()
{
    // This method can't have a contract because leaving the table lock
    // below decrements GCNoTrigger count. Contracts always revert these changes
    // at the end of the method but we need the decremented count to flow out of the
    // method. The balancing increment occurred in the constructor.
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS; // NOTRIGGER until we leave the lock
    STATIC_CONTRACT_CAN_TAKE_LOCK;
    STATIC_CONTRACT_MODE_ANY;

    if (m_pMD)
    {
        CodeVersionManager* pCodeVersionManager = m_pMD->GetCodeVersionManager();
        pCodeVersionManager->LeaveLock();
        if (FAILED(m_hr))
        {
            pCodeVersionManager->ReportCodePublishError(m_pMD->GetModule(), m_pMD->GetMemberDef(), m_pMD, m_hr);
        }
    }
}

PublishMethodTableHolder::PublishMethodTableHolder(MethodTable* pMethodTable) :
    m_pMethodTable(NULL)
{
    // This method can't have a contract because entering the table lock
    // below increments GCNoTrigger count. Contracts always revert these changes
    // at the end of the method but we need the incremented count to flow out of the
    // method. The balancing decrement occurs in the destructor.
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_CAN_TAKE_LOCK;
    STATIC_CONTRACT_MODE_ANY;

    // We come here from MethodTable::SetIsRestored
    // The method table should be effectively restored, but we haven't yet
    // cleared the unrestored bit so we can't assert pMethodTable->IsRestored()

    m_pMethodTable = pMethodTable;
    CodeVersionManager* pCodeVersionManager = pMethodTable->GetModule()->GetCodeVersionManager();
    pCodeVersionManager->EnterLock();
    MethodTable::IntroducedMethodIterator itMethods(pMethodTable, FALSE);
    for (; itMethods.IsValid(); itMethods.Next())
    {
        // Although the MethodTable is restored, the methods might not be.
        // We need to be careful to only query portions of the MethodDesc
        // that work in a partially restored state. The only methods that need
        // further restoration are IL stubs (which aren't rejittable) and
        // generic methods. The only generic methods directly accessible from
        // the MethodTable are definitions. GetNativeCode() on generic defs
        // will run succesfully and return NULL which short circuits the
        // rest of the logic.
        MethodDesc * pMD = itMethods.GetMethodDesc();
        PCODE pCode = pMD->GetNativeCode();
        if (pCode != NULL)
        {
            HRESULT hr = pCodeVersionManager->DoJumpStampIfNecessary(pMD, pCode);
            if (FAILED(hr))
            {
                CodeVersionManager::AddCodePublishError(pMD->GetModule(), pMD->GetMemberDef(), pMD, hr, &m_errors);
            }
        }
    }
}


PublishMethodTableHolder::~PublishMethodTableHolder()
{
    // This method can't have a contract because leaving the table lock
    // below decrements GCNoTrigger count. Contracts always revert these changes
    // at the end of the method but we need the decremented count to flow out of the
    // method. The balancing increment occurred in the constructor.
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS; // NOTRIGGER until we leave the lock
    STATIC_CONTRACT_CAN_TAKE_LOCK;
    STATIC_CONTRACT_MODE_ANY;

    if (m_pMethodTable)
    {
        CodeVersionManager* pCodeVersionManager = m_pMethodTable->GetModule()->GetCodeVersionManager();
        pCodeVersionManager->LeaveLock();
        for (int i = 0; i < m_errors.Count(); i++)
        {
            pCodeVersionManager->ReportCodePublishError(&(m_errors[i]));
        }
    }
}
#endif // !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)

#endif // FEATURE_CODE_VERSIONING

