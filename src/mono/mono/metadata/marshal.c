/*
 * marshal.c: Routines for marshaling complex types in P/Invoke methods.
 * 
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2002 Ximian, Inc.  http://www.ximian.com
 *
 */

#include "config.h"
#include "object.h"
#include "loader.h"
#include "metadata/marshal.h"
#include "metadata/tabledefs.h"

/* FIXME: on win32 we should probably use GlobalAlloc(). */
void*
mono_marshal_alloc (gpointer size) {
	return g_try_malloc ((gulong)size);
}

void
mono_marshal_free (gpointer ptr) {
	g_free (ptr);
}

void*
mono_marshal_realloc (gpointer ptr, gpointer size) {
	return g_try_realloc (ptr, (gulong)size);
}

void*
mono_marshal_string_array (MonoArray *array)
{
	char **result;
	int i, len;

	if (!array)
		return NULL;

	len = mono_array_length (array);

	result = g_malloc (sizeof (char*) * len);
	for (i = 0; i < len; ++i) {
		MonoString *s = (MonoString*)mono_array_get (array, gpointer, i);
		result [i] = s ? mono_string_to_utf8 (s): NULL;
	}
	return result;
}

gint32
mono_marshal_type_size (MonoMarshalNative type, int count, gint32 *align)
{
	switch (type) {
	case MONO_NATIVE_BOOLEAN:
		*align = 4;
		return 4;
	case MONO_NATIVE_I1:
	case MONO_NATIVE_U1:
		*align = 1;
		return 1;
	case MONO_NATIVE_I2:
	case MONO_NATIVE_U2:
		*align = 2;
		return 2;
	case MONO_NATIVE_I4:
	case MONO_NATIVE_U4:
		*align = 4;
		return 4;
	case MONO_NATIVE_I8:
	case MONO_NATIVE_U8:
		*align = 8;
		return 8;
	case MONO_NATIVE_R4:
		*align = 4;
		return 4;
	case MONO_NATIVE_R8:
		*align = 8;
		return 8;
	case MONO_NATIVE_CURRENCY:
	case MONO_NATIVE_BSTR:
	case MONO_NATIVE_LPSTR:
	case MONO_NATIVE_LPWSTR:
	case MONO_NATIVE_LPTSTR:
	case MONO_NATIVE_BYVALTSTR:
	case MONO_NATIVE_IUNKNOWN:
	case MONO_NATIVE_IDISPATCH:
	case MONO_NATIVE_STRUCT:
	case MONO_NATIVE_INTERFACE:
	case MONO_NATIVE_SAFEARRAY:
	case MONO_NATIVE_BYVALARRAY:
	case MONO_NATIVE_INT:
	case MONO_NATIVE_UINT:
	case MONO_NATIVE_VBBYREFSTR:
	case MONO_NATIVE_ANSIBSTR:
	case MONO_NATIVE_TBSTR:
	case MONO_NATIVE_VARIANTBOOL:
	case MONO_NATIVE_FUNC:
	case MONO_NATIVE_ASANY:
	case MONO_NATIVE_ARRAY:
	case MONO_NATIVE_LPSTRUCT:
	case MONO_NATIVE_CUSTOM:
	case MONO_NATIVE_ERROR:
	default:
		break;
	}
	g_assert_not_reached ();
	return 0;
}

guint32
mono_type_to_unmanaged (MonoType *type) {
	int t = type->type;
	if (type->byref)
		return MONO_NATIVE_UINT;

handle_enum:
	switch (t) {
	case MONO_TYPE_BOOLEAN: return MONO_NATIVE_BOOLEAN;
	case MONO_TYPE_CHAR: return MONO_NATIVE_U2;
	case MONO_TYPE_I1: return MONO_NATIVE_I1;
	case MONO_TYPE_U1: return MONO_NATIVE_U1;
	case MONO_TYPE_I2: return MONO_NATIVE_I2;
	case MONO_TYPE_U2: return MONO_NATIVE_U2;
	case MONO_TYPE_I4: return MONO_NATIVE_I4;
	case MONO_TYPE_U4: return MONO_NATIVE_U4;
	case MONO_TYPE_I8: return MONO_NATIVE_I8;
	case MONO_TYPE_U8: return MONO_NATIVE_U8;
	case MONO_TYPE_R4: return MONO_NATIVE_R4;
	case MONO_TYPE_R8: return MONO_NATIVE_R8;
	/* the default may change according to the platform... */
	case MONO_TYPE_STRING: return MONO_NATIVE_LPSTR; 
	case MONO_TYPE_PTR: return MONO_NATIVE_UINT;
	case MONO_TYPE_VALUETYPE: /*FIXME*/
		if (type->data.klass->enumtype) {
			t = type->data.klass->enum_basetype->type;
			goto handle_enum;
		}
		return MONO_NATIVE_STRUCT;
	case MONO_TYPE_ARRAY: return MONO_NATIVE_ARRAY;
	case MONO_TYPE_I: return MONO_NATIVE_INT;
	case MONO_TYPE_U: return MONO_NATIVE_UINT;
	case MONO_TYPE_FNPTR: return MONO_NATIVE_FUNC;
	case MONO_TYPE_OBJECT: return MONO_NATIVE_ASANY; /* ?? */
	case MONO_TYPE_SZARRAY: return MONO_NATIVE_ARRAY;
	case MONO_TYPE_CLASS: 
		/* FIXME : we need to handle ArrayList and StringBuilder here, probably */
	case MONO_TYPE_TYPEDBYREF:
	default:
		g_error ("type 0x%02x not handled in marshal", t);
	}
	return MONO_NATIVE_MAX;
}

void
mono_marshal_load_type_info (MonoClass* klass)
{
	int i, j, count;
	MonoMarshalType *info;

	if (klass->marshal_info)
		return;

	for (i = 0; i < klass->field.count; ++i) {
		if (klass->fields [i].type->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;
		/* handle fields in embedded valuetypes. */
		count++;
	}

	klass->marshal_info = info = g_malloc0 (sizeof (MonoMarshalType) + sizeof (MonoMarshalField) * count);
	info->num_fields = count;
	
	for (j = i = 0; i < klass->field.count; ++i) {
		if (klass->fields [i].type->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;
		/* handle fields in embedded valuetypes. */
		j++;
	}

}

