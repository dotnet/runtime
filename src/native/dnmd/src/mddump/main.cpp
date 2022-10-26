#include <cstdlib>
#include <cstdio>
#include <cstdint>
#include <cassert>
#include <fstream>
#include <stdexcept>
#include <memory>
#include <array>
#include <dnmd.h>

template<typename T>
class span
{
protected:
    T* _ptr;
    size_t _size;
public:
    span()
        : _ptr{}
        , _size{}
    { }

    span(T* ptr, size_t len)
        : _ptr{ ptr }, _size{ len }
    { }

    span(span const & other) = default;

    span& operator=(span&& other) = default;

    size_t size() const noexcept
    {
        return _size;
    }

    operator T* () noexcept
    {
        return _ptr;
    }

    T& operator[](size_t idx)
    {
        if (_ptr == nullptr)
            throw std::runtime_error{ "Deref null" };
        if (idx >= _size)
            throw std::out_of_range{ "Out of bounds access" };
        return _ptr[idx];
    }
};

template<typename T, typename Deleter>
class owning_span : public span<T>
{
public:
    owning_span() : span{}
    { }

    owning_span(T* ptr, size_t len)
        : span{ ptr, len }
    { }

    owning_span(owning_span&& other)
        : span{}
    {
        *this = other;
    }

    ~owning_span()
    {
        Deleter{}(_ptr);
    }

    owning_span& operator=(owning_span&& other) noexcept
    {
        if (_ptr != nullptr)
            Deleter{}(_ptr);

        _ptr = other._ptr;
        _size = other._size;
        other._ptr = {};
        other._size = {};
        return *this;
    }

    T* release() noexcept
    {
        T* tmp = _ptr;
        _ptr = {};
        return tmp;
    }
};

struct free_deleter
{
    void operator()(void* ptr)
    {
        free(ptr);
    }
};

template<typename T>
using malloc_span = owning_span<T, free_deleter>;

struct mdhandle_deleter
{
    using pointer = mdhandle_t;
    void operator()(mdhandle_t handle)
    {
        md_destroy_handle(handle);
    }
};

using mdhandle_lifetime = std::unique_ptr<mdhandle_t, mdhandle_deleter>;

bool create_mdhandle(malloc_span<byte>& buffer, mdhandle_lifetime& handle)
{
    mdhandle_t h;
    if (!md_create_handle(buffer, buffer.size(), &h))
        return false;
    handle.reset(h);
    (void)buffer.release();
    return true;
}

bool read_in_file(char const* file, malloc_span<byte>& b);
bool get_metadata_from_pe(malloc_span<byte>& b);
bool get_metadata_from_file(malloc_span<byte>& b);

void dump(char const* p)
{
    malloc_span<byte> b;
    if (!read_in_file(p, b))
    {
        std::fprintf(stderr, "Failed to read in '%s'\n", p);
        return;
    }

    if (!get_metadata_from_pe(b) && !get_metadata_from_file(b))
    {
        std::fprintf(stderr, "Failed to read file as PE or metadata blob.\n");
        return;
    }

    std::printf("%s = 0x%p, size %zu\n", p, &b, b.size());

    mdhandle_lifetime handle;
    if (!create_mdhandle(b, handle)
        || !md_validate(handle.get())
        || !md_dump_tables(handle.get()))
    {
        std::printf("invalid metadata!\n");
    }
}

int main(int ac, char** av)
{
    if (ac <= 1)
    {
        std::printf("Missing argument.\n\nSyntax: mddump <path ecma-335 assembly>\n");
        return EXIT_FAILURE;
    }

    span<char*> args{ &av[1], (size_t)ac - 1 };
    dump(args[0]);

    return EXIT_SUCCESS;
}

uint32_t get_file_size(char const* path)
{
    uint32_t size_in_bytes = 0;
#ifdef BUILD_WINDOWS
    HANDLE handle = ::CreateFileA(path, GENERIC_READ, FILE_SHARE_READ, nullptr, OPEN_EXISTING, 0, nullptr);
    if (handle != INVALID_HANDLE_VALUE)
    {
        size_in_bytes = ::GetFileSize(handle, nullptr);
        (void)::CloseHandle(handle);
    }
#else
#error Not yet implemented
#endif // !BUILD_WINDOWS

    return size_in_bytes;
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

bool read_in_file(char const* file, malloc_span<byte>& b)
{
    // Read in the entire file
    std::ifstream fd{ file, std::ios::binary | std::ios::in };
    if (!fd)
        return false;

    size_t size = get_file_size(file);
    if (size == 0)
        return false;

    b = { (byte*)std::malloc(size), size };
    fd.read((char*)(byte*)b, b.size());
    return true;
}

bool get_metadata_from_pe(malloc_span<byte>& b)
{
    if (b.size() < sizeof(IMAGE_DOS_HEADER))
        return false;

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
    malloc_span<byte> metadata = { (byte*)std::malloc(metadata_length), metadata_length };
    std::memcpy(metadata, ptr, metadata.size());
    b = std::move(metadata);
    return true;
}

bool get_metadata_from_file(malloc_span<byte>& b)
{
    // Defined in II.24.2.1 - defined in physical byte order
    std::array<byte, 4> const metadata_sig = { 0x42, 0x53, 0x4A, 0x42 };

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
