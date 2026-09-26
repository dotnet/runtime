// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Statically-composed WASI R2R external-assembly probe, shared by every CoreCLR-WASI host
// (the standalone corerun executable and the per-app-linked wasihost corehost / libWasiHost.a). The
// probe is a host_runtime_contract::external_assembly_probe callback: the runtime calls out to it to
// obtain the composite R2R webcil image and the per-assembly stubs. Keeping it here (rather than in a
// single host) means both hosts serve R2R identically instead of one silently falling back to interp.
//
// The in-tree WASI R2R composer populates the image for publishing and runtime tests.
// Both hosts must be linked with the flags that supply a composite's imports -- see
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
// its size. This MUST match the `__table_base` global supplied to the merge (see docs/design/mono/webcil.md);
// the two are a coupled constant and a mismatch is silent -- see the patch in WasiStaticR2RProbe.
#ifndef WASI_R2R_TABLE_BASE
#define WASI_R2R_TABLE_BASE (1u)
#endif
#define WEBCIL_HEADER_V1_SIZE       (32u)
#define WEBCIL_SECTION_HEADER_SIZE  (16u)
#define WEBCIL_VERSION_MAJOR_OFFSET (4u)

// The composite native image's bundle-relative file name (the ownerCompositeExecutable named by each
// per-assembly stub). The runtime asks for this via NativeImage::Open -> external_assembly_probe.
#ifndef WASI_R2R_COMPOSITE_NAME
#define WASI_R2R_COMPOSITE_NAME "composite-r2r.wasm"
#endif

static bool WasiIsWebcilV1(const uint8_t* p, size_t len)
{
    if (len < WEBCIL_HEADER_V1_SIZE)
        return false;

    uint16_t versionMajor;
    memcpy(&versionMajor, p + WEBCIL_VERSION_MAJOR_OFFSET, sizeof(versionMajor));
    return versionMajor == 1;
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
    if (!WasiIsWebcilV1(p, len))
        return 0;

    if (p[0] != 'W' || p[1] != 'b' || p[2] != 'I' || p[3] != 'L')
        return 0;

    uint16_t coffSections;
    memcpy(&coffSections, p + 8, sizeof(coffSections));

    // Section headers must fit entirely within the buffer.
    if ((len - WEBCIL_HEADER_V1_SIZE) / WEBCIL_SECTION_HEADER_SIZE < coffSections)
        return 0;

    const uint8_t* sec = p + WEBCIL_HEADER_V1_SIZE;
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

// Map a raw WebCIL component forwarding stub extracted by the build-time composer. On success the
// file mapping is deliberately retained and returned directly to the runtime.
static bool WasiMapStubPayload(const char* webcilPath, void** data_start, int64_t* size)
{
    void* filedata = nullptr; int64_t filesize = 0;
    if (!pal::try_map_file_readonly(webcilPath, &filedata, &filesize))
        return false;

    if (filesize <= 0)
        return false;

    int64_t payloadSize = WasiWebcilPayloadSize(static_cast<const uint8_t*>(filedata), static_cast<size_t>(filesize));
    if (payloadSize <= 0 || payloadSize != filesize)
    {
        munmap(filedata, static_cast<size_t>(filesize));
        return false;
    }

    *data_start = filedata;
    *size = filesize;
    return true;
}

// The external-assembly R2R probe: serves the composite webcil from the baked buffer and each managed
// assembly's per-assembly stub from "<dir>/comp/<base>.dll" on disk, searching the supplied dirs (each
// expected to carry a trailing path delimiter). Returns false for anything it does not provide, letting
// the caller fall back to its normal assembly load.
static bool WasiStaticR2RProbe(const char* name, const char* const* dirs, size_t ndirs, void** data_start, int64_t* size)
{
    // The composite native image itself: return the merged composite payload at imageBase. Its size is
    // read from the self-describing WbIL header (no baked constant), and validated against the buffer cap.
    if (strcmp(name, WASI_R2R_COMPOSITE_NAME) == 0)
    {
        int64_t payloadSize = WasiWebcilPayloadSize(&g_wasi_r2r_image[0], g_wasi_r2r_image_cap);
        if (payloadSize <= 0 || static_cast<size_t>(payloadSize) > g_wasi_r2r_image_cap)
            return false; // buffer not populated, or composite payload exceeds the cap

        // NOTE: the cap test above cannot protect this buffer -- the engine installs the segment before any
        // host code runs, so an over-cap payload has already overwritten whatever follows by the time we look.
        // The enforceable check is at build time; the C# composer compares the payload size against the cap.

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
            // Build "<dir>/comp/<base>.dll"; the build-time composer extracts raw WebCIL from the
            // passive wrapper so the runtime host does not need its own Wasm parser.
            int written = snprintf(stub, sizeof(stub), "%scomp/%s", dir, name);
            if (written < 0 || static_cast<size_t>(written) >= sizeof(stub))
                continue;
            if (WasiMapStubPayload(stub, data_start, size))
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
// The WASI R2R composer reads them out of the linked binary and validates the composite
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
