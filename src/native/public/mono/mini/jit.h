/**
 * \file
 * Author:
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001, 2002, 2003 Ximian, Inc.
 */

#ifndef _MONO_JIT_JIT_H_
#define _MONO_JIT_JIT_H_

#if defined(MONO_INSIDE_RUNTIME)
#include <mono/mini/details/jit-types.h>
#else
#include <mono/jit/details/jit-types.h>
#endif

MONO_BEGIN_DECLS

#define MONO_API_FUNCTION(ret,name,args) ret name args;
#if defined(MONO_INSIDE_RUNTIME)
#include <mono/mini/details/jit-functions.h>
#else
#include <mono/jit/details/jit-functions.h>
#endif
#undef MONO_API_FUNCTION

MONO_END_DECLS

#endif

