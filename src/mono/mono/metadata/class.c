/*
 * class.c: Class management for the Mono runtime
 *
 * Author:
 *   Miguel de Icaza (miguel@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 *
 * Possible Optimizations:
 *     in mono_class_create, do not allocate the class right away,
 *     but wait until you know the size of the FieldMap, so that
 *     the class embeds directly the FieldMap after the vtable.
 *
 * 
 */
#include <config.h>
#include <glib.h>
#include <stdio.h>
#include <mono/metadata/image.h>
#include <mono/metadata/cil-coff.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/tokentype.h>
#include <mono/cli/class.h>
#include <mono/cli/types.h>
#include <mono/cli/object.h>

#define CSIZE(x) (sizeof (x) / 4)

/*
 * mono_field_type_size:
 * @t: the type to return the size of
 *
 * Returns: the number of bytes required to hold an instance of this
 * type in memory
 */
int
mono_field_type_size (MonoFieldType *ft)
{
	MonoType *t = ft->type;

	switch (t->type){
	case MONO_TYPE_BOOLEAN:
		return sizeof (m_boolean);
		
	case MONO_TYPE_CHAR:
		return sizeof (m_char);
		
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
		return 1;
		
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
		return 2;
		
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_R4:
		return 4;
		
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_R8:
		return 8;
		
	case MONO_TYPE_I:
		return sizeof (m_i);
		
	case MONO_TYPE_U:
		return sizeof (m_u);
		
	case MONO_TYPE_STRING:
		return sizeof (m_string);
		
	case MONO_TYPE_OBJECT:
		return sizeof (m_object);
		
	case MONO_TYPE_VALUETYPE:
		g_error ("FIXME: Add computation of size for MONO_TYPE_VALUETYPE");
		
	case MONO_TYPE_CLASS:
		g_error ("FIXME: Add computation of size for MONO_TYPE_CLASS");
		break;
		
	case MONO_TYPE_SZARRAY:
		g_error ("FIXME: Add computation of size for MONO_TYPE_SZARRAY");
		break;
		
	case MONO_TYPE_PTR:
		g_error ("FIXME: Add computation of size for MONO_TYPE_PTR");
		break;
		
	case MONO_TYPE_FNPTR:
		g_error ("FIXME: Add computation of size for MONO_TYPE_FNPTR");
		break;
		
	case MONO_TYPE_ARRAY:
		g_error ("FIXME: Add computation of size for MONO_TYPE_ARRAY");
		break;
	default:
		g_error ("type 0x%02x unknown", t->type);
	}
	return 0;
}

/** 
 * class_compute_field_layout:
 * @m: pointer to the metadata.
 * @class: The class to initialize
 *
 * Initializes the class->fields.
 *
 * Currently we only support AUTO_LAYOUT, and do not even try to do
 * a good job at it.  This is temporary to get the code for Paolo.
 */
static void
class_compute_field_layout (MonoMetadata *m, MonoClass *class)
{
	const int top = class->field.count;
	guint32 layout = class->flags & TYPE_ATTRIBUTE_LAYOUT_MASK;
	MonoTableInfo *t = &m->tables [MONO_TABLE_FIELD];
	int i;
	
	/*
	 * Fetch all the field information.
	 */
	for (i = 0; i < top; i++){
		const char *sig;
		guint32 cols [3];
		int idx = class->field.first + i;
		
		mono_metadata_decode_row (t, idx, cols, CSIZE (cols));
		sig = mono_metadata_blob_heap (m, cols [2]);
		mono_metadata_decode_value (sig, &sig);

		/* FIELD signature == 0x06 */
		g_assert (*sig == 0x06);
		
		class->fields [i].type = mono_metadata_parse_field_type (
			m, sig, &sig);
		class->fields [i].flags = cols [0];
	}

	/*
	 * Compute field layout and total size.
	 */
	switch (layout){
	case TYPE_ATTRIBUTE_AUTO_LAYOUT:
	case TYPE_ATTRIBUTE_SEQUENTIAL_LAYOUT:
		for (i = 0; i < top; i++){
			int size;
			
			size = mono_field_type_size (class->fields [i].type);
			size += (size % 4);
			if (class->fields [i].flags & FIELD_ATTRIBUTE_STATIC) {
				class->fields [i].offset = class->class_size;
				class->class_size += size;
			} else {
				class->fields [i].offset = class->instance_size;
				class->instance_size += size;
			}
		}
		break;
	case TYPE_ATTRIBUTE_EXPLICIT_LAYOUT:
		g_error ("TODO: Explicit layout not supported yet");
	}
}

/**
 * @image: context where the image is created
 * @tidx:  index of the type to create
 */
static MonoClass *
mono_class_create_from_typedef (MonoImage *image, guint32 type_token)
{
	MonoMetadata *m = &image->metadata;
	MonoTableInfo *tt = &m->tables [MONO_TABLE_TYPEDEF];
	MonoClass stack_class;
	MonoClass *class = &stack_class;
	guint32 cols [6], parent_token;
	guint tidx = type_token & 0xffffff;
	const char *name;

	memset (class, 0, sizeof (MonoClass));

	mono_metadata_decode_row (tt, tidx-1, cols, CSIZE (cols));
	name = mono_metadata_string_heap (m, cols[1]);
	/*g_print ("Init class %s\n", name);*/

	/*
	 * If root of the hierarchy
	 */
	if (cols [3] == 0){
		class->instance_size = sizeof (MonoObject);
		class->parent = NULL;
	} else {
		parent_token = mono_metadata_token_from_dor (cols [3]);
		class->parent = mono_class_get (image, parent_token);
		class->instance_size = class->parent->instance_size;
	}
	
	class->image = image;
	class->type_token = tidx;
	class->flags = cols [0];
	class->class_size = sizeof (MonoClass);
	
	/*
	 * Compute the field and method lists
	 */
	class->field.first  = cols [4] - 1;
	class->method.first = cols [5] - 1;

	if (tt->rows > tidx + 1){
		guint32 cols_next [6];
		
		mono_metadata_decode_row (tt, tidx + 1, cols_next, CSIZE (cols_next));
		class->field.last  = cols_next [4] - 1;
		class->method.last = cols_next [5] - 1;
	} else {
		class->field.last  = m->tables [MONO_TABLE_FIELD].rows;
		class->method.last = m->tables [MONO_TABLE_METHOD].rows;
	}

	if (cols [4] && cols [4] <= m->tables [MONO_TABLE_FIELD].rows)
		class->field.count = class->field.last - class->field.first;
	else
		class->field.count = 0;

	if (cols [5] <= m->tables [MONO_TABLE_METHOD].rows)
		class->method.count = class->method.last - class->method.first;
	else
		class->method.count = 0;

	/*
	 * Computes the size used by the fields, and their locations
	 */
	if (class->field.count > 0){
		class->fields = g_new (MonoClassField, class->field.count);
		class_compute_field_layout (m, class);
	}

	class = g_malloc0 (class->class_size);
	*class = stack_class;
	return class;
}

/*
 * Auxiliary routine to mono_class_get_field
 *
 * Takes a field index instead of a field token.
 */
static MonoClassField *
mono_class_get_field_idx (MonoClass *class, int idx)
{
	if (class->field.count){
		if ((idx >= class->field.first) && (idx < class->field.last)){
			return &class->fields [idx - class->field.first];
		}
	}

	if (!class->parent)
		return NULL;
	
	return mono_class_get_field_idx (class->parent, idx);
}

/**
 * mono_class_get_field:
 * @class: the class to lookup the field.
 * @field_token: the field token
 *
 * Returns: A MonoClassField representing the type and offset of
 * the field, or a NULL value if the field does not belong to this
 * class.
 */
MonoClassField *
mono_class_get_field (MonoClass *class, guint32 field_token)
{
	int idx = mono_metadata_token_index (field_token);

	if (mono_metadata_token_code (field_token) == MONO_TOKEN_MEMBER_REF)
		g_error ("Unsupported Field Token is a MemberRef, implement me");

	g_assert (mono_metadata_token_code (field_token) == MONO_TOKEN_FIELD_DEF);

	return mono_class_get_field_idx (class, idx - 1);
}

static void
typedef_from_typeref (MonoImage *image, guint32 type_token, MonoImage **rimage, guint32 *index)
{
	guint32 cols[6];
	MonoMetadata *m = &image->metadata;
	MonoTableInfo  *t = &m->tables[MONO_TABLE_TYPEREF];
	guint32 idx, i;
	const char *name, *nspace;
	
	mono_metadata_decode_row (t, (type_token&0xffffff)-1, cols, 3);
	g_assert ((cols [0] & 0x3) == 2);
	idx = cols [0] >> 2;
	name = mono_metadata_string_heap (m, cols [1]);
	nspace = mono_metadata_string_heap (m, cols [2]);
	/* load referenced assembly */
	image = image->references [idx-1]->image;
	m = &image->metadata;
	t = &m->tables [MONO_TABLE_TYPEDEF];
	/* dumb search for now */
	for (i=0; i < t->rows; ++i) {
		mono_metadata_decode_row (t, i, cols, 6);
		if (strcmp (name, mono_metadata_string_heap (m, cols [1])) == 0 
				&& strcmp (nspace, mono_metadata_string_heap (m, cols [2])) == 0) {
			*rimage = image;
			*index = i + 1;
			return;
		}
	}
	g_assert_not_reached ();
	
}

/**
 * mono_class_get:
 * @image: the image where the class resides
 * @type_token: the token for the class
 *
 * Returns: the MonoClass that represents @type_token in @image
 */
MonoClass *
mono_class_get (MonoImage *image, guint32 type_token)
{
	MonoClass *class;

	if ((type_token & 0xff000000) == MONO_TOKEN_TYPE_DEF 
					&& (class = g_hash_table_lookup (image->class_cache, GUINT_TO_POINTER (type_token))))
			return class;

	switch (type_token & 0xff000000){
	case MONO_TOKEN_TYPE_DEF:
		class = mono_class_create_from_typedef (image, type_token);
		break;
		
	case MONO_TOKEN_TYPE_REF: {
		typedef_from_typeref (image, type_token, &image, &type_token);
		class = mono_class_create_from_typedef (image, type_token);
		break;
	}
	case MONO_TOKEN_TYPE_SPEC:
		g_error ("Can not handle class creation of TypeSpecs yet");
		
	default:
		g_assert_not_reached ();
	}
	
	g_hash_table_insert (image->class_cache, GUINT_TO_POINTER (type_token), class);

	return class;
}

