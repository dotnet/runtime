#include "pal.hpp"
#include <cstring>
#include <cassert>
#include <functional>
#include <limits>
#include <minipal/utf8.h>
#include <minipal/sha1.h>
#include <minipal/strings.h>

#if defined(BUILD_WINDOWS)
#include <windows.h>
#else
#include <pthread.h>
#endif

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
    minipal_sha1(data, data.size(), hashDestination.data(), SHA1_HASH_SIZE);
    return true;
}

// Read-write lock implementation
// The implementation type matches the C++11 BasicLockable and the C++14 SharedLockable requirements (excluding the try_lock_shared method).
// This allows us to move to exposing the C++14 API surface in the future more easily.
#if defined(BUILD_WINDOWS)
namespace pal
{
    class ReadWriteLock::Impl final
    {
        SRWLOCK _lock;
    public:
        Impl()
        {
            ::InitializeSRWLock(&_lock);
        }

        void lock_shared() noexcept
        {
            ::AcquireSRWLockShared(&_lock);
        }

        void unlock_shared() noexcept
        {
            ::ReleaseSRWLockShared(&_lock);
        }

        void lock() noexcept
        {
            ::AcquireSRWLockExclusive(&_lock);
        }

        void unlock() noexcept
        {
            ::ReleaseSRWLockExclusive(&_lock);
        }
    };
}
#else
namespace pal
{
    class ReadWriteLock::Impl final
    {
        pthread_rwlock_t _lock;
    public:
        Impl()
        {
            ::pthread_rwlock_init(&_lock, nullptr);
        }

        void lock_shared() noexcept
        {
            ::pthread_rwlock_rdlock(&_lock);
        }

        void unlock_shared() noexcept
        {
            ::pthread_rwlock_unlock(&_lock);
        }

        void lock() noexcept
        {
            ::pthread_rwlock_wrlock(&_lock);
        }

        void unlock() noexcept
        {
            ::pthread_rwlock_unlock(&_lock);
        }
    };
}
#endif

pal::ReadWriteLock::ReadWriteLock()
    : _impl{ std::make_unique<Impl>() }
    , _readLock{ *this }
    , _writeLock{ *this }
{
}

// Define here where pal::ReadWriteLock::Impl is defined
pal::ReadWriteLock::~ReadWriteLock() = default;

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
