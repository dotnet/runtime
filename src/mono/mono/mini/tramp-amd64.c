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

#include <mono/metadata/appdomain.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/mono-debug-debugger.h>
#include <mono/metadata/monitor.h>
#include <mono/metadata/monitor.h>
#include <mono/metadata/gc-internal.h>
#include <mono/arch/amd64/amd64-codegen.h>

#include <mono/utils/memcheck.h>

#include "mini.h"
#include "mini-amd64.h"

#if defined(__native_client_codegen__) && defined(__native_client__)
#include <malloc.h>
#include <nacl/nacl_dyncode.h>
#endif

#define IS_REX(inst) (((inst) >= 0x40) && ((inst) <= 0x4f))

static guint8* nullified_class_init_trampoline;

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
#ifdef MONO_ARCH_NOMAP32BIT
				/* Print some diagnostics */
				MonoJitInfo *ji = mono_jit_info_table_find (mono_domain_get (), (char*)orig_code);
				if (ji)
					fprintf (stderr, "At %s, offset 0x%zx\n", mono_method_full_name (ji->method, TRUE), (guint8*)orig_code - (guint8*)ji->code_start);
				fprintf (stderr, "Addr: %p\n", addr);
				ji = mono_jit_info_table_find (mono_domain_get (), (char*)addr);
				if (ji)
					fprintf (stderr, "Callee: %s\n", mono_method_full_name (ji->method, TRUE));
				g_assert_not_reached ();
#else
				/* 
				 * This might happen when calling AOTed code. Create a thunk.
				 */
				guint8 *thunk_start, *thunk_code;

				thunk_start = thunk_code = mono_domain_code_reserve (mono_domain_get (), 32);
				amd64_jump_membase (thunk_code, AMD64_RIP, 0);
				*(guint64*)thunk_code = (guint64)addr;
				addr = thunk_start;
				g_assert ((((guint64)(addr)) >> 32) == 0);
				mono_arch_flush_icache (thunk_start, thunk_code - thunk_start);
#endif
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

void
mono_arch_nullify_class_init_trampoline (guint8 *code, mgreg_t *regs)
{
	guint8 buf [16];
	MonoJitInfo *ji = NULL;
	gboolean can_write;

	if (mono_use_llvm) {
		/* code - 7 might be before the start of the method */
		/* FIXME: Avoid this expensive call somehow */
		ji = mono_jit_info_table_find (mono_domain_get (), (char*)code);
	}

	can_write = mono_breakpoint_clean_code (ji ? ji->code_start : NULL, code, 7, buf, sizeof (buf));

	if (!can_write)
		return;

	/* 
	 * A given byte sequence can match more than case here, so we have to be
	 * really careful about the ordering of the cases. Longer sequences
	 * come first.
	 */
	if ((buf [0] == 0x41) && (buf [1] == 0xff) && (buf [2] == 0x15)) {
		gpointer *vtable_slot;

		/* call *<OFFSET>(%rip) */
		vtable_slot = get_vcall_slot_addr (code, regs);
		g_assert (vtable_slot);

		*vtable_slot = nullified_class_init_trampoline;
	} else if (buf [2] == 0xe8) {
		/* call <TARGET> */
		//guint8 *buf = code - 2;

		/* 
		 * It would be better to replace the call with nops, but that doesn't seem
		 * to work on SMP machines even when the whole call is inside a cache line.
		 * Patching the call address seems to work.
		 */
		/*
		buf [0] = 0x66;
		buf [1] = 0x66;
		buf [2] = 0x90;
		buf [3] = 0x66;
		buf [4] = 0x90;
		*/

		mono_arch_patch_callsite (code - 5, code, nullified_class_init_trampoline);
	} else if ((buf [5] == 0xff) && x86_modrm_mod (buf [6]) == 3 && x86_modrm_reg (buf [6]) == 2) {
		/* call *<reg> */
		/* Generated by the LLVM JIT or on platforms without MAP_32BIT set */
		mono_arch_patch_callsite (code - 13, code, nullified_class_init_trampoline);
	} else if (buf [4] == 0x90 || buf [5] == 0xeb || buf [6] == 0x66) {
		/* Already changed by another thread */
		;
	} else {
		printf ("Invalid trampoline sequence: %x %x %x %x %x %x %x\n", buf [0], buf [1], buf [2], buf [3],
			buf [4], buf [5], buf [6]);
		g_assert_not_reached ();
	}
}

void
mono_arch_nullify_plt_entry (guint8 *code, mgreg_t *regs)
{
	if (mono_aot_only && !nullified_class_init_trampoline)
		nullified_class_init_trampoline = mono_aot_get_trampoline ("nullified_class_init_trampoline");

	mono_arch_patch_plt_entry (code, NULL, regs, nullified_class_init_trampoline);
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
	int i, lmf_offset, offset, res_offset, arg_offset, rax_offset, tramp_offset, saved_regs_offset;
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

	framesize = kMaxCodeSize + sizeof (MonoLMF);
	framesize = (framesize + (MONO_ARCH_FRAME_ALIGNMENT - 1)) & ~ (MONO_ARCH_FRAME_ALIGNMENT - 1);

	orig_rsp_to_rbp_offset = 0;
	r11_save_code = code;
	/* Reserve 5 bytes for the mov_membase_reg to save R11 */
	code += 5;
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

	offset = 0;
	rbp_offset = - offset;

	offset += sizeof(mgreg_t);
	rax_offset = - offset;

	offset += sizeof(mgreg_t);
	tramp_offset = - offset;

	offset += sizeof(gpointer);
	arg_offset = - offset;

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

	offset += sizeof(mgreg_t);
	res_offset = - offset;

	/* Save all registers */

	offset += AMD64_NREG * sizeof(mgreg_t);
	saved_regs_offset = - offset;
	for (i = 0; i < AMD64_NREG; ++i) {
		if (i == AMD64_RBP) {
			/* RAX is already saved */
			amd64_mov_reg_membase (code, AMD64_RAX, AMD64_RBP, rbp_offset, sizeof(mgreg_t));
			amd64_mov_membase_reg (code, AMD64_RBP, saved_regs_offset + (i * sizeof(mgreg_t)), AMD64_RAX, sizeof(mgreg_t));
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
	offset += 8 * sizeof(mgreg_t);
	saved_fpregs_offset = - offset;
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
		amd64_mov_reg_imm (code, AMD64_RDI, tramp_type);
		amd64_mov_reg_imm (code, AMD64_R11, stack_unaligned);
		amd64_call_reg (code, AMD64_R11);
	}
	mono_amd64_patch (br [0], code);
	//amd64_breakpoint (code);
#endif

	if (tramp_type != MONO_TRAMPOLINE_GENERIC_CLASS_INIT &&
		tramp_type != MONO_TRAMPOLINE_MONITOR_ENTER &&
		tramp_type != MONO_TRAMPOLINE_MONITOR_EXIT &&
		tramp_type != MONO_TRAMPOLINE_HANDLER_BLOCK_GUARD) {
		/* Obtain the trampoline argument which is encoded in the instruction stream */
		if (aot) {
			/* Load the GOT offset */
			amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, tramp_offset, sizeof(gpointer));
#if defined(__default_codegen__)
			amd64_mov_reg_membase (code, AMD64_RAX, AMD64_R11, 7, 4);
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

	offset += sizeof (MonoLMF);
	lmf_offset = - offset;

	/* Save ip */
	if (has_caller)
		amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, 8, sizeof(gpointer));
	else
		amd64_mov_reg_imm (code, AMD64_R11, 0);
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, rip), AMD64_R11, sizeof(mgreg_t));
	/* Save fp */
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RSP, framesize, sizeof(mgreg_t));
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, rbp), AMD64_R11, sizeof(mgreg_t));
	/* Save sp */
	amd64_mov_reg_reg (code, AMD64_R11, AMD64_RSP, sizeof(mgreg_t));
	amd64_alu_reg_imm (code, X86_ADD, AMD64_R11, framesize + 16);
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, rsp), AMD64_R11, sizeof(mgreg_t));
	/* Save method */
	if (tramp_type == MONO_TRAMPOLINE_JIT || tramp_type == MONO_TRAMPOLINE_JUMP) {
		amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, arg_offset, sizeof(gpointer));
		amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, method), AMD64_R11, sizeof(gpointer));
	} else {
		amd64_mov_membase_imm (code, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, method), 0, sizeof(gpointer));
	}
	/* Save callee saved regs */
#ifdef TARGET_WIN32
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, rdi), AMD64_RDI, sizeof(mgreg_t));
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, rsi), AMD64_RSI, sizeof(mgreg_t));
#endif
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, rbx), AMD64_RBX, sizeof(mgreg_t));
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, r12), AMD64_R12, sizeof(mgreg_t));
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, r13), AMD64_R13, sizeof(mgreg_t));
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, r14), AMD64_R14, sizeof(mgreg_t));
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, r15), AMD64_R15, sizeof(mgreg_t));

	if (aot) {
		code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, "mono_get_lmf_addr");
	} else {
		amd64_mov_reg_imm (code, AMD64_R11, mono_get_lmf_addr);
	}
	amd64_call_reg (code, AMD64_R11);

	/* Save lmf_addr */
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, lmf_addr), AMD64_RAX, sizeof(gpointer));
	/* Save previous_lmf */
	/* Set the lowest bit to 1 to signal that this LMF has the ip field set */
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RAX, 0, sizeof(gpointer));
	amd64_alu_reg_imm_size (code, X86_ADD, AMD64_R11, 1, sizeof(gpointer));
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, previous_lmf), AMD64_R11, sizeof(gpointer));
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

	amd64_mov_reg_membase (code, AMD64_RCX, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, previous_lmf), sizeof(gpointer));
	amd64_alu_reg_imm_size (code, X86_SUB, AMD64_RCX, 1, sizeof(gpointer));
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, lmf_addr), sizeof(gpointer));
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

	if (tramp_type == MONO_TRAMPOLINE_CLASS_INIT) {
		/* Initialize the nullified class init trampoline used in the AOT case */
		nullified_class_init_trampoline = mono_arch_get_nullified_class_init_trampoline (NULL);
	}

	if (info) {
		tramp_name = mono_get_generic_trampoline_name (tramp_type);
		*info = mono_tramp_info_create (tramp_name, buf, code - buf, ji, unwind_ops);
		g_free (tramp_name);
	}

	return buf;
}

gpointer
mono_arch_get_nullified_class_init_trampoline (MonoTrampInfo **info)
{
	guint8 *code, *buf;
	int size = NACL_SIZE (16, 32);

	code = buf = mono_global_codeman_reserve (size);
	amd64_ret (code);

	nacl_global_codeman_validate(&buf, size, &code);

	mono_arch_flush_icache (buf, code - buf);

	if (info)
		*info = mono_tramp_info_create ("nullified_class_init_trampoline", buf, code - buf, NULL, NULL);

	if (mono_jit_map_is_enabled ())
		mono_emit_jit_tramp (buf, code - buf, "nullified_class_init_trampoline");

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
		amd64_mov_reg_membase (code, AMD64_RAX, AMD64_ARG_REG1, G_STRUCT_OFFSET (MonoVTable, runtime_generic_context), sizeof(gpointer));
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

	g_assert (code - buf <= tramp_size);

	if (info) {
		char *name = mono_get_rgctx_fetch_trampoline_name (slot);
		*info = mono_tramp_info_create (name, buf, code - buf, ji, unwind_ops);
		g_free (name);
	}

	return buf;
}

gpointer
mono_arch_create_generic_class_init_trampoline (MonoTrampInfo **info, gboolean aot)
{
	guint8 *tramp;
	guint8 *code, *buf;
	static int byte_offset = -1;
	static guint8 bitmask;
	guint8 *jump;
	int tramp_size;
	GSList *unwind_ops = NULL;
	MonoJumpInfo *ji = NULL;

	tramp_size = 64;

	code = buf = mono_global_codeman_reserve (tramp_size);

	if (byte_offset < 0)
		mono_marshal_find_bitfield_offset (MonoVTable, initialized, &byte_offset, &bitmask);

	amd64_test_membase_imm_size (code, MONO_AMD64_ARG_REG1, byte_offset, bitmask, 1);
	jump = code;
	amd64_branch8 (code, X86_CC_Z, -1, 1);

	amd64_ret (code);

	x86_patch (jump, code);

	if (aot) {
		code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, "specific_trampoline_generic_class_init");
		amd64_jump_reg (code, AMD64_R11);
	} else {
		tramp = mono_arch_create_specific_trampoline (NULL, MONO_TRAMPOLINE_GENERIC_CLASS_INIT, mono_get_root_domain (), NULL);

		/* jump to the actual trampoline */
		amd64_jump_code (code, tramp);
	}

	nacl_global_codeman_validate (&buf, tramp_size, &code);

	mono_arch_flush_icache (buf, code - buf);

	g_assert (code - buf <= tramp_size);

	if (info)
		*info = mono_tramp_info_create ("generic_class_init_trampoline", buf, code - buf, ji, unwind_ops);

	return buf;
}

#ifdef MONO_ARCH_MONITOR_OBJECT_REG

gpointer
mono_arch_create_monitor_enter_trampoline (MonoTrampInfo **info, gboolean aot)
{
	guint8 *tramp;
	guint8 *code, *buf;
	guint8 *jump_obj_null, *jump_sync_null, *jump_cmpxchg_failed, *jump_other_owner, *jump_tid, *jump_sync_thin_hash = NULL;
	int tramp_size;
	int owner_offset, nest_offset, dummy;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;

	g_assert (MONO_ARCH_MONITOR_OBJECT_REG == AMD64_RDI);

	mono_monitor_threads_sync_members_offset (&owner_offset, &nest_offset, &dummy);
	g_assert (MONO_THREADS_SYNC_MEMBER_SIZE (owner_offset) == sizeof (gpointer));
	g_assert (MONO_THREADS_SYNC_MEMBER_SIZE (nest_offset) == sizeof (guint32));
	owner_offset = MONO_THREADS_SYNC_MEMBER_OFFSET (owner_offset);
	nest_offset = MONO_THREADS_SYNC_MEMBER_OFFSET (nest_offset);

	tramp_size = 96;

	code = buf = mono_global_codeman_reserve (tramp_size);

	unwind_ops = mono_arch_get_cie_program ();

	if (mono_thread_get_tls_offset () != -1) {
		/* MonoObject* obj is in RDI */
		/* is obj null? */
		amd64_test_reg_reg (code, AMD64_RDI, AMD64_RDI);
		/* if yes, jump to actual trampoline */
		jump_obj_null = code;
		amd64_branch8 (code, X86_CC_Z, -1, 1);

		/* load obj->synchronization to RCX */
		amd64_mov_reg_membase (code, AMD64_RCX, AMD64_RDI, G_STRUCT_OFFSET (MonoObject, synchronisation), 8);

		if (mono_gc_is_moving ()) {
			/*if bit zero is set it's a thin hash*/
			/*FIXME use testb encoding*/
			amd64_test_reg_imm (code, AMD64_RCX, 0x01);
			jump_sync_thin_hash = code;
			amd64_branch8 (code, X86_CC_NE, -1, 1);

			/*clear bits used by the gc*/
			amd64_alu_reg_imm (code, X86_AND, AMD64_RCX, ~0x3);
		}

		/* is synchronization null? */
		amd64_test_reg_reg (code, AMD64_RCX, AMD64_RCX);
		/* if yes, jump to actual trampoline */
		jump_sync_null = code;
		amd64_branch8 (code, X86_CC_Z, -1, 1);

		/* load MonoInternalThread* into RDX */
		code = mono_amd64_emit_tls_get (code, AMD64_RDX, mono_thread_get_tls_offset ());
		/* load TID into RDX */
		amd64_mov_reg_membase (code, AMD64_RDX, AMD64_RDX, G_STRUCT_OFFSET (MonoInternalThread, tid), 8);

		/* is synchronization->owner null? */
		amd64_alu_membase_imm_size (code, X86_CMP, AMD64_RCX, owner_offset, 0, 8);
		/* if not, jump to next case */
		jump_tid = code;
		amd64_branch8 (code, X86_CC_NZ, -1, 1);

		/* if yes, try a compare-exchange with the TID */
		/* zero RAX */
		amd64_alu_reg_reg (code, X86_XOR, AMD64_RAX, AMD64_RAX);
		/* compare and exchange */
		amd64_prefix (code, X86_LOCK_PREFIX);
		amd64_cmpxchg_membase_reg_size (code, AMD64_RCX, owner_offset, AMD64_RDX, 8);
		/* if not successful, jump to actual trampoline */
		jump_cmpxchg_failed = code;
		amd64_branch8 (code, X86_CC_NZ, -1, 1);
		/* if successful, return */
		amd64_ret (code);

		/* next case: synchronization->owner is not null */
		x86_patch (jump_tid, code);
		/* is synchronization->owner == TID? */
		amd64_alu_membase_reg_size (code, X86_CMP, AMD64_RCX, owner_offset, AMD64_RDX, 8);
		/* if not, jump to actual trampoline */
		jump_other_owner = code;
		amd64_branch8 (code, X86_CC_NZ, -1, 1);
		/* if yes, increment nest */
		amd64_inc_membase_size (code, AMD64_RCX, nest_offset, 4);
		/* return */
		amd64_ret (code);

		x86_patch (jump_obj_null, code);
		if (jump_sync_thin_hash)
			x86_patch (jump_sync_thin_hash, code);
		x86_patch (jump_sync_null, code);
		x86_patch (jump_cmpxchg_failed, code);
		x86_patch (jump_other_owner, code);
	}

	/* jump to the actual trampoline */
#if MONO_AMD64_ARG_REG1 != AMD64_RDI
	amd64_mov_reg_reg (code, MONO_AMD64_ARG_REG1, AMD64_RDI);
#endif

	if (aot) {
		code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, "specific_trampoline_monitor_enter");
		amd64_jump_reg (code, AMD64_R11);
	} else {
		tramp = mono_arch_create_specific_trampoline (NULL, MONO_TRAMPOLINE_MONITOR_ENTER, mono_get_root_domain (), NULL);

		/* jump to the actual trampoline */
		amd64_jump_code (code, tramp);
	}

	nacl_global_codeman_validate (&buf, tramp_size, &code);

	mono_arch_flush_icache (code, code - buf);
	g_assert (code - buf <= tramp_size);

	if (info)
		*info = mono_tramp_info_create ("monitor_enter_trampoline", buf, code - buf, ji, unwind_ops);

	return buf;
}

gpointer
mono_arch_create_monitor_exit_trampoline (MonoTrampInfo **info, gboolean aot)
{
	guint8 *tramp;
	guint8 *code, *buf;
	guint8 *jump_obj_null, *jump_have_waiters, *jump_sync_null, *jump_not_owned, *jump_sync_thin_hash = NULL;
	guint8 *jump_next;
	int tramp_size;
	int owner_offset, nest_offset, entry_count_offset;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;

	g_assert (MONO_ARCH_MONITOR_OBJECT_REG == AMD64_RDI);

	mono_monitor_threads_sync_members_offset (&owner_offset, &nest_offset, &entry_count_offset);
	g_assert (MONO_THREADS_SYNC_MEMBER_SIZE (owner_offset) == sizeof (gpointer));
	g_assert (MONO_THREADS_SYNC_MEMBER_SIZE (nest_offset) == sizeof (guint32));
	g_assert (MONO_THREADS_SYNC_MEMBER_SIZE (entry_count_offset) == sizeof (gint32));
	owner_offset = MONO_THREADS_SYNC_MEMBER_OFFSET (owner_offset);
	nest_offset = MONO_THREADS_SYNC_MEMBER_OFFSET (nest_offset);
	entry_count_offset = MONO_THREADS_SYNC_MEMBER_OFFSET (entry_count_offset);

	tramp_size = 112;

	code = buf = mono_global_codeman_reserve (tramp_size);

	unwind_ops = mono_arch_get_cie_program ();

	if (mono_thread_get_tls_offset () != -1) {
		/* MonoObject* obj is in RDI */
		/* is obj null? */
		amd64_test_reg_reg (code, AMD64_RDI, AMD64_RDI);
		/* if yes, jump to actual trampoline */
		jump_obj_null = code;
		amd64_branch8 (code, X86_CC_Z, -1, 1);

		/* load obj->synchronization to RCX */
		amd64_mov_reg_membase (code, AMD64_RCX, AMD64_RDI, G_STRUCT_OFFSET (MonoObject, synchronisation), 8);

		if (mono_gc_is_moving ()) {
			/*if bit zero is set it's a thin hash*/
			/*FIXME use testb encoding*/
			amd64_test_reg_imm (code, AMD64_RCX, 0x01);
			jump_sync_thin_hash = code;
			amd64_branch8 (code, X86_CC_NE, -1, 1);

			/*clear bits used by the gc*/
			amd64_alu_reg_imm (code, X86_AND, AMD64_RCX, ~0x3);
		}

		/* is synchronization null? */
		amd64_test_reg_reg (code, AMD64_RCX, AMD64_RCX);
		/* if yes, jump to actual trampoline */
		jump_sync_null = code;
		amd64_branch8 (code, X86_CC_Z, -1, 1);

		/* next case: synchronization is not null */
		/* load MonoInternalThread* into RDX */
		code = mono_amd64_emit_tls_get (code, AMD64_RDX, mono_thread_get_tls_offset ());
		/* load TID into RDX */
		amd64_mov_reg_membase (code, AMD64_RDX, AMD64_RDX, G_STRUCT_OFFSET (MonoInternalThread, tid), 8);
		/* is synchronization->owner == TID */
		amd64_alu_membase_reg_size (code, X86_CMP, AMD64_RCX, owner_offset, AMD64_RDX, 8);
		/* if no, jump to actual trampoline */
		jump_not_owned = code;
		amd64_branch8 (code, X86_CC_NZ, -1, 1);

		/* next case: synchronization->owner == TID */
		/* is synchronization->nest == 1 */
		amd64_alu_membase_imm_size (code, X86_CMP, AMD64_RCX, nest_offset, 1, 4);
		/* if not, jump to next case */
		jump_next = code;
		amd64_branch8 (code, X86_CC_NZ, -1, 1);
		/* if yes, is synchronization->entry_count zero? */
		amd64_alu_membase_imm_size (code, X86_CMP, AMD64_RCX, entry_count_offset, 0, 4);
		/* if not, jump to actual trampoline */
		jump_have_waiters = code;
		amd64_branch8 (code, X86_CC_NZ, -1 , 1);
		/* if yes, set synchronization->owner to null and return */
		amd64_mov_membase_imm (code, AMD64_RCX, owner_offset, 0, 8);
		amd64_ret (code);

		/* next case: synchronization->nest is not 1 */
		x86_patch (jump_next, code);
		/* decrease synchronization->nest and return */
		amd64_dec_membase_size (code, AMD64_RCX, nest_offset, 4);
		amd64_ret (code);

		x86_patch (jump_obj_null, code);
		x86_patch (jump_have_waiters, code);
		x86_patch (jump_not_owned, code);
		x86_patch (jump_sync_null, code);
	}

	/* jump to the actual trampoline */
#if MONO_AMD64_ARG_REG1 != AMD64_RDI
	amd64_mov_reg_reg (code, MONO_AMD64_ARG_REG1, AMD64_RDI);
#endif

	if (aot) {
		code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, "specific_trampoline_monitor_exit");
		amd64_jump_reg (code, AMD64_R11);
	} else {
		tramp = mono_arch_create_specific_trampoline (NULL, MONO_TRAMPOLINE_MONITOR_EXIT, mono_get_root_domain (), NULL);
		amd64_jump_code (code, tramp);
	}

	nacl_global_codeman_validate (&buf, tramp_size, &code);

	mono_arch_flush_icache (code, code - buf);
	g_assert (code - buf <= tramp_size);

	if (info)
		*info = mono_tramp_info_create ("monitor_exit_trampoline", buf, code - buf, ji, unwind_ops);

	return buf;
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
mono_arch_create_handler_block_trampoline (void)
{
	guint8 *tramp = mono_get_trampoline_code (MONO_TRAMPOLINE_HANDLER_BLOCK_GUARD);
	guint8 *code, *buf;
	int tramp_size = 64;
	code = buf = mono_global_codeman_reserve (tramp_size);

	/*
	This trampoline restore the call chain of the handler block then jumps into the code that deals with it.
	*/

	if (mono_get_jit_tls_offset () != -1) {
		code = mono_amd64_emit_tls_get (code, AMD64_RDI, mono_get_jit_tls_offset ());
		amd64_mov_reg_membase (code, AMD64_RDI, AMD64_RDI, G_STRUCT_OFFSET (MonoJitTlsData, handler_block_return_address), 8);
		/* Simulate a call */
		amd64_push_reg (code, AMD64_RAX);
		amd64_jump_code (code, tramp);
	} else {
		/*Slow path uses a c helper*/
		amd64_mov_reg_reg (code, AMD64_RDI, AMD64_RSP, 8);
		amd64_mov_reg_imm (code, AMD64_RAX, tramp);
		amd64_push_reg (code, AMD64_RAX);
		amd64_jump_code (code, handler_block_trampoline_helper);
	}

	mono_arch_flush_icache (buf, code - buf);
	g_assert (code - buf <= tramp_size);

	if (mono_jit_map_is_enabled ())
		mono_emit_jit_tramp (buf, code - buf, "handler_block_trampoline");

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
		guint32 disp = *(guint32*)(code - 4);
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
