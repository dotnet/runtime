/* -----------------------------------------------------------------------
   ffi-mips.h - Copyright (c) 1996, 2001 Red Hat, Inc.
   
   MIPS FFI Definitions

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
   IN NO EVENT SHALL CYGNUS SUPPORT BE LIABLE FOR ANY CLAIM, DAMAGES OR
   OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
   ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
   OTHER DEALINGS IN THE SOFTWARE.
   ----------------------------------------------------------------------- */

#ifndef MIPS_H

#if defined(__mips_eabi)
#  define FFI_MIPS_EABI
#  define FFI_MIPS_O32
#else
#  if !defined(_MIPS_SIM)
-- something is very wrong --
#  else
#    if _MIPS_SIM==_ABIN32 && defined(_ABIN32)
#      define FFI_MIPS_N32
#    else
#      if defined(__GNUC__)
#        define FFI_MIPS_O32
#      else
#        if _MIPS_SIM==_ABIO32
#          define FFI_MIPS_O32
#        else
-- this is an unsupported platform --
#        endif
#      endif
#    endif
#  endif
#endif

#define v0 $2
#define v1 $3
#define a0 $4
#define a1 $5
#define a2 $6
#define a3 $7
#define a4 $8		
#define a5 $9		
#define a6 $10		
#define a7 $11		
#define t0 $8
#define t1 $9
#define t2 $10
#define t3 $11
#define t4 $12		
#define t5 $13
#define t6 $14	
#define t7 $15
#define t8 $24
#define t9 $25
#define ra $31		

#if defined(FFI_MIPS_O32)

#define FFI_DEFAULT_ABI FFI_O32

/* O32 stack frames have 32bit integer args */
#define SLOT_TYPE_UNSIGNED UINT32
#define SLOT_TYPE_SIGNED   SINT32
#define SIZEOF_ARG         4

#define REG_L	lw
#define REG_S	sw
#define SUBU	subu
#define ADDU	addu
#define SRL	srl
#define LI	li

#else

#define FFI_DEFAULT_ABI FFI_N32

/* N32 and N64 frames have 64bit integer args */
#define SLOT_TYPE_UNSIGNED UINT64
#define SLOT_TYPE_SIGNED   SINT64
#define SIZEOF_ARG         8

#define REG_L	ld
#define REG_S	sd
#define SUBU	dsubu
#define ADDU	daddu
#define SRL	dsrl
#define LI 	dli

#endif

#define FFI_FLAG_BITS 2

/* SGI's strange assembler requires that we multiply by 4 rather 
   than shift left by FFI_FLAG_BITS */

#define FFI_ARGS_D   FFI_TYPE_DOUBLE
#define FFI_ARGS_F   FFI_TYPE_FLOAT
#define FFI_ARGS_DD  FFI_TYPE_DOUBLE * 4 + FFI_TYPE_DOUBLE
#define FFI_ARGS_FF  FFI_TYPE_FLOAT * 4 +  FFI_TYPE_FLOAT
#define FFI_ARGS_FD  FFI_TYPE_DOUBLE * 4 + FFI_TYPE_FLOAT
#define FFI_ARGS_DF  FFI_TYPE_FLOAT * 4 + FFI_TYPE_DOUBLE

/* Needed for N32 structure returns */
#define FFI_TYPE_SMALLSTRUCT  FFI_TYPE_UINT8
#define FFI_TYPE_SMALLSTRUCT2 FFI_TYPE_SINT8

#if 0

/* The SGI assembler can't handle this.. */

#define FFI_TYPE_STRUCT_DD (( FFI_ARGS_DD ) << 4) + FFI_TYPE_STRUCT

#else

/* ...so we calculate these by hand! */

#define FFI_TYPE_STRUCT_D      61
#define FFI_TYPE_STRUCT_F      45
#define FFI_TYPE_STRUCT_DD     253
#define FFI_TYPE_STRUCT_FF     173
#define FFI_TYPE_STRUCT_FD     237
#define FFI_TYPE_STRUCT_DF     189
#define FFI_TYPE_STRUCT_SMALL  93
#define FFI_TYPE_STRUCT_SMALL2 109

#endif

#endif
