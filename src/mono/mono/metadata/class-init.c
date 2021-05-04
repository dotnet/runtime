/**
 * \file MonoClass construction and initialization
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Copyright 2012 Xamarin Inc (http://www.xamarin.com)
 * Copyright 2018 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>
#include <mono/metadata/class-init.h>
#include <mono/metadata/class-init-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/custom-attrs-internals.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/exception-internals.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/security-core-clr.h>
#include <mono/metadata/security-manager.h>
#include <mono/metadata/verify-internals.h>
#include <mono/metadata/abi-details.h>
#include <mono/utils/checked-build.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/mono-error-internals.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-memory-model.h>
#include <mono/utils/unlocked.h>
#ifdef MONO_CLASS_DEF_PRIVATE
/* Class initialization gets to see the fields of MonoClass */
#define REALLY_INCLUDE_CLASS_DEF 1
#include <mono/metadata/class-private-definition.h>
#undef REALLY_INCLUDE_CLASS_DEF
#endif

#define FEATURE_COVARIANT_RETURNS

gboolean mono_print_vtable = FALSE;
gboolean mono_align_small_structs = FALSE;

/* Set by the EE */
gint32 mono_simd_register_size;

/* Statistics */
static gint32 classes_size;
static gint32 inflated_classes_size;
gint32 mono_inflated_methods_size;
static gint32 class_def_count, class_gtd_count, class_ginst_count, class_gparam_count, class_array_count, class_pointer_count;

/* Low level lock which protects data structures in this module */
static mono_mutex_t classes_mutex;

static gboolean class_kind_may_contain_generic_instances (MonoTypeKind kind);
static void mono_generic_class_setup_parent (MonoClass *klass, MonoClass *gtd);
static int generic_array_methods (MonoClass *klass);
static void setup_generic_array_ifaces (MonoClass *klass, MonoClass *iface, MonoMethod **methods, int pos, GHashTable *cache);
static gboolean class_has_isbyreflike_attribute (MonoClass *klass);

static
GENERATE_TRY_GET_CLASS_WITH_CACHE(icollection, "System.Collections.Generic", "ICollection`1");
static
GENERATE_TRY_GET_CLASS_WITH_CACHE(ienumerable, "System.Collections.Generic", "IEnumerable`1");
static
GENERATE_TRY_GET_CLASS_WITH_CACHE(ireadonlycollection, "System.Collections.Generic", "IReadOnlyCollection`1");

/* This TLS variable points to a GSList of classes which have setup_fields () executing */
static MonoNativeTlsKey setup_fields_tls_id;

static MonoNativeTlsKey init_pending_tls_id;

static void
classes_lock (void)
{
	mono_locks_os_acquire (&classes_mutex, ClassesLock);
}

static void
classes_unlock (void)
{
	mono_locks_os_release (&classes_mutex, ClassesLock);
}

/*
We use gclass recording to allow recursive system f types to be referenced by a parent.

Given the following type hierarchy:

class TextBox : TextBoxBase<TextBox> {}
class TextBoxBase<T> : TextInput<TextBox> where T : TextBoxBase<T> {}
class TextInput<T> : Input<T> where T: TextInput<T> {}
class Input<T> {}

The runtime tries to load TextBoxBase<>.
To load TextBoxBase<> to do so it must resolve the parent which is TextInput<TextBox>.
To instantiate TextInput<TextBox> it must resolve TextInput<> and TextBox.
To load TextBox it must resolve the parent which is TextBoxBase<TextBox>.

At this point the runtime must instantiate TextBoxBase<TextBox>. Both types are partially loaded 
at this point, iow, both are registered in the type map and both and a NULL parent. This means
that the resulting generic instance will have a NULL parent, which is wrong and will cause breakage.

To fix that what we do is to record all generic instantes created while resolving the parent of
any generic type definition and, after resolved, correct the parent field if needed.

*/
static int record_gclass_instantiation;
static GSList *gclass_recorded_list;
typedef gboolean (*gclass_record_func) (MonoClass*, void*);

/* 
 * LOCKING: loader lock must be held until pairing disable_gclass_recording is called.
*/
static void
enable_gclass_recording (void)
{
	++record_gclass_instantiation;
}

/* 
 * LOCKING: loader lock must be held since pairing enable_gclass_recording was called.
*/
static void
disable_gclass_recording (gclass_record_func func, void *user_data)
{
	GSList **head = &gclass_recorded_list;

	g_assert (record_gclass_instantiation > 0);
	--record_gclass_instantiation;

	while (*head) {
		GSList *node = *head;
		if (func ((MonoClass*)node->data, user_data)) {
			*head = node->next;
			g_slist_free_1 (node);
		} else {
			head = &node->next;
		}
	}

	/* We automatically discard all recorded gclasses when disabled. */
	if (!record_gclass_instantiation && gclass_recorded_list) {
		g_slist_free (gclass_recorded_list);
		gclass_recorded_list = NULL;
	}
}

#define mono_class_new0(klass,struct_type, n_structs)		\
    ((struct_type *) mono_class_alloc0 ((klass), ((gsize) sizeof (struct_type)) * ((gsize) (n_structs))))

/**
 * mono_class_setup_basic_field_info:
 * \param class The class to initialize
 *
 * Initializes the following fields in MonoClass:
 * * klass->fields (only field->parent and field->name)
 * * klass->field.count
 * * klass->first_field_idx
 * LOCKING: Acquires the loader lock
 */
void
mono_class_setup_basic_field_info (MonoClass *klass)
{
	MonoGenericClass *gklass;
	MonoClassField *field;
	MonoClassField *fields;
	MonoClass *gtd;
	MonoImage *image;
	int i, top;

	if (klass->fields)
		return;

	gklass = mono_class_try_get_generic_class (klass);
	gtd = gklass ? mono_class_get_generic_type_definition (klass) : NULL;
	image = klass->image;


	if (gklass && image_is_dynamic (gklass->container_class->image) && !gklass->container_class->wastypebuilder) {
		/*
		 * This happens when a generic instance of an unfinished generic typebuilder
		 * is used as an element type for creating an array type. We can't initialize
		 * the fields of this class using the fields of gklass, since gklass is not
		 * finished yet, fields could be added to it later.
		 */
		return;
	}

	if (gtd) {
		mono_class_setup_basic_field_info (gtd);

		mono_loader_lock ();
		mono_class_set_field_count (klass, mono_class_get_field_count (gtd));
		mono_loader_unlock ();
	}

	top = mono_class_get_field_count (klass);

	fields = (MonoClassField *)mono_class_alloc0 (klass, sizeof (MonoClassField) * top);

	/*
	 * Fetch all the field information.
	 */
	int first_field_idx = mono_class_has_static_metadata (klass) ? mono_class_get_first_field_idx (klass) : 0;
	for (i = 0; i < top; i++) {
		field = &fields [i];
		field->parent = klass;

		if (gtd) {
			field->name = mono_field_get_name (&gtd->fields [i]);
		} else {
			int idx = first_field_idx + i;
			/* first_field_idx and idx points into the fieldptr table */
			guint32 name_idx = mono_metadata_decode_table_row_col (image, MONO_TABLE_FIELD, idx, MONO_FIELD_NAME);
			/* The name is needed for fieldrefs */
			field->name = mono_metadata_string_heap (image, name_idx);
		}
	}

	mono_memory_barrier ();

	mono_loader_lock ();
	if (!klass->fields)
		klass->fields = fields;
	mono_loader_unlock ();
}

/**
 * mono_class_setup_fields:
 * \p klass The class to initialize
 *
 * Initializes klass->fields, computes class layout and sizes.
 * typebuilder_setup_fields () is the corresponding function for dynamic classes.
 * Sets the following fields in \p klass:
 *  - all the fields initialized by mono_class_init_sizes ()
 *  - element_class/cast_class (for enums)
 *  - sizes:element_size (for arrays)
 *  - field->type/offset for all fields
 *  - fields_inited
 *
 * LOCKING: Acquires the loader lock.
 */
void
mono_class_setup_fields (MonoClass *klass)
{
	ERROR_DECL (error);
	MonoImage *m = klass->image;
	int top;
	guint32 layout = mono_class_get_flags (klass) & TYPE_ATTRIBUTE_LAYOUT_MASK;
	int i;
	guint32 real_size = 0;
	guint32 packing_size = 0;
	int instance_size;
	gboolean explicit_size;
	MonoClassField *field;
	MonoGenericClass *gklass = mono_class_try_get_generic_class (klass);
	MonoClass *gtd = gklass ? mono_class_get_generic_type_definition (klass) : NULL;

	if (klass->fields_inited)
		return;

	if (gklass && image_is_dynamic (gklass->container_class->image) && !gklass->container_class->wastypebuilder) {
		/*
		 * This happens when a generic instance of an unfinished generic typebuilder
		 * is used as an element type for creating an array type. We can't initialize
		 * the fields of this class using the fields of gklass, since gklass is not
		 * finished yet, fields could be added to it later.
		 */
		return;
	}

	mono_class_setup_basic_field_info (klass);
	top = mono_class_get_field_count (klass);

	if (gtd) {
		mono_class_setup_fields (gtd);
		if (mono_class_set_type_load_failure_causedby_class (klass, gtd, "Generic type definition failed"))
			return;
	}

	instance_size = 0;
	if (klass->parent) {
		/* For generic instances, klass->parent might not have been initialized */
		mono_class_init_internal (klass->parent);
		mono_class_setup_fields (klass->parent);
		if (mono_class_set_type_load_failure_causedby_class (klass, klass->parent, "Could not set up parent class"))
			return;
		instance_size = klass->parent->instance_size;
	} else {
		instance_size = MONO_ABI_SIZEOF (MonoObject);
	}

	/* Get the real size */
	explicit_size = mono_metadata_packing_from_typedef (klass->image, klass->type_token, &packing_size, &real_size);
	if (explicit_size)
		instance_size += real_size;

	if (mono_is_corlib_image (klass->image) && !strcmp (klass->name_space, "System.Numerics") && !strcmp (klass->name, "Register")) {
		if (mono_simd_register_size)
			instance_size += mono_simd_register_size;
	}

	/*
	 * This function can recursively call itself.
	 * Prevent infinite recursion by using a list in TLS.
	 */
	GSList *init_list = (GSList *)mono_native_tls_get_value (setup_fields_tls_id);
	if (g_slist_find (init_list, klass))
		return;
	init_list = g_slist_prepend (init_list, klass);
	mono_native_tls_set_value (setup_fields_tls_id, init_list);

	/*
	 * Fetch all the field information.
	 */
	int first_field_idx = mono_class_has_static_metadata (klass) ? mono_class_get_first_field_idx (klass) : 0;
	for (i = 0; i < top; i++) {
		int idx = first_field_idx + i;
		field = &klass->fields [i];

		if (!field->type) {
			mono_field_resolve_type (field, error);
			if (!is_ok (error)) {
				/*mono_field_resolve_type already failed class*/
				mono_error_cleanup (error);
				break;
			}
			if (!field->type)
				g_error ("could not resolve %s:%s\n", mono_type_get_full_name(klass), field->name);
			g_assert (field->type);
		}

		if (!mono_type_get_underlying_type (field->type)) {
			mono_class_set_type_load_failure (klass, "Field '%s' is an enum type with a bad underlying type", field->name);
			break;
		}

		if (mono_field_is_deleted (field))
			continue;
		if (layout == TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) {
			guint32 uoffset;
			mono_metadata_field_info (m, idx, &uoffset, NULL, NULL);
			int offset = uoffset;

			if (offset == (guint32)-1 && !(field->type->attrs & FIELD_ATTRIBUTE_STATIC)) {
				mono_class_set_type_load_failure (klass, "Missing field layout info for %s", field->name);
				break;
			}
			if (offset < -1) { /*-1 is used to encode special static fields */
				mono_class_set_type_load_failure (klass, "Field '%s' has a negative offset %d", field->name, offset);
				break;
			}
			if (mono_class_is_gtd (klass)) {
				mono_class_set_type_load_failure (klass, "Generic class cannot have explicit layout.");
				break;
			}
		}
		if (mono_type_has_exceptions (field->type)) {
			char *class_name = mono_type_get_full_name (klass);
			char *type_name = mono_type_full_name (field->type);

			mono_class_set_type_load_failure (klass, "Invalid type %s for instance field %s:%s", type_name, class_name, field->name);
			g_free (class_name);
			g_free (type_name);
			break;
		}
		/* The def_value of fields is compute lazily during vtable creation */
	}

	if (!mono_class_has_failure (klass)) {
		mono_loader_lock ();
		mono_class_layout_fields (klass, instance_size, packing_size, real_size, FALSE);
		mono_loader_unlock ();
	}

	init_list = g_slist_remove (init_list, klass);
	mono_native_tls_set_value (setup_fields_tls_id, init_list);
}

static gboolean
discard_gclass_due_to_failure (MonoClass *gclass, void *user_data)
{
	return mono_class_get_generic_class (gclass)->container_class == user_data;
}

static gboolean
fix_gclass_incomplete_instantiation (MonoClass *gclass, void *user_data)
{
	MonoClass *gtd = (MonoClass*)user_data;
	/* Only try to fix generic instances of @gtd */
	if (mono_class_get_generic_class (gclass)->container_class != gtd)
		return FALSE;

	/* Check if the generic instance has no parent. */
	if (gtd->parent && !gclass->parent)
		mono_generic_class_setup_parent (gclass, gtd);

	return TRUE;
}

static void
mono_class_set_failure_and_error (MonoClass *klass, MonoError *error, const char *msg)
{
	mono_class_set_type_load_failure (klass, "%s", msg);
	mono_error_set_type_load_class (error, klass, "%s", msg);
}

/**
 * mono_class_create_from_typedef:
 * \param image: image where the token is valid
 * \param type_token:  typedef token
 * \param error:  used to return any error found while creating the type
 *
 * Create the MonoClass* representing the specified type token.
 * \p type_token must be a TypeDef token.
 *
 * FIXME: don't return NULL on failure, just let the caller figure it out.
 */
MonoClass *
mono_class_create_from_typedef (MonoImage *image, guint32 type_token, MonoError *error)
{
	MonoTableInfo *tt = &image->tables [MONO_TABLE_TYPEDEF];
	MonoClass *klass, *parent = NULL;
	guint32 cols [MONO_TYPEDEF_SIZE];
	guint32 cols_next [MONO_TYPEDEF_SIZE];
	guint tidx = mono_metadata_token_index (type_token);
	MonoGenericContext *context = NULL;
	const char *name, *nspace;
	guint icount = 0; 
	MonoClass **interfaces;
	guint32 field_last, method_last;
	guint32 nesting_tokeen;

	error_init (error);

	/* FIXME: metadata-update - this function needs extensive work */
	if (mono_metadata_token_table (type_token) != MONO_TABLE_TYPEDEF || tidx > table_info_get_rows (tt)) {
		mono_error_set_bad_image (error, image, "Invalid typedef token %x", type_token);
		return NULL;
	}

	mono_loader_lock ();

	if ((klass = (MonoClass *)mono_internal_hash_table_lookup (&image->class_cache, GUINT_TO_POINTER (type_token)))) {
		mono_loader_unlock ();
		return klass;
	}

	mono_metadata_decode_row (tt, tidx - 1, cols, MONO_TYPEDEF_SIZE);
	
	name = mono_metadata_string_heap (image, cols [MONO_TYPEDEF_NAME]);
	nspace = mono_metadata_string_heap (image, cols [MONO_TYPEDEF_NAMESPACE]);

	if (mono_metadata_has_generic_params (image, type_token)) {
		klass = (MonoClass*)mono_image_alloc0 (image, sizeof (MonoClassGtd));
		klass->class_kind = MONO_CLASS_GTD;
		UnlockedAdd (&classes_size, sizeof (MonoClassGtd));
		++class_gtd_count;
	} else {
		klass = (MonoClass*)mono_image_alloc0 (image, sizeof (MonoClassDef));
		klass->class_kind = MONO_CLASS_DEF;
		UnlockedAdd (&classes_size, sizeof (MonoClassDef));
		++class_def_count;
	}

	klass->name = name;
	klass->name_space = nspace;

	MONO_PROFILER_RAISE (class_loading, (klass));

	klass->image = image;
	klass->type_token = type_token;
	mono_class_set_flags (klass, cols [MONO_TYPEDEF_FLAGS]);

	mono_internal_hash_table_insert (&image->class_cache, GUINT_TO_POINTER (type_token), klass);

	/*
	 * Check whether we're a generic type definition.
	 */
	if (mono_class_is_gtd (klass)) {
		MonoGenericContainer *generic_container = mono_metadata_load_generic_params (image, klass->type_token, NULL, klass);
		context = &generic_container->context;
		mono_class_set_generic_container (klass, generic_container);
		MonoType *canonical_inst = &((MonoClassGtd*)klass)->canonical_inst;
		canonical_inst->type = MONO_TYPE_GENERICINST;
		canonical_inst->data.generic_class = mono_metadata_lookup_generic_class (klass, context->class_inst, FALSE);
		enable_gclass_recording ();
	}

	if (cols [MONO_TYPEDEF_EXTENDS]) {
		MonoClass *tmp;
		guint32 parent_token = mono_metadata_token_from_dor (cols [MONO_TYPEDEF_EXTENDS]);

		if (mono_metadata_token_table (parent_token) == MONO_TABLE_TYPESPEC) {
			/*WARNING: this must satisfy mono_metadata_type_hash*/
			klass->this_arg.byref = 1;
			klass->this_arg.data.klass = klass;
			klass->this_arg.type = MONO_TYPE_CLASS;
			klass->_byval_arg.data.klass = klass;
			klass->_byval_arg.type = MONO_TYPE_CLASS;
		}
		parent = mono_class_get_checked (image, parent_token, error);
		if (parent && context) /* Always inflate */
			parent = mono_class_inflate_generic_class_checked (parent, context, error);

		if (parent == NULL) {
			mono_class_set_type_load_failure (klass, "%s", mono_error_get_message (error));
			goto parent_failure;
		}

		for (tmp = parent; tmp; tmp = tmp->parent) {
			if (tmp == klass) {
				mono_class_set_failure_and_error (klass, error, "Cycle found while resolving parent");
				goto parent_failure;
			}
			if (mono_class_is_gtd (klass) && mono_class_is_ginst (tmp) && mono_class_get_generic_class (tmp)->container_class == klass) {
				mono_class_set_failure_and_error (klass, error, "Parent extends generic instance of this type");
				goto parent_failure;
			}
		}
	}

	mono_class_setup_parent (klass, parent);

	/* uses ->valuetype, which is initialized by mono_class_setup_parent above */
	mono_class_setup_mono_type (klass);

	if (mono_class_is_gtd (klass))
		disable_gclass_recording (fix_gclass_incomplete_instantiation, klass);

	/* 
	 * This might access klass->_byval_arg for recursion generated by generic constraints,
	 * so it has to come after setup_mono_type ().
	 */
	if ((nesting_tokeen = mono_metadata_nested_in_typedef (image, type_token))) {
		klass->nested_in = mono_class_create_from_typedef (image, nesting_tokeen, error);
		if (!is_ok (error)) {
			/*FIXME implement a mono_class_set_failure_from_mono_error */
			mono_class_set_type_load_failure (klass, "%s",  mono_error_get_message (error));
			mono_loader_unlock ();
			MONO_PROFILER_RAISE (class_failed, (klass));
			return NULL;
		}
	}

	if ((mono_class_get_flags (klass) & TYPE_ATTRIBUTE_STRING_FORMAT_MASK) == TYPE_ATTRIBUTE_UNICODE_CLASS)
		klass->unicode = 1;

#ifdef HOST_WIN32
	if ((mono_class_get_flags (klass) & TYPE_ATTRIBUTE_STRING_FORMAT_MASK) == TYPE_ATTRIBUTE_AUTO_CLASS)
		klass->unicode = 1;
#endif

	klass->cast_class = klass->element_class = klass;
	if (mono_is_corlib_image (klass->image)) {
		switch (m_class_get_byval_arg (klass)->type) {
			case MONO_TYPE_I1:
				if (mono_defaults.byte_class)
					klass->cast_class = mono_defaults.byte_class;
				break;
			case MONO_TYPE_U1:
				if (mono_defaults.sbyte_class)
					mono_defaults.sbyte_class = klass;
				break;
			case MONO_TYPE_I2:
				if (mono_defaults.uint16_class)
					mono_defaults.uint16_class = klass;
				break;
			case MONO_TYPE_U2:
				if (mono_defaults.int16_class)
					klass->cast_class = mono_defaults.int16_class;
				break;
			case MONO_TYPE_I4:
				if (mono_defaults.uint32_class)
					mono_defaults.uint32_class = klass;
				break;
			case MONO_TYPE_U4:
				if (mono_defaults.int32_class)
					klass->cast_class = mono_defaults.int32_class;
				break;
			case MONO_TYPE_I8:
				if (mono_defaults.uint64_class)
					mono_defaults.uint64_class = klass;
				break;
			case MONO_TYPE_U8:
				if (mono_defaults.int64_class)
					klass->cast_class = mono_defaults.int64_class;
				break;
			default:
				break;
		}
	}

	if (!klass->enumtype) {
		if (!mono_metadata_interfaces_from_typedef_full (
			    image, type_token, &interfaces, &icount, FALSE, context, error)){

			mono_class_set_type_load_failure (klass, "%s", mono_error_get_message (error));
			mono_loader_unlock ();
			MONO_PROFILER_RAISE (class_failed, (klass));
			return NULL;
		}

		/* This is required now that it is possible for more than 2^16 interfaces to exist. */
		g_assert(icount <= 65535);

		klass->interfaces = interfaces;
		klass->interface_count = icount;
		klass->interfaces_inited = 1;
	}

	/*g_print ("Load class %s\n", name);*/

	/*
	 * Compute the field and method lists
	 */
	int first_field_idx;
	first_field_idx = cols [MONO_TYPEDEF_FIELD_LIST] - 1;
	mono_class_set_first_field_idx (klass, first_field_idx);
	int first_method_idx;
	first_method_idx = cols [MONO_TYPEDEF_METHOD_LIST] - 1;
	mono_class_set_first_method_idx (klass, first_method_idx);

	if (table_info_get_rows (tt) > tidx){		
		mono_metadata_decode_row (tt, tidx, cols_next, MONO_TYPEDEF_SIZE);
		field_last  = cols_next [MONO_TYPEDEF_FIELD_LIST] - 1;
		method_last = cols_next [MONO_TYPEDEF_METHOD_LIST] - 1;
	} else {
		field_last  = table_info_get_rows (&image->tables [MONO_TABLE_FIELD]);
		method_last = table_info_get_rows (&image->tables [MONO_TABLE_METHOD]);
	}

	if (cols [MONO_TYPEDEF_FIELD_LIST] && 
	    cols [MONO_TYPEDEF_FIELD_LIST] <= table_info_get_rows (&image->tables [MONO_TABLE_FIELD]))
		mono_class_set_field_count (klass, field_last - first_field_idx);
	if (cols [MONO_TYPEDEF_METHOD_LIST] <= table_info_get_rows (&image->tables [MONO_TABLE_METHOD]))
		mono_class_set_method_count (klass, method_last - first_method_idx);

	/* reserve space to store vector pointer in arrays */
	if (mono_is_corlib_image (image) && !strcmp (nspace, "System") && !strcmp (name, "Array")) {
		klass->instance_size += 2 * TARGET_SIZEOF_VOID_P;
		/* TODO: check that array has 0 non-const fields */
	}

	if (klass->enumtype) {
		MonoType *enum_basetype = mono_class_find_enum_basetype (klass, error);
		if (!enum_basetype) {
			/*set it to a default value as the whole runtime can't handle this to be null*/
			klass->cast_class = klass->element_class = mono_defaults.int32_class;
			mono_class_set_type_load_failure (klass, "%s", mono_error_get_message (error));
			mono_loader_unlock ();
			MONO_PROFILER_RAISE (class_failed, (klass));
			return NULL;
		}
		klass->cast_class = klass->element_class = mono_class_from_mono_type_internal (enum_basetype);
	}

	/*
	 * If we're a generic type definition, load the constraints.
	 * We must do this after the class has been constructed to make certain recursive scenarios
	 * work.
	 */
	if (mono_class_is_gtd (klass) && !mono_metadata_load_generic_param_constraints_checked (image, type_token, mono_class_get_generic_container (klass), error)) {
		mono_class_set_type_load_failure (klass, "Could not load generic parameter constrains due to %s", mono_error_get_message (error));
		mono_loader_unlock ();
		MONO_PROFILER_RAISE (class_failed, (klass));
		return NULL;
	}

	if (klass->image->assembly_name && !strcmp (klass->image->assembly_name, "Mono.Simd") && !strcmp (nspace, "Mono.Simd")) {
		if (!strncmp (name, "Vector", 6))
			klass->simd_type = !strcmp (name + 6, "2d") || !strcmp (name + 6, "2ul") || !strcmp (name + 6, "2l") || !strcmp (name + 6, "4f") || !strcmp (name + 6, "4ui") || !strcmp (name + 6, "4i") || !strcmp (name + 6, "8s") || !strcmp (name + 6, "8us") || !strcmp (name + 6, "16b") || !strcmp (name + 6, "16sb");
	} else if (klass->image->assembly_name && !strcmp (klass->image->assembly_name, "System.Numerics") && !strcmp (nspace, "System.Numerics")) {
		/* The JIT can't handle SIMD types with != 16 size yet */
		//if (!strcmp (name, "Vector2") || !strcmp (name, "Vector3") || !strcmp (name, "Vector4"))
		if (!strcmp (name, "Vector4"))
			klass->simd_type = 1;
	}

	// compute is_byreflike
	if (m_class_is_valuetype (klass))
		if (class_has_isbyreflike_attribute (klass))
			klass->is_byreflike = 1; 
		
	mono_loader_unlock ();

	MONO_PROFILER_RAISE (class_loaded, (klass));

	return klass;

parent_failure:
	if (mono_class_is_gtd (klass))
		disable_gclass_recording (discard_gclass_due_to_failure, klass);

	mono_class_setup_mono_type (klass);
	mono_loader_unlock ();
	MONO_PROFILER_RAISE (class_failed, (klass));
	return NULL;
}


static void
mono_generic_class_setup_parent (MonoClass *klass, MonoClass *gtd)
{
	if (gtd->parent) {
		ERROR_DECL (error);
		MonoGenericClass *gclass = mono_class_get_generic_class (klass);

		klass->parent = mono_class_inflate_generic_class_checked (gtd->parent, mono_generic_class_get_context (gclass), error);
		if (!is_ok (error)) {
			/*Set parent to something safe as the runtime doesn't handle well this kind of failure.*/
			klass->parent = mono_defaults.object_class;
			mono_class_set_type_load_failure (klass, "Parent is a generic type instantiation that failed due to: %s", mono_error_get_message (error));
			mono_error_cleanup (error);
		}
	}
	mono_loader_lock ();
	if (klass->parent)
		mono_class_setup_parent (klass, klass->parent);

	if (klass->enumtype) {
		klass->cast_class = gtd->cast_class;
		klass->element_class = gtd->element_class;
	}
	mono_loader_unlock ();
}

struct FoundAttrUD {
	/* inputs */
	const char *nspace;
	const char *name;
	gboolean in_corlib;
	/* output */
	gboolean has_attr;
};

static gboolean
has_wellknown_attribute_func (MonoImage *image, guint32 typeref_scope_token, const char *nspace, const char *name, guint32 method_token, gpointer user_data)
{
	struct FoundAttrUD *has_attr = (struct FoundAttrUD *)user_data;
	if (!strcmp (name, has_attr->name) && !strcmp (nspace, has_attr->nspace)) {
		has_attr->has_attr = TRUE;
		return TRUE;
	}
	/* TODO: use typeref_scope_token to check that attribute comes from
	 * corlib if in_corlib is TRUE, without triggering an assembly load.
	 * If we're inside corlib, expect the scope to be
	 * MONO_RESOLUTION_SCOPE_MODULE I think, if we're outside it'll be an
	 * MONO_RESOLUTION_SCOPE_ASSEMBLYREF and we'll need to check the
	 * name.*/
	return FALSE;
}

static gboolean
class_has_wellknown_attribute (MonoClass *klass, const char *nspace, const char *name, gboolean in_corlib)
{
	struct FoundAttrUD has_attr;
	has_attr.nspace = nspace;
	has_attr.name = name;
	has_attr.in_corlib = in_corlib;
	has_attr.has_attr = FALSE;

	mono_class_metadata_foreach_custom_attr (klass, has_wellknown_attribute_func, &has_attr);

	return has_attr.has_attr;
}

static gboolean
method_has_wellknown_attribute (MonoMethod *method, const char *nspace, const char *name, gboolean in_corlib)
{
	struct FoundAttrUD has_attr;
	has_attr.nspace = nspace;
	has_attr.name = name;
	has_attr.in_corlib = in_corlib;
	has_attr.has_attr = FALSE;

	mono_method_metadata_foreach_custom_attr (method, has_wellknown_attribute_func, &has_attr);

	return has_attr.has_attr;
}


static gboolean
class_has_isbyreflike_attribute (MonoClass *klass)
{
	return class_has_wellknown_attribute (klass, "System.Runtime.CompilerServices", "IsByRefLikeAttribute", TRUE);
}


gboolean
mono_class_setup_method_has_preserve_base_overrides_attribute (MonoMethod *method)
{
	MonoImage *image = m_class_get_image (method->klass);
	/* FIXME: implement well known attribute check for dynamic images */
	if (image_is_dynamic (image))
		return FALSE;
	return method_has_wellknown_attribute (method, "System.Runtime.CompilerServices", "PreserveBaseOverridesAttribute", TRUE);
}

static gboolean
check_valid_generic_inst_arguments (MonoGenericInst *inst, MonoError *error)
{
	for (int i = 0; i < inst->type_argc; i++) {
		if (!mono_type_is_valid_generic_argument (inst->type_argv [i])) {
			char *type_name = mono_type_full_name (inst->type_argv [i]);
			mono_error_set_invalid_program (error, "generic type cannot be instantiated with type '%s'", type_name);
			g_free (type_name);
			return FALSE;
		}
	}
	return TRUE;
}

/*
 * Create the `MonoClass' for an instantiation of a generic type.
 * We only do this if we actually need it.
 * This will sometimes return a GTD due to checking the cached_class.
 */
MonoClass*
mono_class_create_generic_inst (MonoGenericClass *gclass)
{
	MonoClass *klass, *gklass;

	if (gclass->cached_class)
		return gclass->cached_class;

	klass = (MonoClass *)mono_mem_manager_alloc0 ((MonoMemoryManager*)gclass->owner, sizeof (MonoClassGenericInst));

	gklass = gclass->container_class;

	if (gklass->nested_in) {
		/* The nested_in type should not be inflated since it's possible to produce a nested type with less generic arguments*/
		klass->nested_in = gklass->nested_in;
	}

	klass->name = gklass->name;
	klass->name_space = gklass->name_space;
	
	klass->image = gklass->image;
	klass->type_token = gklass->type_token;

	klass->class_kind = MONO_CLASS_GINST;
	//FIXME add setter
	((MonoClassGenericInst*)klass)->generic_class = gclass;

	klass->_byval_arg.type = MONO_TYPE_GENERICINST;
	klass->this_arg.type = m_class_get_byval_arg (klass)->type;
	klass->this_arg.data.generic_class = klass->_byval_arg.data.generic_class = gclass;
	klass->this_arg.byref = TRUE;
	klass->enumtype = gklass->enumtype;
	klass->valuetype = gklass->valuetype;

	if (gklass->image->assembly_name && !strcmp (gklass->image->assembly_name, "System.Numerics.Vectors") && !strcmp (gklass->name_space, "System.Numerics") && !strcmp (gklass->name, "Vector`1")) {
		g_assert (gclass->context.class_inst);
		g_assert (gclass->context.class_inst->type_argc > 0);
		if (mono_type_is_primitive (gclass->context.class_inst->type_argv [0]))
			klass->simd_type = 1;
	}

	if (mono_is_corlib_image (gklass->image) &&
		(!strcmp (gklass->name, "Vector`1") || !strcmp (gklass->name, "Vector64`1") || !strcmp (gklass->name, "Vector128`1") || !strcmp (gklass->name, "Vector256`1"))) {
		MonoType *etype = gclass->context.class_inst->type_argv [0];
		if (mono_type_is_primitive (etype) && etype->type != MONO_TYPE_CHAR && etype->type != MONO_TYPE_BOOLEAN)
			klass->simd_type = 1;
	}

	klass->is_array_special_interface = gklass->is_array_special_interface;

	klass->cast_class = klass->element_class = klass;

	if (m_class_is_valuetype (klass)) {
		klass->is_byreflike = gklass->is_byreflike;
	}

	if (gclass->is_dynamic) {
		/*
		 * We don't need to do any init workf with unbaked typebuilders. Generic instances created at this point will be later unregistered and/or fixed.
		 * This is to avoid work that would probably give wrong results as fields change as we build the TypeBuilder.
		 * See remove_instantiations_of_and_ensure_contents in reflection.c and its usage in reflection.c to understand the fixup stage of SRE banking.
		*/
		if (!gklass->wastypebuilder)
			klass->inited = 1;

		if (klass->enumtype) {
			/*
			 * For enums, gklass->fields might not been set, but instance_size etc. is 
			 * already set in mono_reflection_create_internal_class (). For non-enums,
			 * these will be computed normally in mono_class_layout_fields ().
			 */
			klass->instance_size = gklass->instance_size;
			klass->sizes.class_size = gklass->sizes.class_size;
			klass->size_inited = 1;
		}
	}

	{
		ERROR_DECL (error_inst);
		if (!check_valid_generic_inst_arguments (gclass->context.class_inst, error_inst)) {
			char *gklass_name = mono_type_get_full_name (gklass);
			mono_class_set_type_load_failure (klass, "Could not instantiate %s due to %s", gklass_name, mono_error_get_message (error_inst));
			g_free (gklass_name);
			mono_error_cleanup (error_inst);
		}
	}

	mono_loader_lock ();

	if (gclass->cached_class) {
		mono_loader_unlock ();
		return gclass->cached_class;
	}

	if (record_gclass_instantiation > 0)
		gclass_recorded_list = g_slist_append (gclass_recorded_list, klass);

	if (mono_class_is_nullable (klass))
		klass->cast_class = klass->element_class = mono_class_get_nullable_param_internal (klass);

	MONO_PROFILER_RAISE (class_loading, (klass));

	mono_generic_class_setup_parent (klass, gklass);

	if (gclass->is_dynamic)
		mono_class_setup_supertypes (klass);

	mono_memory_barrier ();
	gclass->cached_class = klass;

	MONO_PROFILER_RAISE (class_loaded, (klass));

	++class_ginst_count;
	inflated_classes_size += sizeof (MonoClassGenericInst);
	
	mono_loader_unlock ();

	return klass;
}

/*
 * For a composite class like uint32[], uint32*, set MonoClass:cast_class to the corresponding "intermediate type" (for
 * arrays) or "verification type" (for pointers) in the sense of ECMA I.8.7.3.  This will be used by
 * mono_class_is_assignable_from.
 *
 * Assumes MonoClass:cast_class is already set (for example if it's an array of
 * some enum) and adjusts it.
 */
static void
class_composite_fixup_cast_class (MonoClass *klass, gboolean for_ptr)
{
	switch (m_class_get_byval_arg (m_class_get_cast_class (klass))->type) {
	case MONO_TYPE_BOOLEAN:
		if (!for_ptr)
			break;
		klass->cast_class = mono_defaults.byte_class;
		break;
	case MONO_TYPE_I1:
		klass->cast_class = mono_defaults.byte_class;
		break;
	case MONO_TYPE_U2:
		klass->cast_class = mono_defaults.int16_class;
		break;
	case MONO_TYPE_U4:
#if TARGET_SIZEOF_VOID_P == 4
	case MONO_TYPE_I:
	case MONO_TYPE_U:
#endif
		klass->cast_class = mono_defaults.int32_class;
		break;
	case MONO_TYPE_U8:
#if TARGET_SIZEOF_VOID_P == 8
	case MONO_TYPE_I:
	case MONO_TYPE_U:
#endif
		klass->cast_class = mono_defaults.int64_class;
		break;
	default:
		break;
	}
}

static gboolean
class_kind_may_contain_generic_instances (MonoTypeKind kind)
{
	/* classes of type generic inst may contain generic arguments from other images,
	 * as well as arrays and pointers whose element types (recursively) may be a generic inst */
	return (kind == MONO_CLASS_GINST || kind == MONO_CLASS_ARRAY || kind == MONO_CLASS_POINTER);
}

/**
 * mono_class_create_bounded_array:
 * \param element_class element class 
 * \param rank the dimension of the array class
 * \param bounded whenever the array has non-zero bounds
 * \returns A class object describing the array with element type \p element_type and 
 * dimension \p rank.
 */
MonoClass *
mono_class_create_bounded_array (MonoClass *eclass, guint32 rank, gboolean bounded)
{
	MonoImage *image;
	MonoClass *klass, *cached, *k;
	MonoClass *parent = NULL;
	GSList *list, *rootlist = NULL;
	int nsize;
	char *name;
	MonoMemoryManager *mm;

	if (rank > 1)
		/* bounded only matters for one-dimensional arrays */
		bounded = FALSE;

	image = eclass->image;

	// FIXME: Optimize this
	mm = class_kind_may_contain_generic_instances ((MonoTypeKind)eclass->class_kind) ? mono_metadata_get_mem_manager_for_class (eclass) : NULL;
	/* Check cache */
	cached = NULL;
	if (rank == 1 && !bounded) {
		if (mm) {
			mono_mem_manager_lock (mm);
			if (!mm->szarray_cache)
				mm->szarray_cache = g_hash_table_new_full (mono_aligned_addr_hash, NULL, NULL, NULL);
			cached = (MonoClass *)g_hash_table_lookup (mm->szarray_cache, eclass);
			mono_mem_manager_unlock (mm);
		} else {
			/*
			 * This case is very frequent not just during compilation because of calls
			 * from mono_class_from_mono_type_internal (), mono_array_new (),
			 * Array:CreateInstance (), etc, so use a separate cache + a separate lock.
			 */
			mono_os_mutex_lock (&image->szarray_cache_lock);
			if (!image->szarray_cache)
				image->szarray_cache = g_hash_table_new (mono_aligned_addr_hash, NULL);
			cached = (MonoClass *)g_hash_table_lookup (image->szarray_cache, eclass);
			mono_os_mutex_unlock (&image->szarray_cache_lock);
		}
	} else {
		if (mm) {
			mono_mem_manager_lock (mm);
			if (!mm->array_cache)
				mm->array_cache = g_hash_table_new_full (mono_aligned_addr_hash, NULL, NULL, NULL);
			rootlist = (GSList *)g_hash_table_lookup (mm->array_cache, eclass);
			for (list = rootlist; list; list = list->next) {
				k = (MonoClass *)list->data;
				if ((m_class_get_rank (k) == rank) && (m_class_get_byval_arg (k)->type == (((rank > 1) || bounded) ? MONO_TYPE_ARRAY : MONO_TYPE_SZARRAY))) {
					cached = k;
					break;
				}
			}
			mono_mem_manager_unlock (mm);
		} else {
			mono_loader_lock ();
			if (!image->array_cache)
				image->array_cache = g_hash_table_new (mono_aligned_addr_hash, NULL);
			rootlist = (GSList *)g_hash_table_lookup (image->array_cache, eclass);
			for (list = rootlist; list; list = list->next) {
				k = (MonoClass *)list->data;
				if ((m_class_get_rank (k) == rank) && (m_class_get_byval_arg (k)->type == (((rank > 1) || bounded) ? MONO_TYPE_ARRAY : MONO_TYPE_SZARRAY))) {
					cached = k;
					break;
				}
			}
			mono_loader_unlock ();
		}
	}
	if (cached)
		return cached;

	parent = mono_defaults.array_class;
	if (!parent->inited)
		mono_class_init_internal (parent);

	klass = mm ? (MonoClass *)mono_mem_manager_alloc0 (mm, sizeof (MonoClassArray)) : (MonoClass *)mono_image_alloc0 (image, sizeof (MonoClassArray));

	klass->image = image;
	klass->name_space = eclass->name_space;
	klass->class_kind = MONO_CLASS_ARRAY;

	nsize = strlen (eclass->name);
	int maxrank = MIN (rank, 32);
	name = (char *)g_malloc (nsize + 2 + maxrank + 1);
	memcpy (name, eclass->name, nsize);
	name [nsize] = '[';
	if (maxrank > 1)
		memset (name + nsize + 1, ',', maxrank - 1);
	if (bounded)
		name [nsize + maxrank] = '*';
	name [nsize + maxrank + bounded] = ']';
	name [nsize + maxrank + bounded + 1] = 0;
	klass->name = mm ? mono_mem_manager_strdup (mm, name) : mono_image_strdup (image, name);
	g_free (name);

	klass->type_token = 0;
	klass->parent = parent;
	klass->instance_size = mono_class_instance_size (klass->parent);
	klass->rank = rank;
	klass->element_class = eclass;

	if (m_class_get_byval_arg (eclass)->type == MONO_TYPE_TYPEDBYREF) {
		/*Arrays of those two types are invalid.*/
		ERROR_DECL (prepared_error);
		mono_error_set_invalid_program (prepared_error, "Arrays of System.TypedReference types are invalid.");
		mono_class_set_failure (klass, mono_error_box (prepared_error, klass->image));
		mono_error_cleanup (prepared_error);
	} else if (m_class_is_byreflike (eclass)) {
		/* .NET Core throws a type load exception: "Could not create array type 'fullname[]'" */
		char *full_name = mono_type_get_full_name (eclass);
		mono_class_set_type_load_failure (klass, "Could not create array type '%s[]'", full_name);
		g_free (full_name);
	} else if (eclass->enumtype && !mono_class_enum_basetype_internal (eclass)) {
		MonoGCHandle ref_info_handle = mono_class_get_ref_info_handle (eclass);
		if (!ref_info_handle || eclass->wastypebuilder) {
			g_warning ("Only incomplete TypeBuilder objects are allowed to be an enum without base_type");
			g_assert (ref_info_handle && !eclass->wastypebuilder);
		}
		/* element_size -1 is ok as this is not an instantitable type*/
		klass->sizes.element_size = -1;
	} else
		klass->sizes.element_size = -1;

	mono_class_setup_supertypes (klass);

	if (mono_class_is_ginst (eclass))
		mono_class_init_internal (eclass);
	if (!eclass->size_inited)
		mono_class_setup_fields (eclass);
	mono_class_set_type_load_failure_causedby_class (klass, eclass, "Could not load array element type");
	/*FIXME we fail the array type, but we have to let other fields be set.*/

	klass->has_references = MONO_TYPE_IS_REFERENCE (m_class_get_byval_arg (eclass)) || m_class_has_references (eclass)? TRUE: FALSE;

	if (eclass->enumtype)
		klass->cast_class = eclass->element_class;
	else
		klass->cast_class = eclass;

	class_composite_fixup_cast_class (klass, FALSE);

	if ((rank > 1) || bounded) {
		MonoArrayType *at = mm ? (MonoArrayType *)mono_mem_manager_alloc0 (mm, sizeof (MonoArrayType)) : (MonoArrayType *)mono_image_alloc0 (image, sizeof (MonoArrayType));
		klass->_byval_arg.type = MONO_TYPE_ARRAY;
		klass->_byval_arg.data.array = at;
		at->eklass = eclass;
		at->rank = rank;
		/* FIXME: complete.... */
	} else {
		klass->_byval_arg.type = MONO_TYPE_SZARRAY;
		klass->_byval_arg.data.klass = eclass;
	}
	klass->this_arg = klass->_byval_arg;
	klass->this_arg.byref = 1;

	if (rank > 32) {
		ERROR_DECL (prepared_error);
		name = mono_type_get_full_name (klass);
		mono_error_set_type_load_class (prepared_error, klass, "%s has too many dimensions.", name);
		mono_class_set_failure (klass, mono_error_box (prepared_error, klass->image));
		mono_error_cleanup (prepared_error);
		g_free (name);
	}

	mono_loader_lock ();

	/* Check cache again */
	cached = NULL;
	if (rank == 1 && !bounded) {
		if (mm) {
			mono_mem_manager_lock (mm);
			cached = (MonoClass *)g_hash_table_lookup (mm->szarray_cache, eclass);
			mono_mem_manager_unlock (mm);
		} else {
			mono_os_mutex_lock (&image->szarray_cache_lock);
			cached = (MonoClass *)g_hash_table_lookup (image->szarray_cache, eclass);
			mono_os_mutex_unlock (&image->szarray_cache_lock);
		}
	} else {
		if (mm) {
			mono_mem_manager_lock (mm);
			rootlist = (GSList *)g_hash_table_lookup (mm->array_cache, eclass);
			for (list = rootlist; list; list = list->next) {
				k = (MonoClass *)list->data;
				if ((m_class_get_rank (k) == rank) && (m_class_get_byval_arg (k)->type == (((rank > 1) || bounded) ? MONO_TYPE_ARRAY : MONO_TYPE_SZARRAY))) {
					cached = k;
					break;
				}
			}
			mono_mem_manager_unlock (mm);
		} else {
			rootlist = (GSList *)g_hash_table_lookup (image->array_cache, eclass);
			for (list = rootlist; list; list = list->next) {
				k = (MonoClass *)list->data;
				if ((m_class_get_rank (k) == rank) && (m_class_get_byval_arg (k)->type == (((rank > 1) || bounded) ? MONO_TYPE_ARRAY : MONO_TYPE_SZARRAY))) {
					cached = k;
					break;
				}
			}
		}
	}
	if (cached) {
		mono_loader_unlock ();
		return cached;
	}

	MONO_PROFILER_RAISE (class_loading, (klass));

	UnlockedAdd (&classes_size, sizeof (MonoClassArray));
	++class_array_count;

	if (rank == 1 && !bounded) {
		if (mm) {
			mono_mem_manager_lock (mm);
			g_hash_table_insert (mm->szarray_cache, eclass, klass);
			mono_mem_manager_unlock (mm);
		} else {
			mono_os_mutex_lock (&image->szarray_cache_lock);
			g_hash_table_insert (image->szarray_cache, eclass, klass);
			mono_os_mutex_unlock (&image->szarray_cache_lock);
		}
	} else {
		if (mm) {
			mono_mem_manager_lock (mm);
			list = g_slist_append (rootlist, klass);
			g_hash_table_insert (mm->array_cache, eclass, list);
			mono_mem_manager_unlock (mm);
		} else {
			list = g_slist_append (rootlist, klass);
			g_hash_table_insert (image->array_cache, eclass, list);
		}
	}

	mono_loader_unlock ();

	MONO_PROFILER_RAISE (class_loaded, (klass));

	return klass;
}

/**
 * mono_class_create_array:
 * \param element_class element class 
 * \param rank the dimension of the array class
 * \returns A class object describing the array with element type \p element_type and 
 * dimension \p rank.
 */
MonoClass *
mono_class_create_array (MonoClass *eclass, guint32 rank)
{
	return mono_class_create_bounded_array (eclass, rank, FALSE);
}

// This is called by mono_class_create_generic_parameter when a new class must be created.
static MonoClass*
make_generic_param_class (MonoGenericParam *param)
{
	MonoClass *klass, **ptr;
	int count, pos, i, min_align;
	MonoGenericParamInfo *pinfo = mono_generic_param_info (param);
	MonoGenericContainer *container = mono_generic_param_owner (param);
	g_assert_checked (container);

	MonoImage *image = mono_get_image_for_generic_param (param);
	gboolean is_mvar = container->is_method;
	gboolean is_anonymous = container->is_anonymous;

	klass = (MonoClass *)mono_image_alloc0 (image, sizeof (MonoClassGenericParam));
	klass->class_kind = MONO_CLASS_GPARAM;
	UnlockedAdd (&classes_size, sizeof (MonoClassGenericParam));
	UnlockedIncrement (&class_gparam_count);

	if (!is_anonymous) {
		CHECKED_METADATA_WRITE_PTR_EXEMPT ( klass->name , pinfo->name );
	} else {
		int n = mono_generic_param_num (param);
		CHECKED_METADATA_WRITE_PTR_LOCAL ( klass->name , mono_make_generic_name_string (image, n) );
	}

	if (is_anonymous) {
		CHECKED_METADATA_WRITE_PTR_EXEMPT ( klass->name_space ,  "" );
	} else if (is_mvar) {
		MonoMethod *omethod = container->owner.method;
		CHECKED_METADATA_WRITE_PTR_EXEMPT ( klass->name_space , (omethod && omethod->klass) ? omethod->klass->name_space : "" );
	} else {
		MonoClass *oklass = container->owner.klass;
		CHECKED_METADATA_WRITE_PTR_EXEMPT ( klass->name_space , oklass ? oklass->name_space : "" );
	}

	MONO_PROFILER_RAISE (class_loading, (klass));

	// Count non-NULL items in pinfo->constraints
	count = 0;
	if (!is_anonymous)
		for (ptr = pinfo->constraints; ptr && *ptr; ptr++, count++)
			;

	pos = 0;
	if ((count > 0) && !MONO_CLASS_IS_INTERFACE_INTERNAL (pinfo->constraints [0])) {
		CHECKED_METADATA_WRITE_PTR ( klass->parent , pinfo->constraints [0] );
		pos++;
	} else if (pinfo && pinfo->flags & GENERIC_PARAMETER_ATTRIBUTE_VALUE_TYPE_CONSTRAINT) {
		CHECKED_METADATA_WRITE_PTR ( klass->parent , mono_class_load_from_name (mono_defaults.corlib, "System", "ValueType") );
	} else {
		CHECKED_METADATA_WRITE_PTR ( klass->parent , mono_defaults.object_class );
	}

	if (count - pos > 0) {
		klass->interface_count = count - pos;
		CHECKED_METADATA_WRITE_PTR_LOCAL ( klass->interfaces , (MonoClass **)mono_image_alloc0 (image, sizeof (MonoClass *) * (count - pos)) );
		klass->interfaces_inited = TRUE;
		for (i = pos; i < count; i++)
			CHECKED_METADATA_WRITE_PTR ( klass->interfaces [i - pos] , pinfo->constraints [i] );
	}

	CHECKED_METADATA_WRITE_PTR_EXEMPT ( klass->image , image );

	klass->inited = TRUE;
	CHECKED_METADATA_WRITE_PTR_LOCAL ( klass->cast_class ,    klass );
	CHECKED_METADATA_WRITE_PTR_LOCAL ( klass->element_class , klass );

	MonoTypeEnum t = is_mvar ? MONO_TYPE_MVAR : MONO_TYPE_VAR;
	klass->_byval_arg.type = t;
	klass->this_arg.type = t;
	CHECKED_METADATA_WRITE_PTR ( klass->this_arg.data.generic_param ,  param );
	CHECKED_METADATA_WRITE_PTR ( klass->_byval_arg.data.generic_param , param );
	klass->this_arg.byref = TRUE;

	/* We don't use type_token for VAR since only classes can use it (not arrays, pointer, VARs, etc) */
	klass->sizes.generic_param_token = !is_anonymous ? pinfo->token : 0;

	if (param->gshared_constraint) {
		MonoClass *constraint_class = mono_class_from_mono_type_internal (param->gshared_constraint);
		mono_class_init_sizes (constraint_class);
		klass->has_references = m_class_has_references (constraint_class);
	}
	/*
	 * This makes sure the the value size of this class is equal to the size of the types the gparam is
	 * constrained to, the JIT depends on this.
	 */
	klass->instance_size = MONO_ABI_SIZEOF (MonoObject) + mono_type_size (m_class_get_byval_arg (klass), &min_align);
	klass->min_align = min_align;
	mono_memory_barrier ();
	klass->size_inited = 1;

	mono_class_setup_supertypes (klass);

	if (count - pos > 0) {
		mono_class_setup_vtable (klass->parent);
		if (mono_class_has_failure (klass->parent))
			mono_class_set_type_load_failure (klass, "Failed to setup parent interfaces");
		else
			mono_class_setup_interface_offsets_internal (klass, klass->parent->vtable_size, TRUE);
	}

	return klass;
}

/*
 * LOCKING: Acquires the image lock (@image).
 */
MonoClass *
mono_class_create_generic_parameter (MonoGenericParam *param)
{
	MonoImage *image = mono_get_image_for_generic_param (param);
	MonoGenericParamInfo *pinfo = mono_generic_param_info (param);
	MonoClass *klass, *klass2;

	// If a klass already exists for this object and is cached, return it.
	klass = pinfo->pklass;

	if (klass)
		return klass;

	// Create a new klass
	klass = make_generic_param_class (param);

	// Now we need to cache the klass we created.
	// But since we wait to grab the lock until after creating the klass, we need to check to make sure
	// another thread did not get in and cache a klass ahead of us. In that case, return their klass
	// and allow our newly-created klass object to just leak.
	mono_memory_barrier ();

	mono_image_lock (image);

	// Here "klass2" refers to the klass potentially created by the other thread.
	klass2 = pinfo->pklass;

	if (klass2) {
		klass = klass2;
	} else {
		pinfo->pklass = klass;
	}
	mono_image_unlock (image);

	/* FIXME: Should this go inside 'make_generic_param_klass'? */
	if (klass2)
		MONO_PROFILER_RAISE (class_failed, (klass2));
	else
		MONO_PROFILER_RAISE (class_loaded, (klass));

	return klass;
}

/**
 * mono_class_create_ptr:
 */
MonoClass *
mono_class_create_ptr (MonoType *type)
{
	MonoClass *result;
	MonoClass *el_class;
	MonoImage *image;
	char *name;
	MonoMemoryManager *mm;

	el_class = mono_class_from_mono_type_internal (type);
	image = el_class->image;
	// FIXME: Optimize this
	mm = class_kind_may_contain_generic_instances ((MonoTypeKind)el_class->class_kind) ? mono_metadata_get_mem_manager_for_class (el_class) : NULL;

	if (mm) {
		mono_mem_manager_lock (mm);
		if (!mm->ptr_cache)
			mm->ptr_cache = g_hash_table_new (mono_aligned_addr_hash, NULL);
		result = (MonoClass *)g_hash_table_lookup (mm->ptr_cache, el_class);
		mono_mem_manager_unlock (mm);
		if (result)
			return result;
	} else {
		mono_image_lock (image);
		if (image->ptr_cache) {
			if ((result = (MonoClass *)g_hash_table_lookup (image->ptr_cache, el_class))) {
				mono_image_unlock (image);
				return result;
			}
		}
		mono_image_unlock (image);
	}
	
	result = mm ? (MonoClass *)mono_mem_manager_alloc0 (mm, sizeof (MonoClassPointer)) : (MonoClass *)mono_image_alloc0 (image, sizeof (MonoClassPointer));

	UnlockedAdd (&classes_size, sizeof (MonoClassPointer));
	++class_pointer_count;

	result->parent = NULL; /* no parent for PTR types */
	result->name_space = el_class->name_space;
	name = g_strdup_printf ("%s*", el_class->name);
	result->name = mm ? mono_mem_manager_strdup (mm, name) : mono_image_strdup (image, name);
	result->class_kind = MONO_CLASS_POINTER;
	g_free (name);

	MONO_PROFILER_RAISE (class_loading, (result));

	result->image = el_class->image;
	result->inited = TRUE;
	result->instance_size = MONO_ABI_SIZEOF (MonoObject) + MONO_ABI_SIZEOF (gpointer);
	result->min_align = sizeof (gpointer);
	result->element_class = el_class;
	result->blittable = TRUE;

	if (el_class->enumtype)
		result->cast_class = el_class->element_class;
	else
		result->cast_class = el_class;
	class_composite_fixup_cast_class (result, TRUE);

	result->this_arg.type = result->_byval_arg.type = MONO_TYPE_PTR;
	result->this_arg.data.type = result->_byval_arg.data.type = m_class_get_byval_arg (el_class);
	result->this_arg.byref = TRUE;

	mono_class_setup_supertypes (result);

	if (mm) {
		mono_mem_manager_lock (mm);
		MonoClass *result2;
		result2 = (MonoClass *)g_hash_table_lookup (mm->ptr_cache, el_class);
		if (!result2)
			g_hash_table_insert (mm->ptr_cache, el_class, result);
		mono_mem_manager_unlock (mm);
		if (result2) {
			MONO_PROFILER_RAISE (class_failed, (result));
			return result2;
		}
	} else {
		mono_image_lock (image);
		if (image->ptr_cache) {
			MonoClass *result2;
			if ((result2 = (MonoClass *)g_hash_table_lookup (image->ptr_cache, el_class))) {
				mono_image_unlock (image);
				MONO_PROFILER_RAISE (class_failed, (result));
				return result2;
			}
		} else {
			image->ptr_cache = g_hash_table_new (mono_aligned_addr_hash, NULL);
		}
		g_hash_table_insert (image->ptr_cache, el_class, result);
		mono_image_unlock (image);
	}

	MONO_PROFILER_RAISE (class_loaded, (result));

	return result;
}

MonoClass *
mono_class_create_fnptr (MonoMethodSignature *sig)
{
	MonoClass *result, *cached;
	static GHashTable *ptr_hash = NULL;

	/* FIXME: These should be allocate from a mempool as well, but which one ? */

	mono_loader_lock ();
	if (!ptr_hash)
		ptr_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
	cached = (MonoClass *)g_hash_table_lookup (ptr_hash, sig);
	mono_loader_unlock ();
	if (cached)
		return cached;

	result = g_new0 (MonoClass, 1);

	result->parent = NULL; /* no parent for PTR types */
	result->name_space = "System";
	result->name = "MonoFNPtrFakeClass";
	result->class_kind = MONO_CLASS_POINTER;

	result->image = mono_defaults.corlib; /* need to fix... */
	result->instance_size = MONO_ABI_SIZEOF (MonoObject) + MONO_ABI_SIZEOF (gpointer);
	result->min_align = sizeof (gpointer);
	result->cast_class = result->element_class = result;
	result->this_arg.type = result->_byval_arg.type = MONO_TYPE_FNPTR;
	result->this_arg.data.method = result->_byval_arg.data.method = sig;
	result->this_arg.byref = TRUE;
	result->blittable = TRUE;
	result->inited = TRUE;

	mono_class_setup_supertypes (result);

	mono_loader_lock ();

	cached = (MonoClass *)g_hash_table_lookup (ptr_hash, sig);
	if (cached) {
		g_free (result);
		mono_loader_unlock ();
		return cached;
	}

	MONO_PROFILER_RAISE (class_loading, (result));

	UnlockedAdd (&classes_size, sizeof (MonoClassPointer));
	++class_pointer_count;

	g_hash_table_insert (ptr_hash, sig, result);

	mono_loader_unlock ();

	MONO_PROFILER_RAISE (class_loaded, (result));

	return result;
}


static gboolean 
method_is_reabstracted (guint16 flags)
{
	if ((flags & METHOD_ATTRIBUTE_ABSTRACT && flags & METHOD_ATTRIBUTE_FINAL))
		return TRUE;
	return FALSE;
}

/**
 * mono_class_setup_count_virtual_methods:
 *
 * Return the number of virtual methods.
 * Even for interfaces we can't simply return the number of methods as all CLR types are allowed to have static methods.
 * Return -1 on failure.
 * FIXME It would be nice if this information could be cached somewhere.
 */
int
mono_class_setup_count_virtual_methods (MonoClass *klass)
{
	int i, mcount, vcount = 0;
	guint32 flags;
	klass = mono_class_get_generic_type_definition (klass); /*We can find this information by looking at the GTD*/

	if (klass->methods || !MONO_CLASS_HAS_STATIC_METADATA (klass)) {
		mono_class_setup_methods (klass);
		if (mono_class_has_failure (klass))
			return -1;

		mcount = mono_class_get_method_count (klass);
		for (i = 0; i < mcount; ++i) {
			flags = klass->methods [i]->flags;
			if ((flags & METHOD_ATTRIBUTE_VIRTUAL)) {
				if (method_is_reabstracted (flags))
					continue;
				++vcount;
			}
		}
	} else {
		int first_idx = mono_class_get_first_method_idx (klass);
		mcount = mono_class_get_method_count (klass);
		for (i = 0; i < mcount; ++i) {
			flags = mono_metadata_decode_table_row_col (klass->image, MONO_TABLE_METHOD, first_idx + i, MONO_METHOD_FLAGS);

			if ((flags & METHOD_ATTRIBUTE_VIRTUAL)) {
				if (method_is_reabstracted (flags))
					continue;
				++vcount;
			}
		}
	}
	return vcount;
}

#ifdef COMPRESSED_INTERFACE_BITMAP

/*
 * Compressed interface bitmap design.
 *
 * Interface bitmaps take a large amount of memory, because their size is
 * linear with the maximum interface id assigned in the process (each interface
 * is assigned a unique id as it is loaded). The number of interface classes
 * is high because of the many implicit interfaces implemented by arrays (we'll
 * need to lazy-load them in the future).
 * Most classes implement a very small number of interfaces, so the bitmap is
 * sparse. This bitmap needs to be checked by interface casts, so access to the
 * needed bit must be fast and doable with few jit instructions.
 *
 * The current compression format is as follows:
 * *) it is a sequence of one or more two-byte elements
 * *) the first byte in the element is the count of empty bitmap bytes
 * at the current bitmap position
 * *) the second byte in the element is an actual bitmap byte at the current
 * bitmap position
 *
 * As an example, the following compressed bitmap bytes:
 * 	0x07 0x01 0x00 0x7
 * correspond to the following bitmap:
 * 	0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x01 0x07
 *
 * Each two-byte element can represent up to 2048 bitmap bits, but as few as a single
 * bitmap byte for non-sparse sequences. In practice the interface bitmaps created
 * during a gmcs bootstrap are reduced to less tha 5% of the original size.
 */

/**
 * mono_compress_bitmap:
 * \param dest destination buffer
 * \param bitmap bitmap buffer
 * \param size size of \p bitmap in bytes
 *
 * This is a mono internal function.
 * The \p bitmap data is compressed into a format that is small but
 * still searchable in few instructions by the JIT and runtime.
 * The compressed data is stored in the buffer pointed to by the
 * \p dest array. Passing a NULL value for \p dest allows to just compute
 * the size of the buffer.
 * This compression algorithm assumes the bits set in the bitmap are
 * few and far between, like in interface bitmaps.
 * \returns The size of the compressed bitmap in bytes.
 */
int
mono_compress_bitmap (uint8_t *dest, const uint8_t *bitmap, int size)
{
	int numz = 0;
	int res = 0;
	const uint8_t *end = bitmap + size;
	while (bitmap < end) {
		if (*bitmap || numz == 255) {
			if (dest) {
				*dest++ = numz;
				*dest++ = *bitmap;
			}
			res += 2;
			numz = 0;
			bitmap++;
			continue;
		}
		bitmap++;
		numz++;
	}
	if (numz) {
		res += 2;
		if (dest) {
			*dest++ = numz;
			*dest++ = 0;
		}
	}
	return res;
}

/**
 * mono_class_interface_match:
 * \param bitmap a compressed bitmap buffer
 * \param id the index to check in the bitmap
 *
 * This is a mono internal function.
 * Checks if a bit is set in a compressed interface bitmap. \p id must
 * be already checked for being smaller than the maximum id encoded in the
 * bitmap.
 *
 * \returns A non-zero value if bit \p id is set in the bitmap \p bitmap,
 * FALSE otherwise.
 */
int
mono_class_interface_match (const uint8_t *bitmap, int id)
{
	while (TRUE) {
		id -= bitmap [0] * 8;
		if (id < 8) {
			if (id < 0)
				return 0;
			return bitmap [1] & (1 << id);
		}
		bitmap += 2;
		id -= 8;
	}
}
#endif

static char*
concat_two_strings_with_zero (MonoImage *image, const char *s1, const char *s2)
{
	int null_length = strlen ("(null)");
	int len = (s1 ? strlen (s1) : null_length) + (s2 ? strlen (s2) : null_length) + 2;
	char *s = (char *)mono_image_alloc (image, len);
	int result;

	result = g_snprintf (s, len, "%s%c%s", s1 ? s1 : "(null)", '\0', s2 ? s2 : "(null)");
	g_assert (result == len - 1);

	return s;
}


static void
init_sizes_with_info (MonoClass *klass, MonoCachedClassInfo *cached_info)
{
	if (cached_info) {
		mono_loader_lock ();
		klass->instance_size = cached_info->instance_size;
		klass->sizes.class_size = cached_info->class_size;
		klass->packing_size = cached_info->packing_size;
		klass->min_align = cached_info->min_align;
		klass->blittable = cached_info->blittable;
		klass->has_references = cached_info->has_references;
		klass->has_static_refs = cached_info->has_static_refs;
		klass->no_special_static_fields = cached_info->no_special_static_fields;
		klass->has_weak_fields = cached_info->has_weak_fields;
		mono_loader_unlock ();
	}
	else {
		if (!klass->size_inited)
			mono_class_setup_fields (klass);
	}
}
/*

 * mono_class_init_sizes:
 *
 *   Initializes the size related fields of @klass without loading all field data if possible.
 * Sets the following fields in @klass:
 * - instance_size
 * - sizes.class_size
 * - packing_size
 * - min_align
 * - blittable
 * - has_references
 * - has_static_refs
 * - size_inited
 * Can fail the class.
 *
 * LOCKING: Acquires the loader lock.
 */
void
mono_class_init_sizes (MonoClass *klass)
{
	MonoCachedClassInfo cached_info;
	gboolean has_cached_info;

	if (klass->size_inited)
		return;

	has_cached_info = mono_class_get_cached_class_info (klass, &cached_info);

	init_sizes_with_info (klass, has_cached_info ? &cached_info : NULL);
}


static gboolean
class_has_references (MonoClass *klass)
{
	mono_class_init_sizes (klass);

	/*
	 * has_references is not set if this is called recursively, but this is not a problem since this is only used
	 * during field layout, and instance fields are initialized before static fields, and instance fields can't
	 * embed themselves.
	 */
	return klass->has_references;
}

static gboolean
type_has_references (MonoClass *klass, MonoType *ftype)
{
	if (MONO_TYPE_IS_REFERENCE (ftype) || IS_GC_REFERENCE (klass, ftype) || ((MONO_TYPE_ISSTRUCT (ftype) && class_has_references (mono_class_from_mono_type_internal (ftype)))))
		return TRUE;
	if (!ftype->byref && (ftype->type == MONO_TYPE_VAR || ftype->type == MONO_TYPE_MVAR)) {
		MonoGenericParam *gparam = ftype->data.generic_param;

		if (gparam->gshared_constraint)
			return class_has_references (mono_class_from_mono_type_internal (gparam->gshared_constraint));
	}
	return FALSE;
}

/**
 * mono_class_is_gparam_with_nonblittable_parent:
 * \param klass  a generic parameter
 *
 * \returns TRUE if \p klass is definitely not blittable.
 *
 * A parameter is definitely not blittable if it has the IL 'reference'
 * constraint, or if it has a class specified as a parent.  If it has an IL
 * 'valuetype' constraint or no constraint at all or only interfaces as
 * constraints, we return FALSE because the parameter may be instantiated both
 * with blittable and non-blittable types.
 *
 * If the paramter is a generic sharing parameter, we look at its gshared_constraint->blittable bit.
 */
static gboolean
mono_class_is_gparam_with_nonblittable_parent (MonoClass *klass)
{
	MonoType *type = m_class_get_byval_arg (klass);
	g_assert (mono_type_is_generic_parameter (type));
	MonoGenericParam *gparam = type->data.generic_param;
	if ((mono_generic_param_info (gparam)->flags & GENERIC_PARAMETER_ATTRIBUTE_REFERENCE_TYPE_CONSTRAINT) != 0)
		return TRUE;
	if ((mono_generic_param_info (gparam)->flags & GENERIC_PARAMETER_ATTRIBUTE_VALUE_TYPE_CONSTRAINT) != 0)
		return FALSE;

	if (gparam->gshared_constraint) {
		MonoClass *constraint_class = mono_class_from_mono_type_internal (gparam->gshared_constraint);
		return !m_class_is_blittable (constraint_class);
	}

	if (mono_generic_param_owner (gparam)->is_anonymous)
		return FALSE;

	/* We could have:  T : U,  U : Base.  So have to follow the constraints. */
	MonoClass *parent_class = mono_generic_param_get_base_type (klass);
	g_assert (!MONO_CLASS_IS_INTERFACE_INTERNAL (parent_class));
	/* Parent can only be: System.Object, System.ValueType or some specific base class.
	 *
	 * If the parent_class is ValueType, the valuetype constraint would be set, above, so
	 * we wouldn't get here.
	 *
	 * If there was a reference constraint, the parent_class would be System.Object,
	 * but we would have returned early above.
	 *
	 * So if we get here, there is either no base class constraint at all,
	 * in which case parent_class would be set to System.Object, or there is none at all.
	 */
	return parent_class != mono_defaults.object_class;
}

/*
 * mono_class_layout_fields:
 * @class: a class
 * @base_instance_size: base instance size
 * @packing_size:
 *
 * This contains the common code for computing the layout of classes and sizes.
 * This should only be called from mono_class_setup_fields () and
 * typebuilder_setup_fields ().
 *
 * LOCKING: Acquires the loader lock
 */
void
mono_class_layout_fields (MonoClass *klass, int base_instance_size, int packing_size, int explicit_size, gboolean sre)
{
	int i;
	const int top = mono_class_get_field_count (klass);
	guint32 layout = mono_class_get_flags (klass) & TYPE_ATTRIBUTE_LAYOUT_MASK;
	guint32 pass, passes, real_size;
	gboolean gc_aware_layout = FALSE;
	gboolean has_static_fields = FALSE;
	gboolean has_references = FALSE;
	gboolean has_static_refs = FALSE;
	MonoClassField *field;
	gboolean blittable;
	int instance_size = base_instance_size;
	int element_size = -1;
	int class_size, min_align;
	int *field_offsets;
	gboolean *fields_has_references;

	/*
	 * We want to avoid doing complicated work inside locks, so we compute all the required
	 * information and write it to @klass inside a lock.
	 */
	if (klass->fields_inited)
		return;

	if ((packing_size & 0xffffff00) != 0) {
		mono_class_set_type_load_failure (klass, "Could not load struct '%s' with packing size %d >= 256", klass->name, packing_size);
		return;
	}

	if (klass->parent) {
		min_align = klass->parent->min_align;
		/* we use | since it may have been set already */
		has_references = klass->has_references | klass->parent->has_references;
	} else {
		min_align = 1;
	}
	/* We can't really enable 16 bytes alignment until the GC supports it.
	The whole layout/instance size code must be reviewed because we do alignment calculation in terms of the
	boxed instance, which leads to unexplainable holes at the beginning of an object embedding a simd type.
	Bug #506144 is an example of this issue.

	 if (klass->simd_type)
		min_align = 16;
	 */

	/*
	 * When we do generic sharing we need to have layout
	 * information for open generic classes (either with a generic
	 * context containing type variables or with a generic
	 * container), so we don't return in that case anymore.
	 */

	if (klass->enumtype) {
		for (i = 0; i < top; i++) {
			field = &klass->fields [i];
			if (!(field->type->attrs & FIELD_ATTRIBUTE_STATIC)) {
				klass->cast_class = klass->element_class = mono_class_from_mono_type_internal (field->type);
				break;
			}
		}

		if (!mono_class_enum_basetype_internal (klass)) {
			mono_class_set_type_load_failure (klass, "The enumeration's base type is invalid.");
			return;
		}
	}

	/*
	 * Enable GC aware auto layout: in this mode, reference
	 * fields are grouped together inside objects, increasing collector 
	 * performance.
	 * Requires that all classes whose layout is known to native code be annotated
	 * with [StructLayout (LayoutKind.Sequential)]
	 * Value types have gc_aware_layout disabled by default, as per
	 * what the default is for other runtimes.
	 */
	 /* corlib is missing [StructLayout] directives in many places */
	if (layout == TYPE_ATTRIBUTE_AUTO_LAYOUT) {
		if (!klass->valuetype)
			gc_aware_layout = TRUE;
	}

	/* Compute klass->blittable */
	blittable = TRUE;
	if (klass->parent)
		blittable = klass->parent->blittable;
	if (layout == TYPE_ATTRIBUTE_AUTO_LAYOUT && !(mono_is_corlib_image (klass->image) && !strcmp (klass->name_space, "System") && !strcmp (klass->name, "ValueType")) && top)
		blittable = FALSE;
	for (i = 0; i < top; i++) {
		field = &klass->fields [i];

		if (mono_field_is_deleted (field))
			continue;
		if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;
		if (blittable) {
			if (field->type->byref || MONO_TYPE_IS_REFERENCE (field->type)) {
				blittable = FALSE;
			} else if (mono_type_is_generic_parameter (field->type) &&
				   mono_class_is_gparam_with_nonblittable_parent (mono_class_from_mono_type_internal (field->type))) {
				blittable = FALSE;
			} else {
				MonoClass *field_class = mono_class_from_mono_type_internal (field->type);
				if (field_class) {
					mono_class_setup_fields (field_class);
					if (mono_class_has_failure (field_class)) {
						ERROR_DECL (field_error);
						mono_error_set_for_class_failure (field_error, field_class);
						mono_class_set_type_load_failure (klass, "Could not set up field '%s' due to: %s", field->name, mono_error_get_message (field_error));
						mono_error_cleanup (field_error);
						break;
					}
				}
				if (!field_class || !field_class->blittable)
					blittable = FALSE;
			}
		}
		if (klass->enumtype)
			blittable = klass->element_class->blittable;
	}
	if (mono_class_has_failure (klass))
		return;
	if (klass == mono_defaults.string_class)
		blittable = FALSE;

	/* Compute klass->has_references */
	/* 
	 * Process non-static fields first, since static fields might recursively
	 * refer to the class itself.
	 */
	for (i = 0; i < top; i++) {
		MonoType *ftype;

		field = &klass->fields [i];

		if (!(field->type->attrs & FIELD_ATTRIBUTE_STATIC)) {
			ftype = mono_type_get_underlying_type (field->type);
			ftype = mono_type_get_basic_type_from_generic (ftype);
			if (type_has_references (klass, ftype))
				has_references = TRUE;
		}
	}

	/*
	 * Compute field layout and total size (not considering static fields)
	 */
	field_offsets = g_new0 (int, top);
	fields_has_references = g_new0 (gboolean, top);
	int first_field_idx = mono_class_has_static_metadata (klass) ? mono_class_get_first_field_idx (klass) : 0;
	switch (layout) {
	case TYPE_ATTRIBUTE_AUTO_LAYOUT:
	case TYPE_ATTRIBUTE_SEQUENTIAL_LAYOUT:
		if (gc_aware_layout)
			passes = 2;
		else
			passes = 1;

		if (layout != TYPE_ATTRIBUTE_AUTO_LAYOUT)
			passes = 1;

		if (klass->parent) {
			mono_class_setup_fields (klass->parent);
			if (mono_class_set_type_load_failure_causedby_class (klass, klass->parent, "Cannot initialize parent class"))
				return;
			real_size = klass->parent->instance_size;
		} else {
			real_size = MONO_ABI_SIZEOF (MonoObject);
		}

		for (pass = 0; pass < passes; ++pass) {
			for (i = 0; i < top; i++){
				gint32 align;
				guint32 size;
				MonoType *ftype;

				field = &klass->fields [i];

				if (mono_field_is_deleted (field))
					continue;
				if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
					continue;

				ftype = mono_type_get_underlying_type (field->type);
				ftype = mono_type_get_basic_type_from_generic (ftype);
				if (gc_aware_layout) {
					fields_has_references [i] = type_has_references (klass, ftype);
					if (fields_has_references [i]) {
						if (pass == 1)
							continue;
					} else {
						if (pass == 0)
							continue;
					}
				}

				if ((top == 1) && (instance_size == MONO_ABI_SIZEOF (MonoObject)) &&
					(strcmp (mono_field_get_name (field), "$PRIVATE$") == 0)) {
					/* This field is a hack inserted by MCS to empty structures */
					continue;
				}

				size = mono_type_size (field->type, &align);
			
				/* FIXME (LAMESPEC): should we also change the min alignment according to pack? */
				align = packing_size ? MIN (packing_size, align): align;
				/* if the field has managed references, we need to force-align it
				 * see bug #77788
				 */
				if (type_has_references (klass, ftype))
					align = MAX (align, TARGET_SIZEOF_VOID_P);

				min_align = MAX (align, min_align);
				field_offsets [i] = real_size;
				if (align) {
					field_offsets [i] += align - 1;
					field_offsets [i] &= ~(align - 1);
				}
				/*TypeBuilders produce all sort of weird things*/
				g_assert (image_is_dynamic (klass->image) || field_offsets [i] > 0);
				real_size = field_offsets [i] + size;
			}

			instance_size = MAX (real_size, instance_size);
       
			if (instance_size & (min_align - 1)) {
				instance_size += min_align - 1;
				instance_size &= ~(min_align - 1);
			}
		}
		break;
	case TYPE_ATTRIBUTE_EXPLICIT_LAYOUT: {
		real_size = 0;
		for (i = 0; i < top; i++) {
			gint32 align;
			guint32 size;
			MonoType *ftype;

			field = &klass->fields [i];

			/*
			 * There must be info about all the fields in a type if it
			 * uses explicit layout.
			 */
			if (mono_field_is_deleted (field))
				continue;
			if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
				continue;

			size = mono_type_size (field->type, &align);
			align = packing_size ? MIN (packing_size, align): align;
			min_align = MAX (align, min_align);

			if (sre) {
				/* Already set by typebuilder_setup_fields () */
				field_offsets [i] = field->offset + MONO_ABI_SIZEOF (MonoObject);
			} else {
				int idx = first_field_idx + i;
				guint32 offset;
				mono_metadata_field_info (klass->image, idx, &offset, NULL, NULL);
				field_offsets [i] = offset + MONO_ABI_SIZEOF (MonoObject);
			}
			ftype = mono_type_get_underlying_type (field->type);
			ftype = mono_type_get_basic_type_from_generic (ftype);
			if (type_has_references (klass, ftype)) {
				if (field_offsets [i] % TARGET_SIZEOF_VOID_P) {
					mono_class_set_type_load_failure (klass, "Reference typed field '%s' has explicit offset that is not pointer-size aligned.", field->name);
				}
			}

			/*
			 * Calc max size.
			 */
			real_size = MAX (real_size, size + field_offsets [i]);
		}

		/* check for incorrectly aligned or overlapped by a non-object field */
		guint8 *layout_check;	
		if (has_references) {
			layout_check = g_new0 (guint8, real_size);
			for (i = 0; i < top && !mono_class_has_failure (klass); i++) {
				field = &klass->fields [i];
				if (!field)
					continue;
				if (mono_field_is_deleted (field))
					continue;
				if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
					continue;
				int align = 0;
				int size = mono_type_size (field->type, &align);
				MonoType *ftype = mono_type_get_underlying_type (field->type);
				ftype = mono_type_get_basic_type_from_generic (ftype);
				guint8 type =  type_has_references (klass, ftype) ? 1 : 2;				
				for (int j = 0; j < size; j++) {
					if (layout_check [field_offsets [i] + j] != 0 && layout_check [field_offsets [i] + j] != type) {
						mono_class_set_type_load_failure (klass, "Could not load type '%s' because it contains an object field at offset %d that is incorrectly aligned or overlapped by a non-object field.", klass->name, field->offset);
						break;
					}
					layout_check [field_offsets [i] + j] = type;
				}
			}
			g_free (layout_check);
		}

		instance_size = MAX (real_size, instance_size);
		if (!((layout == TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) && explicit_size)) {
			if (instance_size & (min_align - 1)) {
				instance_size += min_align - 1;
				instance_size &= ~(min_align - 1);
			}
		}
		break;
	}
	}

	if (layout != TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) {
		/*
		 * This leads to all kinds of problems with nested structs, so only
		 * enable it when a MONO_DEBUG property is set.
		 *
		 * For small structs, set min_align to at least the struct size to improve
		 * performance, and since the JIT memset/memcpy code assumes this and generates 
		 * unaligned accesses otherwise. See #78990 for a testcase.
		 */
		if (mono_align_small_structs && top) {
			if (instance_size <= MONO_ABI_SIZEOF (MonoObject) + MONO_ABI_SIZEOF (gpointer))
				min_align = MAX (min_align, instance_size - MONO_ABI_SIZEOF (MonoObject));
		}
	}

	MonoType *klass_byval_arg = m_class_get_byval_arg (klass);
	if (klass_byval_arg->type == MONO_TYPE_VAR || klass_byval_arg->type == MONO_TYPE_MVAR)
		instance_size = MONO_ABI_SIZEOF (MonoObject) + mono_type_size (klass_byval_arg, &min_align);
	else if (klass_byval_arg->type == MONO_TYPE_PTR)
		instance_size = MONO_ABI_SIZEOF (MonoObject) + MONO_ABI_SIZEOF (gpointer);

	if (klass_byval_arg->type == MONO_TYPE_SZARRAY || klass_byval_arg->type == MONO_TYPE_ARRAY)
		element_size = mono_class_array_element_size (klass->element_class);

	/* Publish the data */
	mono_loader_lock ();
	if (klass->instance_size && !klass->image->dynamic && klass_byval_arg->type != MONO_TYPE_FNPTR) {
		/* Might be already set using cached info */
		if (klass->instance_size != instance_size) {
			/* Emit info to help debugging */
			g_print ("%s\n", mono_class_full_name (klass));
			g_print ("%d %d %d %d\n", klass->instance_size, instance_size, klass->blittable, blittable);
			g_print ("%d %d %d %d\n", klass->has_references, has_references, klass->packing_size, packing_size);
			g_print ("%d %d\n", klass->min_align, min_align);
			for (i = 0; i < top; ++i) {
				field = &klass->fields [i];
				if (!(field->type->attrs & FIELD_ATTRIBUTE_STATIC))
					printf ("  %s %d %d %d\n", klass->fields [i].name, klass->fields [i].offset, field_offsets [i], fields_has_references [i]);
			}
		}
		g_assert (klass->instance_size == instance_size);
	} else {
		klass->instance_size = instance_size;
	}
	klass->blittable = blittable;
	klass->has_references = has_references;
	klass->packing_size = packing_size;
	klass->min_align = min_align;
	for (i = 0; i < top; ++i) {
		field = &klass->fields [i];
		if (!(field->type->attrs & FIELD_ATTRIBUTE_STATIC))
			klass->fields [i].offset = field_offsets [i];
	}

	if (klass_byval_arg->type == MONO_TYPE_SZARRAY || klass_byval_arg->type == MONO_TYPE_ARRAY)
		klass->sizes.element_size = element_size;

	mono_memory_barrier ();
	klass->size_inited = 1;
	mono_loader_unlock ();

	/*
	 * Compute static field layout and size
	 * Static fields can reference the class itself, so this has to be
	 * done after instance_size etc. are initialized.
	 */
	class_size = 0;
	for (i = 0; i < top; i++) {
		gint32 align;
		guint32 size;

		field = &klass->fields [i];
			
		if (!(field->type->attrs & FIELD_ATTRIBUTE_STATIC) || field->type->attrs & FIELD_ATTRIBUTE_LITERAL)
			continue;
		if (mono_field_is_deleted (field))
			continue;

		/* Type may not be initialized yet. Don't initialize it. If
		   it's a reference type we can get the size without
		   recursing */
		if (mono_type_has_exceptions (field->type)) {
			mono_class_set_type_load_failure (klass, "Field '%s' has an invalid type.", field->name);
			break;
		}

		has_static_fields = TRUE;

		size = mono_type_size (field->type, &align);
		/* Check again in case initializing the field's type caused a failure */
		if (mono_type_has_exceptions (field->type)) {
			mono_class_set_type_load_failure (klass, "Field '%s' has an invalid type.", field->name);
			break;
		}

		field_offsets [i] = class_size;
		/*align is always non-zero here*/
		field_offsets [i] += align - 1;
		field_offsets [i] &= ~(align - 1);
		class_size = field_offsets [i] + size;
	}

	if (has_static_fields && class_size == 0)
		/* Simplify code which depends on class_size != 0 if the class has static fields */
		class_size = 8;

	/* Compute klass->has_static_refs */
	has_static_refs = FALSE;
	for (i = 0; i < top; i++) {
		MonoType *ftype;

		field = &klass->fields [i];

		if (field->type->attrs & FIELD_ATTRIBUTE_STATIC) {
			ftype = mono_type_get_underlying_type (field->type);
			ftype = mono_type_get_basic_type_from_generic (ftype);
			if (type_has_references (klass, ftype))
				has_static_refs = TRUE;
		}
	}

	/*valuetypes can't be neither bigger than 1Mb or empty. */
	if (klass->valuetype && (klass->instance_size <= 0 || klass->instance_size > (0x100000 + MONO_ABI_SIZEOF (MonoObject)))) {
		/* Special case compiler generated types */
		/* Hard to check for [CompilerGenerated] here */
		if (!strstr (klass->name, "StaticArrayInitTypeSize") && !strstr (klass->name, "$ArrayType"))
			mono_class_set_type_load_failure (klass, "Value type instance size (%d) cannot be zero, negative, or bigger than 1Mb", klass->instance_size);
	}

	// Weak field support
	//
	// FIXME:
	// - generic instances
	// - Disallow on structs/static fields/nonref fields
	gboolean has_weak_fields = FALSE;

	if (mono_class_has_static_metadata (klass)) {
		for (MonoClass *p = klass; p != NULL; p = p->parent) {
			gpointer iter = NULL;
			guint32 first_field_idx = mono_class_get_first_field_idx (p);

			while ((field = mono_class_get_fields_internal (p, &iter))) {
				guint32 field_idx = first_field_idx + (field - p->fields);
				if (MONO_TYPE_IS_REFERENCE (field->type) && mono_assembly_is_weak_field (p->image, field_idx + 1)) {
					has_weak_fields = TRUE;
					mono_trace_message (MONO_TRACE_TYPE, "Field %s:%s at offset %x is weak.", field->parent->name, field->name, field->offset);
				}
			}
		}
	}

	/*
	 * Check that any fields of IsByRefLike type are instance
	 * fields and only inside other IsByRefLike structs.
	 *
	 * (Has to be done late because we call
	 * mono_class_from_mono_type_internal which may recursively
	 * refer to the current class)
	 */
	gboolean allow_isbyreflike_fields = m_class_is_byreflike (klass);
	for (i = 0; i < top; i++) {
		field = &klass->fields [i];

		if (mono_field_is_deleted (field))
			continue;
		if ((field->type->attrs & FIELD_ATTRIBUTE_LITERAL))
			continue;
		MonoClass *field_class = NULL;
		/* have to be careful not to recursively invoke mono_class_init on a static field.
		 * for example - if the field is an array of a subclass of klass, we can loop.
		 */
		switch (field->type->type) {
		case MONO_TYPE_TYPEDBYREF:
		case MONO_TYPE_VALUETYPE:
		case MONO_TYPE_GENERICINST:
			field_class = mono_class_from_mono_type_internal (field->type);
			break;
		default:
			break;
		}
		if (!field_class || !m_class_is_byreflike (field_class))
			continue;
		if ((field->type->attrs & FIELD_ATTRIBUTE_STATIC)) {
			mono_class_set_type_load_failure (klass, "Static ByRefLike field '%s' is not allowed", field->name);
			return;
		} else {
			/* instance field */
			if (allow_isbyreflike_fields)
				continue;
			mono_class_set_type_load_failure (klass, "Instance ByRefLike field '%s' not in a ref struct", field->name);
			return;
		}
	}

	/* Publish the data */
	mono_loader_lock ();
	if (!klass->rank)
		klass->sizes.class_size = class_size;
	klass->has_static_refs = has_static_refs;
	klass->has_weak_fields = has_weak_fields;
	for (i = 0; i < top; ++i) {
		field = &klass->fields [i];

		if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
			field->offset = field_offsets [i];
	}
	mono_memory_barrier ();
	klass->fields_inited = 1;
	mono_loader_unlock ();

	g_free (field_offsets);
	g_free (fields_has_references);
}

static int finalize_slot = -1;

static void
initialize_object_slots (MonoClass *klass)
{
	int i;

	if (klass != mono_defaults.object_class || finalize_slot >= 0)
		return;

	mono_class_setup_vtable (klass);

	for (i = 0; i < klass->vtable_size; ++i) {
		if (!strcmp (klass->vtable [i]->name, "Finalize")) {
			int const j = finalize_slot;
			g_assert (j == -1 || j == i);
			finalize_slot = i;
		}
	}

	g_assert (finalize_slot >= 0);
}

int
mono_class_get_object_finalize_slot ()
{
	return finalize_slot;
}

MonoMethod *
mono_class_get_default_finalize_method ()
{
	int const i = finalize_slot;
	return (i < 0) ? NULL : mono_defaults.object_class->vtable [i];
}

typedef struct {
	MonoMethod *array_method;
	char *name;
} GenericArrayMethodInfo;

static int generic_array_method_num = 0;
static GenericArrayMethodInfo *generic_array_method_info = NULL;

static void
setup_generic_array_ifaces (MonoClass *klass, MonoClass *iface, MonoMethod **methods, int pos, GHashTable *cache)
{
	MonoGenericContext tmp_context;
	MonoGenericClass *gclass;
	int i;

	// The interface can sometimes be a GTD in cases like IList
	// See: https://github.com/mono/mono/issues/7095#issuecomment-470465597
	if (mono_class_is_gtd (iface)) {
		MonoType *ty = mono_class_gtd_get_canonical_inst (iface);
		g_assert (ty->type == MONO_TYPE_GENERICINST);
		gclass = ty->data.generic_class;
	} else
		gclass = mono_class_get_generic_class (iface);

	tmp_context.class_inst = NULL;
	tmp_context.method_inst = gclass->context.class_inst;
	//g_print ("setting up array interface: %s\n", mono_type_get_name_full (m_class_get_byval_arg (iface), 0));

	for (i = 0; i < generic_array_method_num; i++) {
		ERROR_DECL (error);
		MonoMethod *m = generic_array_method_info [i].array_method;
		MonoMethod *inflated, *helper;

		inflated = mono_class_inflate_generic_method_checked (m, &tmp_context, error);
		mono_error_assert_ok (error);
		helper = (MonoMethod*)g_hash_table_lookup (cache, inflated);
		if (!helper) {
			helper = mono_marshal_get_generic_array_helper (klass, generic_array_method_info [i].name, inflated);
			g_hash_table_insert (cache, inflated, helper);
		}
		methods [pos ++] = helper;
	}
}

static gboolean
check_method_exists (MonoClass *iface, const char *method_name)
{
	g_assert (iface != NULL);
	ERROR_DECL (method_lookup_error);
	gboolean found = NULL != mono_class_get_method_from_name_checked (iface, method_name, -1, 0, method_lookup_error);
	mono_error_cleanup (method_lookup_error);
	return found;
}

static int
generic_array_methods (MonoClass *klass)
{
	int i, count_generic = 0, mcount;
	GList *list = NULL, *tmp;
	if (generic_array_method_num)
		return generic_array_method_num;
	mono_class_setup_methods (klass->parent); /*This is setting up System.Array*/
	g_assert (!mono_class_has_failure (klass->parent)); /*So hitting this assert is a huge problem*/
	mcount = mono_class_get_method_count (klass->parent);
	for (i = 0; i < mcount; i++) {
		MonoMethod *m = klass->parent->methods [i];
		if (!strncmp (m->name, "InternalArray__", 15)) {
			count_generic++;
			list = g_list_prepend (list, m);
		}
	}
	list = g_list_reverse (list);
	generic_array_method_info = (GenericArrayMethodInfo *)mono_image_alloc (mono_defaults.corlib, sizeof (GenericArrayMethodInfo) * count_generic);
	i = 0;
	for (tmp = list; tmp; tmp = tmp->next) {
		const char *mname, *iname;
		gchar *name;
		MonoMethod *m = (MonoMethod *)tmp->data;
		const char *ireadonlylist_prefix = "InternalArray__IReadOnlyList_";
		const char *ireadonlycollection_prefix = "InternalArray__IReadOnlyCollection_";

		MonoClass *iface = NULL;

		if (!strncmp (m->name, "InternalArray__ICollection_", 27)) {
			iname = "System.Collections.Generic.ICollection`1.";
			mname = m->name + 27;
			iface = mono_class_try_get_icollection_class ();
		} else if (!strncmp (m->name, "InternalArray__IEnumerable_", 27)) {
			iname = "System.Collections.Generic.IEnumerable`1.";
			mname = m->name + 27;
			iface = mono_class_try_get_ienumerable_class ();
		} else if (!strncmp (m->name, ireadonlylist_prefix, strlen (ireadonlylist_prefix))) {
			iname = "System.Collections.Generic.IReadOnlyList`1.";
			mname = m->name + strlen (ireadonlylist_prefix);
			iface = mono_defaults.generic_ireadonlylist_class;
		} else if (!strncmp (m->name, ireadonlycollection_prefix, strlen (ireadonlycollection_prefix))) {
			iname = "System.Collections.Generic.IReadOnlyCollection`1.";
			mname = m->name + strlen (ireadonlycollection_prefix);
			iface = mono_class_try_get_ireadonlycollection_class ();
		} else if (!strncmp (m->name, "InternalArray__", 15)) {
			iname = "System.Collections.Generic.IList`1.";
			mname = m->name + 15;
			iface = mono_defaults.generic_ilist_class;
		} else {
			g_assert_not_reached ();
		}

		if (!iface || !check_method_exists (iface, mname))
			continue;
		
		generic_array_method_info [i].array_method = m;

		name = (gchar *)mono_image_alloc (mono_defaults.corlib, strlen (iname) + strlen (mname) + 1);
		strcpy (name, iname);
		strcpy (name + strlen (iname), mname);
		generic_array_method_info [i].name = name;
		i++;
	}
	/*g_print ("array generic methods: %d\n", count_generic);*/

	/* only count the methods we actually added, not the ones that we
	 * skipped if they implement an interface method that was trimmed.
	 */
	generic_array_method_num = i;
	g_list_free (list);
	return generic_array_method_num;
}

static int array_get_method_count (MonoClass *klass)
{
	MonoType *klass_byval_arg = m_class_get_byval_arg (klass);
	if (klass_byval_arg->type == MONO_TYPE_ARRAY)
		/* Regular array */
		/* ctor([int32]*rank) */
		/* ctor([int32]*rank*2) */
		/* Get */
		/* Set */
		/* Address */
		return 5;
	else if (klass_byval_arg->type == MONO_TYPE_SZARRAY && klass->rank == 1 && klass->element_class->rank)
		/* Jagged arrays are typed as MONO_TYPE_SZARRAY but have an extra ctor in .net which creates an array of arrays */
		/* ctor([int32]) */
		/* ctor([int32], [int32]) */
		/* Get */
		/* Set */
		/* Address */
		return 5;
	else
		/* Vectors don't have additional constructor since a zero lower bound is assumed */
		/* ctor([int32]*rank) */
		/* Get */
		/* Set */
		/* Address */
		return 4;
}

static gboolean array_supports_additional_ctor_method (MonoClass *klass)
{
	MonoType *klass_byval_arg = m_class_get_byval_arg (klass);
	if (klass_byval_arg->type == MONO_TYPE_ARRAY)
		/* Regular array */
		return TRUE;
	else if (klass_byval_arg->type == MONO_TYPE_SZARRAY && klass->rank == 1 && klass->element_class->rank)
		/* Jagged array */
		return TRUE;
	else
		/* Vector */
		return FALSE;
}

/*
 * Global pool of interface IDs, represented as a bitset.
 * LOCKING: Protected by the classes lock.
 */
static MonoBitSet *global_interface_bitset = NULL;

/*
 * mono_unload_interface_ids:
 * @bitset: bit set of interface IDs
 *
 * When an image is unloaded, the interface IDs associated with
 * the image are put back in the global pool of IDs so the numbers
 * can be reused.
 */
void
mono_unload_interface_ids (MonoBitSet *bitset)
{
	classes_lock ();
	mono_bitset_sub (global_interface_bitset, bitset);
	classes_unlock ();
}

void
mono_unload_interface_id (MonoClass *klass)
{
	if (global_interface_bitset && klass->interface_id) {
		classes_lock ();
		mono_bitset_clear (global_interface_bitset, klass->interface_id);
		classes_unlock ();
	}
}

/**
 * mono_get_unique_iid:
 * \param klass interface
 *
 * Assign a unique integer ID to the interface represented by \p klass.
 * The ID will positive and as small as possible.
 * LOCKING: Acquires the classes lock.
 * \returns The new ID.
 */
static guint32
mono_get_unique_iid (MonoClass *klass)
{
	int iid;
	
	g_assert (MONO_CLASS_IS_INTERFACE_INTERNAL (klass));

	classes_lock ();

	if (!global_interface_bitset) {
		global_interface_bitset = mono_bitset_new (128, 0);
		mono_bitset_set (global_interface_bitset, 0); //don't let 0 be a valid iid
	}

	iid = mono_bitset_find_first_unset (global_interface_bitset, -1);
	if (iid < 0) {
		int old_size = mono_bitset_size (global_interface_bitset);
		MonoBitSet *new_set = mono_bitset_clone (global_interface_bitset, old_size * 2);
		mono_bitset_free (global_interface_bitset);
		global_interface_bitset = new_set;
		iid = old_size;
	}
	mono_bitset_set (global_interface_bitset, iid);
	/* set the bit also in the per-image set */
	if (!mono_class_is_ginst (klass)) {
		if (klass->image->interface_bitset) {
			if (iid >= mono_bitset_size (klass->image->interface_bitset)) {
				MonoBitSet *new_set = mono_bitset_clone (klass->image->interface_bitset, iid + 1);
				mono_bitset_free (klass->image->interface_bitset);
				klass->image->interface_bitset = new_set;
			}
		} else {
			klass->image->interface_bitset = mono_bitset_new (iid + 1, 0);
		}
		mono_bitset_set (klass->image->interface_bitset, iid);
	}

	classes_unlock ();

#ifndef MONO_SMALL_CONFIG
	if (mono_print_vtable) {
		int generic_id;
		char *type_name = mono_type_full_name (m_class_get_byval_arg (klass));
		MonoGenericClass *gklass = mono_class_try_get_generic_class (klass);
		if (gklass && !gklass->context.class_inst->is_open) {
			generic_id = gklass->context.class_inst->id;
			g_assert (generic_id != 0);
		} else {
			generic_id = 0;
		}
		printf ("Interface: assigned id %d to %s|%s|%d\n", iid, klass->image->assembly_name, type_name, generic_id);
		g_free (type_name);
	}
#endif

	/* I've confirmed iids safe past 16 bits, however bitset code uses a signed int while testing.
	 * Once this changes, it should be safe for us to allow 2^32-1 interfaces, until then 2^31-2 is the max. */
	g_assert (iid < INT_MAX);
	return iid;
}

/**
 * mono_class_init_internal:
 * \param klass the class to initialize
 *
 * Compute the \c instance_size, \c class_size and other infos that cannot be 
 * computed at \c mono_class_get time. Also compute vtable_size if possible. 
 * Initializes the following fields in \p klass:
 * - all the fields initialized by \c mono_class_init_sizes
 * - has_cctor
 * - ghcimpl
 * - inited
 *
 * LOCKING: Acquires the loader lock.
 *
 * \returns TRUE on success or FALSE if there was a problem in loading
 * the type (incorrect assemblies, missing assemblies, methods, etc).
 */
gboolean
mono_class_init_internal (MonoClass *klass)
{
	int i, vtable_size = 0, array_method_count = 0;
	MonoCachedClassInfo cached_info;
	gboolean has_cached_info;
	gboolean locked = FALSE;
	gboolean ghcimpl = FALSE;
	gboolean has_cctor = FALSE;
	int first_iface_slot = 0;
	
	g_assert (klass);

	/* Double-checking locking pattern */
	if (klass->inited || mono_class_has_failure (klass))
		return !mono_class_has_failure (klass);

	/*g_print ("Init class %s\n", mono_type_get_full_name (klass));*/

	/*
	 * This function can recursively call itself.
	 */
	GSList *init_list = (GSList *)mono_native_tls_get_value (init_pending_tls_id);
	if (g_slist_find (init_list, klass)) {
		mono_class_set_type_load_failure (klass, "Recursive type definition detected %s.%s", klass->name_space, klass->name);
		goto leave_no_init_pending;
	}
	init_list = g_slist_prepend (init_list, klass);
	mono_native_tls_set_value (init_pending_tls_id, init_list);

	/*
	 * We want to avoid doing complicated work inside locks, so we compute all the required
	 * information and write it to @klass inside a lock.
	 */

	MonoType *klass_byval_arg;
	klass_byval_arg = m_class_get_byval_arg (klass);
	if (klass_byval_arg->type == MONO_TYPE_ARRAY || klass_byval_arg->type == MONO_TYPE_SZARRAY) {
		MonoClass *element_class = klass->element_class;
		MonoClass *cast_class = klass->cast_class;

		if (!element_class->inited) 
			mono_class_init_internal (element_class);
		if (mono_class_set_type_load_failure_causedby_class (klass, element_class, "Could not load array element class"))
			goto leave;
		if (!cast_class->inited)
			mono_class_init_internal (cast_class);
		if (mono_class_set_type_load_failure_causedby_class (klass, cast_class, "Could not load array cast class"))
			goto leave;
	}

	UnlockedIncrement (&mono_stats.initialized_class_count);

	if (mono_class_is_ginst (klass) && !mono_class_get_generic_class (klass)->is_dynamic) {
		MonoClass *gklass = mono_class_get_generic_class (klass)->container_class;

		mono_class_init_internal (gklass);
		if (mono_class_set_type_load_failure_causedby_class (klass, gklass, "Generic Type Definition failed to init"))
			goto leave;

		if (MONO_CLASS_IS_INTERFACE_INTERNAL (klass))
			mono_class_setup_interface_id (klass);
	}

	if (klass->parent && !klass->parent->inited)
		mono_class_init_internal (klass->parent);

	has_cached_info = mono_class_get_cached_class_info (klass, &cached_info);

	/* Compute instance size etc. */
	init_sizes_with_info (klass, has_cached_info ? &cached_info : NULL);
	if (mono_class_has_failure (klass))
		goto leave;

	mono_class_setup_supertypes (klass);

	initialize_object_slots (klass);

	/* 
	 * Initialize the rest of the data without creating a generic vtable if possible.
	 * If possible, also compute vtable_size, so mono_class_create_runtime_vtable () can
	 * also avoid computing a generic vtable.
	 */
	if (has_cached_info) {
		/* AOT case */
		vtable_size = cached_info.vtable_size;
		ghcimpl = cached_info.ghcimpl;
		has_cctor = cached_info.has_cctor;
	} else if (klass->rank == 1 && klass_byval_arg->type == MONO_TYPE_SZARRAY) {
		/* SZARRAY can have 3 vtable layouts, with and without the stelemref method and enum element type
		 * The first slot if for array with.
		 */
		static int szarray_vtable_size[3] = { 0 };

		int slot;

		if (MONO_TYPE_IS_REFERENCE (m_class_get_byval_arg (m_class_get_element_class (klass))))
			slot = 0;
		else if (klass->element_class->enumtype)
			slot = 1;
		else
			slot = 2;

		/* SZARRAY case */
		if (!szarray_vtable_size [slot]) {
			mono_class_setup_vtable (klass);
			szarray_vtable_size [slot] = klass->vtable_size;
			vtable_size = klass->vtable_size;
		} else {
			vtable_size = szarray_vtable_size[slot];
		}
	} else if (mono_class_is_ginst (klass) && !MONO_CLASS_IS_INTERFACE_INTERNAL (klass)) {
		MonoClass *gklass = mono_class_get_generic_class (klass)->container_class;

		/* Generic instance case */
		ghcimpl = gklass->ghcimpl;
		has_cctor = gklass->has_cctor;

		mono_class_setup_vtable (gklass);
		if (mono_class_set_type_load_failure_causedby_class (klass, gklass, "Generic type definition failed to init"))
			goto leave;

		vtable_size = gklass->vtable_size;
	} else {
		/* General case */

		/* C# doesn't allow interfaces to have cctors */
		if (!MONO_CLASS_IS_INTERFACE_INTERNAL (klass) || klass->image != mono_defaults.corlib) {
			MonoMethod *cmethod = NULL;

			if (mono_class_is_ginst (klass)) {
				MonoClass *gklass = mono_class_get_generic_class (klass)->container_class;

				/* Generic instance case */
				ghcimpl = gklass->ghcimpl;
				has_cctor = gklass->has_cctor;
			} else if (klass->type_token && !image_is_dynamic(klass->image)) {
				cmethod = mono_find_method_in_metadata (klass, ".cctor", 0, METHOD_ATTRIBUTE_SPECIAL_NAME);
				/* The find_method function ignores the 'flags' argument */
				if (cmethod && (cmethod->flags & METHOD_ATTRIBUTE_SPECIAL_NAME))
					has_cctor = 1;
			} else {
				mono_class_setup_methods (klass);
				if (mono_class_has_failure (klass))
					goto leave;

				int mcount = mono_class_get_method_count (klass);
				for (i = 0; i < mcount; ++i) {
					MonoMethod *method = klass->methods [i];
					if ((method->flags & METHOD_ATTRIBUTE_SPECIAL_NAME) && 
						(strcmp (".cctor", method->name) == 0)) {
						has_cctor = 1;
						break;
					}
				}
			}
		}
	}

	if (klass->rank) {
		array_method_count = array_get_method_count (klass);

		if (klass->interface_count) {
			int count_generic = generic_array_methods (klass);
			array_method_count += klass->interface_count * count_generic;
		}
	}

	if (klass->parent) {
		if (!klass->parent->vtable_size)
			mono_class_setup_vtable (klass->parent);
		if (mono_class_set_type_load_failure_causedby_class (klass, klass->parent, "Parent class vtable failed to initialize"))
			goto leave;
		g_assert (klass->parent->vtable_size);
		first_iface_slot = klass->parent->vtable_size;
		if (mono_class_setup_need_stelemref_method (klass))
			++first_iface_slot;
	}

	/*
	 * Do the actual changes to @klass inside the loader lock
	 */
	mono_loader_lock ();
	locked = TRUE;

	if (klass->inited || mono_class_has_failure (klass)) {
		/* Somebody might have gotten in before us */
		goto leave;
	}

	UnlockedIncrement (&mono_stats.initialized_class_count);

	if (mono_class_is_ginst (klass) && !mono_class_get_generic_class (klass)->is_dynamic)
		UnlockedIncrement (&mono_stats.generic_class_count);

	if (mono_class_is_ginst (klass) || image_is_dynamic (klass->image) || !klass->type_token || (has_cached_info && !cached_info.has_nested_classes))
		klass->nested_classes_inited = TRUE;
	klass->ghcimpl = ghcimpl;
	klass->has_cctor = has_cctor;
	if (vtable_size)
		klass->vtable_size = vtable_size;
	if (has_cached_info) {
		klass->has_finalize = cached_info.has_finalize;
		klass->has_finalize_inited = TRUE;
	}
	if (klass->rank)
		mono_class_set_method_count (klass, array_method_count);

	mono_loader_unlock ();
	locked = FALSE;

	mono_class_setup_interface_offsets_internal (klass, first_iface_slot, TRUE);

	if (mono_class_is_ginst (klass) && !mono_verifier_class_is_valid_generic_instantiation (klass))
		mono_class_set_type_load_failure (klass, "Invalid generic instantiation");

	goto leave;

leave:
	init_list = (GSList*)mono_native_tls_get_value (init_pending_tls_id);
	init_list = g_slist_remove (init_list, klass);
	mono_native_tls_set_value (init_pending_tls_id, init_list);

leave_no_init_pending:
	if (locked)
		mono_loader_unlock ();

	/* Leave this for last */
	mono_loader_lock ();
	klass->inited = 1;
	mono_loader_unlock ();

	return !mono_class_has_failure (klass);
}

gboolean
mono_class_init_checked (MonoClass *klass, MonoError *error)
{
	error_init (error);
	gboolean const success = mono_class_init_internal (klass);
	if (!success)
		mono_error_set_for_class_failure (error, klass);
	return success;
}

#ifndef DISABLE_COM
/*
 * COM initialization is delayed until needed.
 * However when a [ComImport] attribute is present on a type it will trigger
 * the initialization. This is not a problem unless the BCL being executed 
 * lacks the types that COM depends on (e.g. Variant on Silverlight).
 */
static void
init_com_from_comimport (MonoClass *klass)
{
	/* FIXME : we should add an extra checks to ensure COM can be initialized properly before continuing */
}
#endif /*DISABLE_COM*/

/*
 * LOCKING: this assumes the loader lock is held
 */
void
mono_class_setup_parent (MonoClass *klass, MonoClass *parent)
{
	gboolean system_namespace;
	gboolean is_corlib = mono_is_corlib_image (klass->image);

	system_namespace = !strcmp (klass->name_space, "System") && is_corlib;

	/* if root of the hierarchy */
	if (system_namespace && !strcmp (klass->name, "Object")) {
		klass->parent = NULL;
		klass->instance_size = MONO_ABI_SIZEOF (MonoObject);
		return;
	}
	if (!strcmp (klass->name, "<Module>")) {
		klass->parent = NULL;
		klass->instance_size = 0;
		return;
	}

	if (!MONO_CLASS_IS_INTERFACE_INTERNAL (klass)) {
		/* Imported COM Objects always derive from __ComObject. */
#ifndef DISABLE_COM
		if (MONO_CLASS_IS_IMPORT (klass)) {
			init_com_from_comimport (klass);
			if (parent == mono_defaults.object_class)
				parent = mono_class_get_com_object_class ();
		}
#endif
		if (!parent) {
			/* set the parent to something useful and safe, but mark the type as broken */
			parent = mono_defaults.object_class;
			mono_class_set_type_load_failure (klass, "");
			g_assert (parent);
		}

		klass->parent = parent;

		if (mono_class_is_ginst (parent) && !parent->name) {
			/*
			 * If the parent is a generic instance, we may get
			 * called before it is fully initialized, especially
			 * before it has its name.
			 */
			return;
		}

		klass->delegate  = parent->delegate;

		if (MONO_CLASS_IS_IMPORT (klass) || mono_class_is_com_object (parent))
			mono_class_set_is_com_object (klass);
		
		if (system_namespace) {
			if (klass->name [0] == 'D' && !strcmp (klass->name, "Delegate")) 
				klass->delegate  = 1;
		}

		if (klass->parent->enumtype || (mono_is_corlib_image (klass->parent->image) && (strcmp (klass->parent->name, "ValueType") == 0) && 
						(strcmp (klass->parent->name_space, "System") == 0)))
			klass->valuetype = 1;
		if (mono_is_corlib_image (klass->parent->image) && ((strcmp (klass->parent->name, "Enum") == 0) && (strcmp (klass->parent->name_space, "System") == 0))) {
			klass->valuetype = klass->enumtype = 1;
		}
		/*klass->enumtype = klass->parent->enumtype; */
	} else {
		/* initialize com types if COM interfaces are present */
#ifndef DISABLE_COM
		if (MONO_CLASS_IS_IMPORT (klass))
			init_com_from_comimport (klass);
#endif
		klass->parent = NULL;
	}

}

/* Locking: must be called with the loader lock held. */
void
mono_class_setup_interface_id_nolock (MonoClass *klass)
{
	if (!MONO_CLASS_IS_INTERFACE_INTERNAL (klass) || klass->interface_id)
		return;
	klass->interface_id = mono_get_unique_iid (klass);

	if (mono_is_corlib_image (klass->image) && !strcmp (m_class_get_name_space (klass), "System.Collections.Generic")) {
		//FIXME IEnumerator needs to be special because GetEnumerator uses magic under the hood
	    /* FIXME: System.Array/InternalEnumerator don't need all this interface fabrication machinery.
	    * MS returns diferrent types based on which instance is called. For example:
	    * 	object obj = new byte[10][];
	    *	Type a = ((IEnumerable<byte[]>)obj).GetEnumerator ().GetType ();
	    *	Type b = ((IEnumerable<IList<byte>>)obj).GetEnumerator ().GetType ();
	    * 	a != b ==> true
		*/
		const char *name = m_class_get_name (klass);
		if (!strcmp (name, "IList`1") || !strcmp (name, "ICollection`1") || !strcmp (name, "IEnumerable`1") || !strcmp (name, "IEnumerator`1"))
			klass->is_array_special_interface = 1;
	}
}


/*
 * LOCKING: this assumes the loader lock is held
 */
void
mono_class_setup_mono_type (MonoClass *klass)
{
	const char *name = klass->name;
	const char *nspace = klass->name_space;
	gboolean is_corlib = mono_is_corlib_image (klass->image);

	klass->this_arg.byref = 1;
	klass->this_arg.data.klass = klass;
	klass->this_arg.type = MONO_TYPE_CLASS;
	klass->_byval_arg.data.klass = klass;
	klass->_byval_arg.type = MONO_TYPE_CLASS;

	if (is_corlib && !strcmp (nspace, "System")) {
		if (!strcmp (name, "ValueType")) {
			/*
			 * do not set the valuetype bit for System.ValueType.
			 * klass->valuetype = 1;
			 */
			klass->blittable = TRUE;
		} else if (!strcmp (name, "Enum")) {
			/*
			 * do not set the valuetype bit for System.Enum.
			 * klass->valuetype = 1;
			 */
			klass->valuetype = 0;
			klass->enumtype = 0;
		} else if (!strcmp (name, "Object")) {
			klass->_byval_arg.type = MONO_TYPE_OBJECT;
			klass->this_arg.type = MONO_TYPE_OBJECT;
		} else if (!strcmp (name, "String")) {
			klass->_byval_arg.type = MONO_TYPE_STRING;
			klass->this_arg.type = MONO_TYPE_STRING;
		} else if (!strcmp (name, "TypedReference")) {
			klass->_byval_arg.type = MONO_TYPE_TYPEDBYREF;
			klass->this_arg.type = MONO_TYPE_TYPEDBYREF;
		}
	}

	if (klass->valuetype) {
		int t = MONO_TYPE_VALUETYPE;

		if (is_corlib && !strcmp (nspace, "System")) {
			switch (*name) {
			case 'B':
				if (!strcmp (name, "Boolean")) {
					t = MONO_TYPE_BOOLEAN;
				} else if (!strcmp(name, "Byte")) {
					t = MONO_TYPE_U1;
					klass->blittable = TRUE;						
				}
				break;
			case 'C':
				if (!strcmp (name, "Char")) {
					t = MONO_TYPE_CHAR;
				}
				break;
			case 'D':
				if (!strcmp (name, "Double")) {
					t = MONO_TYPE_R8;
					klass->blittable = TRUE;						
				}
				break;
			case 'I':
				if (!strcmp (name, "Int32")) {
					t = MONO_TYPE_I4;
					klass->blittable = TRUE;
				} else if (!strcmp(name, "Int16")) {
					t = MONO_TYPE_I2;
					klass->blittable = TRUE;
				} else if (!strcmp(name, "Int64")) {
					t = MONO_TYPE_I8;
					klass->blittable = TRUE;
				} else if (!strcmp(name, "IntPtr")) {
					t = MONO_TYPE_I;
					klass->blittable = TRUE;
				}
				break;
			case 'S':
				if (!strcmp (name, "Single")) {
					t = MONO_TYPE_R4;
					klass->blittable = TRUE;						
				} else if (!strcmp(name, "SByte")) {
					t = MONO_TYPE_I1;
					klass->blittable = TRUE;
				}
				break;
			case 'U':
				if (!strcmp (name, "UInt32")) {
					t = MONO_TYPE_U4;
					klass->blittable = TRUE;
				} else if (!strcmp(name, "UInt16")) {
					t = MONO_TYPE_U2;
					klass->blittable = TRUE;
				} else if (!strcmp(name, "UInt64")) {
					t = MONO_TYPE_U8;
					klass->blittable = TRUE;
				} else if (!strcmp(name, "UIntPtr")) {
					t = MONO_TYPE_U;
					klass->blittable = TRUE;
				}
				break;
			case 'T':
				if (!strcmp (name, "TypedReference")) {
					t = MONO_TYPE_TYPEDBYREF;
					klass->blittable = TRUE;
				}
				break;
			case 'V':
				if (!strcmp (name, "Void")) {
					t = MONO_TYPE_VOID;
				}
				break;
			default:
				break;
			}
		}
		klass->_byval_arg.type = (MonoTypeEnum)t;
		klass->this_arg.type = (MonoTypeEnum)t;
	}

	mono_class_setup_interface_id_nolock (klass);
}

static MonoMethod*
create_array_method (MonoClass *klass, const char *name, MonoMethodSignature *sig)
{
	MonoMethod *method;

	method = (MonoMethod *) mono_image_alloc0 (klass->image, sizeof (MonoMethodPInvoke));
	method->klass = klass;
	method->flags = METHOD_ATTRIBUTE_PUBLIC;
	method->iflags = METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL;
	method->signature = sig;
	method->name = name;
	method->slot = -1;
	/* .ctor */
	if (name [0] == '.') {
		method->flags |= METHOD_ATTRIBUTE_RT_SPECIAL_NAME | METHOD_ATTRIBUTE_SPECIAL_NAME;
	} else {
		method->iflags |= METHOD_IMPL_ATTRIBUTE_RUNTIME;
	}
	return method;
}

/*
 * mono_class_setup_methods:
 * @class: a class
 *
 *   Initializes the 'methods' array in CLASS.
 * Calling this method should be avoided if possible since it allocates a lot 
 * of long-living MonoMethod structures.
 * Methods belonging to an interface are assigned a sequential slot starting
 * from 0.
 *
 * On failure this function sets klass->has_failure and stores a MonoErrorBoxed with details
 */
void
mono_class_setup_methods (MonoClass *klass)
{
	int i, count;
	MonoMethod **methods;

	if (klass->methods)
		return;

	if (mono_class_is_ginst (klass)) {
		ERROR_DECL (error);
		MonoClass *gklass = mono_class_get_generic_class (klass)->container_class;

		mono_class_init_internal (gklass);
		if (!mono_class_has_failure (gklass))
			mono_class_setup_methods (gklass);
		if (mono_class_set_type_load_failure_causedby_class (klass, gklass, "Generic type definition failed to load"))
			return;

		/* The + 1 makes this always non-NULL to pass the check in mono_class_setup_methods () */
		count = mono_class_get_method_count (gklass);
		methods = (MonoMethod **)mono_class_alloc0 (klass, sizeof (MonoMethod*) * (count + 1));

		for (i = 0; i < count; i++) {
			methods [i] = mono_class_inflate_generic_method_full_checked (
				gklass->methods [i], klass, mono_class_get_context (klass), error);
			if (!is_ok (error)) {
				char *method = mono_method_full_name (gklass->methods [i], TRUE);
				mono_class_set_type_load_failure (klass, "Could not inflate method %s due to %s", method, mono_error_get_message (error));

				g_free (method);
				mono_error_cleanup (error);
				return;				
			}
		}
	} else if (klass->rank) {
		ERROR_DECL (error);
		MonoMethod *amethod;
		MonoMethodSignature *sig;
		int count_generic = 0, first_generic = 0;
		int method_num = 0;

		count = array_get_method_count (klass);

		mono_class_setup_interfaces (klass, error);
		g_assert (is_ok (error)); /*FIXME can this fail for array types?*/

		if (klass->interface_count) {
			count_generic = generic_array_methods (klass);
			first_generic = count;
			count += klass->interface_count * count_generic;
		}

		methods = (MonoMethod **)mono_class_alloc0 (klass, sizeof (MonoMethod*) * count);

		sig = mono_metadata_signature_alloc (klass->image, klass->rank);
		sig->ret = mono_get_void_type ();
		sig->pinvoke = TRUE;
		sig->hasthis = TRUE;
		for (i = 0; i < klass->rank; ++i)
			sig->params [i] = mono_get_int32_type ();

		amethod = create_array_method (klass, ".ctor", sig);
		methods [method_num++] = amethod;

		if (array_supports_additional_ctor_method (klass)) {
			sig = mono_metadata_signature_alloc (klass->image, klass->rank * 2);
			sig->ret = mono_get_void_type ();
			sig->pinvoke = TRUE;
			sig->hasthis = TRUE;
			for (i = 0; i < klass->rank * 2; ++i)
				sig->params [i] = mono_get_int32_type ();

			amethod = create_array_method (klass, ".ctor", sig);
			methods [method_num++] = amethod;
		}

		/* element Get (idx11, [idx2, ...]) */
		sig = mono_metadata_signature_alloc (klass->image, klass->rank);
		sig->ret = m_class_get_byval_arg (m_class_get_element_class (klass));
		sig->pinvoke = TRUE;
		sig->hasthis = TRUE;
		for (i = 0; i < klass->rank; ++i)
			sig->params [i] = mono_get_int32_type ();
		amethod = create_array_method (klass, "Get", sig);
		methods [method_num++] = amethod;
		/* element& Address (idx11, [idx2, ...]) */
		sig = mono_metadata_signature_alloc (klass->image, klass->rank);
		sig->ret = &klass->element_class->this_arg;
		sig->pinvoke = TRUE;
		sig->hasthis = TRUE;
		for (i = 0; i < klass->rank; ++i)
			sig->params [i] = mono_get_int32_type ();
		amethod = create_array_method (klass, "Address", sig);
		methods [method_num++] = amethod;
		/* void Set (idx11, [idx2, ...], element) */
		sig = mono_metadata_signature_alloc (klass->image, klass->rank + 1);
		sig->ret = mono_get_void_type ();
		sig->pinvoke = TRUE;
		sig->hasthis = TRUE;
		for (i = 0; i < klass->rank; ++i)
			sig->params [i] = mono_get_int32_type ();
		sig->params [i] = m_class_get_byval_arg (m_class_get_element_class (klass));
		amethod = create_array_method (klass, "Set", sig);
		methods [method_num++] = amethod;

		GHashTable *cache = g_hash_table_new (NULL, NULL);
		for (i = 0; i < klass->interface_count; i++)
			setup_generic_array_ifaces (klass, klass->interfaces [i], methods, first_generic + i * count_generic, cache);
		g_hash_table_destroy (cache);
	} else if (mono_class_has_static_metadata (klass)) {
		ERROR_DECL (error);
		int first_idx = mono_class_get_first_method_idx (klass);

		count = mono_class_get_method_count (klass);
		methods = (MonoMethod **)mono_class_alloc (klass, sizeof (MonoMethod*) * count);
		for (i = 0; i < count; ++i) {
			int idx = mono_metadata_translate_token_index (klass->image, MONO_TABLE_METHOD, first_idx + i + 1);
			methods [i] = mono_get_method_checked (klass->image, MONO_TOKEN_METHOD_DEF | idx, klass, NULL, error);
			if (!methods [i]) {
				mono_class_set_type_load_failure (klass, "Could not load method %d due to %s", i, mono_error_get_message (error));
				mono_error_cleanup (error);
			}
		}
	} else {
		methods = (MonoMethod **)mono_class_alloc (klass, sizeof (MonoMethod*) * 1);
		count = 0;
	}

	if (MONO_CLASS_IS_INTERFACE_INTERNAL (klass)) {
		int slot = 0;
		/*Only assign slots to virtual methods as interfaces are allowed to have static methods.*/
		for (i = 0; i < count; ++i) {
			if (methods [i]->flags & METHOD_ATTRIBUTE_VIRTUAL)
			{
				if (method_is_reabstracted (methods[i]->flags)) {
					if (!methods [i]->is_inflated)
						mono_method_set_is_reabstracted (methods [i]);
					continue;
				}
				methods [i]->slot = slot++;
			}
		}
	}

	mono_image_lock (klass->image);

	if (!klass->methods) {
		mono_class_set_method_count (klass, count);

		/* Needed because of the double-checking locking pattern */
		mono_memory_barrier ();

		klass->methods = methods;
	}

	mono_image_unlock (klass->image);
}

/*
 * mono_class_setup_properties:
 *
 *   Initialize klass->ext.property and klass->ext.properties.
 *
 * This method can fail the class.
 */
void
mono_class_setup_properties (MonoClass *klass)
{
	guint startm, endm, i, j;
	guint32 cols [MONO_PROPERTY_SIZE];
	MonoTableInfo *msemt = &klass->image->tables [MONO_TABLE_METHODSEMANTICS];
	MonoProperty *properties;
	guint32 last;
	int first, count;
	MonoClassPropertyInfo *info;

	info = mono_class_get_property_info (klass);
	if (info)
		return;

	if (mono_class_is_ginst (klass)) {
		MonoClass *gklass = mono_class_get_generic_class (klass)->container_class;

		mono_class_init_internal (gklass);
		mono_class_setup_properties (gklass);
		if (mono_class_set_type_load_failure_causedby_class (klass, gklass, "Generic type definition failed to load"))
			return;

		MonoClassPropertyInfo *ginfo = mono_class_get_property_info (gklass);
		properties = mono_class_new0 (klass, MonoProperty, ginfo->count + 1);

		for (i = 0; i < ginfo->count; i++) {
			ERROR_DECL (error);
			MonoProperty *prop = &properties [i];

			*prop = ginfo->properties [i];

			if (prop->get)
				prop->get = mono_class_inflate_generic_method_full_checked (
					prop->get, klass, mono_class_get_context (klass), error);
			if (prop->set)
				prop->set = mono_class_inflate_generic_method_full_checked (
					prop->set, klass, mono_class_get_context (klass), error);

			g_assert (is_ok (error)); /*FIXME proper error handling*/
			prop->parent = klass;
		}

		first = ginfo->first;
		count = ginfo->count;
	} else {
		first = mono_metadata_properties_from_typedef (klass->image, mono_metadata_token_index (klass->type_token) - 1, &last);
		count = last - first;

		if (count) {
			mono_class_setup_methods (klass);
			if (mono_class_has_failure (klass))
				return;
		}

		properties = (MonoProperty *)mono_class_alloc0 (klass, sizeof (MonoProperty) * count);
		for (i = first; i < last; ++i) {
			mono_metadata_decode_table_row (klass->image, MONO_TABLE_PROPERTY, i, cols, MONO_PROPERTY_SIZE);
			properties [i - first].parent = klass;
			properties [i - first].attrs = cols [MONO_PROPERTY_FLAGS];
			properties [i - first].name = mono_metadata_string_heap (klass->image, cols [MONO_PROPERTY_NAME]);

			startm = mono_metadata_methods_from_property (klass->image, i, &endm);
			int first_idx = mono_class_get_first_method_idx (klass);
			for (j = startm; j < endm; ++j) {
				MonoMethod *method;

				mono_metadata_decode_row (msemt, j, cols, MONO_METHOD_SEMA_SIZE);

				if (klass->image->uncompressed_metadata) {
					ERROR_DECL (error);
					/* It seems like the MONO_METHOD_SEMA_METHOD column needs no remapping */
					method = mono_get_method_checked (klass->image, MONO_TOKEN_METHOD_DEF | cols [MONO_METHOD_SEMA_METHOD], klass, NULL, error);
					mono_error_cleanup (error); /* FIXME don't swallow this error */
				} else {
					method = klass->methods [cols [MONO_METHOD_SEMA_METHOD] - 1 - first_idx];
				}

				switch (cols [MONO_METHOD_SEMA_SEMANTICS]) {
				case METHOD_SEMANTIC_SETTER:
					properties [i - first].set = method;
					break;
				case METHOD_SEMANTIC_GETTER:
					properties [i - first].get = method;
					break;
				default:
					break;
				}
			}
		}
	}

	info = (MonoClassPropertyInfo*)mono_class_alloc0 (klass, sizeof (MonoClassPropertyInfo));
	info->first = first;
	info->count = count;
	info->properties = properties;
	mono_memory_barrier ();

	/* This might leak 'info' which was allocated from the image mempool */
	mono_class_set_property_info (klass, info);
}

static MonoMethod**
inflate_method_listz (MonoMethod **methods, MonoClass *klass, MonoGenericContext *context)
{
	MonoMethod **om, **retval;
	int count;

	for (om = methods, count = 0; *om; ++om, ++count)
		;

	retval = g_new0 (MonoMethod*, count + 1);
	count = 0;
	for (om = methods, count = 0; *om; ++om, ++count) {
		ERROR_DECL (error);
		retval [count] = mono_class_inflate_generic_method_full_checked (*om, klass, context, error);
		g_assert (is_ok (error)); /*FIXME proper error handling*/
	}

	return retval;
}

/*This method can fail the class.*/
void
mono_class_setup_events (MonoClass *klass)
{
	int first, count;
	guint startm, endm, i, j;
	guint32 cols [MONO_EVENT_SIZE];
	MonoTableInfo *msemt = &klass->image->tables [MONO_TABLE_METHODSEMANTICS];
	guint32 last;
	MonoEvent *events;

	MonoClassEventInfo *info = mono_class_get_event_info (klass);
	if (info)
		return;

	if (mono_class_is_ginst (klass)) {
		MonoClass *gklass = mono_class_get_generic_class (klass)->container_class;
		MonoGenericContext *context = NULL;

		mono_class_setup_events (gklass);
		if (mono_class_set_type_load_failure_causedby_class (klass, gklass, "Generic type definition failed to load"))
			return;

		MonoClassEventInfo *ginfo = mono_class_get_event_info (gklass);
		first = ginfo->first;
		count = ginfo->count;

		events = mono_class_new0 (klass, MonoEvent, count);

		if (count)
			context = mono_class_get_context (klass);

		for (i = 0; i < count; i++) {
			ERROR_DECL (error);
			MonoEvent *event = &events [i];
			MonoEvent *gevent = &ginfo->events [i];

			event->parent = klass;
			event->name = gevent->name;
			event->add = gevent->add ? mono_class_inflate_generic_method_full_checked (gevent->add, klass, context, error) : NULL;
			g_assert (is_ok (error)); /*FIXME proper error handling*/
			event->remove = gevent->remove ? mono_class_inflate_generic_method_full_checked (gevent->remove, klass, context, error) : NULL;
			g_assert (is_ok (error)); /*FIXME proper error handling*/
			event->raise = gevent->raise ? mono_class_inflate_generic_method_full_checked (gevent->raise, klass, context, error) : NULL;
			g_assert (is_ok (error)); /*FIXME proper error handling*/

#ifndef MONO_SMALL_CONFIG
			event->other = gevent->other ? inflate_method_listz (gevent->other, klass, context) : NULL;
#endif
			event->attrs = gevent->attrs;
		}
	} else {
		first = mono_metadata_events_from_typedef (klass->image, mono_metadata_token_index (klass->type_token) - 1, &last);
		count = last - first;

		if (count) {
			mono_class_setup_methods (klass);
			if (mono_class_has_failure (klass)) {
				return;
			}
		}

		events = (MonoEvent *)mono_class_alloc0 (klass, sizeof (MonoEvent) * count);
		for (i = first; i < last; ++i) {
			MonoEvent *event = &events [i - first];

			mono_metadata_decode_table_row (klass->image, MONO_TABLE_EVENT, i, cols, MONO_EVENT_SIZE);
			event->parent = klass;
			event->attrs = cols [MONO_EVENT_FLAGS];
			event->name = mono_metadata_string_heap (klass->image, cols [MONO_EVENT_NAME]);

			startm = mono_metadata_methods_from_event (klass->image, i, &endm);
			int first_idx = mono_class_get_first_method_idx (klass);
			for (j = startm; j < endm; ++j) {
				MonoMethod *method;

				mono_metadata_decode_row (msemt, j, cols, MONO_METHOD_SEMA_SIZE);

				if (klass->image->uncompressed_metadata) {
					ERROR_DECL (error);
					/* It seems like the MONO_METHOD_SEMA_METHOD column needs no remapping */
					method = mono_get_method_checked (klass->image, MONO_TOKEN_METHOD_DEF | cols [MONO_METHOD_SEMA_METHOD], klass, NULL, error);
					mono_error_cleanup (error); /* FIXME don't swallow this error */
				} else {
					method = klass->methods [cols [MONO_METHOD_SEMA_METHOD] - 1 - first_idx];
				}

				switch (cols [MONO_METHOD_SEMA_SEMANTICS]) {
				case METHOD_SEMANTIC_ADD_ON:
					event->add = method;
					break;
				case METHOD_SEMANTIC_REMOVE_ON:
					event->remove = method;
					break;
				case METHOD_SEMANTIC_FIRE:
					event->raise = method;
					break;
				case METHOD_SEMANTIC_OTHER: {
#ifndef MONO_SMALL_CONFIG
					int n = 0;

					if (event->other == NULL) {
						event->other = g_new0 (MonoMethod*, 2);
					} else {
						while (event->other [n])
							n++;
						event->other = (MonoMethod **)g_realloc (event->other, (n + 2) * sizeof (MonoMethod*));
					}
					event->other [n] = method;
					/* NULL terminated */
					event->other [n + 1] = NULL;
#endif
					break;
				}
				default:
					break;
				}
			}
		}
	}

	info = (MonoClassEventInfo*)mono_class_alloc0 (klass, sizeof (MonoClassEventInfo));
	info->events = events;
	info->first = first;
	info->count = count;

	mono_memory_barrier ();

	mono_class_set_event_info (klass, info);
}


/*
 * mono_class_setup_interface_id:
 *
 * Initializes MonoClass::interface_id if required.
 *
 * LOCKING: Acquires the loader lock.
 */
void
mono_class_setup_interface_id (MonoClass *klass)
{
	g_assert (MONO_CLASS_IS_INTERFACE_INTERNAL (klass));
	mono_loader_lock ();
	mono_class_setup_interface_id_nolock (klass);
	mono_loader_unlock ();
}

/*
 * mono_class_setup_interfaces:
 *
 *   Initialize klass->interfaces/interfaces_count.
 * LOCKING: Acquires the loader lock.
 * This function can fail the type.
 */
void
mono_class_setup_interfaces (MonoClass *klass, MonoError *error)
{
	int i, interface_count;
	MonoClass *iface, **interfaces;

	error_init (error);

	if (klass->interfaces_inited)
		return;

	if (klass->rank == 1 && m_class_get_byval_arg (klass)->type != MONO_TYPE_ARRAY) {
		MonoType *args [1];
		MonoClass *array_ifaces [16];

		/*
		 * Arrays implement IList and IReadOnlyList or their base interfaces if they are not linked out.
		 * For arrays of enums, they implement the interfaces for the base type as well.
		 */
		interface_count = 0;
		if (mono_defaults.generic_ilist_class) {
			array_ifaces [interface_count ++] = mono_defaults.generic_ilist_class;
		} else {
			iface = mono_class_try_get_icollection_class ();
			if (iface)
				array_ifaces [interface_count ++] = iface;
		}
		if (mono_defaults.generic_ireadonlylist_class) {
			array_ifaces [interface_count ++] = mono_defaults.generic_ireadonlylist_class;
		} else {
			iface = mono_class_try_get_ireadonlycollection_class ();
			if (iface)
				array_ifaces [interface_count ++] = iface;
		}
		if (!mono_defaults.generic_ilist_class && !mono_defaults.generic_ireadonlylist_class) {
			iface = mono_class_try_get_ienumerable_class ();
			if (iface)
				array_ifaces [interface_count ++] = iface;
		}
		int mult = klass->element_class->enumtype ? 2 : 1;

		interfaces = (MonoClass **)mono_image_alloc0 (klass->image, sizeof (MonoClass*) * interface_count * mult);

		int itf_idx = 0;
		args [0] = m_class_get_byval_arg (m_class_get_element_class (klass));
		for (int i = 0; i < interface_count; ++i)
			interfaces [itf_idx++] = mono_class_bind_generic_parameters (array_ifaces [i], 1, args, FALSE);
		if (klass->element_class->enumtype) {
			args [0] = mono_class_enum_basetype_internal (klass->element_class);
			for (int i = 0; i < interface_count; ++i)
				interfaces [itf_idx++] = mono_class_bind_generic_parameters (array_ifaces [i], 1, args, FALSE);
		}
		interface_count *= mult;
		g_assert (itf_idx == interface_count);
	} else if (mono_class_is_ginst (klass)) {
		MonoClass *gklass = mono_class_get_generic_class (klass)->container_class;

		mono_class_setup_interfaces (gklass, error);
		if (!is_ok (error)) {
			mono_class_set_type_load_failure (klass, "Could not setup the interfaces");
			return;
		}

		interface_count = gklass->interface_count;
		interfaces = mono_class_new0 (klass, MonoClass *, interface_count);
		for (i = 0; i < interface_count; i++) {
			interfaces [i] = mono_class_inflate_generic_class_checked (gklass->interfaces [i], mono_generic_class_get_context (mono_class_get_generic_class (klass)), error);
			if (!is_ok (error)) {
				mono_class_set_type_load_failure (klass, "Could not setup the interfaces");
				return;
			}
		}
	} else {
		interface_count = 0;
		interfaces = NULL;
	}

	mono_loader_lock ();
	if (!klass->interfaces_inited) {
		klass->interface_count = interface_count;
		klass->interfaces = interfaces;

		mono_memory_barrier ();

		klass->interfaces_inited = TRUE;
	}
	mono_loader_unlock ();
}


/*
 * mono_class_setup_has_finalizer:
 *
 *   Initialize klass->has_finalizer if it isn't already initialized.
 *
 * LOCKING: Acquires the loader lock.
 */
void
mono_class_setup_has_finalizer (MonoClass *klass)
{
	gboolean has_finalize = FALSE;

	if (m_class_is_has_finalize_inited (klass))
		return;

	/* Interfaces and valuetypes are not supposed to have finalizers */
	if (!(MONO_CLASS_IS_INTERFACE_INTERNAL (klass) || m_class_is_valuetype (klass))) {
		MonoMethod *cmethod = NULL;

		if (m_class_get_rank (klass) == 1 && m_class_get_byval_arg (klass)->type == MONO_TYPE_SZARRAY) {
		} else if (mono_class_is_ginst (klass)) {
			MonoClass *gklass = mono_class_get_generic_class (klass)->container_class;

			has_finalize = mono_class_has_finalizer (gklass);
		} else if (m_class_get_parent (klass) && m_class_has_finalize (m_class_get_parent (klass))) {
			has_finalize = TRUE;
		} else {
			if (m_class_get_parent (klass)) {
				/*
				 * Can't search in metadata for a method named Finalize, because that
				 * ignores overrides.
				 */
				mono_class_setup_vtable (klass);
				if (mono_class_has_failure (klass))
					cmethod = NULL;
				else
					cmethod = m_class_get_vtable (klass) [mono_class_get_object_finalize_slot ()];
			}

			if (cmethod) {
				g_assert (m_class_get_vtable_size (klass) > mono_class_get_object_finalize_slot ());

				if (m_class_get_parent (klass)) {
					if (cmethod->is_inflated)
						cmethod = ((MonoMethodInflated*)cmethod)->declaring;
					if (cmethod != mono_class_get_default_finalize_method ())
						has_finalize = TRUE;
				}
			}
		}
	}

	mono_loader_lock ();
	if (!m_class_is_has_finalize_inited (klass)) {
		klass->has_finalize = has_finalize ? 1 : 0;

		mono_memory_barrier ();
		klass->has_finalize_inited = TRUE;
	}
	mono_loader_unlock ();
}

/*
 * mono_class_setup_supertypes:
 * @class: a class
 *
 * Build the data structure needed to make fast type checks work.
 * This currently sets two fields in @class:
 *  - idepth: distance between @class and System.Object in the type
 *    hierarchy + 1
 *  - supertypes: array of classes: each element has a class in the hierarchy
 *    starting from @class up to System.Object
 * 
 * LOCKING: Acquires the loader lock.
 */
void
mono_class_setup_supertypes (MonoClass *klass)
{
	int ms, idepth;
	MonoClass **supertypes;

	mono_atomic_load_acquire (supertypes, MonoClass **, &klass->supertypes);
	if (supertypes)
		return;

	if (klass->parent && !klass->parent->supertypes)
		mono_class_setup_supertypes (klass->parent);
	if (klass->parent)
		idepth = klass->parent->idepth + 1;
	else
		idepth = 1;

	ms = MAX (MONO_DEFAULT_SUPERTABLE_SIZE, idepth);
	supertypes = (MonoClass **)mono_class_alloc0 (klass, sizeof (MonoClass *) * ms);

	if (klass->parent) {
		CHECKED_METADATA_WRITE_PTR ( supertypes [idepth - 1] , klass );

		int supertype_idx;
		for (supertype_idx = 0; supertype_idx < klass->parent->idepth; supertype_idx++)
			CHECKED_METADATA_WRITE_PTR ( supertypes [supertype_idx] , klass->parent->supertypes [supertype_idx] );
	} else {
		CHECKED_METADATA_WRITE_PTR ( supertypes [0] , klass );
	}

	mono_memory_barrier ();

	mono_loader_lock ();
	klass->idepth = idepth;
	/* Needed so idepth is visible before supertypes is set */
	mono_memory_barrier ();
	klass->supertypes = supertypes;
	mono_loader_unlock ();
}

/* mono_class_setup_nested_types:
 *
 * Initialize the nested_classes property for the given MonoClass if it hasn't already been initialized.
 *
 * LOCKING: Acquires the loader lock.
 */
void
mono_class_setup_nested_types (MonoClass *klass)
{
	ERROR_DECL (error);
	GList *classes, *nested_classes, *l;
	int i;

	if (klass->nested_classes_inited)
		return;

	if (!klass->type_token) {
		mono_loader_lock ();
		klass->nested_classes_inited = TRUE;
		mono_loader_unlock ();
		return;
	}

	i = mono_metadata_nesting_typedef (klass->image, klass->type_token, 1);
	classes = NULL;
	while (i) {
		MonoClass* nclass;
		guint32 cols [MONO_NESTED_CLASS_SIZE];
		mono_metadata_decode_row (&klass->image->tables [MONO_TABLE_NESTEDCLASS], i - 1, cols, MONO_NESTED_CLASS_SIZE);
		nclass = mono_class_create_from_typedef (klass->image, MONO_TOKEN_TYPE_DEF | cols [MONO_NESTED_CLASS_NESTED], error);
		if (!is_ok (error)) {
			/*FIXME don't swallow the error message*/
			mono_error_cleanup (error);

			i = mono_metadata_nesting_typedef (klass->image, klass->type_token, i + 1);
			continue;
		}

		classes = g_list_prepend (classes, nclass);

		i = mono_metadata_nesting_typedef (klass->image, klass->type_token, i + 1);
	}

	nested_classes = NULL;
	for (l = classes; l; l = l->next)
		nested_classes = mono_g_list_prepend_image (klass->image, nested_classes, l->data);
	g_list_free (classes);

	mono_loader_lock ();
	if (!klass->nested_classes_inited) {
		mono_class_set_nested_classes_property (klass, nested_classes);
		mono_memory_barrier ();
		klass->nested_classes_inited = TRUE;
	}
	mono_loader_unlock ();
}

/**
 * mono_class_create_array_fill_type:
 *
 * Returns a \c MonoClass that is used by SGen to fill out nursery fragments before a collection.
 */
MonoClass *
mono_class_create_array_fill_type (void)
{
	static MonoClassArray aklass;

	aklass.klass.class_kind = MONO_CLASS_GC_FILLER;
	aklass.klass.element_class = mono_defaults.int64_class;
	aklass.klass.rank = 1;
	aklass.klass.instance_size = MONO_SIZEOF_MONO_ARRAY;
	aklass.klass.sizes.element_size = 8;
	aklass.klass.size_inited = 1;
	aklass.klass.name = "array_filler_type";

	return &aklass.klass;
}

void
mono_class_set_runtime_vtable (MonoClass *klass, MonoVTable *vtable)
{
	klass->runtime_vtable = vtable;
}

/**
 * mono_classes_init:
 *
 * Initialize the resources used by this module.
 * Known racy counters: `class_gparam_count`, `classes_size` and `mono_inflated_methods_size`
 */
MONO_NO_SANITIZE_THREAD
void
mono_classes_init (void)
{
	mono_os_mutex_init (&classes_mutex);

	mono_native_tls_alloc (&setup_fields_tls_id, NULL);
	mono_native_tls_alloc (&init_pending_tls_id, NULL);

	mono_counters_register ("MonoClassDef count",
							MONO_COUNTER_METADATA | MONO_COUNTER_INT, &class_def_count);
	mono_counters_register ("MonoClassGtd count",
							MONO_COUNTER_METADATA | MONO_COUNTER_INT, &class_gtd_count);
	mono_counters_register ("MonoClassGenericInst count",
							MONO_COUNTER_METADATA | MONO_COUNTER_INT, &class_ginst_count);
	mono_counters_register ("MonoClassGenericParam count",
							MONO_COUNTER_METADATA | MONO_COUNTER_INT, &class_gparam_count);
	mono_counters_register ("MonoClassArray count",
							MONO_COUNTER_METADATA | MONO_COUNTER_INT, &class_array_count);
	mono_counters_register ("MonoClassPointer count",
							MONO_COUNTER_METADATA | MONO_COUNTER_INT, &class_pointer_count);
	mono_counters_register ("Inflated methods size",
							MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &mono_inflated_methods_size);
	mono_counters_register ("Inflated classes size",
							MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &inflated_classes_size);
	mono_counters_register ("MonoClass size",
							MONO_COUNTER_METADATA | MONO_COUNTER_INT, &classes_size);
}
