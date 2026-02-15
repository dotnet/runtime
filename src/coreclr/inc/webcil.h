// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef WEBCIL_H_
#define WEBCIL_H_

#include <stdint.h>

#define WEBCIL_MAGIC_W 'W'
#define WEBCIL_MAGIC_B 'b'
#define WEBCIL_MAGIC_I 'I'
#define WEBCIL_MAGIC_L 'L'
#define WEBCIL_VERSION_MAJOR 0
#define WEBCIL_VERSION_MINOR 1

#pragma pack(push, 1)

struct WebcilHeader {
    uint8_t  id[4];              // 'W','b','I','L'
    uint16_t version_major;      // 0
    uint16_t version_minor;      // 1
    uint16_t coff_sections;
    uint16_t reserved0;
    uint32_t pe_cli_header_rva;
    uint32_t pe_cli_header_size;
    uint32_t pe_debug_rva;
    uint32_t pe_debug_size;
};  // 28 bytes

struct WebcilSectionHeader {
    uint32_t virtual_size;
    uint32_t virtual_address;
    uint32_t raw_data_size;
    uint32_t raw_data_ptr;
};  // 16 bytes

#pragma pack(pop)

static_assert(sizeof(WebcilHeader) == 28, "WebcilHeader must be 28 bytes");
static_assert(sizeof(WebcilSectionHeader) == 16, "WebcilSectionHeader must be 16 bytes");

#endif // WEBCIL_H_
