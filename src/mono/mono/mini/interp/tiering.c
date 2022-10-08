#include "tiering.h"

static mono_mutex_t tiering_mutex;
static GHashTable *patch_sites_table;
static gboolean enable_tiering;

void
mono_interp_tiering_init (void)
{
	mono_os_mutex_init_recursive (&tiering_mutex);
	patch_sites_table = g_hash_table_new (NULL, NULL);
	enable_tiering = TRUE;
}

gboolean
mono_interp_tiering_enabled (void)
{
	return enable_tiering;
}

static InterpMethod*
get_tier_up_imethod (InterpMethod *imethod)
{
	MonoMethod *method = imethod->method;
	MonoJitMemoryManager *jit_mm = jit_mm_for_method (method);
	InterpMethod *new_imethod = (InterpMethod*)m_method_alloc0 (method, sizeof (InterpMethod));

	new_imethod->method = imethod->method;
	new_imethod->param_count = imethod->param_count;
	new_imethod->hasthis = imethod->hasthis;
	new_imethod->vararg = imethod->vararg;
	new_imethod->code_type = imethod->code_type;
	new_imethod->rtype = imethod->rtype;
	new_imethod->param_types = imethod->param_types;
	new_imethod->optimized = TRUE;
	new_imethod->prof_flags = imethod->prof_flags;

	jit_mm_lock (jit_mm);
	InterpMethod *old_imethod = mono_internal_hash_table_lookup (&jit_mm->interp_code_hash, method);
	if (old_imethod->optimized) {
		new_imethod = old_imethod; /* leak the newly allocated InterpMethod to the mempool */
	} else {
		mono_internal_hash_table_remove (&jit_mm->interp_code_hash, method);
		mono_internal_hash_table_insert (&jit_mm->interp_code_hash, method, new_imethod);
	}
	jit_mm_unlock (jit_mm);

	return new_imethod;
}

static void
patch_imethod_site (gpointer data, gpointer user_data)
{
	gpointer *addr = (gpointer*)data;
	// Preserve the tagging, in case the address originates in vtables
	gboolean tagged = INTERP_IMETHOD_IS_TAGGED_1 (*addr);
	*addr = (InterpMethod*)(tagged ? INTERP_IMETHOD_TAG_1 (user_data) : user_data);
}

static void
patch_interp_data_items (InterpMethod *old_imethod, InterpMethod *new_imethod)
{
	GSList *sites = g_hash_table_lookup (patch_sites_table, old_imethod);
	g_slist_foreach (sites, patch_imethod_site, new_imethod);

	g_hash_table_remove (patch_sites_table, sites);
	g_slist_free (sites);
}

static InterpMethod*
tier_up_method (InterpMethod *imethod, ThreadContext *context)
{
	g_assert (enable_tiering);
	ERROR_DECL(error);
	// This enables future code to obtain a reference to the optimized imethod
	InterpMethod *new_imethod = get_tier_up_imethod (imethod);

	// In theory we can race with other threads compiling the same imethod, but this is not a problem
	if (!new_imethod->transformed)
		mono_interp_transform_method (new_imethod, context, error);
	// Unoptimized method compiled fine, optimized method should also compile without error
	mono_error_assert_ok (error);

	mono_os_mutex_lock (&tiering_mutex);

	if (!imethod->optimized_imethod) {
		// patch all data items
		patch_interp_data_items (imethod, new_imethod);

		// Other threads executing this imethod will be able to tier the frame up in patchpoints
		imethod->optimized_imethod = new_imethod;
	}
	mono_os_mutex_unlock (&tiering_mutex);

	return new_imethod;
}

static void
register_imethod_patch_site (InterpMethod *imethod, gpointer *ptr)
{
	g_assert (!imethod->optimized);
	GSList *sites = g_hash_table_lookup (patch_sites_table, imethod);
	sites = g_slist_prepend (sites, ptr);
	g_hash_table_insert_replace (patch_sites_table, imethod, sites, TRUE);
}

static void
register_imethod_data_item (gpointer data, gpointer user_data)
{
	gint32 index = (gint32)(gsize)data;
	InterpMethod **data_items = (InterpMethod**)user_data;

	if (data_items [index]) {
		if (data_items [index]->optimized_imethod) {
			// We are under tiering lock, check if the method has been tiered up already
			data_items [index] = data_items [index]->optimized_imethod;
			return;
		}
		register_imethod_patch_site (data_items [index], (gpointer*)&data_items [index]);
	}
}

void
mono_interp_register_imethod_data_items (gpointer *data_items, GSList *indexes)
{
	if (!enable_tiering)
		return;
	mono_os_mutex_lock (&tiering_mutex);
	g_slist_foreach (indexes, register_imethod_data_item, data_items);
	mono_os_mutex_unlock (&tiering_mutex);
}

// This method should be called within mem manager lock which means
// the contents of **imethod_ptr cannot modify until we register the
// patch site
void
mono_interp_register_imethod_patch_site (gpointer *imethod_ptr)
{
	gboolean tagged = INTERP_IMETHOD_IS_TAGGED_1 (*imethod_ptr);
	InterpMethod *imethod = INTERP_IMETHOD_UNTAG_1 (*imethod_ptr);
	if (imethod->optimized) {
		return;
	} else if (imethod->optimized_imethod) {
		*imethod_ptr = tagged ? imethod->optimized_imethod : INTERP_IMETHOD_TAG_1 (imethod->optimized_imethod);
		return;
	}

	mono_os_mutex_lock (&tiering_mutex);
	// We are under tiering lock, check if the method has been tiered up already
	if (imethod->optimized_imethod) {
		*imethod_ptr = tagged ? imethod->optimized_imethod : INTERP_IMETHOD_TAG_1 (imethod->optimized_imethod);
	} else {
		register_imethod_patch_site (imethod, imethod_ptr);
	}
	mono_os_mutex_unlock (&tiering_mutex);
}

const guint16*
mono_interp_tier_up_frame_enter (InterpFrame *frame, ThreadContext *context)
{
	InterpMethod *optimized_method;
	if (frame->imethod->optimized_imethod)
		optimized_method = frame->imethod->optimized_imethod;
	else
		optimized_method = tier_up_method (frame->imethod, context);
	context->stack_pointer = (guchar*)frame->stack + optimized_method->alloca_size;
	frame->imethod = optimized_method;
	return optimized_method->code;
}

static int
lookup_patchpoint_data (InterpMethod *imethod, int data)
{
       int *position = imethod->patchpoint_data;
       while (*position != G_MAXINT32) {
               if (*position == data)
                       return *(position + 1);
               position += 2;
       }
       return G_MAXINT32;
}

const guint16*
mono_interp_tier_up_frame_patchpoint (InterpFrame *frame, ThreadContext *context, int bb_index)
{
	InterpMethod *unoptimized_method = frame->imethod;
	InterpMethod *optimized_method;
	if (unoptimized_method->optimized_imethod)
		optimized_method = unoptimized_method->optimized_imethod;
	else
		optimized_method = tier_up_method (unoptimized_method, context);
	for (int i = 0; i < unoptimized_method->num_clauses; i++) {
		MonoExceptionClause *clause = &unoptimized_method->clauses [i];
		if (clause->flags != MONO_EXCEPTION_CLAUSE_FINALLY)
			continue;
		// Patch return addresses used by MINT_CALL_HANDLER + MINT_ENDFINALLY
		guint16 **ip_addr = (guint16**)((char*)frame->stack + unoptimized_method->clause_data_offsets [i]);
		guint16 *ret_ip = *ip_addr;
		// ret_ip could be junk on stack, do a quick check first
		if (ret_ip < unoptimized_method->code)
			continue;
		int native_offset = (int)(ret_ip - unoptimized_method->code);
		int call_handler_index = lookup_patchpoint_data (unoptimized_method, native_offset);
		if (call_handler_index != G_MAXINT32) {
			int offset = lookup_patchpoint_data (optimized_method, call_handler_index);
			g_assert (offset != G_MAXINT32);
			*ip_addr = optimized_method->code + offset;
		}
	}
	context->stack_pointer = (guchar*)frame->stack + optimized_method->alloca_size;
	frame->imethod = optimized_method;
	return optimized_method->code + lookup_patchpoint_data (optimized_method, bb_index);
}
