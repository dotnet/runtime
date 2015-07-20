/*
 * tramp-amd64.c: JIT trampoline code for amd64
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Zoltan Varga (vargaz@gmail.com)
 *
 * (C) 2001 Ximian, Inc.
 * Copyright 2003-2011 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 */

#include <config.h>
#include <glib.h>

#include <mono/metadata/abi-details.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/mono-debug-debugger.h>
#include <mono/metadata/monitor.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/gc-internal.h>
#include <mono/arch/amd64/amd64-codegen.h>

#include <mono/utils/memcheck.h>

#include "mini.h"
#include "mini-amd64.h"
#include "debugger-agent.h"

#if defined(__native_client_codegen__) && defined(__native_client__)
#include <malloc.h>
#include <nacl/nacl_dyncode.h>
#endif

#define ALIGN_TO(val,align) ((((guint64)val) + ((align) - 1)) & ~((align) - 1))

#define IS_REX(inst) (((inst) >= 0x40) && ((inst) <= 0x4f))

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
	int this_reg, size = NACL_SIZE (20, 32);

	MonoDomain *domain = mono_domain_get ();

	this_reg = mono_arch_get_this_arg_reg (NULL);

	start = code = mono_domain_code_reserve (domain, size);

	amd64_alu_reg_imm (code, X86_ADD, this_reg, sizeof (MonoObject));
	/* FIXME: Optimize this */
	amd64_mov_reg_imm (code, AMD64_RAX, addr);
	amd64_jump_reg (code, AMD64_RAX);
	g_assert ((code - start) < size);

	nacl_domain_code_validate (domain, &start, size, &code);

	mono_arch_flush_icache (start, code - start);
	mono_profiler_code_buffer_new (start, code - start, MONO_PROFILER_CODE_BUFFER_UNBOX_TRAMPOLINE, m);

	return start;
}

/*
 * mono_arch_get_static_rgctx_trampoline:
 *
 *   Create a trampoline which sets RGCTX_REG to MRGCTX, then jumps to ADDR.
 */
gpointer
mono_arch_get_static_rgctx_trampoline (MonoMethod *m, MonoMethodRuntimeGenericContext *mrgctx, gpointer addr)
{
	guint8 *code, *start;
	int buf_len;

	MonoDomain *domain = mono_domain_get ();

#ifdef MONO_ARCH_NOMAP32BIT
	buf_len = 32;
#else
	/* AOTed code could still have a non-32 bit address */
	if ((((guint64)addr) >> 32) == 0)
		buf_len = NACL_SIZE (16, 32);
	else
		buf_len = NACL_SIZE (30, 32);
#endif

	start = code = mono_domain_code_reserve (domain, buf_len);

	amd64_mov_reg_imm (code, MONO_ARCH_RGCTX_REG, mrgctx);
	amd64_jump_code (code, addr);
	g_assert ((code - start) < buf_len);

	nacl_domain_code_validate (domain, &start, buf_len, &code);
	mono_arch_flush_icache (start, code - start);
	mono_profiler_code_buffer_new (start, code - start, MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE, NULL);

	return start;
}

gpointer
mono_arch_get_llvm_imt_trampoline (MonoDomain *domain, MonoMethod *m, int vt_offset)
{
	guint8 *code, *start;
	int buf_len;
	int this_reg;

	buf_len = 32;

	start = code = mono_domain_code_reserve (domain, buf_len);

	this_reg = mono_arch_get_this_arg_reg (NULL);

	/* Set imt arg */
	amd64_mov_reg_imm (code, MONO_ARCH_IMT_REG, m);
	/* Load vtable address */
	amd64_mov_reg_membase (code, AMD64_RAX, this_reg, 0, 8);
	amd64_jump_membase (code, AMD64_RAX, vt_offset);
	amd64_ret (code);

	g_assert ((code - start) < buf_len);

	nacl_domain_code_validate (domain, &start, buf_len, &code);

	mono_arch_flush_icache (start, code - start);
	mono_profiler_code_buffer_new (start, code - start, MONO_PROFILER_CODE_BUFFER_IMT_TRAMPOLINE, NULL);

	return start;
}

/*
 * mono_arch_patch_callsite:
 *
 *   Patch the callsite whose address is given by ORIG_CODE so it calls ADDR. ORIG_CODE
 * points to the pc right after the call.
 */
void
mono_arch_patch_callsite (guint8 *method_start, guint8 *orig_code, guint8 *addr)
{
#if defined(__default_codegen__)
	guint8 *code;
	guint8 buf [16];
	gboolean can_write = mono_breakpoint_clean_code (method_start, orig_code, 14, buf, sizeof (buf));

	code = buf + 14;

	/* mov 64-bit imm into r11 (followed by call reg?)  or direct call*/
	if (((code [-13] == 0x49) && (code [-12] == 0xbb)) || (code [-5] == 0xe8)) {
		if (code [-5] != 0xe8) {
			if (can_write) {
				InterlockedExchangePointer ((gpointer*)(orig_code - 11), addr);
				VALGRIND_DISCARD_TRANSLATIONS (orig_code - 11, sizeof (gpointer));
			}
		} else {
			gboolean disp_32bit = ((((gint64)addr - (gint64)orig_code)) < (1 << 30)) && ((((gint64)addr - (gint64)orig_code)) > -(1 << 30));

			if ((((guint64)(addr)) >> 32) != 0 && !disp_32bit) {
				/* 
				 * This might happen with LLVM or when calling AOTed code. Create a thunk.
				 */
				guint8 *thunk_start, *thunk_code;

				thunk_start = thunk_code = mono_domain_code_reserve (mono_domain_get (), 32);
				amd64_jump_membase (thunk_code, AMD64_RIP, 0);
				*(guint64*)thunk_code = (guint64)addr;
				addr = thunk_start;
				g_assert ((((guint64)(addr)) >> 32) == 0);
				mono_arch_flush_icache (thunk_start, thunk_code - thunk_start);
				mono_profiler_code_buffer_new (thunk_start, thunk_code - thunk_start, MONO_PROFILER_CODE_BUFFER_HELPER, NULL);
			}
			if (can_write) {
				InterlockedExchange ((gint32*)(orig_code - 4), ((gint64)addr - (gint64)orig_code));
				VALGRIND_DISCARD_TRANSLATIONS (orig_code - 5, 4);
			}
		}
	}
	else if ((code [-7] == 0x41) && (code [-6] == 0xff) && (code [-5] == 0x15)) {
		/* call *<OFFSET>(%rip) */
		gpointer *got_entry = (gpointer*)((guint8*)orig_code + (*(guint32*)(orig_code - 4)));
		if (can_write) {
			InterlockedExchangePointer (got_entry, addr);
			VALGRIND_DISCARD_TRANSLATIONS (orig_code - 5, sizeof (gpointer));
		}
	}
#elif defined(__native_client__)
	/* These are essentially the same 2 cases as above, modified for NaCl*/

	/* Target must be bundle-aligned */
	g_assert (((guint32)addr & kNaClAlignmentMask) == 0);
	/* Return target must be bundle-aligned */
	g_assert (((guint32)orig_code & kNaClAlignmentMask) == 0);

	if (orig_code[-5] == 0xe8) {
		/* Direct call */
		int ret;
		gint32 offset = (gint32)addr - (gint32)orig_code;
		guint8 buf[sizeof(gint32)];
		*((gint32*)(buf)) = offset;
		ret = nacl_dyncode_modify (orig_code - sizeof(gint32), buf, sizeof(gint32));
		g_assert (ret == 0);
	}

	else if (is_nacl_call_reg_sequence (orig_code - 10) && orig_code[-16] == 0x41 && orig_code[-15] == 0xbb) {
		int ret;
		guint8 buf[sizeof(gint32)];
		*((gint32 *)(buf)) = addr;
		/* orig_code[-14] is the start of the immediate. */
		ret = nacl_dyncode_modify (orig_code - 14, buf, sizeof(gint32));
		g_assert (ret == 0);
	}
	else {
		g_assert_not_reached ();
	}

	return;
#endif
}

guint8*
mono_arch_create_llvm_native_thunk (MonoDomain *domain, guint8 *addr)
{
	/*
	 * The caller is LLVM code and the call displacement might exceed 32 bits. We can't determine the caller address, so
	 * we add a thunk every time.
	 * Since the caller is also allocated using the domain code manager, hopefully the displacement will fit into 32 bits.
	 * FIXME: Avoid this if possible if !MONO_ARCH_NOMAP32BIT and ADDR is 32 bits.
	 */
	guint8 *thunk_start, *thunk_code;

	thunk_start = thunk_code = mono_domain_code_reserve (mono_domain_get (), 32);
	amd64_jump_membase (thunk_code, AMD64_RIP, 0);
	*(guint64*)thunk_code = (guint64)addr;
	addr = thunk_start;
	mono_arch_flush_icache (thunk_start, thunk_code - thunk_start);
	mono_profiler_code_buffer_new (thunk_start, thunk_code - thunk_start, MONO_PROFILER_CODE_BUFFER_HELPER, NULL);
	return addr;
}

void
mono_arch_patch_plt_entry (guint8 *code, gpointer *got, mgreg_t *regs, guint8 *addr)
{
	gint32 disp;
	gpointer *plt_jump_table_entry;

#if defined(__default_codegen__)
	/* A PLT entry: jmp *<DISP>(%rip) */
	g_assert (code [0] == 0xff);
	g_assert (code [1] == 0x25);

	disp = *(gint32*)(code + 2);

	plt_jump_table_entry = (gpointer*)(code + 6 + disp);
#elif defined(__native_client_codegen__)
	/* A PLT entry:            */
	/* mov <DISP>(%rip), %r11d */
	/* nacljmp *%r11           */

	/* Verify the 'mov' */
	g_assert (code [0] == 0x45);
	g_assert (code [1] == 0x8b);
	g_assert (code [2] == 0x1d);

	disp = *(gint32*)(code + 3);

	/* 7 = 3 (mov opcode) + 4 (disp) */
	/* This needs to resolve to the target of the RIP-relative offset */
	plt_jump_table_entry = (gpointer*)(code + 7 + disp);

#endif /* __native_client_codegen__ */


	InterlockedExchangePointer (plt_jump_table_entry, addr);
}

static gpointer
get_vcall_slot (guint8 *code, mgreg_t *regs, int *displacement)
{
	guint8 buf [10];
	gint32 disp;
	MonoJitInfo *ji = NULL;

#ifdef ENABLE_LLVM
	/* code - 9 might be before the start of the method */
	/* FIXME: Avoid this expensive call somehow */
	ji = mono_jit_info_table_find (mono_domain_get (), (char*)code);
#endif

	mono_breakpoint_clean_code (ji ? ji->code_start : NULL, code, 9, buf, sizeof (buf));
	code = buf + 9;

	*displacement = 0;

	code -= 7;

	if ((code [0] == 0x41) && (code [1] == 0xff) && (code [2] == 0x15)) {
		/* call OFFSET(%rip) */
		g_assert_not_reached ();
		*displacement = *(guint32*)(code + 3);
		return (gpointer*)(code + disp + 7);
	} else {
		g_assert_not_reached ();
		return NULL;
	}
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

static void
stack_unaligned (MonoTrampolineType tramp_type)
{
	printf ("%d\n", tramp_type);
	g_assert_not_reached ();
}

guchar*
mono_arch_create_generic_trampoline (MonoTrampolineType tramp_type, MonoTrampInfo **info, gboolean aot)
{
	char *tramp_name;
	guint8 *buf, *code, *tramp, *br [2], *r11_save_code, *after_r11_save_code;
	int i, lmf_offset, offset, res_offset, arg_offset, rax_offset, tramp_offset, ctx_offset, saved_regs_offset;
	int saved_fpregs_offset, rbp_offset, framesize, orig_rsp_to_rbp_offset, cfa_offset;
	gboolean has_caller;
	GSList *unwind_ops = NULL;
	MonoJumpInfo *ji = NULL;
	const guint kMaxCodeSize = NACL_SIZE (600, 600*2);

#if defined(__native_client_codegen__)
	const guint kNaClTrampOffset = 17;
#endif

	if (tramp_type == MONO_TRAMPOLINE_JUMP || tramp_type == MONO_TRAMPOLINE_HANDLER_BLOCK_GUARD)
		has_caller = FALSE;
	else
		has_caller = TRUE;

	code = buf = mono_global_codeman_reserve (kMaxCodeSize);

	/* Compute stack frame size and offsets */
	offset = 0;
	rbp_offset = -offset;

	offset += sizeof(mgreg_t);
	rax_offset = -offset;

	offset += sizeof(mgreg_t);
	tramp_offset = -offset;

	offset += sizeof(gpointer);
	arg_offset = -offset;

	offset += sizeof(mgreg_t);
	res_offset = -offset;

	offset += sizeof (MonoContext);
	ctx_offset = -offset;
	saved_regs_offset = ctx_offset + MONO_STRUCT_OFFSET (MonoContext, gregs);
	saved_fpregs_offset = ctx_offset + MONO_STRUCT_OFFSET (MonoContext, fregs);

	offset += sizeof (MonoLMFTramp);
	lmf_offset = -offset;

	framesize = ALIGN_TO (offset, MONO_ARCH_FRAME_ALIGNMENT);

	orig_rsp_to_rbp_offset = 0;
	r11_save_code = code;
	/* Reserve space for the mov_membase_reg to save R11 */
	code += 8;
	after_r11_save_code = code;

	// CFA = sp + 16 (the trampoline address is on the stack)
	cfa_offset = 16;
	mono_add_unwind_op_def_cfa (unwind_ops, code, buf, AMD64_RSP, 16);
	// IP saved at CFA - 8
	mono_add_unwind_op_offset (unwind_ops, code, buf, AMD64_RIP, -8);

	/* Pop the return address off the stack */
	amd64_pop_reg (code, AMD64_R11);
	orig_rsp_to_rbp_offset += sizeof(mgreg_t);

	cfa_offset -= sizeof(mgreg_t);
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, buf, cfa_offset);

	/* 
	 * Allocate a new stack frame
	 */
	amd64_push_reg (code, AMD64_RBP);
	cfa_offset += sizeof(mgreg_t);
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, buf, cfa_offset);
	mono_add_unwind_op_offset (unwind_ops, code, buf, AMD64_RBP, - cfa_offset);

	orig_rsp_to_rbp_offset -= sizeof(mgreg_t);
	amd64_mov_reg_reg (code, AMD64_RBP, AMD64_RSP, sizeof(mgreg_t));
	mono_add_unwind_op_def_cfa_reg (unwind_ops, code, buf, AMD64_RBP);
	amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, framesize);

	/* Compute the trampoline address from the return address */
	if (aot) {
#if defined(__default_codegen__)
		/* 7 = length of call *<offset>(rip) */
		amd64_alu_reg_imm (code, X86_SUB, AMD64_R11, 7);
#elif defined(__native_client_codegen__)
		amd64_alu_reg_imm (code, X86_SUB, AMD64_R11, kNaClTrampOffset);
#endif
	} else {
		/* 5 = length of amd64_call_membase () */
		amd64_alu_reg_imm (code, X86_SUB, AMD64_R11, 5);
	}
	amd64_mov_membase_reg (code, AMD64_RBP, tramp_offset, AMD64_R11, sizeof(gpointer));

	/* Save all registers */
	for (i = 0; i < AMD64_NREG; ++i) {
		if (i == AMD64_RBP) {
			/* RAX is already saved */
			amd64_mov_reg_membase (code, AMD64_RAX, AMD64_RBP, rbp_offset, sizeof(mgreg_t));
			amd64_mov_membase_reg (code, AMD64_RBP, saved_regs_offset + (i * sizeof(mgreg_t)), AMD64_RAX, sizeof(mgreg_t));
		} else if (i == AMD64_RIP) {
			if (has_caller)
				amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, 8, sizeof(gpointer));
			else
				amd64_mov_reg_imm (code, AMD64_R11, 0);
			amd64_mov_membase_reg (code, AMD64_RBP, saved_regs_offset + (i * sizeof(mgreg_t)), AMD64_R11, sizeof(mgreg_t));
		} else if (i == AMD64_RSP) {
			amd64_mov_reg_reg (code, AMD64_R11, AMD64_RSP, sizeof(mgreg_t));
			amd64_alu_reg_imm (code, X86_ADD, AMD64_R11, framesize + 16);
			amd64_mov_membase_reg (code, AMD64_RBP, saved_regs_offset + (i * sizeof(mgreg_t)), AMD64_R11, sizeof(mgreg_t));
		} else if (i != AMD64_R11) {
			amd64_mov_membase_reg (code, AMD64_RBP, saved_regs_offset + (i * sizeof(mgreg_t)), i, sizeof(mgreg_t));
		} else {
			/* We have to save R11 right at the start of
			   the trampoline code because it's used as a
			   scratch register */
			amd64_mov_membase_reg (r11_save_code, AMD64_RSP, saved_regs_offset + orig_rsp_to_rbp_offset + (i * sizeof(mgreg_t)), i, sizeof(mgreg_t));
			g_assert (r11_save_code == after_r11_save_code);
		}
	}
	for (i = 0; i < 8; ++i)
		amd64_movsd_membase_reg (code, AMD64_RBP, saved_fpregs_offset + (i * sizeof(mgreg_t)), i);

	/* Check that the stack is aligned */
#if defined(__default_codegen__)
	amd64_mov_reg_reg (code, AMD64_R11, AMD64_RSP, sizeof (mgreg_t));
	amd64_alu_reg_imm (code, X86_AND, AMD64_R11, 15);
	amd64_alu_reg_imm (code, X86_CMP, AMD64_R11, 0);
	br [0] = code;
	amd64_branch_disp (code, X86_CC_Z, 0, FALSE);
	if (aot) {
		amd64_mov_reg_imm (code, AMD64_R11, 0);
		amd64_mov_reg_membase (code, AMD64_R11, AMD64_R11, 0, 8);
	} else {
		amd64_mov_reg_imm (code, MONO_AMD64_ARG_REG1, tramp_type);
		amd64_mov_reg_imm (code, AMD64_R11, stack_unaligned);
		amd64_call_reg (code, AMD64_R11);
	}
	mono_amd64_patch (br [0], code);
	//amd64_breakpoint (code);
#endif

	if (tramp_type != MONO_TRAMPOLINE_MONITOR_ENTER &&
		tramp_type != MONO_TRAMPOLINE_MONITOR_ENTER_V4 &&
		tramp_type != MONO_TRAMPOLINE_MONITOR_EXIT &&
		tramp_type != MONO_TRAMPOLINE_HANDLER_BLOCK_GUARD) {
		/* Obtain the trampoline argument which is encoded in the instruction stream */
		if (aot) {
			/* Load the GOT offset */
			amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, tramp_offset, sizeof(gpointer));
#if defined(__default_codegen__)
			/*
			 * r11 points to a call *<offset>(%rip) instruction, load the
			 * pc-relative offset from the instruction itself.
			 */
			amd64_mov_reg_membase (code, AMD64_RAX, AMD64_R11, 3, 4);
			/* 7 is the length of the call, 8 is the offset to the next got slot */
			amd64_alu_reg_imm_size (code, X86_ADD, AMD64_RAX, 7 + sizeof (gpointer), sizeof(gpointer));
#elif defined(__native_client_codegen__)
			/* The arg is hidden in a "push imm32" instruction, */
			/* add one to skip the opcode.                      */
			amd64_mov_reg_membase (code, AMD64_RAX, AMD64_R11, kNaClTrampOffset+1, 4);
#endif
			/* Compute the address of the GOT slot */
			amd64_alu_reg_reg_size (code, X86_ADD, AMD64_R11, AMD64_RAX, sizeof(gpointer));
			/* Load the value */
			amd64_mov_reg_membase (code, AMD64_R11, AMD64_R11, 0, sizeof(gpointer));
		} else {			
			amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, tramp_offset, sizeof(gpointer));
#if defined(__default_codegen__)
			amd64_mov_reg_membase (code, AMD64_RAX, AMD64_R11, 5, 1);
			amd64_widen_reg (code, AMD64_RAX, AMD64_RAX, TRUE, FALSE);
			amd64_alu_reg_imm_size (code, X86_CMP, AMD64_RAX, 4, 1);
			br [0] = code;
			x86_branch8 (code, X86_CC_NE, 6, FALSE);
			/* 32 bit immediate */
			amd64_mov_reg_membase (code, AMD64_R11, AMD64_R11, 6, 4);
			br [1] = code;
			x86_jump8 (code, 10);
			/* 64 bit immediate */
			mono_amd64_patch (br [0], code);
			amd64_mov_reg_membase (code, AMD64_R11, AMD64_R11, 6, 8);
			mono_amd64_patch (br [1], code);
#elif defined(__native_client_codegen__)
			/* All args are 32-bit pointers in NaCl */
			amd64_mov_reg_membase (code, AMD64_R11, AMD64_R11, 6, 4);
#endif
		}
		amd64_mov_membase_reg (code, AMD64_RBP, arg_offset, AMD64_R11, sizeof(gpointer));
	} else {
		amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, saved_regs_offset + (MONO_AMD64_ARG_REG1 * sizeof(mgreg_t)), sizeof(mgreg_t));
		amd64_mov_membase_reg (code, AMD64_RBP, arg_offset, AMD64_R11, sizeof(gpointer));
	}

	/* Save LMF begin */

	/* Save ip */
	if (has_caller)
		amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, 8, sizeof(gpointer));
	else
		amd64_mov_reg_imm (code, AMD64_R11, 0);
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, rip), AMD64_R11, sizeof(mgreg_t));
	/* Save sp */
	amd64_mov_reg_reg (code, AMD64_R11, AMD64_RSP, sizeof(mgreg_t));
	amd64_alu_reg_imm (code, X86_ADD, AMD64_R11, framesize + 16);
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, rsp), AMD64_R11, sizeof(mgreg_t));
	/* Save pointer to context */
	amd64_lea_membase (code, AMD64_R11, AMD64_RBP, ctx_offset);
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + MONO_STRUCT_OFFSET (MonoLMFTramp, ctx), AMD64_R11, sizeof(mgreg_t));

	if (aot) {
		code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, "mono_get_lmf_addr");
	} else {
		amd64_mov_reg_imm (code, AMD64_R11, mono_get_lmf_addr);
	}
	amd64_call_reg (code, AMD64_R11);

	/* Save lmf_addr */
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + MONO_STRUCT_OFFSET (MonoLMFTramp, lmf_addr), AMD64_RAX, sizeof(gpointer));
	/* Save previous_lmf */
	/* Set the lowest bit to signal that this LMF has the ip field set */
	/* Set the third lowest bit to signal that this is a MonoLMFTramp structure */
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RAX, 0, sizeof(gpointer));
	amd64_alu_reg_imm_size (code, X86_ADD, AMD64_R11, 0x5, sizeof(gpointer));
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, previous_lmf), AMD64_R11, sizeof(gpointer));
	/* Set new lmf */
	amd64_lea_membase (code, AMD64_R11, AMD64_RBP, lmf_offset);
	amd64_mov_membase_reg (code, AMD64_RAX, 0, AMD64_R11, sizeof(gpointer));

	/* Save LMF end */

	/* Arg1 is the pointer to the saved registers */
	amd64_lea_membase (code, AMD64_ARG_REG1, AMD64_RBP, saved_regs_offset);

	/* Arg2 is the address of the calling code */
	if (has_caller)
		amd64_mov_reg_membase (code, AMD64_ARG_REG2, AMD64_RBP, 8, sizeof(gpointer));
	else
		amd64_mov_reg_imm (code, AMD64_ARG_REG2, 0);

	/* Arg3 is the method/vtable ptr */
	amd64_mov_reg_membase (code, AMD64_ARG_REG3, AMD64_RBP, arg_offset, sizeof(gpointer));

	/* Arg4 is the trampoline address */
	amd64_mov_reg_membase (code, AMD64_ARG_REG4, AMD64_RBP, tramp_offset, sizeof(gpointer));

	if (aot) {
		char *icall_name = g_strdup_printf ("trampoline_func_%d", tramp_type);
		code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, icall_name);
	} else {
		tramp = (guint8*)mono_get_trampoline_func (tramp_type);
		amd64_mov_reg_imm (code, AMD64_R11, tramp);
	}
	amd64_call_reg (code, AMD64_R11);

	/* Check for thread interruption */
	/* This is not perf critical code so no need to check the interrupt flag */
	/* 
	 * Have to call the _force_ variant, since there could be a protected wrapper on the top of the stack.
	 */
	amd64_mov_membase_reg (code, AMD64_RBP, res_offset, AMD64_RAX, sizeof(mgreg_t));
	if (aot) {
		code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, "mono_thread_force_interruption_checkpoint");
	} else {
		amd64_mov_reg_imm (code, AMD64_R11, (guint8*)mono_thread_force_interruption_checkpoint);
	}
	amd64_call_reg (code, AMD64_R11);

	amd64_mov_reg_membase (code, AMD64_RAX, AMD64_RBP, res_offset, sizeof(mgreg_t));	

	/* Restore LMF */
	amd64_mov_reg_membase (code, AMD64_RCX, AMD64_RBP, lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, previous_lmf), sizeof(gpointer));
	amd64_alu_reg_imm_size (code, X86_SUB, AMD64_RCX, 0x5, sizeof(gpointer));
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, lmf_offset + MONO_STRUCT_OFFSET (MonoLMFTramp, lmf_addr), sizeof(gpointer));
	amd64_mov_membase_reg (code, AMD64_R11, 0, AMD64_RCX, sizeof(gpointer));

	/* 
	 * Save rax to the stack, after the leave instruction, this will become part of
	 * the red zone.
	 */
	amd64_mov_membase_reg (code, AMD64_RBP, rax_offset, AMD64_RAX, sizeof(mgreg_t));

	/* Restore argument registers, r10 (imt method/rgxtx)
	   and rax (needed for direct calls to C vararg functions). */
	for (i = 0; i < AMD64_NREG; ++i)
		if (AMD64_IS_ARGUMENT_REG (i) || i == AMD64_R10 || i == AMD64_RAX)
			amd64_mov_reg_membase (code, i, AMD64_RBP, saved_regs_offset + (i * sizeof(mgreg_t)), sizeof(mgreg_t));
	for (i = 0; i < 8; ++i)
		amd64_movsd_reg_membase (code, i, AMD64_RBP, saved_fpregs_offset + (i * sizeof(mgreg_t)));

	/* Restore stack */
	amd64_leave (code);

	if (MONO_TRAMPOLINE_TYPE_MUST_RETURN (tramp_type)) {
		/* Load result */
		amd64_mov_reg_membase (code, AMD64_RAX, AMD64_RSP, rax_offset - sizeof(mgreg_t), sizeof(mgreg_t));
		amd64_ret (code);
	} else {
		/* call the compiled method using the saved rax */
		amd64_jump_membase (code, AMD64_RSP, rax_offset - sizeof(mgreg_t));
	}

	g_assert ((code - buf) <= kMaxCodeSize);

	nacl_global_codeman_validate (&buf, kMaxCodeSize, &code);

	mono_arch_flush_icache (buf, code - buf);
	mono_profiler_code_buffer_new (buf, code - buf, MONO_PROFILER_CODE_BUFFER_HELPER, NULL);

	tramp_name = mono_get_generic_trampoline_name (tramp_type);
	*info = mono_tramp_info_create (tramp_name, buf, code - buf, ji, unwind_ops);
	g_free (tramp_name);

	return buf;
}

gpointer
mono_arch_create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type, MonoDomain *domain, guint32 *code_len)
{
	guint8 *code, *buf, *tramp;
	int size;
	gboolean far_addr = FALSE;

	tramp = mono_get_trampoline_code (tramp_type);

#if defined(__default_codegen__)
	if ((((guint64)arg1) >> 32) == 0)
		size = 5 + 1 + 4;
	else
		size = 5 + 1 + 8;

	code = buf = mono_domain_code_reserve_align (domain, size, 1);

	if (((gint64)tramp - (gint64)code) >> 31 != 0 && ((gint64)tramp - (gint64)code) >> 31 != -1) {
#ifndef MONO_ARCH_NOMAP32BIT
		g_assert_not_reached ();
#endif
		far_addr = TRUE;
		size += 16;
		code = buf = mono_domain_code_reserve_align (domain, size, 1);
	}
#elif defined(__native_client_codegen__)
	size = 5 + 1 + 4;
	/* Aligning the call site below could */
	/* add up to kNaClAlignment-1 bytes   */
	size += (kNaClAlignment-1);
	size = NACL_BUNDLE_ALIGN_UP (size);
	buf = mono_domain_code_reserve_align (domain, size, kNaClAlignment);
	code = buf;
#endif

	if (far_addr) {
		amd64_mov_reg_imm (code, AMD64_R11, tramp);
		amd64_call_reg (code, AMD64_R11);
	} else {
		amd64_call_code (code, tramp);
	}
	/* The trampoline code will obtain the argument from the instruction stream */
#if defined(__default_codegen__)
	if ((((guint64)arg1) >> 32) == 0) {
		*code = 0x4;
		*(guint32*)(code + 1) = (gint64)arg1;
		code += 5;
	} else {
		*code = 0x8;
		*(guint64*)(code + 1) = (gint64)arg1;
		code += 9;
	}
#elif defined(__native_client_codegen__)
	/* For NaCl, all tramp args are 32-bit because they're pointers */
	*code = 0x68; /* push imm32 */
	*(guint32*)(code + 1) = (gint32)arg1;
	code += 5;
#endif

	g_assert ((code - buf) <= size);

	if (code_len)
		*code_len = size;

	nacl_domain_code_validate(domain, &buf, size, &code);

	mono_arch_flush_icache (buf, size);
	mono_profiler_code_buffer_new (buf, code - buf, MONO_PROFILER_CODE_BUFFER_SPECIFIC_TRAMPOLINE, mono_get_generic_trampoline_simple_name (tramp_type));

	return buf;
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

	tramp_size = NACL_SIZE (64 + 8 * depth, 128 + 8 * depth);

	code = buf = mono_global_codeman_reserve (tramp_size);

	unwind_ops = mono_arch_get_cie_program ();

	rgctx_null_jumps = g_malloc (sizeof (guint8*) * (depth + 2));

	if (mrgctx) {
		/* get mrgctx ptr */
		amd64_mov_reg_reg (code, AMD64_RAX, AMD64_ARG_REG1, 8);
	} else {
		/* load rgctx ptr from vtable */
		amd64_mov_reg_membase (code, AMD64_RAX, AMD64_ARG_REG1, MONO_STRUCT_OFFSET (MonoVTable, runtime_generic_context), sizeof(gpointer));
		/* is the rgctx ptr null? */
		amd64_test_reg_reg (code, AMD64_RAX, AMD64_RAX);
		/* if yes, jump to actual trampoline */
		rgctx_null_jumps [0] = code;
		amd64_branch8 (code, X86_CC_Z, -1, 1);
	}

	for (i = 0; i < depth; ++i) {
		/* load ptr to next array */
		if (mrgctx && i == 0)
			amd64_mov_reg_membase (code, AMD64_RAX, AMD64_RAX, MONO_SIZEOF_METHOD_RUNTIME_GENERIC_CONTEXT, sizeof(gpointer));
		else
			amd64_mov_reg_membase (code, AMD64_RAX, AMD64_RAX, 0, sizeof(gpointer));
		/* is the ptr null? */
		amd64_test_reg_reg (code, AMD64_RAX, AMD64_RAX);
		/* if yes, jump to actual trampoline */
		rgctx_null_jumps [i + 1] = code;
		amd64_branch8 (code, X86_CC_Z, -1, 1);
	}

	/* fetch slot */
	amd64_mov_reg_membase (code, AMD64_RAX, AMD64_RAX, sizeof (gpointer) * (index + 1), sizeof(gpointer));
	/* is the slot null? */
	amd64_test_reg_reg (code, AMD64_RAX, AMD64_RAX);
	/* if yes, jump to actual trampoline */
	rgctx_null_jumps [depth + 1] = code;
	amd64_branch8 (code, X86_CC_Z, -1, 1);
	/* otherwise return */
	amd64_ret (code);

	for (i = mrgctx ? 1 : 0; i <= depth + 1; ++i)
		mono_amd64_patch (rgctx_null_jumps [i], code);

	g_free (rgctx_null_jumps);

	/* move the rgctx pointer to the VTABLE register */
	amd64_mov_reg_reg (code, MONO_ARCH_VTABLE_REG, AMD64_ARG_REG1, sizeof(gpointer));

	if (aot) {
		code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, g_strdup_printf ("specific_trampoline_lazy_fetch_%u", slot));
		amd64_jump_reg (code, AMD64_R11);
	} else {
		tramp = mono_arch_create_specific_trampoline (GUINT_TO_POINTER (slot), MONO_TRAMPOLINE_RGCTX_LAZY_FETCH, mono_get_root_domain (), NULL);

		/* jump to the actual trampoline */
		amd64_jump_code (code, tramp);
	}

	nacl_global_codeman_validate (&buf, tramp_size, &code);
	mono_arch_flush_icache (buf, code - buf);
	mono_profiler_code_buffer_new (buf, code - buf, MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE, NULL);

	g_assert (code - buf <= tramp_size);

	char *name = mono_get_rgctx_fetch_trampoline_name (slot);
	*info = mono_tramp_info_create (name, buf, code - buf, ji, unwind_ops);
	g_free (name);

	return buf;
}

#ifdef MONO_ARCH_MONITOR_OBJECT_REG

gpointer
mono_arch_create_monitor_enter_trampoline (MonoTrampInfo **info, gboolean is_v4, gboolean aot)
{
	guint8 *tramp;
	guint8 *code, *buf;
	guint8 *jump_obj_null, *jump_sync_null, *jump_cmpxchg_failed, *jump_other_owner, *jump_tid, *jump_sync_thin_hash = NULL;
	guint8 *jump_lock_taken_true = NULL;
	int tramp_size;
	int status_offset, nest_offset;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;
	int obj_reg = MONO_AMD64_ARG_REG1;
	int lock_taken_reg = MONO_AMD64_ARG_REG2;
	int sync_reg = MONO_AMD64_ARG_REG3;
	int tid_reg = MONO_AMD64_ARG_REG4;
	int status_reg = AMD64_RAX;

	g_assert (MONO_ARCH_MONITOR_OBJECT_REG == obj_reg);
#ifdef MONO_ARCH_MONITOR_LOCK_TAKEN_REG
	g_assert (MONO_ARCH_MONITOR_LOCK_TAKEN_REG == lock_taken_reg);
#else
	g_assert (!is_v4);
#endif

	mono_monitor_threads_sync_members_offset (&status_offset, &nest_offset);
	g_assert (MONO_THREADS_SYNC_MEMBER_SIZE (status_offset) == sizeof (guint32));
	g_assert (MONO_THREADS_SYNC_MEMBER_SIZE (nest_offset) == sizeof (guint32));
	status_offset = MONO_THREADS_SYNC_MEMBER_OFFSET (status_offset);
	nest_offset = MONO_THREADS_SYNC_MEMBER_OFFSET (nest_offset);

	tramp_size = 128;

	code = buf = mono_global_codeman_reserve (tramp_size);

	unwind_ops = mono_arch_get_cie_program ();

	if (!aot && mono_thread_get_tls_offset () != -1) {
		/* MonoObject* obj is in obj_reg */
		/* is obj null? */
		amd64_test_reg_reg (code, obj_reg, obj_reg);
		/* if yes, jump to actual trampoline */
		jump_obj_null = code;
		amd64_branch8 (code, X86_CC_Z, -1, 1);

		if (is_v4) {
			amd64_test_membase_imm (code, lock_taken_reg, 0, 1);
			/* if *lock_taken is 1, jump to actual trampoline */
			jump_lock_taken_true = code;
			x86_branch8 (code, X86_CC_NZ, -1, 1);
		}

		/* load obj->synchronization to sync_reg */
		amd64_mov_reg_membase (code, sync_reg, obj_reg, MONO_STRUCT_OFFSET (MonoObject, synchronisation), 8);

		if (mono_gc_is_moving ()) {
			/*if bit zero is set it's a thin hash*/
			/*FIXME use testb encoding*/
			amd64_test_reg_imm (code, sync_reg, 0x01);
			jump_sync_thin_hash = code;
			amd64_branch8 (code, X86_CC_NE, -1, 1);

			/*clear bits used by the gc*/
			amd64_alu_reg_imm (code, X86_AND, sync_reg, ~0x3);
		}

		/* is synchronization null? */
		amd64_test_reg_reg (code, sync_reg, sync_reg);
		/* if yes, jump to actual trampoline */
		jump_sync_null = code;
		amd64_branch8 (code, X86_CC_Z, -1, 1);

		/* load MonoInternalThread* into tid_reg */
		code = mono_amd64_emit_tls_get (code, tid_reg, mono_thread_get_tls_offset ());
		/* load TID into tid_reg */
		amd64_mov_reg_membase (code, tid_reg, tid_reg, MONO_STRUCT_OFFSET (MonoInternalThread, small_id), 4);

		/* is synchronization->owner free */
		amd64_mov_reg_membase (code, status_reg, sync_reg, status_offset, 4);
		amd64_test_reg_imm_size (code, status_reg, OWNER_MASK, 4);
		/* if not, jump to next case */
		jump_tid = code;
		amd64_branch8 (code, X86_CC_NZ, -1, 1);

		/* if yes, try a compare-exchange with the TID */
		g_assert (tid_reg != X86_EAX);
		/* Form new status in tid_reg */
		amd64_alu_reg_reg_size (code, X86_OR, tid_reg, status_reg, 4);
		/* compare and exchange */
		amd64_prefix (code, X86_LOCK_PREFIX);
		amd64_cmpxchg_membase_reg_size (code, sync_reg, status_offset, tid_reg, 4);
		/* if not successful, jump to actual trampoline */
		jump_cmpxchg_failed = code;
		amd64_branch8 (code, X86_CC_NZ, -1, 1);
		/* if successful, return */
		if (is_v4)
			amd64_mov_membase_imm (code, lock_taken_reg, 0, 1, 1);
		amd64_ret (code);

		/* next case: synchronization->owner is not null */
		x86_patch (jump_tid, code);
		/* is synchronization->owner == TID? */
		amd64_alu_reg_imm_size (code, X86_AND, status_reg, OWNER_MASK, 4);
		amd64_alu_reg_reg_size (code, X86_CMP, status_reg, tid_reg, 4);
		/* if not, jump to actual trampoline */
		jump_other_owner = code;
		amd64_branch8 (code, X86_CC_NZ, -1, 1);
		/* if yes, increment nest */
		amd64_inc_membase_size (code, sync_reg, nest_offset, 4);
		/* return */
		if (is_v4)
			amd64_mov_membase_imm (code, lock_taken_reg, 0, 1, 1);
		amd64_ret (code);

		x86_patch (jump_obj_null, code);
		if (jump_sync_thin_hash)
			x86_patch (jump_sync_thin_hash, code);
		x86_patch (jump_sync_null, code);
		x86_patch (jump_cmpxchg_failed, code);
		x86_patch (jump_other_owner, code);
		if (is_v4)
			x86_patch (jump_lock_taken_true, code);
	}

	/* jump to the actual trampoline */
	if (MONO_AMD64_ARG_REG1 != obj_reg)
		amd64_mov_reg_reg (code, MONO_AMD64_ARG_REG1, obj_reg, sizeof (mgreg_t));

	if (aot) {
		if (is_v4)
			code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, "specific_trampoline_monitor_enter_v4");
		else
			code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, "specific_trampoline_monitor_enter");
		amd64_jump_reg (code, AMD64_R11);
	} else {
		if (is_v4)
			tramp = mono_arch_create_specific_trampoline (NULL, MONO_TRAMPOLINE_MONITOR_ENTER_V4, mono_get_root_domain (), NULL);
		else
			tramp = mono_arch_create_specific_trampoline (NULL, MONO_TRAMPOLINE_MONITOR_ENTER, mono_get_root_domain (), NULL);

		/* jump to the actual trampoline */
		amd64_jump_code (code, tramp);
	}

	nacl_global_codeman_validate (&buf, tramp_size, &code);

	mono_arch_flush_icache (code, code - buf);
	mono_profiler_code_buffer_new (buf, code - buf, MONO_PROFILER_CODE_BUFFER_MONITOR, NULL);
	g_assert (code - buf <= tramp_size);

	if (is_v4)
		*info = mono_tramp_info_create ("monitor_enter_v4_trampoline", buf, code - buf, ji, unwind_ops);
	else
		*info = mono_tramp_info_create ("monitor_enter_trampoline", buf, code - buf, ji, unwind_ops);

	return buf;
}

gpointer
mono_arch_create_monitor_exit_trampoline (MonoTrampInfo **info, gboolean aot)
{
	guint8 *tramp;
	guint8 *code, *buf;
	guint8 *jump_obj_null, *jump_have_waiters, *jump_sync_null, *jump_not_owned, *jump_cmpxchg_failed;
	guint8 *jump_next, *jump_sync_thin_hash = NULL;
	int tramp_size;
	int status_offset, nest_offset;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;
	int obj_reg = MONO_AMD64_ARG_REG1;
	int sync_reg = MONO_AMD64_ARG_REG2;
	int status_reg = MONO_AMD64_ARG_REG3;

	g_assert (obj_reg == MONO_ARCH_MONITOR_OBJECT_REG);

	mono_monitor_threads_sync_members_offset (&status_offset, &nest_offset);
	g_assert (MONO_THREADS_SYNC_MEMBER_SIZE (status_offset) == sizeof (guint32));
	g_assert (MONO_THREADS_SYNC_MEMBER_SIZE (nest_offset) == sizeof (guint32));
	status_offset = MONO_THREADS_SYNC_MEMBER_OFFSET (status_offset);
	nest_offset = MONO_THREADS_SYNC_MEMBER_OFFSET (nest_offset);

	tramp_size = 112;

	code = buf = mono_global_codeman_reserve (tramp_size);

	unwind_ops = mono_arch_get_cie_program ();

	if (!aot && mono_thread_get_tls_offset () != -1) {
		/* MonoObject* obj is in obj_reg */
		/* is obj null? */
		amd64_test_reg_reg (code, obj_reg, obj_reg);
		/* if yes, jump to actual trampoline */
		jump_obj_null = code;
		amd64_branch8 (code, X86_CC_Z, -1, 1);

		/* load obj->synchronization to RCX */
		amd64_mov_reg_membase (code, sync_reg, obj_reg, MONO_STRUCT_OFFSET (MonoObject, synchronisation), 8);

		if (mono_gc_is_moving ()) {
			/*if bit zero is set it's a thin hash*/
			/*FIXME use testb encoding*/
			amd64_test_reg_imm (code, sync_reg, 0x01);
			jump_sync_thin_hash = code;
			amd64_branch8 (code, X86_CC_NE, -1, 1);

			/*clear bits used by the gc*/
			amd64_alu_reg_imm (code, X86_AND, sync_reg, ~0x3);
		}

		/* is synchronization null? */
		amd64_test_reg_reg (code, sync_reg, sync_reg);
		/* if yes, jump to actual trampoline */
		jump_sync_null = code;
		amd64_branch8 (code, X86_CC_Z, -1, 1);

		/* next case: synchronization is not null */
		/* load MonoInternalThread* into RAX */
		code = mono_amd64_emit_tls_get (code, AMD64_RAX, mono_thread_get_tls_offset ());
		/* load TID into RAX */
		amd64_mov_reg_membase (code, AMD64_RAX, AMD64_RAX, MONO_STRUCT_OFFSET (MonoInternalThread, small_id), 4);
		/* is synchronization->owner == TID */
		amd64_mov_reg_membase (code, status_reg, sync_reg, status_offset, 4);
		amd64_alu_reg_reg_size (code, X86_XOR, AMD64_RAX, status_reg, 4);
		amd64_test_reg_imm_size (code, AMD64_RAX, OWNER_MASK, 4);

		/* if no, jump to actual trampoline */
		jump_not_owned = code;
		amd64_branch8 (code, X86_CC_NZ, -1, 1);

		/* next case: synchronization->owner == TID */
		/* is synchronization->nest == 1 */
		amd64_alu_membase_imm_size (code, X86_CMP, sync_reg, nest_offset, 1, 4);
		/* if not, jump to next case */
		jump_next = code;
		amd64_branch8 (code, X86_CC_NZ, -1, 1);
		/* if yes, is synchronization->entry_count greater than zero */
		amd64_test_reg_imm_size (code, status_reg, ENTRY_COUNT_WAITERS, 4);
		/* if not, jump to actual trampoline */
		jump_have_waiters = code;
		amd64_branch8 (code, X86_CC_NZ, -1 , 1);
		/* if yes, try to set synchronization->owner to null and return */
		g_assert (status_reg != AMD64_RAX);
		/* old status in RAX */
		amd64_mov_reg_reg (code, AMD64_RAX, status_reg, 4);
		/* form new status */
		amd64_alu_reg_imm_size (code, X86_AND, status_reg, ENTRY_COUNT_MASK, 4);
		/* compare and exchange */
		amd64_prefix (code, X86_LOCK_PREFIX);
		amd64_cmpxchg_membase_reg_size (code, sync_reg, status_offset, status_reg, 4);
		/* if not successful, jump to actual trampoline */
		jump_cmpxchg_failed = code;
		amd64_branch8 (code, X86_CC_NZ, -1, 1);
		amd64_ret (code);

		/* next case: synchronization->nest is not 1 */
		x86_patch (jump_next, code);
		/* decrease synchronization->nest and return */
		amd64_dec_membase_size (code, sync_reg, nest_offset, 4);
		amd64_ret (code);

		if (jump_sync_thin_hash)
			x86_patch (jump_sync_thin_hash, code);
		x86_patch (jump_obj_null, code);
		x86_patch (jump_have_waiters, code);
		x86_patch (jump_not_owned, code);
		x86_patch (jump_cmpxchg_failed, code);
		x86_patch (jump_sync_null, code);
	}

	/* jump to the actual trampoline */
	if (MONO_AMD64_ARG_REG1 != obj_reg)
		amd64_mov_reg_reg (code, MONO_AMD64_ARG_REG1, obj_reg, sizeof (mgreg_t));

	if (aot) {
		code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, "specific_trampoline_monitor_exit");
		amd64_jump_reg (code, AMD64_R11);
	} else {
		tramp = mono_arch_create_specific_trampoline (NULL, MONO_TRAMPOLINE_MONITOR_EXIT, mono_get_root_domain (), NULL);
		amd64_jump_code (code, tramp);
	}

	nacl_global_codeman_validate (&buf, tramp_size, &code);

	mono_arch_flush_icache (code, code - buf);
	mono_profiler_code_buffer_new (buf, code - buf, MONO_PROFILER_CODE_BUFFER_MONITOR, NULL);
	g_assert (code - buf <= tramp_size);

	*info = mono_tramp_info_create ("monitor_exit_trampoline", buf, code - buf, ji, unwind_ops);

	return buf;
}

#else

gpointer
mono_arch_create_monitor_enter_trampoline (MonoTrampInfo **info, gboolean is_v4, gboolean aot)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_create_monitor_exit_trampoline (MonoTrampInfo **info, gboolean aot)
{
	g_assert_not_reached ();
	return NULL;
}

#endif

void
mono_arch_invalidate_method (MonoJitInfo *ji, void *func, gpointer func_arg)
{
	/* FIXME: This is not thread safe */
	guint8 *code = ji->code_start;

	amd64_mov_reg_imm (code, AMD64_ARG_REG1, func_arg);
	amd64_mov_reg_imm (code, AMD64_R11, func);

	x86_push_imm (code, (guint64)func_arg);
	amd64_call_reg (code, AMD64_R11);
}


static void
handler_block_trampoline_helper (gpointer *ptr)
{
	MonoJitTlsData *jit_tls = mono_native_tls_get_value (mono_jit_tls_id);
	*ptr = jit_tls->handler_block_return_address;
}

gpointer
mono_arch_create_handler_block_trampoline (MonoTrampInfo **info, gboolean aot)
{
	guint8 *tramp = mono_get_trampoline_code (MONO_TRAMPOLINE_HANDLER_BLOCK_GUARD);
	guint8 *code, *buf;
	int tramp_size = 64;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;

	g_assert (!aot);

	code = buf = mono_global_codeman_reserve (tramp_size);

	/*
	This trampoline restore the call chain of the handler block then jumps into the code that deals with it.
	*/
	if (mono_get_jit_tls_offset () != -1) {
		code = mono_amd64_emit_tls_get (code, MONO_AMD64_ARG_REG1, mono_get_jit_tls_offset ());
		amd64_mov_reg_membase (code, MONO_AMD64_ARG_REG1, MONO_AMD64_ARG_REG1, MONO_STRUCT_OFFSET (MonoJitTlsData, handler_block_return_address), 8);
		/* Simulate a call */
		amd64_push_reg (code, AMD64_RAX);
		amd64_jump_code (code, tramp);
	} else {
		/*Slow path uses a c helper*/
		amd64_mov_reg_reg (code, MONO_AMD64_ARG_REG1, AMD64_RSP, 8);
		amd64_mov_reg_imm (code, AMD64_RAX, tramp);
		amd64_push_reg (code, AMD64_RAX);
		amd64_push_reg (code, AMD64_RAX);
		amd64_jump_code (code, handler_block_trampoline_helper);
	}

	mono_arch_flush_icache (buf, code - buf);
	mono_profiler_code_buffer_new (buf, code - buf, MONO_PROFILER_CODE_BUFFER_HELPER, NULL);
	g_assert (code - buf <= tramp_size);

	*info = mono_tramp_info_create ("handler_block_trampoline", buf, code - buf, ji, unwind_ops);

	return buf;
}

/*
 * mono_arch_get_call_target:
 *
 *   Return the address called by the code before CODE if exists.
 */
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

/*
 * mono_arch_get_plt_info_offset:
 *
 *   Return the PLT info offset belonging to the plt entry PLT_ENTRY.
 */
guint32
mono_arch_get_plt_info_offset (guint8 *plt_entry, mgreg_t *regs, guint8 *code)
{
#if defined(__native_client__) || defined(__native_client_codegen__)
	/* 18 = 3 (mov opcode) + 4 (disp) + 10 (nacljmp) + 1 (push opcode) */
	/* See aot-compiler.c arch_emit_plt_entry for details.             */
	return *(guint32*)(plt_entry + 18);
#else
	return *(guint32*)(plt_entry + 6);
#endif
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
	int i, framesize, ctx_offset, cfa_offset, gregs_offset;
	guint8 *code, *buf;
	GSList *unwind_ops = NULL;
	MonoJumpInfo *ji = NULL;

	code = buf = mono_global_codeman_reserve (tramp_size);

	framesize = sizeof (MonoContext);
	framesize = ALIGN_TO (framesize, MONO_ARCH_FRAME_ALIGNMENT);

	// CFA = sp + 8
	cfa_offset = 8;
	mono_add_unwind_op_def_cfa (unwind_ops, code, buf, AMD64_RSP, 8);
	// IP saved at CFA - 8
	mono_add_unwind_op_offset (unwind_ops, code, buf, AMD64_RIP, -cfa_offset);

	amd64_push_reg (code, AMD64_RBP);
	cfa_offset += sizeof(mgreg_t);
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, buf, cfa_offset);
	mono_add_unwind_op_offset (unwind_ops, code, buf, AMD64_RBP, - cfa_offset);

	amd64_mov_reg_reg (code, AMD64_RBP, AMD64_RSP, sizeof(mgreg_t));
	mono_add_unwind_op_def_cfa_reg (unwind_ops, code, buf, AMD64_RBP);
	amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, framesize);

	ctx_offset = 0;
	gregs_offset = ctx_offset + MONO_STRUCT_OFFSET (MonoContext, gregs);

	/* Initialize a MonoContext structure on the stack */
	for (i = 0; i < AMD64_NREG; ++i) {
		if (i != AMD64_RIP && i != AMD64_RSP && i != AMD64_RBP)
			amd64_mov_membase_reg (code, AMD64_RSP, gregs_offset + (i * sizeof (mgreg_t)), i, sizeof (mgreg_t));
	}
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, 0, sizeof (mgreg_t));
	amd64_mov_membase_reg (code, AMD64_RSP, gregs_offset + (AMD64_RBP * sizeof (mgreg_t)), AMD64_R11, sizeof (mgreg_t));
	amd64_lea_membase (code, AMD64_R11, AMD64_RBP, 2 * sizeof (mgreg_t));
	amd64_mov_membase_reg (code, AMD64_RSP, gregs_offset + (AMD64_RSP * sizeof (mgreg_t)), AMD64_R11, sizeof (mgreg_t));
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, sizeof (mgreg_t), sizeof (mgreg_t));
	amd64_mov_membase_reg (code, AMD64_RSP, gregs_offset + (AMD64_RIP * sizeof (mgreg_t)), AMD64_R11, sizeof (mgreg_t));

	/* Call the single step/breakpoint function in sdb */
	amd64_lea_membase (code, AMD64_ARG_REG1, AMD64_RSP, ctx_offset);

	if (aot) {
		if (single_step)
			code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, "debugger_agent_single_step_from_context");
		else
			code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, "debugger_agent_breakpoint_from_context");
		amd64_call_reg (code, AMD64_R11);
	} else {
		if (single_step)
			amd64_call_code (code, debugger_agent_single_step_from_context);
		else
			amd64_call_code (code, debugger_agent_breakpoint_from_context);
	}

	/* Restore registers from ctx */
	for (i = 0; i < AMD64_NREG; ++i) {
		if (i != AMD64_RIP && i != AMD64_RSP && i != AMD64_RBP)
			amd64_mov_reg_membase (code, AMD64_RSP, i, gregs_offset + (i * sizeof (mgreg_t)), sizeof (mgreg_t));
	}
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RSP, gregs_offset + (AMD64_RBP * sizeof (mgreg_t)), sizeof (mgreg_t));
	amd64_mov_membase_reg (code, AMD64_RBP, 0, AMD64_R11, sizeof (mgreg_t));
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RSP, gregs_offset + (AMD64_RIP * sizeof (mgreg_t)), sizeof (mgreg_t));
	amd64_mov_membase_reg (code, AMD64_RBP, sizeof (mgreg_t), AMD64_R11, sizeof (mgreg_t));

	amd64_leave (code);
	amd64_ret (code);

	mono_arch_flush_icache (code, code - buf);
	g_assert (code - buf <= tramp_size);

	const char *tramp_name = single_step ? "sdb_single_step_trampoline" : "sdb_breakpoint_trampoline";
	*info = mono_tramp_info_create (tramp_name, buf, code - buf, ji, unwind_ops);

	return buf;
}
