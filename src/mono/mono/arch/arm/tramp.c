/*
 * Create trampolines to invoke arbitrary functions.
 * Copyright (c) 2002 Sergey Chaban <serge@wildwestsoftware.com>
 *
 * Contributions by Malte Hildingson
 */

#include "arm-codegen.h"
#include "arm-dis.h"

#if defined(_WIN32_WCE) || defined (UNDER_CE)
#	include <windows.h>
#else
#include <unistd.h>
#include <sys/mman.h>
#endif

#if !defined(PLATFORM_MACOSX)
#include <errno.h>

#include "mono/metadata/class.h"
#include "mono/metadata/tabledefs.h"
#include "mono/interpreter/interp.h"
#include "mono/metadata/appdomain.h"


#if 0
#	define ARM_DUMP_DISASM 1
#endif

/* prototypes for private functions (to avoid compiler warnings) */
void flush_icache (void);
void* alloc_code_buff (int num_instr);



/*
 * The resulting function takes the form:
 * void func (void (*callme)(), void *retval, void *this_obj, stackval *arguments);
 * NOTE: all args passed in ARM registers (A1-A4),
 *       then copied to R4-R7 (see definitions below).
 */

#define REG_FUNC_ADDR ARMREG_R4
#define REG_RETVAL    ARMREG_R5
#define REG_THIS      ARMREG_R6
#define REG_ARGP      ARMREG_R7


#define ARG_SIZE sizeof(stackval)




void flush_icache ()
{
#if defined(_WIN32)
	FlushInstructionCache(GetCurrentProcess(), NULL, 0);
#else
# if 0
	asm ("mov r0, r0");
	asm ("mov r0, #0");
	asm ("mcr p15, 0, r0, c7, c7, 0");
# else
	/* TODO: use (movnv  pc, rx) method */
# endif
#endif
}


void* alloc_code_buff (int num_instr)
{
	void* code_buff;
	int code_size = num_instr * sizeof(arminstr_t);

#if defined(_WIN32) || defined(UNDER_CE)
	int old_prot = 0;

	code_buff = malloc(code_size);
	VirtualProtect(code_buff, code_size, PAGE_EXECUTE_READWRITE, &old_prot);
#else
	int page_size = sysconf(_SC_PAGESIZE);
	int new_code_size;

	new_code_size = code_size + page_size - 1;
	code_buff = malloc(new_code_size);
	code_buff = (void *) (((int) code_buff + page_size - 1) & ~(page_size - 1));

	if (mprotect(code_buff, code_size, PROT_READ|PROT_WRITE|PROT_EXEC) != 0) {
		g_critical (G_GNUC_PRETTY_FUNCTION
				": mprotect error: %s", g_strerror (errno));
	}
#endif

	return code_buff;
}


/*
 * Refer to ARM Procedure Call Standard (APCS) for more info.
 */
MonoPIFunc mono_arch_create_trampoline (MonoMethodSignature *sig, gboolean string_ctor)
{
	MonoType* param;
	MonoPIFunc code_buff;
	arminstr_t* p;
	guint32 code_size, stack_size;
	guint32 simple_type;
	int i, hasthis, aregs, regc, stack_offs;
	int this_loaded;
	guchar reg_alloc [ARM_NUM_ARG_REGS];

	/* pessimistic estimation for prologue/epilogue size */
	code_size = 16 + 16;
	/* push/pop work regs */
	code_size += 2; 
	/* call */
	code_size += 2;
	/* handle retval */
	code_size += 2;

	stack_size = 0;
	hasthis = sig->hasthis ? 1 : 0;

	aregs = ARM_NUM_ARG_REGS - hasthis;

	for (i = 0, regc = aregs; i < sig->param_count; ++i) {
		param = sig->params [i];

		/* keep track of argument sizes */
		if (i < ARM_NUM_ARG_REGS) reg_alloc [i] = 0;

		if (param->byref) {
			if (regc > 0) {
				code_size += 1;
				reg_alloc [i] = regc;
				--regc;
			} else {
				code_size += 2;
				stack_size += sizeof(gpointer);
			}
		} else {
			simple_type = param->type;
enum_calc_size:
			switch (simple_type) {
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
			case MONO_TYPE_STRING:
				if (regc > 0) {
					/* register arg */
					code_size += 1;
					reg_alloc [i] = regc;
					--regc;
				} else {
					/* stack arg */
					code_size += 2;
					stack_size += 4;
				}
				break;
			case MONO_TYPE_I8:
			case MONO_TYPE_U8:
			case MONO_TYPE_R8:
				/* keep track of argument sizes */
				if (regc > 1) {
					/* fits into registers, two LDRs */
					code_size += 2;
					reg_alloc [i] = regc;
					regc -= 2;
				} else if (regc > 0) {
					/* first half fits into register, one LDR */
					code_size += 1;
					reg_alloc [i] = regc;
					--regc;
					/* the rest on the stack, LDR/STR */
					code_size += 2;
					stack_size += 4;
				} else {
					/* stack arg, 4 instrs - 2x(LDR/STR) */
					code_size += 4;
					stack_size += 2 * 4;
				}
				break;
			case MONO_TYPE_VALUETYPE:
				if (param->data.klass->enumtype) {
					simple_type = param->data.klass->enum_basetype->type;
					goto enum_calc_size;
				}

				if (mono_class_value_size(param->data.klass, NULL) != 4) {
					g_error("can only marshal enums, not generic structures (size: %d)", mono_class_value_size(param->data.klass, NULL));
				}
				if (regc > 0) {
					/* register arg */
					code_size += 1;
					reg_alloc [i] = regc;
					--regc;
				} else {
					/* stack arg */
					code_size += 2;
					stack_size += 4;
				}
				break;
			default :
				break;
			}
		}
	}

	code_buff = (MonoPIFunc)alloc_code_buff(code_size);
	p = (arminstr_t*)code_buff;

	/* prologue */
	p = arm_emit_lean_prologue(p, stack_size,
	        /* save workset (r4-r7) */
	        (1 << ARMREG_R4) | (1 << ARMREG_R5) | (1 << ARMREG_R6) | (1 << ARMREG_R7));


	/* copy args into workset */
	/* callme - always present */
	ARM_MOV_REG_REG(p, ARMREG_R4, ARMREG_A1);
	/* retval */
	if (sig->ret->byref || string_ctor || (sig->ret->type != MONO_TYPE_VOID)) {
		ARM_MOV_REG_REG(p, ARMREG_R5, ARMREG_A2);
	}
	/* this_obj */
	if (sig->hasthis) {
		this_loaded = 0;
		if (stack_size == 0) {
			ARM_MOV_REG_REG(p, ARMREG_A1, ARMREG_A3);
			this_loaded = 1;
		} else {
			ARM_MOV_REG_REG(p, ARMREG_R6, ARMREG_A3);
		}
	}
	/* args */
	if (sig->param_count != 0) {
		ARM_MOV_REG_REG(p, ARMREG_R7, ARMREG_A4);
	}

	stack_offs = stack_size;

	/* handle arguments */
	/* in reverse order so we could use r0 (arg1) for memory transfers */
	for (i = sig->param_count; --i >= 0;) {
		param = sig->params [i];
		if (param->byref) {
			if (i < aregs && reg_alloc[i] > 0) {
				ARM_LDR_IMM(p, ARMREG_A1 + i, REG_ARGP, i*ARG_SIZE);
			} else {
				stack_offs -= sizeof(armword_t);
				ARM_LDR_IMM(p, ARMREG_R0, REG_ARGP, i*ARG_SIZE);
				ARM_STR_IMM(p, ARMREG_R0, ARMREG_SP, stack_offs);
			}
		} else {
			simple_type = param->type;
enum_marshal:
			switch (simple_type) {
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
			case MONO_TYPE_STRING:
				if (i < aregs && reg_alloc [i] > 0) {
					/* pass in register */
					ARM_LDR_IMM(p, ARMREG_A1 + hasthis + (aregs - reg_alloc [i]), REG_ARGP, i*ARG_SIZE);
				} else {
					stack_offs -= sizeof(armword_t);
					ARM_LDR_IMM(p, ARMREG_R0, REG_ARGP, i*ARG_SIZE);
					ARM_STR_IMM(p, ARMREG_R0, ARMREG_SP, stack_offs);
				}
				break;
			case MONO_TYPE_I8:
			case MONO_TYPE_U8:
			case MONO_TYPE_R8:
				if (i < aregs && reg_alloc [i] > 0) {
					if (reg_alloc [i] > 1) {
						/* pass in registers */
						ARM_LDR_IMM(p, ARMREG_A1 + hasthis + (aregs - reg_alloc [i]), REG_ARGP, i*ARG_SIZE);
						ARM_LDR_IMM(p, ARMREG_A1 + hasthis + (aregs - reg_alloc [i]) + 1, REG_ARGP, i*ARG_SIZE + 4);
					} else {
						stack_offs -= sizeof(armword_t);
						ARM_LDR_IMM(p, ARMREG_R0, REG_ARGP, i*ARG_SIZE + 4);
						ARM_STR_IMM(p, ARMREG_R0, ARMREG_SP, stack_offs);
						ARM_LDR_IMM(p, ARMREG_A1 + hasthis + (aregs - reg_alloc [i]), REG_ARGP, i*ARG_SIZE);
					}
				} else {
					/* two words transferred on the stack */
					stack_offs -= 2*sizeof(armword_t);
					ARM_LDR_IMM(p, ARMREG_R0, REG_ARGP, i*ARG_SIZE);
					ARM_STR_IMM(p, ARMREG_R0, ARMREG_SP, stack_offs);
					ARM_LDR_IMM(p, ARMREG_R0, REG_ARGP, i*ARG_SIZE + 4);
					ARM_STR_IMM(p, ARMREG_R0, ARMREG_SP, stack_offs + 4);
				}
				break;
			case MONO_TYPE_VALUETYPE:
				if (param->data.klass->enumtype) {
					/* it's an enum value, proceed based on its base type */
					simple_type = param->data.klass->enum_basetype->type;
					goto enum_marshal;
				} else {
					if (i < aregs && reg_alloc[i] > 0) {
						int vtreg = ARMREG_A1 + hasthis +
								hasthis + (aregs - reg_alloc[i]);
						ARM_LDR_IMM(p, vtreg, REG_ARGP, i * ARG_SIZE);
						ARM_LDR_IMM(p, vtreg, vtreg, 0);
					} else {
						stack_offs -= sizeof(armword_t);
						ARM_LDR_IMM(p, ARMREG_R0, REG_ARGP, i * ARG_SIZE);
						ARM_LDR_IMM(p, ARMREG_R0, ARMREG_R0, 0);
						ARM_STR_IMM(p, ARMREG_R0, ARMREG_SP, stack_offs);
					}
				}
				break;

			default:
				break;
			}
		}
	}

	if (sig->hasthis && !this_loaded) {
		/* [this] always passed in A1, regardless of sig->call_convention */
		ARM_MOV_REG_REG(p, ARMREG_A1, REG_THIS);
	}

	/* call [func] */
	ARM_MOV_REG_REG(p, ARMREG_LR, ARMREG_PC);
	ARM_MOV_REG_REG(p, ARMREG_PC, REG_FUNC_ADDR);

	/* handle retval */
	if (sig->ret->byref || string_ctor) {
		ARM_STR_IMM(p, ARMREG_R0, REG_RETVAL, 0);
	} else {
		simple_type = sig->ret->type;
enum_retvalue:
		switch (simple_type) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
			ARM_STRB_IMM(p, ARMREG_R0, REG_RETVAL, 0);
			break;
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
			ARM_STRH_IMM(p, ARMREG_R0, REG_RETVAL, 0);
			break;
		/*
		 * A 32-bit integer and integer-equivalent return value
		 * is returned in R0.
		 * Single-precision floating-point values are returned in R0.
		 */
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_R4:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_STRING:
			ARM_STR_IMM(p, ARMREG_R0, REG_RETVAL, 0);
			break;
		/*
		 * A 64-bit integer is returned in R0 and R1.
		 * Double-precision floating-point values are returned in R0 and R1.
		 */
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
		case MONO_TYPE_R8:
			ARM_STR_IMM(p, ARMREG_R0, REG_RETVAL, 0);
			ARM_STR_IMM(p, ARMREG_R1, REG_RETVAL, 4);
			break;
		case MONO_TYPE_VALUETYPE:
			if (sig->ret->data.klass->enumtype) {
				simple_type = sig->ret->data.klass->enum_basetype->type;
				goto enum_retvalue;
			}
			break;
		case MONO_TYPE_VOID:
			break;
		default:
			break;
		}
	}
	
	p = arm_emit_std_epilogue(p, stack_size,
	        /* restore R4-R7 */
	        (1 << ARMREG_R4) | (1 << ARMREG_R5) | (1 << ARMREG_R6) | (1 << ARMREG_R7));

	flush_icache();

#ifdef ARM_DUMP_DISASM
	_armdis_decode((arminstr_t*)code_buff, ((guint8*)p) - ((guint8*)code_buff));
#endif

	return code_buff;
}



#define MINV_OFFS(member) G_STRUCT_OFFSET(MonoInvocation, member)



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
void* mono_arch_create_method_pointer (MonoMethod* method)
{
	MonoMethodSignature* sig;
	guchar* p, * p_method, * p_stackval_from_data, * p_exec;
	void* code_buff;
	int i, stack_size, arg_pos, arg_add, stackval_pos, offs;
	int areg, reg_args, shift, pos;
	MonoJitInfo *ji;

	code_buff = alloc_code_buff(128);
	p = (guchar*)code_buff;

	sig = method->signature;

	ARM_B(p, 3);

	/* embed magic number followed by method pointer */
	*p++ = 'M';
	*p++ = 'o';
	*p++ = 'n';
	*p++ = 'o';
	/* method ptr */
	*(void**)p = method;
	p_method = p;
	p += 4;

	/* call table */
	*(void**)p = stackval_from_data;
	p_stackval_from_data = p;
	p += 4;
	*(void**)p = ves_exec_method;
	p_exec = p;
	p += 4;

	stack_size = sizeof(MonoInvocation) + ARG_SIZE*(sig->param_count + 1) + ARM_NUM_ARG_REGS*2*sizeof(armword_t);

	/* prologue */
	p = (guchar*)arm_emit_lean_prologue((arminstr_t*)p, stack_size,
	    (1 << ARMREG_R4) |
	    (1 << ARMREG_R5) |
	    (1 << ARMREG_R6) |
	    (1 << ARMREG_R7));

	/* R7 - ptr to stack args */
	ARM_MOV_REG_REG(p, ARMREG_R7, ARMREG_IP);

	/*
	 * Initialize MonoInvocation fields, first the ones known now.
	 */
	ARM_MOV_REG_IMM8(p, ARMREG_R4, 0);
	ARM_STR_IMM(p, ARMREG_R4, ARMREG_SP, MINV_OFFS(ex));
	ARM_STR_IMM(p, ARMREG_R4, ARMREG_SP, MINV_OFFS(ex_handler));
	ARM_STR_IMM(p, ARMREG_R4, ARMREG_SP, MINV_OFFS(parent));

	/* Set the method pointer. */
	ARM_LDR_IMM(p, ARMREG_R4, ARMREG_PC, -(int)(p - p_method + sizeof(arminstr_t)*2));
	ARM_STR_IMM(p, ARMREG_R4, ARMREG_SP, MINV_OFFS(method));

	if (sig->hasthis) {
		/* [this] in A1 */
		ARM_STR_IMM(p, ARMREG_A1, ARMREG_SP, MINV_OFFS(obj));
	} else {
		/* else set minv.obj to NULL */
		ARM_STR_IMM(p, ARMREG_R4, ARMREG_SP, MINV_OFFS(obj));
	}

	/* copy args from registers to stack */
	areg = ARMREG_A1 + sig->hasthis;
	arg_pos = -(int)(ARM_NUM_ARG_REGS - sig->hasthis) * 2 * sizeof(armword_t);
	arg_add = 0;
	for (i = 0; i < sig->param_count; ++i) {
		if (areg >= ARM_NUM_ARG_REGS) break;
		ARM_STR_IMM(p, areg, ARMREG_R7, arg_pos);
		++areg;
		if (!sig->params[i]->byref) {
			switch (sig->params[i]->type) {
			case MONO_TYPE_I8:
			case MONO_TYPE_U8:
			case MONO_TYPE_R8:
				if (areg >= ARM_NUM_ARG_REGS) {
					/* load second half of 64-bit arg */
					ARM_LDR_IMM(p, ARMREG_R4, ARMREG_R7, 0);
					ARM_STR_IMM(p, ARMREG_R4, ARMREG_R7, arg_pos + sizeof(armword_t));
					arg_add = sizeof(armword_t);
				} else {
					/* second half is already the register */
					ARM_STR_IMM(p, areg, ARMREG_R7, arg_pos + sizeof(armword_t));
					++areg;
				}
				break;
			case MONO_TYPE_VALUETYPE:
				/* assert */
			default:
				break;
			}
		}
		arg_pos += 2 * sizeof(armword_t);
	}
	/* number of args passed in registers */
	reg_args = i;



	/*
	 * Calc and save stack args ptr,
	 * args follow MonoInvocation struct on the stack.
	 */
	ARM_ADD_REG_IMM8(p, ARMREG_R1, ARMREG_SP, sizeof(MonoInvocation));
	ARM_STR_IMM(p, ARMREG_R1, ARMREG_SP, MINV_OFFS(stack_args));

	/* convert method args to stackvals */
	arg_pos = -(int)(ARM_NUM_ARG_REGS - sig->hasthis) * 2 * sizeof(armword_t);
	stackval_pos = sizeof(MonoInvocation);
	for (i = 0; i < sig->param_count; ++i) {
		if (i < reg_args) {
			ARM_SUB_REG_IMM8(p, ARMREG_A3, ARMREG_R7, -arg_pos);
			arg_pos += 2 * sizeof(armword_t);
		} else {
			if (arg_pos < 0) arg_pos = 0;
			pos = arg_pos + arg_add;
			if (pos <= 0xFF) {
				ARM_ADD_REG_IMM8(p, ARMREG_A3, ARMREG_R7, pos);
			} else {
				if (is_arm_const((armword_t)pos)) {
					shift = calc_arm_mov_const_shift((armword_t)pos);
					ARM_ADD_REG_IMM(p, ARMREG_A3, ARMREG_R7, pos >> ((32 - shift) & 31), shift >> 1);
				} else {
					p = (guchar*)arm_mov_reg_imm32((arminstr_t*)p, ARMREG_R6, (armword_t)pos);
					ARM_ADD_REG_REG(p, ARMREG_A2, ARMREG_R7, ARMREG_R6);
				}
			}
			arg_pos += sizeof(armword_t);
			if (!sig->params[i]->byref) {
				switch (sig->params[i]->type) {
				case MONO_TYPE_I8:
				case MONO_TYPE_U8:
				case MONO_TYPE_R8:
					arg_pos += sizeof(armword_t);
					break;
				case MONO_TYPE_VALUETYPE:
					/* assert */
				default:
					break;
				}
			}
		}

		/* A2 = result */
		if (stackval_pos <= 0xFF) {
			ARM_ADD_REG_IMM8(p, ARMREG_A2, ARMREG_SP, stackval_pos);
		} else {
			if (is_arm_const((armword_t)stackval_pos)) {
				shift = calc_arm_mov_const_shift((armword_t)stackval_pos);
				ARM_ADD_REG_IMM(p, ARMREG_A2, ARMREG_SP, stackval_pos >> ((32 - shift) & 31), shift >> 1);
			} else {
				p = (guchar*)arm_mov_reg_imm32((arminstr_t*)p, ARMREG_R6, (armword_t)stackval_pos);
				ARM_ADD_REG_REG(p, ARMREG_A2, ARMREG_SP, ARMREG_R6);
			}
		}

		/* A1 = type */
		p = (guchar*)arm_mov_reg_imm32((arminstr_t*)p, ARMREG_A1, (armword_t)sig->params [i]);

		stackval_pos += ARG_SIZE;

		offs = -(p + 2*sizeof(arminstr_t) - p_stackval_from_data);
		/* load function address */
		ARM_LDR_IMM(p, ARMREG_R4, ARMREG_PC, offs);
		/* call stackval_from_data */
		ARM_MOV_REG_REG(p, ARMREG_LR, ARMREG_PC);
		ARM_MOV_REG_REG(p, ARMREG_PC, ARMREG_R4);
	}

	/* store retval ptr */
	p = (guchar*)arm_mov_reg_imm32((arminstr_t*)p, ARMREG_R5, (armword_t)stackval_pos);
	ARM_ADD_REG_REG(p, ARMREG_R5, ARMREG_SP, ARMREG_R4);
	ARM_STR_IMM(p, ARMREG_R5, ARMREG_SP, MINV_OFFS(retval));

	/*
	 * Call the method.
	 */
	/* A1 = MonoInvocation ptr */
	ARM_MOV_REG_REG(p, ARMREG_A1, ARMREG_SP);
	offs = -(p + 2*sizeof(arminstr_t) - p_exec);
	/* load function address */
	ARM_LDR_IMM(p, ARMREG_R4, ARMREG_PC, offs);
	/* call ves_exec */
	ARM_MOV_REG_REG(p, ARMREG_LR, ARMREG_PC);
	ARM_MOV_REG_REG(p, ARMREG_PC, ARMREG_R4);


	/*
	 * Move retval into reg.
	 */
	if (sig->ret->byref) {
		ARM_LDR_IMM(p, ARMREG_R0, ARMREG_R5, 0);
	} else {
		switch (sig->ret->type) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
			ARM_LDRB_IMM(p, ARMREG_R0, ARMREG_R5, 0);
			break;
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
			ARM_LDRH_IMM(p, ARMREG_R0, ARMREG_R5, 0);
			break;
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_R4:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
			ARM_LDR_IMM(p, ARMREG_R0, ARMREG_R5, 0);
			break;
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
		case MONO_TYPE_R8:
			ARM_LDR_IMM(p, ARMREG_R0, ARMREG_R5, 0);
			ARM_LDR_IMM(p, ARMREG_R1, ARMREG_R5, 4);
			break;
		case MONO_TYPE_VOID:
		default:
			break;
		}
	}


	p = (guchar*)arm_emit_std_epilogue((arminstr_t*)p, stack_size,
	    (1 << ARMREG_R4) |
	    (1 << ARMREG_R5) |
	    (1 << ARMREG_R6) |
	    (1 << ARMREG_R7));

	flush_icache();

#ifdef ARM_DUMP_DISASM
	_armdis_decode((arminstr_t*)code_buff, ((guint8*)p) - ((guint8*)code_buff));
#endif

	ji = g_new0(MonoJitInfo, 1);
	ji->method = method;
	ji->code_size = ((guint8 *) p) - ((guint8 *) code_buff);
	ji->code_start = (gpointer) code_buff;

	mono_jit_info_table_add(mono_get_root_domain (), ji);

	return code_buff;
}


/*
 * mono_create_method_pointer () will insert a pointer to the MonoMethod
 * so that the interp can easily get at the data: this function will retrieve 
 * the method from the code stream.
 */
MonoMethod* mono_method_pointer_get (void* code)
{
	unsigned char* c = code;
	/* check out magic number that follows unconditional branch */
	if (c[4] == 'M' &&
	    c[5] == 'o' &&
	    c[6] == 'n' &&
	    c[7] == 'o') return ((MonoMethod**)code)[2];
	return NULL;
}
#endif
