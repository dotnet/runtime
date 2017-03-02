// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

//
// eax = UMEntryThunk*
//
NESTED_ENTRY TheUMEntryPrestub, _TEXT, UnhandledExceptionHandlerUnix
    push    eax  // UMEntryThunk*
    call    C_FUNC(TheUMEntryPrestubWorker)
    add     esp, 4
    // eax = PCODE

    jmp     eax     // Tail Jmp
NESTED_END TheUMEntryPrestub, _TEXT

//
// eax: UMEntryThunk*
//
NESTED_ENTRY UMThunkStub, _TEXT, UnhandledExceptionHandlerUnix

#define UMThunkStub_SAVEDREG                (3*4)   // ebx, esi, edi
#define UMThunkStub_LOCALVARS               (2*4)   // UMEntryThunk*, Thread*
#define UMThunkStub_UMENTRYTHUNK_OFFSET     (UMThunkStub_SAVEDREG+4)
#define UMThunkStub_THREAD_OFFSET           (UMThunkStub_UMENTRYTHUNK_OFFSET+4)
#define UMThunkStub_INT_ARG_OFFSET          (UMThunkStub_THREAD_OFFSET+4)
#define UMThunkStub_FIXEDALLOCSIZE          (UMThunkStub_LOCALVARS+4) // extra 4 is for stack alignment

// return address                           <-- entry ESP
// saved ebp                                <-- EBP
// saved ebx
// saved esi
// saved edi
// UMEntryThunk*
// Thread*
// dummy 4 byte for 16 byte stack alignment
// {optional stack args passed to callee}   <-- new esp

    PROLOG_BEG
    PROLOG_PUSH ebx
    PROLOG_PUSH esi
    PROLOG_PUSH edi
    PROLOG_END
    sub     esp, UMThunkStub_FIXEDALLOCSIZE

    mov     dword ptr [ebp - UMThunkStub_UMENTRYTHUNK_OFFSET], eax

    call    C_FUNC(GetThread)
    test    eax, eax
    jz      LOCAL_LABEL(DoThreadSetup)

LOCAL_LABEL(HaveThread):

    mov     dword ptr [ebp - UMThunkStub_THREAD_OFFSET], eax

    // FailFast if a native callable method is invoked via ldftn and calli.
    cmp     dword ptr [eax + Thread_m_fPreemptiveGCDisabled], 1
    jz      LOCAL_LABEL(InvalidTransition)

    // disable preemptive GC
    mov     dword ptr [eax + Thread_m_fPreemptiveGCDisabled], 1

    // catch returning thread here if a GC is in progress
    PREPARE_EXTERNAL_VAR g_TrapReturningThreads, eax
    cmp     eax, 0
    jnz     LOCAL_LABEL(DoTrapReturningThreadsTHROW)

LOCAL_LABEL(InCooperativeMode):

#if _DEBUG
    mov     eax, dword ptr [ebp - UMThunkStub_THREAD_OFFSET]
    mov     eax, dword ptr [eax + Thread__m_pDomain]
    mov     esi, dword ptr [eax + AppDomain__m_dwId]

    mov     eax, dword ptr [ebp - UMThunkStub_UMENTRYTHUNK_OFFSET]
    mov     edi, dword ptr [eax + UMEntryThunk__m_dwDomainId]

    cmp     esi, edi
    jne     LOCAL_LABEL(WrongAppDomain)
#endif

    mov     eax, dword ptr [ebp - UMThunkStub_UMENTRYTHUNK_OFFSET]
    mov     ebx, dword ptr [eax + UMEntryThunk__m_pUMThunkMarshInfo]
    mov     eax, dword ptr [ebx + UMThunkMarshInfo__m_cbActualArgSize]
    test    eax, eax
    jnz     LOCAL_LABEL(UMThunkStub_CopyStackArgs)

LOCAL_LABEL(UMThunkStub_ArgumentsSetup):

    mov     eax, dword ptr [ebp - UMThunkStub_UMENTRYTHUNK_OFFSET]
    mov     ebx, dword ptr [eax + UMEntryThunk__m_pUMThunkMarshInfo]
    mov     ebx, dword ptr [ebx + UMThunkMarshInfo__m_pILStub]

    call    ebx

LOCAL_LABEL(PostCall):

    mov     ebx, dword ptr [ebp - UMThunkStub_THREAD_OFFSET]
    mov     dword ptr [ebx + Thread_m_fPreemptiveGCDisabled], 0

    lea     esp, [ebp - UMThunkStub_SAVEDREG]  // deallocate arguments

    mov     ecx, dword ptr [ebp - UMThunkStub_UMENTRYTHUNK_OFFSET]
    mov     edx, dword ptr [ecx + UMEntryThunk__m_pUMThunkMarshInfo]
    mov     edx, dword ptr [edx + UMThunkMarshInfo__m_cbRetPop]

    EPILOG_BEG
    EPILOG_POP edi
    EPILOG_POP esi
    EPILOG_POP ebx
    EPILOG_END

    pop     ecx  // pop return address
    add     esp, edx // adjust ESP
    jmp     ecx // return to caller

LOCAL_LABEL(DoThreadSetup):

    call    C_FUNC(CreateThreadBlockThrow)
    jmp     LOCAL_LABEL(HaveThread)

LOCAL_LABEL(InvalidTransition):

    //No arguments to setup , ReversePInvokeBadTransition will failfast
    call    C_FUNC(ReversePInvokeBadTransition)

LOCAL_LABEL(DoTrapReturningThreadsTHROW):

    // extern "C" VOID STDCALL UMThunkStubRareDisableWorker(Thread *pThread, UMEntryThunk *pUMEntryThunk)
    sub     esp, (2*4) // add padding to ensure 16 byte stack alignment
    mov     eax, dword ptr [ebp - UMThunkStub_UMENTRYTHUNK_OFFSET]
    push    eax
    mov     eax, dword ptr [ebp - UMThunkStub_THREAD_OFFSET]
    push    eax
    call    C_FUNC(UMThunkStubRareDisableWorker)
    add     esp, (2*4) // restore to before stack alignment

    jmp     LOCAL_LABEL(InCooperativeMode)

LOCAL_LABEL(UMThunkStub_CopyStackArgs):

    // eax = m_cbActualArgSize, in bytes
    // esi = src
    // edi = dest
    // ebx = scratch
    lea     esi, [ebp + 0x8]

    sub     esp, eax
    and     esp, -16    // align with 16 byte
    lea     edi, [esp]

    // First, we copy arguments to ecx and edx registers (if needed).
    mov     edx, dword ptr [ebp - UMThunkStub_UMENTRYTHUNK_OFFSET]
    mov     edx, dword ptr [edx + UMEntryThunk__m_pUMThunkMarshInfo]
    mov     ebx, dword ptr [edx + UMThunkMarshInfo__m_ecxArgOffset]
    cmp     ebx, -1
    je      LOCAL_LABEL(InitCopyStack)
    mov     ecx, dword ptr [esi + ebx]
    add     eax, -4
    jz      LOCAL_LABEL(UMThunkStub_ArgumentsSetup)

    mov     ebx, dword ptr [edx + UMThunkMarshInfo__m_edxArgOffset]
    cmp     ebx, -1
    je      LOCAL_LABEL(InitCopyStack)
    mov     edx, dword ptr [esi + ebx]
    add     eax, -4
    jz      LOCAL_LABEL(UMThunkStub_ArgumentsSetup)

LOCAL_LABEL(InitCopyStack):
    push    ecx
    push    edx
    mov     edx, dword ptr [ebp - UMThunkStub_UMENTRYTHUNK_OFFSET]
    mov     edx, dword ptr [edx + UMEntryThunk__m_pUMThunkMarshInfo]
    mov     ebx, [edx + UMThunkMarshInfo__m_cbActualArgSize]
    add     ebx, -4
LOCAL_LABEL(CopyStack):
    cmp     ebx, dword ptr [edx + UMThunkMarshInfo__m_ecxArgOffset]
    je      LOCAL_LABEL(IncreaseOffset)
    cmp     ebx, dword ptr [edx + UMThunkMarshInfo__m_edxArgOffset]
    je      LOCAL_LABEL(IncreaseOffset)
    add     eax, -4
    mov     ecx, dword ptr [esi + ebx]
    mov     dword ptr [edi + eax], ecx
LOCAL_LABEL(IncreaseOffset):
    add     ebx, -4
    jc      LOCAL_LABEL(CopyStack)

    pop     edx
    pop     ecx
    jmp     LOCAL_LABEL(UMThunkStub_ArgumentsSetup)

#if _DEBUG
LOCAL_LABEL(WrongAppDomain):
    int3
#endif

NESTED_END UMThunkStub, _TEXT
