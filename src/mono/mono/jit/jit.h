/*
 * Author:
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#ifndef _MONO_JIT_JIT_H_
#define _MONO_JIT_JIT_H_

#include <config.h>
/*
 * io-layer.h must be _BEFORE_ win32-exception.h to avoid problems when 
 * compiling with version 1.2 of mingw and w32api.
 */
#include <mono/io-layer/io-layer.h>

#ifdef PLATFORM_WIN32
#include "win32-exception.h"
#else
#include <signal.h>
#endif

#include <setjmp.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/object.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/mempool.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/opcodes.h>
#include <mono/utils/monobitset.h>

#include "regset.h"

/* fixme: configure should set that */
#define ARCH_X86 

#ifdef ARCH_X86
#define MB_TERM_LDIND_REF MB_TERM_LDIND_I4
#define MB_TERM_LDIND_U4 MB_TERM_LDIND_I4
#define MB_TERM_STIND_REF MB_TERM_STIND_I4
#define MB_TERM_REMOTE_STIND_REF MB_TERM_REMOTE_STIND_I4
#endif

#define ISSTRUCT(t) (!t->byref && t->type == MONO_TYPE_VALUETYPE && !t->data.klass->enumtype)

#define VARINFO(cfg,num) (g_array_index (cfg->varinfo, MonoVarInfo, num))

#define SET_VARINFO(vi,t,k,o,s) do { vi.type=t; vi.kind=k; vi.offset=o; vi.size=s; } while (0)


typedef struct _MBTree MBTree;

typedef enum {
	VAL_UNKNOWN,
	VAL_I32,
	VAL_I64,
	VAL_POINTER,
	VAL_DOUBLE, /* must be the last - do not reorder */
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

typedef union {
	struct {
		guint16 tid; /* tree number */
		guint16 bid; /* block number */
	} pos ;
	guint32 abs_pos; 
} MonoPosition;

typedef struct {
	MonoPosition first_use, last_use;
} MonoLiveRange;

typedef struct {
	MonoValueType type;
	MonoValueKind kind;
	int offset;
	int size;
	MonoLiveRange range;
	unsigned isvolatile:1;
	int reg;
	int varnum; /* only for debugging */
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
	guint16       num;

	MonoBitSet   *gen_set;
	MonoBitSet   *kill_set;
	MonoBitSet   *live_in_set;
	MonoBitSet   *live_out_set;
	
	GList        *succ;
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
	gint32            prologue_end;
	gint32            epilogue_end;
	MonoRegSet       *rs;
	guint32           epilog;
	guint32           args_start_index;
	guint32           locals_start_index;
	gint             *spillvars; 
	gint              spillcount;
	MonoJumpInfo     *jump_info;
} MonoFlowGraph;

typedef struct {
	gint16 args_size;
	gint16 vtype_num;
} MonoJitCallInfo;

typedef struct {
	MonoClass *klass;
	MonoClassField *field;
} MonoJitFieldInfo;

typedef struct {
	MonoBBlock *target;
	guint32 cond;
} MonoJitBranchInfo;

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
	gulong inlineable_methods;
	gulong inlined_methods;
	MonoMethod *max_ratio_method;
	MonoMethod *biggest_method;
	gboolean enabled;
} MonoJitStats;

typedef struct {
	gpointer          end_of_stack;
	jmp_buf          *env;
	MonoLMF          *lmf;
	void            (*abort_func) (MonoObject *object);
} MonoJitTlsData;

extern MonoJitStats mono_jit_stats;
extern gboolean mono_jit_dump_asm;
extern gboolean mono_jit_dump_forest;
extern gboolean mono_jit_trace_calls;
extern gboolean mono_jit_profile;
extern gboolean mono_jit_share_code;
extern gboolean mono_jit_inline_code;
extern gboolean mono_use_linear_scan;
extern gboolean mono_use_fast_iconv;
extern gboolean mono_break_on_exc;
extern gboolean mono_inline_memcpy;

extern guint32  mono_jit_tls_id;

extern CRITICAL_SECTION *metadata_section;

/* architecture independent functions */

MonoDomain * 
mono_jit_init              (char *file);

int
mono_jit_exec              (MonoDomain *domain, MonoAssembly *assembly, 
			    int argc, char *argv[]);
void        
mono_jit_cleanup           (MonoDomain *domain);

void
mono_jit_compile_image     (MonoImage *image, int verbose);

void
mono_jit_compile_class     (MonoAssembly *assembly, char *compile_class,
			    int compile_times, int verbose);
void
mono_add_jump_info         (MonoFlowGraph *cfg, gpointer ip, 
			    MonoJumpInfoType type, gpointer target);

gpointer 
mono_compile_method        (MonoMethod *method);

void
mono_cpu_detect            (void);

/* architecture dependent functions */

void
arch_handle_exception      (struct sigcontext *ctx, gpointer obj);

gpointer 
arch_get_throw_exception   (void);

gpointer 
arch_get_throw_exception_by_name (void);

MonoJitInfo *
arch_jit_compile_cfg       (MonoDomain *target_domain, MonoFlowGraph *cfg);

gpointer
arch_create_jit_trampoline (MonoMethod *method);

gpointer
arch_create_remoting_trampoline (MonoMethod *method);

MonoObject*
arch_runtime_invoke        (MonoMethod *method, void *obj, void **params, MonoObject **exc);

gpointer
arch_create_native_wrapper (MonoMethod *method);

int
arch_allocate_var          (MonoFlowGraph *cfg, int size, int align, 
			    MonoValueKind kind, MonoValueType type);

void
mono_linear_scan           (MonoFlowGraph *cfg, guint32 *used_mask);

gpointer 
arch_get_lmf_addr          (void);

/* delegate support functions */

gpointer
arch_get_delegate_begin_invoke (MonoMethod *method);

void
arch_end_invoke            (MonoMethod *method, gpointer first_arg, ...);

gpointer
arch_get_delegate_invoke   (MonoMethod *method);

/* remoting support */

gpointer
mono_load_remote_field     (MonoObject *this, MonoClass *klass, MonoClassField *field, gpointer *res);

void
mono_store_remote_field    (MonoObject *this, MonoClass *klass, MonoClassField *field, gpointer val);

/* stack/message transition */

MonoMethodMessage *
arch_method_call_message_new (MonoMethod *method, gpointer stack, MonoMethod *invoke, 
			      MonoDelegate **cb, MonoObject **state);

void
arch_method_return_message_restore (MonoMethod *method, gpointer stack, 
				    MonoObject *result, MonoArray *out_args);


#endif
