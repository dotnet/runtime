// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <iostream>
#include <assert.h>
#include <cstring>
#include <string>
#include <algorithm>

#ifdef _WIN32
#define WCHAR(str) L##str
#define CAST_CHAR(ch) ch

#else // defined(_WIN32)

// Definitely won't work for non-ascii characters so hopefully we never start using
// them in the tests
#define CAST_CHAR(ch) static_cast<wchar_t>(ch)

// On linux the runtime uses 16 bit strings but the native platform wchar_t is 32 bit.
// This means there aren't c runtime functions like wcslen for 16 bit strings. The idea
// here is to provide the easy ones to avoid all the copying and transforming. If more complex
// string operations become necessary we should either write them in C++ or convert the string to
// 32 bit and call the c runtime ones.

#define WCHAR(str) u##str
inline size_t wcslen(const char16_t *str)
{
    if (str == NULL)
    {
        return 0;
    }

    size_t len = 0;
    while (*str != 0)
    {
        ++str;
        ++len;
    }

    return len;
}

inline int wcscmp(const char16_t *lhs, const char16_t *rhs)
{
    int i = 0;
    while (true)
    {
        if (lhs[i] != rhs[i] || (lhs[i] == 0 && rhs[i] == 0))
        {
            break;
        }

        ++i;
    }

    return lhs[i] - rhs[i];
}

#endif // defined(__WIN32)

// 16 bit string type that works cross plat and doesn't require changing widths
// on non-windows platforms
class String
{
    friend std::wostream& operator<<(std::wostream& os, const String& obj);
private:
    WCHAR *buffer;
    size_t bufferLen;
    wchar_t *printBuffer;
    size_t printBufferLen;
    const size_t DefaultStringLength = 1024;

    void CopyBuffer(const WCHAR *other)
    {
        assert(other != nullptr);

        size_t otherLen = wcslen(other) + 1;
        if (buffer == nullptr || otherLen > bufferLen)
        {
            bufferLen = std::max(DefaultStringLength, otherLen);
            if (buffer != nullptr)
            {
                delete[] buffer;
            }

            buffer = new WCHAR[bufferLen];
        }

        memcpy(buffer, other, otherLen * sizeof(WCHAR));
    }

public:
    String(const WCHAR *s = WCHAR("")) :
        buffer(nullptr),
        bufferLen(0),
        printBuffer(nullptr),
        printBufferLen(0)
    {
        CopyBuffer(s);
    }

    ~String()
    {
        if (buffer != nullptr)
        {
            bufferLen = 0;
            delete[] buffer;
        }

        if (printBuffer != nullptr)
        {
            printBufferLen = 0;
            delete[] printBuffer;
        }
    }

    String(const String& other) :
        buffer(nullptr),
        bufferLen(0),
        printBuffer(nullptr),
        printBufferLen(0)
    {
        CopyBuffer(other.buffer);
    }

    String(String&& other) noexcept :
        buffer(nullptr),
        bufferLen(0),
        printBuffer(nullptr),
        printBufferLen(0)
    {
        std::swap(buffer, other.buffer);
        std::swap(bufferLen, other.bufferLen);
    }

    String& operator=(const String& other)
    {
        if(this != &other)
        {
            if (other.buffer != nullptr)
            {
                CopyBuffer(other.buffer);
            }
        }

        printBuffer = nullptr;
        printBufferLen = 0;

        return *this;
    }

    String& operator=(String&& other) noexcept
    {
        std::swap(buffer, other.buffer);
        std::swap(bufferLen, other.bufferLen);

        printBuffer = nullptr;
        printBufferLen = 0;

        return *this;
    }

    bool operator==(const String& other) const
    {
        if (buffer == nullptr)
        {
            return buffer == other.buffer;
        }

        return wcscmp(buffer, other.buffer) == 0;
    }

    bool operator!=(const String& other) const
    {
        return !(*this == other);
    }

    String& operator+=(const String& other)
    {
        size_t currLen = wcslen(buffer);
        size_t otherLen = wcslen(other.buffer);
        size_t candidateLen = currLen + otherLen + 1;

        if (candidateLen > bufferLen)
        {
            WCHAR *newBuffer = new WCHAR[candidateLen];
            memcpy(newBuffer, buffer, currLen * sizeof(WCHAR));
            delete[] buffer;
            buffer = newBuffer;
        }

        memcpy(buffer + currLen, other.buffer, otherLen * sizeof(WCHAR));
        buffer[candidateLen - 1] = 0;
        return *this;
    }

    WCHAR& operator[] (size_t pos)
    {
        return buffer[pos];
    }

    const WCHAR& operator[] (size_t pos) const
    {
        return buffer[pos];
    }

    void Clear()
    {
        if (buffer != nullptr)
        {
            buffer[0] = 0;
        }
    }

    const wchar_t *ToCStr()
    {
        if (bufferLen == 0 || buffer == nullptr)
        {
            // Nothing to convert
            return nullptr;
        }

        if (bufferLen > printBufferLen)
        {
            if (printBuffer != nullptr)
            {
                delete[] printBuffer;
            }

            printBuffer = new wchar_t[bufferLen];
            printBufferLen = bufferLen;
        }

        for (size_t i = 0; i < bufferLen; ++i)
        {
            printBuffer[i] = CAST_CHAR(buffer[i]);
        }

        // Make sure it's null terminated
        printBuffer[bufferLen - 1] = '\0';

        return printBuffer;
    }

    std::wstring ToWString()
    {
        std::wstring temp;
        for (size_t i = 0; i < bufferLen; ++i)
        {
            if (buffer[i] == 0)
            {
                break;
            }

            temp.push_back(CAST_CHAR(buffer[i]));
        }

        return temp;
    }

    size_t Size() const
    {
        return wcslen(buffer);
    }
};

inline std::wostream& operator<<(std::wostream& os, const String& obj)
{
    for (size_t i = 0; i < obj.bufferLen; ++i)
    {
        if (obj.buffer[i] == 0)
        {
            break;
        }

        os << CAST_CHAR(obj.buffer[i]);
    }

    return os;
}
