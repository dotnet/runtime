/**
 * \file
 * Copyright 2018 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_SGEN_MONO_H__
#define __MONO_SGEN_MONO_H__

#define MONO_SGEN_MONO_CALLBACKS_VERSION 1

typedef struct {
	int version;
	void (*emit_nursery_check) (MonoMethodBuilder *mb, gboolean is_concurrent);
	void (*emit_managed_allocator) (MonoMethodBuilder *mb, gboolean slowpath, gboolean profiler, int atype);
} MonoSgenMonoCallbacks;

void
mono_install_sgen_mono_callbacks (MonoSgenMonoCallbacks *cb);

#endif
