/**
 * \file
 * LOONGARCH64 backend for the Mono code generator
 *
 * Authors:
 *   Qiao Pengcheng (qiaopengcheng@loongson.cn), Liu An (liuan@loongson.cn)
 *
 * Copyright (c) 2021 Loongson Technology, Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include "mini.h"
#include "cpu-loongarch64.h"
#include "ir-emit.h"
#include "aot-runtime.h"
#include "mini-runtime.h"

#include <mono/arch/loongarch64/loongarch64-codegen.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/mono-memory-model.h>
#include <mono/metadata/abi-details.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/marshal-shared.h>

#include "interp/interp.h"

#define THUNK_SIZE (4 * 6)

/* The single step trampoline */
static gpointer ss_trampoline;

/* The breakpoint trampoline */
static gpointer bp_trampoline;

#define EMIT_STORE(code, dreg, base, offset, tmp_reg)  if (loongarch_is_imm12 (offset)) {  \
			loongarch_std (code, dreg, base, offset);                \
		} else {                                                     \
			/* NOTE: Assuming the offset is less than 2G. */         \
			loongarch_lu12iw (code, tmp_reg, (offset >> 12) & 0xfffff);          \
			loongarch_ori (code, tmp_reg, tmp_reg, offset & 0xfff);  \
			loongarch_stxd (code, dreg, base, tmp_reg);              \
		}

#define EMIT_IMM_PTR(code, dreg, imm_ptr)   loongarch_lu12iw (code, dreg, (imm_ptr >> 12) & 0xfffff); \
			loongarch_lu32id (code, dreg, (imm_ptr >> 32) & 0xfffff);  \
			loongarch_ori (code, dreg, dreg, imm_ptr & 0xfff)

/* Emit a call sequence to 'v', using 'D' as a scratch register if necessary */
#define loongarch_call(c,D,v) do {	\
		guint32 _target = (guint32)(v); \
		if (1 || ((v) == NULL) || ((_target & 0xfc000000) != (((guint32)(c)) & 0xfc000000))) { \
			loongarch_load_const (c, D, _target); \
			loongarch_jirl (c, D, loongarch_ra); \
		} \
		else { \
			;/*loongarch_jumpl (c, _target >> 2);*/ \
		} \
	} while (0)

/* This mutex protects architecture specific caches */
#define mono_mini_arch_lock() mono_os_mutex_lock (&mini_arch_mutex)
#define mono_mini_arch_unlock() mono_os_mutex_unlock (&mini_arch_mutex)
static mono_mutex_t mini_arch_mutex;

/* Index of ms word/register. */
static int ls_word_idx;
/* Index of ls word/register */
static int ms_word_idx;
/* Same for offsets */
static int ls_word_offset;
static int ms_word_offset;

/*
 * The code generated for sequence points reads from this location, which is
 * made read-only when single stepping is enabled.
 */
// static gpointer ss_trigger_page;

/* Enabled breakpoints read from this trigger page */
// static gpointer bp_trigger_page;

#undef DEBUG
#define DEBUG(a) if (cfg->verbose_level > 1) a
#undef DEBUG
#define DEBUG(a) a
#undef DEBUG
#define DEBUG(a)

#define MONO_EMIT_NEW_LOAD_R8(cfg,dr,addr) do { \
		MonoInst *inst;				   \
		MONO_INST_NEW ((cfg), (inst), OP_R8CONST); \
		inst->type = STACK_R8;			   \
		inst->dreg = (dr);		       \
		inst->inst_p0 = (void*)(addr);	       \
		mono_bblock_add_inst (cfg->cbb, inst); \
	} while (0)

#define ins_is_compare(ins) ((ins) && (((ins)->opcode == OP_COMPARE) \
				       || ((ins)->opcode == OP_ICOMPARE) \
				       || ((ins)->opcode == OP_LCOMPARE) \
				       || ((ins)->opcode == OP_RCOMPARE) \
				       || ((ins)->opcode == OP_FCOMPARE)))
#define ins_is_compare_imm(ins) ((ins) && (((ins)->opcode == OP_COMPARE_IMM) \
					   || ((ins)->opcode == OP_ICOMPARE_IMM) \
					   || ((ins)->opcode == OP_LCOMPARE_IMM)))

#define INS_REWRITE(ins, op, _s1, _s2) do { \
			int s1 = _s1;			\
			int s2 = _s2;			\
			ins->opcode = (op);		\
			ins->sreg1 = (s1);		\
			ins->sreg2 = (s2);		\
	} while (0);

#define INS_REWRITE_IMM(ins, op, _s1, _imm) do { \
			int s1 = _s1;			\
			ins->opcode = (op);		\
			ins->sreg1 = (s1);		\
			ins->inst_imm = (_imm);		\
	} while (0);

void mono_arch_emit_this_vret_args (MonoCompile *cfg, MonoCallInst *inst, int this_reg, int this_type, int vt_reg);
MonoInst *mono_arch_get_inst_for_method (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args);

void
mono_arch_flush_icache (guint8 *code, gint size)
{
	__asm__ volatile( "ibar 0;  \n");
}

void
mono_arch_flush_register_windows (void)
{
}

const char*
mono_arch_regname (int reg) {
	static const char * rnames[] = {
		"zero", "ra", "tp", "sp",
		"a0", "a1", "a2", "a3",
		"a4", "a5", "a6", "a7",
		"t0", "t1", "t2", "t3",
		"t4", "t5", "t6", "t7",
		"t8", "r21", "fp", "s0",
		"s1", "s2", "s3", "s4",
		"s5", "s6", "s7", "s8",
	};
	if (reg >= 0 && reg < 32)
		return rnames [reg];
	return "unknown";
}

const char*
mono_arch_fregname (int reg) {
	static const char * rnames[] = {
		"f0", "f1", "f2", "f3",
		"f4", "f5", "f6", "f7",
		"f8", "f9", "f10", "f11",
		"f12", "f13", "f14", "f15",
		"f16", "f17", "f18", "f19",
		"f20", "f21", "f22", "f23",
		"f24", "f25", "f26", "f27",
		"f28", "f29", "f30", "f31"
	};
	if (reg >= 0 && reg < 32)
		return rnames [reg];
	return "unknown";
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
	int k, frame_size = 0;
	guint32 size, align, pad;
	int offset = 0;

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
		size = mini_type_stack_size_full (csig->params [k], &align, csig->pinvoke);

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
}

/* The delegate object plus 3 params */
#define MAX_ARCH_DELEGATE_PARAMS (4 - 1)

static guint8*
get_delegate_invoke_impl (MonoTrampInfo **info, gboolean has_target, gboolean param_count)
{
	guint8 *code, *start;

	if (has_target) {
		start = code = mono_global_codeman_reserve (16);

		/* Replace the this argument with the target */
		loongarch_ldd (code, loongarch_r21, loongarch_a0, MONO_STRUCT_OFFSET (MonoDelegate, method_ptr));
		loongarch_ldd (code, loongarch_a0, loongarch_a0, MONO_STRUCT_OFFSET (MonoDelegate, target));
		loongarch_jirl (code, 0, loongarch_r21, 0);

		g_assert ((code - start) <= 16);

		mono_arch_flush_icache (start, 16);
		MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_DELEGATE_INVOKE, NULL));
	} else {
		int size, i;

		size = 16 + param_count * 4;
		start = code = mono_global_codeman_reserve (size);

		loongarch_ldd (code, loongarch_r21, loongarch_a0, MONO_STRUCT_OFFSET (MonoDelegate, method_ptr));
		/* slide down the arguments */
		for (i = 0; i < param_count; ++i) {
			loongarch_move (code, loongarch_a0 + i, loongarch_a0 + i + 1);
		}
		loongarch_jirl (code, 0, loongarch_r21, 0);

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

/*
 * mono_arch_get_delegate_invoke_impls:
 *
 *   Return a list of MonoAotTrampInfo structures for the delegate invoke impl
 * trampolines.
 */
GSList*
mono_arch_get_delegate_invoke_impls (void)
{
	GSList *res = NULL;
	MonoTrampInfo *info;
	int i;

	get_delegate_invoke_impl (&info, TRUE, 0);
	res = g_slist_prepend (res, info);

	for (i = 0; i <= MAX_ARCH_DELEGATE_PARAMS; ++i) {
		get_delegate_invoke_impl (&info, FALSE, i);
		res = g_slist_prepend (res, info);
	}

	return res;
}

gpointer
mono_arch_get_delegate_invoke_impl (MonoMethodSignature *sig, gboolean has_target)
{
	guint8 *code, *start;

	if (MONO_TYPE_ISSTRUCT (sig->ret))
		return NULL;

	if (has_target) {
		static guint8* cached = NULL;
		mono_mini_arch_lock ();
		if (cached) {
			mono_mini_arch_unlock ();
			return cached;
		}

		if (mono_ee_features.use_aot_trampolines) {
			start = mono_aot_get_trampoline ("delegate_invoke_impl_has_target");
		} else {
			MonoTrampInfo *info;
			start = get_delegate_invoke_impl (&info, TRUE, 0);
			mono_tramp_info_register (info, NULL);
		}
		cached = start;
		mono_mini_arch_unlock ();
		return cached;
	} else {
		static guint8* cache [MAX_ARCH_DELEGATE_PARAMS + 1] = {NULL};
		int i;

		if (sig->param_count > MAX_ARCH_DELEGATE_PARAMS)
			return NULL;
		for (i = 0; i < sig->param_count; ++i)
			if (!mono_is_regsize_var (sig->params [i]))
				return NULL;

		mono_mini_arch_lock ();
		code = cache [sig->param_count];
		if (code) {
			mono_mini_arch_unlock ();
			return code;
		}

		if (mono_ee_features.use_aot_trampolines) {
			char *name = g_strdup_printf ("delegate_invoke_impl_target_%d", sig->param_count);
			start = mono_aot_get_trampoline (name);
			g_free (name);
		} else {
			MonoTrampInfo *info;
			start = get_delegate_invoke_impl (&info, FALSE, sig->param_count);
			mono_tramp_info_register (info, NULL);
		}
		cache [sig->param_count] = start;
		mono_mini_arch_unlock ();
		return start;
	}

	return NULL;
}

gpointer
mono_arch_get_delegate_virtual_invoke_impl (MonoMethodSignature *sig, MonoMethod *method, int offset, gboolean load_imt_reg)
{
	return NULL;
}

gpointer
mono_arch_get_this_arg_from_call (host_mgreg_t *regs, guint8 *code)
{
	g_assert(regs);
	return (gpointer)regs [loongarch_a0];
}

/*
 * Initialize the cpu to execute managed code.
 */
void
mono_arch_cpu_init (void)
{
	ls_word_idx = 0;
	ms_word_idx = 1;

	ls_word_offset = ls_word_idx * 4;
	ms_word_offset = ms_word_idx * 4;
}

/*
 * Initialize architecture specific code.
 */
void
mono_arch_init (void)
{
	if (!mono_aot_only)
		bp_trampoline = mini_get_breakpoint_trampoline ();

	mono_loongarch_gsharedvt_init ();
}

/*
 * Cleanup architecture specific code.
 */
void
mono_arch_cleanup (void)
{
}

gboolean
mono_arch_have_fast_tls (void)
{
	return TRUE;
}

/*
 * This function returns the optimizations supported on this cpu.
 */
guint32
mono_arch_cpu_optimizations (guint32 *exclude_mask)
{//TODO: add loongarch features.
	*exclude_mask = 0;
	return 0;
}

/*
* Set the float struct field info:
* bit0: whether the first field is float;
* bit1: whether the size of first field is 8-bytes;
* bit2: whether the second field is exist;
* bit3: whether the second field is float;
* bit4: whether the size of second field is 8-bytes;
* bit5-bit7 combined representation:
* 001:all member transmit by fregs;
* 002:all member transmit by regs;
* 003:all member transmit by stack;
* 004:one member transmit by reg another by stack;
* 005:one member transmit by reg another by freg;
*/
static void
set_struct_field_info (guint8 *field_info, int *index, int size, gboolean is_float)
{
	int i = *index;
	switch (i) {
	case 0:			//first field
		if (is_float)
			*field_info = (*field_info) | 0x01;
		if (size == 8)
			*field_info = (*field_info) | 0x02;
		break;
	case 1:			//second field
		*field_info = (*field_info) | 0x04;
		if (is_float)
			*field_info = (*field_info) | 0x08;
		if (size == 8)
			*field_info = (*field_info) | 0x10;
		break;
	default:
		g_assert_not_reached ();
		break;
	}
	(*index)++;
}

/*
* Judge struct whether is a float struct, contains three situations:
* first is only one float member (float/double);
* second is only two float member (float and double have four combinations);
* third is one float and one interger (float、double、int、long have eight combinations);
*/
static gboolean
is_struct_float (MonoType *t, guint8 *field_info, int *index)
{
	MonoClass *klass;
	gpointer iter;
	MonoClassField *field;
	MonoType *ftype;

	klass = mono_class_from_mono_type_internal (t);
	iter = NULL;
	while ((field = mono_class_get_fields_internal (klass, &iter))) {
		if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;
		ftype = mono_field_get_type_internal (field);
		ftype = mini_get_underlying_type (ftype);

		if (MONO_TYPE_ISSTRUCT (ftype)) {
			if (!is_struct_float (ftype, field_info, index)) {
				if ((*index == 1) || (*index == 0)) //when Recursive call this is can prevent (((*field_info) & 0x5) == 0) or null struct return false
					continue;
				return FALSE;
			}
			continue;
		} else {
			if ((*index) >= 2)
				return FALSE;
			switch (ftype->type) {
			case MONO_TYPE_I1:
			case MONO_TYPE_U1:
			case MONO_TYPE_I2:
			case MONO_TYPE_U2:
			case MONO_TYPE_I4:
			case MONO_TYPE_U4:
				set_struct_field_info (field_info, index, 4, FALSE);
				break;
			case MONO_TYPE_I:
			case MONO_TYPE_U:
			case MONO_TYPE_PTR:
			case MONO_TYPE_FNPTR:
			case MONO_TYPE_OBJECT:
			case MONO_TYPE_U8:
			case MONO_TYPE_I8:
				set_struct_field_info (field_info, index, 8, FALSE);
				break;
			case MONO_TYPE_R8:
				set_struct_field_info (field_info, index, 8, TRUE);
				break;
			case MONO_TYPE_R4:
				set_struct_field_info (field_info, index, 4, TRUE);
				break;
			case MONO_TYPE_GENERICINST:
				if (!mono_type_generic_inst_is_valuetype (ftype))
					set_struct_field_info (field_info, index, 8, FALSE);
				break;
			default:
				g_assert_not_reached ();
				break;
			}
		}
	}
	if ((*index) == 0 || (*index) > 2)
		return FALSE;
	if (((*field_info) & 0x5) == 0) //exclude only one interger field
		return FALSE;
	if (((*field_info) & 0x9) == 0) //exclude only two interger field
		return FALSE;
	return TRUE;
}

static void
add_general (CallInfo *cinfo, ArgInfo *ainfo, int size, gboolean sign)
{
	if (cinfo->gr >= PARAM_REGS) {
		size = ALIGN_TO (size, 8);
		ainfo->storage = ArgOnStack;
		cinfo->stack_usage = ALIGN_TO (cinfo->stack_usage, size);
		ainfo->offset = cinfo->stack_usage;
		ainfo->slot_size = size;
		ainfo->sign = sign;
		cinfo->stack_usage += size;
	}
	else {
		ainfo->storage = ArgInIReg;
		ainfo->reg = cinfo->gr;
		cinfo->gr++;
	}
}

/*
   Interger standard for struct that size at 0-16 byte
*/
static void __attribute__((noinline))
add_iter_struct (CallInfo *cinfo, ArgInfo *ainfo, int size, int align)
{
	int nregs;
	size = ALIGN_TO (size, 8);
	nregs = size / 8;
	guint8 field_info = ainfo->field_info;
	if (cinfo->gr >= PARAM_REGS) { //all in stack
		cinfo->stack_usage = ALIGN_TO (cinfo->stack_usage, align);
		ainfo->offset = cinfo->stack_usage;
		ainfo->size = size;
		cinfo->stack_usage += size;
		field_info = field_info | 0x60;
	} else if (cinfo->gr + nregs <= PARAM_REGS) { //all in reg
		ainfo->reg = cinfo->gr;
		ainfo->size = size;
		cinfo->gr += nregs;
		field_info = field_info | 0x40;
	} else { //one in reg and one in stack
		for (int i = 0; i < nregs; i++) {
			add_general (cinfo, ainfo, 8, FALSE);
		}
		field_info = field_info | 0x80;
		ainfo->size = size;
	}
	ainfo->storage = ArgStructByVal;
	ainfo->field_info = field_info;
}

static void
add_fpr (CallInfo *cinfo, ArgInfo *ainfo, int size)
{
	g_assert ((size == 4) || (size == 8));

	if (cinfo->fr >= FP_PARAM_REGS) {
		add_general (cinfo, ainfo, size, FALSE); //use interger standard
		ainfo->fin_ireg = TRUE;
		ainfo->size = size;
		//ainfo->slot_size = size;
	} else {
		ainfo->size = size;
		ainfo->storage = ArgInFReg;
		ainfo->reg = cinfo->fr;
		cinfo->fr ++;
	}
}

static void
add_valuetype (CallInfo *cinfo, ArgInfo *ainfo, MonoType *t)
{
	int index, size, align_size, nregs;
	guint32 align;
	guint8 field_info;
	size = mini_type_stack_size_full (t, &align, cinfo->pinvoke);
	align_size = ALIGN_TO (size, 8);
	if (align_size > 16) {
		ainfo->storage = ArgStructByRef;
		ainfo->size = size;
		return;
	}

	nregs = align_size / 8;
	index = 0;
	field_info = 0;
	if (is_struct_float (t, &field_info, &index)) {
		/*
		 * The struct might include nested float structs aligned at 8,
		 * so need to keep track of the offsets of the individual fields.
		 */
		gboolean first_field = ((field_info & 0x1) != 0) ? 1 : 0;
		gboolean second_field = ((field_info & 0x8) != 0) ? 1 : 0;

		if (index == 1) { //only one float field
			if (cinfo->fr < FP_PARAM_REGS) {
				ainfo->storage = ArgStructByVal;
				ainfo->freg = cinfo->fr;
				cinfo->fr++;
				ainfo->size = size;
				field_info = field_info | 0x20;
			} else { //use interger standard
				add_iter_struct (cinfo, ainfo, size, align);
			}
		}
		if ((index == 2) && first_field && second_field) { //only two float field
			if (cinfo->fr + index <= FP_PARAM_REGS) {
				ainfo->storage = ArgStructByVal;
				ainfo->freg = cinfo->fr;
				cinfo->fr +=  index;
				ainfo->size = size;
				field_info = field_info | 0x20;
			} else { //use interger standard
				add_iter_struct (cinfo, ainfo, size, align);
			}
		}
		if ((index == 2) && ((first_field && !second_field) || (!first_field && second_field))) { //one float and one interger
			if ((cinfo->fr < FP_PARAM_REGS) && (cinfo->gr < PARAM_REGS)) {
				ainfo->storage = ArgStructByVal;
				ainfo->freg = cinfo->fr;
				ainfo->reg = cinfo->gr;
				cinfo->fr++;
				cinfo->gr++;
				ainfo->size = size;
				field_info = field_info | 0xa0;
			} else { //use interger standard
				add_iter_struct (cinfo, ainfo, size, align);
			}
		}
		ainfo->field_info |= field_info;
		return;
	}
	ainfo->field_info = 0;
	add_iter_struct (cinfo, ainfo, size, align);
}

// Perform peephole opts which should/can be performed before local regalloc
void
mono_arch_peephole_pass_1 (MonoCompile *cfg, MonoBasicBlock *bb)
{
	//NOT_IMPLEMENTED;
}

void
mono_arch_register_lowlevel_calls (void)
{
}

static guint8*
emit_thunk (guint8 *code, gconstpointer target)
{
		guint8 *p = code;
		loongarch_pcaddi (code, loongarch_r21, 4);
		loongarch_ldd (code, loongarch_r21, loongarch_r21, 0);
		loongarch_jirl (code, 0, loongarch_r21, 0);
		code += 4;
		*(guint64*)code = (guint64)target;
		code += sizeof (guint64);
		mono_arch_flush_icache (p, code - p);
		return code;
}

static gpointer
create_thunk (MonoCompile *cfg, guchar *code, const guchar *target, int relocation)
{
		MonoJitInfo *ji;
		MonoThunkJitInfo *info;
		guint8 *thunks, *p;
		int thunks_size;
		guint8 *orig_target;
		guint8 *target_thunk;
		MonoJitMemoryManager* jit_mm;

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

			cfg->arch.thunks += THUNK_SIZE;
			cfg->arch.thunks_size -= THUNK_SIZE;

			return thunks;
		 } else {
			ji = mini_jit_info_table_find ((char*)code);
			g_assert (ji);
			info = mono_jit_info_get_thunk_info (ji);
			g_assert (info);

			thunks = (guint8*)ji->code_start + info->thunks_offset;
			thunks_size = info->thunks_size;

			orig_target = mono_arch_get_call_target (code + 4);

			/* Arbitrary lock */
			jit_mm = get_default_jit_mm ();

			jit_mm_lock (jit_mm);

			target_thunk = NULL;
			if (orig_target >= thunks && orig_target < thunks + thunks_size) {
			    /* The call already points to a thunk, because of trampolines etc. */
					target_thunk = orig_target;
			} else {
					for (p = thunks; p < thunks + thunks_size; p += THUNK_SIZE) {
						if (((guint32*)p) [0] == 0) {
								/* Free entry */
								target_thunk = p;
								break;
						} else if (((guint64*)p) [2] == (guint64)target) {
								/* Thunk already points to target */
								target_thunk = p;
								break;
						}
					}
			}

			if (!target_thunk) {
				jit_mm_unlock (jit_mm);
				g_print ("thunk failed %p->%p, thunk space=%d method %s, relocation %d", code, target, thunks_size, cfg ? mono_method_full_name (cfg->method, TRUE) : mono_method_full_name (jinfo_get_method (ji), TRUE), relocation);
				g_assert_not_reached ();
            }

			emit_thunk (target_thunk, target);

			jit_mm_unlock (jit_mm);
			return target_thunk;
		}
}

static void
loongarch64_patch_full (MonoCompile *cfg, guint8 *code, guint8 *target, int relocation)
{
	switch (relocation) {
	case MONO_R_LOONGARCH64_B:
	{
		target = MINI_FTNPTR_TO_ADDR (target);
		gint64 offsets = target - code;
		if (IS_B_SI28_LOONGARCH (offsets)) {
			loongarch_b (code, (offsets >> 2) & 0x3ffffff);
		} else {
			gpointer thunk;
			thunk = create_thunk (cfg, code, target, relocation);
			thunk = MINI_FTNPTR_TO_ADDR (thunk);
			gint64 ofs = (guint8*)thunk - code;
			g_assert (IS_B_SI28_LOONGARCH (ofs));
			loongarch_b (code, (ofs >> 0x2));
		}
		break;
	}
	case MONO_R_LOONGARCH64_BC: //BEQ、BNE、BLT、BGE、BLTU、BGEU
	{
		target = MINI_FTNPTR_TO_ADDR (target);
		gint64 offsets = target - code;
		if (IS_B_SI18_LOONGARCH (offsets)) {
			*(guint32*)code = (*(guint32*)code) | ((offsets << 8) & 0x3fffc00);
			code += 4;
		} else {
			g_assert_not_reached ();
		}
		break;
	}
	case MONO_R_LOONGARCH64_BZ:
	{
		target = MINI_FTNPTR_TO_ADDR (target);
		gint64 offsets = target - code;
		if (IS_B_SI23_LOONGARCH (offsets)) {
			*(guint32*)code = (*(guint32*)code) | ((offsets << 8) & 0x3fffc00) | ((offsets >> 18) & 0x1f);
			code += 4;
		} else {
			g_assert_not_reached ();
		}
		break;
	}
	case MONO_R_LOONGARCH64_J:
	{
		guint64 imm = (guint64)target;
		int dreg;
		dreg = (*(guint32*)code) & 0x1f;
		loongarch_lu12iw (code, dreg, (imm >> 12) & 0xfffff);
		loongarch_lu32id (code, dreg, (imm >> 32) & 0xfffff);
		loongarch_lu52id (code, dreg, dreg, (imm >> 52) & 0xfff);
		loongarch_ori (code, dreg, dreg, imm & 0xfff);
		break;
	}
	case MONO_R_LOONGARCH64_BL:
	{
		target = MINI_FTNPTR_TO_ADDR (target);
		gint64 offsets = target - code;
		if (IS_B_SI28_LOONGARCH (offsets)) {
			loongarch_bl (code, offsets >> 0x2);
		} else {
			gpointer thunk;
			thunk = create_thunk (cfg, code, target, relocation);
			thunk = MINI_FTNPTR_TO_ADDR (thunk);
			gint64 ofs = (guint8*)thunk - code;
			g_assert (IS_B_SI28_LOONGARCH (ofs));
			loongarch_bl (code, (ofs >> 0x2));
		}
	}
		break;
	default:
		g_assert_not_reached ();
	}
}

void
mono_loongarch64_patch (guint8 *code, guint8 *target, int relocation)
{
	loongarch64_patch_full (NULL, code, target, relocation);
}

void
mono_arch_patch_code_new (MonoCompile *cfg, guint8 *code, MonoJumpInfo *ji, gpointer target)
{
	guint8 *ip;

	ip = ji->ip.i + code;

	switch (ji->type) {
	case MONO_PATCH_INFO_METHOD_JUMP:
		/* ji->relocation is not set by the caller */
		loongarch64_patch_full (cfg, ip, (guint8*)target, MONO_R_LOONGARCH64_B);
		mono_arch_flush_icache (ip, 8);
		break;
	default:
		loongarch64_patch_full (cfg, ip, (guint8*)target, ji->relocation);
		break;
	case MONO_PATCH_INFO_NONE:
		break;
	}
}

void
mono_arch_finish_init (void)
{
}

guint8*
mono_loongarch_emit_imm64 (guint8 *code, int dreg, gint64 imm)
{
	if (imm >= -0x800 && imm <= 0x7ff) {
		loongarch_addid (code, dreg, loongarch_zero, imm & 0xfff);
	} else if (imm >= -0x80000000L && imm <= 0x7fffffffL) {
		loongarch_lu12iw (code, dreg, (imm >> 12) & 0xfffff);
		loongarch_ori (code, dreg, dreg, imm & 0xfff);
	} else if (imm >= -0x8000000000000L && imm <= 0x7ffffffffffffL) {
		loongarch_lu12iw (code, dreg, (imm >> 12) & 0xfffff);
		loongarch_lu32id (code, dreg, (imm >> 32) & 0xfffff);
		loongarch_ori (code, dreg, dreg, imm & 0xfff);
	} else {
		loongarch_lu12iw (code, dreg, (imm >> 12) & 0xfffff);
		loongarch_lu32id (code, dreg, (imm >> 32) & 0xfffff);
		loongarch_lu52id (code, dreg, dreg, (imm >> 52) & 0xfff);
		loongarch_ori (code, dreg, dreg, imm & 0xfff);
	}
	return code;
}

guint8*
mono_loongarch_emit_jirl (guint8 *code, int reg)
{
	loongarch_jirl (code, 0, reg, 0);
	return code;
}

void
mono_arch_emit_this_vret_args (MonoCompile *cfg, MonoCallInst *inst, int this_reg, int this_type, int vt_reg)
{
	int this_dreg = loongarch_a0;

	if (vt_reg != -1)
		this_dreg = loongarch_a1;

	/* add the this argument */
	if (this_reg != -1) {
		MonoInst *this_ins;
		MONO_INST_NEW (cfg, this_ins, OP_MOVE);
		this_ins->type = this_type;
		this_ins->sreg1 = this_reg;
		this_ins->dreg = mono_alloc_ireg (cfg);
		mono_bblock_add_inst (cfg->cbb, this_ins);
		mono_call_inst_add_outarg_reg (cfg, inst, this_ins->dreg, this_dreg, FALSE);
	}

	if (vt_reg != -1) {
		MonoInst *vtarg;
		MONO_INST_NEW (cfg, vtarg, OP_MOVE);
		vtarg->type = STACK_MP;
		vtarg->sreg1 = vt_reg;
		vtarg->dreg = mono_alloc_ireg (cfg);
		mono_bblock_add_inst (cfg->cbb, vtarg);
		mono_call_inst_add_outarg_reg (cfg, inst, vtarg->dreg, loongarch_a0, FALSE);
	}
}

MonoInst*
mono_arch_get_inst_for_method (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	MonoInst *ins = NULL;

	return ins;
}

host_mgreg_t
mono_arch_context_get_int_reg (MonoContext *ctx, int reg)
{
	return ctx->regs [reg];
}

host_mgreg_t*
mono_arch_context_get_int_reg_address (MonoContext *ctx, int reg)
{
	return &ctx->regs [reg];
}

void
mono_arch_context_set_int_reg (MonoContext *ctx, int reg, host_mgreg_t val)
{
	ctx->regs [reg] = val;
}

MonoMethod*
mono_arch_find_imt_method (host_mgreg_t *regs, guint8 *code)
{
	return (MonoMethod*) regs [MONO_ARCH_IMT_REG];
}

MonoVTable*
mono_arch_find_static_call_vtable (host_mgreg_t *regs, guint8 *code)
{
	return (MonoVTable*) regs [MONO_ARCH_RGCTX_REG];
}

#ifndef DISABLE_JIT

static __attribute__ ((__warn_unused_result__)) guint8*
emit_ldd (guint8 *code, int rt, int rn, int imm)
{
	if (loongarch_is_imm12 (imm)) {
		loongarch_ldd (code, rt, rn, (imm & 0xfff));
	} else {
		g_assert (rn != loongarch_r21);
		code = mono_loongarch_emit_imm64 (code, loongarch_r21, imm);
		loongarch_ldxd (code, rt, rn, loongarch_r21);
	}
	return code;
}

static __attribute__ ((__warn_unused_result__)) guint8*
emit_std (guint8 *code, int rt, int rn, int imm)
{
	if (loongarch_is_imm12 (imm)) {
		loongarch_std (code, rt, rn, (imm & 0xfff));
	} else {
		g_assert (rn != loongarch_r21);
		code = mono_loongarch_emit_imm64 (code, loongarch_r21, imm);
		loongarch_stxd (code, rt, rn, loongarch_r21);
	}
	return code;
}

static guint8*
emit_aotconst (MonoCompile *cfg, guint8 *code, int dreg, guint32 patch_type, gconstpointer data)
{
	mono_add_patch_info (cfg, code - cfg->native_code, (MonoJumpInfoType)patch_type, data);
	/* See arch_emit_got_access () in aot-compiler.c */
	loongarch_ldd (code, dreg, dreg, 0);
	loongarch_nop (code);
	loongarch_nop (code);
	loongarch_nop (code);
	return NULL;
}

void
mono_arch_decompose_opts (MonoCompile *cfg, MonoInst *ins)
{
	int tmp1 = -1;
	int tmp2 = -1;
	int tmp3 = -1;
	int tmp4 = -1;
	MonoInst* inst_tmp1 = NULL;
	MonoInst* inst_tmp2 = NULL;
	MonoInst* inst_tmp3 = NULL;
	MonoInst* inst_tmp4 = NULL;
	MonoBasicBlock *target_bb = NULL;
	MonoBasicBlock *pos_ov = NULL;

	switch (ins->opcode) {
	case OP_IADD_OVF:
		tmp1 = mono_alloc_ireg (cfg);
		tmp2 = mono_alloc_ireg (cfg);
		tmp3 = mono_alloc_ireg (cfg);
		tmp4 = mono_alloc_ireg (cfg);
		NEW_BBLOCK (cfg, target_bb);
		NEW_BBLOCK (cfg, pos_ov);
		//first,judge whether the two sreg signs are same (sreg1 > 0 && sreg2 > 0) || (sreg1 < 0 && sreg2 < 0)
		ins->opcode = OP_IADD;
		EMIT_NEW_BIALU_IMM (cfg, inst_tmp1, OP_ISHL_IMM, tmp1, ins->sreg1, 0x0);
		EMIT_NEW_BIALU_IMM (cfg, inst_tmp2, OP_LA_SLTI, tmp2, tmp1, 0);
		EMIT_NEW_BIALU_IMM (cfg, inst_tmp3, OP_ISHL_IMM, tmp3, ins->sreg2, 0x0);
		EMIT_NEW_BIALU_IMM (cfg, inst_tmp4, OP_LA_SLTI, tmp4, tmp3, 0);
		MONO_EMIT_NEW_BIALU (cfg, OP_ICOMPARE, -1, tmp2, tmp4);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_LBNE_UN, target_bb);

		//second,judge whether the sreg1 is a positive number
		MONO_EMIT_NEW_ICOMPARE_IMM (cfg, tmp2, 0);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_IBEQ, pos_ov);

		//start handler sreg1 < 0,sreg < 0
		MONO_EMIT_NEW_BIALU (cfg, OP_ICOMPARE, -1, ins->sreg1, ins->dreg);
		MONO_EMIT_NEW_COND_EXC (cfg, ILT, "OverflowException");
		MONO_EMIT_NEW_BIALU (cfg, OP_ICOMPARE, -1, ins->sreg1, ins->dreg);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_IBGE, target_bb);

		//start handler sreg1 > 0,sreg2 > 0
		MONO_START_BB (cfg, pos_ov);
		MONO_EMIT_NEW_BIALU (cfg, OP_ICOMPARE, -1, ins->dreg, ins->sreg1);
		MONO_EMIT_NEW_COND_EXC (cfg, ILT, "OverflowException");

		//skip overflow
		MONO_START_BB (cfg, target_bb);
		break;
	case OP_IADD_OVF_UN:
		ins->opcode = OP_IADD;
		MONO_EMIT_NEW_BIALU (cfg, OP_ICOMPARE, -1, ins->dreg, ins->sreg1);
		MONO_EMIT_NEW_COND_EXC (cfg, ILT_UN, "OverflowException");
		break;
	case OP_ISUB_OVF:
		tmp1 = mono_alloc_ireg (cfg);
		tmp2 = mono_alloc_ireg (cfg);
		tmp3 = mono_alloc_ireg (cfg);
		tmp4 = mono_alloc_ireg (cfg);
		NEW_BBLOCK (cfg, target_bb);
		NEW_BBLOCK (cfg, pos_ov);
		//first,judge whether the two sreg signs are different
		ins->opcode = OP_ISUB;
		EMIT_NEW_BIALU_IMM (cfg, inst_tmp1, OP_ISHL_IMM, tmp1, ins->sreg1, 0x0);
		EMIT_NEW_BIALU_IMM (cfg, inst_tmp2, OP_LA_SLTI, tmp2, tmp1, 0);
		EMIT_NEW_BIALU_IMM (cfg, inst_tmp3, OP_ISHL_IMM, tmp3, ins->sreg2, 0x0);
		EMIT_NEW_BIALU_IMM (cfg, inst_tmp4, OP_LA_SLTI, tmp4, tmp3, 0);
		MONO_EMIT_NEW_BIALU (cfg, OP_LCOMPARE, -1, tmp2, tmp4);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_LBEQ, target_bb);

		//second,judge whether the sreg1 is a positive number
		MONO_EMIT_NEW_LCOMPARE_IMM (cfg, tmp2, 0);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_IBEQ, pos_ov);

		//start handler sreg1 < 0,sreg2 > 0
		MONO_EMIT_NEW_BIALU (cfg, OP_ICOMPARE, -1, ins->sreg1, ins->dreg);
		MONO_EMIT_NEW_COND_EXC (cfg, ILT, "OverflowException");
		MONO_EMIT_NEW_BIALU (cfg, OP_ICOMPARE, -1, ins->sreg1, ins->dreg);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_IBGE, target_bb);

		//start handler sreg1 > 0,sreg2 < 0
		MONO_START_BB (cfg, pos_ov);
		MONO_EMIT_NEW_BIALU (cfg, OP_ICOMPARE, -1, ins->dreg, ins->sreg1);
		MONO_EMIT_NEW_COND_EXC (cfg, ILT, "OverflowException");

		//skip overflow
		MONO_START_BB (cfg, target_bb);
		break;
	case OP_ISUB_OVF_UN:
		ins->opcode = OP_ISUB;
		MONO_EMIT_NEW_BIALU (cfg, OP_ICOMPARE, -1, ins->sreg1, ins->sreg2);
		MONO_EMIT_NEW_COND_EXC (cfg, ILT_UN, "OverflowException");
		break;
	case OP_LADD_OVF:
		tmp1 = mono_alloc_ireg (cfg);
		tmp2 = mono_alloc_ireg (cfg);
		NEW_BBLOCK (cfg, target_bb);
		NEW_BBLOCK (cfg, pos_ov);
		if (COMPILE_LLVM (cfg))
			break;
		//first,judge whether the two sreg signs are same (sreg1 > 0 && sreg2 > 0) || (sreg1 < 0 && sreg 2 < 0)
		ins->opcode = OP_LADD;
		EMIT_NEW_BIALU_IMM (cfg, inst_tmp1, OP_LA_SLTI, tmp1, ins->sreg1, 0);
		EMIT_NEW_BIALU_IMM (cfg, inst_tmp2, OP_LA_SLTI, tmp2, ins->sreg2, 0);
		MONO_EMIT_NEW_BIALU (cfg, OP_LCOMPARE, -1, tmp1, tmp2);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_LBNE_UN, target_bb);

		//second,judge whether the sreg1 is a positive number
		MONO_EMIT_NEW_LCOMPARE_IMM (cfg, tmp1, 0);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_IBEQ, pos_ov);

		//start handler sreg1 < 0,sreg < 0
		MONO_EMIT_NEW_BIALU (cfg, OP_LCOMPARE, -1, ins->sreg1, ins->dreg);
		MONO_EMIT_NEW_COND_EXC (cfg, LT, "OverflowException");
		MONO_EMIT_NEW_BIALU (cfg, OP_LCOMPARE, -1, ins->sreg1, ins->dreg);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_LBGE, target_bb);

		//start handler sreg1 > 0,sreg2 < 0
		MONO_START_BB (cfg, pos_ov);
		MONO_EMIT_NEW_BIALU (cfg, OP_LCOMPARE, -1, ins->dreg, ins->sreg1);
		MONO_EMIT_NEW_COND_EXC (cfg, LT, "OverflowException");

		//skip overflow
		MONO_START_BB (cfg, target_bb);
		break;
	case OP_LADD_OVF_UN:
		if (COMPILE_LLVM (cfg))
			break;
		ins->opcode = OP_LADD;
		MONO_EMIT_NEW_BIALU (cfg, OP_LCOMPARE, -1, ins->dreg, ins->sreg1);
		MONO_EMIT_NEW_COND_EXC (cfg, LT_UN, "OverflowException");
		break;
	case OP_LSUB_OVF:
		tmp1 = mono_alloc_ireg (cfg);
		tmp2 = mono_alloc_ireg (cfg);
		NEW_BBLOCK (cfg, target_bb);
		NEW_BBLOCK (cfg, pos_ov);
		if (COMPILE_LLVM (cfg))
			break;
		//first,judge whether the two sreg signs are different
		ins->opcode = OP_LSUB;
		EMIT_NEW_BIALU_IMM (cfg, inst_tmp1, OP_LA_SLTI, tmp1, ins->sreg1, 0);
		EMIT_NEW_BIALU_IMM (cfg, inst_tmp2, OP_LA_SLTI, tmp2, ins->sreg2, 0);
		MONO_EMIT_NEW_BIALU (cfg, OP_LCOMPARE, -1, tmp1, tmp2);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_LBEQ, target_bb);

		//second,judge whether the sreg1 is a positive number
		MONO_EMIT_NEW_LCOMPARE_IMM (cfg, tmp1, 0);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_IBEQ, pos_ov);

		//start handler sreg1 < 0,sreg > 0
		MONO_EMIT_NEW_BIALU (cfg, OP_LCOMPARE, -1, ins->sreg1, ins->dreg);
		MONO_EMIT_NEW_COND_EXC (cfg, LT, "OverflowException");
		MONO_EMIT_NEW_BIALU (cfg, OP_LCOMPARE, -1, ins->sreg1, ins->dreg);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_LBGE, target_bb);

		//start handler sreg1 > 0,sreg2 < 0
		MONO_START_BB (cfg, pos_ov);
		MONO_EMIT_NEW_BIALU (cfg, OP_LCOMPARE, -1, ins->dreg, ins->sreg1);
		MONO_EMIT_NEW_COND_EXC (cfg, LT, "OverflowException");

		//skip overflow
		MONO_START_BB (cfg, target_bb);
		break;
	case OP_LSUB_OVF_UN:
		if (COMPILE_LLVM (cfg))
			break;
		ins->opcode = OP_LSUB;
		MONO_EMIT_NEW_BIALU (cfg, OP_LCOMPARE, -1, ins->sreg1, ins->sreg2);
		MONO_EMIT_NEW_COND_EXC (cfg, LT_UN, "OverflowException");
		break;
	}
}

/*
 * Emits like:
 * - ori sp, fp, 0
 * - ldd fp, sp, 0
 * - ldd ra, sp, 8
 * - update sp.
 * maybe clobbers loongarch_r21.
 */
__attribute__ ((__warn_unused_result__)) guint8*
mono_loongarch64_emit_destroy_frame (guint8 *code, int stack_offset)
{
	loongarch_ori (code, loongarch_sp, loongarch_fp, 0);

	loongarch_ldd (code, loongarch_fp, loongarch_sp, 0);
	loongarch_ldd (code, loongarch_ra, loongarch_sp, 8);

	if (loongarch_is_imm12 (stack_offset)) {
		loongarch_addid (code, loongarch_sp, loongarch_sp, stack_offset);
	} else {
		loongarch_lu12iw (code, loongarch_r21, (stack_offset >> 12) & 0xfffff);
		loongarch_ori (code, loongarch_r21, loongarch_r21, stack_offset & 0xfff);
		loongarch_addd (code, loongarch_sp, loongarch_sp, loongarch_r21);
	}
	return code;
}

gboolean
mono_arch_opcode_needs_emulation (MonoCompile *cfg, int opcode)
{
	NOT_IMPLEMENTED;
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

		/* we can only allocate 32 bit values */
		if (mono_is_regsize_var (ins->inst_vtype)) {
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

	regs = g_list_prepend (regs, (gpointer)loongarch_s0);
	regs = g_list_prepend (regs, (gpointer)loongarch_s1);
	regs = g_list_prepend (regs, (gpointer)loongarch_s2);
	regs = g_list_prepend (regs, (gpointer)loongarch_s3);
	regs = g_list_prepend (regs, (gpointer)loongarch_s4);
	regs = g_list_prepend (regs, (gpointer)loongarch_s5);
	regs = g_list_prepend (regs, (gpointer)loongarch_s6);
	/* s8 is reserved for cfg->arch.args_reg */
	/* s7 is reserved for the imt argument */
	return regs;
}

/*
 * mono_arch_regalloc_cost:
 *
 * Return the cost, in number of memory references, of the action of
 * allocating the variable VMV into a register during global register
 * allocation.
 */
guint32
mono_arch_regalloc_cost (MonoCompile *cfg, MonoMethodVar *vmv)
{
	MonoInst *ins = cfg->varinfo [vmv->idx];
	if (ins->opcode == OP_ARG)
		return 1;
	else
		return 2;
}

static void
add_param (CallInfo *cinfo, ArgInfo *ainfo, MonoType *t)
{
	MonoType *ptype;

	ptype = mini_get_underlying_type (t);
	switch (ptype->type) {
	case MONO_TYPE_I1:
		add_general (cinfo, ainfo, 1, TRUE);
		break;
	case MONO_TYPE_U1:
		add_general (cinfo, ainfo, 1, FALSE);
		break;
	case MONO_TYPE_I2:
		add_general (cinfo, ainfo, 2, TRUE);
		break;
	case MONO_TYPE_U2:
		add_general (cinfo, ainfo, 2, FALSE);
		break;
#ifdef MONO_ARCH_ILP32
	case MONO_TYPE_I:
#endif
	case MONO_TYPE_I4:
		add_general (cinfo, ainfo, 4, TRUE);
		break;
#ifdef MONO_ARCH_ILP32
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_OBJECT:
#endif
	case MONO_TYPE_U4:
		add_general (cinfo, ainfo, 4, FALSE);
		break;
#ifndef MONO_ARCH_ILP32
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_OBJECT:
#endif
	case MONO_TYPE_U8:
	case MONO_TYPE_I8:
		add_general (cinfo, ainfo, 8, FALSE);
		break;
	case MONO_TYPE_R8:
		add_fpr (cinfo, ainfo, 8);
		break;
	case MONO_TYPE_R4:
		add_fpr (cinfo, ainfo, 4);
		break;
	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_TYPEDBYREF:
		add_valuetype (cinfo, ainfo, ptype);
		break;
	case MONO_TYPE_VOID:
		ainfo->storage = ArgNone;
		break;
	case MONO_TYPE_GENERICINST:
		if (!mono_type_generic_inst_is_valuetype (ptype)) {
			add_general (cinfo, ainfo, 8, FALSE);
		} else if (mini_is_gsharedvt_variable_type (ptype)) {
			ainfo->storage = ArgStructByRef;
		} else {
			add_valuetype (cinfo, ainfo, ptype);
		}
		break;
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		g_assert (mini_is_gsharedvt_type (ptype));
		ainfo->storage = ArgStructByRef;
		break;
	default:
		g_assert_not_reached ();
		break;
	}
}

static int
call_info_size (MonoMethodSignature *sig)
{
	int n = sig->hasthis + sig->param_count;

	return sizeof (CallInfo) + (sizeof (ArgInfo) * n);
}

/*
 * get_call_info:
 *
 *  Obtain information about a call according to the calling convention.
 */
static CallInfo*
get_call_info (MonoMemPool *mp, MonoMethodSignature *sig)
{
	int n = sig->hasthis + sig->param_count;
	int pstart, i;
	CallInfo *cinfo;
	ArgInfo *ainfo;

	DEBUG(printf("params: %d\n", sig->param_count));
	if (mp)
		cinfo = mono_mempool_alloc0 (mp, sizeof (CallInfo) + (sizeof (ArgInfo) * n));
	else
		cinfo = g_malloc0 (sizeof (CallInfo) + (sizeof (ArgInfo) * n));

	cinfo->nargs = n;
	cinfo->pinvoke = sig->pinvoke;
	/* Reset state */
	cinfo->gr = LOONGARCH_FIRST_ARG_REG;
	cinfo->fr = LOONGARCH_FIRST_FPARG_REG;
	cinfo->stack_usage = 0;

	/* return value */
	add_param (cinfo, &cinfo->ret, sig->ret);
	if (cinfo->ret.storage == ArgStructByRef) {
		if (sig->pinvoke) {
			cinfo->ret.reg = loongarch_a0;
			cinfo->gr ++;
		} else {
			cinfo->stack_usage = ALIGN_TO (cinfo->stack_usage, 8);
			cinfo->ret.offset = cinfo->stack_usage;
			cinfo->stack_usage += 8;
		}
	} else {
		cinfo->gr = LOONGARCH_FIRST_ARG_REG;
		cinfo->fr = LOONGARCH_FIRST_FPARG_REG;
	}

	/* Parameters */
	if (sig->hasthis)
		add_general (cinfo, cinfo->args + 0, 8, FALSE);

	pstart = 0;
	for (i = pstart; i < sig->param_count; ++i) {
		//TODO: should confirm  MONO_CALL_VARARG.
		if ((sig->call_convention == MONO_CALL_VARARG) && (i == sig->sentinelpos)) {
			/* Prevent implicit arguments and sig_cookie from being passed in registers
			   Emit the signature cookie just before the implicit arguments */
			cinfo->gr = LOONGARCH_LAST_ARG_REG + 1;
			cinfo->fr = LOONGARCH_LAST_FPARG_REG + 1;
			cinfo->stack_usage = ALIGN_TO (cinfo->stack_usage, 8);
			cinfo->sig_cookie.storage = ArgOnStack;
			cinfo->sig_cookie.offset = cinfo->stack_usage;
			cinfo->sig_cookie.slot_size = 8;
			cinfo->stack_usage += 8;
		}
		DEBUG(printf("param %d: ", i));

		ainfo = cinfo->args + sig->hasthis + i;
		add_param (cinfo, ainfo, sig->params [i]);
		if (ainfo->storage == ArgStructByRef) {
		/* Pass the argument address in the next register */
			if (cinfo->gr >= PARAM_REGS) {
				ainfo->field_info = 0x60; //through stack
				cinfo->stack_usage = ALIGN_TO (cinfo->stack_usage, 8);
				ainfo->offset = cinfo->stack_usage;
				cinfo->stack_usage += 8;
			} else {
				ainfo->field_info = 0x0;
				ainfo->reg = cinfo->gr;
				cinfo->gr ++;
			}
		}
	}

	/* Handle the case where there are no implicit arguments */
	if ((sig->call_convention == MONO_CALL_VARARG) && (i == sig->sentinelpos)) {
		/* Prevent implicit arguments and sig_cookie from
		   being passed in registers */
		/* Emit the signature cookie just before the implicit arguments */
		cinfo->stack_usage = ALIGN_TO (cinfo->stack_usage, 8);
		cinfo->sig_cookie.storage = ArgOnStack;
		cinfo->sig_cookie.offset = cinfo->stack_usage;
		cinfo->sig_cookie.slot_size = 8;
		//cinfo->sig_cookie.sign = FALSE;
		cinfo->stack_usage += 8;
	}

	/* align stack size to 16 */
	cinfo->stack_usage = ALIGN_TO (cinfo->stack_usage, MONO_ARCH_FRAME_ALIGNMENT);

	return cinfo;
}

static int
arg_need_temp (ArgInfo *ainfo)
{
	guint8 field_info = ainfo->field_info;
	int stor_type = (field_info >> 5) & 0x7;
	if (ainfo->storage == ArgStructByVal && (stor_type == 0x4 || stor_type == 0x5))
		return ainfo->size;
	return 0;
}

static gpointer
arg_get_storage (CallContext *ccontext, ArgInfo *ainfo)
{
		switch (ainfo->storage) {
		case ArgInIReg:
			return &ccontext->gregs [ainfo->reg];
		case ArgInFReg:
			return &ccontext->fregs [ainfo->freg];
		case ArgOnStack:
			return ccontext->stack + ainfo->offset;
		case ArgStructByRef:
			if (ainfo->field_info == 0x60) {
				return ccontext->stack + ainfo->offset;
			} else {
				return (gpointer) ccontext->gregs [ainfo->reg];
			}
		case ArgStructByVal: {
			guint8 field_info;
			field_info = ainfo->field_info;
			int stor_type = (field_info >> 5) & 0x7;
			switch (stor_type) {
			case 0x1: //all store in the freg
				return &ccontext->fregs [ainfo->freg];
				break;
			case 0x2: //all store in the reg
				return &ccontext->gregs [ainfo->reg];
				break;
			case 0x3: //all store in stack
				return ccontext->stack + ainfo->offset;
				break;
			default:
				g_assert_not_reached ();
				break;
			}
		}
		default:
			g_error ("Arg storage type not yet supported");
		}
}

static void
arg_get_val (CallContext *ccontext, ArgInfo *ainfo, gpointer dest)
{
	g_assert (arg_need_temp (ainfo));

	guint8 field_info = ainfo->field_info;
	int stor_type = (field_info >> 5) & 0x7;
	int fp1_size = (field_info & 0x2) ? 8 : 4;
	int fp2_size = (field_info & 0x10) ? 8 : 4;
	if (stor_type == 5) {
		// one store in reg another in freg
		if (field_info & 0x1) {
			if (fp1_size == 4)
				*(float*)dest = *(float*)&ccontext->fregs [ainfo->freg];
			else
				*(double*)dest = *(double*)&ccontext->fregs [ainfo->freg];
			if (fp2_size == 4) {
				dest = (gpointer)((long)dest + fp1_size);
				*(int*)dest = *(int*)&ccontext->gregs [ainfo->reg];
			} else {
				dest = (gpointer)((long)dest + 8);
				*(long*)dest = *(long*)&ccontext->gregs [ainfo->reg];
			}
		} else {
			if (fp1_size == 4)
				*(int*)dest = *(int*)&ccontext->gregs [ainfo->reg];
			else
				*(long*)dest = *(long*)&ccontext->gregs [ainfo->reg];
			if (fp2_size == 4) {
				dest = (gpointer)((long)dest + fp1_size);
				*(float*)dest = *(float*)&ccontext->fregs [ainfo->freg];
			} else {
				dest = (gpointer)((long)dest + 8);
				*(double*)dest = *(double*)&ccontext->fregs [ainfo->freg];
			}
		}
	} else {
		g_assert_not_reached ();
	}
}

static void
arg_set_val (CallContext *ccontext, ArgInfo *ainfo, gpointer src)
{
	g_assert (arg_need_temp (ainfo));

	guint8 field_info = ainfo->field_info;
	int stor_type = (field_info >> 5) & 0x7;
	int fp1_size = (field_info & 0x2) ? 8 : 4;
	int fp2_size = (field_info & 0x10) ? 8 : 4;
	switch (stor_type) {
		case 0x4: //one store in reg another in stack
			*(long *)&ccontext->gregs [ainfo->reg] = *(long *)src;
			src = (gpointer)((long)src + 8);
			*(gsize *)(ccontext->stack + ainfo->offset) = *(gsize *)src;
			break;
		case 0x5: //one store in reg another in freg
			if (field_info & 0x1) {
				if (fp1_size == 4)
					*(float*)&ccontext->fregs [ainfo->freg] = *(float*)src;
				else
					*(double*)&ccontext->fregs [ainfo->freg] = *(double*)src;
				if (fp2_size == 4) {
					src = (gpointer)((long)src + fp1_size);
					*(int*)&ccontext->gregs [ainfo->reg] = *(int*)src;
				} else {
					src = (gpointer)((long)src + 8);
					*(long*)&ccontext->gregs [ainfo->reg] = *(long*)src;
				}
			} else {
				if (fp1_size == 4)
					*(int*)&ccontext->gregs [ainfo->reg] = *(int*)src;
				else
					*(long*)&ccontext->gregs [ainfo->reg] = *(long*)src;
				if (fp2_size == 4) {
					src = (gpointer)((long)src + fp1_size);
					*(float*)&ccontext->fregs [ainfo->freg] = *(float*)src;
				} else {
					src = (gpointer)((long)src + 8);
					*(double*)&ccontext->fregs [ainfo->freg] = *(double*)src;
				}
			}
			break;
		default:
			g_assert_not_reached ();
			break;
	}
}

gpointer
mono_arch_get_interp_native_call_info (MonoMemoryManager *mem_manager, MonoMethodSignature *sig)
{
	CallInfo *cinfo = get_call_info (NULL, sig);
	if (mem_manager) {
		int size = call_info_size (sig);
		gpointer res = mono_mem_manager_alloc0 (mem_manager, size);
		memcpy (res, cinfo, size);
		g_free (cinfo);
		return res;
	} else {
		return cinfo;
	}
}

void
mono_arch_free_interp_native_call_info (gpointer call_info)
{
	/* Allocated by get_call_info () */
	g_free (call_info);
}

/* Set arguments in the ccontext (for i2n entry) */
void
mono_arch_set_native_call_context_args (CallContext *ccontext, gpointer frame, MonoMethodSignature *sig, gpointer call_info)
{
	const MonoEECallbacks *interp_cb = mini_get_interp_callbacks ();
	CallInfo *cinfo = (CallInfo*)call_info;
	gpointer storage;
	ArgInfo *ainfo;

	memset (ccontext, 0, sizeof (CallContext));

	ccontext->stack_size = ALIGN_TO (cinfo->stack_usage, MONO_ARCH_FRAME_ALIGNMENT);
	if (ccontext->stack_size)
		ccontext->stack = (guint8*)g_calloc (1, ccontext->stack_size);

	if (sig->ret->type != MONO_TYPE_VOID) {
		ainfo = &cinfo->ret;
		if (ainfo->storage == ArgStructByRef) {
			storage = interp_cb->frame_arg_to_storage ((MonoInterpFrameHandle)frame, sig, -1);
			*(gpointer*)(ccontext->stack + cinfo->ret.offset) = (gsize*)storage;
		}
	}

	g_assert (!sig->hasthis);

	for (int i = 0; i < sig->param_count; i++) {
		ainfo = &cinfo->args [i];

		if (ainfo->storage == ArgStructByRef) {
			storage = arg_get_storage (ccontext, ainfo);
			*(host_mgreg_t *)storage = (host_mgreg_t)interp_cb->frame_arg_to_storage ((MonoInterpFrameHandle)frame, sig, i);
			continue;
		}

		int temp_size = arg_need_temp (ainfo);

		if (temp_size)
			storage = alloca (temp_size); // FIXME? alloca in a loop
		else
			storage = arg_get_storage (ccontext, ainfo);

		interp_cb->frame_arg_to_data ((MonoInterpFrameHandle)frame, sig, i, storage);
		if (temp_size)
			arg_set_val (ccontext, ainfo, storage);
	}

	g_free (cinfo);
}

/* Gets the return value from ccontext (for i2n exit) */
void
mono_arch_get_native_call_context_ret (CallContext *ccontext, gpointer frame, MonoMethodSignature *sig, gpointer call_info)
{
	const MonoEECallbacks *interp_cb;
	CallInfo *cinfo;
	ArgInfo *ainfo;
	gpointer storage;

	if (sig->ret->type == MONO_TYPE_VOID)
		return;

	interp_cb = mini_get_interp_callbacks ();
	cinfo = (CallInfo*)call_info;
	ainfo = &cinfo->ret;

	if (ainfo->storage != ArgStructByRef) {
		int temp_size = arg_need_temp (ainfo);

		if (temp_size) {
			storage = alloca (temp_size);
			arg_get_val (ccontext, ainfo, storage);
		} else {
			storage = arg_get_storage (ccontext, ainfo);
		}
		interp_cb->data_to_frame_arg ((MonoInterpFrameHandle)frame, sig, -1, storage);
	}

	g_free (cinfo);
}

void
mono_arch_create_vars (MonoCompile *cfg)
{
	MonoMethodSignature *sig;
	CallInfo *cinfo;

	sig = mono_method_signature_internal (cfg->method);
	if (!cfg->arch.cinfo)
		cfg->arch.cinfo = get_call_info (cfg->mempool, sig);
	cinfo = cfg->arch.cinfo;

	if (cinfo->ret.storage == ArgStructByRef) {
		cfg->vret_addr = mono_compile_create_var (cfg, mono_get_int_type (), OP_LOCAL);
		cfg->vret_addr->flags |= MONO_INST_VOLATILE;
		if (G_UNLIKELY (cfg->verbose_level > 1)) {
			printf ("vret_addr = ");
			mono_print_ins (cfg->vret_addr);
		}
	}

	if (cfg->gen_sdb_seq_points) {
		MonoInst *ins;

		if (cfg->compile_aot) {
			ins = mono_compile_create_var (cfg, mono_get_int_type (), OP_LOCAL);
			ins->flags |= MONO_INST_VOLATILE;
			cfg->arch.seq_point_info_var = ins;
		}

		ins = mono_compile_create_var (cfg, mono_get_int_type (), OP_LOCAL);
		ins->flags |= MONO_INST_VOLATILE;
		cfg->arch.ss_tramp_var = ins;

		ins = mono_compile_create_var (cfg, mono_get_int_type (), OP_LOCAL);
		ins->flags |= MONO_INST_VOLATILE;
		cfg->arch.bp_tramp_var = ins;
	}

	if (cfg->method->save_lmf) {
		cfg->create_lmf_var = TRUE;
		cfg->lmf_ir = TRUE;
	}
}

/*
 * Set var information according to the calling convention. loongarch64 version.
 * The locals var stuff should most likely be split in another method.
 */
void
mono_arch_allocate_vars (MonoCompile *cfg)
{
	MonoMethodSignature *sig;
	MonoInst *ins;
	CallInfo *cinfo;
	ArgInfo *ainfo;
	int i, offset, size, align;
	guint32 locals_stack_size, locals_stack_align;
	gint32 *offsets;
	MonoInst *vtaddr;
	/*
	 * Allocate arguments and locals to either register (OP_REGVAR) or to a stack slot (OP_REGOFFSET).
	 * Compute cfg->stack_offset and update cfg->used_int_regs.
	 */

	sig = mono_method_signature_internal (cfg->method);

	if (!cfg->arch.cinfo)
		cfg->arch.cinfo = get_call_info (cfg->mempool, sig);
	cinfo = cfg->arch.cinfo;

	/*
	 * The LoongArch64 ABI always uses a frame pointer.
	 * Now the fp points to the bottom of the frame,
	 * and stack slots are at positive offsets.
	 * If some arguments are received on the stack, their offsets relative to fp can
	 * not be computed right now because the stack frame might grow due to spilling
	 * done by the local register allocator. To solve this, we reserve a register
	 * which points to them.
	 * The stack frame looks like this:
	 * args_reg -> <bottom of parent frame>
	 *             <locals etc>
	 *       fp -> <saved fp+lr>
	 *       sp -> <localloc/params area>
	 */
	cfg->frame_reg = loongarch_fp;
	cfg->flags |= MONO_CFG_HAS_SPILLUP;

	//offset = 0;
	/* Saved fp+lr */
	offset = 16;

	if (cinfo->stack_usage) {
		g_assert (!(cfg->used_int_regs & (1 << loongarch_s8)));
		cfg->arch.args_reg = loongarch_s8;
		cfg->used_int_regs |= 1 << loongarch_s8;
	}

	if (cfg->method->save_lmf) {
		/* The LMF var is allocated normally */
	} else {
		/* Callee saved regs */
		cfg->arch.saved_gregs_offset = offset;
		for (i = loongarch_s0; i < 32; ++i)
			if (cfg->used_int_regs & (1 << i))
				offset += 8;
	}

	/* Return value */
	switch (cinfo->ret.storage) {
	case ArgStructByVal: {
		/* Allocate a local to hold the result, the epilog will copy it to the correct place */
		cfg->ret->opcode = OP_REGOFFSET;
		cfg->ret->inst_basereg = cfg->frame_reg;
		cfg->ret->inst_offset = offset;
		offset += 16;
	}
		break;
	case ArgInFReg:
	case ArgInIReg: {
		cfg->ret->opcode = OP_REGVAR;
		cfg->ret->dreg = cinfo->ret.reg;
	}
		break;
	case ArgStructByRef: {
		cfg->vret_addr->opcode = OP_REGOFFSET;
		cfg->vret_addr->inst_basereg = cfg->frame_reg;
		cfg->vret_addr->inst_offset = offset;
		offset += 8;
		if (G_UNLIKELY (cfg->verbose_level > 1)) {
			printf ("vret_addr =");
			mono_print_ins (cfg->vret_addr);
		}
	}
		break;
	case ArgNone:
		break;
	default:
		g_assert_not_reached ();
		break;
	}

	/* Arguments */
	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		ainfo = cinfo->args + i;

		ins = cfg->args [i];
		if (ins->opcode == OP_REGVAR)
			continue;

		ins->opcode = OP_REGOFFSET;
		ins->inst_basereg = cfg->frame_reg;

		switch (ainfo->storage) {
		case ArgInIReg:
		case ArgInFReg:
			// FIXME: Use nregs/size
			/* These will be copied to the stack in the prolog */
			ins->inst_offset = offset;
			offset += 8;
			break;
		case ArgOnStack:
			/* These are in the parent frame */
			g_assert (cfg->arch.args_reg);
			ins->inst_basereg = cfg->arch.args_reg;
			ins->inst_offset = ainfo->offset;
			break;
		case ArgStructByVal:
			if (((ainfo->field_info >> 5) & 0x7) == 3) {
				g_assert (cfg->arch.args_reg);
				ins->inst_basereg = cfg->arch.args_reg;
				ins->inst_offset = ainfo->offset;
			} else {
				ins->opcode = OP_REGOFFSET;
				ins->inst_basereg = cfg->frame_reg;
				/* These arguments are saved to the stack in the prolog */
				ins->inst_offset = offset;
				if (cfg->verbose_level >= 2)
					printf ("arg %d allocated to %s+0x%0x.\n", i, mono_arch_regname (ins->inst_basereg), (int)ins->inst_offset);
				offset += 16;
			}
			break;
		case ArgStructByRef:
			 /* The vtype address is in a register, will be copied to the stack in the prolog */
			MONO_INST_NEW (cfg, vtaddr, 0);
			vtaddr->opcode = OP_REGOFFSET;
			if (((ainfo->field_info >> 5) & 0x7) == 3) {
				g_assert (cfg->arch.args_reg);
				vtaddr->inst_basereg = cfg->arch.args_reg;
				vtaddr->inst_offset = ainfo->offset;
			} else {
				vtaddr->inst_basereg = cfg->frame_reg;
				vtaddr->inst_offset = offset;
				offset += 8;
			}
			 /* Need an indirection */
			ins->opcode = OP_VTARG_ADDR;
			ins->inst_left = vtaddr;
			break;
		default:
			g_assert_not_reached ();
			break;
		}
	}

	/* Allocate these first so they have a small offset, OP_SEQ_POINT depends on this */
	// FIXME: Allocate these to registers
	ins = cfg->arch.seq_point_info_var;
	if (ins) {
		size = 8;
		align = 8;
		offset += align - 1;
		offset &= ~(align - 1);
		ins->opcode = OP_REGOFFSET;
		ins->inst_basereg = cfg->frame_reg;
		ins->inst_offset = offset;
		offset += size;
	}
	ins = cfg->arch.ss_tramp_var;
	if (ins) {
		size = 8;
		align = 8;
		offset += align - 1;
		offset &= ~(align - 1);
		ins->opcode = OP_REGOFFSET;
		ins->inst_basereg = cfg->frame_reg;
		ins->inst_offset = offset;
		offset += size;
	}
	ins = cfg->arch.bp_tramp_var;
	if (ins) {
		size = 8;
		align = 8;
		offset += align - 1;
		offset &= ~(align - 1);
		ins->opcode = OP_REGOFFSET;
		ins->inst_basereg = cfg->frame_reg;
		ins->inst_offset = offset;
		offset += size;
	}

	/* Locals */
	offsets = mono_allocate_stack_slots (cfg, FALSE, &locals_stack_size, &locals_stack_align);
	if (locals_stack_align)
		offset = ALIGN_TO (offset, locals_stack_align);

	for (i = cfg->locals_start; i < cfg->num_varinfo; i++) {
		if (offsets [i] != -1) {
			ins = cfg->varinfo [i];
			ins->opcode = OP_REGOFFSET;
			ins->inst_basereg = cfg->frame_reg;
			ins->inst_offset = offset + offsets [i];
			//printf ("allocated local %d to ", i); mono_print_tree_nl (ins);
		}
	}
	offset += locals_stack_size;

	offset = ALIGN_TO (offset, MONO_ARCH_FRAME_ALIGNMENT);

	cfg->stack_offset = offset;
}

#ifdef ENABLE_LLVM
LLVMCallInfo*
mono_arch_get_llvm_call_info (MonoCompile *cfg, MonoMethodSignature *sig)
{
	int i, n;
	CallInfo *cinfo;
	ArgInfo *ainfo;
	LLVMCallInfo *linfo;

	n = sig->param_count + sig->hasthis;

	cinfo = get_call_info (cfg->mempool, sig);

	linfo = mono_mempool_alloc0 (cfg->mempool, sizeof (LLVMCallInfo) + (sizeof (LLVMArgInfo) * n));

	switch (cinfo->ret.storage) {
	case ArgInIReg:
	case ArgInFReg:
	case ArgInFRegR4:
	case ArgNone:
		break;
	case ArgVtypeByRef:
		linfo->ret.storage = LLVMArgVtypeByRef;
		break;
	default:
		g_assert_not_reached ();
		break;
	}

	for (i = 0; i < n; ++i) {
		LLVMArgInfo *lainfo = &linfo->args [i];

		ainfo = cinfo->args + i;

		lainfo->storage = LLVMArgNone;

		switch (ainfo->storage) {
		case ArgInIReg:
		case ArgInFReg:
		case ArgInFRegR4:
		case ArgOnStack:
		case ArgOnStackR4:
		case ArgOnStackR8:
			lainfo->storage = LLVMArgNormal;
			break;
		case ArgVtypeByRef:
		case ArgVtypeByRefOnStack:
			lainfo->storage = LLVMArgVtypeByRef;
			break;
		case ArgHFA: {
			int j;

			lainfo->storage = LLVMArgAsFpArgs;
			lainfo->nslots = ainfo->nregs;
			lainfo->esize = ainfo->esize;
			for (j = 0; j < ainfo->nregs; ++j)
				lainfo->pair_storage [j] = LLVMArgInFPReg;
			break;
		}
		case ArgVtypeInIRegs:
			lainfo->storage = LLVMArgAsIArgs;
			lainfo->nslots = ainfo->nregs;
			break;
		case ArgVtypeOnStack:
			if (ainfo->hfa) {
				int j;
				/* Same as above */
				lainfo->storage = LLVMArgAsFpArgs;
				lainfo->nslots = ainfo->nregs;
				lainfo->esize = ainfo->esize;
				for (j = 0; j < ainfo->nregs; ++j)
					lainfo->pair_storage [j] = LLVMArgInFPReg;
			} else {
				lainfo->storage = LLVMArgAsIArgs;
				lainfo->nslots = ainfo->size / 8;
			}
			break;
		default:
			g_assert_not_reached ();
			break;
		}
	}

	return linfo;
}
#endif

static void
add_outarg_reg (MonoCompile *cfg, MonoCallInst *call, ArgStorage storage, int reg, MonoInst *arg, gboolean isR4, gboolean fin_ireg)
{
	MonoInst *ins;

	switch (storage) {
	case ArgInIReg:
		if (fin_ireg) {
			if (isR4) {
				MONO_INST_NEW (cfg, ins, OP_MOVE_F_TO_I4);
			} else {
				MONO_INST_NEW (cfg, ins, OP_MOVE_F_TO_I8);
			}
		} else {
			MONO_INST_NEW (cfg, ins, OP_MOVE);
		}
		ins->dreg = mono_alloc_ireg_copy (cfg, arg->dreg);
		ins->sreg1 = arg->dreg;
		MONO_ADD_INS (cfg->cbb, ins);
		mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, reg, FALSE);
		break;
	case ArgInFReg:
		if (isR4) {
			if (cfg->r4fp)
				MONO_INST_NEW (cfg, ins, OP_RMOVE);
			else
				MONO_INST_NEW (cfg, ins, OP_LA_SETFREG_R4);
		} else {
			MONO_INST_NEW (cfg, ins, OP_FMOVE);
		}
		ins->dreg = mono_alloc_freg (cfg);
		ins->sreg1 = arg->dreg;
		MONO_ADD_INS (cfg->cbb, ins);
		mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, reg, TRUE);
		break;
	default:
		g_assert_not_reached ();
		break;
	}
}

/* Fixme: we need an alignment solution for enter_method and mono_arch_call_opcode,
 * currently alignment in mono_arch_call_opcode is computed without arch_get_argument_info
 */

/*
 * take the arguments and generate the arch-specific
 * instructions to properly call the function in call.
 * This includes pushing, moving arguments to the right register
 * etc.
 * Issue: who does the spilling if needed, and when?
 */
static void
emit_sig_cookie (MonoCompile *cfg, MonoCallInst *call, CallInfo *cinfo)
{
	MonoMethodSignature *tmp_sig;
	MonoInst *sig_arg;

	if (MONO_IS_TAILCALL_OPCODE (call))
		NOT_IMPLEMENTED;

	/* TODO: Add support for signature tokens to AOT */
	cfg->disable_aot = TRUE;

	/*
	 * mono_ArgIterator_Setup assumes the signature cookie is
	 * passed first and all the arguments which were before it are
	 * passed on the stack after the signature. So compensate by
	 * passing a different signature.
	 */
	tmp_sig = mono_metadata_signature_dup (call->signature);
	tmp_sig->param_count -= call->signature->sentinelpos;
	tmp_sig->sentinelpos = 0;
	memcpy (tmp_sig->params, call->signature->params + call->signature->sentinelpos, tmp_sig->param_count * sizeof (MonoType*));

	MONO_INST_NEW (cfg, sig_arg, OP_I8CONST);
	sig_arg->dreg = mono_alloc_ireg (cfg);
	sig_arg->inst_p0 = tmp_sig;
	MONO_ADD_INS (cfg->cbb, sig_arg);

	MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, loongarch_sp, cinfo->sig_cookie.offset, sig_arg->dreg);
}

void
mono_arch_emit_call (MonoCompile *cfg, MonoCallInst *call)
{
	MonoInst *arg, *vtarg;
	MonoMethodSignature *sig;
	ArgInfo *ainfo;
	int i, n;
	CallInfo *cinfo;

	sig = call->signature;

	cinfo = get_call_info (cfg->mempool, sig);

	if (cinfo->ret.storage == ArgStructByVal && !MONO_IS_TAILCALL_OPCODE (call)) {
		/*
		 * The vtype is returned in registers, save the return area address in a local, and save the vtype into
		 * the location pointed to by it after call in emit_move_return_value ().
		 */
		if (!cfg->arch.vret_addr_loc) {
			cfg->arch.vret_addr_loc = mono_compile_create_var (cfg, mono_get_int_type (), OP_LOCAL);
			/* Prevent it from being register allocated or optimized away */
			cfg->arch.vret_addr_loc->flags |= MONO_INST_VOLATILE;
		}

		MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, cfg->arch.vret_addr_loc->dreg, call->vret_var->dreg);
	}

	if (cinfo->ret.storage == ArgStructByRef) {
		g_assert (call->vret_var == cfg->vret_addr || !MONO_IS_TAILCALL_OPCODE (call));
		if (sig->pinvoke) {
			/* Pass the vtype return address in R4 */
			MONO_INST_NEW (cfg, vtarg, OP_MOVE);
			vtarg->sreg1 = call->vret_var->dreg;
			vtarg->dreg = mono_alloc_preg (cfg);
			MONO_ADD_INS (cfg->cbb, vtarg);

			mono_call_inst_add_outarg_reg (cfg, call, vtarg->dreg, cinfo->ret.reg, FALSE);
		} else {
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, loongarch_sp, cinfo->ret.offset, call->vret_var->dreg);
		}
	}

	n = cinfo->nargs;
	for (i = 0; i < n; ++i) {
		ainfo = cinfo->args + i;
		arg = call->args [i];
		gboolean isR4 = ainfo->size == 4 ? TRUE : FALSE;
		gboolean fin_ireg = ainfo->fin_ireg;

		if ((sig->call_convention == MONO_CALL_VARARG) && (i == sig->sentinelpos)) {
			/* Emit the signature cookie just before the implicit arguments */
			emit_sig_cookie (cfg, call, cinfo);
		}

		switch (ainfo->storage) {
		case ArgInIReg:
		case ArgInFReg:
			add_outarg_reg (cfg, call, ainfo->storage, ainfo->reg, arg, isR4, fin_ireg);
			break;
		case ArgOnStack:
			switch (ainfo->slot_size) {
			case 8:
				if (fin_ireg)  //double pass from stack
					MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORER8_MEMBASE_REG, loongarch_sp, ainfo->offset, arg->dreg);
				else
					MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, loongarch_sp, ainfo->offset, arg->dreg);
				break;
			case 4:
				if (fin_ireg)  //float pass from stack
					MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORER4_MEMBASE_REG, loongarch_sp, ainfo->offset, arg->dreg);
				else
					MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI4_MEMBASE_REG, loongarch_sp, ainfo->offset, arg->dreg);
				break;
			case 2:
				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI2_MEMBASE_REG, loongarch_sp, ainfo->offset, arg->dreg);
				break;
			case 1:
				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI1_MEMBASE_REG, loongarch_sp, ainfo->offset, arg->dreg);
				break;
			default:
				g_assert_not_reached ();
				break;
			}
			break;
		case ArgStructByVal:
		case ArgStructByRef: {
			MonoInst *ins;
			guint32 align;
			guint32 size;

			size = mono_class_value_size (arg->klass, &align);

			MONO_INST_NEW (cfg, ins, OP_OUTARG_VT);
			ins->sreg1 = arg->dreg;
			ins->klass = arg->klass;
			ins->backend.size = size;
			ins->inst_p0 = call;
			ins->inst_p1 = mono_mempool_alloc (cfg->mempool, sizeof (ArgInfo));
			memcpy (ins->inst_p1, ainfo, sizeof (ArgInfo));
			MONO_ADD_INS (cfg->cbb, ins);
			break;
		}
		default:
			g_assert_not_reached ();
			break;
		}
	}

	/* Handle the case where there are no implicit arguments */
	if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG) && (n == sig->sentinelpos))
		emit_sig_cookie (cfg, call, cinfo);

	call->call_info = cinfo;
	call->stack_usage = cinfo->stack_usage;
}

void
mono_arch_emit_outarg_vt (MonoCompile *cfg, MonoInst *ins, MonoInst *src)
{
	MonoCallInst *call = (MonoCallInst*)ins->inst_p0;
	ArgInfo *ainfo = (ArgInfo*)ins->inst_p1;
	MonoInst *load;

	switch (ainfo->storage) {
	case ArgStructByVal: {
		guint8 field_info = ainfo->field_info;
		int second_exist = (field_info >> 2) & 0x1;
		int stor_type = (field_info >> 5) & 0x7;
		int fp1_size = (field_info & 0x2) ? 8 : 4;
		int fp2_size = (field_info & 0x10) ? 8 : 4;
		int offsets = 0;
		gboolean isR4 = FALSE;
		switch (stor_type) {
		case 0x1: {//all store in the freg
			if (fp1_size == 4) {
				MONO_INST_NEW (cfg, load, OP_LOADR4_MEMBASE);
				isR4 = TRUE;
			} else {
				MONO_INST_NEW (cfg, load, OP_LOADR8_MEMBASE);
			}
			load->dreg = mono_alloc_freg (cfg);
			load->inst_basereg = src->dreg;
			load->inst_offset = 0;
			MONO_ADD_INS (cfg->cbb, load);
			add_outarg_reg (cfg, call, ArgInFReg, ainfo->freg, load, isR4, FALSE);
			if (second_exist) {
				if (fp2_size == 4) {
					offsets = fp1_size;
					MONO_INST_NEW (cfg, load, OP_LOADR4_MEMBASE);
					isR4 = TRUE;
				} else {
					offsets = 8;
					MONO_INST_NEW (cfg, load, OP_LOADR8_MEMBASE);
				}
				load->dreg = mono_alloc_freg (cfg);
				load->inst_basereg = src->dreg;
				load->inst_offset = offsets;
				MONO_ADD_INS (cfg->cbb, load);
				add_outarg_reg (cfg, call, ArgInFReg, ainfo->freg+1, load, isR4, FALSE);
			}
			break;
		}
		case 0x2: {//all store in the reg
			MONO_INST_NEW (cfg, load, OP_LOADI8_MEMBASE);
			load->dreg = mono_alloc_ireg (cfg);
			load->inst_basereg = src->dreg;
			load->inst_offset = 0;
			MONO_ADD_INS (cfg->cbb, load);
			add_outarg_reg (cfg, call, ArgInIReg, ainfo->reg, load, isR4, FALSE);
			if (ainfo->size > 8) {
				MONO_INST_NEW (cfg, load, OP_LOADI8_MEMBASE);
				load->dreg = mono_alloc_ireg (cfg);
				load->inst_basereg = src->dreg;
				load->inst_offset = sizeof (target_mgreg_t);
				MONO_ADD_INS (cfg->cbb, load);
				add_outarg_reg (cfg, call, ArgInIReg, ainfo->reg + 1, load, isR4, FALSE);
			}
			break;
		}
		case 0x3: {//all store in stack
			for (int i = 0; i < ainfo->size / 8; ++i) {
				MONO_INST_NEW (cfg, load, OP_LOADI8_MEMBASE);
				load->dreg = mono_alloc_ireg (cfg);
				load->inst_basereg = src->dreg;
				load->inst_offset = i * 8;
				MONO_ADD_INS (cfg->cbb, load);
				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI8_MEMBASE_REG, loongarch_sp, ainfo->offset + (i * 8), load->dreg);
			}
			break;
		}
		case 0x4: {//one store in reg another in stack
			MONO_INST_NEW (cfg, load, OP_LOADI8_MEMBASE);
			load->dreg = mono_alloc_ireg (cfg);
			load->inst_basereg = src->dreg;
			load->inst_offset = 0;
			MONO_ADD_INS (cfg->cbb, load);
			add_outarg_reg (cfg, call, ArgInIReg, ainfo->reg, load, isR4, FALSE);
			MONO_INST_NEW (cfg, load, OP_LOADI8_MEMBASE);
			load->dreg = mono_alloc_ireg (cfg);
			load->inst_basereg = src->dreg;
			load->inst_offset = 8;
			MONO_ADD_INS (cfg->cbb, load);
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI8_MEMBASE_REG, loongarch_sp, ainfo->offset, load->dreg);
			break;
		}
		case 0x5: {//one store in reg another in freg
			if (field_info & 0x1) {
				if (fp1_size == 4) {
					MONO_INST_NEW (cfg, load, OP_LOADR4_MEMBASE);
					isR4 = TRUE;
				} else {
					MONO_INST_NEW (cfg, load, OP_LOADR8_MEMBASE);
				}
				load->dreg = mono_alloc_freg (cfg);
				load->inst_basereg = src->dreg;
				load->inst_offset = 0;
				MONO_ADD_INS (cfg->cbb, load);
				add_outarg_reg (cfg, call, ArgInFReg, ainfo->freg, load, isR4, FALSE);
				if (fp2_size == 4) {
					MONO_INST_NEW (cfg, load, OP_LOADI4_MEMBASE);
					offsets = fp1_size;
				} else {
					MONO_INST_NEW (cfg, load, OP_LOADI8_MEMBASE);
					offsets = 8;
				}
				load->dreg = mono_alloc_ireg (cfg);
				load->inst_basereg = src->dreg;
				load->inst_offset = offsets;
				MONO_ADD_INS (cfg->cbb, load);
				add_outarg_reg (cfg, call, ArgInIReg, ainfo->reg, load, isR4, FALSE);
			} else {
				if (fp1_size == 4)
					MONO_INST_NEW (cfg, load, OP_LOADI4_MEMBASE);
				else
					MONO_INST_NEW (cfg, load, OP_LOADI8_MEMBASE);
				load->dreg = mono_alloc_ireg (cfg);
				load->inst_basereg = src->dreg;
				load->inst_offset = 0;
				MONO_ADD_INS (cfg->cbb, load);
				add_outarg_reg (cfg, call, ArgInIReg, ainfo->reg, load, isR4, FALSE);
				if (fp2_size == 4) {
					MONO_INST_NEW (cfg, load, OP_LOADR4_MEMBASE);
					isR4 = TRUE;
					offsets = fp1_size;
				} else {
					MONO_INST_NEW (cfg, load, OP_LOADR8_MEMBASE);
					offsets = 8;
				}
				load->dreg = mono_alloc_freg (cfg);
				load->inst_basereg = src->dreg;
				load->inst_offset = offsets;
				MONO_ADD_INS (cfg->cbb, load);
				add_outarg_reg (cfg, call, ArgInFReg, ainfo->freg, load, isR4, FALSE);
			}
			break;
		}
		default:
			g_assert_not_reached ();
			break;
		}
		break;
	}
	case ArgStructByRef: {
		MonoInst *vtaddr, *load, *arg;
		/* Make a copy of the argument */
		vtaddr = mono_compile_create_var (cfg, m_class_get_byval_arg (ins->klass), OP_LOCAL);

		MONO_INST_NEW (cfg, load, OP_LDADDR);
		load->inst_p0 = vtaddr;
		vtaddr->flags |= MONO_INST_INDIRECT;
		load->type = STACK_MP;
		load->klass = vtaddr->klass;
		load->dreg = mono_alloc_ireg (cfg);
		MONO_ADD_INS (cfg->cbb, load);
		mini_emit_memcpy (cfg, load->dreg, 0, src->dreg, 0, ainfo->size, 8);
		if (((ainfo->field_info >> 5) & 0x7) == 3) {
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, loongarch_sp, ainfo->offset, load->dreg);
		} else {
			MONO_INST_NEW (cfg, arg, OP_MOVE);
			arg->dreg = mono_alloc_preg (cfg);
			arg->sreg1 = load->dreg;
			MONO_ADD_INS (cfg->cbb, arg);
			add_outarg_reg (cfg, call, ArgInIReg, ainfo->reg, arg, FALSE, FALSE);
		}
		break;
	}
	default:
		g_assert_not_reached ();
		break;
	}

}

gboolean
mono_arch_tailcall_supported (MonoCompile *cfg, MonoMethodSignature *caller_sig, MonoMethodSignature *callee_sig, gboolean virtual_)
{
	g_assert (caller_sig);
	g_assert (callee_sig);

	CallInfo *caller_info = get_call_info (NULL, caller_sig);
	CallInfo *callee_info = get_call_info (NULL, callee_sig);

	gboolean res = IS_SUPPORTED_TAILCALL (callee_info->stack_usage <= caller_info->stack_usage)
		  && IS_SUPPORTED_TAILCALL (caller_info->ret.storage == callee_info->ret.storage);

	// FIXME Limit stack_usage to 1G. emit_ld / st has 32bit limits.
	res &= IS_SUPPORTED_TAILCALL (callee_info->stack_usage < (1 << 30));
	res &= IS_SUPPORTED_TAILCALL (caller_info->stack_usage < (1 << 30));

	// valuetype parameters are the address of a local
	const ArgInfo *ainfo;
	ainfo = callee_info->args + callee_sig->hasthis;
	for (int i = 0; res && i < callee_sig->param_count; ++i) {
		res = IS_SUPPORTED_TAILCALL (ainfo [i].storage != ArgStructByRef);
	}

	g_free (caller_info);
	g_free (callee_info);

	return res;
}

void
mono_arch_emit_setret (MonoCompile *cfg, MonoMethod *method, MonoInst *val)
{
	MonoMethodSignature *sig;
	CallInfo *cinfo;

	sig = mono_method_signature_internal (cfg->method);
	if (!cfg->arch.cinfo)
		cfg->arch.cinfo = get_call_info (cfg->mempool, sig);
	cinfo = cfg->arch.cinfo;

	switch (cinfo->ret.storage) {
	case ArgNone:
		break;
	case ArgInIReg:
		MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, cfg->ret->dreg, val->dreg);
		break;
	case ArgInFReg:
		if (mini_get_underlying_type (sig->ret)->type == MONO_TYPE_R4) {
			if (cfg->r4fp)
				MONO_EMIT_NEW_UNALU (cfg, OP_RMOVE, cfg->ret->dreg, val->dreg);
			else
				MONO_EMIT_NEW_UNALU (cfg, OP_LA_SETFREG_R4, cfg->ret->dreg, val->dreg);
		} else {
			MONO_EMIT_NEW_UNALU (cfg, OP_FMOVE, cfg->ret->dreg, val->dreg);
		}
			break;
		break;
	default:
		g_assert_not_reached ();
		break;
	}
}

gboolean
mono_arch_is_inst_imm (int opcode, int imm_opcode, gint64 imm)
{
	return (imm >= -((gint64)1 << 31) && imm <= (((gint64)1 << 31)-1));
}

void
mono_arch_peephole_pass_2 (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins, *n, *last_ins = NULL;
	ins = bb->code;

	MONO_BB_FOR_EACH_INS_SAFE (bb, n, ins) {
		MonoInst *last_ins = mono_inst_prev (ins, FILTER_IL_SEQ_POINT);

		switch (ins->opcode) {
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
		case OP_LOADI4_MEMBASE:
			/*
			 * OP_STORE_MEMBASE_REG reg, offset(basereg)
			 * OP_LOAD_MEMBASE offset(basereg), reg
			 */
			if (last_ins && (last_ins->opcode == OP_STOREI4_MEMBASE_REG
					 || last_ins->opcode == OP_STORE_MEMBASE_REG) &&
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
				break;
			}
			/*
			 * Note: reg1 must be different from the basereg in the second load
			 * OP_LOAD_MEMBASE offset(basereg), reg1
			 * OP_LOAD_MEMBASE offset(basereg), reg2
			 * -->
			 * OP_LOAD_MEMBASE offset(basereg), reg1
			 * OP_MOVE reg1, reg2
			 */
			if (last_ins && (last_ins->opcode == OP_LOADI4_MEMBASE
					   || last_ins->opcode == OP_LOAD_MEMBASE) &&
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
				break;
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
		case OP_ICONV_TO_I4:
		case OP_ICONV_TO_U4:
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
mono_arch_decompose_long_opts (MonoCompile *cfg, MonoInst *ins)
{
}

#define ADD_NEW_INS(cfg,dest,op) do { 				 \
		MONO_INST_NEW ((cfg), (dest), (op)); 		\
		mono_bblock_insert_before_ins (bb, ins, (dest)); \
	} while (0)

#define NEW_INS(cfg,after,dest,op) do {					\
		MONO_INST_NEW((cfg), (dest), (op));			\
		mono_bblock_insert_after_ins (bb, (after), (dest));	\
	} while (0)

/*
 * Remove from the instruction list the instructions that can't be
 * represented with very simple instructions with no register
 * requirements.
 *
 * Converts complex opcodes into simpler ones so that each IR instruction
 * corresponds to one machine instruction.
 */
void
mono_arch_lowering_pass (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins, *next, *temp, *last_ins = NULL;

	MONO_BB_FOR_EACH_INS (bb, ins) {
loop_start:
		switch (ins->opcode) {
		case OP_COMPARE:
			next = ins->next;
			/* Branch opts can eliminate the branch */
			if (!next || (!(MONO_IS_COND_BRANCH_OP (next) || MONO_IS_COND_EXC (next) || MONO_IS_SETCC (next)))) {
				NULLIFY_INS (ins);
				break;
			}
			if (next->opcode == OP_IBEQ)
				next->opcode = OP_LBEQ;
			else if (next->opcode == OP_IBGE)
				next->opcode = OP_LBGE;
			else if (next->opcode == OP_IBGT)
				next->opcode = OP_LBGT;
			else if (next->opcode == OP_IBLE)
				next->opcode = OP_LBLE;
			else if (next->opcode == OP_IBLT)
				next->opcode = OP_LBLT;
			else if (next->opcode == OP_IBNE_UN)
				next->opcode = OP_LBNE_UN;
			else if (next->opcode == OP_IBGE_UN)
				next->opcode = OP_LBGE_UN;
			else if (next->opcode == OP_IBGT_UN)
				next->opcode = OP_LBGT_UN;
			else if (next->opcode == OP_IBLE_UN)
				next->opcode = OP_LBLE_UN;
			else if (next->opcode == OP_IBLT_UN)
				next->opcode = OP_LBLT_UN;
			else if (next->opcode == OP_ICEQ)
				next->opcode = OP_LCEQ;
			else if (next->opcode == OP_ICGT)
				next->opcode = OP_LCGT;
			else if (next->opcode == OP_ICGT_UN)
				next->opcode = OP_LCGT_UN;
			else if (next->opcode == OP_ICLT)
				next->opcode = OP_LCLT;
			else if (next->opcode == OP_ICLT_UN)
				next->opcode = OP_LCLT_UN;
			else if (next->opcode == OP_COND_EXC_IEQ)
				next->opcode = OP_COND_EXC_EQ;
			else if (next->opcode == OP_COND_EXC_IGE)
				next->opcode = OP_COND_EXC_GE;
			else if (next->opcode == OP_COND_EXC_IGT)
				next->opcode = OP_COND_EXC_GT;
			else if (next->opcode == OP_COND_EXC_ILE)
				next->opcode = OP_COND_EXC_LE;
			else if (next->opcode == OP_COND_EXC_ILT)
				next->opcode = OP_COND_EXC_LT;
			else if (next->opcode == OP_COND_EXC_INE_UN)
				next->opcode = OP_COND_EXC_NE_UN;
			else if (next->opcode == OP_COND_EXC_IGE_UN)
				next->opcode = OP_COND_EXC_GE_UN;
			else if (next->opcode == OP_COND_EXC_IGT_UN)
				next->opcode = OP_COND_EXC_GT_UN;
			else if (next->opcode == OP_COND_EXC_ILE_UN)
				next->opcode = OP_COND_EXC_LE_UN;
			else if (next->opcode == OP_COND_EXC_ILT_UN)
				next->opcode = OP_COND_EXC_LT_UN;
			else if (next->opcode == OP_COND_EXC_IOV)
				next->opcode = OP_COND_EXC_OV;
			else if (next->opcode == OP_COND_EXC_INO)
				next->opcode = OP_COND_EXC_NO;
			else if (next->opcode == OP_COND_EXC_IC)
				next->opcode = OP_COND_EXC_C;
			else if (next->opcode == OP_COND_EXC_INC)
				next->opcode = OP_COND_EXC_NC;
			break;

		case OP_ICOMPARE:
			next = ins->next;
			/* Branch opts can eliminate the branch */
			if (!next || (!(MONO_IS_COND_BRANCH_OP (next) || MONO_IS_COND_EXC (next) || MONO_IS_SETCC (next)))) {
				NULLIFY_INS (ins);
				break;
			}
			if (next->opcode == OP_LBEQ)
				next->opcode = OP_IBEQ;
			else if (next->opcode == OP_LBGE)
				next->opcode = OP_IBGE;
			else if (next->opcode == OP_LBGT)
				next->opcode = OP_IBGT;
			else if (next->opcode == OP_LBLE)
				next->opcode = OP_IBLE;
			else if (next->opcode == OP_LBLT)
				next->opcode = OP_IBLT;
			else if (next->opcode == OP_LBNE_UN)
				next->opcode = OP_IBNE_UN;
			else if (next->opcode == OP_LBGE_UN)
				next->opcode = OP_IBGE_UN;
			else if (next->opcode == OP_LBGT_UN)
				next->opcode = OP_IBGT_UN;
			else if (next->opcode == OP_LBLE_UN)
				next->opcode = OP_IBLE_UN;
			else if (next->opcode == OP_LBLT_UN)
				next->opcode = OP_IBLT_UN;
			else if (next->opcode == OP_LCEQ)
				next->opcode = OP_ICEQ;
			else if (next->opcode == OP_LCGT)
				next->opcode = OP_ICGT;
			else if (next->opcode == OP_LCGT_UN)
				next->opcode = OP_ICGT_UN;
			else if (next->opcode == OP_LCLT)
				next->opcode = OP_ICLT;
			else if (next->opcode == OP_LCLT_UN)
				next->opcode = OP_ICLT_UN;
			else if (next->opcode == OP_COND_EXC_EQ)
				next->opcode = OP_COND_EXC_IEQ;
			else if (next->opcode == OP_COND_EXC_GE)
				next->opcode = OP_COND_EXC_IGE;
			else if (next->opcode == OP_COND_EXC_GT)
				next->opcode = OP_COND_EXC_IGT;
			else if (next->opcode == OP_COND_EXC_LE)
				next->opcode = OP_COND_EXC_ILE;
			else if (next->opcode == OP_COND_EXC_LT)
				next->opcode = OP_COND_EXC_ILT;
			else if (next->opcode == OP_COND_EXC_NE_UN)
				next->opcode = OP_COND_EXC_INE_UN;
			else if (next->opcode == OP_COND_EXC_GE_UN)
				next->opcode = OP_COND_EXC_IGE_UN;
			else if (next->opcode == OP_COND_EXC_GT_UN)
				next->opcode = OP_COND_EXC_IGT_UN;
			else if (next->opcode == OP_COND_EXC_LE_UN)
				next->opcode = OP_COND_EXC_ILE_UN;
			else if (next->opcode == OP_COND_EXC_LT_UN)
				next->opcode = OP_COND_EXC_ILT_UN;
			else if (next->opcode == OP_COND_EXC_OV)
				next->opcode = OP_COND_EXC_IOV;
			else if (next->opcode == OP_COND_EXC_NO)
				next->opcode = OP_COND_EXC_INO;
			else if (next->opcode == OP_COND_EXC_C)
				next->opcode = OP_COND_EXC_IC;
			else if (next->opcode == OP_COND_EXC_NC)
				next->opcode = OP_COND_EXC_INC;
			break;
		case OP_LCOMPARE:
			next = ins->next;
			/* Branch opts can eliminate the branch */
			if (!next || (!(MONO_IS_COND_BRANCH_OP (next) || MONO_IS_COND_EXC (next) || MONO_IS_SETCC (next)))) {
				NULLIFY_INS (ins);
				break;
			}
			if (next->opcode == OP_IBEQ)
				next->opcode = OP_LBEQ;
			else if (next->opcode == OP_IBGE)
				next->opcode = OP_LBGE;
			else if (next->opcode == OP_IBGT)
				next->opcode = OP_LBGT;
			else if (next->opcode == OP_IBLE)
				next->opcode = OP_LBLE;
			else if (next->opcode == OP_IBLT)
				next->opcode = OP_LBLT;
			else if (next->opcode == OP_IBNE_UN)
				next->opcode = OP_LBNE_UN;
			else if (next->opcode == OP_IBGE_UN)
				next->opcode = OP_LBGE_UN;
			else if (next->opcode == OP_IBGT_UN)
				next->opcode = OP_LBGT_UN;
			else if (next->opcode == OP_IBLE_UN)
				next->opcode = OP_LBLE_UN;
			else if (next->opcode == OP_IBLT_UN)
				next->opcode = OP_LBLT_UN;
			else if (next->opcode == OP_ICEQ)
				next->opcode = OP_LCEQ;
			else if (next->opcode == OP_ICGT)
				next->opcode = OP_LCGT;
			else if (next->opcode == OP_ICGT_UN)
				next->opcode = OP_LCGT_UN;
			else if (next->opcode == OP_ICLT)
				next->opcode = OP_LCLT;
			else if (next->opcode == OP_ICLT_UN)
				next->opcode = OP_LCLT_UN;
			else if (next->opcode == OP_COND_EXC_IEQ)
				next->opcode = OP_COND_EXC_EQ;
			else if (next->opcode == OP_COND_EXC_IGE)
				next->opcode = OP_COND_EXC_GE;
			else if (next->opcode == OP_COND_EXC_IGT)
				next->opcode = OP_COND_EXC_GT;
			else if (next->opcode == OP_COND_EXC_ILE)
				next->opcode = OP_COND_EXC_LE;
			else if (next->opcode == OP_COND_EXC_ILT)
				next->opcode = OP_COND_EXC_LT;
			else if (next->opcode == OP_COND_EXC_INE_UN)
				next->opcode = OP_COND_EXC_NE_UN;
			else if (next->opcode == OP_COND_EXC_IGE_UN)
				next->opcode = OP_COND_EXC_GE_UN;
			else if (next->opcode == OP_COND_EXC_IGT_UN)
				next->opcode = OP_COND_EXC_GT_UN;
			else if (next->opcode == OP_COND_EXC_ILE_UN)
				next->opcode = OP_COND_EXC_LE_UN;
			else if (next->opcode == OP_COND_EXC_ILT_UN)
				next->opcode = OP_COND_EXC_LT_UN;
			else if (next->opcode == OP_COND_EXC_IOV)
				next->opcode = OP_COND_EXC_OV;
			else if (next->opcode == OP_COND_EXC_INO)
				next->opcode = OP_COND_EXC_NO;
			else if (next->opcode == OP_COND_EXC_IC)
				next->opcode = OP_COND_EXC_C;
			else if (next->opcode == OP_COND_EXC_INC)
				next->opcode = OP_COND_EXC_NC;
			break;

		case OP_COMPARE_IMM:
		case OP_ICOMPARE_IMM:
		case OP_LCOMPARE_IMM:
			next = ins->next;
			/* Branch opts can eliminate the branch */
			if (!next || (!(MONO_IS_COND_BRANCH_OP (next) || MONO_IS_COND_EXC (next) || MONO_IS_SETCC (next)))) {
				NULLIFY_INS (ins);
				break;
			}
			if (ins->inst_imm) {
				NEW_INS (cfg, last_ins, temp, OP_I8CONST);
				temp->inst_c0 = ins->inst_imm;
				temp->dreg = mono_alloc_ireg (cfg);
				ins->sreg2 = temp->dreg;
				last_ins = temp;
			}
			else {
				ins->sreg2 = loongarch_zero;
			}
			if (ins->opcode == OP_COMPARE_IMM)
				ins->opcode = OP_COMPARE;
			else if (ins->opcode == OP_ICOMPARE_IMM)
				ins->opcode = OP_ICOMPARE;
			else if (ins->opcode == OP_LCOMPARE_IMM)
				ins->opcode = OP_LCOMPARE;
			goto loop_start;

		case OP_IDIV_IMM:
		case OP_IREM_IMM:
		case OP_IDIV_UN_IMM:
		case OP_IREM_UN_IMM:
		case OP_LREM_IMM:
			mono_decompose_op_imm (cfg, bb, ins);
			break;
		case OP_LOCALLOC_IMM:
			if (ins->inst_imm > 32) {
				ADD_NEW_INS (cfg, temp, OP_ICONST);
				temp->inst_c0 = ins->inst_imm;
				temp->dreg = mono_alloc_ireg (cfg);
				ins->sreg1 = temp->dreg;
				ins->opcode = mono_op_imm_to_op (ins->opcode);
			}
			break;
		case OP_IBEQ:
			g_assert (ins_is_compare (last_ins));
			if (last_ins->sreg2 == loongarch_zero) {
				INS_REWRITE (ins, OP_LA_LBEQZ, last_ins->sreg1, -1);
			} else {
				INS_REWRITE (ins, OP_LA_IBEQ, last_ins->sreg1, last_ins->sreg2);
			}
			NULLIFY_INS (last_ins);
			break;

		case OP_IBNE_UN:
			g_assert (ins_is_compare (last_ins));
			if (last_ins->sreg2 == loongarch_zero) {
				INS_REWRITE (ins, OP_LA_LBNEZ, last_ins->sreg1, -1);
			} else {
				INS_REWRITE (ins, OP_LA_IBNE_UN, last_ins->sreg1, last_ins->sreg2);
			}
			NULLIFY_INS (last_ins);
			break;

		case OP_IBGE:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_IBGE, last_ins->sreg1, last_ins->sreg2);
			NULLIFY_INS (last_ins);
			break;

		case OP_IBGE_UN:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_IBGE_UN, last_ins->sreg1, last_ins->sreg2);
			NULLIFY_INS (last_ins);
			break;

		case OP_IBLT:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_IBLT, last_ins->sreg1, last_ins->sreg2);
			NULLIFY_INS (last_ins);
			break;

		case OP_IBLT_UN:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_IBLT_UN, last_ins->sreg1, last_ins->sreg2);
			NULLIFY_INS (last_ins);
			break;

		case OP_IBLE:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_IBLE, last_ins->sreg1, last_ins->sreg2);
			NULLIFY_INS (last_ins);
			break;

		case OP_IBLE_UN:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_IBLE_UN, last_ins->sreg1, last_ins->sreg2);
			NULLIFY_INS (last_ins);
			break;

		case OP_IBGT:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_IBGT, last_ins->sreg1, last_ins->sreg2);
			NULLIFY_INS (last_ins);
			break;

		case OP_IBGT_UN:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_IBGT_UN, last_ins->sreg1, last_ins->sreg2);
			NULLIFY_INS (last_ins);
			break;

		case OP_LBEQ:
			g_assert (ins_is_compare (last_ins));
			if (last_ins->sreg2 == loongarch_zero) {
				INS_REWRITE (ins, OP_LA_LBEQZ, last_ins->sreg1, -1);
			} else {
				INS_REWRITE (ins, OP_LA_LBEQ, last_ins->sreg1, last_ins->sreg2);
			}
			NULLIFY_INS (last_ins);
			break;

		case OP_LBNE_UN:
			g_assert (ins_is_compare (last_ins));
			if (last_ins->sreg2 == loongarch_zero) {
				INS_REWRITE (ins, OP_LA_LBNEZ, last_ins->sreg1, -1);
			} else {
				INS_REWRITE (ins, OP_LA_LBNE_UN, last_ins->sreg1, last_ins->sreg2);
			}
			NULLIFY_INS (last_ins);
			break;

		case OP_LBGE:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_LBGE, last_ins->sreg1, last_ins->sreg2);
			NULLIFY_INS (last_ins);
			break;

		case OP_LBGE_UN:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_LBGE_UN, last_ins->sreg1, last_ins->sreg2);
			NULLIFY_INS (last_ins);
			break;

		case OP_LBLT:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_LBLT, last_ins->sreg1, last_ins->sreg2);
			NULLIFY_INS (last_ins);
			break;

		case OP_LBLT_UN:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_LBLT_UN, last_ins->sreg1, last_ins->sreg2);
			NULLIFY_INS (last_ins);
			break;

		case OP_LBLE:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_LBLE, last_ins->sreg1, last_ins->sreg2);
			NULLIFY_INS (last_ins);
			break;

		case OP_LBLE_UN:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_LBLE_UN, last_ins->sreg1, last_ins->sreg2);
			NULLIFY_INS (last_ins);
			break;

		case OP_LBGT:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_LBGT, last_ins->sreg1, last_ins->sreg2);
			NULLIFY_INS (last_ins);
			break;

		case OP_LBGT_UN:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_LBGT_UN, last_ins->sreg1, last_ins->sreg2);
			NULLIFY_INS (last_ins);
			break;

		case OP_CEQ:
		case OP_ICEQ:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_ICEQ, last_ins->sreg1, last_ins->sreg2);
			NULLIFY_INS (last_ins);
			break;

		case OP_CLT:
		case OP_ICLT:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_ICLT, last_ins->sreg1, last_ins->sreg2);
			NULLIFY_INS (last_ins);
			break;

		case OP_CLT_UN:
		case OP_ICLT_UN:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_ICLT_UN, last_ins->sreg1, last_ins->sreg2);
			NULLIFY_INS (last_ins);
			break;

		case OP_CGT:
		case OP_ICGT:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_ICGT, last_ins->sreg1, last_ins->sreg2);
			NULLIFY_INS (last_ins);
			break;

		case OP_CGT_UN:
		case OP_ICGT_UN:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_ICGT_UN, last_ins->sreg1, last_ins->sreg2);
			NULLIFY_INS (last_ins);
			break;

		case OP_ICNEQ:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_ICNEQ, last_ins->sreg1, last_ins->sreg2);
			NULLIFY_INS (last_ins);
			break;

		case OP_ICGE:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_ICGE, last_ins->sreg1, last_ins->sreg2);
			NULLIFY_INS (last_ins);
			break;

		case OP_ICGE_UN:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_ICGE_UN, last_ins->sreg1, last_ins->sreg2);
			NULLIFY_INS (last_ins);
			break;

		case OP_ICLE:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_ICLE, last_ins->sreg1, last_ins->sreg2);
			NULLIFY_INS (last_ins);
			break;

		case OP_ICLE_UN:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_ICLE_UN, last_ins->sreg1, last_ins->sreg2);
			NULLIFY_INS (last_ins);
			break;

		case OP_LCEQ:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_LCEQ, last_ins->sreg1, last_ins->sreg2);
			NULLIFY_INS (last_ins);
			break;

		case OP_LCLT:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_LCLT, last_ins->sreg1, last_ins->sreg2);
			NULLIFY_INS (last_ins);
			break;

		case OP_LCLT_UN:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_LCLT_UN, last_ins->sreg1, last_ins->sreg2);
			NULLIFY_INS (last_ins);
			break;

		case OP_LCGT:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_LCGT, last_ins->sreg1, last_ins->sreg2);
			NULLIFY_INS (last_ins);
			break;

		case OP_LCGT_UN:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_LCGT_UN, last_ins->sreg1, last_ins->sreg2);
			NULLIFY_INS (last_ins);
			break;

		case OP_COND_EXC_EQ:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_COND_EXC_EQ, last_ins->sreg1, last_ins->sreg2);
			MONO_DELETE_INS (bb, last_ins);
			break;

		case OP_COND_EXC_GE:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_COND_EXC_GE, last_ins->sreg1, last_ins->sreg2);
			MONO_DELETE_INS (bb, last_ins);
			break;

		case OP_COND_EXC_GT:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_COND_EXC_GT, last_ins->sreg1, last_ins->sreg2);
			MONO_DELETE_INS (bb, last_ins);
			break;

		case OP_COND_EXC_LE:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_COND_EXC_LE, last_ins->sreg1, last_ins->sreg2);
			MONO_DELETE_INS (bb, last_ins);
			break;

		case OP_COND_EXC_LT:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_COND_EXC_LT, last_ins->sreg1, last_ins->sreg2);
			MONO_DELETE_INS (bb, last_ins);
			break;

		case OP_COND_EXC_NE_UN:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_COND_EXC_NE_UN, last_ins->sreg1, last_ins->sreg2);
			MONO_DELETE_INS (bb, last_ins);
			break;

		case OP_COND_EXC_GE_UN:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_COND_EXC_GE_UN, last_ins->sreg1, last_ins->sreg2);
			MONO_DELETE_INS (bb, last_ins);
			break;

		case OP_COND_EXC_GT_UN:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_COND_EXC_GT_UN, last_ins->sreg1, last_ins->sreg2);
			MONO_DELETE_INS (bb, last_ins);
			break;

		case OP_COND_EXC_LE_UN:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_COND_EXC_LE_UN, last_ins->sreg1, last_ins->sreg2);
			MONO_DELETE_INS (bb, last_ins);
			break;

		case OP_COND_EXC_LT_UN:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_COND_EXC_LT_UN, last_ins->sreg1, last_ins->sreg2);
			MONO_DELETE_INS (bb, last_ins);
			break;

		case OP_COND_EXC_OV:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_COND_EXC_OV, last_ins->sreg1, last_ins->sreg2);
			MONO_DELETE_INS (bb, last_ins);
			break;

		case OP_COND_EXC_NO:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_COND_EXC_NO, last_ins->sreg1, last_ins->sreg2);
			MONO_DELETE_INS (bb, last_ins);
			break;

		case OP_COND_EXC_C:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_COND_EXC_C, last_ins->sreg1, last_ins->sreg2);
			MONO_DELETE_INS (bb, last_ins);
			break;

		case OP_COND_EXC_NC:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_COND_EXC_NC, last_ins->sreg1, last_ins->sreg2);
			MONO_DELETE_INS (bb, last_ins);
			break;

		case OP_COND_EXC_IEQ:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_COND_EXC_IEQ, last_ins->sreg1, last_ins->sreg2);
			MONO_DELETE_INS (bb, last_ins);
			break;

		case OP_COND_EXC_IGE:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_COND_EXC_IGE, last_ins->sreg1, last_ins->sreg2);
			MONO_DELETE_INS (bb, last_ins);
			break;

		case OP_COND_EXC_IGT:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_COND_EXC_IGT, last_ins->sreg1, last_ins->sreg2);
			MONO_DELETE_INS (bb, last_ins);
			break;

		case OP_COND_EXC_ILE:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_COND_EXC_ILE, last_ins->sreg1, last_ins->sreg2);
			MONO_DELETE_INS (bb, last_ins);
			break;

		case OP_COND_EXC_ILT:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_COND_EXC_ILT, last_ins->sreg1, last_ins->sreg2);
			MONO_DELETE_INS (bb, last_ins);
			break;

		case OP_COND_EXC_INE_UN:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_COND_EXC_INE_UN, last_ins->sreg1, last_ins->sreg2);
			MONO_DELETE_INS (bb, last_ins);
			break;

		case OP_COND_EXC_IGE_UN:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_COND_EXC_IGE_UN, last_ins->sreg1, last_ins->sreg2);
			MONO_DELETE_INS (bb, last_ins);
			break;

		case OP_COND_EXC_IGT_UN:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_COND_EXC_IGT_UN, last_ins->sreg1, last_ins->sreg2);
			MONO_DELETE_INS (bb, last_ins);
			break;

		case OP_COND_EXC_ILE_UN:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_COND_EXC_ILE_UN, last_ins->sreg1, last_ins->sreg2);
			MONO_DELETE_INS (bb, last_ins);
			break;

		case OP_COND_EXC_ILT_UN:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_COND_EXC_ILT_UN, last_ins->sreg1, last_ins->sreg2);
			MONO_DELETE_INS (bb, last_ins);
			break;

		case OP_COND_EXC_IOV:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_COND_EXC_IOV, last_ins->sreg1, last_ins->sreg2);
			MONO_DELETE_INS (bb, last_ins);
			break;

		case OP_COND_EXC_INO:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_COND_EXC_INO, last_ins->sreg1, last_ins->sreg2);
			MONO_DELETE_INS (bb, last_ins);
			break;

		case OP_COND_EXC_IC:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_COND_EXC_IC, last_ins->sreg1, last_ins->sreg2);
			MONO_DELETE_INS (bb, last_ins);
			break;

		case OP_COND_EXC_INC:
			g_assert (ins_is_compare (last_ins));
			INS_REWRITE (ins, OP_LA_COND_EXC_INC, last_ins->sreg1, last_ins->sreg2);
			MONO_DELETE_INS (bb, last_ins);
			break;

		case OP_FCOMPARE:
		case OP_RCOMPARE:
			next = ins->next;
			/* Branch opts can eliminate the branch */
			if (!next || (!(MONO_IS_COND_BRANCH_OP (next) || MONO_IS_COND_EXC (next) || MONO_IS_SETCC (next)))) {
				NULLIFY_INS (ins);
				break;
			}
			ins->dreg = mono_alloc_ireg (cfg);
			break;

		case OP_FBEQ:
		case OP_RBEQ:
			g_assert (ins_is_compare (last_ins));
			if (last_ins->opcode == OP_FCOMPARE)
				last_ins->opcode = OP_FCEQ;
			else
				last_ins->opcode = OP_RCEQ;
			break;

		case OP_FBGE:
		case OP_RBGE:
			g_assert (ins_is_compare (last_ins));
			if (last_ins->opcode == OP_FCOMPARE)
				last_ins->opcode = OP_FCGE;
			else
				last_ins->opcode = OP_RCGE;
			break;

		case OP_FBGT:
		case OP_RBGT:
			g_assert (ins_is_compare (last_ins));
			if (last_ins->opcode == OP_FCOMPARE)
				last_ins->opcode = OP_FCGT;
			else
				last_ins->opcode = OP_RCGT;
			break;

		case OP_FBLE:
		case OP_RBLE:
			g_assert (ins_is_compare (last_ins));
			if (last_ins->opcode == OP_FCOMPARE)
				last_ins->opcode = OP_FCLE;
			else
				last_ins->opcode = OP_RCLE;
			break;

		case OP_FBLT:
		case OP_RBLT:
			g_assert (ins_is_compare (last_ins));
			if (last_ins->opcode == OP_FCOMPARE)
				last_ins->opcode = OP_FCLT;
			else
				last_ins->opcode = OP_RCLT;
			break;

		case OP_FBNE_UN:
		case OP_RBNE_UN:
			g_assert (ins_is_compare (last_ins));
			if (last_ins->opcode == OP_FCOMPARE)
				last_ins->opcode = OP_LA_FCNEQ_UN;
			else
				last_ins->opcode = OP_LA_RCNEQ_UN;
			break;

		case OP_FBGE_UN:
		case OP_RBGE_UN:
			g_assert (ins_is_compare (last_ins));
			if (last_ins->opcode == OP_FCOMPARE)
				last_ins->opcode = OP_LA_FCGE_UN;
			else
				last_ins->opcode = OP_LA_RCGE_UN;
			break;

		case OP_FBGT_UN:
		case OP_RBGT_UN:
			g_assert (ins_is_compare (last_ins));
			if (last_ins->opcode == OP_FCOMPARE)
				last_ins->opcode = OP_FCGT_UN;
			else
				last_ins->opcode = OP_RCGT_UN;
			break;

		case OP_FBLE_UN:
		case OP_RBLE_UN:
			g_assert (ins_is_compare (last_ins));
			if (last_ins->opcode == OP_FCOMPARE)
				last_ins->opcode = OP_LA_FCLE_UN;
			else
				last_ins->opcode = OP_LA_RCLE_UN;
			break;

		case OP_FBLT_UN:
		case OP_RBLT_UN:
			g_assert (ins_is_compare (last_ins));
			if (last_ins->opcode == OP_FCOMPARE)
				last_ins->opcode = OP_FCLT_UN;
			else
				last_ins->opcode = OP_RCLT_UN;
			break;

		case OP_RCNEQ:
			ins->opcode == OP_LA_RCNEQ_UN;
			break;
		default:
			break;
		}

		last_ins = ins;
	}
	bb->last_ins = last_ins;
	bb->max_vreg = cfg->next_vreg;
}

static int
opcode_to_loongarchcond (int opcode, gint *src1, gint *src2)
{
	gint swp;
	switch (opcode) {
	case OP_LA_IBEQ:
	case OP_LA_LBEQ:
	case OP_FBEQ:
	case OP_LA_COND_EXC_EQ:
	case OP_LA_COND_EXC_IEQ:
		return 0x58000000;
	case OP_LA_IBGE:
	case OP_LA_LBGE:
	case OP_FBGE:
	case OP_LA_COND_EXC_IGE:
	case OP_LA_COND_EXC_GE:
		return 0x64000000;
	case OP_LA_IBGT:
	case OP_LA_LBGT:
	case OP_FBGT:
	case OP_LA_COND_EXC_IGT:
	case OP_LA_COND_EXC_GT:
		swp = *src1;
		*src1 = *src2;
		*src2 = swp;
		return 0x60000000;
	case OP_LA_IBLE:
	case OP_LA_LBLE:
	case OP_FBLE:
	case OP_LA_COND_EXC_ILE:
	case OP_LA_COND_EXC_LE:
		swp = *src1;
		*src1 = *src2;
		*src2 = swp;
		return 0x64000000;
	case OP_LA_IBLT:
	case OP_LA_LBLT:
	case OP_FBLT:
	case OP_LA_COND_EXC_ILT:
	case OP_LA_COND_EXC_LT:
		return 0x60000000;
	case OP_LA_IBNE_UN:
	case OP_LA_LBNE_UN:
	case OP_FBNE_UN:
	case OP_LA_COND_EXC_NE_UN:
	case OP_LA_COND_EXC_INE_UN:
		return 0x5c000000;
	case OP_LA_IBGE_UN:
	case OP_LA_LBGE_UN:
	case OP_FBGE_UN:
	case OP_LA_COND_EXC_IGE_UN:
	case OP_LA_COND_EXC_GE_UN:
	case OP_LA_COND_EXC_C:
	case OP_LA_COND_EXC_IC:
		return 0x6c000000;
	case OP_LA_IBGT_UN:
	case OP_LA_LBGT_UN:
	case OP_FBGT_UN:
	case OP_LA_COND_EXC_IGT_UN:
	case OP_LA_COND_EXC_GT_UN:
		swp = *src1;
		*src1 = *src2;
		*src2 = swp;
		return 0x68000000;
	case OP_LA_IBLE_UN:
	case OP_LA_LBLE_UN:
	case OP_LA_COND_EXC_ILE_UN:
	case OP_LA_COND_EXC_LE_UN:
		swp = *src1;
		*src1 = *src2;
		*src2 = swp;
		return 0x6c000000;
	case OP_LA_IBLT_UN:
	case OP_LA_LBLT_UN:
	case OP_LA_COND_EXC_ILT_UN:
	case OP_LA_COND_EXC_LT_UN:
	case OP_LA_COND_EXC_NC:
	case OP_LA_COND_EXC_INC:
		return 0x68000000;
	default:
		printf ("%s\n", mono_inst_name (opcode));
		g_assert_not_reached ();
		return -1;
	}
}

static guint8*
emit_move_return_value (MonoCompile *cfg, guint8 * code, MonoInst *ins)
{
	CallInfo *cinfo;
	MonoCallInst *call;

	call = (MonoCallInst*)ins;
	cinfo = call->call_info;
	g_assert (cinfo);
	switch (cinfo->ret.storage) {
	case ArgNone:
		break;
	case ArgInIReg:
		/* LLVM compiled code might only set the bottom bits */
		if (call->signature && mini_get_underlying_type (call->signature->ret)->type == MONO_TYPE_I4)
			loongarch_sllw (code, call->inst.dreg, cinfo->ret.reg, loongarch_zero);
		else if (call->inst.dreg != cinfo->ret.reg)
			loongarch_ori (code, call->inst.dreg, cinfo->ret.reg, 0);
		break;
	case ArgInFReg:
		if (mini_get_underlying_type (call->signature->ret)->type == MONO_TYPE_R4) {
			if (cfg->r4fp)
				loongarch_fmovs (code, call->inst.dreg, cinfo->ret.reg);
			else
				loongarch_fcvtds (code, call->inst.dreg, cinfo->ret.reg);
		} else {
			if (call->inst.dreg != cinfo->ret.reg)
				loongarch_fmovd (code, call->inst.dreg, cinfo->ret.reg);
		}
		break;
	case ArgStructByVal: {
		MonoInst *loc = cfg->arch.vret_addr_loc;
		int i;

		/* Load the destination address */
		g_assert (loc && loc->opcode == OP_REGOFFSET);
		code = emit_ldd (code, loongarch_ra, loc->inst_basereg, loc->inst_offset);
		int field_info = cinfo->ret.field_info;
		int offsets;

		if (field_info & 0x1f) {
			if (field_info & 1) {
				g_assert (cinfo->ret.freg == loongarch_f0);
				if (field_info  & 2) {
					loongarch_fstd (code, loongarch_f0 , loongarch_ra, 0);
					offsets = 8;
				} else {
					loongarch_fsts (code, loongarch_f0 , loongarch_ra, 0);
					offsets = 4;
				}
			} else {
				g_assert (cinfo->ret.reg == loongarch_a0);
				if (field_info & 2) {
					loongarch_std (code, loongarch_a0, loongarch_ra, 0);
					offsets = 8;
				} else {
					loongarch_stw (code, loongarch_a0, loongarch_ra, 0);
					offsets = 4;
				}
			}

			if (field_info & 4) {
				if (field_info & 8) {
					i = field_info & 1 ? loongarch_f1 : loongarch_f0;
					if (field_info & 0x10) {
						loongarch_fstd (code, i, loongarch_ra, 8);
					} else {
						loongarch_fsts (code, i, loongarch_ra, offsets & 0xfff);
					}
				} else {
					i = field_info & 1 ? loongarch_a0 : loongarch_a1;
					if (field_info  & 0x10) {
						loongarch_std (code, i, loongarch_ra, 8);
					} else {
						loongarch_stw (code, i, loongarch_ra, offsets & 0xfff);
					}
				}
			}
		} else {
			if (cinfo->ret.size > 8) {
				loongarch_std (code, loongarch_a0, loongarch_ra, 0);
				loongarch_std (code, loongarch_a1, loongarch_ra, 8);
			} else {
				loongarch_std (code, loongarch_a0, loongarch_ra, 0);
			}
		}
		break;
	}
	case ArgStructByRef:
		loongarch_ldd (code, cinfo->ret.reg, loongarch_sp, cinfo->ret.offset);
		break;
	default:
		g_assert_not_reached ();
		break;
	}
	return code;
}

static guint8*
emit_call (MonoCompile *cfg, guint8* code, MonoJumpInfoType patch_type, gconstpointer data)
{
	mono_add_patch_info_rel (cfg, code - cfg->native_code, patch_type, data, MONO_R_LOONGARCH64_BL);
	loongarch_bl (code, 0);
	cfg->thunk_area += THUNK_SIZE;
	return code;
}

/*
 * emit_branch_island:
 *
 *   Emit a branch island for the conditional branches from cfg->native_code + start_offset to code.
 */
static guint8*
emit_branch_island (MonoCompile *cfg, guint8 *code, int start_offset)
{
	MonoJumpInfo *ji;

	/* Iterate over the patch infos added so far by this bb */
	int island_size = 0;
	for (ji = cfg->patch_info; ji; ji = ji->next) {
		if (ji->ip.i < start_offset)
			/* The patch infos are in reverse order, so this means the end */
			break;
		if ((cfg->arch.cond_branch_islands == 1 && ji->relocation == MONO_R_LOONGARCH64_BC) || (cfg->arch.cond_branch_islands == 2 && (ji->relocation == MONO_R_LOONGARCH64_BC || ji->relocation == MONO_R_LOONGARCH64_BZ))) {
			island_size += 4;
		}
	}

	if (island_size) {
		code = realloc_code (cfg, island_size);

		/* Branch over the island */
		loongarch_b (code, 1 + island_size/4);

		for (ji = cfg->patch_info; ji; ji = ji->next) {
			if (ji->ip.i < start_offset)
				break;
			if ((cfg->arch.cond_branch_islands == 1 && ji->relocation == MONO_R_LOONGARCH64_BC) || (cfg->arch.cond_branch_islands == 2 && (ji->relocation == MONO_R_LOONGARCH64_BC || ji->relocation == MONO_R_LOONGARCH64_BZ))) {
				/* Rewrite the cond branch so it branches to an unconditional branch in the branch island */
				mono_loongarch64_patch (cfg->native_code + ji->ip.i, code, ji->relocation);
				/* Rewrite the patch so it points to the unconditional branch */
				ji->ip.i = code - cfg->native_code;
				ji->relocation = MONO_R_LOONGARCH64_B;
				loongarch_b (code, 0);
			}
		}
		set_code_cursor (cfg, code);
	}
	return code;
}

void
mono_arch_output_basic_block (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins;
	MonoCallInst *call;
	guint8 *code = cfg->native_code + cfg->code_len;
	MonoInst *last_ins = NULL;
	int start_offset, max_len, cpos;
	int ins_cnt = 0;

	/* we don't align basic blocks of loops on loongarch64 */

	if (cfg->verbose_level > 2)
		g_print ("Basic block %d starting at offset 0x%x\n", bb->block_num, bb->native_offset);

	cpos = bb->max_offset;
	start_offset = code - cfg->native_code;
	g_assert (start_offset <= cfg->code_size);

	MONO_BB_FOR_EACH_INS (bb, ins) {
		guint offset = code - cfg->native_code;
		set_code_cursor (cfg, code);
		max_len = ins_get_size (ins->opcode);
		code = realloc_code (cfg, max_len);

		if (G_UNLIKELY (cfg->arch.cond_branch_islands > 0 && offset - start_offset > 4 * 0x3fff)) {
			/* Emit a branch island for large basic blocks */
			code = emit_branch_island (cfg, code, start_offset);
			offset = code - cfg->native_code;
			start_offset = offset;
		}

		mono_debug_record_line_number (cfg, ins, offset);
		if (cfg->verbose_level > 2) {
			g_print ("    @ 0x%x\t", offset);
			mono_print_ins_index (ins_cnt++, ins);
		}
		/* Check for virtual regs that snuck by */
		g_assert ((ins->dreg >= -1) && (ins->dreg < 32));

		switch (ins->opcode) {
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
			MonoInst *info_var = cfg->arch.seq_point_info_var;

			/*
			 * For AOT, we use one got slot per method, which will point to a
			 * SeqPointInfo structure, containing all the information required
			 * by the code below.
			 */
			if (cfg->compile_aot) {
				g_assert (info_var);
				g_assert (info_var->opcode == OP_REGOFFSET);
			}

			if (ins->flags & MONO_INST_SINGLE_STEP_LOC) {
				MonoInst *var = cfg->arch.ss_tramp_var;

				g_assert (var);
				g_assert (var->opcode == OP_REGOFFSET);
				/* Load ss_tramp_var */
				/* This is equal to &ss_trampoline */
				loongarch_ldd (code, loongarch_r21, var->inst_basereg, var->inst_offset);
				/* Load the trampoline address */
				loongarch_ldd (code, loongarch_r21, loongarch_r21, 0);
				/* Call it if it is non-null */
				loongarch_beqz (code, loongarch_r21, 2);
				loongarch_jirl (code, 1, loongarch_r21, 0);
			}

			mono_add_seq_point (cfg, bb, ins, code - cfg->native_code);

			if (cfg->compile_aot) {
				const guint32 offset = GPTRDIFF_TO_UINT32 (code - cfg->native_code);
				guint32 val;

				loongarch_ldd (code, loongarch_r21, info_var->inst_basereg, info_var->inst_offset);
				/* Add the offset */
				val = ((offset / 4) * sizeof (target_mgreg_t)) + MONO_STRUCT_OFFSET (SeqPointInfo, bp_addrs);
				/* Load the info->bp_addrs [offset], which is either 0 or the address of the bp trampoline */
				code = emit_ldd (code, loongarch_r21, loongarch_r21, val);
				/* Skip the load if its 0 */
				loongarch_beqz (code, loongarch_r21, 2);
				/* Call the breakpoint trampoline */
				loongarch_jirl (code, 1, loongarch_r21, 0);
			} else {
				MonoInst *var = cfg->arch.bp_tramp_var;

				g_assert (var);
				g_assert (var->opcode == OP_REGOFFSET);
				/* Load the address of the bp trampoline into IP0 */
				loongarch_ldd (code, loongarch_t0, var->inst_basereg, var->inst_offset);
				/*
				 * A placeholder for a possible breakpoint inserted by
				 * mono_arch_set_breakpoint ().
				 */
				loongarch_nop (code);
			}

			break;
		}
		case OP_BIGMUL:
			loongarch_muld (code, ins->dreg, ins->sreg1, ins->sreg2);
			loongarch_mulhd (code, ins->dreg+1, ins->sreg1, ins->sreg2);
			break;
		case OP_BIGMUL_UN:
			loongarch_muld (code, ins->dreg, ins->sreg1, ins->sreg2);
			loongarch_mulhdu (code, ins->dreg+1, ins->sreg1, ins->sreg2);
			break;
		case OP_MEMORY_BARRIER:
			loongarch_dbar (code, 0);
			break;
		case OP_STOREI1_MEMBASE_IMM:
			loongarch_load_const (code, loongarch_temp, ins->inst_imm);
			if (loongarch_is_imm12 (ins->inst_offset)) {
				loongarch_stb (code, loongarch_temp, ins->inst_destbasereg, ins->inst_offset & 0xfff);
			} else {
				loongarch_lu12iw (code, loongarch_r21, (ins->inst_offset >> 12) & 0xfffff);
				loongarch_ori (code, loongarch_r21, loongarch_r21, ins->inst_offset & 0xfff);
				loongarch_stxb (code, loongarch_temp, loongarch_r21, ins->inst_destbasereg);
			}
			break;
		case OP_STOREI2_MEMBASE_IMM:
			loongarch_load_const (code, loongarch_temp, ins->inst_imm);
			if (loongarch_is_imm12 (ins->inst_offset)) {
				loongarch_sth (code, loongarch_temp, ins->inst_destbasereg, ins->inst_offset & 0xfff);
			} else {
				loongarch_lu12iw (code, loongarch_r21, (ins->inst_offset >> 12) & 0xfffff);
				loongarch_ori (code, loongarch_r21, loongarch_r21, ins->inst_offset & 0xfff);
				loongarch_stxh (code, loongarch_temp, loongarch_r21, ins->inst_destbasereg);
			}
			break;
		case OP_STORE_MEMBASE_IMM:
		case OP_STOREI8_MEMBASE_IMM:
			loongarch_load_const (code, loongarch_temp, ins->inst_imm);
			if (loongarch_is_imm12 (ins->inst_offset)) {
				loongarch_std (code, loongarch_temp, ins->inst_destbasereg, ins->inst_offset & 0xfff);
			} else {
				loongarch_lu12iw (code, loongarch_r21, (ins->inst_offset >> 12) & 0xfffff);
				loongarch_ori (code, loongarch_r21, loongarch_r21, ins->inst_offset & 0xfff);
				loongarch_stxd (code, loongarch_temp, loongarch_r21, ins->inst_destbasereg);
			}
			break;
		case OP_STOREI4_MEMBASE_IMM:
			loongarch_load_const (code, loongarch_temp, ins->inst_imm);
			if (loongarch_is_imm12 (ins->inst_offset)) {
				loongarch_stw (code, loongarch_temp, ins->inst_destbasereg, ins->inst_offset & 0xfff);
			} else {
				loongarch_lu12iw (code, loongarch_r21, (ins->inst_offset >> 12) & 0xfffff);
				loongarch_ori (code, loongarch_r21, loongarch_r21, ins->inst_offset & 0xfff);
				loongarch_stxw (code, loongarch_temp, loongarch_r21, ins->inst_destbasereg);
			}
			break;
		case OP_STOREI1_MEMBASE_REG:
			if (loongarch_is_imm12 (ins->inst_offset)) {
				loongarch_stb (code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset & 0xfff);
			} else {
				loongarch_lu12iw (code, loongarch_r21, (ins->inst_offset >> 12) & 0xfffff);
				loongarch_ori (code, loongarch_r21, loongarch_r21, ins->inst_offset & 0xfff);
				loongarch_stxb (code, ins->sreg1, loongarch_r21, ins->inst_destbasereg);
			}
			break;
		case OP_STOREI2_MEMBASE_REG:
			if (loongarch_is_imm12 (ins->inst_offset)) {
				loongarch_sth (code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset & 0xfff);
			} else {
				loongarch_lu12iw (code, loongarch_r21, (ins->inst_offset >> 12) & 0xfffff);
				loongarch_ori (code, loongarch_r21, loongarch_r21, ins->inst_offset & 0xfff);
				loongarch_stxh (code, ins->sreg1, loongarch_r21, ins->inst_destbasereg);
			}
			break;
		case OP_STOREI4_MEMBASE_REG:
			if (loongarch_is_imm12 (ins->inst_offset)) {
				loongarch_stw (code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset & 0xfff);
			} else {
				loongarch_lu12iw (code, loongarch_r21, (ins->inst_offset >> 12) & 0xfffff);
				loongarch_ori (code, loongarch_r21, loongarch_r21, ins->inst_offset & 0xfff);
				loongarch_stxw (code, ins->sreg1, loongarch_r21, ins->inst_destbasereg);
			}
			break;
		case OP_STORE_MEMBASE_REG:
		case OP_STOREI8_MEMBASE_REG:
			if (loongarch_is_imm12 (ins->inst_offset)) {
				loongarch_std (code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset & 0xfff);
			} else {
				loongarch_lu12iw (code, loongarch_r21, (ins->inst_offset >> 12) & 0xfffff);
				loongarch_ori (code, loongarch_r21, loongarch_r21, ins->inst_offset & 0xfff);
				loongarch_stxd (code, ins->sreg1, loongarch_r21, ins->inst_destbasereg);
			}
			break;
		case OP_LOADU4_MEM:
			loongarch_load_const (code, loongarch_temp, ins->inst_imm);
			loongarch_ldwu (code, ins->dreg, loongarch_temp, 0);
			break;
		case OP_LOAD_MEMBASE:
		case OP_LOADI8_MEMBASE:
			if (loongarch_is_imm12 (ins->inst_offset)) {
				loongarch_ldd (code, ins->dreg, ins->inst_basereg, ins->inst_offset & 0xfff);
			} else {
				loongarch_lu12iw (code, loongarch_r21, (ins->inst_offset >> 12) & 0xfffff);
				loongarch_ori (code, loongarch_r21, loongarch_r21, ins->inst_offset & 0xfff);
				loongarch_ldxd (code, ins->dreg, loongarch_r21, ins->inst_basereg);
			}
			break;
		case OP_LOADI4_MEMBASE:
			g_assert (ins->dreg != -1);
			if (loongarch_is_imm12 (ins->inst_offset)) {
				loongarch_ldw (code, ins->dreg, ins->inst_basereg, ins->inst_offset & 0xfff);
			} else {
				loongarch_lu12iw (code, loongarch_r21, (ins->inst_offset >> 12) & 0xfffff);
				loongarch_ori (code, loongarch_r21, loongarch_r21, ins->inst_offset & 0xfff);
				loongarch_ldxw (code, ins->dreg, loongarch_r21, ins->inst_basereg);
			}
			break;
		case OP_LOADU4_MEMBASE:
			if (loongarch_is_imm12 (ins->inst_offset)) {
				loongarch_ldwu (code, ins->dreg, ins->inst_basereg, ins->inst_offset & 0xfff);
			} else {
				loongarch_lu12iw (code, loongarch_r21, (ins->inst_offset >> 12) & 0xfffff);
				loongarch_ori (code, loongarch_r21, loongarch_r21, ins->inst_offset & 0xfff);
				loongarch_ldxwu (code, ins->dreg, loongarch_r21, ins->inst_basereg);
			}
			break;
		case OP_LOADI1_MEMBASE:
			if (loongarch_is_imm12 (ins->inst_offset)) {
				loongarch_ldb (code, ins->dreg, ins->inst_basereg, ins->inst_offset & 0xfff);
			} else {
				loongarch_lu12iw (code, loongarch_r21, (ins->inst_offset >> 12) & 0xfffff);
				loongarch_ori (code, loongarch_r21, loongarch_r21, ins->inst_offset & 0xfff);
				loongarch_ldxb (code, ins->dreg, loongarch_r21, ins->inst_basereg);
			}
			break;
		case OP_LOADU1_MEMBASE:
			if (loongarch_is_imm12 (ins->inst_offset)) {
				loongarch_ldbu (code, ins->dreg, ins->inst_basereg, ins->inst_offset & 0xfff);
			} else {
				loongarch_lu12iw (code, loongarch_r21, (ins->inst_offset >> 12) & 0xfffff);
				loongarch_ori (code, loongarch_r21, loongarch_r21, ins->inst_offset & 0xfff);
				loongarch_ldxbu (code, ins->dreg, loongarch_r21, ins->inst_basereg);
			}
			break;
		case OP_LOADI2_MEMBASE:
			if (loongarch_is_imm12 (ins->inst_offset)) {
				loongarch_ldh (code, ins->dreg, ins->inst_basereg, ins->inst_offset & 0xfff);
			} else {
				loongarch_lu12iw (code, loongarch_r21, (ins->inst_offset >> 12) & 0xfffff);
				loongarch_ori (code, loongarch_r21, loongarch_r21, ins->inst_offset & 0xfff);
				loongarch_ldxh (code, ins->dreg, loongarch_r21, ins->inst_basereg);
			}
			break;
		case OP_LOADU2_MEMBASE:
			if (loongarch_is_imm12 (ins->inst_offset)) {
				loongarch_ldhu (code, ins->dreg, ins->inst_basereg, ins->inst_offset & 0xfff);
			} else {
				loongarch_lu12iw (code, loongarch_r21, (ins->inst_offset >> 12) & 0xfffff);
				loongarch_ori (code, loongarch_r21, loongarch_r21, ins->inst_offset & 0xfff);
				loongarch_ldxhu (code, ins->dreg, loongarch_r21, ins->inst_basereg);
			}
			break;
		case OP_ICONV_TO_I1:
		case OP_LCONV_TO_I1:
			loongarch_extwb (code, ins->dreg, ins->sreg1);
			break;
		case OP_ICONV_TO_I2:
		case OP_LCONV_TO_I2:
			loongarch_extwh (code, ins->dreg, ins->sreg1);
			break;
		case OP_ICONV_TO_U1:
		case OP_LCONV_TO_U1:
			loongarch_andi (code, ins->dreg, ins->sreg1, 0xff);
			break;
		case OP_ICONV_TO_U2:
		case OP_LCONV_TO_U2:
			loongarch_bstrpickd (code, ins->dreg, ins->sreg1, 15, 0);
			break;
		case OP_BREAK:
			/*
			 * gdb does not like encountering the hw breakpoint ins in the debugged code.
			 * So instead of emitting a trap, we emit a call a C function and place a
			 * breakpoint there.
			 */
			mono_add_patch_info_rel (cfg, code - cfg->native_code, MONO_PATCH_INFO_JIT_ICALL_ID, GUINT_TO_POINTER (MONO_JIT_ICALL_mono_break), MONO_R_LOONGARCH64_BL);
			loongarch_bl (code, 0);
			cfg->thunk_area += THUNK_SIZE;
			break;
		case OP_IADD:
			loongarch_addw (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_LADD:
			loongarch_addd (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;

		case OP_IADD_IMM:
			if (loongarch_is_imm12 (ins->inst_imm)) {
				loongarch_addiw (code, ins->dreg, ins->sreg1, (ins->inst_imm) & 0xfff);
			} else {
				loongarch_lu12iw (code, loongarch_r21, (ins->inst_imm >> 12) & 0xfffff);
				loongarch_ori (code, loongarch_r21, loongarch_r21, (ins->inst_imm & 0xfff));
				loongarch_addw (code, ins->dreg, ins->sreg1, loongarch_r21);
			}
			break;
		case OP_ADD_IMM:
		case OP_LADD_IMM:
			if (loongarch_is_imm12 (ins->inst_imm)) {
				loongarch_addid (code, ins->dreg, ins->sreg1, ins->inst_imm & 0xfff);
			} else {
				code = mono_loongarch_emit_imm64 (code, loongarch_r21, ins->inst_imm);
				loongarch_addd (code, ins->dreg, ins->sreg1, loongarch_r21);
			}
			break;

		case OP_ISUB:
			loongarch_subw (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_LSUB:
			loongarch_subd (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;

		case OP_ISUB_IMM:
			// we add the negated value
			if (loongarch_is_imm12 (-ins->inst_imm)) {
				loongarch_addiw (code, ins->dreg, ins->sreg1, (-ins->inst_imm) & 0xfff);
			} else {
				loongarch_lu12iw (code, loongarch_r21, (ins->inst_imm >> 12) & 0xfffff);
				loongarch_ori (code, loongarch_r21, loongarch_r21, (ins->inst_imm & 0xfff));
				loongarch_subw (code, ins->dreg, ins->sreg1, loongarch_r21);
            }
			break;

		case OP_SUB_IMM:
		case OP_LSUB_IMM:
			// we add the negated value
			if (loongarch_is_imm12 (-ins->inst_imm)) {
				loongarch_addid (code, ins->dreg, ins->sreg1, -ins->inst_imm & 0xfff);
			} else {
				code = mono_loongarch_emit_imm64 (code, loongarch_r21, ins->inst_imm);
				loongarch_subd (code, ins->dreg, ins->sreg1, loongarch_r21);
			}
			break;

		case OP_IAND:
			loongarch_and (code, ins->dreg, ins->sreg1, ins->sreg2);
			loongarch_bstrinsd (code, ins->dreg, loongarch_zero, 0x3f, 0x20);
			break;
		case OP_LAND:
			loongarch_and (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;

		case OP_AND_IMM:
		case OP_IAND_IMM:
		case OP_LAND_IMM:
			if (ins->inst_imm & 0xfffff000)
			{
				loongarch_lu12iw (code, loongarch_r21, (ins->inst_imm >> 12) & 0xfffff);
				loongarch_ori (code, loongarch_r21, loongarch_r21, (ins->inst_imm & 0xfff));
				loongarch_and (code, ins->dreg, ins->sreg1, loongarch_r21);
			} else {
				loongarch_andi (code, ins->dreg, ins->sreg1, (ins->inst_imm) & 0xfff);
			}
			if (ins->opcode == OP_IAND_IMM) {
				loongarch_bstrinsd (code, ins->dreg, loongarch_zero, 0x3f, 0x20);
			}
			break;

		case OP_IDIV:
		case OP_IREM: {
			/* Check for zero */
			loongarch_pcaddi (code, loongarch_t0, 1);
			mono_add_patch_info_rel (cfg, code - cfg->native_code, MONO_PATCH_INFO_EXC, "DivideByZeroException", MONO_R_LOONGARCH64_BZ);
			loongarch_beqz (code, ins->sreg2, 0);

			/* Check for INT_MIN/-1 */
			loongarch_lu12iw (code, loongarch_r21, 0x80000);
			loongarch_xor (code, loongarch_r21, ins->sreg1, loongarch_r21);
			loongarch_addiw (code, loongarch_ra, ins->sreg2, 1);
			loongarch_or (code, loongarch_r21, loongarch_r21, loongarch_ra);
			loongarch_bstrpickd (code, loongarch_r21, loongarch_r21, 31, 0);

			loongarch_pcaddi (code, loongarch_t0, 1);
			mono_add_patch_info_rel (cfg, code - cfg->native_code, MONO_PATCH_INFO_EXC, "OverflowException", MONO_R_LOONGARCH64_BZ);
			loongarch_beqz (code, loongarch_r21, 0);

			loongarch_slliw (code, ins->sreg1, ins->sreg1, 0x0);
			loongarch_slliw (code, ins->sreg2, ins->sreg2, 0x0);
			if (ins->opcode == OP_IDIV) {
				loongarch_divw (code, ins->dreg, ins->sreg1, ins->sreg2);
			} else {
				loongarch_modw (code, ins->dreg, ins->sreg1, ins->sreg2);
			}
			break;
		}
		case OP_IDIV_UN:
		case OP_IREM_UN: {
			/* Check for zero */
			loongarch_pcaddi (code, loongarch_t0, 1);
			mono_add_patch_info_rel (cfg, code - cfg->native_code, MONO_PATCH_INFO_EXC, "DivideByZeroException", MONO_R_LOONGARCH64_BZ);
			loongarch_beqz (code, ins->sreg2, 0);
			loongarch_slliw (code, ins->sreg1, ins->sreg1, 0x0);
			loongarch_slliw (code, ins->sreg2, ins->sreg2, 0x0);
			if (ins->opcode == OP_IDIV_UN) {
				loongarch_divwu (code, ins->dreg, ins->sreg1, ins->sreg2);
			} else {
				loongarch_modwu (code, ins->dreg, ins->sreg1, ins->sreg2);
			}
			break;
		}
		case OP_LDIV:
		case OP_LREM: {
			/* Check for zero */
			loongarch_pcaddi (code, loongarch_t0, 1);
			mono_add_patch_info_rel (cfg, code - cfg->native_code, MONO_PATCH_INFO_EXC, "DivideByZeroException", MONO_R_LOONGARCH64_BZ);
			loongarch_beqz (code, ins->sreg2, 0);

			/* Check for INT_MIN/-1 */
			loongarch_lu52id (code, loongarch_r21, loongarch_zero, 0x800);
			loongarch_xor (code, loongarch_r21, ins->sreg1, loongarch_r21);
			loongarch_addid (code, loongarch_ra, ins->sreg2, 1);
			loongarch_or (code, loongarch_r21, loongarch_r21, loongarch_ra);

			loongarch_pcaddi (code, loongarch_t0, 1);
			mono_add_patch_info_rel (cfg, code - cfg->native_code, MONO_PATCH_INFO_EXC, "OverflowException", MONO_R_LOONGARCH64_BZ);
			loongarch_beqz (code, loongarch_r21, 0);

			if (ins->opcode == OP_LDIV) {
				loongarch_divd (code, ins->dreg, ins->sreg1, ins->sreg2);
			} else {
				loongarch_modd (code, ins->dreg, ins->sreg1, ins->sreg2);
			}
			break;
		}
		case OP_LDIV_UN:
		case OP_LREM_UN: {
			/* Check for zero */
			loongarch_pcaddi (code, loongarch_t0, 1);
			mono_add_patch_info_rel (cfg, code - cfg->native_code, MONO_PATCH_INFO_EXC, "DivideByZeroException", MONO_R_LOONGARCH64_BZ);
			loongarch_beqz (code, ins->sreg2, 0);
			if (ins->opcode == OP_LDIV_UN) {
				loongarch_divdu (code, ins->dreg, ins->sreg1, ins->sreg2);
			} else {
				loongarch_moddu (code, ins->dreg, ins->sreg1, ins->sreg2);
			}
			break;
		}
		case OP_DIV_IMM:
			g_assert_not_reached ();
			break;
		case OP_REM_IMM:
			g_assert_not_reached ();
		case OP_IOR:
			loongarch_or (code, ins->dreg, ins->sreg1, ins->sreg2);
			loongarch_bstrinsd (code, ins->dreg, loongarch_zero, 0x3f, 0x20);
			break;
		case OP_LOR:
			loongarch_or (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_OR_IMM:
		case OP_IOR_IMM:
		case OP_LOR_IMM:
			/* unsigned 12-bit immediate */
			if (!(ins->inst_imm & 0xfffff000)) {
				loongarch_ori (code, ins->dreg, ins->sreg1, (ins->inst_imm) & 0xfff);
			} else {
				loongarch_lu12iw (code, loongarch_r21, (ins->inst_imm >> 12) & 0xfffff);
				loongarch_ori (code, loongarch_r21, loongarch_r21, (ins->inst_imm & 0xfff));
				loongarch_or (code, ins->dreg, ins->sreg1, loongarch_r21);
			}
			if (ins->opcode == OP_IOR_IMM) {
				loongarch_bstrinsd (code, ins->dreg, loongarch_zero, 0x3f, 0x20);
			}
			break;
		case OP_IXOR:
			loongarch_xor (code, ins->dreg, ins->sreg1, ins->sreg2);
			loongarch_bstrinsd (code, ins->dreg, loongarch_zero, 0x3f, 0x20);
			break;
		case OP_LXOR:
			loongarch_xor (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_XOR_IMM:
		case OP_IXOR_IMM:
		case OP_LXOR_IMM:
			/* unsigned 12-bit immediate */
			if (!(ins->inst_imm & 0xfffff000)) {
				loongarch_xori (code, ins->dreg, ins->sreg1, (ins->inst_imm) & 0xfff);
			} else {
				loongarch_lu12iw (code, loongarch_r21, (ins->inst_imm >> 12) & 0xfffff);
				loongarch_ori (code, loongarch_r21, loongarch_r21, (ins->inst_imm & 0xfff));
				loongarch_xor (code, ins->dreg, ins->sreg1, loongarch_r21);
			}
			if (ins->opcode == OP_IXOR_IMM) {
				loongarch_bstrinsd (code, ins->dreg, loongarch_zero, 0x3f, 0x20);
			}
			break;
		case OP_ISHL:
			loongarch_sllw (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_ISHL_IMM:
			loongarch_slliw (code, ins->dreg, ins->sreg1, ins->inst_imm & 0x1f);
			break;
		case OP_SHL_IMM:
		case OP_LSHL_IMM:
			loongarch_sllid (code, ins->dreg, ins->sreg1, ins->inst_imm & 0x3f);
			break;
		case OP_LSHL:
			loongarch_slld (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_ISHR:
			loongarch_sraw (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_LSHR:
			loongarch_srad (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_ISHR_IMM:
			loongarch_sraiw (code, ins->dreg, ins->sreg1, ins->inst_imm & 0x1f);
			break;
		case OP_SHR_IMM:
		case OP_LSHR_IMM:
			loongarch_sraid (code, ins->dreg, ins->sreg1, ins->inst_imm & 0x3f);
			break;
		case OP_ISHR_UN_IMM:
			loongarch_srliw (code, ins->dreg, ins->sreg1, ins->inst_imm & 0x1f);
			break;
		case OP_SHR_UN_IMM:
		case OP_LSHR_UN_IMM:
			loongarch_srlid (code, ins->dreg, ins->sreg1, ins->inst_imm & 0x3f);
			break;
		case OP_ISHR_UN:
			loongarch_srlw (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_LSHR_UN:
			loongarch_srld (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_INOT:
		case OP_LNOT:
			loongarch_nor (code, ins->dreg, loongarch_zero, ins->sreg1);
			break;
		case OP_INEG:
			loongarch_subw (code, ins->dreg, loongarch_zero, ins->sreg1);
			break;
		case OP_LNEG:
			loongarch_subd (code, ins->dreg, loongarch_zero, ins->sreg1);
			break;
		case OP_IMUL:
			loongarch_mulw (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_LMUL:
			loongarch_muld (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_IMUL_IMM:
			loongarch_lu12iw (code, loongarch_r21, (ins->inst_imm >> 12) & 0xfffff);
			loongarch_ori (code, loongarch_r21, loongarch_r21, (ins->inst_imm & 0xfff));
			loongarch_mulw (code, ins->dreg, ins->sreg1, loongarch_r21);
			break;
		case OP_MUL_IMM:
		case OP_LMUL_IMM:
			code = mono_loongarch_emit_imm64 (code, loongarch_r21, ins->inst_imm);
			loongarch_muld (code, ins->dreg, ins->sreg1, loongarch_r21);
			break;
		case OP_ICONST:
			if (loongarch_is_imm12 (ins->inst_c0)) {
				loongarch_addid (code, ins->dreg, loongarch_zero, ins->inst_c0 & 0xfff);
			} else {
				loongarch_lu12iw (code, ins->dreg, (ins->inst_c0 >> 12) & 0xfffff);
				loongarch_ori (code, ins->dreg, ins->dreg, ins->inst_c0 & 0xfff);
				if ((ins->inst_c0 > 0) && ((ins->inst_c0) & (0x0000000080000000)))
					loongarch_bstrinsd (code, ins->dreg, loongarch_zero, 0x3f, 0x20);
			}
			break;
		case OP_I8CONST:
			code = mono_loongarch_emit_imm64 (code, ins->dreg, ins->inst_c0);
			break;
		case OP_AOTCONST:
			code = emit_aotconst (cfg, code, ins->dreg, (MonoJumpInfoType)(gsize)ins->inst_i1, ins->inst_p0);
			break;

		case OP_ICONV_TO_I4:
		case OP_ICONV_TO_U4:
		case OP_MOVE:
			if (ins->dreg != ins->sreg1)
				loongarch_ori (code, ins->dreg, ins->sreg1, 0);
			break;
		case OP_ZEXT_I4:
			if (ins->dreg != ins->sreg1)
				loongarch_ori (code, ins->dreg, ins->sreg1, 0);
			loongarch_bstrinsd (code, ins->dreg, loongarch_zero, 63, 32);
			break;
		case OP_SEXT_I4:
			loongarch_slliw (code, ins->dreg, ins->sreg1, 0);
			break;
		case OP_FMOVE:
			if (ins->dreg != ins->sreg1) {
				loongarch_fmovd (code, ins->dreg, ins->sreg1);
			}
			break;
		case OP_RMOVE:
			if (ins->dreg != ins->sreg1) {
				loongarch_fmovs (code, ins->dreg, ins->sreg1);
			}
			break;
		case OP_MOVE_F_TO_I4:
			if (cfg->r4fp) {
				loongarch_movfr2grs (code, ins->dreg, ins->sreg1);
			} else {
				loongarch_fcvtsd (code, loongarch_ftemp, ins->sreg1);
				loongarch_movfr2grd (code, ins->dreg, loongarch_ftemp);
			}
			break;
		case OP_MOVE_I4_TO_F:
			if (cfg->r4fp) {
				loongarch_movgr2frw (code, ins->dreg, ins->sreg1);
			} else {
				loongarch_movgr2frd (code, loongarch_ftemp, ins->sreg1);
				loongarch_fcvtds (code, ins->dreg, loongarch_ftemp);
			}
			break;
		case OP_MOVE_F_TO_I8:
			loongarch_movfr2grd (code, ins->dreg, ins->sreg1);
			break;
		case OP_MOVE_I8_TO_F:
			loongarch_movgr2frd (code, ins->dreg, ins->sreg1);
			break;
		case OP_CHECK_THIS:
			/* ensure ins->sreg1 is not NULL */
			loongarch_ldw (code, loongarch_zero, ins->sreg1, 0);
			break;
		case OP_ARGLIST: {
			g_assert (loongarch_is_imm12 (cfg->sig_cookie));
			loongarch_addid (code, loongarch_r21, cfg->arch.args_reg, cfg->arch.cinfo->sig_cookie.offset);
			loongarch_std (code, loongarch_r21, ins->sreg1, 0);
			break;
		}
		case OP_FCALL:
		case OP_LCALL:
		case OP_VCALL2:
		case OP_VOIDCALL:
		case OP_CALL:
		case OP_RCALL: {
			call = (MonoCallInst*)ins;
			const MonoJumpInfoTarget patch = mono_call_to_patch (call);
			code = emit_call (cfg, code, patch.type, patch.target);
			code = emit_move_return_value (cfg, code, ins);
			break;
		}
		case OP_FCALL_REG:
		case OP_LCALL_REG:
		case OP_VCALL2_REG:
		case OP_VOIDCALL_REG:
		case OP_CALL_REG:
		case OP_RCALL_REG:
			loongarch_jirl (code, 1, ins->sreg1, 0);
			code = emit_move_return_value (cfg, code, ins);
			break;

		case OP_FCALL_MEMBASE:
		case OP_LCALL_MEMBASE:
		case OP_VCALL2_MEMBASE:
		case OP_VOIDCALL_MEMBASE:
		case OP_CALL_MEMBASE:
		case OP_RCALL_MEMBASE:
			code = emit_ldd (code, loongarch_r21, ins->inst_basereg, ins->inst_offset);
			loongarch_jirl (code, 1, loongarch_r21, 0);
			code = emit_move_return_value (cfg, code, ins);
			break;
		case OP_TAILCALL_PARAMETER:
			// This opcode helps compute sizes, i.e.
			// of the subsequent OP_TAILCALL, but contributes no code.
			g_assert (ins->next);
			break;

		case OP_TAILCALL:
		case OP_TAILCALL_REG:
		case OP_TAILCALL_MEMBASE: {
			int branch_reg = loongarch_r21;
			call = (MonoCallInst*)ins;

			g_assert (!cfg->method->save_lmf);

			max_len += call->stack_usage / sizeof (target_mgreg_t) * ins_get_size (OP_TAILCALL_PARAMETER);
			while (G_UNLIKELY (offset + max_len > cfg->code_size)) {
				cfg->code_size *= 2;
				cfg->native_code = (unsigned char *)mono_realloc_native_code (cfg);
				code = cfg->native_code + offset;
				cfg->stat_code_reallocs++;
			}

			switch (ins->opcode) {
			case OP_TAILCALL:
				break;

			case OP_TAILCALL_REG:
				g_assert (ins->sreg1 != -1);
				g_assert (ins->sreg1 != loongarch_r21);
				g_assert (ins->sreg1 != loongarch_ra);
				g_assert (ins->sreg1 != loongarch_sp);
				g_assert (ins->sreg1 != loongarch_fp);
				g_assert (ins->sreg1 != loongarch_s8);
				if ((ins->sreg1 << 1) & MONO_ARCH_CALLEE_SAVED_REGS) {
					loongarch_ori (code, branch_reg, ins->sreg1, 0);
				} else {
					branch_reg = ins->sreg1;
				}
				break;

			case OP_TAILCALL_MEMBASE:
				g_assert (ins->inst_basereg != -1);
				g_assert (ins->inst_basereg != loongarch_r21);
				g_assert (ins->inst_basereg != loongarch_ra);
				g_assert (ins->inst_basereg != loongarch_sp);
				g_assert (ins->inst_basereg != loongarch_fp);
				g_assert (ins->inst_basereg != loongarch_s8);
				code = emit_ldd (code, branch_reg, ins->inst_basereg, ins->inst_offset);
				break;

			default:
				g_assert_not_reached ();
			}

			// Copy stack arguments.
			// FIXME a fixed size memcpy is desirable here,
			// at least for larger values of stack_usage.
			for (int i = 0; i < call->stack_usage; i += sizeof (target_mgreg_t)) {
				code = emit_ldd (code, loongarch_ra, loongarch_sp, i);
				code = emit_std (code, loongarch_ra, loongarch_s8, i);
			}

			code = mono_loongarch_emit_load_regset (code, MONO_ARCH_CALLEE_SAVED_REGS & cfg->used_int_regs, loongarch_fp, cfg->arch.saved_gregs_offset);

			/* Destroy frame */
			code = mono_loongarch64_emit_destroy_frame (code, cfg->stack_offset);

			switch (ins->opcode) {
			case OP_TAILCALL:
				if (cfg->compile_aot) {
					/* This is not a PLT patch */
					code = emit_aotconst (cfg, code, branch_reg, MONO_PATCH_INFO_METHOD_JUMP, call->method);
				} else {
					mono_add_patch_info_rel (cfg, code - cfg->native_code, MONO_PATCH_INFO_METHOD_JUMP, call->method, MONO_R_LOONGARCH64_B);
					loongarch_b (code, 0);
					cfg->thunk_area += THUNK_SIZE;
					break;
				}
				// fallthrough
			case OP_TAILCALL_MEMBASE:
			case OP_TAILCALL_REG:
				loongarch_jirl (code, 0, branch_reg, 0);
				break;

			default:
				g_assert_not_reached ();
			}

			ins->flags |= MONO_INST_GC_CALLSITE;
			ins->backend.pc_offset = code - cfg->native_code;
			break;
		}
		case OP_LOCALLOC: {
			/* Round up ins->sreg1, loongarch_r21 ends up holding size */
			loongarch_addid (code, loongarch_r21, ins->sreg1, MONO_ARCH_FRAME_ALIGNMENT-1);
			loongarch_bstrinsd (code, loongarch_r21, loongarch_zero, 3, 0);

			loongarch_subd (code, loongarch_sp, loongarch_sp, loongarch_r21);

			if (ins->flags & MONO_INST_INIT) {
				loongarch_beqz (code, loongarch_r21, 6);

				loongarch_addd (code, loongarch_temp, loongarch_sp, loongarch_r21);
				loongarch_std (code, loongarch_zero, loongarch_temp, 0xff0);//-16
				loongarch_std (code, loongarch_zero, loongarch_temp, 0xff8);//-8
				loongarch_addid (code, loongarch_r21, loongarch_r21, 0xff0);//-16
				loongarch_bnez (code, loongarch_r21, 0x1ffffc);//-4
			}
			loongarch_ori (code, ins->dreg, loongarch_sp, 0);

			int area_offset = cfg->param_area;
			g_assert ((area_offset >= 0) && (area_offset < 0x7ff));
			if (area_offset)
				loongarch_addid (code, loongarch_sp, loongarch_sp, (-area_offset) & 0xfff);

			break;
		}
		case OP_LOCALLOC_IMM: {
			int imm, offset;

			imm = ALIGN_TO (ins->inst_imm, MONO_ARCH_FRAME_ALIGNMENT);
			g_assert (((imm) >= 0) && ((imm) < 0xfff));
			loongarch_addid (code, loongarch_sp, loongarch_sp, -imm);

			/* Init */
			g_assert (MONO_ARCH_FRAME_ALIGNMENT == 16);
			offset = 0;
			while (offset < imm) {
				loongarch_std (code, loongarch_zero, loongarch_sp, offset);
				loongarch_std (code, loongarch_zero, loongarch_sp, (offset+0x8));
				offset += 16;
			}
			loongarch_ori (code, ins->dreg, loongarch_sp, 0);

			int area_offset = cfg->param_area;
			g_assert ((area_offset >= 0) && (area_offset < 0x7ff));
			if (area_offset)
				loongarch_addid (code, loongarch_sp, loongarch_sp, (-area_offset) & 0xfff);
			break;
		}
			/* EH */
		case OP_LA_COND_EXC_IC:
		case OP_LA_COND_EXC_IOV:
		case OP_LA_COND_EXC_INC:
		case OP_LA_COND_EXC_INO:
		case OP_LA_COND_EXC_IEQ:
		case OP_LA_COND_EXC_INE_UN:
		case OP_LA_COND_EXC_ILT_UN:
		case OP_LA_COND_EXC_ILT:
		case OP_LA_COND_EXC_IGT:
		case OP_LA_COND_EXC_IGT_UN:
		case OP_LA_COND_EXC_IGE:
		case OP_LA_COND_EXC_IGE_UN:
		case OP_LA_COND_EXC_ILE:
		case OP_LA_COND_EXC_ILE_UN: {
			gint src1;
			gint src2;
			loongarch_slliw (code, ins->sreg1, ins->sreg1, 0x0);
			loongarch_slliw (code, ins->sreg2, ins->sreg2, 0x0);
			src1 = ins->sreg1;
			src2 = ins->sreg2;
			gint cond = opcode_to_loongarchcond (ins->opcode, &src1, &src2);
			/* Capture PC */
			loongarch_pcaddi (code, loongarch_t0, 1);
			mono_add_patch_info_rel (cfg, code - cfg->native_code, MONO_PATCH_INFO_EXC, (const char*)ins->inst_p1, MONO_R_LOONGARCH64_BC);
			loongarch_format_2rui (code, cond, 0, src1, src2);
			break;
		}
		case OP_LA_COND_EXC_C:
		case OP_LA_COND_EXC_OV:
		case OP_LA_COND_EXC_NC:
		case OP_LA_COND_EXC_NO:
		case OP_LA_COND_EXC_EQ:
		case OP_LA_COND_EXC_NE_UN:
		case OP_LA_COND_EXC_LT:
		case OP_LA_COND_EXC_LT_UN:
		case OP_LA_COND_EXC_GT:
		case OP_LA_COND_EXC_GT_UN:
		case OP_LA_COND_EXC_GE:
		case OP_LA_COND_EXC_GE_UN:
		case OP_LA_COND_EXC_LE:
		case OP_LA_COND_EXC_LE_UN: {
			gint src1;
			gint src2;
			src1 = ins->sreg1;
			src2 = ins->sreg2;
			gint cond = opcode_to_loongarchcond (ins->opcode, &src1, &src2);
			/* Capture PC */
			loongarch_pcaddi (code, loongarch_t0, 1);
			mono_add_patch_info_rel (cfg, code - cfg->native_code, MONO_PATCH_INFO_EXC, (const char*)ins->inst_p1, MONO_R_LOONGARCH64_BC);
			loongarch_format_2rui (code, cond, 0, src1, src2);
			break;
		}
		case OP_THROW: {
			if (ins->sreg1 != loongarch_a0)
				loongarch_ori (code, loongarch_a0, ins->sreg1, 0);
			code = emit_call (cfg, code, MONO_PATCH_INFO_JIT_ICALL_ID,
							  GUINT_TO_POINTER (MONO_JIT_ICALL_mono_arch_throw_exception));
			break;
		}
		case OP_RETHROW: {
			if (ins->sreg1 != loongarch_a0)
				loongarch_ori (code, loongarch_a0, ins->sreg1, 0);
			code = emit_call (cfg, code, MONO_PATCH_INFO_JIT_ICALL_ID,
							  GUINT_TO_POINTER (MONO_JIT_ICALL_mono_arch_rethrow_exception));
			break;
		}
		case OP_START_HANDLER: {
			/*
			 * The START_HANDLER instruction marks the beginning of
			 * a handler block. It is called using a call
			 * instruction, so loongarch_ra contains the return address.
			 * Since the handler executes in the same stack frame
			 * as the method itself, we can't use save/restore to
			 * save the return address. Instead, we save it into
			 * a dedicated variable.
			 */
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);

			if (loongarch_is_imm12 (spvar->inst_offset)) {
				loongarch_std (code, loongarch_ra, spvar->inst_basereg, spvar->inst_offset);
			} else {
				target_mgreg_t tmp_off = spvar->inst_offset;
				g_assert ((-0x80000000L <= tmp_off) && (tmp_off < 0x7fffffffL));
				g_assert (!(3 & tmp_off));
				tmp_off += (tmp_off & 0x8000);
				loongarch_addu16id (code, loongarch_r21, spvar->inst_basereg, (tmp_off >> 16) & 0xffff);
				tmp_off = spvar->inst_offset & 0xffff;
				tmp_off -= ((tmp_off & 0x8000) << 1);
				loongarch_stptrd (code, loongarch_ra, loongarch_r21, ((tmp_off >> 2) & 0x3fff));
			}
			/*
			 * Reserve a param area, see test_0_finally_param_area ().
			 * This is needed because the param area is not set up when
			 * we are called from EH code.
			 */
			if (cfg->param_area) {
				g_assert ((cfg->param_area > 0) && (cfg->param_area < 0x7fffffff));
				g_assert (!(cfg->param_area & 0xf));
				if (cfg->param_area < 0x7ff) {
					loongarch_addid (code, loongarch_sp, loongarch_sp, -cfg->param_area & 0xfff);
				} else {
					loongarch_lu12iw (code, loongarch_r21, (cfg->param_area >> 12) & 0xfffff);
					loongarch_ori (code, loongarch_r21, loongarch_r21, cfg->param_area & 0xfff);
					loongarch_subd (code, loongarch_sp, loongarch_sp, loongarch_r21);
				}
			}
			break;
		}
		case OP_ENDFINALLY:
		case OP_ENDFILTER: {
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);

			g_assert ((cfg->param_area >= 0) && (cfg->param_area < 0x7ff));
			if (cfg->param_area)
				loongarch_addid (code, loongarch_sp, loongarch_sp, cfg->param_area & 0xfff);

			if (ins->opcode == OP_ENDFILTER && ins->sreg1 != loongarch_a0)
				loongarch_ori (code, loongarch_a0, ins->sreg1, 0);

			/* Return to either after the branch in OP_CALL_HANDLER, or to the EH code */
			code = emit_ldd (code, loongarch_ra, spvar->inst_basereg, spvar->inst_offset);
			loongarch_jirl (code, 0, loongarch_ra, 0);
			break;
		}
		case OP_GET_EX_OBJ:
			if (ins->dreg != loongarch_a0)
				loongarch_ori (code, ins->dreg, loongarch_a0, 0);
			break;
		case OP_CALL_HANDLER:
			mono_add_patch_info_rel (cfg, offset, MONO_PATCH_INFO_BB, ins->inst_target_bb, MONO_R_LOONGARCH64_BL);
			loongarch_bl (code, 0);
			cfg->thunk_area += THUNK_SIZE;
			for (GList *tmp = ins->inst_eh_blocks; tmp != bb->clause_holes; tmp = tmp->prev)
				mono_cfg_add_try_hole (cfg, ((MonoLeaveClause *) tmp->data)->clause, code, bb);

			break;
		case OP_BR:
			mono_add_patch_info_rel (cfg, offset, MONO_PATCH_INFO_BB, ins->inst_target_bb, MONO_R_LOONGARCH64_B);
			loongarch_b (code, 0);
			break;
		case OP_BR_REG:
			loongarch_jirl (code, 0, ins->sreg1, 0);
			break;
		case OP_RCNEQ:
			loongarch_fcmpcnes (code, 1, ins->sreg1, ins->sreg2);//cc=1
			loongarch_movcf2gr (code, ins->dreg, 1);
			break;
		case OP_LA_RCNEQ_UN:
			loongarch_fcmpcunes (code, 1, ins->sreg1, ins->sreg2);//cc=1
			loongarch_movcf2gr (code, ins->dreg, 1);
			break;
		case OP_RCLE:
			loongarch_fcmpcles (code, 1, ins->sreg1, ins->sreg2);//cc=1
			loongarch_movcf2gr (code, ins->dreg, 1);
			break;
		case OP_LA_RCLE_UN:
			loongarch_fcmpcules (code, 1, ins->sreg1, ins->sreg2);//cc=1
			loongarch_movcf2gr (code, ins->dreg, 1);
			break;
		case OP_RCGE:
			loongarch_fcmpcles (code, 1, ins->sreg2, ins->sreg1);//cc=1
			loongarch_movcf2gr (code, ins->dreg, 1);
			break;
		case OP_LA_RCGE_UN:
			loongarch_fcmpcules (code, 1, ins->sreg2, ins->sreg1);//cc=1
			loongarch_movcf2gr (code, ins->dreg, 1);
			break;
		case OP_FCNEQ:
			loongarch_fcmpcuned (code, 1, ins->sreg1, ins->sreg2);//cc=1
			loongarch_movcf2gr (code, ins->dreg, 1);
			break;
		case OP_LA_FCNEQ_UN:
			loongarch_fcmpcuned (code, 1, ins->sreg1, ins->sreg2);//cc=1
			loongarch_movcf2gr (code, ins->dreg, 1);
			break;
		case OP_FCLE:
			loongarch_fcmpcled (code, 1, ins->sreg1, ins->sreg2);//cc=1
			loongarch_movcf2gr (code, ins->dreg, 1);
			break;
		case OP_LA_FCLE_UN:
			loongarch_fcmpculed (code, 1, ins->sreg1, ins->sreg2);//cc=1
			loongarch_movcf2gr (code, ins->dreg, 1);
			break;
		case OP_FCGE:
			loongarch_fcmpcled (code, 1, ins->sreg2, ins->sreg1);//cc=1
			loongarch_movcf2gr (code, ins->dreg, 1);
			break;
		case OP_LA_FCGE_UN:
			loongarch_fcmpculed (code, 1, ins->sreg2, ins->sreg1);//cc=1
			loongarch_movcf2gr (code, ins->dreg, 1);
			break;

		case OP_LA_SLTI:
			if (loongarch_is_imm12 (ins->inst_imm)) {
				loongarch_slti (code, ins->dreg, ins->sreg1, ins->inst_imm & 0xfff);
			} else {
				g_assert_not_reached ();
			}
			break;
		case OP_LA_SLTUI:
			if (loongarch_is_imm12 (ins->inst_imm)) {
				loongarch_sltui (code, ins->dreg, ins->sreg1, ins->inst_imm & 0xfff);
			} else {
				g_assert_not_reached ();
			}
			break;

		case OP_LA_CEQ:
		case OP_LA_ICEQ:
			//NOTE:sreg1 and sreg2 should had been sign-extented.;
			loongarch_slliw (code, ins->sreg1, ins->sreg1, 0x0);
			loongarch_slliw (code, ins->sreg2, ins->sreg2, 0x0);
			loongarch_xor (code, ins->dreg, ins->sreg1, ins->sreg2);
			loongarch_sltui (code, ins->dreg, ins->dreg, 1);
			break;
		case OP_LA_ICNEQ:
			//NOTE:sreg1 and sreg2 should had been sign-extented.;
			loongarch_slliw (code, ins->sreg1, ins->sreg1, 0x0);
			loongarch_slliw (code, ins->sreg2, ins->sreg2, 0x0);
			loongarch_xor (code, ins->dreg, ins->sreg1, ins->sreg2);
			loongarch_sltui (code, ins->dreg, ins->dreg, 1);
			loongarch_xori (code, ins->dreg, ins->dreg, 1);
			break;
		case OP_LA_CLT:
		case OP_LA_ICLT:
			//NOTE:sreg1 and sreg2 should had been sign-extented.;
			loongarch_slliw (code, ins->sreg1, ins->sreg1, 0x0);
			loongarch_slliw (code, ins->sreg2, ins->sreg2, 0x0);
			loongarch_slt (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_LA_CLT_UN:
		case OP_LA_ICLT_UN:
			//NOTE:sreg1 and sreg2 should had been sign-extented.;
			loongarch_slliw (code, ins->sreg1, ins->sreg1, 0x0);
			loongarch_slliw (code, ins->sreg2, ins->sreg2, 0x0);
			loongarch_sltu (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_LA_ICLE:
			//NOTE:sreg1 and sreg2 should had been sign-extented.;
			loongarch_slliw (code, ins->sreg1, ins->sreg1, 0x0);
			loongarch_slliw (code, ins->sreg2, ins->sreg2, 0x0);
			loongarch_slt (code, ins->dreg, ins->sreg2, ins->sreg1);
			loongarch_xori (code, ins->dreg, ins->dreg, 1);
			break;
		case OP_LA_ICLE_UN:
			//NOTE:sreg1 and sreg2 should had been sign-extented.;
			loongarch_slliw (code, ins->sreg1, ins->sreg1, 0x0);
			loongarch_slliw (code, ins->sreg2, ins->sreg2, 0x0);
			loongarch_sltu (code, ins->dreg, ins->sreg2, ins->sreg1);
			loongarch_xori (code, ins->dreg, ins->dreg, 1);
			break;
		case OP_LA_CGT:
		case OP_LA_ICGT:
			//NOTE:sreg1 and sreg2 should had been sign-extented.;
			loongarch_slliw (code, ins->sreg1, ins->sreg1, 0x0);
			loongarch_slliw (code, ins->sreg2, ins->sreg2, 0x0);
			loongarch_slt (code, ins->dreg, ins->sreg2, ins->sreg1);
			break;
		case OP_LA_CGT_UN:
		case OP_LA_ICGT_UN:
			//NOTE:sreg1 and sreg2 should had been sign-extented.;
			loongarch_slliw (code, ins->sreg1, ins->sreg1, 0x0);
			loongarch_slliw (code, ins->sreg2, ins->sreg2, 0x0);
			loongarch_sltu (code, ins->dreg, ins->sreg2, ins->sreg1);
			break;
		case OP_LA_ICGE:
			//NOTE:sreg1 and sreg2 should had been sign-extented.;
			loongarch_slliw (code, ins->sreg1, ins->sreg1, 0x0);
			loongarch_slliw (code, ins->sreg2, ins->sreg2, 0x0);
			loongarch_slt (code, ins->dreg, ins->sreg1, ins->sreg2);
			loongarch_xori (code, ins->dreg, ins->dreg, 1);
			break;
		case OP_LA_ICGE_UN:
			//NOTE:sreg1 and sreg2 should had been sign-extented.;
			loongarch_slliw (code, ins->sreg1, ins->sreg1, 0x0);
			loongarch_slliw (code, ins->sreg2, ins->sreg2, 0x0);
			loongarch_sltu (code, ins->dreg, ins->sreg1, ins->sreg2);
			loongarch_xori (code, ins->dreg, ins->dreg, 1);
			break;

		case OP_LA_LCEQ:
			loongarch_xor (code, ins->dreg, ins->sreg1, ins->sreg2);
			loongarch_sltui (code, ins->dreg, ins->dreg, 1);
			break;
		case OP_LA_LCLT:
			loongarch_slt (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_LA_LCLT_UN:
			loongarch_sltu (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_LA_LCGT:
			loongarch_slt (code, ins->dreg, ins->sreg2, ins->sreg1);
			break;
		case OP_LA_LCGT_UN:
			loongarch_sltu (code, ins->dreg, ins->sreg2, ins->sreg1);
			break;

		case OP_RCEQ:
			loongarch_fcmpceqs (code, 1, ins->sreg1, ins->sreg2);//cc=1
			loongarch_movcf2gr (code, ins->dreg, 1);
			break;
		case OP_RCLT:
			loongarch_fcmpclts (code, 1, ins->sreg1, ins->sreg2);//cc=1
			loongarch_movcf2gr (code, ins->dreg, 1);
			break;
		case OP_RCLT_UN:
			loongarch_fcmpcults (code, 1, ins->sreg1, ins->sreg2);//cc=1
			loongarch_movcf2gr (code, ins->dreg, 1);
			break;
		case OP_RCGT:
			loongarch_fcmpclts (code, 1, ins->sreg2, ins->sreg1);//cc=1
			loongarch_movcf2gr (code, ins->dreg, 1);
			break;
		case OP_RCGT_UN:
			loongarch_fcmpcults (code, 1, ins->sreg2, ins->sreg1);//cc=1
			loongarch_movcf2gr (code, ins->dreg, 1);
			break;
		case OP_FCEQ:
			loongarch_fcmpceqd (code, 1, ins->sreg1, ins->sreg2);//cc=1
			loongarch_movcf2gr (code, ins->dreg, 1);
			break;
		case OP_FCLT:
			loongarch_fcmpcltd (code, 1, ins->sreg1, ins->sreg2);//cc=1
			loongarch_movcf2gr (code, ins->dreg, 1);
			break;
		case OP_FCLT_UN:
			loongarch_fcmpcultd (code, 1, ins->sreg1, ins->sreg2);//cc=1
			loongarch_movcf2gr (code, ins->dreg, 1);
			break;
		case OP_FCGT:
			loongarch_fcmpcltd (code, 1, ins->sreg2, ins->sreg1);//cc=1
			loongarch_movcf2gr (code, ins->dreg, 1);
			break;
		case OP_FCGT_UN:
			loongarch_fcmpcultd (code, 1, ins->sreg2, ins->sreg1);//cc=1
			loongarch_movcf2gr (code, ins->dreg, 1);
			break;

		/* floating point opcodes */
		case OP_R8CONST:
		{
			guint64 imm = *(guint64*)ins->inst_p0;
			if (imm == 0) {
				loongarch_movgr2frd (code, ins->dreg, loongarch_zero);
			} else {
				loongarch_lu12iw (code, loongarch_r21, (imm >> 12) & 0xfffff);
				loongarch_lu32id (code, loongarch_r21, (imm >> 32) & 0xfffff);
				loongarch_lu52id (code, loongarch_r21, loongarch_r21, (imm >> 52) & 0xfff);
				loongarch_ori (code, loongarch_r21, loongarch_r21, imm & 0xfff);
				loongarch_movgr2frd (code, ins->dreg, loongarch_r21);
			}
			break;
		}
		case OP_R4CONST:
		{
			guint32 imm = *(guint32*)ins->inst_p0;
			if (imm == 0) {
				loongarch_movgr2frd (code, ins->dreg, loongarch_zero);
			} else {
				loongarch_lu12iw (code, loongarch_r21, (imm >> 12) & 0xfffff);
				loongarch_ori (code, loongarch_r21, loongarch_r21, imm & 0xfff);
				if (cfg->r4fp) {
					loongarch_movgr2frd (code, ins->dreg, loongarch_r21);
				} else {
					loongarch_movgr2frd (code, ins->dreg, loongarch_r21);
					loongarch_fcvtds (code, ins->dreg, ins->dreg);
				}
			}
			break;
		}
		case OP_STORER8_MEMBASE_REG:
			if (loongarch_is_imm12 (ins->inst_offset)) {
				loongarch_fstd (code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset & 0xfff);
			} else {
				loongarch_lu12iw (code, loongarch_r21, (ins->inst_offset >> 12) & 0xfffff);
				loongarch_ori (code, loongarch_r21, loongarch_r21, ins->inst_offset & 0xfff);
				loongarch_fstxd (code, ins->sreg1, loongarch_r21, ins->inst_destbasereg);
			}
			break;
		case OP_LOADR8_MEMBASE:
			if (loongarch_is_imm12 (ins->inst_offset)) {
				loongarch_fldd (code, ins->dreg, ins->inst_basereg, ins->inst_offset & 0xfff);
			} else {
				loongarch_lu12iw (code, loongarch_r21, (ins->inst_offset >> 12) & 0xfffff);
				loongarch_ori (code, loongarch_r21, loongarch_r21, ins->inst_offset & 0xfff);
				loongarch_fldxd (code, ins->dreg, loongarch_r21, ins->inst_basereg);
			}
			break;
		case OP_STORER4_MEMBASE_REG:
			if (loongarch_is_imm12 (ins->inst_offset)) {
				if (cfg->r4fp) {
					loongarch_fsts (code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset);
				} else {
					/* Convert to single-float precision */
					loongarch_fcvtsd (code, loongarch_ftemp, ins->sreg1);
					loongarch_fsts (code, loongarch_ftemp, ins->inst_destbasereg, ins->inst_offset);
				}
			} else {
				if (cfg->r4fp) {
					loongarch_lu12iw (code, loongarch_r21, (ins->inst_offset >> 12) & 0xfffff);
					loongarch_ori (code, loongarch_r21, loongarch_r21, ins->inst_offset & 0xfff);
					loongarch_fstxs (code, ins->sreg1, ins->inst_destbasereg, loongarch_r21);
				} else {
					/* Convert to single-float precision */
					loongarch_lu12iw (code, loongarch_r21, (ins->inst_offset >> 12) & 0xfffff);
					loongarch_ori (code, loongarch_r21, loongarch_r21, ins->inst_offset & 0xfff);
					loongarch_fcvtsd (code, loongarch_ftemp, ins->sreg1);
					loongarch_fstxs (code, loongarch_ftemp, ins->inst_destbasereg, loongarch_r21);
				}
			}
			break;
		case OP_LOADR4_MEMBASE:
			if (loongarch_is_imm12 (ins->inst_offset)) {
				if (cfg->r4fp) {
					loongarch_flds (code, ins->dreg, ins->inst_basereg, ins->inst_offset & 0xfff);
				} else {
					loongarch_flds (code, ins->dreg, ins->inst_basereg, ins->inst_offset & 0xfff);
					/* Convert to double precision in place */
					loongarch_fcvtds (code, ins->dreg, ins->dreg);
				}
			} else {
				if (cfg->r4fp) {
					loongarch_lu12iw (code, loongarch_r21, (ins->inst_offset >> 12) & 0xfffff);
					loongarch_ori (code, loongarch_r21, loongarch_r21, ins->inst_offset & 0xfff);
					loongarch_fldxs (code, ins->dreg, ins->inst_basereg, loongarch_r21);
				} else {
					/* Convert to single-float precision */
					loongarch_lu12iw (code, loongarch_r21, (ins->inst_offset >> 12) & 0xfffff);
					loongarch_ori (code, loongarch_r21, loongarch_r21, ins->inst_offset & 0xfff);
					loongarch_fldxs (code, ins->dreg, ins->inst_basereg, loongarch_r21);
					/* Convert to double precision in place */
					loongarch_fcvtds (code, ins->dreg, ins->dreg);
				}
			}
			break;
		case OP_LOADR4_MEMINDEX:
			if (cfg->r4fp) {
				loongarch_fldxs (code, ins->dreg, ins->inst_basereg, ins->sreg2);
			} else {
				loongarch_fldxs (code, ins->dreg, ins->inst_basereg, ins->inst_offset);
				/* Convert to double precision in place */
				loongarch_fcvtds (code, ins->dreg, ins->dreg);
			}
			break;
		case OP_LOADR8_MEMINDEX:
			loongarch_fldxd (code, ins->dreg, ins->inst_basereg, ins->sreg2);
			break;
		case OP_STORER4_MEMINDEX:
			if (cfg->r4fp) {
				loongarch_fstxs (code, ins->dreg, ins->inst_basereg, ins->sreg2);
			} else {
				/* Convert to single-float precision */
				loongarch_fcvtsd (code, loongarch_ftemp, ins->dreg);
				loongarch_fstxs (code, loongarch_ftemp, ins->inst_basereg, ins->sreg2);
			}
			break;
		case OP_STORER8_MEMINDEX:
			loongarch_fstxd (code, ins->dreg, ins->inst_basereg, ins->sreg2);
			break;
		case OP_ICONV_TO_R_UN: {
			/* convert int to unsigned int */
			loongarch_bstrpickd (code, loongarch_r21, ins->sreg1, 31, 0);
			loongarch_movgr2frd (code, ins->dreg, loongarch_r21);
			/* convert unsigned int to double */
			loongarch_ffintdl (code, ins->dreg, ins->dreg);
			break;
		}
		case OP_LCONV_TO_R_UN:
			loongarch_movgr2frd (code, loongarch_ftemp, ins->sreg1);
			loongarch_bge (code, ins->sreg1, loongarch_zero, 4);
			loongarch_andi (code, loongarch_r21, ins->sreg1, 0x1);
			loongarch_srlid (code, ins->sreg1, ins->sreg1, 0x1);
			loongarch_or (code, ins->sreg1, ins->sreg1, loongarch_r21);
			loongarch_movgr2frd (code, ins->dreg, ins->sreg1);
			loongarch_ffintdl (code, ins->dreg, ins->dreg);
			loongarch_movfr2grd (code, ins->sreg1, loongarch_ftemp);
			loongarch_bge (code, ins->sreg1, loongarch_zero, 3);
			loongarch_fmovd (code, loongarch_ftemp, ins->dreg);
			loongarch_faddd (code, ins->dreg, loongarch_ftemp, ins->dreg);
			break;
		case OP_ICONV_TO_R4:
			if (cfg->r4fp) {
				loongarch_movgr2frw (code, ins->dreg, ins->sreg1);
				loongarch_ffintsw (code, ins->dreg, ins->dreg);
			} else {
				loongarch_movgr2frw (code, ins->dreg, ins->sreg1);
				loongarch_ffintsw (code, ins->dreg, ins->dreg);
				loongarch_fcvtds (code, ins->dreg, ins->dreg);
			}
			break;
		case OP_LCONV_TO_R4:
			if (cfg->r4fp) {
				loongarch_movgr2frd (code, ins->dreg, ins->sreg1);
				loongarch_ffintsl (code, ins->dreg, ins->dreg);
			} else {
				loongarch_movgr2frd (code, ins->dreg, ins->sreg1);
				loongarch_ffintsl (code, ins->dreg, ins->dreg);
				loongarch_fcvtds (code, ins->dreg, ins->dreg);
			}
			break;
		case OP_ICONV_TO_R8:
			loongarch_movgr2frw (code, ins->dreg, ins->sreg1);
			loongarch_ffintdw (code, ins->dreg, ins->dreg);
			break;
		case OP_LCONV_TO_R8:
			loongarch_movgr2frd (code, ins->dreg, ins->sreg1);
			loongarch_ffintdl (code, ins->dreg, ins->dreg);
			break;
		case OP_FCONV_TO_I1:
			loongarch_ftintrzld (code, loongarch_ftemp, ins->sreg1);
			loongarch_movfr2grs (code, ins->dreg, loongarch_ftemp);
			loongarch_extwb (code, ins->dreg, ins->dreg);
			break;
		case OP_FCONV_TO_U1:
			loongarch_lu52id (code, loongarch_r21, loongarch_zero, 0x43e);
			loongarch_movgr2frd (code, loongarch_ftemp, loongarch_r21);
			loongarch_fcmpcltd (code, 0x1, ins->sreg1, loongarch_ftemp);
			loongarch_ori (code, loongarch_r21, loongarch_zero, 0x0);
			loongarch_bcnez (code, 0x1, 0x4);
			loongarch_fsubd (code, loongarch_ftemp, ins->sreg1, loongarch_ftemp);
			loongarch_ori (code, loongarch_r21, loongarch_zero, 0x1);
			loongarch_sllid (code, loongarch_r21, loongarch_r21, 63);
			loongarch_fsel (code, loongarch_ftemp, loongarch_ftemp, ins->sreg1, 0x1);
			loongarch_ftintrzld (code, loongarch_ftemp, loongarch_ftemp);
			loongarch_movfr2grd (code, ins->dreg, loongarch_ftemp);
			loongarch_or (code, ins->dreg, loongarch_r21, ins->dreg);
			loongarch_bstrpickd (code, ins->dreg, ins->dreg, 0x7, 0x0);
			break;
		case OP_FCONV_TO_I2:
			loongarch_ftintrzld (code, loongarch_ftemp, ins->sreg1);
			loongarch_movfr2grs (code, ins->dreg, loongarch_ftemp);
			loongarch_extwh (code, ins->dreg, ins->dreg);
			break;
		case OP_FCONV_TO_U2:
			loongarch_lu52id (code, loongarch_r21, loongarch_zero, 0x43e);
			loongarch_movgr2frd (code, loongarch_ftemp, loongarch_r21);
			loongarch_fcmpcltd (code, 0x1, ins->sreg1, loongarch_ftemp);
			loongarch_ori (code, loongarch_r21, loongarch_zero, 0x0);
			loongarch_bcnez (code, 0x1, 0x4);
			loongarch_fsubd (code, loongarch_ftemp, ins->sreg1, loongarch_ftemp);
			loongarch_ori (code, loongarch_r21, loongarch_zero, 0x1);
			loongarch_sllid (code, loongarch_r21, loongarch_r21, 63);
			loongarch_fsel (code, loongarch_ftemp, loongarch_ftemp, ins->sreg1, 0x1);
			loongarch_ftintrzld (code, loongarch_ftemp, loongarch_ftemp);
			loongarch_movfr2grd (code, ins->dreg, loongarch_ftemp);
			loongarch_or (code, ins->dreg, loongarch_r21, ins->dreg);
			loongarch_bstrpickd (code, ins->dreg, ins->dreg, 15, 0);
			break;
		case OP_FCONV_TO_I4:
		case OP_FCONV_TO_I:
			loongarch_ftintrzld (code, loongarch_ftemp, ins->sreg1);
			loongarch_movfr2grs (code, ins->dreg, loongarch_ftemp);
			loongarch_slliw (code, ins->dreg, ins->dreg, 0);
			break;
		case OP_FCONV_TO_U4:
			loongarch_lu52id (code, loongarch_r21, loongarch_zero, 0x41e);
			loongarch_movgr2frd (code, loongarch_ftemp, loongarch_r21);
			loongarch_fcmpcltd (code, 0x1, ins->sreg1, loongarch_ftemp);
			loongarch_ori (code, loongarch_r21, loongarch_zero, 0x0);
			loongarch_bcnez (code, 0x1, 0x4);
			loongarch_fsubd (code, loongarch_ftemp, ins->sreg1, loongarch_ftemp);
			loongarch_ori (code, loongarch_r21, loongarch_zero, 0x1);
			loongarch_slliw (code, loongarch_r21, loongarch_r21, 31);
			loongarch_fsel (code, loongarch_ftemp, loongarch_ftemp, ins->sreg1, 0x1);
			loongarch_ftintrzwd (code, loongarch_ftemp, loongarch_ftemp);
			loongarch_movfr2grs (code, ins->dreg, loongarch_ftemp);
			loongarch_or (code, ins->dreg, loongarch_r21, ins->dreg);
			loongarch_bstrpickd (code, ins->dreg, ins->dreg, 31, 0);
			break;
		case OP_FCONV_TO_I8:
			loongarch_ftintrzld (code, loongarch_ftemp, ins->sreg1);
			loongarch_movfr2grd (code, ins->dreg, loongarch_ftemp);
			break;
		case OP_FCONV_TO_U8:
			loongarch_lu52id (code, loongarch_r21, loongarch_zero, 0x43e);
			loongarch_movgr2frd (code, loongarch_ftemp, loongarch_r21);
			loongarch_fcmpcltd (code, 0x1, ins->sreg1, loongarch_ftemp);
			loongarch_ori (code, loongarch_r21, loongarch_zero, 0x0);
			loongarch_bcnez (code, 0x1, 0x4);
			loongarch_fsubd (code, loongarch_ftemp, ins->sreg1, loongarch_ftemp);
			loongarch_ori (code, loongarch_r21, loongarch_zero, 0x1);
			loongarch_sllid (code, loongarch_r21, loongarch_r21, 63);
			loongarch_fsel (code, loongarch_ftemp, loongarch_ftemp, ins->sreg1, 0x1);
			loongarch_ftintrzld (code, loongarch_ftemp, loongarch_ftemp);
			loongarch_movfr2grd (code, ins->dreg, loongarch_ftemp);
			loongarch_or (code, ins->dreg, loongarch_r21, ins->dreg);
			break;
		case OP_FCONV_TO_R4:
			if (cfg->r4fp) {
				loongarch_fcvtsd (code, ins->dreg, ins->sreg1);
			} else {
				loongarch_fcvtsd (code, loongarch_ftemp, ins->sreg1);
				loongarch_fcvtds (code, ins->dreg, loongarch_ftemp);
			}
			break;
		case OP_SQRT:
			if (ins->type == STACK_R4)
				loongarch_fsqrts (code, ins->dreg, ins->sreg1);
			else
				loongarch_fsqrtd (code, ins->dreg, ins->sreg1);
			break;
		case OP_FADD:
			loongarch_faddd (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_FSUB:
			loongarch_fsubd (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_FMUL:
			loongarch_fmuld (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_FDIV:
			loongarch_fdivd (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_FNEG:
			loongarch_fnegd (code, ins->dreg, ins->sreg1);
			break;
		case OP_LA_SETFREG_R4:
			loongarch_fcvtsd (code, ins->dreg, ins->sreg1);
			break;

			/* R4 */
		case OP_RADD:
			loongarch_fadds (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_RSUB:
			loongarch_fsubs (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_RMUL:
			loongarch_fmuls (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_RDIV:
			loongarch_fdivs (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_RNEG:
			loongarch_fnegs (code, ins->dreg, ins->sreg1);
			break;
		case OP_RCONV_TO_I1:
			loongarch_ftintrzls (code, loongarch_ftemp, ins->sreg1);
			loongarch_movfr2grs (code, ins->dreg, loongarch_ftemp);
			loongarch_extwb (code, ins->dreg, ins->dreg);
			break;
		case OP_RCONV_TO_U1:
			loongarch_lu12iw (code, loongarch_r21, 0x5f000);
			loongarch_movgr2frw (code, loongarch_ftemp, loongarch_r21);
			loongarch_fcmpclts (code, 0x1, ins->sreg1, loongarch_ftemp);
			loongarch_ori (code, loongarch_r21, loongarch_zero, 0x0);
			loongarch_bcnez (code, 0x1, 0x4);
			loongarch_fsubs (code, loongarch_ftemp, ins->sreg1, loongarch_ftemp);
			loongarch_ori (code, loongarch_r21, loongarch_zero, 0x1);
			loongarch_sllid (code, loongarch_r21, loongarch_r21, 63);
			loongarch_fsel (code, loongarch_ftemp, loongarch_ftemp, ins->sreg1, 0x1);
			loongarch_ftintrzls (code, loongarch_ftemp, loongarch_ftemp);
			loongarch_movfr2grd (code, ins->dreg, loongarch_ftemp);
			loongarch_or (code, ins->dreg, loongarch_r21, ins->dreg);
			loongarch_bstrpickd (code, ins->dreg, ins->dreg, 0x7, 0x0);
			break;
		case OP_RCONV_TO_I2:
			loongarch_ftintrzls (code, loongarch_ftemp, ins->sreg1);
			loongarch_movfr2grs (code, ins->dreg, loongarch_ftemp);
			loongarch_extwh (code, ins->dreg, ins->dreg);
			break;
        case OP_RCONV_TO_U2:
			loongarch_lu12iw (code, loongarch_r21, 0x5f000);
			loongarch_movgr2frw (code, loongarch_ftemp, loongarch_r21);
			loongarch_fcmpclts (code, 0x1, ins->sreg1, loongarch_ftemp);
			loongarch_ori (code, loongarch_r21, loongarch_zero, 0x0);
			loongarch_bcnez (code, 0x1, 0x4);
			loongarch_fsubs (code, loongarch_ftemp, ins->sreg1, loongarch_ftemp);
			loongarch_ori (code, loongarch_r21, loongarch_zero, 0x1);
			loongarch_sllid (code, loongarch_r21, loongarch_r21, 63);
			loongarch_fsel (code, loongarch_ftemp, loongarch_ftemp, ins->sreg1, 0x1);
			loongarch_ftintrzls (code, loongarch_ftemp, loongarch_ftemp);
			loongarch_movfr2grd (code, ins->dreg, loongarch_ftemp);
			loongarch_or (code, ins->dreg, loongarch_r21, ins->dreg);
			loongarch_bstrpickd (code, ins->dreg, ins->dreg, 15, 0);
			break;
		case OP_RCONV_TO_I:
		case OP_RCONV_TO_I4:
			loongarch_ftintrzls (code, loongarch_ftemp, ins->sreg1);
			loongarch_movfr2grs (code, ins->dreg, loongarch_ftemp);
			loongarch_slliw (code, ins->dreg, ins->dreg, 0);
			break;
		case OP_RCONV_TO_U4:
			loongarch_lu12iw (code, loongarch_r21, 0x5f000);
			loongarch_movgr2frw (code, loongarch_ftemp, loongarch_r21);
			loongarch_fcmpclts (code, 0x1, ins->sreg1, loongarch_ftemp);
			loongarch_ori (code, loongarch_r21, loongarch_zero, 0x0);
			loongarch_bcnez (code, 0x1, 0x4);
			loongarch_fsubs (code, loongarch_ftemp, ins->sreg1, loongarch_ftemp);
			loongarch_ori (code, loongarch_r21, loongarch_zero, 0x1);
			loongarch_sllid (code, loongarch_r21, loongarch_r21, 63);
			loongarch_fsel (code, loongarch_ftemp, loongarch_ftemp, ins->sreg1, 0x1);
			loongarch_ftintrzls (code, loongarch_ftemp, loongarch_ftemp);
			loongarch_movfr2grd (code, ins->dreg, loongarch_ftemp);
			loongarch_or (code, ins->dreg, loongarch_r21, ins->dreg);
			loongarch_bstrpickd (code, ins->dreg, ins->dreg, 31, 0);
			break;
		case OP_RCONV_TO_I8:
			loongarch_ftintrzls (code, loongarch_ftemp, ins->sreg1);
			loongarch_movfr2grd (code, ins->dreg, loongarch_ftemp);
			break;
		case OP_RCONV_TO_U8:
			loongarch_lu12iw (code, loongarch_r21, 0x5f000);
			loongarch_movgr2frw (code, loongarch_ftemp, loongarch_r21);
			loongarch_fcmpclts (code, 0x1, ins->sreg1, loongarch_ftemp);
			loongarch_ori (code, loongarch_r21, loongarch_zero, 0x0);
			loongarch_bcnez (code, 0x1, 0x4);
			loongarch_fsubs (code, loongarch_ftemp, ins->sreg1, loongarch_ftemp);
			loongarch_ori (code, loongarch_r21, loongarch_zero, 0x1);
			loongarch_sllid (code, loongarch_r21, loongarch_r21, 63);
			loongarch_fsel (code, loongarch_ftemp, loongarch_ftemp, ins->sreg1, 0x1);
			loongarch_ftintrzls (code, loongarch_ftemp, loongarch_ftemp);
			loongarch_movfr2grd (code, ins->dreg, loongarch_ftemp);
			loongarch_or (code, ins->dreg, loongarch_r21, ins->dreg);
			break;
		case OP_RCONV_TO_R8:
			loongarch_fcvtds (code, ins->dreg, ins->sreg1);
			break;
		case OP_RCONV_TO_R4:
			if (ins->dreg != ins->sreg1)
				loongarch_fmovs (code, ins->dreg, ins->sreg1);
			break;

		case OP_CKFINITE: {
			/* Check for infinity */
			loongarch_addid (code, loongarch_r21, loongarch_zero, 0xfff);
			loongarch_lu52id (code, loongarch_r21, loongarch_r21, 0x7fe);

			loongarch_movgr2frd (code, loongarch_ftemp, loongarch_r21);
			loongarch_fabsd (code, loongarch_ftemp2, ins->sreg1);
			loongarch_fcmpcltd (code, 1, loongarch_ftemp, loongarch_ftemp2);//cc=1

			loongarch_pcaddi (code, loongarch_t0, 1);
			mono_add_patch_info_rel (cfg, code - cfg->native_code, MONO_PATCH_INFO_EXC, "ArithmeticException", MONO_R_LOONGARCH64_BZ);
			loongarch_bcnez (code, 1, 0);//cc=1

			/* Check for nans */
			loongarch_fcmpcund (code, 1, loongarch_ftemp2, loongarch_ftemp2);//cc=1
			loongarch_pcaddi (code, loongarch_t0, 1);
			mono_add_patch_info_rel (cfg, code - cfg->native_code, MONO_PATCH_INFO_EXC, "ArithmeticException", MONO_R_LOONGARCH64_BZ);
			loongarch_bcnez (code, 1, 0);//cc=1
			loongarch_fmovd (code, ins->dreg, ins->sreg1);
			break;
		}
		case OP_JUMP_TABLE:
			mono_add_patch_info_rel (cfg, code - cfg->native_code, (MonoJumpInfoType)(gsize)ins->inst_i1, ins->inst_p0, MONO_R_LOONGARCH64_J);
			loongarch_lu12iw (code, ins->dreg, 0);
			loongarch_lu32id (code, ins->dreg, 0);
			loongarch_lu52id (code, ins->dreg, ins->dreg, 0);
			loongarch_ori (code, ins->dreg, ins->dreg, 0);
			break;
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
		case OP_GC_SAFE_POINT: {
			guint8 *buf [1];
			loongarch_ldd (code, loongarch_r21, ins->sreg1, 0);
			buf [0] = code;
			/* Call it if it is non-null */
			loongarch_beqz (code, loongarch_r21, 0);
			mono_add_patch_info_rel (cfg, code - cfg->native_code, MONO_PATCH_INFO_JIT_ICALL_ID, GUINT_TO_POINTER (MONO_JIT_ICALL_mono_threads_state_poll), MONO_R_LOONGARCH64_BL);
			cfg->thunk_area += THUNK_SIZE;
			loongarch_bl (code, 0);
			mono_loongarch64_patch (buf [0], code, MONO_R_LOONGARCH64_BZ);
			break;
		}
			break;
		case OP_FILL_PROF_CALL_CTX:
			for (int i = 0; i < MONO_MAX_IREGS; i++)
				if ((MONO_ARCH_CALLEE_SAVED_REGS & (1 << i)) || i == loongarch_sp || i == loongarch_fp)
					loongarch_std (code, i, ins->sreg1, MONO_STRUCT_OFFSET (MonoContext, regs) + i * sizeof (target_mgreg_t));
			break;

		case OP_LA_IBEQ:
		case OP_LA_IBGE:
		case OP_LA_IBGT:
		case OP_LA_IBLE:
		case OP_LA_IBLT:
		case OP_LA_IBNE_UN:
		case OP_LA_IBGE_UN:
		case OP_LA_IBGT_UN:
		case OP_LA_IBLE_UN:
		case OP_LA_IBLT_UN: {
			gint src1;
			gint src2;
			gint cond;
			loongarch_slliw (code, ins->sreg1, ins->sreg1, 0x0);
			loongarch_slliw (code, ins->sreg2, ins->sreg2, 0x0);
			src1 = ins->sreg1;
			src2 = ins->sreg2;
			mono_add_patch_info_rel (cfg, offset + 8, MONO_PATCH_INFO_BB, ins->inst_true_bb, MONO_R_LOONGARCH64_BC);
			cond = opcode_to_loongarchcond (ins->opcode, &src1, &src2);
			loongarch_format_2rui (code, cond, 0, src1, src2);
			break;
		}
		case OP_LA_LBEQ:
		case OP_LA_LBGE:
		case OP_LA_LBGT:
		case OP_LA_LBLE:
		case OP_LA_LBLT:
		case OP_LA_LBNE_UN:
		case OP_LA_LBGE_UN:
		case OP_LA_LBGT_UN:
		case OP_LA_LBLE_UN:
		case OP_LA_LBLT_UN: {
			gint src1;
			gint src2;
			gint cond;
			src1 = ins->sreg1;
			src2 = ins->sreg2;
			mono_add_patch_info_rel (cfg, offset, MONO_PATCH_INFO_BB, ins->inst_true_bb, MONO_R_LOONGARCH64_BC);
			cond = opcode_to_loongarchcond (ins->opcode, &src1, &src2);
			loongarch_format_2rui (code, cond, 0, src1, src2);
			break;
		}
		case OP_FBEQ:
		case OP_FBNE_UN:
		case OP_FBLT:
		case OP_FBLT_UN:
		case OP_FBGT:
		case OP_FBGT_UN:
		case OP_FBLE:
		case OP_FBLE_UN:
		case OP_FBGE:
		case OP_FBGE_UN: {
			mono_add_patch_info_rel (cfg, offset, MONO_PATCH_INFO_BB, ins->inst_true_bb, MONO_R_LOONGARCH64_BZ);
			loongarch_bcnez (code, 1, 0);
			break;
		}
		case OP_LA_LBEQZ:
			mono_add_patch_info_rel (cfg, offset, MONO_PATCH_INFO_BB, ins->inst_true_bb, MONO_R_LOONGARCH64_BZ);
			loongarch_beqz (code, ins->sreg1, 0);
			break;
		case OP_LA_LBNEZ:
			mono_add_patch_info_rel (cfg, offset, MONO_PATCH_INFO_BB, ins->inst_true_bb, MONO_R_LOONGARCH64_BZ);
			loongarch_bnez (code, ins->sreg1, 0);
			break;

		default:
			g_warning ("unknown opcode %s in %s()\n", mono_inst_name (ins->opcode), __FUNCTION__);
			g_assert_not_reached ();
		}

		if ((cfg->opt & MONO_OPT_BRANCH) && ((code - cfg->native_code - offset) > max_len)) {
			g_warning ("wrong maximal instruction length of instruction %s (expected %d, got %d)",
				   mono_inst_name (ins->opcode), max_len, code - cfg->native_code - offset);
			g_assert_not_reached ();
		}

		cpos += max_len;

		last_ins = ins;
	}

	set_code_cursor (cfg, code);
	/*
	 * If the compiled code size is larger than the bcc displacement (16 bits signed),
	 * insert branch islands between/inside basic blocks.
	 */
	if (cfg->arch.cond_branch_islands > 0)
		code = emit_branch_island (cfg, code, start_offset);
}

/*
 * emit_store_regarray:
 *
 *   Emit code to store the registers in REGS into the appropriate elements of
 * the register array at BASEREG+OFFSET.
 */
__attribute__ ((__warn_unused_result__)) guint8*
mono_loongarch_emit_store_regarray (guint8 *code, guint64 regs, int basereg, int offset)
{
	int i;

	for (i = 0; i < 32; ++i) {
		if (regs & (1 << i)) {
			if (i == loongarch_sp) {
				loongarch_ori (code, loongarch_t0, loongarch_sp, 0);
				loongarch_std (code, loongarch_t0, basereg, offset + (i * 8));
			} else {
				loongarch_std (code, i, basereg, offset + (i * 8));
			}
		}
	}
	return code;
}

/*
 * emit_load_regarray:
 *
 *   Emit code to load the registers in REGS from the appropriate elements of
 * the register array at BASEREG+OFFSET.
 */
__attribute__ ((__warn_unused_result__)) guint8*
mono_loongarch_emit_load_regarray (guint8 *code, guint64 regs, int basereg, int offset)
{
	int i;

	for (i = 0; i < 32; ++i) {
		if (regs & (1 << i)) {
			loongarch_ldd (code, i, basereg, offset + (i * 8));
		}
	}
	return code;
}

/*
 * emit_store_regset:
 *
 *   Emit code to store the registers in REGS into consecutive memory locations starting
 * at BASEREG+OFFSET.
 */
__attribute__ ((__warn_unused_result__)) guint8*
mono_loongarch_emit_store_regset (guint8 *code, guint64 regs, int basereg, int offset)
{
	int i, pos;

	pos = 0;
	for (i = 0; i < 32; ++i) {
		if (regs & (1 << i)) {
			if (i == loongarch_sp) {
				loongarch_ori (code, loongarch_t0, loongarch_sp, 0);
				loongarch_std (code, loongarch_t0, basereg, offset + (pos * 8));
			} else {
				loongarch_std (code, i, basereg, offset + (pos * 8));
			}
			pos++;
		}
	}
	return code;
}

/*
 * emit_load_regset:
 *
 *   Emit code to load the registers in REGS from consecutive memory locations starting
 * at BASEREG+OFFSET.
 */
__attribute__ ((__warn_unused_result__)) guint8*
mono_loongarch_emit_load_regset (guint8 *code, guint64 regs, int basereg, int offset)
{
	int i, pos;

	pos = 0;
	for (i = 0; i < 32; ++i) {
		if (regs & (1 << i)) {
			if (i == loongarch_sp) {
				g_assert_not_reached ();
			} else {
				loongarch_ldd (code, i, basereg, offset + (pos * 8));
			}
			pos++;
		}
	}
	return code;
}

/*
 * Stack frame layout:
 * (Hight-addr)
 *      param area		 incoming arguments.
 *   ------------------- sp + cfg->stack_usage + cfg->param_area
 *   ------------------- //sp + cfg->arch.iregs_offset     ,
 *   	locals
 *   ------------------- //sp + cfg->arch.lmf_offset       ,
 *   	saved registers		s0-s8,fs0-fs7,  optional
 *   	MonoLMF structure	optional
 *      ra-incoming
 *      fp-incoming      <------  fp-current pointing.
 *   ------------------- sp + cfg->param_area
 *   	param area		 outgoing arguments.
 *   ------------------- sp
 * (Low-addr)
 *   	red zone
 */
guint8 *
mono_arch_emit_prolog (MonoCompile *cfg)
{
	MonoMethod *method = cfg->method;
	MonoMethodSignature *sig;
	MonoBasicBlock *bb;
	guint8 *code;
	int cfa_offset, max_offset;

	sig = mono_method_signature_internal (method);
	cfg->code_size = 256 + sig->param_count * 64;
	code = cfg->native_code = g_malloc (cfg->code_size);

	/* This can be unaligned */
	cfg->stack_offset = ALIGN_TO (cfg->stack_offset, MONO_ARCH_FRAME_ALIGNMENT);

	 // update the unwind info.
	mono_emit_unwind_op_def_cfa (cfg, code, loongarch_sp, 0);

	/* Setup frame */
	cfa_offset = cfg->stack_offset;
	if (loongarch_is_imm12 (-cfg->stack_offset)) {
		loongarch_addid (code, loongarch_sp, loongarch_sp, -cfa_offset);
	} else {
		////NOTE: Assuming the stack size is less than 2G.
		loongarch_lu12iw (code, loongarch_r21, (cfa_offset >> 12) & 0xfffff);
		loongarch_ori (code, loongarch_r21, loongarch_r21, cfa_offset & 0xfff);
		loongarch_subd (code, loongarch_sp, loongarch_sp, loongarch_r21);
	}
	mono_emit_unwind_op_def_cfa_offset (cfg, code, cfa_offset);
	loongarch_std (code, loongarch_fp, loongarch_sp, 0);
	loongarch_std (code, loongarch_ra, loongarch_sp, 8); //will be optimized in the future.
	mono_emit_unwind_op_offset (cfg, code, loongarch_fp, (- cfa_offset) + 0);
	mono_emit_unwind_op_offset (cfg, code, loongarch_ra, (- cfa_offset) + 8);
	loongarch_ori (code, loongarch_fp, loongarch_sp, 0);
	mono_emit_unwind_op_def_cfa_reg (cfg, code, loongarch_fp);
	if (cfg->param_area) {
		/* The param area is below the frame pointer */
		if (loongarch_is_imm12 (-cfg->param_area)) {
			loongarch_addid (code, loongarch_sp, loongarch_sp, -cfg->param_area);
		} else {
			////NOTE: Assuming the stack size is less than 2G.
			loongarch_lu12iw (code, loongarch_r21, (cfg->param_area >> 12) & 0xfffff);
			loongarch_ori (code, loongarch_r21, loongarch_r21, cfg->param_area & 0xfff);
			loongarch_subd (code, loongarch_sp, loongarch_sp, loongarch_r21);
		}
	}

	if (cfg->method->save_lmf) {
		/*
		 * The LMF should contain all the state required to be able to reconstruct the machine state
		 * at the current point of execution. Since the LMF is only read during EH, only callee
		 * saved etc. registers need to be saved.
		 * FIXME: Save callee saved fp regs, JITted code doesn't use them, but native code does, and they
		 * need to be restored during EH.
		 */

		/* pc */
		loongarch_pcaddi (code, loongarch_ra, 0);
		int pc_offsets = cfg->lmf_var->inst_offset + MONO_STRUCT_OFFSET (MonoLMF, pc);
		if (loongarch_is_imm12 (pc_offsets)) {
			loongarch_std (code, loongarch_ra, loongarch_fp, pc_offsets & 0xfff);
		} else {
			loongarch_lu12iw (code, loongarch_r21, (pc_offsets >> 12) & 0xfffff);
			loongarch_ori (code, loongarch_r21, loongarch_r21, pc_offsets & 0xfff);
			loongarch_stxd (code, loongarch_ra, loongarch_fp, loongarch_r21);
		}
		/* gregs + fp + sp */
		int offsets = cfg->lmf_var->inst_offset + MONO_STRUCT_OFFSET (MonoLMF, gregs);
		if (loongarch_is_imm12 (offsets + 80)) {
			loongarch_addid (code, loongarch_r21, loongarch_fp, offsets & 0xfff);
		} else {
			loongarch_lu12iw (code, loongarch_r21, (offsets >> 12) & 0xfffff);
			loongarch_ori (code, loongarch_r21, loongarch_r21, offsets & 0xfff);
			loongarch_addd (code, loongarch_r21, loongarch_fp, loongarch_r21);
		}
		/* Don't emit unwind info for sp/fp, they are already handled in the prolog */
		loongarch_std (code, loongarch_sp, loongarch_r21, 0);
		loongarch_std (code, loongarch_fp, loongarch_r21, 8);
		loongarch_std (code, loongarch_s0, loongarch_r21, 16);
		mono_emit_unwind_op_offset (cfg, code, loongarch_s0, -cfa_offset + offsets + 16);
		loongarch_std (code, loongarch_s1, loongarch_r21, 24);
		mono_emit_unwind_op_offset (cfg, code, loongarch_s1, -cfa_offset + offsets + 24);
		loongarch_std (code, loongarch_s2, loongarch_r21, 32);
		mono_emit_unwind_op_offset (cfg, code, loongarch_s2, -cfa_offset + offsets + 32);
		loongarch_std (code, loongarch_s3, loongarch_r21, 40);
		mono_emit_unwind_op_offset (cfg, code, loongarch_s3, -cfa_offset + offsets + 40);
		loongarch_std (code, loongarch_s4, loongarch_r21, 48);
		mono_emit_unwind_op_offset (cfg, code, loongarch_s4, -cfa_offset + offsets + 48);
		loongarch_std (code, loongarch_s5, loongarch_r21, 56);
		mono_emit_unwind_op_offset (cfg, code, loongarch_s5, -cfa_offset + offsets + 56);
		loongarch_std (code, loongarch_s6, loongarch_r21, 64);
		mono_emit_unwind_op_offset (cfg, code, loongarch_s6, -cfa_offset + offsets + 64);
		loongarch_std (code, loongarch_s7, loongarch_r21, 72);
		mono_emit_unwind_op_offset (cfg, code, loongarch_s7, -cfa_offset + offsets + 72);
		loongarch_std (code, loongarch_s8, loongarch_r21, 80);
		mono_emit_unwind_op_offset (cfg, code, loongarch_s8, -cfa_offset + offsets + 80);
	} else {
		/* Save gregs */
		int offsets = cfg->arch.saved_gregs_offset;
		int i = loongarch_s0;
		int saved_regs = (MONO_ARCH_CALLEE_SAVED_REGS & ((guint)cfg->used_int_regs)) >> loongarch_s0;

		while (saved_regs) {
			if (saved_regs & 1) {
				if (loongarch_is_imm12 (offsets)) {
					loongarch_std (code, i, loongarch_fp, offsets & 0xfff);
				} else {
					loongarch_lu12iw (code, loongarch_r21, (offsets >> 12) & 0xfffff);
					loongarch_ori (code, loongarch_r21, loongarch_r21, offsets & 0xfff);
					loongarch_stxd (code, i, loongarch_fp, loongarch_r21);
				}
				mono_emit_unwind_op_offset (cfg, code, i, -cfa_offset + offsets);
				offsets += 8;
			}
			saved_regs >>= 1;
			i++;
		};
	}

	/* Setup args reg */
	if (cfg->arch.args_reg) {
		/* The register was already saved above */

		if (loongarch_is_imm12 (cfg->stack_offset)) {
			loongarch_addid (code, cfg->arch.args_reg, loongarch_fp, cfg->stack_offset & 0xfff);
		} else {
			////NOTE: Assuming the args_reg's size is less than 2G.
			loongarch_lu12iw (code, loongarch_r21, (cfg->stack_offset >> 12) & 0xfffff);
			loongarch_ori (code, loongarch_r21, loongarch_r21, cfg->stack_offset & 0xfff);
			loongarch_addd (code, cfg->arch.args_reg, loongarch_fp, loongarch_r21);
		}
	}

	if (cfg->vret_addr) {
		MonoInst *ins = cfg->vret_addr;
		g_assert (ins->opcode == OP_REGOFFSET);
		if (sig->pinvoke) {
			loongarch_std (code, loongarch_a0, ins->inst_basereg, ins->inst_offset);
		} else {
			loongarch_ldd (code, loongarch_r21, cfg->arch.args_reg, 0x0);
			loongarch_std (code, loongarch_r21, ins->inst_basereg, ins->inst_offset);
		}
	}

	/* Save mrgctx received in MONO_ARCH_RGCTX_REG */
	if (cfg->rgctx_var) {
		MonoInst *ins = cfg->rgctx_var;

		g_assert (ins->opcode == OP_REGOFFSET);
		EMIT_STORE (code, MONO_ARCH_RGCTX_REG, ins->inst_basereg, ins->inst_offset, loongarch_r21);

		mono_add_var_location (cfg, cfg->rgctx_var, TRUE, MONO_ARCH_RGCTX_REG, 0, 0, code - cfg->native_code);
		mono_add_var_location (cfg, cfg->rgctx_var, FALSE, ins->inst_basereg, ins->inst_offset, code - cfg->native_code, 0);
	}

	/*
	 * Move arguments to their registers/stack locations.
	 */
	{
		MonoInst *ins;
		CallInfo *cinfo;
		ArgInfo *ainfo;
		int i;
		guint8 field_info;

		cinfo = cfg->arch.cinfo;
		g_assert (cinfo);
		for (i = 0; i < cinfo->nargs; ++i) {
			ainfo = cinfo->args + i;
			ins = cfg->args [i];

			if (ins->opcode == OP_REGVAR) {
				switch (ainfo->storage) {
				case ArgInIReg:
					loongarch_ori (code, ins->dreg, ainfo->reg, 0);
					if (i == 0 && sig->hasthis) {
						mono_add_var_location (cfg, ins, TRUE, ainfo->reg, 0, 0, code - cfg->native_code);
						mono_add_var_location (cfg, ins, TRUE, ins->dreg, 0, code - cfg->native_code, 0);
					}
					break;
				case ArgOnStack:
					switch (ainfo->slot_size) {
					case 1:
						g_assert (loongarch_is_imm12 (ainfo->offset));
						if (ainfo->sign)
							loongarch_ldb (code, ins->dreg, cfg->arch.args_reg, ainfo->offset);
						else
							loongarch_ldbu (code, ins->dreg, cfg->arch.args_reg, ainfo->offset);
						break;
					case 2:
						if (ainfo->sign)
							loongarch_ldh (code, ins->dreg, cfg->arch.args_reg, ainfo->offset);
						else
							loongarch_ldhu (code, ins->dreg, cfg->arch.args_reg, ainfo->offset);
						break;
					case 4:
						if (ainfo->sign)
							loongarch_ldw (code, ins->dreg, cfg->arch.args_reg, ainfo->offset);
						else
							loongarch_ldwu (code, ins->dreg, cfg->arch.args_reg, ainfo->offset);
						break;
					default:
						loongarch_ldd (code, ins->dreg, cfg->arch.args_reg, ainfo->offset);
						break;
					}
					break;
				default:
					g_assert_not_reached ();
					break;
				}
			} else {
				gboolean isR4 = ainfo->size == 4 ? TRUE : FALSE;
				switch (ainfo->storage) {
				case ArgInIReg:
					/* Stack slots for arguments have size 8 */
					loongarch_std (code, ainfo->reg, ins->inst_basereg, ins->inst_offset);
					if (i == 0 && sig->hasthis) {
						mono_add_var_location (cfg, ins, TRUE, ainfo->reg, 0, 0, code - cfg->native_code);
						mono_add_var_location (cfg, ins, FALSE, ins->inst_basereg, ins->inst_offset, code - cfg->native_code, 0);
					}
					break;
				case ArgInFReg:
					if (isR4)
						loongarch_fsts (code, ainfo->reg, ins->inst_basereg, ins->inst_offset);
					else
						loongarch_fstd (code, ainfo->reg, ins->inst_basereg, ins->inst_offset);
					break;
				case ArgStructByVal:
					field_info = ainfo->field_info;
					int second_exist = (field_info >> 2) & 0x1;
					int stor_type = (field_info >> 5) & 0x7;
					int fp1_size = (field_info & 0x2) ? 8 : 4;
					int fp2_size = (field_info & 0x10) ? 8 : 4;
					switch (stor_type) {
					case 0x1: //all store in the freg
						if (second_exist) {
							if (fp1_size == 4)
								loongarch_fsts (code, ainfo->freg, ins->inst_basereg, ins->inst_offset);
							else
								loongarch_fstd (code, ainfo->freg, ins->inst_basereg, ins->inst_offset);
							if (fp2_size == 4)
								loongarch_fsts (code, ainfo->freg + 1, ins->inst_basereg, ins->inst_offset + fp1_size);
							else
								loongarch_fstd (code, ainfo->freg + 1, ins->inst_basereg, ins->inst_offset + 8);
						} else {
							if (fp1_size == 4)
								loongarch_fsts (code, ainfo->freg, ins->inst_basereg, ins->inst_offset);
							else
								loongarch_fstd (code, ainfo->freg, ins->inst_basereg, ins->inst_offset);
						}
						break;
					case 0x2: //all store in the reg
						if (ainfo->size > 8) {
							loongarch_std (code, ainfo->reg, ins->inst_basereg, ins->inst_offset);
							loongarch_std (code, ainfo->reg + 1, ins->inst_basereg, ins->inst_offset + 8);
						} else {
							loongarch_std (code, ainfo->reg, ins->inst_basereg, ins->inst_offset);
						}
						break;
					case 0x3: //all store in stack
						break;
					case 0x4: //one store in reg another in stack
						loongarch_std (code, ainfo->reg, ins->inst_basereg, ins->inst_offset);
						loongarch_ldd (code, ainfo->reg, cfg->arch.args_reg, ainfo->offset); //from caller stack to callee stack
						loongarch_std (code, ainfo->reg, ins->inst_basereg, ins->inst_offset + 8);
						break;
					case 0x5: //one store in reg another in freg
						if (field_info & 0x1) {
							if (fp1_size == 4)
								loongarch_fsts (code, ainfo->freg, ins->inst_basereg, ins->inst_offset);
							else
								loongarch_fstd (code, ainfo->freg, ins->inst_basereg, ins->inst_offset);
							if (fp2_size == 4)
								loongarch_stw (code, ainfo->reg, ins->inst_basereg, ins->inst_offset + fp1_size);
							else
								loongarch_std (code, ainfo->reg, ins->inst_basereg, ins->inst_offset + 8);
						} else {
							if (fp1_size == 4)
								loongarch_stw (code, ainfo->reg, ins->inst_basereg, ins->inst_offset);
							else
								loongarch_std (code, ainfo->reg, ins->inst_basereg, ins->inst_offset);
							if (fp2_size == 4)
								loongarch_fsts (code, ainfo->freg, ins->inst_basereg, ins->inst_offset + fp1_size);
							else
								loongarch_fstd (code, ainfo->freg, ins->inst_basereg, ins->inst_offset + 8);
						}
						break;
					default:
						g_assert_not_reached ();
						break;
				}
					break;
				case ArgStructByRef: {
					if (((ainfo->field_info >> 5) & 0x7) == 3)
						continue;
					MonoInst *addr_arg = ins->inst_left;
					g_assert (ins->opcode == OP_VTARG_ADDR);
					g_assert (addr_arg->opcode == OP_REGOFFSET);
					loongarch_std (code, ainfo->reg, addr_arg->inst_basereg, addr_arg->inst_offset);
					break;
				}
				case ArgOnStack:
					break;
				default:
					g_assert_not_reached ();
					break;
				}
			}
		}
	}

	/* Initialize seq_point_info_var */
	if (cfg->arch.seq_point_info_var) {
		MonoInst *ins = cfg->arch.seq_point_info_var;

		/* Initialize the variable from a GOT slot */
		code = emit_aotconst (cfg, code, loongarch_r21, MONO_PATCH_INFO_SEQ_POINT_INFO, cfg->method);
		g_assert (ins->opcode == OP_REGOFFSET);
		loongarch_std (code, loongarch_r21, ins->inst_basereg, ins->inst_offset);

		/* Initialize ss_tramp_var */
		ins = cfg->arch.ss_tramp_var;
		g_assert (ins->opcode == OP_REGOFFSET);

		code = emit_ldd (code, loongarch_ra, loongarch_r21, MONO_STRUCT_OFFSET (SeqPointInfo, ss_tramp_addr));
		code = emit_std (code, loongarch_ra, ins->inst_basereg, ins->inst_offset);
	} else {
		MonoInst *ins;
		if (cfg->arch.ss_tramp_var) {
			/* Initialize ss_tramp_var */
			ins = cfg->arch.ss_tramp_var;
			g_assert (ins->opcode == OP_REGOFFSET);

			code = mono_loongarch_emit_imm64 (code, loongarch_r21, (guint64)&ss_trampoline);
			code = emit_std (code, loongarch_r21, ins->inst_basereg, ins->inst_offset);
		}

		if (cfg->arch.bp_tramp_var) {
			/* Initialize bp_tramp_var */
			ins = cfg->arch.bp_tramp_var;
			g_assert (ins->opcode == OP_REGOFFSET);

			code = mono_loongarch_emit_imm64 (code, loongarch_r21, (guint64)bp_trampoline);
			loongarch_std (code, loongarch_r21, ins->inst_basereg, ins->inst_offset);
		}
	}

	max_offset = 0;
	if (cfg->opt & MONO_OPT_BRANCH) {
		for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
			MonoInst *ins;
			bb->max_offset = max_offset;

			MONO_BB_FOR_EACH_INS (bb, ins) {
				max_offset += ins_get_size (ins->opcode);
			}
		}
	}
	if (max_offset <= 0x7fff * 4) {
		cfg->arch.cond_branch_islands = 0;
	} else if (max_offset > 0x7fff * 4 && max_offset <= 0xfffff * 4) {
		cfg->arch.cond_branch_islands = 1;
	} else {
		cfg->arch.cond_branch_islands = 2;
	}
	return code;
}

void
mono_arch_emit_epilog (MonoCompile *cfg)
{
	CallInfo *cinfo;
	guint8 *code;
	int max_epilog_size;
	int offsets;
	guint saved_regs =(MONO_ARCH_CALLEE_SAVED_REGS & (guint)cfg->used_int_regs) >> MONO_ARCH_FIRST_LMF_REG;

	max_epilog_size = 20*4;//now the max size is 20*4.
	code = realloc_code (cfg, max_epilog_size);

	if (cfg->method->save_lmf) {
		offsets = cfg->lmf_var->inst_offset + MONO_STRUCT_OFFSET (MonoLMF, gregs) + 16;
	} else {
		/* Restore gregs */
		offsets = cfg->arch.saved_gregs_offset;
	}
	g_assert ((offsets & 0x7) == 0);

	int i = loongarch_s0;
	while (saved_regs) {
		if (saved_regs & 1) {
			if (loongarch_is_imm12 (offsets)) {
				loongarch_ldd (code, i, loongarch_fp, offsets & 0xfff);
			} else {
				loongarch_lu12iw (code, loongarch_r21, (offsets >> 12) & 0xfffff);
				loongarch_ori (code, loongarch_r21, loongarch_r21, offsets & 0xfff);
				loongarch_ldxd (code, i, loongarch_fp, loongarch_r21);
			}
			offsets += 8;
		} else if (cfg->method->save_lmf) {
			offsets += 8;
		}
		saved_regs >>= 1;
		i++;
	};

	/* Load returned vtypes into registers if needed */
	cinfo = cfg->arch.cinfo;
	switch (cinfo->ret.storage) {
	case ArgStructByVal: {
		MonoInst *ins = cfg->ret;
		int field_info = cinfo->ret.field_info;

		if (field_info & 0x1f) {
			if (field_info & 1) {
				g_assert (cinfo->ret.freg == loongarch_f0);
				if (field_info & 2) {
					loongarch_fldd (code, loongarch_f0 /*cinfo->ret.freg*/, ins->inst_basereg, ins->inst_offset & 0xfff);
					offsets = 8;
				} else {
					loongarch_flds (code, loongarch_f0 /*cinfo->ret.freg*/, ins->inst_basereg, ins->inst_offset & 0xfff);
					offsets = 4;
				}
			} else {
				g_assert (cinfo->ret.reg == loongarch_a0);
				if (field_info & 2) {
					loongarch_ldd (code, loongarch_a0 /*cinfo->ret.reg*/, ins->inst_basereg, ins->inst_offset & 0xfff);
					offsets = 8;
				} else {
					loongarch_ldw (code, loongarch_a0 /*cinfo->ret.reg*/, ins->inst_basereg, ins->inst_offset & 0xfff);
					offsets = 4;
				}
			}

			if (field_info & 4) {
				if (field_info & 8) {
					i = field_info & 1 ? loongarch_f1 : loongarch_f0;
					if (field_info & 0x10) {
						loongarch_fldd (code, i, ins->inst_basereg, (ins->inst_offset + 8) & 0xfff);
					} else {
						loongarch_flds (code, i, ins->inst_basereg, (ins->inst_offset + offsets) & 0xfff);
					}
				} else {
					i = field_info & 1 ? loongarch_a0 : loongarch_a1;
					if (field_info & 0x10) {
						loongarch_ldd (code, i, ins->inst_basereg, (ins->inst_offset + 8) & 0xfff);
					} else {
						loongarch_ldw (code, i, ins->inst_basereg, (ins->inst_offset + offsets) & 0xfff);
					}
				}
			}
		} else {
			if (cinfo->ret.size > 8) {
				loongarch_ldd (code, loongarch_a0, ins->inst_basereg, ins->inst_offset & 0xfff);
				loongarch_ldd (code, loongarch_a1, ins->inst_basereg, (ins->inst_offset + 8) & 0xfff);
			} else {
				loongarch_ldd (code, loongarch_a0, ins->inst_basereg, ins->inst_offset & 0xfff);
			}
		}
		break;
	}
	default:
		break;
	}

	/* Destroy frame */
	code = mono_loongarch64_emit_destroy_frame (code, cfg->stack_offset);

	g_assert (code - (cfg->native_code + cfg->code_len) < max_epilog_size);

	loongarch_jirl (code, 0, loongarch_ra, 0);

	set_code_cursor (cfg, code);
}

void
mono_arch_emit_exceptions (MonoCompile *cfg)
{
	MonoJumpInfo *ji;
	MonoClass *exc_class;
	guint8 *code, *ip;
	guint8* exc_throw_pos [MONO_EXC_INTRINS_NUM];
	guint8 exc_throw_found [MONO_EXC_INTRINS_NUM];
	int i, id, size = 0;

	for (i = 0; i < MONO_EXC_INTRINS_NUM; i++) {
		exc_throw_pos [i] = NULL;
		exc_throw_found [i] = 0;
	}

	for (ji = cfg->patch_info; ji; ji = ji->next) {
		if (ji->type == MONO_PATCH_INFO_EXC) {
			i = mini_exception_id_by_name ((const char*)ji->data.target);
			if (!exc_throw_found [i]) {
				size += 40;
				exc_throw_found [i] = TRUE;
			}
		}
	}

	code = realloc_code (cfg, size);

	/* Emit code to raise corlib exceptions */
	for (ji = cfg->patch_info; ji; ji = ji->next) {
		if (ji->type != MONO_PATCH_INFO_EXC)
			continue;

		ip = cfg->native_code + ji->ip.i;

		id = mini_exception_id_by_name ((const char*)ji->data.target);

		if (exc_throw_pos [id]) {
			/* ip points to the bcc () in OP_COND_EXC_... */
			mono_loongarch64_patch (ip, exc_throw_pos [id], ji->relocation);
			ji->type = MONO_PATCH_INFO_NONE;
			continue;
		}

		exc_throw_pos [id] = code;
		mono_loongarch64_patch (ip, code, ji->relocation);

		/* We are being branched to from the code generated by emit_cond_exc (), the pc is in ip1 */

		/* r0 = type token */
		exc_class = mono_class_load_from_name (mono_defaults.corlib, "System", ji->data.name);
		code = mono_loongarch_emit_imm64 (code, loongarch_a0, m_class_get_type_token (exc_class) - MONO_TOKEN_TYPE_DEF);
		/* r1 = throw ip */
		loongarch_ori (code, loongarch_a1, loongarch_t0, 0);
		/* Branch to the corlib exception throwing trampoline */
		ji->ip.i = code - cfg->native_code;
		ji->type = MONO_PATCH_INFO_JIT_ICALL_ID;
		ji->data.jit_icall_id = MONO_JIT_ICALL_mono_arch_throw_corlib_exception;
		ji->relocation = MONO_R_LOONGARCH64_BL;
		loongarch_bl (code, 0);
		cfg->thunk_area += THUNK_SIZE;
		set_code_cursor (cfg, code);
	}

	set_code_cursor (cfg, code);
}

MonoInst*
mono_arch_emit_inst_for_method (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	return NULL;
}

guint32
mono_arch_get_patch_offset (guint8 *code)
{
	return 0;
}

#define ENABLE_WRONG_METHOD_CHECK 0
#define LOONGARCH64_LOAD_SEQUENCE_LENGTH 16
#define CMP_SIZE (LOONGARCH64_LOAD_SEQUENCE_LENGTH + 4)
#define BR_SIZE 4
#define LOADSTORE_SIZE 4
#define JUMP_IMM_SIZE 16
#define JUMP_IMM32_SIZE (LOONGARCH64_LOAD_SEQUENCE_LENGTH + 8)
#define LOAD_CONST_SIZE 16
#define JUMP_JR_SIZE 4

/*
 * LOCKING: called with the domain lock held
 */
gpointer
mono_arch_build_imt_trampoline (MonoVTable *vtable, MonoIMTCheckItem **imt_entries, int count,
								gpointer fail_tramp)
{
	int i;
	int size = 0;
	guint8 *code, *start, *patch;

	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];

		if (item->is_equals) {
			if (item->check_target_idx) {
				item->chunk_size += LOAD_CONST_SIZE + BR_SIZE + JUMP_JR_SIZE;
				if (item->has_target_code) {
					item->chunk_size += LOAD_CONST_SIZE;
				} else {
					if (loongarch_is_imm12 (sizeof (target_mgreg_t) * item->value.vtable_slot)) {
						item->chunk_size += LOADSTORE_SIZE;
					} else {
						item->chunk_size += LOONGARCH64_LOAD_SEQUENCE_LENGTH;
						item->chunk_size += LOADSTORE_SIZE;
					}
				}
			} else {
				if (fail_tramp) {
					item->chunk_size += 3 * LOAD_CONST_SIZE + BR_SIZE + 2 * JUMP_JR_SIZE;
					if (!item->has_target_code) {
						if (loongarch_is_imm12 (sizeof (target_mgreg_t) * item->value.vtable_slot))
							item->chunk_size -= LOONGARCH64_LOAD_SEQUENCE_LENGTH;
						item->chunk_size += LOADSTORE_SIZE;
					}
				} else {
					if (loongarch_is_imm12 (sizeof (target_mgreg_t) * item->value.vtable_slot)) {
						item->chunk_size += LOADSTORE_SIZE;
					} else {
						item->chunk_size += LOONGARCH64_LOAD_SEQUENCE_LENGTH;
						item->chunk_size += LOADSTORE_SIZE + JUMP_JR_SIZE;
					}
#if ENABLE_WRONG_METHOD_CHECK
					item->chunk_size += CMP_SIZE + BR_SIZE + 4;
#endif
				}
			}
		} else {
			item->chunk_size += LOAD_CONST_SIZE + BR_SIZE + 4;
			imt_entries [item->check_target_idx]->compare_done = TRUE;
		}
		size += item->chunk_size;
	}
	size += LOONGARCH64_LOAD_SEQUENCE_LENGTH;
	/* the initial load of the vtable address */
	if (fail_tramp) {
		code = (guint8 *)mini_alloc_generic_virtual_trampoline (vtable, size);
	} else {
		MonoMemoryManager *mem_manager = m_class_get_mem_manager (vtable->klass);
		code = mono_mem_manager_code_reserve (mem_manager, size);
	}
	start = code;
	code = mono_loongarch_emit_imm64 (code, loongarch_t7, (gint64)(& (vtable->vtable [0])));

	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];

		item->code_target = code;
		if (item->is_equals) {
			if (item->check_target_idx) {
				code = mono_loongarch_emit_imm64 (code, loongarch_temp, (gint64)item->key);
				item->jmp_code = code;
				loongarch_bne (code, loongarch_temp, MONO_ARCH_IMT_REG, 0);
				//loongarch_nop (code);
				if (item->has_target_code) {
					code = mono_loongarch_emit_imm64 (code, loongarch_r21, (gint64)item->value.target_code);
				}
				else {
					if (loongarch_is_imm12 (sizeof (target_mgreg_t) * item->value.vtable_slot)) {
						loongarch_ldd (code, loongarch_r21, loongarch_t7, (sizeof (target_mgreg_t) * item->value.vtable_slot));
					} else {
						code = mono_loongarch_emit_imm64 (code, loongarch_r21, (sizeof (target_mgreg_t) * item->value.vtable_slot));
						loongarch_ldxd (code, loongarch_r21, loongarch_t7, loongarch_r21);
					}
				}
				loongarch_jirl (code, 0, loongarch_r21, 0);
			} else {
				if (fail_tramp) {
					code = mono_loongarch_emit_imm64 (code, loongarch_temp, (gint64)item->key);
					patch = code;
					loongarch_bne (code, loongarch_temp, MONO_ARCH_IMT_REG, 0);
					//loongarch_nop (code);
					if (item->has_target_code) {
						code = mono_loongarch_emit_imm64 (code, loongarch_r21, (gint64)item->value.target_code);
					} else {
						g_assert (vtable);
						if (loongarch_is_imm12 (sizeof (target_mgreg_t) * item->value.vtable_slot)) {
							loongarch_ldd (code, loongarch_r21, loongarch_t7, sizeof (target_mgreg_t) * item->value.vtable_slot);
						} else {
							code = mono_loongarch_emit_imm64 (code, loongarch_r21, sizeof (target_mgreg_t) * item->value.vtable_slot);
							loongarch_ldxd (code, loongarch_r21, loongarch_t7, loongarch_r21);
						}
					}
					loongarch_jirl (code, 0, loongarch_r21, 0);
					mono_loongarch64_patch (patch, code, MONO_R_LOONGARCH64_BC);
					code = mono_loongarch_emit_imm64 (code, loongarch_r21, (gint64)fail_tramp);
					loongarch_jirl (code, 0, loongarch_r21, 0);
					//loongarch_nop (code);
				} else {
					/* enable the commented code to assert on wrong method */
#if ENABLE_WRONG_METHOD_CHECK
					ppc_load (code, ppc_r0, (guint32)item->key);
					ppc_compare_log (code, 0, MONO_ARCH_IMT_REG, ppc_r0);
					patch = code;
					ppc_bc (code, PPC_BR_FALSE, PPC_BR_EQ, 0);
#endif
					if (loongarch_is_imm12 (sizeof (target_mgreg_t) * item->value.vtable_slot)) {
						loongarch_ldd (code, loongarch_r21, loongarch_t7, (sizeof (target_mgreg_t) * item->value.vtable_slot));
					} else {
						code = mono_loongarch_emit_imm64 (code, loongarch_r21, sizeof (target_mgreg_t) * item->value.vtable_slot);
						loongarch_ldxd (code, loongarch_r21, loongarch_t7, loongarch_r21);
					}
					loongarch_jirl (code, 0, loongarch_r21, 0);

#if ENABLE_WRONG_METHOD_CHECK
					ppc_patch (patch, code);
					ppc_break (code);
#endif
				}
			}
		} else {
			code = mono_loongarch_emit_imm64 (code, loongarch_temp, (gint64)item->key);
			loongarch_slt (code, loongarch_temp, MONO_ARCH_IMT_REG, loongarch_temp);

			item->jmp_code = code;
			loongarch_beq (code, loongarch_temp, loongarch_zero, 0);
			//loongarch_nop (code);
		}
	}
	/* patch the branches to get to the target items */
	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		if (item->jmp_code && item->check_target_idx) {
			mono_loongarch64_patch (item->jmp_code, imt_entries [item->check_target_idx]->code_target, MONO_R_LOONGARCH64_BC);
		}
	}
	g_assert (code - start <= size);
	mono_arch_flush_icache (start, size);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_IMT_TRAMPOLINE, NULL));

	mono_tramp_info_register (mono_tramp_info_create (NULL, start, code - start, NULL, NULL), m_class_get_mem_manager (vtable->klass));

	return start;
}

GSList *
mono_arch_get_trampolines (gboolean aot)
{
	return mono_loongarch_get_exception_trampolines (aot);
}

#else /* DISABLE_JIT */

gpointer
mono_arch_build_imt_trampoline (MonoVTable *vtable, MonoDomain *domain, MonoIMTCheckItem **imt_entries, int count,
								gpointer fail_tramp)
{
	g_assert_not_reached ();
	return NULL;
}

#endif /* !DISABLE_JIT */

/* Soft Debug support */
#ifdef MONO_ARCH_SOFT_DEBUG_SUPPORTED

/*
 * mono_arch_set_breakpoint:
 *
 *   See mini-amd64.c for docs.
 */
void
mono_arch_set_breakpoint (MonoJitInfo *ji, guint8 *ip)
{
	guint8 *code = MINI_FTNPTR_TO_ADDR (ip);
	guint32 native_offset = GPTRDIFF_TO_UINT32 (ip - (guint8*)ji->code_start);

	if (ji->from_aot) {
		SeqPointInfo *info = mono_arch_get_seq_point_info ((guint8*)ji->code_start);

		g_assert (native_offset % 4 == 0);
		g_assert (info->bp_addrs [native_offset / 4] == 0);
		info->bp_addrs [native_offset / 4] = (guint8*)mini_get_breakpoint_trampoline ();
	} else {
		/* ip points to an ldrx */
		code += 4;
		mono_codeman_enable_write ();
		loongarch_jirl (code, 1, loongarch_t0, 0);
		mono_codeman_disable_write ();
		mono_arch_flush_icache (ip, code - ip);
	}
}

/*
 * mono_arch_clear_breakpoint:
 *
 *   See mini-amd64.c for docs.
 */
void
mono_arch_clear_breakpoint (MonoJitInfo *ji, guint8 *ip)
{
	guint8 *code = MINI_FTNPTR_TO_ADDR (ip);

	if (ji->from_aot) {
		guint32 native_offset = GPTRDIFF_TO_UINT32 (ip - (guint8*)ji->code_start);
		SeqPointInfo *info = mono_arch_get_seq_point_info ((guint8*)ji->code_start);

		g_assert (native_offset % 4 == 0);
		info->bp_addrs [native_offset / 4] = NULL;
	} else {
		/* ip points to an ldrx */
		code += 4;
		mono_codeman_enable_write ();
		loongarch_nop (code);
		mono_codeman_disable_write ();
		mono_arch_flush_icache (ip, code - ip);
	}
}

/*
 * mono_arch_start_single_stepping:
 *
 *   See mini-amd64.c for docs.
 */
void
mono_arch_start_single_stepping (void)
{
	ss_trampoline = mini_get_single_step_trampoline ();
}

/*
 * mono_arch_stop_single_stepping:
 *
 *   See mini-amd64.c for docs.
 */
void
mono_arch_stop_single_stepping (void)
{
	ss_trampoline = NULL;
}

/*
 * mono_arch_is_single_step_event:
 *
 *   See mini-amd64.c for docs.
 */
gboolean
mono_arch_is_single_step_event (void *info, void *sigctx)
{
	return FALSE;
}

/*
 * mono_arch_is_breakpoint_event:
 *
 *   See mini-amd64.c for docs.
 */
gboolean
mono_arch_is_breakpoint_event (void *info, void *sigctx)
{
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
	g_assert_not_reached ();
}

/*
 * mono_arch_skip_single_step:
 *
 *   See mini-amd64.c for docs.
 */
void
mono_arch_skip_single_step (MonoContext *ctx)
{
	g_assert_not_reached ();
}

/*
 * mono_arch_get_seq_point_info:
 *
 *   See mini-amd64.c for docs.
 */
SeqPointInfo*
mono_arch_get_seq_point_info (guint8 *code)
{
	SeqPointInfo *info;
	MonoJitInfo *ji;
	MonoJitMemoryManager *jit_mm;

	jit_mm = get_default_jit_mm ();

	// FIXME: Add a free function

	jit_mm_lock (jit_mm);
	info = (SeqPointInfo *)g_hash_table_lookup (jit_mm->arch_seq_points, code);
	jit_mm_unlock (jit_mm);

	if (!info) {
		ji = mini_jit_info_table_find (code);
		g_assert (ji);

		info = g_malloc0 (sizeof (SeqPointInfo) + (ji->code_size / 4) * sizeof(guint8*));

		info->ss_tramp_addr = &ss_trampoline;

		jit_mm_lock (jit_mm);
		g_hash_table_insert (jit_mm->arch_seq_points, code, info);
		jit_mm_unlock (jit_mm);
	}

	return info;
}

#endif /* MONO_ARCH_SOFT_DEBUG_SUPPORTED */

gboolean
mono_arch_opcode_supported (int opcode)
{
	return FALSE;
}

gpointer
mono_arch_load_function (MonoJitICallId jit_icall_id)
{
	return NULL;
}

GSList*
mono_arch_get_cie_program (void)
{
	NOT_IMPLEMENTED;
	return NULL;
}
