// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
#ifndef _MONO_METADATA_TYPES_H
#define _MONO_METADATA_TYPES_H


#include <mono/utils/details/mono-publib-types.h>

#include <mono/utils/mono-forward.h>
#include <mono/metadata/blob.h>
#include <mono/metadata/row-indexes.h>
#include <mono/metadata/details/image-types.h>
#include <mono/metadata/object-forward.h>

MONO_BEGIN_DECLS

typedef enum {
	MONO_EXCEPTION_CLAUSE_NONE,
	MONO_EXCEPTION_CLAUSE_FILTER,
	MONO_EXCEPTION_CLAUSE_FINALLY,
	MONO_EXCEPTION_CLAUSE_FAULT = 4
} MonoExceptionEnum;

typedef enum {
	MONO_CALL_DEFAULT,
	MONO_CALL_C,
	MONO_CALL_STDCALL,
	MONO_CALL_THISCALL,
	MONO_CALL_FASTCALL,
	MONO_CALL_VARARG = 0x05,
	/* unused, */
	/* unused, */
	/* unused, */
	MONO_CALL_UNMANAGED_MD = 0x09, /* default unmanaged calling convention, with additional attributed encoded in modopts */
} MonoCallConvention;

/* ECMA lamespec: the old spec had more info... */
typedef enum {
	MONO_NATIVE_BOOLEAN = 0x02, /* 4 bytes, 0 is false, != 0 is true */
	MONO_NATIVE_I1 = 0x03,
	MONO_NATIVE_U1 = 0x04,
	MONO_NATIVE_I2 = 0x05,
	MONO_NATIVE_U2 = 0x06,
	MONO_NATIVE_I4 = 0x07,
	MONO_NATIVE_U4 = 0x08,
	MONO_NATIVE_I8 = 0x09,
	MONO_NATIVE_U8 = 0x0a,
	MONO_NATIVE_R4 = 0x0b,
	MONO_NATIVE_R8 = 0x0c,
	MONO_NATIVE_CURRENCY = 0x0f,
	MONO_NATIVE_BSTR = 0x13, /* prefixed length, Unicode */
	MONO_NATIVE_LPSTR = 0x14, /* ANSI, null terminated */
	MONO_NATIVE_LPWSTR = 0x15, /* UNICODE, null terminated */
	MONO_NATIVE_LPTSTR = 0x16, /* platform dep., null terminated */
	MONO_NATIVE_BYVALTSTR = 0x17,
	MONO_NATIVE_IUNKNOWN = 0x19,
	MONO_NATIVE_IDISPATCH = 0x1a,
	MONO_NATIVE_STRUCT = 0x1b,
	MONO_NATIVE_INTERFACE = 0x1c,
	MONO_NATIVE_SAFEARRAY = 0x1d,
	MONO_NATIVE_BYVALARRAY = 0x1e,
	MONO_NATIVE_INT   = 0x1f,
	MONO_NATIVE_UINT  = 0x20,
	MONO_NATIVE_VBBYREFSTR  = 0x22,
	MONO_NATIVE_ANSIBSTR  = 0x23,  /* prefixed length, ANSI */
	MONO_NATIVE_TBSTR  = 0x24, /* prefixed length, platform dep. */
	MONO_NATIVE_VARIANTBOOL  = 0x25,
	MONO_NATIVE_FUNC  = 0x26,
	MONO_NATIVE_ASANY = 0x28,
	MONO_NATIVE_LPARRAY = 0x2a,
	MONO_NATIVE_LPSTRUCT = 0x2b,
	MONO_NATIVE_CUSTOM = 0x2c,
	MONO_NATIVE_ERROR = 0x2d,
	// TODO: MONO_NATIVE_IINSPECTABLE = 0x2e
	// TODO: MONO_NATIVE_HSTRING = 0x2f
	MONO_NATIVE_UTF8STR = 0x30,
	MONO_NATIVE_MAX = 0x50 /* no info */
} MonoMarshalNative;

/* Used only in context of SafeArray */
typedef enum {
	MONO_VARIANT_EMPTY = 0x00,
	MONO_VARIANT_NULL = 0x01,
	MONO_VARIANT_I2 = 0x02,
	MONO_VARIANT_I4 = 0x03,
	MONO_VARIANT_R4 = 0x04,
	MONO_VARIANT_R8 = 0x05,
	MONO_VARIANT_CY = 0x06,
	MONO_VARIANT_DATE = 0x07,
	MONO_VARIANT_BSTR = 0x08,
	MONO_VARIANT_DISPATCH = 0x09,
	MONO_VARIANT_ERROR = 0x0a,
	MONO_VARIANT_BOOL = 0x0b,
	MONO_VARIANT_VARIANT = 0x0c,
	MONO_VARIANT_UNKNOWN = 0x0d,
	MONO_VARIANT_DECIMAL = 0x0e,
	MONO_VARIANT_I1 = 0x10,
	MONO_VARIANT_UI1 = 0x11,
	MONO_VARIANT_UI2 = 0x12,
	MONO_VARIANT_UI4 = 0x13,
	MONO_VARIANT_I8 = 0x14,
	MONO_VARIANT_UI8 = 0x15,
	MONO_VARIANT_INT = 0x16,
	MONO_VARIANT_UINT = 0x17,
	MONO_VARIANT_VOID = 0x18,
	MONO_VARIANT_HRESULT = 0x19,
	MONO_VARIANT_PTR = 0x1a,
	MONO_VARIANT_SAFEARRAY = 0x1b,
	MONO_VARIANT_CARRAY = 0x1c,
	MONO_VARIANT_USERDEFINED = 0x1d,
	MONO_VARIANT_LPSTR = 0x1e,
	MONO_VARIANT_LPWSTR = 0x1f,
	MONO_VARIANT_RECORD = 0x24,
	MONO_VARIANT_FILETIME = 0x40,
	MONO_VARIANT_BLOB = 0x41,
	MONO_VARIANT_STREAM = 0x42,
	MONO_VARIANT_STORAGE = 0x43,
	MONO_VARIANT_STREAMED_OBJECT = 0x44,
	MONO_VARIANT_STORED_OBJECT = 0x45,
	MONO_VARIANT_BLOB_OBJECT = 0x46,
	MONO_VARIANT_CF = 0x47,
	MONO_VARIANT_CLSID = 0x48,
	MONO_VARIANT_VECTOR = 0x1000,
	MONO_VARIANT_ARRAY = 0x2000,
	MONO_VARIANT_BYREF = 0x4000
} MonoMarshalVariant;

typedef enum {
	MONO_MARSHAL_CONV_NONE,
	MONO_MARSHAL_CONV_BOOL_VARIANTBOOL,
	MONO_MARSHAL_CONV_BOOL_I4,
	MONO_MARSHAL_CONV_STR_BSTR,
	MONO_MARSHAL_CONV_STR_LPSTR,
	MONO_MARSHAL_CONV_LPSTR_STR,
	MONO_MARSHAL_CONV_LPTSTR_STR,
	MONO_MARSHAL_CONV_STR_LPWSTR,
	MONO_MARSHAL_CONV_LPWSTR_STR,
	MONO_MARSHAL_CONV_STR_LPTSTR,
	MONO_MARSHAL_CONV_STR_ANSIBSTR,
	MONO_MARSHAL_CONV_STR_TBSTR,
	MONO_MARSHAL_CONV_STR_BYVALSTR,
	MONO_MARSHAL_CONV_STR_BYVALWSTR,
	MONO_MARSHAL_CONV_SB_LPSTR,
	MONO_MARSHAL_CONV_SB_LPTSTR,
	MONO_MARSHAL_CONV_SB_LPWSTR,
	MONO_MARSHAL_CONV_LPSTR_SB,
	MONO_MARSHAL_CONV_LPTSTR_SB,
	MONO_MARSHAL_CONV_LPWSTR_SB,
	MONO_MARSHAL_CONV_ARRAY_BYVALARRAY,
	MONO_MARSHAL_CONV_ARRAY_BYVALCHARARRAY,
	MONO_MARSHAL_CONV_ARRAY_SAVEARRAY,
	MONO_MARSHAL_CONV_ARRAY_LPARRAY,
	MONO_MARSHAL_FREE_LPARRAY,
	MONO_MARSHAL_CONV_OBJECT_INTERFACE,
	MONO_MARSHAL_CONV_OBJECT_IDISPATCH,
	MONO_MARSHAL_CONV_OBJECT_IUNKNOWN,
	MONO_MARSHAL_CONV_OBJECT_STRUCT,
	MONO_MARSHAL_CONV_DEL_FTN,
	MONO_MARSHAL_CONV_FTN_DEL,
	MONO_MARSHAL_FREE_ARRAY,
	MONO_MARSHAL_CONV_BSTR_STR,
	MONO_MARSHAL_CONV_SAFEHANDLE,
	MONO_MARSHAL_CONV_HANDLEREF,
	MONO_MARSHAL_CONV_STR_UTF8STR,
	MONO_MARSHAL_CONV_SB_UTF8STR,
	MONO_MARSHAL_CONV_UTF8STR_STR,
	MONO_MARSHAL_CONV_UTF8STR_SB,
	MONO_MARSHAL_CONV_FIXED_BUFFER,
	MONO_MARSHAL_CONV_ANSIBSTR_STR,
	MONO_MARSHAL_CONV_TBSTR_STR
} MonoMarshalConv;

#define MONO_MARSHAL_CONV_INVALID ((MonoMarshalConv)-1)

typedef struct {
	MonoMarshalNative native;
	union {
		struct {
			MonoMarshalNative elem_type;
			int32_t num_elem; /* -1 if not set */
			int16_t param_num; /* -1 if not set */
			int16_t elem_mult; /* -1 if not set */
		} array_data;
		struct {
			char *custom_name;
			char *cookie;
			MonoImage *image;
		} custom_data;
		struct {
			MonoMarshalVariant elem_type;
			int32_t num_elem;
		} safearray_data;
	} data;
} MonoMarshalSpec;

typedef struct {
	uint32_t flags;
	uint32_t try_offset;
	uint32_t try_len;
	uint32_t handler_offset;
	uint32_t handler_len;
	union {
		uint32_t filter_offset;
		MonoClass *catch_class;
	} data;
} MonoExceptionClause;

typedef struct _MonoType MonoType;
typedef struct _MonoGenericInst MonoGenericInst;
typedef struct _MonoGenericClass MonoGenericClass;
typedef struct _MonoGenericContext MonoGenericContext;
typedef struct _MonoGenericContainer MonoGenericContainer;
typedef struct _MonoGenericParam MonoGenericParam;
typedef struct _MonoArrayType MonoArrayType;
typedef struct _MonoMethodSignature MonoMethodSignature;

/* FIXME: Keeping this name alive for now, since it is part of the exposed API, even though no entrypoint uses it.  */
typedef struct invalid_name MonoGenericMethod;

typedef struct {
	unsigned int required : 1;
	unsigned int token    : 31;
} MonoCustomMod;

typedef struct _MonoCustomModContainer {
	uint8_t count; /* max 64 modifiers follow at the end */
	MonoImage *image; /* Image containing types in modifiers array */
	MonoCustomMod modifiers [1]; /* Actual length is count */
} MonoCustomModContainer;

struct _MonoArrayType {
	MonoClass *eklass;
	// Number of dimensions of the array
	uint8_t rank;

	// Arrays recording known upper and lower index bounds for each dimension
	uint8_t numsizes;
	uint8_t numlobounds;
	int *sizes;
	int *lobounds;
};

typedef struct _MonoMethodHeader MonoMethodHeader;

typedef enum {
	MONO_PARSE_TYPE,
	MONO_PARSE_MOD_TYPE,
	MONO_PARSE_LOCAL,
	MONO_PARSE_PARAM,
	MONO_PARSE_RET,
	MONO_PARSE_FIELD
} MonoParseTypeMode;

MONO_END_DECLS

#endif /* _MONO_METADATA_TYPES_H */
