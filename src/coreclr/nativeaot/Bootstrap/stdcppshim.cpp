// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>

namespace std
{
    struct nothrow_t {};
    extern const nothrow_t nothrow = {};
    using size_t = ::size_t;
}

void* operator new(size_t n, const std::nothrow_t&) noexcept
{
    return malloc(n);
}

void* operator new[](size_t n, const std::nothrow_t&) noexcept
{
    return malloc(n);
}

void operator delete(void *p) noexcept
{
    free(p);
}

void operator delete[](void *p) noexcept
{
    free(p);
}

void operator delete(void* p, std::size_t) noexcept
{
    free(p);
}

void operator delete[](void* p, std::size_t) noexcept
{
    free(p);
}

#ifdef WASI_SINGLE_THREADED

using Dtor = void (*)(void*);

// Provide TLS destructor registration without linking libc++abi.
// The sole thread's destructors run at process exit.
extern "C" int __cxa_thread_atexit(Dtor dtor, void* obj, void*)
{
    struct DtorList
    {
        Dtor dtor;
        void* obj;
        DtorList* next;
    };

    struct DtorsManager
    {
        DtorList* m_dtors = nullptr;

        ~DtorsManager()
        {
            while (DtorList* head = m_dtors)
            {
                m_dtors = head->next;
                head->dtor(head->obj);
                free(head);
            }
        }
    };

    static DtorsManager s_dtorsManager;

    DtorList* head = static_cast<DtorList*>(malloc(sizeof(DtorList)));
    if (head == nullptr)
    {
        return -1;
    }

    head->dtor = dtor;
    head->obj = obj;
    head->next = s_dtorsManager.m_dtors;
    s_dtorsManager.m_dtors = head;

    return 0;
}

#endif // WASI_SINGLE_THREADED
