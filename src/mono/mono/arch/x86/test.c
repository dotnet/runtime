#include "x86-codegen.h"
#include <stdio.h>

/* don't run the resulting program, it will destroy your computer,
 * just objdump -d it to inspect we generated the correct assembler.
 */

int main() {
	unsigned char code [16000];
	unsigned char *p = code;
	unsigned char *target, *start, *end;
	unsigned long mem_addr = 0xdeadbeef;
	int size, i;

	printf (".text\n.align 4\n.globl main\n.type main,@function\nmain:\n");

	x86_prolog (p, 16, X86_CALLER_REGS);

	x86_cmpxchg_reg_reg (p, X86_EAX, X86_EBP);
	x86_cmpxchg_membase_reg (p, X86_EAX, 12, X86_EBP);

	x86_xchg_reg_reg (p, X86_EAX, X86_EBP, 4);
	x86_xchg_reg_reg (p, X86_EAX, X86_EBP, 1); // FIXME?
	x86_xchg_membase_reg (p, X86_EAX, 12, X86_EBP, 4);
	x86_xchg_membase_reg (p, X86_EAX, 12, X86_EBP, 2);
	x86_xchg_membase_reg (p, X86_EAX, 12, X86_EBX, 1); // FIXME?

	x86_inc_reg (p, X86_EAX);
	x86_inc_mem (p, mem_addr);
	x86_inc_membase (p, X86_ESP, 4);
	
	x86_nop (p);
	x86_nop (p);
	
	x86_dec_reg (p, X86_EAX);
	x86_dec_reg (p, X86_ECX);
	x86_dec_mem (p, mem_addr);
	x86_dec_membase (p, X86_ESP, 4);
	
	x86_not_reg (p, X86_EDX);
	x86_not_reg (p, X86_ECX);
	x86_not_mem (p, mem_addr);
	x86_not_membase (p, X86_ESP, 4);
	x86_not_membase (p, X86_ESP, 0x4444444);
	x86_not_membase (p, X86_EBP, 0x4444444);
	x86_not_membase (p, X86_ECX, 0x4444444);
	x86_not_membase (p, X86_EDX, 0);
	x86_not_membase (p, X86_EBP, 0);

	x86_neg_reg (p, X86_EAX);
	x86_neg_reg (p, X86_ECX);
	x86_neg_mem (p, mem_addr);
	x86_neg_membase (p, X86_ESP, 8);

	x86_alu_reg_imm (p, X86_ADD, X86_EAX, 5);
	x86_alu_reg_imm (p, X86_ADD, X86_EBX, -10);
	x86_alu_reg_imm (p, X86_SUB, X86_EDX, 7);
	x86_alu_reg_imm (p, X86_OR, X86_ESP, 0xffffedaf);
	x86_alu_reg_imm (p, X86_CMP, X86_ECX, 1);
	x86_alu_mem_imm (p, X86_ADC, mem_addr, 2);
	x86_alu_membase_imm (p, X86_ADC, X86_ESP, -4, 4);
	x86_alu_membase_imm (p, X86_ADC, X86_ESP, -12, 0xffffedaf);

	x86_alu_mem_reg (p, X86_SUB, mem_addr, X86_EDX);
	x86_alu_reg_reg (p, X86_ADD, X86_EAX, X86_EBX);
	x86_alu_reg_mem (p, X86_ADD, X86_EAX, mem_addr);
	x86_alu_reg_imm (p, X86_ADD, X86_EAX, 0xdeadbeef);
	x86_alu_reg_membase (p, X86_XOR, X86_EDX, X86_ESP, 4);
	x86_alu_membase_reg (p, X86_XOR, X86_EBP, 8, X86_ESI);

	x86_test_reg_imm (p, X86_EAX, 16);
	x86_test_reg_imm (p, X86_EDX, -16);
	x86_test_mem_imm (p, mem_addr, 1);
	x86_test_membase_imm (p, X86_EBP, 8, 1);

	x86_test_reg_reg (p, X86_EAX, X86_EDX);
	x86_test_mem_reg (p, mem_addr, X86_EDX);
	x86_test_membase_reg (p, X86_ESI, 4, X86_EDX);

	x86_shift_reg_imm (p, X86_SHL, X86_EAX, 1);
	x86_shift_reg_imm (p, X86_SHL, X86_EDX, 2);

	x86_shift_mem_imm (p, X86_SHL, mem_addr, 2);
	x86_shift_membase_imm (p, X86_SHLR, X86_EBP, 8, 4);

	/*
	 * Shift by CL
	 */
	x86_shift_reg (p, X86_SHL, X86_EAX);
	x86_shift_mem (p, X86_SHL, mem_addr);

	x86_mul_reg (p, X86_EAX, 0);
	x86_mul_reg (p, X86_EAX, 1);
	x86_mul_membase (p, X86_EBP, 8, 1);

	x86_imul_reg_reg (p, X86_EBX, X86_EDX);
	x86_imul_reg_membase (p, X86_EBX, X86_EBP, 12);

	x86_imul_reg_reg_imm (p, X86_EBX, X86_EDX, 10);
	x86_imul_reg_mem_imm (p, X86_EBX, mem_addr, 20);
	x86_imul_reg_membase_imm (p, X86_EBX, X86_EBP, 16, 300);

	x86_div_reg (p, X86_EDX, 0);
	x86_div_reg (p, X86_EDX, 1);
	x86_div_mem (p, mem_addr, 1);
	x86_div_membase (p, X86_ESI, 4, 1);

	x86_mov_mem_reg (p, mem_addr, X86_EAX, 4);
	x86_mov_mem_reg (p, mem_addr, X86_EAX, 2);
	x86_mov_mem_reg (p, mem_addr, X86_EAX, 1);
	x86_mov_membase_reg (p, X86_EBP, 4, X86_EAX, 1);

	x86_mov_regp_reg (p, X86_EAX, X86_EAX, 4);
	x86_mov_membase_reg (p, X86_EAX, 0, X86_EAX, 4);
	x86_mov_reg_membase (p, X86_EAX, X86_EAX, 0, 4);
	x86_mov_reg_memindex (p, X86_ECX, X86_EAX, 34, X86_EDX, 2, 4);
	x86_mov_reg_memindex (p, X86_ECX, X86_NOBASEREG, 34, X86_EDX, 2, 4);
	x86_mov_memindex_reg (p, X86_EAX, X86_EAX, 0, X86_EDX, 2, 4);
	x86_mov_reg_reg (p, X86_EAX, X86_EAX, 1);
	x86_mov_reg_reg (p, X86_EAX, X86_EAX, 4);
	x86_mov_reg_mem (p, X86_EAX, mem_addr, 4);
	
	x86_mov_reg_imm (p, X86_EAX, 10);
	x86_mov_mem_imm (p, mem_addr, 54, 4);
	x86_mov_mem_imm (p, mem_addr, 54, 1);

	x86_lea_mem (p, X86_EDX, mem_addr);
	/* test widen */
	x86_widen_memindex (p, X86_EDX, X86_ECX, 0, X86_EBX, 2, 1, 0);
	
	x86_cdq (p);
	x86_wait (p);

	x86_fp_op_mem (p, X86_FADD, mem_addr, 1);
	x86_fp_op_mem (p, X86_FSUB, mem_addr, 0);
	x86_fp_op (p, X86_FSUB, 2);
	x86_fp_op_reg (p, X86_FMUL, 1, 0);
	x86_fstp (p, 2);
	x86_fcompp (p);
	x86_fnstsw (p);
	x86_fnstcw (p, mem_addr);
	x86_fnstcw_membase (p, X86_ESP, -8);

	x86_fldcw_membase (p, X86_ESP, -8);
	x86_fchs (p);
	x86_frem (p);
	x86_fxch (p, 3);
	x86_fcomip (p, 3);
	x86_fld_membase (p, X86_ESP, -8, 1);
	x86_fld_membase (p, X86_ESP, -8, 0);
	x86_fld80_membase (p, X86_ESP, -8);
	x86_fild_membase (p, X86_ESP, -8, 1);
	x86_fild_membase (p, X86_ESP, -8, 0);
	x86_fld_reg (p, 4);
	x86_fldz (p);
	x86_fld1 (p);
	
	x86_fst (p, mem_addr, 1, 0);
	x86_fst (p, mem_addr, 1, 1);
	x86_fst (p, mem_addr, 0, 1);
	
	x86_fist_pop_membase (p, X86_EDX, 4, 1);
	x86_fist_pop_membase (p, X86_EDX, 4, 0);

	x86_push_reg (p, X86_EBX);
	x86_push_membase (p, X86_EBP, 8);
	x86_push_imm (p, -1);
	x86_pop_reg (p, X86_EBX);

	x86_pushad (p);
	x86_pushfd (p);
	x86_popfd (p);
	x86_popad (p);

	target = p;

	start = p;
	x86_jump32 (p, mem_addr);
	x86_patch (start, target);
	start = p;
	x86_jump8 (p, 12);
	x86_patch (start, target);
	x86_jump_reg (p, X86_EAX);
	x86_jump_membase (p, X86_EDX, 16);

	x86_jump_code (p, target);

	x86_branch8 (p, X86_CC_EQ, 54, 1);
	x86_branch32 (p, X86_CC_LT, 54, 0);
	x86_branch (p, X86_CC_GT, target, 0);
	x86_branch_disp (p, X86_CC_NE, -4, 0);

	x86_set_reg (p, X86_CC_EQ, X86_EAX, 0);
	x86_set_membase (p, X86_CC_LE, X86_EBP, -8, 0);

	x86_call_code (p, printf);
	x86_call_reg (p, X86_ECX);

	x86_sahf (p);
	
	x86_fsin (p);
	x86_fcos (p);
	x86_fabs (p);
	x86_fpatan (p);
	x86_fprem (p);
	x86_fprem1 (p);
	x86_frndint (p);
	x86_fsqrt (p);
	x86_fptan (p);
	
	x86_leave (p);
	x86_ret (p);
	x86_ret_imm (p, 24);
	
	x86_cmov_reg (p, X86_CC_GT, 1, X86_EAX, X86_EDX);
	x86_cmov_membase (p, X86_CC_GT, 0, X86_EAX, X86_EDX, -4);

	x86_nop (p);
	x86_epilog (p, X86_CALLER_REGS);

	size = p-code;
	for (i = 0; i < size; ++i)
		printf (".byte %d\n", (unsigned int) code [i]);
	return 0;
}
