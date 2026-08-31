#ifndef __MONO_MINI_INTERP_PGO_H__
#define __MONO_MINI_INTERP_PGO_H__

gboolean
mono_interp_pgo_should_tier_method (MonoMethod *method);

void
mono_interp_pgo_method_was_tiered (MonoMethod *method);

void
mono_interp_pgo_generate_start (void);

void
mono_interp_pgo_generate_end (void);

#endif // __MONO_MINI_INTERP_PGO_H__
