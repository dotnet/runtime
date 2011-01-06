/*
 * Create trampolines to invoke arbitrary functions.
 * 
 * Copyright (C) Ximian Inc.
 * 
 * Author: 
 *   Zalman Stern
 * Based on code by:
 *   Paolo Molaro (lupus@ximian.com)
 *   Dietmar Maurer (dietmar@ximian.com)
 * 
 * To understand this code, one will want to the calling convention section of the ABI sepc at:
 *     http://x86-64.org/abi.pdf
 * and the AMD64 architecture docs found at amd.com .
 */

#include "config.h"
#include <stdlib.h>
#include <string.h>
#include "amd64-codegen.h"
#include "mono/metadata/class.h"
#include "mono/metadata/tabledefs.h"
#include "mono/interpreter/interp.h"
#include "mono/metadata/appdomain.h"
#include "mono/metadata/marshal.h"

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

#define MAX_INT_ARG_REGS	6
#define MAX_FLOAT_ARG_REGS	8

// TODO get these right. They are upper bounds anyway, so it doesn't much matter.
#define PUSH_INT_STACK_ARG_SIZE		16
#define MOVE_INT_REG_ARG_SIZE		16
#define PUSH_FLOAT_STACK_ARG_SIZE	16
#define MOVE_FLOAT_REG_ARG_SIZE		16
#define COPY_STRUCT_STACK_ARG_SIZE	16

/* Maps an argument number (starting at 0) to the register it is passed in (if it fits).
 * E.g. int foo(int bar, int quux) has the foo arg in RDI and the quux arg in RSI
 * There is no such map for floating point args as they go in XMM0-XMM7 in order and thus the
 * index is the register number.
 */
static int int_arg_regs[] = { AMD64_RDI, AMD64_RSI, AMD64_RDX, AMD64_RCX, AMD64_R8, AMD64_R9 };

/* This next block of code resolves the ABI rules for passing structures in the argument registers.
 * These basically amount to "Use up to two registers if they are all integer or all floating point.
 * If the structure is bigger than two registers or would be in one integer register and one floating point,
 * it is passed in memory instead.
 *
 * It is possible this code needs to be recursive to be correct in the case when one of the structure members
 * is itself a structure.
 *
 * The 80-bit floating point stuff is ignored.
 */
typedef enum {
	ARG_IN_MEMORY,
	ARG_IN_INT_REGS,
	ARG_IN_FLOAT_REGS
} struct_arg_type;

static struct_arg_type compute_arg_type(MonoType *type)
{
	guint32 simpletype = type->type;

	switch (simpletype) {
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
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_STRING:
		case MONO_TYPE_I8:
			return ARG_IN_INT_REGS;
			break;
		case MONO_TYPE_VALUETYPE: {
			if (type->data.klass->enumtype)
				return ARG_IN_INT_REGS;
 			return ARG_IN_MEMORY;
			break;
		}
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
 			return ARG_IN_FLOAT_REGS;
			break;
		default:
			g_error ("Can't trampoline 0x%x", type->type);
	}

	return ARG_IN_MEMORY;
}

static struct_arg_type value_type_info(MonoClass *klass, int *native_size, int *regs_used, int *offset1, int *size1, int *offset2, int *size2)
{
	MonoMarshalType *info = mono_marshal_load_type_info (klass);

	*native_size = info->native_size;

	if (info->native_size > 8 || info->num_fields > 2)
	{
		*regs_used = 0;
		*offset1 = -1;
		*offset2 = -1;
		return ARG_IN_MEMORY;
	}

	if (info->num_fields == 1)
	{
		struct_arg_type result = compute_arg_type(info->fields[0].field->type);
		if (result != ARG_IN_MEMORY)
		{
			*regs_used = 1;
			*offset1 = info->fields[0].offset;
			*size1 = mono_marshal_type_size (info->fields[0].field->type, info->fields[0].mspec, NULL, 1, 1);
		} 
		else
		{
			*regs_used = 0;
			*offset1 = -1;
		}

		*offset2 = -1;
		return result;
	}

	struct_arg_type result1 = compute_arg_type(info->fields[0].field->type);
	struct_arg_type result2 = compute_arg_type(info->fields[0].field->type);

	if (result1 == result2 && result1 != ARG_IN_MEMORY)
	{
		*regs_used = 2;
		*offset1 = info->fields[0].offset;
		*size1 = mono_marshal_type_size (info->fields[0].field->type, info->fields[0].mspec, NULL, 1, 1);
		*offset2 = info->fields[1].offset;
		*size2 = mono_marshal_type_size (info->fields[1].field->type, info->fields[1].mspec, NULL, 1, 1);
		return result1;
	}

	return ARG_IN_MEMORY;
}

MonoPIFunc
mono_arch_create_trampoline (MonoMethodSignature *sig, gboolean string_ctor)
{
	unsigned char *p, *code_buffer;
	guint32 stack_size = 0, code_size = 50;
	guint32 arg_pos, simpletype;
	int i;
	static GHashTable *cache = NULL;
	MonoPIFunc res;

	guint32 int_arg_regs_used = 0;
	guint32 float_arg_regs_used = 0;
	guint32 next_int_arg_reg = 0;
	guint32 next_float_arg_reg = 0;
	/* Indicates that the return value is filled in inside the called function. */
	int retval_implicit = 0;
	char *arg_in_reg_bitvector; /* A set index by argument number saying if it is in a register
				       (integer or floating point according to type) */

	if (!cache) 
		cache = g_hash_table_new ((GHashFunc)mono_signature_hash, 
					  (GCompareFunc)mono_metadata_signature_equal);

	if ((res = (MonoPIFunc)g_hash_table_lookup (cache, sig)))
		return res;

	if (sig->ret->type == MONO_TYPE_VALUETYPE && !sig->ret->byref && !sig->ret->data.klass->enumtype) {
		int_arg_regs_used++;
		code_size += MOVE_INT_REG_ARG_SIZE;
	}

	if (sig->hasthis) {
		int_arg_regs_used++;
		code_size += MOVE_INT_REG_ARG_SIZE;
	}
	
	/* Run through stuff to calculate code size and argument bytes that will be pushed on stack (stack_size). */
	for (i = 0; i < sig->param_count; ++i) {
		if (sig->params [i]->byref)
			simpletype = MONO_TYPE_PTR;
		else
			simpletype = sig->params [i]->type;
enum_calc_size:
		switch (simpletype) {
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
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_STRING:
		case MONO_TYPE_I8:
			if (int_arg_regs_used++ > MAX_INT_ARG_REGS) {
				stack_size += 8;
				code_size += PUSH_INT_STACK_ARG_SIZE;
			}
			else
				code_size += MOVE_INT_REG_ARG_SIZE;
			break;
		case MONO_TYPE_VALUETYPE: {
			int size;
			int arg_type;
			int regs_used;
			int offset1;
			int size1;
			int offset2;
			int size2;

			if (sig->params [i]->data.klass->enumtype) {
				simpletype = sig->params [i]->data.klass->enum_basetype->type;
				goto enum_calc_size;
			}

			arg_type = value_type_info(sig->params [i]->data.klass, &size, &regs_used, &offset1, &size1, &offset2, &size2);
			if (arg_type == ARG_IN_INT_REGS &&
			    (int_arg_regs_used + regs_used) <= MAX_INT_ARG_REGS)
			{
				code_size += MOVE_INT_REG_ARG_SIZE;
				int_arg_regs_used += regs_used;
				break;
			}

			if (arg_type == ARG_IN_FLOAT_REGS &&
			    (float_arg_regs_used + regs_used) <= MAX_FLOAT_ARG_REGS)
			{
				code_size += MOVE_FLOAT_REG_ARG_SIZE;
				float_arg_regs_used += regs_used;
				break;
			}

			/* Else item is in memory. */

			stack_size += size + 7;
			stack_size &= ~7;
			code_size += COPY_STRUCT_STACK_ARG_SIZE;

			break;
		}
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
			if (float_arg_regs_used++ > MAX_FLOAT_ARG_REGS) {
				stack_size += 8;
				code_size += PUSH_FLOAT_STACK_ARG_SIZE;
			}
			else
				code_size += MOVE_FLOAT_REG_ARG_SIZE;
			break;
		default:
			g_error ("Can't trampoline 0x%x", sig->params [i]->type);
		}
	}
	/*
	 * FIXME: take into account large return values.
	 * (Comment carried over from IA32 code. Not sure what it means :-)
 	 */

	code_buffer = p = alloca (code_size);

	/*
	 * Standard function prolog.
	 */
	amd64_push_reg (p, AMD64_RBP);
	amd64_mov_reg_reg (p, AMD64_RBP, AMD64_RSP, 8);
	/*
	 * and align to 16 byte boundary...
	 */

	if (sig->ret->type == MONO_TYPE_VALUETYPE && !sig->ret->byref) {
		MonoClass *klass = sig->ret->data.klass;
		if (!klass->enumtype) {
			retval_implicit = 1;
		}
	}

	if (sig->ret->byref || string_ctor || !(retval_implicit || sig->ret->type == MONO_TYPE_VOID)) {
		/* Push the retval register so it is saved across the call. It will be addressed via RBP later. */
		amd64_push_reg (p, AMD64_RSI);
		stack_size += 8;
	}

	/* Ensure stack is 16 byte aligned when entering called function as required by calling convention. 
	 * Getting this wrong results in a general protection fault on an SSE load or store somewhere in the
	 * code called under the trampoline.
	 */
	if ((stack_size & 15) != 0)
		amd64_alu_reg_imm (p, X86_SUB, AMD64_RSP, 16 - (stack_size & 15));

	/*
	 * On entry to generated function:
	 *     RDI has target function address
	 *     RSI has return value location address
	 *     RDX has this pointer address
	 *     RCX has the pointer to the args array.
	 *
	 * Inside the stub function:
	 *     R10 holds the pointer to the args 
	 *     R11 holds the target function address.
	 *     The return value address is pushed on the stack.
	 *     The this pointer is moved into the first arg register at the start.
	 *
	 * Optimization note: we could keep the args pointer in RCX and then
	 * load over itself at the end. Ditto the callee addres could be left in RDI in some cases.
	 */

	/* Move args pointer to temp register. */
	amd64_mov_reg_reg (p, AMD64_R10, AMD64_RCX, 8);
	amd64_mov_reg_reg (p, AMD64_R11, AMD64_RDI, 8);

	/* First args register gets return value pointer, if need be.
         * Note that "byref" equal true means the called function returns a pointer.
         */
	if (retval_implicit) {
		amd64_mov_reg_reg (p, int_arg_regs[next_int_arg_reg], AMD64_RSI, 8);
		next_int_arg_reg++;
	}

	/* this pointer goes in next args register. */
	if (sig->hasthis) {
		amd64_mov_reg_reg (p, int_arg_regs[next_int_arg_reg], AMD64_RDX, 8);
		next_int_arg_reg++;
	}

	/*
	 * Generate code to handle arguments in registers. Stack arguments will happen in a loop after this.
	 */
	arg_in_reg_bitvector = (char *)alloca((sig->param_count + 7) / 8);
	memset(arg_in_reg_bitvector, 0, (sig->param_count + 7) / 8);

	/* First, load all the arguments that are passed in registers into the appropriate registers.
	 * Below there is another loop to handle arguments passed on the stack.
	 */
	for (i = 0; i < sig->param_count; i++) {
		arg_pos = ARG_SIZE * i;

		if (sig->params [i]->byref)
			simpletype = MONO_TYPE_PTR;
		else
			simpletype = sig->params [i]->type;
enum_marshal:
		switch (simpletype) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_PTR:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_STRING:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
		case MONO_TYPE_CLASS:
			if (next_int_arg_reg < MAX_INT_ARG_REGS) {
				amd64_mov_reg_membase (p, int_arg_regs[next_int_arg_reg], AMD64_R10, arg_pos, 8);
				next_int_arg_reg++;
				arg_in_reg_bitvector[i >> 3] |= (1 << (i & 7));
			}
			break;
		case MONO_TYPE_R4:
			if (next_float_arg_reg < MAX_FLOAT_ARG_REGS) {
				amd64_movss_reg_membase (p, next_float_arg_reg, AMD64_R10, arg_pos);
				next_float_arg_reg++;
				arg_in_reg_bitvector[i >> 3] |= (1 << (i & 7));
			}
			break;
		case MONO_TYPE_R8:
			if (next_float_arg_reg < MAX_FLOAT_ARG_REGS) {
				amd64_movsd_reg_membase (p, next_float_arg_reg, AMD64_R10, arg_pos);
				next_float_arg_reg++;
				arg_in_reg_bitvector[i >> 3] |= (1 << (i & 7));
			}
			break;
		case MONO_TYPE_VALUETYPE: {
			if (!sig->params [i]->data.klass->enumtype) {
				int size;
				int arg_type;
				int regs_used;
				int offset1;
				int size1;
				int offset2;
				int size2;

				arg_type = value_type_info(sig->params [i]->data.klass, &size, &regs_used, &offset1, &size1, &offset2, &size2);

				if (arg_type == ARG_IN_INT_REGS &&
				    (next_int_arg_reg + regs_used) <= MAX_INT_ARG_REGS)
				{
					amd64_mov_reg_membase (p, int_arg_regs[next_int_arg_reg], AMD64_R10, arg_pos + offset1, size1);
					next_int_arg_reg++;
					if (regs_used > 1)
					{
						amd64_mov_reg_membase (p, int_arg_regs[next_int_arg_reg], AMD64_R10, arg_pos + offset2, size2);
						next_int_arg_reg++;
					}
					arg_in_reg_bitvector[i >> 3] |= (1 << (i & 7));
					break;
				}

				if (arg_type == ARG_IN_FLOAT_REGS &&
				    (next_float_arg_reg + regs_used) <= MAX_FLOAT_ARG_REGS)
				{
					if (size1 == 4)
						amd64_movss_reg_membase (p, next_float_arg_reg, AMD64_R10, arg_pos + offset1);
					else
						amd64_movsd_reg_membase (p, next_float_arg_reg, AMD64_R10, arg_pos + offset1);
					next_float_arg_reg++;

					if (regs_used > 1)
					{
						if (size2 == 4)
							amd64_movss_reg_membase (p, next_float_arg_reg, AMD64_R10, arg_pos + offset2);
						else
							amd64_movsd_reg_membase (p, next_float_arg_reg, AMD64_R10, arg_pos + offset2);
						next_float_arg_reg++;
					}
					arg_in_reg_bitvector[i >> 3] |= (1 << (i & 7));
					break;
				}

				/* Structs in memory are handled in the next loop. */
			} else {
				/* it's an enum value */
				simpletype = sig->params [i]->data.klass->enum_basetype->type;
				goto enum_marshal;
			}
			break;
		}
		default:
			g_error ("Can't trampoline 0x%x", sig->params [i]->type);
		}
	}

	/* Handle stack arguments, pushing the rightmost argument first. */
	for (i = sig->param_count; i > 0; --i) {
		arg_pos = ARG_SIZE * (i - 1);
		if (sig->params [i - 1]->byref)
			simpletype = MONO_TYPE_PTR;
		else
			simpletype = sig->params [i - 1]->type;
enum_marshal2:
		switch (simpletype) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_PTR:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_STRING:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
		case MONO_TYPE_CLASS:
			if ((arg_in_reg_bitvector[(i - 1) >> 3] & (1 << ((i - 1) & 7))) == 0) {
				amd64_push_membase (p, AMD64_R10, arg_pos);
			}
			break;
		case MONO_TYPE_R4:
			if ((arg_in_reg_bitvector[(i - 1) >> 3] & (1 << ((i - 1) & 7))) == 0) {
				amd64_push_membase (p, AMD64_R10, arg_pos);
			}
			break;
		case MONO_TYPE_R8:
			if ((arg_in_reg_bitvector[(i - 1) >> 3] & (1 << ((i - 1) & 7))) == 0) {
				amd64_push_membase (p, AMD64_R10, arg_pos);
			}
			break;
		case MONO_TYPE_VALUETYPE:
			if (!sig->params [i - 1]->data.klass->enumtype) {
				if ((arg_in_reg_bitvector[(i - 1) >> 3] & (1 << ((i - 1) & 7))) == 0)
				{
					int ss = mono_class_native_size (sig->params [i - 1]->data.klass, NULL);
					ss += 7;
					ss &= ~7;

 					amd64_alu_reg_imm(p, X86_SUB, AMD64_RSP, ss);
					/* Count register */
					amd64_mov_reg_imm(p, AMD64_RCX, ss);
					/* Source register */
					amd64_lea_membase(p, AMD64_RSI, AMD64_R10, arg_pos);
					/* Dest register */
					amd64_mov_reg_reg(p, AMD64_RDI, AMD64_RSP, 8);

					/* AMD64 calling convention guarantees direction flag is clear at call boundary. */
					x86_prefix(p, AMD64_REX(AMD64_REX_W));
					x86_prefix(p, X86_REP_PREFIX);
					x86_movsb(p);
				}
			} else {
				/* it's an enum value */
				simpletype = sig->params [i - 1]->data.klass->enum_basetype->type;
				goto enum_marshal2;
			}
			break;
		default:
			g_error ("Can't trampoline 0x%x", sig->params [i - 1]->type);
		}
	}

        /* TODO: Set RAL to number of XMM registers used in case this is a varags function? */
 
	/* 
	 * Insert call to function 
	 */
	amd64_call_reg (p, AMD64_R11);

	if (sig->ret->byref || string_ctor || !(retval_implicit || sig->ret->type == MONO_TYPE_VOID)) {
		amd64_mov_reg_membase(p, AMD64_RSI, AMD64_RBP, -8, SIZEOF_VOID_P);
	}
	/*
	 * Handle retval.
	 * Small integer and pointer values are in EAX.
	 * Long integers are in EAX:EDX.
	 * FP values are on the FP stack.
	 */

	if (sig->ret->byref || string_ctor) {
		simpletype = MONO_TYPE_PTR;
	} else {
		simpletype = sig->ret->type;
	}
	enum_retvalue:
	switch (simpletype) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
			amd64_mov_regp_reg (p, AMD64_RSI, X86_EAX, 1);
			break;
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
			amd64_mov_regp_reg (p, AMD64_RSI, X86_EAX, 2);
			break;
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_STRING: 
		case MONO_TYPE_PTR:
			amd64_mov_regp_reg (p, AMD64_RSI, X86_EAX, 8);
			break;
		case MONO_TYPE_R4:
			amd64_movss_regp_reg (p, AMD64_RSI, AMD64_XMM0);
			break;
		case MONO_TYPE_R8:
			amd64_movsd_regp_reg (p, AMD64_RSI, AMD64_XMM0);
			break;
		case MONO_TYPE_I8:
			amd64_mov_regp_reg (p, AMD64_RSI, X86_EAX, 8);
			break;
		case MONO_TYPE_VALUETYPE: {
			int size;
			int arg_type;
			int regs_used;
			int offset1;
			int size1;
			int offset2;
			int size2;

			if (sig->ret->data.klass->enumtype) {
				simpletype = sig->ret->data.klass->enum_basetype->type;
				goto enum_retvalue;
			}

			arg_type = value_type_info(sig->params [i]->data.klass, &size, &regs_used, &offset1, &size1, &offset2, &size2);

			if (arg_type == ARG_IN_INT_REGS)
			{
				amd64_mov_membase_reg (p, AMD64_RSI, offset1, AMD64_RAX, size1);
				if (regs_used > 1)
					amd64_mov_membase_reg (p, AMD64_RSI, offset2, AMD64_RDX, size2);
				break;
			}

			if (arg_type == ARG_IN_FLOAT_REGS)
			{
				if (size1 == 4)
					amd64_movss_membase_reg (p, AMD64_RSI, offset1, AMD64_XMM0);
				else
					amd64_movsd_membase_reg (p, AMD64_RSI, offset1, AMD64_XMM0);

				if (regs_used > 1)
				{
					if (size2 == 4)
						amd64_movss_membase_reg (p, AMD64_RSI, offset2, AMD64_XMM1);
					else
						amd64_movsd_membase_reg (p, AMD64_RSI, offset2, AMD64_XMM1);
				}
				break;
			}

			/* Else result should have been stored in place already. */
			break;
		}
		case MONO_TYPE_VOID:
			break;
		default:
			g_error ("Can't handle as return value 0x%x", sig->ret->type);
	}

	/*
	 * Standard epilog.
	 */
	amd64_leave (p);
	amd64_ret (p);

	g_assert (p - code_buffer < code_size);
	res = (MonoPIFunc)g_memdup (code_buffer, p - code_buffer);

	g_hash_table_insert (cache, sig, res);

	return res;
}

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
mono_arch_create_method_pointer (MonoMethod *method)
{
	MonoMethodSignature *sig;
	MonoJitInfo *ji;
	unsigned char *p, *code_buffer;
	guint32 simpletype;
	gint32 local_size;
	gint32 stackval_pos;
	gint32 mono_invocation_pos;
	int i, cpos;
	int *vtbuf;
	int *rbpoffsets;
	int int_arg_regs_used = 0;
	int float_arg_regs_used = 0;
	int stacked_args_size = 0; /* bytes of register passed arguments pushed on stack for safe keeping. Used to get alignment right. */
	int next_stack_arg_rbp_offset = 16;
	int retval_ptr_rbp_offset = 0;
	int this_reg = -1; /* Remember register this ptr is in. */

	/*
	 * If it is a static P/Invoke method, we can just return the pointer
	 * to the method implementation.
	 */
	if (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL && ((MonoMethodPInvoke*) method)->addr) {
		ji = g_new0 (MonoJitInfo, 1);
		ji->method = method;
		ji->code_size = 1;
		ji->code_start = ((MonoMethodPInvoke*) method)->addr;

		mono_jit_info_table_add (mono_get_root_domain (), ji);
		return ((MonoMethodPInvoke*) method)->addr;
	}

	sig = method->signature;

	code_buffer = p = alloca (512); /* FIXME: check for overflows... */
	vtbuf = alloca (sizeof(int)*sig->param_count);
	rbpoffsets = alloca (sizeof(int)*sig->param_count);


	/*
	 * Standard function prolog.
	 */
	amd64_push_reg (p, AMD64_RBP);
	amd64_mov_reg_reg (p, AMD64_RBP, AMD64_RSP, 8);

	/* If there is an implicit return value pointer in the first args reg, save it now so
	 * the result can be stored through the pointer at the end.
	 */
	if (sig->ret->type == MONO_TYPE_VALUETYPE && !sig->ret->byref && !sig->ret->data.klass->enumtype) 
	{
		amd64_push_reg (p, int_arg_regs[int_arg_regs_used]);
		int_arg_regs_used++;
		stacked_args_size += 8;
		retval_ptr_rbp_offset = -stacked_args_size;
	}

	/*
	 * If there is a this pointer, remember the number of the register it is in.
	 */
	if (sig->hasthis) {
		this_reg = int_arg_regs[int_arg_regs_used++];
	}

	/* Put all arguments passed in registers on the stack.
	 * Record offsets from RBP to each argument.
	 */
	cpos = 0;

	for (i = 0; i < sig->param_count; i++) {
		if (sig->params [i]->byref)
			simpletype = MONO_TYPE_PTR;
		else
			simpletype = sig->params [i]->type;
enum_calc_size:
		switch (simpletype) {
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
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_STRING:
		case MONO_TYPE_I8:
			if (int_arg_regs_used < MAX_INT_ARG_REGS) {
				amd64_push_reg (p, int_arg_regs[int_arg_regs_used]);
				int_arg_regs_used++;
				stacked_args_size += 8;
				rbpoffsets[i] = -stacked_args_size;
			}
			else
			{
				rbpoffsets[i] = next_stack_arg_rbp_offset;
				next_stack_arg_rbp_offset += 8;
			}
			break;
		case MONO_TYPE_VALUETYPE: {
			if (sig->params [i]->data.klass->enumtype) {
				simpletype = sig->params [i]->data.klass->enum_basetype->type;
				goto enum_calc_size;
			}
			else
			{
				int size;
				int arg_type;
				int regs_used;
				int offset1;
				int size1;
				int offset2;
				int size2;

				arg_type = value_type_info(sig->params [i]->data.klass, &size, &regs_used, &offset1, &size1, &offset2, &size2);

				if (arg_type == ARG_IN_INT_REGS &&
				    (int_arg_regs_used + regs_used) <= MAX_INT_ARG_REGS)
				{
					amd64_alu_reg_imm (p, X86_SUB, AMD64_RSP, size);
					stacked_args_size += size;
					rbpoffsets[i] = stacked_args_size;

					amd64_mov_reg_membase (p, int_arg_regs[int_arg_regs_used], AMD64_RSP, offset1, size1);
					int_arg_regs_used++;
					if (regs_used > 1)
					{
						amd64_mov_reg_membase (p, int_arg_regs[int_arg_regs_used], AMD64_RSP, offset2, size2);
						int_arg_regs_used++;
					}
					break;
				}

				if (arg_type == ARG_IN_FLOAT_REGS &&
				    (float_arg_regs_used + regs_used) <= MAX_FLOAT_ARG_REGS)
				{
					amd64_alu_reg_imm (p, X86_SUB, AMD64_RSP, size);
					stacked_args_size += size;
					rbpoffsets[i] = stacked_args_size;

					if (size1 == 4)
						amd64_movss_reg_membase (p, float_arg_regs_used, AMD64_RSP, offset1);
					else
						amd64_movsd_reg_membase (p, float_arg_regs_used, AMD64_RSP, offset1);
					float_arg_regs_used++;

					if (regs_used > 1)
					{
						if (size2 == 4)
							amd64_movss_reg_membase (p, float_arg_regs_used, AMD64_RSP, offset2);
						else
							amd64_movsd_reg_membase (p, float_arg_regs_used, AMD64_RSP, offset2);
						float_arg_regs_used++;
					}
					break;
				}

				rbpoffsets[i] = next_stack_arg_rbp_offset;
				next_stack_arg_rbp_offset += size;
			}
			break;
		}
		case MONO_TYPE_R4:
			if (float_arg_regs_used < MAX_FLOAT_ARG_REGS) {
				amd64_alu_reg_imm (p, X86_SUB, AMD64_RSP, 8);
				amd64_movss_regp_reg (p, AMD64_RSP, float_arg_regs_used);
				float_arg_regs_used++;
				stacked_args_size += 8;
				rbpoffsets[i] = -stacked_args_size;
			}
			else
			{
				rbpoffsets[i] = next_stack_arg_rbp_offset;
				next_stack_arg_rbp_offset += 8;
			}
			break;
		case MONO_TYPE_R8:
			stacked_args_size += 8;
			if (float_arg_regs_used < MAX_FLOAT_ARG_REGS) {
				amd64_alu_reg_imm (p, X86_SUB, AMD64_RSP, 8);
				amd64_movsd_regp_reg (p, AMD64_RSP, float_arg_regs_used);
				float_arg_regs_used++;
				stacked_args_size += 8;
				rbpoffsets[i] = -stacked_args_size;
			}
			else
			{
				rbpoffsets[i] = next_stack_arg_rbp_offset;
				next_stack_arg_rbp_offset += 8;
			}
			break;
		default:
			g_error ("Can't trampoline 0x%x", sig->params [i]->type);
		}
	}

	local_size = sizeof (MonoInvocation) + sizeof (stackval) * (sig->param_count + 1) + stacked_args_size;

	local_size += 15;
	local_size &= ~15;

	stackval_pos = -local_size;
	mono_invocation_pos = stackval_pos + sizeof (stackval) * (sig->param_count + 1);

	/* stacked_args_size has already been pushed onto the stack. Make room for the rest of it. */
	amd64_alu_reg_imm (p, X86_SUB, AMD64_RSP, local_size - stacked_args_size);

	/* Be careful not to trash any arg regs before saving this_reg to MonoInvocation structure below. */

	/*
	 * Initialize MonoInvocation fields, first the ones known now.
	 */
	amd64_alu_reg_reg (p, X86_XOR, AMD64_RAX, AMD64_RAX);
	amd64_mov_membase_reg (p, AMD64_RBP, (mono_invocation_pos + G_STRUCT_OFFSET (MonoInvocation, ex)), AMD64_RAX, SIZEOF_VOID_P);
	amd64_mov_membase_reg (p, AMD64_RBP, (mono_invocation_pos + G_STRUCT_OFFSET (MonoInvocation, ex_handler)), AMD64_RAX, SIZEOF_VOID_P);
	amd64_mov_membase_reg (p, AMD64_RBP, (mono_invocation_pos + G_STRUCT_OFFSET (MonoInvocation, parent)), AMD64_RAX, SIZEOF_VOID_P);
	/*
	 * Set the method pointer.
	 */
	amd64_mov_membase_imm (p, AMD64_RBP, (mono_invocation_pos + G_STRUCT_OFFSET (MonoInvocation, method)), (long)method, SIZEOF_VOID_P);

	/*
	 * Handle this.
	 */
	if (sig->hasthis)
		amd64_mov_membase_reg(p, AMD64_RBP, (mono_invocation_pos + G_STRUCT_OFFSET (MonoInvocation, obj)), this_reg, SIZEOF_VOID_P);

	/*
	 * Handle the arguments. stackval_pos is the offset from RBP of the stackval in the MonoInvocation args array .
	 * arg_pos is the offset from RBP to the incoming arg on the stack.
	 * We just call stackval_from_data to handle all the (nasty) issues....
	 */
	amd64_lea_membase (p, AMD64_RAX, AMD64_RBP, stackval_pos);
	amd64_mov_membase_reg (p, AMD64_RBP, (mono_invocation_pos + G_STRUCT_OFFSET (MonoInvocation, stack_args)), AMD64_RAX, SIZEOF_VOID_P);
	for (i = 0; i < sig->param_count; ++i) {
/* Need to call stackval_from_data (MonoType *type, stackval *result, char *data, gboolean pinvoke); */
		amd64_mov_reg_imm (p, AMD64_R11, stackval_from_data);
		amd64_mov_reg_imm (p, int_arg_regs[0], sig->params[i]);
		amd64_lea_membase (p, int_arg_regs[1], AMD64_RBP, stackval_pos);
		amd64_lea_membase (p, int_arg_regs[2], AMD64_RBP, rbpoffsets[i]);
		amd64_mov_reg_imm (p, int_arg_regs[3], sig->pinvoke);
		amd64_call_reg (p, AMD64_R11);
		stackval_pos += sizeof (stackval);
#if 0
		/* fixme: alignment */
		if (sig->pinvoke)
			arg_pos += mono_type_native_stack_size (sig->params [i], &align);
		else
			arg_pos += mono_type_stack_size (sig->params [i], &align);
#endif
	}

	/*
	 * Handle the return value storage area.
	 */
	amd64_lea_membase (p, AMD64_RAX, AMD64_RBP, stackval_pos);
	amd64_mov_membase_reg (p, AMD64_RBP, (mono_invocation_pos + G_STRUCT_OFFSET (MonoInvocation, retval)), AMD64_RAX, SIZEOF_VOID_P);
	if (sig->ret->type == MONO_TYPE_VALUETYPE && !sig->ret->byref) {
		MonoClass *klass  = sig->ret->data.klass;
		if (!klass->enumtype) {
			amd64_mov_reg_membase (p, AMD64_RCX, AMD64_RBP, retval_ptr_rbp_offset, SIZEOF_VOID_P);
			amd64_mov_membase_reg (p, AMD64_RBP, stackval_pos, AMD64_RCX, SIZEOF_VOID_P);
		}
	}

	/*
	 * Call the method.
	 */
	amd64_lea_membase (p, int_arg_regs[0], AMD64_RBP, mono_invocation_pos);
	amd64_mov_reg_imm (p, AMD64_R11, ves_exec_method);
	amd64_call_reg (p, AMD64_R11);
	
	/*
	 * Move the return value to the proper place.
	 */
	amd64_lea_membase (p, AMD64_RAX, AMD64_RBP, stackval_pos);
	if (sig->ret->byref) {
		amd64_mov_reg_membase (p, AMD64_RAX, AMD64_RAX, 0, SIZEOF_VOID_P);
	} else {
		int simpletype = sig->ret->type;	
	enum_retvalue:
		switch (sig->ret->type) {
		case MONO_TYPE_VOID:
			break;
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
			amd64_movzx_reg_membase (p, AMD64_RAX, AMD64_RAX, 0, 1);
			break;
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
			amd64_movzx_reg_membase (p, AMD64_RAX, AMD64_RAX, 0, 2);
			break;
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_STRING:
		case MONO_TYPE_CLASS:
			amd64_movzx_reg_membase (p, AMD64_RAX, AMD64_RAX, 0, 4);
			break;
		case MONO_TYPE_I8:
			amd64_movzx_reg_membase (p, AMD64_RAX, AMD64_RAX, 0, 8);
			break;
		case MONO_TYPE_R4:
			amd64_movss_regp_reg (p, AMD64_RAX, AMD64_XMM0);
			break;
		case MONO_TYPE_R8:
			amd64_movsd_regp_reg (p, AMD64_RAX, AMD64_XMM0);
			break;
		case MONO_TYPE_VALUETYPE: {
			int size;
			int arg_type;
			int regs_used;
			int offset1;
			int size1;
			int offset2;
			int size2;

			if (sig->ret->data.klass->enumtype) {
				simpletype = sig->ret->data.klass->enum_basetype->type;
				goto enum_retvalue;
			}

			arg_type = value_type_info(sig->params [i]->data.klass, &size, &regs_used, &offset1, &size1, &offset2, &size2);

			if (arg_type == ARG_IN_INT_REGS)
			{
				if (regs_used > 1)
					amd64_mov_membase_reg (p, AMD64_RAX, offset2, AMD64_RDX, size2);
				amd64_mov_membase_reg (p, AMD64_RAX, offset1, AMD64_RAX, size1);
				break;
			}

			if (arg_type == ARG_IN_FLOAT_REGS)
			{
				if (size1 == 4)
					amd64_movss_membase_reg (p, AMD64_RAX, offset1, AMD64_XMM0);
				else
					amd64_movsd_membase_reg (p, AMD64_RAX, offset1, AMD64_XMM0);

				if (regs_used > 1)
				{
					if (size2 == 4)
						amd64_movss_membase_reg (p, AMD64_RAX, offset2, AMD64_XMM1);
					else
						amd64_movsd_membase_reg (p, AMD64_RAX, offset2, AMD64_XMM1);
				}
				break;
			}

			/* Else result should have been stored in place already. IA32 code has a stackval_to_data call here, which
			 * looks wrong to me as the pointer in the stack val being converted is setup to point to the output area anyway.
			 * It all looks a bit suspect anyway.
			 */
			break;
		}
		default:
			g_error ("Type 0x%x not handled yet in thunk creation", sig->ret->type);
			break;
		}
	}
	
	/*
	 * Standard epilog.
	 */
	amd64_leave (p);
	amd64_ret (p);

	g_assert (p - code_buffer < 512);

	ji = g_new0 (MonoJitInfo, 1);
	ji->method = method;
	ji->code_size = p - code_buffer;
	ji->code_start = g_memdup (code_buffer, p - code_buffer);

	mono_jit_info_table_add (mono_get_root_domain (), ji);

	return ji->code_start;
}
