#include <cstdlib>
#include <cstdio>
#include <cstdint>
#include <cassert>
#include <fstream>
#include <stdexcept>
#include <memory>
#include <array>

#include <platform.h>
#include <dnmd.hpp>
#include <span.hpp>

bool create_mdhandle(malloc_span<uint8_t> const& buffer, mdhandle_ptr& handle)
{
    mdhandle_t h;
    if (!md_create_handle(buffer, buffer.size(), &h))
        return false;
    handle.reset(h);
    return true;
}

bool read_in_file(char const* file, malloc_span<uint8_t>& b);
bool get_metadata_from_pe(malloc_span<uint8_t>& b);
bool get_metadata_from_file(malloc_span<uint8_t>& b);

struct dump_config_t
{
    dump_config_t()
        : path{}
        , table_id{ -1 }
    { }

    char const* path;
    int32_t table_id;
};

void dump(dump_config_t cfg)
{
    malloc_span<uint8_t> b;
    if (!read_in_file(cfg.path, b))
    {
        std::fprintf(stderr, "Failed to read in '%s'\n", cfg.path);
        return;
    }

    if (!get_metadata_from_pe(b) && !get_metadata_from_file(b))
    {
        std::fprintf(stderr, "Failed to read file as PE or metadata blob.\n");
        return;
    }

    std::printf("Loaded '%s'.\n    Metadata blog size %zu bytes\n", cfg.path, b.size());
    if (cfg.table_id != -1)
        std::printf("    Reading in table %d (0x%x)\n", cfg.table_id, cfg.table_id);

    mdhandle_ptr handle;
    if (!create_mdhandle(b, handle)
        || !md_validate(handle.get())
        || !md_dump_tables(handle.get(), cfg.table_id))
    {
        std::fprintf(stderr, "invalid metadata!\n");
    }
}

static char const* s_usage = "Syntax: mddump [-t <table_id>]? <path ecma-335 data>";

int main(int ac, char** av)
{
    if (ac <= 1)
    {
        std::fprintf(stderr, "Missing metadata file.\n\n%s\n", s_usage);
        return EXIT_FAILURE;
    }

    dump_config_t cfg;

    // Process arguments
    span<char*> args{ &av[1], (size_t)ac - 1 };
    for (int i = 0; i < args.size(); ++i)
    {
        char* arg = args[i];
        if (arg[0] != '-')
        {
            cfg.path = arg;
            continue;
        }

        size_t len = strlen(arg);
        if (len >= 2)
        {
            switch (arg[1])
            {
            case 't':
            {
                i++;
                if (i >= args.size())
                {
                    std::fprintf(stderr, "Missing table ID.\n");
                    return EXIT_FAILURE;
                }

                cfg.table_id = ::strtoul(args[i], nullptr, 0);
                if ((errno == ERANGE) || cfg.table_id >= 64)
                {
                    std::fprintf(stderr, "Invalid table ID: '%s'. Must be [0, 64)\n", args[i]);
                    return EXIT_FAILURE;
                }
                continue;
            }
            case 'h':
            case '?':
                std::printf("%s\n", s_usage);
                return EXIT_SUCCESS;
            default:
                break;
            }
        }

        std::fprintf(stderr, "Invalid argument: '%s'\n\n%s\n", arg, s_usage);
        return EXIT_FAILURE;
    }

    dump(cfg);

    return EXIT_SUCCESS;
}

uint32_t get_file_size(char const* path)
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

PIMAGE_SECTION_HEADER find_section_header(
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

    return NULL;
}

bool read_in_file(char const* file, malloc_span<uint8_t>& b)
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

bool get_metadata_from_pe(malloc_span<uint8_t>& b)
{
    if (b.size() < sizeof(IMAGE_DOS_HEADER))
        return false;

    // [TODO] Handle endian issues with .NET generated PE images
    // All integers should be read as little-endian.
    PIMAGE_DOS_HEADER dos_header = (PIMAGE_DOS_HEADER)(void*)b;
    bool is_pe = dos_header->e_magic == IMAGE_DOS_SIGNATURE;
    if (!is_pe)
        return false;

    // Handle headers that are 32 or 64
    PIMAGE_DATA_DIRECTORY dotnet_dir;
    PIMAGE_SECTION_HEADER tgt_header;

    // Section headers begin immediately after the NT_HEADERS.
    span<IMAGE_SECTION_HEADER> section_headers;

    PIMAGE_NT_HEADERS nt_header_any = (PIMAGE_NT_HEADERS)(b + dos_header->e_lfanew);
    if (nt_header_any->FileHeader.Machine == IMAGE_FILE_MACHINE_AMD64)
    {
        PIMAGE_NT_HEADERS64 nt_header64 = (PIMAGE_NT_HEADERS64)nt_header_any;
        dotnet_dir = &nt_header64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR];
        section_headers = { (PIMAGE_SECTION_HEADER)&nt_header64[1], nt_header64->FileHeader.NumberOfSections };
    }
    else
    {
        PIMAGE_NT_HEADERS32 nt_header32 = (PIMAGE_NT_HEADERS32)nt_header_any;
        dotnet_dir = &nt_header32->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR];
        section_headers = { (PIMAGE_SECTION_HEADER)&nt_header32[1], nt_header32->FileHeader.NumberOfSections };
    }

    // Doesn't contain a .NET header
    bool is_dotnet = dotnet_dir->Size != 0;
    if (!is_dotnet)
        return false;

    tgt_header = find_section_header(section_headers, dotnet_dir->VirtualAddress);
    if (tgt_header == nullptr)
        return false;

    PIMAGE_COR20_HEADER cor_header = (PIMAGE_COR20_HEADER)(b + (dotnet_dir->VirtualAddress - tgt_header->VirtualAddress) + tgt_header->PointerToRawData);
    tgt_header = find_section_header(section_headers, cor_header->MetaData.VirtualAddress);
    if (tgt_header == nullptr)
        return false;

    void* ptr = (void*)(b + (cor_header->MetaData.VirtualAddress - tgt_header->VirtualAddress) + tgt_header->PointerToRawData);
    size_t metadata_length = cor_header->MetaData.Size;

    // Capture the metadata portion of the image.
    malloc_span<uint8_t> metadata = { (uint8_t*)std::malloc(metadata_length), metadata_length };
    std::memcpy(metadata, ptr, metadata.size());
    b = std::move(metadata);
    return true;
}

bool get_metadata_from_file(malloc_span<uint8_t>& b)
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
