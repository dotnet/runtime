#ifndef _MONO_JIT_JIT_H_
#define _MONO_JIT_JIT_H_

#include <signal.h>

#include <mono/metadata/loader.h>
#include <mono/metadata/object.h>

#include "regset.h"
#include "mempool.h"

#define ISSTRUCT(t) (!t->byref && t->type == MONO_TYPE_VALUETYPE && !t->data.klass->enumtype)

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
	gpointer    previous_lmf;
	gpointer    lmf_addr;
	MonoMethod *method;
	guint32     eip;
	guint32     ebp;
	guint32     esi;
	guint32     edi;
	guint32     ebx;

} MonoLMF;

typedef struct {
	MonoValueType type;
	MonoValueKind kind;
	int offset;
	int size;
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
	MBTree      **outstack;
	gint32        outdepth;
	gint32        addr;
} MonoBBlock;

typedef struct {
	unsigned          has_vtarg:1;
	MonoMethod       *method;
	MonoBytecodeInfo *bcinfo;
	MonoBBlock       *bblocks;
	int               block_count;
	GArray           *varinfo;
	gint32            locals_size;
	gint32            args_size;
	guint16          *intvars;
	guint16           excvar;

	MonoMemPool      *mp;
	guint8           *start;
	guint8           *code;
	gint32            code_size;
	MonoRegSet       *rs;
	guint32           epilog;
	guint32           args_start_index;
	guint32           locals_start_index;
	gint              invalid;
} MonoFlowGraph;

typedef struct {
	MonoMethod *m;
	int args_size;
	int vtype_num;
} MethodCallInfo;

typedef struct {
	guint32  flags;
	gpointer try_start;
	gpointer try_end;
	gpointer handler_start;
	guint32  token_or_filter;
} MonoJitExceptionInfo;

typedef struct {
	MonoMethod *method;
	gpointer code_start;
	int      code_size;
	guint32  used_regs;
	unsigned num_clauses;
	MonoJitExceptionInfo *clauses;

} MonoJitInfo;

typedef GArray MonoJitInfoTable;

extern gboolean mono_jit_dump_asm;
extern gboolean mono_jit_dump_forest;
extern gboolean mono_jit_trace_calls;
extern MonoJitInfoTable *mono_jit_info_table;
extern gpointer mono_end_of_stack;

extern guint32  lmf_thread_id;

MonoJitInfoTable *
mono_jit_info_table_new    (void);

int
mono_jit_info_table_index  (MonoJitInfoTable *table, gpointer addr);

void
mono_jit_info_table_add    (MonoJitInfoTable *table, MonoJitInfo *ji);

MonoJitInfo *
mono_jit_info_table_find   (MonoJitInfoTable *table, gpointer addr);

void
arch_handle_exception      (struct sigcontext *ctx, gpointer obj);

gpointer 
arch_get_throw_exception   (void);

void
mono_jit_abort             (MonoObject *obj);

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
mono_disassemble_code      (guint8 *code, int size, char *id);

gpointer 
arch_compile_method        (MonoMethod *method);

gpointer
arch_create_jit_trampoline (MonoMethod *method);

gpointer
arch_create_simple_jit_trampoline (MonoMethod *method);

/* some handy debugging functions */

void
mono_print_ctree           (MBTree *tree);

void
mono_print_forest          (GPtrArray *forest);

#endif
