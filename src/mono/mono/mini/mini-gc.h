#ifndef __MONO_MINI_GC_H__
#define __MONO_MINI_GC_H__

#include "mini.h"

void mini_gc_init (void) MONO_INTERNAL;

void mini_gc_create_gc_map (MonoCompile *cfg) MONO_INTERNAL;

#endif
