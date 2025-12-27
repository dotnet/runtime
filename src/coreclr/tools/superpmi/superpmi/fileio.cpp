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
    if (numChars > (m_buffer.size() - m_bufferIndex))
    {
        Flush();

        if (numChars > m_buffer.size())
        {
            DWORD numWritten;
            bool result =
                WriteFile(m_file.Get(), value, static_cast<DWORD>(numChars), &numWritten, nullptr) &&
                (numWritten == static_cast<DWORD>(numChars));
            return result;
        }
    }

    memcpy(m_buffer.data() + m_bufferIndex, value, numChars);
    m_bufferIndex += numChars;
    return true;
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

bool FileWriter::Flush()
{
    if (m_bufferIndex <= 0)
        return true;

    size_t numWritten = fwrite(m_buffer.data(), 1, m_bufferIndex, m_file.Get());
    bool result = (numWritten == m_bufferIndex);
    m_bufferIndex = 0;
    return result;
}

bool FileWriter::CreateNew(const char* path, FileWriter* fw)
{
    FILEHandle handle(fopen(path, "wb"));
    if (!handle.IsValid())
    {
        return false;
    }

    *fw = FileWriter(std::move(handle));
    return true;
}
