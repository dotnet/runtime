/**
 * \file
 */

#ifndef __MONO_TASKLETS_H__
#define __MONO_TASKLETS_H__

#include "mini.h"

typedef struct {
	MonoLMF *lmf;
	gpointer top_sp;
	MonoNativeThreadId thread_id;
	MonoDomain *domain;

	/* the instruction pointer and stack to return to on Restore */
	gpointer return_ip;
	gpointer return_sp;

	/* the saved stack information */
	int stack_alloc_size;
	int stack_used_size;
	/* pointer to GC memory */
	gpointer saved_stack;
} MonoContinuation;

typedef void (*MonoContinuationRestore) (MonoContinuation *cont, int state, MonoLMF **lmf_addr);

void  mono_tasklets_init    (void);
void  mono_tasklets_cleanup (void);

MonoContinuationRestore mono_tasklets_arch_restore (void);

#endif /* __MONO_TASKLETS_H__ */

