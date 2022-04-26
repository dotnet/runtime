// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

#ifdef HOST_UNIX

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

#ifdef __APPLE__
#include <mach/mach.h>
#include <mach-o/loader.h>
#include <mach-o/nlist.h>
#include <mach-o/dyld_images.h>
#include "compact_unwind_encoding.h"
#define MACOS_ARM64_POINTER_AUTH_MASK 0x7fffffffffffull
#endif

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

#define TRACE_VERBOSE

#include "crosscomp.h"

#define KNONVOLATILE_CONTEXT_POINTERS T_KNONVOLATILE_CONTEXT_POINTERS
#define CONTEXT T_CONTEXT

#else // HOST_UNIX

#include <windows.h>
#define __STDC_FORMAT_MACROS
#include <inttypes.h>
#include <libunwind.h>
#include "debugmacros.h"
#include "crosscomp.h"

#define KNONVOLATILE_CONTEXT_POINTERS T_KNONVOLATILE_CONTEXT_POINTERS
#define CONTEXT T_CONTEXT

typedef BOOL(*UnwindReadMemoryCallback)(PVOID address, PVOID buffer, SIZE_T size);

#define ASSERT(x, ...)
#define TRACE(x, ...)
#undef ERROR
#define ERROR(x, ...)

#ifdef TARGET_64BIT
#define ElfW(foo) Elf64_ ## foo
#else // TARGET_64BIT
#define ElfW(foo) Elf32_ ## foo
#endif // TARGET_64BIT

#define PALAPI

#endif // HOST_UNIX

#ifdef HAVE_UNW_GET_ACCESSORS

#ifdef HOST_UNIX
#include <link.h>
#endif // HOST_UNIX

#include <elf.h>

#if defined(TARGET_X86) || defined(TARGET_ARM)
#define PRIx PRIx32
#define PRIu PRIu32
#define PRId PRId32
#define PRIA "08"
#define PRIxA PRIA PRIx
#elif defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_S390X) || defined(TARGET_LOONGARCH64)
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

#ifndef FEATURE_USE_SYSTEM_LIBUNWIND
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
#endif // FEATURE_USE_SYSTEM_LIBUNWIND

#endif // HAVE_UNW_GET_ACCESSORS

#if defined(__APPLE__) || defined(HAVE_UNW_GET_ACCESSORS)

typedef struct _libunwindInfo
{
    SIZE_T BaseAddress;
    CONTEXT *Context;
    ULONG64 FunctionStart;
    UnwindReadMemoryCallback ReadMemory;
} libunwindInfo;

#if defined(__APPLE__) || defined(FEATURE_USE_SYSTEM_LIBUNWIND)

#define EXTRACT_BITS(value, mask)   ((value >> __builtin_ctz(mask)) & (((1 << __builtin_popcount(mask))) - 1))

#define DW_EH_VERSION           1

// DWARF Pointer-Encoding (PEs).
//
// Pointer-Encodings were invented for the GCC exception-handling
// support for C++, but they represent a rather generic way of
// describing the format in which an address/pointer is stored.
// The Pointer-Encoding format is partially documented in Linux Base
// Spec v1.3 (http://www.linuxbase.org/spec/).

// These defines and struct (dwarf_cie_info) were copied from libunwind's dwarf.h

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
typedef struct __attribute__((packed)) _eh_frame_hdr
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
typedef struct table_entry
{
    int32_t start_ip;
    int32_t fde_offset;
} table_entry_t;

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
ReadValue8(
    const libunwindInfo* info,
    unw_word_t* addr,
    uint8_t* valp)
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
ReadValue16(
    const libunwindInfo* info,
    unw_word_t* addr,
    uint16_t* valp)
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
ReadValue32(
    const libunwindInfo* info,
    unw_word_t* addr,
    uint32_t* valp)
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
ReadValue64(
    const libunwindInfo* info,
    unw_word_t* addr,
    uint64_t* valp)
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
ReadPointer(
    const libunwindInfo* info,
    unw_word_t* addr,
    unw_word_t* valp)
{
#ifdef TARGET_64BIT
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
ReadULEB128(
    const libunwindInfo* info,
    unw_word_t* addr,
    unw_word_t* valp)
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
ReadSLEB128(
    const libunwindInfo* info,
    unw_word_t* addr,
    unw_word_t* valp)
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

    if (((size_t)shift < (8 * sizeof(unw_word_t))) && ((byte & 0x40) != 0)) {
        value |= ((unw_word_t)-1) << shift;
    }

    *valp = value;
    return true;
}

static bool
ReadEncodedPointer(
    const libunwindInfo* info,
    unw_word_t* addr,
    unsigned char encoding,
    unw_word_t funcRel,
    unw_word_t* valp)
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

template<class T>
static bool
BinarySearchEntries(
    const libunwindInfo* info,
    int32_t ip,
    unw_word_t tableAddr,
    size_t tableCount,
    T* entry,
    T* entryNext,
    bool compressed,
    bool* found)
{
    size_t low, high, mid;
    unw_word_t addr;
    int32_t functionOffset;

    *found = false;

    static_assert_no_msg(sizeof(T) >= sizeof(uint32_t));

#ifdef __APPLE__
    static_assert_no_msg(offsetof(unwind_info_section_header_index_entry, functionOffset) == 0);
    static_assert_no_msg(sizeof(unwind_info_section_header_index_entry::functionOffset) == sizeof(uint32_t));

    static_assert_no_msg(offsetof(unwind_info_regular_second_level_entry, functionOffset) == 0);
    static_assert_no_msg(sizeof(unwind_info_regular_second_level_entry::functionOffset) == sizeof(uint32_t));

    static_assert_no_msg(offsetof(unwind_info_section_header_lsda_index_entry, functionOffset) == 0);
    static_assert_no_msg(sizeof(unwind_info_section_header_lsda_index_entry::functionOffset) == sizeof(uint32_t));
#endif // __APPLE__

    // Do a binary search on table
    for (low = 0, high = tableCount; low < high;)
    {
        mid = (low + high) / 2;

        // Assumes that the first uint32_t in T is the offset to compare
        addr = tableAddr + (mid * sizeof(T));
        if (!ReadValue32(info, &addr, (uint32_t*)&functionOffset)) {
            return false;
        }
#ifdef __APPLE__
        if (compressed) {
            functionOffset = UNWIND_INFO_COMPRESSED_ENTRY_FUNC_OFFSET(functionOffset);
        }
#endif // __APPLE__
        if (ip < functionOffset) {
            high = mid;
        }
        else {
            low = mid + 1;
        }
    }

    if (high > 0)
    {
        *found = true;

        addr = tableAddr + (high * sizeof(T));
        if (!info->ReadMemory((PVOID)addr, entryNext, sizeof(T))) {
            return false;
        }
    }
    else
    {
        // When the ip isn't found return the last entry in the table
        high = tableCount;

        // No next entry
        memset(entryNext, 0, sizeof(T));
    }

    addr = tableAddr + ((high - 1) * sizeof(T));
    if (!info->ReadMemory((PVOID)addr, entry, sizeof(T))) {
        return false;
    }

    return true;
}

static bool
ParseCie(
    const libunwindInfo* info,
    unw_word_t addr,
    dwarf_cie_info_t* dci)
{
    uint8_t ch, version, fdeEncoding, handlerEncoding;
    unw_word_t cieLength, cieEndAddr;
    uint32_t value32;
    uint64_t value64;

    memset(dci, 0, sizeof(dwarf_cie_info_t));

    // Pick appropriate default for FDE-encoding. DWARF spec says
    // start-IP (initial_location) and the code-size (address_range) are
    // "address-unit sized constants".  The `R' augmentation can be used
    // to override this, but by default, we pick an address-sized unit
    // for fde_encoding.
#if TARGET_64BIT
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

    for (size_t i = 0; i < sizeof(augmentationString); i++)
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
    for (size_t i = 0; i < sizeof(augmentationString); i++)
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
ExtractFde(
    const libunwindInfo* info,
    unw_word_t* addr,
    unw_word_t* fdeEndAddr,
    unw_word_t* ipStart,
    unw_word_t* ipEnd,
    dwarf_cie_info_t* dci)
{
    unw_word_t cieOffsetAddr, cieAddr;
    uint32_t value32;
    uint64_t value64;

    *fdeEndAddr = 0;
    *ipStart = UINT64_MAX;
    *ipEnd = 0;

    if (!ReadValue32(info, addr, &value32)) {
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
        *fdeEndAddr = *addr + value32;
        cieOffsetAddr = *addr;

        if (!ReadValue32(info, addr, (uint32_t*)&cieOffset)) {
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
        if (!ReadValue64(info, addr, (uint64_t*)&value64)) {
            return false;
        }
        *fdeEndAddr = *addr + value64;
        cieOffsetAddr = *addr;

        if (!ReadValue64(info, addr, (uint64_t*)&cieOffset)) {
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

    if (!ParseCie(info, cieAddr, dci)) {
        return false;
    }

    unw_word_t start, range;
    if (!ReadEncodedPointer(info, addr, dci->fde_encoding, UINTPTR_MAX, &start)) {
        return false;
    }

    // IP-range has same encoding as FDE pointers, except that it's always an absolute value
    uint8_t ipRangeEncoding = dci->fde_encoding & DW_EH_PE_FORMAT_MASK;
    if (!ReadEncodedPointer(info, addr, ipRangeEncoding, UINTPTR_MAX, &range)) {
        return false;
    }

    *ipStart = start;
    *ipEnd = start + range;

    return true;
}

static bool
ExtractProcInfoFromFde(
    const libunwindInfo* info,
    unw_word_t* addrp,
    unw_proc_info_t *pip,
    int need_unwind_info)
{
    unw_word_t addr = *addrp;

    unw_word_t ipStart, ipEnd;
    unw_word_t fdeEndAddr;
    dwarf_cie_info_t dci;
    if (!ExtractFde(info, &addr, &fdeEndAddr, &ipStart, &ipEnd, &dci)) {
        return false;
    }
    *addrp = fdeEndAddr;

    pip->start_ip = ipStart;
    pip->end_ip = ipEnd;
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

#ifdef __APPLE__

static bool
SearchCompactEncodingSection(
    libunwindInfo* info,
    unw_word_t ip,
    unw_word_t compactUnwindSectionAddr,
    unw_proc_info_t *pip)
{
    unwind_info_section_header sectionHeader;
    if (!info->ReadMemory((PVOID)compactUnwindSectionAddr, &sectionHeader, sizeof(sectionHeader))) {
        return false;
    }
    int32_t offset = ip - info->BaseAddress;

    TRACE("Unwind %p offset %08x ver %d common off: %08x common cnt: %d pers off: %08x pers cnt: %d index off: %08x index cnt: %d\n",
        (void*)compactUnwindSectionAddr,
        offset,
        sectionHeader.version,
        sectionHeader.commonEncodingsArraySectionOffset,
        sectionHeader.commonEncodingsArrayCount,
        sectionHeader.personalityArraySectionOffset,
        sectionHeader.personalityArrayCount,
        sectionHeader.indexSectionOffset,
        sectionHeader.indexCount);

    if (sectionHeader.version != UNWIND_SECTION_VERSION) {
        return false;
    }

    unwind_info_section_header_index_entry entry;
    unwind_info_section_header_index_entry entryNext;
    bool found;
    if (!BinarySearchEntries(info, offset, compactUnwindSectionAddr + sectionHeader.indexSectionOffset, sectionHeader.indexCount, &entry, &entryNext, false, &found)) {
        return false;
    }
    if (!found) {
        ERROR("Top level index not found\n");
        return false;
    }

    uint32_t firstLevelFunctionOffset = entry.functionOffset;
    uint32_t firstLevelNextPageFunctionOffset = entryNext.functionOffset;

    unw_word_t secondLevelAddr = compactUnwindSectionAddr + entry.secondLevelPagesSectionOffset;
    unw_word_t lsdaArrayStartAddr = compactUnwindSectionAddr + entry.lsdaIndexArraySectionOffset;
    unw_word_t lsdaArrayEndAddr = compactUnwindSectionAddr + entryNext.lsdaIndexArraySectionOffset;

    uint32_t encoding = 0;
    unw_word_t funcStart = 0;
    unw_word_t funcEnd = 0;
    unw_word_t lsda = 0;
    unw_word_t personality = 0;

    uint32_t pageKind;
    if (!info->ReadMemory((PVOID)secondLevelAddr, &pageKind, sizeof(pageKind))) {
        return false;
    }
    if (pageKind == UNWIND_SECOND_LEVEL_REGULAR)
    {
        unwind_info_regular_second_level_page_header pageHeader;
        if (!info->ReadMemory((PVOID)secondLevelAddr, &pageHeader, sizeof(pageHeader))) {
            return false;
        }

        unwind_info_regular_second_level_entry pageEntry;
        unwind_info_regular_second_level_entry pageEntryNext;
        if (!BinarySearchEntries(info, offset, secondLevelAddr + pageHeader.entryPageOffset, pageHeader.entryCount, &pageEntry, &pageEntryNext, false, &found)) {
            return false;
        }

        encoding = pageEntry.encoding;
        TRACE("Second level regular: %08x for offset %08x\n", encoding, offset);
        funcStart = pageEntry.functionOffset + info->BaseAddress;
        if (found) {
            funcEnd = pageEntryNext.functionOffset + info->BaseAddress;
        }
        else {
            funcEnd = firstLevelNextPageFunctionOffset + info->BaseAddress;
            TRACE("Second level regular pageEntry not found start %p end %p\n", (void*)funcStart, (void*)funcEnd);
        }

        if (ip < funcStart || ip > funcEnd) {
            ERROR("ip %p not in regular second level\n", (void*)ip);
            return false;
        }
    }
    else if (pageKind == UNWIND_SECOND_LEVEL_COMPRESSED)
    {
        unwind_info_compressed_second_level_page_header pageHeader;
        if (!info->ReadMemory((PVOID)secondLevelAddr, &pageHeader, sizeof(pageHeader))) {
            return false;
        }

        uint32_t pageOffset = offset - firstLevelFunctionOffset;
        uint32_t pageEntry;
        uint32_t pageEntryNext;
        if (!BinarySearchEntries(info, pageOffset, secondLevelAddr + pageHeader.entryPageOffset, pageHeader.entryCount, &pageEntry, &pageEntryNext, true, &found)) {
            return false;
        }

        funcStart = UNWIND_INFO_COMPRESSED_ENTRY_FUNC_OFFSET(pageEntry) + firstLevelFunctionOffset + info->BaseAddress;
        if (found) {
            funcEnd = UNWIND_INFO_COMPRESSED_ENTRY_FUNC_OFFSET(pageEntryNext) + firstLevelFunctionOffset + info->BaseAddress;
        }
        else {
            funcEnd = firstLevelNextPageFunctionOffset + info->BaseAddress;
            TRACE("Second level compressed pageEntry not found start %p end %p\n", (void*)funcStart, (void*)funcEnd);
        }

        TRACE("Second level compressed: funcStart %p funcEnd %p pageEntry %08x pageEntryNext %08x\n", (void*)funcStart, (void*)funcEnd, pageEntry, pageEntryNext);

        if (ip < funcStart || ip > funcEnd) {
            ERROR("ip %p not in compressed second level\n", (void*)ip);
            return false;
        }

        uint16_t encodingIndex = UNWIND_INFO_COMPRESSED_ENTRY_ENCODING_INDEX(pageEntry);
        if (encodingIndex < sectionHeader.commonEncodingsArrayCount)
        {
            // Encoding is in common table in section header
            unw_word_t addr = compactUnwindSectionAddr + sectionHeader.commonEncodingsArraySectionOffset + (encodingIndex * sizeof(uint32_t));
            if (!ReadValue32(info, &addr, &encoding)) {
                return false;
            }
            TRACE("Second level compressed common table: %08x for offset %08x encodingIndex %d\n", encoding, pageOffset, encodingIndex);
        }
        else
        {
            // Encoding is in page specific table
            uint16_t pageEncodingIndex = encodingIndex - (uint16_t)sectionHeader.commonEncodingsArrayCount;
            if (pageEncodingIndex >= pageHeader.encodingsCount) {
                ERROR("pageEncodingIndex(%d) > page specific table encodingsCount(%d)\n", pageEncodingIndex, pageHeader.encodingsCount);
                return false;
            }
            unw_word_t addr = secondLevelAddr + pageHeader.encodingsPageOffset + (pageEncodingIndex * sizeof(uint32_t));
            if (!ReadValue32(info, &addr, &encoding)) {
                return false;
            }
            TRACE("Second level compressed page specific table: %08x for offset %08x\n", encoding, pageOffset);
        }
    }
    else
    {
        ERROR("Invalid __unwind_info\n");
        return false;
    }

    if (encoding & UNWIND_HAS_LSDA)
    {
        uint32_t funcStartOffset = funcStart - info->BaseAddress;
        size_t lsdaArrayCount = (lsdaArrayEndAddr - lsdaArrayStartAddr) / sizeof(unwind_info_section_header_lsda_index_entry);

        unwind_info_section_header_lsda_index_entry lsdaEntry;
        unwind_info_section_header_lsda_index_entry lsdaEntryNext;
        if (!BinarySearchEntries(info, funcStartOffset, lsdaArrayStartAddr, lsdaArrayCount, &lsdaEntry, &lsdaEntryNext, false, &found)) {
            return false;
        }
        if (!found || funcStartOffset != lsdaEntry.functionOffset || lsdaEntry.lsdaOffset == 0) {
            ERROR("lsda not found(%d), not exact match (%08x != %08x) or lsda(%08x) == 0\n",
                found, funcStartOffset, lsdaEntry.functionOffset, lsdaEntry.lsdaOffset);
            return false;
        }
        lsda = lsdaEntry.lsdaOffset + info->BaseAddress;
    }

    uint32_t personalityIndex = (encoding & UNWIND_PERSONALITY_MASK) >> (__builtin_ctz(UNWIND_PERSONALITY_MASK));
    if (personalityIndex != 0)
    {
        --personalityIndex; // change 1-based to zero-based index
        if (personalityIndex > sectionHeader.personalityArrayCount)
        {
            ERROR("Invalid personality index\n");
            return false;
        }
        int32_t personalityDelta;
        unw_word_t addr = compactUnwindSectionAddr + sectionHeader.personalityArraySectionOffset + personalityIndex * sizeof(uint32_t);
        if (!ReadValue32(info, &addr, (uint32_t*)&personalityDelta)) {
            return false;
        }
        addr = personalityDelta + info->BaseAddress;
        if (!ReadPointer(info, &addr, &personality)) {
            return false;
        }
    }

    info->FunctionStart = funcStart;
    pip->start_ip = funcStart;
    pip->end_ip = funcEnd;
    pip->lsda = lsda;
    pip->handler = personality;
    pip->gp = 0;
    pip->flags = 0;
    pip->format = encoding;
    pip->unwind_info = 0;
    pip->unwind_info_size = 0;

    TRACE("Encoding %08x start %p end %p found for ip %p\n", encoding, (void*)funcStart, (void*)funcEnd, (void*)ip);
    return true;
}

static bool
SearchDwarfSection(
    const libunwindInfo* info,
    unw_word_t ip,
    unw_word_t dwarfSectionAddr,
    unw_word_t dwarfSectionSize,
    uint32_t fdeSectionHint,
    unw_proc_info_t *pip,
    int need_unwind_info)
{
    unw_word_t addr = dwarfSectionAddr + fdeSectionHint;
    unw_word_t fdeAddr;
    while (addr < (dwarfSectionAddr + dwarfSectionSize))
    {
        fdeAddr = addr;

        unw_word_t ipStart, ipEnd;
        unw_word_t fdeEndAddr;
        dwarf_cie_info_t dci;
        if (!ExtractFde(info, &addr, &fdeEndAddr, &ipStart, &ipEnd, &dci)) {
            ERROR("ExtractFde FAILED for ip %p\n", (void*)ip);
            break;
        }

        if (ip >= ipStart && ip < ipEnd) {
            if (!ExtractProcInfoFromFde(info, &fdeAddr, pip, need_unwind_info)) {
                ERROR("ExtractProcInfoFromFde FAILED for ip %p\n", (void*)ip);
                break;
            }
            return true;
        }
        addr = fdeEndAddr;
    }
    return false;
}


static bool
GetProcInfo(unw_word_t ip, unw_proc_info_t *pip, libunwindInfo* info, bool* step, int need_unwind_info)
{
    memset(pip, 0, sizeof(*pip));
    *step = false;

    mach_header_64 header;
    if (!info->ReadMemory((void*)info->BaseAddress, &header, sizeof(mach_header_64))) {
        ERROR("Reading header %p\n", (void*)info->BaseAddress);
        return false;
    }
    // Read load commands
    void* commandsAddress = (void*)(info->BaseAddress + sizeof(mach_header_64));
    load_command* commands = (load_command*)malloc(header.sizeofcmds);
    if (commands == nullptr)
    {
        ERROR("Failed to allocate %d byte load commands\n", header.sizeofcmds);
        return false;
    }
    if (!info->ReadMemory(commandsAddress, commands, header.sizeofcmds))
    {
        ERROR("Failed to read load commands at %p of %d\n", commandsAddress, header.sizeofcmds);
        return false;
    }
    unw_word_t compactUnwindSectionAddr = 0;
    unw_word_t compactUnwindSectionSize = 0;
    unw_word_t ehframeSectionAddr = 0;
    unw_word_t ehframeSectionSize = 0;
    mach_vm_address_t loadBias = 0;

    load_command* command = commands;
    for (int i = 0; i < header.ncmds; i++)
    {
        if (command->cmd == LC_SEGMENT_64)
        {
            segment_command_64* segment = (segment_command_64*)command;

            // Calculate the load bias for the module. This is the value to add to the vmaddr of a
            // segment to get the actual address.
            if (strcmp(segment->segname, SEG_TEXT) == 0)
            {
                loadBias = info->BaseAddress - segment->vmaddr;
                TRACE_VERBOSE("CMD: load bias %016llx\n", loadBias);
            }

            section_64* section = (section_64*)((uint64_t)segment + sizeof(segment_command_64));
            for (int s = 0; s < segment->nsects; s++, section++)
            {
                TRACE_VERBOSE("     addr %016llx size %016llx off %08x align %02x flags %02x %s %s\n",
                    section->addr,
                    section->size,
                    section->offset,
                    section->align,
                    section->flags,
                    section->segname,
                    section->sectname);

                if (strcmp(section->sectname, "__unwind_info") == 0)
                {
                    compactUnwindSectionAddr = section->addr + loadBias;
                    compactUnwindSectionSize = section->size;
                }
                if (strcmp(section->sectname, "__eh_frame") == 0)
                {
                    ehframeSectionAddr = section->addr + loadBias;
                    ehframeSectionSize = section->size;
                }
            }
        }
        // Get next load command
        command = (load_command*)((char*)command + command->cmdsize);
    }

    // If there is a compact unwind encoding table, look there first
    if (compactUnwindSectionAddr != 0)
    {
        if (SearchCompactEncodingSection(info, ip, compactUnwindSectionAddr, pip))
        {
            if (ehframeSectionAddr != 0)
            {
#ifdef TARGET_AMD64
                if ((pip->format & UNWIND_X86_64_MODE_MASK) == UNWIND_X86_64_MODE_DWARF)
                {
                    uint32_t dwarfOffsetHint = pip->format & UNWIND_X86_64_DWARF_SECTION_OFFSET;
#elif TARGET_ARM64
                if ((pip->format & UNWIND_ARM64_MODE_MASK) == UNWIND_ARM64_MODE_DWARF)
                {
                    uint32_t dwarfOffsetHint = pip->format & UNWIND_ARM64_DWARF_SECTION_OFFSET;
#else
#error unsupported architecture
#endif
                    if (SearchDwarfSection(info, ip, ehframeSectionAddr, ehframeSectionSize, dwarfOffsetHint, pip, need_unwind_info)) {
                        TRACE("SUCCESS: found in eh frame from compact hint for %p\n", (void*)ip);
                        return true;
                    }
                }
            }
            // Need to do a compact step based on pip->format and pip->start_ip
            TRACE("Compact step %p format %08x start_ip %p\n", (void*)ip, pip->format, (void*)pip->start_ip);
            *step = true;
            return true;
        }
    }

    // Look in dwarf unwind info next
    if (ehframeSectionAddr != 0)
    {
        if (SearchDwarfSection(info, ip, ehframeSectionAddr, ehframeSectionSize, 0, pip, need_unwind_info)) {
            TRACE("SUCCESS: found in eh frame for %p\n", (void*)ip);
            return true;
        }
    }

    ERROR("Unwind info not found for %p format %08x ehframeSectionAddr %p ehframeSectionSize %p\n", (void*)ip, pip->format, (void*)ehframeSectionAddr, (void*)ehframeSectionSize);
    return false;
}

//===-- CompactUnwindInfo.cpp ---------------------------------------------===//
//
// Part of the LLVM Project, under the Apache License v2.0 with LLVM Exceptions.
// See https://llvm.org/LICENSE.txt for license information.
// SPDX-License-Identifier: Apache-2.0 WITH LLVM-exception
//
//===----------------------------------------------------------------------===//

#if defined(TARGET_AMD64)

static bool
StepWithCompactEncodingRBPFrame(const libunwindInfo* info, compact_unwind_encoding_t compactEncoding)
{
    CONTEXT* context = info->Context;
    uint32_t savedRegistersOffset = EXTRACT_BITS(compactEncoding, UNWIND_X86_64_RBP_FRAME_OFFSET);
    uint32_t savedRegistersLocations = EXTRACT_BITS(compactEncoding, UNWIND_X86_64_RBP_FRAME_REGISTERS);
    unw_word_t savedRegisters = context->Rbp - (sizeof(uint64_t) * savedRegistersOffset);

    // See compact_unwind_encoding.h for the format bit layout details
    for (int i = 0; i < 5; ++i)
    {
        switch (savedRegistersLocations & 0x7)
        {
            case UNWIND_X86_64_REG_NONE:
                // no register saved in this slot
                break;
            case UNWIND_X86_64_REG_RBX:
                if (!ReadValue64(info, &savedRegisters, (uint64_t*)&context->Rbx)) {
                    return false;
                }
                break;
            case UNWIND_X86_64_REG_R12:
                if (!ReadValue64(info, &savedRegisters, (uint64_t*)&context->R12)) {
                    return false;
                }
                break;
            case UNWIND_X86_64_REG_R13:
                if (!ReadValue64(info, &savedRegisters, (uint64_t*)&context->R13)) {
                    return false;
                }
                break;
            case UNWIND_X86_64_REG_R14:
                if (!ReadValue64(info, &savedRegisters, (uint64_t*)&context->R14)) {
                    return false;
                }
                break;
            case UNWIND_X86_64_REG_R15:
                if (!ReadValue64(info, &savedRegisters, (uint64_t*)&context->R15)) {
                    return false;
                }
                break;
            default:
                ERROR("Invalid register for RBP frame %08x\n", compactEncoding);
                return false;
        }
        savedRegistersLocations = (savedRegistersLocations >> 3);
    }
    uint64_t rbp = context->Rbp;

    // ebp points to old ebp
    unw_word_t addr = rbp;
    if (!ReadValue64(info, &addr, (uint64_t*)&context->Rbp)) {
        return false;
    }

    // old esp is ebp less saved ebp and return address
    context->Rsp = rbp + (sizeof(uint64_t) * 2);

    // pop return address into eip
    addr = rbp + sizeof(uint64_t);
    if (!ReadValue64(info, &addr, (uint64_t*)&context->Rip)) {
        return false;
    }

    TRACE("SUCCESS: compact step encoding %08x rip %p rsp %p rbp %p\n",
        compactEncoding, (void*)context->Rip, (void*)context->Rsp, (void*)context->Rbp);
    return true;
}

static bool
StepWithCompactEncodingFrameless(const libunwindInfo* info, compact_unwind_encoding_t compactEncoding, unw_word_t functionStart)
{
    int mode = compactEncoding & UNWIND_X86_64_MODE_MASK;
    CONTEXT* context = info->Context;

    uint32_t stack_size = EXTRACT_BITS(compactEncoding, UNWIND_X86_64_FRAMELESS_STACK_SIZE);
    uint32_t register_count = EXTRACT_BITS(compactEncoding, UNWIND_X86_64_FRAMELESS_STACK_REG_COUNT);
    uint32_t permutation = EXTRACT_BITS(compactEncoding, UNWIND_X86_64_FRAMELESS_STACK_REG_PERMUTATION);

    if (mode == UNWIND_X86_64_MODE_STACK_IND)
    {
        _ASSERTE(functionStart != 0);
        unw_word_t addr = functionStart + stack_size;
        if (!ReadValue32(info, &addr, &stack_size)) {
            return false;
        }
        uint32_t stack_adjust = EXTRACT_BITS(compactEncoding, UNWIND_X86_64_FRAMELESS_STACK_ADJUST);
        stack_size += stack_adjust * 8;
    }
    else
    {
        stack_size *= 8;
    }

    TRACE("Frameless function: encoding %08x stack size %d register count %d\n", compactEncoding, stack_size, register_count);

    // We need to include (up to) 6 registers in 10 bits.
    // That would be 18 bits if we just used 3 bits per reg to indicate
    // the order they're saved on the stack.
    //
    // This is done with Lehmer code permutation, e.g. see
    // http://stackoverflow.com/questions/1506078/fast-permutation-number-permutation-mapping-algorithms
    int permunreg[6];

    // This decodes the variable-base number in the 10 bits
    // and gives us the Lehmer code sequence which can then
    // be decoded.
    switch (register_count) {
        case 6:
            permunreg[0] = permutation / 120; // 120 == 5!
            permutation -= (permunreg[0] * 120);
            permunreg[1] = permutation / 24; // 24 == 4!
            permutation -= (permunreg[1] * 24);
            permunreg[2] = permutation / 6; // 6 == 3!
            permutation -= (permunreg[2] * 6);
            permunreg[3] = permutation / 2; // 2 == 2!
            permutation -= (permunreg[3] * 2);
            permunreg[4] = permutation; // 1 == 1!
            permunreg[5] = 0;
            break;
        case 5:
            permunreg[0] = permutation / 120;
            permutation -= (permunreg[0] * 120);
            permunreg[1] = permutation / 24;
            permutation -= (permunreg[1] * 24);
            permunreg[2] = permutation / 6;
            permutation -= (permunreg[2] * 6);
            permunreg[3] = permutation / 2;
            permutation -= (permunreg[3] * 2);
            permunreg[4] = permutation;
            break;
        case 4:
            permunreg[0] = permutation / 60;
            permutation -= (permunreg[0] * 60);
            permunreg[1] = permutation / 12;
            permutation -= (permunreg[1] * 12);
            permunreg[2] = permutation / 3;
            permutation -= (permunreg[2] * 3);
            permunreg[3] = permutation;
            break;
        case 3:
            permunreg[0] = permutation / 20;
            permutation -= (permunreg[0] * 20);
            permunreg[1] = permutation / 4;
            permutation -= (permunreg[1] * 4);
            permunreg[2] = permutation;
            break;
        case 2:
            permunreg[0] = permutation / 5;
            permutation -= (permunreg[0] * 5);
            permunreg[1] = permutation;
            break;
        case 1:
            permunreg[0] = permutation;
            break;
    }

    // Decode the Lehmer code for this permutation of
    // the registers v. http://en.wikipedia.org/wiki/Lehmer_code
    int registers[6] = {UNWIND_X86_64_REG_NONE, UNWIND_X86_64_REG_NONE,
                        UNWIND_X86_64_REG_NONE, UNWIND_X86_64_REG_NONE,
                        UNWIND_X86_64_REG_NONE, UNWIND_X86_64_REG_NONE};
    bool used[7] = {false, false, false, false, false, false, false};
    for (int i = 0; i < register_count; i++)
    {
        int renum = 0;
        for (int j = 1; j < 7; j++)
        {
            if (!used[j])
            {
                if (renum == permunreg[i])
                {
                    registers[i] = j;
                    used[j] = true;
                    break;
                }
                renum++;
            }
        }
    }

    uint64_t savedRegisters = context->Rsp + stack_size - 8 - (8 * register_count);
    for (int i = 0; i < register_count; i++)
    {
        uint64_t reg;
        if (!ReadValue64(info, &savedRegisters, &reg)) {
            return false;
        }
        switch (registers[i]) {
            case UNWIND_X86_64_REG_RBX:
                context->Rbx = reg;
                break;
            case UNWIND_X86_64_REG_R12:
                context->R12 = reg;
                break;
            case UNWIND_X86_64_REG_R13:
                context->R13 = reg;
                break;
            case UNWIND_X86_64_REG_R14:
                context->R14 = reg;
                break;
            case UNWIND_X86_64_REG_R15:
                context->R15 = reg;
                break;
            case UNWIND_X86_64_REG_RBP:
                context->Rbp = reg;
                break;
            default:
                ERROR("Bad register for frameless\n");
                break;
        }
    }

    // Now unwind the frame
    uint64_t ip;
    if (!ReadValue64(info, &savedRegisters, &ip)) {
        return false;
    }
    context->Rip = ip;
    context->Rsp = savedRegisters;

    TRACE("SUCCESS: frameless encoding %08x rip %p rsp %p rbp %p\n",
        compactEncoding, (void*)context->Rip, (void*)context->Rsp, (void*)context->Rbp);
    return true;
}

#define AMD64_SYSCALL_OPCODE 0x050f

static bool
StepWithCompactNoEncoding(const libunwindInfo* info)
{
    // We get here because we found the function the IP is in the compact unwind info, but the encoding is 0. This
    // usually ends the unwind but here we check that the function is a syscall "wrapper" and assume there is no
    // frame and pop the return address.
    uint16_t opcode;
    unw_word_t addr = info->Context->Rip - sizeof(opcode);
    if (!ReadValue16(info, &addr, &opcode)) {
        return false;
    }
    // Is the IP pointing just after a "syscall" opcode?
    if (opcode != AMD64_SYSCALL_OPCODE) {
        // There are cases where the IP points one byte after the syscall; not sure why.
        addr = info->Context->Rip - sizeof(opcode) + 1;
        if (!ReadValue16(info, &addr, &opcode)) {
            return false;
        }
        // Is the IP pointing just after a "syscall" opcode + 1?
        if (opcode != AMD64_SYSCALL_OPCODE) {
            ERROR("StepWithCompactNoEncoding: not in syscall wrapper function\n");
            return false;
        }
    }
    // Pop the return address from the stack
    uint64_t ip;
    addr = info->Context->Rsp;
    if (!ReadValue64(info, &addr, &ip)) {
        return false;
    }
    info->Context->Rip = ip;
    info->Context->Rsp += sizeof(uint64_t);
    TRACE("StepWithCompactNoEncoding: SUCCESS new rip %p rsp %p\n", (void*)info->Context->Rip, (void*)info->Context->Rsp);
    return true;
}

#endif // TARGET_AMD64

#if defined(TARGET_ARM64)

#define ARM64_SYSCALL_OPCODE    0xD4001001
#define ARM64_BL_OPCODE_MASK    0xFC000000
#define ARM64_BL_OPCODE         0x94000000
#define ARM64_BLR_OPCODE_MASK   0xFFFFFC00
#define ARM64_BLR_OPCODE        0xD63F0000
#define ARM64_BLRA_OPCODE_MASK  0xFEFFF800
#define ARM64_BLRA_OPCODE       0xD63F0800

static bool
StepWithCompactNoEncoding(const libunwindInfo* info)
{
    // Check that the function is a syscall "wrapper" and assume there is no frame and pop the return address.
    uint32_t opcode;
    unw_word_t addr = info->Context->Pc - sizeof(opcode);
    if (!ReadValue32(info, &addr, &opcode)) {
        ERROR("StepWithCompactNoEncoding: can read opcode %p\n", (void*)addr);
        return false;
    }
    // Is the IP pointing just after a "syscall" opcode?
    if (opcode != ARM64_SYSCALL_OPCODE) {
        ERROR("StepWithCompactNoEncoding: not in syscall wrapper function\n");
        return false;
    }
    // Pop the return address from the stack
    info->Context->Pc = info->Context->Lr;
    TRACE("StepWithCompactNoEncoding: SUCCESS new pc %p sp %p\n", (void*)info->Context->Pc, (void*)info->Context->Sp);
    return true;
}

static bool
ReadCompactEncodingRegister(const libunwindInfo* info, unw_word_t* addr, DWORD64* reg)
{
    uint64_t value;
    if (!info->ReadMemory((PVOID)*addr, &value, sizeof(value))) {
        return false;
    }
    *reg = VAL64(value);
    *addr -= sizeof(uint64_t);
    return true;
}

static bool
ReadCompactEncodingRegisterPair(const libunwindInfo* info, unw_word_t* addr, DWORD64* first, DWORD64* second)
{
    // Registers are effectively pushed in pairs
    //
    // *first = **addr
    // *addr -= 8
    // *second= **addr
    // *addr -= 8
    if (!ReadCompactEncodingRegister(info, addr, first)) {
        return false;
    }
    if (!ReadCompactEncodingRegister(info, addr, second)) {
        return false;
    }
    return true;
}

static bool
ReadCompactEncodingRegisterPair(const libunwindInfo* info, unw_word_t* addr, NEON128* first, NEON128* second)
{
    if (!ReadCompactEncodingRegisterPair(info, addr, &first->Low, &second->Low)) {
        return false;
    }
    first->High = 0;
    second->High = 0;
    return true;
}

// Saved registers are pushed
// + in pairs
// + in register number order (after the option frame registers)
// + after the callers SP
//
// Given C++ code that generates this prologue spill sequence
//
// sub     sp, sp, #128            ; =128
// stp     d15, d14, [sp, #16]     ; 16-byte Folded Spill
// stp     d13, d12, [sp, #32]     ; 16-byte Folded Spill
// stp     d11, d10, [sp, #48]     ; 16-byte Folded Spill
// stp     d9, d8, [sp, #64]       ; 16-byte Folded Spill
// stp     x22, x21, [sp, #80]     ; 16-byte Folded Spill
// stp     x20, x19, [sp, #96]     ; 16-byte Folded Spill
// stp     x29, x30, [sp, #112]    ; 16-byte Folded Spill
// add     x29, sp, #112           ; =112
//
// The compiler generates:
//   compactEncoding = 0x04000f03;
static bool
StepWithCompactEncodingArm64(const libunwindInfo* info, compact_unwind_encoding_t compactEncoding, bool hasFrame)
{
    CONTEXT* context = info->Context;
    unw_word_t addr;

    if (hasFrame)
    {
        context->Sp = context->Fp + 16;
        addr = context->Fp + 8;
        if (!ReadCompactEncodingRegisterPair(info, &addr, &context->Lr, &context->Fp)) {
            return false;
        }
        // Strip pointer authentication bits 
        context->Lr &= MACOS_ARM64_POINTER_AUTH_MASK;
    }
    else
    {
        // Get the leat significant bit in UNWIND_ARM64_FRAMELESS_STACK_SIZE_MASK
        uint64_t stackSizeScale = UNWIND_ARM64_FRAMELESS_STACK_SIZE_MASK & ~(UNWIND_ARM64_FRAMELESS_STACK_SIZE_MASK - 1);
        uint64_t stackSize = ((compactEncoding & UNWIND_ARM64_FRAMELESS_STACK_SIZE_MASK) / stackSizeScale) * 16;

        addr = context->Sp + stackSize;
    }

    // Unwound return address is stored in Lr
    context->Pc = context->Lr;

    if (compactEncoding & UNWIND_ARM64_FRAME_X19_X20_PAIR &&
        !ReadCompactEncodingRegisterPair(info, &addr, &context->X[19], &context->X[20])) {
            return false;
    }
    if (compactEncoding & UNWIND_ARM64_FRAME_X21_X22_PAIR &&
        !ReadCompactEncodingRegisterPair(info, &addr, &context->X[21], &context->X[22])) {
            return false;
    }
    if (compactEncoding & UNWIND_ARM64_FRAME_X23_X24_PAIR &&
        !ReadCompactEncodingRegisterPair(info, &addr, &context->X[23], &context->X[24])) {
            return false;
    }
    if (compactEncoding & UNWIND_ARM64_FRAME_X25_X26_PAIR &&
        !ReadCompactEncodingRegisterPair(info, &addr, &context->X[25], &context->X[26])) {
            return false;
    }
    if (compactEncoding & UNWIND_ARM64_FRAME_X27_X28_PAIR &&
        !ReadCompactEncodingRegisterPair(info, &addr, &context->X[27], &context->X[28])) {
            return false;
    }
    if (compactEncoding & UNWIND_ARM64_FRAME_D8_D9_PAIR &&
        !ReadCompactEncodingRegisterPair(info, &addr, &context->V[8], &context->V[9])) {
            return false;
    }
    if (compactEncoding & UNWIND_ARM64_FRAME_D10_D11_PAIR &&
        !ReadCompactEncodingRegisterPair(info, &addr, &context->V[10], &context->V[11])) {
            return false;
    }
    if (compactEncoding & UNWIND_ARM64_FRAME_D12_D13_PAIR &&
        !ReadCompactEncodingRegisterPair(info, &addr, &context->V[12], &context->V[13])) {
            return false;
    }
    if (compactEncoding & UNWIND_ARM64_FRAME_D14_D15_PAIR &&
        !ReadCompactEncodingRegisterPair(info, &addr, &context->V[14], &context->V[15])) {
            return false;
    }
    if (!hasFrame)
    {
        context->Sp = addr;
    }
    TRACE("SUCCESS: compact step encoding %08x pc %p sp %p fp %p lr %p\n",
        compactEncoding, (void*)context->Pc, (void*)context->Sp, (void*)context->Fp, (void*)context->Lr);
    return true;
}

#endif // TARGET_ARM64

static bool
StepWithCompactEncoding(const libunwindInfo* info, compact_unwind_encoding_t compactEncoding, unw_word_t functionStart)
{
    if (compactEncoding == 0)
    {
        return StepWithCompactNoEncoding(info);
    }
#if defined(TARGET_AMD64)
    switch (compactEncoding & UNWIND_X86_64_MODE_MASK)
    {
        case UNWIND_X86_64_MODE_RBP_FRAME:
            return StepWithCompactEncodingRBPFrame(info, compactEncoding);

        case UNWIND_X86_64_MODE_STACK_IMMD:
        case UNWIND_X86_64_MODE_STACK_IND:
            return StepWithCompactEncodingFrameless(info, compactEncoding, functionStart);

        case UNWIND_X86_64_MODE_DWARF:
            return false;
    }
#elif defined(TARGET_ARM64)
    switch (compactEncoding & UNWIND_ARM64_MODE_MASK)
    {
        case UNWIND_ARM64_MODE_FRAME:
            return StepWithCompactEncodingArm64(info, compactEncoding, true);

        case UNWIND_ARM64_MODE_FRAMELESS:
            return StepWithCompactEncodingArm64(info, compactEncoding, false);

        case UNWIND_ARM64_MODE_DWARF:
            return false;
    }
#else
#error unsupported architecture
#endif
    ERROR("Invalid encoding %08x\n", compactEncoding);
    return false;
}

#endif // __APPLE__

#endif // defined(__APPLE__) || defined(FEATURE_USE_SYSTEM_LIBUNWIND)

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
#if defined(TARGET_AMD64)
    GetContextPointer(cursor, unwContext, UNW_X86_64_RBP, &contextPointers->Rbp);
    GetContextPointer(cursor, unwContext, UNW_X86_64_RBX, &contextPointers->Rbx);
    GetContextPointer(cursor, unwContext, UNW_X86_64_R12, &contextPointers->R12);
    GetContextPointer(cursor, unwContext, UNW_X86_64_R13, &contextPointers->R13);
    GetContextPointer(cursor, unwContext, UNW_X86_64_R14, &contextPointers->R14);
    GetContextPointer(cursor, unwContext, UNW_X86_64_R15, &contextPointers->R15);
#elif defined(TARGET_X86)
    GetContextPointer(cursor, unwContext, UNW_X86_EBX, &contextPointers->Ebx);
    GetContextPointer(cursor, unwContext, UNW_X86_EBP, &contextPointers->Ebp);
    GetContextPointer(cursor, unwContext, UNW_X86_ESI, &contextPointers->Esi);
    GetContextPointer(cursor, unwContext, UNW_X86_EDI, &contextPointers->Edi);
#elif defined(TARGET_ARM)
    GetContextPointer(cursor, unwContext, UNW_ARM_R4, &contextPointers->R4);
    GetContextPointer(cursor, unwContext, UNW_ARM_R5, &contextPointers->R5);
    GetContextPointer(cursor, unwContext, UNW_ARM_R6, &contextPointers->R6);
    GetContextPointer(cursor, unwContext, UNW_ARM_R7, &contextPointers->R7);
    GetContextPointer(cursor, unwContext, UNW_ARM_R8, &contextPointers->R8);
    GetContextPointer(cursor, unwContext, UNW_ARM_R9, &contextPointers->R9);
    GetContextPointer(cursor, unwContext, UNW_ARM_R10, &contextPointers->R10);
    GetContextPointer(cursor, unwContext, UNW_ARM_R11, &contextPointers->R11);
#elif defined(TARGET_ARM64)
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
    GetContextPointer(cursor, unwContext, UNW_AARCH64_X29, &contextPointers->Fp);
#elif defined(TARGET_LOONGARCH64)
    GetContextPointer(cursor, unwContext, UNW_LOONGARCH64_R1, &contextPointers->Ra);
    GetContextPointer(cursor, unwContext, UNW_LOONGARCH64_R2, &contextPointers->Tp);
    GetContextPointer(cursor, unwContext, UNW_LOONGARCH64_R22, &contextPointers->Fp);
    GetContextPointer(cursor, unwContext, UNW_LOONGARCH64_R23, &contextPointers->S0);
    GetContextPointer(cursor, unwContext, UNW_LOONGARCH64_R24, &contextPointers->S1);
    GetContextPointer(cursor, unwContext, UNW_LOONGARCH64_R25, &contextPointers->S2);
    GetContextPointer(cursor, unwContext, UNW_LOONGARCH64_R26, &contextPointers->S3);
    GetContextPointer(cursor, unwContext, UNW_LOONGARCH64_R27, &contextPointers->S4);
    GetContextPointer(cursor, unwContext, UNW_LOONGARCH64_R28, &contextPointers->S5);
    GetContextPointer(cursor, unwContext, UNW_LOONGARCH64_R29, &contextPointers->S6);
    GetContextPointer(cursor, unwContext, UNW_LOONGARCH64_R30, &contextPointers->S7);
    GetContextPointer(cursor, unwContext, UNW_LOONGARCH64_R31, &contextPointers->S8);
#elif defined(TARGET_S390X)
    GetContextPointer(cursor, unwContext, UNW_S390X_R6, &contextPointers->R6);
    GetContextPointer(cursor, unwContext, UNW_S390X_R7, &contextPointers->R7);
    GetContextPointer(cursor, unwContext, UNW_S390X_R8, &contextPointers->R8);
    GetContextPointer(cursor, unwContext, UNW_S390X_R9, &contextPointers->R9);
    GetContextPointer(cursor, unwContext, UNW_S390X_R10, &contextPointers->R10);
    GetContextPointer(cursor, unwContext, UNW_S390X_R11, &contextPointers->R11);
    GetContextPointer(cursor, unwContext, UNW_S390X_R12, &contextPointers->R12);
    GetContextPointer(cursor, unwContext, UNW_S390X_R13, &contextPointers->R13);
    GetContextPointer(cursor, unwContext, UNW_S390X_R14, &contextPointers->R14);
    GetContextPointer(cursor, unwContext, UNW_S390X_R15, &contextPointers->R15);
#else
#error unsupported architecture
#endif
}

static void UnwindContextToContext(unw_cursor_t *cursor, CONTEXT *winContext)
{
#if defined(TARGET_AMD64)
    unw_get_reg(cursor, UNW_REG_IP, (unw_word_t *) &winContext->Rip);
    unw_get_reg(cursor, UNW_REG_SP, (unw_word_t *) &winContext->Rsp);
    unw_get_reg(cursor, UNW_X86_64_RBP, (unw_word_t *) &winContext->Rbp);
    unw_get_reg(cursor, UNW_X86_64_RBX, (unw_word_t *) &winContext->Rbx);
    unw_get_reg(cursor, UNW_X86_64_R12, (unw_word_t *) &winContext->R12);
    unw_get_reg(cursor, UNW_X86_64_R13, (unw_word_t *) &winContext->R13);
    unw_get_reg(cursor, UNW_X86_64_R14, (unw_word_t *) &winContext->R14);
    unw_get_reg(cursor, UNW_X86_64_R15, (unw_word_t *) &winContext->R15);
#elif defined(TARGET_X86)
    unw_get_reg(cursor, UNW_REG_IP, (unw_word_t *) &winContext->Eip);
    unw_get_reg(cursor, UNW_REG_SP, (unw_word_t *) &winContext->Esp);
    unw_get_reg(cursor, UNW_X86_EBP, (unw_word_t *) &winContext->Ebp);
    unw_get_reg(cursor, UNW_X86_EBX, (unw_word_t *) &winContext->Ebx);
    unw_get_reg(cursor, UNW_X86_ESI, (unw_word_t *) &winContext->Esi);
    unw_get_reg(cursor, UNW_X86_EDI, (unw_word_t *) &winContext->Edi);
#elif defined(TARGET_ARM)
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
#elif defined(TARGET_ARM64)
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
#ifdef __APPLE__
    // Strip pointer authentication bits which seem to be leaking out of libunwind
    // Seems like ptrauth_strip() / __builtin_ptrauth_strip() should work, but currently
    // errors with "this target does not support pointer authentication"
    winContext->Pc = winContext->Pc & MACOS_ARM64_POINTER_AUTH_MASK;
#endif // __APPLE__
    TRACE("sp %p pc %p lr %p fp %p\n", winContext->Sp, winContext->Pc, winContext->Lr, winContext->Fp);
#elif defined(TARGET_LOONGARCH64)
    unw_get_reg(cursor, UNW_REG_IP, (unw_word_t *) &winContext->Pc);
    unw_get_reg(cursor, UNW_REG_SP, (unw_word_t *) &winContext->Sp);
    unw_get_reg(cursor, UNW_LOONGARCH64_R1, (unw_word_t *) &winContext->Ra);
    unw_get_reg(cursor, UNW_LOONGARCH64_R2, (unw_word_t *) &winContext->Tp);
    unw_get_reg(cursor, UNW_LOONGARCH64_R22, (unw_word_t *) &winContext->Fp);
    unw_get_reg(cursor, UNW_LOONGARCH64_R23, (unw_word_t *) &winContext->S0);
    unw_get_reg(cursor, UNW_LOONGARCH64_R24, (unw_word_t *) &winContext->S1);
    unw_get_reg(cursor, UNW_LOONGARCH64_R25, (unw_word_t *) &winContext->S2);
    unw_get_reg(cursor, UNW_LOONGARCH64_R26, (unw_word_t *) &winContext->S3);
    unw_get_reg(cursor, UNW_LOONGARCH64_R27, (unw_word_t *) &winContext->S4);
    unw_get_reg(cursor, UNW_LOONGARCH64_R28, (unw_word_t *) &winContext->S5);
    unw_get_reg(cursor, UNW_LOONGARCH64_R29, (unw_word_t *) &winContext->S6);
    unw_get_reg(cursor, UNW_LOONGARCH64_R30, (unw_word_t *) &winContext->S7);
    unw_get_reg(cursor, UNW_LOONGARCH64_R31, (unw_word_t *) &winContext->S8);
    TRACE("sp %p pc %p fp %p tp %p ra %p\n", winContext->Sp, winContext->Pc, winContext->Fp, winContext->Tp, winContext->Ra);
#elif defined(TARGET_S390X)
    unw_get_reg(cursor, UNW_REG_IP, (unw_word_t *) &winContext->PSWAddr);
    unw_get_reg(cursor, UNW_REG_SP, (unw_word_t *) &winContext->R15);
    unw_get_reg(cursor, UNW_S390X_R6, (unw_word_t *) &winContext->R6);
    unw_get_reg(cursor, UNW_S390X_R7, (unw_word_t *) &winContext->R7);
    unw_get_reg(cursor, UNW_S390X_R8, (unw_word_t *) &winContext->R8);
    unw_get_reg(cursor, UNW_S390X_R9, (unw_word_t *) &winContext->R9);
    unw_get_reg(cursor, UNW_S390X_R10, (unw_word_t *) &winContext->R10);
    unw_get_reg(cursor, UNW_S390X_R11, (unw_word_t *) &winContext->R11);
    unw_get_reg(cursor, UNW_S390X_R12, (unw_word_t *) &winContext->R12);
    unw_get_reg(cursor, UNW_S390X_R13, (unw_word_t *) &winContext->R13);
    unw_get_reg(cursor, UNW_S390X_R14, (unw_word_t *) &winContext->R14);
    TRACE("sp %p pc %p lr %p\n", winContext->R15, winContext->PSWAddr, winContext->R14);
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
#if defined(TARGET_AMD64)
    case UNW_REG_IP:       *valp = (unw_word_t)winContext->Rip; break;
    case UNW_REG_SP:       *valp = (unw_word_t)winContext->Rsp; break;
    case UNW_X86_64_RBP:   *valp = (unw_word_t)winContext->Rbp; break;
    case UNW_X86_64_RBX:   *valp = (unw_word_t)winContext->Rbx; break;
    case UNW_X86_64_R12:   *valp = (unw_word_t)winContext->R12; break;
    case UNW_X86_64_R13:   *valp = (unw_word_t)winContext->R13; break;
    case UNW_X86_64_R14:   *valp = (unw_word_t)winContext->R14; break;
    case UNW_X86_64_R15:   *valp = (unw_word_t)winContext->R15; break;
#elif defined(TARGET_X86)
    case UNW_REG_IP:       *valp = (unw_word_t)winContext->Eip; break;
    case UNW_REG_SP:       *valp = (unw_word_t)winContext->Esp; break;
    case UNW_X86_EBX:      *valp = (unw_word_t)winContext->Ebx; break;
    case UNW_X86_ESI:      *valp = (unw_word_t)winContext->Esi; break;
    case UNW_X86_EDI:      *valp = (unw_word_t)winContext->Edi; break;
    case UNW_X86_EBP:      *valp = (unw_word_t)winContext->Ebp; break;
#elif defined(TARGET_ARM)
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
#elif defined(TARGET_ARM64)
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
#elif defined(TARGET_LOONGARCH64)
    case UNW_LOONGARCH64_R1:    *valp = (unw_word_t)winContext->Ra; break;
    case UNW_LOONGARCH64_R2:    *valp = (unw_word_t)winContext->Tp; break;
    case UNW_LOONGARCH64_R22:   *valp = (unw_word_t)winContext->Fp; break;
    case UNW_LOONGARCH64_R23:   *valp = (unw_word_t)winContext->S0; break;
    case UNW_LOONGARCH64_R24:   *valp = (unw_word_t)winContext->S1; break;
    case UNW_LOONGARCH64_R25:   *valp = (unw_word_t)winContext->S2; break;
    case UNW_LOONGARCH64_R26:   *valp = (unw_word_t)winContext->S3; break;
    case UNW_LOONGARCH64_R27:   *valp = (unw_word_t)winContext->S4; break;
    case UNW_LOONGARCH64_R28:   *valp = (unw_word_t)winContext->S5; break;
    case UNW_LOONGARCH64_R29:   *valp = (unw_word_t)winContext->S6; break;
    case UNW_LOONGARCH64_R30:   *valp = (unw_word_t)winContext->S7; break;
    case UNW_LOONGARCH64_R31:   *valp = (unw_word_t)winContext->S8; break;
    case UNW_LOONGARCH64_PC:    *valp = (unw_word_t)winContext->Pc; break;
#elif defined(TARGET_S390X)
    case UNW_S390X_R6:     *valp = (unw_word_t)winContext->R6; break;
    case UNW_S390X_R7:     *valp = (unw_word_t)winContext->R7; break;
    case UNW_S390X_R8:     *valp = (unw_word_t)winContext->R8; break;
    case UNW_S390X_R9:     *valp = (unw_word_t)winContext->R9; break;
    case UNW_S390X_R10:    *valp = (unw_word_t)winContext->R10; break;
    case UNW_S390X_R11:    *valp = (unw_word_t)winContext->R11; break;
    case UNW_S390X_R12:    *valp = (unw_word_t)winContext->R12; break;
    case UNW_S390X_R13:    *valp = (unw_word_t)winContext->R13; break;
    case UNW_S390X_R14:    *valp = (unw_word_t)winContext->R14; break;
    case UNW_S390X_R15:    *valp = (unw_word_t)winContext->R15; break;
    case UNW_S390X_IP:     *valp = (unw_word_t)winContext->PSWAddr; break;
#else
#error unsupported architecture
#endif
    default:
        ASSERT("Attempt to read an unknown register %d\n", regnum);
        return -UNW_EBADREG;
    }
    TRACE("REG: %d %p\n", regnum, (void*)*valp);
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
    auto *info = (libunwindInfo*)arg;
#ifdef __APPLE__
    bool step;
    if (!GetProcInfo(ip, pip, info, &step, need_unwind_info)) {
        return -UNW_EINVAL;
    }
    _ASSERTE(!step);
    return UNW_ESUCCESS;
#else
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

#if defined(TARGET_ARM)
#ifndef PT_ARM_EXIDX
#define PT_ARM_EXIDX   0x70000001      /* See llvm ELF.h */
#endif /* !PT_ARM_EXIDX */
        case PT_ARM_EXIDX:
            exidxFrameHdrAddr = loadbias + ph.p_vaddr;
            exidxFrameHdrLen = ph.p_memsz;
            break;
#endif
        }
    }

    if (dynamicAddr != nullptr)
    {
        while (true)
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

#ifdef FEATURE_USE_SYSTEM_LIBUNWIND
    if (ehFrameHdrAddr  == 0) {
        ASSERT("ELF: No PT_GNU_EH_FRAME program header\n");
        return -UNW_EINVAL;
    }
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

    // If there are no frame table entries
    if (fdeCount == 0) {
        TRACE("No frame table entries\n");
        return -UNW_ENOINFO;
    }

    // We assume this encoding
    if (ehFrameHdr.table_enc != (DW_EH_PE_datarel | DW_EH_PE_sdata4)) {
        ASSERT("Table encoding not supported %x\n", ehFrameHdr.table_enc);
        return -UNW_EINVAL;
    }

    // Find the fde using a binary search on the frame table
    table_entry_t entry;
    table_entry_t entryNext;
    bool found;
    if (!BinarySearchEntries(info, ip - ehFrameHdrAddr, addr, fdeCount, &entry, &entryNext, false, &found)) {
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

    if (ip < pip->start_ip || ip >= pip->end_ip) {
        TRACE("ip %p not in range start_ip %p end_ip %p\n", ip, pip->start_ip, pip->end_ip);
        return -UNW_ENOINFO;
    }
    info->FunctionStart = pip->start_ip;
    return UNW_ESUCCESS;
#else
    return _OOP_find_proc_info(start_ip, end_ip, ehFrameHdrAddr, ehFrameHdrLen, exidxFrameHdrAddr, exidxFrameHdrLen, as, ip, pip, need_unwind_info, arg);
#endif // FEATURE_USE_SYSTEM_LIBUNWIND

#endif // __APPLE__
}

static void
put_unwind_info(unw_addr_space_t as, unw_proc_info_t *pip, void *arg)
{
#ifdef FEATURE_USE_SYSTEM_LIBUNWIND
    if (pip->unwind_info != nullptr) {
        free(pip->unwind_info);
        pip->unwind_info = nullptr;
    }
#endif // FEATURE_USE_SYSTEM_LIBUNWIND
}

static unw_accessors_t init_unwind_accessors()
{
    unw_accessors_t a = {0};

    a.find_proc_info = find_proc_info;
    a.put_unwind_info = put_unwind_info;
    a.get_dyn_info_list_addr = get_dyn_info_list_addr;
    a.access_mem = access_mem;
    a.access_reg = access_reg;
    a.access_fpreg = access_fpreg;
    a.resume = resume;
    a.get_proc_name = get_proc_name;

    return a;
};

static unw_accessors_t unwind_accessors = init_unwind_accessors();

/*++
Function:
    PAL_VirtualUnwindOutOfProc

    Unwind the stack given the context for a "remote" target using the
    provided read memory callback.

    Assumes the IP is in the module of the base address provided (coreclr).

Parameters:
    context - the start context in the target
    contextPointers - the context of the next frame
    functionStart - the pointer to return the starting address of the function or nullptr
    baseAddress - base address of the module to find the unwind info
    readMemoryCallback - reads memory from the target
--*/
BOOL
PALAPI
PAL_VirtualUnwindOutOfProc(CONTEXT *context, KNONVOLATILE_CONTEXT_POINTERS *contextPointers, PULONG64 functionStart, SIZE_T baseAddress, UnwindReadMemoryCallback readMemoryCallback)
{
    unw_addr_space_t addrSpace = 0;
    unw_cursor_t cursor;
    libunwindInfo info;
    BOOL result = FALSE;
    int st;

    info.BaseAddress = baseAddress;
    info.Context = context;
    info.FunctionStart = 0;
    info.ReadMemory = readMemoryCallback;

#ifdef __APPLE__
    unw_proc_info_t procInfo;
    bool step;
#if defined(TARGET_AMD64)
    TRACE("Unwind: rip %p rsp %p rbp %p\n", (void*)context->Rip, (void*)context->Rsp, (void*)context->Rbp);
    result = GetProcInfo(context->Rip, &procInfo, &info, &step, false);
#elif defined(TARGET_ARM64)
    TRACE("Unwind: pc %p sp %p fp %p\n", (void*)context->Pc, (void*)context->Sp, (void*)context->Fp);
    result = GetProcInfo(context->Pc, &procInfo, &info, &step, false);
    if (result && step)
    {
        // If the PC is at the start of the function, the previous instruction is BL and the unwind encoding is frameless
        // with nothing on stack (0x02000000), back up PC by 1 to the previous function and get the unwind info for that
        // function.
        if ((context->Pc == procInfo.start_ip) &&
            (procInfo.format & (UNWIND_ARM64_MODE_MASK | UNWIND_ARM64_FRAMELESS_STACK_SIZE_MASK)) == UNWIND_ARM64_MODE_FRAMELESS)
        {
            uint32_t opcode;
            unw_word_t addr = context->Pc - sizeof(opcode);
            if (ReadValue32(&info, &addr, &opcode))
            {
                // Is the previous instruction a BL opcode?
                if ((opcode & ARM64_BL_OPCODE_MASK) == ARM64_BL_OPCODE ||
                    (opcode & ARM64_BLR_OPCODE_MASK) == ARM64_BLR_OPCODE ||
                    (opcode & ARM64_BLRA_OPCODE_MASK) == ARM64_BLRA_OPCODE)
                {
                    TRACE("Unwind: getting unwind info for PC - 1 opcode %08x\n", opcode);
                    result = GetProcInfo(context->Pc - 1, &procInfo, &info, &step, false);
                }
                else
                {
                    TRACE("Unwind: not BL* opcode %08x\n", opcode);
                }
            }
        }
    }
#else
#error Unexpected architecture
#endif
    if (!result)
    {
        goto exit;
    }
    if (step)
    {
        result = StepWithCompactEncoding(&info, procInfo.format, procInfo.start_ip);
        goto exit;
    }
#endif

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
    if (functionStart)
    {
        *functionStart = info.FunctionStart;
    }
    if (addrSpace != 0)
    {
        unw_destroy_addr_space(addrSpace);
    }
    return result;
}

#else

BOOL
PALAPI
PAL_VirtualUnwindOutOfProc(CONTEXT *context, KNONVOLATILE_CONTEXT_POINTERS *contextPointers, PULONG64 functionStart, SIZE_T baseAddress, UnwindReadMemoryCallback readMemoryCallback)
{
    return FALSE;
}

#endif // defined(__APPLE__) || defined(HAVE_UNW_GET_ACCESSORS)
