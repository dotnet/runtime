/*
 * amd64-codegen.h: Macros for generating x86 code
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
 *   Intel Corporation (ORP Project)
 *   Sergey Chaban (serge@wildwestsoftware.com)
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Patrik Torstensson
 *   Zalman Stern
 * 
 *  Not all routines are done for AMD64. Much could also be removed from here if supporting tramp.c is the only goal.
 * 
 * Copyright (C)  2000 Intel Corporation.  All rights reserved.
 * Copyright (C)  2001, 2002 Ximian, Inc.
 */

#ifndef AMD64_H
#define AMD64_H

typedef enum {
	AMD64_RAX = 0,
	AMD64_RCX = 1,
	AMD64_RDX = 2,
	AMD64_RBX = 3,
	AMD64_RSP = 4,
	AMD64_RBP = 5,
	AMD64_RSI = 6,
	AMD64_RDI = 7,
	AMD64_R8 = 8,
	AMD64_R9 = 9,
	AMD64_R10 = 10,
	AMD64_R11 = 11,
	AMD64_R12 = 12,
	AMD64_R13 = 13,
	AMD64R_14 = 14,
	AMD64_R15 = 15,
	AMD64_NREG
} AMD64_Reg_No;

typedef enum {
	AMD64_XMM0 = 0,
	AMD64_XMM1 = 1,
	AMD64_XMM2 = 2,
	AMD64_XMM3 = 3,
	AMD64_XMM4 = 4,
	AMD64_XMM5 = 5,
	AMD64_XMM6 = 6,
	AMD64_XMM8 = 8,
	AMD64_XMM9 = 9,
	AMD64_XMM10 = 10,
	AMD64_XMM11 = 11,
	AMD64_XMM12 = 12,
	AMD64_XMM13 = 13,
	AMD64_XMM14 = 14,
	AMD64_XMM15 = 15,
	AMD64_XMM_NREG = 16,
} AMD64_XMM_Reg_No;

typedef enum
{
  AMD64_REX_B = 1, /* The register in r/m field, base register in SIB byte, or reg in opcode is 8-15 rather than 0-7 */
  AMD64_REX_X = 2, /* The index register in SIB byte is 8-15 rather than 0-7 */
  AMD64_REX_R = 4, /* The reg field of ModRM byte is 8-15 rather than 0-7 */
  AMD64_REX_W = 8  /* Opeartion is 64-bits instead of 32 (default) or 16 (with 0x66 prefix) */
} AMD64_REX_Bits;

#define AMD64_REX(bits) ((unsigned char)(0x40 | (bits)))
#define amd64_emit_rex(inst, width, reg_modrm, reg_index, reg_rm_base_opcode) \
	{ \
		unsigned char _amd64_rex_bits = \
			(((width) > 4) ? AMD64_REX_W : 0) | \
			(((reg_modrm) > 7) ? AMD64_REX_R : 0) | \
			(((reg_index) > 7) ? AMD64_REX_X : 0) | \
			(((reg_rm_base_opcode) > 7) ? AMD64_REX_B : 0); \
		if (_amd64_rex_bits != 0) *(inst)++ = AMD64_REX(_amd64_rex_bits); \
	}

typedef union {
	long val;
	unsigned char b [8];
} amd64_imm_buf;

#include "../x86/x86-codegen.h"


/* Need to fill this info in for amd64. */

#if 0
/*
// bitvector mask for callee-saved registers
*/
#define X86_ESI_MASK (1<<X86_ESI)
#define X86_EDI_MASK (1<<X86_EDI)
#define X86_EBX_MASK (1<<X86_EBX)
#define X86_EBP_MASK (1<<X86_EBP)

#define X86_CALLEE_REGS ((1<<X86_EAX) | (1<<X86_ECX) | (1<<X86_EDX))
#define X86_CALLER_REGS ((1<<X86_EBX) | (1<<X86_EBP) | (1<<X86_ESI) | (1<<X86_EDI))
#define X86_BYTE_REGS   ((1<<X86_EAX) | (1<<X86_ECX) | (1<<X86_EDX) | (1<<X86_EBX))

#define X86_IS_SCRATCH(reg) (X86_CALLER_REGS & (1 << (reg))) /* X86_EAX, X86_ECX, or X86_EDX */
#define X86_IS_CALLEE(reg)  (X86_CALLEE_REGS & (1 << (reg))) 	/* X86_ESI, X86_EDI, X86_EBX, or X86_EBP */

#define X86_IS_BYTE_REG(reg) ((reg) < 4)

/*
// Frame structure:
//
//      +--------------------------------+
//      | in_arg[0]       = var[0]	     |
//      | in_arg[1]	      = var[1]	     |
//      |	      . . .			         |
//      | in_arg[n_arg-1] = var[n_arg-1] |
//      +--------------------------------+
//      |       return IP                |
//      +--------------------------------+
//      |       saved EBP                | <-- frame pointer (EBP)
//      +--------------------------------+
//      |            ...                 |  n_extra
//      +--------------------------------+
//      |	    var[n_arg]	             |
//      |	    var[n_arg+1]             |  local variables area
//      |          . . .                 |
//      |	    var[n_var-1]             | 
//      +--------------------------------+
//      |			                     |
//      |			                     |  
//      |		spill area               | area for spilling mimic stack
//      |			                     |
//      +--------------------------------|
//      |          ebx                   |
//      |          ebp [ESP_Frame only]  |
//      |	       esi                   |  0..3 callee-saved regs
//      |          edi                   | <-- stack pointer (ESP)
//      +--------------------------------+
//      |	stk0	                     |
//      |	stk1	                     |  operand stack area/
//      |	. . .	                     |  out args
//      |	stkn-1	                     |
//      +--------------------------------|
//
//
*/
#endif

#define x86_imm_emit64(inst,imm)     \
	do {	\
			amd64_imm_buf imb; imb.val = (long) (imm);	\
			*(inst)++ = imb.b [0];	\
			*(inst)++ = imb.b [1];	\
			*(inst)++ = imb.b [2];	\
			*(inst)++ = imb.b [3];	\
			*(inst)++ = imb.b [4];	\
			*(inst)++ = imb.b [5];	\
			*(inst)++ = imb.b [6];	\
			*(inst)++ = imb.b [7];	\
	} while (0)

#define amd64_alu_reg_imm(inst,opc,reg,imm) 	\
	do {	\
		if ((reg) == X86_EAX) {	\
			amd64_emit_rex(inst, 8, 0, 0, 0); \
			*(inst)++ = (((unsigned char)(opc)) << 3) + 5;	\
			x86_imm_emit64 ((inst), (imm));	\
			break;	\
		}	\
		if (x86_is_imm8((imm))) {	\
			amd64_emit_rex(inst, 8, 0, 0, (reg)); \
			*(inst)++ = (unsigned char)0x83;	\
			x86_reg_emit ((inst), (opc), (reg));	\
			x86_imm_emit8 ((inst), (imm));	\
		} else {	\
			amd64_emit_rex(inst, 8, 0, 0, (reg)); \
			*(inst)++ = (unsigned char)0x81;	\
			x86_reg_emit ((inst), (opc), (reg));	\
			x86_imm_emit32 ((inst), (imm));	\
		}	\
	} while (0)

#define amd64_alu_reg_reg(inst,opc,dreg,reg)	\
	do {	\
		amd64_emit_rex(inst, 8, (dreg), 0, (reg)); \
		*(inst)++ = (((unsigned char)(opc)) << 3) + 3;	\
		x86_reg_emit ((inst), (dreg), (reg));	\
	} while (0)

#define amd64_mov_regp_reg(inst,regp,reg,size)	\
	do {	\
		if ((size) == 2) \
			*(inst)++ = (unsigned char)0x66; \
		amd64_emit_rex(inst, (size), (reg), 0, (regp)); \
		switch ((size)) {	\
		case 1: *(inst)++ = (unsigned char)0x88; break;	\
		case 2: case 4: case 8: *(inst)++ = (unsigned char)0x89; break;	\
		default: assert (0);	\
		}	\
		x86_regp_emit ((inst), (reg), (regp));	\
	} while (0)

#define amd64_mov_membase_reg(inst,basereg,disp,reg,size)	\
	do {	\
		if ((size) == 2) \
			*(inst)++ = (unsigned char)0x66; \
		amd64_emit_rex(inst, (size), (reg), 0, (basereg)); \
		switch ((size)) {	\
		case 1: *(inst)++ = (unsigned char)0x88; break;	\
		case 2: case 4: case 8: *(inst)++ = (unsigned char)0x89; break;	\
		default: assert (0);	\
		}	\
		x86_membase_emit ((inst), (reg), (basereg), (disp));	\
	} while (0)


#define amd64_mov_reg_reg(inst,dreg,reg,size)	\
	do {	\
		if ((size) == 2) \
			*(inst)++ = (unsigned char)0x66; \
		amd64_emit_rex(inst, (size), (dreg), 0, (reg)); \
		switch ((size)) {	\
		case 1: *(inst)++ = (unsigned char)0x8a; break;	\
		case 2: case 4: case 8: *(inst)++ = (unsigned char)0x8b; break;	\
		default: assert (0);	\
		}	\
		x86_reg_emit ((inst), (dreg), (reg));	\
	} while (0)

#define amd64_mov_reg_mem(inst,reg,mem,size)	\
	do {	\
		if ((size) == 2) \
			*(inst)++ = (unsigned char)0x66; \
		amd64_emit_rex(inst, (size), (reg), 0, 0); \
		switch ((size)) {	\
		case 1: *(inst)++ = (unsigned char)0x8a; break;	\
		case 2: case 4: case 8: *(inst)++ = (unsigned char)0x8b; break;	\
		default: assert (0);	\
		}	\
		x86_mem_emit ((inst), (reg), (mem));	\
	} while (0)

#define amd64_mov_reg_membase(inst,reg,basereg,disp,size)	\
	do {	\
		if ((size) == 2) \
			*(inst)++ = (unsigned char)0x66; \
		amd64_emit_rex(inst, (size), (reg), 0, (basereg)); \
		switch ((size)) {	\
		case 1: *(inst)++ = (unsigned char)0x8a; break;	\
		case 2: case 4: case 8: *(inst)++ = (unsigned char)0x8b; break;	\
		default: assert (0);	\
		}	\
		x86_membase_emit ((inst), (reg), (basereg), (disp));	\
	} while (0)

#define amd64_movzx_reg_membase(inst,reg,basereg,disp,size)	\
	do {	\
		amd64_emit_rex(inst, (size), (reg), 0, (basereg)); \
		switch ((size)) {	\
		case 1: *(inst)++ = (unsigned char)0x0f; *(inst)++ = (unsigned char)0xb6; break;	\
		case 2: *(inst)++ = (unsigned char)0x0f; *(inst)++ = (unsigned char)0xb7; break;	\
		case 4: case 8: *(inst)++ = (unsigned char)0x8b; break;	\
		default: assert (0);	\
		}	\
		x86_membase_emit ((inst), (reg), (basereg), (disp));	\
	} while (0)

/* Pretty much the only instruction that supports a 64-bit immediate. Optimize for common case of
 * 32-bit immediate. Pepper with casts to avoid warnings.
 */
#define amd64_mov_reg_imm(inst,reg,imm)	\
	do {	\
		int _amd64_width_temp = ((long)(imm) == (long)(int)(long)(imm)); \
		amd64_emit_rex(inst, _amd64_width_temp ? 8 : 4, 0, 0, (reg)); \
		*(inst)++ = (unsigned char)0xb8 + ((reg) & 0x7);	\
		if (_amd64_width_temp) \
			x86_imm_emit64 ((inst), (long)(imm));	\
		else \
			x86_imm_emit32 ((inst), (int)(long)(imm));	\
	} while (0)

#define amd64_mov_membase_imm(inst,basereg,disp,imm,size)	\
	do {	\
		if ((size) == 2) \
			*(inst)++ = (unsigned char)0x66; \
		amd64_emit_rex(inst, (size), 0, 0, (basereg)); \
		if ((size) == 1) {	\
			*(inst)++ = (unsigned char)0xc6;	\
			x86_membase_emit ((inst), 0, (basereg), (disp));	\
			x86_imm_emit8 ((inst), (imm));	\
		} else if ((size) == 2) {	\
			*(inst)++ = (unsigned char)0xc7;	\
			x86_membase_emit ((inst), 0, (basereg), (disp));	\
			x86_imm_emit16 ((inst), (imm));	\
		} else {	\
			*(inst)++ = (unsigned char)0xc7;	\
			x86_membase_emit ((inst), 0, (basereg), (disp));	\
			x86_imm_emit32 ((inst), (imm));	\
		}	\
	} while (0)

#define amd64_lea_membase(inst,reg,basereg,disp)	\
	do {	\
		amd64_emit_rex(inst, 8, (reg), 0, (basereg)); \
		*(inst)++ = (unsigned char)0x8d;	\
		x86_membase_emit ((inst), (reg), (basereg), (disp));	\
	} while (0)

/* Instruction are implicitly 64-bits so don't generate REX for just the size. */
#define amd64_push_reg(inst,reg)	\
	do {	\
		amd64_emit_rex(inst, 0, 0, 0, (reg)); \
		*(inst)++ = (unsigned char)0x50 + ((reg) & 0x7);	\
	} while (0)

/* Instruction is implicitly 64-bits so don't generate REX for just the size. */
#define amd64_push_membase(inst,basereg,disp)	\
	do {	\
		amd64_emit_rex(inst, 0, 0, 0, (basereg)); \
		*(inst)++ = (unsigned char)0xff;	\
		x86_membase_emit ((inst), 6, (basereg), (disp));	\
	} while (0)

#define amd64_pop_reg(inst,reg)	\
	do {	\
		amd64_emit_rex(inst, 0, 0, 0, (reg)); \
		*(inst)++ = (unsigned char)0x58 + (reg);	\
	} while (0)

#define amd64_call_reg(inst,reg)	\
	do {	\
		amd64_emit_rex(inst, 0, 0, 0, (reg)); \
		*(inst)++ = (unsigned char)0xff;	\
		x86_reg_emit ((inst), 2, (reg));	\
	} while (0)

#define amd64_ret(inst) do { *(inst)++ = (unsigned char)0xc3; } while (0)
#define amd64_leave(inst) do { *(inst)++ = (unsigned char)0xc9; } while (0)
#define amd64_movsd_reg_regp(inst,reg,regp)	\
	do {	\
		*(inst)++ = (unsigned char)0xf2;	\
		amd64_emit_rex(inst, 0, (reg), 0, (regp)); \
		*(inst)++ = (unsigned char)0x0f;	\
		*(inst)++ = (unsigned char)0x10;	\
		x86_regp_emit ((inst), (reg), (regp));	\
	} while (0)

#define amd64_movsd_regp_reg(inst,regp,reg)	\
	do {	\
		*(inst)++ = (unsigned char)0xf2;	\
		amd64_emit_rex(inst, 0, (reg), 0, (regp)); \
		*(inst)++ = (unsigned char)0x0f;	\
		*(inst)++ = (unsigned char)0x11;	\
		x86_regp_emit ((inst), (reg), (regp));	\
	} while (0)

#define amd64_movss_reg_regp(inst,reg,regp)	\
	do {	\
		*(inst)++ = (unsigned char)0xf3;	\
		amd64_emit_rex(inst, 0, (reg), 0, (regp)); \
		*(inst)++ = (unsigned char)0x0f;	\
		*(inst)++ = (unsigned char)0x10;	\
		x86_regp_emit ((inst), (reg), (regp));	\
	} while (0)

#define amd64_movss_regp_reg(inst,regp,reg)	\
	do {	\
		*(inst)++ = (unsigned char)0xf3;	\
		amd64_emit_rex(inst, 0, (reg), 0, (regp)); \
		*(inst)++ = (unsigned char)0x0f;	\
		*(inst)++ = (unsigned char)0x11;	\
		x86_regp_emit ((inst), (reg), (regp));	\
	} while (0)

#define amd64_movsd_reg_membase(inst,reg,basereg,disp)	\
	do {	\
		*(inst)++ = (unsigned char)0xf2;	\
		amd64_emit_rex(inst, 0, (reg), 0, (basereg)); \
		*(inst)++ = (unsigned char)0x0f;	\
		*(inst)++ = (unsigned char)0x10;	\
		x86_membase_emit ((inst), (reg), (basereg), (disp));	\
	} while (0)

#define amd64_movss_reg_membase(inst,reg,basereg,disp)	\
	do {	\
		*(inst)++ = (unsigned char)0xf3;	\
		amd64_emit_rex(inst, 0, (reg), 0, (basereg)); \
		*(inst)++ = (unsigned char)0x0f;	\
		*(inst)++ = (unsigned char)0x10;	\
		x86_membase_emit ((inst), (reg), (basereg), (disp));	\
	} while (0)

#define amd64_movsd_membase_reg(inst,reg,basereg,disp)	\
	do {	\
		*(inst)++ = (unsigned char)0xf2;	\
		amd64_emit_rex(inst, 0, (reg), 0, (basereg)); \
		*(inst)++ = (unsigned char)0x0f;	\
		*(inst)++ = (unsigned char)0x11;	\
		x86_membase_emit ((inst), (reg), (basereg), (disp));	\
	} while (0)

#define amd64_movss_membase_reg(inst,reg,basereg,disp)	\
	do {	\
		*(inst)++ = (unsigned char)0xf3;	\
		amd64_emit_rex(inst, 0, (reg), 0, (basereg)); \
		*(inst)++ = (unsigned char)0x0f;	\
		*(inst)++ = (unsigned char)0x11;	\
		x86_membase_emit ((inst), (reg), (basereg), (disp));	\
	} while (0)

#endif // AMD64_H
