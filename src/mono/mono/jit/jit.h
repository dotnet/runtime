#ifndef _MONO_JIT_JIT_H_
#define _MONO_JIT_JIT_H_

#include <mono/metadata/loader.h>

#include "regset.h"
#include "mempool.h"

typedef struct _MBTree MBTree;

typedef enum {
	VAL_UNKNOWN,
	VAL_I32,
	VAL_I64,
	VAL_POINTER,
	VAL_DOUBLE, // must be the last - do not reorder
} MonoValueType;

typedef enum {
	MONO_ARGVAR,
	MONO_LOCALVAR,
	MONO_TEMPVAR,
} MonoValueKind;

typedef struct {
	MonoValueType type;
	MonoValueKind kind;
	int offset;
} MonoVarInfo;

typedef struct {
	unsigned block_id:15;
	unsigned is_block_start:1;
} MonoBytecodeInfo;

typedef struct {
	unsigned reached:1;
	unsigned finished:1;

	gint32        cli_addr;  /* start instruction */
	gint32        length;    /* length of stream */
	GPtrArray    *forest;
	MBTree      **instack;
	gint32        indepth;
	gint32        addr;
} MonoBBlock;

typedef struct {
	MonoMethod       *method;
	MonoBytecodeInfo *bcinfo;
	MonoBBlock       *bblocks;
	int               block_count;
	GArray           *varinfo;
	gint32            locals_size;
	gint32            args_size;
	guint16         **intvars;

	MonoMemPool      *mp;
	guint8           *start;
	guint8           *code;
	MonoRegSet       *rs;
	guint32           epilog;
} MonoFlowGraph;

extern gboolean mono_jit_dump_asm;
extern gboolean mono_jit_dump_forest;

MonoFlowGraph *
mono_cfg_new               (MonoMethod *method, MonoMemPool *mp);

void
mono_cfg_free              (MonoFlowGraph *cfg);

MBTree *
mono_ctree_new             (MonoMemPool *mp, int op, MBTree *left, 
			    MBTree *right);
MBTree *
mono_ctree_new_leaf        (MonoMemPool *mp, int op);

void
mono_analyze_flow          (MonoFlowGraph *cfg);

void
mono_analyze_stack         (MonoFlowGraph *cfg);

void
mono_disassemble_code      (guint8 *code, int size);

gpointer 
arch_compile_method        (MonoMethod *method);

gpointer
arch_create_jit_trampoline (MonoMethod *method);

/* some handy debugging functions */

void
mono_print_ctree           (MBTree *tree);

void
mono_print_forest          (GPtrArray *forest);


#endif
