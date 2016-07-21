// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    seh-unwind.cpp

Abstract:

    Implementation of exception API functions based on
    the Unwind API.



--*/

#ifndef FEATURE_PAL_SXS
#error FEATURE_PAL_SXS needs to be defined for this file.
#endif // !FEATURE_PAL_SXS

#include "pal/context.h"
#include "pal.h"
#include <dlfcn.h>
 
#if HAVE_LIBUNWIND_H
#ifndef __linux__
#define UNW_LOCAL_ONLY
#endif // !__linux__       
#include <libunwind.h>
#ifdef __linux__
#ifdef HAVE_LIBUNWIND_PTRACE
#include <libunwind-ptrace.h>
#endif // HAVE_LIBUNWIND_PTRACE
#endif // __linux__    
#endif // HAVE_LIBUNWIND_H


//----------------------------------------------------------------------
// Virtual Unwinding
//----------------------------------------------------------------------

#if HAVE_LIBUNWIND_H
#if UNWIND_CONTEXT_IS_UCONTEXT_T

#if defined(_AMD64_)
#define ASSIGN_UNWIND_REGS \
    ASSIGN_REG(Rip)        \
    ASSIGN_REG(Rsp)        \
    ASSIGN_REG(Rbp)        \
    ASSIGN_REG(Rbx)        \
    ASSIGN_REG(R12)        \
    ASSIGN_REG(R13)        \
    ASSIGN_REG(R14)        \
    ASSIGN_REG(R15)
#elif defined(_ARM64_)
#define ASSIGN_UNWIND_REGS \
    ASSIGN_REG(Pc)         \
    ASSIGN_REG(Sp)         \
    ASSIGN_REG(Fp)         \
    ASSIGN_REG(Lr)         \
    ASSIGN_REG(X19)        \
    ASSIGN_REG(X20)        \
    ASSIGN_REG(X21)        \
    ASSIGN_REG(X22)        \
    ASSIGN_REG(X23)        \
    ASSIGN_REG(X24)        \
    ASSIGN_REG(X25)        \
    ASSIGN_REG(X26)        \
    ASSIGN_REG(X27)        \
    ASSIGN_REG(X28)
#else
#error unsupported architecture
#endif

static void WinContextToUnwindContext(CONTEXT *winContext, unw_context_t *unwContext)
{
#define ASSIGN_REG(reg) MCREG_##reg(unwContext->uc_mcontext) = winContext->reg;
    ASSIGN_UNWIND_REGS
#undef ASSIGN_REG
}
#else
static void WinContextToUnwindContext(CONTEXT *winContext, unw_context_t *unwContext)
{
#if defined(_ARM_)    
    // Assuming that unw_set_reg() on cursor will point the cursor to the
    // supposed stack frame is dangerous for libunwind-arm in Linux.
    // It is because libunwind's unw_cursor_t has other data structure
    // initialized by unw_init_local(), which are not updated by
    // unw_set_reg().
    unwContext->regs[0] = 0;
    unwContext->regs[1] = 0;
    unwContext->regs[2] = 0;
    unwContext->regs[3] = 0;
    unwContext->regs[4] = winContext->R4;
    unwContext->regs[5] = winContext->R5;
    unwContext->regs[6] = winContext->R6;
    unwContext->regs[7] = winContext->R7;
    unwContext->regs[8] = winContext->R8;
    unwContext->regs[9] = winContext->R9;
    unwContext->regs[10] = winContext->R10;
    unwContext->regs[11] = winContext->R11;
    unwContext->regs[12] = 0;
    unwContext->regs[13] = winContext->Sp;
    unwContext->regs[14] = winContext->Lr;
    unwContext->regs[15] = winContext->Pc;
#endif    
} 

static void WinContextToUnwindCursor(CONTEXT *winContext, unw_cursor_t *cursor)
{
#if defined(_AMD64_)
    unw_set_reg(cursor, UNW_REG_IP, winContext->Rip);
    unw_set_reg(cursor, UNW_REG_SP, winContext->Rsp);
    unw_set_reg(cursor, UNW_X86_64_RBP, winContext->Rbp);
    unw_set_reg(cursor, UNW_X86_64_RBX, winContext->Rbx);
    unw_set_reg(cursor, UNW_X86_64_R12, winContext->R12);
    unw_set_reg(cursor, UNW_X86_64_R13, winContext->R13);
    unw_set_reg(cursor, UNW_X86_64_R14, winContext->R14);
    unw_set_reg(cursor, UNW_X86_64_R15, winContext->R15);
#endif
}
#endif

static void UnwindContextToWinContext(unw_cursor_t *cursor, CONTEXT *winContext)
{
#if defined(_AMD64_)
    unw_get_reg(cursor, UNW_REG_IP, (unw_word_t *) &winContext->Rip);
    unw_get_reg(cursor, UNW_REG_SP, (unw_word_t *) &winContext->Rsp);
    unw_get_reg(cursor, UNW_X86_64_RBP, (unw_word_t *) &winContext->Rbp);
    unw_get_reg(cursor, UNW_X86_64_RBX, (unw_word_t *) &winContext->Rbx);
    unw_get_reg(cursor, UNW_X86_64_R12, (unw_word_t *) &winContext->R12);
    unw_get_reg(cursor, UNW_X86_64_R13, (unw_word_t *) &winContext->R13);
    unw_get_reg(cursor, UNW_X86_64_R14, (unw_word_t *) &winContext->R14);
    unw_get_reg(cursor, UNW_X86_64_R15, (unw_word_t *) &winContext->R15);
#elif defined(_ARM_)
    unw_get_reg(cursor, UNW_REG_SP, (unw_word_t *) &winContext->Sp);
    unw_get_reg(cursor, UNW_REG_IP, (unw_word_t *) &winContext->Pc);
    unw_get_reg(cursor, UNW_ARM_R14, (unw_word_t *) &winContext->Lr);
    unw_get_reg(cursor, UNW_ARM_R4, (unw_word_t *) &winContext->R4);
    unw_get_reg(cursor, UNW_ARM_R5, (unw_word_t *) &winContext->R5);
    unw_get_reg(cursor, UNW_ARM_R6, (unw_word_t *) &winContext->R6);
    unw_get_reg(cursor, UNW_ARM_R7, (unw_word_t *) &winContext->R7);
    unw_get_reg(cursor, UNW_ARM_R8, (unw_word_t *) &winContext->R8);
    unw_get_reg(cursor, UNW_ARM_R9, (unw_word_t *) &winContext->R9);
    unw_get_reg(cursor, UNW_ARM_R10, (unw_word_t *) &winContext->R10);
    unw_get_reg(cursor, UNW_ARM_R11, (unw_word_t *) &winContext->R11);
#elif defined(_ARM64_)
    unw_get_reg(cursor, UNW_REG_IP, (unw_word_t *) &winContext->Pc);
    unw_get_reg(cursor, UNW_REG_SP, (unw_word_t *) &winContext->Sp);
    unw_get_reg(cursor, UNW_AARCH64_X29, (unw_word_t *) &winContext->Fp);
    unw_get_reg(cursor, UNW_AARCH64_X30, (unw_word_t *) &winContext->Lr);
    unw_get_reg(cursor, UNW_AARCH64_X19, (unw_word_t *) &winContext->X19);
    unw_get_reg(cursor, UNW_AARCH64_X20, (unw_word_t *) &winContext->X20);
    unw_get_reg(cursor, UNW_AARCH64_X21, (unw_word_t *) &winContext->X21);
    unw_get_reg(cursor, UNW_AARCH64_X22, (unw_word_t *) &winContext->X22);
    unw_get_reg(cursor, UNW_AARCH64_X23, (unw_word_t *) &winContext->X23);
    unw_get_reg(cursor, UNW_AARCH64_X24, (unw_word_t *) &winContext->X24);
    unw_get_reg(cursor, UNW_AARCH64_X25, (unw_word_t *) &winContext->X25);
    unw_get_reg(cursor, UNW_AARCH64_X26, (unw_word_t *) &winContext->X26);
    unw_get_reg(cursor, UNW_AARCH64_X27, (unw_word_t *) &winContext->X27);
    unw_get_reg(cursor, UNW_AARCH64_X28, (unw_word_t *) &winContext->X28);
#else
#error unsupported architecture
#endif
}

static void GetContextPointer(unw_cursor_t *cursor, unw_context_t *unwContext, int reg, SIZE_T **contextPointer)
{
#if defined(HAVE_UNW_GET_SAVE_LOC)
    unw_save_loc_t saveLoc;
    unw_get_save_loc(cursor, reg, &saveLoc);
    if (saveLoc.type == UNW_SLT_MEMORY)
    {
        SIZE_T *pLoc = (SIZE_T *)saveLoc.u.addr;
        // Filter out fake save locations that point to unwContext 
        if (unwContext == NULL || (pLoc < (SIZE_T *)unwContext) || ((SIZE_T *)(unwContext + 1) <= pLoc))
            *contextPointer = (SIZE_T *)saveLoc.u.addr;
    }
#else
    // Returning NULL indicates that we don't have context pointers available
    *contextPointer = NULL;
#endif
}

static void GetContextPointers(unw_cursor_t *cursor, unw_context_t *unwContext, KNONVOLATILE_CONTEXT_POINTERS *contextPointers)
{
#if defined(_AMD64_)
    GetContextPointer(cursor, unwContext, UNW_X86_64_RBP, &contextPointers->Rbp);
    GetContextPointer(cursor, unwContext, UNW_X86_64_RBX, &contextPointers->Rbx);
    GetContextPointer(cursor, unwContext, UNW_X86_64_R12, &contextPointers->R12);
    GetContextPointer(cursor, unwContext, UNW_X86_64_R13, &contextPointers->R13);
    GetContextPointer(cursor, unwContext, UNW_X86_64_R14, &contextPointers->R14);
    GetContextPointer(cursor, unwContext, UNW_X86_64_R15, &contextPointers->R15);
#elif defined(_ARM_)
    GetContextPointer(cursor, unwContext, UNW_ARM_R4, &contextPointers->R4);
    GetContextPointer(cursor, unwContext, UNW_ARM_R5, &contextPointers->R5);
    GetContextPointer(cursor, unwContext, UNW_ARM_R6, &contextPointers->R6);
    GetContextPointer(cursor, unwContext, UNW_ARM_R7, &contextPointers->R7);
    GetContextPointer(cursor, unwContext, UNW_ARM_R8, &contextPointers->R8);
    GetContextPointer(cursor, unwContext, UNW_ARM_R9, &contextPointers->R9);
    GetContextPointer(cursor, unwContext, UNW_ARM_R10, &contextPointers->R10);
    GetContextPointer(cursor, unwContext, UNW_ARM_R11, &contextPointers->R11);
#elif defined(_ARM64_)
    GetContextPointer(cursor, unwContext, UNW_AARCH64_X19, &contextPointers->X19);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_X20, &contextPointers->X20);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_X21, &contextPointers->X21);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_X22, &contextPointers->X22);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_X23, &contextPointers->X23);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_X24, &contextPointers->X24);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_X25, &contextPointers->X25);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_X26, &contextPointers->X26);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_X27, &contextPointers->X27);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_X28, &contextPointers->X28);
#else
#error unsupported architecture
#endif
}

BOOL PAL_VirtualUnwind(CONTEXT *context, KNONVOLATILE_CONTEXT_POINTERS *contextPointers)
{
    int st;
    unw_context_t unwContext;
    unw_cursor_t cursor;

#if defined(__APPLE__) || defined(__FreeBSD__) || defined(__NetBSD__) || defined(_ARM64_) || defined(_ARM_)
    DWORD64 curPc;
#endif

    if ((context->ContextFlags & CONTEXT_EXCEPTION_ACTIVE) != 0)
    {
        // The current frame is a source of hardware exception. Due to the fact that
        // we use the low level unwinder to unwind just one frame a time, the
        // unwinder doesn't have the signal_frame flag set. So it doesn't
        // know that it should not decrement the PC before looking up the unwind info.
        // So we compensate it by incrementing the PC before passing it to the unwinder.
        // Without it, the unwinder would not find unwind info if the hardware exception
        // happened in the first instruction of a function.
        CONTEXTSetPC(context, CONTEXTGetPC(context) + 1);
    }

#if !UNWIND_CONTEXT_IS_UCONTEXT_T
    st = unw_getcontext(&unwContext);
    if (st < 0)
    {
        return FALSE;
    }
#endif

    WinContextToUnwindContext(context, &unwContext);

    st = unw_init_local(&cursor, &unwContext);
    if (st < 0)
    {
        return FALSE;
    }

#if !UNWIND_CONTEXT_IS_UCONTEXT_T
    // Set the unwind context to the specified windows context
    WinContextToUnwindCursor(context, &cursor);
#endif

#if defined(__APPLE__) || defined(__FreeBSD__) || defined(__NetBSD__)  || defined(_ARM64_) || defined(_ARM_)
    // FreeBSD, NetBSD and OSX appear to do two different things when unwinding
    // 1: If it reaches where it cannot unwind anymore, say a 
    // managed frame.  It wil return 0, but also update the $pc
    // 2: If it unwinds all the way to _start it will return
    // 0 from the step, but $pc will stay the same.
    // The behaviour of libunwind from nongnu.org is to null the PC
    // So we bank the original PC here, so we can compare it after
    // the step
    curPc = CONTEXTGetPC(context);
#endif

    st = unw_step(&cursor);
    if (st < 0)
    {
        return FALSE;
    }

    // Check if the frame we have unwound to is a frame that caused
    // synchronous signal, like a hardware exception and record it
    // in the context flags.
    if (unw_is_signal_frame(&cursor) > 0)
    {
        context->ContextFlags |= CONTEXT_EXCEPTION_ACTIVE;
#if defined(_ARM_) || defined(_ARM64_)
        context->ContextFlags &= ~CONTEXT_UNWOUND_TO_CALL;
#endif // _ARM_ || _ARM64_
    }
    else
    {
        context->ContextFlags &= ~CONTEXT_EXCEPTION_ACTIVE;
#if defined(_ARM_) || defined(_ARM64_)
        context->ContextFlags |= CONTEXT_UNWOUND_TO_CALL;
#endif // _ARM_ || _ARM64_
    }

    // Update the passed in windows context to reflect the unwind
    //
    UnwindContextToWinContext(&cursor, context);
#if defined(__APPLE__) || defined(__FreeBSD__) || defined(__NetBSD__)  || defined(_ARM64_) || defined(_ARM_)
    if (st == 0 && CONTEXTGetPC(context) == curPc)
    {
        CONTEXTSetPC(context, 0);
    }
#endif

    if (contextPointers != NULL)
    {
        GetContextPointers(&cursor, &unwContext, contextPointers);
    }
    return TRUE;
}

#else
#error don't know how to unwind on this platform
#endif

// These methods are only used on the AMD64 build
#ifdef _AMD64_
#ifdef HAVE_UNW_GET_ACCESSORS

static struct LibunwindCallbacksInfoType
{
     CONTEXT *Context;
     ReadMemoryWordCallback readMemCallback;
} LibunwindCallbacksInfo;

static int get_dyn_info_list_addr(unw_addr_space_t as, unw_word_t *dilap, void *arg)
{
    return -UNW_ENOINFO;
}

static int access_mem(unw_addr_space_t as, unw_word_t addr, unw_word_t *valp, int write, void *arg)
{
    if (write)
    {
        ASSERT("Memory write must never be called by libunwind during stackwalk");
        return -UNW_EINVAL;
    }

    // access_mem sometimes gets called by _UPT_find_proc_info, in such cases arg has a pointer to libunwind internal data
    // returned by _UPT_create. It makes it impossible to use arg for passing readMemCallback. That's why we have to use global variable.
    if (LibunwindCallbacksInfo.readMemCallback((SIZE_T)addr, (SIZE_T *)valp))
    {
        return UNW_ESUCCESS;
    }
    else 
    {
        return -UNW_EUNSPEC;
    }
}

static int access_reg(unw_addr_space_t as, unw_regnum_t regnum, unw_word_t *valp, int write, void *arg)
{
    if (write)
    {
        ASSERT("Register write must never be called by libunwind during stackwalk");
        return -UNW_EREADONLYREG;
    }

    CONTEXT *winContext = LibunwindCallbacksInfo.Context;

    switch (regnum) 
    {
#if defined(_AMD64_)
        case UNW_REG_IP:       *valp = (unw_word_t) winContext->Rip; break;
        case UNW_REG_SP:       *valp = (unw_word_t) winContext->Rsp; break;
        case UNW_X86_64_RBP:   *valp = (unw_word_t) winContext->Rbp; break;
        case UNW_X86_64_RBX:   *valp = (unw_word_t) winContext->Rbx; break;
        case UNW_X86_64_R12:   *valp = (unw_word_t) winContext->R12; break;
        case UNW_X86_64_R13:   *valp = (unw_word_t) winContext->R13; break;
        case UNW_X86_64_R14:   *valp = (unw_word_t) winContext->R14; break;
        case UNW_X86_64_R15:   *valp = (unw_word_t) winContext->R15; break;
#elif defined(_ARM_)
        case UNW_ARM_R13:      *valp = (unw_word_t) winContext->Sp; break;
        case UNW_ARM_R14:      *valp = (unw_word_t) winContext->Lr; break;
        case UNW_ARM_R15:      *valp = (unw_word_t) winContext->Pc; break;
        case UNW_ARM_R4:       *valp = (unw_word_t) winContext->R4; break;
        case UNW_ARM_R5:       *valp = (unw_word_t) winContext->R5; break;
        case UNW_ARM_R6:       *valp = (unw_word_t) winContext->R6; break;
        case UNW_ARM_R7:       *valp = (unw_word_t) winContext->R7; break;
        case UNW_ARM_R8:       *valp = (unw_word_t) winContext->R8; break;
        case UNW_ARM_R9:       *valp = (unw_word_t) winContext->R9; break;
        case UNW_ARM_R10:      *valp = (unw_word_t) winContext->R10; break;
        case UNW_ARM_R11:      *valp = (unw_word_t) winContext->R11; break;
#elif defined(_ARM64_)
        case UNW_REG_IP:       *valp = (unw_word_t) winContext->Pc; break;
        case UNW_REG_SP:       *valp = (unw_word_t) winContext->Sp; break;
        case UNW_AARCH64_X29:  *valp = (unw_word_t) winContext->Fp; break;
        case UNW_AARCH64_X30:  *valp = (unw_word_t) winContext->Lr; break;
        case UNW_AARCH64_X19:  *valp = (unw_word_t) winContext->X19; break;
        case UNW_AARCH64_X20:  *valp = (unw_word_t) winContext->X20; break;
        case UNW_AARCH64_X21:  *valp = (unw_word_t) winContext->X21; break;
        case UNW_AARCH64_X22:  *valp = (unw_word_t) winContext->X22; break;
        case UNW_AARCH64_X23:  *valp = (unw_word_t) winContext->X23; break;
        case UNW_AARCH64_X24:  *valp = (unw_word_t) winContext->X24; break;
        case UNW_AARCH64_X25:  *valp = (unw_word_t) winContext->X25; break;
        case UNW_AARCH64_X26:  *valp = (unw_word_t) winContext->X26; break;
        case UNW_AARCH64_X27:  *valp = (unw_word_t) winContext->X27; break;
        case UNW_AARCH64_X28:  *valp = (unw_word_t) winContext->X28; break;
#else
#error unsupported architecture
#endif
        default:
            ASSERT("Attempt to read an unknown register.");
            return -UNW_EBADREG;
    }
    return UNW_ESUCCESS;
}

static int access_fpreg(unw_addr_space_t as, unw_regnum_t regnum, unw_fpreg_t *fpvalp, int write, void *arg)
{
    ASSERT("Not supposed to be ever called");
    return -UNW_EINVAL;
}

static int resume(unw_addr_space_t as, unw_cursor_t *cp, void *arg)
{
    ASSERT("Not supposed to be ever called");
    return -UNW_EINVAL;
}

static int get_proc_name(unw_addr_space_t as, unw_word_t addr, char *bufp, size_t buf_len, unw_word_t *offp, void *arg)
{
    ASSERT("Not supposed to be ever called");
    return -UNW_EINVAL;  
}

int find_proc_info(unw_addr_space_t as, 
                   unw_word_t ip, unw_proc_info_t *pip,
                   int need_unwind_info, void *arg)
{
#ifdef HAVE_LIBUNWIND_PTRACE
    // UNIXTODO: libunwind RPM package on Fedora/CentOS/RedHat doesn't have libunwind-ptrace.so 
    // and we can't use it from a shared library like libmscordaccore.so.
    // That's why all calls to ptrace parts of libunwind ifdeffed out for now.
    return _UPT_find_proc_info(as, ip, pip, need_unwind_info, arg);
#else    
    return -UNW_EINVAL;
#endif    
}

void put_unwind_info(unw_addr_space_t as, unw_proc_info_t *pip, void *arg)
{
#ifdef HAVE_LIBUNWIND_PTRACE    
    return _UPT_put_unwind_info(as, pip, arg);
#endif    
}

static unw_accessors_t unwind_accessors =
{
    .find_proc_info = find_proc_info,
    .put_unwind_info = put_unwind_info,
    .get_dyn_info_list_addr = get_dyn_info_list_addr,
    .access_mem = access_mem,
    .access_reg = access_reg,
    .access_fpreg = access_fpreg,
    .resume = resume,
    .get_proc_name = get_proc_name
};

BOOL PAL_VirtualUnwindOutOfProc(CONTEXT *context, 
                                KNONVOLATILE_CONTEXT_POINTERS *contextPointers, 
                                DWORD pid, 
                                ReadMemoryWordCallback readMemCallback)
{
    // This function can be executed only by one thread at a time. 
    // The reason for this is that we need to pass context and read mem function to libunwind callbacks
    // but "arg" is already used by the pointer returned from _UPT_create(). 
    // So we resort to using global variables and a lock.
    struct Lock 
    {
        CRITICAL_SECTION cs;
        Lock()
        {        
            // ctor of a static variable is a thread-safe way to initialize critical section exactly once (clang,gcc)
            InitializeCriticalSection(&cs);
        }
    };
    struct LockHolder
    {
        CRITICAL_SECTION *cs;
        LockHolder(CRITICAL_SECTION *cs)
        {
            this->cs = cs;
            EnterCriticalSection(cs);
        }

        ~LockHolder()
        {
            LeaveCriticalSection(cs);
            cs = NULL;
        }
    };    
    static Lock lock;
    LockHolder lockHolder(&lock.cs);

    int st;
    unw_cursor_t cursor;
    unw_addr_space_t addrSpace = 0;
    void *libunwindUptPtr = NULL;
    BOOL result = FALSE;

    LibunwindCallbacksInfo.Context = context;
    LibunwindCallbacksInfo.readMemCallback = readMemCallback;

    addrSpace = unw_create_addr_space(&unwind_accessors, 0);
#ifdef HAVE_LIBUNWIND_PTRACE    
    libunwindUptPtr = _UPT_create(pid);
#endif    
    st = unw_init_remote(&cursor, addrSpace, libunwindUptPtr);
    if (st < 0)
    {
        result = FALSE;
        goto Exit;
    }

    st = unw_step(&cursor);
    if (st < 0)
    {
        result = FALSE;
        goto Exit;
    }

    UnwindContextToWinContext(&cursor, context);

    if (contextPointers != NULL)
    {
        GetContextPointers(&cursor, NULL, contextPointers);
    }
    result = TRUE;

Exit:
#ifdef HAVE_LIBUNWIND_PTRACE
    if (libunwindUptPtr != NULL) 
    {
        _UPT_destroy(libunwindUptPtr);
    }
#endif    
    if (addrSpace != 0) 
    {
        unw_destroy_addr_space(addrSpace);
    }    
    return result;
}
#else // HAVE_UNW_GET_ACCESSORS

BOOL PAL_VirtualUnwindOutOfProc(CONTEXT *context, 
                                KNONVOLATILE_CONTEXT_POINTERS *contextPointers, 
                                DWORD pid, 
                                ReadMemoryWordCallback readMemCallback)
{
    //UNIXTODO: Implement for Mac flavor of libunwind
    return FALSE;
}

#endif // !HAVE_UNW_GET_ACCESSORS
#endif // _AMD64_

struct ExceptionRecords
{
    CONTEXT ContextRecord;
    EXCEPTION_RECORD ExceptionRecord;
};

// Max number of fallback contexts that are used when malloc fails to allocate ExceptionRecords structure
static const int MaxFallbackContexts = sizeof(size_t) * 8;
// Array of fallback contexts
static ExceptionRecords s_fallbackContexts[MaxFallbackContexts];
// Bitmap used for allocating fallback contexts - bits set to 1 represent already allocated context.
static volatile size_t s_allocatedContextsBitmap = 0;

/*++
Function:
    AllocateExceptionRecords

    Allocate EXCEPTION_RECORD and CONTEXT structures for an exception.
Parameters:
    exceptionRecord - output pointer to the allocated exception record
    contextRecord - output pointer to the allocated context record
--*/
VOID
AllocateExceptionRecords(EXCEPTION_RECORD** exceptionRecord, CONTEXT** contextRecord)
{
    ExceptionRecords* records;
    if (posix_memalign((void**)&records, alignof(ExceptionRecords), sizeof(ExceptionRecords)) != 0)
    {
        size_t bitmap;
        size_t newBitmap;
        int index;

        do
        {
            bitmap = s_allocatedContextsBitmap;
            index = __builtin_ffsl(~bitmap) - 1;
            if (index < 0)
            {
                PROCAbort();
            }

            newBitmap = bitmap | ((size_t)1 << index);
        }
        while (__sync_val_compare_and_swap(&s_allocatedContextsBitmap, bitmap, newBitmap) != bitmap);

        records = &s_fallbackContexts[index];
    }

    *contextRecord = &records->ContextRecord;
    *exceptionRecord = &records->ExceptionRecord;
}

/*++
Function:
    PAL_FreeExceptionRecords

    Free EXCEPTION_RECORD and CONTEXT structures of an exception that were allocated by the
    AllocateExceptionRecords.
Parameters:
    exceptionRecord - exception record
    contextRecord - context record
--*/
VOID
PALAPI
PAL_FreeExceptionRecords(IN EXCEPTION_RECORD *exceptionRecord, IN CONTEXT *contextRecord)
{
    // Both records are allocated at once and the allocated memory starts at the contextRecord
    ExceptionRecords* records = (ExceptionRecords*)contextRecord;
    if ((records >= &s_fallbackContexts[0]) && (records < &s_fallbackContexts[MaxFallbackContexts]))
    {
        int index = records - &s_fallbackContexts[0];
        __sync_fetch_and_and(&s_allocatedContextsBitmap, ~((size_t)1 << index));
    }
    else
    {
        free(contextRecord);
    }
}

/*++
Function:
    RtlpRaiseException

Parameters:
    ExceptionRecord - the Windows exception record to throw

Note:
    The name of this function and the name of the ExceptionRecord 
    parameter is used in the sos lldb plugin code to read the exception
    record. See coreclr\src\ToolBox\SOS\lldbplugin\services.cpp.

    This function must not be inlined or optimized so the below PAL_VirtualUnwind
    calls end up with RaiseException caller's context and so the above debugger 
    code finds the function and ExceptionRecord parameter.
--*/
PAL_NORETURN
__attribute__((noinline))
__attribute__((optnone))
static void 
RtlpRaiseException(EXCEPTION_RECORD *ExceptionRecord, CONTEXT *ContextRecord)
{
    throw PAL_SEHException(ExceptionRecord, ContextRecord);
}

/*++
Function:
  RaiseException

See MSDN doc.
--*/
// no PAL_NORETURN, as callers must assume this can return for continuable exceptions.
__attribute__((noinline))
VOID
PALAPI
RaiseException(IN DWORD dwExceptionCode,
               IN DWORD dwExceptionFlags,
               IN DWORD nNumberOfArguments,
               IN CONST ULONG_PTR *lpArguments)
{
    // PERF_ENTRY_ONLY is used here because RaiseException may or may not
    // return. We can not get latency data without PERF_EXIT. For this reason,
    // PERF_ENTRY_ONLY is used to profile frequency only.
    PERF_ENTRY_ONLY(RaiseException);
    ENTRY("RaiseException(dwCode=%#x, dwFlags=%#x, nArgs=%u, lpArguments=%p)\n",
          dwExceptionCode, dwExceptionFlags, nNumberOfArguments, lpArguments);

    /* Validate parameters */
    if (dwExceptionCode & RESERVED_SEH_BIT)
    {
        WARN("Exception code %08x has bit 28 set; clearing it.\n", dwExceptionCode);
        dwExceptionCode ^= RESERVED_SEH_BIT;
    }

    if (nNumberOfArguments > EXCEPTION_MAXIMUM_PARAMETERS)
    {
        WARN("Number of arguments (%d) exceeds the limit "
            "EXCEPTION_MAXIMUM_PARAMETERS (%d); ignoring extra parameters.\n",
            nNumberOfArguments, EXCEPTION_MAXIMUM_PARAMETERS);
        nNumberOfArguments = EXCEPTION_MAXIMUM_PARAMETERS;
    }

    CONTEXT *contextRecord;
    EXCEPTION_RECORD *exceptionRecord;
    AllocateExceptionRecords(&exceptionRecord, &contextRecord);

    ZeroMemory(exceptionRecord, sizeof(EXCEPTION_RECORD));

    exceptionRecord->ExceptionCode = dwExceptionCode;
    exceptionRecord->ExceptionFlags = dwExceptionFlags;
    exceptionRecord->ExceptionRecord = NULL;
    exceptionRecord->ExceptionAddress = NULL; // will be set by RtlpRaiseException
    exceptionRecord->NumberParameters = nNumberOfArguments;
    if (nNumberOfArguments)
    {
        CopyMemory(exceptionRecord->ExceptionInformation, lpArguments,
                   nNumberOfArguments * sizeof(ULONG_PTR));
    }

    // Capture the context of RaiseException.
    ZeroMemory(contextRecord, sizeof(CONTEXT));
    contextRecord->ContextFlags = CONTEXT_FULL;
    CONTEXT_CaptureContext(contextRecord);

    // We have to unwind one level to get the actual context user code could be resumed at.
    PAL_VirtualUnwind(contextRecord, NULL);

    exceptionRecord->ExceptionAddress = (void *)CONTEXTGetPC(contextRecord);

    RtlpRaiseException(exceptionRecord, contextRecord);

    LOGEXIT("RaiseException returns\n");
}
