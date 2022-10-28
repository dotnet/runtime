#ifndef _SRC_INC_PLATFORM_H_
#define _SRC_INC_PLATFORM_H_

// Defining "DEBUG" since NDEBUG is the only
// macro mentioned by the standard.
#ifndef DEBUG
#ifndef NDEBUG
#define DEBUG
#endif
#endif // DEBUG

#ifdef _MSC_VER

#define BUILD_WINDOWS

#include <Windows.h>

#else

#include <sys/stat.h>

typedef unsigned short WCHAR;
typedef uint32_t ULONG32;

typedef struct _GUID
{
    uint32_t Data1;
    uint16_t Data2;
    uint16_t Data3;
    uint8_t  Data4[8];
} GUID;

#endif

#include "external/corhdr.h"

#endif // _SRC_INC_PLATFORM_H_
