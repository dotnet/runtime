/*
 * jit.c: The mono JIT compiler.
 *
 * Author:
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <mono/os/gc_wrapper.h>
#include <glib.h>
#include <stdlib.h>
#include <stdarg.h>
#include <string.h>
#include <unistd.h>

#include <mono/metadata/verify.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/class.h>
#include <mono/metadata/object.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/opcodes.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/socket-io.h>
#include <mono/metadata/appdomain.h>
#include <mono/arch/x86/x86-codegen.h>
#include <mono/io-layer/io-layer.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/marshal.h>

#include "jit.h"
#include "helpers.h"
#include "regset.h"
#include "codegen.h"
#include "debug.h"

/* 
 * if OPT_BOOL is defined we use 32bit to store boolean local variables.  This
 * gives great speedup for boolean expressions, but unfortunately it changes
 * semantics, so i disable it until we have a real solution  */
/* #define OPT_BOOL */

#define MAKE_CJUMP(name)                                                      \
case CEE_##name:                                                              \
case CEE_##name##_S: {                                                        \
        gint32 target;                                                        \
	int near_jump = *ip == CEE_##name##_S;                                \
	++ip;                                                                 \
	sp -= 2;                                                              \
        t1 = mono_ctree_new (mp, MB_TERM_COMPARE, sp [0], sp [1]);            \
	t1 = mono_ctree_new (mp, MB_TERM_CBRANCH, t1, NULL);                  \
	if (near_jump)                                                        \
		target = cli_addr + 2 + (signed char) *ip;                    \
	else                                                                  \
		target = cli_addr + 5 + (gint32) read32 (ip);                 \
	g_assert (target >= 0 && target <= header->code_size);                \
	g_assert (bcinfo [target].is_block_start);                            \
	tbb = &cfg->bblocks [bcinfo [target].block_id];                       \
	create_outstack (cfg, bb, stack, sp - stack);                         \
	mark_reached (cfg, tbb, bb->outstack, bb->outdepth);                  \
	t1->data.bi.target = tbb;                                             \
	t1->data.bi.cond = CEE_##name;                                        \
	ADD_TREE (t1, cli_addr);                                              \
	ip += near_jump ? 1: 4;		                                      \
	break;                                                                \
}

#define MAKE_BI_ALU(name)                                                     \
case CEE_##name: {                                                            \
	++ip;                                                                 \
	sp -= 2;                                                              \
	t1 = mono_ctree_new (mp, MB_TERM_##name, sp [0], sp [1]);             \
	PUSH_TREE (t1, sp [0]->svt);                                          \
	break;                                                                \
}

#ifndef ARCH_X86
#define MAKE_SPILLED_BI_ALU1(name, s1, s2) {                                  \
	t1 = mono_ctree_new (mp, MB_TERM_##name, s1, s2);                     \
	PUSH_TREE (t1, s1->svt); }                                            
#else
#define MAKE_SPILLED_BI_ALU1(name, s1, s2)                                    \
	t1 = mono_ctree_new (mp, MB_TERM_##name, s1, s2);                     \
        t1->svt = s1->svt;                                                    \
        t1 = mono_store_tree (cfg, -1, t1, &t2);                              \
        g_assert (t1);                                                        \
        ADD_TREE (t1, cli_addr);                                              \
	PUSH_TREE (t2, t2->svt);                                              
#endif

#define MAKE_CMP(cname)                                                       \
case CEE_##cname: {                                                           \
	++ip;                                                                 \
	sp -= 2;                                                              \
	t1 = mono_ctree_new (mp, MB_TERM_COMPARE, sp [0], sp [1]);            \
	t1 = mono_ctree_new (mp, MB_TERM_CSET, t1, NULL);                     \
        t1->data.i = CEE_##cname;                                             \
	PUSH_TREE (t1, VAL_I32);                                              \
	break;                                                                \
}

#define MAKE_SPILLED_BI_ALU(name)                                             \
case CEE_##name: {                                                            \
	++ip;                                                                 \
	sp -= 2;                                                              \
	MAKE_SPILLED_BI_ALU1 (name, sp [0], sp [1])                           \
	break;                                                                \
}

#define MAKE_LDIND(name, op, svt)                                             \
case CEE_##name: {                                                            \
	++ip;                                                                 \
	sp--;                                                                 \
	t1 = mono_ctree_new (mp, op, *sp, NULL);                              \
	PUSH_TREE (t1, svt);                                                  \
	break;                                                                \
}
	
#define MAKE_LDELEM(name, op, svt, s)                                         \
case CEE_##name: {                                                            \
	++ip;                                                                 \
	sp -= 2;                                                              \
        t1 = mono_ctree_new (mp, MB_TERM_LDELEMA, sp [0], sp [1]);            \
        t1->data.i = s;                                                       \
        t1 = mono_ctree_new (mp, op, t1, NULL);                               \
	PUSH_TREE (t1, svt);                                                  \
	break;                                                                \
}

#define MAKE_LDELEM_OLD(name, op, svt, s)                                     \
case CEE_##name: {                                                            \
	++ip;                                                                 \
	sp -= 2;                                                              \
        t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);                      \
        t1->data.i = s;                                                       \
        t1 = mono_ctree_new (mp, MB_TERM_MUL, sp [1], t1);                    \
        t2 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);                      \
        t2->data.i = G_STRUCT_OFFSET (MonoArray, vector);                     \
        t2 = mono_ctree_new (mp, MB_TERM_ADD, sp [0], t2);                    \
	t1 = mono_ctree_new (mp, MB_TERM_ADD, t1, t2);                        \
	t1 = mono_ctree_new (mp, op, t1, NULL);                               \
	PUSH_TREE (t1, svt);                                                  \
	break;                                                                \
}
	
#define MAKE_STIND(name, op)                                                  \
case CEE_##name: {                                                            \
	++ip;                                                                 \
	sp -= 2;                                                              \
	t1 = mono_ctree_new (mp, op, sp [0], sp [1]);                         \
	ADD_TREE (t1, cli_addr);                                              \
	break;                                                                \
}

#define MAKE_STELEM(name, op, s)                                              \
case CEE_##name: {                                                            \
	++ip;                                                                 \
	sp -= 3;                                                              \
        t1 = mono_ctree_new (mp, MB_TERM_LDELEMA, sp [0], sp [1]);            \
        t1->data.i = s;                                                       \
	t1 = mono_ctree_new (mp, op, t1, sp [2]);                             \
	ADD_TREE (t1, cli_addr);                                              \
	break;                                                                \
}
	
#define MAKE_STELEM_OLD(name, op, s)                                          \
case CEE_##name: {                                                            \
	++ip;                                                                 \
	sp -= 3;                                                              \
        t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);                      \
        t1->data.i = s;                                                       \
        t1 = mono_ctree_new (mp, MB_TERM_MUL, sp [1], t1);                    \
        t2 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);                      \
        t2->data.i = G_STRUCT_OFFSET (MonoArray, vector);                     \
        t2 = mono_ctree_new (mp, MB_TERM_ADD, sp [0], t2);                    \
	t1 = mono_ctree_new (mp, MB_TERM_ADD, t1, t2);                        \
	t1 = mono_ctree_new (mp, op, t1, sp [2]);                             \
	ADD_TREE (t1, cli_addr);                                              \
	break;                                                                \
}

typedef struct {
	MonoMethod *method;
	MBTree **arg_map;
	const unsigned char *end, *saved_ip;
	MonoImage *saved_image;
} MonoInlineInfo;
	
/* Whether to dump the assembly code after genreating it */
gboolean mono_jit_dump_asm = FALSE;

/* Whether to dump the forest */
gboolean mono_jit_dump_forest = FALSE;

/* Whether to print function call traces */
gboolean mono_jit_trace_calls = FALSE;

/* Whether to insert in the code profile callbacks */
gboolean mono_jit_profile = FALSE;

/* Force jit to share code between application domains */
gboolean mono_jit_share_code = FALSE;

/* use linear scan register allocation */
gboolean mono_use_linear_scan = TRUE;

/* inline code */
gboolean mono_jit_inline_code = TRUE;

/* generate bound checking */
gboolean mono_jit_boundcheck = TRUE;

/* inline memcpy */
gboolean mono_inline_memcpy = TRUE;

/* Use alternative (faster) sequence to convert FP values to integers */
gboolean mono_use_fast_iconv = FALSE;

/* TLS id to store jit data */
guint32  mono_jit_tls_id;

/* issue a breakpoint on unhandled excepions */
gboolean mono_break_on_exc = FALSE;

MonoDebugFormat mono_debug_format = MONO_DEBUG_FORMAT_NONE;

MonoJitStats mono_jit_stats;

CRITICAL_SECTION *metadata_section = NULL;

/* 
 * We sometimes need static data, for example the forest generator need it to
 * store constants or class data.
 */
inline static gpointer
mono_alloc_static (int size)
{
	return g_malloc (size);
} 
inline static gpointer
mono_alloc_static0 (int size)
{
	return g_malloc0 (size);
} 

typedef void (*MonoCCtor) (void);

static int
mono_allocate_intvar (MonoFlowGraph *cfg, int slot, MonoValueType type)
{
	int size, align, vnum, pos;
	
	g_assert (type != VAL_UNKNOWN);

	/* take care if you modify MonoValueType */
	g_assert (VAL_DOUBLE == 4);

	/* fixme: machine dependant */
	if (type == VAL_POINTER)
		type = VAL_I32; /* VAL_I32 and VAL_POINTER share the same slot */

	pos = type - 1 + slot * VAL_DOUBLE;

	if ((vnum = cfg->intvars [pos])) 		
		return vnum;
	mono_get_val_sizes (type, &size, &align);

	cfg->intvars[pos] = arch_allocate_var (cfg, size, align, MONO_TEMPVAR, type);

	return cfg->intvars[pos];
}

static int
mono_allocate_excvar (MonoFlowGraph *cfg)
{
	if (cfg->excvar)
		return cfg->excvar;

	cfg->excvar = arch_allocate_var (cfg, 4, 4, MONO_TEMPVAR, VAL_POINTER);

	VARINFO (cfg, cfg->excvar).isvolatile = 1;
	
	return cfg->excvar;
}

/**
 * mono_jit_runtime_invoke:
 * @method: the method to invoke
 * @obj: this pointer
 * @params: array of parameter values.
 * @exc: used to catch exceptions objects
 */
static MonoObject*
mono_jit_runtime_invoke (MonoMethod *method, void *obj, void **params, MonoObject **exc)
{
	MonoMethod *invoke;
	MonoObject *(*runtime_invoke) (MonoObject *this, void **params, MonoObject **exc);

	invoke = mono_marshal_get_runtime_invoke (method);
	runtime_invoke = mono_compile_method (invoke);
	return runtime_invoke (obj, params, exc);
}

/**
 * ctree_create_load:
 * @cfg: pointer to the control flow graph
 * @type: the type of the value to load
 * @addr: the address of the value
 *
 * Creates a tree to load the value at address @addr.
 */
inline static MBTree *
ctree_create_load (MonoFlowGraph *cfg, MonoType *type, MBTree *addr, MonoValueType *svt, gboolean arg)
{
	MonoMemPool *mp = cfg->mp;
	int ldind;
	MBTree *t;

	if (arg)
		ldind = mono_map_ldarg_type (type, svt);
	else
		ldind = mono_map_ldind_type (type, svt);

	t = mono_ctree_new (mp, ldind, addr, NULL);

	return t;
}

/**
 * ctree_create_store:
 * @mp: pointer to a memory pool
 * @s: the value (tree) to store
 * @type: the type of the value
 * @addr: the address of the value
 *
 * Creates a tree to store the value @s at address @addr.
 */
inline static MBTree *
ctree_create_store (MonoFlowGraph *cfg, MonoType *type, MBTree *addr, 
		    MBTree *s, gboolean arg)
{
	MonoMemPool *mp = cfg->mp;
	int stind; 
	MBTree *t;
	
	if (arg)
		stind = mono_map_starg_type (type);
	else
		stind = mono_map_stind_type (type);

	t = mono_ctree_new (mp, stind, addr, s);

	if (MONO_TYPE_ISSTRUCT (type))
		t->data.i = mono_class_value_size (type->data.klass, NULL);
	
	return t;
}

inline static MBTree *
ctree_dup_address (MonoMemPool *mp, MBTree *s)
{
	MBTree *t;

	switch (s->op) {

	case MB_TERM_ADDR_L:
	case MB_TERM_ADDR_G:
		t = mono_ctree_new_leaf (mp, s->op);
		t->data.i = s->data.i;
		t->svt = VAL_POINTER;
		return t;
	default:
		g_warning ("unknown tree opcode %d", s->op);
		g_assert_not_reached ();
	}

	return NULL;
}

/**
 * Create a duplicate of the value of a tree. This is
 * easy for trees starting with LDIND/STIND, since the
 * duplicate is simple a LDIND tree with the same address.
 * For other trees we have to split the tree into one tree
 * storing the value to a new temporary variable, and 
 * another tree which loads that value back. We can then
 * duplicate the second tree.
 */
static MBTree *
ctree_create_dup (MonoMemPool *mp, MBTree *s)
{
	MBTree *t;
	
	switch (s->op) {
	case MB_TERM_STIND_I1:
	case MB_TERM_LDIND_I1:
		t = ctree_dup_address (mp, s->left);
		t = mono_ctree_new (mp, MB_TERM_LDIND_I1, t, NULL);
		t->svt = VAL_I32;
		break;
	case MB_TERM_STIND_I2:
	case MB_TERM_LDIND_I2:
		t = ctree_dup_address (mp, s->left);
		t = mono_ctree_new (mp, MB_TERM_LDIND_I2, t, NULL);
		t->svt = VAL_I32;
		break;
	case MB_TERM_STIND_I4:
	case MB_TERM_LDIND_I4:
		t = ctree_dup_address (mp, s->left);
		t = mono_ctree_new (mp, MB_TERM_LDIND_I4, t, NULL);
		t->svt = VAL_I32;
		break;
	case MB_TERM_STIND_I8:
	case MB_TERM_LDIND_I8:
		t = ctree_dup_address (mp, s->left);
		t = mono_ctree_new (mp, MB_TERM_LDIND_I8, t, NULL);
		t->svt = VAL_I64;
		break;
	case MB_TERM_STIND_R4:
	case MB_TERM_LDIND_R4:
		t = ctree_dup_address (mp, s->left);
		t = mono_ctree_new (mp, MB_TERM_LDIND_R4, t, NULL);
		t->svt = VAL_DOUBLE;
		break;
	case MB_TERM_STIND_R8:
	case MB_TERM_LDIND_R8:
		t = ctree_dup_address (mp, s->left);
		t = mono_ctree_new (mp, MB_TERM_LDIND_R8, t, NULL);
		t->svt = VAL_DOUBLE;
		break;
	case MB_TERM_STIND_OBJ:
	case MB_TERM_LDIND_OBJ:
		t = ctree_dup_address (mp, s->left);
		t = mono_ctree_new (mp, MB_TERM_LDIND_OBJ, t, NULL);
		t->svt = VAL_UNKNOWN;
		break;
	default:
		g_warning ("unknown op \"%s\"", mono_burg_term_string [s->op]);
		g_assert_not_reached ();
	}

	return t;
}

static MBTree *
mono_store_tree (MonoFlowGraph *cfg, int slot, MBTree *s, MBTree **tdup)
{
	MonoMemPool *mp = cfg->mp;
	MBTree *t;
	int vnum = 0;

	switch (s->op) {
	case MB_TERM_STIND_I1:
	case MB_TERM_LDIND_I1:
	case MB_TERM_STIND_I2:
	case MB_TERM_LDIND_I2:
	case MB_TERM_STIND_I4:
	case MB_TERM_LDIND_I4:
	case MB_TERM_STIND_I8:
	case MB_TERM_LDIND_I8:
	case MB_TERM_STIND_R4:
	case MB_TERM_LDIND_R4:
	case MB_TERM_STIND_R8:
	case MB_TERM_LDIND_R8: {
		if (slot >= 0) {
			vnum = mono_allocate_intvar (cfg, slot, s->svt);

			if (s->left->op == MB_TERM_ADDR_L && s->left->data.i == vnum) {
				if (tdup)
					*tdup = ctree_create_dup (mp, s);
				return NULL;
			}
			// fall through
		} else {
			if (tdup)
				*tdup = ctree_create_dup (mp, s);
			return NULL;
		}
	}	
	default: {
			g_assert (s->svt != VAL_UNKNOWN);

			if (slot >= 0) {
				if (!vnum)
					vnum = mono_allocate_intvar (cfg, slot, s->svt);
			} else {
				int size, align;
				mono_get_val_sizes (s->svt, &size, &align);
				vnum = arch_allocate_var (cfg, size, align, MONO_TEMPVAR, s->svt);
			}

			t = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
			t->data.i = vnum;
		       
			t = mono_ctree_new (mp, mono_map_store_svt_type (s->svt), t, s);
			t->svt = s->svt;
		}
	}

	if (tdup) 
		*tdup = ctree_create_dup (mp, t);

	return t;
}

static MonoBBlock *
mono_find_final_block (MonoFlowGraph *cfg, guint32 ip, guint32 target, int type)
{
	MonoMethod *method = cfg->method;
	MonoBytecodeInfo *bcinfo = cfg->bcinfo;
	MonoMethodHeader *header = ((MonoMethodNormal *)method)->header;
	MonoExceptionClause *clause;
	int i;

	for (i = 0; i < header->num_clauses; ++i) {
		clause = &header->clauses [i];

		if (MONO_OFFSET_IN_CLAUSE (clause, ip) && 
		    (!MONO_OFFSET_IN_CLAUSE (clause, target))) {
			if (clause->flags & type) {
				g_assert (bcinfo [clause->handler_offset].is_block_start);
				return &cfg->bblocks [bcinfo [clause->handler_offset].block_id];
			}
		}
	}
	return NULL;
}

static void
mono_cfg_add_successor (MonoFlowGraph *cfg, MonoBBlock *bb, gint32 target)
{
	MonoBBlock *tbb;
	GList *l;

	g_assert (cfg->bcinfo [target].is_block_start);

	tbb = &cfg->bblocks [cfg->bcinfo [target].block_id];
	g_assert (tbb);

	for (l = bb->succ; l; l = l->next) {
		MonoBBlock *t = (MonoBBlock *)l->data;
		if (t == tbb)
			return;
	}

	bb->succ = g_list_prepend (bb->succ, tbb);
}


#define CREATE_BLOCK(t) {if (!bcinfo [t].is_block_start) {block_count++;bcinfo [t].is_block_start = 1; }}

static void
mono_analyze_flow (MonoFlowGraph *cfg)
{
	MonoMethod *method = cfg->method;
	const unsigned char *ip, *end;
	MonoMethodHeader *header;
	MonoBytecodeInfo *bcinfo;
	MonoExceptionClause *clause;
	MonoBBlock *bblocks, *bb;
	const MonoOpcode *opcode;
	gboolean block_end;
	int i, block_count;

	header = ((MonoMethodNormal *)method)->header;

	bcinfo = g_malloc0 (header->code_size * sizeof (MonoBytecodeInfo));
	bcinfo [0].is_block_start = 1;
	block_count = 1;
	block_end = FALSE;

	ip = header->code;
	end = ip + header->code_size;

	mono_jit_stats.cil_code_size += header->code_size;

	for (i = 0; i < header->num_clauses; ++i) {
		clause = &header->clauses [i];

		CREATE_BLOCK (clause->try_offset);
		CREATE_BLOCK (clause->handler_offset);

		if (clause->flags == MONO_EXCEPTION_CLAUSE_FILTER)
			CREATE_BLOCK (clause->token_or_filter);
	}

	while (ip < end) {
		guint32 cli_addr = ip - header->code;

		//printf ("IL%04x OPCODE %s\n", cli_addr, mono_opcode_names [*ip]);
		
		if (block_end) {
			CREATE_BLOCK (cli_addr);
			block_end = FALSE;
		}

		i = mono_opcode_value (&ip);

		opcode = &mono_opcodes [i];

		switch (opcode->flow_type) {
		case MONO_FLOW_RETURN:
		case MONO_FLOW_ERROR:
			block_end = 1;
			break;
		case MONO_FLOW_BRANCH: /* we handle branch when checking the argument type */
		case MONO_FLOW_COND_BRANCH:
		case MONO_FLOW_CALL:
		case MONO_FLOW_NEXT:
		case MONO_FLOW_META:
			break;
		default:
			g_assert_not_reached ();
		}

		switch (opcode->argument) {
		case MonoInlineNone:
			++ip;
			break;
		case MonoInlineString:
			mono_ldstr (mono_domain_get (), method->klass->image, mono_metadata_token_index (read32 (ip + 1)));
			/* fall through */
		case MonoInlineType:
		case MonoInlineField:
		case MonoInlineMethod:
		case MonoInlineTok:
		case MonoInlineSig:
		case MonoShortInlineR:
		case MonoInlineI:
			ip += 5;
			break;
		case MonoInlineVar:
			ip += 3;
			break;
		case MonoShortInlineVar:
		case MonoShortInlineI:
			ip += 2;
			break;
		case MonoShortInlineBrTarget:
			ip++;
			i = (signed char)*ip;
			ip++;
			CREATE_BLOCK (cli_addr + 2 + i);
			block_end = 1;
		       
			break;
		case MonoInlineBrTarget:
			ip++;
			i = read32 (ip);
			ip += 4;
			CREATE_BLOCK (cli_addr + 5 + i);
			block_end = 1;
			break;
		case MonoInlineSwitch: {
			gint32 st, target, n;
			++ip;
			n = read32 (ip);
			ip += 4;
			st = cli_addr + 5 + 4 * n;
			CREATE_BLOCK (st);

			for (i = 0; i < n; i++) {
				target = read32 (ip) + st;
				ip += 4;
				CREATE_BLOCK (target);			
			}
			/*
			 * Note: the code didn't set block_end in switch.
			 */
			break;
		}
		case MonoInlineR:
		case MonoInlineI8:
			ip += 9;
			break;
		default:
			g_assert_not_reached ();
		}
	}


	g_assert (block_count);

	bb = bblocks  = g_malloc0 (sizeof (MonoBBlock) * block_count);

	block_count = 0;
	bblocks [0].reached = 1;

	for (i = 0; i < header->code_size; i++) {
		if (bcinfo [i].is_block_start) {
			bb->cli_addr = i;
			bb->num = block_count;
			bb->forest = g_ptr_array_new ();
			if (block_count)
				bb [-1].length = i - bb [-1].cli_addr; 
			bcinfo [i].block_id = block_count;
			bb++;
			block_count++;
		}
	}
	bb [-1].length = header->code_size - bb [-1].cli_addr; 

	cfg->bcinfo = bcinfo;
	cfg->bblocks = bblocks;
	cfg->block_count = block_count;

	mono_jit_stats.basic_blocks += block_count;
	mono_jit_stats.max_basic_blocks = MAX (block_count, mono_jit_stats.max_basic_blocks);
       
	for (i = 0; i < header->num_clauses; ++i) {
		MonoBBlock *sbb, *tbb;
		clause = &header->clauses [i];
		sbb = &cfg->bblocks [bcinfo [clause->try_offset].block_id];
		tbb = &cfg->bblocks [bcinfo [clause->handler_offset].block_id];
		g_assert (sbb && tbb);
		sbb->succ = g_list_prepend (sbb->succ, tbb);
	}

	ip = header->code;
	end = ip + header->code_size;
	bb = NULL;

	while (ip < end) {
		guint32 cli_addr = ip - header->code;

		if (bcinfo [cli_addr].is_block_start) {
			MonoBBlock *tbb = &cfg->bblocks [bcinfo [cli_addr].block_id];		
			if (bb && !bb->succ)
				bb->succ = g_list_prepend (bb->succ, tbb);
			bb = tbb;
		}
		g_assert (bb);

		i = mono_opcode_value (&ip);

		opcode = &mono_opcodes [i];

		switch (opcode->argument) {
		case MonoInlineNone:
			++ip;
			break;
		case MonoInlineString:
		case MonoInlineType:
		case MonoInlineField:
		case MonoInlineMethod:
		case MonoInlineTok:
		case MonoInlineSig:
		case MonoShortInlineR:
		case MonoInlineI:
			ip += 5;
			break;
		case MonoInlineVar:
			ip += 3;
			break;
		case MonoShortInlineVar:
		case MonoShortInlineI:
			ip += 2;
			break;
		case MonoShortInlineBrTarget:
			ip++;
			i = (signed char)*ip;
			ip++;
			mono_cfg_add_successor (cfg, bb, cli_addr + 2 + i);
			if (opcode->flow_type == MONO_FLOW_COND_BRANCH)
				mono_cfg_add_successor (cfg, bb, cli_addr + 2);
			break;
		case MonoInlineBrTarget:
			ip++;
			i = read32 (ip);
			ip += 4;
			mono_cfg_add_successor (cfg, bb, cli_addr + 5 + i);
			if (opcode->flow_type == MONO_FLOW_COND_BRANCH)
				mono_cfg_add_successor (cfg, bb, cli_addr + 5);
			break;
		case MonoInlineSwitch: {
			gint32 st, target, n;
			++ip;
			n = read32 (ip);
			ip += 4;
			st = cli_addr + 5 + 4 * n;
			mono_cfg_add_successor (cfg, bb, st);

			for (i = 0; i < n; i++) {
				target = read32 (ip) + st;
				ip += 4;
				mono_cfg_add_successor (cfg, bb, target);
			}
			break;
		}
		case MonoInlineR:
		case MonoInlineI8:
			ip += 9;
			break;
		default:
			g_assert_not_reached ();
		}
	}
}

/**
 * ves_array_element_address:
 * @this: a pointer to the array object
 *
 * Returns: the address of an array element.
 */
static gpointer 
ves_array_element_address (MonoArray *this, ...)
{
	MonoClass *class;
	va_list ap;
	int i, ind, esize;
	gpointer ea;

	MONO_ARCH_SAVE_REGS;

	g_assert (this != NULL);

	va_start(ap, this);

	class = this->obj.vtable->klass;

	ind = va_arg(ap, int);
	g_assert (this->bounds != NULL);

	ind -= this->bounds [0].lower_bound;
	for (i = 1; i < class->rank; i++) {
		ind = ind*this->bounds [i].length + va_arg(ap, int) -
			this->bounds [i].lower_bound;;
	}

	if (ind >= this->max_length)
		mono_raise_exception (mono_get_exception_index_out_of_range ());

	esize = mono_array_element_size (class);
	ea = (gpointer*)((char*)this->vector + (ind * esize));
	//printf ("AADDRESS %p %p %d %d %08X\n", this, ea, ind, esize, *(gpointer *)ea);

	va_end(ap);

	return ea;
}

static MonoArray *
mono_array_new_va (MonoMethod *cm, ...)
{
	MonoDomain *domain = mono_domain_get ();
	va_list ap;
	guint32 *lengths;
	guint32 *lower_bounds;
	int pcount = cm->signature->param_count;
	int rank = cm->klass->rank;
	int i, d;

	va_start (ap, cm);

	lengths = alloca (sizeof (guint32) * pcount);
	for (i = 0; i < pcount; ++i)
		lengths [i] = d = va_arg(ap, int);

	if (rank == pcount) {
		/* Only lengths provided. */
		lower_bounds = NULL;
	} else {
		g_assert (pcount == (rank * 2));
		/* lower bounds are first. */
		lower_bounds = lengths;
		lengths += rank;
	}
	va_end(ap);

	return mono_array_new_full (domain, cm->klass, lengths, lower_bounds);
}

#define ADD_TREE(t,a)   do { t->cli_addr = a; g_ptr_array_add (forest, (t)); } while (0)
#define PUSH_TREE(t,k)  do { int tt = k; *sp = t; t->svt = tt; t->cli_addr = cli_addr; sp++; } while (0)

#define LOCAL_POS(n)    (1 + n)

#ifdef OPT_BOOL
#define LOCAL_TYPE(n)   ((header)->locals [(n)]->type == MONO_TYPE_BOOLEAN && !(header)->locals [(n)]->byref ? &mono_defaults.int32_class->byval_arg : (header)->locals [(n)])
#else
#define LOCAL_TYPE(n)   ((header)->locals [(n)])
#endif

#define ARG_POS(n)      (firstarg + n)
#define ARG_TYPE(n)     ((n) ? (signature)->params [(n) - (signature)->hasthis] : \
			(signature)->hasthis ? &method->klass->this_arg: (signature)->params [(0)])


/*
 * replaces all occurences of variable @varnum in @tree with @copy. 
 */
static void
mono_copy_used_var (MonoFlowGraph *cfg, MBTree *tree, int varnum, MBTree **copy)
{
	MBTree *t1, *t2;
	int v, size, align;

	if (tree->left)
		mono_copy_used_var (cfg, tree->left, varnum, copy);
	if (tree->right)
		mono_copy_used_var (cfg, tree->right, varnum, copy);

	switch (tree->op) {
	case MB_TERM_LDIND_I1:
	case MB_TERM_LDIND_I2:
	case MB_TERM_LDIND_I4:
	case MB_TERM_LDIND_I8:
	case MB_TERM_LDIND_R4:
	case MB_TERM_LDIND_R8:
		if (tree->left->op == MB_TERM_ADDR_L &&
		    tree->left->data.i == varnum) {
			if (*copy) {
				tree->left->data.i = (*copy)->left->data.i;
				return;
			} 
			
			mono_get_val_sizes (tree->svt, &size, &align);
			v = arch_allocate_var (cfg, size, align, MONO_TEMPVAR, tree->svt);
 
			t1 = mono_ctree_new_leaf (cfg->mp, MB_TERM_ADDR_L);
			t1->data.i = v;
		       
			t2 = mono_ctree_new_leaf (cfg->mp, MB_TERM_ADDR_L);
			t2->data.i = varnum;
			t2 = mono_ctree_new (cfg->mp, tree->op, t2, NULL);

			t2 = mono_ctree_new (cfg->mp, mono_map_store_svt_type (tree->svt), t1, t2);
			t2->svt = tree->svt;

			tree->left->data.i = v;

			*copy = t2;
		}
	}
}

/* 
 * if a variable is modified and there are still referencence
 * to it on the runtime stack we need to store the value into
 * a temporary variable and use that value instead of the 
 * modified one.
 */
static MBTree *
mono_stack_duplicate_used_var (MonoFlowGraph *cfg, MBTree **stack, MBTree **sp, int varnum)
{
	MBTree *res = NULL;

	while (stack < sp) {
		mono_copy_used_var (cfg, *stack, varnum, &res);
		stack++;
	}

	return res;
}

static int
check_inlining (MonoMethod *method)
{
	MonoMethodHeader *header;
	MonoMethodSignature *signature = method->signature;
	register const unsigned char *ip, *end;
	gboolean stop;
	int i, arg_used [256];

	g_assert (method);

	if (method->inline_info)
		return method->inline_count;

	method->inline_info = 1;
	
	if (method->wrapper_type != MONO_WRAPPER_NONE)
		goto fail;

	if ((method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) ||
	    (method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
	    (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) ||
	    (method->klass->marshalbyref) ||
	    MONO_TYPE_ISSTRUCT (signature->ret))
		goto fail;;
       
	if (!(header = ((MonoMethodNormal *)method)->header) ||
	    header->num_clauses)
		goto fail;;

	if (header->num_clauses)
		goto fail;
	
	ip = header->code;
	end = ip + header->code_size;

	for (i = 0; i < 256; i++)
		arg_used [i] = 0;

	stop = FALSE;        
	while (!stop && ip < end) {

		switch (*ip) {
		case CEE_NOP:
		case CEE_BREAK:
		case CEE_DUP:
		case CEE_POP:
		case CEE_LDIND_I1:
		case CEE_LDIND_U1:
		case CEE_LDIND_I2:
		case CEE_LDIND_U2:
		case CEE_LDIND_I4:
		case CEE_LDIND_U4:
		case CEE_LDIND_I8:
		case CEE_LDIND_I:
		case CEE_LDIND_R4:
		case CEE_LDIND_R8:
		case CEE_LDIND_REF:
		case CEE_STIND_REF:
		case CEE_STIND_I1:
		case CEE_STIND_I2:
		case CEE_STIND_I4:
		case CEE_STIND_I8:
		case CEE_STIND_R4:
		case CEE_STIND_R8:
		case CEE_ADD:
		case CEE_SUB:
		case CEE_MUL:
		case CEE_DIV:
		case CEE_DIV_UN:
		case CEE_REM:
		case CEE_REM_UN:
		case CEE_AND:
		case CEE_OR:
		case CEE_XOR:
		case CEE_SHL:
		case CEE_SHR:
		case CEE_SHR_UN:
		case CEE_NEG:
		case CEE_NOT:
		case CEE_CONV_I1:
		case CEE_CONV_I2:
		case CEE_CONV_I4:
		case CEE_CONV_I8:
		case CEE_CONV_R4:
		case CEE_CONV_R8:
		case CEE_CONV_U4:
		case CEE_CONV_U8:
		case CEE_CONV_R_UN:
		case CEE_CONV_OVF_I1_UN:
		case CEE_CONV_OVF_I2_UN:
		case CEE_CONV_OVF_I4_UN:
		case CEE_CONV_OVF_I8_UN:
		case CEE_CONV_OVF_U1_UN:
		case CEE_CONV_OVF_U2_UN:
		case CEE_CONV_OVF_U4_UN:
		case CEE_CONV_OVF_U8_UN:
		case CEE_CONV_OVF_I_UN:
		case CEE_CONV_OVF_U_UN:
		case CEE_LDLEN:
		case CEE_LDELEM_I1:
		case CEE_LDELEM_U1:
		case CEE_LDELEM_I2:
		case CEE_LDELEM_U2:
		case CEE_LDELEM_I4:
		case CEE_LDELEM_U4:
		case CEE_LDELEM_I8:
		case CEE_LDELEM_I:
		case CEE_LDELEM_R4:
		case CEE_LDELEM_R8:
		case CEE_LDELEM_REF:
		case CEE_STELEM_I:
		case CEE_STELEM_I1:
		case CEE_STELEM_I2:
		case CEE_STELEM_I4:
		case CEE_STELEM_I8:
		case CEE_STELEM_R4:
		case CEE_STELEM_R8:
		case CEE_STELEM_REF:
		case CEE_CONV_OVF_I1:
		case CEE_CONV_OVF_U1:
		case CEE_CONV_OVF_I2:
		case CEE_CONV_OVF_U2:
		case CEE_CONV_OVF_I4:
		case CEE_CONV_OVF_U4:
		case CEE_CONV_OVF_I8:
		case CEE_CONV_OVF_U8:
		case CEE_CKFINITE:
		case CEE_CONV_U2:
		case CEE_CONV_U1:
		case CEE_CONV_I:
		case CEE_CONV_OVF_I:
		case CEE_CONV_OVF_U:
		case CEE_ADD_OVF:
		case CEE_ADD_OVF_UN:
		case CEE_MUL_OVF:
		case CEE_MUL_OVF_UN:
		case CEE_SUB_OVF:
		case CEE_SUB_OVF_UN:
		case CEE_STIND_I:
		case CEE_CONV_U:
		case CEE_LDNULL:
		case CEE_LDC_I4_M1:
		case CEE_LDC_I4_0:
		case CEE_LDC_I4_1:
		case CEE_LDC_I4_2:
		case CEE_LDC_I4_3:
		case CEE_LDC_I4_4:
		case CEE_LDC_I4_5:
		case CEE_LDC_I4_6:
		case CEE_LDC_I4_7:
		case CEE_LDC_I4_8:
			++ip;
			break;
		case CEE_LDC_I4_S:
			ip += 2;
			break;
		case CEE_LDC_I4:
		case CEE_LDC_R4:
		case CEE_CPOBJ:
		case CEE_LDOBJ:
		case CEE_LDSTR:
		case CEE_CASTCLASS:
		case CEE_ISINST:
		case CEE_UNBOX:
		case CEE_LDFLD:
		case CEE_LDFLDA:
		case CEE_STFLD:
		case CEE_LDSFLD:
		case CEE_LDSFLDA:
		case CEE_STSFLD:
		case CEE_STOBJ:
		case CEE_BOX:
		case CEE_NEWARR:
		case CEE_LDELEMA:
		case CEE_LDTOKEN:
			ip += 5;
			break;
		case CEE_LDC_I8:
		case CEE_LDC_R8:
			ip += 9;
			break;
		case CEE_NEWOBJ:
		case CEE_CALL:
		case CEE_CALLVIRT: {
			MonoMethod *cm;
			guint32 token;
			++ip;
			token = read32 (ip);
			ip += 4;

			if (method->wrapper_type != MONO_WRAPPER_NONE)
				cm = mono_method_get_wrapper_data (method, token);
			else
				cm = mono_get_method (method->klass->image, token, NULL);
			g_assert (cm);

			if (cm == method)
				goto fail;

			/* we do not inline functions containing calls to 
			   stack query functions */ 
			if (cm->klass == mono_defaults.stack_frame_class ||
			    cm->klass == mono_defaults.stack_trace_class)
				goto fail;
				
			break;
		}
		case CEE_LDARG_0:
		case CEE_LDARG_1:
		case CEE_LDARG_2:
		case CEE_LDARG_3: {
			int an = (*ip) - CEE_LDARG_0;
			if (arg_used [an])
				goto fail;
			arg_used [an] = TRUE;
			++ip;
			break;
		}	
		case CEE_LDARG_S:
			++ip;
			if (arg_used [*ip])
				goto fail;
			arg_used [*ip] = TRUE;
			++ip;
			break;
	
		case CEE_PREFIX1: {
			++ip;			
			switch (*ip) {
			case CEE_LDARG: {
				guint16 an;
				ip++;
				an = read16 (ip);
				ip += 2;
				if (an > 255 || arg_used [an])
					goto fail;
				arg_used [an] = TRUE;
				break;
			}	

			case CEE_CEQ:
			case CEE_CGT:
			case CEE_CGT_UN:
			case CEE_CLT:
			case CEE_CLT_UN:
			case CEE_CPBLK:
			case CEE_INITBLK:
				ip++;
				break;
			case CEE_LDFTN:
			case CEE_LDVIRTFTN: {
				MonoMethod *cm;
				guint32 token;
				++ip;
				token = read32 (ip);
				ip += 4;

				cm = mono_get_method (method->klass->image, token, NULL);
				g_assert (cm);

				if (cm == method)
					goto fail;
				
				break;
			}
			case CEE_INITOBJ:
			case CEE_SIZEOF:
				ip += 5;
				break;
			default:
				stop = TRUE;
				break;
			}
		}
		default:
			stop = TRUE;
			break;			
		}
	}

	if (ip < end &&
	    !(ip [0] == CEE_RET ||
	      ((ip + 4) < end &&
	       ip [0] == CEE_STLOC_0 &&
	       ip [1] == CEE_BR_S &&
	       ip [2] == 0 &&
	       ip [3] == CEE_LDLOC_0 &&
	       ip [4] == CEE_RET)))
		goto fail;
	
	if (signature->hasthis && arg_used [0])
		method->uses_this = 1;

	mono_jit_stats.inlineable_methods++;

	return method->inline_count = ip - header->code;

 fail:
	return method->inline_count = -1;
}

static void
create_outstack (MonoFlowGraph *cfg, MonoBBlock *bb, MBTree **stack, int depth)
{
	MonoMemPool *mp = cfg->mp;
	MBTree **c = stack, *t1, *t2;
	GPtrArray *forest = bb->forest;
	int i;

	g_assert (bb->reached);

	if (depth <= 0)
		return;

	if (bb->outstack) {
		g_assert (bb->outdepth == depth);
		return;
	}

	bb->outdepth = depth;
	bb->outstack = mono_mempool_alloc (mp, depth * sizeof (MBTree *));
	
	for (i = 0; i < depth; i++) {
		if ((t1 = mono_store_tree (cfg, i, c [i], &t2)))
			ADD_TREE (t1, -1);
		bb->outstack [i] = t2;
	}
}

static void
mark_reached (MonoFlowGraph *cfg, MonoBBlock *target, MBTree **stack, int depth)
{
	MonoMemPool *mp = cfg->mp;
	int i;

	if (target->reached)
		return;

	target->reached = 1;

	if (depth == 0)
		return;

	g_assert (stack);

	if (target->instack) {
		g_assert (target->indepth == depth);
		return;
	}

	target->indepth = depth;
	target->instack = mono_mempool_alloc (mp, depth * sizeof (MBTree *));
	
	for (i = 0; i < depth; i++) {
		target->instack [i] = ctree_create_dup (mp, stack [i]);
	}
	
	
}

#define MARK_REACHED(bb) do { if (!bb->reached) { bb->reached = 1; }} while (0)

/**
 * mono_analyze_stack:
 * @cfg: control flow graph
 *
 * This is the architecture independent part of JIT compilation.
 * It creates a forest of trees which can then be fed into the
 * architecture dependent code generation.
 *
 * The algorithm is from Andi Krall, the same is used in CACAO
 */
static gboolean
mono_analyze_stack (MonoFlowGraph *cfg)
{
	MonoMethod *method = cfg->method;
	MonoMemPool *mp = cfg->mp;
	MonoBytecodeInfo *bcinfo = cfg->bcinfo;
	MonoMethodHeader *header;
	MonoMethodSignature *signature;
	MonoImage *image;
	MonoValueType svt;
	MBTree **sp, **stack, **arg_sp, **arg_map = NULL, *t1, *t2, *t3;
	MonoJitArgumentInfo *arg_info, default_arg_info [10];
	register const unsigned char *ip, *end;
	GPtrArray *forest;
	int i, j, depth, repeat_count;
	int varnum = 0, firstarg = 0;
	gboolean repeat, superblock_end;
	MonoBBlock *bb, *tbb;
	int maxstack;
	GList *inline_list = NULL;
	gboolean tail_recursion;

	header = ((MonoMethodNormal *)method)->header;
	signature = method->signature;
	image = method->klass->image; 

	/* we add 10 extra slots for method inlining */
	maxstack = header->max_stack + 10;
	sp = stack = alloca (sizeof (MBTree *) * (maxstack + 1));

	/* allocate local variables */

	if (header->num_locals) {
		int size, align;
		
		for (i = 0; i < header->num_locals; ++i) {
			MonoValueType svt;
			size = mono_type_size (LOCAL_TYPE (i), &align);
			mono_map_ldind_type (header->locals [i], &svt);
			varnum = arch_allocate_var (cfg, size, align, MONO_LOCALVAR, svt);
			if (i == 0)
				cfg->locals_start_index = varnum;
		}
	}

	cfg->args_start_index = firstarg = varnum + 1;
 
	/* allocate argument variables */

	if (signature->param_count + 1 < 10)
		arg_info = default_arg_info;
	else 
		arg_info = g_new (MonoJitArgumentInfo, signature->param_count + 1);

	arch_get_argument_info (signature, signature->param_count, arg_info);

	if (signature->hasthis) 
		arch_allocate_arg (cfg, &arg_info [0], VAL_POINTER);
	
	if (signature->param_count)
		for (i = 0; i < signature->param_count; ++i)
			arch_allocate_arg (cfg, &arg_info [i + 1], VAL_UNKNOWN);

	if (signature->param_count > 9)
		g_free (arg_info);


	for (i = 0; i < header->num_clauses; ++i) {
		MonoExceptionClause *clause = &header->clauses [i];		
		tbb = &cfg->bblocks [bcinfo [clause->handler_offset].block_id];
		if (clause->flags == MONO_EXCEPTION_CLAUSE_NONE ||
		    clause->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
			tbb->instack = mono_mempool_alloc (mp, sizeof (MBTree *));
			tbb->indepth = 1;
			tbb->instack [0] = t1 = mono_ctree_new_leaf (mp, MB_TERM_EXCEPTION);
			t1->data.i = mono_allocate_excvar (cfg);
			t1->svt = VAL_POINTER;
			tbb->reached = 1;
			if (clause->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
				tbb = &cfg->bblocks [bcinfo [clause->token_or_filter].block_id];
				g_assert (tbb);
				tbb->instack = mono_mempool_alloc (mp, sizeof (MBTree *));
				tbb->indepth = 1;
				tbb->instack [0] = t1 = mono_ctree_new_leaf (mp, MB_TERM_EXCEPTION);
				t1->data.i = mono_allocate_excvar (cfg);
				t1->svt = VAL_POINTER;
				tbb->reached = 1;
			}
		} else if (clause->flags == MONO_EXCEPTION_CLAUSE_FINALLY) {
			mark_reached (cfg, tbb, NULL, 0);
		} else {
			g_warning ("implement me");
			g_assert_not_reached ();
		}
	}

	repeat_count = 0;

	do {
		repeat = FALSE;
		superblock_end = TRUE;
		sp = stack;

		//printf ("START\n");
		for (i = 0; i < cfg->block_count; i++) {
			bb = &cfg->bblocks [i];
			
			//printf ("BBS %d %05x %05x %d %d %d %s\n", i, bb->cli_addr, bb->cli_addr + bb->length, bb->reached, bb->finished, superblock_end, method->name);
			
			if (!bb->reached && !superblock_end) {
				MonoBBlock *sbb = &cfg->bblocks [i - 1];

				g_assert (sbb->outdepth == (sp - stack));

				mark_reached (cfg, bb, sbb->outstack, sbb->outdepth);
			} 
			
			if (bb->reached) {

				if (!bb->finished) {

					sp = stack;

					for (j = 0; j < bb->indepth; j++) {
						sp [j] = bb->instack [j];
					}
					sp += bb->indepth;

					bb->finished = 1;
				
					ip = header->code + bb->cli_addr;
					end = ip + bb->length;

					forest = bb->forest;
				
					superblock_end = FALSE;

					tail_recursion = FALSE;

        while (inline_list || ip < end) {
		guint32 cli_addr;

		if (inline_list) {
			MonoInlineInfo *ii = (MonoInlineInfo *)inline_list->data;
			if (ip >= ii->end) {
				inline_list = g_list_remove_link (inline_list, inline_list);
				ip = ii->saved_ip;
				tail_recursion = FALSE;
				image = ii->saved_image;
				if (inline_list)
					arg_map = ((MonoInlineInfo *)inline_list->data)->arg_map;
				else 
					arg_map = NULL;
				continue;
			}
		} else 
			cli_addr = ip - header->code;

		
		//if (inline_list) printf ("INLINE IL%04x OPCODE %s\n", cli_addr, mono_opcode_names [*ip]);

		//printf ("%d IL%04x OPCODE %s %d %d %d\n", i, cli_addr, mono_opcode_names [*ip], 
		//forest->len, superblock_end, sp - stack);

		switch (*ip) {
			case CEE_THROW: {
			--sp;
			ip++;
			
			t1 = mono_ctree_new (mp, MB_TERM_THROW, *sp, NULL);
			ADD_TREE (t1, cli_addr);		
			superblock_end = TRUE;
			break;
		}
		case CEE_BOX: {
			MonoClass *c;
			guint32 token;
			
			--sp;
			++ip;
			token = read32 (ip);
			ip += 4;
			
			if (method->wrapper_type != MONO_WRAPPER_NONE)
				c = (MonoClass *)mono_method_get_wrapper_data (method, token);
			else
				c = mono_class_get (image, token);
			
			t1 = mono_ctree_new_leaf (mp, MB_TERM_NEWOBJ);
			t1->data.p = c;
			t1->svt = VAL_POINTER;

			t1 = mono_store_tree (cfg, -1, t1, &t3);
			g_assert (t1);
			ADD_TREE (t1, cli_addr);

			t1 = ctree_create_dup (mp, t3);
			t2 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
			t2->data.i = sizeof (MonoObject);
			t1 = mono_ctree_new (mp, MB_TERM_ADD, t1, t2);

			t1 = ctree_create_store (cfg, &c->byval_arg, t1, *sp, FALSE);
			ADD_TREE (t1, cli_addr);

			PUSH_TREE (t3, VAL_POINTER);

			break;
		}
		case CEE_UNBOX: {
			MonoClass *class;
			guint32 token;

			++ip;
			token = read32 (ip);
			ip += 4;
			sp--;

			if (method->wrapper_type != MONO_WRAPPER_NONE)
				class = (MonoClass *)mono_method_get_wrapper_data (method, token);
			else 
				class = mono_class_get (image, token);

			t1 = mono_ctree_new (mp, MB_TERM_UNBOX, *sp, NULL);
			t1->data.klass = class;

			PUSH_TREE (t1, VAL_POINTER);
			break;
		}
		case CEE_LDLEN: {
			ip++;
			sp--;

			t1 = mono_ctree_new (mp, MB_TERM_LDLEN, *sp, NULL);
			PUSH_TREE (t1, VAL_I32);
			break;
		}

		case CEE_LDOBJ: {
			guint32 token;
			MonoClass *c;

			++ip;
			token = read32 (ip);
			ip += 4;
			sp--;

			if (method->wrapper_type != MONO_WRAPPER_NONE)
				c = (MonoClass *)mono_method_get_wrapper_data (method, token);
			else
				c = mono_class_get (image, token);
			g_assert (c->valuetype);

			t1 = ctree_create_load (cfg, &c->byval_arg, *sp, &svt, FALSE);
			PUSH_TREE (t1, svt);
			break;
		}
		case CEE_STOBJ: {
			guint32 token;
			MonoClass *c;
			int size;

			++ip;
			token = read32 (ip);
			ip += 4;
			sp -= 2;

			c = mono_class_get (image, token);
			g_assert (c->valuetype);

			size = mono_class_value_size (c, NULL);
			
			t1 = mono_ctree_new (mp, MB_TERM_STIND_OBJ, sp [0], sp [1]);
			t1->data.i = size;
			ADD_TREE (t1, cli_addr);
			break;
		}
		case CEE_LDSTR: {
			MonoObject *o;
			guint32 ind;

			++ip;
			ind = mono_metadata_token_index (read32 (ip));
			ip += 4;

			if (cfg->share_code) {
				t1 = mono_ctree_new_leaf (mp, MB_TERM_LDSTR);
				t1->data.i = ind;
			} else {
				o = (MonoObject *) mono_ldstr (cfg->domain, image, ind);
				t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
				t1->data.p = o;
			}

			PUSH_TREE (t1, VAL_POINTER);
			break;
		}
		case CEE_LDSFLD:
		case CEE_LDSFLDA: {
			MonoClass *klass;
			MonoClassField *field;
			guint32 token;
			int load_addr = *ip == CEE_LDSFLDA;

			++ip;
			token = read32 (ip);
			ip += 4;

			/* need to handle fieldrefs */
			field = mono_field_from_token (image, token, &klass);
			g_assert (field);			

			if (cfg->share_code) {
				t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
				t1->data.i = field->offset;
				t1 = mono_ctree_new (mp, MB_TERM_LDSFLDA, t1, NULL);
				t1->data.klass = klass;
			} else {
				MonoVTable *vt = mono_class_vtable (cfg->domain, klass);
				t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_G);
				t1->data.p = (char*)(vt->data) + field->offset;
			}
			
			if (load_addr) {
				svt = VAL_POINTER;
			} else {
				t1 = ctree_create_load (cfg, field->type, t1, &svt, FALSE);
			}

			PUSH_TREE (t1, svt);
			break;
		}
		case CEE_LDFLD:
		case CEE_LDFLDA: {
			MonoClass *klass;
			MonoClassField *field;
			guint32 token;
			int load_addr = *ip == CEE_LDFLDA;

			++ip;
			token = read32 (ip);
			ip += 4;
			sp--;

			/* need to handle fieldrefs */
			field = mono_field_from_token (image, token, &klass);
			g_assert (field);
			
			if (klass->marshalbyref) {
				t1 = mono_ctree_new (mp, MB_TERM_REMOTE_LDFLDA, sp [0], NULL);
				t1->data.fi.klass = klass;
				t1->data.fi.field = field;
			} else {
				t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);

				if (klass->valuetype)
					t1->data.i = field->offset - sizeof (MonoObject);
				else 
					t1->data.i = field->offset;

				t1 = mono_ctree_new (mp, MB_TERM_ADD, sp [0], t1);
			}

			if (!load_addr) 
				t1 = ctree_create_load (cfg, field->type, t1, &svt, FALSE);
			else
				svt = VAL_POINTER;

			PUSH_TREE (t1, svt);
			break;
		}
		case CEE_STSFLD: {
			MonoClass *klass;
			MonoClassField *field;
			guint32 token;

			++ip;
			token = read32 (ip);
			ip += 4;
			--sp;

			/* need to handle fieldrefs */
			field = mono_field_from_token (image, token, &klass);
			g_assert (field);

			if (cfg->share_code) {
				t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
				t1->data.i = field->offset;
				t1 = mono_ctree_new (mp, MB_TERM_LDSFLDA, t1, NULL);
				t1->data.klass = klass;
			} else {
				MonoVTable *vt = mono_class_vtable (cfg->domain, klass);
				t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_G);
				t1->data.p = (char*)(vt->data) + field->offset;
			}
			t1 = ctree_create_store (cfg, field->type, t1, *sp, FALSE);

			ADD_TREE (t1, cli_addr);
			break;
		}
		case CEE_STFLD: {
			MonoClass *klass;
			MonoClassField *field;
			guint32 token;

			++ip;
			token = read32 (ip);
			ip += 4;
			sp -= 2;

			/* need to handle fieldrefs */
			field = mono_field_from_token (image, token, &klass);
			g_assert (field);

			if (klass->marshalbyref) {
				t1 = mono_ctree_new (mp, mono_map_remote_stind_type (field->type), sp [0], sp [1]);
				t1->data.fi.klass = klass;
				t1->data.fi.field = field;
			} else {
				t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
				t1->data.i = klass->valuetype ? field->offset - sizeof (MonoObject) : field->offset;
				t1 = mono_ctree_new (mp, MB_TERM_ADD, sp [0], t1);
				t1 = mono_ctree_new (mp, MB_TERM_CHECKTHIS, t1, NULL);
				t1 = ctree_create_store (cfg, field->type, t1, sp [1], FALSE);
			}

			ADD_TREE (t1, cli_addr);
			break;
		}
		case CEE_LDELEMA: {
			MonoClass *class;
			guint32 esize, token;

			++ip;
			token = read32 (ip);
			ip += 4;
			sp -= 2;

			class = mono_class_get (image, token);

			mono_class_init (class);

			esize = mono_class_array_element_size (class);

			t1 = mono_ctree_new (mp, MB_TERM_LDELEMA, sp [0], sp [1]);
			t1->data.i = esize;
			PUSH_TREE (t1, VAL_POINTER);
			break;
		}
		case CEE_NOP: { 
			++ip;
			break;
		}
		case CEE_BREAK: { 
			++ip;
			t1 = mono_ctree_new_leaf (mp, MB_TERM_BREAK);
			ADD_TREE (t1, cli_addr);
			break;
		} 
		case CEE_SWITCH: {
			guint32 k, n;
			MonoBBlock **jt;
			gint32 st, target;

			++ip;
			n = read32 (ip);
			ip += 4;
			--sp;

			t1 = mono_ctree_new (mp, MB_TERM_SWITCH, *sp, NULL);
			jt = t1->data.p = mono_alloc_static (sizeof (gpointer) * (n + 2));
			st = cli_addr + 5 + 4 * n;
			
			// hack: we store n at position 0
			jt [0] = (MonoBBlock *)n;

			create_outstack (cfg, bb, stack, sp - stack);

			for (k = 1; k <= (n + 1); k++) {
				if (k > n)
					target = st;
				else {
					target = read32 (ip) + st;
					ip += 4;
				}
				g_assert (target >= 0 && target <= header->code_size);
				g_assert (bcinfo [target].is_block_start);
				tbb = &cfg->bblocks [bcinfo [target].block_id];
				mark_reached (cfg, tbb, stack, sp - stack);
				jt [k] = tbb; 
			}

			ADD_TREE (t1, cli_addr);
			break;
		}
		case CEE_LDTOKEN: {
			gpointer handle;
			MonoClass *handle_class;
			MonoMethod *next_method;

			++ip;
			handle = mono_ldtoken (image, read32 (ip), &handle_class);
			ip += 4;

			if (!cfg->share_code && (*ip == CEE_CALL) && (next_method = mono_get_method (image, read32 (ip+1), NULL)) &&
					(next_method->klass == mono_defaults.monotype_class->parent) &&
					(strcmp (next_method->name, "GetTypeFromHandle") == 0)) {
				MonoClass *tclass = mono_class_from_mono_type (handle);
				mono_class_init (tclass);
				ip += 5;
				t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
				t1->data.p = mono_type_get_object (cfg->domain, handle);
			} else {
				t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
				t1->data.p = handle;
			}
			PUSH_TREE (t1, VAL_POINTER);

			break;
		}
		case CEE_NEWARR: {
			MonoClass *class;
			guint32 token;

			ip++;
			--sp;
			token = read32 (ip);
			ip += 4;

			if (method->wrapper_type != MONO_WRAPPER_NONE)
				class = (MonoClass *)mono_method_get_wrapper_data (method, token);
			else
				class = mono_class_get (image, token);

			if (cfg->share_code) {
				t1 = mono_ctree_new (mp, MB_TERM_NEWARR, *sp, NULL);
				t1->data.p = class;
			}
			else {
				MonoClass  *ac = mono_array_class_get (&class->byval_arg, 1);
				MonoVTable *vt = mono_class_vtable (cfg->domain, ac);

				t1 = mono_ctree_new (mp, MB_TERM_NEWARR_SPEC, *sp, NULL);
				t1->data.p = vt;
			}

			PUSH_TREE (t1, VAL_POINTER);
			break;
		}
		case CEE_CPOBJ: {
			MonoClass *class;
			guint32 token;

			++ip;
			token = read32 (ip);
			class = mono_class_get (image, token);
			ip += 4;
			sp -= 2;

			t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
			t1->data.i = mono_class_value_size (class, NULL);			
			t1 = mono_ctree_new (mp, MB_TERM_CPSRC, sp [1], t1);
			t1 = mono_ctree_new (mp, MB_TERM_CPBLK, sp [0], t1);
			ADD_TREE (t1, cli_addr);
			
			break;
		}
		case CEE_NEWOBJ: {
			MonoMethodSignature *csig;
			MonoMethod *cm;
			MBTree *this = NULL;
			guint32 token;
			int k, frame_size;
			int newarr = FALSE;
			int newstr = FALSE;

			++ip;
			token = read32 (ip);
			ip += 4;

			if (method->wrapper_type != MONO_WRAPPER_NONE)
				cm = mono_method_get_wrapper_data (method, token);
			else
				cm = mono_get_method (image, token, NULL);
			g_assert (cm);
			g_assert (!strcmp (cm->name, ".ctor"));
			
			csig = cm->signature;
			g_assert (csig->call_convention == MONO_CALL_DEFAULT);
			g_assert (csig->hasthis);
			
			arg_sp = sp -= csig->param_count;

			if (!cm->klass->inited)
				mono_class_init (cm->klass);

			if (cm->klass->parent == mono_defaults.array_class) {
				newarr = TRUE;
				this = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
				this->data.m = cm;
			} else if (cm->string_ctor) {
				static MonoString *string_dummy = NULL; 

				if (!string_dummy)
					string_dummy = mono_string_new_wrapper ("dummy");

				newstr = TRUE;
				/* we just pass a dummy as this, it is not used */
				this = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
				this->data.p = string_dummy;
			} else {				
				if (cm->klass->valuetype) {
					t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
					t1->data.i = mono_class_value_size (cm->klass, NULL);
					this = mono_ctree_new (mp, MB_TERM_LOCALLOC, t1, NULL);
					this->data.i = TRUE;
				} else if (cfg->share_code) {
					this = mono_ctree_new_leaf (mp, MB_TERM_NEWOBJ);
					this->data.klass = cm->klass;
				} else {
					MonoVTable *vt = mono_class_vtable (cfg->domain, cm->klass);
					this = mono_ctree_new_leaf (mp, MB_TERM_NEWOBJ_SPEC);
					this->data.p = vt;
				}

				this->svt = VAL_POINTER;

				t1 = mono_store_tree (cfg, -1, this, &this);
				g_assert (t1);
				ADD_TREE (t1, cli_addr);
			}
				
			if (csig->param_count + 1 < 10)
				arg_info = default_arg_info;
			else 
				arg_info = g_new (MonoJitArgumentInfo, csig->param_count + 1);

			frame_size = arch_get_argument_info (csig, csig->param_count, arg_info);
					
			for (k = csig->param_count - 1; k >= 0; k--) {
				t1 = mono_ctree_new (mp, mono_map_arg_type (csig->params [k]), arg_sp [k], NULL);
				t1->data.arg_info = arg_info [k + 1];
				ADD_TREE (t1, cli_addr);
			}

			if (newarr || newstr) {

				t2 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_G);
				t2->data.nonvirt_info.method = cm;
				if (newarr) {
					t2->data.nonvirt_info.p = mono_array_new_va;
				} else {
					t2->data.nonvirt_info.p = arch_create_jit_trampoline (cm);
				}

				t1 = mono_ctree_new (mp, MB_TERM_CALL_I4, this, t2);
				t1->data.call_info.pad = arg_info [0].pad;
				t1->data.call_info.frame_size = frame_size;
				t1->svt = VAL_POINTER;

				t1 = mono_store_tree (cfg, -1, t1, &t2);
				g_assert (t1);
				ADD_TREE (t1, cli_addr);
				PUSH_TREE (t2, t2->svt);

			} else {

				t2 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_G);
				t2->data.nonvirt_info.p = arch_create_jit_trampoline (cm);
				t2->data.nonvirt_info.method = cm;

				t1 = mono_ctree_new (mp, mono_map_call_type (csig->ret, &svt), this, t2);
				t1->data.call_info.pad = arg_info [0].pad;
				t1->data.call_info.frame_size = frame_size;
				t1->svt = svt;

				ADD_TREE (t1, cli_addr);

				t1 = ctree_create_dup (mp, this);	
				
				if (cm->klass->valuetype) {
					t2 = ctree_create_load (cfg, &cm->klass->byval_arg, t1, &svt, FALSE);
					PUSH_TREE (t2, svt);
				} else {
					PUSH_TREE (t1, t1->svt);
				}
			}

			if (csig->param_count > 9)
				g_free (arg_info);

			break;
		}
		case CEE_CALLI: 
		case CEE_CALL: 
		case CEE_CALLVIRT: {
			MonoMethodSignature *csig;
			MonoMethod *cm;
			MBTree *ftn, *this = NULL;
			guint32 token;
			int k, frame_size;
			int virtual = *ip == CEE_CALLVIRT;
			int calli = *ip == CEE_CALLI;
			int array_rank = 0;
			/* fixme: compute this value */
			gboolean shared_to_unshared_call = FALSE;
			int nargs, vtype_num = 0;

			++ip;
			token = read32 (ip);
			ip += 4;

			tail_recursion = FALSE;
					
			if (calli) {
				ftn = *(--sp);
				
				if (method->wrapper_type != MONO_WRAPPER_NONE)
					csig = (MonoMethodSignature *)mono_method_get_wrapper_data (method, token);
				else
					csig = mono_metadata_parse_signature (image, token);
      
				g_assert (csig);
				arg_sp = sp -= csig->param_count;
			} else {
				cm = mono_get_method (image, token, NULL);
				g_assert (cm);

				if (cm->klass == mono_defaults.math_class &&
				    cm->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) {

					if (!strcmp (cm->name, "Sin")) {
						--sp;
						t1 = mono_ctree_new (mp, MB_TERM_SIN, *sp, NULL);
						PUSH_TREE (t1, VAL_DOUBLE);
						break;
					} else if (!strcmp (cm->name, "Cos")) {
						--sp;
						t1 = mono_ctree_new (mp, MB_TERM_COS, *sp, NULL);
						PUSH_TREE (t1, VAL_DOUBLE);
						break;
					} else if (!strcmp (cm->name, "Sqrt")) {
						--sp;
						t1 = mono_ctree_new (mp, MB_TERM_SQRT, *sp, NULL);
						PUSH_TREE (t1, VAL_DOUBLE);
						break;
					}
				}

				if (cm->string_ctor)
					g_assert_not_reached ();

				arg_sp = sp -= cm->signature->param_count;

				if ((cm->flags & METHOD_ATTRIBUTE_FINAL && 
				     cm->klass != mono_defaults.object_class) ||
				    !(cm->flags & METHOD_ATTRIBUTE_VIRTUAL))
					virtual = 0;
			}

			g_assert (sp >= stack);

			if (!calli && mono_jit_inline_code && !virtual && cm->inline_count != -1 &&
			    cm != method && (cm->inline_info || check_inlining (cm) >= 0)) {
				MonoInlineInfo *ii = alloca (sizeof (MonoInlineInfo));
				int args;
				GList *l;

				/* avoid recursive inlining */
				for (l = inline_list; l; l = l->next) {
					if (((MonoInlineInfo *)l->data)->method == cm)
						break;
				}
				
				if (!l) {

					/* make sure runtime_init is called */
					mono_class_vtable (cfg->domain, cm->klass);

					mono_jit_stats.inlined_methods++;
				
					if (cm->signature->hasthis)
						sp--;

					args = cm->signature->param_count + cm->signature->hasthis;

					ii->method = cm;
					ii->saved_ip = ip;
					ii->saved_image = image;
					ii->arg_map = alloca (args * sizeof (MBTree *));
					memcpy (ii->arg_map, sp, args * sizeof (MBTree *));

					if (cm->signature->hasthis && !cm->uses_this && 
					    (ii->arg_map [0]->op != MB_TERM_CHECKTHIS)) {
						ii->arg_map [0] = mono_ctree_new (mp, MB_TERM_CHECKTHIS, 
										  ii->arg_map [0], NULL);
						ADD_TREE (ii->arg_map [0], ii->arg_map [0]->cli_addr);
					}
				
					if (cm->inline_count) {
						inline_list = g_list_prepend (inline_list, ii);
						ip = ((MonoMethodNormal *)cm)->header->code;
						ii->end = ip + cm->inline_count;
						arg_map = ii->arg_map;
						image = cm->klass->image;
					}
					continue;
				}
			}
			
			if (!calli) {
				MonoMethod *wrapper;
				if (cm->signature->pinvoke) {
#ifdef MONO_USE_EXC_TABLES
					if (mono_method_blittable (cm)) {
						csig = cm->signature;
					} else {
#endif
						wrapper = mono_marshal_get_native_wrapper (cm);
						csig = wrapper->signature;
#ifdef MONO_USE_EXC_TABLES
					}
#endif
				} else {
					csig = cm->signature;
				}
			}
			nargs = csig->param_count;

			g_assert (csig->call_convention == MONO_CALL_DEFAULT);
			g_assert (!virtual || csig->hasthis);

			if (!calli && cm->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) {
				if (cm->klass->parent == mono_defaults.array_class) {
					array_rank = cm->klass->rank;
				     
					if (cm->name [0] == 'S') /* Set */ 
						nargs--;
				}
			}

			if (csig->param_count + 1 < 10)
				arg_info = default_arg_info;
			else 
				arg_info = g_new (MonoJitArgumentInfo, csig->param_count + 1);
			
			frame_size = arch_get_argument_info (csig, nargs, arg_info);

			for (k = nargs - 1; k >= 0; k--) {
				g_assert (arg_sp [k]);
				t1 = mono_ctree_new (mp, mono_map_arg_type (csig->params [k]), arg_sp [k], NULL);
				t1->data.arg_info = arg_info [k + 1];
				ADD_TREE (t1, arg_sp [k]->cli_addr);
			}

			if (csig->hasthis) 
				this = *(--sp);		
			else
				this = mono_ctree_new_leaf (mp, MB_TERM_NOP);
			

			if (MONO_TYPE_ISSTRUCT (csig->ret) && !array_rank) {
				int size, align;
				if (csig->pinvoke)
					size = mono_class_native_size (csig->ret->data.klass, &align);
				else
					size = mono_class_value_size (csig->ret->data.klass, &align);

				vtype_num = arch_allocate_var (cfg, size, align, MONO_TEMPVAR, VAL_UNKNOWN);
			}

			if (array_rank) {

				t2 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
				t2->data.p = ves_array_element_address;
			       
				t1 = mono_ctree_new (mp, MB_TERM_CALL_I4, this, t2);
				t1->data.call_info.vtype_num = vtype_num;
				t1->data.call_info.frame_size = frame_size;
				t1->data.call_info.pad = arg_info [0].pad;
				t1->svt = VAL_POINTER;

				t1 = mono_store_tree (cfg, -1, t1, &t2);
				g_assert (t1);
				ADD_TREE (t1, cli_addr);

				if (cm->name [0] == 'G') { /* Get */
					t1 = mono_ctree_new (mp, mono_map_ldind_type (csig->ret, &svt), t2, NULL);
					t1->svt = svt;		
					PUSH_TREE (t1, t1->svt);
				} else if (cm->name [0] == 'S') { /* Set */
					t1 = ctree_create_store (cfg, csig->params [nargs], t2, arg_sp [nargs], FALSE);
					ADD_TREE (t1, cli_addr);			
				} else if (cm->name [0] == 'A') { /* Address */
					PUSH_TREE (t2, t1->svt);
				} else {
					g_assert_not_reached ();
				}

			} else {

				if (calli) {
					t2 = ftn; 
				} else if (virtual || (csig->hasthis && 
						       !(cm->flags & METHOD_ATTRIBUTE_VIRTUAL) &&
						       (cm->klass->marshalbyref || shared_to_unshared_call))) {
				
					mono_class_init (cm->klass);

					if (cm->klass->flags & TYPE_ATTRIBUTE_INTERFACE)
						t2 = mono_ctree_new_leaf (mp, MB_TERM_INTF_ADDR);
					else 
						t2 = mono_ctree_new_leaf (mp, MB_TERM_VFUNC_ADDR);
	 
					t2->data.m = cm;

				} else {
					t2 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_G);
					t2->data.nonvirt_info.p = arch_create_jit_trampoline (cm);
					t2->data.nonvirt_info.method = cm;
				}
	       

				t1 = mono_ctree_new (mp, mono_map_call_type (csig->ret, &svt), this, t2);
				t1->data.call_info.vtype_num = vtype_num;
				t1->data.call_info.frame_size = frame_size;
				t1->data.call_info.pad = arg_info [0].pad;
				t1->svt = svt;

				if (csig->ret->type != MONO_TYPE_VOID) {

					if (vtype_num) {
						ADD_TREE (t1, cli_addr);
						t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
						t1->data.i = vtype_num;
						PUSH_TREE (t1, VAL_POINTER); 
					} else {
						t1 = mono_store_tree (cfg, -1, t1, &t2);
						g_assert (t1);
						ADD_TREE (t1, cli_addr);
						PUSH_TREE (t2, t2->svt);
					}
				} else
					ADD_TREE (t1, cli_addr);
			}

			if (csig->param_count > 9)
				g_free (arg_info);
			
			break;
		}
		case CEE_ISINST: {
			MonoClass *c;
			guint32 token;
			++ip;
			token = read32 (ip);
			--sp;

			c = mono_class_get (image, token);
			if (!c->inited)
				mono_class_init (c);

			t1 = mono_ctree_new (mp, MB_TERM_ISINST, *sp, NULL);
			t1->data.klass = c;
			
			PUSH_TREE (t1, VAL_POINTER);

			ip += 4;
			break;
		}
		case CEE_CASTCLASS: {
			MonoClass *c;
			guint32 token;
			++ip;
			token = read32 (ip);
			--sp;

			c = mono_class_get (image, token);
			if (!c->inited)
				mono_class_init (c);

			t1 = mono_ctree_new (mp, MB_TERM_CASTCLASS, *sp, NULL);
			t1->data.klass = c;
			
			PUSH_TREE (t1, VAL_POINTER);

			ip += 4;
			break;
		}
		case CEE_LDC_I4_S: { 
			++ip;
			t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
			t1->data.i = *(const gint8 *)ip;
			++ip;
			PUSH_TREE (t1, VAL_I32);
			break;
		}
		case CEE_LDC_I4: { 
			++ip;
			t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
			t1->data.i = read32 (ip);
			ip += 4;
			PUSH_TREE (t1, VAL_I32);
			break;
		}
		case CEE_LDC_I4_M1:
		case CEE_LDC_I4_0:
		case CEE_LDC_I4_1:
		case CEE_LDC_I4_2:
		case CEE_LDC_I4_3:
		case CEE_LDC_I4_4:
		case CEE_LDC_I4_5:
		case CEE_LDC_I4_6:
		case CEE_LDC_I4_7:
		case CEE_LDC_I4_8: {
			t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
			t1->data.i = (*ip) - CEE_LDC_I4_0;
			++ip;
			PUSH_TREE (t1, VAL_I32);
			break;
		}
		case CEE_LDNULL: {
			//fixme: don't know if this is portable ?
			++ip;
			t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
			t1->data.i = 0;
			PUSH_TREE (t1, VAL_POINTER);
			break;
		}
		case CEE_LDC_I8: {
			++ip;
			t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I8);
			t1->data.l = read64 (ip);
			ip += 8;
			PUSH_TREE (t1, VAL_I64);		
			break;
		}
		case CEE_LDC_R4: {
			float *f = mono_alloc_static (sizeof (float));
			++ip;
			t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_R4);
			readr4 (ip, f);
			t1->data.p = f;
			ip += 4;
			PUSH_TREE (t1, VAL_DOUBLE);		
			break;
		}
		case CEE_LDC_R8: { 
			double *d = mono_alloc_static (sizeof (double));
			++ip;
			t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_R8);
			readr8 (ip, d);
			t1->data.p = d;
			ip += 8;
			PUSH_TREE (t1, VAL_DOUBLE);		
			break;
		}
		case CEE_LDLOC_0:
		case CEE_LDLOC_1:
		case CEE_LDLOC_2:
		case CEE_LDLOC_3: {
			int n = (*ip) - CEE_LDLOC_0;
			++ip;

			t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
			t1->data.i = LOCAL_POS (n);
			if (!MONO_TYPE_ISSTRUCT (LOCAL_TYPE (n))) 
				t1 = ctree_create_load (cfg, LOCAL_TYPE (n), t1, &svt, FALSE);
			PUSH_TREE (t1, svt);
			break;
		}
		case CEE_LDLOC_S: {
			++ip;
			
			t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
			t1->data.i = LOCAL_POS (*ip);
			if (!MONO_TYPE_ISSTRUCT (LOCAL_TYPE (*ip))) 
				t1 = ctree_create_load (cfg, LOCAL_TYPE (*ip), t1, &svt, FALSE);
			++ip;

			PUSH_TREE (t1, svt);
			break;
		}
		case CEE_LDLOCA_S: {
			++ip;

			t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
			t1->data.i = LOCAL_POS (*ip);
			VARINFO (cfg, t1->data.i).isvolatile = 1;
			++ip;
			PUSH_TREE (t1, VAL_POINTER);			
			break;
		}
		case CEE_STLOC_0:
		case CEE_STLOC_1:
		case CEE_STLOC_2:
		case CEE_STLOC_3: {
			int n = (*ip) - CEE_STLOC_0;
			++ip;
			--sp;

			t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
			t1->data.i = LOCAL_POS (n);

			if ((t2 = mono_stack_duplicate_used_var (cfg, stack, sp, t1->data.i)))
				ADD_TREE (t2, cli_addr); 

			t1 = ctree_create_store (cfg, LOCAL_TYPE (n), t1, *sp, FALSE);
			ADD_TREE (t1, cli_addr);			
			break;
		}
		case CEE_STLOC_S: {
			++ip;
			--sp;

			t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
			t1->data.i = LOCAL_POS (*ip);
			if ((t2 = mono_stack_duplicate_used_var (cfg, stack, sp, t1->data.i)))
				ADD_TREE (t2, cli_addr); 

			t1 = ctree_create_store (cfg, LOCAL_TYPE (*ip), t1, *sp, FALSE);
			++ip;
			ADD_TREE (t1, cli_addr);			
			break;
		}

		case CEE_ADD: {
			++ip;
			sp -= 2;
			if (sp [0]->op == MB_TERM_CONST_I4 && sp [1]->op == MB_TERM_CONST_I4)
				t1 = mono_ctree_new_icon4 (mp, sp [0]->data.i + sp [1]->data.i);
			else
				t1 = mono_ctree_new (mp, MB_TERM_ADD, sp [0], sp [1]);
			PUSH_TREE (t1, sp [0]->svt);
			break;                        
		}

		case CEE_SUB: {
			++ip;
			sp -= 2;
			if (sp [0]->op == MB_TERM_CONST_I4 && sp [1]->op == MB_TERM_CONST_I4)
				t1 = mono_ctree_new_icon4 (mp, sp [0]->data.i - sp [1]->data.i);
			else
				t1 = mono_ctree_new (mp, MB_TERM_SUB, sp [0], sp [1]);
			PUSH_TREE (t1, sp [0]->svt);
			break;                        
		}

		case CEE_AND: {
			++ip;
			sp -= 2;
			if (sp [0]->op == MB_TERM_CONST_I4 && sp [1]->op == MB_TERM_CONST_I4)
				t1 = mono_ctree_new_icon4 (mp, sp [0]->data.i & sp [1]->data.i);
			else
				t1 = mono_ctree_new (mp, MB_TERM_AND, sp [0], sp [1]);
			PUSH_TREE (t1, sp [0]->svt);
			break;                        
		}

		case CEE_OR: {
			++ip;
			sp -= 2;
			if (sp [0]->op == MB_TERM_CONST_I4 && sp [1]->op == MB_TERM_CONST_I4)
				t1 = mono_ctree_new_icon4 (mp, sp [0]->data.i | sp [1]->data.i);
			else
				t1 = mono_ctree_new (mp, MB_TERM_OR, sp [0], sp [1]);
			PUSH_TREE (t1, sp [0]->svt);
			break;                        
		}

		case CEE_XOR: {
			++ip;
			sp -= 2;
			if (sp [0]->op == MB_TERM_CONST_I4 && sp [1]->op == MB_TERM_CONST_I4)
				t1 = mono_ctree_new_icon4 (mp, sp [0]->data.i ^ sp [1]->data.i);
			else
				t1 = mono_ctree_new (mp, MB_TERM_XOR, sp [0], sp [1]);
			PUSH_TREE (t1, sp [0]->svt);
			break;                        
		}

		case CEE_MUL: {
			++ip;
			sp -= 2;
			if (sp [0]->op == MB_TERM_CONST_I4 && sp [1]->op == MB_TERM_CONST_I4) {
				t1 = mono_ctree_new_icon4 (mp, sp [0]->data.i * sp [1]->data.i);
				PUSH_TREE (t1, sp [0]->svt);
			} else {
				MAKE_SPILLED_BI_ALU1 (MUL, sp [0], sp [1])
			}
			break;                        
		}

		case CEE_DIV: {
			++ip;
			sp -= 2;
			if (sp [0]->op == MB_TERM_CONST_I4 && sp [1]->op == MB_TERM_CONST_I4 
			    && (sp[1]->data.i != 0) 
			    && ((sp[0]->data.i != 0x080000000) || (sp[1]->data.i != -1))) {
				t1 = mono_ctree_new_icon4 (mp, sp [0]->data.i / sp [1]->data.i);
				PUSH_TREE (t1, sp [0]->svt);
			} else {
				MAKE_SPILLED_BI_ALU1 (DIV, sp [0], sp [1])
			}
			break;                        
		}

		case CEE_REM: {
			++ip;
			sp -= 2;
			if (sp [0]->op == MB_TERM_CONST_I4 && sp [1]->op == MB_TERM_CONST_I4) {
				t1 = mono_ctree_new_icon4 (mp, sp [0]->data.i % sp [1]->data.i);
				PUSH_TREE (t1, sp [0]->svt);
			} else {
				MAKE_SPILLED_BI_ALU1 (REM, sp [0], sp [1])
			}
			break;                        
		}

		case CEE_SHL: {
			++ip;
			sp -= 2;
			if (sp [0]->op == MB_TERM_CONST_I4 && sp [1]->op == MB_TERM_CONST_I4) {
				t1 = mono_ctree_new_icon4 (mp, sp [0]->data.i << sp [1]->data.i);
				PUSH_TREE (t1, sp [0]->svt);
			} else {
				MAKE_SPILLED_BI_ALU1 (SHL, sp [0], sp [1])
			}
			break;                        
		}

		case CEE_SHR: {
			++ip;
			sp -= 2;
			if (sp [0]->op == MB_TERM_CONST_I4 && sp [1]->op == MB_TERM_CONST_I4) {
				t1 = mono_ctree_new_icon4 (mp, sp [0]->data.i >> sp [1]->data.i);
				PUSH_TREE (t1, sp [0]->svt);
			} else {
				MAKE_SPILLED_BI_ALU1 (SHR, sp [0], sp [1])
			}
			break;                        
		}

		MAKE_BI_ALU (ADD_OVF)
		MAKE_BI_ALU (ADD_OVF_UN)
		MAKE_BI_ALU (SUB_OVF)
		MAKE_BI_ALU (SUB_OVF_UN)
		MAKE_SPILLED_BI_ALU (SHR_UN)
		MAKE_SPILLED_BI_ALU (MUL_OVF)
		MAKE_SPILLED_BI_ALU (MUL_OVF_UN)
		MAKE_SPILLED_BI_ALU (DIV_UN)
		MAKE_SPILLED_BI_ALU (REM_UN)

		MAKE_LDIND (LDIND_I1,  MB_TERM_LDIND_I1, VAL_I32)
		MAKE_LDIND (LDIND_U1,  MB_TERM_LDIND_U1, VAL_I32)
		MAKE_LDIND (LDIND_I2,  MB_TERM_LDIND_I2, VAL_I32)
		MAKE_LDIND (LDIND_U2,  MB_TERM_LDIND_U2, VAL_I32)
		MAKE_LDIND (LDIND_I,   MB_TERM_LDIND_I4, VAL_I32)
		MAKE_LDIND (LDIND_I4,  MB_TERM_LDIND_I4, VAL_I32)
		MAKE_LDIND (LDIND_REF, MB_TERM_LDIND_REF, VAL_POINTER)
		MAKE_LDIND (LDIND_U4,  MB_TERM_LDIND_U4, VAL_I32)
		MAKE_LDIND (LDIND_I8,  MB_TERM_LDIND_I8, VAL_I64)
		MAKE_LDIND (LDIND_R4,  MB_TERM_LDIND_R4, VAL_DOUBLE)
		MAKE_LDIND (LDIND_R8,  MB_TERM_LDIND_R8, VAL_DOUBLE)

		MAKE_STIND (STIND_I1,  MB_TERM_STIND_I1)
		MAKE_STIND (STIND_I2,  MB_TERM_STIND_I2)
		MAKE_STIND (STIND_I,   MB_TERM_STIND_I4)
		MAKE_STIND (STIND_I4,  MB_TERM_STIND_I4)
		MAKE_STIND (STIND_I8,  MB_TERM_STIND_I8)
		MAKE_STIND (STIND_R4,  MB_TERM_STIND_R4)
		MAKE_STIND (STIND_R8,  MB_TERM_STIND_R8)
		MAKE_STIND (STIND_REF, MB_TERM_STIND_REF)

		MAKE_LDELEM (LDELEM_I1,  MB_TERM_LDIND_I1, VAL_I32, 1)
		MAKE_LDELEM (LDELEM_U1,  MB_TERM_LDIND_U1, VAL_I32, 1)
		MAKE_LDELEM (LDELEM_I2,  MB_TERM_LDIND_I2, VAL_I32, 2)
		MAKE_LDELEM (LDELEM_U2,  MB_TERM_LDIND_U2, VAL_I32, 2)
		MAKE_LDELEM (LDELEM_I,   MB_TERM_LDIND_I4, VAL_I32, 4)
		MAKE_LDELEM (LDELEM_I4,  MB_TERM_LDIND_I4, VAL_I32, 4)
		MAKE_LDELEM (LDELEM_REF, MB_TERM_LDIND_REF, VAL_POINTER, sizeof (gpointer))
		MAKE_LDELEM (LDELEM_U4,  MB_TERM_LDIND_U4, VAL_I32, 4)
		MAKE_LDELEM (LDELEM_I8,  MB_TERM_LDIND_I8, VAL_I64, 8)
		MAKE_LDELEM (LDELEM_R4,  MB_TERM_LDIND_R4, VAL_DOUBLE, 4)
		MAKE_LDELEM (LDELEM_R8,  MB_TERM_LDIND_R8, VAL_DOUBLE, 8)

		MAKE_STELEM (STELEM_I1,  MB_TERM_STIND_I1, 1)
		MAKE_STELEM (STELEM_I2,  MB_TERM_STIND_I2, 2)
		MAKE_STELEM (STELEM_I4,  MB_TERM_STIND_I4, 4)
		MAKE_STELEM (STELEM_I,   MB_TERM_STIND_I4, 4)
		MAKE_STELEM (STELEM_REF, MB_TERM_STIND_REF, sizeof (gpointer))
		MAKE_STELEM (STELEM_I8,  MB_TERM_STIND_I8, 8)
		MAKE_STELEM (STELEM_R4,  MB_TERM_STIND_R4, 4)
		MAKE_STELEM (STELEM_R8,  MB_TERM_STIND_R8, 8)

		case CEE_NEG: {
			ip++;
			sp--;
			if (sp [0]->op == MB_TERM_CONST_I4)
				t1 = mono_ctree_new_icon4 (mp, -sp [0]->data.i);
			else
				t1 = mono_ctree_new (mp, MB_TERM_NEG, sp [0], NULL);
			PUSH_TREE (t1, sp [0]->svt);		
			break;
		}
		case CEE_NOT: {
			ip++;
			sp--;
			if (sp [0]->op == MB_TERM_CONST_I4)
				t1 = mono_ctree_new_icon4 (mp, ~sp [0]->data.i);
			else
				t1 = mono_ctree_new (mp, MB_TERM_NOT, sp [0], NULL);
			PUSH_TREE (t1, sp [0]->svt);
			break;
		}
		case CEE_BR: 
	        case CEE_BR_S: {
			gint32 target;
			int br_s = (*ip == CEE_BR_S);

			++ip;
			if (br_s)
				target = cli_addr + 2 + (signed char) *ip;
			else
				target = cli_addr + 5 + (gint32) read32(ip);

			g_assert (target >= 0 && target <= header->code_size);
			g_assert (bcinfo [target].is_block_start);
			tbb = &cfg->bblocks [bcinfo [target].block_id];
			create_outstack (cfg, bb, stack, sp - stack);
			mark_reached (cfg, tbb, bb->outstack, bb->outdepth);

			t1 = mono_ctree_new_leaf (mp, MB_TERM_BR);
			t1->data.p = tbb;
			ADD_TREE (t1, cli_addr);

			if (br_s)
				++ip;
			else
				ip += 4;

			superblock_end = TRUE;
			break;
		}
		case CEE_JMP: {
			MonoMethod *cm;
			guint32 token;
			++ip;
			token = read32 (ip);
			ip += 4;

			cm = mono_get_method (method->klass->image, token, NULL);
			g_assert (cm);

			t1 = mono_ctree_new_leaf (mp, MB_TERM_JMP);
			/* fixme: our magic trampoline code does not work in this case,
			 * so I need to compile the method immediately */
			t1->data.p = mono_compile_method (cm);;

			ADD_TREE (t1, cli_addr);
			break;
		}
	        case CEE_LEAVE:
	        case CEE_LEAVE_S: {
			gint32 target;
			MonoBBlock *hb;
			int leave_s = (*ip == CEE_LEAVE_S);
			int k;
			++ip;
			if (leave_s)
				target = cli_addr + 2 + (signed char) *ip;
			else
				target = cli_addr + 5 + (gint32) read32(ip);

			g_assert (target >= 0 && target <= header->code_size);
			g_assert (bcinfo [target].is_block_start);
			tbb = &cfg->bblocks [bcinfo [target].block_id];

			/* empty the stack */
			while (sp != stack) {
				sp--;
				t1 = mono_ctree_new (mp, MB_TERM_POP, *sp, NULL);
				ADD_TREE (t1, cli_addr);
			}

			mark_reached (cfg, tbb, NULL, 0);

			/* fixme: fault handler */

			if ((hb = mono_find_final_block (cfg, cli_addr, target, MONO_EXCEPTION_CLAUSE_FINALLY))) {
				mark_reached (cfg, hb, NULL, 0);
				t1 = mono_ctree_new_leaf (mp, MB_TERM_HANDLER);
				t1->data.p = hb;
				ADD_TREE (t1, cli_addr);
			} 

			/* check if we leave a catch handler, if so we have to
			 * rethrow ThreadAbort exceptions */
			for (k = 0; k < header->num_clauses; ++k) {
				MonoExceptionClause *clause = &header->clauses [k];
				if (clause->flags == MONO_EXCEPTION_CLAUSE_NONE &&
				    MONO_OFFSET_IN_HANDLER (clause, cli_addr)) {
					t1 = mono_ctree_new_leaf (mp, MB_TERM_RETHROW_ABORT);
					t1->data.i = mono_allocate_excvar (cfg);
					ADD_TREE (t1, cli_addr);
					break;
				}
			}

			t1 = mono_ctree_new_leaf (mp, MB_TERM_BR);
			t1->data.p = tbb;
			ADD_TREE (t1, cli_addr);
			
			if (leave_s)
				++ip;
			else
				ip += 4;

			superblock_end = TRUE;
			break;
		}
		

		MAKE_CJUMP(BGT)
		MAKE_CJUMP(BGT_UN)
		MAKE_CJUMP(BLT)
		MAKE_CJUMP(BLT_UN)
		MAKE_CJUMP(BNE_UN)
		MAKE_CJUMP(BEQ)
		MAKE_CJUMP(BGE)
		MAKE_CJUMP(BGE_UN)
		MAKE_CJUMP(BLE)
		MAKE_CJUMP(BLE_UN)

		case CEE_BRTRUE:
		case CEE_BRTRUE_S: {
			gint32 target;
			int near_jump = *ip == CEE_BRTRUE_S;
			++ip;
			--sp;

			t1 = mono_ctree_new (mp, MB_TERM_BRTRUE, sp [0], NULL);

			if (near_jump)
				target = cli_addr + 2 + (signed char) *ip;
			else 
				target = cli_addr + 5 + (gint32) read32 (ip);

			g_assert (target >= 0 && target <= header->code_size);
			g_assert (bcinfo [target].is_block_start);
			tbb = &cfg->bblocks [bcinfo [target].block_id];
			create_outstack (cfg, bb, stack, sp - stack);
			mark_reached (cfg, tbb, bb->outstack, bb->outdepth);
  
			t1->data.p = tbb;
			ip += near_jump ? 1: 4;
			ADD_TREE (t1, cli_addr);
			break;
		}
		case CEE_BRFALSE:
		case CEE_BRFALSE_S: {
			gint32 target;
			int near_jump = *ip == CEE_BRFALSE_S;
			++ip;
			--sp;

			t1 = mono_ctree_new (mp, MB_TERM_BRFALSE, sp [0], NULL);

			if (near_jump)
				target = cli_addr + 2 + (signed char) *ip;
			else 
				target = cli_addr + 5 + (gint32) read32 (ip);

			g_assert (target >= 0 && target <= header->code_size);
			g_assert (bcinfo [target].is_block_start);
			tbb = &cfg->bblocks [bcinfo [target].block_id];
			create_outstack (cfg, bb, stack, sp - stack);
			mark_reached (cfg, tbb, bb->outstack, bb->outdepth);
		    
			t1->data.p = tbb;
			ip += near_jump ? 1: 4;
			ADD_TREE (t1, cli_addr);
			break;
		}
		case CEE_RET: {
			MonoType *ret = signature->ret;

			ip++;

			if (ret->type != MONO_TYPE_VOID) {
				--sp;
				if (MONO_TYPE_ISSTRUCT (ret)) {
					int align;
					t1 = mono_ctree_new (mp, MB_TERM_RET_OBJ, *sp, NULL);
					t1->data.i = mono_class_value_size (ret->data.klass, &align);
				} else {
					t1 = mono_ctree_new (mp, MB_TERM_RET, *sp, NULL);
				}
			} else {
				t1 = mono_ctree_new_leaf (mp, MB_TERM_RET_VOID);
			}

			t1->last_instr = (ip == (header->code + header->code_size));

			ADD_TREE (t1, cli_addr);

			if (sp > stack) {
				g_warning ("more values on stack at %s IL_%04x: %d",  
					   mono_method_full_name (method, TRUE), 
					   ip - header->code, sp - stack);
				mono_print_ctree (cfg, sp [-1]);
				printf ("\n");
			}
			superblock_end = TRUE;
			break;
		}
		case CEE_ENDFINALLY: {
			ip++;

			t1 = mono_ctree_new_leaf (mp, MB_TERM_ENDFINALLY);
			ADD_TREE (t1, cli_addr);
			t1->last_instr = FALSE;

			g_assert (sp == stack);
			superblock_end = TRUE;
			break;
		}
		case CEE_LDARG_0:
		case CEE_LDARG_1:
		case CEE_LDARG_2:
		case CEE_LDARG_3: {
			int n = (*ip) - CEE_LDARG_0;
			++ip;

			if (arg_map) {
				*sp = arg_map [n];
				sp++;
			} else {
				t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
				t1->data.i = ARG_POS (n);
				if (!MONO_TYPE_ISSTRUCT (ARG_TYPE (n))) 
					t1 = ctree_create_load (cfg, ARG_TYPE (n), t1, &svt, TRUE);
			
				PUSH_TREE (t1, svt);
			}

			break;
		}
		case CEE_LDARG_S: {
			++ip;

			if (arg_map) {
				*sp = arg_map [*ip];
				sp++;
			} else {
				t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
				t1->data.i = ARG_POS (*ip);
				if (!MONO_TYPE_ISSTRUCT (ARG_TYPE (*ip))) 
					t1 = ctree_create_load (cfg, ARG_TYPE (*ip), t1, &svt, TRUE);
				PUSH_TREE (t1, svt);
			}
			++ip;
			break;
		}
		case CEE_LDARGA_S: {
			++ip;
			t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
			t1->data.i = ARG_POS (*ip);
			PUSH_TREE (t1, VAL_POINTER);
			++ip;
			break;
		}
		case CEE_STARG_S: {
			++ip;
			--sp;

			t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
			t1->data.i = ARG_POS (*ip);
			t1 = ctree_create_store (cfg, ARG_TYPE (*ip), t1, *sp, TRUE);
			++ip;
			ADD_TREE (t1, cli_addr);			
			break;
		}
		case CEE_DUP: {
			int vnum;

			++ip; 

			/* fixme: maybe we should add more of these optimisations */
			if (sp [-1]->op == MB_TERM_CONST_I4) {

				t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
				t1->data.i = sp [-1]->data.i;
				PUSH_TREE (t1, VAL_I32);
			      
			} else {
				sp--;

				vnum = mono_allocate_intvar (cfg, sp - stack, sp [0]->svt);
				t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
				t1->data.i = vnum;
		       
				t2 = mono_ctree_new (mp, mono_map_store_svt_type (sp [0]->svt), t1, sp [0]);
				t2->svt = sp [0]->svt;
				ADD_TREE (t2, cli_addr);

				t1 = ctree_create_dup (mp, t2);		
				PUSH_TREE (t1, t1->svt);
				t1 = ctree_create_dup (mp, t1);		
				PUSH_TREE (t1, t1->svt);
			}
			break;
		}
		case CEE_POP: {
			++ip;
			--sp;

			t1 = mono_ctree_new (mp, MB_TERM_POP, *sp, NULL);
			ADD_TREE (t1, cli_addr);

			break;
		}
		case CEE_CKFINITE: {
			int vnum;
			++ip;
			sp--;

			/* this instr. can throw exceptions as side effect,
			 * so we cant eliminate dead code which contains CKFINITE opdodes.
			 * Spilling to memory makes sure that we always perform
			 * this check */
			vnum = mono_allocate_intvar (cfg, sp - stack, sp [0]->svt);
			t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
			t1->data.i = vnum;
		       
			t2 = mono_ctree_new (mp, MB_TERM_CKFINITE, *sp, NULL);

			t2 = mono_ctree_new (mp, mono_map_store_svt_type (sp [0]->svt), t1, t2);
			t2->svt = sp [0]->svt;
			ADD_TREE (t2, cli_addr);

			t1 = ctree_create_dup (mp, t2);		
			PUSH_TREE (t1, t1->svt);
			break;
		}
		case CEE_CONV_U1: 
			++ip;
			sp--;
			if (sp [0]->op == MB_TERM_CONST_I4)
				t1 = mono_ctree_new_icon4 (mp, (guint8)sp [0]->data.i);
			else
				t1 = mono_ctree_new (mp, MB_TERM_CONV_U1, *sp, NULL);
			PUSH_TREE (t1, VAL_I32);		
			break;
		case CEE_CONV_I1:
			++ip;
			sp--;
			if (sp [0]->op == MB_TERM_CONST_I4)
				t1 = mono_ctree_new_icon4 (mp, (gint8)sp [0]->data.i);
			else
				t1 = mono_ctree_new (mp, MB_TERM_CONV_I1, *sp, NULL);
			PUSH_TREE (t1, VAL_I32);		
			break;
		case CEE_CONV_U2: 
			++ip;
			sp--;
			if (sp [0]->op == MB_TERM_CONST_I4)
				t1 = mono_ctree_new_icon4 (mp, (guint16)sp [0]->data.i);
			else
				t1 = mono_ctree_new (mp, MB_TERM_CONV_U2, *sp, NULL);
			PUSH_TREE (t1, VAL_I32);		
			break;
		case CEE_CONV_I2:
			++ip;
			sp--;
			if (sp [0]->op == MB_TERM_CONST_I4)
				t1 = mono_ctree_new_icon4 (mp, (gint16)sp [0]->data.i);
			else
				t1 = mono_ctree_new (mp, MB_TERM_CONV_I2, *sp, NULL);
			PUSH_TREE (t1, VAL_I32);		
			break;
		case CEE_CONV_I: 
		case CEE_CONV_I4:
			++ip;
			sp--;
			if (sp [0]->op == MB_TERM_CONST_I4)
				t1 = *sp;
			else
				t1 = mono_ctree_new (mp, MB_TERM_CONV_I4, *sp, NULL);
			PUSH_TREE (t1, VAL_I32);		
			break;
		case CEE_CONV_U: 
		case CEE_CONV_U4: 
			++ip;
			sp--;
			if (sp [0]->op == MB_TERM_CONST_I4)
				t1 = *sp;
			else
				t1 = mono_ctree_new (mp, MB_TERM_CONV_U4, *sp, NULL);
			PUSH_TREE (t1, VAL_I32);		
			break;
		case CEE_CONV_I8:
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_I8, *sp, NULL);
			PUSH_TREE (t1, VAL_I64);		
			break;
		case CEE_CONV_U8:
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_U8, *sp, NULL);
			PUSH_TREE (t1, VAL_I64);		
			break;
		case CEE_CONV_R8:
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_R8, *sp, NULL);
			PUSH_TREE (t1, VAL_DOUBLE);		
			break;
		case CEE_CONV_R4:
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_R4, *sp, NULL);
			PUSH_TREE (t1, VAL_DOUBLE);		
			break;
		case CEE_CONV_R_UN:
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_R_UN, *sp, NULL);
			PUSH_TREE (t1, VAL_DOUBLE);		
			break;
		case CEE_CONV_OVF_I:
		case CEE_CONV_OVF_I4:
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_OVF_I4, *sp, NULL);
			PUSH_TREE (t1, VAL_I32);
			break;
		case CEE_CONV_OVF_I_UN:
		case CEE_CONV_OVF_I4_UN:
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_OVF_I4_UN, *sp, NULL);
			PUSH_TREE (t1, VAL_I32);		
			break;
		case CEE_CONV_OVF_U:
		case CEE_CONV_OVF_U4:
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_OVF_U4, *sp, NULL);
			PUSH_TREE (t1, VAL_I32);		
			break;
		case CEE_CONV_OVF_I1:
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_OVF_I1, *sp, NULL);
			PUSH_TREE (t1, VAL_I32);
			break;
		case CEE_CONV_OVF_I1_UN:
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_OVF_I1_UN, *sp, NULL);
			PUSH_TREE (t1, VAL_I32);
			break;
		case CEE_CONV_OVF_U1_UN:
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_OVF_U1_UN, *sp, NULL);
			PUSH_TREE (t1, VAL_I32);
			break;
		case CEE_CONV_OVF_U1:
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_OVF_U1, *sp, NULL);
			PUSH_TREE (t1, VAL_I32);
			break;
		case CEE_CONV_OVF_I2:
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_OVF_I2, *sp, NULL);
			PUSH_TREE (t1, VAL_I32);
			break;
		case CEE_CONV_OVF_U2_UN:
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_OVF_U2_UN, *sp, NULL);
			PUSH_TREE (t1, VAL_I32);
			break;
		case CEE_CONV_OVF_U2:
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_OVF_U2, *sp, NULL);
			PUSH_TREE (t1, VAL_I32);
			break;
		case CEE_CONV_OVF_I2_UN:
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_OVF_I2_UN, *sp, NULL);
			PUSH_TREE (t1, VAL_I32);
			break;
		case CEE_CONV_OVF_U8:
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_OVF_U8, *sp, NULL);
			PUSH_TREE (t1, VAL_I32);
			break;
		case CEE_CONV_OVF_U_UN:
		case CEE_CONV_OVF_U4_UN:
			// fixme: raise exceptions ?
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_I4, *sp, NULL);
			PUSH_TREE (t1, VAL_I32);		
			break;
		case CEE_CONV_OVF_I8_UN:
		case CEE_CONV_OVF_U8_UN: /* FIXME: slightly incorrect, but non worth fixing the corner cases in the old jit */
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_OVF_I8_UN, *sp, NULL);
			PUSH_TREE (t1, VAL_I64);
			break;
		case MONO_CUSTOM_PREFIX: {
			++ip;			
			switch (*ip) {
				
			case CEE_MONO_FUNC1: {
				MonoMarshalConv conv;
				++ip;

				conv = *ip;

				++ip;

				sp--;
				t1 = mono_ctree_new (mp, MB_TERM_FUNC1, *sp, NULL);

				switch (conv) {
				case MONO_MARSHAL_CONV_STR_LPWSTR:
					t1->data.p = mono_string_to_utf16;
					break;
				case MONO_MARSHAL_CONV_LPWSTR_STR:
					t1->data.p = mono_string_from_utf16;
					break;
				case MONO_MARSHAL_CONV_LPSTR_STR:
					t1->data.p = mono_string_new_wrapper;
					break;
				case MONO_MARSHAL_CONV_STR_LPTSTR:
				case MONO_MARSHAL_CONV_STR_LPSTR:
					t1->data.p = mono_string_to_utf8;
					break;
				case MONO_MARSHAL_CONV_STR_BSTR:
					t1->data.p = mono_string_to_bstr;
					break;
				case MONO_MARSHAL_CONV_STR_TBSTR:
				case MONO_MARSHAL_CONV_STR_ANSIBSTR:
					t1->data.p = mono_string_to_ansibstr;
					break;
				case MONO_MARSHAL_CONV_SB_LPSTR:
					t1->data.p = mono_string_builder_to_utf8;
					break;
				case MONO_MARSHAL_CONV_ARRAY_SAVEARRAY:
					t1->data.p = mono_array_to_savearray;
					break;
				case MONO_MARSHAL_CONV_ARRAY_LPARRAY:
					t1->data.p = mono_array_to_lparray;
					break;
				case MONO_MARSHAL_CONV_DEL_FTN:
					t1->data.p = mono_delegate_to_ftnptr;
					break;
				case MONO_MARSHAL_CONV_STRARRAY_STRLPARRAY:
					t1->data.p = mono_marshal_string_array;
					break;
				default:
					g_assert_not_reached ();
				}
				PUSH_TREE (t1, VAL_POINTER); 
				break;
			}
			case CEE_MONO_PROC2: {
				MonoMarshalConv conv;
				++ip;
				conv = *ip;
				++ip;

				sp -= 2;
				t1 = mono_ctree_new (mp, MB_TERM_PROC2, sp [0], sp [1]);

				switch (conv) {
				case MONO_MARSHAL_CONV_LPSTR_SB:
					t1->data.p = mono_string_utf8_to_builder;
					break;
				case MONO_MARSHAL_FREE_ARRAY:
					t1->data.p = mono_marshal_free_array;
					break;
				default:
					g_assert_not_reached ();
				}
				ADD_TREE (t1, cli_addr); 
				break;
			}
			case CEE_MONO_PROC3: {
				MonoMarshalConv conv;
				++ip;
				conv = *ip;
				++ip;

				sp -= 3;

				t1 = mono_ctree_new (mp, MB_TERM_CPSRC, sp [1], sp [2]);
				t1 = mono_ctree_new (mp, MB_TERM_PROC3, sp [0], t1);

				switch (conv) {
				case MONO_MARSHAL_CONV_STR_BYVALSTR:
					t1->data.p = mono_string_to_byvalstr;
					break;
				case MONO_MARSHAL_CONV_STR_BYVALWSTR:
					t1->data.p = mono_string_to_byvalwstr;
					break;
				default:
					g_assert_not_reached ();
				}

				ADD_TREE (t1, cli_addr);
				break;
			}
			case CEE_MONO_LDNATIVEOBJ: {
				guint32 token;
				MonoClass *c;

				++ip;
				token = read32 (ip);
				ip += 4;
				sp--;

				g_assert (method->wrapper_type != MONO_WRAPPER_NONE);
				c = (MonoClass *)mono_method_get_wrapper_data (method, token);
				g_assert (c->valuetype);
				
				t1 = ctree_create_load (cfg, &c->byval_arg, *sp, &svt, FALSE);
				PUSH_TREE (t1, svt);
				break;
			}
			case CEE_MONO_FREE: {
				++ip;

				sp -= 1;
				t1 = mono_ctree_new (mp, MB_TERM_FREE, *sp, NULL);
				ADD_TREE (t1, cli_addr);
				break;
			}
			case CEE_MONO_OBJADDR: {
				++ip;

				sp -= 1;
				t1 = mono_ctree_new (mp, MB_TERM_OBJADDR, *sp, NULL);
				PUSH_TREE (t1, VAL_POINTER);
				break;
			}
			case CEE_MONO_VTADDR: {
				++ip;

				sp -= 1;
				t1 = mono_ctree_new (mp, MB_TERM_VTADDR, *sp, NULL);
				PUSH_TREE (t1, VAL_POINTER);
				break;
			}
			case CEE_MONO_LDPTR: {
				guint32 token;
				++ip;
				
				token = read32 (ip);
				ip += 4;
				
				t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_G);
				t1->data.p = mono_method_get_wrapper_data (method, token);
				
				PUSH_TREE (t1, VAL_POINTER);
				break;
			}
			case CEE_MONO_NEWOBJ: {
				MonoClass *class;
				guint32 token;

				++ip;
				token = read32 (ip);
				ip += 4;

				class = (MonoClass *)mono_method_get_wrapper_data (method, token);

				t1 = mono_ctree_new_leaf (mp, MB_TERM_NEWOBJ);
				t1->data.p = class;
				PUSH_TREE (t1, VAL_POINTER);

				break;
			}
			case CEE_MONO_RETOBJ: {
				MonoType *ret = signature->ret;
				MonoClass *class;
				guint32 token;

				++ip;
				token = read32 (ip);
				ip += 4;

				class = (MonoClass *)mono_method_get_wrapper_data (method, token);

				sp--;

				g_assert (MONO_TYPE_ISSTRUCT (ret));

				t1 = ctree_create_load (cfg, &class->byval_arg, *sp, &svt, FALSE);
				t1 = mono_ctree_new (mp, MB_TERM_RET_OBJ, t1, NULL);
				t1->data.i = mono_class_native_size (ret->data.klass, NULL);
				t1->last_instr = (ip == (header->code + header->code_size));

				ADD_TREE (t1, cli_addr);

				if (sp > stack) {
					g_warning ("more values on stack at %s IL_%04x: %d",  
						   mono_method_full_name (method, TRUE), 
						   ip - header->code, sp - stack);
					mono_print_ctree (cfg, sp [-1]);
					printf ("\n");
				}
				superblock_end = TRUE;
				break;
			}
			default:
				g_error ("Unimplemented opcode at IL_%04x "
					 "%02x %02x", ip - header->code, MONO_CUSTOM_PREFIX, *ip);
			}
			break;
		}
		case CEE_PREFIX1: {
			++ip;			
			switch (*ip) {
				
			case CEE_ENDFILTER: {
				ip++;
				
				sp--;
				t1 = mono_ctree_new (mp, MB_TERM_ENDFILTER, *sp, NULL);
				ADD_TREE (t1, cli_addr);
				t1->last_instr = FALSE;
				
				g_assert (sp == stack);
				superblock_end = TRUE;
				break;
			}

			case CEE_LDLOC: {
				int n;
				++ip;
				n = read16 (ip);

				t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
				t1->data.i = LOCAL_POS (n);
				if (!MONO_TYPE_ISSTRUCT (LOCAL_TYPE (n))) 
					t1 = ctree_create_load (cfg, LOCAL_TYPE (n), t1, &svt, FALSE);
				ip += 2;

				PUSH_TREE (t1, svt);
				break;
			}
			case CEE_LDLOCA: {
				++ip;

				t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
				t1->data.i = LOCAL_POS (read16 (ip));
				VARINFO (cfg, t1->data.i).isvolatile = 1;
				ip += 2;
				PUSH_TREE (t1, VAL_POINTER);			
				break;
			}
			case CEE_STLOC: {
				++ip;
				--sp;

				t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
				t1->data.i = LOCAL_POS (read16 (ip));
				if ((t2 = mono_stack_duplicate_used_var (cfg, stack, sp, t1->data.i)))
					ADD_TREE (t2, cli_addr); 

				t1 = ctree_create_store (cfg, LOCAL_TYPE (read16 (ip)), t1, *sp, FALSE);
				ip += 2;
				ADD_TREE (t1, cli_addr);			
				break;
			}
			case CEE_STARG: {
				guint32 arg_pos;
				++ip;
				--sp;

				arg_pos = read16 (ip);
				ip += 2;

				t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
				t1->data.i = ARG_POS (arg_pos);
				t1 = ctree_create_store (cfg, ARG_TYPE (arg_pos), t1, *sp, TRUE);
				ADD_TREE (t1, cli_addr);			
				break;
			}

			MAKE_CMP (CEQ)
			MAKE_CMP (CLT)
			MAKE_CMP (CLT_UN)
			MAKE_CMP (CGT)
			MAKE_CMP (CGT_UN)

			case CEE_RETHROW: {
				++ip;
				t1 = mono_ctree_new_leaf (mp, MB_TERM_RETHROW);
				t1->data.i = mono_allocate_excvar (cfg);
				ADD_TREE (t1, cli_addr);
				break;
			}
			case CEE_LDFTN: {
				MonoMethod *cm;
				guint32 token;
				++ip;
				token = read32 (ip);
				ip += 4;

				if (method->wrapper_type != MONO_WRAPPER_NONE)
					cm = (MonoMethod *)mono_method_get_wrapper_data (method, token);
				else 
					cm = mono_get_method (image, token, NULL);

				g_assert (cm);
				
				t1 = mono_ctree_new_leaf (mp, MB_TERM_LDFTN);
				t1->data.m = cm;
				PUSH_TREE (t1, VAL_POINTER);
				break;
			}
			case CEE_LDVIRTFTN: {
				MonoMethod *cm;
				guint32 token;
				++ip;
				token = read32 (ip);
				ip += 4;
				--sp;

				cm = mono_get_method (image, token, NULL);
				g_assert (cm);

				if (cm->klass->flags & TYPE_ATTRIBUTE_INTERFACE)
					t2 = mono_ctree_new_leaf (mp, MB_TERM_INTF_ADDR);
				else 
					t2 = mono_ctree_new_leaf (mp, MB_TERM_VFUNC_ADDR);

				t2->data.m = cm;

				t1 = mono_ctree_new (mp, MB_TERM_LDVIRTFTN, *sp, t2);

				PUSH_TREE (t1, VAL_POINTER);

				break;
			}
			case CEE_INITOBJ: {
				MonoClass *class;
				guint32 token;
				
				++ip;
				token = read32 (ip);
				class = mono_class_get (image, token);
				ip += 4;
				sp--;
				
				t1 = mono_ctree_new (mp, MB_TERM_INITOBJ, *sp, NULL);
				t1->data.i = mono_class_value_size (class, NULL);
				ADD_TREE (t1, cli_addr);

				break;
			}
			case CEE_LDARG: {
				guint16 n;
				++ip;
				n = read16 (ip);
				ip += 2;

				t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
				t1->data.i = ARG_POS (n);
				if (!MONO_TYPE_ISSTRUCT (ARG_TYPE (n))) 
					t1 = ctree_create_load (cfg, ARG_TYPE (n), t1, &svt, TRUE);
				PUSH_TREE (t1, svt);
				break;
			}
			case CEE_LDARGA: {
				guint16 n;
				++ip;
				n = read16 (ip);
				ip += 2;

				t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
				t1->data.i = ARG_POS (n);
				PUSH_TREE (t1, svt);
				break;
			}
			case CEE_SIZEOF: {
				guint32 token;
				int align;
				++ip;
				token = read32 (ip);
				ip += 4;

				t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
				if (mono_metadata_token_table (token) == MONO_TABLE_TYPESPEC) {
					MonoType *type = mono_type_create_from_typespec (image, token);
					t1->data.i = mono_type_size (type, &align);
					mono_metadata_free_type (type);
				} else {
					MonoClass *szclass = mono_class_get (image, token);
					mono_class_init (szclass);
					g_assert (szclass->valuetype);
					t1->data.i = mono_class_value_size (szclass, &align);
				}

				PUSH_TREE (t1, VAL_I32);
				break;
			}
			case CEE_CPBLK: {
				++ip;
				sp -= 3;

				t1 = mono_ctree_new (mp, MB_TERM_CPSRC, sp [1], sp [2]);
				t1 = mono_ctree_new (mp, MB_TERM_CPBLK, sp [0], t1);
				ADD_TREE (t1, cli_addr);
				break;
			}
			case CEE_UNALIGNED_: {
				++ip;
				/* fixme: implement me */
				break;
			}
			case CEE_VOLATILE_: {
				++ip;
				/* fixme: implement me */ 	
				break;
			}
			case CEE_TAIL_:
				++ip;
				tail_recursion = TRUE;
				break;
			case CEE_LOCALLOC: {
				++ip;
				--sp;

				t1 = mono_ctree_new (mp, MB_TERM_LOCALLOC, *sp, NULL);
				t1->data.i = header->init_locals;
				PUSH_TREE (t1, VAL_POINTER);
				break;
			}
			case CEE_INITBLK: {
				++ip;
				sp -= 3;

				t1 = mono_ctree_new (mp, MB_TERM_CPSRC, sp [1], sp [2]);
				t1 = mono_ctree_new (mp, MB_TERM_INITBLK, sp [0], t1);
				ADD_TREE (t1, cli_addr);
				break;
			}
			default:
				g_error ("Unimplemented opcode at IL_%04x "
					 "0xFE %02x", ip - header->code, *ip);
			}
			break;
		}	
		default:
			g_warning ("unknown instruction `%s' at IL_%04X", 
				   mono_opcode_names [*ip], ip - header->code);
			if (mono_debug_format == MONO_DEBUG_FORMAT_NONE) {
				return FALSE;
			}
			mono_print_forest (cfg, forest);
			g_assert_not_reached ();
		}
	}		

        if ((depth = sp - stack)) {
		//printf ("DEPTH %d %d\n",  depth, sp [0]->op);
		//mono_print_forest (cfg, forest);
		create_outstack (cfg, bb, stack, sp - stack);
	}

	                        } else 
					superblock_end = TRUE;

			} else {
				superblock_end = TRUE;
				//printf ("unreached block %d\n", i);
				repeat = TRUE;
				if (repeat_count >= 10) {
					/*mono_print_forest (cfg, forest);
					g_warning ("repeat count exceeded at ip: 0x%04x in %s\n", bb->cli_addr, cfg->method->name);*/
					repeat = FALSE;
				}
			}
				//printf ("BBE %d %d %d %d\n", i, bb->reached, bb->finished, superblock_end);
		}

		repeat_count++;
		//printf ("REPEAT %d\n", repeat);
		mono_jit_stats.analyze_stack_repeat++;


	} while (repeat);

	return TRUE;
}

static void
mono_cfg_free (MonoFlowGraph *cfg)
{
	int i;

	for (i = 0; i < cfg->block_count; i++) {
		if (!cfg->bblocks [i].reached)
			continue;
		g_ptr_array_free (cfg->bblocks [i].forest, TRUE);
		g_list_free (cfg->bblocks [i].succ);
	}

	if (cfg->bcinfo)
		g_free (cfg->bcinfo);

	if (cfg->bblocks)
		g_free (cfg->bblocks);
		
	g_array_free (cfg->varinfo, TRUE);

	mono_mempool_destroy (cfg->mp);
}

int mono_exc_esp_offset = 0;

static MonoFlowGraph *
mono_cfg_new (MonoMethod *method)
{
	MonoVarInfo vi;
	MonoFlowGraph *cfg;
	MonoMemPool *mp = mono_mempool_new ();

	g_assert (((MonoMethodNormal *)method)->header);

	cfg = mono_mempool_alloc0 (mp, sizeof (MonoFlowGraph));

	cfg->domain = mono_domain_get ();
	cfg->method = method;
	cfg->mp = mp;

	/* reserve space to save LMF */
	cfg->locals_size = sizeof (MonoLMF);
	
	cfg->locals_size += sizeof (gpointer);
	mono_exc_esp_offset = - cfg->locals_size;

	cfg->locals_size += sizeof (gpointer);

	/* aligment check */
	g_assert (!(cfg->locals_size & 0x7));

	/* fixme: we should also consider loader optimisation attributes */
	cfg->share_code = mono_jit_share_code;

	cfg->varinfo = g_array_new (FALSE, TRUE, sizeof (MonoVarInfo));
	
	SET_VARINFO (vi, 0, 0, 0, 0);
	g_array_append_val (cfg->varinfo, vi); /* add invalid value at position 0 */

	cfg->intvars = mono_mempool_alloc0 (mp, sizeof (guint16) * VAL_DOUBLE * 
					    ((MonoMethodNormal *)method)->header->max_stack);

	mono_analyze_flow (cfg);

	if (!mono_analyze_stack (cfg)) {
		mono_cfg_free (cfg);
		return NULL;
	}
	
	return cfg;
}

static gpointer 
mono_get_runtime_method (MonoMethod* method)
{
	MonoMethod *nm;
	const char *name = method->name;

	if (method->klass->parent == mono_defaults.multicastdelegate_class) {
		if (*name == '.' && (strcmp (name, ".ctor") == 0)) {
			return (gpointer)mono_delegate_ctor;
		} else if (*name == 'I' && (strcmp (name, "Invoke") == 0)) {
		        nm = mono_marshal_get_delegate_invoke (method);
			return mono_compile_method (nm);
		} else if (*name == 'B' && (strcmp (name, "BeginInvoke") == 0)) {
			nm = mono_marshal_get_delegate_begin_invoke (method);
			return mono_compile_method (nm);
		} else if (*name == 'E' && (strcmp (name, "EndInvoke") == 0)) {
			nm = mono_marshal_get_delegate_end_invoke (method);
			return mono_compile_method (nm);
		}
	}
	return NULL;
}

#ifdef MONO_USE_EXC_TABLES
static gboolean
mono_type_blittable (MonoType *type)
{
	if (type->byref)
		return FALSE;

	switch (type->type){
	case MONO_TYPE_VOID:
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
		return TRUE;
	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_CLASS:
		return type->data.klass->blittable;
		break;
	default:
		break;
	}

	return FALSE;
}

gboolean
mono_method_blittable (MonoMethod *method)
{
	MonoMethodSignature *sig;
	int i;

	if (!method->addr)
		return FALSE;

	if (!mono_has_unwind_info (method)) {
		return FALSE;
	}

	if (method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL)
		return TRUE;

	sig = method->signature;

	if (!mono_type_blittable (sig->ret))
		return FALSE;

	for (i = 0; i < sig->param_count; i++)
		if (!mono_type_blittable (sig->params [i]))
			return FALSE;

	return TRUE;
}
#endif

/**
 * mono_jit_compile_method:
 * @method: pointer to the method info
 *
 * JIT compilation of a single method. 
 *
 * Returns: a pointer to the newly created code.
 */
static gpointer
mono_jit_compile_method (MonoMethod *method)
{
	MonoDomain *target_domain, *domain = mono_domain_get ();
	MonoJitInfo *ji;
	guint8 *addr;
	GHashTable *jit_code_hash;

	if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
	    (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL)) {

		if (!method->info) {
			MonoMethod *nm;

			if (!method->addr && (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL))
				mono_lookup_pinvoke_call (method);
#ifdef MONO_USE_EXC_TABLES
			if (mono_method_blittable (method)) {
				method->info = method->addr;
			} else {
#endif
				nm = mono_marshal_get_native_wrapper (method);
				method->info = mono_compile_method (nm);

				if (mono_debug_format != MONO_DEBUG_FORMAT_NONE) 
					mono_debug_add_wrapper (method, nm);
#ifdef MONO_USE_EXC_TABLES
			}
#endif
		}

		return method->info;
	}

	if (mono_jit_share_code)
		target_domain = mono_root_domain;
	else 
		target_domain = domain;

	jit_code_hash = target_domain->jit_code_hash;

	if ((addr = g_hash_table_lookup (jit_code_hash, method))) {
		mono_jit_stats.methods_lookups++;

		return addr;
	}

	mono_jit_stats.methods_compiled++;
	
	if (mono_jit_trace_calls || mono_jit_dump_asm || mono_jit_dump_forest) {
		printf ("Start JIT compilation of %s, domain '%s'\n",
			mono_method_full_name (method, TRUE), target_domain->friendly_name);
	}

	if (method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) {
		if (!(addr = mono_get_runtime_method (method))) {	
			mono_profiler_method_end_jit (method, MONO_PROFILE_FAILED);
			if (mono_debug_format != MONO_DEBUG_FORMAT_NONE) 
				return NULL;

			g_error ("Don't know how to exec runtime method %s", mono_method_full_name (method, TRUE));
		}
	} else {
		MonoMethodHeader *header = ((MonoMethodNormal *)method)->header;
		MonoFlowGraph *cfg;
		gulong code_size_ratio;
	
		mono_profiler_method_jit (method);
	
		if (!(cfg = mono_cfg_new (method))) {
			mono_profiler_method_end_jit (method, MONO_PROFILE_FAILED);
			return NULL;
		}
			
		cfg->code_size = MAX (header->code_size * 5, 256);
		cfg->start = cfg->code = g_malloc (cfg->code_size);

		if (mono_method_has_breakpoint (method, FALSE))
			x86_breakpoint (cfg->code);
		else if (mono_debug_format != MONO_DEBUG_FORMAT_NONE)
			x86_nop (cfg->code);

		if (!(ji = arch_jit_compile_cfg (target_domain, cfg))) {
			mono_profiler_method_end_jit (method, MONO_PROFILE_FAILED);
			return NULL;
		}
		
		addr = cfg->start;

		mono_jit_stats.allocated_code_size += cfg->code_size;

		code_size_ratio = cfg->code - cfg->start;
		if (code_size_ratio > mono_jit_stats.biggest_method_size) {
			mono_jit_stats.biggest_method_size = code_size_ratio;
			mono_jit_stats.biggest_method = method;
		}
		code_size_ratio = (code_size_ratio * 100) / header->code_size;
		if (code_size_ratio > mono_jit_stats.max_code_size_ratio) {
			mono_jit_stats.max_code_size_ratio = code_size_ratio;
			mono_jit_stats.max_ratio_method = method;
		}

		
		if (mono_jit_dump_asm) {
			char *id = g_strdup_printf ("%s.%s_%s", method->klass->name_space,
						    method->klass->name, method->name);
			mono_disassemble_code (cfg->start, cfg->code - cfg->start, id);
			g_free (id);
		}
		if (mono_debug_format != MONO_DEBUG_FORMAT_NONE)
			mono_debug_add_method (cfg);


		mono_jit_stats.native_code_size += ji->code_size;

		ji->num_clauses = header->num_clauses;

		if (header->num_clauses) {
			int i, start_block, end_block, filter_block;

			ji->clauses = mono_mempool_alloc0 (target_domain->mp, 
			        sizeof (MonoJitExceptionInfo) * header->num_clauses);

			for (i = 0; i < header->num_clauses; i++) {
				MonoExceptionClause *ec = &header->clauses [i];
				MonoJitExceptionInfo *ei = &ji->clauses [i];
			
				ei->flags = ec->flags;

				if (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
					g_assert (cfg->bcinfo [ec->token_or_filter].is_block_start);
					filter_block = cfg->bcinfo [ec->token_or_filter].block_id;
					ei->data.filter = cfg->start + cfg->bblocks [filter_block].addr;
				} else {
					ei->data.token = ec->token_or_filter;
				}

				g_assert (cfg->bcinfo [ec->try_offset].is_block_start);
				start_block = cfg->bcinfo [ec->try_offset].block_id;
				end_block = cfg->bcinfo [ec->try_offset + ec->try_len].block_id;
				g_assert (cfg->bcinfo [ec->try_offset + ec->try_len].is_block_start);
				
				ei->try_start = cfg->start + cfg->bblocks [start_block].addr;
				ei->try_end = cfg->start + cfg->bblocks [end_block].addr;
				
				g_assert (cfg->bcinfo [ec->handler_offset].is_block_start);
				start_block = cfg->bcinfo [ec->handler_offset].block_id;
				ei->handler_start = cfg->start + cfg->bblocks [start_block].addr;	
			}
		}
		
		mono_jit_info_table_add (target_domain, ji);

		mono_regset_free (cfg->rs);

		mono_cfg_free (cfg);

		mono_profiler_method_end_jit (method, MONO_PROFILE_OK);
	}

	if (mono_jit_trace_calls || mono_jit_dump_asm || mono_jit_dump_forest) {
		printf ("END JIT compilation of %s %p %p, domain '%s'\n",
				 mono_method_full_name (method, FALSE),
				 method,
				 addr,
				 target_domain->friendly_name);
	}

	g_hash_table_insert (jit_code_hash, method, addr);

	/* make sure runtime_init is called */
	mono_class_vtable (target_domain, method->klass);

	return addr;
}

/* mono_jit_create_remoting_trampoline:
 * @method: pointer to the method info
 *
 * Creates a trampoline which calls the remoting functions. This
 * is used in the vtable of transparent proxies.
 * 
 * Returns: a pointer to the newly created code 
 */
gpointer
mono_jit_create_remoting_trampoline (MonoMethod *method)
{
	MonoMethod *nm;
	guint8 *addr = NULL;

	nm = mono_marshal_get_remoting_invoke (method);
	addr = mono_compile_method (nm);

	return addr;
}

/* this function is never called */
static void
ves_array_set (MonoArray *this, ...)
{
	g_assert_not_reached ();
}

/* this function is never called */
static void
ves_array_get (MonoArray *this, ...)
{
	g_assert_not_reached ();
}

/**
 * mono_jit_exec:
 * @assembly: reference to an assembly
 * @argc: argument count
 * @argv: argument vector
 *
 * Start execution of a program.
 */
int 
mono_jit_exec (MonoDomain *domain, MonoAssembly *assembly, int argc, char *argv[])
{
	MonoImage *image = assembly->image;
	MonoMethod *method;

	method = mono_get_method (image, mono_image_get_entry_point (image), NULL);

	return mono_runtime_run_main (method, argc, argv, NULL);
}

#ifdef PLATFORM_WIN32
#define GET_CONTEXT \
	struct sigcontext *ctx = (struct sigcontext*)_dummy;
#else
#define GET_CONTEXT \
	void **_p = (void **)&_dummy; \
	struct sigcontext *ctx = (struct sigcontext *)++_p;
#endif

static void
sigfpe_signal_handler (int _dummy)
{
	MonoException *exc;
	GET_CONTEXT

	exc = mono_get_exception_divide_by_zero ();
	
	arch_handle_exception (ctx, exc, FALSE);
}

static void
sigill_signal_handler (int _dummy)
{
	MonoException *exc;
	GET_CONTEXT
	exc = mono_get_exception_execution_engine ("SIGILL");
	
	arch_handle_exception (ctx, exc, FALSE);
}

static void
sigsegv_signal_handler (int _dummy)
{
	MonoException *exc;
	GET_CONTEXT

	exc = mono_get_exception_null_reference ();
	
	arch_handle_exception (ctx, exc, FALSE);
}

static void
sigusr1_signal_handler (int _dummy)
{
	MonoThread *thread;
	GET_CONTEXT
	
	thread = mono_thread_current ();

	g_assert (thread->abort_exc);

	arch_handle_exception (ctx, thread->abort_exc, FALSE);
}

gpointer 
mono_get_lmf_addr (void)
{
	MonoJitTlsData *jit_tls;	

	if ((jit_tls = TlsGetValue (mono_jit_tls_id)))
		return &jit_tls->lmf;

	g_assert_not_reached ();
	return NULL;
}

/**
 * mono_thread_abort:
 * @obj: exception object
 *
 * abort the thread, print exception information and stack trace
 */
static void
mono_thread_abort (MonoObject *obj)
{
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
	
	g_free (jit_tls);

	ExitThread (-1);
}

static void
mono_thread_start_cb (guint32 tid, gpointer stack_start, gpointer func)
{
	MonoJitTlsData *jit_tls;
	MonoLMF *lmf;

	jit_tls = g_new0 (MonoJitTlsData, 1);

	TlsSetValue (mono_jit_tls_id, jit_tls);

	jit_tls->abort_func = mono_thread_abort;
	jit_tls->end_of_stack = stack_start;

	lmf = g_new0 (MonoLMF, 1);
	lmf->ebp = -1;

	jit_tls->lmf = lmf;

	mono_debugger_event (MONO_DEBUGGER_EVENT_THREAD_CREATED, (gpointer)tid, func);
}

void (*mono_thread_attach_aborted_cb ) (MonoObject *obj) = NULL;

static void
mono_thread_abort_dummy (MonoObject *obj)
{
  if (mono_thread_attach_aborted_cb)
    mono_thread_attach_aborted_cb (obj);
  else
    mono_thread_abort (obj);
}

static void
mono_thread_attach_cb (guint32 tid, gpointer stack_start)
{
	MonoJitTlsData *jit_tls;
	MonoLMF *lmf;

	jit_tls = g_new0 (MonoJitTlsData, 1);

	TlsSetValue (mono_jit_tls_id, jit_tls);

	jit_tls->abort_func = mono_thread_abort_dummy;
	jit_tls->end_of_stack = stack_start;

	lmf = g_new0 (MonoLMF, 1);
	lmf->ebp = -1;

	jit_tls->lmf = lmf;
}

static CRITICAL_SECTION ms;

static void
mono_runtime_install_handlers (void)
{
#ifndef PLATFORM_WIN32
	struct sigaction sa;
#endif

#ifdef PLATFORM_WIN32
	win32_seh_init();
	win32_seh_set_handler(SIGFPE, sigfpe_signal_handler);
	win32_seh_set_handler(SIGILL, sigill_signal_handler);
	win32_seh_set_handler(SIGSEGV, sigsegv_signal_handler);
#else /* !PLATFORM_WIN32 */

	/* libpthreads has its own implementation of sigaction(),
	 * but it seems to work well with our current exception
	 * handlers. If not we must call syscall directly instead 
	 * of sigaction */
	
	/* catch SIGFPE */
	sa.sa_handler = sigfpe_signal_handler;
	sigemptyset (&sa.sa_mask);
	sa.sa_flags = 0;
	//g_assert (syscall (SYS_sigaction, SIGFPE, &sa, NULL) != -1);
	g_assert (sigaction (SIGFPE, &sa, NULL) != -1);

	/* catch SIGILL */
	sa.sa_handler = sigill_signal_handler;
	sigemptyset (&sa.sa_mask);
	sa.sa_flags = 0;
	//g_assert (syscall (SYS_sigaction, SIGILL, &sa, NULL) != -1);
	g_assert (sigaction (SIGILL, &sa, NULL) != -1);

	/* catch the thread abort signal */
	sa.sa_handler = sigusr1_signal_handler;
	sigemptyset (&sa.sa_mask);
	sa.sa_flags = 0;
	//g_assert (syscall (SYS_sigaction, SIGILL, &sa, NULL) != -1);
	g_assert (syscall (SYS_sigaction, mono_thread_get_abort_signal (), &sa, NULL) != -1);
	//g_assert (sigaction (mono_thread_get_abort_signal (), &sa, NULL) != -1);

#if 1
	/* catch SIGSEGV */
	sa.sa_handler = sigsegv_signal_handler;
	sigemptyset (&sa.sa_mask);
	sa.sa_flags = 0;
	//g_assert (syscall (SYS_sigaction, SIGSEGV, &sa, NULL) != -1);
	g_assert (sigaction (SIGSEGV, &sa, NULL) != -1);
#endif
#endif /* PLATFORM_WIN32 */
}

MonoDomain*
mono_jit_init (const char *file) {
	MonoDomain *domain;

	mono_cpu_detect ();

	mono_runtime_install_handlers ();

	mono_init_icall ();
	mono_add_internal_call ("System.Array::Set", ves_array_set);
	mono_add_internal_call ("System.Array::Get", ves_array_get);
	mono_add_internal_call ("System.Array::Address", ves_array_element_address);
	mono_add_internal_call ("System.Diagnostics.StackFrame::get_frame_info", 
				ves_icall_get_frame_info);
	mono_add_internal_call ("System.Diagnostics.StackTrace::get_trace", 
				ves_icall_get_trace);
	mono_add_internal_call ("Mono.Runtime::mono_runtime_install_handlers", mono_runtime_install_handlers);

	metadata_section = &ms;
	InitializeCriticalSection (metadata_section);

	mono_jit_tls_id = TlsAlloc ();

	/* Don't set up the main thread for managed code execution -
	 * this will give a handy assertion fail in
	 * mono_get_lmf_addr() if any buggy runtime code tries to run
	 * managed code in this thread.
	 *
	 * Note, adding static initializer/objects to thread.cs will 
	 *       also cause mon_get_lmf_addr assertion
	 *
	 */
	/* mono_thread_start_cb (GetCurrentThreadId (), (gpointer)-1, NULL); */

	mono_install_compile_method (mono_jit_compile_method);
	mono_install_trampoline (arch_create_jit_trampoline);
	mono_install_remoting_trampoline (mono_jit_create_remoting_trampoline);
	mono_install_handler (arch_get_throw_exception ());
	mono_install_runtime_invoke (mono_jit_runtime_invoke);
	mono_install_stack_walk (mono_jit_walk_stack);
	mono_install_get_config_dir ();

	domain = mono_init (file);
	mono_runtime_init (domain, mono_thread_start_cb, mono_thread_attach_cb);

	return domain;
}

void
mono_jit_cleanup (MonoDomain *domain)
{
	mono_runtime_cleanup (domain);

	mono_domain_finalize (domain);

	mono_debug_cleanup ();

#ifdef PLATFORM_WIN32
	win32_seh_cleanup();
#endif

	mono_domain_unload (domain, TRUE);

	if (mono_jit_stats.enabled) {
		g_print ("Mono Jit statistics\n");
		g_print ("Compiled methods:       %ld\n", mono_jit_stats.methods_compiled);
		g_print ("Methods cache lookup:   %ld\n", mono_jit_stats.methods_lookups);
		g_print ("Method trampolines:     %ld\n", mono_jit_stats.method_trampolines);
		g_print ("Basic blocks:           %ld\n", mono_jit_stats.basic_blocks);
		g_print ("Max basic blocks:       %ld\n", mono_jit_stats.max_basic_blocks);
		g_print ("Allocated vars:         %ld\n", mono_jit_stats.allocate_var);
		g_print ("Analyze stack repeat:   %ld\n", mono_jit_stats.analyze_stack_repeat);
		g_print ("Compiled CIL code size: %ld\n", mono_jit_stats.cil_code_size);
		g_print ("Native code size:       %ld\n", mono_jit_stats.native_code_size);
		g_print ("Max code size ratio:    %.2f (%s::%s)\n", mono_jit_stats.max_code_size_ratio/100.0,
				mono_jit_stats.max_ratio_method->klass->name, mono_jit_stats.max_ratio_method->name);
		g_print ("Biggest method:         %ld (%s::%s)\n", mono_jit_stats.biggest_method_size,
				mono_jit_stats.biggest_method->klass->name, mono_jit_stats.biggest_method->name);
		g_print ("Code reallocs:          %ld\n", mono_jit_stats.code_reallocs);
		g_print ("Allocated code size:    %ld\n", mono_jit_stats.allocated_code_size);
		g_print ("Inlineable methods:     %ld\n", mono_jit_stats.inlineable_methods);
		g_print ("Inlined methods:        %ld\n", mono_jit_stats.inlined_methods);
		
		g_print ("\nCreated object count:   %ld\n", mono_stats.new_object_count);
		g_print ("Initialized classes:    %ld\n", mono_stats.initialized_class_count);
		g_print ("Used classes:           %ld\n", mono_stats.used_class_count);
		g_print ("Static data size:       %ld\n", mono_stats.class_static_data_size);
		g_print ("VTable data size:       %ld\n", mono_stats.class_vtable_size);
	}

	DeleteCriticalSection (metadata_section);

}

/**
 * mono_jit_image:
 * @image: reference to an image
 * @verbose: If true, print debugging information on stdout.
 *
 * JIT compilation of all methods in the image.
 */
void
mono_jit_compile_image (MonoImage *image, int verbose)
{
	MonoMethod *method;
	MonoTableInfo *t = &image->tables [MONO_TABLE_METHOD];
	int i;

	for (i = 0; i < t->rows; i++) {

		method = mono_get_method (image, 
					  (MONO_TABLE_METHOD << 24) | (i + 1), 
					  NULL);

		if (verbose)
			g_print ("Compiling: %s:%s\n\n", image->assembly_name, method->name);

		if (method->flags & METHOD_ATTRIBUTE_ABSTRACT) {
			if (verbose)
				printf ("ABSTARCT\n");
		} else
			mono_compile_method (method);

	}

}

