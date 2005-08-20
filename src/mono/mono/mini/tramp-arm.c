/*
 * tramp-arm.c: JIT trampoline code for ARM
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/tabledefs.h>
#include <mono/arch/arm/arm-codegen.h>
#include <mono/metadata/mono-debug-debugger.h>

#include "mini.h"
#include "mini-arm.h"

/*
 * get_unbox_trampoline:
 * @m: method pointer
 * @addr: pointer to native code for @m
 *
 * when value type methods are called through the vtable we need to unbox the
 * this argument. This method returns a pointer to a trampoline which does
 * unboxing before calling the method
 */
static gpointer
get_unbox_trampoline (MonoMethod *m, gpointer addr)
{
	guint8 *code, *start;
	int this_pos = 0;
	MonoDomain *domain = mono_domain_get ();

	if (!mono_method_signature (m)->ret->byref && MONO_TYPE_ISSTRUCT (mono_method_signature (m)->ret))
		this_pos = 1;

	mono_domain_lock (domain);
	start = code = mono_code_manager_reserve (domain->code_mp, 16);
	mono_domain_unlock (domain);

	ARM_LDR_IMM (code, ARMREG_IP, ARMREG_PC, 4);
	ARM_ADD_REG_IMM8 (code, this_pos, this_pos, sizeof (MonoObject));
	ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_IP);
	*(guint32*)code = (guint32)addr;
	code += 4;
	mono_arch_flush_icache (start, code - start);
	g_assert ((code - start) <= 16);
	/*g_print ("unbox trampoline at %d for %s:%s\n", this_pos, m->klass->name, m->name);
	g_print ("unbox code is at %p for method at %p\n", start, addr);*/

	return start;
}

/* Stack size for trampoline function 
 */
#define STACK (sizeof (MonoLMF))

/* Method-specific trampoline code fragment size */
#define METHOD_TRAMPOLINE_SIZE 64

/* Jump-specific trampoline code fragment size */
#define JUMP_TRAMPOLINE_SIZE   64

/**
 * arm_magic_trampoline:
 * @code: pointer into caller code
 * @method: the method to translate
 * @sp: stack pointer
 *
 * This method is called by the function 'arch_create_jit_trampoline', which in
 * turn is called by the trampoline functions for virtual methods.
 * After having called the JIT compiler to compile the method, it inspects the
 * caller code to find the address of the method-specific part of the
 * trampoline vtable slot for this method, updates it with a fragment that calls
 * the newly compiled code and returns this address of the compiled code to
 * 'arch_create_jit_trampoline' 
 */
static gpointer
arm_magic_trampoline (MonoMethod *method, guint32 *code, gchar **sp)
{
	char *o = NULL;
	gpointer addr;
        MonoJitInfo *ji, *target_ji;
	int reg, offset = 0;

	addr = mono_compile_method (method);
	/*g_print ("method code at %p for %s:%s\n", addr, method->klass->name, method->name);*/
	g_assert(addr);

	if (!code) {
		return addr;
	}

	/* We can't trampoline across domains */
	ji = mono_jit_info_table_find (mono_domain_get (), code);
	target_ji = mono_jit_info_table_find (mono_domain_get (), addr);
	if (!mono_method_same_domain (ji, target_ji))
		return addr;

	/* Locate the address of the method-specific trampoline. The call using
	the vtable slot that took the processing flow to 'arch_create_jit_trampoline' 
	looks something like this:

		ldr rA, rX, #offset
		mov lr, pc
		mov pc, rA
	or better:
		mov lr, pc
		ldr pc, rX, #offset

	The call sequence could be also:
		ldr ip, pc, 0
		b skip
		function pointer literal
		skip:
		mov lr, pc
		mov pc, ip
	Note that on ARM5+ we can use one instruction instead of the last two.
	Therefore, we need to locate the 'ldr rA' instruction to know which
	register was used to hold the method addrs.
	*/
	
	/* This is the 'bl' or the 'mov pc' instruction */
	--code;
	
	/*
	 * Note that methods are called also with the bl opcode.
	 */
	if ((((*code) >> 25)  & 7) == 5) {
		/*g_print ("direct patching\n");*/
		arm_patch ((char*)code, addr);
		mono_arch_flush_icache ((char*)code, 4);
		return addr;
	}

	/* ldr pc, rX, #offset */
#define LDR_MASK ((0xf << ARMCOND_SHIFT) | (3 << 26) | (1 << 22) | (1 << 20) | (15 << 12))
#define LDR_PC_VAL ((ARMCOND_AL << ARMCOND_SHIFT) | (1 << 26) | (0 << 22) | (1 << 20) | (15 << 12))
	if ((*code & LDR_MASK) == LDR_PC_VAL) {
		reg = (*code >> 16 ) & 0xf;
		offset = *code & 0xfff;
		/*g_print ("found vcall at r%d + %d\n", reg, offset);*/
		o = sp [reg];
	}

	/* this is not done for non-virtual calls, because in that case
	   we won't have an object, but the actual pointer to the 
	   valuetype as the this argument
	 */
	if (method->klass->valuetype && !mono_aot_is_got_entry (code, o))
		addr = get_unbox_trampoline (method, addr);

	if (o) {
		o += offset;
		if (mono_aot_is_got_entry (code, o) || mono_domain_owns_vtable_slot (mono_domain_get (), o))
			*((gpointer *)o) = addr;
	} else {
		/*g_print ("no callsite patching\n");
		mono_disassemble_code (code -3, 16, "callsite");*/
	}

	return addr;
}

static void
arm_class_init_trampoline (void *vtable, guint32 *code, char *sp)
{
	mono_runtime_class_init (vtable);

#if 0
	/* This is the 'bl' instruction */
	--code;
	
	if ((((*code) >> 25)  & 7) == 5) {
		ARM_NOP (code); /* nop */
		mono_arch_flush_icache (code, 4);
		return;
	} else {
		g_assert_not_reached ();
	}
#endif
}

/*
 * Stack frame description when the generic trampoline is called.
 * caller frame
 * ------------------- old sp
 *  MonoLMF
 * ------------------- sp
 */
guchar*
mono_arch_create_trampoline_code (MonoTrampolineType tramp_type)
{
	guint8 *buf, *code = NULL;
	int i, offset;
	guint8 *load_get_lmf_addr, *load_trampoline;
	gpointer *constants;

	/* Now we'll create in 'buf' the ARM trampoline code. This
	 is the trampoline code common to all methods  */
	
	code = buf = g_malloc(512);

	/*
	 * At this point r0 has the method and sp points to the saved
	 * regs on the stack (all but PC and SP).
	 */
	ARM_MOV_REG_REG (buf, ARMREG_V1, ARMREG_SP);
	ARM_MOV_REG_REG (buf, ARMREG_V2, ARMREG_R0);
	ARM_MOV_REG_REG (buf, ARMREG_V3, ARMREG_LR);

	/* ok, now we can continue with the MonoLMF setup, mostly untouched 
	 * from emit_prolog in mini-arm.c
	 * This is a sinthetized call to mono_get_lmf_addr ()
	 */
	load_get_lmf_addr = buf;
	buf += 4;
	ARM_MOV_REG_REG (buf, ARMREG_LR, ARMREG_PC);
	ARM_MOV_REG_REG (buf, ARMREG_PC, ARMREG_R0);

	/* we build the MonoLMF structure on the stack - see mini-arm.h
	 * The pointer to the struct is put in r1.
	 * the iregs array is already allocated on the stack by push.
	 */
	ARM_SUB_REG_IMM8 (buf, ARMREG_SP, ARMREG_SP, sizeof (MonoLMF) - sizeof (guint) * 14);
	ARM_ADD_REG_IMM8 (buf, ARMREG_R1, ARMREG_SP, STACK - sizeof (MonoLMF));
	/* r0 is the result from mono_get_lmf_addr () */
	ARM_STR_IMM (buf, ARMREG_R0, ARMREG_R1, G_STRUCT_OFFSET (MonoLMF, lmf_addr));
	/* new_lmf->previous_lmf = *lmf_addr */
	ARM_LDR_IMM (buf, ARMREG_R2, ARMREG_R0, G_STRUCT_OFFSET (MonoLMF, previous_lmf));
	ARM_STR_IMM (buf, ARMREG_R2, ARMREG_R1, G_STRUCT_OFFSET (MonoLMF, previous_lmf));
	/* *(lmf_addr) = r1 */
	ARM_STR_IMM (buf, ARMREG_R1, ARMREG_R0, G_STRUCT_OFFSET (MonoLMF, previous_lmf));
	/* save method info (it's in v2) */
	ARM_STR_IMM (buf, ARMREG_V2, ARMREG_R1, G_STRUCT_OFFSET (MonoLMF, method));
	ARM_STR_IMM (buf, ARMREG_SP, ARMREG_R1, G_STRUCT_OFFSET (MonoLMF, ebp));
	/* save the IP (caller ip) */
	if (tramp_type == MONO_TRAMPOLINE_JUMP) {
		ARM_MOV_REG_IMM8 (buf, ARMREG_R2, 0);
	} else {
		/* assumes STACK == sizeof (MonoLMF) */
		ARM_LDR_IMM (buf, ARMREG_R2, ARMREG_SP, (G_STRUCT_OFFSET (MonoLMF, iregs) + 13*4));
	}
	ARM_STR_IMM (buf, ARMREG_R2, ARMREG_R1, G_STRUCT_OFFSET (MonoLMF, eip));

	/*
	 * Now we're ready to call arm_magic_trampoline ().
	 */
	/* Arg 1: MonoMethod *method. It was put in v2 */
	ARM_MOV_REG_REG (buf, ARMREG_R0, ARMREG_V2);

	/* Arg 2: code (next address to the instruction that called us) */
	if (tramp_type == MONO_TRAMPOLINE_JUMP) {
		ARM_MOV_REG_IMM8 (buf, ARMREG_R1, 0);
	} else {
		ARM_MOV_REG_REG (buf, ARMREG_R1, ARMREG_V3);
	}
	
	/* Arg 3: stack pointer so that the magic trampoline can access the
	 * registers we saved above
	 */
	ARM_MOV_REG_REG (buf, ARMREG_R2, ARMREG_V1);

	load_trampoline = buf;
	buf += 4;

	ARM_MOV_REG_REG (buf, ARMREG_LR, ARMREG_PC);
	ARM_MOV_REG_REG (buf, ARMREG_PC, ARMREG_IP);
	
	/* OK, code address is now on r0. Move it to the place on the stack
	 * where IP was saved (it is now no more useful to us and it can be
	 * clobbered). This way we can just restore all the regs in one inst
	 * and branch to IP.
	 */
	ARM_STR_IMM (buf, ARMREG_R0, ARMREG_V1, (ARMREG_R12 * 4));

	/*
	 * Now we restore the MonoLMF (see emit_epilogue in mini-arm.c)
	 * and the rest of the registers, so the method called will see
	 * the same state as before we executed.
	 * The pointer to MonoLMF is in r2.
	 */
	ARM_MOV_REG_REG (buf, ARMREG_R2, ARMREG_SP);
	/* ip = previous_lmf */
	ARM_LDR_IMM (buf, ARMREG_IP, ARMREG_R2, G_STRUCT_OFFSET (MonoLMF, previous_lmf));
	/* lr = lmf_addr */
	ARM_LDR_IMM (buf, ARMREG_LR, ARMREG_R2, G_STRUCT_OFFSET (MonoLMF, lmf_addr));
	/* *(lmf_addr) = previous_lmf */
	ARM_STR_IMM (buf, ARMREG_LR, ARMREG_IP, G_STRUCT_OFFSET (MonoLMF, previous_lmf));

	/* Non-standard function epilogue. Instead of doing a proper
	 * return, we just jump to the compiled code.
	 */
	/* Restore the registers and jump to the code:
	 * Note that IP has been conveniently set to the method addr.
	 */
	ARM_ADD_REG_IMM8 (buf, ARMREG_SP, ARMREG_SP, sizeof (MonoLMF) - sizeof (guint) * 14);
	ARM_POP_NWB (buf, 0x5fff);
	/* do we need to set sp? */
	ARM_ADD_REG_IMM8 (buf, ARMREG_SP, ARMREG_SP, (14 * 4));
	ARM_MOV_REG_REG (buf, ARMREG_PC, ARMREG_IP);

	constants = (gpointer*)buf;
	constants [0] = mono_get_lmf_addr;
	if (tramp_type == MONO_TRAMPOLINE_CLASS_INIT) {
		constants [1] = arm_class_init_trampoline;
	} else {
		constants [1] = arm_magic_trampoline;
	}

	/* backpatch by emitting the missing instructions skipped above */
	ARM_LDR_IMM (load_get_lmf_addr, ARMREG_R0, ARMREG_PC, (buf - load_get_lmf_addr - 8));
	ARM_LDR_IMM (load_trampoline, ARMREG_IP, ARMREG_PC, (buf + 4 - load_trampoline - 8));

	buf += 8;

	/* Flush instruction cache, since we've generated code */
	mono_arch_flush_icache (code, buf - code);

	/* Sanity check */
	g_assert ((buf - code) <= 512);

	return code;
}

static MonoJitInfo*
create_specific_tramp (MonoMethod *method, guint8* tramp, MonoDomain *domain) {
	guint8 *code, *buf;
	MonoJitInfo *ji;
	gpointer *constants;

	mono_domain_lock (domain);
	code = buf = mono_code_manager_reserve (domain->code_mp, 24);
	mono_domain_unlock (domain);

	/* we could reduce this to 12 bytes if tramp is within reach:
	 * ARM_PUSH ()
	 * ARM_BL ()
	 * method-literal
	 * The called code can access method using the lr register
	 * A 20 byte sequence could be:
	 * ARM_PUSH ()
	 * ARM_MOV_REG_REG (lr, pc)
	 * ARM_LDR_IMM (pc, pc, 0)
	 * method-literal
	 * tramp-literal
	 */
	/* We save all the registers, except PC and SP */
	ARM_PUSH (buf, 0x5fff);
	ARM_LDR_IMM (buf, ARMREG_R0, ARMREG_PC, 4); /* method is the only arg */
	ARM_LDR_IMM (buf, ARMREG_R1, ARMREG_PC, 4); /* temp reg */
	ARM_MOV_REG_REG (buf, ARMREG_PC, ARMREG_R1);

	constants = (gpointer*)buf;
	constants [0] = method;
	constants [1] = tramp;
	buf += 8;

	/* Flush instruction cache, since we've generated code */
	mono_arch_flush_icache (code, buf - code);

	g_assert ((buf - code) <= 24);

	ji = g_new0 (MonoJitInfo, 1);
	ji->method = method;
	ji->code_start = code;
	ji->code_size = buf - code;

	mono_jit_stats.method_trampolines++;

	return ji;
}

MonoJitInfo*
mono_arch_create_jump_trampoline (MonoMethod *method)
{
	guint8 *tramp;
	MonoDomain* domain = mono_domain_get ();
	
	tramp = mono_get_trampoline_code (MONO_TRAMPOLINE_JUMP);
	return create_specific_tramp (method, tramp, domain);
}

/**
 * arch_create_jit_trampoline:
 * @method: pointer to the method info
 *
 * Creates a trampoline function for virtual methods. If the created
 * code is called it first starts JIT compilation of method,
 * and then calls the newly created method. It also replaces the
 * corresponding vtable entry (see arm_magic_trampoline).
 *
 * A trampoline consists of two parts: a main fragment, shared by all method
 * trampolines, and some code specific to each method, which hard-codes a
 * reference to that method and then calls the main fragment.
 *
 * The main fragment contains a call to 'arm_magic_trampoline', which performs
 * call to the JIT compiler and substitutes the method-specific fragment with
 * some code that directly calls the JIT-compiled method.
 * 
 * Returns: a pointer to the newly created code 
 */
gpointer
mono_arch_create_jit_trampoline (MonoMethod *method)
{
	guint8 *tramp;
	MonoJitInfo *ji;
	MonoDomain* domain = mono_domain_get ();
	gpointer code_start;

	tramp = mono_get_trampoline_code (MONO_TRAMPOLINE_GENERIC);
	/* FIXME: should pass the domain down to this function */
	ji = create_specific_tramp (method, tramp, domain);
	code_start = ji->code_start;
	g_free (ji);

	return code_start;
}

/**
 * mono_arch_create_class_init_trampoline:
 *  @vtable: the type to initialize
 *
 * Creates a trampoline function to run a type initializer. 
 * If the trampoline is called, it calls mono_runtime_class_init with the
 * given vtable, then patches the caller code so it does not get called any
 * more.
 * 
 * Returns: a pointer to the newly created code 
 */
gpointer
mono_arch_create_class_init_trampoline (MonoVTable *vtable)
{
	guint8 *code, *buf, *tramp;
	gpointer *constants;

	tramp = mono_get_trampoline_code (MONO_TRAMPOLINE_CLASS_INIT);

	mono_domain_lock (vtable->domain);
	code = buf = mono_code_manager_reserve (vtable->domain->code_mp, METHOD_TRAMPOLINE_SIZE);
	mono_domain_unlock (vtable->domain);

	ARM_MOV_REG_REG (buf, ARMREG_IP, ARMREG_SP);
	ARM_PUSH (buf, ((1 << ARMREG_IP) | (1 << ARMREG_LR)));
	ARM_MOV_REG_REG (buf, ARMREG_R1, ARMREG_LR);
	ARM_LDR_IMM (buf, ARMREG_R0, ARMREG_PC, 12); /* load vtable */
	ARM_LDR_IMM (buf, ARMREG_R3, ARMREG_PC, 12); /* load the func address */
	/* make the call */
	ARM_MOV_REG_REG (buf, ARMREG_LR, ARMREG_PC);
	ARM_MOV_REG_REG (buf, ARMREG_PC, ARMREG_R3);

	/* restore and return */
	ARM_POP_NWB (buf, ((1 << ARMREG_SP) | (1 << ARMREG_PC)));
	constants = (gpointer*)buf;
	constants [0] = vtable;
	constants [1] = arm_class_init_trampoline;
	buf += 8;

	/* Flush instruction cache, since we've generated code */
	mono_arch_flush_icache (code, buf - code);
		
	/* Sanity check */
	g_assert ((buf - code) <= METHOD_TRAMPOLINE_SIZE);

	mono_jit_stats.method_trampolines++;

	return code;
}

/*
 * This method is only called when running in the Mono Debugger.
 */
gpointer
mono_debugger_create_notification_function (gpointer *notification_address)
{
	guint8 *ptr, *buf;

	ptr = buf = g_malloc0 (8);
	//FIXME: ARM_SWI (buf, 0x9F0001);
	if (notification_address)
		*notification_address = buf;
	ARM_MOV_REG_REG (buf, ARMREG_PC, ARMREG_LR);
	mono_arch_flush_icache (ptr, buf - ptr);

	return ptr;
}

