// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This describes information about the COM+ primitive types

#define NO_SIZE ((BYTE)-1)

// TYPEINFO(type (CorElementType),  namespace, class,          size,                 gcType,         isArray,isPrim, isFloat,isModifier,isGenVariable)

TYPEINFO(ELEMENT_TYPE_END,          NULL, NULL,                NO_SIZE,              TYPE_GC_NONE,   false,  false,  false,  false,  false) // 0x00
TYPEINFO(ELEMENT_TYPE_VOID,         "System", "Void",          0,                    TYPE_GC_NONE,   false,  true,   false,  false,  false) // 0x01
TYPEINFO(ELEMENT_TYPE_BOOLEAN,      "System", "Boolean",       1,                    TYPE_GC_NONE,   false,  true,   false,  false,  false) // 0x02
TYPEINFO(ELEMENT_TYPE_CHAR,         "System", "Char",          2,                    TYPE_GC_NONE,   false,  true,   false,  false,  false) // 0x03
TYPEINFO(ELEMENT_TYPE_I1,           "System", "SByte",         1,                    TYPE_GC_NONE,   false,  true,   false,  false,  false) // 0x04
TYPEINFO(ELEMENT_TYPE_U1,           "System", "Byte",          1,                    TYPE_GC_NONE,   false,  true,   false,  false,  false) // 0x05
TYPEINFO(ELEMENT_TYPE_I2,           "System", "Int16",         2,                    TYPE_GC_NONE,   false,  true,   false,  false,  false) // 0x06
TYPEINFO(ELEMENT_TYPE_U2,           "System", "UInt16",        2,                    TYPE_GC_NONE,   false,  true,   false,  false,  false) // 0x07
TYPEINFO(ELEMENT_TYPE_I4,           "System", "Int32",         4,                    TYPE_GC_NONE,   false,  true,   false,  false,  false) // 0x08
TYPEINFO(ELEMENT_TYPE_U4,           "System", "UInt32",        4,                    TYPE_GC_NONE,   false,  true,   false,  false,  false) // 0x09
TYPEINFO(ELEMENT_TYPE_I8,           "System", "Int64",         8,                    TYPE_GC_NONE,   false,  true,   false,  false,  false) // 0x0a
TYPEINFO(ELEMENT_TYPE_U8,           "System", "UInt64",        8,                    TYPE_GC_NONE,   false,  true,   false,  false,  false) // 0x0b

TYPEINFO(ELEMENT_TYPE_R4,           "System", "Single",        4,                    TYPE_GC_NONE,   false,  true,   true,   false,  false) // 0x0c
TYPEINFO(ELEMENT_TYPE_R8,           "System", "Double",        8,                    TYPE_GC_NONE,   false,  true,   true,   false,  false) // 0x0d

TYPEINFO(ELEMENT_TYPE_STRING,       "System", "String",        TARGET_POINTER_SIZE,  TYPE_GC_REF,    false,  false,  false,  false,  false) // 0x0e
TYPEINFO(ELEMENT_TYPE_PTR,          NULL, NULL,                TARGET_POINTER_SIZE,  TYPE_GC_NONE,   false,  false,  false,  true,   false) // 0x0f
TYPEINFO(ELEMENT_TYPE_BYREF,        NULL, NULL,                TARGET_POINTER_SIZE,  TYPE_GC_BYREF,  false,  false,  false,  true,   false) // 0x10
TYPEINFO(ELEMENT_TYPE_VALUETYPE,    NULL, NULL,                NO_SIZE,              TYPE_GC_OTHER,  false,  false,  false,  false,  false) // 0x11
TYPEINFO(ELEMENT_TYPE_CLASS,        NULL, NULL,                TARGET_POINTER_SIZE,  TYPE_GC_REF,    false,  false,  false,  false,  false) // 0x12
TYPEINFO(ELEMENT_TYPE_VAR,          NULL, NULL,                TARGET_POINTER_SIZE,  TYPE_GC_OTHER,  false,  false,  false,  false,  true)  // 0x13
TYPEINFO(ELEMENT_TYPE_ARRAY,        NULL, NULL,                TARGET_POINTER_SIZE,  TYPE_GC_REF,    true,   false,  false,  true,   false) // 0x14

TYPEINFO(ELEMENT_TYPE_GENERICINST,  NULL, NULL,                TARGET_POINTER_SIZE,  TYPE_GC_OTHER,  false,  false,  false,  false,  false) // 0x15
TYPEINFO(ELEMENT_TYPE_TYPEDBYREF,   "System", "TypedReference",2*TARGET_POINTER_SIZE,TYPE_GC_BYREF,  false,  false,  false,  false,  false) // 0x16
TYPEINFO(ELEMENT_TYPE_VALUEARRAY_UNSUPPORTED, NULL,NULL,       NO_SIZE,              TYPE_GC_NONE,   false,  false,  false,  false,  false) // 0x17 (unsupported, not in the ECMA spec)

TYPEINFO(ELEMENT_TYPE_I,            "System", "IntPtr",        TARGET_POINTER_SIZE,  TYPE_GC_NONE,   false,  true,   false,  false,  false) // 0x18
TYPEINFO(ELEMENT_TYPE_U,            "System", "UIntPtr",       TARGET_POINTER_SIZE,  TYPE_GC_NONE,   false,  true,   false,  false,  false) // 0x19
TYPEINFO(ELEMENT_TYPE_R_UNSUPPORTED,NULL, NULL,                NO_SIZE,              TYPE_GC_NONE,   false,  false,  false,  false,  false) // 0x1a (unsupported, not in the ECMA spec)

TYPEINFO(ELEMENT_TYPE_FNPTR,        NULL, NULL,                TARGET_POINTER_SIZE,  TYPE_GC_NONE,   false,  false,  false,  false,  false) // 0x1b
TYPEINFO(ELEMENT_TYPE_OBJECT,       "System", "Object",        TARGET_POINTER_SIZE,  TYPE_GC_REF,    false,  false,  false,  false,  false) // 0x1c
TYPEINFO(ELEMENT_TYPE_SZARRAY,      NULL, NULL,                TARGET_POINTER_SIZE,  TYPE_GC_REF,    true,   false,  false,  true,   false) // 0x1d
TYPEINFO(ELEMENT_TYPE_MVAR,         NULL, NULL,                TARGET_POINTER_SIZE,  TYPE_GC_OTHER,  false,  false,  false,  false,  true)  // x01e
TYPEINFO(ELEMENT_TYPE_CMOD_REQD,    NULL, NULL,                0,                    TYPE_GC_NONE,   false,  false,  false,  false,  false) // 0x1f
TYPEINFO(ELEMENT_TYPE_CMOD_OPT,     NULL, NULL,                0,                    TYPE_GC_NONE,   false,  false,  false,  false,  false) // 0x20
TYPEINFO(ELEMENT_TYPE_INTERNAL,     NULL, NULL,                0,                    TYPE_GC_OTHER,  false,  false,  false,  false,  false) // 0x21
