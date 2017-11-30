/**
 * \file
 * JIT trampoline code for x86
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>

#include <mono/metadata/abi-details.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/gc-internals.h>
#include <mono/arch/x86/x86-codegen.h>

#include <mono/utils/memcheck.h>

#include "mini.h"
#include "mini-x86.h"
#include "mini-runtime.h"
#include "debugger-agent.h"
#include "jit-icalls.h"

#define ALIGN_TO(val,align) ((((guint64)val) + ((align) - 1)) & ~((align) - 1))

/*
 * mono_arch_get_unbox_trampoline:
 * @m: method pointer
 * @addr: pointer to native code for @m
 *
 * when value type methods are called through the vtable we need to unbox the
 * this argument. This method returns a pointer to a trampoline which does
 * unboxing before calling the method
 */
gpointer
mono_arch_get_unbox_trampoline (MonoMethod *m, gpointer addr)
{
	guint8 *code, *start;
	int this_pos = 4, size = 16;
	MonoDomain *domain = mono_domain_get ();
	GSList *unwind_ops;

	start = code = mono_domain_code_reserve (domain, size);

	unwind_ops = mono_arch_get_cie_program ();

	x86_alu_membase_imm (code, X86_ADD, X86_ESP, this_pos, sizeof (MonoObject));
	x86_jump_code (code, addr);
	g_assert ((code - start) < size);

	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_UNBOX_TRAMPOLINE, m));

	mono_tramp_info_register (mono_tramp_info_create (NULL, start, code - start, NULL, unwind_ops), domain);

	return start;
}

gpointer
mono_arch_get_static_rgctx_trampoline (gpointer arg, gpointer addr)
{
	guint8 *code, *start;
	int buf_len;
	GSList *unwind_ops;

	MonoDomain *domain = mono_domain_get ();

	buf_len = 10;

	start = code = mono_domain_code_reserve (domain, buf_len);

	unwind_ops = mono_arch_get_cie_program ();

	x86_mov_reg_imm (code, MONO_ARCH_RGCTX_REG, arg);
	x86_jump_code (code, addr);
	g_assert ((code - start) <= buf_len);

	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE, NULL));

	mono_tramp_info_register (mono_tramp_info_create (NULL, start, code - start, NULL, unwind_ops), domain);

	return start;
}

void
mono_arch_patch_callsite (guint8 *method_start, guint8 *orig_code, guint8 *addr)
{
	guint8 *code;
	guint8 buf [8];
	gboolean can_write = mono_breakpoint_clean_code (method_start, orig_code, 8, buf, sizeof (buf));

	code = buf + 8;

	/* go to the start of the call instruction
	 *
	 * address_byte = (m << 6) | (o << 3) | reg
	 * call opcode: 0xff address_byte displacement
	 * 0xff m=1,o=2 imm8
	 * 0xff m=2,o=2 imm32
	 */
	code -= 6;
	orig_code -= 6;
	if (code [1] == 0xe8) {
		if (can_write) {
			mono_atomic_xchg_i32 ((gint32*)(orig_code + 2), (guint)addr - ((guint)orig_code + 1) - 5);

			/* Tell valgrind to recompile the patched code */
			VALGRIND_DISCARD_TRANSLATIONS (orig_code + 2, 4);
		}
	} else if (code [1] == 0xe9) {
		/* A PLT entry: jmp <DISP> */
		if (can_write)
			mono_atomic_xchg_i32 ((gint32*)(orig_code + 2), (guint)addr - ((guint)orig_code + 1) - 5);
	} else {
		printf ("Invalid trampoline sequence: %x %x %x %x %x %x %x\n", code [0], code [1], code [2], code [3],
				code [4], code [5], code [6]);
		g_assert_not_reached ();
	}
}

void
mono_arch_patch_plt_entry (guint8 *code, gpointer *got, mgreg_t *regs, guint8 *addr)
{
	guint32 offset;

	/* Patch the jump table entry used by the plt entry */

	/* A PLT entry: jmp *<DISP>(%ebx) */
	g_assert (code [0] == 0xff);
	g_assert (code [1] == 0xa3);

	offset = *(guint32*)(code + 2);
	if (!got)
		got = (gpointer*)(gsize) regs [MONO_ARCH_GOT_REG];
	*(guint8**)((guint8*)got + offset) = addr;
}

static gpointer
get_vcall_slot (guint8 *code, mgreg_t *regs, int *displacement)
{
	const int kBufSize = 8;
	guint8 buf [64];
	guint8 reg = 0;
	gint32 disp = 0;

	mono_breakpoint_clean_code (NULL, code, kBufSize, buf, kBufSize);
	code = buf + 8;

	*displacement = 0;

	if ((code [0] == 0xff) && ((code [1] & 0x18) == 0x10) && ((code [1] >> 6) == 2)) {
		reg = code [1] & 0x07;
		disp = *((gint32*)(code + 2));
	} else {
		g_assert_not_reached ();
		return NULL;
	}

	*displacement = disp;
	return (gpointer)regs [reg];
}

static gpointer*
get_vcall_slot_addr (guint8* code, mgreg_t *regs)
{
	gpointer vt;
	int displacement;
	vt = get_vcall_slot (code, regs, &displacement);
	if (!vt)
		return NULL;
	return (gpointer*)((char*)vt + displacement);
}

guchar*
mono_arch_create_generic_trampoline (MonoTrampolineType tramp_type, MonoTrampInfo **info, gboolean aot)
{
	char *tramp_name;
	guint8 *buf, *code, *tramp, *br_ex_check;
	GSList *unwind_ops = NULL;
	MonoJumpInfo *ji = NULL;
	int i, offset, frame_size, regarray_offset, lmf_offset, caller_ip_offset, arg_offset;
	int cfa_offset; /* cfa = cfa_reg + cfa_offset */

	code = buf = mono_global_codeman_reserve (256);

	/* Note that there is a single argument to the trampoline
	 * and it is stored at: esp + pushed_args * sizeof (gpointer)
	 * the ret address is at: esp + (pushed_args + 1) * sizeof (gpointer)
	 */

	/* Compute frame offsets relative to the frame pointer %ebp */
	arg_offset = sizeof (mgreg_t);
	caller_ip_offset = 2 * sizeof (mgreg_t);
	offset = 0;
	offset += sizeof (MonoLMF);
	lmf_offset = -offset;
	offset += X86_NREG * sizeof (mgreg_t);
	regarray_offset = -offset;
	/* Argument area */
	offset += 4 * sizeof (mgreg_t);
	frame_size = ALIGN_TO (offset, MONO_ARCH_FRAME_ALIGNMENT);

	/* ret addr and arg are on the stack */
	cfa_offset = 2 * sizeof (mgreg_t);
	mono_add_unwind_op_def_cfa (unwind_ops, code, buf, X86_ESP, cfa_offset);
	// IP saved at CFA - 4
	mono_add_unwind_op_offset (unwind_ops, code, buf, X86_NREG, -4);

	/* Allocate frame */
	x86_push_reg (code, X86_EBP);
	cfa_offset += sizeof (mgreg_t);
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, buf, cfa_offset);
	mono_add_unwind_op_offset (unwind_ops, code, buf, X86_EBP, -cfa_offset);

	x86_mov_reg_reg (code, X86_EBP, X86_ESP, sizeof (mgreg_t));
	mono_add_unwind_op_def_cfa_reg (unwind_ops, code, buf, X86_EBP);

	/* There are three words on the stack, adding + 4 aligns the stack to 16, which is needed on osx */
	x86_alu_reg_imm (code, X86_SUB, X86_ESP, frame_size + sizeof (mgreg_t));

	/* Save all registers */
	for (i = X86_EAX; i <= X86_EDI; ++i) {
		int reg = i;

		if (i == X86_EBP) {
			/* Save original ebp */
			/* EAX is already saved */
			x86_mov_reg_membase (code, X86_EAX, X86_EBP, 0, sizeof (mgreg_t));
			reg = X86_EAX;
		} else if (i == X86_ESP) {
			/* Save original esp */
			/* EAX is already saved */
			x86_mov_reg_reg (code, X86_EAX, X86_EBP, sizeof (mgreg_t));
			/* Saved ebp + trampoline arg + return addr */
			x86_alu_reg_imm (code, X86_ADD, X86_EAX, 3 * sizeof (mgreg_t));
			reg = X86_EAX;
		}
		x86_mov_membase_reg (code, X86_EBP, regarray_offset + (i * sizeof (mgreg_t)), reg, sizeof (mgreg_t));
	}

	/* Setup LMF */
	/* eip */
	if (tramp_type == MONO_TRAMPOLINE_JUMP) {
		x86_mov_membase_imm (code, X86_EBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, eip), 0, sizeof (mgreg_t));
	} else {
		x86_mov_reg_membase (code, X86_EAX, X86_EBP, caller_ip_offset, sizeof (mgreg_t));
		x86_mov_membase_reg (code, X86_EBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, eip), X86_EAX, sizeof (mgreg_t));
	}
	/* method */
	if ((tramp_type == MONO_TRAMPOLINE_JIT) || (tramp_type == MONO_TRAMPOLINE_JUMP)) {
		x86_mov_reg_membase (code, X86_EAX, X86_EBP, arg_offset, sizeof (mgreg_t));
		x86_mov_membase_reg (code, X86_EBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, method), X86_EAX, sizeof (mgreg_t));
	} else {
		x86_mov_membase_imm (code, X86_EBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, method), 0, sizeof (mgreg_t));
	}
	/* esp */
	x86_mov_reg_membase (code, X86_EAX, X86_EBP, regarray_offset + (X86_ESP * sizeof (mgreg_t)), sizeof (mgreg_t));
	x86_mov_membase_reg (code, X86_EBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, esp), X86_EAX, sizeof (mgreg_t));
	/* callee save registers */
	x86_mov_reg_membase (code, X86_EAX, X86_EBP, regarray_offset + (X86_EBX * sizeof (mgreg_t)), sizeof (mgreg_t));
	x86_mov_membase_reg (code, X86_EBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, ebx), X86_EAX, sizeof (mgreg_t));
	x86_mov_reg_membase (code, X86_EAX, X86_EBP, regarray_offset + (X86_EDI * sizeof (mgreg_t)), sizeof (mgreg_t));
	x86_mov_membase_reg (code, X86_EBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, edi), X86_EAX, sizeof (mgreg_t));
	x86_mov_reg_membase (code, X86_EAX, X86_EBP, regarray_offset + (X86_ESI * sizeof (mgreg_t)), sizeof (mgreg_t));
	x86_mov_membase_reg (code, X86_EBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, esi), X86_EAX, sizeof (mgreg_t));
	x86_mov_reg_membase (code, X86_EAX, X86_EBP, regarray_offset + (X86_EBP * sizeof (mgreg_t)), sizeof (mgreg_t));
	x86_mov_membase_reg (code, X86_EBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, ebp), X86_EAX, sizeof (mgreg_t));

	/* Push LMF */
	/* get the address of lmf for the current thread */
	if (aot) {
		code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, "mono_get_lmf_addr");
		x86_call_reg (code, X86_EAX);
	} else {
		x86_call_code (code, mono_get_lmf_addr);
	}
	/* lmf->lmf_addr = lmf_addr (%eax) */
	x86_mov_membase_reg (code, X86_EBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, lmf_addr), X86_EAX, sizeof (mgreg_t));
	/* lmf->previous_lmf = *(lmf_addr) */
	x86_mov_reg_membase (code, X86_ECX, X86_EAX, 0, sizeof (mgreg_t));
	/* Signal to mono_arch_unwind_frame () that this is a trampoline frame */
	x86_alu_reg_imm (code, X86_ADD, X86_ECX, 1);
	x86_mov_membase_reg (code, X86_EBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, previous_lmf), X86_ECX, sizeof (mgreg_t));
	/* *lmf_addr = lmf */
	x86_lea_membase (code, X86_ECX, X86_EBP, lmf_offset);
	x86_mov_membase_reg (code, X86_EAX, 0, X86_ECX, sizeof (mgreg_t));

	/* Call trampoline function */
	/* Arg 1 - registers */
	x86_lea_membase (code, X86_EAX, X86_EBP, regarray_offset);
	x86_mov_membase_reg (code, X86_ESP, (0 * sizeof (mgreg_t)), X86_EAX, sizeof (mgreg_t));
	/* Arg2 - calling code */
	if (tramp_type == MONO_TRAMPOLINE_JUMP) {
		x86_mov_membase_imm (code, X86_ESP, (1 * sizeof (mgreg_t)), 0, sizeof (mgreg_t));
	} else {
		x86_mov_reg_membase (code, X86_EAX, X86_EBP, caller_ip_offset, sizeof (mgreg_t));
		x86_mov_membase_reg (code, X86_ESP, (1 * sizeof (mgreg_t)), X86_EAX, sizeof (mgreg_t));
	}
	/* Arg3 - trampoline argument */
	x86_mov_reg_membase (code, X86_EAX, X86_EBP, arg_offset, sizeof (mgreg_t));
	x86_mov_membase_reg (code, X86_ESP, (2 * sizeof (mgreg_t)), X86_EAX, sizeof (mgreg_t));
	/* Arg4 - trampoline address */
	// FIXME:
	x86_mov_membase_imm (code, X86_ESP, (3 * sizeof (mgreg_t)), 0, sizeof (mgreg_t));

#ifdef __APPLE__
	/* check the stack is aligned after the ret ip is pushed */
	/*
	x86_mov_reg_reg (code, X86_EDX, X86_ESP, 4);
	x86_alu_reg_imm (code, X86_AND, X86_EDX, 15);
	x86_alu_reg_imm (code, X86_CMP, X86_EDX, 0);
	x86_branch_disp (code, X86_CC_Z, 3, FALSE);
	x86_breakpoint (code);
	*/
#endif

	if (aot) {
		char *icall_name = g_strdup_printf ("trampoline_func_%d", tramp_type);
		code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, icall_name);
		x86_call_reg (code, X86_EAX);
	} else {
		tramp = (guint8*)mono_get_trampoline_func (tramp_type);
		x86_call_code (code, tramp);
	}

	/*
	 * Overwrite the trampoline argument with the address we need to jump to,
	 * to free %eax.
	 */
	x86_mov_membase_reg (code, X86_EBP, arg_offset, X86_EAX, 4);

	/* Restore LMF */
	x86_mov_reg_membase (code, X86_EAX, X86_EBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, lmf_addr), sizeof (mgreg_t));
	x86_mov_reg_membase (code, X86_ECX, X86_EBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, previous_lmf), sizeof (mgreg_t));
	x86_alu_reg_imm (code, X86_SUB, X86_ECX, 1);
	x86_mov_membase_reg (code, X86_EAX, 0, X86_ECX, sizeof (mgreg_t));

	/* Check for interruptions */
	if (aot) {
		code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, "mono_thread_force_interruption_checkpoint_noraise");
		x86_call_reg (code, X86_EAX);
	} else {
		x86_call_code (code, (guint8*)mono_thread_force_interruption_checkpoint_noraise);
	}

	x86_test_reg_reg (code, X86_EAX, X86_EAX);
	br_ex_check = code;
	x86_branch8 (code, X86_CC_Z, -1, 1);

	/*
	 * Exception case:
	 * We have an exception we want to throw in the caller's frame, so pop
	 * the trampoline frame and throw from the caller.
	 */
	x86_leave (code);
	/*
	 * The exception is in eax.
	 * We are calling the throw trampoline used by OP_THROW, so we have to setup the
	 * stack to look the same.
	 * The stack contains the ret addr, and the trampoline argument, the throw trampoline
	 * expects it to contain the ret addr and the exception. It also needs to be aligned
	 * after the exception is pushed.
	 */
	/* Align stack */
	x86_push_reg (code, X86_EAX);
	/* Push the exception */
	x86_push_reg (code, X86_EAX);
	//x86_breakpoint (code);
	/* Push the original return value */
	x86_push_membase (code, X86_ESP, 3 * 4);
	/*
	 * EH is initialized after trampolines, so get the address of the variable
	 * which contains throw_exception, and load it from there.
	 */
	if (aot) {
		/* Not really a jit icall */
		code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, "throw_exception_addr");
	} else {
		x86_mov_reg_imm (code, X86_ECX, (guint8*)mono_get_throw_exception_addr ());
	}
	x86_mov_reg_membase (code, X86_ECX, X86_ECX, 0, sizeof(gpointer));
	x86_jump_reg (code, X86_ECX);

	/* Normal case */
	mono_x86_patch (br_ex_check, code);

	/* Restore registers */
	for (i = X86_EAX; i <= X86_EDI; ++i) {
		if (i == X86_ESP || i == X86_EBP)
			continue;
		if (i == X86_EAX && !((tramp_type == MONO_TRAMPOLINE_RESTORE_STACK_PROT) || (tramp_type == MONO_TRAMPOLINE_AOT_PLT)))
			continue;
		x86_mov_reg_membase (code, i, X86_EBP, regarray_offset + (i * 4), 4);
	}

	/* Restore frame */
	x86_leave (code);
	cfa_offset -= sizeof (mgreg_t);
	mono_add_unwind_op_def_cfa (unwind_ops, code, buf, X86_ESP, cfa_offset);
	mono_add_unwind_op_same_value (unwind_ops, code, buf, X86_EBP);

	if (MONO_TRAMPOLINE_TYPE_MUST_RETURN (tramp_type)) {
		/* Load the value returned by the trampoline */
		x86_mov_reg_membase (code, X86_EAX, X86_ESP, 0, 4);
		/* The trampoline returns normally, pop the trampoline argument */
		x86_alu_reg_imm (code, X86_ADD, X86_ESP, 4);
		cfa_offset -= sizeof (mgreg_t);
		mono_add_unwind_op_def_cfa_offset (unwind_ops, code, buf, cfa_offset);
		x86_ret (code);
	} else {
		x86_ret (code);
	}

	g_assert ((code - buf) <= 256);
	MONO_PROFILER_RAISE (jit_code_buffer, (buf, code - buf, MONO_PROFILER_CODE_BUFFER_HELPER, NULL));

	tramp_name = mono_get_generic_trampoline_name (tramp_type);
	*info = mono_tramp_info_create (tramp_name, buf, code - buf, ji, unwind_ops);
	g_free (tramp_name);

	return buf;
}

#define TRAMPOLINE_SIZE 10

gpointer
mono_arch_create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type, MonoDomain *domain, guint32 *code_len)
{
	guint8 *code, *buf, *tramp;
	
	tramp = mono_get_trampoline_code (tramp_type);

	code = buf = mono_domain_code_reserve_align (domain, TRAMPOLINE_SIZE, 4);

	x86_push_imm (buf, arg1);
	x86_jump_code (buf, tramp);
	g_assert ((buf - code) <= TRAMPOLINE_SIZE);

	mono_arch_flush_icache (code, buf - code);
	MONO_PROFILER_RAISE (jit_code_buffer, (code, buf - code, MONO_PROFILER_CODE_BUFFER_SPECIFIC_TRAMPOLINE, mono_get_generic_trampoline_simple_name (tramp_type)));

	if (code_len)
		*code_len = buf - code;

	return code;
}

gpointer
mono_arch_create_rgctx_lazy_fetch_trampoline (guint32 slot, MonoTrampInfo **info, gboolean aot)
{
	guint8 *tramp;
	guint8 *code, *buf;
	guint8 **rgctx_null_jumps;
	int tramp_size;
	int depth, index;
	int i;
	gboolean mrgctx;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;

	unwind_ops = mono_arch_get_cie_program ();

	mrgctx = MONO_RGCTX_SLOT_IS_MRGCTX (slot);
	index = MONO_RGCTX_SLOT_INDEX (slot);
	if (mrgctx)
		index += MONO_SIZEOF_METHOD_RUNTIME_GENERIC_CONTEXT / sizeof (gpointer);
	for (depth = 0; ; ++depth) {
		int size = mono_class_rgctx_get_array_size (depth, mrgctx);

		if (index < size - 1)
			break;
		index -= size - 1;
	}

	tramp_size = (aot ? 64 : 36) + 6 * depth;

	code = buf = mono_global_codeman_reserve (tramp_size);

	rgctx_null_jumps = g_malloc (sizeof (guint8*) * (depth + 2));

	/* load vtable/mrgctx ptr */
	x86_mov_reg_membase (code, X86_EAX, X86_ESP, 4, 4);
	if (!mrgctx) {
		/* load rgctx ptr from vtable */
		x86_mov_reg_membase (code, X86_EAX, X86_EAX, MONO_STRUCT_OFFSET (MonoVTable, runtime_generic_context), 4);
		/* is the rgctx ptr null? */
		x86_test_reg_reg (code, X86_EAX, X86_EAX);
		/* if yes, jump to actual trampoline */
		rgctx_null_jumps [0] = code;
		x86_branch8 (code, X86_CC_Z, -1, 1);
	}

	for (i = 0; i < depth; ++i) {
		/* load ptr to next array */
		if (mrgctx && i == 0)
			x86_mov_reg_membase (code, X86_EAX, X86_EAX, MONO_SIZEOF_METHOD_RUNTIME_GENERIC_CONTEXT, 4);
		else
			x86_mov_reg_membase (code, X86_EAX, X86_EAX, 0, 4);
		/* is the ptr null? */
		x86_test_reg_reg (code, X86_EAX, X86_EAX);
		/* if yes, jump to actual trampoline */
		rgctx_null_jumps [i + 1] = code;
		x86_branch8 (code, X86_CC_Z, -1, 1);
	}

	/* fetch slot */
	x86_mov_reg_membase (code, X86_EAX, X86_EAX, sizeof (gpointer) * (index + 1), 4);
	/* is the slot null? */
	x86_test_reg_reg (code, X86_EAX, X86_EAX);
	/* if yes, jump to actual trampoline */
	rgctx_null_jumps [depth + 1] = code;
	x86_branch8 (code, X86_CC_Z, -1, 1);
	/* otherwise return */
	x86_ret (code);

	for (i = mrgctx ? 1 : 0; i <= depth + 1; ++i)
		x86_patch (rgctx_null_jumps [i], code);

	g_free (rgctx_null_jumps);

	x86_mov_reg_membase (code, MONO_ARCH_VTABLE_REG, X86_ESP, 4, 4);

	if (aot) {
		code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, g_strdup_printf ("specific_trampoline_lazy_fetch_%u", slot));
		x86_jump_reg (code, X86_EAX);
	} else {
		tramp = mono_arch_create_specific_trampoline (GUINT_TO_POINTER (slot), MONO_TRAMPOLINE_RGCTX_LAZY_FETCH, mono_get_root_domain (), NULL);

		/* jump to the actual trampoline */
		x86_jump_code (code, tramp);
	}

	mono_arch_flush_icache (buf, code - buf);
	MONO_PROFILER_RAISE (jit_code_buffer, (buf, code - buf, MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE, NULL));

	g_assert (code - buf <= tramp_size);

	char *name = mono_get_rgctx_fetch_trampoline_name (slot);
	*info = mono_tramp_info_create (name, buf, code - buf, ji, unwind_ops);
	g_free (name);

	return buf;
}

/*
 * mono_arch_create_general_rgctx_lazy_fetch_trampoline:
 *
 *   This is a general variant of the rgctx fetch trampolines. It receives a pointer to gpointer[2] in the rgctx reg. The first entry contains the slot, the second
 * the trampoline to call if the slot is not filled.
 */
gpointer
mono_arch_create_general_rgctx_lazy_fetch_trampoline (MonoTrampInfo **info, gboolean aot)
{
	guint8 *code, *buf;
	int tramp_size;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;

	g_assert (aot);

	unwind_ops = mono_arch_get_cie_program ();

	tramp_size = 64;

	code = buf = mono_global_codeman_reserve (tramp_size);

	// FIXME: Currently, we always go to the slow path.
	
	/* Load trampoline addr */
	x86_mov_reg_membase (code, X86_EAX, MONO_ARCH_RGCTX_REG, 4, 4);
	/* Load mrgctx/vtable */
	x86_mov_reg_membase (code, MONO_ARCH_VTABLE_REG, X86_ESP, 4, 4);

	x86_jump_reg (code, X86_EAX);

	mono_arch_flush_icache (buf, code - buf);
	MONO_PROFILER_RAISE (jit_code_buffer, (buf, code - buf, MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE, NULL));

	g_assert (code - buf <= tramp_size);

	*info = mono_tramp_info_create ("rgctx_fetch_trampoline_general", buf, code - buf, ji, unwind_ops);

	return buf;
}

void
mono_arch_invalidate_method (MonoJitInfo *ji, void *func, gpointer func_arg)
{
	/* FIXME: This is not thread safe */
	guint8 *code = ji->code_start;

	x86_push_imm (code, func_arg);
	x86_call_code (code, (guint8*)func);
}

guint8*
mono_arch_get_call_target (guint8 *code)
{
	if (code [-5] == 0xe8) {
		gint32 disp = *(gint32*)(code - 4);
		guint8 *target = code + disp;

		return target;
	} else {
		return NULL;
	}
}

guint32
mono_arch_get_plt_info_offset (guint8 *plt_entry, mgreg_t *regs, guint8 *code)
{
	return *(guint32*)(plt_entry + 6);
}

/*
 * mono_arch_get_gsharedvt_arg_trampoline:
 *
 *   Return a trampoline which passes ARG to the gsharedvt in/out trampoline ADDR.
 */
gpointer
mono_arch_get_gsharedvt_arg_trampoline (MonoDomain *domain, gpointer arg, gpointer addr)
{
	guint8 *code, *start;
	int buf_len;
	GSList *unwind_ops;


	buf_len = 10;

	start = code = mono_domain_code_reserve (domain, buf_len);

	unwind_ops = mono_arch_get_cie_program ();

	x86_mov_reg_imm (code, X86_EAX, arg);
	x86_jump_code (code, addr);
	g_assert ((code - start) <= buf_len);

	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE, NULL));

	mono_tramp_info_register (mono_tramp_info_create (NULL, start, code - start, NULL, unwind_ops), domain);

	return start;
}

/*
 * mono_arch_create_sdb_trampoline:
 *
 *   Return a trampoline which captures the current context, passes it to
 * debugger_agent_single_step_from_context ()/debugger_agent_breakpoint_from_context (),
 * then restores the (potentially changed) context.
 */
guint8*
mono_arch_create_sdb_trampoline (gboolean single_step, MonoTrampInfo **info, gboolean aot)
{
	int tramp_size = 256;
	int framesize, ctx_offset, cfa_offset;
	guint8 *code, *buf;
	GSList *unwind_ops = NULL;
	MonoJumpInfo *ji = NULL;

	code = buf = mono_global_codeman_reserve (tramp_size);

	framesize = 0;

	/* Argument area */
	framesize += sizeof (mgreg_t);

	framesize = ALIGN_TO (framesize, 8);
	ctx_offset = framesize;
	framesize += sizeof (MonoContext);

	framesize = ALIGN_TO (framesize, MONO_ARCH_FRAME_ALIGNMENT);

	// CFA = sp + 4
	cfa_offset = 4;
	mono_add_unwind_op_def_cfa (unwind_ops, code, buf, X86_ESP, 4);
	// IP saved at CFA - 4
	mono_add_unwind_op_offset (unwind_ops, code, buf, X86_NREG, -cfa_offset);

	x86_push_reg (code, X86_EBP);
	cfa_offset += sizeof(mgreg_t);
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, buf, cfa_offset);
	mono_add_unwind_op_offset (unwind_ops, code, buf, X86_EBP, - cfa_offset);

	x86_mov_reg_reg (code, X86_EBP, X86_ESP, sizeof(mgreg_t));
	mono_add_unwind_op_def_cfa_reg (unwind_ops, code, buf, X86_EBP);
	/* The + 8 makes the stack aligned */
	x86_alu_reg_imm (code, X86_SUB, X86_ESP, framesize + 8);

	/* Initialize a MonoContext structure on the stack */
	x86_mov_membase_reg (code, X86_ESP, ctx_offset + G_STRUCT_OFFSET (MonoContext, eax), X86_EAX, sizeof (mgreg_t));
	x86_mov_membase_reg (code, X86_ESP, ctx_offset + G_STRUCT_OFFSET (MonoContext, ebx), X86_EBX, sizeof (mgreg_t));
	x86_mov_membase_reg (code, X86_ESP, ctx_offset + G_STRUCT_OFFSET (MonoContext, ecx), X86_ECX, sizeof (mgreg_t));
	x86_mov_membase_reg (code, X86_ESP, ctx_offset + G_STRUCT_OFFSET (MonoContext, edx), X86_EDX, sizeof (mgreg_t));
	x86_mov_reg_membase (code, X86_EAX, X86_EBP, 0, sizeof (mgreg_t));
	x86_mov_membase_reg (code, X86_ESP, ctx_offset + G_STRUCT_OFFSET (MonoContext, ebp), X86_EAX, sizeof (mgreg_t));
	x86_mov_reg_reg (code, X86_EAX, X86_EBP, sizeof (mgreg_t));
	x86_alu_reg_imm (code, X86_ADD, X86_EAX, cfa_offset);
	x86_mov_membase_reg (code, X86_ESP, ctx_offset + G_STRUCT_OFFSET (MonoContext, esp), X86_ESP, sizeof (mgreg_t));
	x86_mov_membase_reg (code, X86_ESP, ctx_offset + G_STRUCT_OFFSET (MonoContext, esi), X86_ESI, sizeof (mgreg_t));
	x86_mov_membase_reg (code, X86_ESP, ctx_offset + G_STRUCT_OFFSET (MonoContext, edi), X86_EDI, sizeof (mgreg_t));
	x86_mov_reg_membase (code, X86_EAX, X86_EBP, 4, sizeof (mgreg_t));
	x86_mov_membase_reg (code, X86_ESP, ctx_offset + G_STRUCT_OFFSET (MonoContext, eip), X86_EAX, sizeof (mgreg_t));

	/* Call the single step/breakpoint function in sdb */
	x86_lea_membase (code, X86_EAX, X86_ESP, ctx_offset);
	x86_mov_membase_reg (code, X86_ESP, 0, X86_EAX, sizeof (mgreg_t));

	if (aot) {
		x86_breakpoint (code);
	} else {
		if (single_step)
			x86_call_code (code, debugger_agent_single_step_from_context);
		else
			x86_call_code (code, debugger_agent_breakpoint_from_context);
	}

	/* Restore registers from ctx */
	/* Overwrite the saved ebp */
	x86_mov_reg_membase (code, X86_EAX, X86_ESP, ctx_offset + G_STRUCT_OFFSET (MonoContext, ebp), sizeof (mgreg_t));
	x86_mov_membase_reg (code, X86_EBP, 0, X86_EAX, sizeof (mgreg_t));
	/* Overwrite saved eip */
	x86_mov_reg_membase (code, X86_EAX, X86_ESP, ctx_offset + G_STRUCT_OFFSET (MonoContext, eip), sizeof (mgreg_t));
	x86_mov_membase_reg (code, X86_EBP, 4, X86_EAX, sizeof (mgreg_t));
	x86_mov_reg_membase (code, X86_EAX, X86_ESP, ctx_offset + G_STRUCT_OFFSET (MonoContext, eax), sizeof (mgreg_t));
	x86_mov_reg_membase (code, X86_EBX, X86_ESP, ctx_offset + G_STRUCT_OFFSET (MonoContext, ebx), sizeof (mgreg_t));
	x86_mov_reg_membase (code, X86_ECX, X86_ESP, ctx_offset + G_STRUCT_OFFSET (MonoContext, ecx), sizeof (mgreg_t));
	x86_mov_reg_membase (code, X86_EDX, X86_ESP, ctx_offset + G_STRUCT_OFFSET (MonoContext, edx), sizeof (mgreg_t));
	x86_mov_reg_membase (code, X86_ESI, X86_ESP, ctx_offset + G_STRUCT_OFFSET (MonoContext, esi), sizeof (mgreg_t));
	x86_mov_reg_membase (code, X86_EDI, X86_ESP, ctx_offset + G_STRUCT_OFFSET (MonoContext, edi), sizeof (mgreg_t));

	x86_leave (code);
	cfa_offset -= sizeof (mgreg_t);
	mono_add_unwind_op_def_cfa (unwind_ops, code, buf, X86_ESP, cfa_offset);
	x86_ret (code);

	mono_arch_flush_icache (code, code - buf);
	g_assert (code - buf <= tramp_size);

	const char *tramp_name = single_step ? "sdb_single_step_trampoline" : "sdb_breakpoint_trampoline";
	*info = mono_tramp_info_create (tramp_name, buf, code - buf, ji, unwind_ops);

	return buf;
}

gpointer
mono_arch_get_enter_icall_trampoline (MonoTrampInfo **info)
{
	g_assert_not_reached ();
	return NULL;
}

