#include "alpha-codegen.h"

#include <sys/types.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>

/* A typical Alpha stack frame looks like this */
/*
fun:			     // called from outside the module.
        ldgp gp,0(pv)        // load the global pointer
fun..ng:		     // called from inside the module.
	lda sp, -SIZE( sp )  // grow the stack downwards.

	stq ra, 0(sp)        // save the return address.

	stq s0, 8(sp)        // callee-saved registers.
	stq s1, 16(sp)       // ...

	// Move the arguments to the argument registers...
	
    	mov addr, pv         // Load the callee address
	jsr  ra, (pv)        // call the method.
	ldgp gp, 0(ra)	     // restore gp

	// return value is in v0
	
	ldq ra, 0(sp)        // free stack frame
	ldq s0, 8(sp)        // restore callee-saved registers.
	ldq s1, 16(sp)       
	ldq sp, 32(sp)       // restore stack pointer

	ret zero, (ra), 1    // return.
*/



//
// Simple function which returns 10.
//
static int testfunc(void)
{
    return 10;
}

// Write it using the known asm bytecodes.
static unsigned int * write_testfunc_1(unsigned int * p )
{
//
//                               ldah     gp, 0(pv)
//                               lda      gp, 0(gp)
//00000001200004d0 <testfunc>:
//   1200004d0:   f0 ff de 23     lda     sp,-16(sp)
//   1200004d4:   00 00 5e b7     stq     ra,0(sp)
//   1200004d8:   08 00 fe b5     stq     fp,8(sp)
//   1200004dc:   0f 04 fe 47     mov     sp,fp
//   1200004e0:   0a 00 3f 20     lda     t0,10
//   1200004e4:   00 04 e1 47     mov     t0,v0
//   1200004e8:   1e 04 ef 47     mov     fp,sp
//   1200004ec:   00 00 5e a7     ldq     ra,0(sp)
//   1200004f0:   08 00 fe a5     ldq     fp,8(sp)
//   1200004f4:   10 00 de 23     lda     sp,16(sp)
//   1200004f8:   01 80 fa 6b     ret

int _func_code[] = {
    0x23defff0,
    0xb75e0000,
    0xb5fe0008,
    0x47fe040f,
    0x203f000a,
    0x47e10400,    
    0x47ef041e,
    0xa75e0000,
    0xa5fe0008,
    0x23de0010,
    0x6bfa8001 };

    memcpy( p , _func_code, 4 * 11 );
    return p + ( 4 * 11 );
}

// The same function encoded with alpha-codegen.h
unsigned int * write_testfunc_2( unsigned int * p )
{    
    alpha_ldah( p, alpha_gp, alpha_pv, 0 );  // start the gp load
    alpha_lda( p, alpha_sp, alpha_sp, -16 ); // allocate the stack
    alpha_lda( p, alpha_gp, alpha_gp, 0 );   // finish the gp load
    alpha_stq( p, alpha_ra, alpha_sp, 0 );   // start param save.
    alpha_stq( p, alpha_fp, alpha_sp, 8 );
    alpha_mov1( p, alpha_sp, alpha_fp );
    alpha_lda( p, alpha_t0, alpha_zero, 10 );
    alpha_mov1( p, alpha_t0, alpha_v0 );
    alpha_mov1( p, alpha_fp, alpha_sp );
    alpha_ldq( p, alpha_ra, alpha_sp, 0 );
    alpha_ldq( p, alpha_fp, alpha_sp, 8 );
    alpha_lda( p, alpha_sp, alpha_sp, 16 );

    alpha_ret( p, alpha_ra, 1 );

    return p;
}


void output( char * p, int len )
{
	char * maxp = p + len;
	char * cp = p;

        printf (".text\n.align 4\n.globl main\n.type main,@function\nmain:\n");
        for ( ; cp < maxp; cp++ ) 
	{
                printf (".byte 0x%0.2x\n", (*cp&0x00ff) );
        }

	int fd = open( "bad.out", O_CREAT | O_TRUNC );
	write( fd, p, len );
	close( fd );
}

unsigned int code [16000/4];

int main( int argc, char ** argv ) {
//	unsigned int code [16000/4];
	unsigned int *p = code;
	unsigned int * cp;

	int (*x)() = 0;
	int y = 0;
	int z = 10;

	// so, `test blah` gets you the byte-encoded function.
	// and  `test` gets you the alpha-codegen.h encoded function.

	if( argc > 1 )
	{
	    p = write_testfunc_1( p );
	}
	else
	{
	    p = write_testfunc_2( p );
	}

	// output( code, p-code );

	// call the procedure.
	x = (int(*)())code;

	while( z-- > 0 )
	    y = x();

	return 0;
}

