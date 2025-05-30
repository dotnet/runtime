// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: JITinterfaceX86.CPP
//
// ===========================================================================

// This contains JITinterface routines that are tailored for
// X86 platforms. Non-X86 versions of these can be found in
// JITinterfaceGen.cpp


#include "common.h"
#include "jitinterface.h"
#include "eeconfig.h"
#include "excep.h"
#include "comdelegate.h"
#include "field.h"
#include "ecall.h"
#include "asmconstants.h"
#include "virtualcallstub.h"
#include "eventtrace.h"
#include "threadsuspend.h"

#include <minipal/cpuid.h>

#if defined(_DEBUG) && !defined (WRITE_BARRIER_CHECK)
#define WRITE_BARRIER_CHECK 1
#endif

// To test with MON_DEBUG off, comment out the following line. DO NOT simply define
// to be 0 as the checks are for #ifdef not #if 0.
//
#ifdef _DEBUG
#define MON_DEBUG 1
#endif

class JIT_TrialAlloc
{
public:
    enum Flags
    {
        NORMAL       = 0x0,
        MP_ALLOCATOR = 0x1,
        SIZE_IN_EAX  = 0x2,
        OBJ_ARRAY    = 0x4,
        ALIGN8       = 0x8,     // insert a dummy object to ensure 8 byte alignment (until the next GC)
        ALIGN8OBJ    = 0x10,
    };

    static void *GenAllocSFast(Flags flags);
    static void *GenBox(Flags flags);
    static void *GenAllocArray(Flags flags);
    static void *GenAllocString(Flags flags);

private:
    static void EmitAlignmentRoundup(CPUSTUBLINKER *psl,X86Reg regTestAlign, X86Reg regToAdj, Flags flags);
    static void EmitDummyObject(CPUSTUBLINKER *psl, X86Reg regTestAlign, Flags flags);
    static void EmitCore(CPUSTUBLINKER *psl, CodeLabel *noLock, CodeLabel *noAlloc, Flags flags);
    static void EmitNoAllocCode(CPUSTUBLINKER *psl, Flags flags);
};

extern "C" LONG g_global_alloc_lock;

extern "C" void STDCALL JIT_WriteBarrierReg_PreGrow();// JIThelp.asm/JIThelp.s
extern "C" void STDCALL JIT_WriteBarrierReg_PostGrow();// JIThelp.asm/JIThelp.s

#ifdef _DEBUG
extern "C" void STDCALL WriteBarrierAssert(BYTE* ptr, Object* obj)
{
    WRAPPER_NO_CONTRACT;

    static BOOL fVerifyHeap = -1;

    if (fVerifyHeap == -1)
        fVerifyHeap = g_pConfig->GetHeapVerifyLevel() & EEConfig::HEAPVERIFY_GC;

    if (fVerifyHeap)
    {
        if (obj)
        {
            obj->Validate(FALSE);
        }
        if (GCHeapUtilities::GetGCHeap()->IsHeapPointer(ptr))
        {
            Object* pObj = *(Object**)ptr;
            _ASSERTE (pObj == NULL || GCHeapUtilities::GetGCHeap()->IsHeapPointer(pObj));
        }
    }
    else
    {
        _ASSERTE((g_lowest_address <= ptr && ptr < g_highest_address) ||
             ((size_t)ptr < MAX_UNCHECKED_OFFSET_FOR_NULL_OBJECT));
    }
}

#endif // _DEBUG

/*********************************************************************/
#ifdef FEATURE_HIJACK
extern "C" void STDCALL JIT_TailCallHelper(Thread * pThread);
void STDCALL JIT_TailCallHelper(Thread * pThread)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    pThread->UnhijackThread();
}
#endif // FEATURE_HIJACK

    // emit code that adds MIN_OBJECT_SIZE to reg if reg is unaligned thus making it aligned
void JIT_TrialAlloc::EmitAlignmentRoundup(CPUSTUBLINKER *psl, X86Reg testAlignReg, X86Reg adjReg, Flags flags)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE((MIN_OBJECT_SIZE & 7) == 4);   // want to change alignment

    CodeLabel *AlreadyAligned = psl->NewCodeLabel();

    // test reg, 7
    psl->Emit16(0xC0F7 | (static_cast<unsigned short>(testAlignReg) << 8));
    psl->Emit32(0x7);

    // jz alreadyAligned
    if (flags & ALIGN8OBJ)
    {
        psl->X86EmitCondJump(AlreadyAligned, X86CondCode::kJNZ);
    }
    else
    {
        psl->X86EmitCondJump(AlreadyAligned, X86CondCode::kJZ);
    }

    psl->X86EmitAddReg(adjReg, MIN_OBJECT_SIZE);
    // AlreadyAligned:
    psl->EmitLabel(AlreadyAligned);
}

    // if 'reg' is unaligned, then set the dummy object at EAX and increment EAX past
    // the dummy object
void JIT_TrialAlloc::EmitDummyObject(CPUSTUBLINKER *psl, X86Reg alignTestReg, Flags flags)
{
    STANDARD_VM_CONTRACT;

    CodeLabel *AlreadyAligned = psl->NewCodeLabel();

    // test reg, 7
    psl->Emit16(0xC0F7 | (static_cast<unsigned short>(alignTestReg) << 8));
    psl->Emit32(0x7);

    // jz alreadyAligned
    if (flags & ALIGN8OBJ)
    {
        psl->X86EmitCondJump(AlreadyAligned, X86CondCode::kJNZ);
    }
    else
    {
        psl->X86EmitCondJump(AlreadyAligned, X86CondCode::kJZ);
    }

    // Make the fake object
    // mov EDX, [g_pObjectClass]
    psl->Emit16(0x158B);
    psl->Emit32((int)(size_t)&g_pObjectClass);

    // mov [EAX], EDX
    psl->X86EmitOffsetModRM(0x89, kEDX, kEAX, 0);

    // add EAX, MIN_OBJECT_SIZE
    psl->X86EmitAddReg(kEAX, MIN_OBJECT_SIZE);

    // AlreadyAligned:
    psl->EmitLabel(AlreadyAligned);
}

void JIT_TrialAlloc::EmitCore(CPUSTUBLINKER *psl, CodeLabel *noLock, CodeLabel *noAlloc, Flags flags)
{
    STANDARD_VM_CONTRACT;

    // Upon entry here, ecx contains the method we are to try allocate memory for
    // Upon exit, eax contains the allocated memory, edx is trashed, and ecx undisturbed

    if (flags & MP_ALLOCATOR)
    {
        if (flags & (ALIGN8 | SIZE_IN_EAX | ALIGN8OBJ))
        {
            if (flags & ALIGN8OBJ)
            {
                // mov             eax, [ecx]MethodTable.m_BaseSize
                psl->X86EmitIndexRegLoad(kEAX, kECX, offsetof(MethodTable, m_BaseSize));
            }

            psl->X86EmitPushReg(kEBX);  // we need a spare register
        }
        else
        {
            // mov             eax, [ecx]MethodTable.m_BaseSize
            psl->X86EmitIndexRegLoad(kEAX, kECX, offsetof(MethodTable, m_BaseSize));
        }

        assert( ((flags & ALIGN8)==0     ||  // EAX loaded by else statement
                 (flags & SIZE_IN_EAX)   ||  // EAX already comes filled out
                 (flags & ALIGN8OBJ)     )   // EAX loaded in the if (flags & ALIGN8OBJ) statement
                 && "EAX should contain size for allocation and it doesnt!!!");

        // Fetch current thread into EDX, preserving EAX and ECX
        psl->X86EmitCurrentThreadAllocContextFetch(kEDX, (1 << kEAX) | (1 << kECX));

        // Try the allocation.


        if (flags & (ALIGN8 | SIZE_IN_EAX | ALIGN8OBJ))
        {
            // MOV EBX, [edx]alloc_context.m_GCAllocContext.alloc_ptr
            psl->X86EmitOffsetModRM(0x8B, kEBX, kEDX, ee_alloc_context::getAllocPtrFieldOffset());
            // add EAX, EBX
            psl->Emit16(0xC303);
            if (flags & ALIGN8)
                EmitAlignmentRoundup(psl, kEBX, kEAX, flags);      // bump EAX up size by 12 if EBX unaligned (so that we are aligned)
        }
        else
        {
            // add             eax, [edx]alloc_context.m_GCAllocContext.alloc_ptr
            psl->X86EmitOffsetModRM(0x03, kEAX, kEDX, ee_alloc_context::getAllocPtrFieldOffset());
        }

        // cmp             eax, [edx]alloc_context.m_CombinedLimit
        psl->X86EmitOffsetModRM(0x3b, kEAX, kEDX, ee_alloc_context::getCombinedLimitFieldOffset());

        // ja              noAlloc
        psl->X86EmitCondJump(noAlloc, X86CondCode::kJA);

        // Fill in the allocation and get out.

        // mov             [edx]alloc_context.m_GCAllocContext.alloc_ptr, eax
        psl->X86EmitIndexRegStore(kEDX, ee_alloc_context::getAllocPtrFieldOffset(), kEAX);

        if (flags & (ALIGN8 | SIZE_IN_EAX | ALIGN8OBJ))
        {
            // mov EAX, EBX
            psl->Emit16(0xC38B);
            // pop EBX
            psl->X86EmitPopReg(kEBX);

            if (flags & ALIGN8)
                EmitDummyObject(psl, kEAX, flags);
        }
        else
        {
            // sub             eax, [ecx]MethodTable.m_BaseSize
            psl->X86EmitOffsetModRM(0x2b, kEAX, kECX, offsetof(MethodTable, m_BaseSize));
        }

        // mov             dword ptr [eax], ecx
        psl->X86EmitIndexRegStore(kEAX, 0, kECX);
    }
    else
    {
        // Take the GC lock (there is no lock prefix required - we will use JIT_TrialAllocSFastMP on an MP System).
        // inc             dword ptr [g_global_alloc_lock]
        psl->Emit16(0x05ff);
        psl->Emit32((int)(size_t)&g_global_alloc_lock);

        // jnz             NoLock
        psl->X86EmitCondJump(noLock, X86CondCode::kJNZ);

        if (flags & SIZE_IN_EAX)
        {
            // mov edx, eax
            psl->Emit16(0xd08b);
        }
        else
        {
            // mov             edx, [ecx]MethodTable.m_BaseSize
            psl->X86EmitIndexRegLoad(kEDX, kECX, offsetof(MethodTable, m_BaseSize));
        }

        // mov             eax, dword ptr [g_global_alloc_context.m_GCAllocContext.alloc_ptr]
        psl->Emit8(0xA1);
        psl->Emit32((int)(size_t)&g_global_alloc_context + ee_alloc_context::getAllocPtrFieldOffset());

        // Try the allocation.
        // add             edx, eax
        psl->Emit16(0xd003);

        if (flags & (ALIGN8 | ALIGN8OBJ))
            EmitAlignmentRoundup(psl, kEAX, kEDX, flags);      // bump up EDX size by 12 if EAX unaligned (so that we are aligned)

        // cmp             edx, dword ptr [g_global_alloc_context.m_CombinedLimit]
        psl->Emit16(0x153b);
        psl->Emit32((int)(size_t)&g_global_alloc_context + ee_alloc_context::getCombinedLimitFieldOffset());

        // ja              noAlloc
        psl->X86EmitCondJump(noAlloc, X86CondCode::kJA);

        // Fill in the allocation and get out.
        // mov             dword ptr [g_global_alloc_context.m_GCAllocContext.alloc_ptr], edx
        psl->Emit16(0x1589);
        psl->Emit32((int)(size_t)&g_global_alloc_context + ee_alloc_context::getAllocPtrFieldOffset());

        if (flags & (ALIGN8 | ALIGN8OBJ))
            EmitDummyObject(psl, kEAX, flags);

        // mov             dword ptr [eax], ecx
        psl->X86EmitIndexRegStore(kEAX, 0, kECX);

        // mov             dword ptr [g_global_alloc_lock], 0FFFFFFFFh
        psl->Emit16(0x05C7);
        psl->Emit32((int)(size_t)&g_global_alloc_lock);
        psl->Emit32(0xFFFFFFFF);
    }


#ifdef INCREMENTAL_MEMCLR
    // <TODO>We're planning to get rid of this anyhow according to Patrick</TODO>
    _ASSERTE(!"NYI");
#endif // INCREMENTAL_MEMCLR
}

void JIT_TrialAlloc::EmitNoAllocCode(CPUSTUBLINKER *psl, Flags flags)
{
    STANDARD_VM_CONTRACT;

    if (flags & MP_ALLOCATOR)
    {
        if (flags & (ALIGN8|SIZE_IN_EAX))
            psl->X86EmitPopReg(kEBX);
    }
    else
    {
        // mov             dword ptr [g_global_alloc_lock], 0FFFFFFFFh
        psl->Emit16(0x05c7);
        psl->Emit32((int)(size_t)&g_global_alloc_lock);
        psl->Emit32(0xFFFFFFFF);
    }
}

FCDECL1(Object*, JIT_New, CORINFO_CLASS_HANDLE typeHnd_);

void *JIT_TrialAlloc::GenAllocSFast(Flags flags)
{
    STANDARD_VM_CONTRACT;

    CPUSTUBLINKER sl;

    CodeLabel *noLock  = sl.NewCodeLabel();
    CodeLabel *noAlloc = sl.NewCodeLabel();

    // Emit the main body of the trial allocator, be it SP or MP
    EmitCore(&sl, noLock, noAlloc, flags);

    // Here we are at the end of the success case - just emit a ret
    sl.X86EmitReturn(0);

    // Come here in case of no space
    sl.EmitLabel(noAlloc);

    // Release the lock in the uniprocessor case
    EmitNoAllocCode(&sl, flags);

    // Come here in case of failure to get the lock
    sl.EmitLabel(noLock);

    // Jump to the framed helper
    sl.X86EmitNearJump(sl.NewExternalCodeLabel((LPVOID)JIT_New));

    Stub *pStub = sl.Link(SystemDomain::GetGlobalLoaderAllocator()->GetExecutableHeap(), NEWSTUB_FL_NONE, "AllocSFast");

    return (void *)pStub->GetEntryPoint();
}

void *JIT_TrialAlloc::GenAllocArray(Flags flags)
{
    STANDARD_VM_CONTRACT;

    CPUSTUBLINKER sl;

    CodeLabel *noLock  = sl.NewCodeLabel();
    CodeLabel *noAlloc = sl.NewCodeLabel();

    // We were passed a (shared) method table in RCX, which contains the element type.

    // If this is the allocator for use from unmanaged code, ECX contains the
    // element type descriptor, or the CorElementType.

    // We need to save ECX for later

    // push ecx
    sl.X86EmitPushReg(kECX);

    // The element count is in EDX - we need to save it for later.

    // push edx
    sl.X86EmitPushReg(kEDX);

    // Do a conservative check here.  This is to avoid doing overflow checks within this function.  We'll
    // still have to do a size check before running through the body of EmitCore.  The way we do the check
    // against the allocation quantum there requires that we not overflow when adding the size to the
    // current allocation context pointer.  There is exactly LARGE_OBJECT_SIZE of headroom there, so do that
    // check before we EmitCore.
    //
    // For reference types, we can just pick the correct value of maxElems and skip the second check.
    //
    // By the way, we use 258 as a "slack" value to ensure that we don't overflow because of the size of the
    // array header or alignment.
    sl.Emit16(0xfa81);

    unsigned maxElems =  0xffff - 256;

    if (flags & OBJ_ARRAY)
    {
        //Since we know that the array elements are sizeof(OBJECTREF), set maxElems exactly here (use the
        //same slack from above.
        maxElems = min(maxElems, (LARGE_OBJECT_SIZE/sizeof(OBJECTREF)) - 256);
    }
    sl.Emit32(maxElems);


    // jae noLock - seems tempting to jump to noAlloc, but we haven't taken the lock yet
    sl.X86EmitCondJump(noLock, X86CondCode::kJAE);

    if (flags & OBJ_ARRAY)
    {
        // In this case we know the element size is sizeof(void *), or 4 for x86
        // This helps us in two ways - we can shift instead of multiplying, and
        // there's no need to align the size either

        _ASSERTE(sizeof(void *) == 4);

        // mov eax, [ecx]MethodTable.m_BaseSize
        sl.X86EmitIndexRegLoad(kEAX, kECX, offsetof(MethodTable, m_BaseSize));

        // lea eax, [eax+edx*4]
        sl.X86EmitOp(0x8d, kEAX, kEAX, 0, kEDX, 4);
    }
    else
    {
        // movzx eax, [ECX]MethodTable.m_dwFlags /* component size */
        sl.Emit8(0x0f);
        sl.X86EmitOffsetModRM(0xb7, kEAX, kECX, offsetof(MethodTable, m_dwFlags /* component size */));

        // mul eax, edx
        sl.Emit16(0xe2f7);

        // add eax, [ecx]MethodTable.m_BaseSize
        sl.X86EmitOffsetModRM(0x03, kEAX, kECX, offsetof(MethodTable, m_BaseSize));

        // Since this is an array of value classes, we need an extra compare here to make sure we're still
        // less than LARGE_OBJECT_SIZE.  This is the last bit of arithmetic before we compare against the
        // allocation context, so do it here.

        // cmp eax, LARGE_OBJECT_SIZE
        // ja noLock
        sl.Emit8(0x3d);
        sl.Emit32(LARGE_OBJECT_SIZE);
        sl.X86EmitCondJump(noLock, X86CondCode::kJA);
    }

#if DATA_ALIGNMENT == 4
    if (flags & OBJ_ARRAY)
    {
        // No need for rounding in this case - element size is 4, and m_BaseSize is guaranteed
        // to be a multiple of 4.
    }
    else
#endif // DATA_ALIGNMENT == 4
    {
        // round the size to a multiple of 4

        // add eax, 3
        sl.X86EmitAddReg(kEAX, (DATA_ALIGNMENT-1));

        // and eax, ~3
        sl.Emit16(0xe083);
        sl.Emit8(~(DATA_ALIGNMENT-1));
    }

    flags = (Flags)(flags | SIZE_IN_EAX);

    // Emit the main body of the trial allocator, be it SP or MP
    EmitCore(&sl, noLock, noAlloc, flags);

    // Here we are at the end of the success case - store element count
    // and possibly the element type descriptor and return

    // pop edx - element count
    sl.X86EmitPopReg(kEDX);

    // pop ecx - array method table
    sl.X86EmitPopReg(kECX);

    // mov             dword ptr [eax]ArrayBase.m_NumComponents, edx
    sl.X86EmitIndexRegStore(kEAX, offsetof(ArrayBase,m_NumComponents), kEDX);

    // no stack parameters
    sl.X86EmitReturn(0);

    // Come here in case of no space
    sl.EmitLabel(noAlloc);

    // Release the lock in the uniprocessor case
    EmitNoAllocCode(&sl, flags);

    // Come here in case of failure to get the lock
    sl.EmitLabel(noLock);

    // pop edx - element count
    sl.X86EmitPopReg(kEDX);

    // pop ecx - array method table
    sl.X86EmitPopReg(kECX);

    // Jump to the framed helper
    CodeLabel * target = sl.NewExternalCodeLabel((LPVOID)JIT_NewArr1);
    _ASSERTE(target->e.m_pExternalAddress);
    sl.X86EmitNearJump(target);

    Stub *pStub = sl.Link(SystemDomain::GetGlobalLoaderAllocator()->GetExecutableHeap(), NEWSTUB_FL_NONE, "AllocArray");

    return (void *)pStub->GetEntryPoint();
}


void *JIT_TrialAlloc::GenAllocString(Flags flags)
{
    STANDARD_VM_CONTRACT;

    CPUSTUBLINKER sl;

    CodeLabel *noLock  = sl.NewCodeLabel();
    CodeLabel *noAlloc = sl.NewCodeLabel();

    // We were passed the number of characters in ECX

    // push ecx
    sl.X86EmitPushReg(kECX);

    // mov eax, ecx
    sl.Emit16(0xc18b);

    // we need to load the method table for string from the global

    // mov ecx, [g_pStringClass]
    sl.Emit16(0x0d8b);
    sl.Emit32((int)(size_t)&g_pStringClass);

    // Instead of doing elaborate overflow checks, we just limit the number of elements
    // to (LARGE_OBJECT_SIZE - 256)/sizeof(WCHAR) or less.
    // This will avoid all overflow problems, as well as making sure
    // big string objects are correctly allocated in the big object heap.

    _ASSERTE(sizeof(WCHAR) == 2);

    // cmp edx,(LARGE_OBJECT_SIZE - 256)/sizeof(WCHAR)
    sl.Emit16(0xf881);
    sl.Emit32((LARGE_OBJECT_SIZE - 256)/sizeof(WCHAR));

    // jae noLock - seems tempting to jump to noAlloc, but we haven't taken the lock yet
    sl.X86EmitCondJump(noLock, X86CondCode::kJAE);

    // Calculate the final size to allocate.
    // We need to calculate baseSize + cnt*2, then round that up by adding 3 and anding ~3.

    // lea eax, [basesize+(alignment-1)+eax*2]
    sl.Emit16(0x048d);
    sl.Emit8(0x45);
    sl.Emit32(StringObject::GetBaseSize() + (DATA_ALIGNMENT-1));

    // and eax, ~3
    sl.Emit16(0xe083);
    sl.Emit8(~(DATA_ALIGNMENT-1));

    flags = (Flags)(flags | SIZE_IN_EAX);

    // Emit the main body of the trial allocator, be it SP or MP
    EmitCore(&sl, noLock, noAlloc, flags);

    // Here we are at the end of the success case - store element count
    // and possibly the element type descriptor and return

    // pop ecx - element count
    sl.X86EmitPopReg(kECX);

    // mov             dword ptr [eax]ArrayBase.m_StringLength, ecx
    sl.X86EmitIndexRegStore(kEAX, offsetof(StringObject,m_StringLength), kECX);

    // no stack parameters
    sl.X86EmitReturn(0);

    // Come here in case of no space
    sl.EmitLabel(noAlloc);

    // Release the lock in the uniprocessor case
    EmitNoAllocCode(&sl, flags);

    // Come here in case of failure to get the lock
    sl.EmitLabel(noLock);

    // pop ecx - element count
    sl.X86EmitPopReg(kECX);

    // Jump to the framed helper
    CodeLabel * target = sl.NewExternalCodeLabel((LPVOID)FramedAllocateString);
    sl.X86EmitNearJump(target);

    Stub *pStub = sl.Link(SystemDomain::GetGlobalLoaderAllocator()->GetExecutableHeap(), NEWSTUB_FL_NONE, "AllocString");

    return (void *)pStub->GetEntryPoint();
}

#define NUM_WRITE_BARRIERS 6

static const BYTE c_rgWriteBarrierRegs[NUM_WRITE_BARRIERS] = {
    0, // EAX
    1, // ECX
    3, // EBX
    6, // ESI
    7, // EDI
    5, // EBP
};

static const void * const c_rgWriteBarriers[NUM_WRITE_BARRIERS] = {
    (void *)JIT_WriteBarrierEAX,
    (void *)JIT_WriteBarrierECX,
    (void *)JIT_WriteBarrierEBX,
    (void *)JIT_WriteBarrierESI,
    (void *)JIT_WriteBarrierEDI,
    (void *)JIT_WriteBarrierEBP,
};

#ifdef WRITE_BARRIER_CHECK
static const void * const c_rgDebugWriteBarriers[NUM_WRITE_BARRIERS] = {
    (void *)JIT_DebugWriteBarrierEAX,
    (void *)JIT_DebugWriteBarrierECX,
    (void *)JIT_DebugWriteBarrierEBX,
    (void *)JIT_DebugWriteBarrierESI,
    (void *)JIT_DebugWriteBarrierEDI,
    (void *)JIT_DebugWriteBarrierEBP,
};
#endif // WRITE_BARRIER_CHECK

/*********************************************************************/
// Initialize the part of the JIT helpers that require very little of
// EE infrastructure to be in place.
/*********************************************************************/
#pragma warning (disable : 4731)
void InitJITHelpers1()
{
    STANDARD_VM_CONTRACT;

#define ETW_NUM_JIT_HELPERS 10
    static const LPCWSTR pHelperNames[ETW_NUM_JIT_HELPERS] = {
                                                      W("@NewObject"),
                                                      W("@NewObjectAlign8"),
                                                      W("@NewArray1Object"),
                                                      W("@NewArray1ValueType"),
                                                      W("@NewArray1ObjectAlign8"),
                                                      W("@StaticBaseObject"),
                                                      W("@StaticBaseNonObject"),
                                                      W("@StaticBaseObjectNoCCtor"),
                                                      W("@StaticBaseNonObjectNoCCtor")
                                                    };

    PVOID pMethodAddresses[ETW_NUM_JIT_HELPERS]={0};

    _ASSERTE(g_SystemInfo.dwNumberOfProcessors != 0);

    JIT_TrialAlloc::Flags flags = GCHeapUtilities::UseThreadAllocationContexts() ?
        JIT_TrialAlloc::MP_ALLOCATOR : JIT_TrialAlloc::NORMAL;

    if (!(TrackAllocationsEnabled()
        || LoggingOn(LF_GCALLOC, LL_INFO10)
#ifdef _DEBUG
        || (g_pConfig->ShouldInjectFault(INJECTFAULT_GCHEAP) != 0)
#endif
         )
        )
    {
        // Replace the slow helpers with faster version

        pMethodAddresses[0] = JIT_TrialAlloc::GenAllocSFast(flags);
        SetJitHelperFunction(CORINFO_HELP_NEWSFAST, pMethodAddresses[0]);
        pMethodAddresses[1] = JIT_TrialAlloc::GenAllocSFast((JIT_TrialAlloc::Flags)(flags|JIT_TrialAlloc::ALIGN8 | JIT_TrialAlloc::ALIGN8OBJ));
        SetJitHelperFunction(CORINFO_HELP_NEWSFAST_ALIGN8, pMethodAddresses[1]);
        pMethodAddresses[2] = JIT_TrialAlloc::GenAllocArray((JIT_TrialAlloc::Flags)(flags|JIT_TrialAlloc::OBJ_ARRAY));
        SetJitHelperFunction(CORINFO_HELP_NEWARR_1_OBJ, pMethodAddresses[2]);
        pMethodAddresses[3] = JIT_TrialAlloc::GenAllocArray(flags);
        SetJitHelperFunction(CORINFO_HELP_NEWARR_1_VC, pMethodAddresses[3]);
        pMethodAddresses[4] = JIT_TrialAlloc::GenAllocArray((JIT_TrialAlloc::Flags)(flags|JIT_TrialAlloc::ALIGN8));
        SetJitHelperFunction(CORINFO_HELP_NEWARR_1_ALIGN8, pMethodAddresses[4]);

        // If allocation logging is on, then we divert calls to FastAllocateString to an Ecall method, not this
        // generated method. Find this workaround in Ecall::Init() in ecall.cpp.
        ECall::DynamicallyAssignFCallImpl((PCODE) JIT_TrialAlloc::GenAllocString(flags), ECall::FastAllocateString);
    }

    ETW::MethodLog::StubsInitialized(pMethodAddresses, (PVOID *)pHelperNames, ETW_NUM_JIT_HELPERS);

    // All write barrier helpers should fit into one page.
    // If you hit this assert on retail build, there is most likely problem with BBT script.
    _ASSERTE_ALL_BUILDS((BYTE*)JIT_WriteBarrierGroup_End - (BYTE*)JIT_WriteBarrierGroup < (ptrdiff_t)GetOsPageSize());
    _ASSERTE_ALL_BUILDS((BYTE*)JIT_PatchedWriteBarrierGroup_End - (BYTE*)JIT_PatchedWriteBarrierGroup < (ptrdiff_t)GetOsPageSize());

    // Copy the write barriers to their final resting place.
    if (IsWriteBarrierCopyEnabled())
    {
        for (int iBarrier = 0; iBarrier < NUM_WRITE_BARRIERS; iBarrier++)
        {
            BYTE * pfunc = (BYTE *) JIT_WriteBarrierReg_PreGrow;

            BYTE * pBuf = GetWriteBarrierCodeLocation((BYTE *)c_rgWriteBarriers[iBarrier]);
            int reg = c_rgWriteBarrierRegs[iBarrier];

            BYTE * pBufRW = pBuf;
            ExecutableWriterHolderNoLog<BYTE> barrierWriterHolder;
            barrierWriterHolder.AssignExecutableWriterHolder(pBuf, 34);
            pBufRW = barrierWriterHolder.GetRW();

            memcpy(pBufRW, pfunc, 34);

            // assert the copied code ends in a ret to make sure we got the right length
            _ASSERTE(pBuf[33] == 0xC3);

            // We need to adjust registers in a couple of instructions
            // It would be nice to have the template contain all zeroes for
            // the register fields (corresponding to EAX), but that doesn't
            // work because then we get a smaller encoding for the compares
            // that only works for EAX but not the other registers.
            // So we always have to clear the register fields before updating them.

            // First instruction to patch is a mov [edx], reg

            _ASSERTE(pBuf[0] == 0x89);
            // Update the reg field (bits 3..5) of the ModR/M byte of this instruction
            pBufRW[1] &= 0xc7;
            pBufRW[1] |= reg << 3;

            // Second instruction to patch is cmp reg, imm32 (low bound)

            _ASSERTE(pBuf[2] == 0x81);
            // Here the lowest three bits in ModR/M field are the register
            pBufRW[3] &= 0xf8;
            pBufRW[3] |= reg;

#ifdef WRITE_BARRIER_CHECK
            // Don't do the fancy optimization just jump to the old one
            // Use the slow one for write barrier checks build because it has some good asserts
            if (g_pConfig->GetHeapVerifyLevel() & EEConfig::HEAPVERIFY_BARRIERCHECK) {
                pfunc = &pBufRW[0];
                *pfunc++ = 0xE9;                // JMP c_rgDebugWriteBarriers[iBarrier]
                *((DWORD*) pfunc) = (BYTE*) c_rgDebugWriteBarriers[iBarrier] - (&pBuf[1] + sizeof(DWORD));
            }
#endif // WRITE_BARRIER_CHECK
        }

#ifndef CODECOVERAGE
        ValidateWriteBarrierHelpers();
#endif
    }

    // Leave the patched region writable for StompWriteBarrierEphemeral(), StompWriteBarrierResize()
}
#pragma warning (default : 4731)

// these constans are offsets into our write barrier helpers for values that get updated as the bounds of the managed heap change.
// ephemeral region
const int AnyGrow_EphemeralLowerBound = 4; // offset is the same for both pre and post grow functions
const int PostGrow_EphemeralUpperBound = 12;

// card table
const int PreGrow_CardTableFirstLocation = 16;
const int PreGrow_CardTableSecondLocation = 28;
const int PostGrow_CardTableFirstLocation = 24;
const int PostGrow_CardTableSecondLocation = 36;


#ifndef CODECOVERAGE        // Deactivate alignment validation for code coverage builds
                            // because the instrumented binaries will not preserve alignment constraints and we will fail.

void ValidateWriteBarrierHelpers()
{
    // we have an invariant that the addresses of all the values that we update in our write barrier
    // helpers must be naturally aligned, this is so that the update can happen atomically since there
    // are places where we update these values while the EE is running

#ifdef WRITE_BARRIER_CHECK
    // write barrier checking uses the slower helpers that we don't bash so there is no need for validation
    if (g_pConfig->GetHeapVerifyLevel() & EEConfig::HEAPVERIFY_BARRIERCHECK)
        return;
#endif // WRITE_BARRIER_CHECK

    // first validate the PreGrow helper
    BYTE* pWriteBarrierFunc = GetWriteBarrierCodeLocation(reinterpret_cast<BYTE*>(JIT_WriteBarrierEAX));

    // ephemeral region
    DWORD* pLocation = reinterpret_cast<DWORD*>(&pWriteBarrierFunc[AnyGrow_EphemeralLowerBound]);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<DWORD>(pLocation) & 0x3) == 0);
    _ASSERTE_ALL_BUILDS(*pLocation == 0xf0f0f0f0);

    // card table
    pLocation = reinterpret_cast<DWORD*>(&pWriteBarrierFunc[PreGrow_CardTableFirstLocation]);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<DWORD>(pLocation) & 0x3) == 0);
    _ASSERTE_ALL_BUILDS(*pLocation == 0xf0f0f0f0);
    pLocation = reinterpret_cast<DWORD*>(&pWriteBarrierFunc[PreGrow_CardTableSecondLocation]);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<DWORD>(pLocation) & 0x3) == 0);
    _ASSERTE_ALL_BUILDS(*pLocation == 0xf0f0f0f0);

    // now validate the PostGrow helper
    pWriteBarrierFunc = reinterpret_cast<BYTE*>(JIT_WriteBarrierReg_PostGrow);

    // ephemeral region
    pLocation = reinterpret_cast<DWORD*>(&pWriteBarrierFunc[AnyGrow_EphemeralLowerBound]);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<DWORD>(pLocation) & 0x3) == 0);
    _ASSERTE_ALL_BUILDS(*pLocation == 0xf0f0f0f0);
    pLocation = reinterpret_cast<DWORD*>(&pWriteBarrierFunc[PostGrow_EphemeralUpperBound]);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<DWORD>(pLocation) & 0x3) == 0);
    _ASSERTE_ALL_BUILDS(*pLocation == 0xf0f0f0f0);

    // card table
    pLocation = reinterpret_cast<DWORD*>(&pWriteBarrierFunc[PostGrow_CardTableFirstLocation]);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<DWORD>(pLocation) & 0x3) == 0);
    _ASSERTE_ALL_BUILDS(*pLocation == 0xf0f0f0f0);
    pLocation = reinterpret_cast<DWORD*>(&pWriteBarrierFunc[PostGrow_CardTableSecondLocation]);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<DWORD>(pLocation) & 0x3) == 0);
    _ASSERTE_ALL_BUILDS(*pLocation == 0xf0f0f0f0);
}

#endif //CODECOVERAGE
/*********************************************************************/

#define WriteBarrierIsPreGrow() ((GetWriteBarrierCodeLocation((BYTE *)JIT_WriteBarrierEAX))[10] == 0xc1)


/*********************************************************************/
// When a GC happens, the upper and lower bounds of the ephemeral
// generation change.  This routine updates the WriteBarrier thunks
// with the new values.
int StompWriteBarrierEphemeral(bool /* isRuntimeSuspended */)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    int stompWBCompleteActions = SWB_PASS;

    if (!IsWriteBarrierCopyEnabled())
    {
        // If we didn't copy the write barriers, then don't update them.
        return SWB_PASS;
    }

#ifdef WRITE_BARRIER_CHECK
        // Don't do the fancy optimization if we are checking write barrier
    if ((GetWriteBarrierCodeLocation((BYTE *)JIT_WriteBarrierEAX))[0] == 0xE9)  // we are using slow write barrier
        return stompWBCompleteActions;
#endif // WRITE_BARRIER_CHECK

    // Update the lower bound.
    for (int iBarrier = 0; iBarrier < NUM_WRITE_BARRIERS; iBarrier++)
    {
        BYTE * pBuf = GetWriteBarrierCodeLocation((BYTE *)c_rgWriteBarriers[iBarrier]);

        BYTE * pBufRW = pBuf;
        ExecutableWriterHolderNoLog<BYTE> barrierWriterHolder;
        barrierWriterHolder.AssignExecutableWriterHolder(pBuf, 42);
        pBufRW = barrierWriterHolder.GetRW();

        // assert there is in fact a cmp r/m32, imm32 there
        _ASSERTE(pBuf[2] == 0x81);

        // Update the immediate which is the lower bound of the ephemeral generation
        size_t *pfunc = (size_t *) &pBufRW[AnyGrow_EphemeralLowerBound];
        //avoid trivial self modifying code
        if (*pfunc != (size_t) g_ephemeral_low)
        {
            stompWBCompleteActions |= SWB_ICACHE_FLUSH;
            *pfunc = (size_t) g_ephemeral_low;
        }
        if (!WriteBarrierIsPreGrow())
        {
            // assert there is in fact a cmp r/m32, imm32 there
            _ASSERTE(pBuf[10] == 0x81);

                // Update the upper bound if we are using the PostGrow thunk.
            pfunc = (size_t *) &pBufRW[PostGrow_EphemeralUpperBound];
            //avoid trivial self modifying code
            if (*pfunc != (size_t) g_ephemeral_high)
            {
                stompWBCompleteActions |= SWB_ICACHE_FLUSH;
                *pfunc = (size_t) g_ephemeral_high;
            }
        }
    }

    return stompWBCompleteActions;
}

/*********************************************************************/
// When the GC heap grows, the ephemeral generation may no longer
// be after the older generations.  If this happens, we need to switch
// to the PostGrow thunk that checks both upper and lower bounds.
// regardless we need to update the thunk with the
// card_table - lowest_address.
int StompWriteBarrierResize(bool isRuntimeSuspended, bool bReqUpperBoundsCheck)
{
    CONTRACTL {
        NOTHROW;
        if (GetThreadNULLOk()) {GC_TRIGGERS;} else {GC_NOTRIGGER;}
    } CONTRACTL_END;

    int stompWBCompleteActions = SWB_PASS;

    if (!IsWriteBarrierCopyEnabled())
    {
        // If we didn't copy the write barriers, then don't update them.
        return SWB_PASS;
    }

#ifdef WRITE_BARRIER_CHECK
        // Don't do the fancy optimization if we are checking write barrier
    if ((GetWriteBarrierCodeLocation((BYTE *)JIT_WriteBarrierEAX))[0] == 0xE9)  // we are using slow write barrier
        return stompWBCompleteActions;
#endif // WRITE_BARRIER_CHECK

    bool bWriteBarrierIsPreGrow = WriteBarrierIsPreGrow();
    bool bStompWriteBarrierEphemeral = false;

    for (int iBarrier = 0; iBarrier < NUM_WRITE_BARRIERS; iBarrier++)
    {
        BYTE * pBuf = GetWriteBarrierCodeLocation((BYTE *)c_rgWriteBarriers[iBarrier]);
        int reg = c_rgWriteBarrierRegs[iBarrier];

        size_t *pfunc;

        BYTE * pBufRW = pBuf;
        ExecutableWriterHolderNoLog<BYTE> barrierWriterHolder;
        barrierWriterHolder.AssignExecutableWriterHolder(pBuf, 42);
        pBufRW = barrierWriterHolder.GetRW();

        // Check if we are still using the pre-grow version of the write barrier.
        if (bWriteBarrierIsPreGrow)
        {
            // Check if we need to use the upper bounds checking barrier stub.
            if (bReqUpperBoundsCheck)
            {
                GCX_MAYBE_COOP_NO_THREAD_BROKEN((GetThreadNULLOk()!=NULL));
                if( !isRuntimeSuspended && !(stompWBCompleteActions & SWB_EE_RESTART) ) {
                    ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_FOR_GC_PREP);
                    stompWBCompleteActions |= SWB_EE_RESTART;
                }

                pfunc = (size_t *) JIT_WriteBarrierReg_PostGrow;
                memcpy(pBufRW, pfunc, 42);

                // assert the copied code ends in a ret to make sure we got the right length
                _ASSERTE(pBuf[41] == 0xC3);

                // We need to adjust registers in a couple of instructions
                // It would be nice to have the template contain all zeroes for
                // the register fields (corresponding to EAX), but that doesn't
                // work because then we get a smaller encoding for the compares
                // that only works for EAX but not the other registers
                // So we always have to clear the register fields before updating them.

                // First instruction to patch is a mov [edx], reg

                _ASSERTE(pBuf[0] == 0x89);
                // Update the reg field (bits 3..5) of the ModR/M byte of this instruction
                pBufRW[1] &= 0xc7;
                pBufRW[1] |= reg << 3;

                // Second instruction to patch is cmp reg, imm32 (low bound)

                _ASSERTE(pBuf[2] == 0x81);
                // Here the lowest three bits in ModR/M field are the register
                pBufRW[3] &= 0xf8;
                pBufRW[3] |= reg;

                // Third instruction to patch is another cmp reg, imm32 (high bound)

                _ASSERTE(pBuf[10] == 0x81);
                // Here the lowest three bits in ModR/M field are the register
                pBufRW[11] &= 0xf8;
                pBufRW[11] |= reg;

                bStompWriteBarrierEphemeral = true;
                // What we're trying to update is the offset field of a

                // cmp offset[edx], 0ffh instruction
                _ASSERTE(pBuf[22] == 0x80);
                pfunc = (size_t *) &pBufRW[PostGrow_CardTableFirstLocation];
                if (*pfunc != (size_t) g_card_table)
                {
                    stompWBCompleteActions |= SWB_ICACHE_FLUSH;
                    *pfunc = (size_t) g_card_table;
                }

                // What we're trying to update is the offset field of a
                // mov offset[edx], 0ffh instruction
                _ASSERTE(pBuf[34] == 0xC6);
                pfunc = (size_t *) &pBufRW[PostGrow_CardTableSecondLocation];

            }
            else
            {
                // What we're trying to update is the offset field of a

                // cmp offset[edx], 0ffh instruction
                _ASSERTE(pBuf[14] == 0x80);
                pfunc = (size_t *) &pBufRW[PreGrow_CardTableFirstLocation];
                if (*pfunc != (size_t) g_card_table)
                {
                    stompWBCompleteActions |= SWB_ICACHE_FLUSH;
                    *pfunc = (size_t) g_card_table;
                }

                // What we're trying to update is the offset field of a

                // mov offset[edx], 0ffh instruction
                _ASSERTE(pBuf[26] == 0xC6);
                pfunc = (size_t *) &pBufRW[PreGrow_CardTableSecondLocation];
            }
        }
        else
        {
            // What we're trying to update is the offset field of a

            // cmp offset[edx], 0ffh instruction
            _ASSERTE(pBuf[22] == 0x80);
            pfunc = (size_t *) &pBufRW[PostGrow_CardTableFirstLocation];
            if (*pfunc != (size_t) g_card_table)
            {
                stompWBCompleteActions |= SWB_ICACHE_FLUSH;
                *pfunc = (size_t) g_card_table;
            }

            // What we're trying to update is the offset field of a
            // mov offset[edx], 0ffh instruction
            _ASSERTE(pBuf[34] == 0xC6);
            pfunc = (size_t *) &pBufRW[PostGrow_CardTableSecondLocation];
        }

        // Stick in the adjustment value.
        if (*pfunc != (size_t) g_card_table)
        {
            stompWBCompleteActions |= SWB_ICACHE_FLUSH;
            *pfunc = (size_t) g_card_table;
        }
    }

    if (bStompWriteBarrierEphemeral)
    {
        _ASSERTE(isRuntimeSuspended || (stompWBCompleteActions & SWB_EE_RESTART));
        stompWBCompleteActions |= StompWriteBarrierEphemeral(true);
    }
    return stompWBCompleteActions;
}

void FlushWriteBarrierInstructionCache()
{
    ClrFlushInstructionCache(GetWriteBarrierCodeLocation((BYTE*)JIT_PatchedWriteBarrierGroup),
        (BYTE*)JIT_PatchedWriteBarrierGroup_End - (BYTE*)JIT_PatchedWriteBarrierGroup, /* hasCodeExecutedBefore */ true);
}

