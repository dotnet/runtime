#ifndef _MONO_JIT_JIT_H_
#define _MONO_JIT_JIT_H_

#include <signal.h>

#include <mono/metadata/loader.h>
#include <mono/metadata/object.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/mempool.h>
#include <mono/metadata/reflection.h>
#include <mono/io-layer/critical-sections.h>

#include "regset.h"

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
	guint32     ebp;
	guint32     esi;
	guint32     edi;
	guint32     ebx;
	guint32     eip;
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

typedef enum {
	MONO_JUMP_INFO_BB,
	MONO_JUMP_INFO_ABS,
	MONO_JUMP_INFO_EPILOG,
	MONO_JUMP_INFO_IP,
} MonoJumpInfoType;

typedef struct _MonoJumpInfo MonoJumpInfo;
struct _MonoJumpInfo {
	MonoJumpInfo *next;
	gpointer      ip;
	MonoJumpInfoType type;
	union {
		gpointer      target;
		MonoBBlock   *bb;
	} data;
};

typedef struct {
	MonoDomain       *domain;
	unsigned          has_vtarg:1;
	unsigned          share_code:1;
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

	MonoJumpInfo     *jump_info;
} MonoFlowGraph;

typedef struct {
	MonoMethod *m;
	int args_size;
	int vtype_num;
} MethodCallInfo;

typedef struct {
	gulong methods_compiled;
	gulong methods_lookups;
	gulong method_trampolines;
	gulong allocate_var;
	gulong analyze_stack_repeat;
	gulong cil_code_size;
	gulong native_code_size;
	gulong code_reallocs;
	gulong max_code_size_ratio;
	gulong biggest_method_size;
	gulong allocated_code_size;
	MonoMethod *max_ratio_method;
	MonoMethod *biggest_method;
	gboolean enabled;
} MonoJitStats;

extern MonoJitStats mono_jit_stats;
extern gboolean mono_jit_dump_asm;
extern gboolean mono_jit_dump_forest;
extern gboolean mono_jit_trace_calls;
extern gboolean mono_jit_share_code;
extern gpointer mono_end_of_stack;
extern guint32  lmf_thread_id;

extern CRITICAL_SECTION *metadata_section;

void
arch_handle_exception      (struct sigcontext *ctx, gpointer obj);

gpointer 
arch_get_throw_exception   (void);

gpointer 
arch_get_throw_exception_by_name (void);

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
mono_add_jump_info         (MonoFlowGraph *cfg, gpointer ip, 
			    MonoJumpInfoType type, gpointer target);

void
mono_disassemble_code      (guint8 *code, int size, char *id);

gpointer 
arch_compile_method        (MonoMethod *method);

gpointer
arch_create_jit_trampoline (MonoMethod *method);

MonoObject*
arch_runtime_invoke        (MonoMethod *method, void *obj, void **params);

gpointer
arch_create_native_wrapper (MonoMethod *method);

/* delegate support functions */

void
mono_delegate_init         (void);

void
mono_delegate_cleanup      (void);

void
mono_delegate_ctor         (MonoDelegate *this, MonoObject *target, gpointer addr);

gpointer 
arch_begin_invoke          (MonoMethod *method, gpointer ret_ip, MonoObject *this, ...);

void
arch_end_invoke            (MonoObject *this, gpointer handle, ...);

gpointer
arch_get_delegate_invoke   (MonoMethod *method, int *size);

/* some handy debugging functions */

void
mono_print_ctree           (MBTree *tree);

void
mono_print_forest          (GPtrArray *forest);

#endif
