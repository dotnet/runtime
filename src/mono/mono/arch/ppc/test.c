#include "ppc-codegen.h"
#include <stdio.h>

/* don't run the resulting program, it will destroy your computer,
 * just objdump -d it to inspect we generated the correct assembler.
 * On Mac OS X use otool[64] -v -t
 */

int main() {
	guint8 code [16000];
	guint8 *p = code;
	guint8 *cp;

	printf (".text\n.align 4\n.globl main\n");
#ifndef __APPLE__
	printf (".type main,@function\n");
#endif
	printf ("main:\n");

	ppc_stwu (p, ppc_r1, -32, ppc_r1);
	ppc_mflr (p, ppc_r0);
	ppc_stw  (p, ppc_r31, 28, ppc_r1);
	ppc_or   (p, ppc_r1, ppc_r2, ppc_r3);
	ppc_mr   (p, ppc_r31, ppc_r1);
	ppc_lwz  (p, ppc_r11, 0, ppc_r1);
	ppc_mtlr (p, ppc_r0);
	ppc_blr  (p);
	ppc_addi (p, ppc_r6, ppc_r6, 16);
		     
	for (cp = code; cp < p; cp++) {
		printf (".byte 0x%x\n", *cp);
	}	     
		     
	return 0;    
}
