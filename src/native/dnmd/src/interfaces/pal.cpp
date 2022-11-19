#include <assert.h>
#include "pal.hpp"

HRESULT pal::ConvertUtf16ToUtf8(
    WCHAR const* str,
    int32_t strLength,
    char* buffer,
    _Inout_ uint32_t* bufferLength)
{
    assert(str != nullptr && bufferLength != nullptr);

#ifdef BUILD_WINDOWS
    int32_t result = ::WideCharToMultiByte(CP_UTF8, 0, str, strLength, buffer, *bufferLength, nullptr, nullptr);
    if (result <= 0)
    {
        return ::GetLastError() == ERROR_INSUFFICIENT_BUFFER
            ? E_NOT_SUFFICIENT_BUFFER
            : E_FAIL;
    }
    *bufferLength = (uint32_t)result;
    return S_OK;
#else
#error Missing implementation
#endif // !BUILD_WINDOWS
}

HRESULT pal::ConvertUtf8ToUtf16(
    char const* str,
    int32_t strLength,
    WCHAR* buffer,
    _Inout_ uint32_t* bufferLength)
{
    assert(str != nullptr && bufferLength != nullptr);

#ifdef BUILD_WINDOWS
    int32_t result = ::MultiByteToWideChar(CP_UTF8, 0, str, strLength, buffer, *bufferLength);
    if (result <= 0)
    {
        return ::GetLastError() == ERROR_INSUFFICIENT_BUFFER
            ? E_NOT_SUFFICIENT_BUFFER
            : E_FAIL;
    }
    *bufferLength = (uint32_t)result;
    return S_OK;
#else
#error Missing implementation
#endif // !BUILD_WINDOWS
}

HRESULT pal::StringConvert<WCHAR, char>::ConvertWorker(WCHAR const* c, char* buffer, uint32_t& bufferLength)
{
    return ConvertUtf16ToUtf8(c, -1, buffer, &bufferLength);
}

HRESULT pal::StringConvert<char, WCHAR>::ConvertWorker(char const* c, WCHAR* buffer, uint32_t& bufferLength)
{
    return ConvertUtf8ToUtf16(c, -1, buffer, &bufferLength);
}
