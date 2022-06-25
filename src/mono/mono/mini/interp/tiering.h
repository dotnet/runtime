#ifndef __MONO_MINI_INTERP_TIERING_H__
#define __MONO_MINI_INTERP_TIERING_H__

#include "interp-internals.h"

#define INTERP_TIER_ENTRY_LIMIT 1000

void
mono_interp_tiering_init (void);

gboolean
mono_interp_tiering_enabled (void);

void
mono_interp_register_imethod_data_items (gpointer *data_items, GSList *indexes);

void
mono_interp_register_imethod_patch_site (gpointer *imethod_ptr);

const guint16*
mono_interp_tier_up_frame_enter (InterpFrame *frame, ThreadContext *context);

const guint16*
mono_interp_tier_up_frame_patchpoint (InterpFrame *frame, ThreadContext *context, int bb_index);

#endif /* __MONO_MINI_INTERP_TIERING_H__ */
