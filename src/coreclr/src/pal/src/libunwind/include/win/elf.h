// This is an incomplete & imprecice implementation
// It defers to the open source freebsd-elf implementations.

// Since this is only intended for VC++ compilers
// use #pragma once instead of guard macros
#pragma once

#ifdef _MSC_VER // Only for cross compilation to windows

#include <inttypes.h>

#include "freebsd-elf_common.h"
#include "freebsd-elf32.h"
#include "freebsd-elf64.h"

#endif // _MSC_VER
