/**
 * \file Declarations of MonoClass field offset functions
 * Copyright 2018 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_CLASS_ABI_DETAILS_H__
#define __MONO_METADATA_CLASS_ABI_DETAILS_H__

#include <mono/metadata/class-internals.h>
#include <mono/metadata/abi-details.h>

#define MONO_CLASS_GETTER(funcname, rettype, optref, argtype, fieldname) /*nothing*/
/*
 * In-tree profilers are allowed to use the offset functions.  So if we're
 * compiling with --enable-checked-build=private_types, mark the symbols with
 * MONO_PROFILER_API
 */
#ifdef MONO_CLASS_DEF_PRIVATE
#define MONO_CLASS_OFFSET(funcname, argtype, fieldname) MONO_PROFILER_API intptr_t funcname (void);
#else
#define MONO_CLASS_OFFSET(funcname, argtype, fieldname) static inline intptr_t funcname (void) { return MONO_STRUCT_OFFSET (argtype, fieldname); }
#endif
#include "class-getters.h"
#undef MONO_CLASS_GETTER
#undef MONO_CLASS_OFFSET

#endif /* __MONO_METADATA_CLASS_ABI_DETAILS_H__ */

