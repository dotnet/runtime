/* -----------------------------------------------------------------------
   ffi.c - Copyright (c) 1996, 2001 Red Hat, Inc.
   
   MIPS Foreign Function Interface 

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

#include <ffi.h>
#include <ffi_private.h>
#include <mips/mips.h>

#include <stdlib.h>

#if _MIPS_SIM == _MIPS_SIM_NABI32
#define FIX_ARGP \
FFI_ASSERT(argp <= &stack[bytes]); \
if (argp == &stack[bytes]) \
{ \
  argp = stack; \
}
#else
#define FIX_ARGP 
#endif


/* ffi_prep_args is called by the assembly routine once stack space
   has been allocated for the function's arguments */

static void ffi_prep_args(char *stack, 
			  extended_cif *ecif,
			  int bytes,
			  int flags)
{
  register int i;
  register int avn;
  register void **p_argv;
  register char *argp;
  register ffi_type **p_arg;

#if _MIPS_SIM == _MIPS_SIM_NABI32
  /* If more than 8 double words are used, the remainder go
     on the stack. We reorder stuff on the stack here to 
     support this easily. */
  if (bytes > 8 * SIZEOF_ARG)
    argp = &stack[bytes - (8 * SIZEOF_ARG)];
  else
    argp = stack;
#else
  argp = stack;
#endif

  memset(stack, 0, bytes);

#if _MIPS_SIM == _MIPS_SIM_NABI32
  if ( ecif->cif->rstruct_flag != 0 )
#else
  if ( ecif->cif->rtype->type == FFI_TYPE_STRUCT )
#endif  
    {
      *(SLOT_TYPE_UNSIGNED *) argp = (SLOT_TYPE_UNSIGNED) ecif->rvalue;
      argp += sizeof(SLOT_TYPE_UNSIGNED);
      FIX_ARGP;
    }

  avn = ecif->cif->nargs;
  p_argv = ecif->avalue;

  for (i = ecif->cif->nargs, p_arg = ecif->cif->arg_types;
       i && avn;
       i--, p_arg++)
    {
      size_t z;

      /* Align if necessary */
      if (((*p_arg)->alignment - 1) & (unsigned) argp) {
	argp = (char *) ALIGN(argp, (*p_arg)->alignment);
	FIX_ARGP;
      }

#if _MIPS_SIM == _MIPS_SIM_ABI32
#define OFFSET 0
#else
#define OFFSET sizeof(int)
#endif      

      if (avn) 
	{
	  avn--;
	  z = (*p_arg)->size;
	  if (z < sizeof(SLOT_TYPE_UNSIGNED))
	    {
	      z = sizeof(SLOT_TYPE_UNSIGNED);

	      switch ((*p_arg)->type)
		{
		case FFI_TYPE_SINT8:
		  *(SINT32 *) &argp[OFFSET] = (SINT32)*(SINT8 *)(* p_argv);
		  break;
		  
		case FFI_TYPE_UINT8:
		  *(UINT32 *) &argp[OFFSET] = (UINT32)*(UINT8 *)(* p_argv);
		  break;
		  
		case FFI_TYPE_SINT16:
		  *(SINT32 *) &argp[OFFSET] = (SINT32)*(SINT16 *)(* p_argv);
		  break;
		  
		case FFI_TYPE_UINT16:
		  *(UINT32 *) &argp[OFFSET] = (UINT32)*(UINT16 *)(* p_argv);
		  break;
		  
		case FFI_TYPE_SINT32:
		  *(SINT32 *) &argp[OFFSET] = (SINT32)*(SINT32 *)(* p_argv);
		  break;
		  
		case FFI_TYPE_UINT32:
		case FFI_TYPE_POINTER:
		  *(UINT32 *) &argp[OFFSET] = (UINT32)*(UINT32 *)(* p_argv);
		  break;

		  /* This can only happen with 64bit slots */
		case FFI_TYPE_FLOAT:
		  *(float *) argp = *(float *)(* p_argv);
		  break;

		  /* Handle small structures */
		case FFI_TYPE_STRUCT:
		  memcpy(argp, *p_argv, (*p_arg)->size);
		  break;

		default:
		  FFI_ASSERT(0);
		}
	    }
	  else
	    {
#if _MIPS_SIM == _MIPS_SIM_ABI32	      
	      memcpy(argp, *p_argv, z);
#else
	      {
		unsigned end = (unsigned) argp+z;
		unsigned cap = (unsigned) stack+bytes;

		/* Check if the data will fit within the register
		   space. Handle it if it doesn't. */

		if (end <= cap)
		  memcpy(argp, *p_argv, z);
		else
		  {
		    unsigned portion = end - cap;

		    memcpy(argp, *p_argv, portion);
		    argp = stack;
		    memcpy(argp, 
			   (void*)((unsigned)(*p_argv)+portion), z - portion);
		  }
	      }
#endif
	    }
	  p_argv++;
	  argp += z;
	  FIX_ARGP;
	}
    }
  
  return;
}

#if _MIPS_SIM == _MIPS_SIM_NABI32

/* The n32 spec says that if "a chunk consists solely of a double 
   float field (but not a double, which is part of a union), it
   is passed in a floating point register. Any other chunk is
   passed in an integer register". This code traverses structure
   definitions and generates the appropriate flags. */

static unsigned calc_n32_struct_flags(ffi_type *arg, unsigned *shift)
{
  unsigned flags = 0;
  unsigned index = 0;

  ffi_type *e;

  while (e = arg->elements[index])
    {
      if (e->type == FFI_TYPE_DOUBLE)
	{
	  flags += (FFI_TYPE_DOUBLE << *shift);
	  *shift += FFI_FLAG_BITS;
	}
      else if (e->type == FFI_TYPE_STRUCT)
	  flags += calc_n32_struct_flags(e, shift);
      else
	*shift += FFI_FLAG_BITS;

      index++;
    }

  return flags;
}

unsigned calc_n32_return_struct_flags(ffi_type *arg)
{
  unsigned flags = 0;
  unsigned index = 0;
  unsigned small = FFI_TYPE_SMALLSTRUCT;
  ffi_type *e;

  /* Returning structures under n32 is a tricky thing.
     A struct with only one or two floating point fields 
     is returned in $f0 (and $f2 if necessary). Any other
     struct results at most 128 bits are returned in $2
     (the first 64 bits) and $3 (remainder, if necessary).
     Larger structs are handled normally. */
  
  if (arg->size > 16)
    return 0;

  if (arg->size > 8)
    small = FFI_TYPE_SMALLSTRUCT2;

  e = arg->elements[0];
  if (e->type == FFI_TYPE_DOUBLE)
    flags = FFI_TYPE_DOUBLE << FFI_FLAG_BITS;
  else if (e->type == FFI_TYPE_FLOAT)
    flags = FFI_TYPE_FLOAT << FFI_FLAG_BITS;

  if (flags && (e = arg->elements[1]))
    {
      if (e->type == FFI_TYPE_DOUBLE)
	flags += FFI_TYPE_DOUBLE;
      else if (e->type == FFI_TYPE_FLOAT)
	flags += FFI_TYPE_FLOAT;
      else 
	return small;

      if (flags && (arg->elements[2]))
	{
	  /* There are three arguments and the first two are 
	     floats! This must be passed the old way. */
	  return small;
	}
    }
  else
    if (!flags)
      return small;

  return flags;
}

#endif

/* Perform machine dependent cif processing */
ffi_status ffi_prep_cif_machdep(ffi_cif *cif)
{
  cif->flags = 0;

#if _MIPS_SIM == _MIPS_SIM_ABI32
  /* Set the flags necessary for O32 processing */

  if (cif->rtype->type != FFI_TYPE_STRUCT)
    {
      if (cif->nargs > 0)
	{
	  switch ((cif->arg_types)[0]->type)
	    {
	    case FFI_TYPE_FLOAT:
	    case FFI_TYPE_DOUBLE:
	      cif->flags += (cif->arg_types)[0]->type;
	      break;
	      
	    default:
	      break;
	    }

	  if (cif->nargs > 1)
	    {
	      /* Only handle the second argument if the first
		 is a float or double. */
	      if (cif->flags)
		{
		  switch ((cif->arg_types)[1]->type)
		    {
		    case FFI_TYPE_FLOAT:
		    case FFI_TYPE_DOUBLE:
		      cif->flags += (cif->arg_types)[1]->type << FFI_FLAG_BITS;
		      break;
		      
		    default:
		      break;
		    }
		}
	    }
	}
    }
      
  /* Set the return type flag */
  switch (cif->rtype->type)
    {
    case FFI_TYPE_VOID:
    case FFI_TYPE_STRUCT:
    case FFI_TYPE_FLOAT:
    case FFI_TYPE_DOUBLE:
      cif->flags += cif->rtype->type << (FFI_FLAG_BITS * 2);
      break;
      
    default:
      cif->flags += FFI_TYPE_INT << (FFI_FLAG_BITS * 2);
      break;
    }
#endif

#if _MIPS_SIM == _MIPS_SIM_NABI32
  /* Set the flags necessary for N32 processing */
  {
    unsigned shift = 0;
    unsigned count = (cif->nargs < 8) ? cif->nargs : 8;
    unsigned index = 0;

    unsigned struct_flags = 0;

    if (cif->rtype->type == FFI_TYPE_STRUCT)
      {
	struct_flags = calc_n32_return_struct_flags(cif->rtype);

	if (struct_flags == 0)
	  {
	    /* This means that the structure is being passed as
	       a hidden argument */

	    shift = FFI_FLAG_BITS;
	    count = (cif->nargs < 7) ? cif->nargs : 7;

	    cif->rstruct_flag = !0;
	  }
	else
	    cif->rstruct_flag = 0;
      }
    else
      cif->rstruct_flag = 0;

    while (count-- > 0)
      {
	switch ((cif->arg_types)[index]->type)
	  {
	  case FFI_TYPE_FLOAT:
	  case FFI_TYPE_DOUBLE:
	    cif->flags += ((cif->arg_types)[index]->type << shift);
	    shift += FFI_FLAG_BITS;
	    break;

	  case FFI_TYPE_STRUCT:
	    cif->flags += calc_n32_struct_flags((cif->arg_types)[index],
						&shift);
	    break;

	  default:
	    shift += FFI_FLAG_BITS;
	  }

	index++;
      }

  /* Set the return type flag */
    switch (cif->rtype->type)
      {
      case FFI_TYPE_STRUCT:
	{
	  if (struct_flags == 0)
	    {
	      /* The structure is returned through a hidden
		 first argument. Do nothing, 'cause FFI_TYPE_VOID 
		 is 0 */
	    }
	  else
	    {
	      /* The structure is returned via some tricky
		 mechanism */
	      cif->flags += FFI_TYPE_STRUCT << (FFI_FLAG_BITS * 8);
	      cif->flags += struct_flags << (4 + (FFI_FLAG_BITS * 8));
	    }
	  break;
	}
      
      case FFI_TYPE_VOID:
	/* Do nothing, 'cause FFI_TYPE_VOID is 0 */
	break;
	
      case FFI_TYPE_FLOAT:
      case FFI_TYPE_DOUBLE:
	cif->flags += cif->rtype->type << (FFI_FLAG_BITS * 8);
	break;
	
      default:
	cif->flags += FFI_TYPE_INT << (FFI_FLAG_BITS * 8);
	break;
      }
  }
#endif
  
  return FFI_OK;
}

/* Low level routine for calling O32 functions */
extern int ffi_call_O32(void (*)(char *, extended_cif *, int, int), 
			extended_cif *, unsigned, 
			unsigned, unsigned *, void (*)());

/* Low level routine for calling N32 functions */
extern int ffi_call_N32(void (*)(char *, extended_cif *, int, int), 
			extended_cif *, unsigned, 
			unsigned, unsigned *, void (*)());

void ffi_call(ffi_cif *cif, void (*fn)(), void *rvalue, void **avalue)
{
  extended_cif ecif;

  ecif.cif = cif;
  ecif.avalue = avalue;
  
  /* If the return value is a struct and we don't have a return	*/
  /* value address then we need to make one		        */
  
  if ((rvalue == NULL) && 
      (cif->rtype->type == FFI_TYPE_STRUCT))
    ecif.rvalue = alloca(cif->rtype->size);
  else
    ecif.rvalue = rvalue;
    
  switch (cif->abi) 
    {
#if _MIPS_SIM == _MIPS_SIM_ABI32
    case FFI_O32:
      ffi_call_O32(ffi_prep_args, &ecif, cif->bytes, 
		   cif->flags, ecif.rvalue, fn);
      break;
#endif

#if _MIPS_SIM == _MIPS_SIM_NABI32
    case FFI_N32:
      ffi_call_N32(ffi_prep_args, &ecif, cif->bytes, 
		   cif->flags, ecif.rvalue, fn);
      break;
#endif

    default:
      FFI_ASSERT(0);
      break;
    }
}
