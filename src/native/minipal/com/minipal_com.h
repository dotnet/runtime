// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef MINIPAL_COM_H
#define MINIPAL_COM_H

#include "comtypes.h"
#include "memory.h"
#include "../guid.h"

#ifdef __cplusplus
    extern "C"
    {
#endif // __cplusplus

//
// Strings
//

// This macro is used to standardize wide character string literals used in .NET.
#ifdef MINIPAL_COM_WINDOWS
    #define W(str)  L ## str
#else
    #define W(str)  u ## str
#endif

//
// GUIDs
//

#ifdef __cplusplus
    #ifdef MINIPAL_COM_WINHDRS
    inline bool operator==(REFGUID a, REFGUID b)
    {
        minipal_guid_t const& ga = reinterpret_cast<minipal_guid_t const&>(a);
        minipal_guid_t const& gb = reinterpret_cast<minipal_guid_t const&>(b);
        return ga == gb;
    }

    inline bool operator!=(REFGUID a, REFGUID b)
    {
        return !(a == b);
    }
    #endif // MINIPAL_COM_WINHDRS
    }
#endif // __cplusplus

#ifdef __cplusplus
    #include <memory>
    #include <type_traits>
    namespace minipal
    {
        // Smart pointer for use with IUnknown based interfaces.
        // It is based off of ATL::CComPtr<T> so adoption is easier.
        template<typename T>
        class com_ptr
        {
        public:
            T* p;

        public:
            com_ptr() : p{} {}

            com_ptr(T* t)
                : p{ t }
            {
                if (p != nullptr)
                    (void)p->AddRef();
            }

            com_ptr(com_ptr const&) = delete;

            com_ptr(com_ptr&& other)
                : p{ other.Detach() }
            { }

            ~com_ptr() { Release(); }

            com_ptr& operator=(com_ptr const&) = delete;

            com_ptr& operator=(com_ptr&& other)
            {
                Attach(other.Detach());
                return (*this);
            }

            operator T*() { return p; }

            T** operator&() { return &p; }

            T* operator->() { return p; }

            void Attach(T* t) noexcept
            {
                Release();
                p = t;
            }

            T* Detach() noexcept
            {
                T* tmp = p;
                p = nullptr;
                return tmp;
            }

            void Release() noexcept
            {
                if (p != nullptr)
                {
                    (void)p->Release();
                    p = nullptr;
                }
            }
        };

        // Smart pointer for CoTaskMem*
        struct cotaskmem_deleter
        {
            void operator()(LPVOID p)
            {
                CoTaskMemFree(p);
            }
        };
        template<typename T>
        using cotaskmem_ptr = std::unique_ptr<T, cotaskmem_deleter>;
    }
#endif // __cplusplus

#endif // MINIPAL_COM_H
