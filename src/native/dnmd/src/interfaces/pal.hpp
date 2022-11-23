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
        char* buffer,
        uint32_t bufferLength,
        _Out_opt_ uint32_t* writtenOrNeeded);

    // Convert the UTF-8 string into UTF-16
    HRESULT ConvertUtf8ToUtf16(
        char const* str,
        WCHAR* buffer,
        uint32_t bufferLength,
        _Out_opt_ uint32_t* writtenOrNeeded);

    // Template class for conversion UTF-8 <=> UTF-16
    template<typename A, typename B>
    class StringConvert
    {
        B* _ptr;
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
            uint32_t neededLength = bufferLength;
            // Compute needed size for conversion and/or rely on the user supplied buffer.
            HRESULT hr = ConvertWorker(c, buffer, neededLength);
            if (hr == S_OK)
            {
                if (bufferLength < neededLength)
                {
                    buffer = (B*)::malloc(sizeof(*buffer) * neededLength);
                    _owner.reset(buffer);
                    bufferLength = neededLength;
                }

                // Do conversion
                hr = ConvertWorker(c, buffer, bufferLength);
                _converted = SUCCEEDED(hr);
                if (_converted)
                {
                    _ptr = buffer;
                    _charLength = bufferLength;
                }
            }
            else if (neededLength != bufferLength)
            {
                // Failed to convert. If the needed length was updated
                // then set that so the caller can possibly use it.
                _charLength = neededLength;
            }
        }

        template<int32_t N>
        StringConvert(A const* c, B(&buffer)[N]) noexcept
            : StringConvert(c, buffer, N)
        { }

        StringConvert(StringConvert const&) = delete;
        StringConvert(StringConvert&&) = delete;

        ~StringConvert() noexcept = default;

        StringConvert& operator=(StringConvert const&) = delete;
        StringConvert& operator=(StringConvert&&) = delete;

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

        operator B*() noexcept
        {
            return _ptr;
        }
    };
}

#endif // _SRC_INTERFACES_PAL_HPP_