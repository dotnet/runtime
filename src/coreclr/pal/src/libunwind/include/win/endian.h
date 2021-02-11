// This is an incomplete & imprecice implementation of the
// standard file by the same name


// Since this is only intended for VC++ compilers
// use #pragma once instead of guard macros
#pragma once

#ifdef _MSC_VER // Only for cross compilation to windows

#define __LITTLE_ENDIAN        1234
#define __BIG_ENDIAN           4321

#define __BYTE_ORDER __LITTLE_ENDIAN

#endif // _MSC_VER
