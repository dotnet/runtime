#ifndef _MONO_JIT_JIT_H_
#define _MONO_JIT_JIT_H_

#include "regset.h"

typedef struct _MonoMemPool MonoMemPool;

typedef struct {
	MonoMemPool *mp;
	guint8 *start;
	guint8 *code;
	gint32 locals_size;
	GPtrArray *forest;
	MonoRegSet *rs;
	guint32 epilog;
} MBCodeGenStatus;

MonoMemPool *
mono_mempool_new      (void);

void
mono_mempool_destroy  (MonoMemPool *pool);

gpointer
mono_mempool_alloc    (MonoMemPool *pool, guint size);

gpointer
mono_mempool_alloc0   (MonoMemPool *pool, guint size);

void 
arch_emit_prologue    (MBCodeGenStatus *s);

void 
arch_emit_epilogue    (MBCodeGenStatus *s);

#endif
