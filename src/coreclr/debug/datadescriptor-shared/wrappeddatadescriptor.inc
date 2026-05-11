// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// No include guards. This file is included multiple times.
// Wraps datadescriptor.inc to define and undefine macros used in the file.

// Field type annotation defines. These always expand to the type name
// (e.g., T_UINT32 -> uint32, TYPE(GCHandle) -> GCHandle). Whether the type name
// is included in the data descriptor blob's string pool is controlled by
// CDAC_STRINGIFY_TYPE in datadescriptor.cpp (currently debug/checked builds only).
// Primitive types use T_<TYPE> (e.g., T_UINT32, T_POINTER).
// Inline struct types use TYPE(<name>) (e.g., TYPE(GCHandle), TYPE(CodePointer)).
// EXTERN_TYPE(<name>) is the same as TYPE() but is not validated within this
// descriptor. Use it for types declared in a different descriptor or for
// well-known types that don't have a CDAC_TYPE_BEGIN in the current descriptor.
// Array types use T_ARRAY(<type>) where <type> is another type define
// (e.g., T_ARRAY(T_UINT8), T_ARRAY(TYPE(StressLogModuleDesc))).
// T_ARRAY always expands to pointer in the blob since arrays are accessed via pointers.
// These are defined once here and persist across multiple inclusions.
#ifndef T_UINT8
#define T_UINT8    uint8
#endif
#ifndef T_UINT16
#define T_UINT16   uint16
#endif
#ifndef T_UINT32
#define T_UINT32   uint32
#endif
#ifndef T_UINT64
#define T_UINT64   uint64
#endif
#ifndef T_INT8
#define T_INT8     int8
#endif
#ifndef T_INT16
#define T_INT16    int16
#endif
#ifndef T_INT32
#define T_INT32    int32
#endif
#ifndef T_INT64
#define T_INT64    int64
#endif
#ifndef T_NUINT
#define T_NUINT    nuint
#endif
#ifndef T_NINT
#define T_NINT     nint
#endif
#ifndef T_POINTER
#define T_POINTER  pointer
#endif
#ifndef T_BOOL
#define T_BOOL     bool
#endif
#ifndef TYPE
#define TYPE(name)          name
#endif
#ifndef EXTERN_TYPE
#define EXTERN_TYPE(name)   name
#endif
#ifndef T_ARRAY
#define T_ARRAY(name)       pointer
#endif

#ifndef CDAC_BASELINE
#define CDAC_BASELINE(identifier)
#endif
#ifndef CDAC_TYPES_BEGIN
#define CDAC_TYPES_BEGIN()
#endif
#ifndef CDAC_TYPE_BEGIN
#define CDAC_TYPE_BEGIN(tyname)
#endif
#ifndef CDAC_TYPE_SIZE
#define CDAC_TYPE_SIZE(k)
#endif
#ifndef CDAC_TYPE_INDETERMINATE
#define CDAC_TYPE_INDETERMINATE(tyname)
#endif
#ifndef CDAC_TYPE_FIELD
#define CDAC_TYPE_FIELD(tyname,fieldtyname,fieldname,off)
#endif
#ifndef CDAC_TYPE_END
#define CDAC_TYPE_END(tyname)
#endif
#ifndef CDAC_TYPES_END
#define CDAC_TYPES_END()
#endif
#ifndef CDAC_GLOBALS_BEGIN
#define CDAC_GLOBALS_BEGIN()
#endif
#ifndef CDAC_GLOBAL
#define CDAC_GLOBAL(globalname,tyname,val)
#endif
#ifndef CDAC_GLOBAL_POINTER
#define CDAC_GLOBAL_POINTER(globalname,addr)
#endif
#ifndef CDAC_GLOBAL_STRING
#define CDAC_GLOBAL_STRING(globalname,stringval)
#endif
#ifndef CDAC_GLOBAL_SUB_DESCRIPTOR
#define CDAC_GLOBAL_SUB_DESCRIPTOR(globalname,addr)
#endif
#ifndef CDAC_GLOBAL_CONTRACT
#define CDAC_GLOBAL_CONTRACT(name,value)
#endif
#ifndef CDAC_GLOBALS_END
#define CDAC_GLOBALS_END()
#endif

#include "datadescriptor.inc"

#undef CDAC_BASELINE
#undef CDAC_TYPES_BEGIN
#undef CDAC_TYPE_BEGIN
#undef CDAC_TYPE_INDETERMINATE
#undef CDAC_TYPE_SIZE
#undef CDAC_TYPE_FIELD
#undef CDAC_TYPE_END
#undef CDAC_TYPES_END
#undef CDAC_GLOBALS_BEGIN
#undef CDAC_GLOBAL
#undef CDAC_GLOBAL_POINTER
#undef CDAC_GLOBAL_STRING
#undef CDAC_GLOBAL_SUB_DESCRIPTOR
#undef CDAC_GLOBAL_CONTRACT
#undef CDAC_GLOBALS_END
