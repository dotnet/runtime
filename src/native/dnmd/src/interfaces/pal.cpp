#include "pal.hpp"
#include <cstring>
#include <cassert>
#include <functional>
#include <limits>
#include <minipal/utf8.h>

#if defined(BUILD_WINDOWS)
#include <bcrypt.h>
#elif defined(BUILD_MACOS)
#include <CommonCrypto/CommonDigest.h>
#elif defined(BUILD_UNIX) && !defined(FEATURE_DISTRO_AGNOSTIC_SSL)
#include <openssl/sha.h>
#elif defined(BUILD_UNIX) && defined(FEATURE_DISTRO_AGNOSTIC_SSL)
#include <dlfcn.h>
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
    size_t length = PAL_wcslen(str) + 1;

    size_t requiredBufferLength = minipal_get_length_utf16_to_utf8((CHAR16_T*)str, length, 0);

    if (requiredBufferLength > std::numeric_limits<int>::max())
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

    if (requiredBufferLength > std::numeric_limits<int>::max())
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

#elif defined(BUILD_UNIX) && !defined(FEATURE_DISTRO_AGNOSTIC_SSL)
#include <openssl/sha.h>
bool pal::ComputeSha1Hash(span<const uint8_t> data, std::array<uint8_t, SHA1_HASH_SIZE>& hashDestination)
{
    static_assert(SHA_DIGEST_LENGTH == SHA1_HASH_SIZE, "SHA1 hash size mismatch");
    SHA1(data, data.size(), hashDestination.data());
    return true;
}
#elif defined(BUILD_UNIX) && defined(FEATURE_DISTRO_AGNOSTIC_SSL)
namespace
{
    struct sha1_func
    {
        void (*func)(const uint8_t*, size_t, uint8_t*);
        sha1_func()
        {
            void* img_handle = dlopen("libcrypto.so", RTLD_LAZY);
            if (img_handle == nullptr)
            {
                img_handle = dlopen("libcrypto.so.3", RTLD_LAZY);
            }
            if (img_handle == nullptr)
            {
                img_handle = dlopen("libcrypto.so.1.1", RTLD_LAZY);
            }
            if (img_handle == nullptr)
            {
                img_handle = dlopen("libcrypto.so.1.0.0", RTLD_LAZY);
            }

            void* func_ptr = dlsym(img_handle, "SHA1");
            func = reinterpret_cast<void (*)(const uint8_t*, size_t, uint8_t*)>(func_ptr);
        }

        void operator()(const uint8_t* data, size_t len, uint8_t* hash)
        {
            func(data, len, hash);
        }
    };
}

bool pal::ComputeSha1Hash(span<const uint8_t> data, std::array<uint8_t, SHA1_HASH_SIZE>& hashDestination)
{
    static sha1_func sha1;
    sha1(data, data.size(), hashDestination.data());
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
