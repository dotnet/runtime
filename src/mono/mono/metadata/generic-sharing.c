/*
 * generic-sharing.c: Support functions for generic sharing.
 *
 * Author:
 *   Mark Probst (mark.probst@gmail.com)
 *
 * (C) 2007-2008 Novell, Inc.
 */

#include <config.h>
#include <string.h>

#ifdef _MSC_VER
#include <glib.h>
#endif
#include <mono/utils/mono-membar.h>
#include <mono/utils/mono-counters.h>

#include "metadata-internals.h"
#include "class.h"
#include "class-internals.h"
#include "marshal.h"

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
 * Guards the two global rgctx (template) hash tables and all rgctx
 * templates.
 */
static CRITICAL_SECTION templates_mutex;

static void
templates_lock (void)
{
	static gboolean inited = FALSE;

	if (!inited) {
		mono_loader_lock ();
		if (!inited) {
			InitializeCriticalSection (&templates_mutex);
			inited = TRUE;
		}
		mono_loader_unlock ();
	}

	EnterCriticalSection (&templates_mutex);
}

static void
templates_unlock (void)
{
	LeaveCriticalSection (&templates_mutex);
}

/*
 * LOCKING: templates lock
 */
static MonoRuntimeGenericContextOtherInfoTemplate*
rgctx_template_get_other_slot (MonoRuntimeGenericContextTemplate *template, int slot)
{
	int i;
	MonoRuntimeGenericContextOtherInfoTemplate *oti;

	g_assert (slot >= 0);

	for (oti = template->other_infos, i = 0; i < slot; oti = oti->next, ++i) {
		if (!oti)
			return NULL;
	}

	return oti;
}

/*
 * LOCKING: templates lock
 */
static int
rgctx_template_num_other_infos (MonoRuntimeGenericContextTemplate *template)
{
	MonoRuntimeGenericContextOtherInfoTemplate *oti;
	int i;

	for (i = 0, oti = template->other_infos; oti; ++i, oti = oti->next)
		;

	return i;
}

/* Maps from uninstantiated generic classes to GList's of
 * uninstantiated generic classes whose parent is the key class or an
 * instance of the key class.
 *
 * LOCKING: templates lock
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
 * LOCKING: templates lock
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
 * LOCKING: templates lock
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

	templates_lock ();

	old_hash = generic_subclass_hash;
	generic_subclass_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);

	g_hash_table_foreach (old_hash, (GHFunc)move_subclasses_not_in_image_foreach_func, image);

	templates_unlock ();

	g_hash_table_destroy (old_hash);
}

/*
 * LOCKING: loader lock
 */
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

	return mono_mempool_alloc0 (class->image->mempool, size);
}

/*
 * LOCKING: loader lock
 */
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

	return mono_mempool_alloc0 (image->mempool, size);
}

/*
 * LOCKING: templates lock
 */
static void
rgctx_template_set_other_slot (MonoImage *image, MonoRuntimeGenericContextTemplate *template, int slot,
	gpointer data, int info_type)
{
	int i;
	MonoRuntimeGenericContextOtherInfoTemplate **oti;

	g_assert (slot >= 0);
	g_assert (data);

	mono_loader_lock ();

	oti = &template->other_infos;
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

	mono_loader_unlock ();
}

#define MONO_RGCTX_SLOT_USED_MARKER	((gpointer)&mono_defaults.object_class->byval_arg)

static gpointer
inflate_other_data (gpointer data, int info_type, MonoGenericContext *context)
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
		return mono_class_inflate_generic_type (data, context);

	case MONO_RGCTX_INFO_METHOD:
	case MONO_RGCTX_INFO_GENERIC_METHOD_CODE: {
		MonoMethod *method = data;
		MonoMethod *inflated_method;

		if (method->wrapper_type != MONO_WRAPPER_NONE) {
			g_assert (method->wrapper_type == MONO_WRAPPER_STATIC_RGCTX_INVOKE);

			method = mono_marshal_method_from_wrapper (method);
			method = mono_class_inflate_generic_method (method, context);
			method = mono_marshal_get_static_rgctx_invoke (method);
		}

		inflated_method = mono_class_inflate_generic_method (method, context);
		mono_class_init (inflated_method->klass);
		return inflated_method;
	}

	case MONO_RGCTX_INFO_CLASS_FIELD: {
		MonoClassField *field = data;
		MonoType *inflated_type = mono_class_inflate_generic_type (&field->parent->byval_arg, context);
		MonoClass *inflated_class = mono_class_from_mono_type (inflated_type);
		int i = field - field->parent->fields;
		gpointer dummy;

		mono_class_get_fields (inflated_class, &dummy);
		g_assert (inflated_class->fields);

		return &inflated_class->fields [i];
	}

	default:
		g_assert_not_reached ();
	}
}

static gpointer
inflate_other_info (MonoRuntimeGenericContextOtherInfoTemplate *oti, MonoGenericContext *context)
{
	return inflate_other_data (oti->data, oti->info_type, context);
}

static MonoRuntimeGenericContextOtherInfoTemplate
class_get_rgctx_template_oti (MonoClass *class, guint32 slot);

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
	MonoGenericInst *inst;
	guint32 i;

	g_assert (!class->generic_class);

	templates_lock ();
	template = class_lookup_rgctx_template (class);
	templates_unlock ();

	if (template)
		return template;

	if (class->generic_container)
		inst = class->generic_container->context.class_inst;
	else
		inst = NULL;

	mono_loader_lock ();
	template = alloc_template (class);
	mono_loader_unlock ();

	if (class->parent) {
		if (class->parent->generic_class) {
			guint32 num_entries;

			parent_template = mono_class_get_runtime_generic_context_template
				(class->parent->generic_class->container_class);
			num_entries = rgctx_template_num_other_infos (parent_template);

			mono_loader_lock ();

			/* FIXME: quadratic! */
			for (i = 0; i < num_entries; ++i) {
				MonoRuntimeGenericContextOtherInfoTemplate oti;

				oti = class_get_rgctx_template_oti (class->parent, i);
				if (oti.data && oti.data != MONO_RGCTX_SLOT_USED_MARKER)
					rgctx_template_set_other_slot (class->image, template, i, oti.data, oti.info_type);
			}

			mono_loader_unlock ();
		} else {
			MonoRuntimeGenericContextOtherInfoTemplate *oti;

			parent_template = mono_class_get_runtime_generic_context_template (class->parent);

			mono_loader_lock ();

			/* FIXME: quadratic! */
			for (i = 0, oti = parent_template->other_infos; oti; ++i, oti = oti->next) {
				if (oti->data && oti->data != MONO_RGCTX_SLOT_USED_MARKER)
					rgctx_template_set_other_slot (class->image, template, i, oti->data, oti->info_type);
			}

			mono_loader_unlock ();
		}
	}

	templates_lock ();

	if (class_lookup_rgctx_template (class)) {
		/* some other thread already set the template */
		template = class_lookup_rgctx_template (class);
	} else {
		class_set_rgctx_template (class, template);

		if (class->parent)
			register_generic_subclass (class);
	}

	templates_unlock ();

	return template;
}

static MonoRuntimeGenericContextOtherInfoTemplate
class_get_rgctx_template_oti (MonoClass *class, guint32 slot)
{
	if (class->generic_class) {
		MonoRuntimeGenericContextOtherInfoTemplate oti;

		oti = class_get_rgctx_template_oti (class->generic_class->container_class, slot);
		if (oti.data)
			oti.data = inflate_other_info (&oti, &class->generic_class->context);

		return oti;
	} else {
		MonoRuntimeGenericContextTemplate *template;
		MonoRuntimeGenericContextOtherInfoTemplate *oti;
		guint32 i;

		template = mono_class_get_runtime_generic_context_template (class);

		for (i = 0, oti = template->other_infos; oti; ++i, oti = oti->next) {
			if (i == slot)
				break;
		}
		g_assert (i == slot && oti);

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
}

static gpointer
instantiate_other_info (MonoDomain *domain, MonoRuntimeGenericContextOtherInfoTemplate *oti, MonoGenericContext *context)
{
	gpointer data;

	if (!oti->data)
		return NULL;

	data = inflate_other_info (oti, context);

	switch (oti->info_type) {
	case MONO_RGCTX_INFO_STATIC_DATA:
	case MONO_RGCTX_INFO_KLASS:
	case MONO_RGCTX_INFO_VTABLE: {
		MonoClass *arg_class = mono_class_from_mono_type (data);

		g_assert (arg_class);

		return class_type_info (domain, arg_class, oti->info_type);
	}
	case MONO_RGCTX_INFO_TYPE:
		return data;
	case MONO_RGCTX_INFO_REFLECTION_TYPE:
		return mono_type_get_object (domain, data);
	case MONO_RGCTX_INFO_METHOD:
		return data;
	case MONO_RGCTX_INFO_GENERIC_METHOD_CODE:
		return mono_compile_method (data);
	case MONO_RGCTX_INFO_CLASS_FIELD:
		return data;
	default:
		g_assert_not_reached ();
	}
}

/*
 * LOCKING: templates lock
 */
static void
fill_in_rgctx_template_slot (MonoClass *class, int index, gpointer data, int info_type)
{
	MonoRuntimeGenericContextTemplate *template = mono_class_get_runtime_generic_context_template (class);
	MonoClass *subclass;
	int old_length, new_length;
	int old_instances_length = -1;

	g_assert (!class->generic_class);

	old_length = rgctx_template_num_other_infos (template);
	rgctx_template_set_other_slot (class->image, template, index, data, info_type);
	new_length = rgctx_template_num_other_infos (template);

	if (old_instances_length < 0)
		old_instances_length = old_length;

	/* The reason why the instance's other_infos list can be
	 * shorter than the uninstanted class's is that when we mark
	 * slots as used in superclasses we only do that in the
	 * uninstantiated classes, not in the instances.
	 */
	g_assert (old_instances_length <= old_length);

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

		subclass_oti = class_get_rgctx_template_oti (subclass->parent, index);
		g_assert (subclass_oti.data);

		fill_in_rgctx_template_slot (subclass, index, subclass_oti.data, info_type);

		subclass = subclass_template->next_subclass;
	}
}

/*
 * LOCKING: templates lock
 */
static int
register_other_info (MonoClass *class, gpointer data, int info_type)
{
	int i;
	MonoRuntimeGenericContextTemplate *template = mono_class_get_runtime_generic_context_template (class);
	MonoClass *parent;
	MonoRuntimeGenericContextOtherInfoTemplate *oti;

	g_assert (!class->generic_class && class->generic_container);

	for (i = 0, oti = template->other_infos; oti; ++i, oti = oti->next) {
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
		oti = rgctx_template_get_other_slot (parent_template, i);

		if (oti && oti->data)
			break;

		rgctx_template_set_other_slot (parent->image, parent_template, i, MONO_RGCTX_SLOT_USED_MARKER, 0);

		parent = parent->parent;
	}

	/* Fill in the slot in this class and in all subclasses
	   recursively. */
	fill_in_rgctx_template_slot (class, i, data, info_type);

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
		return data1 == data2;
	default:
		g_assert_not_reached ();
	}
}

/*
 * mono_class_lookup_or_register_other_info:
 * @class: a class
 * @data: the info data
 * @info_type: the type of info to register about data
 * @generic_context: a generic context
 *
 * Looks up and, if necessary, adds information about other_class in
 * class's runtime generic context.  Returns the index of the
 * corresponding other-infos slot.
 */
int
mono_class_lookup_or_register_other_info (MonoClass *class, gpointer data, int info_type,
	MonoGenericContext *generic_context)
{
	static gboolean inited = FALSE;
	static int max_slot = 0;

	MonoRuntimeGenericContextTemplate *rgctx_template =
		mono_class_get_runtime_generic_context_template (class);
	MonoRuntimeGenericContextOtherInfoTemplate *oti;
	int i;

	g_assert (!class->generic_class && class->generic_container);

	templates_lock ();

	for (oti = rgctx_template->other_infos, i = 0; oti; oti = oti->next, ++i) {
		gpointer inflated_data;

		if (!oti || oti->info_type != info_type || !oti->data)
			continue;

		inflated_data = inflate_other_info (oti, generic_context);

		if (other_info_equal (data, inflated_data, info_type)) {
			templates_unlock ();
			return i;
		}
	}

	i = register_other_info (class, data, info_type);

	templates_unlock ();

	if (!inited) {
		mono_counters_register ("RGCTX max slot number", MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &max_slot);
		inited = TRUE;
	}
	if (i > max_slot)
		max_slot = i;


	return i;
}

int
mono_class_rgctx_get_array_size (int n)
{
	g_assert (n >= 0 && n < 30);

	return 4 << n;
}

/*
 * LOCKING: domain lock
 */
static gpointer*
alloc_rgctx_array (MonoDomain *domain, int n)
{
	static gboolean inited = FALSE;
	static int num_alloced = 0;
	static int bytes_alloced = 0;

	int size = mono_class_rgctx_get_array_size (n) * sizeof (gpointer);
	gpointer array = mono_mempool_alloc0 (domain->mp, size);

	if (!inited) {
		mono_counters_register ("RGCTX num arrays alloced", MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &num_alloced);
		mono_counters_register ("RGCTX bytes alloced", MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &bytes_alloced);
		inited = TRUE;
	}

	num_alloced++;
	bytes_alloced += size;

	return array;
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

	MonoRuntimeGenericContext *rgctx;
	MonoDomain *domain = class_vtable->domain;
	MonoClass *class = class_vtable->klass;
	MonoGenericContext *context = &class->generic_class->context;
	MonoRuntimeGenericContextOtherInfoTemplate oti;
	int i, first_slot, size;
	gpointer info;

	mono_domain_lock (domain);

	if (!inited) {
		mono_counters_register ("RGCTX num alloced", MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &num_alloced);
		inited = TRUE;
	}

	rgctx = class_vtable->runtime_generic_context;
	if (!rgctx) {
		rgctx = alloc_rgctx_array (domain, 0);
		class_vtable->runtime_generic_context = rgctx;
		num_alloced++;
	}

	/* First check whether that slot isn't already instantiated.
	   This might happen because lookup doesn't lock.  Allocate
	   arrays on the way. */
	first_slot = 0;
	size = mono_class_rgctx_get_array_size (0);
	for (i = 0; ; ++i) {
		if (slot < first_slot + size - 1) {
			info = rgctx [slot - first_slot + 1];
			if (info) {
				mono_domain_unlock (domain);
				return info;
			}
			break;
		}
		if (!rgctx [0])
			rgctx [0] = alloc_rgctx_array (domain, i + 1);
		rgctx = rgctx [0];
		first_slot += size - 1;
		size = mono_class_rgctx_get_array_size (i + 1);
	}

	g_assert (!rgctx [slot - first_slot + 1]);

	oti = class_get_rgctx_template_oti (class_uninstantiated (class), slot);

	info = rgctx [slot - first_slot + 1] = instantiate_other_info (domain, &oti, context);

	mono_domain_unlock (domain);

	return info;
}
