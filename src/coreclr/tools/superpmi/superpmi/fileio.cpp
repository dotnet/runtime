#include "standardpch.h"
#include "fileio.h"

bool FileWriter::Printf(const char* fmt, ...)
{
    va_list args;
    va_start(args, fmt);

    char stackBuffer[512];
    size_t bufferSize = sizeof(stackBuffer);
    char* pBuffer = stackBuffer;
    while (true)
    {
        va_list argsCopy;
        va_copy(argsCopy, args);
        int printed = _vsnprintf_s(pBuffer, bufferSize, _TRUNCATE, fmt, argsCopy);
        va_end(argsCopy);

        if (printed < 0)
        {
            // buffer too small
            if (pBuffer != stackBuffer)
                delete[] pBuffer;

            bufferSize *= 2;
            pBuffer = new char[bufferSize];
        }
        else
        {
            bool result = Print(pBuffer, static_cast<size_t>(printed));

            if (pBuffer != stackBuffer)
                delete[] pBuffer;

            va_end(args);
            return result;
        }
    }
}

bool FileWriter::Print(const char* value, size_t numChars)
{
    DWORD numWritten;
    bool result =
        WriteFile(m_file.Get(), value, static_cast<DWORD>(numChars), &numWritten, nullptr) &&
        (numWritten == static_cast<DWORD>(numChars));
    return result;
}

bool FileWriter::Print(const char* value)
{
    return Print(value, strlen(value));
}

bool FileWriter::Print(int value)
{
    return Printf("%d", value);
}

bool FileWriter::Print(int64_t value)
{
    return Printf("%lld", value);
}

bool FileWriter::Print(double value)
{
    return Printf("%f", value);
}

bool FileWriter::PrintQuotedCsvField(const char* value)
{
    size_t numQuotes = 0;
    for (const char* p = value; *p != '\0'; p++)
    {
        if (*p == '"')
        {
            numQuotes++;
        }
    }

    if (numQuotes == 0)
    {
        return Printf("\"%s\"", value);
    }
    else
    {
        size_t len = 2 + strlen(value) + numQuotes;
        char* buffer = new char[len];

        size_t index = 0;
        buffer[index++] = '"';
        for (const char* p = value; *p != '\0'; p++)
        {
            if (*p == '"')
            {
                buffer[index++] = '"';
            }
            buffer[index++] = *p;
        }

        buffer[index++] = '"';
        assert(index == len);

        bool result = Print(buffer, len);
        delete[] buffer;
        return result;
    }
}

bool FileWriter::CreateNew(const char* path, FileWriter* fw)
{
    FileHandle handle(CreateFile(path, GENERIC_WRITE, 0, nullptr, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr));
    if (!handle.IsValid())
    {
        return false;
    }

    *fw = FileWriter(std::move(handle));
    return true;
}

bool FileLineReader::Open(const char* path, FileLineReader* fr)
{
    FileHandle file(CreateFile(path, GENERIC_READ, FILE_SHARE_READ, nullptr, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, nullptr));
    if (!file.IsValid())
    {
        return false;
    }

    LARGE_INTEGER size;
    if (!GetFileSizeEx(file.Get(), &size))
    {
        return false;
    }

    if (static_cast<ULONGLONG>(size.QuadPart) > SIZE_MAX)
    {
        return false;
    }

    FileMappingHandle mapping(CreateFileMapping(file.Get(), nullptr, PAGE_READONLY, size.u.HighPart, size.u.LowPart, nullptr));
    if (!mapping.IsValid())
    {
        return false;
    }

    FileViewHandle view(MapViewOfFile(mapping.Get(), FILE_MAP_READ, 0, 0, 0));
    if (!view.IsValid())
    {
        return false;
    }

    *fr = FileLineReader(std::move(file), std::move(mapping), std::move(view), static_cast<size_t>(size.QuadPart));
    return true;
}

bool FileLineReader::AdvanceLine()
{
    if (m_cur >= m_end)
    {
        return false;
    }

    char* end = m_cur;
    while (end < m_end && *end != '\r' && *end != '\n')
    {
        end++;
    }

    m_currentLine.resize(end - m_cur + 1);
    memcpy(m_currentLine.data(), m_cur, end - m_cur);
    m_currentLine[end - m_cur] = '\0';

    m_cur = end;
    if (m_cur < m_end && *m_cur == '\r')
        m_cur++;
    if (m_cur < m_end && *m_cur == '\n')
        m_cur++;

    return true;
}
