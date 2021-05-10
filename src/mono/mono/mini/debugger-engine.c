/**
 * \file
 * Debugger Engine shared code.
 *
 * Author:
 *   Zoltan Varga (vargaz@gmail.com)
 *   Rodrigo Kumpera (kumpera@gmail.com)
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include "mini-runtime.h"

#if !defined (DISABLE_SDB) || defined(TARGET_WASM)

#include <glib.h>
#include "seq-points.h"
#include "aot-runtime.h"
#include "debugger-engine.h"
#include "debugger-state-machine.h"
#include <mono/metadata/debug-internals.h>

static void mono_de_ss_start (SingleStepReq *ss_req, SingleStepArgs *ss_args);
static gboolean mono_de_ss_update (SingleStepReq *req, MonoJitInfo *ji, SeqPoint *sp, void *tls, MonoContext *ctx, MonoMethod* method);


static DebuggerEngineCallbacks rt_callbacks;

/*
 * Logging support
 */
static int log_level;
static FILE *log_file;


/*
 * Locking
 */
#define dbg_lock() mono_coop_mutex_lock (&debug_mutex)
#define dbg_unlock() mono_coop_mutex_unlock (&debug_mutex)
static MonoCoopMutex debug_mutex;

void
mono_de_lock (void)
{
	dbg_lock ();
}

void
mono_de_unlock (void)
{
	dbg_unlock ();
}


/*
 * Domain support
 */


/* A hash table containing all active domains */
/* Protected by the loader lock */
static GHashTable *domains;


static void
domains_init (void)
{
	domains = g_hash_table_new (mono_aligned_addr_hash, NULL);
}

static void
domains_cleanup (void)
{
	//FIXME can we safely destroy `domains`?
}

/*
 * mono_de_foreach_domain:
 *
 * Iterate over all domains under debugging. Caller must take the loader lock.
 *
 * FIXME can we move the locking to here? Callers in sdb must be properly audited.
 */
void
mono_de_foreach_domain (GHFunc func, gpointer user_data)
{
	g_hash_table_foreach (domains, func, user_data);
}

/*
 * LOCKING: Takes the loader lock
 */
void
mono_de_domain_remove (MonoDomain *domain)
{
	mono_loader_lock ();
	g_hash_table_remove (domains, domain);
	mono_loader_unlock ();
}

/*
 * LOCKING: Takes the loader lock
 */
void
mono_de_domain_add (MonoDomain *domain)
{
	mono_loader_lock ();
	g_hash_table_insert (domains, domain, domain);
	mono_loader_unlock ();
}

/*
 * BREAKPOINTS
 */

/* List of breakpoints */
/* Protected by the loader lock */
static GPtrArray *breakpoints;
/* Maps breakpoint locations to the number of breakpoints at that location */
static GHashTable *bp_locs;

static void
breakpoints_init (void)
{
	breakpoints = g_ptr_array_new ();
	bp_locs = g_hash_table_new (NULL, NULL);
}

/*
 * insert_breakpoint:
 *
 *   Insert the breakpoint described by BP into the method described by
 * JI.
 */
static void
insert_breakpoint (MonoSeqPointInfo *seq_points, MonoDomain *domain, MonoJitInfo *ji, MonoBreakpoint *bp, MonoError *error)
{
	int count;
	BreakpointInstance *inst;
	SeqPointIterator it;
	gboolean it_has_sp = FALSE;

	if (error)
		error_init (error);

	mono_seq_point_iterator_init (&it, seq_points);
	while (mono_seq_point_iterator_next (&it)) {
		if (it.seq_point.il_offset == bp->il_offset) {
			it_has_sp = TRUE;
			break;
		}
	}

	if (!it_has_sp) {
		/*
		 * The set of IL offsets with seq points doesn't completely match the
		 * info returned by CMD_METHOD_GET_DEBUG_INFO (#407).
		 */
		mono_seq_point_iterator_init (&it, seq_points);
		while (mono_seq_point_iterator_next (&it)) {
			if (it.seq_point.il_offset != METHOD_ENTRY_IL_OFFSET &&
				it.seq_point.il_offset != METHOD_EXIT_IL_OFFSET &&
				it.seq_point.il_offset + 1 == bp->il_offset) {
				it_has_sp = TRUE;
				break;
			}
		}
	}

	if (!it_has_sp) {
		char *s = g_strdup_printf ("Unable to insert breakpoint at %s:%ld", mono_method_full_name (jinfo_get_method (ji), TRUE), bp->il_offset);

		mono_seq_point_iterator_init (&it, seq_points);
		while (mono_seq_point_iterator_next (&it))
			PRINT_DEBUG_MSG (1, "%d\n", it.seq_point.il_offset);

		if (error) {
			mono_error_set_error (error, MONO_ERROR_GENERIC, "%s", s);
			g_warning ("%s", s);
			g_free (s);
			return;
		} else {
			g_warning ("%s", s);
			g_free (s);
			return;
		}
	}

	inst = g_new0 (BreakpointInstance, 1);
	inst->il_offset = it.seq_point.il_offset;
	inst->native_offset = it.seq_point.native_offset;
	inst->ip = (guint8*)ji->code_start + it.seq_point.native_offset;
	inst->ji = ji;
	inst->domain = domain;

	mono_loader_lock ();

	g_ptr_array_add (bp->children, inst);

	mono_loader_unlock ();

	dbg_lock ();
	count = GPOINTER_TO_INT (g_hash_table_lookup (bp_locs, inst->ip));
	g_hash_table_insert (bp_locs, inst->ip, GINT_TO_POINTER (count + 1));
	dbg_unlock ();

	if (it.seq_point.native_offset == SEQ_POINT_NATIVE_OFFSET_DEAD_CODE) {
		PRINT_DEBUG_MSG (1, "[dbg] Attempting to insert seq point at dead IL offset %d, ignoring.\n", (int)bp->il_offset);
	} else if (count == 0) {
		if (ji->is_interp) {
			mini_get_interp_callbacks ()->set_breakpoint (ji, inst->ip);
		} else {
#ifdef MONO_ARCH_SOFT_DEBUG_SUPPORTED
			mono_arch_set_breakpoint (ji, inst->ip);
#else
			NOT_IMPLEMENTED;
#endif
		}
	}

	PRINT_DEBUG_MSG (1, "[dbg] Inserted breakpoint at %s:[il=0x%x,native=0x%x] [%p](%d).\n", mono_method_full_name (jinfo_get_method (ji), TRUE), (int)it.seq_point.il_offset, (int)it.seq_point.native_offset, inst->ip, count);
}

static void
remove_breakpoint (BreakpointInstance *inst)
{
	int count;
	MonoJitInfo *ji = inst->ji;
	guint8 *ip = inst->ip;

	dbg_lock ();
	count = GPOINTER_TO_INT (g_hash_table_lookup (bp_locs, ip));
	g_hash_table_insert (bp_locs, ip, GINT_TO_POINTER (count - 1));
	dbg_unlock ();

	g_assert (count > 0);

	if (count == 1 && inst->native_offset != SEQ_POINT_NATIVE_OFFSET_DEAD_CODE) {
		if (ji->is_interp) {
			mini_get_interp_callbacks ()->clear_breakpoint (ji, ip);
		} else {
#ifdef MONO_ARCH_SOFT_DEBUG_SUPPORTED
			mono_arch_clear_breakpoint (ji, ip);
#else
			NOT_IMPLEMENTED;
#endif
		}
		PRINT_DEBUG_MSG (1, "[dbg] Clear breakpoint at %s [%p].\n", mono_method_full_name (jinfo_get_method (ji), TRUE), ip);
	}
}

/*
 * This doesn't take any locks.
 */
static gboolean
bp_matches_method (MonoBreakpoint *bp, MonoMethod *method)
{
	int i;

	if (!bp->method)
		return TRUE;
	if (method == bp->method)
		return TRUE;
	if (method->is_inflated && ((MonoMethodInflated*)method)->declaring == bp->method)
		return TRUE;

	if (bp->method->is_inflated && method->is_inflated) {
		MonoMethodInflated *bpimethod = (MonoMethodInflated*)bp->method;
		MonoMethodInflated *imethod = (MonoMethodInflated*)method;

		/* Open generic methods should match closed generic methods of the same class */
		if (bpimethod->declaring == imethod->declaring && bpimethod->context.class_inst == imethod->context.class_inst && bpimethod->context.method_inst && bpimethod->context.method_inst->is_open) {
			for (i = 0; i < bpimethod->context.method_inst->type_argc; ++i) {
				MonoType *t1 = bpimethod->context.method_inst->type_argv [i];

				/* FIXME: Handle !mvar */
				if (t1->type != MONO_TYPE_MVAR)
					return FALSE;
			}
			return TRUE;
		}
	}

	return FALSE;
}

/*
 * mono_de_add_pending_breakpoints:
 *
 *   Insert pending breakpoints into the newly JITted method METHOD.
 */
void
mono_de_add_pending_breakpoints (MonoMethod *method, MonoJitInfo *ji)
{
	int i, j;
	MonoSeqPointInfo *seq_points;
	MonoDomain *domain;

	if (!breakpoints)
		return;

	domain = mono_domain_get ();

	mono_loader_lock ();

	for (i = 0; i < breakpoints->len; ++i) {
		MonoBreakpoint *bp = (MonoBreakpoint *)g_ptr_array_index (breakpoints, i);
		gboolean found = FALSE;

		if (!bp_matches_method (bp, method))
			continue;

		for (j = 0; j < bp->children->len; ++j) {
			BreakpointInstance *inst = (BreakpointInstance *)g_ptr_array_index (bp->children, j);

			if (inst->ji == ji)
				found = TRUE;
		}

		if (!found) {
			seq_points = (MonoSeqPointInfo *) ji->seq_points;

			if (!seq_points) {
				MonoMethod *jmethod = jinfo_get_method (ji);
				if (jmethod->is_inflated) {
					MonoJitInfo *seq_ji;
					MonoMethod *declaring = mono_method_get_declaring_generic_method (jmethod);
					mono_jit_search_all_backends_for_jit_info (declaring, &seq_ji);
					seq_points = (MonoSeqPointInfo *) seq_ji->seq_points;
				}
			}

			if (!seq_points)
				/* Could be AOT code, or above "search_all_backends" call could have failed */
				continue;

			insert_breakpoint (seq_points, domain, ji, bp, NULL);
		}
	}

	mono_loader_unlock ();
}

static void
set_bp_in_method (MonoDomain *domain, MonoMethod *method, MonoSeqPointInfo *seq_points, MonoBreakpoint *bp, MonoError *error)
{
	MonoJitInfo *ji;

	if (error)
		error_init (error);

	(void)mono_jit_search_all_backends_for_jit_info (method, &ji);
	g_assert (ji);

	insert_breakpoint (seq_points, domain, ji, bp, error);
}

typedef struct {
	MonoBreakpoint *bp;
	GPtrArray *methods;
	GPtrArray *method_domains;
	GPtrArray *method_seq_points;
} CollectDomainData;

static void
collect_domain_bp (gpointer key, gpointer value, gpointer user_data)
{
	GHashTableIter iter;
	MonoSeqPointInfo *seq_points;
	MonoDomain *domain = (MonoDomain*)key;
	CollectDomainData *ud = (CollectDomainData*)user_data;
	MonoMethod *m;

	// FIXME:
	MonoJitMemoryManager *jit_mm = get_default_jit_mm ();
	jit_mm_lock (jit_mm);
	g_hash_table_iter_init (&iter, jit_mm->seq_points);
	while (g_hash_table_iter_next (&iter, (void**)&m, (void**)&seq_points)) {
		if (bp_matches_method (ud->bp, m)) {
			/* Save the info locally to simplify the code inside the domain lock */
			g_ptr_array_add (ud->methods, m);
			g_ptr_array_add (ud->method_domains, domain);
			g_ptr_array_add (ud->method_seq_points, seq_points);
		}
	}
	jit_mm_unlock (jit_mm);
}

void
mono_de_clear_all_breakpoints (void)
{
	while (breakpoints->len)
		mono_de_clear_breakpoint ((MonoBreakpoint*)g_ptr_array_index (breakpoints, 0));
}

/*
 * mono_de_set_breakpoint:
 *
 *   Set a breakpoint at IL_OFFSET in METHOD.
 * METHOD can be NULL, in which case a breakpoint is placed in all methods.
 * METHOD can also be a generic method definition, in which case a breakpoint
 * is placed in all instances of the method.
 * If ERROR is non-NULL, then it is set and NULL is returnd if some breakpoints couldn't be
 * inserted.
 */
MonoBreakpoint*
mono_de_set_breakpoint (MonoMethod *method, long il_offset, EventRequest *req, MonoError *error)
{
	MonoBreakpoint *bp;
	MonoDomain *domain;
	MonoMethod *m;
	MonoSeqPointInfo *seq_points;
	GPtrArray *methods;
	GPtrArray *method_domains;
	GPtrArray *method_seq_points;
	int i;

	if (error)
		error_init (error);

	// FIXME:
	// - suspend/resume the vm to prevent code patching problems
	// - multiple breakpoints on the same location
	// - dynamic methods
	// - races

	bp = g_new0 (MonoBreakpoint, 1);
	bp->method = method;
	bp->il_offset = il_offset;
	bp->req = req;
	bp->children = g_ptr_array_new ();

	PRINT_DEBUG_MSG  (1, "[dbg] Setting %sbreakpoint at %s:0x%x.\n", (req->event_kind == EVENT_KIND_STEP) ? "single step " : "", method ? mono_method_full_name (method, TRUE) : "<all>", (int)il_offset);

	methods = g_ptr_array_new ();
	method_domains = g_ptr_array_new ();
	method_seq_points = g_ptr_array_new ();

	mono_loader_lock ();

	CollectDomainData user_data;
	memset (&user_data, 0, sizeof (user_data));
	user_data.bp = bp;
	user_data.methods = methods;
	user_data.method_domains = method_domains;
	user_data.method_seq_points = method_seq_points;
	mono_de_foreach_domain (collect_domain_bp, &user_data);

	for (i = 0; i < methods->len; ++i) {
		m = (MonoMethod *)g_ptr_array_index (methods, i);
		domain = (MonoDomain *)g_ptr_array_index (method_domains, i);
		seq_points = (MonoSeqPointInfo *)g_ptr_array_index (method_seq_points, i);
		set_bp_in_method (domain, m, seq_points, bp, error);
	}

	g_ptr_array_add (breakpoints, bp);
	mono_debugger_log_add_bp (bp, bp->method, bp->il_offset);
	mono_loader_unlock ();

	g_ptr_array_free (methods, TRUE);
	g_ptr_array_free (method_domains, TRUE);
	g_ptr_array_free (method_seq_points, TRUE);

	if (error && !is_ok (error)) {
		mono_de_clear_breakpoint (bp);
		return NULL;
	}

	return bp;
}

MonoBreakpoint *
mono_de_get_breakpoint_by_id (int id)
{
	for (int i = 0; i < breakpoints->len; ++i) {
		MonoBreakpoint *bp = (MonoBreakpoint *)g_ptr_array_index (breakpoints, i);
		if (bp->req->id == id)
			return bp;
	}
	return NULL;
}

void
mono_de_clear_breakpoint (MonoBreakpoint *bp)
{
	int i;

	// FIXME: locking, races
	for (i = 0; i < bp->children->len; ++i) {
		BreakpointInstance *inst = (BreakpointInstance *)g_ptr_array_index (bp->children, i);

		remove_breakpoint (inst);

		g_free (inst);
	}

	mono_loader_lock ();
	mono_debugger_log_remove_bp (bp, bp->method, bp->il_offset);
	g_ptr_array_remove (breakpoints, bp);
	mono_loader_unlock ();

	g_ptr_array_free (bp->children, TRUE);
	g_free (bp);
}

void
mono_de_collect_breakpoints_by_sp (SeqPoint *sp, MonoJitInfo *ji, GPtrArray *ss_reqs, GPtrArray *bp_reqs)
{
	for (int i = 0; i < breakpoints->len; ++i) {
		MonoBreakpoint *bp = (MonoBreakpoint *)g_ptr_array_index (breakpoints, i);

		if (!bp->method)
			continue;

		for (int j = 0; j < bp->children->len; ++j) {
			BreakpointInstance *inst = (BreakpointInstance *)g_ptr_array_index (bp->children, j);
			if (inst->ji == ji && inst->il_offset == sp->il_offset && inst->native_offset == sp->native_offset) {
				if (bp->req->event_kind == EVENT_KIND_STEP) {
					if (ss_reqs)
						g_ptr_array_add (ss_reqs, bp->req);
				} else {
					if (bp_reqs)
						g_ptr_array_add (bp_reqs, bp->req);
				}
			}
		}
		}
}

static void
breakpoints_cleanup (void)
{
	int i;

	mono_loader_lock ();

	for (i = 0; i < breakpoints->len; ++i)
		g_free (g_ptr_array_index (breakpoints, i));

	g_ptr_array_free (breakpoints, TRUE);
	g_hash_table_destroy (bp_locs);

	breakpoints = NULL;
	bp_locs = NULL;

	mono_loader_unlock ();
}

/*
 * mono_de_clear_breakpoints_for_domain:
 *
 *   Clear breakpoint instances which reference DOMAIN.
 */
void
mono_de_clear_breakpoints_for_domain (MonoDomain *domain)
{
	int i, j;

	/* This could be called after shutdown */
	if (!breakpoints)
		return;

	mono_loader_lock ();
	for (i = 0; i < breakpoints->len; ++i) {
		MonoBreakpoint *bp = (MonoBreakpoint *)g_ptr_array_index (breakpoints, i);

		j = 0;
		while (j < bp->children->len) {
			BreakpointInstance *inst = (BreakpointInstance *)g_ptr_array_index (bp->children, j);

			if (inst->domain == domain) {
				remove_breakpoint (inst);

				g_free (inst);

				g_ptr_array_remove_index_fast (bp->children, j);
			} else {
				j ++;
			}
		}
	}
	mono_loader_unlock ();
}

/* Single stepping engine */
/* Number of single stepping operations in progress */
static int ss_count;

/* The single step request instances */
static GPtrArray *the_ss_reqs;

static void
ss_req_init (void)
{
	the_ss_reqs = g_ptr_array_new ();
}

static void
ss_req_cleanup (void)
{
	dbg_lock ();

	g_ptr_array_free (the_ss_reqs, TRUE);

	the_ss_reqs = NULL;

	dbg_unlock ();
}

/*
 * mono_de_start_single_stepping:
 *
 *   Turn on single stepping. Can be called multiple times, for example,
 * by a single step event request + a suspend.
 */
void
mono_de_start_single_stepping (void)
{
	int val = mono_atomic_inc_i32 (&ss_count);

	if (val == 1) {
#ifdef MONO_ARCH_SOFT_DEBUG_SUPPORTED
		mono_arch_start_single_stepping ();
#endif
		mini_get_interp_callbacks ()->start_single_stepping ();
	}
}

void
mono_de_stop_single_stepping (void)
{
	int val = mono_atomic_dec_i32 (&ss_count);

	if (val == 0) {
#ifdef MONO_ARCH_SOFT_DEBUG_SUPPORTED
		mono_arch_stop_single_stepping ();
#endif
		mini_get_interp_callbacks ()->stop_single_stepping ();
	}
}

static MonoJitInfo*
get_top_method_ji (gpointer ip, MonoDomain **domain, gpointer *out_ip)
{
	MonoJitInfo *ji;

	if (out_ip)
		*out_ip = ip;
	if (domain)
		*domain = mono_get_root_domain ();

	ji = mini_jit_info_table_find (ip);
	if (!ji) {
		/* Could be an interpreter method */

		MonoLMF *lmf = mono_get_lmf ();
		MonoInterpFrameHandle *frame;

		g_assert (((gsize)lmf->previous_lmf) & 2);
		MonoLMFExt *ext = (MonoLMFExt*)lmf;

		g_assert (ext->kind == MONO_LMFEXT_INTERP_EXIT || ext->kind == MONO_LMFEXT_INTERP_EXIT_WITH_CTX);
		frame = (MonoInterpFrameHandle*)ext->interp_exit_data;
		ji = mini_get_interp_callbacks ()->frame_get_jit_info (frame);
		if (domain)
			*domain = mono_domain_get ();
		if (out_ip)
			*out_ip = mini_get_interp_callbacks ()->frame_get_ip (frame);
	}
	return ji;
}

static void
no_seq_points_found (MonoMethod *method, int offset)
{
	/*
	 * This can happen in full-aot mode with assemblies AOTed without the 'soft-debug' option to save space.
	 */
	PRINT_MSG ("Unable to find seq points for method '%s', offset 0x%x.\n", mono_method_full_name (method, TRUE), offset);
}

static const char*
ss_depth_to_string (StepDepth depth)
{
	switch (depth) {
	case STEP_DEPTH_OVER:
		return "over";
	case STEP_DEPTH_OUT:
		return "out";
	case STEP_DEPTH_INTO:
		return "into";
	default:
		g_assert_not_reached ();
		return NULL;
	}
}

/*
 * ss_stop:
 *
 *   Stop the single stepping operation given by SS_REQ.
 */
static void
ss_stop (SingleStepReq *ss_req)
{
	if (ss_req->bps) {
		GSList *l;

		for (l = ss_req->bps; l; l = l->next) {
			mono_de_clear_breakpoint ((MonoBreakpoint *)l->data);
		}
		g_slist_free (ss_req->bps);
		ss_req->bps = NULL;
	}

	ss_req->async_id = 0;
	ss_req->async_stepout_method = NULL;
	if (ss_req->global) {
		mono_de_stop_single_stepping ();
		ss_req->global = FALSE;
	}
}

static void
ss_destroy (SingleStepReq *req)
{
	PRINT_DEBUG_MSG (1, "[dbg] ss_destroy.\n");

	ss_stop (req);

	g_free (req);
}

static SingleStepReq*
ss_req_acquire (MonoInternalThread *thread)
{
	SingleStepReq *req = NULL;
	dbg_lock ();
	int i;
	for (i = 0; i < the_ss_reqs->len; ++i) {
		SingleStepReq *current_req = (SingleStepReq *)g_ptr_array_index (the_ss_reqs, i);
		if (current_req->thread == thread) {
			current_req->refcount ++;	
			req = current_req;
		}
	}
	dbg_unlock ();
	return req;
}

static int 
ss_req_count ()
{
	return the_ss_reqs->len;
}

static void
mono_de_ss_req_release (SingleStepReq *req)
{
	gboolean free = FALSE;

	dbg_lock ();
	g_assert (req->refcount);
	req->refcount --;
	if (req->refcount == 0)
		free = TRUE;
	if (free) {
		g_ptr_array_remove (the_ss_reqs, req);
		ss_destroy (req);
	}
	dbg_unlock ();
}

void
mono_de_cancel_ss (SingleStepReq *req)
{
	if (the_ss_reqs) {
		mono_de_ss_req_release (req);
	}
}

void
mono_de_cancel_all_ss ()
{
	int i;
	for (i = 0; i < the_ss_reqs->len; ++i) {
		SingleStepReq *current_req = (SingleStepReq *)g_ptr_array_index (the_ss_reqs, i);
		mono_de_ss_req_release (current_req);
	}
}

void
mono_de_process_single_step (void *tls, gboolean from_signal)
{
	MonoJitInfo *ji;
	guint8 *ip;
	GPtrArray *reqs;
	int il_offset;
	MonoDomain *domain;
	MonoContext *ctx = rt_callbacks.tls_get_restore_state (tls);
	MonoMethod *method;
	SeqPoint sp;
	MonoSeqPointInfo *info;
	SingleStepReq *ss_req;

	/* Skip the instruction causing the single step */
	rt_callbacks.begin_single_step_processing (ctx, from_signal);

	if (rt_callbacks.try_process_suspend (tls, ctx, FALSE))
		return;

	/*
	 * This can run concurrently with a clear_event_request () call, so needs locking/reference counts.
	 */
	ss_req = ss_req_acquire (mono_thread_internal_current ());

	if (!ss_req)
		// FIXME: A suspend race
		return;
	ip = (guint8 *)MONO_CONTEXT_GET_IP (ctx);

	ji = get_top_method_ji (ip, &domain, (gpointer*)&ip);
	g_assert (ji && !ji->is_trampoline);

	if (log_level > 0) {
		PRINT_DEBUG_MSG (1, "[%p] Single step event (depth=%s) at %s (%p)[0x%x], sp %p, last sp %p\n", (gpointer) (gsize) mono_native_thread_id_get (), ss_depth_to_string (ss_req->depth), mono_method_full_name (jinfo_get_method (ji), TRUE), MONO_CONTEXT_GET_IP (ctx), (int)((guint8*)MONO_CONTEXT_GET_IP (ctx) - (guint8*)ji->code_start), MONO_CONTEXT_GET_SP (ctx), ss_req->last_sp);
	}

	method = jinfo_get_method (ji);
	g_assert (method);

	if (method->wrapper_type && method->wrapper_type != MONO_WRAPPER_DYNAMIC_METHOD)
		goto exit;

	/* 
	 * FIXME:
	 * Stopping in memset makes half-initialized vtypes visible.
	 * Stopping in memcpy makes half-copied vtypes visible.
	 */
	if (method->klass == mono_defaults.string_class && (!strcmp (method->name, "memset") || strstr (method->name, "memcpy")))
		goto exit;

	/*
	 * This could be in mono_de_ss_update method, but mono_find_next_seq_point_for_native_offset is pretty expensive method,
	 * hence we prefer this check here.
	 */
	if (ss_req->user_assemblies) {
		gboolean found = FALSE;
		for (int k = 0; ss_req->user_assemblies[k]; k++)
			if (ss_req->user_assemblies[k] == m_class_get_image (method->klass)->assembly) {
				found = TRUE;
				break;
			}
		if (!found)
			goto exit;
	}

	/*
	 * The ip points to the instruction causing the single step event, which is before
	 * the offset recorded in the seq point map, so find the next seq point after ip.
	 */
	if (!mono_find_next_seq_point_for_native_offset (method, (guint8*)ip - (guint8*)ji->code_start, &info, &sp)) {
		g_assert_not_reached ();
		goto exit;
	}

	il_offset = sp.il_offset;

	if (!mono_de_ss_update (ss_req, ji, &sp, tls, ctx, method))
		goto exit;

	/* Start single stepping again from the current sequence point */

	SingleStepArgs args;
	memset (&args, 0, sizeof (args));
	args.method = method;
	args.ctx = ctx;
	args.tls = tls;
	args.step_to_catch = FALSE;
	args.sp = sp;
	args.info = info;
	args.frames = NULL;
	args.nframes = 0;
	mono_de_ss_start (ss_req, &args);

	if ((ss_req->filter & STEP_FILTER_STATIC_CTOR) &&
		(method->flags & METHOD_ATTRIBUTE_SPECIAL_NAME) &&
		!strcmp (method->name, ".cctor"))
		goto exit;

	// FIXME: Has to lock earlier

	reqs = g_ptr_array_new ();

	mono_loader_lock ();

	g_ptr_array_add (reqs, ss_req->req);

	void *bp_events;
	bp_events = rt_callbacks.create_breakpoint_events (reqs, NULL, ji, EVENT_KIND_BREAKPOINT);

	g_ptr_array_free (reqs, TRUE);

	mono_loader_unlock ();

	rt_callbacks.process_breakpoint_events (bp_events, method, ctx, il_offset);

 exit:
	mono_de_ss_req_release (ss_req);
}

/*
 * mono_de_ss_update:
 *
 * Return FALSE if single stepping needs to continue.
 */
static gboolean
mono_de_ss_update (SingleStepReq *req, MonoJitInfo *ji, SeqPoint *sp, void *tls, MonoContext *ctx, MonoMethod* method)
{
	MonoDebugMethodInfo *minfo;
	MonoDebugSourceLocation *loc = NULL;
	gboolean hit = TRUE;

	if ((req->filter & STEP_FILTER_STATIC_CTOR)) {
		DbgEngineStackFrame **frames;
		int nframes;
		rt_callbacks.ss_calculate_framecount (tls, ctx, TRUE, &frames, &nframes);

		gboolean ret = FALSE;
		gboolean method_in_stack = FALSE;

		for (int i = 0; i < nframes; i++) {
			MonoMethod *external_method = frames [i]->method;
			if (method == external_method)
				method_in_stack = TRUE;

			if (!ret) {
				ret = (external_method->flags & METHOD_ATTRIBUTE_SPECIAL_NAME);
				ret = ret && !strcmp (external_method->name, ".cctor");
				ret = ret && (external_method != req->start_method);
			}
		}

		if (!method_in_stack) {
			PRINT_ERROR_MSG ("[%p] The instruction pointer of the currently executing method(%s) is not on the recorded stack. This is likely due to a runtime bug. The %d frames are as follow: \n", (gpointer)(gsize)mono_native_thread_id_get (), mono_method_full_name (method, TRUE), nframes);
			/*PRINT_DEBUG_MSG (1, "[%p] The instruction pointer of the currently executing method(%s) is not on the recorded stack. This is likely due to a runtime bug. The %d frames are as follow: \n", (gpointer)(gsize)mono_native_thread_id_get (), mono_method_full_name (method, TRUE), tls->frame_count);*/

			for (int i=0; i < nframes; i++)
				PRINT_ERROR_MSG ("\t [%p] Frame (%d / %d): %s\n", (gpointer)(gsize)mono_native_thread_id_get (), i, nframes, mono_method_full_name (frames [i]->method, TRUE));
		}

		rt_callbacks.ss_discard_frame_context (tls);

		if (ret)
			return FALSE;
	}

	if (req->async_stepout_method == method) {
		PRINT_DEBUG_MSG (1, "[%p] Breakpoint hit during async step-out at %s hit, continuing stepping out.\n", (gpointer)(gsize)mono_native_thread_id_get (), method->name);
		return FALSE;
	}

	if (req->depth == STEP_DEPTH_OVER && (sp->flags & MONO_SEQ_POINT_FLAG_NONEMPTY_STACK) && !(sp->flags & MONO_SEQ_POINT_FLAG_NESTED_CALL)) {
		/*
		 * These seq points are inserted by the JIT after calls, step over needs to skip them.
		 */
		PRINT_DEBUG_MSG (1, "[%p] Seq point at nonempty stack %x while stepping over, continuing single stepping.\n", (gpointer) (gsize) mono_native_thread_id_get (), sp->il_offset);
		return FALSE;
	}

	if ((req->depth == STEP_DEPTH_OVER || req->depth == STEP_DEPTH_OUT) && hit && !req->async_stepout_method) {
		gboolean is_step_out = req->depth == STEP_DEPTH_OUT;
		int nframes;
		rt_callbacks.ss_calculate_framecount (tls, ctx, FALSE, NULL, &nframes);

		// Because functions can call themselves recursively, we need to make sure we're stopping at the right stack depth.
		// In case of step out, the target is the frame *enclosing* the one where the request was made.
		int target_frames = req->nframes + (is_step_out ? -1 : 0);
		if (req->nframes > 0 && nframes > 0 && nframes > target_frames) {
			/* Hit the breakpoint in a recursive call, don't halt */
			PRINT_DEBUG_MSG (1, "[%p] Breakpoint at lower frame while stepping %s, continuing single stepping.\n", (gpointer) (gsize) mono_native_thread_id_get (), is_step_out ? "out" : "over");
			return FALSE;
		}
	}

	if (req->depth == STEP_DEPTH_INTO && req->size == STEP_SIZE_MIN && (sp->flags & MONO_SEQ_POINT_FLAG_NONEMPTY_STACK) && req->start_method) {
		int nframes;
		rt_callbacks.ss_calculate_framecount (tls, ctx, FALSE, NULL, &nframes);
		if (req->start_method == method && req->nframes && nframes == req->nframes) { //Check also frame count(could be recursion)
			PRINT_DEBUG_MSG (1, "[%p] Seq point at nonempty stack %x while stepping in, continuing single stepping.\n", (gpointer) (gsize) mono_native_thread_id_get (), sp->il_offset);
			return FALSE;
		}
	}

	MonoDebugMethodAsyncInfo* async_method = mono_debug_lookup_method_async_debug_info (method);
	if (async_method) {
		for (int i = 0; i < async_method->num_awaits; i++) {
			if (async_method->yield_offsets[i] == sp->il_offset || async_method->resume_offsets[i] == sp->il_offset) {
				mono_debug_free_method_async_debug_info (async_method);
				return FALSE;
			}
		}
		mono_debug_free_method_async_debug_info (async_method);
	}

	if (req->size != STEP_SIZE_LINE)
		return TRUE;

	/* Have to check whenever a different source line was reached */
	minfo = mono_debug_lookup_method (method);

	if (minfo)
		loc = mono_debug_method_lookup_location (minfo, sp->il_offset);

	if (!loc) {
		PRINT_DEBUG_MSG (1, "[%p] No line number info for il offset %x, continuing single stepping.\n", (gpointer) (gsize) mono_native_thread_id_get (), sp->il_offset);
		req->last_method = method;
		hit = FALSE;
	} else if (loc && method == req->last_method && loc->row == req->last_line) {
		int nframes;
		rt_callbacks.ss_calculate_framecount (tls, ctx, FALSE, NULL, &nframes);
		if (nframes == req->nframes) { // If the frame has changed we're clearly not on the same source line.
			PRINT_DEBUG_MSG (1, "[%p] Same source line (%d), continuing single stepping.\n", (gpointer) (gsize) mono_native_thread_id_get (), loc->row);
			hit = FALSE;
		}
	}
				
	if (loc) {
		req->last_method = method;
		req->last_line = loc->row;
		mono_debug_free_source_location (loc);
	}

	return hit;
}

void
mono_de_process_breakpoint (void *void_tls, gboolean from_signal)
{
	DebuggerTlsData *tls = (DebuggerTlsData*)void_tls;
	MonoJitInfo *ji;
	guint8 *ip;
	int i;
	guint32 native_offset;
	GPtrArray *bp_reqs, *ss_reqs_orig, *ss_reqs;
	EventKind kind = EVENT_KIND_BREAKPOINT;
	MonoContext *ctx = rt_callbacks.tls_get_restore_state (tls);
	MonoMethod *method;
	MonoSeqPointInfo *info;
	SeqPoint sp;
	gboolean found_sp;

	if (rt_callbacks.try_process_suspend (tls, ctx, TRUE))
		return;

	ip = (guint8 *)MONO_CONTEXT_GET_IP (ctx);

	ji = get_top_method_ji (ip, NULL, (gpointer*)&ip);
	g_assert (ji && !ji->is_trampoline);
	method = jinfo_get_method (ji);

	/* Compute the native offset of the breakpoint from the ip */
	native_offset = ip - (guint8*)ji->code_start;

	if (!rt_callbacks.begin_breakpoint_processing (tls, ctx, ji, from_signal))
		return;

	if (method->wrapper_type)
		return;

	bp_reqs = g_ptr_array_new ();
	ss_reqs = g_ptr_array_new ();
	ss_reqs_orig = g_ptr_array_new ();

	mono_loader_lock ();

	/*
	 * The ip points to the instruction causing the breakpoint event, which is after
	 * the offset recorded in the seq point map, so find the prev seq point before ip.
	 */
	found_sp = mono_find_prev_seq_point_for_native_offset (method, native_offset, &info, &sp);

	if (!found_sp)
		no_seq_points_found (method, native_offset);

	g_assert (found_sp);

	PRINT_DEBUG_MSG (1, "[%p] Breakpoint hit, method=%s, ip=%p, [il=0x%x,native=0x%x].\n", (gpointer) (gsize) mono_native_thread_id_get (), method->name, ip, sp.il_offset, native_offset);

	mono_debugger_log_bp_hit (tls, method, sp.il_offset);

	mono_de_collect_breakpoints_by_sp (&sp, ji, ss_reqs_orig, bp_reqs);

	if (bp_reqs->len == 0 && ss_reqs_orig->len == 0) {
		/* Maybe a method entry/exit event */
		if (sp.il_offset == METHOD_ENTRY_IL_OFFSET)
			kind = EVENT_KIND_METHOD_ENTRY;
		else if (sp.il_offset == METHOD_EXIT_IL_OFFSET)
			kind = EVENT_KIND_METHOD_EXIT;
	}

	/* Process single step requests */
	for (i = 0; i < ss_reqs_orig->len; ++i) {
		EventRequest *req = (EventRequest *)g_ptr_array_index (ss_reqs_orig, i);
		SingleStepReq *ss_req = (SingleStepReq *)req->info;
		gboolean hit;

		//if we hit async_stepout_method, it's our no matter which thread
		if ((ss_req->async_stepout_method != method) && (ss_req->async_id || mono_thread_internal_current () != ss_req->thread)) {
			DbgEngineStackFrame **frames;
			int nframes;
			//We have different thread and we don't have async stepping in progress
			//it's breakpoint in parallel thread, ignore it
			if (ss_req->async_id == 0)
				continue;

			rt_callbacks.ss_discard_frame_context (tls);
			rt_callbacks.ss_calculate_framecount (tls, ctx, FALSE, &frames, &nframes);
			//make sure we have enough data to get current async method instance id
			if (nframes == 0 || !rt_callbacks.ensure_jit (frames [0]))
				continue;

			//Check method is async before calling get_this_async_id
			MonoDebugMethodAsyncInfo* asyncMethod = mono_debug_lookup_method_async_debug_info (method);
			if (!asyncMethod)
				continue;
			else
				mono_debug_free_method_async_debug_info (asyncMethod);

			//breakpoint was hit in parallelly executing async method, ignore it
			if (ss_req->async_id != rt_callbacks.get_this_async_id (frames [0]))
				continue;
		}

		//Update stepping request to new thread/frame_count that we are continuing on
		//so continuing with normal stepping works as expected
		if (ss_req->async_stepout_method || ss_req->async_id) {
			int nframes;
			rt_callbacks.ss_discard_frame_context (tls);
			rt_callbacks.ss_calculate_framecount (tls, ctx, FALSE, NULL, &nframes);
			ss_req->thread = mono_thread_internal_current ();
			ss_req->nframes = nframes;
		}

		hit = mono_de_ss_update (ss_req, ji, &sp, tls, ctx, method);
		if (hit)
			g_ptr_array_add (ss_reqs, req);

		SingleStepArgs args;
		memset (&args, 0, sizeof (args));
		args.method = method;
		args.ctx = ctx;
		args.tls = tls;
		args.step_to_catch = FALSE;
		args.sp = sp;
		args.info = info;
		args.frames = NULL;
		args.nframes = 0;
		mono_de_ss_start (ss_req, &args);
	}

	void *bp_events = rt_callbacks.create_breakpoint_events (ss_reqs, bp_reqs, ji, kind);

	mono_loader_unlock ();

	g_ptr_array_free (bp_reqs, TRUE);
	g_ptr_array_free (ss_reqs, TRUE);

	rt_callbacks.process_breakpoint_events (bp_events, method, ctx, sp.il_offset);
}

/*
 * ss_bp_is_unique:
 *
 * Reject breakpoint if it is a duplicate of one already in list or hash table.
 */
static gboolean
ss_bp_is_unique (GSList *bps, GHashTable *ss_req_bp_cache, MonoMethod *method, guint32 il_offset)
{
	if (ss_req_bp_cache) {
		MonoBreakpoint dummy = {method, (long)il_offset, NULL, NULL};
		return !g_hash_table_lookup (ss_req_bp_cache, &dummy);
	}
	for (GSList *l = bps; l; l = l->next) {
		MonoBreakpoint *bp = (MonoBreakpoint *)l->data;
		if (bp->method == method && bp->il_offset == il_offset)
			return FALSE;
	}
	return TRUE;
}

/*
 * ss_bp_eq:
 *
 * GHashTable equality for a MonoBreakpoint (only care about method and il_offset fields)
 */
static gint
ss_bp_eq (gconstpointer ka, gconstpointer kb)
{
	const MonoBreakpoint *s1 = (const MonoBreakpoint *)ka;
	const MonoBreakpoint *s2 = (const MonoBreakpoint *)kb;
	return (s1->method == s2->method && s1->il_offset == s2->il_offset) ? 1 : 0;
}

/*
 * ss_bp_eq:
 *
 * GHashTable hash for a MonoBreakpoint (only care about method and il_offset fields)
 */
static guint
ss_bp_hash (gconstpointer data)
{
	const MonoBreakpoint *s = (const MonoBreakpoint *)data;
	guint hash = (guint) (uintptr_t) s->method;
	hash ^= ((guint)s->il_offset) << 16; // Assume low bits are more interesting
	hash ^= ((guint)s->il_offset) >> 16;
	return hash;
}

#define MAX_LINEAR_SCAN_BPS 7

/*
 * ss_bp_add_one:
 *
 * Create a new breakpoint and add it to a step request.
 * Will adjust the bp count and cache used by mono_de_ss_start.
 */
static void
ss_bp_add_one (SingleStepReq *ss_req, int *ss_req_bp_count, GHashTable **ss_req_bp_cache,
	          MonoMethod *method, guint32 il_offset)
{
	// This list is getting too long, switch to using the hash table
	if (!*ss_req_bp_cache && *ss_req_bp_count > MAX_LINEAR_SCAN_BPS) {
		*ss_req_bp_cache = g_hash_table_new (ss_bp_hash, ss_bp_eq);
		for (GSList *l = ss_req->bps; l; l = l->next)
			g_hash_table_insert (*ss_req_bp_cache, l->data, l->data);
	}

	if (ss_bp_is_unique (ss_req->bps, *ss_req_bp_cache, method, il_offset)) {
		// Create and add breakpoint
		MonoBreakpoint *bp = mono_de_set_breakpoint (method, il_offset, ss_req->req, NULL);
		ss_req->bps = g_slist_append (ss_req->bps, bp);
		if (*ss_req_bp_cache)
			g_hash_table_insert (*ss_req_bp_cache, bp, bp);
		(*ss_req_bp_count)++;
	} else {
		PRINT_DEBUG_MSG (1, "[dbg] Candidate breakpoint at %s:[il=0x%x] is a duplicate for this step request, will not add.\n", mono_method_full_name (method, TRUE), (int)il_offset);
	}
}

static gboolean
is_last_non_empty (SeqPoint* sp, MonoSeqPointInfo *info)
{
	if (!sp->next_len)
		return TRUE;
	SeqPoint* next = g_new (SeqPoint, sp->next_len);
	mono_seq_point_init_next (info, *sp, next);
	for (int i = 0; i < sp->next_len; i++) {
		if (next [i].flags & MONO_SEQ_POINT_FLAG_NONEMPTY_STACK && !(next [i].flags & MONO_SEQ_POINT_FLAG_NESTED_CALL)) {
			if (!is_last_non_empty (&next [i], info)) {
				g_free (next);
				return FALSE;
			}
		} else {
			g_free (next);
			return FALSE;
		}
	}
	g_free (next);
	return TRUE;
}

/*
 * mono_de_ss_start:
 *
 *   Start the single stepping operation given by SS_REQ from the sequence point SP.
 * If CTX is not set, then this can target any thread. If CTX is set, then TLS should
 * belong to the same thread as CTX.
 * If FRAMES is not-null, use that instead of tls->frames for placing breakpoints etc.
 */
static void
mono_de_ss_start (SingleStepReq *ss_req, SingleStepArgs *ss_args)
{
	int i, j, frame_index;
	SeqPoint *next_sp, *parent_sp = NULL;
	SeqPoint local_sp, local_parent_sp;
	gboolean found_sp;
	MonoSeqPointInfo *parent_info;
	MonoMethod *parent_sp_method = NULL;
	gboolean enable_global = FALSE;

	// When 8 or more entries are in bps, we build a hash table to serve as a set of breakpoints.
	// Recreating this on each pass is a little wasteful but at least keeps behavior linear.
	int ss_req_bp_count = g_slist_length (ss_req->bps);
	GHashTable *ss_req_bp_cache = NULL;

	/* Stop the previous operation */
	ss_stop (ss_req);

	gboolean locked = FALSE;

	void *tls = ss_args->tls;
	MonoMethod *method = ss_args->method;
	DbgEngineStackFrame **frames = ss_args->frames;
	int nframes = ss_args->nframes;
	SeqPoint *sp = &ss_args->sp;

	/* this can happen on a single step in a exception on android (Mono_UnhandledException_internal) and on IOS */
	if (!method)
		return;

	/*
	 * Implement single stepping using breakpoints if possible.
	 */
	if (ss_args->step_to_catch) {
		ss_bp_add_one (ss_req, &ss_req_bp_count, &ss_req_bp_cache, method, sp->il_offset);
	} else {
		frame_index = 1;

#ifndef TARGET_WASM
		if (ss_args->ctx && !frames) {
#else
		if (!frames) {
#endif
			mono_loader_lock ();
			locked = TRUE;

			/* Need parent frames */
			rt_callbacks.ss_calculate_framecount (tls, ss_args->ctx, FALSE, &frames, &nframes);
		}

		MonoDebugMethodAsyncInfo* asyncMethod = mono_debug_lookup_method_async_debug_info (method);

		/* Need to stop in catch clauses as well */
		for (i = ss_req->depth == STEP_DEPTH_OUT ? 1 : 0; i < nframes; ++i) {
			DbgEngineStackFrame *frame = frames [i];

			if (frame->ji) {
				MonoJitInfo *jinfo = frame->ji;
				for (j = 0; j < jinfo->num_clauses; ++j) {
					// In case of async method we don't want to place breakpoint on last catch handler(which state machine added for whole method)
					if (asyncMethod && asyncMethod->num_awaits && i == 0 && j + 1 == jinfo->num_clauses)
						break;
					MonoJitExceptionInfo *ei = &jinfo->clauses [j];

					if (mono_find_next_seq_point_for_native_offset (frame->method, (char*)ei->handler_start - (char*)jinfo->code_start, NULL, &local_sp))
						ss_bp_add_one (ss_req, &ss_req_bp_count, &ss_req_bp_cache, frame->method, local_sp.il_offset);
				}
			}
		}

		if (asyncMethod && asyncMethod->num_awaits && nframes && rt_callbacks.ensure_jit (frames [0])) {
			//asyncMethod has value and num_awaits > 0, this means we are inside async method with awaits

			// Check if we hit yield_offset during normal stepping, because if we did...
			// Go into special async stepping mode which places breakpoint on resumeOffset
			// of this await call and sets async_id so we can distinguish it from parallel executions
			for (i = 0; i < asyncMethod->num_awaits; i++) {
				if (sp->il_offset == asyncMethod->yield_offsets [i]) {
					ss_req->async_id = rt_callbacks.get_this_async_id (frames [0]);
					ss_bp_add_one (ss_req, &ss_req_bp_count, &ss_req_bp_cache, method, asyncMethod->resume_offsets [i]);
					g_hash_table_destroy (ss_req_bp_cache);
					mono_debug_free_method_async_debug_info (asyncMethod);
					if (locked)
						mono_loader_unlock ();
					goto cleanup;
				}
			}
			//If we are at end of async method and doing step-in or step-over...
			//Switch to step-out, so whole NotifyDebuggerOfWaitCompletion magic happens...
			if (is_last_non_empty (sp, ss_args->info)) {
				ss_req->depth = STEP_DEPTH_OUT;//setting depth to step-out is important, don't inline IF, because code later depends on this
			}
			if (ss_req->depth == STEP_DEPTH_OUT) {
				//If we are inside `async void` method, do normal step-out
				if (rt_callbacks.set_set_notification_for_wait_completion_flag (frames [0])) {
					ss_req->async_id = rt_callbacks.get_this_async_id (frames [0]);
					ss_req->async_stepout_method = rt_callbacks.get_notify_debugger_of_wait_completion_method ();
					ss_bp_add_one (ss_req, &ss_req_bp_count, &ss_req_bp_cache, ss_req->async_stepout_method, 0);
					g_hash_table_destroy (ss_req_bp_cache);
					mono_debug_free_method_async_debug_info (asyncMethod);
					if (locked)
						mono_loader_unlock ();
					goto cleanup;
				}
			}
		}

		if (asyncMethod)
			mono_debug_free_method_async_debug_info (asyncMethod);

		/*
		* Find the first sequence point in the current or in a previous frame which
		* is not the last in its method.
		*/
		if (ss_req->depth == STEP_DEPTH_OUT) {
			/* Ignore seq points in current method */
			while (frame_index < nframes) {
				DbgEngineStackFrame *frame = frames [frame_index];

				method = frame->method;
				found_sp = mono_find_prev_seq_point_for_native_offset (frame->method, frame->native_offset, &ss_args->info, &local_sp);
				sp = (found_sp)? &local_sp : NULL;
				frame_index ++;
				if (sp && sp->next_len != 0)
					break;
			}
			// There could be method calls before the next seq point in the caller when using nested calls
			//enable_global = TRUE;
		} else {
			if (sp && sp->next_len == 0) {
				sp = NULL;
				while (frame_index < nframes) {
					DbgEngineStackFrame *frame = frames [frame_index];

					method = frame->method;
					found_sp = mono_find_prev_seq_point_for_native_offset (frame->method, frame->native_offset, &ss_args->info, &local_sp);
					sp = (found_sp)? &local_sp : NULL;
					if (sp && sp->next_len != 0)
						break;
					sp = NULL;
					frame_index ++;
				}
			} else {
				/* Have to put a breakpoint into a parent frame since the seq points might not cover all control flow out of the method */
				while (frame_index < nframes) {
					DbgEngineStackFrame *frame = frames [frame_index];

					parent_sp_method = frame->method;
					found_sp = mono_find_prev_seq_point_for_native_offset (frame->method, frame->native_offset, &parent_info, &local_parent_sp);
					parent_sp = found_sp ? &local_parent_sp : NULL;
					if (found_sp && parent_sp->next_len != 0)
						break;
					parent_sp = NULL;
					frame_index ++;
				}
			}
		}

		if (sp && sp->next_len > 0) {
			SeqPoint* next = g_new(SeqPoint, sp->next_len);

			mono_seq_point_init_next (ss_args->info, *sp, next);
			for (i = 0; i < sp->next_len; i++) {
				next_sp = &next[i];

				ss_bp_add_one (ss_req, &ss_req_bp_count, &ss_req_bp_cache, method, next_sp->il_offset);
			}
			g_free (next);
		}

		if (parent_sp) {
			SeqPoint* next = g_new(SeqPoint, parent_sp->next_len);

			mono_seq_point_init_next (parent_info, *parent_sp, next);
			for (i = 0; i < parent_sp->next_len; i++) {
				next_sp = &next[i];

				ss_bp_add_one (ss_req, &ss_req_bp_count, &ss_req_bp_cache, parent_sp_method, next_sp->il_offset);
			}
			g_free (next);
		}

		if (ss_req->nframes == 0)
			ss_req->nframes = nframes;

		if ((ss_req->depth == STEP_DEPTH_OVER) && (!sp && !parent_sp)) {
			PRINT_DEBUG_MSG (1, "[dbg] No parent frame for step over, transition to step into.\n");
			/*
			 * This is needed since if we leave managed code, and later return to it, step over
			 * is not going to stop.
			 * This approach is a bit ugly, since we change the step depth, but it only affects
			 * clients who reuse the same step request, and only in this special case.
			 */
			ss_req->depth = STEP_DEPTH_INTO;
		}

		if (ss_req->depth == STEP_DEPTH_INTO) {
			/* Enable global stepping so we stop at method entry too */
			enable_global = TRUE;
		}

		/*
		 * The ctx/frame info computed above will become invalid when we continue.
		 */
		rt_callbacks.ss_discard_frame_context (tls);
	}

	if (enable_global) {
		PRINT_DEBUG_MSG (1, "[dbg] Turning on global single stepping.\n");
		ss_req->global = TRUE;
		mono_de_start_single_stepping ();
	} else if (!ss_req->bps) {
		PRINT_DEBUG_MSG (1, "[dbg] Turning on global single stepping.\n");
		ss_req->global = TRUE;
		mono_de_start_single_stepping ();
	} else {
		ss_req->global = FALSE;
	}

	g_hash_table_destroy (ss_req_bp_cache);

	if (locked)
		mono_loader_unlock ();

cleanup:
	rt_callbacks.ss_args_destroy (ss_args);
}


/*
 * Start single stepping of thread THREAD
 */
DbgEngineErrorCode
mono_de_ss_create (MonoInternalThread *thread, StepSize size, StepDepth depth, StepFilter filter, EventRequest *req)
{
	int err = rt_callbacks.ensure_runtime_is_suspended ();
	if (err)
		return err;

	// FIXME: Multiple requests
	if (ss_req_count () > 1) {
		err = rt_callbacks.handle_multiple_ss_requests ();

		if (err == DE_ERR_NOT_IMPLEMENTED) {
			PRINT_DEBUG_MSG (0, "Received a single step request while the previous one was still active.\n");		
			return DE_ERR_NOT_IMPLEMENTED;
		}
	}

	PRINT_DEBUG_MSG (1, "[dbg] Starting single step of thread %p (depth=%s).\n", thread, ss_depth_to_string (depth));

	SingleStepReq *ss_req = g_new0 (SingleStepReq, 1);
	ss_req->req = req;
	ss_req->thread = thread;
	ss_req->size = size;
	ss_req->depth = depth;
	ss_req->filter = filter;
	ss_req->refcount = 1;
	req->info = ss_req;

	for (int i = 0; i < req->nmodifiers; i++) {
		if (req->modifiers[i].kind == MOD_KIND_ASSEMBLY_ONLY) {
			ss_req->user_assemblies = req->modifiers[i].data.assemblies;
			break;
		}
	}

	SingleStepArgs args;
	err = rt_callbacks.ss_create_init_args (ss_req, &args);
	if (err)
		return err;
	g_ptr_array_add (the_ss_reqs, ss_req);

	mono_de_ss_start (ss_req, &args);

	return DE_ERR_NONE;
}

/*
 * mono_de_set_log_level:
 *
 * Configures logging level and output file. Must be called together with mono_de_init.
 */
void
mono_de_set_log_level (int level, FILE *file)
{
	log_level = level;
	log_file = file;
}

/*
 * mono_de_init:
 *
 * Inits the shared debugger engine. Not reentrant.
 */
void
mono_de_init (DebuggerEngineCallbacks *cbs)
{
	rt_callbacks = *cbs;
	mono_coop_mutex_init_recursive (&debug_mutex);

	domains_init ();
	breakpoints_init ();
	ss_req_init ();
	mono_debugger_log_init ();
}

void
mono_de_cleanup (void)
{
	breakpoints_cleanup ();
	domains_cleanup ();
	ss_req_cleanup ();
}

void
mono_debugger_free_objref (gpointer value)
{
	ObjRef *o = (ObjRef *)value;

	mono_gchandle_free_internal (o->handle);

	g_free (o);
}

// Returns true if TaskBuilder has NotifyDebuggerOfWaitCompletion method
// false if not(AsyncVoidBuilder)
MonoClass *
get_class_to_get_builder_field(DbgEngineStackFrame *frame)
{
	ERROR_DECL (error);
	gpointer this_addr = get_this_addr (frame);
	MonoClass *original_class = frame->method->klass;
	MonoClass *ret;
	if (!m_class_is_valuetype (original_class) && mono_class_is_open_constructed_type (m_class_get_byval_arg (original_class))) {
		MonoObject *this_obj = *(MonoObject**)this_addr;
		MonoGenericContext context;
		MonoType *inflated_type;

		if (!this_obj)
			return NULL;
			
		context = mono_get_generic_context_from_stack_frame (frame->ji, this_obj->vtable);
		inflated_type = mono_class_inflate_generic_type_checked (m_class_get_byval_arg (original_class), &context, error);
		mono_error_assert_ok (error); /* FIXME don't swallow the error */

		ret = mono_class_from_mono_type_internal (inflated_type);
		mono_metadata_free_type (inflated_type);
		return ret;
	}
	return original_class;
}


gboolean
set_set_notification_for_wait_completion_flag (DbgEngineStackFrame *frame)
{
	MonoClassField *builder_field = mono_class_get_field_from_name_full (get_class_to_get_builder_field(frame), "<>t__builder", NULL);
	if (!builder_field)
		return FALSE;
	gpointer builder = get_async_method_builder (frame);
	if (!builder)
		return FALSE;

	MonoMethod* method = get_set_notification_method (mono_class_from_mono_type_internal (builder_field->type));
	if (method == NULL)
		return FALSE;
	gboolean arg = TRUE;
	ERROR_DECL (error);
	void *args [ ] = { &arg };
	mono_runtime_invoke_checked (method, builder, args, error);
	mono_error_assert_ok (error);
	return TRUE;
}

MonoMethod*
get_object_id_for_debugger_method (MonoClass* async_builder_class)
{
	ERROR_DECL (error);
	GPtrArray *array = mono_class_get_methods_by_name (async_builder_class, "get_ObjectIdForDebugger", 0x24, 1, FALSE, error);
	mono_error_assert_ok (error);
	if (array->len != 1) {
		g_ptr_array_free (array, TRUE);
		//if we don't find method get_ObjectIdForDebugger we try to find the property Task to continue async debug.
		MonoProperty *prop = mono_class_get_property_from_name_internal (async_builder_class, "Task");
		if (!prop) {
			PRINT_DEBUG_MSG (1, "Impossible to debug async methods.\n");
			return NULL;
		}
		return prop->get;
	}
	MonoMethod *method = (MonoMethod *)g_ptr_array_index (array, 0);
	g_ptr_array_free (array, TRUE);
	return method;
}

gpointer
get_this_addr (DbgEngineStackFrame *the_frame)
{
	StackFrame *frame = (StackFrame *)the_frame;
	if (frame->de.ji->is_interp)
		return mini_get_interp_callbacks ()->frame_get_this (frame->interp_frame);

	MonoDebugVarInfo *var = frame->jit->this_var;
	if ((var->index & MONO_DEBUG_VAR_ADDRESS_MODE_FLAGS) != MONO_DEBUG_VAR_ADDRESS_MODE_REGOFFSET)
		return NULL;

	guint8 *addr = (guint8 *)mono_arch_context_get_int_reg (&frame->ctx, var->index & ~MONO_DEBUG_VAR_ADDRESS_MODE_FLAGS);
	addr += (gint32)var->offset;
	return addr;
}

/* Return the address of the AsyncMethodBuilder struct belonging to the state machine method pointed to by FRAME */
gpointer
get_async_method_builder (DbgEngineStackFrame *frame)
{
	MonoObject *this_obj;
	MonoClassField *builder_field;
	gpointer builder;
	gpointer this_addr;
	MonoClass* klass = frame->method->klass;

	klass = get_class_to_get_builder_field(frame);
	builder_field = mono_class_get_field_from_name_full (klass, "<>t__builder", NULL);
	if (!builder_field)
		return NULL;

	this_addr = get_this_addr (frame);
	if (!this_addr)
		return NULL;

	if (m_class_is_valuetype (klass)) {
		builder = mono_vtype_get_field_addr (*(guint8**)this_addr, builder_field);
	} else {
		this_obj = *(MonoObject**)this_addr;
		builder = (char*)this_obj + builder_field->offset;
	}

	return builder;
}

MonoMethod*
get_set_notification_method (MonoClass* async_builder_class)
{
	ERROR_DECL (error);
	GPtrArray* array = mono_class_get_methods_by_name (async_builder_class, "SetNotificationForWaitCompletion", 0x24, 1, FALSE, error);
	mono_error_assert_ok (error);
	if (array->len == 0) {
		g_ptr_array_free (array, TRUE);
		return NULL;
	}
	MonoMethod* set_notification_method = (MonoMethod *)g_ptr_array_index (array, 0);
	g_ptr_array_free (array, TRUE);
	return set_notification_method;
}

static MonoMethod* notify_debugger_of_wait_completion_method_cache;

MonoMethod*
get_notify_debugger_of_wait_completion_method (void)
{
	if (notify_debugger_of_wait_completion_method_cache != NULL)
		return notify_debugger_of_wait_completion_method_cache;
	ERROR_DECL (error);
	MonoClass* task_class = mono_class_load_from_name (mono_defaults.corlib, "System.Threading.Tasks", "Task");
	GPtrArray* array = mono_class_get_methods_by_name (task_class, "NotifyDebuggerOfWaitCompletion", 0x24, 1, FALSE, error);
	mono_error_assert_ok (error);
	g_assert (array->len == 1);
	notify_debugger_of_wait_completion_method_cache = (MonoMethod *)g_ptr_array_index (array, 0);
	g_ptr_array_free (array, TRUE);
	return notify_debugger_of_wait_completion_method_cache;
}

DbgEngineErrorCode
mono_de_set_interp_var (MonoType *t, gpointer addr, guint8 *val_buf)
{
	int size;

	if (t->byref) {
		addr = *(gpointer*)addr;
		if (!addr)
			return ERR_INVALID_OBJECT;
	}

	if (MONO_TYPE_IS_REFERENCE (t))
		size = sizeof (gpointer);
	else
		size = mono_class_value_size (mono_class_from_mono_type_internal (t), NULL);

	memcpy (addr, val_buf, size);
	return ERR_NONE;
}

#endif
