/*
 * trampoline.c: JIT trampoline code
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>
#include <mono/arch/x86/x86-codegen.h>

#include "jit.h"
#include "codegen.h"

static void
arch_remoting_invoke (MonoMethod *method, gpointer ip, gpointer first_arg)
{
	MonoMethodSignature *sig = method->signature;
	MonoMethodMessage *msg;
	MonoTransparentProxy *this;
	MonoObject *res, *exc;
	MonoArray *out_args;
	int this_pos = 0;

	//printf ("REMOTING %s.%s:%s\n", method->klass->name_space, method->klass->name,
	//method->name);

	if (ISSTRUCT (sig->ret))
		this_pos += 4;

	this = *(MonoTransparentProxy **)(((char *)&first_arg) + this_pos);

	g_assert (((MonoObject *)this)->vtable->klass == mono_defaults.transparent_proxy_class);

	msg = arch_method_call_message_new (method, &first_arg, NULL, NULL, NULL);

	res = mono_remoting_invoke ((MonoObject *)this->rp, msg, &exc, &out_args);

	if (exc)
		mono_raise_exception ((MonoException *)exc);

	arch_method_return_message_restore (method, &first_arg, res, out_args);

	/* WARNING: do not write any code here, because that would destroy 
	 * the return value 
	 */
}

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
	int this_pos = 4;

	if (!m->signature->ret->byref && m->signature->ret->type == MONO_TYPE_VALUETYPE)
		this_pos = 8;
	    
	start = code = g_malloc (16);

	x86_alu_membase_imm (code, X86_ADD, X86_ESP, this_pos, sizeof (MonoObject));
	x86_jump_code (code, addr);
	g_assert ((code - start) < 16);

	return start;
}

/**
 * x86_magic_trampoline:
 * @eax: saved x86 register 
 * @ecx: saved x86 register 
 * @edx: saved x86 register 
 * @esi: saved x86 register 
 * @edi: saved x86 register 
 * @ebx: saved x86 register
 * @code: pointer into caller code
 * @method: the method to translate
 *
 * This method is called by the trampoline functions for virtual
 * methods. It inspects the caller code to find the address of the
 * vtable slot, then calls the JIT compiler and writes the address
 * of the compiled method back to the vtable. All virtual methods 
 * are called with: x86_call_membase (inst, basereg, disp). We always
 * use 32 bit displacement to ensure that the length of the call 
 * instruction is 6 bytes. We need to get the value of the basereg 
 * and the constant displacement.
 */
static gpointer
x86_magic_trampoline (int eax, int ecx, int edx, int esi, int edi, 
		      int ebx, guint8 *code, MonoMethod *m)
{
	guint8 reg;
	gint32 disp;
	char *o;
	gpointer addr;

	EnterCriticalSection (metadata_section);
	addr = arch_compile_method (m);
	LeaveCriticalSection (metadata_section);
	g_assert (addr);

	/* go to the start of the call instruction
	 *
	 * address_byte = (m << 6) | (o << 3) | reg
	 * call opcode: 0xff address_byte displacement
	 * 0xff m=1,o=2 imm8
	 * 0xff m=2,o=2 imm32
	 */
	code -= 6;
	if ((code [1] != 0xe8) && (code [3] == 0xff) && ((code [4] & 0x18) == 0x10) && ((code [4] >> 6) == 1)) {
		reg = code [4] & 0x07;
		disp = (signed char)code [5];
	} else {
		if ((code [0] == 0xff) && ((code [1] & 0x18) == 0x10) && ((code [1] >> 6) == 2)) {
			reg = code [1] & 0x07;
			disp = *((gint32*)(code + 2));
		} else if ((code [1] == 0xe8)) {
			*((guint32*)(code + 2)) = (guint)addr - ((guint)code + 1) - 5; 
			return addr;
		} else {
			printf ("%x %x %x %x %x %x \n", code [0], code [1], code [2], code [3],
				code [4], code [5]);
			g_assert_not_reached ();
		}
	}

	switch (reg) {
	case X86_EAX:
		o = (gpointer)eax;
		break;
	case X86_EDX:
		o = (gpointer)edx;
		break;
	case X86_ECX:
		o = (gpointer)ecx;
		break;
	case X86_ESI:
		o = (gpointer)esi;
		break;
	case X86_EDI:
		o = (gpointer)edi;
		break;
	case X86_EBX:
		o = (gpointer)ebx;
		break;
	default:
		g_assert_not_reached ();
	}

	o += disp;

	if (m->klass->valuetype) {
		return *((gpointer *)o) = get_unbox_trampoline (m, addr);
	} else {
		return *((gpointer *)o) = addr;
	}
}

/**
 * arch_create_jit_trampoline:
 * @method: pointer to the method info
 *
 * Creates a trampoline function for virtual methods. If the created
 * code is called it first starts JIT compilation of method,
 * and then calls the newly created method. I also replaces the
 * corresponding vtable entry (see x86_magic_trampoline).
 * 
 * Returns: a pointer to the newly created code 
 */
gpointer
arch_create_jit_trampoline (MonoMethod *method)
{
	MonoDomain *domain = mono_domain_get ();
	guint8 *code, *buf;
	static guint8 *vc = NULL;
	GHashTable *jit_code_hash;

	/* previously created trampoline code */
	if (method->info)
		return method->info;

	/* we immediately compile runtime provided functions */
	if (method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) {
		method->info = arch_compile_method (method);
		return method->info;
	}

	/* icalls use method->addr */
	if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
	    (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL)) {
		method->info = arch_create_native_wrapper (method);
		return method->info;
	}

	/* check if we already have JITed code */
	if (mono_jit_share_code)
		jit_code_hash = mono_root_domain->jit_code_hash;
	else
		jit_code_hash = domain->jit_code_hash;

	if ((code = g_hash_table_lookup (jit_code_hash, method))) {
		mono_jit_stats.methods_lookups++;
		return code;
	}

	if (!vc) {
		vc = buf = g_malloc (256);
		/* save caller save regs because we need to do a call */ 
		x86_push_reg (buf, X86_EDX);
		x86_push_reg (buf, X86_EAX);
		x86_push_reg (buf, X86_ECX);

		/* save LMF begin */

		/* save the IP (caller ip) */
		x86_push_membase (buf, X86_ESP, 16);

		x86_push_reg (buf, X86_EBX);
		x86_push_reg (buf, X86_EDI);
		x86_push_reg (buf, X86_ESI);
		x86_push_reg (buf, X86_EBP);

		/* save method info */
		x86_push_membase (buf, X86_ESP, 32);
		/* get the address of lmf for the current thread */
		x86_call_code (buf, arch_get_lmf_addr);
		/* push lmf */
		x86_push_reg (buf, X86_EAX); 
		/* push *lfm (previous_lmf) */
		x86_push_membase (buf, X86_EAX, 0);
		/* *(lmf) = ESP */
		x86_mov_membase_reg (buf, X86_EAX, 0, X86_ESP, 4);
		/* save LFM end */

		/* push the method info */
		x86_push_membase (buf, X86_ESP, 44);
		/* push the return address onto the stack */
		x86_push_membase (buf, X86_ESP, 52);

		/* save all register values */
		x86_push_reg (buf, X86_EBX);
		x86_push_reg (buf, X86_EDI);
		x86_push_reg (buf, X86_ESI);
		x86_push_membase (buf, X86_ESP, 64); /* EDX */
		x86_push_membase (buf, X86_ESP, 64); /* ECX */
		x86_push_membase (buf, X86_ESP, 64); /* EAX */

		x86_call_code (buf, x86_magic_trampoline);
		x86_alu_reg_imm (buf, X86_ADD, X86_ESP, 8*4);

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
		x86_pop_reg (buf, X86_EBP);
		x86_pop_reg (buf, X86_ESI);
		x86_pop_reg (buf, X86_EDI);
		x86_pop_reg (buf, X86_EBX);
		/* discard save IP */
		x86_alu_reg_imm (buf, X86_ADD, X86_ESP, 4);		
		/* restore LMF end */

		x86_alu_reg_imm (buf, X86_ADD, X86_ESP, 16);

		/* call the compiled method */
		x86_jump_reg (buf, X86_EAX);

		g_assert ((buf - vc) <= 256);
	}

	code = buf = g_malloc (16);
	x86_push_imm (buf, method);
	x86_jump_code (buf, vc);
	g_assert ((buf - code) <= 16);

	/* store trampoline address */
	method->info = code;

	mono_jit_stats.method_trampolines++;

	return code;
}

/* arch_create_remoting_trampoline:
 * @method: pointer to the method info
 *
 * Creates a trampoline which calls the remoting functions. This
 * is used in the vtable of transparent proxies.
 * 
 * Returns: a pointer to the newly created code 
 */
gpointer
arch_create_remoting_trampoline (MonoMethod *method)
{
	MonoJitInfo *ji;
	guint8 *code, *buf;

	if (method->remoting_tramp)
		return method->remoting_tramp;
	
	code = buf = g_malloc (16);
	x86_push_imm (buf, method);
	x86_call_code (buf, arch_remoting_invoke);
	x86_alu_reg_imm (buf, X86_ADD, X86_ESP, 4);
	x86_ret (buf);
	
	g_assert ((buf - code) <= 16);

	method->remoting_tramp = code;

	/* we store a jit info, so that mono_delegate_ctor
	 * is able to find a method info */
	ji = mono_mempool_alloc0 (mono_root_domain->mp, sizeof (MonoJitInfo));
	ji->method = method;
	ji->code_start = code;
	ji->code_size = buf - code;
	ji->used_regs = 0;
	ji->num_clauses = 0;
	mono_jit_info_table_add (mono_root_domain, ji);

	return code;
}
