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
#include <mono/metadata/class.h>
#include <mono/metadata/object.h>

#define CSIZE(x) (sizeof (x) / 4)

static void
typedef_from_typeref (MonoImage *image, guint32 type_token, MonoImage **rimage, guint32 *index)
{
	guint32 cols[MONO_TYPEDEF_SIZE];
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
		mono_metadata_decode_row (t, i, cols, MONO_TYPEDEF_SIZE);

		if (!strcmp (name, mono_metadata_string_heap (m, cols [1])) &&
		    !strcmp (nspace, mono_metadata_string_heap (m, cols [2]))) {
			*rimage = image;
			*index =  MONO_TOKEN_TYPE_DEF | (i + 1);
			return;
		}
	}
	g_assert_not_reached ();
	
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
	int i, j;

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
			m, sig + 1, &sig);
		class->fields [i].flags = cols [0];
	}
	/*
	 * Compute field layout and total size.
	 */
	switch (layout){
	case TYPE_ATTRIBUTE_AUTO_LAYOUT:
	case TYPE_ATTRIBUTE_SEQUENTIAL_LAYOUT:
		for (i = 0; i < top; i++){
			int size, align;
			
			size = mono_type_size (class->fields [i].type->type, &align);
			if (class->fields [i].flags & FIELD_ATTRIBUTE_STATIC) {
				class->fields [i].offset = class->class_size;
				class->class_size += (class->class_size % align);
				class->class_size += size;
			} else {
				class->fields [i].offset = class->instance_size;
				class->instance_size += (class->instance_size % align);
				class->instance_size += size;
			}
		}
		break;
	case TYPE_ATTRIBUTE_EXPLICIT_LAYOUT:
		for (i = 0; i < top; i++){
			guint32 cols [2];
			int size, align;
			int idx = class->field.first + i;

			t = &m->tables [MONO_TABLE_FIELDLAYOUT];

			for (j = 0; j < t->rows; j++) {

				mono_metadata_decode_row (t, j, cols, CSIZE (cols));
				if (cols [1] == idx) {
					g_warning ("TODO: Explicit layout not supported yet");
				}
			}
			
			size = mono_type_size (class->fields [i].type->type, &align);
			if (class->fields [i].flags & FIELD_ATTRIBUTE_STATIC) {
				class->fields [i].offset = class->class_size;
				class->class_size += (class->class_size % align);
				class->class_size += size;
			} else {
				class->fields [i].offset = class->instance_size;
				class->instance_size += (class->instance_size % align);
				class->instance_size += size;
			}
		}
		break;
	}
}

/**
 * @image: context where the image is created
 * @type_token:  typedef token
 */
static MonoClass *
mono_class_create_from_typedef (MonoImage *image, guint32 type_token)
{
	MonoMetadata *m = &image->metadata;
	MonoTableInfo *tt = &m->tables [MONO_TABLE_TYPEDEF];
	MonoClass stack_class;
	MonoClass *class = &stack_class;
	guint32 cols [MONO_TYPEDEF_SIZE], parent_token;
	guint tidx = mono_metadata_token_index (type_token);
	const char *name, *nspace;
     
	g_assert (mono_metadata_token_table (type_token) == MONO_TABLE_TYPEDEF);

	memset (class, 0, sizeof (MonoClass));

	mono_metadata_decode_row (tt, tidx-1, cols, CSIZE (cols));
	name = mono_metadata_string_heap (m, cols[1]);
	nspace = mono_metadata_string_heap (m, cols[2]);
	/*g_print ("Init class %s\n", name);*/
 
	/* if root of the hierarchy */
	if (!strcmp (nspace, "System") && !strcmp (name, "Object")) {
		class->instance_size = sizeof (MonoObject);
		class->parent = NULL;
	} else {
		parent_token = mono_metadata_token_from_dor (cols [3]);
		class->parent = mono_class_get (image, parent_token);
		class->instance_size = class->parent->instance_size;
		class->valuetype = class->parent->valuetype;
	}
	if (!strcmp (nspace, "System") && !strcmp (name, "ValueType"))
		class->valuetype = 1;

	g_assert (class->instance_size);
	class->image = image;
	class->type_token = type_token;
	class->flags = cols [0];
	class->class_size = sizeof (MonoClass);
	
	/*
	 * Compute the field and method lists
	 */
	class->field.first  = cols [MONO_TYPEDEF_FIELD_LIST] - 1;
	class->method.first = cols [MONO_TYPEDEF_METHOD_LIST] - 1;

	if (tt->rows > tidx){
		guint32 cols_next [MONO_TYPEDEF_SIZE];
		
		mono_metadata_decode_row (tt, tidx, cols_next, CSIZE (cols_next));
		class->field.last  = cols_next [MONO_TYPEDEF_FIELD_LIST] - 1;
		class->method.last = cols_next [MONO_TYPEDEF_METHOD_LIST] - 1;
	} else {
		class->field.last  = m->tables [MONO_TABLE_FIELD].rows;
		class->method.last = m->tables [MONO_TABLE_METHOD].rows;
	}

	if (cols [MONO_TYPEDEF_FIELD_LIST] && 
	    cols [MONO_TYPEDEF_FIELD_LIST] <= m->tables [MONO_TABLE_FIELD].rows)
		class->field.count = class->field.last - class->field.first;
	else
		class->field.count = 0;

	if (cols [MONO_TYPEDEF_METHOD_LIST] <= m->tables [MONO_TABLE_METHOD].rows)
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

	/* reserve space to store vector pointer in arrays */
	if (!strcmp (nspace, "System") && !strcmp (name, "Array")) {
		class->instance_size += 2 * sizeof (gpointer);
		g_assert (class->field.count == 0);
		g_assert (class->instance_size == sizeof (MonoArrayObject));
	}

	if (class->method.count > 0) {
		int i;
		class->methods = g_new (MonoMethod*, class->method.count);
		for (i = class->method.first; i < class->method.last; ++i)
			class->methods [i - class->method.first] = mono_get_method (image,
							MONO_TOKEN_METHOD_DEF | (i + 1));
	}
	
	class = g_malloc0 (class->class_size);
	*class = stack_class;
	return class;
}

static guint32
mono_type_to_tydedef (MonoImage *image, MonoType *type, MonoImage **rimage)
{
	MonoImage *corlib, *res;
	guint32 etype;

	res = corlib = mono_defaults.corlib;

	switch (type->type) {
	case MONO_TYPE_BOOLEAN:
		etype = mono_typedef_from_name (corlib, "Boolean", "System", NULL);
		break;
	case MONO_TYPE_CHAR:
		etype = mono_typedef_from_name (corlib, "Char", "System", NULL); 
		break;
	case MONO_TYPE_I1:
		etype = mono_typedef_from_name (corlib, "Byte", "System", NULL); 
		break;
	case MONO_TYPE_I2:
		etype = mono_typedef_from_name (corlib, "Int16", "System", NULL); 
		break;
	case MONO_TYPE_U2:
		etype = mono_typedef_from_name (corlib, "UInt16", "System", NULL); 
		break;
	case MONO_TYPE_I4:
		etype = mono_typedef_from_name (corlib, "Int32", "System", NULL); 
		break;
	case MONO_TYPE_U4:
		etype = mono_typedef_from_name (corlib, "UInt32", "System", NULL); 
		break;
	case MONO_TYPE_I8:
		etype = mono_typedef_from_name (corlib, "Int64", "System", NULL); 
		break;
	case MONO_TYPE_U8:
		etype = mono_typedef_from_name (corlib, "UInt64", "System", NULL); 
		break;
	case MONO_TYPE_R8:
		etype = mono_typedef_from_name (corlib, "Double", "System", NULL); 
		break;
	case MONO_TYPE_STRING:
		etype = mono_typedef_from_name (corlib, "String", "System", NULL); 
		break;
	case MONO_TYPE_CLASS:
		etype = type->data.token;
		res = image;
		break;
	default:
		g_warning ("implement me %08x\n", type->type);
		g_assert_not_reached ();
	}
	
	*rimage = res;
	return etype;
}

/**
 * @image: context where the image is created
 * @type_spec:  typespec token
 * @at: an optional pointer to return the array type
 */
static MonoClass *
mono_class_create_from_typespec (MonoImage *image, guint32 type_spec)
{
	MonoMetadata *m = &image->metadata;
	guint32 idx = mono_metadata_token_index (type_spec);
	MonoTableInfo *t;
	guint32 cols [MONO_TYPESPEC_SIZE];       
	const char *ptr;
	guint32 len, etype;
	MonoType *type;
	MonoClass *class;
	MonoImage *rimage;

	t = &m->tables [MONO_TABLE_TYPESPEC];
	
	mono_metadata_decode_row (t, idx-1, cols, MONO_TYPESPEC_SIZE);
	ptr = mono_metadata_blob_heap (m, cols [MONO_TYPESPEC_SIGNATURE]);
	len = mono_metadata_decode_value (ptr, &ptr);
	type = mono_metadata_parse_type (m, ptr, &ptr);

	switch (type->type) {
	case MONO_TYPE_ARRAY:
		etype = mono_type_to_tydedef (image, type->data.array->type, &rimage);
		class = mono_array_class_get (rimage, etype, type->data.array->rank);
		break;
	case MONO_TYPE_SZARRAY:
		g_assert (!type->custom_mod);
		etype = mono_type_to_tydedef (image, type->data.type, &rimage);
		class = mono_array_class_get (rimage, etype, 1);
		break;
	default:
		g_assert_not_reached ();		
	}

	mono_metadata_free_type (type);
	
	return class;
}

MonoClass *
mono_array_class_get (MonoImage *image, guint32 etype, guint32 rank)
{
	MonoClass *class, *eclass;
	static MonoClass *parent = NULL;
	MonoArrayClass *aclass;
	guint32 esize, key;

	g_assert (rank <= 255);

	if (!parent) {
		parent = mono_class_get (mono_defaults.corlib, 
					 mono_defaults.array_token);
		g_assert (parent != NULL);
	}

	eclass = mono_class_get (image, etype);
	g_assert (eclass != NULL);

	image = eclass->image;

	esize = eclass->instance_size;

	g_assert (!eclass->type_token ||
		  mono_metadata_token_table (eclass->type_token) == MONO_TABLE_TYPEDEF);
	
	key = ((rank & 0xff) << 24) | (eclass->type_token & 0xffffff);
	if ((class = g_hash_table_lookup (image->array_cache, GUINT_TO_POINTER (key))))
		return class;
	
	aclass = g_new0 (MonoArrayClass, 1);
	class = (MonoClass *)aclass;
       
	class->image = image;
	class->type_token = 0;
	class->flags = TYPE_ATTRIBUTE_CLASS;
	class->parent = parent;
	class->instance_size = class->parent->instance_size;
	class->class_size = sizeof (MonoArrayClass);

	aclass->rank = rank;
	aclass->element_class = eclass;
	
	g_hash_table_insert (image->array_cache, GUINT_TO_POINTER (key), class);
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

/**
 * mono_class_get:
 * @image: the image where the class resides
 * @type_token: the token for the class
 * @at: an optional pointer to return the array element type
 *
 * Returns: the MonoClass that represents @type_token in @image
 */
MonoClass *
mono_class_get (MonoImage *image, guint32 type_token)
{
	MonoClass *class;
       
	switch (type_token & 0xff000000){
	case MONO_TOKEN_TYPE_DEF:
		if ((class = g_hash_table_lookup (image->class_cache, 
						  GUINT_TO_POINTER (type_token))))
			return class;

		class = mono_class_create_from_typedef (image, type_token);
		break;
		
	case MONO_TOKEN_TYPE_REF: {
		typedef_from_typeref (image, type_token, &image, &type_token);
		return mono_class_get (image, type_token);
	}
	case MONO_TOKEN_TYPE_SPEC:
		if ((class = g_hash_table_lookup (image->class_cache, 
						  GUINT_TO_POINTER (type_token))))
			return class;

		class = mono_class_create_from_typespec (image, type_token);
		break;
	default:
		g_assert_not_reached ();
	}
	
	g_hash_table_insert (image->class_cache, GUINT_TO_POINTER (type_token), class);

	return class;
}

gint32
mono_array_element_size (MonoArrayClass *ac)
{
	gint32 esize;

	esize = ac->element_class->instance_size;
	
	if (ac->element_class->valuetype)
		esize -= sizeof (MonoObject);
	
	return esize;
}
