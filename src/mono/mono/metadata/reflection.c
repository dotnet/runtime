/*
 * reflection.c: Routines for creating an image at runtime.
 * 
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Rodrigo Kumpera
 *
 */
#include <config.h>
#include "mono/utils/mono-digest.h"
#include "mono/utils/mono-membar.h"
#include "mono/metadata/reflection.h"
#include "mono/metadata/tabledefs.h"
#include "mono/metadata/metadata-internals.h"
#include <mono/metadata/profiler-private.h>
#include "mono/metadata/class-internals.h"
#include "mono/metadata/gc-internal.h"
#include "mono/metadata/tokentype.h"
#include "mono/metadata/domain-internals.h"
#include "mono/metadata/opcodes.h"
#include "mono/metadata/assembly.h"
#include "mono/metadata/object-internals.h"
#include <mono/metadata/exception.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/security-manager.h>
#include <stdio.h>
#include <glib.h>
#include <errno.h>
#include <time.h>
#include <string.h>
#include <ctype.h>
#include "image.h"
#include "cil-coff.h"
#include "mono-endian.h"
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/mempool-internals.h>
#include <mono/metadata/security-core-clr.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/verify-internals.h>
#include <mono/metadata/mono-ptr-array.h>
#include <mono/utils/mono-string.h>
#include <mono/utils/mono-error-internals.h>

static gboolean is_usertype (MonoReflectionType *ref);
static MonoReflectionType *mono_reflection_type_resolve_user_types (MonoReflectionType *type);

typedef struct {
	char *p;
	char *buf;
	char *end;
} SigBuffer;

#define TEXT_OFFSET 512
#define CLI_H_SIZE 136
#define FILE_ALIGN 512
#define VIRT_ALIGN 8192
#define START_TEXT_RVA  0x00002000

typedef struct {
	MonoReflectionILGen *ilgen;
	MonoReflectionType *rtype;
	MonoArray *parameters;
	MonoArray *generic_params;
	MonoGenericContainer *generic_container;
	MonoArray *pinfo;
	MonoArray *opt_types;
	guint32 attrs;
	guint32 iattrs;
	guint32 call_conv;
	guint32 *table_idx; /* note: it's a pointer */
	MonoArray *code;
	MonoObject *type;
	MonoString *name;
	MonoBoolean init_locals;
	MonoBoolean skip_visibility;
	MonoArray *return_modreq;
	MonoArray *return_modopt;
	MonoArray *param_modreq;
	MonoArray *param_modopt;
	MonoArray *permissions;
	MonoMethod *mhandle;
	guint32 nrefs;
	gpointer *refs;
	/* for PInvoke */
	int charset, extra_flags, native_cc;
	MonoString *dll, *dllentry;
} ReflectionMethodBuilder;

typedef struct {
	guint32 owner;
	MonoReflectionGenericParam *gparam;
} GenericParamTableEntry;

const unsigned char table_sizes [MONO_TABLE_NUM] = {
	MONO_MODULE_SIZE,
	MONO_TYPEREF_SIZE,
	MONO_TYPEDEF_SIZE,
	0,
	MONO_FIELD_SIZE,
	0,
	MONO_METHOD_SIZE,
	0,
	MONO_PARAM_SIZE,
	MONO_INTERFACEIMPL_SIZE,
	MONO_MEMBERREF_SIZE,	/* 0x0A */
	MONO_CONSTANT_SIZE,
	MONO_CUSTOM_ATTR_SIZE,
	MONO_FIELD_MARSHAL_SIZE,
	MONO_DECL_SECURITY_SIZE,
	MONO_CLASS_LAYOUT_SIZE,
	MONO_FIELD_LAYOUT_SIZE,	/* 0x10 */
	MONO_STAND_ALONE_SIGNATURE_SIZE,
	MONO_EVENT_MAP_SIZE,
	0,
	MONO_EVENT_SIZE,
	MONO_PROPERTY_MAP_SIZE,
	0,
	MONO_PROPERTY_SIZE,
	MONO_METHOD_SEMA_SIZE,
	MONO_METHODIMPL_SIZE,
	MONO_MODULEREF_SIZE,	/* 0x1A */
	MONO_TYPESPEC_SIZE,
	MONO_IMPLMAP_SIZE,	
	MONO_FIELD_RVA_SIZE,
	0,
	0,
	MONO_ASSEMBLY_SIZE,	/* 0x20 */
	MONO_ASSEMBLY_PROCESSOR_SIZE,
	MONO_ASSEMBLYOS_SIZE,
	MONO_ASSEMBLYREF_SIZE,
	MONO_ASSEMBLYREFPROC_SIZE,
	MONO_ASSEMBLYREFOS_SIZE,
	MONO_FILE_SIZE,
	MONO_EXP_TYPE_SIZE,
	MONO_MANIFEST_SIZE,
	MONO_NESTED_CLASS_SIZE,

	MONO_GENERICPARAM_SIZE,	/* 0x2A */
	MONO_METHODSPEC_SIZE,
	MONO_GENPARCONSTRAINT_SIZE

};

#ifndef DISABLE_REFLECTION_EMIT
static guint32 mono_image_get_methodref_token (MonoDynamicImage *assembly, MonoMethod *method, gboolean create_typespec);
static guint32 mono_image_get_methodbuilder_token (MonoDynamicImage *assembly, MonoReflectionMethodBuilder *mb, gboolean create_open_instance);
static guint32 mono_image_get_ctorbuilder_token (MonoDynamicImage *assembly, MonoReflectionCtorBuilder *cb);
static guint32 mono_image_get_sighelper_token (MonoDynamicImage *assembly, MonoReflectionSigHelper *helper);
static void    ensure_runtime_vtable (MonoClass *klass);
static gpointer resolve_object (MonoImage *image, MonoObject *obj, MonoClass **handle_class, MonoGenericContext *context);
static guint32 mono_image_get_methodref_token_for_methodbuilder (MonoDynamicImage *assembly, MonoReflectionMethodBuilder *method);
static guint32 encode_generic_method_sig (MonoDynamicImage *assembly, MonoGenericContext *context);
static gpointer register_assembly (MonoDomain *domain, MonoReflectionAssembly *res, MonoAssembly *assembly);
static void reflection_methodbuilder_from_method_builder (ReflectionMethodBuilder *rmb, MonoReflectionMethodBuilder *mb);
static void reflection_methodbuilder_from_ctor_builder (ReflectionMethodBuilder *rmb, MonoReflectionCtorBuilder *mb);
static guint32 create_generic_typespec (MonoDynamicImage *assembly, MonoReflectionTypeBuilder *tb);
#endif

static guint32 mono_image_typedef_or_ref (MonoDynamicImage *assembly, MonoType *type);
static guint32 mono_image_typedef_or_ref_full (MonoDynamicImage *assembly, MonoType *type, gboolean try_typespec);
static void    mono_image_get_generic_param_info (MonoReflectionGenericParam *gparam, guint32 owner, MonoDynamicImage *assembly);
static guint32 encode_marshal_blob (MonoDynamicImage *assembly, MonoReflectionMarshal *minfo);
static guint32 encode_constant (MonoDynamicImage *assembly, MonoObject *val, guint32 *ret_type);
static char*   type_get_qualified_name (MonoType *type, MonoAssembly *ass);
static void    encode_type (MonoDynamicImage *assembly, MonoType *type, SigBuffer *buf);
static void get_default_param_value_blobs (MonoMethod *method, char **blobs, guint32 *types);
static MonoReflectionType *mono_reflection_type_get_underlying_system_type (MonoReflectionType* t);
static MonoType* mono_reflection_get_type_with_rootimage (MonoImage *rootimage, MonoImage* image, MonoTypeNameParse *info, gboolean ignorecase, gboolean *type_resolve);
static MonoReflectionType* mono_reflection_type_resolve_user_types (MonoReflectionType *type);
static gboolean is_sre_array (MonoClass *class);
static gboolean is_sre_byref (MonoClass *class);
static gboolean is_sre_pointer (MonoClass *class);
static gboolean is_sre_type_builder (MonoClass *class);
static gboolean is_sre_method_builder (MonoClass *class);
static gboolean is_sre_ctor_builder (MonoClass *class);
static gboolean is_sre_field_builder (MonoClass *class);
static gboolean is_sr_mono_method (MonoClass *class);
static gboolean is_sr_mono_cmethod (MonoClass *class);
static gboolean is_sr_mono_generic_method (MonoClass *class);
static gboolean is_sr_mono_generic_cmethod (MonoClass *class);
static gboolean is_sr_mono_field (MonoClass *class);
static gboolean is_sr_mono_property (MonoClass *class);
static gboolean is_sre_method_on_tb_inst (MonoClass *class);
static gboolean is_sre_ctor_on_tb_inst (MonoClass *class);

static guint32 mono_image_get_methodspec_token (MonoDynamicImage *assembly, MonoMethod *method);
static guint32 mono_image_get_inflated_method_token (MonoDynamicImage *assembly, MonoMethod *m);
static MonoMethod * inflate_method (MonoReflectionType *type, MonoObject *obj);

static guint32 create_typespec (MonoDynamicImage *assembly, MonoType *type);
static void init_type_builder_generics (MonoObject *type);

#define RESOLVE_TYPE(type) do { type = (void*)mono_reflection_type_resolve_user_types ((MonoReflectionType*)type); } while (0)
#define RESOLVE_ARRAY_TYPE_ELEMENT(array, index) do {	\
	MonoReflectionType *__type = mono_array_get (array, MonoReflectionType*, index);	\
	__type = mono_reflection_type_resolve_user_types (__type);	\
	mono_array_set (arr, MonoReflectionType*, index, __type);	\
} while (0)

#define mono_type_array_get_and_resolve(array, index) mono_reflection_type_get_handle ((MonoReflectionType*)mono_array_get (array, gpointer, index))

void
mono_reflection_init (void)
{
}

static void
sigbuffer_init (SigBuffer *buf, int size)
{
	buf->buf = g_malloc (size);
	buf->p = buf->buf;
	buf->end = buf->buf + size;
}

static void
sigbuffer_make_room (SigBuffer *buf, int size)
{
	if (buf->end - buf->p < size) {
		int new_size = buf->end - buf->buf + size + 32;
		char *p = g_realloc (buf->buf, new_size);
		size = buf->p - buf->buf;
		buf->buf = p;
		buf->p = p + size;
		buf->end = buf->buf + new_size;
	}
}

static void
sigbuffer_add_value (SigBuffer *buf, guint32 val)
{
	sigbuffer_make_room (buf, 6);
	mono_metadata_encode_value (val, buf->p, &buf->p);
}

static void
sigbuffer_add_byte (SigBuffer *buf, guint8 val)
{
	sigbuffer_make_room (buf, 1);
	buf->p [0] = val;
	buf->p++;
}

static void
sigbuffer_add_mem (SigBuffer *buf, char *p, guint32 size)
{
	sigbuffer_make_room (buf, size);
	memcpy (buf->p, p, size);
	buf->p += size;
}

static void
sigbuffer_free (SigBuffer *buf)
{
	g_free (buf->buf);
}

#ifndef DISABLE_REFLECTION_EMIT
/**
 * mp_g_alloc:
 *
 * Allocate memory from the @image mempool if it is non-NULL. Otherwise, allocate memory
 * from the C heap.
 */
static gpointer
image_g_malloc (MonoImage *image, guint size)
{
	if (image)
		return mono_image_alloc (image, size);
	else
		return g_malloc (size);
}
#endif /* !DISABLE_REFLECTION_EMIT */

/**
 * image_g_alloc0:
 *
 * Allocate memory from the @image mempool if it is non-NULL. Otherwise, allocate memory
 * from the C heap.
 */
static gpointer
image_g_malloc0 (MonoImage *image, guint size)
{
	if (image)
		return mono_image_alloc0 (image, size);
	else
		return g_malloc0 (size);
}

#ifndef DISABLE_REFLECTION_EMIT
static char*
image_strdup (MonoImage *image, const char *s)
{
	if (image)
		return mono_image_strdup (image, s);
	else
		return g_strdup (s);
}
#endif

#define image_g_new(image,struct_type, n_structs)		\
    ((struct_type *) image_g_malloc (image, ((gsize) sizeof (struct_type)) * ((gsize) (n_structs))))

#define image_g_new0(image,struct_type, n_structs)		\
    ((struct_type *) image_g_malloc0 (image, ((gsize) sizeof (struct_type)) * ((gsize) (n_structs))))


static void
alloc_table (MonoDynamicTable *table, guint nrows)
{
	table->rows = nrows;
	g_assert (table->columns);
	if (nrows + 1 >= table->alloc_rows) {
		while (nrows + 1 >= table->alloc_rows) {
			if (table->alloc_rows == 0)
				table->alloc_rows = 16;
			else
				table->alloc_rows *= 2;
		}

		table->values = g_renew (guint32, table->values, (table->alloc_rows) * table->columns);
	}
}

static void
make_room_in_stream (MonoDynamicStream *stream, int size)
{
	if (size <= stream->alloc_size)
		return;
	
	while (stream->alloc_size <= size) {
		if (stream->alloc_size < 4096)
			stream->alloc_size = 4096;
		else
			stream->alloc_size *= 2;
	}
	
	stream->data = g_realloc (stream->data, stream->alloc_size);
}

static guint32
string_heap_insert (MonoDynamicStream *sh, const char *str)
{
	guint32 idx;
	guint32 len;
	gpointer oldkey, oldval;

	if (g_hash_table_lookup_extended (sh->hash, str, &oldkey, &oldval))
		return GPOINTER_TO_UINT (oldval);

	len = strlen (str) + 1;
	idx = sh->index;
	
	make_room_in_stream (sh, idx + len);

	/*
	 * We strdup the string even if we already copy them in sh->data
	 * so that the string pointers in the hash remain valid even if
	 * we need to realloc sh->data. We may want to avoid that later.
	 */
	g_hash_table_insert (sh->hash, g_strdup (str), GUINT_TO_POINTER (idx));
	memcpy (sh->data + idx, str, len);
	sh->index += len;
	return idx;
}

static guint32
string_heap_insert_mstring (MonoDynamicStream *sh, MonoString *str)
{
	char *name = mono_string_to_utf8 (str);
	guint32 idx;
	idx = string_heap_insert (sh, name);
	g_free (name);
	return idx;
}

#ifndef DISABLE_REFLECTION_EMIT
static void
string_heap_init (MonoDynamicStream *sh)
{
	sh->index = 0;
	sh->alloc_size = 4096;
	sh->data = g_malloc (4096);
	sh->hash = g_hash_table_new_full (g_str_hash, g_str_equal, g_free, NULL);
	string_heap_insert (sh, "");
}
#endif

static guint32
mono_image_add_stream_data (MonoDynamicStream *stream, const char *data, guint32 len)
{
	guint32 idx;
	
	make_room_in_stream (stream, stream->index + len);
	memcpy (stream->data + stream->index, data, len);
	idx = stream->index;
	stream->index += len;
	/* 
	 * align index? Not without adding an additional param that controls it since
	 * we may store a blob value in pieces.
	 */
	return idx;
}

static guint32
mono_image_add_stream_zero (MonoDynamicStream *stream, guint32 len)
{
	guint32 idx;
	
	make_room_in_stream (stream, stream->index + len);
	memset (stream->data + stream->index, 0, len);
	idx = stream->index;
	stream->index += len;
	return idx;
}

static void
stream_data_align (MonoDynamicStream *stream)
{
	char buf [4] = {0};
	guint32 count = stream->index % 4;

	/* we assume the stream data will be aligned */
	if (count)
		mono_image_add_stream_data (stream, buf, 4 - count);
}

#ifndef DISABLE_REFLECTION_EMIT
static int
mono_blob_entry_hash (const char* str)
{
	guint len, h;
	const char *end;
	len = mono_metadata_decode_blob_size (str, &str);
	if (len > 0) {
		end = str + len;
		h = *str;
		for (str += 1; str < end; str++)
			h = (h << 5) - h + *str;
		return h;
	} else {
		return 0;
	}
}

static gboolean
mono_blob_entry_equal (const char *str1, const char *str2) {
	int len, len2;
	const char *end1;
	const char *end2;
	len = mono_metadata_decode_blob_size (str1, &end1);
	len2 = mono_metadata_decode_blob_size (str2, &end2);
	if (len != len2)
		return 0;
	return memcmp (end1, end2, len) == 0;
}
#endif
static guint32
add_to_blob_cached (MonoDynamicImage *assembly, char *b1, int s1, char *b2, int s2)
{
	guint32 idx;
	char *copy;
	gpointer oldkey, oldval;

	copy = g_malloc (s1+s2);
	memcpy (copy, b1, s1);
	memcpy (copy + s1, b2, s2);
	if (g_hash_table_lookup_extended (assembly->blob_cache, copy, &oldkey, &oldval)) {
		g_free (copy);
		idx = GPOINTER_TO_UINT (oldval);
	} else {
		idx = mono_image_add_stream_data (&assembly->blob, b1, s1);
		mono_image_add_stream_data (&assembly->blob, b2, s2);
		g_hash_table_insert (assembly->blob_cache, copy, GUINT_TO_POINTER (idx));
	}
	return idx;
}

static guint32
sigbuffer_add_to_blob_cached (MonoDynamicImage *assembly, SigBuffer *buf)
{
	char blob_size [8];
	char *b = blob_size;
	guint32 size = buf->p - buf->buf;
	/* store length */
	g_assert (size <= (buf->end - buf->buf));
	mono_metadata_encode_value (size, b, &b);
	return add_to_blob_cached (assembly, blob_size, b-blob_size, buf->buf, size);
}

/*
 * Copy len * nelem bytes from val to dest, swapping bytes to LE if necessary.
 * dest may be misaligned.
 */
static void
swap_with_size (char *dest, const char* val, int len, int nelem) {
#if G_BYTE_ORDER != G_LITTLE_ENDIAN
	int elem;

	for (elem = 0; elem < nelem; ++elem) {
		switch (len) {
		case 1:
			*dest = *val;
			break;
		case 2:
			dest [0] = val [1];
			dest [1] = val [0];
			break;
		case 4:
			dest [0] = val [3];
			dest [1] = val [2];
			dest [2] = val [1];
			dest [3] = val [0];
			break;
		case 8:
			dest [0] = val [7];
			dest [1] = val [6];
			dest [2] = val [5];
			dest [3] = val [4];
			dest [4] = val [3];
			dest [5] = val [2];
			dest [6] = val [1];
			dest [7] = val [0];
			break;
		default:
			g_assert_not_reached ();
		}
		dest += len;
		val += len;
	}
#else
	memcpy (dest, val, len * nelem);
#endif
}

static guint32
add_mono_string_to_blob_cached (MonoDynamicImage *assembly, MonoString *str)
{
	char blob_size [64];
	char *b = blob_size;
	guint32 idx = 0, len;

	len = str->length * 2;
	mono_metadata_encode_value (len, b, &b);
#if G_BYTE_ORDER != G_LITTLE_ENDIAN
	{
		char *swapped = g_malloc (2 * mono_string_length (str));
		const char *p = (const char*)mono_string_chars (str);

		swap_with_size (swapped, p, 2, mono_string_length (str));
		idx = add_to_blob_cached (assembly, blob_size, b-blob_size, swapped, len);
		g_free (swapped);
	}
#else
	idx = add_to_blob_cached (assembly, blob_size, b-blob_size, (char*)mono_string_chars (str), len);
#endif
	return idx;
}

#ifndef DISABLE_REFLECTION_EMIT
static MonoClass *
default_class_from_mono_type (MonoType *type)
{
	switch (type->type) {
	case MONO_TYPE_OBJECT:
		return mono_defaults.object_class;
	case MONO_TYPE_VOID:
		return mono_defaults.void_class;
	case MONO_TYPE_BOOLEAN:
		return mono_defaults.boolean_class;
	case MONO_TYPE_CHAR:
		return mono_defaults.char_class;
	case MONO_TYPE_I1:
		return mono_defaults.sbyte_class;
	case MONO_TYPE_U1:
		return mono_defaults.byte_class;
	case MONO_TYPE_I2:
		return mono_defaults.int16_class;
	case MONO_TYPE_U2:
		return mono_defaults.uint16_class;
	case MONO_TYPE_I4:
		return mono_defaults.int32_class;
	case MONO_TYPE_U4:
		return mono_defaults.uint32_class;
	case MONO_TYPE_I:
		return mono_defaults.int_class;
	case MONO_TYPE_U:
		return mono_defaults.uint_class;
	case MONO_TYPE_I8:
		return mono_defaults.int64_class;
	case MONO_TYPE_U8:
		return mono_defaults.uint64_class;
	case MONO_TYPE_R4:
		return mono_defaults.single_class;
	case MONO_TYPE_R8:
		return mono_defaults.double_class;
	case MONO_TYPE_STRING:
		return mono_defaults.string_class;
	default:
		g_warning ("default_class_from_mono_type: implement me 0x%02x\n", type->type);
		g_assert_not_reached ();
	}
	
	return NULL;
}
#endif

/*
 * mono_class_get_ref_info:
 *
 *   Return the type builder/generic param builder corresponding to KLASS, if it exists.
 */
gpointer
mono_class_get_ref_info (MonoClass *klass)
{
	if (klass->ref_info_handle == 0)
		return NULL;
	else
		return mono_gchandle_get_target (klass->ref_info_handle);
}

void
mono_class_set_ref_info (MonoClass *klass, gpointer obj)
{
	klass->ref_info_handle = mono_gchandle_new ((MonoObject*)obj, FALSE);
	g_assert (klass->ref_info_handle != 0);
}

void
mono_class_free_ref_info (MonoClass *klass)
{
	if (klass->ref_info_handle) {
		mono_gchandle_free (klass->ref_info_handle);
		klass->ref_info_handle = 0;
	}
}

static void
encode_generic_class (MonoDynamicImage *assembly, MonoGenericClass *gclass, SigBuffer *buf)
{
	int i;
	MonoGenericInst *class_inst;
	MonoClass *klass;

	g_assert (gclass);

	class_inst = gclass->context.class_inst;

	sigbuffer_add_value (buf, MONO_TYPE_GENERICINST);
	klass = gclass->container_class;
	sigbuffer_add_value (buf, klass->byval_arg.type);
	sigbuffer_add_value (buf, mono_image_typedef_or_ref_full (assembly, &klass->byval_arg, FALSE));

	sigbuffer_add_value (buf, class_inst->type_argc);
	for (i = 0; i < class_inst->type_argc; ++i)
		encode_type (assembly, class_inst->type_argv [i], buf);

}

static void
encode_type (MonoDynamicImage *assembly, MonoType *type, SigBuffer *buf)
{
	if (!type) {
		g_assert_not_reached ();
		return;
	}
		
	if (type->byref)
		sigbuffer_add_value (buf, MONO_TYPE_BYREF);

	switch (type->type){
	case MONO_TYPE_VOID:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_TYPEDBYREF:
		sigbuffer_add_value (buf, type->type);
		break;
	case MONO_TYPE_PTR:
		sigbuffer_add_value (buf, type->type);
		encode_type (assembly, type->data.type, buf);
		break;
	case MONO_TYPE_SZARRAY:
		sigbuffer_add_value (buf, type->type);
		encode_type (assembly, &type->data.klass->byval_arg, buf);
		break;
	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_CLASS: {
		MonoClass *k = mono_class_from_mono_type (type);

		if (k->generic_container) {
			MonoGenericClass *gclass = mono_metadata_lookup_generic_class (k, k->generic_container->context.class_inst, TRUE);
			encode_generic_class (assembly, gclass, buf);
		} else {
			/*
			 * Make sure we use the correct type.
			 */
			sigbuffer_add_value (buf, k->byval_arg.type);
			/*
			 * ensure only non-byref gets passed to mono_image_typedef_or_ref(),
			 * otherwise two typerefs could point to the same type, leading to
			 * verification errors.
			 */
			sigbuffer_add_value (buf, mono_image_typedef_or_ref (assembly, &k->byval_arg));
		}
		break;
	}
	case MONO_TYPE_ARRAY:
		sigbuffer_add_value (buf, type->type);
		encode_type (assembly, &type->data.array->eklass->byval_arg, buf);
		sigbuffer_add_value (buf, type->data.array->rank);
		sigbuffer_add_value (buf, 0); /* FIXME: set to 0 for now */
		sigbuffer_add_value (buf, 0);
		break;
	case MONO_TYPE_GENERICINST:
		encode_generic_class (assembly, type->data.generic_class, buf);
		break;
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		sigbuffer_add_value (buf, type->type);
		sigbuffer_add_value (buf, mono_type_get_generic_param_num (type));
		break;
	default:
		g_error ("need to encode type %x", type->type);
	}
}

static void
encode_reflection_type (MonoDynamicImage *assembly, MonoReflectionType *type, SigBuffer *buf)
{
	if (!type) {
		sigbuffer_add_value (buf, MONO_TYPE_VOID);
		return;
	}

	encode_type (assembly, mono_reflection_type_get_handle (type), buf);
}

static void
encode_custom_modifiers (MonoDynamicImage *assembly, MonoArray *modreq, MonoArray *modopt, SigBuffer *buf)
{
	int i;

	if (modreq) {
		for (i = 0; i < mono_array_length (modreq); ++i) {
			MonoType *mod = mono_type_array_get_and_resolve (modreq, i);
			sigbuffer_add_byte (buf, MONO_TYPE_CMOD_REQD);
			sigbuffer_add_value (buf, mono_image_typedef_or_ref (assembly, mod));
		}
	}
	if (modopt) {
		for (i = 0; i < mono_array_length (modopt); ++i) {
			MonoType *mod = mono_type_array_get_and_resolve (modopt, i);
			sigbuffer_add_byte (buf, MONO_TYPE_CMOD_OPT);
			sigbuffer_add_value (buf, mono_image_typedef_or_ref (assembly, mod));
		}
	}
}

#ifndef DISABLE_REFLECTION_EMIT
static guint32
method_encode_signature (MonoDynamicImage *assembly, MonoMethodSignature *sig)
{
	SigBuffer buf;
	int i;
	guint32 nparams =  sig->param_count;
	guint32 idx;

	if (!assembly->save)
		return 0;

	sigbuffer_init (&buf, 32);
	/*
	 * FIXME: vararg, explicit_this, differenc call_conv values...
	 */
	idx = sig->call_convention;
	if (sig->hasthis)
		idx |= 0x20; /* hasthis */
	if (sig->generic_param_count)
		idx |= 0x10; /* generic */
	sigbuffer_add_byte (&buf, idx);
	if (sig->generic_param_count)
		sigbuffer_add_value (&buf, sig->generic_param_count);
	sigbuffer_add_value (&buf, nparams);
	encode_type (assembly, sig->ret, &buf);
	for (i = 0; i < nparams; ++i) {
		if (i == sig->sentinelpos)
			sigbuffer_add_byte (&buf, MONO_TYPE_SENTINEL);
		encode_type (assembly, sig->params [i], &buf);
	}
	idx = sigbuffer_add_to_blob_cached (assembly, &buf);
	sigbuffer_free (&buf);
	return idx;
}
#endif

static guint32
method_builder_encode_signature (MonoDynamicImage *assembly, ReflectionMethodBuilder *mb)
{
	/*
	 * FIXME: reuse code from method_encode_signature().
	 */
	SigBuffer buf;
	int i;
	guint32 nparams =  mb->parameters ? mono_array_length (mb->parameters): 0;
	guint32 ngparams = mb->generic_params ? mono_array_length (mb->generic_params): 0;
	guint32 notypes = mb->opt_types ? mono_array_length (mb->opt_types): 0;
	guint32 idx;

	sigbuffer_init (&buf, 32);
	/* LAMESPEC: all the call conv spec is foobared */
	idx = mb->call_conv & 0x60; /* has-this, explicit-this */
	if (mb->call_conv & 2)
		idx |= 0x5; /* vararg */
	if (!(mb->attrs & METHOD_ATTRIBUTE_STATIC))
		idx |= 0x20; /* hasthis */
	if (ngparams)
		idx |= 0x10; /* generic */
	sigbuffer_add_byte (&buf, idx);
	if (ngparams)
		sigbuffer_add_value (&buf, ngparams);
	sigbuffer_add_value (&buf, nparams + notypes);
	encode_custom_modifiers (assembly, mb->return_modreq, mb->return_modopt, &buf);
	encode_reflection_type (assembly, mb->rtype, &buf);
	for (i = 0; i < nparams; ++i) {
		MonoArray *modreq = NULL;
		MonoArray *modopt = NULL;
		MonoReflectionType *pt;

		if (mb->param_modreq && (i < mono_array_length (mb->param_modreq)))
			modreq = mono_array_get (mb->param_modreq, MonoArray*, i);
		if (mb->param_modopt && (i < mono_array_length (mb->param_modopt)))
			modopt = mono_array_get (mb->param_modopt, MonoArray*, i);
		encode_custom_modifiers (assembly, modreq, modopt, &buf);
		pt = mono_array_get (mb->parameters, MonoReflectionType*, i);
		encode_reflection_type (assembly, pt, &buf);
	}
	if (notypes)
		sigbuffer_add_byte (&buf, MONO_TYPE_SENTINEL);
	for (i = 0; i < notypes; ++i) {
		MonoReflectionType *pt;

		pt = mono_array_get (mb->opt_types, MonoReflectionType*, i);
		encode_reflection_type (assembly, pt, &buf);
	}

	idx = sigbuffer_add_to_blob_cached (assembly, &buf);
	sigbuffer_free (&buf);
	return idx;
}

static guint32
encode_locals (MonoDynamicImage *assembly, MonoReflectionILGen *ilgen)
{
	MonoDynamicTable *table;
	guint32 *values;
	guint32 idx, sig_idx;
	guint nl = mono_array_length (ilgen->locals);
	SigBuffer buf;
	int i;

	sigbuffer_init (&buf, 32);
	sigbuffer_add_value (&buf, 0x07);
	sigbuffer_add_value (&buf, nl);
	for (i = 0; i < nl; ++i) {
		MonoReflectionLocalBuilder *lb = mono_array_get (ilgen->locals, MonoReflectionLocalBuilder*, i);
		
		if (lb->is_pinned)
			sigbuffer_add_value (&buf, MONO_TYPE_PINNED);
		
		encode_reflection_type (assembly, (MonoReflectionType*)lb->type, &buf);
	}
	sig_idx = sigbuffer_add_to_blob_cached (assembly, &buf);
	sigbuffer_free (&buf);

	if (assembly->standalonesig_cache == NULL)
		assembly->standalonesig_cache = g_hash_table_new (NULL, NULL);
	idx = GPOINTER_TO_UINT (g_hash_table_lookup (assembly->standalonesig_cache, GUINT_TO_POINTER (sig_idx)));
	if (idx)
		return idx;

	table = &assembly->tables [MONO_TABLE_STANDALONESIG];
	idx = table->next_idx ++;
	table->rows ++;
	alloc_table (table, table->rows);
	values = table->values + idx * MONO_STAND_ALONE_SIGNATURE_SIZE;

	values [MONO_STAND_ALONE_SIGNATURE] = sig_idx;

	g_hash_table_insert (assembly->standalonesig_cache, GUINT_TO_POINTER (sig_idx), GUINT_TO_POINTER (idx));

	return idx;
}

static guint32
method_count_clauses (MonoReflectionILGen *ilgen)
{
	guint32 num_clauses = 0;
	int i;

	MonoILExceptionInfo *ex_info;
	for (i = 0; i < mono_array_length (ilgen->ex_handlers); ++i) {
		ex_info = (MonoILExceptionInfo*)mono_array_addr (ilgen->ex_handlers, MonoILExceptionInfo, i);
		if (ex_info->handlers)
			num_clauses += mono_array_length (ex_info->handlers);
		else
			num_clauses++;
	}

	return num_clauses;
}

#ifndef DISABLE_REFLECTION_EMIT
static MonoExceptionClause*
method_encode_clauses (MonoImage *image, MonoDynamicImage *assembly, MonoReflectionILGen *ilgen, guint32 num_clauses)
{
	MonoExceptionClause *clauses;
	MonoExceptionClause *clause;
	MonoILExceptionInfo *ex_info;
	MonoILExceptionBlock *ex_block;
	guint32 finally_start;
	int i, j, clause_index;;

	clauses = image_g_new0 (image, MonoExceptionClause, num_clauses);

	clause_index = 0;
	for (i = mono_array_length (ilgen->ex_handlers) - 1; i >= 0; --i) {
		ex_info = (MonoILExceptionInfo*)mono_array_addr (ilgen->ex_handlers, MonoILExceptionInfo, i);
		finally_start = ex_info->start + ex_info->len;
		if (!ex_info->handlers)
			continue;
		for (j = 0; j < mono_array_length (ex_info->handlers); ++j) {
			ex_block = (MonoILExceptionBlock*)mono_array_addr (ex_info->handlers, MonoILExceptionBlock, j);
			clause = &(clauses [clause_index]);

			clause->flags = ex_block->type;
			clause->try_offset = ex_info->start;

			if (ex_block->type == MONO_EXCEPTION_CLAUSE_FINALLY)
				clause->try_len = finally_start - ex_info->start;
			else
				clause->try_len = ex_info->len;
			clause->handler_offset = ex_block->start;
			clause->handler_len = ex_block->len;
			if (ex_block->extype) {
				clause->data.catch_class = mono_class_from_mono_type (mono_reflection_type_get_handle ((MonoReflectionType*)ex_block->extype));
			} else {
				if (ex_block->type == MONO_EXCEPTION_CLAUSE_FILTER)
					clause->data.filter_offset = ex_block->filter_offset;
				else
					clause->data.filter_offset = 0;
			}
			finally_start = ex_block->start + ex_block->len;

			clause_index ++;
		}
	}

	return clauses;
}
#endif /* !DISABLE_REFLECTION_EMIT */

static guint32
method_encode_code (MonoDynamicImage *assembly, ReflectionMethodBuilder *mb)
{
	char flags = 0;
	guint32 idx;
	guint32 code_size;
	gint32 max_stack, i;
	gint32 num_locals = 0;
	gint32 num_exception = 0;
	gint maybe_small;
	guint32 fat_flags;
	char fat_header [12];
	guint32 int_value;
	guint16 short_value;
	guint32 local_sig = 0;
	guint32 header_size = 12;
	MonoArray *code;

	if ((mb->attrs & (METHOD_ATTRIBUTE_PINVOKE_IMPL | METHOD_ATTRIBUTE_ABSTRACT)) ||
			(mb->iattrs & (METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL | METHOD_IMPL_ATTRIBUTE_RUNTIME)))
		return 0;

	/*if (mb->name)
		g_print ("Encode method %s\n", mono_string_to_utf8 (mb->name));*/
	if (mb->ilgen) {
		code = mb->ilgen->code;
		code_size = mb->ilgen->code_len;
		max_stack = mb->ilgen->max_stack;
		num_locals = mb->ilgen->locals ? mono_array_length (mb->ilgen->locals) : 0;
		if (mb->ilgen->ex_handlers)
			num_exception = method_count_clauses (mb->ilgen);
	} else {
		code = mb->code;
		if (code == NULL){
			char *name = mono_string_to_utf8 (mb->name);
			char *str = g_strdup_printf ("Method %s does not have any IL associated", name);
			MonoException *exception = mono_get_exception_argument (NULL, "a method does not have any IL associated");
			g_free (str);
			g_free (name);
			mono_raise_exception (exception);
		}

		code_size = mono_array_length (code);
		max_stack = 8; /* we probably need to run a verifier on the code... */
	}

	stream_data_align (&assembly->code);

	/* check for exceptions, maxstack, locals */
	maybe_small = (max_stack <= 8) && (!num_locals) && (!num_exception);
	if (maybe_small) {
		if (code_size < 64 && !(code_size & 1)) {
			flags = (code_size << 2) | 0x2;
		} else if (code_size < 32 && (code_size & 1)) {
			flags = (code_size << 2) | 0x6; /* LAMESPEC: see metadata.c */
		} else {
			goto fat_header;
		}
		idx = mono_image_add_stream_data (&assembly->code, &flags, 1);
		/* add to the fixup todo list */
		if (mb->ilgen && mb->ilgen->num_token_fixups)
			mono_g_hash_table_insert (assembly->token_fixups, mb->ilgen, GUINT_TO_POINTER (idx + 1));
		mono_image_add_stream_data (&assembly->code, mono_array_addr (code, char, 0), code_size);
		return assembly->text_rva + idx;
	} 
fat_header:
	if (num_locals)
		local_sig = MONO_TOKEN_SIGNATURE | encode_locals (assembly, mb->ilgen);
	/* 
	 * FIXME: need to set also the header size in fat_flags.
	 * (and more sects and init locals flags)
	 */
	fat_flags =  0x03;
	if (num_exception)
		fat_flags |= METHOD_HEADER_MORE_SECTS;
	if (mb->init_locals)
		fat_flags |= METHOD_HEADER_INIT_LOCALS;
	fat_header [0] = fat_flags;
	fat_header [1] = (header_size / 4 ) << 4;
	short_value = GUINT16_TO_LE (max_stack);
	memcpy (fat_header + 2, &short_value, 2);
	int_value = GUINT32_TO_LE (code_size);
	memcpy (fat_header + 4, &int_value, 4);
	int_value = GUINT32_TO_LE (local_sig);
	memcpy (fat_header + 8, &int_value, 4);
	idx = mono_image_add_stream_data (&assembly->code, fat_header, 12);
	/* add to the fixup todo list */
	if (mb->ilgen && mb->ilgen->num_token_fixups)
		mono_g_hash_table_insert (assembly->token_fixups, mb->ilgen, GUINT_TO_POINTER (idx + 12));
	
	mono_image_add_stream_data (&assembly->code, mono_array_addr (code, char, 0), code_size);
	if (num_exception) {
		unsigned char sheader [4];
		MonoILExceptionInfo * ex_info;
		MonoILExceptionBlock * ex_block;
		int j;

		stream_data_align (&assembly->code);
		/* always use fat format for now */
		sheader [0] = METHOD_HEADER_SECTION_FAT_FORMAT | METHOD_HEADER_SECTION_EHTABLE;
		num_exception *= 6 * sizeof (guint32);
		num_exception += 4; /* include the size of the header */
		sheader [1] = num_exception & 0xff;
		sheader [2] = (num_exception >> 8) & 0xff;
		sheader [3] = (num_exception >> 16) & 0xff;
		mono_image_add_stream_data (&assembly->code, (char*)sheader, 4);
		/* fat header, so we are already aligned */
		/* reverse order */
		for (i = mono_array_length (mb->ilgen->ex_handlers) - 1; i >= 0; --i) {
			ex_info = (MonoILExceptionInfo *)mono_array_addr (mb->ilgen->ex_handlers, MonoILExceptionInfo, i);
			if (ex_info->handlers) {
				int finally_start = ex_info->start + ex_info->len;
				for (j = 0; j < mono_array_length (ex_info->handlers); ++j) {
					guint32 val;
					ex_block = (MonoILExceptionBlock*)mono_array_addr (ex_info->handlers, MonoILExceptionBlock, j);
					/* the flags */
					val = GUINT32_TO_LE (ex_block->type);
					mono_image_add_stream_data (&assembly->code, (char*)&val, sizeof (guint32));
					/* try offset */
					val = GUINT32_TO_LE (ex_info->start);
					mono_image_add_stream_data (&assembly->code, (char*)&val, sizeof (guint32));
					/* need fault, too, probably */
					if (ex_block->type == MONO_EXCEPTION_CLAUSE_FINALLY)
						val = GUINT32_TO_LE (finally_start - ex_info->start);
					else
						val = GUINT32_TO_LE (ex_info->len);
					mono_image_add_stream_data (&assembly->code, (char*)&val, sizeof (guint32));
					/* handler offset */
					val = GUINT32_TO_LE (ex_block->start);
					mono_image_add_stream_data (&assembly->code, (char*)&val, sizeof (guint32));
					/* handler len */
					val = GUINT32_TO_LE (ex_block->len);
					mono_image_add_stream_data (&assembly->code, (char*)&val, sizeof (guint32));
					finally_start = ex_block->start + ex_block->len;
					if (ex_block->extype) {
						val = mono_metadata_token_from_dor (mono_image_typedef_or_ref (assembly, mono_reflection_type_get_handle ((MonoReflectionType*)ex_block->extype)));
					} else {
						if (ex_block->type == MONO_EXCEPTION_CLAUSE_FILTER)
							val = ex_block->filter_offset;
						else
							val = 0;
					}
					val = GUINT32_TO_LE (val);
					mono_image_add_stream_data (&assembly->code, (char*)&val, sizeof (guint32));
					/*g_print ("out clause %d: from %d len=%d, handler at %d, %d, finally_start=%d, ex_info->start=%d, ex_info->len=%d, ex_block->type=%d, j=%d, i=%d\n", 
							clause.flags, clause.try_offset, clause.try_len, clause.handler_offset, clause.handler_len, finally_start, ex_info->start, ex_info->len, ex_block->type, j, i);*/
				}
			} else {
				g_error ("No clauses for ex info block %d", i);
			}
		}
	}
	return assembly->text_rva + idx;
}

static guint32
find_index_in_table (MonoDynamicImage *assembly, int table_idx, int col, guint32 token)
{
	int i;
	MonoDynamicTable *table;
	guint32 *values;
	
	table = &assembly->tables [table_idx];

	g_assert (col < table->columns);

	values = table->values + table->columns;
	for (i = 1; i <= table->rows; ++i) {
		if (values [col] == token)
			return i;
		values += table->columns;
	}
	return 0;
}

/*
 * LOCKING: Acquires the loader lock. 
 */
static MonoCustomAttrInfo*
lookup_custom_attr (MonoImage *image, gpointer member)
{
	MonoCustomAttrInfo* res;

	res = mono_image_property_lookup (image, member, MONO_PROP_DYNAMIC_CATTR);

	if (!res)
		return NULL;

	res = g_memdup (res, MONO_SIZEOF_CUSTOM_ATTR_INFO + sizeof (MonoCustomAttrEntry) * res->num_attrs);
	res->cached = 0;
	return res;
}

static gboolean
custom_attr_visible (MonoImage *image, MonoReflectionCustomAttr *cattr)
{
	/* FIXME: Need to do more checks */
	if (cattr->ctor->method && (cattr->ctor->method->klass->image != image)) {
		int visibility = cattr->ctor->method->klass->flags & TYPE_ATTRIBUTE_VISIBILITY_MASK;

		if ((visibility != TYPE_ATTRIBUTE_PUBLIC) && (visibility != TYPE_ATTRIBUTE_NESTED_PUBLIC))
			return FALSE;
	}

	return TRUE;
}

static MonoCustomAttrInfo*
mono_custom_attrs_from_builders (MonoImage *alloc_img, MonoImage *image, MonoArray *cattrs)
{
	int i, index, count, not_visible;
	MonoCustomAttrInfo *ainfo;
	MonoReflectionCustomAttr *cattr;

	if (!cattrs)
		return NULL;
	/* FIXME: check in assembly the Run flag is set */

	count = mono_array_length (cattrs);

	/* Skip nonpublic attributes since MS.NET seems to do the same */
	/* FIXME: This needs to be done more globally */
	not_visible = 0;
	for (i = 0; i < count; ++i) {
		cattr = (MonoReflectionCustomAttr*)mono_array_get (cattrs, gpointer, i);
		if (!custom_attr_visible (image, cattr))
			not_visible ++;
	}
	count -= not_visible;

	ainfo = image_g_malloc0 (alloc_img, MONO_SIZEOF_CUSTOM_ATTR_INFO + sizeof (MonoCustomAttrEntry) * count);

	ainfo->image = image;
	ainfo->num_attrs = count;
	ainfo->cached = alloc_img != NULL;
	index = 0;
	for (i = 0; i < count; ++i) {
		cattr = (MonoReflectionCustomAttr*)mono_array_get (cattrs, gpointer, i);
		if (custom_attr_visible (image, cattr)) {
			unsigned char *saved = mono_image_alloc (image, mono_array_length (cattr->data));
			memcpy (saved, mono_array_addr (cattr->data, char, 0), mono_array_length (cattr->data));
			ainfo->attrs [index].ctor = cattr->ctor->method;
			ainfo->attrs [index].data = saved;
			ainfo->attrs [index].data_size = mono_array_length (cattr->data);
			index ++;
		}
	}

	return ainfo;
}

#ifndef DISABLE_REFLECTION_EMIT
/*
 * LOCKING: Acquires the loader lock. 
 */
static void
mono_save_custom_attrs (MonoImage *image, void *obj, MonoArray *cattrs)
{
	MonoCustomAttrInfo *ainfo, *tmp;

	if (!cattrs || !mono_array_length (cattrs))
		return;

	ainfo = mono_custom_attrs_from_builders (image, image, cattrs);

	mono_loader_lock ();
	tmp = mono_image_property_lookup (image, obj, MONO_PROP_DYNAMIC_CATTR);
	if (tmp)
		mono_custom_attrs_free (tmp);
	mono_image_property_insert (image, obj, MONO_PROP_DYNAMIC_CATTR, ainfo);
	mono_loader_unlock ();

}
#endif

void
mono_custom_attrs_free (MonoCustomAttrInfo *ainfo)
{
	if (!ainfo->cached)
		g_free (ainfo);
}

/*
 * idx is the table index of the object
 * type is one of MONO_CUSTOM_ATTR_*
 */
static void
mono_image_add_cattrs (MonoDynamicImage *assembly, guint32 idx, guint32 type, MonoArray *cattrs)
{
	MonoDynamicTable *table;
	MonoReflectionCustomAttr *cattr;
	guint32 *values;
	guint32 count, i, token;
	char blob_size [6];
	char *p = blob_size;
	
	/* it is legal to pass a NULL cattrs: we avoid to use the if in a lot of places */
	if (!cattrs)
		return;
	count = mono_array_length (cattrs);
	table = &assembly->tables [MONO_TABLE_CUSTOMATTRIBUTE];
	table->rows += count;
	alloc_table (table, table->rows);
	values = table->values + table->next_idx * MONO_CUSTOM_ATTR_SIZE;
	idx <<= MONO_CUSTOM_ATTR_BITS;
	idx |= type;
	for (i = 0; i < count; ++i) {
		cattr = (MonoReflectionCustomAttr*)mono_array_get (cattrs, gpointer, i);
		values [MONO_CUSTOM_ATTR_PARENT] = idx;
		token = mono_image_create_token (assembly, (MonoObject*)cattr->ctor, FALSE, FALSE);
		type = mono_metadata_token_index (token);
		type <<= MONO_CUSTOM_ATTR_TYPE_BITS;
		switch (mono_metadata_token_table (token)) {
		case MONO_TABLE_METHOD:
			type |= MONO_CUSTOM_ATTR_TYPE_METHODDEF;
			/*
			 * fixup_cattrs () needs to fix this up. We can't use image->tokens, since it contains the old token for the
			 * method, not the one returned by mono_image_create_token ().
			 */
			mono_g_hash_table_insert (assembly->remapped_tokens, GUINT_TO_POINTER (token), cattr->ctor);
			break;
		case MONO_TABLE_MEMBERREF:
			type |= MONO_CUSTOM_ATTR_TYPE_MEMBERREF;
			break;
		default:
			g_warning ("got wrong token in custom attr");
			continue;
		}
		values [MONO_CUSTOM_ATTR_TYPE] = type;
		p = blob_size;
		mono_metadata_encode_value (mono_array_length (cattr->data), p, &p);
		values [MONO_CUSTOM_ATTR_VALUE] = add_to_blob_cached (assembly, blob_size, p - blob_size,
			mono_array_addr (cattr->data, char, 0), mono_array_length (cattr->data));
		values += MONO_CUSTOM_ATTR_SIZE;
		++table->next_idx;
	}
}

static void
mono_image_add_decl_security (MonoDynamicImage *assembly, guint32 parent_token, MonoArray *permissions)
{
	MonoDynamicTable *table;
	guint32 *values;
	guint32 count, i, idx;
	MonoReflectionPermissionSet *perm;

	if (!permissions)
		return;

	count = mono_array_length (permissions);
	table = &assembly->tables [MONO_TABLE_DECLSECURITY];
	table->rows += count;
	alloc_table (table, table->rows);

	for (i = 0; i < mono_array_length (permissions); ++i) {
		perm = (MonoReflectionPermissionSet*)mono_array_addr (permissions, MonoReflectionPermissionSet, i);

		values = table->values + table->next_idx * MONO_DECL_SECURITY_SIZE;

		idx = mono_metadata_token_index (parent_token);
		idx <<= MONO_HAS_DECL_SECURITY_BITS;
		switch (mono_metadata_token_table (parent_token)) {
		case MONO_TABLE_TYPEDEF:
			idx |= MONO_HAS_DECL_SECURITY_TYPEDEF;
			break;
		case MONO_TABLE_METHOD:
			idx |= MONO_HAS_DECL_SECURITY_METHODDEF;
			break;
		case MONO_TABLE_ASSEMBLY:
			idx |= MONO_HAS_DECL_SECURITY_ASSEMBLY;
			break;
		default:
			g_assert_not_reached ();
		}

		values [MONO_DECL_SECURITY_ACTION] = perm->action;
		values [MONO_DECL_SECURITY_PARENT] = idx;
		values [MONO_DECL_SECURITY_PERMISSIONSET] = add_mono_string_to_blob_cached (assembly, perm->pset);

		++table->next_idx;
	}
}

/*
 * Fill in the MethodDef and ParamDef tables for a method.
 * This is used for both normal methods and constructors.
 */
static void
mono_image_basic_method (ReflectionMethodBuilder *mb, MonoDynamicImage *assembly)
{
	MonoDynamicTable *table;
	guint32 *values;
	guint i, count;

	/* room in this table is already allocated */
	table = &assembly->tables [MONO_TABLE_METHOD];
	*mb->table_idx = table->next_idx ++;
	g_hash_table_insert (assembly->method_to_table_idx, mb->mhandle, GUINT_TO_POINTER ((*mb->table_idx)));
	values = table->values + *mb->table_idx * MONO_METHOD_SIZE;
	values [MONO_METHOD_NAME] = string_heap_insert_mstring (&assembly->sheap, mb->name);
	values [MONO_METHOD_FLAGS] = mb->attrs;
	values [MONO_METHOD_IMPLFLAGS] = mb->iattrs;
	values [MONO_METHOD_SIGNATURE] = method_builder_encode_signature (assembly, mb);
	values [MONO_METHOD_RVA] = method_encode_code (assembly, mb);
	
	table = &assembly->tables [MONO_TABLE_PARAM];
	values [MONO_METHOD_PARAMLIST] = table->next_idx;

	mono_image_add_decl_security (assembly, 
		mono_metadata_make_token (MONO_TABLE_METHOD, *mb->table_idx), mb->permissions);

	if (mb->pinfo) {
		MonoDynamicTable *mtable;
		guint32 *mvalues;
		
		mtable = &assembly->tables [MONO_TABLE_FIELDMARSHAL];
		mvalues = mtable->values + mtable->next_idx * MONO_FIELD_MARSHAL_SIZE;
		
		count = 0;
		for (i = 0; i < mono_array_length (mb->pinfo); ++i) {
			if (mono_array_get (mb->pinfo, gpointer, i))
				count++;
		}
		table->rows += count;
		alloc_table (table, table->rows);
		values = table->values + table->next_idx * MONO_PARAM_SIZE;
		for (i = 0; i < mono_array_length (mb->pinfo); ++i) {
			MonoReflectionParamBuilder *pb;
			if ((pb = mono_array_get (mb->pinfo, MonoReflectionParamBuilder*, i))) {
				values [MONO_PARAM_FLAGS] = pb->attrs;
				values [MONO_PARAM_SEQUENCE] = i;
				if (pb->name != NULL) {
					values [MONO_PARAM_NAME] = string_heap_insert_mstring (&assembly->sheap, pb->name);
				} else {
					values [MONO_PARAM_NAME] = 0;
				}
				values += MONO_PARAM_SIZE;
				if (pb->marshal_info) {
					mtable->rows++;
					alloc_table (mtable, mtable->rows);
					mvalues = mtable->values + mtable->rows * MONO_FIELD_MARSHAL_SIZE;
					mvalues [MONO_FIELD_MARSHAL_PARENT] = (table->next_idx << MONO_HAS_FIELD_MARSHAL_BITS) | MONO_HAS_FIELD_MARSHAL_PARAMDEF;
					mvalues [MONO_FIELD_MARSHAL_NATIVE_TYPE] = encode_marshal_blob (assembly, pb->marshal_info);
				}
				pb->table_idx = table->next_idx++;
				if (pb->attrs & PARAM_ATTRIBUTE_HAS_DEFAULT) {
					guint32 field_type = 0;
					mtable = &assembly->tables [MONO_TABLE_CONSTANT];
					mtable->rows ++;
					alloc_table (mtable, mtable->rows);
					mvalues = mtable->values + mtable->rows * MONO_CONSTANT_SIZE;
					mvalues [MONO_CONSTANT_PARENT] = MONO_HASCONSTANT_PARAM | (pb->table_idx << MONO_HASCONSTANT_BITS);
					mvalues [MONO_CONSTANT_VALUE] = encode_constant (assembly, pb->def_value, &field_type);
					mvalues [MONO_CONSTANT_TYPE] = field_type;
					mvalues [MONO_CONSTANT_PADDING] = 0;
				}
			}
		}
	}
}

#ifndef DISABLE_REFLECTION_EMIT
static void
reflection_methodbuilder_from_method_builder (ReflectionMethodBuilder *rmb, MonoReflectionMethodBuilder *mb)
{
	memset (rmb, 0, sizeof (ReflectionMethodBuilder));

	rmb->ilgen = mb->ilgen;
	rmb->rtype = mono_reflection_type_resolve_user_types ((MonoReflectionType*)mb->rtype);
	rmb->parameters = mb->parameters;
	rmb->generic_params = mb->generic_params;
	rmb->generic_container = mb->generic_container;
	rmb->opt_types = NULL;
	rmb->pinfo = mb->pinfo;
	rmb->attrs = mb->attrs;
	rmb->iattrs = mb->iattrs;
	rmb->call_conv = mb->call_conv;
	rmb->code = mb->code;
	rmb->type = mb->type;
	rmb->name = mb->name;
	rmb->table_idx = &mb->table_idx;
	rmb->init_locals = mb->init_locals;
	rmb->skip_visibility = FALSE;
	rmb->return_modreq = mb->return_modreq;
	rmb->return_modopt = mb->return_modopt;
	rmb->param_modreq = mb->param_modreq;
	rmb->param_modopt = mb->param_modopt;
	rmb->permissions = mb->permissions;
	rmb->mhandle = mb->mhandle;
	rmb->nrefs = 0;
	rmb->refs = NULL;

	if (mb->dll) {
		rmb->charset = mb->charset;
		rmb->extra_flags = mb->extra_flags;
		rmb->native_cc = mb->native_cc;
		rmb->dllentry = mb->dllentry;
		rmb->dll = mb->dll;
	}
}

static void
reflection_methodbuilder_from_ctor_builder (ReflectionMethodBuilder *rmb, MonoReflectionCtorBuilder *mb)
{
	const char *name = mb->attrs & METHOD_ATTRIBUTE_STATIC ? ".cctor": ".ctor";

	memset (rmb, 0, sizeof (ReflectionMethodBuilder));

	rmb->ilgen = mb->ilgen;
	rmb->rtype = mono_type_get_object (mono_domain_get (), &mono_defaults.void_class->byval_arg);
	rmb->parameters = mb->parameters;
	rmb->generic_params = NULL;
	rmb->generic_container = NULL;
	rmb->opt_types = NULL;
	rmb->pinfo = mb->pinfo;
	rmb->attrs = mb->attrs;
	rmb->iattrs = mb->iattrs;
	rmb->call_conv = mb->call_conv;
	rmb->code = NULL;
	rmb->type = mb->type;
	rmb->name = mono_string_new (mono_domain_get (), name);
	rmb->table_idx = &mb->table_idx;
	rmb->init_locals = mb->init_locals;
	rmb->skip_visibility = FALSE;
	rmb->return_modreq = NULL;
	rmb->return_modopt = NULL;
	rmb->param_modreq = mb->param_modreq;
	rmb->param_modopt = mb->param_modopt;
	rmb->permissions = mb->permissions;
	rmb->mhandle = mb->mhandle;
	rmb->nrefs = 0;
	rmb->refs = NULL;
}

static void
reflection_methodbuilder_from_dynamic_method (ReflectionMethodBuilder *rmb, MonoReflectionDynamicMethod *mb)
{
	memset (rmb, 0, sizeof (ReflectionMethodBuilder));

	rmb->ilgen = mb->ilgen;
	rmb->rtype = mb->rtype;
	rmb->parameters = mb->parameters;
	rmb->generic_params = NULL;
	rmb->generic_container = NULL;
	rmb->opt_types = NULL;
	rmb->pinfo = NULL;
	rmb->attrs = mb->attrs;
	rmb->iattrs = 0;
	rmb->call_conv = mb->call_conv;
	rmb->code = NULL;
	rmb->type = (MonoObject *) mb->owner;
	rmb->name = mb->name;
	rmb->table_idx = NULL;
	rmb->init_locals = mb->init_locals;
	rmb->skip_visibility = mb->skip_visibility;
	rmb->return_modreq = NULL;
	rmb->return_modopt = NULL;
	rmb->param_modreq = NULL;
	rmb->param_modopt = NULL;
	rmb->permissions = NULL;
	rmb->mhandle = mb->mhandle;
	rmb->nrefs = 0;
	rmb->refs = NULL;
}	
#endif

static void
mono_image_add_methodimpl (MonoDynamicImage *assembly, MonoReflectionMethodBuilder *mb)
{
	MonoReflectionTypeBuilder *tb = (MonoReflectionTypeBuilder *)mb->type;
	MonoDynamicTable *table;
	guint32 *values;
	guint32 tok;
	MonoReflectionMethod *m;
	int i;

	if (!mb->override_methods)
		return;

	for (i = 0; i < mono_array_length (mb->override_methods); ++i) {
		m = mono_array_get (mb->override_methods, MonoReflectionMethod*, i);

		table = &assembly->tables [MONO_TABLE_METHODIMPL];
		table->rows ++;
		alloc_table (table, table->rows);
		values = table->values + table->rows * MONO_METHODIMPL_SIZE;
		values [MONO_METHODIMPL_CLASS] = tb->table_idx;
		values [MONO_METHODIMPL_BODY] = MONO_METHODDEFORREF_METHODDEF | (mb->table_idx << MONO_METHODDEFORREF_BITS);

		tok = mono_image_create_token (assembly, (MonoObject*)m, FALSE, FALSE);
		switch (mono_metadata_token_table (tok)) {
		case MONO_TABLE_MEMBERREF:
			tok = (mono_metadata_token_index (tok) << MONO_METHODDEFORREF_BITS ) | MONO_METHODDEFORREF_METHODREF;
			break;
		case MONO_TABLE_METHOD:
			tok = (mono_metadata_token_index (tok) << MONO_METHODDEFORREF_BITS ) | MONO_METHODDEFORREF_METHODDEF;
			break;
		default:
			g_assert_not_reached ();
		}
		values [MONO_METHODIMPL_DECLARATION] = tok;
	}
}

#ifndef DISABLE_REFLECTION_EMIT
static void
mono_image_get_method_info (MonoReflectionMethodBuilder *mb, MonoDynamicImage *assembly)
{
	MonoDynamicTable *table;
	guint32 *values;
	ReflectionMethodBuilder rmb;
	int i;

	reflection_methodbuilder_from_method_builder (&rmb, mb);

	mono_image_basic_method (&rmb, assembly);
	mb->table_idx = *rmb.table_idx;

	if (mb->dll) { /* It's a P/Invoke method */
		guint32 moduleref;
		/* map CharSet values to on-disk values */
		int ncharset = (mb->charset ? (mb->charset - 1) * 2 : 0);
		int extra_flags = mb->extra_flags;
		table = &assembly->tables [MONO_TABLE_IMPLMAP];
		table->rows ++;
		alloc_table (table, table->rows);
		values = table->values + table->rows * MONO_IMPLMAP_SIZE;
		
		values [MONO_IMPLMAP_FLAGS] = (mb->native_cc << 8) | ncharset | extra_flags;
		values [MONO_IMPLMAP_MEMBER] = (mb->table_idx << 1) | 1; /* memberforwarded: method */
		if (mb->dllentry)
			values [MONO_IMPLMAP_NAME] = string_heap_insert_mstring (&assembly->sheap, mb->dllentry);
		else
			values [MONO_IMPLMAP_NAME] = string_heap_insert_mstring (&assembly->sheap, mb->name);
		moduleref = string_heap_insert_mstring (&assembly->sheap, mb->dll);
		if (!(values [MONO_IMPLMAP_SCOPE] = find_index_in_table (assembly, MONO_TABLE_MODULEREF, MONO_MODULEREF_NAME, moduleref))) {
			table = &assembly->tables [MONO_TABLE_MODULEREF];
			table->rows ++;
			alloc_table (table, table->rows);
			table->values [table->rows * MONO_MODULEREF_SIZE + MONO_MODULEREF_NAME] = moduleref;
			values [MONO_IMPLMAP_SCOPE] = table->rows;
		}
	}

	if (mb->generic_params) {
		table = &assembly->tables [MONO_TABLE_GENERICPARAM];
		table->rows += mono_array_length (mb->generic_params);
		alloc_table (table, table->rows);
		for (i = 0; i < mono_array_length (mb->generic_params); ++i) {
			guint32 owner = MONO_TYPEORMETHOD_METHOD | (mb->table_idx << MONO_TYPEORMETHOD_BITS);

			mono_image_get_generic_param_info (
				mono_array_get (mb->generic_params, gpointer, i), owner, assembly);
		}
	}

}

static void
mono_image_get_ctor_info (MonoDomain *domain, MonoReflectionCtorBuilder *mb, MonoDynamicImage *assembly)
{
	ReflectionMethodBuilder rmb;

	reflection_methodbuilder_from_ctor_builder (&rmb, mb);

	mono_image_basic_method (&rmb, assembly);
	mb->table_idx = *rmb.table_idx;
}
#endif

static char*
type_get_fully_qualified_name (MonoType *type)
{
	return mono_type_get_name_full (type, MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED);
}

static char*
type_get_qualified_name (MonoType *type, MonoAssembly *ass) {
	MonoClass *klass;
	MonoAssembly *ta;

	klass = mono_class_from_mono_type (type);
	if (!klass) 
		return mono_type_get_name_full (type, MONO_TYPE_NAME_FORMAT_REFLECTION);
	ta = klass->image->assembly;
	if (ta->dynamic || (ta == ass)) {
		if (klass->generic_class || klass->generic_container)
			/* For generic type definitions, we want T, while REFLECTION returns T<K> */
			return mono_type_get_name_full (type, MONO_TYPE_NAME_FORMAT_FULL_NAME);
		else
			return mono_type_get_name_full (type, MONO_TYPE_NAME_FORMAT_REFLECTION);
	}

	return mono_type_get_name_full (type, MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED);
}

#ifndef DISABLE_REFLECTION_EMIT
/*field_image is the image to which the eventual custom mods have been encoded against*/
static guint32
fieldref_encode_signature (MonoDynamicImage *assembly, MonoImage *field_image, MonoType *type)
{
	SigBuffer buf;
	guint32 idx, i, token;

	if (!assembly->save)
		return 0;

	sigbuffer_init (&buf, 32);
	
	sigbuffer_add_value (&buf, 0x06);
	/* encode custom attributes before the type */
	if (type->num_mods) {
		for (i = 0; i < type->num_mods; ++i) {
			if (field_image) {
				MonoClass *class = mono_class_get (field_image, type->modifiers [i].token);
				g_assert (class);
				token = mono_image_typedef_or_ref (assembly, &class->byval_arg);
			} else {
				token = type->modifiers [i].token;
			}

			if (type->modifiers [i].required)
				sigbuffer_add_byte (&buf, MONO_TYPE_CMOD_REQD);
			else
				sigbuffer_add_byte (&buf, MONO_TYPE_CMOD_OPT);

			sigbuffer_add_value (&buf, token);
		}
	}
	encode_type (assembly, type, &buf);
	idx = sigbuffer_add_to_blob_cached (assembly, &buf);
	sigbuffer_free (&buf);
	return idx;
}
#endif

static guint32
field_encode_signature (MonoDynamicImage *assembly, MonoReflectionFieldBuilder *fb)
{
	SigBuffer buf;
	guint32 idx;
	guint32 typespec = 0;
	MonoType *type;
	MonoClass *class;

	init_type_builder_generics (fb->type);

	type = mono_reflection_type_get_handle ((MonoReflectionType*)fb->type);
	class = mono_class_from_mono_type (type);

	sigbuffer_init (&buf, 32);
	
	sigbuffer_add_value (&buf, 0x06);
	encode_custom_modifiers (assembly, fb->modreq, fb->modopt, &buf);
	/* encode custom attributes before the type */

	if (class->generic_container)
		typespec = create_typespec (assembly, type);

	if (typespec) {
		MonoGenericClass *gclass;
		gclass = mono_metadata_lookup_generic_class (class, class->generic_container->context.class_inst, TRUE);
		encode_generic_class (assembly, gclass, &buf);
	} else {
		encode_type (assembly, type, &buf);
	}
	idx = sigbuffer_add_to_blob_cached (assembly, &buf);
	sigbuffer_free (&buf);
	return idx;
}

static guint32
encode_constant (MonoDynamicImage *assembly, MonoObject *val, guint32 *ret_type) {
	char blob_size [64];
	char *b = blob_size;
	char *p, *box_val;
	char* buf;
	guint32 idx = 0, len = 0, dummy = 0;

	p = buf = g_malloc (64);
	if (!val) {
		*ret_type = MONO_TYPE_CLASS;
		len = 4;
		box_val = (char*)&dummy;
	} else {
		box_val = ((char*)val) + sizeof (MonoObject);
		*ret_type = val->vtable->klass->byval_arg.type;
	}
handle_enum:
	switch (*ret_type) {
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_U1:
	case MONO_TYPE_I1:
		len = 1;
		break;
	case MONO_TYPE_CHAR:
	case MONO_TYPE_U2:
	case MONO_TYPE_I2:
		len = 2;
		break;
	case MONO_TYPE_U4:
	case MONO_TYPE_I4:
	case MONO_TYPE_R4:
		len = 4;
		break;
	case MONO_TYPE_U8:
	case MONO_TYPE_I8:
		len = 8;
		break;
	case MONO_TYPE_R8:
		len = 8;
		break;
	case MONO_TYPE_VALUETYPE: {
		MonoClass *klass = val->vtable->klass;
		
		if (klass->enumtype) {
			*ret_type = mono_class_enum_basetype (klass)->type;
			goto handle_enum;
		} else if (mono_is_corlib_image (klass->image) && strcmp (klass->name_space, "System") == 0 && strcmp (klass->name, "DateTime") == 0) {
			len = 8;
		} else 
			g_error ("we can't encode valuetypes, we should have never reached this line");
		break;
	}
	case MONO_TYPE_CLASS:
		break;
	case MONO_TYPE_STRING: {
		MonoString *str = (MonoString*)val;
		/* there is no signature */
		len = str->length * 2;
		mono_metadata_encode_value (len, b, &b);
#if G_BYTE_ORDER != G_LITTLE_ENDIAN
		{
			char *swapped = g_malloc (2 * mono_string_length (str));
			const char *p = (const char*)mono_string_chars (str);

			swap_with_size (swapped, p, 2, mono_string_length (str));
			idx = add_to_blob_cached (assembly, blob_size, b-blob_size, swapped, len);
			g_free (swapped);
		}
#else
		idx = add_to_blob_cached (assembly, blob_size, b-blob_size, (char*)mono_string_chars (str), len);
#endif

		g_free (buf);
		return idx;
	}
	case MONO_TYPE_GENERICINST:
		*ret_type = val->vtable->klass->generic_class->container_class->byval_arg.type;
		goto handle_enum;
	default:
		g_error ("we don't encode constant type 0x%02x yet", *ret_type);
	}

	/* there is no signature */
	mono_metadata_encode_value (len, b, &b);
#if G_BYTE_ORDER != G_LITTLE_ENDIAN
	idx = mono_image_add_stream_data (&assembly->blob, blob_size, b-blob_size);
	swap_with_size (blob_size, box_val, len, 1);
	mono_image_add_stream_data (&assembly->blob, blob_size, len);
#else
	idx = add_to_blob_cached (assembly, blob_size, b-blob_size, box_val, len);
#endif

	g_free (buf);
	return idx;
}

static guint32
encode_marshal_blob (MonoDynamicImage *assembly, MonoReflectionMarshal *minfo) {
	char *str;
	SigBuffer buf;
	guint32 idx, len;

	sigbuffer_init (&buf, 32);

	sigbuffer_add_value (&buf, minfo->type);

	switch (minfo->type) {
	case MONO_NATIVE_BYVALTSTR:
	case MONO_NATIVE_BYVALARRAY:
		sigbuffer_add_value (&buf, minfo->count);
		break;
	case MONO_NATIVE_LPARRAY:
		if (minfo->eltype || minfo->has_size) {
			sigbuffer_add_value (&buf, minfo->eltype);
			if (minfo->has_size) {
				sigbuffer_add_value (&buf, minfo->param_num != -1? minfo->param_num: 0);
				sigbuffer_add_value (&buf, minfo->count != -1? minfo->count: 0);

				/* LAMESPEC: ElemMult is undocumented */
				sigbuffer_add_value (&buf, minfo->param_num != -1? 1: 0);
			}
		}
		break;
	case MONO_NATIVE_SAFEARRAY:
		if (minfo->eltype)
			sigbuffer_add_value (&buf, minfo->eltype);
		break;
	case MONO_NATIVE_CUSTOM:
		if (minfo->guid) {
			str = mono_string_to_utf8 (minfo->guid);
			len = strlen (str);
			sigbuffer_add_value (&buf, len);
			sigbuffer_add_mem (&buf, str, len);
			g_free (str);
		} else {
			sigbuffer_add_value (&buf, 0);
		}
		/* native type name */
		sigbuffer_add_value (&buf, 0);
		/* custom marshaler type name */
		if (minfo->marshaltype || minfo->marshaltyperef) {
			if (minfo->marshaltyperef)
				str = type_get_fully_qualified_name (mono_reflection_type_get_handle ((MonoReflectionType*)minfo->marshaltyperef));
			else
				str = mono_string_to_utf8 (minfo->marshaltype);
			len = strlen (str);
			sigbuffer_add_value (&buf, len);
			sigbuffer_add_mem (&buf, str, len);
			g_free (str);
		} else {
			/* FIXME: Actually a bug, since this field is required.  Punting for now ... */
			sigbuffer_add_value (&buf, 0);
		}
		if (minfo->mcookie) {
			str = mono_string_to_utf8 (minfo->mcookie);
			len = strlen (str);
			sigbuffer_add_value (&buf, len);
			sigbuffer_add_mem (&buf, str, len);
			g_free (str);
		} else {
			sigbuffer_add_value (&buf, 0);
		}
		break;
	default:
		break;
	}
	idx = sigbuffer_add_to_blob_cached (assembly, &buf);
	sigbuffer_free (&buf);
	return idx;
}

static void
mono_image_get_field_info (MonoReflectionFieldBuilder *fb, MonoDynamicImage *assembly)
{
	MonoDynamicTable *table;
	guint32 *values;

	/* maybe this fixup should be done in the C# code */
	if (fb->attrs & FIELD_ATTRIBUTE_LITERAL)
		fb->attrs |= FIELD_ATTRIBUTE_HAS_DEFAULT;
	table = &assembly->tables [MONO_TABLE_FIELD];
	fb->table_idx = table->next_idx ++;
	g_hash_table_insert (assembly->field_to_table_idx, fb->handle, GUINT_TO_POINTER (fb->table_idx));
	values = table->values + fb->table_idx * MONO_FIELD_SIZE;
	values [MONO_FIELD_NAME] = string_heap_insert_mstring (&assembly->sheap, fb->name);
	values [MONO_FIELD_FLAGS] = fb->attrs;
	values [MONO_FIELD_SIGNATURE] = field_encode_signature (assembly, fb);

	if (fb->offset != -1) {
		table = &assembly->tables [MONO_TABLE_FIELDLAYOUT];
		table->rows ++;
		alloc_table (table, table->rows);
		values = table->values + table->rows * MONO_FIELD_LAYOUT_SIZE;
		values [MONO_FIELD_LAYOUT_FIELD] = fb->table_idx;
		values [MONO_FIELD_LAYOUT_OFFSET] = fb->offset;
	}
	if (fb->attrs & FIELD_ATTRIBUTE_LITERAL) {
		guint32 field_type = 0;
		table = &assembly->tables [MONO_TABLE_CONSTANT];
		table->rows ++;
		alloc_table (table, table->rows);
		values = table->values + table->rows * MONO_CONSTANT_SIZE;
		values [MONO_CONSTANT_PARENT] = MONO_HASCONSTANT_FIEDDEF | (fb->table_idx << MONO_HASCONSTANT_BITS);
		values [MONO_CONSTANT_VALUE] = encode_constant (assembly, fb->def_value, &field_type);
		values [MONO_CONSTANT_TYPE] = field_type;
		values [MONO_CONSTANT_PADDING] = 0;
	}
	if (fb->attrs & FIELD_ATTRIBUTE_HAS_FIELD_RVA) {
		guint32 rva_idx;
		table = &assembly->tables [MONO_TABLE_FIELDRVA];
		table->rows ++;
		alloc_table (table, table->rows);
		values = table->values + table->rows * MONO_FIELD_RVA_SIZE;
		values [MONO_FIELD_RVA_FIELD] = fb->table_idx;
		/*
		 * We store it in the code section because it's simpler for now.
		 */
		if (fb->rva_data) {
			if (mono_array_length (fb->rva_data) >= 10)
				stream_data_align (&assembly->code);
			rva_idx = mono_image_add_stream_data (&assembly->code, mono_array_addr (fb->rva_data, char, 0), mono_array_length (fb->rva_data));
		} else
			rva_idx = mono_image_add_stream_zero (&assembly->code, mono_class_value_size (fb->handle->parent, NULL));
		values [MONO_FIELD_RVA_RVA] = rva_idx + assembly->text_rva;
	}
	if (fb->marshal_info) {
		table = &assembly->tables [MONO_TABLE_FIELDMARSHAL];
		table->rows ++;
		alloc_table (table, table->rows);
		values = table->values + table->rows * MONO_FIELD_MARSHAL_SIZE;
		values [MONO_FIELD_MARSHAL_PARENT] = (fb->table_idx << MONO_HAS_FIELD_MARSHAL_BITS) | MONO_HAS_FIELD_MARSHAL_FIELDSREF;
		values [MONO_FIELD_MARSHAL_NATIVE_TYPE] = encode_marshal_blob (assembly, fb->marshal_info);
	}
}

static guint32
property_encode_signature (MonoDynamicImage *assembly, MonoReflectionPropertyBuilder *fb)
{
	SigBuffer buf;
	guint32 nparams = 0;
	MonoReflectionMethodBuilder *mb = fb->get_method;
	MonoReflectionMethodBuilder *smb = fb->set_method;
	guint32 idx, i;

	if (mb && mb->parameters)
		nparams = mono_array_length (mb->parameters);
	if (!mb && smb && smb->parameters)
		nparams = mono_array_length (smb->parameters) - 1;
	sigbuffer_init (&buf, 32);
	if (fb->call_conv & 0x20)
		sigbuffer_add_byte (&buf, 0x28);
	else
		sigbuffer_add_byte (&buf, 0x08);
	sigbuffer_add_value (&buf, nparams);
	if (mb) {
		encode_reflection_type (assembly, (MonoReflectionType*)mb->rtype, &buf);
		for (i = 0; i < nparams; ++i) {
			MonoReflectionType *pt = mono_array_get (mb->parameters, MonoReflectionType*, i);
			encode_reflection_type (assembly, pt, &buf);
		}
	} else if (smb && smb->parameters) {
		/* the property type is the last param */
		encode_reflection_type (assembly, mono_array_get (smb->parameters, MonoReflectionType*, nparams), &buf);
		for (i = 0; i < nparams; ++i) {
			MonoReflectionType *pt = mono_array_get (smb->parameters, MonoReflectionType*, i);
			encode_reflection_type (assembly, pt, &buf);
		}
	} else {
		encode_reflection_type (assembly, (MonoReflectionType*)fb->type, &buf);
	}

	idx = sigbuffer_add_to_blob_cached (assembly, &buf);
	sigbuffer_free (&buf);
	return idx;
}

static void
mono_image_get_property_info (MonoReflectionPropertyBuilder *pb, MonoDynamicImage *assembly)
{
	MonoDynamicTable *table;
	guint32 *values;
	guint num_methods = 0;
	guint32 semaidx;

	/* 
	 * we need to set things in the following tables:
	 * PROPERTYMAP (info already filled in _get_type_info ())
	 * PROPERTY    (rows already preallocated in _get_type_info ())
	 * METHOD      (method info already done with the generic method code)
	 * METHODSEMANTICS
	 * CONSTANT
	 */
	table = &assembly->tables [MONO_TABLE_PROPERTY];
	pb->table_idx = table->next_idx ++;
	values = table->values + pb->table_idx * MONO_PROPERTY_SIZE;
	values [MONO_PROPERTY_NAME] = string_heap_insert_mstring (&assembly->sheap, pb->name);
	values [MONO_PROPERTY_FLAGS] = pb->attrs;
	values [MONO_PROPERTY_TYPE] = property_encode_signature (assembly, pb);

	/* FIXME: we still don't handle 'other' methods */
	if (pb->get_method) num_methods ++;
	if (pb->set_method) num_methods ++;

	table = &assembly->tables [MONO_TABLE_METHODSEMANTICS];
	table->rows += num_methods;
	alloc_table (table, table->rows);

	if (pb->get_method) {
		semaidx = table->next_idx ++;
		values = table->values + semaidx * MONO_METHOD_SEMA_SIZE;
		values [MONO_METHOD_SEMA_SEMANTICS] = METHOD_SEMANTIC_GETTER;
		values [MONO_METHOD_SEMA_METHOD] = pb->get_method->table_idx;
		values [MONO_METHOD_SEMA_ASSOCIATION] = (pb->table_idx << MONO_HAS_SEMANTICS_BITS) | MONO_HAS_SEMANTICS_PROPERTY;
	}
	if (pb->set_method) {
		semaidx = table->next_idx ++;
		values = table->values + semaidx * MONO_METHOD_SEMA_SIZE;
		values [MONO_METHOD_SEMA_SEMANTICS] = METHOD_SEMANTIC_SETTER;
		values [MONO_METHOD_SEMA_METHOD] = pb->set_method->table_idx;
		values [MONO_METHOD_SEMA_ASSOCIATION] = (pb->table_idx << MONO_HAS_SEMANTICS_BITS) | MONO_HAS_SEMANTICS_PROPERTY;
	}
	if (pb->attrs & PROPERTY_ATTRIBUTE_HAS_DEFAULT) {
		guint32 field_type = 0;
		table = &assembly->tables [MONO_TABLE_CONSTANT];
		table->rows ++;
		alloc_table (table, table->rows);
		values = table->values + table->rows * MONO_CONSTANT_SIZE;
		values [MONO_CONSTANT_PARENT] = MONO_HASCONSTANT_PROPERTY | (pb->table_idx << MONO_HASCONSTANT_BITS);
		values [MONO_CONSTANT_VALUE] = encode_constant (assembly, pb->def_value, &field_type);
		values [MONO_CONSTANT_TYPE] = field_type;
		values [MONO_CONSTANT_PADDING] = 0;
	}
}

static void
mono_image_get_event_info (MonoReflectionEventBuilder *eb, MonoDynamicImage *assembly)
{
	MonoDynamicTable *table;
	guint32 *values;
	guint num_methods = 0;
	guint32 semaidx;

	/* 
	 * we need to set things in the following tables:
	 * EVENTMAP (info already filled in _get_type_info ())
	 * EVENT    (rows already preallocated in _get_type_info ())
	 * METHOD      (method info already done with the generic method code)
	 * METHODSEMANTICS
	 */
	table = &assembly->tables [MONO_TABLE_EVENT];
	eb->table_idx = table->next_idx ++;
	values = table->values + eb->table_idx * MONO_EVENT_SIZE;
	values [MONO_EVENT_NAME] = string_heap_insert_mstring (&assembly->sheap, eb->name);
	values [MONO_EVENT_FLAGS] = eb->attrs;
	values [MONO_EVENT_TYPE] = mono_image_typedef_or_ref (assembly, mono_reflection_type_get_handle (eb->type));

	/*
	 * FIXME: we still don't handle 'other' methods 
	 */
	if (eb->add_method) num_methods ++;
	if (eb->remove_method) num_methods ++;
	if (eb->raise_method) num_methods ++;

	table = &assembly->tables [MONO_TABLE_METHODSEMANTICS];
	table->rows += num_methods;
	alloc_table (table, table->rows);

	if (eb->add_method) {
		semaidx = table->next_idx ++;
		values = table->values + semaidx * MONO_METHOD_SEMA_SIZE;
		values [MONO_METHOD_SEMA_SEMANTICS] = METHOD_SEMANTIC_ADD_ON;
		values [MONO_METHOD_SEMA_METHOD] = eb->add_method->table_idx;
		values [MONO_METHOD_SEMA_ASSOCIATION] = (eb->table_idx << MONO_HAS_SEMANTICS_BITS) | MONO_HAS_SEMANTICS_EVENT;
	}
	if (eb->remove_method) {
		semaidx = table->next_idx ++;
		values = table->values + semaidx * MONO_METHOD_SEMA_SIZE;
		values [MONO_METHOD_SEMA_SEMANTICS] = METHOD_SEMANTIC_REMOVE_ON;
		values [MONO_METHOD_SEMA_METHOD] = eb->remove_method->table_idx;
		values [MONO_METHOD_SEMA_ASSOCIATION] = (eb->table_idx << MONO_HAS_SEMANTICS_BITS) | MONO_HAS_SEMANTICS_EVENT;
	}
	if (eb->raise_method) {
		semaidx = table->next_idx ++;
		values = table->values + semaidx * MONO_METHOD_SEMA_SIZE;
		values [MONO_METHOD_SEMA_SEMANTICS] = METHOD_SEMANTIC_FIRE;
		values [MONO_METHOD_SEMA_METHOD] = eb->raise_method->table_idx;
		values [MONO_METHOD_SEMA_ASSOCIATION] = (eb->table_idx << MONO_HAS_SEMANTICS_BITS) | MONO_HAS_SEMANTICS_EVENT;
	}
}

static void
encode_constraints (MonoReflectionGenericParam *gparam, guint32 owner, MonoDynamicImage *assembly)
{
	MonoDynamicTable *table;
	guint32 num_constraints, i;
	guint32 *values;
	guint32 table_idx;

	table = &assembly->tables [MONO_TABLE_GENERICPARAMCONSTRAINT];
	num_constraints = gparam->iface_constraints ?
		mono_array_length (gparam->iface_constraints) : 0;
	table->rows += num_constraints;
	if (gparam->base_type)
		table->rows++;
	alloc_table (table, table->rows);

	if (gparam->base_type) {
		table_idx = table->next_idx ++;
		values = table->values + table_idx * MONO_GENPARCONSTRAINT_SIZE;

		values [MONO_GENPARCONSTRAINT_GENERICPAR] = owner;
		values [MONO_GENPARCONSTRAINT_CONSTRAINT] = mono_image_typedef_or_ref (
			assembly, mono_reflection_type_get_handle (gparam->base_type));
	}

	for (i = 0; i < num_constraints; i++) {
		MonoReflectionType *constraint = mono_array_get (
			gparam->iface_constraints, gpointer, i);

		table_idx = table->next_idx ++;
		values = table->values + table_idx * MONO_GENPARCONSTRAINT_SIZE;

		values [MONO_GENPARCONSTRAINT_GENERICPAR] = owner;
		values [MONO_GENPARCONSTRAINT_CONSTRAINT] = mono_image_typedef_or_ref (
			assembly, mono_reflection_type_get_handle (constraint));
	}
}

static void
mono_image_get_generic_param_info (MonoReflectionGenericParam *gparam, guint32 owner, MonoDynamicImage *assembly)
{
	GenericParamTableEntry *entry;

	/*
	 * The GenericParam table must be sorted according to the `owner' field.
	 * We need to do this sorting prior to writing the GenericParamConstraint
	 * table, since we have to use the final GenericParam table indices there
	 * and they must also be sorted.
	 */

	entry = g_new0 (GenericParamTableEntry, 1);
	entry->owner = owner;
	/* FIXME: track where gen_params should be freed and remove the GC root as well */
	MONO_GC_REGISTER_ROOT_IF_MOVING (entry->gparam);
	entry->gparam = gparam;
	
	g_ptr_array_add (assembly->gen_params, entry);
}

static void
write_generic_param_entry (MonoDynamicImage *assembly, GenericParamTableEntry *entry)
{
	MonoDynamicTable *table;
	MonoGenericParam *param;
	guint32 *values;
	guint32 table_idx;

	table = &assembly->tables [MONO_TABLE_GENERICPARAM];
	table_idx = table->next_idx ++;
	values = table->values + table_idx * MONO_GENERICPARAM_SIZE;

	param = mono_reflection_type_get_handle ((MonoReflectionType*)entry->gparam)->data.generic_param;

	values [MONO_GENERICPARAM_OWNER] = entry->owner;
	values [MONO_GENERICPARAM_FLAGS] = entry->gparam->attrs;
	values [MONO_GENERICPARAM_NUMBER] = mono_generic_param_num (param);
	values [MONO_GENERICPARAM_NAME] = string_heap_insert (&assembly->sheap, mono_generic_param_info (param)->name);

	mono_image_add_cattrs (assembly, table_idx, MONO_CUSTOM_ATTR_GENERICPAR, entry->gparam->cattrs);

	encode_constraints (entry->gparam, table_idx, assembly);
}

static guint32
resolution_scope_from_image (MonoDynamicImage *assembly, MonoImage *image)
{
	MonoDynamicTable *table;
	guint32 token;
	guint32 *values;
	guint32 cols [MONO_ASSEMBLY_SIZE];
	const char *pubkey;
	guint32 publen;

	if ((token = GPOINTER_TO_UINT (g_hash_table_lookup (assembly->handleref, image))))
		return token;

	if (image->assembly->dynamic && (image->assembly == assembly->image.assembly)) {
		table = &assembly->tables [MONO_TABLE_MODULEREF];
		token = table->next_idx ++;
		table->rows ++;
		alloc_table (table, table->rows);
		values = table->values + token * MONO_MODULEREF_SIZE;
		values [MONO_MODULEREF_NAME] = string_heap_insert (&assembly->sheap, image->module_name);

		token <<= MONO_RESOLTION_SCOPE_BITS;
		token |= MONO_RESOLTION_SCOPE_MODULEREF;
		g_hash_table_insert (assembly->handleref, image, GUINT_TO_POINTER (token));

		return token;
	}
	
	if (image->assembly->dynamic)
		/* FIXME: */
		memset (cols, 0, sizeof (cols));
	else {
		/* image->assembly->image is the manifest module */
		image = image->assembly->image;
		mono_metadata_decode_row (&image->tables [MONO_TABLE_ASSEMBLY], 0, cols, MONO_ASSEMBLY_SIZE);
	}

	table = &assembly->tables [MONO_TABLE_ASSEMBLYREF];
	token = table->next_idx ++;
	table->rows ++;
	alloc_table (table, table->rows);
	values = table->values + token * MONO_ASSEMBLYREF_SIZE;
	values [MONO_ASSEMBLYREF_NAME] = string_heap_insert (&assembly->sheap, image->assembly_name);
	values [MONO_ASSEMBLYREF_MAJOR_VERSION] = cols [MONO_ASSEMBLY_MAJOR_VERSION];
	values [MONO_ASSEMBLYREF_MINOR_VERSION] = cols [MONO_ASSEMBLY_MINOR_VERSION];
	values [MONO_ASSEMBLYREF_BUILD_NUMBER] = cols [MONO_ASSEMBLY_BUILD_NUMBER];
	values [MONO_ASSEMBLYREF_REV_NUMBER] = cols [MONO_ASSEMBLY_REV_NUMBER];
	values [MONO_ASSEMBLYREF_FLAGS] = 0;
	values [MONO_ASSEMBLYREF_CULTURE] = 0;
	values [MONO_ASSEMBLYREF_HASH_VALUE] = 0;

	if (strcmp ("", image->assembly->aname.culture)) {
		values [MONO_ASSEMBLYREF_CULTURE] = string_heap_insert (&assembly->sheap,
				image->assembly->aname.culture);
	}

	if ((pubkey = mono_image_get_public_key (image, &publen))) {
		guchar pubtoken [9];
		pubtoken [0] = 8;
		mono_digest_get_public_token (pubtoken + 1, (guchar*)pubkey, publen);
		values [MONO_ASSEMBLYREF_PUBLIC_KEY] = mono_image_add_stream_data (&assembly->blob, (char*)pubtoken, 9);
	} else {
		values [MONO_ASSEMBLYREF_PUBLIC_KEY] = 0;
	}
	token <<= MONO_RESOLTION_SCOPE_BITS;
	token |= MONO_RESOLTION_SCOPE_ASSEMBLYREF;
	g_hash_table_insert (assembly->handleref, image, GUINT_TO_POINTER (token));
	return token;
}

static guint32
create_typespec (MonoDynamicImage *assembly, MonoType *type)
{
	MonoDynamicTable *table;
	guint32 *values;
	guint32 token;
	SigBuffer buf;

	if ((token = GPOINTER_TO_UINT (g_hash_table_lookup (assembly->typespec, type))))
		return token;

	sigbuffer_init (&buf, 32);
	switch (type->type) {
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_PTR:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
	case MONO_TYPE_GENERICINST:
		encode_type (assembly, type, &buf);
		break;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_VALUETYPE: {
		MonoClass *k = mono_class_from_mono_type (type);
		if (!k || !k->generic_container) {
			sigbuffer_free (&buf);
			return 0;
		}
		encode_type (assembly, type, &buf);
		break;
	}
	default:
		sigbuffer_free (&buf);
		return 0;
	}

	table = &assembly->tables [MONO_TABLE_TYPESPEC];
	if (assembly->save) {
		token = sigbuffer_add_to_blob_cached (assembly, &buf);
		alloc_table (table, table->rows + 1);
		values = table->values + table->next_idx * MONO_TYPESPEC_SIZE;
		values [MONO_TYPESPEC_SIGNATURE] = token;
	}
	sigbuffer_free (&buf);

	token = MONO_TYPEDEFORREF_TYPESPEC | (table->next_idx << MONO_TYPEDEFORREF_BITS);
	g_hash_table_insert (assembly->typespec, type, GUINT_TO_POINTER(token));
	table->next_idx ++;
	return token;
}

static guint32
mono_image_typedef_or_ref_full (MonoDynamicImage *assembly, MonoType *type, gboolean try_typespec)
{
	MonoDynamicTable *table;
	guint32 *values;
	guint32 token, scope, enclosing;
	MonoClass *klass;

	/* if the type requires a typespec, we must try that first*/
	if (try_typespec && (token = create_typespec (assembly, type)))
		return token;
	token = GPOINTER_TO_UINT (g_hash_table_lookup (assembly->typeref, type));
	if (token)
		return token;
	klass = mono_class_from_mono_type (type);
	if (!klass)
		klass = mono_class_from_mono_type (type);

	/*
	 * If it's in the same module and not a generic type parameter:
	 */
	if ((klass->image == &assembly->image) && (type->type != MONO_TYPE_VAR) && 
			(type->type != MONO_TYPE_MVAR)) {
		MonoReflectionTypeBuilder *tb = mono_class_get_ref_info (klass);
		token = MONO_TYPEDEFORREF_TYPEDEF | (tb->table_idx << MONO_TYPEDEFORREF_BITS);
		mono_g_hash_table_insert (assembly->tokens, GUINT_TO_POINTER (token), mono_class_get_ref_info (klass));
		return token;
	}

	if (klass->nested_in) {
		enclosing = mono_image_typedef_or_ref_full (assembly, &klass->nested_in->byval_arg, FALSE);
		/* get the typeref idx of the enclosing type */
		enclosing >>= MONO_TYPEDEFORREF_BITS;
		scope = (enclosing << MONO_RESOLTION_SCOPE_BITS) | MONO_RESOLTION_SCOPE_TYPEREF;
	} else {
		scope = resolution_scope_from_image (assembly, klass->image);
	}
	table = &assembly->tables [MONO_TABLE_TYPEREF];
	if (assembly->save) {
		alloc_table (table, table->rows + 1);
		values = table->values + table->next_idx * MONO_TYPEREF_SIZE;
		values [MONO_TYPEREF_SCOPE] = scope;
		values [MONO_TYPEREF_NAME] = string_heap_insert (&assembly->sheap, klass->name);
		values [MONO_TYPEREF_NAMESPACE] = string_heap_insert (&assembly->sheap, klass->name_space);
	}
	token = MONO_TYPEDEFORREF_TYPEREF | (table->next_idx << MONO_TYPEDEFORREF_BITS); /* typeref */
	g_hash_table_insert (assembly->typeref, type, GUINT_TO_POINTER(token));
	table->next_idx ++;
	mono_g_hash_table_insert (assembly->tokens, GUINT_TO_POINTER (token), mono_class_get_ref_info (klass));
	return token;
}

/*
 * Despite the name, we handle also TypeSpec (with the above helper).
 */
static guint32
mono_image_typedef_or_ref (MonoDynamicImage *assembly, MonoType *type)
{
	return mono_image_typedef_or_ref_full (assembly, type, TRUE);
}

#ifndef DISABLE_REFLECTION_EMIT
static guint32
mono_image_add_memberef_row (MonoDynamicImage *assembly, guint32 parent, const char *name, guint32 sig)
{
	MonoDynamicTable *table;
	guint32 *values;
	guint32 token, pclass;

	switch (parent & MONO_TYPEDEFORREF_MASK) {
	case MONO_TYPEDEFORREF_TYPEREF:
		pclass = MONO_MEMBERREF_PARENT_TYPEREF;
		break;
	case MONO_TYPEDEFORREF_TYPESPEC:
		pclass = MONO_MEMBERREF_PARENT_TYPESPEC;
		break;
	case MONO_TYPEDEFORREF_TYPEDEF:
		pclass = MONO_MEMBERREF_PARENT_TYPEDEF;
		break;
	default:
		g_warning ("unknown typeref or def token 0x%08x for %s", parent, name);
		return 0;
	}
	/* extract the index */
	parent >>= MONO_TYPEDEFORREF_BITS;

	table = &assembly->tables [MONO_TABLE_MEMBERREF];

	if (assembly->save) {
		alloc_table (table, table->rows + 1);
		values = table->values + table->next_idx * MONO_MEMBERREF_SIZE;
		values [MONO_MEMBERREF_CLASS] = pclass | (parent << MONO_MEMBERREF_PARENT_BITS);
		values [MONO_MEMBERREF_NAME] = string_heap_insert (&assembly->sheap, name);
		values [MONO_MEMBERREF_SIGNATURE] = sig;
	}

	token = MONO_TOKEN_MEMBER_REF | table->next_idx;
	table->next_idx ++;

	return token;
}

/*
 * Insert a memberef row into the metadata: the token that point to the memberref
 * is returned. Caching is done in the caller (mono_image_get_methodref_token() or
 * mono_image_get_fieldref_token()).
 * The sig param is an index to an already built signature.
 */
static guint32
mono_image_get_memberref_token (MonoDynamicImage *assembly, MonoType *type, const char *name, guint32 sig)
{
	guint32 parent = mono_image_typedef_or_ref (assembly, type);
	return mono_image_add_memberef_row (assembly, parent, name, sig);
}


static guint32
mono_image_get_methodref_token (MonoDynamicImage *assembly, MonoMethod *method, gboolean create_typespec)
{
	guint32 token;
	MonoMethodSignature *sig;
	
	create_typespec = create_typespec && method->is_generic && method->klass->image != &assembly->image;

	if (create_typespec) {
		token = GPOINTER_TO_UINT (g_hash_table_lookup (assembly->handleref, GUINT_TO_POINTER (GPOINTER_TO_UINT (method) + 1)));
		if (token)
			return token;
	} 

	token = GPOINTER_TO_UINT (g_hash_table_lookup (assembly->handleref, method));
	if (token && !create_typespec)
		return token;

	g_assert (!method->is_inflated);
	if (!token) {
		/*
		 * A methodref signature can't contain an unmanaged calling convention.
		 */
		sig = mono_metadata_signature_dup (mono_method_signature (method));
		if ((sig->call_convention != MONO_CALL_DEFAULT) && (sig->call_convention != MONO_CALL_VARARG))
			sig->call_convention = MONO_CALL_DEFAULT;
		token = mono_image_get_memberref_token (assembly, &method->klass->byval_arg,
			method->name,  method_encode_signature (assembly, sig));
		g_free (sig);
		g_hash_table_insert (assembly->handleref, method, GUINT_TO_POINTER(token));
	}

	if (create_typespec) {
		MonoDynamicTable *table = &assembly->tables [MONO_TABLE_METHODSPEC];
		g_assert (mono_metadata_token_table (token) == MONO_TABLE_MEMBERREF);
		token = (mono_metadata_token_index (token) << MONO_METHODDEFORREF_BITS) | MONO_METHODDEFORREF_METHODREF;

		if (assembly->save) {
			guint32 *values;

			alloc_table (table, table->rows + 1);
			values = table->values + table->next_idx * MONO_METHODSPEC_SIZE;
			values [MONO_METHODSPEC_METHOD] = token;
			values [MONO_METHODSPEC_SIGNATURE] = encode_generic_method_sig (assembly, &mono_method_get_generic_container (method)->context);
		}

		token = MONO_TOKEN_METHOD_SPEC | table->next_idx;
		table->next_idx ++;
		/*methodspec and memberef tokens are diferent, */
		g_hash_table_insert (assembly->handleref, GUINT_TO_POINTER (GPOINTER_TO_UINT (method) + 1), GUINT_TO_POINTER (token));
		return token;
	}
	return token;
}

static guint32
mono_image_get_methodref_token_for_methodbuilder (MonoDynamicImage *assembly, MonoReflectionMethodBuilder *method)
{
	guint32 token, parent, sig;
	ReflectionMethodBuilder rmb;
	char *name;
	MonoReflectionTypeBuilder *tb = (MonoReflectionTypeBuilder *)method->type;
	
	token = GPOINTER_TO_UINT (g_hash_table_lookup (assembly->handleref, method));
	if (token)
		return token;

	name = mono_string_to_utf8 (method->name);
	reflection_methodbuilder_from_method_builder (&rmb, method);

	/*
	 * A methodref signature can't contain an unmanaged calling convention.
	 * Since some flags are encoded as part of call_conv, we need to check against it.
	*/
	if ((rmb.call_conv & ~0x60) != MONO_CALL_DEFAULT && (rmb.call_conv & ~0x60) != MONO_CALL_VARARG)
		rmb.call_conv = (rmb.call_conv & 0x60) | MONO_CALL_DEFAULT;

	sig = method_builder_encode_signature (assembly, &rmb);

	if (tb->generic_params)
		parent = create_generic_typespec (assembly, tb);
	else
		parent = mono_image_typedef_or_ref (assembly, mono_reflection_type_get_handle ((MonoReflectionType*)rmb.type));

	token = mono_image_add_memberef_row (assembly, parent, name, sig);

	g_free (name);
	g_hash_table_insert (assembly->handleref, method, GUINT_TO_POINTER(token));
	return token;
}

static guint32
mono_image_get_varargs_method_token (MonoDynamicImage *assembly, guint32 original,
				     const gchar *name, guint32 sig)
{
	MonoDynamicTable *table;
	guint32 token;
	guint32 *values;
	
	table = &assembly->tables [MONO_TABLE_MEMBERREF];

	if (assembly->save) {
		alloc_table (table, table->rows + 1);
		values = table->values + table->next_idx * MONO_MEMBERREF_SIZE;
		values [MONO_MEMBERREF_CLASS] = original;
		values [MONO_MEMBERREF_NAME] = string_heap_insert (&assembly->sheap, name);
		values [MONO_MEMBERREF_SIGNATURE] = sig;
	}

	token = MONO_TOKEN_MEMBER_REF | table->next_idx;
	table->next_idx ++;

	return token;
}

static guint32
encode_generic_method_definition_sig (MonoDynamicImage *assembly, MonoReflectionMethodBuilder *mb)
{
	SigBuffer buf;
	int i;
	guint32 nparams = mono_array_length (mb->generic_params);
	guint32 idx;

	if (!assembly->save)
		return 0;

	sigbuffer_init (&buf, 32);

	sigbuffer_add_value (&buf, 0xa);
	sigbuffer_add_value (&buf, nparams);

	for (i = 0; i < nparams; i++) {
		sigbuffer_add_value (&buf, MONO_TYPE_MVAR);
		sigbuffer_add_value (&buf, i);
	}

	idx = sigbuffer_add_to_blob_cached (assembly, &buf);
	sigbuffer_free (&buf);
	return idx;
}

static guint32
mono_image_get_methodspec_token_for_generic_method_definition (MonoDynamicImage *assembly, MonoReflectionMethodBuilder *mb)
{
	MonoDynamicTable *table;
	guint32 *values;
	guint32 token, mtoken = 0;

	token = GPOINTER_TO_UINT (mono_g_hash_table_lookup (assembly->methodspec, mb));
	if (token)
		return token;

	table = &assembly->tables [MONO_TABLE_METHODSPEC];

	mtoken = mono_image_get_methodref_token_for_methodbuilder (assembly, mb);
	switch (mono_metadata_token_table (mtoken)) {
	case MONO_TABLE_MEMBERREF:
		mtoken = (mono_metadata_token_index (mtoken) << MONO_METHODDEFORREF_BITS) | MONO_METHODDEFORREF_METHODREF;
		break;
	case MONO_TABLE_METHOD:
		mtoken = (mono_metadata_token_index (mtoken) << MONO_METHODDEFORREF_BITS) | MONO_METHODDEFORREF_METHODDEF;
		break;
	default:
		g_assert_not_reached ();
	}

	if (assembly->save) {
		alloc_table (table, table->rows + 1);
		values = table->values + table->next_idx * MONO_METHODSPEC_SIZE;
		values [MONO_METHODSPEC_METHOD] = mtoken;
		values [MONO_METHODSPEC_SIGNATURE] = encode_generic_method_definition_sig (assembly, mb);
	}

	token = MONO_TOKEN_METHOD_SPEC | table->next_idx;
	table->next_idx ++;

	mono_g_hash_table_insert (assembly->methodspec, mb, GUINT_TO_POINTER(token));
	return token;
}

static guint32
mono_image_get_methodbuilder_token (MonoDynamicImage *assembly, MonoReflectionMethodBuilder *mb, gboolean create_methodspec)
{
	guint32 token;

	if (mb->generic_params && create_methodspec) 
		return mono_image_get_methodspec_token_for_generic_method_definition (assembly, mb);

	token = GPOINTER_TO_UINT (mono_g_hash_table_lookup (assembly->handleref_managed, mb));
	if (token)
		return token;

	token = mono_image_get_methodref_token_for_methodbuilder (assembly, mb);
	mono_g_hash_table_insert (assembly->handleref_managed, mb, GUINT_TO_POINTER(token));
	return token;
}

static guint32
mono_image_get_ctorbuilder_token (MonoDynamicImage *assembly, MonoReflectionCtorBuilder *mb)
{
	guint32 token, parent, sig;
	ReflectionMethodBuilder rmb;
	char *name;
	MonoReflectionTypeBuilder *tb = (MonoReflectionTypeBuilder *)mb->type;
	
	token = GPOINTER_TO_UINT (mono_g_hash_table_lookup (assembly->handleref_managed, mb));
	if (token)
		return token;

	reflection_methodbuilder_from_ctor_builder (&rmb, mb);

	if (tb->generic_params)
		parent = create_generic_typespec (assembly, tb);
	else
		parent = mono_image_typedef_or_ref (assembly, mono_reflection_type_get_handle ((MonoReflectionType*)tb));
	
	name = mono_string_to_utf8 (rmb.name);
	sig = method_builder_encode_signature (assembly, &rmb);

	token = mono_image_add_memberef_row (assembly, parent, name, sig);

	g_free (name);
	mono_g_hash_table_insert (assembly->handleref_managed, mb, GUINT_TO_POINTER(token));
	return token;
}
#endif

static gboolean
is_field_on_inst (MonoClassField *field)
{
	return (field->parent->generic_class && field->parent->generic_class->is_dynamic && ((MonoDynamicGenericClass*)field->parent->generic_class)->fields);
}

/*
 * If FIELD is a field of a MonoDynamicGenericClass, return its non-inflated type.
 */
static MonoType*
get_field_on_inst_generic_type (MonoClassField *field)
{
	MonoClass *class, *gtd;
	MonoDynamicGenericClass *dgclass;
	int field_index;

	g_assert (is_field_on_inst (field));

	dgclass = (MonoDynamicGenericClass*)field->parent->generic_class;

	if (field >= dgclass->fields && field - dgclass->fields < dgclass->count_fields) {
		field_index = field - dgclass->fields;
		return dgclass->field_generic_types [field_index];		
	}

	class = field->parent;
	gtd = class->generic_class->container_class;

	if (field >= class->fields && field - class->fields < class->field.count) {
		field_index = field - class->fields;
		return gtd->fields [field_index].type;
	}

	g_assert_not_reached ();
	return 0;
}

#ifndef DISABLE_REFLECTION_EMIT
static guint32
mono_image_get_fieldref_token (MonoDynamicImage *assembly, MonoObject *f, MonoClassField *field)
{
	MonoType *type;
	guint32 token;

	g_assert (field);
	g_assert (field->parent);

	token = GPOINTER_TO_UINT (mono_g_hash_table_lookup (assembly->handleref_managed, f));
	if (token)
		return token;

	if (field->parent->generic_class && field->parent->generic_class->container_class && field->parent->generic_class->container_class->fields) {
		int index = field - field->parent->fields;
		type = mono_field_get_type (&field->parent->generic_class->container_class->fields [index]);
	} else {
		if (is_field_on_inst (field))
			type = get_field_on_inst_generic_type (field);
		else
			type = mono_field_get_type (field);
	}
	token = mono_image_get_memberref_token (assembly, &field->parent->byval_arg,
											mono_field_get_name (field),
											fieldref_encode_signature (assembly, field->parent->image, type));
	mono_g_hash_table_insert (assembly->handleref_managed, f, GUINT_TO_POINTER(token));
	return token;
}

static guint32
mono_image_get_field_on_inst_token (MonoDynamicImage *assembly, MonoReflectionFieldOnTypeBuilderInst *f)
{
	guint32 token;
	MonoClass *klass;
	MonoGenericClass *gclass;
	MonoDynamicGenericClass *dgclass;
	MonoType *type;
	char *name;

	token = GPOINTER_TO_UINT (mono_g_hash_table_lookup (assembly->handleref_managed, f));
	if (token)
		return token;
	if (is_sre_field_builder (mono_object_class (f->fb))) {
		MonoReflectionFieldBuilder *fb = (MonoReflectionFieldBuilder *)f->fb;
		type = mono_reflection_type_get_handle ((MonoReflectionType*)f->inst);
		klass = mono_class_from_mono_type (type);
		gclass = type->data.generic_class;
		g_assert (gclass->is_dynamic);
		dgclass = (MonoDynamicGenericClass *) gclass;

		name = mono_string_to_utf8 (fb->name);
		token = mono_image_get_memberref_token (assembly, &klass->byval_arg, name, 
												field_encode_signature (assembly, fb));
		g_free (name);		
	} else if (is_sr_mono_field (mono_object_class (f->fb))) {
		guint32 sig;
		MonoClassField *field = ((MonoReflectionField *)f->fb)->field;

		type = mono_reflection_type_get_handle ((MonoReflectionType*)f->inst);
		klass = mono_class_from_mono_type (type);

		sig = fieldref_encode_signature (assembly, field->parent->image, field->type);
		token = mono_image_get_memberref_token (assembly, &klass->byval_arg, field->name, sig);
	} else {
		char *name = mono_type_get_full_name (mono_object_class (f->fb));
		g_error ("mono_image_get_field_on_inst_token: don't know how to handle %s", name);
	}

	mono_g_hash_table_insert (assembly->handleref_managed, f, GUINT_TO_POINTER (token));
	return token;
}

static guint32
mono_image_get_ctor_on_inst_token (MonoDynamicImage *assembly, MonoReflectionCtorOnTypeBuilderInst *c, gboolean create_methodspec)
{
	guint32 sig, token;
	MonoClass *klass;
	MonoGenericClass *gclass;
	MonoType *type;

	/* A ctor cannot be a generic method, so we can ignore create_methodspec */

	token = GPOINTER_TO_UINT (mono_g_hash_table_lookup (assembly->handleref_managed, c));
	if (token)
		return token;

	if (is_sre_ctor_builder (mono_object_class (c->cb))) {
		MonoReflectionCtorBuilder *cb = (MonoReflectionCtorBuilder *)c->cb;
		MonoDynamicGenericClass *dgclass;
		ReflectionMethodBuilder rmb;
		char *name;

		type = mono_reflection_type_get_handle ((MonoReflectionType*)c->inst);
		klass = mono_class_from_mono_type (type);

		gclass = type->data.generic_class;
		g_assert (gclass->is_dynamic);
		dgclass = (MonoDynamicGenericClass *) gclass;

		reflection_methodbuilder_from_ctor_builder (&rmb, cb);

		name = mono_string_to_utf8 (rmb.name);

		sig = method_builder_encode_signature (assembly, &rmb);

		token = mono_image_get_memberref_token (assembly, &klass->byval_arg, name, sig);
		g_free (name);
	} else if (is_sr_mono_cmethod (mono_object_class (c->cb))) {
		MonoMethod *mm = ((MonoReflectionMethod *)c->cb)->method;

		type = mono_reflection_type_get_handle ((MonoReflectionType*)c->inst);
		klass = mono_class_from_mono_type (type);

		sig = method_encode_signature (assembly, mono_method_signature (mm));
		token = mono_image_get_memberref_token (assembly, &klass->byval_arg, mm->name, sig);
	} else {
		char *name = mono_type_get_full_name (mono_object_class (c->cb));
		g_error ("mono_image_get_method_on_inst_token: don't know how to handle %s", name);
	}


	mono_g_hash_table_insert (assembly->handleref_managed, c, GUINT_TO_POINTER (token));
	return token;
}

static MonoMethod*
mono_reflection_method_on_tb_inst_get_handle (MonoReflectionMethodOnTypeBuilderInst *m)
{
	MonoClass *klass;
	MonoGenericContext tmp_context;
	MonoType **type_argv;
	MonoGenericInst *ginst;
	MonoMethod *method, *inflated;
	int count, i;

	init_type_builder_generics ((MonoObject*)m->inst);

	method = inflate_method (m->inst, (MonoObject*)m->mb);

	klass = method->klass;

	if (m->method_args == NULL)
		return method;

	if (method->is_inflated)
		method = ((MonoMethodInflated *) method)->declaring;

	count = mono_array_length (m->method_args);

	type_argv = g_new0 (MonoType *, count);
	for (i = 0; i < count; i++) {
		MonoReflectionType *garg = mono_array_get (m->method_args, gpointer, i);
		type_argv [i] = mono_reflection_type_get_handle (garg);
	}
	ginst = mono_metadata_get_generic_inst (count, type_argv);
	g_free (type_argv);

	tmp_context.class_inst = klass->generic_class ? klass->generic_class->context.class_inst : NULL;
	tmp_context.method_inst = ginst;

	inflated = mono_class_inflate_generic_method (method, &tmp_context);
	return inflated;
}

static guint32
mono_image_get_method_on_inst_token (MonoDynamicImage *assembly, MonoReflectionMethodOnTypeBuilderInst *m, gboolean create_methodspec)
{
	guint32 sig, token = 0;
	MonoType *type;
	MonoClass *klass;

	if (m->method_args) {
		MonoMethod *inflated;

		inflated = mono_reflection_method_on_tb_inst_get_handle (m);
		if (create_methodspec)
			token = mono_image_get_methodspec_token (assembly, inflated);
		else
			token = mono_image_get_inflated_method_token (assembly, inflated);
		return token;
	}

	token = GPOINTER_TO_UINT (mono_g_hash_table_lookup (assembly->handleref_managed, m));
	if (token)
		return token;

	if (is_sre_method_builder (mono_object_class (m->mb))) {
		MonoReflectionMethodBuilder *mb = (MonoReflectionMethodBuilder *)m->mb;
		MonoGenericClass *gclass;
		ReflectionMethodBuilder rmb;
		char *name;

		type = mono_reflection_type_get_handle ((MonoReflectionType*)m->inst);
		klass = mono_class_from_mono_type (type);
		gclass = type->data.generic_class;
		g_assert (gclass->is_dynamic);

		reflection_methodbuilder_from_method_builder (&rmb, mb);

		name = mono_string_to_utf8 (rmb.name);

		sig = method_builder_encode_signature (assembly, &rmb);

		token = mono_image_get_memberref_token (assembly, &klass->byval_arg, name, sig);
		g_free (name);		
	} else if (is_sr_mono_method (mono_object_class (m->mb))) {
		MonoMethod *mm = ((MonoReflectionMethod *)m->mb)->method;

		type = mono_reflection_type_get_handle ((MonoReflectionType*)m->inst);
		klass = mono_class_from_mono_type (type);

		sig = method_encode_signature (assembly, mono_method_signature (mm));
		token = mono_image_get_memberref_token (assembly, &klass->byval_arg, mm->name, sig);
	} else {
		char *name = mono_type_get_full_name (mono_object_class (m->mb));
		g_error ("mono_image_get_method_on_inst_token: don't know how to handle %s", name);
	}

	mono_g_hash_table_insert (assembly->handleref_managed, m, GUINT_TO_POINTER (token));
	return token;
}

static guint32
encode_generic_method_sig (MonoDynamicImage *assembly, MonoGenericContext *context)
{
	SigBuffer buf;
	int i;
	guint32 nparams = context->method_inst->type_argc;
	guint32 idx;

	if (!assembly->save)
		return 0;

	sigbuffer_init (&buf, 32);
	/*
	 * FIXME: vararg, explicit_this, differenc call_conv values...
	 */
	sigbuffer_add_value (&buf, 0xa); /* FIXME FIXME FIXME */
	sigbuffer_add_value (&buf, nparams);

	for (i = 0; i < nparams; i++)
		encode_type (assembly, context->method_inst->type_argv [i], &buf);

	idx = sigbuffer_add_to_blob_cached (assembly, &buf);
	sigbuffer_free (&buf);
	return idx;
}

static guint32
method_encode_methodspec (MonoDynamicImage *assembly, MonoMethod *method)
{
	MonoDynamicTable *table;
	guint32 *values;
	guint32 token, mtoken = 0, sig;
	MonoMethodInflated *imethod;
	MonoMethod *declaring;

	table = &assembly->tables [MONO_TABLE_METHODSPEC];

	g_assert (method->is_inflated);
	imethod = (MonoMethodInflated *) method;
	declaring = imethod->declaring;

	sig = method_encode_signature (assembly, mono_method_signature (declaring));
	mtoken = mono_image_get_memberref_token (assembly, &method->klass->byval_arg, declaring->name, sig);

	if (!mono_method_signature (declaring)->generic_param_count)
		return mtoken;

	switch (mono_metadata_token_table (mtoken)) {
	case MONO_TABLE_MEMBERREF:
		mtoken = (mono_metadata_token_index (mtoken) << MONO_METHODDEFORREF_BITS) | MONO_METHODDEFORREF_METHODREF;
		break;
	case MONO_TABLE_METHOD:
		mtoken = (mono_metadata_token_index (mtoken) << MONO_METHODDEFORREF_BITS) | MONO_METHODDEFORREF_METHODDEF;
		break;
	default:
		g_assert_not_reached ();
	}

	sig = encode_generic_method_sig (assembly, mono_method_get_context (method));

	if (assembly->save) {
		alloc_table (table, table->rows + 1);
		values = table->values + table->next_idx * MONO_METHODSPEC_SIZE;
		values [MONO_METHODSPEC_METHOD] = mtoken;
		values [MONO_METHODSPEC_SIGNATURE] = sig;
	}

	token = MONO_TOKEN_METHOD_SPEC | table->next_idx;
	table->next_idx ++;

	return token;
}

static guint32
mono_image_get_methodspec_token (MonoDynamicImage *assembly, MonoMethod *method)
{
	MonoMethodInflated *imethod;
	guint32 token;
	
	token = GPOINTER_TO_UINT (g_hash_table_lookup (assembly->handleref, method));
	if (token)
		return token;

	g_assert (method->is_inflated);
	imethod = (MonoMethodInflated *) method;

	if (mono_method_signature (imethod->declaring)->generic_param_count) {
		token = method_encode_methodspec (assembly, method);
	} else {
		guint32 sig = method_encode_signature (
			assembly, mono_method_signature (imethod->declaring));
		token = mono_image_get_memberref_token (
			assembly, &method->klass->byval_arg, method->name, sig);
	}

	g_hash_table_insert (assembly->handleref, method, GUINT_TO_POINTER(token));
	return token;
}

static guint32
mono_image_get_inflated_method_token (MonoDynamicImage *assembly, MonoMethod *m)
{
	MonoMethodInflated *imethod = (MonoMethodInflated *) m;
	guint32 sig, token;

	sig = method_encode_signature (assembly, mono_method_signature (imethod->declaring));
	token = mono_image_get_memberref_token (
		assembly, &m->klass->byval_arg, m->name, sig);

	return token;
}

static guint32
create_generic_typespec (MonoDynamicImage *assembly, MonoReflectionTypeBuilder *tb)
{
	MonoDynamicTable *table;
	MonoClass *klass;
	MonoType *type;
	guint32 *values;
	guint32 token;
	SigBuffer buf;
	int count, i;

	/*
	 * We're creating a TypeSpec for the TypeBuilder of a generic type declaration,
	 * ie. what we'd normally use as the generic type in a TypeSpec signature.
	 * Because of this, we must not insert it into the `typeref' hash table.
	 */
	type = mono_reflection_type_get_handle ((MonoReflectionType*)tb);
	token = GPOINTER_TO_UINT (g_hash_table_lookup (assembly->typespec, type));
	if (token)
		return token;

	sigbuffer_init (&buf, 32);

	g_assert (tb->generic_params);
	klass = mono_class_from_mono_type (type);

	if (tb->generic_container)
		mono_reflection_create_generic_class (tb);

	sigbuffer_add_value (&buf, MONO_TYPE_GENERICINST);
	g_assert (klass->generic_container);
	sigbuffer_add_value (&buf, klass->byval_arg.type);
	sigbuffer_add_value (&buf, mono_image_typedef_or_ref_full (assembly, &klass->byval_arg, FALSE));

	count = mono_array_length (tb->generic_params);
	sigbuffer_add_value (&buf, count);
	for (i = 0; i < count; i++) {
		MonoReflectionGenericParam *gparam;

		gparam = mono_array_get (tb->generic_params, MonoReflectionGenericParam *, i);

		encode_type (assembly, mono_reflection_type_get_handle ((MonoReflectionType*)gparam), &buf);
	}

	table = &assembly->tables [MONO_TABLE_TYPESPEC];

	if (assembly->save) {
		token = sigbuffer_add_to_blob_cached (assembly, &buf);
		alloc_table (table, table->rows + 1);
		values = table->values + table->next_idx * MONO_TYPESPEC_SIZE;
		values [MONO_TYPESPEC_SIGNATURE] = token;
	}
	sigbuffer_free (&buf);

	token = MONO_TYPEDEFORREF_TYPESPEC | (table->next_idx << MONO_TYPEDEFORREF_BITS);
	g_hash_table_insert (assembly->typespec, type, GUINT_TO_POINTER(token));
	table->next_idx ++;
	return token;
}

/*
 * Return a copy of TYPE, adding the custom modifiers in MODREQ and MODOPT.
 */
static MonoType*
add_custom_modifiers (MonoDynamicImage *assembly, MonoType *type, MonoArray *modreq, MonoArray *modopt)
{
	int i, count, len, pos;
	MonoType *t;

	count = 0;
	if (modreq)
		count += mono_array_length (modreq);
	if (modopt)
		count += mono_array_length (modopt);

	if (count == 0)
		return mono_metadata_type_dup (NULL, type);

	len = MONO_SIZEOF_TYPE + ((gint32)count) * sizeof (MonoCustomMod);
	t = g_malloc (len);
	memcpy (t, type, MONO_SIZEOF_TYPE);

	t->num_mods = count;
	pos = 0;
	if (modreq) {
		for (i = 0; i < mono_array_length (modreq); ++i) {
			MonoType *mod = mono_type_array_get_and_resolve (modreq, i);
			t->modifiers [pos].required = 1;
			t->modifiers [pos].token = mono_image_typedef_or_ref (assembly, mod);
			pos ++;
		}
	}
	if (modopt) {
		for (i = 0; i < mono_array_length (modopt); ++i) {
			MonoType *mod = mono_type_array_get_and_resolve (modopt, i);
			t->modifiers [pos].required = 0;
			t->modifiers [pos].token = mono_image_typedef_or_ref (assembly, mod);
			pos ++;
		}
	}

	return t;
}

static void
init_type_builder_generics (MonoObject *type)
{
	MonoReflectionTypeBuilder *tb;

	if (!is_sre_type_builder(mono_object_class (type)))
		return;
	tb = (MonoReflectionTypeBuilder *)type;

	if (tb && tb->generic_container)
		mono_reflection_create_generic_class (tb);
}

static guint32
mono_image_get_generic_field_token (MonoDynamicImage *assembly, MonoReflectionFieldBuilder *fb)
{
	MonoDynamicTable *table;
	MonoClass *klass;
	MonoType *custom = NULL, *type;
	guint32 *values;
	guint32 token, pclass, parent, sig;
	gchar *name;

	token = GPOINTER_TO_UINT (mono_g_hash_table_lookup (assembly->handleref_managed, fb));
	if (token)
		return token;

	klass = mono_class_from_mono_type (mono_reflection_type_get_handle (fb->typeb));
	name = mono_string_to_utf8 (fb->name);

	/*FIXME this is one more layer of ugliness due how types are created.*/
	init_type_builder_generics (fb->type);

	/* fb->type does not include the custom modifiers */
	/* FIXME: We should do this in one place when a fieldbuilder is created */
	type = mono_reflection_type_get_handle ((MonoReflectionType*)fb->type);
	if (fb->modreq || fb->modopt)
		type = custom = add_custom_modifiers (assembly, type, fb->modreq, fb->modopt);

	sig = fieldref_encode_signature (assembly, NULL, type);
	g_free (custom);

	parent = create_generic_typespec (assembly, (MonoReflectionTypeBuilder *) fb->typeb);
	g_assert ((parent & MONO_TYPEDEFORREF_MASK) == MONO_TYPEDEFORREF_TYPESPEC);
	
	pclass = MONO_MEMBERREF_PARENT_TYPESPEC;
	parent >>= MONO_TYPEDEFORREF_BITS;

	table = &assembly->tables [MONO_TABLE_MEMBERREF];

	if (assembly->save) {
		alloc_table (table, table->rows + 1);
		values = table->values + table->next_idx * MONO_MEMBERREF_SIZE;
		values [MONO_MEMBERREF_CLASS] = pclass | (parent << MONO_MEMBERREF_PARENT_BITS);
		values [MONO_MEMBERREF_NAME] = string_heap_insert (&assembly->sheap, name);
		values [MONO_MEMBERREF_SIGNATURE] = sig;
	}

	token = MONO_TOKEN_MEMBER_REF | table->next_idx;
	table->next_idx ++;
	mono_g_hash_table_insert (assembly->handleref_managed, fb, GUINT_TO_POINTER(token));
	g_free (name);
	return token;
}

static guint32
mono_reflection_encode_sighelper (MonoDynamicImage *assembly, MonoReflectionSigHelper *helper)
{
	SigBuffer buf;
	guint32 nargs;
	guint32 size;
	guint32 i, idx;

	if (!assembly->save)
		return 0;

	/* FIXME: this means SignatureHelper.SignatureHelpType.HELPER_METHOD */
	g_assert (helper->type == 2);

	if (helper->arguments)
		nargs = mono_array_length (helper->arguments);
	else
		nargs = 0;

	size = 10 + (nargs * 10);
	
	sigbuffer_init (&buf, 32);

	/* Encode calling convention */
	/* Change Any to Standard */
	if ((helper->call_conv & 0x03) == 0x03)
		helper->call_conv = 0x01;
	/* explicit_this implies has_this */
	if (helper->call_conv & 0x40)
		helper->call_conv &= 0x20;

	if (helper->call_conv == 0) { /* Unmanaged */
		idx = helper->unmanaged_call_conv - 1;
	} else {
		/* Managed */
		idx = helper->call_conv & 0x60; /* has_this + explicit_this */
		if (helper->call_conv & 0x02) /* varargs */
			idx += 0x05;
	}

	sigbuffer_add_byte (&buf, idx);
	sigbuffer_add_value (&buf, nargs);
	encode_reflection_type (assembly, helper->return_type, &buf);
	for (i = 0; i < nargs; ++i) {
		MonoArray *modreqs = NULL;
		MonoArray *modopts = NULL;
		MonoReflectionType *pt;

		if (helper->modreqs && (i < mono_array_length (helper->modreqs)))
			modreqs = mono_array_get (helper->modreqs, MonoArray*, i);
		if (helper->modopts && (i < mono_array_length (helper->modopts)))
			modopts = mono_array_get (helper->modopts, MonoArray*, i);

		encode_custom_modifiers (assembly, modreqs, modopts, &buf);
		pt = mono_array_get (helper->arguments, MonoReflectionType*, i);
		encode_reflection_type (assembly, pt, &buf);
	}
	idx = sigbuffer_add_to_blob_cached (assembly, &buf);
	sigbuffer_free (&buf);

	return idx;
}

static guint32 
mono_image_get_sighelper_token (MonoDynamicImage *assembly, MonoReflectionSigHelper *helper)
{
	guint32 idx;
	MonoDynamicTable *table;
	guint32 *values;

	table = &assembly->tables [MONO_TABLE_STANDALONESIG];
	idx = table->next_idx ++;
	table->rows ++;
	alloc_table (table, table->rows);
	values = table->values + idx * MONO_STAND_ALONE_SIGNATURE_SIZE;

	values [MONO_STAND_ALONE_SIGNATURE] =
		mono_reflection_encode_sighelper (assembly, helper);

	return idx;
}

static int
reflection_cc_to_file (int call_conv) {
	switch (call_conv & 0x3) {
	case 0:
	case 1: return MONO_CALL_DEFAULT;
	case 2: return MONO_CALL_VARARG;
	default:
		g_assert_not_reached ();
	}
	return 0;
}
#endif /* !DISABLE_REFLECTION_EMIT */

typedef struct {
	MonoType *parent;
	MonoMethodSignature *sig;
	char *name;
	guint32 token;
} ArrayMethod;

#ifndef DISABLE_REFLECTION_EMIT
static guint32
mono_image_get_array_token (MonoDynamicImage *assembly, MonoReflectionArrayMethod *m)
{
	guint32 nparams, i;
	GList *tmp;
	char *name;
	MonoMethodSignature *sig;
	ArrayMethod *am;
	MonoType *mtype;

	name = mono_string_to_utf8 (m->name);
	nparams = mono_array_length (m->parameters);
	sig = g_malloc0 (MONO_SIZEOF_METHOD_SIGNATURE + sizeof (MonoType*) * nparams);
	sig->hasthis = 1;
	sig->sentinelpos = -1;
	sig->call_convention = reflection_cc_to_file (m->call_conv);
	sig->param_count = nparams;
	sig->ret = m->ret ? mono_reflection_type_get_handle (m->ret): &mono_defaults.void_class->byval_arg;
	mtype = mono_reflection_type_get_handle (m->parent);
	for (i = 0; i < nparams; ++i)
		sig->params [i] = mono_type_array_get_and_resolve (m->parameters, i);

	for (tmp = assembly->array_methods; tmp; tmp = tmp->next) {
		am = tmp->data;
		if (strcmp (name, am->name) == 0 && 
				mono_metadata_type_equal (am->parent, mtype) &&
				mono_metadata_signature_equal (am->sig, sig)) {
			g_free (name);
			g_free (sig);
			m->table_idx = am->token & 0xffffff;
			return am->token;
		}
	}
	am = g_new0 (ArrayMethod, 1);
	am->name = name;
	am->sig = sig;
	am->parent = mtype;
	am->token = mono_image_get_memberref_token (assembly, am->parent, name,
		method_encode_signature (assembly, sig));
	assembly->array_methods = g_list_prepend (assembly->array_methods, am);
	m->table_idx = am->token & 0xffffff;
	return am->token;
}

/*
 * Insert into the metadata tables all the info about the TypeBuilder tb.
 * Data in the tables is inserted in a predefined order, since some tables need to be sorted.
 */
static void
mono_image_get_type_info (MonoDomain *domain, MonoReflectionTypeBuilder *tb, MonoDynamicImage *assembly)
{
	MonoDynamicTable *table;
	guint *values;
	int i, is_object = 0, is_system = 0;
	char *n;

	table = &assembly->tables [MONO_TABLE_TYPEDEF];
	values = table->values + tb->table_idx * MONO_TYPEDEF_SIZE;
	values [MONO_TYPEDEF_FLAGS] = tb->attrs;
	n = mono_string_to_utf8 (tb->name);
	if (strcmp (n, "Object") == 0)
		is_object++;
	values [MONO_TYPEDEF_NAME] = string_heap_insert (&assembly->sheap, n);
	g_free (n);
	n = mono_string_to_utf8 (tb->nspace);
	if (strcmp (n, "System") == 0)
		is_system++;
	values [MONO_TYPEDEF_NAMESPACE] = string_heap_insert (&assembly->sheap, n);
	g_free (n);
	if (tb->parent && !(is_system && is_object) && 
			!(tb->attrs & TYPE_ATTRIBUTE_INTERFACE)) { /* interfaces don't have a parent */
		values [MONO_TYPEDEF_EXTENDS] = mono_image_typedef_or_ref (assembly, mono_reflection_type_get_handle ((MonoReflectionType*)tb->parent));
	} else {
		values [MONO_TYPEDEF_EXTENDS] = 0;
	}
	values [MONO_TYPEDEF_FIELD_LIST] = assembly->tables [MONO_TABLE_FIELD].next_idx;
	values [MONO_TYPEDEF_METHOD_LIST] = assembly->tables [MONO_TABLE_METHOD].next_idx;

	/*
	 * if we have explicitlayout or sequentiallayouts, output data in the
	 * ClassLayout table.
	 */
	if (((tb->attrs & TYPE_ATTRIBUTE_LAYOUT_MASK) != TYPE_ATTRIBUTE_AUTO_LAYOUT) &&
			((tb->class_size > 0) || (tb->packing_size > 0))) {
		table = &assembly->tables [MONO_TABLE_CLASSLAYOUT];
		table->rows++;
		alloc_table (table, table->rows);
		values = table->values + table->rows * MONO_CLASS_LAYOUT_SIZE;
		values [MONO_CLASS_LAYOUT_PARENT] = tb->table_idx;
		values [MONO_CLASS_LAYOUT_CLASS_SIZE] = tb->class_size;
		values [MONO_CLASS_LAYOUT_PACKING_SIZE] = tb->packing_size;
	}

	/* handle interfaces */
	if (tb->interfaces) {
		table = &assembly->tables [MONO_TABLE_INTERFACEIMPL];
		i = table->rows;
		table->rows += mono_array_length (tb->interfaces);
		alloc_table (table, table->rows);
		values = table->values + (i + 1) * MONO_INTERFACEIMPL_SIZE;
		for (i = 0; i < mono_array_length (tb->interfaces); ++i) {
			MonoReflectionType* iface = (MonoReflectionType*) mono_array_get (tb->interfaces, gpointer, i);
			values [MONO_INTERFACEIMPL_CLASS] = tb->table_idx;
			values [MONO_INTERFACEIMPL_INTERFACE] = mono_image_typedef_or_ref (assembly, mono_reflection_type_get_handle (iface));
			values += MONO_INTERFACEIMPL_SIZE;
		}
	}

	/* handle fields */
	if (tb->fields) {
		table = &assembly->tables [MONO_TABLE_FIELD];
		table->rows += tb->num_fields;
		alloc_table (table, table->rows);
		for (i = 0; i < tb->num_fields; ++i)
			mono_image_get_field_info (
				mono_array_get (tb->fields, MonoReflectionFieldBuilder*, i), assembly);
	}

	/* handle constructors */
	if (tb->ctors) {
		table = &assembly->tables [MONO_TABLE_METHOD];
		table->rows += mono_array_length (tb->ctors);
		alloc_table (table, table->rows);
		for (i = 0; i < mono_array_length (tb->ctors); ++i)
			mono_image_get_ctor_info (domain,
				mono_array_get (tb->ctors, MonoReflectionCtorBuilder*, i), assembly);
	}

	/* handle methods */
	if (tb->methods) {
		table = &assembly->tables [MONO_TABLE_METHOD];
		table->rows += tb->num_methods;
		alloc_table (table, table->rows);
		for (i = 0; i < tb->num_methods; ++i)
			mono_image_get_method_info (
				mono_array_get (tb->methods, MonoReflectionMethodBuilder*, i), assembly);
	}

	/* Do the same with properties etc.. */
	if (tb->events && mono_array_length (tb->events)) {
		table = &assembly->tables [MONO_TABLE_EVENT];
		table->rows += mono_array_length (tb->events);
		alloc_table (table, table->rows);
		table = &assembly->tables [MONO_TABLE_EVENTMAP];
		table->rows ++;
		alloc_table (table, table->rows);
		values = table->values + table->rows * MONO_EVENT_MAP_SIZE;
		values [MONO_EVENT_MAP_PARENT] = tb->table_idx;
		values [MONO_EVENT_MAP_EVENTLIST] = assembly->tables [MONO_TABLE_EVENT].next_idx;
		for (i = 0; i < mono_array_length (tb->events); ++i)
			mono_image_get_event_info (
				mono_array_get (tb->events, MonoReflectionEventBuilder*, i), assembly);
	}
	if (tb->properties && mono_array_length (tb->properties)) {
		table = &assembly->tables [MONO_TABLE_PROPERTY];
		table->rows += mono_array_length (tb->properties);
		alloc_table (table, table->rows);
		table = &assembly->tables [MONO_TABLE_PROPERTYMAP];
		table->rows ++;
		alloc_table (table, table->rows);
		values = table->values + table->rows * MONO_PROPERTY_MAP_SIZE;
		values [MONO_PROPERTY_MAP_PARENT] = tb->table_idx;
		values [MONO_PROPERTY_MAP_PROPERTY_LIST] = assembly->tables [MONO_TABLE_PROPERTY].next_idx;
		for (i = 0; i < mono_array_length (tb->properties); ++i)
			mono_image_get_property_info (
				mono_array_get (tb->properties, MonoReflectionPropertyBuilder*, i), assembly);
	}

	/* handle generic parameters */
	if (tb->generic_params) {
		table = &assembly->tables [MONO_TABLE_GENERICPARAM];
		table->rows += mono_array_length (tb->generic_params);
		alloc_table (table, table->rows);
		for (i = 0; i < mono_array_length (tb->generic_params); ++i) {
			guint32 owner = MONO_TYPEORMETHOD_TYPE | (tb->table_idx << MONO_TYPEORMETHOD_BITS);

			mono_image_get_generic_param_info (
				mono_array_get (tb->generic_params, MonoReflectionGenericParam*, i), owner, assembly);
		}
	}

	mono_image_add_decl_security (assembly, 
		mono_metadata_make_token (MONO_TABLE_TYPEDEF, tb->table_idx), tb->permissions);

	if (tb->subtypes) {
		MonoDynamicTable *ntable;
		
		ntable = &assembly->tables [MONO_TABLE_NESTEDCLASS];
		ntable->rows += mono_array_length (tb->subtypes);
		alloc_table (ntable, ntable->rows);
		values = ntable->values + ntable->next_idx * MONO_NESTED_CLASS_SIZE;

		for (i = 0; i < mono_array_length (tb->subtypes); ++i) {
			MonoReflectionTypeBuilder *subtype = mono_array_get (tb->subtypes, MonoReflectionTypeBuilder*, i);

			values [MONO_NESTED_CLASS_NESTED] = subtype->table_idx;
			values [MONO_NESTED_CLASS_ENCLOSING] = tb->table_idx;
			/*g_print ("nesting %s (%d) in %s (%d) (rows %d/%d)\n",
				mono_string_to_utf8 (subtype->name), subtype->table_idx,
				mono_string_to_utf8 (tb->name), tb->table_idx,
				ntable->next_idx, ntable->rows);*/
			values += MONO_NESTED_CLASS_SIZE;
			ntable->next_idx++;
		}
	}
}
#endif

static void
collect_types (MonoPtrArray *types, MonoReflectionTypeBuilder *type)
{
	int i;

	mono_ptr_array_append (*types, type);

	if (!type->subtypes)
		return;

	for (i = 0; i < mono_array_length (type->subtypes); ++i) {
		MonoReflectionTypeBuilder *subtype = mono_array_get (type->subtypes, MonoReflectionTypeBuilder*, i);
		collect_types (types, subtype);
	}
}

static gint
compare_types_by_table_idx (MonoReflectionTypeBuilder **type1, MonoReflectionTypeBuilder **type2)
{
	if ((*type1)->table_idx < (*type2)->table_idx)
		return -1;
	else
		if ((*type1)->table_idx > (*type2)->table_idx)
			return 1;
	else
		return 0;
}

static void
params_add_cattrs (MonoDynamicImage *assembly, MonoArray *pinfo) {
	int i;

	if (!pinfo)
		return;
	for (i = 0; i < mono_array_length (pinfo); ++i) {
		MonoReflectionParamBuilder *pb;
		pb = mono_array_get (pinfo, MonoReflectionParamBuilder *, i);
		if (!pb)
			continue;
		mono_image_add_cattrs (assembly, pb->table_idx, MONO_CUSTOM_ATTR_PARAMDEF, pb->cattrs);
	}
}

static void
type_add_cattrs (MonoDynamicImage *assembly, MonoReflectionTypeBuilder *tb) {
	int i;
	
	mono_image_add_cattrs (assembly, tb->table_idx, MONO_CUSTOM_ATTR_TYPEDEF, tb->cattrs);
	if (tb->fields) {
		for (i = 0; i < tb->num_fields; ++i) {
			MonoReflectionFieldBuilder* fb;
			fb = mono_array_get (tb->fields, MonoReflectionFieldBuilder*, i);
			mono_image_add_cattrs (assembly, fb->table_idx, MONO_CUSTOM_ATTR_FIELDDEF, fb->cattrs);
		}
	}
	if (tb->events) {
		for (i = 0; i < mono_array_length (tb->events); ++i) {
			MonoReflectionEventBuilder* eb;
			eb = mono_array_get (tb->events, MonoReflectionEventBuilder*, i);
			mono_image_add_cattrs (assembly, eb->table_idx, MONO_CUSTOM_ATTR_EVENT, eb->cattrs);
		}
	}
	if (tb->properties) {
		for (i = 0; i < mono_array_length (tb->properties); ++i) {
			MonoReflectionPropertyBuilder* pb;
			pb = mono_array_get (tb->properties, MonoReflectionPropertyBuilder*, i);
			mono_image_add_cattrs (assembly, pb->table_idx, MONO_CUSTOM_ATTR_PROPERTY, pb->cattrs);
		}
	}
	if (tb->ctors) {
		for (i = 0; i < mono_array_length (tb->ctors); ++i) {
			MonoReflectionCtorBuilder* cb;
			cb = mono_array_get (tb->ctors, MonoReflectionCtorBuilder*, i);
			mono_image_add_cattrs (assembly, cb->table_idx, MONO_CUSTOM_ATTR_METHODDEF, cb->cattrs);
			params_add_cattrs (assembly, cb->pinfo);
		}
	}

	if (tb->methods) {
		for (i = 0; i < tb->num_methods; ++i) {
			MonoReflectionMethodBuilder* mb;
			mb = mono_array_get (tb->methods, MonoReflectionMethodBuilder*, i);
			mono_image_add_cattrs (assembly, mb->table_idx, MONO_CUSTOM_ATTR_METHODDEF, mb->cattrs);
			params_add_cattrs (assembly, mb->pinfo);
		}
	}

	if (tb->subtypes) {
		for (i = 0; i < mono_array_length (tb->subtypes); ++i)
			type_add_cattrs (assembly, mono_array_get (tb->subtypes, MonoReflectionTypeBuilder*, i));
	}
}

static void
module_add_cattrs (MonoDynamicImage *assembly, MonoReflectionModuleBuilder *moduleb)
{
	int i;
	
	mono_image_add_cattrs (assembly, moduleb->table_idx, MONO_CUSTOM_ATTR_MODULE, moduleb->cattrs);

	if (moduleb->global_methods) {
		for (i = 0; i < mono_array_length (moduleb->global_methods); ++i) {
			MonoReflectionMethodBuilder* mb = mono_array_get (moduleb->global_methods, MonoReflectionMethodBuilder*, i);
			mono_image_add_cattrs (assembly, mb->table_idx, MONO_CUSTOM_ATTR_METHODDEF, mb->cattrs);
			params_add_cattrs (assembly, mb->pinfo);
		}
	}

	if (moduleb->global_fields) {
		for (i = 0; i < mono_array_length (moduleb->global_fields); ++i) {
			MonoReflectionFieldBuilder *fb = mono_array_get (moduleb->global_fields, MonoReflectionFieldBuilder*, i);
			mono_image_add_cattrs (assembly, fb->table_idx, MONO_CUSTOM_ATTR_FIELDDEF, fb->cattrs);
		}
	}
	
	if (moduleb->types) {
		for (i = 0; i < moduleb->num_types; ++i)
			type_add_cattrs (assembly, mono_array_get (moduleb->types, MonoReflectionTypeBuilder*, i));
	}
}

static void
mono_image_fill_file_table (MonoDomain *domain, MonoReflectionModule *module, MonoDynamicImage *assembly)
{
	MonoDynamicTable *table;
	guint32 *values;
	char blob_size [6];
	guchar hash [20];
	char *b = blob_size;
	char *dir, *path;

	table = &assembly->tables [MONO_TABLE_FILE];
	table->rows++;
	alloc_table (table, table->rows);
	values = table->values + table->next_idx * MONO_FILE_SIZE;
	values [MONO_FILE_FLAGS] = FILE_CONTAINS_METADATA;
	values [MONO_FILE_NAME] = string_heap_insert (&assembly->sheap, module->image->module_name);
	if (module->image->dynamic) {
		/* This depends on the fact that the main module is emitted last */
		dir = mono_string_to_utf8 (((MonoReflectionModuleBuilder*)module)->assemblyb->dir);
		path = g_strdup_printf ("%s%c%s", dir, G_DIR_SEPARATOR, module->image->module_name);
	} else {
		dir = NULL;
		path = g_strdup (module->image->name);
	}
	mono_sha1_get_digest_from_file (path, hash);
	g_free (dir);
	g_free (path);
	mono_metadata_encode_value (20, b, &b);
	values [MONO_FILE_HASH_VALUE] = mono_image_add_stream_data (&assembly->blob, blob_size, b-blob_size);
	mono_image_add_stream_data (&assembly->blob, (char*)hash, 20);
	table->next_idx ++;
}

static void
mono_image_fill_module_table (MonoDomain *domain, MonoReflectionModuleBuilder *mb, MonoDynamicImage *assembly)
{
	MonoDynamicTable *table;
	int i;

	table = &assembly->tables [MONO_TABLE_MODULE];
	mb->table_idx = table->next_idx ++;
	table->values [mb->table_idx * MONO_MODULE_SIZE + MONO_MODULE_NAME] = string_heap_insert_mstring (&assembly->sheap, mb->module.name);
	i = mono_image_add_stream_data (&assembly->guid, mono_array_addr (mb->guid, char, 0), 16);
	i /= 16;
	++i;
	table->values [mb->table_idx * MONO_MODULE_SIZE + MONO_MODULE_GENERATION] = 0;
	table->values [mb->table_idx * MONO_MODULE_SIZE + MONO_MODULE_MVID] = i;
	table->values [mb->table_idx * MONO_MODULE_SIZE + MONO_MODULE_ENC] = 0;
	table->values [mb->table_idx * MONO_MODULE_SIZE + MONO_MODULE_ENCBASE] = 0;
}

static guint32
mono_image_fill_export_table_from_class (MonoDomain *domain, MonoClass *klass,
	guint32 module_index, guint32 parent_index, MonoDynamicImage *assembly)
{
	MonoDynamicTable *table;
	guint32 *values;
	guint32 visib, res;

	visib = klass->flags & TYPE_ATTRIBUTE_VISIBILITY_MASK;
	if (! ((visib & TYPE_ATTRIBUTE_PUBLIC) || (visib & TYPE_ATTRIBUTE_NESTED_PUBLIC)))
		return 0;

	table = &assembly->tables [MONO_TABLE_EXPORTEDTYPE];
	table->rows++;
	alloc_table (table, table->rows);
	values = table->values + table->next_idx * MONO_EXP_TYPE_SIZE;

	values [MONO_EXP_TYPE_FLAGS] = klass->flags;
	values [MONO_EXP_TYPE_TYPEDEF] = klass->type_token;
	if (klass->nested_in)
		values [MONO_EXP_TYPE_IMPLEMENTATION] = (parent_index << MONO_IMPLEMENTATION_BITS) + MONO_IMPLEMENTATION_EXP_TYPE;
	else
		values [MONO_EXP_TYPE_IMPLEMENTATION] = (module_index << MONO_IMPLEMENTATION_BITS) + MONO_IMPLEMENTATION_FILE;
	values [MONO_EXP_TYPE_NAME] = string_heap_insert (&assembly->sheap, klass->name);
	values [MONO_EXP_TYPE_NAMESPACE] = string_heap_insert (&assembly->sheap, klass->name_space);

	res = table->next_idx;

	table->next_idx ++;

	/* Emit nested types */
	if (klass->ext && klass->ext->nested_classes) {
		GList *tmp;

		for (tmp = klass->ext->nested_classes; tmp; tmp = tmp->next)
			mono_image_fill_export_table_from_class (domain, tmp->data, module_index, table->next_idx - 1, assembly);
	}

	return res;
}

static void
mono_image_fill_export_table (MonoDomain *domain, MonoReflectionTypeBuilder *tb,
	guint32 module_index, guint32 parent_index, MonoDynamicImage *assembly)
{
	MonoClass *klass;
	guint32 idx, i;

	klass = mono_class_from_mono_type (mono_reflection_type_get_handle ((MonoReflectionType*)tb));

	klass->type_token = mono_metadata_make_token (MONO_TABLE_TYPEDEF, tb->table_idx);

	idx = mono_image_fill_export_table_from_class (domain, klass, module_index, 
												   parent_index, assembly);

	/* 
	 * Emit nested types
	 * We need to do this ourselves since klass->nested_classes is not set up.
	 */
	if (tb->subtypes) {
		for (i = 0; i < mono_array_length (tb->subtypes); ++i)
			mono_image_fill_export_table (domain, mono_array_get (tb->subtypes, MonoReflectionTypeBuilder*, i), module_index, idx, assembly);
	}
}

static void
mono_image_fill_export_table_from_module (MonoDomain *domain, MonoReflectionModule *module,
	guint32 module_index, MonoDynamicImage *assembly)
{
	MonoImage *image = module->image;
	MonoTableInfo  *t;
	guint32 i;

	t = &image->tables [MONO_TABLE_TYPEDEF];

	for (i = 0; i < t->rows; ++i) {
		MonoClass *klass = mono_class_get (image, mono_metadata_make_token (MONO_TABLE_TYPEDEF, i + 1));

		if (klass->flags & TYPE_ATTRIBUTE_PUBLIC)
			mono_image_fill_export_table_from_class (domain, klass, module_index, 0, assembly);
	}
}

static void
add_exported_type (MonoReflectionAssemblyBuilder *assemblyb, MonoDynamicImage *assembly, MonoClass *klass, guint32 parent_index)
{
	MonoDynamicTable *table;
	guint32 *values;
	guint32 scope, scope_idx, impl, current_idx;
	gboolean forwarder = TRUE;
	gpointer iter = NULL;
	MonoClass *nested;

	if (klass->nested_in) {
		impl = (parent_index << MONO_IMPLEMENTATION_BITS) + MONO_IMPLEMENTATION_EXP_TYPE;
		forwarder = FALSE;
	} else {
		scope = resolution_scope_from_image (assembly, klass->image);
		g_assert ((scope & MONO_RESOLTION_SCOPE_MASK) == MONO_RESOLTION_SCOPE_ASSEMBLYREF);
		scope_idx = scope >> MONO_RESOLTION_SCOPE_BITS;
		impl = (scope_idx << MONO_IMPLEMENTATION_BITS) + MONO_IMPLEMENTATION_ASSEMBLYREF;
	}

	table = &assembly->tables [MONO_TABLE_EXPORTEDTYPE];

	table->rows++;
	alloc_table (table, table->rows);
	current_idx = table->next_idx;
	values = table->values + current_idx * MONO_EXP_TYPE_SIZE;

	values [MONO_EXP_TYPE_FLAGS] = forwarder ? TYPE_ATTRIBUTE_FORWARDER : 0;
	values [MONO_EXP_TYPE_TYPEDEF] = 0;
	values [MONO_EXP_TYPE_IMPLEMENTATION] = impl;
	values [MONO_EXP_TYPE_NAME] = string_heap_insert (&assembly->sheap, klass->name);
	values [MONO_EXP_TYPE_NAMESPACE] = string_heap_insert (&assembly->sheap, klass->name_space);

	table->next_idx++;

	while ((nested = mono_class_get_nested_types (klass, &iter)))
		add_exported_type (assemblyb, assembly, nested, current_idx);
}

static void
mono_image_fill_export_table_from_type_forwarders (MonoReflectionAssemblyBuilder *assemblyb, MonoDynamicImage *assembly)
{
	MonoClass *klass;
	int i;

	if (!assemblyb->type_forwarders)
		return;

	for (i = 0; i < mono_array_length (assemblyb->type_forwarders); ++i) {
		MonoReflectionType *t = mono_array_get (assemblyb->type_forwarders, MonoReflectionType *, i);
		MonoType *type;
		if (!t)
			continue;

		type = mono_reflection_type_get_handle (t);
		g_assert (type);

		klass = mono_class_from_mono_type (type);

		add_exported_type (assemblyb, assembly, klass, 0);
	}
}

#define align_pointer(base,p)\
	do {\
		guint32 __diff = (unsigned char*)(p)-(unsigned char*)(base);\
		if (__diff & 3)\
			(p) += 4 - (__diff & 3);\
	} while (0)

static int
compare_constants (const void *a, const void *b)
{
	const guint32 *a_values = a;
	const guint32 *b_values = b;
	return a_values [MONO_CONSTANT_PARENT] - b_values [MONO_CONSTANT_PARENT];
}

static int
compare_semantics (const void *a, const void *b)
{
	const guint32 *a_values = a;
	const guint32 *b_values = b;
	int assoc = a_values [MONO_METHOD_SEMA_ASSOCIATION] - b_values [MONO_METHOD_SEMA_ASSOCIATION];
	if (assoc)
		return assoc;
	return a_values [MONO_METHOD_SEMA_SEMANTICS] - b_values [MONO_METHOD_SEMA_SEMANTICS];
}

static int
compare_custom_attrs (const void *a, const void *b)
{
	const guint32 *a_values = a;
	const guint32 *b_values = b;

	return a_values [MONO_CUSTOM_ATTR_PARENT] - b_values [MONO_CUSTOM_ATTR_PARENT];
}

static int
compare_field_marshal (const void *a, const void *b)
{
	const guint32 *a_values = a;
	const guint32 *b_values = b;

	return a_values [MONO_FIELD_MARSHAL_PARENT] - b_values [MONO_FIELD_MARSHAL_PARENT];
}

static int
compare_nested (const void *a, const void *b)
{
	const guint32 *a_values = a;
	const guint32 *b_values = b;

	return a_values [MONO_NESTED_CLASS_NESTED] - b_values [MONO_NESTED_CLASS_NESTED];
}

static int
compare_genericparam (const void *a, const void *b)
{
	const GenericParamTableEntry **a_entry = (const GenericParamTableEntry **) a;
	const GenericParamTableEntry **b_entry = (const GenericParamTableEntry **) b;

	if ((*b_entry)->owner == (*a_entry)->owner)
		return 
			mono_type_get_generic_param_num (mono_reflection_type_get_handle ((MonoReflectionType*)(*a_entry)->gparam)) -
			mono_type_get_generic_param_num (mono_reflection_type_get_handle ((MonoReflectionType*)(*b_entry)->gparam));
	else
		return (*a_entry)->owner - (*b_entry)->owner;
}

static int
compare_declsecurity_attrs (const void *a, const void *b)
{
	const guint32 *a_values = a;
	const guint32 *b_values = b;

	return a_values [MONO_DECL_SECURITY_PARENT] - b_values [MONO_DECL_SECURITY_PARENT];
}

static int
compare_interface_impl (const void *a, const void *b)
{
	const guint32 *a_values = a;
	const guint32 *b_values = b;

	int klass = a_values [MONO_INTERFACEIMPL_CLASS] - b_values [MONO_INTERFACEIMPL_CLASS];
	if (klass)
		return klass;

	return a_values [MONO_INTERFACEIMPL_INTERFACE] - b_values [MONO_INTERFACEIMPL_INTERFACE];
}

static void
pad_heap (MonoDynamicStream *sh)
{
	if (sh->index & 3) {
		int sz = 4 - (sh->index & 3);
		memset (sh->data + sh->index, 0, sz);
		sh->index += sz;
	}
}

struct StreamDesc {
	const char *name;
	MonoDynamicStream *stream;
};

/*
 * build_compressed_metadata() fills in the blob of data that represents the 
 * raw metadata as it will be saved in the PE file. The five streams are output 
 * and the metadata tables are comnpressed from the guint32 array representation, 
 * to the compressed on-disk format.
 */
static void
build_compressed_metadata (MonoDynamicImage *assembly)
{
	MonoDynamicTable *table;
	int i;
	guint64 valid_mask = 0;
	guint64 sorted_mask;
	guint32 heapt_size = 0;
	guint32 meta_size = 256; /* allow for header and other stuff */
	guint32 table_offset;
	guint32 ntables = 0;
	guint64 *int64val;
	guint32 *int32val;
	guint16 *int16val;
	MonoImage *meta;
	unsigned char *p;
	struct StreamDesc stream_desc [5];

	qsort (assembly->gen_params->pdata, assembly->gen_params->len, sizeof (gpointer), compare_genericparam);
	for (i = 0; i < assembly->gen_params->len; i++){
		GenericParamTableEntry *entry = g_ptr_array_index (assembly->gen_params, i);
		write_generic_param_entry (assembly, entry);
	}

	stream_desc [0].name  = "#~";
	stream_desc [0].stream = &assembly->tstream;
	stream_desc [1].name  = "#Strings";
	stream_desc [1].stream = &assembly->sheap;
	stream_desc [2].name  = "#US";
	stream_desc [2].stream = &assembly->us;
	stream_desc [3].name  = "#Blob";
	stream_desc [3].stream = &assembly->blob;
	stream_desc [4].name  = "#GUID";
	stream_desc [4].stream = &assembly->guid;
	
	/* tables that are sorted */
	sorted_mask = ((guint64)1 << MONO_TABLE_CONSTANT) | ((guint64)1 << MONO_TABLE_FIELDMARSHAL)
		| ((guint64)1 << MONO_TABLE_METHODSEMANTICS) | ((guint64)1 << MONO_TABLE_CLASSLAYOUT)
		| ((guint64)1 << MONO_TABLE_FIELDLAYOUT) | ((guint64)1 << MONO_TABLE_FIELDRVA)
		| ((guint64)1 << MONO_TABLE_IMPLMAP) | ((guint64)1 << MONO_TABLE_NESTEDCLASS)
		| ((guint64)1 << MONO_TABLE_METHODIMPL) | ((guint64)1 << MONO_TABLE_CUSTOMATTRIBUTE)
		| ((guint64)1 << MONO_TABLE_DECLSECURITY) | ((guint64)1 << MONO_TABLE_GENERICPARAM)
		| ((guint64)1 << MONO_TABLE_INTERFACEIMPL);
	
	/* Compute table sizes */
	/* the MonoImage has already been created in mono_image_basic_init() */
	meta = &assembly->image;

	/* sizes should be multiple of 4 */
	pad_heap (&assembly->blob);
	pad_heap (&assembly->guid);
	pad_heap (&assembly->sheap);
	pad_heap (&assembly->us);

	/* Setup the info used by compute_sizes () */
	meta->idx_blob_wide = assembly->blob.index >= 65536 ? 1 : 0;
	meta->idx_guid_wide = assembly->guid.index >= 65536 ? 1 : 0;
	meta->idx_string_wide = assembly->sheap.index >= 65536 ? 1 : 0;

	meta_size += assembly->blob.index;
	meta_size += assembly->guid.index;
	meta_size += assembly->sheap.index;
	meta_size += assembly->us.index;

	for (i=0; i < MONO_TABLE_NUM; ++i)
		meta->tables [i].rows = assembly->tables [i].rows;
	
	for (i = 0; i < MONO_TABLE_NUM; i++){
		if (meta->tables [i].rows == 0)
			continue;
		valid_mask |= (guint64)1 << i;
		ntables ++;
		meta->tables [i].row_size = mono_metadata_compute_size (
			meta, i, &meta->tables [i].size_bitfield);
		heapt_size += meta->tables [i].row_size * meta->tables [i].rows;
	}
	heapt_size += 24; /* #~ header size */
	heapt_size += ntables * 4;
	/* make multiple of 4 */
	heapt_size += 3;
	heapt_size &= ~3;
	meta_size += heapt_size;
	meta->raw_metadata = g_malloc0 (meta_size);
	p = (unsigned char*)meta->raw_metadata;
	/* the metadata signature */
	*p++ = 'B'; *p++ = 'S'; *p++ = 'J'; *p++ = 'B';
	/* version numbers and 4 bytes reserved */
	int16val = (guint16*)p;
	*int16val++ = GUINT16_TO_LE (meta->md_version_major);
	*int16val = GUINT16_TO_LE (meta->md_version_minor);
	p += 8;
	/* version string */
	int32val = (guint32*)p;
	*int32val = GUINT32_TO_LE ((strlen (meta->version) + 3) & (~3)); /* needs to be multiple of 4 */
	p += 4;
	memcpy (p, meta->version, strlen (meta->version));
	p += GUINT32_FROM_LE (*int32val);
	align_pointer (meta->raw_metadata, p);
	int16val = (guint16*)p;
	*int16val++ = GUINT16_TO_LE (0); /* flags must be 0 */
	*int16val = GUINT16_TO_LE (5); /* number of streams */
	p += 4;

	/*
	 * write the stream info.
	 */
	table_offset = (p - (unsigned char*)meta->raw_metadata) + 5 * 8 + 40; /* room needed for stream headers */
	table_offset += 3; table_offset &= ~3;

	assembly->tstream.index = heapt_size;
	for (i = 0; i < 5; ++i) {
		int32val = (guint32*)p;
		stream_desc [i].stream->offset = table_offset;
		*int32val++ = GUINT32_TO_LE (table_offset);
		*int32val = GUINT32_TO_LE (stream_desc [i].stream->index);
		table_offset += GUINT32_FROM_LE (*int32val);
		table_offset += 3; table_offset &= ~3;
		p += 8;
		strcpy ((char*)p, stream_desc [i].name);
		p += strlen (stream_desc [i].name) + 1;
		align_pointer (meta->raw_metadata, p);
	}
	/* 
	 * now copy the data, the table stream header and contents goes first.
	 */
	g_assert ((p - (unsigned char*)meta->raw_metadata) < assembly->tstream.offset);
	p = (guchar*)meta->raw_metadata + assembly->tstream.offset;
	int32val = (guint32*)p;
	*int32val = GUINT32_TO_LE (0); /* reserved */
	p += 4;

	*p++ = 2; /* version */
	*p++ = 0;

	if (meta->idx_string_wide)
		*p |= 0x01;
	if (meta->idx_guid_wide)
		*p |= 0x02;
	if (meta->idx_blob_wide)
		*p |= 0x04;
	++p;
	*p++ = 1; /* reserved */
	int64val = (guint64*)p;
	*int64val++ = GUINT64_TO_LE (valid_mask);
	*int64val++ = GUINT64_TO_LE (valid_mask & sorted_mask); /* bitvector of sorted tables  */
	p += 16;
	int32val = (guint32*)p;
	for (i = 0; i < MONO_TABLE_NUM; i++){
		if (meta->tables [i].rows == 0)
			continue;
		*int32val++ = GUINT32_TO_LE (meta->tables [i].rows);
	}
	p = (unsigned char*)int32val;

	/* sort the tables that still need sorting */
	table = &assembly->tables [MONO_TABLE_CONSTANT];
	if (table->rows)
		qsort (table->values + MONO_CONSTANT_SIZE, table->rows, sizeof (guint32) * MONO_CONSTANT_SIZE, compare_constants);
	table = &assembly->tables [MONO_TABLE_METHODSEMANTICS];
	if (table->rows)
		qsort (table->values + MONO_METHOD_SEMA_SIZE, table->rows, sizeof (guint32) * MONO_METHOD_SEMA_SIZE, compare_semantics);
	table = &assembly->tables [MONO_TABLE_CUSTOMATTRIBUTE];
	if (table->rows)
		qsort (table->values + MONO_CUSTOM_ATTR_SIZE, table->rows, sizeof (guint32) * MONO_CUSTOM_ATTR_SIZE, compare_custom_attrs);
	table = &assembly->tables [MONO_TABLE_FIELDMARSHAL];
	if (table->rows)
		qsort (table->values + MONO_FIELD_MARSHAL_SIZE, table->rows, sizeof (guint32) * MONO_FIELD_MARSHAL_SIZE, compare_field_marshal);
	table = &assembly->tables [MONO_TABLE_NESTEDCLASS];
	if (table->rows)
		qsort (table->values + MONO_NESTED_CLASS_SIZE, table->rows, sizeof (guint32) * MONO_NESTED_CLASS_SIZE, compare_nested);
	/* Section 21.11 DeclSecurity in Partition II doesn't specify this to be sorted by MS implementation requires it */
	table = &assembly->tables [MONO_TABLE_DECLSECURITY];
	if (table->rows)
		qsort (table->values + MONO_DECL_SECURITY_SIZE, table->rows, sizeof (guint32) * MONO_DECL_SECURITY_SIZE, compare_declsecurity_attrs);
	table = &assembly->tables [MONO_TABLE_INTERFACEIMPL];
	if (table->rows)
		qsort (table->values + MONO_INTERFACEIMPL_SIZE, table->rows, sizeof (guint32) * MONO_INTERFACEIMPL_SIZE, compare_interface_impl);

	/* compress the tables */
	for (i = 0; i < MONO_TABLE_NUM; i++){
		int row, col;
		guint32 *values;
		guint32 bitfield = meta->tables [i].size_bitfield;
		if (!meta->tables [i].rows)
			continue;
		if (assembly->tables [i].columns != mono_metadata_table_count (bitfield))
			g_error ("col count mismatch in %d: %d %d", i, assembly->tables [i].columns, mono_metadata_table_count (bitfield));
		meta->tables [i].base = (char*)p;
		for (row = 1; row <= meta->tables [i].rows; ++row) {
			values = assembly->tables [i].values + row * assembly->tables [i].columns;
			for (col = 0; col < assembly->tables [i].columns; ++col) {
				switch (mono_metadata_table_size (bitfield, col)) {
				case 1:
					*p++ = values [col];
					break;
				case 2:
					*p++ = values [col] & 0xff;
					*p++ = (values [col] >> 8) & 0xff;
					break;
				case 4:
					*p++ = values [col] & 0xff;
					*p++ = (values [col] >> 8) & 0xff;
					*p++ = (values [col] >> 16) & 0xff;
					*p++ = (values [col] >> 24) & 0xff;
					break;
				default:
					g_assert_not_reached ();
				}
			}
		}
		g_assert ((p - (const unsigned char*)meta->tables [i].base) == (meta->tables [i].rows * meta->tables [i].row_size));
	}
	
	g_assert (assembly->guid.offset + assembly->guid.index < meta_size);
	memcpy (meta->raw_metadata + assembly->sheap.offset, assembly->sheap.data, assembly->sheap.index);
	memcpy (meta->raw_metadata + assembly->us.offset, assembly->us.data, assembly->us.index);
	memcpy (meta->raw_metadata + assembly->blob.offset, assembly->blob.data, assembly->blob.index);
	memcpy (meta->raw_metadata + assembly->guid.offset, assembly->guid.data, assembly->guid.index);

	assembly->meta_size = assembly->guid.offset + assembly->guid.index;
}

/*
 * Some tables in metadata need to be sorted according to some criteria, but
 * when methods and fields are first created with reflection, they may be assigned a token
 * that doesn't correspond to the final token they will get assigned after the sorting.
 * ILGenerator.cs keeps a fixup table that maps the position of tokens in the IL code stream
 * with the reflection objects that represent them. Once all the tables are set up, the 
 * reflection objects will contains the correct table index. fixup_method() will fixup the
 * tokens for the method with ILGenerator @ilgen.
 */
static void
fixup_method (MonoReflectionILGen *ilgen, gpointer value, MonoDynamicImage *assembly)
{
	guint32 code_idx = GPOINTER_TO_UINT (value);
	MonoReflectionILTokenInfo *iltoken;
	MonoReflectionFieldBuilder *field;
	MonoReflectionCtorBuilder *ctor;
	MonoReflectionMethodBuilder *method;
	MonoReflectionTypeBuilder *tb;
	MonoReflectionArrayMethod *am;
	guint32 i, idx = 0;
	unsigned char *target;

	for (i = 0; i < ilgen->num_token_fixups; ++i) {
		iltoken = (MonoReflectionILTokenInfo *)mono_array_addr_with_size (ilgen->token_fixups, sizeof (MonoReflectionILTokenInfo), i);
		target = (guchar*)assembly->code.data + code_idx + iltoken->code_pos;
		switch (target [3]) {
		case MONO_TABLE_FIELD:
			if (!strcmp (iltoken->member->vtable->klass->name, "FieldBuilder")) {
				field = (MonoReflectionFieldBuilder *)iltoken->member;
				idx = field->table_idx;
			} else if (!strcmp (iltoken->member->vtable->klass->name, "MonoField")) {
				MonoClassField *f = ((MonoReflectionField*)iltoken->member)->field;
				idx = GPOINTER_TO_UINT (g_hash_table_lookup (assembly->field_to_table_idx, f));
			} else {
				g_assert_not_reached ();
			}
			break;
		case MONO_TABLE_METHOD:
			if (!strcmp (iltoken->member->vtable->klass->name, "MethodBuilder")) {
				method = (MonoReflectionMethodBuilder *)iltoken->member;
				idx = method->table_idx;
			} else if (!strcmp (iltoken->member->vtable->klass->name, "ConstructorBuilder")) {
				ctor = (MonoReflectionCtorBuilder *)iltoken->member;
				idx = ctor->table_idx;
			} else if (!strcmp (iltoken->member->vtable->klass->name, "MonoMethod") || 
					   !strcmp (iltoken->member->vtable->klass->name, "MonoCMethod")) {
				MonoMethod *m = ((MonoReflectionMethod*)iltoken->member)->method;
				idx = GPOINTER_TO_UINT (g_hash_table_lookup (assembly->method_to_table_idx, m));
			} else {
				g_assert_not_reached ();
			}
			break;
		case MONO_TABLE_TYPEDEF:
			if (strcmp (iltoken->member->vtable->klass->name, "TypeBuilder"))
				g_assert_not_reached ();
			tb = (MonoReflectionTypeBuilder *)iltoken->member;
			idx = tb->table_idx;
			break;
		case MONO_TABLE_MEMBERREF:
			if (!strcmp (iltoken->member->vtable->klass->name, "MonoArrayMethod")) {
				am = (MonoReflectionArrayMethod*)iltoken->member;
				idx = am->table_idx;
			} else if (!strcmp (iltoken->member->vtable->klass->name, "MonoMethod") ||
				   !strcmp (iltoken->member->vtable->klass->name, "MonoCMethod") ||
				   !strcmp (iltoken->member->vtable->klass->name, "MonoGenericMethod") ||
				   !strcmp (iltoken->member->vtable->klass->name, "MonoGenericCMethod")) {
				MonoMethod *m = ((MonoReflectionMethod*)iltoken->member)->method;
				g_assert (m->klass->generic_class || m->klass->generic_container);
				continue;
			} else if (!strcmp (iltoken->member->vtable->klass->name, "FieldBuilder")) {
				continue;
			} else if (!strcmp (iltoken->member->vtable->klass->name, "MonoField")) {
				MonoClassField *f = ((MonoReflectionField*)iltoken->member)->field;
				g_assert (is_field_on_inst (f));
				continue;
			} else if (!strcmp (iltoken->member->vtable->klass->name, "MethodBuilder") ||
					!strcmp (iltoken->member->vtable->klass->name, "ConstructorBuilder")) {
				continue;
			} else if (!strcmp (iltoken->member->vtable->klass->name, "FieldOnTypeBuilderInst")) {
				continue;
			} else if (!strcmp (iltoken->member->vtable->klass->name, "MethodOnTypeBuilderInst")) {
				continue;
			} else if (!strcmp (iltoken->member->vtable->klass->name, "ConstructorOnTypeBuilderInst")) {
				continue;
			} else {
				g_assert_not_reached ();
			}
			break;
		case MONO_TABLE_METHODSPEC:
			if (!strcmp (iltoken->member->vtable->klass->name, "MonoGenericMethod")) {
				MonoMethod *m = ((MonoReflectionMethod*)iltoken->member)->method;
				g_assert (mono_method_signature (m)->generic_param_count);
				continue;
			} else if (!strcmp (iltoken->member->vtable->klass->name, "MethodBuilder")) {
				continue;
			} else if (!strcmp (iltoken->member->vtable->klass->name, "MethodOnTypeBuilderInst")) {
				continue;
			} else {
				g_assert_not_reached ();
			}
			break;
		default:
			g_error ("got unexpected table 0x%02x in fixup", target [3]);
		}
		target [0] = idx & 0xff;
		target [1] = (idx >> 8) & 0xff;
		target [2] = (idx >> 16) & 0xff;
	}
}

/*
 * fixup_cattrs:
 *
 *   The CUSTOM_ATTRIBUTE table might contain METHODDEF tokens whose final
 * value is not known when the table is emitted.
 */
static void
fixup_cattrs (MonoDynamicImage *assembly)
{
	MonoDynamicTable *table;
	guint32 *values;
	guint32 type, i, idx, token;
	MonoObject *ctor;

	table = &assembly->tables [MONO_TABLE_CUSTOMATTRIBUTE];

	for (i = 0; i < table->rows; ++i) {
		values = table->values + ((i + 1) * MONO_CUSTOM_ATTR_SIZE);

		type = values [MONO_CUSTOM_ATTR_TYPE];
		if ((type & MONO_CUSTOM_ATTR_TYPE_MASK) == MONO_CUSTOM_ATTR_TYPE_METHODDEF) {
			idx = type >> MONO_CUSTOM_ATTR_TYPE_BITS;
			token = mono_metadata_make_token (MONO_TABLE_METHOD, idx);
			ctor = mono_g_hash_table_lookup (assembly->remapped_tokens, GUINT_TO_POINTER (token));
			g_assert (ctor);

			if (!strcmp (ctor->vtable->klass->name, "MonoCMethod")) {
				MonoMethod *m = ((MonoReflectionMethod*)ctor)->method;
				idx = GPOINTER_TO_UINT (g_hash_table_lookup (assembly->method_to_table_idx, m));
				values [MONO_CUSTOM_ATTR_TYPE] = (idx << MONO_CUSTOM_ATTR_TYPE_BITS) | MONO_CUSTOM_ATTR_TYPE_METHODDEF;
			} else if (!strcmp (ctor->vtable->klass->name, "ConstructorBuilder")) {
				MonoMethod *m = ((MonoReflectionCtorBuilder*)ctor)->mhandle;
				idx = GPOINTER_TO_UINT (g_hash_table_lookup (assembly->method_to_table_idx, m));
				values [MONO_CUSTOM_ATTR_TYPE] = (idx << MONO_CUSTOM_ATTR_TYPE_BITS) | MONO_CUSTOM_ATTR_TYPE_METHODDEF;
			}
		}
	}
}

static void
assembly_add_resource_manifest (MonoReflectionModuleBuilder *mb, MonoDynamicImage *assembly, MonoReflectionResource *rsrc, guint32 implementation)
{
	MonoDynamicTable *table;
	guint32 *values;

	table = &assembly->tables [MONO_TABLE_MANIFESTRESOURCE];
	table->rows++;
	alloc_table (table, table->rows);
	values = table->values + table->next_idx * MONO_MANIFEST_SIZE;
	values [MONO_MANIFEST_OFFSET] = rsrc->offset;
	values [MONO_MANIFEST_FLAGS] = rsrc->attrs;
	values [MONO_MANIFEST_NAME] = string_heap_insert_mstring (&assembly->sheap, rsrc->name);
	values [MONO_MANIFEST_IMPLEMENTATION] = implementation;
	table->next_idx++;
}

static void
assembly_add_resource (MonoReflectionModuleBuilder *mb, MonoDynamicImage *assembly, MonoReflectionResource *rsrc)
{
	MonoDynamicTable *table;
	guint32 *values;
	char blob_size [6];
	guchar hash [20];
	char *b = blob_size;
	char *name, *sname;
	guint32 idx, offset;

	if (rsrc->filename) {
		name = mono_string_to_utf8 (rsrc->filename);
		sname = g_path_get_basename (name);
	
		table = &assembly->tables [MONO_TABLE_FILE];
		table->rows++;
		alloc_table (table, table->rows);
		values = table->values + table->next_idx * MONO_FILE_SIZE;
		values [MONO_FILE_FLAGS] = FILE_CONTAINS_NO_METADATA;
		values [MONO_FILE_NAME] = string_heap_insert (&assembly->sheap, sname);
		g_free (sname);

		mono_sha1_get_digest_from_file (name, hash);
		mono_metadata_encode_value (20, b, &b);
		values [MONO_FILE_HASH_VALUE] = mono_image_add_stream_data (&assembly->blob, blob_size, b-blob_size);
		mono_image_add_stream_data (&assembly->blob, (char*)hash, 20);
		g_free (name);
		idx = table->next_idx++;
		rsrc->offset = 0;
		idx = MONO_IMPLEMENTATION_FILE | (idx << MONO_IMPLEMENTATION_BITS);
	} else {
		char sizebuf [4];
		char *data;
		guint len;
		if (rsrc->data) {
			data = mono_array_addr (rsrc->data, char, 0);
			len = mono_array_length (rsrc->data);
		} else {
			data = NULL;
			len = 0;
		}
		offset = len;
		sizebuf [0] = offset; sizebuf [1] = offset >> 8;
		sizebuf [2] = offset >> 16; sizebuf [3] = offset >> 24;
		rsrc->offset = mono_image_add_stream_data (&assembly->resources, sizebuf, 4);
		mono_image_add_stream_data (&assembly->resources, data, len);

		if (!mb->is_main)
			/* 
			 * The entry should be emitted into the MANIFESTRESOURCE table of 
			 * the main module, but that needs to reference the FILE table
			 * which isn't emitted yet.
			 */
			return;
		else
			idx = 0;
	}

	assembly_add_resource_manifest (mb, assembly, rsrc, idx);
}

static void
set_version_from_string (MonoString *version, guint32 *values)
{
	gchar *ver, *p, *str;
	guint32 i;
	
	values [MONO_ASSEMBLY_MAJOR_VERSION] = 0;
	values [MONO_ASSEMBLY_MINOR_VERSION] = 0;
	values [MONO_ASSEMBLY_REV_NUMBER] = 0;
	values [MONO_ASSEMBLY_BUILD_NUMBER] = 0;
	if (!version)
		return;
	ver = str = mono_string_to_utf8 (version);
	for (i = 0; i < 4; ++i) {
		values [MONO_ASSEMBLY_MAJOR_VERSION + i] = strtol (ver, &p, 10);
		switch (*p) {
		case '.':
			p++;
			break;
		case '*':
			/* handle Revision and Build */
			p++;
			break;
		}
		ver = p;
	}
	g_free (str);
}

static guint32
load_public_key (MonoArray *pkey, MonoDynamicImage *assembly) {
	gsize len;
	guint32 token = 0;
	char blob_size [6];
	char *b = blob_size;

	if (!pkey)
		return token;

	len = mono_array_length (pkey);
	mono_metadata_encode_value (len, b, &b);
	token = mono_image_add_stream_data (&assembly->blob, blob_size, b - blob_size);
	mono_image_add_stream_data (&assembly->blob, mono_array_addr (pkey, char, 0), len);

	assembly->public_key = g_malloc (len);
	memcpy (assembly->public_key, mono_array_addr (pkey, char, 0), len);
	assembly->public_key_len = len;

	/* Special case: check for ECMA key (16 bytes) */
	if ((len == MONO_ECMA_KEY_LENGTH) && mono_is_ecma_key (mono_array_addr (pkey, char, 0), len)) {
		/* In this case we must reserve 128 bytes (1024 bits) for the signature */
		assembly->strong_name_size = MONO_DEFAULT_PUBLIC_KEY_LENGTH;
	} else if (len >= MONO_PUBLIC_KEY_HEADER_LENGTH + MONO_MINIMUM_PUBLIC_KEY_LENGTH) {
		/* minimum key size (in 2.0) is 384 bits */
		assembly->strong_name_size = len - MONO_PUBLIC_KEY_HEADER_LENGTH;
	} else {
		/* FIXME - verifier */
		g_warning ("Invalid public key length: %d bits (total: %d)", (int)MONO_PUBLIC_KEY_BIT_SIZE (len), (int)len);
		assembly->strong_name_size = MONO_DEFAULT_PUBLIC_KEY_LENGTH; /* to be safe */
	}
	assembly->strong_name = g_malloc0 (assembly->strong_name_size);

	return token;
}

static void
mono_image_emit_manifest (MonoReflectionModuleBuilder *moduleb)
{
	MonoDynamicTable *table;
	MonoDynamicImage *assembly;
	MonoReflectionAssemblyBuilder *assemblyb;
	MonoDomain *domain;
	guint32 *values;
	int i;
	guint32 module_index;

	assemblyb = moduleb->assemblyb;
	assembly = moduleb->dynamic_image;
	domain = mono_object_domain (assemblyb);

	/* Emit ASSEMBLY table */
	table = &assembly->tables [MONO_TABLE_ASSEMBLY];
	alloc_table (table, 1);
	values = table->values + MONO_ASSEMBLY_SIZE;
	values [MONO_ASSEMBLY_HASH_ALG] = assemblyb->algid? assemblyb->algid: ASSEMBLY_HASH_SHA1;
	values [MONO_ASSEMBLY_NAME] = string_heap_insert_mstring (&assembly->sheap, assemblyb->name);
	if (assemblyb->culture) {
		values [MONO_ASSEMBLY_CULTURE] = string_heap_insert_mstring (&assembly->sheap, assemblyb->culture);
	} else {
		values [MONO_ASSEMBLY_CULTURE] = string_heap_insert (&assembly->sheap, "");
	}
	values [MONO_ASSEMBLY_PUBLIC_KEY] = load_public_key (assemblyb->public_key, assembly);
	values [MONO_ASSEMBLY_FLAGS] = assemblyb->flags;
	set_version_from_string (assemblyb->version, values);

	/* Emit FILE + EXPORTED_TYPE table */
	module_index = 0;
	for (i = 0; i < mono_array_length (assemblyb->modules); ++i) {
		int j;
		MonoReflectionModuleBuilder *file_module = 
			mono_array_get (assemblyb->modules, MonoReflectionModuleBuilder*, i);
		if (file_module != moduleb) {
			mono_image_fill_file_table (domain, (MonoReflectionModule*)file_module, assembly);
			module_index ++;
			if (file_module->types) {
				for (j = 0; j < file_module->num_types; ++j) {
					MonoReflectionTypeBuilder *tb = mono_array_get (file_module->types, MonoReflectionTypeBuilder*, j);
					mono_image_fill_export_table (domain, tb, module_index, 0, assembly);
				}
			}
		}
	}
	if (assemblyb->loaded_modules) {
		for (i = 0; i < mono_array_length (assemblyb->loaded_modules); ++i) {
			MonoReflectionModule *file_module = 
				mono_array_get (assemblyb->loaded_modules, MonoReflectionModule*, i);
			mono_image_fill_file_table (domain, file_module, assembly);
			module_index ++;
			mono_image_fill_export_table_from_module (domain, file_module, module_index, assembly);
		}
	}
	if (assemblyb->type_forwarders)
		mono_image_fill_export_table_from_type_forwarders (assemblyb, assembly);

	/* Emit MANIFESTRESOURCE table */
	module_index = 0;
	for (i = 0; i < mono_array_length (assemblyb->modules); ++i) {
		int j;
		MonoReflectionModuleBuilder *file_module = 
			mono_array_get (assemblyb->modules, MonoReflectionModuleBuilder*, i);
		/* The table for the main module is emitted later */
		if (file_module != moduleb) {
			module_index ++;
			if (file_module->resources) {
				int len = mono_array_length (file_module->resources);
				for (j = 0; j < len; ++j) {
					MonoReflectionResource* res = (MonoReflectionResource*)mono_array_addr (file_module->resources, MonoReflectionResource, j);
					assembly_add_resource_manifest (file_module, assembly, res, MONO_IMPLEMENTATION_FILE | (module_index << MONO_IMPLEMENTATION_BITS));
				}
			}
		}
	}		
}

#ifndef DISABLE_REFLECTION_EMIT_SAVE

/*
 * mono_image_build_metadata() will fill the info in all the needed metadata tables
 * for the modulebuilder @moduleb.
 * At the end of the process, method and field tokens are fixed up and the 
 * on-disk compressed metadata representation is created.
 */
void
mono_image_build_metadata (MonoReflectionModuleBuilder *moduleb)
{
	MonoDynamicTable *table;
	MonoDynamicImage *assembly;
	MonoReflectionAssemblyBuilder *assemblyb;
	MonoDomain *domain;
	MonoPtrArray types;
	guint32 *values;
	int i, j;

	assemblyb = moduleb->assemblyb;
	assembly = moduleb->dynamic_image;
	domain = mono_object_domain (assemblyb);

	if (assembly->text_rva)
		return;

	assembly->text_rva = START_TEXT_RVA;

	if (moduleb->is_main) {
		mono_image_emit_manifest (moduleb);
	}

	table = &assembly->tables [MONO_TABLE_TYPEDEF];
	table->rows = 1; /* .<Module> */
	table->next_idx++;
	alloc_table (table, table->rows);
	/*
	 * Set the first entry.
	 */
	values = table->values + table->columns;
	values [MONO_TYPEDEF_FLAGS] = 0;
	values [MONO_TYPEDEF_NAME] = string_heap_insert (&assembly->sheap, "<Module>") ;
	values [MONO_TYPEDEF_NAMESPACE] = string_heap_insert (&assembly->sheap, "") ;
	values [MONO_TYPEDEF_EXTENDS] = 0;
	values [MONO_TYPEDEF_FIELD_LIST] = 1;
	values [MONO_TYPEDEF_METHOD_LIST] = 1;

	/* 
	 * handle global methods 
	 * FIXME: test what to do when global methods are defined in multiple modules.
	 */
	if (moduleb->global_methods) {
		table = &assembly->tables [MONO_TABLE_METHOD];
		table->rows += mono_array_length (moduleb->global_methods);
		alloc_table (table, table->rows);
		for (i = 0; i < mono_array_length (moduleb->global_methods); ++i)
			mono_image_get_method_info (
				mono_array_get (moduleb->global_methods, MonoReflectionMethodBuilder*, i), assembly);
	}
	if (moduleb->global_fields) {
		table = &assembly->tables [MONO_TABLE_FIELD];
		table->rows += mono_array_length (moduleb->global_fields);
		alloc_table (table, table->rows);
		for (i = 0; i < mono_array_length (moduleb->global_fields); ++i)
			mono_image_get_field_info (
				mono_array_get (moduleb->global_fields, MonoReflectionFieldBuilder*, i), assembly);
	}

	table = &assembly->tables [MONO_TABLE_MODULE];
	alloc_table (table, 1);
	mono_image_fill_module_table (domain, moduleb, assembly);

	/* Collect all types into a list sorted by their table_idx */
	mono_ptr_array_init (types, moduleb->num_types);

	if (moduleb->types)
		for (i = 0; i < moduleb->num_types; ++i) {
			MonoReflectionTypeBuilder *type = mono_array_get (moduleb->types, MonoReflectionTypeBuilder*, i);
			collect_types (&types, type);
		}

	mono_ptr_array_sort (types, (gpointer)compare_types_by_table_idx);
	table = &assembly->tables [MONO_TABLE_TYPEDEF];
	table->rows += mono_ptr_array_size (types);
	alloc_table (table, table->rows);

	/*
	 * Emit type names + namespaces at one place inside the string heap,
	 * so load_class_names () needs to touch fewer pages.
	 */
	for (i = 0; i < mono_ptr_array_size (types); ++i) {
		MonoReflectionTypeBuilder *tb = mono_ptr_array_get (types, i);
		string_heap_insert_mstring (&assembly->sheap, tb->nspace);
	}
	for (i = 0; i < mono_ptr_array_size (types); ++i) {
		MonoReflectionTypeBuilder *tb = mono_ptr_array_get (types, i);
		string_heap_insert_mstring (&assembly->sheap, tb->name);
	}

	for (i = 0; i < mono_ptr_array_size (types); ++i) {
		MonoReflectionTypeBuilder *type = mono_ptr_array_get (types, i);
		mono_image_get_type_info (domain, type, assembly);
	}

	/* 
	 * table->rows is already set above and in mono_image_fill_module_table.
	 */
	/* add all the custom attributes at the end, once all the indexes are stable */
	mono_image_add_cattrs (assembly, 1, MONO_CUSTOM_ATTR_ASSEMBLY, assemblyb->cattrs);

	/* CAS assembly permissions */
	if (assemblyb->permissions_minimum)
		mono_image_add_decl_security (assembly, mono_metadata_make_token (MONO_TABLE_ASSEMBLY, 1), assemblyb->permissions_minimum);
	if (assemblyb->permissions_optional)
		mono_image_add_decl_security (assembly, mono_metadata_make_token (MONO_TABLE_ASSEMBLY, 1), assemblyb->permissions_optional);
	if (assemblyb->permissions_refused)
		mono_image_add_decl_security (assembly, mono_metadata_make_token (MONO_TABLE_ASSEMBLY, 1), assemblyb->permissions_refused);

	module_add_cattrs (assembly, moduleb);

	/* fixup tokens */
	mono_g_hash_table_foreach (assembly->token_fixups, (GHFunc)fixup_method, assembly);

	/* Create the MethodImpl table.  We do this after emitting all methods so we already know
	 * the final tokens and don't need another fixup pass. */

	if (moduleb->global_methods) {
		for (i = 0; i < mono_array_length (moduleb->global_methods); ++i) {
			MonoReflectionMethodBuilder *mb = mono_array_get (
				moduleb->global_methods, MonoReflectionMethodBuilder*, i);
			mono_image_add_methodimpl (assembly, mb);
		}
	}

	for (i = 0; i < mono_ptr_array_size (types); ++i) {
		MonoReflectionTypeBuilder *type = mono_ptr_array_get (types, i);
		if (type->methods) {
			for (j = 0; j < type->num_methods; ++j) {
				MonoReflectionMethodBuilder *mb = mono_array_get (
					type->methods, MonoReflectionMethodBuilder*, j);

				mono_image_add_methodimpl (assembly, mb);
			}
		}
	}

	mono_ptr_array_destroy (types);

	fixup_cattrs (assembly);
}

#else /* DISABLE_REFLECTION_EMIT_SAVE */

void
mono_image_build_metadata (MonoReflectionModuleBuilder *moduleb)
{
	g_error ("This mono runtime was configured with --enable-minimal=reflection_emit_save, so saving of dynamic assemblies is not supported.");
}

#endif /* DISABLE_REFLECTION_EMIT_SAVE */


typedef struct {
	guint32 import_lookup_table;
	guint32 timestamp;
	guint32 forwarder;
	guint32 name_rva;
	guint32 import_address_table_rva;
} MonoIDT;

typedef struct {
	guint32 name_rva;
	guint32 flags;
} MonoILT;

#ifndef DISABLE_REFLECTION_EMIT

/*
 * mono_image_insert_string:
 * @module: module builder object
 * @str: a string
 *
 * Insert @str into the user string stream of @module.
 */
guint32
mono_image_insert_string (MonoReflectionModuleBuilder *module, MonoString *str)
{
	MonoDynamicImage *assembly;
	guint32 idx;
	char buf [16];
	char *b = buf;
	
	MONO_ARCH_SAVE_REGS;

	if (!module->dynamic_image)
		mono_image_module_basic_init (module);

	assembly = module->dynamic_image;
	
	if (assembly->save) {
		mono_metadata_encode_value (1 | (str->length * 2), b, &b);
		idx = mono_image_add_stream_data (&assembly->us, buf, b-buf);
#if G_BYTE_ORDER != G_LITTLE_ENDIAN
	{
		char *swapped = g_malloc (2 * mono_string_length (str));
		const char *p = (const char*)mono_string_chars (str);

		swap_with_size (swapped, p, 2, mono_string_length (str));
		mono_image_add_stream_data (&assembly->us, swapped, str->length * 2);
		g_free (swapped);
	}
#else
		mono_image_add_stream_data (&assembly->us, (const char*)mono_string_chars (str), str->length * 2);
#endif
		mono_image_add_stream_data (&assembly->us, "", 1);
	} else {
		idx = assembly->us.index ++;
	}

	mono_g_hash_table_insert (assembly->tokens, GUINT_TO_POINTER (MONO_TOKEN_STRING | idx), str);

	return MONO_TOKEN_STRING | idx;
}

guint32
mono_image_create_method_token (MonoDynamicImage *assembly, MonoObject *obj, MonoArray *opt_param_types)
{
	MonoClass *klass;
	guint32 token = 0;
	MonoMethodSignature *sig;

	klass = obj->vtable->klass;
	if (strcmp (klass->name, "MonoMethod") == 0) {
		MonoMethod *method = ((MonoReflectionMethod *)obj)->method;
		MonoMethodSignature *old;
		guint32 sig_token, parent;
		int nargs, i;

		g_assert (opt_param_types && (mono_method_signature (method)->sentinelpos >= 0));

		nargs = mono_array_length (opt_param_types);
		old = mono_method_signature (method);
		sig = mono_metadata_signature_alloc ( &assembly->image, old->param_count + nargs);

		sig->hasthis = old->hasthis;
		sig->explicit_this = old->explicit_this;
		sig->call_convention = old->call_convention;
		sig->generic_param_count = old->generic_param_count;
		sig->param_count = old->param_count + nargs;
		sig->sentinelpos = old->param_count;
		sig->ret = old->ret;

		for (i = 0; i < old->param_count; i++)
			sig->params [i] = old->params [i];

		for (i = 0; i < nargs; i++) {
			MonoReflectionType *rt = mono_array_get (opt_param_types, MonoReflectionType *, i);
			sig->params [old->param_count + i] = mono_reflection_type_get_handle (rt);
		}

		parent = mono_image_typedef_or_ref (assembly, &method->klass->byval_arg);
		g_assert ((parent & MONO_TYPEDEFORREF_MASK) == MONO_MEMBERREF_PARENT_TYPEREF);
		parent >>= MONO_TYPEDEFORREF_BITS;

		parent <<= MONO_MEMBERREF_PARENT_BITS;
		parent |= MONO_MEMBERREF_PARENT_TYPEREF;

		sig_token = method_encode_signature (assembly, sig);
		token = mono_image_get_varargs_method_token (assembly, parent, method->name, sig_token);
	} else if (strcmp (klass->name, "MethodBuilder") == 0) {
		MonoReflectionMethodBuilder *mb = (MonoReflectionMethodBuilder *)obj;
		ReflectionMethodBuilder rmb;
		guint32 parent, sig_token;
		int nopt_args, nparams, ngparams, i;
		char *name;

		reflection_methodbuilder_from_method_builder (&rmb, mb);
		rmb.opt_types = opt_param_types;
		nopt_args = mono_array_length (opt_param_types);

		nparams = rmb.parameters ? mono_array_length (rmb.parameters): 0;
		ngparams = rmb.generic_params ? mono_array_length (rmb.generic_params): 0;
		sig = mono_metadata_signature_alloc (&assembly->image, nparams + nopt_args);

		sig->hasthis = !(rmb.attrs & METHOD_ATTRIBUTE_STATIC);
		sig->explicit_this = (rmb.call_conv & 0x40) == 0x40;
		sig->call_convention = rmb.call_conv;
		sig->generic_param_count = ngparams;
		sig->param_count = nparams + nopt_args;
		sig->sentinelpos = nparams;
		sig->ret = mono_reflection_type_get_handle (rmb.rtype);

		for (i = 0; i < nparams; i++) {
			MonoReflectionType *rt = mono_array_get (rmb.parameters, MonoReflectionType *, i);
			sig->params [i] = mono_reflection_type_get_handle (rt);
		}

		for (i = 0; i < nopt_args; i++) {
			MonoReflectionType *rt = mono_array_get (opt_param_types, MonoReflectionType *, i);
			sig->params [nparams + i] = mono_reflection_type_get_handle (rt);
		}

		sig_token = method_builder_encode_signature (assembly, &rmb);

		parent = mono_image_create_token (assembly, obj, TRUE, TRUE);
		g_assert (mono_metadata_token_table (parent) == MONO_TABLE_METHOD);

		parent = mono_metadata_token_index (parent) << MONO_MEMBERREF_PARENT_BITS;
		parent |= MONO_MEMBERREF_PARENT_METHODDEF;

		name = mono_string_to_utf8 (rmb.name);
		token = mono_image_get_varargs_method_token (
			assembly, parent, name, sig_token);
		g_free (name);
	} else {
		g_error ("requested method token for %s\n", klass->name);
	}

	g_hash_table_insert (assembly->vararg_aux_hash, GUINT_TO_POINTER (token), sig);
	mono_g_hash_table_insert (assembly->tokens, GUINT_TO_POINTER (token), obj);
	return token;
}

/*
 * mono_image_create_token:
 * @assembly: a dynamic assembly
 * @obj:
 * @register_token: Whenever to register the token in the assembly->tokens hash. 
 *
 * Get a token to insert in the IL code stream for the given MemberInfo.
 * The metadata emission routines need to pass FALSE as REGISTER_TOKEN, since by that time, 
 * the table_idx-es were recomputed, so registering the token would overwrite an existing 
 * entry.
 */
guint32
mono_image_create_token (MonoDynamicImage *assembly, MonoObject *obj, 
						 gboolean create_open_instance, gboolean register_token)
{
	MonoClass *klass;
	guint32 token = 0;

	klass = obj->vtable->klass;

	/* Check for user defined reflection objects */
	/* TypeDelegator is the only corlib type which doesn't look like a MonoReflectionType */
	if (klass->image != mono_defaults.corlib || (strcmp (klass->name, "TypeDelegator") == 0))
		mono_raise_exception (mono_get_exception_not_supported ("User defined subclasses of System.Type are not yet supported")); \

	if (strcmp (klass->name, "MethodBuilder") == 0) {
		MonoReflectionMethodBuilder *mb = (MonoReflectionMethodBuilder *)obj;
		MonoReflectionTypeBuilder *tb = (MonoReflectionTypeBuilder*)mb->type;

		if (tb->module->dynamic_image == assembly && !tb->generic_params && !mb->generic_params)
			token = mb->table_idx | MONO_TOKEN_METHOD_DEF;
		else
			token = mono_image_get_methodbuilder_token (assembly, mb, create_open_instance);
		/*g_print ("got token 0x%08x for %s\n", token, mono_string_to_utf8 (mb->name));*/
	} else if (strcmp (klass->name, "ConstructorBuilder") == 0) {
		MonoReflectionCtorBuilder *mb = (MonoReflectionCtorBuilder *)obj;
		MonoReflectionTypeBuilder *tb = (MonoReflectionTypeBuilder*)mb->type;

		if (tb->module->dynamic_image == assembly && !tb->generic_params)
			token = mb->table_idx | MONO_TOKEN_METHOD_DEF;
		else
			token = mono_image_get_ctorbuilder_token (assembly, mb);
		/*g_print ("got token 0x%08x for %s\n", token, mono_string_to_utf8 (mb->name));*/
	} else if (strcmp (klass->name, "FieldBuilder") == 0) {
		MonoReflectionFieldBuilder *fb = (MonoReflectionFieldBuilder *)obj;
		MonoReflectionTypeBuilder *tb = (MonoReflectionTypeBuilder *)fb->typeb;
		if (tb->generic_params) {
			token = mono_image_get_generic_field_token (assembly, fb);
		} else {
			if (tb->module->dynamic_image == assembly) {
				token = fb->table_idx | MONO_TOKEN_FIELD_DEF;
			} else {
				token = mono_image_get_fieldref_token (assembly, (MonoObject*)fb, fb->handle);
			}
		}
	} else if (strcmp (klass->name, "TypeBuilder") == 0) {
		MonoReflectionTypeBuilder *tb = (MonoReflectionTypeBuilder *)obj;
		if (create_open_instance && tb->generic_params) {
			MonoType *type;
			init_type_builder_generics (obj);
			type = mono_reflection_type_get_handle ((MonoReflectionType *)obj);
			token = mono_image_typedef_or_ref_full (assembly, type, TRUE);
			token = mono_metadata_token_from_dor (token);
		} else if (tb->module->dynamic_image == assembly) {
			token = tb->table_idx | MONO_TOKEN_TYPE_DEF;
		} else {
			MonoType *type;
			type = mono_reflection_type_get_handle ((MonoReflectionType *)obj);
			token = mono_metadata_token_from_dor (mono_image_typedef_or_ref (assembly, type));
		}
	} else if (strcmp (klass->name, "MonoType") == 0) {
		MonoType *type = mono_reflection_type_get_handle ((MonoReflectionType *)obj);
		MonoClass *mc = mono_class_from_mono_type (type);
		token = mono_metadata_token_from_dor (
			mono_image_typedef_or_ref_full (assembly, type, mc->generic_container == NULL || create_open_instance));
	} else if (strcmp (klass->name, "GenericTypeParameterBuilder") == 0) {
		MonoType *type = mono_reflection_type_get_handle ((MonoReflectionType *)obj);
		token = mono_metadata_token_from_dor (
			mono_image_typedef_or_ref (assembly, type));
	} else if (strcmp (klass->name, "MonoGenericClass") == 0) {
		MonoType *type = mono_reflection_type_get_handle ((MonoReflectionType *)obj);
		token = mono_metadata_token_from_dor (
			mono_image_typedef_or_ref (assembly, type));
	} else if (strcmp (klass->name, "MonoCMethod") == 0 ||
		   strcmp (klass->name, "MonoMethod") == 0 ||
		   strcmp (klass->name, "MonoGenericMethod") == 0 ||
		   strcmp (klass->name, "MonoGenericCMethod") == 0) {
		MonoReflectionMethod *m = (MonoReflectionMethod *)obj;
		if (m->method->is_inflated) {
			if (create_open_instance)
				token = mono_image_get_methodspec_token (assembly, m->method);
			else
				token = mono_image_get_inflated_method_token (assembly, m->method);
		} else if ((m->method->klass->image == &assembly->image) &&
			 !m->method->klass->generic_class) {
			static guint32 method_table_idx = 0xffffff;
			if (m->method->klass->wastypebuilder) {
				/* we use the same token as the one that was assigned
				 * to the Methodbuilder.
				 * FIXME: do the equivalent for Fields.
				 */
				token = m->method->token;
			} else {
				/*
				 * Each token should have a unique index, but the indexes are
				 * assigned by managed code, so we don't know about them. An
				 * easy solution is to count backwards...
				 */
				method_table_idx --;
				token = MONO_TOKEN_METHOD_DEF | method_table_idx;
			}
		} else {
			token = mono_image_get_methodref_token (assembly, m->method, create_open_instance);
		}
		/*g_print ("got token 0x%08x for %s\n", token, m->method->name);*/
	} else if (strcmp (klass->name, "MonoField") == 0) {
		MonoReflectionField *f = (MonoReflectionField *)obj;
		if ((f->field->parent->image == &assembly->image) && !is_field_on_inst (f->field)) {
			static guint32 field_table_idx = 0xffffff;
			field_table_idx --;
			token = MONO_TOKEN_FIELD_DEF | field_table_idx;
		} else {
			token = mono_image_get_fieldref_token (assembly, (MonoObject*)f, f->field);
		}
		/*g_print ("got token 0x%08x for %s\n", token, f->field->name);*/
	} else if (strcmp (klass->name, "MonoArrayMethod") == 0) {
		MonoReflectionArrayMethod *m = (MonoReflectionArrayMethod *)obj;
		token = mono_image_get_array_token (assembly, m);
	} else if (strcmp (klass->name, "SignatureHelper") == 0) {
		MonoReflectionSigHelper *s = (MonoReflectionSigHelper*)obj;
		token = MONO_TOKEN_SIGNATURE | mono_image_get_sighelper_token (assembly, s);
	} else if (strcmp (klass->name, "EnumBuilder") == 0) {
		MonoType *type = mono_reflection_type_get_handle ((MonoReflectionType *)obj);
		token = mono_metadata_token_from_dor (
			mono_image_typedef_or_ref (assembly, type));
	} else if (strcmp (klass->name, "FieldOnTypeBuilderInst") == 0) {
		MonoReflectionFieldOnTypeBuilderInst *f = (MonoReflectionFieldOnTypeBuilderInst*)obj;
		token = mono_image_get_field_on_inst_token (assembly, f);
	} else if (strcmp (klass->name, "ConstructorOnTypeBuilderInst") == 0) {
		MonoReflectionCtorOnTypeBuilderInst *c = (MonoReflectionCtorOnTypeBuilderInst*)obj;
		token = mono_image_get_ctor_on_inst_token (assembly, c, create_open_instance);
	} else if (strcmp (klass->name, "MethodOnTypeBuilderInst") == 0) {
		MonoReflectionMethodOnTypeBuilderInst *m = (MonoReflectionMethodOnTypeBuilderInst*)obj;
		token = mono_image_get_method_on_inst_token (assembly, m, create_open_instance);
	} else if (is_sre_array (klass) || is_sre_byref (klass) || is_sre_pointer (klass)) {
		MonoReflectionType *type = (MonoReflectionType *)obj;
		token = mono_metadata_token_from_dor (
				mono_image_typedef_or_ref (assembly, mono_reflection_type_get_handle (type)));
	} else {
		g_error ("requested token for %s\n", klass->name);
	}

	if (register_token)
		mono_image_register_token (assembly, token, obj);

	return token;
}

/*
 * mono_image_register_token:
 *
 *   Register the TOKEN->OBJ mapping in the mapping table in ASSEMBLY. This is required for
 * the Module.ResolveXXXToken () methods to work.
 */
void
mono_image_register_token (MonoDynamicImage *assembly, guint32 token, MonoObject *obj)
{
	MonoObject *prev = mono_g_hash_table_lookup (assembly->tokens, GUINT_TO_POINTER (token));
	if (prev) {
		/* There could be multiple MethodInfo objects with the same token */
		//g_assert (prev == obj);
	} else {
		mono_g_hash_table_insert (assembly->tokens, GUINT_TO_POINTER (token), obj);
	}
}

static MonoDynamicImage*
create_dynamic_mono_image (MonoDynamicAssembly *assembly, char *assembly_name, char *module_name)
{
	static const guchar entrycode [16] = {0xff, 0x25, 0};
	MonoDynamicImage *image;
	int i;

	const char *version;

	if (!strcmp (mono_get_runtime_info ()->framework_version, "2.1"))
		version = "v2.0.50727"; /* HACK: SL 2 enforces the .net 2 metadata version */
	else
		version = mono_get_runtime_info ()->runtime_version;

#if HAVE_BOEHM_GC
	/* The MonoGHashTable's need GC tracking */
	image = GC_MALLOC (sizeof (MonoDynamicImage));
#else
	image = g_new0 (MonoDynamicImage, 1);
#endif
	
	mono_profiler_module_event (&image->image, MONO_PROFILE_START_LOAD);
	
	/*g_print ("created image %p\n", image);*/
	/* keep in sync with image.c */
	image->image.name = assembly_name;
	image->image.assembly_name = image->image.name; /* they may be different */
	image->image.module_name = module_name;
	image->image.version = g_strdup (version);
	image->image.md_version_major = 1;
	image->image.md_version_minor = 1;
	image->image.dynamic = TRUE;

	image->image.references = g_new0 (MonoAssembly*, 1);
	image->image.references [0] = NULL;

	mono_image_init (&image->image);

	image->token_fixups = mono_g_hash_table_new_type ((GHashFunc)mono_object_hash, NULL, MONO_HASH_KEY_GC);
	image->method_to_table_idx = g_hash_table_new (NULL, NULL);
	image->field_to_table_idx = g_hash_table_new (NULL, NULL);
	image->method_aux_hash = g_hash_table_new (NULL, NULL);
	image->vararg_aux_hash = g_hash_table_new (NULL, NULL);
	image->handleref = g_hash_table_new (NULL, NULL);
	image->handleref_managed = mono_g_hash_table_new_type ((GHashFunc)mono_object_hash, NULL, MONO_HASH_KEY_GC);
	image->tokens = mono_g_hash_table_new_type (NULL, NULL, MONO_HASH_VALUE_GC);
	image->generic_def_objects = mono_g_hash_table_new_type (NULL, NULL, MONO_HASH_VALUE_GC);
	image->methodspec = mono_g_hash_table_new_type ((GHashFunc)mono_object_hash, NULL, MONO_HASH_KEY_GC);
	image->typespec = g_hash_table_new ((GHashFunc)mono_metadata_type_hash, (GCompareFunc)mono_metadata_type_equal);
	image->typeref = g_hash_table_new ((GHashFunc)mono_metadata_type_hash, (GCompareFunc)mono_metadata_type_equal);
	image->blob_cache = g_hash_table_new ((GHashFunc)mono_blob_entry_hash, (GCompareFunc)mono_blob_entry_equal);
	image->gen_params = g_ptr_array_new ();
	image->remapped_tokens = mono_g_hash_table_new_type (NULL, NULL, MONO_HASH_VALUE_GC);

	/*g_print ("string heap create for image %p (%s)\n", image, module_name);*/
	string_heap_init (&image->sheap);
	mono_image_add_stream_data (&image->us, "", 1);
	add_to_blob_cached (image, (char*) "", 1, NULL, 0);
	/* import tables... */
	mono_image_add_stream_data (&image->code, (char*)entrycode, sizeof (entrycode));
	image->iat_offset = mono_image_add_stream_zero (&image->code, 8); /* two IAT entries */
	image->idt_offset = mono_image_add_stream_zero (&image->code, 2 * sizeof (MonoIDT)); /* two IDT entries */
	image->imp_names_offset = mono_image_add_stream_zero (&image->code, 2); /* flags for name entry */
	mono_image_add_stream_data (&image->code, "_CorExeMain", 12);
	mono_image_add_stream_data (&image->code, "mscoree.dll", 12);
	image->ilt_offset = mono_image_add_stream_zero (&image->code, 8); /* two ILT entries */
	stream_data_align (&image->code);

	image->cli_header_offset = mono_image_add_stream_zero (&image->code, sizeof (MonoCLIHeader));

	for (i=0; i < MONO_TABLE_NUM; ++i) {
		image->tables [i].next_idx = 1;
		image->tables [i].columns = table_sizes [i];
	}

	image->image.assembly = (MonoAssembly*)assembly;
	image->run = assembly->run;
	image->save = assembly->save;
	image->pe_kind = 0x1; /* ILOnly */
	image->machine = 0x14c; /* I386 */
	
	mono_profiler_module_loaded (&image->image, MONO_PROFILE_OK);

	return image;
}
#endif

static void
free_blob_cache_entry (gpointer key, gpointer val, gpointer user_data)
{
	g_free (key);
}

static void
release_hashtable (MonoGHashTable **hash)
{
	if (*hash) {
		mono_g_hash_table_destroy (*hash);
		*hash = NULL;
	}
}

void
mono_dynamic_image_release_gc_roots (MonoDynamicImage *image)
{
	release_hashtable (&image->token_fixups);
	release_hashtable (&image->handleref_managed);
	release_hashtable (&image->tokens);
	release_hashtable (&image->remapped_tokens);
	release_hashtable (&image->generic_def_objects);
	release_hashtable (&image->methodspec);
}

void
mono_dynamic_image_free (MonoDynamicImage *image)
{
	MonoDynamicImage *di = image;
	GList *list;
	int i;

	if (di->methodspec)
		mono_g_hash_table_destroy (di->methodspec);
	if (di->typespec)
		g_hash_table_destroy (di->typespec);
	if (di->typeref)
		g_hash_table_destroy (di->typeref);
	if (di->handleref)
		g_hash_table_destroy (di->handleref);
	if (di->handleref_managed)
		mono_g_hash_table_destroy (di->handleref_managed);
	if (di->tokens)
		mono_g_hash_table_destroy (di->tokens);
	if (di->remapped_tokens)
		mono_g_hash_table_destroy (di->remapped_tokens);
	if (di->generic_def_objects)
		mono_g_hash_table_destroy (di->generic_def_objects);
	if (di->blob_cache) {
		g_hash_table_foreach (di->blob_cache, free_blob_cache_entry, NULL);
		g_hash_table_destroy (di->blob_cache);
	}
	if (di->standalonesig_cache)
		g_hash_table_destroy (di->standalonesig_cache);
	for (list = di->array_methods; list; list = list->next) {
		ArrayMethod *am = (ArrayMethod *)list->data;
		g_free (am->sig);
		g_free (am->name);
		g_free (am);
	}
	g_list_free (di->array_methods);
	if (di->gen_params) {
		for (i = 0; i < di->gen_params->len; i++) {
			GenericParamTableEntry *entry = g_ptr_array_index (di->gen_params, i);
			mono_gc_deregister_root ((char*) &entry->gparam);
			g_free (entry);
		}
	 	g_ptr_array_free (di->gen_params, TRUE);
	}
	if (di->token_fixups)
		mono_g_hash_table_destroy (di->token_fixups);
	if (di->method_to_table_idx)
		g_hash_table_destroy (di->method_to_table_idx);
	if (di->field_to_table_idx)
		g_hash_table_destroy (di->field_to_table_idx);
	if (di->method_aux_hash)
		g_hash_table_destroy (di->method_aux_hash);
	if (di->vararg_aux_hash)
		g_hash_table_destroy (di->vararg_aux_hash);
	g_free (di->strong_name);
	g_free (di->win32_res);
	if (di->public_key)
		g_free (di->public_key);

	/*g_print ("string heap destroy for image %p\n", di);*/
	mono_dynamic_stream_reset (&di->sheap);
	mono_dynamic_stream_reset (&di->code);
	mono_dynamic_stream_reset (&di->resources);
	mono_dynamic_stream_reset (&di->us);
	mono_dynamic_stream_reset (&di->blob);
	mono_dynamic_stream_reset (&di->tstream);
	mono_dynamic_stream_reset (&di->guid);
	for (i = 0; i < MONO_TABLE_NUM; ++i) {
		g_free (di->tables [i].values);
	}
}	

#ifndef DISABLE_REFLECTION_EMIT

/*
 * mono_image_basic_init:
 * @assembly: an assembly builder object
 *
 * Create the MonoImage that represents the assembly builder and setup some
 * of the helper hash table and the basic metadata streams.
 */
void
mono_image_basic_init (MonoReflectionAssemblyBuilder *assemblyb)
{
	MonoDynamicAssembly *assembly;
	MonoDynamicImage *image;
	MonoDomain *domain = mono_object_domain (assemblyb);
	
	MONO_ARCH_SAVE_REGS;

	if (assemblyb->dynamic_assembly)
		return;

#if HAVE_BOEHM_GC
	/* assembly->assembly.image might be GC allocated */
	assembly = assemblyb->dynamic_assembly = GC_MALLOC (sizeof (MonoDynamicAssembly));
#else
	assembly = assemblyb->dynamic_assembly = g_new0 (MonoDynamicAssembly, 1);
#endif

	mono_profiler_assembly_event (&assembly->assembly, MONO_PROFILE_START_LOAD);
	
	assembly->assembly.ref_count = 1;
	assembly->assembly.dynamic = TRUE;
	assembly->assembly.corlib_internal = assemblyb->corlib_internal;
	assemblyb->assembly.assembly = (MonoAssembly*)assembly;
	assembly->assembly.basedir = mono_string_to_utf8 (assemblyb->dir);
	if (assemblyb->culture)
		assembly->assembly.aname.culture = mono_string_to_utf8 (assemblyb->culture);
	else
		assembly->assembly.aname.culture = g_strdup ("");

        if (assemblyb->version) {
			char *vstr = mono_string_to_utf8 (assemblyb->version);
			char **version = g_strsplit (vstr, ".", 4);
			char **parts = version;
			assembly->assembly.aname.major = atoi (*parts++);
			assembly->assembly.aname.minor = atoi (*parts++);
			assembly->assembly.aname.build = *parts != NULL ? atoi (*parts++) : 0;
			assembly->assembly.aname.revision = *parts != NULL ? atoi (*parts) : 0;

			g_strfreev (version);
			g_free (vstr);
        } else {
			assembly->assembly.aname.major = 0;
			assembly->assembly.aname.minor = 0;
			assembly->assembly.aname.build = 0;
			assembly->assembly.aname.revision = 0;
        }

	assembly->run = assemblyb->access != 2;
	assembly->save = assemblyb->access != 1;
	assembly->domain = domain;

	image = create_dynamic_mono_image (assembly, mono_string_to_utf8 (assemblyb->name), g_strdup ("RefEmit_YouForgotToDefineAModule"));
	image->initial_image = TRUE;
	assembly->assembly.aname.name = image->image.name;
	assembly->assembly.image = &image->image;
	if (assemblyb->pktoken && assemblyb->pktoken->max_length) {
		/* -1 to correct for the trailing NULL byte */
		if (assemblyb->pktoken->max_length != MONO_PUBLIC_KEY_TOKEN_LENGTH - 1) {
			g_error ("Public key token length invalid for assembly %s: %i", assembly->assembly.aname.name, assemblyb->pktoken->max_length);
		}
		memcpy (&assembly->assembly.aname.public_key_token, mono_array_addr (assemblyb->pktoken, guint8, 0), assemblyb->pktoken->max_length);		
	}

	mono_domain_assemblies_lock (domain);
	domain->domain_assemblies = g_slist_append (domain->domain_assemblies, assembly);
	mono_domain_assemblies_unlock (domain);

	register_assembly (mono_object_domain (assemblyb), &assemblyb->assembly, &assembly->assembly);
	
	mono_profiler_assembly_loaded (&assembly->assembly, MONO_PROFILE_OK);
	
	mono_assembly_invoke_load_hook ((MonoAssembly*)assembly);
}

#endif /* !DISABLE_REFLECTION_EMIT */

#ifndef DISABLE_REFLECTION_EMIT_SAVE

static int
calc_section_size (MonoDynamicImage *assembly)
{
	int nsections = 0;

	/* alignment constraints */
	mono_image_add_stream_zero (&assembly->code, 4 - (assembly->code.index % 4));
	g_assert ((assembly->code.index % 4) == 0);
	assembly->meta_size += 3;
	assembly->meta_size &= ~3;
	mono_image_add_stream_zero (&assembly->resources, 4 - (assembly->resources.index % 4));
	g_assert ((assembly->resources.index % 4) == 0);

	assembly->sections [MONO_SECTION_TEXT].size = assembly->meta_size + assembly->code.index + assembly->resources.index + assembly->strong_name_size;
	assembly->sections [MONO_SECTION_TEXT].attrs = SECT_FLAGS_HAS_CODE | SECT_FLAGS_MEM_EXECUTE | SECT_FLAGS_MEM_READ;
	nsections++;

	if (assembly->win32_res) {
		guint32 res_size = (assembly->win32_res_size + 3) & ~3;

		assembly->sections [MONO_SECTION_RSRC].size = res_size;
		assembly->sections [MONO_SECTION_RSRC].attrs = SECT_FLAGS_HAS_INITIALIZED_DATA | SECT_FLAGS_MEM_READ;
		nsections++;
	}

	assembly->sections [MONO_SECTION_RELOC].size = 12;
	assembly->sections [MONO_SECTION_RELOC].attrs = SECT_FLAGS_MEM_READ | SECT_FLAGS_MEM_DISCARDABLE | SECT_FLAGS_HAS_INITIALIZED_DATA;
	nsections++;

	return nsections;
}

typedef struct {
	guint32 id;
	guint32 offset;
	GSList *children;
	MonoReflectionWin32Resource *win32_res; /* Only for leaf nodes */
} ResTreeNode;

static int
resource_tree_compare_by_id (gconstpointer a, gconstpointer b)
{
	ResTreeNode *t1 = (ResTreeNode*)a;
	ResTreeNode *t2 = (ResTreeNode*)b;

	return t1->id - t2->id;
}

/*
 * resource_tree_create:
 *
 *  Organize the resources into a resource tree.
 */
static ResTreeNode *
resource_tree_create (MonoArray *win32_resources)
{
	ResTreeNode *tree, *res_node, *type_node, *lang_node;
	GSList *l;
	int i;

	tree = g_new0 (ResTreeNode, 1);
	
	for (i = 0; i < mono_array_length (win32_resources); ++i) {
		MonoReflectionWin32Resource *win32_res =
			(MonoReflectionWin32Resource*)mono_array_addr (win32_resources, MonoReflectionWin32Resource, i);

		/* Create node */

		/* FIXME: BUG: this stores managed references in unmanaged memory */
		lang_node = g_new0 (ResTreeNode, 1);
		lang_node->id = win32_res->lang_id;
		lang_node->win32_res = win32_res;

		/* Create type node if neccesary */
		type_node = NULL;
		for (l = tree->children; l; l = l->next)
			if (((ResTreeNode*)(l->data))->id == win32_res->res_type) {
				type_node = (ResTreeNode*)l->data;
				break;
			}

		if (!type_node) {
			type_node = g_new0 (ResTreeNode, 1);
			type_node->id = win32_res->res_type;

			/* 
			 * The resource types have to be sorted otherwise
			 * Windows Explorer can't display the version information.
			 */
			tree->children = g_slist_insert_sorted (tree->children, 
				type_node, resource_tree_compare_by_id);
		}

		/* Create res node if neccesary */
		res_node = NULL;
		for (l = type_node->children; l; l = l->next)
			if (((ResTreeNode*)(l->data))->id == win32_res->res_id) {
				res_node = (ResTreeNode*)l->data;
				break;
			}

		if (!res_node) {
			res_node = g_new0 (ResTreeNode, 1);
			res_node->id = win32_res->res_id;
			type_node->children = g_slist_append (type_node->children, res_node);
		}

		res_node->children = g_slist_append (res_node->children, lang_node);
	}

	return tree;
}

/*
 * resource_tree_encode:
 * 
 *   Encode the resource tree into the format used in the PE file.
 */
static void
resource_tree_encode (ResTreeNode *node, char *begin, char *p, char **endbuf)
{
	char *entries;
	MonoPEResourceDir dir;
	MonoPEResourceDirEntry dir_entry;
	MonoPEResourceDataEntry data_entry;
	GSList *l;
	guint32 res_id_entries;

	/*
	 * For the format of the resource directory, see the article
	 * "An In-Depth Look into the Win32 Portable Executable File Format" by
	 * Matt Pietrek
	 */

	memset (&dir, 0, sizeof (dir));
	memset (&dir_entry, 0, sizeof (dir_entry));
	memset (&data_entry, 0, sizeof (data_entry));

	g_assert (sizeof (dir) == 16);
	g_assert (sizeof (dir_entry) == 8);
	g_assert (sizeof (data_entry) == 16);

	node->offset = p - begin;

	/* IMAGE_RESOURCE_DIRECTORY */
	res_id_entries = g_slist_length (node->children);
	dir.res_id_entries = GUINT16_TO_LE (res_id_entries);

	memcpy (p, &dir, sizeof (dir));
	p += sizeof (dir);

	/* Reserve space for entries */
	entries = p;
	p += sizeof (dir_entry) * res_id_entries;

	/* Write children */
	for (l = node->children; l; l = l->next) {
		ResTreeNode *child = (ResTreeNode*)l->data;

		if (child->win32_res) {
			guint32 size;

			child->offset = p - begin;

			/* IMAGE_RESOURCE_DATA_ENTRY */
			data_entry.rde_data_offset = GUINT32_TO_LE (p - begin + sizeof (data_entry));
			size = mono_array_length (child->win32_res->res_data);
			data_entry.rde_size = GUINT32_TO_LE (size);

			memcpy (p, &data_entry, sizeof (data_entry));
			p += sizeof (data_entry);

			memcpy (p, mono_array_addr (child->win32_res->res_data, char, 0), size);
			p += size;
		} else {
			resource_tree_encode (child, begin, p, &p);
		}
	}

	/* IMAGE_RESOURCE_ENTRY */
	for (l = node->children; l; l = l->next) {
		ResTreeNode *child = (ResTreeNode*)l->data;

		MONO_PE_RES_DIR_ENTRY_SET_NAME (dir_entry, FALSE, child->id);
		MONO_PE_RES_DIR_ENTRY_SET_DIR (dir_entry, !child->win32_res, child->offset);

		memcpy (entries, &dir_entry, sizeof (dir_entry));
		entries += sizeof (dir_entry);
	}

	*endbuf = p;
}

static void
resource_tree_free (ResTreeNode * node)
{
	GSList * list;
	for (list = node->children; list; list = list->next)
		resource_tree_free ((ResTreeNode*)list->data);
	g_slist_free(node->children);
	g_free (node);
}

static void
assembly_add_win32_resources (MonoDynamicImage *assembly, MonoReflectionAssemblyBuilder *assemblyb)
{
	char *buf;
	char *p;
	guint32 size, i;
	MonoReflectionWin32Resource *win32_res;
	ResTreeNode *tree;

	if (!assemblyb->win32_resources)
		return;

	/*
	 * Resources are stored in a three level tree inside the PE file.
	 * - level one contains a node for each type of resource
	 * - level two contains a node for each resource
	 * - level three contains a node for each instance of a resource for a
	 *   specific language.
	 */

	tree = resource_tree_create (assemblyb->win32_resources);

	/* Estimate the size of the encoded tree */
	size = 0;
	for (i = 0; i < mono_array_length (assemblyb->win32_resources); ++i) {
		win32_res = (MonoReflectionWin32Resource*)mono_array_addr (assemblyb->win32_resources, MonoReflectionWin32Resource, i);
		size += mono_array_length (win32_res->res_data);
	}
	/* Directory structure */
	size += mono_array_length (assemblyb->win32_resources) * 256;
	p = buf = g_malloc (size);

	resource_tree_encode (tree, p, p, &p);

	g_assert (p - buf <= size);

	assembly->win32_res = g_malloc (p - buf);
	assembly->win32_res_size = p - buf;
	memcpy (assembly->win32_res, buf, p - buf);

	g_free (buf);
	resource_tree_free (tree);
}

static void
fixup_resource_directory (char *res_section, char *p, guint32 rva)
{
	MonoPEResourceDir *dir = (MonoPEResourceDir*)p;
	int i;

	p += sizeof (MonoPEResourceDir);
	for (i = 0; i < GUINT16_FROM_LE (dir->res_named_entries) + GUINT16_FROM_LE (dir->res_id_entries); ++i) {
		MonoPEResourceDirEntry *dir_entry = (MonoPEResourceDirEntry*)p;
		char *child = res_section + MONO_PE_RES_DIR_ENTRY_DIR_OFFSET (*dir_entry);
		if (MONO_PE_RES_DIR_ENTRY_IS_DIR (*dir_entry)) {
			fixup_resource_directory (res_section, child, rva);
		} else {
			MonoPEResourceDataEntry *data_entry = (MonoPEResourceDataEntry*)child;
			data_entry->rde_data_offset = GUINT32_TO_LE (GUINT32_FROM_LE (data_entry->rde_data_offset) + rva);
		}

		p += sizeof (MonoPEResourceDirEntry);
	}
}

static void
checked_write_file (HANDLE f, gconstpointer buffer, guint32 numbytes)
{
	guint32 dummy;
	if (!WriteFile (f, buffer, numbytes, &dummy, NULL))
		g_error ("WriteFile returned %d\n", GetLastError ());
}

/*
 * mono_image_create_pefile:
 * @mb: a module builder object
 * 
 * This function creates the PE-COFF header, the image sections, the CLI header  * etc. all the data is written in
 * assembly->pefile where it can be easily retrieved later in chunks.
 */
void
mono_image_create_pefile (MonoReflectionModuleBuilder *mb, HANDLE file)
{
	MonoMSDOSHeader *msdos;
	MonoDotNetHeader *header;
	MonoSectionTable *section;
	MonoCLIHeader *cli_header;
	guint32 size, image_size, virtual_base, text_offset;
	guint32 header_start, section_start, file_offset, virtual_offset;
	MonoDynamicImage *assembly;
	MonoReflectionAssemblyBuilder *assemblyb;
	MonoDynamicStream pefile_stream = {0};
	MonoDynamicStream *pefile = &pefile_stream;
	int i, nsections;
	guint32 *rva, value;
	guchar *p;
	static const unsigned char msheader[] = {
		0x4d, 0x5a, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00,  0x04, 0x00, 0x00, 0x00, 0xff, 0xff, 0x00, 0x00,
		0xb8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,  0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,  0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00,
		0x0e, 0x1f, 0xba, 0x0e, 0x00, 0xb4, 0x09, 0xcd,  0x21, 0xb8, 0x01, 0x4c, 0xcd, 0x21, 0x54, 0x68,
		0x69, 0x73, 0x20, 0x70, 0x72, 0x6f, 0x67, 0x72,  0x61, 0x6d, 0x20, 0x63, 0x61, 0x6e, 0x6e, 0x6f,
		0x74, 0x20, 0x62, 0x65, 0x20, 0x72, 0x75, 0x6e,  0x20, 0x69, 0x6e, 0x20, 0x44, 0x4f, 0x53, 0x20,
		0x6d, 0x6f, 0x64, 0x65, 0x2e, 0x0d, 0x0d, 0x0a,  0x24, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
	};

	assemblyb = mb->assemblyb;

	mono_image_basic_init (assemblyb);
	assembly = mb->dynamic_image;

	assembly->pe_kind = assemblyb->pe_kind;
	assembly->machine = assemblyb->machine;
	((MonoDynamicImage*)assemblyb->dynamic_assembly->assembly.image)->pe_kind = assemblyb->pe_kind;
	((MonoDynamicImage*)assemblyb->dynamic_assembly->assembly.image)->machine = assemblyb->machine;
	
	mono_image_build_metadata (mb);

	if (mb->is_main && assemblyb->resources) {
		int len = mono_array_length (assemblyb->resources);
		for (i = 0; i < len; ++i)
			assembly_add_resource (mb, assembly, (MonoReflectionResource*)mono_array_addr (assemblyb->resources, MonoReflectionResource, i));
	}

	if (mb->resources) {
		int len = mono_array_length (mb->resources);
		for (i = 0; i < len; ++i)
			assembly_add_resource (mb, assembly, (MonoReflectionResource*)mono_array_addr (mb->resources, MonoReflectionResource, i));
	}

	build_compressed_metadata (assembly);

	if (mb->is_main)
		assembly_add_win32_resources (assembly, assemblyb);

	nsections = calc_section_size (assembly);
	
	/* The DOS header and stub */
	g_assert (sizeof (MonoMSDOSHeader) == sizeof (msheader));
	mono_image_add_stream_data (pefile, (char*)msheader, sizeof (msheader));

	/* the dotnet header */
	header_start = mono_image_add_stream_zero (pefile, sizeof (MonoDotNetHeader));

	/* the section tables */
	section_start = mono_image_add_stream_zero (pefile, sizeof (MonoSectionTable) * nsections);

	file_offset = section_start + sizeof (MonoSectionTable) * nsections;
	virtual_offset = VIRT_ALIGN;
	image_size = 0;

	for (i = 0; i < MONO_SECTION_MAX; ++i) {
		if (!assembly->sections [i].size)
			continue;
		/* align offsets */
		file_offset += FILE_ALIGN - 1;
		file_offset &= ~(FILE_ALIGN - 1);
		virtual_offset += VIRT_ALIGN - 1;
		virtual_offset &= ~(VIRT_ALIGN - 1);

		assembly->sections [i].offset = file_offset;
		assembly->sections [i].rva = virtual_offset;

		file_offset += assembly->sections [i].size;
		virtual_offset += assembly->sections [i].size;
		image_size += (assembly->sections [i].size + VIRT_ALIGN - 1) & ~(VIRT_ALIGN - 1);
	}

	file_offset += FILE_ALIGN - 1;
	file_offset &= ~(FILE_ALIGN - 1);

	image_size += section_start + sizeof (MonoSectionTable) * nsections;

	/* back-patch info */
	msdos = (MonoMSDOSHeader*)pefile->data;
	msdos->pe_offset = GUINT32_FROM_LE (sizeof (MonoMSDOSHeader));

	header = (MonoDotNetHeader*)(pefile->data + header_start);
	header->pesig [0] = 'P';
	header->pesig [1] = 'E';
	
	header->coff.coff_machine = GUINT16_FROM_LE (assemblyb->machine);
	header->coff.coff_sections = GUINT16_FROM_LE (nsections);
	header->coff.coff_time = GUINT32_FROM_LE (time (NULL));
	header->coff.coff_opt_header_size = GUINT16_FROM_LE (sizeof (MonoDotNetHeader) - sizeof (MonoCOFFHeader) - 4);
	if (assemblyb->pekind == 1) {
		/* it's a dll */
		header->coff.coff_attributes = GUINT16_FROM_LE (0x210e);
	} else {
		/* it's an exe */
		header->coff.coff_attributes = GUINT16_FROM_LE (0x010e);
	}

	virtual_base = 0x400000; /* FIXME: 0x10000000 if a DLL */

	header->pe.pe_magic = GUINT16_FROM_LE (0x10B);
	header->pe.pe_major = 6;
	header->pe.pe_minor = 0;
	size = assembly->sections [MONO_SECTION_TEXT].size;
	size += FILE_ALIGN - 1;
	size &= ~(FILE_ALIGN - 1);
	header->pe.pe_code_size = GUINT32_FROM_LE(size);
	size = assembly->sections [MONO_SECTION_RSRC].size;
	size += FILE_ALIGN - 1;
	size &= ~(FILE_ALIGN - 1);
	header->pe.pe_data_size = GUINT32_FROM_LE(size);
	g_assert (START_TEXT_RVA == assembly->sections [MONO_SECTION_TEXT].rva);
	header->pe.pe_rva_code_base = GUINT32_FROM_LE (assembly->sections [MONO_SECTION_TEXT].rva);
	header->pe.pe_rva_data_base = GUINT32_FROM_LE (assembly->sections [MONO_SECTION_RSRC].rva);
	/* pe_rva_entry_point always at the beginning of the text section */
	header->pe.pe_rva_entry_point = GUINT32_FROM_LE (assembly->sections [MONO_SECTION_TEXT].rva);

	header->nt.pe_image_base = GUINT32_FROM_LE (virtual_base);
	header->nt.pe_section_align = GUINT32_FROM_LE (VIRT_ALIGN);
	header->nt.pe_file_alignment = GUINT32_FROM_LE (FILE_ALIGN);
	header->nt.pe_os_major = GUINT16_FROM_LE (4);
	header->nt.pe_os_minor = GUINT16_FROM_LE (0);
	header->nt.pe_subsys_major = GUINT16_FROM_LE (4);
	size = section_start;
	size += FILE_ALIGN - 1;
	size &= ~(FILE_ALIGN - 1);
	header->nt.pe_header_size = GUINT32_FROM_LE (size);
	size = image_size;
	size += VIRT_ALIGN - 1;
	size &= ~(VIRT_ALIGN - 1);
	header->nt.pe_image_size = GUINT32_FROM_LE (size);

	/*
	// Translate the PEFileKind value to the value expected by the Windows loader
	*/
	{
		short kind;

		/*
		// PEFileKinds.Dll == 1
		// PEFileKinds.ConsoleApplication == 2
		// PEFileKinds.WindowApplication == 3
		//
		// need to get:
		//     IMAGE_SUBSYSTEM_WINDOWS_GUI 2 // Image runs in the Windows GUI subsystem.
                //     IMAGE_SUBSYSTEM_WINDOWS_CUI 3 // Image runs in the Windows character subsystem.
		*/
		if (assemblyb->pekind == 3)
			kind = 2;
		else
			kind = 3;
		
		header->nt.pe_subsys_required = GUINT16_FROM_LE (kind);
	}    
	header->nt.pe_stack_reserve = GUINT32_FROM_LE (0x00100000);
	header->nt.pe_stack_commit = GUINT32_FROM_LE (0x00001000);
	header->nt.pe_heap_reserve = GUINT32_FROM_LE (0x00100000);
	header->nt.pe_heap_commit = GUINT32_FROM_LE (0x00001000);
	header->nt.pe_loader_flags = GUINT32_FROM_LE (0);
	header->nt.pe_data_dir_count = GUINT32_FROM_LE (16);

	/* fill data directory entries */

	header->datadir.pe_resource_table.size = GUINT32_FROM_LE (assembly->sections [MONO_SECTION_RSRC].size);
	header->datadir.pe_resource_table.rva = GUINT32_FROM_LE (assembly->sections [MONO_SECTION_RSRC].rva);

	header->datadir.pe_reloc_table.size = GUINT32_FROM_LE (assembly->sections [MONO_SECTION_RELOC].size);
	header->datadir.pe_reloc_table.rva = GUINT32_FROM_LE (assembly->sections [MONO_SECTION_RELOC].rva);

	header->datadir.pe_cli_header.size = GUINT32_FROM_LE (72);
	header->datadir.pe_cli_header.rva = GUINT32_FROM_LE (assembly->text_rva + assembly->cli_header_offset);
	header->datadir.pe_iat.size = GUINT32_FROM_LE (8);
	header->datadir.pe_iat.rva = GUINT32_FROM_LE (assembly->text_rva + assembly->iat_offset);
	/* patch entrypoint name */
	if (assemblyb->pekind == 1)
		memcpy (assembly->code.data + assembly->imp_names_offset + 2, "_CorDllMain", 12);
	else
		memcpy (assembly->code.data + assembly->imp_names_offset + 2, "_CorExeMain", 12);
	/* patch imported function RVA name */
	rva = (guint32*)(assembly->code.data + assembly->iat_offset);
	*rva = GUINT32_FROM_LE (assembly->text_rva + assembly->imp_names_offset);

	/* the import table */
	header->datadir.pe_import_table.size = GUINT32_FROM_LE (79); /* FIXME: magic number? */
	header->datadir.pe_import_table.rva = GUINT32_FROM_LE (assembly->text_rva + assembly->idt_offset);
	/* patch imported dll RVA name and other entries in the dir */
	rva = (guint32*)(assembly->code.data + assembly->idt_offset + G_STRUCT_OFFSET (MonoIDT, name_rva));
	*rva = GUINT32_FROM_LE (assembly->text_rva + assembly->imp_names_offset + 14); /* 14 is hint+strlen+1 of func name */
	rva = (guint32*)(assembly->code.data + assembly->idt_offset + G_STRUCT_OFFSET (MonoIDT, import_address_table_rva));
	*rva = GUINT32_FROM_LE (assembly->text_rva + assembly->iat_offset);
	rva = (guint32*)(assembly->code.data + assembly->idt_offset + G_STRUCT_OFFSET (MonoIDT, import_lookup_table));
	*rva = GUINT32_FROM_LE (assembly->text_rva + assembly->ilt_offset);

	p = (guchar*)(assembly->code.data + assembly->ilt_offset);
	value = (assembly->text_rva + assembly->imp_names_offset);
	*p++ = (value) & 0xff;
	*p++ = (value >> 8) & (0xff);
	*p++ = (value >> 16) & (0xff);
	*p++ = (value >> 24) & (0xff);

	/* the CLI header info */
	cli_header = (MonoCLIHeader*)(assembly->code.data + assembly->cli_header_offset);
	cli_header->ch_size = GUINT32_FROM_LE (72);
	cli_header->ch_runtime_major = GUINT16_FROM_LE (2);
	cli_header->ch_runtime_minor = GUINT16_FROM_LE (5);
	cli_header->ch_flags = GUINT32_FROM_LE (assemblyb->pe_kind);
	if (assemblyb->entry_point) {
		guint32 table_idx = 0;
		if (!strcmp (assemblyb->entry_point->object.vtable->klass->name, "MethodBuilder")) {
			MonoReflectionMethodBuilder *methodb = (MonoReflectionMethodBuilder*)assemblyb->entry_point;
			table_idx = methodb->table_idx;
		} else {
			table_idx = GPOINTER_TO_UINT (g_hash_table_lookup (assembly->method_to_table_idx, assemblyb->entry_point->method));
		}
		cli_header->ch_entry_point = GUINT32_FROM_LE (table_idx | MONO_TOKEN_METHOD_DEF);
	} else {
		cli_header->ch_entry_point = GUINT32_FROM_LE (0);
	}
	/* The embedded managed resources */
	text_offset = assembly->text_rva + assembly->code.index;
	cli_header->ch_resources.rva = GUINT32_FROM_LE (text_offset);
	cli_header->ch_resources.size = GUINT32_FROM_LE (assembly->resources.index);
	text_offset += assembly->resources.index;
	cli_header->ch_metadata.rva = GUINT32_FROM_LE (text_offset);
	cli_header->ch_metadata.size = GUINT32_FROM_LE (assembly->meta_size);
	text_offset += assembly->meta_size;
	if (assembly->strong_name_size) {
		cli_header->ch_strong_name.rva = GUINT32_FROM_LE (text_offset);
		cli_header->ch_strong_name.size = GUINT32_FROM_LE (assembly->strong_name_size);
		text_offset += assembly->strong_name_size;
	}

	/* write the section tables and section content */
	section = (MonoSectionTable*)(pefile->data + section_start);
	for (i = 0; i < MONO_SECTION_MAX; ++i) {
		static const char section_names [][7] = {
			".text", ".rsrc", ".reloc"
		};
		if (!assembly->sections [i].size)
			continue;
		strcpy (section->st_name, section_names [i]);
		/*g_print ("output section %s (%d), size: %d\n", section->st_name, i, assembly->sections [i].size);*/
		section->st_virtual_address = GUINT32_FROM_LE (assembly->sections [i].rva);
		section->st_virtual_size = GUINT32_FROM_LE (assembly->sections [i].size);
		section->st_raw_data_size = GUINT32_FROM_LE (GUINT32_TO_LE (section->st_virtual_size) + (FILE_ALIGN - 1));
		section->st_raw_data_size &= GUINT32_FROM_LE (~(FILE_ALIGN - 1));
		section->st_raw_data_ptr = GUINT32_FROM_LE (assembly->sections [i].offset);
		section->st_flags = GUINT32_FROM_LE (assembly->sections [i].attrs);
		section ++;
	}
	
	checked_write_file (file, pefile->data, pefile->index);
	
	mono_dynamic_stream_reset (pefile);
	
	for (i = 0; i < MONO_SECTION_MAX; ++i) {
		if (!assembly->sections [i].size)
			continue;
		
		if (SetFilePointer (file, assembly->sections [i].offset, NULL, FILE_BEGIN) == INVALID_SET_FILE_POINTER)
			g_error ("SetFilePointer returned %d\n", GetLastError ());
		
		switch (i) {
		case MONO_SECTION_TEXT:
			/* patch entry point */
			p = (guchar*)(assembly->code.data + 2);
			value = (virtual_base + assembly->text_rva + assembly->iat_offset);
			*p++ = (value) & 0xff;
			*p++ = (value >> 8) & 0xff;
			*p++ = (value >> 16) & 0xff;
			*p++ = (value >> 24) & 0xff;
		
			checked_write_file (file, assembly->code.data, assembly->code.index);
			checked_write_file (file, assembly->resources.data, assembly->resources.index);
			checked_write_file (file, assembly->image.raw_metadata, assembly->meta_size);
			checked_write_file (file, assembly->strong_name, assembly->strong_name_size);
				

			g_free (assembly->image.raw_metadata);
			break;
		case MONO_SECTION_RELOC: {
			struct {
				guint32 page_rva;
				guint32 block_size;
				guint16 type_and_offset;
				guint16 term;
			} reloc;
			
			g_assert (sizeof (reloc) == 12);
			
			reloc.page_rva = GUINT32_FROM_LE (assembly->text_rva);
			reloc.block_size = GUINT32_FROM_LE (12);
			
			/* 
			 * the entrypoint is always at the start of the text section 
			 * 3 is IMAGE_REL_BASED_HIGHLOW
			 * 2 is patch_size_rva - text_rva
			 */
			reloc.type_and_offset = GUINT16_FROM_LE ((3 << 12) + (2));
			reloc.term = 0;
			
			checked_write_file (file, &reloc, sizeof (reloc));
			
			break;
		}
		case MONO_SECTION_RSRC:
			if (assembly->win32_res) {

				/* Fixup the offsets in the IMAGE_RESOURCE_DATA_ENTRY structures */
				fixup_resource_directory (assembly->win32_res, assembly->win32_res, assembly->sections [i].rva);
				checked_write_file (file, assembly->win32_res, assembly->win32_res_size);
			}
			break;
		default:
			g_assert_not_reached ();
		}
	}
	
	/* check that the file is properly padded */
	if (SetFilePointer (file, file_offset, NULL, FILE_BEGIN) == INVALID_SET_FILE_POINTER)
		g_error ("SetFilePointer returned %d\n", GetLastError ());
	if (! SetEndOfFile (file))
		g_error ("SetEndOfFile returned %d\n", GetLastError ());
	
	mono_dynamic_stream_reset (&assembly->code);
	mono_dynamic_stream_reset (&assembly->us);
	mono_dynamic_stream_reset (&assembly->blob);
	mono_dynamic_stream_reset (&assembly->guid);
	mono_dynamic_stream_reset (&assembly->sheap);

	g_hash_table_foreach (assembly->blob_cache, (GHFunc)g_free, NULL);
	g_hash_table_destroy (assembly->blob_cache);
	assembly->blob_cache = NULL;
}

#else /* DISABLE_REFLECTION_EMIT_SAVE */

void
mono_image_create_pefile (MonoReflectionModuleBuilder *mb, HANDLE file)
{
	g_assert_not_reached ();
}

#endif /* DISABLE_REFLECTION_EMIT_SAVE */

#ifndef DISABLE_REFLECTION_EMIT

MonoReflectionModule *
mono_image_load_module_dynamic (MonoReflectionAssemblyBuilder *ab, MonoString *fileName)
{
	char *name;
	MonoImage *image;
	MonoImageOpenStatus status;
	MonoDynamicAssembly *assembly;
	guint32 module_count;
	MonoImage **new_modules;
	gboolean *new_modules_loaded;
	
	name = mono_string_to_utf8 (fileName);

	image = mono_image_open (name, &status);
	if (!image) {
		MonoException *exc;
		if (status == MONO_IMAGE_ERROR_ERRNO)
			exc = mono_get_exception_file_not_found (fileName);
		else
			exc = mono_get_exception_bad_image_format (name);
		g_free (name);
		mono_raise_exception (exc);
	}

	g_free (name);

	assembly = ab->dynamic_assembly;
	image->assembly = (MonoAssembly*)assembly;

	module_count = image->assembly->image->module_count;
	new_modules = g_new0 (MonoImage *, module_count + 1);
	new_modules_loaded = g_new0 (gboolean, module_count + 1);

	if (image->assembly->image->modules)
		memcpy (new_modules, image->assembly->image->modules, module_count * sizeof (MonoImage *));
	if (image->assembly->image->modules_loaded)
		memcpy (new_modules_loaded, image->assembly->image->modules_loaded, module_count * sizeof (gboolean));
	new_modules [module_count] = image;
	new_modules_loaded [module_count] = TRUE;
	mono_image_addref (image);

	g_free (image->assembly->image->modules);
	image->assembly->image->modules = new_modules;
	image->assembly->image->modules_loaded = new_modules_loaded;
	image->assembly->image->module_count ++;

	mono_assembly_load_references (image, &status);
	if (status) {
		mono_image_close (image);
		mono_raise_exception (mono_get_exception_file_not_found (fileName));
	}

	return mono_module_get_object (mono_domain_get (), image);
}

#endif /* DISABLE_REFLECTION_EMIT */

/*
 * We need to return always the same object for MethodInfo, FieldInfo etc..
 * but we need to consider the reflected type.
 * type uses a different hash, since it uses custom hash/equal functions.
 */

typedef struct {
	gpointer item;
	MonoClass *refclass;
} ReflectedEntry;

static gboolean
reflected_equal (gconstpointer a, gconstpointer b) {
	const ReflectedEntry *ea = a;
	const ReflectedEntry *eb = b;

	return (ea->item == eb->item) && (ea->refclass == eb->refclass);
}

static guint
reflected_hash (gconstpointer a) {
	const ReflectedEntry *ea = a;
	return mono_aligned_addr_hash (ea->item);
}

#define CHECK_OBJECT(t,p,k)	\
	do {	\
		t _obj;	\
		ReflectedEntry e; 	\
		e.item = (p);	\
		e.refclass = (k);	\
		mono_domain_lock (domain);	\
		if (!domain->refobject_hash)	\
			domain->refobject_hash = mono_g_hash_table_new_type (reflected_hash, reflected_equal, MONO_HASH_VALUE_GC);	\
		if ((_obj = mono_g_hash_table_lookup (domain->refobject_hash, &e))) {	\
			mono_domain_unlock (domain);	\
			return _obj;	\
		}	\
        mono_domain_unlock (domain); \
	} while (0)

#ifdef HAVE_BOEHM_GC
/* ReflectedEntry doesn't need to be GC tracked */
#define ALLOC_REFENTRY g_new0 (ReflectedEntry, 1)
#define FREE_REFENTRY(entry) g_free ((entry))
#define REFENTRY_REQUIRES_CLEANUP
#else
#define ALLOC_REFENTRY mono_mempool_alloc (domain->mp, sizeof (ReflectedEntry))
/* FIXME: */
#define FREE_REFENTRY(entry)
#endif

#define CACHE_OBJECT(t,p,o,k)	\
	do {	\
		t _obj;	\
        ReflectedEntry pe; \
        pe.item = (p); \
        pe.refclass = (k); \
        mono_domain_lock (domain); \
		if (!domain->refobject_hash)	\
			domain->refobject_hash = mono_g_hash_table_new_type (reflected_hash, reflected_equal, MONO_HASH_VALUE_GC);	\
        _obj = mono_g_hash_table_lookup (domain->refobject_hash, &pe); \
        if (!_obj) { \
		    ReflectedEntry *e = ALLOC_REFENTRY; 	\
		    e->item = (p);	\
		    e->refclass = (k);	\
		    mono_g_hash_table_insert (domain->refobject_hash, e,o);	\
            _obj = o; \
        } \
		mono_domain_unlock (domain);	\
        return _obj; \
	} while (0)

static void
clear_cached_object (MonoDomain *domain, gpointer o, MonoClass *klass)
{
	mono_domain_lock (domain);
	if (domain->refobject_hash) {
        ReflectedEntry pe;
		gpointer orig_pe, orig_value;

		pe.item = o;
		pe.refclass = klass;
		if (mono_g_hash_table_lookup_extended (domain->refobject_hash, &pe, &orig_pe, &orig_value)) {
			mono_g_hash_table_remove (domain->refobject_hash, &pe);
			FREE_REFENTRY (orig_pe);
		}
	}
	mono_domain_unlock (domain);
}

#ifdef REFENTRY_REQUIRES_CLEANUP
static void
cleanup_refobject_hash (gpointer key, gpointer value, gpointer user_data)
{
	FREE_REFENTRY (key);
}
#endif

void
mono_reflection_cleanup_domain (MonoDomain *domain)
{
	if (domain->refobject_hash) {
/*let's avoid scanning the whole hashtable if not needed*/
#ifdef REFENTRY_REQUIRES_CLEANUP
		mono_g_hash_table_foreach (domain->refobject_hash, cleanup_refobject_hash, NULL);
#endif
		mono_g_hash_table_destroy (domain->refobject_hash);
		domain->refobject_hash = NULL;
	}
}

#ifndef DISABLE_REFLECTION_EMIT
static gpointer
register_assembly (MonoDomain *domain, MonoReflectionAssembly *res, MonoAssembly *assembly)
{
	CACHE_OBJECT (MonoReflectionAssembly *, assembly, res, NULL);
}

static gpointer
register_module (MonoDomain *domain, MonoReflectionModuleBuilder *res, MonoDynamicImage *module)
{
	CACHE_OBJECT (MonoReflectionModuleBuilder *, module, res, NULL);
}

void
mono_image_module_basic_init (MonoReflectionModuleBuilder *moduleb)
{
	MonoDynamicImage *image = moduleb->dynamic_image;
	MonoReflectionAssemblyBuilder *ab = moduleb->assemblyb;
	if (!image) {
		MonoError error;
		int module_count;
		MonoImage **new_modules;
		MonoImage *ass;
		char *name, *fqname;
		/*
		 * FIXME: we already created an image in mono_image_basic_init (), but
		 * we don't know which module it belongs to, since that is only 
		 * determined at assembly save time.
		 */
		/*image = (MonoDynamicImage*)ab->dynamic_assembly->assembly.image; */
		name = mono_string_to_utf8 (ab->name);
		fqname = mono_string_to_utf8_checked (moduleb->module.fqname, &error);
		if (!mono_error_ok (&error)) {
			g_free (name);
			mono_error_raise_exception (&error);
		}
		image = create_dynamic_mono_image (ab->dynamic_assembly, name, fqname);

		moduleb->module.image = &image->image;
		moduleb->dynamic_image = image;
		register_module (mono_object_domain (moduleb), moduleb, image);

		/* register the module with the assembly */
		ass = ab->dynamic_assembly->assembly.image;
		module_count = ass->module_count;
		new_modules = g_new0 (MonoImage *, module_count + 1);

		if (ass->modules)
			memcpy (new_modules, ass->modules, module_count * sizeof (MonoImage *));
		new_modules [module_count] = &image->image;
		mono_image_addref (&image->image);

		g_free (ass->modules);
		ass->modules = new_modules;
		ass->module_count ++;
	}
}

void
mono_image_set_wrappers_type (MonoReflectionModuleBuilder *moduleb, MonoReflectionType *type)
{
	MonoDynamicImage *image = moduleb->dynamic_image;

	g_assert (type->type);
	image->wrappers_type = mono_class_from_mono_type (type->type);
}

#endif

/*
 * mono_assembly_get_object:
 * @domain: an app domain
 * @assembly: an assembly
 *
 * Return an System.Reflection.Assembly object representing the MonoAssembly @assembly.
 */
MonoReflectionAssembly*
mono_assembly_get_object (MonoDomain *domain, MonoAssembly *assembly)
{
	static MonoClass *assembly_type;
	MonoReflectionAssembly *res;
	
	CHECK_OBJECT (MonoReflectionAssembly *, assembly, NULL);
	if (!assembly_type) {
		MonoClass *class = mono_class_from_name (mono_defaults.corlib, "System.Reflection", "MonoAssembly");
		if (class == NULL)
			class = mono_class_from_name (mono_defaults.corlib, "System.Reflection", "Assembly");
		g_assert (class);
		assembly_type = class;
	}
	res = (MonoReflectionAssembly *)mono_object_new (domain, assembly_type);
	res->assembly = assembly;

	CACHE_OBJECT (MonoReflectionAssembly *, assembly, res, NULL);
}



MonoReflectionModule*   
mono_module_get_object   (MonoDomain *domain, MonoImage *image)
{
	static MonoClass *module_type;
	MonoReflectionModule *res;
	char* basename;
	
	CHECK_OBJECT (MonoReflectionModule *, image, NULL);
	if (!module_type) {
		MonoClass *class = mono_class_from_name (mono_defaults.corlib, "System.Reflection", "MonoModule");
		if (class == NULL)
			class = mono_class_from_name (mono_defaults.corlib, "System.Reflection", "Module");
		g_assert (class);
		module_type = class;
	}
	res = (MonoReflectionModule *)mono_object_new (domain, module_type);

	res->image = image;
	MONO_OBJECT_SETREF (res, assembly, (MonoReflectionAssembly *) mono_assembly_get_object(domain, image->assembly));

	MONO_OBJECT_SETREF (res, fqname, mono_string_new (domain, image->name));
	basename = g_path_get_basename (image->name);
	MONO_OBJECT_SETREF (res, name, mono_string_new (domain, basename));
	MONO_OBJECT_SETREF (res, scopename, mono_string_new (domain, image->module_name));
	
	g_free (basename);

	if (image->assembly->image == image) {
		res->token = mono_metadata_make_token (MONO_TABLE_MODULE, 1);
	} else {
		int i;
		res->token = 0;
		if (image->assembly->image->modules) {
			for (i = 0; i < image->assembly->image->module_count; i++) {
				if (image->assembly->image->modules [i] == image)
					res->token = mono_metadata_make_token (MONO_TABLE_MODULEREF, i + 1);
			}
			g_assert (res->token);
		}
	}

	CACHE_OBJECT (MonoReflectionModule *, image, res, NULL);
}

MonoReflectionModule*   
mono_module_file_get_object (MonoDomain *domain, MonoImage *image, int table_index)
{
	static MonoClass *module_type;
	MonoReflectionModule *res;
	MonoTableInfo *table;
	guint32 cols [MONO_FILE_SIZE];
	const char *name;
	guint32 i, name_idx;
	const char *val;
	
	if (!module_type) {
		MonoClass *class = mono_class_from_name (mono_defaults.corlib, "System.Reflection", "MonoModule");
		if (class == NULL)
			class = mono_class_from_name (mono_defaults.corlib, "System.Reflection", "Module");
		g_assert (class);
		module_type = class;
	}
	res = (MonoReflectionModule *)mono_object_new (domain, module_type);

	table = &image->tables [MONO_TABLE_FILE];
	g_assert (table_index < table->rows);
	mono_metadata_decode_row (table, table_index, cols, MONO_FILE_SIZE);

	res->image = NULL;
	MONO_OBJECT_SETREF (res, assembly, (MonoReflectionAssembly *) mono_assembly_get_object(domain, image->assembly));
	name = mono_metadata_string_heap (image, cols [MONO_FILE_NAME]);

	/* Check whenever the row has a corresponding row in the moduleref table */
	table = &image->tables [MONO_TABLE_MODULEREF];
	for (i = 0; i < table->rows; ++i) {
		name_idx = mono_metadata_decode_row_col (table, i, MONO_MODULEREF_NAME);
		val = mono_metadata_string_heap (image, name_idx);
		if (strcmp (val, name) == 0)
			res->image = image->modules [i];
	}

	MONO_OBJECT_SETREF (res, fqname, mono_string_new (domain, name));
	MONO_OBJECT_SETREF (res, name, mono_string_new (domain, name));
	MONO_OBJECT_SETREF (res, scopename, mono_string_new (domain, name));
	res->is_resource = cols [MONO_FILE_FLAGS] && FILE_CONTAINS_NO_METADATA;
	res->token = mono_metadata_make_token (MONO_TABLE_FILE, table_index + 1);

	return res;
}

static gboolean
verify_safe_for_managed_space (MonoType *type)
{
	switch (type->type) {
#ifdef DEBUG_HARDER
	case MONO_TYPE_ARRAY:
		return verify_safe_for_managed_space (&type->data.array->eklass->byval_arg);
	case MONO_TYPE_PTR:
		return verify_safe_for_managed_space (type->data.type);
	case MONO_TYPE_SZARRAY:
		return verify_safe_for_managed_space (&type->data.klass->byval_arg);
	case MONO_TYPE_GENERICINST: {
		MonoGenericInst *inst = type->data.generic_class->inst;
		int i;
		if (!inst->is_open)
			break;
		for (i = 0; i < inst->type_argc; ++i)
			if (!verify_safe_for_managed_space (inst->type_argv [i]))
				return FALSE;
		break;
	}
#endif
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		return TRUE;
	}
	return TRUE;
}

static MonoType*
mono_type_normalize (MonoType *type)
{
	int i;
	MonoGenericClass *gclass;
	MonoGenericInst *ginst;
	MonoClass *gtd;
	MonoGenericContainer *gcontainer;
	MonoType **argv = NULL;
	gboolean is_denorm_gtd = TRUE, requires_rebind = FALSE;

	if (type->type != MONO_TYPE_GENERICINST)
		return type;

	gclass = type->data.generic_class;
	ginst = gclass->context.class_inst;
	if (!ginst->is_open)
		return type;

	gtd = gclass->container_class;
	gcontainer = gtd->generic_container;
	argv = g_newa (MonoType*, ginst->type_argc);

	for (i = 0; i < ginst->type_argc; ++i) {
		MonoType *t = ginst->type_argv [i], *norm;
		if (t->type != MONO_TYPE_VAR || t->data.generic_param->num != i || t->data.generic_param->owner != gcontainer)
			is_denorm_gtd = FALSE;
		norm = mono_type_normalize (t);
		argv [i] = norm;
		if (norm != t)
			requires_rebind = TRUE;
	}

	if (is_denorm_gtd)
		return type->byref == gtd->byval_arg.byref ? &gtd->byval_arg : &gtd->this_arg;

	if (requires_rebind) {
		MonoClass *klass = mono_class_bind_generic_parameters (gtd, ginst->type_argc, argv, gclass->is_dynamic);
		return type->byref == klass->byval_arg.byref ? &klass->byval_arg : &klass->this_arg;
	}

	return type;
}
/*
 * mono_type_get_object:
 * @domain: an app domain
 * @type: a type
 *
 * Return an System.MonoType object representing the type @type.
 */
MonoReflectionType*
mono_type_get_object (MonoDomain *domain, MonoType *type)
{
	MonoType *norm_type;
	MonoReflectionType *res;
	MonoClass *klass = mono_class_from_mono_type (type);

	/*we must avoid using @type as it might have come
	 * from a mono_metadata_type_dup and the caller
	 * expects that is can be freed.
	 * Using the right type from 
	 */
	type = klass->byval_arg.byref == type->byref ? &klass->byval_arg : &klass->this_arg;

	/* void is very common */
	if (type->type == MONO_TYPE_VOID && domain->typeof_void)
		return (MonoReflectionType*)domain->typeof_void;

	/*
	 * If the vtable of the given class was already created, we can use
	 * the MonoType from there and avoid all locking and hash table lookups.
	 * 
	 * We cannot do this for TypeBuilders as mono_reflection_create_runtime_class expects
	 * that the resulting object is different.   
	 */
	if (type == &klass->byval_arg && !klass->image->dynamic) {
		MonoVTable *vtable = mono_class_try_get_vtable (domain, klass);
		if (vtable && vtable->type)
			return vtable->type;
	}

	mono_loader_lock (); /*FIXME mono_class_init and mono_class_vtable acquire it*/
	mono_domain_lock (domain);
	if (!domain->type_hash)
		domain->type_hash = mono_g_hash_table_new_type ((GHashFunc)mono_metadata_type_hash, 
				(GCompareFunc)mono_metadata_type_equal, MONO_HASH_VALUE_GC);
	if ((res = mono_g_hash_table_lookup (domain->type_hash, type))) {
		mono_domain_unlock (domain);
		mono_loader_unlock ();
		return res;
	}

	/*Types must be normalized so a generic instance of the GTD get's the same inner type.
	 * For example in: Foo<A,B>; Bar<A> : Foo<A, Bar<A>>
	 * The second Bar will be encoded a generic instance of Bar with <A> as parameter.
	 * On all other places, Bar<A> will be encoded as the GTD itself. This is an implementation
	 * artifact of how generics are encoded and should be transparent to managed code so we
	 * need to weed out this diference when retrieving managed System.Type objects.
	 */
	norm_type = mono_type_normalize (type);
	if (norm_type != type) {
		res = mono_type_get_object (domain, norm_type);
		mono_g_hash_table_insert (domain->type_hash, type, res);
		mono_domain_unlock (domain);
		mono_loader_unlock ();
		return res;
	}

	/* This MonoGenericClass hack is no longer necessary. Let's leave it here until we finish with the 2-stage type-builder setup.*/
	if ((type->type == MONO_TYPE_GENERICINST) && type->data.generic_class->is_dynamic && !type->data.generic_class->container_class->wastypebuilder)
		g_assert (0);

	if (!verify_safe_for_managed_space (type)) {
		mono_domain_unlock (domain);
		mono_loader_unlock ();
		mono_raise_exception (mono_get_exception_invalid_operation ("This type cannot be propagated to managed space"));
	}

	if (mono_class_get_ref_info (klass) && !klass->wastypebuilder) {
		gboolean is_type_done = TRUE;
		/* Generic parameters have reflection_info set but they are not finished together with their enclosing type.
		 * We must ensure that once a type is finished we don't return a GenericTypeParameterBuilder.
		 * We can't simply close the types as this will interfere with other parts of the generics machinery.
		*/
		if (klass->byval_arg.type == MONO_TYPE_MVAR || klass->byval_arg.type == MONO_TYPE_VAR) {
			MonoGenericParam *gparam = klass->byval_arg.data.generic_param;

			if (gparam->owner && gparam->owner->is_method) {
				MonoMethod *method = gparam->owner->owner.method;
				if (method && mono_class_get_generic_type_definition (method->klass)->wastypebuilder)
					is_type_done = FALSE;
			} else if (gparam->owner && !gparam->owner->is_method) {
				MonoClass *klass = gparam->owner->owner.klass;
				if (klass && mono_class_get_generic_type_definition (klass)->wastypebuilder)
					is_type_done = FALSE;
			}
		} 

		/* g_assert_not_reached (); */
		/* should this be considered an error condition? */
		if (is_type_done && !type->byref) {
			mono_domain_unlock (domain);
			mono_loader_unlock ();
			return mono_class_get_ref_info (klass);
		}
	}
	/* This is stored in vtables/JITted code so it has to be pinned */
	res = (MonoReflectionType *)mono_object_new_pinned (domain, mono_defaults.monotype_class);
	res->type = type;
	mono_g_hash_table_insert (domain->type_hash, type, res);

	if (type->type == MONO_TYPE_VOID)
		domain->typeof_void = (MonoObject*)res;

	mono_domain_unlock (domain);
	mono_loader_unlock ();
	return res;
}

/*
 * mono_method_get_object:
 * @domain: an app domain
 * @method: a method
 * @refclass: the reflected type (can be NULL)
 *
 * Return an System.Reflection.MonoMethod object representing the method @method.
 */
MonoReflectionMethod*
mono_method_get_object (MonoDomain *domain, MonoMethod *method, MonoClass *refclass)
{
	/*
	 * We use the same C representation for methods and constructors, but the type 
	 * name in C# is different.
	 */
	static MonoClass *System_Reflection_MonoMethod = NULL;
	static MonoClass *System_Reflection_MonoCMethod = NULL;
	static MonoClass *System_Reflection_MonoGenericMethod = NULL;
	static MonoClass *System_Reflection_MonoGenericCMethod = NULL;
	MonoClass *klass;
	MonoReflectionMethod *ret;

	if (method->is_inflated) {
		MonoReflectionGenericMethod *gret;

		refclass = method->klass;
		CHECK_OBJECT (MonoReflectionMethod *, method, refclass);
		if ((*method->name == '.') && (!strcmp (method->name, ".ctor") || !strcmp (method->name, ".cctor"))) {
			if (!System_Reflection_MonoGenericCMethod)
				System_Reflection_MonoGenericCMethod = mono_class_from_name (mono_defaults.corlib, "System.Reflection", "MonoGenericCMethod");
			klass = System_Reflection_MonoGenericCMethod;
		} else {
			if (!System_Reflection_MonoGenericMethod)
				System_Reflection_MonoGenericMethod = mono_class_from_name (mono_defaults.corlib, "System.Reflection", "MonoGenericMethod");
			klass = System_Reflection_MonoGenericMethod;
		}
		gret = (MonoReflectionGenericMethod*)mono_object_new (domain, klass);
		gret->method.method = method;
		MONO_OBJECT_SETREF (gret, method.name, mono_string_new (domain, method->name));
		MONO_OBJECT_SETREF (gret, method.reftype, mono_type_get_object (domain, &refclass->byval_arg));
		CACHE_OBJECT (MonoReflectionMethod *, method, (MonoReflectionMethod*)gret, refclass);
	}

	if (!refclass)
		refclass = method->klass;

	CHECK_OBJECT (MonoReflectionMethod *, method, refclass);
	if (*method->name == '.' && (strcmp (method->name, ".ctor") == 0 || strcmp (method->name, ".cctor") == 0)) {
		if (!System_Reflection_MonoCMethod)
			System_Reflection_MonoCMethod = mono_class_from_name (mono_defaults.corlib, "System.Reflection", "MonoCMethod");
		klass = System_Reflection_MonoCMethod;
	}
	else {
		if (!System_Reflection_MonoMethod)
			System_Reflection_MonoMethod = mono_class_from_name (mono_defaults.corlib, "System.Reflection", "MonoMethod");
		klass = System_Reflection_MonoMethod;
	}
	ret = (MonoReflectionMethod*)mono_object_new (domain, klass);
	ret->method = method;
	MONO_OBJECT_SETREF (ret, reftype, mono_type_get_object (domain, &refclass->byval_arg));
	CACHE_OBJECT (MonoReflectionMethod *, method, ret, refclass);
}

/*
 * mono_method_clear_object:
 *
 *   Clear the cached reflection objects for the dynamic method METHOD.
 */
void
mono_method_clear_object (MonoDomain *domain, MonoMethod *method)
{
	MonoClass *klass;
	g_assert (method->dynamic);

	klass = method->klass;
	while (klass) {
		clear_cached_object (domain, method, klass);
		klass = klass->parent;
	}
	/* Added by mono_param_get_objects () */
	clear_cached_object (domain, &(method->signature), NULL);
	klass = method->klass;
	while (klass) {
		clear_cached_object (domain, &(method->signature), klass);
		klass = klass->parent;
	}
}

/*
 * mono_field_get_object:
 * @domain: an app domain
 * @klass: a type
 * @field: a field
 *
 * Return an System.Reflection.MonoField object representing the field @field
 * in class @klass.
 */
MonoReflectionField*
mono_field_get_object (MonoDomain *domain, MonoClass *klass, MonoClassField *field)
{
	MonoReflectionField *res;
	static MonoClass *monofield_klass;

	CHECK_OBJECT (MonoReflectionField *, field, klass);
	if (!monofield_klass)
		monofield_klass = mono_class_from_name (mono_defaults.corlib, "System.Reflection", "MonoField");
	res = (MonoReflectionField *)mono_object_new (domain, monofield_klass);
	res->klass = klass;
	res->field = field;
	MONO_OBJECT_SETREF (res, name, mono_string_new (domain, mono_field_get_name (field)));

	if (is_field_on_inst (field)) {
		res->attrs = get_field_on_inst_generic_type (field)->attrs;
		MONO_OBJECT_SETREF (res, type, mono_type_get_object (domain, field->type));
	} else {
		if (field->type)
			MONO_OBJECT_SETREF (res, type, mono_type_get_object (domain, field->type));
		res->attrs = mono_field_get_flags (field);
	}
	CACHE_OBJECT (MonoReflectionField *, field, res, klass);
}

/*
 * mono_property_get_object:
 * @domain: an app domain
 * @klass: a type
 * @property: a property
 *
 * Return an System.Reflection.MonoProperty object representing the property @property
 * in class @klass.
 */
MonoReflectionProperty*
mono_property_get_object (MonoDomain *domain, MonoClass *klass, MonoProperty *property)
{
	MonoReflectionProperty *res;
	static MonoClass *monoproperty_klass;

	CHECK_OBJECT (MonoReflectionProperty *, property, klass);
	if (!monoproperty_klass)
		monoproperty_klass = mono_class_from_name (mono_defaults.corlib, "System.Reflection", "MonoProperty");
	res = (MonoReflectionProperty *)mono_object_new (domain, monoproperty_klass);
	res->klass = klass;
	res->property = property;
	CACHE_OBJECT (MonoReflectionProperty *, property, res, klass);
}

/*
 * mono_event_get_object:
 * @domain: an app domain
 * @klass: a type
 * @event: a event
 *
 * Return an System.Reflection.MonoEvent object representing the event @event
 * in class @klass.
 */
MonoReflectionEvent*
mono_event_get_object (MonoDomain *domain, MonoClass *klass, MonoEvent *event)
{
	MonoReflectionEvent *res;
	MonoReflectionMonoEvent *mono_event;
	static MonoClass *monoevent_klass;

	CHECK_OBJECT (MonoReflectionEvent *, event, klass);
	if (!monoevent_klass)
		monoevent_klass = mono_class_from_name (mono_defaults.corlib, "System.Reflection", "MonoEvent");
	mono_event = (MonoReflectionMonoEvent *)mono_object_new (domain, monoevent_klass);
	mono_event->klass = klass;
	mono_event->event = event;
	res = (MonoReflectionEvent*)mono_event;
	CACHE_OBJECT (MonoReflectionEvent *, event, res, klass);
}

/**
 * mono_get_reflection_missing_object:
 * @domain: Domain where the object lives
 *
 * Returns the System.Reflection.Missing.Value singleton object
 * (of type System.Reflection.Missing).
 *
 * Used as the value for ParameterInfo.DefaultValue when Optional
 * is present
 */
static MonoObject *
mono_get_reflection_missing_object (MonoDomain *domain)
{
	MonoObject *obj;
	static MonoClassField *missing_value_field = NULL;
	
	if (!missing_value_field) {
		MonoClass *missing_klass;
		missing_klass = mono_class_from_name (mono_defaults.corlib, "System.Reflection", "Missing");
		mono_class_init (missing_klass);
		missing_value_field = mono_class_get_field_from_name (missing_klass, "Value");
		g_assert (missing_value_field);
	}
	obj = mono_field_get_value_object (domain, missing_value_field, NULL); 
	g_assert (obj);
	return obj;
}

static MonoObject*
get_dbnull (MonoDomain *domain, MonoObject **dbnull)
{
	if (!*dbnull)
		*dbnull = mono_get_dbnull_object (domain);
	return *dbnull;
}

static MonoObject*
get_reflection_missing (MonoDomain *domain, MonoObject **reflection_missing)
{
	if (!*reflection_missing)
		*reflection_missing = mono_get_reflection_missing_object (domain);
	return *reflection_missing;
}

/*
 * mono_param_get_objects:
 * @domain: an app domain
 * @method: a method
 *
 * Return an System.Reflection.ParameterInfo array object representing the parameters
 * in the method @method.
 */
MonoArray*
mono_param_get_objects_internal (MonoDomain *domain, MonoMethod *method, MonoClass *refclass)
{
	static MonoClass *System_Reflection_ParameterInfo;
	static MonoClass *System_Reflection_ParameterInfo_array;
	MonoError error;
	MonoArray *res = NULL;
	MonoReflectionMethod *member = NULL;
	MonoReflectionParameter *param = NULL;
	char **names, **blobs = NULL;
	guint32 *types = NULL;
	MonoType *type = NULL;
	MonoObject *dbnull = NULL;
	MonoObject *missing = NULL;
	MonoMarshalSpec **mspecs;
	MonoMethodSignature *sig;
	MonoVTable *pinfo_vtable;
	int i;

	if (!System_Reflection_ParameterInfo_array) {
		MonoClass *klass;

		klass = mono_class_from_name (mono_defaults.corlib, "System.Reflection", "ParameterInfo");
		mono_memory_barrier ();
		System_Reflection_ParameterInfo = klass; 
	
		klass = mono_array_class_get (klass, 1);
		mono_memory_barrier ();
		System_Reflection_ParameterInfo_array = klass;
	}

	sig = mono_method_signature_checked (method, &error);
	if (!mono_error_ok (&error))
		mono_error_raise_exception (&error);

	if (!sig->param_count)
		return mono_array_new_specific (mono_class_vtable (domain, System_Reflection_ParameterInfo_array), 0);

	/* Note: the cache is based on the address of the signature into the method
	 * since we already cache MethodInfos with the method as keys.
	 */
	CHECK_OBJECT (MonoArray*, &(method->signature), refclass);

	member = mono_method_get_object (domain, method, refclass);
	names = g_new (char *, sig->param_count);
	mono_method_get_param_names (method, (const char **) names);

	mspecs = g_new (MonoMarshalSpec*, sig->param_count + 1);
	mono_method_get_marshal_info (method, mspecs);

	res = mono_array_new_specific (mono_class_vtable (domain, System_Reflection_ParameterInfo_array), sig->param_count);
	pinfo_vtable = mono_class_vtable (domain, System_Reflection_ParameterInfo);
	for (i = 0; i < sig->param_count; ++i) {
		param = (MonoReflectionParameter *)mono_object_new_specific (pinfo_vtable);
		MONO_OBJECT_SETREF (param, ClassImpl, mono_type_get_object (domain, sig->params [i]));
		MONO_OBJECT_SETREF (param, MemberImpl, (MonoObject*)member);
		MONO_OBJECT_SETREF (param, NameImpl, mono_string_new (domain, names [i]));
		param->PositionImpl = i;
		param->AttrsImpl = sig->params [i]->attrs;

		if (!(param->AttrsImpl & PARAM_ATTRIBUTE_HAS_DEFAULT)) {
			if (param->AttrsImpl & PARAM_ATTRIBUTE_OPTIONAL)
				MONO_OBJECT_SETREF (param, DefaultValueImpl, get_reflection_missing (domain, &missing));
			else
				MONO_OBJECT_SETREF (param, DefaultValueImpl, get_dbnull (domain, &dbnull));
		} else {

			if (!blobs) {
				blobs = g_new0 (char *, sig->param_count);
				types = g_new0 (guint32, sig->param_count);
				get_default_param_value_blobs (method, blobs, types); 
			}

			/* Build MonoType for the type from the Constant Table */
			if (!type)
				type = g_new0 (MonoType, 1);
			type->type = types [i];
			type->data.klass = NULL;
			if (types [i] == MONO_TYPE_CLASS)
				type->data.klass = mono_defaults.object_class;
			else if ((sig->params [i]->type == MONO_TYPE_VALUETYPE) && sig->params [i]->data.klass->enumtype) {
				/* For enums, types [i] contains the base type */

					type->type = MONO_TYPE_VALUETYPE;
					type->data.klass = mono_class_from_mono_type (sig->params [i]);
			} else
				type->data.klass = mono_class_from_mono_type (type);

			MONO_OBJECT_SETREF (param, DefaultValueImpl, mono_get_object_from_blob (domain, type, blobs [i]));

			/* Type in the Constant table is MONO_TYPE_CLASS for nulls */
			if (types [i] != MONO_TYPE_CLASS && !param->DefaultValueImpl) {
				if (param->AttrsImpl & PARAM_ATTRIBUTE_OPTIONAL)
					MONO_OBJECT_SETREF (param, DefaultValueImpl, get_reflection_missing (domain, &missing));
				else
					MONO_OBJECT_SETREF (param, DefaultValueImpl, get_dbnull (domain, &dbnull));
			}
			
		}

		if (mspecs [i + 1])
			MONO_OBJECT_SETREF (param, MarshalAsImpl, (MonoObject*)mono_reflection_marshal_as_attribute_from_marshal_spec (domain, method->klass, mspecs [i + 1]));
		
		mono_array_setref (res, i, param);
	}
	g_free (names);
	g_free (blobs);
	g_free (types);
	g_free (type);

	for (i = mono_method_signature (method)->param_count; i >= 0; i--)
		if (mspecs [i])
			mono_metadata_free_marshal_spec (mspecs [i]);
	g_free (mspecs);
	
	CACHE_OBJECT (MonoArray *, &(method->signature), res, refclass);
}

MonoArray*
mono_param_get_objects (MonoDomain *domain, MonoMethod *method)
{
	return mono_param_get_objects_internal (domain, method, NULL);
}

/*
 * mono_method_body_get_object:
 * @domain: an app domain
 * @method: a method
 *
 * Return an System.Reflection.MethodBody object representing the method @method.
 */
MonoReflectionMethodBody*
mono_method_body_get_object (MonoDomain *domain, MonoMethod *method)
{
	static MonoClass *System_Reflection_MethodBody = NULL;
	static MonoClass *System_Reflection_LocalVariableInfo = NULL;
	static MonoClass *System_Reflection_ExceptionHandlingClause = NULL;
	MonoReflectionMethodBody *ret;
	MonoMethodHeader *header;
	MonoImage *image;
	guint32 method_rva, local_var_sig_token;
    char *ptr;
	unsigned char format, flags;
	int i;

	/* for compatibility with .net */
    if (method->dynamic)
        mono_raise_exception (mono_get_exception_invalid_operation (NULL));

	if (!System_Reflection_MethodBody)
		System_Reflection_MethodBody = mono_class_from_name (mono_defaults.corlib, "System.Reflection", "MethodBody");
	if (!System_Reflection_LocalVariableInfo)
		System_Reflection_LocalVariableInfo = mono_class_from_name (mono_defaults.corlib, "System.Reflection", "LocalVariableInfo");
	if (!System_Reflection_ExceptionHandlingClause)
		System_Reflection_ExceptionHandlingClause = mono_class_from_name (mono_defaults.corlib, "System.Reflection", "ExceptionHandlingClause");

	CHECK_OBJECT (MonoReflectionMethodBody *, method, NULL);

	if ((method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) ||
		(method->flags & METHOD_ATTRIBUTE_ABSTRACT) ||
	    (method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
	    (method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME))
		return NULL;

	image = method->klass->image;
	header = mono_method_get_header (method);

	if (!image->dynamic) {
		/* Obtain local vars signature token */
		method_rva = mono_metadata_decode_row_col (&image->tables [MONO_TABLE_METHOD], mono_metadata_token_index (method->token) - 1, MONO_METHOD_RVA);
		ptr = mono_image_rva_map (image, method_rva);
		flags = *(const unsigned char *) ptr;
		format = flags & METHOD_HEADER_FORMAT_MASK;
		switch (format){
		case METHOD_HEADER_TINY_FORMAT:
			local_var_sig_token = 0;
			break;
		case METHOD_HEADER_FAT_FORMAT:
			ptr += 2;
			ptr += 2;
			ptr += 4;
			local_var_sig_token = read32 (ptr);
			break;
		default:
			g_assert_not_reached ();
		}
	} else
		local_var_sig_token = 0; //FIXME

	ret = (MonoReflectionMethodBody*)mono_object_new (domain, System_Reflection_MethodBody);

	ret->init_locals = header->init_locals;
	ret->max_stack = header->max_stack;
	ret->local_var_sig_token = local_var_sig_token;
	MONO_OBJECT_SETREF (ret, il, mono_array_new_cached (domain, mono_defaults.byte_class, header->code_size));
	memcpy (mono_array_addr (ret->il, guint8, 0), header->code, header->code_size);

	/* Locals */
	MONO_OBJECT_SETREF (ret, locals, mono_array_new_cached (domain, System_Reflection_LocalVariableInfo, header->num_locals));
	for (i = 0; i < header->num_locals; ++i) {
		MonoReflectionLocalVariableInfo *info = (MonoReflectionLocalVariableInfo*)mono_object_new (domain, System_Reflection_LocalVariableInfo);
		MONO_OBJECT_SETREF (info, local_type, mono_type_get_object (domain, header->locals [i]));
		info->is_pinned = header->locals [i]->pinned;
		info->local_index = i;
		mono_array_setref (ret->locals, i, info);
	}

	/* Exceptions */
	MONO_OBJECT_SETREF (ret, clauses, mono_array_new_cached (domain, System_Reflection_ExceptionHandlingClause, header->num_clauses));
	for (i = 0; i < header->num_clauses; ++i) {
		MonoReflectionExceptionHandlingClause *info = (MonoReflectionExceptionHandlingClause*)mono_object_new (domain, System_Reflection_ExceptionHandlingClause);
		MonoExceptionClause *clause = &header->clauses [i];

		info->flags = clause->flags;
		info->try_offset = clause->try_offset;
		info->try_length = clause->try_len;
		info->handler_offset = clause->handler_offset;
		info->handler_length = clause->handler_len;
		if (clause->flags == MONO_EXCEPTION_CLAUSE_FILTER)
			info->filter_offset = clause->data.filter_offset;
		else if (clause->data.catch_class)
			MONO_OBJECT_SETREF (info, catch_type, mono_type_get_object (mono_domain_get (), &clause->data.catch_class->byval_arg));

		mono_array_setref (ret->clauses, i, info);
	}

	mono_metadata_free_mh (header);
	CACHE_OBJECT (MonoReflectionMethodBody *, method, ret, NULL);
	return ret;
}

/**
 * mono_get_dbnull_object:
 * @domain: Domain where the object lives
 *
 * Returns the System.DBNull.Value singleton object
 *
 * Used as the value for ParameterInfo.DefaultValue 
 */
MonoObject *
mono_get_dbnull_object (MonoDomain *domain)
{
	MonoObject *obj;
	static MonoClassField *dbnull_value_field = NULL;
	
	if (!dbnull_value_field) {
		MonoClass *dbnull_klass;
		dbnull_klass = mono_class_from_name (mono_defaults.corlib, "System", "DBNull");
		mono_class_init (dbnull_klass);
		dbnull_value_field = mono_class_get_field_from_name (dbnull_klass, "Value");
		g_assert (dbnull_value_field);
	}
	obj = mono_field_get_value_object (domain, dbnull_value_field, NULL); 
	g_assert (obj);
	return obj;
}

static void
get_default_param_value_blobs (MonoMethod *method, char **blobs, guint32 *types)
{
	guint32 param_index, i, lastp, crow = 0;
	guint32 param_cols [MONO_PARAM_SIZE], const_cols [MONO_CONSTANT_SIZE];
	gint32 idx;

	MonoClass *klass = method->klass;
	MonoImage *image = klass->image;
	MonoMethodSignature *methodsig = mono_method_signature (method);

	MonoTableInfo *constt;
	MonoTableInfo *methodt;
	MonoTableInfo *paramt;

	if (!methodsig->param_count)
		return;

	mono_class_init (klass);

	if (klass->image->dynamic) {
		MonoReflectionMethodAux *aux;
		if (method->is_inflated)
			method = ((MonoMethodInflated*)method)->declaring;
		aux = g_hash_table_lookup (((MonoDynamicImage*)method->klass->image)->method_aux_hash, method);
		if (aux && aux->param_defaults) {
			memcpy (blobs, &(aux->param_defaults [1]), methodsig->param_count * sizeof (char*));
			memcpy (types, &(aux->param_default_types [1]), methodsig->param_count * sizeof (guint32));
		}
		return;
	}

	methodt = &klass->image->tables [MONO_TABLE_METHOD];
	paramt = &klass->image->tables [MONO_TABLE_PARAM];
	constt = &image->tables [MONO_TABLE_CONSTANT];

	idx = mono_method_get_index (method) - 1;
	g_assert (idx != -1);

	param_index = mono_metadata_decode_row_col (methodt, idx, MONO_METHOD_PARAMLIST);
	if (idx + 1 < methodt->rows)
		lastp = mono_metadata_decode_row_col (methodt, idx + 1, MONO_METHOD_PARAMLIST);
	else
		lastp = paramt->rows + 1;

	for (i = param_index; i < lastp; ++i) {
		guint32 paramseq;

		mono_metadata_decode_row (paramt, i - 1, param_cols, MONO_PARAM_SIZE);
		paramseq = param_cols [MONO_PARAM_SEQUENCE];

		if (!(param_cols [MONO_PARAM_FLAGS] & PARAM_ATTRIBUTE_HAS_DEFAULT))
			continue;

		crow = mono_metadata_get_constant_index (image, MONO_TOKEN_PARAM_DEF | i, crow + 1);
		if (!crow) {
			continue;
		}
	
		mono_metadata_decode_row (constt, crow - 1, const_cols, MONO_CONSTANT_SIZE);
		blobs [paramseq - 1] = (gpointer) mono_metadata_blob_heap (image, const_cols [MONO_CONSTANT_VALUE]);
		types [paramseq - 1] = const_cols [MONO_CONSTANT_TYPE];
	}

	return;
}

MonoObject *
mono_get_object_from_blob (MonoDomain *domain, MonoType *type, const char *blob)
{
	void *retval;
	MonoClass *klass;
	MonoObject *object;
	MonoType *basetype = type;

	if (!blob)
		return NULL;
	
	klass = mono_class_from_mono_type (type);
	if (klass->valuetype) {
		object = mono_object_new (domain, klass);
		retval = ((gchar *) object + sizeof (MonoObject));
		if (klass->enumtype)
			basetype = mono_class_enum_basetype (klass);
	} else {
		retval = &object;
	}
			
	if (!mono_get_constant_value_from_blob (domain, basetype->type,  blob, retval))
		return object;
	else
		return NULL;
}

static int
assembly_name_to_aname (MonoAssemblyName *assembly, char *p) {
	int found_sep;
	char *s;
	gboolean quoted = FALSE;

	memset (assembly, 0, sizeof (MonoAssemblyName));
	assembly->culture = "";
	memset (assembly->public_key_token, 0, MONO_PUBLIC_KEY_TOKEN_LENGTH);

	if (*p == '"') {
		quoted = TRUE;
		p++;
	}
	assembly->name = p;
	while (*p && (isalnum (*p) || *p == '.' || *p == '-' || *p == '_' || *p == '$' || *p == '@' || g_ascii_isspace (*p)))
		p++;
	if (quoted) {
		if (*p != '"')
			return 1;
		*p = 0;
		p++;
	}
	if (*p != ',')
		return 1;
	*p = 0;
	/* Remove trailing whitespace */
	s = p - 1;
	while (*s && g_ascii_isspace (*s))
		*s-- = 0;
	p ++;
	while (g_ascii_isspace (*p))
		p++;
	while (*p) {
		if (*p == 'V' && g_ascii_strncasecmp (p, "Version=", 8) == 0) {
			p += 8;
			assembly->major = strtoul (p, &s, 10);
			if (s == p || *s != '.')
				return 1;
			p = ++s;
			assembly->minor = strtoul (p, &s, 10);
			if (s == p || *s != '.')
				return 1;
			p = ++s;
			assembly->build = strtoul (p, &s, 10);
			if (s == p || *s != '.')
				return 1;
			p = ++s;
			assembly->revision = strtoul (p, &s, 10);
			if (s == p)
				return 1;
			p = s;
		} else if (*p == 'C' && g_ascii_strncasecmp (p, "Culture=", 8) == 0) {
			p += 8;
			if (g_ascii_strncasecmp (p, "neutral", 7) == 0) {
				assembly->culture = "";
				p += 7;
			} else {
				assembly->culture = p;
				while (*p && *p != ',') {
					p++;
				}
			}
		} else if (*p == 'P' && g_ascii_strncasecmp (p, "PublicKeyToken=", 15) == 0) {
			p += 15;
			if (strncmp (p, "null", 4) == 0) {
				p += 4;
			} else {
				int len;
				gchar *start = p;
				while (*p && *p != ',') {
					p++;
				}
				len = (p - start + 1);
				if (len > MONO_PUBLIC_KEY_TOKEN_LENGTH)
					len = MONO_PUBLIC_KEY_TOKEN_LENGTH;
				g_strlcpy ((char*)assembly->public_key_token, start, len);
			}
		} else {
			while (*p && *p != ',')
				p++;
		}
		found_sep = 0;
		while (g_ascii_isspace (*p) || *p == ',') {
			*p++ = 0;
			found_sep = 1;
			continue;
		}
		/* failed */
		if (!found_sep)
			return 1;
	}

	return 0;
}

/*
 * mono_reflection_parse_type:
 * @name: type name
 *
 * Parse a type name as accepted by the GetType () method and output the info
 * extracted in the info structure.
 * the name param will be mangled, so, make a copy before passing it to this function.
 * The fields in info will be valid until the memory pointed to by name is valid.
 *
 * See also mono_type_get_name () below.
 *
 * Returns: 0 on parse error.
 */
static int
_mono_reflection_parse_type (char *name, char **endptr, gboolean is_recursed,
			     MonoTypeNameParse *info)
{
	char *start, *p, *w, *temp, *last_point, *startn;
	int in_modifiers = 0;
	int isbyref = 0, rank, arity = 0, i;

	start = p = w = name;

	//FIXME could we just zero the whole struct? memset (&info, 0, sizeof (MonoTypeNameParse))
	memset (&info->assembly, 0, sizeof (MonoAssemblyName));
	info->name = info->name_space = NULL;
	info->nested = NULL;
	info->modifiers = NULL;
	info->type_arguments = NULL;

	/* last_point separates the namespace from the name */
	last_point = NULL;
	/* Skips spaces */
	while (*p == ' ') p++, start++, w++, name++;

	while (*p) {
		switch (*p) {
		case '+':
			*p = 0; /* NULL terminate the name */
			startn = p + 1;
			info->nested = g_list_append (info->nested, startn);
			/* we have parsed the nesting namespace + name */
			if (info->name)
				break;
			if (last_point) {
				info->name_space = start;
				*last_point = 0;
				info->name = last_point + 1;
			} else {
				info->name_space = (char *)"";
				info->name = start;
			}
			break;
		case '.':
			last_point = p;
			break;
		case '\\':
			++p;
			break;
		case '&':
		case '*':
		case '[':
		case ',':
		case ']':
			in_modifiers = 1;
			break;
		case '`':
			++p;
			i = strtol (p, &temp, 10);
			arity += i;
			if (p == temp)
				return 0;
			p = temp-1;
			break;
		default:
			break;
		}
		if (in_modifiers)
			break;
		// *w++ = *p++;
		p++;
	}
	
	if (!info->name) {
		if (last_point) {
			info->name_space = start;
			*last_point = 0;
			info->name = last_point + 1;
		} else {
			info->name_space = (char *)"";
			info->name = start;
		}
	}
	while (*p) {
		switch (*p) {
		case '&':
			if (isbyref) /* only one level allowed by the spec */
				return 0;
			isbyref = 1;
			info->modifiers = g_list_append (info->modifiers, GUINT_TO_POINTER (0));
			*p++ = 0;
			break;
		case '*':
			info->modifiers = g_list_append (info->modifiers, GUINT_TO_POINTER (-1));
			*p++ = 0;
			break;
		case '[':
			if (arity != 0) {
				*p++ = 0;
				info->type_arguments = g_ptr_array_new ();
				for (i = 0; i < arity; i++) {
					MonoTypeNameParse *subinfo = g_new0 (MonoTypeNameParse, 1);
					gboolean fqname = FALSE;

					g_ptr_array_add (info->type_arguments, subinfo);

					if (*p == '[') {
						p++;
						fqname = TRUE;
					}

					if (!_mono_reflection_parse_type (p, &p, TRUE, subinfo))
						return 0;

					/*MS is lenient on [] delimited parameters that aren't fqn - and F# uses them.*/
					if (fqname && (*p != ']')) {
						char *aname;

						if (*p != ',')
							return 0;
						*p++ = 0;

						aname = p;
						while (*p && (*p != ']'))
							p++;

						if (*p != ']')
							return 0;

						*p++ = 0;
						while (*aname) {
							if (g_ascii_isspace (*aname)) {
								++aname;
								continue;
							}
							break;
						}
						if (!*aname ||
						    !assembly_name_to_aname (&subinfo->assembly, aname))
							return 0;
					} else if (fqname && (*p == ']')) {
						*p++ = 0;
					}

					if (i + 1 < arity) {
						if (*p != ',')
							return 0;
					} else {
						if (*p != ']')
							return 0;
					}
					*p++ = 0;
				}

				arity = 0;
				break;
			}
			rank = 1;
			*p++ = 0;
			while (*p) {
				if (*p == ']')
					break;
				if (*p == ',')
					rank++;
				else if (*p == '*') /* '*' means unknown lower bound */
					info->modifiers = g_list_append (info->modifiers, GUINT_TO_POINTER (-2));
				else
					return 0;
				++p;
			}
			if (*p++ != ']')
				return 0;
			info->modifiers = g_list_append (info->modifiers, GUINT_TO_POINTER (rank));
			break;
		case ']':
			if (is_recursed)
				goto end;
			return 0;
		case ',':
			if (is_recursed)
				goto end;
			*p++ = 0;
			while (*p) {
				if (g_ascii_isspace (*p)) {
					++p;
					continue;
				}
				break;
			}
			if (!*p)
				return 0; /* missing assembly name */
			if (!assembly_name_to_aname (&info->assembly, p))
				return 0;
			break;
		default:
			return 0;
		}
		if (info->assembly.name)
			break;
	}
	// *w = 0; /* terminate class name */
 end:
	if (!info->name || !*info->name)
		return 0;
	if (endptr)
		*endptr = p;
	/* add other consistency checks */
	return 1;
}

int
mono_reflection_parse_type (char *name, MonoTypeNameParse *info)
{
	return _mono_reflection_parse_type (name, NULL, FALSE, info);
}

static MonoType*
_mono_reflection_get_type_from_info (MonoTypeNameParse *info, MonoImage *image, gboolean ignorecase)
{
	gboolean type_resolve = FALSE;
	MonoType *type;
	MonoImage *rootimage = image;

	if (info->assembly.name) {
		MonoAssembly *assembly = mono_assembly_loaded (&info->assembly);
		if (!assembly && image && image->assembly && mono_assembly_names_equal (&info->assembly, &image->assembly->aname))
			/* 
			 * This could happen in the AOT compiler case when the search hook is not
			 * installed.
			 */
			assembly = image->assembly;
		if (!assembly) {
			/* then we must load the assembly ourselve - see #60439 */
			assembly = mono_assembly_load (&info->assembly, NULL, NULL);
			if (!assembly)
				return NULL;
		}
		image = assembly->image;
	} else if (!image) {
		image = mono_defaults.corlib;
	}

	type = mono_reflection_get_type_with_rootimage (rootimage, image, info, ignorecase, &type_resolve);
	if (type == NULL && !info->assembly.name && image != mono_defaults.corlib) {
		image = mono_defaults.corlib;
		type = mono_reflection_get_type_with_rootimage (rootimage, image, info, ignorecase, &type_resolve);
	}

	return type;
}

static MonoType*
mono_reflection_get_type_internal (MonoImage *rootimage, MonoImage* image, MonoTypeNameParse *info, gboolean ignorecase)
{
	MonoClass *klass;
	GList *mod;
	int modval;
	gboolean bounded = FALSE;
	
	if (!image)
		image = mono_defaults.corlib;

	if (ignorecase)
		klass = mono_class_from_name_case (image, info->name_space, info->name);
	else
		klass = mono_class_from_name (image, info->name_space, info->name);
	if (!klass)
		return NULL;
	for (mod = info->nested; mod; mod = mod->next) {
		gpointer iter = NULL;
		MonoClass *parent;

		parent = klass;
		mono_class_init (parent);

		while ((klass = mono_class_get_nested_types (parent, &iter))) {
			if (ignorecase) {
				if (mono_utf8_strcasecmp (klass->name, mod->data) == 0)
					break;
			} else {
				if (strcmp (klass->name, mod->data) == 0)
					break;
			}
		}
		if (!klass)
			break;
	}
	if (!klass)
		return NULL;

	if (info->type_arguments) {
		MonoType **type_args = g_new0 (MonoType *, info->type_arguments->len);
		MonoReflectionType *the_type;
		MonoType *instance;
		int i;

		for (i = 0; i < info->type_arguments->len; i++) {
			MonoTypeNameParse *subinfo = g_ptr_array_index (info->type_arguments, i);

			type_args [i] = _mono_reflection_get_type_from_info (subinfo, rootimage, ignorecase);
			if (!type_args [i]) {
				g_free (type_args);
				return NULL;
			}
		}

		the_type = mono_type_get_object (mono_domain_get (), &klass->byval_arg);

		instance = mono_reflection_bind_generic_parameters (
			the_type, info->type_arguments->len, type_args);

		g_free (type_args);
		if (!instance)
			return NULL;

		klass = mono_class_from_mono_type (instance);
	}

	for (mod = info->modifiers; mod; mod = mod->next) {
		modval = GPOINTER_TO_UINT (mod->data);
		if (!modval) { /* byref: must be last modifier */
			return &klass->this_arg;
		} else if (modval == -1) {
			klass = mono_ptr_class_get (&klass->byval_arg);
		} else if (modval == -2) {
			bounded = TRUE;
		} else { /* array rank */
			klass = mono_bounded_array_class_get (klass, modval, bounded);
		}
	}

	return &klass->byval_arg;
}

/*
 * mono_reflection_get_type:
 * @image: a metadata context
 * @info: type description structure
 * @ignorecase: flag for case-insensitive string compares
 * @type_resolve: whenever type resolve was already tried
 *
 * Build a MonoType from the type description in @info.
 * 
 */

MonoType*
mono_reflection_get_type (MonoImage* image, MonoTypeNameParse *info, gboolean ignorecase, gboolean *type_resolve) {
	return mono_reflection_get_type_with_rootimage(image, image, info, ignorecase, type_resolve);
}

static MonoType*
mono_reflection_get_type_internal_dynamic (MonoImage *rootimage, MonoAssembly *assembly, MonoTypeNameParse *info, gboolean ignorecase)
{
	MonoReflectionAssemblyBuilder *abuilder;
	MonoType *type;
	int i;

	g_assert (assembly->dynamic);
	abuilder = (MonoReflectionAssemblyBuilder*)mono_assembly_get_object (((MonoDynamicAssembly*)assembly)->domain, assembly);

	/* Enumerate all modules */

	type = NULL;
	if (abuilder->modules) {
		for (i = 0; i < mono_array_length (abuilder->modules); ++i) {
			MonoReflectionModuleBuilder *mb = mono_array_get (abuilder->modules, MonoReflectionModuleBuilder*, i);
			type = mono_reflection_get_type_internal (rootimage, &mb->dynamic_image->image, info, ignorecase);
			if (type)
				break;
		}
	}

	if (!type && abuilder->loaded_modules) {
		for (i = 0; i < mono_array_length (abuilder->loaded_modules); ++i) {
			MonoReflectionModule *mod = mono_array_get (abuilder->loaded_modules, MonoReflectionModule*, i);
			type = mono_reflection_get_type_internal (rootimage, mod->image, info, ignorecase);
			if (type)
				break;
		}
	}

	return type;
}
	
MonoType*
mono_reflection_get_type_with_rootimage (MonoImage *rootimage, MonoImage* image, MonoTypeNameParse *info, gboolean ignorecase, gboolean *type_resolve)
{
	MonoType *type;
	MonoReflectionAssembly *assembly;
	GString *fullName;
	GList *mod;

	if (image && image->dynamic)
		type = mono_reflection_get_type_internal_dynamic (rootimage, image->assembly, info, ignorecase);
	else
		type = mono_reflection_get_type_internal (rootimage, image, info, ignorecase);
	if (type)
		return type;
	if (!mono_domain_has_type_resolve (mono_domain_get ()))
		return NULL;

	if (type_resolve) {
		if (*type_resolve) 
			return NULL;
		else
			*type_resolve = TRUE;
	}
	
	/* Reconstruct the type name */
	fullName = g_string_new ("");
	if (info->name_space && (info->name_space [0] != '\0'))
		g_string_printf (fullName, "%s.%s", info->name_space, info->name);
	else
		g_string_printf (fullName, "%s", info->name);
	for (mod = info->nested; mod; mod = mod->next)
		g_string_append_printf (fullName, "+%s", (char*)mod->data);

	assembly = mono_domain_try_type_resolve ( mono_domain_get (), fullName->str, NULL);
	if (assembly) {
		if (assembly->assembly->dynamic)
			type = mono_reflection_get_type_internal_dynamic (rootimage, assembly->assembly, info, ignorecase);
		else
			type = mono_reflection_get_type_internal (rootimage, assembly->assembly->image, 
													  info, ignorecase);
	}
	g_string_free (fullName, TRUE);
	return type;
}

void
mono_reflection_free_type_info (MonoTypeNameParse *info)
{
	g_list_free (info->modifiers);
	g_list_free (info->nested);

	if (info->type_arguments) {
		int i;

		for (i = 0; i < info->type_arguments->len; i++) {
			MonoTypeNameParse *subinfo = g_ptr_array_index (info->type_arguments, i);

			mono_reflection_free_type_info (subinfo);
			/*We free the subinfo since it is allocated by _mono_reflection_parse_type*/
			g_free (subinfo);
		}

		g_ptr_array_free (info->type_arguments, TRUE);
	}
}

/*
 * mono_reflection_type_from_name:
 * @name: type name.
 * @image: a metadata context (can be NULL).
 *
 * Retrieves a MonoType from its @name. If the name is not fully qualified,
 * it defaults to get the type from @image or, if @image is NULL or loading
 * from it fails, uses corlib.
 * 
 */
MonoType*
mono_reflection_type_from_name (char *name, MonoImage *image)
{
	MonoType *type = NULL;
	MonoTypeNameParse info;
	char *tmp;

	/* Make a copy since parse_type modifies its argument */
	tmp = g_strdup (name);
	
	/*g_print ("requested type %s\n", str);*/
	if (mono_reflection_parse_type (tmp, &info)) {
		type = _mono_reflection_get_type_from_info (&info, image, FALSE);
	}

	g_free (tmp);
	mono_reflection_free_type_info (&info);
	return type;
}

/*
 * mono_reflection_get_token:
 *
 *   Return the metadata token of OBJ which should be an object
 * representing a metadata element.
 */
guint32
mono_reflection_get_token (MonoObject *obj)
{
	MonoClass *klass;
	guint32 token = 0;

	klass = obj->vtable->klass;

	if (strcmp (klass->name, "MethodBuilder") == 0) {
		MonoReflectionMethodBuilder *mb = (MonoReflectionMethodBuilder *)obj;

		token = mb->table_idx | MONO_TOKEN_METHOD_DEF;
	} else if (strcmp (klass->name, "ConstructorBuilder") == 0) {
		MonoReflectionCtorBuilder *mb = (MonoReflectionCtorBuilder *)obj;

		token = mb->table_idx | MONO_TOKEN_METHOD_DEF;
	} else if (strcmp (klass->name, "FieldBuilder") == 0) {
		MonoReflectionFieldBuilder *fb = (MonoReflectionFieldBuilder *)obj;

		token = fb->table_idx | MONO_TOKEN_FIELD_DEF;
	} else if (strcmp (klass->name, "TypeBuilder") == 0) {
		MonoReflectionTypeBuilder *tb = (MonoReflectionTypeBuilder *)obj;
		token = tb->table_idx | MONO_TOKEN_TYPE_DEF;
	} else if (strcmp (klass->name, "MonoType") == 0) {
		MonoType *type = mono_reflection_type_get_handle ((MonoReflectionType*)obj);
		MonoClass *mc = mono_class_from_mono_type (type);
		if (!mono_class_init (mc))
			mono_raise_exception (mono_class_get_exception_for_failure (mc));

		token = mc->type_token;
	} else if (strcmp (klass->name, "MonoCMethod") == 0 ||
		   strcmp (klass->name, "MonoMethod") == 0 ||
		   strcmp (klass->name, "MonoGenericMethod") == 0 ||
		   strcmp (klass->name, "MonoGenericCMethod") == 0) {
		MonoReflectionMethod *m = (MonoReflectionMethod *)obj;
		if (m->method->is_inflated) {
			MonoMethodInflated *inflated = (MonoMethodInflated *) m->method;
			return inflated->declaring->token;
		} else {
			token = m->method->token;
		}
	} else if (strcmp (klass->name, "MonoField") == 0) {
		MonoReflectionField *f = (MonoReflectionField*)obj;

		if (is_field_on_inst (f->field)) {
			MonoDynamicGenericClass *dgclass = (MonoDynamicGenericClass*)f->field->parent->generic_class;
			int field_index = f->field - dgclass->fields;
			MonoObject *obj;

			g_assert (field_index >= 0 && field_index < dgclass->count_fields);
			obj = dgclass->field_objects [field_index];
			return mono_reflection_get_token (obj);
		}
		token = mono_class_get_field_token (f->field);
	} else if (strcmp (klass->name, "MonoProperty") == 0) {
		MonoReflectionProperty *p = (MonoReflectionProperty*)obj;

		token = mono_class_get_property_token (p->property);
	} else if (strcmp (klass->name, "MonoEvent") == 0) {
		MonoReflectionMonoEvent *p = (MonoReflectionMonoEvent*)obj;

		token = mono_class_get_event_token (p->event);
	} else if (strcmp (klass->name, "ParameterInfo") == 0) {
		MonoReflectionParameter *p = (MonoReflectionParameter*)obj;
		MonoClass *member_class = mono_object_class (p->MemberImpl);
		g_assert (mono_class_is_reflection_method_or_constructor (member_class));

		token = mono_method_get_param_token (((MonoReflectionMethod*)p->MemberImpl)->method, p->PositionImpl);
	} else if (strcmp (klass->name, "Module") == 0 || strcmp (klass->name, "MonoModule") == 0) {
		MonoReflectionModule *m = (MonoReflectionModule*)obj;

		token = m->token;
	} else if (strcmp (klass->name, "Assembly") == 0 || strcmp (klass->name, "MonoAssembly") == 0) {
		token = mono_metadata_make_token (MONO_TABLE_ASSEMBLY, 1);
	} else {
		gchar *msg = g_strdup_printf ("MetadataToken is not supported for type '%s.%s'", klass->name_space, klass->name);
		MonoException *ex = mono_get_exception_not_implemented (msg);
		g_free (msg);
		mono_raise_exception (ex);
	}

	return token;
}

static void*
load_cattr_value (MonoImage *image, MonoType *t, const char *p, const char **end)
{
	int slen, type = t->type;
	MonoClass *tklass = t->data.klass;

handle_enum:
	switch (type) {
	case MONO_TYPE_U1:
	case MONO_TYPE_I1:
	case MONO_TYPE_BOOLEAN: {
		MonoBoolean *bval = g_malloc (sizeof (MonoBoolean));
		*bval = *p;
		*end = p + 1;
		return bval;
	}
	case MONO_TYPE_CHAR:
	case MONO_TYPE_U2:
	case MONO_TYPE_I2: {
		guint16 *val = g_malloc (sizeof (guint16));
		*val = read16 (p);
		*end = p + 2;
		return val;
	}
#if SIZEOF_VOID_P == 4
	case MONO_TYPE_U:
	case MONO_TYPE_I:
#endif
	case MONO_TYPE_R4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I4: {
		guint32 *val = g_malloc (sizeof (guint32));
		*val = read32 (p);
		*end = p + 4;
		return val;
	}
#if SIZEOF_VOID_P == 8
	case MONO_TYPE_U: /* error out instead? this should probably not happen */
	case MONO_TYPE_I:
#endif
	case MONO_TYPE_U8:
	case MONO_TYPE_I8: {
		guint64 *val = g_malloc (sizeof (guint64));
		*val = read64 (p);
		*end = p + 8;
		return val;
	}
	case MONO_TYPE_R8: {
		double *val = g_malloc (sizeof (double));
		readr8 (p, val);
		*end = p + 8;
		return val;
	}
	case MONO_TYPE_VALUETYPE:
		if (t->data.klass->enumtype) {
			type = mono_class_enum_basetype (t->data.klass)->type;
			goto handle_enum;
		} else {
			MonoClass *k =  t->data.klass;
			
			if (mono_is_corlib_image (k->image) && strcmp (k->name_space, "System") == 0 && strcmp (k->name, "DateTime") == 0){
				guint64 *val = g_malloc (sizeof (guint64));
				*val = read64 (p);
				*end = p + 8;
				return val;
			}
		}
		g_error ("generic valutype %s not handled in custom attr value decoding", t->data.klass->name);
		break;
		
	case MONO_TYPE_STRING:
		if (*p == (char)0xFF) {
			*end = p + 1;
			return NULL;
		}
		slen = mono_metadata_decode_value (p, &p);
		*end = p + slen;
		return mono_string_new_len (mono_domain_get (), p, slen);
	case MONO_TYPE_CLASS: {
		char *n;
		MonoType *t;
		if (*p == (char)0xFF) {
			*end = p + 1;
			return NULL;
		}
handle_type:
		slen = mono_metadata_decode_value (p, &p);
		n = g_memdup (p, slen + 1);
		n [slen] = 0;
		t = mono_reflection_type_from_name (n, image);
		if (!t)
			g_warning ("Cannot load type '%s'", n);
		g_free (n);
		*end = p + slen;
		if (t)
			return mono_type_get_object (mono_domain_get (), t);
		else
			return NULL;
	}
	case MONO_TYPE_OBJECT: {
		char subt = *p++;
		MonoObject *obj;
		MonoClass *subc = NULL;
		void *val;

		if (subt == 0x50) {
			goto handle_type;
		} else if (subt == 0x0E) {
			type = MONO_TYPE_STRING;
			goto handle_enum;
		} else if (subt == 0x1D) {
			MonoType simple_type = {{0}};
			int etype = *p;
			p ++;

			type = MONO_TYPE_SZARRAY;
			if (etype == 0x50) {
				tklass = mono_defaults.systemtype_class;
			} else {
				if (etype == 0x51)
					/* See Partition II, Appendix B3 */
					etype = MONO_TYPE_OBJECT;
				simple_type.type = etype;
				tklass = mono_class_from_mono_type (&simple_type);
			}
			goto handle_enum;
		} else if (subt == 0x55) {
			char *n;
			MonoType *t;
			slen = mono_metadata_decode_value (p, &p);
			n = g_memdup (p, slen + 1);
			n [slen] = 0;
			t = mono_reflection_type_from_name (n, image);
			if (!t)
				g_error ("Cannot load type '%s'", n);
			g_free (n);
			p += slen;
			subc = mono_class_from_mono_type (t);
		} else if (subt >= MONO_TYPE_BOOLEAN && subt <= MONO_TYPE_R8) {
			MonoType simple_type = {{0}};
			simple_type.type = subt;
			subc = mono_class_from_mono_type (&simple_type);
		} else {
			g_error ("Unknown type 0x%02x for object type encoding in custom attr", subt);
		}
		val = load_cattr_value (image, &subc->byval_arg, p, end);
		obj = mono_object_new (mono_domain_get (), subc);
		g_assert (!subc->has_references);
		mono_gc_memmove ((char*)obj + sizeof (MonoObject), val, mono_class_value_size (subc, NULL));
		g_free (val);
		return obj;
	}
	case MONO_TYPE_SZARRAY: {
		MonoArray *arr;
		guint32 i, alen, basetype;
		alen = read32 (p);
		p += 4;
		if (alen == 0xffffffff) {
			*end = p;
			return NULL;
		}
		arr = mono_array_new (mono_domain_get(), tklass, alen);
		basetype = tklass->byval_arg.type;
		if (basetype == MONO_TYPE_VALUETYPE && tklass->enumtype)
			basetype = mono_class_enum_basetype (tklass)->type;
		switch (basetype)
		{
			case MONO_TYPE_U1:
			case MONO_TYPE_I1:
			case MONO_TYPE_BOOLEAN:
				for (i = 0; i < alen; i++) {
					MonoBoolean val = *p++;
					mono_array_set (arr, MonoBoolean, i, val);
				}
				break;
			case MONO_TYPE_CHAR:
			case MONO_TYPE_U2:
			case MONO_TYPE_I2:
				for (i = 0; i < alen; i++) {
					guint16 val = read16 (p);
					mono_array_set (arr, guint16, i, val);
					p += 2;
				}
				break;
			case MONO_TYPE_R4:
			case MONO_TYPE_U4:
			case MONO_TYPE_I4:
				for (i = 0; i < alen; i++) {
					guint32 val = read32 (p);
					mono_array_set (arr, guint32, i, val);
					p += 4;
				}
				break;
			case MONO_TYPE_R8:
				for (i = 0; i < alen; i++) {
					double val;
					readr8 (p, &val);
					mono_array_set (arr, double, i, val);
					p += 8;
				}
				break;
			case MONO_TYPE_U8:
			case MONO_TYPE_I8:
				for (i = 0; i < alen; i++) {
					guint64 val = read64 (p);
					mono_array_set (arr, guint64, i, val);
					p += 8;
				}
				break;
			case MONO_TYPE_CLASS:
			case MONO_TYPE_OBJECT:
			case MONO_TYPE_STRING:
				for (i = 0; i < alen; i++) {
					MonoObject *item = load_cattr_value (image, &tklass->byval_arg, p, &p);
					mono_array_setref (arr, i, item);
				}
				break;
			default:
				g_error ("Type 0x%02x not handled in custom attr array decoding", basetype);
		}
		*end=p;
		return arr;
	}
	default:
		g_error ("Type 0x%02x not handled in custom attr value decoding", type);
	}
	return NULL;
}

static MonoObject*
create_cattr_typed_arg (MonoType *t, MonoObject *val)
{
	static MonoClass *klass;
	static MonoMethod *ctor;
	MonoObject *retval;
	void *params [2], *unboxed;

	if (!klass)
		klass = mono_class_from_name (mono_defaults.corlib, "System.Reflection", "CustomAttributeTypedArgument");
	if (!ctor)
		ctor = mono_class_get_method_from_name (klass, ".ctor", 2);
	
	params [0] = mono_type_get_object (mono_domain_get (), t);
	params [1] = val;
	retval = mono_object_new (mono_domain_get (), klass);
	unboxed = mono_object_unbox (retval);
	mono_runtime_invoke (ctor, unboxed, params, NULL);

	return retval;
}

static MonoObject*
create_cattr_named_arg (void *minfo, MonoObject *typedarg)
{
	static MonoClass *klass;
	static MonoMethod *ctor;
	MonoObject *retval;
	void *unboxed, *params [2];

	if (!klass)
		klass = mono_class_from_name (mono_defaults.corlib, "System.Reflection", "CustomAttributeNamedArgument");
	if (!ctor)
		ctor = mono_class_get_method_from_name (klass, ".ctor", 2);

	params [0] = minfo;
	params [1] = typedarg;
	retval = mono_object_new (mono_domain_get (), klass);
	unboxed = mono_object_unbox (retval);
	mono_runtime_invoke (ctor, unboxed, params, NULL);

	return retval;
}

static gboolean
type_is_reference (MonoType *type)
{
	switch (type->type) {
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_U:
	case MONO_TYPE_I:
	case MONO_TYPE_U1:
	case MONO_TYPE_I1:
	case MONO_TYPE_U2:
	case MONO_TYPE_I2:
	case MONO_TYPE_U4:
	case MONO_TYPE_I4:
	case MONO_TYPE_U8:
	case MONO_TYPE_I8:
	case MONO_TYPE_R8:
	case MONO_TYPE_R4:
	case MONO_TYPE_VALUETYPE:
		return FALSE;
	default:
		return TRUE;
	}
}

static void
free_param_data (MonoMethodSignature *sig, void **params) {
	int i;
	for (i = 0; i < sig->param_count; ++i) {
		if (!type_is_reference (sig->params [i]))
			g_free (params [i]);
	}
}

/*
 * Find the field index in the metadata FieldDef table.
 */
static guint32
find_field_index (MonoClass *klass, MonoClassField *field) {
	int i;

	for (i = 0; i < klass->field.count; ++i) {
		if (field == &klass->fields [i])
			return klass->field.first + 1 + i;
	}
	return 0;
}

/*
 * Find the property index in the metadata Property table.
 */
static guint32
find_property_index (MonoClass *klass, MonoProperty *property) {
	int i;

	for (i = 0; i < klass->ext->property.count; ++i) {
		if (property == &klass->ext->properties [i])
			return klass->ext->property.first + 1 + i;
	}
	return 0;
}

/*
 * Find the event index in the metadata Event table.
 */
static guint32
find_event_index (MonoClass *klass, MonoEvent *event) {
	int i;

	for (i = 0; i < klass->ext->event.count; ++i) {
		if (event == &klass->ext->events [i])
			return klass->ext->event.first + 1 + i;
	}
	return 0;
}

static MonoObject*
create_custom_attr (MonoImage *image, MonoMethod *method, const guchar *data, guint32 len, MonoError *error)
{
	const char *p = (const char*)data;
	const char *named;
	guint32 i, j, num_named;
	MonoObject *attr;
	void *params_buf [32];
	void **params;
	MonoMethodSignature *sig;

	mono_error_init (error);

	mono_class_init (method->klass);

	if (!mono_verifier_verify_cattr_content (image, method, data, len, NULL)) {
		mono_error_set_generic_error (error, "System.Reflection", "CustomAttributeFormatException", "Binary format of the specified custom attribute was invalid.");
		return NULL;
	}

	if (len == 0) {
		attr = mono_object_new (mono_domain_get (), method->klass);
		mono_runtime_invoke (method, attr, NULL, NULL);
		return attr;
	}

	if (len < 2 || read16 (p) != 0x0001) /* Prolog */
		return NULL;

	/*g_print ("got attr %s\n", method->klass->name);*/

	sig = mono_method_signature (method);
	if (sig->param_count < 32)
		params = params_buf;
	else
		/* Allocate using GC so it gets GC tracking */
		params = mono_gc_alloc_fixed (sig->param_count * sizeof (void*), NULL);

	/* skip prolog */
	p += 2;
	for (i = 0; i < mono_method_signature (method)->param_count; ++i) {
		params [i] = load_cattr_value (image, mono_method_signature (method)->params [i], p, &p);
	}

	named = p;
	attr = mono_object_new (mono_domain_get (), method->klass);
	mono_runtime_invoke (method, attr, params, NULL);
	free_param_data (method->signature, params);
	num_named = read16 (named);
	named += 2;
	for (j = 0; j < num_named; j++) {
		gint name_len;
		char *name, named_type, data_type;
		named_type = *named++;
		data_type = *named++; /* type of data */
		if (data_type == MONO_TYPE_SZARRAY)
			data_type = *named++;
		if (data_type == MONO_TYPE_ENUM) {
			gint type_len;
			char *type_name;
			type_len = mono_metadata_decode_blob_size (named, &named);
			type_name = g_malloc (type_len + 1);
			memcpy (type_name, named, type_len);
			type_name [type_len] = 0;
			named += type_len;
			/* FIXME: lookup the type and check type consistency */
			g_free (type_name);
		}
		name_len = mono_metadata_decode_blob_size (named, &named);
		name = g_malloc (name_len + 1);
		memcpy (name, named, name_len);
		name [name_len] = 0;
		named += name_len;
		if (named_type == 0x53) {
			MonoClassField *field = mono_class_get_field_from_name (mono_object_class (attr), name);
			void *val = load_cattr_value (image, field->type, named, &named);
			mono_field_set_value (attr, field, val);
			if (!type_is_reference (field->type))
				g_free (val);
		} else if (named_type == 0x54) {
			MonoProperty *prop;
			void *pparams [1];
			MonoType *prop_type;

			prop = mono_class_get_property_from_name (mono_object_class (attr), name);
			/* can we have more that 1 arg in a custom attr named property? */
			prop_type = prop->get? mono_method_signature (prop->get)->ret :
			     mono_method_signature (prop->set)->params [mono_method_signature (prop->set)->param_count - 1];
			pparams [0] = load_cattr_value (image, prop_type, named, &named);
			mono_property_set_value (prop, attr, pparams, NULL);
			if (!type_is_reference (prop_type))
				g_free (pparams [0]);
		}
		g_free (name);
	}

	if (params != params_buf)
		mono_gc_free_fixed (params);

	return attr;
}
	
/*
 * mono_reflection_create_custom_attr_data_args:
 *
 *   Create an array of typed and named arguments from the cattr blob given by DATA.
 * TYPED_ARGS and NAMED_ARGS will contain the objects representing the arguments,
 * NAMED_ARG_INFO will contain information about the named arguments.
 */
void
mono_reflection_create_custom_attr_data_args (MonoImage *image, MonoMethod *method, const guchar *data, guint32 len, MonoArray **typed_args, MonoArray **named_args, CattrNamedArg **named_arg_info)
{
	MonoArray *typedargs, *namedargs;
	MonoClass *attrklass;
	MonoDomain *domain;
	const char *p = (const char*)data;
	const char *named;
	guint32 i, j, num_named;
	CattrNamedArg *arginfo = NULL;

	if (!mono_verifier_verify_cattr_content (image, method, data, len, NULL))
		return;

	mono_class_init (method->klass);

	*typed_args = NULL;
	*named_args = NULL;
	*named_arg_info = NULL;
	
	domain = mono_domain_get ();

	if (len < 2 || read16 (p) != 0x0001) /* Prolog */
		return;

	typedargs = mono_array_new (domain, mono_get_object_class (), mono_method_signature (method)->param_count);
	
	/* skip prolog */
	p += 2;
	for (i = 0; i < mono_method_signature (method)->param_count; ++i) {
		MonoObject *obj;
		void *val;

		val = load_cattr_value (image, mono_method_signature (method)->params [i], p, &p);
		obj = type_is_reference (mono_method_signature (method)->params [i]) ? 
			val : mono_value_box (domain, mono_class_from_mono_type (mono_method_signature (method)->params [i]), val);
		mono_array_setref (typedargs, i, obj);

		if (!type_is_reference (mono_method_signature (method)->params [i]))
			g_free (val);
	}

	named = p;
	num_named = read16 (named);
	namedargs = mono_array_new (domain, mono_get_object_class (), num_named);
	named += 2;
	attrklass = method->klass;

	arginfo = g_new0 (CattrNamedArg, num_named);
	*named_arg_info = arginfo;

	for (j = 0; j < num_named; j++) {
		gint name_len;
		char *name, named_type, data_type;
		named_type = *named++;
		data_type = *named++; /* type of data */
		if (data_type == MONO_TYPE_SZARRAY)
			data_type = *named++;
		if (data_type == MONO_TYPE_ENUM) {
			gint type_len;
			char *type_name;
			type_len = mono_metadata_decode_blob_size (named, &named);
			type_name = g_malloc (type_len + 1);
			memcpy (type_name, named, type_len);
			type_name [type_len] = 0;
			named += type_len;
			/* FIXME: lookup the type and check type consistency */
			g_free (type_name);
		}
		name_len = mono_metadata_decode_blob_size (named, &named);
		name = g_malloc (name_len + 1);
		memcpy (name, named, name_len);
		name [name_len] = 0;
		named += name_len;
		if (named_type == 0x53) {
			MonoObject *obj;
			MonoClassField *field = mono_class_get_field_from_name (attrklass, name);
			void *val;

			arginfo [j].type = field->type;
			arginfo [j].field = field;

			val = load_cattr_value (image, field->type, named, &named);
			obj = type_is_reference (field->type) ? val : mono_value_box (domain, mono_class_from_mono_type (field->type), val);
			mono_array_setref (namedargs, j, obj);
			if (!type_is_reference (field->type))
				g_free (val);
		} else if (named_type == 0x54) {
			MonoObject *obj;
			MonoType *prop_type;
			MonoProperty *prop = mono_class_get_property_from_name (attrklass, name);
			void *val;

			prop_type = prop->get? mono_method_signature (prop->get)->ret :
			     mono_method_signature (prop->set)->params [mono_method_signature (prop->set)->param_count - 1];

			arginfo [j].type = prop_type;
			arginfo [j].prop = prop;

			val = load_cattr_value (image, prop_type, named, &named);
			obj = type_is_reference (prop_type) ? val : mono_value_box (domain, mono_class_from_mono_type (prop_type), val);
			mono_array_setref (namedargs, j, obj);
			if (!type_is_reference (prop_type))
				g_free (val);
		}
		g_free (name);
	}

	*typed_args = typedargs;
	*named_args = namedargs;
}

void
mono_reflection_resolve_custom_attribute_data (MonoReflectionMethod *ref_method, MonoReflectionAssembly *assembly, gpointer data, guint32 len, MonoArray **ctor_args, MonoArray **named_args)
{
	MonoDomain *domain;
	MonoArray *typedargs, *namedargs;
	MonoImage *image;
	MonoMethod *method;
	CattrNamedArg *arginfo;
	int i;

	*ctor_args = NULL;
	*named_args = NULL;

	if (len == 0)
		return;

	image = assembly->assembly->image;
	method = ref_method->method;
	domain = mono_object_domain (ref_method);

	if (!mono_class_init (method->klass))
		mono_raise_exception (mono_class_get_exception_for_failure (method->klass));

	mono_reflection_create_custom_attr_data_args (image, method, data, len, &typedargs, &namedargs, &arginfo);
	if (mono_loader_get_last_error ())
		mono_raise_exception (mono_loader_error_prepare_exception (mono_loader_get_last_error ()));

	if (!typedargs || !namedargs)
		return;

	for (i = 0; i < mono_method_signature (method)->param_count; ++i) {
		MonoObject *obj = mono_array_get (typedargs, MonoObject*, i);
		MonoObject *typedarg;

		typedarg = create_cattr_typed_arg (mono_method_signature (method)->params [i], obj);
		mono_array_setref (typedargs, i, typedarg);
	}

	for (i = 0; i < mono_array_length (namedargs); ++i) {
		MonoObject *obj = mono_array_get (namedargs, MonoObject*, i);
		MonoObject *typedarg, *namedarg, *minfo;

		if (arginfo [i].prop)
			minfo = (MonoObject*)mono_property_get_object (domain, NULL, arginfo [i].prop);
		else
			minfo = (MonoObject*)mono_field_get_object (domain, NULL, arginfo [i].field);

		typedarg = create_cattr_typed_arg (arginfo [i].type, obj);
		namedarg = create_cattr_named_arg (minfo, typedarg);

		mono_array_setref (namedargs, i, namedarg);
	}

	*ctor_args = typedargs;
	*named_args = namedargs;
}

static MonoObject*
create_custom_attr_data (MonoImage *image, MonoCustomAttrEntry *cattr)
{
	static MonoMethod *ctor;
	MonoDomain *domain;
	MonoObject *attr;
	void *params [4];

	g_assert (image->assembly);

	if (!ctor)
		ctor = mono_class_get_method_from_name (mono_defaults.customattribute_data_class, ".ctor", 4);

	domain = mono_domain_get ();
	attr = mono_object_new (domain, mono_defaults.customattribute_data_class);
	params [0] = mono_method_get_object (domain, cattr->ctor, NULL);
	params [1] = mono_assembly_get_object (domain, image->assembly);
	params [2] = (gpointer)&cattr->data;
	params [3] = &cattr->data_size;
	mono_runtime_invoke (ctor, attr, params, NULL);
	return attr;
}

static MonoArray*
mono_custom_attrs_construct_by_type (MonoCustomAttrInfo *cinfo, MonoClass *attr_klass, MonoError *error)
{
	MonoArray *result;
	MonoObject *attr;
	int i, n;

	mono_error_init (error);

	n = 0;
	for (i = 0; i < cinfo->num_attrs; ++i) {
		if (!attr_klass || mono_class_is_assignable_from (attr_klass, cinfo->attrs [i].ctor->klass))
			n ++;
	}

	result = mono_array_new_cached (mono_domain_get (), mono_defaults.attribute_class, n);
	n = 0;
	for (i = 0; i < cinfo->num_attrs; ++i) {
		if (!cinfo->attrs [i].ctor)
			/* The cattr type is not finished yet */
			/* We should include the type name but cinfo doesn't contain it */
			mono_raise_exception (mono_get_exception_type_load (NULL, NULL));
		if (!attr_klass || mono_class_is_assignable_from (attr_klass, cinfo->attrs [i].ctor->klass)) {
			attr = create_custom_attr (cinfo->image, cinfo->attrs [i].ctor, cinfo->attrs [i].data, cinfo->attrs [i].data_size, error);
			if (!mono_error_ok (error))
				return result;
			mono_array_setref (result, n, attr);
			n ++;
		}
	}
	return result;
}

MonoArray*
mono_custom_attrs_construct (MonoCustomAttrInfo *cinfo)
{
	MonoError error;

	return mono_custom_attrs_construct_by_type (cinfo, NULL, &error);
}

static MonoArray*
mono_custom_attrs_data_construct (MonoCustomAttrInfo *cinfo)
{
	MonoArray *result;
	MonoObject *attr;
	int i;
	
	result = mono_array_new (mono_domain_get (), mono_defaults.customattribute_data_class, cinfo->num_attrs);
	for (i = 0; i < cinfo->num_attrs; ++i) {
		attr = create_custom_attr_data (cinfo->image, &cinfo->attrs [i]);
		mono_array_setref (result, i, attr);
	}
	return result;
}

/**
 * mono_custom_attrs_from_index:
 *
 * Returns: NULL if no attributes are found or if a loading error occurs.
 */
MonoCustomAttrInfo*
mono_custom_attrs_from_index (MonoImage *image, guint32 idx)
{
	guint32 mtoken, i, len;
	guint32 cols [MONO_CUSTOM_ATTR_SIZE];
	MonoTableInfo *ca;
	MonoCustomAttrInfo *ainfo;
	GList *tmp, *list = NULL;
	const char *data;

	ca = &image->tables [MONO_TABLE_CUSTOMATTRIBUTE];

	i = mono_metadata_custom_attrs_from_index (image, idx);
	if (!i)
		return NULL;
	i --;
	while (i < ca->rows) {
		if (mono_metadata_decode_row_col (ca, i, MONO_CUSTOM_ATTR_PARENT) != idx)
			break;
		list = g_list_prepend (list, GUINT_TO_POINTER (i));
		++i;
	}
	len = g_list_length (list);
	if (!len)
		return NULL;
	ainfo = g_malloc0 (MONO_SIZEOF_CUSTOM_ATTR_INFO + sizeof (MonoCustomAttrEntry) * len);
	ainfo->num_attrs = len;
	ainfo->image = image;
	for (i = 0, tmp = list; i < len; ++i, tmp = tmp->next) {
		mono_metadata_decode_row (ca, GPOINTER_TO_UINT (tmp->data), cols, MONO_CUSTOM_ATTR_SIZE);
		mtoken = cols [MONO_CUSTOM_ATTR_TYPE] >> MONO_CUSTOM_ATTR_TYPE_BITS;
		switch (cols [MONO_CUSTOM_ATTR_TYPE] & MONO_CUSTOM_ATTR_TYPE_MASK) {
		case MONO_CUSTOM_ATTR_TYPE_METHODDEF:
			mtoken |= MONO_TOKEN_METHOD_DEF;
			break;
		case MONO_CUSTOM_ATTR_TYPE_MEMBERREF:
			mtoken |= MONO_TOKEN_MEMBER_REF;
			break;
		default:
			g_error ("Unknown table for custom attr type %08x", cols [MONO_CUSTOM_ATTR_TYPE]);
			break;
		}
		ainfo->attrs [i].ctor = mono_get_method (image, mtoken, NULL);
		if (!ainfo->attrs [i].ctor) {
			g_warning ("Can't find custom attr constructor image: %s mtoken: 0x%08x", image->name, mtoken);
			g_list_free (list);
			g_free (ainfo);
			return NULL;
		}

		if (!mono_verifier_verify_cattr_blob (image, cols [MONO_CUSTOM_ATTR_VALUE], NULL)) {
			/*FIXME raising an exception here doesn't make any sense*/
			g_warning ("Invalid custom attribute blob on image %s for index %x", image->name, idx);
			g_list_free (list);
			g_free (ainfo);
			return NULL;
		}
		data = mono_metadata_blob_heap (image, cols [MONO_CUSTOM_ATTR_VALUE]);
		ainfo->attrs [i].data_size = mono_metadata_decode_value (data, &data);
		ainfo->attrs [i].data = (guchar*)data;
	}
	g_list_free (list);

	return ainfo;
}

MonoCustomAttrInfo*
mono_custom_attrs_from_method (MonoMethod *method)
{
	guint32 idx;

	/*
	 * An instantiated method has the same cattrs as the generic method definition.
	 *
	 * LAMESPEC: The .NET SRE throws an exception for instantiations of generic method builders
	 *           Note that this stanza is not necessary for non-SRE types, but it's a micro-optimization
	 */
	if (method->is_inflated)
		method = ((MonoMethodInflated *) method)->declaring;
	
	if (method->dynamic || method->klass->image->dynamic)
		return lookup_custom_attr (method->klass->image, method);

	if (!method->token)
		/* Synthetic methods */
		return NULL;

	idx = mono_method_get_index (method);
	idx <<= MONO_CUSTOM_ATTR_BITS;
	idx |= MONO_CUSTOM_ATTR_METHODDEF;
	return mono_custom_attrs_from_index (method->klass->image, idx);
}

MonoCustomAttrInfo*
mono_custom_attrs_from_class (MonoClass *klass)
{
	guint32 idx;

	if (klass->generic_class)
		klass = klass->generic_class->container_class;

	if (klass->image->dynamic)
		return lookup_custom_attr (klass->image, klass);

	if (klass->byval_arg.type == MONO_TYPE_VAR || klass->byval_arg.type == MONO_TYPE_MVAR) {
		idx = mono_metadata_token_index (klass->sizes.generic_param_token);
		idx <<= MONO_CUSTOM_ATTR_BITS;
		idx |= MONO_CUSTOM_ATTR_GENERICPAR;
	} else {
		idx = mono_metadata_token_index (klass->type_token);
		idx <<= MONO_CUSTOM_ATTR_BITS;
		idx |= MONO_CUSTOM_ATTR_TYPEDEF;
	}
	return mono_custom_attrs_from_index (klass->image, idx);
}

MonoCustomAttrInfo*
mono_custom_attrs_from_assembly (MonoAssembly *assembly)
{
	guint32 idx;
	
	if (assembly->image->dynamic)
		return lookup_custom_attr (assembly->image, assembly);
	idx = 1; /* there is only one assembly */
	idx <<= MONO_CUSTOM_ATTR_BITS;
	idx |= MONO_CUSTOM_ATTR_ASSEMBLY;
	return mono_custom_attrs_from_index (assembly->image, idx);
}

static MonoCustomAttrInfo*
mono_custom_attrs_from_module (MonoImage *image)
{
	guint32 idx;
	
	if (image->dynamic)
		return lookup_custom_attr (image, image);
	idx = 1; /* there is only one module */
	idx <<= MONO_CUSTOM_ATTR_BITS;
	idx |= MONO_CUSTOM_ATTR_MODULE;
	return mono_custom_attrs_from_index (image, idx);
}

MonoCustomAttrInfo*
mono_custom_attrs_from_property (MonoClass *klass, MonoProperty *property)
{
	guint32 idx;
	
	if (klass->image->dynamic) {
		property = mono_metadata_get_corresponding_property_from_generic_type_definition (property);
		return lookup_custom_attr (klass->image, property);
	}
	idx = find_property_index (klass, property);
	idx <<= MONO_CUSTOM_ATTR_BITS;
	idx |= MONO_CUSTOM_ATTR_PROPERTY;
	return mono_custom_attrs_from_index (klass->image, idx);
}

MonoCustomAttrInfo*
mono_custom_attrs_from_event (MonoClass *klass, MonoEvent *event)
{
	guint32 idx;
	
	if (klass->image->dynamic) {
		event = mono_metadata_get_corresponding_event_from_generic_type_definition (event);
		return lookup_custom_attr (klass->image, event);
	}
	idx = find_event_index (klass, event);
	idx <<= MONO_CUSTOM_ATTR_BITS;
	idx |= MONO_CUSTOM_ATTR_EVENT;
	return mono_custom_attrs_from_index (klass->image, idx);
}

MonoCustomAttrInfo*
mono_custom_attrs_from_field (MonoClass *klass, MonoClassField *field)
{
	guint32 idx;
	if (klass->image->dynamic) {
		field = mono_metadata_get_corresponding_field_from_generic_type_definition (field);
		return lookup_custom_attr (klass->image, field);
	}
	idx = find_field_index (klass, field);
	idx <<= MONO_CUSTOM_ATTR_BITS;
	idx |= MONO_CUSTOM_ATTR_FIELDDEF;
	return mono_custom_attrs_from_index (klass->image, idx);
}

MonoCustomAttrInfo*
mono_custom_attrs_from_param (MonoMethod *method, guint32 param)
{
	MonoTableInfo *ca;
	guint32 i, idx, method_index;
	guint32 param_list, param_last, param_pos, found;
	MonoImage *image;
	MonoReflectionMethodAux *aux;

	/*
	 * An instantiated method has the same cattrs as the generic method definition.
	 *
	 * LAMESPEC: The .NET SRE throws an exception for instantiations of generic method builders
	 *           Note that this stanza is not necessary for non-SRE types, but it's a micro-optimization
	 */
	if (method->is_inflated)
		method = ((MonoMethodInflated *) method)->declaring;

	if (method->klass->image->dynamic) {
		MonoCustomAttrInfo *res, *ainfo;
		int size;

		aux = g_hash_table_lookup (((MonoDynamicImage*)method->klass->image)->method_aux_hash, method);
		if (!aux || !aux->param_cattr)
			return NULL;

		/* Need to copy since it will be freed later */
		ainfo = aux->param_cattr [param];
		if (!ainfo)
			return NULL;
		size = MONO_SIZEOF_CUSTOM_ATTR_INFO + sizeof (MonoCustomAttrEntry) * ainfo->num_attrs;
		res = g_malloc0 (size);
		memcpy (res, ainfo, size);
		return res;
	}

	image = method->klass->image;
	method_index = mono_method_get_index (method);
	if (!method_index)
		return NULL;
	ca = &image->tables [MONO_TABLE_METHOD];

	param_list = mono_metadata_decode_row_col (ca, method_index - 1, MONO_METHOD_PARAMLIST);
	if (method_index == ca->rows) {
		ca = &image->tables [MONO_TABLE_PARAM];
		param_last = ca->rows + 1;
	} else {
		param_last = mono_metadata_decode_row_col (ca, method_index, MONO_METHOD_PARAMLIST);
		ca = &image->tables [MONO_TABLE_PARAM];
	}
	found = FALSE;
	for (i = param_list; i < param_last; ++i) {
		param_pos = mono_metadata_decode_row_col (ca, i - 1, MONO_PARAM_SEQUENCE);
		if (param_pos == param) {
			found = TRUE;
			break;
		}
	}
	if (!found)
		return NULL;
	idx = i;
	idx <<= MONO_CUSTOM_ATTR_BITS;
	idx |= MONO_CUSTOM_ATTR_PARAMDEF;
	return mono_custom_attrs_from_index (image, idx);
}

gboolean
mono_custom_attrs_has_attr (MonoCustomAttrInfo *ainfo, MonoClass *attr_klass)
{
	int i;
	MonoClass *klass;
	for (i = 0; i < ainfo->num_attrs; ++i) {
		klass = ainfo->attrs [i].ctor->klass;
		if (mono_class_has_parent (klass, attr_klass) || (MONO_CLASS_IS_INTERFACE (attr_klass) && mono_class_is_assignable_from (attr_klass, klass)))
			return TRUE;
	}
	return FALSE;
}

MonoObject*
mono_custom_attrs_get_attr (MonoCustomAttrInfo *ainfo, MonoClass *attr_klass)
{
	int i, attr_index;
	MonoClass *klass;
	MonoArray *attrs;

	attr_index = -1;
	for (i = 0; i < ainfo->num_attrs; ++i) {
		klass = ainfo->attrs [i].ctor->klass;
		if (mono_class_has_parent (klass, attr_klass)) {
			attr_index = i;
			break;
		}
	}
	if (attr_index == -1)
		return NULL;

	attrs = mono_custom_attrs_construct (ainfo);
	if (attrs)
		return mono_array_get (attrs, MonoObject*, attr_index);
	else
		return NULL;
}

/*
 * mono_reflection_get_custom_attrs_info:
 * @obj: a reflection object handle
 *
 * Return the custom attribute info for attributes defined for the
 * reflection handle @obj. The objects.
 *
 * FIXME this function leaks like a sieve for SRE objects.
 */
MonoCustomAttrInfo*
mono_reflection_get_custom_attrs_info (MonoObject *obj)
{
	MonoClass *klass;
	MonoCustomAttrInfo *cinfo = NULL;
	
	klass = obj->vtable->klass;
	if (klass == mono_defaults.monotype_class) {
		MonoType *type = mono_reflection_type_get_handle ((MonoReflectionType *)obj);
		klass = mono_class_from_mono_type (type);
		/*We cannot mono_class_init the class from which we'll load the custom attributes since this must work with broken types.*/
		cinfo = mono_custom_attrs_from_class (klass);
	} else if (strcmp ("Assembly", klass->name) == 0 || strcmp ("MonoAssembly", klass->name) == 0) {
		MonoReflectionAssembly *rassembly = (MonoReflectionAssembly*)obj;
		cinfo = mono_custom_attrs_from_assembly (rassembly->assembly);
	} else if (strcmp ("Module", klass->name) == 0 || strcmp ("MonoModule", klass->name) == 0) {
		MonoReflectionModule *module = (MonoReflectionModule*)obj;
		cinfo = mono_custom_attrs_from_module (module->image);
	} else if (strcmp ("MonoProperty", klass->name) == 0) {
		MonoReflectionProperty *rprop = (MonoReflectionProperty*)obj;
		cinfo = mono_custom_attrs_from_property (rprop->property->parent, rprop->property);
	} else if (strcmp ("MonoEvent", klass->name) == 0) {
		MonoReflectionMonoEvent *revent = (MonoReflectionMonoEvent*)obj;
		cinfo = mono_custom_attrs_from_event (revent->event->parent, revent->event);
	} else if (strcmp ("MonoField", klass->name) == 0) {
		MonoReflectionField *rfield = (MonoReflectionField*)obj;
		cinfo = mono_custom_attrs_from_field (rfield->field->parent, rfield->field);
	} else if ((strcmp ("MonoMethod", klass->name) == 0) || (strcmp ("MonoCMethod", klass->name) == 0)) {
		MonoReflectionMethod *rmethod = (MonoReflectionMethod*)obj;
		cinfo = mono_custom_attrs_from_method (rmethod->method);
	} else if ((strcmp ("MonoGenericMethod", klass->name) == 0) || (strcmp ("MonoGenericCMethod", klass->name) == 0)) {
		MonoReflectionMethod *rmethod = (MonoReflectionMethod*)obj;
		cinfo = mono_custom_attrs_from_method (rmethod->method);
	} else if (strcmp ("ParameterInfo", klass->name) == 0) {
		MonoReflectionParameter *param = (MonoReflectionParameter*)obj;
		MonoClass *member_class = mono_object_class (param->MemberImpl);
		if (mono_class_is_reflection_method_or_constructor (member_class)) {
			MonoReflectionMethod *rmethod = (MonoReflectionMethod*)param->MemberImpl;
			cinfo = mono_custom_attrs_from_param (rmethod->method, param->PositionImpl + 1);
		} else if (is_sr_mono_property (member_class)) {
			MonoReflectionProperty *prop = (MonoReflectionProperty *)param->MemberImpl;
			MonoMethod *method;
			if (!(method = prop->property->get))
				method = prop->property->set;
			g_assert (method);

			cinfo = mono_custom_attrs_from_param (method, param->PositionImpl + 1);
		} 
#ifndef DISABLE_REFLECTION_EMIT
		else if (is_sre_method_on_tb_inst (member_class)) {/*XXX This is a workaround for Compiler Context*/
			MonoMethod *method = mono_reflection_method_on_tb_inst_get_handle ((MonoReflectionMethodOnTypeBuilderInst*)param->MemberImpl);
			cinfo = mono_custom_attrs_from_param (method, param->PositionImpl + 1);
		} else if (is_sre_ctor_on_tb_inst (member_class)) { /*XX This is a workaround for Compiler Context*/
			MonoReflectionCtorOnTypeBuilderInst *c = (MonoReflectionCtorOnTypeBuilderInst*)param->MemberImpl;
			MonoMethod *method = NULL;
			if (is_sre_ctor_builder (mono_object_class (c->cb)))
				method = ((MonoReflectionCtorBuilder *)c->cb)->mhandle;
			else if (is_sr_mono_cmethod (mono_object_class (c->cb)))
				method = ((MonoReflectionMethod *)c->cb)->method;
			else
				g_error ("mono_reflection_get_custom_attrs_info:: can't handle a CTBI with base_method of type %s", mono_type_get_full_name (member_class));

			cinfo = mono_custom_attrs_from_param (method, param->PositionImpl + 1);
		} 
#endif
		else {
			char *type_name = mono_type_get_full_name (member_class);
			char *msg = g_strdup_printf ("Custom attributes on a ParamInfo with member %s are not supported", type_name);
			MonoException *ex = mono_get_exception_not_supported  (msg);
			g_free (type_name);
			g_free (msg);
			mono_raise_exception (ex);
		}
	} else if (strcmp ("AssemblyBuilder", klass->name) == 0) {
		MonoReflectionAssemblyBuilder *assemblyb = (MonoReflectionAssemblyBuilder*)obj;
		cinfo = mono_custom_attrs_from_builders (NULL, assemblyb->assembly.assembly->image, assemblyb->cattrs);
	} else if (strcmp ("TypeBuilder", klass->name) == 0) {
		MonoReflectionTypeBuilder *tb = (MonoReflectionTypeBuilder*)obj;
		cinfo = mono_custom_attrs_from_builders (NULL, &tb->module->dynamic_image->image, tb->cattrs);
	} else if (strcmp ("ModuleBuilder", klass->name) == 0) {
		MonoReflectionModuleBuilder *mb = (MonoReflectionModuleBuilder*)obj;
		cinfo = mono_custom_attrs_from_builders (NULL, &mb->dynamic_image->image, mb->cattrs);
	} else if (strcmp ("ConstructorBuilder", klass->name) == 0) {
		MonoReflectionCtorBuilder *cb = (MonoReflectionCtorBuilder*)obj;
		cinfo = mono_custom_attrs_from_builders (NULL, cb->mhandle->klass->image, cb->cattrs);
	} else if (strcmp ("MethodBuilder", klass->name) == 0) {
		MonoReflectionMethodBuilder *mb = (MonoReflectionMethodBuilder*)obj;
		cinfo = mono_custom_attrs_from_builders (NULL, mb->mhandle->klass->image, mb->cattrs);
	} else if (strcmp ("FieldBuilder", klass->name) == 0) {
		MonoReflectionFieldBuilder *fb = (MonoReflectionFieldBuilder*)obj;
		cinfo = mono_custom_attrs_from_builders (NULL, &((MonoReflectionTypeBuilder*)fb->typeb)->module->dynamic_image->image, fb->cattrs);
	} else if (strcmp ("MonoGenericClass", klass->name) == 0) {
		MonoReflectionGenericClass *gclass = (MonoReflectionGenericClass*)obj;
		cinfo = mono_reflection_get_custom_attrs_info ((MonoObject*)gclass->generic_type);
	} else { /* handle other types here... */
		g_error ("get custom attrs not yet supported for %s", klass->name);
	}

	return cinfo;
}

/*
 * mono_reflection_get_custom_attrs_by_type:
 * @obj: a reflection object handle
 *
 * Return an array with all the custom attributes defined of the
 * reflection handle @obj. If @attr_klass is non-NULL, only custom attributes 
 * of that type are returned. The objects are fully build. Return NULL if a loading error
 * occurs.
 */
MonoArray*
mono_reflection_get_custom_attrs_by_type (MonoObject *obj, MonoClass *attr_klass, MonoError *error)
{
	MonoArray *result;
	MonoCustomAttrInfo *cinfo;

	mono_error_init (error);

	cinfo = mono_reflection_get_custom_attrs_info (obj);
	if (cinfo) {
		result = mono_custom_attrs_construct_by_type (cinfo, attr_klass, error);
		if (!cinfo->cached)
			mono_custom_attrs_free (cinfo);
	} else {
		if (mono_loader_get_last_error ())
			return NULL;
		result = mono_array_new_cached (mono_domain_get (), mono_defaults.attribute_class, 0);
	}

	return result;
}

/*
 * mono_reflection_get_custom_attrs:
 * @obj: a reflection object handle
 *
 * Return an array with all the custom attributes defined of the
 * reflection handle @obj. The objects are fully build. Return NULL if a loading error
 * occurs.
 */
MonoArray*
mono_reflection_get_custom_attrs (MonoObject *obj)
{
	MonoError error;

	return mono_reflection_get_custom_attrs_by_type (obj, NULL, &error);
}

/*
 * mono_reflection_get_custom_attrs_data:
 * @obj: a reflection obj handle
 *
 * Returns an array of System.Reflection.CustomAttributeData,
 * which include information about attributes reflected on
 * types loaded using the Reflection Only methods
 */
MonoArray*
mono_reflection_get_custom_attrs_data (MonoObject *obj)
{
	MonoArray *result;
	MonoCustomAttrInfo *cinfo;

	cinfo = mono_reflection_get_custom_attrs_info (obj);
	if (cinfo) {
		result = mono_custom_attrs_data_construct (cinfo);
		if (!cinfo->cached)
			mono_custom_attrs_free (cinfo);
	} else
		result = mono_array_new (mono_domain_get (), mono_defaults.customattribute_data_class, 0);

	return result;
}

static MonoReflectionType*
mono_reflection_type_get_underlying_system_type (MonoReflectionType* t)
{
	static MonoMethod *method_get_underlying_system_type = NULL;
	MonoMethod *usertype_method;

	if (!method_get_underlying_system_type)
		method_get_underlying_system_type = mono_class_get_method_from_name (mono_defaults.systemtype_class, "get_UnderlyingSystemType", 0);
	usertype_method = mono_object_get_virtual_method ((MonoObject *) t, method_get_underlying_system_type);
        return (MonoReflectionType *) mono_runtime_invoke (usertype_method, t, NULL, NULL);
}


static gboolean
is_corlib_type (MonoClass *class)
{
	return class->image == mono_defaults.corlib;
}

#define check_corlib_type_cached(_class, _namespace, _name) do { \
	static MonoClass *cached_class; \
	if (cached_class) \
		return cached_class == _class; \
	if (is_corlib_type (_class) && !strcmp (_name, _class->name) && !strcmp (_namespace, _class->name_space)) { \
		cached_class = _class; \
		return TRUE; \
	} \
	return FALSE; \
} while (0) \


#ifndef DISABLE_REFLECTION_EMIT
static gboolean
is_sre_array (MonoClass *class)
{
	check_corlib_type_cached (class, "System.Reflection.Emit", "ArrayType");
}

static gboolean
is_sre_byref (MonoClass *class)
{
	check_corlib_type_cached (class, "System.Reflection.Emit", "ByRefType");
}

static gboolean
is_sre_pointer (MonoClass *class)
{
	check_corlib_type_cached (class, "System.Reflection.Emit", "PointerType");
}

static gboolean
is_sre_generic_instance (MonoClass *class)
{
	check_corlib_type_cached (class, "System.Reflection", "MonoGenericClass");
}

static gboolean
is_sre_type_builder (MonoClass *class)
{
	check_corlib_type_cached (class, "System.Reflection.Emit", "TypeBuilder");
}

static gboolean
is_sre_method_builder (MonoClass *class)
{
	check_corlib_type_cached (class, "System.Reflection.Emit", "MethodBuilder");
}

static gboolean
is_sre_ctor_builder (MonoClass *class)
{
	check_corlib_type_cached (class, "System.Reflection.Emit", "ConstructorBuilder");
}

static gboolean
is_sre_field_builder (MonoClass *class)
{
	check_corlib_type_cached (class, "System.Reflection.Emit", "FieldBuilder");
}

static gboolean
is_sre_method_on_tb_inst (MonoClass *class)
{
	check_corlib_type_cached (class, "System.Reflection.Emit", "MethodOnTypeBuilderInst");
}

static gboolean
is_sre_ctor_on_tb_inst (MonoClass *class)
{
	check_corlib_type_cached (class, "System.Reflection.Emit", "ConstructorOnTypeBuilderInst");
}

MonoType*
mono_reflection_type_get_handle (MonoReflectionType* ref)
{
	MonoClass *class;
	if (!ref)
		return NULL;
	if (ref->type)
		return ref->type;

	if (is_usertype (ref)) {
		ref = mono_reflection_type_get_underlying_system_type (ref);
		if (ref == NULL || is_usertype (ref))
			return NULL;
		if (ref->type)
			return ref->type;
	}

	class = mono_object_class (ref);

	if (is_sre_array (class)) {
		MonoType *res;
		MonoReflectionArrayType *sre_array = (MonoReflectionArrayType*)ref;
		MonoType *base = mono_reflection_type_get_handle (sre_array->element_type);
		g_assert (base);
		if (sre_array->rank == 0) //single dimentional array
			res = &mono_array_class_get (mono_class_from_mono_type (base), 1)->byval_arg;
		else
			res = &mono_bounded_array_class_get (mono_class_from_mono_type (base), sre_array->rank, TRUE)->byval_arg;
		sre_array->type.type = res;
		return res;
	} else if (is_sre_byref (class)) {
		MonoType *res;
		MonoReflectionDerivedType *sre_byref = (MonoReflectionDerivedType*)ref;
		MonoType *base = mono_reflection_type_get_handle (sre_byref->element_type);
		g_assert (base);
		res = &mono_class_from_mono_type (base)->this_arg;
		sre_byref->type.type = res;
		return res;
	} else if (is_sre_pointer (class)) {
		MonoType *res;
		MonoReflectionDerivedType *sre_pointer = (MonoReflectionDerivedType*)ref;
		MonoType *base = mono_reflection_type_get_handle (sre_pointer->element_type);
		g_assert (base);
		res = &mono_ptr_class_get (base)->byval_arg;
		sre_pointer->type.type = res;
		return res;
	} else if (is_sre_generic_instance (class)) {
		MonoType *res, **types;
		MonoReflectionGenericClass *gclass = (MonoReflectionGenericClass*)ref;
		int i, count;

		count = mono_array_length (gclass->type_arguments);
		types = g_new0 (MonoType*, count);
		for (i = 0; i < count; ++i) {
			MonoReflectionType *t = mono_array_get (gclass->type_arguments, gpointer, i);
			types [i] = mono_reflection_type_get_handle (t);
			if (!types[i]) {
				g_free (types);
				return NULL;
			}
		}

		res = mono_reflection_bind_generic_parameters (gclass->generic_type, count, types);
		g_free (types);
		g_assert (res);
		gclass->type.type = res;
		return res;
	}

	g_error ("Cannot handle corlib user type %s", mono_type_full_name (&mono_object_class(ref)->byval_arg));
	return NULL;
}



void
mono_reflection_create_unmanaged_type (MonoReflectionType *type)
{
	mono_reflection_type_get_handle (type);
}

void
mono_reflection_register_with_runtime (MonoReflectionType *type)
{
	MonoType *res = mono_reflection_type_get_handle (type);
	MonoDomain *domain = mono_object_domain ((MonoObject*)type);
	MonoClass *class;

	if (!res)
		mono_raise_exception (mono_get_exception_argument (NULL, "Invalid generic instantiation, one or more arguments are not proper user types"));

	class = mono_class_from_mono_type (res);

	mono_loader_lock (); /*same locking as mono_type_get_object*/
	mono_domain_lock (domain);

	if (!class->image->dynamic) {
		mono_class_setup_supertypes (class);
	} else {
		if (!domain->type_hash)
			domain->type_hash = mono_g_hash_table_new_type ((GHashFunc)mono_metadata_type_hash, 
					(GCompareFunc)mono_metadata_type_equal, MONO_HASH_VALUE_GC);
		mono_g_hash_table_insert (domain->type_hash, res, type);
	}
	mono_domain_unlock (domain);
	mono_loader_unlock ();
}

/**
 * LOCKING: Assumes the loader lock is held.
 */
static MonoMethodSignature*
parameters_to_signature (MonoImage *image, MonoArray *parameters) {
	MonoMethodSignature *sig;
	int count, i;

	count = parameters? mono_array_length (parameters): 0;

	sig = image_g_malloc0 (image, MONO_SIZEOF_METHOD_SIGNATURE + sizeof (MonoType*) * count);
	sig->param_count = count;
	sig->sentinelpos = -1; /* FIXME */
	for (i = 0; i < count; ++i)
		sig->params [i] = mono_type_array_get_and_resolve (parameters, i);
	return sig;
}

/**
 * LOCKING: Assumes the loader lock is held.
 */
static MonoMethodSignature*
ctor_builder_to_signature (MonoImage *image, MonoReflectionCtorBuilder *ctor) {
	MonoMethodSignature *sig;

	sig = parameters_to_signature (image, ctor->parameters);
	sig->hasthis = ctor->attrs & METHOD_ATTRIBUTE_STATIC? 0: 1;
	sig->ret = &mono_defaults.void_class->byval_arg;
	return sig;
}

/**
 * LOCKING: Assumes the loader lock is held.
 */
static MonoMethodSignature*
method_builder_to_signature (MonoImage *image, MonoReflectionMethodBuilder *method) {
	MonoMethodSignature *sig;

	sig = parameters_to_signature (image, method->parameters);
	sig->hasthis = method->attrs & METHOD_ATTRIBUTE_STATIC? 0: 1;
	sig->ret = method->rtype? mono_reflection_type_get_handle ((MonoReflectionType*)method->rtype): &mono_defaults.void_class->byval_arg;
	sig->generic_param_count = method->generic_params ? mono_array_length (method->generic_params) : 0;
	return sig;
}

static MonoMethodSignature*
dynamic_method_to_signature (MonoReflectionDynamicMethod *method) {
	MonoMethodSignature *sig;

	sig = parameters_to_signature (NULL, method->parameters);
	sig->hasthis = method->attrs & METHOD_ATTRIBUTE_STATIC? 0: 1;
	sig->ret = method->rtype? mono_reflection_type_get_handle (method->rtype): &mono_defaults.void_class->byval_arg;
	sig->generic_param_count = 0;
	return sig;
}

static void
get_prop_name_and_type (MonoObject *prop, char **name, MonoType **type)
{
	MonoClass *klass = mono_object_class (prop);
	if (strcmp (klass->name, "PropertyBuilder") == 0) {
		MonoReflectionPropertyBuilder *pb = (MonoReflectionPropertyBuilder *)prop;
		*name = mono_string_to_utf8 (pb->name);
		*type = mono_reflection_type_get_handle ((MonoReflectionType*)pb->type);
	} else {
		MonoReflectionProperty *p = (MonoReflectionProperty *)prop;
		*name = g_strdup (p->property->name);
		if (p->property->get)
			*type = mono_method_signature (p->property->get)->ret;
		else
			*type = mono_method_signature (p->property->set)->params [mono_method_signature (p->property->set)->param_count - 1];
	}
}

static void
get_field_name_and_type (MonoObject *field, char **name, MonoType **type)
{
	MonoClass *klass = mono_object_class (field);
	if (strcmp (klass->name, "FieldBuilder") == 0) {
		MonoReflectionFieldBuilder *fb = (MonoReflectionFieldBuilder *)field;
		*name = mono_string_to_utf8 (fb->name);
		*type = mono_reflection_type_get_handle ((MonoReflectionType*)fb->type);
	} else {
		MonoReflectionField *f = (MonoReflectionField *)field;
		*name = g_strdup (mono_field_get_name (f->field));
		*type = f->field->type;
	}
}

#else /* DISABLE_REFLECTION_EMIT */

void
mono_reflection_register_with_runtime (MonoReflectionType *type)
{
	/* This is empty */
}

static gboolean
is_sre_type_builder (MonoClass *class)
{
	return FALSE;
}

static gboolean
is_sre_generic_instance (MonoClass *class)
{
	return FALSE;
}

static void
init_type_builder_generics (MonoObject *type)
{
}

#endif /* !DISABLE_REFLECTION_EMIT */


static gboolean
is_sr_mono_field (MonoClass *class)
{
	check_corlib_type_cached (class, "System.Reflection", "MonoField");
}

static gboolean
is_sr_mono_property (MonoClass *class)
{
	check_corlib_type_cached (class, "System.Reflection", "MonoProperty");
}

static gboolean
is_sr_mono_method (MonoClass *class)
{
	check_corlib_type_cached (class, "System.Reflection", "MonoMethod");
}

static gboolean
is_sr_mono_cmethod (MonoClass *class)
{
	check_corlib_type_cached (class, "System.Reflection", "MonoCMethod");
}

static gboolean
is_sr_mono_generic_method (MonoClass *class)
{
	check_corlib_type_cached (class, "System.Reflection", "MonoGenericMethod");
}

static gboolean
is_sr_mono_generic_cmethod (MonoClass *class)
{
	check_corlib_type_cached (class, "System.Reflection", "MonoGenericCMethod");
}

gboolean
mono_class_is_reflection_method_or_constructor (MonoClass *class)
{
	return is_sr_mono_method (class) || is_sr_mono_cmethod (class) || is_sr_mono_generic_method (class) || is_sr_mono_generic_cmethod (class);
}

static gboolean
is_usertype (MonoReflectionType *ref)
{
	MonoClass *class = mono_object_class (ref);
	return class->image != mono_defaults.corlib || strcmp ("TypeDelegator", class->name) == 0;
}

static MonoReflectionType*
mono_reflection_type_resolve_user_types (MonoReflectionType *type)
{
	if (!type || type->type)
		return type;

	if (is_usertype (type)) {
		type = mono_reflection_type_get_underlying_system_type (type);
		if (is_usertype (type))
			mono_raise_exception (mono_get_exception_not_supported ("User defined subclasses of System.Type are not yet supported22"));
	}

	return type;
}
/*
 * Encode a value in a custom attribute stream of bytes.
 * The value to encode is either supplied as an object in argument val
 * (valuetypes are boxed), or as a pointer to the data in the
 * argument argval.
 * @type represents the type of the value
 * @buffer is the start of the buffer
 * @p the current position in the buffer
 * @buflen contains the size of the buffer and is used to return the new buffer size
 * if this needs to be realloced.
 * @retbuffer and @retp return the start and the position of the buffer
 */
static void
encode_cattr_value (MonoAssembly *assembly, char *buffer, char *p, char **retbuffer, char **retp, guint32 *buflen, MonoType *type, MonoObject *arg, char *argval)
{
	MonoTypeEnum simple_type;
	
	if ((p-buffer) + 10 >= *buflen) {
		char *newbuf;
		*buflen *= 2;
		newbuf = g_realloc (buffer, *buflen);
		p = newbuf + (p-buffer);
		buffer = newbuf;
	}
	if (!argval)
		argval = ((char*)arg + sizeof (MonoObject));
	simple_type = type->type;
handle_enum:
	switch (simple_type) {
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_U1:
	case MONO_TYPE_I1:
		*p++ = *argval;
		break;
	case MONO_TYPE_CHAR:
	case MONO_TYPE_U2:
	case MONO_TYPE_I2:
		swap_with_size (p, argval, 2, 1);
		p += 2;
		break;
	case MONO_TYPE_U4:
	case MONO_TYPE_I4:
	case MONO_TYPE_R4:
		swap_with_size (p, argval, 4, 1);
		p += 4;
		break;
	case MONO_TYPE_R8:
		swap_with_size (p, argval, 8, 1);
		p += 8;
		break;
	case MONO_TYPE_U8:
	case MONO_TYPE_I8:
		swap_with_size (p, argval, 8, 1);
		p += 8;
		break;
	case MONO_TYPE_VALUETYPE:
		if (type->data.klass->enumtype) {
			simple_type = mono_class_enum_basetype (type->data.klass)->type;
			goto handle_enum;
		} else {
			g_warning ("generic valutype %s not handled in custom attr value decoding", type->data.klass->name);
		}
		break;
	case MONO_TYPE_STRING: {
		char *str;
		guint32 slen;
		if (!arg) {
			*p++ = 0xFF;
			break;
		}
		str = mono_string_to_utf8 ((MonoString*)arg);
		slen = strlen (str);
		if ((p-buffer) + 10 + slen >= *buflen) {
			char *newbuf;
			*buflen *= 2;
			*buflen += slen;
			newbuf = g_realloc (buffer, *buflen);
			p = newbuf + (p-buffer);
			buffer = newbuf;
		}
		mono_metadata_encode_value (slen, p, &p);
		memcpy (p, str, slen);
		p += slen;
		g_free (str);
		break;
	}
	case MONO_TYPE_CLASS: {
		char *str;
		guint32 slen;
		if (!arg) {
			*p++ = 0xFF;
			break;
		}
handle_type:
		str = type_get_qualified_name (mono_reflection_type_get_handle ((MonoReflectionType*)arg), NULL);
		slen = strlen (str);
		if ((p-buffer) + 10 + slen >= *buflen) {
			char *newbuf;
			*buflen *= 2;
			*buflen += slen;
			newbuf = g_realloc (buffer, *buflen);
			p = newbuf + (p-buffer);
			buffer = newbuf;
		}
		mono_metadata_encode_value (slen, p, &p);
		memcpy (p, str, slen);
		p += slen;
		g_free (str);
		break;
	}
	case MONO_TYPE_SZARRAY: {
		int len, i;
		MonoClass *eclass, *arg_eclass;

		if (!arg) {
			*p++ = 0xff; *p++ = 0xff; *p++ = 0xff; *p++ = 0xff;
			break;
		}
		len = mono_array_length ((MonoArray*)arg);
		*p++ = len & 0xff;
		*p++ = (len >> 8) & 0xff;
		*p++ = (len >> 16) & 0xff;
		*p++ = (len >> 24) & 0xff;
		*retp = p;
		*retbuffer = buffer;
		eclass = type->data.klass;
		arg_eclass = mono_object_class (arg)->element_class;

		if (!eclass) {
			/* Happens when we are called from the MONO_TYPE_OBJECT case below */
			eclass = mono_defaults.object_class;
		}
		if (eclass == mono_defaults.object_class && arg_eclass->valuetype) {
			char *elptr = mono_array_addr ((MonoArray*)arg, char, 0);
			int elsize = mono_class_array_element_size (arg_eclass);
			for (i = 0; i < len; ++i) {
				encode_cattr_value (assembly, buffer, p, &buffer, &p, buflen, &arg_eclass->byval_arg, NULL, elptr);
				elptr += elsize;
			}
		} else if (eclass->valuetype && arg_eclass->valuetype) {
			char *elptr = mono_array_addr ((MonoArray*)arg, char, 0);
			int elsize = mono_class_array_element_size (eclass);
			for (i = 0; i < len; ++i) {
				encode_cattr_value (assembly, buffer, p, &buffer, &p, buflen, &eclass->byval_arg, NULL, elptr);
				elptr += elsize;
			}
		} else {
			for (i = 0; i < len; ++i) {
				encode_cattr_value (assembly, buffer, p, &buffer, &p, buflen, &eclass->byval_arg, mono_array_get ((MonoArray*)arg, MonoObject*, i), NULL);
			}
		}
		break;
	}
	case MONO_TYPE_OBJECT: {
		MonoClass *klass;
		char *str;
		guint32 slen;

		/*
		 * The parameter type is 'object' but the type of the actual
		 * argument is not. So we have to add type information to the blob
		 * too. This is completely undocumented in the spec.
		 */

		if (arg == NULL) {
			*p++ = MONO_TYPE_STRING;	// It's same hack as MS uses
			*p++ = 0xFF;
			break;
		}
		
		klass = mono_object_class (arg);

		if (mono_object_isinst (arg, mono_defaults.systemtype_class)) {
			*p++ = 0x50;
			goto handle_type;
		} else if (klass->enumtype) {
			*p++ = 0x55;
		} else if (klass == mono_defaults.string_class) {
			simple_type = MONO_TYPE_STRING;
			*p++ = 0x0E;
			goto handle_enum;
		} else if (klass->rank == 1) {
			*p++ = 0x1D;
			if (klass->element_class->byval_arg.type == MONO_TYPE_OBJECT)
				/* See Partition II, Appendix B3 */
				*p++ = 0x51;
			else
				*p++ = klass->element_class->byval_arg.type;
			encode_cattr_value (assembly, buffer, p, &buffer, &p, buflen, &klass->byval_arg, arg, NULL);
			break;
		} else if (klass->byval_arg.type >= MONO_TYPE_BOOLEAN && klass->byval_arg.type <= MONO_TYPE_R8) {
			*p++ = simple_type = klass->byval_arg.type;
			goto handle_enum;
		} else {
			g_error ("unhandled type in custom attr");
		}
		str = type_get_qualified_name (mono_class_get_type(klass), NULL);
		slen = strlen (str);
		if ((p-buffer) + 10 + slen >= *buflen) {
			char *newbuf;
			*buflen *= 2;
			*buflen += slen;
			newbuf = g_realloc (buffer, *buflen);
			p = newbuf + (p-buffer);
			buffer = newbuf;
		}
		mono_metadata_encode_value (slen, p, &p);
		memcpy (p, str, slen);
		p += slen;
		g_free (str);
		simple_type = mono_class_enum_basetype (klass)->type;
		goto handle_enum;
	}
	default:
		g_error ("type 0x%02x not yet supported in custom attr encoder", simple_type);
	}
	*retp = p;
	*retbuffer = buffer;
}

static void
encode_field_or_prop_type (MonoType *type, char *p, char **retp)
{
	if (type->type == MONO_TYPE_VALUETYPE && type->data.klass->enumtype) {
		char *str = type_get_qualified_name (type, NULL);
		int slen = strlen (str);

		*p++ = 0x55;
		/*
		 * This seems to be optional...
		 * *p++ = 0x80;
		 */
		mono_metadata_encode_value (slen, p, &p);
		memcpy (p, str, slen);
		p += slen;
		g_free (str);
	} else if (type->type == MONO_TYPE_OBJECT) {
		*p++ = 0x51;
	} else if (type->type == MONO_TYPE_CLASS) {
		/* it should be a type: encode_cattr_value () has the check */
		*p++ = 0x50;
	} else {
		mono_metadata_encode_value (type->type, p, &p);
		if (type->type == MONO_TYPE_SZARRAY)
			/* See the examples in Partition VI, Annex B */
			encode_field_or_prop_type (&type->data.klass->byval_arg, p, &p);
	}

	*retp = p;
}

#ifndef DISABLE_REFLECTION_EMIT
static void
encode_named_val (MonoReflectionAssembly *assembly, char *buffer, char *p, char **retbuffer, char **retp, guint32 *buflen, MonoType *type, char *name, MonoObject *value)
{
	int len;
	/* Preallocate a large enough buffer */
	if (type->type == MONO_TYPE_VALUETYPE && type->data.klass->enumtype) {
		char *str = type_get_qualified_name (type, NULL);
		len = strlen (str);
		g_free (str);
	} else if (type->type == MONO_TYPE_SZARRAY && type->data.klass->enumtype) {
		char *str = type_get_qualified_name (&type->data.klass->byval_arg, NULL);
		len = strlen (str);
		g_free (str);
	} else {
		len = 0;
	}
	len += strlen (name);

	if ((p-buffer) + 20 + len >= *buflen) {
		char *newbuf;
		*buflen *= 2;
		*buflen += len;
		newbuf = g_realloc (buffer, *buflen);
		p = newbuf + (p-buffer);
		buffer = newbuf;
	}

	encode_field_or_prop_type (type, p, &p);

	len = strlen (name);
	mono_metadata_encode_value (len, p, &p);
	memcpy (p, name, len);
	p += len;
	encode_cattr_value (assembly->assembly, buffer, p, &buffer, &p, buflen, type, value, NULL);
	*retp = p;
	*retbuffer = buffer;
}

/*
 * mono_reflection_get_custom_attrs_blob:
 * @ctor: custom attribute constructor
 * @ctorArgs: arguments o the constructor
 * @properties:
 * @propValues:
 * @fields:
 * @fieldValues:
 * 
 * Creates the blob of data that needs to be saved in the metadata and that represents
 * the custom attributed described by @ctor, @ctorArgs etc.
 * Returns: a Byte array representing the blob of data.
 */
MonoArray*
mono_reflection_get_custom_attrs_blob (MonoReflectionAssembly *assembly, MonoObject *ctor, MonoArray *ctorArgs, MonoArray *properties, MonoArray *propValues, MonoArray *fields, MonoArray* fieldValues) 
{
	MonoArray *result;
	MonoMethodSignature *sig;
	MonoObject *arg;
	char *buffer, *p;
	guint32 buflen, i;

	MONO_ARCH_SAVE_REGS;

	if (strcmp (ctor->vtable->klass->name, "MonoCMethod")) {
		/* sig is freed later so allocate it in the heap */
		sig = ctor_builder_to_signature (NULL, (MonoReflectionCtorBuilder*)ctor);
	} else {
		sig = mono_method_signature (((MonoReflectionMethod*)ctor)->method);
	}

	g_assert (mono_array_length (ctorArgs) == sig->param_count);
	buflen = 256;
	p = buffer = g_malloc (buflen);
	/* write the prolog */
	*p++ = 1;
	*p++ = 0;
	for (i = 0; i < sig->param_count; ++i) {
		arg = mono_array_get (ctorArgs, MonoObject*, i);
		encode_cattr_value (assembly->assembly, buffer, p, &buffer, &p, &buflen, sig->params [i], arg, NULL);
	}
	i = 0;
	if (properties)
		i += mono_array_length (properties);
	if (fields)
		i += mono_array_length (fields);
	*p++ = i & 0xff;
	*p++ = (i >> 8) & 0xff;
	if (properties) {
		MonoObject *prop;
		for (i = 0; i < mono_array_length (properties); ++i) {
			MonoType *ptype;
			char *pname;

			prop = mono_array_get (properties, gpointer, i);
			get_prop_name_and_type (prop, &pname, &ptype);
			*p++ = 0x54; /* PROPERTY signature */
			encode_named_val (assembly, buffer, p, &buffer, &p, &buflen, ptype, pname, (MonoObject*)mono_array_get (propValues, gpointer, i));
			g_free (pname);
		}
	}

	if (fields) {
		MonoObject *field;
		for (i = 0; i < mono_array_length (fields); ++i) {
			MonoType *ftype;
			char *fname;

			field = mono_array_get (fields, gpointer, i);
			get_field_name_and_type (field, &fname, &ftype);
			*p++ = 0x53; /* FIELD signature */
			encode_named_val (assembly, buffer, p, &buffer, &p, &buflen, ftype, fname, (MonoObject*)mono_array_get (fieldValues, gpointer, i));
			g_free (fname);
		}
	}

	g_assert (p - buffer <= buflen);
	buflen = p - buffer;
	result = mono_array_new (mono_domain_get (), mono_defaults.byte_class, buflen);
	p = mono_array_addr (result, char, 0);
	memcpy (p, buffer, buflen);
	g_free (buffer);
	if (strcmp (ctor->vtable->klass->name, "MonoCMethod"))
		g_free (sig);
	return result;
}

/*
 * mono_reflection_setup_internal_class:
 * @tb: a TypeBuilder object
 *
 * Creates a MonoClass that represents the TypeBuilder.
 * This is a trick that lets us simplify a lot of reflection code
 * (and will allow us to support Build and Run assemblies easier).
 */
void
mono_reflection_setup_internal_class (MonoReflectionTypeBuilder *tb)
{
	MonoError error;
	MonoClass *klass, *parent;

	MONO_ARCH_SAVE_REGS;

	RESOLVE_TYPE (tb->parent);

	mono_loader_lock ();

	if (tb->parent) {
		/* check so we can compile corlib correctly */
		if (strcmp (mono_object_class (tb->parent)->name, "TypeBuilder") == 0) {
			/* mono_class_setup_mono_type () guaranteess type->data.klass is valid */
			parent = mono_reflection_type_get_handle ((MonoReflectionType*)tb->parent)->data.klass;
		} else {
			parent = mono_class_from_mono_type (mono_reflection_type_get_handle ((MonoReflectionType*)tb->parent));
		}
	} else {
		parent = NULL;
	}
	
	/* the type has already being created: it means we just have to change the parent */
	if (tb->type.type) {
		klass = mono_class_from_mono_type (tb->type.type);
		klass->parent = NULL;
		/* fool mono_class_setup_parent */
		klass->supertypes = NULL;
		mono_class_setup_parent (klass, parent);
		mono_class_setup_mono_type (klass);
		mono_loader_unlock ();
		return;
	}

	klass = mono_image_alloc0 (&tb->module->dynamic_image->image, sizeof (MonoClass));

	klass->image = &tb->module->dynamic_image->image;

	klass->inited = 1; /* we lie to the runtime */
	klass->name = mono_string_to_utf8_image (klass->image, tb->name, &error);
	if (!mono_error_ok (&error))
		goto failure;
	klass->name_space = mono_string_to_utf8_image (klass->image, tb->nspace, &error);
	if (!mono_error_ok (&error))
		goto failure;
	klass->type_token = MONO_TOKEN_TYPE_DEF | tb->table_idx;
	klass->flags = tb->attrs;
	
	mono_profiler_class_event (klass, MONO_PROFILE_START_LOAD);

	klass->element_class = klass;

	if (mono_class_get_ref_info (klass) == NULL) {

		mono_class_set_ref_info (klass, tb);

		/* Put into cache so mono_class_get () will find it.
		Skip nested types as those should not be available on the global scope. */
		if (!tb->nesting_type) {
			mono_image_add_to_name_cache (klass->image, klass->name_space, klass->name, tb->table_idx);
		} else {
			klass->image->reflection_info_unregister_classes =
				g_slist_prepend (klass->image->reflection_info_unregister_classes, klass);
		}
	} else {
		g_assert (mono_class_get_ref_info (klass) == tb);
	}

	mono_g_hash_table_insert (tb->module->dynamic_image->tokens,
		GUINT_TO_POINTER (MONO_TOKEN_TYPE_DEF | tb->table_idx), tb);

	if (parent != NULL) {
		mono_class_setup_parent (klass, parent);
	} else if (strcmp (klass->name, "Object") == 0 && strcmp (klass->name_space, "System") == 0) {
		const char *old_n = klass->name;
		/* trick to get relative numbering right when compiling corlib */
		klass->name = "BuildingObject";
		mono_class_setup_parent (klass, mono_defaults.object_class);
		klass->name = old_n;
	}

	if ((!strcmp (klass->name, "ValueType") && !strcmp (klass->name_space, "System")) ||
			(!strcmp (klass->name, "Object") && !strcmp (klass->name_space, "System")) ||
			(!strcmp (klass->name, "Enum") && !strcmp (klass->name_space, "System"))) {
		klass->instance_size = sizeof (MonoObject);
		klass->size_inited = 1;
		mono_class_setup_vtable_general (klass, NULL, 0, NULL);
	}

	mono_class_setup_mono_type (klass);

	mono_class_setup_supertypes (klass);

	/*
	 * FIXME: handle interfaces.
	 */

	tb->type.type = &klass->byval_arg;

	if (tb->nesting_type) {
		g_assert (tb->nesting_type->type);
		klass->nested_in = mono_class_from_mono_type (mono_reflection_type_get_handle (tb->nesting_type));
	}

	/*g_print ("setup %s as %s (%p)\n", klass->name, ((MonoObject*)tb)->vtable->klass->name, tb);*/

	mono_profiler_class_loaded (klass, MONO_PROFILE_OK);
	
	mono_loader_unlock ();
	return;

failure:
	mono_loader_unlock ();
	mono_error_raise_exception (&error);
}

/*
 * mono_reflection_setup_generic_class:
 * @tb: a TypeBuilder object
 *
 * Setup the generic class before adding the first generic parameter.
 */
void
mono_reflection_setup_generic_class (MonoReflectionTypeBuilder *tb)
{
}

/*
 * mono_reflection_create_generic_class:
 * @tb: a TypeBuilder object
 *
 * Creates the generic class after all generic parameters have been added.
 */
void
mono_reflection_create_generic_class (MonoReflectionTypeBuilder *tb)
{
	MonoClass *klass;
	int count, i;

	MONO_ARCH_SAVE_REGS;

	klass = mono_class_from_mono_type (tb->type.type);

	count = tb->generic_params ? mono_array_length (tb->generic_params) : 0;

	if (klass->generic_container || (count == 0))
		return;

	g_assert (tb->generic_container && (tb->generic_container->owner.klass == klass));

	klass->generic_container = mono_image_alloc0 (klass->image, sizeof (MonoGenericContainer));

	klass->generic_container->owner.klass = klass;
	klass->generic_container->type_argc = count;
	klass->generic_container->type_params = mono_image_alloc0 (klass->image, sizeof (MonoGenericParamFull) * count);

	klass->is_generic = 1;

	for (i = 0; i < count; i++) {
		MonoReflectionGenericParam *gparam = mono_array_get (tb->generic_params, gpointer, i);
		MonoGenericParamFull *param = (MonoGenericParamFull *) mono_reflection_type_get_handle ((MonoReflectionType*)gparam)->data.generic_param;
		klass->generic_container->type_params [i] = *param;
		/*Make sure we are a diferent type instance */
		klass->generic_container->type_params [i].param.owner = klass->generic_container;
		klass->generic_container->type_params [i].info.pklass = NULL;
		klass->generic_container->type_params [i].info.flags = gparam->attrs;

		g_assert (klass->generic_container->type_params [i].param.owner);
	}

	klass->generic_container->context.class_inst = mono_get_shared_generic_inst (klass->generic_container);
}

/*
 * mono_reflection_create_internal_class:
 * @tb: a TypeBuilder object
 *
 * Actually create the MonoClass that is associated with the TypeBuilder.
 */
void
mono_reflection_create_internal_class (MonoReflectionTypeBuilder *tb)
{
	MonoClass *klass;

	MONO_ARCH_SAVE_REGS;

	klass = mono_class_from_mono_type (tb->type.type);

	mono_loader_lock ();
	if (klass->enumtype && mono_class_enum_basetype (klass) == NULL) {
		MonoReflectionFieldBuilder *fb;
		MonoClass *ec;
		MonoType *enum_basetype;

		g_assert (tb->fields != NULL);
		g_assert (mono_array_length (tb->fields) >= 1);

		fb = mono_array_get (tb->fields, MonoReflectionFieldBuilder*, 0);

		if (!mono_type_is_valid_enum_basetype (mono_reflection_type_get_handle ((MonoReflectionType*)fb->type))) {
			mono_loader_unlock ();
			return;
		}

		enum_basetype = mono_reflection_type_get_handle ((MonoReflectionType*)fb->type);
		klass->element_class = mono_class_from_mono_type (enum_basetype);
		if (!klass->element_class)
			klass->element_class = mono_class_from_mono_type (enum_basetype);

		/*
		 * get the element_class from the current corlib.
		 */
		ec = default_class_from_mono_type (enum_basetype);
		klass->instance_size = ec->instance_size;
		klass->size_inited = 1;
		/* 
		 * this is almost safe to do with enums and it's needed to be able
		 * to create objects of the enum type (for use in SetConstant).
		 */
		/* FIXME: Does this mean enums can't have method overrides ? */
		mono_class_setup_vtable_general (klass, NULL, 0, NULL);
	}
	mono_loader_unlock ();
}

static MonoMarshalSpec*
mono_marshal_spec_from_builder (MonoImage *image, MonoAssembly *assembly,
								MonoReflectionMarshal *minfo)
{
	MonoMarshalSpec *res;

	res = image_g_new0 (image, MonoMarshalSpec, 1);
	res->native = minfo->type;

	switch (minfo->type) {
	case MONO_NATIVE_LPARRAY:
		res->data.array_data.elem_type = minfo->eltype;
		if (minfo->has_size) {
			res->data.array_data.param_num = minfo->param_num;
			res->data.array_data.num_elem = minfo->count;
			res->data.array_data.elem_mult = minfo->param_num == -1 ? 0 : 1;
		}
		else {
			res->data.array_data.param_num = -1;
			res->data.array_data.num_elem = -1;
			res->data.array_data.elem_mult = -1;
		}
		break;

	case MONO_NATIVE_BYVALTSTR:
	case MONO_NATIVE_BYVALARRAY:
		res->data.array_data.num_elem = minfo->count;
		break;

	case MONO_NATIVE_CUSTOM:
		if (minfo->marshaltyperef)
			res->data.custom_data.custom_name =
				type_get_fully_qualified_name (mono_reflection_type_get_handle ((MonoReflectionType*)minfo->marshaltyperef));
		if (minfo->mcookie)
			res->data.custom_data.cookie = mono_string_to_utf8 (minfo->mcookie);
		break;

	default:
		break;
	}

	return res;
}
#endif /* !DISABLE_REFLECTION_EMIT */

MonoReflectionMarshalAsAttribute*
mono_reflection_marshal_as_attribute_from_marshal_spec (MonoDomain *domain, MonoClass *klass,
										   MonoMarshalSpec *spec)
{
	static MonoClass *System_Reflection_Emit_MarshalAsAttribute;
	MonoReflectionMarshalAsAttribute *minfo;
	MonoType *mtype;

	if (!System_Reflection_Emit_MarshalAsAttribute) {
		System_Reflection_Emit_MarshalAsAttribute = mono_class_from_name (
		   mono_defaults.corlib, "System.Runtime.InteropServices", "MarshalAsAttribute");
		g_assert (System_Reflection_Emit_MarshalAsAttribute);
	}

	minfo = (MonoReflectionMarshalAsAttribute*)mono_object_new (domain, System_Reflection_Emit_MarshalAsAttribute);
	minfo->utype = spec->native;

	switch (minfo->utype) {
	case MONO_NATIVE_LPARRAY:
		minfo->array_subtype = spec->data.array_data.elem_type;
		minfo->size_const = spec->data.array_data.num_elem;
		if (spec->data.array_data.param_num != -1)
			minfo->size_param_index = spec->data.array_data.param_num;
		break;

	case MONO_NATIVE_BYVALTSTR:
	case MONO_NATIVE_BYVALARRAY:
		minfo->size_const = spec->data.array_data.num_elem;
		break;

	case MONO_NATIVE_CUSTOM:
		if (spec->data.custom_data.custom_name) {
			mtype = mono_reflection_type_from_name (spec->data.custom_data.custom_name, klass->image);
			if (mtype)
				MONO_OBJECT_SETREF (minfo, marshal_type_ref, mono_type_get_object (domain, mtype));

			MONO_OBJECT_SETREF (minfo, marshal_type, mono_string_new (domain, spec->data.custom_data.custom_name));
		}
		if (spec->data.custom_data.cookie)
			MONO_OBJECT_SETREF (minfo, marshal_cookie, mono_string_new (domain, spec->data.custom_data.cookie));
		break;

	default:
		break;
	}

	return minfo;
}

#ifndef DISABLE_REFLECTION_EMIT
static MonoMethod*
reflection_methodbuilder_to_mono_method (MonoClass *klass,
					 ReflectionMethodBuilder *rmb,
					 MonoMethodSignature *sig)
{
	MonoError error;
	MonoMethod *m;
	MonoMethodWrapper *wrapperm;
	MonoMarshalSpec **specs;
	MonoReflectionMethodAux *method_aux;
	MonoImage *image;
	gboolean dynamic;
	int i;

	mono_error_init (&error);
	/*
	 * Methods created using a MethodBuilder should have their memory allocated
	 * inside the image mempool, while dynamic methods should have their memory
	 * malloc'd.
	 */
	dynamic = rmb->refs != NULL;
	image = dynamic ? NULL : klass->image;

	if (!dynamic)
		g_assert (!klass->generic_class);

	mono_loader_lock ();

	if ((rmb->attrs & METHOD_ATTRIBUTE_PINVOKE_IMPL) ||
			(rmb->iattrs & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL))
		m = (MonoMethod *)image_g_new0 (image, MonoMethodPInvoke, 1);
	else
		m = (MonoMethod *)image_g_new0 (image, MonoMethodWrapper, 1);

	wrapperm = (MonoMethodWrapper*)m;

	m->dynamic = dynamic;
	m->slot = -1;
	m->flags = rmb->attrs;
	m->iflags = rmb->iattrs;
	m->name = mono_string_to_utf8_image_ignore (image, rmb->name);
	m->klass = klass;
	m->signature = sig;
	m->sre_method = TRUE;
	m->skip_visibility = rmb->skip_visibility;
	if (rmb->table_idx)
		m->token = MONO_TOKEN_METHOD_DEF | (*rmb->table_idx);

	if (m->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) {
		if (klass == mono_defaults.string_class && !strcmp (m->name, ".ctor"))
			m->string_ctor = 1;

		m->signature->pinvoke = 1;
	} else if (m->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) {
		m->signature->pinvoke = 1;

		method_aux = image_g_new0 (image, MonoReflectionMethodAux, 1);

		method_aux->dllentry = rmb->dllentry ? mono_string_to_utf8_image (image, rmb->dllentry, &error) : image_strdup (image, m->name);
		g_assert (mono_error_ok (&error));
		method_aux->dll = mono_string_to_utf8_image (image, rmb->dll, &error);
		g_assert (mono_error_ok (&error));
		
		((MonoMethodPInvoke*)m)->piflags = (rmb->native_cc << 8) | (rmb->charset ? (rmb->charset - 1) * 2 : 0) | rmb->extra_flags;

		if (klass->image->dynamic)
			g_hash_table_insert (((MonoDynamicImage*)klass->image)->method_aux_hash, m, method_aux);

		mono_loader_unlock ();

		return m;
	} else if (!(m->flags & METHOD_ATTRIBUTE_ABSTRACT) &&
			   !(m->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME)) {
		MonoMethodHeader *header;
		guint32 code_size;
		gint32 max_stack, i;
		gint32 num_locals = 0;
		gint32 num_clauses = 0;
		guint8 *code;

		if (rmb->ilgen) {
			code = mono_array_addr (rmb->ilgen->code, guint8, 0);
			code_size = rmb->ilgen->code_len;
			max_stack = rmb->ilgen->max_stack;
			num_locals = rmb->ilgen->locals ? mono_array_length (rmb->ilgen->locals) : 0;
			if (rmb->ilgen->ex_handlers)
				num_clauses = method_count_clauses (rmb->ilgen);
		} else {
			if (rmb->code) {
				code = mono_array_addr (rmb->code, guint8, 0);
				code_size = mono_array_length (rmb->code);
				/* we probably need to run a verifier on the code... */
				max_stack = 8; 
			}
			else {
				code = NULL;
				code_size = 0;
				max_stack = 8;
			}
		}

		header = image_g_malloc0 (image, MONO_SIZEOF_METHOD_HEADER + num_locals * sizeof (MonoType*));
		header->code_size = code_size;
		header->code = image_g_malloc (image, code_size);
		memcpy ((char*)header->code, code, code_size);
		header->max_stack = max_stack;
		header->init_locals = rmb->init_locals;
		header->num_locals = num_locals;

		for (i = 0; i < num_locals; ++i) {
			MonoReflectionLocalBuilder *lb = 
				mono_array_get (rmb->ilgen->locals, MonoReflectionLocalBuilder*, i);

			header->locals [i] = image_g_new0 (image, MonoType, 1);
			memcpy (header->locals [i], mono_reflection_type_get_handle ((MonoReflectionType*)lb->type), MONO_SIZEOF_TYPE);
		}

		header->num_clauses = num_clauses;
		if (num_clauses) {
			header->clauses = method_encode_clauses (image, (MonoDynamicImage*)klass->image,
				 rmb->ilgen, num_clauses);
		}

		wrapperm->header = header;
	}

	if (rmb->generic_params) {
		int count = mono_array_length (rmb->generic_params);
		MonoGenericContainer *container = rmb->generic_container;

		g_assert (container);

		container->type_argc = count;
		container->type_params = image_g_new0 (image, MonoGenericParamFull, count);
		container->owner.method = m;

		m->is_generic = TRUE;
		mono_method_set_generic_container (m, container);

		for (i = 0; i < count; i++) {
			MonoReflectionGenericParam *gp =
				mono_array_get (rmb->generic_params, MonoReflectionGenericParam*, i);
			MonoGenericParamFull *param = (MonoGenericParamFull *) mono_reflection_type_get_handle ((MonoReflectionType*)gp)->data.generic_param;
			container->type_params [i] = *param;
		}

		/*
		 * The method signature might have pointers to generic parameters that belong to other methods.
		 * This is a valid SRE case, but the resulting method signature must be encoded using the proper
		 * generic parameters.
		 */
		for (i = 0; i < m->signature->param_count; ++i) {
			MonoType *t = m->signature->params [i];
			if (t->type == MONO_TYPE_MVAR) {
				MonoGenericParam *gparam =  t->data.generic_param;
				if (gparam->num < count) {
					m->signature->params [i] = mono_metadata_type_dup (image, m->signature->params [i]);
					m->signature->params [i]->data.generic_param = mono_generic_container_get_param (container, gparam->num);
				}

			}
		}

		if (klass->generic_container) {
			container->parent = klass->generic_container;
			container->context.class_inst = klass->generic_container->context.class_inst;
		}
		container->context.method_inst = mono_get_shared_generic_inst (container);
	}

	if (rmb->refs) {
		MonoMethodWrapper *mw = (MonoMethodWrapper*)m;
		int i;
		void **data;

		m->wrapper_type = MONO_WRAPPER_DYNAMIC_METHOD;

		mw->method_data = data = image_g_new (image, gpointer, rmb->nrefs + 1);
		data [0] = GUINT_TO_POINTER (rmb->nrefs);
		for (i = 0; i < rmb->nrefs; ++i)
			data [i + 1] = rmb->refs [i];
	}

	method_aux = NULL;

	/* Parameter info */
	if (rmb->pinfo) {
		if (!method_aux)
			method_aux = image_g_new0 (image, MonoReflectionMethodAux, 1);
		method_aux->param_names = image_g_new0 (image, char *, mono_method_signature (m)->param_count + 1);
		for (i = 0; i <= m->signature->param_count; ++i) {
			MonoReflectionParamBuilder *pb;
			if ((pb = mono_array_get (rmb->pinfo, MonoReflectionParamBuilder*, i))) {
				if ((i > 0) && (pb->attrs)) {
					/* Make a copy since it might point to a shared type structure */
					m->signature->params [i - 1] = mono_metadata_type_dup (klass->image, m->signature->params [i - 1]);
					m->signature->params [i - 1]->attrs = pb->attrs;
				}

				if (pb->attrs & PARAM_ATTRIBUTE_HAS_DEFAULT) {
					MonoDynamicImage *assembly;
					guint32 idx, def_type, len;
					char *p;
					const char *p2;

					if (!method_aux->param_defaults) {
						method_aux->param_defaults = image_g_new0 (image, guint8*, m->signature->param_count + 1);
						method_aux->param_default_types = image_g_new0 (image, guint32, m->signature->param_count + 1);
					}
					assembly = (MonoDynamicImage*)klass->image;
					idx = encode_constant (assembly, pb->def_value, &def_type);
					/* Copy the data from the blob since it might get realloc-ed */
					p = assembly->blob.data + idx;
					len = mono_metadata_decode_blob_size (p, &p2);
					len += p2 - p;
					method_aux->param_defaults [i] = image_g_malloc (image, len);
					method_aux->param_default_types [i] = def_type;
					memcpy ((gpointer)method_aux->param_defaults [i], p, len);
				}

				if (pb->name) {
					method_aux->param_names [i] = mono_string_to_utf8_image (image, pb->name, &error);
					g_assert (mono_error_ok (&error));
				}
				if (pb->cattrs) {
					if (!method_aux->param_cattr)
						method_aux->param_cattr = image_g_new0 (image, MonoCustomAttrInfo*, m->signature->param_count + 1);
					method_aux->param_cattr [i] = mono_custom_attrs_from_builders (image, klass->image, pb->cattrs);
				}
			}
		}
	}

	/* Parameter marshalling */
	specs = NULL;
	if (rmb->pinfo)		
		for (i = 0; i < mono_array_length (rmb->pinfo); ++i) {
			MonoReflectionParamBuilder *pb;
			if ((pb = mono_array_get (rmb->pinfo, MonoReflectionParamBuilder*, i))) {
				if (pb->marshal_info) {
					if (specs == NULL)
						specs = image_g_new0 (image, MonoMarshalSpec*, sig->param_count + 1);
					specs [pb->position] = 
						mono_marshal_spec_from_builder (image, klass->image->assembly, pb->marshal_info);
				}
			}
		}
	if (specs != NULL) {
		if (!method_aux)
			method_aux = image_g_new0 (image, MonoReflectionMethodAux, 1);
		method_aux->param_marshall = specs;
	}

	if (klass->image->dynamic && method_aux)
		g_hash_table_insert (((MonoDynamicImage*)klass->image)->method_aux_hash, m, method_aux);

	mono_loader_unlock ();

	return m;
}	

static MonoMethod*
ctorbuilder_to_mono_method (MonoClass *klass, MonoReflectionCtorBuilder* mb)
{
	ReflectionMethodBuilder rmb;
	MonoMethodSignature *sig;

	mono_loader_lock ();
	sig = ctor_builder_to_signature (klass->image, mb);
	mono_loader_unlock ();

	reflection_methodbuilder_from_ctor_builder (&rmb, mb);

	mb->mhandle = reflection_methodbuilder_to_mono_method (klass, &rmb, sig);
	mono_save_custom_attrs (klass->image, mb->mhandle, mb->cattrs);

	/* If we are in a generic class, we might be called multiple times from inflate_method */
	if (!((MonoDynamicImage*)(MonoDynamicImage*)klass->image)->save && !klass->generic_container) {
		/* ilgen is no longer needed */
		mb->ilgen = NULL;
	}

	return mb->mhandle;
}

static MonoMethod*
methodbuilder_to_mono_method (MonoClass *klass, MonoReflectionMethodBuilder* mb)
{
	ReflectionMethodBuilder rmb;
	MonoMethodSignature *sig;

	mono_loader_lock ();
	sig = method_builder_to_signature (klass->image, mb);
	mono_loader_unlock ();

	reflection_methodbuilder_from_method_builder (&rmb, mb);

	mb->mhandle = reflection_methodbuilder_to_mono_method (klass, &rmb, sig);
	mono_save_custom_attrs (klass->image, mb->mhandle, mb->cattrs);

	/* If we are in a generic class, we might be called multiple times from inflate_method */
	if (!((MonoDynamicImage*)(MonoDynamicImage*)klass->image)->save && !klass->generic_container) {
		/* ilgen is no longer needed */
		mb->ilgen = NULL;
	}
	return mb->mhandle;
}

static MonoClassField*
fieldbuilder_to_mono_class_field (MonoClass *klass, MonoReflectionFieldBuilder* fb)
{
	MonoClassField *field;
	MonoType *custom;
	MonoError error;

	field = g_new0 (MonoClassField, 1);

	field->name = mono_string_to_utf8_image (klass->image, fb->name, &error);
	g_assert (mono_error_ok (&error));
	if (fb->attrs || fb->modreq || fb->modopt) {
		field->type = mono_metadata_type_dup (NULL, mono_reflection_type_get_handle ((MonoReflectionType*)fb->type));
		field->type->attrs = fb->attrs;

		g_assert (klass->image->dynamic);
		custom = add_custom_modifiers ((MonoDynamicImage*)klass->image, field->type, fb->modreq, fb->modopt);
		g_free (field->type);
		field->type = mono_metadata_type_dup (klass->image, custom);
		g_free (custom);
	} else {
		field->type = mono_reflection_type_get_handle ((MonoReflectionType*)fb->type);
	}
	if (fb->offset != -1)
		field->offset = fb->offset;
	field->parent = klass;
	mono_save_custom_attrs (klass->image, field, fb->cattrs);

	// FIXME: Can't store fb->def_value/RVA, is it needed for field_on_insts ?

	return field;
}
#endif

MonoType*
mono_reflection_bind_generic_parameters (MonoReflectionType *type, int type_argc, MonoType **types)
{
	MonoClass *klass;
	MonoReflectionTypeBuilder *tb = NULL;
	gboolean is_dynamic = FALSE;
	MonoDomain *domain;
	MonoClass *geninst;

	mono_loader_lock ();

	domain = mono_object_domain (type);

	if (is_sre_type_builder (mono_object_class (type))) {
		tb = (MonoReflectionTypeBuilder *) type;

		is_dynamic = TRUE;
	} else if (is_sre_generic_instance (mono_object_class (type))) {
		MonoReflectionGenericClass *rgi = (MonoReflectionGenericClass *) type;
		MonoReflectionType *gtd = rgi->generic_type;

		if (is_sre_type_builder (mono_object_class (gtd))) {
			tb = (MonoReflectionTypeBuilder *)gtd;
			is_dynamic = TRUE;
		}
	}

	/* FIXME: fix the CreateGenericParameters protocol to avoid the two stage setup of TypeBuilders */
	if (tb && tb->generic_container)
		mono_reflection_create_generic_class (tb);

	klass = mono_class_from_mono_type (mono_reflection_type_get_handle (type));
	if (!klass->generic_container) {
		mono_loader_unlock ();
		return NULL;
	}

	if (klass->wastypebuilder) {
		tb = (MonoReflectionTypeBuilder *) mono_class_get_ref_info (klass);

		is_dynamic = TRUE;
	}

	mono_loader_unlock ();

	geninst = mono_class_bind_generic_parameters (klass, type_argc, types, is_dynamic);

	return &geninst->byval_arg;
}

MonoClass*
mono_class_bind_generic_parameters (MonoClass *klass, int type_argc, MonoType **types, gboolean is_dynamic)
{
	MonoGenericClass *gclass;
	MonoGenericInst *inst;

	g_assert (klass->generic_container);

	inst = mono_metadata_get_generic_inst (type_argc, types);
	gclass = mono_metadata_lookup_generic_class (klass, inst, is_dynamic);

	return mono_generic_class_get_class (gclass);
}

MonoReflectionMethod*
mono_reflection_bind_generic_method_parameters (MonoReflectionMethod *rmethod, MonoArray *types)
{
	MonoClass *klass;
	MonoMethod *method, *inflated;
	MonoMethodInflated *imethod;
	MonoGenericContext tmp_context;
	MonoGenericInst *ginst;
	MonoType **type_argv;
	int count, i;

	MONO_ARCH_SAVE_REGS;

	/*FIXME but this no longer should happen*/
	if (!strcmp (rmethod->object.vtable->klass->name, "MethodBuilder")) {
#ifndef DISABLE_REFLECTION_EMIT
		MonoReflectionMethodBuilder *mb = NULL;
		MonoReflectionTypeBuilder *tb;
		MonoClass *klass;

		mb = (MonoReflectionMethodBuilder *) rmethod;
		tb = (MonoReflectionTypeBuilder *) mb->type;
		klass = mono_class_from_mono_type (mono_reflection_type_get_handle ((MonoReflectionType*)tb));

		method = methodbuilder_to_mono_method (klass, mb);
#else
		g_assert_not_reached ();
		method = NULL;
#endif
	} else {
		method = rmethod->method;
	}

	klass = method->klass;

	if (method->is_inflated)
		method = ((MonoMethodInflated *) method)->declaring;

	count = mono_method_signature (method)->generic_param_count;
	if (count != mono_array_length (types))
		return NULL;

	type_argv = g_new0 (MonoType *, count);
	for (i = 0; i < count; i++) {
		MonoReflectionType *garg = mono_array_get (types, gpointer, i);
		type_argv [i] = mono_reflection_type_get_handle (garg);
	}
	ginst = mono_metadata_get_generic_inst (count, type_argv);
	g_free (type_argv);

	tmp_context.class_inst = klass->generic_class ? klass->generic_class->context.class_inst : NULL;
	tmp_context.method_inst = ginst;

	inflated = mono_class_inflate_generic_method (method, &tmp_context);
	imethod = (MonoMethodInflated *) inflated;

	/*FIXME but I think this is no longer necessary*/
	if (method->klass->image->dynamic) {
		MonoDynamicImage *image = (MonoDynamicImage*)method->klass->image;
		/*
		 * This table maps metadata structures representing inflated methods/fields
		 * to the reflection objects representing their generic definitions.
		 */
		mono_loader_lock ();
		mono_g_hash_table_insert (image->generic_def_objects, imethod, rmethod);
		mono_loader_unlock ();
	}

	if (!mono_verifier_is_method_valid_generic_instantiation (inflated))
		mono_raise_exception (mono_get_exception_argument ("typeArguments", "Invalid generic arguments"));
	
	return mono_method_get_object (mono_object_domain (rmethod), inflated, NULL);
}

#ifndef DISABLE_REFLECTION_EMIT

static MonoMethod *
inflate_mono_method (MonoClass *klass, MonoMethod *method, MonoObject *obj)
{
	MonoMethodInflated *imethod;
	MonoGenericContext *context;
	int i;

	/*
	 * With generic code sharing the klass might not be inflated.
	 * This can happen because classes inflated with their own
	 * type arguments are "normalized" to the uninflated class.
	 */
	if (!klass->generic_class)
		return method;

	context = mono_class_get_context (klass);

	if (klass->method.count && klass->methods) {
		/* Find the already created inflated method */
		for (i = 0; i < klass->method.count; ++i) {
			g_assert (klass->methods [i]->is_inflated);
			if (((MonoMethodInflated*)klass->methods [i])->declaring == method)
				break;
		}
		g_assert (i < klass->method.count);
		imethod = (MonoMethodInflated*)klass->methods [i];
	} else {
		imethod = (MonoMethodInflated *) mono_class_inflate_generic_method_full (method, klass, context);
	}

	if (method->is_generic && method->klass->image->dynamic) {
		MonoDynamicImage *image = (MonoDynamicImage*)method->klass->image;

		mono_loader_lock ();
		mono_g_hash_table_insert (image->generic_def_objects, imethod, obj);
		mono_loader_unlock ();
	}
	return (MonoMethod *) imethod;
}

static MonoMethod *
inflate_method (MonoReflectionType *type, MonoObject *obj)
{
	MonoMethod *method;
	MonoClass *gklass;

	MonoClass *type_class = mono_object_class (type);

	if (is_sre_generic_instance (type_class)) {
		MonoReflectionGenericClass *mgc = (MonoReflectionGenericClass*)type;
		gklass = mono_class_from_mono_type (mono_reflection_type_get_handle ((MonoReflectionType*)mgc->generic_type));
	} else if (is_sre_type_builder (type_class)) {
		gklass = mono_class_from_mono_type (mono_reflection_type_get_handle (type));
	} else if (type->type) {
		gklass = mono_class_from_mono_type (type->type);
		gklass = mono_class_get_generic_type_definition (gklass);
	} else {
		g_error ("Can't handle type %s", mono_type_get_full_name (mono_object_class (type)));
	}

	if (!strcmp (obj->vtable->klass->name, "MethodBuilder"))
		if (((MonoReflectionMethodBuilder*)obj)->mhandle)
			method = ((MonoReflectionMethodBuilder*)obj)->mhandle;
		else
			method = methodbuilder_to_mono_method (gklass, (MonoReflectionMethodBuilder *) obj);
	else if (!strcmp (obj->vtable->klass->name, "ConstructorBuilder"))
		method = ctorbuilder_to_mono_method (gklass, (MonoReflectionCtorBuilder *) obj);
	else if (!strcmp (obj->vtable->klass->name, "MonoMethod") || !strcmp (obj->vtable->klass->name, "MonoCMethod"))
		method = ((MonoReflectionMethod *) obj)->method;
	else {
		method = NULL; /* prevent compiler warning */
		g_error ("can't handle type %s", obj->vtable->klass->name);
	}

	return inflate_mono_method (mono_class_from_mono_type (mono_reflection_type_get_handle ((MonoReflectionType*)type)), method, obj);
}

/*TODO avoid saving custom attrs for generic classes as it's enough to have them on the generic type definition.*/
void
mono_reflection_generic_class_initialize (MonoReflectionGenericClass *type, MonoArray *fields)
{
	MonoGenericClass *gclass;
	MonoDynamicGenericClass *dgclass;
	MonoClass *klass, *gklass;
	MonoType *gtype;
	int i;

	MONO_ARCH_SAVE_REGS;

	gtype = mono_reflection_type_get_handle ((MonoReflectionType*)type);
	klass = mono_class_from_mono_type (gtype);
	g_assert (gtype->type == MONO_TYPE_GENERICINST);
	gclass = gtype->data.generic_class;

	if (!gclass->is_dynamic)
		return;

	dgclass = (MonoDynamicGenericClass *) gclass;

	if (dgclass->initialized)
		return;

	gklass = gclass->container_class;
	mono_class_init (gklass);

	dgclass->count_fields = fields ? mono_array_length (fields) : 0;

	dgclass->fields = mono_image_set_new0 (gclass->owner, MonoClassField, dgclass->count_fields);
	dgclass->field_objects = mono_image_set_new0 (gclass->owner, MonoObject*, dgclass->count_fields);
	dgclass->field_generic_types = mono_image_set_new0 (gclass->owner, MonoType*, dgclass->count_fields);

	for (i = 0; i < dgclass->count_fields; i++) {
		MonoObject *obj = mono_array_get (fields, gpointer, i);
		MonoClassField *field, *inflated_field = NULL;

		if (!strcmp (obj->vtable->klass->name, "FieldBuilder"))
			inflated_field = field = fieldbuilder_to_mono_class_field (klass, (MonoReflectionFieldBuilder *) obj);
		else if (!strcmp (obj->vtable->klass->name, "MonoField"))
			field = ((MonoReflectionField *) obj)->field;
		else {
			field = NULL; /* prevent compiler warning */
			g_assert_not_reached ();
		}

		dgclass->fields [i] = *field;
		dgclass->fields [i].parent = klass;
		dgclass->fields [i].type = mono_class_inflate_generic_type (
			field->type, mono_generic_class_get_context ((MonoGenericClass *) dgclass));
		dgclass->field_generic_types [i] = field->type;
		MONO_GC_REGISTER_ROOT_IF_MOVING (dgclass->field_objects [i]);
		dgclass->field_objects [i] = obj;

		if (inflated_field) {
			g_free (inflated_field);
		} else {
			dgclass->fields [i].name = mono_image_set_strdup (gclass->owner, dgclass->fields [i].name);
		}
	}

	dgclass->initialized = TRUE;
}

void
mono_reflection_free_dynamic_generic_class (MonoGenericClass *gclass)
{
	MonoDynamicGenericClass *dgclass;
	int i;

	g_assert (gclass->is_dynamic);

	dgclass = (MonoDynamicGenericClass *)gclass;

	for (i = 0; i < dgclass->count_fields; ++i) {
		MonoClassField *field = dgclass->fields + i;
		mono_metadata_free_type (field->type);
		MONO_GC_UNREGISTER_ROOT_IF_MOVING (dgclass->field_objects [i]);
	}
}

static void
fix_partial_generic_class (MonoClass *klass)
{
	MonoClass *gklass = klass->generic_class->container_class;
	MonoDynamicGenericClass *dgclass;
	int i;

	if (klass->wastypebuilder)
		return;

	dgclass = (MonoDynamicGenericClass *)  klass->generic_class;
	if (klass->parent != gklass->parent) {
		MonoError error;
		MonoType *parent_type = mono_class_inflate_generic_type_checked (&gklass->parent->byval_arg, &klass->generic_class->context, &error);
		if (mono_error_ok (&error)) {
			MonoClass *parent = mono_class_from_mono_type (parent_type);
			mono_metadata_free_type (parent_type);
			if (parent != klass->parent) {
				/*fool mono_class_setup_parent*/
				klass->supertypes = NULL;
				mono_class_setup_parent (klass, parent);
			}
		} else {
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, NULL);
			mono_error_cleanup (&error);
			if (gklass->wastypebuilder)
				klass->wastypebuilder = TRUE;
			return;
		}
	}

	if (!dgclass->initialized)
		return;

	if (klass->method.count != gklass->method.count) {
		klass->method.count = gklass->method.count;
		klass->methods = mono_image_alloc (klass->image, sizeof (MonoMethod*) * (klass->method.count + 1));

		for (i = 0; i < klass->method.count; i++) {
			klass->methods [i] = mono_class_inflate_generic_method_full (
				gklass->methods [i], klass, mono_class_get_context (klass));
		}
	}

	if (klass->interface_count && klass->interface_count != gklass->interface_count) {
		klass->interface_count = gklass->interface_count;
		klass->interfaces = mono_image_alloc (klass->image, sizeof (MonoClass*) * gklass->interface_count);
		klass->interfaces_packed = NULL; /*make setup_interface_offsets happy*/

		for (i = 0; i < gklass->interface_count; ++i) {
			MonoType *iface_type = mono_class_inflate_generic_type (&gklass->interfaces [i]->byval_arg, mono_class_get_context (klass));
			klass->interfaces [i] = mono_class_from_mono_type (iface_type);
			mono_metadata_free_type (iface_type);

			ensure_runtime_vtable (klass->interfaces [i]);
		}
		klass->interfaces_inited = 1;
	}

	if (klass->field.count != gklass->field.count) {
		klass->field.count = gklass->field.count;
		klass->fields = image_g_new0 (klass->image, MonoClassField, klass->field.count);

		for (i = 0; i < klass->field.count; i++) {
			klass->fields [i] = gklass->fields [i];
			klass->fields [i].parent = klass;
			klass->fields [i].type = mono_class_inflate_generic_type (gklass->fields [i].type, mono_class_get_context (klass));
		}
	}

	/*We can only finish with this klass once it's parent has as well*/
	if (gklass->wastypebuilder)
		klass->wastypebuilder = TRUE;
	return;
}

static void
ensure_generic_class_runtime_vtable (MonoClass *klass)
{
	MonoClass *gklass = klass->generic_class->container_class;

	ensure_runtime_vtable (gklass);	

	fix_partial_generic_class (klass);
}

static void
ensure_runtime_vtable (MonoClass *klass)
{
	MonoReflectionTypeBuilder *tb = mono_class_get_ref_info (klass);
	int i, num, j;

	if (!klass->image->dynamic || (!tb && !klass->generic_class) || klass->wastypebuilder)
		return;
	if (klass->parent)
		ensure_runtime_vtable (klass->parent);

	if (tb) {
		num = tb->ctors? mono_array_length (tb->ctors): 0;
		num += tb->num_methods;
		klass->method.count = num;
		klass->methods = mono_image_alloc (klass->image, sizeof (MonoMethod*) * num);
		num = tb->ctors? mono_array_length (tb->ctors): 0;
		for (i = 0; i < num; ++i)
			klass->methods [i] = ctorbuilder_to_mono_method (klass, mono_array_get (tb->ctors, MonoReflectionCtorBuilder*, i));
		num = tb->num_methods;
		j = i;
		for (i = 0; i < num; ++i)
			klass->methods [j++] = methodbuilder_to_mono_method (klass, mono_array_get (tb->methods, MonoReflectionMethodBuilder*, i));
	
		if (tb->interfaces) {
			klass->interface_count = mono_array_length (tb->interfaces);
			klass->interfaces = mono_image_alloc (klass->image, sizeof (MonoClass*) * klass->interface_count);
			for (i = 0; i < klass->interface_count; ++i) {
				MonoType *iface = mono_type_array_get_and_resolve (tb->interfaces, i);
				klass->interfaces [i] = mono_class_from_mono_type (iface);
				ensure_runtime_vtable (klass->interfaces [i]);
			}
			klass->interfaces_inited = 1;
		}
	} else if (klass->generic_class){
		ensure_generic_class_runtime_vtable (klass);
	}

	if (klass->flags & TYPE_ATTRIBUTE_INTERFACE) {
		int slot_num = 0;
		for (i = 0; i < klass->method.count; ++i) {
			MonoMethod *im = klass->methods [i];
			if (!(im->flags & METHOD_ATTRIBUTE_STATIC))
				im->slot = slot_num++;
		}
		
		klass->interfaces_packed = NULL; /*make setup_interface_offsets happy*/
		mono_class_setup_interface_offsets (klass);
		mono_class_setup_interface_id (klass);
	}

	/*
	 * The generic vtable is needed even if image->run is not set since some
	 * runtime code like ves_icall_Type_GetMethodsByName depends on 
	 * method->slot being defined.
	 */

	/* 
	 * tb->methods could not be freed since it is used for determining 
	 * overrides during dynamic vtable construction.
	 */
}

static MonoMethod*
mono_reflection_method_get_handle (MonoObject *method)
{
	MonoClass *class = mono_object_class (method);
	if (is_sr_mono_method (class) || is_sr_mono_generic_method (class)) {
		MonoReflectionMethod *sr_method = (MonoReflectionMethod*)method;
		return sr_method->method;
	}
	if (is_sre_method_builder (class)) {
		MonoReflectionMethodBuilder *mb = (MonoReflectionMethodBuilder*)method;
		return mb->mhandle;
	}
	if (is_sre_method_on_tb_inst (class)) {
		MonoReflectionMethodOnTypeBuilderInst *m = (MonoReflectionMethodOnTypeBuilderInst*)method;
		MonoMethod *result;
		/*FIXME move this to a proper method and unify with resolve_object*/
		if (m->method_args) {
			result = mono_reflection_method_on_tb_inst_get_handle (m);
		} else {
			MonoType *type = mono_reflection_type_get_handle ((MonoReflectionType*)m->inst);
			MonoClass *inflated_klass = mono_class_from_mono_type (type);
			MonoMethod *mono_method;

			if (is_sre_method_builder (mono_object_class (m->mb)))
				mono_method = ((MonoReflectionMethodBuilder *)m->mb)->mhandle;
 			else if (is_sr_mono_method (mono_object_class (m->mb)))
				mono_method = ((MonoReflectionMethod *)m->mb)->method;
			else
				g_error ("resolve_object:: can't handle a MTBI with base_method of type %s", mono_type_get_full_name (mono_object_class (m->mb)));

			result = inflate_mono_method (inflated_klass, mono_method, (MonoObject*)m->mb);
		}
		return result;
	}

	g_error ("Can't handle methods of type %s:%s", class->name_space, class->name);
	return NULL;
}

void
mono_reflection_get_dynamic_overrides (MonoClass *klass, MonoMethod ***overrides, int *num_overrides)
{
	MonoReflectionTypeBuilder *tb;
	int i, j, onum;
	MonoReflectionMethod *m;

	*overrides = NULL;
	*num_overrides = 0;

	g_assert (klass->image->dynamic);

	if (!mono_class_get_ref_info (klass))
		return;

	g_assert (strcmp (((MonoObject*)mono_class_get_ref_info (klass))->vtable->klass->name, "TypeBuilder") == 0);

	tb = (MonoReflectionTypeBuilder*)mono_class_get_ref_info (klass);

	onum = 0;
	if (tb->methods) {
		for (i = 0; i < tb->num_methods; ++i) {
			MonoReflectionMethodBuilder *mb = 
				mono_array_get (tb->methods, MonoReflectionMethodBuilder*, i);
			if (mb->override_methods)
				onum += mono_array_length (mb->override_methods);
		}
	}

	if (onum) {
		*overrides = g_new0 (MonoMethod*, onum * 2);

		onum = 0;
		for (i = 0; i < tb->num_methods; ++i) {
			MonoReflectionMethodBuilder *mb = 
				mono_array_get (tb->methods, MonoReflectionMethodBuilder*, i);
			if (mb->override_methods) {
				for (j = 0; j < mono_array_length (mb->override_methods); ++j) {
					m = mono_array_get (mb->override_methods, MonoReflectionMethod*, j);

					(*overrides) [onum * 2] = mono_reflection_method_get_handle ((MonoObject*)m);
					(*overrides) [onum * 2 + 1] = mb->mhandle;

					g_assert (mb->mhandle);

					onum ++;
				}
			}
		}
	}

	*num_overrides = onum;
}

static void
typebuilder_setup_fields (MonoClass *klass, MonoError *error)
{
	MonoReflectionTypeBuilder *tb = mono_class_get_ref_info (klass);
	MonoReflectionFieldBuilder *fb;
	MonoClassField *field;
	MonoImage *image = klass->image;
	const char *p, *p2;
	int i;
	guint32 len, idx, real_size = 0;

	klass->field.count = tb->num_fields;
	klass->field.first = 0;

	mono_error_init (error);

	if (tb->class_size) {
		if ((tb->packing_size & 0xfffffff0) != 0) {
			char *err_msg = g_strdup_printf ("Could not load struct '%s' with packing size %d >= 16", klass->name, tb->packing_size);
			mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, err_msg);
			return;
		}
		klass->packing_size = tb->packing_size;
		real_size = klass->instance_size + tb->class_size;
	}

	if (!klass->field.count) {
		klass->instance_size = MAX (klass->instance_size, real_size);
		return;
	}
	
	klass->fields = image_g_new0 (image, MonoClassField, klass->field.count);
	mono_class_alloc_ext (klass);
	klass->ext->field_def_values = image_g_new0 (image, MonoFieldDefaultValue, klass->field.count);
	/*
	This is, guess what, a hack.
	The issue is that the runtime doesn't know how to setup the fields of a typebuider and crash.
	On the static path no field class is resolved, only types are built. This is the right thing to do
	but we suck.
	Setting size_inited is harmless because we're doing the same job as mono_class_setup_fields anyway.
	*/
	klass->size_inited = 1;

	for (i = 0; i < klass->field.count; ++i) {
		MonoArray *rva_data;
		fb = mono_array_get (tb->fields, gpointer, i);
		field = &klass->fields [i];
		field->name = mono_string_to_utf8_image (image, fb->name, error);
		if (!mono_error_ok (error))
			return;
		if (fb->attrs) {
			field->type = mono_metadata_type_dup (klass->image, mono_reflection_type_get_handle ((MonoReflectionType*)fb->type));
			field->type->attrs = fb->attrs;
		} else {
			field->type = mono_reflection_type_get_handle ((MonoReflectionType*)fb->type);
		}

		if ((fb->attrs & FIELD_ATTRIBUTE_HAS_FIELD_RVA) && (rva_data = fb->rva_data)) {
			char *base = mono_array_addr (rva_data, char, 0);
			size_t size = mono_array_length (rva_data);
			char *data = mono_image_alloc (klass->image, size);
			memcpy (data, base, size);
			klass->ext->field_def_values [i].data = data;
		}
		if (fb->offset != -1)
			field->offset = fb->offset;
		field->parent = klass;
		fb->handle = field;
		mono_save_custom_attrs (klass->image, field, fb->cattrs);

		if (klass->enumtype && !(field->type->attrs & FIELD_ATTRIBUTE_STATIC)) {
			klass->cast_class = klass->element_class = mono_class_from_mono_type (field->type);
		}
		if (fb->def_value) {
			MonoDynamicImage *assembly = (MonoDynamicImage*)klass->image;
			field->type->attrs |= FIELD_ATTRIBUTE_HAS_DEFAULT;
			idx = encode_constant (assembly, fb->def_value, &klass->ext->field_def_values [i].def_type);
			/* Copy the data from the blob since it might get realloc-ed */
			p = assembly->blob.data + idx;
			len = mono_metadata_decode_blob_size (p, &p2);
			len += p2 - p;
			klass->ext->field_def_values [i].data = mono_image_alloc (image, len);
			memcpy ((gpointer)klass->ext->field_def_values [i].data, p, len);
		}
	}

	klass->instance_size = MAX (klass->instance_size, real_size);
	mono_class_layout_fields (klass);
}

static void
typebuilder_setup_properties (MonoClass *klass, MonoError *error)
{
	MonoReflectionTypeBuilder *tb = mono_class_get_ref_info (klass);
	MonoReflectionPropertyBuilder *pb;
	MonoImage *image = klass->image;
	MonoProperty *properties;
	int i;

	mono_error_init (error);

	if (!klass->ext)
		klass->ext = image_g_new0 (image, MonoClassExt, 1);

	klass->ext->property.count = tb->properties ? mono_array_length (tb->properties) : 0;
	klass->ext->property.first = 0;

	properties = image_g_new0 (image, MonoProperty, klass->ext->property.count);
	klass->ext->properties = properties;
	for (i = 0; i < klass->ext->property.count; ++i) {
		pb = mono_array_get (tb->properties, MonoReflectionPropertyBuilder*, i);
		properties [i].parent = klass;
		properties [i].attrs = pb->attrs;
		properties [i].name = mono_string_to_utf8_image (image, pb->name, error);
		if (!mono_error_ok (error))
			return;
		if (pb->get_method)
			properties [i].get = pb->get_method->mhandle;
		if (pb->set_method)
			properties [i].set = pb->set_method->mhandle;

		mono_save_custom_attrs (klass->image, &properties [i], pb->cattrs);
		if (pb->def_value) {
			guint32 len, idx;
			const char *p, *p2;
			MonoDynamicImage *assembly = (MonoDynamicImage*)klass->image;
			if (!klass->ext->prop_def_values)
				klass->ext->prop_def_values = image_g_new0 (image, MonoFieldDefaultValue, klass->ext->property.count);
			properties [i].attrs |= PROPERTY_ATTRIBUTE_HAS_DEFAULT;
			idx = encode_constant (assembly, pb->def_value, &klass->ext->prop_def_values [i].def_type);
			/* Copy the data from the blob since it might get realloc-ed */
			p = assembly->blob.data + idx;
			len = mono_metadata_decode_blob_size (p, &p2);
			len += p2 - p;
			klass->ext->prop_def_values [i].data = mono_image_alloc (image, len);
			memcpy ((gpointer)klass->ext->prop_def_values [i].data, p, len);
		}
	}
}

MonoReflectionEvent *
mono_reflection_event_builder_get_event_info (MonoReflectionTypeBuilder *tb, MonoReflectionEventBuilder *eb)
{
	MonoEvent *event = g_new0 (MonoEvent, 1);
	MonoClass *klass;

	klass = mono_class_from_mono_type (mono_reflection_type_get_handle ((MonoReflectionType*)tb));

	event->parent = klass;
	event->attrs = eb->attrs;
	event->name = mono_string_to_utf8 (eb->name);
	if (eb->add_method)
		event->add = eb->add_method->mhandle;
	if (eb->remove_method)
		event->remove = eb->remove_method->mhandle;
	if (eb->raise_method)
		event->raise = eb->raise_method->mhandle;

#ifndef MONO_SMALL_CONFIG
	if (eb->other_methods) {
		int j;
		event->other = g_new0 (MonoMethod*, mono_array_length (eb->other_methods) + 1);
		for (j = 0; j < mono_array_length (eb->other_methods); ++j) {
			MonoReflectionMethodBuilder *mb = 
				mono_array_get (eb->other_methods,
						MonoReflectionMethodBuilder*, j);
			event->other [j] = mb->mhandle;
		}
	}
#endif

	return mono_event_get_object (mono_object_domain (tb), klass, event);
}

static void
typebuilder_setup_events (MonoClass *klass, MonoError *error)
{
	MonoReflectionTypeBuilder *tb = mono_class_get_ref_info (klass);
	MonoReflectionEventBuilder *eb;
	MonoImage *image = klass->image;
	MonoEvent *events;
	int i;

	mono_error_init (error);

	if (!klass->ext)
		klass->ext = image_g_new0 (image, MonoClassExt, 1);

	klass->ext->event.count = tb->events ? mono_array_length (tb->events) : 0;
	klass->ext->event.first = 0;

	events = image_g_new0 (image, MonoEvent, klass->ext->event.count);
	klass->ext->events = events;
	for (i = 0; i < klass->ext->event.count; ++i) {
		eb = mono_array_get (tb->events, MonoReflectionEventBuilder*, i);
		events [i].parent = klass;
		events [i].attrs = eb->attrs;
		events [i].name = mono_string_to_utf8_image (image, eb->name, error);
		if (!mono_error_ok (error))
			return;
		if (eb->add_method)
			events [i].add = eb->add_method->mhandle;
		if (eb->remove_method)
			events [i].remove = eb->remove_method->mhandle;
		if (eb->raise_method)
			events [i].raise = eb->raise_method->mhandle;

#ifndef MONO_SMALL_CONFIG
		if (eb->other_methods) {
			int j;
			events [i].other = image_g_new0 (image, MonoMethod*, mono_array_length (eb->other_methods) + 1);
			for (j = 0; j < mono_array_length (eb->other_methods); ++j) {
				MonoReflectionMethodBuilder *mb = 
					mono_array_get (eb->other_methods,
									MonoReflectionMethodBuilder*, j);
				events [i].other [j] = mb->mhandle;
			}
		}
#endif
		mono_save_custom_attrs (klass->image, &events [i], eb->cattrs);
	}
}

static gboolean
remove_instantiations_of_and_ensure_contents (gpointer key,
						  gpointer value,
						  gpointer user_data)
{
	MonoType *type = (MonoType*)key;
	MonoClass *klass = (MonoClass*)user_data;

	if ((type->type == MONO_TYPE_GENERICINST) && (type->data.generic_class->container_class == klass)) {
		fix_partial_generic_class (mono_class_from_mono_type (type)); //Ensure it's safe to use it.
		return TRUE;
	} else
		return FALSE;
}

static void
check_array_for_usertypes (MonoArray *arr)
{
	int i;

	if (!arr)
		return;

	for (i = 0; i < mono_array_length (arr); ++i)
		RESOLVE_ARRAY_TYPE_ELEMENT (arr, i);
}

MonoReflectionType*
mono_reflection_create_runtime_class (MonoReflectionTypeBuilder *tb)
{
	MonoError error;
	MonoClass *klass;
	MonoDomain* domain;
	MonoReflectionType* res;
	int i, j;

	MONO_ARCH_SAVE_REGS;

	domain = mono_object_domain (tb);
	klass = mono_class_from_mono_type (tb->type.type);

	/*
	 * Check for user defined Type subclasses.
	 */
	RESOLVE_TYPE (tb->parent);
	check_array_for_usertypes (tb->interfaces);
	if (tb->fields) {
		for (i = 0; i < mono_array_length (tb->fields); ++i) {
			MonoReflectionFieldBuilder *fb = mono_array_get (tb->fields, gpointer, i);
			if (fb) {
				RESOLVE_TYPE (fb->type);
				check_array_for_usertypes (fb->modreq);
				check_array_for_usertypes (fb->modopt);
				if (fb->marshal_info && fb->marshal_info->marshaltyperef)
					RESOLVE_TYPE (fb->marshal_info->marshaltyperef);
			}
		}
	}
	if (tb->methods) {
		for (i = 0; i < mono_array_length (tb->methods); ++i) {
			MonoReflectionMethodBuilder *mb = mono_array_get (tb->methods, gpointer, i);
			if (mb) {
				RESOLVE_TYPE (mb->rtype);
				check_array_for_usertypes (mb->return_modreq);
				check_array_for_usertypes (mb->return_modopt);
				check_array_for_usertypes (mb->parameters);
				if (mb->param_modreq)
					for (j = 0; j < mono_array_length (mb->param_modreq); ++j)
						check_array_for_usertypes (mono_array_get (mb->param_modreq, MonoArray*, j));
				if (mb->param_modopt)
					for (j = 0; j < mono_array_length (mb->param_modopt); ++j)
						check_array_for_usertypes (mono_array_get (mb->param_modopt, MonoArray*, j));
			}
		}
	}
	if (tb->ctors) {
		for (i = 0; i < mono_array_length (tb->ctors); ++i) {
			MonoReflectionCtorBuilder *mb = mono_array_get (tb->ctors, gpointer, i);
			if (mb) {
				check_array_for_usertypes (mb->parameters);
				if (mb->param_modreq)
					for (j = 0; j < mono_array_length (mb->param_modreq); ++j)
						check_array_for_usertypes (mono_array_get (mb->param_modreq, MonoArray*, j));
				if (mb->param_modopt)
					for (j = 0; j < mono_array_length (mb->param_modopt); ++j)
						check_array_for_usertypes (mono_array_get (mb->param_modopt, MonoArray*, j));
			}
		}
	}

	mono_save_custom_attrs (klass->image, klass, tb->cattrs);

	/* 
	 * we need to lock the domain because the lock will be taken inside
	 * So, we need to keep the locking order correct.
	 */
	mono_loader_lock ();
	mono_domain_lock (domain);
	if (klass->wastypebuilder) {
		mono_domain_unlock (domain);
		mono_loader_unlock ();
		return mono_type_get_object (mono_object_domain (tb), &klass->byval_arg);
	}
	/*
	 * Fields to set in klass:
	 * the various flags: delegate/unicode/contextbound etc.
	 */
	klass->flags = tb->attrs;
	klass->has_cctor = 1;
	klass->has_finalize = 1;
	klass->has_finalize_inited = 1;

	/* fool mono_class_setup_parent */
	klass->supertypes = NULL;
	mono_class_setup_parent (klass, klass->parent);
	mono_class_setup_mono_type (klass);

#if 0
	if (!((MonoDynamicImage*)klass->image)->run) {
		if (klass->generic_container) {
			/* FIXME: The code below can't handle generic classes */
			klass->wastypebuilder = TRUE;
			mono_loader_unlock ();
			mono_domain_unlock (domain);
			return mono_type_get_object (mono_object_domain (tb), &klass->byval_arg);
		}
	}
#endif

	/* enums are done right away */
	if (!klass->enumtype)
		ensure_runtime_vtable (klass);

	if (tb->subtypes) {
		for (i = 0; i < mono_array_length (tb->subtypes); ++i) {
			MonoReflectionTypeBuilder *subtb = mono_array_get (tb->subtypes, MonoReflectionTypeBuilder*, i);
			mono_class_alloc_ext (klass);
			klass->ext->nested_classes = g_list_prepend_image (klass->image, klass->ext->nested_classes, mono_class_from_mono_type (mono_reflection_type_get_handle ((MonoReflectionType*)subtb)));
		}
	}

	klass->nested_classes_inited = TRUE;

	/* fields and object layout */
	if (klass->parent) {
		if (!klass->parent->size_inited)
			mono_class_init (klass->parent);
		klass->instance_size = klass->parent->instance_size;
		klass->sizes.class_size = 0;
		klass->min_align = klass->parent->min_align;
		/* if the type has no fields we won't call the field_setup
		 * routine which sets up klass->has_references.
		 */
		klass->has_references |= klass->parent->has_references;
	} else {
		klass->instance_size = sizeof (MonoObject);
		klass->min_align = 1;
	}

	/* FIXME: handle packing_size and instance_size */
	typebuilder_setup_fields (klass, &error);
	if (!mono_error_ok (&error))
		goto failure;
	typebuilder_setup_properties (klass, &error);
	if (!mono_error_ok (&error))
		goto failure;

	typebuilder_setup_events (klass, &error);
	if (!mono_error_ok (&error))
		goto failure;

	klass->wastypebuilder = TRUE;

	/* 
	 * If we are a generic TypeBuilder, there might be instantiations in the type cache
	 * which have type System.Reflection.MonoGenericClass, but after the type is created, 
	 * we want to return normal System.MonoType objects, so clear these out from the cache.
	 *
	 * Together with this we must ensure the contents of all instances to match the created type.
	 */
	if (domain->type_hash && klass->generic_container)
		mono_g_hash_table_foreach_remove (domain->type_hash, remove_instantiations_of_and_ensure_contents, klass);

	mono_domain_unlock (domain);
	mono_loader_unlock ();

	if (klass->enumtype && !mono_class_is_valid_enum (klass)) {
		mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, NULL);
		mono_raise_exception (mono_get_exception_type_load (tb->name, NULL));
	}

	res = mono_type_get_object (mono_object_domain (tb), &klass->byval_arg);
	g_assert (res != (MonoReflectionType*)tb);

	return res;

failure:
	mono_class_set_failure (klass, MONO_EXCEPTION_TYPE_LOAD, NULL);
	klass->wastypebuilder = TRUE;
	mono_domain_unlock (domain);
	mono_loader_unlock ();
	mono_error_raise_exception (&error);
	return NULL;
}

void
mono_reflection_initialize_generic_parameter (MonoReflectionGenericParam *gparam)
{
	MonoGenericParamFull *param;
	MonoImage *image;
	MonoClass *pklass;
	MonoError error;

	MONO_ARCH_SAVE_REGS;

	image = &gparam->tbuilder->module->dynamic_image->image;

	param = mono_image_new0 (image, MonoGenericParamFull, 1);

	param->info.name = mono_string_to_utf8_image (image, gparam->name, &error);
	g_assert (mono_error_ok (&error));
	param->param.num = gparam->index;

	if (gparam->mbuilder) {
		if (!gparam->mbuilder->generic_container) {
			MonoReflectionTypeBuilder *tb = (MonoReflectionTypeBuilder *)gparam->mbuilder->type;
			MonoClass *klass = mono_class_from_mono_type (mono_reflection_type_get_handle ((MonoReflectionType*)tb));
			gparam->mbuilder->generic_container = mono_image_alloc0 (klass->image, sizeof (MonoGenericContainer));
			gparam->mbuilder->generic_container->is_method = TRUE;
			/* 
			 * Cannot set owner.method, since the MonoMethod is not created yet.
			 * Set the image field instead, so type_in_image () works.
			 */
			gparam->mbuilder->generic_container->image = klass->image;
		}
		param->param.owner = gparam->mbuilder->generic_container;
	} else if (gparam->tbuilder) {
		if (!gparam->tbuilder->generic_container) {
			MonoClass *klass = mono_class_from_mono_type (mono_reflection_type_get_handle ((MonoReflectionType*)gparam->tbuilder));
			gparam->tbuilder->generic_container = mono_image_alloc0 (klass->image, sizeof (MonoGenericContainer));
			gparam->tbuilder->generic_container->owner.klass = klass;
		}
		param->param.owner = gparam->tbuilder->generic_container;
	}

	pklass = mono_class_from_generic_parameter ((MonoGenericParam *) param, image, gparam->mbuilder != NULL);

	gparam->type.type = &pklass->byval_arg;

	mono_class_set_ref_info (pklass, gparam);
	mono_image_lock (image);
	image->reflection_info_unregister_classes = g_slist_prepend (image->reflection_info_unregister_classes, pklass);
	mono_image_unlock (image);
}

MonoArray *
mono_reflection_sighelper_get_signature_local (MonoReflectionSigHelper *sig)
{
	MonoReflectionModuleBuilder *module = sig->module;
	MonoDynamicImage *assembly = module != NULL ? module->dynamic_image : NULL;
	guint32 na = sig->arguments ? mono_array_length (sig->arguments) : 0;
	guint32 buflen, i;
	MonoArray *result;
	SigBuffer buf;

	check_array_for_usertypes (sig->arguments);

	sigbuffer_init (&buf, 32);

	sigbuffer_add_value (&buf, 0x07);
	sigbuffer_add_value (&buf, na);
	if (assembly != NULL){
		for (i = 0; i < na; ++i) {
			MonoReflectionType *type = mono_array_get (sig->arguments, MonoReflectionType*, i);
			encode_reflection_type (assembly, type, &buf);
		}
	}

	buflen = buf.p - buf.buf;
	result = mono_array_new (mono_domain_get (), mono_defaults.byte_class, buflen);
	memcpy (mono_array_addr (result, char, 0), buf.buf, buflen);
	sigbuffer_free (&buf);

	return result;
}

MonoArray *
mono_reflection_sighelper_get_signature_field (MonoReflectionSigHelper *sig)
{
	MonoDynamicImage *assembly = sig->module->dynamic_image;
	guint32 na = sig->arguments ? mono_array_length (sig->arguments) : 0;
	guint32 buflen, i;
	MonoArray *result;
	SigBuffer buf;

	check_array_for_usertypes (sig->arguments);

	sigbuffer_init (&buf, 32);

	sigbuffer_add_value (&buf, 0x06);
	for (i = 0; i < na; ++i) {
		MonoReflectionType *type = mono_array_get (sig->arguments, MonoReflectionType*, i);
		encode_reflection_type (assembly, type, &buf);
	}

	buflen = buf.p - buf.buf;
	result = mono_array_new (mono_domain_get (), mono_defaults.byte_class, buflen);
	memcpy (mono_array_addr (result, char, 0), buf.buf, buflen);
	sigbuffer_free (&buf);

	return result;
}

typedef struct {
	MonoMethod *handle;
	MonoDomain *domain;
} DynamicMethodReleaseData;

/*
 * The runtime automatically clean up those after finalization.
*/	
static MonoReferenceQueue *dynamic_method_queue;

static void
free_dynamic_method (void *dynamic_method)
{
	DynamicMethodReleaseData *data = dynamic_method;

	mono_runtime_free_method (data->domain, data->handle);
	g_free (data);
}

void 
mono_reflection_create_dynamic_method (MonoReflectionDynamicMethod *mb)
{
	MonoReferenceQueue *queue;
	MonoMethod *handle;
	DynamicMethodReleaseData *release_data;
	ReflectionMethodBuilder rmb;
	MonoMethodSignature *sig;
	MonoClass *klass;
	GSList *l;
	int i;

	if (mono_runtime_is_shutting_down ())
		mono_raise_exception (mono_get_exception_invalid_operation (""));

	if (!(queue = dynamic_method_queue)) {
		mono_loader_lock ();
		if (!(queue = dynamic_method_queue))
			queue = dynamic_method_queue = mono_gc_reference_queue_new (free_dynamic_method);
		mono_loader_unlock ();
	}

	sig = dynamic_method_to_signature (mb);

	reflection_methodbuilder_from_dynamic_method (&rmb, mb);

	/*
	 * Resolve references.
	 */
	/* 
	 * Every second entry in the refs array is reserved for storing handle_class,
	 * which is needed by the ldtoken implementation in the JIT.
	 */
	rmb.nrefs = mb->nrefs;
	rmb.refs = g_new0 (gpointer, mb->nrefs + 1);
	for (i = 0; i < mb->nrefs; i += 2) {
		MonoClass *handle_class;
		gpointer ref;
		MonoObject *obj = mono_array_get (mb->refs, MonoObject*, i);

		if (strcmp (obj->vtable->klass->name, "DynamicMethod") == 0) {
			MonoReflectionDynamicMethod *method = (MonoReflectionDynamicMethod*)obj;
			/*
			 * The referenced DynamicMethod should already be created by the managed
			 * code, except in the case of circular references. In that case, we store
			 * method in the refs array, and fix it up later when the referenced 
			 * DynamicMethod is created.
			 */
			if (method->mhandle) {
				ref = method->mhandle;
			} else {
				/* FIXME: GC object stored in unmanaged memory */
				ref = method;

				/* FIXME: GC object stored in unmanaged memory */
				method->referenced_by = g_slist_append (method->referenced_by, mb);
			}
			handle_class = mono_defaults.methodhandle_class;
		} else {
			MonoException *ex = NULL;
			ref = resolve_object (mb->module->image, obj, &handle_class, NULL);
			if (!ref)
				ex = mono_get_exception_type_load (NULL, NULL);
			else if (mono_security_core_clr_enabled ())
				ex = mono_security_core_clr_ensure_dynamic_method_resolved_object (ref, handle_class);

			if (ex) {
				g_free (rmb.refs);
				mono_raise_exception (ex);
				return;
			}
		}

		rmb.refs [i] = ref; /* FIXME: GC object stored in unmanaged memory (change also resolve_object() signature) */
		rmb.refs [i + 1] = handle_class;
	}		

	klass = mb->owner ? mono_class_from_mono_type (mono_reflection_type_get_handle ((MonoReflectionType*)mb->owner)) : mono_defaults.object_class;

	mb->mhandle = handle = reflection_methodbuilder_to_mono_method (klass, &rmb, sig);
	release_data = g_new (DynamicMethodReleaseData, 1);
	release_data->handle = handle;
	release_data->domain = mono_object_get_domain ((MonoObject*)mb);
	if (!mono_gc_reference_queue_add (queue, (MonoObject*)mb, release_data))
		g_free (release_data);

	/* Fix up refs entries pointing at us */
	for (l = mb->referenced_by; l; l = l->next) {
		MonoReflectionDynamicMethod *method = (MonoReflectionDynamicMethod*)l->data;
		MonoMethodWrapper *wrapper = (MonoMethodWrapper*)method->mhandle;
		gpointer *data;
		
		g_assert (method->mhandle);

		data = (gpointer*)wrapper->method_data;
		for (i = 0; i < GPOINTER_TO_UINT (data [0]); i += 2) {
			if ((data [i + 1] == mb) && (data [i + 1 + 1] == mono_defaults.methodhandle_class))
				data [i + 1] = mb->mhandle;
		}
	}
	g_slist_free (mb->referenced_by);

	g_free (rmb.refs);

	/* ilgen is no longer needed */
	mb->ilgen = NULL;
}

#endif /* DISABLE_REFLECTION_EMIT */

/**
 * 
 * mono_reflection_is_valid_dynamic_token:
 * 
 * Returns TRUE if token is valid.
 * 
 */
gboolean
mono_reflection_is_valid_dynamic_token (MonoDynamicImage *image, guint32 token)
{
	return mono_g_hash_table_lookup (image->tokens, GUINT_TO_POINTER (token)) != NULL;
}

MonoMethodSignature *
mono_reflection_lookup_signature (MonoImage *image, MonoMethod *method, guint32 token)
{
	MonoMethodSignature *sig;
	g_assert (image->dynamic);

	sig = g_hash_table_lookup (((MonoDynamicImage*)image)->vararg_aux_hash, GUINT_TO_POINTER (token));
	if (sig)
		return sig;

	return mono_method_signature (method);
}

#ifndef DISABLE_REFLECTION_EMIT

/**
 * mono_reflection_lookup_dynamic_token:
 *
 * Finish the Builder object pointed to by TOKEN and return the corresponding
 * runtime structure. If HANDLE_CLASS is not NULL, it is set to the class required by 
 * mono_ldtoken. If valid_token is TRUE, assert if it is not found in the token->object
 * mapping table.
 *
 * LOCKING: Take the loader lock
 */
gpointer
mono_reflection_lookup_dynamic_token (MonoImage *image, guint32 token, gboolean valid_token, MonoClass **handle_class, MonoGenericContext *context)
{
	MonoDynamicImage *assembly = (MonoDynamicImage*)image;
	MonoObject *obj;
	MonoClass *klass;

	mono_loader_lock ();
	obj = mono_g_hash_table_lookup (assembly->tokens, GUINT_TO_POINTER (token));
	mono_loader_unlock ();
	if (!obj) {
		if (valid_token)
			g_error ("Could not find required dynamic token 0x%08x", token);
		else
			return NULL;
	}

	if (!handle_class)
		handle_class = &klass;
	return resolve_object (image, obj, handle_class, context);
}

/*
 * ensure_complete_type:
 *
 *   Ensure that KLASS is completed if it is a dynamic type, or references
 * dynamic types.
 */
static void
ensure_complete_type (MonoClass *klass)
{
	if (klass->image->dynamic && !klass->wastypebuilder && mono_class_get_ref_info (klass)) {
		MonoReflectionTypeBuilder *tb = mono_class_get_ref_info (klass);

		mono_domain_try_type_resolve (mono_domain_get (), NULL, (MonoObject*)tb);

		// Asserting here could break a lot of code
		//g_assert (klass->wastypebuilder);
	}

	if (klass->generic_class) {
		MonoGenericInst *inst = klass->generic_class->context.class_inst;
		int i;

		for (i = 0; i < inst->type_argc; ++i) {
			ensure_complete_type (mono_class_from_mono_type (inst->type_argv [i]));
		}
	}
}

static gpointer
resolve_object (MonoImage *image, MonoObject *obj, MonoClass **handle_class, MonoGenericContext *context)
{
	gpointer result = NULL;

	if (strcmp (obj->vtable->klass->name, "String") == 0) {
		result = mono_string_intern ((MonoString*)obj);
		*handle_class = mono_defaults.string_class;
		g_assert (result);
	} else if (strcmp (obj->vtable->klass->name, "MonoType") == 0) {
		MonoType *type = mono_reflection_type_get_handle ((MonoReflectionType*)obj);
		MonoClass *mc = mono_class_from_mono_type (type);
		if (!mono_class_init (mc))
			mono_raise_exception (mono_class_get_exception_for_failure (mc));

		if (context) {
			MonoType *inflated = mono_class_inflate_generic_type (type, context);
			result = mono_class_from_mono_type (inflated);
			mono_metadata_free_type (inflated);
		} else {
			result = mono_class_from_mono_type (type);
		}
		*handle_class = mono_defaults.typehandle_class;
		g_assert (result);
	} else if (strcmp (obj->vtable->klass->name, "MonoMethod") == 0 ||
		   strcmp (obj->vtable->klass->name, "MonoCMethod") == 0 ||
		   strcmp (obj->vtable->klass->name, "MonoGenericCMethod") == 0 ||
		   strcmp (obj->vtable->klass->name, "MonoGenericMethod") == 0) {
		result = ((MonoReflectionMethod*)obj)->method;
		if (context)
			result = mono_class_inflate_generic_method (result, context);
		*handle_class = mono_defaults.methodhandle_class;
		g_assert (result);
	} else if (strcmp (obj->vtable->klass->name, "MethodBuilder") == 0) {
		MonoReflectionMethodBuilder *mb = (MonoReflectionMethodBuilder*)obj;
		result = mb->mhandle;
		if (!result) {
			/* Type is not yet created */
			MonoReflectionTypeBuilder *tb = (MonoReflectionTypeBuilder*)mb->type;

			mono_domain_try_type_resolve (mono_domain_get (), NULL, (MonoObject*)tb);

			/*
			 * Hopefully this has been filled in by calling CreateType() on the
			 * TypeBuilder.
			 */
			/*
			 * TODO: This won't work if the application finishes another 
			 * TypeBuilder instance instead of this one.
			 */
			result = mb->mhandle;
		}
		if (context)
			result = mono_class_inflate_generic_method (result, context);
		*handle_class = mono_defaults.methodhandle_class;
	} else if (strcmp (obj->vtable->klass->name, "ConstructorBuilder") == 0) {
		MonoReflectionCtorBuilder *cb = (MonoReflectionCtorBuilder*)obj;

		result = cb->mhandle;
		if (!result) {
			MonoReflectionTypeBuilder *tb = (MonoReflectionTypeBuilder*)cb->type;

			mono_domain_try_type_resolve (mono_domain_get (), NULL, (MonoObject*)tb);
			result = cb->mhandle;
		}
		if (context)
			result = mono_class_inflate_generic_method (result, context);
		*handle_class = mono_defaults.methodhandle_class;
	} else if (strcmp (obj->vtable->klass->name, "MonoField") == 0) {
		MonoClassField *field = ((MonoReflectionField*)obj)->field;

		ensure_complete_type (field->parent);
		if (context) {
			MonoType *inflated = mono_class_inflate_generic_type (&field->parent->byval_arg, context);
			MonoClass *class = mono_class_from_mono_type (inflated);
			MonoClassField *inflated_field;
			gpointer iter = NULL;
			mono_metadata_free_type (inflated);
			while ((inflated_field = mono_class_get_fields (class, &iter))) {
				if (!strcmp (field->name, inflated_field->name))
					break;
			}
			g_assert (inflated_field && !strcmp (field->name, inflated_field->name));
			result = inflated_field;
		} else {
			result = field;
		}
		*handle_class = mono_defaults.fieldhandle_class;
		g_assert (result);
	} else if (strcmp (obj->vtable->klass->name, "FieldBuilder") == 0) {
		MonoReflectionFieldBuilder *fb = (MonoReflectionFieldBuilder*)obj;
		result = fb->handle;

		if (!result) {
			MonoReflectionTypeBuilder *tb = (MonoReflectionTypeBuilder*)fb->typeb;

			mono_domain_try_type_resolve (mono_domain_get (), NULL, (MonoObject*)tb);
			result = fb->handle;
		}

		if (fb->handle && fb->handle->parent->generic_container) {
			MonoClass *klass = fb->handle->parent;
			MonoType *type = mono_class_inflate_generic_type (&klass->byval_arg, context);
			MonoClass *inflated = mono_class_from_mono_type (type);

			result = mono_class_get_field_from_name (inflated, mono_field_get_name (fb->handle));
			g_assert (result);
			mono_metadata_free_type (type);
		}
		*handle_class = mono_defaults.fieldhandle_class;
	} else if (strcmp (obj->vtable->klass->name, "TypeBuilder") == 0) {
		MonoReflectionTypeBuilder *tb = (MonoReflectionTypeBuilder*)obj;
		MonoType *type = mono_reflection_type_get_handle ((MonoReflectionType*)tb); 
		MonoClass *klass;

		klass = type->data.klass;
		if (klass->wastypebuilder) {
			/* Already created */
			result = klass;
		}
		else {
			mono_domain_try_type_resolve (mono_domain_get (), NULL, (MonoObject*)tb);
			result = type->data.klass;
			g_assert (result);
		}
		*handle_class = mono_defaults.typehandle_class;
	} else if (strcmp (obj->vtable->klass->name, "SignatureHelper") == 0) {
		MonoReflectionSigHelper *helper = (MonoReflectionSigHelper*)obj;
		MonoMethodSignature *sig;
		int nargs, i;

		if (helper->arguments)
			nargs = mono_array_length (helper->arguments);
		else
			nargs = 0;

		sig = mono_metadata_signature_alloc (image, nargs);
		sig->explicit_this = helper->call_conv & 64 ? 1 : 0;
		sig->hasthis = helper->call_conv & 32 ? 1 : 0;

		if (helper->unmanaged_call_conv) { /* unmanaged */
			sig->call_convention = helper->unmanaged_call_conv - 1;
			sig->pinvoke = TRUE;
		} else if (helper->call_conv & 0x02) {
			sig->call_convention = MONO_CALL_VARARG;
		} else {
			sig->call_convention = MONO_CALL_DEFAULT;
		}

		sig->param_count = nargs;
		/* TODO: Copy type ? */
		sig->ret = helper->return_type->type;
		for (i = 0; i < nargs; ++i)
			sig->params [i] = mono_type_array_get_and_resolve (helper->arguments, i);

		result = sig;
		*handle_class = NULL;
	} else if (strcmp (obj->vtable->klass->name, "DynamicMethod") == 0) {
		MonoReflectionDynamicMethod *method = (MonoReflectionDynamicMethod*)obj;
		/* Already created by the managed code */
		g_assert (method->mhandle);
		result = method->mhandle;
		*handle_class = mono_defaults.methodhandle_class;
	} else if (strcmp (obj->vtable->klass->name, "GenericTypeParameterBuilder") == 0) {
		MonoType *type = mono_reflection_type_get_handle ((MonoReflectionType*)obj);
		type = mono_class_inflate_generic_type (type, context);
		result = mono_class_from_mono_type (type);
		*handle_class = mono_defaults.typehandle_class;
		g_assert (result);
		mono_metadata_free_type (type);
	} else if (strcmp (obj->vtable->klass->name, "MonoGenericClass") == 0) {
		MonoType *type = mono_reflection_type_get_handle ((MonoReflectionType*)obj);
		type = mono_class_inflate_generic_type (type, context);
		result = mono_class_from_mono_type (type);
		*handle_class = mono_defaults.typehandle_class;
		g_assert (result);
		mono_metadata_free_type (type);
	} else if (strcmp (obj->vtable->klass->name, "FieldOnTypeBuilderInst") == 0) {
		MonoReflectionFieldOnTypeBuilderInst *f = (MonoReflectionFieldOnTypeBuilderInst*)obj;
		MonoClass *inflated;
		MonoType *type;
		MonoClassField *field;

		if (is_sre_field_builder (mono_object_class (f->fb)))
			field = ((MonoReflectionFieldBuilder*)f->fb)->handle;
		else if (is_sr_mono_field (mono_object_class (f->fb)))
			field = ((MonoReflectionField*)f->fb)->field;
		else
			g_error ("resolve_object:: can't handle a FTBI with base_method of type %s", mono_type_get_full_name (mono_object_class (f->fb)));

		type = mono_class_inflate_generic_type (mono_reflection_type_get_handle ((MonoReflectionType*)f->inst), context);
		inflated = mono_class_from_mono_type (type);

		result = field = mono_class_get_field_from_name (inflated, mono_field_get_name (field));
		ensure_complete_type (field->parent);
		g_assert (result);
		mono_metadata_free_type (type);
		*handle_class = mono_defaults.fieldhandle_class;
	} else if (strcmp (obj->vtable->klass->name, "ConstructorOnTypeBuilderInst") == 0) {
		MonoReflectionCtorOnTypeBuilderInst *c = (MonoReflectionCtorOnTypeBuilderInst*)obj;
		MonoType *type = mono_class_inflate_generic_type (mono_reflection_type_get_handle ((MonoReflectionType*)c->inst), context);
		MonoClass *inflated_klass = mono_class_from_mono_type (type);
		MonoMethod *method;

		if (is_sre_ctor_builder (mono_object_class (c->cb)))
			method = ((MonoReflectionCtorBuilder *)c->cb)->mhandle;
		else if (is_sr_mono_cmethod (mono_object_class (c->cb)))
			method = ((MonoReflectionMethod *)c->cb)->method;
		else
			g_error ("resolve_object:: can't handle a CTBI with base_method of type %s", mono_type_get_full_name (mono_object_class (c->cb)));

		result = inflate_mono_method (inflated_klass, method, (MonoObject*)c->cb);
		*handle_class = mono_defaults.methodhandle_class;
		mono_metadata_free_type (type);
	} else if (strcmp (obj->vtable->klass->name, "MethodOnTypeBuilderInst") == 0) {
		MonoReflectionMethodOnTypeBuilderInst *m = (MonoReflectionMethodOnTypeBuilderInst*)obj;
		if (m->method_args) {
			result = mono_reflection_method_on_tb_inst_get_handle (m);
			if (context)
				result = mono_class_inflate_generic_method (result, context);
		} else {
			MonoType *type = mono_class_inflate_generic_type (mono_reflection_type_get_handle ((MonoReflectionType*)m->inst), context);
			MonoClass *inflated_klass = mono_class_from_mono_type (type);
			MonoMethod *method;

			if (is_sre_method_builder (mono_object_class (m->mb)))
				method = ((MonoReflectionMethodBuilder *)m->mb)->mhandle;
 			else if (is_sr_mono_method (mono_object_class (m->mb)))
				method = ((MonoReflectionMethod *)m->mb)->method;
			else
				g_error ("resolve_object:: can't handle a MTBI with base_method of type %s", mono_type_get_full_name (mono_object_class (m->mb)));

			result = inflate_mono_method (inflated_klass, method, (MonoObject*)m->mb);
			mono_metadata_free_type (type);
		}
		*handle_class = mono_defaults.methodhandle_class;
	} else if (strcmp (obj->vtable->klass->name, "MonoArrayMethod") == 0) {
		MonoReflectionArrayMethod *m = (MonoReflectionArrayMethod*)obj;
		MonoType *mtype;
		MonoClass *klass;
		MonoMethod *method;
		gpointer iter;
		char *name;

		mtype = mono_reflection_type_get_handle (m->parent);
		klass = mono_class_from_mono_type (mtype);

		/* Find the method */

		name = mono_string_to_utf8 (m->name);
		iter = NULL;
		while ((method = mono_class_get_methods (klass, &iter))) {
			if (!strcmp (method->name, name))
				break;
		}
		g_free (name);

		// FIXME:
		g_assert (method);
		// FIXME: Check parameters/return value etc. match

		result = method;
		*handle_class = mono_defaults.methodhandle_class;
	} else if (is_sre_array (mono_object_get_class(obj)) ||
				is_sre_byref (mono_object_get_class(obj)) ||
				is_sre_pointer (mono_object_get_class(obj))) {
		MonoReflectionType *ref_type = (MonoReflectionType *)obj;
		MonoType *type = mono_reflection_type_get_handle (ref_type);

		if (context) {
			MonoType *inflated = mono_class_inflate_generic_type (type, context);
			result = mono_class_from_mono_type (inflated);
			mono_metadata_free_type (inflated);
		} else {
			result = mono_class_from_mono_type (type);
		}
		*handle_class = mono_defaults.typehandle_class;
	} else {
		g_print ("%s\n", obj->vtable->klass->name);
		g_assert_not_reached ();
	}
	return result;
}

#else /* DISABLE_REFLECTION_EMIT */

MonoArray*
mono_reflection_get_custom_attrs_blob (MonoReflectionAssembly *assembly, MonoObject *ctor, MonoArray *ctorArgs, MonoArray *properties, MonoArray *propValues, MonoArray *fields, MonoArray* fieldValues) 
{
	g_assert_not_reached ();
	return NULL;
}

void
mono_reflection_setup_internal_class (MonoReflectionTypeBuilder *tb)
{
	g_assert_not_reached ();
}

void
mono_reflection_setup_generic_class (MonoReflectionTypeBuilder *tb)
{
	g_assert_not_reached ();
}

void
mono_reflection_create_generic_class (MonoReflectionTypeBuilder *tb)
{
	g_assert_not_reached ();
}

void
mono_reflection_create_internal_class (MonoReflectionTypeBuilder *tb)
{
	g_assert_not_reached ();
}

void
mono_image_basic_init (MonoReflectionAssemblyBuilder *assemblyb)
{
	g_error ("This mono runtime was configured with --enable-minimal=reflection_emit, so System.Reflection.Emit is not supported.");
}

void
mono_image_module_basic_init (MonoReflectionModuleBuilder *moduleb)
{
	g_assert_not_reached ();
}

void
mono_image_set_wrappers_type (MonoReflectionModuleBuilder *moduleb, MonoReflectionType *type)
{
	g_assert_not_reached ();
}

MonoReflectionModule *
mono_image_load_module_dynamic (MonoReflectionAssemblyBuilder *ab, MonoString *fileName)
{
	g_assert_not_reached ();
	return NULL;
}

guint32
mono_image_insert_string (MonoReflectionModuleBuilder *module, MonoString *str)
{
	g_assert_not_reached ();
	return 0;
}

guint32
mono_image_create_method_token (MonoDynamicImage *assembly, MonoObject *obj, MonoArray *opt_param_types)
{
	g_assert_not_reached ();
	return 0;
}

guint32
mono_image_create_token (MonoDynamicImage *assembly, MonoObject *obj, 
						 gboolean create_open_instance, gboolean register_token)
{
	g_assert_not_reached ();
	return 0;
}

void
mono_image_register_token (MonoDynamicImage *assembly, guint32 token, MonoObject *obj)
{
}

void
mono_reflection_generic_class_initialize (MonoReflectionGenericClass *type, MonoArray *fields)
{
	g_assert_not_reached ();
}

void
mono_reflection_get_dynamic_overrides (MonoClass *klass, MonoMethod ***overrides, int *num_overrides)
{
	*overrides = NULL;
	*num_overrides = 0;
}

MonoReflectionEvent *
mono_reflection_event_builder_get_event_info (MonoReflectionTypeBuilder *tb, MonoReflectionEventBuilder *eb)
{
	g_assert_not_reached ();
	return NULL;
}

MonoReflectionType*
mono_reflection_create_runtime_class (MonoReflectionTypeBuilder *tb)
{
	g_assert_not_reached ();
	return NULL;
}

void
mono_reflection_initialize_generic_parameter (MonoReflectionGenericParam *gparam)
{
	g_assert_not_reached ();
}

MonoArray *
mono_reflection_sighelper_get_signature_local (MonoReflectionSigHelper *sig)
{
	g_assert_not_reached ();
	return NULL;
}

MonoArray *
mono_reflection_sighelper_get_signature_field (MonoReflectionSigHelper *sig)
{
	g_assert_not_reached ();
	return NULL;
}

void 
mono_reflection_create_dynamic_method (MonoReflectionDynamicMethod *mb)
{
}

gpointer
mono_reflection_lookup_dynamic_token (MonoImage *image, guint32 token, gboolean valid_token, MonoClass **handle_class, MonoGenericContext *context)
{
	return NULL;
}

MonoType*
mono_reflection_type_get_handle (MonoReflectionType* ref)
{
	if (!ref)
		return NULL;
	return ref->type;
}

void
mono_reflection_free_dynamic_generic_class (MonoGenericClass *gclass)
{
	g_assert_not_reached ();
}

#endif /* DISABLE_REFLECTION_EMIT */

/* SECURITY_ACTION_* are defined in mono/metadata/tabledefs.h */
const static guint32 declsec_flags_map[] = {
	0x00000000,					/* empty */
	MONO_DECLSEC_FLAG_REQUEST,			/* SECURITY_ACTION_REQUEST			(x01) */
	MONO_DECLSEC_FLAG_DEMAND,			/* SECURITY_ACTION_DEMAND			(x02) */
	MONO_DECLSEC_FLAG_ASSERT,			/* SECURITY_ACTION_ASSERT			(x03) */
	MONO_DECLSEC_FLAG_DENY,				/* SECURITY_ACTION_DENY				(x04) */
	MONO_DECLSEC_FLAG_PERMITONLY,			/* SECURITY_ACTION_PERMITONLY			(x05) */
	MONO_DECLSEC_FLAG_LINKDEMAND,			/* SECURITY_ACTION_LINKDEMAND			(x06) */
	MONO_DECLSEC_FLAG_INHERITANCEDEMAND,		/* SECURITY_ACTION_INHERITANCEDEMAND		(x07) */
	MONO_DECLSEC_FLAG_REQUEST_MINIMUM,		/* SECURITY_ACTION_REQUEST_MINIMUM		(x08) */
	MONO_DECLSEC_FLAG_REQUEST_OPTIONAL,		/* SECURITY_ACTION_REQUEST_OPTIONAL		(x09) */
	MONO_DECLSEC_FLAG_REQUEST_REFUSE,		/* SECURITY_ACTION_REQUEST_REFUSE		(x0A) */
	MONO_DECLSEC_FLAG_PREJIT_GRANT,			/* SECURITY_ACTION_PREJIT_GRANT			(x0B) */
	MONO_DECLSEC_FLAG_PREJIT_DENY,			/* SECURITY_ACTION_PREJIT_DENY			(x0C) */
	MONO_DECLSEC_FLAG_NONCAS_DEMAND,		/* SECURITY_ACTION_NONCAS_DEMAND		(x0D) */
	MONO_DECLSEC_FLAG_NONCAS_LINKDEMAND,		/* SECURITY_ACTION_NONCAS_LINKDEMAND		(x0E) */
	MONO_DECLSEC_FLAG_NONCAS_INHERITANCEDEMAND,	/* SECURITY_ACTION_NONCAS_INHERITANCEDEMAND	(x0F) */
	MONO_DECLSEC_FLAG_LINKDEMAND_CHOICE,		/* SECURITY_ACTION_LINKDEMAND_CHOICE		(x10) */
	MONO_DECLSEC_FLAG_INHERITANCEDEMAND_CHOICE,	/* SECURITY_ACTION_INHERITANCEDEMAND_CHOICE	(x11) */
	MONO_DECLSEC_FLAG_DEMAND_CHOICE,		/* SECURITY_ACTION_DEMAND_CHOICE		(x12) */
};

/*
 * Returns flags that includes all available security action associated to the handle.
 * @token: metadata token (either for a class or a method)
 * @image: image where resides the metadata.
 */
static guint32
mono_declsec_get_flags (MonoImage *image, guint32 token)
{
	int index = mono_metadata_declsec_from_index (image, token);
	MonoTableInfo *t = &image->tables [MONO_TABLE_DECLSECURITY];
	guint32 result = 0;
	guint32 action;
	int i;

	/* HasSecurity can be present for other, not specially encoded, attributes,
	   e.g. SuppressUnmanagedCodeSecurityAttribute */
	if (index < 0)
		return 0;

	for (i = index; i < t->rows; i++) {
		guint32 cols [MONO_DECL_SECURITY_SIZE];

		mono_metadata_decode_row (t, i, cols, MONO_DECL_SECURITY_SIZE);
		if (cols [MONO_DECL_SECURITY_PARENT] != token)
			break;

		action = cols [MONO_DECL_SECURITY_ACTION];
		if ((action >= MONO_DECLSEC_ACTION_MIN) && (action <= MONO_DECLSEC_ACTION_MAX)) {
			result |= declsec_flags_map [action];
		} else {
			g_assert_not_reached ();
		}
	}
	return result;
}

/*
 * Get the security actions (in the form of flags) associated with the specified method.
 *
 * @method: The method for which we want the declarative security flags.
 * Return the declarative security flags for the method (only).
 *
 * Note: To keep MonoMethod size down we do not cache the declarative security flags
 *       (except for the stack modifiers which are kept in the MonoJitInfo structure)
 */
guint32
mono_declsec_flags_from_method (MonoMethod *method)
{
	if (method->flags & METHOD_ATTRIBUTE_HAS_SECURITY) {
		/* FIXME: No cache (for the moment) */
		guint32 idx = mono_method_get_index (method);
		idx <<= MONO_HAS_DECL_SECURITY_BITS;
		idx |= MONO_HAS_DECL_SECURITY_METHODDEF;
		return mono_declsec_get_flags (method->klass->image, idx);
	}
	return 0;
}

/*
 * Get the security actions (in the form of flags) associated with the specified class.
 *
 * @klass: The class for which we want the declarative security flags.
 * Return the declarative security flags for the class.
 *
 * Note: We cache the flags inside the MonoClass structure as this will get 
 *       called very often (at least for each method).
 */
guint32
mono_declsec_flags_from_class (MonoClass *klass)
{
	if (klass->flags & TYPE_ATTRIBUTE_HAS_SECURITY) {
		if (!klass->ext || !klass->ext->declsec_flags) {
			guint32 idx;

			idx = mono_metadata_token_index (klass->type_token);
			idx <<= MONO_HAS_DECL_SECURITY_BITS;
			idx |= MONO_HAS_DECL_SECURITY_TYPEDEF;
			mono_loader_lock ();
			mono_class_alloc_ext (klass);
			mono_loader_unlock ();
			/* we cache the flags on classes */
			klass->ext->declsec_flags = mono_declsec_get_flags (klass->image, idx);
		}
		return klass->ext->declsec_flags;
	}
	return 0;
}

/*
 * Get the security actions (in the form of flags) associated with the specified assembly.
 *
 * @assembly: The assembly for which we want the declarative security flags.
 * Return the declarative security flags for the assembly.
 */
guint32
mono_declsec_flags_from_assembly (MonoAssembly *assembly)
{
	guint32 idx = 1; /* there is only one assembly */
	idx <<= MONO_HAS_DECL_SECURITY_BITS;
	idx |= MONO_HAS_DECL_SECURITY_ASSEMBLY;
	return mono_declsec_get_flags (assembly->image, idx);
}


/*
 * Fill actions for the specific index (which may either be an encoded class token or
 * an encoded method token) from the metadata image.
 * Returns TRUE if some actions requiring code generation are present, FALSE otherwise.
 */
static MonoBoolean
fill_actions_from_index (MonoImage *image, guint32 token, MonoDeclSecurityActions* actions,
	guint32 id_std, guint32 id_noncas, guint32 id_choice)
{
	MonoBoolean result = FALSE;
	MonoTableInfo *t;
	guint32 cols [MONO_DECL_SECURITY_SIZE];
	int index = mono_metadata_declsec_from_index (image, token);
	int i;

	t  = &image->tables [MONO_TABLE_DECLSECURITY];
	for (i = index; i < t->rows; i++) {
		mono_metadata_decode_row (t, i, cols, MONO_DECL_SECURITY_SIZE);

		if (cols [MONO_DECL_SECURITY_PARENT] != token)
			return result;

		/* if present only replace (class) permissions with method permissions */
		/* if empty accept either class or method permissions */
		if (cols [MONO_DECL_SECURITY_ACTION] == id_std) {
			if (!actions->demand.blob) {
				const char *blob = mono_metadata_blob_heap (image, cols [MONO_DECL_SECURITY_PERMISSIONSET]);
				actions->demand.index = cols [MONO_DECL_SECURITY_PERMISSIONSET];
				actions->demand.blob = (char*) (blob + 2);
				actions->demand.size = mono_metadata_decode_blob_size (blob, &blob);
				result = TRUE;
			}
		} else if (cols [MONO_DECL_SECURITY_ACTION] == id_noncas) {
			if (!actions->noncasdemand.blob) {
				const char *blob = mono_metadata_blob_heap (image, cols [MONO_DECL_SECURITY_PERMISSIONSET]);
				actions->noncasdemand.index = cols [MONO_DECL_SECURITY_PERMISSIONSET];
				actions->noncasdemand.blob = (char*) (blob + 2);
				actions->noncasdemand.size = mono_metadata_decode_blob_size (blob, &blob);
				result = TRUE;
			}
		} else if (cols [MONO_DECL_SECURITY_ACTION] == id_choice) {
			if (!actions->demandchoice.blob) {
				const char *blob = mono_metadata_blob_heap (image, cols [MONO_DECL_SECURITY_PERMISSIONSET]);
				actions->demandchoice.index = cols [MONO_DECL_SECURITY_PERMISSIONSET];
				actions->demandchoice.blob = (char*) (blob + 2);
				actions->demandchoice.size = mono_metadata_decode_blob_size (blob, &blob);
				result = TRUE;
			}
		}
	}

	return result;
}

static MonoBoolean
mono_declsec_get_class_demands_params (MonoClass *klass, MonoDeclSecurityActions* demands, 
	guint32 id_std, guint32 id_noncas, guint32 id_choice)
{
	guint32 idx = mono_metadata_token_index (klass->type_token);
	idx <<= MONO_HAS_DECL_SECURITY_BITS;
	idx |= MONO_HAS_DECL_SECURITY_TYPEDEF;
	return fill_actions_from_index (klass->image, idx, demands, id_std, id_noncas, id_choice);
}

static MonoBoolean
mono_declsec_get_method_demands_params (MonoMethod *method, MonoDeclSecurityActions* demands, 
	guint32 id_std, guint32 id_noncas, guint32 id_choice)
{
	guint32 idx = mono_method_get_index (method);
	idx <<= MONO_HAS_DECL_SECURITY_BITS;
	idx |= MONO_HAS_DECL_SECURITY_METHODDEF;
	return fill_actions_from_index (method->klass->image, idx, demands, id_std, id_noncas, id_choice);
}

/*
 * Collect all actions (that requires to generate code in mini) assigned for
 * the specified method.
 * Note: Don't use the content of actions if the function return FALSE.
 */
MonoBoolean
mono_declsec_get_demands (MonoMethod *method, MonoDeclSecurityActions* demands)
{
	guint32 mask = MONO_DECLSEC_FLAG_DEMAND | MONO_DECLSEC_FLAG_NONCAS_DEMAND | 
		MONO_DECLSEC_FLAG_DEMAND_CHOICE;
	MonoBoolean result = FALSE;
	guint32 flags;

	/* quick exit if no declarative security is present in the metadata */
	if (!method->klass->image->tables [MONO_TABLE_DECLSECURITY].rows)
		return FALSE;

	/* we want the original as the wrapper is "free" of the security informations */
	if (method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE || method->wrapper_type == MONO_WRAPPER_MANAGED_TO_MANAGED) {
		method = mono_marshal_method_from_wrapper (method);
		if (!method)
			return FALSE;
	}

	/* First we look for method-level attributes */
	if (method->flags & METHOD_ATTRIBUTE_HAS_SECURITY) {
		mono_class_init (method->klass);
		memset (demands, 0, sizeof (MonoDeclSecurityActions));

		result = mono_declsec_get_method_demands_params (method, demands, 
			SECURITY_ACTION_DEMAND, SECURITY_ACTION_NONCASDEMAND, SECURITY_ACTION_DEMANDCHOICE);
	}

	/* Here we use (or create) the class declarative cache to look for demands */
	flags = mono_declsec_flags_from_class (method->klass);
	if (flags & mask) {
		if (!result) {
			mono_class_init (method->klass);
			memset (demands, 0, sizeof (MonoDeclSecurityActions));
		}
		result |= mono_declsec_get_class_demands_params (method->klass, demands, 
			SECURITY_ACTION_DEMAND, SECURITY_ACTION_NONCASDEMAND, SECURITY_ACTION_DEMANDCHOICE);
	}

	/* The boolean return value is used as a shortcut in case nothing needs to
	   be generated (e.g. LinkDemand[Choice] and InheritanceDemand[Choice]) */
	return result;
}


/*
 * Collect all Link actions: LinkDemand, NonCasLinkDemand and LinkDemandChoice (2.0).
 *
 * Note: Don't use the content of actions if the function return FALSE.
 */
MonoBoolean
mono_declsec_get_linkdemands (MonoMethod *method, MonoDeclSecurityActions* klass, MonoDeclSecurityActions *cmethod)
{
	MonoBoolean result = FALSE;
	guint32 flags;

	/* quick exit if no declarative security is present in the metadata */
	if (!method->klass->image->tables [MONO_TABLE_DECLSECURITY].rows)
		return FALSE;

	/* we want the original as the wrapper is "free" of the security informations */
	if (method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE || method->wrapper_type == MONO_WRAPPER_MANAGED_TO_MANAGED) {
		method = mono_marshal_method_from_wrapper (method);
		if (!method)
			return FALSE;
	}

	/* results are independant - zeroize both */
	memset (cmethod, 0, sizeof (MonoDeclSecurityActions));
	memset (klass, 0, sizeof (MonoDeclSecurityActions));

	/* First we look for method-level attributes */
	if (method->flags & METHOD_ATTRIBUTE_HAS_SECURITY) {
		mono_class_init (method->klass);

		result = mono_declsec_get_method_demands_params (method, cmethod, 
			SECURITY_ACTION_LINKDEMAND, SECURITY_ACTION_NONCASLINKDEMAND, SECURITY_ACTION_LINKDEMANDCHOICE);
	}

	/* Here we use (or create) the class declarative cache to look for demands */
	flags = mono_declsec_flags_from_class (method->klass);
	if (flags & (MONO_DECLSEC_FLAG_LINKDEMAND | MONO_DECLSEC_FLAG_NONCAS_LINKDEMAND | MONO_DECLSEC_FLAG_LINKDEMAND_CHOICE)) {
		mono_class_init (method->klass);

		result |= mono_declsec_get_class_demands_params (method->klass, klass, 
			SECURITY_ACTION_LINKDEMAND, SECURITY_ACTION_NONCASLINKDEMAND, SECURITY_ACTION_LINKDEMANDCHOICE);
	}

	return result;
}

/*
 * Collect all Inherit actions: InheritanceDemand, NonCasInheritanceDemand and InheritanceDemandChoice (2.0).
 *
 * @klass	The inherited class - this is the class that provides the security check (attributes)
 * @demans	
 * return TRUE if inheritance demands (any kind) are present, FALSE otherwise.
 * 
 * Note: Don't use the content of actions if the function return FALSE.
 */
MonoBoolean
mono_declsec_get_inheritdemands_class (MonoClass *klass, MonoDeclSecurityActions* demands)
{
	MonoBoolean result = FALSE;
	guint32 flags;

	/* quick exit if no declarative security is present in the metadata */
	if (!klass->image->tables [MONO_TABLE_DECLSECURITY].rows)
		return FALSE;

	/* Here we use (or create) the class declarative cache to look for demands */
	flags = mono_declsec_flags_from_class (klass);
	if (flags & (MONO_DECLSEC_FLAG_INHERITANCEDEMAND | MONO_DECLSEC_FLAG_NONCAS_INHERITANCEDEMAND | MONO_DECLSEC_FLAG_INHERITANCEDEMAND_CHOICE)) {
		mono_class_init (klass);
		memset (demands, 0, sizeof (MonoDeclSecurityActions));

		result |= mono_declsec_get_class_demands_params (klass, demands, 
			SECURITY_ACTION_INHERITDEMAND, SECURITY_ACTION_NONCASINHERITANCE, SECURITY_ACTION_INHERITDEMANDCHOICE);
	}

	return result;
}

/*
 * Collect all Inherit actions: InheritanceDemand, NonCasInheritanceDemand and InheritanceDemandChoice (2.0).
 *
 * Note: Don't use the content of actions if the function return FALSE.
 */
MonoBoolean
mono_declsec_get_inheritdemands_method (MonoMethod *method, MonoDeclSecurityActions* demands)
{
	/* quick exit if no declarative security is present in the metadata */
	if (!method->klass->image->tables [MONO_TABLE_DECLSECURITY].rows)
		return FALSE;

	/* we want the original as the wrapper is "free" of the security informations */
	if (method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE || method->wrapper_type == MONO_WRAPPER_MANAGED_TO_MANAGED) {
		method = mono_marshal_method_from_wrapper (method);
		if (!method)
			return FALSE;
	}

	if (method->flags & METHOD_ATTRIBUTE_HAS_SECURITY) {
		mono_class_init (method->klass);
		memset (demands, 0, sizeof (MonoDeclSecurityActions));

		return mono_declsec_get_method_demands_params (method, demands, 
			SECURITY_ACTION_INHERITDEMAND, SECURITY_ACTION_NONCASINHERITANCE, SECURITY_ACTION_INHERITDEMANDCHOICE);
	}
	return FALSE;
}


static MonoBoolean
get_declsec_action (MonoImage *image, guint32 token, guint32 action, MonoDeclSecurityEntry *entry)
{
	guint32 cols [MONO_DECL_SECURITY_SIZE];
	MonoTableInfo *t;
	int i;

	int index = mono_metadata_declsec_from_index (image, token);
	if (index == -1)
		return FALSE;

	t =  &image->tables [MONO_TABLE_DECLSECURITY];
	for (i = index; i < t->rows; i++) {
		mono_metadata_decode_row (t, i, cols, MONO_DECL_SECURITY_SIZE);

		/* shortcut - index are ordered */
		if (token != cols [MONO_DECL_SECURITY_PARENT])
			return FALSE;

		if (cols [MONO_DECL_SECURITY_ACTION] == action) {
			const char *metadata = mono_metadata_blob_heap (image, cols [MONO_DECL_SECURITY_PERMISSIONSET]);
			entry->blob = (char*) (metadata + 2);
			entry->size = mono_metadata_decode_blob_size (metadata, &metadata);
			return TRUE;
		}
	}

	return FALSE;
}

MonoBoolean
mono_declsec_get_method_action (MonoMethod *method, guint32 action, MonoDeclSecurityEntry *entry)
{
	if (method->flags & METHOD_ATTRIBUTE_HAS_SECURITY) {
		guint32 idx = mono_method_get_index (method);
		idx <<= MONO_HAS_DECL_SECURITY_BITS;
		idx |= MONO_HAS_DECL_SECURITY_METHODDEF;
		return get_declsec_action (method->klass->image, idx, action, entry);
	}
	return FALSE;
}

MonoBoolean
mono_declsec_get_class_action (MonoClass *klass, guint32 action, MonoDeclSecurityEntry *entry)
{
	/* use cache */
	guint32 flags = mono_declsec_flags_from_class (klass);
	if (declsec_flags_map [action] & flags) {
		guint32 idx = mono_metadata_token_index (klass->type_token);
		idx <<= MONO_HAS_DECL_SECURITY_BITS;
		idx |= MONO_HAS_DECL_SECURITY_TYPEDEF;
		return get_declsec_action (klass->image, idx, action, entry);
	}
	return FALSE;
}

MonoBoolean
mono_declsec_get_assembly_action (MonoAssembly *assembly, guint32 action, MonoDeclSecurityEntry *entry)
{
	guint32 idx = 1; /* there is only one assembly */
	idx <<= MONO_HAS_DECL_SECURITY_BITS;
	idx |= MONO_HAS_DECL_SECURITY_ASSEMBLY;

	return get_declsec_action (assembly->image, idx, action, entry);
}

gboolean
mono_reflection_call_is_assignable_to (MonoClass *klass, MonoClass *oklass)
{
	MonoObject *res, *exc;
	void *params [1];
	static MonoClass *System_Reflection_Emit_TypeBuilder = NULL;
	static MonoMethod *method = NULL;

	if (!System_Reflection_Emit_TypeBuilder) {
		System_Reflection_Emit_TypeBuilder = mono_class_from_name (mono_defaults.corlib, "System.Reflection.Emit", "TypeBuilder");
		g_assert (System_Reflection_Emit_TypeBuilder);
	}
	if (method == NULL) {
		method = mono_class_get_method_from_name (System_Reflection_Emit_TypeBuilder, "IsAssignableTo", 1);
		g_assert (method);
	}

	/* 
	 * The result of mono_type_get_object () might be a System.MonoType but we
	 * need a TypeBuilder so use mono_class_get_ref_info (klass).
	 */
	g_assert (mono_class_get_ref_info (klass));
	g_assert (!strcmp (((MonoObject*)(mono_class_get_ref_info (klass)))->vtable->klass->name, "TypeBuilder"));

	params [0] = mono_type_get_object (mono_domain_get (), &oklass->byval_arg);

	res = mono_runtime_invoke (method, (MonoObject*)(mono_class_get_ref_info (klass)), params, &exc);
	if (exc)
		return FALSE;
	else
		return *(MonoBoolean*)mono_object_unbox (res);
}

/**
 * mono_reflection_type_get_type:
 * @reftype: the System.Type object
 *
 * Returns the MonoType* associated with the C# System.Type object @reftype.
 */
MonoType*
mono_reflection_type_get_type (MonoReflectionType *reftype)
{
	g_assert (reftype);

	return mono_reflection_type_get_handle (reftype);
}

