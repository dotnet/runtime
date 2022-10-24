#ifndef _SRC_INC_PLATFORM_H_
#define _SRC_INC_PLATFORM_H_

#if _MSC_VER

#include <Windows.h>

#else

typedef unsigned short WCHAR;

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
