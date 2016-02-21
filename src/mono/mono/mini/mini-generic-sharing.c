/*
 * generic-sharing.c: Support functions for generic sharing.
 *
 * Author:
 *   Mark Probst (mark.probst@gmail.com)
 *
 * Copyright 2007-2011 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 */

#include <config.h>

#include <mono/metadata/class.h>
#include <mono/metadata/method-builder.h>
#include <mono/metadata/reflection-internals.h>
#include <mono/utils/mono-counters.h>

#include "mini.h"

#define ALLOW_PARTIAL_SHARING TRUE
//#define ALLOW_PARTIAL_SHARING FALSE
 
#if 0
#define DEBUG(...) __VA_ARGS__
#else
#define DEBUG(...)
#endif

static void
mono_class_unregister_image_generic_subclasses (MonoImage *image, gpointer user_data);

/* Counters */
static int num_templates_allocted;
static int num_templates_bytes;
static int num_oti_allocted;
static int num_oti_bytes;

#define gshared_lock() mono_os_mutex_lock (&gshared_mutex)
#define gshared_unlock() mono_os_mutex_unlock (&gshared_mutex)
static mono_mutex_t gshared_mutex;

static gboolean partial_supported = FALSE;

static inline gboolean
partial_sharing_supported (void)
{
	if (!ALLOW_PARTIAL_SHARING)
		return FALSE;
	/* Enable this when AOT compiling or running in full-aot mode */
	if (mono_aot_only)
		return TRUE;
	if (partial_supported)
		return TRUE;
	return FALSE;
}

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
mono_class_check_context_used (MonoClass *klass)
{
	int context_used = 0;

	context_used |= type_check_context_used (&klass->this_arg, FALSE);
	context_used |= type_check_context_used (&klass->byval_arg, FALSE);

	if (klass->generic_class)
		context_used |= mono_generic_context_check_used (&klass->generic_class->context);
	else if (klass->generic_container)
		context_used |= mono_generic_context_check_used (&klass->generic_container->context);

	return context_used;
}

/*
 * LOCKING: loader lock
 */
static MonoRuntimeGenericContextInfoTemplate*
get_info_templates (MonoRuntimeGenericContextTemplate *template_, int type_argc)
{
	g_assert (type_argc >= 0);
	if (type_argc == 0)
		return template_->infos;
	return (MonoRuntimeGenericContextInfoTemplate *)g_slist_nth_data (template_->method_templates, type_argc - 1);
}

/*
 * LOCKING: loader lock
 */
static void
set_info_templates (MonoImage *image, MonoRuntimeGenericContextTemplate *template_, int type_argc,
	MonoRuntimeGenericContextInfoTemplate *oti)
{
	g_assert (type_argc >= 0);
	if (type_argc == 0)
		template_->infos = oti;
	else {
		int length = g_slist_length (template_->method_templates);
		GSList *list;

		/* FIXME: quadratic! */
		while (length < type_argc) {
			template_->method_templates = g_slist_append_image (image, template_->method_templates, NULL);
			length++;
		}

		list = g_slist_nth (template_->method_templates, type_argc - 1);
		g_assert (list);
		list->data = oti;
	}
}

/*
 * LOCKING: loader lock
 */
static int
template_get_max_argc (MonoRuntimeGenericContextTemplate *template_)
{
	return g_slist_length (template_->method_templates);
}

/*
 * LOCKING: loader lock
 */
static MonoRuntimeGenericContextInfoTemplate*
rgctx_template_get_other_slot (MonoRuntimeGenericContextTemplate *template_, int type_argc, int slot)
{
	int i;
	MonoRuntimeGenericContextInfoTemplate *oti;

	g_assert (slot >= 0);

	for (oti = get_info_templates (template_, type_argc), i = 0; i < slot; oti = oti->next, ++i) {
		if (!oti)
			return NULL;
	}

	return oti;
}

/*
 * LOCKING: loader lock
 */
static int
rgctx_template_num_infos (MonoRuntimeGenericContextTemplate *template_, int type_argc)
{
	MonoRuntimeGenericContextInfoTemplate *oti;
	int i;

	for (i = 0, oti = get_info_templates (template_, type_argc); oti; ++i, oti = oti->next)
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
class_set_rgctx_template (MonoClass *klass, MonoRuntimeGenericContextTemplate *rgctx_template)
{
	if (!klass->image->rgctx_template_hash)
		klass->image->rgctx_template_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);

	g_hash_table_insert (klass->image->rgctx_template_hash, klass, rgctx_template);
}

/*
 * LOCKING: loader lock
 */
static MonoRuntimeGenericContextTemplate*
class_lookup_rgctx_template (MonoClass *klass)
{
	MonoRuntimeGenericContextTemplate *template_;

	if (!klass->image->rgctx_template_hash)
		return NULL;

	template_ = (MonoRuntimeGenericContextTemplate *)g_hash_table_lookup (klass->image->rgctx_template_hash, klass);

	return template_;
}

/*
 * LOCKING: loader lock
 */
static void
register_generic_subclass (MonoClass *klass)
{
	MonoClass *parent = klass->parent;
	MonoClass *subclass;
	MonoRuntimeGenericContextTemplate *rgctx_template = class_lookup_rgctx_template (klass);

	g_assert (rgctx_template);

	if (parent->generic_class)
		parent = parent->generic_class->container_class;

	if (!generic_subclass_hash)
		generic_subclass_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);

	subclass = (MonoClass *)g_hash_table_lookup (generic_subclass_hash, parent);
	rgctx_template->next_subclass = subclass;
	g_hash_table_insert (generic_subclass_hash, parent, klass);
}

static void
move_subclasses_not_in_image_foreach_func (MonoClass *klass, MonoClass *subclass, MonoImage *image)
{
	MonoClass *new_list;

	if (klass->image == image) {
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
		g_hash_table_insert (generic_subclass_hash, klass, new_list);
}

/*
 * mono_class_unregister_image_generic_subclasses:
 * @image: an image
 *
 * Removes all classes of the image from the generic subclass hash.
 * Must be called when an image is unloaded.
 */
static void
mono_class_unregister_image_generic_subclasses (MonoImage *image, gpointer user_data)
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
alloc_template (MonoClass *klass)
{
	int size = sizeof (MonoRuntimeGenericContextTemplate);

	num_templates_allocted++;
	num_templates_bytes += size;

	return (MonoRuntimeGenericContextTemplate *)mono_image_alloc0 (klass->image, size);
}

/* LOCKING: Takes the loader lock */
static MonoRuntimeGenericContextInfoTemplate*
alloc_oti (MonoImage *image)
{
	int size = sizeof (MonoRuntimeGenericContextInfoTemplate);

	num_oti_allocted++;
	num_oti_bytes += size;

	return (MonoRuntimeGenericContextInfoTemplate *)mono_image_alloc0 (image, size);
}

#define MONO_RGCTX_SLOT_USED_MARKER	((gpointer)&mono_defaults.object_class->byval_arg)

/*
 * Return true if this info type has the notion of identify.
 *
 * Some info types expect that each insert results in a new slot been assigned.
 */
static int
info_has_identity (MonoRgctxInfoType info_type)
{
	return info_type != MONO_RGCTX_INFO_CAST_CACHE;
}

/*
 * LOCKING: loader lock
 */
static void
rgctx_template_set_slot (MonoImage *image, MonoRuntimeGenericContextTemplate *template_, int type_argc,
	int slot, gpointer data, MonoRgctxInfoType info_type)
{
	static gboolean inited = FALSE;
	static int num_markers = 0;
	static int num_data = 0;

	int i;
	MonoRuntimeGenericContextInfoTemplate *list = get_info_templates (template_, type_argc);
	MonoRuntimeGenericContextInfoTemplate **oti = &list;

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

	set_info_templates (image, template_, type_argc, list);

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
		if (klass->exception_type)
			return NULL;
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
		MonoError error;
		MonoGenericContext context;

		context.class_inst = NULL;
		context.method_inst = mono_method_get_context (method)->method_inst;

		m = mono_class_inflate_generic_method_checked (m, &context, &error);
		g_assert (mono_error_ok (&error)); /* FIXME don't swallow the error */
	}

	return m;
}

static gpointer
inflate_info (MonoRuntimeGenericContextInfoTemplate *oti, MonoGenericContext *context, MonoClass *klass, gboolean temporary)
{
	gpointer data = oti->data;
	MonoRgctxInfoType info_type = oti->info_type;
	MonoError error;

	g_assert (data);

	if (data == MONO_RGCTX_SLOT_USED_MARKER)
		return MONO_RGCTX_SLOT_USED_MARKER;

	switch (info_type)
	{
	case MONO_RGCTX_INFO_STATIC_DATA:
	case MONO_RGCTX_INFO_KLASS:
	case MONO_RGCTX_INFO_ELEMENT_KLASS:
	case MONO_RGCTX_INFO_VTABLE:
	case MONO_RGCTX_INFO_TYPE:
	case MONO_RGCTX_INFO_REFLECTION_TYPE:
	case MONO_RGCTX_INFO_CAST_CACHE:
	case MONO_RGCTX_INFO_ARRAY_ELEMENT_SIZE:
	case MONO_RGCTX_INFO_VALUE_SIZE:
	case MONO_RGCTX_INFO_CLASS_BOX_TYPE:
	case MONO_RGCTX_INFO_MEMCPY:
	case MONO_RGCTX_INFO_BZERO:
	case MONO_RGCTX_INFO_LOCAL_OFFSET:
	case MONO_RGCTX_INFO_NULLABLE_CLASS_BOX:
	case MONO_RGCTX_INFO_NULLABLE_CLASS_UNBOX: {
		gpointer result = mono_class_inflate_generic_type_with_mempool (temporary ? NULL : klass->image,
			(MonoType *)data, context, &error);
		if (!mono_error_ok (&error)) /*FIXME proper error handling */
			g_error ("Could not inflate generic type due to %s", mono_error_get_message (&error));
		return result;
	}

	case MONO_RGCTX_INFO_METHOD:
	case MONO_RGCTX_INFO_GENERIC_METHOD_CODE:
	case MONO_RGCTX_INFO_GSHAREDVT_OUT_WRAPPER:
	case MONO_RGCTX_INFO_METHOD_RGCTX:
	case MONO_RGCTX_INFO_METHOD_CONTEXT:
	case MONO_RGCTX_INFO_REMOTING_INVOKE_WITH_CHECK:
	case MONO_RGCTX_INFO_METHOD_DELEGATE_CODE: {
		MonoMethod *method = (MonoMethod *)data;
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
			MonoError error;
			inflated_method = mono_class_inflate_generic_method_checked (method, context, &error);
			g_assert (mono_error_ok (&error)); /* FIXME don't swallow the error */
		}
		mono_class_init (inflated_method->klass);
		g_assert (inflated_method->klass == inflated_class);
		return inflated_method;
	}
	case MONO_RGCTX_INFO_METHOD_GSHAREDVT_INFO: {
		MonoGSharedVtMethodInfo *oinfo = (MonoGSharedVtMethodInfo *)data;
		MonoGSharedVtMethodInfo *res;
		MonoDomain *domain = mono_domain_get ();
		int i;

		res = (MonoGSharedVtMethodInfo *)mono_domain_alloc0 (domain, sizeof (MonoGSharedVtMethodInfo));
		/*
		res->nlocals = info->nlocals;
		res->locals_types = g_new0 (MonoType*, info->nlocals);
		for (i = 0; i < info->nlocals; ++i)
			res->locals_types [i] = mono_class_inflate_generic_type (info->locals_types [i], context);
		*/
		res->num_entries = oinfo->num_entries;
		res->entries = (MonoRuntimeGenericContextInfoTemplate *)mono_domain_alloc0 (domain, sizeof (MonoRuntimeGenericContextInfoTemplate) * oinfo->num_entries);
		for (i = 0; i < oinfo->num_entries; ++i) {
			MonoRuntimeGenericContextInfoTemplate *otemplate = &oinfo->entries [i];
			MonoRuntimeGenericContextInfoTemplate *template_ = &res->entries [i];

			memcpy (template_, otemplate, sizeof (MonoRuntimeGenericContextInfoTemplate));
			template_->data = inflate_info (template_, context, klass, FALSE);
		}
		return res;
	}
	case MONO_RGCTX_INFO_METHOD_GSHAREDVT_OUT_TRAMPOLINE:
	case MONO_RGCTX_INFO_METHOD_GSHAREDVT_OUT_TRAMPOLINE_VIRT: {
		MonoJumpInfoGSharedVtCall *info = (MonoJumpInfoGSharedVtCall *)data;
		MonoMethod *method = info->method;
		MonoMethod *inflated_method;
		MonoType *inflated_type = mono_class_inflate_generic_type (&method->klass->byval_arg, context);
		MonoClass *inflated_class = mono_class_from_mono_type (inflated_type);
		MonoJumpInfoGSharedVtCall *res;
		MonoDomain *domain = mono_domain_get ();

		res = (MonoJumpInfoGSharedVtCall *)mono_domain_alloc0 (domain, sizeof (MonoJumpInfoGSharedVtCall));
		/* Keep the original signature */
		res->sig = info->sig;

		mono_metadata_free_type (inflated_type);

		mono_class_init (inflated_class);

		g_assert (!method->wrapper_type);

		if (inflated_class->byval_arg.type == MONO_TYPE_ARRAY ||
				inflated_class->byval_arg.type == MONO_TYPE_SZARRAY) {
			inflated_method = mono_method_search_in_array_class (inflated_class,
				method->name, method->signature);
		} else {
			MonoError error;
			inflated_method = mono_class_inflate_generic_method_checked (method, context, &error);
			g_assert (mono_error_ok (&error)); /* FIXME don't swallow the error */
		}
		mono_class_init (inflated_method->klass);
		g_assert (inflated_method->klass == inflated_class);
		res->method = inflated_method;

		return res;
	}

	case MONO_RGCTX_INFO_CLASS_FIELD:
	case MONO_RGCTX_INFO_FIELD_OFFSET: {
		MonoClassField *field = (MonoClassField *)data;
		MonoType *inflated_type = mono_class_inflate_generic_type (&field->parent->byval_arg, context);
		MonoClass *inflated_class = mono_class_from_mono_type (inflated_type);
		int i = field - field->parent->fields;
		gpointer dummy = NULL;

		mono_metadata_free_type (inflated_type);

		mono_class_get_fields (inflated_class, &dummy);
		g_assert (inflated_class->fields);

		return &inflated_class->fields [i];
	}
	case MONO_RGCTX_INFO_SIG_GSHAREDVT_IN_TRAMPOLINE_CALLI:
	case MONO_RGCTX_INFO_SIG_GSHAREDVT_OUT_TRAMPOLINE_CALLI: {
		MonoMethodSignature *sig = (MonoMethodSignature *)data;
		MonoMethodSignature *isig;
		MonoError error;

		isig = mono_inflate_generic_signature (sig, context, &error);
		g_assert (mono_error_ok (&error));
		return isig;
	}
	case MONO_RGCTX_INFO_VIRT_METHOD_CODE:
	case MONO_RGCTX_INFO_VIRT_METHOD_BOX_TYPE: {
		MonoJumpInfoVirtMethod *info = (MonoJumpInfoVirtMethod *)data;
		MonoJumpInfoVirtMethod *res;
		MonoType *t;
		MonoDomain *domain = mono_domain_get ();
		MonoError error;

		// FIXME: Temporary
		res = (MonoJumpInfoVirtMethod *)mono_domain_alloc0 (domain, sizeof (MonoJumpInfoVirtMethod));
		t = mono_class_inflate_generic_type (&info->klass->byval_arg, context);
		res->klass = mono_class_from_mono_type (t);
		mono_metadata_free_type (t);

		res->method = mono_class_inflate_generic_method_checked (info->method, context, &error);
		g_assert (mono_error_ok (&error)); /* FIXME don't swallow the error */

		return res;
	}
	default:
		g_assert_not_reached ();
	}
	/* Not reached, quiet compiler */
	return NULL;
}

static void
free_inflated_info (MonoRgctxInfoType info_type, gpointer info)
{
	if (!info)
		return;

	switch (info_type) {
	case MONO_RGCTX_INFO_STATIC_DATA:
	case MONO_RGCTX_INFO_KLASS:
	case MONO_RGCTX_INFO_ELEMENT_KLASS:
	case MONO_RGCTX_INFO_VTABLE:
	case MONO_RGCTX_INFO_TYPE:
	case MONO_RGCTX_INFO_REFLECTION_TYPE:
	case MONO_RGCTX_INFO_CAST_CACHE:
		mono_metadata_free_type ((MonoType *)info);
		break;
	default:
		break;
	}
}

static MonoRuntimeGenericContextInfoTemplate
class_get_rgctx_template_oti (MonoClass *klass, int type_argc, guint32 slot, gboolean temporary, gboolean shared, gboolean *do_free);
 
static MonoClass*
class_uninstantiated (MonoClass *klass)
{
	if (klass->generic_class)
		return klass->generic_class->container_class;
	return klass;
}

/*
 * get_shared_class:
 *
 *   Return the class used to store information when using generic sharing.
 */
static MonoClass*
get_shared_class (MonoClass *klass)
{
	return class_uninstantiated (klass);
}

/*
 * mono_class_get_runtime_generic_context_template:
 * @class: a class
 *
 * Looks up or constructs, if necessary, the runtime generic context template for class.
 * The template is the same for all instantiations of a class.
 */
static MonoRuntimeGenericContextTemplate*
mono_class_get_runtime_generic_context_template (MonoClass *klass)
{
	MonoRuntimeGenericContextTemplate *parent_template, *template_;
	guint32 i;

	klass = get_shared_class (klass);

	mono_loader_lock ();
	template_ = class_lookup_rgctx_template (klass);
	mono_loader_unlock ();

	if (template_)
		return template_;

	//g_assert (get_shared_class (class) == class);

	template_ = alloc_template (klass);

	mono_loader_lock ();

	if (klass->parent) {
		guint32 num_entries;
		int max_argc, type_argc;

		parent_template = mono_class_get_runtime_generic_context_template (klass->parent);
		max_argc = template_get_max_argc (parent_template);

		for (type_argc = 0; type_argc <= max_argc; ++type_argc) {
			num_entries = rgctx_template_num_infos (parent_template, type_argc);

			/* FIXME: quadratic! */
			for (i = 0; i < num_entries; ++i) {
				MonoRuntimeGenericContextInfoTemplate oti;

				oti = class_get_rgctx_template_oti (klass->parent, type_argc, i, FALSE, FALSE, NULL);
				if (oti.data && oti.data != MONO_RGCTX_SLOT_USED_MARKER) {
					rgctx_template_set_slot (klass->image, template_, type_argc, i,
											 oti.data, oti.info_type);
				}
			}
		}
	}

	if (class_lookup_rgctx_template (klass)) {
		/* some other thread already set the template */
		template_ = class_lookup_rgctx_template (klass);
	} else {
		class_set_rgctx_template (klass, template_);

		if (klass->parent)
			register_generic_subclass (klass);
	}

	mono_loader_unlock ();

	return template_;
}

/*
 * class_get_rgctx_template_oti:
 *
 *   Return the info template of CLASS numbered TYPE_ARGC/SLOT.
 * temporary signifies whether the inflated info (oti.data) will be
 * used temporarily, in which case it might be heap-allocated, or
 * permanently, in which case it will be mempool-allocated.  If
 * temporary is set then *do_free will return whether the returned
 * data must be freed.
 *
 * LOCKING: loader lock
 */
static MonoRuntimeGenericContextInfoTemplate
class_get_rgctx_template_oti (MonoClass *klass, int type_argc, guint32 slot, gboolean temporary, gboolean shared, gboolean *do_free)
{
	g_assert ((temporary && do_free) || (!temporary && !do_free));

	DEBUG (printf ("get slot: %s %d\n", mono_type_full_name (&class->byval_arg), slot));

	if (klass->generic_class && !shared) {
		MonoRuntimeGenericContextInfoTemplate oti;
		gboolean tmp_do_free;

		oti = class_get_rgctx_template_oti (klass->generic_class->container_class,
											type_argc, slot, TRUE, FALSE, &tmp_do_free);
		if (oti.data) {
			gpointer info = oti.data;
			oti.data = inflate_info (&oti, &klass->generic_class->context, klass, temporary);
			if (tmp_do_free)
				free_inflated_info (oti.info_type, info);
		}
		if (temporary)
			*do_free = TRUE;

		return oti;
	} else {
		MonoRuntimeGenericContextTemplate *template_;
		MonoRuntimeGenericContextInfoTemplate *oti;

		template_ = mono_class_get_runtime_generic_context_template (klass);
		oti = rgctx_template_get_other_slot (template_, type_argc, slot);
		g_assert (oti);

		if (temporary)
			*do_free = FALSE;

		return *oti;
	}
}

static gpointer
class_type_info (MonoDomain *domain, MonoClass *klass, MonoRgctxInfoType info_type, MonoError *error)
{
	mono_error_init (error);

	switch (info_type) {
	case MONO_RGCTX_INFO_STATIC_DATA: {
		MonoVTable *vtable = mono_class_vtable (domain, klass);
		if (!vtable) {
			mono_error_set_exception_instance (error, mono_class_get_exception_for_failure (klass));
			return NULL;
		}
		return mono_vtable_get_static_field_data (vtable);
	}
	case MONO_RGCTX_INFO_KLASS:
		return klass;
	case MONO_RGCTX_INFO_ELEMENT_KLASS:
		return klass->element_class;
	case MONO_RGCTX_INFO_VTABLE: {
		MonoVTable *vtable = mono_class_vtable (domain, klass);
		if (!vtable) {
			mono_error_set_exception_instance (error, mono_class_get_exception_for_failure (klass));
			return NULL;
		}
		return vtable;
	}
	case MONO_RGCTX_INFO_CAST_CACHE: {
		/*First slot is the cache itself, the second the vtable.*/
		gpointer **cache_data = (gpointer **)mono_domain_alloc0 (domain, sizeof (gpointer) * 2);
		cache_data [1] = (gpointer *)klass;
		return cache_data;
	}
	case MONO_RGCTX_INFO_ARRAY_ELEMENT_SIZE:
		return GUINT_TO_POINTER (mono_class_array_element_size (klass));
	case MONO_RGCTX_INFO_VALUE_SIZE:
		if (MONO_TYPE_IS_REFERENCE (&klass->byval_arg))
			return GUINT_TO_POINTER (sizeof (gpointer));
		else
			return GUINT_TO_POINTER (mono_class_value_size (klass, NULL));
	case MONO_RGCTX_INFO_CLASS_BOX_TYPE:
		if (MONO_TYPE_IS_REFERENCE (&klass->byval_arg))
			return GUINT_TO_POINTER (MONO_GSHAREDVT_BOX_TYPE_REF);
		else if (mono_class_is_nullable (klass))
			return GUINT_TO_POINTER (MONO_GSHAREDVT_BOX_TYPE_NULLABLE);
		else
			return GUINT_TO_POINTER (MONO_GSHAREDVT_BOX_TYPE_VTYPE);
	case MONO_RGCTX_INFO_MEMCPY:
	case MONO_RGCTX_INFO_BZERO: {
		static MonoMethod *memcpy_method [17];
		static MonoMethod *bzero_method [17];
		MonoJitDomainInfo *domain_info;
		int size;
		guint32 align;

		domain_info = domain_jit_info (domain);

		if (MONO_TYPE_IS_REFERENCE (&klass->byval_arg)) {
			size = sizeof (gpointer);
			align = sizeof (gpointer);
		} else {
			size = mono_class_value_size (klass, &align);
		}

		if (size != 1 && size != 2 && size != 4 && size != 8)
			size = 0;
		if (align < size)
			size = 0;

		if (info_type == MONO_RGCTX_INFO_MEMCPY) {
			if (!memcpy_method [size]) {
				MonoMethod *m;
				char name [32];

				if (size == 0)
					sprintf (name, "memcpy");
				else
					sprintf (name, "memcpy_aligned_%d", size);
				m = mono_class_get_method_from_name (mono_defaults.string_class, name, 3);
				g_assert (m);
				mono_memory_barrier ();
				memcpy_method [size] = m;
			}
			if (!domain_info->memcpy_addr [size]) {
				gpointer addr = mono_compile_method (memcpy_method [size]);
				mono_memory_barrier ();
				domain_info->memcpy_addr [size] = (gpointer *)addr;
			}
			return domain_info->memcpy_addr [size];
		} else {
			if (!bzero_method [size]) {
				MonoMethod *m;
				char name [32];

				if (size == 0)
					sprintf (name, "bzero");
				else
					sprintf (name, "bzero_aligned_%d", size);
				m = mono_class_get_method_from_name (mono_defaults.string_class, name, 2);
				g_assert (m);
				mono_memory_barrier ();
				bzero_method [size] = m;
			}
			if (!domain_info->bzero_addr [size]) {
				gpointer addr = mono_compile_method (bzero_method [size]);
				mono_memory_barrier ();
				domain_info->bzero_addr [size] = (gpointer *)addr;
			}
			return domain_info->bzero_addr [size];
		}
	}
	case MONO_RGCTX_INFO_NULLABLE_CLASS_BOX:
	case MONO_RGCTX_INFO_NULLABLE_CLASS_UNBOX: {
		MonoMethod *method;
		gpointer addr, arg;
		MonoJitInfo *ji;
		MonoMethodSignature *sig, *gsig;
		MonoMethod *gmethod;

		if (!mono_class_is_nullable (klass))
			/* This can happen since all the entries in MonoGSharedVtMethodInfo are inflated, even those which are not used */
			return NULL;

		if (info_type == MONO_RGCTX_INFO_NULLABLE_CLASS_BOX)
			method = mono_class_get_method_from_name (klass, "Box", 1);
		else
			method = mono_class_get_method_from_name (klass, "Unbox", 1);

		addr = mono_jit_compile_method (method, error);
		if (!mono_error_ok (error))
			return NULL;

		// The caller uses the gsharedvt call signature

		if (mono_llvm_only) {
			/* FIXME: We have no access to the gsharedvt signature/gsctx used by the caller, so have to construct it ourselves */
			gmethod = mini_get_shared_method_full (method, FALSE, TRUE);
			sig = mono_method_signature (method);
			gsig = mono_method_signature (gmethod);

			addr = mini_add_method_wrappers_llvmonly (method, addr, TRUE, FALSE, &arg);
			return mini_create_llvmonly_ftndesc (domain, addr, arg);
		}

		ji = mini_jit_info_table_find (mono_domain_get (), (char *)mono_get_addr_from_ftnptr (addr), NULL);
		g_assert (ji);
		if (mini_jit_info_is_gsharedvt (ji))
			return mono_create_static_rgctx_trampoline (method, addr);
		else {
			/* Need to add an out wrapper */

			/* FIXME: We have no access to the gsharedvt signature/gsctx used by the caller, so have to construct it ourselves */
			gmethod = mini_get_shared_method_full (method, FALSE, TRUE);
			sig = mono_method_signature (method);
			gsig = mono_method_signature (gmethod);

			addr = mini_get_gsharedvt_wrapper (FALSE, addr, sig, gsig, -1, FALSE);
			addr = mono_create_static_rgctx_trampoline (method, addr);
			return addr;
		}
	}
	default:
		g_assert_not_reached ();
	}
	/* Not reached */
	return NULL;
}

static gboolean
ji_is_gsharedvt (MonoJitInfo *ji)
{
	if (ji && ji->has_generic_jit_info && (mono_jit_info_get_generic_sharing_context (ji)->is_gsharedvt))
		return TRUE;
	else
		return FALSE;
}

/*
 * Describes the information used to construct a gsharedvt arg trampoline.
 */
typedef struct {
	gboolean is_in;
	gboolean calli;
	gint32 vcall_offset;
	gpointer addr;
	MonoMethodSignature *sig, *gsig;
} GSharedVtTrampInfo;

static guint
tramp_info_hash (gconstpointer key)
{
	GSharedVtTrampInfo *tramp = (GSharedVtTrampInfo *)key;

	return (gsize)tramp->addr;
}

static gboolean
tramp_info_equal (gconstpointer a, gconstpointer b)
{
	GSharedVtTrampInfo *tramp1 = (GSharedVtTrampInfo *)a;
	GSharedVtTrampInfo *tramp2 = (GSharedVtTrampInfo *)b;

	/* The signatures should be internalized */
	return tramp1->is_in == tramp2->is_in && tramp1->calli == tramp2->calli && tramp1->vcall_offset == tramp2->vcall_offset &&
		tramp1->addr == tramp2->addr && tramp1->sig == tramp2->sig && tramp1->gsig == tramp2->gsig;
}

static MonoType*
get_wrapper_shared_type (MonoType *t)
{
	if (t->byref)
		return &mono_defaults.int_class->this_arg;
	t = mini_get_underlying_type (t);

	switch (t->type) {
	case MONO_TYPE_I1:
		/* This removes any attributes etc. */
		return &mono_defaults.sbyte_class->byval_arg;
	case MONO_TYPE_U1:
		return &mono_defaults.byte_class->byval_arg;
	case MONO_TYPE_I2:
		return &mono_defaults.int16_class->byval_arg;
	case MONO_TYPE_U2:
		return &mono_defaults.uint16_class->byval_arg;
	case MONO_TYPE_I4:
		return &mono_defaults.int32_class->byval_arg;
	case MONO_TYPE_U4:
		return &mono_defaults.uint32_class->byval_arg;
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_PTR:
		return &mono_defaults.int_class->byval_arg;
	case MONO_TYPE_GENERICINST: {
		MonoClass *klass;
		MonoGenericContext ctx;
		MonoGenericContext *orig_ctx;
		MonoGenericInst *inst;
		MonoType *args [16];
		int i;

		if (!MONO_TYPE_ISSTRUCT (t))
			return &mono_defaults.int_class->byval_arg;

		klass = mono_class_from_mono_type (t);
		orig_ctx = &klass->generic_class->context;

		memset (&ctx, 0, sizeof (MonoGenericContext));

		inst = orig_ctx->class_inst;
		if (inst) {
			g_assert (inst->type_argc < 16);
			for (i = 0; i < inst->type_argc; ++i)
				args [i] = get_wrapper_shared_type (inst->type_argv [i]);
			ctx.class_inst = mono_metadata_get_generic_inst (inst->type_argc, args);
		}
		inst = orig_ctx->method_inst;
		if (inst) {
			g_assert (inst->type_argc < 16);
			for (i = 0; i < inst->type_argc; ++i)
				args [i] = get_wrapper_shared_type (inst->type_argv [i]);
			ctx.method_inst = mono_metadata_get_generic_inst (inst->type_argc, args);
		}
		klass = mono_class_inflate_generic_class (klass->generic_class->container_class, &ctx);
		return &klass->byval_arg;
	}
#if SIZEOF_VOID_P == 8
	case MONO_TYPE_I8:
		return &mono_defaults.int_class->byval_arg;
#endif
	default:
		break;
	}

	//printf ("%s\n", mono_type_full_name (t));

	return t;
}

static MonoMethodSignature*
mini_get_underlying_signature (MonoMethodSignature *sig)
{
	MonoMethodSignature *res = mono_metadata_signature_dup (sig);
	int i;

	res->ret = get_wrapper_shared_type (sig->ret);
	for (i = 0; i < sig->param_count; ++i)
		res->params [i] = get_wrapper_shared_type (sig->params [i]);
	res->generic_param_count = 0;
	res->is_inflated = 0;

	return res;
}

/*
 * mini_get_gsharedvt_in_sig_wrapper:
 *
 *   Return a wrapper to translate between the normal and gsharedvt calling conventions of SIG.
 * The returned wrapper has a signature of SIG, plus one extra argument, which is an <addr, rgctx> pair.
 * The extra argument is passed the same way as an rgctx to shared methods.
 * It calls <addr> using the gsharedvt version of SIG, passing in <rgctx> as an extra argument.
 */
MonoMethod*
mini_get_gsharedvt_in_sig_wrapper (MonoMethodSignature *sig)
{
	MonoMethodBuilder *mb;
	MonoMethod *res, *cached;
	WrapperInfo *info;
	MonoMethodSignature *csig, *gsharedvt_sig;
	int i, pindex, retval_var;
	static GHashTable *cache;

	// FIXME: Memory management
	sig = mini_get_underlying_signature (sig);

	// FIXME: Normal cache
	if (!cache)
		cache = g_hash_table_new_full ((GHashFunc)mono_signature_hash, (GEqualFunc)mono_metadata_signature_equal, NULL, NULL);
	gshared_lock ();
	res = g_hash_table_lookup (cache, sig);
	gshared_unlock ();
	if (res) {
		g_free (sig);
		return res;
	}

	/* Create the signature for the wrapper */
	// FIXME:
	csig = g_malloc0 (MONO_SIZEOF_METHOD_SIGNATURE + ((sig->param_count + 1) * sizeof (MonoType*)));
	memcpy (csig, sig, mono_metadata_signature_size (sig));
	csig->param_count ++;
	csig->params [sig->param_count] = &mono_defaults.int_class->byval_arg;

	/* Create the signature for the gsharedvt callconv */
	gsharedvt_sig = g_malloc0 (MONO_SIZEOF_METHOD_SIGNATURE + ((sig->param_count + 2) * sizeof (MonoType*)));
	memcpy (gsharedvt_sig, sig, mono_metadata_signature_size (sig));
	pindex = 0;
	/* The return value is returned using an explicit vret argument */
	if (sig->ret->type != MONO_TYPE_VOID) {
		gsharedvt_sig->params [pindex ++] = &mono_defaults.int_class->byval_arg;
		gsharedvt_sig->ret = &mono_defaults.void_class->byval_arg;
	}
	for (i = 0; i < sig->param_count; i++) {
		gsharedvt_sig->params [pindex] = sig->params [i];
		if (!sig->params [i]->byref) {
			gsharedvt_sig->params [pindex] = mono_metadata_type_dup (NULL, gsharedvt_sig->params [pindex]);
			gsharedvt_sig->params [pindex]->byref = 1;
		}
		pindex ++;
	}
	/* Rgctx arg */
	gsharedvt_sig->params [pindex ++] = &mono_defaults.int_class->byval_arg;
	gsharedvt_sig->param_count = pindex;

	// FIXME: Use shared signatures
	mb = mono_mb_new (mono_defaults.object_class, sig->hasthis ? "gsharedvt_in_sig" : "gsharedvt_in_sig_static", MONO_WRAPPER_UNKNOWN);

#ifndef DISABLE_JIT
	if (sig->ret->type != MONO_TYPE_VOID)
		retval_var = mono_mb_add_local (mb, sig->ret);

	/* Make the call */
	if (sig->hasthis)
		mono_mb_emit_ldarg (mb, 0);
	if (sig->ret->type != MONO_TYPE_VOID)
		mono_mb_emit_ldloc_addr (mb, retval_var);
	for (i = 0; i < sig->param_count; i++) {
		if (sig->params [i]->byref)
			mono_mb_emit_ldarg (mb, i + (sig->hasthis == TRUE));
		else
			mono_mb_emit_ldarg_addr (mb, i + (sig->hasthis == TRUE));
	}
	/* Rgctx arg */
	mono_mb_emit_ldarg (mb, sig->param_count + (sig->hasthis ? 1 : 0));
	mono_mb_emit_icon (mb, sizeof (gpointer));
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	/* Method to call */
	mono_mb_emit_ldarg (mb, sig->param_count + (sig->hasthis ? 1 : 0));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_calli (mb, gsharedvt_sig);
	if (sig->ret->type != MONO_TYPE_VOID)
		mono_mb_emit_ldloc (mb, retval_var);
	mono_mb_emit_byte (mb, CEE_RET);
#endif

	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_GSHAREDVT_IN_SIG);
	info->d.gsharedvt.sig = sig;

	res = mono_mb_create (mb, csig, sig->param_count + 16, info);

	gshared_lock ();
	cached = g_hash_table_lookup (cache, sig);
	if (cached)
		res = cached;
	else
		g_hash_table_insert (cache, sig, res);
	gshared_unlock ();
	return res;
}

/*
 * mini_get_gsharedvt_out_sig_wrapper:
 *
 *   Same as in_sig_wrapper, but translate between the gsharedvt and normal signatures.
 */
MonoMethod*
mini_get_gsharedvt_out_sig_wrapper (MonoMethodSignature *sig)
{
	MonoMethodBuilder *mb;
	MonoMethod *res, *cached;
	WrapperInfo *info;
	MonoMethodSignature *normal_sig, *csig;
	int i, pindex, args_start, ldind_op, stind_op;
	static GHashTable *cache;

	// FIXME: Memory management
	sig = mini_get_underlying_signature (sig);

	// FIXME: Normal cache
	if (!cache)
		cache = g_hash_table_new_full ((GHashFunc)mono_signature_hash, (GEqualFunc)mono_metadata_signature_equal, NULL, NULL);
	gshared_lock ();
	res = g_hash_table_lookup (cache, sig);
	gshared_unlock ();
	if (res) {
		g_free (sig);
		return res;
	}

	/* Create the signature for the wrapper */
	// FIXME:
	csig = g_malloc0 (MONO_SIZEOF_METHOD_SIGNATURE + ((sig->param_count + 2) * sizeof (MonoType*)));
	memcpy (csig, sig, mono_metadata_signature_size (sig));
	pindex = 0;
	/* The return value is returned using an explicit vret argument */
	if (sig->ret->type != MONO_TYPE_VOID) {
		csig->params [pindex ++] = &mono_defaults.int_class->byval_arg;
		csig->ret = &mono_defaults.void_class->byval_arg;
	}
	args_start = pindex;
	if (sig->hasthis)
		args_start ++;
	for (i = 0; i < sig->param_count; i++) {
		csig->params [pindex] = sig->params [i];
		if (!sig->params [i]->byref) {
			csig->params [pindex] = mono_metadata_type_dup (NULL, csig->params [pindex]);
			csig->params [pindex]->byref = 1;
		}
		pindex ++;
	}
	/* Rgctx arg */
	csig->params [pindex ++] = &mono_defaults.int_class->byval_arg;
	csig->param_count = pindex;

	/* Create the signature for the normal callconv */
	normal_sig = g_malloc0 (MONO_SIZEOF_METHOD_SIGNATURE + ((sig->param_count + 2) * sizeof (MonoType*)));
	memcpy (normal_sig, sig, mono_metadata_signature_size (sig));
	normal_sig->param_count ++;
	normal_sig->params [sig->param_count] = &mono_defaults.int_class->byval_arg;

	// FIXME: Use shared signatures
	mb = mono_mb_new (mono_defaults.object_class, "gsharedvt_out_sig", MONO_WRAPPER_UNKNOWN);

#ifndef DISABLE_JIT
	if (sig->ret->type != MONO_TYPE_VOID)
		/* Load return address */
		mono_mb_emit_ldarg (mb, sig->hasthis ? 1 : 0);

	/* Make the call */
	if (sig->hasthis)
		mono_mb_emit_ldarg (mb, 0);
	for (i = 0; i < sig->param_count; i++) {
		if (sig->params [i]->byref) {
			mono_mb_emit_ldarg (mb, args_start + i);
		} else {
			ldind_op = mono_type_to_ldind (sig->params [i]);
			mono_mb_emit_ldarg (mb, args_start + i);
			// FIXME:
			if (ldind_op == CEE_LDOBJ)
				mono_mb_emit_op (mb, CEE_LDOBJ, mono_class_from_mono_type (sig->params [i]));
			else
				mono_mb_emit_byte (mb, ldind_op);
		}
	}
	/* Rgctx arg */
	mono_mb_emit_ldarg (mb, args_start + sig->param_count);
	mono_mb_emit_icon (mb, sizeof (gpointer));
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	/* Method to call */
	mono_mb_emit_ldarg (mb, args_start + sig->param_count);
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_calli (mb, normal_sig);
	if (sig->ret->type != MONO_TYPE_VOID) {
		/* Store return value */
		stind_op = mono_type_to_stind (sig->ret);
		// FIXME:
		if (stind_op == CEE_STOBJ)
			mono_mb_emit_op (mb, CEE_STOBJ, mono_class_from_mono_type (sig->ret));
		else
			mono_mb_emit_byte (mb, stind_op);
	}
	mono_mb_emit_byte (mb, CEE_RET);
#endif

	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_GSHAREDVT_OUT_SIG);
	info->d.gsharedvt.sig = sig;

	res = mono_mb_create (mb, csig, sig->param_count + 16, info);

	gshared_lock ();
	cached = g_hash_table_lookup (cache, sig);
	if (cached)
		res = cached;
	else
		g_hash_table_insert (cache, sig, res);
	gshared_unlock ();
	return res;
}

MonoMethodSignature*
mini_get_gsharedvt_out_sig_wrapper_signature (gboolean has_this, gboolean has_ret, int param_count)
{
	MonoMethodSignature *sig = g_malloc0 (sizeof (MonoMethodSignature) + (32 * sizeof (MonoType*)));
	int i, pindex;

	sig->ret = &mono_defaults.void_class->byval_arg;
	sig->sentinelpos = -1;
	pindex = 0;
	if (has_this)
		/* this */
		sig->params [pindex ++] = &mono_defaults.int_class->byval_arg;
	if (has_ret)
		/* vret */
		sig->params [pindex ++] = &mono_defaults.int_class->byval_arg;
	for (i = 0; i < param_count; ++i)
		/* byref arguments */
		sig->params [pindex ++] = &mono_defaults.int_class->byval_arg;
	/* extra arg */
	sig->params [pindex ++] = &mono_defaults.int_class->byval_arg;
	sig->param_count = pindex;

	return sig;
}

/*
 * mini_get_gsharedvt_wrapper:
 *
 *   Return a gsharedvt in/out wrapper for calling ADDR.
 */
gpointer
mini_get_gsharedvt_wrapper (gboolean gsharedvt_in, gpointer addr, MonoMethodSignature *normal_sig, MonoMethodSignature *gsharedvt_sig, gint32 vcall_offset, gboolean calli)
{
	static gboolean inited = FALSE;
	static int num_trampolines;
	gpointer res, info;
	MonoDomain *domain = mono_domain_get ();
	MonoJitDomainInfo *domain_info;
	GSharedVtTrampInfo *tramp_info;
	GSharedVtTrampInfo tinfo;

	if (!inited) {
		mono_counters_register ("GSHAREDVT arg trampolines", MONO_COUNTER_JIT | MONO_COUNTER_INT, &num_trampolines);
		inited = TRUE;
	}

	if (mono_llvm_only) {
		MonoMethod *wrapper;

		if (gsharedvt_in)
			wrapper = mini_get_gsharedvt_in_sig_wrapper (normal_sig);
		else
			wrapper = mini_get_gsharedvt_out_sig_wrapper (normal_sig);
		res = mono_compile_method (wrapper);
		return res;
	}

	memset (&tinfo, 0, sizeof (tinfo));
	tinfo.is_in = gsharedvt_in;
	tinfo.calli = calli;
	tinfo.vcall_offset = vcall_offset;
	tinfo.addr = addr;
	tinfo.sig = normal_sig;
	tinfo.gsig = gsharedvt_sig;

	domain_info = domain_jit_info (domain);

	/*
	 * The arg trampolines might only have a finite number in full-aot, so use a cache.
	 */
	mono_domain_lock (domain);
	if (!domain_info->gsharedvt_arg_tramp_hash)
		domain_info->gsharedvt_arg_tramp_hash = g_hash_table_new (tramp_info_hash, tramp_info_equal);
	res = g_hash_table_lookup (domain_info->gsharedvt_arg_tramp_hash, &tinfo);
	mono_domain_unlock (domain);
	if (res)
		return res;

	info = mono_arch_get_gsharedvt_call_info (addr, normal_sig, gsharedvt_sig, gsharedvt_in, vcall_offset, calli);

	if (gsharedvt_in) {
		static gpointer tramp_addr;
		MonoMethod *wrapper;

		if (!tramp_addr) {
			wrapper = mono_marshal_get_gsharedvt_in_wrapper ();
			addr = mono_compile_method (wrapper);
			mono_memory_barrier ();
			tramp_addr = addr;
		}
		addr = tramp_addr;
	} else {
		static gpointer tramp_addr;
		MonoMethod *wrapper;

		if (!tramp_addr) {
			wrapper = mono_marshal_get_gsharedvt_out_wrapper ();
			addr = mono_compile_method (wrapper);
			mono_memory_barrier ();
			tramp_addr = addr;
		}
		addr = tramp_addr;
	}

	if (mono_aot_only)
		addr = mono_aot_get_gsharedvt_arg_trampoline (info, addr);
	else
		addr = mono_arch_get_gsharedvt_arg_trampoline (mono_domain_get (), info, addr);

	num_trampolines ++;

	/* Cache it */
	tramp_info = (GSharedVtTrampInfo *)mono_domain_alloc0 (domain, sizeof (GSharedVtTrampInfo));
	memcpy (tramp_info, &tinfo, sizeof (GSharedVtTrampInfo));

	mono_domain_lock (domain);
	/* Duplicates are not a problem */
	g_hash_table_insert (domain_info->gsharedvt_arg_tramp_hash, tramp_info, addr);
	mono_domain_unlock (domain);

	return addr;
}

/*
 * instantiate_info:
 *
 *   Instantiate the info given by OTI for context CONTEXT.
 */
static gpointer
instantiate_info (MonoDomain *domain, MonoRuntimeGenericContextInfoTemplate *oti,
				  MonoGenericContext *context, MonoClass *klass, MonoError *error)
{
	gpointer data;
	gboolean temporary;

	mono_error_init (error);

	if (!oti->data)
		return NULL;

	switch (oti->info_type) {
	case MONO_RGCTX_INFO_STATIC_DATA:
	case MONO_RGCTX_INFO_KLASS:
	case MONO_RGCTX_INFO_ELEMENT_KLASS:
	case MONO_RGCTX_INFO_VTABLE:
	case MONO_RGCTX_INFO_CAST_CACHE:
		temporary = TRUE;
		break;
	default:
		temporary = FALSE;
	}

	data = inflate_info (oti, context, klass, temporary);

	switch (oti->info_type) {
	case MONO_RGCTX_INFO_STATIC_DATA:
	case MONO_RGCTX_INFO_KLASS:
	case MONO_RGCTX_INFO_ELEMENT_KLASS:
	case MONO_RGCTX_INFO_VTABLE:
	case MONO_RGCTX_INFO_CAST_CACHE:
	case MONO_RGCTX_INFO_ARRAY_ELEMENT_SIZE:
	case MONO_RGCTX_INFO_VALUE_SIZE:
	case MONO_RGCTX_INFO_CLASS_BOX_TYPE:
	case MONO_RGCTX_INFO_MEMCPY:
	case MONO_RGCTX_INFO_BZERO:
	case MONO_RGCTX_INFO_NULLABLE_CLASS_BOX:
	case MONO_RGCTX_INFO_NULLABLE_CLASS_UNBOX: {
		MonoClass *arg_class = mono_class_from_mono_type ((MonoType *)data);

		free_inflated_info (oti->info_type, data);
		g_assert (arg_class);

		/* The class might be used as an argument to
		   mono_value_copy(), which requires that its GC
		   descriptor has been computed. */
		if (oti->info_type == MONO_RGCTX_INFO_KLASS)
			mono_class_compute_gc_descriptor (arg_class);

		return class_type_info (domain, arg_class, oti->info_type, error);
	}
	case MONO_RGCTX_INFO_TYPE:
		return data;
	case MONO_RGCTX_INFO_REFLECTION_TYPE: {
		MonoReflectionType *ret = mono_type_get_object_checked (domain, (MonoType *)data, error);

		return ret;
	}
	case MONO_RGCTX_INFO_METHOD:
		return data;
	case MONO_RGCTX_INFO_GENERIC_METHOD_CODE: {
		MonoMethod *m = (MonoMethod*)data;
		gpointer addr;
		gpointer arg = NULL;

		if (mono_llvm_only) {
			addr = mono_compile_method (m);
			addr = mini_add_method_wrappers_llvmonly (m, addr, FALSE, FALSE, &arg);

			/* Returns an ftndesc */
			return mini_create_llvmonly_ftndesc (domain, addr, arg);
		} else {
			addr = mono_compile_method ((MonoMethod *)data);
			return mini_add_method_trampoline ((MonoMethod *)data, addr, mono_method_needs_static_rgctx_invoke ((MonoMethod *)data, FALSE), FALSE);
		}
	}
	case MONO_RGCTX_INFO_GSHAREDVT_OUT_WRAPPER: {
		MonoMethod *m = (MonoMethod*)data;
		gpointer addr;
		gpointer arg = NULL;

		g_assert (mono_llvm_only);

		addr = mono_compile_method (m);

		MonoJitInfo *ji;
		gboolean callee_gsharedvt;

		ji = mini_jit_info_table_find (mono_domain_get (), (char *)mono_get_addr_from_ftnptr (addr), NULL);
		g_assert (ji);
		callee_gsharedvt = mini_jit_info_is_gsharedvt (ji);
		if (callee_gsharedvt)
			callee_gsharedvt = mini_is_gsharedvt_variable_signature (mono_method_signature (jinfo_get_method (ji)));
		if (callee_gsharedvt) {
			/* No need for a wrapper */
			return mini_create_llvmonly_ftndesc (domain, addr, mini_method_get_rgctx (m));
		} else {
			addr = mini_add_method_wrappers_llvmonly (m, addr, TRUE, FALSE, &arg);

			/* Returns an ftndesc */
			return mini_create_llvmonly_ftndesc (domain, addr, arg);
		}
	}
	case MONO_RGCTX_INFO_VIRT_METHOD_CODE: {
		MonoJumpInfoVirtMethod *info = (MonoJumpInfoVirtMethod *)data;
		MonoClass *iface_class = info->method->klass;
		MonoMethod *method;
		int ioffset, slot;
		gpointer addr;

		mono_class_setup_vtable (info->klass);
		// FIXME: Check type load
		if (iface_class->flags & TYPE_ATTRIBUTE_INTERFACE) {
			ioffset = mono_class_interface_offset (info->klass, iface_class);
			g_assert (ioffset != -1);
		} else {
			ioffset = 0;
		}
		slot = mono_method_get_vtable_slot (info->method);
		g_assert (slot != -1);
		g_assert (info->klass->vtable);
		method = info->klass->vtable [ioffset + slot];

		method = mono_class_inflate_generic_method_checked (method, context, error);
		if (!mono_error_ok (error))
			return NULL;
		addr = mono_compile_method (method);
		return mini_add_method_trampoline (method, addr, mono_method_needs_static_rgctx_invoke (method, FALSE), FALSE);
	}
	case MONO_RGCTX_INFO_VIRT_METHOD_BOX_TYPE: {
		MonoJumpInfoVirtMethod *info = (MonoJumpInfoVirtMethod *)data;
		MonoClass *iface_class = info->method->klass;
		MonoMethod *method;
		MonoClass *impl_class;
		int ioffset, slot;

		mono_class_setup_vtable (info->klass);
		// FIXME: Check type load
		if (iface_class->flags & TYPE_ATTRIBUTE_INTERFACE) {
			ioffset = mono_class_interface_offset (info->klass, iface_class);
			g_assert (ioffset != -1);
		} else {
			ioffset = 0;
		}
		slot = mono_method_get_vtable_slot (info->method);
		g_assert (slot != -1);
		g_assert (info->klass->vtable);
		method = info->klass->vtable [ioffset + slot];

		impl_class = method->klass;
		if (MONO_TYPE_IS_REFERENCE (&impl_class->byval_arg))
			return GUINT_TO_POINTER (MONO_GSHAREDVT_BOX_TYPE_REF);
		else if (mono_class_is_nullable (impl_class))
			return GUINT_TO_POINTER (MONO_GSHAREDVT_BOX_TYPE_NULLABLE);
		else
			return GUINT_TO_POINTER (MONO_GSHAREDVT_BOX_TYPE_VTYPE);
	}
#ifndef DISABLE_REMOTING
	case MONO_RGCTX_INFO_REMOTING_INVOKE_WITH_CHECK:
		return mono_compile_method (mono_marshal_get_remoting_invoke_with_check ((MonoMethod *)data));
#endif
	case MONO_RGCTX_INFO_METHOD_DELEGATE_CODE:
		return mono_domain_alloc0 (domain, sizeof (gpointer));
	case MONO_RGCTX_INFO_CLASS_FIELD:
		return data;
	case MONO_RGCTX_INFO_FIELD_OFFSET: {
		MonoClassField *field = (MonoClassField *)data;

		/* The value is offset by 1 */
		if (field->parent->valuetype && !(field->type->attrs & FIELD_ATTRIBUTE_STATIC))
			return GUINT_TO_POINTER (field->offset - sizeof (MonoObject) + 1);
		else
			return GUINT_TO_POINTER (field->offset + 1);
	}
	case MONO_RGCTX_INFO_METHOD_RGCTX: {
		MonoMethodInflated *method = (MonoMethodInflated *)data;
		MonoVTable *vtable;

		g_assert (method->method.method.is_inflated);
		g_assert (method->context.method_inst);

		vtable = mono_class_vtable (domain, method->method.method.klass);
		if (!vtable) {
			mono_error_set_exception_instance (error, mono_class_get_exception_for_failure (method->method.method.klass));
			return NULL;
		}

		return mono_method_lookup_rgctx (vtable, method->context.method_inst);
	}
	case MONO_RGCTX_INFO_METHOD_CONTEXT: {
		MonoMethodInflated *method = (MonoMethodInflated *)data;

		g_assert (method->method.method.is_inflated);
		g_assert (method->context.method_inst);

		return method->context.method_inst;
	}
	case MONO_RGCTX_INFO_SIG_GSHAREDVT_IN_TRAMPOLINE_CALLI: {
		MonoMethodSignature *gsig = (MonoMethodSignature *)oti->data;
		MonoMethodSignature *sig = (MonoMethodSignature *)data;
		gpointer addr;

		/*
		 * This is an indirect call to the address passed by the caller in the rgctx reg.
		 */
		addr = mini_get_gsharedvt_wrapper (TRUE, NULL, sig, gsig, -1, TRUE);
		return addr;
	}
	case MONO_RGCTX_INFO_SIG_GSHAREDVT_OUT_TRAMPOLINE_CALLI: {
		MonoMethodSignature *gsig = (MonoMethodSignature *)oti->data;
		MonoMethodSignature *sig = (MonoMethodSignature *)data;
		gpointer addr;

		/*
		 * This is an indirect call to the address passed by the caller in the rgctx reg.
		 */
		addr = mini_get_gsharedvt_wrapper (FALSE, NULL, sig, gsig, -1, TRUE);
		return addr;
	}
	case MONO_RGCTX_INFO_METHOD_GSHAREDVT_OUT_TRAMPOLINE:
	case MONO_RGCTX_INFO_METHOD_GSHAREDVT_OUT_TRAMPOLINE_VIRT: {
		MonoJumpInfoGSharedVtCall *call_info = (MonoJumpInfoGSharedVtCall *)data;
		MonoMethodSignature *call_sig;
		MonoMethod *method;
		gpointer addr;
		MonoJitInfo *callee_ji;
		gboolean virtual_ = oti->info_type == MONO_RGCTX_INFO_METHOD_GSHAREDVT_OUT_TRAMPOLINE_VIRT;
		gint32 vcall_offset;
		gboolean callee_gsharedvt;

		/* This is the original generic signature used by the caller */
		call_sig = call_info->sig;
		/* This is the instantiated method which is called */
		method = call_info->method;

		g_assert (method->is_inflated);

		if (!virtual_)
			addr = mono_compile_method (method);
		else
			addr = NULL;

		if (virtual_) {
			/* Same as in mono_emit_method_call_full () */
			if ((method->klass->parent == mono_defaults.multicastdelegate_class) && (!strcmp (method->name, "Invoke"))) {
				/* See mono_emit_method_call_full () */
				/* The gsharedvt trampoline will recognize this constant */
				vcall_offset = MONO_GSHAREDVT_DEL_INVOKE_VT_OFFSET;
			} else if (method->klass->flags & TYPE_ATTRIBUTE_INTERFACE) {
				guint32 imt_slot = mono_method_get_imt_slot (method);
				vcall_offset = ((gint32)imt_slot - MONO_IMT_SIZE) * SIZEOF_VOID_P;
			} else {
				vcall_offset = G_STRUCT_OFFSET (MonoVTable, vtable) +
					((mono_method_get_vtable_index (method)) * (SIZEOF_VOID_P));
			}
		} else {
			vcall_offset = -1;
		}

		// FIXME: This loads information in the AOT case
		callee_ji = mini_jit_info_table_find (mono_domain_get (), (char *)mono_get_addr_from_ftnptr (addr), NULL);
		callee_gsharedvt = ji_is_gsharedvt (callee_ji);

		/*
		 * For gsharedvt calls made out of gsharedvt methods, the callee could end up being a gsharedvt method, or a normal
		 * non-shared method. The latter call cannot be patched, so instead of using a normal call, we make an indirect
		 * call through the rgctx, in effect patching the rgctx entry instead of the call site.
		 * For virtual calls, the caller might be a normal or a gsharedvt method. Since there is only one vtable slot,
		 * this difference needs to be handed on the caller side. This is currently implemented by adding a gsharedvt-in
		 * trampoline to all gsharedvt methods and storing this trampoline into the vtable slot. Virtual calls made from
		 * gsharedvt methods always go through a gsharedvt-out trampoline, so the calling sequence is:
		 * caller -> out trampoline -> in trampoline -> callee
		 * This is not very efficient, but it is easy to implement.
		 */
		if (virtual_ || !callee_gsharedvt) {
			MonoMethodSignature *sig, *gsig;

			g_assert (method->is_inflated);

			sig = mono_method_signature (method);
			gsig = call_sig;

			if (mono_llvm_only) {
				if (mini_is_gsharedvt_variable_signature (call_sig)) {
					/* The virtual case doesn't go through this code */
					g_assert (!virtual_);

					sig = mono_method_signature (jinfo_get_method (callee_ji));
					gpointer out_wrapper = mini_get_gsharedvt_wrapper (FALSE, NULL, sig, gsig, -1, FALSE);
					MonoFtnDesc *out_wrapper_arg = mini_create_llvmonly_ftndesc (domain, callee_ji->code_start, mini_method_get_rgctx (method));

					/* Returns an ftndesc */
					addr = mini_create_llvmonly_ftndesc (domain, out_wrapper, out_wrapper_arg);
				} else {
					addr = mini_create_llvmonly_ftndesc (domain, addr, mini_method_get_rgctx (method));
				}
			} else {
				addr = mini_get_gsharedvt_wrapper (FALSE, addr, sig, gsig, vcall_offset, FALSE);
			}
#if 0
			if (virtual)
				printf ("OUT-VCALL: %s\n", mono_method_full_name (method, TRUE));
			else
				printf ("OUT: %s\n", mono_method_full_name (method, TRUE));
#endif
		} else if (callee_gsharedvt) {
			MonoMethodSignature *sig, *gsig;

			/*
			 * This is a combination of the out and in cases, since both the caller and the callee are gsharedvt methods.
			 * The caller and the callee can use different gsharedvt signatures, so we have to add both an out and an in
			 * trampoline, i.e.:
			 * class Base<T> {
			 *   public void foo<T1> (T1 t1, T t, object o) {}
			 * }
			 * class AClass : Base<long> {
			 * public void bar<T> (T t, long time, object o) {
			 *   foo (t, time, o);
			 * }
			 * }
			 * Here, the caller uses !!0,long, while the callee uses !!0,!0
			 * FIXME: Optimize this.
			 */

			if (mono_llvm_only) {
				/* Both wrappers receive an extra <addr, rgctx> argument */
				sig = mono_method_signature (method);
				gsig = mono_method_signature (jinfo_get_method (callee_ji));

				/* Return a function descriptor */

				if (mini_is_gsharedvt_variable_signature (call_sig)) {
					/*
					 * This is not an optimization, but its needed, since the concrete signature 'sig'
					 * might not exist at all in IL, so the AOT compiler cannot generate the wrappers
					 * for it.
					 */
					addr = mini_create_llvmonly_ftndesc (domain, callee_ji->code_start, mini_method_get_rgctx (method));
				} else if (mini_is_gsharedvt_variable_signature (gsig)) {
					gpointer in_wrapper = mini_get_gsharedvt_wrapper (TRUE, callee_ji->code_start, sig, gsig, -1, FALSE);

					gpointer in_wrapper_arg = mini_create_llvmonly_ftndesc (domain, callee_ji->code_start, mini_method_get_rgctx (method));

					addr = mini_create_llvmonly_ftndesc (domain, in_wrapper, in_wrapper_arg);
				} else {
					addr = mini_create_llvmonly_ftndesc (domain, addr, mini_method_get_rgctx (method));
				}
			} else if (call_sig == mono_method_signature (method)) {
			} else {
				sig = mono_method_signature (method);
				gsig = mono_method_signature (jinfo_get_method (callee_ji)); 

				addr = mini_get_gsharedvt_wrapper (TRUE, callee_ji->code_start, sig, gsig, -1, FALSE);

				sig = mono_method_signature (method);
				gsig = call_sig;

				addr = mini_get_gsharedvt_wrapper (FALSE, addr, sig, gsig, -1, FALSE);

				//printf ("OUT-IN-RGCTX: %s\n", mono_method_full_name (method, TRUE));
			}
		}

		return addr;
	}
	case MONO_RGCTX_INFO_METHOD_GSHAREDVT_INFO: {
		MonoGSharedVtMethodInfo *info = (MonoGSharedVtMethodInfo *)data;
		MonoGSharedVtMethodRuntimeInfo *res;
		MonoType *t;
		int i, offset, align, size;

		// FIXME:
		res = (MonoGSharedVtMethodRuntimeInfo *)g_malloc0 (sizeof (MonoGSharedVtMethodRuntimeInfo) + (info->num_entries * sizeof (gpointer)));

		offset = 0;
		for (i = 0; i < info->num_entries; ++i) {
			MonoRuntimeGenericContextInfoTemplate *template_ = &info->entries [i];

			switch (template_->info_type) {
			case MONO_RGCTX_INFO_LOCAL_OFFSET:
				t = (MonoType *)template_->data;

				size = mono_type_size (t, &align);

				if (align < sizeof (gpointer))
					align = sizeof (gpointer);
				if (MONO_TYPE_ISSTRUCT (t) && align < 2 * sizeof (gpointer))
					align = 2 * sizeof (gpointer);
			
				// FIXME: Do the same things as alloc_stack_slots
				offset += align - 1;
				offset &= ~(align - 1);
				res->entries [i] = GINT_TO_POINTER (offset);
				offset += size;
				break;
			default:
				res->entries [i] = instantiate_info (domain, template_, context, klass, error);
				if (!mono_error_ok (error))
					return NULL;
				break;
			}
		}
		res->locals_size = offset;

		return res;
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
fill_in_rgctx_template_slot (MonoClass *klass, int type_argc, int index, gpointer data, MonoRgctxInfoType info_type)
{
	MonoRuntimeGenericContextTemplate *template_ = mono_class_get_runtime_generic_context_template (klass);
	MonoClass *subclass;

	rgctx_template_set_slot (klass->image, template_, type_argc, index, data, info_type);

	/* Recurse for all subclasses */
	if (generic_subclass_hash)
		subclass = (MonoClass *)g_hash_table_lookup (generic_subclass_hash, klass);
	else
		subclass = NULL;

	while (subclass) {
		MonoRuntimeGenericContextInfoTemplate subclass_oti;
		MonoRuntimeGenericContextTemplate *subclass_template = class_lookup_rgctx_template (subclass);

		g_assert (subclass_template);

		subclass_oti = class_get_rgctx_template_oti (subclass->parent, type_argc, index, FALSE, FALSE, NULL);
		g_assert (subclass_oti.data);

		fill_in_rgctx_template_slot (subclass, type_argc, index, subclass_oti.data, info_type);

		subclass = subclass_template->next_subclass;
	}
}

const char*
mono_rgctx_info_type_to_str (MonoRgctxInfoType type)
{
	switch (type) {
	case MONO_RGCTX_INFO_STATIC_DATA: return "STATIC_DATA";
	case MONO_RGCTX_INFO_KLASS: return "KLASS";
	case MONO_RGCTX_INFO_ELEMENT_KLASS: return "ELEMENT_KLASS";
	case MONO_RGCTX_INFO_VTABLE: return "VTABLE";
	case MONO_RGCTX_INFO_TYPE: return "TYPE";
	case MONO_RGCTX_INFO_REFLECTION_TYPE: return "REFLECTION_TYPE";
	case MONO_RGCTX_INFO_METHOD: return "METHOD";
	case MONO_RGCTX_INFO_METHOD_GSHAREDVT_INFO: return "GSHAREDVT_INFO";
	case MONO_RGCTX_INFO_GENERIC_METHOD_CODE: return "GENERIC_METHOD_CODE";
	case MONO_RGCTX_INFO_GSHAREDVT_OUT_WRAPPER: return "GSHAREDVT_OUT_WRAPPER";
	case MONO_RGCTX_INFO_CLASS_FIELD: return "CLASS_FIELD";
	case MONO_RGCTX_INFO_METHOD_RGCTX: return "METHOD_RGCTX";
	case MONO_RGCTX_INFO_METHOD_CONTEXT: return "METHOD_CONTEXT";
	case MONO_RGCTX_INFO_REMOTING_INVOKE_WITH_CHECK: return "REMOTING_INVOKE_WITH_CHECK";
	case MONO_RGCTX_INFO_METHOD_DELEGATE_CODE: return "METHOD_DELEGATE_CODE";
	case MONO_RGCTX_INFO_CAST_CACHE: return "CAST_CACHE";
	case MONO_RGCTX_INFO_ARRAY_ELEMENT_SIZE: return "ARRAY_ELEMENT_SIZE";
	case MONO_RGCTX_INFO_VALUE_SIZE: return "VALUE_SIZE";
	case MONO_RGCTX_INFO_CLASS_BOX_TYPE: return "CLASS_BOX_TYPE";
	case MONO_RGCTX_INFO_FIELD_OFFSET: return "FIELD_OFFSET";
	case MONO_RGCTX_INFO_METHOD_GSHAREDVT_OUT_TRAMPOLINE: return "METHOD_GSHAREDVT_OUT_TRAMPOLINE";
	case MONO_RGCTX_INFO_METHOD_GSHAREDVT_OUT_TRAMPOLINE_VIRT: return "METHOD_GSHAREDVT_OUT_TRAMPOLINE_VIRT";
	case MONO_RGCTX_INFO_SIG_GSHAREDVT_IN_TRAMPOLINE_CALLI: return "SIG_GSHAREDVT_IN_TRAMPOLINE_CALLI";
	case MONO_RGCTX_INFO_SIG_GSHAREDVT_OUT_TRAMPOLINE_CALLI: return "SIG_GSHAREDVT_OUT_TRAMPOLINE_CALLI";
	case MONO_RGCTX_INFO_MEMCPY: return "MEMCPY";
	case MONO_RGCTX_INFO_BZERO: return "BZERO";
	case MONO_RGCTX_INFO_NULLABLE_CLASS_BOX: return "NULLABLE_CLASS_BOX";
	case MONO_RGCTX_INFO_NULLABLE_CLASS_UNBOX: return "NULLABLE_CLASS_UNBOX";
	case MONO_RGCTX_INFO_VIRT_METHOD_CODE: return "VIRT_METHOD_CODE";
	case MONO_RGCTX_INFO_VIRT_METHOD_BOX_TYPE: return "VIRT_METHOD_BOX_TYPE";
	default:
		return "<UNKNOWN RGCTX INFO TYPE>";
	}
}

G_GNUC_UNUSED static char*
rgctx_info_to_str (MonoRgctxInfoType info_type, gpointer data)
{
	switch (info_type) {
	case MONO_RGCTX_INFO_VTABLE:
		return mono_type_full_name ((MonoType*)data);
	default:
		return g_strdup_printf ("<%p>", data);
	}
}

/*
 * LOCKING: loader lock
 */
static int
register_info (MonoClass *klass, int type_argc, gpointer data, MonoRgctxInfoType info_type)
{
	int i;
	MonoRuntimeGenericContextTemplate *template_ = mono_class_get_runtime_generic_context_template (klass);
	MonoClass *parent;
	MonoRuntimeGenericContextInfoTemplate *oti;

	for (i = 0, oti = get_info_templates (template_, type_argc); oti; ++i, oti = oti->next) {
		if (!oti->data)
			break;
	}

	DEBUG (printf ("set slot %s, infos [%d] = %s, %s\n", mono_type_get_full_name (class), i, mono_rgctx_info_type_to_str (info_type), rgctx_info_to_str (info_type, data)));

	/* Mark the slot as used in all parent classes (until we find
	   a parent class which already has it marked used). */
	parent = klass->parent;
	while (parent != NULL) {
		MonoRuntimeGenericContextTemplate *parent_template;
		MonoRuntimeGenericContextInfoTemplate *oti;

		if (parent->generic_class)
			parent = parent->generic_class->container_class;

		parent_template = mono_class_get_runtime_generic_context_template (parent);
		oti = rgctx_template_get_other_slot (parent_template, type_argc, i);

		if (oti && oti->data)
			break;

		rgctx_template_set_slot (parent->image, parent_template, type_argc, i,
								 MONO_RGCTX_SLOT_USED_MARKER, (MonoRgctxInfoType)0);

		parent = parent->parent;
	}

	/* Fill in the slot in this class and in all subclasses
	   recursively. */
	fill_in_rgctx_template_slot (klass, type_argc, i, data, info_type);

	return i;
}

static gboolean
info_equal (gpointer data1, gpointer data2, MonoRgctxInfoType info_type)
{
	switch (info_type) {
	case MONO_RGCTX_INFO_STATIC_DATA:
	case MONO_RGCTX_INFO_KLASS:
	case MONO_RGCTX_INFO_ELEMENT_KLASS:
	case MONO_RGCTX_INFO_VTABLE:
	case MONO_RGCTX_INFO_TYPE:
	case MONO_RGCTX_INFO_REFLECTION_TYPE:
	case MONO_RGCTX_INFO_CAST_CACHE:
	case MONO_RGCTX_INFO_ARRAY_ELEMENT_SIZE:
	case MONO_RGCTX_INFO_VALUE_SIZE:
	case MONO_RGCTX_INFO_CLASS_BOX_TYPE:
	case MONO_RGCTX_INFO_MEMCPY:
	case MONO_RGCTX_INFO_BZERO:
	case MONO_RGCTX_INFO_NULLABLE_CLASS_BOX:
	case MONO_RGCTX_INFO_NULLABLE_CLASS_UNBOX:
		return mono_class_from_mono_type ((MonoType *)data1) == mono_class_from_mono_type ((MonoType *)data2);
	case MONO_RGCTX_INFO_METHOD:
	case MONO_RGCTX_INFO_METHOD_GSHAREDVT_INFO:
	case MONO_RGCTX_INFO_GENERIC_METHOD_CODE:
	case MONO_RGCTX_INFO_GSHAREDVT_OUT_WRAPPER:
	case MONO_RGCTX_INFO_CLASS_FIELD:
	case MONO_RGCTX_INFO_FIELD_OFFSET:
	case MONO_RGCTX_INFO_METHOD_RGCTX:
	case MONO_RGCTX_INFO_METHOD_CONTEXT:
	case MONO_RGCTX_INFO_REMOTING_INVOKE_WITH_CHECK:
	case MONO_RGCTX_INFO_METHOD_DELEGATE_CODE:
	case MONO_RGCTX_INFO_METHOD_GSHAREDVT_OUT_TRAMPOLINE:
	case MONO_RGCTX_INFO_METHOD_GSHAREDVT_OUT_TRAMPOLINE_VIRT:
	case MONO_RGCTX_INFO_SIG_GSHAREDVT_IN_TRAMPOLINE_CALLI:
	case MONO_RGCTX_INFO_SIG_GSHAREDVT_OUT_TRAMPOLINE_CALLI:
		return data1 == data2;
	case MONO_RGCTX_INFO_VIRT_METHOD_CODE:
	case MONO_RGCTX_INFO_VIRT_METHOD_BOX_TYPE: {
		MonoJumpInfoVirtMethod *info1 = (MonoJumpInfoVirtMethod *)data1;
		MonoJumpInfoVirtMethod *info2 = (MonoJumpInfoVirtMethod *)data2;

		return info1->klass == info2->klass && info1->method == info2->method;
	}
	default:
		g_assert_not_reached ();
	}
	/* never reached */
	return FALSE;
}

/*
 * mini_rgctx_info_type_to_patch_info_type:
 *
 *   Return the type of the runtime object referred to by INFO_TYPE.
 */
MonoJumpInfoType
mini_rgctx_info_type_to_patch_info_type (MonoRgctxInfoType info_type)
{
	switch (info_type) {
	case MONO_RGCTX_INFO_STATIC_DATA:
	case MONO_RGCTX_INFO_KLASS:
	case MONO_RGCTX_INFO_ELEMENT_KLASS:
	case MONO_RGCTX_INFO_VTABLE:
	case MONO_RGCTX_INFO_TYPE:
	case MONO_RGCTX_INFO_REFLECTION_TYPE:
	case MONO_RGCTX_INFO_CAST_CACHE:
	case MONO_RGCTX_INFO_ARRAY_ELEMENT_SIZE:
	case MONO_RGCTX_INFO_VALUE_SIZE:
	case MONO_RGCTX_INFO_CLASS_BOX_TYPE:
	case MONO_RGCTX_INFO_MEMCPY:
	case MONO_RGCTX_INFO_BZERO:
	case MONO_RGCTX_INFO_NULLABLE_CLASS_BOX:
	case MONO_RGCTX_INFO_NULLABLE_CLASS_UNBOX:
	case MONO_RGCTX_INFO_LOCAL_OFFSET:
		return MONO_PATCH_INFO_CLASS;
	case MONO_RGCTX_INFO_FIELD_OFFSET:
		return MONO_PATCH_INFO_FIELD;
	default:
		g_assert_not_reached ();
		return (MonoJumpInfoType)-1;
	}
}

static int
lookup_or_register_info (MonoClass *klass, int type_argc, gpointer data, MonoRgctxInfoType info_type,
	MonoGenericContext *generic_context)
{
	static gboolean inited = FALSE;
	static int max_slot = 0;

	MonoRuntimeGenericContextTemplate *rgctx_template =
		mono_class_get_runtime_generic_context_template (klass);
	MonoRuntimeGenericContextInfoTemplate *oti_list, *oti;
	int i;

	klass = get_shared_class (klass);

	mono_loader_lock ();

	if (info_has_identity (info_type)) {
		oti_list = get_info_templates (rgctx_template, type_argc);

		for (oti = oti_list, i = 0; oti; oti = oti->next, ++i) {
			gpointer inflated_data;

			if (oti->info_type != info_type || !oti->data)
				continue;

			inflated_data = inflate_info (oti, generic_context, klass, TRUE);

			if (info_equal (data, inflated_data, info_type)) {
				free_inflated_info (info_type, inflated_data);
				mono_loader_unlock ();
				return i;
			}
			free_inflated_info (info_type, inflated_data);
		}
	}

	/* We haven't found the info */
	i = register_info (klass, type_argc, data, info_type);

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
 * mono_method_lookup_or_register_info:
 * @method: a method
 * @in_mrgctx: whether to put the data into the MRGCTX
 * @data: the info data
 * @info_type: the type of info to register about data
 * @generic_context: a generic context
 *
 * Looks up and, if necessary, adds information about data/info_type in
 * method's or method's class runtime generic context.  Returns the
 * encoded slot number.
 */
guint32
mono_method_lookup_or_register_info (MonoMethod *method, gboolean in_mrgctx, gpointer data,
	MonoRgctxInfoType info_type, MonoGenericContext *generic_context)
{
	MonoClass *klass = method->klass;
	int type_argc, index;

	if (in_mrgctx) {
		MonoGenericInst *method_inst = mono_method_get_context (method)->method_inst;

		g_assert (method->is_inflated && method_inst);
		type_argc = method_inst->type_argc;
		g_assert (type_argc > 0);
	} else {
		type_argc = 0;
	}

	index = lookup_or_register_info (klass, type_argc, data, info_type, generic_context);

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
	gpointer *array = (gpointer *)mono_domain_alloc0 (domain, size);

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
							  MonoGenericInst *method_inst, MonoError *error)
{
	gpointer info;
	int i, first_slot, size;
	MonoDomain *domain = class_vtable->domain;
	MonoClass *klass = class_vtable->klass;
	MonoGenericContext *class_context = klass->generic_class ? &klass->generic_class->context : NULL;
	MonoRuntimeGenericContextInfoTemplate oti;
	MonoGenericContext context = { class_context ? class_context->class_inst : NULL, method_inst };
	int rgctx_index;
	gboolean do_free;

	mono_error_init (error);

	g_assert (rgctx);

	mono_domain_lock (domain);

	/* First check whether that slot isn't already instantiated.
	   This might happen because lookup doesn't lock.  Allocate
	   arrays on the way. */
	first_slot = 0;
	size = mono_class_rgctx_get_array_size (0, method_inst != NULL);
	if (method_inst)
		size -= MONO_SIZEOF_METHOD_RUNTIME_GENERIC_CONTEXT / sizeof (gpointer);
	for (i = 0; ; ++i) {
		int offset;

		if (method_inst && i == 0)
			offset = MONO_SIZEOF_METHOD_RUNTIME_GENERIC_CONTEXT / sizeof (gpointer);
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
		rgctx = (void **)rgctx [offset + 0];
		first_slot += size - 1;
		size = mono_class_rgctx_get_array_size (i + 1, method_inst != NULL);
	}

	g_assert (!rgctx [rgctx_index]);

	mono_domain_unlock (domain);

	oti = class_get_rgctx_template_oti (get_shared_class (klass),
										method_inst ? method_inst->type_argc : 0, slot, TRUE, TRUE, &do_free);
	/* This might take the loader lock */
	info = instantiate_info (domain, &oti, &context, klass, error);
	g_assert (info);

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
 * Instantiates a slot in the RGCTX, returning its value.
 */
gpointer
mono_class_fill_runtime_generic_context (MonoVTable *class_vtable, guint32 slot, MonoError *error)
{
	static gboolean inited = FALSE;
	static int num_alloced = 0;

	MonoDomain *domain = class_vtable->domain;
	MonoRuntimeGenericContext *rgctx;
	gpointer info;

	mono_error_init (error);

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

	info = fill_runtime_generic_context (class_vtable, rgctx, slot, 0, error);

	DEBUG (printf ("get rgctx slot: %s %d -> %p\n", mono_type_full_name (&class_vtable->klass->byval_arg), slot, info));

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
mono_method_fill_runtime_generic_context (MonoMethodRuntimeGenericContext *mrgctx, guint32 slot, MonoError *error)
{
	gpointer info;

	info = fill_runtime_generic_context (mrgctx->class_vtable, (MonoRuntimeGenericContext*)mrgctx, slot, mrgctx->method_inst, error);

	return info;
}

static guint
mrgctx_hash_func (gconstpointer key)
{
	const MonoMethodRuntimeGenericContext *mrgctx = (const MonoMethodRuntimeGenericContext *)key;

	return mono_aligned_addr_hash (mrgctx->class_vtable) ^ mono_metadata_generic_inst_hash (mrgctx->method_inst);
}

static gboolean
mrgctx_equal_func (gconstpointer a, gconstpointer b)
{
	const MonoMethodRuntimeGenericContext *mrgctx1 = (const MonoMethodRuntimeGenericContext *)a;
	const MonoMethodRuntimeGenericContext *mrgctx2 = (const MonoMethodRuntimeGenericContext *)b;

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

	mrgctx = (MonoMethodRuntimeGenericContext *)g_hash_table_lookup (domain->method_rgctx_hash, &key);

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
generic_inst_is_sharable (MonoGenericInst *inst, gboolean allow_type_vars,
						  gboolean allow_partial);

static gboolean
type_is_sharable (MonoType *type, gboolean allow_type_vars, gboolean allow_partial)
{
	if (allow_type_vars && (type->type == MONO_TYPE_VAR || type->type == MONO_TYPE_MVAR)) {
		MonoType *constraint = type->data.generic_param->gshared_constraint;
		if (!constraint)
			return TRUE;
		type = constraint;
	}

	if (MONO_TYPE_IS_REFERENCE (type))
		return TRUE;

	/* Allow non ref arguments if they are primitive types or enums (partial sharing). */
	if (allow_partial && !type->byref && (((type->type >= MONO_TYPE_BOOLEAN) && (type->type <= MONO_TYPE_R8)) || (type->type == MONO_TYPE_I) || (type->type == MONO_TYPE_U) || (type->type == MONO_TYPE_VALUETYPE && type->data.klass->enumtype)))
		return TRUE;

	if (allow_partial && !type->byref && type->type == MONO_TYPE_GENERICINST && MONO_TYPE_ISSTRUCT (type)) {
		MonoGenericClass *gclass = type->data.generic_class;

		if (gclass->context.class_inst && !generic_inst_is_sharable (gclass->context.class_inst, allow_type_vars, allow_partial))
			return FALSE;
		if (gclass->context.method_inst && !generic_inst_is_sharable (gclass->context.method_inst, allow_type_vars, allow_partial))
			return FALSE;
		if (mono_class_is_nullable (mono_class_from_mono_type (type)))
			return FALSE;
		return TRUE;
	}

	return FALSE;
}

static gboolean
generic_inst_is_sharable (MonoGenericInst *inst, gboolean allow_type_vars,
						  gboolean allow_partial)
{
	int i;

	for (i = 0; i < inst->type_argc; ++i) {
		if (!type_is_sharable (inst->type_argv [i], allow_type_vars, allow_partial))
			return FALSE;
	}

	return TRUE;
}

/*
 * mono_is_partially_sharable_inst:
 *
 *   Return TRUE if INST has ref and non-ref type arguments.
 */
gboolean
mono_is_partially_sharable_inst (MonoGenericInst *inst)
{
	int i;
	gboolean has_refs = FALSE, has_non_refs = FALSE;

	for (i = 0; i < inst->type_argc; ++i) {
		if (MONO_TYPE_IS_REFERENCE (inst->type_argv [i]) || inst->type_argv [i]->type == MONO_TYPE_VAR || inst->type_argv [i]->type == MONO_TYPE_MVAR)
			has_refs = TRUE;
		else
			has_non_refs = TRUE;
	}

	return has_refs && has_non_refs;
}

/*
 * mono_generic_context_is_sharable_full:
 * @context: a generic context
 *
 * Returns whether the generic context is sharable.  A generic context
 * is sharable iff all of its type arguments are reference type, or some of them have a
 * reference type, and ALLOW_PARTIAL is TRUE.
 */
gboolean
mono_generic_context_is_sharable_full (MonoGenericContext *context,
									   gboolean allow_type_vars,
									   gboolean allow_partial)
{
	g_assert (context->class_inst || context->method_inst);

	if (context->class_inst && !generic_inst_is_sharable (context->class_inst, allow_type_vars, allow_partial))
		return FALSE;

	if (context->method_inst && !generic_inst_is_sharable (context->method_inst, allow_type_vars, allow_partial))
		return FALSE;

	return TRUE;
}

gboolean
mono_generic_context_is_sharable (MonoGenericContext *context, gboolean allow_type_vars)
{
	return mono_generic_context_is_sharable_full (context, allow_type_vars, partial_sharing_supported ());
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
	if (method->is_inflated)
		return TRUE;
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

static gboolean
mini_method_is_open (MonoMethod *method)
{
	if (method->is_inflated) {
		MonoGenericContext *ctx = mono_method_get_context (method);

		if (ctx->class_inst && ctx->class_inst->is_open)
			return TRUE;
		if (ctx->method_inst && ctx->method_inst->is_open)
			return TRUE;
	}
	return FALSE;
}

/* Lazy class loading functions */
static GENERATE_TRY_GET_CLASS_WITH_CACHE (iasync_state_machine, System.Runtime.CompilerServices, IAsyncStateMachine)
static GENERATE_TRY_GET_CLASS_WITH_CACHE (async_state_machine_attribute, System.Runtime.CompilerServices, AsyncStateMachineAttribute)


static G_GNUC_UNUSED gboolean
is_async_state_machine_class (MonoClass *klass)
{
	MonoClass *iclass;

	return FALSE;

	iclass = mono_class_try_get_iasync_state_machine_class ();

	if (iclass && klass->valuetype && mono_class_is_assignable_from (iclass, klass))
		return TRUE;
	return FALSE;
}

static G_GNUC_UNUSED gboolean
is_async_method (MonoMethod *method)
{
	MonoCustomAttrInfo *cattr;
	MonoMethodSignature *sig;
	gboolean res = FALSE;
	MonoClass *attr_class;

	return FALSE;

	attr_class = mono_class_try_get_iasync_state_machine_class ();

	/* Do less expensive checks first */
	sig = mono_method_signature (method);
	if (attr_class && sig && ((sig->ret->type == MONO_TYPE_VOID) ||
				(sig->ret->type == MONO_TYPE_CLASS && !strcmp (sig->ret->data.generic_class->container_class->name, "Task")) ||
				(sig->ret->type == MONO_TYPE_GENERICINST && !strcmp (sig->ret->data.generic_class->container_class->name, "Task`1")))) {
		//printf ("X: %s\n", mono_method_full_name (method, TRUE));
		cattr = mono_custom_attrs_from_method (method);
		if (cattr) {
			if (mono_custom_attrs_has_attr (cattr, attr_class))
				res = TRUE;
			mono_custom_attrs_free (cattr);
		}
	}
	return res;
}

/*
 * mono_method_is_generic_sharable_full:
 * @method: a method
 * @allow_type_vars: whether to regard type variables as reference types
 * @allow_partial: whether to allow partial sharing
 * @allow_gsharedvt: whenever to allow sharing over valuetypes
 *
 * Returns TRUE iff the method is inflated or part of an inflated
 * class, its context is sharable and it has no constraints on its
 * type parameters.  Otherwise returns FALSE.
 */
gboolean
mono_method_is_generic_sharable_full (MonoMethod *method, gboolean allow_type_vars,
										   gboolean allow_partial, gboolean allow_gsharedvt)
{
	if (!mono_method_is_generic_impl (method))
		return FALSE;

	/*
	if (!mono_debug_count ())
		allow_partial = FALSE;
	*/

	if (!partial_sharing_supported ())
		allow_partial = FALSE;

	if (mono_class_is_nullable (method->klass))
		// FIXME:
		allow_partial = FALSE;

	if (method->klass->image->dynamic)
		/*
		 * Enabling this causes corlib test failures because the JIT encounters generic instances whose
		 * instance_size is 0.
		 */
		allow_partial = FALSE;

	/*
	 * Generic async methods have an associated state machine class which is a generic struct. This struct
	 * is too large to be handled by gsharedvt so we make it visible to the AOT compiler by disabling sharing
	 * of the async method and the state machine class.
	 */
	if (is_async_state_machine_class (method->klass))
		return FALSE;

	if (allow_gsharedvt && mini_is_gsharedvt_sharable_method (method)) {
		if (is_async_method (method))
			return FALSE;
		return TRUE;
	}

	if (method->is_inflated) {
		MonoMethodInflated *inflated = (MonoMethodInflated*)method;
		MonoGenericContext *context = &inflated->context;

		if (!mono_generic_context_is_sharable_full (context, allow_type_vars, allow_partial))
			return FALSE;

		g_assert (inflated->declaring);

		if (inflated->declaring->is_generic) {
			if (has_constraints (mono_method_get_generic_container (inflated->declaring)))
				return FALSE;
		}
	}

	if (method->klass->generic_class) {
		if (!mono_generic_context_is_sharable_full (&method->klass->generic_class->context, allow_type_vars, allow_partial))
			return FALSE;

		g_assert (method->klass->generic_class->container_class &&
				method->klass->generic_class->container_class->generic_container);

		if (has_constraints (method->klass->generic_class->container_class->generic_container))
			return FALSE;
	}

	if (method->klass->generic_container && !allow_type_vars)
		return FALSE;

	/* This does potentially expensive cattr checks, so do it at the end */
	if (is_async_method (method)) {
		if (mini_method_is_open (method))
			/* The JIT can't compile these without sharing */
			return TRUE;
		return FALSE;
	}

	return TRUE;
}

gboolean
mono_method_is_generic_sharable (MonoMethod *method, gboolean allow_type_vars)
{
	return mono_method_is_generic_sharable_full (method, allow_type_vars, partial_sharing_supported (), TRUE);
}

/*
 * mono_method_needs_static_rgctx_invoke:
 *
 *   Return whenever METHOD needs an rgctx argument.
 * An rgctx argument is needed when the method is generic sharable, but it doesn't
 * have a this argument which can be used to load the rgctx.
 */
gboolean
mono_method_needs_static_rgctx_invoke (MonoMethod *method, gboolean allow_type_vars)
{
	if (!mono_class_generic_sharing_enabled (method->klass))
		return FALSE;

	if (!mono_method_is_generic_sharable (method, allow_type_vars))
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

	type_argv = (MonoType **)alloca (sizeof (MonoType*) * type_argc);

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

static gboolean gshared_supported;
static gboolean gsharedvt_supported;

void
mono_set_generic_sharing_supported (gboolean supported)
{
	gshared_supported = supported;
}

void
mono_set_generic_sharing_vt_supported (gboolean supported)
{
	gsharedvt_supported = supported;
}

void
mono_set_partial_sharing_supported (gboolean supported)
{
	partial_supported = supported;
}

/*
 * mono_class_generic_sharing_enabled:
 * @class: a class
 *
 * Returns whether generic sharing is enabled for class.
 *
 * This is a stop-gap measure to slowly introduce generic sharing
 * until we have all the issues sorted out, at which time this
 * function will disappear and generic sharing will always be enabled.
 */
gboolean
mono_class_generic_sharing_enabled (MonoClass *klass)
{
	if (gshared_supported)
		return TRUE;
	else
		return FALSE;
}

MonoGenericContext*
mini_method_get_context (MonoMethod *method)
{
	return mono_method_get_context_general (method, TRUE);
}

/*
 * mono_method_check_context_used:
 * @method: a method
 *
 * Checks whether the method's generic context uses a type variable.
 * Returns an int with the bits MONO_GENERIC_CONTEXT_USED_CLASS and
 * MONO_GENERIC_CONTEXT_USED_METHOD set to reflect whether the
 * context's class or method instantiation uses type variables.
 */
int
mono_method_check_context_used (MonoMethod *method)
{
	MonoGenericContext *method_context = mini_method_get_context (method);
	int context_used = 0;

	if (!method_context) {
		/* It might be a method of an array of an open generic type */
		if (method->klass->rank)
			context_used = mono_class_check_context_used (method->klass);
	} else {
		context_used = mono_generic_context_check_used (method_context);
		context_used |= mono_class_check_context_used (method->klass);
	}

	return context_used;
}

static gboolean
generic_inst_equal (MonoGenericInst *inst1, MonoGenericInst *inst2)
{
	int i;

	if (!inst1) {
		g_assert (!inst2);
		return TRUE;
	}

	g_assert (inst2);

	if (inst1->type_argc != inst2->type_argc)
		return FALSE;

	for (i = 0; i < inst1->type_argc; ++i)
		if (!mono_metadata_type_equal (inst1->type_argv [i], inst2->type_argv [i]))
			return FALSE;

	return TRUE;
}

/*
 * mono_generic_context_equal_deep:
 * @context1: a generic context
 * @context2: a generic context
 *
 * Returns whether context1's type arguments are equal to context2's
 * type arguments.
 */
gboolean
mono_generic_context_equal_deep (MonoGenericContext *context1, MonoGenericContext *context2)
{
	return generic_inst_equal (context1->class_inst, context2->class_inst) &&
		generic_inst_equal (context1->method_inst, context2->method_inst);
}

/*
 * mini_class_get_container_class:
 * @class: a generic class
 *
 * Returns the class's container class, which is the class itself if
 * it doesn't have generic_class set.
 */
MonoClass*
mini_class_get_container_class (MonoClass *klass)
{
	if (klass->generic_class)
		return klass->generic_class->container_class;

	g_assert (klass->generic_container);
	return klass;
}

/*
 * mini_class_get_context:
 * @class: a generic class
 *
 * Returns the class's generic context.
 */
MonoGenericContext*
mini_class_get_context (MonoClass *klass)
{
	if (klass->generic_class)
		return &klass->generic_class->context;

	g_assert (klass->generic_container);
	return &klass->generic_container->context;
}

/*
 * mini_get_basic_type_from_generic:
 * @type: a type
 *
 * Returns a closed type corresponding to the possibly open type
 * passed to it.
 */
static MonoType*
mini_get_basic_type_from_generic (MonoType *type)
{
	if (!type->byref && (type->type == MONO_TYPE_VAR || type->type == MONO_TYPE_MVAR) && mini_is_gsharedvt_type (type))
		return type;
	else if (!type->byref && (type->type == MONO_TYPE_VAR || type->type == MONO_TYPE_MVAR)) {
		MonoType *constraint = type->data.generic_param->gshared_constraint;
		/* The gparam serial encodes the type this gparam can represent */
		if (!constraint) {
			return &mono_defaults.object_class->byval_arg;
		} else {
			MonoClass *klass;

			g_assert (constraint != &mono_defaults.int_class->parent->byval_arg);
			klass = mono_class_from_mono_type (constraint);
			return &klass->byval_arg;
		}
	} else {
		return mini_native_type_replace_type (mono_type_get_basic_type_from_generic (type));
	}
}

/*
 * mini_type_get_underlying_type:
 *
 *   Return the underlying type of TYPE, taking into account enums, byref, bool, char and generic
 * sharing.
 */
MonoType*
mini_type_get_underlying_type (MonoType *type)
{
	type = mini_native_type_replace_type (type);

	if (type->byref)
		return &mono_defaults.int_class->byval_arg;
	if (!type->byref && (type->type == MONO_TYPE_VAR || type->type == MONO_TYPE_MVAR) && mini_is_gsharedvt_type (type))
		return type;
	type = mini_get_basic_type_from_generic (mono_type_get_underlying_type (type));
	switch (type->type) {
	case MONO_TYPE_BOOLEAN:
		return &mono_defaults.byte_class->byval_arg;
	case MONO_TYPE_CHAR:
		return &mono_defaults.uint16_class->byval_arg;
	case MONO_TYPE_STRING:
		return &mono_defaults.object_class->byval_arg;
	default:
		return type;
	}
}

/*
 * mini_type_stack_size:
 * @t: a type
 * @align: Pointer to an int for returning the alignment
 *
 * Returns the type's stack size and the alignment in *align.
 */
int
mini_type_stack_size (MonoType *t, int *align)
{
	return mono_type_stack_size_internal (t, align, TRUE);
}

/*
 * mini_type_stack_size_full:
 *
 *   Same as mini_type_stack_size, but handle pinvoke data types as well.
 */
int
mini_type_stack_size_full (MonoType *t, guint32 *align, gboolean pinvoke)
{
	int size;

	//g_assert (!mini_is_gsharedvt_type (t));

	if (pinvoke) {
		size = mono_type_native_stack_size (t, align);
	} else {
		int ialign;

		if (align) {
			size = mini_type_stack_size (t, &ialign);
			*align = ialign;
		} else {
			size = mini_type_stack_size (t, NULL);
		}
	}
	
	return size;
}

/*
 * mono_generic_sharing_init:
 *
 * Initialize the module.
 */
void
mono_generic_sharing_init (void)
{
	mono_counters_register ("RGCTX template num allocted", MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &num_templates_allocted);
	mono_counters_register ("RGCTX template bytes allocted", MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &num_templates_bytes);
	mono_counters_register ("RGCTX oti num allocted", MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &num_oti_allocted);
	mono_counters_register ("RGCTX oti bytes allocted", MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &num_oti_bytes);

	mono_install_image_unload_hook (mono_class_unregister_image_generic_subclasses, NULL);

	mono_os_mutex_init_recursive (&gshared_mutex);
}

void
mono_generic_sharing_cleanup (void)
{
	mono_remove_image_unload_hook (mono_class_unregister_image_generic_subclasses, NULL);

	if (generic_subclass_hash)
		g_hash_table_destroy (generic_subclass_hash);
}

/*
 * mini_type_var_is_vt:
 *
 *   Return whenever T is a type variable instantiated with a vtype.
 */
gboolean
mini_type_var_is_vt (MonoType *type)
{
	if (type->type == MONO_TYPE_VAR || type->type == MONO_TYPE_MVAR) {
		return type->data.generic_param->gshared_constraint && (type->data.generic_param->gshared_constraint->type == MONO_TYPE_VALUETYPE || type->data.generic_param->gshared_constraint->type == MONO_TYPE_GENERICINST);
	} else {
		g_assert_not_reached ();
		return FALSE;
	}
}

gboolean
mini_type_is_reference (MonoType *type)
{
	type = mini_type_get_underlying_type (type);
	return mono_type_is_reference (type);
}

/*
 * mini_method_get_rgctx:
 *
 *  Return the RGCTX which needs to be passed to M when it is called.
 */
gpointer
mini_method_get_rgctx (MonoMethod *m)
{
	if (mini_method_get_context (m)->method_inst)
		return mono_method_lookup_rgctx (mono_class_vtable (mono_domain_get (), m->klass), mini_method_get_context (m)->method_inst);
	else
		return mono_class_vtable (mono_domain_get (), m->klass);
}

/*
 * mini_type_is_vtype:
 *
 *   Return whenever T is a vtype, or a type param instantiated with a vtype.
 * Should be used in place of MONO_TYPE_ISSTRUCT () which can't handle gsharedvt.
 */
gboolean
mini_type_is_vtype (MonoType *t)
{
	t = mini_type_get_underlying_type (t);

	return MONO_TYPE_ISSTRUCT (t) || mini_is_gsharedvt_variable_type (t);
}

gboolean
mini_class_is_generic_sharable (MonoClass *klass)
{
	if (klass->generic_class && is_async_state_machine_class (klass))
		return FALSE;

	return (klass->generic_class && mono_generic_context_is_sharable (&klass->generic_class->context, FALSE));
}

gboolean
mini_is_gsharedvt_variable_klass (MonoClass *klass)
{
	return mini_is_gsharedvt_variable_type (&klass->byval_arg);
}

gboolean
mini_is_gsharedvt_gparam (MonoType *t)
{
	/* Matches get_gsharedvt_type () */
	return (t->type == MONO_TYPE_VAR || t->type == MONO_TYPE_MVAR) && t->data.generic_param->gshared_constraint && t->data.generic_param->gshared_constraint->type == MONO_TYPE_VALUETYPE;
}

static char*
get_shared_gparam_name (MonoTypeEnum constraint, const char *name)
{
	if (constraint == MONO_TYPE_VALUETYPE) {
		return g_strdup_printf ("%s_GSHAREDVT", name);
	} else if (constraint == MONO_TYPE_OBJECT) {
		return g_strdup_printf ("%s_REF", name);
	} else if (constraint == MONO_TYPE_GENERICINST) {
		return g_strdup_printf ("%s_INST", name);
	} else {
		MonoType t;
		char *tname, *tname2, *res;

		memset (&t, 0, sizeof (t));
		t.type = constraint;
		tname = mono_type_full_name (&t);
		tname2 = g_utf8_strup (tname, strlen (tname));
		res = g_strdup_printf ("%s_%s", name, tname2);
		g_free (tname);
		g_free (tname2);
		return res;
	}
}

static guint
shared_gparam_hash (gconstpointer data)
{
	MonoGSharedGenericParam *p = (MonoGSharedGenericParam*)data;
	guint hash;

	hash = mono_metadata_generic_param_hash (p->parent);
	hash = ((hash << 5) - hash) ^ mono_metadata_type_hash (p->param.param.gshared_constraint);

	return hash;
}

static gboolean
shared_gparam_equal (gconstpointer ka, gconstpointer kb)
{
	MonoGSharedGenericParam *p1 = (MonoGSharedGenericParam*)ka;
	MonoGSharedGenericParam *p2 = (MonoGSharedGenericParam*)kb;

	if (p1 == p2)
		return TRUE;
	if (p1->parent != p2->parent)
		return FALSE;
	if (!mono_metadata_type_equal (p1->param.param.gshared_constraint, p2->param.param.gshared_constraint))
		return FALSE;
	return TRUE;
}

/*
 * mini_get_shared_gparam:
 *
 *   Create an anonymous gparam from T with a constraint which encodes which types can match it.
 */
MonoType*
mini_get_shared_gparam (MonoType *t, MonoType *constraint)
{
	MonoGenericParam *par = t->data.generic_param;
	MonoGSharedGenericParam *copy, key;
	MonoType *res;
	MonoImage *image = NULL;
	char *name;

	memset (&key, 0, sizeof (key));
	key.parent = par;
	key.param.param.gshared_constraint = constraint;

	g_assert (mono_generic_param_info (par));
	image = get_image_for_generic_param(par);

	/*
	 * Need a cache to ensure the newly created gparam
	 * is unique wrt T/CONSTRAINT.
	 */
	mono_image_lock (image);
	if (!image->gshared_types) {
		image->gshared_types_len = MONO_TYPE_INTERNAL;
		image->gshared_types = g_new0 (GHashTable*, image->gshared_types_len);
	}
	if (!image->gshared_types [constraint->type])
		image->gshared_types [constraint->type] = g_hash_table_new (shared_gparam_hash, shared_gparam_equal);
	res = (MonoType *)g_hash_table_lookup (image->gshared_types [constraint->type], &key);
	mono_image_unlock (image);
	if (res)
		return res;
	copy = (MonoGSharedGenericParam *)mono_image_alloc0 (image, sizeof (MonoGSharedGenericParam));
	memcpy (&copy->param, par, sizeof (MonoGenericParamFull));
	copy->param.info.pklass = NULL;
	name = get_shared_gparam_name (constraint->type, ((MonoGenericParamFull*)copy)->info.name);
	copy->param.info.name = mono_image_strdup (image, name);
	g_free (name);

	copy->param.param.owner = par->owner;

	copy->param.param.gshared_constraint = constraint;
	copy->parent = par;
	res = mono_metadata_type_dup (NULL, t);
	res->data.generic_param = (MonoGenericParam*)copy;

	if (image) {
		mono_image_lock (image);
		/* Duplicates are ok */
		g_hash_table_insert (image->gshared_types [constraint->type], copy, res);
		mono_image_unlock (image);
	}

	return res;
}

static MonoGenericInst*
get_shared_inst (MonoGenericInst *inst, MonoGenericInst *shared_inst, MonoGenericContainer *container, gboolean all_vt, gboolean gsharedvt, gboolean partial);

static MonoType*
get_shared_type (MonoType *t, MonoType *type)
{
	MonoTypeEnum ttype;

	if (!type->byref && type->type == MONO_TYPE_GENERICINST && MONO_TYPE_ISSTRUCT (type)) {
		MonoGenericClass *gclass = type->data.generic_class;
		MonoGenericContext context;
		MonoClass *k;

		memset (&context, 0, sizeof (context));
		if (gclass->context.class_inst)
			context.class_inst = get_shared_inst (gclass->context.class_inst, gclass->container_class->generic_container->context.class_inst, NULL, FALSE, FALSE, TRUE);
		if (gclass->context.method_inst)
			context.method_inst = get_shared_inst (gclass->context.method_inst, gclass->container_class->generic_container->context.method_inst, NULL, FALSE, FALSE, TRUE);

		k = mono_class_inflate_generic_class (gclass->container_class, &context);

		return mini_get_shared_gparam (t, &k->byval_arg);
	} else if (MONO_TYPE_ISSTRUCT (type)) {
		return type;
	}

	/* Create a type variable with a constraint which encodes which types can match it */
	ttype = type->type;
	if (type->type == MONO_TYPE_VALUETYPE) {
		ttype = mono_class_enum_basetype (type->data.klass)->type;
	} else if (MONO_TYPE_IS_REFERENCE (type)) {
		ttype = MONO_TYPE_OBJECT;
	} else if (type->type == MONO_TYPE_VAR || type->type == MONO_TYPE_MVAR) {
		if (type->data.generic_param->gshared_constraint)
			return mini_get_shared_gparam (t, type->data.generic_param->gshared_constraint);
		ttype = MONO_TYPE_OBJECT;
	}

	{
		MonoType t2;
		MonoClass *klass;

		memset (&t2, 0, sizeof (t2));
		t2.type = ttype;
		klass = mono_class_from_mono_type (&t2);

		return mini_get_shared_gparam (t, &klass->byval_arg);
	}
}

static MonoType*
get_gsharedvt_type (MonoType *t)
{
	/* Use TypeHandle as the constraint type since its a valuetype */
	return mini_get_shared_gparam (t, &mono_defaults.typehandle_class->byval_arg);
}

static MonoGenericInst*
get_shared_inst (MonoGenericInst *inst, MonoGenericInst *shared_inst, MonoGenericContainer *container, gboolean all_vt, gboolean gsharedvt, gboolean partial)
{
	MonoGenericInst *res;
	MonoType **type_argv;
	int i;

	type_argv = g_new0 (MonoType*, inst->type_argc);
	for (i = 0; i < inst->type_argc; ++i) {
		if (all_vt || gsharedvt) {
			type_argv [i] = get_gsharedvt_type (shared_inst->type_argv [i]);
		} else {
			/* These types match the ones in generic_inst_is_sharable () */
			type_argv [i] = get_shared_type (shared_inst->type_argv [i], inst->type_argv [i]);
		}
	}

	res = mono_metadata_get_generic_inst (inst->type_argc, type_argv);
	g_free (type_argv);
	return res;
}

/*
 * mini_get_shared_method_full:
 *
 *   Return the method which is actually compiled/registered when doing generic sharing.
 * If ALL_VT is true, return the shared method belonging to an all-vtype instantiation.
 * If IS_GSHAREDVT is true, treat METHOD as a gsharedvt method even if it fails some constraints.
 * METHOD can be a non-inflated generic method.
 */
MonoMethod*
mini_get_shared_method_full (MonoMethod *method, gboolean all_vt, gboolean is_gsharedvt)
{
	MonoError error;
	MonoGenericContext shared_context;
	MonoMethod *declaring_method, *res;
	gboolean partial = FALSE;
	gboolean gsharedvt = FALSE;
	MonoGenericContainer *class_container, *method_container = NULL;
	MonoGenericContext *context = mono_method_get_context (method);
	MonoGenericInst *inst;

	if (method->is_generic || (method->klass->generic_container && !method->is_inflated)) {
		declaring_method = method;
	} else {
		declaring_method = mono_method_get_declaring_generic_method (method);
	}

	/* shared_context is the context containing type variables. */
	if (declaring_method->is_generic)
		shared_context = mono_method_get_generic_container (declaring_method)->context;
	else
		shared_context = declaring_method->klass->generic_container->context;

	if (!is_gsharedvt)
		partial = mono_method_is_generic_sharable_full (method, FALSE, TRUE, FALSE);

	gsharedvt = is_gsharedvt || (!partial && mini_is_gsharedvt_sharable_method (method));

	class_container = declaring_method->klass->generic_container;
	method_container = mono_method_get_generic_container (declaring_method);

	/*
	 * Create the shared context by replacing the ref type arguments with
	 * type parameters, and keeping the rest.
	 */
	if (context)
		inst = context->class_inst;
	else
		inst = shared_context.class_inst;
	if (inst)
		shared_context.class_inst = get_shared_inst (inst, shared_context.class_inst, class_container, all_vt, gsharedvt, partial);

	if (context)
		inst = context->method_inst;
	else
		inst = shared_context.method_inst;
	if (inst)
		shared_context.method_inst = get_shared_inst (inst, shared_context.method_inst, method_container, all_vt, gsharedvt, partial);

	res = mono_class_inflate_generic_method_checked (declaring_method, &shared_context, &error);
	g_assert (mono_error_ok (&error)); /* FIXME don't swallow the error */

	//printf ("%s\n", mono_method_full_name (res, 1));

	return res;
}

MonoMethod*
mini_get_shared_method (MonoMethod *method)
{
	return mini_get_shared_method_full (method, FALSE, FALSE);
}

int
mini_get_rgctx_entry_slot (MonoJumpInfoRgctxEntry *entry)
{
	guint32 slot = -1;

	switch (entry->data->type) {
	case MONO_PATCH_INFO_CLASS:
		slot = mono_method_lookup_or_register_info (entry->method, entry->in_mrgctx, &entry->data->data.klass->byval_arg, entry->info_type, mono_method_get_context (entry->method));
		break;
	case MONO_PATCH_INFO_METHOD:
	case MONO_PATCH_INFO_METHODCONST:
		slot = mono_method_lookup_or_register_info (entry->method, entry->in_mrgctx, entry->data->data.method, entry->info_type, mono_method_get_context (entry->method));
		break;
	case MONO_PATCH_INFO_FIELD:
		slot = mono_method_lookup_or_register_info (entry->method, entry->in_mrgctx, entry->data->data.field, entry->info_type, mono_method_get_context (entry->method));
		break;
	case MONO_PATCH_INFO_SIGNATURE:
		slot = mono_method_lookup_or_register_info (entry->method, entry->in_mrgctx, entry->data->data.sig, entry->info_type, mono_method_get_context (entry->method));
		break;
	case MONO_PATCH_INFO_GSHAREDVT_CALL: {
		MonoJumpInfoGSharedVtCall *call_info = (MonoJumpInfoGSharedVtCall *)g_malloc0 (sizeof (MonoJumpInfoGSharedVtCall)); //mono_domain_alloc0 (domain, sizeof (MonoJumpInfoGSharedVtCall));

		memcpy (call_info, entry->data->data.gsharedvt, sizeof (MonoJumpInfoGSharedVtCall));
		slot = mono_method_lookup_or_register_info (entry->method, entry->in_mrgctx, call_info, entry->info_type, mono_method_get_context (entry->method));
		break;
	}
	case MONO_PATCH_INFO_GSHAREDVT_METHOD: {
		MonoGSharedVtMethodInfo *info;
		MonoGSharedVtMethodInfo *oinfo = entry->data->data.gsharedvt_method;
		int i;

		/* Make a copy into the domain mempool */
		info = (MonoGSharedVtMethodInfo *)g_malloc0 (sizeof (MonoGSharedVtMethodInfo)); //mono_domain_alloc0 (domain, sizeof (MonoGSharedVtMethodInfo));
		info->method = oinfo->method;
		info->num_entries = oinfo->num_entries;
		info->entries = (MonoRuntimeGenericContextInfoTemplate *)g_malloc0 (sizeof (MonoRuntimeGenericContextInfoTemplate) * info->num_entries);
		for (i = 0; i < oinfo->num_entries; ++i) {
			MonoRuntimeGenericContextInfoTemplate *otemplate = &oinfo->entries [i];
			MonoRuntimeGenericContextInfoTemplate *template_ = &info->entries [i];

			memcpy (template_, otemplate, sizeof (MonoRuntimeGenericContextInfoTemplate));
		}
		slot = mono_method_lookup_or_register_info (entry->method, entry->in_mrgctx, info, entry->info_type, mono_method_get_context (entry->method));
		break;
	}
	case MONO_PATCH_INFO_VIRT_METHOD: {
		MonoJumpInfoVirtMethod *info;
		MonoJumpInfoVirtMethod *oinfo = entry->data->data.virt_method;

		info = (MonoJumpInfoVirtMethod *)g_malloc0 (sizeof (MonoJumpInfoVirtMethod));
		memcpy (info, oinfo, sizeof (MonoJumpInfoVirtMethod));
		slot = mono_method_lookup_or_register_info (entry->method, entry->in_mrgctx, info, entry->info_type, mono_method_get_context (entry->method));
		break;
	}
	default:
		g_assert_not_reached ();
		break;
	}

	return slot;
}

#if defined(ENABLE_GSHAREDVT)

#include "../../../mono-extensions/mono/mini/mini-generic-sharing-gsharedvt.c"

#else

gboolean
mini_is_gsharedvt_type (MonoType *t)
{
	return FALSE;
}

gboolean
mini_is_gsharedvt_klass (MonoClass *klass)
{
	return FALSE;
}

gboolean
mini_is_gsharedvt_signature (MonoMethodSignature *sig)
{
	return FALSE;
}

gboolean
mini_is_gsharedvt_variable_type (MonoType *t)
{
	return FALSE;
}

gboolean
mini_is_gsharedvt_sharable_method (MonoMethod *method)
{
	return FALSE;
}

gboolean
mini_is_gsharedvt_variable_signature (MonoMethodSignature *sig)
{
	return FALSE;
}

#endif /* !MONOTOUCH */
