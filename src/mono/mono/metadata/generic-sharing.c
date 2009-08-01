/*
 * generic-sharing.c: Support functions for generic sharing.
 *
 * Author:
 *   Mark Probst (mark.probst@gmail.com)
 *
 * Copyright 2007-2009 Novell, Inc (http://www.novell.com)
 */

#include <config.h>
#include <string.h>
#ifdef HAVE_ALLOCA_H
#include <alloca.h>
#endif

#ifdef _MSC_VER
#include <glib.h>
#endif
#include <mono/utils/mono-membar.h>
#include <mono/utils/mono-counters.h>

#include "metadata-internals.h"
#include "class.h"
#include "class-internals.h"
#include "marshal.h"
#include "debug-helpers.h"
#include "tabledefs.h"
#include "mempool-internals.h"

static int
type_check_context_used (MonoType *type, gboolean recursive)
{
	switch (mono_type_get_type (type)) {
	case MONO_TYPE_VAR:
		return MONO_GENERIC_CONTEXT_USED_CLASS;
	case MONO_TYPE_MVAR:
		return MONO_GENERIC_CONTEXT_USED_METHOD;
	case MONO_TYPE_SZARRAY:
		return mono_class_check_context_used (mono_type_get_class (type));
	case MONO_TYPE_ARRAY:
		return mono_class_check_context_used (mono_type_get_array_type (type)->eklass);
	case MONO_TYPE_CLASS:
		if (recursive)
			return mono_class_check_context_used (mono_type_get_class (type));
		else
			return 0;
	case MONO_TYPE_GENERICINST:
		if (recursive) {
			MonoGenericClass *gclass = type->data.generic_class;

			g_assert (gclass->container_class->generic_container);
			return mono_generic_context_check_used (&gclass->context);
		} else {
			return 0;
		}
	default:
		return 0;
	}
}

static int
inst_check_context_used (MonoGenericInst *inst)
{
	int context_used = 0;
	int i;

	if (!inst)
		return 0;

	for (i = 0; i < inst->type_argc; ++i)
		context_used |= type_check_context_used (inst->type_argv [i], TRUE);

	return context_used;
}

/*
 * mono_generic_context_check_used:
 * @context: a generic context
 *
 * Checks whether the context uses a type variable.  Returns an int
 * with the bit MONO_GENERIC_CONTEXT_USED_CLASS set to reflect whether
 * the context's class instantiation uses type variables.
 */
int
mono_generic_context_check_used (MonoGenericContext *context)
{
	int context_used = 0;

	context_used |= inst_check_context_used (context->class_inst);
	context_used |= inst_check_context_used (context->method_inst);

	return context_used;
}

/*
 * mono_class_check_context_used:
 * @class: a class
 *
 * Checks whether the class's generic context uses a type variable.
 * Returns an int with the bit MONO_GENERIC_CONTEXT_USED_CLASS set to
 * reflect whether the context's class instantiation uses type
 * variables.
 */
int
mono_class_check_context_used (MonoClass *class)
{
	int context_used = 0;

	context_used |= type_check_context_used (&class->this_arg, FALSE);
	context_used |= type_check_context_used (&class->byval_arg, FALSE);

	if (class->generic_class)
		context_used |= mono_generic_context_check_used (&class->generic_class->context);
	else if (class->generic_container)
		context_used |= mono_generic_context_check_used (&class->generic_container->context);

	return context_used;
}

/*
 * LOCKING: loader lock
 */
static MonoRuntimeGenericContextOtherInfoTemplate*
get_other_info_templates (MonoRuntimeGenericContextTemplate *template, int type_argc)
{
	g_assert (type_argc >= 0);
	if (type_argc == 0)
		return template->other_infos;
	return g_slist_nth_data (template->method_templates, type_argc - 1);
}

/*
 * LOCKING: loader lock
 */
static void
set_other_info_templates (MonoImage *image, MonoRuntimeGenericContextTemplate *template, int type_argc,
	MonoRuntimeGenericContextOtherInfoTemplate *oti)
{
	g_assert (type_argc >= 0);
	if (type_argc == 0)
		template->other_infos = oti;
	else {
		int length = g_slist_length (template->method_templates);
		GSList *list;

		/* FIXME: quadratic! */
		while (length < type_argc) {
			template->method_templates = g_slist_append_image (image, template->method_templates, NULL);
			length++;
		}

		list = g_slist_nth (template->method_templates, type_argc - 1);
		g_assert (list);
		list->data = oti;
	}
}

/*
 * LOCKING: loader lock
 */
static int
template_get_max_argc (MonoRuntimeGenericContextTemplate *template)
{
	return g_slist_length (template->method_templates);
}

/*
 * LOCKING: loader lock
 */
static MonoRuntimeGenericContextOtherInfoTemplate*
rgctx_template_get_other_slot (MonoRuntimeGenericContextTemplate *template, int type_argc, int slot)
{
	int i;
	MonoRuntimeGenericContextOtherInfoTemplate *oti;

	g_assert (slot >= 0);

	for (oti = get_other_info_templates (template, type_argc), i = 0; i < slot; oti = oti->next, ++i) {
		if (!oti)
			return NULL;
	}

	return oti;
}

/*
 * LOCKING: loader lock
 */
static int
rgctx_template_num_other_infos (MonoRuntimeGenericContextTemplate *template, int type_argc)
{
	MonoRuntimeGenericContextOtherInfoTemplate *oti;
	int i;

	for (i = 0, oti = get_other_info_templates (template, type_argc); oti; ++i, oti = oti->next)
		;

	return i;
}

/* Maps from uninstantiated generic classes to GList's of
 * uninstantiated generic classes whose parent is the key class or an
 * instance of the key class.
 *
 * LOCKING: loader lock
 */
static GHashTable *generic_subclass_hash;

/*
 * LOCKING: templates lock
 */
static void
class_set_rgctx_template (MonoClass *class, MonoRuntimeGenericContextTemplate *rgctx_template)
{
	if (!class->image->rgctx_template_hash)
		class->image->rgctx_template_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);

	g_hash_table_insert (class->image->rgctx_template_hash, class, rgctx_template);
}

/*
 * LOCKING: loader lock
 */
static MonoRuntimeGenericContextTemplate*
class_lookup_rgctx_template (MonoClass *class)
{
	MonoRuntimeGenericContextTemplate *template;

	if (!class->image->rgctx_template_hash)
		return NULL;

	template = g_hash_table_lookup (class->image->rgctx_template_hash, class);

	return template;
}

/*
 * LOCKING: loader lock
 */
static void
register_generic_subclass (MonoClass *class)
{
	MonoClass *parent = class->parent;
	MonoClass *subclass;
	MonoRuntimeGenericContextTemplate *rgctx_template = class_lookup_rgctx_template (class);

	g_assert (rgctx_template);

	if (parent->generic_class)
		parent = parent->generic_class->container_class;

	if (!generic_subclass_hash)
		generic_subclass_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);

	subclass = g_hash_table_lookup (generic_subclass_hash, parent);
	rgctx_template->next_subclass = subclass;
	g_hash_table_insert (generic_subclass_hash, parent, class);
}

static void
move_subclasses_not_in_image_foreach_func (MonoClass *class, MonoClass *subclass, MonoImage *image)
{
	MonoClass *new_list;

	if (class->image == image) {
		/* The parent class itself is in the image, so all the
		   subclasses must be in the image, too.  If not,
		   we're removing an image containing a class which
		   still has a subclass in another image. */

		while (subclass) {
			g_assert (subclass->image == image);
			subclass = class_lookup_rgctx_template (subclass)->next_subclass;
		}

		return;
	}

	new_list = NULL;
	while (subclass) {
		MonoRuntimeGenericContextTemplate *subclass_template = class_lookup_rgctx_template (subclass);
		MonoClass *next = subclass_template->next_subclass;

		if (subclass->image != image) {
			subclass_template->next_subclass = new_list;
			new_list = subclass;
		}

		subclass = next;
	}

	if (new_list)
		g_hash_table_insert (generic_subclass_hash, class, new_list);
}

/*
 * mono_class_unregister_image_generic_subclasses:
 * @image: an image
 *
 * Removes all classes of the image from the generic subclass hash.
 * Must be called when an image is unloaded.
 */
void
mono_class_unregister_image_generic_subclasses (MonoImage *image)
{
	GHashTable *old_hash;

	//g_print ("unregistering image %s\n", image->name);

	if (!generic_subclass_hash)
		return;

	mono_loader_lock ();

	old_hash = generic_subclass_hash;
	generic_subclass_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);

	g_hash_table_foreach (old_hash, (GHFunc)move_subclasses_not_in_image_foreach_func, image);

	mono_loader_unlock ();

	g_hash_table_destroy (old_hash);
}

static MonoRuntimeGenericContextTemplate*
alloc_template (MonoClass *class)
{
	static gboolean inited = FALSE;
	static int num_allocted = 0;
	static int num_bytes = 0;

	int size = sizeof (MonoRuntimeGenericContextTemplate);

	if (!inited) {
		mono_counters_register ("RGCTX template num allocted", MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &num_allocted);
		mono_counters_register ("RGCTX template bytes allocted", MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &num_bytes);
		inited = TRUE;
	}

	num_allocted++;
	num_bytes += size;

	return mono_image_alloc0 (class->image, size);
}

static MonoRuntimeGenericContextOtherInfoTemplate*
alloc_oti (MonoImage *image)
{
	static gboolean inited = FALSE;
	static int num_allocted = 0;
	static int num_bytes = 0;

	int size = sizeof (MonoRuntimeGenericContextOtherInfoTemplate);

	if (!inited) {
		mono_counters_register ("RGCTX oti num allocted", MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &num_allocted);
		mono_counters_register ("RGCTX oti bytes allocted", MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &num_bytes);
		inited = TRUE;
	}

	num_allocted++;
	num_bytes += size;

	return mono_image_alloc0 (image, size);
}

#define MONO_RGCTX_SLOT_USED_MARKER	((gpointer)&mono_defaults.object_class->byval_arg)

/*
 * LOCKING: loader lock
 */
static void
rgctx_template_set_other_slot (MonoImage *image, MonoRuntimeGenericContextTemplate *template, int type_argc,
	int slot, gpointer data, int info_type)
{
	static gboolean inited = FALSE;
	static int num_markers = 0;
	static int num_data = 0;

	int i;
	MonoRuntimeGenericContextOtherInfoTemplate *list = get_other_info_templates (template, type_argc);
	MonoRuntimeGenericContextOtherInfoTemplate **oti = &list;

	if (!inited) {
		mono_counters_register ("RGCTX oti num markers", MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &num_markers);
		mono_counters_register ("RGCTX oti num data", MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &num_data);
		inited = TRUE;
	}

	g_assert (slot >= 0);
	g_assert (data);

	i = 0;
	while (i <= slot) {
		if (i > 0)
			oti = &(*oti)->next;
		if (!*oti)
			*oti = alloc_oti (image);
		++i;
	}

	g_assert (!(*oti)->data);
	(*oti)->data = data;
	(*oti)->info_type = info_type;

	set_other_info_templates (image, template, type_argc, list);

	if (data == MONO_RGCTX_SLOT_USED_MARKER)
		++num_markers;
	else
		++num_data;
}

/*
 * mono_method_get_declaring_generic_method:
 * @method: an inflated method
 *
 * Returns an inflated method's declaring method.
 */
MonoMethod*
mono_method_get_declaring_generic_method (MonoMethod *method)
{
	MonoMethodInflated *inflated;

	g_assert (method->is_inflated);

	inflated = (MonoMethodInflated*)method;

	return inflated->declaring;
}

/*
 * mono_class_get_method_generic:
 * @klass: a class
 * @method: a method
 *
 * Given a class and a generic method, which has to be of an
 * instantiation of the same class that klass is an instantiation of,
 * returns the corresponding method in klass.  Example:
 *
 * klass is Gen<string>
 * method is Gen<object>.work<int>
 *
 * returns: Gen<string>.work<int>
 */
MonoMethod*
mono_class_get_method_generic (MonoClass *klass, MonoMethod *method)
{
	MonoMethod *declaring, *m;
	int i;

	if (method->is_inflated)
		declaring = mono_method_get_declaring_generic_method (method);
	else
		declaring = method;

	m = NULL;
	if (klass->generic_class)
		m = mono_class_get_inflated_method (klass, declaring);

	if (!m) {
		mono_class_setup_methods (klass);
		for (i = 0; i < klass->method.count; ++i) {
			m = klass->methods [i];
			if (m == declaring)
				break;
			if (m->is_inflated && mono_method_get_declaring_generic_method (m) == declaring)
				break;
		}
		if (i >= klass->method.count)
			return NULL;
	}

	if (method != declaring) {
		MonoGenericContext context;

		context.class_inst = NULL;
		context.method_inst = mono_method_get_context (method)->method_inst;

		m = mono_class_inflate_generic_method (m, &context);
	}

	return m;
}

static gpointer
inflate_other_data (gpointer data, int info_type, MonoGenericContext *context, MonoClass *class, gboolean temporary)
{
	g_assert (data);

	if (data == MONO_RGCTX_SLOT_USED_MARKER)
		return MONO_RGCTX_SLOT_USED_MARKER;

	switch (info_type)
	{
	case MONO_RGCTX_INFO_STATIC_DATA:
	case MONO_RGCTX_INFO_KLASS:
	case MONO_RGCTX_INFO_VTABLE:
	case MONO_RGCTX_INFO_TYPE:
	case MONO_RGCTX_INFO_REFLECTION_TYPE:
		return mono_class_inflate_generic_type_with_mempool (temporary ? NULL : class->image,
			data, context);

	case MONO_RGCTX_INFO_METHOD:
	case MONO_RGCTX_INFO_GENERIC_METHOD_CODE:
	case MONO_RGCTX_INFO_METHOD_RGCTX:
	case MONO_RGCTX_INFO_METHOD_CONTEXT:
	case MONO_RGCTX_INFO_REMOTING_INVOKE_WITH_CHECK: {
		MonoMethod *method = data;
		MonoMethod *inflated_method;
		MonoType *inflated_type = mono_class_inflate_generic_type (&method->klass->byval_arg, context);
		MonoClass *inflated_class = mono_class_from_mono_type (inflated_type);

		mono_metadata_free_type (inflated_type);

		mono_class_init (inflated_class);

		g_assert (!method->wrapper_type);

		if (inflated_class->byval_arg.type == MONO_TYPE_ARRAY ||
				inflated_class->byval_arg.type == MONO_TYPE_SZARRAY) {
			inflated_method = mono_method_search_in_array_class (inflated_class,
				method->name, method->signature);
		} else {
			inflated_method = mono_class_inflate_generic_method (method, context);
		}
		mono_class_init (inflated_method->klass);
		g_assert (inflated_method->klass == inflated_class);
		return inflated_method;
	}

	case MONO_RGCTX_INFO_CLASS_FIELD: {
		MonoClassField *field = data;
		MonoType *inflated_type = mono_class_inflate_generic_type (&field->parent->byval_arg, context);
		MonoClass *inflated_class = mono_class_from_mono_type (inflated_type);
		int i = field - field->parent->fields;
		gpointer dummy = NULL;

		mono_metadata_free_type (inflated_type);

		mono_class_get_fields (inflated_class, &dummy);
		g_assert (inflated_class->fields);

		return &inflated_class->fields [i];
	}

	default:
		g_assert_not_reached ();
	}
	/* Not reached, quiet compiler */
	return NULL;
}

static gpointer
inflate_other_info (MonoRuntimeGenericContextOtherInfoTemplate *oti,
	MonoGenericContext *context, MonoClass *class, gboolean temporary)
{
	return inflate_other_data (oti->data, oti->info_type, context, class, temporary);
}

static void
free_inflated_info (int info_type, gpointer info)
{
	if (!info)
		return;

	switch (info_type) {
	case MONO_RGCTX_INFO_STATIC_DATA:
	case MONO_RGCTX_INFO_KLASS:
	case MONO_RGCTX_INFO_VTABLE:
	case MONO_RGCTX_INFO_TYPE:
	case MONO_RGCTX_INFO_REFLECTION_TYPE:
		mono_metadata_free_type (info);
		break;
	default:
		break;
	}
}

static MonoRuntimeGenericContextOtherInfoTemplate
class_get_rgctx_template_oti (MonoClass *class, int type_argc, guint32 slot, gboolean temporary, gboolean *do_free);

/*
 * mono_class_get_runtime_generic_context_template:
 * @class: a class
 *
 * Looks up or constructs, if necessary, the runtime generic context
 * for class.
 */
static MonoRuntimeGenericContextTemplate*
mono_class_get_runtime_generic_context_template (MonoClass *class)
{
	MonoRuntimeGenericContextTemplate *parent_template, *template;
	guint32 i;

	g_assert (!class->generic_class);

	mono_loader_lock ();
	template = class_lookup_rgctx_template (class);
	mono_loader_unlock ();

	if (template)
		return template;

	template = alloc_template (class);

	mono_loader_lock ();

	if (class->parent) {
		if (class->parent->generic_class) {
			guint32 num_entries;
			int max_argc, type_argc;

			parent_template = mono_class_get_runtime_generic_context_template
				(class->parent->generic_class->container_class);

			max_argc = template_get_max_argc (parent_template);

			for (type_argc = 0; type_argc <= max_argc; ++type_argc) {
				num_entries = rgctx_template_num_other_infos (parent_template, type_argc);

				/* FIXME: quadratic! */
				for (i = 0; i < num_entries; ++i) {
					MonoRuntimeGenericContextOtherInfoTemplate oti;

					oti = class_get_rgctx_template_oti (class->parent, type_argc, i, FALSE, NULL);
					if (oti.data && oti.data != MONO_RGCTX_SLOT_USED_MARKER) {
						rgctx_template_set_other_slot (class->image, template, type_argc, i,
							oti.data, oti.info_type);
					}
				}
			}
		} else {
			MonoRuntimeGenericContextOtherInfoTemplate *oti;
			int max_argc, type_argc;

			parent_template = mono_class_get_runtime_generic_context_template (class->parent);

			max_argc = template_get_max_argc (parent_template);

			for (type_argc = 0; type_argc <= max_argc; ++type_argc) {
				/* FIXME: quadratic! */
				for (i = 0, oti = parent_template->other_infos; oti; ++i, oti = oti->next) {
					if (oti->data && oti->data != MONO_RGCTX_SLOT_USED_MARKER) {
						rgctx_template_set_other_slot (class->image, template, type_argc, i,
							oti->data, oti->info_type);
					}
				}
			}
		}
	}

	if (class_lookup_rgctx_template (class)) {
		/* some other thread already set the template */
		template = class_lookup_rgctx_template (class);
	} else {
		class_set_rgctx_template (class, template);

		if (class->parent)
			register_generic_subclass (class);
	}

	mono_loader_unlock ();

	return template;
}

/*
 * temporary signifies whether the inflated info (oti.data) will be
 * used temporarily, in which case it might be heap-allocated, or
 * permanently, in which case it will be mempool-allocated.  If
 * temporary is set then *do_free will return whether the returned
 * data must be freed.
 *
 * LOCKING: loader lock
 */
static MonoRuntimeGenericContextOtherInfoTemplate
class_get_rgctx_template_oti (MonoClass *class, int type_argc, guint32 slot, gboolean temporary, gboolean *do_free)
{
	g_assert ((temporary && do_free) || (!temporary && !do_free));

	if (class->generic_class) {
		MonoRuntimeGenericContextOtherInfoTemplate oti;
		gboolean tmp_do_free;

		oti = class_get_rgctx_template_oti (class->generic_class->container_class,
			type_argc, slot, TRUE, &tmp_do_free);
		if (oti.data) {
			gpointer info = oti.data;
			oti.data = inflate_other_info (&oti, &class->generic_class->context, class, temporary);
			if (tmp_do_free)
				free_inflated_info (oti.info_type, info);
		}
		if (temporary)
			*do_free = TRUE;

		return oti;
	} else {
		MonoRuntimeGenericContextTemplate *template;
		MonoRuntimeGenericContextOtherInfoTemplate *oti;

		template = mono_class_get_runtime_generic_context_template (class);
		oti = rgctx_template_get_other_slot (template, type_argc, slot);
		g_assert (oti);

		if (temporary)
			*do_free = FALSE;

		return *oti;
	}
}

static MonoClass*
class_uninstantiated (MonoClass *class)
{
	if (class->generic_class)
		return class->generic_class->container_class;
	return class;
}

static gpointer
class_type_info (MonoDomain *domain, MonoClass *class, int info_type)
{
	switch (info_type) {
	case MONO_RGCTX_INFO_STATIC_DATA:
		return mono_class_vtable (domain, class)->data;
	case MONO_RGCTX_INFO_KLASS:
		return class;
	case MONO_RGCTX_INFO_VTABLE:
		return mono_class_vtable (domain, class);
	default:
		g_assert_not_reached ();
	}
	/* Not reached */
	return NULL;
}

static gpointer
instantiate_other_info (MonoDomain *domain, MonoRuntimeGenericContextOtherInfoTemplate *oti,
	MonoGenericContext *context, MonoClass *class)
{
	gpointer data;
	gboolean temporary;

	if (!oti->data)
		return NULL;

	switch (oti->info_type) {
	case MONO_RGCTX_INFO_STATIC_DATA:
	case MONO_RGCTX_INFO_KLASS:
	case MONO_RGCTX_INFO_VTABLE:
		temporary = TRUE;
		break;
	default:
		temporary = FALSE;
	}

	data = inflate_other_info (oti, context, class, temporary);

	switch (oti->info_type) {
	case MONO_RGCTX_INFO_STATIC_DATA:
	case MONO_RGCTX_INFO_KLASS:
	case MONO_RGCTX_INFO_VTABLE: {
		MonoClass *arg_class = mono_class_from_mono_type (data);

		free_inflated_info (oti->info_type, data);
		g_assert (arg_class);

		/* The class might be used as an argument to
		   mono_value_copy(), which requires that its GC
		   descriptor has been computed. */
		if (oti->info_type == MONO_RGCTX_INFO_KLASS)
			mono_class_compute_gc_descriptor (arg_class);

		return class_type_info (domain, arg_class, oti->info_type);
	}
	case MONO_RGCTX_INFO_TYPE:
		return data;
	case MONO_RGCTX_INFO_REFLECTION_TYPE:
		return mono_type_get_object (domain, data);
	case MONO_RGCTX_INFO_METHOD:
		return data;
	case MONO_RGCTX_INFO_GENERIC_METHOD_CODE:
		return mono_create_ftnptr (mono_domain_get (),
				mono_runtime_create_jump_trampoline (mono_domain_get (), data, TRUE));
	case MONO_RGCTX_INFO_REMOTING_INVOKE_WITH_CHECK:
		return mono_create_ftnptr (mono_domain_get (),
				mono_runtime_create_jump_trampoline (mono_domain_get (),
						mono_marshal_get_remoting_invoke_with_check (data), TRUE));
	case MONO_RGCTX_INFO_CLASS_FIELD:
		return data;
	case MONO_RGCTX_INFO_METHOD_RGCTX: {
		MonoMethodInflated *method = data;

		g_assert (method->method.method.is_inflated);
		g_assert (method->context.method_inst);

		return mono_method_lookup_rgctx (mono_class_vtable (domain, method->method.method.klass),
			method->context.method_inst);
	}
	case MONO_RGCTX_INFO_METHOD_CONTEXT: {
		MonoMethodInflated *method = data;

		g_assert (method->method.method.is_inflated);
		g_assert (method->context.method_inst);

		return method->context.method_inst;
	}
	default:
		g_assert_not_reached ();
	}
	/* Not reached */
	return NULL;
}

/*
 * LOCKING: loader lock
 */
static void
fill_in_rgctx_template_slot (MonoClass *class, int type_argc, int index, gpointer data, int info_type)
{
	MonoRuntimeGenericContextTemplate *template = mono_class_get_runtime_generic_context_template (class);
	MonoClass *subclass;

	g_assert (!class->generic_class);

	rgctx_template_set_other_slot (class->image, template, type_argc, index, data, info_type);

	/* Recurse for all subclasses */
	if (generic_subclass_hash)
		subclass = g_hash_table_lookup (generic_subclass_hash, class);
	else
		subclass = NULL;

	while (subclass) {
		MonoRuntimeGenericContextOtherInfoTemplate subclass_oti;
		MonoRuntimeGenericContextTemplate *subclass_template = class_lookup_rgctx_template (subclass);

		g_assert (!subclass->generic_class);
		g_assert (subclass_template);

		subclass_oti = class_get_rgctx_template_oti (subclass->parent, type_argc, index, FALSE, NULL);
		g_assert (subclass_oti.data);

		fill_in_rgctx_template_slot (subclass, type_argc, index, subclass_oti.data, info_type);

		subclass = subclass_template->next_subclass;
	}
}

/*
 * LOCKING: loader lock
 */
static int
register_other_info (MonoClass *class, int type_argc, gpointer data, int info_type)
{
	int i;
	MonoRuntimeGenericContextTemplate *template = mono_class_get_runtime_generic_context_template (class);
	MonoClass *parent;
	MonoRuntimeGenericContextOtherInfoTemplate *oti;

	for (i = 0, oti = get_other_info_templates (template, type_argc); oti; ++i, oti = oti->next) {
		if (!oti->data)
			break;
	}

	//g_print ("template %s . other_infos [%d] = %s\n", mono_type_get_full_name (class), i, mono_type_get_full_name (other_class));

	/* Mark the slot as used in all parent classes (until we find
	   a parent class which already has it marked used). */
	parent = class->parent;
	while (parent != NULL) {
		MonoRuntimeGenericContextTemplate *parent_template;
		MonoRuntimeGenericContextOtherInfoTemplate *oti;

		if (parent->generic_class)
			parent = parent->generic_class->container_class;

		parent_template = mono_class_get_runtime_generic_context_template (parent);
		oti = rgctx_template_get_other_slot (parent_template, type_argc, i);

		if (oti && oti->data)
			break;

		rgctx_template_set_other_slot (parent->image, parent_template, type_argc, i,
				MONO_RGCTX_SLOT_USED_MARKER, 0);

		parent = parent->parent;
	}

	/* Fill in the slot in this class and in all subclasses
	   recursively. */
	fill_in_rgctx_template_slot (class, type_argc, i, data, info_type);

	return i;
}

static gboolean
other_info_equal (gpointer data1, gpointer data2, int info_type)
{
	switch (info_type) {
	case MONO_RGCTX_INFO_STATIC_DATA:
	case MONO_RGCTX_INFO_KLASS:
	case MONO_RGCTX_INFO_VTABLE:
	case MONO_RGCTX_INFO_TYPE:
	case MONO_RGCTX_INFO_REFLECTION_TYPE:
		return mono_class_from_mono_type (data1) == mono_class_from_mono_type (data2);
	case MONO_RGCTX_INFO_METHOD:
	case MONO_RGCTX_INFO_GENERIC_METHOD_CODE:
	case MONO_RGCTX_INFO_CLASS_FIELD:
	case MONO_RGCTX_INFO_METHOD_RGCTX:
	case MONO_RGCTX_INFO_METHOD_CONTEXT:
	case MONO_RGCTX_INFO_REMOTING_INVOKE_WITH_CHECK:
		return data1 == data2;
	default:
		g_assert_not_reached ();
	}
	/* never reached */
	return FALSE;
}

static int
lookup_or_register_other_info (MonoClass *class, int type_argc, gpointer data, int info_type,
	MonoGenericContext *generic_context)
{
	static gboolean inited = FALSE;
	static int max_slot = 0;

	MonoRuntimeGenericContextTemplate *rgctx_template =
		mono_class_get_runtime_generic_context_template (class);
	MonoRuntimeGenericContextOtherInfoTemplate *oti_list, *oti;
	int i;

	g_assert (!class->generic_class);
	g_assert (class->generic_container || type_argc);

	mono_loader_lock ();

	oti_list = get_other_info_templates (rgctx_template, type_argc);

	for (oti = oti_list, i = 0; oti; oti = oti->next, ++i) {
		gpointer inflated_data;

		if (oti->info_type != info_type || !oti->data)
			continue;

		inflated_data = inflate_other_info (oti, generic_context, class, TRUE);

		if (other_info_equal (data, inflated_data, info_type)) {
			free_inflated_info (info_type, inflated_data);
			mono_loader_unlock ();
			return i;
		}
		free_inflated_info (info_type, inflated_data);
	}

	/* We haven't found the info */
	i = register_other_info (class, type_argc, data, info_type);

	mono_loader_unlock ();

	if (!inited) {
		mono_counters_register ("RGCTX max slot number", MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &max_slot);
		inited = TRUE;
	}
	if (i > max_slot)
		max_slot = i;

	return i;
}

/*
 * mono_method_lookup_or_register_other_info:
 * @method: a method
 * @in_mrgctx: whether to put the data into the MRGCTX
 * @data: the info data
 * @info_type: the type of info to register about data
 * @generic_context: a generic context
 *
 * Looks up and, if necessary, adds information about other_class in
 * method's or method's class runtime generic context.  Returns the
 * encoded slot number.
 */
guint32
mono_method_lookup_or_register_other_info (MonoMethod *method, gboolean in_mrgctx, gpointer data,
	int info_type, MonoGenericContext *generic_context)
{
	MonoClass *class = method->klass;
	int type_argc, index;

	if (in_mrgctx) {
		MonoGenericInst *method_inst = mono_method_get_context (method)->method_inst;

		g_assert (method->is_inflated && method_inst);
		type_argc = method_inst->type_argc;
		g_assert (type_argc > 0);
	} else {
		type_argc = 0;
	}

	index = lookup_or_register_other_info (class, type_argc, data, info_type, generic_context);

	//g_print ("rgctx item at index %d argc %d\n", index, type_argc);

	if (in_mrgctx)
		return MONO_RGCTX_SLOT_MAKE_MRGCTX (index);
	else
		return MONO_RGCTX_SLOT_MAKE_RGCTX (index);
}

/*
 * mono_class_rgctx_get_array_size:
 * @n: The number of the array
 * @mrgctx: Whether it's an MRGCTX as opposed to a RGCTX.
 *
 * Returns the number of slots in the n'th array of a (M)RGCTX.  That
 * number includes the slot for linking and - for MRGCTXs - the two
 * slots in the first array for additional information.
 */
int
mono_class_rgctx_get_array_size (int n, gboolean mrgctx)
{
	g_assert (n >= 0 && n < 30);

	if (mrgctx)
		return 6 << n;
	else
		return 4 << n;
}

/*
 * LOCKING: domain lock
 */
static gpointer*
alloc_rgctx_array (MonoDomain *domain, int n, gboolean is_mrgctx)
{
	static gboolean inited = FALSE;
	static int rgctx_num_alloced = 0;
	static int rgctx_bytes_alloced = 0;
	static int mrgctx_num_alloced = 0;
	static int mrgctx_bytes_alloced = 0;

	int size = mono_class_rgctx_get_array_size (n, is_mrgctx) * sizeof (gpointer);
	gpointer array = mono_domain_alloc0 (domain, size);

	if (!inited) {
		mono_counters_register ("RGCTX num arrays alloced", MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &rgctx_num_alloced);
		mono_counters_register ("RGCTX bytes alloced", MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &rgctx_bytes_alloced);
		mono_counters_register ("MRGCTX num arrays alloced", MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &mrgctx_num_alloced);
		mono_counters_register ("MRGCTX bytes alloced", MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &mrgctx_bytes_alloced);
		inited = TRUE;
	}

	if (is_mrgctx) {
		mrgctx_num_alloced++;
		mrgctx_bytes_alloced += size;
	} else {
		rgctx_num_alloced++;
		rgctx_bytes_alloced += size;
	}

	return array;
}

static gpointer
fill_runtime_generic_context (MonoVTable *class_vtable, MonoRuntimeGenericContext *rgctx, guint32 slot,
		MonoGenericInst *method_inst)
{
	gpointer info;
	int i, first_slot, size;
	MonoDomain *domain = class_vtable->domain;
	MonoClass *class = class_vtable->klass;
	MonoGenericContext *class_context = class->generic_class ? &class->generic_class->context : NULL;
	MonoRuntimeGenericContextOtherInfoTemplate oti;
	MonoGenericContext context = { class_context ? class_context->class_inst : NULL, method_inst };
	int rgctx_index;
	gboolean do_free;

	g_assert (rgctx);

	mono_domain_lock (domain);

	/* First check whether that slot isn't already instantiated.
	   This might happen because lookup doesn't lock.  Allocate
	   arrays on the way. */
	first_slot = 0;
	size = mono_class_rgctx_get_array_size (0, method_inst != NULL);
	if (method_inst)
		size -= sizeof (MonoMethodRuntimeGenericContext) / sizeof (gpointer);
	for (i = 0; ; ++i) {
		int offset;

		if (method_inst && i == 0)
			offset = sizeof (MonoMethodRuntimeGenericContext) / sizeof (gpointer);
		else
			offset = 0;

		if (slot < first_slot + size - 1) {
			rgctx_index = slot - first_slot + 1 + offset;
			info = rgctx [rgctx_index];
			if (info) {
				mono_domain_unlock (domain);
				return info;
			}
			break;
		}
		if (!rgctx [offset + 0])
			rgctx [offset + 0] = alloc_rgctx_array (domain, i + 1, method_inst != NULL);
		rgctx = rgctx [offset + 0];
		first_slot += size - 1;
		size = mono_class_rgctx_get_array_size (i + 1, method_inst != NULL);
	}

	g_assert (!rgctx [rgctx_index]);

	mono_domain_unlock (domain);

	oti = class_get_rgctx_template_oti (class_uninstantiated (class),
			method_inst ? method_inst->type_argc : 0, slot, TRUE, &do_free);
	/* This might take the loader lock */
	info = instantiate_other_info (domain, &oti, &context, class);

	/*
	if (method_inst)
		g_print ("filling mrgctx slot %d table %d index %d\n", slot, i, rgctx_index);
	*/

	/*FIXME We should use CAS here, no need to take a lock.*/
	mono_domain_lock (domain);

	/* Check whether the slot hasn't been instantiated in the
	   meantime. */
	if (rgctx [rgctx_index])
		info = rgctx [rgctx_index];
	else
		rgctx [rgctx_index] = info;

	mono_domain_unlock (domain);

	if (do_free)
		free_inflated_info (oti.info_type, oti.data);

	return info;
}

/*
 * mono_class_fill_runtime_generic_context:
 * @class_vtable: a vtable
 * @slot: a slot index to be instantiated
 *
 * Instantiates a slot in the RGCTX.
 */
gpointer
mono_class_fill_runtime_generic_context (MonoVTable *class_vtable, guint32 slot)
{
	static gboolean inited = FALSE;
	static int num_alloced = 0;

	MonoDomain *domain = class_vtable->domain;
	MonoRuntimeGenericContext *rgctx;
	gpointer info;

	mono_domain_lock (domain);

	if (!inited) {
		mono_counters_register ("RGCTX num alloced", MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &num_alloced);
		inited = TRUE;
	}

	rgctx = class_vtable->runtime_generic_context;
	if (!rgctx) {
		rgctx = alloc_rgctx_array (domain, 0, FALSE);
		class_vtable->runtime_generic_context = rgctx;
		num_alloced++;
	}

	mono_domain_unlock (domain);

	info = fill_runtime_generic_context (class_vtable, rgctx, slot, 0);

	return info;
}

/*
 * mono_method_fill_runtime_generic_context:
 * @mrgctx: an MRGCTX
 * @slot: a slot index to be instantiated
 *
 * Instantiates a slot in the MRGCTX.
 */
gpointer
mono_method_fill_runtime_generic_context (MonoMethodRuntimeGenericContext *mrgctx, guint32 slot)
{
	gpointer info;

	info = fill_runtime_generic_context (mrgctx->class_vtable, (MonoRuntimeGenericContext*)mrgctx, slot,
		mrgctx->method_inst);

	return info;
}

static guint
mrgctx_hash_func (gconstpointer key)
{
	const MonoMethodRuntimeGenericContext *mrgctx = key;

	return mono_aligned_addr_hash (mrgctx->class_vtable) ^ mono_metadata_generic_inst_hash (mrgctx->method_inst);
}

static gboolean
mrgctx_equal_func (gconstpointer a, gconstpointer b)
{
	const MonoMethodRuntimeGenericContext *mrgctx1 = a;
	const MonoMethodRuntimeGenericContext *mrgctx2 = b;

	return mrgctx1->class_vtable == mrgctx2->class_vtable &&
		mono_metadata_generic_inst_equal (mrgctx1->method_inst, mrgctx2->method_inst);
}

/*
 * mono_method_lookup_rgctx:
 * @class_vtable: a vtable
 * @method_inst: the method inst of a generic method
 *
 * Returns the MRGCTX for the generic method(s) with the given
 * method_inst of the given class_vtable.
 *
 * LOCKING: Take the domain lock.
 */
MonoMethodRuntimeGenericContext*
mono_method_lookup_rgctx (MonoVTable *class_vtable, MonoGenericInst *method_inst)
{
	MonoDomain *domain = class_vtable->domain;
	MonoMethodRuntimeGenericContext *mrgctx;
	MonoMethodRuntimeGenericContext key;

	g_assert (!class_vtable->klass->generic_container);
	g_assert (!method_inst->is_open);

	mono_domain_lock (domain);
	if (!domain->method_rgctx_hash)
		domain->method_rgctx_hash = g_hash_table_new (mrgctx_hash_func, mrgctx_equal_func);

	key.class_vtable = class_vtable;
	key.method_inst = method_inst;

	mrgctx = g_hash_table_lookup (domain->method_rgctx_hash, &key);

	if (!mrgctx) {
		//int i;

		mrgctx = (MonoMethodRuntimeGenericContext*)alloc_rgctx_array (domain, 0, TRUE);
		mrgctx->class_vtable = class_vtable;
		mrgctx->method_inst = method_inst;

		g_hash_table_insert (domain->method_rgctx_hash, mrgctx, mrgctx);

		/*
		g_print ("mrgctx alloced for %s <", mono_type_get_full_name (class_vtable->klass));
		for (i = 0; i < method_inst->type_argc; ++i)
			g_print ("%s, ", mono_type_full_name (method_inst->type_argv [i]));
		g_print (">\n");
		*/
	}

	mono_domain_unlock (domain);

	g_assert (mrgctx);

	return mrgctx;
}

static gboolean
generic_inst_is_sharable (MonoGenericInst *inst, gboolean allow_type_vars)
{
	int i;

	for (i = 0; i < inst->type_argc; ++i) {
		MonoType *type = inst->type_argv [i];
		int type_type;

		if (MONO_TYPE_IS_REFERENCE (type))
			continue;

		type_type = mono_type_get_type (type);
		if (allow_type_vars && (type_type == MONO_TYPE_VAR || type_type == MONO_TYPE_MVAR))
			continue;

		return FALSE;
	}

	return TRUE;
}

/*
 * mono_generic_context_is_sharable:
 * @context: a generic context
 *
 * Returns whether the generic context is sharable.  A generic context
 * is sharable iff all of its type arguments are reference type.
 */
gboolean
mono_generic_context_is_sharable (MonoGenericContext *context, gboolean allow_type_vars)
{
	g_assert (context->class_inst || context->method_inst);

	if (context->class_inst && !generic_inst_is_sharable (context->class_inst, allow_type_vars))
		return FALSE;

	if (context->method_inst && !generic_inst_is_sharable (context->method_inst, allow_type_vars))
		return FALSE;

	return TRUE;
}

/*
 * mono_method_is_generic_impl:
 * @method: a method
 *
 * Returns whether the method is either generic or part of a generic
 * class.
 */
gboolean
mono_method_is_generic_impl (MonoMethod *method)
{
	if (method->is_inflated) {
		g_assert (method->wrapper_type == MONO_WRAPPER_NONE);
		return TRUE;
	}
	/* We don't treat wrappers as generic code, i.e., we never
	   apply generic sharing to them.  This is especially
	   important for static rgctx invoke wrappers, which only work
	   if not compiled with sharing. */
	if (method->wrapper_type != MONO_WRAPPER_NONE)
		return FALSE;
	if (method->klass->generic_container)
		return TRUE;
	return FALSE;
}

static gboolean
has_constraints (MonoGenericContainer *container)
{
	//int i;

	return FALSE;
	/*
	g_assert (container->type_argc > 0);
	g_assert (container->type_params);

	for (i = 0; i < container->type_argc; ++i)
		if (container->type_params [i].constraints)
			return TRUE;
	return FALSE;
	*/
}

/*
 * mono_method_is_generic_sharable_impl:
 * @method: a method
 * @allow_type_vars: whether to regard type variables as reference types
 *
 * Returns TRUE iff the method is inflated or part of an inflated
 * class, its context is sharable and it has no constraints on its
 * type parameters.  Otherwise returns FALSE.
 */
gboolean
mono_method_is_generic_sharable_impl (MonoMethod *method, gboolean allow_type_vars)
{
	if (!mono_method_is_generic_impl (method))
		return FALSE;

	if (method->is_inflated) {
		MonoMethodInflated *inflated = (MonoMethodInflated*)method;
		MonoGenericContext *context = &inflated->context;

		if (!mono_generic_context_is_sharable (context, allow_type_vars))
			return FALSE;

		g_assert (inflated->declaring);

		if (inflated->declaring->is_generic) {
			if (has_constraints (mono_method_get_generic_container (inflated->declaring)))
				return FALSE;
		}
	}

	if (method->klass->generic_class) {
		if (!mono_generic_context_is_sharable (&method->klass->generic_class->context, allow_type_vars))
			return FALSE;

		g_assert (method->klass->generic_class->container_class &&
				method->klass->generic_class->container_class->generic_container);

		if (has_constraints (method->klass->generic_class->container_class->generic_container))
			return FALSE;
	}

	if (method->klass->generic_container && !allow_type_vars)
		return FALSE;

	return TRUE;
}

gboolean
mono_method_needs_static_rgctx_invoke (MonoMethod *method, gboolean allow_type_vars)
{
	if (!mono_class_generic_sharing_enabled (method->klass))
		return FALSE;

	if (!mono_method_is_generic_sharable_impl (method, allow_type_vars))
		return FALSE;

	if (method->is_inflated && mono_method_get_context (method)->method_inst)
		return TRUE;

	return ((method->flags & METHOD_ATTRIBUTE_STATIC) ||
			method->klass->valuetype) &&
		(method->klass->generic_class || method->klass->generic_container);
}

static MonoGenericInst*
get_object_generic_inst (int type_argc)
{
	MonoType **type_argv;
	int i;

	type_argv = alloca (sizeof (MonoType*) * type_argc);

	for (i = 0; i < type_argc; ++i)
		type_argv [i] = &mono_defaults.object_class->byval_arg;

	return mono_metadata_get_generic_inst (type_argc, type_argv);
}

/*
 * mono_method_construct_object_context:
 * @method: a method
 *
 * Returns a generic context for method with all type variables for
 * class and method instantiated with Object.
 */
MonoGenericContext
mono_method_construct_object_context (MonoMethod *method)
{
	MonoGenericContext object_context;

	g_assert (!method->klass->generic_class);
	if (method->klass->generic_container) {
		int type_argc = method->klass->generic_container->type_argc;

		object_context.class_inst = get_object_generic_inst (type_argc);
	} else {
		object_context.class_inst = NULL;
	}

	if (mono_method_get_context_general (method, TRUE)->method_inst) {
		int type_argc = mono_method_get_context_general (method, TRUE)->method_inst->type_argc;

		object_context.method_inst = get_object_generic_inst (type_argc);
	} else {
		object_context.method_inst = NULL;
	}

	g_assert (object_context.class_inst || object_context.method_inst);

	return object_context;
}

/*
 * mono_domain_lookup_shared_generic:
 * @domain: a domain
 * @open_method: an open generic method
 *
 * Looks up the jit info for method via the domain's jit code hash.
 */
MonoJitInfo*
mono_domain_lookup_shared_generic (MonoDomain *domain, MonoMethod *open_method)
{
	static gboolean inited = FALSE;
	static int lookups = 0;
	static int failed_lookups = 0;

	MonoGenericContext object_context;
	MonoMethod *object_method;
	MonoJitInfo *ji;

	object_context = mono_method_construct_object_context (open_method);
	object_method = mono_class_inflate_generic_method (open_method, &object_context);

	mono_domain_jit_code_hash_lock (domain);
	ji = mono_internal_hash_table_lookup (&domain->jit_code_hash, object_method);
	if (ji && !ji->has_generic_jit_info)
		ji = NULL;
	mono_domain_jit_code_hash_unlock (domain);

	if (!inited) {
		mono_counters_register ("Shared generic lookups", MONO_COUNTER_INT|MONO_COUNTER_GENERICS, &lookups);
		mono_counters_register ("Failed shared generic lookups", MONO_COUNTER_INT|MONO_COUNTER_GENERICS, &failed_lookups);
		inited = TRUE;
	}

	++lookups;
	if (!ji)
		++failed_lookups;

	return ji;
}
