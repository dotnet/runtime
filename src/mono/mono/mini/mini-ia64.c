/*
 * mini-ia64.c: IA64 backend for the Mono code generator
 *
 * Authors:
 *   Zoltan Varga (vargaz@gmail.com)
 *
 * (C) 2003 Ximian, Inc.
 */
#include "mini.h"
#include <string.h>
#include <math.h>
#include <unistd.h>
#include <sys/mman.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/profiler-private.h>
#include <mono/utils/mono-math.h>

#include "trace.h"
#include "mini-ia64.h"
#include "inssel.h"
#include "cpu-ia64.h"

static gint lmf_tls_offset = -1;
static gint appdomain_tls_offset = -1;
static gint thread_tls_offset = -1;

const char * const ia64_desc [OP_LAST];
static const char*const * ins_spec = ia64_desc;

#define ALIGN_TO(val,align) ((((guint64)val) + ((align) - 1)) & ~((align) - 1))

#define IS_IMM32(val) ((((guint64)val) >> 32) == 0)

#define SIGNAL_STACK_SIZE (64 * 1024)

#define NOT_IMPLEMENTED g_assert_not_reached ()

const char*
mono_arch_regname (int reg) {
	g_assert_not_reached ();

	switch (reg) {
	}
	return "unknown";
}

const char*
mono_arch_fregname (int reg)
{
	g_assert_not_reached ();
}

static inline void 
ia64_patch (unsigned char* code, gpointer target)
{
	g_assert_not_reached ();
}

typedef enum {
	ArgInIReg,
	ArgInFloatSSEReg,
	ArgInDoubleSSEReg,
	ArgOnStack,
	ArgValuetypeInReg,
	ArgNone /* only in pair_storage */
} ArgStorage;

typedef struct {
	gint16 offset;
	gint8  reg;
	ArgStorage storage;

	/* Only if storage == ArgValuetypeInReg */
	ArgStorage pair_storage [2];
	gint8 pair_regs [2];
} ArgInfo;

typedef struct {
	int nargs;
	guint32 stack_usage;
	guint32 reg_usage;
	guint32 freg_usage;
	gboolean need_stack_align;
	ArgInfo ret;
	ArgInfo sig_cookie;
	ArgInfo args [1];
} CallInfo;

#define DEBUG(a) if (cfg->verbose_level > 1) a

#define NEW_ICONST(cfg,dest,val) do {	\
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->opcode = OP_ICONST;	\
		(dest)->inst_c0 = (val);	\
		(dest)->type = STACK_I4;	\
	} while (0)

/*
 * get_call_info:
 *
 *  Obtain information about a call according to the calling convention.
 */
static CallInfo*
get_call_info (MonoMethodSignature *sig, gboolean is_pinvoke)
{
	guint32 i, gr, fr;
	MonoType *ret_type;
	int n = sig->hasthis + sig->param_count;
	guint32 stack_size = 0;
	CallInfo *cinfo;

	cinfo = g_malloc0 (sizeof (CallInfo) + (sizeof (ArgInfo) * n));

	gr = 0;
	fr = 0;

	g_assert_not_reached ();
	return NULL;
}

/*
 * mono_arch_get_argument_info:
 * @csig:  a method signature
 * @param_count: the number of parameters to consider
 * @arg_info: an array to store the result infos
 *
 * Gathers information on parameters such as size, alignment and
 * padding. arg_info should be large enought to hold param_count + 1 entries. 
 *
 * Returns the size of the argument area on the stack.
 */
int
mono_arch_get_argument_info (MonoMethodSignature *csig, int param_count, MonoJitArgumentInfo *arg_info)
{
	g_assert_not_reached ();

	return 0;
}

/*
 * Initialize the cpu to execute managed code.
 */
void
mono_arch_cpu_init (void)
{
}

/*
 * This function returns the optimizations supported on this cpu.
 */
guint32
mono_arch_cpu_optimizazions (guint32 *exclude_mask)
{
	g_assert_not_reached ();

	return 0;
}

static gboolean
is_regsize_var (MonoType *t) {
	if (t->byref)
		return TRUE;
	t = mono_type_get_underlying_type (t);
	switch (t->type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
		return TRUE;
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_STRING:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:
		return TRUE;
	case MONO_TYPE_VALUETYPE:
		return FALSE;
	}
	return FALSE;
}

GList *
mono_arch_get_allocatable_int_vars (MonoCompile *cfg)
{
	GList *vars = NULL;
	int i;

	for (i = 0; i < cfg->num_varinfo; i++) {
		MonoInst *ins = cfg->varinfo [i];
		MonoMethodVar *vmv = MONO_VARINFO (cfg, i);

		/* unused vars */
		if (vmv->range.first_use.abs_pos >= vmv->range.last_use.abs_pos)
			continue;

		if ((ins->flags & (MONO_INST_IS_DEAD|MONO_INST_VOLATILE|MONO_INST_INDIRECT)) || 
		    (ins->opcode != OP_LOCAL && ins->opcode != OP_ARG))
			continue;

		if (is_regsize_var (ins->inst_vtype)) {
			g_assert (MONO_VARINFO (cfg, i)->reg == -1);
			g_assert (i == vmv->idx);
			vars = g_list_prepend (vars, vmv);
		}
	}

	vars = mono_varlist_sort (cfg, vars, 0);

	return vars;
}

GList *
mono_arch_get_global_int_regs (MonoCompile *cfg)
{
	GList *regs = NULL;

	g_assert_not_reached ();

	return regs;
}

/*
 * mono_arch_regalloc_cost:
 *
 *  Return the cost, in number of memory references, of the action of 
 * allocating the variable VMV into a register during global register
 * allocation.
 */
guint32
mono_arch_regalloc_cost (MonoCompile *cfg, MonoMethodVar *vmv)
{
	MonoInst *ins = cfg->varinfo [vmv->idx];

	g_assert_not_reached ();

	return 0;
}
 
void
mono_arch_allocate_vars (MonoCompile *m)
{
	g_assert_not_reached ();
}

void
mono_arch_create_vars (MonoCompile *cfg)
{
	g_assert_not_reached ();
}

/* 
 * take the arguments and generate the arch-specific
 * instructions to properly call the function in call.
 * This includes pushing, moving arguments to the right register
 * etc.
 * Issue: who does the spilling if needed, and when?
 */
MonoCallInst*
mono_arch_call_opcode (MonoCompile *cfg, MonoBasicBlock* bb, MonoCallInst *call, int is_virtual)
{
	g_assert_not_reached ();

	return NULL;
}

static void
peephole_pass (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins, *last_ins = NULL;
	ins = bb->code;

	g_assert_not_reached ();

	while (ins) {
		last_ins = ins;
		ins = ins->next;
	}
	bb->last_ins = last_ins;
}

static void
insert_after_ins (MonoBasicBlock *bb, MonoInst *ins, MonoInst *to_insert)
{
	if (ins == NULL) {
		ins = bb->code;
		bb->code = to_insert;
		to_insert->next = ins;
	}
	else {
		to_insert->next = ins->next;
		ins->next = to_insert;
	}
}

#define NEW_INS(cfg,dest,op) do {	\
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->opcode = (op);	\
        insert_after_ins (bb, last_ins, (dest)); \
	} while (0)

/*
 * mono_arch_lowering_pass:
 *
 *  Converts complex opcodes into simpler ones so that each IR instruction
 * corresponds to one machine instruction.
 */
static void
mono_arch_lowering_pass (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins, *temp, *last_ins = NULL;
	ins = bb->code;

	if (bb->max_ireg > cfg->rs->next_vireg)
		cfg->rs->next_vireg = bb->max_ireg;
	if (bb->max_freg > cfg->rs->next_vfreg)
		cfg->rs->next_vfreg = bb->max_freg;

	g_assert_not_reached ();

	bb->max_ireg = cfg->rs->next_vireg;
	bb->max_freg = cfg->rs->next_vfreg;
}

void
mono_arch_local_regalloc (MonoCompile *cfg, MonoBasicBlock *bb)
{
	if (!bb->code)
		return;

	mono_arch_lowering_pass (cfg, bb);

	mono_local_regalloc (cfg, bb);
}

static guint8*
emit_move_return_value (MonoCompile *cfg, MonoInst *ins, guint8 *code)
{
	CallInfo *cinfo;
	guint32 quad;

	g_assert_not_reached ();

	return code;
}

#define bb_is_loop_start(bb) ((bb)->loop_body_start && (bb)->nesting)

void
mono_arch_output_basic_block (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins;
	MonoCallInst *call;
	guint offset;
	guint8 *code = cfg->native_code + cfg->code_len;
	MonoInst *last_ins = NULL;
	guint last_offset = 0;
	int max_len, cpos;

	if (cfg->opt & MONO_OPT_PEEPHOLE)
		peephole_pass (cfg, bb);

	if (cfg->opt & MONO_OPT_LOOP) {
		/* FIXME: */
		g_assert_not_reached ();
	}

	if (cfg->verbose_level > 2)
		g_print ("Basic block %d starting at offset 0x%x\n", bb->block_num, bb->native_offset);

	cpos = bb->max_offset;

	if (cfg->prof_options & MONO_PROFILE_COVERAGE) {
		NOT_IMPLEMENTED;
	}

	offset = code - cfg->native_code;

	ins = bb->code;
	while (ins) {
		offset = code - cfg->native_code;

		max_len = ((guint8 *)ins_spec [ins->opcode])[MONO_INST_LEN];

		if (offset > (cfg->code_size - max_len - 16)) {
			cfg->code_size *= 2;
			cfg->native_code = g_realloc (cfg->native_code, cfg->code_size);
			code = cfg->native_code + offset;
			mono_jit_stats.code_reallocs++;
		}

		mono_debug_record_line_number (cfg, ins, offset);

		switch (ins->opcode) {
		default:
			g_warning ("unknown opcode %s in %s()\n", mono_inst_name (ins->opcode), __FUNCTION__);
			g_assert_not_reached ();
		}

		if ((code - cfg->native_code - offset) > max_len) {
			g_warning ("wrong maximal instruction length of instruction %s (expected %d, got %ld)",
				   mono_inst_name (ins->opcode), max_len, code - cfg->native_code - offset);
			g_assert_not_reached ();
		}
	       
		cpos += max_len;

		last_ins = ins;
		last_offset = offset;
		
		ins = ins->next;
	}

	cfg->code_len = code - cfg->native_code;
}

void
mono_arch_register_lowlevel_calls (void)
{
}

void
mono_arch_patch_code (MonoMethod *method, MonoDomain *domain, guint8 *code, MonoJumpInfo *ji, gboolean run_cctors)
{
	MonoJumpInfo *patch_info;

	for (patch_info = ji; patch_info; patch_info = patch_info->next) {
		unsigned char *ip = patch_info->ip.i + code;
		const unsigned char *target;

		target = mono_resolve_patch_target (method, domain, code, patch_info, run_cctors);

		if (mono_compile_aot) {
			NOT_IMPLEMENTED;
		}

		NOT_IMPLEMENTED;
	}
}

guint8 *
mono_arch_emit_prolog (MonoCompile *cfg)
{
	NOT_IMPLEMENTED;

	return NULL;
}

void
mono_arch_emit_epilog (MonoCompile *cfg)
{
	NOT_IMPLEMENTED;
}

void
mono_arch_emit_exceptions (MonoCompile *cfg)
{
	NOT_IMPLEMENTED;
}

void*
mono_arch_instrument_prolog (MonoCompile *cfg, void *func, void *p, gboolean enable_arguments)
{
	NOT_IMPLEMENTED;

	return NULL;
}

void*
mono_arch_instrument_epilog (MonoCompile *cfg, void *func, void *p, gboolean enable_arguments)
{
	NOT_IMPLEMENTED;

	return NULL;
}

void
mono_arch_flush_icache (guint8 *code, gint size)
{
	NOT_IMPLEMENTED;
}

void
mono_arch_flush_register_windows (void)
{
	NOT_IMPLEMENTED;
}

gboolean 
mono_arch_is_inst_imm (gint64 imm)
{
	NOT_IMPLEMENTED;

	return NULL;
}

/*
 * Determine whenever the trap whose info is in SIGINFO is caused by
 * integer overflow.
 */
gboolean
mono_arch_is_int_overflow (void *sigctx, void *info)
{
	NOT_IMPLEMENTED;

	return FALSE;
}

guint32
mono_arch_get_patch_offset (guint8 *code)
{
	NOT_IMPLEMENTED;

	return 0;
}

gpointer*
mono_arch_get_vcall_slot_addr (guint8* code, gpointer *regs)
{
	NOT_IMPLEMENTED;

	return NULL;
}

gpointer*
mono_arch_get_delegate_method_ptr_addr (guint8* code, gpointer *regs)
{
	NOT_IMPLEMENTED;

	return NULL;
}

static gboolean tls_offset_inited = FALSE;

/* code should be simply return <tls var>; */
static int 
read_tls_offset_from_method (void* method)
{
	NOT_IMPLEMENTED;

	return 0;
}

#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK

static void
setup_stack (MonoJitTlsData *tls)
{
	NOT_IMPLEMENTED;
}

#endif

void
mono_arch_setup_jit_tls_data (MonoJitTlsData *tls)
{
	if (!tls_offset_inited) {
		tls_offset_inited = TRUE;

		lmf_tls_offset = read_tls_offset_from_method (mono_get_lmf_addr);
		appdomain_tls_offset = read_tls_offset_from_method (mono_domain_get);
		thread_tls_offset = read_tls_offset_from_method (mono_thread_current);
	}		

#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK
	setup_stack (tls);
#endif
}

void
mono_arch_free_jit_tls_data (MonoJitTlsData *tls)
{
#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK
	struct sigaltstack sa;

	sa.ss_sp = tls->signal_stack;
	sa.ss_size = SIGNAL_STACK_SIZE;
	sa.ss_flags = SS_DISABLE;
	sigaltstack  (&sa, NULL);

	if (tls->signal_stack)
		munmap (tls->signal_stack, SIGNAL_STACK_SIZE);
#endif
}

void
mono_arch_emit_this_vret_args (MonoCompile *cfg, MonoCallInst *inst, int this_reg, int this_type, int vt_reg)
{
	NOT_IMPLEMENTED;
}

MonoInst*
mono_arch_get_inst_for_method (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	MonoInst *ins = NULL;

	if (cmethod->klass == mono_defaults.math_class) {
		if (strcmp (cmethod->name, "Sin") == 0) {
			MONO_INST_NEW (cfg, ins, OP_SIN);
			ins->inst_i0 = args [0];
		} else if (strcmp (cmethod->name, "Cos") == 0) {
			MONO_INST_NEW (cfg, ins, OP_COS);
			ins->inst_i0 = args [0];
		} else if (strcmp (cmethod->name, "Tan") == 0) {
				return ins;
			MONO_INST_NEW (cfg, ins, OP_TAN);
			ins->inst_i0 = args [0];
		} else if (strcmp (cmethod->name, "Atan") == 0) {
				return ins;
			MONO_INST_NEW (cfg, ins, OP_ATAN);
			ins->inst_i0 = args [0];
		} else if (strcmp (cmethod->name, "Sqrt") == 0) {
			MONO_INST_NEW (cfg, ins, OP_SQRT);
			ins->inst_i0 = args [0];
		} else if (strcmp (cmethod->name, "Abs") == 0 && fsig->params [0]->type == MONO_TYPE_R8) {
			MONO_INST_NEW (cfg, ins, OP_ABS);
			ins->inst_i0 = args [0];
		}
#if 0
		/* OP_FREM is not IEEE compatible */
		else if (strcmp (cmethod->name, "IEEERemainder") == 0) {
			MONO_INST_NEW (cfg, ins, OP_FREM);
			ins->inst_i0 = args [0];
			ins->inst_i1 = args [1];
		}
#endif
	} else if(cmethod->klass->image == mono_defaults.corlib &&
			   (strcmp (cmethod->klass->name_space, "System.Threading") == 0) &&
			   (strcmp (cmethod->klass->name, "Interlocked") == 0)) {

		if (strcmp (cmethod->name, "Increment") == 0) {
			MonoInst *ins_iconst;
			guint32 opcode;

			if (fsig->params [0]->type == MONO_TYPE_I4)
				opcode = OP_ATOMIC_ADD_NEW_I4;
			else if (fsig->params [0]->type == MONO_TYPE_I8)
				opcode = OP_ATOMIC_ADD_NEW_I8;
			else
				g_assert_not_reached ();
			MONO_INST_NEW (cfg, ins, opcode);
			MONO_INST_NEW (cfg, ins_iconst, OP_ICONST);
			ins_iconst->inst_c0 = 1;

			ins->inst_i0 = args [0];
			ins->inst_i1 = ins_iconst;
		} else if (strcmp (cmethod->name, "Decrement") == 0) {
			MonoInst *ins_iconst;
			guint32 opcode;

			if (fsig->params [0]->type == MONO_TYPE_I4)
				opcode = OP_ATOMIC_ADD_NEW_I4;
			else if (fsig->params [0]->type == MONO_TYPE_I8)
				opcode = OP_ATOMIC_ADD_NEW_I8;
			else
				g_assert_not_reached ();
			MONO_INST_NEW (cfg, ins, opcode);
			MONO_INST_NEW (cfg, ins_iconst, OP_ICONST);
			ins_iconst->inst_c0 = -1;

			ins->inst_i0 = args [0];
			ins->inst_i1 = ins_iconst;
		} else if (strcmp (cmethod->name, "Add") == 0) {
			guint32 opcode;

			if (fsig->params [0]->type == MONO_TYPE_I4)
				opcode = OP_ATOMIC_ADD_I4;
			else if (fsig->params [0]->type == MONO_TYPE_I8)
				opcode = OP_ATOMIC_ADD_I8;
			else
				g_assert_not_reached ();
			
			MONO_INST_NEW (cfg, ins, opcode);

			ins->inst_i0 = args [0];
			ins->inst_i1 = args [1];
		} else if (strcmp (cmethod->name, "Exchange") == 0) {
			guint32 opcode;

			if (fsig->params [0]->type == MONO_TYPE_I4)
				opcode = OP_ATOMIC_EXCHANGE_I4;
			else if ((fsig->params [0]->type == MONO_TYPE_I8) ||
					 (fsig->params [0]->type == MONO_TYPE_I) ||
					 (fsig->params [0]->type == MONO_TYPE_OBJECT))
				opcode = OP_ATOMIC_EXCHANGE_I8;
			else
				return NULL;

			MONO_INST_NEW (cfg, ins, opcode);

			ins->inst_i0 = args [0];
			ins->inst_i1 = args [1];
		} else if (strcmp (cmethod->name, "Read") == 0 && (fsig->params [0]->type == MONO_TYPE_I8)) {
			/* 64 bit reads are already atomic */
			MONO_INST_NEW (cfg, ins, CEE_LDIND_I8);
			ins->inst_i0 = args [0];
		}

		/* 
		 * Can't implement CompareExchange methods this way since they have
		 * three arguments.
		 */
	}

	return ins;
}

gboolean
mono_arch_print_tree (MonoInst *tree, int arity)
{
	return 0;
}

MonoInst* mono_arch_get_domain_intrinsic (MonoCompile* cfg)
{
	MonoInst* ins;
	
	if (appdomain_tls_offset == -1)
		return NULL;
	
	MONO_INST_NEW (cfg, ins, OP_TLS_GET);
	ins->inst_offset = appdomain_tls_offset;
	return ins;
}

MonoInst* mono_arch_get_thread_intrinsic (MonoCompile* cfg)
{
	MonoInst* ins;
	
	if (thread_tls_offset == -1)
		return NULL;
	
	MONO_INST_NEW (cfg, ins, OP_TLS_GET);
	ins->inst_offset = thread_tls_offset;
	return ins;
}
