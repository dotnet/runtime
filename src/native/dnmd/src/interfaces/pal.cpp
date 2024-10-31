#include "pal.hpp"
#include <cstring>
#include <cassert>
#include <functional>

#if defined(BUILD_MACOS) || defined(BUILD_UNIX)
#include <unicode/ustring.h>
#endif

#if defined(BUILD_WINDOWS)
#include <bcrypt.h>
#elif defined(BUILD_MACOS)
#include <CommonCrypto/CommonDigest.h>
#elif defined(BUILD_UNIX)
#include <openssl/sha.h>
#endif

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

// SHA1 implementation
#if defined(BUILD_WINDOWS)
namespace
{
    struct BCRYPT_ALG_HANDLE_deleter final
    {
        void operator()(BCRYPT_ALG_HANDLE h) const noexcept
        {
            ::BCryptCloseAlgorithmProvider(h, 0);
        }
    };

    using bcrypt_alg_handle = std::unique_ptr<void, BCRYPT_ALG_HANDLE_deleter>;

    struct BCRYPT_HASH_HANDLE_deleter final
    {
        void operator()(BCRYPT_HASH_HANDLE h) const noexcept
        {
            ::BCryptDestroyHash(h);
        }
    };

    using bcrypt_hash_handle = std::unique_ptr<void, BCRYPT_HASH_HANDLE_deleter>;
}

bool pal::ComputeSha1Hash(span<const uint8_t> data, std::array<uint8_t, SHA1_HASH_SIZE>& hashDestination)
{
    BCRYPT_ALG_HANDLE hAlg;
    if (!BCRYPT_SUCCESS(::BCryptOpenAlgorithmProvider(&hAlg, BCRYPT_SHA1_ALGORITHM, nullptr, 0)))
    {
        return false;
    }
    bcrypt_alg_handle algHandle { hAlg };

    BCRYPT_HASH_HANDLE hHash;
    if (!BCRYPT_SUCCESS(::BCryptCreateHash(hAlg, &hHash, nullptr, 0, nullptr, 0, 0)))
    {
        return false;
    }
    bcrypt_hash_handle hashHandle { hHash };
    if (!BCRYPT_SUCCESS(::BCryptHashData(hHash, (PUCHAR)(uint8_t const*)data, (ULONG)data.size(), 0)))
    {
        return false;
    }

    return BCRYPT_SUCCESS(::BCryptFinishHash(hHash, hashDestination.data(), (ULONG)hashDestination.size(), 0));
}
#elif defined(BUILD_MACOS)
#include <CommonCrypto/CommonDigest.h>

bool pal::ComputeSha1Hash(span<const uint8_t> data, std::array<uint8_t, SHA1_HASH_SIZE>& hashDestination)
{
    static_assert(CC_SHA1_DIGEST_LENGTH == SHA1_HASH_SIZE, "SHA1 hash size mismatch");
    CC_SHA1(data, data.size(), hashDestination.data());
    return true;
}

#elif defined(BUILD_UNIX)
#include <openssl/sha.h>
bool pal::ComputeSha1Hash(span<const uint8_t> data, std::array<uint8_t, SHA1_HASH_SIZE>& hashDestination)
{
    static_assert(SHA_DIGEST_LENGTH == SHA1_HASH_SIZE, "SHA1 hash size mismatch");
    SHA1(data, data.size(), hashDestination.data());
    return true;
}

#endif // defined(BUILD_WINDOWS)

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
