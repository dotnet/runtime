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

static GHashTable *class_hash;

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
	MonoType *t = &ft->type;

	switch (t->type){
	case ELEMENT_TYPE_BOOLEAN:
		return sizeof (m_boolean);
		
	case ELEMENT_TYPE_CHAR:
		return sizeof (m_char);
		
	case ELEMENT_TYPE_I1:
	case ELEMENT_TYPE_U1:
		return 1;
		
	case ELEMENT_TYPE_I2:
	case ELEMENT_TYPE_U2:
		return 2;
		
	case ELEMENT_TYPE_I4:
	case ELEMENT_TYPE_U4:
	case ELEMENT_TYPE_R4:
		return 4;
		
	case ELEMENT_TYPE_I8:
	case ELEMENT_TYPE_U8:
	case ELEMENT_TYPE_R8:
		return 8;
		
	case ELEMENT_TYPE_I:
		return sizeof (m_i);
		
	case ELEMENT_TYPE_U:
		return sizeof (m_u);
		
	case ELEMENT_TYPE_STRING:
		return sizeof (m_string);
		
	case ELEMENT_TYPE_OBJECT:
		return sizeof (m_object);
		
	case ELEMENT_TYPE_VALUETYPE:
		g_error ("FIXME: Add computation of size for ELEMENT_TYPE_VALUETYPE");
		
	case ELEMENT_TYPE_CLASS:
		g_error ("FIXME: Add computation of size for ELEMENT_TYPE_CLASS");
		break;
		
	case ELEMENT_TYPE_SZARRAY:
		g_error ("FIXME: Add computation of size for ELEMENT_TYPE_SZARRAY");
		break;
		
	case ELEMENT_TYPE_PTR:
		g_error ("FIXME: Add computation of size for ELEMENT_TYPE_PTR");
		break;
		
	case ELEMENT_TYPE_FNPTR:
		g_error ("FIXME: Add computation of size for ELEMENT_TYPE_FNPTR");
		break;
		
	case ELEMENT_TYPE_ARRAY:
		g_error ("FIXME: Add computation of size for ELEMENT_TYPE_ARRAY");
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
class_compute_field_layout (metadata_t *m, MonoClass *class)
{
	const int top = class->field.count;
	guint32 layout = class->flags & TYPE_ATTRIBUTE_LAYOUT_MASK;
	metadata_tableinfo_t *t = &m->tables [META_TABLE_FIELD];
	int instance_size = sizeof (MonoObject);
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
		
		class->fields [i].type = mono_metadata_parse_field_type (
			m, sig, &sig);
		class->fields [i].flags = cols [0];
	}

	/*
	 * Compute field layout and total size.
	 */
	switch (layout){
	case TYPE_ATTRIBUTE_AUTO_LAYOUT:
		for (i = 0; i < top; i++){
			int size;
			
			class->fields [i].offset = instance_size;
			
			size = mono_field_type_size (class->fields [i].type);
			size += (size % 4);
			instance_size += size;
		}
		break;
		
	case TYPE_ATTRIBUTE_SEQUENTIAL_LAYOUT:
		for (i = 0; i < top; i++){
			int size;
			
			class->fields [i].offset = instance_size;
			
			size = mono_field_type_size (class->fields [i].type);
			size += (size % 4);
			instance_size += size;
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
	cli_image_info_t *iinfo = image->image_info;
	metadata_t *m = &iinfo->cli_metadata;
	metadata_tableinfo_t *tt = &m->tables [META_TABLE_TYPEDEF];
	MonoClass *class;
	guint32 cols [6];
	guint tidx = type_token & 0xffffff;
	
	class = g_new0 (MonoClass, 1);
	class->image = image;
	class->type_token = tidx;

	mono_metadata_decode_row (tt, tidx, cols, CSIZE (cols));

	class->flags = cols [0];
	
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
		class->field.last  = m->tables [META_TABLE_FIELD].rows;
		class->method.last = m->tables [META_TABLE_METHOD].rows;
	}

	if (cols [4] && cols [4] <= m->tables [META_TABLE_FIELD].rows)
		class->field.count = class->field.last - class->field.first;
	else
		class->field.count = 0;

	if (cols [5] <= m->tables [META_TABLE_METHOD].rows)
		class->method.count = class->method.last - class->method.first;
	else
		class->method.count = 0;

	/*
	 * Computes the size used by the fields, and their locations
	 */
	if (class->field.count > 0){
		class->fields = g_new (MonoClassFields, class->field.count);
		class_compute_field_layout (m, class);
	}

	return class;
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
	MonoClass *class, hash_lookup;

	hash_lookup.image = image;
	hash_lookup.type_token = type_token;
	
	class = g_hash_table_lookup (class_hash, &hash_lookup);

	if (class)
		return class;

	switch (type_token & 0xff000000){
	case TOKEN_TYPE_TYPE_DEF:
		class = mono_class_create_from_typedef (image, type_token);
		break;
		
	case TOKEN_TYPE_TYPE_REF:
		g_error ("Can not handle class creation of TypeRefs yet");
		
	default:
		g_assert_not_reached ();
	}
	
	g_hash_table_insert (class_hash, class, class);

	return class;
}

static guint
mono_class_hash (gconstpointer p)
{
	MonoClass *c = (MonoClass *) p;

	return (((guint32)(c->image)) ^ c->type_token);
}

static gint
mono_class_equal (gconstpointer ap, gconstpointer bp)
{
	MonoClass *a = (MonoClass *) ap;
	MonoClass *b = (MonoClass *) bp;

	if ((a->image == b->image) && (a->type_token == b->type_token))
		return TRUE;

	return FALSE;
}

/**
 * mono_class_init:
 *
 * Initializes the runtime class system
 */
void
mono_class_init (void)
{
	class_hash = g_hash_table_new (
		mono_class_hash, mono_class_equal);
}
