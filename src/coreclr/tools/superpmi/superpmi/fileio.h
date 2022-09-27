// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _FileIO
#define _FileIO

template<typename HandleType, HandleType InvalidHandleValue, typename FreeFunction>
struct HandleWrapper
{
    HandleWrapper()
        : m_handle(InvalidHandleValue)
    {
    }

    explicit HandleWrapper(HandleType handle)
        : m_handle(handle)
    {
    }

    ~HandleWrapper()
    {
        if (m_handle != InvalidHandleValue)
        {
            FreeFunction{}(m_handle);

            m_handle = InvalidHandleValue;
        }
    }

    HandleWrapper(const HandleWrapper&) = delete;
    HandleWrapper& operator=(HandleWrapper&) = delete;

    HandleWrapper(HandleWrapper&& hw)
        : m_handle(hw.m_handle)
    {
        hw.m_handle = INVALID_HANDLE_VALUE;
    }

    HandleWrapper& operator=(HandleWrapper&& hw)
    {
        if (m_handle != InvalidHandleValue)
            CloseHandle(m_handle);

        m_handle = hw.m_handle;
        hw.m_handle = InvalidHandleValue;
        return *this;
    }

    bool IsValid() { return m_handle != InvalidHandleValue; }
    HandleType Get() { return m_handle; }

private:
    HandleType m_handle;
};

struct CloseHandleFunctor
{
    void operator()(HANDLE handle)
    {
        CloseHandle(handle);
    }
};

struct UnmapViewOfFileFunctor
{
    void operator()(LPVOID view)
    {
        UnmapViewOfFile(view);
    }
};

typedef HandleWrapper<HANDLE, INVALID_HANDLE_VALUE, CloseHandleFunctor> FileHandle;
typedef HandleWrapper<HANDLE, nullptr, CloseHandleFunctor> FileMappingHandle;
typedef HandleWrapper<LPVOID, nullptr, UnmapViewOfFileFunctor> FileViewHandle;

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
    {
    }

    bool AdvanceLine();
    const char* GetCurrentLine() { return m_currentLine.data(); }

    static bool Open(const char* path, FileLineReader* fr);
};

#endif
