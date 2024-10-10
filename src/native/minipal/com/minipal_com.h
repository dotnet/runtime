// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef MINIPAL_COM_H
#define MINIPAL_COM_H

#include "comtypes.h"

#ifdef __cplusplus
    extern "C"
    {
#endif // __cplusplus

//
// Memory allocators
//
LPVOID PAL_CoTaskMemAlloc(SIZE_T);
void PAL_CoTaskMemFree(LPVOID);

//
// Strings
//

// This macro is used to standardize wide character string literals used in .NET.
#ifdef DNCP_WINDOWS
    #define W(str)  L ## str
#else
    #define W(str)  u ## str
#endif

size_t PAL_wcslen(WCHAR const*);
int PAL_wcscmp(WCHAR const*, WCHAR const*);
WCHAR* PAL_wcsstr(WCHAR const*, WCHAR const*);

//
// GUIDs
//

HRESULT PAL_CoCreateGuid(GUID*);
BOOL PAL_IsEqualGUID(GUID const*, GUID const*);

int32_t PAL_StringFromGUID2(GUID const*, LPOLESTR, int32_t);
HRESULT PAL_IIDFromString(LPCOLESTR, IID*);

#ifdef __cplusplus
    #ifdef MINIPAL_COM_WINHDRS
    inline bool operator==(REFGUID a, REFGUID b)
    {
        return FALSE != PAL_IsEqualGUID(&a, &b);
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
            void operator()(LPVOID p) { PAL_CoTaskMemFree(p); }
        };
        template<typename T>
        using cotaskmem_ptr = std::unique_ptr<T, cotaskmem_deleter>;
    }
#endif // __cplusplus

#endif // MINIPAL_COM_H
