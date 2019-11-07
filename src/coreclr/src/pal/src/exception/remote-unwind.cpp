// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++

Module Name:

    remote-unwind.cpp

Abstract:

    Implementation of out of context unwind using libunwind8
    remote unwind API.

This file contains code based on libunwind8

Copyright (c) 2003-2005 Hewlett-Packard Development Company, L.P.
   Contributed by David Mosberger-Tang <davidm@hpl.hp.com>

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

--*/

#include "config.h"
#include "pal/palinternal.h"
#include "pal/dbgmsg.h"
#include "pal/critsect.h"
#include "pal/debug.h"
#include "pal_endian.h"
#include "pal.h"
#define __STDC_FORMAT_MACROS
#include <inttypes.h>
#include <dlfcn.h>

// Sub-headers included from the libunwind.h contain an empty struct
// and clang issues a warning. Until the libunwind is fixed, disable
// the warning.
#ifdef __llvm__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wextern-c-compat"
#endif
#include <libunwind.h>
#ifdef __llvm__
#pragma clang diagnostic pop
#endif

SET_DEFAULT_DEBUG_CHANNEL(EXCEPT);

#if defined(HAVE_UNW_GET_ACCESSORS)

#include <elf.h>
#include <link.h>

#if defined(_X86_) || defined(_ARM_)
#define PRIx PRIx32
#define PRIu PRIu32
#define PRId PRId32
#define PRIA "08"
#define PRIxA PRIA PRIx
#elif defined(_AMD64_) || defined(_ARM64_)
#define PRIx PRIx64
#define PRIu PRIu64
#define PRId PRId64
#define PRIA "016"
#define PRIxA PRIA PRIx
#endif

#ifndef ElfW
#define ElfW(foo) Elf_ ## foo
#endif
#define Ehdr   ElfW(Ehdr)
#define Phdr   ElfW(Phdr)
#define Shdr   ElfW(Shdr)
#define Nhdr   ElfW(Nhdr)
#define Dyn    ElfW(Dyn)

extern "C" int
_OOP_find_proc_info(
    unw_word_t start_ip,
    unw_word_t end_ip,
    unw_word_t eh_frame_table,
    unw_word_t eh_frame_table_len,
    unw_word_t exidx_frame_table,
    unw_word_t exidx_frame_table_len,
    unw_addr_space_t as,
    unw_word_t ip,
    unw_proc_info_t *pi,
    int need_unwind_info,
    void *arg);

extern void GetContextPointers(
    unw_cursor_t *cursor,
    unw_context_t *unwContext,
    KNONVOLATILE_CONTEXT_POINTERS *contextPointers);

typedef struct _libunwindInfo
{
    SIZE_T BaseAddress;
    CONTEXT *Context;
    UnwindReadMemoryCallback ReadMemory;
} libunwindInfo;

static void UnwindContextToContext(unw_cursor_t *cursor, CONTEXT *winContext)
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
#elif defined(_X86_)
    unw_get_reg(cursor, UNW_REG_IP, (unw_word_t *) &winContext->Eip);
    unw_get_reg(cursor, UNW_REG_SP, (unw_word_t *) &winContext->Esp);
    unw_get_reg(cursor, UNW_X86_EBP, (unw_word_t *) &winContext->Ebp);
    unw_get_reg(cursor, UNW_X86_EBX, (unw_word_t *) &winContext->Ebx);
    unw_get_reg(cursor, UNW_X86_ESI, (unw_word_t *) &winContext->Esi);
    unw_get_reg(cursor, UNW_X86_EDI, (unw_word_t *) &winContext->Edi);
#elif defined(_ARM_)
    unw_get_reg(cursor, UNW_REG_IP, (unw_word_t *) &winContext->Pc);
    unw_get_reg(cursor, UNW_REG_SP, (unw_word_t *) &winContext->Sp);
    unw_get_reg(cursor, UNW_ARM_R4, (unw_word_t *) &winContext->R4);
    unw_get_reg(cursor, UNW_ARM_R5, (unw_word_t *) &winContext->R5);
    unw_get_reg(cursor, UNW_ARM_R6, (unw_word_t *) &winContext->R6);
    unw_get_reg(cursor, UNW_ARM_R7, (unw_word_t *) &winContext->R7);
    unw_get_reg(cursor, UNW_ARM_R8, (unw_word_t *) &winContext->R8);
    unw_get_reg(cursor, UNW_ARM_R9, (unw_word_t *) &winContext->R9);
    unw_get_reg(cursor, UNW_ARM_R10, (unw_word_t *) &winContext->R10);
    unw_get_reg(cursor, UNW_ARM_R11, (unw_word_t *) &winContext->R11);
    unw_get_reg(cursor, UNW_ARM_R14, (unw_word_t *) &winContext->Lr);
    TRACE("sp %p pc %p lr %p\n", winContext->Sp, winContext->Pc, winContext->Lr);
#elif defined(_ARM64_)
    unw_get_reg(cursor, UNW_REG_IP, (unw_word_t *) &winContext->Pc);
    unw_get_reg(cursor, UNW_REG_SP, (unw_word_t *) &winContext->Sp);
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
    unw_get_reg(cursor, UNW_AARCH64_X29, (unw_word_t *) &winContext->Fp);
    unw_get_reg(cursor, UNW_AARCH64_X30, (unw_word_t *) &winContext->Lr);
    TRACE("sp %p pc %p lr %p fp %p\n", winContext->Sp, winContext->Pc, winContext->Lr, winContext->Fp);
#else
#error unsupported architecture
#endif
}

static int
get_dyn_info_list_addr(unw_addr_space_t as, unw_word_t *dilap, void *arg)
{
    return -UNW_ENOINFO;
}

static int
access_mem(unw_addr_space_t as, unw_word_t addr, unw_word_t *valp, int write, void *arg)
{
    if (write)
    {
        ASSERT("Memory write must never be called by libunwind during stackwalk\n");
        return -UNW_EINVAL;
    }
    const auto *info = (libunwindInfo*)arg;

    if (info->ReadMemory((PVOID)addr, valp, sizeof(*valp)))
    {
        return UNW_ESUCCESS;
    }
    else
    {
        return -UNW_EUNSPEC;
    }
}

static int
access_reg(unw_addr_space_t as, unw_regnum_t regnum, unw_word_t *valp, int write, void *arg)
{
    if (write)
    {
        ASSERT("Register write must never be called by libunwind during stackwalk\n");
        return -UNW_EREADONLYREG;
    }

    const auto *info = (libunwindInfo*)arg;
    CONTEXT *winContext = info->Context;

    switch (regnum)
    {
#if defined(_AMD64_)
    case UNW_REG_IP:       *valp = (unw_word_t)winContext->Rip; break;
    case UNW_REG_SP:       *valp = (unw_word_t)winContext->Rsp; break;
    case UNW_X86_64_RBP:   *valp = (unw_word_t)winContext->Rbp; break;
    case UNW_X86_64_RBX:   *valp = (unw_word_t)winContext->Rbx; break;
    case UNW_X86_64_R12:   *valp = (unw_word_t)winContext->R12; break;
    case UNW_X86_64_R13:   *valp = (unw_word_t)winContext->R13; break;
    case UNW_X86_64_R14:   *valp = (unw_word_t)winContext->R14; break;
    case UNW_X86_64_R15:   *valp = (unw_word_t)winContext->R15; break;
#elif defined(_X86_)
    case UNW_REG_IP:       *valp = (unw_word_t)winContext->Eip; break;
    case UNW_REG_SP:       *valp = (unw_word_t)winContext->Esp; break;
    case UNW_X86_EBX:      *valp = (unw_word_t)winContext->Ebx; break;
    case UNW_X86_ESI:      *valp = (unw_word_t)winContext->Esi; break;
    case UNW_X86_EDI:      *valp = (unw_word_t)winContext->Edi; break;
    case UNW_X86_EBP:      *valp = (unw_word_t)winContext->Ebp; break;
#elif defined(_ARM_)
    case UNW_ARM_R4:       *valp = (unw_word_t)winContext->R4; break;
    case UNW_ARM_R5:       *valp = (unw_word_t)winContext->R5; break;
    case UNW_ARM_R6:       *valp = (unw_word_t)winContext->R6; break;
    case UNW_ARM_R7:       *valp = (unw_word_t)winContext->R7; break;
    case UNW_ARM_R8:       *valp = (unw_word_t)winContext->R8; break;
    case UNW_ARM_R9:       *valp = (unw_word_t)winContext->R9; break;
    case UNW_ARM_R10:      *valp = (unw_word_t)winContext->R10; break;
    case UNW_ARM_R11:      *valp = (unw_word_t)winContext->R11; break;
    case UNW_ARM_R13:      *valp = (unw_word_t)winContext->Sp; break;
    case UNW_ARM_R14:      *valp = (unw_word_t)winContext->Lr; break;
    case UNW_ARM_R15:      *valp = (unw_word_t)winContext->Pc; break;
#elif defined(_ARM64_)
    case UNW_AARCH64_X19:  *valp = (unw_word_t)winContext->X19; break;
    case UNW_AARCH64_X20:  *valp = (unw_word_t)winContext->X20; break;
    case UNW_AARCH64_X21:  *valp = (unw_word_t)winContext->X21; break;
    case UNW_AARCH64_X22:  *valp = (unw_word_t)winContext->X22; break;
    case UNW_AARCH64_X23:  *valp = (unw_word_t)winContext->X23; break;
    case UNW_AARCH64_X24:  *valp = (unw_word_t)winContext->X24; break;
    case UNW_AARCH64_X25:  *valp = (unw_word_t)winContext->X25; break;
    case UNW_AARCH64_X26:  *valp = (unw_word_t)winContext->X26; break;
    case UNW_AARCH64_X27:  *valp = (unw_word_t)winContext->X27; break;
    case UNW_AARCH64_X28:  *valp = (unw_word_t)winContext->X28; break;
    case UNW_AARCH64_X29:  *valp = (unw_word_t)winContext->Fp; break;
    case UNW_AARCH64_X30:  *valp = (unw_word_t)winContext->Lr; break;
    case UNW_AARCH64_SP:   *valp = (unw_word_t)winContext->Sp; break;
    case UNW_AARCH64_PC:   *valp = (unw_word_t)winContext->Pc; break;
#else
#error unsupported architecture
#endif
    default:
        ASSERT("Attempt to read an unknown register %d\n", regnum);
        return -UNW_EBADREG;
    }
    TRACE("REG: %d %p\n", regnum, *valp);
    return UNW_ESUCCESS;
}

static int
access_fpreg(unw_addr_space_t as, unw_regnum_t regnum, unw_fpreg_t *fpvalp, int write, void *arg)
{
    ASSERT("Not supposed to be ever called\n");
    return -UNW_EINVAL;
}

static int
resume(unw_addr_space_t as, unw_cursor_t *cp, void *arg)
{
    ASSERT("Not supposed to be ever called\n");
    return -UNW_EINVAL;
}

static int
get_proc_name(unw_addr_space_t as, unw_word_t addr, char *bufp, size_t buf_len, unw_word_t *offp, void *arg)
{
    ASSERT("Not supposed to be ever called\n");
    return -UNW_EINVAL;
}

static int
find_proc_info(unw_addr_space_t as, unw_word_t ip, unw_proc_info_t *pip, int need_unwind_info, void *arg)
{
    const auto *info = (libunwindInfo*)arg;
    memset(pip, 0, sizeof(*pip));

    Ehdr ehdr;
    if (!info->ReadMemory((void*)info->BaseAddress, &ehdr, sizeof(ehdr))) {
        ERROR("ELF: reading ehdr %p\n", info->BaseAddress);
        return -UNW_EINVAL;
    }
    Phdr* phdrAddr = reinterpret_cast<Phdr*>(info->BaseAddress + ehdr.e_phoff);
    int phnum = ehdr.e_phnum;
    TRACE("ELF: base %p ip %p e_type %d e_phnum %d e_phoff %p\n", info->BaseAddress, ip, ehdr.e_type, ehdr.e_phnum, ehdr.e_phoff);

    unw_word_t loadbias = info->BaseAddress;
    for (int i = 0; i < phnum; i++)
    {
        Phdr ph;
        if (!info->ReadMemory(phdrAddr + i, &ph, sizeof(ph))) {
            ERROR("ELF: reading phdrAddr %p\n", phdrAddr + i);
            return -UNW_EINVAL;
        }
        if (ph.p_type == PT_LOAD && ph.p_offset == 0)
        {
            loadbias -= ph.p_vaddr;
            TRACE("PHDR: loadbias %p\n", loadbias);
            break;
        }
    }

    unw_word_t start_ip = (unw_word_t)-1;
    unw_word_t end_ip = 0;

    // The eh_frame header address
    unw_word_t ehFrameHdrAddr = 0;
    unw_word_t ehFrameHdrLen = 0;

    // The arm exidx header address
    unw_word_t exidxFrameHdrAddr = 0;
    unw_word_t exidxFrameHdrLen = 0;

    // Search for the module's dynamic header and unwind frames
    Dyn* dynamicAddr = nullptr;

    for (int i = 0; i < phnum; i++, phdrAddr++)
    {
        Phdr ph;
        if (!info->ReadMemory(phdrAddr, &ph, sizeof(ph))) {
            ERROR("ELF: reading phdrAddr %p\n", phdrAddr);
            return -UNW_EINVAL;
        }
        TRACE("ELF: phdr %p type %d (%x) vaddr %" PRIxA " memsz %" PRIxA " paddr %" PRIxA " filesz %" PRIxA " offset %" PRIxA " align %" PRIxA "\n",
            phdrAddr, ph.p_type, ph.p_type, ph.p_vaddr, ph.p_memsz, ph.p_paddr, ph.p_filesz, ph.p_offset, ph.p_align);

        switch (ph.p_type)
        {
        case PT_LOAD:
            if ((ip >= (loadbias + ph.p_vaddr)) && (ip < (loadbias + ph.p_vaddr + ph.p_memsz))) {
                start_ip = loadbias + ph.p_vaddr;
                end_ip = start_ip + ph.p_memsz;
                TRACE("ELF: found start_ip/end_ip\n");
            }
            break;

        case PT_DYNAMIC:
            dynamicAddr = reinterpret_cast<Dyn*>(loadbias + ph.p_vaddr);
            break;

        case PT_GNU_EH_FRAME:
            ehFrameHdrAddr = loadbias + ph.p_vaddr;
            ehFrameHdrLen = ph.p_memsz;
            break;

        case PT_ARM_EXIDX:
            exidxFrameHdrAddr = loadbias + ph.p_vaddr;
            exidxFrameHdrLen = ph.p_memsz;
            break;
        }
    }

    if (dynamicAddr != nullptr)
    {
        for (;;)
        {
            Dyn dyn;
            if (!info->ReadMemory(dynamicAddr, &dyn, sizeof(dyn))) {
                ERROR("ELF: reading dynamicAddr %p\n", dynamicAddr);
                return -UNW_EINVAL;
            }
            if (dyn.d_tag == DT_PLTGOT) {
                TRACE("ELF: dyn %p tag %d (%x) d_ptr %p\n", dynamicAddr, dyn.d_tag, dyn.d_tag, dyn.d_un.d_ptr);
                pip->gp = dyn.d_un.d_ptr;
                break;
            }
            else if (dyn.d_tag == DT_NULL) {
                break;
            }
            dynamicAddr++;
        }
    }

    return _OOP_find_proc_info(start_ip, end_ip, ehFrameHdrAddr, ehFrameHdrLen, exidxFrameHdrAddr, exidxFrameHdrLen, as, ip, pip, need_unwind_info, arg);
}

static void
put_unwind_info(unw_addr_space_t as, unw_proc_info_t *pip, void *arg)
{
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

/*++
Function:
    PAL_VirtualUnwindOutOfProc

    Unwind the stack given the context for a "remote" target using the
    provided read memory callback.

    Assumes the IP is in the module of the base address provided (coreclr).

Parameters:
    context - the start context in the target
    contextPointers - the context of the next frame
    baseAddress - base address of the module to find the unwind info
    readMemoryCallback - reads memory from the target
--*/
BOOL
PALAPI
PAL_VirtualUnwindOutOfProc(CONTEXT *context, KNONVOLATILE_CONTEXT_POINTERS *contextPointers, SIZE_T baseAddress, UnwindReadMemoryCallback readMemoryCallback)
{
    unw_addr_space_t addrSpace = 0;
    unw_cursor_t cursor;
    libunwindInfo info;
    BOOL result = FALSE;
    int st;

    info.BaseAddress = baseAddress;
    info.Context = context;
    info.ReadMemory = readMemoryCallback;

    addrSpace = unw_create_addr_space(&unwind_accessors, 0);

    st = unw_init_remote(&cursor, addrSpace, &info);
    if (st < 0)
    {
        result = FALSE;
        goto exit;
    }

    st = unw_step(&cursor);
    if (st < 0)
    {
        result = FALSE;
        goto exit;
    }

    UnwindContextToContext(&cursor, context);

    if (contextPointers != NULL)
    {
        GetContextPointers(&cursor, NULL, contextPointers);
    }
    result = TRUE;

exit:
    if (addrSpace != 0)
    {
        unw_destroy_addr_space(addrSpace);
    }
    return result;
}

#else

BOOL
PALAPI
PAL_VirtualUnwindOutOfProc(CONTEXT *context, KNONVOLATILE_CONTEXT_POINTERS *contextPointers, SIZE_T baseAddress, UnwindReadMemoryCallback readMemoryCallback)
{
    return FALSE;
}

#endif // defined(_AMD64_) && defined(HAVE_UNW_GET_ACCESSORS)
