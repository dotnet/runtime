
/*
 * marshal.h: Routines for marshaling complex types in P/Invoke methods.
 * 
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2002 Ximian, Inc.  http://www.ximian.com
 *
 */

#ifndef __MONO_MARSHAL_H__
#define __MONO_MARSHAL_H__

#include <mono/metadata/class.h>
#include <mono/metadata/object.h>

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
	MONO_NATIVE_BSTR = 0x13,
	MONO_NATIVE_LPSTR = 0x14, /* char* */
	MONO_NATIVE_LPWSTR = 0x15, /* gunichar2* */
	MONO_NATIVE_LPTSTR = 0x16,
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
	MONO_NATIVE_ANSIBSTR  = 0x23,
	MONO_NATIVE_TBSTR  = 0x24,
	MONO_NATIVE_VARIANTBOOL  = 0x25,
	MONO_NATIVE_FUNC  = 0x26,
	MONO_NATIVE_ASANY = 0x28,
	MONO_NATIVE_ARRAY = 0x2a,
	MONO_NATIVE_LPSTRUCT = 0x2b,
	MONO_NATIVE_CUSTOM = 0x2c,
	MONO_NATIVE_ERROR = 0x2d,
	MONO_NATIVE_MAX = 0x50 /* no info */
} MonoMarshalNative;

typedef struct {
	MonoClassField *field;
	guint32 offset;
	guint32 utype;
	guint32 conv;
	guint32 count;
} MonoMarshalField;

struct MonoMarshalType {
	guint32 unmanaged_size;
	guint32 num_fields;
	MonoMarshalField fields [MONO_ZERO_LEN_ARRAY];
};

void* mono_marshal_string_array (MonoArray *array);

guint32 mono_type_to_unmanaged      (MonoType *type);
void    mono_marshal_load_type_info (MonoClass* klass);

void* mono_marshal_alloc   (gpointer size);
void  mono_marshal_free    (gpointer ptr);
void* mono_marshal_realloc (gpointer ptr, gpointer size);

#endif /* __MONO_MARSHAL_H__ */

