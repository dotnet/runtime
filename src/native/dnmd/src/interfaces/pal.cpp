#include <assert.h>
#include "pal.hpp"

HRESULT pal::ConvertUtf16ToUtf8(
    WCHAR const* str,
    char* buffer,
    uint32_t bufferLength,
    _Out_opt_ uint32_t* writtenOrNeeded)
{
    assert(str != nullptr);

#ifdef BUILD_WINDOWS
    int32_t result = ::WideCharToMultiByte(CP_UTF8, 0, str, -1, buffer, bufferLength, nullptr, nullptr);
    if (result <= 0)
    {
        if (::GetLastError() == ERROR_INSUFFICIENT_BUFFER)
        {
            if (writtenOrNeeded != nullptr)
                *writtenOrNeeded = ::WideCharToMultiByte(CP_UTF8, 0, str, -1, nullptr, 0, nullptr, nullptr);
            return E_NOT_SUFFICIENT_BUFFER;
        }
        return E_FAIL;
    }
    if (writtenOrNeeded != nullptr)
        *writtenOrNeeded = (uint32_t)result;
    return S_OK;
#else
#error Missing implementation
#endif // !BUILD_WINDOWS
}

HRESULT pal::ConvertUtf8ToUtf16(
    char const* str,
    WCHAR* buffer,
    uint32_t bufferLength,
    _Out_opt_ uint32_t* writtenOrNeeded)
{
    assert(str != nullptr);

#ifdef BUILD_WINDOWS
    int32_t result = ::MultiByteToWideChar(CP_UTF8, 0, str, -1, buffer, bufferLength);
    if (result <= 0)
    {
        if (::GetLastError() == ERROR_INSUFFICIENT_BUFFER)
        {
            if (writtenOrNeeded != nullptr)
                *writtenOrNeeded = ::MultiByteToWideChar(CP_UTF8, 0, str, -1, nullptr, 0);
            return E_NOT_SUFFICIENT_BUFFER;
        }
        return E_FAIL;
    }
    if (writtenOrNeeded != nullptr)
        *writtenOrNeeded = (uint32_t)result;
    return S_OK;
#else
#error Missing implementation
#endif // !BUILD_WINDOWS
}

HRESULT pal::StringConvert<WCHAR, char>::ConvertWorker(WCHAR const* c, char* buffer, uint32_t& bufferLength)
{
    return ConvertUtf16ToUtf8(c, buffer, bufferLength, &bufferLength);
}

HRESULT pal::StringConvert<char, WCHAR>::ConvertWorker(char const* c, WCHAR* buffer, uint32_t& bufferLength)
{
    return ConvertUtf8ToUtf16(c, buffer, bufferLength, &bufferLength);
}
