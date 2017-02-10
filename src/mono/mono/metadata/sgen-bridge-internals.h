/**
 * \file
 * The cross-GC bridge.
 *
 * Copyright (C) 2015 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef __MONO_SGENBRIDGEINTERNAL_H__
#define __MONO_SGENBRIDGEINTERNAL_H__

#include "config.h"

#ifdef HAVE_SGEN_GC

#include "mono/utils/mono-compiler.h"

#include "mono/sgen/sgen-gc.h"
#include "mono/metadata/sgen-bridge.h"

extern volatile gboolean bridge_processing_in_progress;
extern MonoGCBridgeCallbacks bridge_callbacks;

gboolean sgen_need_bridge_processing (void);
void sgen_bridge_reset_data (void);
void sgen_bridge_processing_stw_step (void);
void sgen_bridge_processing_finish (int generation);
gboolean sgen_is_bridge_object (GCObject *obj);
MonoGCBridgeObjectKind sgen_bridge_class_kind (MonoClass *klass);
void sgen_bridge_register_finalized_object (GCObject *object);
void sgen_bridge_describe_pointer (GCObject *object);

gboolean sgen_is_bridge_object (GCObject *obj);
void sgen_mark_bridge_object (GCObject *obj);

gboolean sgen_bridge_handle_gc_param (const char *opt);
gboolean sgen_bridge_handle_gc_debug (const char *opt);
void sgen_bridge_print_gc_debug_usage (void);

typedef struct {
	char *dump_prefix;
	gboolean accounting;
	gboolean scc_precise_merge; // Used by Tarjan
} SgenBridgeProcessorConfig;

typedef struct {
	void (*reset_data) (void);
	void (*processing_stw_step) (void);
	void (*processing_build_callback_data) (int generation);
	void (*processing_after_callback) (int generation);
	MonoGCBridgeObjectKind (*class_kind) (MonoClass *klass);
	void (*register_finalized_object) (GCObject *object);
	void (*describe_pointer) (GCObject *object);

	/* Should be called once, immediately after init */
	void (*set_config) (const SgenBridgeProcessorConfig *);

	/*
	 * These are set by processing_build_callback_data().
	 */
	int num_sccs;
	MonoGCBridgeSCC **api_sccs;

	int num_xrefs;
	MonoGCBridgeXRef *api_xrefs;
} SgenBridgeProcessor;

void sgen_old_bridge_init (SgenBridgeProcessor *collector);
void sgen_new_bridge_init (SgenBridgeProcessor *collector);
void sgen_tarjan_bridge_init (SgenBridgeProcessor *collector);
void sgen_set_bridge_implementation (const char *name);
void sgen_bridge_set_dump_prefix (const char *prefix);
void sgen_init_bridge (void);

#endif

#endif
