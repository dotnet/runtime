/* -----------------------------------------------------------------------
   ia64_flags.h - Copyright (c) 2000 Hewlett Packard Company
   
   IA64/unix Foreign Function Interface 

   Original author: Hans Boehm, HP Labs

   Permission is hereby granted, free of charge, to any person obtaining
   a copy of this software and associated documentation files (the
   ``Software''), to deal in the Software without restriction, including
   without limitation the rights to use, copy, modify, merge, publish,
   distribute, sublicense, and/or sell copies of the Software, and to
   permit persons to whom the Software is furnished to do so, subject to
   the following conditions:

   The above copyright notice and this permission notice shall be included
   in all copies or substantial portions of the Software.

   THE SOFTWARE IS PROVIDED ``AS IS'', WITHOUT WARRANTY OF ANY KIND, EXPRESS
   OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
   MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
   IN NO EVENT SHALL CYGNUS SOLUTIONS BE LIABLE FOR ANY CLAIM, DAMAGES OR
   OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
   ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
   OTHER DEALINGS IN THE SOFTWARE.
   ----------------------------------------------------------------------- */


/* Homogeneous Floating Point Aggregates (HFAs) which are returned	*/
/* in FP registers.  The least significant bits specify the size in 	*/
/* words.								*/
#define FFI_IS_FLOAT_FP_AGGREGATE 0x1000
#define FFI_IS_DOUBLE_FP_AGGREGATE 0x0800
#define FLOAT_FP_AGGREGATE_BIT 12
#define DOUBLE_FP_AGGREGATE_BIT 11

/* Small structures containing N words.  If N=1, they are returned	*/
/* as though they were integers.					*/
#define FFI_IS_SMALL_STRUCT2	0x40 /* Struct > 8, <=16 bytes	*/
#define FFI_IS_SMALL_STRUCT3	0x41 /* Struct > 16 <= 24 bytes	*/
#define FFI_IS_SMALL_STRUCT4	0x42 /* Struct > 24, <=32 bytes	*/

/* Flag values identifying particularly simple cases, which are 	*/
/* handled specially.  We treat functions as simple if they take all	*/
/* arguments can be passed as 32 or 64 bit integer quantities, there is	*/
/* either no return value or it can be treated as a 64bit integer, and	*/
/* if there are at most 2 arguments.					*/
/* This is OR'ed with the normal flag values.				*/
#define FFI_SIMPLE_V 0x10000	/* () -> X	*/
#define FFI_SIMPLE_I 0x20000	/* (int) -> X	*/
#define FFI_SIMPLE_L 0x30000	/* (long) -> X	*/
#define FFI_SIMPLE_II 0x40000	/* (int,int) -> X	*/
#define FFI_SIMPLE_IL 0x50000	/* (int,long) -> X	*/
#define FFI_SIMPLE_LI 0x60000	/* (long,int) -> X	*/
#define FFI_SIMPLE_LL 0x70000	/* (long,long) -> X	*/

/* Mask for all of the FFI_SIMPLE bits:	*/
#define FFI_SIMPLE 0xf0000

/* An easy way to build FFI_SIMPLE flags from FFI_SIMPLE_V:	*/
#define FFI_ADD_LONG_ARG(flag) (((flag) << 1) | 0x10000)
#define FFI_ADD_INT_ARG(flag) ((flag) << 1)
