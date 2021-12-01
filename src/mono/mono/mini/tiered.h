#ifdef ENABLE_EXPERIMENT_TIERED

#ifndef __MONO_MINI_TIERED_H__
#define __MONO_MINI_TIERED_H__

#define TIERED_PATCH_KIND_INTERP 0
#define TIERED_PATCH_KIND_JIT 1
#define TIERED_PATCH_KIND_NUM 2

typedef struct {
	int hotness;
	gboolean promoted;
} MiniTieredCounter;

typedef struct {
	gint64 methods_promoted;
} MiniTieredStats;

typedef struct {
	MonoMethod *target_method;
	int tier_level;
} MiniTieredPatchPointContext;

typedef gboolean (*CallsitePatcher)(MiniTieredPatchPointContext *context, gpointer patchsite);

void
mini_tiered_init (void);

void
mini_tiered_inc (MonoMethod *method, MiniTieredCounter *tcnt, int level);

void
mini_tiered_record_callsite (gpointer callsite, MonoMethod *target_method, int level);

void
mini_tiered_register_callsite_patcher (CallsitePatcher func, int level);

#endif /* __MONO_MINI_TIERED_H__ */
#endif /* ENABLE_EXPERIMENT_TIERED */
