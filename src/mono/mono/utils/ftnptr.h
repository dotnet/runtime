#ifndef __MONO_UTILS_FTNPTR_H__
#define __MONO_UTILS_FTNPTR_H__

#include <config.h>

#if defined(TARGET_OSX)
#if __has_feature(ptrauth_intrinsics)
#define MONO_ARCH_ENABLE_PTRAUTH 1
#include <ptrauth.h>
#endif
#endif

/*
 * This is a general function pointer type. On most platforms,
 * it is equal to the function's address in memory, except:
 * - on wasm, its equal to the function's wasm index
 * - on arm64e, its an authenticated pointer.
 */
typedef gpointer MonoFtnPtr;

/*
 * Authenticated pointer support for arm64e.
 *   Pointers are in their authenticated form in most parts of the runtime code,
 * except the parts which needs their numerical value i.e. codegen, jit info
 * table, EH, etc.
 * FIXME:
 * - use returns in trampolines as well
 * - clean up ifdefs
 */
#if defined(MONO_ARCH_ENABLE_PTRAUTH)
/*
 * Convert a raw pointer to a function to a C function pointer.
 */
#define MINI_ADDR_TO_FTNPTR(addr) (MonoFtnPtr)ptrauth_sign_unauthenticated ((addr), ptrauth_key_function_pointer, NULL)

#define MINI_FTNPTR_TO_ADDR(addr) ptrauth_strip ((addr), ptrauth_key_function_pointer)
#else
#define MINI_ADDR_TO_FTNPTR(addr) ((MonoFtnPtr)(addr))
#define MINI_FTNPTR_TO_ADDR(addr) ((gpointer)(addr))
#endif

#endif

