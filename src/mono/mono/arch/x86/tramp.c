/*
 * Create trampolines to invoke arbitrary functions.
 * 
 * Copyright (C) Ximian Inc.
 * 
 * Author: Paolo Molaro (lupus@ximian.com)
 * 
 */

#include "config.h"
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
	 * and align to 16 byte boundary...
	 */
	if (local_size) {
		x86_alu_reg_imm (p, X86_SUB, X86_ESP, local_size * 4);
		stack_size = (stack_size * local_size * 4) % 16;
	} else {
		stack_size = stack_size % 16;
	}
	x86_alu_reg_imm (p, X86_SUB, X86_ESP, stack_size);

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
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
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
		case MONO_TYPE_STRING: /* this is going to cause large pains... */
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

#define MINV_POS  (- sizeof (MonoInvocation))
#define STACK_POS (MINV_POS - sizeof (stackval) * sig->param_count)
#define OBJ_POS   8
#define TYPE_OFFSET (G_STRUCT_OFFSET (stackval, type))

/*
 * Returns a pointer to a native function that can be used to
 * call the specified method.
 * The function created will receive the arguments according
 * to the call convention specified in the method.
 * This function works by creating a MonoInvocation structure,
 * filling the fields in and calling ves_exec_method on it.
 * Still need to figure out how to handle the exception stuff
 * across the managed/unmanaged boundary.
 */
void *
mono_create_method_pointer (MonoMethod *method)
{
	MonoMethodSignature *sig;
	unsigned char *p, *code_buffer;
	gint32 local_size;
	gint32 stackval_pos, arg_pos = 8;
	int i;

	/*
	 * If it is a static P/Invoke method, we can just return the pointer
	 * to the method implementation.
	 */
	sig = method->signature;

	code_buffer = p = alloca (512); /* FIXME: check for overflows... */

	local_size = sizeof (MonoInvocation) + sizeof (stackval) * (sig->param_count + 1);
	stackval_pos = -local_size;

	/*
	 * Standard function prolog with magic trick.
	 */
	x86_jump_code (p, code_buffer + 8);
	*p++ = 'M';
	*p++ = 'o';
	*(void**)p = method;
	p += 4;
	x86_push_reg (p, X86_EBP);
	x86_mov_reg_reg (p, X86_EBP, X86_ESP, 4);
	x86_alu_reg_imm (p, X86_SUB, X86_ESP, local_size);

	/*
	 * Initialize MonoInvocation fields, first the ones known now.
	 */
	x86_mov_reg_imm (p, X86_EAX, 0);
	x86_mov_membase_reg (p, X86_EBP, (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, ex)), X86_EAX, 4);
	x86_mov_membase_reg (p, X86_EBP, (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, ex_handler)), X86_EAX, 4);
	x86_mov_membase_reg (p, X86_EBP, (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, child)), X86_EAX, 4);
	x86_mov_membase_reg (p, X86_EBP, (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, parent)), X86_EAX, 4);
	/*
	 * Set the method pointer.
	 */
	x86_mov_membase_imm (p, X86_EBP, (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, method)), (int)method, 4);

	/*
	 * Handle this.
	 */
	if (sig->hasthis) {
		if (sig->call_convention != MONO_CALL_THISCALL) {
			/*
			 * Grab it from the stack, otherwise it's already in ECX.
			 */
			x86_mov_reg_membase (p, X86_ECX, X86_EBP, OBJ_POS, 4);
			arg_pos += 4;
		}
		x86_mov_membase_reg (p, X86_EBP, (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, obj)), X86_ECX, 4);
	}
	/*
	 * Handle the arguments. stackval_pos is the posset of the stackval array from EBP.
	 * arg_pos is the offset from EBP to the incoming arg on the stack.
	 * We just call stackval_from_data to handle all the (nasty) issues....
	 */
	for (i = 0; i < sig->param_count; ++i) {
		x86_mov_reg_imm (p, X86_ECX, stackval_from_data);
		x86_lea_membase (p, X86_EDX, X86_EBP, arg_pos);
		x86_lea_membase (p, X86_EAX, X86_EBP, stackval_pos);
		x86_push_reg (p, X86_EDX);
		x86_push_reg (p, X86_EAX);
		x86_push_imm (p, sig->params [i]);
		x86_call_reg (p, X86_ECX);
		x86_alu_reg_imm (p, X86_SUB, X86_ESP, 12);
		stackval_pos += sizeof (stackval);
		arg_pos += 4;
		if (!sig->params [i]->byref) {
			switch (sig->params [i]->type) {
			case MONO_TYPE_I8:
			case MONO_TYPE_R8:
				arg_pos += 4;
				break;
			case MONO_TYPE_VALUETYPE:
				g_assert_not_reached (); /* Not implemented yet. */
			default:
				break;
			}
		}
	}

	/*
	 * Handle the return value storage area.
	 */
	x86_lea_membase (p, X86_EAX, X86_EBP, stackval_pos);
	x86_mov_membase_reg (p, X86_EBP, (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, retval)), X86_EAX, 4);

	/*
	 * Call the method.
	 */
	x86_lea_membase (p, X86_EAX, X86_EBP, MINV_POS);
	x86_push_reg (p, X86_EAX);
	x86_mov_reg_imm (p, X86_EDX, ves_exec_method);
	x86_call_reg (p, X86_EDX);

	/*
	 * Move the return value to the proper place.
	 */
	x86_lea_membase (p, X86_EAX, X86_EBP, stackval_pos);
	if (sig->ret->byref) {
		x86_mov_reg_membase (p, X86_EAX, X86_EAX, 0, 4);
	} else {
		switch (sig->ret->type) {
		case MONO_TYPE_VOID:
			break;
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_STRING:
			x86_mov_reg_membase (p, X86_EAX, X86_EAX, 0, 4);
			break;
		case MONO_TYPE_I8:
			x86_mov_reg_membase (p, X86_EDX, X86_EAX, 4, 4);
			x86_mov_reg_membase (p, X86_EAX, X86_EAX, 0, 4);
			break;
		case MONO_TYPE_R8:
			x86_fld_membase (p, X86_EAX, 0, TRUE);
			break;
		default:
			g_error ("Type 0x%x not handled yet in thunk creation", sig->ret->type);
			break;
		}
	}
	
	/*
	 * Standard epilog.
	 */
	x86_leave (p);
	x86_ret (p);

	return g_memdup (code_buffer, p - code_buffer);
}


