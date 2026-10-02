#ifndef _SRC_INC_INTERNAL_DNMD_TOOLS_PLATFORM_HPP_
#define _SRC_INC_INTERNAL_DNMD_TOOLS_PLATFORM_HPP_

#include <cstdlib>
#include <fstream>
#include <array>
#include <cstring>
#include <utility>

#include "dnmd_platform.hpp"
#include "dnmd_peimage.hpp"
#include "span.hpp"

inline bool create_mdhandle(malloc_span<uint8_t> const& buffer, mdhandle_ptr& handle)
{
    mdhandle_t h;
    if (!md_create_handle(buffer.data(), buffer.size(), &h))
        return false;
    handle.reset(h);
    return true;
}

//
// PE File functions
//

inline uint32_t get_file_size(char const* path)
{
    uint32_t size_in_uint8_ts = 0;
#ifdef BUILD_WINDOWS
    HANDLE handle = ::CreateFileA(path, GENERIC_READ, FILE_SHARE_READ, nullptr, OPEN_EXISTING, 0, nullptr);
    if (handle != INVALID_HANDLE_VALUE)
    {
        size_in_uint8_ts = ::GetFileSize(handle, nullptr);
        (void)::CloseHandle(handle);
    }
#else
    struct stat st;
    int rc = stat(path, &st);
    if (rc == 0)
        size_in_uint8_ts = st.st_size;
#endif // !BUILD_WINDOWS

    return size_in_uint8_ts;
}

inline bool read_in_file(char const* file, malloc_span<uint8_t>& b)
{
    // Read in the entire file
    std::ifstream fd{ file, std::ios::binary | std::ios::in };
    if (!fd)
        return false;

    size_t size = get_file_size(file);
    if (size == 0)
        return false;

    b = { (uint8_t*)std::malloc(size), size };
    fd.read((char*)b.data(), b.size());
    return true;
}

inline bool write_out_file(char const* file, malloc_span<uint8_t> b)
{
    // Read in the entire file
    std::ofstream fd{ file, std::ios::binary | std::ios::out };
    if (!fd)
        return false;

    fd.write((char*)b.data(), b.size());
    return true;
}

inline bool get_metadata_from_pe(malloc_span<uint8_t>& b)
{
    dnmd::PEMetadataInfo info;
    if (!dnmd::TryGetPEMetadata(b.data(), b.size(), info))
        return false;

    uint8_t* data = static_cast<uint8_t*>(std::malloc(info.metadataSize));
    if (data == nullptr)
        return false;
    malloc_span<uint8_t> metadata{ data, info.metadataSize };
    std::memcpy(metadata.data(), b.data() + info.metadataOffset, metadata.size());
    b = std::move(metadata);
    return true;
}

inline bool get_metadata_from_file(malloc_span<uint8_t>& b)
{
    // Defined in II.24.2.1 - defined in physical uint8_t order
    std::array<uint8_t, 4> const metadata_sig = { 0x42, 0x53, 0x4A, 0x42 };

    if (b.size() < metadata_sig.size())
        return false;

    // If the header doesn't match, the file is unknown.
    for (size_t i = 0; i < metadata_sig.size(); ++i)
    {
        if (b[i] != metadata_sig[i])
            return false;
    }

    return true;
}

#endif // _SRC_INC_INTERNAL_DNMD_TOOLS_PLATFORM_HPP_
