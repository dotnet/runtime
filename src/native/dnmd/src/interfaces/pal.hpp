#ifndef _SRC_INTERFACES_PAL_HPP_
#define _SRC_INTERFACES_PAL_HPP_

#include <cstddef>
#include <cstdint>
#include <array>
#include <internal/dnmd_platform.hpp>
#include <internal/span.hpp>

namespace pal
{
    // Convert the UTF-16 string into UTF-8
    // Buffer length should include null terminator.
    // Written length includes null terminator.
    HRESULT ConvertUtf16ToUtf8(
        WCHAR const* str,
        char* buffer,
        uint32_t bufferLength,
        _Out_opt_ uint32_t* writtenOrNeeded);

    // Convert the UTF-8 string into UTF-16
    // Buffer length should include null terminator.
    // Written length includes null terminator.
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
        malloc_ptr<void> _owner;
        uint32_t _charLength;
        bool _converted;
        HRESULT ConvertWorker(A const* c, B* buffer, uint32_t& bufferLength);

    public:
        StringConvert(A const* c, B* buffer, uint32_t bufferLength) noexcept
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

        explicit StringConvert(A const* c) noexcept
            : StringConvert(c, nullptr, 0)
        { }

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

    constexpr size_t SHA1_HASH_SIZE = 20;

    bool ComputeSha1Hash(span<uint8_t const> data, std::array<uint8_t, SHA1_HASH_SIZE>& hashDestination);
    
    // A simple read-write lock that provides accessors to meet the C++11 BasicLockable requirements.
    class ReadWriteLock;

    class ReadLock final
    {
        ReadWriteLock& _lock;
        public:
            ReadLock(ReadWriteLock& lock) noexcept;
            ReadLock(ReadLock const&) = delete;
            ReadLock(ReadLock&&) = delete;
            void lock() noexcept;
            void unlock() noexcept;
    };

    class WriteLock final
    {
        ReadWriteLock& _lock;
        public:
            WriteLock(ReadWriteLock& lock) noexcept;
            WriteLock(WriteLock const&) = delete;
            WriteLock(WriteLock&&) = delete;
            void lock() noexcept;
            void unlock() noexcept;
    };

    class ReadWriteLock
    {
        friend class ReadLock;
        friend class WriteLock;
        class Impl;
        std::unique_ptr<Impl> _impl;
        ReadLock _readLock;
        WriteLock _writeLock;
    public:
        ReadWriteLock();
        ~ReadWriteLock();
        ReadLock& GetReadLock() noexcept
        {
            return _readLock;
        }
        WriteLock& GetWriteLock() noexcept
        {
            return _writeLock;
        }
    };
}

// Implementations for missing bounds checking APIs.
// See https://en.cppreference.com/w/c/error#Bounds_checking
#if !defined(__STDC_LIB_EXT1__) && !defined(BUILD_WINDOWS)
using rsize_t = size_t;
int strcat_s(char* dest, rsize_t destsz, char const* src);
#endif // !defined(__STDC_LIB_EXT1__) && !defined(BUILD_WINDOWS)

#endif // _SRC_INTERFACES_PAL_HPP_