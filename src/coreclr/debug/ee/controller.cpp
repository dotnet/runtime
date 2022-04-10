// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ==++==
//

//
// ==--==
// ****************************************************************************
// File: controller.cpp
//

//
// controller.cpp: Debugger execution control routines
//
// ****************************************************************************
// Putting code & #includes, #defines, etc, before the stdafx.h will
// cause the code,etc, to be silently ignored
#include "stdafx.h"
#include "openum.h"
#include "../inc/common.h"
#include "eeconfig.h"

#include "../../vm/methoditer.h"
#include "../../vm/tailcallhelp.h"

const char *GetTType( TraceType tt);

#define IsSingleStep(exception) ((exception) == EXCEPTION_SINGLE_STEP)





// -------------------------------------------------------------------------
//  DebuggerController routines
// -------------------------------------------------------------------------

SPTR_IMPL_INIT(DebuggerPatchTable, DebuggerController, g_patches, NULL);
SVAL_IMPL_INIT(BOOL, DebuggerController, g_patchTableValid, FALSE);

#if !defined(DACCESS_COMPILE)

DebuggerController             *DebuggerController::g_controllers = NULL;
DebuggerControllerPage         *DebuggerController::g_protections = NULL;
CrstStatic                      DebuggerController::g_criticalSection;
int                             DebuggerController::g_cTotalMethodEnter = 0;


// Is this patch at a position at which it's safe to take a stack?
bool DebuggerControllerPatch::IsSafeForStackTrace()
{
    LIMITED_METHOD_CONTRACT;

    TraceType tt = this->trace.GetTraceType();
    Module *module = this->key.module;
    BOOL managed = this->IsManagedPatch();

    // Patches placed by MgrPush can come at lots of illegal spots. Can't take a stack trace here.
    if ((module == NULL) && managed && (tt == TRACE_MGR_PUSH))
    {
        return false;
    }

    // Consider everything else legal.
    // This is a little shady for TRACE_FRAME_PUSH. But TraceFrame() needs a stackInfo
    // to get a RegDisplay (though almost nobody uses it, so perhaps it could be removed).
    return true;

}

#ifndef FEATURE_EMULATE_SINGLESTEP
// returns a pointer to the shared buffer.  each call will AddRef() the object
// before returning it so callers only need to Release() when they're finished with it.
SharedPatchBypassBuffer* DebuggerControllerPatch::GetOrCreateSharedPatchBypassBuffer()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (m_pSharedPatchBypassBuffer == NULL)
    {
        void *pSharedPatchBypassBufferRX = g_pDebugger->GetInteropSafeExecutableHeap()->Alloc(sizeof(SharedPatchBypassBuffer));
#if defined(HOST_OSX) && defined(HOST_ARM64)
        ExecutableWriterHolder<SharedPatchBypassBuffer> sharedPatchBypassBufferWriterHolder((SharedPatchBypassBuffer*)pSharedPatchBypassBufferRX, sizeof(SharedPatchBypassBuffer));
        void *pSharedPatchBypassBufferRW = sharedPatchBypassBufferWriterHolder.GetRW();
#else // HOST_OSX && HOST_ARM64
        void *pSharedPatchBypassBufferRW = pSharedPatchBypassBufferRX;
#endif // HOST_OSX && HOST_ARM64
        new (pSharedPatchBypassBufferRW) SharedPatchBypassBuffer();
        m_pSharedPatchBypassBuffer = (SharedPatchBypassBuffer*)pSharedPatchBypassBufferRX;

        _ASSERTE(m_pSharedPatchBypassBuffer);
        TRACE_ALLOC(m_pSharedPatchBypassBuffer);
    }

    m_pSharedPatchBypassBuffer->AddRef();

    return m_pSharedPatchBypassBuffer;
}
#endif // FEATURE_EMULATE_SINGLESTEP

// @todo - remove all this splicing trash
// This Sort/Splice stuff just reorders the patches within a particular chain such
// that when we iterate through by calling GetPatch() and GetNextPatch(DebuggerControllerPatch),
// we'll get patches in increasing order of DebuggerControllerTypes.
// Practically, this means that calling GetPatch() will return EnC patches before stepping patches.
//
#if 1
void DebuggerPatchTable::SortPatchIntoPatchList(DebuggerControllerPatch **ppPatch)
{
    LOG((LF_CORDB, LL_EVERYTHING, "DPT::SPIPL called.\n"));
#ifdef _DEBUG
    DebuggerControllerPatch *patchFirst
        = (DebuggerControllerPatch *) Find(Hash((*ppPatch)), Key((*ppPatch)));
    _ASSERTE(patchFirst == (*ppPatch));
    _ASSERTE((*ppPatch)->controller->GetDCType() != DEBUGGER_CONTROLLER_STATIC);
#endif //_DEBUG
    DebuggerControllerPatch *patchNext = GetNextPatch((*ppPatch));
LOG((LF_CORDB, LL_EVERYTHING, "DPT::SPIPL GetNextPatch passed\n"));
    //List contains one, (sorted) element
    if (patchNext == NULL)
    {
        LOG((LF_CORDB, LL_INFO10000,
             "DPT::SPIPL: Patch 0x%x is a sorted singleton\n", (*ppPatch)));
        return;
    }

    // If we decide to reorder the list, we'll need to keep the element
    // indexed by the hash function as the (sorted)first item.  Everything else
    // chains off this element, can can thus stay put.
    // Thus, either the element we just added is already sorted, or else we'll
    // have to move it elsewhere in the list, meaning that we'll have to swap
    // the second item & the new item, so that the index points to the proper
    // first item in the list.

    //use Cur ptr for case where patch gets appended to list
    DebuggerControllerPatch *patchCur = patchNext;

    while (patchNext != NULL &&
            ((*ppPatch)->controller->GetDCType() >
             patchNext->controller->GetDCType()) )
    {
        patchCur = patchNext;
        patchNext = GetNextPatch(patchNext);
    }

    if (patchNext == GetNextPatch((*ppPatch)))
    {
        LOG((LF_CORDB, LL_INFO10000,
             "DPT::SPIPL: Patch 0x%x is already sorted\n", (*ppPatch)));
        return; //already sorted
    }

    LOG((LF_CORDB, LL_INFO10000,
         "DPT::SPIPL: Patch 0x%x will be moved \n", (*ppPatch)));

    //remove it from the list
    SpliceOutOfList((*ppPatch));

    // the kinda neat thing is: since we put it originally at the front of the list,
    // and it's not in order, then it must be behind another element of this list,
    // so we don't have to write any 'SpliceInFrontOf' code.

    _ASSERTE(patchCur != NULL);
    SpliceInBackOf((*ppPatch), patchCur);

    LOG((LF_CORDB, LL_INFO10000,
         "DPT::SPIPL: Patch 0x%x is now sorted\n", (*ppPatch)));
}

// This can leave the list empty, so don't do this unless you put
// the patch back somewhere else.
void DebuggerPatchTable::SpliceOutOfList(DebuggerControllerPatch *patch)
{
    // We need to get iHash, the index of the ptr within
    // m_piBuckets, ie it's entry in the hashtable.
    ULONG iHash = Hash(patch) % m_iBuckets;
    ULONG iElement = m_piBuckets[iHash];
    DebuggerControllerPatch *patchFirst
        = (DebuggerControllerPatch *) EntryPtr(iElement);

    // Fix up pointers to chain
    if (patchFirst == patch)
    {
        // The first patch shouldn't have anything behind it.
        _ASSERTE(patch->entry.iPrev == DPT_INVALID_SLOT);

        if (patch->entry.iNext != DPT_INVALID_SLOT)
        {
            m_piBuckets[iHash] = patch->entry.iNext;
        }
        else
        {
            m_piBuckets[iHash] = DPT_INVALID_SLOT;
        }
    }

    if (patch->entry.iNext != DPT_INVALID_SLOT)
    {
        EntryPtr(patch->entry.iNext)->iPrev = patch->entry.iPrev;
    }

    if (patch->entry.iPrev != DPT_INVALID_SLOT)
    {
        EntryPtr(patch->entry.iNext)->iNext = patch->entry.iNext;
    }

    patch->entry.iNext = DPT_INVALID_SLOT;
    patch->entry.iPrev = DPT_INVALID_SLOT;
}

void DebuggerPatchTable::SpliceInBackOf(DebuggerControllerPatch *patchAppend,
                                        DebuggerControllerPatch *patchEnd)
{
    ULONG iAppend = ItemIndex((HASHENTRY*)patchAppend);
    ULONG iEnd = ItemIndex((HASHENTRY*)patchEnd);

    patchAppend->entry.iPrev = iEnd;
    patchAppend->entry.iNext = patchEnd->entry.iNext;

    if (patchAppend->entry.iNext != DPT_INVALID_SLOT)
        EntryPtr(patchAppend->entry.iNext)->iPrev = iAppend;

    patchEnd->entry.iNext = iAppend;
}
#endif

//-----------------------------------------------------------------------------
// Stack safety rules.
// In general, we're safe to crawl whenever we're in preemptive mode.
// We're also must be safe at any spot the thread could get synchronized,
// because that means that the thread will be stopped to let the debugger shell
// inspect it and that can definitely take stack traces.
// Basically the only unsafe spot is in the middle of goofy stub with some
// partially constructed frame while in coop mode.
//-----------------------------------------------------------------------------

// Safe if we're at certain types of patches.
// See Patch::IsSafeForStackTrace for details.
StackTraceTicket::StackTraceTicket(DebuggerControllerPatch * patch)
{
    _ASSERTE(patch != NULL);
    _ASSERTE(patch->IsSafeForStackTrace());
}

// Safe if there was already another stack trace at this spot. (Grandfather clause)
// This is commonly used for StepOut, which takes runs stacktraces to crawl up
// the stack to find a place to patch.
StackTraceTicket::StackTraceTicket(ControllerStackInfo * info)
{
    _ASSERTE(info != NULL);

    // Ensure that the other stack info object actually executed (and thus was
    // actually valid).
    _ASSERTE(info->m_dbgExecuted);
}

// Safe b/c the context shows we're in native managed code.
// This must be safe because we could always set a managed breakpoint by native
// offset and thus synchronize the shell at this spot. So this is
// a specific example of the Synchronized case. The fact that we don't actually
// synchronize doesn't make us any less safe.
StackTraceTicket::StackTraceTicket(const BYTE * ip)
{
    _ASSERTE(g_pEEInterface->IsManagedNativeCode(ip));
}

// Safe it we're at a Synchronized point point.
StackTraceTicket::StackTraceTicket(Thread * pThread)
{
    _ASSERTE(pThread != NULL);

    // If we're synchronized, the debugger should be stopped.
    // That means all threads are synced and must be safe to take a stacktrace.
    // Thus we don't even need to do a thread-specific check.
    _ASSERTE(g_pDebugger->IsStopped());
}

// DebuggerUserBreakpoint has a special case of safety. See that ctor for details.
StackTraceTicket::StackTraceTicket(DebuggerUserBreakpoint * p)
{
    _ASSERTE(p != NULL);
}

//void ControllerStackInfo::GetStackInfo():   GetStackInfo
//      is invoked by the user to trigger the stack walk.  This will
//      cause the stack walk detailed in the class description to happen.
// Thread* thread:  The thread to do the stack walk on.
// void* targetFP:  Can be either NULL (meaning that the bottommost
//      frame is the target), or an frame pointer, meaning that the
//      caller wants information about a specific frame.
// CONTEXT* pContext:  A pointer to a CONTEXT structure.  Can be null,
// we use our temp context.
// bool suppressUMChainFromComPlusMethodFrameGeneric - A ridiculous flag that is trying to narrowly
//      target a fix for issue 650903.
// StackTraceTicket - ticket to ensure that we actually have permission for this stacktrace
void ControllerStackInfo::GetStackInfo(
    StackTraceTicket ticket,
    Thread *thread,
    FramePointer targetFP,
    CONTEXT *pContext,
    bool suppressUMChainFromComPlusMethodFrameGeneric
    )
{
    _ASSERTE(thread != NULL);

    BOOL contextValid = (pContext != NULL);
    if (!contextValid)
    {
        // We're assuming the thread is protected w/ a frame (which includes the redirection
        // case). The stackwalker will use that protection to prime the context.
        pContext = &this->m_tempContext;
    }
    else
    {
        // If we provided an explicit context for this thread, it better not be redirected.
        _ASSERTE(!ISREDIRECTEDTHREAD(thread));
    }

    // Mark this stackwalk as valid so that it can in turn be used to grandfather
    // in other stackwalks.
    INDEBUG(m_dbgExecuted = true);

    m_activeFound = false;
    m_returnFound = false;
    m_bottomFP  = LEAF_MOST_FRAME;
    m_targetFP  = targetFP;
    m_targetFrameFound = (m_targetFP == LEAF_MOST_FRAME);
    m_specialChainReason = CHAIN_NONE;
    m_suppressUMChainFromComPlusMethodFrameGeneric = suppressUMChainFromComPlusMethodFrameGeneric;

    int result = DebuggerWalkStack(thread,
                                   LEAF_MOST_FRAME,
                                   pContext,
                                   contextValid,
                                   WalkStack,
                                   (void *) this,
                                   FALSE);

    _ASSERTE(m_activeFound); // All threads have at least one unmanaged frame

    if (result == SWA_DONE)
    {
        _ASSERTE(!HasReturnFrame()); // We didn't find a managed return frame
        _ASSERTE(HasReturnFrame(true)); // All threads have at least one unmanaged frame
    }
}

//---------------------------------------------------------------------------------------
//
// This function "undoes" an unwind, i.e. it takes the active frame (the current frame)
// and sets it to be the return frame (the caller frame).  Currently it is only used by
// the stepper to step out of an LCG method.  See DebuggerStepper::DetectHandleLCGMethods()
// for more information.
//
// Assumptions:
//    The current frame is valid on entry.
//
// Notes:
//    After this function returns, the active frame on this instance of ControllerStackInfo will no longer be valid.
//
//    This function is specifically for DebuggerStepper::DetectHandleLCGMethods().  Using it in other scencarios may
//    require additional changes.
//

void ControllerStackInfo::SetReturnFrameWithActiveFrame()
{
    // Copy the active frame into the return frame.
    m_returnFound = true;
    m_returnFrame = m_activeFrame;

    // Invalidate the active frame.
    m_activeFound = false;
    memset(&(m_activeFrame), 0, sizeof(m_activeFrame));
    m_activeFrame.fp = LEAF_MOST_FRAME;
}

// Fill in a controller-stack info.
StackWalkAction ControllerStackInfo::WalkStack(FrameInfo *pInfo, void *data)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(!pInfo->HasStubFrame()); // we didn't ask for stub frames.

    ControllerStackInfo *i = (ControllerStackInfo *) data;

    //save this info away for later use
    if (i->m_bottomFP == LEAF_MOST_FRAME)
        i->m_bottomFP = pInfo->fp;

    // This is part of the targetted fix for issue 650903. (See the other
    // parts in in code:TrackUMChain and code:DebuggerStepper::TrapStepOut.)
    // pInfo->fIgnoreThisFrameIfSuppressingUMChainFromComPlusMethodFrameGeneric has been
    // set by TrackUMChain to help us remember that the current frame we're looking at is
    // ComPlusMethodFrameGeneric (we can't rely on looking at pInfo->frame to check
    // this), and i->m_suppressUMChainFromComPlusMethodFrameGeneric has been set by the
    // dude initiating this walk to remind us that our goal in life is to do a Step Out
    // during managed-only debugging. These two things together tell us we should ignore
    // this frame, rather than erroneously identifying it as the target frame.
#ifdef FEATURE_COMINTEROP
    if(i->m_suppressUMChainFromComPlusMethodFrameGeneric &&
        (pInfo->chainReason == CHAIN_ENTER_UNMANAGED) &&
        (pInfo->fIgnoreThisFrameIfSuppressingUMChainFromComPlusMethodFrameGeneric))
    {
        return SWA_CONTINUE;
    }
#endif // FEATURE_COMINTEROP

    //have we reached the correct frame yet?
    if (!i->m_targetFrameFound &&
        IsEqualOrCloserToLeaf(i->m_targetFP, pInfo->fp))
    {
        i->m_targetFrameFound = true;
    }

    if (i->m_targetFrameFound )
    {
        // Ignore Enter-managed chains.
        if (pInfo->chainReason == CHAIN_ENTER_MANAGED)
        {
            return SWA_CONTINUE;
        }

        if (i->m_activeFound )
        {
            if (pInfo->chainReason == CHAIN_CLASS_INIT)
                i->m_specialChainReason = pInfo->chainReason;

            if (pInfo->fp != i->m_activeFrame.fp) // avoid dups
            {
                i->m_returnFrame = *pInfo;

#if defined(FEATURE_EH_FUNCLETS)
                CopyREGDISPLAY(&(i->m_returnFrame.registers), &(pInfo->registers));
#endif // FEATURE_EH_FUNCLETS

                i->m_returnFound = true;

                // We care if the current frame is unmanaged
                // Continue unless we found a managed return frame.
                return pInfo->managed ? SWA_ABORT : SWA_CONTINUE;
            }
        }
        else
        {
            i->m_activeFrame = *pInfo;

#if defined(FEATURE_EH_FUNCLETS)
            CopyREGDISPLAY(&(i->m_activeFrame.registers), &(pInfo->registers));
#endif // FEATURE_EH_FUNCLETS

            i->m_activeFound = true;

            return SWA_CONTINUE;
        }
    }

    return SWA_CONTINUE;
}


//
// Note that patches may be reallocated - do not keep a pointer to a patch.
//
DebuggerControllerPatch *DebuggerPatchTable::AddPatchForMethodDef(DebuggerController *controller,
                                  Module *module,
                                  mdMethodDef md,
                                  MethodDesc* pMethodDescFilter,
                                  size_t offset,
                                  BOOL offsetIsIL,
                                  DebuggerPatchKind kind,
                                  FramePointer fp,
                                  AppDomain *pAppDomain,
                                  SIZE_T masterEnCVersion,
                                  DebuggerJitInfo *dji)
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;



    LOG( (LF_CORDB,LL_INFO10000,"DCP:AddPatchForMethodDef unbound "
        "relative in methodDef 0x%x with dji 0x%x "
        "controller:0x%x AD:0x%x\n", md,
        dji, controller, pAppDomain));

    DebuggerFunctionKey key;

    key.module = module;
    key.md = md;

    // Get a new uninitialized patch object
    DebuggerControllerPatch *patch =
      (DebuggerControllerPatch *) Add(HashKey(&key));
    if (patch == NULL)
    {
        ThrowOutOfMemory();
    }
#ifndef FEATURE_EMULATE_SINGLESTEP
    patch->Initialize();
#endif

    //initialize the patch data structure.
    InitializePRD(&(patch->opcode));
    patch->controller = controller;
    patch->key.module = module;
    patch->key.md = md;
    patch->pMethodDescFilter = pMethodDescFilter;
    patch->offset = offset;
    patch->offsetIsIL = offsetIsIL;
    patch->address = NULL;
    patch->fp = fp;
    patch->trace.Bad_SetTraceType(DPT_DEFAULT_TRACE_TYPE);      // TRACE_OTHER
    patch->refCount   = 1;            // AddRef()
    patch->fSaveOpcode = false;
    patch->pAppDomain = pAppDomain;
    patch->pid = m_pid++;

    if (kind == PATCH_KIND_IL_MASTER)
    {
        _ASSERTE(dji == NULL);
        patch->encVersion = masterEnCVersion;
    }
    else
    {
        patch->dji = dji;
    }
    patch->kind = kind;

    if (dji)
        LOG((LF_CORDB,LL_INFO10000,"AddPatchForMethodDef w/ version 0x%04x, "
        "pid:0x%x\n", dji->m_encVersion, patch->pid));
    else if (kind == PATCH_KIND_IL_MASTER)
        LOG((LF_CORDB,LL_INFO10000,"AddPatchForMethodDef w/ version 0x%04x, "
        "pid:0x%x\n", masterEnCVersion,patch->pid));
    else
        LOG((LF_CORDB,LL_INFO10000,"AddPatchForMethodDef w/ no dji or dmi, pid:0x%x\n",patch->pid));


    // This patch is not yet bound or activated
    _ASSERTE( !patch->IsBound() );
    _ASSERTE( !patch->IsActivated() );

    // The only kind of patch with IL offset is the IL master patch.
    _ASSERTE(patch->IsILMasterPatch() || patch->offsetIsIL == FALSE);

    // The only kind of patch that allows a MethodDescFilter is the IL master patch
    _ASSERTE(patch->IsILMasterPatch() || patch->pMethodDescFilter == NULL);

    // Zero is the only native offset that we allow to bind across different jitted
    // code bodies. There isn't any sensible meaning to binding at some other native offset.
    // Even if all the code bodies had an instruction that started at that offset there is
    // no guarantee those instructions represent a semantically equivalent point in the
    // method's execution.
    _ASSERTE(!(patch->IsILMasterPatch() && !patch->offsetIsIL && patch->offset != 0));

    return patch;
}

// Create and bind a patch to the specified address
// The caller should immediately activate the patch since we typically expect bound patches
// will always be activated.
DebuggerControllerPatch *DebuggerPatchTable::AddPatchForAddress(DebuggerController *controller,
                                  MethodDesc *fd,
                                  size_t offset,
                                  DebuggerPatchKind kind,
                                  CORDB_ADDRESS_TYPE *address,
                                  FramePointer fp,
                                  AppDomain *pAppDomain,
                                  DebuggerJitInfo *dji,
                                  SIZE_T pid,
                                  TraceType traceType)

{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;


    _ASSERTE(kind == PATCH_KIND_NATIVE_MANAGED || kind == PATCH_KIND_NATIVE_UNMANAGED);
    LOG((LF_CORDB,LL_INFO10000,"DCP:AddPatchForAddress bound "
        "absolute to 0x%p with dji 0x%p (mdDef:0x%x) "
        "controller:0x%p AD:0x%p\n",
        address, dji, (fd!=NULL?fd->GetMemberDef():0), controller,
        pAppDomain));

    // get new uninitialized patch object
    DebuggerControllerPatch *patch =
      (DebuggerControllerPatch *) Add(HashAddress(address));

    if (patch == NULL)
    {
        ThrowOutOfMemory();
    }
#ifndef FEATURE_EMULATE_SINGLESTEP
    patch->Initialize();
#endif

    // initialize the patch data structure
    InitializePRD(&(patch->opcode));
    patch->controller = controller;

    if (fd == NULL)
    {
        patch->key.module = NULL;
        patch->key.md = mdTokenNil;
    }
    else
    {
        patch->key.module = g_pEEInterface->MethodDescGetModule(fd);
        patch->key.md = fd->GetMemberDef();
    }
    patch->pMethodDescFilter = NULL;
    patch->offset = offset;
    patch->offsetIsIL = FALSE;
    patch->address = address;
    patch->fp = fp;
    patch->trace.Bad_SetTraceType(traceType);
    patch->refCount   = 1;            // AddRef()
    patch->fSaveOpcode = false;
    patch->pAppDomain = pAppDomain;
    if (pid == DCP_PID_INVALID)
        patch->pid = m_pid++;
    else
        patch->pid = pid;

    patch->dji = dji;
    patch->kind = kind;

    if (dji == NULL)
        LOG((LF_CORDB,LL_INFO10000,"AddPatchForAddress w/ version with no dji, pid:0x%x\n", patch->pid));
    else
    {
        LOG((LF_CORDB,LL_INFO10000,"AddPatchForAddress w/ version 0x%04x, "
            "pid:0x%x\n", dji->m_methodInfo->GetCurrentEnCVersion(), patch->pid));

        _ASSERTE( fd==NULL || fd == dji->m_nativeCodeVersion.GetMethodDesc() );
    }

    SortPatchIntoPatchList(&patch);

    // This patch is bound but not yet activated
    _ASSERTE( patch->IsBound() );
    _ASSERTE( !patch->IsActivated() );

    // The only kind of patch with IL offset is the IL master patch.
    _ASSERTE(patch->IsILMasterPatch() || patch->offsetIsIL == FALSE);
    return patch;
}

// Set the native address for this patch.
void DebuggerPatchTable::BindPatch(DebuggerControllerPatch *patch, CORDB_ADDRESS_TYPE *address)
{
    _ASSERTE(patch != NULL);
    _ASSERTE(address != NULL);
    _ASSERTE( !patch->IsILMasterPatch() );
    _ASSERTE(!patch->IsBound() );

    //Since the actual patch doesn't move, we don't have to worry about
    //zeroing out the opcode field (see lenghty comment above)
    // Since the patch is double-hashed based off Address, if we change the address,
    // we must remove and reinsert the patch.
    CHashTable::Delete(HashKey(&patch->key), ItemIndex((HASHENTRY*)patch));

    patch->address = address;

    CHashTable::Add(HashAddress(address), ItemIndex((HASHENTRY*)patch));

    SortPatchIntoPatchList(&patch);

    _ASSERTE(patch->IsBound() );
    _ASSERTE(!patch->IsActivated() );
}

// Disassociate a patch from a specific code address.
void DebuggerPatchTable::UnbindPatch(DebuggerControllerPatch *patch)
{
    _ASSERTE(patch != NULL);
    _ASSERTE(patch->kind != PATCH_KIND_IL_MASTER);
    _ASSERTE(patch->IsBound() );
    _ASSERTE(!patch->IsActivated() );

    //<REVISIT_TODO>@todo We're hosed if the patch hasn't been primed with
    // this info & we can't get it...</REVISIT_TODO>
    if (patch->key.module == NULL ||
        patch->key.md == mdTokenNil)
    {
        MethodDesc *fd = g_pEEInterface->GetNativeCodeMethodDesc(
            dac_cast<PCODE>(patch->address));
        _ASSERTE( fd != NULL );
        patch->key.module = g_pEEInterface->MethodDescGetModule(fd);
        patch->key.md = fd->GetMemberDef();
    }

    // Update it's index entry in the table to use it's unbound key
    // Since the patch is double-hashed based off Address, if we change the address,
    // we must remove and reinsert the patch.
    CHashTable::Delete( HashAddress(patch->address),
                        ItemIndex((HASHENTRY*)patch));

    patch->address = NULL;      // we're no longer bound to this address

    CHashTable::Add( HashKey(&patch->key),
                     ItemIndex((HASHENTRY*)patch));

     _ASSERTE(!patch->IsBound() );

}

void DebuggerPatchTable::RemovePatch(DebuggerControllerPatch *patch)
{
    // Since we're deleting this patch, it must not be activated (i.e. it must not have a stored opcode)
    _ASSERTE( !patch->IsActivated() );
#ifndef FEATURE_EMULATE_SINGLESTEP
    patch->DoCleanup();
#endif

    //
    // Because of the implementation of CHashTable, we can safely
    // delete elements while iterating through the table.  This
    // behavior is relied upon - do not change to a different
    // implementation without considering this fact.
    //
    Delete(Hash(patch),  (HASHENTRY *) patch);

}

DebuggerControllerPatch *DebuggerPatchTable::GetNextPatch(DebuggerControllerPatch *prev)
{
    ULONG iNext;
    HASHENTRY *psEntry;

    // Start at the next entry in the chain.
    // @todo - note that: EntryPtr(ItemIndex(x)) == x
    iNext = EntryPtr(ItemIndex((HASHENTRY*)prev))->iNext;

    // Search until we hit the end.
    while (iNext != UINT32_MAX)
    {
        // Compare the keys.
        psEntry = EntryPtr(iNext);

        // Careful here... we can hash the entries in this table
        // by two types of keys. In this type of search, the type
        // of the second key (psEntry) does not necessarily
        // indicate the type of the first key (prev), so we have
        // to check for sure.
        DebuggerControllerPatch *pc2 = (DebuggerControllerPatch*)psEntry;

        if (((pc2->address == NULL) && (prev->address == NULL)) ||
            ((pc2->address != NULL) && (prev->address != NULL)))
            if (!Cmp(Key(prev), psEntry))
                return pc2;

        // Advance to the next item in the chain.
        iNext = psEntry->iNext;
    }

    return NULL;
}

#ifdef _DEBUG_PATCH_TABLE
    // DEBUG An internal debugging routine, it iterates
    //      through the hashtable, stopping at every
    //      single entry, no matter what it's state.  For this to
    //      compile, you're going to have to add friend status
    //      of this class to CHashTableAndData in
    //      to $\Com99\Src\inc\UtilCode.h
void DebuggerPatchTable::CheckPatchTable()
{
    if (NULL != m_pcEntries)
    {
        DebuggerControllerPatch *dcp;
        int i = 0;
        while (i++ <m_iEntries)
        {
            dcp = (DebuggerControllerPatch*)&(((DebuggerControllerPatch *)m_pcEntries)[i]);
            if (dcp->opcode != 0 )
            {
                LOG((LF_CORDB,LL_INFO1000, "dcp->addr:0x%8x "
                    "mdMD:0x%8x, offset:0x%x, native:%d\n",
                    dcp->address, dcp->key.md, dcp->offset,
                    dcp->IsNativePatch()));
            }
        }
    }
}

#endif // _DEBUG_PATCH_TABLE

// Count how many patches are in the table.
// Use for asserts
int DebuggerPatchTable::GetNumberOfPatches()
{
    int total = 0;

    if (NULL != m_pcEntries)
    {
        DebuggerControllerPatch *dcp;
        ULONG i = 0;

        while (i++ <m_iEntries)
        {
            dcp = (DebuggerControllerPatch*)&(((DebuggerControllerPatch *)m_pcEntries)[i]);

            if (dcp->IsActivated() || !dcp->IsFree())
                total++;
        }
    }
    return total;
}

#if defined(_DEBUG)
//-----------------------------------------------------------------------------
// Debug check that we only have 1 thread-starter per thread.
// pNew - the new DTS. We'll make sure there's not already a DTS on this thread.
//-----------------------------------------------------------------------------
void DebuggerController::EnsureUniqueThreadStarter(DebuggerThreadStarter * pNew)
{
    // This lock should be safe to take since our base class ctor takes it.
    ControllerLockHolder lockController;
    DebuggerController * pExisting = g_controllers;
    while(pExisting != NULL)
    {
        if (pExisting->GetDCType() == DEBUGGER_CONTROLLER_THREAD_STARTER)
        {
            if (pExisting != pNew)
            {
                // If we have 2 thread starters, they'd better be on different threads.
                _ASSERTE((pExisting->GetThread() != pNew->GetThread()));
            }
        }
        pExisting = pExisting->m_next;
    }
}
#endif

//-----------------------------------------------------------------------------
// If we have a thread-starter on the given EE thread, make sure it's cancel.
// Thread-Starters normally delete themselves when they fire. But if the EE
// destroys the thread before it fires, then we'd still have an active DTS.
//-----------------------------------------------------------------------------
void DebuggerController::CancelOutstandingThreadStarter(Thread * pThread)
{
    _ASSERTE(pThread != NULL);
    LOG((LF_CORDB, LL_EVERYTHING, "DC:CancelOutstandingThreadStarter - checking on thread =0x%p\n", pThread));

    ControllerLockHolder lockController;
    DebuggerController * p = g_controllers;
    while(p != NULL)
    {
        if (p->GetDCType() == DEBUGGER_CONTROLLER_THREAD_STARTER)
        {
            if (p->GetThread() == pThread)
            {
                LOG((LF_CORDB, LL_EVERYTHING, "DC:CancelOutstandingThreadStarter, pThread=0x%p, Found=0x%p\n", p));

                // There's only 1 DTS per thread, so once we find it, we can quit.
                p->Delete();
                p = NULL;
                break;
            }
        }
        p = p->m_next;
    }
    // The common case is that our DTS hit its patch and did a SendEvent (and
    // deleted itself). So usually we'll get through the whole list w/o deleting anything.

}

//void DebuggerController::Initialize()   Sets up the static
// variables for the static DebuggerController class.
// How: initializes the critical section
HRESULT DebuggerController::Initialize()
{
    CONTRACT(HRESULT)
    {
        THROWS;
        GC_NOTRIGGER;
        // This can be called in an "early attach" case, so DebuggerIsInvolved()
        // will be b/c we don't realize the debugger's attaching to us.
        //PRECONDITION(DebuggerIsInvolved());
        POSTCONDITION(CheckPointer(g_patches));
        POSTCONDITION(RETVAL == S_OK);
    }
    CONTRACT_END;

    if (g_patches == NULL)
    {
        ZeroMemory(&g_criticalSection, sizeof(g_criticalSection)); // Init() expects zero-init memory.

        // NOTE: CRST_UNSAFE_ANYMODE prevents a GC mode switch when entering this crst.
        // If you remove this flag, we will switch to preemptive mode when entering
        // g_criticalSection, which means all functions that enter it will become
        // GC_TRIGGERS.  (This includes all uses of ControllerLockHolder.)  So be sure
        // to update the contracts if you remove this flag.
        g_criticalSection.Init(CrstDebuggerController,
            (CrstFlags)(CRST_UNSAFE_ANYMODE | CRST_REENTRANCY | CRST_DEBUGGER_THREAD));

        g_patches = new (interopsafe) DebuggerPatchTable();
        _ASSERTE(g_patches != NULL); // throws on oom

        HRESULT hr = g_patches->Init();

        if (FAILED(hr))
        {
            DeleteInteropSafe(g_patches);
            ThrowHR(hr);
        }

        g_patchTableValid = TRUE;
        TRACE_ALLOC(g_patches);
    }

    _ASSERTE(g_patches != NULL);

    RETURN (S_OK);
}


//---------------------------------------------------------------------------------------
//
// Constructor for a controller
//
// Arguments:
//    pThread - thread that controller has affinity to. NULL if no thread - affinity.
//    pAppdomain - appdomain that controller has affinity to. NULL if no AD affinity.
//
//
// Notes:
//    "Affinity" is per-controller specific. Affinity is generally passed on to
//    any patches the controller creates. So if a controller has affinity to Thread X,
//    then any patches it creates will only fire on Thread-X.
//
//---------------------------------------------------------------------------------------

DebuggerController::DebuggerController(Thread * pThread, AppDomain * pAppDomain)
  : m_pAppDomain(pAppDomain),
    m_thread(pThread),
    m_singleStep(false),
    m_exceptionHook(false),
    m_traceCall(0),
    m_traceCallFP(ROOT_MOST_FRAME),
    m_unwindFP(LEAF_MOST_FRAME),
    m_eventQueuedCount(0),
    m_deleted(false),
    m_fEnableMethodEnter(false)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CONSTRUCTOR_CHECK;
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO10000, "DC: 0x%x m_eventQueuedCount to 0 - DC::DC\n", this));
    ControllerLockHolder lockController;
    {
        m_next = g_controllers;
        g_controllers = this;
    }
}

//---------------------------------------------------------------------------------------
//
// Debugger::Controller::DeleteAllControlers - deletes all debugger contollers
//
// Arguments:
//    None
//
// Return Value:
//    None
//
// Notes:
//    This is used at detach time to remove all DebuggerControllers.  This will remove all
//    patches and do whatever other cleanup individual DebuggerControllers consider
//    necessary to allow the debugger to detach and the process to run normally.
//

void DebuggerController::DeleteAllControllers()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    ControllerLockHolder lockController;
    DebuggerController * pDebuggerController = g_controllers;
    DebuggerController * pNextDebuggerController = NULL;

    while (pDebuggerController != NULL)
    {
        pNextDebuggerController = pDebuggerController->m_next;
        pDebuggerController->DebuggerDetachClean();
        pDebuggerController->Delete();
        pDebuggerController = pNextDebuggerController;
    }
}

DebuggerController::~DebuggerController()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        DESTRUCTOR_CHECK;
    }
    CONTRACTL_END;

    ControllerLockHolder lockController;

    _ASSERTE(m_eventQueuedCount == 0);

    DisableAll();

    //
    // Remove controller from list
    //

    DebuggerController **c;

    c = &g_controllers;
    while (*c != this)
        c = &(*c)->m_next;

    *c = m_next;

}

// void DebuggerController::Delete()
// What: Marks an instance as deletable.  If it's ref count
// (see Enqueue, Dequeue) is currently zero, it actually gets deleted
// How: Set m_deleted to true.  If m_eventQueuedCount==0, delete this
void DebuggerController::Delete()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (m_eventQueuedCount == 0)
    {
        LOG((LF_CORDB|LF_ENC, LL_INFO100000, "DC::Delete: actual delete of this:0x%x!\n", this));
        TRACE_FREE(this);
        DeleteInteropSafe(this);
    }
    else
    {
        LOG((LF_CORDB|LF_ENC, LL_INFO100000, "DC::Delete: marked for "
            "future delete of this:0x%x!\n", this));
        LOG((LF_CORDB|LF_ENC, LL_INFO10000, "DC:0x%x m_eventQueuedCount at 0x%x\n",
            this, m_eventQueuedCount));
        m_deleted = true;
    }
}

void DebuggerController::DebuggerDetachClean()
{
    //do nothing here
}

//static
void DebuggerController::AddRef(DebuggerControllerPatch *patch)
{
    patch->refCount++;
}

//static
void DebuggerController::Release(DebuggerControllerPatch *patch)
{
    patch->refCount--;
    if (patch->refCount == 0)
    {
        LOG((LF_CORDB, LL_INFO10000, "DCP::R: patch deleted, deactivating\n"));
        DeactivatePatch(patch);
        GetPatchTable()->RemovePatch(patch);
    }
}

// void DebuggerController::DisableAll()   DisableAll removes
// all control from the controller.  This includes all patches & page
// protection.  This will invoke Disable* for unwind,singlestep,
// exceptionHook, and tracecall.  It will also go through the patch table &
// attempt to remove any and all patches that belong to this controller.
// If the patch is currently triggering, then a Dispatch* method expects the
// patch to be there after we return, so we instead simply mark the patch
// itself as deleted.
void DebuggerController::DisableAll()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    LOG((LF_CORDB,LL_INFO1000, "DC::DisableAll\n"));
    _ASSERTE(g_patches != NULL);

    ControllerLockHolder ch;
    {
        //
        // Remove controller's patches from list.
        // Don't do this on shutdown because the shutdown thread may have killed another thread asynchronously
        // thus leaving the patchtable in an inconsistent state such that we may fail trying to walk it.
        // Since we're exiting anyways, leaving int3 in the code can't harm anybody.
        //
        if (!g_fProcessDetach)
        {
            HASHFIND f;
            for (DebuggerControllerPatch *patch = g_patches->GetFirstPatch(&f);
                 patch != NULL;
                 patch = g_patches->GetNextPatch(&f))
            {
                if (patch->controller == this)
                {
                    Release(patch);
                }
            }
        }

        if (m_singleStep)
            DisableSingleStep();
        if (m_exceptionHook)
            DisableExceptionHook();
        if (m_unwindFP != LEAF_MOST_FRAME)
            DisableUnwind();
        if (m_traceCall)
            DisableTraceCall();
        if (m_fEnableMethodEnter)
            DisableMethodEnter();
    }
}

// void DebuggerController::Enqueue()    What: Does
// reference counting so we don't toast a
// DebuggerController while it's in a Dispatch queue.
// Why: In DispatchPatchOrSingleStep, we can't hold locks when going
// into PreEmptiveGC mode b/c we'll create a deadlock.
// So we have to UnLock() prior to
// EnablePreEmptiveGC().  But somebody else can show up and delete the
// DebuggerControllers since we no longer have the lock.  So we have to
// do this reference counting thing to make sure that the controllers
// don't get toasted as we're trying to invoke SendEvent on them.  We have to
// reaquire the lock before invoking Dequeue because Dequeue may
// result in the controller being deleted, which would change the global
// controller list.
// How: InterlockIncrement( m_eventQueuedCount )
void DebuggerController::Enqueue()
{
    LIMITED_METHOD_CONTRACT;

    m_eventQueuedCount++;
    LOG((LF_CORDB, LL_INFO10000, "DC::Enq DC:0x%x m_eventQueuedCount at 0x%x\n",
        this, m_eventQueuedCount));
}

// void DebuggerController::Dequeue()   What: Does
// reference counting so we don't toast a
// DebuggerController while it's in a Dispatch queue.
// How: InterlockDecrement( m_eventQueuedCount ), delete this if
// m_eventQueuedCount == 0 AND m_deleted has been set to true
void DebuggerController::Dequeue()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO10000, "DC::Deq DC:0x%x m_eventQueuedCount at 0x%x\n",
    this, m_eventQueuedCount));
    if (--m_eventQueuedCount == 0)
    {
        if (m_deleted)
        {
            TRACE_FREE(this);
            DeleteInteropSafe(this);
        }
    }
}


// bool DebuggerController::BindPatch()  If the method has
// been JITted and isn't hashed by address already, then hash
// it into the hashtable by address and not DebuggerFunctionKey.
// If the patch->address field is nonzero, we're done.
// Otherwise ask g_pEEInterface to FindLoadedMethodRefOrDef, then
// GetFunctionAddress of the method, if the method is in IL,
// MapILOffsetToNative.  If everything else went Ok, we can now invoke
// g_patches->BindPatch.
// Returns: false if we know that we can't bind the patch immediately.
//      true if we either can bind the patch right now, or can't right now,
//      but might be able to in the future (eg, the method hasn't been JITted)

// Have following outcomes:
// 1) Succeeded in binding the patch to a raw address. patch->address is set.
// (Note we still must apply the patch to put the int 3 in.)
// returns true, *pFail = false
//
// 2) Fails to bind, but a future attempt may succeed. Obvious ex, for an IL-only
// patch on an unjitted method.
// returns false, *pFail = false
//
// 3) Fails to bind because something's wrong. Ex: bad IL offset, no DJI to do a
// mapping with. Future calls will fail too.
// returns false, *pFail = true
bool DebuggerController::BindPatch(DebuggerControllerPatch *patch,
                                   MethodDesc *fd,
                                   CORDB_ADDRESS_TYPE *startAddr)
{
    CONTRACTL
    {
        THROWS; // from GetJitInfo
        GC_NOTRIGGER;
        MODE_ANY; // don't really care what mode we're in.

        PRECONDITION(ThisMaybeHelperThread());
    }
    CONTRACTL_END;

    _ASSERTE(patch != NULL);
    _ASSERTE(!patch->IsILMasterPatch());
    _ASSERTE(fd != NULL);

    //
    // Translate patch to address, if it hasn't been already.
    //

    if (patch->address != NULL)
    {
        return true;
    }

    if (startAddr == NULL)
    {
        if (patch->HasDJI() && patch->GetDJI()->m_jitComplete)
        {
            startAddr = (CORDB_ADDRESS_TYPE *) CORDB_ADDRESS_TO_PTR(patch->GetDJI()->m_addrOfCode);
            _ASSERTE(startAddr != NULL);
        }
        if (startAddr == NULL)
        {
            // Should not be trying to place patches on MethodDecs's for stubs.
            // These stubs will never get jitted.
            CONSISTENCY_CHECK_MSGF(!fd->IsWrapperStub(), ("Can't place patch at stub md %p, %s::%s",
                                   fd, fd->m_pszDebugClassName, fd->m_pszDebugMethodName));

            startAddr = (CORDB_ADDRESS_TYPE *)g_pEEInterface->GetFunctionAddress(fd);
            //
            // Code is not available yet to patch.  The prestub should
            // notify us when it is executed.
            //
            if (startAddr == NULL)
            {
                LOG((LF_CORDB, LL_INFO10000,
                    "DC::BP:Patch at 0x%x not bindable yet.\n", patch->offset));

                return false;
            }
        }
    }

    _ASSERTE(!g_pEEInterface->IsStub((const BYTE *)startAddr));

    // If we've jitted, map to a native offset.
    DebuggerJitInfo *info = g_pDebugger->GetJitInfo(fd, (const BYTE *)startAddr);

#ifdef LOGGING
    if (info == NULL)
    {
        LOG((LF_CORDB,LL_INFO10000, "DC::BindPa: For startAddr 0x%x, didn't find a DJI\n", startAddr));
    }
#endif //LOGGING
    if (info != NULL)
    {
        // There is a strange case with prejitted code and unjitted trace patches. We can enter this function
        // with no DebuggerJitInfo created, then have the call just above this actually create the
        // DebuggerJitInfo, which causes JitComplete to be called, which causes all patches to be bound! If this
        // happens, then we don't need to continue here (its already been done recursivley) and we don't need to
        // re-active the patch, so we return false from right here. We can check this by seeing if we suddently
        // have the address in the patch set.
        if (patch->address != NULL)
        {
            LOG((LF_CORDB,LL_INFO10000, "DC::BindPa: patch bound recursivley by GetJitInfo, bailing...\n"));
            return false;
        }

        LOG((LF_CORDB,LL_INFO10000, "DC::BindPa: For startAddr 0x%p, got DJI "
             "0x%p, from 0x%p size: 0x%x\n", startAddr, info, info->m_addrOfCode, info->m_sizeOfCode));
    }

    LOG((LF_CORDB, LL_INFO10000, "DC::BP:Trying to bind patch in %s::%s version %d\n",
         fd->m_pszDebugClassName, fd->m_pszDebugMethodName, info ? info->m_encVersion : (SIZE_T)-1));

    _ASSERTE(g_patches != NULL);

    CORDB_ADDRESS_TYPE *addr = (CORDB_ADDRESS_TYPE *)
                               CodeRegionInfo::GetCodeRegionInfo(NULL, NULL, startAddr).OffsetToAddress(patch->offset);
    g_patches->BindPatch(patch, addr);

    LOG((LF_CORDB, LL_INFO10000, "DC::BP:Binding patch at 0x%x(off:%x)\n", addr, patch->offset));

    return true;
}

// bool DebuggerController::ApplyPatch()    applies
// the patch described to the code, and
// remembers the replaced opcode.  Note that the same address
// cannot be patched twice at the same time.
// Grabs the opcode & stores in patch, then sets a break
// instruction for either native or IL.
// VirtualProtect & some macros.  Returns false if anything
// went bad.
// DebuggerControllerPatch *patch:  The patch, indicates where
//        to set the INT3 instruction
// Returns: true if the user break instruction was successfully
//        placed into the code-stream, false otherwise
bool DebuggerController::ApplyPatch(DebuggerControllerPatch *patch)
{
    LOG((LF_CORDB, LL_INFO10000, "DC::ApplyPatch at addr 0x%p\n",
        patch->address));

    // If we try to apply an already applied patch, we'll override our saved opcode
    // with the break opcode and end up getting a break in out patch bypass buffer.
    _ASSERTE(!patch->IsActivated() );
    _ASSERTE(patch->IsBound());

    // Note we may be patching at certain "blessed" points in mscorwks.
    // This is very dangerous b/c we can't be sure patch->Address is blessed or not.


    //
    // Apply the patch.
    //
    _ASSERTE(!(g_pConfig->GetGCStressLevel() & (EEConfig::GCSTRESS_INSTR_JIT|EEConfig::GCSTRESS_INSTR_NGEN))
                 && "Debugger does not work with GCSTRESS 4");

    if (patch->IsNativePatch())
    {
        if (patch->fSaveOpcode)
        {
            // We only used SaveOpcode for when we've moved code, so
            // the patch should already be there.
            patch->opcode = patch->opcodeSaved;
            _ASSERTE( AddressIsBreakpoint(patch->address) );
            return true;
        }

#if _DEBUG
        VerifyExecutableAddress((BYTE*)patch->address);
#endif

        LPVOID baseAddress = (LPVOID)(patch->address);

#if !defined(HOST_OSX) || !defined(HOST_ARM64)
        DWORD oldProt;

        if (!VirtualProtect(baseAddress,
                            CORDbg_BREAK_INSTRUCTION_SIZE,
                            PAGE_EXECUTE_READWRITE, &oldProt))
        {
            _ASSERTE(!"VirtualProtect of code page failed");
            return false;
        }
#endif // !defined(HOST_OSX) || !defined(HOST_ARM64)

        patch->opcode = CORDbgGetInstruction(patch->address);

        CORDbgInsertBreakpoint((CORDB_ADDRESS_TYPE *)patch->address);
        LOG((LF_CORDB, LL_EVERYTHING, "Breakpoint was inserted at %p for opcode %x\n", patch->address, patch->opcode));

#if !defined(HOST_OSX) || !defined(HOST_ARM64)
        if (!VirtualProtect(baseAddress,
                            CORDbg_BREAK_INSTRUCTION_SIZE,
                            oldProt, &oldProt))
        {
            _ASSERTE(!"VirtualProtect of code page failed");
            return false;
        }
#endif // !defined(HOST_OSX) || !defined(HOST_ARM64)
    }
// TODO: : determine if this is needed for AMD64
#if defined(TARGET_X86) //REVISIT_TODO what is this?!
    else
    {
        DWORD oldProt;

        //
        // !!! IL patch logic assumes reference insruction encoding
        //
        if (!VirtualProtect((void *) patch->address, 2,
                            PAGE_EXECUTE_READWRITE, &oldProt))
        {
            _ASSERTE(!"VirtualProtect of code page failed");
            return false;
        }

        patch->opcode =
          (unsigned int) *(unsigned short*)(patch->address+1);

        _ASSERTE(patch->opcode != CEE_BREAK);

        ExecutableWriterHolder<BYTE> breakpointWriterHolder((BYTE*)patch->address, 2);
        *(unsigned short *) (breakpointWriterHolder.GetRW()+1) = CEE_BREAK;

        if (!VirtualProtect((void *) patch->address, 2, oldProt, &oldProt))
        {
            _ASSERTE(!"VirtualProtect of code page failed");
            return false;
        }
    }
#endif //TARGET_X86

    return true;
}

// bool DebuggerController::UnapplyPatch()
// UnapplyPatch removes the patch described by the patch.
// (CopyOpcodeFromAddrToPatch, in reverse.)
// Looks a lot like CopyOpcodeFromAddrToPatch, except that we use a macro to
// copy the instruction back to the code-stream & immediately set the
// opcode field to 0 so ReadMemory,WriteMemory will work right.
// Note that it's very important to zero out the opcode field, as it
// is used by the right side to determine if a patch is
// valid or not.
// NO LOCKING
// DebuggerControllerPatch * patch:  Patch to remove
// Returns:  true if the patch was unapplied, false otherwise
bool DebuggerController::UnapplyPatch(DebuggerControllerPatch *patch)
{
    _ASSERTE(patch->address != NULL);
    _ASSERTE(patch->IsActivated() );

    LOG((LF_CORDB,LL_INFO1000, "DC::UP unapply patch at addr 0x%p\n",
        patch->address));

    if (patch->IsNativePatch())
    {
        if (patch->fSaveOpcode)
        {
            // We're doing this for MoveCode, and we don't want to
            // overwrite something if we don't get moved far enough.
            patch->opcodeSaved = patch->opcode;
            InitializePRD(&(patch->opcode));
            _ASSERTE( !patch->IsActivated() );
            return true;
        }

        LPVOID baseAddress = (LPVOID)(patch->address);

#if !defined(HOST_OSX) || !defined(HOST_ARM64)
        DWORD oldProt;

        if (!VirtualProtect(baseAddress,
                            CORDbg_BREAK_INSTRUCTION_SIZE,
                            PAGE_EXECUTE_READWRITE, &oldProt))
        {
            //
            // We may be trying to remove a patch from memory
            // which has been unmapped. We can ignore the
            // error in this case.
            //
            InitializePRD(&(patch->opcode));
            return false;
        }
#endif // !defined(HOST_OSX) || !defined(HOST_ARM64)

        CORDbgSetInstruction((CORDB_ADDRESS_TYPE *)patch->address, patch->opcode);

        //VERY IMPORTANT to zero out opcode, else we might mistake
        //this patch for an active on on ReadMem/WriteMem (see
        //header file comment)
        InitializePRD(&(patch->opcode));

#if !defined(HOST_OSX) || !defined(HOST_ARM64)
        if (!VirtualProtect(baseAddress,
                            CORDbg_BREAK_INSTRUCTION_SIZE,
                            oldProt, &oldProt))
        {
            _ASSERTE(!"VirtualProtect of code page failed");
            return false;
        }
#endif // !defined(HOST_OSX) || !defined(HOST_ARM64)
    }
    else
    {
        DWORD oldProt;

        if (!VirtualProtect((void *) patch->address, 2,
                            PAGE_EXECUTE_READWRITE, &oldProt))
        {
            //
            // We may be trying to remove a patch from memory
            // which has been unmapped. We can ignore the
            // error in this case.
            //
            InitializePRD(&(patch->opcode));
            return false;
        }

        //
        // !!! IL patch logic assumes reference encoding
        //
// TODO: : determine if this is needed for AMD64
#if defined(TARGET_X86)
        _ASSERTE(*(unsigned short*)(patch->address+1) == CEE_BREAK);

        ExecutableWriterHolder<BYTE> breakpointWriterHolder((BYTE*)patch->address, 2);
        *(unsigned short *) (breakpointWriterHolder.GetRW()+1)
          = (unsigned short) patch->opcode;
#endif //this makes no sense on anything but X86
        //VERY IMPORTANT to zero out opcode, else we might mistake
        //this patch for an active on on ReadMem/WriteMem (see
        //header file comment
        InitializePRD(&(patch->opcode));

        if (!VirtualProtect((void *) patch->address, 2, oldProt, &oldProt))
        {
            _ASSERTE(!"VirtualProtect of code page failed");
            return false;
        }
    }

    _ASSERTE( !patch->IsActivated() );
    _ASSERTE( patch->IsBound() );
    return true;
}

// void DebuggerController::UnapplyPatchAt()
// NO LOCKING
// UnapplyPatchAt removes the patch from a copy of the patched code.
// Like UnapplyPatch, except that we don't bother checking
// memory permissions, but instead replace the breakpoint instruction
// with the opcode at an arbitrary memory address.
void DebuggerController::UnapplyPatchAt(DebuggerControllerPatch *patch,
                                        CORDB_ADDRESS_TYPE *address)
{
    _ASSERTE(patch->IsBound() );

    if (patch->IsNativePatch())
    {
        CORDbgSetInstruction((CORDB_ADDRESS_TYPE *)address, patch->opcode);
        //note that we don't have to zero out opcode field
        //since we're unapplying at something other than
        //the original spot. We assert this is true:
        _ASSERTE( patch->address != address );
    }
    else
    {
        //
        // !!! IL patch logic assumes reference encoding
        //
// TODO: : determine if this is needed for AMD64
#ifdef TARGET_X86
        _ASSERTE(*(unsigned short*)(address+1) == CEE_BREAK);

        *(unsigned short *) (address+1)
          = (unsigned short) patch->opcode;
        _ASSERTE( patch->address != address );
#endif // this makes no sense on anything but X86
    }
}

// bool DebuggerController::IsPatched()  Is there a patch at addr?
// How: if fNative && the instruction at addr is the break
// instruction for this platform.
bool DebuggerController::IsPatched(CORDB_ADDRESS_TYPE *address, BOOL native)
{
    LIMITED_METHOD_CONTRACT;

    if (native)
    {
        return AddressIsBreakpoint(address);
    }
    else
        return false;
}

// DWORD DebuggerController::GetPatchedOpcode()  Gets the opcode
// at addr, 'looking underneath' any patches if needed.
// GetPatchedInstruction is a function for the EE to call to "see through"
// a patch to the opcodes which was patched.
// How: Lock() grab opcode directly unless there's a patch, in
// which case grab it out of the patch table.
// BYTE * address:  The address that we want to 'see through'
// Returns:  DWORD value, that is the opcode that should really be there,
//         if we hadn't placed a patch there.  If we haven't placed a patch
//        there, then we'll see the actual opcode at that address.
PRD_TYPE DebuggerController::GetPatchedOpcode(CORDB_ADDRESS_TYPE *address)
{
    _ASSERTE(g_patches != NULL);

    PRD_TYPE opcode;
    ZeroMemory(&opcode, sizeof(opcode));

    ControllerLockHolder lockController;

    //
    // Look for a patch at the address
    //

    DebuggerControllerPatch *patch = g_patches->GetPatch((CORDB_ADDRESS_TYPE *)address);

    if (patch != NULL)
    {
        // Since we got the patch at this address, is must by definition be bound to that address
        _ASSERTE( patch->IsBound() );
        _ASSERTE( patch->address == address );
        // If we're going to be returning it's opcode, then the patch must also be activated
        _ASSERTE( patch->IsActivated() );
        opcode = patch->opcode;
    }
    else
    {
        //
        // Patch was not found - it either is not our patch, or it has
        // just been removed. In either case, just return the current
        // opcode.
        //

        if (g_pEEInterface->IsManagedNativeCode((const BYTE *)address))
        {
            opcode = CORDbgGetInstruction((CORDB_ADDRESS_TYPE *)address);
        }
// <REVISIT_TODO>
// TODO: : determine if this is needed for AMD64
// </REVISIT_TODO>
#ifdef TARGET_X86 //what is this?!
        else
        {
            //
            // !!! IL patch logic assumes reference encoding
            //

            opcode = *(unsigned short*)(address+1);
        }
#endif //TARGET_X86

    }

    return opcode;
}

// Holding the controller lock, this will check if an address is patched,
// and if so will then set the PRT_TYPE out parameter to the unpatched value.
BOOL DebuggerController::CheckGetPatchedOpcode(CORDB_ADDRESS_TYPE *address,
                                               /*OUT*/ PRD_TYPE *pOpcode)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(g_patches != NULL);

    BOOL res;

    ControllerLockHolder lockController;

    //
    // Look for a patch at the address
    //

    if (IsAddressPatched(address))
    {
        *pOpcode = GetPatchedOpcode(address);
        res = TRUE;
    }
    else
    {
        InitializePRD(pOpcode);
        res = FALSE;
    }


    return res;
}

// void DebuggerController::ActivatePatch()  Place a breakpoint
// so that threads will trip over this patch.
// If there any patches at the address already, then copy
// their opcode into this one & return.  Otherwise,
// call ApplyPatch(patch).  There is an implicit list of patches at this
// address by virtue of the fact that we can iterate through all the
// patches in the patch with the same address.
// DebuggerControllerPatch *patch:  The patch to activate
/* static */ void DebuggerController::ActivatePatch(DebuggerControllerPatch *patch)
{
    _ASSERTE(g_patches != NULL);
    _ASSERTE(patch != NULL);
    _ASSERTE(patch->IsBound() );
    _ASSERTE(!patch->IsActivated() );

    bool fApply = true;

    //
    // See if we already have an active patch at this address.
    //
    for (DebuggerControllerPatch *p = g_patches->GetPatch(patch->address);
         p != NULL;
         p = g_patches->GetNextPatch(p))
    {
        if (p != patch)
        {
            // If we're going to skip activating 'patch' because 'p' already exists at the same address
            // then 'p' must be activated.  We expect that all bound patches are activated.
            _ASSERTE( p->IsActivated() );
            patch->opcode = p->opcode;
            fApply = false;
            break;
        }
    }

    //
    // This is the only patch at this address - apply the patch
    // to the code.
    //
    if (fApply)
    {
        ApplyPatch(patch);
    }

    _ASSERTE(patch->IsActivated() );
}

// void DebuggerController::DeactivatePatch()  Make sure that a
// patch won't be hit.
// How: If this patch is the last one at this address, then
// UnapplyPatch.  The caller should then invoke RemovePatch to remove the
// patch from the patch table.
// DebuggerControllerPatch *patch:  Patch to deactivate
void DebuggerController::DeactivatePatch(DebuggerControllerPatch *patch)
{
    _ASSERTE(g_patches != NULL);

    if( !patch->IsBound() ) {
        // patch is not bound, nothing to do
        return;
    }

    // We expect that all bound patches are also activated.
    // One exception to this is if the shutdown thread killed another thread right after
    // if deactivated a patch but before it got to remove it.
    _ASSERTE(patch->IsActivated() );

    bool fUnapply = true;

    //
    // See if we already have an active patch at this address.
    //
    for (DebuggerControllerPatch *p = g_patches->GetPatch(patch->address);
         p != NULL;
         p = g_patches->GetNextPatch(p))
    {
        if (p != patch)
        {
            // There is another patch at this address, so don't remove it
            // However, clear the patch data so that we no longer consider this particular patch activated
            fUnapply = false;
            InitializePRD(&(patch->opcode));
            break;
        }
    }

    if (fUnapply)
    {
        UnapplyPatch(patch);
    }

     _ASSERTE(!patch->IsActivated() );

    //
    // Patch must now be removed from the table.
    //
}

// AddILMasterPatch: record a patch on IL code but do not bind it or activate it.  The master b.p.
// is associated with a module/token pair.  It is used later
// (e.g. in MapAndBindFunctionPatches) to create one or more "slave"
// breakpoints which are associated with particular MethodDescs/JitInfos.
//
// Rationale: For generic code a single IL patch (e.g a breakpoint)
// may give rise to several patches, one for each JITting of
// the IL (i.e. generic code may be JITted multiple times for
// different instantiations).
//
// So we keep one patch which describes
// the breakpoint but which is never actually bound or activated.
// This is then used to apply new "slave" patches to all copies of
// JITted code associated with the method.
//
// <REVISIT_TODO>In theory we could bind and apply the master patch when the
// code is known not to be generic (as used to happen to all breakpoint
// patches in V1).  However this seems like a premature
// optimization.</REVISIT_TODO>
DebuggerControllerPatch *DebuggerController::AddILMasterPatch(Module *module,
                                                              mdMethodDef md,
                                                              MethodDesc *pMethodDescFilter,
                                                              SIZE_T offset,
                                                              BOOL offsetIsIL,
                                                              SIZE_T encVersion)
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(g_patches != NULL);

    ControllerLockHolder ch;


    DebuggerControllerPatch *patch = g_patches->AddPatchForMethodDef(this,
                                     module,
                                     md,
                                     pMethodDescFilter,
                                     offset,
                                     offsetIsIL,
                                     PATCH_KIND_IL_MASTER,
                                     LEAF_MOST_FRAME,
                                     NULL,
                                     encVersion,
                                     NULL);

    LOG((LF_CORDB, LL_INFO10000,
        "DC::AP: Added IL master patch 0x%p for mdTok 0x%x, desc 0x%p at %s offset %d encVersion %d\n",
        patch, md, pMethodDescFilter, offsetIsIL ? "il" : "native", offset, encVersion));

    return patch;
}

// See notes above on AddILMasterPatch
BOOL DebuggerController::AddBindAndActivateILSlavePatch(DebuggerControllerPatch *master,
                                                        DebuggerJitInfo *dji)
{
    _ASSERTE(g_patches != NULL);
    _ASSERTE(master->IsILMasterPatch());
    _ASSERTE(dji != NULL);

    BOOL   result = FALSE;

    if (!master->offsetIsIL)
    {
        // Zero is the only native offset that we allow to bind across different jitted
        // code bodies.
        _ASSERTE(master->offset == 0);
        INDEBUG(BOOL fOk = )
            AddBindAndActivatePatchForMethodDesc(dji->m_nativeCodeVersion.GetMethodDesc(), dji,
                0, PATCH_KIND_IL_SLAVE,
                LEAF_MOST_FRAME, m_pAppDomain);
        _ASSERTE(fOk);
        result = TRUE;
    }
    else // bind by IL offset
    {
        // Do not dereference the "master" pointer in the loop!  The loop may add more patches,
        // causing the patch table to grow and move.
        SIZE_T masterILOffset = master->offset;

        // Loop through all the native offsets mapped to the given IL offset.  On x86 the mapping
        // should be 1:1.  On WIN64, because there are funclets, we have have an 1:N mapping.
        DebuggerJitInfo::ILToNativeOffsetIterator it;
        for (dji->InitILToNativeOffsetIterator(it, masterILOffset); !it.IsAtEnd(); it.Next())
        {
            BOOL   fExact;
            SIZE_T offsetNative = it.Current(&fExact);

            // We special case offset 0, which is when a breakpoint is set
            // at the beginning of a method that hasn't been jitted yet.  In
            // that case it's possible that offset 0 has been optimized out,
            // but we still want to set the closest breakpoint to that.
            if (!fExact && (masterILOffset != 0))
            {
                LOG((LF_CORDB, LL_INFO10000, "DC::BP:Failed to bind patch at IL offset 0x%p in %s::%s\n",
                    masterILOffset, dji->m_nativeCodeVersion.GetMethodDesc()->m_pszDebugClassName, dji->m_nativeCodeVersion.GetMethodDesc()->m_pszDebugMethodName));

                continue;
            }
            else
            {
                result = TRUE;
            }

            INDEBUG(BOOL fOk = )
                AddBindAndActivatePatchForMethodDesc(dji->m_nativeCodeVersion.GetMethodDesc(), dji,
                    offsetNative, PATCH_KIND_IL_SLAVE,
                    LEAF_MOST_FRAME, m_pAppDomain);
            _ASSERTE(fOk);
        }
    }

    // As long as we have successfully bound at least one patch, we consider the operation successful.
    return result;
}



// This routine places a patch that is conceptually a patch on the IL code.
// The IL code may be jitted multiple times, e.g. due to generics.
// This routine ensures that both present and subsequent JITtings of code will
// also be patched.
//
// This routine will return FALSE only if we will _never_ be able to
// place the patch in any native code corresponding to the given offset.
// Otherwise it will:
// (a) record a "master" patch
// (b) apply as many slave patches as it can to existing copies of code
//     that have debugging information
BOOL DebuggerController::AddILPatch(AppDomain * pAppDomain, Module *module,
                                  mdMethodDef md,
                                  MethodDesc *pMethodDescFilter,
                                  SIZE_T encVersion,  // what encVersion does this apply to?
                                  SIZE_T offset,
                                  BOOL offsetIsIL)
{
    _ASSERTE(g_patches != NULL);
    _ASSERTE(md != NULL);
    _ASSERTE(module != NULL);

    BOOL fOk = FALSE;

    DebuggerMethodInfo *dmi = g_pDebugger->GetOrCreateMethodInfo(module, md); // throws
    LOG((LF_CORDB,LL_INFO10000,"DC::AILP: dmi:0x%p, mdToken:0x%x, mdFilter:0x%p, "
            "encVer:%zu, offset:0x%zx <- isIL:%d, Mod:0x%p\n",
            dmi, md, pMethodDescFilter, encVersion, offset, offsetIsIL, module));

    if (dmi == NULL)
    {
        return false;
    }

    EX_TRY
    {
        // OK, we either have (a) no code at all or (b) we have both JIT information and code
        //.
        // Either way, lay down the MasterPatch.
        //
        // MapAndBindFunctionPatches will take care of any instantiations that haven't
        // finished JITting, by making a copy of the master breakpoint.
        DebuggerControllerPatch *master = AddILMasterPatch(module, md, pMethodDescFilter, offset, offsetIsIL, encVersion);

        // We have to keep the index here instead of the pointer.  The loop below adds more patches,
        // which may cause the patch table to grow and move.
        ULONG masterIndex = g_patches->GetItemIndex((HASHENTRY*)master);

        // Iterate through every existing NativeCodeBlob (with the same EnC version).
        // This includes generics + prejitted code.
        DebuggerMethodInfo::DJIIterator it;
        dmi->IterateAllDJIs(pAppDomain, NULL /* module filter */, pMethodDescFilter, &it);

        if (it.IsAtEnd())
        {
            // It is okay if we don't have any DJIs yet.  It just means that the method hasn't been jitted.
            fOk = TRUE;
        }
        else
        {
            // On the other hand, if the method has been jitted, then we expect to be able to bind at least
            // one breakpoint. The exception is when we have multiple EnC versions of the method, in which
            // case it is ok if we don't bind any breakpoint. One scenario is when a method has been updated
            // via EnC but it's not yet jitted. We need to allow a debugger to put a breakpoint on the new
            // version of the method, but the new version won't have a DJI yet.
            BOOL fVersionMatch = FALSE;
            while(!it.IsAtEnd())
            {
                DebuggerJitInfo *dji = it.Current();
                _ASSERTE(dji->m_jitComplete);
                if (dji->m_encVersion == encVersion &&
                   (pMethodDescFilter == NULL || pMethodDescFilter == dji->m_nativeCodeVersion.GetMethodDesc()))
                {
                    fVersionMatch = TRUE;

                    master = (DebuggerControllerPatch *)g_patches->GetEntryPtr(masterIndex);

                    // <REVISIT_TODO> If we're missing JIT info for any then
                    // we won't have applied the bp to every instantiation.  That should probably be reported
                    // as a new kind of condition to the debugger, i.e. report "bp only partially applied".  It would be
                    // a shame to completely fail just because on instantiation is missing debug info: e.g. just because
                    // one component hasn't been prejitted with debugging information.</REVISIT_TODO>
                    fOk = (AddBindAndActivateILSlavePatch(master, dji) || fOk);
                }
                it.Next();
            }

            // This is the exceptional case referred to in the comment above.  If we fail to put a breakpoint
            // because we don't have a matching version of the method, we need to return TRUE.
            if (fVersionMatch == FALSE)
            {
                fOk = TRUE;
            }
        }
    }
    EX_CATCH
    {
        fOk = FALSE;
    }
    EX_END_CATCH(SwallowAllExceptions)
    return fOk;
}

// Add a patch at native-offset 0 in the latest version of the method.
// This is used by step-in.
// Calls to new methods always go to the latest version, so EnC is not an issue here.
// The method may be not yet jitted. Or it may be prejitted.
void DebuggerController::AddPatchToStartOfLatestMethod(MethodDesc * fd)
{
    CONTRACTL
    {
        THROWS; // from GetJitInfo
        GC_NOTRIGGER;
        MODE_ANY; // don't really care what mode we're in.

        PRECONDITION(ThisMaybeHelperThread());
        PRECONDITION(CheckPointer(fd));
    }
    CONTRACTL_END;

    _ASSERTE(g_patches != NULL);
    Module* pModule = fd->GetModule();
    mdToken defToken = fd->GetMemberDef();
    DebuggerMethodInfo* pDMI = g_pDebugger->GetOrCreateMethodInfo(pModule, defToken);
    DebuggerController::AddILPatch(GetAppDomain(), pModule, defToken, fd, pDMI->GetCurrentEnCVersion(), 0, FALSE);
    return;
}


// Place patch in method at native offset.
BOOL DebuggerController::AddBindAndActivateNativeManagedPatch(MethodDesc * fd,
                                  DebuggerJitInfo *dji,
                                  SIZE_T offsetNative,
                                  FramePointer fp,
                                  AppDomain *pAppDomain)
{
    CONTRACTL
    {
        THROWS; // from GetJitInfo
        GC_NOTRIGGER;
        MODE_ANY; // don't really care what mode we're in.

        PRECONDITION(ThisMaybeHelperThread());
        PRECONDITION(CheckPointer(fd));
        PRECONDITION(fd->IsDynamicMethod() || (dji != NULL));
    }
    CONTRACTL_END;

    // For non-dynamic methods, we always expect to have a DJI, but just in case, we don't want the assert to AV.
    _ASSERTE((dji == NULL) || (fd == dji->m_nativeCodeVersion.GetMethodDesc()));
    _ASSERTE(g_patches != NULL);
    return DebuggerController::AddBindAndActivatePatchForMethodDesc(fd, dji, offsetNative, PATCH_KIND_NATIVE_MANAGED, fp, pAppDomain);
}

// Adds a breakpoint at a specific native offset in a particular jitted code version
BOOL DebuggerController::AddBindAndActivatePatchForMethodDesc(MethodDesc *fd,
                                  DebuggerJitInfo *dji,
                                  SIZE_T nativeOffset,
                                  DebuggerPatchKind kind,
                                  FramePointer fp,
                                  AppDomain *pAppDomain)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY; // don't really care what mode we're in.

        PRECONDITION(ThisMaybeHelperThread());
        PRECONDITION(kind != PATCH_KIND_IL_MASTER);
    }
    CONTRACTL_END;

    BOOL ok = FALSE;
    ControllerLockHolder ch;

    LOG((LF_CORDB|LF_ENC,LL_INFO10000,"DC::AP: Add to %s::%s, at offs 0x%x "
            "fp:0x%p AD:0x%p\n", fd->m_pszDebugClassName,
            fd->m_pszDebugMethodName,
            nativeOffset, fp.GetSPValue(), pAppDomain));

    DebuggerControllerPatch *patch = g_patches->AddPatchForMethodDef(
                            this,
                            g_pEEInterface->MethodDescGetModule(fd),
                            fd->GetMemberDef(),
                            NULL,
                            nativeOffset,
                            FALSE,
                            kind,
                            fp,
                            pAppDomain,
                            NULL,
                            dji);

    if (DebuggerController::BindPatch(patch, fd, NULL))
    {
        LOG((LF_CORDB|LF_ENC,LL_INFO1000,"BindPatch went fine, doing ActivatePatch\n"));
        DebuggerController::ActivatePatch(patch);
        ok = TRUE;
    }

    return ok;
}


// This version is particularly useful b/c it doesn't assume that the
// patch is inside a managed method.
DebuggerControllerPatch *DebuggerController::AddAndActivateNativePatchForAddress(CORDB_ADDRESS_TYPE *address,
                                  FramePointer fp,
                                  bool managed,
                                  TraceType traceType)
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
        GC_NOTRIGGER;

        PRECONDITION(g_patches != NULL);
    }
    CONTRACTL_END;


    ControllerLockHolder ch;

    DebuggerControllerPatch *patch
      = g_patches->AddPatchForAddress(this,
                            NULL,
                            0,
                            (managed? PATCH_KIND_NATIVE_MANAGED : PATCH_KIND_NATIVE_UNMANAGED),
                            address,
                            fp,
                            NULL,
                            NULL,
                            DebuggerPatchTable::DCP_PID_INVALID,
                            traceType);

    ActivatePatch(patch);

    return patch;
}

void DebuggerController::RemovePatchesFromModule(Module *pModule, AppDomain *pAppDomain )
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO100000, "DPT::CPFM mod:0x%p (%S)\n",
        pModule, pModule->GetDebugName()));

    // First find all patches of interest
    DebuggerController::ControllerLockHolder ch;
    HASHFIND f;
    for (DebuggerControllerPatch *patch = g_patches->GetFirstPatch(&f);
         patch != NULL;
         patch = g_patches->GetNextPatch(&f))
    {
        // Skip patches not in the specified domain
        if ((pAppDomain != NULL) && (patch->pAppDomain != pAppDomain))
            continue;

        BOOL fRemovePatch = FALSE;

        // Remove both native and IL patches the belong to this module
        if (patch->HasDJI())
        {
            DebuggerJitInfo * dji = patch->GetDJI();

            _ASSERTE(patch->key.module == dji->m_nativeCodeVersion.GetMethodDesc()->GetModule());

            // It is not necessary to check for m_fd->GetModule() here. It will
            // be covered by other module unload notifications issued for the appdomain.
            if ( dji->m_pLoaderModule == pModule )
                fRemovePatch = TRUE;
        }
        else
        if (patch->key.module == pModule)
        {
            fRemovePatch = TRUE;
        }

        if (fRemovePatch)
        {
            LOG((LF_CORDB, LL_EVERYTHING, "Removing patch 0x%p\n",
                patch));
            // we shouldn't be both hitting this patch AND
            // unloading the module it belongs to.
            _ASSERTE(!patch->IsTriggering());
            Release( patch );
        }
    }
}

#ifdef _DEBUG
bool DebuggerController::ModuleHasPatches( Module* pModule )
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if( g_patches == NULL )
    {
        // Patch table hasn't been initialized
        return false;
    }

    // First find all patches of interest
    HASHFIND f;
    for (DebuggerControllerPatch *patch = g_patches->GetFirstPatch(&f);
         patch != NULL;
         patch = g_patches->GetNextPatch(&f))
    {
        //
        // This mirrors logic in code:DebuggerController::RemovePatchesFromModule
        //

        if (patch->HasDJI())
        {
            DebuggerJitInfo * dji = patch->GetDJI();

            _ASSERTE(patch->key.module == dji->m_nativeCodeVersion.GetMethodDesc()->GetModule());

            // It may be sufficient to just check m_pLoaderModule here. Since this is used for debug-only
            // check, we will check for m_fd->GetModule() as well to catch more potential problems.
            if ( (dji->m_pLoaderModule == pModule) || (dji->m_nativeCodeVersion.GetMethodDesc()->GetModule() == pModule) )
            {
                return true;
            }
        }

        if (patch->key.module == pModule)
        {
            return true;
        }
    }

    return false;
}
#endif // _DEBUG

//
// Returns true if the given address is in an internal helper
// function, false if its not.
//
// This is a temporary workaround function to avoid having us stop in
// unmanaged code belonging to the Runtime during a StepIn operation.
//
static bool _AddrIsJITHelper(PCODE addr)
{
#if !defined(HOST_64BIT) && !defined(TARGET_UNIX)
    // Is the address in the runtime dll (clr.dll or coreclr.dll) at all? (All helpers are in
    // that dll)
    if (g_runtimeLoadedBaseAddress <= addr &&
            addr < g_runtimeLoadedBaseAddress + g_runtimeVirtualSize)
    {
        for (int i = 0; i < CORINFO_HELP_COUNT; i++)
        {
            if (hlpFuncTable[i].pfnHelper == (void*)addr)
            {
                LOG((LF_CORDB, LL_INFO10000,
                        "_ANIM: address of helper function found: 0x%08x\n",
                        addr));
                return true;
            }
        }

        for (unsigned d = 0; d < DYNAMIC_CORINFO_HELP_COUNT; d++)
        {
            if (hlpDynamicFuncTable[d].pfnHelper == (void*)addr)
            {
                LOG((LF_CORDB, LL_INFO10000,
                        "_ANIM: address of helper function found: 0x%08x\n",
                        addr));
                return true;
            }
        }

        LOG((LF_CORDB, LL_INFO10000,
             "_ANIM: address within runtime dll, but not a helper function "
             "0x%08x\n", addr));
    }
#else // !defined(HOST_64BIT) && !defined(TARGET_UNIX)
    // TODO: Figure out what we want to do here
#endif // !defined(HOST_64BIT) && !defined(TARGET_UNIX)

    return false;
}

// bool DebuggerController::PatchTrace()   What: Invoke
// AddPatch depending on the type of the given TraceDestination.
// How: Invokes AddPatch based on the trace type: TRACE_OTHER will
// return false, the others will obtain args for a call to an AddPatch
// method & return true.
//
// Return true if we set a patch, else false
bool DebuggerController::PatchTrace(TraceDestination *trace,
                                    FramePointer fp,
                                    bool fStopInUnmanaged)
{
    CONTRACTL
    {
        THROWS; // Because AddPatch may throw on oom. We may want to convert this to nothrow and return false.
        MODE_ANY;
        DISABLED(GC_TRIGGERS); // @todo - what should this be?

        PRECONDITION(ThisMaybeHelperThread());
    }
    CONTRACTL_END;
    DebuggerControllerPatch *dcp = NULL;
    SIZE_T nativeOffset = 0;

    switch (trace->GetTraceType())
    {
    case TRACE_ENTRY_STUB:  // fall through
    case TRACE_UNMANAGED:
        LOG((LF_CORDB, LL_INFO10000,
             "DC::PT: Setting unmanaged trace patch at 0x%p(%p)\n",
             trace->GetAddress(), fp.GetSPValue()));

        if (fStopInUnmanaged && !_AddrIsJITHelper(trace->GetAddress()))
        {
            AddAndActivateNativePatchForAddress((CORDB_ADDRESS_TYPE *)trace->GetAddress(),
                     fp,
                     FALSE,
                     trace->GetTraceType());
            return true;
        }
        else
        {
            LOG((LF_CORDB, LL_INFO10000, "DC::PT: decided to NOT "
                "place a patch in unmanaged code\n"));
            return false;
        }

    case TRACE_MANAGED:
        LOG((LF_CORDB, LL_INFO10000,
             "Setting managed trace patch at 0x%p(%p)\n", trace->GetAddress(), fp.GetSPValue()));

        MethodDesc *fd;
        fd = g_pEEInterface->GetNativeCodeMethodDesc(trace->GetAddress());
        _ASSERTE(fd);

        DebuggerJitInfo *dji;
        dji = g_pDebugger->GetJitInfoFromAddr(trace->GetAddress());
        //_ASSERTE(dji); //we'd like to assert this, but attach won't work

        nativeOffset = CodeRegionInfo::GetCodeRegionInfo(dji, fd).AddressToOffset((const BYTE *)trace->GetAddress());

        // Code versioning allows calls to be redirected to alternate code potentially after this trace is complete but before
        // execution reaches the call target. Rather than bind the breakpoint to a specific jitted code instance that is currently
        // configured to receive execution we need to prepare for that potential retargetting by binding all jitted code instances.
        //
        // Triggering this based of the native offset is a little subtle, but all of the stubmanagers follow a rule that if they
        // trace across a call boundary into jitted code they either stop at offset zero of the new method, or they continue tracing
        // out of that jitted code.
        if (nativeOffset == 0)
        {
            AddPatchToStartOfLatestMethod(fd);
        }
        else
        {
            AddBindAndActivateNativeManagedPatch(fd, dji, nativeOffset, fp, NULL);
        }


        return true;

    case TRACE_UNJITTED_METHOD:
        // trace->address is actually a MethodDesc* of the method that we'll
        // soon JIT, so put a relative bp at offset zero in.
        LOG((LF_CORDB, LL_INFO10000,
            "Setting unjitted method patch in MethodDesc 0x%p %s\n", trace->GetMethodDesc(), trace->GetMethodDesc() ? trace->GetMethodDesc()->m_pszDebugMethodName : ""));

        // Note: we have to make sure to bind here. If this function is prejitted, this may be our only chance to get a
        // DebuggerJITInfo and thereby cause a JITComplete callback.
        AddPatchToStartOfLatestMethod(trace->GetMethodDesc());
        return true;

    case TRACE_FRAME_PUSH:
        LOG((LF_CORDB, LL_INFO10000,
             "Setting frame patch at 0x%p(%p)\n", trace->GetAddress(), fp.GetSPValue()));

        AddAndActivateNativePatchForAddress((CORDB_ADDRESS_TYPE *)trace->GetAddress(),
                 fp,
                 TRUE,
                 TRACE_FRAME_PUSH);
        return true;

    case TRACE_MGR_PUSH:
        LOG((LF_CORDB, LL_INFO10000,
             "Setting frame patch (TRACE_MGR_PUSH) at 0x%p(%p)\n",
             trace->GetAddress(), fp.GetSPValue()));

        dcp = AddAndActivateNativePatchForAddress((CORDB_ADDRESS_TYPE *)trace->GetAddress(),
                       LEAF_MOST_FRAME, // But Mgr_push can't have fp affinity!
                       TRUE,
                       DPT_DEFAULT_TRACE_TYPE); // TRACE_OTHER
        // Now copy over the trace field since TriggerPatch will expect this
        // to be set for this case.
        if (dcp != NULL)
        {
            dcp->trace = *trace;
        }

        return true;

    case TRACE_OTHER:
        LOG((LF_CORDB, LL_INFO10000,
             "Can't set a trace patch for TRACE_OTHER...\n"));
        return false;

    default:
        _ASSERTE(0);
        return false;
    }
}

//-----------------------------------------------------------------------------
// Checks if the patch matches the context + thread.
// Multiple patches can exist at a single address, so given a patch at the
// Context's current address, this does additional patch-affinity checks like
// thread, AppDomain, and frame-pointer.
// thread - thread executing the given context that hit the patch
// context - context of the thread that hit the patch
// patch - candidate patch that we're looking for a match.
// Returns:
//     True if the patch matches.
//     False
//-----------------------------------------------------------------------------
bool DebuggerController::MatchPatch(Thread *thread,
                                    CONTEXT *context,
                                    DebuggerControllerPatch *patch)
{
    LOG((LF_CORDB, LL_INFO100000, "DC::MP: EIP:0x%p\n", GetIP(context)));

    // Caller should have already matched our addresses.
    if (patch->address != dac_cast<PTR_CORDB_ADDRESS_TYPE>(GetIP(context)))
    {
        return false;
    }

    // <BUGNUM>RAID 67173 -</BUGNUM> we'll make sure that intermediate patches have NULL
    // pAppDomain so that we don't end up running to completion when
    // the appdomain switches halfway through a step.
    if (patch->pAppDomain != NULL)
    {
        AppDomain *pAppDomainCur = thread->GetDomain();

        if (pAppDomainCur != patch->pAppDomain)
        {
            LOG((LF_CORDB, LL_INFO10000, "DC::MP: patches didn't match b/c of "
                "appdomains!\n"));
            return false;
        }
    }

    if (patch->controller->m_thread != NULL && patch->controller->m_thread != thread)
    {
        LOG((LF_CORDB, LL_INFO10000, "DC::MP: patches didn't match b/c threads\n"));
        return false;
    }

    if (patch->fp != LEAF_MOST_FRAME)
    {
        // If we specified a Frame-pointer, than it should have been safe to take a stack trace.

        ControllerStackInfo info;
        StackTraceTicket ticket(patch);
        info.GetStackInfo(ticket, thread, LEAF_MOST_FRAME, context);

        // !!! This check should really be != , but there is some ambiguity about which frame is the parent frame
        // in the destination returned from Frame::TraceFrame, so this allows some slop there.

        if (info.HasReturnFrame() && IsCloserToLeaf(info.GetReturnFrame().fp, patch->fp))
        {
            LOG((LF_CORDB, LL_INFO10000, "Patch hit but frame not matched at %p (current=%p, patch=%p)\n",
                patch->address, info.GetReturnFrame().fp.GetSPValue(), patch->fp.GetSPValue()));

            return false;
        }
    }

    LOG((LF_CORDB, LL_INFO100000, "DC::MP: Returning true"));

    return true;
}

DebuggerPatchSkip *DebuggerController::ActivatePatchSkip(Thread *thread,
                                                         const BYTE *PC,
                                                         BOOL fForEnC)
{
#ifdef _DEBUG
    BOOL shouldBreak = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_ActivatePatchSkip);
    if (shouldBreak > 0) {
        _ASSERTE(!"ActivatePatchSkip");
    }
#endif

    LOG((LF_CORDB,LL_INFO10000, "DC::APS thread=0x%p pc=0x%p fForEnc=%d\n",
        thread, PC, fForEnC));
    _ASSERTE(g_patches != NULL);

    //      Previously, we assumed that if we got to this point & the patch
    // was still there that we'd have to skip the patch.  SetIP changes
    // this like so:
    //      A breakpoint is set, and hit (but not removed), and all the
    // EE threads come to a skreeching halt.  The Debugger RC thread
    // continues along, and is told to SetIP of the thread that hit
    // the BP to whatever.  Eventually the RC thread is told to continue,
    // and at that point the EE thread is released, finishes DispatchPatchOrSingleStep,
    // and shows up here.
    //      At that point, if the thread's current PC is
    // different from the patch PC, then SetIP must have moved it elsewhere
    // & we shouldn't do this patch skip (which will put us back to where
    // we were, which is clearly wrong).  If the PC _is_ the same, then
    // the thread hasn't been moved, the patch is still in the code stream,
    // and we want to do the patch skip thing in order to execute this
    // instruction w/o removing it from the code stream.

    DebuggerControllerPatch *patch = g_patches->GetPatch((CORDB_ADDRESS_TYPE *)PC);
    DebuggerPatchSkip *skip = NULL;

    if (patch != NULL && patch->IsNativePatch())
    {
        //
        // We adjust the thread's PC to someplace where we write
        // the next instruction, then
        // we single step over that, then we set the PC back here so
        // we don't let other threads race past here while we're stepping
        // this one.
        //
        // !!! check result
        LOG((LF_CORDB,LL_INFO10000, "DC::APS: About to skip from PC=0x%p\n", PC));
        skip = new (interopsafe) DebuggerPatchSkip(thread, patch, thread->GetDomain());
        TRACE_ALLOC(skip);
    }

    return skip;
}

DPOSS_ACTION DebuggerController::ScanForTriggers(CORDB_ADDRESS_TYPE *address,
                                         Thread *thread,
                                         CONTEXT *context,
                                         DebuggerControllerQueue *pDcq,
                                         SCAN_TRIGGER stWhat,
                                         TP_RESULT *pTpr)
{
    CONTRACTL
    {
        // @todo - should this throw or not?
        NOTHROW;

        // call Triggers which may invoke GC stuff... See comment in DispatchNativeException for why it's disabled.
        DISABLED(GC_TRIGGERS);
        PRECONDITION(!ThisIsHelperThreadWorker());

        PRECONDITION(CheckPointer(address));
        PRECONDITION(CheckPointer(thread));
        PRECONDITION(CheckPointer(context));
        PRECONDITION(CheckPointer(pDcq));
        PRECONDITION(CheckPointer(pTpr));
    }
    CONTRACTL_END;

    _ASSERTE(HasLock());

    CONTRACT_VIOLATION(ThrowsViolation);

    LOG((LF_CORDB, LL_INFO10000, "DC::SFT: starting scan for addr:0x%p"
            " thread:0x%x\n", address, thread));

    _ASSERTE( pTpr != NULL );
    DebuggerControllerPatch *patch = NULL;

    if (g_patches != NULL)
        patch = g_patches->GetPatch(address);

    ULONG iEvent = UINT32_MAX;
    ULONG iEventNext = UINT32_MAX;
    BOOL fDone = FALSE;

    // This is a debugger exception if there's a patch here, or
    // we're here for something like a single step.
    DPOSS_ACTION used = DPOSS_INVALID;
    if ((patch != NULL) || !IsPatched(address, TRUE))
    {
        // we are sure that we care for this exception but not sure
        // if we will send event to the RS
        used = DPOSS_USED_WITH_NO_EVENT;
    }
    else
    {
        // initialize it to don't care for now
        used = DPOSS_DONT_CARE;
    }

    TP_RESULT tpr = TPR_IGNORE;

    while (stWhat & ST_PATCH &&
           patch != NULL &&
           !fDone)
    {
        _ASSERTE(IsInUsedAction(used) == true);

        DebuggerControllerPatch *patchNext
          = g_patches->GetNextPatch(patch);

        LOG((LF_CORDB, LL_INFO10000, "DC::SFT: patch 0x%x, patchNext 0x%x\n", patch, patchNext));

        // Annoyingly, TriggerPatch may add patches, which may cause
        // the patch table to move, which may, in turn, invalidate
        // the patch (and patchNext) pointers.  Store indeces, instead.
        iEvent = g_patches->GetItemIndex( (HASHENTRY *)patch );

        if (patchNext != NULL)
        {
            iEventNext = g_patches->GetItemIndex((HASHENTRY *)patchNext);
        }

        if (MatchPatch(thread, context, patch))
        {
            LOG((LF_CORDB, LL_INFO10000, "DC::SFT: patch matched\n"));
            AddRef(patch);

            // We are hitting a patch at a virtual trace call target, so let's trigger trace call here.
            if (patch->trace.GetTraceType() == TRACE_ENTRY_STUB)
            {
                patch->controller->TriggerTraceCall(thread, dac_cast<PTR_CBYTE>(::GetIP(context)));
                tpr = TPR_IGNORE;
            }
            else
            {
                // Mark if we're at an unsafe place.
                AtSafePlaceHolder unsafePlaceHolder(thread);

                tpr = patch->controller->TriggerPatch(patch,
                                                    thread,
                                                    TY_NORMAL);
            }

            // Any patch may potentially send an event.
            // (Whereas some single-steps are "internal-only" and can
            // never send an event- such as a single step over an exception that
            // lands us in la-la land.)
            used = DPOSS_USED_WITH_EVENT;

            if (tpr == TPR_TRIGGER ||
                tpr == TPR_TRIGGER_ONLY_THIS ||
                tpr == TPR_TRIGGER_ONLY_THIS_AND_LOOP)
            {
                // Make sure we've still got a valid pointer.
                patch = (DebuggerControllerPatch *)
                    DebuggerController::g_patches->GetEntryPtr( iEvent );

                pDcq->dcqEnqueue(patch->controller, TRUE); // <REVISIT_TODO>@todo Return value</REVISIT_TODO>
            }

            // Make sure we've got a valid pointer in case TriggerPatch
            // returned false but still caused the table to move.
            patch = (DebuggerControllerPatch *)
                g_patches->GetEntryPtr( iEvent );

            // A patch can be deleted as a result of it's being triggered.
            // The actual deletion of the patch is delayed until after the
            // the end of the trigger.
            // Moreover, "patchNext" could have been deleted as a result of DisableAll()
            // being called in TriggerPatch().  Thus, we should update our patchNext
            // pointer now.  We were just lucky before, because the now-deprecated
            // "deleted" flag didn't get set when we iterate the patches in DisableAll().
            patchNext = g_patches->GetNextPatch(patch);
            if (patchNext != NULL)
                iEventNext = g_patches->GetItemIndex((HASHENTRY *)patchNext);

            // Note that Release() actually removes the patch if its ref count
            // reaches 0 after the release.
            Release(patch);
        }

        if (tpr == TPR_IGNORE_AND_STOP ||
            tpr == TPR_TRIGGER_ONLY_THIS ||
            tpr == TPR_TRIGGER_ONLY_THIS_AND_LOOP)
        {
#ifdef _DEBUG
            if (tpr == TPR_TRIGGER_ONLY_THIS ||
                tpr == TPR_TRIGGER_ONLY_THIS_AND_LOOP)
                _ASSERTE(pDcq->dcqGetCount() == 1);
#endif //_DEBUG

            fDone = TRUE;
        }
        else if (patchNext != NULL)
        {
            patch = (DebuggerControllerPatch *)
                g_patches->GetEntryPtr(iEventNext);
        }
        else
        {
            patch = NULL;
        }
    }

#ifdef FEATURE_DATABREAKPOINT
    if (stWhat & ST_SINGLE_STEP &&
        tpr != TPR_TRIGGER_ONLY_THIS &&
        DebuggerDataBreakpoint::IsDataBreakpoint(thread, context))
    {
        if (g_pDebugger->m_isSuspendedForGarbageCollection)
        {
            // The debugger is not interested in Data Breakpoints during garbage collection
            // We can safely ignore them since the Data Breakpoints are now on pinned objects
            LOG((LF_CORDB, LL_INFO10000, "D:DDBP: Ignoring data breakpoint while suspended for GC \n"));

            used = DPOSS_USED_WITH_NO_EVENT;
        }
        else if(DebuggerDataBreakpoint::TriggerDataBreakpoint(thread, context))
        {
            DebuggerDataBreakpoint *pDataBreakpoint = new (interopsafe) DebuggerDataBreakpoint(thread);
            pDcq->dcqEnqueue(pDataBreakpoint, FALSE);
        }
    }
#endif

    if (stWhat & ST_SINGLE_STEP &&
        tpr != TPR_TRIGGER_ONLY_THIS)
    {
        LOG((LF_CORDB, LL_INFO10000, "DC::SFT: Trigger controllers with single step\n"));

        //
        // Now, go ahead & trigger all controllers with
        // single step events
        //

        DebuggerController *p;

        p = g_controllers;
        while (p != NULL)
        {
            DebuggerController *pNext = p->m_next;

            if (p->m_thread == thread && p->m_singleStep)
            {
                if (used == DPOSS_DONT_CARE)
                {
                    // Debugger does care for this exception.
                    used = DPOSS_USED_WITH_NO_EVENT;
                }

                if (p->TriggerSingleStep(thread, (const BYTE *)address))
                {
                    // by now, we should already know that we care for this exception.
                    _ASSERTE(IsInUsedAction(used) == true);

                    // now we are sure that we will send event to the RS
                    used = DPOSS_USED_WITH_EVENT;
                    pDcq->dcqEnqueue(p, FALSE); // <REVISIT_TODO>@todo Return value</REVISIT_TODO>

                }
            }

            p = pNext;
        }

        UnapplyTraceFlag(thread);

        //
        // See if we have any steppers still active for this thread, if so
        // re-apply the trace flag.
        //

        p = g_controllers;
        while (p != NULL)
        {
            if (p->m_thread == thread && p->m_singleStep)
            {
                ApplyTraceFlag(thread);
                break;
            }

            p = p->m_next;
        }
    }

    // Significant speed increase from single dereference, I bet :)
    (*pTpr) = tpr;

    LOG((LF_CORDB, LL_INFO10000, "DC::SFT returning 0x%x as used\n",used));
    return used;
}

#ifdef EnC_SUPPORTED
DebuggerControllerPatch *DebuggerController::IsXXXPatched(const BYTE *PC,
                                                          DEBUGGER_CONTROLLER_TYPE dct)
{
    _ASSERTE(g_patches != NULL);

    DebuggerControllerPatch *patch = g_patches->GetPatch((CORDB_ADDRESS_TYPE *)PC);

    while(patch != NULL &&
          (int)patch->controller->GetDCType() <= (int)dct)
    {
        if (patch->IsNativePatch() &&
            patch->controller->GetDCType()==dct)
        {
            return patch;
        }
        patch = g_patches->GetNextPatch(patch);
    }

    return NULL;
}

// This function will check for an EnC patch at the given address and return
// it if one is there, otherwise it will return NULL.
DebuggerControllerPatch *DebuggerController::GetEnCPatch(const BYTE *address)
{
    _ASSERTE(address);

    if( g_pEEInterface->IsManagedNativeCode(address) )
    {
        DebuggerJitInfo *dji = g_pDebugger->GetJitInfoFromAddr((TADDR) address);
        if (dji == NULL)
            return NULL;

        // we can have two types of patches - one in code where the IL has been updated to trigger
        // the switch and the other in the code we've switched to in order to trigger FunctionRemapComplete
        // callback. If version == default then can't be the latter, but otherwise if haven't handled the
        // remap for this function yet is certainly the latter.
        if (! dji->m_encBreakpointsApplied &&
            (dji->m_encVersion == CorDB_DEFAULT_ENC_FUNCTION_VERSION))
        {
            return NULL;
        }
    }
    return IsXXXPatched(address, DEBUGGER_CONTROLLER_ENC);
}
#endif //EnC_SUPPORTED

// DebuggerController::DispatchPatchOrSingleStep - Ask any patches that are active at a given
// address if they want to do anything about the exception that's occurred there.  How: For the given
// address, go through the list of patches & see if any of them are interested (by invoking their
// DebuggerController's TriggerPatch).  Put any DCs that are interested into a queue and then calls
// SendEvent on each.
// Note that control will not return from this function in the case of EnC remap
DPOSS_ACTION DebuggerController::DispatchPatchOrSingleStep(Thread *thread, CONTEXT *context, CORDB_ADDRESS_TYPE *address, SCAN_TRIGGER which)
{
    CONTRACT(DPOSS_ACTION)
    {
        // @todo - should this throw or not?
        NOTHROW;
        DISABLED(GC_TRIGGERS); // Only GC triggers if we send an event. See Comment in DispatchNativeException
        PRECONDITION(!ThisIsHelperThreadWorker());

        PRECONDITION(CheckPointer(thread));
        PRECONDITION(CheckPointer(context));
        PRECONDITION(CheckPointer(address));
        PRECONDITION(!HasLock());

        POSTCONDITION(!HasLock()); // make sure we're not leaking the controller lock
    }
    CONTRACT_END;

    CONTRACT_VIOLATION(ThrowsViolation);

    LOG((LF_CORDB|LF_ENC,LL_INFO1000,"DC:DPOSS at 0x%p trigger:0x%x\n", address, which));

    // We should only have an exception if some managed thread was running.
    // Thus we should never be here when we're stopped.
    // @todo - this assert fires! Is that an issue, or is it invalid?
    //_ASSERTE(!g_pDebugger->IsStopped());
    DPOSS_ACTION used = DPOSS_DONT_CARE;

    DebuggerControllerQueue dcq;
    if (!g_patchTableValid)
    {

        LOG((LF_CORDB|LF_ENC, LL_INFO1000, "DC::DPOSS returning, no patch table.\n"));
        RETURN (used);
    }
    _ASSERTE(g_patches != NULL);

    CrstHolderWithState lockController(&g_criticalSection);

    TADDR originalAddress = 0;

#ifdef EnC_SUPPORTED
    DebuggerControllerPatch *dcpEnCOriginal = NULL;

    // If this sequence point has an EnC patch, we want to process it ahead of any others. If the
    // debugger wants to remap the function at this point, then we'll call ResumeInUpdatedFunction and
    // not return, otherwise we will just continue with regular patch-handling logic
    dcpEnCOriginal = GetEnCPatch(dac_cast<PTR_CBYTE>(GetIP(context)));

    if (dcpEnCOriginal)
    {
        LOG((LF_CORDB|LF_ENC,LL_INFO10000, "DC::DPOSS EnC short-circuit\n"));
        TP_RESULT tpres =
            dcpEnCOriginal->controller->TriggerPatch(dcpEnCOriginal,
                                                     thread,
                                                     TY_SHORT_CIRCUIT);

        // We will only come back here on a RemapOpportunity that wasn't taken, or on a RemapComplete.
        // If we processed a RemapComplete (which returns TPR_IGNORE_AND_STOP), then don't want to handle
        // additional breakpoints on the current line because we've already effectively executed to that point
        // and would have hit them already. If they are new, we also don't want to hit them because eg. if are
        // sitting on line 10 and add a breakpoint at line 10 and step,
        // don't expect to stop at line 10, expect to go to line 11.
        //
        // Special case is if an EnC remap breakpoint exists in the function. This could only happen if the function was
        // updated between the RemapOpportunity and the RemapComplete.  In that case we want to not skip the patches
        // and fall through to handle the remap breakpoint.

        if (tpres == TPR_IGNORE_AND_STOP)
        {
            // It was a RemapComplete, so fall through.  Set dcpEnCOriginal to NULL to indicate that any
            // EnC patch still there should be treated as a new patch.  Any RemapComplete patch will have been
            // already removed by patch processing.
            dcpEnCOriginal = NULL;
            LOG((LF_CORDB|LF_ENC,LL_INFO10000, "DC::DPOSS done EnC short-circuit, exiting\n"));
            used = DPOSS_USED_WITH_EVENT;    // indicate that we handled a patch
            goto Exit;
        }

        _ASSERTE(tpres==TPR_IGNORE);
        LOG((LF_CORDB|LF_ENC,LL_INFO10000, "DC::DPOSS done EnC short-circuit, ignoring\n"));
        // if we got here, then the EnC remap opportunity was not taken, so just continue on.
    }
#endif // EnC_SUPPORTED

    TP_RESULT tpr;

    used = ScanForTriggers((CORDB_ADDRESS_TYPE *)address, thread, context, &dcq, which, &tpr);

    LOG((LF_CORDB|LF_ENC, LL_EVERYTHING, "DC::DPOSS ScanForTriggers called and returned.\n"));


    // If we setip, then that will change the address in the context.
    // Remeber the old address so that we can compare it to the context's ip and see if it changed.
    // If it did change, then don't dispatch our current event.
    originalAddress = (TADDR) address;

#ifdef _DEBUG
    // If we do a SetIP after this point, the value of address will be garbage.  Set it to a distictive pattern now, so
    // we don't accidentally use what will (98% of the time) appear to be a valid value.
    address = (CORDB_ADDRESS_TYPE *)(UINT_PTR)0xAABBCCFF;
#endif //_DEBUG

    if (dcq.dcqGetCount()> 0)
    {
        lockController.Release();

        // Mark if we're at an unsafe place.
        bool atSafePlace = g_pDebugger->IsThreadAtSafePlace(thread);
        if (!atSafePlace)
            g_pDebugger->IncThreadsAtUnsafePlaces();

        DWORD dwEvent = 0xFFFFFFFF;
        DWORD dwNumberEvents = 0;

        SENDIPCEVENT_BEGIN(g_pDebugger, thread);

        // Now that we've resumed from blocking, check if somebody did a SetIp on us.
        bool fIpChanged = (originalAddress != GetIP(context));

        // Send the events outside of the controller lock
        bool anyEventsSent = false;

        dwNumberEvents = dcq.dcqGetCount();
        dwEvent = 0;

        while (dwEvent < dwNumberEvents)
        {
            DebuggerController *event = dcq.dcqGetElement(dwEvent);

            if (!event->m_deleted)
            {
#ifdef DEBUGGING_SUPPORTED
                if (thread->GetDomain()->IsDebuggerAttached())
                {
                    if (event->SendEvent(thread, fIpChanged))
                    {
                        anyEventsSent = true;
                    }
                }
#endif //DEBUGGING_SUPPORTED
            }

            dwEvent++;
        }

        // Trap all threads if necessary, but only if we actually sent a event up (i.e., all the queued events weren't
        // deleted before we got a chance to get the EventSending lock.)
        if (anyEventsSent)
        {
            LOG((LF_CORDB|LF_ENC, LL_EVERYTHING, "DC::DPOSS We sent an event\n"));
            g_pDebugger->SyncAllThreads(SENDIPCEVENT_PtrDbgLockHolder);
            LOG((LF_CORDB,LL_INFO1000, "SAT called!\n"));
        }

        SENDIPCEVENT_END;

        if (!atSafePlace)
            g_pDebugger->DecThreadsAtUnsafePlaces();

        lockController.Acquire();

        // Dequeue the events while we have the controller lock.
        dwEvent = 0;
        while (dwEvent < dwNumberEvents)
        {
            dcq.dcqDequeue();
            dwEvent++;
        }
    }

#if defined EnC_SUPPORTED
Exit:
#endif

    // Note: if the thread filter context is NULL, then SetIP would have failed & thus we should do the
    // patch skip thing.
    // @todo  - do we need to get the context again here?
    CONTEXT *pCtx = GetManagedLiveCtx(thread);

#ifdef EnC_SUPPORTED
    DebuggerControllerPatch *dcpEnCCurrent = GetEnCPatch(dac_cast<PTR_CBYTE>((GetIP(context))));

    // we have a new patch if the original was null and the current is non-null. Otherwise we have an old
    // patch. We want to skip old patches, but handle new patches.
    if (dcpEnCOriginal == NULL && dcpEnCCurrent != NULL)
    {
        LOG((LF_CORDB|LF_ENC,LL_INFO10000, "DC::DPOSS EnC post-processing\n"));
            dcpEnCCurrent->controller->TriggerPatch( dcpEnCCurrent,
                                                     thread,
                                                     TY_SHORT_CIRCUIT);
        used = DPOSS_USED_WITH_EVENT;    // indicate that we handled a patch
    }
#endif

    ActivatePatchSkip(thread, dac_cast<PTR_CBYTE>(GetIP(pCtx)), FALSE);

    lockController.Release();


    // We pulse the GC mode here too cooperate w/ a thread trying to suspend the runtime. If we didn't pulse
    // the GC, the odds of catching this thread in interuptable code may be very small (since this filter
    // could be very large compared to the managed code this thread is running).
    // Only do this if the exception was actually for the debugger. (We don't want to toggle the GC mode on every
    // random exception). We can't do this while holding any debugger locks.
    if (used == DPOSS_USED_WITH_EVENT)
    {
        bool atSafePlace = g_pDebugger->IsThreadAtSafePlace(thread);
        if (!atSafePlace)
        {
            g_pDebugger->IncThreadsAtUnsafePlaces();
        }

        // Always pulse the GC mode. This will allow an async break to complete even if we have a patch
        // at an unsafe place.
        // If we are at an unsafe place, then we can't do a GC.
        thread->PulseGCMode();

        if (!atSafePlace)
        {
            g_pDebugger->DecThreadsAtUnsafePlaces();
        }

    }

    RETURN used;
}

bool DebuggerController::IsSingleStepEnabled()
{
    LIMITED_METHOD_CONTRACT;
    return m_singleStep;
}

void DebuggerController::EnableSingleStep()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifdef _DEBUG
    // Some controllers don't need to set the SS to do their job, and if they are setting it, it's likely an issue.
    // So we assert here to catch them red-handed. This assert can always be updated to accomodate changes
    // in a controller's behavior.

    switch(GetDCType())
    {
        case DEBUGGER_CONTROLLER_THREAD_STARTER:
        case DEBUGGER_CONTROLLER_BREAKPOINT:
        case DEBUGGER_CONTROLLER_USER_BREAKPOINT:
        case DEBUGGER_CONTROLLER_FUNC_EVAL_COMPLETE:
            CONSISTENCY_CHECK_MSGF(false, ("Controller pThis=%p shouldn't be setting ss flag.", this));
            break;
        default: // MingW compilers require all enum cases to be handled in switch statement.
            break;
    }
#endif

    EnableSingleStep(m_thread);
    m_singleStep = true;
}

#ifdef EnC_SUPPORTED
// Note that this doesn't tell us if Single Stepping is currently enabled
// at the hardware level (ie, for x86, if (context->EFlags & 0x100), but
// rather, if we WANT single stepping enabled (pThread->m_State &Thread::TS_DebuggerIsStepping)
// This gets called from exactly one place - ActivatePatchSkipForEnC
BOOL DebuggerController::IsSingleStepEnabled(Thread *pThread)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // This should be an atomic operation, do we
    // don't need to lock it.
    if(pThread->m_StateNC & Thread::TSNC_DebuggerIsStepping)
    {
        _ASSERTE(pThread->m_StateNC & Thread::TSNC_DebuggerIsStepping);

        return TRUE;
    }
    else
        return FALSE;
}
#endif //EnC_SUPPORTED

void DebuggerController::EnableSingleStep(Thread *pThread)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    LOG((LF_CORDB,LL_INFO1000, "DC::EnableSingleStep\n"));

    _ASSERTE(pThread != NULL);

    ControllerLockHolder lockController;

    ApplyTraceFlag(pThread);
}

// Disable Single stepping for this controller.
// If none of the controllers on this thread want single-stepping, then also
// ensure that it's disabled on the hardware level.
void DebuggerController::DisableSingleStep()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(m_thread != NULL);

    LOG((LF_CORDB,LL_INFO1000, "DC::DisableSingleStep\n"));

    ControllerLockHolder lockController;
    {
        DebuggerController *p = g_controllers;

        m_singleStep = false;

        while (p != NULL)
        {
            if (p->m_thread == m_thread
                && p->m_singleStep)
                break;

            p = p->m_next;
        }

        if (p == NULL)
        {
            UnapplyTraceFlag(m_thread);
        }
    }
}


//
// ApplyTraceFlag sets the trace flag (i.e., turns on single-stepping)
// for a thread.
//
void DebuggerController::ApplyTraceFlag(Thread *thread)
{
    LOG((LF_CORDB,LL_INFO1000, "DC::ApplyTraceFlag thread:0x%p [0x%0x]\n", thread, Debugger::GetThreadIdHelper(thread)));

    CONTEXT *context;
    if(thread->GetInteropDebuggingHijacked())
    {
        context = GetManagedLiveCtx(thread);
    }
    else
    {
        context = GetManagedStoppedCtx(thread);
    }
    CONSISTENCY_CHECK_MSGF(context != NULL, ("Can't apply ss flag to thread 0x%p b/c it's not in a safe place.\n", thread));
    PREFIX_ASSUME(context != NULL);


    g_pEEInterface->MarkThreadForDebugStepping(thread, true);
    LOG((LF_CORDB,LL_INFO1000, "DC::ApplyTraceFlag marked thread for debug stepping\n"));

    SetSSFlag(reinterpret_cast<DT_CONTEXT *>(context) ARM_ARG(thread) ARM64_ARG(thread));
}

//
// UnapplyTraceFlag sets the trace flag for a thread.
// Removes the hardware trace flag on this thread.
//

void DebuggerController::UnapplyTraceFlag(Thread *thread)
{
    LOG((LF_CORDB,LL_INFO1000, "DC::UnapplyTraceFlag thread:0x%p\n", thread));


    // Either this is the helper thread, or we're manipulating our own context.
    _ASSERTE(
        ThisIsHelperThreadWorker() ||
        (thread == ::GetThreadNULLOk())
    );

    CONTEXT *context = GetManagedStoppedCtx(thread);

    // If there's no context available, then the thread shouldn't have the single-step flag
    // enabled and there's nothing for us to do.
    if (context == NULL)
    {
        // In theory, I wouldn't expect us to ever get here.
        // Even if we are here, our single-step flag should already be deactivated,
        // so there should be nothing to do. However, we still assert b/c we want to know how
        // we'd actually hit this.
        // @todo - is there a path if TriggerUnwind() calls DisableAll(). But why would
        CONSISTENCY_CHECK_MSGF(false, ("How did we get here?. thread=%p\n", thread));
        LOG((LF_CORDB,LL_INFO1000, "DC::UnapplyTraceFlag couldn't get context.\n"));
        return;
    }

    // Always need to unmark for stepping
    g_pEEInterface->MarkThreadForDebugStepping(thread, false);
    UnsetSSFlag(reinterpret_cast<DT_CONTEXT *>(context) ARM_ARG(thread) ARM64_ARG(thread));
}

void DebuggerController::EnableExceptionHook()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(m_thread != NULL);

    ControllerLockHolder lockController;

    m_exceptionHook = true;
}

void DebuggerController::DisableExceptionHook()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(m_thread != NULL);

    ControllerLockHolder lockController;
    m_exceptionHook = false;
}


// void DebuggerController::DispatchExceptionHook()  Called before
// the switch statement in DispatchNativeException (therefore
// when any exception occurs), this allows patches to do something before the
// regular DispatchX methods.
// How: Iterate through list of controllers.  If m_exceptionHook
// is set & m_thread is either thread or NULL, then invoke TriggerExceptionHook()
BOOL DebuggerController::DispatchExceptionHook(Thread *thread,
                                               CONTEXT *context,
                                               EXCEPTION_RECORD *pException)
{
    // ExceptionHook has restrictive contract b/c it could come from anywhere.
    // This can only modify controller's internal state. Can't send managed debug events.
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        MODE_ANY;

        // Filter context not set yet b/c we can only set it in COOP, and this may be in preemptive.
        PRECONDITION(thread == ::GetThreadNULLOk());
        PRECONDITION((g_pEEInterface->GetThreadFilterContext(thread) == NULL));
        PRECONDITION(CheckPointer(pException));
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO1000, "DC:: DispatchExceptionHook\n"));

    if (!g_patchTableValid)
    {
        LOG((LF_CORDB, LL_INFO1000, "DC::DEH returning, no patch table.\n"));
        return (TRUE);
    }


    _ASSERTE(g_patches != NULL);

    ControllerLockHolder lockController;

    TP_RESULT tpr = TPR_IGNORE;
    DebuggerController *p;

    p = g_controllers;
    while (p != NULL)
    {
        DebuggerController *pNext = p->m_next;

        if (p->m_exceptionHook
            && (p->m_thread == NULL || p->m_thread == thread) &&
            tpr != TPR_IGNORE_AND_STOP)
        {
                        LOG((LF_CORDB, LL_INFO1000, "DC::DEH calling TEH...\n"));
            tpr = p->TriggerExceptionHook(thread, context , pException);
                        LOG((LF_CORDB, LL_INFO1000, "DC::DEH ... returned.\n"));

            if (tpr == TPR_IGNORE_AND_STOP)
            {
                LOG((LF_CORDB, LL_INFO1000, "DC:: DEH: leaving early!\n"));
                break;
            }
        }

        p = pNext;
    }

    LOG((LF_CORDB, LL_INFO1000, "DC:: DEH: returning 0x%x!\n", tpr));

    return (tpr != TPR_IGNORE_AND_STOP);
}

//
// EnableUnwind enables an unwind event to be called when the stack is unwound
// (via an exception) to or past the given pointer.
//

void DebuggerController::EnableUnwind(FramePointer fp)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    ASSERT(m_thread != NULL);
    LOG((LF_CORDB,LL_EVERYTHING,"DC:EU EnableUnwind at 0x%x\n", fp.GetSPValue()));

    ControllerLockHolder lockController;
    m_unwindFP = fp;
}

FramePointer DebuggerController::GetUnwind()
{
    LIMITED_METHOD_CONTRACT;

    return m_unwindFP;
}

//
// DisableUnwind disables the unwind event for the controller.
//

void DebuggerController::DisableUnwind()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    ASSERT(m_thread != NULL);

    LOG((LF_CORDB,LL_INFO1000, "DC::DU\n"));

    ControllerLockHolder lockController;

    m_unwindFP = LEAF_MOST_FRAME;
}

//
// DispatchUnwind is called when an unwind happens.
// the event to the appropriate controllers.
// - handlerFP is the frame pointer that the handler will be invoked at.
// - DJI is EnC-aware method that the handler is in.
// - newOffset is the
//
bool DebuggerController::DispatchUnwind(Thread *thread,
                                        MethodDesc *fd, DebuggerJitInfo * pDJI,
                                        SIZE_T newOffset,
                                        FramePointer handlerFP,
                                        CorDebugStepReason unwindReason)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER; // don't send IPC events
        MODE_COOPERATIVE; // TriggerUnwind always is coop

        PRECONDITION(!IsDbgHelperSpecialThread());
    }
    CONTRACTL_END;


    CONTRACT_VIOLATION(ThrowsViolation); // trigger unwind throws

    _ASSERTE(unwindReason == STEP_EXCEPTION_FILTER || unwindReason == STEP_EXCEPTION_HANDLER);

    bool used = false;

    LOG((LF_CORDB, LL_INFO10000, "DC: Dispatch Unwind\n"));

    ControllerLockHolder lockController;
    {
        DebuggerController *p;

        p = g_controllers;

        while (p != NULL)
        {
            DebuggerController *pNext = p->m_next;

            if (p->m_thread == thread && p->m_unwindFP != LEAF_MOST_FRAME)
            {
                LOG((LF_CORDB, LL_INFO10000, "Dispatch Unwind: Found candidate\n"));


                //  Assumptions here:
                //      Function with handlers are -ALWAYS- EBP-frame based (JIT assumption)
                //
                //      newFrame is the EBP for the handler
                //      p->m_unwindFP points to the stack slot with the return address of the function.
                //
                //  For the interesting case: stepover, we want to know if the handler is in the same function
                //  as the stepper, if its above it (caller) o under it (callee) in order to know if we want
                //  to patch the handler or not.
                //
                //  3 cases:
                //
                //      a) Handler is in a function under the function where the step happened. It therefore is
                //         a stepover. We don't want to patch this handler. The handler will have an EBP frame.
                //         So it will be at least be 2 DWORDs away from the m_unwindFP of the controller (
                //         1 DWORD from the pushed return address and 1 DWORD for the push EBP).
                //
                //      b) Handler is in the same function as the stepper. We want to patch the handler. In this
                //         case handlerFP will be the same as p->m_unwindFP-sizeof(void*). Why? p->m_unwindFP
                //         stores a pointer to the return address of the function. As a function with a handler
                //         is always EBP frame based it will have the following code in the prolog:
                //
                //                  push ebp        <- ( sub esp, 4 ; mov [esp], ebp )
                //                  mov  esp, ebp
                //
                //         Therefore EBP will be equal to &CallerReturnAddress-4.
                //
                //      c) Handler is above the function where the stepper is. We want to patch the handler. handlerFP
                //         will be always greater than the pointer to the return address of the function where the
                //         stepper is.
                //
                //
                //

                if (IsEqualOrCloserToRoot(handlerFP, p->m_unwindFP))
                {
                    used = true;

                    //
                    // Assume that this isn't going to block us at all --
                    // other threads may be waiting to patch or unpatch something,
                    // or to dispatch.
                    //
                    LOG((LF_CORDB, LL_INFO10000,
                        "Unwind trigger at offset 0x%p; handlerFP: 0x%p unwindReason: 0x%x.\n",
                         newOffset, handlerFP.GetSPValue(), unwindReason));

                    p->TriggerUnwind(thread,
                                     fd, pDJI,
                                     newOffset,
                                     handlerFP,
                                     unwindReason);
                }
                else
                {
                    LOG((LF_CORDB, LL_INFO10000,
                        "Unwind trigger at offset 0x%p; handlerFP: 0x%p unwindReason: 0x%x.\n",
                         newOffset, handlerFP.GetSPValue(), unwindReason));
                }
            }

            p = pNext;
        }
    }

    return used;
}

//
// EnableTraceCall enables a call event on the controller
// maxFrame is the leaf-most frame that we want notifications for.
// For step-in stuff, this will always be LEAF_MOST_FRAME.
// for step-out, this will be the current frame because we don't
// care if the current frame calls back into managed code when we're
// only interested in our parent frames.
//

void DebuggerController::EnableTraceCall(FramePointer maxFrame)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    ASSERT(m_thread != NULL);

    LOG((LF_CORDB,LL_INFO1000, "DC::ETC maxFrame=0x%x, thread=0x%x\n",
         maxFrame.GetSPValue(), Debugger::GetThreadIdHelper(m_thread)));

    // JMC stepper should never enabled this. (They should enable ME instead).
    _ASSERTE((DEBUGGER_CONTROLLER_JMC_STEPPER != this->GetDCType()) || !"JMC stepper shouldn't enable trace-call");


    ControllerLockHolder lockController;
    {
        if (!m_traceCall)
        {
            m_traceCall = true;
            g_pEEInterface->EnableTraceCall(m_thread);
        }

        if (IsCloserToLeaf(maxFrame, m_traceCallFP))
            m_traceCallFP = maxFrame;
    }
}

struct PatchTargetVisitorData
{
    DebuggerController* controller;
    FramePointer maxFrame;
};

VOID DebuggerController::PatchTargetVisitor(TADDR pVirtualTraceCallTarget, VOID* pUserData)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    DebuggerController* controller = ((PatchTargetVisitorData*) pUserData)->controller;
    FramePointer maxFrame = ((PatchTargetVisitorData*) pUserData)->maxFrame;

    EX_TRY
    {
        CONTRACT_VIOLATION(GCViolation);    // PatchTrace throws, which implies GC-triggers
        TraceDestination trace;
        trace.InitForUnmanagedStub(pVirtualTraceCallTarget);
        controller->PatchTrace(&trace, maxFrame, true);
    }
    EX_CATCH
    {
        // not much we can do here
    }
    EX_END_CATCH(SwallowAllExceptions)
}

//
// DisableTraceCall disables call events on the controller
//

void DebuggerController::DisableTraceCall()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    ASSERT(m_thread != NULL);

    ControllerLockHolder lockController;
    {
        if (m_traceCall)
        {
            LOG((LF_CORDB,LL_INFO1000, "DC::DTC thread=0x%x\n",
             Debugger::GetThreadIdHelper(m_thread)));

            g_pEEInterface->DisableTraceCall(m_thread);

            m_traceCall = false;
            m_traceCallFP = ROOT_MOST_FRAME;
        }
    }
}

// Get a FramePointer for the leafmost frame on this thread's stacktrace.
// It's tempting to create this off the head of the Frame chain, but that may
// include internal EE Frames (like GCRoot frames) which a FrameInfo-stackwalk may skip over.
// Thus using the Frame chain would err on the side of returning a FramePointer that
// closer to the leaf.
FramePointer GetCurrentFramePointerFromStackTraceForTraceCall(Thread * thread)
{
    _ASSERTE(thread != NULL);

    // Ensure this is really the same as CSI.
    ControllerStackInfo info;

    // It's possible this stackwalk may be done at an unsafe time.
    // this method may trigger a GC, for example, in
    // FramedMethodFrame::AskStubForUnmanagedCallSite
    // which will trash the incoming argument array
    // which is not gc-protected.

    // We could probably imagine a more specialized stackwalk that
    // avoids these calls and is thus GC_NOTRIGGER.
    CONTRACT_VIOLATION(GCViolation);

    // This is being run live, so there's no filter available.
    CONTEXT *context;
    context = g_pEEInterface->GetThreadFilterContext(thread);
    _ASSERTE(context == NULL);
    _ASSERTE(!ISREDIRECTEDTHREAD(thread));

    // This is actually safe because we're coming from a TraceCall, which
    // means we're not in the middle of a stub. We don't have some partially
    // constructed frame, so we can safely traverse the stack.
    // However, we may still have a problem w/ the GC-violation.
    StackTraceTicket ticket(StackTraceTicket::SPECIAL_CASE_TICKET);
    info.GetStackInfo(ticket, thread, LEAF_MOST_FRAME, NULL);

    FramePointer fp = info.m_activeFrame.fp;

    return fp;
}
//
// DispatchTraceCall is called when a call is traced in the EE
// It dispatches the event to the appropriate controllers.
//

bool DebuggerController::DispatchTraceCall(Thread *thread,
                                           const BYTE *ip)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        THROWS;
    }
    CONTRACTL_END;

    bool used = false;

    LOG((LF_CORDB, LL_INFO10000,
         "DC::DTC: TraceCall at 0x%x\n", ip));

    ControllerLockHolder lockController;
    {
        DebuggerController *p;

        p = g_controllers;
        while (p != NULL)
        {
            DebuggerController *pNext = p->m_next;

            if (p->m_thread == thread && p->m_traceCall)
            {
                bool trigger;

                if (p->m_traceCallFP == LEAF_MOST_FRAME)
                    trigger = true;
                else
                {
                    // We know we don't have a filter context, so get a frame pointer from our frame chain.
                    FramePointer fpToCheck = GetCurrentFramePointerFromStackTraceForTraceCall(thread);


                    // <REVISIT_TODO>
                    //
                    // Currently, we never ever put a patch in an IL stub, and as such, if the IL stub
                    // throws an exception after returning from unmanaged code, we would not trigger
                    // a trace call when we call the constructor of the exception.  The following is
                    // kind of a workaround to make that working.  If we ever make the change to stop in
                    // IL stubs (for example, if we start to share security IL stub), then this can be
                    // removed.
                    //
                    // </REVISIT_TODO>



                    // It's possible this stackwalk may be done at an unsafe time.
                    // this method may trigger a GC, for example, in
                    // FramedMethodFrame::AskStubForUnmanagedCallSite
                    // which will trash the incoming argument array
                    // which is not gc-protected.
                    ControllerStackInfo info;
                    {
                        CONTRACT_VIOLATION(GCViolation);
#ifdef _DEBUG
                        CONTEXT *context = g_pEEInterface->GetThreadFilterContext(thread);
#endif // _DEBUG
                        _ASSERTE(context == NULL);
                        _ASSERTE(!ISREDIRECTEDTHREAD(thread));

                        // See explanation in GetCurrentFramePointerFromStackTraceForTraceCall.
                        StackTraceTicket ticket(StackTraceTicket::SPECIAL_CASE_TICKET);
                        info.GetStackInfo(ticket, thread, LEAF_MOST_FRAME, NULL);
                    }

                    if (info.m_activeFrame.chainReason == CHAIN_ENTER_UNMANAGED)
                    {
                        _ASSERTE(info.HasReturnFrame());

                        // This check makes sure that we don't do this logic for inlined frames.
                        if (info.GetReturnFrame().md->IsILStub())
                        {
                            // Make sure that the frame pointer of the active frame is actually
                            // the address of an exit frame.
                            _ASSERTE( (static_cast<Frame*>(info.m_activeFrame.fp.GetSPValue()))->GetFrameType()
                                      == Frame::TYPE_EXIT );
                            _ASSERTE(!info.GetReturnFrame().HasChainMarker());
                            fpToCheck = info.GetReturnFrame().fp;
                        }
                    }

                    // @todo - This comparison seems somewhat nonsensical. We don't have a filter context
                    // in place, so what frame pointer is fpToCheck actually for?
                    trigger = IsEqualOrCloserToRoot(fpToCheck, p->m_traceCallFP);
                }

                if (trigger)
                {
                    used = true;

                    // This can only update controller's state, can't actually send IPC events.
                    p->TriggerTraceCall(thread, ip);
                }
            }

            p = pNext;
        }
    }

    return used;
}

bool DebuggerController::IsMethodEnterEnabled()
{
    LIMITED_METHOD_CONTRACT;
    return m_fEnableMethodEnter;
}


// Notify dispatching logic that this controller wants to get TriggerMethodEnter
// We keep a count of total controllers waiting for MethodEnter (in g_cTotalMethodEnter).
// That way we know if any controllers want MethodEnter callbacks. If none do,
// then we can set the JMC probe flag to false for all modules.
void DebuggerController::EnableMethodEnter()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    ControllerLockHolder chController;
    Debugger::DebuggerDataLockHolder chInfo(g_pDebugger);

    // Both JMC + Traditional steppers may use MethodEnter.
    // For JMC, it's a core part of functionality. For Traditional steppers, we use it as a backstop
    // in case the stub-managers fail.
    _ASSERTE(g_cTotalMethodEnter >= 0);
    if (!m_fEnableMethodEnter)
    {
        LOG((LF_CORDB, LL_INFO1000000, "DC::EnableME, this=%p, previously disabled\n", this));
        m_fEnableMethodEnter = true;

        g_cTotalMethodEnter++;
    }
    else
    {
        LOG((LF_CORDB, LL_INFO1000000, "DC::EnableME, this=%p, already set\n", this));
    }
    g_pDebugger->UpdateAllModuleJMCFlag(g_cTotalMethodEnter != 0); // Needs JitInfo lock
}

// Notify dispatching logic that this controller doesn't want to get
// TriggerMethodEnter
void DebuggerController::DisableMethodEnter()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    ControllerLockHolder chController;
    Debugger::DebuggerDataLockHolder chInfo(g_pDebugger);

    if (m_fEnableMethodEnter)
    {
        LOG((LF_CORDB, LL_INFO1000000, "DC::DisableME, this=%p, previously set\n", this));
        m_fEnableMethodEnter = false;

        g_cTotalMethodEnter--;
        _ASSERTE(g_cTotalMethodEnter >= 0);
    }
    else
    {
        LOG((LF_CORDB, LL_INFO1000000, "DC::DisableME, this=%p, already disabled\n", this));
    }

    g_pDebugger->UpdateAllModuleJMCFlag(g_cTotalMethodEnter != 0); // Needs JitInfo lock
}

// Loop through controllers and dispatch TriggerMethodEnter
void DebuggerController::DispatchMethodEnter(void * pIP, FramePointer fp)
{
    _ASSERTE(pIP != NULL);

    Thread * pThread = g_pEEInterface->GetThread();
    _ASSERTE(pThread  != NULL);

    // Lookup the DJI for this method & ip.
    // Since we create DJIs when we jit the code, and this code has been jitted
    // (that's where the probe's coming from!), we will have a DJI.
    DebuggerJitInfo * dji = g_pDebugger->GetJitInfoFromAddr((TADDR) pIP);

    // This includes the case where we have a LightWeight codegen method.
    if (dji == NULL)
    {
        return;
    }

    LOG((LF_CORDB, LL_INFO100000, "DC::DispatchMethodEnter for '%s::%s'\n",
        dji->m_nativeCodeVersion.GetMethodDesc()->m_pszDebugClassName,
        dji->m_nativeCodeVersion.GetMethodDesc()->m_pszDebugMethodName));

    ControllerLockHolder lockController;

    // For debug check, keep a count to make sure that g_cTotalMethodEnter
    // is actually the number of controllers w/ MethodEnter enabled.
    int count = 0;

    DebuggerController *p = g_controllers;
    while (p != NULL)
    {
        if (p->m_fEnableMethodEnter)
        {
            if ((p->GetThread() == NULL) || (p->GetThread() == pThread))
            {
                ++count;
                p->TriggerMethodEnter(pThread, dji, (const BYTE *) pIP, fp);
            }
        }
        p = p->m_next;
    }

    _ASSERTE(g_cTotalMethodEnter == count);

}

//
// AddProtection adds page protection to (at least) the given range of
// addresses
//

void DebuggerController::AddProtection(const BYTE *start, const BYTE *end,
                                       bool readable)
{
    // !!!
    _ASSERTE(!"Not implemented yet");
}

//
// RemoveProtection removes page protection from the given
// addresses. The parameters should match an earlier call to
// AddProtection
//

void DebuggerController::RemoveProtection(const BYTE *start, const BYTE *end,
                                          bool readable)
{
    // !!!
    _ASSERTE(!"Not implemented yet");
}


// Default implementations for FuncEvalEnter & Exit notifications.
void DebuggerController::TriggerFuncEvalEnter(Thread * thread)
{
    LOG((LF_CORDB, LL_INFO100000, "DC::TFEEnter, thead=%p, this=%p\n", thread, this));
}

void DebuggerController::TriggerFuncEvalExit(Thread * thread)
{
    LOG((LF_CORDB, LL_INFO100000, "DC::TFEExit, thead=%p, this=%p\n", thread, this));
}

// bool DebuggerController::TriggerPatch()   What: Tells the
// static DC whether this patch should be activated now.
// Returns true if it should be, false otherwise.
// How: Base class implementation returns false.  Others may
// return true.
TP_RESULT DebuggerController::TriggerPatch(DebuggerControllerPatch *patch,
                              Thread *thread,
                              TRIGGER_WHY tyWhy)
{
    LOG((LF_CORDB, LL_INFO10000, "DC::TP: in default TriggerPatch\n"));
    return TPR_IGNORE;
}

bool DebuggerController::TriggerSingleStep(Thread *thread,
                                           const BYTE *ip)
{
    LOG((LF_CORDB, LL_INFO10000, "DC::TP: in default TriggerSingleStep\n"));
    return false;
}

void DebuggerController::TriggerUnwind(Thread *thread,
                                       MethodDesc *fd, DebuggerJitInfo * pDJI, SIZE_T offset,
                                       FramePointer fp,
                                       CorDebugStepReason unwindReason)
{
    LOG((LF_CORDB, LL_INFO10000, "DC::TP: in default TriggerUnwind\n"));
}

void DebuggerController::TriggerTraceCall(Thread *thread,
                                          const BYTE *ip)
{
    LOG((LF_CORDB, LL_INFO10000, "DC::TP: in default TriggerTraceCall\n"));
}

TP_RESULT DebuggerController::TriggerExceptionHook(Thread *thread, CONTEXT * pContext,
                                              EXCEPTION_RECORD *exception)
{
    LOG((LF_CORDB, LL_INFO10000, "DC::TP: in default TriggerExceptionHook\n"));
    return TPR_IGNORE;
}

void DebuggerController::TriggerMethodEnter(Thread * thread,
                                            DebuggerJitInfo * dji,
                                            const BYTE * ip,
                                            FramePointer fp)
{
    LOG((LF_CORDB, LL_INFO10000, "DC::TME in default impl. dji=%p, addr=%p, fp=%p\n",
        dji, ip, fp.GetSPValue()));
}

bool DebuggerController::SendEvent(Thread *thread, bool fIpChanged)
{
    CONTRACTL
    {
        NOTHROW;
        SENDEVENT_CONTRACT_ITEMS;
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO10000, "DC::TP: in default SendEvent\n"));

    // If any derived class trigger SendEvent, it should also implement SendEvent.
    _ASSERTE(false || !"Base DebuggerController sending an event?");
    return false;
}


// Dispacth Func-Eval Enter & Exit notifications.
void DebuggerController::DispatchFuncEvalEnter(Thread * thread)
{
    LOG((LF_CORDB, LL_INFO100000, "DC::DispatchFuncEvalEnter for thread 0x%p\n", thread));

    ControllerLockHolder lockController;

    DebuggerController *p = g_controllers;
    while (p != NULL)
    {
        if ((p->GetThread() == NULL) || (p->GetThread() == thread))
        {
            p->TriggerFuncEvalEnter(thread);
        }

        p = p->m_next;
    }


}

void DebuggerController::DispatchFuncEvalExit(Thread * thread)
{
    LOG((LF_CORDB, LL_INFO100000, "DC::DispatchFuncEvalExit for thread 0x%p\n", thread));

    ControllerLockHolder lockController;

    DebuggerController *p = g_controllers;
    while (p != NULL)
    {
        if ((p->GetThread() == NULL) || (p->GetThread() == thread))
        {
            p->TriggerFuncEvalExit(thread);
        }

        p = p->m_next;
    }


}


#ifdef _DEBUG
// See comment in DispatchNativeException
void ThisFunctionMayHaveTriggerAGC()
{
    CONTRACTL
    {
        GC_TRIGGERS;
        NOTHROW;
    }
    CONTRACTL_END;
}
#endif

// bool DebuggerController::DispatchNativeException()  Figures out
// if any debugger controllers will handle the exception.
// DispatchNativeException should be called by the EE when a native exception
// occurs.  If it returns true, the exception was generated by a Controller and
// should be ignored.
// How: Calls DispatchExceptionHook to see if anything is
// interested in ExceptionHook, then does a switch on dwCode:
//         EXCEPTION_BREAKPOINT means invoke DispatchPatchOrSingleStep(ST_PATCH).
//         EXCEPTION_SINGLE_STEP means DispatchPatchOrSingleStep(ST_SINGLE_STEP).
//         EXCEPTION_ACCESS_VIOLATION means invoke DispatchAccessViolation.
// Returns true if the exception was actually meant for the debugger,
//        returns false otherwise.
bool DebuggerController::DispatchNativeException(EXCEPTION_RECORD *pException,
                                                 CONTEXT *pContext,
                                                 DWORD dwCode,
                                                 Thread *pCurThread)
{
    CONTRACTL
    {
        NOTHROW;

        // If this exception is for the debugger, then we may trigger a GC.
        // But we'll be called on _any_ exception, including ones in a GC-no-triggers region.
        // Our current contract system doesn't let us specify such conditions on GC_TRIGGERS.
        // So we disable it now, and if we find out the exception is meant for the debugger,
        // we'll call ThisFunctionMayHaveTriggerAGC() to ping that we're really a GC_TRIGGERS.
        DISABLED(GC_TRIGGERS); // Only GC triggers if we send an event,
        PRECONDITION(!IsDbgHelperSpecialThread());

        // If we're called from preemptive mode, than our caller has protected the stack.
        // If we're in cooperative mode, then we need to protect the stack before toggling GC modes
        // (by setting the filter-context)
        MODE_ANY;

        PRECONDITION(CheckPointer(pException));
        PRECONDITION(CheckPointer(pContext));
        PRECONDITION(CheckPointer(pCurThread));
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_EVERYTHING, "DispatchNativeException was called\n"));
    LOG((LF_CORDB, LL_INFO10000, "Native exception at 0x%p, code=0x%8x, context=0x%p, er=0x%p\n",
         pException->ExceptionAddress, dwCode, pContext, pException));


    bool fDebuggers;
    BOOL fDispatch;
    DPOSS_ACTION result = DPOSS_DONT_CARE;


    // We have a potentially ugly locking problem here. This notification is called on any exception,
    // but we have no idea what our locking context is at the time. Thus we may hold locks smaller
    // than the controller lock.
    // The debugger logic really only cares about exceptions directly in managed code (eg, hardware exceptions)
    // or in patch-skippers (since that's a copy of managed code running in a look-aside buffer).
    // That should exclude all C++ exceptions, which are the common case if Runtime code throws an internal ex.
    // So we ignore those to avoid the lock violation.
    if (pException->ExceptionCode == EXCEPTION_MSVC)
    {
        LOG((LF_CORDB, LL_INFO1000, "Debugger skipping for C++ exception.\n"));
        return FALSE;
    }

    // The debugger really only cares about exceptions in managed code.  Any exception that occurs
    // while the thread is redirected (such as EXCEPTION_HIJACK) is not of interest to the debugger.
    // Allowing this would be problematic because when an exception occurs while the thread is
    // redirected, we don't know which context (saved redirection context or filter context)
    // we should be operating on (see code:GetManagedStoppedCtx).
    if( ISREDIRECTEDTHREAD(pCurThread) )
    {
        LOG((LF_CORDB, LL_INFO1000, "Debugger ignoring exception 0x%x on redirected thread.\n", dwCode));

        // We shouldn't be seeing debugging exceptions on a redirected thread.  While a thread is
        // redirected we only call a few internal things (see code:Thread.RedirectedHandledJITCase),
        // and may call into the host.  We can't call normal managed code or anything we'd want to debug.
        _ASSERTE(dwCode != EXCEPTION_BREAKPOINT);
        _ASSERTE(dwCode != EXCEPTION_SINGLE_STEP);

        return FALSE;
    }

    // It's possible we're here without a debugger (since we have to call the
    // patch skippers). The Debugger may detach anytime,
    // so remember the attach state now.
#ifdef _DEBUG
    bool fWasAttached = false;
#ifdef DEBUGGING_SUPPORTED
    fWasAttached = (CORDebuggerAttached() != 0);
#endif //DEBUGGING_SUPPORTED
#endif //_DEBUG

    {
        // If we're in cooperative mode, it's unsafe to do a GC until we've put a filter context in place.
        GCX_NOTRIGGER();

        // If we know the debugger doesn't care about this exception, bail now.
        // Usually this is just if there's a debugger attached.
        // However, if a debugger detached but left outstanding controllers (like patch-skippers),
        // we still may care.
        // The only way a controller would get created outside of the helper thread is from
        // a patch skipper, so we always handle breakpoints.
        if (!CORDebuggerAttached() && (g_controllers == NULL) && (dwCode != EXCEPTION_BREAKPOINT))
        {
            return false;
        }

        FireEtwDebugExceptionProcessingStart();

        // We should never be here if the debugger was never involved.
        CONTEXT * pOldContext;
        pOldContext = pCurThread->GetFilterContext();

        // In most cases it is an error to nest, however in the patch-skipping logic we must
        // copy an unknown amount of code into another buffer and it occasionally triggers
        // an AV. This heuristic should filter that case out. See DDB 198093.
        // Ensure we perform this exception nesting filtering even before the call to
        // DebuggerController::DispatchExceptionHook, otherwise the nesting will continue when
        // a contract check is triggered in DispatchExceptionHook and another BP exception is
        // raised. See Dev11 66058.
        if ((pOldContext != NULL) && pCurThread->AVInRuntimeImplOkay() &&
            pException->ExceptionCode == STATUS_ACCESS_VIOLATION)
        {
            STRESS_LOG1(LF_CORDB, LL_INFO100, "DC::DNE Nested Access Violation at 0x%p is being ignored\n",
                pException->ExceptionAddress);
            return false;
        }
        // Otherwise it is an error to nest at all
        _ASSERTE(pOldContext == NULL);

        fDispatch = DebuggerController::DispatchExceptionHook(pCurThread,
                                                                   pContext,
                                                                   pException);

        {
            // Must be in cooperative mode to set the filter context. We know there are times we'll be in preemptive mode,
            // (such as M2U handoff, or potentially patches in the middle of a stub, or various random exceptions)

            // @todo - We need to worry about GC-protecting our stack. If we're in preemptive mode, the caller did it for us.
            // If we're in cooperative, then we need to set the FilterContext *before* we toggle GC mode (since
            // the FC protects the stack).
            // If we're in preemptive, then we need to set the FilterContext *after* we toggle ourselves to Cooperative.
            // Also note it may not be possible to toggle GC mode at these times (such as in the middle of the stub).
            //
            // Part of the problem is that the Filter Context is serving 2 purposes here:
            // - GC protect the stack. (essential if we're in coop mode).
            // - provide info to controllers (such as current IP, and a place to set the Single-Step flag).
            //
            // This contract violation is mitigated in that we must have had the debugger involved to get to this point.
            CONTRACT_VIOLATION(ModeViolation);
            g_pEEInterface->SetThreadFilterContext(pCurThread, pContext);
        }
        // Now that we've set the filter context, we can let the GCX_NOTRIGGER expire.
        // It's still possible that we may be called from a No-trigger region.
    }


    if (fDispatch)
    {
        // Disable SingleStep for all controllers on this thread. This requires the filter context set.
        // This is what would disable the ss-flag when single-stepping over an AV.
        if (g_patchTableValid && (dwCode != EXCEPTION_SINGLE_STEP))
        {
            LOG((LF_CORDB, LL_INFO1000, "DC::DNE non-single-step exception; check if any controller has ss turned on\n"));

            ControllerLockHolder lockController;
            for (DebuggerController* p = g_controllers; p != NULL; p = p->m_next)
            {
                if (p->m_singleStep && (p->m_thread == pCurThread))
                {
                    LOG((LF_CORDB, LL_INFO1000, "DC::DNE turn off ss for controller 0x%p\n", p));
                    p->DisableSingleStep();
                }
            }
            // implicit controller lock release
        }

        CORDB_ADDRESS_TYPE * ip = dac_cast<PTR_CORDB_ADDRESS_TYPE>(GetIP(pContext));

        switch (dwCode)
        {
        case EXCEPTION_BREAKPOINT:
            // EIP should be properly set up at this point.
            result = DebuggerController::DispatchPatchOrSingleStep(pCurThread,
                                                       pContext,
                                                       ip,
                                                       ST_PATCH);
            LOG((LF_CORDB, LL_EVERYTHING, "DC::DNE DispatchPatch call returned\n"));

            // If we detached, we should remove all our breakpoints. So if we try
            // to handle this breakpoint, make sure that we're attached.
            if (IsInUsedAction(result) == true)
            {
                _ASSERTE(fWasAttached);
            }
            break;

        case EXCEPTION_SINGLE_STEP:
            LOG((LF_CORDB, LL_EVERYTHING, "DC::DNE SINGLE_STEP Exception\n"));

            result = DebuggerController::DispatchPatchOrSingleStep(pCurThread,
                                                            pContext,
                                                            ip,
                                        (SCAN_TRIGGER)(ST_PATCH|ST_SINGLE_STEP));
                // We pass patch | single step since single steps actually
                // do both (eg, you SS onto a breakpoint).
            break;

        default:
            break;
        } // end switch

    }
#ifdef _DEBUG
    else
    {
        LOG((LF_CORDB, LL_INFO1000, "DC:: DNE step-around fDispatch:0x%x!\n", fDispatch));
    }
#endif //_DEBUG

    fDebuggers = (fDispatch?(IsInUsedAction(result)?true:false):true);

    LOG((LF_CORDB, LL_INFO10000, "DC::DNE, returning 0x%x.\n", fDebuggers));

#ifdef _DEBUG
    if (fDebuggers && (result == DPOSS_USED_WITH_EVENT))
    {
        // If the exception belongs to the debugger, then we may have sent an event,
        // and thus we may have triggered a GC.
        ThisFunctionMayHaveTriggerAGC();
    }
#endif



    // Must restore the filter context. After the filter context is gone, we're
    // unprotected again and unsafe for a GC.
    {
        CONTRACT_VIOLATION(ModeViolation);
        g_pEEInterface->SetThreadFilterContext(pCurThread, NULL);
    }

#ifdef FEATURE_EMULATE_SINGLESTEP
    if (pCurThread->IsSingleStepEnabled())
        pCurThread->ApplySingleStep(pContext);
#endif

    FireEtwDebugExceptionProcessingEnd();

    return fDebuggers;
}

// * -------------------------------------------------------------------------
// * DebuggerPatchSkip routines
// * -------------------------------------------------------------------------

DebuggerPatchSkip::DebuggerPatchSkip(Thread *thread,
                                     DebuggerControllerPatch *patch,
                                     AppDomain *pAppDomain)
  : DebuggerController(thread, pAppDomain),
    m_address(patch->address)
{
    LOG((LF_CORDB, LL_INFO10000,
         "DPS::DPS: Patch skip 0x%p\n", patch->address));

    // On ARM the single-step emulation already utilizes a per-thread execution buffer similar to the scheme
    // below. As a result we can skip most of the instruction parsing logic that's instead internalized into
    // the single-step emulation itself.
#ifndef FEATURE_EMULATE_SINGLESTEP

    // NOTE: in order to correctly single-step RIP-relative writes on multiple threads we need to set up
    // a shared buffer with the instruction and a buffer for the RIP-relative value so that all threads
    // are working on the same copy.  as the single-steps complete the modified data in the buffer is
    // copied back to the real address to ensure proper execution of the program.

    //
    // Create the shared instruction block. this will also create the shared RIP-relative buffer
    //

    m_pSharedPatchBypassBuffer = patch->GetOrCreateSharedPatchBypassBuffer();
#if defined(HOST_OSX) && defined(HOST_ARM64)
    ExecutableWriterHolder<SharedPatchBypassBuffer> sharedPatchBypassBufferWriterHolder((SharedPatchBypassBuffer*)m_pSharedPatchBypassBuffer, sizeof(SharedPatchBypassBuffer));
    SharedPatchBypassBuffer *pSharedPatchBypassBufferRW = sharedPatchBypassBufferWriterHolder.GetRW();
#else // HOST_OSX && HOST_ARM64
    SharedPatchBypassBuffer *pSharedPatchBypassBufferRW = m_pSharedPatchBypassBuffer;
#endif // HOST_OSX && HOST_ARM64

    BYTE* patchBypassRX = m_pSharedPatchBypassBuffer->PatchBypass;
    BYTE* patchBypassRW = pSharedPatchBypassBufferRW->PatchBypass;
    LOG((LF_CORDB, LL_INFO10000, "DPS::DPS: Patch skip for opcode 0x%.4x at address %p buffer allocated at 0x%.8x\n", patch->opcode, patch->address, m_pSharedPatchBypassBuffer));

    // Copy the instruction block over to the patch skip
    // WARNING: there used to be an issue here because CopyInstructionBlock copied the breakpoint from the
    // jitted code stream into the patch buffer. Further below CORDbgSetInstruction would correct the
    // first instruction. This buffer is shared by all threads so if another thread executed the buffer
    // between this thread's execution of CopyInstructionBlock and CORDbgSetInstruction the wrong
    // code would be executed. The bug has been fixed by changing CopyInstructionBlock to only copy
    // the code bytes after the breakpoint.
    //   You might be tempted to stop copying the code at all, however that wouldn't work well with rejit.
    // If we skip a breakpoint that is sitting at the beginning of a method, then the profiler rejits that
    // method causing a jump-stamp to be placed, then we skip the breakpoint again, we need to make sure
    // the 2nd skip executes the new jump-stamp code and not the original method prologue code. Copying
    // the code every time ensures that we have the most up-to-date version of the code in the buffer.
    _ASSERTE( patch->IsBound() );
    CopyInstructionBlock(patchBypassRW, (const BYTE *)patch->address);

    // Technically, we could create a patch skipper for an inactive patch, but we rely on the opcode being
    // set here.
    _ASSERTE( patch->IsActivated() );
    CORDbgSetInstruction((CORDB_ADDRESS_TYPE *)patchBypassRW, patch->opcode);

    LOG((LF_CORDB, LL_EVERYTHING, "SetInstruction was called\n"));
    //
    // Look at instruction to get some attributes
    //

    NativeWalker::DecodeInstructionForPatchSkip(patchBypassRX, &(m_instrAttrib));

#if defined(TARGET_AMD64)


    // The code below handles RIP-relative addressing on AMD64.  the original implementation made the assumption that
    // we are only using RIP-relative addressing to access read-only data (see VSW 246145 for more information).  this
    // has since been expanded to handle RIP-relative writes as well.
    if (m_instrAttrib.m_dwOffsetToDisp != 0)
    {
        _ASSERTE(m_instrAttrib.m_cbInstr != 0);

        //
        // Populate the RIP-relative buffer with the current value if needed
        //

        BYTE* bufferBypassRW = pSharedPatchBypassBufferRW->BypassBuffer;

        // Overwrite the *signed* displacement.
        int dwOldDisp = *(int*)(&patchBypassRX[m_instrAttrib.m_dwOffsetToDisp]);
        int dwNewDisp = offsetof(SharedPatchBypassBuffer, BypassBuffer) -
                          (offsetof(SharedPatchBypassBuffer, PatchBypass) + m_instrAttrib.m_cbInstr);
        *(int*)(&patchBypassRW[m_instrAttrib.m_dwOffsetToDisp]) = dwNewDisp;

        // This could be an LEA, which we'll just have to change into a MOV
        // and copy the original address
        if (((patchBypassRX[0] == 0x4C) || (patchBypassRX[0] == 0x48)) && (patchBypassRX[1] == 0x8d))
        {
            patchBypassRW[1] = 0x8b; // MOV reg, mem
            _ASSERTE((int)sizeof(void*) <= SharedPatchBypassBuffer::cbBufferBypass);
            *(void**)bufferBypassRW = (void*)(patch->address + m_instrAttrib.m_cbInstr + dwOldDisp);
        }
        else
        {
            _ASSERTE(m_instrAttrib.m_cOperandSize <= SharedPatchBypassBuffer::cbBufferBypass);
            // Copy the data into our buffer.
            memcpy(bufferBypassRW, patch->address + m_instrAttrib.m_cbInstr + dwOldDisp, m_instrAttrib.m_cOperandSize);

            if (m_instrAttrib.m_fIsWrite)
            {
                // save the actual destination address and size so when we TriggerSingleStep() we can update the value
                pSharedPatchBypassBufferRW->RipTargetFixup = (UINT_PTR)(patch->address + m_instrAttrib.m_cbInstr + dwOldDisp);
                pSharedPatchBypassBufferRW->RipTargetFixupSize = m_instrAttrib.m_cOperandSize;
            }
        }
    }
#endif // TARGET_AMD64

#endif // !FEATURE_EMULATE_SINGLESTEP

    // Signals our thread that the debugger will be manipulating the context
    // during the patch skip operation. This effectively prevents other threads
    // from suspending us until we have completed skiping the patch and restored
    // a good context (See DDB 188816)
    thread->BeginDebuggerPatchSkip(this);

    //
    // Set IP of context to point to patch bypass buffer
    //

    T_CONTEXT *context = g_pEEInterface->GetThreadFilterContext(thread);
    _ASSERTE(!ISREDIRECTEDTHREAD(thread));
    CONTEXT c;
    if (context == NULL)
    {
        // We can't play with our own context!
#if _DEBUG
        if (g_pEEInterface->GetThread())
        {
            // current thread is mamaged thread
            _ASSERTE(Debugger::GetThreadIdHelper(thread) != Debugger::GetThreadIdHelper(g_pEEInterface->GetThread()));
        }
#endif // _DEBUG

        c.ContextFlags = CONTEXT_CONTROL;

        thread->GetThreadContext(&c);
        context =(T_CONTEXT *) &c;

        ARM_ONLY(_ASSERTE(!"We should always have a filter context in DebuggerPatchSkip."));
    }

#ifdef FEATURE_EMULATE_SINGLESTEP
    // Since we emulate all single-stepping on ARM/ARM64 using an instruction buffer and a breakpoint all we have to
    // do here is initiate a normal single-step except that we pass the instruction to be stepped explicitly
    // (calling EnableSingleStep() would infer this by looking at the PC in the context, which would pick up
    // the patch we're trying to skip).
    //
    // Ideally we'd refactor the EnableSingleStep to support this alternative calling sequence but since this
    // involves three levels of methods and is only applicable to ARM we've chosen to replicate the relevant
    // implementation here instead.
    {
        ControllerLockHolder lockController;
        g_pEEInterface->MarkThreadForDebugStepping(thread, true);

#ifdef TARGET_ARM
        WORD opcode2 = 0;

        if (Is32BitInstruction(patch->opcode))
        {
            opcode2 = CORDbgGetInstruction((CORDB_ADDRESS_TYPE *)(((DWORD)patch->address) + 2));
        }
#endif // TARGET_ARM

        thread->BypassWithSingleStep(patch->address, patch->opcode ARM_ARG(opcode2));
        m_singleStep = true;
    }

#else // FEATURE_EMULATE_SINGLESTEP

#ifdef TARGET_ARM64
    patchBypassRX = NativeWalker::SetupOrSimulateInstructionForPatchSkip(context, m_pSharedPatchBypassBuffer, (const BYTE *)patch->address, patch->opcode);
#endif //TARGET_ARM64

    //set eip to point to buffer...
    SetIP(context, (PCODE)patchBypassRX);

    if (context ==(T_CONTEXT*) &c)
        thread->SetThreadContext(&c);


    LOG((LF_CORDB, LL_INFO10000, "DPS::DPS Bypass at 0x%p for opcode %p \n", patchBypassRX, patch->opcode));

    //
    // Turn on single step (if the platform supports it) so we can
    // fix up state after the instruction is executed.
    // Also turn on exception hook so we can adjust IP in exceptions
    //

    EnableSingleStep();

#endif // FEATURE_EMULATE_SINGLESTEP

    EnableExceptionHook();
}

DebuggerPatchSkip::~DebuggerPatchSkip()
{
#ifndef FEATURE_EMULATE_SINGLESTEP
    _ASSERTE(m_pSharedPatchBypassBuffer);
    m_pSharedPatchBypassBuffer->Release();
#endif
}

void DebuggerPatchSkip::DebuggerDetachClean()
{
// Since for ARM/ARM64 SharedPatchBypassBuffer isn't existed, we don't have to anything here.
#ifndef FEATURE_EMULATE_SINGLESTEP
   // Fix for Bug 1176448
   // When a debugger is detaching from the debuggee, we need to move the IP if it is pointing
   // somewhere in PatchBypassBuffer.All managed threads are suspended during detach, so changing
   // the context without notifications is safe.
   // Notice:
   // THIS FIX IS INCOMPLETE!It attempts to update the IP in the cases we can easily detect.However,
   // if a thread is in pre - emptive mode, and its filter context has been propagated to a VEH
   // context, then the filter context we get will be NULL and this fix will not work.Our belief is
   // that this scenario is rare enough that it doesnt justify the cost and risk associated with a
   // complete fix, in which we would have to either :
   // 1. Change the reference counting for DebuggerController and then change the exception handling
   // logic in the debuggee so that we can handle the debugger event after detach.
   // 2. Create a "stack walking" implementation for native code and use it to get the current IP and
   // set the IP to the right place.

    Thread *thread = GetThreadNULLOk();
    if (thread != NULL)
    {
        BYTE *patchBypass = m_pSharedPatchBypassBuffer->PatchBypass;
        CONTEXT *context = thread->GetFilterContext();
        if (patchBypass != NULL &&
            context != NULL &&
            (size_t)GetIP(context) >= (size_t)patchBypass &&
            (size_t)GetIP(context) <= (size_t)(patchBypass + MAX_INSTRUCTION_LENGTH + 1))
        {
            SetIP(context, (PCODE)((BYTE *)GetIP(context) - (patchBypass - (BYTE *)m_address)));
        }
    }
#endif
}


//
// We have to have a whole seperate function for this because you
// can't use __try in a function that requires object unwinding...
//

LONG FilterAccessViolation2(LPEXCEPTION_POINTERS ep, PVOID pv)
{
    LIMITED_METHOD_CONTRACT;

    return (ep->ExceptionRecord->ExceptionCode == EXCEPTION_ACCESS_VIOLATION)
        ? EXCEPTION_EXECUTE_HANDLER : EXCEPTION_CONTINUE_SEARCH;
}

// This helper is required because the AVInRuntimeImplOkayHolder can not
// be directly placed inside the scope of a PAL_TRY
void _CopyInstructionBlockHelper(BYTE* to, const BYTE* from)
{
    AVInRuntimeImplOkayHolder AVOkay;

    // This function only copies the portion of the instruction that follows the
    // breakpoint opcode, not the breakpoint itself
    to += CORDbg_BREAK_INSTRUCTION_SIZE;
    from += CORDbg_BREAK_INSTRUCTION_SIZE;

    // If an AV occurs because we walked off a valid page then we need
    // to be certain that all bytes on the previous page were copied.
    // We are certain that we copied enough bytes to contain the instruction
    // because it must have fit within the valid page.
    for (int i = 0; i < MAX_INSTRUCTION_LENGTH - CORDbg_BREAK_INSTRUCTION_SIZE; i++)
    {
        *to++ = *from++;
    }

}

// WARNING: this function skips copying the first CORDbg_BREAK_INSTRUCTION_SIZE bytes by design
// See the comment at the callsite in DebuggerPatchSkip::DebuggerPatchSkip for more details on
// this
void DebuggerPatchSkip::CopyInstructionBlock(BYTE *to, const BYTE* from)
{
    // We wrap the memcpy in an exception handler to handle the
    // extremely rare case where we're copying an instruction off the
    // end of a method that is also at the end of a page, and the next
    // page is unmapped.
    struct Param
    {
        BYTE *to;
        const BYTE* from;
    } param;
    param.to = to;
    param.from = from;
    PAL_TRY(Param *, pParam, &param)
    {
        _CopyInstructionBlockHelper(pParam->to, pParam->from);
    }
    PAL_EXCEPT_FILTER(FilterAccessViolation2)
    {
        // The whole point is that if we copy up the the AV, then
        // that's enough to execute, otherwise we would not have been
        // able to execute the code anyway. So we just ignore the
        // exception.
        LOG((LF_CORDB, LL_INFO10000,
             "DPS::DPS: AV copying instruction block ignored.\n"));
    }
    PAL_ENDTRY

    // We just created a new buffer of code, but the CPU caches code and may
    // not be aware of our changes. This should force the CPU to dump any cached
    // instructions it has in this region and load the new ones from memory
    FlushInstructionCache(GetCurrentProcess(), to + CORDbg_BREAK_INSTRUCTION_SIZE,
                          MAX_INSTRUCTION_LENGTH - CORDbg_BREAK_INSTRUCTION_SIZE);
}

TP_RESULT DebuggerPatchSkip::TriggerPatch(DebuggerControllerPatch *patch,
                              Thread *thread,
                              TRIGGER_WHY tyWhy)
{
    ARM_ONLY(_ASSERTE(!"Should not have called DebuggerPatchSkip::TriggerPatch."));
    LOG((LF_CORDB, LL_EVERYTHING, "DPS::TP called\n"));

#if defined(_DEBUG) && !defined(FEATURE_EMULATE_SINGLESTEP)
    CONTEXT *context = GetManagedLiveCtx(thread);

    LOG((LF_CORDB, LL_INFO1000, "DPS::TP: We've patched 0x%x (byPass:0x%x) "
        "for a skip after an EnC update!\n", GetIP(context),
        GetBypassAddress()));
    _ASSERTE(g_patches != NULL);

    // We shouldn't have mucked with EIP, yet.
    _ASSERTE(dac_cast<PTR_CORDB_ADDRESS_TYPE>(GetIP(context)) == GetBypassAddress());

    //We should be the _only_ patch here
    MethodDesc *md2 = dac_cast<PTR_MethodDesc>(GetIP(context));
    DebuggerControllerPatch *patchCheck = g_patches->GetPatch(g_pEEInterface->MethodDescGetModule(md2),md2->GetMemberDef());
    _ASSERTE(patchCheck == patch);
    _ASSERTE(patchCheck->controller == patch->controller);

    patchCheck = g_patches->GetNextPatch(patchCheck);
    _ASSERTE(patchCheck == NULL);
#endif // defined(_DEBUG) && !defined(FEATURE_EMULATE_SINGLESTEP)

    DisableAll();
    EnableExceptionHook();
    EnableSingleStep(); //gets us back to where we want.
    return TPR_IGNORE; // don't actually want to stop here....
}

TP_RESULT DebuggerPatchSkip::TriggerExceptionHook(Thread *thread, CONTEXT * context,
                                                  EXCEPTION_RECORD *exception)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        // Patch skippers only operate on patches set in managed code. But the infrastructure may have
        // toggled the GC mode underneath us.
        MODE_ANY;

        PRECONDITION(GetThreadNULLOk() == thread);
        PRECONDITION(thread != NULL);
        PRECONDITION(CheckPointer(context));
    }
    CONTRACTL_END;

    if (m_pAppDomain != NULL)
    {
        AppDomain *pAppDomainCur = thread->GetDomain();

        if (pAppDomainCur != m_pAppDomain)
        {
            LOG((LF_CORDB,LL_INFO10000, "DPS::TEH: Appdomain mismatch - not skiiping!\n"));
            return TPR_IGNORE;
        }
    }

    LOG((LF_CORDB,LL_INFO10000, "DPS::TEH: doing the patch-skip thing\n"));

#if defined(TARGET_ARM64) && !defined(FEATURE_EMULATE_SINGLESTEP)

    if (!IsSingleStep(exception->ExceptionCode))
    {
        LOG((LF_CORDB, LL_INFO10000, "Exception in patched Bypass instruction .\n"));
        return (TPR_IGNORE_AND_STOP);
    }

    _ASSERTE(m_pSharedPatchBypassBuffer);
    BYTE* patchBypass = m_pSharedPatchBypassBuffer->PatchBypass;
    PCODE targetIp;
    if (m_pSharedPatchBypassBuffer->RipTargetFixup)
    {
        targetIp = m_pSharedPatchBypassBuffer->RipTargetFixup;
    }
    else
    {
        targetIp = (PCODE)((BYTE *)GetIP(context) - (patchBypass - (BYTE *)m_address));
    }

    SetIP(context, targetIp);
    LOG((LF_CORDB, LL_ALWAYS, "Redirecting after Patch to 0x%p\n", GetIP(context)));

#elif defined(FEATURE_EMULATE_SINGLESTEP)

//Do nothing
#else
    _ASSERTE(m_pSharedPatchBypassBuffer);
    BYTE* patchBypass = m_pSharedPatchBypassBuffer->PatchBypass;

    if (m_instrAttrib.m_fIsCall && IsSingleStep(exception->ExceptionCode))
    {
        // Fixup return address on stack
#if defined(TARGET_X86) || defined(TARGET_AMD64)
        SIZE_T *sp = (SIZE_T *) GetSP(context);

        LOG((LF_CORDB, LL_INFO10000,
             "Bypass call return address redirected from 0x%p\n", *sp));

        *sp -= patchBypass - (BYTE*)m_address;

        LOG((LF_CORDB, LL_INFO10000, "to 0x%p\n", *sp));
#else
        PORTABILITY_ASSERT("DebuggerPatchSkip::TriggerExceptionHook -- return address fixup NYI");
#endif
    }

    if (!m_instrAttrib.m_fIsAbsBranch || !IsSingleStep(exception->ExceptionCode))
    {
        // Fixup IP

        LOG((LF_CORDB, LL_INFO10000, "Bypass instruction redirected from 0x%p\n", GetIP(context)));

        if (IsSingleStep(exception->ExceptionCode))
        {
#ifndef TARGET_UNIX
            // Check if the current IP is anywhere near the exception dispatcher logic.
            // If it is, ignore the exception, as the real exception is coming next.
            static FARPROC pExcepDispProc = NULL;

            if (!pExcepDispProc)
            {
                HMODULE hNtDll = WszGetModuleHandle(W("ntdll.dll"));

                if (hNtDll != NULL)
                {
                    pExcepDispProc = GetProcAddress(hNtDll, "KiUserExceptionDispatcher");

                    if (!pExcepDispProc)
                        pExcepDispProc = (FARPROC)(size_t)(-1);
                }
                else
                    pExcepDispProc = (FARPROC)(size_t)(-1);
            }

            _ASSERTE(pExcepDispProc != NULL);

            if ((size_t)pExcepDispProc != (size_t)(-1))
            {
                LPVOID pExcepDispEntryPoint = pExcepDispProc;

                if ((size_t)GetIP(context) > (size_t)pExcepDispEntryPoint &&
                    (size_t)GetIP(context) <= ((size_t)pExcepDispEntryPoint + MAX_INSTRUCTION_LENGTH * 2 + 1))
                {
                    LOG((LF_CORDB, LL_INFO10000,
                         "Bypass instruction not redirected. Landed in exception dispatcher.\n"));

                    return (TPR_IGNORE_AND_STOP);
                }
            }
#endif // TARGET_UNIX

            // If the IP is close to the skip patch start, or if we were skipping over a call, then assume the IP needs
            // adjusting.
            if (m_instrAttrib.m_fIsCall ||
                ((size_t)GetIP(context) >  (size_t)patchBypass &&
                 (size_t)GetIP(context) <= (size_t)(patchBypass + MAX_INSTRUCTION_LENGTH + 1)))
            {
                LOG((LF_CORDB, LL_INFO10000, "Bypass instruction redirected because still in skip area.\n"));
                LOG((LF_CORDB, LL_INFO10000, "m_fIsCall = %d, patchBypass = 0x%x, m_address = 0x%x\n",
                    m_instrAttrib.m_fIsCall, patchBypass, m_address));
                SetIP(context, (PCODE)((BYTE *)GetIP(context) - (patchBypass - (BYTE *)m_address)));
            }
            else
            {
                // Otherwise, need to see if the IP is something we recognize (either managed code
                // or stub code) - if not, we ignore the exception
                PCODE newIP = GetIP(context);
                newIP -= PCODE(patchBypass - (BYTE *)m_address);
                TraceDestination trace;

                if (g_pEEInterface->IsManagedNativeCode(dac_cast<PTR_CBYTE>(newIP)) ||
                    (g_pEEInterface->TraceStub(LPBYTE(newIP), &trace)))
                {
                    LOG((LF_CORDB, LL_INFO10000, "Bypass instruction redirected because we landed in managed or stub code\n"));
                    SetIP(context, newIP);
                }

                // If we have no idea where things have gone, then we assume that the IP needs no adjusting (which
                // could happen if the instruction we were trying to patch skip caused an AV).  In this case we want
                // to claim it as ours but ignore it and continue execution.
                else
                {
                    LOG((LF_CORDB, LL_INFO10000, "Bypass instruction not redirected because we're not in managed or stub code.\n"));
                    return (TPR_IGNORE_AND_STOP);
                }
            }
        }
        else
        {
            LOG((LF_CORDB, LL_INFO10000, "Bypass instruction redirected because it wasn't a single step exception.\n"));
            SetIP(context, (PCODE)((BYTE *)GetIP(context) - (patchBypass - (BYTE *)m_address)));
        }

        LOG((LF_CORDB, LL_ALWAYS, "to 0x%x\n", GetIP(context)));

    }

#endif


    // Signals our thread that the debugger is done manipulating the context
    // during the patch skip operation. This effectively prevented other threads
    // from suspending us until we completed skiping the patch and restored
    // a good context (See DDB 188816)
    m_thread->EndDebuggerPatchSkip();

    // Don't delete the controller yet if this is a single step exception, as the code will still want to dispatch to
    // our single step method, and if it doesn't find something to dispatch to we won't continue from the exception.
    //
    // (This is kind of broken behavior but is easily worked around here
    // by this test)
    if (!IsSingleStep(exception->ExceptionCode))
    {
        Delete();
    }

    DisableExceptionHook();

    return TPR_TRIGGER;
}

bool DebuggerPatchSkip::TriggerSingleStep(Thread *thread, const BYTE *ip)
{
    LOG((LF_CORDB,LL_INFO10000, "DPS::TSS: basically a no-op\n"));

    if (m_pAppDomain != NULL)
    {
        AppDomain *pAppDomainCur = thread->GetDomain();

        if (pAppDomainCur != m_pAppDomain)
        {
            LOG((LF_CORDB,LL_INFO10000, "DPS::TSS: Appdomain mismatch - "
                "not SingSteping!!\n"));
            return false;
        }
    }
#if defined(TARGET_AMD64)
    // Dev11 91932: for RIP-relative writes we need to copy the value that was written in our buffer to the actual address
    _ASSERTE(m_pSharedPatchBypassBuffer);
    if (m_pSharedPatchBypassBuffer->RipTargetFixup)
    {
        _ASSERTE(m_pSharedPatchBypassBuffer->RipTargetFixupSize);

        BYTE* bufferBypass = m_pSharedPatchBypassBuffer->BypassBuffer;
        BYTE fixupSize = m_pSharedPatchBypassBuffer->RipTargetFixupSize;
        UINT_PTR targetFixup = m_pSharedPatchBypassBuffer->RipTargetFixup;

        switch (fixupSize)
        {
        case 1:
            *(reinterpret_cast<BYTE*>(targetFixup)) = *(reinterpret_cast<BYTE*>(bufferBypass));
            break;

        case 2:
            *(reinterpret_cast<WORD*>(targetFixup)) = *(reinterpret_cast<WORD*>(bufferBypass));
            break;

        case 4:
            *(reinterpret_cast<DWORD*>(targetFixup)) = *(reinterpret_cast<DWORD*>(bufferBypass));
            break;

        case 8:
            *(reinterpret_cast<ULONGLONG*>(targetFixup)) = *(reinterpret_cast<ULONGLONG*>(bufferBypass));
            break;

        case 16:
        case 32:
            memcpy(reinterpret_cast<void*>(targetFixup), bufferBypass, fixupSize);
            break;

        default:
            _ASSERTE(!"bad operand size. If you hit this and it was because we need to process instructions with larger \
                relative immediates, make sure to update the SharedPatchBypassBuffer size, the DebuggerHeapExecutableMemoryAllocator, \
                and structures depending on DBG_MAX_EXECUTABLE_ALLOC_SIZE.");
        }
    }
#endif
    LOG((LF_CORDB,LL_INFO10000, "DPS::TSS: triggered, about to delete\n"));

    TRACE_FREE(this);
    Delete();
    return false;
}

// * -------------------------------------------------------------------------
// * DebuggerBreakpoint routines
// * -------------------------------------------------------------------------
// DebuggerBreakpoint::DebuggerBreakpoint()   The constructor
// invokes AddBindAndActivatePatch to set the breakpoint
DebuggerBreakpoint::DebuggerBreakpoint(Module *module,
                                       mdMethodDef md,
                                       AppDomain *pAppDomain,
                                       SIZE_T offset,
                                       bool native,
                                       SIZE_T ilEnCVersion,  // must give the EnC version for non-native bps
                                       MethodDesc *nativeMethodDesc,  // use only when m_native
                                       DebuggerJitInfo *nativeJITInfo,  // optional when m_native, null otherwise
                                       bool nativeCodeBindAllVersions,
                                       BOOL *pSucceed
                                       )
                                       : DebuggerController(NULL, pAppDomain)
{
    _ASSERTE(pSucceed != NULL);
    _ASSERTE((native == (nativeMethodDesc != NULL)) || nativeCodeBindAllVersions);
    _ASSERTE(native || nativeJITInfo == NULL);
    _ASSERTE(!nativeJITInfo || nativeJITInfo->m_jitComplete); // this is sent by the left-side, and it couldn't have got the code if the JIT wasn't complete

    if (native && !nativeCodeBindAllVersions)
    {
        (*pSucceed) = AddBindAndActivateNativeManagedPatch(nativeMethodDesc, nativeJITInfo, offset, LEAF_MOST_FRAME, pAppDomain);
        return;
    }
    else
    {
        _ASSERTE(!native || offset == 0);
        (*pSucceed) = AddILPatch(pAppDomain, module, md, NULL, ilEnCVersion, offset, !native);
    }
}

// TP_RESULT DebuggerBreakpoint::TriggerPatch()
// What: This patch will always be activated.
// How: return true.
TP_RESULT DebuggerBreakpoint::TriggerPatch(DebuggerControllerPatch *patch,
                                      Thread *thread,
                                      TRIGGER_WHY tyWhy)
{
    LOG((LF_CORDB, LL_INFO10000, "DB::TP\n"));

    return TPR_TRIGGER;
}

// void DebuggerBreakpoint::SendEvent()  What: Inform
// the right side that the breakpoint was reached.
// How: g_pDebugger->SendBreakpoint()
bool DebuggerBreakpoint::SendEvent(Thread *thread, bool fIpChanged)
{
    CONTRACTL
    {
        NOTHROW;
        SENDEVENT_CONTRACT_ITEMS;
    }
    CONTRACTL_END;


    LOG((LF_CORDB, LL_INFO10000, "DB::SE: in DebuggerBreakpoint's SendEvent\n"));

    CONTEXT *context = g_pEEInterface->GetThreadFilterContext(thread);

    // If we got interupted by SetIp, we just don't send the IPC event. Our triggers are still
    // active so no harm done.
    if (!fIpChanged)
    {
        g_pDebugger->SendBreakpoint(thread, context, this);
        return true;
    }

    // Controller is still alive, will fire if we hit the breakpoint again.
    return false;
}

//* -------------------------------------------------------------------------
// * DebuggerStepper routines
// * -------------------------------------------------------------------------

DebuggerStepper::DebuggerStepper(Thread *thread,
                                 CorDebugUnmappedStop rgfMappingStop,
                                 CorDebugIntercept interceptStop,
                                 AppDomain *appDomain)
  : DebuggerController(thread, appDomain),
    m_stepIn(false),
    m_reason(STEP_NORMAL),
    m_fpStepInto(LEAF_MOST_FRAME),
    m_rgfInterceptStop(interceptStop),
    m_rgfMappingStop(rgfMappingStop),
    m_range(NULL),
    m_rangeCount(0),
    m_realRangeCount(0),
    m_fp(LEAF_MOST_FRAME),
#if defined(FEATURE_EH_FUNCLETS)
    m_fpParentMethod(LEAF_MOST_FRAME),
#endif // FEATURE_EH_FUNCLETS
    m_fpException(LEAF_MOST_FRAME),
    m_fdException(0),
    m_cFuncEvalNesting(0)
{
#ifdef _DEBUG
    m_fReadyToSend = false;
#endif
}

DebuggerStepper::~DebuggerStepper()
{
    if (m_range != NULL)
    {
        TRACE_FREE(m_range);
        DeleteInteropSafe(m_range);
    }
}

// bool DebuggerStepper::ShouldContinueStep()   Return true if
// the stepper should not stop at this address.  The stepper should not
// stop here if: here is in the {prolog,epilog,etc};
// and the stepper is not interested in stopping here.
// We assume that this is being called in the frame which the stepper steps
// through.  Unless, of course, we're returning from a call, in which
// case we want to stop in the epilog even if the user didn't say so,
// to prevent stepping out of multiple frames at once.
// <REVISIT_TODO>Possible optimization: GetJitInfo, then AddPatch @ end of prolog?</REVISIT_TODO>
bool DebuggerStepper::ShouldContinueStep( ControllerStackInfo *info,
                                          SIZE_T nativeOffset)
{
    LOG((LF_CORDB,LL_INFO10000, "DeSt::ShContSt: nativeOffset:0x%p \n", nativeOffset));
    if (m_rgfMappingStop != STOP_ALL && (m_reason != STEP_EXIT) )
    {

        DebuggerJitInfo *ji = info->m_activeFrame.GetJitInfoFromFrame();

        if ( ji != NULL )
        {
            LOG((LF_CORDB,LL_INFO10000,"DeSt::ShContSt: For code 0x%p, got "
            "DJI 0x%p, from 0x%p to 0x%p\n",
            (const BYTE*)GetControlPC(&(info->m_activeFrame.registers)),
            ji, ji->m_addrOfCode, ji->m_addrOfCode+ji->m_sizeOfCode));
        }
        else
        {
            LOG((LF_CORDB,LL_INFO10000,"DeSt::ShCoSt: For code 0x%p, didn't "
                "get DJI\n",(const BYTE*)GetControlPC(&(info->m_activeFrame.registers))));

            return false; // Haven't a clue if we should continue, so
                          // don't
        }
        CorDebugMappingResult map = MAPPING_UNMAPPED_ADDRESS;
        DWORD whichIDontCare;
        ji->MapNativeOffsetToIL( nativeOffset, &map, &whichIDontCare);
        unsigned int interestingMappings =
            (map & ~(MAPPING_APPROXIMATE | MAPPING_EXACT));

        LOG((LF_CORDB,LL_INFO10000,
             "DeSt::ShContSt: interestingMappings:0x%x m_rgfMappingStop:%x\n",
             interestingMappings,m_rgfMappingStop));

        // If we're in a prolog,epilog, then we may want to skip
        // over it or stop
        if ( interestingMappings )
        {
            if ( interestingMappings & m_rgfMappingStop )
                return false;
            else
                return true;
        }
    }
    return false;
}

bool DebuggerStepper::IsRangeAppropriate(ControllerStackInfo *info)
{
    LOG((LF_CORDB,LL_INFO10000, "DS::IRA: info:0x%p \n", info));
    if (m_range == NULL)
    {
        LOG((LF_CORDB,LL_INFO10000, "DS::IRA: m_range == NULL, returning FALSE\n"));
        return false;
    }

    const FrameInfo *realFrame;

#if defined(FEATURE_EH_FUNCLETS)
    bool fActiveFrameIsFunclet = info->m_activeFrame.IsNonFilterFuncletFrame();

    if (fActiveFrameIsFunclet)
    {
        realFrame = &(info->GetReturnFrame());
    }
    else
#endif // FEATURE_EH_FUNCLETS
    {
        realFrame = &(info->m_activeFrame);
    }

    LOG((LF_CORDB,LL_INFO10000, "DS::IRA: info->m_activeFrame.fp:0x%p m_fp:0x%p\n", info->m_activeFrame.fp, m_fp));
    LOG((LF_CORDB,LL_INFO10000, "DS::IRA: m_fdException:0x%p realFrame->md:0x%p realFrame->fp:0x%p m_fpException:0x%p\n",
        m_fdException, realFrame->md, realFrame->fp, m_fpException));
    if ( (info->m_activeFrame.fp == m_fp) ||
         ( (m_fdException != NULL) && (realFrame->md == m_fdException) &&
           IsEqualOrCloserToRoot(realFrame->fp, m_fpException) ) )
    {
        LOG((LF_CORDB,LL_INFO10000, "DS::IRA: returning TRUE\n"));
        return true;
    }

#if defined(FEATURE_EH_FUNCLETS)
    // There are three scenarios which make this function more complicated on WIN64.
    // 1)  We initiate a step in the parent method or a funclet but end up stepping into another funclet closer to the leaf.
    //      a)  start in the parent method
    //      b)  start in a funclet
    // 2)  We initiate a step in a funclet but end up stepping out to the parent method or a funclet closer to the root.
    //      a) end up in the parent method
    //      b) end up in a funclet
    // 3)  We initiate a step and then change stack allocation within the method or funclet
    // In both cases the range of the stepper should still be appropriate.

    bool fValidParentMethodFP = (m_fpParentMethod != LEAF_MOST_FRAME);

    // All scenarios have the same condition
    if (fValidParentMethodFP && (m_fpParentMethod == info->GetReturnFrame(true).fp))
    {
        LOG((LF_CORDB,LL_INFO10000, "DS::IRA: (parent SP) returning TRUE\n"));
        return true;
    }
#endif // FEATURE_EH_FUNCLETS

    LOG((LF_CORDB,LL_INFO10000, "DS::IRA: returning FALSE\n"));
    return false;
}

// bool DebuggerStepper::IsInRange()   Given the native offset ip,
// returns true if ip falls within any of the native offset ranges specified
// by the array of COR_DEBUG_STEP_RANGEs.
// Returns true if ip falls within any of the ranges.  Returns false
// if ip doesn't, or if there are no ranges (rangeCount==0). Note that a
// COR_DEBUG_STEP_RANGE with an endOffset of zero is interpreted as extending
// from startOffset to the end of the method.
// SIZE_T ip:  Native offset, relative to the beginning of the method.
// COR_DEBUG_STEP_RANGE *range:  An array of ranges, which are themselves
//        native offsets, to compare against ip.
// SIZE_T rangeCount:  Number of elements in range
bool DebuggerStepper::IsInRange(SIZE_T ip, COR_DEBUG_STEP_RANGE *range, SIZE_T rangeCount,
                                ControllerStackInfo *pInfo)
{
    LOG((LF_CORDB,LL_INFO10000,"DS::IIR: off=0x%p\n", ip));

    if (range == NULL)
    {
        LOG((LF_CORDB,LL_INFO10000,"DS::IIR: range == NULL -> not in range\n"));
        return false;
    }

    if (pInfo && !IsRangeAppropriate(pInfo))
    {
        LOG((LF_CORDB,LL_INFO10000,"DS::IIR: no pInfo or range not appropriate -> not in range\n"));
        return false;
    }

    COR_DEBUG_STEP_RANGE *r = range;
    COR_DEBUG_STEP_RANGE *rEnd = r + rangeCount;

    while (r < rEnd)
    {
        SIZE_T endOffset = r->endOffset ? r->endOffset : ~0;
        LOG((LF_CORDB,LL_INFO100000,"DS::IIR: so=0x%x, eo=0x%x\n",
             r->startOffset, endOffset));

        if (ip >= r->startOffset && ip < endOffset)
        {
            LOG((LF_CORDB,LL_INFO1000,"DS::IIR:this:0x%p Found native offset "
                "0x%x to be in the range"
                "[0x%x, 0x%x), index 0x%x\n\n", this, ip, r->startOffset,
                endOffset, ((r-range)/sizeof(COR_DEBUG_STEP_RANGE *)) ));
            return true;
        }

        r++;
    }

    LOG((LF_CORDB,LL_INFO10000,"DS::IIR: not in range\n"));
    return false;
}

// bool DebuggerStepper::DetectHandleInterceptors()  Return true if
// the current execution takes place within an interceptor (that is, either
// the current frame, or the parent frame is a framed frame whose
// GetInterception method returns something other than INTERCEPTION_NONE),
// and this stepper doesn't want to stop in an interceptor, and we successfully
// set a breakpoint after the top-most interceptor in the stack.
bool DebuggerStepper::DetectHandleInterceptors(ControllerStackInfo *info)
{
    LOG((LF_CORDB,LL_INFO10000,"DS::DHI: Start DetectHandleInterceptors\n"));
    LOG((LF_CORDB,LL_INFO10000,"DS::DHI: active frame=0x%p, has return frame=%d, return frame=0x%p m_reason:%d\n",
         info->m_activeFrame.frame, info->HasReturnFrame(), info->GetReturnFrame().frame, m_reason));

    // If this is a normal step, then we want to continue stepping, even if we
    // are in an interceptor.
    if (m_reason == STEP_NORMAL || m_reason == STEP_RETURN || m_reason == STEP_EXCEPTION_HANDLER)
    {
        LOG((LF_CORDB,LL_INFO1000,"DS::DHI: Returning false while stepping within function, finally!\n"));
        return false;
    }

    bool fAttemptStepOut = false;

    if (m_rgfInterceptStop != INTERCEPT_ALL) // we may have to skip out of one
    {
        if (info->m_activeFrame.frame != NULL &&
            info->m_activeFrame.frame != FRAME_TOP &&
            info->m_activeFrame.frame->GetInterception() != Frame::INTERCEPTION_NONE)
        {
            if (!((CorDebugIntercept)info->m_activeFrame.frame->GetInterception() & Frame::Interception(m_rgfInterceptStop)))
            {
                LOG((LF_CORDB,LL_INFO10000,"DS::DHI: Stepping out b/c of excluded frame type:0x%x\n",
                     info->m_activeFrame.frame->GetInterception()));

                fAttemptStepOut = true;
            }
            else
            {
                LOG((LF_CORDB,LL_INFO10000,"DS::DHI: 0x%p set to STEP_INTERCEPT\n", this));

                m_reason = STEP_INTERCEPT; //remember why we've stopped
            }
        }

        if ((m_reason == STEP_EXCEPTION_FILTER) ||
            (info->HasReturnFrame() &&
            info->GetReturnFrame().frame != NULL &&
            info->GetReturnFrame().frame != FRAME_TOP &&
            info->GetReturnFrame().frame->GetInterception() != Frame::INTERCEPTION_NONE))
        {
            if (m_reason == STEP_EXCEPTION_FILTER)
            {
                // Exceptions raised inside of the EE by COMPlusThrow, FCThrow, etc will not
                // insert an ExceptionFrame, and hence info->GetReturnFrame().frame->GetInterception()
                // will not be accurate. Hence we use m_reason instead

                if (!(Frame::INTERCEPTION_EXCEPTION & Frame::Interception(m_rgfInterceptStop)))
                {
                    LOG((LF_CORDB,LL_INFO10000,"DS::DHI: Stepping out b/c of excluded INTERCEPTION_EXCEPTION\n"));
                    fAttemptStepOut = true;
                }
            }
            else if (!(info->GetReturnFrame().frame->GetInterception() & Frame::Interception(m_rgfInterceptStop)))
            {
                LOG((LF_CORDB,LL_INFO10000,"DS::DHI: Stepping out b/c of excluded return frame type:0x%x\n",
                     info->GetReturnFrame().frame->GetInterception()));

                fAttemptStepOut = true;
            }

            if (!fAttemptStepOut)
            {
                LOG((LF_CORDB,LL_INFO10000,"DS::DHI 0x%x set to STEP_INTERCEPT\n", this));

                m_reason = STEP_INTERCEPT; //remember why we've stopped
            }
        }
        else if (info->m_specialChainReason != CHAIN_NONE)
        {
            if(!(info->m_specialChainReason & CorDebugChainReason(m_rgfInterceptStop)) )
            {
                LOG((LF_CORDB,LL_INFO10000, "DS::DHI: (special) Stepping out b/c of excluded return frame type:0x%x\n",
                     info->m_specialChainReason));

                fAttemptStepOut = true;
            }
            else
            {
                LOG((LF_CORDB,LL_INFO10000,"DS::DHI 0x%x set to STEP_INTERCEPT\n", this));

                m_reason = STEP_INTERCEPT; //remember why we've stopped
            }
        }
        else if (info->m_activeFrame.frame == NULL)
        {
            // Make sure we are not dealing with a chain here.
            if (info->m_activeFrame.HasMethodFrame())
            {
                // Check whether we are executing in a class constructor.
                _ASSERTE(info->m_activeFrame.md != NULL);
                if (info->m_activeFrame.md->IsClassConstructor())
                {
                    // We are in a class constructor.  Check whether we want to stop in it.
                    if (!(CHAIN_CLASS_INIT & CorDebugChainReason(m_rgfInterceptStop)))
                    {
                        LOG((LF_CORDB, LL_INFO10000, "DS::DHI: Stepping out b/c of excluded cctor:0x%x\n",
                             CHAIN_CLASS_INIT));

                        fAttemptStepOut = true;
                    }
                    else
                    {
                        LOG((LF_CORDB, LL_INFO10000,"DS::DHI 0x%x set to STEP_INTERCEPT\n", this));

                        m_reason = STEP_INTERCEPT; //remember why we've stopped
                    }
                }
            }
        }
    }

    if (fAttemptStepOut)
    {
        LOG((LF_CORDB,LL_INFO1000,"DS::DHI: Doing TSO!\n"));

        // TrapStepOut could alter the step reason if we're stepping out of an inteceptor and it looks like we're
        // running off the top of the program. So hold onto it here, and if our step reason becomes STEP_EXIT, then
        // reset it to what it was.
        CorDebugStepReason holdReason = m_reason;

        // @todo - should this be TrapStepNext??? But that may stop in a child...
        TrapStepOut(info);
        EnableUnwind(m_fp);

        if (m_reason == STEP_EXIT)
        {
            m_reason = holdReason;
        }

        return true;
    }

    // We're not in a special area of code, so we don't want to continue unless some other part of the code decides that
    // we should.
    LOG((LF_CORDB,LL_INFO1000,"DS::DHI: Returning false, finally!\n"));

    return false;
}


//---------------------------------------------------------------------------------------
//
// This function checks whether the given IP is in an LCG method.  If so, it enables
// JMC and does a step out.  This effectively makes sure that we never stop in an LCG method.
//
// There are two common scnearios here:
// 1)  We single-step into an LCG method from a managed method.
// 2)  We single-step off the end of a method called by an LCG method and end up in the calling LCG method.
//
// In both cases, we don't want to stop in the LCG method.  If the LCG method directly or indirectly calls
// another user method, we want to stop there.  Otherwise, we just want to step out back to the caller of
// LCG method.  In other words, what we want is exactly the JMC behaviour.
//
// Arguments:
//    ip    - the current IP where the thread is stopped at
//    pMD   - This is the MethodDesc for the specified ip.  This can be NULL, but if it's not,
//            then it has to match the specified IP.
//    pInfo - the ControllerStackInfo taken at the specified IP (see Notes below)
//
// Return Value:
//    Returns TRUE if the specified IP is indeed in an LCG method, in which case this function has already
//    enabled all the traps to catch the thread, including turning on JMC, enabling unwind callback, and
//    putting a patch in the caller.
//
// Notes:
//    LCG methods don't show up in stackwalks done by the ControllerStackInfo.  So even if the specified IP
//    is in an LCG method, the LCG method won't show up in the call strack.  That's why we need to call
//    ControllerStackInfo::SetReturnFrameWithActiveFrame() in this function before calling TrapStepOut().
//    Otherwise TrapStepOut() will put a patch in the caller's caller (if there is one).
//

BOOL DebuggerStepper::DetectHandleLCGMethods(const PCODE ip, MethodDesc * pMD, ControllerStackInfo * pInfo)
{
    // Look up the MethodDesc for the given IP.
    if (pMD == NULL)
    {
        if (g_pEEInterface->IsManagedNativeCode((const BYTE *)ip))
        {
            pMD = g_pEEInterface->GetNativeCodeMethodDesc(ip);
            _ASSERTE(pMD != NULL);
        }
    }
#if defined(_DEBUG)
    else
    {
        // If a MethodDesc is specified, it has to match the given IP.
        _ASSERTE(pMD == g_pEEInterface->GetNativeCodeMethodDesc(ip));
    }
#endif // _DEBUG

    // If the given IP is in unmanaged code, then we won't have a MethodDesc by this point.
    if (pMD != NULL)
    {
        if (pMD->IsLCGMethod())
        {
            // Enable all the traps to catch the thread.
            EnableUnwind(m_fp);
            EnableJMCBackStop(pMD);

            pInfo->SetReturnFrameWithActiveFrame();
            TrapStepOut(pInfo);
            return TRUE;
        }
    }

    return FALSE;
}


// Steppers override these so that they can skip func-evals. Note that steppers can
// be created & used inside of func-evals (nested-break states).
// On enter, we check for freezing the stepper.
void DebuggerStepper::TriggerFuncEvalEnter(Thread * thread)
{
    LOG((LF_CORDB, LL_INFO10000, "DS::TFEEnter, this=0x%p, old nest=%d\n", this, m_cFuncEvalNesting));

    // Since this is always called on the hijacking thread, we should be thread-safe
    _ASSERTE(thread == this->GetThread());

    if (IsDead())
        return;

    m_cFuncEvalNesting++;

    if (m_cFuncEvalNesting == 1)
    {
        // We're entering our 1st funceval, so freeze us.
        LOG((LF_CORDB, LL_INFO100000, "DS::TFEEnter - freezing stepper\n"));

        // Freeze the stepper by disabling all triggers
        m_bvFrozenTriggers = 0;

        //
        // We dont explicitly disable single-stepping because the OS
        // gives us a new thread context during an exception.  Since
        // all func-evals are done inside exceptions, we should never
        // have this problem.
        //
        // Note: however, that if func-evals were no longer done in
        // exceptions, this would have to change.
        //


        if (IsMethodEnterEnabled())
        {
            m_bvFrozenTriggers |= kMethodEnter;
            DisableMethodEnter();
        }

    }
    else
    {
        LOG((LF_CORDB, LL_INFO100000, "DS::TFEEnter - new nest=%d\n", m_cFuncEvalNesting));
    }
}

// On Func-EvalExit, we check if the stepper is trying to step-out of a func-eval
// (in which case we kill it)
// or if we previously entered this func-eval and should thaw it now.
void DebuggerStepper::TriggerFuncEvalExit(Thread * thread)
{
    LOG((LF_CORDB, LL_INFO10000, "DS::TFEExit, this=0x%p, old nest=%d\n", this, m_cFuncEvalNesting));

    // Since this is always called on the hijacking thread, we should be thread-safe
    _ASSERTE(thread == this->GetThread());

    if (IsDead())
        return;

    m_cFuncEvalNesting--;

    if (m_cFuncEvalNesting == -1)
    {
        LOG((LF_CORDB, LL_INFO100000, "DS::TFEExit - disabling stepper\n"));

        // we're exiting the func-eval session we were created in. So we just completely
        // disable ourselves so that we don't fire anything anymore.
        // The RS still has to free the stepper though.

        // This prevents us from stepping-out of a func-eval. For traditional steppers,
        // this is overkill since it won't have any outstanding triggers. (trap-step-out
        // won't patch if it crosses a func-eval frame).
        // But JMC-steppers have Method-Enter; and so this is the only place we have to
        // disable that.
        DisableAll();
    }
    else if (m_cFuncEvalNesting == 0)
    {
        // We're back to our starting Func-eval session, we should have been frozen,
        // so now we thaw.
        LOG((LF_CORDB, LL_INFO100000, "DS::TFEExit - thawing stepper\n"));

        // Thaw the stepper (reenable triggers)
        if ((m_bvFrozenTriggers & kMethodEnter) != 0)
        {
            EnableMethodEnter();
        }
        m_bvFrozenTriggers = 0;

    }
    else
    {
        LOG((LF_CORDB, LL_INFO100000, "DS::TFEExit - new nest=%d\n", m_cFuncEvalNesting));
    }
}


// Return true iff we set a patch (which implies to caller that we should
// let controller run free and hit that patch)
bool DebuggerStepper::TrapStepInto(ControllerStackInfo *info,
                                   const BYTE *ip,
                                   TraceDestination *pTD)
{
    _ASSERTE( pTD != NULL );
    _ASSERTE(this->GetDCType() == DEBUGGER_CONTROLLER_STEPPER);

    EnableTraceCall(LEAF_MOST_FRAME);
    if (IsCloserToRoot(info->m_activeFrame.fp, m_fpStepInto))
        m_fpStepInto = info->m_activeFrame.fp;

    LOG((LF_CORDB, LL_INFO1000, "DS::TSI this:0x%p m_fpStepInto:0x%p\n",
        this, m_fpStepInto.GetSPValue()));

    TraceDestination trace;

    // Trace through the stubs.
    // If we're calling from managed code, this should either succeed
    // or become an ecall into mscorwks.
    // @Todo - what about stubs in mscorwks.
    // @todo - if this fails, we want to provde as much info as possible.
    if (!g_pEEInterface->TraceStub(ip, &trace)
        || !g_pEEInterface->FollowTrace(&trace))
    {
        return false;
    }


    (*pTD) = trace; //bitwise copy

    // Step-in always operates at the leaf-most frame. Thus the frame pointer for any
    // patch for step-in should be LEAF_MOST_FRAME, regardless of whatever our current fp
    // is before the step-in.
    // Note that step-in may skip 'internal' frames (FrameInfo w/ internal=true) since
    // such frames may really just be a marker for an internal EE Frame on the stack.
    // However, step-out uses these frames b/c it may call frame->TraceFrame() on them.
    return PatchTrace(&trace,
                      LEAF_MOST_FRAME, // step-in is always leaf-most frame.
                      (m_rgfMappingStop&STOP_UNMANAGED)?(true):(false));
}

// Enable the JMC backstop for stepping on Step-In.
// This activate the JMC probes, which will provide a safety net
// to stop a stepper if the StubManagers don't predict the call properly.
// Ideally, this should never be necessary (because the SMs would do their job).
void DebuggerStepper::EnableJMCBackStop(MethodDesc * pStartMethod)
{
    // JMC steppers should not need the JMC backstop unless a thread inadvertently stops in an LCG method.
    //_ASSERTE(DEBUGGER_CONTROLLER_JMC_STEPPER != this->GetDCType());

    // Since we should never hit the JMC backstop (since it's really a SM issue), we'll assert if we actually do.
    // However, there's 1 corner case here. If we trace calls at the start of the method before the JMC-probe,
    // then we'll still hit the JMC backstop in our own method.
    // Record that starting method. That way, if we end up hitting our JMC backstop in our own method,
    // we don't over aggressively fire the assert. (This won't work for recursive cases, but since this is just
    // changing an assert, we don't care).

#ifdef _DEBUG
    // May be NULL if we didn't start in a method.
    m_StepInStartMethod = pStartMethod;
#endif

    // We don't want traditional steppers to rely on MethodEnter (b/c it's not guaranteed to be correct),
    // but it may be a useful last resort.
    this->EnableMethodEnter();
}

// Return true if the stepper can run free.
bool DebuggerStepper::TrapStepInHelper(
    ControllerStackInfo * pInfo,
    const BYTE * ipCallTarget,
    const BYTE * ipNext,
    bool fCallingIntoFunclet,
    bool fIsJump)
{
    TraceDestination td;

#ifdef _DEBUG
    // Begin logging the step-in activity in debug builds.
    StubManager::DbgBeginLog((TADDR) ipNext, (TADDR) ipCallTarget);
#endif


    if (TrapStepInto(pInfo, ipCallTarget, &td))
    {
        // If we placed a patch, see if we need to update our step-reason
        if (td.GetTraceType() == TRACE_MANAGED )
        {
            // Possible optimization: Roll all of g_pEEInterface calls into
            // one function so we don't repeatedly get the CodeMan,etc
            MethodDesc *md = NULL;
            _ASSERTE( g_pEEInterface->IsManagedNativeCode((const BYTE *)td.GetAddress()) );
            md = g_pEEInterface->GetNativeCodeMethodDesc(td.GetAddress());

            DebuggerJitInfo* pDJI = g_pDebugger->GetJitInfoFromAddr(td.GetAddress());
            CodeRegionInfo code = CodeRegionInfo::GetCodeRegionInfo(pDJI, md);
            if (code.AddressToOffset((const BYTE *)td.GetAddress()) == 0)
            {

                LOG((LF_CORDB,LL_INFO1000,"\tDS::TS 0x%x m_reason = STEP_CALL"
                     "@ip0x%x\n", this, (BYTE*)GetControlPC(&(pInfo->m_activeFrame.registers))));
                  m_reason = STEP_CALL;
            }
            else
            {
                LOG((LF_CORDB, LL_INFO1000, "Didn't step: md:0x%x"
                     "td.type:%s td.address:0x%p,  hot code address:0x%p\n",
                     md, GetTType(td.GetTraceType()), td.GetAddress(),
                    code.getAddrOfHotCode()));
            }
        }
        else
        {
            LOG((LF_CORDB,LL_INFO10000,"DS::TS else 0x%x m_reason = STEP_CALL\n",
                 this));
            m_reason = STEP_CALL;
        }


        return true;
    } // end TrapStepIn
    else
    {
        // If we can't figure out where the stepper should call into (likely because we can't find a stub-manager),
        // then enable the JMC backstop.
        EnableJMCBackStop(pInfo->m_activeFrame.md);

    }

    // We ignore ipNext here. Instead we'll return false and let the caller (TrapStep)
    // set the patch for us.
    return false;
}

static bool IsTailCallJitHelper(const BYTE * ip)
{
    return TailCallStubManager::IsTailCallJitHelper(reinterpret_cast<PCODE>(ip));
}

// Check whether a call to an IP will be a tailcall dispatched by first
// returning. When a tailcall cannot be performed just with a jump instruction,
// the code will be doing a regular call to a managed function called the
// tailcall dispatcher. This functions dispatches tailcalls in a special way: if
// there is a previous "tailcall aware" frame, then it will simply record the
// next tailcall to perform and immediately return. Otherwise it will set up
// such a tailcall aware frame and dispatch tailcalls. In the former case the
// control flow will be a little peculiar in that the function will return
// immediately, so we need special handling in the debugger for it. This
// function detects that case to be used for those scenarios.
static bool IsTailCallThatReturns(const BYTE * ip, ControllerStackInfo* info)
{
    MethodDesc* pTailCallDispatcherMD = TailCallHelp::GetTailCallDispatcherMD();
    if (pTailCallDispatcherMD == NULL)
    {
        return false;
    }

    TraceDestination trace;
    if (!g_pEEInterface->TraceStub(ip, &trace) || !g_pEEInterface->FollowTrace(&trace))
    {
        return false;
    }

    MethodDesc* pTargetMD =
        trace.GetTraceType() == TRACE_UNJITTED_METHOD
        ? trace.GetMethodDesc()
        : g_pEEInterface->GetNativeCodeMethodDesc(trace.GetAddress());

    if (pTargetMD != pTailCallDispatcherMD)
    {
        return false;
    }

    LOG((LF_CORDB, LL_INFO1000, "ITCTR: target %p is the tailcall dispatcher\n", ip));

    _ASSERTE(info->HasReturnFrame());
    LPVOID retAddr = (LPVOID)GetControlPC(&info->GetReturnFrame().registers);
    TailCallTls* tls = GetThread()->GetTailCallTls();
    LPVOID tailCallAwareRetAddr = tls->GetFrame()->TailCallAwareReturnAddress;

    LOG((LF_CORDB,LL_INFO1000, "ITCTR: ret addr is %p, tailcall aware ret addr is %p\n",
        retAddr, tailCallAwareRetAddr));

    return retAddr == tailCallAwareRetAddr;
}

// bool DebuggerStepper::TrapStep()   TrapStep attepts to set a
// patch at the next IL instruction to be executed.  If we're stepping in &
// the next IL instruction is a call, then this'll set a breakpoint inside
// the code that will be called.
// How: There are a number of cases, depending on where the IP
// currently is:
// Unmanaged code: EnableTraceCall() & return false - try and get
// it when it returns.
// In a frame: if the <p in> param is true, then do an
// EnableTraceCall().  If the frame isn't the top frame, also do
// g_pEEInterface->TraceFrame(), g_pEEInterface->FollowTrace, and
// PatchTrace.
// Normal managed frame: create a Walker and walk the instructions until either
// leave the provided range (AddPatch there, return true), or we don't know what the
// next instruction is (say, after a call, or return, or branch - return false).
// Returns a boolean indicating if we were able to set a patch successfully
// in either this method, or (if in == true & the next instruction is a call)
// inside a callee method.
// true:   Patch successfully placed either in this method or a callee,
// so the stepping is taken care of.
// false:  Unable to place patch in either this method or any
// applicable callee methods, so the only option the caller has to put
// patch to control flow is to call TrapStepOut & try and place a patch
// on the method that called the current frame's method.
bool DebuggerStepper::TrapStep(ControllerStackInfo *info, bool in)
{
    LOG((LF_CORDB,LL_INFO10000,"DS::TS: this:0x%x\n", this));
    if (!info->m_activeFrame.managed)
    {
        //
        // We're not in managed code.  Patch up all paths back in.
        //

        LOG((LF_CORDB,LL_INFO10000, "DS::TS: not in managed code\n"));

        if (in)
        {
            EnablePolyTraceCall();
        }

        return false;
    }

    if (info->m_activeFrame.frame != NULL)
    {

        //
        // We're in some kind of weird frame.  Patch further entry to the frame.
        // or if we can't, patch return from the frame
        //

        LOG((LF_CORDB,LL_INFO10000, "DS::TS: in a weird frame\n"));

        if (in)
        {
            EnablePolyTraceCall();

            // Only traditional steppers should patch a frame. JMC steppers will
            // just rely on TriggerMethodEnter.
            if (DEBUGGER_CONTROLLER_STEPPER == this->GetDCType())
            {
                if (info->m_activeFrame.frame != FRAME_TOP)
                {
                    TraceDestination trace;

                    CONTRACT_VIOLATION(GCViolation); // TraceFrame GC-triggers

                    // This could be anywhere, especially b/c step could be on non-leaf frame.
                    if (g_pEEInterface->TraceFrame(this->GetThread(),
                                                   info->m_activeFrame.frame,
                                                   FALSE, &trace,
                                                   &(info->m_activeFrame.registers))
                        && g_pEEInterface->FollowTrace(&trace)
                        && PatchTrace(&trace, info->m_activeFrame.fp,
                                      (m_rgfMappingStop&STOP_UNMANAGED)?
                                        (true):(false)))

                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

#ifdef TARGET_X86
    LOG((LF_CORDB,LL_INFO1000, "GetJitInfo for pc = 0x%x (addr of "
        "that value:0x%x)\n", (const BYTE*)(GetControlPC(&info->m_activeFrame.registers)),
        info->m_activeFrame.registers.PCTAddr));
#endif

    // Note: we used to pass in the IP from the active frame to GetJitInfo, but there seems to be no value in that, and
    // it was causing problems creating a stepper while sitting in ndirect stubs after we'd returned from the unmanaged
    // function that had been called.
    DebuggerJitInfo *ji = info->m_activeFrame.GetJitInfoFromFrame();
    if( ji != NULL )
    {
        LOG((LF_CORDB,LL_INFO10000,"DS::TS: For code 0x%p, got DJI 0x%p, "
            "from 0x%p to 0x%p\n",
            (const BYTE*)(GetControlPC(&info->m_activeFrame.registers)),
            ji, ji->m_addrOfCode, ji->m_addrOfCode+ji->m_sizeOfCode));
    }
    else
    {
        LOG((LF_CORDB,LL_INFO10000,"DS::TS: For code 0x%p, "
            "didn't get a DJI \n",
            (const BYTE*)(GetControlPC(&info->m_activeFrame.registers))));
    }

    //
    // We're in a normal managed frame - walk the code
    //

    NativeWalker walker;

    LOG((LF_CORDB,LL_INFO1000, "DS::TS: &info->m_activeFrame.registers 0x%p\n", &info->m_activeFrame.registers));

    // !!! Eventually when using the fjit, we'll want
    // to walk the IL to get the next location, & then map
    // it back to native.
    walker.Init((BYTE*)GetControlPC(&(info->m_activeFrame.registers)), &info->m_activeFrame.registers);


    // Is the active frame really the active frame?
    // What if the thread is stopped at a managed debug event outside of a filter ctx? Eg, stopped
    // somewhere directly in mscorwks (like sending a LogMsg or ClsLoad event) or even at WaitForSingleObject.
    // ActiveFrame is either the stepper's initial frame or the frame of a filterctx.
    bool fIsActiveFrameLive = (info->m_activeFrame.fp == info->m_bottomFP);

    // If this thread isn't stopped in managed code, it can't be at the active frame.
    if (GetManagedStoppedCtx(this->GetThread()) == NULL)
    {
        fIsActiveFrameLive = false;
    }

    bool fIsJump             = false;
    bool fCallingIntoFunclet = false;

    // If m_activeFrame is not the actual active frame,
    // we should skip this first switch - never single step, and
    // assume our context is bogus.
    if (fIsActiveFrameLive)
    {
        // Note that by definition our walker must always be able to step
        // through a single instruction, so any return
        // of NULL IP's from those cases on the first step
        // means that an exception is going to be generated.
        //
        // (On future steps, it can also mean that the destination
        // simply can't be computed.)
        WALK_TYPE wt = walker.GetOpcodeWalkType();
        {
            switch (wt)
            {
            case WALK_RETURN:
                {
                    LOG((LF_CORDB,LL_INFO10000, "DC::TS:Imm:WALK_RETURN\n"));

                    // Normally a 'ret' opcode means we're at the end of a function and doing a step-out.
                    // But the jit is free to use a 'ret' opcode to implement various goofy constructs like
                    // managed filters, in which case we may ret to the same function or we may ret to some
                    // internal CLR stub code.
                    // So we'll just ignore this and tell the Stepper to enable every notification it has
                    // and let the thread run free. This will include TrapStepOut() and EnableUnwind()
                    // to catch any potential filters.


                    // Go ahead and enable the single-step flag too. We know it's safe.
                    // If this lands in random code, then TriggerSingleStep will just ignore it.
                    EnableSingleStep();

                    // Don't set step-reason yet. If another trigger gets hit, it will set the reason.
                    return false;
                }

            case WALK_BRANCH:
                LOG((LF_CORDB,LL_INFO10000, "DC::TS:Imm:WALK_BRANCH\n"));
                // A branch can be handled just like a call. If the branch is within the current method, then we just
                // down to WALK_UNKNOWN, otherwise we handle it just like a call.  Note: we need to force in=true
                // because for a jmp, in or over is the same thing, we're still going there, and the in==true case is
                // the case we want to use...
                fIsJump = true;

                FALLTHROUGH;

            case WALK_CALL:
                LOG((LF_CORDB,LL_INFO10000, "DC::TS:Imm:WALK_CALL ip=%p nextip=%p skipip=%p\n", walker.GetIP(), walker.GetNextIP(), walker.GetSkipIP()));

                // If we're doing some sort of intra-method jump (usually, to get EIP in a clever way, via the CALL
                // instruction), then put the bp where we're going, NOT at the instruction following the call
                if (IsAddrWithinFrame(ji, info->m_activeFrame.md, walker.GetIP(), walker.GetNextIP()))
                {
                    LOG((LF_CORDB, LL_INFO1000, "Walk call within method!" ));
                    goto LWALK_UNKNOWN;
                }

                if (walker.GetNextIP() != NULL)
                {
#ifdef FEATURE_EH_FUNCLETS
                    // There are 4 places we could be jumping:
                    // 1) to the beginning of the same method (recursive call)
                    // 2) somewhere in the same funclet, that isn't the method start
                    // 3) somewhere in the same method but different funclet
                    // 4) somewhere in a different method
                    //
                    // IsAddrWithinFrame ruled out option 2, IsAddrWithinMethodIncludingFunclet rules out option 4,
                    // and checking the IP against the start address rules out option 1. That leaves option only what we
                    // wanted, option #3
                    fCallingIntoFunclet = IsAddrWithinMethodIncludingFunclet(ji, info->m_activeFrame.md, walker.GetNextIP()) &&
                        ((CORDB_ADDRESS)(SIZE_T)walker.GetNextIP() != ji->m_addrOfCode);
#endif
                    // At this point, we know that the call/branch target is not
                    // in the current method. The possible cases is that this is
                    // a jump or a tailcall-via-helper. There are two separate
                    // tailcalling mechanisms: on x86 we use a JIT helper which
                    // will look like a regular call and which won't return, so
                    // a step over becomes a step out. On other platforms we use
                    // a separate mechanism that will perform a tailcall by
                    // returning to an IL stub first. A step over in this case
                    // is done by stepping out to the previous user function
                    // (non IL stub).
                    if ((fIsJump && !fCallingIntoFunclet) || IsTailCallJitHelper(walker.GetNextIP()) ||
                        IsTailCallThatReturns(walker.GetNextIP(), info))
                    {
                        // A step-over becomes a step-out for a tail call.
                        if (!in)
                        {
                            TrapStepOut(info);
                            return true;
                        }
                    }

                    // To preserve the old behaviour, if this is not a tail call, then we assume we want to
                    // follow the call/jump.
                    if (fIsJump)
                    {
                        in = true;
                    }


                    // There are two cases where we need to perform a step-in.  One, if the step operation is
                    // a step-in.  Two, if the target address of the call is in a funclet of the current method.
                    // In this case, we want to step into the funclet even if the step operation is a step-over.
                    if (in || fCallingIntoFunclet)
                    {
                        if (TrapStepInHelper(info, walker.GetNextIP(), walker.GetSkipIP(), fCallingIntoFunclet, fIsJump))
                        {
                            return true;
                        }
                    }

                }
                if (walker.GetSkipIP() == NULL)
                {
                    LOG((LF_CORDB,LL_INFO10000,"DS::TS 0x%p m_reason = STEP_CALL (skip)\n",
                         this));
                    m_reason = STEP_CALL;

                    return true;
                }


                LOG((LF_CORDB,LL_INFO100000, "DC::TS:Imm:WALK_CALL Skip instruction\n"));
                walker.Skip();
                break;

            case WALK_UNKNOWN:
    LWALK_UNKNOWN:
                LOG((LF_CORDB,LL_INFO10000,"DS::TS:WALK_UNKNOWN - curIP:0x%p "
                    "nextIP:0x%p skipIP:0x%p 1st byte of opcode:0x%x\n", (BYTE*)GetControlPC(&(info->m_activeFrame.
                    registers)), walker.GetNextIP(),walker.GetSkipIP(),
                    *(BYTE*)GetControlPC(&(info->m_activeFrame.registers))));

                EnableSingleStep();

                return true;

            default:
                if (walker.GetNextIP() == NULL)
                {
                    return true;
                }

                walker.Next();
            }
        }
    } // if (fIsActiveFrameLive)

    //
    // Use our range, if we're in the original
    // frame.
    //

    COR_DEBUG_STEP_RANGE    *range;
    SIZE_T                    rangeCount;

    if (info->m_activeFrame.fp == m_fp)
    {
        range = m_range;
        rangeCount = m_rangeCount;
    }
    else
    {
        range = NULL;
        rangeCount = 0;
    }

    //
    // Keep walking until either we're out of range, or
    // else we can't predict ahead any more.
    //

    while (TRUE)
    {
        const BYTE *ip = walker.GetIP();

        SIZE_T offset = CodeRegionInfo::GetCodeRegionInfo(ji, info->m_activeFrame.md).AddressToOffset(ip);

        LOG((LF_CORDB, LL_INFO1000, "Walking to ip 0x%p (natOff:0x%x)\n",ip,offset));

        if (!IsInRange(offset, range, rangeCount)
            && !ShouldContinueStep( info, offset ))
        {
            AddBindAndActivateNativeManagedPatch(info->m_activeFrame.md,
                     ji,
                     offset,
                     info->GetReturnFrame().fp,
                     NULL);
            return true;
        }

        switch (walker.GetOpcodeWalkType())
        {
        case WALK_RETURN:

            LOG((LF_CORDB, LL_INFO10000, "DS::TS: WALK_RETURN Adding Patch.\n"));

            // In the loop above, if we're at the return address, we'll check & see
            // if we're returning to elsewhere within the same method, and if so,
            // we'll single step rather than TrapStepOut. If we see a return in the
            // code stream, then we'll set a breakpoint there, so that we can
            // examine the return address, and decide whether to SS or TSO then
            AddBindAndActivateNativeManagedPatch(info->m_activeFrame.md,
                     ji,
                     offset,
                     info->GetReturnFrame().fp,
                     NULL);
            return true;

        case WALK_CALL:

            LOG((LF_CORDB, LL_INFO10000, "DS::TS: WALK_CALL.\n"));

            // If we're doing some sort of intra-method jump (usually, to get EIP in a clever way, via the CALL
            // instruction), then put the bp where we're going, NOT at the instruction following the call
            if (IsAddrWithinFrame(ji, info->m_activeFrame.md, walker.GetIP(), walker.GetNextIP()))
            {
                LOG((LF_CORDB, LL_INFO10000, "DS::TS: WALK_CALL IsAddrWithinFrame, Adding Patch.\n"));

                // How else to detect this?
                AddBindAndActivateNativeManagedPatch(info->m_activeFrame.md,
                         ji,
                         CodeRegionInfo::GetCodeRegionInfo(ji, info->m_activeFrame.md).AddressToOffset(walker.GetNextIP()),
                         info->GetReturnFrame().fp,
                         NULL);
                return true;
            }

            if (IsTailCallJitHelper(walker.GetNextIP()) || IsTailCallThatReturns(walker.GetNextIP(), info))
            {
                if (!in)
                {
                    AddBindAndActivateNativeManagedPatch(info->m_activeFrame.md,
                                                         ji,
                                                         offset,
                                                         info->GetReturnFrame().fp,
                                                         NULL);
                    return true;
                }
            }

#ifdef FEATURE_EH_FUNCLETS
            fCallingIntoFunclet = IsAddrWithinMethodIncludingFunclet(ji, info->m_activeFrame.md, walker.GetNextIP());
#endif
            if (in || fCallingIntoFunclet)
            {
                LOG((LF_CORDB, LL_INFO10000, "DS::TS: WALK_CALL step in is true\n"));
                if (walker.GetNextIP() == NULL)
                {
                    LOG((LF_CORDB, LL_INFO10000, "DS::TS: WALK_CALL NextIP == NULL\n"));
                    AddBindAndActivateNativeManagedPatch(info->m_activeFrame.md,
                             ji,
                             offset,
                             info->GetReturnFrame().fp,
                             NULL);

                    LOG((LF_CORDB,LL_INFO10000,"DS0x%x m_reason=STEP_CALL 2\n",
                         this));
                    m_reason = STEP_CALL;

                    return true;
                }

                if (TrapStepInHelper(info, walker.GetNextIP(), walker.GetSkipIP(), fCallingIntoFunclet, false))
                {
                    return true;
                }

            }

            LOG((LF_CORDB, LL_INFO10000, "DS::TS: WALK_CALL Calling GetSkipIP\n"));
            if (walker.GetSkipIP() == NULL)
            {
                AddBindAndActivateNativeManagedPatch(info->m_activeFrame.md,
                         ji,
                         offset,
                         info->GetReturnFrame().fp,
                         NULL);

                LOG((LF_CORDB,LL_INFO10000,"DS 0x%p m_reason=STEP_CALL4\n",this));
                m_reason = STEP_CALL;

                return true;
            }

            walker.Skip();
            LOG((LF_CORDB, LL_INFO10000, "DS::TS: skipping over call.\n"));
            break;

        default:
            if (walker.GetNextIP() == NULL)
            {
                AddBindAndActivateNativeManagedPatch(info->m_activeFrame.md,
                    ji,
                    offset,
                    info->GetReturnFrame().fp,
                    NULL);
                return true;
            }
            walker.Next();
            break;
        }
    }
    LOG((LF_CORDB,LL_INFO1000,"Ending TrapStep\n"));
}

bool DebuggerStepper::IsAddrWithinFrame(DebuggerJitInfo *dji,
                                        MethodDesc* pMD,
                                        const BYTE* currentAddr,
                                        const BYTE* targetAddr)
{
    _ASSERTE(dji != NULL);

    bool result = IsAddrWithinMethodIncludingFunclet(dji, pMD, targetAddr);

    // We need to check if this is a recursive call.  In RTM we should see if this method is really necessary,
    // since it looks like the X86 JIT doesn't emit intra-method jumps anymore.
    if (result)
    {
        if ((CORDB_ADDRESS)(SIZE_T)targetAddr == dji->m_addrOfCode)
        {
            result = false;
        }
    }

#if defined(FEATURE_EH_FUNCLETS)
    // On WIN64, we also check whether the targetAddr and the currentAddr is in the same funclet.
    _ASSERTE(currentAddr != NULL);
    if (result)
    {
        int currentFuncletIndex = dji->GetFuncletIndex((CORDB_ADDRESS)currentAddr, DebuggerJitInfo::GFIM_BYADDRESS);
        int targetFuncletIndex  = dji->GetFuncletIndex((CORDB_ADDRESS)targetAddr,  DebuggerJitInfo::GFIM_BYADDRESS);
        result = (currentFuncletIndex == targetFuncletIndex);
    }
#endif // FEATURE_EH_FUNCLETS

    return result;
}

// x86 shouldn't need to call this method directly.  We should call IsAddrWithinFrame() on x86 instead.
// That's why I use a name with the word "funclet" in it to scare people off.
bool DebuggerStepper::IsAddrWithinMethodIncludingFunclet(DebuggerJitInfo *dji,
                                                         MethodDesc* pMD,
                                                         const BYTE* targetAddr)
{
    _ASSERTE(dji != NULL);
    return CodeRegionInfo::GetCodeRegionInfo(dji, pMD).IsMethodAddress(targetAddr);
}

void DebuggerStepper::TrapStepNext(ControllerStackInfo *info)
{
    LOG((LF_CORDB, LL_INFO10000, "DS::TrapStepNext, this=%p\n", this));
    // StepNext for a Normal stepper is just a step-out
    TrapStepOut(info);

    // @todo -should we also EnableTraceCall??
}

// Is this frame interesting?
// For a traditional stepper, all frames are interesting.
bool DebuggerStepper::IsInterestingFrame(FrameInfo * pFrame)
{
    LIMITED_METHOD_CONTRACT;

    return true;
}

// Place a single patch somewhere up the stack to do a step-out
void DebuggerStepper::TrapStepOut(ControllerStackInfo *info, bool fForceTraditional)
{
    ControllerStackInfo returnInfo;
    DebuggerJitInfo *dji;

    LOG((LF_CORDB, LL_INFO10000, "DS::TSO this:0x%p\n", this));

    bool fReturningFromFinallyFunclet = false;

#if defined(FEATURE_EH_FUNCLETS)
    // When we step out of a funclet, we should do one of two things, depending
    // on the original stepping intention:
    // 1) If we originally want to step out, then we should skip the parent method.
    // 2) If we originally want to step in/over but we step off the end of the funclet,
    //    then we should resume in the parent, if possible.
    if (info->m_activeFrame.IsNonFilterFuncletFrame())
    {
        // There should always be a frame for the parent method.
        _ASSERTE(info->HasReturnFrame());

#ifdef TARGET_ARM
        while (info->HasReturnFrame() && info->m_activeFrame.md != info->GetReturnFrame().md)
        {
            StackTraceTicket ticket(info);
            returnInfo.GetStackInfo(ticket, GetThread(), info->GetReturnFrame().fp, NULL);
            info = &returnInfo;
        }

        _ASSERTE(info->HasReturnFrame());
#endif

        _ASSERTE(info->m_activeFrame.md == info->GetReturnFrame().md);

        if (m_eMode == cStepOut)
        {
            StackTraceTicket ticket(info);
            returnInfo.GetStackInfo(ticket, GetThread(), info->GetReturnFrame().fp, NULL);
            info = &returnInfo;
        }
        else
        {
            _ASSERTE(info->GetReturnFrame().managed);
            _ASSERTE(info->GetReturnFrame().frame == NULL);

            MethodDesc *md = info->GetReturnFrame().md;
            dji = info->GetReturnFrame().GetJitInfoFromFrame();

            // The return value of a catch funclet is the control PC to resume to.
            // The return value of a finally funclet has no meaning, so we need to check
            // if the return value is in the main method.
            LPVOID resumePC = GetRegdisplayReturnValue(&(info->m_activeFrame.registers));

            // For finally funclet, there are two possible situations.  Either the finally is
            // called normally (i.e. no exception), in which case we simply fall through and
            // let the normal loop do its work below, or the finally is called by the EH
            // routines, in which case we need the unwind notification.
            if (IsAddrWithinMethodIncludingFunclet(dji, md, (const BYTE *)resumePC))
            {
                SIZE_T reloffset = dji->m_codeRegionInfo.AddressToOffset((BYTE*)resumePC);

                AddBindAndActivateNativeManagedPatch(info->GetReturnFrame().md,
                    dji,
                    reloffset,
                    info->GetReturnFrame().fp,
                    NULL);

                LOG((LF_CORDB, LL_INFO10000,
                     "DS::TSO:normally managed code AddPatch"
                     " in %s::%s, offset 0x%x, m_reason=%d\n",
                     info->GetReturnFrame().md->m_pszDebugClassName,
                     info->GetReturnFrame().md->m_pszDebugMethodName,
                     reloffset, m_reason));

                // Do not set m_reason to STEP_RETURN here.  Logically, the funclet and the parent method are the
                // same method, so we should not "return" to the parent method.
                LOG((LF_CORDB, LL_INFO10000,"DS::TSO: done\n"));

                return;
            }
            else
            {
                // This is the case where we step off the end of a finally funclet.
                fReturningFromFinallyFunclet = true;
            }
        }
    }
#endif // FEATURE_EH_FUNCLETS

#ifdef _DEBUG
    FramePointer dbgLastFP; // for debug, make sure we're making progress through the stack.
#endif

    while (info->HasReturnFrame())
    {

#ifdef _DEBUG
        dbgLastFP = info->m_activeFrame.fp;
#endif

        // Continue walking up the stack & set a patch upon the next
        // frame up.  We will eventually either hit managed code
        // (which we can set a definite patch in), or the top of the
        // stack.
        StackTraceTicket ticket(info);

        // The last parameter here is part of a really targetted (*cough* dirty) fix to
        // disable getting an unwanted UMChain to fix issue 650903 (See
        // code:ControllerStackInfo::WalkStack and code:TrackUMChain for the other
        // parts.) In the case of managed step out we know that we aren't interested in
        // unmanaged frames, and generating that unmanaged frame causes the stackwalker
        // not to report the managed frame that was at the same SP. However the unmanaged
        // frame might be used in the mixed-mode step out case so I don't suppress it
        // there.
        returnInfo.GetStackInfo(ticket, GetThread(), info->GetReturnFrame().fp, NULL, !(m_rgfMappingStop & STOP_UNMANAGED));
        info = &returnInfo;

#ifdef _DEBUG
        // If this assert fires, then it means that we're not making progress while
        // tracing up the towards the root of the stack. Likely an issue in the Left-Side's
        // stackwalker.
        _ASSERTE(IsCloserToLeaf(dbgLastFP, info->m_activeFrame.fp));
#endif

#ifdef FEATURE_MULTICASTSTUB_AS_IL
        if (info->m_activeFrame.md != nullptr && info->m_activeFrame.md->IsILStub() && info->m_activeFrame.md->AsDynamicMethodDesc()->IsMulticastStub())
        {
            LOG((LF_CORDB, LL_INFO10000,
                 "DS::TSO: multicast frame.\n"));

            // User break should always be called from managed code, so it should never actually hit this codepath.
            _ASSERTE(GetDCType() != DEBUGGER_CONTROLLER_USER_BREAKPOINT);

            // JMC steppers shouldn't be patching stubs.
            if (DEBUGGER_CONTROLLER_JMC_STEPPER == this->GetDCType())
            {
                LOG((LF_CORDB, LL_INFO10000, "DS::TSO: JMC stepper skipping frame.\n"));
                continue;
            }

            TraceDestination trace;

            EnableTraceCall(info->m_activeFrame.fp);

            PCODE ip = GetControlPC(&(info->m_activeFrame.registers));
            if (g_pEEInterface->TraceStub((BYTE*)ip, &trace)
                && g_pEEInterface->FollowTrace(&trace)
                && PatchTrace(&trace, info->m_activeFrame.fp,
                              true))
                break;
        }
        else
#endif // FEATURE_MULTICASTSTUB_AS_IL
        if (info->m_activeFrame.md != nullptr && info->m_activeFrame.md->IsILStub() &&
            info->m_activeFrame.md->AsDynamicMethodDesc()->GetILStubType() == DynamicMethodDesc::StubTailCallCallTarget)
        {
            // Normally the stack trace would not include IL stubs, but we
            // include this specific IL stub so that we can check if a call into
            // the tailcall dispatcher will result in any user code being
            // executed or will return and allow a previous tailcall dispatcher
            // to deal with the tailcall. Thus we just skip that frame here.
            LOG((LF_CORDB, LL_INFO10000,
                 "DS::TSO: CallTailCallTarget frame.\n"));
            continue;
        }
        else if (info->m_activeFrame.managed)
        {
            LOG((LF_CORDB, LL_INFO10000,
                 "DS::TSO: return frame is managed.\n"));

            if (info->m_activeFrame.frame == NULL)
            {
                // Returning normally to managed code.
                _ASSERTE(info->m_activeFrame.md != NULL);

                // Polymorphic check to skip over non-interesting frames.
                if (!fForceTraditional && !this->IsInterestingFrame(&info->m_activeFrame))
                    continue;

                dji = info->m_activeFrame.GetJitInfoFromFrame();
                _ASSERTE(dji != NULL);

                // Note: we used to pass in the IP from the active frame to GetJitInfo, but there seems to be no value
                // in that, and it was causing problems creating a stepper while sitting in ndirect stubs after we'd
                // returned from the unmanaged function that had been called.
                ULONG reloffset = info->m_activeFrame.relOffset;

                AddBindAndActivateNativeManagedPatch(info->m_activeFrame.md,
                    dji,
                    reloffset,
                    info->GetReturnFrame().fp,
                    NULL);

                LOG((LF_CORDB, LL_INFO10000,
                     "DS::TSO:normally managed code AddPatch"
                     " in %s::%s, offset 0x%x, m_reason=%d\n",
                     info->m_activeFrame.md->m_pszDebugClassName,
                     info->m_activeFrame.md->m_pszDebugMethodName,
                     reloffset, m_reason));


                // Do not set m_reason to STEP_RETURN here.  Logically, the funclet and the parent method are the
                // same method, so we should not "return" to the parent method.
                if (!fReturningFromFinallyFunclet)
                {
                    m_reason = STEP_RETURN;
                }
                break;
            }
            else if (info->m_activeFrame.frame == FRAME_TOP)
            {

                // Trad-stepper's step-out is actually like a step-next when we go off the top.
                // JMC-steppers do a true-step out. So for JMC-steppers, don't enable trace-call.
                if (DEBUGGER_CONTROLLER_JMC_STEPPER == this->GetDCType())
                {
                    LOG((LF_CORDB, LL_EVERYTHING, "DS::TSO: JMC stepper skipping exit-frame case.\n"));
                    break;
                }

                // User break should always be called from managed code, so it should never actually hit this codepath.
                _ASSERTE(GetDCType() != DEBUGGER_CONTROLLER_USER_BREAKPOINT);


                // We're walking off the top of the stack. Note that if we call managed code again,
                // this trace-call will cause us our stepper-to fire. So we'll actually do a
                // step-next; not a true-step out.
                EnableTraceCall(info->m_activeFrame.fp);

                LOG((LF_CORDB, LL_INFO1000, "DS::TSO: Off top of frame!\n"));

                m_reason = STEP_EXIT; //we're on the way out..

                // <REVISIT_TODO>@todo not that it matters since we don't send a
                // stepComplete message to the right side.</REVISIT_TODO>
                break;
            }
            else if (info->m_activeFrame.frame->GetFrameType() == Frame::TYPE_FUNC_EVAL)
            {
                // Note: we treat walking off the top of the stack and
                // walking off the top of a func eval the same way,
                // except that we don't enable trace call since we
                // know exactly where were going.

                LOG((LF_CORDB, LL_INFO1000,
                     "DS::TSO: Off top of func eval!\n"));

                m_reason = STEP_EXIT;
                break;
            }
            else if (info->m_activeFrame.frame->GetFrameType() == Frame::TYPE_SECURITY &&
                     info->m_activeFrame.frame->GetInterception() == Frame::INTERCEPTION_NONE)
            {
                // If we're stepping out of something that was protected by (declarative) security,
                // the security subsystem may leave a frame on the stack to cache it's computation.
                // HOWEVER, this isn't a real frame, and so we don't want to stop here.  On the other
                // hand, if we're in the security goop (sec. executes managed code to do stuff), then
                // we'll want to use the "returning to stub case", below.  GetInterception()==NONE
                // indicates that the frame is just a cache frame:
                // Skip it and keep on going

                LOG((LF_CORDB, LL_INFO10000,
                     "DS::TSO: returning to a non-intercepting frame. Keep unwinding\n"));
                continue;
            }
            else
            {
                LOG((LF_CORDB, LL_INFO10000,
                     "DS::TSO: returning to a stub frame.\n"));

                // User break should always be called from managed code, so it should never actually hit this codepath.
                _ASSERTE(GetDCType() != DEBUGGER_CONTROLLER_USER_BREAKPOINT);

                // JMC steppers shouldn't be patching stubs.
                if (DEBUGGER_CONTROLLER_JMC_STEPPER == this->GetDCType())
                {
                    LOG((LF_CORDB, LL_INFO10000, "DS::TSO: JMC stepper skipping frame.\n"));
                    continue;
                }

                // We're returning to some funky frame.
                // (E.g. a security frame has called a native method.)

                // Patch the frame from entering other methods. This effectively gives the Step-out
                // a step-next behavior. For eg, this can be useful for step-out going between multicast delegates.
                // This step-next could actually land us leaf-more on the callstack than we currently are!
                // If we were a true-step out, we'd skip this and keep crawling.
                // up the callstack.
                //
                // !!! For now, we assume that the TraceFrame entry
                // point is smart enough to tell where it is in the
                // calling sequence.  We'll see how this holds up.
                TraceDestination trace;

                // We don't want notifications of trace-calls leaf-more than our current frame.
                // For eg, if our current frame calls out to unmanaged code and then back in,
                // we'll get a TraceCall notification. But since it's leaf-more than our current frame,
                // we don't care because we just want to step out of our current frame (and everything
                // our current frame may call).
                EnableTraceCall(info->m_activeFrame.fp);

                CONTRACT_VIOLATION(GCViolation); // TraceFrame GC-triggers

                if (g_pEEInterface->TraceFrame(GetThread(),
                                               info->m_activeFrame.frame, FALSE,
                                               &trace, &(info->m_activeFrame.registers))
                    && g_pEEInterface->FollowTrace(&trace)
                    && PatchTrace(&trace, info->m_activeFrame.fp,
                                  true))
                    break;

                // !!! Problem: we don't know which return frame to use -
                // the TraceFrame patch may be in a frame below the return
                // frame, or in a frame parallel with it
                // (e.g. prestub popping itself & then calling.)
                //
                // For now, I've tweaked the FP comparison in the
                // patch dispatching code to allow either case.
            }
        }
        else
        {
            LOG((LF_CORDB, LL_INFO10000,
                 "DS::TSO: return frame is not managed.\n"));

            // Only step out to unmanaged code if we're actually
            // marked to stop in unamanged code. Otherwise, just loop
            // to get us past the unmanaged frames.
            if (m_rgfMappingStop & STOP_UNMANAGED)
            {
                LOG((LF_CORDB, LL_INFO10000,
                     "DS::TSO: return to unmanaged code "
                     "m_reason=STEP_RETURN\n"));

                // Do not set m_reason to STEP_RETURN here.  Logically, the funclet and the parent method are the
                // same method, so we should not "return" to the parent method.
                if (!fReturningFromFinallyFunclet)
                {
                    m_reason = STEP_RETURN;
                }

                // We're stepping out into unmanaged code
                LOG((LF_CORDB, LL_INFO10000,
                 "DS::TSO: Setting unmanaged trace patch at 0x%x(%x)\n",
                     GetControlPC(&(info->m_activeFrame.registers)),
                     info->GetReturnFrame().fp.GetSPValue()));

                AddAndActivateNativePatchForAddress((CORDB_ADDRESS_TYPE *)GetControlPC(&(info->m_activeFrame.registers)),
                         info->GetReturnFrame().fp,
                         FALSE,
                         TRACE_UNMANAGED);

                break;

            }
        }
    }

    // <REVISIT_TODO>If we get here, we may be stepping out of the last frame.  Our thread
    // exit logic should catch this case. (@todo)</REVISIT_TODO>
    LOG((LF_CORDB, LL_INFO10000,"DS::TSO: done\n"));
}


// void DebuggerStepper::StepOut()
// Called by Debugger::HandleIPCEvent  to setup
// everything so that the process will step over the range of IL
// correctly.
// How: Converts the provided array of ranges from IL ranges to
// native ranges (if they're not native already), and then calls
// TrapStep or TrapStepOut, like so:
//   Get the appropriate MethodDesc & JitInfo
//   Iterate through array of IL ranges, use
//   JitInfo::MapILRangeToMapEntryRange to translate IL to native
//   ranges.
// Set member variables to remember that the DebuggerStepper now uses
// the ranges: m_range, m_rangeCount, m_stepIn, m_fp
// If (!TrapStep()) then {m_stepIn = true; TrapStepOut()}
// EnableUnwind( m_fp );
void DebuggerStepper::StepOut(FramePointer fp, StackTraceTicket ticket)
{
    LOG((LF_CORDB, LL_INFO10000, "Attempting to step out, fp:0x%x this:0x%x"
        "\n", fp.GetSPValue(), this ));

    Thread *thread = GetThread();
    CONTEXT *context = g_pEEInterface->GetThreadFilterContext(thread);
    ControllerStackInfo info;

    // We pass in the ticket b/c this is called both when we're live (via
    // DebuggerUserBreakpoint) and when we're stopped (via normal StepOut)
    info.GetStackInfo(ticket, thread, fp, context);


    ResetRange();


    m_stepIn = FALSE;
    m_fp = info.m_activeFrame.fp;
#if defined(FEATURE_EH_FUNCLETS)
    // We need to remember the parent method frame pointer here so that we will recognize
    // the range of the stepper as being valid when we return to the parent method or stackalloc.
    if (info.HasReturnFrame(true))
    {
        m_fpParentMethod = info.GetReturnFrame(true).fp;
    }
#endif // FEATURE_EH_FUNCLETS

    m_eMode = cStepOut;

    _ASSERTE((fp == LEAF_MOST_FRAME) || (info.m_activeFrame.md != NULL) || (info.GetReturnFrame().md != NULL));

    TrapStepOut(&info);
    EnableUnwind(m_fp);
}

#define GROW_RANGES_IF_NECESSARY()                            \
    if (rTo == rToEnd)                                        \
    {                                                         \
        ULONG NewSize, OldSize;                               \
        if (!ClrSafeInt<ULONG>::multiply(sizeof(COR_DEBUG_STEP_RANGE), (ULONG)(realRangeCount*2), NewSize) || \
            !ClrSafeInt<ULONG>::multiply(sizeof(COR_DEBUG_STEP_RANGE), (ULONG)realRangeCount, OldSize) || \
            NewSize < OldSize)                                \
        {                                                     \
            DeleteInteropSafe(m_range);                       \
            m_range = NULL;                                   \
            return false;                                     \
        }                                                     \
        COR_DEBUG_STEP_RANGE *_pTmp = (COR_DEBUG_STEP_RANGE*) \
            g_pDebugger->GetInteropSafeHeap()->Realloc(m_range, \
                                         NewSize,             \
                                         OldSize);            \
                                                              \
        if (_pTmp == NULL)                                    \
        {                                                     \
            DeleteInteropSafe(m_range);                       \
            m_range = NULL;                                   \
            return false;                                     \
        }                                                     \
                                                              \
        m_range = _pTmp;                                      \
        rTo     = m_range + realRangeCount;                   \
        rToEnd  = m_range + (realRangeCount*2);               \
        realRangeCount *= 2;                                  \
    }

//-----------------------------------------------------------------------------
//  Given a set of IL ranges, convert them to native and cache them.
// Return true on success, false on error.
//-----------------------------------------------------------------------------
bool DebuggerStepper::SetRangesFromIL(DebuggerJitInfo *dji, COR_DEBUG_STEP_RANGE *ranges, SIZE_T rangeCount)
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        GC_NOTRIGGER;
        PRECONDITION(ThisIsHelperThreadWorker()); // Only help initializes a stepper.
        PRECONDITION(m_range == NULL); // shouldn't be set already.
        PRECONDITION(CheckPointer(ranges));
        PRECONDITION(CheckPointer(dji));
    }
    CONTRACTL_END;

    // Note: we used to pass in the IP from the active frame to GetJitInfo, but there seems to be no value in that, and
    // it was causing problems creating a stepper while sitting in ndirect stubs after we'd returned from the unmanaged
    // function that had been called.
    MethodDesc *fd = dji->m_nativeCodeVersion.GetMethodDesc();

    // The "+1" is for internal use, when we need to
    // set an intermediate patch in pitched code.  Isn't
    // used unless the method is pitched & a patch is set
    // inside it.  Thus we still pass cRanges as the
    // range count.
    m_range = new (interopsafe) COR_DEBUG_STEP_RANGE[rangeCount+1];

    if (m_range == NULL)
        return false;

    TRACE_ALLOC(m_range);

    SIZE_T realRangeCount = rangeCount;

    if (dji != NULL)
    {
        LOG((LF_CORDB,LL_INFO10000,"DeSt::St: For code md=0x%p, got DJI 0x%p, from 0x%p to 0x%p\n",
            fd,
            dji, dji->m_addrOfCode, (ULONG)dji->m_addrOfCode
            + (ULONG)dji->m_sizeOfCode));

        //
        // Map ranges to native offsets for jitted code
        //
        COR_DEBUG_STEP_RANGE *r, *rEnd, *rTo, *rToEnd;

        r = ranges;
        rEnd = r + rangeCount;

        rTo = m_range;
        rToEnd  = rTo + realRangeCount;

        // <NOTE>
        // rTo may also be incremented in the middle of the loop on WIN64 platforms.
        // </NOTE>
        for (/**/; r < rEnd; r++, rTo++)
        {
            // If we are already at the end of our allocated array, but there are still
            // more ranges to copy over, then grow the array.
            GROW_RANGES_IF_NECESSARY();

            if (r->startOffset == 0 && r->endOffset == (ULONG) ~0)
            {
                // {0...-1} means use the entire method as the range
                // Code dup'd from below case.
                LOG((LF_CORDB, LL_INFO10000, "DS:Step: Have DJI, special (0,-1) entry\n"));
                rTo->startOffset = 0;
                rTo->endOffset   = (ULONG32)g_pEEInterface->GetFunctionSize(fd);
            }
            else
            {
                //
                // One IL range may consist of multiple
                // native ranges.
                //

                DebuggerILToNativeMap *mStart, *mEnd;

                dji->MapILRangeToMapEntryRange(r->startOffset,
                                                r->endOffset,
                                                &mStart,
                                                &mEnd);

                // Either mStart and mEnd are both NULL (we don't have any sequence point),
                // or they are both non-NULL.
                _ASSERTE( ((mStart == NULL) && (mEnd == NULL)) ||
                          ((mStart != NULL) && (mEnd != NULL)) );

                if (mStart == NULL)
                {
                    // <REVISIT_TODO>@todo Won't this result in us stepping across
                    // the entire method?</REVISIT_TODO>
                    rTo->startOffset = 0;
                    rTo->endOffset   = 0;
                }
                else if (mStart == mEnd)
                {
                    rTo->startOffset = mStart->nativeStartOffset;
                    rTo->endOffset   = mStart->nativeEndOffset;
                }
                else
                {
                    // Account for more than one continuous range here.

                    // Move the pointer back to work with the loop increment below.
                    // Don't dereference this pointer now!
                    rTo--;

                    for (DebuggerILToNativeMap* pMap = mStart;
                         pMap <= mEnd;
                         pMap = pMap + 1)
                    {
                        if ((pMap == mStart) ||
                            (pMap->nativeStartOffset != (pMap-1)->nativeEndOffset))
                        {
                            rTo++;
                            GROW_RANGES_IF_NECESSARY();

                            rTo->startOffset = pMap->nativeStartOffset;
                            rTo->endOffset   = pMap->nativeEndOffset;
                        }
                        else
                        {
                            // If we have continuous ranges, then lump them together.
                            _ASSERTE(rTo->endOffset == pMap->nativeStartOffset);
                            rTo->endOffset = pMap->nativeEndOffset;
                        }
                    }

                    LOG((LF_CORDB, LL_INFO10000, "DS:Step: nat off:0x%x to 0x%x\n", rTo->startOffset, rTo->endOffset));
                }
            }
        }

        rangeCount = (int)((BYTE*)rTo - (BYTE*)m_range) / sizeof(COR_DEBUG_STEP_RANGE);
    }
    else
    {
        // Even if we don't have debug info, we'll be able to
        // step through the method
        SIZE_T functionSize = g_pEEInterface->GetFunctionSize(fd);

        COR_DEBUG_STEP_RANGE *r    = ranges;
        COR_DEBUG_STEP_RANGE *rEnd = r + rangeCount;

        COR_DEBUG_STEP_RANGE *rTo  = m_range;

        for(/**/; r < rEnd; r++, rTo++)
        {
            if (r->startOffset == 0 && r->endOffset == (ULONG) ~0)
            {
                LOG((LF_CORDB, LL_INFO10000, "DS:Step:No DJI, (0,-1) special entry\n"));
                // Code dup'd from above case.
                // {0...-1} means use the entire method as the range
                rTo->startOffset = 0;
                rTo->endOffset   = (ULONG32)functionSize;
            }
            else
            {
                LOG((LF_CORDB, LL_INFO10000, "DS:Step:No DJI, regular entry\n"));
                // We can't just leave ths IL entry - we have to
                // get rid of it.
                // This will just be ignored
                rTo->startOffset = rTo->endOffset = (ULONG32)functionSize;
            }
        }
    }


    m_rangeCount = rangeCount;
    m_realRangeCount = rangeCount;

    return true;
}


// void DebuggerStepper::Step()  Tells the stepper to step over
// the provided ranges.
// void *fp:  frame pointer.
// bool in:  true if we want to step into a function within the range,
//        false if we want to step over functions within the range.
// COR_DEBUG_STEP_RANGE *ranges:  Assumed to be nonNULL, it will
//        always hold at least one element.
// SIZE_T rangeCount:  One less than the true number of elements in
//        the ranges argument.
// bool rangeIL:  true if the ranges are provided in IL (they'll be
//        converted to native before the DebuggerStepper uses them,
//      false if they already are native.
bool DebuggerStepper::Step(FramePointer fp, bool in,
                           COR_DEBUG_STEP_RANGE *ranges, SIZE_T rangeCount,
                           bool rangeIL)
{
    LOG((LF_CORDB, LL_INFO1000, "DeSt:Step this:0x%x  ", this));
    if (rangeCount>0)
        LOG((LF_CORDB,LL_INFO10000," start,end[0]:(0x%x,0x%x)\n",
             ranges[0].startOffset, ranges[0].endOffset));
    else
        LOG((LF_CORDB,LL_INFO10000," single step\n"));

    Thread *thread = GetThread();
    CONTEXT *context = g_pEEInterface->GetThreadFilterContext(thread);

    // ControllerStackInfo doesn't report IL stubs, so if we are in an IL stub, we need
    // to handle the single-step specially.  There are probably other problems when we stop
    // in an IL stub.  We need to revisit this later.
    bool fIsILStub = false;
    if ((context != NULL) &&
        g_pEEInterface->IsManagedNativeCode(reinterpret_cast<const BYTE *>(GetIP(context))))
    {
        MethodDesc * pMD = g_pEEInterface->GetNativeCodeMethodDesc(GetIP(context));
        if (pMD != NULL)
        {
            fIsILStub = pMD->IsILStub();
        }
    }
    LOG((LF_CORDB, LL_INFO10000, "DS::S - fIsILStub = %d\n", fIsILStub));

    ControllerStackInfo info;


    StackTraceTicket ticket(thread);
    info.GetStackInfo(ticket, thread, fp, context);

    _ASSERTE((fp == LEAF_MOST_FRAME) || (info.m_activeFrame.md != NULL) ||
             (info.GetReturnFrame().md != NULL));

    m_stepIn = in;

    DebuggerJitInfo *dji = info.m_activeFrame.GetJitInfoFromFrame();

    if (dji == NULL)
    {
        // !!! ERROR range step in frame with no code
        ranges = NULL;
        rangeCount = 0;
    }


    if (m_range != NULL)
    {
        TRACE_FREE(m_range);
        DeleteInteropSafe(m_range);
        m_range = NULL;
        m_rangeCount = 0;
        m_realRangeCount = 0;
    }

    if (rangeCount > 0)
    {
        if (rangeIL)
        {
            // IL ranges supplied, we need to convert them to native ranges.
            bool fOk = SetRangesFromIL(dji, ranges, rangeCount);
            if (!fOk)
            {
                return false;
            }
        }
        else
        {
            // Native ranges, already supplied. Just copy them over.
            m_range = new (interopsafe) COR_DEBUG_STEP_RANGE[rangeCount];

            if (m_range == NULL)
            {
                return false;
            }

            memcpy(m_range, ranges, sizeof(COR_DEBUG_STEP_RANGE) * rangeCount);
            m_realRangeCount  = m_rangeCount = rangeCount;
        }
        _ASSERTE(m_range != NULL);
        _ASSERTE(m_rangeCount > 0);
        _ASSERTE(m_realRangeCount > 0);
    }
    else
    {
        // !!! ERROR cannot map IL ranges
        ranges = NULL;
        rangeCount = 0;
    }

    if (fIsILStub)
    {
        // Don't use the ControllerStackInfo if we are in an IL stub.
        m_fp = fp;
    }
    else
    {
        m_fp = info.m_activeFrame.fp;
#if defined(FEATURE_EH_FUNCLETS)
        // We need to remember the parent method frame pointer here so that we will recognize
        // the range of the stepper as being valid when we return to the parent method or stackalloc.
        if (info.HasReturnFrame(true))
        {
            m_fpParentMethod = info.GetReturnFrame(true).fp;
        }
#endif // FEATURE_EH_FUNCLETS
    }
    m_eMode = m_stepIn ? cStepIn : cStepOver;

    LOG((LF_CORDB,LL_INFO10000,"DS 0x%p Step: STEP_NORMAL\n",this));
    m_reason = STEP_NORMAL; //assume it'll be a normal step & set it to
    //something else if we walk over it
    if (fIsILStub)
    {
        LOG((LF_CORDB, LL_INFO10000, "DS:Step: stepping in an IL stub\n"));

        // Enable the right triggers if the user wants to step in.
        if (in)
        {
            if (this->GetDCType() == DEBUGGER_CONTROLLER_STEPPER)
            {
                EnableTraceCall(info.m_activeFrame.fp);
            }
            else if (this->GetDCType() == DEBUGGER_CONTROLLER_JMC_STEPPER)
            {
                EnableMethodEnter();
            }
        }

        // Also perform a step-out in case this IL stub is returning to managed code.
        // However, we must fix up the ControllerStackInfo first, since it doesn't
        // report IL stubs.  The active frame reported by the ControllerStackInfo is
        // actually the return frame in this case.
        info.SetReturnFrameWithActiveFrame();
        TrapStepOut(&info);
    }
    else if (!TrapStep(&info, in))
    {
        LOG((LF_CORDB,LL_INFO10000,"DS:Step: Did TS\n"));
        m_stepIn = true;
        TrapStepNext(&info);
    }

    LOG((LF_CORDB,LL_INFO10000,"DS:Step: Did TS,TSO\n"));

    EnableUnwind(m_fp);

    return true;
}

// TP_RESULT DebuggerStepper::TriggerPatch()
// What: Triggers patch if we're not in a stub, and we're
// outside of the stepping range.  Otherwise sets another patch so as to
// step out of the stub, or in the next instruction within the range.
// How: If module==NULL & managed==> we're in a stub:
// TrapStepOut() and return false.  Module==NULL&!managed==> return
// true.  If m_range != NULL & execution is currently in the range,
// attempt a TrapStep (TrapStepOut otherwise) & return false.  Otherwise,
// return true.
TP_RESULT DebuggerStepper::TriggerPatch(DebuggerControllerPatch *patch,
                                   Thread *thread,
                                   TRIGGER_WHY tyWhy)
{
    LOG((LF_CORDB, LL_INFO10000, "DS::TP\n"));

    // If we're frozen, we may hit a patch but we just ignore it
    if (IsFrozen())
    {
        LOG((LF_CORDB, LL_INFO1000000, "DS::TP, ignoring patch at %p during frozen state\n", patch->address));
        return TPR_IGNORE;
    }

    Module *module = patch->key.module;
    BOOL managed = patch->IsManagedPatch();
    mdMethodDef md = patch->key.md;
    SIZE_T offset = patch->offset;

    _ASSERTE((this->GetThread() == thread) || !"Stepper should only get patches on its thread");

    // Note we can only run a stack trace if:
    // - the context is in managed code (eg, not a stub)
    // - OR we have a frame in place to prime the stackwalk.
    ControllerStackInfo info;
    CONTEXT *context = g_pEEInterface->GetThreadFilterContext(thread);

    _ASSERTE(!ISREDIRECTEDTHREAD(thread));

    // Context should always be from patch.
    _ASSERTE(context != NULL);

    bool fSafeToDoStackTrace = true;

    // If we're in a stub (module == NULL and still in managed code), then our context is off in lala-land
    // Then, it's only safe to do a stackwalk if the top frame is protecting us. That's only true for a
    // frame_push. If we're here on a manager_push, then we don't have any such protection, so don't do the
    // stackwalk.

    fSafeToDoStackTrace = patch->IsSafeForStackTrace();


    if (fSafeToDoStackTrace)
    {
        StackTraceTicket ticket(patch);
        info.GetStackInfo(ticket, thread, LEAF_MOST_FRAME, context);

        LOG((LF_CORDB, LL_INFO10000, "DS::TP: this:0x%p in %s::%s (fp:0x%p, "
            "off:0x%p md:0x%p), \n\texception source:%s::%s (fp:0x%p)\n",
            this,
            info.m_activeFrame.md!=NULL?info.m_activeFrame.md->m_pszDebugClassName:"Unknown",
            info.m_activeFrame.md!=NULL?info.m_activeFrame.md->m_pszDebugMethodName:"Unknown",
            info.m_activeFrame.fp.GetSPValue(), patch->offset, patch->key.md,
            m_fdException!=NULL?m_fdException->m_pszDebugClassName:"None",
            m_fdException!=NULL?m_fdException->m_pszDebugMethodName:"None",
             m_fpException.GetSPValue()));
    }

    DisableAll();

    if (DetectHandleLCGMethods(dac_cast<PCODE>(patch->address), NULL, &info))
    {
        return TPR_IGNORE;
    }

    if (module == NULL)
    {
        // JMC steppers should not be patching here...
        _ASSERTE(DEBUGGER_CONTROLLER_JMC_STEPPER != this->GetDCType());

        if (managed)
        {

            LOG((LF_CORDB, LL_INFO10000,
                 "Frame (stub) patch hit at offset 0x%x\n", offset));

            // This is a stub patch. If it was a TRACE_FRAME_PUSH that
            // got us here, then the stub's frame is pushed now, so we
            // tell the frame to apply the real patch. If we got here
            // via a TRACE_MGR_PUSH, however, then there is no frame
            // and we tell the stub manager that generated the
            // TRACE_MGR_PUSH to apply the real patch.
            TraceDestination trace;
            bool traceOk;
            FramePointer     frameFP;
            PTR_BYTE traceManagerRetAddr = NULL;

            if (patch->trace.GetTraceType() == TRACE_MGR_PUSH)
            {
                _ASSERTE(context != NULL);
                CONTRACT_VIOLATION(GCViolation);
                traceOk = g_pEEInterface->TraceManager(
                                                 thread,
                                                 patch->trace.GetStubManager(),
                                                 &trace,
                                                 context,
                                                 &traceManagerRetAddr);

                // We don't hae an active frame here, so patch with a
                // FP of NULL so anything will match.
                //
                // <REVISIT_TODO>@todo: should we take Esp out of the context?</REVISIT_TODO>
                frameFP = LEAF_MOST_FRAME;
            }
            else
            {
                _ASSERTE(fSafeToDoStackTrace);
                CONTRACT_VIOLATION(GCViolation); // TraceFrame GC-triggers
                traceOk = g_pEEInterface->TraceFrame(thread,
                                                     thread->GetFrame(),
                                                     TRUE,
                                                     &trace,
                                                     &(info.m_activeFrame.registers));

                frameFP = info.m_activeFrame.fp;
            }

            // Enable the JMC backstop for traditional steppers to catch us in case
            // we didn't predict the call target properly.
            EnableJMCBackStop(NULL);


            if (!traceOk
                || !g_pEEInterface->FollowTrace(&trace)
                || !PatchTrace(&trace, frameFP,
                               (m_rgfMappingStop&STOP_UNMANAGED)?
                                    (true):(false)))
            {
                //
                // We can't set a patch in the frame -- we need
                // to trap returning from this frame instead.
                //
                // Note: if we're in the TRACE_MGR_PUSH case from
                // above, then we must place a patch where the
                // TraceManager function told us to, since we can't
                // actually unwind from here.
                //
                if (patch->trace.GetTraceType() != TRACE_MGR_PUSH)
                {
                    _ASSERTE(fSafeToDoStackTrace);
                    LOG((LF_CORDB,LL_INFO10000,"TSO for non TRACE_MGR_PUSH case\n"));
                    TrapStepOut(&info);
                }
                else
                {
                    LOG((LF_CORDB, LL_INFO10000,
                         "TSO for TRACE_MGR_PUSH case. RetAddr: 0x%p\n", traceManagerRetAddr));

                    // We'd better have a valid return address.
                    _ASSERTE(traceManagerRetAddr != NULL);

                    if (g_pEEInterface->IsManagedNativeCode(traceManagerRetAddr))
                    {
                        // Grab the jit info for the method.
                        DebuggerJitInfo *dji;
                        dji = g_pDebugger->GetJitInfoFromAddr((TADDR) traceManagerRetAddr);

                        MethodDesc* mdNative = NULL;
                        PCODE pcodeNative = NULL;
                        if (dji != NULL)
                        {
                            mdNative = dji->m_nativeCodeVersion.GetMethodDesc();
                            pcodeNative = dji->m_nativeCodeVersion.GetNativeCode();
                        }
                        else
                        {
                            // Find the method that the return is to.
                            mdNative = g_pEEInterface->GetNativeCodeMethodDesc(dac_cast<PCODE>(traceManagerRetAddr));
                            _ASSERTE(g_pEEInterface->GetFunctionAddress(mdNative) != NULL);
                            pcodeNative = g_pEEInterface->GetFunctionAddress(mdNative);
                        }

                        _ASSERTE(mdNative != NULL && pcodeNative != NULL);
                        SIZE_T offsetRet = dac_cast<TADDR>(traceManagerRetAddr - pcodeNative);
                        LOG((LF_CORDB, LL_INFO10000,
                             "DS::TP: Before normally managed code AddPatch"
                             " in %s::%s \n\tmd=0x%p, offset 0x%x, pcode=0x%p, dji=0x%p\n",
                             mdNative->m_pszDebugClassName,
                             mdNative->m_pszDebugMethodName,
                             mdNative,
                             offsetRet,
                             pcodeNative,
                             dji));

                        // Place the patch.
                        AddBindAndActivateNativeManagedPatch(mdNative,
                                 dji,
                                 offsetRet,
                                 LEAF_MOST_FRAME,
                                 NULL);
                    }
                    else
                    {
                        // We're hitting this code path with MC++ assemblies
                        // that have an unmanaged entry point so the stub returns to CallDescrWorker.
                        _ASSERTE(g_pEEInterface->GetNativeCodeMethodDesc(dac_cast<PCODE>(patch->address))->IsILStub());
                    }

                }

                m_reason = STEP_NORMAL; //we tried to do a STEP_CALL, but since it didn't
                //work, we're doing what amounts to a normal step.
                LOG((LF_CORDB,LL_INFO10000,"DS 0x%p m_reason = STEP_NORMAL"
                     "(attempted call thru stub manager, SM didn't know where"
                     " we're going, so did a step out to original call\n",this));
            }
            else
            {
                m_reason = STEP_CALL;
            }

            EnableTraceCall(LEAF_MOST_FRAME);
            EnableUnwind(m_fp);

            return TPR_IGNORE;
        }
        else
        {
            // @todo - when would we hit this codepath?
            // If we're not in managed, then we should have pushed a frame onto the Thread's frame chain,
            // and thus we should still safely be able to do a stackwalk here.
            _ASSERTE(fSafeToDoStackTrace);
            if (DetectHandleInterceptors(&info) )
            {
                return TPR_IGNORE; //don't actually want to stop
            }

            LOG((LF_CORDB, LL_INFO10000,
                 "Unmanaged step patch hit at 0x%x\n", offset));

            StackTraceTicket ticket(patch);
            PrepareForSendEvent(ticket);
            return TPR_TRIGGER;
        }
    } // end (module == NULL)

    // If we're inside an interceptor but don't want to be,then we'll set a
    // patch outside the current function.
    _ASSERTE(fSafeToDoStackTrace);
    if (DetectHandleInterceptors(&info) )
    {
        return TPR_IGNORE; //don't actually want to stop
    }

    LOG((LF_CORDB,LL_INFO10000, "DS: m_fp:0x%p, activeFP:0x%p fpExc:0x%p\n",
        m_fp.GetSPValue(), info.m_activeFrame.fp.GetSPValue(), m_fpException.GetSPValue()));

    if (IsInRange(offset, m_range, m_rangeCount, &info) ||
        ShouldContinueStep( &info, offset))
    {
        LOG((LF_CORDB, LL_INFO10000,
             "Intermediate step patch hit at 0x%x\n", offset));

        if (!TrapStep(&info, m_stepIn))
            TrapStepNext(&info);

        EnableUnwind(m_fp);
        return TPR_IGNORE;
    }
    else
    {
        LOG((LF_CORDB, LL_INFO10000, "Step patch hit at 0x%x\n", offset));

        // For a JMC stepper, we have an additional constraint:
        // skip non-user code. So if we're still in non-user code, then
        // we've got to keep going
        DebuggerMethodInfo * dmi = g_pDebugger->GetOrCreateMethodInfo(module, md);

        if ((dmi != NULL) && DetectHandleNonUserCode(&info, dmi))
        {
            return TPR_IGNORE;
        }

        StackTraceTicket ticket(patch);
        PrepareForSendEvent(ticket);
        return TPR_TRIGGER;
    }
}

// Return true if this should be skipped.
// For a non-jmc stepper, we don't care about non-user code, so we
// don't skip it and so we always return false.
bool DebuggerStepper::DetectHandleNonUserCode(ControllerStackInfo *info, DebuggerMethodInfo * pInfo)
{
    LIMITED_METHOD_CONTRACT;

    return false;
}

// For regular steppers, trace-call is just a trace-call.
void DebuggerStepper::EnablePolyTraceCall()
{
    this->EnableTraceCall(LEAF_MOST_FRAME);
}

// Traditional steppers enable MethodEnter as a back-stop for step-in.
// We hope that the stub-managers will predict the step-in for us,
// but in case they don't the Method-Enter should catch us.
// MethodEnter is not fully correct for traditional steppers for a few reasons:
// - doesn't handle step-in to native
// - stops us *after* the prolog (a traditional stepper can stop us before the prolog).
// - only works for methods that have the JMC probe. That can exclude all optimized code.
void DebuggerStepper::TriggerMethodEnter(Thread * thread,
                                            DebuggerJitInfo *dji,
                                            const BYTE * ip,
                                            FramePointer fp)
{
    _ASSERTE(dji != NULL);
    _ASSERTE(thread != NULL);
    _ASSERTE(ip != NULL);



    _ASSERTE(this->GetDCType() == DEBUGGER_CONTROLLER_STEPPER);

    _ASSERTE(!IsFrozen());

    MethodDesc * pDesc = dji->m_nativeCodeVersion.GetMethodDesc();
    LOG((LF_CORDB, LL_INFO10000, "DebuggerStepper::TME, desc=%p, addr=%p\n",
        pDesc, ip));

    // JMC steppers won't stop in Lightweight codegen (LCG). Just return & keep executing.
    if (pDesc->IsNoMetadata())
    {
        LOG((LF_CORDB, LL_INFO100000, "DebuggerStepper::TME, skipping b/c it's dynamic code (LCG)\n"));
        return;
    }

    // This is really just a heuristic.  We don't want to trigger a JMC probe when we are
    // executing in an IL stub, or in one of the marshaling methods called by the IL stub.
    // The problem is that the IL stub can call into arbitrary code, including custom marshalers.
    // In that case the user has to put a breakpoint to stop in the code.
    if (g_pEEInterface->DetectHandleILStubs(thread))
    {
        return;
    }

#ifdef _DEBUG
    // To help trace down if a problem is related to a stubmanager,
    // we add a knob that lets us skip the MethodEnter checks. This lets tests directly
    // go against the Stub-managers w/o the MethodEnter check backstops.
    int fSkip = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DbgSkipMEOnStep);
    if (fSkip)
    {
        return;
    }

    // See EnableJMCBackStop() for details here. This check just makes sure that we don't fire
    // the assert if we end up in the method we started in (which could happen if we trace call
    // instructions before the JMC probe).
    // m_StepInStartMethod may be null (if this step-in didn't start from managed code).
    if ((m_StepInStartMethod != pDesc) &&
        (!m_StepInStartMethod->IsLCGMethod()))
    {
        // Since normal step-in should stop us at the prolog, and TME is after the prolog,
        // if a stub-manager did successfully find the address, we should get a TriggerPatch first
        // at native offset 0 (before the prolog) and before we get the TME. That means if
        // we do get the TME, then there was no stub-manager to find us.

        SString sLog;
        StubManager::DbgGetLog(&sLog);

        // Assert b/c the Stub-manager should have caught us first.
        // We don't want people relying on TriggerMethodEnter as the real implementation for Traditional Step-in
        // (see above for reasons why). However, using TME will provide a bandage for the final retail product
        // in cases where we are missing a stub-manager.
        CONSISTENCY_CHECK_MSGF(false, (
            "\nThe Stubmanagers failed to identify and trace a stub on step-in. The stub-managers for this code-path path need to be fixed.\n"
            "See http://team/sites/clrdev/Devdocs/StubManagers.rtf for more information on StubManagers.\n"
            "Stepper this=0x%p, startMethod='%s::%s'\n"
            "---------------------------------\n"
            "Stub manager log:\n%S"
            "\n"
            "The thread is now in managed method '%s::%s'.\n"
            "---------------------------------\n",
            this,
            ((m_StepInStartMethod == NULL) ? "unknown" : m_StepInStartMethod->m_pszDebugClassName),
            ((m_StepInStartMethod == NULL) ? "unknown" : m_StepInStartMethod->m_pszDebugMethodName),
            sLog.GetUnicode(),
            pDesc->m_pszDebugClassName, pDesc->m_pszDebugMethodName
            ));
    }
#endif



    // Place a patch to stopus.
    // Don't bind to a particular AppDomain so that we can do a Cross-Appdomain step.
    AddBindAndActivateNativeManagedPatch(pDesc,
                  dji,
                  CodeRegionInfo::GetCodeRegionInfo(dji, pDesc).AddressToOffset(ip),
                  fp,
                  NULL // AppDomain
    );

    LOG((LF_CORDB, LL_INFO10000, "DJMCStepper::TME, after setting patch to stop\n"));

    // Once we resume, we'll go hit that patch (duh, we patched our return address)
    // Furthermore, we know the step will complete with reason = call, so set that now.
    m_reason = STEP_CALL;
}


// We may have single-stepped over a return statement to land us up a frame.
// Or we may have single-stepped through a method.
// We never single-step into calls (we place a patch at the call destination).
bool DebuggerStepper::TriggerSingleStep(Thread *thread, const BYTE *ip)
{
    LOG((LF_CORDB,LL_INFO10000,"DS:TSS this:0x%p, @ ip:0x%p\n", this, ip));

    _ASSERTE(!IsFrozen());

    // User break should only do a step-out and never actually need a singlestep flag.
    _ASSERTE(GetDCType() != DEBUGGER_CONTROLLER_USER_BREAKPOINT);

    //
    // there's one weird case here - if the last instruction generated
    // a hardware exception, we may be in lala land.  If so, rely on the unwind
    // handler to figure out what happened.
    //
    // <REVISIT_TODO>@todo this could be wrong when we have the incremental collector going</REVISIT_TODO>
    //

    if (!g_pEEInterface->IsManagedNativeCode(ip))
    {
        LOG((LF_CORDB,LL_INFO10000, "DS::TSS: not in managed code, Returning false (case 0)!\n"));
        DisableSingleStep();
        return false;
    }

    // If we EnC the method, we'll blast the function address,
    // and so have to get it from the DJI that we'll have.  If
    // we haven't gotten debugger info about a regular function, then
    // we'll have to get the info from the EE, which will be valid
    // since we're standing in the function at this point, and
    // EnC couldn't have happened yet.
    MethodDesc *fd = g_pEEInterface->GetNativeCodeMethodDesc((PCODE)ip);

    SIZE_T offset;
    DebuggerJitInfo *dji = g_pDebugger->GetJitInfoFromAddr((TADDR) ip);
    offset = CodeRegionInfo::GetCodeRegionInfo(dji, fd).AddressToOffset(ip);

    ControllerStackInfo info;

    // Safe to stackwalk b/c we've already checked that our IP is in crawlable code.
    StackTraceTicket ticket(ip);
    info.GetStackInfo(ticket, GetThread(), LEAF_MOST_FRAME, NULL);

    // This is a special case where we return from a managed method back to an IL stub.  This can
    // only happen if there's no more managed method frames closer to the root and we want to perform
    // a step out, or if we step-next off the end of a method called by an IL stub.  In either case,
    // we'll get a single step in an IL stub, which we want to ignore.  We also want to enable trace
    // call here, just in case this IL stub is about to call the managed target (in the reverse interop case).
    if (fd->IsILStub())
    {
        LOG((LF_CORDB,LL_INFO10000, "DS::TSS: not in managed code, Returning false (case 0)!\n"));
        if (this->GetDCType() == DEBUGGER_CONTROLLER_STEPPER)
        {
            EnableTraceCall(info.m_activeFrame.fp);
        }
        else if (this->GetDCType() == DEBUGGER_CONTROLLER_JMC_STEPPER)
        {
            EnableMethodEnter();
        }
        DisableSingleStep();
        return false;
    }

    DisableAll();

    LOG((LF_CORDB,LL_INFO10000, "DS::TSS m_fp:0x%p, activeFP:0x%p fpExc:0x%p\n",
        m_fp.GetSPValue(), info.m_activeFrame.fp.GetSPValue(), m_fpException.GetSPValue()));

    if (DetectHandleLCGMethods((PCODE)ip, fd, &info))
    {
        return false;
    }

    if (IsInRange(offset, m_range, m_rangeCount, &info) ||
        ShouldContinueStep( &info, offset))
    {
        if (!TrapStep(&info, m_stepIn))
            TrapStepNext(&info);

        EnableUnwind(m_fp);

        LOG((LF_CORDB,LL_INFO10000, "DS::TSS: Returning false Case 1!\n"));
        return false;
    }
    else
    {
        LOG((LF_CORDB,LL_INFO10000, "DS::TSS: Returning true Case 2 for reason STEP_%02x!\n", m_reason));

        // @todo - when would a single-step (not a patch) land us in user-code?
        // For a JMC stepper, we have an additional constraint:
        // skip non-user code. So if we're still in non-user code, then
        // we've got to keep going
        DebuggerMethodInfo * dmi = g_pDebugger->GetOrCreateMethodInfo(fd->GetModule(), fd->GetMemberDef());

        if ((dmi != NULL) && DetectHandleNonUserCode(&info, dmi))
            return false;

        PrepareForSendEvent(ticket);
        return true;
    }
}

void DebuggerStepper::TriggerTraceCall(Thread *thread, const BYTE *ip)
{
    LOG((LF_CORDB,LL_INFO10000,"DS:TTC this:0x%x, @ ip:0x%x\n",this,ip));
    TraceDestination trace;

    if (IsFrozen())
    {
        LOG((LF_CORDB,LL_INFO10000,"DS:TTC exit b/c of Frozen\n"));
        return;
    }

    // This is really just a heuristic.  We don't want to trigger a JMC probe when we are
    // executing in an IL stub, or in one of the marshaling methods called by the IL stub.
    // The problem is that the IL stub can call into arbitrary code, including custom marshalers.
    // In that case the user has to put a breakpoint to stop in the code.
    if (g_pEEInterface->DetectHandleILStubs(thread))
    {
        return;
    }

    if (g_pEEInterface->TraceStub(ip, &trace)
        && g_pEEInterface->FollowTrace(&trace)
        && PatchTrace(&trace, LEAF_MOST_FRAME,
            (m_rgfMappingStop&STOP_UNMANAGED)?(true):(false)))
    {
        // !!! We really want to know ahead of time if PatchTrace will succeed.
        DisableAll();
        PatchTrace(&trace, LEAF_MOST_FRAME, (m_rgfMappingStop&STOP_UNMANAGED)?
            (true):(false));

        // If we're triggering a trace call, and we're following a trace into either managed code or unjitted managed
        // code, then we need to update our stepper's reason to STEP_CALL to reflect the fact that we're going to land
        // into a new function because of a call.
        if ((trace.GetTraceType() == TRACE_UNJITTED_METHOD) || (trace.GetTraceType() == TRACE_MANAGED))
        {
            m_reason = STEP_CALL;
        }

        EnableUnwind(m_fp);

        LOG((LF_CORDB, LL_INFO10000, "DS::TTC potentially a step call!\n"));
    }
}

void DebuggerStepper::TriggerUnwind(Thread *thread,
                                    MethodDesc *fd, DebuggerJitInfo * pDJI, SIZE_T offset,
                                    FramePointer fp,
                                    CorDebugStepReason unwindReason)
{
    CONTRACTL
    {
        THROWS; // from GetJitInfo
        GC_NOTRIGGER; // don't send IPC events
        MODE_COOPERATIVE; // TriggerUnwind always is coop

        PRECONDITION(!IsDbgHelperSpecialThread());
        PRECONDITION(fd->IsDynamicMethod() ||  (pDJI != NULL));
    }
    CONTRACTL_END;

    LOG((LF_CORDB,LL_INFO10000,"DS::TU this:0x%p, in %s::%s, offset 0x%p "
        "frame:0x%p unwindReason:0x%x\n", this, fd->m_pszDebugClassName,
        fd->m_pszDebugMethodName, offset, fp.GetSPValue(), unwindReason));

    _ASSERTE(unwindReason == STEP_EXCEPTION_FILTER || unwindReason == STEP_EXCEPTION_HANDLER);

    if (IsFrozen())
    {
        LOG((LF_CORDB,LL_INFO10000,"DS:TTC exit b/c of Frozen\n"));
        return;
    }

    if (IsCloserToRoot(fp, GetUnwind()))
    {
        // Handler is in a parent frame . For all steps (in,out,over)
        // we want to stop in the handler.
        // This will be like a Step Out, so we don't need any range.
        ResetRange();
    }
    else
    {
        // Handler/Filter is in the same frame as the stepper
        // For a step-in/over, we want to patch the handler/filter.
        // But for a step-out, we want to just continue executing (and don't change
        // the step-reason either).
        if (m_eMode == cStepOut)
        {
            LOG((LF_CORDB, LL_INFO10000, "DS::TU Step-out, returning for same-frame case.\n"));
            return;
        }

    }

    // Remember the origin of the exception, so that if the step looks like
    // it's going to complete in a different frame, but the code comes from the
    // same frame as the one we're in, we won't stop twice in the "same" range
    m_fpException = fp;
    m_fdException = fd;

    //
    // An exception is exiting the step region.  Set a patch on
    // the filter/handler.
    //

    DisableAll();

    BOOL fOk;
    fOk = AddBindAndActivateNativeManagedPatch(fd, pDJI, offset, LEAF_MOST_FRAME, NULL);

    // Since we're unwinding to an already executed method, the method should already
    // be jitted and placing the patch should work.
    CONSISTENCY_CHECK_MSGF(fOk, ("Failed to place patch at TriggerUnwind.\npThis=0x%p md=0x%p, native offset=0x%x\n", this, fd, offset));

    LOG((LF_CORDB,LL_INFO100000,"Step reason:%s\n", unwindReason==STEP_EXCEPTION_FILTER
        ? "STEP_EXCEPTION_FILTER":"STEP_EXCEPTION_HANDLER"));
    m_reason = unwindReason;
}


// Prepare for sending an event.
// This is called 1:1 w/ SendEvent, but this method can be called in a GC_TRIGGERABLE context
// whereas SendEvent is pretty strict.
// Caller ensures that it's safe to run a stack trace.
void DebuggerStepper::PrepareForSendEvent(StackTraceTicket ticket)
{
#ifdef _DEBUG
    _ASSERTE(!m_fReadyToSend);
    m_fReadyToSend = true;
#endif

    LOG((LF_CORDB, LL_INFO10000, "DS::SE m_fpStepInto:0x%x\n", m_fpStepInto.GetSPValue()));

    if (m_fpStepInto != LEAF_MOST_FRAME)
    {
        ControllerStackInfo csi;
        csi.GetStackInfo(ticket, GetThread(), LEAF_MOST_FRAME, NULL);

        if (csi.m_targetFrameFound &&
#if !defined(FEATURE_EH_FUNCLETS)
            IsCloserToRoot(m_fpStepInto, csi.m_activeFrame.fp)
#else
            IsCloserToRoot(m_fpStepInto, (csi.m_activeFrame.IsNonFilterFuncletFrame() ? csi.GetReturnFrame().fp : csi.m_activeFrame.fp))
#endif // FEATURE_EH_FUNCLETS
           )

        {
            m_reason = STEP_CALL;
            LOG((LF_CORDB, LL_INFO10000, "DS::SE this:0x%x STEP_CALL!\n", this));
        }
#ifdef _DEBUG
        else
        {
            LOG((LF_CORDB, LL_INFO10000, "DS::SE this:0x%x not a step call!\n", this));
        }
#endif
    }

#ifdef _DEBUG
        // Steppers should only stop in interesting code.
        if (this->GetDCType() == DEBUGGER_CONTROLLER_JMC_STEPPER)
        {
            // If we're at either a patch or SS, we'll have a context.
            CONTEXT *context = g_pEEInterface->GetThreadFilterContext(GetThread());
            if (context == NULL)
            {
                void * pIP = CORDbgGetIP(reinterpret_cast<DT_CONTEXT *>(context));

                DebuggerJitInfo * dji = g_pDebugger->GetJitInfoFromAddr((TADDR) pIP);
                DebuggerMethodInfo * dmi = NULL;
                if (dji != NULL)
                {
                    dmi = dji->m_methodInfo;

                    CONSISTENCY_CHECK_MSGF(dmi->IsJMCFunction(), ("JMC stepper %p stopping in non-jmc method, MD=%p, '%s::%s'",
                        this, dji->m_nativeCodeVersion.GetMethodDesc(), dji->m_nativeCodeVersion.GetMethodDesc()->m_pszDebugClassName, dji->m_nativeCodeVersion.GetMethodDesc()->m_pszDebugMethodName));

                }


            }
        }

#endif
}

bool DebuggerStepper::SendEvent(Thread *thread, bool fIpChanged)
{
    CONTRACTL
    {
        NOTHROW;
        SENDEVENT_CONTRACT_ITEMS;
    }
    CONTRACTL_END;

    // We practically should never have a step interupted by SetIp.
    // We'll still go ahead and send the Step-complete event because we've already
    // deactivated our triggers by now and we haven't placed any new patches to catch us.
    // We assert here because we don't believe we'll ever be able to hit this scenario.
    // This is technically an issue, but we consider it benign enough to leave in.
    _ASSERTE(!fIpChanged || !"Stepper interupted by SetIp");

    LOG((LF_CORDB, LL_INFO10000, "DS::SE m_fpStepInto:0x%x\n", m_fpStepInto.GetSPValue()));

    _ASSERTE(m_fReadyToSend);
    _ASSERTE(GetThreadNULLOk() == thread);

    CONTEXT *context = g_pEEInterface->GetThreadFilterContext(thread);
    _ASSERTE(!ISREDIRECTEDTHREAD(thread));

    // We need to send the stepper and delete the controller because our stepper
    // no longer has any patches or other triggers that will let it send the step-complete event.
    g_pDebugger->SendStep(thread, context, this, m_reason);

    this->Delete();

#ifdef _DEBUG
    // Now that we've sent the event, we can stop recording information.
    StubManager::DbgFinishLog();
#endif

    return true;
}

void DebuggerStepper::ResetRange()
{
    if (m_range)
    {
        TRACE_FREE(m_range);
        DeleteInteropSafe(m_range);

        m_range = NULL;
    }
}

//-----------------------------------------------------------------------------
// Return true if this stepper is alive, but frozen. (we freeze when the stepper
// enters a nested func-eval).
//-----------------------------------------------------------------------------
bool DebuggerStepper::IsFrozen()
{
    return (m_cFuncEvalNesting > 0);
}

//-----------------------------------------------------------------------------
// Returns true if this stepper is 'dead' - which happens if a non-frozen stepper
// gets a func-eval exit.
//-----------------------------------------------------------------------------
bool DebuggerStepper::IsDead()
{
    return (m_cFuncEvalNesting < 0);
}

// * ------------------------------------------------------------------------
// * DebuggerJMCStepper routines
// * ------------------------------------------------------------------------
DebuggerJMCStepper::DebuggerJMCStepper(Thread *thread,
                    CorDebugUnmappedStop rgfMappingStop,
                    CorDebugIntercept interceptStop,
                    AppDomain *appDomain) :
    DebuggerStepper(thread, rgfMappingStop, interceptStop, appDomain)
{
    LOG((LF_CORDB, LL_INFO10000, "DJMCStepper ctor, this=%p\n", this));
}

DebuggerJMCStepper::~DebuggerJMCStepper()
{
    LOG((LF_CORDB, LL_INFO10000, "DJMCStepper dtor, this=%p\n", this));
}

// If we're a JMC stepper, then don't stop in non-user code.
bool DebuggerJMCStepper::IsInterestingFrame(FrameInfo * pFrame)
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    DebuggerMethodInfo *pInfo = pFrame->GetMethodInfoFromFrameOrThrow();
    _ASSERTE(pInfo != NULL); // throws on failure

    bool fIsUserCode = pInfo->IsJMCFunction();


    LOG((LF_CORDB, LL_INFO1000000, "DS::TSO, frame '%s::%s' is '%s' code\n",
            pFrame->DbgGetClassName(), pFrame->DbgGetMethodName(),
            fIsUserCode ? "user" : "non-user"));

    return fIsUserCode;
}

// A JMC stepper's step-next stops at the next thing of code run.
// This may be a Step-Out, or any User code called before that.
// A1 -> B1 -> { A2, B2 -> B3 -> A3}
// So TrapStepNex at end of A2 should land us in A3.
void DebuggerJMCStepper::TrapStepNext(ControllerStackInfo *info)
{
    LOG((LF_CORDB, LL_INFO10000, "DJMCStepper::TrapStepNext, this=%p\n", this));
    EnableMethodEnter();

    // This will place a patch up the stack and set m_reason = STEP_RETURN.
    // If we end up hitting JMC before that patch, we'll hit TriggerMethodEnter
    // and that will set our reason to STEP_CALL.
    TrapStepOut(info);
}

// ip - target address for call instruction
bool DebuggerJMCStepper::TrapStepInHelper(
    ControllerStackInfo * pInfo,
    const BYTE * ipCallTarget,
    const BYTE * ipNext,
    bool fCallingIntoFunclet,
    bool fIsJump)
{
#ifndef FEATURE_EH_FUNCLETS
    // There are no funclets on x86.
    _ASSERTE(!fCallingIntoFunclet);
#endif

    // If we are calling into a funclet, then we can't rely on the JMC probe to stop us because there are no
    // JMC probes in funclets.  Instead, we have to perform a traditional step-in here.
    if (fCallingIntoFunclet)
    {
        TraceDestination td;
        td.InitForManaged(reinterpret_cast<PCODE>(ipCallTarget));
        PatchTrace(&td, LEAF_MOST_FRAME, false);

        // If this succeeds, then we still need to put a patch at the return address.  This is done below.
        // If this fails, then we definitely need to put a patch at the return address to trap the thread.
        // So in either case, we have to execute the rest of this function.
    }

    if (!fIsJump)
    {
        MethodDesc * pDesc = pInfo->m_activeFrame.md;
        DebuggerJitInfo *dji = NULL;

        // We may not have a DJI if we're in an attach case. We should still be able to do a JMC-step in though.
        // So NULL is ok here.
        dji = g_pDebugger->GetJitInfo(pDesc, (const BYTE*) ipNext);

        // Place patch after call, which is at ipNext. Note we don't need an IL->Native map here
        // since we disassembled native code to find the ip after the call.
        SIZE_T offset = CodeRegionInfo::GetCodeRegionInfo(dji, pDesc).AddressToOffset(ipNext);


        LOG((LF_CORDB, LL_INFO100000, "DJMCStepper::TSIH, at '%s::%s', calling=0x%p, next=0x%p, offset=%d\n",
            pDesc->m_pszDebugClassName,
            pDesc->m_pszDebugMethodName,
            ipCallTarget, ipNext,
            offset));

        // Place a patch at the native address (inside the managed method).
        AddBindAndActivateNativeManagedPatch(pInfo->m_activeFrame.md,
                dji,
                offset,
                pInfo->GetReturnFrame().fp,
                NULL);
    }

    EnableMethodEnter();

    // Return true means that we want to let the stepper run free. It will either
    // hit the patch after the call instruction or it will hit a TriggerMethodEnter.
    return true;
}

// For JMC-steppers, we don't enable trace-call; we enable Method-Enter.
void DebuggerJMCStepper::EnablePolyTraceCall()
{
    _ASSERTE(!IsFrozen());

    this->EnableMethodEnter();
}

// Return true if this is non-user code. This means we've setup the proper patches &
// triggers, etc and so we expect the controller to just run free.
// This is called when all other stepping criteria are met and we're about to
// send a step-complete. For JMC, this is when we see if we're in non-user code
// and if so, continue stepping instead of send the step complete.
// Return false if this is user-code.
bool DebuggerJMCStepper::DetectHandleNonUserCode(ControllerStackInfo *pInfo, DebuggerMethodInfo * dmi)
{
    _ASSERTE(dmi != NULL);
    bool fIsUserCode = dmi->IsJMCFunction();

    if (!fIsUserCode)
    {
        LOG((LF_CORDB, LL_INFO10000, "JMC stepper stopped in non-user code, continuing.\n"));
        // Not-user code, we want to skip through this.

        // We may be here while trying to step-out.
        // Step-out just means stop at the first interesting frame above us.
        // So JMC TrapStepOut won't patch a non-user frame.
        // But if we're skipping over other stuff (prolog, epilog, interceptors,
        // trace calls), then we may still be in the middle of non-user
        //_ASSERTE(m_eMode != cStepOut);

        if (m_eMode == cStepOut)
        {
            TrapStepOut(pInfo);
        }
        else if (m_stepIn)
        {
            EnableMethodEnter();
            TrapStepOut(pInfo);
            // Run until we hit the next thing of managed code.
        } else {
            // Do a traditional step-out since we just want to go up 1 frame.
            TrapStepOut(pInfo, true); // force trad step out.


            // If we're not in the original frame anymore, then
            // If we did a Step-over at the end of a method, and that did a single-step over the return
            // then we may already be in our parent frame. In that case, we also want to behave
            // like a step-in and TriggerMethodEnter.
            if (this->m_fp != pInfo->m_activeFrame.fp)
            {
                // If we're a step-over, then we should only be stopped in a parent frame.
                _ASSERTE(m_stepIn || IsCloserToLeaf(this->m_fp, pInfo->m_activeFrame.fp));
                EnableMethodEnter();
            }

            // Step-over shouldn't stop in a frame below us in the same callstack.
            // So we do a tradional step-out of our current frame, which guarantees
            // that. After that, we act just like a step-in.
            m_stepIn = true;
        }
        EnableUnwind(m_fp);

        // Must keep going...
        return true;
    }

    return false;
}

// Dispatched right after the prolog of a JMC function.
// We may be blocking the GC here, so let's be fast!
void DebuggerJMCStepper::TriggerMethodEnter(Thread * thread,
                                            DebuggerJitInfo *dji,
                                            const BYTE * ip,
                                            FramePointer fp)
{
    _ASSERTE(dji != NULL);
    _ASSERTE(thread != NULL);
    _ASSERTE(ip != NULL);

    _ASSERTE(!IsFrozen());

    MethodDesc * pDesc = dji->m_nativeCodeVersion.GetMethodDesc();
    LOG((LF_CORDB, LL_INFO10000, "DJMCStepper::TME, desc=%p, addr=%p\n",
        pDesc, ip));

    // JMC steppers won't stop in Lightweight delegates. Just return & keep executing.
    if (pDesc->IsNoMetadata())
    {
        LOG((LF_CORDB, LL_INFO100000, "DJMCStepper::TME, skipping b/c it's lw-codegen\n"));
        return;
    }

    // Is this user code?
    DebuggerMethodInfo * dmi = dji->m_methodInfo;
    bool fIsUserCode = dmi->IsJMCFunction();


    LOG((LF_CORDB, LL_INFO100000, "DJMCStepper::TME, '%s::%s' is '%s' code\n",
        pDesc->m_pszDebugClassName,
        pDesc->m_pszDebugMethodName,
        fIsUserCode ? "user" : "non-user"
    ));

    // If this isn't user code, then just return and continue executing.
    if (!fIsUserCode)
        return;

    // MethodEnter is only enabled when we want to stop in a JMC function.
    // And that's where we are now. So patch the ip and resume.
    // The stepper will hit the patch, and stop.

    // It's a good thing we have the fp passed in, because we have no other
    // way of getting it. We can't do a stack trace here (the stack trace
    // would start at the last pushed Frame, which miss a lot of managed
    // frames).

    // Don't bind to a particular AppDomain so that we can do a Cross-Appdomain step.
    AddBindAndActivateNativeManagedPatch(pDesc,
                  dji,
                  CodeRegionInfo::GetCodeRegionInfo(dji, pDesc).AddressToOffset(ip),
                  fp,
                  NULL // AppDomain
    );

    LOG((LF_CORDB, LL_INFO10000, "DJMCStepper::TME, after setting patch to stop\n"));

    // Once we resume, we'll go hit that patch (duh, we patched our return address)
    // Furthermore, we know the step will complete with reason = call, so set that now.
    m_reason = STEP_CALL;
}



//-----------------------------------------------------------------------------
// Helper to convert form an EE Frame's interception enum to a CorDebugIntercept
// bitfield.
// The intercept value in EE Frame's is a 0-based enumeration (not a bitfield).
// The intercept value for ICorDebug is a bitfied.
//-----------------------------------------------------------------------------
CorDebugIntercept ConvertFrameBitsToDbg(Frame::Interception i)
{
    _ASSERTE(i >= 0 && i < Frame::INTERCEPTION_COUNT);

    // Since the ee frame is a 0-based enum, we can just use a map.
    const CorDebugIntercept map[Frame::INTERCEPTION_COUNT] =
    {
        // ICorDebug                     EE Frame
        INTERCEPT_NONE,               // INTERCEPTION_NONE,
        INTERCEPT_CLASS_INIT,         // INTERCEPTION_CLASS_INIT
        INTERCEPT_EXCEPTION_FILTER,   // INTERCEPTION_EXCEPTION
        INTERCEPT_CONTEXT_POLICY,     // INTERCEPTION_CONTEXT
        INTERCEPT_SECURITY,           // INTERCEPTION_SECURITY
        INTERCEPT_INTERCEPTION,       // INTERCEPTION_OTHER
    };

    return map[i];
}

//-----------------------------------------------------------------------------
// This is a helper class to do a stack walk over a certain range and find all the interceptors.
// This allows a JMC stepper to see if there are any interceptors it wants to skip over (though
// there's nothing JMC-specific about this).
// Note that we only want to walk the stack range that the stepper is operating in.
// That's because we don't care about interceptors that happened _before_ the
// stepper was created.
//-----------------------------------------------------------------------------
class InterceptorStackInfo
{
public:
#ifdef _DEBUG
    InterceptorStackInfo()
    {
        // since this ctor just nulls out fpTop (which is already done in Init), we
        // only need it in debug.
        m_fpTop = LEAF_MOST_FRAME;
    }
#endif

    // Get a CorDebugIntercept bitfield that contains a bit for each type of interceptor
    // if that interceptor is present within our stack-range.
    // Stack range is from leaf-most up to and including fp
    CorDebugIntercept GetInterceptorsInRange()
    {
        _ASSERTE(m_fpTop != LEAF_MOST_FRAME || !"Must call Init first");
        return (CorDebugIntercept) m_bits;
    }

    // Prime the stackwalk.
    void Init(FramePointer fpTop, Thread *thread, CONTEXT *pContext, BOOL contextValid)
    {
        _ASSERTE(fpTop != LEAF_MOST_FRAME);
        _ASSERTE(thread != NULL);

        m_bits = 0;
        m_fpTop = fpTop;

        LOG((LF_CORDB,LL_EVERYTHING, "ISI::Init - fpTop=%p, thread=%p, pContext=%p, contextValid=%d\n",
            fpTop.GetSPValue(), thread, pContext, contextValid));

        int result;
        result = DebuggerWalkStack(
           thread,
           LEAF_MOST_FRAME,
           pContext,
           contextValid,
           WalkStack,
           (void *) this,
           FALSE
        );
    }


protected:
    // This is a bitfield of all the interceptors we encounter in our stack-range
    int m_bits;

    // This is the top of our stack range.
    FramePointer m_fpTop;

    static StackWalkAction WalkStack(FrameInfo *pInfo, void *data)
    {
        _ASSERTE(pInfo != NULL);
        _ASSERTE(data != NULL);
        InterceptorStackInfo * pThis = (InterceptorStackInfo*) data;

        // If there's an interceptor frame here, then set those
        // bits in our bitfield.
        Frame::Interception i = Frame::INTERCEPTION_NONE;
        Frame * pFrame = pInfo->frame;
        if ((pFrame != NULL) && (pFrame != FRAME_TOP))
        {
            i = pFrame->GetInterception();
            if (i != Frame::INTERCEPTION_NONE)
            {
                pThis->m_bits |= (int) ConvertFrameBitsToDbg(i);
            }
        }
        else if (pInfo->HasMethodFrame())
        {
            // Check whether we are executing in a class constructor.
            _ASSERTE(pInfo->md != NULL);

            // Need to be careful about an off-by-one error here! Imagine your stack looks like:
            // Foo.DoSomething()
            // Foo..cctor <--- step starts/ends in here
            // Bar.Bar();
            //
            // and your code looks like this:
            // Foo..cctor()
            // {
            //     Foo.DoSomething();  <-- JMC step started here
            //     int x = 1;          <-- step ends here
            //  }
            // This stackwalk covers the inclusive range [Foo..cctor, Foo.DoSomething()] so we will see
            // the static cctor in this walk. However executing inside a static class constructor does not
            // count as an interceptor. You must start the step outside the static constructor and then call
            // into it to have an interceptor. Therefore only static constructors that aren't the outermost
            // frame should be treated as interceptors.
            if (pInfo->md->IsClassConstructor() && (pInfo->fp != pThis->m_fpTop))
            {
                // We called a class constructor, add the appropriate flag
                pThis->m_bits |= (int) INTERCEPT_CLASS_INIT;
            }
        }
        LOG((LF_CORDB,LL_EVERYTHING,"ISI::WS- Frame=%p, fp=%p, Frame bits=%x, Cor bits=0x%x\n", pInfo->frame, pInfo->fp.GetSPValue(), i, pThis->m_bits));


        // We can stop once we hit the top frame.
        if (pInfo->fp == pThis->m_fpTop)
        {
            return SWA_ABORT;
        }
        else
        {
            return SWA_CONTINUE;
        }
    }
};




// Skip interceptors for JMC steppers.
// Return true if we patch something (and thus should keep stepping)
// Return false if we're done.
bool DebuggerJMCStepper::DetectHandleInterceptors(ControllerStackInfo * info)
{
    LOG((LF_CORDB,LL_INFO10000,"DJMCStepper::DHI: Start DetectHandleInterceptors\n"));

    // For JMC, we could stop very far way from an interceptor.
    // So we have to do a stack walk to search for interceptors...
    // If we find any in our stack range (from m_fp ... current fp), then we just do a trap-step-next.

    // Note that this logic should also work for regular steppers, but we've left that in
    // as to keep that code-path unchanged.

    // ControllerStackInfo only gives us the bottom 2 frames on the stack, so we ignore it and
    // have to do our own stack walk.

    // @todo - for us to properly skip filters, we need to make sure that filters show up in our chains.


    InterceptorStackInfo info2;
    CONTEXT *context = g_pEEInterface->GetThreadFilterContext(this->GetThread());
    CONTEXT tempContext;

    _ASSERTE(!ISREDIRECTEDTHREAD(this->GetThread()));

    if (context == NULL)
    {
        info2.Init(this->m_fp, this->GetThread(), &tempContext, FALSE);
    }
    else
    {
        info2.Init(this->m_fp, this->GetThread(), context, TRUE);
    }

    // The following casts are safe on WIN64 platforms.
    int iOnStack = (int) info2.GetInterceptorsInRange();
    int iSkip = ~((int) m_rgfInterceptStop);

    LOG((LF_CORDB,LL_INFO10000,"DJMCStepper::DHI: iOnStack=%x, iSkip=%x\n", iOnStack, iSkip));

    // If the bits on the stack contain any interceptors we want to skip, then we need to keep going.
    if ((iOnStack & iSkip) != 0)
    {
        LOG((LF_CORDB,LL_INFO10000,"DJMCStepper::DHI: keep going!\n"));
        TrapStepNext(info);
        EnableUnwind(m_fp);
        return true;
    }

    LOG((LF_CORDB,LL_INFO10000,"DJMCStepper::DHI: Done!!\n"));
    return false;
}


// * ------------------------------------------------------------------------
// * DebuggerThreadStarter routines
// * ------------------------------------------------------------------------

DebuggerThreadStarter::DebuggerThreadStarter(Thread *thread)
  : DebuggerController(thread, NULL)
{
    LOG((LF_CORDB, LL_INFO1000, "DTS::DTS: this:0x%x Thread:0x%x\n",
        this, thread));

    // Check to make sure we only have 1 ThreadStarter on a given thread. (Inspired by NDPWhidbey issue 16888)
#if defined(_DEBUG)
    EnsureUniqueThreadStarter(this);
#endif
}

// TP_RESULT DebuggerThreadStarter::TriggerPatch()   If we're in a
// stub (module==NULL&&managed) then do a PatchTrace up the stack &
// return false.  Otherwise DisableAll & return
// true
TP_RESULT DebuggerThreadStarter::TriggerPatch(DebuggerControllerPatch *patch,
                                         Thread *thread,
                                         TRIGGER_WHY tyWhy)
{
    Module *module = patch->key.module;
    BOOL managed = patch->IsManagedPatch();

    LOG((LF_CORDB,LL_INFO1000, "DebuggerThreadStarter::TriggerPatch for thread 0x%x\n", Debugger::GetThreadIdHelper(thread)));

    if (module == NULL && managed)
    {
        // This is a stub patch. If it was a TRACE_FRAME_PUSH that got us here, then the stub's frame is pushed now, so
        // we tell the frame to apply the real patch. If we got here via a TRACE_MGR_PUSH, however, then there is no
        // frame and we go back to the stub manager that generated the stub for where to patch next.
        TraceDestination trace;
        bool traceOk;
        if (patch->trace.GetTraceType() == TRACE_MGR_PUSH)
        {
            BYTE *dummy = NULL;
            CONTEXT *context = GetManagedLiveCtx(thread);
            CONTRACT_VIOLATION(GCViolation);
            traceOk = g_pEEInterface->TraceManager(thread, patch->trace.GetStubManager(), &trace, context, &dummy);
        }
        else if ((patch->trace.GetTraceType() == TRACE_FRAME_PUSH) && (thread->GetFrame()->IsTransitionToNativeFrame()))
        {
            // If we've got a frame that is transitioning to native, there's no reason to try to keep tracing. So we
            // bail early and save ourselves some effort. This also works around a problem where we deadlock trying to
            // do too much work to determine the destination of a ComPlusMethodFrame. (See issue 87103.)
            //
            // Note: trace call is still enabled, so we can just ignore this patch and wait for trace call to fire
            // again...
            return TPR_IGNORE;
        }
        else
        {
            // It's questionable whether Trace_Frame_Push is actually safe or not.
            ControllerStackInfo csi;
            StackTraceTicket ticket(patch);
            csi.GetStackInfo(ticket, thread, LEAF_MOST_FRAME, NULL);

            CONTRACT_VIOLATION(GCViolation); // TraceFrame GC-triggers
            traceOk = g_pEEInterface->TraceFrame(thread, thread->GetFrame(), TRUE, &trace, &(csi.m_activeFrame.registers));
        }

        if (traceOk && g_pEEInterface->FollowTrace(&trace))
        {
            PatchTrace(&trace, LEAF_MOST_FRAME, TRUE);
        }

        return TPR_IGNORE;
    }
    else
    {
        // We've hit user code; trigger our event.
        DisableAll();


        {

            // Give the helper thread a chance to get ready. The temporary helper can't handle
            // execution control well, and the RS won't do any execution control until it gets a
            // create Thread event, which it won't get until here.
            // So now's our best time to wait for the real helper thread.
            g_pDebugger->PollWaitingForHelper();
        }

        return TPR_TRIGGER;
    }
}

void DebuggerThreadStarter::TriggerTraceCall(Thread *thread, const BYTE *ip)
{
    LOG((LF_CORDB, LL_EVERYTHING, "DTS::TTC called\n"));
#ifdef DEBUGGING_SUPPORTED
    if (thread->GetDomain()->IsDebuggerAttached())
    {
        TraceDestination trace;

        if (g_pEEInterface->TraceStub(ip, &trace) && g_pEEInterface->FollowTrace(&trace))
        {
            PatchTrace(&trace, LEAF_MOST_FRAME, true);
        }
    }
#endif //DEBUGGING_SUPPORTED

}

bool DebuggerThreadStarter::SendEvent(Thread *thread, bool fIpChanged)
{
    CONTRACTL
    {
        NOTHROW;
        SENDEVENT_CONTRACT_ITEMS;
    }
    CONTRACTL_END;

    // This SendEvent can't be interupted by a SetIp because until the client
    // gets a ThreadStarter event, it doesn't even know the thread exists, so
    // it certainly can't change its ip.
    _ASSERTE(!fIpChanged);

    LOG((LF_CORDB, LL_INFO10000, "DTS::SE: in DebuggerThreadStarter's SendEvent\n"));

    // Send the thread started event.
    g_pDebugger->ThreadStarted(thread);

    // We delete this now because its no longer needed. We can call
    // delete here because the queued count is above 0. This object
    // will really be deleted when its dequeued shortly after this
    // call returns.
    Delete();

    return true;
}

// * ------------------------------------------------------------------------
// * DebuggerUserBreakpoint routines
// * ------------------------------------------------------------------------

bool DebuggerUserBreakpoint::IsFrameInDebuggerNamespace(FrameInfo * pFrame)
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // Steppers ignore internal frames, so should only be called on real frames.
    _ASSERTE(pFrame->HasMethodFrame());

    // Now get the namespace of the active frame
    MethodDesc *pMD = pFrame->md;

    if (pMD  != NULL)
    {
        MethodTable * pMT = pMD->GetMethodTable();

        LPCUTF8 szNamespace = NULL;
        LPCUTF8 szClassName = pMT->GetFullyQualifiedNameInfo(&szNamespace);

        if (szClassName != NULL && szNamespace != NULL)
        {
            MAKE_WIDEPTR_FROMUTF8(wszNamespace, szNamespace); // throw
            MAKE_WIDEPTR_FROMUTF8(wszClassName, szClassName);
            if (wcscmp(wszClassName, W("Debugger")) == 0 &&
                wcscmp(wszNamespace, W("System.Diagnostics")) == 0)
            {
                // This will continue stepping
                return true;
            }
        }
    }
    return false;
}

// Helper check if we're directly in a dynamic method (ignoring any chain goo
// or stuff in the Debugger namespace.
class IsLeafFrameDynamic
{
protected:
    static StackWalkAction WalkStackWrapper(FrameInfo *pInfo, void *data)
    {
        IsLeafFrameDynamic * pThis = reinterpret_cast<IsLeafFrameDynamic*> (data);
        return pThis->WalkStack(pInfo);
    }

    StackWalkAction WalkStack(FrameInfo *pInfo)
    {
        _ASSERTE(pInfo != NULL);

        // A FrameInfo may have both Method + Chain rolled into one.
        if (!pInfo->HasMethodFrame() && !pInfo->HasStubFrame())
        {
            // We're  a chain. Ignore it and keep looking.
            return SWA_CONTINUE;
        }

        // So now this is the first non-chain, non-Debugger namespace frame.
        // LW frames don't have a name, so we check if it's LW first.
        if (pInfo->eStubFrameType == STUBFRAME_LIGHTWEIGHT_FUNCTION)
        {
            m_fInLightWeightMethod = true;
            return SWA_ABORT;
        }

        // Ignore Debugger.Break() frames.
        // All Debugger.Break calls will have this on the stack.
        if (DebuggerUserBreakpoint::IsFrameInDebuggerNamespace(pInfo))
        {
            return SWA_CONTINUE;
        }

        // We've now determined leafmost thing, so stop stackwalking.
        _ASSERTE(m_fInLightWeightMethod == false);
        return SWA_ABORT;
    }


    bool m_fInLightWeightMethod;

    // Need this context to do stack trace.
    CONTEXT m_tempContext;

public:
    // On success, copies the leafmost non-chain frameinfo (including stubs) for the current thread into pInfo
    // and returns true.
    // On failure, returns false.
    // Return true on success.
    bool DoCheck(IN Thread * pThread)
    {
        CONTRACTL
        {
            GC_TRIGGERS;
            THROWS;
            MODE_ANY;

            PRECONDITION(CheckPointer(pThread));
        }
        CONTRACTL_END;

        m_fInLightWeightMethod = false;


        DebuggerWalkStack(
            pThread,
            LEAF_MOST_FRAME,
            &m_tempContext, false,
            WalkStackWrapper,
            (void *) this,
            TRUE // includes everything
            );

        // We don't care whether the stackwalk succeeds or not because the
        // callback sets our status via this field either way, so just return it.
        return m_fInLightWeightMethod;
    };
};

// Handle a Debug.Break() notification.
// This may create a controller to step-out out the Debug.Break() call (so that
// we appear stopped at the callsite).
// If we can't step-out (eg, we're directly in a dynamic method), then send
// the debug event immediately.
void DebuggerUserBreakpoint::HandleDebugBreak(Thread * pThread)
{
    bool fDoStepOut = true;

    // If the leaf frame is not a LW method, then step-out.
    IsLeafFrameDynamic info;
    fDoStepOut = !info.DoCheck(pThread);

    if (fDoStepOut)
    {
        // Create a controller that will step out for us.
        new (interopsafe) DebuggerUserBreakpoint(pThread);
    }
    else
    {
        // Send debug event immediately.
        g_pDebugger->SendUserBreakpointAndSynchronize(pThread);
    }
}


DebuggerUserBreakpoint::DebuggerUserBreakpoint(Thread *thread)
  : DebuggerStepper(thread, (CorDebugUnmappedStop) (STOP_ALL & ~STOP_UNMANAGED), INTERCEPT_ALL,  NULL)
{
    // Setup a step out from the current frame (which we know is
    // unmanaged, actually...)


    // This happens to be safe, but it's a very special case (so we have a special case ticket)
    // This is called while we're live (so no filter context) and from the fcall,
    // and we pushed a HelperMethodFrame to protect us. We also happen to know that we have
    // done anything illegal or dangerous since then.

    StackTraceTicket ticket(this);
    StepOut(LEAF_MOST_FRAME, ticket);
}


// Is this frame interesting?
// Use this to skip all code in the namespace "Debugger.Diagnostics"
bool DebuggerUserBreakpoint::IsInterestingFrame(FrameInfo * pFrame)
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return !IsFrameInDebuggerNamespace(pFrame);
}

bool DebuggerUserBreakpoint::SendEvent(Thread *thread, bool fIpChanged)
{
    CONTRACTL
    {
        NOTHROW;
        SENDEVENT_CONTRACT_ITEMS;
    }
    CONTRACTL_END;

    // See DebuggerStepper::SendEvent for why we assert here.
    // This is technically an issue, but it's too benign to fix.
    _ASSERTE(!fIpChanged);

    LOG((LF_CORDB, LL_INFO10000,
         "DUB::SE: in DebuggerUserBreakpoint's SendEvent\n"));

    // Send the user breakpoint event.
    g_pDebugger->SendRawUserBreakpoint(thread);

    // We delete this now because its no longer needed. We can call
    // delete here because the queued count is above 0. This object
    // will really be deleted when its dequeued shortly after this
    // call returns.
    Delete();

    return true;
}

// * ------------------------------------------------------------------------
// * DebuggerFuncEvalComplete routines
// * ------------------------------------------------------------------------

DebuggerFuncEvalComplete::DebuggerFuncEvalComplete(Thread *thread,
                                                   void *dest)
  : DebuggerController(thread, NULL)
{
#ifdef TARGET_ARM
    m_pDE = reinterpret_cast<DebuggerEvalBreakpointInfoSegment*>(((DWORD)dest) & ~THUMB_CODE)->m_associatedDebuggerEval;
#else
    m_pDE = reinterpret_cast<DebuggerEvalBreakpointInfoSegment*>(dest)->m_associatedDebuggerEval;
#endif

    // Add an unmanaged patch at the destination.
    AddAndActivateNativePatchForAddress((CORDB_ADDRESS_TYPE*)dest, LEAF_MOST_FRAME, FALSE, TRACE_UNMANAGED);
}

TP_RESULT DebuggerFuncEvalComplete::TriggerPatch(DebuggerControllerPatch *patch,
                                            Thread *thread,
                                            TRIGGER_WHY tyWhy)
{

    // It had better be an unmanaged patch...
    _ASSERTE((patch->key.module == NULL) && !patch->IsManagedPatch());

    // set ThreadFilterContext back here because we need make stack crawlable! In case,
    // GC got triggered.

    // Restore the thread's context to what it was before we hijacked it for this func eval.
    CONTEXT *pCtx = GetManagedLiveCtx(thread);
#ifdef FEATURE_DATABREAKPOINT
#ifdef TARGET_UNIX
        #error Not supported
#endif // TARGET_UNIX
#if defined(TARGET_X86) || defined(TARGET_AMD64)
    // If a data breakpoint is set while we hit a breakpoint inside a FuncEval, this will make sure the data breakpoint stays
    m_pDE->m_context.Dr0 = pCtx->Dr0;
    m_pDE->m_context.Dr1 = pCtx->Dr1;
    m_pDE->m_context.Dr2 = pCtx->Dr2;
    m_pDE->m_context.Dr3 = pCtx->Dr3;
    m_pDE->m_context.Dr6 = pCtx->Dr6;
    m_pDE->m_context.Dr7 = pCtx->Dr7;
#else
    #error Not supported
#endif
#endif
    CORDbgCopyThreadContext(reinterpret_cast<DT_CONTEXT *>(pCtx),
                            reinterpret_cast<DT_CONTEXT *>(&(m_pDE->m_context)));

    // We've hit our patch, so simply disable all (which removes the
    // patch) and trigger the event.
    DisableAll();
    return TPR_TRIGGER;
}

bool DebuggerFuncEvalComplete::SendEvent(Thread *thread, bool fIpChanged)
{
    CONTRACTL
    {
        THROWS;
        SENDEVENT_CONTRACT_ITEMS;
    }
    CONTRACTL_END;


    // This should not ever be interupted by a SetIp.
    // The BP will be off in random native code for which SetIp would be illegal.
    // However, func-eval conroller will restore the context from when we're at the patch,
    // so that will look like the IP changed on us.
    _ASSERTE(fIpChanged);

    LOG((LF_CORDB, LL_INFO10000, "DFEC::SE: in DebuggerFuncEval's SendEvent\n"));

    _ASSERTE(!ISREDIRECTEDTHREAD(thread));

    // The DebuggerEval is at our faulting address.
    DebuggerEval *pDE = m_pDE;

    // Send the func eval complete (or exception) event.
    g_pDebugger->FuncEvalComplete(thread, pDE);

    // We delete this now because its no longer needed. We can call
    // delete here because the queued count is above 0. This object
    // will really be deleted when its dequeued shortly after this
    // call returns.
    Delete();

    return true;
}

#ifdef EnC_SUPPORTED

// * ------------------------------------------------------------------------ *
// * DebuggerEnCBreakpoint routines
// * ------------------------------------------------------------------------ *

//---------------------------------------------------------------------------------------
//
// DebuggerEnCBreakpoint constructor - creates and activates a new EnC breakpoint
//
// Arguments:
//    offset        - native offset in the function to place the patch
//    jitInfo       - identifies the function in which the breakpoint is being placed
//    fTriggerType  - breakpoint type: either REMAP_PENDING or REMAP_COMPLETE
//    pAppDomain    - the breakpoint applies to the specified AppDomain only
//

DebuggerEnCBreakpoint::DebuggerEnCBreakpoint(SIZE_T offset,
                                             DebuggerJitInfo *jitInfo,
                                             DebuggerEnCBreakpoint::TriggerType fTriggerType,
                                             AppDomain *pAppDomain)
  : DebuggerController(NULL, pAppDomain),
    m_jitInfo(jitInfo),
    m_fTriggerType(fTriggerType)
{
    _ASSERTE( jitInfo != NULL );
    // Add and activate the specified patch
    AddBindAndActivateNativeManagedPatch(jitInfo->m_nativeCodeVersion.GetMethodDesc(), jitInfo, offset, LEAF_MOST_FRAME, pAppDomain);
    LOG((LF_ENC,LL_INFO1000, "DEnCBPDEnCBP::adding %S patch!\n",
        fTriggerType == REMAP_PENDING ? W("remap pending") : W("remap complete")));
}


//---------------------------------------------------------------------------------------
//
// DebuggerEnCBreakpoint::TriggerPatch
//   called by the debugging infrastructure when the patch is hit.
//
// Arguments:
//    patch         - specifies the patch that was hit
//    thread        - identifies the thread on which the patch was hit
//    tyWhy         - TY_SHORT_CIRCUIT for normal REMAP_PENDING EnC patches
//
// Return value:
//   TPR_IGNORE if the debugger chooses not to take a remap opportunity
//   TPR_IGNORE_AND_STOP when a remap-complete event is sent
//   Doesn't return at all if the debugger remaps execution to the new version of the method
//
TP_RESULT DebuggerEnCBreakpoint::TriggerPatch(DebuggerControllerPatch *patch,
                                         Thread *thread,
                                         TRIGGER_WHY tyWhy)
{
    _ASSERTE(HasLock());

    Module *module = patch->key.module;
    mdMethodDef md = patch->key.md;
    SIZE_T offset = patch->offset;

    // Map the current native offset back to the IL offset in the old
    // function.  This will be mapped to the new native offset within
    // ResumeInUpdatedFunction
    CorDebugMappingResult map;
    DWORD which;
    SIZE_T currentIP = (SIZE_T)m_jitInfo->MapNativeOffsetToIL(offset,
            &map, &which);

    // We only lay DebuggerEnCBreakpoints at sequence points
    _ASSERTE(map == MAPPING_EXACT);

    LOG((LF_ENC, LL_ALWAYS,
         "DEnCBP::TP: triggered E&C %S breakpoint: tid=0x%x, module=0x%08x, "
         "method def=0x%08x, version=%d, native offset=0x%x, IL offset=0x%x\n this=0x%x\n",
         m_fTriggerType == REMAP_PENDING ? W("ResumePending") : W("ResumeComplete"),
         thread, module, md, m_jitInfo->m_encVersion, offset, currentIP, this));

    // If this is a REMAP_COMPLETE patch, then dispatch the RemapComplete callback
    if (m_fTriggerType == REMAP_COMPLETE)
    {
        return HandleRemapComplete(patch, thread, tyWhy);
    }

    // This must be a REMAP_PENDING patch
    // unless we got here on an explicit short-circuit, don't do any work
    if (tyWhy != TY_SHORT_CIRCUIT)
    {
        LOG((LF_ENC, LL_ALWAYS, "DEnCBP::TP: not short-circuit ... bailing\n"));
        return TPR_IGNORE;
    }

    _ASSERTE(patch->IsManagedPatch());

    // Grab the MethodDesc for this function.
    _ASSERTE(module != NULL);

    // GENERICS: @todo generics. This should be replaced by a similar loop
    // over the DJIs for the DMI as in BindPatch up above.
    MethodDesc *pFD = g_pEEInterface->FindLoadedMethodRefOrDef(module, md);

    _ASSERTE(pFD != NULL);

    LOG((LF_ENC, LL_ALWAYS,
         "DEnCBP::TP: in %s::%s\n", pFD->m_pszDebugClassName,pFD->m_pszDebugMethodName));

    // Grab the jit info for the original copy of the method, which is
    // what we are executing right now.
    DebuggerJitInfo *pJitInfo = m_jitInfo;
    _ASSERTE(pJitInfo);
    _ASSERTE(pJitInfo->m_nativeCodeVersion.GetMethodDesc() == pFD);

    // Grab the context for this thread. This is the context that was
    // passed to COMPlusFrameHandler.
    CONTEXT *pContext = GetManagedLiveCtx(thread);

    // We use the module the current function is in.
    _ASSERTE(module->IsEditAndContinueEnabled());
    EditAndContinueModule *pModule = (EditAndContinueModule*)module;

    // Release the controller lock for the rest of this method
    CrstBase::UnsafeCrstInverseHolder inverseLock(&g_criticalSection);

    // resumeIP is the native offset in the new version of the method the debugger wants
    // to resume to.  We'll pass the address of this variable over to the right-side
    // and if it modifies the contents while we're stopped dispatching the RemapOpportunity,
    // then we know it wants a remap.
    // This form of side-channel communication seems like an error-prone workaround.  Ideally the
    // remap IP (if any) would just be returned in a response event.
    SIZE_T resumeIP = (SIZE_T) -1;

    // Debugging code to enable a break after N RemapOpportunities
#ifdef _DEBUG
        static int breakOnRemapOpportunity = -1;
        if (breakOnRemapOpportunity == -1)
            breakOnRemapOpportunity = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_EnCBreakOnRemapOpportunity);

        static int remapOpportunityCount = 0;

        ++remapOpportunityCount;
        if (breakOnRemapOpportunity == 1 || breakOnRemapOpportunity == remapOpportunityCount)
        {
            _ASSERTE(!"BreakOnRemapOpportunity");
        }
#endif

    // Send an event to the RS to call the RemapOpportunity callback, passing the address of resumeIP.
    // If the debugger responds with a call to RemapFunction, the supplied IP will be copied into resumeIP
    // and we will know to update the context and resume the function at the new IP. Otherwise we just do
    // nothing and try again on next RemapFunction breakpoint
    g_pDebugger->LockAndSendEnCRemapEvent(pJitInfo, currentIP, &resumeIP);

    LOG((LF_ENC, LL_ALWAYS,
         "DEnCBP::TP: resume IL offset is 0x%x\n", resumeIP));

    // Has the debugger requested a remap?
    if (resumeIP != (SIZE_T) -1)
    {
        // This will jit the function, update the context, and resume execution at the new location.
        g_pEEInterface->ResumeInUpdatedFunction(pModule,
                                                pFD,
                                                (void*)pJitInfo,
                                                resumeIP,
                                                pContext);
        _ASSERTE(!"Returned from ResumeInUpdatedFunction!");
    }

    LOG((LF_CORDB, LL_ALWAYS, "DEnCB::TP: We've returned from ResumeInUpd"
        "atedFunction, we're going to skip the EnC patch ####\n"));

    // We're returning then we'll have to re-get this lock. Be careful that we haven't kept any controller/patches
    // in the caller. They can move when we unlock, so when we release the lock and reget it here, things might have
    // changed underneath us.
    // inverseLock holder will reaquire lock.

    return TPR_IGNORE;
}

//
// HandleResumeComplete is called for an EnC patch in the newly updated function
// so that we can notify the debugger that the remap has completed and they can
// now remap their steppers or anything else that depends on the new code actually
// being on the stack. We return TPR_IGNORE_AND_STOP because it's possible that the
// function was edited after we handled remap complete and want to make sure we
// start a fresh call to TriggerPatch
//
TP_RESULT DebuggerEnCBreakpoint::HandleRemapComplete(DebuggerControllerPatch *patch,
                                                     Thread *thread,
                                                     TRIGGER_WHY tyWhy)
{
    LOG((LF_ENC, LL_ALWAYS, "DEnCBP::HRC: HandleRemapComplete\n"));

    // Debugging code to enable a break after N RemapCompletes
#ifdef _DEBUG
    static int breakOnRemapComplete = -1;
    if (breakOnRemapComplete == -1)
        breakOnRemapComplete = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_EnCBreakOnRemapComplete);

    static int remapCompleteCount = 0;
    ++remapCompleteCount;
    if (breakOnRemapComplete == 1 || breakOnRemapComplete == remapCompleteCount)
    {
        _ASSERTE(!"BreakOnRemapComplete");
    }
#endif
    _ASSERTE(HasLock());


    bool fApplied = m_jitInfo->m_encBreakpointsApplied;
    // Need to delete this before unlock below so if any other thread come in after the unlock
    // they won't handle this patch.
    Delete();

    // We just deleted ourselves. Can't access anything any instances after this point.

    // if have somehow updated this function before we resume into it then just bail
    if (fApplied)
    {
        LOG((LF_ENC, LL_ALWAYS, "DEnCBP::HRC:  function already updated, ignoring\n"));
        return TPR_IGNORE_AND_STOP;
    }

    // GENERICS: @todo generics. This should be replaced by a similar loop
    // over the DJIs for the DMI as in BindPatch up above.
    MethodDesc *pFD = g_pEEInterface->FindLoadedMethodRefOrDef(patch->key.module, patch->key.md);

    LOG((LF_ENC, LL_ALWAYS, "DEnCBP::HRC: unlocking controller\n"));

    // Unlock the controller lock and dispatch the remap complete event
    CrstBase::UnsafeCrstInverseHolder inverseLock(&g_criticalSection);

    LOG((LF_ENC, LL_ALWAYS, "DEnCBP::HRC: sending RemapCompleteEvent\n"));

    g_pDebugger->LockAndSendEnCRemapCompleteEvent(pFD);

    // We're returning then we'll have to re-get this lock. Be careful that we haven't kept any controller/patches
    // in the caller. They can move when we unlock, so when we release the lock and reget it here, things might have
    // changed underneath us.
    // inverseLock  holder will reacquire.

    return TPR_IGNORE_AND_STOP;
}
#endif //EnC_SUPPORTED

// continuable-exceptions
// * ------------------------------------------------------------------------ *
// * DebuggerContinuableExceptionBreakpoint routines
// * ------------------------------------------------------------------------ *


//---------------------------------------------------------------------------------------
//
// constructor
//
// Arguments:
//    pThread       - the thread on which we are intercepting an exception
//    nativeOffset  - This is the target native offset.  It is where we are going to resume execution.
//    jitInfo       - the DebuggerJitInfo of the method at which we are intercepting
//    pAppDomain    - the AppDomain in which the thread is executing
//

DebuggerContinuableExceptionBreakpoint::DebuggerContinuableExceptionBreakpoint(Thread *pThread,
                                                                               SIZE_T nativeOffset,
                                                                               DebuggerJitInfo *jitInfo,
                                                                               AppDomain *pAppDomain)
  : DebuggerController(pThread, pAppDomain)
{
    _ASSERTE( jitInfo != NULL );
    // Add a native patch at the specified native offset, which is where we are going to resume execution.
    AddBindAndActivateNativeManagedPatch(jitInfo->m_nativeCodeVersion.GetMethodDesc(), jitInfo, nativeOffset, LEAF_MOST_FRAME, pAppDomain);
}

//---------------------------------------------------------------------------------------
//
// This function is called when the patch added in the constructor is hit.  At this point,
// we have already resumed execution, and the exception is no longer in flight.
//
// Arguments:
//    patch     - the patch added in the constructor; unused
//    thread    - the thread in question; unused
//    tyWhy     - a flag which is only useful for EnC; unused
//
// Return Value:
//    This function always returns TPR_TRIGGER, meaning that it wants to send an event to notify the RS.
//

TP_RESULT DebuggerContinuableExceptionBreakpoint::TriggerPatch(DebuggerControllerPatch *patch,
                                                               Thread *thread,
                                                               TRIGGER_WHY tyWhy)
{
    LOG((LF_CORDB, LL_INFO10000, "DCEBP::TP\n"));

    //
    // Disable the patch
    //
    DisableAll();

    // We will send a notification to the RS when the patch is triggered.
    return TPR_TRIGGER;
}

//---------------------------------------------------------------------------------------
//
// This function is called when we want to notify the RS that an interception is complete.
// At this point, we have already resumed execution, and the exception is no longer in flight.
//
// Arguments:
//    thread        - the thread in question
//    fIpChanged    - whether the IP has changed by SetIP after the patch is hit but
//                    before this function is called
//

bool DebuggerContinuableExceptionBreakpoint::SendEvent(Thread *thread, bool fIpChanged)
{
    CONTRACTL
    {
        NOTHROW;
        SENDEVENT_CONTRACT_ITEMS;
    }
    CONTRACTL_END;



    LOG((LF_CORDB, LL_INFO10000,
         "DCEBP::SE: in DebuggerContinuableExceptionBreakpoint's SendEvent\n"));

    if (!fIpChanged)
    {
        g_pDebugger->SendInterceptExceptionComplete(thread);
    }

    // On WIN64, by the time we get here the DebuggerExState is gone already.
    // ExceptionTrackers are cleaned up before we resume execution for a handled exception.
#if !defined(FEATURE_EH_FUNCLETS)
    thread->GetExceptionState()->GetDebuggerState()->SetDebuggerInterceptContext(NULL);
#endif // !FEATURE_EH_FUNCLETS


    //
    // We delete this now because its no longer needed. We can call
    // delete here because the queued count is above 0. This object
    // will really be deleted when its dequeued shortly after this
    // call returns.
    //
    Delete();

    return true;
}

#ifdef FEATURE_DATABREAKPOINT

/* static */ bool DebuggerDataBreakpoint::IsDataBreakpoint(Thread *thread, CONTEXT * pContext)
{
    bool hitDataBp = false;
#ifdef TARGET_UNIX
    #error Not supported
#endif // TARGET_UNIX
#if defined(TARGET_X86) || defined(TARGET_AMD64)
    PDR6 pdr6 = (PDR6)&(pContext->Dr6);

    if (pdr6->B0 || pdr6->B1 || pdr6->B2 || pdr6->B3)
    {
        hitDataBp = true;
    }
#else // defined(TARGET_X86) || defined(TARGET_AMD64)
    #error Not supported
#endif // defined(TARGET_X86) || defined(TARGET_AMD64)
    return hitDataBp;
}

/* static */ bool DebuggerDataBreakpoint::TriggerDataBreakpoint(Thread *thread, CONTEXT * pContext)
{
    LOG((LF_CORDB, LL_INFO10000, "D::DDBP: Doing TriggerDataBreakpoint...\n"));

    bool hitDataBp = IsDataBreakpoint(thread, pContext);
    bool result = false;
    if (hitDataBp)
    {
        if (g_pDebugger->IsThreadAtSafePlace(thread))
        {
            LOG((LF_CORDB, LL_INFO10000, "D::DDBP: HIT DATA BREAKPOINT...\n"));
            result = true;
        }
        else
        {
            CONTEXT contextToAdjust;
            BOOL adjustedContext = FALSE;
            memcpy(&contextToAdjust, pContext, sizeof(CONTEXT));
            adjustedContext = g_pEEInterface->AdjustContextForJITHelpersForDebugger(&contextToAdjust);
            if (adjustedContext)
            {
                LOG((LF_CORDB, LL_INFO10000, "D::DDBP: HIT DATA BREAKPOINT INSIDE WRITE BARRIER...\n"));
                DebuggerDataBreakpoint *pDataBreakpoint = new (interopsafe) DebuggerDataBreakpoint(thread);
                pDataBreakpoint->AddAndActivateNativePatchForAddress((CORDB_ADDRESS_TYPE*)GetIP(&contextToAdjust), FramePointer::MakeFramePointer(GetFP(&contextToAdjust)), true, DPT_DEFAULT_TRACE_TYPE);
            }
            else
            {
                LOG((LF_CORDB, LL_INFO10000, "D::DDBP: HIT DATA BREAKPOINT BUT STILL NEED TO ROLL ...\n"));
                DebuggerDataBreakpoint *pDataBreakpoint = new (interopsafe) DebuggerDataBreakpoint(thread);
                pDataBreakpoint->EnableSingleStep();
            }
            result = false;
        }
    }
    else
    {
        LOG((LF_CORDB, LL_INFO10000, "D::DDBP: DIDN'T TRIGGER DATA BREAKPOINT...\n"));
        result = false;
    }
    return result;
}

TP_RESULT DebuggerDataBreakpoint::TriggerPatch(DebuggerControllerPatch *patch, Thread *thread,  TRIGGER_WHY tyWhy)
{
    if (g_pDebugger->IsThreadAtSafePlace(thread))
    {
        return TPR_TRIGGER;
    }
    else
    {
        LOG((LF_CORDB, LL_INFO10000, "D::DDBP: REACH RETURN OF JIT HELPER BUT STILL NEED TO ROLL ...\n"));
        this->EnableSingleStep();
        return TPR_IGNORE;
    }
}

bool DebuggerDataBreakpoint::TriggerSingleStep(Thread *thread, const BYTE *ip)
{
    if (g_pDebugger->IsThreadAtSafePlace(thread))
    {
        LOG((LF_CORDB, LL_INFO10000, "D:DDBP: Finally safe for stopping, stop stepping\n"));
        this->DisableSingleStep();
        return true;
    }
    else
    {
        LOG((LF_CORDB, LL_INFO10000, "D:DDBP: Still not safe for stopping, continue stepping\n"));
        return false;
    }
}

#endif // FEATURE_DATABREAKPOINT

#endif // !DACCESS_COMPILE
