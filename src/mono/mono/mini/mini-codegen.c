/**
 * \file
 * Arch independent code generation functionality
 *
 * (C) 2003 Ximian, Inc.
 */

#include "config.h"

#include <string.h>
#include <math.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif

#include <mono/metadata/appdomain.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/mempool-internals.h>
#include <mono/utils/mono-math.h>

#include "mini.h"
#include "mini-runtime.h"
#include "trace.h"
#include "mini-arch.h"

#ifndef DISABLE_JIT

#ifndef MONO_MAX_XREGS

#define MONO_MAX_XREGS 0
#define MONO_ARCH_CALLEE_SAVED_XREGS 0
#define MONO_ARCH_CALLEE_XREGS 0

#endif

#define MONO_ARCH_BANK_MIRRORED -2

#ifdef MONO_ARCH_USE_SHARED_FP_SIMD_BANK

#ifndef MONO_ARCH_NEED_SIMD_BANK
#error "MONO_ARCH_USE_SHARED_FP_SIMD_BANK needs MONO_ARCH_NEED_SIMD_BANK to work"
#endif

#define get_mirrored_bank(bank) (((bank) == MONO_REG_SIMD ) ? MONO_REG_DOUBLE : (((bank) == MONO_REG_DOUBLE ) ? MONO_REG_SIMD : -1))

#define is_hreg_mirrored(rs, bank, hreg) ((rs)->symbolic [(bank)] [(hreg)] == MONO_ARCH_BANK_MIRRORED)


#else


#define get_mirrored_bank(bank) (-1)

#define is_hreg_mirrored(rs, bank, hreg) (0)

#endif

#if _MSC_VER
#pragma warning(disable:4293) // FIXME negative shift is undefined
#endif

/* If the bank is mirrored return the true logical bank that the register in the
 * physical register bank is allocated to.
 */
static int translate_bank (MonoRegState *rs, int bank, int hreg) {
	return is_hreg_mirrored (rs, bank, hreg) ? get_mirrored_bank (bank) : bank;
}

/*
 * Every hardware register belongs to a register type or register bank. bank 0 
 * contains the int registers, bank 1 contains the fp registers.
 * int registers are used 99% of the time, so they are special cased in a lot of 
 * places.
 */

static const int regbank_size [] = {
	MONO_MAX_IREGS,
	MONO_MAX_FREGS,
	MONO_MAX_IREGS,
	MONO_MAX_IREGS,
	MONO_MAX_XREGS
};

static const int regbank_load_ops [] = { 
	OP_LOADR_MEMBASE,
	OP_LOADR8_MEMBASE,
	OP_LOADR_MEMBASE,
	OP_LOADR_MEMBASE,
	OP_LOADX_MEMBASE
};

static const int regbank_store_ops [] = { 
	OP_STORER_MEMBASE_REG,
	OP_STORER8_MEMBASE_REG,
	OP_STORER_MEMBASE_REG,
	OP_STORER_MEMBASE_REG,
	OP_STOREX_MEMBASE
};

static const int regbank_move_ops [] = { 
	OP_MOVE,
	OP_FMOVE,
	OP_MOVE,
	OP_MOVE,
	OP_XMOVE
};

#define regmask(reg) (((regmask_t)1) << (reg))

#ifdef MONO_ARCH_USE_SHARED_FP_SIMD_BANK
static const regmask_t regbank_callee_saved_regs [] = {
	MONO_ARCH_CALLEE_SAVED_REGS,
	MONO_ARCH_CALLEE_SAVED_FREGS,
	MONO_ARCH_CALLEE_SAVED_REGS,
	MONO_ARCH_CALLEE_SAVED_REGS,
	MONO_ARCH_CALLEE_SAVED_XREGS,
};
#endif

static const regmask_t regbank_callee_regs [] = {
	MONO_ARCH_CALLEE_REGS,
	MONO_ARCH_CALLEE_FREGS,
	MONO_ARCH_CALLEE_REGS,
	MONO_ARCH_CALLEE_REGS,
	MONO_ARCH_CALLEE_XREGS,
};

static const int regbank_spill_var_size[] = {
	sizeof (target_mgreg_t),
	sizeof (double),
	sizeof (target_mgreg_t),
	sizeof (target_mgreg_t),
	16 /*FIXME make this a constant. Maybe MONO_ARCH_SIMD_VECTOR_SIZE? */
};

#define DEBUG(a) MINI_DEBUG(cfg->verbose_level, 3, a;)

static void
mono_regstate_assign (MonoRegState *rs)
{
#ifdef MONO_ARCH_USE_SHARED_FP_SIMD_BANK
	/* The regalloc may fail if fp and simd logical regbanks share the same physical reg bank and
	 * if the values here are not the same.
	 */
	g_assert(regbank_callee_regs [MONO_REG_SIMD] == regbank_callee_regs [MONO_REG_DOUBLE]);
	g_assert(regbank_callee_saved_regs [MONO_REG_SIMD] == regbank_callee_saved_regs [MONO_REG_DOUBLE]);
	g_assert(regbank_size [MONO_REG_SIMD] == regbank_size [MONO_REG_DOUBLE]);
#endif

	if (rs->next_vreg > rs->vassign_size) {
		g_free (rs->vassign);
		rs->vassign_size = MAX (rs->next_vreg, 256);
		rs->vassign = (gint32 *)g_malloc (rs->vassign_size * sizeof (gint32));
	}

	memset (rs->isymbolic, 0, MONO_MAX_IREGS * sizeof (rs->isymbolic [0]));
	memset (rs->fsymbolic, 0, MONO_MAX_FREGS * sizeof (rs->fsymbolic [0]));

	rs->symbolic [MONO_REG_INT] = rs->isymbolic;
	rs->symbolic [MONO_REG_DOUBLE] = rs->fsymbolic;

#ifdef MONO_ARCH_NEED_SIMD_BANK
	memset (rs->xsymbolic, 0, MONO_MAX_XREGS * sizeof (rs->xsymbolic [0]));
	rs->symbolic [MONO_REG_SIMD] = rs->xsymbolic;
#endif
}

static int
mono_regstate_alloc_int (MonoRegState *rs, regmask_t allow)
{
	regmask_t mask = allow & rs->ifree_mask;

#if defined(__x86_64__) && defined(__GNUC__)
 {
	guint64 i;

	if (mask == 0)
		return -1;

	__asm__("bsfq %1,%0\n\t"
			: "=r" (i) : "rm" (mask));

	rs->ifree_mask &= ~ ((regmask_t)1 << i);
	return i;
 }
#else
	int i;

	for (i = 0; i < MONO_MAX_IREGS; ++i) {
		if (mask & ((regmask_t)1 << i)) {
			rs->ifree_mask &= ~ ((regmask_t)1 << i);
			return i;
		}
	}
	return -1;
#endif
}

static void
mono_regstate_free_int (MonoRegState *rs, int reg)
{
	if (reg >= 0) {
		rs->ifree_mask |= (regmask_t)1 << reg;
		rs->isymbolic [reg] = 0;
	}
}

static int
mono_regstate_alloc_general (MonoRegState *rs, regmask_t allow, int bank)
{
	int i;
	int mirrored_bank;
	regmask_t mask = allow & rs->free_mask [bank];
	for (i = 0; i < regbank_size [bank]; ++i) {
		if (mask & ((regmask_t)1 << i)) {
			rs->free_mask [bank] &= ~ ((regmask_t)1 << i);

			mirrored_bank = get_mirrored_bank (bank);
			if (mirrored_bank == -1)
				return i;

			rs->free_mask [mirrored_bank] = rs->free_mask [bank];
			return i;
		}
	}
	return -1;
}

static void
mono_regstate_free_general (MonoRegState *rs, int reg, int bank)
{
	int mirrored_bank;

	if (reg >= 0) {
		rs->free_mask [bank] |= (regmask_t)1 << reg;
		rs->symbolic [bank][reg] = 0;

		mirrored_bank = get_mirrored_bank (bank);
		if (mirrored_bank == -1)
			return;
		rs->free_mask [mirrored_bank] = rs->free_mask [bank];
		rs->symbolic [mirrored_bank][reg] = 0;
	}
}

const char*
mono_regname_full (int reg, int bank)
{
	if (G_UNLIKELY (bank)) {
#if MONO_ARCH_NEED_SIMD_BANK
		if (bank == MONO_REG_SIMD)
			return mono_arch_xregname (reg);
#endif
		if (bank == MONO_REG_INT_REF || bank == MONO_REG_INT_MP)
			return mono_arch_regname (reg);
		g_assert (bank == MONO_REG_DOUBLE);
		return mono_arch_fregname (reg);
	} else {
		return mono_arch_regname (reg);
	}
}

void
mono_call_inst_add_outarg_reg (MonoCompile *cfg, MonoCallInst *call, int vreg, int hreg, int bank)
{
	guint32 regpair;

	regpair = (((guint32)hreg) << 24) + vreg;
	if (G_UNLIKELY (bank)) {
		g_assert (vreg >= regbank_size [bank]);
		g_assert (hreg < regbank_size [bank]);
		call->used_fregs |= (regmask_t)1 << hreg;
		call->out_freg_args = g_slist_append_mempool (cfg->mempool, call->out_freg_args, (gpointer)(gssize)(regpair));
	} else {
		g_assert (vreg >= MONO_MAX_IREGS);
		g_assert (hreg < MONO_MAX_IREGS);
		call->used_iregs |= (regmask_t)1 << hreg;
		call->out_ireg_args = g_slist_append_mempool (cfg->mempool, call->out_ireg_args, (gpointer)(gssize)(regpair));
	}
}

/*
 * mono_call_inst_add_outarg_vt:
 *
 *   Register OUTARG_VT as belonging to CALL.
 */
void
mono_call_inst_add_outarg_vt (MonoCompile *cfg, MonoCallInst *call, MonoInst *outarg_vt)
{
	call->outarg_vts = g_slist_append_mempool (cfg->mempool, call->outarg_vts, outarg_vt);
}

static void
resize_spill_info (MonoCompile *cfg, int bank)
{
	MonoSpillInfo *orig_info = cfg->spill_info [bank];
	int orig_len = cfg->spill_info_len [bank];
	int new_len = orig_len ? orig_len * 2 : 16;
	MonoSpillInfo *new_info;
	int i;

	g_assert (bank < MONO_NUM_REGBANKS);

	new_info = (MonoSpillInfo *)mono_mempool_alloc0 (cfg->mempool, sizeof (MonoSpillInfo) * new_len);
	if (orig_info)
		memcpy (new_info, orig_info, sizeof (MonoSpillInfo) * orig_len);
	for (i = orig_len; i < new_len; ++i)
		new_info [i].offset = -1;

	cfg->spill_info [bank] = new_info;
	cfg->spill_info_len [bank] = new_len;
}

/*
 * returns the offset used by spillvar. It allocates a new
 * spill variable if necessary. 
 */
static int
mono_spillvar_offset (MonoCompile *cfg, int spillvar, int bank)
{
	MonoSpillInfo *info;
	int size;

	if (G_UNLIKELY (spillvar >= (cfg->spill_info_len [bank]))) {
		while (spillvar >= cfg->spill_info_len [bank])
			resize_spill_info (cfg, bank);
	}

	/*
	 * Allocate separate spill slots for fp/non-fp variables since most processors prefer it.
	 */
	info = &cfg->spill_info [bank][spillvar];
	if (info->offset == -1) {
		cfg->stack_offset += sizeof (target_mgreg_t) - 1;
		cfg->stack_offset &= ~(sizeof (target_mgreg_t) - 1);

		g_assert (bank < MONO_NUM_REGBANKS);
		if (G_UNLIKELY (bank))
			size = regbank_spill_var_size [bank];
		else
			size = sizeof (target_mgreg_t);

		if (cfg->flags & MONO_CFG_HAS_SPILLUP) {
			cfg->stack_offset += size - 1;
			cfg->stack_offset &= ~(size - 1);
			info->offset = cfg->stack_offset;
			cfg->stack_offset += size;
		} else {
			cfg->stack_offset += size - 1;
			cfg->stack_offset &= ~(size - 1);
			cfg->stack_offset += size;
			info->offset = - cfg->stack_offset;
		}
	}

	return info->offset;
}

#define is_hard_ireg(r) ((r) >= 0 && (r) < MONO_MAX_IREGS)
#define is_hard_freg(r) ((r) >= 0 && (r) < MONO_MAX_FREGS)
#define is_global_ireg(r) (is_hard_ireg ((r)) && (MONO_ARCH_CALLEE_SAVED_REGS & (regmask (r))))
#define is_local_ireg(r) (is_hard_ireg ((r)) && (MONO_ARCH_CALLEE_REGS & (regmask (r))))
#define is_global_freg(r) (is_hard_freg ((r)) && (MONO_ARCH_CALLEE_SAVED_FREGS & (regmask (r))))
#define is_local_freg(r) (is_hard_freg ((r)) && (MONO_ARCH_CALLEE_FREGS & (regmask (r))))

#define is_hard_reg(r,bank) (G_UNLIKELY (bank) ? ((r) >= 0 && (r) < regbank_size [bank]) : ((r) < MONO_MAX_IREGS))
#define is_soft_reg(r,bank) (!is_hard_reg((r),(bank)))
#define is_global_reg(r,bank) (G_UNLIKELY (bank) ? (is_hard_reg ((r), (bank)) && (regbank_callee_saved_regs [bank] & regmask (r))) : is_global_ireg (r))
#define is_local_reg(r,bank) (G_UNLIKELY (bank) ? (is_hard_reg ((r), (bank)) && (regbank_callee_regs [bank] & regmask (r))) : is_local_ireg (r))
#define reg_is_freeable(r,bank) (G_UNLIKELY (bank) ? is_local_reg ((r), (bank)) : is_local_ireg ((r)))

#ifndef MONO_ARCH_INST_IS_FLOAT
#define MONO_ARCH_INST_IS_FLOAT(desc) ((desc) == 'f')
#endif

#define reg_is_fp(desc) (MONO_ARCH_INST_IS_FLOAT (desc))
#define dreg_is_fp(spec)  (MONO_ARCH_INST_IS_FLOAT (spec [MONO_INST_DEST]))
#define sreg_is_fp(n,spec) (MONO_ARCH_INST_IS_FLOAT (spec [MONO_INST_SRC1+(n)]))
#define sreg1_is_fp(spec) sreg_is_fp (0,(spec))
#define sreg2_is_fp(spec) sreg_is_fp (1,(spec))

#define reg_is_simd(desc) ((desc) == 'x') 

#ifdef MONO_ARCH_NEED_SIMD_BANK

#define reg_bank(desc) (G_UNLIKELY (reg_is_fp (desc)) ? MONO_REG_DOUBLE : G_UNLIKELY (reg_is_simd(desc)) ? MONO_REG_SIMD : MONO_REG_INT)

#else

#define reg_bank(desc) reg_is_fp ((desc))

#endif

#define sreg_bank(n,spec) reg_bank ((spec)[MONO_INST_SRC1+(n)])
#define sreg1_bank(spec) sreg_bank (0, (spec))
#define sreg2_bank(spec) sreg_bank (1, (spec))
#define dreg_bank(spec) reg_bank ((spec)[MONO_INST_DEST])

#define sreg_bank_ins(n,ins) sreg_bank ((n), ins_get_spec ((ins)->opcode))
#define sreg1_bank_ins(ins) sreg_bank_ins (0, (ins))
#define sreg2_bank_ins(ins) sreg_bank_ins (1, (ins))
#define dreg_bank_ins(ins) dreg_bank (ins_get_spec ((ins)->opcode))

#define regpair_reg2_mask(desc,hreg1) ((MONO_ARCH_INST_REGPAIR_REG2 (desc,hreg1) != -1) ? (regmask (MONO_ARCH_INST_REGPAIR_REG2 (desc,hreg1))) : MONO_ARCH_CALLEE_REGS)

#ifdef MONO_ARCH_IS_GLOBAL_IREG
#undef is_global_ireg
#define is_global_ireg(reg) MONO_ARCH_IS_GLOBAL_IREG ((reg))
#endif

typedef struct {
	int born_in;
	int killed_in;
	/* Not (yet) used */
	//int last_use;
	//int prev_use;
	regmask_t preferred_mask; /* the hreg where the register should be allocated, or 0 */
} RegTrack;

#if !defined(DISABLE_LOGGING)

void
mono_print_ins_index (int i, MonoInst *ins)
{
	GString *buf = mono_print_ins_index_strbuf (i, ins);
	printf ("%s\n", buf->str);
	g_string_free (buf, TRUE);
}

GString *
mono_print_ins_index_strbuf (int i, MonoInst *ins)
{
	const char *spec = ins_get_spec (ins->opcode);
	GString *sbuf = g_string_new (NULL);
	int num_sregs, j;
	int sregs [MONO_MAX_SRC_REGS];

	if (i != -1)
		g_string_append_printf (sbuf, "\t%-2d %s", i, mono_inst_name (ins->opcode));
	else
		g_string_append_printf (sbuf, " %s", mono_inst_name (ins->opcode));
	if (spec == (gpointer)/*FIXME*/MONO_ARCH_CPU_SPEC) {
		gboolean dest_base = FALSE;
		switch (ins->opcode) {
		case OP_STOREV_MEMBASE:
			dest_base = TRUE;
			break;
		default:
			break;
		}

		/* This is a lowered opcode */
		if (ins->dreg != -1) {
			if (dest_base)
				g_string_append_printf (sbuf, " [R%d + 0x%lx] <-", ins->dreg, (long)ins->inst_offset);
			else
				g_string_append_printf (sbuf, " R%d <-", ins->dreg);
		}
		if (ins->sreg1 != -1)
			g_string_append_printf (sbuf, " R%d", ins->sreg1);
		if (ins->sreg2 != -1)
			g_string_append_printf (sbuf, " R%d", ins->sreg2);
		if (ins->sreg3 != -1)
			g_string_append_printf (sbuf, " R%d", ins->sreg3);

		switch (ins->opcode) {
		case OP_LBNE_UN:
		case OP_LBEQ:
		case OP_LBLT:
		case OP_LBLT_UN:
		case OP_LBGT:
		case OP_LBGT_UN:
		case OP_LBGE:
		case OP_LBGE_UN:
		case OP_LBLE:
		case OP_LBLE_UN:
			if (!ins->inst_false_bb)
				g_string_append_printf (sbuf, " [B%d]", ins->inst_true_bb->block_num);
			else
				g_string_append_printf (sbuf, " [B%dB%d]", ins->inst_true_bb->block_num, ins->inst_false_bb->block_num);
			break;
		case OP_PHI:
		case OP_VPHI:
		case OP_XPHI:
		case OP_FPHI: {
			int i;
			g_string_append_printf (sbuf, " [%d (", (int)ins->inst_c0);
			for (i = 0; i < ins->inst_phi_args [0]; i++) {
				if (i)
					g_string_append_printf (sbuf, ", ");
				g_string_append_printf (sbuf, "R%d", ins->inst_phi_args [i + 1]);
			}
			g_string_append_printf (sbuf, ")]");
			break;
		}
		case OP_LDADDR:
		case OP_OUTARG_VTRETADDR:
			g_string_append_printf (sbuf, " R%d", ((MonoInst*)ins->inst_p0)->dreg);
			break;
		case OP_REGOFFSET:
		case OP_GSHAREDVT_ARG_REGOFFSET:
			g_string_append_printf (sbuf, " + 0x%lx", (long)ins->inst_offset);
			break;
		case OP_ISINST:
		case OP_CASTCLASS:
			g_string_append_printf (sbuf, " %s", m_class_get_name (ins->klass));
			break;
		default:
			break;
		}

		//g_error ("Unknown opcode: %s\n", mono_inst_name (ins->opcode));
		return sbuf;
	}

	if (spec [MONO_INST_DEST]) {
		int bank = dreg_bank (spec);
		if (is_soft_reg (ins->dreg, bank)) {
			if (spec [MONO_INST_DEST] == 'b') {
				if (ins->inst_offset == 0)
					g_string_append_printf (sbuf, " [R%d] <-", ins->dreg);
				else
					g_string_append_printf (sbuf, " [R%d + 0x%lx] <-", ins->dreg, (long)ins->inst_offset);
			}
			else
				g_string_append_printf (sbuf, " R%d <-", ins->dreg);
		} else if (spec [MONO_INST_DEST] == 'b') {
			if (ins->inst_offset == 0)
				g_string_append_printf (sbuf, " [%s] <-", mono_arch_regname (ins->dreg));
			else
				g_string_append_printf (sbuf, " [%s + 0x%lx] <-", mono_arch_regname (ins->dreg), (long)ins->inst_offset);
		} else
			g_string_append_printf (sbuf, " %s <-", mono_regname_full (ins->dreg, bank));
	}
	if (spec [MONO_INST_SRC1]) {
		int bank = sreg1_bank (spec);
		if (is_soft_reg (ins->sreg1, bank)) {
			if (spec [MONO_INST_SRC1] == 'b')
				g_string_append_printf (sbuf, " [R%d + 0x%lx]", ins->sreg1, (long)ins->inst_offset);
			else
				g_string_append_printf (sbuf, " R%d", ins->sreg1);
		} else if (spec [MONO_INST_SRC1] == 'b')
			g_string_append_printf (sbuf, " [%s + 0x%lx]", mono_arch_regname (ins->sreg1), (long)ins->inst_offset);
		else
			g_string_append_printf (sbuf, " %s", mono_regname_full (ins->sreg1, bank));
	}
	num_sregs = mono_inst_get_src_registers (ins, sregs);
	for (j = 1; j < num_sregs; ++j) {
		int bank = sreg_bank (j, spec);
		if (is_soft_reg (sregs [j], bank))
			g_string_append_printf (sbuf, " R%d", sregs [j]);
		else
			g_string_append_printf (sbuf, " %s", mono_regname_full (sregs [j], bank));
	}

	switch (ins->opcode) {
	case OP_ICONST:
		g_string_append_printf (sbuf, " [%d]", (int)ins->inst_c0);
		break;
#if defined(TARGET_X86) || defined(TARGET_AMD64)
	case OP_X86_PUSH_IMM:
#endif
	case OP_ICOMPARE_IMM:
	case OP_COMPARE_IMM:
	case OP_IADD_IMM:
	case OP_ISUB_IMM:
	case OP_IAND_IMM:
	case OP_IOR_IMM:
	case OP_IXOR_IMM:
	case OP_SUB_IMM:
	case OP_MUL_IMM:
	case OP_STORE_MEMBASE_IMM:
		g_string_append_printf (sbuf, " [%d]", (int)ins->inst_imm);
		break;
	case OP_ADD_IMM:
	case OP_LADD_IMM:
		g_string_append_printf (sbuf, " [%d]", (int)(gssize)ins->inst_p1);
		break;
	case OP_I8CONST:
		g_string_append_printf (sbuf, " [%" PRId64 "]", (gint64)ins->inst_l);
		break;
	case OP_R8CONST:
		g_string_append_printf (sbuf, " [%f]", *(double*)ins->inst_p0);
		break;
	case OP_R4CONST:
		g_string_append_printf (sbuf, " [%f]", *(float*)ins->inst_p0);
		break;
	case OP_CALL:
	case OP_CALL_MEMBASE:
	case OP_CALL_REG:
	case OP_FCALL:
	case OP_LCALL:
	case OP_VCALL:
	case OP_VCALL_REG:
	case OP_VCALL_MEMBASE:
	case OP_VCALL2:
	case OP_VCALL2_REG:
	case OP_VCALL2_MEMBASE:
	case OP_VOIDCALL:
	case OP_VOIDCALL_MEMBASE:
	case OP_TAILCALL:
	case OP_TAILCALL_MEMBASE:
	case OP_RCALL:
	case OP_RCALL_REG:
	case OP_RCALL_MEMBASE: {
		MonoCallInst *call = (MonoCallInst*)ins;
		GSList *list;
		MonoJitICallId jit_icall_id;
		MonoMethod *method;

		if (ins->opcode == OP_VCALL || ins->opcode == OP_VCALL_REG || ins->opcode == OP_VCALL_MEMBASE) {
			/*
			 * These are lowered opcodes, but they are in the .md files since the old 
			 * JIT passes them to backends.
			 */
			if (ins->dreg != -1)
				g_string_append_printf (sbuf, " R%d <-", ins->dreg);
		}

		if ((method = call->method)) {
			char *full_name = mono_method_get_full_name (method);
			g_string_append_printf (sbuf, " [%s]", full_name);
			g_free (full_name);
		} else if (call->fptr_is_patch) {
			MonoJumpInfo *ji = (MonoJumpInfo*)call->fptr;

			g_string_append_printf (sbuf, " ");
			mono_print_ji (ji);
		} else if ((jit_icall_id = call->jit_icall_id)) {
			g_string_append_printf (sbuf, " [%s]", mono_find_jit_icall_info (jit_icall_id)->name);
		}

		list = call->out_ireg_args;
		while (list) {
			guint32 regpair;
			int reg, hreg;

			regpair = (guint32)(gssize)(list->data);
			hreg = regpair >> 24;
			reg = regpair & 0xffffff;

			g_string_append_printf (sbuf, " [%s <- R%d]", mono_arch_regname (hreg), reg);

			list = g_slist_next (list);
		}
		list = call->out_freg_args;
		while (list) {
			guint32 regpair;
			int reg, hreg;

			regpair = (guint32)(gssize)(list->data);
			hreg = regpair >> 24;
			reg = regpair & 0xffffff;

			g_string_append_printf (sbuf, " [%s <- R%d]", mono_arch_fregname (hreg), reg);

			list = g_slist_next (list);
		}
		break;
	}
	case OP_BR:
	case OP_CALL_HANDLER:
		g_string_append_printf (sbuf, " [B%d]", ins->inst_target_bb->block_num);
		break;
	case OP_IBNE_UN:
	case OP_IBEQ:
	case OP_IBLT:
	case OP_IBLT_UN:
	case OP_IBGT:
	case OP_IBGT_UN:
	case OP_IBGE:
	case OP_IBGE_UN:
	case OP_IBLE:
	case OP_IBLE_UN:
	case OP_LBNE_UN:
	case OP_LBEQ:
	case OP_LBLT:
	case OP_LBLT_UN:
	case OP_LBGT:
	case OP_LBGT_UN:
	case OP_LBGE:
	case OP_LBGE_UN:
	case OP_LBLE:
	case OP_LBLE_UN:
		if (!ins->inst_false_bb)
			g_string_append_printf (sbuf, " [B%d]", ins->inst_true_bb->block_num);
		else
			g_string_append_printf (sbuf, " [B%dB%d]", ins->inst_true_bb->block_num, ins->inst_false_bb->block_num);
		break;
	case OP_LIVERANGE_START:
	case OP_LIVERANGE_END:
	case OP_GC_LIVENESS_DEF:
	case OP_GC_LIVENESS_USE:
		g_string_append_printf (sbuf, " R%d", (int)ins->inst_c1);
		break;
	case OP_IL_SEQ_POINT:
	case OP_SEQ_POINT:
		g_string_append_printf (sbuf, "%s il: 0x%x%s", (ins->flags & MONO_INST_SINGLE_STEP_LOC) ? " intr" : "", (int)ins->inst_imm, ins->flags & MONO_INST_NONEMPTY_STACK ? ", nonempty-stack" : "");
		break;
	case OP_COND_EXC_EQ:
	case OP_COND_EXC_GE:
	case OP_COND_EXC_GT:
	case OP_COND_EXC_LE:
	case OP_COND_EXC_LT:
	case OP_COND_EXC_NE_UN:
	case OP_COND_EXC_GE_UN:
	case OP_COND_EXC_GT_UN:
	case OP_COND_EXC_LE_UN:
	case OP_COND_EXC_LT_UN:
	case OP_COND_EXC_OV:
	case OP_COND_EXC_NO:
	case OP_COND_EXC_C:
	case OP_COND_EXC_NC:
	case OP_COND_EXC_IEQ:
	case OP_COND_EXC_IGE:
	case OP_COND_EXC_IGT:
	case OP_COND_EXC_ILE:
	case OP_COND_EXC_ILT:
	case OP_COND_EXC_INE_UN:
	case OP_COND_EXC_IGE_UN:
	case OP_COND_EXC_IGT_UN:
	case OP_COND_EXC_ILE_UN:
	case OP_COND_EXC_ILT_UN:
	case OP_COND_EXC_IOV:
	case OP_COND_EXC_INO:
	case OP_COND_EXC_IC:
	case OP_COND_EXC_INC:
		g_string_append_printf (sbuf, " %s", (const char*)ins->inst_p1);
		break;
	default:
		break;
	}

	if (spec [MONO_INST_CLOB])
		g_string_append_printf (sbuf, " clobbers: %c", spec [MONO_INST_CLOB]);
	return sbuf;
}

static void
print_regtrack (RegTrack *t, int num)
{
	int i;
	char buf [32];
	const char *r;
	
	for (i = 0; i < num; ++i) {
		if (!t [i].born_in)
			continue;
		if (i >= MONO_MAX_IREGS) {
			g_snprintf (buf, sizeof (buf), "R%d", i);
			r = buf;
		} else
			r = mono_arch_regname (i);
		printf ("liveness: %s [%d - %d]\n", r, t [i].born_in, t[i].killed_in);
	}
}
#else

void
mono_print_ins_index (int i, MonoInst *ins)
{
}
#endif /* !defined(DISABLE_LOGGING) */

void
mono_print_ins (MonoInst *ins)
{
	mono_print_ins_index (-1, ins);
}

static void
insert_before_ins (MonoBasicBlock *bb, MonoInst *ins, MonoInst* to_insert)
{
	/*
	 * If this function is called multiple times, the new instructions are inserted
	 * in the proper order.
	 */
	mono_bblock_insert_before_ins (bb, ins, to_insert);
}

static void
insert_after_ins (MonoBasicBlock *bb, MonoInst *ins, MonoInst **last, MonoInst* to_insert)
{
	/*
	 * If this function is called multiple times, the new instructions are inserted in
	 * proper order.
	 */
	mono_bblock_insert_after_ins (bb, *last, to_insert);

	*last = to_insert;
}

static int
get_vreg_bank (MonoCompile *cfg, int reg, int bank)
{
	if (vreg_is_ref (cfg, reg))
		return MONO_REG_INT_REF;
	else if (vreg_is_mp (cfg, reg))
		return MONO_REG_INT_MP;
	else
		return bank;
}

/*
 * Force the spilling of the variable in the symbolic register 'reg', and free 
 * the hreg it was assigned to.
 */
static void
spill_vreg (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst **last, MonoInst *ins, int reg, int bank)
{
	MonoInst *load;
	int i, sel, spill;
	MonoRegState *rs = cfg->rs;

	sel = rs->vassign [reg];

	/* the vreg we need to spill lives in another logical reg bank */
	bank = translate_bank (cfg->rs, bank, sel);

	/*i = rs->isymbolic [sel];
	g_assert (i == reg);*/
	i = reg;
	spill = ++cfg->spill_count;
	rs->vassign [i] = -spill - 1;
	if (G_UNLIKELY (bank))
		mono_regstate_free_general (rs, sel, bank);
	else
		mono_regstate_free_int (rs, sel);
	/* we need to create a spill var and insert a load to sel after the current instruction */
	MONO_INST_NEW (cfg, load, regbank_load_ops [bank]);
	load->dreg = sel;
	load->inst_basereg = cfg->frame_reg;
	load->inst_offset = mono_spillvar_offset (cfg, spill, get_vreg_bank (cfg, reg, bank));
	insert_after_ins (bb, ins, last, load);
	DEBUG (printf ("SPILLED LOAD (%d at 0x%08lx(%%ebp)) R%d (freed %s)\n", spill, (long)load->inst_offset, i, mono_regname_full (sel, bank)));
	if (G_UNLIKELY (bank))
		i = mono_regstate_alloc_general (rs, regmask (sel), bank);
	else
		i = mono_regstate_alloc_int (rs, regmask (sel));
	g_assert (i == sel);

	if (G_UNLIKELY (bank))
		mono_regstate_free_general (rs, sel, bank);
	else
		mono_regstate_free_int (rs, sel);
}

static int
get_register_spilling (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst **last, MonoInst *ins, regmask_t regmask, int reg, int bank)
{
	MonoInst *load;
	int i, sel, spill, num_sregs;
	int sregs [MONO_MAX_SRC_REGS];
	MonoRegState *rs = cfg->rs;

	g_assert (bank < MONO_NUM_REGBANKS);

	DEBUG (printf ("\tstart regmask to assign R%d: 0x%08" PRIu64 " (R%d <- R%d R%d R%d)\n", reg, (guint64)regmask, ins->dreg, ins->sreg1, ins->sreg2, ins->sreg3));
	/* exclude the registers in the current instruction */
	num_sregs = mono_inst_get_src_registers (ins, sregs);
	for (i = 0; i < num_sregs; ++i) {
		if ((sreg_bank_ins (i, ins) == bank) && (reg != sregs [i]) && (reg_is_freeable (sregs [i], bank) || (is_soft_reg (sregs [i], bank) && rs->vassign [sregs [i]] >= 0))) {
			if (is_soft_reg (sregs [i], bank))
				regmask &= ~ (regmask (rs->vassign [sregs [i]]));
			else
				regmask &= ~ (regmask (sregs [i]));
			DEBUG (printf ("\t\texcluding sreg%d %s %d\n", i + 1, mono_regname_full (sregs [i], bank), sregs [i]));
		}
	}
	if ((dreg_bank_ins (ins) == bank) && (reg != ins->dreg) && reg_is_freeable (ins->dreg, bank)) {
		regmask &= ~ (regmask (ins->dreg));
		DEBUG (printf ("\t\texcluding dreg %s\n", mono_regname_full (ins->dreg, bank)));
	}

	DEBUG (printf ("\t\tavailable regmask: 0x%08" PRIu64 "\n", (guint64)regmask));
	g_assert (regmask); /* need at least a register we can free */
	sel = 0;
	/* we should track prev_use and spill the register that's farther */
	if (G_UNLIKELY (bank)) {
		for (i = 0; i < regbank_size [bank]; ++i) {
			if (regmask & (regmask (i))) {
				sel = i;

				/* the vreg we need to load lives in another logical bank */
				bank = translate_bank (cfg->rs, bank, sel);

				DEBUG (printf ("\t\tselected register %s has assignment %d\n", mono_regname_full (sel, bank), rs->symbolic [bank] [sel]));
				break;
			}
		}

		i = rs->symbolic [bank] [sel];
		spill = ++cfg->spill_count;
		rs->vassign [i] = -spill - 1;
		mono_regstate_free_general (rs, sel, bank);
	}
	else {
		for (i = 0; i < MONO_MAX_IREGS; ++i) {
			if (regmask & (regmask (i))) {
				sel = i;
				DEBUG (printf ("\t\tselected register %s has assignment %d\n", mono_arch_regname (sel), rs->isymbolic [sel]));
				break;
			}
		}

		i = rs->isymbolic [sel];
		spill = ++cfg->spill_count;
		rs->vassign [i] = -spill - 1;
		mono_regstate_free_int (rs, sel);
	}

	/* we need to create a spill var and insert a load to sel after the current instruction */
	MONO_INST_NEW (cfg, load, regbank_load_ops [bank]);
	load->dreg = sel;
	load->inst_basereg = cfg->frame_reg;
	load->inst_offset = mono_spillvar_offset (cfg, spill, get_vreg_bank (cfg, i, bank));
	insert_after_ins (bb, ins, last, load);
	DEBUG (printf ("\tSPILLED LOAD (%d at 0x%08lx(%%ebp)) R%d (freed %s)\n", spill, (long)load->inst_offset, i, mono_regname_full (sel, bank)));
	if (G_UNLIKELY (bank))
		i = mono_regstate_alloc_general (rs, regmask (sel), bank);
	else
		i = mono_regstate_alloc_int (rs, regmask (sel));
	g_assert (i == sel);
	
	return sel;
}

/*
 * free_up_hreg:
 *
 *   Free up the hreg HREG by spilling the vreg allocated to it.
 */
static void
free_up_hreg (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst **last, MonoInst *ins, int hreg, int bank)
{
	if (G_UNLIKELY (bank)) {
		if (!(cfg->rs->free_mask [bank] & (regmask (hreg)))) {
			bank = translate_bank (cfg->rs, bank, hreg);
			DEBUG (printf ("\tforced spill of R%d\n", cfg->rs->symbolic [bank] [hreg]));
			spill_vreg (cfg, bb, last, ins, cfg->rs->symbolic [bank] [hreg], bank);
		}
	}
	else {
		if (!(cfg->rs->ifree_mask & (regmask (hreg)))) {
			DEBUG (printf ("\tforced spill of R%d\n", cfg->rs->isymbolic [hreg]));
			spill_vreg (cfg, bb, last, ins, cfg->rs->isymbolic [hreg], bank);
		}
	}
}

static MonoInst*
create_copy_ins (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst **last, int dest, int src, MonoInst *ins, const unsigned char *ip, int bank)
{
	MonoInst *copy;

	MONO_INST_NEW (cfg, copy, regbank_move_ops [bank]);

	copy->dreg = dest;
	copy->sreg1 = src;
	copy->cil_code = ip;
	if (ins) {
		mono_bblock_insert_after_ins (bb, ins, copy);
		*last = copy;
	}
	DEBUG (printf ("\tforced copy from %s to %s\n", mono_regname_full (src, bank), mono_regname_full (dest, bank)));
	return copy;
}

static const char*
regbank_to_string (int bank)
{
	if (bank == MONO_REG_INT_REF)
		return "REF ";
	else if (bank == MONO_REG_INT_MP)
		return "MP ";
	else
		return "";
}

static void
create_spilled_store (MonoCompile *cfg, MonoBasicBlock *bb, int spill, int reg, int prev_reg, MonoInst **last, MonoInst *ins, MonoInst *insert_before, int bank)
{
	MonoInst *store, *def;
	
	bank = get_vreg_bank (cfg, prev_reg, bank);

	MONO_INST_NEW (cfg, store, regbank_store_ops [bank]);
	store->sreg1 = reg;
	store->inst_destbasereg = cfg->frame_reg;
	store->inst_offset = mono_spillvar_offset (cfg, spill, bank);
	if (ins) {
		mono_bblock_insert_after_ins (bb, ins, store);
		*last = store;
	} else if (insert_before) {
		insert_before_ins (bb, insert_before, store);
	} else {
		g_assert_not_reached ();
	}
	DEBUG (printf ("\t%sSPILLED STORE (%d at 0x%08lx(%%ebp)) R%d (from %s)\n", regbank_to_string (bank), spill, (long)store->inst_offset, prev_reg, mono_regname_full (reg, bank)));

	if (((bank == MONO_REG_INT_REF) || (bank == MONO_REG_INT_MP)) && cfg->compute_gc_maps) {
		g_assert (prev_reg != -1);
		MONO_INST_NEW (cfg, def, OP_GC_SPILL_SLOT_LIVENESS_DEF);
		def->inst_c0 = spill;
		def->inst_c1 = bank;
		mono_bblock_insert_after_ins (bb, store, def);
	}
}

/* flags used in reginfo->flags */
enum {
	MONO_FP_NEEDS_LOAD_SPILL	= regmask (0),
	MONO_FP_NEEDS_SPILL			= regmask (1),
	MONO_FP_NEEDS_LOAD			= regmask (2)
};

static int
alloc_int_reg (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst **last, MonoInst *ins, regmask_t dest_mask, int sym_reg, RegTrack *info)
{
	int val;

	if (info && info->preferred_mask) {
		val = mono_regstate_alloc_int (cfg->rs, info->preferred_mask & dest_mask);
		if (val >= 0) {
			DEBUG (printf ("\tallocated preferred reg R%d to %s\n", sym_reg, mono_arch_regname (val)));
			return val;
		}
	}

	val = mono_regstate_alloc_int (cfg->rs, dest_mask);
	if (val < 0)
		val = get_register_spilling (cfg, bb, last, ins, dest_mask, sym_reg, 0);

	return val;
}

static int
alloc_general_reg (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst **last, MonoInst *ins, regmask_t dest_mask, int sym_reg, int bank)
{
	int val;

	val = mono_regstate_alloc_general (cfg->rs, dest_mask, bank);

	if (val < 0)
		val = get_register_spilling (cfg, bb, last, ins, dest_mask, sym_reg, bank);

#ifdef MONO_ARCH_HAVE_TRACK_FPREGS
	cfg->arch.used_fp_regs |= 1 << val;
#endif
	return val;
}

static int
alloc_reg (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst **last, MonoInst *ins, regmask_t dest_mask, int sym_reg, RegTrack *info, int bank)
{
	if (G_UNLIKELY (bank))
		return alloc_general_reg (cfg, bb, last, ins, dest_mask, sym_reg, bank);
	else
		return alloc_int_reg (cfg, bb, last, ins, dest_mask, sym_reg, info);
}

static void
assign_reg (MonoCompile *cfg, MonoRegState *rs, int reg, int hreg, int bank)
{
	if (G_UNLIKELY (bank)) {
		int mirrored_bank;

		g_assert (reg >= regbank_size [bank]);
		g_assert (hreg < regbank_size [bank]);
		g_assert (! is_global_freg (hreg));

		rs->vassign [reg] = hreg;
		rs->symbolic [bank] [hreg] = reg;
		rs->free_mask [bank] &= ~ (regmask (hreg));

		mirrored_bank = get_mirrored_bank (bank);
		if (mirrored_bank == -1)
			return;

		/* Make sure the other logical reg bank that this bank shares
		 * a single hard reg bank knows that this hard reg is not free.
		 */
		rs->free_mask [mirrored_bank] = rs->free_mask [bank];

		/* Mark the other logical bank that the this bank shares
		 * a single hard reg bank with as mirrored.
		 */
		rs->symbolic [mirrored_bank] [hreg] = MONO_ARCH_BANK_MIRRORED;

	}
	else {
		g_assert (reg >= MONO_MAX_IREGS);
		g_assert (hreg < MONO_MAX_IREGS);
#if !defined(TARGET_ARM) && !defined(TARGET_ARM64)
		/* this seems to trigger a gcc compilation bug sometime (hreg is 0) */
		/* On arm64, rgctx_reg is a global hreg, and it is used to pass an argument */
		g_assert (! is_global_ireg (hreg));
#endif

		rs->vassign [reg] = hreg;
		rs->isymbolic [hreg] = reg;
		rs->ifree_mask &= ~ (regmask (hreg));
	}
}

static regmask_t
get_callee_mask (const char spec)
{
	if (G_UNLIKELY (reg_bank (spec)))
		return regbank_callee_regs [reg_bank (spec)];
	return MONO_ARCH_CALLEE_REGS;
}

static gint8 desc_to_fixed_reg [256];
static gboolean desc_to_fixed_reg_inited = FALSE;

/*
 * Local register allocation.
 * We first scan the list of instructions and we save the liveness info of
 * each register (when the register is first used, when it's value is set etc.).
 * We also reverse the list of instructions because assigning registers backwards allows 
 * for more tricks to be used.
 */
void
mono_local_regalloc (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins, *prev, *last;
	MonoInst **tmp;
	MonoRegState *rs = cfg->rs;
	int i, j, val, max;
	RegTrack *reginfo;
	const char *spec;
	unsigned char spec_src1, spec_dest;
	int bank = 0;
#if MONO_ARCH_USE_FPSTACK
	gboolean has_fp = FALSE;
	int fpstack [8];
	int sp = 0;
#endif
	int num_sregs = 0;
	int sregs [MONO_MAX_SRC_REGS];

	if (!bb->code)
		return;

	if (!desc_to_fixed_reg_inited) {
		for (i = 0; i < 256; ++i)
			desc_to_fixed_reg [i] = MONO_ARCH_INST_FIXED_REG (i);
		desc_to_fixed_reg_inited = TRUE;

		/* Validate the cpu description against the info in mini-ops.h */
#if defined(TARGET_AMD64) || defined(TARGET_X86) || defined(TARGET_ARM) || defined(TARGET_ARM64) || defined (TARGET_RISCV)
		/* Check that the table size is correct */
		g_assert (MONO_ARCH_CPU_SPEC_IDX(MONO_ARCH_CPU_SPEC)[OP_LAST - OP_LOAD] == 0xffff);
		for (i = OP_LOAD; i < OP_LAST; ++i) {
			const char *ispec;

			spec = ins_get_spec (i);
			ispec = INS_INFO (i);

			if ((spec [MONO_INST_DEST] && (ispec [MONO_INST_DEST] == ' ')))
				g_error ("Instruction metadata for %s inconsistent.\n", mono_inst_name (i));
			if ((spec [MONO_INST_SRC1] && (ispec [MONO_INST_SRC1] == ' ')))
				g_error ("Instruction metadata for %s inconsistent.\n", mono_inst_name (i));
			if ((spec [MONO_INST_SRC2] && (ispec [MONO_INST_SRC2] == ' ')))
				g_error ("Instruction metadata for %s inconsistent.\n", mono_inst_name (i));
		}
#endif
	}

	rs->next_vreg = bb->max_vreg;
	mono_regstate_assign (rs);

	rs->ifree_mask = MONO_ARCH_CALLEE_REGS;
	for (i = 0; i < MONO_NUM_REGBANKS; ++i)
		rs->free_mask [i] = regbank_callee_regs [i];

	max = rs->next_vreg;

	if (cfg->reginfo && cfg->reginfo_len < max)
		cfg->reginfo = NULL;

	reginfo = (RegTrack *)cfg->reginfo;
	if (!reginfo) {
		cfg->reginfo_len = MAX (1024, max * 2);
		reginfo = (RegTrack *)mono_mempool_alloc (cfg->mempool, sizeof (RegTrack) * cfg->reginfo_len);
		cfg->reginfo = reginfo;
	} 
	else
		g_assert (cfg->reginfo_len >= rs->next_vreg);

	if (cfg->verbose_level > 1) {
		/* print_regtrack reads the info of all variables */
		memset (cfg->reginfo, 0, cfg->reginfo_len * sizeof (RegTrack));
	}

	/* 
	 * For large methods, next_vreg can be very large, so g_malloc0 time can
	 * be prohibitive. So we manually init the reginfo entries used by the 
	 * bblock.
	 */
	for (ins = bb->code; ins; ins = ins->next) {
		gboolean modify = FALSE;

		spec = ins_get_spec (ins->opcode);

		if ((ins->dreg != -1) && (ins->dreg < max)) {
			memset (&reginfo [ins->dreg], 0, sizeof (RegTrack));
#if SIZEOF_REGISTER == 4
			if (MONO_ARCH_INST_IS_REGPAIR (spec [MONO_INST_DEST])) {
				/**
				 * In the new IR, the two vregs of the regpair do not alias the
				 * original long vreg. shift the vreg here so the rest of the 
				 * allocator doesn't have to care about it.
				 */
				ins->dreg ++;
				memset (&reginfo [ins->dreg + 1], 0, sizeof (RegTrack));
			}
#endif
		}

		num_sregs = mono_inst_get_src_registers (ins, sregs);
		for (j = 0; j < num_sregs; ++j) {
			g_assert (sregs [j] != -1);
			if (sregs [j] < max) {
				memset (&reginfo [sregs [j]], 0, sizeof (RegTrack));
#if SIZEOF_REGISTER == 4
				if (MONO_ARCH_INST_IS_REGPAIR (spec [MONO_INST_SRC1 + j])) {
					sregs [j]++;
					modify = TRUE;
					memset (&reginfo [sregs [j] + 1], 0, sizeof (RegTrack));
				}
#endif
			}
		}
		if (modify)
			mono_inst_set_src_registers (ins, sregs);
	}

	/*if (cfg->opt & MONO_OPT_COPYPROP)
		local_copy_prop (cfg, ins);*/

	i = 1;
	DEBUG (printf ("\nLOCAL REGALLOC BLOCK %d:\n", bb->block_num));
	/* forward pass on the instructions to collect register liveness info */
	MONO_BB_FOR_EACH_INS (bb, ins) {
		spec = ins_get_spec (ins->opcode);
		spec_dest = spec [MONO_INST_DEST];

		if (G_UNLIKELY (spec == (gpointer)/*FIXME*/MONO_ARCH_CPU_SPEC)) {
			g_error ("Opcode '%s' missing from machine description file.", mono_inst_name (ins->opcode));
		}
		
		DEBUG (mono_print_ins_index (i, ins));

		num_sregs = mono_inst_get_src_registers (ins, sregs);

#if MONO_ARCH_USE_FPSTACK
		if (dreg_is_fp (spec)) {
			has_fp = TRUE;
		} else {
			for (j = 0; j < num_sregs; ++j) {
				if (sreg_is_fp (j, spec))
					has_fp = TRUE;
			}
		}
#endif

		for (j = 0; j < num_sregs; ++j) {
			int sreg = sregs [j];
			int sreg_spec = spec [MONO_INST_SRC1 + j];
			if (sreg_spec) {
				bank = sreg_bank (j, spec);
				g_assert (sreg != -1);
				if (is_soft_reg (sreg, bank))
					/* This means the vreg is not local to this bb */
					g_assert (reginfo [sreg].born_in > 0);
				rs->vassign [sreg] = -1;
				//reginfo [ins->sreg2].prev_use = reginfo [ins->sreg2].last_use;
				//reginfo [ins->sreg2].last_use = i;
				if (MONO_ARCH_INST_IS_REGPAIR (sreg_spec)) {
					/* The virtual register is allocated sequentially */
					rs->vassign [sreg + 1] = -1;
					//reginfo [ins->sreg2 + 1].prev_use = reginfo [ins->sreg2 + 1].last_use;
					//reginfo [ins->sreg2 + 1].last_use = i;
					if (reginfo [sreg + 1].born_in == 0 || reginfo [sreg + 1].born_in > i)
						reginfo [sreg + 1].born_in = i;
				}
			} else {
				sregs [j] = -1;
			}
		}
		mono_inst_set_src_registers (ins, sregs);

		if (spec_dest) {
			int dest_dreg;

			bank = dreg_bank (spec);
			if (spec_dest != 'b') /* it's not just a base register */
				reginfo [ins->dreg].killed_in = i;
			g_assert (ins->dreg != -1);
			rs->vassign [ins->dreg] = -1;
			//reginfo [ins->dreg].prev_use = reginfo [ins->dreg].last_use;
			//reginfo [ins->dreg].last_use = i;
			if (reginfo [ins->dreg].born_in == 0 || reginfo [ins->dreg].born_in > i)
				reginfo [ins->dreg].born_in = i;

			dest_dreg = desc_to_fixed_reg [spec_dest];
			if (dest_dreg != -1)
				reginfo [ins->dreg].preferred_mask = (regmask (dest_dreg));

#ifdef MONO_ARCH_INST_FIXED_MASK
			reginfo [ins->dreg].preferred_mask |= MONO_ARCH_INST_FIXED_MASK (spec_dest);
#endif

			if (MONO_ARCH_INST_IS_REGPAIR (spec_dest)) {
				/* The virtual register is allocated sequentially */
				rs->vassign [ins->dreg + 1] = -1;
				//reginfo [ins->dreg + 1].prev_use = reginfo [ins->dreg + 1].last_use;
				//reginfo [ins->dreg + 1].last_use = i;
				if (reginfo [ins->dreg + 1].born_in == 0 || reginfo [ins->dreg + 1].born_in > i)
					reginfo [ins->dreg + 1].born_in = i;
				if (MONO_ARCH_INST_REGPAIR_REG2 (spec_dest, -1) != -1)
					reginfo [ins->dreg + 1].preferred_mask = regpair_reg2_mask (spec_dest, -1);
			}
		} else {
			ins->dreg = -1;
		}

		++i;
	}

	tmp = &last;

	DEBUG (print_regtrack (reginfo, rs->next_vreg));
	MONO_BB_FOR_EACH_INS_REVERSE_SAFE (bb, prev, ins) {
		int prev_dreg;
		int dest_dreg, clob_reg;
		int dest_sregs [MONO_MAX_SRC_REGS], prev_sregs [MONO_MAX_SRC_REGS];
		int dreg_high, sreg1_high;
		regmask_t dreg_mask, mask;
		regmask_t sreg_masks [MONO_MAX_SRC_REGS], sreg_fixed_masks [MONO_MAX_SRC_REGS];
		regmask_t dreg_fixed_mask;
		const unsigned char *ip;
		--i;
		spec = ins_get_spec (ins->opcode);
		spec_src1 = spec [MONO_INST_SRC1];
		spec_dest = spec [MONO_INST_DEST];
		prev_dreg = -1;
		clob_reg = -1;
		dest_dreg = -1;
		dreg_high = -1;
		sreg1_high = -1;
		dreg_mask = get_callee_mask (spec_dest);
		for (j = 0; j < MONO_MAX_SRC_REGS; ++j) {
			prev_sregs [j] = -1;
			sreg_masks [j] = get_callee_mask (spec [MONO_INST_SRC1 + j]);
			dest_sregs [j] = desc_to_fixed_reg [(int)spec [MONO_INST_SRC1 + j]];
#ifdef MONO_ARCH_INST_FIXED_MASK
			sreg_fixed_masks [j] = MONO_ARCH_INST_FIXED_MASK (spec [MONO_INST_SRC1 + j]);
#else
			sreg_fixed_masks [j] = 0;
#endif
		}

		DEBUG (printf ("processing:"));
		DEBUG (mono_print_ins_index (i, ins));

		ip = ins->cil_code;

		last = ins;

		/*
		 * FIXED REGS
		 */
		dest_dreg = desc_to_fixed_reg [spec_dest];
		clob_reg = desc_to_fixed_reg [(int)spec [MONO_INST_CLOB]];
		sreg_masks [1] &= ~ (MONO_ARCH_INST_SREG2_MASK (spec));

#ifdef MONO_ARCH_INST_FIXED_MASK
		dreg_fixed_mask = MONO_ARCH_INST_FIXED_MASK (spec_dest);
#else
		dreg_fixed_mask = 0;
#endif

		num_sregs = mono_inst_get_src_registers (ins, sregs);

		/*
		 * TRACK FIXED SREG2, 3, ...
		 */
		for (j = 1; j < num_sregs; ++j) {
			int sreg = sregs [j];
			int dest_sreg = dest_sregs [j];

			if (dest_sreg == -1)
				continue;

			if (j == 2) {
				int k;

				/*
				 * CAS.
				 * We need to special case this, since on x86, there are only 3
				 * free registers, and the code below assigns one of them to
				 * sreg, so we can run out of registers when trying to assign
				 * dreg. Instead, we just set up the register masks, and let the
				 * normal sreg2 assignment code handle this. It would be nice to
				 * do this for all the fixed reg cases too, but there is too much
				 * risk of breakage.
				 */

				/* Make sure sreg will be assigned to dest_sreg, and the other sregs won't */
				sreg_masks [j] = regmask (dest_sreg);
				for (k = 0; k < num_sregs; ++k) {
					if (k != j)
						sreg_masks [k] &= ~ (regmask (dest_sreg));
				}						

				/*
				 * Spill sreg1/2 if they are assigned to dest_sreg.
				 */
				for (k = 0; k < num_sregs; ++k) {
					if (k != j && is_soft_reg (sregs [k], 0) && rs->vassign [sregs [k]] == dest_sreg)
						free_up_hreg (cfg, bb, tmp, ins, dest_sreg, 0);
				}

				/*
				 * We can also run out of registers while processing sreg2 if sreg3 is
				 * assigned to another hreg, so spill sreg3 now.
				 */
				if (is_soft_reg (sreg, 0) && rs->vassign [sreg] >= 0 && rs->vassign [sreg] != dest_sreg) {
					spill_vreg (cfg, bb, tmp, ins, sreg, 0);
				}
				continue;
			}

			if (rs->ifree_mask & (regmask (dest_sreg))) {
				if (is_global_ireg (sreg)) {
					int k;
					/* Argument already in hard reg, need to copy */
					MonoInst *copy = create_copy_ins (cfg, bb, tmp, dest_sreg, sreg, NULL, ip, 0);
					insert_before_ins (bb, ins, copy);
					for (k = 0; k < num_sregs; ++k) {
						if (k != j)
							sreg_masks [k] &= ~ (regmask (dest_sreg));
					}
					/* See below */
					dreg_mask &= ~ (regmask (dest_sreg));
				} else {
					val = rs->vassign [sreg];
					if (val == -1) {
						DEBUG (printf ("\tshortcut assignment of R%d to %s\n", sreg, mono_arch_regname (dest_sreg)));
						assign_reg (cfg, rs, sreg, dest_sreg, 0);
					} else if (val < -1) {
						/* FIXME: */
						g_assert_not_reached ();
					} else {
						/* Argument already in hard reg, need to copy */
						MonoInst *copy = create_copy_ins (cfg, bb, tmp, dest_sreg, val, NULL, ip, 0);
						int k;

						insert_before_ins (bb, ins, copy);
						for (k = 0; k < num_sregs; ++k) {
							if (k != j)
								sreg_masks [k] &= ~ (regmask (dest_sreg));
						}
						/* 
						 * Prevent the dreg from being allocated to dest_sreg
						 * too, since it could force sreg1 to be allocated to 
						 * the same reg on x86.
						 */
						dreg_mask &= ~ (regmask (dest_sreg));
					}
				}
			} else {
				gboolean need_spill = TRUE;
				gboolean need_assign = TRUE;
				int k;

				dreg_mask &= ~ (regmask (dest_sreg));
				for (k = 0; k < num_sregs; ++k) {
					if (k != j)
						sreg_masks [k] &= ~ (regmask (dest_sreg));
				}

				/* 
				 * First check if dreg is assigned to dest_sreg2, since we
				 * can't spill a dreg.
				 */
				if (spec [MONO_INST_DEST])
					val = rs->vassign [ins->dreg];
				else
					val = -1;
				if (val == dest_sreg && ins->dreg != sreg) {
					/* 
					 * the destination register is already assigned to 
					 * dest_sreg2: we need to allocate another register for it 
					 * and then copy from this to dest_sreg2.
					 */
					int new_dest;
					new_dest = alloc_int_reg (cfg, bb, tmp, ins, dreg_mask, ins->dreg, &reginfo [ins->dreg]);
					g_assert (new_dest >= 0);
					DEBUG (printf ("\tchanging dreg R%d to %s from %s\n", ins->dreg, mono_arch_regname (new_dest), mono_arch_regname (dest_sreg)));

					prev_dreg = ins->dreg;
					assign_reg (cfg, rs, ins->dreg, new_dest, 0);
					create_copy_ins (cfg, bb, tmp, dest_sreg, new_dest, ins, ip, 0);
					mono_regstate_free_int (rs, dest_sreg);
					need_spill = FALSE;
				}

				if (is_global_ireg (sreg)) {
					MonoInst *copy = create_copy_ins (cfg, bb, tmp, dest_sreg, sreg, NULL, ip, 0);
					insert_before_ins (bb, ins, copy);
					need_assign = FALSE;
				}
				else {
					val = rs->vassign [sreg];
					if (val == dest_sreg) {
						/* sreg2 is already assigned to the correct register */
						need_spill = FALSE;
					} else if (val < -1) {
						/* sreg2 is spilled, it can be assigned to dest_sreg2 */
					} else if (val >= 0) {
						/* sreg2 already assigned to another register */
						/*
						 * We couldn't emit a copy from val to dest_sreg2, because
						 * val might be spilled later while processing this 
						 * instruction. So we spill sreg2 so it can be allocated to
						 * dest_sreg2.
						 */
						free_up_hreg (cfg, bb, tmp, ins, val, 0);
					}
				}

				if (need_spill) {
					free_up_hreg (cfg, bb, tmp, ins, dest_sreg, 0);
				}

				if (need_assign) {
					if (rs->vassign [sreg] < -1) {
						int spill;

						/* Need to emit a spill store */
						spill = - rs->vassign [sreg] - 1;
						create_spilled_store (cfg, bb, spill, dest_sreg, sreg, tmp, NULL, ins, bank);
					}
					/* force-set sreg2 */
					assign_reg (cfg, rs, sregs [j], dest_sreg, 0);
				}
			}
			sregs [j] = dest_sreg;
		}
		mono_inst_set_src_registers (ins, sregs);

		/*
		 * TRACK DREG
		 */
		bank = dreg_bank (spec);
		if (spec_dest && is_soft_reg (ins->dreg, bank)) {
			prev_dreg = ins->dreg;
		}

		if (spec_dest == 'b') {
			/* 
			 * The dest reg is read by the instruction, not written, so
			 * avoid allocating sreg1/sreg2 to the same reg.
			 */
			if (dest_sregs [0] != -1)
				dreg_mask &= ~ (regmask (dest_sregs [0]));
			for (j = 1; j < num_sregs; ++j) {
				if (dest_sregs [j] != -1)
					dreg_mask &= ~ (regmask (dest_sregs [j]));
			}

			val = rs->vassign [ins->dreg];
			if (is_soft_reg (ins->dreg, bank) && (val >= 0) && (!(regmask (val) & dreg_mask))) {
				/* DREG is already allocated to a register needed for sreg1 */
			    spill_vreg (cfg, bb, tmp, ins, ins->dreg, 0);
			}
		}

		/*
		 * If dreg is a fixed regpair, free up both of the needed hregs to avoid
		 * various complex situations.
		 */
		if (MONO_ARCH_INST_IS_REGPAIR (spec_dest)) {
			guint32 dreg2, dest_dreg2;

			g_assert (is_soft_reg (ins->dreg, bank));

			if (dest_dreg != -1) {
				if (rs->vassign [ins->dreg] != dest_dreg)
					free_up_hreg (cfg, bb, tmp, ins, dest_dreg, 0);

				dreg2 = ins->dreg + 1;
				dest_dreg2 = MONO_ARCH_INST_REGPAIR_REG2 (spec_dest, dest_dreg);
				if (dest_dreg2 != -1) {
					if (rs->vassign [dreg2] != dest_dreg2)
						free_up_hreg (cfg, bb, tmp, ins, dest_dreg2, 0);
				}
			}
		}

		if (dreg_fixed_mask) {
			g_assert (!bank);
			if (is_global_ireg (ins->dreg)) {
				/* 
				 * The argument is already in a hard reg, but that reg is
				 * not usable by this instruction, so allocate a new one.
				 */
				val = mono_regstate_alloc_int (rs, dreg_fixed_mask);
				if (val < 0)
					val = get_register_spilling (cfg, bb, tmp, ins, dreg_fixed_mask, -1, bank);
				mono_regstate_free_int (rs, val);
				dest_dreg = val;

				/* Fall through */
			}
			else
				dreg_mask &= dreg_fixed_mask;
		}

		if (is_soft_reg (ins->dreg, bank)) {
			val = rs->vassign [ins->dreg];

			if (val < 0) {
				int spill = 0;
				if (val < -1) {
					/* the register gets spilled after this inst */
					spill = -val -1;
				}
				val = alloc_reg (cfg, bb, tmp, ins, dreg_mask, ins->dreg, &reginfo [ins->dreg], bank);
				assign_reg (cfg, rs, ins->dreg, val, bank);
				if (spill)
					create_spilled_store (cfg, bb, spill, val, prev_dreg, tmp, ins, NULL, bank);
			}

			DEBUG (printf ("\tassigned dreg %s to dest R%d\n", mono_regname_full (val, bank), ins->dreg));
			ins->dreg = val;
		}

		/* Handle regpairs */
		if (MONO_ARCH_INST_IS_REGPAIR (spec_dest)) {
			int reg2 = prev_dreg + 1;

			g_assert (!bank);
			g_assert (prev_dreg > -1);
			g_assert (!is_global_ireg (rs->vassign [prev_dreg]));
			mask = regpair_reg2_mask (spec_dest, rs->vassign [prev_dreg]);
#ifdef TARGET_X86
			/* bug #80489 */
			mask &= ~regmask (X86_ECX);
#endif
			val = rs->vassign [reg2];
			if (val < 0) {
				int spill = 0;
				if (val < -1) {
					/* the register gets spilled after this inst */
					spill = -val -1;
				}
				val = mono_regstate_alloc_int (rs, mask);
				if (val < 0)
					val = get_register_spilling (cfg, bb, tmp, ins, mask, reg2, bank);
				if (spill)
					create_spilled_store (cfg, bb, spill, val, reg2, tmp, ins, NULL, bank);
			}
			else {
				if (! (mask & (regmask (val)))) {
					val = mono_regstate_alloc_int (rs, mask);
					if (val < 0)
						val = get_register_spilling (cfg, bb, tmp, ins, mask, reg2, bank);

					/* Reallocate hreg to the correct register */
					create_copy_ins (cfg, bb, tmp, rs->vassign [reg2], val, ins, ip, bank);

					mono_regstate_free_int (rs, rs->vassign [reg2]);
				}
			}					

			DEBUG (printf ("\tassigned dreg-high %s to dest R%d\n", mono_arch_regname (val), reg2));
			assign_reg (cfg, rs, reg2, val, bank);

			dreg_high = val;
			ins->backend.reg3 = val;

			if (reg_is_freeable (val, bank) && reg2 >= 0 && (reginfo [reg2].born_in >= i)) {
				DEBUG (printf ("\tfreeable %s (R%d)\n", mono_arch_regname (val), reg2));
				mono_regstate_free_int (rs, val);
			}
		}

		if (prev_dreg >= 0 && is_soft_reg (prev_dreg, bank) && (spec_dest != 'b')) {
			/* 
			 * In theory, we could free up the hreg even if the vreg is alive,
			 * but branches inside bblocks force us to assign the same hreg
			 * to a vreg every time it is encountered.
			 */
			int dreg = rs->vassign [prev_dreg];
			g_assert (dreg >= 0);
			DEBUG (printf ("\tfreeable %s (R%d) (born in %d)\n", mono_regname_full (dreg, bank), prev_dreg, reginfo [prev_dreg].born_in));
			if (G_UNLIKELY (bank))
				mono_regstate_free_general (rs, dreg, bank);
			else
				mono_regstate_free_int (rs, dreg);
			rs->vassign [prev_dreg] = -1;
		}

		if ((dest_dreg != -1) && (ins->dreg != dest_dreg)) {
			/* this instruction only outputs to dest_dreg, need to copy */
			create_copy_ins (cfg, bb, tmp, ins->dreg, dest_dreg, ins, ip, bank);
			ins->dreg = dest_dreg;

			if (G_UNLIKELY (bank)) {
				/* the register we need to free up may be used in another logical regbank
				 * so do a translate just in case.
				 */
				int translated_bank = translate_bank (cfg->rs, bank, dest_dreg);
				if (rs->symbolic [translated_bank] [dest_dreg] >= regbank_size [translated_bank])
					free_up_hreg (cfg, bb, tmp, ins, dest_dreg, translated_bank);
			}
			else {
				if (rs->isymbolic [dest_dreg] >= MONO_MAX_IREGS)
					free_up_hreg (cfg, bb, tmp, ins, dest_dreg, bank);
			}
		}

		if (spec_dest == 'b') {
			/* 
			 * The dest reg is read by the instruction, not written, so
			 * avoid allocating sreg1/sreg2 to the same reg.
			 */
			for (j = 0; j < num_sregs; ++j)
				if (!sreg_bank (j, spec))
					sreg_masks [j] &= ~ (regmask (ins->dreg));
		}

		/*
		 * TRACK CLOBBERING
		 */
		if ((clob_reg != -1) && (!(rs->ifree_mask & (regmask (clob_reg))))) {
			DEBUG (printf ("\tforced spill of clobbered reg R%d\n", rs->isymbolic [clob_reg]));
			free_up_hreg (cfg, bb, tmp, ins, clob_reg, 0);
		}

		if (spec [MONO_INST_CLOB] == 'c') {
			int j, dreg, dreg2, cur_bank;
			regmask_t s;
			guint64 clob_mask;

			clob_mask = MONO_ARCH_CALLEE_REGS;

			if (rs->ifree_mask != MONO_ARCH_CALLEE_REGS) {
				/*
				 * Need to avoid spilling the dreg since the dreg is not really
				 * clobbered by the call.
				 */
				if ((prev_dreg != -1) && !reg_bank (spec_dest))
					dreg = rs->vassign [prev_dreg];
				else
					dreg = -1;

				if (MONO_ARCH_INST_IS_REGPAIR (spec_dest))
					dreg2 = rs->vassign [prev_dreg + 1];
				else
					dreg2 = -1;

				for (j = 0; j < MONO_MAX_IREGS; ++j) {
					s = regmask (j);
					if ((clob_mask & s) && !(rs->ifree_mask & s) && (j != ins->sreg1)) {
						if ((j != dreg) && (j != dreg2))
							free_up_hreg (cfg, bb, tmp, ins, j, 0);
						else if (rs->isymbolic [j])
							/* The hreg is assigned to the dreg of this instruction */
							rs->vassign [rs->isymbolic [j]] = -1;
						mono_regstate_free_int (rs, j);
					}
				}
			}

			for (cur_bank = 1; cur_bank < MONO_NUM_REGBANKS; ++ cur_bank) {
				if (rs->free_mask [cur_bank] != regbank_callee_regs [cur_bank]) {
					clob_mask = regbank_callee_regs [cur_bank];
					if ((prev_dreg != -1) && reg_bank (spec_dest))
						dreg = rs->vassign [prev_dreg];
					else
						dreg = -1;

					for (j = 0; j < regbank_size [cur_bank]; ++j) {

						/* we are looping though the banks in the outer loop
						 * so, we don't need to deal with mirrored hregs
						 * because we will get them in one of the other bank passes.
						 */
						if (is_hreg_mirrored (rs, cur_bank, j))
							continue;

						s = regmask (j);
						if ((clob_mask & s) && !(rs->free_mask [cur_bank] & s)) {
							if (j != dreg)
								free_up_hreg (cfg, bb, tmp, ins, j, cur_bank);
							else if (rs->symbolic [cur_bank] [j])
								/* The hreg is assigned to the dreg of this instruction */
								rs->vassign [rs->symbolic [cur_bank] [j]] = -1;
							mono_regstate_free_general (rs, j, cur_bank);
						}
					}
				}
			}
		}

		/*
		 * TRACK ARGUMENT REGS
		 */
		if (spec [MONO_INST_CLOB] == 'c' && MONO_IS_CALL (ins)) {
			MonoCallInst *call = (MonoCallInst*)ins;
			GSList *list;

			/* 
			 * This needs to be done before assigning sreg1, so sreg1 will
			 * not be assigned one of the argument regs.
			 */

			/* 
			 * Assign all registers in call->out_reg_args to the proper 
			 * argument registers.
			 */

			list = call->out_ireg_args;
			if (list) {
				while (list) {
					guint32 regpair;
					int reg, hreg;

					regpair = (guint32)(gssize)(list->data);
					hreg = regpair >> 24;
					reg = regpair & 0xffffff;

					assign_reg (cfg, rs, reg, hreg, 0);

					sreg_masks [0] &= ~(regmask (hreg));

					DEBUG (printf ("\tassigned arg reg %s to R%d\n", mono_arch_regname (hreg), reg));

					list = g_slist_next (list);
				}
			}

			list = call->out_freg_args;
			if (list) {
				while (list) {
					guint32 regpair;
					int reg, hreg;

					regpair = (guint32)(gssize)(list->data);
					hreg = regpair >> 24;
					reg = regpair & 0xffffff;

					assign_reg (cfg, rs, reg, hreg, 1);

					DEBUG (printf ("\tassigned arg reg %s to R%d\n", mono_regname_full (hreg, 1), reg));

					list = g_slist_next (list);
				}
			}
		}

		/*
		 * TRACK SREG1
		 */
		bank = sreg1_bank (spec);
		if (MONO_ARCH_INST_IS_REGPAIR (spec_dest) && (spec [MONO_INST_CLOB] == '1')) {
			int sreg1 = sregs [0];
			int dest_sreg1 = dest_sregs [0];

			g_assert (is_soft_reg (sreg1, bank));

			/* To simplify things, we allocate the same regpair to sreg1 and dreg */
			if (dest_sreg1 != -1)
				g_assert (dest_sreg1 == ins->dreg);
			val = mono_regstate_alloc_int (rs, regmask (ins->dreg));
			g_assert (val >= 0);

			if (rs->vassign [sreg1] >= 0 && rs->vassign [sreg1] != val)
				// FIXME:
				g_assert_not_reached ();

			assign_reg (cfg, rs, sreg1, val, bank);

			DEBUG (printf ("\tassigned sreg1-low %s to R%d\n", mono_regname_full (val, bank), sreg1));

			g_assert ((regmask (dreg_high)) & regpair_reg2_mask (spec_src1, ins->dreg));
			val = mono_regstate_alloc_int (rs, regmask (dreg_high));
			g_assert (val >= 0);

			if (rs->vassign [sreg1 + 1] >= 0 && rs->vassign [sreg1 + 1] != val)
				// FIXME:
				g_assert_not_reached ();

			assign_reg (cfg, rs, sreg1 + 1, val, bank);

			DEBUG (printf ("\tassigned sreg1-high %s to R%d\n", mono_regname_full (val, bank), sreg1 + 1));

			/* Skip rest of this section */
			dest_sregs [0] = -1;
		}

		if (sreg_fixed_masks [0]) {
			g_assert (!bank);
			if (is_global_ireg (sregs [0])) {
				/* 
				 * The argument is already in a hard reg, but that reg is
				 * not usable by this instruction, so allocate a new one.
				 */
				val = mono_regstate_alloc_int (rs, sreg_fixed_masks [0]);
				if (val < 0)
					val = get_register_spilling (cfg, bb, tmp, ins, sreg_fixed_masks [0], -1, bank);
				mono_regstate_free_int (rs, val);
				dest_sregs [0] = val;

				/* Fall through to the dest_sreg1 != -1 case */
			}
			else
				sreg_masks [0] &= sreg_fixed_masks [0];
		}

		if (dest_sregs [0] != -1) {
			sreg_masks [0] = regmask (dest_sregs [0]);

			if ((rs->vassign [sregs [0]] != dest_sregs [0]) && !(rs->ifree_mask & (regmask (dest_sregs [0])))) {
				free_up_hreg (cfg, bb, tmp, ins, dest_sregs [0], 0);
			}
			if (is_global_ireg (sregs [0])) {
				/* The argument is already in a hard reg, need to copy */
				MonoInst *copy = create_copy_ins (cfg, bb, tmp, dest_sregs [0], sregs [0], NULL, ip, 0);
				insert_before_ins (bb, ins, copy);
				sregs [0] = dest_sregs [0];
			}
		}

		if (is_soft_reg (sregs [0], bank)) {
			val = rs->vassign [sregs [0]];
			prev_sregs [0] = sregs [0];
			if (val < 0) {
				int spill = 0;
				if (val < -1) {
					/* the register gets spilled after this inst */
					spill = -val -1;
				}

				if ((ins->opcode == OP_MOVE) && !spill && !bank && is_local_ireg (ins->dreg) && (rs->ifree_mask & (regmask (ins->dreg)))) {
					/* 
					 * Allocate the same hreg to sreg1 as well so the 
					 * peephole can get rid of the move.
					 */
					sreg_masks [0] = regmask (ins->dreg);
				}

				if (spec [MONO_INST_CLOB] == '1' && !dreg_bank (spec) && (rs->ifree_mask & (regmask (ins->dreg))))
					/* Allocate the same reg to sreg1 to avoid a copy later */
					sreg_masks [0] = regmask (ins->dreg);

				val = alloc_reg (cfg, bb, tmp, ins, sreg_masks [0], sregs [0], &reginfo [sregs [0]], bank);
				assign_reg (cfg, rs, sregs [0], val, bank);
				DEBUG (printf ("\tassigned sreg1 %s to R%d\n", mono_regname_full (val, bank), sregs [0]));

				if (spill) {
					/*
					 * Need to insert before the instruction since it can
					 * overwrite sreg1.
					 */
					create_spilled_store (cfg, bb, spill, val, prev_sregs [0], tmp, NULL, ins, bank);
				}
			}
			else if ((dest_sregs [0] != -1) && (dest_sregs [0] != val)) {
				MonoInst *copy = create_copy_ins (cfg, bb, tmp, dest_sregs [0], val, NULL, ip, bank);
				insert_before_ins (bb, ins, copy);
				for (j = 1; j < num_sregs; ++j)
					sreg_masks [j] &= ~(regmask (dest_sregs [0]));
				val = dest_sregs [0];
			}
				
			sregs [0] = val;
		}
		else {
			prev_sregs [0] = -1;
		}
		mono_inst_set_src_registers (ins, sregs);

		for (j = 1; j < num_sregs; ++j)
			sreg_masks [j] &= ~(regmask (sregs [0]));

		/* Handle the case when sreg1 is a regpair but dreg is not */
		if (MONO_ARCH_INST_IS_REGPAIR (spec_src1) && (spec [MONO_INST_CLOB] != '1')) {
			int reg2 = prev_sregs [0] + 1;

			g_assert (!bank);
			g_assert (prev_sregs [0] > -1);
			g_assert (!is_global_ireg (rs->vassign [prev_sregs [0]]));
			mask = regpair_reg2_mask (spec_src1, rs->vassign [prev_sregs [0]]);
			val = rs->vassign [reg2];
			if (val < 0) {
				int spill = 0;
				if (val < -1) {
					/* the register gets spilled after this inst */
					spill = -val -1;
				}
				val = mono_regstate_alloc_int (rs, mask);
				if (val < 0)
					val = get_register_spilling (cfg, bb, tmp, ins, mask, reg2, bank);
				if (spill)
					g_assert_not_reached ();
			}
			else {
				if (! (mask & (regmask (val)))) {
					/* The vreg is already allocated to a wrong hreg */
					/* FIXME: */
					g_assert_not_reached ();
#if 0
					val = mono_regstate_alloc_int (rs, mask);
					if (val < 0)
						val = get_register_spilling (cfg, bb, tmp, ins, mask, reg2, bank);

					/* Reallocate hreg to the correct register */
					create_copy_ins (cfg, bb, tmp, rs->vassign [reg2], val, ins, ip, bank);

					mono_regstate_free_int (rs, rs->vassign [reg2]);
#endif
				}
			}					

			sreg1_high = val;
			DEBUG (printf ("\tassigned sreg1 hreg %s to dest R%d\n", mono_arch_regname (val), reg2));
			assign_reg (cfg, rs, reg2, val, bank);
		}

		/* Handle dreg==sreg1 */
		if (((dreg_is_fp (spec) && sreg1_is_fp (spec)) || spec [MONO_INST_CLOB] == '1') && ins->dreg != sregs [0]) {
			MonoInst *sreg2_copy = NULL;
			MonoInst *copy;
			int bank = reg_bank (spec_src1);

			if (ins->dreg == sregs [1]) {
				/* 
				 * copying sreg1 to dreg could clobber sreg2, so allocate a new
				 * register for it.
				 */
				int reg2 = alloc_reg (cfg, bb, tmp, ins, dreg_mask, sregs [1], NULL, bank);

				DEBUG (printf ("\tneed to copy sreg2 %s to reg %s\n", mono_regname_full (sregs [1], bank), mono_regname_full (reg2, bank)));
				sreg2_copy = create_copy_ins (cfg, bb, tmp, reg2, sregs [1], NULL, ip, bank);
				prev_sregs [1] = sregs [1] = reg2;

				if (G_UNLIKELY (bank))
					mono_regstate_free_general (rs, reg2, bank);
				else
					mono_regstate_free_int (rs, reg2);
			}

			if (MONO_ARCH_INST_IS_REGPAIR (spec_src1)) {
				/* Copying sreg1_high to dreg could also clobber sreg2 */
				if (rs->vassign [prev_sregs [0] + 1] == sregs [1])
					/* FIXME: */
					g_assert_not_reached ();

				/* 
				 * sreg1 and dest are already allocated to the same regpair by the
				 * SREG1 allocation code.
				 */
				g_assert (sregs [0] == ins->dreg);
				g_assert (dreg_high == sreg1_high);
			}

			DEBUG (printf ("\tneed to copy sreg1 %s to dreg %s\n", mono_regname_full (sregs [0], bank), mono_regname_full (ins->dreg, bank)));
			copy = create_copy_ins (cfg, bb, tmp, ins->dreg, sregs [0], NULL, ip, bank);
			insert_before_ins (bb, ins, copy);

			if (sreg2_copy)
				insert_before_ins (bb, copy, sreg2_copy);

			/*
			 * Need to prevent sreg2 to be allocated to sreg1, since that
			 * would screw up the previous copy.
			 */
			sreg_masks [1] &= ~ (regmask (sregs [0]));
			/* we set sreg1 to dest as well */
			prev_sregs [0] = sregs [0] = ins->dreg;
			sreg_masks [1] &= ~ (regmask (ins->dreg));
		}
		mono_inst_set_src_registers (ins, sregs);

		/*
		 * TRACK SREG2, 3, ...
		 */
		for (j = 1; j < num_sregs; ++j) {
			int k;

			bank = sreg_bank (j, spec);
			if (MONO_ARCH_INST_IS_REGPAIR (spec [MONO_INST_SRC1 + j]))
				g_assert_not_reached ();

			if (dest_sregs [j] != -1 && is_global_ireg (sregs [j])) {
				/*
				 * Argument already in a global hard reg, copy it to the fixed reg, without
				 * allocating it to the fixed reg.
				 */
				MonoInst *copy = create_copy_ins (cfg, bb, tmp, dest_sregs [j], sregs [j], NULL, ip, 0);
				insert_before_ins (bb, ins, copy);
				sregs [j] = dest_sregs [j];
			} else if (is_soft_reg (sregs [j], bank)) {
				val = rs->vassign [sregs [j]];

				if (dest_sregs [j] != -1 && val >= 0 && dest_sregs [j] != val) {
					/*
					 * The sreg is already allocated to a hreg, but not to the fixed
					 * reg required by the instruction. Spill the sreg, so it can be
					 * allocated to the fixed reg by the code below.
					 */
					/* Currently, this code should only be hit for CAS */
					spill_vreg (cfg, bb, tmp, ins, sregs [j], 0);
					val = rs->vassign [sregs [j]];
				}

				if (val < 0) {
					int spill = 0;
					if (val < -1) {
						/* the register gets spilled after this inst */
						spill = -val -1;
					}
					val = alloc_reg (cfg, bb, tmp, ins, sreg_masks [j], sregs [j], &reginfo [sregs [j]], bank);
					assign_reg (cfg, rs, sregs [j], val, bank);
					DEBUG (printf ("\tassigned sreg%d %s to R%d\n", j + 1, mono_regname_full (val, bank), sregs [j]));
					if (spill) {
						/*
						 * Need to insert before the instruction since it can
						 * overwrite sreg2.
						 */
						create_spilled_store (cfg, bb, spill, val, sregs [j], tmp, NULL, ins, bank);
					}
				}
				sregs [j] = val;
				for (k = j + 1; k < num_sregs; ++k)
					sreg_masks [k] &= ~ (regmask (sregs [j]));
			}
			else {
				prev_sregs [j] = -1;
			}
		}
		mono_inst_set_src_registers (ins, sregs);

		/* Sanity check */
		/* Do this only for CAS for now */
		for (j = 1; j < num_sregs; ++j) {
			int sreg = sregs [j];
			int dest_sreg = dest_sregs [j];

			if (j == 2 && dest_sreg != -1) {
				int k;

				g_assert (sreg == dest_sreg);

				for (k = 0; k < num_sregs; ++k) {
					if (k != j)
						g_assert (sregs [k] != dest_sreg);
				}
			}
		}

		/*if (reg_is_freeable (ins->sreg1) && prev_sreg1 >= 0 && reginfo [prev_sreg1].born_in >= i) {
			DEBUG (printf ("freeable %s\n", mono_arch_regname (ins->sreg1)));
			mono_regstate_free_int (rs, ins->sreg1);
		}
		if (reg_is_freeable (ins->sreg2) && prev_sreg2 >= 0 && reginfo [prev_sreg2].born_in >= i) {
			DEBUG (printf ("freeable %s\n", mono_arch_regname (ins->sreg2)));
			mono_regstate_free_int (rs, ins->sreg2);
		}*/
	
		DEBUG (mono_print_ins_index (i, ins));
	}

	// FIXME: Set MAX_FREGS to 8
	// FIXME: Optimize generated code
#if MONO_ARCH_USE_FPSTACK
	/*
	 * Make a forward pass over the code, simulating the fp stack, making sure the
	 * arguments required by the fp opcodes are at the top of the stack.
	 */
	if (has_fp) {
		MonoInst *prev = NULL;
		MonoInst *fxch;
		int tmp;

		g_assert (num_sregs <= 2);

		for (ins = bb->code; ins; ins = ins->next) {
			spec = ins_get_spec (ins->opcode);

			DEBUG (printf ("processing:"));
			DEBUG (mono_print_ins_index (0, ins));

			if (ins->opcode == OP_FMOVE) {
				/* Do it by renaming the source to the destination on the stack */
				// FIXME: Is this correct ?
				for (i = 0; i < sp; ++i)
					if (fpstack [i] == ins->sreg1)
						fpstack [i] = ins->dreg;
				prev = ins;
				continue;
			}

			if (sreg1_is_fp (spec) && sreg2_is_fp (spec) && (fpstack [sp - 2] != ins->sreg1)) {
				/* Arg1 must be in %st(1) */
				g_assert (prev);

				i = 0;
				while ((i < sp) && (fpstack [i] != ins->sreg1))
					i ++;
				g_assert (i < sp);

				if (sp - 1 - i > 0) {
					/* First move it to %st(0) */
					DEBUG (printf ("\tswap %%st(0) and %%st(%d)\n", sp - 1 - i));
						
					MONO_INST_NEW (cfg, fxch, OP_X86_FXCH);
					fxch->inst_imm = sp - 1 - i;

					mono_bblock_insert_after_ins (bb, prev, fxch);
					prev = fxch;

					tmp = fpstack [sp - 1];
					fpstack [sp - 1] = fpstack [i];
					fpstack [i] = tmp;
				}
					
				/* Then move it to %st(1) */
				DEBUG (printf ("\tswap %%st(0) and %%st(1)\n"));
				
				MONO_INST_NEW (cfg, fxch, OP_X86_FXCH);
				fxch->inst_imm = 1;

				mono_bblock_insert_after_ins (bb, prev, fxch);
				prev = fxch;

				tmp = fpstack [sp - 1];
				fpstack [sp - 1] = fpstack [sp - 2];
				fpstack [sp - 2] = tmp;
			}

			if (sreg2_is_fp (spec)) {
				g_assert (sp > 0);

				if (fpstack [sp - 1] != ins->sreg2) {
					g_assert (prev);

					i = 0;
					while ((i < sp) && (fpstack [i] != ins->sreg2))
						i ++;
					g_assert (i < sp);

					DEBUG (printf ("\tswap %%st(0) and %%st(%d)\n", sp - 1 - i));

					MONO_INST_NEW (cfg, fxch, OP_X86_FXCH);
					fxch->inst_imm = sp - 1 - i;

					mono_bblock_insert_after_ins (bb, prev, fxch);
					prev = fxch;

					tmp = fpstack [sp - 1];
					fpstack [sp - 1] = fpstack [i];
					fpstack [i] = tmp;
				}

				sp --;
			}

			if (sreg1_is_fp (spec)) {
				g_assert (sp > 0);

				if (fpstack [sp - 1] != ins->sreg1) {
					g_assert (prev);

					i = 0;
					while ((i < sp) && (fpstack [i] != ins->sreg1))
						i ++;
					g_assert (i < sp);

					DEBUG (printf ("\tswap %%st(0) and %%st(%d)\n", sp - 1 - i));

					MONO_INST_NEW (cfg, fxch, OP_X86_FXCH);
					fxch->inst_imm = sp - 1 - i;

					mono_bblock_insert_after_ins (bb, prev, fxch);
					prev = fxch;

					tmp = fpstack [sp - 1];
					fpstack [sp - 1] = fpstack [i];
					fpstack [i] = tmp;
				}

				sp --;
			}

			if (dreg_is_fp (spec)) {
				g_assert (sp < 8);
				fpstack [sp ++] = ins->dreg;
			}

			if (G_UNLIKELY (cfg->verbose_level >= 2)) {
				printf ("\t[");
				for (i = 0; i < sp; ++i)
					printf ("%s%%fr%d", (i > 0) ? ", " : "", fpstack [i]);
				printf ("]\n");
			}

			prev = ins;
		}

		if (sp && bb != cfg->bb_exit && !(bb->out_count == 1 && bb->out_bb [0] == cfg->bb_exit)) {
			/* Remove remaining items from the fp stack */
			/* 
			 * These can remain for example as a result of a dead fmove like in
			 * System.Collections.Generic.EqualityComparer<double>.Equals ().
			 */
			while (sp) {
				MONO_INST_NEW (cfg, ins, OP_X86_FPOP);
				mono_add_ins_to_end (bb, ins);
				sp --;
			}
		}
	}
#endif
}

CompRelation
mono_opcode_to_cond (int opcode)
{
	switch (opcode) {
	case OP_CEQ:
	case OP_IBEQ:
	case OP_ICEQ:
	case OP_LBEQ:
	case OP_LCEQ:
	case OP_FBEQ:
	case OP_FCEQ:
	case OP_RBEQ:
	case OP_RCEQ:
	case OP_COND_EXC_EQ:
	case OP_COND_EXC_IEQ:
	case OP_CMOV_IEQ:
	case OP_CMOV_LEQ:
		return CMP_EQ;
	case OP_FCNEQ:
	case OP_RCNEQ:
	case OP_ICNEQ:
	case OP_IBNE_UN:
	case OP_LBNE_UN:
	case OP_FBNE_UN:
	case OP_COND_EXC_NE_UN:
	case OP_COND_EXC_INE_UN:
	case OP_CMOV_INE_UN:
	case OP_CMOV_LNE_UN:
		return CMP_NE;
	case OP_FCLE:
	case OP_ICLE:
	case OP_IBLE:
	case OP_LBLE:
	case OP_FBLE:
	case OP_CMOV_ILE:
	case OP_CMOV_LLE:
		return CMP_LE;
	case OP_FCGE:
	case OP_ICGE:
	case OP_IBGE:
	case OP_LBGE:
	case OP_FBGE:
	case OP_CMOV_IGE:
	case OP_CMOV_LGE:
		return CMP_GE;
	case OP_CLT:
	case OP_IBLT:
	case OP_ICLT:
	case OP_LBLT:
	case OP_LCLT:
	case OP_FBLT:
	case OP_FCLT:
	case OP_RBLT:
	case OP_RCLT:
	case OP_COND_EXC_LT:
	case OP_COND_EXC_ILT:
	case OP_CMOV_ILT:
	case OP_CMOV_LLT:
		return CMP_LT;
	case OP_CGT:
	case OP_IBGT:
	case OP_ICGT:
	case OP_LBGT:
	case OP_LCGT:
	case OP_FBGT:
	case OP_FCGT:
	case OP_RBGT:
	case OP_RCGT:
	case OP_COND_EXC_GT:
	case OP_COND_EXC_IGT:
	case OP_CMOV_IGT:
	case OP_CMOV_LGT:
		return CMP_GT;

	case OP_ICLE_UN:
	case OP_IBLE_UN:
	case OP_LBLE_UN:
	case OP_FBLE_UN:
	case OP_COND_EXC_LE_UN:
	case OP_COND_EXC_ILE_UN:
	case OP_CMOV_ILE_UN:
	case OP_CMOV_LLE_UN:
		return CMP_LE_UN;

	case OP_ICGE_UN:
	case OP_IBGE_UN:
	case OP_LBGE_UN:
	case OP_FBGE_UN:
	case OP_COND_EXC_GE_UN:
	case OP_CMOV_IGE_UN:
	case OP_CMOV_LGE_UN:
		return CMP_GE_UN;
	case OP_CLT_UN:
	case OP_IBLT_UN:
	case OP_ICLT_UN:
	case OP_LBLT_UN:
	case OP_LCLT_UN:
	case OP_FBLT_UN:
	case OP_FCLT_UN:
	case OP_RBLT_UN:
	case OP_RCLT_UN:
	case OP_COND_EXC_LT_UN:
	case OP_COND_EXC_ILT_UN:
	case OP_CMOV_ILT_UN:
	case OP_CMOV_LLT_UN:
		return CMP_LT_UN;
	case OP_CGT_UN:
	case OP_IBGT_UN:
	case OP_ICGT_UN:
	case OP_LBGT_UN:
	case OP_LCGT_UN:
	case OP_FCGT_UN:
	case OP_FBGT_UN:
	case OP_RCGT_UN:
	case OP_RBGT_UN:
	case OP_COND_EXC_GT_UN:
	case OP_COND_EXC_IGT_UN:
	case OP_CMOV_IGT_UN:
	case OP_CMOV_LGT_UN:
		return CMP_GT_UN;
	default:
		printf ("%s\n", mono_inst_name (opcode));
		g_assert_not_reached ();
		return (CompRelation)0;
	}
}

CompRelation
mono_negate_cond (CompRelation cond)
{
	switch (cond) {
	case CMP_EQ:
		return CMP_NE;
	case CMP_NE:
		return CMP_EQ;
	case CMP_LE:
		return CMP_GT;
	case CMP_GE:
		return CMP_LT;
	case CMP_LT:
		return CMP_GE;
	case CMP_GT:
		return CMP_LE;
	case CMP_LE_UN:
		return CMP_GT_UN;
	case CMP_GE_UN:
		return CMP_LT_UN;
	case CMP_LT_UN:
		return CMP_GE_UN;
	case CMP_GT_UN:
		return CMP_LE_UN;
	default:
		g_assert_not_reached ();
	}
}

CompType
mono_opcode_to_type (int opcode, int cmp_opcode)
{
	if ((opcode >= OP_CEQ) && (opcode <= OP_CLT_UN))
		return CMP_TYPE_L;
	else if ((opcode >= OP_IBEQ) && (opcode <= OP_IBLT_UN))
		return CMP_TYPE_I;
	else if ((opcode >= OP_ICEQ) && (opcode <= OP_ICLT_UN))
		return CMP_TYPE_I;
	else if ((opcode >= OP_LBEQ) && (opcode <= OP_LBLT_UN))
		return CMP_TYPE_L;
	else if ((opcode >= OP_LCEQ) && (opcode <= OP_LCLT_UN))
		return CMP_TYPE_L;
	else if ((opcode >= OP_FBEQ) && (opcode <= OP_FBLT_UN))
		return CMP_TYPE_F;
	else if ((opcode >= OP_FCEQ) && (opcode <= OP_FCLT_UN))
		return CMP_TYPE_F;
	else if ((opcode >= OP_COND_EXC_IEQ) && (opcode <= OP_COND_EXC_ILT_UN))
		return CMP_TYPE_I;
	else if ((opcode >= OP_COND_EXC_EQ) && (opcode <= OP_COND_EXC_LT_UN)) {
		switch (cmp_opcode) {
		case OP_ICOMPARE:
		case OP_ICOMPARE_IMM:
			return CMP_TYPE_I;
		default:
			return CMP_TYPE_L;
		}
	} else {
		g_error ("Unknown opcode '%s' in opcode_to_type", mono_inst_name (opcode));
		return (CompType)0;
	}
}

/*
 * mono_peephole_ins:
 *
 *   Perform some architecture independent peephole optimizations.
 */
void
mono_peephole_ins (MonoBasicBlock *bb, MonoInst *ins)
{
	int filter = FILTER_IL_SEQ_POINT;
	MonoInst *last_ins = mono_inst_prev (ins, filter);

	switch (ins->opcode) {
	case OP_MUL_IMM: 
		/* remove unnecessary multiplication with 1 */
		if (ins->inst_imm == 1) {
			if (ins->dreg != ins->sreg1)
				ins->opcode = OP_MOVE;
			else
				MONO_DELETE_INS (bb, ins);
		}
		break;
	case OP_LOAD_MEMBASE:
	case OP_LOADI4_MEMBASE:
		/* 
		 * Note: if reg1 = reg2 the load op is removed
		 *
		 * OP_STORE_MEMBASE_REG reg1, offset(basereg) 
		 * OP_LOAD_MEMBASE offset(basereg), reg2
		 * -->
		 * OP_STORE_MEMBASE_REG reg1, offset(basereg)
		 * OP_MOVE reg1, reg2
		 */
		if (last_ins && last_ins->opcode == OP_GC_LIVENESS_DEF)
			last_ins = mono_inst_prev (ins, filter);
		if (last_ins &&
			(((ins->opcode == OP_LOADI4_MEMBASE) && (last_ins->opcode == OP_STOREI4_MEMBASE_REG)) ||
			 ((ins->opcode == OP_LOAD_MEMBASE) && (last_ins->opcode == OP_STORE_MEMBASE_REG))) &&
			ins->inst_basereg == last_ins->inst_destbasereg &&
			ins->inst_offset == last_ins->inst_offset) {
			if (ins->dreg == last_ins->sreg1) {
				MONO_DELETE_INS (bb, ins);
				break;
			} else {
				ins->opcode = OP_MOVE;
				ins->sreg1 = last_ins->sreg1;
			}
			
			/* 
			 * Note: reg1 must be different from the basereg in the second load
			 * Note: if reg1 = reg2 is equal then second load is removed
			 *
			 * OP_LOAD_MEMBASE offset(basereg), reg1
			 * OP_LOAD_MEMBASE offset(basereg), reg2
			 * -->
			 * OP_LOAD_MEMBASE offset(basereg), reg1
			 * OP_MOVE reg1, reg2
			 */
		} if (last_ins && (last_ins->opcode == OP_LOADI4_MEMBASE
						   || last_ins->opcode == OP_LOAD_MEMBASE) &&
			  ins->inst_basereg != last_ins->dreg &&
			  ins->inst_basereg == last_ins->inst_basereg &&
			  ins->inst_offset == last_ins->inst_offset) {

			if (ins->dreg == last_ins->dreg) {
				MONO_DELETE_INS (bb, ins);
			} else {
				ins->opcode = OP_MOVE;
				ins->sreg1 = last_ins->dreg;
			}

			//g_assert_not_reached ();

#if 0
			/* 
			 * OP_STORE_MEMBASE_IMM imm, offset(basereg) 
			 * OP_LOAD_MEMBASE offset(basereg), reg
			 * -->
			 * OP_STORE_MEMBASE_IMM imm, offset(basereg) 
			 * OP_ICONST reg, imm
			 */
		} else if (last_ins && (last_ins->opcode == OP_STOREI4_MEMBASE_IMM
						|| last_ins->opcode == OP_STORE_MEMBASE_IMM) &&
				   ins->inst_basereg == last_ins->inst_destbasereg &&
				   ins->inst_offset == last_ins->inst_offset) {
			ins->opcode = OP_ICONST;
			ins->inst_c0 = last_ins->inst_imm;
			g_assert_not_reached (); // check this rule
#endif
		}
		break;
	case OP_LOADI1_MEMBASE:
	case OP_LOADU1_MEMBASE:
		/* 
		 * Note: if reg1 = reg2 the load op is removed
		 *
		 * OP_STORE_MEMBASE_REG reg1, offset(basereg) 
		 * OP_LOAD_MEMBASE offset(basereg), reg2
		 * -->
		 * OP_STORE_MEMBASE_REG reg1, offset(basereg)
		 * OP_MOVE reg1, reg2
		 */
		if (last_ins && (last_ins->opcode == OP_STOREI1_MEMBASE_REG) &&
			ins->inst_basereg == last_ins->inst_destbasereg &&
			ins->inst_offset == last_ins->inst_offset) {
			ins->opcode = (ins->opcode == OP_LOADI1_MEMBASE) ? OP_PCONV_TO_I1 : OP_PCONV_TO_U1;
			ins->sreg1 = last_ins->sreg1;
		}
		break;
	case OP_LOADI2_MEMBASE:
	case OP_LOADU2_MEMBASE:
		/* 
		 * Note: if reg1 = reg2 the load op is removed
		 *
		 * OP_STORE_MEMBASE_REG reg1, offset(basereg) 
		 * OP_LOAD_MEMBASE offset(basereg), reg2
		 * -->
		 * OP_STORE_MEMBASE_REG reg1, offset(basereg)
		 * OP_MOVE reg1, reg2
		 */
		if (last_ins && (last_ins->opcode == OP_STOREI2_MEMBASE_REG) &&
			ins->inst_basereg == last_ins->inst_destbasereg &&
			ins->inst_offset == last_ins->inst_offset) {
#if SIZEOF_REGISTER == 8
			ins->opcode = (ins->opcode == OP_LOADI2_MEMBASE) ? OP_PCONV_TO_I2 : OP_PCONV_TO_U2;
#else
			/* The definition of OP_PCONV_TO_U2 is wrong */
			ins->opcode = (ins->opcode == OP_LOADI2_MEMBASE) ? OP_PCONV_TO_I2 : OP_ICONV_TO_U2;
#endif
			ins->sreg1 = last_ins->sreg1;
		}
		break;
	case OP_LOADX_MEMBASE:
		if (last_ins && last_ins->opcode == OP_STOREX_MEMBASE &&
			ins->inst_basereg == last_ins->inst_destbasereg &&
			ins->inst_offset == last_ins->inst_offset) {
			if (ins->dreg == last_ins->sreg1) {
				MONO_DELETE_INS (bb, ins);
				break;
			} else {
				ins->opcode = OP_XMOVE;
				ins->sreg1 = last_ins->sreg1;
			}
		}
		break;
	case OP_MOVE:
	case OP_FMOVE:
		/*
		 * Removes:
		 *
		 * OP_MOVE reg, reg 
		 */
		if (ins->dreg == ins->sreg1) {
			MONO_DELETE_INS (bb, ins);
			break;
		}
		/* 
		 * Removes:
		 *
		 * OP_MOVE sreg, dreg 
		 * OP_MOVE dreg, sreg
		 */
		if (last_ins && last_ins->opcode == ins->opcode &&
			ins->sreg1 == last_ins->dreg &&
			ins->dreg == last_ins->sreg1) {
			MONO_DELETE_INS (bb, ins);
		}
		break;
	case OP_NOP:
		MONO_DELETE_INS (bb, ins);
		break;
	}
}

int
mini_exception_id_by_name (const char *name)
{
	if (strcmp (name, "NullReferenceException") == 0)
		return MONO_EXC_NULL_REF;
	if (strcmp (name, "IndexOutOfRangeException") == 0)
		return MONO_EXC_INDEX_OUT_OF_RANGE;
	if (strcmp (name, "OverflowException") == 0)
		return MONO_EXC_OVERFLOW;
	if (strcmp (name, "ArithmeticException") == 0)
		return MONO_EXC_ARITHMETIC;
	if (strcmp (name, "DivideByZeroException") == 0)
		return MONO_EXC_DIVIDE_BY_ZERO;
	if (strcmp (name, "InvalidCastException") == 0)
		return MONO_EXC_INVALID_CAST;
	if (strcmp (name, "ArrayTypeMismatchException") == 0)
		return MONO_EXC_ARRAY_TYPE_MISMATCH;
	if (strcmp (name, "ArgumentException") == 0)
		return MONO_EXC_ARGUMENT;
	if (strcmp (name, "ArgumentOutOfRangeException") == 0)
		return MONO_EXC_ARGUMENT_OUT_OF_RANGE;
	if (strcmp (name, "OutOfMemoryException") == 0)
		return MONO_EXC_ARGUMENT_OUT_OF_MEMORY;
	g_error ("Unknown intrinsic exception %s\n", name);
	return -1;
}

gboolean
mini_type_is_hfa (MonoType *t, int *out_nfields, int *out_esize)
{
	MonoClass *klass;
	gpointer iter;
	MonoClassField *field;
	MonoType *ftype, *prev_ftype = NULL;
	int nfields = 0;

	klass = mono_class_from_mono_type_internal (t);
	iter = NULL;
	while ((field = mono_class_get_fields_internal (klass, &iter))) {
		if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;
		ftype = mono_field_get_type_internal (field);
		ftype = mini_native_type_replace_type (ftype);

		if (MONO_TYPE_ISSTRUCT (ftype)) {
			int nested_nfields, nested_esize;

			if (!mini_type_is_hfa (ftype, &nested_nfields, &nested_esize))
				return FALSE;
			if (nested_esize == 4)
				ftype = m_class_get_byval_arg (mono_defaults.single_class);
			else
				ftype = m_class_get_byval_arg (mono_defaults.double_class);
			if (prev_ftype && prev_ftype->type != ftype->type)
				return FALSE;
			prev_ftype = ftype;
			nfields += nested_nfields;
		} else {
			if (!(!m_type_is_byref (ftype) && (ftype->type == MONO_TYPE_R4 || ftype->type == MONO_TYPE_R8)))
				return FALSE;
			if (prev_ftype && prev_ftype->type != ftype->type)
				return FALSE;
			prev_ftype = ftype;
			nfields ++;
		}
	}
	if (nfields == 0)
		return FALSE;
	*out_esize = prev_ftype->type == MONO_TYPE_R4 ? 4 : 8;
	*out_nfields = mono_class_value_size (klass, NULL) / *out_esize;
	return TRUE;
}

MonoRegState*
mono_regstate_new (void)
{
	MonoRegState* rs = g_new0 (MonoRegState, 1);

	rs->next_vreg = MAX (MONO_MAX_IREGS, MONO_MAX_FREGS);
#ifdef MONO_ARCH_NEED_SIMD_BANK
	rs->next_vreg = MAX (rs->next_vreg, MONO_MAX_XREGS);
#endif

	return rs;
}

void
mono_regstate_free (MonoRegState *rs) {
	g_free (rs->vassign);
	g_free (rs);
}

#endif /* DISABLE_JIT */

gboolean
mono_is_regsize_var (MonoType *t)
{
	t = mini_get_underlying_type (t);
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
#if SIZEOF_REGISTER == 8
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
#endif
		return TRUE;
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_STRING:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:
		return TRUE;
	case MONO_TYPE_GENERICINST:
		if (!mono_type_generic_inst_is_valuetype (t))
			return TRUE;
		return FALSE;
	case MONO_TYPE_VALUETYPE:
		return FALSE;
	default:
		return FALSE;
	}
}
