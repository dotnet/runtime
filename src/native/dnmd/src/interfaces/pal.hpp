#ifndef _SRC_INTERFACES_PAL_HPP_
#define _SRC_INTERFACES_PAL_HPP_

#include <cstdint>
#include <platform.h>
#include <dnmd.hpp>

namespace pal
{
    // Convert the UTF-16 string into UTF-8
    HRESULT ConvertUtf16ToUtf8(
        WCHAR const* str,
        int32_t strLength,
        char* buffer,
        _Inout_ uint32_t* bufferLength);

    // Convert the UTF-8 string into UTF-16
    HRESULT ConvertUtf8ToUtf16(
        char const* str,
        int32_t strLength,
        WCHAR* buffer,
        _Inout_ uint32_t* bufferLength);

    // Template class for conversion UTF-8 <=> UTF-16
    template<typename A, typename B>
    class StringConvert
    {
        B const* _ptr;
        malloc_ptr _owner;
        uint32_t _charLength;
        bool _converted;
        HRESULT ConvertWorker(A const* c, B* buffer, uint32_t& bufferLength);

    public:
        StringConvert(A const* c, B* buffer = nullptr, uint32_t bufferLength = 0) noexcept
            : _owner{}
            , _charLength{}
            , _converted{}
        {
            bool allocd = false;
            uint32_t neededLength = 0;
            // Compute needed size
            HRESULT hr = ConvertWorker(c, nullptr, neededLength);
            if (hr == S_OK)
            {
                if (bufferLength < neededLength)
                {
                    buffer = (B*)::malloc(sizeof(*buffer) * neededLength);
                    bufferLength = neededLength;
                    allocd = true;
                }

                // Do real conversion
                hr = ConvertWorker(c, buffer, bufferLength);
                _converted = SUCCEEDED(hr);
                if (_converted)
                {
                    _ptr = buffer;
                    _charLength = bufferLength;
                    if (allocd)
                        _owner.reset(buffer);
                }
            }
        }

        template<int32_t N>
        StringConvert(A const* c, B(&buffer)[N]) noexcept
            : StringConvert(c, buffer, N)
        { }

        ~StringConvert() = default;

        bool Success() const noexcept
        {
            return _converted;
        }

        uint32_t Length() const noexcept
        {
            return _charLength;
        }

        operator B const*() const noexcept
        {
            return _ptr;
        }

        bool CopyTo(B* buffer, uint32_t bufferLength, uint32_t* writtenLength) noexcept
        {
            if (!Success())
                return false;

            // Copy the converted string to the buffer.
            if (bufferLength < _charLength)
            {
                *writtenLength = bufferLength;
                ::memcpy(buffer, _ptr, bufferLength * sizeof(*buffer));
                ::memset(&buffer[bufferLength - 1], 0, sizeof(*buffer));
            }
            else
            {
                *writtenLength = _charLength;
                ::memcpy(buffer, _ptr, _charLength * sizeof(*buffer));
            }
            return true;
        }
    };
}

#endif // _SRC_INTERFACES_PAL_HPP_