/* -----------------------------------------------------------------------
   ffi.c - Copyright (c) 1998 Cygnus Solutions
	   Copyright (c) 2000 Hewlett Packard Company
   
   IA64 Foreign Function Interface 

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

#include <stdlib.h>

#include "ia64_flags.h"

/* Memory image of fp register contents.  Should eventually be an fp 	*/
/* type long enough to hold an entire register.  For now we use double.	*/
typedef double float80;

/* The stack layout at call to ffi_prep_regs.  Other_args will remain	*/
/* on the stack for the actual call.  Everything else we be transferred	*/
/* to registers and popped by the assembly code.			*/

struct ia64_args {
    long scratch[2];	/* Two scratch words at top of stack.		*/
			/* Allows sp to passed as arg pointer.		*/
    void * r8_contents;	/* Value to be passed in r8			*/
    long spare;		/* Not used.					*/
    float80 fp_regs[8]; /* Contents of 8 floating point argument 	*/
			/* registers.					*/
    long out_regs[8];	/* Contents of the 8 out registers used 	*/
			/* for integer parameters.			*/
    long other_args[0]; /* Arguments passed on stack, variable size	*/
			/* Treated as continuation of out_regs.		*/
};

static size_t float_type_size(unsigned short tp)
{
  switch(tp) {
    case FFI_TYPE_FLOAT:
      return sizeof(float);
    case FFI_TYPE_DOUBLE:
      return sizeof(double);
#if FFI_TYPE_LONGDOUBLE != FFI_TYPE_DOUBLE
    case FFI_TYPE_LONGDOUBLE:
      return sizeof(long double);
#endif
    default:
      FFI_ASSERT(0);
  }
}

/*
 * Is type a struct containing at most n floats, doubles, or extended
 * doubles, all of the same fp type?
 * If so, set *element_type to the fp type.
 */
static bool is_homogeneous_fp_aggregate(ffi_type * type, int n,
				        unsigned short * element_type)
{
  ffi_type **ptr; 
  unsigned short element, struct_element;

  int type_set = 0;

  FFI_ASSERT(type != NULL);

  FFI_ASSERT(type->elements != NULL);

  ptr = &(type->elements[0]);

  while ((*ptr) != NULL)
    {
      switch((*ptr) -> type) {
	case FFI_TYPE_FLOAT:
	  if (type_set && element != FFI_TYPE_FLOAT) return 0;
	  if (--n < 0) return FALSE;
	  type_set = 1;
	  element = FFI_TYPE_FLOAT;
	  break;
	case FFI_TYPE_DOUBLE:
	  if (type_set && element != FFI_TYPE_DOUBLE) return 0;
	  if (--n < 0) return FALSE;
	  type_set = 1;
	  element = FFI_TYPE_DOUBLE;
	  break;
	case FFI_TYPE_STRUCT:
	  if (!is_homogeneous_fp_aggregate(type, n, &struct_element))
	      return FALSE;
	  if (type_set && struct_element != element) return FALSE;
	  n -= (type -> size)/float_type_size(element);
	  element = struct_element;
	  if (n < 0) return FALSE;
	  break;
	/* case FFI_TYPE_LONGDOUBLE:
	  Not yet implemented.	*/
	default:
	  return FALSE;
      }
      ptr++;
    }
  *element_type = element;
  return TRUE;
   
} 

/* ffi_prep_args is called by the assembly routine once stack space
   has been allocated for the function's arguments.  Returns nonzero
   if fp registers are used for arguments. */

static bool
ffi_prep_args(struct ia64_args *stack, extended_cif *ecif, int bytes)
{
  register long i, avn;
  register void **p_argv;
  register long *argp = stack -> out_regs;
  register float80 *fp_argp = stack -> fp_regs;
  register ffi_type **p_arg;

  /* For big return structs, r8 needs to contain the target address.	*/
  /* Since r8 is otherwise dead, we set it unconditionally.		*/
  stack -> r8_contents = ecif -> rvalue;
  i = 0;
  avn = ecif->cif->nargs;
  p_arg = ecif->cif->arg_types;
  p_argv = ecif->avalue;
  while (i < avn)
    {
      size_t z; /* z is in units of arg slots or words, not bytes.	*/

      switch ((*p_arg)->type)
	{
	case FFI_TYPE_SINT8:
	  z = 1;
	  *(SINT64 *) argp = *(SINT8 *)(* p_argv);
	  break;
		  
	case FFI_TYPE_UINT8:
	  z = 1;
	  *(UINT64 *) argp = *(UINT8 *)(* p_argv);
	  break;
		  
	case FFI_TYPE_SINT16:
	  z = 1;
	  *(SINT64 *) argp = *(SINT16 *)(* p_argv);
	  break;
		  
	case FFI_TYPE_UINT16:
	  z = 1;
	  *(UINT64 *) argp = *(UINT16 *)(* p_argv);
	  break;
		  
	case FFI_TYPE_SINT32:
	  z = 1;
	  *(SINT64 *) argp = *(SINT32 *)(* p_argv);
	  break;
		  
	case FFI_TYPE_UINT32:
	  z = 1;
	  *(UINT64 *) argp = *(UINT32 *)(* p_argv);
	  break;

	case FFI_TYPE_SINT64:
	case FFI_TYPE_UINT64:
	case FFI_TYPE_POINTER:
	  z = 1;
	  *(UINT64 *) argp = *(UINT64 *)(* p_argv);
	  break;

	case FFI_TYPE_FLOAT:
	  z = 1;
	  if (fp_argp - stack->fp_regs < 8)
	    {
	      /* Note the conversion -- all the fp regs are loaded as
		 doubles.  */
	      *fp_argp++ = *(float *)(* p_argv);
	    }
	  /* Also put it into the integer registers or memory: */
	    *(UINT64 *) argp = *(UINT32 *)(* p_argv);
	  break;

	case FFI_TYPE_DOUBLE:
	  z = 1;
	  if (fp_argp - stack->fp_regs < 8)
	    *fp_argp++ = *(double *)(* p_argv);
	  /* Also put it into the integer registers or memory: */
	    *(double *) argp = *(double *)(* p_argv);
	  break;

	case FFI_TYPE_STRUCT:
	  {
	      size_t sz = (*p_arg)->size;
	      unsigned short element_type;
              z = ((*p_arg)->size + SIZEOF_ARG - 1)/SIZEOF_ARG;
	      if (is_homogeneous_fp_aggregate(*p_arg, 8, &element_type)) {
		int i;
		int nelements = sz/float_type_size(element_type);
		for (i = 0; i < nelements; ++i) {
		  switch (element_type) {
		    case FFI_TYPE_FLOAT:
		      if (fp_argp - stack->fp_regs < 8)
			*fp_argp++ = ((float *)(* p_argv))[i];
		      break;
		    case FFI_TYPE_DOUBLE:
		      if (fp_argp - stack->fp_regs < 8)
			*fp_argp++ = ((double *)(* p_argv))[i];
		      break;
		    default:
			/* Extended precision not yet implemented. */
			abort();
		  }
		}
	      }
	      /* And pass it in integer registers as a struct, with	*/
	      /* its actual field sizes packed into registers.		*/
	      memcpy(argp, *p_argv, (*p_arg)->size);
	  }
	  break;

	default:
	  FFI_ASSERT(0);
	}

      argp += z;
      i++, p_arg++, p_argv++;
    }
  return (fp_argp != stack -> fp_regs);
}

/* Perform machine dependent cif processing */
ffi_status
ffi_prep_cif_machdep(ffi_cif *cif)
{
  long i, avn;
  bool is_simple = TRUE;
  long simple_flag = FFI_SIMPLE_V;
  /* Adjust cif->bytes to include space for the 2 scratch words,
     r8 register contents, spare word,
     the 8 fp register contents, and all 8 integer register contents.
     This will be removed before the call, though 2 scratch words must
     remain.  */

  cif->bytes += 4*sizeof(long) + 8 *sizeof(float80);
  if (cif->bytes < sizeof(struct ia64_args))
    cif->bytes = sizeof(struct ia64_args);

  /* The stack must be double word aligned, so round bytes up
     appropriately. */

  cif->bytes = ALIGN(cif->bytes, 2*sizeof(void*));

  avn = cif->nargs;
  if (avn <= 2) {
    for (i = 0; i < avn; ++i) {
      switch(cif -> arg_types[i] -> type) {
	case FFI_TYPE_SINT32:
	  simple_flag = FFI_ADD_INT_ARG(simple_flag);
	  break;
	case FFI_TYPE_SINT64:
	case FFI_TYPE_UINT64:
	case FFI_TYPE_POINTER:
	  simple_flag = FFI_ADD_LONG_ARG(simple_flag);
	  break;
	default:
	  is_simple = FALSE;
      }
    }
  } else {
    is_simple = FALSE;
  }

  /* Set the return type flag */
  switch (cif->rtype->type)
    {
    case FFI_TYPE_VOID:
      cif->flags = FFI_TYPE_VOID;
      break;

    case FFI_TYPE_STRUCT:
      {
        size_t sz = cif -> rtype -> size;
  	unsigned short element_type;

	is_simple = FALSE;
  	if (is_homogeneous_fp_aggregate(cif -> rtype, 8, &element_type)) {
	  int nelements = sz/float_type_size(element_type);
	  if (nelements <= 1) {
	    if (0 == nelements) {
	      cif -> flags = FFI_TYPE_VOID;
	    } else {
	      cif -> flags = element_type;
	    }
	  } else {
	    switch(element_type) {
	      case FFI_TYPE_FLOAT:
	        cif -> flags = FFI_IS_FLOAT_FP_AGGREGATE | nelements;
		break;
	      case FFI_TYPE_DOUBLE:
	        cif -> flags = FFI_IS_DOUBLE_FP_AGGREGATE | nelements;
		break;
	      default:
		/* long double NYI */
		abort();
	    }
	  }
	  break;
        }
        if (sz <= 32) {
	  if (sz <= 8) {
              cif->flags = FFI_TYPE_INT;
  	  } else if (sz <= 16) {
              cif->flags = FFI_IS_SMALL_STRUCT2;
  	  } else if (sz <= 24) {
              cif->flags = FFI_IS_SMALL_STRUCT3;
	  } else {
              cif->flags = FFI_IS_SMALL_STRUCT4;
	  }
        } else {
          cif->flags = FFI_TYPE_STRUCT;
	}
      }
      break;

    case FFI_TYPE_FLOAT:
      is_simple = FALSE;
      cif->flags = FFI_TYPE_FLOAT;
      break;

    case FFI_TYPE_DOUBLE:
      is_simple = FALSE;
      cif->flags = FFI_TYPE_DOUBLE;
      break;

    default:
      cif->flags = FFI_TYPE_INT;
      /* This seems to depend on little endian mode, and the fact that	*/
      /* the return pointer always points to at least 8 bytes.  But 	*/
      /* that also seems to be true for other platforms.		*/
      break;
    }
  
  if (is_simple) cif -> flags |= simple_flag;
  return FFI_OK;
}

extern int ffi_call_unix(bool (*)(struct ia64_args *, extended_cif *, int), 
			 extended_cif *, unsigned, 
			 unsigned, unsigned *, void (*)());

void
ffi_call(ffi_cif *cif, void (*fn)(), void *rvalue, void **avalue)
{
  extended_cif ecif;
  long simple = cif -> flags & FFI_SIMPLE;

  /* Should this also check for Unix ABI? */
  /* This is almost, but not quite, machine independent.  Note that	*/
  /* we can get away with not caring about length of the result because	*/
  /* we assume we are little endian, and the result buffer is large 	*/
  /* enough.								*/
  /* This needs work for HP/UX.						*/
  if (simple) {
    long (*lfn)() = (long (*)())fn;
    long result;
    switch(simple) {
      case FFI_SIMPLE_V:
	result = lfn();
	break;
      case FFI_SIMPLE_I:
	result = lfn(*(int *)avalue[0]);
	break;
      case FFI_SIMPLE_L:
	result = lfn(*(long *)avalue[0]);
	break;
      case FFI_SIMPLE_II:
	result = lfn(*(int *)avalue[0], *(int *)avalue[1]);
	break;
      case FFI_SIMPLE_IL:
	result = lfn(*(int *)avalue[0], *(long *)avalue[1]);
	break;
      case FFI_SIMPLE_LI:
	result = lfn(*(long *)avalue[0], *(int *)avalue[1]);
	break;
      case FFI_SIMPLE_LL:
	result = lfn(*(long *)avalue[0], *(long *)avalue[1]);
	break;
    }
    if ((cif->flags & ~FFI_SIMPLE) != FFI_TYPE_VOID && 0 != rvalue) {
      * (long *)rvalue = result;
    }
    return;
  }
  ecif.cif = cif;
  ecif.avalue = avalue;
  
  /* If the return value is a struct and we don't have a return
     value address then we need to make one.  */
  
  if (rvalue == NULL && cif->rtype->type == FFI_TYPE_STRUCT)
    ecif.rvalue = alloca(cif->rtype->size);
  else
    ecif.rvalue = rvalue;
    
  switch (cif->abi) 
    {
    case FFI_UNIX:
      ffi_call_unix(ffi_prep_args, &ecif, cif->bytes,
		    cif->flags, rvalue, fn);
      break;

    default:
      FFI_ASSERT(0);
      break;
    }
}

/*
 * Closures represent a pair consisting of a function pointer, and
 * some user data.  A closure is invoked by reinterpreting the closure
 * as a function pointer, and branching to it.  Thus we can make an
 * interpreted function callable as a C function:  We turn the interpreter
 * itself, together with a pointer specifying the interpreted procedure,
 * into a closure.
 * On X86, the first few words of the closure structure actually contain code,
 * which will do the right thing.  On most other architectures, this
 * would raise some Icache/Dcache coherence issues (which can be solved, but
 * often not cheaply).
 * For IA64, function pointer are already pairs consisting of a code
 * pointer, and a gp pointer.  The latter is needed to access global variables.
 * Here we set up such a pair as the first two words of the closure (in
 * the "trampoline" area), but we replace the gp pointer with a pointer
 * to the closure itself.  We also add the real gp pointer to the
 * closure.  This allows the function entry code to both retrieve the
 * user data, and to restire the correct gp pointer.
 */

static void 
ffi_prep_incoming_args_UNIX(struct ia64_args *args, void **rvalue,
			    void **avalue, ffi_cif *cif);

/* This function is entered with the doctored gp (r1) value.
 * This code is extremely gcc specific.  There is some argument that
 * it should really be written in assembly code, since it depends on
 * gcc properties that might change over time.
 */

/* ffi_closure_UNIX is an assembly routine, which copies the register 	*/
/* state into s struct ia64_args, and the invokes			*/
/* ffi_closure_UNIX_inner.  It also recovers the closure pointer	*/
/* from its fake gp pointer.						*/
void ffi_closure_UNIX();

#ifndef __GNUC__
#   error This requires gcc
#endif
void
ffi_closure_UNIX_inner (ffi_closure *closure, struct ia64_args * args)
/* Hopefully declarint this as a varargs function will force all args	*/
/* to memory.								*/
{
  // this is our return value storage
  long double    res;

  // our various things...
  ffi_cif       *cif;
  unsigned short rtype;
  void          *resp;
  void		**arg_area;

  resp = (void*)&res;
  cif         = closure->cif;
  arg_area    = (void**) alloca (cif->nargs * sizeof (void*));  

  /* this call will initialize ARG_AREA, such that each
   * element in that array points to the corresponding 
   * value on the stack; and if the function returns
   * a structure, it will re-set RESP to point to the
   * structure return address.  */

  ffi_prep_incoming_args_UNIX(args, (void**)&resp, arg_area, cif);
  
  (closure->fun) (cif, resp, arg_area, closure->user_data);

  rtype = cif->flags;

  /* now, do a generic return based on the value of rtype */
  if (rtype == FFI_TYPE_INT)
    {
      asm volatile ("ld8 r8=[%0]" : : "r" (resp) : "r8");
    }
  else if (rtype == FFI_TYPE_FLOAT)
    {
      asm volatile ("ldfs f8=[%0]" : : "r" (resp) : "f8");
    }
  else if (rtype == FFI_TYPE_DOUBLE)
    {
      asm volatile ("ldfd f8=[%0]" : : "r" (resp) : "f8");
    }
  else if (rtype == FFI_IS_SMALL_STRUCT2)
    {
      asm volatile ("ld8 r8=[%0]; ld8 r9=[%1]"
		    : : "r" (resp), "r" (resp+8) : "r8","r9");
    }
  else if (rtype == FFI_IS_SMALL_STRUCT3)
    {
      asm volatile ("ld8 r8=[%0]; ld8 r9=[%1]; ld8 r10=[%2]"
		    : : "r" (resp), "r" (resp+8), "r" (resp+16)
		    : "r8","r9","r10");
    }
  else if (rtype == FFI_IS_SMALL_STRUCT4)
    {
      asm volatile ("ld8 r8=[%0]; ld8 r9=[%1]; ld8 r10=[%2]; ld8 r11=[%3]"
		    : : "r" (resp), "r" (resp+8), "r" (resp+16), "r" (resp+24)
		    : "r8","r9","r10","r11");
    }
  else if (rtype != FFI_TYPE_VOID && rtype != FFI_TYPE_STRUCT)
    {
      /* Can only happen for homogeneous FP aggregates?	*/
      abort();
    }
}

static void 
ffi_prep_incoming_args_UNIX(struct ia64_args *args, void **rvalue,
			    void **avalue, ffi_cif *cif)
{
  register unsigned int i;
  register unsigned int avn;
  register void **p_argv;
  register unsigned long *argp = args -> out_regs;
  unsigned fp_reg_num = 0;
  register ffi_type **p_arg;

  avn = cif->nargs;
  p_argv = avalue;

  for (i = cif->nargs, p_arg = cif->arg_types; i != 0; i--, p_arg++)
    {
      size_t z; /* In units of words or argument slots.	*/

      switch ((*p_arg)->type)
	{
	case FFI_TYPE_SINT8:
	case FFI_TYPE_UINT8:
	case FFI_TYPE_SINT16:
	case FFI_TYPE_UINT16:
	case FFI_TYPE_SINT32:
	case FFI_TYPE_UINT32:
	case FFI_TYPE_SINT64:
	case FFI_TYPE_UINT64:
	case FFI_TYPE_POINTER:
	  z = 1;
	  *p_argv = (void *)argp;
	  break;
		  
	case FFI_TYPE_FLOAT:
	  z = 1;
	  /* Convert argument back to float in place from the saved value */
	  if (fp_reg_num < 8) {
	      *(float *)argp = args -> fp_regs[fp_reg_num++];
	  } else {
	      *(float *)argp = *(double *)argp;
	  }
	  *p_argv = (void *)argp;
	  break;

	case FFI_TYPE_DOUBLE:
	  z = 1;
	  if (fp_reg_num < 8) {
	      *p_argv = args -> fp_regs + fp_reg_num++;
	  } else {
	      *p_argv = (void *)argp;
	  }
	  break;

	case FFI_TYPE_STRUCT:
	  {
	      size_t sz = (*p_arg)->size;
	      unsigned short element_type;
              z = ((*p_arg)->size + SIZEOF_ARG - 1)/SIZEOF_ARG;
	      if (is_homogeneous_fp_aggregate(*p_arg, 8, &element_type)) {
		int nelements = sz/float_type_size(element_type);
		if (nelements + fp_reg_num >= 8) {
		  /* hard case NYI.	*/
		  abort();
		}
		if (element_type == FFI_TYPE_DOUBLE) {
	          *p_argv = args -> fp_regs + fp_reg_num;
		  fp_reg_num += nelements;
		  break;
		}
		if (element_type == FFI_TYPE_FLOAT) {
		  int j;
		  for (j = 0; j < nelements; ++ j) {
		     ((float *)argp)[j] = args -> fp_regs[fp_reg_num + j];
		  }
	          *p_argv = (void *)argp;
		  fp_reg_num += nelements;
		  break;
		}
		abort();  /* Other fp types NYI */
	      }
	  }
	  break;

	default:
	  FFI_ASSERT(0);
	}

      argp += z;
      p_argv++;

    }
  
  return;
}


/* Fill in a closure to refer to the specified fun and user_data.	*/
/* cif specifies the argument and result types for fun.			*/
/* the cif must already be prep'ed */

/* The layout of a function descriptor.  A C function pointer really 	*/
/* points to one of these.						*/
typedef struct ia64_fd_struct {
    void *code_pointer;
    void *gp;
} ia64_fd;

ffi_status
ffi_prep_closure (ffi_closure* closure,
		  ffi_cif* cif,
		  void (*fun)(ffi_cif*,void*,void**,void*),
		  void *user_data)
{
  struct ffi_ia64_trampoline_struct *tramp =
    (struct ffi_ia64_trampoline_struct *) (closure -> tramp);
  ia64_fd *fd = (ia64_fd *)(void *)ffi_closure_UNIX;

  FFI_ASSERT (cif->abi == FFI_UNIX);

  tramp -> code_pointer = fd -> code_pointer;
  tramp -> real_gp = fd -> gp;
  tramp -> fake_gp = closure;
  closure->cif  = cif;
  closure->user_data = user_data;
  closure->fun  = fun;

  return FFI_OK;
}


