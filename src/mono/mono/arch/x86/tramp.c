/*
 * Create trampolines to invoke arbitrary functions.
 * 
 * Copyright (C) Ximian Inc.
 * 
 * Author: Paolo Molaro (lupus@ximian.com)
 * 
 */

#include "x86-codegen.h"
#include "mono/metadata/class.h"
#include "mono/interpreter/interp.h"

/*
 * The resulting function takes the form:
 * void func (void (*callme)(), void *retval, void *this_obj, stackval *arguments);
 */
#define FUNC_ADDR_POS	8
#define RETVAL_POS	12
#define THIS_POS	16
#define ARGP_POS	20
#define LOC_POS	-4

#define ARG_SIZE	sizeof (stackval)

static char *
mono_get_ansi_string (MonoObject *o)
{
	MonoStringObject *s = (MonoStringObject *)o;
	char *as, *vector;
	int i;

	g_assert (o != NULL);

	if (!s->length)
		return g_strdup ("");

	vector = s->c_str->vector;

	g_assert (vector != NULL);

	as = g_malloc (s->length + 1);

	/* fixme: replace with a real unicode/ansi conversion */
	for (i = 0; i < s->length; i++) {
		as [i] = vector [i*2];
	}

	as [i] = '\0';

	return as;
}

MonoPIFunc
mono_create_trampoline (MonoMethod *method)
{
	MonoMethodSignature *sig;
	unsigned char *p, *code_buffer;
	guint32 local_size = 0, stack_size = 0, code_size = 30;
	guint32 arg_pos;
	int i, stringp;

	sig = method->signature;
	
	if (sig->hasthis) {
		stack_size += sizeof (gpointer);
		code_size += 5;
	}
	
	for (i = 0; i < sig->param_count; ++i) {
		if (sig->params [i]->byref) {
			stack_size += sizeof (gpointer);
			code_size += i < 10 ? 5 : 8;
			continue;
		}
		switch (sig->params [i]->type) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_PTR:
		case MONO_TYPE_R4:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
			stack_size += 4;
			code_size += i < 10 ? 5 : 8;
			break;
		case MONO_TYPE_STRING:
			stack_size += 4;
			code_size += 20;
			local_size++;
			break;
		case MONO_TYPE_I8:
			stack_size += 8;
			code_size += i < 10 ? 5 : 8;
			break;
		case MONO_TYPE_R8:
			stack_size += 8;
			code_size += i < 10 ? 7 : 10;
			break;
		default:
			g_error ("Can't trampoline 0x%x", sig->params [i]->type);
		}
	}
	/*
	 * FIXME: take into account large return values.
	 */

	code_buffer = p = alloca (code_size);

	/*
	 * Standard function prolog.
	 */
	x86_push_reg (p, X86_EBP);
	x86_mov_reg_reg (p, X86_EBP, X86_ESP, 4);
	/*
	 * We store some local vars here to handle string pointers.
	 */
	if (local_size)
		x86_alu_reg_imm (p, X86_SUB, X86_ESP, local_size * 4);

	/*
	 * We'll need to align to at least 8 bytes boudary... (16 may be better)
	 * x86_alu_reg_imm (p, X86_SUB, X86_ESP, stack_size);
	 */

	/*
	 * EDX has the pointer to the args.
	 */
	x86_mov_reg_membase (p, X86_EDX, X86_EBP, ARGP_POS, 4);

	/*
	 * Push arguments in reverse order.
	 */
	stringp = 0;
	for (i = sig->param_count; i; --i) {
		arg_pos = ARG_SIZE * (i - 1);
		if (sig->params [i - 1]->byref) {
			x86_push_membase (p, X86_EDX, arg_pos);
			continue;
		}
		switch (sig->params [i - 1]->type) {
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_PTR:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_R4:
			x86_push_membase (p, X86_EDX, arg_pos);
			break;
		case MONO_TYPE_R8:
			x86_alu_reg_imm (p, X86_SUB, X86_ESP, 8);
			x86_fld_membase (p, X86_EDX, arg_pos, TRUE);
			x86_fst_membase (p, X86_ESP, 0, TRUE, TRUE);
			break;
		case MONO_TYPE_STRING:
			/*if (frame->method->flags & PINVOKE_ATTRIBUTE_CHAR_SET_ANSI*/
			x86_push_membase (p, X86_EDX, arg_pos);
			x86_mov_reg_imm (p, X86_EDX, mono_get_ansi_string);
			x86_call_reg (p, X86_EDX);
			x86_alu_reg_imm (p, X86_SUB, X86_ESP, 4);
			x86_push_reg (p, X86_EAX);
			/*
			 * Store the pointer in a local we'll free later.
			 */
			stringp++;
			x86_mov_membase_reg (p, X86_EBP, LOC_POS * stringp, X86_EAX, 4);
			/*
			 * we didn't save the reg: restore it here.
			 */
			x86_mov_reg_membase (p, X86_EDX, X86_EBP, ARGP_POS, 4);
			break;
		case MONO_TYPE_I8:
			x86_push_membase (p, X86_EDX, arg_pos + 4);
			x86_push_membase (p, X86_EDX, arg_pos);
			break;
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		default:
			g_error ("Can't trampoline 0x%x", sig->params [i - 1]->type);
		}
	}

	if (sig->hasthis) {
		if (sig->call_convention != MONO_CALL_THISCALL) {
			x86_mov_reg_membase (p, X86_EDX, X86_EBP, THIS_POS, 4);
			x86_push_reg (p, X86_EDX);
		} else {
			x86_mov_reg_membase (p, X86_ECX, X86_EBP, THIS_POS, 4);
		}
	}

	/* 
	 * Insert call to function 
	 */
	x86_mov_reg_membase (p, X86_EDX, X86_EBP, FUNC_ADDR_POS, 4);
	x86_call_reg (p, X86_EDX);

	/*
	 * Handle retval.
	 * Small integer and pointer values are in EAX.
	 * Long integers are in EAX:EDX.
	 * FP values are on the FP stack.
	 */
	if (sig->ret->byref) {
		x86_mov_reg_membase (p, X86_ECX, X86_EBP, RETVAL_POS, 4);
		x86_mov_regp_reg (p, X86_ECX, X86_EAX, 4);
	} else {
		switch (sig->ret->type) {
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_OBJECT:
			x86_mov_reg_membase (p, X86_ECX, X86_EBP, RETVAL_POS, 4);
			x86_mov_regp_reg (p, X86_ECX, X86_EAX, 4);
			break;
		case MONO_TYPE_R4:
			x86_mov_reg_membase (p, X86_ECX, X86_EBP, RETVAL_POS, 4);
			x86_fst_membase (p, X86_ECX, 0, FALSE, TRUE);
			break;
		case MONO_TYPE_R8:
			x86_mov_reg_membase (p, X86_ECX, X86_EBP, RETVAL_POS, 4);
			x86_fst_membase (p, X86_ECX, 0, TRUE, TRUE);
			break;
		case MONO_TYPE_I8:
			x86_mov_reg_membase (p, X86_ECX, X86_EBP, RETVAL_POS, 4);
			x86_mov_regp_reg (p, X86_ECX, X86_EAX, 4);
			x86_mov_membase_reg (p, X86_ECX, 4, X86_EDX, 4);
			break;
		case MONO_TYPE_VOID:
			break;
		default:
			g_error ("Can't handle as return value 0x%x", sig->ret->type);
		}
	}

	/*
	 * free the allocated strings.
	 */
	if (local_size)
		x86_mov_reg_imm (p, X86_EDX, g_free);
	for (i = 1; i <= local_size; ++i) {
		x86_push_membase (p, X86_EBP, LOC_POS * i);
		x86_call_reg (p, X86_EDX);
	}
	/*
	 * Standard epilog.
	 */
	x86_leave (p);
	x86_ret (p);

	return g_memdup (code_buffer, p - code_buffer);
}

