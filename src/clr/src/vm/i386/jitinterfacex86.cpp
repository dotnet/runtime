// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
#ifdef FEATURE_REMOTING
#include "remoting.h" // create context bound and remote class instances
#endif
#include "field.h"
#include "ecall.h"
#include "asmconstants.h"
#include "virtualcallstub.h"
#include "eventtrace.h"
#include "threadsuspend.h"

#if defined(_DEBUG) && !defined (WRITE_BARRIER_CHECK) 
#define WRITE_BARRIER_CHECK 1
#endif

// To test with MON_DEBUG off, comment out the following line. DO NOT simply define
// to be 0 as the checks are for #ifdef not #if 0.
// 
#ifdef _DEBUG 
#define MON_DEBUG 1
#endif

class generation;
extern "C" generation generation_table[];

extern "C" void STDCALL JIT_WriteBarrierReg_PreGrow();// JIThelp.asm/JIThelp.s
extern "C" void STDCALL JIT_WriteBarrierReg_PostGrow();// JIThelp.asm/JIThelp.s

#ifdef _DEBUG 
extern "C" void STDCALL WriteBarrierAssert(BYTE* ptr, Object* obj)
{
    STATIC_CONTRACT_SO_TOLERANT;
    WRAPPER_NO_CONTRACT;

    static BOOL fVerifyHeap = -1;

    if (fVerifyHeap == -1)
        fVerifyHeap = g_pConfig->GetHeapVerifyLevel() & EEConfig::HEAPVERIFY_GC;

    if (fVerifyHeap)
    {
        obj->Validate(FALSE);
        if(GCHeap::GetGCHeap()->IsHeapPointer(ptr))
        {
            Object* pObj = *(Object**)ptr;
            _ASSERTE (pObj == NULL || GCHeap::GetGCHeap()->IsHeapPointer(pObj));
        }
    }
    else
    {
        _ASSERTE((g_lowest_address <= ptr && ptr < g_highest_address) ||
             ((size_t)ptr < MAX_UNCHECKED_OFFSET_FOR_NULL_OBJECT));
    }
}

#endif // _DEBUG

/****************************************************************************/
/* assigns 'val to 'array[idx], after doing all the proper checks */

/* note that we can do almost as well in portable code, but this
   squezes the last little bit of perf out */

__declspec(naked) void F_CALL_CONV JIT_Stelem_Ref(PtrArray* array, unsigned idx, Object* val)
{
    STATIC_CONTRACT_SO_TOLERANT;
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;

    enum { CanCast = TypeHandle::CanCast,
#if CHECK_APP_DOMAIN_LEAKS 
           EEClassFlags = EEClass::AUXFLAG_APP_DOMAIN_AGILE |
                          EEClass::AUXFLAG_CHECK_APP_DOMAIN_AGILE,
#endif // CHECK_APP_DOMAIN_LEAKS
         };

    __asm {
        mov EAX, [ESP+4]            // EAX = val

        test ECX, ECX
        je ThrowNullReferenceException

        cmp EDX, [ECX+4];           // test if in bounds
        jae ThrowIndexOutOfRangeException

        test EAX, EAX
        jz Assigning0

#if CHECK_APP_DOMAIN_LEAKS 
        mov EAX,[g_pConfig]
        movzx EAX, [EAX]EEConfig.fAppDomainLeaks;
        test EAX, EAX
        jz NoCheck
        // Check if the instance is agile or check agile
        mov EAX, [ECX]
        mov EAX, [EAX]MethodTable.m_ElementTypeHnd
        test EAX, 2                 // Check for non-MT
        jnz NoCheck
        // Check VMflags of element type
        mov EAX, [EAX]MethodTable.m_pEEClass
        mov EAX, dword ptr [EAX]EEClass.m_wAuxFlags
        test EAX, EEClassFlags
        jnz NeedFrame             // Jump to the generic case so we can do an app domain check
 NoCheck:
        mov EAX, [ESP+4]            // EAX = val
#endif // CHECK_APP_DOMAIN_LEAKS

        push EDX
        mov EDX, [ECX]
        mov EDX, [EDX]MethodTable.m_ElementTypeHnd

        cmp EDX, [EAX]               // do we have an exact match
        jne NotExactMatch

DoWrite2:
        pop EDX
        lea EDX, [ECX + 4*EDX + 8]
        call JIT_WriteBarrierEAX
        ret     4

Assigning0:
        // write barrier is not necessary for assignment of NULL references
        mov     [ECX + 4*EDX + 8], EAX
        ret     4

DoWrite:
        mov EAX, [ESP+4]            // EAX = val
        lea EDX, [ECX + 4*EDX + 8]
        call JIT_WriteBarrierEAX
        ret     4

NotExactMatch:
        cmp EDX, [g_pObjectClass]   // are we assigning to Array of objects
        je DoWrite2

        // push EDX                 // caller-save ECX and EDX
        push ECX

        push EDX                    // element type handle
        push EAX                    // object

        call ObjIsInstanceOfNoGC

        pop ECX                     // caller-restore ECX and EDX
        pop EDX

        cmp EAX, CanCast
        je DoWrite

#if CHECK_APP_DOMAIN_LEAKS 
NeedFrame:
#endif
        // Call the helper that knows how to erect a frame
        push EDX
        push ECX

        lea ECX, [ESP+8+4]              // ECX = address of object being stored
        lea EDX, [ESP]                  // EDX = address of array

        call ArrayStoreCheck

        pop ECX                         // these might have been updated!
        pop EDX

        cmp EAX, EAX                    // set zero flag
        jnz Epilog                      // This jump never happens, it keeps the epilog walker happy

        jmp DoWrite

ThrowNullReferenceException:
        mov ECX, CORINFO_NullReferenceException
        jmp Throw

ThrowIndexOutOfRangeException:
        mov ECX, CORINFO_IndexOutOfRangeException

Throw:
        call    JIT_InternalThrowFromHelper
Epilog:
        ret     4
    }
}

extern "C" __declspec(naked) Object* F_CALL_CONV JIT_IsInstanceOfClass(MethodTable *pMT, Object *pObject)
{
    STATIC_CONTRACT_SO_TOLERANT;
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;

#if defined(FEATURE_TYPEEQUIVALENCE) || defined(FEATURE_REMOTING)
    enum
    {
        MTEquivalenceFlags = MethodTable::public_enum_flag_HasTypeEquivalence,
    };
#endif

    __asm
    {
        // Check if the instance is NULL
        test            ARGUMENT_REG2, ARGUMENT_REG2
        je              ReturnInst

        // Get the method table for the instance.
        mov             eax, dword ptr [ARGUMENT_REG2]

        // Check if they are the same.
        cmp             eax, ARGUMENT_REG1
        jne             CheckParent

    ReturnInst:
        // We matched the class.
        mov             eax, ARGUMENT_REG2
        ret

    // Check if the parent class matches.
    CheckParent:
        mov             eax, dword ptr [eax]MethodTable.m_pParentMethodTable
        cmp             eax, ARGUMENT_REG1
        je              ReturnInst

    // Check if we hit the top of the hierarchy.
        test            eax, eax
        jne             CheckParent

    // Check if the instance is a proxy.
#if defined(FEATURE_TYPEEQUIVALENCE) || defined(FEATURE_REMOTING)
        mov             eax, [ARGUMENT_REG2]
        test            dword ptr [eax]MethodTable.m_dwFlags, MTEquivalenceFlags
        jne             SlowPath
#endif
    // It didn't match and it isn't a proxy and it doesn't have type equivalence
        xor             eax, eax
        ret

    // Cast didn't match, so try the worker to check for the proxy/equivalence case.
#if defined(FEATURE_TYPEEQUIVALENCE) || defined(FEATURE_REMOTING)
    SlowPath:
        jmp             JITutil_IsInstanceOfAny
#endif            
    }
}

extern "C" __declspec(naked) Object* F_CALL_CONV JIT_ChkCastClass(MethodTable *pMT, Object *pObject)
{
    STATIC_CONTRACT_SO_TOLERANT;
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;

    __asm
    {
        // Check if the instance is NULL
        test            ARGUMENT_REG2, ARGUMENT_REG2
        je              ReturnInst

        // Get the method table for the instance.
        mov             eax, dword ptr [ARGUMENT_REG2]

        // Check if they are the same.
        cmp             eax, ARGUMENT_REG1
        jne             CheckParent

    ReturnInst:
        // We matched the class.
        mov             eax, ARGUMENT_REG2
        ret

    // Check if the parent class matches.
    CheckParent:
        mov             eax, dword ptr [eax]MethodTable.m_pParentMethodTable
        cmp             eax, ARGUMENT_REG1
        je              ReturnInst

    // Check if we hit the top of the hierarchy.
        test            eax, eax
        jne             CheckParent

    // Call out to JITutil_ChkCastAny to handle the proxy case and throw a rich
    // InvalidCastException in case of failure.
        jmp             JITutil_ChkCastAny
    }
}

extern "C" __declspec(naked) Object* F_CALL_CONV JIT_ChkCastClassSpecial(MethodTable *pMT, Object *pObject)
{
    STATIC_CONTRACT_SO_TOLERANT;
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;

    // Assumes that the check for the trivial cases has been inlined by the JIT.

    __asm
    {
        // Get the method table for the instance.
        mov             eax, dword ptr [ARGUMENT_REG2]

    // Check if the parent class matches.
    CheckParent:
        mov             eax, dword ptr [eax]MethodTable.m_pParentMethodTable
        cmp             eax, ARGUMENT_REG1
        jne             CheckNull

    // We matched the class.
        mov             eax, ARGUMENT_REG2
        ret

    CheckNull:
    // Check if we hit the top of the hierarchy.
        test            eax, eax
        jne             CheckParent

    // Call out to JITutil_ChkCastAny to handle the proxy case and throw a rich
    // InvalidCastException in case of failure.
        jmp             JITutil_ChkCastAny
    }
}

HCIMPL1_V(INT32, JIT_Dbl2IntOvf, double val)
{
    FCALL_CONTRACT;

    INT64 ret = HCCALL1_V(JIT_Dbl2Lng, val);

    if (ret != (INT32) ret)
        goto THROW;

    return (INT32) ret;

THROW:
    FCThrow(kOverflowException);
}
HCIMPLEND


FCDECL1(Object*, JIT_New, CORINFO_CLASS_HANDLE typeHnd_);

#ifdef FEATURE_REMOTING    
HCIMPL1(Object*, JIT_NewCrossContextHelper, CORINFO_CLASS_HANDLE typeHnd_)
{
    CONTRACTL
    {
        FCALL_CHECK;
    }
    CONTRACTL_END;

    TypeHandle typeHnd(typeHnd_);

    OBJECTREF newobj = NULL;
    HELPER_METHOD_FRAME_BEGIN_RET_0();    // Set up a frame

    _ASSERTE(!typeHnd.IsTypeDesc());                                   // we never use this helper for arrays
    MethodTable *pMT = typeHnd.AsMethodTable();
    pMT->CheckRestore();

    // Remoting services determines if the current context is appropriate
    // for activation. If the current context is OK then it creates an object
    // else it creates a proxy.
    // Note: 3/20/03 Added fIsNewObj flag to indicate that CreateProxyOrObject
    // is being called from Jit_NewObj ... the fIsCom flag is FALSE by default -
    // which used to be the case before this change as well.
    newobj = CRemotingServices::CreateProxyOrObject(pMT,FALSE /*fIsCom*/,TRUE/*fIsNewObj*/);

    HELPER_METHOD_FRAME_END();
    return(OBJECTREFToObject(newobj));
}
HCIMPLEND
#endif //  FEATURE_REMOTING    

HCIMPL1(Object*, AllocObjectWrapper, MethodTable *pMT)
{
    CONTRACTL
    {
        FCALL_CHECK;
    }
    CONTRACTL_END;

    OBJECTREF newObj = NULL;
    HELPER_METHOD_FRAME_BEGIN_RET_0();    // Set up a frame
    newObj = AllocateObject(pMT);
    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(newObj);
}
HCIMPLEND

/*********************************************************************/
// This is a frameless helper for allocating an object whose type derives
// from marshalbyref. We check quickly to see if it is configured to
// have remote activation. If not, we use the superfast allocator to
// allocate the object. Otherwise, we take the slow path of allocating
// the object via remoting services.
#ifdef FEATURE_REMOTING
__declspec(naked) Object* F_CALL_CONV JIT_NewCrossContext(CORINFO_CLASS_HANDLE typeHnd_)
{
    STATIC_CONTRACT_SO_TOLERANT;
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;

    _asm
    {
        // Check if remoting has been configured
        push ARGUMENT_REG1  // save registers
        push ARGUMENT_REG1
        call CRemotingServices::RequiresManagedActivation
        test eax, eax
        // Jump to the slow path
        jne SpecialOrXCtxHelper
#ifdef _DEBUG 
        push LL_INFO10
        push LF_GCALLOC
        call LoggingOn
        test eax, eax
        jne AllocWithLogHelper
#endif // _DEBUG

        // if the object doesn't have a finalizer and the size is small, jump to super fast asm helper
        mov     ARGUMENT_REG1, [esp]
        call    MethodTable::CannotUseSuperFastHelper
        test    eax, eax
        jne     FastHelper

        pop     ARGUMENT_REG1
        // Jump to the super fast helper
        jmp     dword ptr [hlpDynamicFuncTable + DYNAMIC_CORINFO_HELP_NEWSFAST * SIZE VMHELPDEF]VMHELPDEF.pfnHelper

FastHelper:
        pop     ARGUMENT_REG1
        // Jump to the helper
        jmp     JIT_New

SpecialOrXCtxHelper:
#ifdef FEATURE_COMINTEROP 
        test    eax, ComObjectType
        jz      XCtxHelper
        pop     ARGUMENT_REG1
        // Jump to the helper
        jmp     JIT_New

XCtxHelper:
#endif // FEATURE_COMINTEROP

        pop     ARGUMENT_REG1
        // Jump to the helper
        jmp     JIT_NewCrossContextHelper

#ifdef _DEBUG 
AllocWithLogHelper:
        pop     ARGUMENT_REG1
        // Jump to the helper
        jmp     AllocObjectWrapper
#endif // _DEBUG
    }
}
#endif // FEATURE_REMOTING


/*********************************************************************/
extern "C" void* g_TailCallFrameVptr;
void* g_TailCallFrameVptr;

#ifdef FEATURE_HIJACK
extern "C" void STDCALL JIT_TailCallHelper(Thread * pThread);
void STDCALL JIT_TailCallHelper(Thread * pThread)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    } CONTRACTL_END;

    pThread->UnhijackThread();
}
#endif // FEATURE_HIJACK

#if CHECK_APP_DOMAIN_LEAKS 
HCIMPL1(void *, SetObjectAppDomain, Object *pObject)
{
    FCALL_CONTRACT;
    DEBUG_ONLY_FUNCTION;

    HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB_NOPOLL(Frame::FRAME_ATTR_CAPTURE_DEPTH_2|Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_NO_THREAD_ABORT);
    pObject->SetAppDomain();
    HELPER_METHOD_FRAME_END();

    return pObject;
}
HCIMPLEND
#endif // CHECK_APP_DOMAIN_LEAKS

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

#if CHECK_APP_DOMAIN_LEAKS 
    EmitSetAppDomain(psl);
#endif

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
        psl->X86EmitCurrentThreadFetch(kEDX, (1<<kEAX)|(1<<kECX));

        // Try the allocation.


        if (flags & (ALIGN8 | SIZE_IN_EAX | ALIGN8OBJ))
        {
            // MOV EBX, [edx]Thread.m_alloc_context.alloc_ptr
            psl->X86EmitOffsetModRM(0x8B, kEBX, kEDX, offsetof(Thread, m_alloc_context) + offsetof(alloc_context, alloc_ptr));
            // add EAX, EBX
            psl->Emit16(0xC303);
            if (flags & ALIGN8)
                EmitAlignmentRoundup(psl, kEBX, kEAX, flags);      // bump EAX up size by 12 if EBX unaligned (so that we are aligned)
        }
        else
        {
            // add             eax, [edx]Thread.m_alloc_context.alloc_ptr
            psl->X86EmitOffsetModRM(0x03, kEAX, kEDX, offsetof(Thread, m_alloc_context) + offsetof(alloc_context, alloc_ptr));
        }

        // cmp             eax, [edx]Thread.m_alloc_context.alloc_limit
        psl->X86EmitOffsetModRM(0x3b, kEAX, kEDX, offsetof(Thread, m_alloc_context) + offsetof(alloc_context, alloc_limit));

        // ja              noAlloc
        psl->X86EmitCondJump(noAlloc, X86CondCode::kJA);

        // Fill in the allocation and get out.

        // mov             [edx]Thread.m_alloc_context.alloc_ptr, eax
        psl->X86EmitIndexRegStore(kEDX, offsetof(Thread, m_alloc_context) + offsetof(alloc_context, alloc_ptr), kEAX);

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
        // inc             dword ptr [m_GCLock]
        psl->Emit16(0x05ff);
        psl->Emit32((int)(size_t)&m_GCLock);

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

        // mov             eax, dword ptr [generation_table]
        psl->Emit8(0xA1);
        psl->Emit32((int)(size_t)&generation_table);

        // Try the allocation.
        // add             edx, eax
        psl->Emit16(0xd003);

        if (flags & (ALIGN8 | ALIGN8OBJ))
            EmitAlignmentRoundup(psl, kEAX, kEDX, flags);      // bump up EDX size by 12 if EAX unaligned (so that we are aligned)

        // cmp             edx, dword ptr [generation_table+4]
        psl->Emit16(0x153b);
        psl->Emit32((int)(size_t)&generation_table + 4);

        // ja              noAlloc
        psl->X86EmitCondJump(noAlloc, X86CondCode::kJA);

        // Fill in the allocation and get out.
        // mov             dword ptr [generation_table], edx
        psl->Emit16(0x1589);
        psl->Emit32((int)(size_t)&generation_table);

        if (flags & (ALIGN8 | ALIGN8OBJ))
            EmitDummyObject(psl, kEAX, flags);

        // mov             dword ptr [eax], ecx
        psl->X86EmitIndexRegStore(kEAX, 0, kECX);

        // mov             dword ptr [m_GCLock], 0FFFFFFFFh
        psl->Emit16(0x05C7);
        psl->Emit32((int)(size_t)&m_GCLock);
        psl->Emit32(0xFFFFFFFF);
    }


#ifdef INCREMENTAL_MEMCLR 
    // <TODO>We're planning to get rid of this anyhow according to Patrick</TODO>
    _ASSERTE(!"NYI");
#endif // INCREMENTAL_MEMCLR
}

#if CHECK_APP_DOMAIN_LEAKS 
void JIT_TrialAlloc::EmitSetAppDomain(CPUSTUBLINKER *psl)
{
    STANDARD_VM_CONTRACT;

    if (!g_pConfig->AppDomainLeaks())
        return;

    // At both entry & exit, eax contains the allocated object.
    // ecx is preserved, edx is not.

    //
    // Add in a call to SetAppDomain.  (Note that this
    // probably would have been easier to implement by just not using
    // the generated helpers in a checked build, but we'd lose code
    // coverage that way.)
    //

    // Save ECX over function call
    psl->X86EmitPushReg(kECX);

    // mov object to ECX
    // mov ecx, eax
    psl->Emit16(0xc88b);

    // SetObjectAppDomain pops its arg & returns object in EAX
    psl->X86EmitCall(psl->NewExternalCodeLabel((LPVOID)SetObjectAppDomain), 4);

    psl->X86EmitPopReg(kECX);
}

#endif // CHECK_APP_DOMAIN_LEAKS


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
        // mov             dword ptr [m_GCLock], 0FFFFFFFFh
        psl->Emit16(0x05c7);
        psl->Emit32((int)(size_t)&m_GCLock);
        psl->Emit32(0xFFFFFFFF);
    }
}

void *JIT_TrialAlloc::GenAllocSFast(Flags flags)
{
    STANDARD_VM_CONTRACT;

    CPUSTUBLINKER sl;

    CodeLabel *noLock  = sl.NewCodeLabel();
    CodeLabel *noAlloc = sl.NewCodeLabel();

    // Emit the main body of the trial allocator, be it SP or MP
    EmitCore(&sl, noLock, noAlloc, flags);

#if CHECK_APP_DOMAIN_LEAKS 
    EmitSetAppDomain(&sl);
#endif

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

    Stub *pStub = sl.Link(SystemDomain::GetGlobalLoaderAllocator()->GetExecutableHeap());

    return (void *)pStub->GetEntryPoint();
}


void *JIT_TrialAlloc::GenBox(Flags flags)
{
    STANDARD_VM_CONTRACT;

    CPUSTUBLINKER sl;

    CodeLabel *noLock  = sl.NewCodeLabel();
    CodeLabel *noAlloc = sl.NewCodeLabel();

    // Save address of value to be boxed
    sl.X86EmitPushReg(kEBX);
    sl.Emit16(0xda8b);

    // Save the MethodTable ptr
    sl.X86EmitPushReg(kECX);

    // mov             ecx, [ecx]MethodTable.m_pWriteableData
    sl.X86EmitOffsetModRM(0x8b, kECX, kECX, offsetof(MethodTable, m_pWriteableData));

    // Check whether the class has not been initialized
    // test [ecx]MethodTableWriteableData.m_dwFlags,MethodTableWriteableData::enum_flag_Unrestored
    sl.X86EmitOffsetModRM(0xf7, (X86Reg)0x0, kECX, offsetof(MethodTableWriteableData, m_dwFlags));
    sl.Emit32(MethodTableWriteableData::enum_flag_Unrestored);

    // Restore the MethodTable ptr in ecx
    sl.X86EmitPopReg(kECX);

    // jne              noAlloc
    sl.X86EmitCondJump(noAlloc, X86CondCode::kJNE);

    // Emit the main body of the trial allocator
    EmitCore(&sl, noLock, noAlloc, flags);

#if CHECK_APP_DOMAIN_LEAKS 
    EmitSetAppDomain(&sl);
#endif

    // Here we are at the end of the success case

    // Check whether the object contains pointers
    // test [ecx]MethodTable.m_dwFlags,MethodTable::enum_flag_ContainsPointers
    sl.X86EmitOffsetModRM(0xf7, (X86Reg)0x0, kECX, offsetof(MethodTable, m_dwFlags));
    sl.Emit32(MethodTable::enum_flag_ContainsPointers);

    CodeLabel *pointerLabel = sl.NewCodeLabel();

    // jne              pointerLabel
    sl.X86EmitCondJump(pointerLabel, X86CondCode::kJNE);

    // We have no pointers - emit a simple inline copy loop

    // mov             ecx, [ecx]MethodTable.m_BaseSize
    sl.X86EmitOffsetModRM(0x8b, kECX, kECX, offsetof(MethodTable, m_BaseSize));

    // sub ecx,12
    sl.X86EmitSubReg(kECX, 12);

    CodeLabel *loopLabel = sl.NewCodeLabel();

    sl.EmitLabel(loopLabel);

    // mov edx,[ebx+ecx]
    sl.X86EmitOp(0x8b, kEDX, kEBX, 0, kECX, 1);

    // mov [eax+ecx+4],edx
    sl.X86EmitOp(0x89, kEDX, kEAX, 4, kECX, 1);

    // sub ecx,4
    sl.X86EmitSubReg(kECX, 4);

    // jg loopLabel
    sl.X86EmitCondJump(loopLabel, X86CondCode::kJGE);

    sl.X86EmitPopReg(kEBX);

    sl.X86EmitReturn(0);

    // Arrive at this label if there are pointers in the object
    sl.EmitLabel(pointerLabel);

    // Do call to CopyValueClassUnchecked(object, data, pMT)

    // Pass pMT (still in ECX)
    sl.X86EmitPushReg(kECX);

    // Pass data (still in EBX)
    sl.X86EmitPushReg(kEBX);

    // Save the address of the object just allocated
    // mov ebx,eax
    sl.Emit16(0xD88B);


    // Pass address of first user byte in the newly allocated object
    sl.X86EmitAddReg(kEAX, 4);
    sl.X86EmitPushReg(kEAX);

    // call CopyValueClass
    sl.X86EmitCall(sl.NewExternalCodeLabel((LPVOID) CopyValueClassUnchecked), 12);

    // Restore the address of the newly allocated object and return it.
    // mov eax,ebx
    sl.Emit16(0xC38B);

    sl.X86EmitPopReg(kEBX);

    sl.X86EmitReturn(0);

    // Come here in case of no space
    sl.EmitLabel(noAlloc);

    // Release the lock in the uniprocessor case
    EmitNoAllocCode(&sl, flags);

    // Come here in case of failure to get the lock
    sl.EmitLabel(noLock);

    // Restore the address of the value to be boxed
    // mov edx,ebx
    sl.Emit16(0xD38B);

    // pop ebx
    sl.X86EmitPopReg(kEBX);

    // Jump to the slow version of JIT_Box
    sl.X86EmitNearJump(sl.NewExternalCodeLabel((LPVOID) JIT_Box));

    Stub *pStub = sl.Link(SystemDomain::GetGlobalLoaderAllocator()->GetExecutableHeap());

    return (void *)pStub->GetEntryPoint();
}


HCIMPL2_RAW(Object*, UnframedAllocateObjectArray, /*TypeHandle*/PVOID ArrayType, DWORD cElements)
{
    // This isn't _really_ an FCALL and therefore shouldn't have the 
    // SO_TOLERANT part of the FCALL_CONTRACT b/c it is not entered
    // from managed code.
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        SO_INTOLERANT;
    } CONTRACTL_END;

    return OBJECTREFToObject(AllocateArrayEx(TypeHandle::FromPtr(ArrayType),
                           (INT32 *)(&cElements),
                           1,
                           FALSE
                           DEBUG_ARG(FALSE)));
}
HCIMPLEND_RAW


HCIMPL2_RAW(Object*, UnframedAllocatePrimitiveArray, CorElementType type, DWORD cElements)
{
    // This isn't _really_ an FCALL and therefore shouldn't have the 
    // SO_TOLERANT part of the FCALL_CONTRACT b/c it is not entered
    // from managed code.
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        SO_INTOLERANT;
    } CONTRACTL_END;

    return OBJECTREFToObject( AllocatePrimitiveArray(type, cElements, FALSE) );
}
HCIMPLEND_RAW


void *JIT_TrialAlloc::GenAllocArray(Flags flags)
{
    STANDARD_VM_CONTRACT;

    CPUSTUBLINKER sl;

    CodeLabel *noLock  = sl.NewCodeLabel();
    CodeLabel *noAlloc = sl.NewCodeLabel();

    // We were passed a type descriptor in ECX, which contains the (shared)
    // array method table and the element type.

    // If this is the allocator for use from unmanaged code, ECX contains the
    // element type descriptor, or the CorElementType.

    // We need to save ECX for later

    // push ecx
    sl.X86EmitPushReg(kECX);

    // The element count is in EDX - we need to save it for later.

    // push edx
    sl.X86EmitPushReg(kEDX);

    if (flags & NO_FRAME)
    {
        if (flags & OBJ_ARRAY)
        {
            // we need to load the true method table from the type desc
            sl.X86EmitIndexRegLoad(kECX, kECX, offsetof(ArrayTypeDesc,m_TemplateMT)-2);
        }
        else
        {
            // mov ecx,[g_pPredefinedArrayTypes+ecx*4]
            sl.Emit8(0x8b);
            sl.Emit16(0x8d0c);
            sl.Emit32((int)(size_t)&g_pPredefinedArrayTypes);

            // test ecx,ecx
            sl.Emit16(0xc985);

            // je noLock
            sl.X86EmitCondJump(noLock, X86CondCode::kJZ);

            // we need to load the true method table from the type desc
            sl.X86EmitIndexRegLoad(kECX, kECX, offsetof(ArrayTypeDesc,m_TemplateMT));
        }
    }
    else
    {
        // we need to load the true method table from the type desc
        sl.X86EmitIndexRegLoad(kECX, kECX, offsetof(ArrayTypeDesc,m_TemplateMT)-2);

#ifdef FEATURE_PREJIT
        CodeLabel *indir = sl.NewCodeLabel();

        // test cl,1
        sl.Emit16(0xC1F6);
        sl.Emit8(0x01);

        // je indir
        sl.X86EmitCondJump(indir, X86CondCode::kJZ);

        // mov ecx, [ecx-1]
        sl.X86EmitIndexRegLoad(kECX, kECX, -1);

        sl.EmitLabel(indir);
#endif
    }

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


        // The large object heap is 8 byte aligned, so for double arrays we
        // want to bias toward putting things in the large object heap
    unsigned maxElems =  0xffff - 256;

    if ((flags & ALIGN8) && g_pConfig->GetDoubleArrayToLargeObjectHeapThreshold() < maxElems)
        maxElems = g_pConfig->GetDoubleArrayToLargeObjectHeapThreshold();
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

    // pop ecx - array type descriptor
    sl.X86EmitPopReg(kECX);

    // mov             dword ptr [eax]ArrayBase.m_NumComponents, edx
    sl.X86EmitIndexRegStore(kEAX, offsetof(ArrayBase,m_NumComponents), kEDX);

#if CHECK_APP_DOMAIN_LEAKS 
    EmitSetAppDomain(&sl);
#endif

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

    // pop ecx - array type descriptor
    sl.X86EmitPopReg(kECX);

    CodeLabel * target;
    if (flags & NO_FRAME)
    {
        if (flags & OBJ_ARRAY)
        {
            // Jump to the unframed helper
            target = sl.NewExternalCodeLabel((LPVOID)UnframedAllocateObjectArray);
            _ASSERTE(target->e.m_pExternalAddress);
        }
        else
        {
            // Jump to the unframed helper
            target = sl.NewExternalCodeLabel((LPVOID)UnframedAllocatePrimitiveArray);
            _ASSERTE(target->e.m_pExternalAddress);
        }
    }
    else
    {
        // Jump to the framed helper
        target = sl.NewExternalCodeLabel((LPVOID)JIT_NewArr1);
        _ASSERTE(target->e.m_pExternalAddress);
    }
    sl.X86EmitNearJump(target);

    Stub *pStub = sl.Link(SystemDomain::GetGlobalLoaderAllocator()->GetExecutableHeap());

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

    // mov ecx, [g_pStringMethodTable]
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

    // mov edx, [ecx]MethodTable.m_BaseSize
    sl.X86EmitIndexRegLoad(kEDX, kECX, offsetof(MethodTable,m_BaseSize));

    // Calculate the final size to allocate.
    // We need to calculate baseSize + cnt*2, then round that up by adding 3 and anding ~3.

    // lea eax, [edx+eax*2+5]
    sl.X86EmitOp(0x8d, kEAX, kEDX, (DATA_ALIGNMENT-1), kEAX, 2);

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

#if CHECK_APP_DOMAIN_LEAKS 
    EmitSetAppDomain(&sl);
#endif

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

    CodeLabel * target;
    if (flags & NO_FRAME)
    {
        // Jump to the unframed helper
        target = sl.NewExternalCodeLabel((LPVOID)UnframedAllocateString);
    }
    else
    {
        // Jump to the framed helper
        target = sl.NewExternalCodeLabel((LPVOID)FramedAllocateString);
    }
    sl.X86EmitNearJump(target);

    Stub *pStub = sl.Link(SystemDomain::GetGlobalLoaderAllocator()->GetExecutableHeap());

    return (void *)pStub->GetEntryPoint();
}


FastStringAllocatorFuncPtr fastStringAllocator = UnframedAllocateString;

FastObjectArrayAllocatorFuncPtr fastObjectArrayAllocator = UnframedAllocateObjectArray;

FastPrimitiveArrayAllocatorFuncPtr fastPrimitiveArrayAllocator = UnframedAllocatePrimitiveArray;

// For this helper,
// If bCCtorCheck == true
//          ECX contains the domain neutral module ID
//          EDX contains the class domain ID, and the
// else
//          ECX contains the domain neutral module ID
//          EDX is junk
// shared static base is returned in EAX.

// "init" should be the address of a routine which takes an argument of
// the module domain ID, the class domain ID, and returns the static base pointer
void EmitFastGetSharedStaticBase(CPUSTUBLINKER *psl, CodeLabel *init, bool bCCtorCheck, bool bGCStatic, bool bSingleAppDomain)
{
    STANDARD_VM_CONTRACT;

    CodeLabel *DoInit = 0;
    if (bCCtorCheck)
    {
        DoInit = psl->NewCodeLabel();
    }

    // mov eax, ecx
    psl->Emit8(0x89);
    psl->Emit8(0xc8);

    if(!bSingleAppDomain)
    {
        // Check tag
        CodeLabel *cctorCheck = psl->NewCodeLabel();


        // test eax, 1
        psl->Emit8(0xa9);
        psl->Emit32(1);

        // jz cctorCheck
        psl->X86EmitCondJump(cctorCheck, X86CondCode::kJZ);

        // mov eax GetAppDomain()
        psl->X86EmitCurrentAppDomainFetch(kEAX, (1<<kECX)|(1<<kEDX));

        // mov eax [eax->m_sDomainLocalBlock.m_pModuleSlots]
        psl->X86EmitIndexRegLoad(kEAX, kEAX, (__int32) AppDomain::GetOffsetOfModuleSlotsPointer());

        // Note: weird address arithmetic effectively does:
        // shift over 1 to remove tag bit (which is always 1), then multiply by 4.
        // mov eax [eax + ecx*2 - 2]
        psl->X86EmitOp(0x8b, kEAX, kEAX, -2, kECX, 2);

        // cctorCheck:
        psl->EmitLabel(cctorCheck);

    }

    if (bCCtorCheck)
    {
        // test [eax + edx + offsetof(DomainLocalModule, m_pDataBlob], ClassInitFlags::INITIALIZED_FLAG       // Is class inited
        _ASSERTE(FitsInI1(ClassInitFlags::INITIALIZED_FLAG));
        _ASSERTE(FitsInI1(DomainLocalModule::GetOffsetOfDataBlob()));

        BYTE testClassInit[] = { 0xF6, 0x44, 0x10,
            (BYTE) DomainLocalModule::GetOffsetOfDataBlob(), (BYTE)ClassInitFlags::INITIALIZED_FLAG };

        psl->EmitBytes(testClassInit, sizeof(testClassInit));

        // jz  init                                    // no, init it
        psl->X86EmitCondJump(DoInit, X86CondCode::kJZ);
    }

    if (bGCStatic)
    {
        // Indirect to get the pointer to the first GC Static
        psl->X86EmitIndexRegLoad(kEAX, kEAX, (__int32) DomainLocalModule::GetOffsetOfGCStaticPointer());
    }

    // ret
    psl->X86EmitReturn(0);

    if (bCCtorCheck)
    {
        // DoInit:
        psl->EmitLabel(DoInit);

        // push edx (must be preserved)
        psl->X86EmitPushReg(kEDX);

        // call init
        psl->X86EmitCall(init, 0);

        // pop edx
        psl->X86EmitPopReg(kEDX);

        // ret
        psl->X86EmitReturn(0);
    }

}

void *GenFastGetSharedStaticBase(bool bCheckCCtor, bool bGCStatic, bool bSingleAppDomain)
{
    STANDARD_VM_CONTRACT;

    CPUSTUBLINKER sl;

    CodeLabel *init;
    if (bGCStatic)
    {
        init = sl.NewExternalCodeLabel((LPVOID)JIT_GetSharedGCStaticBase);
    }
    else
    {
        init = sl.NewExternalCodeLabel((LPVOID)JIT_GetSharedNonGCStaticBase);
    }

    EmitFastGetSharedStaticBase(&sl, init, bCheckCCtor, bGCStatic, bSingleAppDomain);

    Stub *pStub = sl.Link(SystemDomain::GetGlobalLoaderAllocator()->GetExecutableHeap());

    return (void*) pStub->GetEntryPoint();
}


#ifdef ENABLE_FAST_GCPOLL_HELPER
void    EnableJitGCPoll()
{
    SetJitHelperFunction(CORINFO_HELP_POLL_GC, (void*)JIT_PollGC);
}
void    DisableJitGCPoll()
{
    SetJitHelperFunction(CORINFO_HELP_POLL_GC, (void*)JIT_PollGC_Nop);
}
#endif

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

#define DEBUG_RANDOM_BARRIER_CHECK DbgGetEXETimeStamp() % 7 == 4

/*********************************************************************/
// Initialize the part of the JIT helpers that require very little of
// EE infrastructure to be in place.
/*********************************************************************/
void InitJITHelpers1()
{
    STANDARD_VM_CONTRACT;

#define ETW_NUM_JIT_HELPERS 10
    static const LPCWSTR pHelperNames[ETW_NUM_JIT_HELPERS] = {
                                                      W("@NewObject"),
                                                      W("@NewObjectAlign8"),
                                                      W("@Box"),
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

    JIT_TrialAlloc::Flags flags = GCHeap::UseAllocationContexts() ?
        JIT_TrialAlloc::MP_ALLOCATOR : JIT_TrialAlloc::NORMAL;

    // Get CPU features and check for SSE2 support.
    // This code should eventually probably be moved into codeman.cpp,
    // where we set the cpu feature flags for the JIT based on CPU type and features.
    DWORD dwCPUFeaturesECX;
    DWORD dwCPUFeaturesEDX;

    __asm
    {
        pushad
        mov eax, 1
        cpuid
	mov dwCPUFeaturesECX, ecx
        mov dwCPUFeaturesEDX, edx
        popad
    }

    //  If bit 26 (SSE2) is set, then we can use the SSE2 flavors
    //  and faster x87 implementation for the P4 of Dbl2Lng.
    if (dwCPUFeaturesEDX & (1<<26))
    {
        SetJitHelperFunction(CORINFO_HELP_DBL2INT, JIT_Dbl2IntSSE2);
        if (dwCPUFeaturesECX & 1)  // check SSE3
        {
            SetJitHelperFunction(CORINFO_HELP_DBL2UINT, JIT_Dbl2LngSSE3);
            SetJitHelperFunction(CORINFO_HELP_DBL2LNG, JIT_Dbl2LngSSE3);
	}
        else
        {
            SetJitHelperFunction(CORINFO_HELP_DBL2UINT, JIT_Dbl2LngP4x87);   // SSE2 only for signed
            SetJitHelperFunction(CORINFO_HELP_DBL2LNG, JIT_Dbl2LngP4x87);
        }
    }

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
        pMethodAddresses[2] = JIT_TrialAlloc::GenBox(flags);
        SetJitHelperFunction(CORINFO_HELP_BOX, pMethodAddresses[2]);
        pMethodAddresses[3] = JIT_TrialAlloc::GenAllocArray((JIT_TrialAlloc::Flags)(flags|JIT_TrialAlloc::OBJ_ARRAY));
        SetJitHelperFunction(CORINFO_HELP_NEWARR_1_OBJ, pMethodAddresses[3]);
        pMethodAddresses[4] = JIT_TrialAlloc::GenAllocArray(flags);
        SetJitHelperFunction(CORINFO_HELP_NEWARR_1_VC, pMethodAddresses[4]);
        pMethodAddresses[5] = JIT_TrialAlloc::GenAllocArray((JIT_TrialAlloc::Flags)(flags|JIT_TrialAlloc::ALIGN8));
        SetJitHelperFunction(CORINFO_HELP_NEWARR_1_ALIGN8, pMethodAddresses[5]);

        fastObjectArrayAllocator = (FastObjectArrayAllocatorFuncPtr)JIT_TrialAlloc::GenAllocArray((JIT_TrialAlloc::Flags)(flags|JIT_TrialAlloc::NO_FRAME|JIT_TrialAlloc::OBJ_ARRAY));
        fastPrimitiveArrayAllocator = (FastPrimitiveArrayAllocatorFuncPtr)JIT_TrialAlloc::GenAllocArray((JIT_TrialAlloc::Flags)(flags|JIT_TrialAlloc::NO_FRAME));

        // If allocation logging is on, then we divert calls to FastAllocateString to an Ecall method, not this
        // generated method. Find this workaround in Ecall::Init() in ecall.cpp.
        ECall::DynamicallyAssignFCallImpl((PCODE) JIT_TrialAlloc::GenAllocString(flags), ECall::FastAllocateString);

        // generate another allocator for use from unmanaged code (won't need a frame)
        fastStringAllocator = (FastStringAllocatorFuncPtr) JIT_TrialAlloc::GenAllocString((JIT_TrialAlloc::Flags)(flags|JIT_TrialAlloc::NO_FRAME));
        //UnframedAllocateString;
    }

    bool bSingleAppDomain = IsSingleAppDomain();

    // Replace static helpers with faster assembly versions
    pMethodAddresses[6] = GenFastGetSharedStaticBase(true, true, bSingleAppDomain);
    SetJitHelperFunction(CORINFO_HELP_GETSHARED_GCSTATIC_BASE, pMethodAddresses[6]);
    pMethodAddresses[7] = GenFastGetSharedStaticBase(true, false, bSingleAppDomain);
    SetJitHelperFunction(CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE, pMethodAddresses[7]);
    pMethodAddresses[8] = GenFastGetSharedStaticBase(false, true, bSingleAppDomain);
    SetJitHelperFunction(CORINFO_HELP_GETSHARED_GCSTATIC_BASE_NOCTOR, pMethodAddresses[8]);
    pMethodAddresses[9] = GenFastGetSharedStaticBase(false, false, bSingleAppDomain);
    SetJitHelperFunction(CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE_NOCTOR, pMethodAddresses[9]);

    ETW::MethodLog::StubsInitialized(pMethodAddresses, (PVOID *)pHelperNames, ETW_NUM_JIT_HELPERS);

#ifdef ENABLE_FAST_GCPOLL_HELPER
    // code:JIT_PollGC_Nop
    SetJitHelperFunction(CORINFO_HELP_POLL_GC, (void*)JIT_PollGC_Nop);
#endif //ENABLE_FAST_GCPOLL_HELPER

    // All write barrier helpers should fit into one page.
    // If you hit this assert on retail build, there is most likely problem with BBT script.
    _ASSERTE_ALL_BUILDS("clr/src/VM/i386/JITinterfaceX86.cpp", (BYTE*)JIT_WriteBarrierLast - (BYTE*)JIT_WriteBarrierStart < PAGE_SIZE);
    _ASSERTE_ALL_BUILDS("clr/src/VM/i386/JITinterfaceX86.cpp", (BYTE*)JIT_PatchedWriteBarrierLast - (BYTE*)JIT_PatchedWriteBarrierStart < PAGE_SIZE);

    // Copy the write barriers to their final resting place.
    for (int iBarrier = 0; iBarrier < NUM_WRITE_BARRIERS; iBarrier++)
    {
        BYTE * pfunc = (BYTE *) JIT_WriteBarrierReg_PreGrow;

        BYTE * pBuf = (BYTE *)c_rgWriteBarriers[iBarrier];
        int reg = c_rgWriteBarrierRegs[iBarrier];

        memcpy(pBuf, pfunc, 34);

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
        pBuf[1] &= 0xc7;
        pBuf[1] |= reg << 3;

        // Second instruction to patch is cmp reg, imm32 (low bound)

        _ASSERTE(pBuf[2] == 0x81);
        // Here the lowest three bits in ModR/M field are the register
        pBuf[3] &= 0xf8;
        pBuf[3] |= reg;

#ifdef WRITE_BARRIER_CHECK 
        // Don't do the fancy optimization just jump to the old one
        // Use the slow one from time to time in a debug build because
        // there are some good asserts in the unoptimized one
        if ((g_pConfig->GetHeapVerifyLevel() & EEConfig::HEAPVERIFY_BARRIERCHECK) || DEBUG_RANDOM_BARRIER_CHECK) {
            pfunc = &pBuf[0];
            *pfunc++ = 0xE9;                // JMP c_rgDebugWriteBarriers[iBarrier]
            *((DWORD*) pfunc) = (BYTE*) c_rgDebugWriteBarriers[iBarrier] - (pfunc + sizeof(DWORD));
        }
#endif // WRITE_BARRIER_CHECK
    }

#ifndef CODECOVERAGE
    ValidateWriteBarrierHelpers();
#endif

    // Leave the patched region writable for StompWriteBarrierEphemeral(), StompWriteBarrierResize()
    // and CTPMethodTable::ActivatePrecodeRemotingThunk

    // Initialize g_TailCallFrameVptr for JIT_TailCall helper
    g_TailCallFrameVptr = (void*)TailCallFrame::GetMethodFrameVPtr();
}

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
                            // because the instrumented binaries will not preserve alignmant constraits and we will fail.

void ValidateWriteBarrierHelpers()
{
    // we have an invariant that the addresses of all the values that we update in our write barrier
    // helpers must be naturally aligned, this is so that the update can happen atomically since there
    // are places where we update these values while the EE is running

#ifdef WRITE_BARRIER_CHECK
    // write barrier checking uses the slower helpers that we don't bash so there is no need for validation
    if ((g_pConfig->GetHeapVerifyLevel() & EEConfig::HEAPVERIFY_BARRIERCHECK) || DEBUG_RANDOM_BARRIER_CHECK)
        return;
#endif // WRITE_BARRIER_CHECK
    
    // first validate the PreGrow helper
    BYTE* pWriteBarrierFunc = reinterpret_cast<BYTE*>(JIT_WriteBarrierEAX);

    // ephemeral region
    DWORD* pLocation = reinterpret_cast<DWORD*>(&pWriteBarrierFunc[AnyGrow_EphemeralLowerBound]);
    _ASSERTE_ALL_BUILDS("clr/src/VM/i386/JITinterfaceX86.cpp", (reinterpret_cast<DWORD>(pLocation) & 0x3) == 0);
    _ASSERTE_ALL_BUILDS("clr/src/VM/i386/JITinterfaceX86.cpp", *pLocation == 0xf0f0f0f0);

    // card table
    pLocation = reinterpret_cast<DWORD*>(&pWriteBarrierFunc[PreGrow_CardTableFirstLocation]);
    _ASSERTE_ALL_BUILDS("clr/src/VM/i386/JITinterfaceX86.cpp", (reinterpret_cast<DWORD>(pLocation) & 0x3) == 0);
    _ASSERTE_ALL_BUILDS("clr/src/VM/i386/JITinterfaceX86.cpp", *pLocation == 0xf0f0f0f0);
    pLocation = reinterpret_cast<DWORD*>(&pWriteBarrierFunc[PreGrow_CardTableSecondLocation]);
    _ASSERTE_ALL_BUILDS("clr/src/VM/i386/JITinterfaceX86.cpp", (reinterpret_cast<DWORD>(pLocation) & 0x3) == 0);
    _ASSERTE_ALL_BUILDS("clr/src/VM/i386/JITinterfaceX86.cpp", *pLocation == 0xf0f0f0f0);

    // now validate the PostGrow helper
    pWriteBarrierFunc = reinterpret_cast<BYTE*>(JIT_WriteBarrierReg_PostGrow);

    // ephemeral region
    pLocation = reinterpret_cast<DWORD*>(&pWriteBarrierFunc[AnyGrow_EphemeralLowerBound]);
    _ASSERTE_ALL_BUILDS("clr/src/VM/i386/JITinterfaceX86.cpp", (reinterpret_cast<DWORD>(pLocation) & 0x3) == 0);
    _ASSERTE_ALL_BUILDS("clr/src/VM/i386/JITinterfaceX86.cpp", *pLocation == 0xf0f0f0f0);
    pLocation = reinterpret_cast<DWORD*>(&pWriteBarrierFunc[PostGrow_EphemeralUpperBound]);
    _ASSERTE_ALL_BUILDS("clr/src/VM/i386/JITinterfaceX86.cpp", (reinterpret_cast<DWORD>(pLocation) & 0x3) == 0);
    _ASSERTE_ALL_BUILDS("clr/src/VM/i386/JITinterfaceX86.cpp", *pLocation == 0xf0f0f0f0);

    // card table
    pLocation = reinterpret_cast<DWORD*>(&pWriteBarrierFunc[PostGrow_CardTableFirstLocation]);
    _ASSERTE_ALL_BUILDS("clr/src/VM/i386/JITinterfaceX86.cpp", (reinterpret_cast<DWORD>(pLocation) & 0x3) == 0);
    _ASSERTE_ALL_BUILDS("clr/src/VM/i386/JITinterfaceX86.cpp", *pLocation == 0xf0f0f0f0);
    pLocation = reinterpret_cast<DWORD*>(&pWriteBarrierFunc[PostGrow_CardTableSecondLocation]);
    _ASSERTE_ALL_BUILDS("clr/src/VM/i386/JITinterfaceX86.cpp", (reinterpret_cast<DWORD>(pLocation) & 0x3) == 0);
    _ASSERTE_ALL_BUILDS("clr/src/VM/i386/JITinterfaceX86.cpp", *pLocation == 0xf0f0f0f0);
}

#endif //CODECOVERAGE
/*********************************************************************/

#define WriteBarrierIsPreGrow() (((BYTE *)JIT_WriteBarrierEAX)[10] == 0xc1)


/*********************************************************************/
// When a GC happens, the upper and lower bounds of the ephemeral
// generation change.  This routine updates the WriteBarrier thunks
// with the new values.
void StompWriteBarrierEphemeral(bool /* isRuntimeSuspended */)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

#ifdef WRITE_BARRIER_CHECK 
        // Don't do the fancy optimization if we are checking write barrier
    if (((BYTE *)JIT_WriteBarrierEAX)[0] == 0xE9)  // we are using slow write barrier
        return;
#endif // WRITE_BARRIER_CHECK

    BOOL flushICache = FALSE;

    // Update the lower bound.
    for (int iBarrier = 0; iBarrier < NUM_WRITE_BARRIERS; iBarrier++)
    {
        BYTE * pBuf = (BYTE *)c_rgWriteBarriers[iBarrier];

        // assert there is in fact a cmp r/m32, imm32 there
        _ASSERTE(pBuf[2] == 0x81);

        // Update the immediate which is the lower bound of the ephemeral generation
        size_t *pfunc = (size_t *) &pBuf[AnyGrow_EphemeralLowerBound];
        //avoid trivial self modifying code
        if (*pfunc != (size_t) g_ephemeral_low)
        {
            flushICache = TRUE;
            *pfunc = (size_t) g_ephemeral_low;
        }
        if (!WriteBarrierIsPreGrow())
        {
            // assert there is in fact a cmp r/m32, imm32 there
            _ASSERTE(pBuf[10] == 0x81);

                // Update the upper bound if we are using the PostGrow thunk.
            pfunc = (size_t *) &pBuf[PostGrow_EphemeralUpperBound];
            //avoid trivial self modifying code
            if (*pfunc != (size_t) g_ephemeral_high)
            {
                flushICache = TRUE;
                *pfunc = (size_t) g_ephemeral_high;
            }
        }
    }

    if (flushICache)
        FlushInstructionCache(GetCurrentProcess(), (void *)JIT_PatchedWriteBarrierStart,
            (BYTE*)JIT_PatchedWriteBarrierLast - (BYTE*)JIT_PatchedWriteBarrierStart);
}

/*********************************************************************/
// When the GC heap grows, the ephemeral generation may no longer
// be after the older generations.  If this happens, we need to switch
// to the PostGrow thunk that checks both upper and lower bounds.
// regardless we need to update the thunk with the
// card_table - lowest_address.
void StompWriteBarrierResize(bool isRuntimeSuspended, bool bReqUpperBoundsCheck)
{
    CONTRACTL {
        NOTHROW;
        if (GetThread()) {GC_TRIGGERS;} else {GC_NOTRIGGER;}
    } CONTRACTL_END;

#ifdef WRITE_BARRIER_CHECK 
        // Don't do the fancy optimization if we are checking write barrier
    if (((BYTE *)JIT_WriteBarrierEAX)[0] == 0xE9)  // we are using slow write barrier
        return;
#endif // WRITE_BARRIER_CHECK

    bool bWriteBarrierIsPreGrow = WriteBarrierIsPreGrow();
    bool bStompWriteBarrierEphemeral = false;

    BOOL bEESuspendedHere = FALSE;

    for (int iBarrier = 0; iBarrier < NUM_WRITE_BARRIERS; iBarrier++)
    {
        BYTE * pBuf = (BYTE *)c_rgWriteBarriers[iBarrier];
        int reg = c_rgWriteBarrierRegs[iBarrier];

        size_t *pfunc;

    // Check if we are still using the pre-grow version of the write barrier.
        if (bWriteBarrierIsPreGrow)
        {
            // Check if we need to use the upper bounds checking barrier stub.
            if (bReqUpperBoundsCheck)
            {
                GCX_MAYBE_COOP_NO_THREAD_BROKEN((GetThread()!=NULL));
                if( !isRuntimeSuspended && !bEESuspendedHere) {
                    ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_FOR_GC_PREP);
                    bEESuspendedHere = TRUE;
                }

                pfunc = (size_t *) JIT_WriteBarrierReg_PostGrow;
                memcpy(pBuf, pfunc, 42);

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
                pBuf[1] &= 0xc7;
                pBuf[1] |= reg << 3;

                // Second instruction to patch is cmp reg, imm32 (low bound)

                _ASSERTE(pBuf[2] == 0x81);
                // Here the lowest three bits in ModR/M field are the register
                pBuf[3] &= 0xf8;
                pBuf[3] |= reg;

                // Third instruction to patch is another cmp reg, imm32 (high bound)

                _ASSERTE(pBuf[10] == 0x81);
                // Here the lowest three bits in ModR/M field are the register
                pBuf[11] &= 0xf8;
                pBuf[11] |= reg;

                bStompWriteBarrierEphemeral = true;
                // What we're trying to update is the offset field of a

                // cmp offset[edx], 0ffh instruction
                _ASSERTE(pBuf[22] == 0x80);
                pfunc = (size_t *) &pBuf[PostGrow_CardTableFirstLocation];
               *pfunc = (size_t) g_card_table;

                // What we're trying to update is the offset field of a
                // mov offset[edx], 0ffh instruction
                _ASSERTE(pBuf[34] == 0xC6);
                pfunc = (size_t *) &pBuf[PostGrow_CardTableSecondLocation];

            }
            else
            {
                // What we're trying to update is the offset field of a

                // cmp offset[edx], 0ffh instruction
                _ASSERTE(pBuf[14] == 0x80);
                pfunc = (size_t *) &pBuf[PreGrow_CardTableFirstLocation];
               *pfunc = (size_t) g_card_table;

                // What we're trying to update is the offset field of a

                // mov offset[edx], 0ffh instruction
                _ASSERTE(pBuf[26] == 0xC6);
                pfunc = (size_t *) &pBuf[PreGrow_CardTableSecondLocation];
            }
        }
        else
        {
            // What we're trying to update is the offset field of a

            // cmp offset[edx], 0ffh instruction
            _ASSERTE(pBuf[22] == 0x80);
            pfunc = (size_t *) &pBuf[PostGrow_CardTableFirstLocation];
           *pfunc = (size_t) g_card_table;

            // What we're trying to update is the offset field of a
            // mov offset[edx], 0ffh instruction
            _ASSERTE(pBuf[34] == 0xC6);
            pfunc = (size_t *) &pBuf[PostGrow_CardTableSecondLocation];
        }

        // Stick in the adjustment value.
        *pfunc = (size_t) g_card_table;
    }

    if (bStompWriteBarrierEphemeral)
    {
        _ASSERTE(isRuntimeSuspended || bEESuspendedHere);
        StompWriteBarrierEphemeral(true);
    }
    else
    {
        FlushInstructionCache(GetCurrentProcess(), (void *)JIT_PatchedWriteBarrierStart,
            (BYTE*)JIT_PatchedWriteBarrierLast - (BYTE*)JIT_PatchedWriteBarrierStart);
    }

    if(bEESuspendedHere)
        ThreadSuspend::RestartEE(FALSE, TRUE);
}

