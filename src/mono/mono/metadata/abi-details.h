/*
 * Copyright 2014 Xamarin Inc
 */
#ifndef __MONO_METADATA_ABI_DETAILS_H__
#define __MONO_METADATA_ABI_DETAILS_H__

#include <config.h>
#include <glib.h>

#define MONO_ABI_ALIGNOF(type) MONO_ALIGN_ ## type
#define MONO_CURRENT_ABI_ALIGNOF(type) ((int)G_STRUCT_OFFSET(struct { char c; type x; }, x))


#undef DECL_OFFSET
#undef DECL_OFFSET2
#define DECL_OFFSET(struct,field) MONO_OFFSET_ ## struct ## _ ## field = -1,
#define DECL_OFFSET2(struct,field,offset) MONO_OFFSET_ ## struct ## _ ## field = offset,
#define DECL_ALIGN(type) MONO_ALIGN_ ##type = MONO_CURRENT_ABI_ALIGNOF (type),
#define DECL_ALIGN2(type,size) MONO_ALIGN_ ##type = size,

enum {
#include "object-offsets.h"
};

#ifdef USED_CROSS_COMPILER_OFFSETS
#define MONO_STRUCT_OFFSET(struct,field) MONO_OFFSET_ ## struct ## _ ## field
#else
#define MONO_STRUCT_OFFSET(struct,field) (MONO_OFFSET_ ## struct ## _ ## field == -1, G_STRUCT_OFFSET (struct,field))
#endif

#endif
