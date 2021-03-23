/**
 * \file
 * PowerPC backend for the Mono code generator
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Andreas Faerber <andreas.faerber@web.de>
 *
 * (C) 2003 Ximian, Inc.
 * (C) 2007-2008 Andreas Faerber
 */
#include "mini.h"
#include <string.h>

#include <mono/metadata/abi-details.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/utils/mono-proclib.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/mono-hwcap.h>
#include <mono/utils/unlocked.h>
#include "mono/utils/mono-tls-inline.h"

#include "mini-ppc.h"
#ifdef TARGET_POWERPC64
#include "cpu-ppc64.h"
#else
#include "cpu-ppc.h"
#endif
#include "ir-emit.h"
#include "aot-runtime.h"
#include "mini-runtime.h"
#ifdef __APPLE__
#include <sys/sysctl.h>
#endif
#ifdef __linux__
#include <unistd.h>
#endif
#ifdef _AIX
#include <sys/systemcfg.h>
#endif

static GENERATE_TRY_GET_CLASS_WITH_CACHE (math, "System", "Math")
static GENERATE_TRY_GET_CLASS_WITH_CACHE (mathf, "System", "MathF")

#define FORCE_INDIR_CALL 1

enum {
	TLS_MODE_DETECT,
	TLS_MODE_FAILED,
	TLS_MODE_LTHREADS,
	TLS_MODE_NPTL,
	TLS_MODE_DARWIN_G4,
	TLS_MODE_DARWIN_G5
};

/* cpu_hw_caps contains the flags defined below */
static int cpu_hw_caps = 0;
static int cachelinesize = 0;
static int cachelineinc = 0;
enum {
	PPC_ICACHE_SNOOP      = 1 << 0,
	PPC_MULTIPLE_LS_UNITS = 1 << 1,
	PPC_SMP_CAPABLE       = 1 << 2,
	PPC_ISA_2X            = 1 << 3,
	PPC_ISA_64            = 1 << 4,
	PPC_MOVE_FPR_GPR      = 1 << 5,
	PPC_ISA_2_03          = 1 << 6,
	PPC_HW_CAP_END
};

#define BREAKPOINT_SIZE (PPC_LOAD_SEQUENCE_LENGTH + 4)

/* This mutex protects architecture specific caches */
#define mono_mini_arch_lock() mono_os_mutex_lock (&mini_arch_mutex)
#define mono_mini_arch_unlock() mono_os_mutex_unlock (&mini_arch_mutex)
static mono_mutex_t mini_arch_mutex;

/*
 * The code generated for sequence points reads from this location, which is
 * made read-only when single stepping is enabled.
 */
static gpointer ss_trigger_page;

/* Enabled breakpoints read from this trigger page */
static gpointer bp_trigger_page;

#define MONO_EMIT_NEW_LOAD_R8(cfg,dr,addr) do { \
		MonoInst *inst;							   \
		MONO_INST_NEW ((cfg), (inst), OP_R8CONST); \
		inst->type = STACK_R8;			   \
		inst->dreg = (dr);		       \
		inst->inst_p0 = (void*)(addr);	       \
		mono_bblock_add_inst (cfg->cbb, inst); \
	} while (0)

const char*
mono_arch_regname (int reg) {
	static const char rnames[][4] = {
		"r0", "sp", "r2", "r3", "r4",
		"r5", "r6", "r7", "r8", "r9",
		"r10", "r11", "r12", "r13", "r14",
		"r15", "r16", "r17", "r18", "r19",
		"r20", "r21", "r22", "r23", "r24",
		"r25", "r26", "r27", "r28", "r29",
		"r30", "r31"
	};
	if (reg >= 0 && reg < 32)
		return rnames [reg];
	return "unknown";
}

const char*
mono_arch_fregname (int reg) {
	static const char rnames[][4] = {
		"f0", "f1", "f2", "f3", "f4",
		"f5", "f6", "f7", "f8", "f9",
		"f10", "f11", "f12", "f13", "f14",
		"f15", "f16", "f17", "f18", "f19",
		"f20", "f21", "f22", "f23", "f24",
		"f25", "f26", "f27", "f28", "f29",
		"f30", "f31"
	};
	if (reg >= 0 && reg < 32)
		return rnames [reg];
	return "unknown";
}

/* this function overwrites r0, r11, r12 */
static guint8*
emit_memcpy (guint8 *code, int size, int dreg, int doffset, int sreg, int soffset)
{
	/* unrolled, use the counter in big */
	if (size > sizeof (target_mgreg_t) * 5) {
		long shifted = size / TARGET_SIZEOF_VOID_P;
		guint8 *copy_loop_start, *copy_loop_jump;

		ppc_load (code, ppc_r0, shifted);
		ppc_mtctr (code, ppc_r0);
		//g_assert (sreg == ppc_r12);
		ppc_addi (code, ppc_r11, dreg, (doffset - sizeof (target_mgreg_t)));
		ppc_addi (code, ppc_r12, sreg, (soffset - sizeof (target_mgreg_t)));
		copy_loop_start = code;
		ppc_ldptr_update (code, ppc_r0, (unsigned int)sizeof (target_mgreg_t), ppc_r12);
		ppc_stptr_update (code, ppc_r0, (unsigned int)sizeof (target_mgreg_t), ppc_r11);
		copy_loop_jump = code;
		ppc_bc (code, PPC_BR_DEC_CTR_NONZERO, 0, 0);
		ppc_patch (copy_loop_jump, copy_loop_start);
		size -= shifted * sizeof (target_mgreg_t);
		doffset = soffset = 0;
		dreg = ppc_r11;
	}
#ifdef __mono_ppc64__
	/* the hardware has multiple load/store units and the move is long
	   enough to use more then one register, then use load/load/store/store
	   to execute 2 instructions per cycle. */
	if ((cpu_hw_caps & PPC_MULTIPLE_LS_UNITS) && (dreg != ppc_r11) && (sreg != ppc_r11)) { 
		while (size >= 16) {
			ppc_ldptr (code, ppc_r0, soffset, sreg);
			ppc_ldptr (code, ppc_r11, soffset+8, sreg);
			ppc_stptr (code, ppc_r0, doffset, dreg);
			ppc_stptr (code, ppc_r11, doffset+8, dreg);
			size -= 16;
			soffset += 16;
			doffset += 16; 
		}
	}
	while (size >= 8) {
		ppc_ldr (code, ppc_r0, soffset, sreg);
		ppc_str (code, ppc_r0, doffset, dreg);
		size -= 8;
		soffset += 8;
		doffset += 8;
	}
#else
	if ((cpu_hw_caps & PPC_MULTIPLE_LS_UNITS) && (dreg != ppc_r11) && (sreg != ppc_r11)) { 
		while (size >= 8) {
			ppc_lwz (code, ppc_r0, soffset, sreg);
			ppc_lwz (code, ppc_r11, soffset+4, sreg);
			ppc_stw (code, ppc_r0, doffset, dreg);
			ppc_stw (code, ppc_r11, doffset+4, dreg);
			size -= 8;
			soffset += 8;
			doffset += 8; 
		}
	}
#endif
	while (size >= 4) {
		ppc_lwz (code, ppc_r0, soffset, sreg);
		ppc_stw (code, ppc_r0, doffset, dreg);
		size -= 4;
		soffset += 4;
		doffset += 4;
	}
	while (size >= 2) {
		ppc_lhz (code, ppc_r0, soffset, sreg);
		ppc_sth (code, ppc_r0, doffset, dreg);
		size -= 2;
		soffset += 2;
		doffset += 2;
	}
	while (size >= 1) {
		ppc_lbz (code, ppc_r0, soffset, sreg);
		ppc_stb (code, ppc_r0, doffset, dreg);
		size -= 1;
		soffset += 1;
		doffset += 1;
	}
	return code;
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
 * Returns the size of the activation frame.
 */
int
mono_arch_get_argument_info (MonoMethodSignature *csig, int param_count, MonoJitArgumentInfo *arg_info)
{
#ifdef __mono_ppc64__
	NOT_IMPLEMENTED;
	return -1;
#else
	int k, frame_size = 0;
	int size, align, pad;
	int offset = 8;

	if (MONO_TYPE_ISSTRUCT (csig->ret)) { 
		frame_size += sizeof (target_mgreg_t);
		offset += 4;
	}

	arg_info [0].offset = offset;

	if (csig->hasthis) {
		frame_size += sizeof (target_mgreg_t);
		offset += 4;
	}

	arg_info [0].size = frame_size;

	for (k = 0; k < param_count; k++) {
		
		if (csig->pinvoke)
			size = mono_type_native_stack_size (csig->params [k], (guint32*)&align);
		else
			size = mini_type_stack_size (csig->params [k], &align);

		/* ignore alignment for now */
		align = 1;

		frame_size += pad = (align - (frame_size & (align - 1))) & (align - 1);	
		arg_info [k].pad = pad;
		frame_size += size;
		arg_info [k + 1].pad = 0;
		arg_info [k + 1].size = size;
		offset += pad;
		arg_info [k + 1].offset = offset;
		offset += size;
	}

	align = MONO_ARCH_FRAME_ALIGNMENT;
	frame_size += pad = (align - (frame_size & (align - 1))) & (align - 1);
	arg_info [k].pad = pad;

	return frame_size;
#endif
}

#ifdef __mono_ppc64__
static gboolean
is_load_sequence (guint32 *seq)
{
	return ppc_opcode (seq [0]) == 15 && /* lis */
		ppc_opcode (seq [1]) == 24 && /* ori */
		ppc_opcode (seq [2]) == 30 && /* sldi */
		ppc_opcode (seq [3]) == 25 && /* oris */
		ppc_opcode (seq [4]) == 24; /* ori */
}

#define ppc_load_get_dest(l)	(((l)>>21) & 0x1f)
#define ppc_load_get_off(l)	((gint16)((l) & 0xffff))
#endif

/* ld || lwz */
#define ppc_is_load_op(opcode) (ppc_opcode ((opcode)) == 58 || ppc_opcode ((opcode)) == 32)

/* code must point to the blrl */
gboolean
mono_ppc_is_direct_call_sequence (guint32 *code)
{
#ifdef __mono_ppc64__
	g_assert(*code == 0x4e800021 || *code == 0x4e800020 || *code == 0x4e800420);

	/* the thunk-less direct call sequence: lis/ori/sldi/oris/ori/mtlr/blrl */
	if (ppc_opcode (code [-1]) == 31) { /* mtlr */
		if (ppc_is_load_op (code [-2]) && ppc_is_load_op (code [-3])) { /* ld/ld */
			if (!is_load_sequence (&code [-8]))
				return FALSE;
			/* one of the loads must be "ld r2,8(rX)" or "ld r2,4(rX) for ilp32 */
			return (ppc_load_get_dest (code [-2]) == ppc_r2 && ppc_load_get_off (code [-2]) == sizeof (target_mgreg_t)) ||
				(ppc_load_get_dest (code [-3]) == ppc_r2 && ppc_load_get_off (code [-3]) == sizeof (target_mgreg_t));
		}
		if (ppc_opcode (code [-2]) == 24 && ppc_opcode (code [-3]) == 31) /* mr/nop */
			return is_load_sequence (&code [-8]);
		else
			return is_load_sequence (&code [-6]);
	}
	return FALSE;
#else
	g_assert(*code == 0x4e800021);

	/* the thunk-less direct call sequence: lis/ori/mtlr/blrl */
	return ppc_opcode (code [-1]) == 31 &&
		ppc_opcode (code [-2]) == 24 &&
		ppc_opcode (code [-3]) == 15;
#endif
}

#define MAX_ARCH_DELEGATE_PARAMS 7

static guint8*
get_delegate_invoke_impl (MonoTrampInfo **info, gboolean has_target, guint32 param_count, gboolean aot)
{
	guint8 *code, *start;

	if (has_target) {
		int size = MONO_PPC_32_64_CASE (32, 32) + PPC_FTNPTR_SIZE;

		start = code = mono_global_codeman_reserve (size);
		if (!aot)
			code = mono_ppc_create_pre_code_ftnptr (code);

		/* Replace the this argument with the target */
		ppc_ldptr (code, ppc_r0, MONO_STRUCT_OFFSET (MonoDelegate, method_ptr), ppc_r3);
#ifdef PPC_USES_FUNCTION_DESCRIPTOR
		/* it's a function descriptor */
		/* Can't use ldptr as it doesn't work with r0 */
		ppc_ldptr_indexed (code, ppc_r0, 0, ppc_r0);
#endif
		ppc_mtctr (code, ppc_r0);
		ppc_ldptr (code, ppc_r3, MONO_STRUCT_OFFSET (MonoDelegate, target), ppc_r3);
		ppc_bcctr (code, PPC_BR_ALWAYS, 0);

		g_assert ((code - start) <= size);

		mono_arch_flush_icache (start, size);
		MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_DELEGATE_INVOKE, NULL));
	} else {
		int size, i;

		size = MONO_PPC_32_64_CASE (32, 32) + param_count * 4 + PPC_FTNPTR_SIZE;
		start = code = mono_global_codeman_reserve (size);
		if (!aot)
			code = mono_ppc_create_pre_code_ftnptr (code);

		ppc_ldptr (code, ppc_r0, MONO_STRUCT_OFFSET (MonoDelegate, method_ptr), ppc_r3);
#ifdef PPC_USES_FUNCTION_DESCRIPTOR
		/* it's a function descriptor */
		ppc_ldptr_indexed (code, ppc_r0, 0, ppc_r0);
#endif
		ppc_mtctr (code, ppc_r0);
		/* slide down the arguments */
		for (i = 0; i < param_count; ++i) {
			ppc_mr (code, (ppc_r3 + i), (ppc_r3 + i + 1));
		}
		ppc_bcctr (code, PPC_BR_ALWAYS, 0);

		g_assert ((code - start) <= size);

		mono_arch_flush_icache (start, size);
		MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_DELEGATE_INVOKE, NULL));
	}

	if (has_target) {
		*info = mono_tramp_info_create ("delegate_invoke_impl_has_target", start, code - start, NULL, NULL);
	} else {
		char *name = g_strdup_printf ("delegate_invoke_impl_target_%d", param_count);
		*info = mono_tramp_info_create (name, start, code - start, NULL, NULL);
		g_free (name);
	}

	return start;
}

GSList*
mono_arch_get_delegate_invoke_impls (void)
{
	GSList *res = NULL;
	MonoTrampInfo *info;
	int i;

	get_delegate_invoke_impl (&info, TRUE, 0, TRUE);
	res = g_slist_prepend (res, info);

	for (i = 0; i <= MAX_ARCH_DELEGATE_PARAMS; ++i) {
		get_delegate_invoke_impl (&info, FALSE, i, TRUE);
		res = g_slist_prepend (res, info);
	}

	return res;
}

gpointer
mono_arch_get_delegate_invoke_impl (MonoMethodSignature *sig, gboolean has_target)
{
	guint8 *code, *start;

	/* FIXME: Support more cases */
	if (MONO_TYPE_ISSTRUCT (sig->ret))
		return NULL;

	if (has_target) {
		static guint8* cached = NULL;

		if (cached)
			return cached;

		if (mono_ee_features.use_aot_trampolines) {
			start = (guint8*)mono_aot_get_trampoline ("delegate_invoke_impl_has_target");
		} else {
			MonoTrampInfo *info;
			start = get_delegate_invoke_impl (&info, TRUE, 0, FALSE);
			mono_tramp_info_register (info, NULL);
		}
		mono_memory_barrier ();

		cached = start;
	} else {
		static guint8* cache [MAX_ARCH_DELEGATE_PARAMS + 1] = {NULL};
		int i;

		if (sig->param_count > MAX_ARCH_DELEGATE_PARAMS)
			return NULL;
		for (i = 0; i < sig->param_count; ++i)
			if (!mono_is_regsize_var (sig->params [i]))
				return NULL;


		code = cache [sig->param_count];
		if (code)
			return code;

		if (mono_ee_features.use_aot_trampolines) {
			char *name = g_strdup_printf ("delegate_invoke_impl_target_%d", sig->param_count);
			start = (guint8*)mono_aot_get_trampoline (name);
			g_free (name);
		} else {
			MonoTrampInfo *info;
			start = get_delegate_invoke_impl (&info, FALSE, sig->param_count, FALSE);
			mono_tramp_info_register (info, NULL);
		}

		mono_memory_barrier ();

		cache [sig->param_count] = start;
	}
	return start;
}

gpointer
mono_arch_get_delegate_virtual_invoke_impl (MonoMethodSignature *sig, MonoMethod *method, int offset, gboolean load_imt_reg)
{
	return NULL;
}

gpointer
mono_arch_get_this_arg_from_call (host_mgreg_t *r, guint8 *code)
{
	return (gpointer)(gsize)r [ppc_r3];
}

typedef struct {
	long int type;
	long int value;
} AuxVec;

#define MAX_AUX_ENTRIES 128
/* 
 * PPC_FEATURE_POWER4, PPC_FEATURE_POWER5, PPC_FEATURE_POWER5_PLUS, PPC_FEATURE_CELL,
 * PPC_FEATURE_PA6T, PPC_FEATURE_ARCH_2_05 are considered supporting 2X ISA features
 */
#define ISA_2X (0x00080000 | 0x00040000 | 0x00020000 | 0x00010000 | 0x00000800 | 0x00001000)

/* define PPC_FEATURE_64 HWCAP for 64-bit category.  */
#define ISA_64 0x40000000

/* define PPC_FEATURE_POWER6_EXT HWCAP for power6x mffgpr/mftgpr instructions.  */
#define ISA_MOVE_FPR_GPR 0x00000200
/*
 * Initialize the cpu to execute managed code.
 */
void
mono_arch_cpu_init (void)
{
}

/*
 * Initialize architecture specific code.
 */
void
mono_arch_init (void)
{
#if defined(MONO_CROSS_COMPILE)
#elif defined(__APPLE__)
	int mib [3];
	size_t len = sizeof (cachelinesize);

	mib [0] = CTL_HW;
	mib [1] = HW_CACHELINE;

	if (sysctl (mib, 2, &cachelinesize, &len, NULL, 0) == -1) {
		perror ("sysctl");
		cachelinesize = 128;
	} else {
		cachelineinc = cachelinesize;
	}
#elif defined(__linux__)
	AuxVec vec [MAX_AUX_ENTRIES];
	int i, vec_entries = 0;
	/* sadly this will work only with 2.6 kernels... */
	FILE* f = fopen ("/proc/self/auxv", "rb");

	if (f) {
		vec_entries = fread (&vec, sizeof (AuxVec), MAX_AUX_ENTRIES, f);
		fclose (f);
	}

	for (i = 0; i < vec_entries; i++) {
		int type = vec [i].type;

		if (type == 19) { /* AT_DCACHEBSIZE */
			cachelinesize = vec [i].value;
			continue;
		}
	}
#elif defined(G_COMPILER_CODEWARRIOR)
	cachelinesize = 32;
	cachelineinc = 32;
#elif defined(_AIX)
	/* FIXME: use block instead? */
	cachelinesize = _system_configuration.icache_line;
	cachelineinc = _system_configuration.icache_line;
#else
//#error Need a way to get cache line size
#endif

	if (mono_hwcap_ppc_has_icache_snoop)
		cpu_hw_caps |= PPC_ICACHE_SNOOP;

	if (mono_hwcap_ppc_is_isa_2x)
		cpu_hw_caps |= PPC_ISA_2X;

	if (mono_hwcap_ppc_is_isa_2_03)
		cpu_hw_caps |= PPC_ISA_2_03;

	if (mono_hwcap_ppc_is_isa_64)
		cpu_hw_caps |= PPC_ISA_64;

	if (mono_hwcap_ppc_has_move_fpr_gpr)
		cpu_hw_caps |= PPC_MOVE_FPR_GPR;

	if (mono_hwcap_ppc_has_multiple_ls_units)
		cpu_hw_caps |= PPC_MULTIPLE_LS_UNITS;

	if (!cachelinesize)
		cachelinesize = 32;

	if (!cachelineinc)
		cachelineinc = cachelinesize;

	if (mono_cpu_count () > 1)
		cpu_hw_caps |= PPC_SMP_CAPABLE;

	mono_os_mutex_init_recursive (&mini_arch_mutex);

	ss_trigger_page = mono_valloc (NULL, mono_pagesize (), MONO_MMAP_READ, MONO_MEM_ACCOUNT_OTHER);
	bp_trigger_page = mono_valloc (NULL, mono_pagesize (), MONO_MMAP_READ, MONO_MEM_ACCOUNT_OTHER);
	mono_mprotect (bp_trigger_page, mono_pagesize (), 0);

	// FIXME: Fix partial sharing for power and remove this
	mono_set_partial_sharing_supported (FALSE);
}

/*
 * Cleanup architecture specific code.
 */
void
mono_arch_cleanup (void)
{
	mono_os_mutex_destroy (&mini_arch_mutex);
}

gboolean
mono_arch_have_fast_tls (void)
{
	return FALSE;
}

/*
 * This function returns the optimizations supported on this cpu.
 */
guint32
mono_arch_cpu_optimizations (guint32 *exclude_mask)
{
	guint32 opts = 0;

	/* no ppc-specific optimizations yet */
	*exclude_mask = 0;
	return opts;
}

#ifdef __mono_ppc64__
#define CASE_PPC32(c)
#define CASE_PPC64(c)	case c:
#else
#define CASE_PPC32(c)	case c:
#define CASE_PPC64(c)
#endif

static gboolean
is_regsize_var (MonoType *t) {
	if (t->byref)
		return TRUE;
	t = mini_get_underlying_type (t);
	switch (t->type) {
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	CASE_PPC64 (MONO_TYPE_I8)
	CASE_PPC64 (MONO_TYPE_U8)
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
	case MONO_TYPE_GENERICINST:
		if (!mono_type_generic_inst_is_valuetype (t))
			return TRUE;
		return FALSE;
	case MONO_TYPE_VALUETYPE:
		return FALSE;
	}
	return FALSE;
}

#ifndef DISABLE_JIT
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

		if (ins->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT) || (ins->opcode != OP_LOCAL && ins->opcode != OP_ARG))
			continue;

		/* we can only allocate 32 bit values */
		if (is_regsize_var (ins->inst_vtype)) {
			g_assert (MONO_VARINFO (cfg, i)->reg == -1);
			g_assert (i == vmv->idx);
			vars = mono_varlist_insert_sorted (cfg, vars, vmv, FALSE);
		}
	}

	return vars;
}
#endif /* ifndef DISABLE_JIT */

GList *
mono_arch_get_global_int_regs (MonoCompile *cfg)
{
	GList *regs = NULL;
	int i, top = 32;
	if (cfg->frame_reg != ppc_sp)
		top = 31;
	/* ppc_r13 is used by the system on PPC EABI */
	for (i = 14; i < top; ++i) {
		/*
		 * Reserve r29 for holding the vtable address for virtual calls in AOT mode,
		 * since the trampolines can clobber r12.
		 */
		if (!(cfg->compile_aot && i == 29))
			regs = g_list_prepend (regs, GUINT_TO_POINTER (i));
	}

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
	/* FIXME: */
	return 2;
}

void
mono_arch_flush_icache (guint8 *code, gint size)
{
#ifdef MONO_CROSS_COMPILE
	/* do nothing */
#else
	register guint8 *p;
	guint8 *endp, *start;

	p = start = code;
	endp = p + size;
	start = (guint8*)((gsize)start & ~(cachelinesize - 1));
	/* use dcbf for smp support, later optimize for UP, see pem._64bit.d20030611.pdf page 211 */
#if defined(G_COMPILER_CODEWARRIOR)
	if (cpu_hw_caps & PPC_SMP_CAPABLE) {
		for (p = start; p < endp; p += cachelineinc) {
			asm { dcbf 0, p };
		}
	} else {
		for (p = start; p < endp; p += cachelineinc) {
			asm { dcbst 0, p };
		}
	}
	asm { sync };
	p = code;
	for (p = start; p < endp; p += cachelineinc) {
		asm {
			icbi 0, p
			sync
		}
	}
	asm {
		sync
		isync
	}
#else
	/* For POWER5/6 with ICACHE_SNOOPing only one icbi in the range is required.
	 * The sync is required to insure that the store queue is completely empty.
	 * While the icbi performs no cache operations, icbi/isync is required to
	 * kill local prefetch.
	 */
	if (cpu_hw_caps & PPC_ICACHE_SNOOP) {
		asm ("sync");
		asm ("icbi 0,%0;" : : "r"(code) : "memory");
		asm ("isync");
		return;
	}
	/* use dcbf for smp support, see pem._64bit.d20030611.pdf page 211 */
	if (cpu_hw_caps & PPC_SMP_CAPABLE) {
		for (p = start; p < endp; p += cachelineinc) {
			asm ("dcbf 0,%0;" : : "r"(p) : "memory");
		}
	} else {
		for (p = start; p < endp; p += cachelineinc) {
			asm ("dcbst 0,%0;" : : "r"(p) : "memory");
		}
	}
	asm ("sync");
	p = code;
	for (p = start; p < endp; p += cachelineinc) {
		/* for ISA2.0+ implementations we should not need any extra sync between the
		 * icbi instructions.  Both the 2.0 PEM and the PowerISA-2.05 say this.
		 * So I am not sure which chip had this problem but its not an issue on
		 * of the ISA V2 chips.
		 */
		if (cpu_hw_caps & PPC_ISA_2X)
			asm ("icbi 0,%0;" : : "r"(p) : "memory");
		else
			asm ("icbi 0,%0; sync;" : : "r"(p) : "memory");
	}
	if (!(cpu_hw_caps & PPC_ISA_2X))
		asm ("sync");
	asm ("isync");
#endif
#endif
}

void
mono_arch_flush_register_windows (void)
{
}

#ifdef __APPLE__
#define ALWAYS_ON_STACK(s) s
#define FP_ALSO_IN_REG(s) s
#else
#ifdef __mono_ppc64__
#define ALWAYS_ON_STACK(s) s
#define FP_ALSO_IN_REG(s) s
#else
#define ALWAYS_ON_STACK(s)
#define FP_ALSO_IN_REG(s)
#endif
#define ALIGN_DOUBLES
#endif

enum {
	RegTypeGeneral,
	RegTypeBase,
	RegTypeFP,
	RegTypeStructByVal,
	RegTypeStructByAddr,
	RegTypeFPStructByVal,  // For the v2 ABI, floats should be passed in FRs instead of GRs.  Only valid for ABI v2!
};

typedef struct {
	gint32  offset;
	guint32 vtsize; /* in param area */
	guint8  reg;
	guint8  vtregs; /* number of registers used to pass a RegTypeStructByVal/RegTypeFPStructByVal */
	guint8  regtype : 4; /* 0 general, 1 basereg, 2 floating point register, see RegType* */
	guint8  size    : 4; /* 1, 2, 4, 8, or regs used by RegTypeStructByVal/RegTypeFPStructByVal */
	guint8  bytes   : 4; /* size in bytes - only valid for
				RegTypeStructByVal/RegTypeFPStructByVal if the struct fits
				in one word, otherwise it's 0*/
} ArgInfo;

struct CallInfo {
	int nargs;
	guint32 stack_usage;
	guint32 struct_ret;
	ArgInfo ret;
	ArgInfo sig_cookie;
	gboolean vtype_retaddr;
	int vret_arg_index;
	ArgInfo args [1];
};

#define DEBUG(a)


#if PPC_RETURN_SMALL_FLOAT_STRUCTS_IN_FR_REGS
//
// Test if a structure is completely composed of either float XOR double fields and has fewer than
// PPC_MOST_FLOAT_STRUCT_MEMBERS_TO_RETURN_VIA_REGISTER members.
// If this is true the structure can be returned directly via float registers instead of by a hidden parameter
// pointing to where the return value should be stored.
// This is as per the ELF ABI v2.
//
static gboolean
is_float_struct_returnable_via_regs  (MonoType *type, int* member_cnt, int* member_size)
{
	int local_member_cnt, local_member_size;         
	if (!member_cnt) {
		member_cnt = &local_member_cnt;
	}
	if (!member_size) {
		member_size = &local_member_size;
	}

	gboolean is_all_floats = mini_type_is_hfa(type, member_cnt, member_size);
	return is_all_floats && (*member_cnt <= PPC_MOST_FLOAT_STRUCT_MEMBERS_TO_RETURN_VIA_REGISTERS);
}
#else

#define is_float_struct_returnable_via_regs(a,b,c) (FALSE)

#endif

#if PPC_RETURN_SMALL_STRUCTS_IN_REGS
//
// Test if a structure is smaller in size than 2 doublewords (PPC_LARGEST_STRUCT_SIZE_TO_RETURN_VIA_REGISTERS) and is
// completely composed of fields all of basic types.
// If this is true the structure can be returned directly via registers r3/r4 instead of by a hidden parameter
// pointing to where the return value should be stored.
// This is as per the ELF ABI v2.
//
static gboolean
is_struct_returnable_via_regs  (MonoClass *klass, gboolean is_pinvoke)
{
  	gboolean has_a_field = FALSE;
	int size = 0;
	if (klass) {
		gpointer iter = NULL;
		MonoClassField *f;
		if (is_pinvoke)
			size = mono_type_native_stack_size (m_class_get_byval_arg (klass), 0);
		else
			size = mini_type_stack_size (m_class_get_byval_arg (klass), 0);
		if (size == 0)
			return TRUE;
		if (size > PPC_LARGEST_STRUCT_SIZE_TO_RETURN_VIA_REGISTERS)
			return FALSE;
		while ((f = mono_class_get_fields_internal (klass, &iter))) {
			if (!(f->type->attrs & FIELD_ATTRIBUTE_STATIC)) {
				// TBD: Is there a better way to check for the basic types?
				if (f->type->byref) {
					return FALSE;
				} else if ((f->type->type >= MONO_TYPE_BOOLEAN) && (f->type->type <= MONO_TYPE_R8)) {
					has_a_field = TRUE;
				} else if (MONO_TYPE_ISSTRUCT (f->type)) {
					MonoClass *klass = mono_class_from_mono_type_internal (f->type);
					if (is_struct_returnable_via_regs(klass, is_pinvoke)) {
						has_a_field = TRUE;
					} else {
						return FALSE;
					}
				} else {
					return FALSE;
				}
			}
		}
	}
	return has_a_field;
}
#else

#define is_struct_returnable_via_regs(a,b) (FALSE)

#endif

static void inline
add_general (guint *gr, guint *stack_size, ArgInfo *ainfo, gboolean simple)
{
#ifdef __mono_ppc64__
	g_assert (simple);
#endif

	if (simple) {
		if (*gr >= 3 + PPC_NUM_REG_ARGS) {
			ainfo->offset = PPC_STACK_PARAM_OFFSET + *stack_size;
			ainfo->reg = ppc_sp; /* in the caller */
			ainfo->regtype = RegTypeBase;
			*stack_size += sizeof (target_mgreg_t);
		} else {
			ALWAYS_ON_STACK (*stack_size += sizeof (target_mgreg_t));
			ainfo->reg = *gr;
		}
	} else {
		if (*gr >= 3 + PPC_NUM_REG_ARGS - 1) {
#ifdef ALIGN_DOUBLES
			//*stack_size += (*stack_size % 8);
#endif
			ainfo->offset = PPC_STACK_PARAM_OFFSET + *stack_size;
			ainfo->reg = ppc_sp; /* in the caller */
			ainfo->regtype = RegTypeBase;
			*stack_size += 8;
		} else {
#ifdef ALIGN_DOUBLES
		if (!((*gr) & 1))
			(*gr) ++;
#endif
			ALWAYS_ON_STACK (*stack_size += 8);
			ainfo->reg = *gr;
		}
		(*gr) ++;
	}
	(*gr) ++;
}

#if defined(__APPLE__) || (defined(__mono_ppc64__) && !PPC_PASS_SMALL_FLOAT_STRUCTS_IN_FR_REGS)
static gboolean
has_only_a_r48_field (MonoClass *klass)
{
	gpointer iter;
	MonoClassField *f;
	gboolean have_field = FALSE;
	iter = NULL;
	while ((f = mono_class_get_fields_internal (klass, &iter))) {
		if (!(f->type->attrs & FIELD_ATTRIBUTE_STATIC)) {
			if (have_field)
				return FALSE;
			if (!f->type->byref && (f->type->type == MONO_TYPE_R4 || f->type->type == MONO_TYPE_R8))
				have_field = TRUE;
			else
				return FALSE;
		}
	}
	return have_field;
}
#endif

static CallInfo*
get_call_info (MonoMethodSignature *sig)
{
	guint i, fr, gr, pstart;
	int n = sig->hasthis + sig->param_count;
	MonoType *simpletype;
	guint32 stack_size = 0;
	CallInfo *cinfo = g_malloc0 (sizeof (CallInfo) + sizeof (ArgInfo) * n);
	gboolean is_pinvoke = sig->pinvoke;

	fr = PPC_FIRST_FPARG_REG;
	gr = PPC_FIRST_ARG_REG;

	if (mini_type_is_vtype (sig->ret)) {
		cinfo->vtype_retaddr = TRUE;
	}

	pstart = 0;
	n = 0;
	/*
	 * To simplify get_this_arg_reg () and LLVM integration, emit the vret arg after
	 * the first argument, allowing 'this' to be always passed in the first arg reg.
	 * Also do this if the first argument is a reference type, since virtual calls
	 * are sometimes made using calli without sig->hasthis set, like in the delegate
	 * invoke wrappers.
	 */
	if (cinfo->vtype_retaddr && !is_pinvoke && (sig->hasthis || (sig->param_count > 0 && MONO_TYPE_IS_REFERENCE (mini_get_underlying_type (sig->params [0]))))) {
		if (sig->hasthis) {
			add_general (&gr, &stack_size, cinfo->args + 0, TRUE);
			n ++;
		} else {
			add_general (&gr, &stack_size, &cinfo->args [sig->hasthis + 0], TRUE);
			pstart = 1;
			n ++;
		}
		add_general (&gr, &stack_size, &cinfo->ret, TRUE);
		cinfo->struct_ret = cinfo->ret.reg;
		cinfo->vret_arg_index = 1;
	} else {
		/* this */
		if (sig->hasthis) {
			add_general (&gr, &stack_size, cinfo->args + 0, TRUE);
			n ++;
		}

		if (cinfo->vtype_retaddr) {
			add_general (&gr, &stack_size, &cinfo->ret, TRUE);
			cinfo->struct_ret = cinfo->ret.reg;
		}
	}

        DEBUG(printf("params: %d\n", sig->param_count));
	for (i = pstart; i < sig->param_count; ++i) {
		if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG) && (i == sig->sentinelpos)) {
                        /* Prevent implicit arguments and sig_cookie from
			   being passed in registers */
                        gr = PPC_LAST_ARG_REG + 1;
			/* FIXME: don't we have to set fr, too? */
                        /* Emit the signature cookie just before the implicit arguments */
                        add_general (&gr, &stack_size, &cinfo->sig_cookie, TRUE);
                }
                DEBUG(printf("param %d: ", i));
		if (sig->params [i]->byref) {
                        DEBUG(printf("byref\n"));
			add_general (&gr, &stack_size, cinfo->args + n, TRUE);
			n++;
			continue;
		}
		simpletype = mini_get_underlying_type (sig->params [i]);
		switch (simpletype->type) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
			cinfo->args [n].size = 1;
			add_general (&gr, &stack_size, cinfo->args + n, TRUE);
			n++;
			break;
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
			cinfo->args [n].size = 2;
			add_general (&gr, &stack_size, cinfo->args + n, TRUE);
			n++;
			break;
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
			cinfo->args [n].size = 4;
			add_general (&gr, &stack_size, cinfo->args + n, TRUE);
			n++;
			break;
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_PTR:
		case MONO_TYPE_FNPTR:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_STRING:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_ARRAY:
			cinfo->args [n].size = sizeof (target_mgreg_t);
			add_general (&gr, &stack_size, cinfo->args + n, TRUE);
			n++;
			break;
		case MONO_TYPE_GENERICINST:
			if (!mono_type_generic_inst_is_valuetype (simpletype)) {
				cinfo->args [n].size = sizeof (target_mgreg_t);
				add_general (&gr, &stack_size, cinfo->args + n, TRUE);
				n++;
				break;
			}
			/* Fall through */
		case MONO_TYPE_VALUETYPE:
		case MONO_TYPE_TYPEDBYREF: {
			gint size;
			MonoClass *klass = mono_class_from_mono_type_internal (sig->params [i]);
			if (simpletype->type == MONO_TYPE_TYPEDBYREF)
				size = MONO_ABI_SIZEOF (MonoTypedRef);
			else if (is_pinvoke)
			    size = mono_class_native_size (klass, NULL);
			else
			    size = mono_class_value_size (klass, NULL);

#if defined(__APPLE__) || (defined(__mono_ppc64__) && !PPC_PASS_SMALL_FLOAT_STRUCTS_IN_FR_REGS)
			if ((size == 4 || size == 8) && has_only_a_r48_field (klass)) {
				cinfo->args [n].size = size;

				/* It was 7, now it is 8 in LinuxPPC */
				if (fr <= PPC_LAST_FPARG_REG) {
					cinfo->args [n].regtype = RegTypeFP;
					cinfo->args [n].reg = fr;
					fr ++;
					FP_ALSO_IN_REG (gr ++);
#if !defined(__mono_ppc64__)
					if (size == 8)
						FP_ALSO_IN_REG (gr ++);
#endif
					ALWAYS_ON_STACK (stack_size += size);
				} else {
					cinfo->args [n].offset = PPC_STACK_PARAM_OFFSET + stack_size;
					cinfo->args [n].regtype = RegTypeBase;
					cinfo->args [n].reg = ppc_sp; /* in the caller*/
					stack_size += 8;
				}
				n++;
				break;
			}
#endif
			DEBUG(printf ("load %d bytes struct\n",
				      mono_class_native_size (sig->params [i]->data.klass, NULL)));

#if PPC_PASS_STRUCTS_BY_VALUE
			{
				int align_size = size;
				int nregs = 0;
				int rest = PPC_LAST_ARG_REG - gr + 1;
				int n_in_regs = 0;

#if PPC_PASS_SMALL_FLOAT_STRUCTS_IN_FR_REGS
				int mbr_cnt = 0;
				int mbr_size = 0;
				gboolean is_all_floats = is_float_struct_returnable_via_regs (sig->params [i], &mbr_cnt, &mbr_size);

				if (is_all_floats) {
					rest = PPC_LAST_FPARG_REG - fr + 1;
				}
				// Pass small (<= 8 member) structures entirely made up of either float or double members
				// in FR registers.  There have to be at least mbr_cnt registers left.
				if (is_all_floats &&
					 (rest >= mbr_cnt)) {
					nregs = mbr_cnt;
					n_in_regs = MIN (rest, nregs);
					cinfo->args [n].regtype = RegTypeFPStructByVal;
					cinfo->args [n].vtregs = n_in_regs;
					cinfo->args [n].size = mbr_size;
					cinfo->args [n].vtsize = nregs - n_in_regs;
					cinfo->args [n].reg = fr;
					fr += n_in_regs;
					if (mbr_size == 4) {
						// floats
						FP_ALSO_IN_REG (gr += (n_in_regs+1)/2);
					} else {
						// doubles
						FP_ALSO_IN_REG (gr += (n_in_regs));
					}
				} else
#endif
				{
					align_size += (sizeof (target_mgreg_t) - 1);
					align_size &= ~(sizeof (target_mgreg_t) - 1);
					nregs = (align_size + sizeof (target_mgreg_t) -1 ) / sizeof (target_mgreg_t);
					n_in_regs = MIN (rest, nregs);
					if (n_in_regs < 0)
						n_in_regs = 0;
#ifdef __APPLE__
					/* FIXME: check this */
					if (size >= 3 && size % 4 != 0)
						n_in_regs = 0;
#endif
					cinfo->args [n].regtype = RegTypeStructByVal;
					cinfo->args [n].vtregs = n_in_regs;
					cinfo->args [n].size = n_in_regs;
					cinfo->args [n].vtsize = nregs - n_in_regs;
					cinfo->args [n].reg = gr;
					gr += n_in_regs;
				}

#ifdef __mono_ppc64__
				if (nregs == 1 && is_pinvoke)
					cinfo->args [n].bytes = size;
				else
#endif
					cinfo->args [n].bytes = 0;
				cinfo->args [n].offset = PPC_STACK_PARAM_OFFSET + stack_size;
				/*g_print ("offset for arg %d at %d\n", n, PPC_STACK_PARAM_OFFSET + stack_size);*/
				stack_size += nregs * sizeof (target_mgreg_t);
			}
#else
			add_general (&gr, &stack_size, cinfo->args + n, TRUE);
			cinfo->args [n].regtype = RegTypeStructByAddr;
			cinfo->args [n].vtsize = size;
#endif
			n++;
			break;
		}
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
			cinfo->args [n].size = 8;
			add_general (&gr, &stack_size, cinfo->args + n, SIZEOF_REGISTER == 8);
			n++;
			break;
		case MONO_TYPE_R4:
			cinfo->args [n].size = 4;

			/* It was 7, now it is 8 in LinuxPPC */
			if (fr <= PPC_LAST_FPARG_REG
			// For non-native vararg calls the parms must go in storage
				 && !(!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG))
				) {
				cinfo->args [n].regtype = RegTypeFP;
				cinfo->args [n].reg = fr;
				fr ++;
				FP_ALSO_IN_REG (gr ++);
				ALWAYS_ON_STACK (stack_size += SIZEOF_REGISTER);
			} else {
				cinfo->args [n].offset = PPC_STACK_PARAM_OFFSET + stack_size + MONO_PPC_32_64_CASE (0, 4);
				cinfo->args [n].regtype = RegTypeBase;
				cinfo->args [n].reg = ppc_sp; /* in the caller*/
				stack_size += SIZEOF_REGISTER;
			}
			n++;
			break;
		case MONO_TYPE_R8:
			cinfo->args [n].size = 8;
			/* It was 7, now it is 8 in LinuxPPC */
			if (fr <= PPC_LAST_FPARG_REG
			// For non-native vararg calls the parms must go in storage
				 && !(!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG))
				 ) {
				cinfo->args [n].regtype = RegTypeFP;
				cinfo->args [n].reg = fr;
				fr ++;
				FP_ALSO_IN_REG (gr += sizeof (double) / SIZEOF_REGISTER);
				ALWAYS_ON_STACK (stack_size += 8);
			} else {
				cinfo->args [n].offset = PPC_STACK_PARAM_OFFSET + stack_size;
				cinfo->args [n].regtype = RegTypeBase;
				cinfo->args [n].reg = ppc_sp; /* in the caller*/
				stack_size += 8;
			}
			n++;
			break;
		default:
			g_error ("Can't trampoline 0x%x", sig->params [i]->type);
		}
	}
	cinfo->nargs = n;

	if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG) && (i == sig->sentinelpos)) {
		/* Prevent implicit arguments and sig_cookie from
		   being passed in registers */
		gr = PPC_LAST_ARG_REG + 1;
		/* Emit the signature cookie just before the implicit arguments */
		add_general (&gr, &stack_size, &cinfo->sig_cookie, TRUE);
	}

	{
		simpletype = mini_get_underlying_type (sig->ret);
		switch (simpletype->type) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_PTR:
		case MONO_TYPE_FNPTR:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_STRING:
			cinfo->ret.reg = ppc_r3;
			break;
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
			cinfo->ret.reg = ppc_r3;
			break;
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
			cinfo->ret.reg = ppc_f1;
			cinfo->ret.regtype = RegTypeFP;
			break;
		case MONO_TYPE_GENERICINST:
			if (!mono_type_generic_inst_is_valuetype (simpletype)) {
				cinfo->ret.reg = ppc_r3;
				break;
			}
			break;
		case MONO_TYPE_VALUETYPE:
			break;
		case MONO_TYPE_TYPEDBYREF:
		case MONO_TYPE_VOID:
			break;
		default:
			g_error ("Can't handle as return value 0x%x", sig->ret->type);
		}
	}

	/* align stack size to 16 */
	DEBUG (printf ("      stack size: %d (%d)\n", (stack_size + 15) & ~15, stack_size));
	stack_size = (stack_size + 15) & ~15;

	cinfo->stack_usage = stack_size;
	return cinfo;
}

#ifndef DISABLE_JIT

gboolean
mono_arch_tailcall_supported (MonoCompile *cfg, MonoMethodSignature *caller_sig, MonoMethodSignature *callee_sig, gboolean virtual_)
{
	CallInfo *caller_info = get_call_info (caller_sig);
	CallInfo *callee_info = get_call_info (callee_sig);

	gboolean res = IS_SUPPORTED_TAILCALL (callee_info->stack_usage <= caller_info->stack_usage)
		&& IS_SUPPORTED_TAILCALL (memcmp (&callee_info->ret, &caller_info->ret, sizeof (caller_info->ret)) == 0);

	// FIXME ABIs vary as to if this local is in the parameter area or not,
	// so this check might not be needed.
	for (int i = 0; res && i < callee_info->nargs; ++i) {
		res = IS_SUPPORTED_TAILCALL (callee_info->args [i].regtype != RegTypeStructByAddr);
			/* An address on the callee's stack is passed as the argument */
	}

	g_free (caller_info);
	g_free (callee_info);

	return res;
}

#endif

/*
 * Set var information according to the calling convention. ppc version.
 * The locals var stuff should most likely be split in another method.
 */
void
mono_arch_allocate_vars (MonoCompile *m)
{
	MonoMethodSignature *sig;
	MonoMethodHeader *header;
	MonoInst *inst;
	int i, offset, size, align, curinst;
	int frame_reg = ppc_sp;
	gint32 *offsets;
	guint32 locals_stack_size, locals_stack_align;

	m->flags |= MONO_CFG_HAS_SPILLUP;

	/* this is bug #60332: remove when #59509 is fixed, so no weird vararg 
	 * call convs needs to be handled this way.
	 */
	if (m->flags & MONO_CFG_HAS_VARARGS)
		m->param_area = MAX (m->param_area, sizeof (target_mgreg_t)*8);
	/* gtk-sharp and other broken code will dllimport vararg functions even with
	 * non-varargs signatures. Since there is little hope people will get this right
	 * we assume they won't.
	 */
	if (m->method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE)
		m->param_area = MAX (m->param_area, sizeof (target_mgreg_t)*8);

	header = m->header;

	/* 
	 * We use the frame register also for any method that has
	 * exception clauses. This way, when the handlers are called,
	 * the code will reference local variables using the frame reg instead of
	 * the stack pointer: if we had to restore the stack pointer, we'd
	 * corrupt the method frames that are already on the stack (since
	 * filters get called before stack unwinding happens) when the filter
	 * code would call any method (this also applies to finally etc.).
	 */ 
	if ((m->flags & MONO_CFG_HAS_ALLOCA) || header->num_clauses)
		frame_reg = ppc_r31;
	m->frame_reg = frame_reg;
	if (frame_reg != ppc_sp) {
		m->used_int_regs |= 1 << frame_reg;
	}

	sig = mono_method_signature_internal (m->method);
	
	offset = 0;
	curinst = 0;
	if (MONO_TYPE_ISSTRUCT (sig->ret)) {
		m->ret->opcode = OP_REGVAR;
		m->ret->inst_c0 = m->ret->dreg = ppc_r3;
	} else {
		/* FIXME: handle long values? */
		switch (mini_get_underlying_type (sig->ret)->type) {
		case MONO_TYPE_VOID:
			break;
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
			m->ret->opcode = OP_REGVAR;
			m->ret->inst_c0 = m->ret->dreg = ppc_f1;
			break;
		default:
			m->ret->opcode = OP_REGVAR;
			m->ret->inst_c0 = m->ret->dreg = ppc_r3;
			break;
		}
	}
	/* local vars are at a positive offset from the stack pointer */
	/* 
	 * also note that if the function uses alloca, we use ppc_r31
	 * to point at the local variables.
	 */
	offset = PPC_MINIMAL_STACK_SIZE; /* linkage area */
	/* align the offset to 16 bytes: not sure this is needed here  */
	//offset += 16 - 1;
	//offset &= ~(16 - 1);

	/* add parameter area size for called functions */
	offset += m->param_area;
	offset += 16 - 1;
	offset &= ~(16 - 1);

	/* the MonoLMF structure is stored just below the stack pointer */
	if (MONO_TYPE_ISSTRUCT (sig->ret)) {
		offset += sizeof(gpointer) - 1;
		offset &= ~(sizeof(gpointer) - 1);

		m->vret_addr->opcode = OP_REGOFFSET;
		m->vret_addr->inst_basereg = frame_reg;
		m->vret_addr->inst_offset = offset;

		if (G_UNLIKELY (m->verbose_level > 1)) {
			printf ("vret_addr =");
			mono_print_ins (m->vret_addr);
		}

		offset += sizeof(gpointer);
	}

	offsets = mono_allocate_stack_slots (m, FALSE, &locals_stack_size, &locals_stack_align);
	if (locals_stack_align) {
		offset += (locals_stack_align - 1);
		offset &= ~(locals_stack_align - 1);
	}
	for (i = m->locals_start; i < m->num_varinfo; i++) {
		if (offsets [i] != -1) {
			MonoInst *inst = m->varinfo [i];
			inst->opcode = OP_REGOFFSET;
			inst->inst_basereg = frame_reg;
			inst->inst_offset = offset + offsets [i];
			/*
			g_print ("allocating local %d (%s) to %d\n",
				i, mono_type_get_name (inst->inst_vtype), inst->inst_offset);
			*/
		}
	}
	offset += locals_stack_size;

	curinst = 0;
	if (sig->hasthis) {
		inst = m->args [curinst];
		if (inst->opcode != OP_REGVAR) {
			inst->opcode = OP_REGOFFSET;
			inst->inst_basereg = frame_reg;
			offset += sizeof (target_mgreg_t) - 1;
			offset &= ~(sizeof (target_mgreg_t) - 1);
			inst->inst_offset = offset;
			offset += sizeof (target_mgreg_t);
		}
		curinst++;
	}

	for (i = 0; i < sig->param_count; ++i) {
		inst = m->args [curinst];
		if (inst->opcode != OP_REGVAR) {
			inst->opcode = OP_REGOFFSET;
			inst->inst_basereg = frame_reg;
			if (sig->pinvoke) {
				size = mono_type_native_stack_size (sig->params [i], (guint32*)&align);
				inst->backend.is_pinvoke = 1;
			} else {
				size = mono_type_size (sig->params [i], &align);
			}
			if (MONO_TYPE_ISSTRUCT (sig->params [i]) && size < sizeof (target_mgreg_t))
				size = align = sizeof (target_mgreg_t);
			/* 
			 * Use at least 4/8 byte alignment, since these might be passed in registers, and
			 * they are saved using std in the prolog.
			 */
			align = sizeof (target_mgreg_t);
			offset += align - 1;
			offset &= ~(align - 1);
			inst->inst_offset = offset;
			offset += size;
		}
		curinst++;
	}

	/* some storage for fp conversions */
	offset += 8 - 1;
	offset &= ~(8 - 1);
	m->arch.fp_conv_var_offset = offset;
	offset += 8;

	/* align the offset to 16 bytes */
	offset += 16 - 1;
	offset &= ~(16 - 1);

	/* change sign? */
	m->stack_offset = offset;

	if (sig->call_convention == MONO_CALL_VARARG) {
		CallInfo *cinfo = get_call_info (m->method->signature);

		m->sig_cookie = cinfo->sig_cookie.offset;

		g_free(cinfo);
	}
}

void
mono_arch_create_vars (MonoCompile *cfg)
{
	MonoMethodSignature *sig = mono_method_signature_internal (cfg->method);

	if (MONO_TYPE_ISSTRUCT (sig->ret)) {
		cfg->vret_addr = mono_compile_create_var (cfg, mono_get_int_type (), OP_ARG);
	}
}

/* Fixme: we need an alignment solution for enter_method and mono_arch_call_opcode,
 * currently alignment in mono_arch_call_opcode is computed without arch_get_argument_info 
 */

static void
emit_sig_cookie (MonoCompile *cfg, MonoCallInst *call, CallInfo *cinfo)
{
	int sig_reg = mono_alloc_ireg (cfg);

	/* FIXME: Add support for signature tokens to AOT */
	cfg->disable_aot = TRUE;

	MONO_EMIT_NEW_ICONST (cfg, sig_reg, (gulong)call->signature);
	MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG,
			ppc_r1, cinfo->sig_cookie.offset, sig_reg);
}

void
mono_arch_emit_call (MonoCompile *cfg, MonoCallInst *call)
{
	MonoInst *in, *ins;
	MonoMethodSignature *sig;
	int i, n;
	CallInfo *cinfo;

	sig = call->signature;
	n = sig->param_count + sig->hasthis;
	
	cinfo = get_call_info (sig);

	for (i = 0; i < n; ++i) {
		ArgInfo *ainfo = cinfo->args + i;
		MonoType *t;

		if (i >= sig->hasthis)
			t = sig->params [i - sig->hasthis];
		else
			t = mono_get_int_type ();
		t = mini_get_underlying_type (t);

		if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG) && (i == sig->sentinelpos))
			emit_sig_cookie (cfg, call, cinfo);

		in = call->args [i];

		if (ainfo->regtype == RegTypeGeneral) {
#ifndef __mono_ppc64__
			if (!t->byref && ((t->type == MONO_TYPE_I8) || (t->type == MONO_TYPE_U8))) {
				MONO_INST_NEW (cfg, ins, OP_MOVE);
				ins->dreg = mono_alloc_ireg (cfg);
				ins->sreg1 = MONO_LVREG_LS (in->dreg);
				MONO_ADD_INS (cfg->cbb, ins);
				mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, ainfo->reg + 1, FALSE);

				MONO_INST_NEW (cfg, ins, OP_MOVE);
				ins->dreg = mono_alloc_ireg (cfg);
				ins->sreg1 = MONO_LVREG_MS (in->dreg);
				MONO_ADD_INS (cfg->cbb, ins);
				mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, ainfo->reg, FALSE);
			} else
#endif
			{
				MONO_INST_NEW (cfg, ins, OP_MOVE);
				ins->dreg = mono_alloc_ireg (cfg);
				ins->sreg1 = in->dreg;
				MONO_ADD_INS (cfg->cbb, ins);

				mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, ainfo->reg, FALSE);
			}
		} else if (ainfo->regtype == RegTypeStructByAddr) {
			MONO_INST_NEW (cfg, ins, OP_OUTARG_VT);
			ins->opcode = OP_OUTARG_VT;
			ins->sreg1 = in->dreg;
			ins->klass = in->klass;
			ins->inst_p0 = call;
			ins->inst_p1 = mono_mempool_alloc (cfg->mempool, sizeof (ArgInfo));
			memcpy (ins->inst_p1, ainfo, sizeof (ArgInfo));
			MONO_ADD_INS (cfg->cbb, ins);
		} else if (ainfo->regtype == RegTypeStructByVal) {
			/* this is further handled in mono_arch_emit_outarg_vt () */
			MONO_INST_NEW (cfg, ins, OP_OUTARG_VT);
			ins->opcode = OP_OUTARG_VT;
			ins->sreg1 = in->dreg;
			ins->klass = in->klass;
			ins->inst_p0 = call;
			ins->inst_p1 = mono_mempool_alloc (cfg->mempool, sizeof (ArgInfo));
			memcpy (ins->inst_p1, ainfo, sizeof (ArgInfo));
			MONO_ADD_INS (cfg->cbb, ins);
		} else if (ainfo->regtype == RegTypeFPStructByVal) {
			/* this is further handled in mono_arch_emit_outarg_vt () */
			MONO_INST_NEW (cfg, ins, OP_OUTARG_VT);
			ins->opcode = OP_OUTARG_VT;
			ins->sreg1 = in->dreg;
			ins->klass = in->klass;
			ins->inst_p0 = call;
			ins->inst_p1 = mono_mempool_alloc (cfg->mempool, sizeof (ArgInfo));
			memcpy (ins->inst_p1, ainfo, sizeof (ArgInfo));
			MONO_ADD_INS (cfg->cbb, ins);
			cfg->flags |= MONO_CFG_HAS_FPOUT;
		} else if (ainfo->regtype == RegTypeBase) {
			if (!t->byref && ((t->type == MONO_TYPE_I8) || (t->type == MONO_TYPE_U8))) {
				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI8_MEMBASE_REG, ppc_r1, ainfo->offset, in->dreg);
			} else if (!t->byref && ((t->type == MONO_TYPE_R4) || (t->type == MONO_TYPE_R8))) {
				if (t->type == MONO_TYPE_R8)
					MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORER8_MEMBASE_REG, ppc_r1, ainfo->offset, in->dreg);
				else
					MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORER4_MEMBASE_REG, ppc_r1, ainfo->offset, in->dreg);
			} else {
				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, ppc_r1, ainfo->offset, in->dreg);
			}
		} else if (ainfo->regtype == RegTypeFP) {
			if (t->type == MONO_TYPE_VALUETYPE) {
				/* this is further handled in mono_arch_emit_outarg_vt () */
				MONO_INST_NEW (cfg, ins, OP_OUTARG_VT);
				ins->opcode = OP_OUTARG_VT;
				ins->sreg1 = in->dreg;
				ins->klass = in->klass;
				ins->inst_p0 = call;
				ins->inst_p1 = mono_mempool_alloc (cfg->mempool, sizeof (ArgInfo));
				memcpy (ins->inst_p1, ainfo, sizeof (ArgInfo));
				MONO_ADD_INS (cfg->cbb, ins);

				cfg->flags |= MONO_CFG_HAS_FPOUT;
			} else {
				int dreg = mono_alloc_freg (cfg);

				if (ainfo->size == 4) {
					MONO_EMIT_NEW_UNALU (cfg, OP_FCONV_TO_R4, dreg, in->dreg);
				} else {
					MONO_INST_NEW (cfg, ins, OP_FMOVE);
					ins->dreg = dreg;
					ins->sreg1 = in->dreg;
					MONO_ADD_INS (cfg->cbb, ins);
				}

				mono_call_inst_add_outarg_reg (cfg, call, dreg, ainfo->reg, TRUE);
				cfg->flags |= MONO_CFG_HAS_FPOUT;
			}
		} else {
			g_assert_not_reached ();
		}
	}

	/* Emit the signature cookie in the case that there is no
	   additional argument */
	if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG) && (n == sig->sentinelpos))
		emit_sig_cookie (cfg, call, cinfo);

	if (cinfo->struct_ret) {
		MonoInst *vtarg;

		MONO_INST_NEW (cfg, vtarg, OP_MOVE);
		vtarg->sreg1 = call->vret_var->dreg;
		vtarg->dreg = mono_alloc_preg (cfg);
		MONO_ADD_INS (cfg->cbb, vtarg);

		mono_call_inst_add_outarg_reg (cfg, call, vtarg->dreg, cinfo->struct_ret, FALSE);
	}

	call->stack_usage = cinfo->stack_usage;
	cfg->param_area = MAX (PPC_MINIMAL_PARAM_AREA_SIZE, MAX (cfg->param_area, cinfo->stack_usage));
	cfg->flags |= MONO_CFG_HAS_CALLS;

	g_free (cinfo);
}

#ifndef DISABLE_JIT

void
mono_arch_emit_outarg_vt (MonoCompile *cfg, MonoInst *ins, MonoInst *src)
{
	MonoCallInst *call = (MonoCallInst*)ins->inst_p0;
	ArgInfo *ainfo = (ArgInfo*)ins->inst_p1;
	int ovf_size = ainfo->vtsize;
	int doffset = ainfo->offset;
	int i, soffset, dreg;

	if (ainfo->regtype == RegTypeStructByVal) {
#ifdef __APPLE__
		guint32 size = 0;
#endif
		soffset = 0;
#ifdef __APPLE__
		/*
		 * Darwin pinvokes needs some special handling for 1
		 * and 2 byte arguments
		 */
		g_assert (ins->klass);
		if (call->signature->pinvoke)
			size =  mono_class_native_size (ins->klass, NULL);
		if (size == 2 || size == 1) {
			int tmpr = mono_alloc_ireg (cfg);
			if (size == 1)
				MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADI1_MEMBASE, tmpr, src->dreg, soffset);
			else
				MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADI2_MEMBASE, tmpr, src->dreg, soffset);
			dreg = mono_alloc_ireg (cfg);
			MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, dreg, tmpr);
			mono_call_inst_add_outarg_reg (cfg, call, dreg, ainfo->reg, FALSE);
		} else
#endif
			for (i = 0; i < ainfo->vtregs; ++i) {
	 			dreg = mono_alloc_ireg (cfg);
#if G_BYTE_ORDER == G_BIG_ENDIAN
				int antipadding = 0;
				if (ainfo->bytes) {
					g_assert (i == 0);
					antipadding = sizeof (target_mgreg_t) - ainfo->bytes;
				}
				MONO_EMIT_NEW_LOAD_MEMBASE (cfg, dreg, src->dreg, soffset);
				if (antipadding)
					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SHR_UN_IMM, dreg, dreg, antipadding * 8);
#else
				MONO_EMIT_NEW_LOAD_MEMBASE (cfg, dreg, src->dreg, soffset);
#endif
				mono_call_inst_add_outarg_reg (cfg, call, dreg, ainfo->reg + i, FALSE);
				soffset += sizeof (target_mgreg_t);
			}
		if (ovf_size != 0)
			mini_emit_memcpy (cfg, ppc_r1, doffset + soffset, src->dreg, soffset, ovf_size * sizeof (target_mgreg_t), TARGET_SIZEOF_VOID_P);
	} else if (ainfo->regtype == RegTypeFPStructByVal) {
		soffset = 0;
		for (i = 0; i < ainfo->vtregs; ++i) {
			int tmpr = mono_alloc_freg (cfg);
			if (ainfo->size == 4)
				MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADR4_MEMBASE, tmpr, src->dreg, soffset);
			else // ==8
				MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADR8_MEMBASE, tmpr, src->dreg, soffset);
			dreg = mono_alloc_freg (cfg);
			MONO_EMIT_NEW_UNALU (cfg, OP_FMOVE, dreg, tmpr);
			mono_call_inst_add_outarg_reg (cfg, call, dreg, ainfo->reg+i, TRUE);
			soffset += ainfo->size;
			}
		if (ovf_size != 0)
			mini_emit_memcpy (cfg, ppc_r1, doffset + soffset, src->dreg, soffset, ovf_size * sizeof (target_mgreg_t), TARGET_SIZEOF_VOID_P);
	} else if (ainfo->regtype == RegTypeFP) {
		int tmpr = mono_alloc_freg (cfg);
		if (ainfo->size == 4)
			MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADR4_MEMBASE, tmpr, src->dreg, 0);
		else
			MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADR8_MEMBASE, tmpr, src->dreg, 0);
		dreg = mono_alloc_freg (cfg);
		MONO_EMIT_NEW_UNALU (cfg, OP_FMOVE, dreg, tmpr);
		mono_call_inst_add_outarg_reg (cfg, call, dreg, ainfo->reg, TRUE);
	} else {
		MonoInst *vtcopy = mono_compile_create_var (cfg, m_class_get_byval_arg (src->klass), OP_LOCAL);
		MonoInst *load;
		guint32 size;

		/* FIXME: alignment? */
		if (call->signature->pinvoke) {
			size = mono_type_native_stack_size (m_class_get_byval_arg (src->klass), NULL);
			vtcopy->backend.is_pinvoke = 1;
		} else {
			size = mini_type_stack_size (m_class_get_byval_arg (src->klass), NULL);
		}
		if (size > 0)
			g_assert (ovf_size > 0);

		EMIT_NEW_VARLOADA (cfg, load, vtcopy, vtcopy->inst_vtype);
		mini_emit_memcpy (cfg, load->dreg, 0, src->dreg, 0, size, TARGET_SIZEOF_VOID_P);

		if (ainfo->offset)
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, ppc_r1, ainfo->offset, load->dreg);
		else
			mono_call_inst_add_outarg_reg (cfg, call, load->dreg, ainfo->reg, FALSE);
	}
}

void
mono_arch_emit_setret (MonoCompile *cfg, MonoMethod *method, MonoInst *val)
{
	MonoType *ret = mini_get_underlying_type (mono_method_signature_internal (method)->ret);
	if (!ret->byref) {
#ifndef __mono_ppc64__
		if (ret->type == MONO_TYPE_I8 || ret->type == MONO_TYPE_U8) {
			MonoInst *ins;

			MONO_INST_NEW (cfg, ins, OP_SETLRET);
			ins->sreg1 = MONO_LVREG_LS (val->dreg);
			ins->sreg2 = MONO_LVREG_MS (val->dreg);
			MONO_ADD_INS (cfg->cbb, ins);
			return;
		}
#endif
		if (ret->type == MONO_TYPE_R8 || ret->type == MONO_TYPE_R4) {
			MONO_EMIT_NEW_UNALU (cfg, OP_FMOVE, cfg->ret->dreg, val->dreg);
			return;
		}
	}
	MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, cfg->ret->dreg, val->dreg);
}

gboolean
mono_arch_is_inst_imm (int opcode, int imm_opcode, gint64 imm)
{
       return TRUE;
}

#endif /* DISABLE_JIT */

/*
 * Conditional branches have a small offset, so if it is likely overflowed,
 * we do a branch to the end of the method (uncond branches have much larger
 * offsets) where we perform the conditional and jump back unconditionally.
 * It's slightly slower, since we add two uncond branches, but it's very simple
 * with the current patch implementation and such large methods are likely not
 * going to be perf critical anyway.
 */
typedef struct {
	union {
		MonoBasicBlock *bb;
		const char *exception;
	} data;
	guint32 ip_offset;
	guint16 b0_cond;
	guint16 b1_cond;
} MonoOvfJump;

#define EMIT_COND_BRANCH_FLAGS(ins,b0,b1) \
if (0 && ins->inst_true_bb->native_offset) { \
	ppc_bc (code, (b0), (b1), (code - cfg->native_code + ins->inst_true_bb->native_offset) & 0xffff); \
} else { \
	int br_disp = ins->inst_true_bb->max_offset - offset;	\
	if (!ppc_is_imm16 (br_disp + 8 * 1024) || !ppc_is_imm16 (br_disp - 8 * 1024)) {	\
		MonoOvfJump *ovfj = mono_mempool_alloc (cfg->mempool, sizeof (MonoOvfJump));	\
		ovfj->data.bb = ins->inst_true_bb;	\
		ovfj->ip_offset = 0;	\
		ovfj->b0_cond = (b0);	\
		ovfj->b1_cond = (b1);	\
		mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_BB_OVF, ovfj); \
		ppc_b (code, 0);	\
	} else {	\
		mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_BB, ins->inst_true_bb); \
		ppc_bc (code, (b0), (b1), 0);	\
	}	\
}

#define EMIT_COND_BRANCH(ins,cond) EMIT_COND_BRANCH_FLAGS(ins, branch_b0_table [(cond)], branch_b1_table [(cond)])

/* emit an exception if condition is fail
 *
 * We assign the extra code used to throw the implicit exceptions
 * to cfg->bb_exit as far as the big branch handling is concerned
 */
#define EMIT_COND_SYSTEM_EXCEPTION_FLAGS(b0,b1,exc_name)            \
        do {                                                        \
		int br_disp = cfg->bb_exit->max_offset - offset;	\
		if (!ppc_is_imm16 (br_disp + 1024) || ! ppc_is_imm16 (ppc_is_imm16 (br_disp - 1024))) {	\
			MonoOvfJump *ovfj = mono_mempool_alloc (cfg->mempool, sizeof (MonoOvfJump));	\
			ovfj->data.exception = (exc_name);	\
			ovfj->ip_offset = code - cfg->native_code;	\
			ovfj->b0_cond = (b0);	\
			ovfj->b1_cond = (b1);	\
		        mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_EXC_OVF, ovfj); \
			ppc_bl (code, 0);	\
			cfg->bb_exit->max_offset += 24;	\
		} else {	\
			mono_add_patch_info (cfg, code - cfg->native_code,   \
				    MONO_PATCH_INFO_EXC, exc_name);  \
			ppc_bcl (code, (b0), (b1), 0);	\
		}	\
	} while (0); 

#define EMIT_COND_SYSTEM_EXCEPTION(cond,exc_name) EMIT_COND_SYSTEM_EXCEPTION_FLAGS(branch_b0_table [(cond)], branch_b1_table [(cond)], (exc_name))

void
mono_arch_peephole_pass_1 (MonoCompile *cfg, MonoBasicBlock *bb)
{
}

static int
normalize_opcode (int opcode)
{
	switch (opcode) {
#ifndef MONO_ARCH_ILP32
	case MONO_PPC_32_64_CASE (OP_LOADI4_MEMBASE, OP_LOADI8_MEMBASE):
		return OP_LOAD_MEMBASE;
	case MONO_PPC_32_64_CASE (OP_LOADI4_MEMINDEX, OP_LOADI8_MEMINDEX):
		return OP_LOAD_MEMINDEX;
	case MONO_PPC_32_64_CASE (OP_STOREI4_MEMBASE_REG, OP_STOREI8_MEMBASE_REG):
		return OP_STORE_MEMBASE_REG;
	case MONO_PPC_32_64_CASE (OP_STOREI4_MEMBASE_IMM, OP_STOREI8_MEMBASE_IMM):
		return OP_STORE_MEMBASE_IMM;
	case MONO_PPC_32_64_CASE (OP_STOREI4_MEMINDEX, OP_STOREI8_MEMINDEX):
		return OP_STORE_MEMINDEX;
#endif
	case MONO_PPC_32_64_CASE (OP_ISHR_IMM, OP_LSHR_IMM):
		return OP_SHR_IMM;
	case MONO_PPC_32_64_CASE (OP_ISHR_UN_IMM, OP_LSHR_UN_IMM):
		return OP_SHR_UN_IMM;
	default:
		return opcode;
	}
}

void
mono_arch_peephole_pass_2 (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins, *n, *last_ins = NULL;

	MONO_BB_FOR_EACH_INS_SAFE (bb, n, ins) {
		switch (normalize_opcode (ins->opcode)) {
		case OP_MUL_IMM: 
			/* remove unnecessary multiplication with 1 */
			if (ins->inst_imm == 1) {
				if (ins->dreg != ins->sreg1) {
					ins->opcode = OP_MOVE;
				} else {
					MONO_DELETE_INS (bb, ins);
					continue;
				}
			} else {
				int power2 = mono_is_power_of_two (ins->inst_imm);
				if (power2 > 0) {
					ins->opcode = OP_SHL_IMM;
					ins->inst_imm = power2;
				}
			}
			break;
		case OP_LOAD_MEMBASE:
			/* 
			 * OP_STORE_MEMBASE_REG reg, offset(basereg) 
			 * OP_LOAD_MEMBASE offset(basereg), reg
			 */
			if (last_ins && normalize_opcode (last_ins->opcode) == OP_STORE_MEMBASE_REG &&
			    ins->inst_basereg == last_ins->inst_destbasereg &&
			    ins->inst_offset == last_ins->inst_offset) {
				if (ins->dreg == last_ins->sreg1) {
					MONO_DELETE_INS (bb, ins);
					continue;
				} else {
					//static int c = 0; printf ("MATCHX %s %d\n", cfg->method->name,c++);
					ins->opcode = OP_MOVE;
					ins->sreg1 = last_ins->sreg1;
				}

			/* 
			 * Note: reg1 must be different from the basereg in the second load
			 * OP_LOAD_MEMBASE offset(basereg), reg1
			 * OP_LOAD_MEMBASE offset(basereg), reg2
			 * -->
			 * OP_LOAD_MEMBASE offset(basereg), reg1
			 * OP_MOVE reg1, reg2
			 */
			} else if (last_ins && normalize_opcode (last_ins->opcode) == OP_LOAD_MEMBASE &&
			      ins->inst_basereg != last_ins->dreg &&
			      ins->inst_basereg == last_ins->inst_basereg &&
			      ins->inst_offset == last_ins->inst_offset) {

				if (ins->dreg == last_ins->dreg) {
					MONO_DELETE_INS (bb, ins);
					continue;
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
			} else if (last_ins && normalize_opcode (last_ins->opcode) == OP_STORE_MEMBASE_IMM &&
				   ins->inst_basereg == last_ins->inst_destbasereg &&
				   ins->inst_offset == last_ins->inst_offset) {
				//static int c = 0; printf ("MATCHX %s %d\n", cfg->method->name,c++);
				ins->opcode = OP_ICONST;
				ins->inst_c0 = last_ins->inst_imm;
				g_assert_not_reached (); // check this rule
#endif
			}
			break;
		case OP_LOADU1_MEMBASE:
		case OP_LOADI1_MEMBASE:
			if (last_ins && (last_ins->opcode == OP_STOREI1_MEMBASE_REG) &&
					ins->inst_basereg == last_ins->inst_destbasereg &&
					ins->inst_offset == last_ins->inst_offset) {
				ins->opcode = (ins->opcode == OP_LOADI1_MEMBASE) ? OP_ICONV_TO_I1 : OP_ICONV_TO_U1;
				ins->sreg1 = last_ins->sreg1;				
			}
			break;
		case OP_LOADU2_MEMBASE:
		case OP_LOADI2_MEMBASE:
			if (last_ins && (last_ins->opcode == OP_STOREI2_MEMBASE_REG) &&
					ins->inst_basereg == last_ins->inst_destbasereg &&
					ins->inst_offset == last_ins->inst_offset) {
				ins->opcode = (ins->opcode == OP_LOADI2_MEMBASE) ? OP_ICONV_TO_I2 : OP_ICONV_TO_U2;
				ins->sreg1 = last_ins->sreg1;				
			}
			break;
#ifdef __mono_ppc64__
		case OP_LOADU4_MEMBASE:
		case OP_LOADI4_MEMBASE:
			if (last_ins && (last_ins->opcode == OP_STOREI4_MEMBASE_REG) &&
					ins->inst_basereg == last_ins->inst_destbasereg &&
					ins->inst_offset == last_ins->inst_offset) {
				ins->opcode = (ins->opcode == OP_LOADI4_MEMBASE) ? OP_ICONV_TO_I4 : OP_ICONV_TO_U4;
				ins->sreg1 = last_ins->sreg1;
			}
			break;
#endif
		case OP_MOVE:
			ins->opcode = OP_MOVE;
			/* 
			 * OP_MOVE reg, reg 
			 */
			if (ins->dreg == ins->sreg1) {
				MONO_DELETE_INS (bb, ins);
				continue;
			}
			/* 
			 * OP_MOVE sreg, dreg 
			 * OP_MOVE dreg, sreg
			 */
			if (last_ins && last_ins->opcode == OP_MOVE &&
			    ins->sreg1 == last_ins->dreg &&
			    ins->dreg == last_ins->sreg1) {
				MONO_DELETE_INS (bb, ins);
				continue;
			}
			break;
		}
		last_ins = ins;
		ins = ins->next;
	}
	bb->last_ins = last_ins;
}

void
mono_arch_decompose_opts (MonoCompile *cfg, MonoInst *ins)
{
	switch (ins->opcode) {
	case OP_ICONV_TO_R_UN: {
		// This value is OK as-is for both big and little endian because of how it is stored
		static const guint64 adjust_val = 0x4330000000000000ULL;
		int msw_reg = mono_alloc_ireg (cfg);
		int adj_reg = mono_alloc_freg (cfg);
		int tmp_reg = mono_alloc_freg (cfg);
		int basereg = ppc_sp;
		int offset = -8;
		MONO_EMIT_NEW_ICONST (cfg, msw_reg, 0x43300000);
		if (!ppc_is_imm16 (offset + 4)) {
			basereg = mono_alloc_ireg (cfg);
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_IADD_IMM, basereg, cfg->frame_reg, offset);
		}
#if G_BYTE_ORDER == G_BIG_ENDIAN
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI4_MEMBASE_REG, basereg, offset, msw_reg);
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI4_MEMBASE_REG, basereg, offset + 4, ins->sreg1);
#else
		// For little endian the words are reversed
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI4_MEMBASE_REG, basereg, offset + 4, msw_reg);
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI4_MEMBASE_REG, basereg, offset, ins->sreg1);
#endif
		MONO_EMIT_NEW_LOAD_R8 (cfg, adj_reg, &adjust_val);
		MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADR8_MEMBASE, tmp_reg, basereg, offset);
		MONO_EMIT_NEW_BIALU (cfg, OP_FSUB, ins->dreg, tmp_reg, adj_reg);
		ins->opcode = OP_NOP;
		break;
	}
#ifndef __mono_ppc64__
	case OP_ICONV_TO_R4:
	case OP_ICONV_TO_R8: {
		/* If we have a PPC_FEATURE_64 machine we can avoid
		   this and use the fcfid instruction.  Otherwise
		   on an old 32-bit chip and we have to do this the
		   hard way.  */
		if (!(cpu_hw_caps & PPC_ISA_64)) {
			/* FIXME: change precision for CEE_CONV_R4 */
			static const guint64 adjust_val = 0x4330000080000000ULL;
			int msw_reg = mono_alloc_ireg (cfg);
			int xored = mono_alloc_ireg (cfg);
			int adj_reg = mono_alloc_freg (cfg);
			int tmp_reg = mono_alloc_freg (cfg);
			int basereg = ppc_sp;
			int offset = -8;
			if (!ppc_is_imm16 (offset + 4)) {
				basereg = mono_alloc_ireg (cfg);
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_IADD_IMM, basereg, cfg->frame_reg, offset);
			}
			MONO_EMIT_NEW_ICONST (cfg, msw_reg, 0x43300000);
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI4_MEMBASE_REG, basereg, offset, msw_reg);
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_XOR_IMM, xored, ins->sreg1, 0x80000000);
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI4_MEMBASE_REG, basereg, offset + 4, xored);
			MONO_EMIT_NEW_LOAD_R8 (cfg, adj_reg, (gpointer)&adjust_val);
			MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADR8_MEMBASE, tmp_reg, basereg, offset);
			MONO_EMIT_NEW_BIALU (cfg, OP_FSUB, ins->dreg, tmp_reg, adj_reg);
			if (ins->opcode == OP_ICONV_TO_R4)
				MONO_EMIT_NEW_UNALU (cfg, OP_FCONV_TO_R4, ins->dreg, ins->dreg);
			ins->opcode = OP_NOP;
		}
		break;
	}
#endif
	case OP_CKFINITE: {
		int msw_reg = mono_alloc_ireg (cfg);
		int basereg = ppc_sp;
		int offset = -8;
		if (!ppc_is_imm16 (offset + 4)) {
			basereg = mono_alloc_ireg (cfg);
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_IADD_IMM, basereg, cfg->frame_reg, offset);
		}
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORER8_MEMBASE_REG, basereg, offset, ins->sreg1);
#if G_BYTE_ORDER == G_BIG_ENDIAN
		MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADI4_MEMBASE, msw_reg, basereg, offset);
#else
		MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADI4_MEMBASE, msw_reg, basereg, offset+4);
#endif
		MONO_EMIT_NEW_UNALU (cfg, OP_PPC_CHECK_FINITE, -1, msw_reg);
		MONO_EMIT_NEW_UNALU (cfg, OP_FMOVE, ins->dreg, ins->sreg1);
		ins->opcode = OP_NOP;
		break;
	}
#ifdef __mono_ppc64__
	case OP_IADD_OVF:
	case OP_IADD_OVF_UN:
	case OP_ISUB_OVF: {
		int shifted1_reg = mono_alloc_ireg (cfg);
		int shifted2_reg = mono_alloc_ireg (cfg);
		int result_shifted_reg = mono_alloc_ireg (cfg);

		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SHL_IMM, shifted1_reg, ins->sreg1, 32);
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SHL_IMM, shifted2_reg, ins->sreg2, 32);
		MONO_EMIT_NEW_BIALU (cfg, ins->opcode, result_shifted_reg, shifted1_reg, shifted2_reg);
		if (ins->opcode == OP_IADD_OVF_UN)
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SHR_UN_IMM, ins->dreg, result_shifted_reg, 32);
		else
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SHR_IMM, ins->dreg, result_shifted_reg, 32);
		ins->opcode = OP_NOP;
		break;
	}
#endif
	default:
		break;
	}
}

void
mono_arch_decompose_long_opts (MonoCompile *cfg, MonoInst *ins)
{
	switch (ins->opcode) {
	case OP_LADD_OVF:
		/* ADC sets the condition code */
		MONO_EMIT_NEW_BIALU (cfg, OP_ADDCC, MONO_LVREG_LS (ins->dreg), MONO_LVREG_LS (ins->sreg1), MONO_LVREG_LS (ins->sreg2));
		MONO_EMIT_NEW_BIALU (cfg, OP_ADD_OVF_CARRY, MONO_LVREG_MS (ins->dreg), MONO_LVREG_MS (ins->sreg1), MONO_LVREG_MS (ins->sreg2));
		NULLIFY_INS (ins);
		break;
	case OP_LADD_OVF_UN:
		/* ADC sets the condition code */
		MONO_EMIT_NEW_BIALU (cfg, OP_ADDCC, MONO_LVREG_LS (ins->dreg), MONO_LVREG_LS (ins->sreg1), MONO_LVREG_LS (ins->sreg2));
		MONO_EMIT_NEW_BIALU (cfg, OP_ADD_OVF_UN_CARRY, MONO_LVREG_MS (ins->dreg), MONO_LVREG_MS (ins->sreg1), MONO_LVREG_MS (ins->sreg2));
		NULLIFY_INS (ins);
		break;
	case OP_LSUB_OVF:
		/* SBB sets the condition code */
		MONO_EMIT_NEW_BIALU (cfg, OP_SUBCC, MONO_LVREG_LS (ins->dreg), MONO_LVREG_LS (ins->sreg1), MONO_LVREG_LS (ins->sreg2));
		MONO_EMIT_NEW_BIALU (cfg, OP_SUB_OVF_CARRY, MONO_LVREG_MS (ins->dreg), MONO_LVREG_MS (ins->sreg1), MONO_LVREG_MS (ins->sreg2));
		NULLIFY_INS (ins);
		break;
	case OP_LSUB_OVF_UN:
		/* SBB sets the condition code */
		MONO_EMIT_NEW_BIALU (cfg, OP_SUBCC, MONO_LVREG_LS (ins->dreg), MONO_LVREG_LS (ins->sreg1), MONO_LVREG_LS (ins->sreg2));
		MONO_EMIT_NEW_BIALU (cfg, OP_SUB_OVF_UN_CARRY, MONO_LVREG_MS (ins->dreg), MONO_LVREG_MS (ins->sreg1), MONO_LVREG_MS (ins->sreg2));
		NULLIFY_INS (ins);
		break;
	case OP_LNEG:
		/* From gcc generated code */
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_PPC_SUBFIC, MONO_LVREG_LS (ins->dreg), MONO_LVREG_LS (ins->sreg1), 0);
		MONO_EMIT_NEW_UNALU (cfg, OP_PPC_SUBFZE, MONO_LVREG_MS (ins->dreg), MONO_LVREG_MS (ins->sreg1));
		NULLIFY_INS (ins);
		break;
	default:
		break;
	}
}

/* 
 * the branch_b0_table should maintain the order of these
 * opcodes.
case CEE_BEQ:
case CEE_BGE:
case CEE_BGT:
case CEE_BLE:
case CEE_BLT:
case CEE_BNE_UN:
case CEE_BGE_UN:
case CEE_BGT_UN:
case CEE_BLE_UN:
case CEE_BLT_UN:
 */
static const guchar 
branch_b0_table [] = {
	PPC_BR_TRUE, 
	PPC_BR_FALSE, 
	PPC_BR_TRUE, 
	PPC_BR_FALSE, 
	PPC_BR_TRUE, 
	
	PPC_BR_FALSE, 
	PPC_BR_FALSE, 
	PPC_BR_TRUE, 
	PPC_BR_FALSE,
	PPC_BR_TRUE
};

static const guchar 
branch_b1_table [] = {
	PPC_BR_EQ, 
	PPC_BR_LT, 
	PPC_BR_GT, 
	PPC_BR_GT,
	PPC_BR_LT, 
	
	PPC_BR_EQ, 
	PPC_BR_LT, 
	PPC_BR_GT, 
	PPC_BR_GT,
	PPC_BR_LT 
};

#define NEW_INS(cfg,dest,op) do {					\
		MONO_INST_NEW((cfg), (dest), (op));			\
		mono_bblock_insert_after_ins (bb, last_ins, (dest));	\
	} while (0)

static int
map_to_reg_reg_op (int op)
{
	switch (op) {
	case OP_ADD_IMM:
		return OP_IADD;
	case OP_SUB_IMM:
		return OP_ISUB;
	case OP_AND_IMM:
		return OP_IAND;
	case OP_COMPARE_IMM:
		return OP_COMPARE;
	case OP_ICOMPARE_IMM:
		return OP_ICOMPARE;
	case OP_LCOMPARE_IMM:
		return OP_LCOMPARE;
	case OP_ADDCC_IMM:
		return OP_IADDCC;
	case OP_ADC_IMM:
		return OP_IADC;
	case OP_SUBCC_IMM:
		return OP_ISUBCC;
	case OP_SBB_IMM:
		return OP_ISBB;
	case OP_OR_IMM:
		return OP_IOR;
	case OP_XOR_IMM:
		return OP_IXOR;
	case OP_MUL_IMM:
		return OP_IMUL;
	case OP_LMUL_IMM:
		return OP_LMUL;
	case OP_LOAD_MEMBASE:
		return OP_LOAD_MEMINDEX;
	case OP_LOADI4_MEMBASE:
		return OP_LOADI4_MEMINDEX;
	case OP_LOADU4_MEMBASE:
		return OP_LOADU4_MEMINDEX;
	case OP_LOADI8_MEMBASE:
		return OP_LOADI8_MEMINDEX;
	case OP_LOADU1_MEMBASE:
		return OP_LOADU1_MEMINDEX;
	case OP_LOADI2_MEMBASE:
		return OP_LOADI2_MEMINDEX;
	case OP_LOADU2_MEMBASE:
		return OP_LOADU2_MEMINDEX;
	case OP_LOADI1_MEMBASE:
		return OP_LOADI1_MEMINDEX;
	case OP_LOADR4_MEMBASE:
		return OP_LOADR4_MEMINDEX;
	case OP_LOADR8_MEMBASE:
		return OP_LOADR8_MEMINDEX;
	case OP_STOREI1_MEMBASE_REG:
		return OP_STOREI1_MEMINDEX;
	case OP_STOREI2_MEMBASE_REG:
		return OP_STOREI2_MEMINDEX;
	case OP_STOREI4_MEMBASE_REG:
		return OP_STOREI4_MEMINDEX;
	case OP_STOREI8_MEMBASE_REG:
		return OP_STOREI8_MEMINDEX;
	case OP_STORE_MEMBASE_REG:
		return OP_STORE_MEMINDEX;
	case OP_STORER4_MEMBASE_REG:
		return OP_STORER4_MEMINDEX;
	case OP_STORER8_MEMBASE_REG:
		return OP_STORER8_MEMINDEX;
	case OP_STORE_MEMBASE_IMM:
		return OP_STORE_MEMBASE_REG;
	case OP_STOREI1_MEMBASE_IMM:
		return OP_STOREI1_MEMBASE_REG;
	case OP_STOREI2_MEMBASE_IMM:
		return OP_STOREI2_MEMBASE_REG;
	case OP_STOREI4_MEMBASE_IMM:
		return OP_STOREI4_MEMBASE_REG;
	case OP_STOREI8_MEMBASE_IMM:
		return OP_STOREI8_MEMBASE_REG;
	}
	if (mono_op_imm_to_op (op) == -1)
		g_error ("mono_op_imm_to_op failed for %s\n", mono_inst_name (op));
	return mono_op_imm_to_op (op);
}

//#define map_to_reg_reg_op(op) (cfg->new_ir? mono_op_imm_to_op (op): map_to_reg_reg_op (op))

#define compare_opcode_is_unsigned(opcode) \
		(((opcode) >= CEE_BNE_UN && (opcode) <= CEE_BLT_UN) ||	\
		((opcode) >= OP_IBNE_UN && (opcode) <= OP_IBLT_UN) ||	\
		((opcode) >= OP_LBNE_UN && (opcode) <= OP_LBLT_UN) ||	\
		((opcode) >= OP_COND_EXC_NE_UN && (opcode) <= OP_COND_EXC_LT_UN) ||	\
		((opcode) >= OP_COND_EXC_INE_UN && (opcode) <= OP_COND_EXC_ILT_UN) ||	\
		((opcode) == OP_CLT_UN || (opcode) == OP_CGT_UN ||	\
		 (opcode) == OP_ICLT_UN || (opcode) == OP_ICGT_UN ||	\
		 (opcode) == OP_LCLT_UN || (opcode) == OP_LCGT_UN))

/*
 * Remove from the instruction list the instructions that can't be
 * represented with very simple instructions with no register
 * requirements.
 */
void
mono_arch_lowering_pass (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins, *next, *temp, *last_ins = NULL;
	int imm;

	MONO_BB_FOR_EACH_INS (bb, ins) {
loop_start:
		switch (ins->opcode) {
		case OP_IDIV_UN_IMM:
		case OP_IDIV_IMM:
		case OP_IREM_IMM:
		case OP_IREM_UN_IMM:
		CASE_PPC64 (OP_LREM_IMM) {
			NEW_INS (cfg, temp, OP_ICONST);
			temp->inst_c0 = ins->inst_imm;
			temp->dreg = mono_alloc_ireg (cfg);
			ins->sreg2 = temp->dreg;
			if (ins->opcode == OP_IDIV_IMM)
				ins->opcode = OP_IDIV;
			else if (ins->opcode == OP_IREM_IMM)
				ins->opcode = OP_IREM;
			else if (ins->opcode == OP_IDIV_UN_IMM)
				ins->opcode = OP_IDIV_UN;
			else if (ins->opcode == OP_IREM_UN_IMM)
				ins->opcode = OP_IREM_UN;
			else if (ins->opcode == OP_LREM_IMM)
				ins->opcode = OP_LREM;
			last_ins = temp;
			/* handle rem separately */
			goto loop_start;
		}
		case OP_IREM:
		case OP_IREM_UN:
		CASE_PPC64 (OP_LREM)
		CASE_PPC64 (OP_LREM_UN) {
			MonoInst *mul;
			/* we change a rem dest, src1, src2 to
			 * div temp1, src1, src2
			 * mul temp2, temp1, src2
			 * sub dest, src1, temp2
			 */
			if (ins->opcode == OP_IREM || ins->opcode == OP_IREM_UN) {
				NEW_INS (cfg, mul, OP_IMUL);
				NEW_INS (cfg, temp, ins->opcode == OP_IREM? OP_IDIV: OP_IDIV_UN);
				ins->opcode = OP_ISUB;
			} else {
				NEW_INS (cfg, mul, OP_LMUL);
				NEW_INS (cfg, temp, ins->opcode == OP_LREM? OP_LDIV: OP_LDIV_UN);
				ins->opcode = OP_LSUB;
			}
			temp->sreg1 = ins->sreg1;
			temp->sreg2 = ins->sreg2;
			temp->dreg = mono_alloc_ireg (cfg);
			mul->sreg1 = temp->dreg;
			mul->sreg2 = ins->sreg2;
			mul->dreg = mono_alloc_ireg (cfg);
			ins->sreg2 = mul->dreg;
			break;
		}
		case OP_IADD_IMM:
		CASE_PPC64 (OP_LADD_IMM)
		case OP_ADD_IMM:
		case OP_ADDCC_IMM:
			if (!ppc_is_imm16 (ins->inst_imm)) {
				NEW_INS (cfg,  temp, OP_ICONST);
				temp->inst_c0 = ins->inst_imm;
				temp->dreg = mono_alloc_ireg (cfg);
				ins->sreg2 = temp->dreg;
				ins->opcode = map_to_reg_reg_op (ins->opcode);
			}
			break;
		case OP_ISUB_IMM:
		CASE_PPC64 (OP_LSUB_IMM)
		case OP_SUB_IMM:
			if (!ppc_is_imm16 (-ins->inst_imm)) {
				NEW_INS (cfg, temp, OP_ICONST);
				temp->inst_c0 = ins->inst_imm;
				temp->dreg = mono_alloc_ireg (cfg);
				ins->sreg2 = temp->dreg;
				ins->opcode = map_to_reg_reg_op (ins->opcode);
			}
			break;
		case OP_IAND_IMM:
		case OP_IOR_IMM:
		case OP_IXOR_IMM:
		case OP_LAND_IMM:
		case OP_LOR_IMM:
		case OP_LXOR_IMM:
		case OP_AND_IMM:
		case OP_OR_IMM:
		case OP_XOR_IMM: {
			gboolean is_imm = ((ins->inst_imm & 0xffff0000) && (ins->inst_imm & 0xffff));
#ifdef __mono_ppc64__
			if (ins->inst_imm & 0xffffffff00000000ULL)
				is_imm = TRUE;
#endif
			if (is_imm) {
				NEW_INS (cfg, temp, OP_ICONST);
				temp->inst_c0 = ins->inst_imm;
				temp->dreg = mono_alloc_ireg (cfg);
				ins->sreg2 = temp->dreg;
				ins->opcode = map_to_reg_reg_op (ins->opcode);
			}
			break;
		}
		case OP_ISBB_IMM:
		case OP_IADC_IMM:
		case OP_SBB_IMM:
		case OP_SUBCC_IMM:
		case OP_ADC_IMM:
			NEW_INS (cfg, temp, OP_ICONST);
			temp->inst_c0 = ins->inst_imm;
			temp->dreg = mono_alloc_ireg (cfg);
			ins->sreg2 = temp->dreg;
			ins->opcode = map_to_reg_reg_op (ins->opcode);
			break;
		case OP_COMPARE_IMM:
		case OP_ICOMPARE_IMM:
		CASE_PPC64 (OP_LCOMPARE_IMM)
			next = ins->next;
			/* Branch opts can eliminate the branch */
			if (!next || (!(MONO_IS_COND_BRANCH_OP (next) || MONO_IS_COND_EXC (next) || MONO_IS_SETCC (next)))) {
				ins->opcode = OP_NOP;
				break;
			}
			g_assert(next);
			if (compare_opcode_is_unsigned (next->opcode)) {
				if (!ppc_is_uimm16 (ins->inst_imm)) {
					NEW_INS (cfg, temp, OP_ICONST);
					temp->inst_c0 = ins->inst_imm;
					temp->dreg = mono_alloc_ireg (cfg);
					ins->sreg2 = temp->dreg;
					ins->opcode = map_to_reg_reg_op (ins->opcode);
				}
			} else {
				if (!ppc_is_imm16 (ins->inst_imm)) {
					NEW_INS (cfg, temp, OP_ICONST);
					temp->inst_c0 = ins->inst_imm;
					temp->dreg = mono_alloc_ireg (cfg);
					ins->sreg2 = temp->dreg;
					ins->opcode = map_to_reg_reg_op (ins->opcode);
				}
			}
			break;
		case OP_IMUL_IMM:
		case OP_MUL_IMM:
		CASE_PPC64 (OP_LMUL_IMM)
			if (ins->inst_imm == 1) {
				ins->opcode = OP_MOVE;
				break;
			}
			if (ins->inst_imm == 0) {
				ins->opcode = OP_ICONST;
				ins->inst_c0 = 0;
				break;
			}
			imm = mono_is_power_of_two (ins->inst_imm);
			if (imm > 0) {
				ins->opcode = OP_SHL_IMM;
				ins->inst_imm = imm;
				break;
			}
			if (!ppc_is_imm16 (ins->inst_imm)) {
				NEW_INS (cfg, temp, OP_ICONST);
				temp->inst_c0 = ins->inst_imm;
				temp->dreg = mono_alloc_ireg (cfg);
				ins->sreg2 = temp->dreg;
				ins->opcode = map_to_reg_reg_op (ins->opcode);
			}
			break;
		case OP_LOCALLOC_IMM:
			NEW_INS (cfg, temp, OP_ICONST);
			temp->inst_c0 = ins->inst_imm;
			temp->dreg = mono_alloc_ireg (cfg);
			ins->sreg1 = temp->dreg;
			ins->opcode = OP_LOCALLOC;
			break;
		case OP_LOAD_MEMBASE:
		case OP_LOADI4_MEMBASE:
		CASE_PPC64 (OP_LOADI8_MEMBASE)
		case OP_LOADU4_MEMBASE:
		case OP_LOADI2_MEMBASE:
		case OP_LOADU2_MEMBASE:
		case OP_LOADI1_MEMBASE:
		case OP_LOADU1_MEMBASE:
		case OP_LOADR4_MEMBASE:
		case OP_LOADR8_MEMBASE:
		case OP_STORE_MEMBASE_REG:
		CASE_PPC64 (OP_STOREI8_MEMBASE_REG)
		case OP_STOREI4_MEMBASE_REG:
		case OP_STOREI2_MEMBASE_REG:
		case OP_STOREI1_MEMBASE_REG:
		case OP_STORER4_MEMBASE_REG:
		case OP_STORER8_MEMBASE_REG:
			/* we can do two things: load the immed in a register
			 * and use an indexed load, or see if the immed can be
			 * represented as an ad_imm + a load with a smaller offset
			 * that fits. We just do the first for now, optimize later.
			 */
			if (ppc_is_imm16 (ins->inst_offset))
				break;
			NEW_INS (cfg, temp, OP_ICONST);
			temp->inst_c0 = ins->inst_offset;
			temp->dreg = mono_alloc_ireg (cfg);
			ins->sreg2 = temp->dreg;
			ins->opcode = map_to_reg_reg_op (ins->opcode);
			break;
		case OP_STORE_MEMBASE_IMM:
		case OP_STOREI1_MEMBASE_IMM:
		case OP_STOREI2_MEMBASE_IMM:
		case OP_STOREI4_MEMBASE_IMM:
		CASE_PPC64 (OP_STOREI8_MEMBASE_IMM)
			NEW_INS (cfg, temp, OP_ICONST);
			temp->inst_c0 = ins->inst_imm;
			temp->dreg = mono_alloc_ireg (cfg);
			ins->sreg1 = temp->dreg;
			ins->opcode = map_to_reg_reg_op (ins->opcode);
			last_ins = temp;
			goto loop_start; /* make it handle the possibly big ins->inst_offset */
		case OP_R8CONST:
		case OP_R4CONST:
			if (cfg->compile_aot) {
				/* Keep these in the aot case */
				break;
			}
			NEW_INS (cfg, temp, OP_ICONST);
			temp->inst_c0 = (gulong)ins->inst_p0;
			temp->dreg = mono_alloc_ireg (cfg);
			ins->inst_basereg = temp->dreg;
			ins->inst_offset = 0;
			ins->opcode = ins->opcode == OP_R4CONST? OP_LOADR4_MEMBASE: OP_LOADR8_MEMBASE;
			last_ins = temp;
			/* make it handle the possibly big ins->inst_offset
			 * later optimize to use lis + load_membase
			 */
			goto loop_start;
		}
		last_ins = ins;
	}
	bb->last_ins = last_ins;
	bb->max_vreg = cfg->next_vreg;	
}

static guchar*
emit_float_to_int (MonoCompile *cfg, guchar *code, int dreg, int sreg, int size, gboolean is_signed)
{
	long offset = cfg->arch.fp_conv_var_offset;
	long sub_offset;
	/* sreg is a float, dreg is an integer reg. ppc_f0 is used a scratch */
#ifdef __mono_ppc64__
	if (size == 8) {
		ppc_fctidz (code, ppc_f0, sreg);
		sub_offset = 0;
	} else
#endif
	{
		ppc_fctiwz (code, ppc_f0, sreg);
		sub_offset = 4;
	}
	if (ppc_is_imm16 (offset + sub_offset)) {
		ppc_stfd (code, ppc_f0, offset, cfg->frame_reg);
		if (size == 8)
			ppc_ldr (code, dreg, offset + sub_offset, cfg->frame_reg);
		else
			ppc_lwz (code, dreg, offset + sub_offset, cfg->frame_reg);
	} else {
		ppc_load (code, dreg, offset);
		ppc_add (code, dreg, dreg, cfg->frame_reg);
		ppc_stfd (code, ppc_f0, 0, dreg);
		if (size == 8)
			ppc_ldr (code, dreg, sub_offset, dreg);
		else
			ppc_lwz (code, dreg, sub_offset, dreg);
	}
	if (!is_signed) {
		if (size == 1)
			ppc_andid (code, dreg, dreg, 0xff);
		else if (size == 2)
			ppc_andid (code, dreg, dreg, 0xffff);
#ifdef __mono_ppc64__
		else if (size == 4)
			ppc_clrldi (code, dreg, dreg, 32);
#endif
	} else {
		if (size == 1)
			ppc_extsb (code, dreg, dreg);
		else if (size == 2)
			ppc_extsh (code, dreg, dreg);
#ifdef __mono_ppc64__
		else if (size == 4)
			ppc_extsw (code, dreg, dreg);
#endif
	}
	return code;
}

static void
emit_thunk (guint8 *code, gconstpointer target)
{
	guint8 *p = code;

	/* 2 bytes on 32bit, 5 bytes on 64bit */
	ppc_load_sequence (code, ppc_r0, target);

	ppc_mtctr (code, ppc_r0);
	ppc_bcctr (code, PPC_BR_ALWAYS, 0);

	mono_arch_flush_icache (p, code - p);
}

static void
handle_thunk (MonoCompile *cfg, MonoDomain *domain, guchar *code, const guchar *target)
{
	MonoJitInfo *ji = NULL;
	MonoThunkJitInfo *info;
	guint8 *thunks, *p;
	int thunks_size;
	guint8 *orig_target;
	guint8 *target_thunk;

	if (!domain)
		domain = mono_domain_get ();

	if (cfg) {
		/*
		 * This can be called multiple times during JITting,
		 * save the current position in cfg->arch to avoid
		 * doing a O(n^2) search.
		 */
		if (!cfg->arch.thunks) {
			cfg->arch.thunks = cfg->thunks;
			cfg->arch.thunks_size = cfg->thunk_area;
		}
		thunks = cfg->arch.thunks;
		thunks_size = cfg->arch.thunks_size;
		if (!thunks_size) {
			g_print ("thunk failed %p->%p, thunk space=%d method %s", code, target, thunks_size, mono_method_full_name (cfg->method, TRUE));
			g_assert_not_reached ();
		}

		g_assert (*(guint32*)thunks == 0);
		emit_thunk (thunks, target);
		ppc_patch (code, thunks);

		cfg->arch.thunks += THUNK_SIZE;
		cfg->arch.thunks_size -= THUNK_SIZE;
	} else {
		ji = mini_jit_info_table_find (code);
		g_assert (ji);
		info = mono_jit_info_get_thunk_info (ji);
		g_assert (info);

		thunks = (guint8 *) ji->code_start + info->thunks_offset;
		thunks_size = info->thunks_size;

		orig_target = mono_arch_get_call_target (code + 4);

		mono_mini_arch_lock ();

		target_thunk = NULL;
		if (orig_target >= thunks && orig_target < thunks + thunks_size) {
			/* The call already points to a thunk, because of trampolines etc. */
			target_thunk = orig_target;
		} else {
			for (p = thunks; p < thunks + thunks_size; p += THUNK_SIZE) {
				if (((guint32 *) p) [0] == 0) {
					/* Free entry */
					target_thunk = p;
					break;
				} else {
					/* ppc64 requires 5 instructions, 32bit two instructions */
#ifdef __mono_ppc64__
					const int const_load_size = 5;
#else
					const int const_load_size = 2;
#endif
					guint32 load [const_load_size];
					guchar *templ = (guchar *) load;
					ppc_load_sequence (templ, ppc_r0, target);
					if (!memcmp (p, load, const_load_size)) {
						/* Thunk already points to target */
						target_thunk = p;
						break;
					}
				}
			}
		}

		// g_print ("THUNK: %p %p %p\n", code, target, target_thunk);

		if (!target_thunk) {
			mono_mini_arch_unlock ();
			g_print ("thunk failed %p->%p, thunk space=%d method %s", code, target, thunks_size, cfg ? mono_method_full_name (cfg->method, TRUE) : mono_method_full_name (jinfo_get_method (ji), TRUE));
			g_assert_not_reached ();
		}

		emit_thunk (target_thunk, target);
		ppc_patch (code, target_thunk);

		mono_mini_arch_unlock ();
	}
}

static void
patch_ins (guint8 *code, guint32 ins)
{
	*(guint32*)code = ins;
	mono_arch_flush_icache (code, 4);
}

static void
ppc_patch_full (MonoCompile *cfg, MonoDomain *domain, guchar *code, const guchar *target, gboolean is_fd)
{
	guint32 ins = *(guint32*)code;
	guint32 prim = ins >> 26;
	guint32 ovf;

	//g_print ("patching 0x%08x (0x%08x) to point to 0x%08x\n", code, ins, target);
	if (prim == 18) {
		// prefer relative branches, they are more position independent (e.g. for AOT compilation).
		gint diff = target - code;
		g_assert (!is_fd);
		if (diff >= 0){
			if (diff <= 33554431){
				ins = (18 << 26) | (diff) | (ins & 1);
				patch_ins (code, ins);
				return;
			}
		} else {
			/* diff between 0 and -33554432 */
			if (diff >= -33554432){
				ins = (18 << 26) | (diff & ~0xfc000000) | (ins & 1);
				patch_ins (code, ins);
				return;
			}
		}
		
		if ((glong)target >= 0){
			if ((glong)target <= 33554431){
				ins = (18 << 26) | ((gulong) target) | (ins & 1) | 2;
				patch_ins (code, ins);
				return;
			}
		} else {
			if ((glong)target >= -33554432){
				ins = (18 << 26) | (((gulong)target) & ~0xfc000000) | (ins & 1) | 2;
				patch_ins (code, ins);
				return;
			}
		}

		handle_thunk (cfg, domain, code, target);
		return;

		g_assert_not_reached ();
	}
	
	
	if (prim == 16) {
		g_assert (!is_fd);
		// absolute address
		if (ins & 2) {
			guint32 li = (gulong)target;
			ins = (ins & 0xffff0000) | (ins & 3);
			ovf  = li & 0xffff0000;
			if (ovf != 0 && ovf != 0xffff0000)
				g_assert_not_reached ();
			li &= 0xffff;
			ins |= li;
			// FIXME: assert the top bits of li are 0
		} else {
			gint diff = target - code;
			ins = (ins & 0xffff0000) | (ins & 3);
			ovf  = diff & 0xffff0000;
			if (ovf != 0 && ovf != 0xffff0000)
				g_assert_not_reached ();
			diff &= 0xffff;
			ins |= diff;
		}
		patch_ins (code, ins);
		return;
	}

	if (prim == 15 || ins == 0x4e800021 || ins == 0x4e800020 || ins == 0x4e800420) {
#ifdef __mono_ppc64__
		guint32 *seq = (guint32*)code;
		guint32 *branch_ins;

		/* the trampoline code will try to patch the blrl, blr, bcctr */
		if (ins == 0x4e800021 || ins == 0x4e800020 || ins == 0x4e800420) {
			branch_ins = seq;
			if (ppc_is_load_op (seq [-3]) || ppc_opcode (seq [-3]) == 31) /* ld || lwz || mr */
				code -= 32;
			else
				code -= 24;
		} else {
			if (ppc_is_load_op (seq [5])
#ifdef PPC_USES_FUNCTION_DESCRIPTOR
			    /* With function descs we need to do more careful
			       matches.  */
			    || ppc_opcode (seq [5]) == 31 /* ld || lwz || mr */
#endif
			   )
				branch_ins = seq + 8;
			else
				branch_ins = seq + 6;
		}

		seq = (guint32*)code;
		/* this is the lis/ori/sldi/oris/ori/(ld/ld|mr/nop)/mtlr/blrl sequence */
		g_assert (mono_ppc_is_direct_call_sequence (branch_ins));

		if (ppc_is_load_op (seq [5])) {
			g_assert (ppc_is_load_op (seq [6]));

			if (!is_fd) {
				guint8 *buf = (guint8*)&seq [5];
				ppc_mr (buf, PPC_CALL_REG, ppc_r12);
				ppc_nop (buf);
			}
		} else {
			if (is_fd)
				target = (const guchar*)mono_get_addr_from_ftnptr ((gpointer)target);
		}

		/* FIXME: make this thread safe */
#ifdef PPC_USES_FUNCTION_DESCRIPTOR
		/* FIXME: we're assuming we're using r12 here */
		ppc_load_ptr_sequence (code, ppc_r12, target);
#else
		ppc_load_ptr_sequence (code, PPC_CALL_REG, target);
#endif
		mono_arch_flush_icache ((guint8*)seq, 28);
#else
		guint32 *seq;
		/* the trampoline code will try to patch the blrl, blr, bcctr */
		if (ins == 0x4e800021 || ins == 0x4e800020 || ins == 0x4e800420) {
			code -= 12;
		}
		/* this is the lis/ori/mtlr/blrl sequence */
		seq = (guint32*)code;
		g_assert ((seq [0] >> 26) == 15);
		g_assert ((seq [1] >> 26) == 24);
		g_assert ((seq [2] >> 26) == 31);
		g_assert (seq [3] == 0x4e800021 || seq [3] == 0x4e800020 || seq [3] == 0x4e800420);
		/* FIXME: make this thread safe */
		ppc_lis (code, PPC_CALL_REG, (guint32)(target) >> 16);
		ppc_ori (code, PPC_CALL_REG, PPC_CALL_REG, (guint32)(target) & 0xffff);
		mono_arch_flush_icache (code - 8, 8);
#endif
	} else {
		g_assert_not_reached ();
	}
//	g_print ("patched with 0x%08x\n", ins);
}

void
ppc_patch (guchar *code, const guchar *target)
{
	ppc_patch_full (NULL, NULL, code, target, FALSE);
}

void
mono_ppc_patch (guchar *code, const guchar *target)
{
	ppc_patch (code, target);
}

static guint8*
emit_move_return_value (MonoCompile *cfg, MonoInst *ins, guint8 *code)
{
	switch (ins->opcode) {
	case OP_FCALL:
	case OP_FCALL_REG:
	case OP_FCALL_MEMBASE:
		if (ins->dreg != ppc_f1)
			ppc_fmr (code, ins->dreg, ppc_f1);
		break;
	}

	return code;
}

static guint8*
emit_reserve_param_area (MonoCompile *cfg, guint8 *code)
{
	long size = cfg->param_area;

	size += MONO_ARCH_FRAME_ALIGNMENT - 1;
	size &= -MONO_ARCH_FRAME_ALIGNMENT;

	if (!size)
		return code;

	ppc_ldptr (code, ppc_r0, 0, ppc_sp);
	if (ppc_is_imm16 (-size)) {
		ppc_stptr_update (code, ppc_r0, -size, ppc_sp);
	} else {
		ppc_load (code, ppc_r12, -size);
		ppc_stptr_update_indexed (code, ppc_r0, ppc_sp, ppc_r12);
	}

	return code;
}

static guint8*
emit_unreserve_param_area (MonoCompile *cfg, guint8 *code)
{
	long size = cfg->param_area;

	size += MONO_ARCH_FRAME_ALIGNMENT - 1;
	size &= -MONO_ARCH_FRAME_ALIGNMENT;

	if (!size)
		return code;

	ppc_ldptr (code, ppc_r0, 0, ppc_sp);
	if (ppc_is_imm16 (size)) {
		ppc_stptr_update (code, ppc_r0, size, ppc_sp);
	} else {
		ppc_load (code, ppc_r12, size);
		ppc_stptr_update_indexed (code, ppc_r0, ppc_sp, ppc_r12);
	}

	return code;
}

#define MASK_SHIFT_IMM(i)	((i) & MONO_PPC_32_64_CASE (0x1f, 0x3f))

#ifndef DISABLE_JIT
void
mono_arch_output_basic_block (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins, *next;
	MonoCallInst *call;
	guint8 *code = cfg->native_code + cfg->code_len;
	MonoInst *last_ins = NULL;
	int max_len, cpos;
	int L;

	/* we don't align basic blocks of loops on ppc */

	if (cfg->verbose_level > 2)
		g_print ("Basic block %d starting at offset 0x%x\n", bb->block_num, bb->native_offset);

	cpos = bb->max_offset;

	MONO_BB_FOR_EACH_INS (bb, ins) {
		const guint offset = code - cfg->native_code;
		set_code_cursor (cfg, code);
		max_len = ins_get_size (ins->opcode);
		code = realloc_code (cfg, max_len);
	//	if (ins->cil_code)
	//		g_print ("cil code\n");
		mono_debug_record_line_number (cfg, ins, offset);

		switch (normalize_opcode (ins->opcode)) {
		case OP_RELAXED_NOP:
		case OP_NOP:
		case OP_DUMMY_USE:
		case OP_DUMMY_ICONST:
		case OP_DUMMY_I8CONST:
		case OP_DUMMY_R8CONST:
		case OP_DUMMY_R4CONST:
		case OP_NOT_REACHED:
		case OP_NOT_NULL:
			break;
		case OP_IL_SEQ_POINT:
			mono_add_seq_point (cfg, bb, ins, code - cfg->native_code);
			break;
		case OP_SEQ_POINT: {
			int i;

			if (cfg->compile_aot)
				NOT_IMPLEMENTED;

			/* 
			 * Read from the single stepping trigger page. This will cause a
			 * SIGSEGV when single stepping is enabled.
			 * We do this _before_ the breakpoint, so single stepping after
			 * a breakpoint is hit will step to the next IL offset.
			 */
			if (ins->flags & MONO_INST_SINGLE_STEP_LOC) {
				ppc_load (code, ppc_r12, (gsize)ss_trigger_page);
				ppc_ldptr (code, ppc_r12, 0, ppc_r12);
			}

			mono_add_seq_point (cfg, bb, ins, code - cfg->native_code);

			/* 
			 * A placeholder for a possible breakpoint inserted by
			 * mono_arch_set_breakpoint ().
			 */
			for (i = 0; i < BREAKPOINT_SIZE / 4; ++i)
				ppc_nop (code);
			break;
		}
		case OP_BIGMUL:
			ppc_mullw (code, ppc_r0, ins->sreg1, ins->sreg2);
			ppc_mulhw (code, ppc_r3, ins->sreg1, ins->sreg2);
			ppc_mr (code, ppc_r4, ppc_r0);
			break;
		case OP_BIGMUL_UN:
			ppc_mullw (code, ppc_r0, ins->sreg1, ins->sreg2);
			ppc_mulhwu (code, ppc_r3, ins->sreg1, ins->sreg2);
			ppc_mr (code, ppc_r4, ppc_r0);
			break;
		case OP_MEMORY_BARRIER:
			ppc_sync (code);
			break;
		case OP_STOREI1_MEMBASE_REG:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_stb (code, ins->sreg1, ins->inst_offset, ins->inst_destbasereg);
			} else {
				if (ppc_is_imm32 (ins->inst_offset)) {
					ppc_addis (code, ppc_r11, ins->inst_destbasereg, ppc_ha(ins->inst_offset));
					ppc_stb (code, ins->sreg1, ins->inst_offset, ppc_r11);
				} else {
					ppc_load (code, ppc_r0, ins->inst_offset);
					ppc_stbx (code, ins->sreg1, ins->inst_destbasereg, ppc_r0);
				}
			}
			break;
		case OP_STOREI2_MEMBASE_REG:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_sth (code, ins->sreg1, ins->inst_offset, ins->inst_destbasereg);
			} else {
				if (ppc_is_imm32 (ins->inst_offset)) {
					ppc_addis (code, ppc_r11, ins->inst_destbasereg, ppc_ha(ins->inst_offset));
					ppc_sth (code, ins->sreg1, ins->inst_offset, ppc_r11);
				} else {
					ppc_load (code, ppc_r0, ins->inst_offset);
					ppc_sthx (code, ins->sreg1, ins->inst_destbasereg, ppc_r0);
				}
			}
			break;
		case OP_STORE_MEMBASE_REG:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_stptr (code, ins->sreg1, ins->inst_offset, ins->inst_destbasereg);
			} else {
				if (ppc_is_imm32 (ins->inst_offset)) {
					ppc_addis (code, ppc_r11, ins->inst_destbasereg, ppc_ha(ins->inst_offset));
					ppc_stptr (code, ins->sreg1, ins->inst_offset, ppc_r11);
				} else {
					ppc_load (code, ppc_r0, ins->inst_offset);
					ppc_stptr_indexed (code, ins->sreg1, ins->inst_destbasereg, ppc_r0);
				}
			}
			break;
#ifdef MONO_ARCH_ILP32
		case OP_STOREI8_MEMBASE_REG:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_str (code, ins->sreg1, ins->inst_offset, ins->inst_destbasereg);
			} else {
				ppc_load (code, ppc_r0, ins->inst_offset);
				ppc_str_indexed (code, ins->sreg1, ins->inst_destbasereg, ppc_r0);
			}
			break;
#endif
		case OP_STOREI1_MEMINDEX:
			ppc_stbx (code, ins->sreg1, ins->inst_destbasereg, ins->sreg2);
			break;
		case OP_STOREI2_MEMINDEX:
			ppc_sthx (code, ins->sreg1, ins->inst_destbasereg, ins->sreg2);
			break;
		case OP_STORE_MEMINDEX:
			ppc_stptr_indexed (code, ins->sreg1, ins->inst_destbasereg, ins->sreg2);
			break;
		case OP_LOADU4_MEM:
			g_assert_not_reached ();
			break;
		case OP_LOAD_MEMBASE:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_ldptr (code, ins->dreg, ins->inst_offset, ins->inst_basereg);
			} else {
				if (ppc_is_imm32 (ins->inst_offset) && (ins->dreg > 0)) {
					ppc_addis (code, ins->dreg, ins->inst_basereg, ppc_ha(ins->inst_offset));
					ppc_ldptr (code, ins->dreg, ins->inst_offset, ins->dreg);
				} else {
					ppc_load (code, ppc_r0, ins->inst_offset);
					ppc_ldptr_indexed (code, ins->dreg, ins->inst_basereg, ppc_r0);
				}
			}
			break;
		case OP_LOADI4_MEMBASE:
#ifdef __mono_ppc64__
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_lwa (code, ins->dreg, ins->inst_offset, ins->inst_basereg);
			} else {
				if (ppc_is_imm32 (ins->inst_offset) && (ins->dreg > 0)) {
					ppc_addis (code, ins->dreg, ins->inst_basereg, ppc_ha(ins->inst_offset));
					ppc_lwa (code, ins->dreg, ins->inst_offset, ins->dreg);
				} else {
					ppc_load (code, ppc_r0, ins->inst_offset);
					ppc_lwax (code, ins->dreg, ins->inst_basereg, ppc_r0);
				}
			}
			break;
#endif
		case OP_LOADU4_MEMBASE:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_lwz (code, ins->dreg, ins->inst_offset, ins->inst_basereg);
			} else {
				if (ppc_is_imm32 (ins->inst_offset) && (ins->dreg > 0)) {
					ppc_addis (code, ins->dreg, ins->inst_basereg, ppc_ha(ins->inst_offset));
					ppc_lwz (code, ins->dreg, ins->inst_offset, ins->dreg);
				} else {
					ppc_load (code, ppc_r0, ins->inst_offset);
					ppc_lwzx (code, ins->dreg, ins->inst_basereg, ppc_r0);
				}
			}
			break;
		case OP_LOADI1_MEMBASE:
		case OP_LOADU1_MEMBASE:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_lbz (code, ins->dreg, ins->inst_offset, ins->inst_basereg);
			} else {
				if (ppc_is_imm32 (ins->inst_offset) && (ins->dreg > 0)) {
					ppc_addis (code, ins->dreg, ins->inst_basereg, ppc_ha(ins->inst_offset));
					ppc_lbz (code, ins->dreg, ins->inst_offset, ins->dreg);
				} else {
					ppc_load (code, ppc_r0, ins->inst_offset);
					ppc_lbzx (code, ins->dreg, ins->inst_basereg, ppc_r0);
				}
			}
			if (ins->opcode == OP_LOADI1_MEMBASE)
				ppc_extsb (code, ins->dreg, ins->dreg);
			break;
		case OP_LOADU2_MEMBASE:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_lhz (code, ins->dreg, ins->inst_offset, ins->inst_basereg);
			} else {
				if (ppc_is_imm32 (ins->inst_offset) && (ins->dreg > 0)) {
					ppc_addis (code, ins->dreg, ins->inst_basereg, ppc_ha(ins->inst_offset));
					ppc_lhz (code, ins->dreg, ins->inst_offset, ins->dreg);
				} else {
					ppc_load (code, ppc_r0, ins->inst_offset);
					ppc_lhzx (code, ins->dreg, ins->inst_basereg, ppc_r0);
				}
			}
			break;
		case OP_LOADI2_MEMBASE:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_lha (code, ins->dreg, ins->inst_offset, ins->inst_basereg);
			} else {
				if (ppc_is_imm32 (ins->inst_offset) && (ins->dreg > 0)) {
					ppc_addis (code, ins->dreg, ins->inst_basereg, ppc_ha(ins->inst_offset));
					ppc_lha (code, ins->dreg, ins->inst_offset, ins->dreg);
				} else {
					ppc_load (code, ppc_r0, ins->inst_offset);
					ppc_lhax (code, ins->dreg, ins->inst_basereg, ppc_r0);
				}
			}
			break;
#ifdef MONO_ARCH_ILP32
		case OP_LOADI8_MEMBASE:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_ldr (code, ins->dreg, ins->inst_offset, ins->inst_basereg);
			} else {
				ppc_load (code, ppc_r0, ins->inst_offset);
				ppc_ldr_indexed (code, ins->dreg, ins->inst_basereg, ppc_r0);
			}
			break;
#endif
		case OP_LOAD_MEMINDEX:
			ppc_ldptr_indexed (code, ins->dreg, ins->inst_basereg, ins->sreg2);
			break;
		case OP_LOADI4_MEMINDEX:
#ifdef __mono_ppc64__
			ppc_lwax (code, ins->dreg, ins->inst_basereg, ins->sreg2);
			break;
#endif
		case OP_LOADU4_MEMINDEX:
			ppc_lwzx (code, ins->dreg, ins->inst_basereg, ins->sreg2);
			break;
		case OP_LOADU2_MEMINDEX:
			ppc_lhzx (code, ins->dreg, ins->inst_basereg, ins->sreg2);
			break;
		case OP_LOADI2_MEMINDEX:
			ppc_lhax (code, ins->dreg, ins->inst_basereg, ins->sreg2);
			break;
		case OP_LOADU1_MEMINDEX:
			ppc_lbzx (code, ins->dreg, ins->inst_basereg, ins->sreg2);
			break;
		case OP_LOADI1_MEMINDEX:
			ppc_lbzx (code, ins->dreg, ins->inst_basereg, ins->sreg2);
			ppc_extsb (code, ins->dreg, ins->dreg);
			break;
		case OP_ICONV_TO_I1:
		CASE_PPC64 (OP_LCONV_TO_I1)
			ppc_extsb (code, ins->dreg, ins->sreg1);
			break;
		case OP_ICONV_TO_I2:
		CASE_PPC64 (OP_LCONV_TO_I2)
			ppc_extsh (code, ins->dreg, ins->sreg1);
			break;
		case OP_ICONV_TO_U1:
		CASE_PPC64 (OP_LCONV_TO_U1)
			ppc_clrlwi (code, ins->dreg, ins->sreg1, 24);
			break;
		case OP_ICONV_TO_U2:
		CASE_PPC64 (OP_LCONV_TO_U2)
			ppc_clrlwi (code, ins->dreg, ins->sreg1, 16);
			break;
		case OP_COMPARE:
		case OP_ICOMPARE:
		CASE_PPC64 (OP_LCOMPARE)
			L = (sizeof (target_mgreg_t) == 4 || ins->opcode == OP_ICOMPARE) ? 0 : 1;
			next = ins->next;
			if (next && compare_opcode_is_unsigned (next->opcode))
				ppc_cmpl (code, 0, L, ins->sreg1, ins->sreg2);
			else
				ppc_cmp (code, 0, L, ins->sreg1, ins->sreg2);
			break;
		case OP_COMPARE_IMM:
		case OP_ICOMPARE_IMM:
		CASE_PPC64 (OP_LCOMPARE_IMM)
			L = (sizeof (target_mgreg_t) == 4 || ins->opcode == OP_ICOMPARE_IMM) ? 0 : 1;
			next = ins->next;
			if (next && compare_opcode_is_unsigned (next->opcode)) {
				if (ppc_is_uimm16 (ins->inst_imm)) {
					ppc_cmpli (code, 0, L, ins->sreg1, (ins->inst_imm & 0xffff));
				} else {
					g_assert_not_reached ();
				}
			} else {
				if (ppc_is_imm16 (ins->inst_imm)) {
					ppc_cmpi (code, 0, L, ins->sreg1, (ins->inst_imm & 0xffff));
				} else {
					g_assert_not_reached ();
				}
			}
			break;
		case OP_BREAK:
			/*
			 * gdb does not like encountering a trap in the debugged code. So 
			 * instead of emitting a trap, we emit a call a C function and place a 
			 * breakpoint there.
			 */
			//ppc_break (code);
			ppc_mr (code, ppc_r3, ins->sreg1);
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_JIT_ICALL_ID, GUINT_TO_POINTER (MONO_JIT_ICALL_mono_break));
			if ((FORCE_INDIR_CALL || cfg->method->dynamic) && !cfg->compile_aot) {
				ppc_load_func (code, PPC_CALL_REG, 0);
				ppc_mtlr (code, PPC_CALL_REG);
				ppc_blrl (code);
			} else {
				ppc_bl (code, 0);
			}
			break;
		case OP_ADDCC:
		case OP_IADDCC:
			ppc_addco (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_IADD:
		CASE_PPC64 (OP_LADD)
			ppc_add (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_ADC:
		case OP_IADC:
			ppc_adde (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_ADDCC_IMM:
			if (ppc_is_imm16 (ins->inst_imm)) {
				ppc_addic (code, ins->dreg, ins->sreg1, ins->inst_imm);
			} else {
				g_assert_not_reached ();
			}
			break;
		case OP_ADD_IMM:
		case OP_IADD_IMM:
		CASE_PPC64 (OP_LADD_IMM)
			if (ppc_is_imm16 (ins->inst_imm)) {
				ppc_addi (code, ins->dreg, ins->sreg1, ins->inst_imm);
			} else {
				g_assert_not_reached ();
			}
			break;
		case OP_IADD_OVF:
			/* check XER [0-3] (SO, OV, CA): we can't use mcrxr
			 */
			ppc_addo (code, ins->dreg, ins->sreg1, ins->sreg2);
			ppc_mfspr (code, ppc_r0, ppc_xer);
			ppc_andisd (code, ppc_r0, ppc_r0, (1<<14));
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, "OverflowException");
			break;
		case OP_IADD_OVF_UN:
			/* check XER [0-3] (SO, OV, CA): we can't use mcrxr
			 */
			ppc_addco (code, ins->dreg, ins->sreg1, ins->sreg2);
			ppc_mfspr (code, ppc_r0, ppc_xer);
			ppc_andisd (code, ppc_r0, ppc_r0, (1<<13));
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, "OverflowException");
			break;
		case OP_ISUB_OVF:
		CASE_PPC64 (OP_LSUB_OVF)
			/* check XER [0-3] (SO, OV, CA): we can't use mcrxr
			 */
			ppc_subfo (code, ins->dreg, ins->sreg2, ins->sreg1);
			ppc_mfspr (code, ppc_r0, ppc_xer);
			ppc_andisd (code, ppc_r0, ppc_r0, (1<<14));
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, "OverflowException");
			break;
		case OP_ISUB_OVF_UN:
		CASE_PPC64 (OP_LSUB_OVF_UN)
			/* check XER [0-3] (SO, OV, CA): we can't use mcrxr
			 */
			ppc_subfc (code, ins->dreg, ins->sreg2, ins->sreg1);
			ppc_mfspr (code, ppc_r0, ppc_xer);
			ppc_andisd (code, ppc_r0, ppc_r0, (1<<13));
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_TRUE, PPC_BR_EQ, "OverflowException");
			break;
		case OP_ADD_OVF_CARRY:
			/* check XER [0-3] (SO, OV, CA): we can't use mcrxr
			 */
			ppc_addeo (code, ins->dreg, ins->sreg1, ins->sreg2);
			ppc_mfspr (code, ppc_r0, ppc_xer);
			ppc_andisd (code, ppc_r0, ppc_r0, (1<<14));
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, "OverflowException");
			break;
		case OP_ADD_OVF_UN_CARRY:
			/* check XER [0-3] (SO, OV, CA): we can't use mcrxr
			 */
			ppc_addeo (code, ins->dreg, ins->sreg1, ins->sreg2);
			ppc_mfspr (code, ppc_r0, ppc_xer);
			ppc_andisd (code, ppc_r0, ppc_r0, (1<<13));
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, "OverflowException");
			break;
		case OP_SUB_OVF_CARRY:
			/* check XER [0-3] (SO, OV, CA): we can't use mcrxr
			 */
			ppc_subfeo (code, ins->dreg, ins->sreg2, ins->sreg1);
			ppc_mfspr (code, ppc_r0, ppc_xer);
			ppc_andisd (code, ppc_r0, ppc_r0, (1<<14));
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, "OverflowException");
			break;
		case OP_SUB_OVF_UN_CARRY:
			/* check XER [0-3] (SO, OV, CA): we can't use mcrxr
			 */
			ppc_subfeo (code, ins->dreg, ins->sreg2, ins->sreg1);
			ppc_mfspr (code, ppc_r0, ppc_xer);
			ppc_andisd (code, ppc_r0, ppc_r0, (1<<13));
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_TRUE, PPC_BR_EQ, "OverflowException");
			break;
		case OP_SUBCC:
		case OP_ISUBCC:
			ppc_subfco (code, ins->dreg, ins->sreg2, ins->sreg1);
			break;
		case OP_ISUB:
		CASE_PPC64 (OP_LSUB)
			ppc_subf (code, ins->dreg, ins->sreg2, ins->sreg1);
			break;
		case OP_SBB:
		case OP_ISBB:
			ppc_subfe (code, ins->dreg, ins->sreg2, ins->sreg1);
			break;
		case OP_SUB_IMM:
		case OP_ISUB_IMM:
		CASE_PPC64 (OP_LSUB_IMM)
			// we add the negated value
			if (ppc_is_imm16 (-ins->inst_imm))
				ppc_addi (code, ins->dreg, ins->sreg1, -ins->inst_imm);
			else {
				g_assert_not_reached ();
			}
			break;
		case OP_PPC_SUBFIC:
			g_assert (ppc_is_imm16 (ins->inst_imm));
			ppc_subfic (code, ins->dreg, ins->sreg1, ins->inst_imm);
			break;
		case OP_PPC_SUBFZE:
			ppc_subfze (code, ins->dreg, ins->sreg1);
			break;
		case OP_IAND:
		CASE_PPC64 (OP_LAND)
			/* FIXME: the ppc macros as inconsistent here: put dest as the first arg! */
			ppc_and (code, ins->sreg1, ins->dreg, ins->sreg2);
			break;
		case OP_AND_IMM:
		case OP_IAND_IMM:
		CASE_PPC64 (OP_LAND_IMM)
			if (!(ins->inst_imm & 0xffff0000)) {
				ppc_andid (code, ins->sreg1, ins->dreg, ins->inst_imm);
			} else if (!(ins->inst_imm & 0xffff)) {
				ppc_andisd (code, ins->sreg1, ins->dreg, ((guint32)ins->inst_imm >> 16));
			} else {
				g_assert_not_reached ();
			}
			break;
		case OP_IDIV:
		CASE_PPC64 (OP_LDIV) {
			guint8 *divisor_is_m1;
                         /* XER format: SO, OV, CA, reserved [21 bits], count [8 bits]
                         */
			ppc_compare_reg_imm (code, 0, ins->sreg2, -1);
			divisor_is_m1 = code;
			ppc_bc (code, PPC_BR_FALSE | PPC_BR_LIKELY, PPC_BR_EQ, 0);
			ppc_lis (code, ppc_r0, 0x8000);
#ifdef __mono_ppc64__
			if (ins->opcode == OP_LDIV)
				ppc_sldi (code, ppc_r0, ppc_r0, 32);
#endif
			ppc_compare (code, 0, ins->sreg1, ppc_r0);
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_TRUE, PPC_BR_EQ, "OverflowException");
			ppc_patch (divisor_is_m1, code);
			 /* XER format: SO, OV, CA, reserved [21 bits], count [8 bits]
			 */
			if (ins->opcode == OP_IDIV)
				ppc_divwod (code, ins->dreg, ins->sreg1, ins->sreg2);
#ifdef __mono_ppc64__
			else
				ppc_divdod (code, ins->dreg, ins->sreg1, ins->sreg2);
#endif
			ppc_mfspr (code, ppc_r0, ppc_xer);
			ppc_andisd (code, ppc_r0, ppc_r0, (1<<14));
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, "DivideByZeroException");
			break;
		}
		case OP_IDIV_UN:
		CASE_PPC64 (OP_LDIV_UN)
			if (ins->opcode == OP_IDIV_UN)
				ppc_divwuod (code, ins->dreg, ins->sreg1, ins->sreg2);
#ifdef __mono_ppc64__
			else
				ppc_divduod (code, ins->dreg, ins->sreg1, ins->sreg2);
#endif
			ppc_mfspr (code, ppc_r0, ppc_xer);
			ppc_andisd (code, ppc_r0, ppc_r0, (1<<14));
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, "DivideByZeroException");
			break;
		case OP_DIV_IMM:
		case OP_IREM:
		case OP_IREM_UN:
		case OP_REM_IMM:
			g_assert_not_reached ();
		case OP_IOR:
		CASE_PPC64 (OP_LOR)
			ppc_or (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_OR_IMM:
		case OP_IOR_IMM:
		CASE_PPC64 (OP_LOR_IMM)
			if (!(ins->inst_imm & 0xffff0000)) {
				ppc_ori (code, ins->sreg1, ins->dreg, ins->inst_imm);
			} else if (!(ins->inst_imm & 0xffff)) {
				ppc_oris (code, ins->dreg, ins->sreg1, ((guint32)(ins->inst_imm) >> 16));
			} else {
				g_assert_not_reached ();
			}
			break;
		case OP_IXOR:
		CASE_PPC64 (OP_LXOR)
			ppc_xor (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_IXOR_IMM:
		case OP_XOR_IMM:
		CASE_PPC64 (OP_LXOR_IMM)
			if (!(ins->inst_imm & 0xffff0000)) {
				ppc_xori (code, ins->sreg1, ins->dreg, ins->inst_imm);
			} else if (!(ins->inst_imm & 0xffff)) {
				ppc_xoris (code, ins->sreg1, ins->dreg, ((guint32)(ins->inst_imm) >> 16));
			} else {
				g_assert_not_reached ();
			}
			break;
		case OP_ISHL:
		CASE_PPC64 (OP_LSHL)
			ppc_shift_left (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_SHL_IMM:
		case OP_ISHL_IMM:
		CASE_PPC64 (OP_LSHL_IMM)
			ppc_shift_left_imm (code, ins->dreg, ins->sreg1, MASK_SHIFT_IMM (ins->inst_imm));
			break;
		case OP_ISHR:
			ppc_sraw (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_SHR_IMM:
			ppc_shift_right_arith_imm (code, ins->dreg, ins->sreg1, MASK_SHIFT_IMM (ins->inst_imm));
			break;
		case OP_SHR_UN_IMM:
			if (MASK_SHIFT_IMM (ins->inst_imm))
				ppc_shift_right_imm (code, ins->dreg, ins->sreg1, MASK_SHIFT_IMM (ins->inst_imm));
			else
				ppc_mr (code, ins->dreg, ins->sreg1);
			break;
		case OP_ISHR_UN:
			ppc_srw (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_INOT:
		CASE_PPC64 (OP_LNOT)
			ppc_not (code, ins->dreg, ins->sreg1);
			break;
		case OP_INEG:
		CASE_PPC64 (OP_LNEG)
			ppc_neg (code, ins->dreg, ins->sreg1);
			break;
		case OP_IMUL:
		CASE_PPC64 (OP_LMUL)
			ppc_multiply (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_IMUL_IMM:
		case OP_MUL_IMM:
		CASE_PPC64 (OP_LMUL_IMM)
			if (ppc_is_imm16 (ins->inst_imm)) {
			    ppc_mulli (code, ins->dreg, ins->sreg1, ins->inst_imm);
			} else {
			    g_assert_not_reached ();
			}
			break;
		case OP_IMUL_OVF:
		CASE_PPC64 (OP_LMUL_OVF)
			/* we annot use mcrxr, since it's not implemented on some processors 
			 * XER format: SO, OV, CA, reserved [21 bits], count [8 bits]
			 */
			if (ins->opcode == OP_IMUL_OVF)
				ppc_mullwo (code, ins->dreg, ins->sreg1, ins->sreg2);
#ifdef __mono_ppc64__
			else
				ppc_mulldo (code, ins->dreg, ins->sreg1, ins->sreg2);
#endif
			ppc_mfspr (code, ppc_r0, ppc_xer);
			ppc_andisd (code, ppc_r0, ppc_r0, (1<<14));
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, "OverflowException");
			break;
		case OP_IMUL_OVF_UN:
		CASE_PPC64 (OP_LMUL_OVF_UN)
			/* we first multiply to get the high word and compare to 0
			 * to set the flags, then the result is discarded and then 
			 * we multiply to get the lower * bits result
			 */
			if (ins->opcode == OP_IMUL_OVF_UN)
				ppc_mulhwu (code, ppc_r0, ins->sreg1, ins->sreg2);
#ifdef __mono_ppc64__
			else
				ppc_mulhdu (code, ppc_r0, ins->sreg1, ins->sreg2);
#endif
			ppc_cmpi (code, 0, 0, ppc_r0, 0);
			EMIT_COND_SYSTEM_EXCEPTION (CEE_BNE_UN - CEE_BEQ, "OverflowException");
			ppc_multiply (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_ICONST:
			ppc_load (code, ins->dreg, ins->inst_c0);
			break;
		case OP_I8CONST: {
			ppc_load (code, ins->dreg, ins->inst_l);
			break;
		}
		case OP_LOAD_GOTADDR:
			/* The PLT implementation depends on this */
			g_assert (ins->dreg == ppc_r30);

			code = mono_arch_emit_load_got_addr (cfg->native_code, code, cfg, NULL);
			break;
		case OP_GOT_ENTRY:
			// FIXME: Fix max instruction length
			/* XXX: This is hairy; we're casting a pointer from a union to an enum... */
			mono_add_patch_info (cfg, offset, (MonoJumpInfoType)(intptr_t)ins->inst_right->inst_i1, ins->inst_right->inst_p0);
			/* arch_emit_got_access () patches this */
			ppc_load32 (code, ppc_r0, 0);
			ppc_ldptr_indexed (code, ins->dreg, ins->inst_basereg, ppc_r0);
			break;
		case OP_AOTCONST:
			mono_add_patch_info (cfg, offset, (MonoJumpInfoType)(intptr_t)ins->inst_i1, ins->inst_p0);
			ppc_load_sequence (code, ins->dreg, 0);
			break;
		CASE_PPC32 (OP_ICONV_TO_I4)
		CASE_PPC32 (OP_ICONV_TO_U4)
		case OP_MOVE:
			if (ins->dreg != ins->sreg1)
				ppc_mr (code, ins->dreg, ins->sreg1);
			break;
		case OP_SETLRET: {
			int saved = ins->sreg1;
			if (ins->sreg1 == ppc_r3) {
				ppc_mr (code, ppc_r0, ins->sreg1);
				saved = ppc_r0;
			}
			if (ins->sreg2 != ppc_r3)
				ppc_mr (code, ppc_r3, ins->sreg2);
			if (saved != ppc_r4)
				ppc_mr (code, ppc_r4, saved);
			break;
		}
		case OP_FMOVE:
			if (ins->dreg != ins->sreg1)
				ppc_fmr (code, ins->dreg, ins->sreg1);
			break;
		case OP_MOVE_F_TO_I4:
			ppc_stfs (code, ins->sreg1, -4, ppc_r1);
			ppc_ldptr (code, ins->dreg, -4, ppc_r1);
			break;
		case OP_MOVE_I4_TO_F:
			ppc_stw (code, ins->sreg1, -4, ppc_r1);
			ppc_lfs (code, ins->dreg, -4, ppc_r1);
			break;
#ifdef __mono_ppc64__
		case OP_MOVE_F_TO_I8:
			ppc_stfd (code, ins->sreg1, -8, ppc_r1);
			ppc_ldptr (code, ins->dreg, -8, ppc_r1);
			break;
		case OP_MOVE_I8_TO_F:
			ppc_stptr (code, ins->sreg1, -8, ppc_r1);
			ppc_lfd (code, ins->dreg, -8, ppc_r1);
			break;
#endif
		case OP_FCONV_TO_R4:
			ppc_frsp (code, ins->dreg, ins->sreg1);
			break;

		case OP_TAILCALL_PARAMETER:
			// This opcode helps compute sizes, i.e.
			// of the subsequent OP_TAILCALL, but contributes no code.
			g_assert (ins->next);
			break;

		case OP_TAILCALL: {
			int i, pos;
			MonoCallInst *call = (MonoCallInst*)ins;

			/*
			 * Keep in sync with mono_arch_emit_epilog
			 */
			g_assert (!cfg->method->save_lmf);
			/*
			 * Note: we can use ppc_r12 here because it is dead anyway:
			 * we're leaving the method.
			 */
			if (1 || cfg->flags & MONO_CFG_HAS_CALLS) {
				long ret_offset = cfg->stack_usage + PPC_RET_ADDR_OFFSET;
				if (ppc_is_imm16 (ret_offset)) {
					ppc_ldptr (code, ppc_r0, ret_offset, cfg->frame_reg);
				} else {
					ppc_load (code, ppc_r12, ret_offset);
					ppc_ldptr_indexed (code, ppc_r0, cfg->frame_reg, ppc_r12);
				}
				ppc_mtlr (code, ppc_r0);
			}

			if (ppc_is_imm16 (cfg->stack_usage)) {
				ppc_addi (code, ppc_r12, cfg->frame_reg, cfg->stack_usage);
			} else {
				/* cfg->stack_usage is an int, so we can use
				 * an addis/addi sequence here even in 64-bit.  */
				ppc_addis (code, ppc_r12, cfg->frame_reg, ppc_ha(cfg->stack_usage));
				ppc_addi (code, ppc_r12, ppc_r12, cfg->stack_usage);
			}
			if (!cfg->method->save_lmf) {
				pos = 0;
				for (i = 31; i >= 13; --i) {
					if (cfg->used_int_regs & (1 << i)) {
						pos += sizeof (target_mgreg_t);
						ppc_ldptr (code, i, -pos, ppc_r12);
					}
				}
			} else {
				/* FIXME restore from MonoLMF: though this can't happen yet */
			}

			/* Copy arguments on the stack to our argument area */
			if (call->stack_usage) {
				code = emit_memcpy (code, call->stack_usage, ppc_r12, PPC_STACK_PARAM_OFFSET, ppc_sp, PPC_STACK_PARAM_OFFSET);
				/* r12 was clobbered */
				g_assert (cfg->frame_reg == ppc_sp);
				if (ppc_is_imm16 (cfg->stack_usage)) {
					ppc_addi (code, ppc_r12, cfg->frame_reg, cfg->stack_usage);
				} else {
					/* cfg->stack_usage is an int, so we can use
					 * an addis/addi sequence here even in 64-bit.  */
					ppc_addis (code, ppc_r12, cfg->frame_reg, ppc_ha(cfg->stack_usage));
					ppc_addi (code, ppc_r12, ppc_r12, cfg->stack_usage);
				}
			}

			ppc_mr (code, ppc_sp, ppc_r12);
			mono_add_patch_info (cfg, (guint8*) code - cfg->native_code, MONO_PATCH_INFO_METHOD_JUMP, call->method);
			cfg->thunk_area += THUNK_SIZE;
			if (cfg->compile_aot) {
				/* arch_emit_got_access () patches this */
				ppc_load32 (code, ppc_r0, 0);
#ifdef PPC_USES_FUNCTION_DESCRIPTOR
				ppc_ldptr_indexed (code, ppc_r12, ppc_r30, ppc_r0);
				ppc_ldptr (code, ppc_r0, 0, ppc_r12);
#else
				ppc_ldptr_indexed (code, ppc_r0, ppc_r30, ppc_r0);
#endif
				ppc_mtctr (code, ppc_r0);
				ppc_bcctr (code, PPC_BR_ALWAYS, 0);
			} else {
				ppc_b (code, 0);
			}
			break;
		}
		case OP_CHECK_THIS:
			/* ensure ins->sreg1 is not NULL */
			ppc_ldptr (code, ppc_r0, 0, ins->sreg1);
			break;
		case OP_ARGLIST: {
			long cookie_offset = cfg->sig_cookie + cfg->stack_usage;
			if (ppc_is_imm16 (cookie_offset)) {
				ppc_addi (code, ppc_r0, cfg->frame_reg, cookie_offset);
			} else {
				ppc_load (code, ppc_r0, cookie_offset);
				ppc_add (code, ppc_r0, cfg->frame_reg, ppc_r0);
			}
			ppc_stptr (code, ppc_r0, 0, ins->sreg1);
			break;
		}
		case OP_FCALL:
		case OP_LCALL:
		case OP_VCALL:
		case OP_VCALL2:
		case OP_VOIDCALL:
		case OP_CALL:
			call = (MonoCallInst*)ins;
			mono_call_add_patch_info (cfg, call, offset);
			if ((FORCE_INDIR_CALL || cfg->method->dynamic) && !cfg->compile_aot) {
				ppc_load_func (code, PPC_CALL_REG, 0);
				ppc_mtlr (code, PPC_CALL_REG);
				ppc_blrl (code);
			} else {
				ppc_bl (code, 0);
			}
			/* FIXME: this should be handled somewhere else in the new jit */
			code = emit_move_return_value (cfg, ins, code);
			break;
		case OP_FCALL_REG:
		case OP_LCALL_REG:
		case OP_VCALL_REG:
		case OP_VCALL2_REG:
		case OP_VOIDCALL_REG:
		case OP_CALL_REG:
#ifdef PPC_USES_FUNCTION_DESCRIPTOR
			ppc_ldptr (code, ppc_r0, 0, ins->sreg1);
			/* FIXME: if we know that this is a method, we
			   can omit this load */
			ppc_ldptr (code, ppc_r2, 8, ins->sreg1);
			ppc_mtlr (code, ppc_r0);
#else
#if (_CALL_ELF == 2)
			if (ins->flags & MONO_INST_HAS_METHOD) {
			  // Not a global entry point
			} else {
				 // Need to set up r12 with function entry address for global entry point
				 if (ppc_r12 != ins->sreg1) {
					 ppc_mr(code,ppc_r12,ins->sreg1);
				 }
			}
#endif
			ppc_mtlr (code, ins->sreg1);
#endif
			ppc_blrl (code);
			/* FIXME: this should be handled somewhere else in the new jit */
			code = emit_move_return_value (cfg, ins, code);
			break;
		case OP_FCALL_MEMBASE:
		case OP_LCALL_MEMBASE:
		case OP_VCALL_MEMBASE:
		case OP_VCALL2_MEMBASE:
		case OP_VOIDCALL_MEMBASE:
		case OP_CALL_MEMBASE:
			if (cfg->compile_aot && ins->sreg1 == ppc_r12) {
				/* The trampolines clobber this */
				ppc_mr (code, ppc_r29, ins->sreg1);
				ppc_ldptr (code, ppc_r0, ins->inst_offset, ppc_r29);
			} else {
				ppc_ldptr (code, ppc_r0, ins->inst_offset, ins->sreg1);
			}
			ppc_mtlr (code, ppc_r0);
			ppc_blrl (code);
			/* FIXME: this should be handled somewhere else in the new jit */
			code = emit_move_return_value (cfg, ins, code);
			break;
		case OP_LOCALLOC: {
			guint8 * zero_loop_jump, * zero_loop_start;
			/* keep alignment */
			int alloca_waste = PPC_STACK_PARAM_OFFSET + cfg->param_area + 31;
			int area_offset = alloca_waste;
			area_offset &= ~31;
			ppc_addi (code, ppc_r12, ins->sreg1, alloca_waste + 31);
			/* FIXME: should be calculated from MONO_ARCH_FRAME_ALIGNMENT */
			ppc_clear_right_imm (code, ppc_r12, ppc_r12, 4);
			/* use ctr to store the number of words to 0 if needed */
			if (ins->flags & MONO_INST_INIT) {
				/* we zero 4 bytes at a time:
				 * we add 7 instead of 3 so that we set the counter to
				 * at least 1, otherwise the bdnz instruction will make
				 * it negative and iterate billions of times.
				 */
				ppc_addi (code, ppc_r0, ins->sreg1, 7);
				ppc_shift_right_arith_imm (code, ppc_r0, ppc_r0, 2);
				ppc_mtctr (code, ppc_r0);
			}
			ppc_ldptr (code, ppc_r0, 0, ppc_sp);
			ppc_neg (code, ppc_r12, ppc_r12);
			ppc_stptr_update_indexed (code, ppc_r0, ppc_sp, ppc_r12);

			/* FIXME: make this loop work in 8 byte
			   increments on PPC64 */
			if (ins->flags & MONO_INST_INIT) {
				/* adjust the dest reg by -4 so we can use stwu */
				/* we actually adjust -8 because we let the loop
				 * run at least once
				 */
				ppc_addi (code, ins->dreg, ppc_sp, (area_offset - 8));
				ppc_li (code, ppc_r12, 0);
				zero_loop_start = code;
				ppc_stwu (code, ppc_r12, 4, ins->dreg);
				zero_loop_jump = code;
				ppc_bc (code, PPC_BR_DEC_CTR_NONZERO, 0, 0);
				ppc_patch (zero_loop_jump, zero_loop_start);
			}
			ppc_addi (code, ins->dreg, ppc_sp, area_offset);
			break;
		}
		case OP_THROW: {
			//ppc_break (code);
			ppc_mr (code, ppc_r3, ins->sreg1);
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_JIT_ICALL_ID, GUINT_TO_POINTER (MONO_JIT_ICALL_mono_arch_throw_exception));
			if ((FORCE_INDIR_CALL || cfg->method->dynamic) && !cfg->compile_aot) {
				ppc_load_func (code, PPC_CALL_REG, 0);
				ppc_mtlr (code, PPC_CALL_REG);
				ppc_blrl (code);
			} else {
				ppc_bl (code, 0);
			}
			break;
		}
		case OP_RETHROW: {
			//ppc_break (code);
			ppc_mr (code, ppc_r3, ins->sreg1);
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_JIT_ICALL_ID,
					     GUINT_TO_POINTER (MONO_JIT_ICALL_mono_arch_rethrow_exception));
			if ((FORCE_INDIR_CALL || cfg->method->dynamic) && !cfg->compile_aot) {
				ppc_load_func (code, PPC_CALL_REG, 0);
				ppc_mtlr (code, PPC_CALL_REG);
				ppc_blrl (code);
			} else {
				ppc_bl (code, 0);
			}
			break;
		}
		case OP_START_HANDLER: {
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);
			g_assert (spvar->inst_basereg != ppc_sp);
			code = emit_reserve_param_area (cfg, code);
			ppc_mflr (code, ppc_r0);
			if (ppc_is_imm16 (spvar->inst_offset)) {
				ppc_stptr (code, ppc_r0, spvar->inst_offset, spvar->inst_basereg);
			} else {
				ppc_load (code, ppc_r12, spvar->inst_offset);
				ppc_stptr_indexed (code, ppc_r0, ppc_r12, spvar->inst_basereg);
			}
			break;
		}
		case OP_ENDFILTER: {
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);
			g_assert (spvar->inst_basereg != ppc_sp);
			code = emit_unreserve_param_area (cfg, code);
			if (ins->sreg1 != ppc_r3)
				ppc_mr (code, ppc_r3, ins->sreg1);
			if (ppc_is_imm16 (spvar->inst_offset)) {
				ppc_ldptr (code, ppc_r0, spvar->inst_offset, spvar->inst_basereg);
			} else {
				ppc_load (code, ppc_r12, spvar->inst_offset);
				ppc_ldptr_indexed (code, ppc_r0, spvar->inst_basereg, ppc_r12);
			}
			ppc_mtlr (code, ppc_r0);
			ppc_blr (code);
			break;
		}
		case OP_ENDFINALLY: {
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);
			g_assert (spvar->inst_basereg != ppc_sp);
			code = emit_unreserve_param_area (cfg, code);
			ppc_ldptr (code, ppc_r0, spvar->inst_offset, spvar->inst_basereg);
			ppc_mtlr (code, ppc_r0);
			ppc_blr (code);
			break;
		}
		case OP_CALL_HANDLER: 
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_BB, ins->inst_target_bb);
			ppc_bl (code, 0);
			for (GList *tmp = ins->inst_eh_blocks; tmp != bb->clause_holes; tmp = tmp->prev)
				mono_cfg_add_try_hole (cfg, ((MonoLeaveClause *) tmp->data)->clause, code, bb);
			break;
		case OP_LABEL:
			ins->inst_c0 = code - cfg->native_code;
			break;
		case OP_BR:
			/*if (ins->inst_target_bb->native_offset) {
				ppc_b (code, 0);
				//x86_jump_code (code, cfg->native_code + ins->inst_target_bb->native_offset); 
			} else*/ {
				mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_BB, ins->inst_target_bb);
				ppc_b (code, 0);
			}
			break;
		case OP_BR_REG:
			ppc_mtctr (code, ins->sreg1);
			ppc_bcctr (code, PPC_BR_ALWAYS, 0);
			break;
		case OP_ICNEQ:
			ppc_li (code, ins->dreg, 0);
			ppc_bc (code, PPC_BR_TRUE, PPC_BR_EQ, 2);
			ppc_li (code, ins->dreg, 1);
			break;
		case OP_CEQ:
		case OP_ICEQ:
		CASE_PPC64 (OP_LCEQ)
			ppc_li (code, ins->dreg, 0);
			ppc_bc (code, PPC_BR_FALSE, PPC_BR_EQ, 2);
			ppc_li (code, ins->dreg, 1);
			break;
		case OP_CLT:
		case OP_CLT_UN:
		case OP_ICLT:
		case OP_ICLT_UN:
		CASE_PPC64 (OP_LCLT)
		CASE_PPC64 (OP_LCLT_UN)
			ppc_li (code, ins->dreg, 1);
			ppc_bc (code, PPC_BR_TRUE, PPC_BR_LT, 2);
			ppc_li (code, ins->dreg, 0);
			break;
		case OP_ICGE:
		case OP_ICGE_UN:
			ppc_li (code, ins->dreg, 1);
			ppc_bc (code, PPC_BR_FALSE, PPC_BR_LT, 2);
			ppc_li (code, ins->dreg, 0);
			break;
		case OP_CGT:
		case OP_CGT_UN:
		case OP_ICGT:
		case OP_ICGT_UN:
		CASE_PPC64 (OP_LCGT)
		CASE_PPC64 (OP_LCGT_UN)
			ppc_li (code, ins->dreg, 1);
			ppc_bc (code, PPC_BR_TRUE, PPC_BR_GT, 2);
			ppc_li (code, ins->dreg, 0);
			break;
		case OP_ICLE:
		case OP_ICLE_UN:
			ppc_li (code, ins->dreg, 1);
			ppc_bc (code, PPC_BR_FALSE, PPC_BR_GT, 2);
			ppc_li (code, ins->dreg, 0);
			break;
		case OP_COND_EXC_EQ:
		case OP_COND_EXC_NE_UN:
		case OP_COND_EXC_LT:
		case OP_COND_EXC_LT_UN:
		case OP_COND_EXC_GT:
		case OP_COND_EXC_GT_UN:
		case OP_COND_EXC_GE:
		case OP_COND_EXC_GE_UN:
		case OP_COND_EXC_LE:
		case OP_COND_EXC_LE_UN:
			EMIT_COND_SYSTEM_EXCEPTION (ins->opcode - OP_COND_EXC_EQ, (const char*)ins->inst_p1);
			break;
		case OP_COND_EXC_IEQ:
		case OP_COND_EXC_INE_UN:
		case OP_COND_EXC_ILT:
		case OP_COND_EXC_ILT_UN:
		case OP_COND_EXC_IGT:
		case OP_COND_EXC_IGT_UN:
		case OP_COND_EXC_IGE:
		case OP_COND_EXC_IGE_UN:
		case OP_COND_EXC_ILE:
		case OP_COND_EXC_ILE_UN:
			EMIT_COND_SYSTEM_EXCEPTION (ins->opcode - OP_COND_EXC_IEQ, (const char*)ins->inst_p1);
			break;
		case OP_IBEQ:
		case OP_IBNE_UN:
		case OP_IBLT:
		case OP_IBLT_UN:
		case OP_IBGT:
		case OP_IBGT_UN:
		case OP_IBGE:
		case OP_IBGE_UN:
		case OP_IBLE:
		case OP_IBLE_UN:
			EMIT_COND_BRANCH (ins, ins->opcode - OP_IBEQ);
			break;

		/* floating point opcodes */
		case OP_R8CONST:
			g_assert (cfg->compile_aot);

			/* FIXME: Optimize this */
			ppc_bl (code, 1);
			ppc_mflr (code, ppc_r12);
			ppc_b (code, 3);
			*(double*)code = *(double*)ins->inst_p0;
			code += 8;
			ppc_lfd (code, ins->dreg, 8, ppc_r12);
			break;
		case OP_R4CONST:
			g_assert_not_reached ();
			break;
		case OP_STORER8_MEMBASE_REG:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_stfd (code, ins->sreg1, ins->inst_offset, ins->inst_destbasereg);
			} else {
				if (ppc_is_imm32 (ins->inst_offset)) {
					ppc_addis (code, ppc_r11, ins->inst_destbasereg, ppc_ha(ins->inst_offset));
					ppc_stfd (code, ins->sreg1, ins->inst_offset, ppc_r11);
				} else {
					ppc_load (code, ppc_r0, ins->inst_offset);
					ppc_stfdx (code, ins->sreg1, ins->inst_destbasereg, ppc_r0);
				}
			}
			break;
		case OP_LOADR8_MEMBASE:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_lfd (code, ins->dreg, ins->inst_offset, ins->inst_basereg);
			} else {
				if (ppc_is_imm32 (ins->inst_offset)) {
					ppc_addis (code, ppc_r11, ins->inst_destbasereg, ppc_ha(ins->inst_offset));
					ppc_lfd (code, ins->dreg, ins->inst_offset, ppc_r11);
				} else {
					ppc_load (code, ppc_r0, ins->inst_offset);
					ppc_lfdx (code, ins->dreg, ins->inst_destbasereg, ppc_r0);
				}
			}
			break;
		case OP_STORER4_MEMBASE_REG:
			ppc_frsp (code, ins->sreg1, ins->sreg1);
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_stfs (code, ins->sreg1, ins->inst_offset, ins->inst_destbasereg);
			} else {
				if (ppc_is_imm32 (ins->inst_offset)) {
					ppc_addis (code, ppc_r11, ins->inst_destbasereg, ppc_ha(ins->inst_offset));
					ppc_stfs (code, ins->sreg1, ins->inst_offset, ppc_r11);
				} else {
					ppc_load (code, ppc_r0, ins->inst_offset);
					ppc_stfsx (code, ins->sreg1, ins->inst_destbasereg, ppc_r0);
				}
			}
			break;
		case OP_LOADR4_MEMBASE:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_lfs (code, ins->dreg, ins->inst_offset, ins->inst_basereg);
			} else {
				if (ppc_is_imm32 (ins->inst_offset)) {
					ppc_addis (code, ppc_r11, ins->inst_destbasereg, ppc_ha(ins->inst_offset));
					ppc_lfs (code, ins->dreg, ins->inst_offset, ppc_r11);
				} else {
					ppc_load (code, ppc_r0, ins->inst_offset);
					ppc_lfsx (code, ins->dreg, ins->inst_destbasereg, ppc_r0);
				}
			}
			break;
		case OP_LOADR4_MEMINDEX:
			ppc_lfsx (code, ins->dreg, ins->inst_basereg, ins->sreg2);
			break;
		case OP_LOADR8_MEMINDEX:
			ppc_lfdx (code, ins->dreg, ins->inst_basereg, ins->sreg2);
			break;
		case OP_STORER4_MEMINDEX:
			ppc_frsp (code, ins->sreg1, ins->sreg1);
			ppc_stfsx (code, ins->sreg1, ins->inst_destbasereg, ins->sreg2);
			break;
		case OP_STORER8_MEMINDEX:
			ppc_stfdx (code, ins->sreg1, ins->inst_destbasereg, ins->sreg2);
			break;
		case CEE_CONV_R_UN:
		case CEE_CONV_R4: /* FIXME: change precision */
		case CEE_CONV_R8:
			g_assert_not_reached ();
		case OP_FCONV_TO_I1:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 1, TRUE);
			break;
		case OP_FCONV_TO_U1:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 1, FALSE);
			break;
		case OP_FCONV_TO_I2:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 2, TRUE);
			break;
		case OP_FCONV_TO_U2:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 2, FALSE);
			break;
		case OP_FCONV_TO_I4:
		case OP_FCONV_TO_I:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 4, TRUE);
			break;
		case OP_FCONV_TO_U4:
		case OP_FCONV_TO_U:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 4, FALSE);
			break;
		case OP_LCONV_TO_R_UN:
			g_assert_not_reached ();
			/* Implemented as helper calls */
			break;
		case OP_LCONV_TO_OVF_I4_2:
		case OP_LCONV_TO_OVF_I: {
#ifdef __mono_ppc64__
			NOT_IMPLEMENTED;
#else
			guint8 *negative_branch, *msword_positive_branch, *msword_negative_branch, *ovf_ex_target;
			// Check if its negative
			ppc_cmpi (code, 0, 0, ins->sreg1, 0);
			negative_branch = code;
			ppc_bc (code, PPC_BR_TRUE, PPC_BR_LT, 0);
			// Its positive msword == 0
			ppc_cmpi (code, 0, 0, ins->sreg2, 0);
			msword_positive_branch = code;
			ppc_bc (code, PPC_BR_TRUE, PPC_BR_EQ, 0);

			ovf_ex_target = code;
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_ALWAYS, 0, "OverflowException");
			// Negative
			ppc_patch (negative_branch, code);
			ppc_cmpi (code, 0, 0, ins->sreg2, -1);
			msword_negative_branch = code;
			ppc_bc (code, PPC_BR_FALSE, PPC_BR_EQ, 0);
			ppc_patch (msword_negative_branch, ovf_ex_target);
			
			ppc_patch (msword_positive_branch, code);
			if (ins->dreg != ins->sreg1)
				ppc_mr (code, ins->dreg, ins->sreg1);
			break;
#endif
		}
		case OP_ROUND:
			ppc_frind (code, ins->dreg, ins->sreg1);
			break;
		case OP_PPC_TRUNC:
			ppc_frizd (code, ins->dreg, ins->sreg1);
			break;
		case OP_PPC_CEIL:
			ppc_fripd (code, ins->dreg, ins->sreg1);
			break;
		case OP_PPC_FLOOR:
			ppc_frimd (code, ins->dreg, ins->sreg1);
			break;
		case OP_ABS:
			ppc_fabsd (code, ins->dreg, ins->sreg1);
			break;
		case OP_SQRTF:
			ppc_fsqrtsd (code, ins->dreg, ins->sreg1);
			break;
		case OP_SQRT:
			ppc_fsqrtd (code, ins->dreg, ins->sreg1);
			break;
		case OP_FADD:
			ppc_fadd (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_FSUB:
			ppc_fsub (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;		
		case OP_FMUL:
			ppc_fmul (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;		
		case OP_FDIV:
			ppc_fdiv (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;		
		case OP_FNEG:
			ppc_fneg (code, ins->dreg, ins->sreg1);
			break;		
		case OP_FREM:
			/* emulated */
			g_assert_not_reached ();
			break;
		/* These min/max require POWER5 */
		case OP_IMIN:
			ppc_cmp (code, 0, 0, ins->sreg1, ins->sreg2);
			ppc_isellt (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_IMIN_UN:
			ppc_cmpl (code, 0, 0, ins->sreg1, ins->sreg2);
			ppc_isellt (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_IMAX:
			ppc_cmp (code, 0, 0, ins->sreg1, ins->sreg2);
			ppc_iselgt (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_IMAX_UN:
			ppc_cmpl (code, 0, 0, ins->sreg1, ins->sreg2);
			ppc_iselgt (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		CASE_PPC64 (OP_LMIN)
			ppc_cmp (code, 0, 1, ins->sreg1, ins->sreg2);
			ppc_isellt (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		CASE_PPC64 (OP_LMIN_UN)
			ppc_cmpl (code, 0, 1, ins->sreg1, ins->sreg2);
			ppc_isellt (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		CASE_PPC64 (OP_LMAX)
			ppc_cmp (code, 0, 1, ins->sreg1, ins->sreg2);
			ppc_iselgt (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		CASE_PPC64 (OP_LMAX_UN)
			ppc_cmpl (code, 0, 1, ins->sreg1, ins->sreg2);
			ppc_iselgt (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_FCOMPARE:
			ppc_fcmpu (code, 0, ins->sreg1, ins->sreg2);
			break;
		case OP_FCEQ:
		case OP_FCNEQ:
			ppc_fcmpo (code, 0, ins->sreg1, ins->sreg2);
			ppc_li (code, ins->dreg, 1);
			ppc_bc (code, ins->opcode == OP_FCEQ ? PPC_BR_TRUE : PPC_BR_FALSE, PPC_BR_EQ, 2);
			ppc_li (code, ins->dreg, 0);
			break;
		case OP_FCLT:
		case OP_FCGE:
			ppc_fcmpo (code, 0, ins->sreg1, ins->sreg2);
			ppc_li (code, ins->dreg, 1);
			ppc_bc (code, ins->opcode == OP_FCLT ? PPC_BR_TRUE : PPC_BR_FALSE, PPC_BR_LT, 2);
			ppc_li (code, ins->dreg, 0);
			break;
		case OP_FCLT_UN:
			ppc_fcmpu (code, 0, ins->sreg1, ins->sreg2);
			ppc_li (code, ins->dreg, 1);
			ppc_bc (code, PPC_BR_TRUE, PPC_BR_SO, 3);
			ppc_bc (code, PPC_BR_TRUE, PPC_BR_LT, 2);
			ppc_li (code, ins->dreg, 0);
			break;
		case OP_FCGT:
		case OP_FCLE:
			ppc_fcmpo (code, 0, ins->sreg1, ins->sreg2);
			ppc_li (code, ins->dreg, 1);
			ppc_bc (code, ins->opcode == OP_FCGT ? PPC_BR_TRUE : PPC_BR_FALSE, PPC_BR_GT, 2);
			ppc_li (code, ins->dreg, 0);
			break;
		case OP_FCGT_UN:
			ppc_fcmpu (code, 0, ins->sreg1, ins->sreg2);
			ppc_li (code, ins->dreg, 1);
			ppc_bc (code, PPC_BR_TRUE, PPC_BR_SO, 3);
			ppc_bc (code, PPC_BR_TRUE, PPC_BR_GT, 2);
			ppc_li (code, ins->dreg, 0);
			break;
		case OP_FBEQ:
			EMIT_COND_BRANCH (ins, CEE_BEQ - CEE_BEQ);
			break;
		case OP_FBNE_UN:
			EMIT_COND_BRANCH (ins, CEE_BNE_UN - CEE_BEQ);
			break;
		case OP_FBLT:
			ppc_bc (code, PPC_BR_TRUE, PPC_BR_SO, 2);
			EMIT_COND_BRANCH (ins, CEE_BLT - CEE_BEQ);
			break;
		case OP_FBLT_UN:
			EMIT_COND_BRANCH_FLAGS (ins, PPC_BR_TRUE, PPC_BR_SO);
			EMIT_COND_BRANCH (ins, CEE_BLT_UN - CEE_BEQ);
			break;
		case OP_FBGT:
			ppc_bc (code, PPC_BR_TRUE, PPC_BR_SO, 2);
			EMIT_COND_BRANCH (ins, CEE_BGT - CEE_BEQ);
			break;
		case OP_FBGT_UN:
			EMIT_COND_BRANCH_FLAGS (ins, PPC_BR_TRUE, PPC_BR_SO);
			EMIT_COND_BRANCH (ins, CEE_BGT_UN - CEE_BEQ);
			break;
		case OP_FBGE:
			ppc_bc (code, PPC_BR_TRUE, PPC_BR_SO, 2);
			EMIT_COND_BRANCH (ins, CEE_BGE - CEE_BEQ);
			break;
		case OP_FBGE_UN:
			EMIT_COND_BRANCH (ins, CEE_BGE_UN - CEE_BEQ);
			break;
		case OP_FBLE:
			ppc_bc (code, PPC_BR_TRUE, PPC_BR_SO, 2);
			EMIT_COND_BRANCH (ins, CEE_BLE - CEE_BEQ);
			break;
		case OP_FBLE_UN:
			EMIT_COND_BRANCH (ins, CEE_BLE_UN - CEE_BEQ);
			break;
		case OP_CKFINITE:
			g_assert_not_reached ();
		case OP_PPC_CHECK_FINITE: {
			ppc_rlwinm (code, ins->sreg1, ins->sreg1, 0, 1, 31);
			ppc_addis (code, ins->sreg1, ins->sreg1, -32752);
			ppc_rlwinmd (code, ins->sreg1, ins->sreg1, 1, 31, 31);
			EMIT_COND_SYSTEM_EXCEPTION (CEE_BEQ - CEE_BEQ, "ArithmeticException");
			break;
		case OP_JUMP_TABLE:
			mono_add_patch_info (cfg, offset, (MonoJumpInfoType)ins->inst_c1, ins->inst_p0);
#ifdef __mono_ppc64__
			ppc_load_sequence (code, ins->dreg, (guint64)0x0f0f0f0f0f0f0f0fLL);
#else
			ppc_load_sequence (code, ins->dreg, (gulong)0x0f0f0f0fL);
#endif
			break;
		}

#ifdef __mono_ppc64__
		case OP_ICONV_TO_I4:
		case OP_SEXT_I4:
			ppc_extsw (code, ins->dreg, ins->sreg1);
			break;
		case OP_ICONV_TO_U4:
		case OP_ZEXT_I4:
			ppc_clrldi (code, ins->dreg, ins->sreg1, 32);
			break;
		case OP_ICONV_TO_R4:
		case OP_ICONV_TO_R8:
		case OP_LCONV_TO_R4:
		case OP_LCONV_TO_R8: {
			int tmp;
			if (ins->opcode == OP_ICONV_TO_R4 || ins->opcode == OP_ICONV_TO_R8) {
				ppc_extsw (code, ppc_r0, ins->sreg1);
				tmp = ppc_r0;
			} else {
				tmp = ins->sreg1;
			}
			if (cpu_hw_caps & PPC_MOVE_FPR_GPR) {
				ppc_mffgpr (code, ins->dreg, tmp);
			} else {
				ppc_str (code, tmp, -8, ppc_r1);
				ppc_lfd (code, ins->dreg, -8, ppc_r1);
			}
			ppc_fcfid (code, ins->dreg, ins->dreg);
			if (ins->opcode == OP_ICONV_TO_R4 || ins->opcode == OP_LCONV_TO_R4)
				ppc_frsp (code, ins->dreg, ins->dreg);
			break;
		}
		case OP_LSHR:
			ppc_srad (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_LSHR_UN:
			ppc_srd (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_COND_EXC_C:
			/* check XER [0-3] (SO, OV, CA): we can't use mcrxr
			 */
			ppc_mfspr (code, ppc_r0, ppc_xer);
			ppc_andisd (code, ppc_r0, ppc_r0, (1 << 13)); /* CA */
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, (const char*)ins->inst_p1);
			break;
		case OP_COND_EXC_OV:
			ppc_mfspr (code, ppc_r0, ppc_xer);
			ppc_andisd (code, ppc_r0, ppc_r0, (1 << 14)); /* OV */
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, (const char*)ins->inst_p1);
			break;
		case OP_LBEQ:
		case OP_LBNE_UN:
		case OP_LBLT:
		case OP_LBLT_UN:
		case OP_LBGT:
		case OP_LBGT_UN:
		case OP_LBGE:
		case OP_LBGE_UN:
		case OP_LBLE:
		case OP_LBLE_UN:
			EMIT_COND_BRANCH (ins, ins->opcode - OP_LBEQ);
			break;
		case OP_FCONV_TO_I8:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 8, TRUE);
			break;
		case OP_FCONV_TO_U8:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 8, FALSE);
			break;
		case OP_STOREI4_MEMBASE_REG:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_stw (code, ins->sreg1, ins->inst_offset, ins->inst_destbasereg);
			} else {
				ppc_load (code, ppc_r0, ins->inst_offset);
				ppc_stwx (code, ins->sreg1, ins->inst_destbasereg, ppc_r0);
			}
			break;
		case OP_STOREI4_MEMINDEX:
			ppc_stwx (code, ins->sreg1, ins->sreg2, ins->inst_destbasereg);
			break;
		case OP_ISHR_IMM:
			ppc_srawi (code, ins->dreg, ins->sreg1, (ins->inst_imm & 0x1f));
			break;
		case OP_ISHR_UN_IMM:
			if (ins->inst_imm & 0x1f)
				ppc_srwi (code, ins->dreg, ins->sreg1, (ins->inst_imm & 0x1f));
			else
				ppc_mr (code, ins->dreg, ins->sreg1);
			break;
#else
		case OP_ICONV_TO_R4:
		case OP_ICONV_TO_R8: {
			if (cpu_hw_caps & PPC_ISA_64) {
				ppc_srawi(code, ppc_r0, ins->sreg1, 31);
				ppc_stw (code, ppc_r0, -8, ppc_r1);
				ppc_stw (code, ins->sreg1, -4, ppc_r1);
				ppc_lfd (code, ins->dreg, -8, ppc_r1);
				ppc_fcfid (code, ins->dreg, ins->dreg);
				if (ins->opcode == OP_ICONV_TO_R4)
					ppc_frsp (code, ins->dreg, ins->dreg);
				}
			break;
		}
#endif

		case OP_ATOMIC_ADD_I4:
		CASE_PPC64 (OP_ATOMIC_ADD_I8) {
			int location = ins->inst_basereg;
			int addend = ins->sreg2;
			guint8 *loop, *branch;
			g_assert (ins->inst_offset == 0);

			loop = code;
			ppc_sync (code);
			if (ins->opcode == OP_ATOMIC_ADD_I4)
				ppc_lwarx (code, ppc_r0, 0, location);
#ifdef __mono_ppc64__
			else
				ppc_ldarx (code, ppc_r0, 0, location);
#endif

			ppc_add (code, ppc_r0, ppc_r0, addend);

			if (ins->opcode == OP_ATOMIC_ADD_I4)
				ppc_stwcxd (code, ppc_r0, 0, location);
#ifdef __mono_ppc64__
			else
				ppc_stdcxd (code, ppc_r0, 0, location);
#endif

			branch = code;
			ppc_bc (code, PPC_BR_FALSE, PPC_BR_EQ, 0);
			ppc_patch (branch, loop);

			ppc_sync (code);
			ppc_mr (code, ins->dreg, ppc_r0);
			break;
		}
		case OP_ATOMIC_CAS_I4:
		CASE_PPC64 (OP_ATOMIC_CAS_I8) {
			int location = ins->sreg1;
			int value = ins->sreg2;
			int comparand = ins->sreg3;
			guint8 *start, *not_equal, *lost_reservation;

			start = code;
			ppc_sync (code);
			if (ins->opcode == OP_ATOMIC_CAS_I4)
				ppc_lwarx (code, ppc_r0, 0, location);
#ifdef __mono_ppc64__
			else
				ppc_ldarx (code, ppc_r0, 0, location);
#endif

			ppc_cmp (code, 0, ins->opcode == OP_ATOMIC_CAS_I4 ? 0 : 1, ppc_r0, comparand);
			not_equal = code;
			ppc_bc (code, PPC_BR_FALSE, PPC_BR_EQ, 0);

			if (ins->opcode == OP_ATOMIC_CAS_I4)
				ppc_stwcxd (code, value, 0, location);
#ifdef __mono_ppc64__
			else
				ppc_stdcxd (code, value, 0, location);
#endif

			lost_reservation = code;
			ppc_bc (code, PPC_BR_FALSE, PPC_BR_EQ, 0);
			ppc_patch (lost_reservation, start);
			ppc_patch (not_equal, code);

			ppc_sync (code);
			ppc_mr (code, ins->dreg, ppc_r0);
			break;
		}
		case OP_LIVERANGE_START: {
			if (cfg->verbose_level > 1)
				printf ("R%d START=0x%x\n", MONO_VARINFO (cfg, ins->inst_c0)->vreg, (int)(code - cfg->native_code));
			MONO_VARINFO (cfg, ins->inst_c0)->live_range_start = code - cfg->native_code;
			break;
		}
		case OP_LIVERANGE_END: {
			if (cfg->verbose_level > 1)
				printf ("R%d END=0x%x\n", MONO_VARINFO (cfg, ins->inst_c0)->vreg, (int)(code - cfg->native_code));
			MONO_VARINFO (cfg, ins->inst_c0)->live_range_end = code - cfg->native_code;
			break;
		}
		case OP_GC_SAFE_POINT:
			break;

		default:
			g_warning ("unknown opcode %s in %s()\n", mono_inst_name (ins->opcode), __FUNCTION__);
			g_assert_not_reached ();
		}

		if ((cfg->opt & MONO_OPT_BRANCH) && ((code - cfg->native_code - offset) > max_len)) {
			g_warning ("wrong maximal instruction length of instruction %s (expected %d, got %ld)",
				   mono_inst_name (ins->opcode), max_len, (glong)(code - cfg->native_code - offset));
			g_assert_not_reached ();
		}
	       
		cpos += max_len;

		last_ins = ins;
	}

	set_code_cursor (cfg, code);
}
#endif /* !DISABLE_JIT */

void
mono_arch_register_lowlevel_calls (void)
{
	/* The signature doesn't matter */
	mono_register_jit_icall (mono_ppc_throw_exception, mono_icall_sig_void, TRUE);
}

#ifdef __mono_ppc64__
#if G_BYTE_ORDER == G_LITTLE_ENDIAN
#define patch_load_sequence(ip,val) do {\
		guint16 *__load = (guint16*)(ip);	\
		g_assert (sizeof (val) == sizeof (gsize)); \
		__load [0] = (((guint64)(gsize)(val)) >> 48) & 0xffff;	\
		__load [2] = (((guint64)(gsize)(val)) >> 32) & 0xffff;	\
		__load [6] = (((guint64)(gsize)(val)) >> 16) & 0xffff;	\
		__load [8] =  ((guint64)(gsize)(val))        & 0xffff;	\
	} while (0)
#elif G_BYTE_ORDER == G_BIG_ENDIAN
#define patch_load_sequence(ip,val) do {\
		guint16 *__load = (guint16*)(ip);	\
		g_assert (sizeof (val) == sizeof (gsize)); \
		__load [1] = (((guint64)(gsize)(val)) >> 48) & 0xffff;	\
		__load [3] = (((guint64)(gsize)(val)) >> 32) & 0xffff;	\
		__load [7] = (((guint64)(gsize)(val)) >> 16) & 0xffff;	\
		__load [9] =  ((guint64)(gsize)(val))        & 0xffff;	\
	} while (0)
#else
#error huh?  No endianess defined by compiler
#endif
#else
#define patch_load_sequence(ip,val) do {\
		guint16 *__lis_ori = (guint16*)(ip);	\
		__lis_ori [1] = (((gulong)(val)) >> 16) & 0xffff;	\
		__lis_ori [3] = ((gulong)(val)) & 0xffff;	\
	} while (0)
#endif

#ifndef DISABLE_JIT
void
mono_arch_patch_code_new (MonoCompile *cfg, guint8 *code, MonoJumpInfo *ji, gpointer target)
{
	unsigned char *ip = ji->ip.i + code;
	gboolean is_fd = FALSE;
	MonoDomain *domain = mono_get_root_domain ();

	switch (ji->type) {
	case MONO_PATCH_INFO_IP:
		patch_load_sequence (ip, ip);
		break;
	case MONO_PATCH_INFO_SWITCH: {
		gpointer *table = (gpointer *)ji->data.table->table;
		int i;

		patch_load_sequence (ip, table);

		for (i = 0; i < ji->data.table->table_size; i++) {
			table [i] = (glong)ji->data.table->table [i] + code;
		}
		/* we put into the table the absolute address, no need for ppc_patch in this case */
		break;
	}
	case MONO_PATCH_INFO_METHODCONST:
	case MONO_PATCH_INFO_CLASS:
	case MONO_PATCH_INFO_IMAGE:
	case MONO_PATCH_INFO_FIELD:
	case MONO_PATCH_INFO_VTABLE:
	case MONO_PATCH_INFO_IID:
	case MONO_PATCH_INFO_SFLDA:
	case MONO_PATCH_INFO_LDSTR:
	case MONO_PATCH_INFO_TYPE_FROM_HANDLE:
	case MONO_PATCH_INFO_LDTOKEN:
		/* from OP_AOTCONST : lis + ori */
		patch_load_sequence (ip, target);
		break;
	case MONO_PATCH_INFO_R4:
	case MONO_PATCH_INFO_R8:
		g_assert_not_reached ();
		*((gconstpointer *)(ip + 2)) = ji->data.target;
		break;
	case MONO_PATCH_INFO_EXC_NAME:
		g_assert_not_reached ();
		*((gconstpointer *)(ip + 1)) = ji->data.name;
		break;
	case MONO_PATCH_INFO_NONE:
	case MONO_PATCH_INFO_BB_OVF:
	case MONO_PATCH_INFO_EXC_OVF:
		/* everything is dealt with at epilog output time */
		break;
#ifdef PPC_USES_FUNCTION_DESCRIPTOR
	case MONO_PATCH_INFO_JIT_ICALL_ID:
	case MONO_PATCH_INFO_ABS:
	case MONO_PATCH_INFO_RGCTX_FETCH:
	case MONO_PATCH_INFO_JIT_ICALL_ADDR:
	case MONO_PATCH_INFO_SPECIFIC_TRAMPOLINE_LAZY_FETCH_ADDR:
		is_fd = TRUE;
		/* fall through */
#endif
	default:
		ppc_patch_full (cfg, domain, ip, (const guchar*)target, is_fd);
		break;
	}
}

/*
 * Emit code to save the registers in used_int_regs or the registers in the MonoLMF
 * structure at positive offset pos from register base_reg. pos is guaranteed to fit into
 * the instruction offset immediate for all the registers.
 */
static guint8*
save_registers (MonoCompile *cfg, guint8* code, int pos, int base_reg, gboolean save_lmf, guint32 used_int_regs, int cfa_offset)
{
	int i;
	if (!save_lmf) {
		for (i = 13; i <= 31; i++) {
			if (used_int_regs & (1 << i)) {
				ppc_str (code, i, pos, base_reg);
				mono_emit_unwind_op_offset (cfg, code, i, pos - cfa_offset);
				pos += sizeof (target_mgreg_t);
			}
		}
	} else {
		/* pos is the start of the MonoLMF structure */
		int offset = pos + G_STRUCT_OFFSET (MonoLMF, iregs);
		for (i = 13; i <= 31; i++) {
			ppc_str (code, i, offset, base_reg);
			mono_emit_unwind_op_offset (cfg, code, i, offset - cfa_offset);
			offset += sizeof (target_mgreg_t);
		}
		offset = pos + G_STRUCT_OFFSET (MonoLMF, fregs);
		for (i = 14; i < 32; i++) {
			ppc_stfd (code, i, offset, base_reg);
			offset += sizeof (gdouble);
		}
	}
	return code;
}

/*
 * Stack frame layout:
 * 
 *   ------------------- sp
 *   	MonoLMF structure or saved registers
 *   -------------------
 *   	spilled regs
 *   -------------------
 *   	locals
 *   -------------------
 *   	param area             size is cfg->param_area
 *   -------------------
 *   	linkage area           size is PPC_STACK_PARAM_OFFSET
 *   ------------------- sp
 *   	red zone
 */
guint8 *
mono_arch_emit_prolog (MonoCompile *cfg)
{
	MonoMethod *method = cfg->method;
	MonoBasicBlock *bb;
	MonoMethodSignature *sig;
	MonoInst *inst;
	long alloc_size, pos, max_offset, cfa_offset;
	int i;
	guint8 *code;
	CallInfo *cinfo;
	int lmf_offset = 0;
	int tailcall_struct_index;

	sig = mono_method_signature_internal (method);
	cfg->code_size = 512 + sig->param_count * 32;
	code = cfg->native_code = g_malloc (cfg->code_size);

	cfa_offset = 0;

	/* We currently emit unwind info for aot, but don't use it */
	mono_emit_unwind_op_def_cfa (cfg, code, ppc_r1, 0);

	if (1 || cfg->flags & MONO_CFG_HAS_CALLS) {
		ppc_mflr (code, ppc_r0);
		ppc_str (code, ppc_r0, PPC_RET_ADDR_OFFSET, ppc_sp);
		mono_emit_unwind_op_offset (cfg, code, ppc_lr, PPC_RET_ADDR_OFFSET);
	}

	alloc_size = cfg->stack_offset;
	pos = 0;

	if (!method->save_lmf) {
		for (i = 31; i >= 13; --i) {
			if (cfg->used_int_regs & (1 << i)) {
				pos += sizeof (target_mgreg_t);
			}
		}
	} else {
		pos += sizeof (MonoLMF);
		lmf_offset = pos;
	}
	alloc_size += pos;
	// align to MONO_ARCH_FRAME_ALIGNMENT bytes
	if (alloc_size & (MONO_ARCH_FRAME_ALIGNMENT - 1)) {
		alloc_size += MONO_ARCH_FRAME_ALIGNMENT - 1;
		alloc_size &= ~(MONO_ARCH_FRAME_ALIGNMENT - 1);
	}

	cfg->stack_usage = alloc_size;
	g_assert ((alloc_size & (MONO_ARCH_FRAME_ALIGNMENT-1)) == 0);
	if (alloc_size) {
		if (ppc_is_imm16 (-alloc_size)) {
			ppc_str_update (code, ppc_sp, -alloc_size, ppc_sp);
			cfa_offset = alloc_size;
			mono_emit_unwind_op_def_cfa_offset (cfg, code, alloc_size);
			code = save_registers (cfg, code, alloc_size - pos, ppc_sp, method->save_lmf, cfg->used_int_regs, cfa_offset);
		} else {
			if (pos)
				ppc_addi (code, ppc_r12, ppc_sp, -pos);
			ppc_load (code, ppc_r0, -alloc_size);
			ppc_str_update_indexed (code, ppc_sp, ppc_sp, ppc_r0);
			cfa_offset = alloc_size;
			mono_emit_unwind_op_def_cfa_offset (cfg, code, alloc_size);
			code = save_registers (cfg, code, 0, ppc_r12, method->save_lmf, cfg->used_int_regs, cfa_offset);
		}
	}
	if (cfg->frame_reg != ppc_sp) {
		ppc_mr (code, cfg->frame_reg, ppc_sp);
		mono_emit_unwind_op_def_cfa_reg (cfg, code, cfg->frame_reg);
	}

	/* store runtime generic context */
	if (cfg->rgctx_var) {
		g_assert (cfg->rgctx_var->opcode == OP_REGOFFSET &&
				(cfg->rgctx_var->inst_basereg == ppc_r1 || cfg->rgctx_var->inst_basereg == ppc_r31));

		ppc_stptr (code, MONO_ARCH_RGCTX_REG, cfg->rgctx_var->inst_offset, cfg->rgctx_var->inst_basereg);
	}

        /* compute max_offset in order to use short forward jumps
	 * we always do it on ppc because the immediate displacement
	 * for jumps is too small 
	 */
	max_offset = 0;
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoInst *ins;
		bb->max_offset = max_offset;

		MONO_BB_FOR_EACH_INS (bb, ins)
			max_offset += ins_get_size (ins->opcode);
	}

	/* load arguments allocated to register from the stack */
	pos = 0;

	cinfo = get_call_info (sig);

	if (MONO_TYPE_ISSTRUCT (sig->ret)) {
		ArgInfo *ainfo = &cinfo->ret;

		inst = cfg->vret_addr;
		g_assert (inst);

		if (ppc_is_imm16 (inst->inst_offset)) {
			ppc_stptr (code, ainfo->reg, inst->inst_offset, inst->inst_basereg);
		} else {
			ppc_load (code, ppc_r12, inst->inst_offset);
			ppc_stptr_indexed (code, ainfo->reg, ppc_r12, inst->inst_basereg);
		}
	}

	tailcall_struct_index = 0;
	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		ArgInfo *ainfo = cinfo->args + i;
		inst = cfg->args [pos];
		
		if (cfg->verbose_level > 2)
			g_print ("Saving argument %d (type: %d)\n", i, ainfo->regtype);
		if (inst->opcode == OP_REGVAR) {
			if (ainfo->regtype == RegTypeGeneral)
				ppc_mr (code, inst->dreg, ainfo->reg);
			else if (ainfo->regtype == RegTypeFP)
				ppc_fmr (code, inst->dreg, ainfo->reg);
			else if (ainfo->regtype == RegTypeBase) {
				ppc_ldr (code, ppc_r12, 0, ppc_sp);
				ppc_ldptr (code, inst->dreg, ainfo->offset, ppc_r12);
			} else
				g_assert_not_reached ();

			if (cfg->verbose_level > 2)
				g_print ("Argument %ld assigned to register %s\n", pos, mono_arch_regname (inst->dreg));
		} else {
			/* the argument should be put on the stack: FIXME handle size != word  */
			if (ainfo->regtype == RegTypeGeneral) {
				switch (ainfo->size) {
				case 1:
					if (ppc_is_imm16 (inst->inst_offset)) {
						ppc_stb (code, ainfo->reg, inst->inst_offset, inst->inst_basereg);
					} else {
						if (ppc_is_imm32 (inst->inst_offset)) {
							ppc_addis (code, ppc_r12, inst->inst_basereg, ppc_ha(inst->inst_offset));
							ppc_stb (code, ainfo->reg, inst->inst_offset, ppc_r12);
						} else {
							ppc_load (code, ppc_r12, inst->inst_offset);
							ppc_stbx (code, ainfo->reg, inst->inst_basereg, ppc_r12);
						}
					}
					break;
				case 2:
					if (ppc_is_imm16 (inst->inst_offset)) {
						ppc_sth (code, ainfo->reg, inst->inst_offset, inst->inst_basereg);
					} else {
						if (ppc_is_imm32 (inst->inst_offset)) {
							ppc_addis (code, ppc_r12, inst->inst_basereg, ppc_ha(inst->inst_offset));
							ppc_sth (code, ainfo->reg, inst->inst_offset, ppc_r12);
						} else {
							ppc_load (code, ppc_r12, inst->inst_offset);
							ppc_sthx (code, ainfo->reg, inst->inst_basereg, ppc_r12);
						}
					}
					break;
#ifdef __mono_ppc64__
				case 4:
					if (ppc_is_imm16 (inst->inst_offset)) {
						ppc_stw (code, ainfo->reg, inst->inst_offset, inst->inst_basereg);
					} else {
						if (ppc_is_imm32 (inst->inst_offset)) {
							ppc_addis (code, ppc_r12, inst->inst_basereg, ppc_ha(inst->inst_offset));
							ppc_stw (code, ainfo->reg, inst->inst_offset, ppc_r12);
						} else {
							ppc_load (code, ppc_r12, inst->inst_offset);
							ppc_stwx (code, ainfo->reg, inst->inst_basereg, ppc_r12);
						}
					}
					break;
				case 8:
					if (ppc_is_imm16 (inst->inst_offset)) {
						ppc_str (code, ainfo->reg, inst->inst_offset, inst->inst_basereg);
					} else {
						ppc_load (code, ppc_r12, inst->inst_offset);
						ppc_str_indexed (code, ainfo->reg, ppc_r12, inst->inst_basereg);
					}
					break;
#else
				case 8:
					if (ppc_is_imm16 (inst->inst_offset + 4)) {
						ppc_stw (code, ainfo->reg, inst->inst_offset, inst->inst_basereg);
						ppc_stw (code, ainfo->reg + 1, inst->inst_offset + 4, inst->inst_basereg);
					} else {
						ppc_addis (code, ppc_r12, inst->inst_basereg, ppc_ha(inst->inst_offset));
						ppc_addi (code, ppc_r12, ppc_r12, inst->inst_offset);
						ppc_stw (code, ainfo->reg, 0, ppc_r12);
						ppc_stw (code, ainfo->reg + 1, 4, ppc_r12);
					}
					break;
#endif
				default:
					if (ppc_is_imm16 (inst->inst_offset)) {
						ppc_stptr (code, ainfo->reg, inst->inst_offset, inst->inst_basereg);
					} else {
						if (ppc_is_imm32 (inst->inst_offset)) {
							ppc_addis (code, ppc_r12, inst->inst_basereg, ppc_ha(inst->inst_offset));
							ppc_stptr (code, ainfo->reg, inst->inst_offset, ppc_r12);
						} else {
							ppc_load (code, ppc_r12, inst->inst_offset);
							ppc_stptr_indexed (code, ainfo->reg, inst->inst_basereg, ppc_r12);
						}
					}
					break;
				}
			} else if (ainfo->regtype == RegTypeBase) {
				g_assert (ppc_is_imm16 (ainfo->offset));
				/* load the previous stack pointer in r12 */
				ppc_ldr (code, ppc_r12, 0, ppc_sp);
				ppc_ldptr (code, ppc_r0, ainfo->offset, ppc_r12);
				switch (ainfo->size) {
				case 1:
					if (ppc_is_imm16 (inst->inst_offset)) {
						ppc_stb (code, ppc_r0, inst->inst_offset, inst->inst_basereg);
					} else {
						if (ppc_is_imm32 (inst->inst_offset)) {
							ppc_addis (code, ppc_r12, inst->inst_basereg, ppc_ha(inst->inst_offset));
							ppc_stb (code, ppc_r0, inst->inst_offset, ppc_r12);
						} else {
							ppc_load (code, ppc_r12, inst->inst_offset);
							ppc_stbx (code, ppc_r0, inst->inst_basereg, ppc_r12);
						}
					}
					break;
				case 2:
					if (ppc_is_imm16 (inst->inst_offset)) {
						ppc_sth (code, ppc_r0, inst->inst_offset, inst->inst_basereg);
					} else {
						if (ppc_is_imm32 (inst->inst_offset)) {
							ppc_addis (code, ppc_r12, inst->inst_basereg, ppc_ha(inst->inst_offset));
							ppc_sth (code, ppc_r0, inst->inst_offset, ppc_r12);
						} else {
							ppc_load (code, ppc_r12, inst->inst_offset);
							ppc_sthx (code, ppc_r0, inst->inst_basereg, ppc_r12);
						}
					}
					break;
#ifdef __mono_ppc64__
				case 4:
					if (ppc_is_imm16 (inst->inst_offset)) {
						ppc_stw (code, ppc_r0, inst->inst_offset, inst->inst_basereg);
					} else {
						if (ppc_is_imm32 (inst->inst_offset)) {
							ppc_addis (code, ppc_r12, inst->inst_basereg, ppc_ha(inst->inst_offset));
							ppc_stw (code, ppc_r0, inst->inst_offset, ppc_r12);
						} else {
							ppc_load (code, ppc_r12, inst->inst_offset);
							ppc_stwx (code, ppc_r0, inst->inst_basereg, ppc_r12);
						}
					}
					break;
				case 8:
					if (ppc_is_imm16 (inst->inst_offset)) {
						ppc_str (code, ppc_r0, inst->inst_offset, inst->inst_basereg);
					} else {
						ppc_load (code, ppc_r12, inst->inst_offset);
						ppc_str_indexed (code, ppc_r0, ppc_r12, inst->inst_basereg);
					}
					break;
#else
				case 8:
					g_assert (ppc_is_imm16 (ainfo->offset + 4));
					if (ppc_is_imm16 (inst->inst_offset + 4)) {
						ppc_stw (code, ppc_r0, inst->inst_offset, inst->inst_basereg);
						ppc_lwz (code, ppc_r0, ainfo->offset + 4, ppc_r12);
						ppc_stw (code, ppc_r0, inst->inst_offset + 4, inst->inst_basereg);
					} else {
						/* use r11 to load the 2nd half of the long before we clobber r12.  */
						ppc_lwz (code, ppc_r11, ainfo->offset + 4, ppc_r12);
						ppc_addis (code, ppc_r12, inst->inst_basereg, ppc_ha(inst->inst_offset));
						ppc_addi (code, ppc_r12, ppc_r12, inst->inst_offset);
						ppc_stw (code, ppc_r0, 0, ppc_r12);
						ppc_stw (code, ppc_r11, 4, ppc_r12);
					}
					break;
#endif
				default:
					if (ppc_is_imm16 (inst->inst_offset)) {
						ppc_stptr (code, ppc_r0, inst->inst_offset, inst->inst_basereg);
					} else {
						if (ppc_is_imm32 (inst->inst_offset)) {
							ppc_addis (code, ppc_r12, inst->inst_basereg, ppc_ha(inst->inst_offset));
							ppc_stptr (code, ppc_r0, inst->inst_offset, ppc_r12);
						} else {
							ppc_load (code, ppc_r12, inst->inst_offset);
							ppc_stptr_indexed (code, ppc_r0, inst->inst_basereg, ppc_r12);
						}
					}
					break;
				}
			} else if (ainfo->regtype == RegTypeFP) {
				g_assert (ppc_is_imm16 (inst->inst_offset));
				if (ainfo->size == 8)
					ppc_stfd (code, ainfo->reg, inst->inst_offset, inst->inst_basereg);
				else if (ainfo->size == 4)
					ppc_stfs (code, ainfo->reg, inst->inst_offset, inst->inst_basereg);
				else
					g_assert_not_reached ();
			 } else if (ainfo->regtype == RegTypeFPStructByVal) {
				int doffset = inst->inst_offset;
				int soffset = 0;
				int cur_reg;
				int size = 0;
				g_assert (ppc_is_imm16 (inst->inst_offset));
				g_assert (ppc_is_imm16 (inst->inst_offset + ainfo->vtregs * sizeof (target_mgreg_t)));
				/* FIXME: what if there is no class? */
				if (sig->pinvoke && mono_class_from_mono_type_internal (inst->inst_vtype))
					size = mono_class_native_size (mono_class_from_mono_type_internal (inst->inst_vtype), NULL);
				for (cur_reg = 0; cur_reg < ainfo->vtregs; ++cur_reg) {
					if (ainfo->size == 4) {
						ppc_stfs (code, ainfo->reg + cur_reg, doffset, inst->inst_basereg);
					} else {
						ppc_stfd (code, ainfo->reg + cur_reg, doffset, inst->inst_basereg);
					}
					soffset += ainfo->size;
					doffset += ainfo->size;
				}
			} else if (ainfo->regtype == RegTypeStructByVal) {
				int doffset = inst->inst_offset;
				int soffset = 0;
				int cur_reg;
				int size = 0;
				g_assert (ppc_is_imm16 (inst->inst_offset));
				g_assert (ppc_is_imm16 (inst->inst_offset + ainfo->vtregs * sizeof (target_mgreg_t)));
				/* FIXME: what if there is no class? */
				if (sig->pinvoke && mono_class_from_mono_type_internal (inst->inst_vtype))
					size = mono_class_native_size (mono_class_from_mono_type_internal (inst->inst_vtype), NULL);
				for (cur_reg = 0; cur_reg < ainfo->vtregs; ++cur_reg) {
#if __APPLE__
					/*
					 * Darwin handles 1 and 2 byte
					 * structs specially by
					 * loading h/b into the arg
					 * register.  Only done for
					 * pinvokes.
					 */
					if (size == 2)
						ppc_sth (code, ainfo->reg + cur_reg, doffset, inst->inst_basereg);
					else if (size == 1)
						ppc_stb (code, ainfo->reg + cur_reg, doffset, inst->inst_basereg);
					else
#endif
					{
#ifdef __mono_ppc64__
						if (ainfo->bytes) {
							g_assert (cur_reg == 0);
#if G_BYTE_ORDER == G_BIG_ENDIAN
							ppc_sldi (code, ppc_r0, ainfo->reg,
						                         (sizeof (target_mgreg_t) - ainfo->bytes) * 8);
							ppc_stptr (code, ppc_r0, doffset, inst->inst_basereg);
#else
							if (mono_class_native_size (inst->klass, NULL) == 1) {
							  ppc_stb (code, ainfo->reg + cur_reg, doffset, inst->inst_basereg);
							} else if (mono_class_native_size (inst->klass, NULL) == 2) {
								ppc_sth (code, ainfo->reg + cur_reg, doffset, inst->inst_basereg);
							} else if (mono_class_native_size (inst->klass, NULL) == 4) {  // WDS -- maybe <=4?
								ppc_stw (code, ainfo->reg + cur_reg, doffset, inst->inst_basereg);
							} else {
								ppc_stptr (code, ainfo->reg + cur_reg, doffset, inst->inst_basereg);  // WDS -- Better way?
							}
#endif
						} else
#endif
						{
							ppc_stptr (code, ainfo->reg + cur_reg, doffset,
									inst->inst_basereg);
						}
					}
					soffset += sizeof (target_mgreg_t);
					doffset += sizeof (target_mgreg_t);
				}
				if (ainfo->vtsize) {
					/* FIXME: we need to do the shifting here, too */
					if (ainfo->bytes)
						NOT_IMPLEMENTED;
					/* load the previous stack pointer in r12 (r0 gets overwritten by the memcpy) */
					ppc_ldr (code, ppc_r12, 0, ppc_sp);
					if ((size & MONO_PPC_32_64_CASE (3, 7)) != 0) {
						code = emit_memcpy (code, size - soffset,
							inst->inst_basereg, doffset,
							ppc_r12, ainfo->offset + soffset);
					} else {
						code = emit_memcpy (code, ainfo->vtsize * sizeof (target_mgreg_t),
							inst->inst_basereg, doffset,
							ppc_r12, ainfo->offset + soffset);
					}
				}
			} else if (ainfo->regtype == RegTypeStructByAddr) {
				/* if it was originally a RegTypeBase */
				if (ainfo->offset) {
					/* load the previous stack pointer in r12 */
					ppc_ldr (code, ppc_r12, 0, ppc_sp);
					ppc_ldptr (code, ppc_r12, ainfo->offset, ppc_r12);
				} else {
					ppc_mr (code, ppc_r12, ainfo->reg);
				}

				g_assert (ppc_is_imm16 (inst->inst_offset));
				code = emit_memcpy (code, ainfo->vtsize, inst->inst_basereg, inst->inst_offset, ppc_r12, 0);
				/*g_print ("copy in %s: %d bytes from %d to offset: %d\n", method->name, ainfo->vtsize, ainfo->reg, inst->inst_offset);*/
			} else
				g_assert_not_reached ();
		}
		pos++;
	}

	if (method->save_lmf) {
		if (cfg->compile_aot) {
			/* Compute the got address which is needed by the PLT entry */
			code = mono_arch_emit_load_got_addr (cfg->native_code, code, cfg, NULL);
		}
		mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_JIT_ICALL_ID,
			     GUINT_TO_POINTER (MONO_JIT_ICALL_mono_tls_get_lmf_addr_extern));
		if ((FORCE_INDIR_CALL || cfg->method->dynamic) && !cfg->compile_aot) {
			ppc_load_func (code, PPC_CALL_REG, 0);
			ppc_mtlr (code, PPC_CALL_REG);
			ppc_blrl (code);
		} else {
			ppc_bl (code, 0);
		}
		/* we build the MonoLMF structure on the stack - see mini-ppc.h */
		/* lmf_offset is the offset from the previous stack pointer,
		 * alloc_size is the total stack space allocated, so the offset
		 * of MonoLMF from the current stack ptr is alloc_size - lmf_offset.
		 * The pointer to the struct is put in ppc_r12 (new_lmf).
		 * The callee-saved registers are already in the MonoLMF structure
		 */
		ppc_addi (code, ppc_r12, ppc_sp, alloc_size - lmf_offset);
		/* ppc_r3 is the result from mono_get_lmf_addr () */
		ppc_stptr (code, ppc_r3, G_STRUCT_OFFSET(MonoLMF, lmf_addr), ppc_r12);
		/* new_lmf->previous_lmf = *lmf_addr */
		ppc_ldptr (code, ppc_r0, G_STRUCT_OFFSET(MonoLMF, previous_lmf), ppc_r3);
		ppc_stptr (code, ppc_r0, G_STRUCT_OFFSET(MonoLMF, previous_lmf), ppc_r12);
		/* *(lmf_addr) = r12 */
		ppc_stptr (code, ppc_r12, G_STRUCT_OFFSET(MonoLMF, previous_lmf), ppc_r3);
		/* save method info */
		if (cfg->compile_aot)
			// FIXME:
			ppc_load (code, ppc_r0, 0);
		else
			ppc_load_ptr (code, ppc_r0, method);
		ppc_stptr (code, ppc_r0, G_STRUCT_OFFSET(MonoLMF, method), ppc_r12);
		ppc_stptr (code, ppc_sp, G_STRUCT_OFFSET(MonoLMF, ebp), ppc_r12);
		/* save the current IP */
		if (cfg->compile_aot) {
			ppc_bl (code, 1);
			ppc_mflr (code, ppc_r0);
		} else {
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_IP, NULL);
#ifdef __mono_ppc64__
			ppc_load_sequence (code, ppc_r0, (guint64)0x0101010101010101LL);
#else
			ppc_load_sequence (code, ppc_r0, (gulong)0x01010101L);
#endif
		}
		ppc_stptr (code, ppc_r0, G_STRUCT_OFFSET(MonoLMF, eip), ppc_r12);
	}

	set_code_cursor (cfg, code);
	g_free (cinfo);

	return code;
}

void
mono_arch_emit_epilog (MonoCompile *cfg)
{
	MonoMethod *method = cfg->method;
	int pos, i;
	int max_epilog_size = 16 + 20*4;
	guint8 *code;

	if (cfg->method->save_lmf)
		max_epilog_size += 128;
	
	code = realloc_code (cfg, max_epilog_size);

	pos = 0;

	if (method->save_lmf) {
		int lmf_offset;
		pos +=  sizeof (MonoLMF);
		lmf_offset = pos;
		/* save the frame reg in r8 */
		ppc_mr (code, ppc_r8, cfg->frame_reg);
		ppc_addi (code, ppc_r12, cfg->frame_reg, cfg->stack_usage - lmf_offset);
		/* r5 = previous_lmf */
		ppc_ldptr (code, ppc_r5, G_STRUCT_OFFSET(MonoLMF, previous_lmf), ppc_r12);
		/* r6 = lmf_addr */
		ppc_ldptr (code, ppc_r6, G_STRUCT_OFFSET(MonoLMF, lmf_addr), ppc_r12);
		/* *(lmf_addr) = previous_lmf */
		ppc_stptr (code, ppc_r5, G_STRUCT_OFFSET(MonoLMF, previous_lmf), ppc_r6);
		/* FIXME: speedup: there is no actual need to restore the registers if
		 * we didn't actually change them (idea from Zoltan).
		 */
		/* restore iregs */
		ppc_ldr_multiple (code, ppc_r13, G_STRUCT_OFFSET(MonoLMF, iregs), ppc_r12);
		/* restore fregs */
		/*for (i = 14; i < 32; i++) {
			ppc_lfd (code, i, G_STRUCT_OFFSET(MonoLMF, fregs) + ((i-14) * sizeof (gdouble)), ppc_r12);
		}*/
		g_assert (ppc_is_imm16 (cfg->stack_usage + PPC_RET_ADDR_OFFSET));
		/* use the saved copy of the frame reg in r8 */
		if (1 || cfg->flags & MONO_CFG_HAS_CALLS) {
			ppc_ldr (code, ppc_r0, cfg->stack_usage + PPC_RET_ADDR_OFFSET, ppc_r8);
			ppc_mtlr (code, ppc_r0);
		}
		ppc_addic (code, ppc_sp, ppc_r8, cfg->stack_usage);
	} else {
		if (1 || cfg->flags & MONO_CFG_HAS_CALLS) {
			long return_offset = cfg->stack_usage + PPC_RET_ADDR_OFFSET;
			if (ppc_is_imm16 (return_offset)) {
				ppc_ldr (code, ppc_r0, return_offset, cfg->frame_reg);
			} else {
				ppc_load (code, ppc_r12, return_offset);
				ppc_ldr_indexed (code, ppc_r0, cfg->frame_reg, ppc_r12);
			}
			ppc_mtlr (code, ppc_r0);
		}
		if (ppc_is_imm16 (cfg->stack_usage)) {
			int offset = cfg->stack_usage;
			for (i = 13; i <= 31; i++) {
				if (cfg->used_int_regs & (1 << i))
					offset -= sizeof (target_mgreg_t);
			}
			if (cfg->frame_reg != ppc_sp)
				ppc_mr (code, ppc_r12, cfg->frame_reg);
			/* note r31 (possibly the frame register) is restored last */
			for (i = 13; i <= 31; i++) {
				if (cfg->used_int_regs & (1 << i)) {
					ppc_ldr (code, i, offset, cfg->frame_reg);
					offset += sizeof (target_mgreg_t);
				}
			}
			if (cfg->frame_reg != ppc_sp)
				ppc_addi (code, ppc_sp, ppc_r12, cfg->stack_usage);
			else
				ppc_addi (code, ppc_sp, ppc_sp, cfg->stack_usage);
		} else {
			ppc_load32 (code, ppc_r12, cfg->stack_usage);
			if (cfg->used_int_regs) {
				ppc_add (code, ppc_r12, cfg->frame_reg, ppc_r12);
				for (i = 31; i >= 13; --i) {
					if (cfg->used_int_regs & (1 << i)) {
						pos += sizeof (target_mgreg_t);
						ppc_ldr (code, i, -pos, ppc_r12);
					}
				}
				ppc_mr (code, ppc_sp, ppc_r12);
			} else {
				ppc_add (code, ppc_sp, cfg->frame_reg, ppc_r12);
			}
		}
	}
	ppc_blr (code);

	set_code_cursor (cfg, code);

}
#endif /* ifndef DISABLE_JIT */

/* remove once throw_exception_by_name is eliminated */
static int
exception_id_by_name (const char *name)
{
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
	if (strcmp (name, "NullReferenceException") == 0)
		return MONO_EXC_NULL_REF;
	if (strcmp (name, "ArrayTypeMismatchException") == 0)
		return MONO_EXC_ARRAY_TYPE_MISMATCH;
	if (strcmp (name, "ArgumentException") == 0)
		return MONO_EXC_ARGUMENT;
	g_error ("Unknown intrinsic exception %s\n", name);
	return 0;
}

#ifndef DISABLE_JIT
void
mono_arch_emit_exceptions (MonoCompile *cfg)
{
	MonoJumpInfo *patch_info;
	int i;
	guint8 *code;
	guint8* exc_throw_pos [MONO_EXC_INTRINS_NUM];
	guint8 exc_throw_found [MONO_EXC_INTRINS_NUM];
	int max_epilog_size = 50;

	for (i = 0; i < MONO_EXC_INTRINS_NUM; i++) {
		exc_throw_pos [i] = NULL;
		exc_throw_found [i] = 0;
	}

	/* count the number of exception infos */
     
	/* 
	 * make sure we have enough space for exceptions
	 */
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		if (patch_info->type == MONO_PATCH_INFO_EXC) {
			i = exception_id_by_name ((const char*)patch_info->data.target);
			if (!exc_throw_found [i]) {
				max_epilog_size += (2 * PPC_LOAD_SEQUENCE_LENGTH) + 5 * 4;
				exc_throw_found [i] = TRUE;
			}
		} else if (patch_info->type == MONO_PATCH_INFO_BB_OVF)
			max_epilog_size += 12;
		else if (patch_info->type == MONO_PATCH_INFO_EXC_OVF) {
			MonoOvfJump *ovfj = (MonoOvfJump*)patch_info->data.target;
			i = exception_id_by_name (ovfj->data.exception);
			if (!exc_throw_found [i]) {
				max_epilog_size += (2 * PPC_LOAD_SEQUENCE_LENGTH) + 5 * 4;
				exc_throw_found [i] = TRUE;
			}
			max_epilog_size += 8;
		}
	}

	code = realloc_code (cfg, max_epilog_size);

	/* add code to raise exceptions */
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		switch (patch_info->type) {
		case MONO_PATCH_INFO_BB_OVF: {
			MonoOvfJump *ovfj = (MonoOvfJump*)patch_info->data.target;
			unsigned char *ip = patch_info->ip.i + cfg->native_code;
			/* patch the initial jump */
			ppc_patch (ip, code);
			ppc_bc (code, ovfj->b0_cond, ovfj->b1_cond, 2);
			ppc_b (code, 0);
			ppc_patch (code - 4, ip + 4); /* jump back after the initiali branch */
			/* jump back to the true target */
			ppc_b (code, 0);
			ip = ovfj->data.bb->native_offset + cfg->native_code;
			ppc_patch (code - 4, ip);
			patch_info->type = MONO_PATCH_INFO_NONE;
			break;
		}
		case MONO_PATCH_INFO_EXC_OVF: {
			MonoOvfJump *ovfj = (MonoOvfJump*)patch_info->data.target;
			MonoJumpInfo *newji;
			unsigned char *ip = patch_info->ip.i + cfg->native_code;
			unsigned char *bcl = code;
			/* patch the initial jump: we arrived here with a call */
			ppc_patch (ip, code);
			ppc_bc (code, ovfj->b0_cond, ovfj->b1_cond, 0);
			ppc_b (code, 0);
			ppc_patch (code - 4, ip + 4); /* jump back after the initiali branch */
			/* patch the conditional jump to the right handler */
			/* make it processed next */
			newji = mono_mempool_alloc (cfg->mempool, sizeof (MonoJumpInfo));
			newji->type = MONO_PATCH_INFO_EXC;
			newji->ip.i = bcl - cfg->native_code;
			newji->data.target = ovfj->data.exception;
			newji->next = patch_info->next;
			patch_info->next = newji;
			patch_info->type = MONO_PATCH_INFO_NONE;
			break;
		}
		case MONO_PATCH_INFO_EXC: {
			MonoClass *exc_class;

			unsigned char *ip = patch_info->ip.i + cfg->native_code;
			i = exception_id_by_name ((const char*)patch_info->data.target);
			if (exc_throw_pos [i] && !(ip > exc_throw_pos [i] && ip - exc_throw_pos [i] > 50000)) {
				ppc_patch (ip, exc_throw_pos [i]);
				patch_info->type = MONO_PATCH_INFO_NONE;
				break;
			} else {
				exc_throw_pos [i] = code;
			}

			exc_class = mono_class_load_from_name (mono_defaults.corlib, "System", patch_info->data.name);

			ppc_patch (ip, code);
			/*mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_EXC_NAME, patch_info->data.target);*/
			ppc_load (code, ppc_r3, m_class_get_type_token (exc_class));
			/* we got here from a conditional call, so the calling ip is set in lr */
			ppc_mflr (code, ppc_r4);
			patch_info->type = MONO_PATCH_INFO_JIT_ICALL_ID;
			patch_info->data.jit_icall_id = MONO_JIT_ICALL_mono_arch_throw_corlib_exception;
			patch_info->ip.i = code - cfg->native_code;
			if (FORCE_INDIR_CALL || cfg->method->dynamic) {
				ppc_load_func (code, PPC_CALL_REG, 0);
				ppc_mtctr (code, PPC_CALL_REG);
				ppc_bcctr (code, PPC_BR_ALWAYS, 0);
			} else {
				ppc_bl (code, 0);
			}
			break;
		}
		default:
			/* do nothing */
			break;
		}
	}

	set_code_cursor (cfg, code);
}
#endif

#if DEAD_CODE
static int
try_offset_access (void *value, guint32 idx)
{
	register void* me __asm__ ("r2");
	void ***p = (void***)((char*)me + 284);
	int idx1 = idx / 32;
	int idx2 = idx % 32;
	if (!p [idx1])
		return 0;
	if (value != p[idx1][idx2])
		return 0;
	return 1;
}
#endif

void
mono_arch_finish_init (void)
{
}

#define CMP_SIZE (PPC_LOAD_SEQUENCE_LENGTH + 4)
#define BR_SIZE 4
#define LOADSTORE_SIZE 4
#define JUMP_IMM_SIZE 12
#define JUMP_IMM32_SIZE (PPC_LOAD_SEQUENCE_LENGTH + 8)
#define ENABLE_WRONG_METHOD_CHECK 0

gpointer
mono_arch_build_imt_trampoline (MonoVTable *vtable, MonoIMTCheckItem **imt_entries, int count,
								gpointer fail_tramp)
{
	int i;
	int size = 0;
	guint8 *code, *start;

	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		if (item->is_equals) {
			if (item->check_target_idx) {
				if (!item->compare_done)
					item->chunk_size += CMP_SIZE;
				if (item->has_target_code)
					item->chunk_size += BR_SIZE + JUMP_IMM32_SIZE;
				else
					item->chunk_size += LOADSTORE_SIZE + BR_SIZE + JUMP_IMM_SIZE;
			} else {
				if (fail_tramp) {
					item->chunk_size += CMP_SIZE + BR_SIZE + JUMP_IMM32_SIZE * 2;
					if (!item->has_target_code)
						item->chunk_size += LOADSTORE_SIZE;
				} else {
					item->chunk_size += LOADSTORE_SIZE + JUMP_IMM_SIZE;
#if ENABLE_WRONG_METHOD_CHECK
					item->chunk_size += CMP_SIZE + BR_SIZE + 4;
#endif
				}
			}
		} else {
			item->chunk_size += CMP_SIZE + BR_SIZE;
			imt_entries [item->check_target_idx]->compare_done = TRUE;
		}
		size += item->chunk_size;
	}
	/* the initial load of the vtable address */
	size += PPC_LOAD_SEQUENCE_LENGTH + LOADSTORE_SIZE;
	if (fail_tramp) {
		code = (guint8 *)mini_alloc_generic_virtual_trampoline (vtable, size);
	} else {
		MonoMemoryManager *mem_manager = m_class_get_mem_manager (vtable->klass);
		code = mono_mem_manager_code_reserve (mem_manager, size);
	}
	start = code;

	/*
	 * We need to save and restore r12 because it might be
	 * used by the caller as the vtable register, so
	 * clobbering it will trip up the magic trampoline.
	 *
	 * FIXME: Get rid of this by making sure that r12 is
	 * not used as the vtable register in interface calls.
	 */
	ppc_stptr (code, ppc_r12, PPC_RET_ADDR_OFFSET, ppc_sp);
	ppc_load (code, ppc_r12, (gsize)(& (vtable->vtable [0])));

	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		item->code_target = code;
		if (item->is_equals) {
			if (item->check_target_idx) {
				if (!item->compare_done) {
					ppc_load (code, ppc_r0, (gsize)item->key);
					ppc_compare_log (code, 0, MONO_ARCH_IMT_REG, ppc_r0);
				}
				item->jmp_code = code;
				ppc_bc (code, PPC_BR_FALSE, PPC_BR_EQ, 0);
				if (item->has_target_code) {
					ppc_load_ptr (code, ppc_r0, item->value.target_code);
				} else {
					ppc_ldptr (code, ppc_r0, (sizeof (target_mgreg_t) * item->value.vtable_slot), ppc_r12);
					ppc_ldptr (code, ppc_r12, PPC_RET_ADDR_OFFSET, ppc_sp);
				}
				ppc_mtctr (code, ppc_r0);
				ppc_bcctr (code, PPC_BR_ALWAYS, 0);
			} else {
				if (fail_tramp) {
					ppc_load (code, ppc_r0, (gulong)item->key);
					ppc_compare_log (code, 0, MONO_ARCH_IMT_REG, ppc_r0);
					item->jmp_code = code;
					ppc_bc (code, PPC_BR_FALSE, PPC_BR_EQ, 0);
					if (item->has_target_code) {
						ppc_load_ptr (code, ppc_r0, item->value.target_code);
					} else {
						g_assert (vtable);
						ppc_load_ptr (code, ppc_r0, & (vtable->vtable [item->value.vtable_slot]));
						ppc_ldptr_indexed (code, ppc_r0, 0, ppc_r0);
					}
					ppc_mtctr (code, ppc_r0);
					ppc_bcctr (code, PPC_BR_ALWAYS, 0);
					ppc_patch (item->jmp_code, code);
					ppc_load_ptr (code, ppc_r0, fail_tramp);
					ppc_mtctr (code, ppc_r0);
					ppc_bcctr (code, PPC_BR_ALWAYS, 0);
					item->jmp_code = NULL;
				} else {
					/* enable the commented code to assert on wrong method */
#if ENABLE_WRONG_METHOD_CHECK
					ppc_load (code, ppc_r0, (guint32)item->key);
					ppc_compare_log (code, 0, MONO_ARCH_IMT_REG, ppc_r0);
					item->jmp_code = code;
					ppc_bc (code, PPC_BR_FALSE, PPC_BR_EQ, 0);
#endif
					ppc_ldptr (code, ppc_r0, (sizeof (target_mgreg_t) * item->value.vtable_slot), ppc_r12);
					ppc_ldptr (code, ppc_r12, PPC_RET_ADDR_OFFSET, ppc_sp);
					ppc_mtctr (code, ppc_r0);
					ppc_bcctr (code, PPC_BR_ALWAYS, 0);
#if ENABLE_WRONG_METHOD_CHECK
					ppc_patch (item->jmp_code, code);
					ppc_break (code);
					item->jmp_code = NULL;
#endif
				}
			}
		} else {
			ppc_load (code, ppc_r0, (gulong)item->key);
			ppc_compare_log (code, 0, MONO_ARCH_IMT_REG, ppc_r0);
			item->jmp_code = code;
			ppc_bc (code, PPC_BR_FALSE, PPC_BR_LT, 0);
		}
	}
	/* patch the branches to get to the target items */
	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		if (item->jmp_code) {
			if (item->check_target_idx) {
				ppc_patch (item->jmp_code, imt_entries [item->check_target_idx]->code_target);
			}
		}
	}

	if (!fail_tramp)
		UnlockedAdd (&mono_stats.imt_trampolines_size, code - start);
	g_assert (code - start <= size);
	mono_arch_flush_icache (start, size);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_IMT_TRAMPOLINE, NULL));

	mono_tramp_info_register (mono_tramp_info_create (NULL, start, code - start, NULL, NULL), NULL);

	return start;
}

MonoMethod*
mono_arch_find_imt_method (host_mgreg_t *regs, guint8 *code)
{
	host_mgreg_t *r = (host_mgreg_t*)regs;

	return (MonoMethod*)(gsize) r [MONO_ARCH_IMT_REG];
}

MonoVTable*
mono_arch_find_static_call_vtable (host_mgreg_t *regs, guint8 *code)
{
	return (MonoVTable*)(gsize) regs [MONO_ARCH_RGCTX_REG];
}

GSList*
mono_arch_get_cie_program (void)
{
	GSList *l = NULL;

	mono_add_unwind_op_def_cfa (l, (guint8*)NULL, (guint8*)NULL, ppc_r1, 0);

	return l;
}

MonoInst*
mono_arch_emit_inst_for_method (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	MonoInst *ins = NULL;
	int opcode = 0;

	if (cmethod->klass == mono_class_try_get_math_class ()) {
		if (strcmp (cmethod->name, "Sqrt") == 0) {
			opcode = OP_SQRT;
		} else if (strcmp (cmethod->name, "Abs") == 0 && fsig->params [0]->type == MONO_TYPE_R8) {
			opcode = OP_ABS;
		}

		if (opcode && fsig->param_count == 1) {
			MONO_INST_NEW (cfg, ins, opcode);
			ins->type = STACK_R8;
			ins->dreg = mono_alloc_freg (cfg);
			ins->sreg1 = args [0]->dreg;
			MONO_ADD_INS (cfg->cbb, ins);
		}

		/* Check for Min/Max for (u)int(32|64) */
		opcode = 0;
		if (cpu_hw_caps & PPC_ISA_2_03) {
			if (strcmp (cmethod->name, "Min") == 0) {
				if (fsig->params [0]->type == MONO_TYPE_I4)
					opcode = OP_IMIN;
				if (fsig->params [0]->type == MONO_TYPE_U4)
					opcode = OP_IMIN_UN;
#ifdef __mono_ppc64__
				else if (fsig->params [0]->type == MONO_TYPE_I8)
					opcode = OP_LMIN;
				else if (fsig->params [0]->type == MONO_TYPE_U8)
					opcode = OP_LMIN_UN;
#endif
			} else if (strcmp (cmethod->name, "Max") == 0) {
				if (fsig->params [0]->type == MONO_TYPE_I4)
					opcode = OP_IMAX;
				if (fsig->params [0]->type == MONO_TYPE_U4)
					opcode = OP_IMAX_UN;
#ifdef __mono_ppc64__
				else if (fsig->params [0]->type == MONO_TYPE_I8)
					opcode = OP_LMAX;
				else if (fsig->params [0]->type == MONO_TYPE_U8)
					opcode = OP_LMAX_UN;
#endif
			}
			/*
			 * TODO: Floating point version with fsel, but fsel has
			 * some peculiarities (need a scratch reg unless
			 * comparing with 0, NaN/Inf behaviour (then MathF too)
			 */
		}

		if (opcode && fsig->param_count == 2) {
			MONO_INST_NEW (cfg, ins, opcode);
			ins->type = fsig->params [0]->type == MONO_TYPE_I4 ? STACK_I4 : STACK_I8;
			ins->dreg = mono_alloc_ireg (cfg);
			ins->sreg1 = args [0]->dreg;
			ins->sreg2 = args [1]->dreg;
			MONO_ADD_INS (cfg->cbb, ins);
		}

		/* Rounding instructions */
		opcode = 0;
		if ((cpu_hw_caps & PPC_ISA_2X) && (fsig->param_count == 1) && (fsig->params [0]->type == MONO_TYPE_R8)) {
			/*
			 * XXX: sysmath.c and the POWER ISA documentation for
			 * frin[.] imply rounding is a little more complicated
			 * than expected; the semantics are slightly different,
			 * so just "frin." isn't a drop-in replacement. Floor,
			 * Truncate, and Ceiling seem to work normally though.
			 * (also, no float versions of these ops, but frsp
			 * could be preprended?)
			 */
			//if (!strcmp (cmethod->name, "Round"))
			//	opcode = OP_ROUND;
			if (!strcmp (cmethod->name, "Floor"))
				opcode = OP_PPC_FLOOR;
			else if (!strcmp (cmethod->name, "Ceiling"))
				opcode = OP_PPC_CEIL;
			else if (!strcmp (cmethod->name, "Truncate"))
				opcode = OP_PPC_TRUNC;
			if (opcode != 0) {
				MONO_INST_NEW (cfg, ins, opcode);
				ins->type = STACK_R8;
				ins->dreg = mono_alloc_freg (cfg);
				ins->sreg1 = args [0]->dreg;
				MONO_ADD_INS (cfg->cbb, ins);
			}
		}
	}
	if (cmethod->klass == mono_class_try_get_mathf_class ()) {
		if (strcmp (cmethod->name, "Sqrt") == 0) {
			opcode = OP_SQRTF;
		} /* XXX: POWER has no single-precision normal FPU abs? */

		if (opcode && fsig->param_count == 1) {
			MONO_INST_NEW (cfg, ins, opcode);
			ins->type = STACK_R4;
			ins->dreg = mono_alloc_freg (cfg);
			ins->sreg1 = args [0]->dreg;
			MONO_ADD_INS (cfg->cbb, ins);
		}
	}
	return ins;
}

host_mgreg_t
mono_arch_context_get_int_reg (MonoContext *ctx, int reg)
{
	if (reg == ppc_r1)
		return (host_mgreg_t)(gsize)MONO_CONTEXT_GET_SP (ctx);

	return ctx->regs [reg];
}

guint32
mono_arch_get_patch_offset (guint8 *code)
{
	return 0;
}

/*
 * mono_aot_emit_load_got_addr:
 *
 *   Emit code to load the got address.
 * On PPC, the result is placed into r30.
 */
guint8*
mono_arch_emit_load_got_addr (guint8 *start, guint8 *code, MonoCompile *cfg, MonoJumpInfo **ji)
{
	ppc_bl (code, 1);
	ppc_mflr (code, ppc_r30);
	if (cfg)
		mono_add_patch_info (cfg, code - start, MONO_PATCH_INFO_GOT_OFFSET, NULL);
	else
		*ji = mono_patch_info_list_prepend (*ji, code - start, MONO_PATCH_INFO_GOT_OFFSET, NULL);
	/* arch_emit_got_address () patches this */
#if defined(TARGET_POWERPC64)
	ppc_nop (code);
	ppc_nop (code);
	ppc_nop (code);
	ppc_nop (code);
#else
	ppc_load32 (code, ppc_r0, 0);
	ppc_add (code, ppc_r30, ppc_r30, ppc_r0);
#endif

	set_code_cursor (cfg, code);
	return code;
}

/*
 * mono_ppc_emit_load_aotconst:
 *
 *   Emit code to load the contents of the GOT slot identified by TRAMP_TYPE and
 * TARGET from the mscorlib GOT in full-aot code.
 * On PPC, the GOT address is assumed to be in r30, and the result is placed into 
 * r12.
 */
guint8*
mono_arch_emit_load_aotconst (guint8 *start, guint8 *code, MonoJumpInfo **ji, MonoJumpInfoType tramp_type, gconstpointer target)
{
	/* Load the mscorlib got address */
	ppc_ldptr (code, ppc_r12, sizeof (target_mgreg_t), ppc_r30);
	*ji = mono_patch_info_list_prepend (*ji, code - start, tramp_type, target);
	/* arch_emit_got_access () patches this */
	ppc_load32 (code, ppc_r0, 0);
	ppc_ldptr_indexed (code, ppc_r12, ppc_r12, ppc_r0);

	return code;
}

/* Soft Debug support */
#ifdef MONO_ARCH_SOFT_DEBUG_SUPPORTED

/*
 * BREAKPOINTS
 */

/*
 * mono_arch_set_breakpoint:
 *
 *   See mini-amd64.c for docs.
 */
void
mono_arch_set_breakpoint (MonoJitInfo *ji, guint8 *ip)
{
	guint8 *code = ip;
	guint8 *orig_code = code;

	ppc_load_sequence (code, ppc_r12, (gsize)bp_trigger_page);
	ppc_ldptr (code, ppc_r12, 0, ppc_r12);

	g_assert (code - orig_code == BREAKPOINT_SIZE);

	mono_arch_flush_icache (orig_code, code - orig_code);
}

/*
 * mono_arch_clear_breakpoint:
 *
 *   See mini-amd64.c for docs.
 */
void
mono_arch_clear_breakpoint (MonoJitInfo *ji, guint8 *ip)
{
	guint8 *code = ip;
	int i;

	for (i = 0; i < BREAKPOINT_SIZE / 4; ++i)
		ppc_nop (code);

	mono_arch_flush_icache (ip, code - ip);
}

/*
 * mono_arch_is_breakpoint_event:
 *
 *   See mini-amd64.c for docs.
 */
gboolean
mono_arch_is_breakpoint_event (void *info, void *sigctx)
{
	siginfo_t* sinfo = (siginfo_t*) info;
	/* Sometimes the address is off by 4 */
	if (sinfo->si_addr >= bp_trigger_page && (guint8*)sinfo->si_addr <= (guint8*)bp_trigger_page + 128)
		return TRUE;
	else
		return FALSE;
}

/*
 * mono_arch_skip_breakpoint:
 *
 *   See mini-amd64.c for docs.
 */
void
mono_arch_skip_breakpoint (MonoContext *ctx, MonoJitInfo *ji)
{
	/* skip the ldptr */
	MONO_CONTEXT_SET_IP (ctx, (guint8*)MONO_CONTEXT_GET_IP (ctx) + 4);
}

/*
 * SINGLE STEPPING
 */
	
/*
 * mono_arch_start_single_stepping:
 *
 *   See mini-amd64.c for docs.
 */
void
mono_arch_start_single_stepping (void)
{
	mono_mprotect (ss_trigger_page, mono_pagesize (), 0);
}
	
/*
 * mono_arch_stop_single_stepping:
 *
 *   See mini-amd64.c for docs.
 */
void
mono_arch_stop_single_stepping (void)
{
	mono_mprotect (ss_trigger_page, mono_pagesize (), MONO_MMAP_READ);
}

/*
 * mono_arch_is_single_step_event:
 *
 *   See mini-amd64.c for docs.
 */
gboolean
mono_arch_is_single_step_event (void *info, void *sigctx)
{
	siginfo_t* sinfo = (siginfo_t*) info;
	/* Sometimes the address is off by 4 */
	if (sinfo->si_addr >= ss_trigger_page && (guint8*)sinfo->si_addr <= (guint8*)ss_trigger_page + 128)
		return TRUE;
	else
		return FALSE;
}

/*
 * mono_arch_skip_single_step:
 *
 *   See mini-amd64.c for docs.
 */
void
mono_arch_skip_single_step (MonoContext *ctx)
{
	/* skip the ldptr */
	MONO_CONTEXT_SET_IP (ctx, (guint8*)MONO_CONTEXT_GET_IP (ctx) + 4);
}

/*
 * mono_arch_create_seq_point_info:
 *
 *   See mini-amd64.c for docs.
 */
SeqPointInfo*
mono_arch_get_seq_point_info (guint8 *code)
{
	NOT_IMPLEMENTED;
	return NULL;
}

#endif

gboolean
mono_arch_opcode_supported (int opcode)
{
	switch (opcode) {
	case OP_ATOMIC_ADD_I4:
	case OP_ATOMIC_CAS_I4:
#ifdef TARGET_POWERPC64
	case OP_ATOMIC_ADD_I8:
	case OP_ATOMIC_CAS_I8:
#endif
		return TRUE;
	default:
		return FALSE;
	}
}

gpointer
mono_arch_load_function (MonoJitICallId jit_icall_id)
{
	gpointer target = NULL;
	switch (jit_icall_id) {
#undef MONO_AOT_ICALL
#define MONO_AOT_ICALL(x) case MONO_JIT_ICALL_ ## x: target = (gpointer)x; break;
	MONO_AOT_ICALL (mono_ppc_throw_exception)
	}
	return target;
}
