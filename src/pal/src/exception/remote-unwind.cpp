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
#include <dlfcn.h>

// Sub-headers included from the libunwind.h contain an empty struct
// and clang issues a warning. Until the libunwind is fixed, disable
// the warning.
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wextern-c-compat"
#include <libunwind.h>
#pragma clang diagnostic pop

SET_DEFAULT_DEBUG_CHANNEL(EXCEPT);

// Only used on the AMD64 build
#if defined(_AMD64_) && defined(HAVE_UNW_GET_ACCESSORS)

#include <elf.h>
#include <link.h>

#ifndef ElfW
#define ElfW(foo) Elf_ ## foo
#endif
#define Ehdr   ElfW(Ehdr)
#define Phdr   ElfW(Phdr)
#define Shdr   ElfW(Shdr)
#define Nhdr   ElfW(Nhdr)
#define Dyn    ElfW(Dyn)

extern void UnwindContextToWinContext(unw_cursor_t *cursor, CONTEXT *winContext);
extern void GetContextPointers(unw_cursor_t *cursor, unw_context_t *unwContext, KNONVOLATILE_CONTEXT_POINTERS *contextPointers);

typedef struct _libunwindInfo
{
    SIZE_T BaseAddress;
    CONTEXT *Context;
    UnwindReadMemoryCallback ReadMemory;
} libunwindInfo;

#define DW_EH_VERSION           1

// DWARF Pointer-Encoding (PEs).
//
// Pointer-Encodings were invented for the GCC exception-handling
// support for C++, but they represent a rather generic way of
// describing the format in which an address/pointer is stored.
// The Pointer-Encoding format is partially documented in Linux Base
// Spec v1.3 (http://www.linuxbase.org/spec/).

#define DW_EH_PE_FORMAT_MASK    0x0f    // format of the encoded value
#define DW_EH_PE_APPL_MASK      0x70    // how the value is to be applied
#define DW_EH_PE_indirect       0x80    // Flag bit. If set, the resulting pointer is the
                                        //  address of the word that contains the final address
// Pointer-encoding formats
#define DW_EH_PE_omit           0xff
#define DW_EH_PE_ptr            0x00    // pointer-sized unsigned value
#define DW_EH_PE_uleb128        0x01    // unsigned LE base-128 value
#define DW_EH_PE_udata2         0x02    // unsigned 16-bit value
#define DW_EH_PE_udata4         0x03    // unsigned 32-bit value
#define DW_EH_PE_udata8         0x04    // unsigned 64-bit value
#define DW_EH_PE_sleb128        0x09    // signed LE base-128 value
#define DW_EH_PE_sdata2         0x0a    // signed 16-bit value
#define DW_EH_PE_sdata4         0x0b    // signed 32-bit value
#define DW_EH_PE_sdata8         0x0c    // signed 64-bit value

// Pointer-encoding application
#define DW_EH_PE_absptr         0x00    // absolute value
#define DW_EH_PE_pcrel          0x10    // rel. to addr. of encoded value
#define DW_EH_PE_textrel        0x20    // text-relative (GCC-specific???)
#define DW_EH_PE_datarel        0x30    // data-relative

// The following are not documented by LSB v1.3, yet they are used by
// GCC, presumably they aren't documented by LSB since they aren't
// used on Linux
#define DW_EH_PE_funcrel        0x40    // start-of-procedure-relative
#define DW_EH_PE_aligned        0x50    // aligned pointer

#define DWARF_CIE_VERSION       3       // GCC emits version 1???

// DWARF frame header
typedef struct _eh_frame_hdr
{
    unsigned char version;
    unsigned char eh_frame_ptr_enc;
    unsigned char fde_count_enc;
    unsigned char table_enc;
    // The rest of the header is variable-length and consists of the
    // following members:
    //
    //   encoded_t eh_frame_ptr;
    //   encoded_t fde_count;
    //   struct
    //   {
    //      encoded_t start_ip;     // first address covered by this FDE
    //      encoded_t fde_offset;   // offset of the FDE
    //   } binary_search_table[fde_count]; 
} eh_frame_hdr;

// "DW_EH_PE_datarel|DW_EH_PE_sdata4" encoded fde table entry
typedef struct _table_entry
{
    int32_t start_ip;
    int32_t fde_offset;
} table_entry;

// DWARF unwind info
typedef struct dwarf_cie_info
{
    unw_word_t cie_instr_start;     // start addr. of CIE "initial_instructions"
    unw_word_t cie_instr_end;       // end addr. of CIE "initial_instructions"
    unw_word_t fde_instr_start;     // start addr. of FDE "instructions"
    unw_word_t fde_instr_end;       // end addr. of FDE "instructions"
    unw_word_t code_align;          // code-alignment factor
    unw_word_t data_align;          // data-alignment factor
    unw_word_t ret_addr_column;     // column of return-address register
    unw_word_t handler;             // address of personality-routine
    uint16_t abi;
    uint16_t tag;
    uint8_t fde_encoding;
    uint8_t lsda_encoding;
    unsigned int sized_augmentation : 1;
    unsigned int have_abi_marker : 1;
    unsigned int signal_frame : 1;
} dwarf_cie_info_t;

static bool 
ReadValue8(const libunwindInfo* info, unw_word_t* addr, uint8_t* valp)
{
    uint8_t value;
    if (!info->ReadMemory((PVOID)*addr, &value, sizeof(value))) {
        return false;
    }
    *addr += sizeof(value);
    *valp = value;
    return true;
}

static bool 
ReadValue16(const libunwindInfo* info, unw_word_t* addr, uint16_t* valp)
{
    uint16_t value;
    if (!info->ReadMemory((PVOID)*addr, &value, sizeof(value))) {
        return false;
    }
    *addr += sizeof(value);
    *valp = VAL16(value);
    return true;
}

static bool 
ReadValue32(const libunwindInfo* info, unw_word_t* addr, uint32_t* valp)
{
    uint32_t value;
    if (!info->ReadMemory((PVOID)*addr, &value, sizeof(value))) {
        return false;
    }
    *addr += sizeof(value);
    *valp = VAL32(value);
    return true;
}

static bool 
ReadValue64(const libunwindInfo* info, unw_word_t* addr, uint64_t* valp)
{
    uint64_t value;
    if (!info->ReadMemory((PVOID)*addr, &value, sizeof(value))) {
        return false;
    }
    *addr += sizeof(value);
    *valp = VAL64(value);
    return true;
}

static bool 
ReadPointer(const libunwindInfo* info, unw_word_t* addr, unw_word_t* valp)
{
#ifdef BIT64
    uint64_t val64;
    if (ReadValue64(info, addr, &val64)) {
        *valp = val64;
        return true;
    }
#else
    uint32_t val32;
    if (ReadValue32(info, addr, &val32)) {
        *valp = val32;
        return true;
    }
#endif
    return false;
}

// Read a unsigned "little-endian base 128" value. See Chapter 7.6 of DWARF spec v3.
static bool 
ReadULEB128(const libunwindInfo* info, unw_word_t* addr, unw_word_t* valp)
{
    unw_word_t value = 0;
    unsigned char byte;
    int shift = 0;

    do
    {
        if (!ReadValue8(info, addr, &byte)) {
            return false;
        }
        value |= ((unw_word_t)byte & 0x7f) << shift;
        shift += 7;
    } while (byte & 0x80);

    *valp = value;
    return true;
}

// Read a signed "little-endian base 128" value. See Chapter 7.6 of DWARF spec v3.
static bool 
ReadSLEB128(const libunwindInfo* info, unw_word_t* addr, unw_word_t* valp)
{
    unw_word_t value = 0;
    unsigned char byte;
    int shift = 0;

    do
    {
        if (!ReadValue8(info, addr, &byte)) {
            return false;
        }
        value |= ((unw_word_t)byte & 0x7f) << shift;
        shift += 7;
    } while (byte & 0x80);

    if ((shift < (8 * sizeof(unw_word_t))) && ((byte & 0x40) != 0)) {
        value |= ((unw_word_t)-1) << shift;
    }

    *valp = value;
    return true;
}

static bool 
ReadEncodedPointer(const libunwindInfo* info, unw_word_t* addr, unsigned char encoding, unw_word_t funcRel, unw_word_t* valp)
{
    unw_word_t initialAddr = *addr;
    uint16_t value16;
    uint32_t value32;
    uint64_t value64;
    unw_word_t value;

    if (encoding == DW_EH_PE_omit)
    {
        *valp = 0;
        return true;
    }
    else if (encoding == DW_EH_PE_aligned)
    {
        int size = sizeof(unw_word_t);
        *addr = (initialAddr + size - 1) & -size;
        return ReadPointer(info, addr, valp);
    }

    switch (encoding & DW_EH_PE_FORMAT_MASK)
    {
    case DW_EH_PE_ptr:
        if (!ReadPointer(info, addr, &value)) {
            return false;
        }
        break;

    case DW_EH_PE_uleb128:
        if (!ReadULEB128(info, addr, &value)) {
            return false;
        }
        break;

    case DW_EH_PE_sleb128:
        if (!ReadSLEB128(info, addr, &value)) {
            return false;
        }
        break;

    case DW_EH_PE_udata2:
        if (!ReadValue16(info, addr, &value16)) {
            return false;
        }
        value = value16;
        break;

    case DW_EH_PE_udata4:
        if (!ReadValue32(info, addr, &value32)) {
            return false;
        }
        value = value32;
        break;

    case DW_EH_PE_udata8:
        if (!ReadValue64(info, addr, &value64)) {
            return false;
        }
        value = value64;
        break;

    case DW_EH_PE_sdata2:
        if (!ReadValue16(info, addr, &value16)) {
            return false;
        }
        value = (int16_t)value16;
        break;

    case DW_EH_PE_sdata4:
        if (!ReadValue32(info, addr, &value32)) {
            return false;
        }
        value = (int32_t)value32;
        break;

    case DW_EH_PE_sdata8:
        if (!ReadValue64(info, addr, &value64)) {
            return false;
        }
        value = (int64_t)value64;
        break;

    default:
        ASSERT("ReadEncodedPointer: invalid encoding format %x\n", encoding);
        return false;
    }

    // 0 is a special value and always absolute
    if (value == 0) {
        *valp = 0;
        return true;
    }

    switch (encoding & DW_EH_PE_APPL_MASK)
    {
    case DW_EH_PE_absptr:
        break;

    case DW_EH_PE_pcrel:
        value += initialAddr;
        break;

    case DW_EH_PE_funcrel:
        _ASSERTE(funcRel != UINTPTR_MAX);
        value += funcRel;
        break;

    case DW_EH_PE_textrel:
    case DW_EH_PE_datarel:
    default:
        ASSERT("ReadEncodedPointer: invalid application type %x\n", encoding);
        return false;
    }

    if (encoding & DW_EH_PE_indirect)
    {
        unw_word_t indirect_addr = value;
        if (!ReadPointer(info, &indirect_addr, &value)) {
            return false;
        }
    }

    *valp = value;
    return true;
}

static bool 
LookupTableEntry(const libunwindInfo* info, int32_t ip, unw_word_t tableAddr, size_t tableCount, table_entry* entry, bool* found)
{
    size_t low, high, mid;
    unw_word_t addr;
    int32_t start_ip;

    *found = false;

    // do a binary search on table
    for (low = 0, high = tableCount; low < high;)
    {
        mid = (low + high) / 2;
        addr = tableAddr + (mid * sizeof(table_entry));

        if (!ReadValue32(info, &addr, (uint32_t*)&start_ip)) {
            return false;
        }
        if (ip < start_ip) {
            high = mid;
        }
        else {
            low = mid + 1;
        }
    }

    if (high > 0) {
        addr = tableAddr + ((high - 1) * sizeof(table_entry));
        // Assumes that the table_entry is two 32 bit values
        _ASSERTE(sizeof(*entry) == sizeof(uint64_t));
        if (!ReadValue64(info, &addr, (uint64_t*)entry)) {
            return false;
        }
        *found = true;
    }

    return true;
}

static bool
ParseCie(const libunwindInfo* info, unw_word_t addr, dwarf_cie_info_t* dci)
{
    uint8_t ch, version, fdeEncoding, handlerEncoding;
    unw_word_t cieLength, cieEndAddr;
    uint32_t value32;
    uint64_t value64;

    memset(dci, 0, sizeof (*dci));

    // Pick appropriate default for FDE-encoding. DWARF spec says
    // start-IP (initial_location) and the code-size (address_range) are
    // "address-unit sized constants".  The `R' augmentation can be used
    // to override this, but by default, we pick an address-sized unit
    // for fde_encoding.
#if BIT64
    fdeEncoding = DW_EH_PE_udata8;
#else
    fdeEncoding = DW_EH_PE_udata4;
#endif

    dci->lsda_encoding = DW_EH_PE_omit;
    dci->handler = 0;

    if (!ReadValue32(info, &addr, &value32)) {
        return false;
    }

    if (value32 != 0xffffffff)
    {
        // The CIE is in the 32-bit DWARF format
        uint32_t cieId;

        // DWARF says CIE id should be 0xffffffff, but in .eh_frame, it's 0
        const uint32_t expectedId = 0;

        cieLength = value32;
        cieEndAddr = addr + cieLength;

        if (!ReadValue32(info, &addr, &cieId)) {
            return false;
        }
        if (cieId != expectedId) {
            ASSERT("ParseCie: unexpected cie id %x\n", cieId);
            return false;
        }
    }
    else
    {
        // The CIE is in the 64-bit DWARF format
        uint64_t cieId;

        // DWARF says CIE id should be 0xffffffffffffffff, but in .eh_frame, it's 0
        const uint64_t expectedId = 0;

        if (!ReadValue64(info, &addr, &value64)) {
            return false;
        }
        cieLength = value64;
        cieEndAddr = addr + cieLength;

        if (!ReadValue64(info, &addr, &cieId)) {
            return false;
        }
        if (cieId != expectedId) {
            ASSERT("ParseCie: unexpected cie id %lx\n", cieId);
            return false;
        }
    }
    dci->cie_instr_end = cieEndAddr;

    if (!ReadValue8(info, &addr, &version)) {
        return false;
    }
    if (version != 1 && version != DWARF_CIE_VERSION) {
        ASSERT("ParseCie: invalid cie version %x\n", version);
        return false;
    }

    // Read the augmentation string
    uint8_t augmentationString[8];
    memset(augmentationString, 0, sizeof(augmentationString));

    for (int i = 0; i < sizeof(augmentationString); i++)
    {
        if (!ReadValue8(info, &addr, &ch)) {
            return false;
        }
        if (ch == 0) {
            break;
        }
        augmentationString[i] = ch;
    }

    // Read the code and data alignment
    if (!ReadULEB128(info, &addr, &dci->code_align)) {
        return false;
    }
    if (!ReadSLEB128(info, &addr, &dci->data_align)) {
        return false;
    }

    // Read the return-address column either as a u8 or as a uleb128
    if (version == 1)
    {
        if (!ReadValue8(info, &addr, &ch)) {
            return false;
        }
        dci->ret_addr_column = ch;
    }
    else
    {
        if (!ReadULEB128(info, &addr, &dci->ret_addr_column)) {
            return false;
        }
    }

    // Parse the augmentation string
    for (int i = 0; i < sizeof(augmentationString); i++)
    {
        bool done = false;
        unw_word_t augmentationSize;

        switch (augmentationString[i])
        {
        case '\0':
            done = true;
            break;

        case 'z':
            dci->sized_augmentation = 1;
            if (!ReadULEB128(info, &addr, &augmentationSize)) {
                return false;
            }
            break;

        case 'L':
            // read the LSDA pointer-encoding format
            if (!ReadValue8(info, &addr, &ch)) {
                return false;
            }
            dci->lsda_encoding = ch;
            break;

        case 'R':
            // read the FDE pointer-encoding format
            if (!ReadValue8(info, &addr, &fdeEncoding)) {
                return false;
            }
            break;

        case 'P':
            // read the personality-routine pointer-encoding format
            if (!ReadValue8(info, &addr, &handlerEncoding)) {
                return false;
            }
            if (!ReadEncodedPointer(info, &addr, handlerEncoding, UINTPTR_MAX, &dci->handler)) {
                return false;
            }
            break;

        case 'S':
           // This is a signal frame
           dci->signal_frame = 1;

           // Temporarily set it to one so dwarf_parse_fde() knows that
           // it should fetch the actual ABI/TAG pair from the FDE.
           dci->have_abi_marker = 1;
           break;

        default:
            if (dci->sized_augmentation) {
                // If we have the size of the augmentation body, we can skip
                // over the parts that we don't understand, so we're OK
                done = true;
                break;
            }
            ASSERT("ParseCie: unexpected argumentation string '%s'\n", augmentationString[i]);
            return false;
        }

        if (done) {
            break;
        }
    }
    dci->fde_encoding = fdeEncoding;
    dci->cie_instr_start = addr;
    return true;
}

static bool
ExtractProcInfoFromFde(const libunwindInfo* info, unw_word_t* addrp, unw_proc_info_t *pip, int need_unwind_info)
{
    unw_word_t addr = *addrp, fdeEndAddr, cieOffsetAddr, cieAddr;
    uint32_t value32;
    uint64_t value64;

    if (!ReadValue32(info, &addr, &value32)) {
        return false;
    }
    if (value32 != 0xffffffff)
    {
        int32_t cieOffset = 0;

        // In some configurations, an FDE with a 0 length indicates the end of the FDE-table
        if (value32 == 0) {
            return false;
        }
        // the FDE is in the 32-bit DWARF format */
        *addrp = fdeEndAddr = addr + value32;
        cieOffsetAddr = addr;

        if (!ReadValue32(info, &addr, (uint32_t*)&cieOffset)) {
            return false;
        }
        // Ignore CIEs (happens during linear search)
        if (cieOffset == 0) {
            return true;
        }
        // DWARF says that the CIE_pointer in the FDE is a .debug_frame-relative offset, 
        // but the GCC-generated .eh_frame sections instead store a "pcrelative" offset, 
        // which is just as fine as it's self-contained
        cieAddr = cieOffsetAddr - cieOffset;
    }
    else 
    {
        int64_t cieOffset = 0;

        // the FDE is in the 64-bit DWARF format */
        if (!ReadValue64(info, &addr, (uint64_t*)&value64)) {
            return false;
        }
        *addrp = fdeEndAddr = addr + value64;
        cieOffsetAddr = addr;

        if (!ReadValue64(info, &addr, (uint64_t*)&cieOffset)) {
            return false;
        }
        // Ignore CIEs (happens during linear search)
        if (cieOffset == 0) {
            return true;
        }
        // DWARF says that the CIE_pointer in the FDE is a .debug_frame-relative offset, 
        // but the GCC-generated .eh_frame sections instead store a "pcrelative" offset, 
        // which is just as fine as it's self-contained
        cieAddr = (unw_word_t)((uint64_t)cieOffsetAddr - cieOffset);
    }

    dwarf_cie_info_t dci;
    if (!ParseCie(info, cieAddr, &dci)) {
        return false;
    }

    unw_word_t ipStart, ipRange;
    if (!ReadEncodedPointer(info, &addr, dci.fde_encoding, UINTPTR_MAX, &ipStart)) {
        return false;
    }

    // IP-range has same encoding as FDE pointers, except that it's always an absolute value
    uint8_t ipRangeEncoding = dci.fde_encoding & DW_EH_PE_FORMAT_MASK;
    if (!ReadEncodedPointer(info, &addr, ipRangeEncoding, UINTPTR_MAX, &ipRange)) {
        return false;
    }
    pip->start_ip = ipStart;
    pip->end_ip = ipStart + ipRange;
    pip->handler = dci.handler;

    unw_word_t augmentationSize, augmentationEndAddr;
    if (dci.sized_augmentation) {
        if (!ReadULEB128(info, &addr, &augmentationSize)) {
            return false;
        }
        augmentationEndAddr = addr + augmentationSize;
    }

    // Read language specific data area address
    if (!ReadEncodedPointer(info, &addr, dci.lsda_encoding, pip->start_ip, &pip->lsda)) {
        return false;
    }

    // Now fill out the proc info if requested
    if (need_unwind_info)
    {
        if (dci.have_abi_marker)
        {
            if (!ReadValue16(info, &addr, &dci.abi)) {
                return false;
            }
            if (!ReadValue16(info, &addr, &dci.tag)) {
                return false;
            }
        }
        if (dci.sized_augmentation) {
            dci.fde_instr_start = augmentationEndAddr;
        }
        else {
            dci.fde_instr_start = addr;
        }
        dci.fde_instr_end = fdeEndAddr;

        pip->format = UNW_INFO_FORMAT_TABLE;
        pip->unwind_info_size = sizeof(dci);
        pip->unwind_info = malloc(sizeof(dci));
        if (pip->unwind_info == nullptr) {
            return -UNW_ENOMEM;
        }
        memcpy(pip->unwind_info, &dci, sizeof(dci));
    }

    return true;
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
#elif defined(_ARM_)
    case UNW_ARM_R13:      *valp = (unw_word_t)winContext->Sp; break;
    case UNW_ARM_R14:      *valp = (unw_word_t)winContext->Lr; break;
    case UNW_ARM_R15:      *valp = (unw_word_t)winContext->Pc; break;
    case UNW_ARM_R4:       *valp = (unw_word_t)winContext->R4; break;
    case UNW_ARM_R5:       *valp = (unw_word_t)winContext->R5; break;
    case UNW_ARM_R6:       *valp = (unw_word_t)winContext->R6; break;
    case UNW_ARM_R7:       *valp = (unw_word_t)winContext->R7; break;
    case UNW_ARM_R8:       *valp = (unw_word_t)winContext->R8; break;
    case UNW_ARM_R9:       *valp = (unw_word_t)winContext->R9; break;
    case UNW_ARM_R10:      *valp = (unw_word_t)winContext->R10; break;
    case UNW_ARM_R11:      *valp = (unw_word_t)winContext->R11; break;
#elif defined(_ARM64_)
    case UNW_REG_IP:       *valp = (unw_word_t)winContext->Pc; break;
    case UNW_REG_SP:       *valp = (unw_word_t)winContext->Sp; break;
    case UNW_AARCH64_X29:  *valp = (unw_word_t)winContext->Fp; break;
    case UNW_AARCH64_X30:  *valp = (unw_word_t)winContext->Lr; break;
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
#else
#error unsupported architecture
#endif
    default:
        ASSERT("Attempt to read an unknown register\n");
        return -UNW_EBADREG;
    }
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

    // The eh_frame header
    Phdr ehPhdr;
    memset(&ehPhdr, 0, sizeof(ehPhdr));

    // Search for the module's dynamic header and unwind frames
    Dyn* dynamicAddr = nullptr;

    for (int i = 0; i < phnum; i++, phdrAddr++)
    {
        Phdr ph;
        if (!info->ReadMemory(phdrAddr, &ph, sizeof(ph))) {
            ERROR("ELF: reading phdrAddr %p\n", phdrAddr);
            return -UNW_EINVAL;
        }
        TRACE("ELF: phdr %p type %d (%x) vaddr %p memsz %016llx paddr %p filesz %016llx offset %p align %016llx\n",
            phdrAddr, ph.p_type, ph.p_type, ph.p_vaddr, ph.p_memsz, ph.p_paddr, ph.p_filesz, ph.p_offset, ph.p_align);

        switch (ph.p_type)
        {
        case PT_DYNAMIC:
            if (ehdr.e_type == ET_EXEC) {
                dynamicAddr = reinterpret_cast<Dyn*>(ph.p_vaddr);
            }
            if (ehdr.e_type == ET_DYN) {
                dynamicAddr = reinterpret_cast<Dyn*>(ph.p_vaddr + info->BaseAddress);
            }
            break;

        case PT_GNU_EH_FRAME:
            ehPhdr = ph;
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
    unw_word_t ehFrameHdrAddr = ehPhdr.p_offset + info->BaseAddress;
    eh_frame_hdr ehFrameHdr;

    if (!info->ReadMemory((PVOID)ehFrameHdrAddr, &ehFrameHdr, sizeof(eh_frame_hdr))) {
        ERROR("ELF: reading ehFrameHdrAddr %p\n", ehFrameHdrAddr);
        return -UNW_EINVAL;
    }
    TRACE("ehFrameHdrAddr %p version %d eh_frame_ptr_enc %d fde_count_enc %d table_enc %d\n", 
        ehFrameHdrAddr, ehFrameHdr.version, ehFrameHdr.eh_frame_ptr_enc, ehFrameHdr.fde_count_enc, ehFrameHdr.table_enc);

    if (ehFrameHdr.version != DW_EH_VERSION) {
        ASSERT("ehFrameHdr version %x not supported\n", ehFrameHdr.version);
        return -UNW_EBADVERSION;
    }
    unw_word_t addr = ehFrameHdrAddr + sizeof(eh_frame_hdr);
    unw_word_t ehFrameStart;
    unw_word_t fdeCount;

    // Decode the eh_frame_hdr info
    if (!ReadEncodedPointer(info, &addr, ehFrameHdr.eh_frame_ptr_enc, UINTPTR_MAX, &ehFrameStart)) {
        ERROR("decoding eh_frame_ptr\n");
        return -UNW_EINVAL;
    }
    if (!ReadEncodedPointer(info, &addr, ehFrameHdr.fde_count_enc, UINTPTR_MAX, &fdeCount)) {
        ERROR("decoding fde_count_enc\n");
        return -UNW_EINVAL;
    }
    TRACE("ehFrameStart %p fdeCount %p ip offset %08x\n", ehFrameStart, fdeCount, (int32_t)(ip - ehFrameHdrAddr));

    // LookupTableEntry assumes this encoding
    if (ehFrameHdr.table_enc != (DW_EH_PE_datarel | DW_EH_PE_sdata4)) {
        ASSERT("Table encoding not supported %x\n", ehFrameHdr.table_enc);
        return -UNW_EINVAL;
    }
    // Find the fde using a binary search on the frame table
    table_entry entry;
    bool found;
    if (!LookupTableEntry(info, ip - ehFrameHdrAddr, addr, fdeCount, &entry, &found)) {
        ERROR("LookupTableEntry\n");
        return -UNW_EINVAL;
    }
    unw_word_t fdeAddr = entry.fde_offset + ehFrameHdrAddr;
    TRACE("start_ip %08x fde_offset %08x fdeAddr %p found %d\n", entry.start_ip, entry.fde_offset, fdeAddr, found);

    // Unwind info not found
    if (!found) {
        return -UNW_ENOINFO;
    }

    // Now get the unwind info
    if (!ExtractProcInfoFromFde(info, &fdeAddr, pip, need_unwind_info)) {
        ERROR("ExtractProcInfoFromFde\n");
        return -UNW_EINVAL;
    }

    _ASSERTE(ip >= pip->start_ip && ip <= pip->end_ip);
    return UNW_ESUCCESS;
}

static void 
put_unwind_info(unw_addr_space_t as, unw_proc_info_t *pip, void *arg)
{
    if (pip->unwind_info != nullptr) {
        free(pip->unwind_info);
        pip->unwind_info = nullptr;
    }
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

    UnwindContextToWinContext(&cursor, context);

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
