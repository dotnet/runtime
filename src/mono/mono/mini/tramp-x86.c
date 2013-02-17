/*
 * tramp-x86.c: JIT trampoline code for x86
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/mono-debug-debugger.h>
#include <mono/metadata/monitor.h>
#include <mono/metadata/gc-internal.h>
#include <mono/arch/x86/x86-codegen.h>

#include <mono/utils/memcheck.h>

#include "mini.h"
#include "mini-x86.h"

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
	int this_pos = 4;
	MonoDomain *domain = mono_domain_get ();

	start = code = mono_domain_code_reserve (domain, 16);

	x86_alu_membase_imm (code, X86_ADD, X86_ESP, this_pos, sizeof (MonoObject));
	x86_jump_code (code, addr);
	g_assert ((code - start) < 16);

	nacl_domain_code_validate (domain, &start, 16, &code);

	return start;
}

gpointer
mono_arch_get_static_rgctx_trampoline (MonoMethod *m, MonoMethodRuntimeGenericContext *mrgctx, gpointer addr)
{
	guint8 *code, *start;
	int buf_len;

	MonoDomain *domain = mono_domain_get ();

	buf_len = 10;

	start = code = mono_domain_code_reserve (domain, buf_len);

	x86_mov_reg_imm (code, MONO_ARCH_RGCTX_REG, mrgctx);
	x86_jump_code (code, addr);
	g_assert ((code - start) <= buf_len);

	nacl_domain_code_validate (domain, &start, buf_len, &code);
	mono_arch_flush_icache (start, code - start);

	return start;
}

gpointer
mono_arch_get_llvm_imt_trampoline (MonoDomain *domain, MonoMethod *m, int vt_offset)
{
	guint8 *code, *start;
	int buf_len;
	int this_offset;

	buf_len = 32;

	start = code = mono_domain_code_reserve (domain, buf_len);

	this_offset = mono_x86_get_this_arg_offset (NULL, mono_method_signature (m));

	/* Set imt arg */
	x86_mov_reg_imm (code, MONO_ARCH_IMT_REG, m);
	/* Load this */
	x86_mov_reg_membase (code, X86_EAX, X86_ESP, this_offset + 4, 4);
	/* Load vtable address */
	x86_mov_reg_membase (code, X86_EAX, X86_EAX, 0, 4);
	x86_jump_membase (code, X86_EAX, vt_offset);

	g_assert ((code - start) < buf_len);

	nacl_domain_code_validate (domain, &start, buf_len, &code);

	mono_arch_flush_icache (start, code - start);

	return start;
}

void
mono_arch_patch_callsite (guint8 *method_start, guint8 *orig_code, guint8 *addr)
{
#if defined(__default_codegen__)
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
			InterlockedExchange ((gint32*)(orig_code + 2), (guint)addr - ((guint)orig_code + 1) - 5);

			/* Tell valgrind to recompile the patched code */
			VALGRIND_DISCARD_TRANSLATIONS (orig_code + 2, 4);
		}
	} else if (code [1] == 0xe9) {
		/* A PLT entry: jmp <DISP> */
		if (can_write)
			InterlockedExchange ((gint32*)(orig_code + 2), (guint)addr - ((guint)orig_code + 1) - 5);
	} else {
		printf ("Invalid trampoline sequence: %x %x %x %x %x %x %x\n", code [0], code [1], code [2], code [3],
				code [4], code [5], code [6]);
		g_assert_not_reached ();
	}
#elif defined(__native_client__)
	/* Target must be bundle-aligned */
	g_assert (((guint32)addr & kNaClAlignmentMask) == 0);

	/* 0xe8 = call <DISP>, 0xe9 = jump <DISP> */
	if ((orig_code [-5] == 0xe8) || orig_code [-6] == 0xe9) {
		int ret;
		gint32 offset = (gint32)addr - (gint32)orig_code;
		guint8 buf[sizeof(gint32)];
		*((gint32*)(buf)) = offset;
		ret = nacl_dyncode_modify (orig_code - sizeof(gint32), buf, sizeof(gint32));
		g_assert (ret == 0);
	} else {
		printf ("Invalid trampoline sequence %p: %02x %02x %02x %02x %02x\n", orig_code, orig_code [-5], orig_code [-4], orig_code [-3], orig_code [-2], orig_code[-1]);
		g_assert_not_reached ();
	}
#endif
}

void
mono_arch_patch_plt_entry (guint8 *code, gpointer *got, mgreg_t *regs, guint8 *addr)
{
	guint32 offset;

	/* Patch the jump table entry used by the plt entry */

#if defined(__native_client_codegen__) || defined(__native_client__)
	/* for both compiler and runtime      */
	/* A PLT entry:                       */
	/*        mov <DISP>(%ebx), %ecx      */
	/*        and 0xffffffe0, %ecx        */
	/*        jmp *%ecx                   */
	g_assert (code [0] == 0x8b);
	g_assert (code [1] == 0x8b);

	offset = *(guint32*)(code + 2);
#elif defined(__default_codegen__)
	/* A PLT entry: jmp *<DISP>(%ebx) */
	g_assert (code [0] == 0xff);
	g_assert (code [1] == 0xa3);

	offset = *(guint32*)(code + 2);
#endif  /* __native_client_codegen__ */
	if (!got)
		got = (gpointer*)(gsize) regs [MONO_ARCH_GOT_REG];
	*(guint8**)((guint8*)got + offset) = addr;
}

static gpointer
get_vcall_slot (guint8 *code, mgreg_t *regs, int *displacement)
{
	const int kBufSize = NACL_SIZE (8, 16);
	guint8 buf [64];
	guint8 reg = 0;
	gint32 disp = 0;

	mono_breakpoint_clean_code (NULL, code, kBufSize, buf, kBufSize);
	code = buf + 8;

	*displacement = 0;

	if ((code [0] == 0xff) && ((code [1] & 0x18) == 0x10) && ((code [1] >> 6) == 2)) {
		reg = code [1] & 0x07;
		disp = *((gint32*)(code + 2));
#if defined(__native_client_codegen__) || defined(__native_client__)
	} else if ((code[1] == 0x83) && (code[2] == 0xe1) && (code[4] == 0xff) &&
			   (code[5] == 0xd1) && (code[-5] == 0x8b)) {
		disp = *((gint32*)(code - 3));
		reg = code[-4] & 0x07;
	} else if ((code[-2] == 0x8b) && (code[1] == 0x83) && (code[4] == 0xff)) {
		reg = code[-1] & 0x07;
		disp = (signed char)code[0];
#endif
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

void
mono_arch_nullify_class_init_trampoline (guint8 *code, mgreg_t *regs)
{
	guint8 buf [16];
	gboolean can_write = mono_breakpoint_clean_code (NULL, code, 6, buf, sizeof (buf));

	if (!can_write)
		return;

	code -= 5;
	if (code [0] == 0xe8) {
#if defined(__default_codegen__)
		if (!mono_running_on_valgrind ()) {
			guint32 ops;
			/*
			 * Thread safe code patching using the algorithm from the paper
			 * 'Practicing JUDO: Java Under Dynamic Optimizations'
			 */
			/* 
			 * First atomically change the the first 2 bytes of the call to a
			 * spinning jump.
			 */
			ops = 0xfeeb;
			InterlockedExchange ((gint32*)code, ops);

			/* Then change the other bytes to a nop */
			code [2] = 0x90;
			code [3] = 0x90;
			code [4] = 0x90;

			/* Then atomically change the first 4 bytes to a nop as well */
			ops = 0x90909090;
			InterlockedExchange ((gint32*)code, ops);
			/* FIXME: the calltree skin trips on the self modifying code above */

			/* Tell valgrind to recompile the patched code */
			//VALGRIND_DISCARD_TRANSLATIONS (code, 8);
		}
#elif defined(__native_client_codegen__)
		mono_arch_patch_callsite (code, code + 5, nullified_class_init_trampoline);
#endif
	} else if (code [0] == 0x90 || code [0] == 0xeb) {
		/* Already changed by another thread */
		;
	} else if ((code [-1] == 0xff) && (x86_modrm_reg (code [0]) == 0x2)) {
		/* call *<OFFSET>(<REG>) -> Call made from AOT code */
		gpointer *vtable_slot;

		vtable_slot = get_vcall_slot_addr (code + 5, regs);
		g_assert (vtable_slot);

		*vtable_slot = nullified_class_init_trampoline;
	} else {
			printf ("Invalid trampoline sequence: %x %x %x %x %x %x %x\n", code [0], code [1], code [2], code [3],
				code [4], code [5], code [6]);
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

guchar*
mono_arch_create_generic_trampoline (MonoTrampolineType tramp_type, MonoTrampInfo **info, gboolean aot)
{
	guint8 *buf, *code, *tramp;
	int pushed_args, pushed_args_caller_saved;
	GSList *unwind_ops = NULL;
	MonoJumpInfo *ji = NULL;

	unwind_ops = mono_arch_get_cie_program ();

	code = buf = mono_global_codeman_reserve (256);

	/* Note that there is a single argument to the trampoline
	 * and it is stored at: esp + pushed_args * sizeof (gpointer)
	 * the ret address is at: esp + (pushed_args + 1) * sizeof (gpointer)
	 */

	/* Put all registers into an array on the stack
	 * If this code is changed, make sure to update the offset value in
	 * mono_arch_get_this_arg_from_call () in mini-x86.c.
	 */
	x86_push_reg (code, X86_EDI);
	x86_push_reg (code, X86_ESI);
	x86_push_reg (code, X86_EBP);
	x86_push_reg (code, X86_ESP);
	x86_push_reg (code, X86_EBX);
	x86_push_reg (code, X86_EDX);
	x86_push_reg (code, X86_ECX);
	x86_push_reg (code, X86_EAX);

	pushed_args_caller_saved = pushed_args = 8;

	/* Align stack on apple */
	x86_alu_reg_imm (code, X86_SUB, X86_ESP, 4);

	pushed_args ++;

	/* save LMF begin */

	/* save the IP (caller ip) */
	if (tramp_type == MONO_TRAMPOLINE_JUMP)
		x86_push_imm (code, 0);
	else
		x86_push_membase (code, X86_ESP, (pushed_args + 1) * sizeof (gpointer));

	pushed_args++;

	x86_push_reg (code, X86_EBP);
	x86_push_reg (code, X86_ESI);
	x86_push_reg (code, X86_EDI);
	x86_push_reg (code, X86_EBX);

	pushed_args += 4;

	/* save ESP */
	x86_push_reg (code, X86_ESP);
	/* Adjust ESP so it points to the previous frame */
	x86_alu_membase_imm (code, X86_ADD, X86_ESP, 0, (pushed_args + 2) * 4);

	pushed_args ++;

	/* save method info */
	if ((tramp_type == MONO_TRAMPOLINE_JIT) || (tramp_type == MONO_TRAMPOLINE_JUMP))
		x86_push_membase (code, X86_ESP, pushed_args * sizeof (gpointer));
	else
		x86_push_imm (code, 0);

	pushed_args++;

	/* On apple, the stack is correctly aligned to 16 bytes because pushed_args is
	 * 16 and there is the extra trampoline arg + the return ip pushed by call
	 * FIXME: Note that if an exception happens while some args are pushed
	 * on the stack, the stack will be misaligned.
	 */
	g_assert (pushed_args == 16);

	/* get the address of lmf for the current thread */
	if (aot) {
		code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, "mono_get_lmf_addr");
		x86_call_reg (code, X86_EAX);
	} else {
		x86_call_code (code, mono_get_lmf_addr);
	}
	/* push lmf */
	x86_push_reg (code, X86_EAX); 
	/* push *lfm (previous_lmf) */
	x86_push_membase (code, X86_EAX, 0);
	/* Signal to mono_arch_find_jit_info () that this is a trampoline frame */
	x86_alu_membase_imm (code, X86_ADD, X86_ESP, 0, 1);
	/* *(lmf) = ESP */
	x86_mov_membase_reg (code, X86_EAX, 0, X86_ESP, 4);
	/* save LFM end */

	pushed_args += 2;

	/* starting the call sequence */

	/* FIXME: Push the trampoline address */
	x86_push_imm (code, 0);

	pushed_args++;

	/* push the method info */
	x86_push_membase (code, X86_ESP, pushed_args * sizeof (gpointer));

	pushed_args++;

	/* push the return address onto the stack */
	if (tramp_type == MONO_TRAMPOLINE_JUMP)
		x86_push_imm (code, 0);
	else
		x86_push_membase (code, X86_ESP, (pushed_args + 1) * sizeof (gpointer));
	pushed_args++;
	/* push the address of the register array */
	x86_lea_membase (code, X86_EAX, X86_ESP, (pushed_args - 8) * sizeof (gpointer));
	x86_push_reg (code, X86_EAX);

	pushed_args++;

#ifdef __APPLE__
	/* check the stack is aligned after the ret ip is pushed */
	/*x86_mov_reg_reg (buf, X86_EDX, X86_ESP, 4);
	x86_alu_reg_imm (buf, X86_AND, X86_EDX, 15);
	x86_alu_reg_imm (buf, X86_CMP, X86_EDX, 0);
	x86_branch_disp (buf, X86_CC_Z, 3, FALSE);
	x86_breakpoint (buf);*/
#endif

	mono_add_unwind_op_def_cfa (unwind_ops, code, buf, X86_ESP, ((pushed_args + 2) * 4));

	if (aot) {
		char *icall_name = g_strdup_printf ("trampoline_func_%d", tramp_type);
		code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, icall_name);
		x86_call_reg (code, X86_EAX);
	} else {
		tramp = (guint8*)mono_get_trampoline_func (tramp_type);
		x86_call_code (code, tramp);
	}

	x86_alu_reg_imm (code, X86_ADD, X86_ESP, 4*4);

	pushed_args -= 4;

	/* Check for thread interruption */
	/* This is not perf critical code so no need to check the interrupt flag */
	/* Align the stack on osx */
	x86_alu_reg_imm (code, X86_SUB, X86_ESP, 3 * 4);
	x86_push_reg (code, X86_EAX);
	if (aot) {
		code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, "mono_thread_force_interruption_checkpoint");
		x86_call_reg (code, X86_EAX);
	} else {
		x86_call_code (code, (guint8*)mono_thread_force_interruption_checkpoint);
	}
	x86_pop_reg (code, X86_EAX);
	x86_alu_reg_imm (code, X86_ADD, X86_ESP, 3 * 4);

	/* Restore LMF */

	/* ebx = previous_lmf */
	x86_pop_reg (code, X86_EBX);
	pushed_args--;
	x86_alu_reg_imm (code, X86_SUB, X86_EBX, 1);

	/* edi = lmf */
	x86_pop_reg (code, X86_EDI);
	pushed_args--;

	/* *(lmf) = previous_lmf */
	x86_mov_membase_reg (code, X86_EDI, 0, X86_EBX, 4);

	/* discard method info */
	x86_pop_reg (code, X86_ESI);
	pushed_args--;

	/* discard ESP */
	x86_pop_reg (code, X86_ESI);
	pushed_args--;

	/* restore caller saved regs */
	x86_pop_reg (code, X86_EBX);
	x86_pop_reg (code, X86_EDI);
	x86_pop_reg (code, X86_ESI);
	x86_pop_reg (code, X86_EBP);

	pushed_args -= 4;

	/* discard save IP */
	x86_alu_reg_imm (code, X86_ADD, X86_ESP, 4);
	pushed_args--;

	/* restore LMF end */

	if (!MONO_TRAMPOLINE_TYPE_MUST_RETURN (tramp_type)) {
		/* 
		 * Overwrite the method ptr with the address we need to jump to,
		 * to free %eax.
		 */
		x86_mov_membase_reg (code, X86_ESP, pushed_args * sizeof (gpointer), X86_EAX, 4);
	}

	/* Restore caller saved registers */
	x86_mov_reg_membase (code, X86_ECX, X86_ESP, (pushed_args - pushed_args_caller_saved + X86_ECX) * 4, 4);
	x86_mov_reg_membase (code, X86_EDX, X86_ESP, (pushed_args - pushed_args_caller_saved + X86_EDX) * 4, 4);
	if ((tramp_type == MONO_TRAMPOLINE_RESTORE_STACK_PROT) || (tramp_type == MONO_TRAMPOLINE_AOT_PLT))
		x86_mov_reg_membase (code, X86_EAX, X86_ESP, (pushed_args - pushed_args_caller_saved + X86_EAX) * 4, 4);

	if (!MONO_TRAMPOLINE_TYPE_MUST_RETURN (tramp_type)) {
		/* Pop saved reg array + stack align */
		x86_alu_reg_imm (code, X86_ADD, X86_ESP, 9 * 4);
		pushed_args -= 9;
		g_assert (pushed_args == 0);
	} else {
		/* Pop saved reg array + stack align + method ptr */
		x86_alu_reg_imm (code, X86_ADD, X86_ESP, 10 * 4);
		pushed_args -= 10;

		/* We've popped one more stack item than we've pushed (the
		   method ptr argument), so we must end up at -1. */
		g_assert (pushed_args == -1);
	}

	x86_ret (code);

	nacl_global_codeman_validate (&buf, 256, &code);
	g_assert ((code - buf) <= 256);

	if (info)
		*info = mono_tramp_info_create (mono_get_generic_trampoline_name (tramp_type), buf, code - buf, ji, unwind_ops);

	if (tramp_type == MONO_TRAMPOLINE_CLASS_INIT) {
		/* Initialize the nullified class init trampoline used in the AOT case */
		nullified_class_init_trampoline = mono_arch_get_nullified_class_init_trampoline (NULL);
	}

	return buf;
}

gpointer
mono_arch_get_nullified_class_init_trampoline (MonoTrampInfo **info)
{
	guint8 *code, *buf;
	int tramp_size = NACL_SIZE (16, kNaClAlignment);		

	code = buf = mono_global_codeman_reserve (tramp_size);
	x86_ret (code);

	nacl_global_codeman_validate (&buf, tramp_size, &code);

	mono_arch_flush_icache (buf, code - buf);

	if (info)
		*info = mono_tramp_info_create (g_strdup_printf ("nullified_class_init_trampoline"), buf, code - buf, NULL, NULL);

	if (mono_jit_map_is_enabled ())
		mono_emit_jit_tramp (buf, code - buf, "nullified_class_init_trampoline");

	return buf;
}

#define TRAMPOLINE_SIZE 10

gpointer
mono_arch_create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type, MonoDomain *domain, guint32 *code_len)
{
	guint8 *code, *buf, *tramp;
	
	tramp = mono_get_trampoline_code (tramp_type);

	code = buf = mono_domain_code_reserve_align (domain, TRAMPOLINE_SIZE, NACL_SIZE (4, kNaClAlignment));

	x86_push_imm (buf, arg1);
	x86_jump_code (buf, tramp);
	g_assert ((buf - code) <= TRAMPOLINE_SIZE);

	nacl_domain_code_validate (domain, &code, NACL_SIZE (4, kNaClAlignment), &buf);

	mono_arch_flush_icache (code, buf - code);

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

#if defined(__default_codegen__)
	tramp_size = (aot ? 64 : 36) + 6 * depth;
#elif defined(__native_client_codegen__)
	tramp_size = (aot ? 64 : 36) + 2 * kNaClAlignment +
	  6 * (depth + kNaClAlignment);
#endif

	code = buf = mono_global_codeman_reserve (tramp_size);

	rgctx_null_jumps = g_malloc (sizeof (guint8*) * (depth + 2));

	/* load vtable/mrgctx ptr */
	x86_mov_reg_membase (code, X86_EAX, X86_ESP, 4, 4);
	if (!mrgctx) {
		/* load rgctx ptr from vtable */
		x86_mov_reg_membase (code, X86_EAX, X86_EAX, G_STRUCT_OFFSET (MonoVTable, runtime_generic_context), 4);
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

	nacl_global_codeman_validate (&buf, tramp_size, &code);
	mono_arch_flush_icache (buf, code - buf);

	g_assert (code - buf <= tramp_size);

	if (info)
		*info = mono_tramp_info_create (mono_get_rgctx_fetch_trampoline_name (slot), buf, code - buf, ji, unwind_ops);

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

	nacl_global_codeman_validate (&buf, tramp_size, &code);
	mono_arch_flush_icache (buf, code - buf);

	g_assert (code - buf <= tramp_size);

	if (info)
		*info = mono_tramp_info_create ("rgctx_fetch_trampoline_general", buf, code - buf, ji, unwind_ops);

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

	unwind_ops = mono_arch_get_cie_program ();

	if (byte_offset < 0)
		mono_marshal_find_bitfield_offset (MonoVTable, initialized, &byte_offset, &bitmask);

	x86_test_membase_imm (code, MONO_ARCH_VTABLE_REG, byte_offset, bitmask);
	jump = code;
	x86_branch8 (code, X86_CC_Z, -1, 1);

	x86_ret (code);

	x86_patch (jump, code);

	/* Push the vtable so the stack is the same as in a specific trampoline */
	x86_push_reg (code, MONO_ARCH_VTABLE_REG);

	if (aot) {
		code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, "generic_trampoline_generic_class_init");
		x86_jump_reg (code, X86_EAX);
	} else {
		tramp = mono_get_trampoline_code (MONO_TRAMPOLINE_GENERIC_CLASS_INIT);

		/* jump to the actual trampoline */
		x86_jump_code (code, tramp);
	}

	mono_arch_flush_icache (code, code - buf);

	g_assert (code - buf <= tramp_size);
#ifdef __native_client_codegen__
	g_assert (code - buf <= kNaClAlignment);
#endif

	nacl_global_codeman_validate (&buf, tramp_size, &code);

	if (info)
		*info = mono_tramp_info_create (g_strdup_printf ("generic_class_init_trampoline"), buf, code - buf, ji, unwind_ops);

	return buf;
}

#ifdef MONO_ARCH_MONITOR_OBJECT_REG
/*
 * The code produced by this trampoline is equivalent to this:
 *
 * if (obj) {
 * 	if (obj->synchronisation) {
 * 		if (obj->synchronisation->owner == 0) {
 * 			if (cmpxch (&obj->synchronisation->owner, TID, 0) == 0)
 * 				return;
 * 		}
 * 		if (obj->synchronisation->owner == TID) {
 * 			++obj->synchronisation->nest;
 * 			return;
 * 		}
 * 	}
 * }
 * return full_monitor_enter ();
 *
 */
gpointer
mono_arch_create_monitor_enter_trampoline (MonoTrampInfo **info, gboolean aot)
{
	guint8 *tramp = mono_get_trampoline_code (MONO_TRAMPOLINE_MONITOR_ENTER);
	guint8 *code, *buf;
	guint8 *jump_obj_null, *jump_sync_null, *jump_other_owner, *jump_cmpxchg_failed, *jump_tid, *jump_sync_thin_hash = NULL;
	int tramp_size;
	int owner_offset, nest_offset, dummy;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;

	g_assert (MONO_ARCH_MONITOR_OBJECT_REG == X86_EAX);

	mono_monitor_threads_sync_members_offset (&owner_offset, &nest_offset, &dummy);
	g_assert (MONO_THREADS_SYNC_MEMBER_SIZE (owner_offset) == sizeof (gpointer));
	g_assert (MONO_THREADS_SYNC_MEMBER_SIZE (nest_offset) == sizeof (guint32));
	owner_offset = MONO_THREADS_SYNC_MEMBER_OFFSET (owner_offset);
	nest_offset = MONO_THREADS_SYNC_MEMBER_OFFSET (nest_offset);

	tramp_size = NACL_SIZE (96, 128);

	code = buf = mono_global_codeman_reserve (tramp_size);

	if (mono_thread_get_tls_offset () != -1) {
		/* MonoObject* obj is in EAX */
		/* is obj null? */
		x86_test_reg_reg (code, X86_EAX, X86_EAX);
		/* if yes, jump to actual trampoline */
		jump_obj_null = code;
		x86_branch8 (code, X86_CC_Z, -1, 1);

		/* load obj->synchronization to ECX */
		x86_mov_reg_membase (code, X86_ECX, X86_EAX, G_STRUCT_OFFSET (MonoObject, synchronisation), 4);

		if (mono_gc_is_moving ()) {
			/*if bit zero is set it's a thin hash*/
			/*FIXME use testb encoding*/
			x86_test_reg_imm (code, X86_ECX, 0x01);
			jump_sync_thin_hash = code;
			x86_branch8 (code, X86_CC_NE, -1, 1);

			/*clear bits used by the gc*/
			x86_alu_reg_imm (code, X86_AND, X86_ECX, ~0x3);
		}

		/* is synchronization null? */
		x86_test_reg_reg (code, X86_ECX, X86_ECX);

		/* if yes, jump to actual trampoline */
		jump_sync_null = code;
		x86_branch8 (code, X86_CC_Z, -1, 1);

		/* load MonoInternalThread* into EDX */
		code = mono_x86_emit_tls_get (code, X86_EDX, mono_thread_get_tls_offset ());
		/* load TID into EDX */
		x86_mov_reg_membase (code, X86_EDX, X86_EDX, G_STRUCT_OFFSET (MonoInternalThread, tid), 4);

		/* is synchronization->owner null? */
		x86_alu_membase_imm (code, X86_CMP, X86_ECX, owner_offset, 0);
		/* if not, jump to next case */
		jump_tid = code;
		x86_branch8 (code, X86_CC_NZ, -1, 1);

		/* if yes, try a compare-exchange with the TID */
		/* free up register EAX, needed for the zero */
		x86_push_reg (code, X86_EAX);
		/* zero EAX */
		x86_alu_reg_reg (code, X86_XOR, X86_EAX, X86_EAX);
		/* compare and exchange */
		x86_prefix (code, X86_LOCK_PREFIX);
		x86_cmpxchg_membase_reg (code, X86_ECX, owner_offset, X86_EDX);
		/* if not successful, jump to actual trampoline */
		jump_cmpxchg_failed = code;
		x86_branch8 (code, X86_CC_NZ, -1, 1);
		/* if successful, pop and return */
		x86_pop_reg (code, X86_EAX);
		x86_ret (code);

		/* next case: synchronization->owner is not null */
		x86_patch (jump_tid, code);
		/* is synchronization->owner == TID? */
		x86_alu_membase_reg (code, X86_CMP, X86_ECX, owner_offset, X86_EDX);
		/* if not, jump to actual trampoline */
		jump_other_owner = code;
		x86_branch8 (code, X86_CC_NZ, -1, 1);
		/* if yes, increment nest */
		x86_inc_membase (code, X86_ECX, nest_offset);
		/* return */
		x86_ret (code);

		/* push obj */
		x86_patch (jump_obj_null, code);
		if (jump_sync_thin_hash)
			x86_patch (jump_sync_thin_hash, code);
		x86_patch (jump_sync_null, code);
		x86_patch (jump_other_owner, code);
		x86_push_reg (code, X86_EAX);
		/* jump to the actual trampoline */
		x86_patch (jump_cmpxchg_failed, code);
		if (aot) {
			/* We are calling the generic trampoline directly, the argument is pushed
			 * on the stack just like a specific trampoline.
			 */
			code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, "generic_trampoline_monitor_enter");
			x86_jump_reg (code, X86_EAX);
		} else {
			x86_jump_code (code, tramp);
		}
	} else {
		/* push obj and jump to the actual trampoline */
		x86_push_reg (code, X86_EAX);
		if (aot) {
			code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, "generic_trampoline_monitor_enter");
			x86_jump_reg (code, X86_EAX);
		} else {
			x86_jump_code (code, tramp);
		}
	}

	mono_arch_flush_icache (buf, code - buf);
	g_assert (code - buf <= tramp_size);

	nacl_global_codeman_validate (&buf, tramp_size, &code);

	if (info)
		*info = mono_tramp_info_create (g_strdup_printf ("monitor_enter_trampoline"), buf, code - buf, ji, unwind_ops);

	return buf;
}

gpointer
mono_arch_create_monitor_exit_trampoline (MonoTrampInfo **info, gboolean aot)
{
	guint8 *tramp = mono_get_trampoline_code (MONO_TRAMPOLINE_MONITOR_EXIT);
	guint8 *code, *buf;
	guint8 *jump_obj_null, *jump_have_waiters, *jump_sync_null, *jump_not_owned, *jump_sync_thin_hash = NULL;
	guint8 *jump_next;
	int tramp_size;
	int owner_offset, nest_offset, entry_count_offset;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;

	g_assert (MONO_ARCH_MONITOR_OBJECT_REG == X86_EAX);

	mono_monitor_threads_sync_members_offset (&owner_offset, &nest_offset, &entry_count_offset);
	g_assert (MONO_THREADS_SYNC_MEMBER_SIZE (owner_offset) == sizeof (gpointer));
	g_assert (MONO_THREADS_SYNC_MEMBER_SIZE (nest_offset) == sizeof (guint32));
	g_assert (MONO_THREADS_SYNC_MEMBER_SIZE (entry_count_offset) == sizeof (gint32));
	owner_offset = MONO_THREADS_SYNC_MEMBER_OFFSET (owner_offset);
	nest_offset = MONO_THREADS_SYNC_MEMBER_OFFSET (nest_offset);
	entry_count_offset = MONO_THREADS_SYNC_MEMBER_OFFSET (entry_count_offset);

	tramp_size = NACL_SIZE (96, 128);

	code = buf = mono_global_codeman_reserve (tramp_size);

	if (mono_thread_get_tls_offset () != -1) {
		/* MonoObject* obj is in EAX */
		/* is obj null? */
		x86_test_reg_reg (code, X86_EAX, X86_EAX);
		/* if yes, jump to actual trampoline */
		jump_obj_null = code;
		x86_branch8 (code, X86_CC_Z, -1, 1);

		/* load obj->synchronization to ECX */
		x86_mov_reg_membase (code, X86_ECX, X86_EAX, G_STRUCT_OFFSET (MonoObject, synchronisation), 4);

		if (mono_gc_is_moving ()) {
			/*if bit zero is set it's a thin hash*/
			/*FIXME use testb encoding*/
			x86_test_reg_imm (code, X86_ECX, 0x01);
			jump_sync_thin_hash = code;
			x86_branch8 (code, X86_CC_NE, -1, 1);

			/*clear bits used by the gc*/
			x86_alu_reg_imm (code, X86_AND, X86_ECX, ~0x3);
		}

		/* is synchronization null? */
		x86_test_reg_reg (code, X86_ECX, X86_ECX);
		/* if yes, jump to actual trampoline */
		jump_sync_null = code;
		x86_branch8 (code, X86_CC_Z, -1, 1);

		/* next case: synchronization is not null */
		/* load MonoInternalThread* into EDX */
		code = mono_x86_emit_tls_get (code, X86_EDX, mono_thread_get_tls_offset ());
		/* load TID into EDX */
		x86_mov_reg_membase (code, X86_EDX, X86_EDX, G_STRUCT_OFFSET (MonoInternalThread, tid), 4);
		/* is synchronization->owner == TID */
		x86_alu_membase_reg (code, X86_CMP, X86_ECX, owner_offset, X86_EDX);
		/* if no, jump to actual trampoline */
		jump_not_owned = code;
		x86_branch8 (code, X86_CC_NZ, -1, 1);

		/* next case: synchronization->owner == TID */
		/* is synchronization->nest == 1 */
		x86_alu_membase_imm (code, X86_CMP, X86_ECX, nest_offset, 1);
		/* if not, jump to next case */
		jump_next = code;
		x86_branch8 (code, X86_CC_NZ, -1, 1);
		/* if yes, is synchronization->entry_count zero? */
		x86_alu_membase_imm (code, X86_CMP, X86_ECX, entry_count_offset, 0);
		/* if not, jump to actual trampoline */
		jump_have_waiters = code;
		x86_branch8 (code, X86_CC_NZ, -1 , 1);
		/* if yes, set synchronization->owner to null and return */
		x86_mov_membase_imm (code, X86_ECX, owner_offset, 0, 4);
		x86_ret (code);

		/* next case: synchronization->nest is not 1 */
		x86_patch (jump_next, code);
		/* decrease synchronization->nest and return */
		x86_dec_membase (code, X86_ECX, nest_offset);
		x86_ret (code);

		/* push obj and jump to the actual trampoline */
		x86_patch (jump_obj_null, code);
		if (jump_sync_thin_hash)
			x86_patch (jump_sync_thin_hash, code);
		x86_patch (jump_have_waiters, code);
		x86_patch (jump_not_owned, code);
		x86_patch (jump_sync_null, code);
	}

	/* push obj and jump to the actual trampoline */
	x86_push_reg (code, X86_EAX);
	if (aot) {
		code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, "generic_trampoline_monitor_exit");
		x86_jump_reg (code, X86_EAX);
	} else {
		x86_jump_code (code, tramp);
	}

	nacl_global_codeman_validate (&buf, tramp_size, &code);

	mono_arch_flush_icache (buf, code - buf);
	g_assert (code - buf <= tramp_size);

	if (info)
		*info = mono_tramp_info_create (g_strdup_printf ("monitor_exit_trampoline"), buf, code - buf, ji, unwind_ops);

	return buf;
}

#else

gpointer
mono_arch_create_monitor_enter_trampoline (MonoTrampInfo **info, gboolean aot)
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

	x86_push_imm (code, func_arg);
	x86_call_code (code, (guint8*)func);
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
		code = mono_x86_emit_tls_get (code, X86_EAX, mono_get_jit_tls_offset ());
		x86_mov_reg_membase (code, X86_EAX, X86_EAX, G_STRUCT_OFFSET (MonoJitTlsData, handler_block_return_address), 4);
		/*simulate a call*/
		/*Fix stack alignment*/
		x86_alu_reg_imm (code, X86_SUB, X86_ESP, 0x8);
		x86_push_reg (code, X86_EAX);
		x86_jump_code (code, tramp);
	} else {
		/*Slow path uses a c helper*/
		x86_alu_reg_imm (code, X86_SUB, X86_ESP, 0x8);
		x86_push_reg (code, X86_ESP);
		x86_push_imm (code, tramp);
		x86_jump_code (code, handler_block_trampoline_helper);
	}

	nacl_global_codeman_validate (&buf, tramp_size, &code);

	mono_arch_flush_icache (buf, code - buf);
	g_assert (code - buf <= tramp_size);

	if (mono_jit_map_is_enabled ())
		mono_emit_jit_tramp (buf, code - buf, "handler_block_trampoline");

	return buf;
}

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

guint32
mono_arch_get_plt_info_offset (guint8 *plt_entry, mgreg_t *regs, guint8 *code)
{
	return *(guint32*)(plt_entry + NACL_SIZE (6, 12));
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

	buf_len = 10;

	start = code = mono_domain_code_reserve (domain, buf_len);

	x86_mov_reg_imm (code, X86_EAX, arg);
	x86_jump_code (code, addr);
	g_assert ((code - start) <= buf_len);

	nacl_domain_code_validate (domain, &start, buf_len, &code);
	mono_arch_flush_icache (start, code - start);

	return start;
}

#if defined(MONOTOUCH) || defined(MONO_EXTENSIONS)

#include "../../../mono-extensions/mono/mini/tramp-x86-gsharedvt.c"

#else

gpointer
mono_arch_get_gsharedvt_trampoline (MonoTrampInfo **info, gboolean aot)
{
	if (info)
		*info = NULL;
	return NULL;
}

#endif /* !MONOTOUCH */
