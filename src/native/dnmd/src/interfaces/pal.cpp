#include "pal.hpp"
#include <cstring>
#include <cassert>
#include <exception>
#include <functional>
#include <limits>
#include <new>
#include <minipal/rwlock.h>
#include <minipal/utf8.h>
#include <minipal/sha1.h>
#include <minipal/strings.h>

// String conversion functions
HRESULT pal::ConvertUtf16ToUtf8(
    WCHAR const* str,
    char* buffer,
    uint32_t bufferLength,
    _Out_opt_ uint32_t* writtenOrNeeded)
{
    assert(str != nullptr);
    size_t length = minipal_u16_strlen((CHAR16_T*)str) + 1;

    size_t requiredBufferLength = minipal_get_length_utf16_to_utf8((CHAR16_T*)str, length, 0);

    if (requiredBufferLength > (size_t)std::numeric_limits<int>::max())
    {
        return E_FAIL;
    }

    if (requiredBufferLength > bufferLength)
    {
        if (writtenOrNeeded != nullptr)
        {
            *writtenOrNeeded = (uint32_t)requiredBufferLength;
        }
        if (bufferLength == 0)
        {
            return S_OK;
        }
        return E_NOT_SUFFICIENT_BUFFER;
    }

    size_t written = minipal_convert_utf16_to_utf8((CHAR16_T*)str, length, buffer, bufferLength, 0);
    if (written >= 0)
    {
        *writtenOrNeeded = (uint32_t)written;
        return S_OK;
    }
    return E_FAIL;
}

HRESULT pal::ConvertUtf8ToUtf16(
    char const* str,
    WCHAR* buffer,
    uint32_t bufferLength,
    _Out_opt_ uint32_t* writtenOrNeeded)
{
    assert(str != nullptr);
    size_t length = strlen(str) + 1;

    size_t requiredBufferLength = minipal_get_length_utf8_to_utf16(str, length, 0);

    if (requiredBufferLength > (size_t)std::numeric_limits<int>::max())
    {
        return E_FAIL;
    }

    if (requiredBufferLength > bufferLength)
    {
        if (writtenOrNeeded != nullptr)
        {
            *writtenOrNeeded = (uint32_t)requiredBufferLength;
        }
        if (bufferLength == 0)
        {
            return S_OK;
        }
        return E_NOT_SUFFICIENT_BUFFER;
    }

    size_t written = minipal_convert_utf8_to_utf16(str, length, (CHAR16_T*)buffer, bufferLength, 0);
    if (written >= 0)
    {
        *writtenOrNeeded = (uint32_t)written;
        return S_OK;
    }
    return E_FAIL;
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

bool pal::ComputeSha1Hash(span<uint8_t const> data, std::array<uint8_t, SHA1_HASH_SIZE>& hashDestination)
{
    minipal_sha1(data.data(), data.size(), hashDestination.data(), SHA1_HASH_SIZE);
    return true;
}

// Read-write lock implementation
// The implementation type matches the C++11 BasicLockable and the C++14 SharedLockable requirements (excluding the try_lock_shared method).
// This allows us to move to exposing the C++14 API surface in the future more easily.
namespace pal
{
    class ReadWriteLock::Impl final
    {
        minipal_rwlock _lock{};
    public:
        Impl()
        {
            if (!minipal_rwlock_init(&_lock))
                throw std::bad_alloc();
        }

        ~Impl()
        {
            minipal_rwlock_destroy(&_lock);
        }

        minipal_rwlock* NativeHandle() noexcept
        {
            return &_lock;
        }

        // BasicLockable cannot report acquisition failure; never continue without the lock.
        void lock_shared() noexcept
        {
            if (!minipal_rwlock_enter_read(&_lock))
                std::terminate();
        }

        void unlock_shared() noexcept
        {
            minipal_rwlock_leave_read(&_lock);
        }

        void lock() noexcept
        {
            if (!minipal_rwlock_enter_write(&_lock))
                std::terminate();
        }

        void unlock() noexcept
        {
            minipal_rwlock_leave_write(&_lock);
        }
    };
}

pal::ReadWriteLock::ReadWriteLock()
    : _impl{ std::make_unique<Impl>() }
    , _readLock{ *this }
    , _writeLock{ *this }
{
}

// Define here where pal::ReadWriteLock::Impl is defined
pal::ReadWriteLock::~ReadWriteLock() = default;

minipal_rwlock* pal::ReadWriteLock::NativeHandle() noexcept
{
    return _impl->NativeHandle();
}

pal::ReadLock::ReadLock(pal::ReadWriteLock& lock) noexcept
    : _lock{ lock }
{
}

void pal::ReadLock::lock() noexcept
{
    _lock._impl->lock_shared();
}

void pal::ReadLock::unlock() noexcept
{
    _lock._impl->unlock_shared();
}

pal::WriteLock::WriteLock(pal::ReadWriteLock& lock) noexcept
    : _lock{ lock }
{
}

void pal::WriteLock::lock() noexcept
{
    _lock._impl->lock();
}

void pal::WriteLock::unlock() noexcept
{
    _lock._impl->unlock();
}
