#ifndef _SRC_INC_INTERNAL_DNMD_PLATFORM_HPP_
#define _SRC_INC_INTERNAL_DNMD_PLATFORM_HPP_

#ifdef BUILD_WINDOWS

#define NOMINMAX
#include <Windows.h>

#else

#include <sys/stat.h>
#include "dnmd_peimage.h"

#endif // !BUILD_WINDOWS

#include <cstdlib>
#include <cstdint>

#include <dncp.h>
#include <cor.h>
#include <dnmd.hpp>

#define ARRAY_SIZE(a) (sizeof(a) / sizeof(*a))

template<typename T>
struct malloc_deleter_t final
{
    using pointer = T*;
    void operator()(T* mem)
    {
        ::free((void*)mem);
    }
};

// C++ lifetime wrapper for malloc'd memory
template<typename T>
using malloc_ptr = std::unique_ptr<T, malloc_deleter_t<T>>;

#endif // _SRC_INC_INTERNAL_DNMD_PLATFORM_HPP_