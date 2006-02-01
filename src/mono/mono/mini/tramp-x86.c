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
#include <mono/arch/x86/x86-codegen.h>

#ifdef HAVE_VALGRIND_MEMCHECK_H
#include <valgrind/memcheck.h>
#endif

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

	if (!mono_method_signature (m)->ret->byref && MONO_TYPE_ISSTRUCT (mono_method_signature (m)->ret))
		this_pos = 8;
	    
	mono_domain_lock (domain);
	start = code = mono_code_manager_reserve (domain->code_mp, 16);
	mono_domain_unlock (domain);

	x86_alu_membase_imm (code, X86_ADD, X86_ESP, this_pos, sizeof (MonoObject));
	x86_jump_code (code, addr);
	g_assert ((code - start) < 16);

	return start;
}

void
mono_arch_patch_callsite (guint8 *code, guint8 *addr)
{
	/* go to the start of the call instruction
	 *
	 * address_byte = (m << 6) | (o << 3) | reg
	 * call opcode: 0xff address_byte displacement
	 * 0xff m=1,o=2 imm8
	 * 0xff m=2,o=2 imm32
	 */
	code -= 6;
	if ((code [1] == 0xe8)) {
		if (!mono_running_on_valgrind ()) {
			InterlockedExchange ((gint32*)(code + 2), (guint)addr - ((guint)code + 1) - 5);

#ifdef HAVE_VALGRIND_MEMCHECK_H
				/* Tell valgrind to recompile the patched code */
				//VALGRIND_DISCARD_TRANSLATIONS (code + 2, code + 6);
#endif
		}
	} else {
		printf ("Invalid trampoline sequence: %x %x %x %x %x %x %x\n", code [0], code [1], code [2], code [3],
				code [4], code [5], code [6]);
		g_assert_not_reached ();
	}
}

void
mono_arch_nullify_class_init_trampoline (guint8 *code, gssize *regs)
{
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

		vtable_slot = mono_arch_get_vcall_slot_addr (code + 5, (gpointer*)regs);
		g_assert (vtable_slot);

		*vtable_slot = nullified_class_init_trampoline;
	} else {
			printf ("Invalid trampoline sequence: %x %x %x %x %x %x %x\n", code [0], code [1], code [2], code [3],
				code [4], code [5], code [6]);
			g_assert_not_reached ();
		}
}

void
mono_arch_patch_delegate_trampoline (guint8 *code, guint8 *tramp, gssize *regs, guint8 *addr)
{
	guint32 reg;
	guint32 disp;

	if ((code [-3] == 0xff) && (x86_modrm_reg (code [-2]) == 0x2) && (x86_modrm_mod (code [-2]) == 0x1)) {
		/* call *[reg+disp8] */
		reg = x86_modrm_rm (code [-2]);
		disp = *(guint8*)(code - 1);
		//printf ("B: [%%r%d+0x%x]\n", reg, disp);
	}
	else {
		int i;

		for (i = -16; i < 0; ++i)
			printf ("%d ", code [i]);
		printf ("\n");
		g_assert_not_reached ();
	}

	*(gpointer*)(((guint32)(regs [reg])) + disp) = addr;
}

guchar*
mono_arch_create_trampoline_code (MonoTrampolineType tramp_type)
{
	guint8 *buf, *code;

	code = buf = mono_global_codeman_reserve (256);

	/* Put all registers into an array on the stack */
	x86_push_reg (buf, X86_EDI);
	x86_push_reg (buf, X86_ESI);
	x86_push_reg (buf, X86_EBP);
	x86_push_reg (buf, X86_ESP);
	x86_push_reg (buf, X86_EBX);
	x86_push_reg (buf, X86_EDX);
	x86_push_reg (buf, X86_ECX);
	x86_push_reg (buf, X86_EAX);

	/* save LMF begin */

	/* save the IP (caller ip) */
	if (tramp_type == MONO_TRAMPOLINE_JUMP)
		x86_push_imm (buf, 0);
	else
		x86_push_membase (buf, X86_ESP, 8 * 4 + 4);

	x86_push_reg (buf, X86_EBP);
	x86_push_reg (buf, X86_ESI);
	x86_push_reg (buf, X86_EDI);
	x86_push_reg (buf, X86_EBX);

	/* save method info */
	x86_push_membase (buf, X86_ESP, 13 * 4);
	/* get the address of lmf for the current thread */
	x86_call_code (buf, mono_get_lmf_addr);
	/* push lmf */
	x86_push_reg (buf, X86_EAX); 
	/* push *lfm (previous_lmf) */
	x86_push_membase (buf, X86_EAX, 0);
	/* *(lmf) = ESP */
	x86_mov_membase_reg (buf, X86_EAX, 0, X86_ESP, 4);
	/* save LFM end */

	/* FIXME: Push the trampoline address */
	x86_push_imm (buf, 0);

	/* push the method info */
	x86_push_membase (buf, X86_ESP, 17 * 4);
	/* push the return address onto the stack */
	if (tramp_type == MONO_TRAMPOLINE_JUMP)
		x86_push_imm (buf, 0);
	else
		x86_push_membase (buf, X86_ESP, 18 * 4 + 4);
	/* push the address of the register array */
	x86_lea_membase (buf, X86_EAX, X86_ESP, 11 * 4);
	x86_push_reg (buf, X86_EAX);

	if (tramp_type == MONO_TRAMPOLINE_CLASS_INIT)
		x86_call_code (buf, mono_class_init_trampoline);
	else if (tramp_type == MONO_TRAMPOLINE_AOT)
		x86_call_code (buf, mono_aot_trampoline);
	else if (tramp_type == MONO_TRAMPOLINE_DELEGATE)
		x86_call_code (buf, mono_delegate_trampoline);
	else
		x86_call_code (buf, mono_magic_trampoline);
	x86_alu_reg_imm (buf, X86_ADD, X86_ESP, 4*4);

	/* restore LMF start */
	/* ebx = previous_lmf */
	x86_pop_reg (buf, X86_EBX);
	/* edi = lmf */
	x86_pop_reg (buf, X86_EDI);
	/* *(lmf) = previous_lmf */
	x86_mov_membase_reg (buf, X86_EDI, 0, X86_EBX, 4);
	/* discard method info */
	x86_pop_reg (buf, X86_ESI);
	/* restore caller saved regs */
	x86_pop_reg (buf, X86_EBX);
	x86_pop_reg (buf, X86_EDI);
	x86_pop_reg (buf, X86_ESI);
	x86_pop_reg (buf, X86_EBP);

	/* discard save IP */
	x86_alu_reg_imm (buf, X86_ADD, X86_ESP, 4);		
	/* restore LMF end */

	/* Restore caller saved registers */
	x86_mov_reg_membase (buf, X86_ECX, X86_ESP, 1 * 4, 4);
	x86_mov_reg_membase (buf, X86_EDX, X86_ESP, 2 * 4, 4);

	/* Pop saved reg array + method ptr */
	x86_alu_reg_imm (buf, X86_ADD, X86_ESP, 9 * 4);

	if (tramp_type == MONO_TRAMPOLINE_CLASS_INIT)
		x86_ret (buf);
	else
		/* call the compiled method */
		x86_jump_reg (buf, X86_EAX);

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

	mono_domain_lock (domain);
	code = buf = mono_code_manager_reserve (domain->code_mp, TRAMPOLINE_SIZE);
	mono_domain_unlock (domain);

	x86_push_imm (buf, arg1);
	x86_jump_code (buf, tramp);
	g_assert ((buf - code) <= TRAMPOLINE_SIZE);

	mono_arch_flush_icache (code, buf - code);

	mono_jit_stats.method_trampolines++;

	if (code_len)
		*code_len = buf - code;

	return code;
}

void
mono_arch_invalidate_method (MonoJitInfo *ji, void *func, gpointer func_arg)
{
	/* FIXME: This is not thread safe */
	guint8 *code = ji->code_start;

	x86_push_imm (code, func_arg);
	x86_call_code (code, (guint8*)func);
}

/*
 * This method is only called when running in the Mono Debugger.
 */
gpointer
mono_debugger_create_notification_function (gpointer *notification_address)
{
	guint8 *ptr, *buf;

	ptr = buf = mono_global_codeman_reserve (16);

	x86_breakpoint (buf);
	if (notification_address)
		*notification_address = buf;
	x86_ret (buf);

	return ptr;
}
