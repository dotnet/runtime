#ifndef _SRC_INC_INTERNAL_DNMD_PLATFORM_HPP_
#define _SRC_INC_INTERNAL_DNMD_PLATFORM_HPP_

#ifdef BUILD_WINDOWS

#define NOMINMAX
#include <Windows.h>

#else

#include <sys/stat.h>
#include "dnmd_peimage.h"

#endif // !BUILD_WINDOWS

#include <dncp.h>
#include <cor.h>
#include <dnmd.hpp>

#define ARRAY_SIZE(a) (sizeof(a) / sizeof(*a))

#endif // _SRC_INC_INTERNAL_DNMD_PLATFORM_HPP_