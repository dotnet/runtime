/*
 * regalloc2.c: Global Register Allocator
 *
 * Author:
 *   Zoltan Varga (vargaz@gmail.com)
 *
 * (C) 2007 Novell, Inc.
 */

#include "mini.h"
#include "ir-emit.h"
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/mempool-internals.h>

/* Disable for now to save space */
//#undef MONO_ARCH_ENABLE_GLOBAL_RA

#ifdef MONO_ARCH_ENABLE_GLOBAL_RA

/*
 * Documentation
 *
 *   This allocator is based on the paper 
 * "Linear Scan Register Allocation for the Java HotSpot Client Compiler"
 * by Christian Wimmer.
 *
 * It differs from the current allocator in the following respects:
 * - all variables (vregs) are allocated a register, there is no separate local/global
 *   register allocator.
 * - there are no variables allocated strictly to the stack. Even those variables 
 *   allocated to the stack are loaded into a register before they are accessed.
 */

/*
 * Current status:
 *
 * - Only works on amd64.
 * - Tests in mono/mini and mono/tests work, "Hello, World!" works.
 * - The quality of generated code is not bad, but needs more optimizations.
 * - Focus was on correctness and easy debuggability so performance is bad, especially on
 *   large methods (like Main in transparentproxy.exe). Since each interval can be
 *   split at each use position, run time is linear in the number of variable accesses, 
 *   not the number of variables.
 * - Have to think about splitting the available registers between caller saved and callee
 *   saved, and take this into account during allocation (callee saved - good for 
 *   variables which are accessed a lot, and/or are live across calls, caller saved -
 *   good for scratch registers used only in a bblock and not live across calls).
 * - FIXME: Fix mono_arch_get_ireg_clobbered_by_call () to only return caller saved
 *   registers.
 */

/*
 * TYPES
 */
typedef struct MonoRegallocInterval {
	/*
	 * 0..MONO_MAX_IREGS - iregs
	 * MONO_MAX_IREGS + 1...MONO_MAX_IREGS+MONO_MAX_FREGS - fregs
	 * MONO_MAX_IREGS+MONO_MAX_FREGS... cfg->next_vreg - vregs
     * cfg->next_vreg... - split intervals
	 */
	int vreg;
	/*
	 * Hard register assigned to this interval, -1 if no register is assigned or the
	 * interval is spilled.
	 */
	int hreg;

	/*
	 * The actual interval data
	 */
	MonoLiveInterval *interval;

	/*
	 * Split children of this interval. NULL if the interval is not split.
	 */
	struct MonoRegallocInterval *child1, *child2;

	/*
	 * List of use positions, each position is identified by (bb->dfn << 16) + ins_pos
	 * The list is sorted by increasing position.
	 */
	GSList *use_pos;

	/*
	 * The offset relative to the frame pointer where this interval is spilled.
	 */
	int offset;

	/*
	 * If we are a split child of an interval, this points to our parent
	 */
	struct MonoRegallocInterval *parent;

	/*
	 * Whenever vreg is an fp vreg
	 */
	guint fp : 1;

	/*
	 * Whenever the variable is volatile
	 */
	guint is_volatile : 1;

	/*
	 * The exact type of the variable
	 */
	MonoType *type;

	/*
	 * The register where this interval should be allocated
	 */
	int preferred_reg;
} MonoRegallocInterval;

typedef struct MonoRegallocContext {
	MonoCompile *cfg;
	MonoRegallocInterval *varinfo;
	int num_intervals;
	/* 
	 * Maps ins pos -> GSList of intervals split at that pos.
	 */
	GHashTable *split_positions;
	/*
	 * Used to avoid lookups in split_positions for every position.
	 */
	GHashTable *split_position_set;
	/*
	 * Contains MonoInst's representing spill loads/stores
	 */
	GHashTable *spill_ins;
} MonoRegallocContext;

/*
 * MACROS
 */

#define BITS_PER_CHUNK MONO_BITSET_BITS_PER_CHUNK
#define MONO_FIRST_VREG (MONO_MAX_IREGS+MONO_MAX_FREGS)

/* 
 * Each instruction is allocated 4 liveness phases:
 * 0 - USE  - the instruction reads its input registers in this phase
 * 1 - CLOB   - the instruction clobbers some registers in this phase
 * 2 - DEF - the instruction writes its output register(s) in this phase
 * 3 - SPILL  - spill code 
 * In liveness ranges, the start position of the range is the DEF position of the 
 * instruction which defines the vreg.
 */

#define INS_POS_USE 0
#define INS_POS_CLOB 1
#define INS_POS_DEF 2
#define INS_POS_SPILL 3

/* 
 * Should use 16 so liveness ranges are easier to read, but that would overflow
 * on big bblocks.
 */
#define INS_POS_INTERVAL 8

#define is_hard_reg(r,fp) ((fp) ? ((r) < MONO_MAX_FREGS) : ((r) < MONO_MAX_IREGS))
#define is_soft_reg(r,fp) (!is_hard_reg((r),(fp)))

#ifdef MONO_ARCH_INST_IS_FLOAT
#define dreg_is_fp(spec)  (MONO_ARCH_INST_IS_FLOAT (spec [MONO_INST_DEST]))
#define sreg1_is_fp(spec) (MONO_ARCH_INST_IS_FLOAT (spec [MONO_INST_SRC1]))
#define sreg2_is_fp(spec) (MONO_ARCH_INST_IS_FLOAT (spec [MONO_INST_SRC2]))
#else
#define sreg1_is_fp(spec) (spec [MONO_INST_SRC1] == 'f')
#define sreg2_is_fp(spec) (spec [MONO_INST_SRC2] == 'f')
#define dreg_is_fp(spec)  (spec [MONO_INST_DEST] == 'f')
#endif

/* 
 * Get the base ins position from an ins pos.
 * FIXME: This shouldn't be required but some parts of the code can't seem to
 * handle use positions which have an INS_POS_DEF added.
 */
#define USE_POS_BASE(ins_pos) ((ins_pos) & ~(INS_POS_INTERVAL - 1))

#define USE_POS_IS_DEF(ins_pos) ((ins_pos) & INS_POS_DEF)

static MonoInst*
create_move (MonoCompile *cfg, int dreg, int sreg)
{
	MonoInst *ins;

	MONO_INST_NEW (cfg, ins, OP_MOVE);
	ins->dreg = dreg;
	ins->sreg1 = sreg;

	return ins;
}

static MonoInst*
create_fp_move (MonoCompile *cfg, int dreg, int sreg)
{
	MonoInst *ins;

	MONO_INST_NEW (cfg, ins, OP_FMOVE);
	ins->dreg = dreg;
	ins->sreg1 = sreg;

	return ins;
}

static void
emit_move (MonoCompile *cfg, int dreg, int sreg, MonoInst *insert_after)
{
	MonoInst *ins = create_move (cfg, dreg, sreg);

	mono_bblock_insert_after_ins (cfg->cbb, insert_after, ins);
}

static void
emit_fp_move (MonoCompile *cfg, int dreg, int sreg, MonoInst *insert_after)
{
	MonoInst *ins = create_fp_move (cfg, dreg, sreg);

	mono_bblock_insert_after_ins (cfg->cbb, insert_after, ins);
}

static void
emit_nop (MonoCompile *cfg, MonoInst *insert_after)
{
	MonoInst *ins;

	MONO_INST_NEW (cfg, ins, OP_NOP);

	mono_bblock_insert_after_ins (cfg->cbb, insert_after, ins);	
}

/**
 * handle_reg_constraints:
 *
 *   Rewrite the IR so it satisfies the register constraints of the architecture.
 */
static void
handle_reg_constraints (MonoCompile *cfg)
{
	MonoMethodSignature *sig;
	MonoBasicBlock *bb;
	int i;

	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoInst *ins;
		MonoInst *prev = NULL;

		if (cfg->verbose_level > 1) mono_print_bb (bb, "BEFORE HANDLE-REG-CONSTRAINTS ");

		cfg->cbb = bb;
		MONO_BB_FOR_EACH_INS (bb, ins) {
			const char *spec = ins_get_spec (ins->opcode);
			int dest_sreg1, dest_sreg2, dest_dreg;

			dest_sreg1 = MONO_ARCH_INST_FIXED_REG (spec [MONO_INST_SRC1]);
			dest_sreg2 = MONO_ARCH_INST_FIXED_REG (spec [MONO_INST_SRC2]);
			dest_dreg = MONO_ARCH_INST_FIXED_REG (spec [MONO_INST_DEST]);

			if (MONO_ARCH_INST_IS_REGPAIR (spec [MONO_INST_DEST]) ||
				MONO_ARCH_INST_IS_REGPAIR (spec [MONO_INST_SRC1]) ||
				MONO_ARCH_INST_IS_REGPAIR (spec [MONO_INST_SRC2]))
				/* FIXME: */
				g_assert_not_reached ();

			if (spec [MONO_INST_CLOB] == 'c') {
				MonoCallInst *call = (MonoCallInst*)ins;
				GSList *list;

				/*
				 * FIXME: mono_arch_emit_call () already adds moves for each argument,
				 * it might be better to rewrite those by changing the dreg to the hreg.
				 */
				for (list = call->out_ireg_args; list; list = list->next) {
					guint32 regpair;
					int reg, hreg;
					MonoInst *move;

					regpair = (guint32)(gssize)(list->data);
					hreg = regpair >> 24;
					reg = regpair & 0xffffff;

					move = create_move (cfg, hreg, reg);
					mono_bblock_insert_after_ins (bb, prev, move);
					prev = move;
				}

				for (list = call->out_freg_args; list; list = list->next) {
					guint32 regpair;
					int reg, hreg;
					MonoInst *move;

					regpair = (guint32)(gssize)(list->data);
					hreg = regpair >> 24;
					reg = regpair & 0xffffff;

					move = create_fp_move (cfg, hreg + MONO_MAX_IREGS, reg);
					mono_bblock_insert_after_ins (bb, prev, move);
					prev = move;
				}
			}

			if (spec [MONO_INST_CLOB] == '1') {
				/* Copying sreg1 to dreg could clobber sreg2 so make a copy of sreg2 */
				if (spec [MONO_INST_SRC2] && (ins->dreg == ins->sreg2)) {
					int new_sreg2 = mono_alloc_preg (cfg);
					MonoInst *move;
					g_assert (spec [MONO_INST_DEST] != 'f');
					move = create_move (cfg, new_sreg2, ins->sreg2);
					mono_bblock_insert_after_ins (bb, prev, move);
					prev = move;
					ins->sreg2 = new_sreg2;
				}
				if (spec [MONO_INST_DEST] == 'f')
					emit_fp_move (cfg, ins->dreg, ins->sreg1, prev);
				else
					emit_move (cfg, ins->dreg, ins->sreg1, prev);
				ins->sreg1 = ins->dreg;
			}

			if (dest_sreg1 != -1) {
				emit_move (cfg, dest_sreg1, ins->sreg1, prev);
				ins->sreg1 = dest_sreg1;
			}

			if (dest_sreg2 != -1) {
				emit_move (cfg, dest_sreg2, ins->sreg2, prev);
				ins->sreg2 = dest_sreg2;
			}

			if (dest_dreg != -1) {
				emit_move (cfg, ins->dreg, dest_dreg, ins);
				g_assert (spec [MONO_INST_CLOB] != '1');
				ins->dreg = dest_dreg;
			}				

			/* FIXME: Add fixed fp regs to the machine description */
			if (ins->opcode == OP_FCALL || ins->opcode == OP_FCALL_REG || ins->opcode == OP_FCALL_MEMBASE) {
				emit_fp_move (cfg, ins->dreg, MONO_MAX_IREGS + MONO_ARCH_FP_RETURN_REG, ins);
				ins->dreg = MONO_MAX_IREGS + MONO_ARCH_FP_RETURN_REG;
			}

			/*
			 * Add a dummy instruction after each definition of a volatile vreg, this is
			 * needed by the code in decompose_volatile_intervals ().
			 */
			if (get_vreg_to_inst (cfg, ins->dreg) && (get_vreg_to_inst (cfg, ins->dreg)->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT))) {
				emit_nop (cfg, ins);
			}

			prev = ins;
		}

		sig = mono_method_signature (cfg->method);

		/* Add move of arguments */
		/* 
		 * FIXME: Maybe this should be done by the allocator.
		 */
		if (bb == cfg->bb_entry) {
			MonoType *arg_type;

			prev = NULL;
			if (cfg->vret_addr) {
				g_assert (cfg->vret_addr->opcode == OP_REGVAR);
				emit_move (cfg, cfg->vret_addr->dreg, cfg->vret_addr->inst_c0, prev);
				prev = bb->code;
			}

			for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
				ins = cfg->args [i];

				if (sig->hasthis && (i == 0))
					arg_type = &mono_defaults.object_class->byval_arg;
				else
					arg_type = sig->params [i - sig->hasthis];

				// FIXME: vtypes in registers (pass + return)

				if (ins->opcode == OP_REGVAR) {
					if (!arg_type->byref && ((arg_type->type == MONO_TYPE_R4) || (arg_type->type == MONO_TYPE_R8)))
						/* For R4, the prolog is assumed to do the conversion */
						emit_fp_move (cfg, ins->dreg, ins->inst_c0 + MONO_MAX_IREGS, prev);
					else
						emit_move (cfg, ins->dreg, ins->inst_c0, prev);
				}

				prev = bb->code;
			}
		}

		/* Add move of return value */
		for (i = 0; i < bb->out_count; ++i) {
			/* bb->dfn == 0 -> unreachable */
			if (cfg->ret && !cfg->vret_addr && !MONO_TYPE_ISSTRUCT (sig->ret) && bb->out_bb [i] == cfg->bb_exit && bb->dfn) {
				MonoInst *ins = NULL;
				int hreg;

				hreg = cfg->ret->inst_c0;

				if ((sig->ret->type == MONO_TYPE_R4) || (sig->ret->type == MONO_TYPE_R8))
					/* For R4, the JIT has already emitted code to do the conversion */
					ins = create_fp_move (cfg, hreg + MONO_MAX_IREGS, cfg->ret->dreg);
				else
					ins = create_move (cfg, hreg, cfg->ret->dreg);
				mono_add_ins_to_end (bb, ins);
			}
		}

		if (cfg->verbose_level > 1) mono_print_bb (bb, "AFTER HANDLE-REG-CONSTRAINTS ");
	}

	mono_verify_cfg (cfg);
}

/*
 * collect_fp_vregs:
 *
 *   Set varinfo->fp for all float vregs
 */
static void
collect_fp_vregs (MonoCompile *cfg, MonoRegallocContext *ctx)
{
	MonoBasicBlock *bb;

	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoInst *ins;

		MONO_BB_FOR_EACH_INS (bb, ins) {
			const char *spec = ins_get_spec (ins->opcode);

			if (G_UNLIKELY (sreg1_is_fp (spec) || sreg2_is_fp (spec) || dreg_is_fp (spec))) {
				if (sreg1_is_fp (spec)) {
					g_assert (ins->sreg1 >= MONO_MAX_IREGS);
					ctx->varinfo [ins->sreg1].fp = TRUE;
					if (ctx->varinfo [ins->sreg1].type->type != MONO_TYPE_R4)
						ctx->varinfo [ins->sreg1].type = &mono_defaults.double_class->byval_arg;
				}
				if (sreg2_is_fp (spec)) {
					g_assert (ins->sreg2 >= MONO_MAX_IREGS);
					ctx->varinfo [ins->sreg2].fp = TRUE;
					if (ctx->varinfo [ins->sreg2].type->type != MONO_TYPE_R4)
						ctx->varinfo [ins->sreg2].type = &mono_defaults.double_class->byval_arg;
				}
				if (dreg_is_fp (spec)) {
					g_assert (ins->dreg >= MONO_MAX_IREGS);
					ctx->varinfo [ins->dreg].fp = TRUE;
					if (ctx->varinfo [ins->dreg].type->type != MONO_TYPE_R4)
						ctx->varinfo [ins->dreg].type = &mono_defaults.double_class->byval_arg;
				}
			}
		}
	}
}

#if 1
#define LIVENESS_DEBUG(a) do { if (cfg->verbose_level > 2) { a; } } while (0)
#else
#define LIVENESS_DEBUG(a)
#endif

// #define DEBUG_LIVENESS 1

G_GNUC_UNUSED static void
mono_bitset_print (MonoBitSet *set)
{
	int i;

	printf ("{");
	for (i = 0; i < mono_bitset_size (set); i++) {

		if (mono_bitset_test (set, i))
			printf ("%d, ", i);

	}
	printf ("}\n");
}

static inline void
update_gen_kill_set (MonoCompile *cfg, MonoRegallocContext *ctx, MonoBasicBlock *bb, MonoInst *ins)
{
	const char *spec = INS_INFO (ins->opcode);
	int sreg;

	/* SREG1 */
	sreg = ins->sreg1;
	if (spec [MONO_INST_SRC1] != ' ') {
		if (!mono_bitset_test_fast (bb->kill_set, sreg))
			mono_bitset_set_fast (bb->gen_set, sreg);
	}

	/* SREG2 */
	sreg = ins->sreg2;
	if (spec [MONO_INST_SRC2] != ' ') {
		if (!mono_bitset_test_fast (bb->kill_set, sreg))
			mono_bitset_set_fast (bb->gen_set, sreg);
	}

	/* DREG */
	if (spec [MONO_INST_DEST] != ' ') {
		if (MONO_IS_STORE_MEMBASE (ins)) {
			if (!mono_bitset_test_fast (bb->kill_set, ins->dreg))
				mono_bitset_set_fast (bb->gen_set, ins->dreg);
		} else {
			mono_bitset_set_fast (bb->kill_set, ins->dreg);
		}
	}
}

static void
compute_gen_kill_sets (MonoCompile *cfg, MonoRegallocContext *ctx)
{
	int i, max_vars = cfg->next_vreg;
	int bitsize;
	guint8 *mem;

	bitsize = mono_bitset_alloc_size (max_vars, 0);
	mem = mono_mempool_alloc0 (cfg->mempool, cfg->num_bblocks * bitsize * 3);

	for (i = 0; i < cfg->num_bblocks; ++i) {
		MonoBasicBlock *bb = cfg->bblocks [i];

		bb->gen_set = mono_bitset_mem_new (mem, max_vars, MONO_BITSET_DONT_FREE);
		mem += bitsize;
		bb->kill_set = mono_bitset_mem_new (mem, max_vars, MONO_BITSET_DONT_FREE);
		mem += bitsize;
		/* Initialized later */
		bb->live_in_set = NULL;
		bb->live_out_set = mono_bitset_mem_new (mem, max_vars, MONO_BITSET_DONT_FREE);
		mem += bitsize;
	}

	for (i = 0; i < cfg->num_bblocks; ++i) {
		MonoBasicBlock *bb = cfg->bblocks [i];
		MonoInst *ins;
#ifdef DEBUG_LIVENESS
		int j;
#endif

		MONO_BB_FOR_EACH_INS (bb, ins)
			update_gen_kill_set (cfg, ctx, bb, ins);

#ifdef DEBUG_LIVENESS
		printf ("BLOCK BB%d (", bb->block_num);
		for (j = 0; j < bb->out_count; j++) 
			printf ("BB%d, ", bb->out_bb [j]->block_num);
		
		printf (")\n");
		printf ("GEN  BB%d: ", bb->block_num); mono_bitset_print (bb->gen_set);
		printf ("KILL BB%d: ", bb->block_num); mono_bitset_print (bb->kill_set);
#endif
	}

	if (cfg->ret && cfg->ret->opcode == OP_REGVAR) {
		int hreg = cfg->ret->inst_c0;

		/* gen_set might be empty if bb_exit is not reachable, like when using a tail call */
		if (cfg->bb_exit->gen_set)
			mono_bitset_set (cfg->bb_exit->gen_set, hreg);
	}
}

static void
compute_live_in_out_sets (MonoCompile *cfg, MonoRegallocContext *ctx)
{
	MonoBitSet *old_live_out_set;
	int i, j, max_vars = cfg->next_vreg;
	int out_iter;
	gboolean *in_worklist;
	MonoBasicBlock **worklist;
	guint32 l_end;
	int bitsize;
	guint8 *mem;

	bitsize = mono_bitset_alloc_size (max_vars, 0);
	mem = mono_mempool_alloc0 (cfg->mempool, cfg->num_bblocks * bitsize);

	old_live_out_set = mono_bitset_new (max_vars, 0);
	in_worklist = g_new0 (gboolean, cfg->num_bblocks + 1);

	worklist = g_new (MonoBasicBlock *, cfg->num_bblocks + 1);
	l_end = 0;

	/*
	 * This is a backward dataflow analysis problem, so we process blocks in
	 * decreasing dfn order, this speeds up the iteration.
	 */
	for (i = 0; i < cfg->num_bblocks; i ++) {
		MonoBasicBlock *bb = cfg->bblocks [i];

		worklist [l_end ++] = bb;
		in_worklist [bb->dfn] = TRUE;
	}

	out_iter = 0;

	while (l_end != 0) {
		MonoBasicBlock *bb = worklist [--l_end];
		MonoBasicBlock *out_bb;
		gboolean changed;

		in_worklist [bb->dfn] = FALSE;

#ifdef DEBUG_LIVENESS
		printf ("P: %d(%d): IN: ", bb->block_num, bb->dfn);
		for (j = 0; j < bb->in_count; ++j) 
			printf ("BB%d ", bb->in_bb [j]->block_num);
		printf ("OUT:");
		for (j = 0; j < bb->out_count; ++j) 
			printf ("BB%d ", bb->out_bb [j]->block_num);
		printf ("\n");
#endif


		if (bb->out_count == 0)
			continue;

		out_iter ++;

		if (!bb->live_in_set) {
			/* First pass over this bblock */
			changed = TRUE;
		}
		else {
			changed = FALSE;
			mono_bitset_copyto_fast (bb->live_out_set, old_live_out_set);
		}
 
		for (j = 0; j < bb->out_count; j++) {
			out_bb = bb->out_bb [j];

			if (!out_bb->live_in_set) {
				out_bb->live_in_set = mono_bitset_mem_new (mem, max_vars, MONO_BITSET_DONT_FREE);
				mem += bitsize;

				mono_bitset_copyto_fast (out_bb->live_out_set, out_bb->live_in_set);
				mono_bitset_sub_fast (out_bb->live_in_set, out_bb->kill_set);
				mono_bitset_union_fast (out_bb->live_in_set, out_bb->gen_set);
			}

			mono_bitset_union_fast (bb->live_out_set, out_bb->live_in_set);
		}
				
		if (changed || !mono_bitset_equal (old_live_out_set, bb->live_out_set)) {
			if (!bb->live_in_set) {
				bb->live_in_set = mono_bitset_mem_new (mem, max_vars, MONO_BITSET_DONT_FREE);
				mem += bitsize;
			}
			mono_bitset_copyto_fast (bb->live_out_set, bb->live_in_set);
			mono_bitset_sub_fast (bb->live_in_set, bb->kill_set);
			mono_bitset_union_fast (bb->live_in_set, bb->gen_set);

			for (j = 0; j < bb->in_count; j++) {
				MonoBasicBlock *in_bb = bb->in_bb [j];
				/* 
				 * Some basic blocks do not seem to be in the 
				 * cfg->bblocks array...
				 */
				if (in_bb->gen_set && !in_worklist [in_bb->dfn]) {
#ifdef DEBUG_LIVENESS
					printf ("\tADD: %d\n", in_bb->block_num);
#endif
					/*
					 * Put the block at the top of the stack, so it
					 * will be processed right away.
					 */
					worklist [l_end ++] = in_bb;
					in_worklist [in_bb->dfn] = TRUE;
				}
			}
		}
	}

#ifdef DEBUG_LIVENESS
		printf ("IT: %d %d.\n", cfg->num_bblocks, out_iter);
#endif

	mono_bitset_free (old_live_out_set);

	g_free (worklist);
	g_free (in_worklist);

	/* Compute live_in_set for bblocks skipped earlier */
	for (i = 0; i < cfg->num_bblocks; ++i) {
		MonoBasicBlock *bb = cfg->bblocks [i];

		if (!bb->live_in_set) {
			bb->live_in_set = mono_bitset_mem_new (mem, max_vars, MONO_BITSET_DONT_FREE);
			mem += bitsize;

			mono_bitset_copyto_fast (bb->live_out_set, bb->live_in_set);
			mono_bitset_sub_fast (bb->live_in_set, bb->kill_set);
			mono_bitset_union_fast (bb->live_in_set, bb->gen_set);
		}
	}

#ifdef DEBUG_LIVENESS
	for (i = cfg->num_bblocks - 1; i >= 0; i--) {
		MonoBasicBlock *bb = cfg->bblocks [i];
		
		printf ("LIVE IN  BB%d: ", bb->block_num); 
		mono_bitset_print (bb->live_in_set); 
		printf ("LIVE OUT BB%d: ", bb->block_num); 
		mono_bitset_print (bb->live_out_set); 
	}
#endif
}

static inline void
update_liveness (MonoCompile *cfg, MonoRegallocContext *ctx, MonoInst *ins, int inst_num, gint32 *last_use)
{
	const char *spec = INS_INFO (ins->opcode);
	int sreg;

	LIVENESS_DEBUG (printf ("\t%x: ", inst_num); mono_print_ins (ins));

	/* DREG */
	if (spec [MONO_INST_DEST] != ' ') {
		if (MONO_IS_STORE_MEMBASE (ins)) {
			if (last_use [ins->dreg] == 0) {
				LIVENESS_DEBUG (printf ("\tlast use of R%d set to %x\n", ins->dreg, inst_num + INS_POS_USE));
				last_use [ins->dreg] = inst_num + INS_POS_USE;
			}
		} else {
			if (last_use [ins->dreg] > 0) {
				LIVENESS_DEBUG (printf ("\tadd range to R%d: [%x, %x]\n", ins->dreg, inst_num + INS_POS_DEF, last_use [ins->dreg]));
				if (ins->dreg == ins->sreg1 && ins->dreg < MONO_FIRST_VREG) {
					/* 
					 * Avoid a hole in the liveness range, since the allocation code
					 * could think the register is free there.
					 */
					mono_linterval_add_range (ctx->cfg, ctx->varinfo [ins->dreg].interval, inst_num, last_use [ins->dreg]);
				} else {
					mono_linterval_add_range (ctx->cfg, ctx->varinfo [ins->dreg].interval, inst_num + INS_POS_DEF, last_use [ins->dreg]);
				}
				last_use [ins->dreg] = 0;
			}
			else {
				if (!vreg_is_volatile (cfg, ins->dreg) && ((ins->opcode == OP_ICONST) || (ins->opcode == OP_I8CONST) || (ins->opcode == OP_R8CONST) || (ins->opcode == OP_MOVE) || (ins->opcode == OP_FMOVE))) {
					LIVENESS_DEBUG (printf ("\tdead def of R%d eliminated\n", ins->dreg));
					NULLIFY_INS (ins);
					spec = INS_INFO (ins->opcode);
				} else {
					LIVENESS_DEBUG (printf ("\tdead def of R%d, add range to R%d: [%x, %x]\n", ins->dreg, ins->dreg, inst_num + INS_POS_DEF, inst_num + INS_POS_DEF));
					mono_linterval_add_range (ctx->cfg, ctx->varinfo [ins->dreg].interval, inst_num + INS_POS_DEF, inst_num + INS_POS_DEF);
				}
			}
		}
		if (ins->opcode != OP_NOP) {
			/* Since we process instructions backwards, the list will be properly sorted */
			if (MONO_IS_STORE_MEMBASE (ins))
				ctx->varinfo [ins->dreg].use_pos = g_slist_prepend_mempool (ctx->cfg->mempool, ctx->varinfo [ins->dreg].use_pos, GINT_TO_POINTER (inst_num));
			else
				ctx->varinfo [ins->dreg].use_pos = g_slist_prepend_mempool (ctx->cfg->mempool, ctx->varinfo [ins->dreg].use_pos, GINT_TO_POINTER (inst_num + INS_POS_DEF));
		}

		/* Set preferred vregs */
		if ((ins->opcode == OP_MOVE) || (ins->opcode == OP_FMOVE)) {
			if (ins->sreg1 < MONO_FIRST_VREG) {
				ctx->varinfo [ins->dreg].preferred_reg = ins->sreg1;
			} else if (ins->dreg < MONO_FIRST_VREG) {
				ctx->varinfo [ins->sreg1].preferred_reg = ins->dreg;
			} else if (ctx->varinfo [ins->dreg].preferred_reg != -1) {
				/*
				 * Propagate preferred vregs. This works because instructions are
				 * processed in reverse order.
				 */
				ctx->varinfo [ins->sreg1].preferred_reg = ctx->varinfo [ins->dreg].preferred_reg;
			}
		}
	}

	/* SREG1 */
	sreg = ins->sreg1;
	if (spec [MONO_INST_SRC1] != ' ') {
		if (last_use [sreg] == 0) {
			LIVENESS_DEBUG (printf ("\tlast use of R%d set to %x\n", sreg, inst_num + INS_POS_USE));
			last_use [sreg] = inst_num + INS_POS_USE;
		}
		ctx->varinfo [sreg].use_pos = g_slist_prepend_mempool (ctx->cfg->mempool, ctx->varinfo [sreg].use_pos, GINT_TO_POINTER (inst_num));
	}

	/* SREG2 */
	sreg = ins->sreg2;
	if (spec [MONO_INST_SRC2] != ' ') {
		if (last_use [sreg] == 0) {
			LIVENESS_DEBUG (printf ("\tlast use of R%d set to %x\n", sreg, inst_num + INS_POS_USE));
			last_use [sreg] = inst_num + INS_POS_USE;
		}
		ctx->varinfo [sreg].use_pos = g_slist_prepend_mempool (ctx->cfg->mempool, ctx->varinfo [sreg].use_pos, GINT_TO_POINTER (inst_num));
	}

	if (ins_get_spec (ins->opcode)[MONO_INST_CLOB] == 'c') {
		MonoCallInst *call = (MonoCallInst*)ins;
		GSList *list;

		for (list = call->out_ireg_args; list; list = list->next) {
			guint32 regpair;

			regpair = (guint32)(gssize)(list->data);
			sreg = regpair >> 24;

			if (last_use [sreg] == 0) {
				LIVENESS_DEBUG (printf ("\tlast use of R%d set to %x\n", sreg, inst_num + INS_POS_USE));
				last_use [sreg] = inst_num + INS_POS_USE;
			}
			ctx->varinfo [sreg].use_pos = g_slist_prepend_mempool (ctx->cfg->mempool, ctx->varinfo [sreg].use_pos, GINT_TO_POINTER (inst_num));
		}

		for (list = call->out_freg_args; list; list = list->next) {
			guint32 regpair;

			regpair = (guint32)(gssize)(list->data);
			sreg = (regpair >> 24) + MONO_MAX_IREGS;

			if (last_use [sreg] == 0) {
				LIVENESS_DEBUG (printf ("\tlast use of R%d set to %x\n", sreg, inst_num + INS_POS_USE));
				last_use [sreg] = inst_num + INS_POS_USE;
			}
			ctx->varinfo [sreg].use_pos = g_slist_prepend_mempool (ctx->cfg->mempool, ctx->varinfo [sreg].use_pos, GINT_TO_POINTER (inst_num));
		}
	}

	/* CLOBBERING */
	if (ins_get_spec (ins->opcode)[MONO_INST_CLOB]) {
		char clob = ins_get_spec (ins->opcode)[MONO_INST_CLOB];
		GList *l;

		if (clob == 'c') {
			/* A call clobbers some int/fp registers */
			for (l = mono_arch_get_iregs_clobbered_by_call ((MonoCallInst*)ins); l; l = l->next)
				mono_linterval_add_range (ctx->cfg, ctx->varinfo [GPOINTER_TO_INT (l->data)].interval, inst_num + INS_POS_CLOB, inst_num + INS_POS_CLOB);
			for (l = mono_arch_get_fregs_clobbered_by_call ((MonoCallInst*)ins); l; l = l->next)
				mono_linterval_add_range (ctx->cfg, ctx->varinfo [GPOINTER_TO_INT (l->data)].interval, inst_num + INS_POS_CLOB, inst_num + INS_POS_CLOB);
		}
		else {
			int clob_reg = MONO_ARCH_INST_FIXED_REG (clob);

			if (clob_reg != -1)
				mono_linterval_add_range (ctx->cfg, ctx->varinfo [clob_reg].interval, inst_num + INS_POS_CLOB, inst_num + INS_POS_CLOB);
		}
	}
}

/*
 * compute_intervals:
 *
 *   Compute liveness intervals for all vregs.
 */
static void
compute_intervals (MonoCompile *cfg, MonoRegallocContext *ctx)
{
	int bnum, idx, i, j, nins, rem, max, max_vars, block_from, block_to, pos, reverse_len;
	gint32 *last_use;
	MonoInst **reverse;

	max_vars = cfg->next_vreg;
	last_use = g_new0 (gint32, max_vars);

	reverse_len = 1024;
	reverse = mono_mempool_alloc (cfg->mempool, sizeof (MonoInst*) * reverse_len);

	for (idx = 0; idx < max_vars; ++idx) {
		ctx->varinfo [idx].interval = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoLiveInterval));
	}

	/*
	 * Process bblocks in reverse order, so the addition of new live ranges
	 * to the intervals is faster.
	 */
	for (bnum = cfg->num_bblocks - 1; bnum >= 0; --bnum) {
		MonoBasicBlock *bb = cfg->bblocks [bnum];
		MonoInst *ins;

		block_from = (bb->dfn << 16); /* so pos > 0 */
		if (bnum < cfg->num_bblocks - 1)
			/* Beginning of the next bblock */
			block_to = (cfg->bblocks [bnum + 1]->dfn << 16);
		else
			block_to = (bb->dfn << 16) + 0xffff;

		LIVENESS_DEBUG (printf ("LIVENESS BLOCK BB%d:\n", bb->block_num));

		memset (last_use, 0, max_vars * sizeof (gint32));
		
		/* For variables in bb->live_out, set last_use to block_to */

		rem = max_vars % BITS_PER_CHUNK;
		max = ((max_vars + (BITS_PER_CHUNK -1)) / BITS_PER_CHUNK);
		for (j = 0; j < max; ++j) {
			gsize bits_out;
			int k;

			bits_out = mono_bitset_get_fast (bb->live_out_set, j);
			k = (j * BITS_PER_CHUNK);	
			while (bits_out) {
				if (bits_out & 1) {
					LIVENESS_DEBUG (printf ("Var R%d live at exit, set last_use to %x\n", k, block_to));
					last_use [k] = block_to;
				}
				bits_out >>= 1;
				k ++;
			}
		}

		for (nins = 0, pos = block_from, ins = bb->code; ins; ins = ins->next, ++nins, pos += INS_POS_INTERVAL) {
			if (nins >= reverse_len) {
				int new_reverse_len = reverse_len * 2;
				MonoInst **new_reverse = mono_mempool_alloc (cfg->mempool, sizeof (MonoInst*) * new_reverse_len);
				memcpy (new_reverse, reverse, sizeof (MonoInst*) * reverse_len);
				reverse = new_reverse;
				reverse_len = new_reverse_len;
			}

			reverse [nins] = ins;
		}

		g_assert (pos < block_to);

		/* Process instructions backwards */
		for (i = nins - 1; i >= 0; --i) {
			MonoInst *ins = (MonoInst*)reverse [i];

 			update_liveness (cfg, ctx, ins, pos, last_use);

			pos -= INS_POS_INTERVAL;
		}

		for (idx = 0; idx < max_vars; ++idx) {
			if (last_use [idx] != 0) {
				/* Live at exit, not written -> live on enter */
				LIVENESS_DEBUG (printf ("Var R%d live at enter, add range to R%d: [%x, %x)\n", idx, idx, block_from, last_use [idx]));
				mono_linterval_add_range (cfg, ctx->varinfo [idx].interval, block_from, last_use [idx]);
			}
		}
	}

#if 0
	// FIXME:
	/*
	 * Arguments need to have their live ranges extended to the beginning of
	 * the method to account for the arg reg/memory -> global register copies
	 * in the prolog (bug #74992).
	 */
	for (i = 0; i < cfg->num_varinfo; i ++) {
		MonoMethodVar *vi = MONO_VARINFO (cfg, i);
		if ((cfg->varinfo [vi->idx]->opcode == OP_ARG) && (cfg->varinfo [vi->idx] != cfg->ret))
			mono_linterval_add_range (cfg, ctx->varinfo [cfg->varinfo [i]->dreg].interval, 0, 1);
	}
#endif

#if 0
		for (idx = 0; idx < max_vars; ++idx) {
			printf ("LIVENESS R%d: ", idx);
			mono_linterval_print (ctx->varinfo [idx].interval);
			printf ("\n");
		}
	}
#endif

	g_free (last_use);
}

/*
 * analyze_liveness:
 *
 *   Perform liveness analysis.
 */
static void
analyze_liveness (MonoCompile *cfg, MonoRegallocContext *ctx)
{
	LIVENESS_DEBUG (printf ("LIVENESS 3 %s\n", mono_method_full_name (cfg->method, TRUE)));

	/* FIXME: Make only one pass over the IR */

	compute_gen_kill_sets (cfg, ctx);

	compute_live_in_out_sets (cfg, ctx);

	compute_intervals (cfg, ctx);
}


static gint
compare_by_interval_start_pos_func (gconstpointer a, gconstpointer b)
{
	MonoRegallocInterval *v1 = (MonoRegallocInterval*)a;
	MonoRegallocInterval *v2 = (MonoRegallocInterval*)b;

	if (v1 == v2)
		return 0;
	else if (v1->interval->range && v2->interval->range)
		return v1->interval->range->from - v2->interval->range->from;
	else if (v1->interval->range)
		return -1;
	else
		return 1;
}

#define LSCAN_DEBUG(a) MINI_DEBUG(cfg->verbose_level, 2, a;)

/**
 * split_interval:
 *
 *   Split the interval into two child intervals at POS. 
 * [a, b] becomes [a, POS - 1], [POS, b].
 */
static void
split_interval (MonoCompile *cfg, MonoRegallocContext *ctx, MonoRegallocInterval *interval, int pos)
{
	MonoRegallocInterval *child1, *child2;
	GSList *l, *split_list;

	child1 = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoRegallocInterval));
	child2 = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoRegallocInterval));
	child1->vreg = ctx->num_intervals ++;
	child1->hreg = -1;
	child1->offset = -1;
	child1->preferred_reg = -1;
	child1->is_volatile = interval->is_volatile;
	child1->fp = interval->fp;
	child1->type = interval->type;
	child2->vreg = ctx->num_intervals ++;
	child2->hreg = -1;
	child2->offset = -1;
	child2->preferred_reg = -1;
	child2->is_volatile = interval->is_volatile;
	child2->fp = interval->fp;
	child2->type = interval->type;

	interval->child1 = child1;
	interval->child2 = child2;
	child1->parent = interval;
	child2->parent = interval;

	mono_linterval_split (cfg, interval->interval, &child1->interval, &child2->interval, pos);

	/* Split use positions */
	for (l = interval->use_pos; l; l = l->next) {
		int use_pos = GPOINTER_TO_INT (l->data);

		if (use_pos < pos)
			child1->use_pos = g_slist_append_mempool (cfg->mempool, child1->use_pos, l->data);
		else
			child2->use_pos = g_slist_append_mempool (cfg->mempool, child2->use_pos, l->data);
	}

	/* Remember where spill code needs to be inserted */
	split_list = g_hash_table_lookup (ctx->split_positions, GUINT_TO_POINTER (pos));
	split_list = g_slist_prepend (split_list, interval);
	g_hash_table_insert (ctx->split_positions, GUINT_TO_POINTER (pos), split_list);
	g_hash_table_insert (ctx->split_position_set, GUINT_TO_POINTER (pos - (pos % INS_POS_INTERVAL)), GUINT_TO_POINTER (pos));

	if (cfg->verbose_level > 2) {
		printf ("\tSplit R%d into R%d and R%d at %x\n", interval->vreg, child1->vreg, child2->vreg, pos);
		printf ("\t R%d ", interval->vreg);
		mono_linterval_print (interval->interval);
		printf ("-> R%d ", child1->vreg);
		mono_linterval_print (child1->interval);
		printf ("||| R%d ", child2->vreg);
		mono_linterval_print (child2->interval);
		printf ("\n");
	}
}

/**
 * child_at:
 *
 *   Return L or one of its children which covers POS.
 */
static MonoRegallocInterval*
child_at (MonoRegallocInterval *l, int pos)
{
	if (l->vreg < MONO_FIRST_VREG)
		return l;

	if (!l->child1) {
		g_assert (mono_linterval_covers (l->interval, pos));
		return l;
	}

	if (mono_linterval_covers (l->child1->interval, pos))
		return child_at (l->child1, pos);
	else if (mono_linterval_covers (l->child2->interval, pos))
		return child_at (l->child2, pos);
	else {
		g_assert_not_reached ();
		return NULL;
	}
}

/**
 * decompose_volatile_intervals:
 *
 *   Decompose intervals belonging to volatile variables. Return the decomposed intervals
 * which should be allocated to registers.
 */
static GList*
decompose_volatile_intervals (MonoCompile *cfg, MonoRegallocContext *ctx, GList *intervals)
{
	GList *new_intervals;
	GList *l;

	/*
	 * We model volatile intervals by splitting them at use positions and spilling the
	 * sub intervals, ie. [a, b] is transformed to [a, a], [a + 1, b], [b, b] with the
	 * middle interval spilled. This ensures that the variable will be spilled after each
	 * def, and it will be loaded before each use.
	 * FIXME: Stress test this by making most variables volatile
	 */
	new_intervals = g_list_copy (intervals);
	for (l = intervals; l; l = l->next) {
		MonoRegallocInterval *current = l->data;
		MonoLiveInterval *new;
		GSList *use_pos;
		gboolean ends_with_def;

		if (!current->is_volatile)
			continue;

		/*
		 * Instead of trying to split the arbitrary interval produced by the liveness
		 * analysis phase, just use one big interval.
		 */
		ends_with_def = FALSE;
		use_pos = current->use_pos;
		while (use_pos) {
			int pos = GPOINTER_TO_INT (use_pos->data);

			use_pos = use_pos->next;
			if (!use_pos && USE_POS_IS_DEF (pos))
				ends_with_def = TRUE;
		}

		new = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoLiveInterval));
		mono_linterval_add_range (cfg, new, 0, current->interval->last_range->to + (ends_with_def ? INS_POS_INTERVAL : 0));
		current->interval = new;

		LSCAN_DEBUG (printf ("R%d is volatile ", current->vreg));
		LSCAN_DEBUG (mono_linterval_print (current->interval));
		LSCAN_DEBUG (printf ("\n"));

		new_intervals = g_list_remove (new_intervals, current);

		use_pos = current->use_pos;
		while (use_pos) {
			gboolean is_def = USE_POS_IS_DEF (GPOINTER_TO_INT (use_pos->data));
			int pos = USE_POS_BASE (GPOINTER_TO_INT (use_pos->data));
			use_pos = use_pos->next;

			LSCAN_DEBUG (printf ("\tUse pos: %x\n", pos));

			/* Split the part of the interval before the definition into its own interval */
			if (pos > current->interval->range->from) {
				split_interval (cfg, ctx, current, pos);
				current = current->child2;
			}

			if (!is_def && pos == current->interval->last_range->to) {
				/* No need to split the last use */
				new_intervals = g_list_insert_sorted (new_intervals, current, compare_by_interval_start_pos_func);				
				break;
			}

			/* Split the use into its own interval */
			split_interval (cfg, ctx, current, pos + INS_POS_INTERVAL);
			new_intervals = g_list_insert_sorted (new_intervals, current->child1, compare_by_interval_start_pos_func);
			current = current->child2;

			/* No need to (and hard to) split between use positions at the same place */
			while (use_pos && USE_POS_BASE (GPOINTER_TO_INT (use_pos->data)) == pos)
				use_pos = use_pos->next;
		}
	}

	return new_intervals;
}

/**
 * linear_scan:
 *
 *   The actual linear scan register allocation algorithm.
 */
static void
linear_scan (MonoCompile *cfg, MonoRegallocContext *ctx)
{
	GList *int_regs = mono_arch_get_global_int_regs (cfg);
	GList *fp_regs = mono_arch_get_global_fp_regs (cfg);
	GList *vars;
	GList *unhandled, *active, *inactive, *l, *next;
	gint32 free_pos [MONO_MAX_IREGS + MONO_MAX_FREGS];
	gboolean allocateable [MONO_MAX_IREGS + MONO_MAX_FREGS];
	int i;
	MonoMethodSignature *sig;
	MonoMethodHeader *header;

	LSCAN_DEBUG (printf ("\nLINEAR SCAN 2 for %s:\n", mono_method_full_name (cfg->method, TRUE)));

	header = cfg->header;

	sig = mono_method_signature (cfg->method);

	/* Create list of allocatable variables */
	vars = NULL;
	for (i = MONO_FIRST_VREG; i < cfg->next_vreg; ++i) {
		if (ctx->varinfo [i].interval->range)
			vars = g_list_prepend (vars, &ctx->varinfo [i]);
	}

	for (i = 0; i < MONO_MAX_IREGS; ++i)
		allocateable [i] = g_list_find (int_regs, GINT_TO_POINTER (i)) != NULL;
	for (i = 0; i < MONO_MAX_FREGS; ++i)
		allocateable [MONO_MAX_IREGS + i] = g_list_find (fp_regs, GINT_TO_POINTER (i)) != NULL;
	g_list_free (int_regs);
	g_list_free (fp_regs);

	unhandled = g_list_sort (g_list_copy (vars), compare_by_interval_start_pos_func);
	active = NULL;
	inactive = NULL;

	/* The hard registers are assigned to themselves */
	for (i = 0; i < MONO_MAX_IREGS + MONO_MAX_FREGS; ++i) {
		ctx->varinfo [i].hreg = i;
		if (ctx->varinfo [i].interval->range)
			inactive = g_list_append (inactive, &ctx->varinfo [i]);		
	}

	unhandled = decompose_volatile_intervals (cfg, ctx, unhandled);

	/*
	 * Handle arguments received on the stack by splitting their interval, and later
	 * allocating the spilled part to the arg location.
	 */
	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		MonoInst *ins = cfg->args [i];
		MonoRegallocInterval *current = &ctx->varinfo [ins->dreg];
		MonoType *arg_type;

		if (sig->hasthis && (i == 0))
			arg_type = &mono_defaults.object_class->byval_arg;
		else
			arg_type = sig->params [i - sig->hasthis];

		if (ins->opcode != OP_REGVAR && !MONO_TYPE_ISSTRUCT (arg_type) && !current->is_volatile && current->interval->range) {
			/* This ensures there is some part of the interval before the use pos */
			g_assert (current->interval->range->from == 0);

			/* Have to split at an use pos so a spill load can be inserted */
			if (current->use_pos) {
				guint32 pos = USE_POS_BASE (GPOINTER_TO_INT (current->use_pos->data));

				split_interval (cfg, ctx, current, pos);
				unhandled = g_list_remove (unhandled, current);
				unhandled = g_list_insert_sorted (unhandled, current->child2, compare_by_interval_start_pos_func);
			}
		}
	}

	while (unhandled) {
	    MonoRegallocInterval *current = unhandled->data;
		int pos, reg, max_free_pos;
		gboolean changed;

		unhandled = g_list_delete_link (unhandled, unhandled);

		LSCAN_DEBUG (printf ("Processing R%d: ", current->vreg));
		LSCAN_DEBUG (mono_linterval_print (current->interval));
		LSCAN_DEBUG (printf ("\n"));

		if (!current->interval->range)
			continue;
			
		/* Happens when splitting intervals */
		if (!current->use_pos)
			continue;

		pos = current->interval->range->from;

		/* Check for intervals in active which expired or inactive */
		changed = TRUE;
		/* FIXME: Optimize this */
		l = active;
		while (l) {
			MonoRegallocInterval *v = l->data;

			next = l->next;
			if (v->interval->last_range->to < pos) {
				active = g_list_delete_link (active, l);
				LSCAN_DEBUG (printf ("\tInterval R%d has expired\n", v->vreg));
			} else if (!mono_linterval_covers (v->interval, pos)) {
				inactive = g_list_append (inactive, v);
				active = g_list_delete_link (active, l);
				LSCAN_DEBUG (printf ("\tInterval R%d became inactive\n", v->vreg));
			}
			l = next;
		}

		/* Check for intervals in inactive which are expired or active */
		l = inactive;
		while (l) {
			MonoRegallocInterval *v = l->data;

			next = l->next;
			if (v->interval->last_range->to < pos) {
				inactive = g_list_delete_link (inactive, l);
				LSCAN_DEBUG (printf ("\tInterval R%d has expired\n", v->vreg));
			} else if (mono_linterval_covers (v->interval, pos)) {
				active = g_list_append (active, v);
				inactive = g_list_delete_link (inactive, l);
				LSCAN_DEBUG (printf ("\tInterval R%d became active\n", v->vreg));
			}
			l = next;
		}

		/* Find a register for the current interval */
		if (G_UNLIKELY (current->fp)) {
			for (i = MONO_MAX_IREGS; i < MONO_MAX_IREGS + MONO_MAX_FREGS; ++i)
				if (allocateable [i])
					free_pos [i] = G_MAXINT32;
				else
					free_pos [i] = 0;
		} else {
			for (i = 0; i < MONO_MAX_IREGS; ++i)
				if (allocateable [i])
					free_pos [i] = G_MAXINT32;
				else
					free_pos [i] = 0;
		}

		for (l = active; l != NULL; l = l->next) {
			MonoRegallocInterval *v = l->data;

			if (v->hreg >= 0) {
				free_pos [v->hreg] = 0;
				LSCAN_DEBUG (printf ("\threg %d is busy (R%d)\n", v->hreg, v->vreg));
			}
		}

		for (l = inactive; l != NULL; l = l->next) {
			MonoRegallocInterval *v = l->data;
			gint32 intersect_pos;

			if ((v->hreg >= 0) && (current->fp == v->fp)) {
				intersect_pos = mono_linterval_get_intersect_pos (current->interval, v->interval);
				if (intersect_pos != -1) {
					if (intersect_pos < free_pos [v->hreg])
						free_pos [v->hreg] = intersect_pos;
					LSCAN_DEBUG (printf ("\threg %d becomes free at %x\n", v->hreg, intersect_pos));
				}
			}
		}

		max_free_pos = -1;
		reg = -1;

#if 0
		/* 
		 * Arguments should be allocated to the registers they reside in at the start of
		 * the method.
		 */
		for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
			MonoInst *ins = cfg->args [i];

			g_assert (ins->opcode == OP_REGVAR);
			if (ins->dreg == current->vreg)
				reg = ins->inst_c0;
		}
#endif

		if (reg == -1) {
			if (G_UNLIKELY (current->fp)) {
				for (i = MONO_MAX_IREGS; i < MONO_MAX_IREGS + MONO_MAX_FREGS; ++i)
					if (free_pos [i] > max_free_pos) {
						reg = i;
						max_free_pos = free_pos [i];
					}
			} else {
				for (i = 0; i < MONO_MAX_IREGS; ++i)
					if (free_pos [i] > max_free_pos) {
						reg = i;
						max_free_pos = free_pos [i];
					}
			}

			if (current->preferred_reg != -1) {
				LSCAN_DEBUG (printf ("\tPreferred register is hreg %d\n", current->preferred_reg));
				/* FIXME: Add more cases */
				if (free_pos [current->preferred_reg] >= free_pos [reg]) {
					reg = current->preferred_reg;
				} else {
#if 0
					/*
					 * We have a choice to make: assigning to the preferred reg avoids
					 * a move, while assigning to 'reg' will keep the variable in a
					 * register for longer.
					 */
					if (free_pos [current->preferred_reg] >= current->interval->range->from)
						reg = current->preferred_reg;
#endif
				}
			}
		}

		g_assert (reg != -1);

		if (!(free_pos [reg] > 0 && free_pos [reg] >= current->interval->range->from) &&
			USE_POS_BASE (GPOINTER_TO_INT (current->use_pos->data)) <= current->interval->range->from) {
			/*
			 * No register is available, and current is needed in a register right now.
			 * So free up a register by spilling an interval in active.
			 */
			MonoRegallocInterval *to_spill;
			guint32 split_pos;

			// FIXME: Why was this needed ?
			//g_assert (!current->is_volatile);

			/* Spill the first */
			/* FIXME: Optimize the selection of the interval */
			l = active;
			to_spill = NULL;
			for (l = active; l; l = l->next) {
				to_spill = l->data;

				/* Fixed intervals cannot be spilled */
				if (to_spill->vreg >= MONO_FIRST_VREG)
					break;
			}
			g_assert (to_spill);

			LSCAN_DEBUG (printf ("\tNo free register found, splitting and spilling R%d\n", to_spill->vreg));
			split_pos = USE_POS_BASE (GPOINTER_TO_INT (current->use_pos->data));
			/* 
			 * Avoid splitting to_spill before the start of current, since
			 * its second child, which is added to unhandled would begin before 
			 * current.
			 */
			if (split_pos < current->interval->range->from)
				split_pos = current->interval->range->from;
			split_interval (cfg, ctx, to_spill, split_pos);
			to_spill->child1->hreg = to_spill->hreg;
			active = g_list_remove (active, to_spill);
			unhandled = g_list_insert_sorted (unhandled, to_spill->child2, compare_by_interval_start_pos_func);
			reg = to_spill->hreg;

			/* Recompute free_pos [reg] */
			free_pos [reg] = G_MAXINT32;
			for (l = active; l != NULL; l = l->next) {
				MonoRegallocInterval *v = l->data;

				if (v->hreg == reg) {
					free_pos [v->hreg] = 0;
					LSCAN_DEBUG (printf ("\threg %d is busy (R%d)\n", v->hreg, v->vreg));
				}
			}

			for (l = inactive; l != NULL; l = l->next) {
				MonoRegallocInterval *v = l->data;
				gint32 intersect_pos;

				if ((v->hreg == reg) && (current->fp == v->fp)) {
					intersect_pos = mono_linterval_get_intersect_pos (current->interval, v->interval);
					if (intersect_pos != -1) {
						if (intersect_pos < free_pos [v->hreg])
							free_pos [v->hreg] = intersect_pos;
						LSCAN_DEBUG (printf ("\threg %d becomes free at %x\n", v->hreg, intersect_pos));
					}
				}
			}
		}

		if (free_pos [reg] > 0 && free_pos [reg] >= current->interval->last_range->to) {
			/* Register available for whole interval */
			current->hreg = reg;
			if (!current->fp)
				cfg->used_int_regs |= (1 << reg);
			LSCAN_DEBUG (printf ("\tAssigned hreg %d to R%d\n", reg, current->vreg));

			active = g_list_append (active, current);
		}
		else if (free_pos [reg] > 0 && free_pos [reg] >= current->interval->range->from) {
			/* 
			 * The register is available for some part of the interval.
			 * Split the interval, assign the register to the first part of the 
			 * interval, and save the second part for later processing.
			 */
			LSCAN_DEBUG (printf ("\tRegister %d is available until %x, splitting current.\n", reg, free_pos [reg]));
			split_interval (cfg, ctx, current, free_pos [reg]);

			current->child1->hreg = reg;
			if (!current->fp)
				cfg->used_int_regs |= (1 << reg);
			LSCAN_DEBUG (printf ("\tAssigned hreg %d to R%d\n", reg, current->child1->vreg));
			active = g_list_append (active, current->child1);

			unhandled = g_list_insert_sorted (unhandled, current->child2, compare_by_interval_start_pos_func);
		} else {
			guint32 use_pos = USE_POS_BASE (GPOINTER_TO_INT (current->use_pos->data));

			/* No register is available */
			if (use_pos > current->interval->range->from) {
				/*
				 * The interval is not currently needed in a register. So split it, and
				 * spill the first part to memory, and save the second part for later
				 * processing.
				 */
				LSCAN_DEBUG (printf ("\tSplitting R%d(current) at first use pos %x, spilling the first part.\n", current->vreg, use_pos));
				split_interval (cfg, ctx, current, use_pos);
				unhandled = g_list_insert_sorted (unhandled, current->child2, compare_by_interval_start_pos_func);
			} else {
				/* Handled previously */
				g_assert_not_reached ();
			}
		}
	}

	/* 
	 * The fp registers are numbered from MONO_MAX_IREGS during allocation, but they are
	 * numbered from 0 in machine code.
	 */
	for (i = 0; i < cfg->next_vreg; ++i) {
		if (ctx->varinfo [i].fp) {
			GSList *children;

			/* Need to process child intervals as well */
			/* This happens rarely so it is not perf critical */
			children = NULL;
			children = g_slist_prepend (children, &ctx->varinfo [i]);
			while (children) {
				MonoRegallocInterval *interval = children->data;

				children = g_slist_delete_link (children, children);
				if (interval->hreg != -1)
					interval->hreg -= MONO_MAX_IREGS;
				if (interval->child1)
					children = g_slist_prepend (children, interval->child1);
				if (interval->child2)
					children = g_slist_prepend (children, interval->child2);			
			}
		}
	}
}

static GSList*
collect_spilled_intervals (MonoRegallocInterval *interval, GSList *list)
{
	if ((interval->hreg == -1) && !interval->child1 && interval->interval->range) 
		list = g_slist_prepend (list, interval);

	if (interval->is_volatile && !interval->interval->range)
		/* Variables which are only referenced by ldaddr */
		list = g_slist_prepend (list, interval);		

	if (interval->child1) {
		list = collect_spilled_intervals (interval->child1, list);
		list = collect_spilled_intervals (interval->child2, list);
	}

	return list;
}

static int
alloc_spill_slot (MonoCompile *cfg, guint32 size, guint32 align)
{
	guint32 res;

	if (size == 0) {
		res = cfg->stack_offset;
	} else {
		if (cfg->flags & MONO_CFG_HAS_SPILLUP) {
			cfg->stack_offset += align - 1;
			cfg->stack_offset &= ~(align - 1);
			res = cfg->stack_offset;
			cfg->stack_offset += size;
		} else {
			cfg->stack_offset += align - 1;
			cfg->stack_offset &= ~(align - 1);
			cfg->stack_offset += size;
			res = - cfg->stack_offset;
		}
	}

	return res;
}

static void
assign_spill_slots (MonoCompile *cfg, MonoRegallocContext *ctx)
{
	GSList *spilled_intervals = NULL;
	GSList *l;
	MonoMethodSignature *sig;
	int i;

	/* Handle arguments passed on the stack */
	sig = mono_method_signature (cfg->method);
	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		MonoInst *ins = cfg->args [i];
		MonoType *arg_type;

		if (sig->hasthis && (i == 0))
			arg_type = &mono_defaults.object_class->byval_arg;
		else
			arg_type = sig->params [i - sig->hasthis];

		if (MONO_TYPE_ISSTRUCT (arg_type) || (ins->opcode != OP_REGVAR)) {
			g_assert (ins->opcode == OP_REGOFFSET);
			// FIXME: Add a basereg field to varinfo
			// FIXME:
			g_assert (ins->inst_offset != -1);
			ctx->varinfo [ins->dreg].offset = ins->inst_offset;
		}
	}

	/* Handle a vtype return */
	if (!cfg->vret_addr && MONO_TYPE_ISSTRUCT (sig->ret)) {
		MonoInst *ins = cfg->ret;

		ctx->varinfo [ins->dreg].offset = ins->inst_offset;
	}

	for (i = 0; i < cfg->next_vreg; ++i) {
		spilled_intervals = collect_spilled_intervals (&ctx->varinfo [i], spilled_intervals);
	}

	LSCAN_DEBUG (printf ("\nSPILL OFFSETS:\n\n"));
							 
	for (l = spilled_intervals; l; l = l->next) {
		MonoRegallocInterval *interval = l->data;
		MonoRegallocInterval *parent;

		/*
		 * All spilled sub-intervals of a interval must share the stack slot.
		 * This is accomplished by storing the stack offset in the original interval 
		 * and using that offset for all its children.
		 */

		for (parent = interval; parent->parent != NULL; parent = parent->parent)
			;
		if (parent->offset != -1) {
			interval->offset = parent->offset;
		} else if (interval->offset != -1) {
			/* Already allocated (for example, valuetypes as arguments) */
		} else {
			guint32 size, align;

			if (MONO_TYPE_ISSTRUCT (interval->type)) {
				// FIXME: pinvoke, gsctx
				// FIXME: Align
				size = mini_type_stack_size (NULL, interval->type, NULL);
			} else if (interval->fp) {
				size = sizeof (double);
			} else {
				size = sizeof (gpointer);
			}

			align = sizeof (gpointer);
			interval->offset = alloc_spill_slot (cfg, size, align);
		}

		for (parent = interval; parent != NULL; parent = parent->parent) {
			if (parent->offset == -1)
				parent->offset = interval->offset;
		}

		LSCAN_DEBUG (printf ("R%d %d", interval->vreg, interval->offset));
		LSCAN_DEBUG (mono_linterval_print (interval->interval));
		LSCAN_DEBUG (printf ("\n"));
	}

	/* Write back information needed by the backend */
	if (cfg->rgctx_var) {
		/* rgctx_var is marked as volatile, so it won't be allocated to a register */
		cfg->rgctx_var->opcode = OP_REGOFFSET;
		cfg->rgctx_var->inst_basereg = cfg->frame_reg;
		cfg->rgctx_var->inst_offset = ctx->varinfo [cfg->rgctx_var->dreg].offset;
	}
}

/**
 * order_moves:
 *
 *   Order the instructions in MOVES so earlier moves don't overwrite the sources of
 * later moves.
 */
static GSList*
order_moves (MonoCompile *cfg, MonoRegallocContext *ctx, MonoInst **moves, int nmoves)
{
	int i, j, niter;
	GSList *l;

	/* 
	 * Sort the moves so earlier moves don't overwrite the sources of later
	 * moves.
	 */
	/* FIXME: Do proper cycle detection instead of the current ugly hack */
	niter = 0;
	for (i = 0; i < nmoves; ++i) {
		gboolean found;

		found = TRUE;
		while (found) {
			found = FALSE;
			for (j = i + 1; j < nmoves; ++j)
				if (moves [i]->dreg == moves [j]->sreg1) {
					found = TRUE;
					break;
				}
			if (found) {
				MonoInst *ins;

				ins = moves [j];
				moves [j] = moves [i];
				moves [i] = ins;

				niter ++;
				if (niter > nmoves * 2)
					/* Possible cycle */
					break;
			}
		}
		if (niter > nmoves * 2)
			break;
	}

	l = NULL;
	if (niter > nmoves * 2) {
		MonoInst *ins;
		int *offsets;

		/*
		 * Save all registers to the stack and reload them again.
		 * FIXME: Optimize this.
		 */

		/* Allocate spill slots */
		offsets = mono_mempool_alloc (cfg->mempool, nmoves * sizeof (int));
		for (i = 0; i < nmoves; ++i) {
			guint32 size = sizeof (gpointer);

			if (cfg->flags & MONO_CFG_HAS_SPILLUP) {
				cfg->stack_offset += size - 1;
				cfg->stack_offset &= ~(size - 1);
				offsets [i] = cfg->stack_offset;
				cfg->stack_offset += size;
			} else {
				cfg->stack_offset += size - 1;
				cfg->stack_offset &= ~(size - 1);
				cfg->stack_offset += size;
				offsets [i] = - cfg->stack_offset;
			}
		}

		/* Create stores */
		for (i = 0; i < nmoves; ++i) {
			if (moves [i]->opcode == OP_MOVE)
				MONO_INST_NEW (cfg, ins, OP_STORE_MEMBASE_REG);
			else if (moves [i]->opcode == OP_FMOVE)
				MONO_INST_NEW (cfg, ins, OP_STORER8_MEMBASE_REG);
			else
				NOT_IMPLEMENTED;
			ins->sreg1 = moves [i]->sreg1;
			ins->inst_destbasereg = cfg->frame_reg;
			ins->inst_offset = offsets [i];

			l = g_slist_append_mempool (cfg->mempool, l, ins);
			g_hash_table_insert (ctx->spill_ins, ins, ins);
		}

		/* Create loads */
		for (i = 0; i < nmoves; ++i) {
			if (moves [i]->opcode == OP_MOVE)
				MONO_INST_NEW (cfg, ins, OP_LOAD_MEMBASE);
			else if (moves [i]->opcode == OP_FMOVE)
				MONO_INST_NEW (cfg, ins, OP_LOADR8_MEMBASE);
			else
				NOT_IMPLEMENTED;
			ins->dreg = moves [i]->dreg;
			ins->inst_basereg = cfg->frame_reg;
			ins->inst_offset = offsets [i];

			l = g_slist_append_mempool (cfg->mempool, l, ins);
			g_hash_table_insert (ctx->spill_ins, ins, ins);
		}

		return l;
	} else {
		for (i = 0; i < nmoves; ++i)
			l = g_slist_append_mempool (cfg->mempool, l, moves [i]);

		return l;
	}
}

/**
 * add_spill_code:
 *
 *   Add spill loads and stores to the IR at the locations where intervals were split.
 */
static void
add_spill_code (MonoCompile *cfg, MonoRegallocContext *ctx)
{
	MonoBasicBlock *bb;
	MonoInst *ins, *prev, *store, *load, *move, *insert_after;
	GSList *spill_list, *l, *ins_to_add, *moves_to_add;
	MonoRegallocInterval *child1, *child2;
	int pos, pos_interval, pos_interval_limit;
	MonoBasicBlock *out_bb;
	int i, bb_count, from_pos, to_pos, iter;
	gboolean after_last_ins, add_at_head;

	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		if (cfg->verbose_level > 1)
			printf ("\nREGALLOC-ADD SPILL CODE %d (DFN 0x%x):\n", bb->block_num, bb->dfn);

		/* First pass: Add spill loads/stores to the IR */
		pos = (bb->dfn << 16) + INS_POS_INTERVAL;
		prev = NULL;
		after_last_ins = FALSE;
		for (ins = bb->code; !after_last_ins;) {
			if (ins == NULL) {
				after_last_ins = TRUE;
			} else if (g_hash_table_lookup (ctx->spill_ins, ins)) {
				/* Spill instruction added by an earlier bblock */
				/* No need to increase pos */

				if (G_UNLIKELY (cfg->verbose_level > 1)) {
					printf (" <spill ins>\n");
				}

				prev = ins;
				ins = ins->next;
				continue;
			}

			if (!g_hash_table_lookup (ctx->split_position_set, GUINT_TO_POINTER (pos))) {
				/* No split position at this instruction */
				pos_interval_limit = 0;
				pos += INS_POS_INTERVAL;
			} else {
				pos_interval_limit = INS_POS_INTERVAL;
			}

			/*
			 * This is the most complex/hackish part of the allocator, but I failed to
			 * make it any simpler.
			 * FIXME FIXME FIXME: CLEAN THIS UP
			 */
			for (pos_interval = 0; pos_interval < pos_interval_limit; ++pos_interval) {
				spill_list = g_hash_table_lookup (ctx->split_positions, GUINT_TO_POINTER (pos));
				/* Insert stores first, then loads so registers don't get overwritten */
				for (iter = 0; iter < 2; ++iter) {
					for (l = spill_list; l; l = l->next) {
						MonoRegallocInterval *interval = l->data;

						/* The childs might be split */
						if (interval->child1->child1)
							child1 = child_at (interval->child1, pos - pos_interval);
						else
							child1 = interval->child1;
						if (pos < interval->child2->interval->range->from)
							/* Happens when volatile intervals are split */
							continue;
						child2 = child_at (interval->child2, pos);

						if ((child1->hreg == -1) && (child2->hreg == -1))
							/*
							 * Happens when an interval is split, then the first child
							 * is split again.
							 */
							continue;

						// FIXME: Why is !is_volatile needed ?
						// It seems to fail when the same volatile var is a source and a
						// destination of the same instruction
						if ((iter == 0) && (child1->hreg != -1) && (child2->hreg != -1) && !interval->is_volatile && pos_interval > 0) {
							int offset;

							/*
							 * This is complex situation: the vreg is expected to be in
							 * child1->hreg before the instruction, and in child2->hreg
							 * after the instruction. We can't insert a move before,
							 * because that could overwrite the input regs of the 
							 * instruction, and we can't insert a move after, since the
							 * instruction could overwrite the source reg of the move.
							 * Instead, we insert a store before the instruction, and a
							 * load afterwards.
							 * FIXME: Optimize child1->hreg == child2->hreg
							 */
							offset = alloc_spill_slot (cfg, sizeof (gpointer), sizeof (gpointer));

							NEW_STORE_MEMBASE (cfg, store, mono_type_to_store_membase (cfg, interval->type), cfg->frame_reg, offset, child1->hreg);

							mono_bblock_insert_after_ins (bb, prev, store);
							prev = store;
							g_hash_table_insert (ctx->spill_ins, store, store);

							NEW_LOAD_MEMBASE (cfg, load, mono_type_to_load_membase (cfg, interval->type), child2->hreg, cfg->frame_reg, offset);

							mono_bblock_insert_after_ins (bb, ins, load);
							g_hash_table_insert (ctx->spill_ins, load, load);

							LSCAN_DEBUG (printf (" Spill store/load added for R%d (R%d -> R%d) at %x\n", interval->vreg, child1->vreg, child2->vreg, pos));
						} else if ((iter == 0) && (child1->hreg != -1) && (child2->hreg != -1) && (child1->hreg != child2->hreg) && pos_interval == 0) {
							/* Happens with volatile intervals, i.e. in
							 * R1 <- FOO
							 * R2 <- OP R1 R2
							 * R1's interval is split between the two instructions.
							 */
							// FIXME: This should be done in iter 1, but it has 
							// ordering problems with other loads. Now it might have
							// ordering problems with stores.
							g_assert (!interval->fp);
							move = create_move (cfg, child2->hreg, child1->hreg);
							mono_bblock_insert_before_ins (bb, ins, move);
							prev = move;
							g_hash_table_insert (ctx->spill_ins, move, move);
						} else if ((iter == 0) && (child1->hreg != -1) && (child2->hreg == -1)) {
							g_assert (child2->offset != -1);

							NEW_STORE_MEMBASE (cfg, store, mono_type_to_store_membase (cfg, interval->type), cfg->frame_reg, child2->offset, child1->hreg);

							mono_bblock_insert_after_ins (bb, prev, store);
							prev = store;
							g_hash_table_insert (ctx->spill_ins, store, store);

							LSCAN_DEBUG (printf (" Spill store added for R%d (R%d -> R%d) at %x\n", interval->vreg, child1->vreg, child2->vreg, pos));
						} else if ((iter == 1) && (child1->hreg == -1) && (child2->hreg != -1)) {
							g_assert (child1->offset != -1);
							NEW_LOAD_MEMBASE (cfg, load, mono_type_to_load_membase (cfg, interval->type), child2->hreg, cfg->frame_reg, child1->offset);

							if (pos_interval >= INS_POS_DEF)
								/* Happens in InternalGetChars, couldn't create a testcase */
								mono_bblock_insert_after_ins (bb, ins, load);
							else {
								mono_bblock_insert_before_ins (bb, ins, load);
								prev = load;
							}
							g_hash_table_insert (ctx->spill_ins, load, load);

							LSCAN_DEBUG (printf (" Spill load added for R%d (R%d -> R%d) at %x\n", interval->vreg, child1->vreg, child2->vreg, pos));
						}
					}
				}

				pos ++;
			}

			if (G_UNLIKELY (cfg->verbose_level > 1))
				if (ins)
					mono_print_ins (ins);

			prev = ins;

			if (ins)
				ins = ins->next;
		}

		/* Second pass: Resolve data flow */
		for (bb_count = 0; bb_count < bb->out_count; ++bb_count) {
			out_bb = bb->out_bb [bb_count];

			if (!out_bb->live_in_set)
				/* Exception handling block */
				continue;

			from_pos = (bb->dfn << 16) + 0xffff;
			to_pos = (out_bb->dfn << 16);

			ins_to_add = NULL;
			for (i = 0; i < cfg->next_vreg; ++i) {
				MonoRegallocInterval *interval = &ctx->varinfo [i];

				if (mono_bitset_test_fast (out_bb->live_in_set, i) && mono_linterval_covers (interval->interval, from_pos) && mono_linterval_covers (interval->interval, to_pos)) {
					child1 = child_at (interval, from_pos);
					child2 = child_at (interval, to_pos);
					if (child1 != child2) {
						if ((child1->hreg != -1) && (child2->hreg == -1)) {
							LSCAN_DEBUG (printf (" Add store for R%d (R%d -> R%d) at BB%d -> BB%d [%x - %x]\n", interval->vreg, child1->vreg, child2->vreg, bb->block_num, out_bb->block_num, from_pos, to_pos));
							NEW_STORE_MEMBASE (cfg, store, mono_type_to_store_membase (cfg, interval->type), cfg->frame_reg, child2->offset, child1->hreg);
							ins_to_add = g_slist_prepend_mempool (cfg->mempool, ins_to_add, store);
							g_hash_table_insert (ctx->spill_ins, store, store);
						} else if ((child1->hreg != -1) && (child2->hreg != -1)) {
							if (child1->hreg != child2->hreg) {
								LSCAN_DEBUG (printf (" Add move for R%d (R%d -> R%d) at BB%d -> BB%d [%x - %x]\n", interval->vreg, child1->vreg, child2->vreg, bb->block_num, out_bb->block_num, from_pos, to_pos));
								NEW_UNALU (cfg, move, interval->fp ? OP_FMOVE : OP_MOVE, child2->hreg, child1->hreg);
								ins_to_add = g_slist_prepend_mempool (cfg->mempool, ins_to_add, move);
								g_hash_table_insert (ctx->spill_ins, move, move);
							}
						} else if ((child1->hreg == -1) && (child2->hreg != -1)) {
							LSCAN_DEBUG (printf (" Add load for R%d (R%d -> R%d) at BB%d -> BB%d [%x - %x]\n", interval->vreg, child1->vreg, child2->vreg, bb->block_num, out_bb->block_num, from_pos, to_pos));
							NEW_LOAD_MEMBASE (cfg, load, mono_type_to_load_membase (cfg, interval->type), child2->hreg, cfg->frame_reg, child1->offset);
							ins_to_add = g_slist_prepend_mempool (cfg->mempool, ins_to_add, load);
							g_hash_table_insert (ctx->spill_ins, load, load);
						} else {
							g_assert (child1->offset == child2->offset);
						}
					}
				}
			}

			if (bb->out_count == 1) {
				add_at_head = TRUE;
			} else if (out_bb->in_count == 1) {
				add_at_head = FALSE;
			} else {
				// FIXME: Split critical edges
				add_at_head = TRUE;
				NOT_IMPLEMENTED;
			}

			insert_after = NULL;

			if (ins_to_add) {
				MonoInst **moves;
				int nmoves;

				/*
				 * Emit spill instructions in such a way that instructions don't 
				 * overwrite the source registers of instructions coming after them.
				 */
				/* Simply emit stores, then moves then loads */
				for (l = ins_to_add; l; l = l->next) {
					MonoInst *ins = l->data;

					if (MONO_IS_STORE_MEMBASE (ins)) {
						if (add_at_head) {
							mono_add_ins_to_end (bb, ins);
						} else {
							mono_bblock_insert_after_ins (out_bb, insert_after, ins);
							insert_after = ins;
						}
					}
				}

				/* Collect the moves */
				nmoves = 0;
				for (l = ins_to_add; l; l = l->next) {
					MonoInst *ins = l->data;

					if (MONO_IS_MOVE (ins))
						nmoves ++;
				}
				moves = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoInst*) * nmoves);
				nmoves = 0;
				for (l = ins_to_add; l; l = l->next) {
					MonoInst *ins = l->data;

					if (MONO_IS_MOVE (ins))
						moves [nmoves ++] = ins;
				}

				moves_to_add = order_moves (cfg, ctx, moves, nmoves);

				for (l = moves_to_add; l; l = l->next) {
					MonoInst *ins = l->data;

					if (add_at_head) {
						mono_add_ins_to_end (bb, ins);
					} else {
						mono_bblock_insert_after_ins (out_bb, insert_after, ins);
						insert_after = ins;
					}
				}

				for (l = ins_to_add; l; l = l->next) {
					MonoInst *ins = l->data;

					if (MONO_IS_LOAD_MEMBASE (ins)) {
						if (add_at_head) {
							mono_add_ins_to_end (bb, ins);
						} else {
							mono_bblock_insert_after_ins (out_bb, insert_after, ins);
							insert_after = ins;
						}
					}
				}
			}
		}
	}
}

/*
 * rewrite_code:
 *
 *   Replace references to vregs with their assigned physical registers or spill 
 * locations.
 */
static void
rewrite_code (MonoCompile *cfg, MonoRegallocContext *ctx)
{
	MonoBasicBlock *bb;
	MonoInst *ins, *prev;
	int pos;
	MonoInst **defs;

	defs = g_new (MonoInst*, MONO_MAX_IREGS + MONO_MAX_FREGS);

	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		if (cfg->verbose_level > 1)
			printf ("\nREGALLOC-REWRITE BLOCK %d:\n", bb->block_num);

		memset (defs, 0, sizeof (MonoInst*) * (MONO_MAX_IREGS + MONO_MAX_FREGS));

		pos = (bb->dfn << 16);
		prev = NULL;
		MONO_BB_FOR_EACH_INS (bb, ins) {
			const char *spec = INS_INFO (ins->opcode);
			pos += INS_POS_INTERVAL;

			if (G_UNLIKELY (cfg->verbose_level > 1))
				mono_print_ins (ins);

			if (g_hash_table_lookup (ctx->spill_ins, ins)) {
				/* 
				 * This instruction was added after liveness info was computed, and thus
				 * screws up the pos calculation. The instruction already uses hregs.
				 */
				pos -= INS_POS_INTERVAL;
				prev = ins;
				continue;
			}

			/* FIXME: */
			if (ins->opcode == OP_NOP)
				continue;

			if (ins->opcode == OP_LDADDR) {
				MonoRegallocInterval *l = child_at (&ctx->varinfo [ins->dreg], pos + INS_POS_DEF);
				MonoInst *var = ins->inst_p0;
				MonoInst *move;

				g_assert (ctx->varinfo [var->dreg].hreg == -1);
				g_assert (ctx->varinfo [var->dreg].offset != -1);

				if (ctx->varinfo [var->dreg].offset != 0) {
					/*
					 * The ADD_IMM does not satisfy register constraints on x86/amd64.
					 */
					MONO_INST_NEW (cfg, move, OP_MOVE);
					move->dreg = l->hreg;
					move->sreg1 = cfg->frame_reg;
					mono_bblock_insert_before_ins (bb, ins, move);

					ins->opcode = OP_ADD_IMM;
					ins->dreg = l->hreg;
					ins->sreg1 = l->hreg;
					ins->inst_imm = ctx->varinfo [var->dreg].offset;
					defs [ins->dreg] = ins;
				} else {
					ins->opcode = OP_MOVE;
					ins->dreg = l->hreg;
					ins->sreg1 = cfg->frame_reg;
					defs [ins->dreg] = ins;
				}
				spec = INS_INFO (OP_NOP);

				/* 
				 * We need to fold these instructions into the instructions which
				 * use them, but we can't call mono_local_cprop () since that could
				 * generate code which doesn't obey register constraints.
				 * So we do it manually.
				 */
			}

			if (spec [MONO_INST_DEST] != ' ') {
				if (MONO_IS_STORE_MEMBASE (ins)) {
					MonoRegallocInterval *l = child_at (&ctx->varinfo [ins->dreg], pos + INS_POS_USE);
					g_assert (l->hreg != -1);
					ins->dreg = l->hreg;

					/* Fold the instruction computing the address */
					/* FIXME: fails in generics-sharing.2.exe
					def = defs [ins->dreg];
					if (def && def->opcode == OP_MOVE && def->sreg1 == cfg->frame_reg) {
						ins->dreg = cfg->frame_reg;
					} else if (def && def->opcode == OP_ADD_IMM && def->sreg1 == cfg->frame_reg) {
						ins->dreg = cfg->frame_reg;
						ins->inst_destbasereg += def->inst_imm;
					}
					*/
					/*
					 * FIXME: Deadce the def. This is hard to do, since it could be
					 * accessed in other bblocks.
					 */
				} else {
					MonoRegallocInterval *l = child_at (&ctx->varinfo [ins->dreg], pos + INS_POS_DEF);
					g_assert (l->hreg != -1);
					ins->dreg = l->hreg;
					defs [ins->dreg] = NULL;
				}
			}
			if (spec [MONO_INST_SRC1] != ' ') {
				MonoRegallocInterval *l = child_at (&ctx->varinfo [ins->sreg1], pos + INS_POS_USE);
				g_assert (l->hreg != -1);
				ins->sreg1 = l->hreg;

				/*
				def = defs [ins->sreg1];
				if (def && def->opcode == OP_MOVE && def->sreg1 == cfg->frame_reg)
					ins->sreg1 = cfg->frame_reg;
				*/
			}
			if (spec [MONO_INST_SRC2] != ' ') {
				MonoRegallocInterval *l = child_at (&ctx->varinfo [ins->sreg2], pos + INS_POS_USE);
				g_assert (l->hreg != -1);
				ins->sreg2 = l->hreg;
			}

			if (cfg->verbose_level > 1)
				mono_print_ins_index (1, ins);

			prev = ins;
		}
	}

	g_free (defs);
}

static MonoRegallocContext*
regalloc_ctx_create (MonoCompile *cfg)
{
	MonoRegallocContext *ctx;
	int i;

	ctx = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoRegallocContext));
	ctx->cfg = cfg;
	ctx->varinfo = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoRegallocInterval) * cfg->next_vreg);
	ctx->num_intervals = cfg->next_vreg;
	for (i = 0; i < cfg->next_vreg; ++i) {
		MonoInst *var;

		ctx->varinfo [i].vreg = i;
		ctx->varinfo [i].hreg = -1;
		ctx->varinfo [i].offset = -1;
		ctx->varinfo [i].preferred_reg = -1;

		if (i >= MONO_MAX_IREGS && i < MONO_MAX_IREGS + MONO_MAX_FREGS)
			ctx->varinfo [i].fp = TRUE;

		var = get_vreg_to_inst (cfg, i);
		if (var && (var != cfg->ret) && (var->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT))) {
			ctx->varinfo [i].is_volatile = TRUE;
		}
		if (var)
			ctx->varinfo [i].type = var->inst_vtype;
		else
			ctx->varinfo [i].type = sizeof (gpointer) == 8 ? &mono_defaults.int64_class->byval_arg : &mono_defaults.int_class->byval_arg;
	}

	ctx->split_positions = g_hash_table_new (NULL, NULL);
	ctx->split_position_set = g_hash_table_new (NULL, NULL);
	ctx->spill_ins = g_hash_table_new (NULL, NULL);

	return ctx;
}

void
mono_global_regalloc (MonoCompile *cfg)
{
	MonoRegallocContext *ctx;

	mono_arch_fill_argument_info (cfg);

	/* This could create vregs, so it has to come before ctx_create */
	handle_reg_constraints (cfg);

	ctx = regalloc_ctx_create (cfg);

	collect_fp_vregs (cfg, ctx);

	analyze_liveness (cfg, ctx);
	
	linear_scan (cfg, ctx);

	mono_arch_allocate_vars (cfg);

	assign_spill_slots (cfg, ctx);

	add_spill_code (cfg, ctx);

	rewrite_code (cfg, ctx);
}

#else

void
mono_global_regalloc (MonoCompile *cfg)
{
	NOT_IMPLEMENTED;
}

#endif
