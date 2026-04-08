// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Stub link.h for WASI — provides minimal types for PAL compilation.

#ifndef _WASI_LINK_H
#define _WASI_LINK_H

#include <stdint.h>

typedef uint32_t Elf32_Addr;
typedef uint32_t Elf32_Word;
typedef uint32_t Elf32_Off;
typedef uint16_t Elf32_Half;

typedef struct {
    Elf32_Word p_type;
    Elf32_Off  p_offset;
    Elf32_Addr p_vaddr;
    Elf32_Addr p_paddr;
    Elf32_Word p_filesz;
    Elf32_Word p_memsz;
    Elf32_Word p_flags;
    Elf32_Word p_align;
} Elf32_Phdr;

struct dl_phdr_info {
    Elf32_Addr dlpi_addr;
    const char *dlpi_name;
    const Elf32_Phdr *dlpi_phdr;
    Elf32_Half dlpi_phnum;
};

static inline int dl_iterate_phdr(int (*callback)(struct dl_phdr_info *, size_t, void *), void *data) { (void)callback; (void)data; return 0; }

#endif // _WASI_LINK_H
