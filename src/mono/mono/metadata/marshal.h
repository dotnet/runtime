
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

/* ECMA lamespec: the old spec had more info... */
enum {
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
	MONO_NATIVE_LPSTR = 0x14,
	MONO_NATIVE_INT   = 0x1f,
	MONO_NATIVE_UINT  = 0x20,
	MONO_NATIVE_FUNC  = 0x26,
	MONO_NATIVE_ARRAY = 0x2a
} MonoMarshalNative;

void* mono_marshal_string_array (MonoArray *array);

void* mono_marshal_alloc   (gpointer size);
void  mono_marshal_free    (gpointer ptr);
void* mono_marshal_realloc (gpointer ptr, gpointer size);

#endif /* __MONO_MARSHAL_H__ */

