#include "ppc-codegen.h"
#include <stdio.h>

/* don't run the resulting program, it will destroy your computer,
 * just objdump -d it to inspect we generated the correct assembler.
 */

int main() {
	guint8 code [16000];
	guint8 *p = code;
	guint8 *cp;

	printf (".text\n.align 4\n.globl main\n.type main,@function\nmain:\n");

	stwu (p, r1, -32, r1);
	mflr (p, r0);
	stw  (p, r31, 28, r1);
	or   (p, r1, r2, r3);
	mr   (p, r31, r1);
	lwz  (p, r11, 0, r1);
	mtlr (p, r0);
	blr  (p);
	addi (p, r6, r6, 16);

	for (cp = code; cp < p; cp++) {
		printf (".byte 0x%x\n", *cp);
	}

	return 0;
}
