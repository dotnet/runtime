// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef WEBCIL_H_
#define WEBCIL_H_

#include <stdint.h>

#define WEBCIL_MAGIC_W 'W'
#define WEBCIL_MAGIC_B 'b'
#define WEBCIL_MAGIC_I 'I'
#define WEBCIL_MAGIC_L 'L'
#define WEBCIL_VERSION_MAJOR 1
#define WEBCIL_VERSION_MINOR 0

#pragma pack(push, 1)

struct WebCILHeader {
    uint8_t  id[4];
    uint16_t version_major;
    uint16_t version_minor;
    uint16_t coff_sections;
    uint16_t reserved0;
    uint32_t pe_cli_header_rva;
    uint32_t pe_cli_header_size;
    uint32_t pe_debug_rva;
    uint32_t pe_debug_size;
};  // 28 bytes

#pragma pack(pop)

static_assert(sizeof(WebCILHeader) == 28, "WebCILHeader must be 28 bytes");
// Section headers following WebCILHeader are standard IMAGE_SECTION_HEADER (40 bytes each).

#endif // WEBCIL_H_
