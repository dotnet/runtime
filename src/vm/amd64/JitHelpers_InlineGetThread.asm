; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.
; See the LICENSE file in the project root for more information.

; ==++==
;

;
; ==--==
; ***********************************************************************
; File: JitHelpers_InlineGetThread.asm, see history in jithelp.asm
;
; Notes: These routinues will be patched at runtime with the location in 
;        the TLS to find the Thread* and are the fastest implementation 
;        of their specific functionality.
; ***********************************************************************

include AsmMacros.inc
include asmconstants.inc

; Min amount of stack space that a nested function should allocate.
MIN_SIZE equ 28h

; Macro to create a patchable inline GetAppdomain, if we decide to create patchable
; high TLS inline versions then just change this macro to make sure to create enough
; space in the asm to patch the high TLS getter instructions.
PATCHABLE_INLINE_GETTHREAD macro Reg, PatchLabel
PATCH_LABEL PatchLabel
        mov     Reg, gs:[OFFSET__TEB__TlsSlots]
        endm


JIT_NEW                 equ     ?JIT_New@@YAPEAVObject@@PEAUCORINFO_CLASS_STRUCT_@@@Z
Object__DEBUG_SetAppDomain equ ?DEBUG_SetAppDomain@Object@@QEAAXPEAVAppDomain@@@Z
CopyValueClassUnchecked equ     ?CopyValueClassUnchecked@@YAXPEAX0PEAVMethodTable@@@Z
JIT_Box                 equ     ?JIT_Box@@YAPEAVObject@@PEAUCORINFO_CLASS_STRUCT_@@PEAX@Z
g_pStringClass          equ     ?g_pStringClass@@3PEAVMethodTable@@EA
FramedAllocateString    equ     ?FramedAllocateString@@YAPEAVStringObject@@K@Z
JIT_NewArr1             equ     ?JIT_NewArr1@@YAPEAVObject@@PEAUCORINFO_CLASS_STRUCT_@@_J@Z

INVALIDGCVALUE          equ     0CCCCCCCDh

extern JIT_NEW:proc
extern CopyValueClassUnchecked:proc
extern JIT_Box:proc
extern g_pStringClass:QWORD
extern FramedAllocateString:proc
extern JIT_NewArr1:proc

extern JIT_InternalThrow:proc

ifdef _DEBUG
extern DEBUG_TrialAllocSetAppDomain:proc
extern DEBUG_TrialAllocSetAppDomain_NoScratchArea:proc
endif

; IN: rcx: MethodTable*
; OUT: rax: new object
LEAF_ENTRY JIT_TrialAllocSFastMP_InlineGetThread, _TEXT
        mov     edx, [rcx + OFFSET__MethodTable__m_BaseSize]

        ; m_BaseSize is guaranteed to be a multiple of 8.

        PATCHABLE_INLINE_GETTHREAD r11, JIT_TrialAllocSFastMP_InlineGetThread__PatchTLSOffset
        mov     r10, [r11 + OFFSET__Thread__m_alloc_context__alloc_limit]
        mov     rax, [r11 + OFFSET__Thread__m_alloc_context__alloc_ptr]

        add     rdx, rax

        cmp     rdx, r10
        ja      AllocFailed

        mov     [r11 + OFFSET__Thread__m_alloc_context__alloc_ptr], rdx
        mov     [rax], rcx

ifdef _DEBUG
        call    DEBUG_TrialAllocSetAppDomain_NoScratchArea
endif ; _DEBUG

        ret

    AllocFailed:
        jmp     JIT_NEW
LEAF_END JIT_TrialAllocSFastMP_InlineGetThread, _TEXT

; HCIMPL2(Object*, JIT_Box, CORINFO_CLASS_HANDLE type, void* unboxedData)
NESTED_ENTRY JIT_BoxFastMP_InlineGetThread, _TEXT
        mov     rax, [rcx + OFFSETOF__MethodTable__m_pWriteableData]

        ; Check whether the class has not been initialized
        test    dword ptr [rax + OFFSETOF__MethodTableWriteableData__m_dwFlags], MethodTableWriteableData__enum_flag_Unrestored
        jnz     ClassNotInited

        mov     r8d, [rcx + OFFSET__MethodTable__m_BaseSize]

        ; m_BaseSize is guaranteed to be a multiple of 8.

        PATCHABLE_INLINE_GETTHREAD r11, JIT_BoxFastMPIGT__PatchTLSLabel
        mov     r10, [r11 + OFFSET__Thread__m_alloc_context__alloc_limit]
        mov     rax, [r11 + OFFSET__Thread__m_alloc_context__alloc_ptr]

        add     r8, rax

        cmp     r8, r10
        ja      AllocFailed

        mov     [r11 + OFFSET__Thread__m_alloc_context__alloc_ptr], r8
        mov     [rax], rcx

ifdef _DEBUG
        call    DEBUG_TrialAllocSetAppDomain_NoScratchArea
endif ; _DEBUG

        ; Check whether the object contains pointers
        test    dword ptr [rcx + OFFSETOF__MethodTable__m_dwFlags], MethodTable__enum_flag_ContainsPointers
        jnz     ContainsPointers

        ; We have no pointers - emit a simple inline copy loop
        ; Copy the contents from the end
        mov     ecx, [rcx + OFFSET__MethodTable__m_BaseSize]
        sub     ecx, 18h  ; sizeof(ObjHeader) + sizeof(Object) + last slot

align 16
    CopyLoop:
        mov     r8, [rdx+rcx]
        mov     [rax+rcx+8], r8
        sub     ecx, 8
        jge     CopyLoop
        REPRET

    ContainsPointers:
        ; Do call to CopyValueClassUnchecked(object, data, pMT)
        push_vol_reg rax
        alloc_stack 20h
        END_PROLOGUE

        mov     r8, rcx
        lea     rcx, [rax + 8]
        call    CopyValueClassUnchecked

        add     rsp, 20h
        pop     rax
        ret

    ClassNotInited:
    AllocFailed:
        jmp     JIT_Box
NESTED_END JIT_BoxFastMP_InlineGetThread, _TEXT

FIX_INDIRECTION macro Reg
ifdef FEATURE_PREJIT
        test    Reg, 1
        jz      @F
        mov     Reg, [Reg-1]
    @@:
endif
endm

LEAF_ENTRY AllocateStringFastMP_InlineGetThread, _TEXT
        ; We were passed the number of characters in ECX

        ; we need to load the method table for string from the global
        mov     r9, [g_pStringClass]

        ; Instead of doing elaborate overflow checks, we just limit the number of elements
        ; to (LARGE_OBJECT_SIZE - 256)/sizeof(WCHAR) or less.
        ; This will avoid all overflow problems, as well as making sure
        ; big string objects are correctly allocated in the big object heap.

        cmp     ecx, (ASM_LARGE_OBJECT_SIZE - 256)/2
        jae     OversizedString

        mov     edx, [r9 + OFFSET__MethodTable__m_BaseSize]

        ; Calculate the final size to allocate.
        ; We need to calculate baseSize + cnt*2, then round that up by adding 7 and anding ~7.

        lea     edx, [edx + ecx*2 + 7]
        and     edx, -8

        PATCHABLE_INLINE_GETTHREAD r11, AllocateStringFastMP_InlineGetThread__PatchTLSOffset
        mov     r10, [r11 + OFFSET__Thread__m_alloc_context__alloc_limit]
        mov     rax, [r11 + OFFSET__Thread__m_alloc_context__alloc_ptr]

        add     rdx, rax

        cmp     rdx, r10
        ja      AllocFailed

        mov     [r11 + OFFSET__Thread__m_alloc_context__alloc_ptr], rdx
        mov     [rax], r9

        mov     [rax + OFFSETOF__StringObject__m_StringLength], ecx

ifdef _DEBUG
        call    DEBUG_TrialAllocSetAppDomain_NoScratchArea
endif ; _DEBUG

        ret

    OversizedString:
    AllocFailed:
        jmp     FramedAllocateString
LEAF_END AllocateStringFastMP_InlineGetThread, _TEXT

; HCIMPL2(Object*, JIT_NewArr1, CORINFO_CLASS_HANDLE arrayTypeHnd_, INT_PTR size)
LEAF_ENTRY JIT_NewArr1VC_MP_InlineGetThread, _TEXT
        ; We were passed a type descriptor in RCX, which contains the (shared)
        ; array method table and the element type.

        ; The element count is in RDX

        ; NOTE: if this code is ported for CORINFO_HELP_NEWSFAST_ALIGN8, it will need
        ; to emulate the double-specific behavior of JIT_TrialAlloc::GenAllocArray.

        ; Do a conservative check here.  This is to avoid overflow while doing the calculations.  We don't
        ; have to worry about "large" objects, since the allocation quantum is never big enough for
        ; LARGE_OBJECT_SIZE.

        ; For Value Classes, this needs to be 2^16 - slack (2^32 / max component size), 
        ; The slack includes the size for the array header and round-up ; for alignment.  Use 256 for the
        ; slack value out of laziness.

        ; In both cases we do a final overflow check after adding to the alloc_ptr.

        ; we need to load the true method table from the type desc
        mov     r9, [rcx + OFFSETOF__ArrayTypeDesc__m_TemplateMT - 2]
        
        FIX_INDIRECTION r9

        cmp     rdx, (65535 - 256)
        jae     OversizedArray

        movzx   r8d, word ptr [r9 + OFFSETOF__MethodTable__m_dwFlags]  ; component size is low 16 bits
        imul    r8d, edx
        add     r8d, dword ptr [r9 + OFFSET__MethodTable__m_BaseSize] 

        ; round the size to a multiple of 8

        add     r8d, 7
        and     r8d, -8


        PATCHABLE_INLINE_GETTHREAD r11, JIT_NewArr1VC_MP_InlineGetThread__PatchTLSOffset
        mov     r10, [r11 + OFFSET__Thread__m_alloc_context__alloc_limit]
        mov     rax, [r11 + OFFSET__Thread__m_alloc_context__alloc_ptr]

        add     r8, rax
        jc      AllocFailed

        cmp     r8, r10
        ja      AllocFailed

        mov     [r11 + OFFSET__Thread__m_alloc_context__alloc_ptr], r8
        mov     [rax], r9

        mov     dword ptr [rax + OFFSETOF__ArrayBase__m_NumComponents], edx

ifdef _DEBUG
        call    DEBUG_TrialAllocSetAppDomain_NoScratchArea
endif ; _DEBUG

        ret

    OversizedArray:
    AllocFailed:
        jmp     JIT_NewArr1
LEAF_END JIT_NewArr1VC_MP_InlineGetThread, _TEXT


; HCIMPL2(Object*, JIT_NewArr1, CORINFO_CLASS_HANDLE arrayTypeHnd_, INT_PTR size)
LEAF_ENTRY JIT_NewArr1OBJ_MP_InlineGetThread, _TEXT
        ; We were passed a type descriptor in RCX, which contains the (shared)
        ; array method table and the element type.

        ; The element count is in RDX

        ; NOTE: if this code is ported for CORINFO_HELP_NEWSFAST_ALIGN8, it will need
        ; to emulate the double-specific behavior of JIT_TrialAlloc::GenAllocArray.

        ; Verifies that LARGE_OBJECT_SIZE fits in 32-bit.  This allows us to do array size
        ; arithmetic using 32-bit registers.
        .erre ASM_LARGE_OBJECT_SIZE lt 100000000h

        cmp     rdx, (ASM_LARGE_OBJECT_SIZE - 256)/8 ; sizeof(void*)
        jae     OversizedArray

        ; we need to load the true method table from the type desc
        mov     r9, [rcx + OFFSETOF__ArrayTypeDesc__m_TemplateMT - 2]
        
        FIX_INDIRECTION r9

        ; In this case we know the element size is sizeof(void *), or 8 for x64
        ; This helps us in two ways - we can shift instead of multiplying, and
        ; there's no need to align the size either

        mov     r8d, dword ptr [r9 + OFFSET__MethodTable__m_BaseSize]
        lea     r8d, [r8d + edx * 8]

        ; No need for rounding in this case - element size is 8, and m_BaseSize is guaranteed
        ; to be a multiple of 8.

        PATCHABLE_INLINE_GETTHREAD r11, JIT_NewArr1OBJ_MP_InlineGetThread__PatchTLSOffset
        mov     r10, [r11 + OFFSET__Thread__m_alloc_context__alloc_limit]
        mov     rax, [r11 + OFFSET__Thread__m_alloc_context__alloc_ptr]

        add     r8, rax

        cmp     r8, r10
        ja      AllocFailed

        mov     [r11 + OFFSET__Thread__m_alloc_context__alloc_ptr], r8
        mov     [rax], r9

        mov     dword ptr [rax + OFFSETOF__ArrayBase__m_NumComponents], edx

ifdef _DEBUG
        call    DEBUG_TrialAllocSetAppDomain_NoScratchArea
endif ; _DEBUG

        ret

    OversizedArray:
    AllocFailed:
        jmp     JIT_NewArr1
LEAF_END JIT_NewArr1OBJ_MP_InlineGetThread, _TEXT


MON_ENTER_STACK_SIZE                equ     00000020h
MON_EXIT_STACK_SIZE                 equ     00000068h

ifdef MON_DEBUG
ifdef TRACK_SYNC
MON_ENTER_STACK_SIZE_INLINEGETTHREAD equ     00000020h
MON_EXIT_STACK_SIZE_INLINEGETTHREAD  equ     00000068h
endif
endif

BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX    equ     08000000h   ; syncblk.h
BIT_SBLK_IS_HASHCODE                equ     04000000h   ; syncblk.h
BIT_SBLK_SPIN_LOCK                  equ     10000000h   ; syncblk.h

SBLK_MASK_LOCK_THREADID             equ     000003FFh   ; syncblk.h
SBLK_LOCK_RECLEVEL_INC              equ     00000400h   ; syncblk.h
SBLK_MASK_LOCK_RECLEVEL             equ     0000FC00h   ; syncblk.h

MASK_SYNCBLOCKINDEX                 equ     03FFFFFFh   ; syncblk.h
STATE_CHECK                         equ    0FFFFFFFEh

MT_CTX_PROXY_FLAG                   equ     10000000h

g_pSyncTable    equ ?g_pSyncTable@@3PEAVSyncTableEntry@@EA
g_SystemInfo    equ ?g_SystemInfo@@3U_SYSTEM_INFO@@A
g_SpinConstants equ ?g_SpinConstants@@3USpinConstants@@A

extern g_pSyncTable:QWORD
extern g_SystemInfo:QWORD
extern g_SpinConstants:QWORD

; JITutil_MonEnterWorker(Object* obj, BYTE* pbLockTaken)
extern JITutil_MonEnterWorker:proc
; JITutil_MonTryEnter(Object* obj, INT32 timeout, BYTE* pbLockTaken)
extern JITutil_MonTryEnter:proc
; JITutil_MonExitWorker(Object* obj, BYTE* pbLockTaken)
extern JITutil_MonExitWorker:proc
; JITutil_MonSignal(AwareLock* lock, BYTE* pbLockTaken)
extern JITutil_MonSignal:proc
; JITutil_MonContention(AwareLock* lock, BYTE* pbLockTaken)
extern JITutil_MonContention:proc

ifdef _DEBUG
MON_DEBUG   equ  1
endif

ifdef MON_DEBUG
ifdef TRACK_SYNC
extern EnterSyncHelper:proc
extern LeaveSyncHelper:proc
endif
endif


MON_ENTER_EPILOG_ADJUST_STACK macro
ifdef MON_DEBUG
ifdef TRACK_SYNC
        add     rsp, MON_ENTER_STACK_SIZE_INLINEGETTHREAD
endif
endif
        endm


MON_ENTER_RETURN_SUCCESS macro
        ; This is sensitive to the potential that pbLockTaken is NULL
        test    rsi, rsi
        jz      @F
        mov     byte ptr [rsi], 1
    @@:
        MON_ENTER_EPILOG_ADJUST_STACK
        pop     rsi
        ret

        endm

        
; The worker versions of these functions are smart about the potential for pbLockTaken
; to be NULL, and if it is then they treat it as if they don't have a state variable.
; This is because when locking is not inserted by the JIT (instead by explicit calls to
; Monitor.Enter() and Monitor.Exit()) we will call these guys.
;
; This is a frameless helper for entering a monitor on a object.
; The object is in ARGUMENT_REG1.  This tries the normal case (no
; blocking or object allocation) in line and calls a framed helper
; for the other cases.
;
; EXTERN_C void JIT_MonEnterWorker_InlineGetThread(Object* obj, /*OUT*/ BYTE* pbLockTaken)
JIT_HELPER_MONITOR_THUNK JIT_MonEnter, _TEXT
NESTED_ENTRY JIT_MonEnterWorker_InlineGetThread, _TEXT
        push_nonvol_reg     rsi
ifdef MON_DEBUG
ifdef TRACK_SYNC
        alloc_stack         MON_ENTER_STACK_SIZE_INLINEGETTHREAD

        save_reg_postrsp    rcx, MON_ENTER_STACK_SIZE_INLINEGETTHREAD + 10h + 0h
        save_reg_postrsp    rdx, MON_ENTER_STACK_SIZE_INLINEGETTHREAD + 10h + 8h
        save_reg_postrsp    r8,  MON_ENTER_STACK_SIZE_INLINEGETTHREAD + 10h + 10h
        save_reg_postrsp    r9,  MON_ENTER_STACK_SIZE_INLINEGETTHREAD + 10h + 18h
endif
endif
        END_PROLOGUE

        ; Put pbLockTaken in rsi, this can be null
        mov     rsi, rdx

        ; Check if the instance is NULL
        test    rcx, rcx
        jz      FramedLockHelper

        PATCHABLE_INLINE_GETTHREAD r11, JIT_MonEnterWorker_InlineGetThread_GetThread_PatchLabel

        ; Initialize delay value for retry with exponential backoff
        mov     r10d, dword ptr [g_SpinConstants + OFFSETOF__g_SpinConstants__dwInitialDuration]

        ; Check if we can abort here
        mov     eax, dword ptr [r11 + OFFSETOF__Thread__m_State]
        and     eax, THREAD_CATCHATSAFEPOINT_BITS
        ; Go through the slow code path to initiate ThreadAbort
        jnz     FramedLockHelper

        ; r8 will hold the syncblockindex address
        lea     r8, [rcx - OFFSETOF__ObjHeader__SyncBlkIndex]

    RetryThinLock:
        ; Fetch the syncblock dword
        mov     eax, dword ptr [r8]

        ; Check whether we have the "thin lock" layout, the lock is free and the spin lock bit is not set
        test    eax, BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX + BIT_SBLK_SPIN_LOCK + SBLK_MASK_LOCK_THREADID + SBLK_MASK_LOCK_RECLEVEL
        jnz     NeedMoreTests

        ; Everything is fine - get the thread id to store in the lock
        mov     edx, dword ptr [r11 + OFFSETOF__Thread__m_ThreadId]

        ; If the thread id is too large, we need a syncblock for sure
        cmp     edx, SBLK_MASK_LOCK_THREADID
        ja      FramedLockHelper

        ; We want to store a new value with the current thread id set in the low 10 bits
        or      edx, eax
   lock cmpxchg dword ptr [r8], edx
        jnz     PrepareToWaitThinLock

        ; Everything went fine and we're done
        add     dword ptr [r11 + OFFSETOF__Thread__m_dwLockCount], 1

        ; Done, leave and set pbLockTaken if we have it
        MON_ENTER_RETURN_SUCCESS

    NeedMoreTests:
        ; OK, not the simple case, find out which case it is
        test    eax, BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX
        jnz     HaveHashOrSyncBlockIndex

        ; The header is transitioning or the lock, treat this as if the lock was taken
        test    eax, BIT_SBLK_SPIN_LOCK
        jnz     PrepareToWaitThinLock

        ; Here we know we have the "thin lock" layout, but the lock is not free.
        ; It could still be the recursion case, compare the thread id to check
        mov     edx, eax
        and     edx, SBLK_MASK_LOCK_THREADID
        cmp     edx, dword ptr [r11 + OFFSETOF__Thread__m_ThreadId]
        jne     PrepareToWaitThinLock

        ; Ok, the thread id matches, it's the recursion case.
        ; Bump up the recursion level and check for overflow
        lea     edx, [eax + SBLK_LOCK_RECLEVEL_INC]
        test    edx, SBLK_MASK_LOCK_RECLEVEL
        jz      FramedLockHelper

        ; Try to put the new recursion level back. If the header was changed in the meantime
        ; we need a full retry, because the layout could have changed
   lock cmpxchg dword ptr [r8], edx
        jnz     RetryHelperThinLock

        ; Done, leave and set pbLockTaken if we have it
        MON_ENTER_RETURN_SUCCESS

    PrepareToWaitThinLock:
        ; If we are on an MP system, we try spinning for a certain number of iterations
        cmp     dword ptr [g_SystemInfo + OFFSETOF__g_SystemInfo__dwNumberOfProcessors], 1
        jle     FramedLockHelper

        ; Exponential backoff; delay by approximately 2*r10 clock cycles
        mov     eax, r10d
    delayLoopThinLock:
        pause   ; indicate to the CPU that we are spin waiting
        sub     eax, 1
        jnz     delayLoopThinLock

        ; Next time, wait a factor longer
        imul    r10d, dword ptr [g_SpinConstants + OFFSETOF__g_SpinConstants__dwBackoffFactor]

        cmp     r10d, dword ptr [g_SpinConstants + OFFSETOF__g_SpinConstants__dwMaximumDuration]
        jle     RetryHelperThinLock

        jmp     FramedLockHelper

    RetryHelperThinLock:
        jmp     RetryThinLock

    HaveHashOrSyncBlockIndex:
        ; If we have a hash code already, we need to create a sync block
        test    eax, BIT_SBLK_IS_HASHCODE
        jnz     FramedLockHelper

        ; OK, we have a sync block index, just and out the top bits and grab the synblock index
        and     eax, MASK_SYNCBLOCKINDEX

        ; Get the sync block pointer
        mov     rdx, qword ptr [g_pSyncTable]
        shl     eax, 4h
        mov     rdx, [rdx + rax + OFFSETOF__SyncTableEntry__m_SyncBlock]

        ; Check if the sync block has been allocated
        test    rdx, rdx
        jz      FramedLockHelper

        ; Get a pointer to the lock object
        lea     rdx, [rdx + OFFSETOF__SyncBlock__m_Monitor]

        ; Attempt to acquire the lock
    RetrySyncBlock:
        mov     eax, dword ptr [rdx + OFFSETOF__AwareLock__m_MonitorHeld]
        test    eax, eax
        jne     HaveWaiters

        ; Common case, lock isn't held and there are no waiters. Attempt to
        ; gain ownership ourselves
        xor     ecx, ecx
        inc     ecx

   lock cmpxchg dword ptr [rdx + OFFSETOF__AwareLock__m_MonitorHeld], ecx
        jnz     RetryHelperSyncBlock

        ; Success. Save the thread object in the lock and increment the use count
        mov     qword ptr [rdx + OFFSETOF__AwareLock__m_HoldingThread], r11
        add     dword ptr [rdx + OFFSETOF__AwareLock__m_Recursion], 1
        add     dword ptr [r11 + OFFSETOF__Thread__m_dwLockCount], 1

ifdef MON_DEBUG
ifdef TRACK_SYNC
        mov     rcx, [rsp + MON_ENTER_STACK_SIZE_INLINEGETTHREAD + 8h]       ; return address
        ; void EnterSyncHelper(UINT_PTR caller, AwareLock* lock)
        call    EnterSyncHelper
endif
endif

        ; Done, leave and set pbLockTaken if we have it
        MON_ENTER_RETURN_SUCCESS

        ; It's possible to get here with waiters by no lock held, but in this
        ; case a signal is about to be fired which will wake up the waiter. So
        ; for fairness sake we should wait too.
        ; Check first for recur11ve lock attempts on the same thread.
    HaveWaiters:
        ; Is mutex already owned by current thread?
        cmp     [rdx + OFFSETOF__AwareLock__m_HoldingThread], r11
        jne     PrepareToWait

        ; Yes, bump our use count.
        add     dword ptr [rdx + OFFSETOF__AwareLock__m_Recursion], 1

ifdef MON_DEBUG
ifdef TRACK_SYNC
        mov     rcx, [rsp + MON_ENTER_STACK_SIZE_INLINEGETTHREAD + 8h]       ; return address
        ; void EnterSyncHelper(UINT_PTR caller, AwareLock* lock)
        call    EnterSyncHelper
endif
endif
        ; Done, leave and set pbLockTaken if we have it
        MON_ENTER_RETURN_SUCCESS

    PrepareToWait:
        ; If we are on a MP system we try spinning for a certain number of iterations
        cmp     dword ptr [g_SystemInfo + OFFSETOF__g_SystemInfo__dwNumberOfProcessors], 1
        jle     HaveWaiters1

        ; Exponential backoff: delay by approximately 2*r10 clock cycles
        mov     eax, r10d
    delayLoop:
        pause   ; indicate to the CPU that we are spin waiting
        sub     eax, 1
        jnz     delayLoop

        ; Next time, wait a factor longer
        imul    r10d, dword ptr [g_SpinConstants + OFFSETOF__g_SpinConstants__dwBackoffFactor]

        cmp     r10d, dword ptr [g_SpinConstants + OFFSETOF__g_SpinConstants__dwMaximumDuration]
        jle     RetrySyncBlock

    HaveWaiters1:
        mov     rcx, rdx
        mov     rdx, rsi
        MON_ENTER_EPILOG_ADJUST_STACK
        pop     rsi
        ; void JITutil_MonContention(AwareLock* lock, BYTE* pbLockTaken)
        jmp     JITutil_MonContention

    RetryHelperSyncBlock:
        jmp     RetrySyncBlock

    FramedLockHelper:
        mov     rdx, rsi
        MON_ENTER_EPILOG_ADJUST_STACK
        pop     rsi
        ; void JITutil_MonEnterWorker(Object* obj, BYTE* pbLockTaken)
        jmp     JITutil_MonEnterWorker

NESTED_END JIT_MonEnterWorker_InlineGetThread, _TEXT


MON_EXIT_EPILOG_ADJUST_STACK macro
ifdef MON_DEBUG
ifdef TRACK_SYNC
        add     rsp, MON_EXIT_STACK_SIZE_INLINEGETTHREAD
endif
endif
        endm

MON_EXIT_RETURN_SUCCESS macro
        ; This is sensitive to the potential that pbLockTaken is null
        test    r10, r10
        jz      @F
        mov     byte ptr [r10], 0
    @@:
        MON_EXIT_EPILOG_ADJUST_STACK
        ret

        endm

               
; The worker versions of these functions are smart about the potential for pbLockTaken
; to be NULL, and if it is then they treat it as if they don't have a state variable.
; This is because when locking is not inserted by the JIT (instead by explicit calls to
; Monitor.Enter() and Monitor.Exit()) we will call these guys.
;
; This is a frameless helper for exiting a monitor on a object.
; The object is in ARGUMENT_REG1.  This tries the normal case (no
; blocking or object allocation) in line and calls a framed helper
; for the other cases.
;
; void JIT_MonExitWorker_InlineGetThread(Object* obj, BYTE* pbLockTaken)
JIT_HELPER_MONITOR_THUNK JIT_MonExit, _TEXT
NESTED_ENTRY JIT_MonExitWorker_InlineGetThread, _TEXT
        .savereg    rcx, 0
ifdef MON_DEBUG
ifdef TRACK_SYNC
        alloc_stack         MON_EXIT_STACK_SIZE_INLINEGETTHREAD

        save_reg_postrsp    rcx, MON_EXIT_STACK_SIZE_INLINEGETTHREAD + 8h + 0h
        save_reg_postrsp    rdx, MON_EXIT_STACK_SIZE_INLINEGETTHREAD + 8h + 8h
        save_reg_postrsp    r8,  MON_EXIT_STACK_SIZE_INLINEGETTHREAD + 8h + 10h
        save_reg_postrsp    r9,  MON_EXIT_STACK_SIZE_INLINEGETTHREAD + 8h + 18h
endif
endif 
        END_PROLOGUE

        ; pbLockTaken is stored in r10, this can be null
        mov     r10, rdx

        ; if pbLockTaken is NULL then we got here without a state variable, avoid the
        ; next comparison in that case as it will AV
        test    rdx, rdx
        jz      Null_pbLockTaken

        ; If the lock wasn't taken then we bail quickly without doing anything
        cmp     byte ptr [rdx], 0
        je      LockNotTaken

    Null_pbLockTaken:
        ; Check is the instance is null
        test    rcx, rcx
        jz      FramedLockHelper

        PATCHABLE_INLINE_GETTHREAD r11, JIT_MonExitWorker_InlineGetThread_GetThread_PatchLabel

        ; r8 will hold the syncblockindex address
        lea     r8, [rcx - OFFSETOF__ObjHeader__SyncBlkIndex]

    RetryThinLock:
        ; Fetch the syncblock dword
        mov     eax, dword ptr [r8]
        test    eax, BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX + BIT_SBLK_SPIN_LOCK
        jnz     NeedMoreTests

        ; Ok, we have a "thin lock" layout - check whether the thread id matches
        mov     edx, eax
        and     edx, SBLK_MASK_LOCK_THREADID
        cmp     edx, dword ptr [r11 + OFFSETOF__Thread__m_ThreadId]
        jne     FramedLockHelper

        ; check the recursion level
        test    eax, SBLK_MASK_LOCK_RECLEVEL
        jne     DecRecursionLevel

        ; It's zero -- we're leaving the lock.
        ; So try to put back a zero thread id.
        ; edx and eax match in the thread id bits, and edx is zero else where, so the xor is sufficient
        xor     edx, eax
   lock cmpxchg dword ptr [r8], edx
        jnz     RetryThinLockHelper1  ; forward jump to avoid mispredict on success

        ; Dec the dwLockCount on the thread
        sub     dword ptr [r11 + OFFSETOF__Thread__m_dwLockCount], 1

        ; Done, leave and set pbLockTaken if we have it
        MON_EXIT_RETURN_SUCCESS

    RetryThinLockHelper1:
        jmp     RetryThinLock

    DecRecursionLevel:
        lea     edx, [eax - SBLK_LOCK_RECLEVEL_INC]
   lock cmpxchg dword ptr [r8], edx
        jnz     RetryThinLockHelper2  ; forward jump to avoid mispredict on success

        ; We're done, leave and set pbLockTaken if we have it
        MON_EXIT_RETURN_SUCCESS
        
    RetryThinLockHelper2:
        jmp     RetryThinLock

    NeedMoreTests:
        ; Forward all special cases to the slow helper
        test    eax, BIT_SBLK_IS_HASHCODE + BIT_SBLK_SPIN_LOCK
        jnz     FramedLockHelper

        ; Get the sync block index and use it to compute the sync block pointer
        mov     rdx, qword ptr [g_pSyncTable]
        and     eax, MASK_SYNCBLOCKINDEX
        shl     eax, 4
        mov     rdx, [rdx + rax + OFFSETOF__SyncTableEntry__m_SyncBlock]

        ; Was there a sync block?
        test    rdx, rdx
        jz      FramedLockHelper

        ; Get a pointer to the lock object.
        lea     rdx, [rdx + OFFSETOF__SyncBlock__m_Monitor]

        ; Check if the lock is held.
        cmp     qword ptr [rdx + OFFSETOF__AwareLock__m_HoldingThread], r11
        jne     FramedLockHelper

ifdef MON_DEBUG
ifdef TRACK_SYNC
        mov     [rsp + 28h], rcx
        mov     [rsp + 30h], rdx
        mov     [rsp + 38h], r10
        mov     [rsp + 40h], r11

        mov     rcx, [rsp + MON_EXIT_STACK_SIZE_INLINEGETTHREAD ]       ; return address
        ; void LeaveSyncHelper(UINT_PTR caller, AwareLock* lock)
        call    LeaveSyncHelper

        mov     rcx, [rsp + 28h]
        mov     rdx, [rsp + 30h]
        mov     r10, [rsp + 38h]
        mov     r11, [rsp + 40h]
endif
endif

        ; Reduce our recursion count
        sub     dword ptr [rdx + OFFSETOF__AwareLock__m_Recursion], 1
        jz      LastRecursion

        ; Done, leave and set pbLockTaken if we have it
        MON_EXIT_RETURN_SUCCESS

    RetryHelperThinLock:
        jmp     RetryThinLock

    FramedLockHelper:
        mov     rdx, r10
        MON_EXIT_EPILOG_ADJUST_STACK
        ; void JITutil_MonExitWorker(Object* obj, BYTE* pbLockTaken)
        jmp     JITutil_MonExitWorker

    LastRecursion:
ifdef MON_DEBUG
ifdef TRACK_SYNC
        mov     rax, [rdx + OFFSETOF__AwareLock__m_HoldingThread]
endif
endif

        sub     dword ptr [r11 + OFFSETOF__Thread__m_dwLockCount], 1
        mov     qword ptr [rdx + OFFSETOF__AwareLock__m_HoldingThread], 0

    Retry:
        mov     eax, dword ptr [rdx + OFFSETOF__AwareLock__m_MonitorHeld]
        lea     r9d, [eax - 1]
   lock cmpxchg dword ptr [rdx + OFFSETOF__AwareLock__m_MonitorHeld], r9d
        jne     RetryHelper

        test    eax, STATE_CHECK
        jne     MustSignal

        ; Done, leave and set pbLockTaken if we have it
        MON_EXIT_RETURN_SUCCESS

    MustSignal:
        mov     rcx, rdx
        mov     rdx, r10
        MON_EXIT_EPILOG_ADJUST_STACK
        ; void JITutil_MonSignal(AwareLock* lock, BYTE* pbLockTaken)
        jmp     JITutil_MonSignal

    RetryHelper:
        jmp     Retry

    LockNotTaken:
        MON_EXIT_EPILOG_ADJUST_STACK
        REPRET
NESTED_END JIT_MonExitWorker_InlineGetThread, _TEXT


; This is a frameless helper for trying to enter a monitor on a object.
; The object is in ARGUMENT_REG1 and a timeout in ARGUMENT_REG2. This tries the
; normal case (no object allocation) in line and calls a framed helper for the
; other cases.
;
; void JIT_MonTryEnter_InlineGetThread(Object* obj, INT32 timeOut, BYTE* pbLockTaken)
NESTED_ENTRY JIT_MonTryEnter_InlineGetThread, _TEXT
        ; save rcx, rdx (timeout) in the shadow space
        .savereg            rcx, 8h
        mov                 [rsp + 8h], rcx
        .savereg            rdx, 10h
        mov                 [rsp + 10h], rdx
ifdef MON_DEBUG
ifdef TRACK_SYNC        
        alloc_stack         MON_ENTER_STACK_SIZE_INLINEGETTHREAD

; rcx has already been saved
;        save_reg_postrsp    rcx, MON_ENTER_STACK_SIZE_INLINEGETTHREAD + 8h + 0h
; rdx has already been saved
;        save_reg_postrsp    rdx, MON_ENTER_STACK_SIZE + 8h + 8h
        save_reg_postrsp    r8,  MON_ENTER_STACK_SIZE_INLINEGETTHREAD + 8h + 10h
        save_reg_postrsp    r9,  MON_ENTER_STACK_SIZE_INLINEGETTHREAD + 8h + 18h
endif
endif
        END_PROLOGUE

        ; Check if the instance is NULL
        test    rcx, rcx
        jz      FramedLockHelper

        ; Check if the timeout looks valid
        cmp     edx, -1
        jl      FramedLockHelper

        PATCHABLE_INLINE_GETTHREAD r11, JIT_MonTryEnter_GetThread_PatchLabel

        ; Initialize delay value for retry with exponential backoff
        mov     r10d, dword ptr [g_SpinConstants + OFFSETOF__g_SpinConstants__dwInitialDuration]

        ; Check if we can abort here
        mov     eax, dword ptr [r11 + OFFSETOF__Thread__m_State]
        and     eax, THREAD_CATCHATSAFEPOINT_BITS
        ; Go through the slow code path to initiate THreadAbort
        jnz     FramedLockHelper

        ; r9 will hold the syncblockindex address
        lea     r9, [rcx - OFFSETOF__ObjHeader__SyncBlkIndex]

    RetryThinLock:
        ; Fetch the syncblock dword
        mov     eax, dword ptr [r9]

        ; Check whether we have the "thin lock" layout, the lock is free and the spin lock bit is not set
        test    eax, BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX + BIT_SBLK_SPIN_LOCK + SBLK_MASK_LOCK_THREADID + SBLK_MASK_LOCK_RECLEVEL
        jne     NeedMoreTests

        ; Everything is fine - get the thread id to store in the lock
        mov     edx, dword ptr [r11 + OFFSETOF__Thread__m_ThreadId]

        ; If the thread id is too large, we need a syncblock for sure
        cmp     edx, SBLK_MASK_LOCK_THREADID
        ja      FramedLockHelper

        ; We want to store a new value with the current thread id set in the low 10 bits
        or      edx, eax
   lock cmpxchg dword ptr [r9], edx
        jnz     RetryHelperThinLock

        ; Got the lock, everything is fine
        add     dword ptr [r11 + OFFSETOF__Thread__m_dwLockCount], 1
        ; Return TRUE
        mov     byte ptr [r8], 1
ifdef MON_DEBUG
ifdef TRACK_SYNC
        add     rsp, MON_ENTER_STACK_SIZE_INLINEGETTHREAD
endif
endif
        ret

    NeedMoreTests:
        ; OK, not the simple case, find out which case it is
        test    eax, BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX
        jnz     HaveHashOrSyncBlockIndex

        ; The header is transitioning or the lock
        test    eax, BIT_SBLK_SPIN_LOCK
        jnz     RetryHelperThinLock

        ; Here we know we have the "thin lock" layout, but the lock is not free.
        ; It could still be the recursion case, compare the thread id to check
        mov     edx, eax
        and     edx, SBLK_MASK_LOCK_THREADID
        cmp     edx, dword ptr [r11 + OFFSETOF__Thread__m_ThreadId]
        jne     PrepareToWaitThinLock

        ; Ok, the thread id matches, it's the recursion case.
        ; Dump up the recursion level and check for overflow
        lea     edx, [eax + SBLK_LOCK_RECLEVEL_INC]
        test    edx, SBLK_MASK_LOCK_RECLEVEL
        jz      FramedLockHelper

        ; Try to put the new recursion level back. If the header was changed in the meantime
        ; we need a full retry, because the layout could have changed
   lock cmpxchg dword ptr [r9], edx
        jnz     RetryHelperThinLock

        ; Everything went fine and we're done, return TRUE
        mov     byte ptr [r8], 1
ifdef MON_DEBUG
ifdef TRACK_SYNC
        add     rsp, MON_ENTER_STACK_SIZE_INLINEGETTHREAD
endif
endif
        ret

    PrepareToWaitThinLock:
        ; Return failure if timeout is zero
        cmp     dword ptr [rsp + 10h], 0
        je      TimeoutZero
 
        ; If we are on an MP system, we try spinning for a certain number of iterations
        cmp     dword ptr [g_SystemInfo + OFFSETOF__g_SystemInfo__dwNumberOfProcessors], 1
        jle     FramedLockHelper

        ; Exponential backoff; delay by approximately 2*r10d clock cycles
        mov     eax, r10d
    DelayLoopThinLock:
        pause   ; indicate to the CPU that we are spin waiting
        sub     eax, 1
        jnz     DelayLoopThinLock

        ; Next time, wait a factor longer
        imul    r10d, dword ptr [g_SpinConstants + OFFSETOF__g_SpinConstants__dwBackoffFactor]

        cmp     r10d, dword ptr [g_SpinConstants + OFFSETOF__g_SpinConstants__dwMaximumDuration]
        jle     RetryHelperThinLock

        jmp     FramedLockHelper

    RetryHelperThinLock:
        jmp     RetryThinLock

	TimeoutZero:
        ; Did not acquire, return FALSE
        mov     byte ptr [r8], 0
ifdef MON_DEBUG
ifdef TRACK_SYNC
        add     rsp, MON_ENTER_STACK_SIZE_INLINEGETTHREAD
endif
endif
        ret

    HaveHashOrSyncBlockIndex:
        ; If we have a hash code already, we need to create a sync block
        test    eax, BIT_SBLK_IS_HASHCODE
        jnz     FramedLockHelper

        ; OK, we have a sync block index, just and out the top bits and grab the synblock index
        and     eax, MASK_SYNCBLOCKINDEX

        ; Get the sync block pointer
        mov     rdx, qword ptr [g_pSyncTable]
        shl     eax, 4
        mov     rdx, [rdx + rax + OFFSETOF__SyncTableEntry__m_SyncBlock]

        ; Check if the sync block has been allocated
        test    rdx, rdx
        jz      FramedLockHelper

        ; Get a pointer to the lock object
        lea     rdx, [rdx + OFFSETOF__SyncBlock__m_Monitor]

    RetrySyncBlock:
        ; Attempt to acuire the lock
        mov     eax, dword ptr [rdx + OFFSETOF__AwareLock__m_MonitorHeld]
        test    eax, eax
        jne     HaveWaiters

        ; Common case, lock isn't held and there are no waiters. Attempt to
        ; gain ownership ourselves
        xor     ecx, ecx
        inc     ecx
   lock cmpxchg dword ptr [rdx + OFFSETOF__AwareLock__m_MonitorHeld], ecx
        jnz     RetryHelperSyncBlock

        ; Success. Save the thread object in the lock and increment the use count
        mov     qword ptr [rdx + OFFSETOF__AwareLock__m_HoldingThread], r11
        add     dword ptr [rdx + OFFSETOF__AwareLock__m_Recursion], 1
        add     dword ptr [r11 + OFFSETOF__Thread__m_dwLockCount], 1

ifdef MON_DEBUG
ifdef TRACK_SYNC
        mov     rcx, [rsp + MON_ENTER_STACK_SIZE_INLINEGETTHREAD]       ; return address
        ; void EnterSyncHelper(UINT_PTR caller, AwareLock* lock)
        call    EnterSyncHelper
endif
endif

        ; Return TRUE
        mov     byte ptr [r8], 1
ifdef MON_DEBUG
ifdef TRACK_SYNC
        add     rsp, MON_ENTER_STACK_SIZE_INLINEGETTHREAD
endif
endif
        ret

        ; It's possible to get here with waiters by no lock held, but in this
        ; case a signal is about to be fired which will wake up the waiter. So
        ; for fairness sake we should wait too.
        ; Check first for recur11ve lock attempts on the same thread.
    HaveWaiters:
        ; Is mutex already owned by current thread?
        cmp     [rdx + OFFSETOF__AwareLock__m_HoldingThread], r11
        jne     PrepareToWait

        ; Yes, bump our use count.
        add     dword ptr [rdx + OFFSETOF__AwareLock__m_Recursion], 1

ifdef MON_DEBUG
ifdef TRACK_SYNC
        mov     rcx, [rsp + MON_ENTER_STACK_SIZE_INLINEGETTHREAD]       ; return address
        ; void EnterSyncHelper(UINT_PTR caller, AwareLock* lock)
        call    EnterSyncHelper
endif
endif

        ; Return TRUE
        mov     byte ptr [r8], 1
ifdef MON_DEBUG
ifdef TRACK_SYNC
        add     rsp, MON_ENTER_STACK_SIZE_INLINEGETTHREAD
endif
endif
        ret

    PrepareToWait:
        ; Return failure if timeout is zero
        cmp     dword ptr [rsp + 10h], 0
ifdef MON_DEBUG
ifdef TRACK_SYNC
        ; if we are using the _DEBUG stuff then rsp has been adjusted
        ; so compare the value at the adjusted position
        ; there's really little harm in the extra stack read
        cmp     dword ptr [rsp + MON_ENTER_STACK_SIZE_INLINEGETTHREAD + 10h]
endif
endif
        je      TimeoutZero

        ; If we are on an MP system, we try spinning for a certain number of iterations
        cmp     dword ptr [g_SystemInfo + OFFSETOF__g_SystemInfo__dwNumberOfProcessors], 1
        jle     Block
    
        ; Exponential backoff; delay by approximately 2*r10d clock cycles
        mov     eax, r10d
    DelayLoop:
        pause   ; indicate to the CPU that we are spin waiting
        sub     eax, 1
        jnz     DelayLoop
    
        ; Next time, wait a factor longer
        imul    r10d, dword ptr [g_SpinConstants + OFFSETOF__g_SpinConstants__dwBackoffFactor]
    
        cmp     r10d, dword ptr [g_SpinConstants + OFFSETOF__g_SpinConstants__dwMaximumDuration]
        jle     RetrySyncBlock

        jmp     Block

    RetryHelperSyncBlock:
        jmp     RetrySyncBlock

    Block:
        ; In the Block case we've trashed RCX, restore it
        mov     rcx, [rsp + 8h]
ifdef MON_DEBUG
ifdef TRACK_SYNC
        ; if we're tracking this stuff then rcx is at a different offset to RSP, we just
        ; overwrite the wrong value which we just got... this is for debug purposes only
        ; so there's really no performance issue here
        mov     rcx, [rsp + MON_ENTER_STACK_SIZE_INLINEGETTHREAD + 8h]
endif
endif
    FramedLockHelper:
ifdef MON_DEBUG
ifdef TRACK_SYNC
        add     rsp, MON_ENTER_STACK_SIZE_INLINEGETTHREAD
endif
endif
        mov     rdx, [rsp + 10h]
        ; void JITutil_MonTryEnter(Object* obj, INT32 timeout)
        jmp     JITutil_MonTryEnter

NESTED_END JIT_MonTryEnter_InlineGetThread, _TEXT


MON_ENTER_STATIC_RETURN_SUCCESS macro
        ; pbLockTaken is never null for static helpers
        test    rdx, rdx
        mov     byte ptr [rdx], 1
        REPRET
        
        endm

MON_EXIT_STATIC_RETURN_SUCCESS macro
        ; pbLockTaken is never null for static helpers
        mov     byte ptr [rdx], 0
        REPRET
        
        endm


; This is a frameless helper for entering a static monitor on a class.
; The methoddesc is in ARGUMENT_REG1.  This tries the normal case (no
; blocking or object allocation) in line and calls a framed helper
; for the other cases.
;
; void JIT_MonEnterStatic_InlineGetThread(AwareLock *lock, BYTE *pbLockTaken)
NESTED_ENTRY JIT_MonEnterStatic_InlineGetThread, _TEXT
        .savereg            rcx, 0
ifdef MON_DEBUG
ifdef TRACK_SYNC
        alloc_stack         MIN_SIZE
        save_reg_postrsp    rcx, MIN_SIZE + 8h + 0h
endif
endif
    END_PROLOGUE

        ; Attempt to acquire the lock
    Retry:
        mov     eax, dword ptr [rcx + OFFSETOF__AwareLock__m_MonitorHeld]
        test    eax, eax
        jne     HaveWaiters

        ; Common case; lock isn't held and there are no waiters. Attempt to
        ; gain ownership by ourselves.
        mov     r10d, 1

   lock cmpxchg dword ptr [rcx + OFFSETOF__AwareLock__m_MonitorHeld], r10d
        jnz     RetryHelper

        PATCHABLE_INLINE_GETTHREAD rax, JIT_MonEnterStaticWorker_InlineGetThread_GetThread_PatchLabel_1
        
        mov     qword ptr [rcx + OFFSETOF__AwareLock__m_HoldingThread], rax
        add     dword ptr [rcx + OFFSETOF__AwareLock__m_Recursion], 1
        add     dword ptr [rax + OFFSETOF__Thread__m_dwLockCount], 1

ifdef MON_DEBUG
ifdef TRACK_SYNC
        mov     rdx, rcx
        mov     rcx, [rsp]
        add     rsp, MIN_SIZE
        ; void EnterSyncHelper(UINT_PTR caller, AwareLock* lock)
        jmp     EnterSyncHelper
endif
endif
        MON_ENTER_STATIC_RETURN_SUCCESS

        ; It's possible to get here with waiters by with no lock held, in this
        ; case a signal is about to be fired which will wake up a waiter. So
        ; for fairness sake we should wait too.
        ; Check first for recursive lock attempts on the same thread.
    HaveWaiters:
        PATCHABLE_INLINE_GETTHREAD rax, JIT_MonEnterStaticWorker_InlineGetThread_GetThread_PatchLabel_2

        ; Is mutex alread owned by current thread?
        cmp     [rcx + OFFSETOF__AwareLock__m_HoldingThread], rax
        jne     PrepareToWait

        ; Yes, bump our use count.
        add     dword ptr [rcx + OFFSETOF__AwareLock__m_Recursion], 1
ifdef MON_DEBUG
ifdef TRACK_SYNC
        mov     rdx, rcx
        mov     rcx, [rsp + MIN_SIZE]
        add     rsp, MIN_SIZE
        ; void EnterSyncHelper(UINT_PTR caller, AwareLock* lock)
        jmp     EnterSyncHelper
endif
endif
        ret

    PrepareToWait:
ifdef MON_DEBUG
ifdef TRACK_SYNC
        add     rsp, MIN_SIZE
endif
endif
        ; void JITutil_MonContention(AwareLock* obj, BYTE* pbLockTaken)
        jmp     JITutil_MonContention

    RetryHelper:
        jmp     Retry
NESTED_END JIT_MonEnterStatic_InlineGetThread, _TEXT

; A frameless helper for exiting a static monitor on a class.
; The methoddesc is in ARGUMENT_REG1.  This tries the normal case (no
; blocking or object allocation) in line and calls a framed helper
; for the other cases.
;
; void JIT_MonExitStatic_InlineGetThread(AwareLock *lock, BYTE *pbLockTaken)
NESTED_ENTRY JIT_MonExitStatic_InlineGetThread, _TEXT
        .savereg        rcx, 0
ifdef MON_DEBUG
ifdef TRACK_SYNC
        alloc_stack     MIN_SIZE
        save_reg_postrsp    rcx, MIN_SIZE + 8h + 0h
endif
endif
    END_PROLOGUE

ifdef MON_DEBUG
ifdef TRACK_SYNC
        push    rsi
        push    rdi
        mov     rsi, rcx
        mov     rdi, rdx
        mov     rdx, [rsp + 8]
        call    LeaveSyncHelper
        mov     rcx, rsi
        mov     rdx, rdi
        pop     rdi
        pop     rsi
endif
endif
        PATCHABLE_INLINE_GETTHREAD rax, JIT_MonExitStaticWorker_InlineGetThread_GetThread_PatchLabel

        ; Check if lock is held        
        cmp     [rcx + OFFSETOF__AwareLock__m_HoldingThread], rax
        jne     LockError

        ; Reduce our recursion count
        sub     dword ptr [rcx + OFFSETOF__AwareLock__m_Recursion], 1
        jz      LastRecursion

ifdef MON_DEBUG
ifdef TRACK_SYNC
        add     rsp, MIN_SIZE
        ret
endif
endif
        REPRET

        ; This is the last count we held on this lock, so release the lock
    LastRecursion:
        ; Thead* is in rax
        sub     dword ptr [rax + OFFSETOF__Thread__m_dwLockCount], 1
        mov     qword ptr [rcx + OFFSETOF__AwareLock__m_HoldingThread], 0

    Retry:
        mov     eax, dword ptr [rcx + OFFSETOF__AwareLock__m_MonitorHeld]
        lea     r10d, [eax - 1]
   lock cmpxchg dword ptr [rcx + OFFSETOF__AwareLock__m_MonitorHeld], r10d
        jne     RetryHelper
        test    eax, STATE_CHECK
        jne     MustSignal

ifdef MON_DEBUG
ifdef TRACK_SYNC
        add     rsp, MIN_SIZE
        ret
endif
endif
        MON_EXIT_STATIC_RETURN_SUCCESS

    MustSignal:
ifdef MON_DEBUG
ifdef TRACK_SYNC
        add     rsp, MIN_SIZE
endif
endif
        ; void JITutil_MonSignal(AwareLock* lock, BYTE* pbLockTaken)
        jmp     JITutil_MonSignal

    RetryHelper:
        jmp     Retry

    LockError:
        mov     rcx, CORINFO_SynchronizationLockException_ASM
ifdef MON_DEBUG
ifdef TRACK_SYNC
        add     rsp, MIN_SIZE
endif
endif
        ; void JIT_InternalThrow(unsigned exceptNum)
        jmp     JIT_InternalThrow
NESTED_END JIT_MonExitStatic_InlineGetThread, _TEXT

        end

