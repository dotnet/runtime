#include "alpha-codegen.h"

#include <sys/types.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <stdio.h>


//
// Simple function which returns 10.
//
char * write_testfunc_1( char * p )
{
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


//   1200004d0:   f0 ff de 23     lda     sp,-16(sp)
	*p++ = 0xf0; *p++ = 0xff;*p++ = 0xde;*p++ = 0x23;
//   1200004d4:   00 00 5e b7     stq     ra,0(sp)
	*p++ = 0x00; *p++ = 0x00;*p++ = 0x5e;*p++ = 0xb7;
//   1200004d8:   08 00 fe b5     stq     fp,8(sp)
	*p++ = 0x08; *p++ = 0x00;*p++ = 0xfe;*p++ = 0xb5;
//   1200004dc:   0f 04 fe 47     mov     sp,fp
	*p++ = 0x0f; *p++ = 0x04;*p++ = 0xfe;*p++ = 0x47;
//   1200004e0:   0a 00 3f 20     lda     t0,10
	*p++ = 0x0a; *p++ = 0x00;*p++ = 0x3f;*p++ = 0x20;
//   1200004e4:   00 04 e1 47     mov     t0,v0
	*p++ = 0x00; *p++ = 0x04;*p++ = 0xe1;*p++ = 0x47;
//   1200004e8:   1e 04 ef 47     mov     fp,sp
	*p++ = 0x1e; *p++ = 0x04;*p++ = 0xef;*p++ = 0x47;
//   1200004ec:   00 00 5e a7     ldq     ra,0(sp)
	*p++ = 0x00; *p++ = 0x00;*p++ = 0x5e;*p++ = 0xa7;
//   1200004f0:   08 00 fe a5     ldq     fp,8(sp)
	*p++ = 0x08; *p++ = 0x00;*p++ = 0xfe;*p++ = 0xa5;
//   1200004f4:   10 00 de 23     lda     sp,16(sp)
	*p++ = 0x10; *p++ = 0x00;*p++ = 0xde;*p++ = 0x23;
//   1200004f8:   01 80 fa 6b     ret
	*p++ = 0x01; *p++ = 0x80;*p++ = 0xfa;*p++ = 0x6b;
	return p;
}

#define t0 1
#define t1 2
#define a0 16
#define sp 30
#define gp 29 
#define t12 27
#define ra 26
#define v0 0

// The same function encoded with alpha-codegen.h
// Still needs work on the fp insns.
//
char * write_testfunc_2( char * p )
{
	alpha_lda( p, sp, sp, -16 );
	alpha_stq( p, ra, sp, 0 );
//	alpha_stq( p, fp, sp, 8 );
//	alpha_mov1( p, sp, fp );
	alpha_lda( p, t0, alpha_reg_zero, 10 );
	alpha_mov1( p, t0, v0 );
//	alpha_mov1( p, sp, fp );
	alpha_ldq( p, ra, sp, 0 );
//	alpha_ldq( p, fp, sp, 8 );
	alpha_lda( p, sp, sp, 16 );

	alpha_ret( p, ra, 1 );

	return p;
}


void output( char * p, int len )
{
	int fd = open( "bad.out", O_CREAT | O_TRUNC );
	write( fd, p, len );
	close( fd );
}

int main( int argc, char ** argv ) {
	char code [16000];
	char *p = code;
	char * cp;

	printf( "%d", sizeof( unsigned int ) );

        printf (".text\n.align 4\n.globl main\n.type main,@function\nmain:\n");

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

        for (cp = code; cp < p; cp++) 
	{
                printf (".byte 0x%0.2x\n", (*cp&0x00ff) );
        }

	output( code, p-code );

	return 0;
}
