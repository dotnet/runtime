#ifndef MONOCORECLR_H
#define MONOCORECLR_H

#include <stdlib.h>

// Builtin types used by MonoFunctions.h
typedef signed short SInt16;
typedef unsigned short UInt16;
typedef unsigned char UInt8;
typedef signed char SInt8;
typedef signed int SInt32;
typedef unsigned int UInt32;
typedef signed long long SInt64;
//typedef unsigned long long UInt64; Defined already

// TODO: Add char (utf8 for mono)
typedef wchar_t mono_char; // used by CoreCLR

// TODO: Temp def
typedef void* mono_register_object_callback;
typedef void* mono_liveness_world_state_callback;

#define UNUSED_SYMBOL

#ifndef DO_API

//TODO: Use EXPORT_API instead of __declspec
#ifdef WIN32
#define DO_API(r,n,p)	extern "C" __declspec(dllexport) r __cdecl n p;
#else
#define DO_API(r,n,p)	extern "C" r n p;
#endif
#endif

// TODO: Move this to CMake
#define ENABLE_MONO 1
#define CORECLR 1
#define ENABLE_CORECLR 1

#if defined(_DEBUG)
#define MONO_PRE_ASSERTE         /* if you need to change modes before doing asserts override */
#define MONO_POST_ASSERTE        /* put it back */

#if !defined(MONO_ASSERTE_MSG)
#define MONO_ASSERTE_MSG(expr, msg)                                           \
        do {                                                                \
             if (!(expr)) {                                                 \
                MONO_PRE_ASSERTE                                                 \
                mono_debug_assert_dialog(__FILE__, __LINE__, msg);                   \
                MONO_POST_ASSERTE                                                \
             }                                                              \
        } while (0)
#endif // MONO__ASSERTE_MSG

#if !defined(MONO_ASSERTE)
#define MONO_ASSERTE(expr) MONO_ASSERTE_MSG(expr, #expr)
#endif  // !MONO_ASSERTE

#else // _DEBUG

#define MONO_ASSERTE(expr) ((void)0)
#define MONO_ASSERTE_MSG(expr, msg) ((void)0)

#endif // !_DEBUG


#include "MonoTypesClr.h"

#include "MonoFunctionsClr.h"

#include "../../../../unity/unity-sources/Runtime/Mono/tabledefs.h"

#endif //MONOCORECLR_H
