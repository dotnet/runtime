/* -----------------------------------------------------------------------
   ffi.c - Copyright (c) 2000 Software AG
 
   S390 Foreign Function Interface
 
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
   IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY CLAIM, DAMAGES OR
   OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
   ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
   OTHER DEALINGS IN THE SOFTWARE.
   ----------------------------------------------------------------------- */
/*====================================================================*/
/*                          Includes                                  */
/*                          --------                                  */
/*====================================================================*/
 
#include <ffi.h>
#include <ffi_private.h>
 
#include <stdlib.h>
#include <stdio.h>
 
/*====================== End of Includes =============================*/
 
/*====================================================================*/
/*                           Defines                                  */
/*                           -------                                  */
/*====================================================================*/
 
#define MAX_GPRARGS 5        /* Max. no. of GPR available             */
#define MAX_FPRARGS 2        /* Max. no. of FPR available             */
 
#define STR_GPR     1        /* Structure will fit in 1 or 2 GPR      */
#define STR_FPR     2        /* Structure will fit in a FPR           */
#define STR_STACK   3        /* Structure needs to go on stack        */
 
/*===================== End of Defines ===============================*/
 
/*====================================================================*/
/*                            Types                                   */
/*                            -----                                   */
/*====================================================================*/
 
typedef struct stackLayout
{
  int   *backChain;
  int   *endOfStack;
  int   glue[2];
  int   scratch[2];
  int   gprArgs[MAX_GPRARGS];
  int   notUsed;
  union
  {
    float  f;
    double d;
  } fprArgs[MAX_FPRARGS];
  int   unUsed[8];
  int   outArgs[100];
} stackLayout;
 
/*======================== End of Types ==============================*/
 
/*====================================================================*/
/*                          Prototypes                                */
/*                          ----------                                */
/*====================================================================*/
 
void ffi_prep_args(stackLayout *, extended_cif *);
static int  ffi_check_struct(ffi_type *, unsigned int *);
static void ffi_insert_int(int, stackLayout *, int *, int *);
static void ffi_insert_int64(long long, stackLayout *, int *, int *);
static void ffi_insert_double(double, stackLayout *, int *, int *);
 
/*====================== End of Prototypes ===========================*/
 
/*====================================================================*/
/*                          Externals                                 */
/*                          ---------                                 */
/*====================================================================*/
 
extern void ffi_call_SYSV(void (*)(stackLayout *, extended_cif *),
			  extended_cif *,
			  unsigned, unsigned,
			  unsigned *,
			  void (*fn)());
 
/*====================== End of Externals ============================*/
 
/*====================================================================*/
/*                                                                    */
/* Name     - ffi_check_struct.                                       */
/*                                                                    */
/* Function - Determine if a structure can be passed within a         */
/*            general or floating point register.                     */
/*                                                                    */
/*====================================================================*/
 
int
ffi_check_struct(ffi_type *arg, unsigned int *strFlags)
{
 ffi_type *element;
 int      i_Element;
 
 for (i_Element = 0; arg->elements[i_Element]; i_Element++) {
   element = arg->elements[i_Element];
   switch (element->type) {
   case FFI_TYPE_DOUBLE :
     *strFlags |= STR_FPR;
     break;
     
   case FFI_TYPE_STRUCT :
     *strFlags |= ffi_check_struct(element, strFlags);
     break;
     
   default :
     *strFlags |= STR_GPR;
   }
 }
 return (*strFlags);
}
 
/*======================== End of Routine ============================*/
 
/*====================================================================*/
/*                                                                    */
/* Name     - ffi_insert_int.                                         */
/*                                                                    */
/* Function - Insert an integer parameter in a register if there are  */
/*            spares else on the stack.                               */
/*                                                                    */
/*====================================================================*/
 
void
ffi_insert_int(int gprValue, stackLayout *stack,
               int *intArgC, int *outArgC)
{
  if (*intArgC < MAX_GPRARGS) {
    stack->gprArgs[*intArgC] = gprValue;
    *intArgC += 1;
  }
  else {
    stack->outArgs[*outArgC++] = gprValue;
    *outArgC += 1;
  }
}
 
/*======================== End of Routine ============================*/
 
/*====================================================================*/
/*                                                                    */
/* Name     - ffi_insert_int64.                                       */
/*                                                                    */
/* Function - Insert a long long parameter in registers if there are  */
/*            spares else on the stack.                               */
/*                                                                    */
/*====================================================================*/
 
void
ffi_insert_int64(long long llngValue, stackLayout *stack,
                 int *intArgC, int *outArgC)
{
 
  if (*intArgC < (MAX_GPRARGS-1)) {
    memcpy(&stack->gprArgs[*intArgC],
	   &llngValue, sizeof(long long));	
    *intArgC += 2;
  }
  else {
    memcpy(&stack->outArgs[*outArgC],
	   &llngValue, sizeof(long long));
    *outArgC += 2;
  }
 
}
 
/*======================== End of Routine ============================*/
 
/*====================================================================*/
/*                                                                    */
/* Name     - ffi_insert_double.                                      */
/*                                                                    */
/* Function - Insert a double parameter in a FP register if there is  */
/*            a spare else on the stack.                              */
/*                                                                    */
/*====================================================================*/
 
void
ffi_insert_double(double dblValue, stackLayout *stack,
                  int *fprArgC, int *outArgC)
{
 
  if (*fprArgC < MAX_FPRARGS) {
    stack->fprArgs[*fprArgC].d = dblValue;
    *fprArgC += 1;
  }
  else {
    memcpy(&stack->outArgs[*outArgC],
	   &dblValue,sizeof(double));
    *outArgC += 2;
  }
 
}
 
/*======================== End of Routine ============================*/
 
/*====================================================================*/
/*                                                                    */
/* Name     - ffi_prep_args.                                          */
/*                                                                    */
/* Function - Prepare parameters for call to function.                */
/*                                                                    */
/* ffi_prep_args is called by the assembly routine once stack space   */
/* has been allocated for the function's arguments.                   */
/*                                                                    */
/* The stack layout we want looks like this:                          */
/* *------------------------------------------------------------*     */
/* |  0     | Back chain (a 0 here signifies end of back chain) |     */
/* +--------+---------------------------------------------------+     */
/* |  4     | EOS (end of stack, not used on Linux for S390)    |     */
/* +--------+---------------------------------------------------+     */
/* |  8     | Glue used in other linkage formats                |     */
/* +--------+---------------------------------------------------+     */
/* | 12     | Glue used in other linkage formats                |     */
/* +--------+---------------------------------------------------+     */
/* | 16     | Scratch area                                      |     */
/* +--------+---------------------------------------------------+     */
/* | 20     | Scratch area                                      |     */
/* +--------+---------------------------------------------------+     */
/* | 24     | GPR parameter register 1                          |     */
/* +--------+---------------------------------------------------+     */
/* | 28     | GPR parameter register 2                          |     */
/* +--------+---------------------------------------------------+     */
/* | 32     | GPR parameter register 3                          |     */
/* +--------+---------------------------------------------------+     */
/* | 36     | GPR parameter register 4                          |     */
/* +--------+---------------------------------------------------+     */
/* | 40     | GPR parameter register 5                          |     */
/* +--------+---------------------------------------------------+     */
/* | 44     | Unused                                            |     */
/* +--------+---------------------------------------------------+     */
/* | 48     | FPR parameter register 1                          |     */
/* +--------+---------------------------------------------------+     */
/* | 56     | FPR parameter register 2                          |     */
/* +--------+---------------------------------------------------+     */
/* | 64     | Unused                                            |     */
/* +--------+---------------------------------------------------+     */
/* | 96     | Outgoing args (length x)                          |     */
/* +--------+---------------------------------------------------+     */
/* | 96+x   | Copy area for structures (length y)               |     */
/* +--------+---------------------------------------------------+     */
/* | 96+x+y | Possible stack alignment                          |     */
/* *------------------------------------------------------------*     */
/*                                                                    */
/*====================================================================*/
 
void
ffi_prep_args(stackLayout *stack, extended_cif *ecif)
{
  const unsigned bytes = ecif->cif->bytes;
  const unsigned flags = ecif->cif->flags;
 
  /*----------------------------------------------------------*/
  /* Pointer to the copy area on stack for structures         */
  /*----------------------------------------------------------*/
  char *copySpace = (char *) stack + bytes + sizeof(stackLayout);
 
  /*----------------------------------------------------------*/
  /* Count of general and floating point register usage       */
  /*----------------------------------------------------------*/
  int intArgC = 0,
    fprArgC = 0,
    outArgC = 0;
 
  int      i;
  ffi_type **ptr;
  void     **p_argv;
  size_t   structCopySize;
  unsigned gprValue, strFlags = 0;
  unsigned long long llngValue;
  double   dblValue;
 
  /* Now for the arguments.  */
  p_argv  = ecif->avalue;
 
  /*----------------------------------------------------------------------*/
  /* If we returning a structure then we set the first parameter register */
  /* to the address of where we are returning this structure              */
  /*----------------------------------------------------------------------*/
  if (flags == FFI_TYPE_STRUCT)
    stack->gprArgs[intArgC++] = (int) ecif->rvalue;
 
  for (ptr = ecif->cif->arg_types, i = ecif->cif->nargs;
       i > 0;
       i--, ptr++, p_argv++)
    {
      switch ((*ptr)->type) {
 
      case FFI_TYPE_FLOAT:
	if (fprArgC < MAX_FPRARGS)
	  stack->fprArgs[fprArgC++].f = *(float *) *p_argv;
	else
	  stack->outArgs[outArgC++] = *(int *) *p_argv;
	break;
 
      case FFI_TYPE_DOUBLE:
	dblValue = *(double *) *p_argv;
	ffi_insert_double(dblValue, stack, &fprArgC, &outArgC);
	break;
	
      case FFI_TYPE_UINT64:
      case FFI_TYPE_SINT64:
	llngValue = *(unsigned long long *) *p_argv;
	ffi_insert_int64(llngValue, stack, &intArgC, &outArgC);
	break;
 
      case FFI_TYPE_UINT8:
	gprValue = *(unsigned char *)*p_argv;
	ffi_insert_int(gprValue, stack, &intArgC, &outArgC);
	break;
 
      case FFI_TYPE_SINT8:
	gprValue = *(signed char *)*p_argv;
	ffi_insert_int(gprValue, stack, &intArgC, &outArgC);
	break;
 
      case FFI_TYPE_UINT16:
	gprValue = *(unsigned short *)*p_argv;
	ffi_insert_int(gprValue, stack, &intArgC, &outArgC);
	break;
 
      case FFI_TYPE_SINT16:
	gprValue = *(signed short *)*p_argv;
	ffi_insert_int(gprValue, stack, &intArgC, &outArgC);
	break;
 
      case FFI_TYPE_STRUCT:
	/*--------------------------------------------------*/
	/* If structure > 8 bytes then it goes on the stack */
	/*--------------------------------------------------*/
	if (((*ptr)->size > 8) ||
	    ((*ptr)->size > 4  &&
	     (*ptr)->size < 8))
	  strFlags = STR_STACK;
	else
	  strFlags = ffi_check_struct((ffi_type *) *ptr, &strFlags);
 
	switch (strFlags) {
	/*-------------------------------------------*/
	/* Structure that will fit in one or two GPR */
	/*-------------------------------------------*/
	case STR_GPR :
	  if ((*ptr)->size <= 4) {
	    gprValue = *(unsigned int *) *p_argv;
	    gprValue = gprValue >> ((4 - (*ptr)->size) * 8);
	    ffi_insert_int(gprValue, stack, &intArgC, &outArgC);
	  }
	  else {
	    llngValue = *(unsigned long long *) *p_argv;
	    ffi_insert_int64(llngValue, stack, &intArgC, &outArgC);
	  }
	  break;
 
	/*-------------------------------------------*/
	/* Structure that will fit in one FPR        */
	/*-------------------------------------------*/
	case STR_FPR :
	  dblValue = *(double *) *p_argv;
	  ffi_insert_double(dblValue, stack, &fprArgC, &outArgC);
	  break;
 
	/*-------------------------------------------*/
	/* Structure that must be copied to stack    */
	/*-------------------------------------------*/
	default :
	  structCopySize = (((*ptr)->size + 15) & ~0xF);
	  copySpace -= structCopySize;
	  memcpy(copySpace, (char *)*p_argv, (*ptr)->size);
	  gprValue = (unsigned) copySpace;
	  if (intArgC < MAX_GPRARGS)
	    stack->gprArgs[intArgC++] = gprValue;
	  else
	    stack->outArgs[outArgC++] = gprValue;
	}
	break;
 
#if FFI_TYPE_LONGDOUBLE != FFI_TYPE_DOUBLE
      case FFI_TYPE_LONGDOUBLE:
	structCopySize = (((*ptr)->size + 15) & ~0xF);
	copySpace -= structCopySize;
	memcpy(copySpace, (char *)*p_argv, (*ptr)->size);
	gprValue = (unsigned) copySpace;
	if (intArgC < MAX_GPRARGS)
	  stack->gprArgs[intArgC++] = gprValue;
	else
	  stack->outArgs[outArgC++] = gprValue;
	break;
#endif
 
      case FFI_TYPE_INT:
      case FFI_TYPE_UINT32:
      case FFI_TYPE_SINT32:
      case FFI_TYPE_POINTER:
	gprValue = *(unsigned *)*p_argv;
	if (intArgC < MAX_GPRARGS)
	  stack->gprArgs[intArgC++] = gprValue;
	else
	  stack->outArgs[outArgC++] = gprValue;
	break;
 
      }
    }
}
 
/*======================== End of Routine ============================*/
 
/*====================================================================*/
/*                                                                    */
/* Name     - ffi_prep_cif_machdep.                                   */
/*                                                                    */
/* Function - Perform machine dependent CIF processing.               */
/*                                                                    */
/*====================================================================*/
 
ffi_status
ffi_prep_cif_machdep(ffi_cif *cif)
{
  int i;
  ffi_type **ptr;
  unsigned bytes;
  int fpArgC  = 0,
    intArgC = 0;
  unsigned flags = 0;
  unsigned structCopySize = 0;
 
  /*-----------------------------------------------------------------*/
  /* Extra space required in stack for overflow parameters.          */
  /*-----------------------------------------------------------------*/
  bytes = 0;
 
  /*--------------------------------------------------------*/
  /* Return value handling.  The rules are as follows:	    */
  /* - 32-bit (or less) integer values are returned in gpr2 */
  /* - Structures are returned as pointers in gpr2	    */
  /* - 64-bit integer values are returned in gpr2 and 3	    */
  /* - Single/double FP values are returned in fpr0	    */
  /*--------------------------------------------------------*/
  flags = cif->rtype->type;
 
  /*------------------------------------------------------------------------*/
  /* The first MAX_GPRARGS words of integer arguments, and the      	    */
  /* first MAX_FPRARGS floating point arguments, go in registers; the rest  */
  /* goes on the stack.  Structures and long doubles (if not equivalent     */
  /* to double) are passed as a pointer to a copy of the structure.	    */
  /* Stuff on the stack needs to keep proper alignment.  		    */
  /*------------------------------------------------------------------------*/
  for (ptr = cif->arg_types, i = cif->nargs; i > 0; i--, ptr++)
    {
      switch ((*ptr)->type)
	{
	case FFI_TYPE_FLOAT:
	case FFI_TYPE_DOUBLE:
	  fpArgC++;
	  if (fpArgC > MAX_FPRARGS && intArgC%2 != 0)
	    intArgC++;
	  break;
 
	case FFI_TYPE_UINT64:
	case FFI_TYPE_SINT64:
	  /*----------------------------------------------------*/
	  /* 'long long' arguments are passed as two words, but */
	  /* either both words must fit in registers or both go */
	  /* on the stack.  If they go on the stack, they must  */
	  /* be 8-byte-aligned. 			 	      */
	  /*----------------------------------------------------*/
	  if ((intArgC == MAX_GPRARGS-1) ||
	      (intArgC >= MAX_GPRARGS)   &&
	      (intArgC%2 != 0))
	    intArgC++;
	  intArgC += 2;
	  break;
 
	case FFI_TYPE_STRUCT:
#if FFI_TYPE_LONGDOUBLE != FFI_TYPE_DOUBLE
	case FFI_TYPE_LONGDOUBLE:
#endif
	  /*----------------------------------------------------*/
	  /* We must allocate space for a copy of these to      */
	  /* enforce pass-by-value. Pad the space up to a       */
	  /* multiple of 16 bytes (the maximum alignment 	      */
	  /* required for anything under the SYSV ABI). 	      */
	  /*----------------------------------------------------*/
	  structCopySize += ((*ptr)->size + 15) & ~0xF;
	  /*----------------------------------------------------*/
	  /* Fall through (allocate space for the pointer).     */
	  /*----------------------------------------------------*/
 
	default:
	  /*----------------------------------------------------*/
	  /* Everything else is passed as a 4-byte word in a    */
	  /* GPR either the object itself or a pointer to it.   */
	  /*----------------------------------------------------*/
	  intArgC++;
	  break;
	}
    }
 
  /*-----------------------------------------------------------------*/
  /* Stack space.                                                    */
  /*-----------------------------------------------------------------*/
  if (intArgC > MAX_GPRARGS)
    bytes += (intArgC - MAX_GPRARGS) * sizeof(int);
  if (fpArgC > MAX_FPRARGS)
    bytes += (fpArgC - MAX_FPRARGS) * sizeof(double);
 
  /*-----------------------------------------------------------------*/
  /* The stack space allocated needs to be a multiple of 16 bytes.   */
  /*-----------------------------------------------------------------*/
  bytes = (bytes + 15) & ~0xF;
 
  /*-----------------------------------------------------------------*/
  /* Add in the space for the copied structures.                     */
  /*-----------------------------------------------------------------*/
  bytes += structCopySize;
 
  cif->flags = flags;
  cif->bytes = bytes;
 
  return FFI_OK;
}
 
/*======================== End of Routine ============================*/
 
/*====================================================================*/
/*                                                                    */
/* Name     - ffi_call.                                               */
/*                                                                    */
/* Function - Call the FFI routine.                                   */
/*                                                                    */
/*====================================================================*/
 
void
ffi_call(ffi_cif *cif,
	 void (*fn)(),
	 void *rvalue,
	 void **avalue)
{
  extended_cif ecif;
 
  ecif.cif    = cif;
  ecif.avalue = avalue;
 
  /*-----------------------------------------------------------------*/
  /* If the return value is a struct and we don't have a return      */
  /* value address then we need to make one                          */
  /*-----------------------------------------------------------------*/
  if ((rvalue == NULL) &&
      (cif->rtype->type == FFI_TYPE_STRUCT))
    ecif.rvalue = alloca(cif->rtype->size);
  else
    ecif.rvalue = rvalue;
 
  switch (cif->abi)
    {
    case FFI_SYSV:
      ffi_call_SYSV(ffi_prep_args,
		    &ecif, cif->bytes,
		    cif->flags, ecif.rvalue, fn);
      break;
 
    default:
      FFI_ASSERT(0);
      break;
    }
}
 
/*======================== End of Routine ============================*/
