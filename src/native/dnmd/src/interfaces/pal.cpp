#include <assert.h>
#include "pal.hpp"

#ifdef BUILD_MACOS
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
#elif defined(BUILD_MACOS)
	UErrorCode err = U_ZERO_ERROR;
    (void)::u_strToUTF8(buffer, bufferLength, &length, (UChar const*)str, -1, &err);
    if (U_FAILURE(err))
    {
        if (err == U_BUFFER_OVERFLOW_ERROR)
        {
            if (writtenOrNeeded != nullptr)
            {
                (void)::u_strToUTF8(nullptr, 0, &length, (UChar const*)str, -1, &err);
                *writtenOrNeeded = (uint32_t)length;
            }
            return E_NOT_SUFFICIENT_BUFFER;
        }
        return E_FAIL;
    }
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
#elif defined(BUILD_MACOS)
	UErrorCode err = U_ZERO_ERROR;
    (void)::u_strFromUTF8((UChar*)buffer, bufferLength, &length, str, -1, &err);
    if (U_FAILURE(err))
    {
        if (err == U_BUFFER_OVERFLOW_ERROR)
        {
            if (writtenOrNeeded != nullptr)
            {
                (void)::u_strFromUTF8(nullptr, 0, &length, str, -1, &err);
                *writtenOrNeeded = (uint32_t)length;
            }
            return E_NOT_SUFFICIENT_BUFFER;
        }
        return E_FAIL;
    }
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

#ifndef __STDC_LIB_EXT1__
errno_t strcat_s(char* dest, rsize_t destsz, char const* src)
{
    assert(dest != nullptr && src != nullptr);
    ::strcat(dest, src);
    return 0;
}
#endif // !__STDC_LIB_EXT1__
