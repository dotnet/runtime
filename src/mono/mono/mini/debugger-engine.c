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

/*
 * Logging support
 */
static int log_level;
static FILE *log_file;

#ifdef HOST_ANDROID
#define DEBUG_PRINTF(level, ...) do { if (G_UNLIKELY ((level) <= log_level)) { g_print (__VA_ARGS__); } } while (0)
#else
#define DEBUG_PRINTF(level, ...) do { if (G_UNLIKELY ((level) <= log_level)) { fprintf (log_file, __VA_ARGS__); fflush (log_file); } } while (0)
#endif

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
		char *s = g_strdup_printf ("Unable to insert breakpoint at %s:%d", mono_method_full_name (jinfo_get_method (ji), TRUE), bp->il_offset);

		mono_seq_point_iterator_init (&it, seq_points);
		while (mono_seq_point_iterator_next (&it))
			DEBUG_PRINTF (1, "%d\n", it.seq_point.il_offset);

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
		DEBUG_PRINTF (1, "[dbg] Attempting to insert seq point at dead IL offset %d, ignoring.\n", (int)bp->il_offset);
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

	DEBUG_PRINTF (1, "[dbg] Inserted breakpoint at %s:[il=0x%x,native=0x%x] [%p](%d).\n", mono_method_full_name (jinfo_get_method (ji), TRUE), (int)it.seq_point.il_offset, (int)it.seq_point.native_offset, inst->ip, count);
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
		DEBUG_PRINTF (1, "[dbg] Clear breakpoint at %s [%p].\n", mono_method_full_name (jinfo_get_method (ji), TRUE), ip);
	}
}

/*
 * This doesn't take any locks.
 */
static inline gboolean
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

int
mono_de_current_breakpoints (MonoBreakpoint **out)
{
	mono_loader_lock ();

	int len = breakpoints->len;
	MonoBreakpoint *bps = g_malloc0 (sizeof (MonoBreakpoint) * len);

	for (int i = 0; i < len; ++i)
		bps [i] = *(MonoBreakpoint *) g_ptr_array_index (breakpoints, i);

	mono_loader_unlock ();

	*out = bps;

	return len;
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
	MonoMethod *jmethod;

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
			MonoMethod *declaring = NULL;

			jmethod = jinfo_get_method (ji);
			if (jmethod->is_inflated)
				declaring = mono_method_get_declaring_generic_method (jmethod);

			mono_domain_lock (domain);
			seq_points = (MonoSeqPointInfo *)g_hash_table_lookup (domain_jit_info (domain)->seq_points, jmethod);
			if (!seq_points && declaring)
				seq_points = (MonoSeqPointInfo *)g_hash_table_lookup (domain_jit_info (domain)->seq_points, declaring);
			mono_domain_unlock (domain);
			if (!seq_points)
				/* Could be AOT code */
				continue;
			g_assert (seq_points);

			insert_breakpoint (seq_points, domain, ji, bp, NULL);
		}
	}

	mono_loader_unlock ();
}

static void
set_bp_in_method (MonoDomain *domain, MonoMethod *method, MonoSeqPointInfo *seq_points, MonoBreakpoint *bp, MonoError *error)
{
	gpointer code;
	MonoJitInfo *ji;

	if (error)
		error_init (error);

	code = mono_jit_find_compiled_method_with_jit_info (domain, method, &ji);
	if (!code) {
		ERROR_DECL_VALUE (oerror);

		/* Might be AOTed code */
		mono_class_init (method->klass);
		code = mono_aot_get_method (domain, method, &oerror);
		if (code) {
			mono_error_assert_ok (&oerror);
			ji = mono_jit_info_table_find (domain, code);
		} else {
			/* Might be interpreted */
			ji = mini_get_interp_callbacks ()->find_jit_info (domain, method);
		}
		g_assert (ji);
	}

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
	MonoDomain *domain = key;
	MonoSeqPointInfo *seq_points;
	CollectDomainData *ud = user_data;
	MonoMethod *m;

	mono_domain_lock (domain);
	g_hash_table_iter_init (&iter, domain_jit_info (domain)->seq_points);
	while (g_hash_table_iter_next (&iter, (void**)&m, (void**)&seq_points)) {
		if (bp_matches_method (ud->bp, m)) {
			/* Save the info locally to simplify the code inside the domain lock */
			g_ptr_array_add (ud->methods, m);
			g_ptr_array_add (ud->method_domains, domain);
			g_ptr_array_add (ud->method_seq_points, seq_points);
		}
	}
	mono_domain_unlock (domain);
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

	DEBUG_PRINTF (1, "[dbg] Setting %sbreakpoint at %s:0x%x.\n", (req->event_kind == EVENT_KIND_STEP) ? "single step " : "", method ? mono_method_full_name (method, TRUE) : "<all>", (int)il_offset);

	methods = g_ptr_array_new ();
	method_domains = g_ptr_array_new ();
	method_seq_points = g_ptr_array_new ();

	mono_loader_lock ();

	CollectDomainData user_data = {
		.bp = bp,
		.methods = methods,
		.method_domains = method_domains,
		.method_seq_points = method_seq_points
	};
	mono_de_foreach_domain (collect_domain_bp, &user_data);

	for (i = 0; i < methods->len; ++i) {
		m = (MonoMethod *)g_ptr_array_index (methods, i);
		domain = (MonoDomain *)g_ptr_array_index (method_domains, i);
		seq_points = (MonoSeqPointInfo *)g_ptr_array_index (method_seq_points, i);
		set_bp_in_method (domain, m, seq_points, bp, error);
	}

	g_ptr_array_add (breakpoints, bp);
	mono_debugger_log_add_bp (bp->method, bp->il_offset);
	mono_loader_unlock ();

	g_ptr_array_free (methods, TRUE);
	g_ptr_array_free (method_domains, TRUE);
	g_ptr_array_free (method_seq_points, TRUE);

	if (error && !mono_error_ok (error)) {
		mono_de_clear_breakpoint (bp);
		return NULL;
	}

	return bp;
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
	mono_debugger_log_remove_bp (bp->method, bp->il_offset);
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
					g_ptr_array_add (ss_reqs, bp->req);
				} else {
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
mono_de_init (void)
{
	mono_coop_mutex_init_recursive (&debug_mutex);

	domains_init ();
	breakpoints_init ();
}

void
mono_de_cleanup (void)
{
	breakpoints_cleanup ();
	domains_cleanup ();
}

#endif
