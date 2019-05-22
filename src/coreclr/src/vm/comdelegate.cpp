// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// File: COMDelegate.cpp 
// 

// This module contains the implementation of the native methods for the
// Delegate class.
// 


#include "common.h"
#include "comdelegate.h"
#include "invokeutil.h"
#include "excep.h"
#include "class.h"
#include "field.h"
#include "dllimportcallback.h"
#include "dllimport.h"
#include "eeconfig.h"
#include "cgensys.h"
#include "asmconstants.h"
#include "virtualcallstub.h"
#include "callingconvention.h"
#include "customattribute.h"
#include "typestring.h"
#include "../md/compiler/custattr.h"
#ifdef FEATURE_COMINTEROP
#include "comcallablewrapper.h"
#endif // FEATURE_COMINTEROP

#define DELEGATE_MARKER_UNMANAGEDFPTR -1


#ifndef DACCESS_COMPILE

#if defined(_TARGET_AMD64_) && !defined(UNIX_AMD64_ABI)

// ShuffleOfs not needed

#elif defined(_TARGET_X86_)

// Return an encoded shuffle entry describing a general register or stack offset that needs to be shuffled.
static UINT16 ShuffleOfs(INT ofs, UINT stackSizeDelta = 0)
{
    STANDARD_VM_CONTRACT;

    if (TransitionBlock::IsStackArgumentOffset(ofs))
    {
        ofs = (ofs - TransitionBlock::GetOffsetOfReturnAddress()) + stackSizeDelta;

        if (ofs >= ShuffleEntry::REGMASK)
        {
            // method takes too many stack args
            COMPlusThrow(kNotSupportedException);
        }
    }
    else
    {
        ofs -= TransitionBlock::GetOffsetOfArgumentRegisters();
        ofs |= ShuffleEntry::REGMASK;
    }

    return static_cast<UINT16>(ofs);
}

#else // Portable default implementation

// Iterator for extracting shuffle entries for argument desribed by an ArgLocDesc.
// Used when calculating shuffle array entries in GenerateShuffleArray below.
class ShuffleIterator
{
    // Argument location description
    ArgLocDesc* m_argLocDesc;

#if defined(UNIX_AMD64_ABI)
    // Current eightByte used for struct arguments in registers
    int m_currentEightByte;
#endif    
    // Current general purpose register index (relative to the ArgLocDesc::m_idxGenReg)
    int m_currentGenRegIndex;
    // Current floating point register index (relative to the ArgLocDesc::m_idxFloatReg)
    int m_currentFloatRegIndex;
    // Current stack slot index (relative to the ArgLocDesc::m_idxStack)
    int m_currentStackSlotIndex;

#if defined(UNIX_AMD64_ABI)
    // Get next shuffle offset for struct passed in registers. There has to be at least one offset left.
    UINT16 GetNextOfsInStruct()
    {
        EEClass* eeClass = m_argLocDesc->m_eeClass;
        _ASSERTE(eeClass != NULL);
        
        if (m_currentEightByte < eeClass->GetNumberEightBytes())
        {
            SystemVClassificationType eightByte = eeClass->GetEightByteClassification(m_currentEightByte);
            unsigned int eightByteSize = eeClass->GetEightByteSize(m_currentEightByte);

            m_currentEightByte++;

            int index;
            UINT16 mask = ShuffleEntry::REGMASK;

            if (eightByte == SystemVClassificationTypeSSE)
            {
                _ASSERTE(m_currentFloatRegIndex < m_argLocDesc->m_cFloatReg);
                index = m_argLocDesc->m_idxFloatReg + m_currentFloatRegIndex;
                m_currentFloatRegIndex++;

                mask |= ShuffleEntry::FPREGMASK;
                if (eightByteSize == 4)
                {
                    mask |= ShuffleEntry::FPSINGLEMASK;
                }
            }
            else
            {
                _ASSERTE(m_currentGenRegIndex < m_argLocDesc->m_cGenReg);
                index = m_argLocDesc->m_idxGenReg + m_currentGenRegIndex;
                m_currentGenRegIndex++;
            }

            return (UINT16)index | mask;
        }

        // There are no more offsets to get, the caller should not have called us
        _ASSERTE(false);
        return 0;
    }
#endif // UNIX_AMD64_ABI

public:

    // Construct the iterator for the ArgLocDesc
    ShuffleIterator(ArgLocDesc* argLocDesc)
    :
        m_argLocDesc(argLocDesc),
#if defined(UNIX_AMD64_ABI)
        m_currentEightByte(0),
#endif
        m_currentGenRegIndex(0),
        m_currentFloatRegIndex(0),
        m_currentStackSlotIndex(0)
    {
    }

    // Check if there are more offsets to shuffle
    bool HasNextOfs()
    {
        return (m_currentGenRegIndex < m_argLocDesc->m_cGenReg) || 
#if defined(UNIX_AMD64_ABI)
               (m_currentFloatRegIndex < m_argLocDesc->m_cFloatReg) ||
#endif
               (m_currentStackSlotIndex < m_argLocDesc->m_cStack);        
    }

    // Get next offset to shuffle. There has to be at least one offset left.
    UINT16 GetNextOfs()
    {
        int index;

#if defined(UNIX_AMD64_ABI)

        // Check if the argLocDesc is for a struct in registers
        EEClass* eeClass = m_argLocDesc->m_eeClass;
        if (m_argLocDesc->m_eeClass != 0)
        {
            return GetNextOfsInStruct();
        }

        // Shuffle float registers first
        if (m_currentFloatRegIndex < m_argLocDesc->m_cFloatReg)
        {        
            index = m_argLocDesc->m_idxFloatReg + m_currentFloatRegIndex;
            m_currentFloatRegIndex++;

            return (UINT16)index | ShuffleEntry::REGMASK | ShuffleEntry::FPREGMASK;
        }
#endif // UNIX_AMD64_ABI

        // Shuffle any registers first (the order matters since otherwise we could end up shuffling a stack slot
        // over a register we later need to shuffle down as well).
        if (m_currentGenRegIndex < m_argLocDesc->m_cGenReg)
        {        
            index = m_argLocDesc->m_idxGenReg + m_currentGenRegIndex;
            m_currentGenRegIndex++;

            return (UINT16)index | ShuffleEntry::REGMASK;
        }

        // If we get here we must have at least one stack slot left to shuffle (this method should only be called
        // when AnythingToShuffle(pArg) == true).
        if (m_currentStackSlotIndex < m_argLocDesc->m_cStack)
        {
            index = m_argLocDesc->m_idxStack + m_currentStackSlotIndex;
            m_currentStackSlotIndex++;

            // Delegates cannot handle overly large argument stacks due to shuffle entry encoding limitations.
            if (index >= ShuffleEntry::REGMASK)
            {
                COMPlusThrow(kNotSupportedException);
            }

            return (UINT16)index;
        }

        // There are no more offsets to get, the caller should not have called us
        _ASSERTE(false);
        return 0;
    }
};

#endif

#if defined(UNIX_AMD64_ABI)
// Return an index of argument slot. First indices are reserved for general purpose registers,
// the following ones for float registers and then the rest for stack slots.
// This index is independent of how many registers are actually used to pass arguments.
int GetNormalizedArgumentSlotIndex(UINT16 offset)
{
    int index;

    if (offset & ShuffleEntry::FPREGMASK)
    {
        index = NUM_ARGUMENT_REGISTERS + (offset & ShuffleEntry::OFSREGMASK);
    }
    else if (offset & ShuffleEntry::REGMASK)
    {
        index = offset & ShuffleEntry::OFSREGMASK;
    }
    else
    {
        // stack slot
        index = NUM_ARGUMENT_REGISTERS + NUM_FLOAT_ARGUMENT_REGISTERS + (offset & ShuffleEntry::OFSMASK);
    }

    return index;
}
#endif // UNIX_AMD64_ABI

VOID GenerateShuffleArray(MethodDesc* pInvoke, MethodDesc *pTargetMeth, SArray<ShuffleEntry> * pShuffleEntryArray)
{
    STANDARD_VM_CONTRACT;

    ShuffleEntry entry;
    ZeroMemory(&entry, sizeof(entry));

#if defined(_TARGET_AMD64_) && !defined(UNIX_AMD64_ABI)
    MetaSig msig(pInvoke);
    ArgIterator argit(&msig);

    if (argit.HasRetBuffArg())
    {
        if (!pTargetMeth->IsStatic())
        {
            // Use ELEMENT_TYPE_END to signal the special handling required by
            // instance method with return buffer. "this" needs to come from
            // the first argument.
            entry.argtype = ELEMENT_TYPE_END;
            pShuffleEntryArray->Append(entry);

            msig.NextArgNormalized();
        }
        else
        {
            entry.argtype = ELEMENT_TYPE_PTR;
            pShuffleEntryArray->Append(entry);
        }
    }

    CorElementType sigType;

    while ((sigType = msig.NextArgNormalized()) != ELEMENT_TYPE_END)
    {        
        ZeroMemory(&entry, sizeof(entry));
        entry.argtype = sigType;
        pShuffleEntryArray->Append(entry);
    }

    ZeroMemory(&entry, sizeof(entry));
    entry.srcofs  = ShuffleEntry::SENTINEL;
    pShuffleEntryArray->Append(entry);

#elif defined(_TARGET_X86_)
    // Must create independent msigs to prevent the argiterators from
    // interfering with other.
    MetaSig sSigSrc(pInvoke);
    MetaSig sSigDst(pTargetMeth);

    _ASSERTE(sSigSrc.HasThis());

    ArgIterator sArgPlacerSrc(&sSigSrc);
    ArgIterator sArgPlacerDst(&sSigDst);

    UINT stackSizeSrc = sArgPlacerSrc.SizeOfArgStack();
    UINT stackSizeDst = sArgPlacerDst.SizeOfArgStack();

    if (stackSizeDst > stackSizeSrc)
    {
        // we can drop arguments but we can never make them up - this is definitely not allowed
        COMPlusThrow(kVerificationException);
    }

    UINT stackSizeDelta;

#ifdef UNIX_X86_ABI
    // Stack does not shrink as UNIX_X86_ABI uses CDECL (instead of STDCALL).
    stackSizeDelta = 0;
#else
    stackSizeDelta = stackSizeSrc - stackSizeDst;
#endif

    INT ofsSrc, ofsDst;

    // if the function is non static we need to place the 'this' first
    if (!pTargetMeth->IsStatic())
    {
        entry.srcofs = ShuffleOfs(sArgPlacerSrc.GetNextOffset());
        entry.dstofs = ShuffleEntry::REGMASK | 4;
        pShuffleEntryArray->Append(entry);
    }
    else if (sArgPlacerSrc.HasRetBuffArg())
    {
        // the first register is used for 'this'
        entry.srcofs = ShuffleOfs(sArgPlacerSrc.GetRetBuffArgOffset());
        entry.dstofs = ShuffleOfs(sArgPlacerDst.GetRetBuffArgOffset(), stackSizeDelta);
        if (entry.srcofs != entry.dstofs)
            pShuffleEntryArray->Append(entry);
    }

    while (TransitionBlock::InvalidOffset != (ofsSrc = sArgPlacerSrc.GetNextOffset()))
    {
        ofsDst = sArgPlacerDst.GetNextOffset();

        int cbSize = sArgPlacerDst.GetArgSize();

        do
        {
            entry.srcofs = ShuffleOfs(ofsSrc);
            entry.dstofs = ShuffleOfs(ofsDst, stackSizeDelta);

            ofsSrc += STACK_ELEM_SIZE;
            ofsDst += STACK_ELEM_SIZE;

            if (entry.srcofs != entry.dstofs)
                pShuffleEntryArray->Append(entry);

            cbSize -= STACK_ELEM_SIZE;
        }
        while (cbSize > 0);
    }

    if (stackSizeDelta != 0)
    {
        // Emit code to move the return address
        entry.srcofs = 0;     // retaddress is assumed to be at esp
        entry.dstofs = static_cast<UINT16>(stackSizeDelta);
        pShuffleEntryArray->Append(entry);
    }

    entry.srcofs = ShuffleEntry::SENTINEL;
    entry.dstofs = static_cast<UINT16>(stackSizeDelta);
    pShuffleEntryArray->Append(entry);

#else // Portable default implementation
    MetaSig sSigSrc(pInvoke);
    MetaSig sSigDst(pTargetMeth);

    // Initialize helpers that determine how each argument for the source and destination signatures is placed
    // in registers or on the stack.
    ArgIterator sArgPlacerSrc(&sSigSrc);
    ArgIterator sArgPlacerDst(&sSigDst);

    INT ofsSrc;
    INT ofsDst;
    ArgLocDesc sArgSrc;
    ArgLocDesc sArgDst;

#if defined(UNIX_AMD64_ABI)
    int argSlots = NUM_FLOAT_ARGUMENT_REGISTERS + NUM_ARGUMENT_REGISTERS + sArgPlacerSrc.SizeOfArgStack() / sizeof(size_t);
#endif // UNIX_AMD64_ABI

    // If the target method in non-static (this happens for open instance delegates), we need to account for
    // the implicit this parameter.
    if (sSigDst.HasThis())
    {
        // The this pointer is an implicit argument for the destination signature. But on the source side it's
        // just another regular argument and needs to be iterated over by sArgPlacerSrc and the MetaSig.
        sArgPlacerSrc.GetArgLoc(sArgPlacerSrc.GetNextOffset(), &sArgSrc);

        sArgPlacerSrc.GetThisLoc(&sArgDst);

        ShuffleIterator iteratorSrc(&sArgSrc);
        ShuffleIterator iteratorDst(&sArgDst);

        entry.srcofs = iteratorSrc.GetNextOfs();
        entry.dstofs = iteratorDst.GetNextOfs();
        pShuffleEntryArray->Append(entry);
    }

    // Handle any return buffer argument.
    if (sArgPlacerDst.HasRetBuffArg())
    {
        // The return buffer argument is implicit in both signatures.

#if !defined(_TARGET_ARM64_) || !defined(CALLDESCR_RETBUFFARGREG)
        // The ifdef above disables this code if the ret buff arg is always in the same register, which
        // means that we don't need to do any shuffling for it.

        sArgPlacerSrc.GetRetBuffArgLoc(&sArgSrc);
        sArgPlacerDst.GetRetBuffArgLoc(&sArgDst);

        ShuffleIterator iteratorSrc(&sArgSrc);
        ShuffleIterator iteratorDst(&sArgDst);

        entry.srcofs = iteratorSrc.GetNextOfs();
        entry.dstofs = iteratorDst.GetNextOfs();

        // Depending on the type of target method (static vs instance) the return buffer argument may end up
        // in the same register in both signatures. So we only commit the entry (by moving the entry pointer
        // along) in the case where it's not a no-op (i.e. the source and destination ops are different).
        if (entry.srcofs != entry.dstofs)
            pShuffleEntryArray->Append(entry);
#endif // !defined(_TARGET_ARM64_) || !defined(CALLDESCR_RETBUFFARGREG)
    }

    // Iterate all the regular arguments. mapping source registers and stack locations to the corresponding
    // destination locations.
    while ((ofsSrc = sArgPlacerSrc.GetNextOffset()) != TransitionBlock::InvalidOffset)
    {
        ofsDst = sArgPlacerDst.GetNextOffset();

        // Find the argument location mapping for both source and destination signature. A single argument can
        // occupy a floating point register, a general purpose register, a pair of registers of any kind or
        // a stack slot.
        sArgPlacerSrc.GetArgLoc(ofsSrc, &sArgSrc);
        sArgPlacerDst.GetArgLoc(ofsDst, &sArgDst);

        ShuffleIterator iteratorSrc(&sArgSrc);
        ShuffleIterator iteratorDst(&sArgDst);

        // Shuffle each slot in the argument (register or stack slot) from source to destination.
        while (iteratorSrc.HasNextOfs())
        {
            // Locate the next slot to shuffle in the source and destination and encode the transfer into a
            // shuffle entry.
            entry.srcofs = iteratorSrc.GetNextOfs();
            entry.dstofs = iteratorDst.GetNextOfs();

            // Only emit this entry if it's not a no-op (i.e. the source and destination locations are
            // different).
            if (entry.srcofs != entry.dstofs)
                pShuffleEntryArray->Append(entry);
        }

        // We should have run out of slots to shuffle in the destination at the same time as the source.
        _ASSERTE(!iteratorDst.HasNextOfs());
    }

#if defined(UNIX_AMD64_ABI)
    // The Unix AMD64 ABI can cause a struct to be passed on stack for the source and in registers for the destination.
    // That can cause some arguments that are passed on stack for the destination to be passed in registers in the source.
    // An extreme example of that is e.g.:
    //   void fn(int, int, int, int, int, struct {int, double}, double, double, double, double, double, double, double, double, double, double)
    // For this signature, the shuffle needs to move slots as follows (please note the "forward" movement of xmm registers):
    //   RDI->RSI, RDX->RCX, R8->RDX, R9->R8, stack[0]->R9, xmm0->xmm1, xmm1->xmm2, ... xmm6->xmm7, xmm7->stack[0], stack[1]->xmm0, stack[2]->stack[1], stack[3]->stack[2]
    // To prevent overwriting of slots before they are moved, we need to sort the move operations.

    NewArrayHolder<bool> filledSlots = new bool[argSlots];

    bool reordered;
    do
    {
        reordered = false;

        for (int i = 0; i < argSlots; i++)
        {
            filledSlots[i] = false;
        }
        for (unsigned int i = 0; i < pShuffleEntryArray->GetCount(); i++)
        {
            entry = (*pShuffleEntryArray)[i];

            // If the slot that we are moving the argument to was filled in already, we need to move this entry in front
            // of the entry that filled it in.
            if (filledSlots[GetNormalizedArgumentSlotIndex(entry.srcofs)])
            {
                unsigned int j;
                for (j = i; (*pShuffleEntryArray)[j].dstofs != entry.srcofs; j--)
                    (*pShuffleEntryArray)[j] = (*pShuffleEntryArray)[j - 1];

                (*pShuffleEntryArray)[j] = entry;
                reordered = true;
            }

            filledSlots[GetNormalizedArgumentSlotIndex(entry.dstofs)] = true;
        }
    }
    while (reordered);
#endif // UNIX_AMD64_ABI

    entry.srcofs = ShuffleEntry::SENTINEL;
    entry.dstofs = 0;
    pShuffleEntryArray->Append(entry);
#endif
}


ShuffleThunkCache *COMDelegate::m_pShuffleThunkCache = NULL;
MulticastStubCache *COMDelegate::m_pSecureDelegateStubCache = NULL;
MulticastStubCache *COMDelegate::m_pMulticastStubCache = NULL;

CrstStatic   COMDelegate::s_DelegateToFPtrHashCrst;
PtrHashMap*  COMDelegate::s_pDelegateToFPtrHash = NULL;


// One time init.
void COMDelegate::Init()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    s_DelegateToFPtrHashCrst.Init(CrstDelegateToFPtrHash, CRST_UNSAFE_ANYMODE);

    s_pDelegateToFPtrHash = ::new PtrHashMap();

    LockOwner lock = {&COMDelegate::s_DelegateToFPtrHashCrst, IsOwnerOfCrst};
    s_pDelegateToFPtrHash->Init(TRUE, &lock);

    m_pShuffleThunkCache = new ShuffleThunkCache(SystemDomain::GetGlobalLoaderAllocator()->GetStubHeap());
    m_pMulticastStubCache = new MulticastStubCache();
    m_pSecureDelegateStubCache = new MulticastStubCache();
}

#ifdef FEATURE_COMINTEROP
ComPlusCallInfo * COMDelegate::PopulateComPlusCallInfo(MethodTable * pDelMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    DelegateEEClass * pClass = (DelegateEEClass *)pDelMT->GetClass();

    // set up the ComPlusCallInfo if it does not exist already
    if (pClass->m_pComPlusCallInfo == NULL)
    {
        LoaderHeap *pHeap = pDelMT->GetLoaderAllocator()->GetHighFrequencyHeap();
        ComPlusCallInfo *pTemp = (ComPlusCallInfo *)(void *)pHeap->AllocMem(S_SIZE_T(sizeof(ComPlusCallInfo)));

        pTemp->m_cachedComSlot = ComMethodTable::GetNumExtraSlots(ifVtable);
        pTemp->InitStackArgumentSize();

        InterlockedCompareExchangeT(EnsureWritablePages(&pClass->m_pComPlusCallInfo), pTemp, NULL);
    }

    *EnsureWritablePages(&pClass->m_pComPlusCallInfo->m_pInterfaceMT) = pDelMT;

    return pClass->m_pComPlusCallInfo;
}
#endif // FEATURE_COMINTEROP

// We need a LoaderHeap that lives at least as long as the DelegateEEClass, but ideally no longer
LoaderHeap *DelegateEEClass::GetStubHeap()
{
    return GetInvokeMethod()->GetLoaderAllocator()->GetStubHeap();
}


Stub* COMDelegate::SetupShuffleThunk(MethodTable * pDelMT, MethodDesc *pTargetMeth)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    GCX_PREEMP();

    DelegateEEClass * pClass = (DelegateEEClass *)pDelMT->GetClass();
    
    MethodDesc *pMD = pClass->GetInvokeMethod();

    StackSArray<ShuffleEntry> rShuffleEntryArray;
    GenerateShuffleArray(pMD, pTargetMeth, &rShuffleEntryArray);

    ShuffleThunkCache* pShuffleThunkCache = m_pShuffleThunkCache;

    LoaderAllocator* pLoaderAllocator = pDelMT->GetLoaderAllocator();
    if (pLoaderAllocator->IsCollectible())
    {
        pShuffleThunkCache = ((AssemblyLoaderAllocator*)pLoaderAllocator)->GetShuffleThunkCache();
    }

    Stub* pShuffleThunk = pShuffleThunkCache->Canonicalize((const BYTE *)&rShuffleEntryArray[0]);
    if (!pShuffleThunk)
    {
        COMPlusThrowOM();
    }

    g_IBCLogger.LogEEClassCOWTableAccess(pDelMT);

    EnsureWritablePages(pClass);

    if (!pTargetMeth->IsStatic() && pTargetMeth->HasRetBuffArg() && IsRetBuffPassedAsFirstArg()) 
    {
        if (FastInterlockCompareExchangePointer(&pClass->m_pInstRetBuffCallStub, pShuffleThunk, NULL ) != NULL)
        {
            pShuffleThunk->DecRef();
            pShuffleThunk = pClass->m_pInstRetBuffCallStub;
        }
    }
    else
    {
        if (FastInterlockCompareExchangePointer(&pClass->m_pStaticCallStub, pShuffleThunk, NULL ) != NULL)
        {
            pShuffleThunk->DecRef();
            pShuffleThunk = pClass->m_pStaticCallStub;
        }
    }    

    return pShuffleThunk;
}


#ifndef CROSSGEN_COMPILE

static PCODE GetVirtualCallStub(MethodDesc *method, TypeHandle scopeType)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM()); // from MetaSig::SizeOfArgStack
    }
    CONTRACTL_END;

    //TODO: depending on what we decide for generics method we may want to move this check to better places
    if (method->IsGenericMethodDefinition() || method->HasMethodInstantiation())
    {
        COMPlusThrow(kNotSupportedException);
    }

    // need to grab a virtual dispatch stub 
    // method can be on a canonical MethodTable, we need to allocate the stub on the loader allocator associated with the exact type instantiation.
    VirtualCallStubManager *pVirtualStubManager = scopeType.GetMethodTable()->GetLoaderAllocator()->GetVirtualCallStubManager();
    PCODE pTargetCall = pVirtualStubManager->GetCallStub(scopeType, method);
    _ASSERTE(pTargetCall);
    return pTargetCall;
}

FCIMPL5(FC_BOOL_RET, COMDelegate::BindToMethodName, 
                        Object *refThisUNSAFE, 
                        Object *targetUNSAFE, 
                        ReflectClassBaseObject *pMethodTypeUNSAFE,
                        StringObject* methodNameUNSAFE, 
                        int flags)
{
    FCALL_CONTRACT;

    struct _gc
    {
        DELEGATEREF refThis;
        OBJECTREF target;
        STRINGREF methodName;
        REFLECTCLASSBASEREF refMethodType;
    } gc;

    gc.refThis    = (DELEGATEREF) ObjectToOBJECTREF(refThisUNSAFE);
    gc.target     = (OBJECTREF) targetUNSAFE;
    gc.methodName = (STRINGREF) methodNameUNSAFE;
    gc.refMethodType = (REFLECTCLASSBASEREF) ObjectToOBJECTREF(pMethodTypeUNSAFE);

    TypeHandle methodType = gc.refMethodType->GetType();

    MethodDesc *pMatchingMethod = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    // Caching of MethodDescs (impl and decl) for MethodTable slots provided significant
    // performance gain in some reflection emit scenarios.
    MethodTable::AllowMethodDataCaching();

    TypeHandle targetType((gc.target != NULL) ? gc.target->GetMethodTable() : NULL);
    // get the invoke of the delegate
    MethodTable * pDelegateType = gc.refThis->GetMethodTable();
    MethodDesc* pInvokeMeth = COMDelegate::FindDelegateInvokeMethod(pDelegateType);
    _ASSERTE(pInvokeMeth);

    //
    // now loop through the methods looking for a match
    //

    // get the name in UTF8 format
    SString wszName(SString::Literal, gc.methodName->GetBuffer());
    StackScratchBuffer utf8Name;
    LPCUTF8 szNameStr = wszName.GetUTF8(utf8Name);

    // pick a proper compare function
    typedef int (__cdecl *UTF8StringCompareFuncPtr)(const char *, const char *);
    UTF8StringCompareFuncPtr StrCompFunc = (flags & DBF_CaselessMatching) ? stricmpUTF8 : strcmp;
    
    // search the type hierarchy
    MethodTable *pMTOrig = methodType.GetMethodTable()->GetCanonicalMethodTable();
    for (MethodTable *pMT = pMTOrig; pMT != NULL; pMT = pMT->GetParentMethodTable())
    {
        MethodTable::MethodIterator it(pMT);
        it.MoveToEnd();
        for (; it.IsValid() && (pMT == pMTOrig || !it.IsVirtual()); it.Prev())
        {
            MethodDesc *pCurMethod = it.GetDeclMethodDesc();
            
            // We can't match generic methods (since no instantiation information has been provided).
            if (pCurMethod->IsGenericMethodDefinition())
                continue;

            if ((pCurMethod != NULL) && (StrCompFunc(szNameStr, pCurMethod->GetName()) == 0))
            {
                // found a matching string, get an associated method desc if needed
                // Use unboxing stubs for instance and virtual methods on value types.
                // If this is a open delegate to an instance method BindToMethod will rebind it to the non-unboxing method.
                // Open delegate
                //   Static: never use unboxing stub
                //     BindToMethodInfo/Name will bind to the non-unboxing stub. BindToMethod will reinforce that.
                //   Instance: We only support binding to an unboxed value type reference here, so we must never use an unboxing stub
                //     BindToMethodInfo/Name will bind to the unboxing stub. BindToMethod will rebind to the non-unboxing stub.
                //   Virtual: trivial (not allowed)
                // Closed delegate
                //   Static: never use unboxing stub
                //     BindToMethodInfo/Name will bind to the non-unboxing stub.
                //   Instance: always use unboxing stub
                //     BindToMethodInfo/Name will bind to the unboxing stub.
                //   Virtual: always use unboxing stub
                //     BindToMethodInfo/Name will bind to the unboxing stub.

                pCurMethod =
                    MethodDesc::FindOrCreateAssociatedMethodDesc(pCurMethod,
                                                                 methodType.GetMethodTable(),
                                                                 (!pCurMethod->IsStatic() && pCurMethod->GetMethodTable()->IsValueType()), 
                                                                 pCurMethod->GetMethodInstantiation(),
                                                                 false /* do not allow code with a shared-code calling convention to be returned */,
                                                                 true /* Ensure that methods on generic interfaces are returned as instantiated method descs */);
                BOOL fIsOpenDelegate;
                if (!COMDelegate::IsMethodDescCompatible((gc.target == NULL) ? TypeHandle() : gc.target->GetTrueTypeHandle(), 
                                                        methodType, 
                                                        pCurMethod, 
                                                        gc.refThis->GetTypeHandle(), 
                                                        pInvokeMeth,
                                                        flags,
                                                        &fIsOpenDelegate))
                {
                    // Signature doesn't match, skip.
                    continue;
                }

                // Found the target that matches the signature and satisfies security transparency rules
                // Initialize the delegate to point to the target method.
                BindToMethod(&gc.refThis,
                             &gc.target,
                             pCurMethod,
                             methodType.GetMethodTable(),
                             fIsOpenDelegate,
                             TRUE);

                pMatchingMethod = pCurMethod;
                goto done;
            }
        }
    }
    done:
        ;
    HELPER_METHOD_FRAME_END();

    FC_RETURN_BOOL(pMatchingMethod != NULL);
}
FCIMPLEND


FCIMPL5(FC_BOOL_RET, COMDelegate::BindToMethodInfo, Object* refThisUNSAFE, Object* targetUNSAFE, ReflectMethodObject *pMethodUNSAFE, ReflectClassBaseObject *pMethodTypeUNSAFE, int flags)
{
    FCALL_CONTRACT;

    BOOL result = TRUE;

    struct _gc
    {
        DELEGATEREF refThis;
        OBJECTREF refFirstArg;
        REFLECTCLASSBASEREF refMethodType;
        REFLECTMETHODREF refMethod;
    } gc;

    gc.refThis          = (DELEGATEREF) ObjectToOBJECTREF(refThisUNSAFE);
    gc.refFirstArg      = ObjectToOBJECTREF(targetUNSAFE);
    gc.refMethodType    = (REFLECTCLASSBASEREF) ObjectToOBJECTREF(pMethodTypeUNSAFE);
    gc.refMethod        = (REFLECTMETHODREF) ObjectToOBJECTREF(pMethodUNSAFE);

    MethodTable *pMethMT = gc.refMethodType->GetType().GetMethodTable();
    MethodDesc *method = gc.refMethod->GetMethod();

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    // Assert to track down VS#458689.
    _ASSERTE(gc.refThis != gc.refFirstArg);

    // A generic method had better be instantiated (we can't dispatch to an uninstantiated one).
    if (method->IsGenericMethodDefinition())
        COMPlusThrow(kArgumentException, W("Arg_DlgtTargMeth"));

    // get the invoke of the delegate
    MethodTable * pDelegateType = gc.refThis->GetMethodTable();
    MethodDesc* pInvokeMeth = COMDelegate::FindDelegateInvokeMethod(pDelegateType);
    _ASSERTE(pInvokeMeth);

    // See the comment in BindToMethodName
    method =
        MethodDesc::FindOrCreateAssociatedMethodDesc(method,
                                                     pMethMT,
                                                     (!method->IsStatic() && pMethMT->IsValueType()), 
                                                     method->GetMethodInstantiation(),
                                                     false /* do not allow code with a shared-code calling convention to be returned */,
                                                     true /* Ensure that methods on generic interfaces are returned as instantiated method descs */);

    BOOL fIsOpenDelegate;
    if (COMDelegate::IsMethodDescCompatible((gc.refFirstArg == NULL) ? TypeHandle() : gc.refFirstArg->GetTrueTypeHandle(), 
                                            TypeHandle(pMethMT), 
                                            method, 
                                            gc.refThis->GetTypeHandle(), 
                                            pInvokeMeth,
                                            flags,
                                            &fIsOpenDelegate))
    {
        // Initialize the delegate to point to the target method.
        BindToMethod(&gc.refThis,
                     &gc.refFirstArg,
                     method,
                     pMethMT,
                     fIsOpenDelegate,
                     !(flags & DBF_SkipSecurityChecks));
    }
    else
        result = FALSE;

    HELPER_METHOD_FRAME_END();

    FC_RETURN_BOOL(result);
}
FCIMPLEND

// This method is called (in the late bound case only) once a target method has been decided on. All the consistency checks
// (signature matching etc.) have been done at this point and the only major reason we could fail now is on security grounds
// (someone trying to create a delegate over a method that's not visible to them for instance). This method will initialize the
// delegate (wrapping it in a secure delegate if necessary). Upon return the delegate should be ready for invocation.
void COMDelegate::BindToMethod(DELEGATEREF   *pRefThis,
                               OBJECTREF     *pRefFirstArg,
                               MethodDesc    *pTargetMethod,
                               MethodTable   *pExactMethodType,
                               BOOL           fIsOpenDelegate,
                               BOOL           fCheckSecurity)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pRefThis));
        PRECONDITION(CheckPointer(pRefFirstArg, NULL_OK));
        PRECONDITION(CheckPointer(pTargetMethod));
        PRECONDITION(CheckPointer(pExactMethodType));
    }
    CONTRACTL_END;
    
    // We might have to wrap the delegate in a secure delegate depending on the location of the target method. The following local
    // keeps track of the real (i.e. non-secure) delegate whether or not this is required.
    DELEGATEREF refRealDelegate = NULL;
    GCPROTECT_BEGIN(refRealDelegate);

    // Security checks (i.e. whether the creator of the delegate is allowed to access the target method) are the norm. They are only
    // disabled when:
    //   1. this is called by deserialization to recreate an existing delegate instance, where such checks are unwarranted.
    //   2. this is called from DynamicMethod.CreateDelegate which doesn't need access check.
    if (fCheckSecurity)
    {
        MethodTable *pInstanceMT = pExactMethodType;
        bool targetPossiblyRemoted = false;

        if (fIsOpenDelegate)
        {
            _ASSERTE(pRefFirstArg == NULL || *pRefFirstArg == NULL);

        }
        else
        {
            // closed-static is OK and we can check the target in the closed-instance case
            pInstanceMT = (*pRefFirstArg == NULL ? NULL : (*pRefFirstArg)->GetMethodTable());
        }

        RefSecContext sCtx(InvokeUtil::GetInvocationAccessCheckType(targetPossiblyRemoted));

        // Check visibility of the target method. If it's an instance method, we have to pass the type
        // of the instance being accessed which we get from the first argument or from the method itself.
        // The type of the instance is necessary for visibility checks of protected methods.
        InvokeUtil::CheckAccessMethod(&sCtx,
                                      pExactMethodType,
                                      pTargetMethod->IsStatic() ? NULL : pInstanceMT,
                                      pTargetMethod);
    }

    // If we didn't wrap the real delegate in a secure delegate then the real delegate is the one passed in.
    if (refRealDelegate == NULL)
    {
        if (NeedsWrapperDelegate(pTargetMethod))
            refRealDelegate = CreateSecureDelegate(*pRefThis, NULL, pTargetMethod);
        else
            refRealDelegate = *pRefThis;
    }

    pTargetMethod->EnsureActive();

    if (fIsOpenDelegate) 
    {
        _ASSERTE(pRefFirstArg == NULL || *pRefFirstArg == NULL);

        // Open delegates use themselves as the target (which handily allows their shuffle thunks to locate additional data at
        // invocation time).
        refRealDelegate->SetTarget(refRealDelegate);

        // We need to shuffle arguments for open delegates since the first argument on the calling side is not meaningful to the
        // callee.
        MethodTable * pDelegateMT = (*pRefThis)->GetMethodTable();
        DelegateEEClass *pDelegateClass = (DelegateEEClass*)pDelegateMT->GetClass();
        Stub *pShuffleThunk = NULL;

        // Look for a thunk cached on the delegate class first. Note we need a different thunk for instance methods with a
        // hidden return buffer argument because the extra argument switches place with the target when coming from the caller.
        if (!pTargetMethod->IsStatic() && pTargetMethod->HasRetBuffArg() && IsRetBuffPassedAsFirstArg()) 
            pShuffleThunk = pDelegateClass->m_pInstRetBuffCallStub;
        else
            pShuffleThunk = pDelegateClass->m_pStaticCallStub;

        // If we haven't already setup a shuffle thunk go do it now (which will cache the result automatically).
        if (!pShuffleThunk)
            pShuffleThunk = SetupShuffleThunk(pDelegateMT, pTargetMethod);

        // Indicate that the delegate will jump to the shuffle thunk rather than directly to the target method.
        refRealDelegate->SetMethodPtr(pShuffleThunk->GetEntryPoint());
            
        // Use stub dispatch for all virtuals.
        // <TODO> Investigate not using this for non-interface virtuals. </TODO>
        // The virtual dispatch stub doesn't work on unboxed value type objects which don't have MT pointers.
        // Since open instance delegates on value type methods require unboxed objects we cannot use the
        // virtual dispatch stub for them. On the other hand, virtual methods on value types don't need
        // to be dispatched because value types cannot be derived. So we treat them like non-virtual methods.
        if (pTargetMethod->IsVirtual() && !pTargetMethod->GetMethodTable()->IsValueType())
        {
            // Since this is an open delegate over a virtual method we cannot virtualize the call target now. So the shuffle thunk
            // needs to jump to another stub (this time provided by the VirtualStubManager) that will virtualize the call at
            // runtime.
            PCODE pTargetCall = GetVirtualCallStub(pTargetMethod, TypeHandle(pExactMethodType));
            refRealDelegate->SetMethodPtrAux(pTargetCall);
            refRealDelegate->SetInvocationCount((INT_PTR)(void *)pTargetMethod);
        }
        else
        {
            // <TODO> If VSD isn't compiled in this gives the wrong result for virtuals (we need run time virtualization). </TODO>
            // Reflection or the code in BindToMethodName will pass us the unboxing stub for non-static methods on value types. But
            // for open invocation on value type methods the actual reference will be passed so we need the unboxed method desc
            // instead.
            if (pTargetMethod->IsUnboxingStub())
            {
                // We want a MethodDesc which is not an unboxing stub, but is an instantiating stub if needed.
                pTargetMethod = MethodDesc::FindOrCreateAssociatedMethodDesc(
                                                        pTargetMethod,
                                                        pExactMethodType,
                                                        FALSE /* don't want unboxing entry point */,
                                                        pTargetMethod->GetMethodInstantiation(),
                                                        FALSE /* don't want MD that requires inst. arguments */,
                                                        true /* Ensure that methods on generic interfaces are returned as instantiated method descs */);
            }

            // The method must not require any extra hidden instantiation arguments.
            _ASSERTE(!pTargetMethod->RequiresInstArg());

            // Note that it is important to cache pTargetCode in local variable to avoid GC hole.
            // GetMultiCallableAddrOfCode() can trigger GC.
            PCODE pTargetCode = pTargetMethod->GetMultiCallableAddrOfCode();
            refRealDelegate->SetMethodPtrAux(pTargetCode);
        }
    }
    else
    {
        PCODE pTargetCode = NULL;

        // For virtual methods we can (and should) virtualize the call now (so we don't have to insert a thunk to do so at runtime).
        // <TODO>
        // Remove the following if we decide we won't cope with this case on late bound.
        // We can get virtual delegates closed over null through this code path, so be careful to handle that case (no need to
        // virtualize since we're just going to throw NullRefException at invocation time).
        // </TODO>
        if (pTargetMethod->IsVirtual() &&
            *pRefFirstArg != NULL &&
            pTargetMethod->GetMethodTable() != (*pRefFirstArg)->GetMethodTable())
            pTargetCode = pTargetMethod->GetMultiCallableAddrOfVirtualizedCode(pRefFirstArg, pTargetMethod->GetMethodTable());
        else
#ifdef HAS_THISPTR_RETBUF_PRECODE
        if (pTargetMethod->IsStatic() && pTargetMethod->HasRetBuffArg() && IsRetBuffPassedAsFirstArg())
            pTargetCode = pTargetMethod->GetLoaderAllocator()->GetFuncPtrStubs()->GetFuncPtrStub(pTargetMethod, PRECODE_THISPTR_RETBUF);
        else
#endif // HAS_THISPTR_RETBUF_PRECODE
            pTargetCode = pTargetMethod->GetMultiCallableAddrOfCode();
        _ASSERTE(pTargetCode);

        refRealDelegate->SetTarget(*pRefFirstArg);
        refRealDelegate->SetMethodPtr(pTargetCode);
    }

    LoaderAllocator *pLoaderAllocator = pTargetMethod->GetLoaderAllocator();

    if (pLoaderAllocator->IsCollectible())
        refRealDelegate->SetMethodBase(pLoaderAllocator->GetExposedObject());

    GCPROTECT_END();
}

// Marshals a managed method to an unmanaged callback provided the 
// managed method is static and it's parameters require no marshalling.
PCODE COMDelegate::ConvertToCallback(MethodDesc* pMD)
{
    CONTRACTL
    {
        THROWS;
    GC_TRIGGERS;
    INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    PCODE pCode = NULL;

    // only static methods are allowed
    if (!pMD->IsStatic())
        COMPlusThrow(kNotSupportedException, W("NotSupported_NonStaticMethod"));

    // no generic methods
    if (pMD->IsGenericMethodDefinition())
        COMPlusThrow(kNotSupportedException, W("NotSupported_GenericMethod"));

    // Arguments 
    if (NDirect::MarshalingRequired(pMD, pMD->GetSig(), pMD->GetModule()))
        COMPlusThrow(kNotSupportedException, W("NotSupported_NonBlittableTypes"));

    // Get UMEntryThunk from the thunk cache.
    UMEntryThunk *pUMEntryThunk = pMD->GetLoaderAllocator()->GetUMEntryThunkCache()->GetUMEntryThunk(pMD);

#if defined(_TARGET_X86_) && !defined(FEATURE_STUBS_AS_IL)

    // System.Runtime.InteropServices.NativeCallableAttribute
    BYTE* pData = NULL;
    LONG cData = 0;
    CorPinvokeMap callConv = (CorPinvokeMap)0;

    HRESULT hr = pMD->GetCustomAttribute(WellKnownAttribute::NativeCallable, (const VOID **)(&pData), (ULONG *)&cData);
    IfFailThrow(hr);

    if (cData > 0)
    {
        CustomAttributeParser ca(pData, cData);
        // NativeCallable has two optional named arguments CallingConvention and EntryPoint.
        CaNamedArg namedArgs[2];
        CaTypeCtor caType(SERIALIZATION_TYPE_STRING);
        // First, the void constructor.
        IfFailThrow(ParseKnownCaArgs(ca, NULL, 0));

        // Now the optional named properties
        namedArgs[0].InitI4FieldEnum("CallingConvention", "System.Runtime.InteropServices.CallingConvention", (ULONG)callConv);
        namedArgs[1].Init("EntryPoint", SERIALIZATION_TYPE_STRING, caType);
        IfFailThrow(ParseKnownCaNamedArgs(ca, namedArgs, lengthof(namedArgs)));

        callConv = (CorPinvokeMap)(namedArgs[0].val.u4 << 8);
        // Let UMThunkMarshalInfo choose the default if calling convension not definied.
        if (namedArgs[0].val.type.tag != SERIALIZATION_TYPE_UNDEFINED)
        {
            UMThunkMarshInfo* pUMThunkMarshalInfo = pUMEntryThunk->GetUMThunkMarshInfo();
            pUMThunkMarshalInfo->SetCallingConvention(callConv);
        }
}
#endif  //_TARGET_X86_ && !FEATURE_STUBS_AS_IL

    pCode = (PCODE)pUMEntryThunk->GetCode();
    _ASSERTE(pCode != NULL);
    return pCode;
}

// Marshals a delegate to a unmanaged callback.
LPVOID COMDelegate::ConvertToCallback(OBJECTREF pDelegateObj)
{
    CONTRACTL
    { 
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    if (!pDelegateObj) 
        return NULL;

    DELEGATEREF pDelegate = (DELEGATEREF) pDelegateObj;

    PCODE pCode;
    GCPROTECT_BEGIN(pDelegate);

    MethodTable* pMT = pDelegate->GetMethodTable();
    DelegateEEClass* pClass = (DelegateEEClass*)(pMT->GetClass());

    if (pMT->HasInstantiation())
        COMPlusThrowArgumentException(W("delegate"), W("Argument_NeedNonGenericType"));

    // If we are a delegate originally created from an unmanaged function pointer, we will simply return 
    // that function pointer.
    if (DELEGATE_MARKER_UNMANAGEDFPTR == pDelegate->GetInvocationCount())
    {
        pCode = pDelegate->GetMethodPtrAux();
    }
    else
    {
        UMEntryThunk*   pUMEntryThunk   = NULL;
        SyncBlock*      pSyncBlock      = pDelegate->GetSyncBlock();
            
        InteropSyncBlockInfo* pInteropInfo = pSyncBlock->GetInteropInfo();
        
        pUMEntryThunk = (UMEntryThunk*)pInteropInfo->GetUMEntryThunk();

        if (!pUMEntryThunk) 
        {

            UMThunkMarshInfo *pUMThunkMarshInfo = pClass->m_pUMThunkMarshInfo;
            MethodDesc *pInvokeMeth = FindDelegateInvokeMethod(pMT);

            if (!pUMThunkMarshInfo) 
            {
                GCX_PREEMP();

                pUMThunkMarshInfo = new UMThunkMarshInfo();
                pUMThunkMarshInfo->LoadTimeInit(pInvokeMeth);

                g_IBCLogger.LogEEClassCOWTableAccess(pMT);
                EnsureWritablePages(pClass);
                if (FastInterlockCompareExchangePointer(&(pClass->m_pUMThunkMarshInfo),
                                                        pUMThunkMarshInfo,
                                                        NULL ) != NULL)
                {
                    delete pUMThunkMarshInfo;
                    pUMThunkMarshInfo = pClass->m_pUMThunkMarshInfo;
                }
            }

            _ASSERTE(pUMThunkMarshInfo != NULL);
            _ASSERTE(pUMThunkMarshInfo == pClass->m_pUMThunkMarshInfo);

            pUMEntryThunk = UMEntryThunk::CreateUMEntryThunk();
            Holder<UMEntryThunk *, DoNothing, UMEntryThunk::FreeUMEntryThunk> umHolder;
            umHolder.Assign(pUMEntryThunk);

            // multicast. go thru Invoke
            OBJECTHANDLE objhnd = GetAppDomain()->CreateLongWeakHandle(pDelegate);
            _ASSERTE(objhnd != NULL);

            // This target should not ever be used. We are storing it in the thunk for better diagnostics of "call on collected delegate" crashes.
            PCODE pManagedTargetForDiagnostics = (pDelegate->GetMethodPtrAux() != NULL) ? pDelegate->GetMethodPtrAux() : pDelegate->GetMethodPtr();

            // MethodDesc is passed in for profiling to know the method desc of target
            pUMEntryThunk->LoadTimeInit(
                pManagedTargetForDiagnostics,
                objhnd,
                pUMThunkMarshInfo, pInvokeMeth);

            if (!pInteropInfo->SetUMEntryThunk(pUMEntryThunk)) 
            {
                pUMEntryThunk = (UMEntryThunk*)pInteropInfo->GetUMEntryThunk();
            }
            else
            {
                umHolder.SuppressRelease();
                // Insert the delegate handle / UMEntryThunk* into the hash
                LPVOID key = (LPVOID)pUMEntryThunk;

                // Assert that the entry isn't already in the hash.
                _ASSERTE((LPVOID)INVALIDENTRY == COMDelegate::s_pDelegateToFPtrHash->LookupValue((UPTR)key, 0));
                
                {
                    CrstHolder ch(&COMDelegate::s_DelegateToFPtrHashCrst);               
                    COMDelegate::s_pDelegateToFPtrHash->InsertValue((UPTR)key, pUMEntryThunk->GetObjectHandle());
                }
            }
            
            _ASSERTE(pUMEntryThunk != NULL);
            _ASSERTE(pUMEntryThunk == (UMEntryThunk*)pInteropInfo->GetUMEntryThunk()); 

        }
        pCode = (PCODE)pUMEntryThunk->GetCode();
    }
    
    GCPROTECT_END();
    return (LPVOID)pCode;
}

// Marshals an unmanaged callback to Delegate
//static
OBJECTREF COMDelegate::ConvertToDelegate(LPVOID pCallback, MethodTable* pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (!pCallback)
    {
        return NULL;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Check if this callback was originally a managed method passed out to unmanaged code.
    //

    UMEntryThunk* pUMEntryThunk = UMEntryThunk::Decode(pCallback);

    // Lookup the callsite in the hash, if found, we can map this call back to its managed function.
    // Otherwise, we'll treat this as an unmanaged callsite.
    // Make sure that the pointer doesn't have the value of 1 which is our hash table deleted item marker.
    LPVOID DelegateHnd = (pUMEntryThunk != NULL) && ((UPTR)pUMEntryThunk != (UPTR)1)
        ? COMDelegate::s_pDelegateToFPtrHash->LookupValue((UPTR)pUMEntryThunk, 0)
        : (LPVOID)INVALIDENTRY;

    if (DelegateHnd != (LPVOID)INVALIDENTRY)
    {
        // Found a managed callsite
        OBJECTREF pDelegate = NULL;
        GCPROTECT_BEGIN(pDelegate);

        pDelegate = ObjectFromHandle((OBJECTHANDLE)DelegateHnd);

        // Make sure we're not trying to sneak into another domain.
        SyncBlock* pSyncBlock = pDelegate->GetSyncBlock();
        _ASSERTE(pSyncBlock);
            
        InteropSyncBlockInfo* pInteropInfo = pSyncBlock->GetInteropInfo();
        _ASSERTE(pInteropInfo);
        
        pUMEntryThunk = (UMEntryThunk*)pInteropInfo->GetUMEntryThunk();
        _ASSERTE(pUMEntryThunk);

        GCPROTECT_END();
        return pDelegate;
    }
    

    //////////////////////////////////////////////////////////////////////////////////////////////////////////
    // This is an unmanaged callsite. We need to create a new delegate.
    //
    // The delegate's invoke method will point to a call thunk.
    // The call thunk will internally shuffle the args, set up a DelegateTransitionFrame, marshal the args,
    //  call the UM Function located at m_pAuxField, unmarshal the args, and return.
    // Invoke -> CallThunk -> ShuffleThunk -> Frame -> Marshal -> Call AuxField -> UnMarshal
    
    DelegateEEClass*    pClass      = (DelegateEEClass*)pMT->GetClass();
    MethodDesc*         pMD         = FindDelegateInvokeMethod(pMT);

    PREFIX_ASSUME(pClass != NULL);

    //////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Get or create the marshaling stub information
    //

    PCODE pMarshalStub = pClass->m_pMarshalStub;
    if (pMarshalStub == NULL)
    {
        GCX_PREEMP();

        pMarshalStub = GetStubForInteropMethod(pMD, 0, &(pClass->m_pForwardStubMD));

        // Save this new stub on the DelegateEEClass.       
        EnsureWritablePages(dac_cast<PVOID>(&pClass->m_pMarshalStub), sizeof(PCODE));
        InterlockedCompareExchangeT<PCODE>(&pClass->m_pMarshalStub, pMarshalStub, NULL);

        pMarshalStub = pClass->m_pMarshalStub;
    }

    // The IL marshaling stub performs the function of the shuffle thunk - it simply omits 'this' in
    // the call to unmanaged code. The stub recovers the unmanaged target from the delegate instance.

    _ASSERTE(pMarshalStub != NULL);

    ////////////////////////////////////////////////////////////////////////////////////////////////////////// 
    // Wire up the stubs to the new delegate instance.
    // 
    
    LOG((LF_INTEROP, LL_INFO10000, "Created delegate for function pointer: entrypoint: %p\n", pMarshalStub));

    // Create the new delegate
    DELEGATEREF delObj = (DELEGATEREF) pMT->Allocate();

    {
        // delObj is not protected
        GCX_NOTRIGGER();
        
        // Wire up the unmanaged call stub to the delegate.
        delObj->SetTarget(delObj);              // We are the "this" object
        
        // For X86, we save the entry point in the delegate's method pointer and the UM Callsite in the aux pointer.
        delObj->SetMethodPtr(pMarshalStub);
        delObj->SetMethodPtrAux((PCODE)pCallback);

        // Also, mark this delegate as an unmanaged function pointer wrapper.
        delObj->SetInvocationCount(DELEGATE_MARKER_UNMANAGEDFPTR);
    }

#if defined(_TARGET_X86_)
    GCPROTECT_BEGIN(delObj);

    Stub *pInterceptStub = NULL;

    {
        GCX_PREEMP();

        MethodDesc *pStubMD = pClass->m_pForwardStubMD;
        _ASSERTE(pStubMD != NULL && pStubMD->IsILStub());

    }

    if (pInterceptStub != NULL)
    {
        // install the outer-most stub to sync block
        SyncBlock *pSyncBlock = delObj->GetSyncBlock();
            
        InteropSyncBlockInfo *pInteropInfo = pSyncBlock->GetInteropInfo();
        VERIFY(pInteropInfo->SetInterceptStub(pInterceptStub));
    }

    GCPROTECT_END();
#endif // _TARGET_X86_

    return delObj;
}

#ifdef FEATURE_COMINTEROP
// Marshals a WinRT delegate interface pointer to a managed Delegate
//static
OBJECTREF COMDelegate::ConvertWinRTInterfaceToDelegate(IUnknown *pIdentity, MethodTable* pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pIdentity));
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;

    MethodDesc*         pMD         = FindDelegateInvokeMethod(pMT);

    if (pMD->IsSharedByGenericInstantiations())
    {
        // we need an exact MD to represent the call
        pMD = InstantiatedMethodDesc::FindOrCreateExactClassMethod(pMT, pMD);
    }
    else
    {
        // set up ComPlusCallInfo
        PopulateComPlusCallInfo(pMT);
    }

    ComPlusCallInfo *pComInfo = ComPlusCallInfo::FromMethodDesc(pMD);
    PCODE pMarshalStub = (pComInfo == NULL ? NULL : pComInfo->m_pILStub);

    if (pMarshalStub == NULL)
    {
        GCX_PREEMP();

        DWORD dwStubFlags = NDIRECTSTUB_FL_COM | NDIRECTSTUB_FL_WINRT | NDIRECTSTUB_FL_WINRTDELEGATE;

        pMarshalStub = GetStubForInteropMethod(pMD, dwStubFlags);

        // At this point we must have a non-NULL ComPlusCallInfo
        pComInfo = ComPlusCallInfo::FromMethodDesc(pMD);
        _ASSERTE(pComInfo != NULL);

        // Save this new stub on the ComPlusCallInfo
        InterlockedCompareExchangeT<PCODE>(EnsureWritablePages(&pComInfo->m_pILStub), pMarshalStub, NULL);

        pMarshalStub = pComInfo->m_pILStub;
    }

    _ASSERTE(pMarshalStub != NULL);

    ////////////////////////////////////////////////////////////////////////////////////////////////////////// 
    // Wire up the stub to the new delegate instance.
    // 
    
    LOG((LF_INTEROP, LL_INFO10000, "Created delegate for WinRT interface: pUnk: %p\n", pIdentity));

    // Create the new delegate
    DELEGATEREF delObj = (DELEGATEREF) pMT->Allocate();

    {
        // delObj is not protected
        GCX_NOTRIGGER();
        
        // Wire up the unmanaged call stub to the delegate.
        delObj->SetTarget(delObj);              // We are the "this" object
        
        // We save the entry point in the delegate's method pointer and the identity pUnk in the aux pointer.
        delObj->SetMethodPtr(pMarshalStub);
        delObj->SetMethodPtrAux((PCODE)pIdentity);

        // Also, mark this delegate as an unmanaged function pointer wrapper.
        delObj->SetInvocationCount(DELEGATE_MARKER_UNMANAGEDFPTR);
    }

    return delObj;
}
#endif // FEATURE_COMINTEROP

void COMDelegate::ValidateDelegatePInvoke(MethodDesc* pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;

        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END;
    
    if (pMD->IsSynchronized())
        COMPlusThrow(kTypeLoadException, IDS_EE_NOSYNCHRONIZED);

    if (pMD->MethodDesc::IsVarArg())
        COMPlusThrow(kNotSupportedException, IDS_EE_VARARG_NOT_SUPPORTED);
}

// static
PCODE COMDelegate::GetStubForILStub(EEImplMethodDesc* pDelegateMD, MethodDesc** ppStubMD, DWORD dwStubFlags)
{
    CONTRACT(PCODE)
    {
        STANDARD_VM_CHECK;

        PRECONDITION(CheckPointer(pDelegateMD));
        POSTCONDITION(RETVAL != NULL);
    }
    CONTRACT_END;

    ValidateDelegatePInvoke(pDelegateMD);

    dwStubFlags |= NDIRECTSTUB_FL_DELEGATE;

    RETURN NDirect::GetStubForILStub(pDelegateMD, ppStubMD, dwStubFlags);
}

#endif // CROSSGEN_COMPILE


// static
MethodDesc* COMDelegate::GetILStubMethodDesc(EEImplMethodDesc* pDelegateMD, DWORD dwStubFlags)
{
    STANDARD_VM_CONTRACT;

    MethodTable *pMT = pDelegateMD->GetMethodTable();

#ifdef FEATURE_COMINTEROP
    if (pMT->IsWinRTDelegate())
    {
        dwStubFlags |= NDIRECTSTUB_FL_COM | NDIRECTSTUB_FL_WINRT | NDIRECTSTUB_FL_WINRTDELEGATE;
    }
    else
#endif // FEATURE_COMINTEROP
    {
        dwStubFlags |= NDIRECTSTUB_FL_DELEGATE;
    }

    PInvokeStaticSigInfo sigInfo(pDelegateMD);
    return NDirect::CreateCLRToNativeILStub(&sigInfo, dwStubFlags, pDelegateMD);
}


#ifndef CROSSGEN_COMPILE

FCIMPL2(FC_BOOL_RET, COMDelegate::CompareUnmanagedFunctionPtrs, Object *refDelegate1UNSAFE, Object *refDelegate2UNSAFE)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(refDelegate1UNSAFE != NULL);
        PRECONDITION(refDelegate2UNSAFE != NULL);
    }
    CONTRACTL_END;

    DELEGATEREF refD1 = (DELEGATEREF) ObjectToOBJECTREF(refDelegate1UNSAFE);
    DELEGATEREF refD2 = (DELEGATEREF) ObjectToOBJECTREF(refDelegate2UNSAFE);
    BOOL ret = FALSE;
   
    // Make sure this is an unmanaged function pointer wrapped in a delegate.
    CONSISTENCY_CHECK(DELEGATE_MARKER_UNMANAGEDFPTR == refD1->GetInvocationCount());
    CONSISTENCY_CHECK(DELEGATE_MARKER_UNMANAGEDFPTR == refD2->GetInvocationCount());

    ret = (refD1->GetMethodPtr() == refD2->GetMethodPtr() &&
           refD1->GetMethodPtrAux() == refD2->GetMethodPtrAux());
    
    FC_RETURN_BOOL(ret);
}
FCIMPLEND


void COMDelegate::RemoveEntryFromFPtrHash(UPTR key)
{
    WRAPPER_NO_CONTRACT;
    
    // Remove this entry from the lookup hash.
    CrstHolder ch(&COMDelegate::s_DelegateToFPtrHashCrst);
    COMDelegate::s_pDelegateToFPtrHash->DeleteValue(key, NULL);
}

FCIMPL2(PCODE, COMDelegate::GetCallStub, Object* refThisUNSAFE, PCODE method)
{
    FCALL_CONTRACT;

    PCODE target = NULL;

    DELEGATEREF refThis = (DELEGATEREF)ObjectToOBJECTREF(refThisUNSAFE);
    HELPER_METHOD_FRAME_BEGIN_RET_1(refThis);
    MethodDesc *pMeth = MethodTable::GetMethodDescForSlotAddress((PCODE)method);
    _ASSERTE(pMeth);
    _ASSERTE(!pMeth->IsStatic() && pMeth->IsVirtual());
    target = GetVirtualCallStub(pMeth, TypeHandle(pMeth->GetMethodTable()));
    refThis->SetInvocationCount((INT_PTR)(void*)pMeth);
    HELPER_METHOD_FRAME_END();
    return target;
}
FCIMPLEND

FCIMPL3(PCODE, COMDelegate::AdjustTarget, Object* refThisUNSAFE, Object* targetUNSAFE, PCODE method)
{
    FCALL_CONTRACT;

    if (targetUNSAFE == NULL)
        FCThrow(kArgumentNullException);
    
    OBJECTREF refThis = ObjectToOBJECTREF(refThisUNSAFE);
    OBJECTREF target  = ObjectToOBJECTREF(targetUNSAFE);

    HELPER_METHOD_FRAME_BEGIN_RET_2(refThis, target);

    _ASSERTE(refThis);
    _ASSERTE(method);

    MethodTable *pMT = target->GetMethodTable();

    MethodDesc *pMeth = Entry2MethodDesc(method, pMT);
    _ASSERTE(pMeth);
    _ASSERTE(!pMeth->IsStatic());

    // close delegates
    MethodTable* pMTTarg = target->GetMethodTable();
    MethodTable* pMTMeth = pMeth->GetMethodTable();
    
    MethodDesc *pCorrectedMethod = pMeth;

    // Use the Unboxing stub for value class methods, since the value
    // class is constructed using the boxed instance.
    if (pCorrectedMethod->GetMethodTable()->IsValueType() && !pCorrectedMethod->IsUnboxingStub())
    {
        // those should have been ruled out at jit time (code:COMDelegate::GetDelegateCtor)
        _ASSERTE((pMTMeth != g_pValueTypeClass) && (pMTMeth != g_pObjectClass));
        pCorrectedMethod->CheckRestore();
        pCorrectedMethod = pMTTarg->GetBoxedEntryPointMD(pCorrectedMethod);
        _ASSERTE(pCorrectedMethod != NULL);
    }
        
    if (pMeth != pCorrectedMethod)
    {
        method = pCorrectedMethod->GetMultiCallableAddrOfCode();
    }
    HELPER_METHOD_FRAME_END();

    return method;
}
FCIMPLEND

#if defined(_MSC_VER) && !defined(FEATURE_PAL)
// VC++ Compiler intrinsic.
extern "C" void * _ReturnAddress(void);
#endif // _MSC_VER && !FEATURE_PAL

// This is the single constructor for all Delegates.  The compiler
//  doesn't provide an implementation of the Delegate constructor.  We
//  provide that implementation through an ECall call to this method.
FCIMPL3(void, COMDelegate::DelegateConstruct, Object* refThisUNSAFE, Object* targetUNSAFE, PCODE method)
{
    FCALL_CONTRACT;

    struct _gc
    {
        DELEGATEREF refThis;
        OBJECTREF target;
    } gc;

    gc.refThis = (DELEGATEREF) ObjectToOBJECTREF(refThisUNSAFE);
    gc.target  = (OBJECTREF) targetUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_PROTECT(gc);

    // via reflection you can pass in just about any value for the method.
    // we can do some basic verification up front to prevent EE exceptions.
    if (method == NULL)
        COMPlusThrowArgumentNull(W("method"));

    _ASSERTE(gc.refThis);
    _ASSERTE(method);

    //  programmers could feed garbage data to DelegateConstruct().
    // It's difficult to validate a method code pointer, but at least we'll
    // try to catch the easy garbage.
    _ASSERTE(isMemoryReadable(method, 1));
    
    MethodTable *pMTTarg = NULL;

    if (gc.target != NULL)
    {
        pMTTarg = gc.target->GetMethodTable();
    }

    MethodDesc *pMethOrig = Entry2MethodDesc(method, pMTTarg);
    MethodDesc *pMeth = pMethOrig;

    MethodTable* pDelMT = gc.refThis->GetMethodTable();

    LOG((LF_STUBS, LL_INFO1000, "In DelegateConstruct: for delegate type %s binding to method %s::%s%s, static = %d\n", 
         pDelMT->GetDebugClassName(),
         pMeth->m_pszDebugClassName, pMeth->m_pszDebugMethodName, pMeth->m_pszDebugMethodSignature, pMeth->IsStatic()));

    _ASSERTE(pMeth);

#ifdef _DEBUG
    // Assert that everything is OK...This is not some bogus
    //  address...Very unlikely that the code below would work
    //  for a random address in memory....
    MethodTable* p = pMeth->GetMethodTable();
    _ASSERTE(p);
    _ASSERTE(p->ValidateWithPossibleAV());
#endif // _DEBUG

    if (Nullable::IsNullableType(pMeth->GetMethodTable()))
        COMPlusThrow(kNotSupportedException); 

    DelegateEEClass *pDelCls = (DelegateEEClass*)pDelMT->GetClass();    
    MethodDesc *pDelegateInvoke = COMDelegate::FindDelegateInvokeMethod(pDelMT);

    MetaSig invokeSig(pDelegateInvoke);
    MetaSig methodSig(pMeth);
    UINT invokeArgCount = invokeSig.NumFixedArgs();
    UINT methodArgCount = methodSig.NumFixedArgs();
    BOOL isStatic = pMeth->IsStatic();
    if (!isStatic) 
    {
        methodArgCount++; // count 'this'
    }

    if (NeedsWrapperDelegate(pMeth))
        gc.refThis = CreateSecureDelegate(gc.refThis, NULL, pMeth);

    if (pMeth->GetLoaderAllocator()->IsCollectible())
        gc.refThis->SetMethodBase(pMeth->GetLoaderAllocator()->GetExposedObject());

    // Open delegates.
    if (invokeArgCount == methodArgCount) 
    {
        // set the target
        gc.refThis->SetTarget(gc.refThis);

        // set the shuffle thunk
        Stub *pShuffleThunk = NULL;
        if (!pMeth->IsStatic() && pMeth->HasRetBuffArg() && IsRetBuffPassedAsFirstArg()) 
            pShuffleThunk = pDelCls->m_pInstRetBuffCallStub;
        else
            pShuffleThunk = pDelCls->m_pStaticCallStub;
        if (!pShuffleThunk) 
            pShuffleThunk = SetupShuffleThunk(pDelMT, pMeth);
        
        gc.refThis->SetMethodPtr(pShuffleThunk->GetEntryPoint());

        // set the ptr aux according to what is needed, if virtual need to call make virtual stub dispatch
        if (!pMeth->IsStatic() && pMeth->IsVirtual() && !pMeth->GetMethodTable()->IsValueType())
        {
            PCODE pTargetCall = GetVirtualCallStub(pMeth, TypeHandle(pMeth->GetMethodTable()));
            gc.refThis->SetMethodPtrAux(pTargetCall);
            gc.refThis->SetInvocationCount((INT_PTR)(void *)pMeth);
        }
        else
        {
            gc.refThis->SetMethodPtrAux(method);
        }
    }
    else 
    {
        MethodTable* pMTMeth = pMeth->GetMethodTable();

        if (!pMeth->IsStatic())
        {
            if (pMTTarg)
            {
                g_IBCLogger.LogMethodTableAccess(pMTTarg);

                // Use the Unboxing stub for value class methods, since the value
                // class is constructed using the boxed instance.
                //
                // <NICE> We could get the JIT to recognise all delegate creation sequences and
                // ensure the thing is always an BoxedEntryPointStub anyway </NICE>

                if (pMTMeth->IsValueType() && !pMeth->IsUnboxingStub())
                {
                    // If these are Object/ValueType.ToString().. etc,
                    // don't need an unboxing Stub.

                    if ((pMTMeth != g_pValueTypeClass) 
                        && (pMTMeth != g_pObjectClass))
                    {
                        pMeth->CheckRestore();
                        pMeth = pMTTarg->GetBoxedEntryPointMD(pMeth);
                        _ASSERTE(pMeth != NULL);
                    }
                }
                // Only update the code address if we've decided to go to a different target...
                // <NICE> We should make sure the code address that the JIT provided to us is always the right one anyway,
                // so we don't have to do all this mucking about. </NICE>
                if (pMeth != pMethOrig)
                {
                    method = pMeth->GetMultiCallableAddrOfCode();
                }
            }

            if (gc.target == NULL)
            {
                COMPlusThrow(kArgumentException, W("Arg_DlgtNullInst"));
            }
        }
#ifdef HAS_THISPTR_RETBUF_PRECODE
        else if (pMeth->HasRetBuffArg() && IsRetBuffPassedAsFirstArg())
            method = pMeth->GetLoaderAllocator()->GetFuncPtrStubs()->GetFuncPtrStub(pMeth, PRECODE_THISPTR_RETBUF);
#endif // HAS_THISPTR_RETBUF_PRECODE

        gc.refThis->SetTarget(gc.target);
        gc.refThis->SetMethodPtr((PCODE)(void *)method);
    }
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

MethodDesc *COMDelegate::GetMethodDesc(OBJECTREF orDelegate)
{        
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    MethodDesc *pMethodHandle = NULL;

    DELEGATEREF thisDel = (DELEGATEREF) orDelegate;
    DELEGATEREF innerDel = NULL;

    INT_PTR count = thisDel->GetInvocationCount();
    if (count != 0)
    {
        // this is one of the following:
        // - multicast - _invocationList is Array && _invocationCount != 0
        // - unamanaged ftn ptr - _invocationList == NULL && _invocationCount == -1
        // - secure delegate - _invocationList is Delegate && _invocationCount != NULL
        // - virtual delegate - _invocationList == null && _invocationCount == (target MethodDesc)
        //                    or _invocationList points to a LoaderAllocator/DynamicResolver (inner open virtual delegate of a Secure Delegate)
        // in the secure delegate case we want to unwrap and return the method desc of the inner delegate
        // in the other cases we return the method desc for the invoke
        innerDel = (DELEGATEREF) thisDel->GetInvocationList();
        bool fOpenVirtualDelegate = false;

        if (innerDel != NULL) 
        {
            MethodTable *pMT = innerDel->GetMethodTable();
            if (pMT->IsDelegate()) 
                return GetMethodDesc(innerDel);
            if (!pMT->IsArray())
            {
                // must be a virtual one
                fOpenVirtualDelegate = true;
            }
        }
        else
        {
            if (count != DELEGATE_MARKER_UNMANAGEDFPTR) 
            {
                // must be a virtual one
                fOpenVirtualDelegate = true;
            }
        }

        if (fOpenVirtualDelegate)
            pMethodHandle = (MethodDesc*)thisDel->GetInvocationCount();
        else
            pMethodHandle = FindDelegateInvokeMethod(thisDel->GetMethodTable());
    }
    else
    {
        // Next, check for an open delegate
        PCODE code = thisDel->GetMethodPtrAux();

        if (code != NULL)
        {
            // Note that MethodTable::GetMethodDescForSlotAddress is significantly faster than Entry2MethodDesc
            pMethodHandle = MethodTable::GetMethodDescForSlotAddress(code);
        }
        else
        {
            MethodTable * pMT = NULL;

            // Must be a normal delegate
            code = thisDel->GetMethodPtr();

            OBJECTREF orThis = thisDel->GetTarget();
            if (orThis!=NULL)
            {
                pMT = orThis->GetMethodTable();
            }

            pMethodHandle = Entry2MethodDesc(code, pMT);
        }
    }

    _ASSERTE(pMethodHandle);
    return pMethodHandle;
}
    
OBJECTREF COMDelegate::GetTargetObject(OBJECTREF obj)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    OBJECTREF targetObject = NULL;

    DELEGATEREF thisDel = (DELEGATEREF) obj; 
    OBJECTREF innerDel = NULL;

    if (thisDel->GetInvocationCount() != 0)
    {
        // this is one of the following:
        // - multicast
        // - unmanaged ftn ptr
        // - secure delegate
        // - virtual delegate - _invocationList == null && _invocationCount == (target MethodDesc)
        //                    or _invocationList points to a LoaderAllocator/DynamicResolver (inner open virtual delegate of a Secure Delegate)
        // in the secure delegate case we want to unwrap and return the object of the inner delegate
        innerDel = (DELEGATEREF) thisDel->GetInvocationList();
        if (innerDel != NULL) 
        {
            MethodTable *pMT = innerDel->GetMethodTable();
            if (pMT->IsDelegate()) 
            {
                targetObject = GetTargetObject(innerDel);
            }
        }
    }
    
    if (targetObject == NULL)
        targetObject = thisDel->GetTarget();

    return targetObject;
}

BOOL COMDelegate::IsTrueMulticastDelegate(OBJECTREF delegate)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    BOOL isMulticast = FALSE;

    size_t invocationCount = ((DELEGATEREF)delegate)->GetInvocationCount();
    if (invocationCount)
    {
        OBJECTREF invocationList = ((DELEGATEREF)delegate)->GetInvocationList();
        if (invocationList != NULL)
        {
            MethodTable *pMT = invocationList->GetMethodTable();
            isMulticast = pMT->IsArray();
        }
    }

    return isMulticast;
}

PCODE COMDelegate::TheDelegateInvokeStub()
{                           
    CONTRACT(PCODE)
    {
        STANDARD_VM_CHECK;
        POSTCONDITION(RETVAL != NULL);
    }
    CONTRACT_END;

#if defined(_TARGET_X86_) && !defined(FEATURE_STUBS_AS_IL)
    static PCODE s_pInvokeStub;

    if (s_pInvokeStub == NULL)
    {
        CPUSTUBLINKER sl;
        sl.EmitDelegateInvoke();
        // Process-wide singleton stub that never unloads
        Stub *pCandidate = sl.Link(SystemDomain::GetGlobalLoaderAllocator()->GetStubHeap(), NEWSTUB_FL_MULTICAST);

        if (InterlockedCompareExchangeT<PCODE>(&s_pInvokeStub, pCandidate->GetEntryPoint(), NULL) != NULL)
        {
            // if we are here someone managed to set the stub before us so we release the current
            pCandidate->DecRef();
        }
    }

    RETURN s_pInvokeStub;
#else
    RETURN GetEEFuncEntryPoint(SinglecastDelegateInvokeStub);
#endif // _TARGET_X86_ && !FEATURE_STUBS_AS_IL
}

// Get the cpu stub for a delegate invoke.
PCODE COMDelegate::GetInvokeMethodStub(EEImplMethodDesc* pMD)
{
    CONTRACT(PCODE)
    {
        STANDARD_VM_CHECK;
        POSTCONDITION(RETVAL != NULL);

        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACT_END;

    PCODE               ret = NULL;
    MethodTable *       pDelMT = pMD->GetMethodTable();
    DelegateEEClass*    pClass = (DelegateEEClass*) pDelMT->GetClass();

    if (pMD == pClass->GetInvokeMethod())
    {
        // Validate the invoke method, which at the moment just means checking the calling convention

        if (*pMD->GetSig() != (IMAGE_CEE_CS_CALLCONV_HASTHIS | IMAGE_CEE_CS_CALLCONV_DEFAULT))
            COMPlusThrow(kInvalidProgramException);

        ret = COMDelegate::TheDelegateInvokeStub();
    }
    else
    {

        // Since we do not support asynchronous delegates in CoreCLR, we much ensure that it was indeed a async delegate call
        // and not an invalid-delegate-layout condition. 
        // 
        // If the call was indeed for async delegate invocation, we will just throw an exception.
        if ((pMD == pClass->GetBeginInvokeMethod()) || (pMD == pClass->GetEndInvokeMethod()))
        {
            COMPlusThrow(kPlatformNotSupportedException);
        }


        _ASSERTE(!"Bad Delegate layout");
        COMPlusThrow(kInvalidProgramException);
    }

    RETURN ret;
}

FCIMPL1(Object*, COMDelegate::InternalAlloc, ReflectClassBaseObject * pTargetUNSAFE)
{
    FCALL_CONTRACT;

    REFLECTCLASSBASEREF refTarget = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTargetUNSAFE);
    OBJECTREF refRetVal = NULL;
    TypeHandle targetTH = refTarget->GetType();
    HELPER_METHOD_FRAME_BEGIN_RET_1(refTarget);

    _ASSERTE(targetTH.GetMethodTable() != NULL && targetTH.GetMethodTable()->IsDelegate());
    
    refRetVal = targetTH.GetMethodTable()->Allocate();

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(refRetVal);
}
FCIMPLEND

FCIMPL1(Object*, COMDelegate::InternalAllocLike, Object* pThis)
{
    FCALL_CONTRACT;

    OBJECTREF refRetVal = NULL;
    HELPER_METHOD_FRAME_BEGIN_RET_NOPOLL();

    _ASSERTE(pThis->GetMethodTable() != NULL && pThis->GetMethodTable()->IsDelegate());
    
    refRetVal = pThis->GetMethodTable()->AllocateNoChecks();

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(refRetVal);
}
FCIMPLEND

FCIMPL2(FC_BOOL_RET, COMDelegate::InternalEqualTypes, Object* pThis, Object *pThat)
{
    FCALL_CONTRACT;

    MethodTable *pThisMT = pThis->GetMethodTable();
    MethodTable *pThatMT = pThat->GetMethodTable();

    _ASSERTE(pThisMT != NULL && pThisMT->IsDelegate());
    _ASSERTE(pThatMT != NULL);
    
    BOOL bResult = (pThisMT == pThatMT);

    if (!bResult)
    {
        HELPER_METHOD_FRAME_BEGIN_RET_0();
        bResult = pThisMT->IsEquivalentTo(pThatMT);
        HELPER_METHOD_FRAME_END();
    }

    FC_RETURN_BOOL(bResult);
}
FCIMPLEND

#endif // CROSSGEN_COMPILE

BOOL COMDelegate::NeedsWrapperDelegate(MethodDesc* pTargetMD)
{
    LIMITED_METHOD_CONTRACT;

#ifdef _TARGET_ARM_
    // For arm VSD expects r4 to contain the indirection cell. However r4 is a non-volatile register
    // and its value must be preserved. So we need to erect a frame and store indirection cell in r4 before calling
    // virtual stub dispatch. Erecting frame is already done by secure delegates so the secureDelegate infrastructure
    //  can easliy be used for our purpose.
    // set needsSecureDelegate flag in order to erect a frame. (Secure Delegate stub also loads the right value in r4)
    if (!pTargetMD->IsStatic() && pTargetMD->IsVirtual() && !pTargetMD->GetMethodTable()->IsValueType())
        return TRUE;
#endif

     return FALSE;
}


#ifndef CROSSGEN_COMPILE

// to create a secure delegate wrapper we need:
// - the delegate to forward to         -> _invocationList
// - the creator assembly               -> _methodAuxPtr
// - the delegate invoke MethodDesc     -> _count
// the 2 fields used for invocation will contain:
// - the delegate itself                -> _pORField
// - the secure stub                    -> _pFPField
DELEGATEREF COMDelegate::CreateSecureDelegate(DELEGATEREF delegate, MethodDesc* pCreatorMethod, MethodDesc* pTargetMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    MethodTable *pDelegateType = delegate->GetMethodTable();
    MethodDesc *pMD = ((DelegateEEClass*)(pDelegateType->GetClass()))->GetInvokeMethod();
    // allocate the object
    struct _gc {
        DELEGATEREF refSecDel;
        DELEGATEREF innerDel;
    } gc;
    gc.refSecDel = delegate; 
    gc.innerDel = NULL;

    GCPROTECT_BEGIN(gc);

    // set the proper fields
    //

    // Object reference field... 
    gc.refSecDel->SetTarget(gc.refSecDel);       

    // save the secure invoke stub.  GetSecureInvoke() can trigger GC.
    PCODE tmp = GetSecureInvoke(pMD);
    gc.refSecDel->SetMethodPtr(tmp);       
    // save the assembly
    gc.refSecDel->SetMethodPtrAux((PCODE)(void *)pCreatorMethod);
    // save the delegate MethodDesc for the frame
    gc.refSecDel->SetInvocationCount((INT_PTR)pMD);
    
    // save the delegate to forward to
    gc.innerDel = (DELEGATEREF) pDelegateType->Allocate();
    gc.refSecDel->SetInvocationList(gc.innerDel); 

    if (pCreatorMethod != NULL)
    {
        // If the pCreatorMethod is a collectible method, then stash a reference to the 
        // LoaderAllocator/DynamicResolver of the collectible assembly/method in the invocationList 
        // of the inner delegate
        // (The invocationList of the inner delegate is the only field garaunteed to be unused for
        //  other purposes at this time.)
        if (pCreatorMethod->IsLCGMethod())
        {
            OBJECTREF refCollectible = pCreatorMethod->AsDynamicMethodDesc()->GetLCGMethodResolver()->GetManagedResolver();
            gc.innerDel->SetInvocationList(refCollectible);
        }
        else if (pCreatorMethod->GetLoaderAllocator()->IsCollectible())
        {
            OBJECTREF refCollectible = pCreatorMethod->GetLoaderAllocator()->GetExposedObject();
            gc.innerDel->SetInvocationList(refCollectible);
        }
    }

    GCPROTECT_END();

    return gc.innerDel;
}

// InternalGetMethodInfo
// This method will get the MethodInfo for a delegate
FCIMPL1(ReflectMethodObject *, COMDelegate::FindMethodHandle, Object* refThisIn)
{
    FCALL_CONTRACT;

    MethodDesc* pMD = NULL;
    REFLECTMETHODREF pRet = NULL;
    OBJECTREF refThis = ObjectToOBJECTREF(refThisIn);

    HELPER_METHOD_FRAME_BEGIN_RET_1(refThis);
    
    pMD = GetMethodDesc(refThis);
    pRet = pMD->GetStubMethodInfo();
    HELPER_METHOD_FRAME_END();

    return (ReflectMethodObject*)OBJECTREFToObject(pRet);
}
FCIMPLEND

FCIMPL2(FC_BOOL_RET, COMDelegate::InternalEqualMethodHandles, Object *refLeftIn, Object *refRightIn)
{
    FCALL_CONTRACT;

    OBJECTREF refLeft = ObjectToOBJECTREF(refLeftIn);
    OBJECTREF refRight = ObjectToOBJECTREF(refRightIn);
    BOOL fRet = FALSE;

    HELPER_METHOD_FRAME_BEGIN_RET_2(refLeft, refRight);
    
    MethodDesc* pMDLeft = GetMethodDesc(refLeft);
    MethodDesc* pMDRight = GetMethodDesc(refRight);
    fRet = pMDLeft == pMDRight;
    
    HELPER_METHOD_FRAME_END();

    FC_RETURN_BOOL(fRet);
}
FCIMPLEND

FCIMPL1(MethodDesc*, COMDelegate::GetInvokeMethod, Object* refThisIn)
{
    FCALL_CONTRACT;

    OBJECTREF refThis = ObjectToOBJECTREF(refThisIn);
    MethodTable * pDelMT = refThis->GetMethodTable();

    MethodDesc* pMD = ((DelegateEEClass*)(pDelMT->GetClass()))->GetInvokeMethod();
    _ASSERTE(pMD);
    return pMD;
}
FCIMPLEND

#ifdef FEATURE_MULTICASTSTUB_AS_IL
FCIMPL1(PCODE, COMDelegate::GetMulticastInvoke, Object* refThisIn)
{
    FCALL_CONTRACT;

    OBJECTREF refThis = ObjectToOBJECTREF(refThisIn);
    MethodTable *pDelegateMT = refThis->GetMethodTable();

    DelegateEEClass *delegateEEClass = ((DelegateEEClass*)(pDelegateMT->GetClass()));
    Stub *pStub = delegateEEClass->m_pMultiCastInvokeStub;
    if (pStub == NULL)
    {
        MethodDesc* pMD = delegateEEClass->GetInvokeMethod();
    
        HELPER_METHOD_FRAME_BEGIN_RET_0();

        GCX_PREEMP();

        MetaSig sig(pMD);

        BOOL fReturnVal = !sig.IsReturnTypeVoid();
        
        SigTypeContext emptyContext;
        ILStubLinker sl(pMD->GetModule(), pMD->GetSignature(), &emptyContext, pMD, TRUE, TRUE, FALSE);

        ILCodeStream *pCode = sl.NewCodeStream(ILStubLinker::kDispatch);

        DWORD dwInvocationCountNum = pCode->NewLocal(ELEMENT_TYPE_I4);
        DWORD dwLoopCounterNum = pCode->NewLocal(ELEMENT_TYPE_I4);

        DWORD dwReturnValNum = -1;
        if(fReturnVal)
            dwReturnValNum = pCode->NewLocal(sig.GetRetTypeHandleNT());

        ILCodeLabel *nextDelegate = pCode->NewCodeLabel();
        ILCodeLabel *endOfMethod = pCode->NewCodeLabel();

        // Get count of delegates
        pCode->EmitLoadThis();
        pCode->EmitLDFLD(pCode->GetToken(MscorlibBinder::GetField(FIELD__MULTICAST_DELEGATE__INVOCATION_COUNT)));
        pCode->EmitSTLOC(dwInvocationCountNum);

        // initialize counter
        pCode->EmitLDC(0);
        pCode->EmitSTLOC(dwLoopCounterNum);

        //Label_nextDelegate:
        pCode->EmitLabel(nextDelegate);

#ifdef DEBUGGING_SUPPORTED
        pCode->EmitLoadThis();
        pCode->EmitLDLOC(dwLoopCounterNum);
        pCode->EmitCALL(METHOD__STUBHELPERS__MULTICAST_DEBUGGER_TRACE_HELPER, 2, 0);
#endif // DEBUGGING_SUPPORTED

        // compare LoopCounter with InvocationCount. If equal then branch to Label_endOfMethod
        pCode->EmitLDLOC(dwLoopCounterNum);
        pCode->EmitLDLOC(dwInvocationCountNum);
        pCode->EmitBEQ(endOfMethod);

        // Load next delegate from array using LoopCounter as index
        pCode->EmitLoadThis();
        pCode->EmitLDFLD(pCode->GetToken(MscorlibBinder::GetField(FIELD__MULTICAST_DELEGATE__INVOCATION_LIST)));
        pCode->EmitLDLOC(dwLoopCounterNum);
        pCode->EmitLDELEM_REF();

        // Load the arguments
        UINT paramCount = 0;
        while(paramCount < sig.NumFixedArgs())
            pCode->EmitLDARG(paramCount++);

        // call the delegate
        pCode->EmitCALL(pCode->GetToken(pMD), sig.NumFixedArgs(), fReturnVal);

        // Save return value.
        if(fReturnVal)
            pCode->EmitSTLOC(dwReturnValNum);

        // increment counter
        pCode->EmitLDLOC(dwLoopCounterNum);
        pCode->EmitLDC(1);
        pCode->EmitADD();
        pCode->EmitSTLOC(dwLoopCounterNum);

        // branch to next delegate
        pCode->EmitBR(nextDelegate);

        //Label_endOfMethod
        pCode->EmitLabel(endOfMethod);

        // load the return value. return value from the last delegate call is returned
        if(fReturnVal)
            pCode->EmitLDLOC(dwReturnValNum);

        // return
        pCode->EmitRET();

        PCCOR_SIGNATURE pSig;
        DWORD cbSig;

        pMD->GetSig(&pSig,&cbSig);

        MethodDesc* pStubMD = ILStubCache::CreateAndLinkNewILStubMethodDesc(pMD->GetLoaderAllocator(),
                                                               pMD->GetMethodTable(),
                                                               ILSTUB_MULTICASTDELEGATE_INVOKE,
                                                               pMD->GetModule(),
                                                               pSig, cbSig,
                                                               NULL,
                                                               &sl);
    
        pStub = Stub::NewStub(JitILStub(pStubMD));

        g_IBCLogger.LogEEClassCOWTableAccess(pDelegateMT);

        InterlockedCompareExchangeT<PTR_Stub>(EnsureWritablePages(&delegateEEClass->m_pMultiCastInvokeStub), pStub, NULL);

        HELPER_METHOD_FRAME_END();
    }

    return pStub->GetEntryPoint();
}
FCIMPLEND

#else // FEATURE_MULTICASTSTUB_AS_IL

FCIMPL1(PCODE, COMDelegate::GetMulticastInvoke, Object* refThisIn)
{
    FCALL_CONTRACT;

    OBJECTREF refThis = ObjectToOBJECTREF(refThisIn);
    MethodTable *pDelegateMT = refThis->GetMethodTable();

    DelegateEEClass *delegateEEClass = ((DelegateEEClass*)(pDelegateMT->GetClass()));
    Stub *pStub = delegateEEClass->m_pMultiCastInvokeStub;
    if (pStub == NULL)
    {
        MethodDesc* pMD = delegateEEClass->GetInvokeMethod();
    
        HELPER_METHOD_FRAME_BEGIN_RET_0();

        GCX_PREEMP();

        MetaSig sig(pMD);

        UINT_PTR hash = CPUSTUBLINKER::HashMulticastInvoke(&sig);

        pStub = m_pMulticastStubCache->GetStub(hash);
        if (!pStub)
        {
            CPUSTUBLINKER sl;

            LOG((LF_CORDB,LL_INFO10000, "COMD::GIMS making a multicast delegate\n"));

            sl.EmitMulticastInvoke(hash);

            // The cache is process-wide, based on signature.  It never unloads
            Stub *pCandidate = sl.Link(SystemDomain::GetGlobalLoaderAllocator()->GetStubHeap(), NEWSTUB_FL_MULTICAST);

            Stub *pWinner = m_pMulticastStubCache->AttemptToSetStub(hash,pCandidate);
            pCandidate->DecRef();
            if (!pWinner)
                COMPlusThrowOM();

            LOG((LF_CORDB,LL_INFO10000, "Putting a MC stub at 0x%x (code:0x%x)\n",
                pWinner, (BYTE*)pWinner+sizeof(Stub)));

            pStub = pWinner;
        }

        g_IBCLogger.LogEEClassCOWTableAccess(pDelegateMT);

        // we don't need to do an InterlockedCompareExchange here - the m_pMulticastStubCache->AttemptToSetStub
        // will make sure all threads racing here will get the same stub, so they'll all store the same value
        EnsureWritablePages(&delegateEEClass->m_pMultiCastInvokeStub);
        delegateEEClass->m_pMultiCastInvokeStub = pStub;

        HELPER_METHOD_FRAME_END();
    }

    return pStub->GetEntryPoint();
}
FCIMPLEND
#endif // FEATURE_MULTICASTSTUB_AS_IL

#ifdef FEATURE_STUBS_AS_IL
PCODE COMDelegate::GetSecureInvoke(MethodDesc* pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    MethodTable *       pDelegateMT = pMD->GetMethodTable();
    DelegateEEClass*    delegateEEClass = (DelegateEEClass*) pDelegateMT->GetClass();
    Stub *pStub = delegateEEClass->m_pSecureDelegateInvokeStub;

    if (pStub == NULL)
    {

        GCX_PREEMP();

        MetaSig sig(pMD);

        BOOL fReturnVal = !sig.IsReturnTypeVoid();

        SigTypeContext emptyContext;
        ILStubLinker sl(pMD->GetModule(), pMD->GetSignature(), &emptyContext, pMD, TRUE, TRUE, FALSE);

        ILCodeStream *pCode = sl.NewCodeStream(ILStubLinker::kDispatch);

        // Load the "real" delegate
        pCode->EmitLoadThis();
        pCode->EmitLDFLD(pCode->GetToken(MscorlibBinder::GetField(FIELD__MULTICAST_DELEGATE__INVOCATION_LIST)));

        // Load the arguments
        UINT paramCount = 0;
        while(paramCount < sig.NumFixedArgs())
            pCode->EmitLDARG(paramCount++);

        // Call the delegate
        pCode->EmitCALL(pCode->GetToken(pMD), sig.NumFixedArgs(), fReturnVal);

        // Return
        pCode->EmitRET();

        PCCOR_SIGNATURE pSig;
        DWORD cbSig;

        pMD->GetSig(&pSig,&cbSig);

        MethodDesc* pStubMD =
            ILStubCache::CreateAndLinkNewILStubMethodDesc(pMD->GetLoaderAllocator(),
                                                          pMD->GetMethodTable(),
                                                          ILSTUB_SECUREDELEGATE_INVOKE,
                                                          pMD->GetModule(),
                                                          pSig, cbSig,
                                                          NULL,
                                                          &sl);

        pStub = Stub::NewStub(JitILStub(pStubMD));

        g_IBCLogger.LogEEClassCOWTableAccess(pDelegateMT);

        InterlockedCompareExchangeT<PTR_Stub>(EnsureWritablePages(&delegateEEClass->m_pSecureDelegateInvokeStub), pStub, NULL);

    }
    return pStub->GetEntryPoint();
}
#else // FEATURE_STUBS_AS_IL
PCODE COMDelegate::GetSecureInvoke(MethodDesc* pMD)
{
    CONTRACT (PCODE)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(RETVAL != NULL);
    }
    CONTRACT_END;

    MethodTable *       pDelegateMT = pMD->GetMethodTable();
    DelegateEEClass*    delegateEEClass = (DelegateEEClass*) pDelegateMT->GetClass();

    Stub *pStub = delegateEEClass->m_pSecureDelegateInvokeStub;

    if (pStub == NULL)
    {
        GCX_PREEMP();

        MetaSig sig(pMD);

        UINT_PTR hash = CPUSTUBLINKER::HashMulticastInvoke(&sig);

        pStub = m_pSecureDelegateStubCache->GetStub(hash);
        if (!pStub)
        {
            CPUSTUBLINKER sl;

            LOG((LF_CORDB,LL_INFO10000, "COMD::GIMS making a multicast delegate\n"));
            sl.EmitSecureDelegateInvoke(hash);

            // The cache is process-wide, based on signature.  It never unloads
            Stub *pCandidate = sl.Link(SystemDomain::GetGlobalLoaderAllocator()->GetStubHeap(), NEWSTUB_FL_MULTICAST);

            Stub *pWinner = m_pSecureDelegateStubCache->AttemptToSetStub(hash, pCandidate);
            pCandidate->DecRef();
            if (!pWinner)
                COMPlusThrowOM();

            LOG((LF_CORDB,LL_INFO10000, "Putting a MC stub at 0x%x (code:0x%x)\n",
                pWinner, (BYTE*)pWinner+sizeof(Stub)));

            pStub = pWinner;
        }

        g_IBCLogger.LogEEClassCOWTableAccess(pDelegateMT);
        EnsureWritablePages(&delegateEEClass->m_pSecureDelegateInvokeStub);
        delegateEEClass->m_pSecureDelegateInvokeStub = pStub;
    }
    RETURN (pStub->GetEntryPoint());
}
#endif // FEATURE_STUBS_AS_IL

#endif // CROSSGEN_COMPILE


static BOOL IsLocationAssignable(TypeHandle fromHandle, TypeHandle toHandle, BOOL relaxedMatch, BOOL fromHandleIsBoxed)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    // Identical types are obviously compatible.
    if (fromHandle == toHandle)
        return TRUE;

    // Byref parameters can never be allowed relaxed matching since type safety will always be violated in one
    // of the two directions (in or out). Checking one of the types is enough since a byref type is never
    // compatible with a non-byref type.
    if (fromHandle.IsByRef())
        relaxedMatch = FALSE;

    // If we allow relaxed matching then any subtype of toHandle is probably
    // compatible (definitely so if we know fromHandle is coming from a boxed
    // value such as we get from the bound argument in a closed delegate).
    if (relaxedMatch && fromHandle.CanCastTo(toHandle))
    {
        // If the fromHandle isn't boxed then we need to be careful since
        // non-object reference arguments aren't going to be compatible with
        // object reference locations (there's no implicit boxing going to happen
        // for us).
        if (!fromHandleIsBoxed)
        {
            // Check that the "objrefness" of source and destination matches. In
            // reality there are only three objref classes that would have
            // passed the CanCastTo above given a value type source (Object,
            // ValueType and Enum), but why hard code these in when we can be
            // more robust?
            if (fromHandle.IsGenericVariable())
            {
                TypeVarTypeDesc *fromHandleVar = fromHandle.AsGenericVariable();

                // We need to check whether constraints of fromHandle have been loaded, because the
                // CanCastTo operation might have made its decision without enumerating constraints
                // (e.g. when toHandle is System.Object).
                if (!fromHandleVar->ConstraintsLoaded())
                    fromHandleVar->LoadConstraints(CLASS_DEPENDENCIES_LOADED);

                if (toHandle.IsGenericVariable())
                {
                    TypeVarTypeDesc *toHandleVar = toHandle.AsGenericVariable();

                    // Constraints of toHandleVar were not touched by CanCastTo.
                    if (!toHandleVar->ConstraintsLoaded())
                        toHandleVar->LoadConstraints(CLASS_DEPENDENCIES_LOADED);

                    // Both handles are type variables. The following table lists all possible combinations.
                    // 
                    // In brackets are results of IsConstrainedAsObjRef/IsConstrainedAsValueType
                    // 
                    //            To:| [FALSE/FALSE]         | [FALSE/TRUE]          | [TRUE/FALSE]
                    // From:         |                       |                       |
                    // --------------------------------------------------------------------------------------
                    // [FALSE/FALSE] | ERROR                 | NEVER HAPPENS         | ERROR
                    //               | we know nothing       |                       | From may be a VT
                    // --------------------------------------------------------------------------------------
                    // [FALSE/TRUE]  | ERROR                 | OK                    | ERROR
                    //               | To may be an ObjRef   | both are VT           | mismatch
                    // --------------------------------------------------------------------------------------
                    // [TRUE/FALSE]  | OK (C# compat)        | ERROR - mismatch and  | OK
                    //               | (*)                   | no such instantiation | both are ObjRef
                    // --------------------------------------------------------------------------------------

                    if (fromHandleVar->ConstrainedAsObjRef())
                    {
                        // (*) Normally we would need to check whether toHandleVar is also constrained
                        // as ObjRef here and fail if it's not. However, the C# compiler currently
                        // allows the toHandleVar constraint to be omitted and infers it. We have to
                        // follow the same rule to avoid introducing a breaking change. 
                        // 
                        // Example:
                        // class Gen<T, U> where T : class, U
                        // 
                        // For the sake of delegate co(ntra)variance, U is also regarded as being
                        // constrained as ObjRef even though it has no constraints.
                        
                        if (toHandleVar->ConstrainedAsValueType())
                        {
                            // reference type / value type mismatch
                            return FALSE;
                        }
                    }
                    else
                    {
                        if (toHandleVar->ConstrainedAsValueType())
                        {
                            // If toHandleVar is constrained as value type, fromHandle must be as well.
                            _ASSERTE(fromHandleVar->ConstrainedAsValueType());
                        }
                        else
                        {
                            // It was not possible to prove that the variables are both reference types
                            // or both value types.
                            return FALSE;
                        }
                    }
                }
                else
                {
                    // We need toHandle to be an ObjRef and fromHandle to be constrained as ObjRef,
                    // or toHandle to be a value type and fromHandle to be constrained as a value
                    // type (which must be this specific value type actually as value types are sealed).
                    
                    // Constraints of fromHandle must ensure that it will be ObjRef if toHandle is an
                    // ObjRef, and a value type if toHandle is not an ObjRef.
                    if (CorTypeInfo::IsObjRef_NoThrow(toHandle.GetInternalCorElementType()))
                    {
                        if (!fromHandleVar->ConstrainedAsObjRef())
                            return FALSE;
                    }
                    else
                    {
                        if (!fromHandleVar->ConstrainedAsValueType())
                            return FALSE;
                    }
                }
            }
            else
            {
                _ASSERTE(!toHandle.IsGenericVariable());

                // The COR element types have all the information we need.
                if (CorTypeInfo::IsObjRef_NoThrow(fromHandle.GetInternalCorElementType()) !=
                    CorTypeInfo::IsObjRef_NoThrow(toHandle.GetInternalCorElementType()))
                    return FALSE;
            }
        }

        return TRUE;
    }
    else
    {
        // they are not compatible yet enums can go into each other if their underlying element type is the same
        if (toHandle.GetVerifierCorElementType() == fromHandle.GetVerifierCorElementType()
            && (toHandle.IsEnum() || fromHandle.IsEnum()))
            return TRUE;

    }

    return FALSE;
}

MethodDesc* COMDelegate::FindDelegateInvokeMethod(MethodTable *pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(pMT->IsDelegate());

    MethodDesc * pMD = ((DelegateEEClass*)pMT->GetClass())->GetInvokeMethod();
    if (pMD == NULL)
        COMPlusThrowNonLocalized(kMissingMethodException, W("Invoke"));
    return pMD;
}

BOOL COMDelegate::IsDelegateInvokeMethod(MethodDesc *pMD)
{
    LIMITED_METHOD_CONTRACT;

    MethodTable *pMT = pMD->GetMethodTable();
    _ASSERTE(pMT->IsDelegate());

    return (pMD == ((DelegateEEClass *)pMT->GetClass())->GetInvokeMethod());
}

BOOL COMDelegate::IsMethodDescCompatible(TypeHandle   thFirstArg,
                                         TypeHandle   thExactMethodType,
                                         MethodDesc  *pTargetMethod,
                                         TypeHandle   thDelegate,
                                         MethodDesc  *pInvokeMethod,
                                         int          flags,
                                         BOOL        *pfIsOpenDelegate)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Handle easy cases first -- if there's a constraint on whether the target method is static or instance we can check that very
    // quickly.
    if (flags & DBF_StaticMethodOnly && !pTargetMethod->IsStatic())
        return FALSE;
    if (flags & DBF_InstanceMethodOnly && pTargetMethod->IsStatic())
        return FALSE;

    // we don't allow you to bind to methods on Nullable<T> because the unboxing stubs don't know how to 
    // handle this case.   
    if (!pTargetMethod->IsStatic() && Nullable::IsNullableType(pTargetMethod->GetMethodTable()))
        return FALSE;

    // Have to be careful with automatically generated array methods (Get, Set, etc.). The TypeHandle here may actually be one
    // of the "special case" MethodTables (such as Object[]) instead of an ArrayTypeDesc and our TypeHandle CanCastTo code can't
    // cope with all the different possible combinations. In general we want to normalize the TypeHandle into an ArrayTypeDesc
    // for these cases.
    if (thExactMethodType.IsArrayType() && !thExactMethodType.IsArray())
    {
        TypeHandle thElement = thExactMethodType.AsMethodTable()->GetApproxArrayElementTypeHandle();
        CorElementType etElement = thExactMethodType.AsMethodTable()->GetInternalCorElementType();
        unsigned uRank = thExactMethodType.AsMethodTable()->GetRank();

        thExactMethodType = ClassLoader::LoadArrayTypeThrowing(thElement,
                                                               etElement,
                                                               uRank,
                                                               ClassLoader::DontLoadTypes);
    }

    // Get signatures for the delegate invoke and target methods.
    MetaSig sigInvoke(pInvokeMethod, thDelegate);
    MetaSig sigTarget(pTargetMethod, thExactMethodType);

    // Check that there is no vararg mismatch.
    if (sigInvoke.IsVarArg() != sigTarget.IsVarArg())
        return FALSE;

    // The relationship between the number of arguments on the delegate invoke and target methods tells us a lot about the type of
    // delegate we'll create (open or closed over the first argument). We're getting the fixed argument counts here, which are all
    // the arguments apart from any implicit 'this' pointers.
    // On the delegate invoke side (the caller) the total number of arguments is the number of fixed args to Invoke plus one if the
    // delegate is closed over an argument (i.e. that argument is provided at delegate creation time).
    // On the target method side (the callee) the total number of arguments is the number of fixed args plus one if the target is an
    // instance method.
    // These two totals should match for any compatible delegate and target method.
    UINT numFixedInvokeArgs = sigInvoke.NumFixedArgs();
    UINT numFixedTargetArgs = sigTarget.NumFixedArgs();
    UINT numTotalTargetArgs = numFixedTargetArgs + (pTargetMethod->IsStatic() ? 0 : 1);

    // Determine whether the match (if it is otherwise compatible) would result in an open or closed delegate or is just completely
    // out of whack.
    BOOL fIsOpenDelegate;
    if (numTotalTargetArgs == numFixedInvokeArgs)
        // All arguments provided by invoke, delegate must be open.
        fIsOpenDelegate = TRUE;
    else if (numTotalTargetArgs == numFixedInvokeArgs + 1)
        // One too few arguments provided by invoke, delegate must be closed.
        fIsOpenDelegate = FALSE;
    else
        // Target method cannot possibly match the invoke method.
        return FALSE;

    // Deal with cases where the caller wants a specific type of delegate.
    if (flags & DBF_OpenDelegateOnly && !fIsOpenDelegate)
        return FALSE;
    if (flags & DBF_ClosedDelegateOnly && fIsOpenDelegate)
        return FALSE;

    // If the target (or first argument) is null, the delegate type would be closed and the caller explicitly doesn't want to allow
    // closing over null then filter that case now.
    if (flags & DBF_NeverCloseOverNull && thFirstArg.IsNull() && !fIsOpenDelegate)
        return FALSE;

    // If, on the other hand, we're looking at an open delegate but the caller has provided a target it's also not a match.
    if (fIsOpenDelegate && !thFirstArg.IsNull())
        return FALSE;

    // **********OLD COMMENT**********
    // We don't allow open delegates over virtual value type methods. That's because we currently have no way to allow the first
    // argument of the invoke method to be specified in such a way that the passed value would be both compatible with the target
    // method and type safe. Virtual methods always have an objref instance (they depend on this for the vtable lookup algorithm) so
    // we can't take a Foo& first argument like other value type methods. We also can't accept System.Object or System.ValueType in
    // the invoke signature since that's not specific enough and would allow type safety violations.
    // Someday we may invent a boxing stub which would take a Foo& passed in box it before dispatch. This is unlikely given that
    // it's a lot of work for an edge case (especially considering that open delegates over value types are always going to be
    // tightly bound to the specific value type). It would also be an odd case where merely calling a delegate would involve an
    // allocation and thus potential failure before you even entered the method.
    // So for now we simply disallow this case.
    // **********OLD COMMENT END**********
    // Actually we allow them now. We will treat them like non-virtual methods.


    // If we get here the basic shape of the signatures match up for either an open or closed delegate. Now we need to verify that
    // those signatures are type compatible. This is complicated somewhat by the matrix of delegate type to target method types
    // (open static vs closed instance etc.). Where we get the first argument type on the invoke side is controlled by open vs
    // closed: closed delegates get the type from the target, open from the first invoke method argument (which is always a fixed
    // arg). Similarly the location of the first argument type on the target method side is based on static vs instance (static from
    // the first fixed arg, instance from the type of the method).

    TypeHandle thFirstInvokeArg;
    TypeHandle thFirstTargetArg;

    // There is one edge case for an open static delegate which takes no arguments. In that case we're nearly done, just compare the
    // return types.
    if (numTotalTargetArgs == 0)
    {
        _ASSERTE(pTargetMethod->IsStatic());
        _ASSERTE(fIsOpenDelegate);

        goto CheckReturnType;
    }
                
    // Invoke side first...
    if (fIsOpenDelegate)
    {
        // No bound arguments, take first type from invoke signature.
        if (sigInvoke.NextArgNormalized() == ELEMENT_TYPE_END)
            return FALSE;
        thFirstInvokeArg = sigInvoke.GetLastTypeHandleThrowing();
    }
    else
        // We have one bound argument and the type of that is what we must compare first.
        thFirstInvokeArg = thFirstArg;

    // And now the first target method argument for comparison...
    if (pTargetMethod->IsStatic())
    {
        // The first argument for a static method is the first fixed arg.
        if (sigTarget.NextArgNormalized() == ELEMENT_TYPE_END)
            return FALSE;
        thFirstTargetArg = sigTarget.GetLastTypeHandleThrowing();

        // Delegates closed over static methods have a further constraint: the first argument of the target must be an object
        // reference type (otherwise the argument shuffling logic could get complicated).
        if (!fIsOpenDelegate)
        {
            if (thFirstTargetArg.IsGenericVariable())
            {
                // If the first argument of the target is a generic variable, it must be constrained to be an object reference.
                TypeVarTypeDesc *varFirstTargetArg = thFirstTargetArg.AsGenericVariable();
                if (!varFirstTargetArg->ConstrainedAsObjRef())
                    return FALSE;
            }
            else
            {
                // Otherwise the code:CorElementType of the argument must be classified as an object reference.
                CorElementType etFirstTargetArg = thFirstTargetArg.GetInternalCorElementType();
                if (!CorTypeInfo::IsObjRef(etFirstTargetArg))
                    return FALSE;
            }
        }
    }
    else
    {
        // The type of the first argument to an instance method is from the method type.
        thFirstTargetArg = thExactMethodType;

        // If the delegate is open and the target method is on a value type or primitive then the first argument of the invoke
        // method must be a reference to that type. So make promote the type we got from the reference to a ref. (We don't need to
        // do this for the closed instance case because there we got the invocation side type from the first arg passed in, i.e.
        // it's had the ref stripped from it implicitly).
        if (fIsOpenDelegate)
        {
            CorElementType etFirstTargetArg = thFirstTargetArg.GetInternalCorElementType();
            if (etFirstTargetArg <= ELEMENT_TYPE_R8 ||
                etFirstTargetArg == ELEMENT_TYPE_VALUETYPE ||
                etFirstTargetArg == ELEMENT_TYPE_I ||
                etFirstTargetArg == ELEMENT_TYPE_U)
                thFirstTargetArg = thFirstTargetArg.MakeByRef();
        }
    }

    // Now we have enough data to compare the first arguments on the invoke and target side. Skip this if we are closed over null
    // (we don't have enough type information for the match but it doesn't matter because the null matches all object reference
    // types, which our first arg must be in this case). We always relax signature matching for the first argument of an instance
    // method, since it's always allowable to call the method on a more derived type. In cases where we're closed over the first
    // argument we know that argument is boxed (because it was passed to us as an object). We provide this information to
    // IsLocationAssignable because it relaxes signature matching for some important cases (e.g. passing a value type to an argument
    // typed as Object).
    if (!thFirstInvokeArg.IsNull())
        if (!IsLocationAssignable(thFirstInvokeArg,
                                  thFirstTargetArg,
                                  !pTargetMethod->IsStatic() || flags & DBF_RelaxedSignature,
                                  !fIsOpenDelegate))
            return FALSE;

        // Loop over the remaining fixed args, the list should be one to one at this point.
    while (TRUE)
    {
        CorElementType etInvokeArg = sigInvoke.NextArgNormalized();
        CorElementType etTargetArg = sigTarget.NextArgNormalized();
        if (etInvokeArg == ELEMENT_TYPE_END || etTargetArg == ELEMENT_TYPE_END)
        {
            // We've reached the end of one signature. We better be at the end of the other or it's not a match.
            if (etInvokeArg != etTargetArg)
                return FALSE;
            break;
        }
        else
        {
            TypeHandle thInvokeArg = sigInvoke.GetLastTypeHandleThrowing();
            TypeHandle thTargetArg = sigTarget.GetLastTypeHandleThrowing();

            if (!IsLocationAssignable(thInvokeArg, thTargetArg, flags & DBF_RelaxedSignature, FALSE))
                return FALSE;
        }
    } 

 CheckReturnType:

    // Almost there, just compare the return types (remember that the assignment is in the other direction here, from callee to
    // caller, so switch the order of the arguments to IsLocationAssignable).
    // If we ever relax this we have to think about how to unbox this arg in the Nullable<T> case also.  
    if (!IsLocationAssignable(sigTarget.GetRetTypeHandleThrowing(),
                              sigInvoke.GetRetTypeHandleThrowing(),
                              flags & DBF_RelaxedSignature,
                              FALSE))
        return FALSE;

    // We must have a match.
    if (pfIsOpenDelegate)
        *pfIsOpenDelegate = fIsOpenDelegate;
    return TRUE;
}

MethodDesc* COMDelegate::GetDelegateCtor(TypeHandle delegateType, MethodDesc *pTargetMethod, DelegateCtorArgs *pCtorData)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    MethodDesc *pRealCtor = NULL;

    MethodTable *pDelMT = delegateType.AsMethodTable();
    DelegateEEClass *pDelCls = (DelegateEEClass*)(pDelMT->GetClass());

    MethodDesc *pDelegateInvoke = COMDelegate::FindDelegateInvokeMethod(pDelMT);

    MetaSig invokeSig(pDelegateInvoke);
    MetaSig methodSig(pTargetMethod);
    UINT invokeArgCount = invokeSig.NumFixedArgs();
    UINT methodArgCount = methodSig.NumFixedArgs();
    BOOL isStatic = pTargetMethod->IsStatic();
    LoaderAllocator *pTargetMethodLoaderAllocator = pTargetMethod->GetLoaderAllocator();
    BOOL isCollectible = pTargetMethodLoaderAllocator->IsCollectible();
    // A method that may be instantiated over a collectible type, and is static will require a delegate
    // that has the _methodBase field filled in with the LoaderAllocator of the collectible assembly
    // associated with the instantiation.
    BOOL fMaybeCollectibleAndStatic = FALSE;
   
    // Do not allow static methods with [NativeCallableAttribute] to be a delegate target.
    // A native callable method is special and allowing it to be delegate target will destabilize the runtime.
    if (pTargetMethod->HasNativeCallableAttribute())
    {
        COMPlusThrow(kNotSupportedException, W("NotSupported_NativeCallableTarget"));
    }

    if (isStatic)
    {
        // When this method is called and the method being considered is shared, we typically
        // are passed a Wrapper method for the explicit canonical instantiation. It would be illegal
        // to actually call that method, but the jit uses it as a proxy for the real instantiated
        // method, so we can't make the methoddesc apis that report that it is the shared methoddesc
        // report that it is. Hence, this collection of checks that will detect if the methoddesc
        // being used is a normal method desc to shared code, or if it is a wrapped methoddesc
        // corresponding to the actually uncallable instantiation over __Canon.
        if (pTargetMethod->GetMethodTable()->IsSharedByGenericInstantiations())
        {
            fMaybeCollectibleAndStatic = TRUE;
        }
        else if (pTargetMethod->IsSharedByGenericMethodInstantiations())
        {
            fMaybeCollectibleAndStatic = TRUE;
        }
        else if (pTargetMethod->HasMethodInstantiation())
        {
            Instantiation instantiation = pTargetMethod->GetMethodInstantiation();
            for (DWORD iParam = 0; iParam < instantiation.GetNumArgs(); iParam++)
            {
                if (instantiation[iParam] == g_pCanonMethodTableClass)
                {
                    fMaybeCollectibleAndStatic = TRUE;
                    break;
                }
            }
        }
    }

    // If this might be collectible and is static, then we will go down the slow path. Implementing
    // yet another fast path would require a methoddesc parameter, but hopefully isn't necessary.
    if (fMaybeCollectibleAndStatic)
        return NULL;

    if (!isStatic) 
        methodArgCount++; // count 'this'
    MethodDesc *pCallerMethod = (MethodDesc*)pCtorData->pMethod;

    if (NeedsWrapperDelegate(pTargetMethod))
    {
        // If we need a wrapper even it is not a secure delegate, go through slow path
        return NULL;
    }
    
    // Force the slow path for nullable so that we can give the user an error in case were the verifier is not run. 
    MethodTable* pMT = pTargetMethod->GetMethodTable();
    if (!pTargetMethod->IsStatic() && Nullable::IsNullableType(pMT))
        return NULL;

#ifdef FEATURE_COMINTEROP
    // We'll always force classic COM types to go down the slow path for security checks.
    if ((pMT->IsComObjectType() && !pMT->IsWinRTObjectType()) ||
        (pMT->IsComImport() && !pMT->IsProjectedFromWinRT()))
    {
        return NULL;
    }
#endif

    // DELEGATE KINDS TABLE
    //
    //                                  _target         _methodPtr              _methodPtrAux       _invocationList     _invocationCount
    //
    // 1- Instance closed               'this' ptr      target method           null                null                0
    // 2- Instance open non-virt        delegate        shuffle thunk           target method       null                0
    // 3- Instance open virtual         delegate        Virtual-stub dispatch   method id           null                0
    // 4- Static closed                 first arg       target method           null                null                0
    // 5- Static closed (special sig)   delegate        specialSig thunk        target method       first arg           0
    // 6- Static opened                 delegate        shuffle thunk           target method       null                0
    // 7- Secure                        delegate        call thunk              MethodDesc (frame)  target delegate     creator assembly 
    //
    // Delegate invoke arg count == target method arg count - 2, 3, 6
    // Delegate invoke arg count == 1 + target method arg count - 1, 4, 5
    //
    // 1, 4     - MulticastDelegate.ctor1 (simply assign _target and _methodPtr)
    // 5        - MulticastDelegate.ctor2 (see table, takes 3 args)
    // 2, 6     - MulticastDelegate.ctor3 (take shuffle thunk)
    // 3        - MulticastDelegate.ctor4 (take shuffle thunk, retrieve MethodDesc) ???
    //
    // 7 - Needs special handling
    //
    // With collectible types, we need to fill the _methodBase field in with a value that represents the LoaderAllocator of the target method
    // if the delegate is not a closed instance delegate.
    //
    // There are two techniques that will work for this. 
    // One is to simply use the slow path. We use this for unusual constructs. It is rather slow. 
    //  We will use this for the secure variants
    //
    // Another is to pass a gchandle to the delegate ctor. This is fastest, but only works if we can predict the gc handle at this time. 
    //  We will use this for the non secure variants

    if (invokeArgCount == methodArgCount) 
    {
        // case 2, 3, 6
        //@TODO:NEWVTWORK: Might need changing.
        // The virtual dispatch stub doesn't work on unboxed value type objects which don't have MT pointers.
        // Since open virtual (delegate kind 3) delegates on value type methods require unboxed objects we cannot use the
        // virtual dispatch stub for them. On the other hand, virtual methods on value types don't need
        // to be dispatched because value types cannot be derived. So we treat them like non-virtual methods (delegate kind 2).
        if (!isStatic && pTargetMethod->IsVirtual() && !pTargetMethod->GetMethodTable()->IsValueType()) 
        {
            // case 3
            if (isCollectible)
                pRealCtor = MscorlibBinder::GetMethod(METHOD__MULTICAST_DELEGATE__CTOR_COLLECTIBLE_VIRTUAL_DISPATCH);
            else
                pRealCtor = MscorlibBinder::GetMethod(METHOD__MULTICAST_DELEGATE__CTOR_VIRTUAL_DISPATCH);
        }
        else
        {
            // case 2, 6
            if (isCollectible)
                pRealCtor = MscorlibBinder::GetMethod(METHOD__MULTICAST_DELEGATE__CTOR_COLLECTIBLE_OPENED);
            else
                pRealCtor = MscorlibBinder::GetMethod(METHOD__MULTICAST_DELEGATE__CTOR_OPENED);
        }
        Stub *pShuffleThunk = NULL;
        if (!pTargetMethod->IsStatic() && pTargetMethod->HasRetBuffArg() && IsRetBuffPassedAsFirstArg()) 
            pShuffleThunk = pDelCls->m_pInstRetBuffCallStub;
        else
            pShuffleThunk = pDelCls->m_pStaticCallStub;

        if (!pShuffleThunk) 
            pShuffleThunk = SetupShuffleThunk(pDelMT, pTargetMethod);
        pCtorData->pArg3 = (void*)pShuffleThunk->GetEntryPoint();
        if (isCollectible)
        {
            pCtorData->pArg4 = pTargetMethodLoaderAllocator->GetLoaderAllocatorObjectHandle();
        }
    }
    else 
    {
        // case 1, 4, 5
        //TODO: need to differentiate on 5
        _ASSERTE(invokeArgCount + 1 == methodArgCount);

#ifdef HAS_THISPTR_RETBUF_PRECODE
        // Force closed delegates over static methods with return buffer to go via 
        // the slow path to create ThisPtrRetBufPrecode
        if (isStatic && pTargetMethod->HasRetBuffArg() && IsRetBuffPassedAsFirstArg())
            return NULL;
#endif

        // under the conditions below the delegate ctor needs to perform some heavy operation
        // to get the unboxing stub
        BOOL needsRuntimeInfo = !pTargetMethod->IsStatic() && 
                    pTargetMethod->GetMethodTable()->IsValueType() && !pTargetMethod->IsUnboxingStub();

        if (needsRuntimeInfo)
            pRealCtor = MscorlibBinder::GetMethod(METHOD__MULTICAST_DELEGATE__CTOR_RT_CLOSED);
        else
        {
            if (!isStatic) 
                pRealCtor = MscorlibBinder::GetMethod(METHOD__MULTICAST_DELEGATE__CTOR_CLOSED);
            else
            {
                if (isCollectible)
                {
                    pRealCtor = MscorlibBinder::GetMethod(METHOD__MULTICAST_DELEGATE__CTOR_COLLECTIBLE_CLOSED_STATIC);
                    pCtorData->pArg3 = pTargetMethodLoaderAllocator->GetLoaderAllocatorObjectHandle();
                }
                else
                {
                    pRealCtor = MscorlibBinder::GetMethod(METHOD__MULTICAST_DELEGATE__CTOR_CLOSED_STATIC);
                }
            }
        }
    }

    return pRealCtor;
}


/*@GENERICSVER: new (works for generics too)
    Does a static validation of parameters passed into a delegate constructor.


    For "new Delegate(obj.method)" where method is statically typed as "C::m" and
    the static type of obj is D (some subclass of C)...

    Params:
    instHnd : Static type of the instance, from which pFtn is obtained. Ignored if pFtn 
             is static (i.e. D)
    ftnParentHnd: Parent of the MethodDesc, pFtn, used to create the delegate (i.e. type C)
    pFtn  : (possibly shared) MethodDesc of the function pointer used to create the delegate (i.e. C::m)
    pDlgt : The delegate type (i.e. Delegate)
    module: The module scoping methodMemberRef and delegateConstructorMemberRef
    methodMemberRef: the MemberRef, MemberDef or MemberSpec of the target method  (i.e. a mdToken for C::m)
    delegateConstructorMemberRef: the MemberRef, MemberDef or MemberSpec of the delegate constructor (i.e. a mdToken for Delegate::.ctor)

    Validates the following conditions:
    1.  If the function (pFtn) is not static, pInst should be equal to the type where 
        pFtn is defined or pInst should be a parent of pFtn's type.
    2.  The signature of the function should be compatible with the signature
        of the Invoke method of the delegate type.
        The signature is retrieved from module, methodMemberRef and delegateConstructorMemberRef

    NB: Although some of these arguments are redundant, we pass them in to avoid looking up 
        information that should already be available.
        Instead of comparing type handles modulo some context, the method directly compares metadata to avoid 
        loading classes referenced in the method signatures (hence the need for the module and member refs).
        Also, because this method works directly on metadata, without allowing any additional instantiation of the
        free type variables in the signature of the method or delegate constructor, this code
        will *only* verify a constructor application at the typical (ie. formal) instantiation.
*/
/* static */
BOOL COMDelegate::ValidateCtor(TypeHandle instHnd,
                               TypeHandle ftnParentHnd, 
                               MethodDesc *pFtn, 
                               TypeHandle dlgtHnd,
                               BOOL       *pfIsOpenDelegate)

{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;

        PRECONDITION(CheckPointer(pFtn));
        PRECONDITION(!dlgtHnd.IsNull());
        PRECONDITION(!ftnParentHnd.IsNull());

        INJECT_FAULT(COMPlusThrowOM()); // from MetaSig::CompareElementType
    }
    CONTRACTL_END;

    DelegateEEClass *pdlgEEClass = (DelegateEEClass*)dlgtHnd.AsMethodTable()->GetClass();
    PREFIX_ASSUME(pdlgEEClass != NULL);
    MethodDesc *pDlgtInvoke = pdlgEEClass->GetInvokeMethod();
    if (pDlgtInvoke == NULL)
        return FALSE;
    return IsMethodDescCompatible(instHnd, ftnParentHnd, pFtn, dlgtHnd, pDlgtInvoke, DBF_RelaxedSignature, pfIsOpenDelegate);
}

BOOL COMDelegate::ValidateBeginInvoke(DelegateEEClass* pClass)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;

        PRECONDITION(CheckPointer(pClass));
        PRECONDITION(CheckPointer(pClass->GetBeginInvokeMethod()));

        // insert fault. Can the binder throw an OOM?
    }
    CONTRACTL_END;

    if (pClass->GetInvokeMethod() == NULL)
        return FALSE;

    // We check the signatures under the typical instantiation of the possibly generic class 
    MetaSig beginInvokeSig(pClass->GetBeginInvokeMethod()->LoadTypicalMethodDefinition());
    MetaSig invokeSig(pClass->GetInvokeMethod()->LoadTypicalMethodDefinition());

    if (beginInvokeSig.GetCallingConventionInfo() != (IMAGE_CEE_CS_CALLCONV_HASTHIS | IMAGE_CEE_CS_CALLCONV_DEFAULT))
        return FALSE;

    if (beginInvokeSig.NumFixedArgs() != invokeSig.NumFixedArgs() + 2)
        return FALSE;

    if (beginInvokeSig.GetRetTypeHandleThrowing() != TypeHandle(MscorlibBinder::GetClass(CLASS__IASYNCRESULT)))
        return FALSE;

    while(invokeSig.NextArg() != ELEMENT_TYPE_END)
    {
        beginInvokeSig.NextArg();
        if (beginInvokeSig.GetLastTypeHandleThrowing() != invokeSig.GetLastTypeHandleThrowing())
            return FALSE;
    }

    beginInvokeSig.NextArg();
    if (beginInvokeSig.GetLastTypeHandleThrowing()!= TypeHandle(MscorlibBinder::GetClass(CLASS__ASYNCCALLBACK)))
        return FALSE;

    beginInvokeSig.NextArg();
    if (beginInvokeSig.GetLastTypeHandleThrowing()!= TypeHandle(g_pObjectClass))
        return FALSE;

    if (beginInvokeSig.NextArg() != ELEMENT_TYPE_END)
        return FALSE;

    return TRUE;
}

BOOL COMDelegate::ValidateEndInvoke(DelegateEEClass* pClass)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;

        PRECONDITION(CheckPointer(pClass));
        PRECONDITION(CheckPointer(pClass->GetEndInvokeMethod()));

        // insert fault. Can the binder throw an OOM?
    }
    CONTRACTL_END;

    if (pClass->GetInvokeMethod() == NULL)
        return FALSE;

    // We check the signatures under the typical instantiation of the possibly generic class 
    MetaSig endInvokeSig(pClass->GetEndInvokeMethod()->LoadTypicalMethodDefinition());
    MetaSig invokeSig(pClass->GetInvokeMethod()->LoadTypicalMethodDefinition());

    if (endInvokeSig.GetCallingConventionInfo() != (IMAGE_CEE_CS_CALLCONV_HASTHIS | IMAGE_CEE_CS_CALLCONV_DEFAULT))
        return FALSE;

    if (endInvokeSig.GetRetTypeHandleThrowing() != invokeSig.GetRetTypeHandleThrowing())
        return FALSE;

    CorElementType type;
    while((type = invokeSig.NextArg()) != ELEMENT_TYPE_END)
    {
        if (type == ELEMENT_TYPE_BYREF)
        {
            endInvokeSig.NextArg();
            if (endInvokeSig.GetLastTypeHandleThrowing() != invokeSig.GetLastTypeHandleThrowing())
                return FALSE;
        }
    }

    if (endInvokeSig.NextArg() == ELEMENT_TYPE_END)
        return FALSE;

    if (endInvokeSig.GetLastTypeHandleThrowing() != TypeHandle(MscorlibBinder::GetClass(CLASS__IASYNCRESULT)))
        return FALSE;

    if (endInvokeSig.NextArg() != ELEMENT_TYPE_END)
        return FALSE;

    return TRUE;
}

BOOL COMDelegate::IsSecureDelegate(DELEGATEREF dRef)
{
    CONTRACTL
    {
        MODE_ANY;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
    DELEGATEREF innerDel = NULL;
    if (dRef->GetInvocationCount() != 0)
    {
        innerDel = (DELEGATEREF) dRef->GetInvocationList();
        if (innerDel != NULL && innerDel->GetMethodTable()->IsDelegate())
        {
            // We have a secure delegate
            return TRUE;
        }
    }        
    return FALSE;
}

#endif // !DACCESS_COMPILE


// Decides if pcls derives from Delegate.
BOOL COMDelegate::IsDelegate(MethodTable *pMT)
{
    WRAPPER_NO_CONTRACT;
    return (pMT == g_pDelegateClass) || (pMT == g_pMulticastDelegateClass) || pMT->IsDelegate();
}


#if !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)


// Helper to construct an UnhandledExceptionEventArgs.  This may fail for out-of-memory or
// other reasons.  Currently, we fall back on passing a NULL eventargs to the event sink.
// Another possibility is to have two shared immutable instances (one for isTerminating and
// another for !isTerminating).  These must be immutable because we perform no synchronization
// around delivery of unhandled exceptions.  They occur in a free-threaded manner on various
// threads.
//
// It doesn't add much value to communicate the isTerminating flag under these unusual
// conditions.
static void TryConstructUnhandledExceptionArgs(OBJECTREF *pThrowable,
                                               BOOL       isTerminating,
                                               OBJECTREF *pOutEventArgs)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    _ASSERTE(pThrowable    != NULL && IsProtectedByGCFrame(pThrowable));
    _ASSERTE(pOutEventArgs != NULL && IsProtectedByGCFrame(pOutEventArgs));
    _ASSERTE(*pOutEventArgs == NULL);

    EX_TRY
    {
        MethodTable *pMT = MscorlibBinder::GetClass(CLASS__UNHANDLED_EVENTARGS);
        *pOutEventArgs = AllocateObject(pMT);

        MethodDescCallSite ctor(METHOD__UNHANDLED_EVENTARGS__CTOR, pOutEventArgs);

        ARG_SLOT args[] =
        {
            ObjToArgSlot(*pOutEventArgs),
            ObjToArgSlot(*pThrowable),
            BoolToArgSlot(isTerminating)
        };

        ctor.Call(args);
    }
    EX_CATCH
    {
        *pOutEventArgs = NULL;      // arguably better than half-constructed object

        // It's not even worth asserting, because these aren't our bugs.
    }
    EX_END_CATCH(SwallowAllExceptions)
}


// Helper to dispatch a single unhandled exception notification, swallowing anything
// that goes wrong.
static void InvokeUnhandledSwallowing(OBJECTREF *pDelegate,
                                      OBJECTREF *pDomain,
                                      OBJECTREF *pEventArgs)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    _ASSERTE(pDelegate  != NULL && IsProtectedByGCFrame(pDelegate));
    _ASSERTE(pDomain    != NULL && IsProtectedByGCFrame(pDomain));
    _ASSERTE(pEventArgs == NULL || IsProtectedByGCFrame(pEventArgs));

    EX_TRY
    {
#if defined(FEATURE_CORRUPTING_EXCEPTIONS)
        BOOL fCanMethodHandleException = g_pConfig->LegacyCorruptedStateExceptionsPolicy();
        if (!fCanMethodHandleException)
        {
            // CSE policy has not been overridden - proceed with our checks.
            //
            // Notifications for CSE are only delivered if the delegate target follows CSE rules.
            // So, get the corruption severity of the active exception that has gone unhandled.
            //
            // By Default, assume that the active exception is not corrupting.
            CorruptionSeverity severity = NotCorrupting;
            Thread *pCurThread = GetThread();
            _ASSERTE(pCurThread != NULL);
            ThreadExceptionState *pExState = pCurThread->GetExceptionState();
            if (pExState->IsExceptionInProgress())
            {
                // If an exception is active, it implies we have a tracker for it.
                // Hence, get the corruption severity from the active exception tracker.
                severity = pExState->GetCurrentExceptionTracker()->GetCorruptionSeverity();
                _ASSERTE(severity > NotSet);
            }

            // Notifications are delivered based upon corruption severity of the exception
            fCanMethodHandleException = ExceptionNotifications::CanDelegateBeInvokedForException(pDelegate, severity);
            if (!fCanMethodHandleException)
            {
                LOG((LF_EH, LL_INFO100, "InvokeUnhandledSwallowing: ADUEN Delegate cannot be invoked for corruption severity %d\n",
                    severity));
            }
        }

        if (fCanMethodHandleException)
#endif // defined(FEATURE_CORRUPTING_EXCEPTIONS)
        {
            // We've already exercised the prestub on this delegate's COMDelegate::GetMethodDesc,
            // as part of wiring up a reliable event sink. Deliver the notification.
            ExceptionNotifications::DeliverExceptionNotification(UnhandledExceptionHandler, pDelegate, pDomain, pEventArgs);
        }
    }
    EX_CATCH
    {
        // It's not even worth asserting, because these aren't our bugs.
    }
    EX_END_CATCH(SwallowAllExceptions)
}

// The unhandled exception event is a little easier to distribute, because
// we simply swallow any failures and proceed to the next event sink.
void DistributeUnhandledExceptionReliably(OBJECTREF *pDelegate,
                                          OBJECTREF *pDomain,
                                          OBJECTREF *pThrowable,
                                          BOOL       isTerminating)
{
    CONTRACTL
    {
        NOTHROW;  
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    _ASSERTE(pDelegate  != NULL && IsProtectedByGCFrame(pDelegate));
    _ASSERTE(pDomain    != NULL && IsProtectedByGCFrame(pDomain));
    _ASSERTE(pThrowable != NULL && IsProtectedByGCFrame(pThrowable));

    EX_TRY
    {
        struct _gc
        {
            PTRARRAYREF Array;
            OBJECTREF   InnerDelegate;
            OBJECTREF   EventArgs;
        } gc;
        ZeroMemory(&gc, sizeof(gc));

        GCPROTECT_BEGIN(gc);

        // Try to construct an UnhandledExceptionEventArgs out of pThrowable & isTerminating.
        // If unsuccessful, the best we can do is pass NULL.
        TryConstructUnhandledExceptionArgs(pThrowable, isTerminating, &gc.EventArgs);

        gc.Array = (PTRARRAYREF) ((DELEGATEREF)(*pDelegate))->GetInvocationList();
        if (gc.Array == NULL || !gc.Array->GetMethodTable()->IsArray())
        {
            InvokeUnhandledSwallowing(pDelegate, pDomain, &gc.EventArgs);
        }
        else
        {
            // The _invocationCount could be less than the array size, if we are sharing
            // immutable arrays cleverly.
            INT_PTR invocationCount = ((DELEGATEREF)(*pDelegate))->GetInvocationCount();
            
            _ASSERTE(FitsInU4(invocationCount));
            DWORD cnt = static_cast<DWORD>(invocationCount);

            _ASSERTE(cnt <= gc.Array->GetNumComponents());

            for (DWORD i=0; i<cnt; i++)
            {
                gc.InnerDelegate = gc.Array->m_Array[i];
                InvokeUnhandledSwallowing(&gc.InnerDelegate, pDomain, &gc.EventArgs);
            }
        }
        GCPROTECT_END();
    }
    EX_CATCH
    {
        // It's not even worth asserting, because these aren't our bugs.
    }
    EX_END_CATCH(SwallowAllExceptions)
}

#endif // !DACCESS_COMPILE && !CROSSGEN_COMPILE
