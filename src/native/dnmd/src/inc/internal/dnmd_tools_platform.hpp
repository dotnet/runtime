#ifndef _SRC_INC_INTERNAL_DNMD_TOOLS_PLATFORM_HPP_
#define _SRC_INC_INTERNAL_DNMD_TOOLS_PLATFORM_HPP_

#include <cstdlib>
#include <fstream>
#include <stdexcept>
#include <array>
#include <cstring>

#include "dnmd_platform.hpp"
#include "span.hpp"

inline bool create_mdhandle(malloc_span<uint8_t> const& buffer, mdhandle_ptr& handle)
{
    mdhandle_t h;
    if (!md_create_handle(buffer, buffer.size(), &h))
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

inline PIMAGE_SECTION_HEADER find_section_header(
        span<IMAGE_SECTION_HEADER> section_headers,
        uint32_t rva)
{
    for (size_t i = 0; i < section_headers.size(); ++i)
    {
        if (section_headers[i].VirtualAddress <= rva
            && rva < (section_headers[i].VirtualAddress + section_headers[i].SizeOfRawData))
        {
            return &section_headers[i];
        }
    }

    return nullptr;
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
    fd.read((char*)(uint8_t*)b, b.size());
    return true;
}

inline bool write_out_file(char const* file, malloc_span<uint8_t> b)
{
    // Read in the entire file
    std::ofstream fd{ file, std::ios::binary | std::ios::out };
    if (!fd)
        return false;

    fd.write((char*)(uint8_t*)b, b.size());
    return true;
}

inline bool find_pe_image_bitness(uint16_t machine, uint8_t& bitness)
{
#define MAKE_MACHINE_CASE(x) \
    case ((x) ^ IMAGE_FILE_MACHINE_OS_MASK_APPLE): \
    case ((x) ^ IMAGE_FILE_MACHINE_OS_MASK_FREEBSD): \
    case ((x) ^ IMAGE_FILE_MACHINE_OS_MASK_LINUX): \
    case ((x) ^ IMAGE_FILE_MACHINE_OS_MASK_NETBSD): \
    case ((x) ^ IMAGE_FILE_MACHINE_OS_MASK_SUN): \
    case (x)
    
    switch (machine)
    {
    MAKE_MACHINE_CASE(IMAGE_FILE_MACHINE_I386):
    MAKE_MACHINE_CASE(IMAGE_FILE_MACHINE_ARM):
        bitness = 32;
        return true;
    MAKE_MACHINE_CASE(IMAGE_FILE_MACHINE_AMD64):
    MAKE_MACHINE_CASE(IMAGE_FILE_MACHINE_ARM64):
        bitness = 64;
        return true;
    default:
        return false;
    }

#undef MAKE_MACHINE_CASE
}

inline bool get_metadata_from_pe(malloc_span<uint8_t>& b)
{
    if (b.size() < sizeof(IMAGE_DOS_HEADER))
        return false;

    // [TODO] Handle endian issues with .NET generated PE images
    // All integers should be read as little-endian.
    auto dos_header = (PIMAGE_DOS_HEADER)(void*)b;
    bool is_pe = dos_header->e_magic == IMAGE_DOS_SIGNATURE;
    if (!is_pe)
        return false;

    // Handle headers that are 32 or 64
    PIMAGE_SECTION_HEADER tgt_header;
    PIMAGE_DATA_DIRECTORY dotnet_dir;

    // Section headers begin immediately after the NT_HEADERS.
    span<IMAGE_SECTION_HEADER> section_headers;

    if ((size_t)dos_header->e_lfanew > b.size())
        return false;

    size_t remaining_pe_size = b.size() - dos_header->e_lfanew;
    uint16_t section_header_count;
    uint8_t* section_header_begin;
    auto nt_header_any = (PIMAGE_NT_HEADERS)(b + dos_header->e_lfanew);
    uint16_t machine = nt_header_any->FileHeader.Machine;

    uint8_t bitness;
    if (!find_pe_image_bitness(machine, bitness))
        return false;

    if (bitness == 64)
    {
        auto nt_header64 = (PIMAGE_NT_HEADERS64)nt_header_any;
        if (remaining_pe_size < sizeof(*nt_header64))
            return false;
        remaining_pe_size -= sizeof(*nt_header64);
        section_header_count = nt_header64->FileHeader.NumberOfSections;
        section_header_begin = (uint8_t*)&nt_header64[1];
        dotnet_dir = &nt_header64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR];
    }
    else if (bitness == 32)
    {
        auto nt_header32 = (PIMAGE_NT_HEADERS32)nt_header_any;
        if (remaining_pe_size < sizeof(*nt_header32))
            return false;
        remaining_pe_size -= sizeof(*nt_header32);
        section_header_count = nt_header32->FileHeader.NumberOfSections;
        section_header_begin = (uint8_t*)&nt_header32[1];
        dotnet_dir = &nt_header32->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR];
    }
    else
    {
        // Unknown machine type
        return false;
    }

    // Doesn't contain a .NET header
    bool is_dotnet = dotnet_dir->Size != 0;
    if (!is_dotnet)
        return false;

    // Compute the maximum space in the PE to validate section header count.
    if (section_header_count > (remaining_pe_size / sizeof(IMAGE_SECTION_HEADER)))
        return false;

    remaining_pe_size -= section_header_count * sizeof(IMAGE_SECTION_HEADER);

    section_headers = { (PIMAGE_SECTION_HEADER)section_header_begin, section_header_count };

    tgt_header = find_section_header(section_headers, dotnet_dir->VirtualAddress);
    if (tgt_header == nullptr)
        return false;

    // Sanity check
    if (dotnet_dir->VirtualAddress < tgt_header->VirtualAddress)
        return false;

    DWORD cor_header_offset = (DWORD)(dotnet_dir->VirtualAddress - tgt_header->VirtualAddress) + tgt_header->PointerToRawData;
    if (cor_header_offset > b.size() - sizeof(IMAGE_COR20_HEADER))
        return false;

    auto cor_header = (PIMAGE_COR20_HEADER)(b + cor_header_offset);
    tgt_header = find_section_header(section_headers, cor_header->MetaData.VirtualAddress);
    if (tgt_header == nullptr)
        return false;

    // Sanity check
    if (cor_header->MetaData.VirtualAddress < tgt_header->VirtualAddress)
        return false;

    DWORD metadata_offset = (DWORD)(cor_header->MetaData.VirtualAddress - tgt_header->VirtualAddress) + tgt_header->PointerToRawData;
    if (metadata_offset > b.size())
        return false;

    void* ptr = (void*)(b + metadata_offset);

    size_t metadata_length = cor_header->MetaData.Size;
    if (metadata_length > b.size() - metadata_offset)
        return false;

    // Capture the metadata portion of the image.
    malloc_span<uint8_t> metadata = { (uint8_t*)std::malloc(metadata_length), metadata_length };
    std::memcpy(metadata, ptr, metadata.size());
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
