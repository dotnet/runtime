#ifndef _SRC_INC_DNMD_HPP_
#define _SRC_INC_DNMD_HPP_

#include <memory>
#include "dnmd.h"

struct mdhandle_deleter_t
{
    using pointer = mdhandle_t;
    void operator()(mdhandle_t handle)
    {
        ::md_destroy_handle(handle);
    }
};

// C++ lifetime wrapper for mdhandle_t type
using mdhandle_ptr = std::unique_ptr<mdhandle_t, mdhandle_deleter_t>;

struct cotaskmem_deleter_t
{
    using pointer = void*;
    void operator()(void* mem)
    {
        ::CoTaskMemFree(mem);
    }
};

// C++ lifetime wrapper for CoTaskMemAlloc'd memory
using cotaskmem_ptr = std::unique_ptr<void, cotaskmem_deleter_t>;

template<typename T>
struct malloc_deleter_t
{
    using pointer = T*;
    void operator()(T* mem)
    {
        ::free((void*)mem);
    }
};

// C++ lifetime wrapper for malloc'd memory
template<typename T>
using malloc_ptr = std::unique_ptr<T, typename malloc_deleter_t<T>>;

#endif // _SRC_INC_DNMD_HPP_
