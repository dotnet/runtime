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
#include <mono/arch/x86/x86-codegen.h>

#ifdef HAVE_VALGRIND_MEMCHECK_H
#include <valgrind/memcheck.h>
#endif

#include "mini.h"
#include "mini-x86.h"

static guint8* nullified_class_init_trampoline;

/*
 * mono_arch_get_unbox_trampoline:
 * @gsctx: the generic sharing context
 * @m: method pointer
 * @addr: pointer to native code for @m
 *
 * when value type methods are called through the vtable we need to unbox the
 * this argument. This method returns a pointer to a trampoline which does
 * unboxing before calling the method
 */
gpointer
mono_arch_get_unbox_trampoline (MonoGenericSharingContext *gsctx, MonoMethod *m, gpointer addr)
{
	guint8 *code, *start;
	int this_pos = 4;
	MonoDomain *domain = mono_domain_get ();

	if (MONO_TYPE_ISSTRUCT (mono_method_signature (m)->ret))
		this_pos = 8;
	    
	start = code = mono_domain_code_reserve (domain, 16);

	x86_alu_membase_imm (code, X86_ADD, X86_ESP, this_pos, sizeof (MonoObject));
	x86_jump_code (code, addr);
	g_assert ((code - start) < 16);

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

	mono_arch_flush_icache (start, code - start);

	return start;
}

void
mono_arch_patch_callsite (guint8 *method_start, guint8 *orig_code, guint8 *addr)
{
	guint8 *code;
	guint8 buf [8];
	gboolean can_write = mono_breakpoint_clean_code (method_start, orig_code, 8, buf, sizeof (buf));

	code = buf + 8;
	if (mono_running_on_valgrind ())
		can_write = FALSE;

	/* go to the start of the call instruction
	 *
	 * address_byte = (m << 6) | (o << 3) | reg
	 * call opcode: 0xff address_byte displacement
	 * 0xff m=1,o=2 imm8
	 * 0xff m=2,o=2 imm32
	 */
	code -= 6;
	orig_code -= 6;
	if ((code [1] == 0xe8)) {
		if (can_write) {
			InterlockedExchange ((gint32*)(orig_code + 2), (guint)addr - ((guint)orig_code + 1) - 5);

#ifdef HAVE_VALGRIND_MEMCHECK_H
				/* Tell valgrind to recompile the patched code */
				//VALGRIND_DISCARD_TRANSLATIONS (code + 2, code + 6);
#endif
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
}

void
mono_arch_patch_plt_entry (guint8 *code, gpointer *got, mgreg_t *regs, guint8 *addr)
{
	/* A PLT entry: jmp <DISP> */
	g_assert (code [0] == 0xe9);

	if (!mono_running_on_valgrind ())
		InterlockedExchange ((gint32*)(code + 1), (guint)addr - (guint)code - 5);
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
#ifdef HAVE_VALGRIND_MEMCHECK_H
			/* FIXME: the calltree skin trips on the self modifying code above */

			/* Tell valgrind to recompile the patched code */
			//VALGRIND_DISCARD_TRANSLATIONS (code, code + 8);
#endif
		}
	} else if (code [0] == 0x90 || code [0] == 0xeb) {
		/* Already changed by another thread */
		;
	} else if ((code [-1] == 0xff) && (x86_modrm_reg (code [0]) == 0x2)) {
		/* call *<OFFSET>(<REG>) -> Call made from AOT code */
		gpointer *vtable_slot;

		vtable_slot = mono_get_vcall_slot_addr (code + 5, (gpointer*)regs);
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
	if (!mono_running_on_valgrind ()) {
		guint32 ops;

		ops = 0xfeeb;
		InterlockedExchange ((gint32*)code, ops);

		/* Then change the other bytes to a nop */
		code [2] = 0x90;
		code [3] = 0x90;
		code [4] = 0x90;

		/* Change the first byte to a nop */
		ops = 0xc3;
		InterlockedExchange ((gint32*)code, ops);
	}
}

guchar*
mono_arch_create_trampoline_code (MonoTrampolineType tramp_type)
{
	guint8 *buf, *code, *tramp;
	int pushed_args, pushed_args_caller_saved;

	code = buf = mono_global_codeman_reserve (256);

	/* Note that there is a single argument to the trampoline
	 * and it is stored at: esp + pushed_args * sizeof (gpointer)
	 * the ret address is at: esp + (pushed_args + 1) * sizeof (gpointer)
	 */

	/* Put all registers into an array on the stack
	 * If this code is changed, make sure to update the offset value in
	 * mono_arch_find_this_argument () in mini-x86.c.
	 */
	x86_push_reg (buf, X86_EDI);
	x86_push_reg (buf, X86_ESI);
	x86_push_reg (buf, X86_EBP);
	x86_push_reg (buf, X86_ESP);
	x86_push_reg (buf, X86_EBX);
	x86_push_reg (buf, X86_EDX);
	x86_push_reg (buf, X86_ECX);
	x86_push_reg (buf, X86_EAX);

	pushed_args_caller_saved = pushed_args = 8;

	/* Align stack on apple */
	x86_alu_reg_imm (buf, X86_SUB, X86_ESP, 4);

	pushed_args ++;

	/* save LMF begin */

	/* save the IP (caller ip) */
	if (tramp_type == MONO_TRAMPOLINE_JUMP)
		x86_push_imm (buf, 0);
	else
		x86_push_membase (buf, X86_ESP, (pushed_args + 1) * sizeof (gpointer));

	pushed_args++;

	x86_push_reg (buf, X86_EBP);
	x86_push_reg (buf, X86_ESI);
	x86_push_reg (buf, X86_EDI);
	x86_push_reg (buf, X86_EBX);

	pushed_args += 4;

	/* save ESP */
	x86_push_reg (buf, X86_ESP);
	/* Adjust ESP so it points to the previous frame */
	x86_alu_membase_imm (buf, X86_ADD, X86_ESP, 0, (pushed_args + 2) * 4);

	pushed_args ++;

	/* save method info */
	if ((tramp_type == MONO_TRAMPOLINE_JIT) || (tramp_type == MONO_TRAMPOLINE_JUMP))
		x86_push_membase (buf, X86_ESP, pushed_args * sizeof (gpointer));
	else
		x86_push_imm (buf, 0);

	pushed_args++;

	/* On apple, the stack is correctly aligned to 16 bytes because pushed_args is
	 * 16 and there is the extra trampoline arg + the return ip pushed by call
	 * FIXME: Note that if an exception happens while some args are pushed
	 * on the stack, the stack will be misaligned.
	 */
	g_assert (pushed_args == 16);

	/* get the address of lmf for the current thread */
	x86_call_code (buf, mono_get_lmf_addr);
	/* push lmf */
	x86_push_reg (buf, X86_EAX); 
	/* push *lfm (previous_lmf) */
	x86_push_membase (buf, X86_EAX, 0);
	/* Signal to mono_arch_find_jit_info () that this is a trampoline frame */
	x86_alu_membase_imm (buf, X86_ADD, X86_ESP, 0, 1);
	/* *(lmf) = ESP */
	x86_mov_membase_reg (buf, X86_EAX, 0, X86_ESP, 4);
	/* save LFM end */

	pushed_args += 2;

	/* starting the call sequence */

	/* FIXME: Push the trampoline address */
	x86_push_imm (buf, 0);

	pushed_args++;

	/* push the method info */
	x86_push_membase (buf, X86_ESP, pushed_args * sizeof (gpointer));

	pushed_args++;

	/* push the return address onto the stack */
	if (tramp_type == MONO_TRAMPOLINE_JUMP)
		x86_push_imm (buf, 0);
	else
		x86_push_membase (buf, X86_ESP, (pushed_args + 1) * sizeof (gpointer));
	pushed_args++;
	/* push the address of the register array */
	x86_lea_membase (buf, X86_EAX, X86_ESP, (pushed_args - 8) * sizeof (gpointer));
	x86_push_reg (buf, X86_EAX);

	pushed_args++;

#ifdef __APPLE__
	/* check the stack is aligned after the ret ip is pushed */
	/*x86_mov_reg_reg (buf, X86_EDX, X86_ESP, 4);
	x86_alu_reg_imm (buf, X86_AND, X86_EDX, 15);
	x86_alu_reg_imm (buf, X86_CMP, X86_EDX, 0);
	x86_branch_disp (buf, X86_CC_Z, 3, FALSE);
	x86_breakpoint (buf);*/
#endif

	tramp = (guint8*)mono_get_trampoline_func (tramp_type);
	x86_call_code (buf, tramp);

	x86_alu_reg_imm (buf, X86_ADD, X86_ESP, 4*4);

	pushed_args -= 4;

	/* Check for thread interruption */
	/* This is not perf critical code so no need to check the interrupt flag */
	x86_push_reg (buf, X86_EAX);
	x86_call_code (buf, (guint8*)mono_thread_force_interruption_checkpoint);
	x86_pop_reg (buf, X86_EAX);

	/* Restore LMF */

	/* ebx = previous_lmf */
	x86_pop_reg (buf, X86_EBX);
	pushed_args--;
	x86_alu_reg_imm (buf, X86_SUB, X86_EBX, 1);

	/* edi = lmf */
	x86_pop_reg (buf, X86_EDI);
	pushed_args--;

	/* *(lmf) = previous_lmf */
	x86_mov_membase_reg (buf, X86_EDI, 0, X86_EBX, 4);

	/* discard method info */
	x86_pop_reg (buf, X86_ESI);
	pushed_args--;

	/* discard ESP */
	x86_pop_reg (buf, X86_ESI);
	pushed_args--;

	/* restore caller saved regs */
	x86_pop_reg (buf, X86_EBX);
	x86_pop_reg (buf, X86_EDI);
	x86_pop_reg (buf, X86_ESI);
	x86_pop_reg (buf, X86_EBP);

	pushed_args -= 4;

	/* discard save IP */
	x86_alu_reg_imm (buf, X86_ADD, X86_ESP, 4);
	pushed_args--;

	/* restore LMF end */

	if (!MONO_TRAMPOLINE_TYPE_MUST_RETURN (tramp_type)) {
		/* 
		 * Overwrite the method ptr with the address we need to jump to,
		 * to free %eax.
		 */
		x86_mov_membase_reg (buf, X86_ESP, pushed_args * sizeof (gpointer), X86_EAX, 4);
	}

	/* Restore caller saved registers */
	x86_mov_reg_membase (buf, X86_ECX, X86_ESP, (pushed_args - pushed_args_caller_saved + X86_ECX) * 4, 4);
	x86_mov_reg_membase (buf, X86_EDX, X86_ESP, (pushed_args - pushed_args_caller_saved + X86_EDX) * 4, 4);
	if ((tramp_type == MONO_TRAMPOLINE_RESTORE_STACK_PROT) || (tramp_type == MONO_TRAMPOLINE_AOT_PLT))
		x86_mov_reg_membase (buf, X86_EAX, X86_ESP, (pushed_args - pushed_args_caller_saved + X86_EAX) * 4, 4);

	if (!MONO_TRAMPOLINE_TYPE_MUST_RETURN (tramp_type)) {
		/* Pop saved reg array + stack align */
		x86_alu_reg_imm (buf, X86_ADD, X86_ESP, 9 * 4);
		pushed_args -= 9;
		g_assert (pushed_args == 0);
	} else {
		/* Pop saved reg array + stack align + method ptr */
		x86_alu_reg_imm (buf, X86_ADD, X86_ESP, 10 * 4);
		pushed_args -= 10;

		/* We've popped one more stack item than we've pushed (the
		   method ptr argument), so we must end up at -1. */
		g_assert (pushed_args == -1);
	}

	x86_ret (buf);

	g_assert ((buf - code) <= 256);

	if (tramp_type == MONO_TRAMPOLINE_CLASS_INIT) {
		/* Initialize the nullified class init trampoline used in the AOT case */
		nullified_class_init_trampoline = buf = mono_global_codeman_reserve (16);
		x86_ret (buf);
	}

	return code;
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

	if (code_len)
		*code_len = buf - code;

	return code;
}

gpointer
mono_arch_create_rgctx_lazy_fetch_trampoline (guint32 slot)
{
	guint8 *tramp;
	guint8 *code, *buf;
	guint8 **rgctx_null_jumps;
	int tramp_size;
	int depth, index;
	int i;
	gboolean mrgctx;

	mrgctx = MONO_RGCTX_SLOT_IS_MRGCTX (slot);
	index = MONO_RGCTX_SLOT_INDEX (slot);
	if (mrgctx)
		index += sizeof (MonoMethodRuntimeGenericContext) / sizeof (gpointer);
	for (depth = 0; ; ++depth) {
		int size = mono_class_rgctx_get_array_size (depth, mrgctx);

		if (index < size - 1)
			break;
		index -= size - 1;
	}

	tramp_size = 36 + 6 * depth;

	code = buf = mono_global_codeman_reserve (tramp_size);

	rgctx_null_jumps = g_malloc (sizeof (guint8*) * (depth + 2));

	/* load vtable/mrgctx ptr */
	x86_mov_reg_membase (buf, X86_EAX, X86_ESP, 4, 4);
	if (!mrgctx) {
		/* load rgctx ptr from vtable */
		x86_mov_reg_membase (buf, X86_EAX, X86_EAX, G_STRUCT_OFFSET (MonoVTable, runtime_generic_context), 4);
		/* is the rgctx ptr null? */
		x86_test_reg_reg (buf, X86_EAX, X86_EAX);
		/* if yes, jump to actual trampoline */
		rgctx_null_jumps [0] = buf;
		x86_branch8 (buf, X86_CC_Z, -1, 1);
	}

	for (i = 0; i < depth; ++i) {
		/* load ptr to next array */
		if (mrgctx && i == 0)
			x86_mov_reg_membase (buf, X86_EAX, X86_EAX, sizeof (MonoMethodRuntimeGenericContext), 4);
		else
			x86_mov_reg_membase (buf, X86_EAX, X86_EAX, 0, 4);
		/* is the ptr null? */
		x86_test_reg_reg (buf, X86_EAX, X86_EAX);
		/* if yes, jump to actual trampoline */
		rgctx_null_jumps [i + 1] = buf;
		x86_branch8 (buf, X86_CC_Z, -1, 1);
	}

	/* fetch slot */
	x86_mov_reg_membase (buf, X86_EAX, X86_EAX, sizeof (gpointer) * (index + 1), 4);
	/* is the slot null? */
	x86_test_reg_reg (buf, X86_EAX, X86_EAX);
	/* if yes, jump to actual trampoline */
	rgctx_null_jumps [depth + 1] = buf;
	x86_branch8 (buf, X86_CC_Z, -1, 1);
	/* otherwise return */
	x86_ret (buf);

	for (i = mrgctx ? 1 : 0; i <= depth + 1; ++i)
		x86_patch (rgctx_null_jumps [i], buf);

	g_free (rgctx_null_jumps);

	x86_mov_reg_membase (buf, MONO_ARCH_VTABLE_REG, X86_ESP, 4, 4);

	tramp = mono_arch_create_specific_trampoline (GUINT_TO_POINTER (slot), MONO_TRAMPOLINE_RGCTX_LAZY_FETCH, mono_get_root_domain (), NULL);

	/* jump to the actual trampoline */
	x86_jump_code (buf, tramp);

	mono_arch_flush_icache (code, buf - code);

	g_assert (buf - code <= tramp_size);

	return code;
}

gpointer
mono_arch_create_generic_class_init_trampoline (void)
{
	guint8 *tramp;
	guint8 *code, *buf;
	static int byte_offset = -1;
	static guint8 bitmask;
	guint8 *jump;
	int tramp_size;

	tramp_size = 64;

	code = buf = mono_global_codeman_reserve (tramp_size);

	if (byte_offset < 0)
		mono_marshal_find_bitfield_offset (MonoVTable, initialized, &byte_offset, &bitmask);

	x86_test_membase_imm (code, MONO_ARCH_VTABLE_REG, byte_offset, bitmask);
	jump = code;
	x86_branch8 (code, X86_CC_Z, -1, 1);

	x86_ret (code);

	x86_patch (jump, code);

	/* Push the vtable so the stack is the same as in a specific trampoline */
	x86_push_reg (code, MONO_ARCH_VTABLE_REG);

	tramp = mono_get_trampoline_code (MONO_TRAMPOLINE_GENERIC_CLASS_INIT);

	/* jump to the actual trampoline */
	x86_jump_code (code, tramp);

	mono_arch_flush_icache (code, code - buf);

	g_assert (code - buf <= tramp_size);

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
mono_arch_create_monitor_enter_trampoline (void)
{
	guint32 code_size;
	MonoJumpInfo *ji;

	return mono_arch_create_monitor_enter_trampoline_full (&code_size, &ji, FALSE);
}

gpointer
mono_arch_create_monitor_enter_trampoline_full (guint32 *code_size, MonoJumpInfo **ji, gboolean aot)
{
	guint8 *tramp = mono_get_trampoline_code (MONO_TRAMPOLINE_MONITOR_ENTER);
	guint8 *code, *buf;
	guint8 *jump_obj_null, *jump_sync_null, *jump_other_owner, *jump_cmpxchg_failed, *jump_tid;
	int tramp_size;
	int owner_offset, nest_offset, dummy;

	g_assert (MONO_ARCH_MONITOR_OBJECT_REG == X86_EAX);

	mono_monitor_threads_sync_members_offset (&owner_offset, &nest_offset, &dummy);
	g_assert (MONO_THREADS_SYNC_MEMBER_SIZE (owner_offset) == sizeof (gpointer));
	g_assert (MONO_THREADS_SYNC_MEMBER_SIZE (nest_offset) == sizeof (guint32));
	owner_offset = MONO_THREADS_SYNC_MEMBER_OFFSET (owner_offset);
	nest_offset = MONO_THREADS_SYNC_MEMBER_OFFSET (nest_offset);

	tramp_size = 64;

	code = buf = mono_global_codeman_reserve (tramp_size);

	if (mono_thread_get_tls_offset () != -1) {
		/* MonoObject* obj is in EAX */
		/* is obj null? */
		x86_test_reg_reg (buf, X86_EAX, X86_EAX);
		/* if yes, jump to actual trampoline */
		jump_obj_null = buf;
		x86_branch8 (buf, X86_CC_Z, -1, 1);

		/* load obj->synchronization to ECX */
		x86_mov_reg_membase (buf, X86_ECX, X86_EAX, G_STRUCT_OFFSET (MonoObject, synchronisation), 4);
		/* is synchronization null? */
		x86_test_reg_reg (buf, X86_ECX, X86_ECX);
		/* if yes, jump to actual trampoline */
		jump_sync_null = buf;
		x86_branch8 (buf, X86_CC_Z, -1, 1);

		/* load MonoThread* into EDX */
		buf = mono_x86_emit_tls_get (buf, X86_EDX, mono_thread_get_tls_offset ());
		/* load TID into EDX */
		x86_mov_reg_membase (buf, X86_EDX, X86_EDX, G_STRUCT_OFFSET (MonoThread, tid), 4);

		/* is synchronization->owner null? */
		x86_alu_membase_imm (buf, X86_CMP, X86_ECX, owner_offset, 0);
		/* if not, jump to next case */
		jump_tid = buf;
		x86_branch8 (buf, X86_CC_NZ, -1, 1);

		/* if yes, try a compare-exchange with the TID */
		/* free up register EAX, needed for the zero */
		x86_push_reg (buf, X86_EAX);
		/* zero EAX */
		x86_alu_reg_reg (buf, X86_XOR, X86_EAX, X86_EAX);
		/* compare and exchange */
		x86_prefix (buf, X86_LOCK_PREFIX);
		x86_cmpxchg_membase_reg (buf, X86_ECX, owner_offset, X86_EDX);
		/* if not successful, jump to actual trampoline */
		jump_cmpxchg_failed = buf;
		x86_branch8 (buf, X86_CC_NZ, -1, 1);
		/* if successful, pop and return */
		x86_pop_reg (buf, X86_EAX);
		x86_ret (buf);

		/* next case: synchronization->owner is not null */
		x86_patch (jump_tid, buf);
		/* is synchronization->owner == TID? */
		x86_alu_membase_reg (buf, X86_CMP, X86_ECX, owner_offset, X86_EDX);
		/* if not, jump to actual trampoline */
		jump_other_owner = buf;
		x86_branch8 (buf, X86_CC_NZ, -1, 1);
		/* if yes, increment nest */
		x86_inc_membase (buf, X86_ECX, nest_offset);
		/* return */
		x86_ret (buf);

		/* push obj */
		x86_patch (jump_obj_null, buf);
		x86_patch (jump_sync_null, buf);
		x86_patch (jump_other_owner, buf);
		x86_push_reg (buf, X86_EAX);
		/* jump to the actual trampoline */
		x86_patch (jump_cmpxchg_failed, buf);
		x86_jump_code (buf, tramp);
	} else {
		/* push obj and jump to the actual trampoline */
		x86_push_reg (buf, X86_EAX);
		x86_jump_code (buf, tramp);
	}

	mono_arch_flush_icache (buf, buf - code);
	g_assert (buf - code <= tramp_size);

	return code;
}

gpointer
mono_arch_create_monitor_exit_trampoline (void)
{
	guint32 code_size;
	MonoJumpInfo *ji;

	return mono_arch_create_monitor_exit_trampoline_full (&code_size, &ji, FALSE);
}

gpointer
mono_arch_create_monitor_exit_trampoline_full (guint32 *code_size, MonoJumpInfo **ji, gboolean aot)
{
	guint8 *tramp = mono_get_trampoline_code (MONO_TRAMPOLINE_MONITOR_EXIT);
	guint8 *code, *buf;
	guint8 *jump_obj_null, *jump_have_waiters;
	guint8 *jump_next;
	int tramp_size;
	int owner_offset, nest_offset, entry_count_offset;

	g_assert (MONO_ARCH_MONITOR_OBJECT_REG == X86_EAX);

	mono_monitor_threads_sync_members_offset (&owner_offset, &nest_offset, &entry_count_offset);
	g_assert (MONO_THREADS_SYNC_MEMBER_SIZE (owner_offset) == sizeof (gpointer));
	g_assert (MONO_THREADS_SYNC_MEMBER_SIZE (nest_offset) == sizeof (guint32));
	g_assert (MONO_THREADS_SYNC_MEMBER_SIZE (entry_count_offset) == sizeof (gint32));
	owner_offset = MONO_THREADS_SYNC_MEMBER_OFFSET (owner_offset);
	nest_offset = MONO_THREADS_SYNC_MEMBER_OFFSET (nest_offset);
	entry_count_offset = MONO_THREADS_SYNC_MEMBER_OFFSET (entry_count_offset);

	tramp_size = 64;

	code = buf = mono_global_codeman_reserve (tramp_size);

	if (mono_thread_get_tls_offset () != -1) {
		/* MonoObject* obj is in EAX */
		/* is obj null? */
		x86_test_reg_reg (buf, X86_EAX, X86_EAX);
		/* if yes, jump to actual trampoline */
		jump_obj_null = buf;
		x86_branch8 (buf, X86_CC_Z, -1, 1);

		/* load obj->synchronization to ECX */
		x86_mov_reg_membase (buf, X86_ECX, X86_EAX, G_STRUCT_OFFSET (MonoObject, synchronisation), 4);
		/* is synchronization null? */
		x86_test_reg_reg (buf, X86_ECX, X86_ECX);
		/* if not, jump to next case */
		jump_next = buf;
		x86_branch8 (buf, X86_CC_NZ, -1, 1);
		/* if yes, just return */
		x86_ret (buf);

		/* next case: synchronization is not null */
		x86_patch (jump_next, buf);
		/* load MonoThread* into EDX */
		buf = mono_x86_emit_tls_get (buf, X86_EDX, mono_thread_get_tls_offset ());
		/* load TID into EDX */
		x86_mov_reg_membase (buf, X86_EDX, X86_EDX, G_STRUCT_OFFSET (MonoThread, tid), 4);
		/* is synchronization->owner == TID */
		x86_alu_membase_reg (buf, X86_CMP, X86_ECX, owner_offset, X86_EDX);
		/* if yes, jump to next case */
		jump_next = buf;
		x86_branch8 (buf, X86_CC_Z, -1, 1);
		/* if not, just return */
		x86_ret (buf);

		/* next case: synchronization->owner == TID */
		x86_patch (jump_next, buf);
		/* is synchronization->nest == 1 */
		x86_alu_membase_imm (buf, X86_CMP, X86_ECX, nest_offset, 1);
		/* if not, jump to next case */
		jump_next = buf;
		x86_branch8 (buf, X86_CC_NZ, -1, 1);
		/* if yes, is synchronization->entry_count zero? */
		x86_alu_membase_imm (buf, X86_CMP, X86_ECX, entry_count_offset, 0);
		/* if not, jump to actual trampoline */
		jump_have_waiters = buf;
		x86_branch8 (buf, X86_CC_NZ, -1 , 1);
		/* if yes, set synchronization->owner to null and return */
		x86_mov_membase_imm (buf, X86_ECX, owner_offset, 0, 4);
		x86_ret (buf);

		/* next case: synchronization->nest is not 1 */
		x86_patch (jump_next, buf);
		/* decrease synchronization->nest and return */
		x86_dec_membase (buf, X86_ECX, nest_offset);
		x86_ret (buf);

		/* push obj and jump to the actual trampoline */
		x86_patch (jump_obj_null, buf);
		x86_patch (jump_have_waiters, buf);
		x86_push_reg (buf, X86_EAX);
		x86_jump_code (buf, tramp);
	} else {
		/* push obj and jump to the actual trampoline */
		x86_push_reg (buf, X86_EAX);
		x86_jump_code (buf, tramp);
	}

	mono_arch_flush_icache (buf, buf - code);
	g_assert (buf - code <= tramp_size);

	return code;
}
#else
gpointer
mono_arch_create_monitor_enter_trampoline (void)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_create_monitor_exit_trampoline (void)
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
