#ifndef _MONO_JIT_JIT_H_
#define _MONO_JIT_JIT_H_

#include "codegen.h"
#include "regset.h"
#include "mempool.h"

extern gboolean mono_jit_dump_asm;
extern gboolean mono_jit_dump_forest;

MBTree *
mono_ctree_new             (MonoMemPool *mp, int op, MBTree *left, 
			    MBTree *right);

MBTree *
mono_ctree_new_leaf        (MonoMemPool *mp, int op);

GPtrArray *
mono_create_forest         (MonoMethod *method, MonoMemPool *mp, 
			    guint *locals_size);
void
mono_disassemble_code      (guint8 *code, int size);

void 
arch_emit_prologue         (MBCodeGenStatus *s);

void 
arch_emit_epilogue         (MBCodeGenStatus *s);

gpointer 
arch_compile_method        (MonoMethod *method);

gpointer
arch_create_jit_trampoline (MonoMethod *method);

gpointer
arch_compile_method        (MonoMethod *method);


/* some handy debugging functions */

void
mono_print_ctree           (MBTree *tree);

void
mono_print_forest          (GPtrArray *forest);


#endif
