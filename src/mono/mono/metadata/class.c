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

static MonoClass *
mono_class_create_from_typeref (MonoImage *image, guint32 type_token)
{
	guint32 cols [MONO_TYPEREF_SIZE];
	MonoTableInfo  *t = &image->tables [MONO_TABLE_TYPEREF];
	guint32 idx;
	const char *name, *nspace;
	MonoClass *res;

	mono_metadata_decode_row (t, (type_token&0xffffff)-1, cols, MONO_TYPEREF_SIZE);
	g_assert ((cols [MONO_TYPEREF_SCOPE] & 0x3) == 2);
	idx = cols [MONO_TYPEREF_SCOPE] >> 2;

	if (!image->references ||  !image->references [idx-1]) {
		/* 
		 * detected a reference to mscorlib, we simply return a reference to a dummy 
		 * until we have a better solution.
		 */
		res = mono_class_from_name (image, "System", "MonoDummy");
		/* prevent method loading */
		res->dummy = 1;
		/* some storage if the type is used  - very ugly hack */
		res->instance_size = 2*sizeof (gpointer);
		return res;
	}	

	name = mono_metadata_string_heap (image, cols [MONO_TYPEREF_NAME]);
	nspace = mono_metadata_string_heap (image, cols [MONO_TYPEREF_NAMESPACE]);
	
	/* load referenced assembly */
	image = image->references [idx-1]->image;

	return mono_class_from_name (image, nspace, name);
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
class_compute_field_layout (MonoClass *class)
{
	MonoImage *m = class->image; 
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
			m, cols [MONO_FIELD_FLAGS], sig + 1, &sig);
	}
	/*
	 * Compute field layout and total size.
	 */
	switch (layout){
	case TYPE_ATTRIBUTE_AUTO_LAYOUT:
	case TYPE_ATTRIBUTE_SEQUENTIAL_LAYOUT:
		for (i = 0; i < top; i++){
			int size, align;
			
			size = mono_type_size (class->fields [i].type, &align);
			if (class->fields [i].type->attrs & FIELD_ATTRIBUTE_STATIC) {
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
			
			size = mono_type_size (class->fields [i].type, &align);
			if (class->fields [i].type->attrs & FIELD_ATTRIBUTE_STATIC) {
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

void
mono_class_metadata_init (MonoClass *class)
{
	int i;

	if (class->metadata_inited)
		return;

	if (class->parent) {
		if (!class->parent->metadata_inited)
			mono_class_metadata_init (class->parent);
		class->instance_size = class->parent->instance_size;
		class->class_size = class->parent->class_size;
	}

	class->metadata_inited = 1;

	/*
	 * Computes the size used by the fields, and their locations
	 */
	if (class->field.count > 0){
		class->fields = g_new (MonoClassField, class->field.count);
		class_compute_field_layout (class);
	}

	if (!class->method.count)
		return;

	class->methods = g_new (MonoMethod*, class->method.count);
	for (i = class->method.first; i < class->method.last; ++i)
		class->methods [i - class->method.first] = 
			mono_get_method (class->image,
					 MONO_TOKEN_METHOD_DEF | (i + 1), 
					 class);
}

/**
 * @image: context where the image is created
 * @type_token:  typedef token
 */
static MonoClass *
mono_class_create_from_typedef (MonoImage *image, guint32 type_token)
{
	MonoTableInfo *tt = &image->tables [MONO_TABLE_TYPEDEF];
	MonoClass *class;
	guint32 cols [MONO_TYPEDEF_SIZE], parent_token;
	guint tidx = mono_metadata_token_index (type_token);
	const char *name, *nspace;
     
	g_assert (mono_metadata_token_table (type_token) == MONO_TABLE_TYPEDEF);

	class = g_malloc0 (sizeof (MonoClass));

	class->this_arg.byref = 1;
	class->this_arg.data.klass = class;
	class->this_arg.type = MONO_TYPE_CLASS;

	mono_metadata_decode_row (tt, tidx-1, cols, CSIZE (cols));
	class->name = name = mono_metadata_string_heap (image, cols[1]);
	class->name_space = nspace = mono_metadata_string_heap (image, cols[2]);

	class->image = image;
	class->type_token = type_token;
	class->flags = cols [0];

	/*g_print ("Init class %s\n", name);*/

	/* if root of the hierarchy */
	if (!strcmp (nspace, "System") && !strcmp (name, "Object")) {
		class->parent = NULL;
		class->instance_size = sizeof (MonoObject);
	} else if (!(cols [0] & TYPE_ATTRIBUTE_INTERFACE)) {
		parent_token = mono_metadata_token_from_dor (cols [3]);
		class->parent = mono_class_get (image, parent_token);
		class->valuetype = class->parent->valuetype;
		class->enumtype = class->parent->enumtype;
	}

	if (!strcmp (nspace, "System")) {
		if (!strcmp (name, "ValueType")) {
			class->valuetype = 1;
		} else if (!strcmp (name, "Enum")) {
			class->valuetype = 1;
			class->enumtype = 1;
		}
	}
	if (class->valuetype)
		class->this_arg.type = MONO_TYPE_VALUETYPE;
	
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
		class->field.last  = image->tables [MONO_TABLE_FIELD].rows;
		class->method.last = image->tables [MONO_TABLE_METHOD].rows;
	}

	if (cols [MONO_TYPEDEF_FIELD_LIST] && 
	    cols [MONO_TYPEDEF_FIELD_LIST] <= image->tables [MONO_TABLE_FIELD].rows)
		class->field.count = class->field.last - class->field.first;
	else
		class->field.count = 0;

	if (cols [MONO_TYPEDEF_METHOD_LIST] <= image->tables [MONO_TABLE_METHOD].rows)
		class->method.count = class->method.last - class->method.first;
	else
		class->method.count = 0;

	/* reserve space to store vector pointer in arrays */
	if (!strcmp (nspace, "System") && !strcmp (name, "Array")) {
		class->instance_size += 2 * sizeof (gpointer);
		g_assert (class->field.count == 0);
	}

	class->interfaces = mono_metadata_interfaces_from_typedef (image, type_token);
	return class;
}

MonoClass *
mono_class_from_mono_type (MonoType *type)
{
	MonoImage *corlib;
	MonoClass *res;

	corlib = mono_defaults.corlib;

	switch (type->type) {
	case MONO_TYPE_OBJECT:
		res = mono_defaults.object_class;
		break;
	case MONO_TYPE_VOID:
		res = mono_defaults.void_class;
		break;
	case MONO_TYPE_BOOLEAN:
		res = mono_defaults.boolean_class;
		break;
	case MONO_TYPE_CHAR:
		res = mono_defaults.char_class;
		break;
	case MONO_TYPE_I1:
		res = mono_defaults.byte_class;
		break;
	case MONO_TYPE_U1:
		res = mono_defaults.sbyte_class;
		break;
	case MONO_TYPE_I2:
		res = mono_defaults.int16_class;
		break;
	case MONO_TYPE_U2:
		res = mono_defaults.uint16_class;
		break;
	case MONO_TYPE_I4:
		res = mono_defaults.int32_class;
		break;
	case MONO_TYPE_U4:
		res = mono_defaults.uint32_class;
		break;
	case MONO_TYPE_I:
		res = mono_defaults.int_class;
		break;
	case MONO_TYPE_U:
		res = mono_defaults.uint_class;
		break;
	case MONO_TYPE_I8:
		res = mono_defaults.int64_class;
		break;
	case MONO_TYPE_U8:
		res = mono_defaults.uint64_class;
		break;
	case MONO_TYPE_R4:
		res = mono_defaults.single_class;
		break;
	case MONO_TYPE_R8:
		res = mono_defaults.double_class;
		break;
	case MONO_TYPE_STRING:
		res = mono_defaults.string_class;
		break;
	case MONO_TYPE_ARRAY:
		res = mono_defaults.array_class;
		break;
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_PTR:
		/* Not really sure about these. */
		res = mono_class_from_mono_type (type->data.type);
		break;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_VALUETYPE:
		res = type->data.klass;
		break;
	default:
		g_warning ("implement me %02x\n", type->type);
		g_assert_not_reached ();
	}
	
	return res;
}

/**
 * @image: context where the image is created
 * @type_spec:  typespec token
 * @at: an optional pointer to return the array type
 */
static MonoClass *
mono_class_create_from_typespec (MonoImage *image, guint32 type_spec)
{
	guint32 idx = mono_metadata_token_index (type_spec);
	MonoTableInfo *t;
	guint32 cols [MONO_TYPESPEC_SIZE];       
	const char *ptr;
	guint32 len;
	MonoType *type;
	MonoClass *class, *eclass;

	t = &image->tables [MONO_TABLE_TYPESPEC];
	
	mono_metadata_decode_row (t, idx-1, cols, MONO_TYPESPEC_SIZE);
	ptr = mono_metadata_blob_heap (image, cols [MONO_TYPESPEC_SIGNATURE]);
	len = mono_metadata_decode_value (ptr, &ptr);
	type = mono_metadata_parse_type (image, MONO_PARSE_TYPE, 0, ptr, &ptr);

	switch (type->type) {
	case MONO_TYPE_ARRAY:
		eclass = mono_class_from_mono_type (type->data.array->type);
		class = mono_array_class_get (eclass, type->data.array->rank);
		break;
	case MONO_TYPE_SZARRAY:
		eclass = mono_class_from_mono_type (type->data.type);
		class = mono_array_class_get (eclass, 1);
		break;
	default:
		g_warning ("implement me: %08x", type->type);
		g_assert_not_reached ();		
	}

	mono_metadata_free_type (type);
	
	return class;
}

/**
 * mono_array_class_get:
 * @eclass: element type class
 * @rank: the dimension of the array class
 *
 * Returns: a class object describing the array with element type @etype and 
 * dimension @rank. 
 */
MonoClass *
mono_array_class_get (MonoClass *eclass, guint32 rank)
{
	MonoImage *image;
	MonoClass *class;
	static MonoClass *parent = NULL;
	MonoArrayClass *aclass;
	guint32 key;

	g_assert (rank <= 255);

	if (!parent)
		parent = mono_defaults.array_class;

	image = eclass->image;

	g_assert (!eclass->type_token ||
		  mono_metadata_token_table (eclass->type_token) == MONO_TABLE_TYPEDEF);
	
	key = ((rank & 0xff) << 24) | (eclass->type_token & 0xffffff);
	if ((class = g_hash_table_lookup (image->array_cache, GUINT_TO_POINTER (key))))
		return class;
	
	aclass = g_new0 (MonoArrayClass, 1);
	class = (MonoClass *)aclass;
       
	class->image = image;
	class->name_space = "System";
	class->name = "Array";
	class->type_token = 0;
	class->flags = TYPE_ATTRIBUTE_CLASS;
	class->parent = parent;
	class->instance_size = mono_class_instance_size (class->parent);
	class->class_size = 0;

	aclass->rank = rank;
	aclass->element_class = eclass;
	
	g_hash_table_insert (image->array_cache, GUINT_TO_POINTER (key), class);
	return class;
}

/**
 * mono_class_instance_size:
 * @klass: a class 
 * 
 * Returns: the size of an object instance
 */
gint32
mono_class_instance_size (MonoClass *klass)
{
	
	if (!klass->metadata_inited)
		mono_class_metadata_init (klass);

	return klass->instance_size;
}

/**
 * mono_class_value_size:
 * @klass: a class 
 *
 * This function is used for value types, and return the
 * space and the alignment to store that kind of value object.
 *
 * Returns: the size of a value of kind @klass
 */
gint32
mono_class_value_size      (MonoClass *klass, guint32 *align)
{
	gint32 size;

	/* fixme: check disable, because we still have external revereces to
	 * mscorlib and Dummy Objects 
	 */
	//g_assert (klass->valuetype);

	size = mono_class_instance_size (klass) - sizeof (MonoObject);

	if (align) {
		if (size <= 4)
			*align = 4;
		else
			*align = 8;
	}

	return size;
}

/**
 * mono_class_data_size:
 * @klass: a class 
 * 
 * Returns: the size of the static class data
 */
gint32
mono_class_data_size (MonoClass *klass)
{
	
	if (!klass->metadata_inited)
		mono_class_metadata_init (klass);

	return klass->class_size;
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
	case MONO_TOKEN_TYPE_REF:
		return mono_class_create_from_typeref (image, type_token);
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

MonoClass *
mono_class_from_name (MonoImage *image, const char* name_space, const char *name)
{
	GHashTable *nspace_table;
	guint32 token;

	nspace_table = g_hash_table_lookup (image->name_cache, name_space);
	if (!nspace_table)
		return 0;
	token = GPOINTER_TO_UINT (g_hash_table_lookup (nspace_table, name));
	
	if (!token)
		g_error ("token not found for %s.%s in image %s", name_space, name, image->name);

	token = MONO_TOKEN_TYPE_DEF | token;

	return mono_class_get (image, token);
}

/**
 * mono_array_element_size:
 * @ac: pointer to a #MonoArrayClass
 *
 * Returns: the size of single array element.
 */
gint32
mono_array_element_size (MonoArrayClass *ac)
{
	gint32 esize;

	esize = mono_class_instance_size (ac->element_class);
	
	if (ac->element_class->valuetype)
		esize -= sizeof (MonoObject);
	
	return esize;
}
