#include "config.h"
#include <stdlib.h>
#include <string.h>

#define NO_MIPS_JIT_DEBUG

#include "mips-codegen.h"
#include "mono/metadata/class.h"

/* don't run the resulting program, it will destroy your computer,
 * just objdump -d it to inspect we generated the correct assembler.
 */

int main (int argc, char *argv[]) {
	guint32 *code, * p;

	code = p = (guint32 *) malloc (sizeof (guint32) * 1024);
	
	mips_add (p, 3, 4, 5);
	mips_addi (p, 3, 4, 5);
	mips_addu (p, 3, 4, 5);
	mips_addiu (p, 3, 4, 5);
	mips_sub (p, 3, 4, 5);
	mips_subu (p, 3, 4, 5);
	mips_dadd (p, 3, 4, 5);
	mips_daddi (p, 3, 4, 5);
	mips_daddu (p, 3, 4, 5);
	mips_daddiu (p, 3, 4, 5);
	mips_dsub (p, 3, 4, 5);
	mips_dsubu (p, 3, 4, 5);

	mips_mult (p, 6, 7);
	mips_multu (p, 6, 7);
	mips_div (p, 6, 7);
	mips_divu (p, 6, 7);
	mips_dmult (p, 6, 7);
	mips_dmultu (p, 6, 7);
	mips_ddiv (p, 6, 7);
	mips_ddivu (p, 6, 7);

	mips_sll (p, 3, 4, 5);
	mips_sllv (p, 3, 4, 5);
	mips_sra (p, 3, 4, 5);
	mips_srav (p, 3, 4, 5);
	mips_srl (p, 3, 4, 5);
	mips_srlv (p, 3, 4, 5);
	mips_dsll (p, 3, 4, 5);
	mips_dsll32 (p, 3, 4, 5);
	mips_dsllv (p, 3, 4, 5);
	mips_dsra (p, 3, 4, 5);
	mips_dsra32 (p, 3, 4, 5);
	mips_dsrav (p, 3, 4, 5);
	mips_dsrl (p, 3, 4, 5);
	mips_dsrl32 (p, 3, 4, 5);
	mips_dsrlv (p, 3, 4, 5);

	mips_and (p, 8, 9, 10);
	mips_andi (p, 8, 9, 10);
	mips_nor (p, 8, 9, 10);
	mips_or (p, 8, 9, 10);
	mips_ori (p, 8, 9, 10);
	mips_xor (p, 8, 9, 10);
	mips_xori (p, 8, 9, 10);

	mips_slt (p, 8, 9, 10);
	mips_slti (p, 8, 9, 10);
	mips_sltu (p, 8, 9, 10);
	mips_sltiu (p, 8, 9, 10);

	mips_beq (p, 8, 9, 0xff1f);
	mips_beql (p, 8, 9, 0xff1f);
	mips_bne (p, 8, 9, 0xff1f);
	mips_bnel (p, 8, 9, 0xff1f);
	mips_bgez (p, 11, 0xff1f);
	mips_bgezal (p, 11, 0xff1f);
	mips_bgezall (p, 11, 0xff1f);
	mips_bgezl (p, 11, 0xff1f);
	mips_bgtz (p, 11, 0xff1f);
	mips_bgtzl (p, 11, 0xff1f);
	mips_blez (p, 11, 0xff1f);
	mips_blezl (p, 11, 0xff1f);
	mips_bltz (p, 11, 0xff1f);
	mips_bltzal (p, 11, 0xff1f);
	mips_bltzall (p, 11, 0xff1f);
	mips_bltzl (p, 11, 0xff1f);

	mips_jump (p, 0xff1f);
	mips_jumpl (p, 0xff1f);
	mips_jalr (p, 12, mips_ra);
	mips_jr (p, 12);

	mips_lb (p, 13, 14, 128);
	mips_lbu (p, 13, 14, 128);
	mips_ld (p, 13, 14, 128);
	mips_ldl (p, 13, 14, 128);
	mips_ldr (p, 13, 14, 128);
	mips_lh (p, 13, 14, 128);
	mips_lhu (p, 13, 14, 128);
	mips_ll (p, 13, 14, 128);
	mips_lld (p, 13, 14, 128);
	mips_lui (p, 13, 14, 128);
	mips_lw (p, 13, 14, 128);
	mips_lwl (p, 13, 14, 128);
	mips_lwr (p, 13, 14, 128);
	mips_lwu (p, 13, 14, 128);
	mips_sb (p, 13, 14, 128);
	mips_sc (p, 13, 14, 128);
	mips_scd (p, 13, 14, 128);
	mips_sd (p, 13, 14, 128);
	mips_sdl (p, 13, 14, 128);
	mips_sdr (p, 13, 14, 128);
	mips_sh (p, 13, 14, 128);
	mips_sw (p, 13, 14, 128);
	mips_swl (p, 13, 14, 128);
	mips_swr (p, 13, 14, 128);

	mips_move (p, 15, 16);
	mips_nop (p);
	mips_break (p, 0);
	mips_sync (p, 0);
	mips_mfhi (p, 17);
	mips_mflo (p, 17);
	mips_mthi (p, 17);
	mips_mtlo (p, 17);

	mips_fabsd (p, 16, 18);
	mips_fnegd (p, 16, 18);
	mips_fsqrtd (p, 16, 18);
	mips_faddd (p, 16, 18, 20);
	mips_fdivd (p, 16, 18, 20);
	mips_fmuld (p, 16, 18, 20);
	mips_fsubd (p, 16, 18, 20);

	mips_fcmpd (p, MIPS_FPU_EQ, 18, 20);
	mips_fbfalse (p, 0xff1f);
	mips_fbfalsel (p, 0xff1f);
	mips_fbtrue (p, 0xff1f);
	mips_fbtruel (p, 0xff1f);

	mips_ceilwd (p, 20, 22);
	mips_ceilld (p, 20, 22);
	mips_floorwd (p, 20, 22);
	mips_floorld (p, 20, 22);
	mips_roundwd (p, 20, 22);
	mips_roundld (p, 20, 22);
	mips_truncwd (p, 20, 22);
	mips_truncld (p, 20, 22);
	mips_cvtdw (p, 20, 22);
	mips_cvtds (p, 20, 22);
	mips_cvtdl (p, 20, 22);
	mips_cvtld (p, 20, 22);
	mips_cvtsd (p, 20, 22);
	mips_cvtwd (p, 20, 22);

	mips_fmovd (p, 20, 22);
	printf ("size: %d\n", p - code);

	return 0;
}
