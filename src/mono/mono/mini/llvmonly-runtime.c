/**
 * \file
 * llvmonly runtime support code.
 *
 */

#include <config.h>
#include "llvmonly-runtime.h"
#include "aot-runtime.h"

/*
 * mini_llvmonly_load_method:
 *
 *   Return the AOT-ed code METHOD, or an interpreter entry for it.
 *
 */
gpointer
mini_llvmonly_load_method (MonoMethod *method, gboolean caller_gsharedvt, gboolean need_unbox, gpointer *out_arg, MonoError *error)
{
	gpointer addr = mono_compile_method_checked (method, error);

	if (!is_ok (error)) {
		mono_error_cleanup (error);
		error_init_reuse (error);
	}

	if (addr) {
		return mini_llvmonly_add_method_wrappers (method, (gpointer)addr, caller_gsharedvt, need_unbox, out_arg);
	} else {
		MonoFtnDesc *desc = mini_get_interp_callbacks ()->create_method_pointer_llvmonly (method, need_unbox, error);
		return_val_if_nok (error, NULL);
		*out_arg = desc->arg;
		return desc->addr;
	}
}

/*
 * Same but returns an ftndesc which might be newly allocated.
 */
MonoFtnDesc*
mini_llvmonly_load_method_ftndesc (MonoMethod *method, gboolean caller_gsharedvt, gboolean need_unbox, MonoError *error)
{
	gpointer addr = mono_compile_method_checked (method, error);
	return_val_if_nok (error, NULL);

	if (addr) {
		gpointer arg = NULL;
		addr = mini_llvmonly_add_method_wrappers (method, (gpointer)addr, caller_gsharedvt, need_unbox, &arg);
		// FIXME: Cache this
		return mini_llvmonly_create_ftndesc (mono_domain_get (), addr, arg);
	} else {
		MonoFtnDesc *ftndesc = mini_get_interp_callbacks ()->create_method_pointer_llvmonly (method, need_unbox, error);
		return_val_if_nok (error, NULL);
		return ftndesc;
	}
}

/*
 * Same as load_method, but for delegates.
 * See mini_llvmonly_get_delegate_arg ().
 */
gpointer
mini_llvmonly_load_method_delegate (MonoMethod *method, gboolean caller_gsharedvt, gboolean need_unbox, gpointer *out_arg, MonoError *error)
{
	gpointer addr = mono_compile_method_checked (method, error);
	return_val_if_nok (error, NULL);

	if (addr) {
		if (need_unbox)
			addr = mono_aot_get_unbox_trampoline (method, NULL);
		*out_arg = mini_llvmonly_get_delegate_arg (method, addr);
		return addr;
	} else {
		MonoFtnDesc *desc = mini_get_interp_callbacks ()->create_method_pointer_llvmonly (method, need_unbox, error);
		return_val_if_nok (error, NULL);

		g_assert (!caller_gsharedvt);
		*out_arg = desc->arg;
		return desc->addr;
	}
}

gpointer
mini_llvmonly_get_delegate_arg (MonoMethod *method, gpointer method_ptr)
{
	gpointer arg = NULL;

	if (mono_method_needs_static_rgctx_invoke (method, FALSE))
		arg = mini_method_get_rgctx (method);

	/*
	 * Avoid adding gsharedvt in wrappers since they might not exist if
	 * this delegate is called through a gsharedvt delegate invoke wrapper.
	 * Instead, encode that the method is gsharedvt in del->extra_arg,
	 * the CEE_MONO_CALLI_EXTRA_ARG implementation in the JIT depends on this.
	 */
	g_assert ((((gsize)arg) & 1) == 0);
	if (method->is_inflated && (mono_aot_get_method_flags ((guint8*)method_ptr) & MONO_AOT_METHOD_FLAG_GSHAREDVT_VARIABLE)) {
		arg = (gpointer)(((gsize)arg) | 1);
	}
	return arg;
}


/*
 * mini_llvmonly_create_ftndesc:
 *
 *   Create a function descriptor of the form <addr, arg>, which
 * represents a callee ADDR with ARG as the last argument.
 * This is used for:
 * - generic sharing (ARG is the rgctx)
 * - gsharedvt signature wrappers (ARG is a function descriptor)
 */
MonoFtnDesc*
mini_llvmonly_create_ftndesc (MonoDomain *domain, gpointer addr, gpointer arg)
{
	MonoFtnDesc *ftndesc = (MonoFtnDesc*)mono_domain_alloc0 (mono_domain_get (), 2 * sizeof (gpointer));
	ftndesc->addr = addr;
	ftndesc->arg = arg;

	return ftndesc;
}

/**
 * mini_llvmonly_add_method_wrappers:
 *
 *   Add unbox/gsharedvt wrappers around COMPILED_METHOD if needed. Return the wrapper address or COMPILED_METHOD
 * if no wrapper is needed. Set OUT_ARG to the rgctx/extra argument needed to be passed to the returned method.
 */
gpointer
mini_llvmonly_add_method_wrappers (MonoMethod *m, gpointer compiled_method, gboolean caller_gsharedvt, gboolean add_unbox_tramp, gpointer *out_arg)
{
	gpointer addr;
	gboolean callee_gsharedvt;

	*out_arg = NULL;

	if (m->wrapper_type == MONO_WRAPPER_MANAGED_TO_MANAGED) {
		WrapperInfo *info = mono_marshal_get_wrapper_info (m);

		/*
		 * generic array helpers.
		 * Have to replace the wrappers with the original generic instances.
		 */
		if (info && info->subtype == WRAPPER_SUBTYPE_GENERIC_ARRAY_HELPER) {
			m = info->d.generic_array_helper.method;
		}
	} else if (m->wrapper_type == MONO_WRAPPER_OTHER) {
		WrapperInfo *info = mono_marshal_get_wrapper_info (m);

		/* Same for synchronized inner wrappers */
		if (info && info->subtype == WRAPPER_SUBTYPE_SYNCHRONIZED_INNER) {
			m = info->d.synchronized_inner.method;
		}
	}

	addr = compiled_method;

	if (add_unbox_tramp) {
		/* 
		 * The unbox trampolines call the method directly, so need to add
		 * an rgctx tramp before them.
		 */
		addr = mono_aot_get_unbox_trampoline (m, addr);
	}

	g_assert (mono_llvm_only);
	g_assert (out_arg);

	callee_gsharedvt = mono_aot_get_method_flags ((guint8*)compiled_method) & MONO_AOT_METHOD_FLAG_GSHAREDVT_VARIABLE;

	if (!caller_gsharedvt && callee_gsharedvt) {
		MonoMethodSignature *sig, *gsig;
		MonoJitInfo *ji;
		MonoMethod *jmethod;
		gpointer wrapper_addr;

		ji = mini_jit_info_table_find (mono_get_addr_from_ftnptr (compiled_method));
		g_assert (ji);
		jmethod = jinfo_get_method (ji);

		/* Here m is a generic instance, while ji->method is the gsharedvt method implementing it */

		/* Call from normal/gshared code to gsharedvt code with variable signature */
		sig = mono_method_signature_internal (m);
		gsig = mono_method_signature_internal (jmethod);

		wrapper_addr = mini_get_gsharedvt_wrapper (TRUE, addr, sig, gsig, -1, FALSE);

		/*
		 * This is a gsharedvt in wrapper, it gets passed a ftndesc for the gsharedvt method as an argument.
		 */
		*out_arg = mini_llvmonly_create_ftndesc (mono_domain_get (), addr, mini_method_get_rgctx (m));
		addr = wrapper_addr;
		//printf ("IN: %s\n", mono_method_full_name (m, TRUE));
	}

	if (!(*out_arg) && mono_method_needs_static_rgctx_invoke (m, FALSE))
		*out_arg = mini_method_get_rgctx (m);

	if (caller_gsharedvt && !callee_gsharedvt) {
		/*
		 * The callee uses the gsharedvt calling convention, have to add an out wrapper.
		 */
		gpointer out_wrapper = mini_get_gsharedvt_wrapper (FALSE, NULL, mono_method_signature_internal (m), NULL, -1, FALSE);
		MonoFtnDesc *out_wrapper_arg = mini_llvmonly_create_ftndesc (mono_domain_get (), addr, *out_arg);

		addr = out_wrapper;
		*out_arg = out_wrapper_arg;
	}

	return addr;
}


typedef struct {
	MonoVTable *vtable;
	int slot;
} IMTTrampInfo;

typedef gpointer (*IMTTrampFunc) (gpointer *arg, MonoMethod *imt_method);

/*
 * mini_llvmonly_initial_imt_tramp:
 *
 *  This function is called the first time a call is made through an IMT trampoline.
 * It should have the same signature as the llvmonly_imt_tramp_... functions.
 */
static gpointer
mini_llvmonly_initial_imt_tramp (gpointer *arg, MonoMethod *imt_method)
{
	IMTTrampInfo *info = (IMTTrampInfo*)arg;
	IMTTrampFunc **imt;
	IMTTrampFunc *ftndesc;
	IMTTrampFunc func;

	mono_vtable_build_imt_slot (info->vtable, info->slot);

	imt = (IMTTrampFunc**)info->vtable;
	imt -= MONO_IMT_SIZE;

	/* Return what the real IMT trampoline returns */
	ftndesc = imt [info->slot];
	func = ftndesc [0];

	if (func == (IMTTrampFunc)mini_llvmonly_initial_imt_tramp)
		/* Happens when the imt slot contains only a generic virtual method */
		return NULL;
	return func ((gpointer *)ftndesc [1], imt_method);
}

/* This is called indirectly through an imt slot. */
static gpointer
llvmonly_imt_tramp (gpointer *arg, MonoMethod *imt_method)
{
	int i = 0;

	/* arg points to an array created in mini_llvmonly_get_imt_trampoline () */
	while (arg [i] && arg [i] != imt_method)
		i += 2;
	g_assert (arg [i]);

	return arg [i + 1];
}

/* Optimized versions of mini_llvmonly_imt_trampoline () for different table sizes */
static gpointer
llvmonly_imt_tramp_1 (gpointer *arg, MonoMethod *imt_method)
{
	//g_assert (arg [0] == imt_method);
	return arg [1];
}

static gpointer
llvmonly_imt_tramp_2 (gpointer *arg, MonoMethod *imt_method)
{
	//g_assert (arg [0] == imt_method || arg [2] == imt_method);
	if (arg [0] == imt_method)
		return arg [1];
	else
		return arg [3];
}

static gpointer
llvmonly_imt_tramp_3 (gpointer *arg, MonoMethod *imt_method)
{
	//g_assert (arg [0] == imt_method || arg [2] == imt_method || arg [4] == imt_method);
	if (arg [0] == imt_method)
		return arg [1];
	else if (arg [2] == imt_method)
		return arg [3];
	else
		return arg [5];
}

/*
 * A version of the imt trampoline used for generic virtual/variant iface methods.
 * Unlikely a normal imt trampoline, its possible that IMT_METHOD is not found
 * in the search table. The original JIT code had a 'fallback' trampoline it could
 * call, but we can't do that, so we just return NULL, and the compiled code
 * will handle it.
 */
static gpointer
llvmonly_fallback_imt_tramp (gpointer *arg, MonoMethod *imt_method)
{
	int i = 0;

	while (arg [i] && arg [i] != imt_method)
		i += 2;
	if (!arg [i])
		return NULL;

	return arg [i + 1];
}

gpointer
mini_llvmonly_get_imt_trampoline (MonoVTable *vtable, MonoIMTCheckItem **imt_entries, int count, gpointer fail_tramp)
{
	gpointer *buf;
	gpointer *res;
	int i, index, real_count;
	gboolean virtual_generic = FALSE;

	/*
	 * Create an array which is passed to the imt trampoline functions.
	 * The array contains MonoMethod-function descriptor pairs, terminated by a NULL entry.
	 */

	real_count = 0;
	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];

		if (item->is_equals)
			real_count ++;
		if (item->has_target_code)
			virtual_generic = TRUE;
	}

	/*
	 * Initialize all vtable entries reachable from this imt slot, so the compiled
	 * code doesn't have to check it.
	 */
	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		int vt_slot;

		if (!item->is_equals || item->has_target_code)
			continue;
		vt_slot = item->value.vtable_slot;
		mini_llvmonly_init_vtable_slot (vtable, vt_slot);
	}

	/* Save the entries into an array */
	buf = (void **)m_class_alloc (vtable->klass, (real_count + 1) * 2 * sizeof (gpointer));
	index = 0;
	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];

		if (!item->is_equals)
			continue;

		g_assert (item->key);
		buf [(index * 2)] = item->key;
		if (item->has_target_code)
			buf [(index * 2) + 1] = item->value.target_code;
		else
			buf [(index * 2) + 1] = vtable->vtable [item->value.vtable_slot];
		index ++;
	}
	buf [(index * 2)] = NULL;
	buf [(index * 2) + 1] = fail_tramp;

	/*
	 * Return a function descriptor for a C function with 'buf' as its argument.
	 * It will by called by JITted code.
	 */
	res = (void **)m_class_alloc (vtable->klass, 2 * sizeof (gpointer));
	switch (real_count) {
	case 1:
		res [0] = (gpointer)llvmonly_imt_tramp_1;
		break;
	case 2:
		res [0] = (gpointer)llvmonly_imt_tramp_2;
		break;
	case 3:
		res [0] = (gpointer)llvmonly_imt_tramp_3;
		break;
	default:
		res [0] = (gpointer)llvmonly_imt_tramp;
		break;
	}
	if (virtual_generic || fail_tramp)
		res [0] = (gpointer)llvmonly_fallback_imt_tramp;
	res [1] = buf;

	return res;
}

gpointer
mini_llvmonly_get_vtable_trampoline (MonoVTable *vt, int slot_index, int index)
{
	if (slot_index < 0) {
		/* Initialize the IMT trampoline to a 'trampoline' so the generated code doesn't have to initialize it */
		// FIXME: Memory management
		gpointer *ftndesc = g_malloc (2 * sizeof (gpointer));
		IMTTrampInfo *info = g_new0 (IMTTrampInfo, 1);
		info->vtable = vt;
		info->slot = index;
		ftndesc [0] = (gpointer)mini_llvmonly_initial_imt_tramp;
		ftndesc [1] = info;
		mono_memory_barrier ();
		return ftndesc;
	} else {
		return NULL;
	}
}

static gboolean
is_generic_method_definition (MonoMethod *m)
{
	MonoGenericContext *context;
	if (m->is_generic)
		return TRUE;
	if (!m->is_inflated)
		return FALSE;

	context = mono_method_get_context (m);
	if (!context->method_inst)
		return FALSE;
	if (context->method_inst == mono_method_get_generic_container (((MonoMethodInflated*)m)->declaring)->context.method_inst)
		return TRUE;
	return FALSE;
}

/*
 * resolve_vcall:
 *
 *   Return the executable code for calling vt->vtable [slot].
 * This function is called on a slowpath, so it doesn't need to be fast.
 * This returns an ftnptr by returning the address part, and the arg in the OUT_ARG
 * out parameter.
 */
static gpointer
resolve_vcall (MonoVTable *vt, int slot, MonoMethod *imt_method, gpointer *out_arg, gboolean gsharedvt, MonoError *error)
{
	MonoMethod *m, *generic_virtual = NULL;
	gpointer addr, compiled_method;
	gboolean need_unbox_tramp = FALSE;

	error_init (error);
	/* Same as in common_call_trampoline () */

	/* Avoid loading metadata or creating a generic vtable if possible */
	addr = mono_aot_get_method_from_vt_slot (mono_domain_get (), vt, slot, error);
	return_val_if_nok (error, NULL);
	if (addr && !m_class_is_valuetype (vt->klass))
		return mono_create_ftnptr (mono_domain_get (), addr);

	m = mono_class_get_vtable_entry (vt->klass, slot);

	if (is_generic_method_definition (m)) {
		MonoGenericContext context = { NULL, NULL };
		MonoMethod *declaring;

		if (m->is_inflated)
			declaring = mono_method_get_declaring_generic_method (m);
		else
			declaring = m;

		if (mono_class_is_ginst (m->klass))
			context.class_inst = mono_class_get_generic_class (m->klass)->context.class_inst;
		else
			g_assert (!mono_class_is_gtd (m->klass));

		generic_virtual = imt_method;
		g_assert (generic_virtual);
		g_assert (generic_virtual->is_inflated);
		context.method_inst = ((MonoMethodInflated*)generic_virtual)->context.method_inst;

		m = mono_class_inflate_generic_method_checked (declaring, &context, error);
		mono_error_assert_ok (error); /* FIXME don't swallow the error */
	}

	if (generic_virtual) {
		if (m_class_is_valuetype (vt->klass))
			need_unbox_tramp = TRUE;
	} else {
		if (m_class_is_valuetype (m->klass))
			need_unbox_tramp = TRUE;
	}

	if (m->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED)
		m = mono_marshal_get_synchronized_wrapper (m);

	addr = compiled_method = mini_llvmonly_load_method (m, gsharedvt, need_unbox_tramp, out_arg, error);
	mono_error_assert_ok (error);

	if (!gsharedvt && generic_virtual) {
		// FIXME: This wastes memory since add_generic_virtual_invocation ignores it in a lot of cases
		MonoFtnDesc *ftndesc = mini_llvmonly_create_ftndesc (mono_domain_get (), addr, out_arg);

		mono_method_add_generic_virtual_invocation (mono_domain_get (),
													vt, vt->vtable + slot,
													generic_virtual, ftndesc);
	}

	return addr;
}

gpointer
mini_llvmonly_resolve_vcall_gsharedvt (MonoObject *this_obj, int slot, MonoMethod *imt_method, gpointer *out_arg)
{
	g_assert (this_obj);

	ERROR_DECL (error);
	gpointer result = resolve_vcall (this_obj->vtable, slot, imt_method, out_arg, TRUE, error);
	if (!is_ok (error)) {
		MonoException *ex = mono_error_convert_to_exception (error);
		mono_llvm_throw_exception ((MonoObject*)ex);
	}
	return result;
}

/*
 * mini_llvmonly_resolve_generic_virtual_call:
 *
 *   Resolve a generic virtual call.
 * This function is called on a slowpath, so it doesn't need to be fast.
 */
MonoFtnDesc*
mini_llvmonly_resolve_generic_virtual_call (MonoVTable *vt, int slot, MonoMethod *generic_virtual)
{
	MonoMethod *m;
	gboolean need_unbox_tramp = FALSE;
	ERROR_DECL (error);
	MonoGenericContext context = { NULL, NULL };
	MonoMethod *declaring;

	m = mono_class_get_vtable_entry (vt->klass, slot);

	g_assert (is_generic_method_definition (m));

	if (m->is_inflated)
		declaring = mono_method_get_declaring_generic_method (m);
	else
		declaring = m;

	if (mono_class_is_ginst (m->klass))
		context.class_inst = mono_class_get_generic_class (m->klass)->context.class_inst;
	else
		g_assert (!mono_class_is_gtd (m->klass));

	g_assert (generic_virtual->is_inflated);
	context.method_inst = ((MonoMethodInflated*)generic_virtual)->context.method_inst;

	m = mono_class_inflate_generic_method_checked (declaring, &context, error);
	g_assert (is_ok (error));

	if (m_class_is_valuetype (vt->klass))
		need_unbox_tramp = TRUE;

	/*
	 * This wastes memory but the memory usage is bounded since
	 * mono_method_add_generic_virtual_invocation () eventually builds an imt trampoline for
	 * this vtable slot so we are not called any more for this instantiation.
	 */
	MonoFtnDesc *ftndesc = mini_llvmonly_load_method_ftndesc (m, FALSE, need_unbox_tramp, error);
	mono_error_assert_ok (error);

	mono_method_add_generic_virtual_invocation (mono_domain_get (),
												vt, vt->vtable + slot,
												generic_virtual, ftndesc);
	return ftndesc;
}

/*
 * mini_llvmonly_resolve_generic_virtual_iface_call:
 *
 *   Resolve a generic virtual/variant iface call on interfaces.
 * This function is called on a slowpath, so it doesn't need to be fast.
 */
MonoFtnDesc*
mini_llvmonly_resolve_generic_virtual_iface_call (MonoVTable *vt, int imt_slot, MonoMethod *generic_virtual)
{
	ERROR_DECL (error);
	MonoMethod *m, *variant_iface;
	MonoFtnDesc *ftndesc;
	gpointer aot_addr;
	gboolean need_unbox_tramp = FALSE;
	gboolean need_rgctx_tramp;
	gpointer *imt;

	imt = (gpointer*)vt - MONO_IMT_SIZE;

	mini_resolve_imt_method (vt, imt + imt_slot, generic_virtual, &m, &aot_addr, &need_rgctx_tramp, &variant_iface, error);
	if (!is_ok (error)) {
		MonoException *ex = mono_error_convert_to_exception (error);
		mono_llvm_throw_exception ((MonoObject*)ex);
	}

	if (m_class_is_valuetype (vt->klass))
		need_unbox_tramp = TRUE;

	if (m->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED)
		m = mono_marshal_get_synchronized_wrapper (m);

	/*
	 * This wastes memory but the memory usage is bounded since
	 * mono_method_add_generic_virtual_invocation () eventually builds an imt trampoline for
	 * this vtable slot so we are not called any more for this instantiation.
	 */
	ftndesc = mini_llvmonly_load_method_ftndesc (m, FALSE, need_unbox_tramp, error);

	mono_method_add_generic_virtual_invocation (mono_domain_get (),
												vt, imt + imt_slot,
												variant_iface ? variant_iface : generic_virtual, ftndesc);
	return ftndesc;
}

/*
 * mini_llvmonly_init_vtable_slot:
 *
 *   Initialize slot SLOT of VTABLE.
 * Return the contents of the vtable slot.
 */
gpointer
mini_llvmonly_init_vtable_slot (MonoVTable *vtable, int slot)
{
	ERROR_DECL (error);
	gpointer arg = NULL;
	gpointer addr;
	gpointer *ftnptr;

	addr = resolve_vcall (vtable, slot, NULL, &arg, FALSE, error);
	if (mono_error_set_pending_exception (error))
		return NULL;
	ftnptr = mono_domain_alloc0 (vtable->domain, 2 * sizeof (gpointer));
	ftnptr [0] = addr;
	ftnptr [1] = arg;
	mono_memory_barrier ();

	vtable->vtable [slot] = ftnptr;

	return ftnptr;
}

/*
 * mini_llvmonly_init_delegate:
 *
 *   Initialize a MonoDelegate object.
 * Similar to mono_delegate_ctor ().
 */
void
mini_llvmonly_init_delegate (MonoDelegate *del)
{
	ERROR_DECL (error);
	MonoFtnDesc *ftndesc = *(MonoFtnDesc**)del->method_code;

	/*
	 * We store a MonoFtnDesc in del->method_code.
	 * It would be better to store an ftndesc in del->method_ptr too,
	 * but we don't have a a structure which could own its memory.
	 */
	if (G_UNLIKELY (!ftndesc)) {
		MonoMethod *m = del->method;
		gboolean need_unbox = FALSE;

		if (m->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED)
			m = mono_marshal_get_synchronized_wrapper (m);

		if (m_class_is_valuetype (m->klass) && mono_method_signature_internal (m)->hasthis)
			need_unbox = TRUE;

		gpointer arg = NULL;
		gpointer addr = mini_llvmonly_load_method_delegate (m, FALSE, need_unbox, &arg, error);
		if (mono_error_set_pending_exception (error))
			return;
		ftndesc = mini_llvmonly_create_ftndesc (mono_domain_get (), addr, arg);
		mono_memory_barrier ();
		*del->method_code = (guint8*)ftndesc;
	}
	del->method_ptr = ftndesc->addr;
	del->extra_arg = ftndesc->arg;
}

void
mini_llvmonly_init_delegate_virtual (MonoDelegate *del, MonoObject *target, MonoMethod *method)
{
	ERROR_DECL (error);
	gpointer addr, arg;
	gboolean need_unbox;

	g_assert (target);

	method = mono_object_get_virtual_method_internal (target, method);

	if (method->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED)
		method = mono_marshal_get_synchronized_wrapper (method);
	need_unbox = m_class_is_valuetype (method->klass);

	del->method = method;
	addr = mini_llvmonly_load_method_delegate (method, FALSE, need_unbox, &arg, error);
	if (mono_error_set_pending_exception (error))
		return;
	del->method_ptr = addr;
	del->extra_arg = arg;
}

/*
 * resolve_iface_call:
 *
 *   Return the executable code for the iface method IMT_METHOD called on THIS.
 * This function is called on a slowpath, so it doesn't need to be fast.
 * This returns an ftnptr by returning the address part, and the arg in the OUT_ARG
 * out parameter.
 */
static gpointer
resolve_iface_call (MonoObject *this_obj, int imt_slot, MonoMethod *imt_method, gpointer *out_arg, gboolean caller_gsharedvt, MonoError *error)
{
	MonoVTable *vt;
	gpointer *imt;
	MonoMethod *impl_method, *generic_virtual = NULL, *variant_iface = NULL;
	gpointer addr, aot_addr;
	gboolean need_rgctx_tramp = FALSE, need_unbox_tramp = FALSE;

	error_init (error);
	if (!this_obj)
		/* The caller will handle it */
		return NULL;

	vt = this_obj->vtable;
	imt = (gpointer*)vt - MONO_IMT_SIZE;

	mini_resolve_imt_method (vt, imt + imt_slot, imt_method, &impl_method, &aot_addr, &need_rgctx_tramp, &variant_iface, error);
	return_val_if_nok (error, NULL);

	if (imt_method->is_inflated && ((MonoMethodInflated*)imt_method)->context.method_inst)
		generic_virtual = imt_method;

	if (generic_virtual || variant_iface) {
		if (m_class_is_valuetype (vt->klass)) /*FIXME is this required variant iface?*/
			need_unbox_tramp = TRUE;
	} else {
		if (m_class_is_valuetype (impl_method->klass))
			need_unbox_tramp = TRUE;
	}

	addr = mini_llvmonly_load_method (impl_method, caller_gsharedvt, need_unbox_tramp, out_arg, error);
	mono_error_assert_ok (error);
	g_assert (addr);

	if (generic_virtual || variant_iface) {
		MonoMethod *target = generic_virtual ? generic_virtual : variant_iface;

		mono_method_add_generic_virtual_invocation (mono_domain_get (),
													vt, imt + imt_slot,
													target, addr);
	}

	return addr;
}

gpointer
mini_llvmonly_resolve_iface_call_gsharedvt (MonoObject *this_obj, int imt_slot, MonoMethod *imt_method, gpointer *out_arg)
{
	ERROR_DECL (error);
	gpointer res = resolve_iface_call (this_obj, imt_slot, imt_method, out_arg, TRUE, error);
	if (!is_ok (error)) {
		MonoException *ex = mono_error_convert_to_exception (error);
		mono_llvm_throw_exception ((MonoObject*)ex);
	}
	return res;
}

/* Called from LLVM code to initialize a method */
// FIXME: This should be somewhere else
void
mini_llvm_init_method (MonoAotFileInfo *info, gpointer aot_module, gpointer method_info, MonoVTable *vtable)
{
	gboolean res;
	MonoAotModule *amodule = (MonoAotModule *)aot_module;
	ERROR_DECL (error);

	res = mono_aot_init_llvm_method (amodule, method_info, vtable ? vtable->klass : NULL, error);
	if (!res || !is_ok (error)) {
		MonoException *ex = mono_error_convert_to_exception (error);
		if (ex) {
			/* Its okay to raise in llvmonly mode */
			if (mono_llvm_only) {
				mono_llvm_throw_exception ((MonoObject*)ex);
			} else {
				mono_set_pending_exception (ex);
			}
		}
	}
}

static GENERATE_GET_CLASS_WITH_CACHE (nullref, "System", "NullReferenceException")

void
mini_llvmonly_throw_nullref_exception (void)
{
	MonoClass *klass = mono_class_get_nullref_class ();

	guint32 ex_token_index = m_class_get_type_token (klass) - MONO_TOKEN_TYPE_DEF;

	mono_llvm_throw_corlib_exception (ex_token_index);
}

void
mini_llvmonly_throw_aot_failed_exception (const char *name)
{
	char *msg = g_strdup_printf ("AOT Compilation failed for method '%s'.", name);
	MonoException *ex = mono_get_exception_execution_engine (msg);
	g_free (msg);
	mono_llvm_throw_exception ((MonoObject*)ex);
}

/*
 * mini_llvmonly_pop_lmf:
 *
 *   Pop LMF off the LMF stack.
 */
void
mini_llvmonly_pop_lmf (MonoLMF *lmf)
{
	if (lmf->previous_lmf)
		mono_set_lmf ((MonoLMF*)lmf->previous_lmf);
}

gpointer
mini_llvmonly_get_interp_entry (MonoMethod *method)
{
	ERROR_DECL (error);

	MonoFtnDesc *desc = mini_get_interp_callbacks ()->create_method_pointer_llvmonly (method, FALSE, error);
	mono_error_assert_ok (error);
	return desc;
}
