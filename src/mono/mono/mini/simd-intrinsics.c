/*
 * simd-instrisics.c: simd support for intrinsics
 *
 * Author:
 *   Rodrigo Kumpera (rkumpera@novell.com)
 *
 * (C) 2008 Novell, Inc.
 */

#include <config.h>
#include <stdio.h>

#define NEW_IR
#include "mini.h"
#include "ir-emit.h"

/*
General notes on SIMD intrinsics

TODO handle operands with non SIMD args, such as op_Addition (Vector4f, float)
TODO optimize r4const in .ctor so it doesn't go into the FP stack first
TODO extend op_to_op_dest_membase to handle simd ops
TODO add support for indexed versions of simd ops
TODO to an amd64 port and figure out how to properly handle extractors/.ctor
TODO make sure locals, arguments and spills are properly aligned.
TODO add support for fusing a XMOVE into a simd op in mono_spill_global_vars.
TODO add stuff to man pages
TODO document this under /docs
TODO make passing a xmm as argument not cause it to be LDADDR'ed (introduce an OP_XPUSH)
TODO revamp the .ctor sequence as it looks very fragile, maybe use a var just like iconv_to_r8_raw. 
TODO figure out what's wrong with OP_STOREX_MEMBASE_REG and OP_STOREX_MEMBASE (the 2nd is for imm operands)
TODO maybe add SSE3 emulation on top of SSE2, or just implement the corresponding functions using SSE2 intrinsics.
TODO pass simd arguments in registers or, at least, add SSE support for pushing large (>=16) valuetypes 
TODO pass simd args byval to a non-intrinsic method cause some useless local var load/store to happen. 

General notes for SIMD intrinsics.

-Bad extractor and constructor performance
Extracting a float from a XMM is a complete disaster if you are passing it as an argument.
It will be loaded in the FP stack just to be pushed on the call stack.

A similar thing happens with Vector4f constructor that require float vars to be 

The fix for this issue is similar to the one required for r4const as method args. Avoiding the
trip to the FP stack is desirable.

-Extractor and constructor code doesn't make sense under amd64. Both currently assume separate banks
for simd and fp.


-Promote OP_EXTRACT_I4 to a STORE op
The advantage of this change is that it could have a _membase version and promote further optimizations.

-Create a MONO_INST_DONT_REGALLOC and use it in all places that MONO_INST_INDIRECT is used
without a OP_LDADDR.
*/

#ifdef MONO_ARCH_SIMD_INTRINSICS

//#define IS_DEBUG_ON(cfg) (0)

#define IS_DEBUG_ON(cfg) ((cfg)->verbose_level >= 3)
#define DEBUG(a) do { if (IS_DEBUG_ON(cfg)) { a; } } while (0)
enum {
	SIMD_EMIT_BINARY,
	SIMD_EMIT_BINARY_SSE3,
	SIMD_EMIT_UNARY,
	SIMD_EMIT_GETTER,
	SIMD_EMIT_CTOR,
	SIMD_EMIT_CAST,
	SIMD_EMIT_SHUFFLE,
	SIMD_EMIT_SHIFT,
	SIMD_EMIT_LOAD_ALIGNED,
	SIMD_EMIT_STORE_ALIGNED
};

/*This is the size of the largest method name + 1 (to fit the ending \0). Align to 4 as well.*/
#define SIMD_INTRINSIC_NAME_MAX 22

typedef struct {
	const char name[SIMD_INTRINSIC_NAME_MAX];
	guint16 opcode;
	guint8 simd_emit_mode;
	guint8 flags;
} SimdIntrinsc;

/*
Missing:
setters
 */
static const SimdIntrinsc vector4f_intrinsics[] = {
	{ ".ctor", 0, SIMD_EMIT_CTOR },
	{ "AddSub", OP_ADDSUBPS, SIMD_EMIT_BINARY_SSE3 },
	{ "HorizontalAdd", OP_HADDPS, SIMD_EMIT_BINARY_SSE3 },
	{ "HorizontalSub", OP_HSUBPS, SIMD_EMIT_BINARY_SSE3 },	
	{ "InvSqrt", OP_RSQRTPS, SIMD_EMIT_UNARY },
	{ "LoadAligned", 0, SIMD_EMIT_LOAD_ALIGNED },
	{ "Max", OP_MAXPS, SIMD_EMIT_BINARY },
	{ "Min", OP_MINPS, SIMD_EMIT_BINARY },
	{ "Shuffle", 0, SIMD_EMIT_SHUFFLE },
	{ "Sqrt", OP_SQRTPS, SIMD_EMIT_UNARY },
	{ "StoreAligned", 0, SIMD_EMIT_STORE_ALIGNED },
	{ "get_W", 3, SIMD_EMIT_GETTER },
	{ "get_X", 0, SIMD_EMIT_GETTER },
	{ "get_Y", 1, SIMD_EMIT_GETTER },
	{ "get_Z", 2, SIMD_EMIT_GETTER },
	{ "op_Addition", OP_ADDPS, SIMD_EMIT_BINARY },
	{ "op_Division", OP_DIVPS, SIMD_EMIT_BINARY },
	{ "op_Explicit", 0, SIMD_EMIT_CAST }, 
	{ "op_Multiply", OP_MULPS, SIMD_EMIT_BINARY },
	{ "op_Subtraction", OP_SUBPS, SIMD_EMIT_BINARY },
};

/*
Missing:
A lot, revisit Vector4u.
 */
static const SimdIntrinsc vector4u_intrinsics[] = {
	{ "op_BitwiseAnd", OP_PAND, SIMD_EMIT_BINARY },
	{ "op_BitwiseOr", OP_POR, SIMD_EMIT_BINARY },
	{ "op_BitwiseXor", OP_PXOR, SIMD_EMIT_BINARY },
};

/*
Missing:
.ctor
getters
setters
 */
static const SimdIntrinsc vector8us_intrinsics[] = {
	{ "AddWithSaturation", OP_PADDW_SAT_UN, SIMD_EMIT_BINARY },
	{ "LoadAligned", 0, SIMD_EMIT_LOAD_ALIGNED },
	{ "ShiftRightArithmetic", OP_PSARW, SIMD_EMIT_SHIFT },
	{ "StoreAligned", 0, SIMD_EMIT_STORE_ALIGNED },
	{ "SubWithSaturation", OP_PSUBW_SAT_UN, SIMD_EMIT_BINARY },
	{ "UnpackHigh", OP_UNPACK_HIGHW, SIMD_EMIT_BINARY },
	{ "UnpackLow", OP_UNPACK_LOWW, SIMD_EMIT_BINARY },
	{ "op_Addition", OP_PADDW, SIMD_EMIT_BINARY },
	{ "op_BitwiseAnd", OP_PAND, SIMD_EMIT_BINARY },
	{ "op_BitwiseOr", OP_POR, SIMD_EMIT_BINARY },
	{ "op_BitwiseXor", OP_PXOR, SIMD_EMIT_BINARY },
	{ "op_Explicit", 0, SIMD_EMIT_CAST },
	{ "op_LeftShift", OP_PSHLW, SIMD_EMIT_SHIFT },
	{ "op_Multiply", OP_PMULW, SIMD_EMIT_BINARY },
	{ "op_RightShift", OP_PSHRW, SIMD_EMIT_SHIFT },
	{ "op_Subtraction", OP_PSUBW, SIMD_EMIT_BINARY },
};

/*
Missing:
.ctor
getters
setters
 */
static const SimdIntrinsc vector16b_intrinsics[] = {
	{ "AddWithSaturation", OP_PADDB_SAT_UN, SIMD_EMIT_BINARY },
	{ "LoadAligned", 0, SIMD_EMIT_LOAD_ALIGNED },
	{ "StoreAligned", 0, SIMD_EMIT_STORE_ALIGNED },
	{ "SubWithSaturation", OP_PSUBB_SAT_UN, SIMD_EMIT_BINARY },
	{ "UnpackHigh", OP_UNPACK_HIGHB, SIMD_EMIT_BINARY },
	{ "UnpackLow", OP_UNPACK_LOWB, SIMD_EMIT_BINARY },
	{ "op_Addition", OP_PADDB, SIMD_EMIT_BINARY },
	{ "op_BitwiseAnd", OP_PAND, SIMD_EMIT_BINARY },
	{ "op_BitwiseOr", OP_POR, SIMD_EMIT_BINARY },
	{ "op_BitwiseXor", OP_PXOR, SIMD_EMIT_BINARY },
	{ "op_Explicit", 0, SIMD_EMIT_CAST },
	{ "op_Subtraction", OP_PSUBB, SIMD_EMIT_BINARY },
};

static guint32 simd_supported_versions;

/*TODO match using number of parameters as well*/
static int
simd_intrinsic_compare_by_name (const void *key, const void *value)
{
	return strncmp(key, ((SimdIntrinsc *)value)->name, SIMD_INTRINSIC_NAME_MAX);
}

typedef enum {
	VREG_USED  				= 0x01,
	VREG_HAS_XZERO_BB0		= 0x02,
	VREG_HAS_OTHER_OP_BB0	= 0x04,
	VREG_SINGLE_BB_USE		= 0x08,
	VREG_MANY_BB_USE		= 0x10,
} KillFlags;

static inline int
get_ins_reg_by_idx (MonoInst *ins, int idx)
{
	switch (idx) {
	case 0: return ins->dreg;
	case 1: return ins->sreg1;
	case 2: return ins->sreg2;
	}
	return -1;
}

void
mono_simd_intrinsics_init (void)
{
	simd_supported_versions = mono_arch_cpu_enumerate_simd_versions ();
}
/*
This pass recalculate which vars need MONO_INST_INDIRECT.

We cannot do this for non SIMD vars since code like mono_get_vtable_var
uses MONO_INST_INDIRECT to signal that the variable must be stack allocated.
*/
void
mono_simd_simplify_indirection (MonoCompile *cfg)
{
	int i, max_vreg = 0;
	MonoBasicBlock *bb, *first_bb = NULL, **target_bb;
	MonoInst *ins;
	char * vreg_flags;

	for (i = 0; i < cfg->num_varinfo; i++) {
		MonoInst *var = cfg->varinfo [i];
		if (var->klass->simd_type) {
			// printf ("cleaning indirect flag for %d\n", var->dreg);
			var->flags &= ~MONO_INST_INDIRECT;
			max_vreg = MAX (var->dreg, max_vreg);
		}
	}

	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		if (!first_bb && bb->code)
			first_bb = bb;
		for (ins = bb->code; ins; ins = ins->next) {
			if (ins->opcode == OP_LDADDR) {
				MonoInst *var = (MonoInst*)ins->inst_p0;
				if (var->klass->simd_type) {
					var->flags |= MONO_INST_INDIRECT;
				}
			}
		}
	}

	DEBUG (printf ("[simd-simplify] max vreg is %d\n", max_vreg));
	vreg_flags = g_malloc0 (max_vreg + 1);
	target_bb = g_new0 (MonoBasicBlock*, max_vreg + 1);

	for (i = 0; i < cfg->num_varinfo; i++) {
		MonoInst *var = cfg->varinfo [i];
		if (var->klass->simd_type && !(var->flags & (MONO_INST_INDIRECT|MONO_INST_VOLATILE))) {
			vreg_flags [var->dreg] = VREG_USED;
			DEBUG (printf ("[simd-simplify] processing var %d with vreg %d\n", i, var->dreg));
		}
	}

	/*Scan the first basic block looking xzeros not used*/
	for (ins = first_bb->code; ins; ins = ins->next) {
		if (ins->opcode == OP_XZERO) {
			if (!(vreg_flags [ins->dreg] & VREG_HAS_OTHER_OP_BB0)) {
				DEBUG (printf ("[simd-simplify] R%d has vzero: ", ins->dreg); mono_print_ins(ins));
				vreg_flags [ins->dreg] |= VREG_HAS_XZERO_BB0;
			}
			continue;
		}
		for (i = 0; i < 3; ++i) {
			int reg = get_ins_reg_by_idx (ins, i);
			if (reg != -1 && reg <= max_vreg && vreg_flags [reg]) {
				vreg_flags [reg] &= ~VREG_HAS_XZERO_BB0;
				vreg_flags [reg] |= VREG_HAS_OTHER_OP_BB0;
				DEBUG (printf ("[simd-simplify] R%d used: ", reg); mono_print_ins(ins));
			}
		}
	}

	if (IS_DEBUG_ON (cfg)) {
		for (i = 0; i < cfg->num_varinfo; i++) {
			MonoInst *var = cfg->varinfo [i];
			if (var->klass->simd_type) {
				if ((vreg_flags [var->dreg] & VREG_HAS_XZERO_BB0))
					DEBUG (printf ("[simd-simplify] R%d has xzero only\n", var->dreg));
				if ((vreg_flags [var->dreg] & VREG_HAS_OTHER_OP_BB0))
					DEBUG (printf ("[simd-simplify] R%d has other ops on bb0\n", var->dreg));
			}
		}
	}

	/*TODO stop here if no var is xzero only*/

	/*
	Scan all other bb and check if it has only one other use
	Ideally this would be done after an extended bb formation pass

	FIXME This pass could use dominator information to properly
	place the XZERO on the bb that dominates all uses of the var,
	but this will have zero effect with the current local reg alloc 
	
	TODO simply the use of flags.
	*/

	for (bb = first_bb->next_bb; bb; bb = bb->next_bb) {
		for (ins = bb->code; ins; ins = ins->next) {
			for (i = 0; i < 3; ++i) {
				int reg = get_ins_reg_by_idx (ins, i);
				if (reg == -1 || reg > max_vreg || !(vreg_flags [reg] & VREG_HAS_XZERO_BB0) || target_bb [reg] == bb)
					continue;

				if (vreg_flags [reg] & VREG_SINGLE_BB_USE) {
					vreg_flags [reg] &= ~VREG_SINGLE_BB_USE;
					vreg_flags [reg] |= VREG_MANY_BB_USE;
					DEBUG (printf ("[simd-simplify] R%d used by many bb: ", reg); mono_print_ins(ins));
					break;
				} else if (!(vreg_flags [reg] & VREG_MANY_BB_USE)) {
					vreg_flags [reg] |= VREG_SINGLE_BB_USE;
					target_bb [reg] = bb;
					DEBUG (printf ("[simd-simplify] R%d first used by: ", reg); mono_print_ins(ins));
					break;
				}
			}
		}
	}

	for (i = 0; i < cfg->num_varinfo; i++) {
		MonoInst *var = cfg->varinfo [i];
		if (!var->klass->simd_type)
			continue;
		if ((vreg_flags [var->dreg] & VREG_SINGLE_BB_USE))
			DEBUG (printf ("[simd-simplify] R%d has single bb use\n", var->dreg));
		if ((vreg_flags [var->dreg] & VREG_MANY_BB_USE))
			DEBUG (printf ("[simd-simplify] R%d has many bb in use\n", var->dreg));

		if (!(vreg_flags [var->dreg] & VREG_SINGLE_BB_USE))
			continue;
		for (ins = target_bb [var->dreg]->code; ins; ins = ins->next) {
			/*We can, pretty much kill it.*/
			if (ins->dreg == var->dreg) {
				break;
			} else if (ins->sreg1 == var->dreg || ins->sreg2 == var->dreg) {
				MonoInst *tmp;
				MONO_INST_NEW (cfg, tmp, OP_XZERO);
				tmp->dreg = var->dreg;
				tmp->type = STACK_VTYPE;
		        tmp->klass = var->klass;
				mono_bblock_insert_before_ins (target_bb [var->dreg], ins, tmp);
				break;
			}
		}
	}

	for (ins = first_bb->code; ins; ins = ins->next) {
		if (ins->opcode == OP_XZERO && (vreg_flags [ins->dreg] & VREG_SINGLE_BB_USE))
			NULLIFY_INS (ins);
	}

	g_free (vreg_flags);
	g_free (target_bb);
}

static int
get_simd_vreg (MonoCompile *cfg, MonoMethod *cmethod, MonoInst *src, gboolean is_this_ptr)
{
	if (src->opcode == OP_XMOVE) {
		/*FIXME returning src->sreg1 breaks during regalloc */
		return src->dreg;
	} else if (src->opcode == OP_LDADDR && is_this_ptr) {
		int res = ((MonoInst*)src->inst_p0)->dreg;
		NULLIFY_INS (src);
		return res;
	} else if (src->opcode == OP_LOADX_MEMBASE) {
		return src->dreg;
	} else if (src->klass && src->klass->simd_type) {
		return src->dreg;
	}
	g_warning ("get_simd_vreg:: could not infer source simd vreg for op");
	mono_print_ins (src);
	g_assert_not_reached ();
}

static MonoInst*
get_int_to_float_spill_area (MonoCompile *cfg)
{
	if (!cfg->iconv_raw_var) {
		cfg->iconv_raw_var = mono_compile_create_var (cfg, &mono_defaults.int32_class->byval_arg, OP_LOCAL);
		cfg->iconv_raw_var->flags |= MONO_INST_VOLATILE; /*FIXME, use the don't regalloc flag*/
	}	
	return cfg->iconv_raw_var;
}

static MonoInst*
simd_intrinsic_emit_binary (const SimdIntrinsc *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoInst* ins;
	int left_vreg, right_vreg;

	left_vreg = get_simd_vreg (cfg, cmethod, args [0], FALSE);
	right_vreg = get_simd_vreg (cfg, cmethod, args [1], FALSE);
	

	MONO_INST_NEW (cfg, ins, intrinsic->opcode);
	ins->klass = cmethod->klass;
	ins->sreg1 = left_vreg;
	ins->sreg2 = right_vreg;
	ins->type = STACK_VTYPE;
	ins->klass = cmethod->klass;
	ins->dreg = alloc_ireg (cfg);
	MONO_ADD_INS (cfg->cbb, ins);
	return ins;
}

static MonoInst*
simd_intrinsic_emit_unary (const SimdIntrinsc *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoInst* ins;
	int vreg;
	
	vreg = get_simd_vreg (cfg, cmethod, args [0], FALSE);

	MONO_INST_NEW (cfg, ins, intrinsic->opcode);
	ins->klass = cmethod->klass;
	ins->sreg1 = vreg;
	ins->type = STACK_VTYPE;
	ins->dreg = alloc_ireg (cfg);
	MONO_ADD_INS (cfg->cbb, ins);
	return ins;
}

static MonoInst*
simd_intrinsic_emit_getter (const SimdIntrinsc *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoInst *tmp, *ins;
	int vreg;
	
	vreg = get_simd_vreg (cfg, cmethod, args [0], TRUE);

	if (intrinsic->opcode) {
		MONO_INST_NEW (cfg, ins, OP_SHUFLEPS);
		ins->klass = cmethod->klass;
		ins->sreg1 = vreg;
		ins->inst_c0 = intrinsic->opcode;
		ins->type = STACK_VTYPE;
		ins->dreg = vreg = alloc_ireg (cfg);
		MONO_ADD_INS (cfg->cbb, ins);
	}

	MONO_INST_NEW (cfg, tmp, OP_EXTRACT_I4);
	tmp->klass = cmethod->klass;
	tmp->sreg1 = vreg;
	tmp->type = STACK_I4;
	tmp->dreg = alloc_ireg (cfg);
	MONO_ADD_INS (cfg->cbb, tmp);

	MONO_INST_NEW (cfg, ins, OP_ICONV_TO_R8_RAW);
	ins->klass = mono_defaults.single_class;
	ins->sreg1 = tmp->dreg;
	ins->type = STACK_R8;
	ins->dreg = alloc_freg (cfg);
	ins->backend.spill_var = get_int_to_float_spill_area (cfg);
	MONO_ADD_INS (cfg->cbb, ins);	
	return ins;
}

static MonoInst*
simd_intrinsic_emit_ctor (const SimdIntrinsc *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoInst *ins;
	int i;

	for (i = 1; i < 5; ++i) {
		MONO_INST_NEW (cfg, ins, OP_PUSH_R4);
		ins->sreg1 = args [5 - i]->dreg;
		ins->klass = args [5 - i]->klass;
		MONO_ADD_INS (cfg->cbb, ins);
	}

	/*TODO replace with proper LOAD macro */
	MONO_INST_NEW (cfg, ins, OP_LOADX_STACK);
	ins->klass = cmethod->klass;
	ins->type = STACK_VTYPE;
	ins->dreg = get_simd_vreg (cfg, cmethod, args [0], TRUE);
	MONO_ADD_INS (cfg->cbb, ins);

	return ins;
}

static MonoInst*
simd_intrinsic_emit_cast (const SimdIntrinsc *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoInst *ins;
	int vreg;

	vreg = get_simd_vreg (cfg, cmethod, args [0], FALSE);		

	//TODO macroize this
	MONO_INST_NEW (cfg, ins, OP_XMOVE);
	ins->klass = cmethod->klass;
	ins->type = STACK_VTYPE;
	ins->sreg1 = vreg;
	ins->dreg = alloc_ireg (cfg);
	MONO_ADD_INS (cfg->cbb, ins);
	return ins;
}

static MonoInst*

simd_intrinsic_emit_shift (const SimdIntrinsc *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoInst *ins;
	int vreg, vreg2 = -1, opcode = intrinsic->opcode;

	vreg = get_simd_vreg (cfg, cmethod, args [0], FALSE);

	if (args [1]->opcode != OP_ICONST) {
		MONO_INST_NEW (cfg, ins, OP_ICONV_TO_X);
		ins->klass = mono_defaults.int32_class;
		ins->sreg1 = args [1]->dreg;
		ins->type = STACK_I4;
		ins->dreg = vreg2 = alloc_ireg (cfg);
		MONO_ADD_INS (cfg->cbb, ins);

		++opcode; /*The shift_reg version op is always +1 from the regular one.*/
	}

	MONO_INST_NEW (cfg, ins, opcode);
	ins->klass = cmethod->klass;
	ins->sreg1 = vreg;
	ins->sreg2 = vreg2;

	if (args [1]->opcode == OP_ICONST) {
		ins->inst_imm = args [1]->inst_c0;
		NULLIFY_INS (args [1]);
	}

	ins->type = STACK_VTYPE;
	ins->dreg = alloc_ireg (cfg);
	MONO_ADD_INS (cfg->cbb, ins);
	return ins;
}


static MonoInst*
simd_intrinsic_emit_shuffle (const SimdIntrinsc *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoInst *ins;
	int vreg;

	/*TODO Exposing shuffle is not a good thing as it's non obvious. We should come up with better abstractions*/

	if (args [1]->opcode != OP_ICONST) {
		g_warning ("Vector4f:Shuffle with non literals is not yet supported");
		g_assert_not_reached ();
	}
	vreg = get_simd_vreg (cfg, cmethod, args [0], FALSE);
	NULLIFY_INS (args [1]);

	MONO_INST_NEW (cfg, ins, OP_SHUFLEPS);
	ins->klass = cmethod->klass;
	ins->sreg1 = vreg;
	ins->inst_c0 = args [1]->inst_c0;
	ins->type = STACK_VTYPE;
	ins->dreg = alloc_ireg (cfg);
	MONO_ADD_INS (cfg->cbb, ins);
	return ins;
}

static MonoInst*
simd_intrinsic_emit_load_aligned (const SimdIntrinsc *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoInst *ins;

	MONO_INST_NEW (cfg, ins, OP_LOADX_ALIGNED_MEMBASE);
	ins->klass = cmethod->klass;
	ins->sreg1 = args [0]->dreg;
	/*FIXME, shouldn't use use ->inst_offset?*/
	ins->type = STACK_VTYPE;
	ins->dreg = alloc_ireg (cfg);
	MONO_ADD_INS (cfg->cbb, ins);
	return ins;
}

static MonoInst*
simd_intrinsic_emit_store_aligned (const SimdIntrinsc *intrinsic, MonoCompile *cfg, MonoMethod *cmethod, MonoInst **args)
{
	MonoInst *ins;
	int vreg;

	vreg = get_simd_vreg (cfg, cmethod, args [0], FALSE);

	MONO_INST_NEW (cfg, ins, OP_STOREX_ALIGNED_MEMBASE_REG);
	ins->klass = cmethod->klass;
	ins->dreg = args [0]->dreg;
	ins->inst_offset = args [0]->inst_offset;
	ins->sreg1 = vreg;
	ins->type = STACK_VTYPE;
	MONO_ADD_INS (cfg->cbb, ins);
	return ins;
}


static MonoInst*
emit_intrinsics (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args, const SimdIntrinsc *intrinsics, guint32 size)
{
	const SimdIntrinsc * result = bsearch (cmethod->name, intrinsics, size, sizeof (SimdIntrinsc), &simd_intrinsic_compare_by_name);
	if (!result) {
		DEBUG (printf ("function doesn't have a simd intrinsic %s::%s/%d\n", cmethod->klass->name, cmethod->name, fsig->param_count));
		return NULL;
	}
	if (IS_DEBUG_ON (cfg)) {
		int i, max;
		printf ("found call to intrinsic %s::%s/%d -> %s\n", cmethod->klass->name, cmethod->name, fsig->param_count, result->name);
		max = fsig->param_count + fsig->hasthis;
		for (i = 0; i < max; ++i) {
			printf ("param %d:  ", i);
			mono_print_ins (args [i]);
		}
	}

	switch (result->simd_emit_mode) {
	case SIMD_EMIT_BINARY_SSE3:
		if (simd_supported_versions & SIMD_VERSION_SSE3)
			return simd_intrinsic_emit_binary (result, cfg, cmethod, args);
		return NULL;
	case SIMD_EMIT_BINARY:
		return simd_intrinsic_emit_binary (result, cfg, cmethod, args);
	case SIMD_EMIT_UNARY:
		return simd_intrinsic_emit_unary (result, cfg, cmethod, args);
	case SIMD_EMIT_GETTER:
		return simd_intrinsic_emit_getter (result, cfg, cmethod, args);
	case SIMD_EMIT_CTOR:
		return simd_intrinsic_emit_ctor (result, cfg, cmethod, args);
	case SIMD_EMIT_CAST:
		return simd_intrinsic_emit_cast (result, cfg, cmethod, args);
	case SIMD_EMIT_SHUFFLE:
		return simd_intrinsic_emit_shuffle (result, cfg, cmethod, args); 
	case SIMD_EMIT_SHIFT:
		return simd_intrinsic_emit_shift (result, cfg, cmethod, args);
	case SIMD_EMIT_LOAD_ALIGNED:
		return simd_intrinsic_emit_load_aligned (result, cfg, cmethod, args);
	case SIMD_EMIT_STORE_ALIGNED:
		return simd_intrinsic_emit_store_aligned (result, cfg, cmethod, args);
	}
	g_assert_not_reached ();
}

MonoInst*
mono_emit_simd_intrinsics (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	if (!cmethod->klass->simd_type)
		return NULL;
	cfg->uses_simd_intrinsics = 1;
	if (!strcmp ("Vector4f", cmethod->klass->name))
		return emit_intrinsics (cfg, cmethod, fsig, args, vector4f_intrinsics, sizeof (vector4f_intrinsics) / sizeof (SimdIntrinsc));
	if (!strcmp ("Vector4ui", cmethod->klass->name))
		return emit_intrinsics (cfg, cmethod, fsig, args, vector4u_intrinsics, sizeof (vector4u_intrinsics) / sizeof (SimdIntrinsc));
	if (!strcmp ("Vector8us", cmethod->klass->name))
		return emit_intrinsics (cfg, cmethod, fsig, args, vector8us_intrinsics, sizeof (vector8us_intrinsics) / sizeof (SimdIntrinsc));
	if (!strcmp ("Vector16b", cmethod->klass->name))
		return emit_intrinsics (cfg, cmethod, fsig, args, vector16b_intrinsics, sizeof (vector16b_intrinsics) / sizeof (SimdIntrinsc));
	return NULL;
}

#endif
