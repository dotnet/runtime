#include "pal.hpp"
#include <cstring>
#include <cassert>

#if defined(BUILD_MACOS) || defined(BUILD_UNIX)
#include <unicode/ustring.h>
#endif

HRESULT pal::ConvertUtf16ToUtf8(
    WCHAR const* str,
    char* buffer,
    uint32_t bufferLength,
    _Out_opt_ uint32_t* writtenOrNeeded)
{
    assert(str != nullptr);

    int32_t length;
#ifdef BUILD_WINDOWS
    length = ::WideCharToMultiByte(CP_UTF8, 0, str, -1, buffer, bufferLength, nullptr, nullptr);
    if (length <= 0)
    {
        if (::GetLastError() == ERROR_INSUFFICIENT_BUFFER)
        {
            if (writtenOrNeeded != nullptr)
                *writtenOrNeeded = ::WideCharToMultiByte(CP_UTF8, 0, str, -1, nullptr, 0, nullptr, nullptr);
            return E_NOT_SUFFICIENT_BUFFER;
        }
        return E_FAIL;
    }
#elif defined(BUILD_MACOS) || defined(BUILD_UNIX)
    // Buffer lengths assume null terminator
    if (bufferLength > 0)
        bufferLength -= 1;

    UErrorCode err = U_ZERO_ERROR;
    (void)::u_strToUTF8(buffer, bufferLength, &length, (UChar const*)str, -1, &err);
    if (U_FAILURE(err))
    {
        if (err != U_BUFFER_OVERFLOW_ERROR)
            return E_FAIL;

        if (bufferLength != 0)
        {
            if (writtenOrNeeded != nullptr)
                *writtenOrNeeded = (uint32_t)length + 1; // Add null terminator
            return E_NOT_SUFFICIENT_BUFFER;
        }
    }
    if (buffer != nullptr)
        buffer[length] = '\0';
    length += 1; // Add null terminator
#else
#error Missing implementation
#endif // !BUILD_WINDOWS

    if (writtenOrNeeded != nullptr)
        *writtenOrNeeded = (uint32_t)length;
    return S_OK;
}

HRESULT pal::ConvertUtf8ToUtf16(
    char const* str,
    WCHAR* buffer,
    uint32_t bufferLength,
    _Out_opt_ uint32_t* writtenOrNeeded)
{
    assert(str != nullptr);

    int32_t length;
#ifdef BUILD_WINDOWS
    length = ::MultiByteToWideChar(CP_UTF8, 0, str, -1, buffer, bufferLength);
    if (length <= 0)
    {
        if (::GetLastError() == ERROR_INSUFFICIENT_BUFFER)
        {
            if (writtenOrNeeded != nullptr)
                *writtenOrNeeded = ::MultiByteToWideChar(CP_UTF8, 0, str, -1, nullptr, 0);
            return E_NOT_SUFFICIENT_BUFFER;
        }
        return E_FAIL;
    }
#elif defined(BUILD_MACOS) || defined(BUILD_UNIX)
    // Buffer lengths assume null terminator
    if (bufferLength > 0)
        bufferLength -= 1;

    UErrorCode err = U_ZERO_ERROR;
    (void)::u_strFromUTF8((UChar*)buffer, bufferLength, &length, str, -1, &err);
    if (U_FAILURE(err))
    {
        if (err != U_BUFFER_OVERFLOW_ERROR)
            return E_FAIL;

        if (bufferLength != 0)
        {
            if (writtenOrNeeded != nullptr)
                *writtenOrNeeded = (uint32_t)length + 1; // Add null terminator
            return E_NOT_SUFFICIENT_BUFFER;
        }
    }
    if (buffer != nullptr)
        buffer[length] = W('\0');
    length += 1; // Add null terminator
#else
#error Missing implementation
#endif // !BUILD_WINDOWS

    if (writtenOrNeeded != nullptr)
        *writtenOrNeeded = (uint32_t)length;
    return S_OK;
}

template<>
HRESULT pal::StringConvert<WCHAR, char>::ConvertWorker(WCHAR const* c, char* buffer, uint32_t& bufferLength)
{
    return ConvertUtf16ToUtf8(c, buffer, bufferLength, &bufferLength);
}

template<>
HRESULT pal::StringConvert<char, WCHAR>::ConvertWorker(char const* c, WCHAR* buffer, uint32_t& bufferLength)
{
    return ConvertUtf8ToUtf16(c, buffer, bufferLength, &bufferLength);
}

#if !defined(__STDC_LIB_EXT1__) && !defined(BUILD_WINDOWS)
int strcat_s(char* dest, rsize_t destsz, char const* src)
{
    assert(dest != nullptr && src != nullptr);
    (void)::strcat(dest, src);
    return 0;
}
#endif // !defined(__STDC_LIB_EXT1__) && !defined(BUILD_WINDOWS)
