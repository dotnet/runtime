#include <config.h>
#include <mono/utils/mono-compiler.h>

#include "mini.h"
#include "mini-runtime.h"
#include <mono/utils/mono-counters.h>
#include <mono/utils/mono-logger-internals.h>

#ifdef ENABLE_EXPERIMENT_TIERED

MiniTieredStats mini_tiered_stats;

static MonoCoopCond compilation_wait;
static MonoCoopMutex compilation_mutex;

#define NUM_TIERS 2

static GSList *compilation_queue [NUM_TIERS];
static CallsitePatcher patchers [NUM_TIERS] = { NULL };

static const char* const patch_kind_str[] = {
	"INTERP",
	"JIT",
};

static GHashTable *callsites_hash [TIERED_PATCH_KIND_NUM] = { NULL };

/* TODO: use scientific methods (TM) to determine values */
static const int threshold [NUM_TIERS] = {
	1000, /* tier 0 */
	3000, /* tier 1 */
};

static void
compiler_thread (void)
{
	MonoInternalThread *internal = mono_thread_internal_current ();
	internal->state |= ThreadState_Background;
	internal->flags |= MONO_THREAD_FLAG_DONT_MANAGE;

	mono_native_thread_set_name (mono_native_thread_id_get (), "Tiered Compilation Thread");

	while (TRUE) {
		mono_coop_cond_wait (&compilation_wait, &compilation_mutex);

		for (int tier_level = 0; tier_level < NUM_TIERS; tier_level++) {
			GSList *ppcs = compilation_queue [tier_level];
			compilation_queue [tier_level] = NULL;

			for (GSList *ppc_= ppcs; ppc_ != NULL; ppc_ = ppc_->next) {
				MiniTieredPatchPointContext *ppc = (MiniTieredPatchPointContext *) ppc_->data;

				for (int patch_kind = 0; patch_kind < TIERED_PATCH_KIND_NUM; patch_kind++) {
					if (!callsites_hash [patch_kind])
						continue;

					GSList *patchsites = g_hash_table_lookup (callsites_hash [patch_kind], ppc->target_method);

					for (; patchsites != NULL; patchsites = patchsites->next) {
						gpointer patchsite = (gpointer) patchsites->data;

						mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_TIERED, "tiered: patching %p with patch_kind=%s @ tier_level=%d", patchsite, patch_kind_str [patch_kind], tier_level);
						mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_TIERED, "\t-> caller=%s", mono_pmip (patchsite));
						mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_TIERED, "\t-> callee=%s", mono_method_full_name (ppc->target_method, TRUE));

						gboolean success = patchers [patch_kind] (ppc, patchsite);

						if (!success)
							mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_TIERED, "tiered: couldn't patch %p with target %s, dropping it.", patchsite, mono_method_full_name (ppc->target_method, TRUE));
					}
					g_hash_table_remove (callsites_hash [patch_kind], ppc->target_method);
					g_slist_free (patchsites);
				}
				g_free (ppc);
			}
			g_slist_free (ppcs);
		}
		mono_coop_mutex_unlock (&compilation_mutex);
	}
}

void
mini_tiered_init (void)
{
	ERROR_DECL (error);

	mono_counters_init ();
	mono_counters_register ("Methods promoted", MONO_COUNTER_TIERED | MONO_COUNTER_LONG, &mini_tiered_stats.methods_promoted);

	mono_coop_cond_init (&compilation_wait);
	mono_coop_mutex_init (&compilation_mutex);

	mono_thread_create_internal ((MonoThreadStart)compiler_thread, NULL, MONO_THREAD_CREATE_FLAGS_THREADPOOL, error);
	mono_error_assert_ok (error);
}

void
mini_tiered_register_callsite_patcher (CallsitePatcher func, int level)
{
	g_assert (level < NUM_TIERS);

	patchers [level] = func;
}

void
mini_tiered_record_callsite (gpointer ip, MonoMethod *target_method, int patch_kind)
{
	if (!callsites_hash [patch_kind])
		callsites_hash [patch_kind] = g_hash_table_new (NULL, NULL);

	GSList *patchsites = g_hash_table_lookup (callsites_hash [patch_kind], target_method);
	patchsites = g_slist_prepend (patchsites, ip);
	g_hash_table_insert (callsites_hash [patch_kind], target_method, patchsites);
}

void
mini_tiered_inc (MonoMethod *method, MiniTieredCounter *tcnt, int tier_level)
{
	if (G_UNLIKELY (tcnt->hotness == threshold [tier_level] && !tcnt->promoted)) {
		tcnt->promoted = TRUE;
		mini_tiered_stats.methods_promoted++;

		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_TIERED, "tiered: queued %s", mono_method_full_name (method, TRUE));

		MiniTieredPatchPointContext *ppc = g_new0 (MiniTieredPatchPointContext, 1);
		ppc->target_method = method;
		ppc->tier_level = tier_level;

		mono_coop_mutex_lock (&compilation_mutex);
		compilation_queue [tier_level] = g_slist_append (compilation_queue [tier_level], ppc);
		mono_coop_mutex_unlock (&compilation_mutex);
		mono_coop_cond_signal (&compilation_wait);
	} else if (!tcnt->promoted) {
		/* FIXME: inline that into caller */
		tcnt->hotness++;
	}
}
#else
MONO_EMPTY_SOURCE_FILE (tiered);
#endif
