// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// No include guards. This file is included multiple times.
// Validates that every TYPE(name) and T_ARRAY(TYPE(name)) reference in CDAC_TYPE_FIELD
// matches a cDAC type declared with CDAC_TYPE_BEGIN(name) within the same descriptor.
// EXTERN_TYPE references are not validated (cross-descriptor or well-known types).
//
// Included from datadescriptor.cpp under #ifdef _DEBUG before the blob construction.
// This file must be included BEFORE any wrappeddatadescriptor.inc inclusion so that
// the type defines (T_*, TYPE, etc.) have not yet been set. After validation, all
// overrides are undef'd so the real defines get established by the first
// wrappeddatadescriptor.inc inclusion via the #ifndef T_UINT8 guard.
//
// Uses two passes over the descriptor:
//   Pass 1: CDAC_TYPE_BEGIN(name) declares a tag struct for each cDAC type.
//   Pass 2: TYPE(name) and T_ARRAY inner types are validated via static_assert.
// T_* primitives and EXTERN_TYPE expand to char (always valid for sizeof).

// TYPE(name) expands to a tag struct type -- sizeof() fails if undeclared.
// EXTERN_TYPE and T_* primitives expand to char -- always valid.
// T_ARRAY passes through to its inner type for validation.
#define TYPE(name)        struct cdac_type_tag_##name
#define EXTERN_TYPE(name) char
#define T_UINT8           char
#define T_UINT16          char
#define T_UINT32          char
#define T_UINT64          char
#define T_INT8            char
#define T_INT16           char
#define T_INT32           char
#define T_INT64           char
#define T_NUINT           char
#define T_NINT            char
#define T_POINTER         char
#define T_BOOL            char
#define T_ARRAY(name)     name

// Pass 1: Declare a tag struct for each CDAC_TYPE_BEGIN in this descriptor.
#define CDAC_TYPE_BEGIN(name) struct cdac_type_tag_##name { char dummy; };
#include "wrappeddatadescriptor.inc"

// Pass 2: Validate TYPE() references via static_assert on the tag structs.
#define CDAC_TYPE_FIELD(tyname,membertyname,membername,offset) \
    static_assert(sizeof(membertyname) > 0, \
        "Field " #tyname "." #membername " references undeclared cDAC type " #membertyname);
#include "wrappeddatadescriptor.inc"

// Clean up all validation overrides. The real T_*/TYPE defines will be
// established by the next wrappeddatadescriptor.inc inclusion via #ifndef T_UINT8.
#undef TYPE
#undef EXTERN_TYPE
#undef T_UINT8
#undef T_UINT16
#undef T_UINT32
#undef T_UINT64
#undef T_INT8
#undef T_INT16
#undef T_INT32
#undef T_INT64
#undef T_NUINT
#undef T_NINT
#undef T_POINTER
#undef T_BOOL
#undef T_ARRAY
