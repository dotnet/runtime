#include <cstdlib>
#include <cstdio>
#include <cstdint>
#include <cassert>
#include <stdexcept>
#include <memory>
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

bool get_metadata_from_pe(char const* file, malloc_span<byte>& b);

void dump(char const* p)
{
    malloc_span<byte> b;
    if (!get_metadata_from_pe(p, b))
        return;

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

PIMAGE_SECTION_HEADER find_section_header(
    PIMAGE_SECTION_HEADER section_headers,
    size_t count,
    uint32_t rva)
{
    for (size_t i = 0; i < count; ++i)
    {
        if (section_headers[i].VirtualAddress <= rva
            && rva < (section_headers[i].VirtualAddress + section_headers[i].SizeOfRawData))
        {
            return &section_headers[i];
        }
    }

    return NULL;
}

bool get_metadata_from_pe(char const* file, malloc_span<byte>& b)
{
    FILE* fd;
    errno_t ec = ::fopen_s(&fd, file, "rb");
    if (ec)
        return false;

    // [FIXME] Just a big block of bytes
    size_t size = 12 * 1024 * 1024;
    b = { (byte*)std::malloc(size), size };
    size_t bytes_read = ::fread(b, 1, b.size(), fd);
    if (bytes_read == 0 || bytes_read > size)
        return false;

    PIMAGE_DOS_HEADER dos_header = (PIMAGE_DOS_HEADER)(void*)b;
    bool is_pe = dos_header->e_magic == IMAGE_DOS_SIGNATURE;
    assert(is_pe);

    // Handle headers that are 32 or 64
    PIMAGE_DATA_DIRECTORY dotnet_dir;
    PIMAGE_SECTION_HEADER tgt_header;
    size_t section_count;
    // Section headers begin immediately after the NT_HEADERS.
    PIMAGE_SECTION_HEADER section_headers;

    PIMAGE_NT_HEADERS nt_header_any = (PIMAGE_NT_HEADERS)(b + dos_header->e_lfanew);
    if (nt_header_any->FileHeader.Machine == IMAGE_FILE_MACHINE_AMD64)
    {
        PIMAGE_NT_HEADERS64 nt_header64 = (PIMAGE_NT_HEADERS64)nt_header_any;
        dotnet_dir = &nt_header64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR];
        section_count = nt_header64->FileHeader.NumberOfSections;
        section_headers = (PIMAGE_SECTION_HEADER)&nt_header64[1];
    }
    else
    {
        PIMAGE_NT_HEADERS32 nt_header32 = (PIMAGE_NT_HEADERS32)nt_header_any;
        dotnet_dir = &nt_header32->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR];
        section_count = nt_header32->FileHeader.NumberOfSections;
        section_headers = (PIMAGE_SECTION_HEADER)&nt_header32[1];
    }

    bool is_dotnet = dotnet_dir->Size != 0;
    if (!is_dotnet)
    {
        ::fclose(fd);
        return false;
    }

    tgt_header = find_section_header(section_headers, section_count, dotnet_dir->VirtualAddress);

    // [FIXME] Did we find the section to work in?

    PIMAGE_COR20_HEADER cor_header = (PIMAGE_COR20_HEADER)(b + (dotnet_dir->VirtualAddress - tgt_header->VirtualAddress) + tgt_header->PointerToRawData);
    tgt_header = find_section_header(section_headers, section_count, cor_header->MetaData.VirtualAddress);

    // [FIXME] Did we find the section to work in?

    void* ptr = (void*)(b + (cor_header->MetaData.VirtualAddress - tgt_header->VirtualAddress) + tgt_header->PointerToRawData);
    size_t metadata_length = cor_header->MetaData.Size;

    byte* metadata = (byte*)std::malloc(metadata_length);
    std::memcpy(metadata, ptr, metadata_length);
    ::fclose(fd);

    b = { metadata, metadata_length };
    return true;
}
