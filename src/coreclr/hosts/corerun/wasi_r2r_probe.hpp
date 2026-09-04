// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Statically-composed WASI R2R external-assembly probe, shared by every CoreCLR-WASI host
// (the standalone corerun executable and the per-app-linked wasihost corehost / libWasiHost.a). The
// probe is a host_runtime_contract::external_assembly_probe callback: the runtime calls out to it to
// obtain the composite R2R webcil image and the per-assembly stubs. Keeping it here (rather than in a
// single host) means both hosts serve R2R identically instead of one silently falling back to interp.
//
// The splice that populates it is hand-driven (eng/wasi-r2r/pipeline_shim.py); there is no SDK path
// for WASI R2R yet, so this serves the runtime tests and the development loop rather than shipping
// apps. Both hosts must be linked with the flags that supply a composite's imports -- see
// CORERUN_WASI_COMPOSITE_R2R in corerun/CMakeLists.txt and WasiEnableCompositeR2R in
// WasiApp.CoreCLR.targets. Without them this probe compiles but can never be satisfied.
//
// Requires corerun.hpp to be included first (for pal::try_map_file_readonly). Include exactly once per
// host translation unit; the internal-linkage buffer/functions then give one instance per host binary.

#ifndef WASI_R2R_PROBE_HPP
#define WASI_R2R_PROBE_HPP

#ifdef TARGET_WASI

#include <cstdint>
#include <cstdlib>
#include <cstring>
#include <cstdio>
#include <sys/mman.h>

namespace wasi_r2r
{
// A crossgen2-produced R2R webcil image is merged into the host module post-link (its native
// functions land in the shared indirect function table and its webcil payload/metadata is written
// into g_wasi_r2r_image by the offline merge's active data segment at this buffer's address == the
// composite image's imageBase). The runtime then finds the R2R webcil via this probe, exactly the way
// the browser host does via BrowserHost_ExternalAssemblyProbe.
//
// Standalone corerun uses the fixed development buffer below. The per-app host declares the symbols
// external instead: its publish builds a strong buffer definition sized exactly to the composite,
// while the host archive carries a 64-byte weak fallback for non-R2R apps.
#ifndef WASI_R2R_IMAGE_CAP
#define WASI_R2R_IMAGE_CAP (16u * 1024u * 1024u)
#endif

#ifdef WASI_R2R_EXTERNAL_IMAGE_BUFFER
extern "C" uint8_t g_wasi_r2r_image[];
extern "C" uint32_t g_wasi_r2r_image_cap;
#else
alignas(16) static uint8_t g_wasi_r2r_image[WASI_R2R_IMAGE_CAP];
static constexpr uint32_t g_wasi_r2r_image_cap = WASI_R2R_IMAGE_CAP;
#endif

// The table index at which the composite's functions are installed. Under the reservation model the
// host is linked with `-Wl,--table-base=<N+1>`, which moves corerun's own address-taken functions up
// to start at N+1 and leaves slots 1..N free, so the composite always sits at base 1 regardless of
// its size. This MUST match the `__table_base` global supplied to the merge (see eng/wasi-r2r/README.md);
// the two are a coupled constant and a mismatch is silent -- see the patch in WasiStaticR2RProbe.
#ifndef WASI_R2R_TABLE_BASE
#define WASI_R2R_TABLE_BASE (1u)
#endif
// Header version 1 adds TableBase to the 28-byte version 0 header.
#define WEBCIL_HEADER_V0_SIZE       (28u)
#define WEBCIL_HEADER_V1_SIZE       (32u)
#define WEBCIL_SECTION_HEADER_SIZE  (16u)
#define WEBCIL_VERSION_MAJOR_OFFSET (4u)
#define WEBCIL_TABLE_BASE_OFFSET    (28u)

// The composite native image's bundle-relative file name (the ownerCompositeExecutable named by each
// per-assembly stub). The runtime asks for this via NativeImage::Open -> external_assembly_probe.
#ifndef WASI_R2R_COMPOSITE_NAME
#define WASI_R2R_COMPOSITE_NAME "composite-r2r.wasm"
#endif

static size_t WasiWebcilHeaderSize(const uint8_t* p, size_t len)
{
    if (len < WEBCIL_HEADER_V0_SIZE)
        return 0;

    uint16_t versionMajor;
    memcpy(&versionMajor, p + WEBCIL_VERSION_MAJOR_OFFSET, sizeof(versionMajor));
    return versionMajor >= 1 ? WEBCIL_HEADER_V1_SIZE : WEBCIL_HEADER_V0_SIZE;
}

// Compute the exact WbIL payload size from its self-describing header - no baked constant needed.
// WebcilHeader_1 (32 bytes): Id[4] 'WbIL', VersionMajor u16, VersionMinor u16, CoffSections u16,
// Reserved0 u16, PeCliHeaderRva u32, PeCliHeaderSize u32, PeDebugRva u32, PeDebugSize u32, TableBase u32.
// Followed by CoffSections * WebcilSectionHeader{VirtualSize, VirtualAddress, SizeOfRawData, PointerToRawData}.
// The payload extent is the maximum (PointerToRawData + SizeOfRawData) across all sections.
//
// Every field here comes from an image this host did not produce, so bounds and overflow are checked
// rather than assumed: a wrapped sum would yield a SMALL extent that passes the cap check below and
// hands the runtime a truncated image.
static int64_t WasiWebcilPayloadSize(const uint8_t* p, size_t len)
{
    size_t headerSize = WasiWebcilHeaderSize(p, len);
    if (headerSize == 0 || headerSize > len)
        return 0;

    if (p[0] != 'W' || p[1] != 'b' || p[2] != 'I' || p[3] != 'L')
        return 0;

    uint16_t coffSections;
    memcpy(&coffSections, p + 8, sizeof(coffSections));

    // Section headers must fit entirely within the buffer.
    if ((len - headerSize) / WEBCIL_SECTION_HEADER_SIZE < coffSections)
        return 0;

    const uint8_t* sec = p + headerSize;
    uint32_t maxEnd = 0;
    for (uint16_t i = 0; i < coffSections; i++)
    {
        uint32_t sizeOfRawData;
        uint32_t pointerToRawData;
        memcpy(&sizeOfRawData, sec + 8, sizeof(sizeOfRawData));
        memcpy(&pointerToRawData, sec + 12, sizeof(pointerToRawData));

        // Reject rather than wrap: UINT32_MAX - a < b  <=>  a + b would overflow.
        if (UINT32_MAX - pointerToRawData < sizeOfRawData)
            return 0;

        uint32_t end = pointerToRawData + sizeOfRawData;
        if (end > maxEnd)
            maxEnd = end;
        sec += WEBCIL_SECTION_HEADER_SIZE;
    }
    return (int64_t)maxEnd;
}

// Minimal LEB128 reader for parsing a wasm binary's Data section. Returns false on a truncated or
// over-long encoding rather than shifting past the width of the result (which would be UB).
static bool wasi_read_uleb(const uint8_t* p, size_t len, size_t* pos, uint64_t* value)
{
    uint64_t result = 0;
    int shift = 0;
    while (*pos < len)
    {
        uint8_t b = p[(*pos)++];
        if (shift >= 64)
            return false; // over-long encoding
        result |= (uint64_t)(b & 0x7f) << shift;
        if ((b & 0x80) == 0)
        {
            *value = result;
            return true;
        }
        shift += 7;
    }
    return false; // ran off the end without a terminating byte
}

// Extract the raw WbIL webcil payload (passive data segment index 1) from a wasm-wrapped-webcil stub
// on disk. The stub's tableBase field (WEBCIL_TABLE_BASE_OFFSET) is authoritative: the offline merge
// step patches it to the composite's merge-time table base, so this host trusts the on-disk value
// rather than injecting a baked constant.
// Mirrors what the browser JS loader's getWebcilPayload does, but purely in native code (no instantiation).
//
// On success the file mapping is deliberately RETAINED and *data_start points into it: the runtime
// takes ownership of neither (ProbeExtensionResult::External never frees), so copying to a malloc'd
// buffer would leak the copy on top of the mapping. Every failure path unmaps.
//
// The stub is untrusted input, so each length read is validated against the remaining extent before
// it is used to advance or copy.
static bool WasiExtractStubPayload(const char* wasmPath, void** data_start, int64_t* size)
{
    void* filedata = nullptr; int64_t filesize = 0;
    if (!pal::try_map_file_readonly(wasmPath, &filedata, &filesize))
        return false;

    const uint8_t* p = (const uint8_t*)filedata;
    size_t len = (size_t)filesize;
    bool ok = false;
    if (len >= 8 && p[0] == 0x00 && p[1] == 0x61 && p[2] == 0x73 && p[3] == 0x6d)
    {
        size_t pos = 8;
        while (pos < len)
        {
            uint8_t secId = p[pos++];
            uint64_t secSize;
            if (!wasi_read_uleb(p, len, &pos, &secSize))
                break;
            // len - pos cannot underflow (pos <= len) and avoids overflowing pos + secSize, which
            // wraps on wasm32 where size_t is 32-bit.
            if (secSize > (uint64_t)(len - pos))
                break;
            size_t secEnd = pos + (size_t)secSize;
            if (secId == 11) // Data section
            {
                size_t q = pos;
                uint64_t segCount;
                if (!wasi_read_uleb(p, secEnd, &q, &segCount))
                    break;
                for (uint64_t s = 0; s < segCount && q < secEnd; s++)
                {
                    uint64_t mode;
                    if (!wasi_read_uleb(p, secEnd, &q, &mode))
                        break;
                    // Only passive segments (mode 1) are used by the webcil wrapper. A composite's
                    // payload segment is ACTIVE, so this also declines a composite handed here by
                    // mistake rather than misreading its offset expression as segment data.
                    if (mode != 1) { break; }
                    uint64_t dlen;
                    if (!wasi_read_uleb(p, secEnd, &q, &dlen))
                        break;
                    if (dlen > (uint64_t)(secEnd - q))
                        break; // segment claims more bytes than the section holds
                    size_t dstart = q;
                    q += (size_t)dlen;
                    if (s == 1) // segment[1] == the WbIL payload
                    {
                        // Validate rather than assume: if the wrapper's segment layout ever changes,
                        // fail loudly here instead of handing the runtime a non-webcil buffer.
                        if (dlen >= 4 && memcmp(p + dstart, "WbIL", 4) == 0)
                        {
                            *data_start = (void*)(p + dstart);
                            *size = (int64_t)dlen;
                            ok = true;
                        }
                        break;
                    }
                }
                break;
            }
            pos = secEnd;
        }
    }
    if (!ok)
        munmap(filedata, (size_t)filesize);
    return ok;
}

// The external-assembly R2R probe: serves the composite webcil from the baked buffer and each managed
// assembly's per-assembly stub from "<dir>/comp/<base>.wasm" on disk, searching the supplied dirs (each
// expected to carry a trailing path delimiter). Returns false for anything it does not provide, letting
// the caller fall back to its normal assembly load.
static bool WasiStaticR2RProbe(const char* name, const char* const* dirs, size_t ndirs, void** data_start, int64_t* size)
{
    // The composite native image itself: return the merged composite payload at imageBase. Its size is
    // read from the self-describing WbIL header (no baked constant), and validated against the buffer cap.
    if (strcmp(name, WASI_R2R_COMPOSITE_NAME) == 0)
    {
        int64_t payloadSize = WasiWebcilPayloadSize(&g_wasi_r2r_image[0], g_wasi_r2r_image_cap);
        if (payloadSize <= 0 || (size_t)payloadSize > g_wasi_r2r_image_cap)
            return false; // buffer not populated, or composite payload exceeds the cap

        // A current self-installing image patches its own TableBase from the composition shim's start
        // function. Older images predate patchWebcilHeader, so retain the native fallback when the
        // field is still zero. WebcilDecoder treats an unwritten zero as a valid base and would
        // otherwise shift every R2R function index to the wrong table slot.
        //
        // NOTE: the cap test above cannot protect this buffer -- the engine installs the segment before any
        // host code runs, so an over-cap payload has already overwritten whatever follows by the time we look.
        // The enforceable check is at build time; pipeline_shim.py compares the payload size against the cap.
        uint8_t* hdr = &g_wasi_r2r_image[0];
        if (WasiWebcilHeaderSize(hdr, (size_t)payloadSize) >= WEBCIL_HEADER_V1_SIZE)
        {
            uint32_t existingTableBase;
            memcpy(&existingTableBase, hdr + WEBCIL_TABLE_BASE_OFFSET, sizeof(existingTableBase));
            if (existingTableBase == 0)
            {
                uint32_t tableBase = WASI_R2R_TABLE_BASE;
                memcpy(hdr + WEBCIL_TABLE_BASE_OFFSET, &tableBase, sizeof(tableBase));
            }
        }

        *data_start = &g_wasi_r2r_image[0];
        *size = payloadSize;
        return true;
    }

    // A managed assembly: return its per-assembly stub payload (extracted from <base>.wasm on disk).
    // The stub carries the assembly metadata + the R2R header naming the composite, which drives the
    // runtime to then request WASI_R2R_COMPOSITE_NAME above.
    size_t nlen = strlen(name);
    if (nlen > 4 && strcmp(name + nlen - 4, ".dll") == 0)
    {
        char stub[512];
        for (size_t i = 0; i < ndirs; i++)
        {
            const char* dir = dirs[i];
            if (dir == nullptr) continue;
            // Build "<dir>/comp/<base>.wasm"
            snprintf(stub, sizeof(stub), "%scomp/%.*s.wasm", dir, (int)(nlen - 4), name);
            if (WasiExtractStubPayload(stub, data_start, size))
            {
                return true;
            }
        }
    }
    return false;
}

} // namespace wasi_r2r

// Exported so the offline merge step can discover the buffer's address and wire it to the R2R image's
// __memory_base global. Defined outside the namespace with C linkage so the export name is exactly
// "wasi_r2r_image_base" (the merge step targets this symbol).
//
// This header is included once in each host binary. Keeping the exported accessor here ensures the
// linker roots the selected fixed or per-app buffer and gives the composition step a stable anchor.
extern "C" __attribute__((export_name("wasi_r2r_image_base"))) uint32_t wasi_r2r_image_base(void)
{
    return (uint32_t)(uintptr_t)&wasi_r2r::g_wasi_r2r_image[0];
}

// The staging buffer's capacity and the table slot the composite installs at, exported for the same
// reason as the base: the splice must not carry its own copy of either. The host owns these values;
// eng/wasi-r2r/pipeline_shim.py reads them out of the linked binary and validates the composite
// against them, so a mismatch is a build-time error instead of a wrong-function dispatch at runtime.
#ifdef WASI_R2R_EXTERNAL_IMAGE_BUFFER
#define WASI_R2R_IMAGE_CAP_WEAK __attribute__((weak))
#else
#define WASI_R2R_IMAGE_CAP_WEAK
#endif
extern "C" WASI_R2R_IMAGE_CAP_WEAK __attribute__((export_name("wasi_r2r_image_cap"))) uint32_t wasi_r2r_image_cap(void)
{
    return wasi_r2r::g_wasi_r2r_image_cap;
}
#undef WASI_R2R_IMAGE_CAP_WEAK

extern "C" __attribute__((export_name("wasi_r2r_table_base"))) uint32_t wasi_r2r_table_base(void)
{
    return (uint32_t)WASI_R2R_TABLE_BASE;
}

#endif // TARGET_WASI

#endif // WASI_R2R_PROBE_HPP
