// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef __HOLDER_H_
#define __HOLDER_H_

#include "cor.h"
#include "contract.h"
#include "volatile.h"
#include "palclr.h"
#include <minipal/com/memory.h>

#include <utility>
#include <type_traits>
#include <memory>

template <void (*ACQUIRE)(), void (*RELEASEF)()>
class StateHolder final
{
  private:
    BOOL    m_acquired;      // Have we acquired the state?

  public:
    FORCEINLINE StateHolder(BOOL take = TRUE)
      : m_acquired(FALSE)
    {
        STATIC_CONTRACT_WRAPPER;
        if (take)
            Acquire();
    }
    FORCEINLINE ~StateHolder()
    {
        STATIC_CONTRACT_WRAPPER;
        Release();
    }
    FORCEINLINE void Acquire()
    {
        STATIC_CONTRACT_WRAPPER;
        // Insert any global or thread bookkeeping here

        _ASSERTE(!m_acquired);

        ACQUIRE();
        m_acquired = TRUE;
    }
    FORCEINLINE void Release()
    {
        STATIC_CONTRACT_WRAPPER;
        // Insert any global or thread bookkeeping here

        if (m_acquired)
        {
            RELEASEF();
            m_acquired = FALSE;
        }
    }
    FORCEINLINE void SuppressRelease()
    {
        STATIC_CONTRACT_LEAF;
        m_acquired = FALSE;
    }

    StateHolder(const StateHolder&) = delete;
    StateHolder& operator=(const StateHolder&) = delete;
};  // class StateHolder<>

//-----------------------------------------------------------------------------
// No-op acquire/release callback for StateHolder
//-----------------------------------------------------------------------------

FORCEINLINE void DoNothing()
{
}

//-----------------------------------------------------------------------------
// Holders
//-----------------------------------------------------------------------------

template<typename T>
class LifetimeHolder final
{
    using Type = typename T::Type;
    Type m_value;

public:
    LifetimeHolder() : m_value{ T::Default() } { STATIC_CONTRACT_LEAF; }
    explicit LifetimeHolder(Type value) : m_value{ std::move(value) } { STATIC_CONTRACT_LEAF; }

    LifetimeHolder(const LifetimeHolder&) = delete;
    LifetimeHolder& operator=(const LifetimeHolder&) = delete;

    LifetimeHolder(LifetimeHolder&& other)
        : m_value{ other.Detach() }
    {
        STATIC_CONTRACT_WRAPPER;
    }

    LifetimeHolder& operator=(LifetimeHolder&& other)
    {
        STATIC_CONTRACT_WRAPPER;
        if (this != std::addressof(other))
        {
            Free();
            m_value = other.Detach();
        }
        return *this;
    }

    ~LifetimeHolder() noexcept { STATIC_CONTRACT_WRAPPER; Free(); }

    LifetimeHolder& operator=(Type value)
    {
        STATIC_CONTRACT_WRAPPER;
        Free();
        m_value = std::move(value);
        return *this;
    }

    operator Type() const { STATIC_CONTRACT_LEAF; return m_value; }

    Type* operator&()
    {
        STATIC_CONTRACT_LEAF;
#ifdef _DEBUG
        Type tmp = T::Default();
        _ASSERTE(memcmp(&m_value, &tmp, sizeof(Type)) == 0 && "Taking address of non-empty holder");
#endif // _DEBUG
        return &m_value;
    }

    template <typename U = Type, typename = std::enable_if_t<std::is_pointer<U>::value>>
    U operator->() const
    {
        STATIC_CONTRACT_LEAF;
        _ASSERTE(m_value != NULL);
        return m_value;
    }

    void Free()
    {
        STATIC_CONTRACT_WRAPPER;
        T::Free(m_value);
        m_value = T::Default();
    }

    Type Detach()
    {
        STATIC_CONTRACT_WRAPPER;
        return std::exchange(m_value, T::Default());
    }
};

template <typename TYPE>
struct NewHolderTraits final
{
    using Type = TYPE*;
    static constexpr Type Default() { return NULL; }
    static void Free(Type value) noexcept
    {
        STATIC_CONTRACT_WRAPPER;
        static_assert(!std::is_same<typename std::remove_cv<TYPE>::type, WCHAR>::value,
                    "Use WStringHolder (not NewHolder) for strings.");
        static_assert(!std::is_same<typename std::remove_cv<TYPE>::type, CHAR>::value,
                    "Use AStringHolder (not NewHolder) for strings.");

        delete value;
    }
};

template <typename TYPE>
using NewHolder = LifetimeHolder<NewHolderTraits<TYPE>>;

template <typename TYPE>
struct NewArrayHolderTraits final
{
    using Type = TYPE*;
    static constexpr Type Default() { return NULL; }
    static void Free(Type value) noexcept
    {
        STATIC_CONTRACT_WRAPPER;
        delete[] value;
    }
};

template <typename TYPE>
using NewArrayHolder = LifetimeHolder<NewArrayHolderTraits<TYPE>>;

using AStringHolder = NewArrayHolder<CHAR>;
using WStringHolder = NewArrayHolder<WCHAR>;

//-----------------------------------------------------------------------------
// A special array holder that expects its contents are interface pointers,
// and will call Release() on them.
//
// NOTE: You may ONLY use this if you've determined that it is SAFE to call
// Release() on the contained interface pointers (e.g., as opposed to SafeRelease)
//
template <typename INTERFACE>
class NewInterfaceArrayHolder final
{
    struct Traits
    {
        struct Type
        {
            INTERFACE ** values;
            ULONG32 count;
        };

        static Type Default() { return {}; }

        static void Free(Type array) noexcept
        {
            STATIC_CONTRACT_LEAF;
            if (array.values != NULL)
            {
                for (ULONG32 i = 0; i < array.count; i++)
                {
                    if (array.values[i] != NULL)
                        array.values[i]->Release();
                }
                delete[] array.values;
            }
        }
    };

    LifetimeHolder<Traits> m_array;

public:
    NewInterfaceArrayHolder() = default;

    NewInterfaceArrayHolder(INTERFACE ** value, ULONG32 cElements) :
        m_array(typename Traits::Type{ value, cElements })
    {
        STATIC_CONTRACT_WRAPPER;
    }

    operator INTERFACE **() const
    {
        STATIC_CONTRACT_LEAF;
        return static_cast<typename Traits::Type>(m_array).values;
    }

    INTERFACE ** Detach()
    {
        STATIC_CONTRACT_WRAPPER;
        return m_array.Detach().values;
    }
};

template <typename TYPE>
struct ReleaseHolderTraits final
{
    using Type = TYPE*;
    static constexpr Type Default() { return NULL; }
    static void Free(Type value) noexcept
    {
        CONTRACTL
        {
            NOTHROW;
            GC_TRIGGERS;
            MODE_PREEMPTIVE;
        }
        CONTRACTL_END;

        if (value != NULL)
            value->Release();
    }
};

template<typename _TYPE>
using ReleaseHolder = LifetimeHolder<ReleaseHolderTraits<_TYPE>>;

//-----------------------------------------------------------------------------
// Wrap win32 functions using HANDLE
//-----------------------------------------------------------------------------
struct HandleTraits final
{
    using Type = HANDLE;
    static Type Default() { return INVALID_HANDLE_VALUE; }
    static void Free(Type h) noexcept
    {
        STATIC_CONTRACT_WRAPPER;
        if (h != NULL && h != Default())
            CloseHandle(h);
    }
};
using HandleHolder = LifetimeHolder<HandleTraits>;

#ifdef HOST_WINDOWS
struct MapViewTraits final
{
    using Type = void*;
    static constexpr Type Default() { return NULL; }
    static void Free(Type h) noexcept
    {
        STATIC_CONTRACT_WRAPPER;
        if (h != NULL)
            ::UnmapViewOfFile(h);
    }
};

using MapViewHolder = LifetimeHolder<MapViewTraits>;
#endif // HOST_WINDOWS

// A holder for HMODULE.
struct HModuleTraits final
{
    using Type = HMODULE;
    static constexpr Type Default() { return NULL; }
    static void Free(Type h) noexcept
    {
        STATIC_CONTRACT_WRAPPER;
        if (h != NULL)
            ::FreeLibrary(h);
    }
};

using HModuleHolder = LifetimeHolder<HModuleTraits>;

//-----------------------------------------------------------------------------
// ResetPointerHolder : pointer which needs to be set to NULL (or zeroed) on
// scope exit. The holder stores the address of the slot to clear; on Free it
// nulls the pointer (for pointer T) or zeroes the storage (for non-pointer T).
//
//  {
//      ResetPointerHolder<Foo*> holder{ &pFoo };
//  } // "pFoo = NULL" on out of scope
//-----------------------------------------------------------------------------
template<typename T>
struct ResetPointerTraits final
{
    using Type = T*;
    static constexpr Type Default() { return NULL; }
    static void Free(Type p) noexcept
    {
        STATIC_CONTRACT_WRAPPER;
        if (p == NULL)
            return;

        // If T is a pointer type, we set the pointer to NULL. If T is a non-pointer type, we zero the memory.
        if constexpr (std::is_pointer<T>::value)
            *p = NULL;
        else
            ZeroMemory(p, sizeof(T));
    }
};

template<typename T>
using ResetPointerHolder = LifetimeHolder<ResetPointerTraits<T>>;

//-----------------------------------------------------------------------------
// CoTaskMemHolder : holder for memory allocated by ::CoTaskMemAlloc.
//
//  {
//      CoTaskMemHolder<Foo> foo{ (Foo*)::CoTaskMemAlloc(sizeof(Foo)) };
//  } // ::CoTaskMemFree(foo) on out of scope
//-----------------------------------------------------------------------------
template<typename T>
struct CoTaskMemTraits final
{
    using Type = T*;
    static constexpr Type Default() { return NULL; }
    static void Free(Type value) noexcept
    {
        STATIC_CONTRACT_WRAPPER;
        if (value != NULL)
            ::CoTaskMemFree(value);
    }
};

template<typename T>
using CoTaskMemHolder = LifetimeHolder<CoTaskMemTraits<T>>;

class CounterHolder final
{
    // Accept both raw counter pointers and the result of Volatile<T>::operator&.
    struct Traits final
    {
        using Type = RAW_KEYWORD(volatile) LONG*;
        static constexpr Type Default() { return NULL; }
        static void Free(Type value) noexcept
        {
            STATIC_CONTRACT_WRAPPER;
            if (value != NULL)
                InterlockedDecrement(value);
        }
    };

    LifetimeHolder<Traits> m_counter;

public:
    explicit CounterHolder(RAW_KEYWORD(volatile) LONG* counter)
        : m_counter(counter)
    {
        STATIC_CONTRACT_WRAPPER;
        _ASSERTE(counter != NULL);
        InterlockedIncrement(counter);
    }

    CounterHolder(const CounterHolder&) = delete;
    CounterHolder& operator=(const CounterHolder&) = delete;

    void Release()
    {
        STATIC_CONTRACT_WRAPPER;
        m_counter.Free();
    }
};

class ErrorModeHolder final
{
#ifdef HOST_WINDOWS
    BOOL m_revert;
    DWORD m_oldMode;
public:
    ErrorModeHolder()
        : m_revert{ FALSE }
    {
        DWORD newMode = SEM_FAILCRITICALERRORS;
        m_revert = ::SetThreadErrorMode(newMode, &m_oldMode);
    }
    ~ErrorModeHolder() noexcept
    {
        if (m_revert != FALSE)
            (void)::SetThreadErrorMode(m_oldMode, NULL);
    }
#endif // HOST_WINDOWS
};

#ifdef HOST_WINDOWS
//-----------------------------------------------------------------------------
// HKEYHolder : HKEY holder, Calls RegCloseKey on scope exit.
//
//  {
//      HKEYHolder hFoo;
//      RegOpenKeyEx(HKEY_CLASSES_ROOT, L"Interface",0, KEY_READ, &hFoo);
//
//  } // close key on out of scope via RegCloseKey.
//-----------------------------------------------------------------------------
struct HKEYTraits final
{
    using Type = HKEY;
    static constexpr Type Default() { return NULL; }
    static void Free(Type h) noexcept
    {
        STATIC_CONTRACT_WRAPPER;
        if (h != NULL)
            ::RegCloseKey(h);
    }
};

using HKEYHolder = LifetimeHolder<HKEYTraits>;
#endif // HOST_WINDOWS

#ifdef FEATURE_COMINTEROP
struct BSTRTraits final
{
    using Type = BSTR;
    static constexpr Type Default() { return NULL; }
    static void Free(Type h) noexcept
    {
        STATIC_CONTRACT_WRAPPER;
        if (h != NULL)
            ::SysFreeString(h);
    }
};

using BSTRHolder = LifetimeHolder<BSTRTraits>;
#endif // FEATURE_COMINTEROP

#endif  // __HOLDER_H_
