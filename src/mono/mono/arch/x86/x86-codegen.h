/* Copyright (C)  2000 Intel Corporation.  All rights reserved.
   Copyright (C)  2001 Ximian, Inc. 
//
// $Header: /home/miguel/third-conversion/public/mono/mono/arch/x86/x86-codegen.h,v 1.19 2001/12/17 06:50:02 dietmar Exp $
*/

#ifndef X86_H
#define X86_H
#include <assert.h>
/*
// x86 register numbers
*/
typedef enum {
	X86_EAX = 0,
	X86_ECX = 1,
	X86_EDX = 2,
	X86_EBX = 3,
	X86_ESP = 4,
	X86_EBP = 5,
	X86_ESI = 6,
	X86_EDI = 7,
	X86_NREG
} X86_Reg_No;
/*
// opcodes for alu instructions
*/
typedef enum {
	X86_ADD = 0,
	X86_OR  = 1,
	X86_ADC = 2,
	X86_SBB = 3,
	X86_AND = 4,
	X86_SUB = 5,
	X86_XOR = 6,
	X86_CMP = 7,
	X86_NALU
} X86_ALU_Opcode;
/*
// opcodes for shift instructions
*/
typedef enum {
	X86_SHLD,
	X86_SHLR,
	X86_SHL = 4,
	X86_SHR = 5,
	X86_SAR = 7,
	X86_NSHIFT = 8
} X86_Shift_Opcode;
/*
// opcodes for floating-point instructions
*/
typedef enum {
	X86_FADD  = 0,
	X86_FMUL  = 1,
	X86_FCOM  = 2,
	X86_FCOMP = 3,
	X86_FSUB  = 4,
	X86_FSUBR = 5,
	X86_FDIV  = 6,
	X86_FDIVR = 7,
	X86_NFP   = 8
} X86_FP_Opcode;
/*
// integer conditions codes
*/
typedef enum {
	X86_CC_EQ = 0,
	X86_CC_NE,
	X86_CC_LT,
	X86_CC_LE,
	X86_CC_GT,
	X86_CC_GE,
	X86_CC_LZ,
	X86_CC_GEZ,
	X86_CC_P,
	X86_CC_NP,
	X86_NCC
} X86_CC;
/*
// prefix code
*/
typedef enum {
	X86_LOCK_PREFIX = 0xF0,
	X86_REPNZ_PREFIX = 0xF2,
	X86_REPZ_PREFIX = 0xF3, 
	X86_REP_PREFIX = 0xF3,
	X86_CS_PREFIX = 0x2E,
	X86_SS_PREFIX = 0x36,
	X86_DS_PREFIX = 0x3E,
	X86_ES_PREFIX = 0x26,
	X86_FS_PREFIX = 0x64,
	X86_GS_PREFIX = 0x65,
	X86_OPERAND_PREFIX = 0x66,
	X86_ADDRESS_PREFIX = 0x67
} X86_Prefix;

static const unsigned char 
x86_cc_unsigned_map [X86_NCC] = {
	0x74, /* eq  */
	0x75, /* ne  */
	0x72, /* lt  */
	0x76, /* le  */
	0x77, /* gt  */
	0x73, /* ge  */
	0x78, /* lz  */
	0x79, /* gez */
	0x7a, /* p   */
	0x7b, /* np  */
};

static const unsigned char 
x86_cc_signed_map [X86_NCC] = {
	0x74, /* eq  */
	0x75, /* ne  */
	0x7c, /* lt  */
	0x7e, /* le  */
	0x7f, /* gt  */
	0x7d, /* ge  */
	0x78, /* lz  */
	0x79, /* gez */
	0x7a, /* p   */
	0x7b, /* np  */
};

typedef union {
	int val;
	unsigned char b [4];
} x86_imm_buf;

#define X86_NOBASEREG (-1)

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


/*
 * useful building blocks
 */
#define x86_address_byte(inst,m,o,r) do { *(inst)++ = ((((m)&0x03)<<6)|(((o)&0x07)<<3)|(((r)&0x07))); } while (0)
#define x86_imm_emit32(inst,imm)     \
	do {	\
			x86_imm_buf imb; imb.val = (int) (imm);	\
			*(inst)++ = imb.b [0];	\
			*(inst)++ = imb.b [1];	\
			*(inst)++ = imb.b [2];	\
			*(inst)++ = imb.b [3];	\
	} while (0)
#define x86_imm_emit16(inst,imm)     do { *(short*)(inst) = (imm); (inst) += 2; } while (0)
#define x86_imm_emit8(inst,imm)      do { *(inst) = (unsigned char)((imm) & 0xff); ++(inst); } while (0)
#define x86_is_imm8(imm)             (((int)(imm) >= -128 && (int)(imm) <= 127))
#define x86_is_imm16(imm)            (((int)(imm) >= -(1<<16) && (int)(imm) <= ((1<<16)-1)))

#define x86_reg_emit(inst,r,regno)   do { x86_address_byte ((inst), 3, (r), (regno)); } while (0)
#define x86_regp_emit(inst,r,regno)  do { x86_address_byte ((inst), 0, (r), (regno)); } while (0)
#define x86_mem_emit(inst,r,disp)    do { x86_address_byte ((inst), 0, (r), 5); x86_imm_emit32((inst), (disp)); } while (0)

#define x86_membase_emit(inst,r,basereg,disp)	do {\
	if ((basereg) == X86_ESP) {	\
		if ((disp) == 0) {	\
			x86_address_byte ((inst), 0, (r), X86_ESP);	\
			x86_address_byte ((inst), 0, X86_ESP, X86_ESP);	\
		} else if (x86_is_imm8((disp))) {	\
			x86_address_byte ((inst), 1, (r), X86_ESP);	\
			x86_address_byte ((inst), 0, X86_ESP, X86_ESP);	\
			x86_imm_emit8 ((inst), (disp));	\
		} else {	\
			x86_address_byte ((inst), 2, (r), X86_ESP);	\
			x86_address_byte ((inst), 0, X86_ESP, X86_ESP);	\
			x86_imm_emit32 ((inst), (disp));	\
		}	\
		break;	\
	}	\
	if ((disp) == 0 && (basereg) != X86_EBP) {	\
		x86_address_byte ((inst), 0, (r), (basereg));	\
		break;	\
	}	\
	if (x86_is_imm8((disp))) {	\
		x86_address_byte ((inst), 1, (r), (basereg));	\
		x86_imm_emit8 ((inst), (disp));	\
	} else {	\
		x86_address_byte ((inst), 2, (r), (basereg));	\
		x86_imm_emit32 ((inst), (disp));	\
	}	\
	} while (0)

#define x86_memindex_emit(inst,r,basereg,disp,indexreg,shift)	\
	do {	\
		if ((basereg) == X86_NOBASEREG) {	\
			x86_address_byte ((inst), 0, (r), 4);	\
			x86_address_byte ((inst), (shift), (indexreg), 5);	\
			x86_imm_emit32 ((inst), (disp));	\
		} else if ((disp) == 0 && (basereg) != X86_EBP) {	\
			x86_address_byte ((inst), 0, (r), 4);	\
			x86_address_byte ((inst), (shift), (indexreg), (basereg));	\
		} else if (x86_is_imm8((disp))) {	\
			x86_address_byte ((inst), 1, (r), 4);	\
			x86_address_byte ((inst), (shift), (indexreg), (basereg));	\
			x86_imm_emit8 ((inst), (disp));	\
		} else {	\
			x86_address_byte ((inst), 0, (r), 4);	\
			x86_address_byte ((inst), (shift), (indexreg), 5);	\
			x86_imm_emit32 ((inst), (disp));	\
		}	\
	} while (0)

#define x86_breakpoint(inst) \
	do {	\
		*(inst)++ = 0xcc;	\
	} while (0)

#define x86_cld(inst) do { *(inst)++ =(unsigned char)0xfc; } while (0)
#define x86_stosb(inst) do { *(inst)++ =(unsigned char)0xaa; } while (0)
#define x86_stosl(inst) do { *(inst)++ =(unsigned char)0xab; } while (0)

#define x86_prefix(inst,p) do { *(inst)++ =(unsigned char) (p); } while (0)

#define x86_rdtsc(inst) \
	do {	\
		*(inst)++ = 0x0f;	\
		*(inst)++ = 0x31;	\
	} while (0)

#define x86_cmpxchg_reg_reg(inst,dreg,reg)	\
	do {	\
		*(inst)++ = (unsigned char)0x0f;	\
		*(inst)++ = (unsigned char)0xb1;	\
		x86_reg_emit ((inst), (reg), (dreg));	\
	} while (0)
	
#define x86_cmpxchg_mem_reg(inst,mem,reg)	\
	do {	\
		*(inst)++ = (unsigned char)0x0f;	\
		*(inst)++ = (unsigned char)0xb1;	\
		x86_mem_emit ((inst), (reg), (mem));	\
	} while (0)
	
#define x86_cmpxchg_membase_reg(inst,basereg,disp,reg)	\
	do {	\
		*(inst)++ = (unsigned char)0x0f;	\
		*(inst)++ = (unsigned char)0xb1;	\
		x86_membase_emit ((inst), (reg), (basereg), (disp));	\
	} while (0)

#define x86_xchg_reg_reg(inst,dreg,reg,size)	\
	do {	\
		if ((size) == 1)	\
			*(inst)++ = (unsigned char)0x86;	\
		else	\
			*(inst)++ = (unsigned char)0x87;	\
		x86_reg_emit ((inst), (reg), (dreg));	\
	} while (0)

#define x86_xchg_mem_reg(inst,mem,reg,size)	\
	do {	\
		if ((size) == 1)	\
			*(inst)++ = (unsigned char)0x86;	\
		else	\
			*(inst)++ = (unsigned char)0x87;	\
		x86_mem_emit ((inst), (reg), (mem));	\
	} while (0)

#define x86_xchg_membase_reg(inst,basereg,disp,reg,size)	\
	do {	\
		if ((size) == 1)	\
			*(inst)++ = (unsigned char)0x86;	\
		else	\
			*(inst)++ = (unsigned char)0x87;	\
		x86_membase_emit ((inst), (reg), (basereg), (disp));	\
	} while (0)

#define x86_inc_mem(inst,mem)	\
	do {	\
		*(inst)++ = (unsigned char)0xff;	\
		x86_mem_emit ((inst), 0, (mem)); 	\
	} while (0)

#define x86_inc_membase(inst,basereg,disp)	\
	do {	\
		*(inst)++ = (unsigned char)0xff;	\
		x86_membase_emit ((inst), 0, (basereg), (disp));	\
	} while (0)

#define x86_inc_reg(inst,reg) do { *(inst)++ = (unsigned char)0x40 + (reg); } while (0)

#define x86_dec_mem(inst,mem)	\
	do {	\
		*(inst)++ = (unsigned char)0xff;	\
		x86_mem_emit ((inst), 1, (mem));	\
	} while (0)

#define x86_dec_membase(inst,basereg,disp)	\
	do {	\
		*(inst)++ = (unsigned char)0xff;	\
		x86_membase_emit ((inst), 1, (basereg), (disp));	\
	} while (0)

#define x86_dec_reg(inst,reg) do { *(inst)++ = (unsigned char)0x48 + (reg); } while (0)

#define x86_not_mem(inst,mem)	\
	do {	\
		*(inst)++ = (unsigned char)0xf7;	\
		x86_mem_emit ((inst), 2, (mem));	\
	} while (0)

#define x86_not_membase(inst,basereg,disp)	\
	do {	\
		*(inst)++ = (unsigned char)0xf7;	\
		x86_membase_emit ((inst), 2, (basereg), (disp));	\
	} while (0)

#define x86_not_reg(inst,reg)	\
	do {	\
		*(inst)++ = (unsigned char)0xf7;	\
		x86_reg_emit ((inst), 2, (reg));	\
	} while (0)

#define x86_neg_mem(inst,mem)	\
	do {	\
		*(inst)++ = (unsigned char)0xf7;	\
		x86_mem_emit ((inst), 3, (mem));	\
	} while (0)

#define x86_neg_membase(inst,basereg,disp)	\
	do {	\
		*(inst)++ = (unsigned char)0xf7;	\
		x86_membase_emit ((inst), 3, (basereg), (disp));	\
	} while (0)

#define x86_neg_reg(inst,reg)	\
	do {	\
		*(inst)++ = (unsigned char)0xf7;	\
		x86_reg_emit ((inst), 3, (reg));	\
	} while (0)

#define x86_nop(inst) do { *(inst)++ = (unsigned char)0x90; } while (0)

#define x86_alu_reg_imm(inst,opc,reg,imm) 	\
	do {	\
		if ((reg) == X86_EAX) {	\
			*(inst)++ = (((unsigned char)(opc)) << 3) + 5;	\
			x86_imm_emit32 ((inst), (imm));	\
			break;	\
		}	\
		if (x86_is_imm8((imm))) {	\
			*(inst)++ = (unsigned char)0x83;	\
			x86_reg_emit ((inst), (opc), (reg));	\
			x86_imm_emit8 ((inst), (imm));	\
		} else {	\
			*(inst)++ = (unsigned char)0x81;	\
			x86_reg_emit ((inst), (opc), (reg));	\
			x86_imm_emit32 ((inst), (imm));	\
		}	\
	} while (0)

#define x86_alu_mem_imm(inst,opc,mem,imm) 	\
	do {	\
		if (x86_is_imm8((imm))) {	\
			*(inst)++ = (unsigned char)0x83;	\
			x86_mem_emit ((inst), (opc), (mem));	\
			x86_imm_emit8 ((inst), (imm));	\
		} else {	\
			*(inst)++ = (unsigned char)0x81;	\
			x86_mem_emit ((inst), (opc), (mem));	\
			x86_imm_emit32 ((inst), (imm));	\
		}	\
	} while (0)

#define x86_alu_membase_imm(inst,opc,basereg,disp,imm) 	\
	do {	\
		if (x86_is_imm8((imm))) {	\
			*(inst)++ = (unsigned char)0x83;	\
			x86_membase_emit ((inst), (opc), (basereg), (disp));	\
			x86_imm_emit8 ((inst), (imm));	\
		} else {	\
			*(inst)++ = (unsigned char)0x81;	\
			x86_membase_emit ((inst), (opc), (basereg), (disp));	\
			x86_imm_emit32 ((inst), (imm));	\
		}	\
	} while (0)

#define x86_alu_mem_reg(inst,opc,mem,reg)	\
	do {	\
		*(inst)++ = (((unsigned char)(opc)) << 3) + 1;	\
		x86_mem_emit ((inst), (reg), (mem));	\
	} while (0)

#define x86_alu_membase_reg(inst,opc,basereg,disp,reg)	\
	do {	\
		*(inst)++ = (((unsigned char)(opc)) << 3) + 1;	\
		x86_membase_emit ((inst), (reg), (basereg), (disp));	\
	} while (0)

#define x86_alu_reg_reg(inst,opc,dreg,reg)	\
	do {	\
		*(inst)++ = (((unsigned char)(opc)) << 3) + 3;	\
		x86_reg_emit ((inst), (dreg), (reg));	\
	} while (0)

#define x86_alu_reg_mem(inst,opc,reg,mem)	\
	do {	\
		*(inst)++ = (((unsigned char)(opc)) << 3) + 3;	\
		x86_mem_emit ((inst), (reg), (mem));	\
	} while (0)

#define x86_alu_reg_membase(inst,opc,reg,basereg,disp)	\
	do {	\
		*(inst)++ = (((unsigned char)(opc)) << 3) + 3;	\
		x86_membase_emit ((inst), (reg), (basereg), (disp));	\
	} while (0)

#define x86_test_reg_imm(inst,reg,imm)	\
	do {	\
		if ((reg) == X86_EAX) {	\
			*(inst)++ = (unsigned char)0xa9;	\
		} else {	\
			*(inst)++ = (unsigned char)0xf7;	\
			x86_reg_emit ((inst), 0, (reg));	\
		}	\
		x86_imm_emit32 ((inst), (imm));	\
	} while (0)

#define x86_test_mem_imm(inst,mem,imm)	\
	do {	\
		*(inst)++ = (unsigned char)0xf7;	\
		x86_mem_emit ((inst), 0, (mem));	\
		x86_imm_emit32 ((inst), (imm));	\
	} while (0)

#define x86_test_membase_imm(inst,basereg,disp,imm)	\
	do {	\
		*(inst)++ = (unsigned char)0xf7;	\
		x86_membase_emit ((inst), 0, (basereg), (disp));	\
		x86_imm_emit32 ((inst), (imm));	\
	} while (0)

#define x86_test_reg_reg(inst,dreg,reg)	\
	do {	\
		*(inst)++ = (unsigned char)0x85;	\
		x86_reg_emit ((inst), (reg), (dreg));	\
	} while (0)

#define x86_test_mem_reg(inst,mem,reg)	\
	do {	\
		*(inst)++ = (unsigned char)0x85;	\
		x86_mem_emit ((inst), (reg), (mem));	\
	} while (0)

#define x86_test_membase_reg(inst,basereg,disp,reg)	\
	do {	\
		*(inst)++ = (unsigned char)0x85;	\
		x86_membase_emit ((inst), (reg), (basereg), (disp));	\
	} while (0)

#define x86_shift_reg_imm(inst,opc,reg,imm)	\
	do {	\
		if ((imm) == 1) {	\
			*(inst)++ = (unsigned char)0xd1;	\
			x86_reg_emit ((inst), (opc), (reg));	\
		} else {	\
			*(inst)++ = (unsigned char)0xc1;	\
			x86_reg_emit ((inst), (opc), (reg));	\
			x86_imm_emit8 ((inst), (imm));	\
		}	\
	} while (0)

#define x86_shift_mem_imm(inst,opc,mem,imm)	\
	do {	\
		if ((imm) == 1) {	\
			*(inst)++ = (unsigned char)0xd1;	\
			x86_mem_emit ((inst), (opc), (mem));	\
		} else {	\
			*(inst)++ = (unsigned char)0xc1;	\
			x86_mem_emit ((inst), (opc), (mem));	\
			x86_imm_emit8 ((inst), (imm));	\
		}	\
	} while (0)

#define x86_shift_membase_imm(inst,opc,basereg,disp,imm)	\
	do {	\
		if ((imm) == 1) {	\
			*(inst)++ = (unsigned char)0xd1;	\
			x86_membase_emit ((inst), (opc), (basereg), (disp));	\
		} else {	\
			*(inst)++ = (unsigned char)0xc1;	\
			x86_membase_emit ((inst), (opc), (basereg), (disp));	\
			x86_imm_emit8 ((inst), (imm));	\
		}	\
	} while (0)

#define x86_shift_reg(inst,opc,reg)	\
	do {	\
		*(inst)++ = (unsigned char)0xd3;	\
		x86_reg_emit ((inst), (opc), (reg));	\
	} while (0)

#define x86_shift_mem(inst,opc,mem)	\
	do {	\
		*(inst)++ = (unsigned char)0xd3;	\
		x86_mem_emit ((inst), (opc), (mem));	\
	} while (0)

#define x86_shift_membase(inst,opc,basereg,disp)	\
	do {	\
		*(inst)++ = (unsigned char)0xd3;	\
		x86_membase_emit ((inst), (opc), (basereg), (disp));	\
	} while (0)

/*
 * Multi op shift missing.
 */

/*
 * EDX:EAX = EAX * rm
 */
#define x86_mul_reg(inst,reg,is_signed)	\
	do {	\
		*(inst)++ = (unsigned char)0xf7;	\
		x86_reg_emit ((inst), 4 + ((is_signed) ? 1 : 0), (reg));	\
	} while (0)

#define x86_mul_mem(inst,mem,is_signed)	\
	do {	\
		*(inst)++ = (unsigned char)0xf7;	\
		x86_mem_emit ((inst), 4 + ((is_signed) ? 1 : 0), (mem));	\
	} while (0)

#define x86_mul_membase(inst,basereg,disp,is_signed)	\
	do {	\
		*(inst)++ = (unsigned char)0xf7;	\
		x86_membase_emit ((inst), 4 + ((is_signed) ? 1 : 0), (basereg), (disp));	\
	} while (0)

/*
 * r *= rm
 */
#define x86_imul_reg_reg(inst,dreg,reg)	\
	do {	\
		*(inst)++ = (unsigned char)0x0f;	\
		*(inst)++ = (unsigned char)0xaf;	\
		x86_reg_emit ((inst), (dreg), (reg));	\
	} while (0)

#define x86_imul_reg_mem(inst,reg,mem)	\
	do {	\
		*(inst)++ = (unsigned char)0x0f;	\
		*(inst)++ = (unsigned char)0xaf;	\
		x86_mem_emit ((inst), (reg), (mem));	\
	} while (0)

#define x86_imul_reg_membase(inst,reg,basereg,disp)	\
	do {	\
		*(inst)++ = (unsigned char)0x0f;	\
		*(inst)++ = (unsigned char)0xaf;	\
		x86_membase_emit ((inst), (reg), (basereg), (disp));	\
	} while (0)

/*
 * dreg = rm * imm
 */
#define x86_imul_reg_reg_imm(inst,dreg,reg,imm)	\
	do {	\
		if (x86_is_imm8 ((imm))) {	\
			*(inst)++ = (unsigned char)0x6b;	\
			x86_reg_emit ((inst), (dreg), (reg));	\
			x86_imm_emit8 ((inst), (imm));	\
		} else {	\
			*(inst)++ = (unsigned char)0x69;	\
			x86_reg_emit ((inst), (dreg), (reg));	\
			x86_imm_emit32 ((inst), (imm));	\
		}	\
	} while (0)

#define x86_imul_reg_mem_imm(inst,reg,mem,imm)	\
	do {	\
		if (x86_is_imm8 ((imm))) {	\
			*(inst)++ = (unsigned char)0x6b;	\
			x86_mem_emit ((inst), (reg), (mem));	\
			x86_imm_emit8 ((inst), (imm));	\
		} else {	\
			*(inst)++ = (unsigned char)0x69;	\
			x86_reg_emit ((inst), (reg), (mem));	\
			x86_imm_emit32 ((inst), (imm));	\
		}	\
	} while (0)

#define x86_imul_reg_membase_imm(inst,reg,basereg,disp,imm)	\
	do {	\
		if (x86_is_imm8 ((imm))) {	\
			*(inst)++ = (unsigned char)0x6b;	\
			x86_membase_emit ((inst), (reg), (basereg), (disp));	\
			x86_imm_emit8 ((inst), (imm));	\
		} else {	\
			*(inst)++ = (unsigned char)0x69;	\
			x86_membase_emit ((inst), (reg), (basereg), (disp));	\
			x86_imm_emit32 ((inst), (imm));	\
		}	\
	} while (0)

/*
 * divide EDX:EAX by rm;
 * eax = quotient, edx = remainder
 */

#define x86_div_reg(inst,reg,is_signed)	\
	do {	\
		*(inst)++ = (unsigned char)0xf7;	\
		x86_reg_emit ((inst), 6 + ((is_signed) ? 1 : 0), (reg));	\
	} while (0)

#define x86_div_mem(inst,mem,is_signed)	\
	do {	\
		*(inst)++ = (unsigned char)0xf7;	\
		x86_mem_emit ((inst), 6 + ((is_signed) ? 1 : 0), (mem));	\
	} while (0)

#define x86_div_membase(inst,basereg,disp,is_signed)	\
	do {	\
		*(inst)++ = (unsigned char)0xf7;	\
		x86_membase_emit ((inst), 6 + ((is_signed) ? 1 : 0), (basereg), (disp));	\
	} while (0)

#define x86_mov_mem_reg(inst,mem,reg,size)	\
	do {	\
		switch ((size)) {	\
		case 1: *(inst)++ = (unsigned char)0x88; break;	\
		case 2: *(inst)++ = (unsigned char)0x66; /* fall through */	\
		case 4: *(inst)++ = (unsigned char)0x89; break;	\
		default: assert (0);	\
		}	\
		x86_mem_emit ((inst), (reg), (mem));	\
	} while (0)

#define x86_mov_regp_reg(inst,regp,reg,size)	\
	do {	\
		switch ((size)) {	\
		case 1: *(inst)++ = (unsigned char)0x88; break;	\
		case 2: *(inst)++ = (unsigned char)0x66; /* fall through */	\
		case 4: *(inst)++ = (unsigned char)0x89; break;	\
		default: assert (0);	\
		}	\
		x86_regp_emit ((inst), (reg), (regp));	\
	} while (0)

#define x86_mov_membase_reg(inst,basereg,disp,reg,size)	\
	do {	\
		switch ((size)) {	\
		case 1: *(inst)++ = (unsigned char)0x88; break;	\
		case 2: *(inst)++ = (unsigned char)0x66; /* fall through */	\
		case 4: *(inst)++ = (unsigned char)0x89; break;	\
		default: assert (0);	\
		}	\
		x86_membase_emit ((inst), (reg), (basereg), (disp));	\
	} while (0)

#define x86_mov_memindex_reg(inst,basereg,disp,indexreg,shift,reg,size)	\
	do {	\
		switch ((size)) {	\
		case 1: *(inst)++ = (unsigned char)0x88; break;	\
		case 2: *(inst)++ = (unsigned char)0x66; /* fall through */	\
		case 4: *(inst)++ = (unsigned char)0x89; break;	\
		default: assert (0);	\
		}	\
		x86_memindex_emit ((inst), (reg), (basereg), (disp), (indexreg), (shift));	\
	} while (0)

#define x86_mov_reg_reg(inst,dreg,reg,size)	\
	do {	\
		switch ((size)) {	\
		case 1: *(inst)++ = (unsigned char)0x8a; break;	\
		case 2: *(inst)++ = (unsigned char)0x66; /* fall through */	\
		case 4: *(inst)++ = (unsigned char)0x8b; break;	\
		default: assert (0);	\
		}	\
		x86_reg_emit ((inst), (dreg), (reg));	\
	} while (0)

#define x86_mov_reg_mem(inst,reg,mem,size)	\
	do {	\
		switch ((size)) {	\
		case 1: *(inst)++ = (unsigned char)0x8a; break;	\
		case 2: *(inst)++ = (unsigned char)0x66; /* fall through */	\
		case 4: *(inst)++ = (unsigned char)0x8b; break;	\
		default: assert (0);	\
		}	\
		x86_mem_emit ((inst), (reg), (mem));	\
	} while (0)

#define x86_mov_reg_membase(inst,reg,basereg,disp,size)	\
	do {	\
		switch ((size)) {	\
		case 1: *(inst)++ = (unsigned char)0x8a; break;	\
		case 2: *(inst)++ = (unsigned char)0x66; /* fall through */	\
		case 4: *(inst)++ = (unsigned char)0x8b; break;	\
		default: assert (0);	\
		}	\
		x86_membase_emit ((inst), (reg), (basereg), (disp));	\
	} while (0)

#define x86_mov_reg_memindex(inst,reg,basereg,disp,indexreg,shift,size)	\
	do {	\
		switch ((size)) {	\
		case 1: *(inst)++ = (unsigned char)0x8a; break;	\
		case 2: *(inst)++ = (unsigned char)0x66; /* fall through */	\
		case 4: *(inst)++ = (unsigned char)0x8b; break;	\
		default: assert (0);	\
		}	\
		x86_memindex_emit ((inst), (reg), (basereg), (disp), (indexreg), (shift));	\
	} while (0)

#define x86_mov_reg_imm(inst,reg,imm)	\
	do {	\
		if ((imm) == 0) {	\
			x86_alu_reg_reg ((inst), X86_XOR, (reg), (reg));	\
		} else {	\
			*(inst)++ = (unsigned char)0xb8 + (reg);	\
			x86_imm_emit32 ((inst), (imm));	\
		}	\
	} while (0)

#define x86_mov_mem_imm(inst,mem,imm,size)	\
	do {	\
		if ((size) == 1) {	\
			*(inst)++ = (unsigned char)0xc6;	\
			x86_mem_emit ((inst), 0, (mem));	\
			x86_imm_emit8 ((inst), (imm));	\
		} else if ((size) == 2) {	\
			*(inst)++ = (unsigned char)0x66;	\
			*(inst)++ = (unsigned char)0xc7;	\
			x86_mem_emit ((inst), 0, (mem));	\
			x86_imm_emit16 ((inst), (imm));	\
		} else {	\
			*(inst)++ = (unsigned char)0xc7;	\
			x86_mem_emit ((inst), 0, (mem));	\
			x86_imm_emit32 ((inst), (imm));	\
		}	\
	} while (0)

#define x86_mov_membase_imm(inst,basereg,disp,imm,size)	\
	do {	\
		if ((size) == 1) {	\
			*(inst)++ = (unsigned char)0xc6;	\
			x86_membase_emit ((inst), 0, (basereg), (disp));	\
			x86_imm_emit8 ((inst), (imm));	\
		} else if ((size) == 2) {	\
			*(inst)++ = (unsigned char)0x66;	\
			*(inst)++ = (unsigned char)0xc7;	\
			x86_membase_emit ((inst), 0, (basereg), (disp));	\
			x86_imm_emit16 ((inst), (imm));	\
		} else {	\
			*(inst)++ = (unsigned char)0xc7;	\
			x86_membase_emit ((inst), 0, (basereg), (disp));	\
			x86_imm_emit32 ((inst), (imm));	\
		}	\
	} while (0)

#define x86_mov_memindex_imm(inst,basereg,disp,indexreg,shift,imm,size)	\
	do {	\
		if ((size) == 1) {	\
			*(inst)++ = (unsigned char)0xc6;	\
			x86_memindex_emit ((inst), 0, (basereg), (disp), (indexreg), (shift));	\
			x86_imm_emit8 ((inst), (imm));	\
		} else if ((size) == 2) {	\
			*(inst)++ = (unsigned char)0x66;	\
			*(inst)++ = (unsigned char)0xc7;	\
			x86_memindex_emit ((inst), 0, (basereg), (disp), (indexreg), (shift));	\
			x86_imm_emit16 ((inst), (imm));	\
		} else {	\
			*(inst)++ = (unsigned char)0xc7;	\
			x86_memindex_emit ((inst), 0, (basereg), (disp), (indexreg), (shift));	\
			x86_imm_emit32 ((inst), (imm));	\
		}	\
	} while (0)

#define x86_lea_mem(inst,reg,mem)	\
	do {	\
		*(inst)++ = (unsigned char)0x8d;	\
		x86_mem_emit ((inst), (reg), (mem));	\
	} while (0)

#define x86_lea_membase(inst,reg,basereg,disp)	\
	do {	\
		*(inst)++ = (unsigned char)0x8d;	\
		x86_membase_emit ((inst), (reg), (basereg), (disp));	\
	} while (0)

#define x86_lea_memindex(inst,reg,basereg,disp,indexreg,shift)	\
	do {	\
		*(inst)++ = (unsigned char)0x8d;	\
		x86_memindex_emit ((inst), (reg), (basereg), (disp), (indexreg), (shift));	\
	} while (0)

#define x86_widen_reg(inst,dreg,reg,is_signed,is_half)	\
	do {	\
		unsigned char op = 0xb6;	\
		*(inst)++ = (unsigned char)0x0f;	\
		if ((is_signed)) op += 0x08;	\
		if ((is_half)) op += 0x01;	\
		*(inst)++ = op;	\
		x86_reg_emit ((inst), (dreg), (reg));	\
	} while (0)

#define x86_widen_mem(inst,dreg,mem,is_signed,is_half)	\
	do {	\
		unsigned char op = 0xb6;	\
		*(inst)++ = (unsigned char)0x0f;	\
		if ((is_signed)) op += 0x08;	\
		if ((is_half)) op += 0x01;	\
		*(inst)++ = op;	\
		x86_mem_emit ((inst), (dreg), (mem));	\
	} while (0)

#define x86_widen_membase(inst,dreg,basereg,disp,is_signed,is_half)	\
	do {	\
		unsigned char op = 0xb6;	\
		*(inst)++ = (unsigned char)0x0f;	\
		if ((is_signed)) op += 0x08;	\
		if ((is_half)) op += 0x01;	\
		*(inst)++ = op;	\
		x86_membase_emit ((inst), (dreg), (basereg), (disp));	\
	} while (0)

#define x86_widen_memindex(inst,dreg,basereg,disp,indexreg,shift,is_signed,is_half)	\
	do {	\
		unsigned char op = 0xb6;	\
		*(inst)++ = (unsigned char)0x0f;	\
		if ((is_signed)) op += 0x08;	\
		if ((is_half)) op += 0x01;	\
		*(inst)++ = op;	\
		x86_memindex_emit ((inst), (dreg), (basereg), (disp), (indexreg), (shift));	\
	} while (0)

#define x86_cdq(inst)  do { *(inst)++ = (unsigned char)0x99; } while (0)
#define x86_wait(inst) do { *(inst)++ = (unsigned char)0x9b; } while (0)

#define x86_fp_op_mem(inst,opc,mem,is_double)	\
	do {	\
		*(inst)++ = (is_double) ? (unsigned char)0xdc : (unsigned char)0xd8;	\
		x86_mem_emit ((inst), (opc), (mem));	\
	} while (0)

#define x86_fp_op(inst,opc,index)	\
	do {	\
		*(inst)++ = (unsigned char)0xd8;	\
		*(inst)++ = (unsigned char)0xc0+((opc)<<3)+((index)&0x07);	\
	} while (0)

#define x86_fp_op_reg(inst,opc,index,pop_stack)	\
	do {	\
		static const unsigned char map[] = { 0, 1, 2, 3, 5, 4, 7, 6, 8};	\
		*(inst)++ = (pop_stack) ? (unsigned char)0xde : (unsigned char)0xdc;	\
		*(inst)++ = (unsigned char)0xc0+(map[(opc)]<<3)+((index)&0x07);	\
	} while (0)

#define x86_fstp(inst,index)	\
	do {	\
		*(inst)++ = (unsigned char)0xdd;	\
		*(inst)++ = (unsigned char)0xd8+(index);	\
	} while (0)

#define x86_fcompp(inst)	\
	do {	\
		*(inst)++ = (unsigned char)0xde;	\
		*(inst)++ = (unsigned char)0xd9;	\
	} while (0)

#define x86_fnstsw(inst)	\
	do {	\
		*(inst)++ = (unsigned char)0xdf;	\
		*(inst)++ = (unsigned char)0xe0;	\
	} while (0)

#define x86_fnstcw(inst,mem)	\
	do {	\
		*(inst)++ = (unsigned char)0xd9;	\
		x86_mem_emit ((inst), 7, (mem));	\
	} while (0)

#define x86_fnstcw_membase(inst,basereg,disp)	\
	do {	\
		*(inst)++ = (unsigned char)0xd9;	\
		x86_membase_emit ((inst), 7, (basereg), (disp));	\
	} while (0)

#define x86_fldcw(inst,mem)	\
	do {	\
		*(inst)++ = (unsigned char)0xd9;	\
		x86_mem_emit ((inst), 5, (mem));	\
	} while (0)

#define x86_fldcw_membase(inst,basereg,disp)	\
	do {	\
		*(inst)++ = (unsigned char)0xd9;	\
		x86_membase_emit ((inst), 5, (basereg), (disp));	\
	} while (0)

#define x86_fchs(inst)	\
	do {	\
		*(inst)++ = (unsigned char)0xd9;	\
		*(inst)++ = (unsigned char)0xe0;	\
	} while (0)

#define x86_frem(inst)	\
	do {	\
		*(inst)++ = (unsigned char)0xd9;	\
		*(inst)++ = (unsigned char)0xf8;	\
	} while (0)

#define x86_fxch(inst,index)	\
	do {	\
		*(inst)++ = (unsigned char)0xd9;	\
		*(inst)++ = (unsigned char)0xc8 + ((index) & 0x07);	\
	} while (0)

#define x86_fcomip(inst,index)	\
	do {	\
		*(inst)++ = (unsigned char)0xdf;	\
		*(inst)++ = (unsigned char)0xf0 + ((index) & 0x07);	\
	} while (0)

#define x86_fld(inst,mem,is_double)	\
	do {	\
		*(inst)++ = (is_double) ? (unsigned char)0xdd : (unsigned char)0xd9;	\
		x86_mem_emit ((inst), 0, (mem));	\
	} while (0)

#define x86_fld_membase(inst,basereg,disp,is_double)	\
	do {	\
		*(inst)++ = (is_double) ? (unsigned char)0xdd : (unsigned char)0xd9;	\
		x86_membase_emit ((inst), 0, (basereg), (disp));	\
	} while (0)

#define x86_fld80(inst,mem)	\
	do {	\
		*(inst)++ = (unsigned char)0xdb;	\
		x86_mem_emit ((inst), 5, (mem));	\
	} while (0)

#define x86_fld80_membase(inst,basereg,disp)	\
	do {	\
		*(inst)++ = (unsigned char)0xdb;	\
		x86_membase_emit ((inst), 5, (basereg), (disp));	\
	} while (0)

#define x86_fild(inst,mem,is_long)	\
	do {	\
		if ((is_long)) {	\
			*(inst)++ = (unsigned char)0xdf;	\
			x86_mem_emit ((inst), 5, (mem));	\
		} else {	\
			*(inst)++ = (unsigned char)0xdb;	\
			x86_mem_emit ((inst), 0, (mem));	\
		}	\
	} while (0)

#define x86_fild_membase(inst,basereg,disp,is_long)	\
	do {	\
		if ((is_long)) {	\
			*(inst)++ = (unsigned char)0xdf;	\
			x86_membase_emit ((inst), 5, (basereg), (disp));	\
		} else {	\
			*(inst)++ = (unsigned char)0xdb;	\
			x86_membase_emit ((inst), 0, (basereg), (disp));	\
		}	\
	} while (0)

#define x86_fld_reg(inst,index)	\
	do {	\
		*(inst)++ = (unsigned char)0xd9;	\
		*(inst)++ = (unsigned char)0xc0 + ((index) & 0x07);	\
	} while (0)

#define x86_fldz(inst)	\
	do {	\
		*(inst)++ = (unsigned char)0xd9;	\
		*(inst)++ = (unsigned char)0xee;	\
	} while (0)

#define x86_fld1(inst)	\
	do {	\
		*(inst)++ = (unsigned char)0xd9;	\
		*(inst)++ = (unsigned char)0xe8;	\
	} while (0)

#define x86_fst(inst,mem,is_double,pop_stack)	\
	do {	\
		*(inst)++ = (is_double) ? (unsigned char)0xdd: (unsigned char)0xd9;	\
		x86_mem_emit ((inst), 2 + ((pop_stack) ? 1 : 0), (mem));	\
	} while (0)

#define x86_fst_membase(inst,basereg,disp,is_double,pop_stack)	\
	do {	\
		*(inst)++ = (is_double) ? (unsigned char)0xdd: (unsigned char)0xd9;	\
		x86_membase_emit ((inst), 2 + ((pop_stack) ? 1 : 0), (basereg), (disp));	\
	} while (0)

#define x86_fist_pop(inst,mem,is_long)	\
	do {	\
		if ((is_long)) {	\
			*(inst)++ = (unsigned char)0xdf;	\
			x86_mem_emit ((inst), 7, (mem));	\
		} else {	\
			*(inst)++ = (unsigned char)0xdb;	\
			x86_mem_emit ((inst), 3, (mem));	\
		}	\
	} while (0)

#define x86_fist_pop_membase(inst,basereg,disp,is_long)	\
	do {	\
		if ((is_long)) {	\
			*(inst)++ = (unsigned char)0xdf;	\
			x86_membase_emit ((inst), 7, (basereg), (disp));	\
		} else {	\
			*(inst)++ = (unsigned char)0xdb;	\
			x86_membase_emit ((inst), 3, (basereg), (disp));	\
		}	\
	} while (0)

#define x86_push_reg(inst,reg)	\
	do {	\
		*(inst)++ = (unsigned char)0x50 + (reg);	\
	} while (0)

#define x86_push_regp(inst,reg)	\
	do {	\
		*(inst)++ = (unsigned char)0xff;	\
		x86_regp_emit ((inst), 6, (reg));	\
	} while (0)

#define x86_push_mem(inst,mem)	\
	do {	\
		*(inst)++ = (unsigned char)0xff;	\
		x86_mem_emit ((inst), 6, (mem));	\
	} while (0)

#define x86_push_membase(inst,basereg,disp)	\
	do {	\
		*(inst)++ = (unsigned char)0xff;	\
		x86_membase_emit ((inst), 6, (basereg), (disp));	\
	} while (0)

#define x86_push_memindex(inst,basereg,disp,indexreg,shift)	\
	do {	\
		*(inst)++ = (unsigned char)0xff;	\
		x86_memindex_emit ((inst), 6, (basereg), (disp), (indexreg), (shift));	\
	} while (0)

#define x86_push_imm(inst,imm)	\
	do {	\
		*(inst)++ = (unsigned char)0x68;	\
		x86_imm_emit32 ((inst), (imm));	\
	} while (0)

#define x86_pop_reg(inst,reg)	\
	do {	\
		*(inst)++ = (unsigned char)0x58 + (reg);	\
	} while (0)

#define x86_pop_mem(inst,mem)	\
	do {	\
		*(inst)++ = (unsigned char)0x87;	\
		x86_mem_emit ((inst), 0, (mem));	\
	} while (0)

#define x86_pop_membase(inst,basereg,disp)	\
	do {	\
		*(inst)++ = (unsigned char)0x87;	\
		x86_membase_emit ((inst), 0, (basereg), (disp));	\
	} while (0)

#define x86_pushad(inst) do { *(inst)++ = (unsigned char)0x60; } while (0)
#define x86_pushfd(inst) do { *(inst)++ = (unsigned char)0x9c; } while (0)
#define x86_popad(inst)  do { *(inst)++ = (unsigned char)0x61; } while (0)
#define x86_popfd(inst)  do { *(inst)++ = (unsigned char)0x9d; } while (0)

#define x86_jump32(inst,imm)	\
	do {	\
		*(inst)++ = (unsigned char)0xe9;	\
		x86_imm_emit32 ((inst), (imm));	\
	} while (0)

#define x86_jump8(inst,imm)	\
	do {	\
		*(inst)++ = (unsigned char)0xeb;	\
		x86_imm_emit8 ((inst), (imm));	\
	} while (0)

#define x86_jump_reg(inst,reg)	\
	do {	\
		*(inst)++ = (unsigned char)0xff;	\
		x86_reg_emit ((inst), 4, (reg));	\
	} while (0)

#define x86_jump_mem(inst,mem)	\
	do {	\
		*(inst)++ = (unsigned char)0xff;	\
		x86_mem_emit ((inst), 4, (mem));	\
	} while (0)

#define x86_jump_membase(inst,basereg,disp)	\
	do {	\
		*(inst)++ = (unsigned char)0xff;	\
		x86_membase_emit ((inst), 4, (basereg), (disp));	\
	} while (0)

/*
 * target is a pointer in our buffer.
 */
#define x86_jump_code(inst,target)	\
	do {	\
		int t = (unsigned char*)(target) - (inst) - 2;	\
		if (x86_is_imm8(t)) {	\
			x86_jump8 ((inst), t);	\
		} else {	\
			t -= 3;	\
			x86_jump32 ((inst), t);	\
		}	\
	} while (0)

#define x86_jump_disp(inst,disp)	\
	do {	\
		int t = (disp) - 2;	\
		if (x86_is_imm8(t)) {	\
			x86_jump8 ((inst), t);	\
		} else {	\
			t -= 3;	\
			x86_jump32 ((inst), t);	\
		}	\
	} while (0)

#define x86_branch8(inst,cond,imm,is_signed)	\
	do {	\
		if ((is_signed))	\
			*(inst)++ = x86_cc_signed_map [(cond)];	\
		else	\
			*(inst)++ = x86_cc_unsigned_map [(cond)];	\
		x86_imm_emit8 ((inst), (imm));	\
	} while (0)

#define x86_branch32(inst,cond,imm,is_signed)	\
	do {	\
		*(inst)++ = (unsigned char)0x0f;	\
		if ((is_signed))	\
			*(inst)++ = x86_cc_signed_map [(cond)] + 0x10;	\
		else	\
			*(inst)++ = x86_cc_unsigned_map [(cond)] + 0x10;	\
		x86_imm_emit32 ((inst), (imm));	\
	} while (0)

#define x86_branch(inst,cond,target,is_signed)	\
	do {	\
		int offset = (target) - (inst) - 2;	\
		if (x86_is_imm8 ((offset)))	\
			x86_branch8 ((inst), (cond), offset, (is_signed));	\
		else {	\
			offset -= 4;	\
			x86_branch32 ((inst), (cond), offset, (is_signed));	\
		}	\
	} while (0)

#define x86_branch_disp(inst,cond,disp,is_signed)	\
	do {	\
		int offset = (disp) - 2;	\
		if (x86_is_imm8 ((offset)))	\
			x86_branch8 ((inst), (cond), offset, (is_signed));	\
		else {	\
			offset -= 4;	\
			x86_branch32 ((inst), (cond), offset, (is_signed));	\
		}	\
	} while (0)

#define x86_set_reg(inst,cond,reg,is_signed)	\
	do {	\
		*(inst)++ = (unsigned char)0x0f;	\
		if ((is_signed))	\
			*(inst)++ = x86_cc_signed_map [(cond)] + 0x20;	\
		else	\
			*(inst)++ = x86_cc_unsigned_map [(cond)] + 0x20;	\
		x86_reg_emit ((inst), 0, (reg));	\
	} while (0)

#define x86_set_mem(inst,cond,mem,is_signed)	\
	do {	\
		*(inst)++ = (unsigned char)0x0f;	\
		if ((is_signed))	\
			*(inst)++ = x86_cc_signed_map [(cond)] + 0x20;	\
		else	\
			*(inst)++ = x86_cc_unsigned_map [(cond)] + 0x20;	\
		x86_mem_emit ((inst), 0, (mem));	\
	} while (0)

#define x86_set_membase(inst,cond,basereg,disp,is_signed)	\
	do {	\
		*(inst)++ = (unsigned char)0x0f;	\
		if ((is_signed))	\
			*(inst)++ = x86_cc_signed_map [(cond)] + 0x20;	\
		else	\
			*(inst)++ = x86_cc_unsigned_map [(cond)] + 0x20;	\
		x86_membase_emit ((inst), 0, (basereg), (disp));	\
	} while (0)

#define x86_call_imm(inst,disp)	\
	do {	\
		*(inst)++ = (unsigned char)0xe8;	\
		x86_imm_emit32 ((inst), (int)(disp));	\
	} while (0)

#define x86_call_reg(inst,reg)	\
	do {	\
		*(inst)++ = (unsigned char)0xff;	\
		x86_reg_emit ((inst), 2, (reg));	\
	} while (0)

#define x86_call_mem(inst,mem)	\
	do {	\
		*(inst)++ = (unsigned char)0xff;	\
		x86_mem_emit ((inst), 2, (mem));	\
	} while (0)

#define x86_call_membase(inst,basereg,disp)	\
	do {	\
		*(inst)++ = (unsigned char)0xff;	\
		x86_membase_emit ((inst), 2, (basereg), (disp));	\
	} while (0)

#define x86_call_code(inst,target)	\
	do {	\
		int offset = (unsigned char*)(target) - (inst);	\
		offset -= 5;	\
		x86_call_imm ((inst), offset);	\
	} while (0)

#define x86_ret(inst) do { *(inst)++ = (unsigned char)0xc3; } while (0)

#define x86_ret_imm(inst,imm)	\
	do {	\
		if ((imm) == 0) {	\
			x86_ret ((inst));	\
		} else {	\
			*(inst)++ = (unsigned char)0xc2;	\
			x86_imm_emit16 ((inst), (imm));	\
		}	\
	} while (0)

#define x86_cmov_reg(inst,cond,is_signed,dreg,reg)	\
	do {	\
		*(inst)++ = (unsigned char) 0x0f;	\
		if ((is_signed))	\
			*(inst)++ = x86_cc_signed_map [(cond)] - 0x30;	\
		else	\
			*(inst)++ = x86_cc_unsigned_map [(cond)] - 0x30;	\
		x86_reg_emit ((inst), (dreg), (reg));	\
	} while (0)

#define x86_cmov_mem(inst,cond,is_signed,reg,mem)	\
	do {	\
		*(inst)++ = (unsigned char) 0x0f;	\
		if ((is_signed))	\
			*(inst)++ = x86_cc_signed_map [(cond)] - 0x30;	\
		else	\
			*(inst)++ = x86_cc_unsigned_map [(cond)] - 0x30;	\
		x86_mem_emit ((inst), (reg), (mem));	\
	} while (0)

#define x86_cmov_membase(inst,cond,is_signed,reg,basereg,disp)	\
	do {	\
		*(inst)++ = (unsigned char) 0x0f;	\
		if ((is_signed))	\
			*(inst)++ = x86_cc_signed_map [(cond)] - 0x30;	\
		else	\
			*(inst)++ = x86_cc_unsigned_map [(cond)] - 0x30;	\
		x86_membase_emit ((inst), (reg), (basereg), (disp));	\
	} while (0)

#define x86_enter(inst,framesize)	\
	do {	\
		*(inst)++ = (unsigned char)0xc8;	\
		x86_imm_emit16 ((inst), (framesize));	\
		*(inst)++ = 0;	\
	} while (0)
	
#define x86_leave(inst) do { *(inst)++ = (unsigned char)0xc9; } while (0)
#define x86_sahf(inst)  do { *(inst)++ = (unsigned char)0x9e; } while (0)

#define x86_fsin(inst) do { *(inst)++ = (unsigned char)0xd9; *(inst)++ = (unsigned char)0xfe; } while (0)
#define x86_fcos(inst) do { *(inst)++ = (unsigned char)0xd9; *(inst)++ = (unsigned char)0xff; } while (0)
#define x86_fabs(inst) do { *(inst)++ = (unsigned char)0xd9; *(inst)++ = (unsigned char)0xe1; } while (0)
#define x86_fpatan(inst) do { *(inst)++ = (unsigned char)0xd9; *(inst)++ = (unsigned char)0xf3; } while (0)
#define x86_fprem(inst) do { *(inst)++ = (unsigned char)0xd9; *(inst)++ = (unsigned char)0xf8; } while (0)
#define x86_fprem1(inst) do { *(inst)++ = (unsigned char)0xd9; *(inst)++ = (unsigned char)0xf5; } while (0)
#define x86_frndint(inst) do { *(inst)++ = (unsigned char)0xd9; *(inst)++ = (unsigned char)0xfc; } while (0)
#define x86_fsqrt(inst) do { *(inst)++ = (unsigned char)0xd9; *(inst)++ = (unsigned char)0xfa; } while (0)
#define x86_fptan(inst) do { *(inst)++ = (unsigned char)0xd9; *(inst)++ = (unsigned char)0xf2; } while (0)

#define x86_padding(inst,size)	\
	do {	\
		switch ((size)) {	\
		case 1: x86_nop ((inst)); break;	\
		case 2: *(inst)++ = 0x8b;	\
			*(inst)++ = 0xc0; break;	\
		case 3: *(inst)++ = 0x8d; *(inst)++ = 0x6d;	\
			*(inst)++ = 0x00; break;	\
		case 4: *(inst)++ = 0x8d; *(inst)++ = 0x64;	\
			*(inst)++ = 0x24; *(inst)++ = 0x00;	\
			break;	\
		case 5: *(inst)++ = 0x8d; *(inst)++ = 0x64;	\
			*(inst)++ = 0x24; *(inst)++ = 0x00;	\
			x86_nop ((inst)); break;	\
		case 6: *(inst)++ = 0x8d; *(inst)++ = 0xad;	\
			*(inst)++ = 0x00; *(inst)++ = 0x00;	\
			*(inst)++ = 0x00; *(inst)++ = 0x00;	\
			break;	\
		case 7: *(inst)++ = 0x8d; *(inst)++ = 0xa4;	\
			*(inst)++ = 0x24; *(inst)++ = 0x00;	\
			*(inst)++ = 0x00; *(inst)++ = 0x00;	\
			*(inst)++ = 0x00; break;	\
		default: assert (0);	\
		}	\
	} while (0)

#define x86_prolog(inst,frame_size,reg_mask)	\
	do {	\
		unsigned i, m = 1;	\
		x86_enter ((inst), (frame_size));	\
		for (i = 0; i < X86_NREG; ++i, m <<= 1) {	\
			if ((reg_mask) & m)	\
				x86_push_reg ((inst), i);	\
		}	\
	} while (0)

#define x86_epilog(inst,reg_mask)	\
	do {	\
		unsigned i, m = 1 << X86_EDI;	\
		for (i = X86_EDI; m != 0; i--, m=m>>1) {	\
			if ((reg_mask) & m)	\
				x86_pop_reg ((inst), i);	\
		}	\
		x86_leave ((inst));	\
		x86_ret ((inst));	\
	} while (0)

#endif // X86_H
