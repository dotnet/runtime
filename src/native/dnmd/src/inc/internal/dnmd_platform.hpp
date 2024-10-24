#ifndef _SRC_INC_INTERNAL_DNMD_PLATFORM_HPP_
#define _SRC_INC_INTERNAL_DNMD_PLATFORM_HPP_

#ifdef BUILD_WINDOWS

#define NOMINMAX
#include <Windows.h>

#else

#include <sys/stat.h>
#include "dnmd_peimage.h"

#endif // !BUILD_WINDOWS

// Machine code masks for native (R2R) images
// See pedecoder.h in CoreCLR
#define IMAGE_FILE_MACHINE_OS_MASK_APPLE 0x4644
#define IMAGE_FILE_MACHINE_OS_MASK_FREEBSD 0xADC4
#define IMAGE_FILE_MACHINE_OS_MASK_LINUX 0x7B79
#define IMAGE_FILE_MACHINE_OS_MASK_NETBSD 0x1993
#define IMAGE_FILE_MACHINE_OS_MASK_SUN 0x1992

#include <cstdlib>
#include <cstdint>

#define _HRESULT_TYPEDEF_(_sc) ((HRESULT)_sc)

#include <minipal/utils.h>
#include <minipal_com.h>
#include <cor.h>
#include <dnmd.hpp>

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