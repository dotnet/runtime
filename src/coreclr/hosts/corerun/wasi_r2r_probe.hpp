// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Statically-composed WASI R2R external-assembly probe for the per-app-linked wasihost corehost
// (libWasiHost.a). The probe is a host_runtime_contract::external_assembly_probe callback: the runtime
// calls out to it to obtain the merged R2R webcil images and the per-assembly stubs.
//
// The in-tree WASI R2R composer populates the images. The host must be linked with the flags that
// supply the images' imports -- see WasiEnableCompositeR2R in WasiApp.CoreCLR.targets.
// Without them this probe compiles but can never be satisfied.
//
// Requires corerun.hpp to be included first (for pal::try_map_file_readonly). Include exactly once per
// host binary.

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
// Crossgen2-produced R2R webcil images are merged into the host module post-link: their native
// functions land in the shared indirect function table, and the offline merge's active data segments
// write their webcil payloads back to back into g_wasi_r2r_image (each at its own imageBase). The
// runtime then finds the R2R webcil via this probe, exactly the way the browser host does via
// BrowserHost_ExternalAssemblyProbe.
//
// The buffer is defined outside this header: a ReadyToRun publish builds a strong definition sized
// exactly to its composite, corerun reserves a fixed capacity for its test images, and the host archive
// carries a 64-byte weak fallback for non-R2R apps.
extern "C" uint8_t g_wasi_r2r_image[];
extern "C" uint32_t g_wasi_r2r_image_cap;

// The table index at which the first merged image's functions are installed; later images follow it.
// Under the reservation model the host is linked with `-Wl,--table-base=<N+1>`, which moves the host's
// own address-taken functions up to start at N+1 and leaves slots 1..N free for the images. This MUST
// match the first image's `__table_base` supplied to the merge (see docs/design/mono/webcil.md); the
// composer reads the host's exported value and lays the images out from it.
#ifndef WASI_R2R_TABLE_BASE
#define WASI_R2R_TABLE_BASE (1u)
#endif
#define WEBCIL_HEADER_V1_SIZE       (32u)
#define WEBCIL_SECTION_HEADER_SIZE  (16u)
#define WEBCIL_VERSION_MAJOR_OFFSET (4u)

// Registry of the R2R images merged into the host. Each entry names one image the runtime may request
// through external_assembly_probe -- a composite's file name (the ownerCompositeExecutable recorded in each
// per-assembly stub), or an assembly file name such as "System.Private.CoreLib.dll" for a non-composite
// image -- and gives where its payload sits inside g_wasi_r2r_image. The composer lays the images out back
// to back and writes the entries with an active data segment, so a host that was never composed matches
// no name at all. The layout (name[NAME_CAP], base u32, size u32) is validated by the composer against the
// exported entry size.
#ifndef WASI_R2R_NAME_CAP
#define WASI_R2R_NAME_CAP (256u)
#endif
#ifndef WASI_R2R_MAX_IMAGES
#define WASI_R2R_MAX_IMAGES (8u)
#endif
struct WasiR2RImageEntry
{
    char name[WASI_R2R_NAME_CAP];
    uint32_t base;
    uint32_t size;
};
static_assert(sizeof(WasiR2RImageEntry) == WASI_R2R_NAME_CAP + 8, "registry entry layout is shared with the composer");

extern "C"
{
    WasiR2RImageEntry g_wasi_r2r_images[WASI_R2R_MAX_IMAGES] = {};
}

static const WasiR2RImageEntry* WasiFindImage(const char* name)
{
    for (uint32_t i = 0; i < WASI_R2R_MAX_IMAGES; i++)
    {
        const WasiR2RImageEntry& entry = g_wasi_r2r_images[i];
        if (entry.name[0] == '\0')
            break;
        if (strncmp(name, entry.name, WASI_R2R_NAME_CAP) == 0)
            return &entry;
    }
    return nullptr;
}

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

// The external-assembly R2R probe: serves registered images from the baked buffer and each managed
// assembly's per-assembly stub from "<dir>/comp/<base>.dll" on disk, searching the supplied dirs (each
// expected to carry a trailing path delimiter). Returns false for anything it does not provide, letting
// the caller fall back to its normal assembly load.
static bool WasiStaticR2RProbe(const char* name, const char* const* dirs, size_t ndirs, void** data_start, int64_t* size)
{
    // A merged image (a composite, or a non-composite image such as CoreLib): return its payload from the
    // buffer. Its size is read from the self-describing WbIL header and must agree with the registry and lie
    // within the buffer.
    if (const WasiR2RImageEntry* entry = WasiFindImage(name))
    {
        uint32_t cap = g_wasi_r2r_image_cap;
        uintptr_t bufferStart = (uintptr_t)&g_wasi_r2r_image[0];
        uintptr_t imageStart = (uintptr_t)entry->base;
        if (imageStart < bufferStart || imageStart - bufferStart > cap || entry->size > cap - (imageStart - bufferStart))
            return false;

        uint8_t* image = reinterpret_cast<uint8_t*>(imageStart);
        int64_t payloadSize = WasiWebcilPayloadSize(image, entry->size);
        if (payloadSize <= 0 || payloadSize != (int64_t)entry->size)
            return false;

        // NOTE: these checks cannot protect the buffer -- the engine installs the segments before any host
        // code runs. The enforceable check is at build time; the C# composer validates the layout against the cap.
        *data_start = image;
        *size = payloadSize;
        return true;
    }

    // A managed assembly: return its per-assembly stub payload (extracted from <base>.wasm on disk).
    // The stub carries the assembly metadata + the R2R header naming the composite, which drives the
    // runtime to then request the composite name above.
    size_t nlen = strlen(name);
    if (nlen > 4 && strcmp(name + nlen - 4, ".dll") == 0)
    {
        char stub[512];
        for (size_t i = 0; i < ndirs; i++)
        {
            const char* dir = dirs[i];
            if (dir == nullptr) continue;
            // Build "<dir>/comp/<base>.dll"; the build-time composer extracts raw WebCIL from the Wasm
            // wrapper so the runtime host does not need its own Wasm parser.
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
// Keeping the exported accessor here ensures the linker roots the buffer and gives the composition
// step a stable anchor.
extern "C" __attribute__((export_name("wasi_r2r_image_base"))) uint32_t wasi_r2r_image_base(void)
{
    return (uint32_t)(uintptr_t)&wasi_r2r::g_wasi_r2r_image[0];
}

// The staging buffer's capacity and the table slot the first image installs at, exported for the same
// reason as the base: the composition must not carry its own copy of either. The host owns these
// values; the WASI R2R composer reads them out of the linked binary and validates the images
// against them, so a mismatch is a build-time error instead of a wrong-function dispatch at runtime.
// The capacity accessor is weak because a ReadyToRun publish supplies a strong definition alongside
// the sized buffer.
#ifndef CORERUN_WASI_R2R_STRONG_CAP
extern "C" __attribute__((weak, export_name("wasi_r2r_image_cap"))) uint32_t wasi_r2r_image_cap(void)
{
    return wasi_r2r::g_wasi_r2r_image_cap;
}
#endif // CORERUN_WASI_R2R_STRONG_CAP

extern "C" __attribute__((export_name("wasi_r2r_table_base"))) uint32_t wasi_r2r_table_base(void)
{
    return (uint32_t)WASI_R2R_TABLE_BASE;
}

// The image registry's address, entry count, and entry size, so the composer can describe the merged
// images without a host rebuild.
extern "C" __attribute__((export_name("wasi_r2r_registry_base"))) uint32_t wasi_r2r_registry_base(void)
{
    return (uint32_t)(uintptr_t)&wasi_r2r::g_wasi_r2r_images[0];
}

extern "C" __attribute__((export_name("wasi_r2r_registry_cap"))) uint32_t wasi_r2r_registry_cap(void)
{
    return (uint32_t)WASI_R2R_MAX_IMAGES;
}

extern "C" __attribute__((export_name("wasi_r2r_registry_entry_size"))) uint32_t wasi_r2r_registry_entry_size(void)
{
    return (uint32_t)sizeof(wasi_r2r::WasiR2RImageEntry);
}

#endif // TARGET_WASI

#endif // WASI_R2R_PROBE_HPP
