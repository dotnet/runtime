#include <glib.h>
#include "sparc-codegen.h"

/* don't run the resulting program, it will destroy your computer,
 * just objdump -d it to inspect we generated the correct assembler.
 */

int
main ()
{
	guint32 *p;
	guint32 code_buffer [500];
	guint32 local_size = 0, stack_size = 0, code_size = 6;
	guint32 arg_pos, simpletype;
	unsigned char *ins;
	int i, stringp, cur_out_reg, size;

	p = code_buffer;

	printf (".text\n.align 4\n.globl main\n.type main,@function\nmain:\n");

	/*
	 * Standard function prolog.
	 */
	sparc_save_imm (p, sparc_sp, -112-stack_size, sparc_sp);
	cur_out_reg = sparc_o0;
	arg_pos = 0;

	if (1) {
		sparc_mov_reg_reg (p, sparc_i2, cur_out_reg);
		++cur_out_reg;
	}

	sparc_ld_imm (p, sparc_i3, arg_pos, cur_out_reg);
	++cur_out_reg;
	sparc_ld_imm (p, sparc_i3, arg_pos+4, cur_out_reg);
	++cur_out_reg;
	/* 
	 * Insert call to function 
	 */
	sparc_jmpl (p, sparc_i0, 0, sparc_callsite);
	sparc_nop (p);

	sparc_jmpl_imm (p, sparc_i7, 8, sparc_zero);
	sparc_restore (p, sparc_zero, sparc_zero, sparc_zero);

	sparc_ldsb (p, sparc_i3, sparc_l0, sparc_o5);
	sparc_ldsb_imm (p, sparc_i3, 2, sparc_o5);

	sparc_ldsh (p, sparc_i3, sparc_l0, sparc_o5);
	sparc_ldsh_imm (p, sparc_i3, 2, sparc_o5);

	sparc_ldub (p, sparc_i3, sparc_l0, sparc_o5);
	sparc_ldub_imm (p, sparc_i3, 2, sparc_o5);

	sparc_lduh (p, sparc_i3, sparc_l0, sparc_o5);
	sparc_lduh_imm (p, sparc_i3, 2, sparc_o5);

	sparc_ldf (p, sparc_i3, sparc_l0, sparc_o5);
	sparc_ldf_imm (p, sparc_i3, 2, sparc_o5);

	sparc_stb (p, sparc_i3, sparc_l0, sparc_l2);
	sparc_stb_imm (p, sparc_i3, sparc_o5, 2);

	sparc_sethi (p, 0xff000000, sparc_o2);
	sparc_rdy (p, sparc_l0);
	sparc_wry (p, sparc_l0, sparc_l1);
	sparc_wry_imm (p, sparc_l0, 16);
	sparc_stbar (p);
	sparc_unimp (p, 24);
	sparc_flush (p, sparc_l4, 0);

	sparc_and (p, sparc_cc, sparc_l0, sparc_l1, sparc_o1);
	sparc_and_imm (p, FALSE, sparc_l0, 0xff, sparc_o1);
	sparc_andn (p, sparc_cc, sparc_l0, sparc_l1, sparc_o1);
	sparc_or (p, sparc_cc, sparc_l0, sparc_l1, sparc_o1);
	sparc_orn (p, sparc_cc, sparc_l0, sparc_l1, sparc_o1);
	sparc_xor (p, sparc_cc, sparc_l0, sparc_l1, sparc_o1);
	sparc_xnor (p, sparc_cc, sparc_l0, sparc_l1, sparc_o1);

	sparc_sll (p, sparc_l0, sparc_l1, sparc_o1);
	sparc_sll_imm (p, sparc_l0, 2, sparc_o1);
	sparc_srl (p, sparc_l0, sparc_l1, sparc_o1);
	sparc_srl_imm (p, sparc_l0, 2, sparc_o1);
	sparc_sra (p, sparc_l0, sparc_l1, sparc_o1);
	sparc_sra_imm (p, sparc_l0, 2, sparc_o1);

	sparc_add (p, sparc_cc, sparc_l0, sparc_l1, sparc_o1);
	sparc_add_imm (p, FALSE, sparc_l0, 0xff, sparc_o1);
	sparc_addx (p, sparc_cc, sparc_l0, sparc_l1, sparc_o1);
	sparc_sub (p, sparc_cc, sparc_l0, sparc_l1, sparc_o1);
	sparc_subx (p, sparc_cc, sparc_l0, sparc_l1, sparc_o1);

	sparc_muls (p, sparc_l0, sparc_l1, sparc_o1);
	sparc_umul (p, sparc_cc, sparc_l0, sparc_l1, sparc_o1);
	sparc_smul (p, sparc_cc, sparc_l0, sparc_l1, sparc_o1);
	sparc_udiv (p, sparc_cc, sparc_l0, sparc_l1, sparc_o1);
	sparc_sdiv (p, sparc_cc, sparc_l0, sparc_l1, sparc_o1);

	sparc_branch (p, FALSE, sparc_bne, -12);
	sparc_ret (p);
	sparc_retl (p);
	sparc_test (p, sparc_l4);
	sparc_cmp (p, sparc_l4, sparc_l6);
	sparc_cmp_imm (p, sparc_l4, 4);
	sparc_restore_simple (p);

	sparc_set (p, 0xff000000, sparc_l7);
	sparc_set (p, 1, sparc_l7);
	sparc_set (p, 0xff0000ff, sparc_l7);

	sparc_not (p, sparc_g2);
	sparc_neg (p, sparc_g3);
	sparc_clr_reg (p, sparc_g4);


	size = (p-code_buffer)*4;
	ins = (gchar*)code_buffer;
	for (i = 0; i < size; ++i)
		printf (".byte %d\n", (unsigned int) ins [i]);
	return 0;
}

