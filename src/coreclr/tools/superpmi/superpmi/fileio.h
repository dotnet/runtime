// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _FileIO
#define _FileIO

template<typename HandleSpec>
struct HandleWrapper
{
    using HandleType = typename HandleSpec::Type;

    HandleWrapper()
        : m_handle(HandleSpec::Invalid())
    {
    }

    explicit HandleWrapper(HandleType handle)
        : m_handle(handle)
    {
    }

    ~HandleWrapper()
    {
        if (m_handle != HandleSpec::Invalid())
        {
            HandleSpec::Close(m_handle);
            m_handle = HandleSpec::Invalid();
        }
    }

    HandleWrapper(const HandleWrapper&) = delete;
    HandleWrapper& operator=(HandleWrapper&) = delete;

    HandleWrapper(HandleWrapper&& hw) noexcept
        : m_handle(hw.m_handle)
    {
        hw.m_handle = HandleSpec::Invalid();
    }

    HandleWrapper& operator=(HandleWrapper&& hw) noexcept
    {
        if (m_handle != HandleSpec::Invalid())
            HandleSpec::Close(m_handle);

        m_handle = hw.m_handle;
        hw.m_handle = HandleSpec::Invalid();
        return *this;
    }

    bool IsValid() { return m_handle != HandleSpec::Invalid(); }
    HandleType Get() { return m_handle; }

private:
    HandleType m_handle;
};

struct FileHandleSpec
{
    using Type = HANDLE;
    static HANDLE Invalid() { return INVALID_HANDLE_VALUE; }
    static void Close(HANDLE h) { CloseHandle(h); }
};

struct FileMappingHandleSpec
{
    using Type = HANDLE;
    static HANDLE Invalid() { return nullptr; }
    static void Close(HANDLE h) { CloseHandle(h); }
};

struct FileViewHandleSpec
{
    using Type = LPVOID;
    static LPVOID Invalid() { return nullptr; }
    static void Close(LPVOID p) { UnmapViewOfFile(p); }
};

typedef HandleWrapper<FileHandleSpec> FileHandle;
typedef HandleWrapper<FileMappingHandleSpec> FileMappingHandle;
typedef HandleWrapper<FileViewHandleSpec> FileViewHandle;

class FileWriter
{
    FileHandle m_file;

    FileWriter(FileHandle file)
        : m_file(std::move(file))
    {
    }

public:
    FileWriter()
    {
    }

    bool Print(const char* value, size_t numChars);
    bool Print(const char* value);
    bool Print(int value);
    bool Print(int64_t value);
    bool Print(double value);
    bool PrintQuotedCsvField(const char* value);
    bool Printf(const char* fmt, ...);

    static bool CreateNew(const char* path, FileWriter* fw);
};

class FileLineReader
{
    FileHandle m_file;
    FileMappingHandle m_fileMapping;
    FileViewHandle m_view;

    char* m_cur;
    char* m_end;
    std::vector<char> m_currentLine;

    FileLineReader(FileHandle file, FileMappingHandle fileMapping, FileViewHandle view, size_t size)
        : m_file(std::move(file))
        , m_fileMapping(std::move(fileMapping))
        , m_view(std::move(view))
        , m_cur(static_cast<char*>(m_view.Get()))
        , m_end(static_cast<char*>(m_view.Get()) + size)
    {
    }

public:
    FileLineReader()
        : m_cur(nullptr)
        , m_end(nullptr)
    {
    }

    bool AdvanceLine();
    const char* GetCurrentLine() { return m_currentLine.data(); }

    static bool Open(const char* path, FileLineReader* fr);
};

#endif
