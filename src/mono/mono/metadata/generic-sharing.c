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

#include "metadata-internals.h"
#include "class.h"
#include "class-internals.h"

static int
type_check_context_used (MonoType *type, gboolean recursive)
{
	int t = mono_type_get_type (type);
	int context_used = 0;

	if (t == MONO_TYPE_VAR)
		context_used |= MONO_GENERIC_CONTEXT_USED_CLASS;
	else if (t == MONO_TYPE_MVAR)
		context_used |= MONO_GENERIC_CONTEXT_USED_METHOD;
	else if (recursive) {
		if (t == MONO_TYPE_CLASS)
			context_used |= mono_class_check_context_used (mono_type_get_class (type));
		else if (t == MONO_TYPE_GENERICINST) {
			MonoGenericClass *gclass = type->data.generic_class;

			context_used |= mono_generic_context_check_used (&gclass->context);
			g_assert (gclass->container_class->generic_container);
		}
	}

	return context_used;
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

/* Maps from uninstantiated generic classes to GSLists's of
 * MonoVTable's of classes that are instantiations of the key class.
 *
 * LOCKING: templates lock
 */
static GHashTable *generic_class_rgctx_hash;

/* Maps from uninstantiated generic classes to GList's of
 * uninstantiated generic classes whose parent is the key class or an
 * instance of the key class.
 *
 * LOCKING: templates lock
 */
static GHashTable *generic_subclass_hash;

static void
list_hash_table_insert (GHashTable **hash, gpointer key, gpointer value)
{
	GSList *list;

	if (!*hash)
		*hash = g_hash_table_new (mono_aligned_addr_hash, NULL);

	list = g_hash_table_lookup (*hash, key);
	g_hash_table_insert (*hash, key, g_slist_prepend (list, value));
}

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
register_open_instance (MonoClass *class)
{
	g_assert (class->generic_class && class->generic_class->container_class);
	g_assert (class_lookup_rgctx_template (class));

	list_hash_table_insert (&class->image->generic_class_open_instances_hash, class->generic_class->container_class, class);
}

/*
 * LOCKING: templates lock
 */
static void
register_rgctx_vtable (MonoVTable *vtable)
{
	MonoClass *class = vtable->klass;

	if (class->generic_class)
		class = class->generic_class->container_class;

	//g_print ("registering rgctx %p for class %s (%p)\n", vtable, mono_type_get_full_name (class), class);

	list_hash_table_insert (&generic_class_rgctx_hash, class, vtable);
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

static void
move_vtables_not_in_domain_foreach_func (MonoClass *class, GSList *list, MonoDomain *domain)
{
	GSList *new_list = NULL;
	GSList *iter = list;

	while (iter) {
		MonoVTable *vtable = iter->data;

		if (vtable->domain != domain)
			new_list = g_slist_prepend (new_list, vtable);

		iter = iter->next;
	}

	g_slist_free (list);

	if (new_list)
		g_hash_table_insert (generic_class_rgctx_hash, class, new_list);
}

/*
 * mono_class_unregister_domain_generic_vtables:
 * @domain: a domain
 *
 * Removes all vtables of the domain from the generic vtables hash.
 * Must be called when a domain is destroyed.
 */
void
mono_class_unregister_domain_generic_vtables (MonoDomain *domain)
{
	GHashTable *old_hash;

	if (!generic_class_rgctx_hash)
		return;

	templates_lock ();

	old_hash = generic_class_rgctx_hash;
	generic_class_rgctx_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);

	g_hash_table_foreach (old_hash, (GHFunc)move_vtables_not_in_domain_foreach_func, domain);

	templates_unlock ();

	g_hash_table_destroy (old_hash);
}

/*
 * LOCKING: templates lock
 */
static void
rgctx_template_set_other_slot (MonoImage *image, MonoRuntimeGenericContextTemplate *template, int slot,
	MonoType *type, int info_type)
{
	int i;
	MonoRuntimeGenericContextOtherInfoTemplate **oti;

	g_assert (slot >= 0);

	mono_loader_lock ();

	oti = &template->other_infos;
	i = 0;
	while (i <= slot) {
		if (i > 0)
			oti = &(*oti)->next;
		if (!*oti)
			*oti = mono_mempool_alloc0 (image->mempool, sizeof (MonoRuntimeGenericContextOtherInfoTemplate));
		++i;
	}

	g_assert (!(*oti)->type);
	(*oti)->type = type;
	(*oti)->info_type = info_type;

	mono_loader_unlock ();
}

static gboolean
include_arg_info (MonoType *type, MonoRuntimeGenericContextTemplate *parent_template)
{
	int i;

	if (!MONO_TYPE_IS_REFERENCE (type) &&
			mono_type_get_type (type) != MONO_TYPE_VAR &&
			mono_type_get_type (type) != MONO_TYPE_MVAR)
		return FALSE;

	if (!parent_template)
		return TRUE;

	for (i = 0; i < parent_template->num_arg_infos; ++i)
		if (type == parent_template->arg_infos [i])
			return FALSE;

	return TRUE;
}

#define MONO_RGCTX_SLOT_USED_MARKER	(&mono_defaults.object_class->byval_arg)

/*
 * mono_class_get_runtime_generic_context_template:
 * @class: a class
 *
 * Looks up or constructs, if necessary, the runtime generic context
 * for class.
 */
MonoRuntimeGenericContextTemplate*
mono_class_get_runtime_generic_context_template (MonoClass *class)
{
	int num_parent_args, num_class_args, total_num_args;
	MonoRuntimeGenericContextTemplate *parent_template, *template;
	MonoGenericInst *inst;
	int i, j;
	MonoRuntimeGenericContextOtherInfoTemplate *oti;

	templates_lock ();
	template = class_lookup_rgctx_template (class);
	templates_unlock ();

	if (template)
		return template;

	if (class->generic_class) {
		parent_template = mono_class_get_runtime_generic_context_template (class->generic_class->container_class);

		mono_loader_lock ();
		template = mono_mempool_alloc0 (class->image->mempool,
			sizeof (MonoRuntimeGenericContextTemplate) + sizeof (MonoType*) * parent_template->num_arg_infos);
		mono_loader_unlock ();

		template->num_arg_infos = parent_template->num_arg_infos;

		for (i = 0; i < parent_template->num_arg_infos; ++i) {
			template->arg_infos [i] = mono_class_inflate_generic_type (parent_template->arg_infos [i],
				&class->generic_class->context);
			/*
			g_print("arg info %d of %s is %s (from %s)\n", i,
				mono_type_get_full_name (class),
				mono_type_get_name (template->arg_infos [i]),
				mono_type_get_name (parent_template->arg_infos [i]));
			*/
		}

		/* FIXME: quadratic */
		for (i = 0, oti = parent_template->other_infos; oti; ++i, oti = oti->next) {
			if (oti->type && oti->type != MONO_RGCTX_SLOT_USED_MARKER) {
				rgctx_template_set_other_slot (class->image, template, i,
					mono_class_inflate_generic_type (oti->type, &class->generic_class->context),
					oti->info_type);
			}
		}

		templates_lock ();

		if (class_lookup_rgctx_template (class)) {
			/* some other thread already set the template */
			template = class_lookup_rgctx_template (class);
		} else {
			class_set_rgctx_template (class, template);
			register_open_instance (class);
		}

		templates_unlock ();

		return template;
	}

	if (class->parent)
		parent_template = mono_class_get_runtime_generic_context_template (class->parent);
	else
		parent_template = NULL;

	if (parent_template)
		num_parent_args = parent_template->num_arg_infos;
	else
		num_parent_args = 0;

	if (class->generic_container)
		inst = class->generic_container->context.class_inst;
	else
		inst = NULL;

	num_class_args = 0;
	if (inst) {
		for (i = 0; i < inst->type_argc; ++i) {
			if (include_arg_info (inst->type_argv [i], parent_template))
				++num_class_args;
		}
	}

	total_num_args = num_parent_args + num_class_args;

	mono_loader_lock ();
	template = mono_mempool_alloc0 (class->image->mempool,
		sizeof (MonoRuntimeGenericContextTemplate) + sizeof (MonoType*) * total_num_args);
	mono_loader_unlock ();

	template->num_arg_infos = total_num_args;

	if (num_parent_args > 0)
		memcpy (template->arg_infos, parent_template->arg_infos,
			sizeof (MonoType*) * parent_template->num_arg_infos);

	j = 0;
	if (inst) {
		for (i = 0; i < inst->type_argc; ++i) {
			MonoType *type = inst->type_argv [i];

			if (include_arg_info (type, parent_template))
				template->arg_infos [num_parent_args + j++] = type;
		}
	}
	g_assert (j == num_class_args);

	/*
	g_print ("class %s has %d type args (%d from parent)\n",
		mono_type_get_full_name(class), total_num_args, num_parent_args);
	*/

	if (parent_template) {
		MonoRuntimeGenericContextOtherInfoTemplate *oti, **new_oti;

		oti = parent_template->other_infos;
		new_oti = &template->other_infos;

		mono_loader_lock ();

		while (oti) {
			*new_oti = mono_mempool_alloc0 (class->image->mempool,
				sizeof (MonoRuntimeGenericContextOtherInfoTemplate));
			(*new_oti)->type = oti->type;
			(*new_oti)->info_type = oti->info_type;

			oti = oti->next;
			new_oti = &(*new_oti)->next;
		}

		mono_loader_unlock ();
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
instantiate_other_info (MonoDomain *domain, MonoType *type, int info_type, MonoGenericContext *context)
{
	MonoType *arg_type;
	MonoClass *arg_class;

	if (!type)
		return NULL;

	arg_type = mono_class_inflate_generic_type (type, context);

	arg_class = mono_class_from_mono_type (arg_type);
	g_assert (arg_class);

	return class_type_info (domain, arg_class, info_type);
}

/*
 * LOCKING: templates lock
 */
static void
fill_in_rgctx_template_slot (MonoClass *class, int index, MonoType *type, int info_type)
{
	MonoRuntimeGenericContextTemplate *template = mono_class_get_runtime_generic_context_template (class);
	MonoClass *subclass;
	GSList *instances;
	int old_length, new_length, old_instances_extra_length;
	int old_instances_length = -1;

	g_assert (!class->generic_class);

	old_length = rgctx_template_num_other_infos (template);
	rgctx_template_set_other_slot (class->image, template, index, type, info_type);
	new_length = rgctx_template_num_other_infos (template);

	/* For all open instantiations on this level: Instantiate the
	 * type and put it in the rgctx template.
	 */
	if (class->image->generic_class_open_instances_hash)
		instances = g_hash_table_lookup (class->image->generic_class_open_instances_hash, class);
	else
		instances = NULL;

	while (instances) {
		MonoClass *instance = instances->data;
		MonoRuntimeGenericContextTemplate *instance_template = class_lookup_rgctx_template (instance);
		int length;
		MonoType *inflated_type;

		g_assert (instance_template);
		g_assert (instance->generic_class != NULL && instance->generic_class->container_class == class);

		length = rgctx_template_num_other_infos (instance_template);
		if (old_instances_length < 0)
			old_instances_length = length;

		g_assert (length == old_instances_length);

		inflated_type = mono_class_inflate_generic_type (type, &instance->generic_class->context);
		rgctx_template_set_other_slot (instance->image, instance_template, index,
			inflated_type, info_type);

		g_assert (rgctx_template_num_other_infos (instance_template) == new_length);

		instances = instances->next;
	}

	if (old_instances_length < 0)
		old_instances_length = old_length;
	old_instances_extra_length = MAX(old_instances_length - MONO_RGCTX_MAX_OTHER_INFOS, 0);

	g_assert (old_instances_length <= old_length);

	/* The reason why the instance's other_infos list can be
	 * shorter than the uninstanted class's is that when we mark
	 * slots as used in superclasses we only do that in the
	 * uninstantiated classes, not in the instances.
	 */
	g_assert (old_instances_length <= old_length);

	/* For all rgctx's on this level: Instantiate the type and put
	 * the class in the rgctx.
	 */
	if (generic_class_rgctx_hash)
		instances = g_hash_table_lookup (generic_class_rgctx_hash, class);
	else
		instances = NULL;

	while (instances) {
		MonoVTable *vtable = instances->data;
		MonoRuntimeGenericContextTemplate *instance_template;
		int real_index;
		gpointer *table;

		instance_template = mono_class_get_runtime_generic_context_template (class_uninstantiated (vtable->klass));

		g_assert (vtable->runtime_generic_context);

		if (index < MONO_RGCTX_MAX_OTHER_INFOS) {
			real_index = index;
			table = vtable->runtime_generic_context->other_infos;
		} else {
			real_index = index - MONO_RGCTX_MAX_OTHER_INFOS;

			if (old_instances_length == new_length) {
				table = vtable->runtime_generic_context->extra_other_infos;
			} else {
				g_assert (old_instances_extra_length < real_index + 1);

				/* Allocate new table with the required number
				   of slots and copy the old slots over. */
				mono_domain_lock (vtable->domain);
				table = mono_mempool_alloc0 (vtable->domain->mp,
					sizeof (gpointer) * (real_index + 1));
				mono_domain_unlock (vtable->domain);

				memcpy (table, vtable->runtime_generic_context->extra_other_infos,
					sizeof (gpointer) * old_instances_extra_length);
				mono_memory_write_barrier ();
				vtable->runtime_generic_context->extra_other_infos = table;
			}
		}

		g_assert (!table [real_index]);

		//g_print ("rgctx %s . other_infos [%d] = %s\n", mono_type_get_full_name (vtable->klass), index, mono_type_get_full_name (other_class));

		instances = instances->next;
	}

	/* Recurse for all subclasses */
	if (generic_subclass_hash)
		subclass = g_hash_table_lookup (generic_subclass_hash, class);
	else
		subclass = NULL;

	while (subclass) {
		MonoType *subclass_type;
		MonoRuntimeGenericContextTemplate *subclass_template = class_lookup_rgctx_template (subclass);

		g_assert (!subclass->generic_class);
		g_assert (subclass_template);

		subclass_type = rgctx_template_get_other_slot (class_lookup_rgctx_template (subclass->parent), index)->type;
		g_assert (subclass_type != NULL);

		fill_in_rgctx_template_slot (subclass, index, subclass_type, info_type);

		subclass = subclass_template->next_subclass;
	}
}

/*
 * LOCKING: templates lock
 */
static int
register_other_info (MonoClass *class, MonoClass *other_class, int info_type)
{
	int i;
	MonoRuntimeGenericContextTemplate *template = mono_class_get_runtime_generic_context_template (class);
	MonoClass *parent;
	MonoRuntimeGenericContextOtherInfoTemplate *oti;

	g_assert (!class->generic_class && class->generic_container);

	for (i = 0, oti = template->other_infos; oti; ++i, oti = oti->next) {
		if (!oti->type)
			break;
	}

	//g_print ("template %s . other_infos [%d] = %s\n", mono_type_get_full_name (class), i, mono_type_get_full_name (other_class));

	/* Mark the slot as used in all parent classes (until we find
	   a parent class which already has it marked used). */
	parent = class->parent;
	while (parent != NULL) {
		MonoRuntimeGenericContextTemplate *parent_template;

		if (parent->generic_class)
			parent = parent->generic_class->container_class;

		parent_template = mono_class_get_runtime_generic_context_template (parent);

		if (rgctx_template_get_other_slot (parent_template, i))
			break;

		rgctx_template_set_other_slot (parent->image, parent_template, i, MONO_RGCTX_SLOT_USED_MARKER, 0);

		parent = parent->parent;
	}

	/* Fill in the slot in this class and in all subclasses
	   recursively. */
	fill_in_rgctx_template_slot (class, i, &other_class->byval_arg, info_type);

	return i;
}

/*
 * mono_class_lookup_or_register_other_info:
 * @class: a class
 * @other_class: the class about which to register info
 * @info_type: the type of info to register about other_class
 * @generic_context: a generic context
 *
 * Looks up and, if necessary, adds information about other_class in
 * class's runtime generic context.  Returns the index of the
 * corresponding other-infos slot.
 */
int
mono_class_lookup_or_register_other_info (MonoClass *class, MonoClass *other_class, int info_type, 
	MonoGenericContext *generic_context)
{
	MonoRuntimeGenericContextTemplate *rgctx_template =
		mono_class_get_runtime_generic_context_template (class);
	int i, num_other_infos;

	templates_lock ();

	num_other_infos = rgctx_template_num_other_infos (rgctx_template);
	for (i = 0; i < num_other_infos; ++i) {
		/* FIXME: This is quadratic in complexity! */
		MonoRuntimeGenericContextOtherInfoTemplate *oti = rgctx_template_get_other_slot (rgctx_template, i);
		MonoType *inflated_type;

		if (!oti || oti->info_type != info_type)
			continue;

		inflated_type = mono_class_inflate_generic_type (oti->type, generic_context);

		if (other_class == mono_class_from_mono_type (inflated_type)) {
			templates_unlock ();
			return i;
		}
	}

	i = register_other_info (class, other_class, info_type);

	templates_unlock ();

	return i;
}

static void
instantiate_arg_info (MonoDomain *domain, MonoRuntimeGenericArgInfo *destination, MonoType *arg_info,
		MonoGenericContext *context)
{
	MonoType *arg_type;
	MonoClass *arg_class;
	MonoVTable *vtable;

	if (!arg_info)
		return;

	arg_type = mono_class_inflate_generic_type (arg_info, context);

	arg_class = mono_class_from_mono_type (arg_type);
	g_assert (arg_class);

	vtable = mono_class_vtable (domain, arg_class);

	destination->static_data = vtable->data;
	destination->klass = arg_class;
	destination->vtable = vtable;
}

/*
 * LOCKING: domain lock for rgctx->vtable->domain
 */
static void
rgctx_alloc_extra_other_types (MonoRuntimeGenericContext *rgctx)
{
	MonoClass *class = rgctx->vtable->klass;
	MonoDomain *domain = rgctx->vtable->domain;
	MonoRuntimeGenericContextTemplate *rgctx_template;
	int num_extra_other_infos;

	rgctx_template = mono_class_get_runtime_generic_context_template (class_uninstantiated (class));
	num_extra_other_infos = MAX (rgctx_template_num_other_infos (rgctx_template) - MONO_RGCTX_MAX_OTHER_INFOS, 0);

	if (num_extra_other_infos > 0)
		rgctx->extra_other_infos = mono_mempool_alloc0 (domain->mp,
			sizeof (gpointer) * num_extra_other_infos);
}

/*
 * mono_class_fill_runtime_generic_context:
 * @rgctx: a runtime generic context
 *
 * Instantiates all slots of the runtime generic context rgctx.
 */
void
mono_class_fill_runtime_generic_context (MonoRuntimeGenericContext *rgctx)
{
	MonoVTable *class_vtable = rgctx->vtable;
	MonoDomain *domain = class_vtable->domain;
	MonoClass *class = class_vtable->klass;
	int depth = class->idepth;
	MonoGenericContext *context = &class->generic_class->context;
	MonoRuntimeGenericSuperInfo *super_infos = (MonoRuntimeGenericSuperInfo*)rgctx - depth;
	MonoRuntimeGenericContextTemplate *rgctx_template;
	MonoRuntimeGenericContextOtherInfoTemplate *oti;
	MonoClass *super;
	int i;

	rgctx_template = mono_class_get_runtime_generic_context_template (class_uninstantiated (class));

	mono_domain_lock (domain);

	depth = 0;
	for (super = class; super; super = super->parent) {
		MonoVTable *vtable = mono_class_vtable (domain, super);

		super_infos [depth].static_data = vtable->data;
		super_infos [depth].klass = super;
		super_infos [depth].vtable = vtable;

		depth++;
	}

	rgctx_alloc_extra_other_types (rgctx);

	for (i = 0, oti = rgctx_template->other_infos; oti; ++i, oti = oti->next) {
		gpointer *arg_info;

		if (i < MONO_RGCTX_MAX_OTHER_INFOS)
			arg_info = &rgctx->other_infos [i];
		else
			arg_info = &rgctx->extra_other_infos [i - MONO_RGCTX_MAX_OTHER_INFOS];

		*arg_info = instantiate_other_info (domain, oti->type, oti->info_type, context);
	}

	for (i = 0; i < rgctx_template->num_arg_infos; ++i)
		instantiate_arg_info (domain, &rgctx->arg_infos [i], rgctx_template->arg_infos [i], context);

	mono_domain_unlock (domain);
}

/*
 * mono_class_setup_runtime_generic_context:
 * @class: a class
 * @domain: a domain
 *
 * Sets up the runtime generic context of class in domain.
 */
void
mono_class_setup_runtime_generic_context (MonoClass *class, MonoDomain *domain)
{
	MonoVTable *class_vtable = mono_class_vtable (domain, class);
	int depth = class->idepth;
	MonoRuntimeGenericSuperInfo *super_infos;
	MonoRuntimeGenericContext *rgctx;
	MonoRuntimeGenericContextTemplate *rgctx_template;

	/* Never setup a rgctx for an open class. */
	if (mono_class_check_context_used (class))
		return;

	//g_print ("setting up rgctx for %s\n", mono_type_get_full_name (class));

	/* We have to take the lock here and only release it after
	 * we've registered the vtable because otherwise another
	 * thread might add a slot to the template which we wouldn't
	 * know anything about and we'd end up with a rgctx that's not
	 * fully filled in.
	 */
	templates_lock ();

	rgctx_template = mono_class_get_runtime_generic_context_template (class_uninstantiated (class));

	mono_domain_lock (domain);
	/* We don't allocate arg_infos because we don't use it yet.
	 */
	super_infos = mono_mempool_alloc0 (domain->mp,
		sizeof (MonoRuntimeGenericSuperInfo) * depth +
		sizeof (MonoRuntimeGenericContext) +
		sizeof (MonoRuntimeGenericArgInfo) * rgctx_template->num_arg_infos);
	mono_domain_unlock (domain);

	rgctx = class_vtable->runtime_generic_context = (MonoRuntimeGenericContext*) (super_infos + depth);

	rgctx->domain = domain;
	rgctx->vtable = class_vtable;

	rgctx_alloc_extra_other_types (rgctx);

	register_rgctx_vtable (class_vtable);

	templates_unlock ();
}
