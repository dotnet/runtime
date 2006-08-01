/*------------------------------------------------------------------*/
/*                                                                  */
/* Name        - tramp-alpha.c                                      */
/*                                                                  */
/* Function    - JIT trampoline code for Alpha.                     */
/*                                                                  */
/* Name        - Sergey Tikhonov (tsv@solvo.ru)                     */
/*                                                                  */
/* Date        - January, 2006                                      */
/*                                                                  */
/* Derivation  - From exceptions-amd64 & exceptions-ia64            */
/*               Dietmar Maurer (dietmar@ximian.com)                */
/*               Zoltan Varga (vargaz@gmail.com)                    */
/*                                                                  */
/*                                                                  */
/*------------------------------------------------------------------*/

/*------------------------------------------------------------------*/
/*                 D e f i n e s                                    */
/*------------------------------------------------------------------*/
#define NOT_IMPLEMENTED(x) \
        g_error ("FIXME: %s is not yet implemented.", x);

#define ALPHA_DEBUG(x) \
        g_debug ("ALPHA_DEBUG: %s is called.", x);

/*========================= End of Defines =========================*/

/*------------------------------------------------------------------*/
/*                 I n c l u d e s                                  */
/*------------------------------------------------------------------*/

#include <config.h>
#include <glib.h>
#include <string.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/tabledefs.h>
#include <mono/arch/alpha/alpha-codegen.h>
#include <mono/metadata/mono-debug-debugger.h>

#include "mini.h"
#include "mini-alpha.h"

/*========================= End of Includes ========================*/

/*------------------------------------------------------------------*/
/*                 T y p e d e f s                                  */
/*------------------------------------------------------------------*/

/*========================= End of Typedefs ========================*/

/*------------------------------------------------------------------*/
/*                   P r o t o t y p e s                            */
/*------------------------------------------------------------------*/

/*========================= End of Prototypes ======================*/

/*------------------------------------------------------------------*/
/*                 G l o b a l   V a r i a b l e s                  */
/*------------------------------------------------------------------*/


/*====================== End of Global Variables ===================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_create_trampoline_code                            */
/*                                                                  */
/* Function     - Create the designated type of trampoline according*/
/*                to the 'tramp_type' parameter.                    */
/*                                                                  */
/*
  This code should expect to be called by tramp stub function
  On Alpha:
  - pv points to start of stub function
  - at points to start of this trampoline
  - allocate stack to save all regs and lmfs
  - save regs
  - fill params for trampoline methods (They expect 4 params)
  - call trampoline method (by standard call convention (pv + ra))
  - save return value (r0)
  - restore saved regs + lmfs
  - restore stack (don't forget space allocated by stub)
  - use saved return values as new address to give control to
  - return or jump to new address (don't expect to return here -
    don't save return address. RA will be holding return address
    of original caller of tramp stub function). New address function
    expect standart calling convention (pv)

*/
/*------------------------------------------------------------------*/

guchar *
mono_arch_create_trampoline_code (MonoTrampolineType tramp_type)
{
  unsigned int *buf, *code, *tramp;
  int i, lmf_offset, offset, method_offset, tramp_offset,
    saved_regs_offset, saved_fpregs_offset, framesize, off;
  gboolean has_caller;
  
  ALPHA_DEBUG("mono_arch_create_trampoline_code");
  
  if (tramp_type == MONO_TRAMPOLINE_JUMP)
    has_caller = FALSE;
  else
    has_caller = TRUE;
  
  code = buf = mono_global_codeman_reserve (512);
  
  framesize = 512 + sizeof (MonoLMF);
  framesize = (framesize +
	       (MONO_ARCH_FRAME_ALIGNMENT - 1)) & ~(MONO_ARCH_FRAME_ALIGNMENT - 1);
  
  offset = 16;
  
  // Expect that generated code is called with 2 parameters
  // method and tramp (in a0 and a1)
  
  // Allocate stack
  alpha_lda(code, alpha_sp, alpha_sp, -framesize);

  /* store call convention parameters on stack.*/
  alpha_stq( code, alpha_ra, alpha_sp, 0 ); // ra
  alpha_stq( code, alpha_fp, alpha_sp, 8 ); // fp
  
  // Store all integer regs
  for (i=0; i<30 /*alpha_pc*/; i++)
    {
      alpha_stq(code, i, alpha_sp, offset);
      offset += 8;
    }
  
  // Store all fp regs (TODO)

  /* set the frame pointer */
  alpha_mov1( code, alpha_sp, alpha_fp );
  
  /* Arg3 is the method/vtable ptr */
  alpha_ldq(code, alpha_a2, alpha_sp, framesize);
  //alpha_mov1(code, alpha_a0, alpha_a2);
  
  /* Arg4 is the trampoline address */
  alpha_mov1(code, alpha_pv, alpha_a3);
  //alpha_mov1(code, alpha_a1, alpha_a3);
  
  /* Arg1 is the pointer to the saved registers */
  alpha_lda(code, alpha_a0, alpha_sp, 16);
  
  /* Arg2 is the address of the calling code */
  if (has_caller)
    alpha_mov1(code, alpha_ra, alpha_a1);
  else
    alpha_mov1(code, alpha_zero, alpha_a1);
  
  /* Arg3 is the method/vtable ptr 
     alpha_mov1(code, alpha_a0, alpha_a2);
     
     Arg4 is the trampoline address
     alpha_mov1(code, alpha_a1, alpha_a3);
  */
  
  if (tramp_type == MONO_TRAMPOLINE_CLASS_INIT)
    tramp = (unsigned int*)mono_class_init_trampoline;
  else if (tramp_type == MONO_TRAMPOLINE_AOT)
    tramp = (unsigned int*)mono_aot_trampoline;
  else if (tramp_type == MONO_TRAMPOLINE_DELEGATE)
    tramp = (unsigned int*)mono_delegate_trampoline;
  else
    tramp = (unsigned int*)mono_magic_trampoline;
  
  off = (char *)code - (char *)buf;
  off += 2*4;
  
  if (off % 8)
    {
      alpha_nop(code);
      off += 4;
    }
  
  // alpha_at points to start of this method !!!
  alpha_ldq(code, alpha_pv, alpha_at, off);
  alpha_br(code, alpha_zero, 2);
  
  *code = (unsigned int)(((unsigned long)tramp) & 0xFFFFFFFF);
  code++;
  *code = (unsigned int)((((unsigned long)tramp) >> 32) & 0xFFFFFFFF);
  code++;
  
  alpha_jsr(code, alpha_ra, alpha_pv, 0);
  
  alpha_stq(code, alpha_r0, alpha_sp, framesize);
  
  /* Restore LMF */
  
  offset = 16;
  
  // Restore all integer regs
  for (i=0; i<30 /*alpha_pc*/; i++)
    {
      alpha_ldq(code, i, alpha_sp, offset);
      offset += 8;
    }
  
  alpha_ldq(code, alpha_r0, alpha_sp, framesize);
  
  // Restore stack
  alpha_lda(code, alpha_sp, alpha_sp, framesize+16);
  
  if (tramp_type == MONO_TRAMPOLINE_CLASS_INIT)
    alpha_ret (code, alpha_ra, 1);
  else
    {
      /* call the compiled method */
      // It will expect correct call frame 
      
      alpha_mov1(code, alpha_r0, alpha_pv);
      alpha_jsr (code, alpha_zero, alpha_pv, 0);
    }
  
  g_assert (((char *)code - (char *)buf) <= 512);
  
  mono_arch_flush_icache (buf, (char *)code - (char *)buf);
  
  return buf;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_create_jit_trampoline                   */
/*                                                                  */
/* Function     - Creates a trampoline function for virtual methods.*/
/*                If the created code is called it first starts JIT */
/*                compilation and then calls the newly created      */
/*                method. It also replaces the corresponding vtable */
/*                entry (see s390_magic_trampoline).                */
/*                                                                  */
/*                A trampoline consists of two parts: a main        */
/*                fragment, shared by all method trampolines, and   */
/*                and some code specific to each method, which      */
/*                hard-codes a reference to that method and then    */
/*                calls the main fragment.                          */
/*                                                                  */
/*                The main fragment contains a call to              */
/*                's390_magic_trampoline', which performs a call    */
/*                to the JIT compiler and substitutes the method-   */
/*                specific fragment with some code that directly    */
/*                calls the JIT-compiled method.                    */
/*                                                                  */
/* Parameter    - method - Pointer to the method information        */
/*                                                                  */
/* Returns      - A pointer to the newly created code               */
/*                                                                  */
/*------------------------------------------------------------------*/

gpointer
mono_arch_create_jit_trampoline (MonoMethod *method)
{
	ALPHA_DEBUG("mono_arch_create_jit_trampoline: check MONO_ARCH_HAVE_CREATE_SPECIFIC_TRAMPOLINE define");

//	NOT_IMPLEMENTED("mono_arch_create_jit_trampoline: check MONO_ARCH_HAVE_CREATE_SPECIFIC_TRAMPOLINE define");

        return 0;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_create_jump_trampoline                  */
/*                                                                  */
/* Function     - Create the designated type of trampoline according*/
/*                to the 'tramp_type' parameter.                    */
/*                                                                  */
/*------------------------------------------------------------------*/

MonoJitInfo *
mono_arch_create_jump_trampoline (MonoMethod *method)
{
	ALPHA_DEBUG("mono_arch_create_jump_trampoline");

	NOT_IMPLEMENTED("mono_arch_create_jump_trampoline");
	
        return 0;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_create_specific_trampoline              */
/*                                                                  */
/* Function     - ???Create the designated type of trampoline according*/
/*                to the 'tramp_type' parameter.                    */
/*                                                                  */
/* This method should create a stub code that will transfer
   control to corresponding trampoline. We need to pass "arg1" and
   start address of this stab method to trampoline code.
   We should not modify any registers!!!
   For Alpha:
   - allocate 2 qword on stack
   - save stab start address on 8(sp)
   - save "arg1" on 0(sp)
   - jump to trampoline code keeping original caller return address
     in ra
*/
/*------------------------------------------------------------------*/

#define TRAMPOLINE_SIZE 52

gpointer
mono_arch_create_specific_trampoline (gpointer arg1,
	MonoTrampolineType tramp_type, MonoDomain *domain, guint32 *code_len)
{
  unsigned int *code, *buf, *tramp, *real_code;
  int offset, size, jump_offset;
  
  //  ALPHA_DEBUG("mono_arch_create_specific_trampoline");
  
  tramp = mono_get_trampoline_code (tramp_type);
  
  code = buf = g_alloca (TRAMPOLINE_SIZE);
  
  /* push trampoline address */
  //amd64_lea_membase (code, AMD64_R11, AMD64_RIP, -7);
  //amd64_push_reg (code, AMD64_R11);
  
  // Allocate two qwords on stack
  alpha_lda(code, alpha_sp, alpha_sp, -16);
  
  alpha_stq(code, alpha_pv, alpha_sp, 8);
  
  // Load arg1 into alpha_at
  offset = (char *)code - (char *)buf;
  offset += 2*4;
  if (offset % 8)
    {
      alpha_nop(code);
      offset += 4;
    }
  
  alpha_ldq(code, alpha_at, alpha_pv, offset);
  alpha_br(code, alpha_zero, 2);
  
  *code = (unsigned int)(((unsigned long)arg1) & 0xFFFFFFFF);
  code++;
  *code = (unsigned int)((((unsigned long)arg1) >> 32) & 0xFFFFFFFF);
  code++;
  
  // Store arg1 on stack
  alpha_stq(code, alpha_at, alpha_sp, 0);
  
  offset = (char *)code - (char *)buf;
  offset += 2*4;
  if (offset % 8)
    {
      alpha_nop(code);
      offset += 4;
    }
  
  alpha_ldq(code, alpha_at, alpha_pv, offset);
  alpha_br(code, alpha_zero, 2);
  
  *code = (unsigned int)(((unsigned long)tramp) & 0xFFFFFFFF);
  code++;
  *code = (unsigned int)((((unsigned long)tramp) >> 32) & 0xFFFFFFFF);
  code++;
  
  // Jump to trampoline
  alpha_jmp(code, alpha_zero, alpha_at, 0);
  
  
  /* push argument */
  //if (amd64_is_imm32 ((gint64)arg1))
  //        amd64_push_imm (code, (gint64)arg1);
  //else {
  //        amd64_mov_reg_imm (code, AMD64_R11, arg1);
  //        amd64_push_reg (code, AMD64_R11);
  //}
  
  // jump_offset = code - buf;
  //amd64_jump_disp (code, 0xffffffff);
  
  g_assert (((char *)code - (char *)buf) <= TRAMPOLINE_SIZE);
  mono_domain_lock (domain);
  /*
   * FIXME: Changing the size to code - buf causes strange crashes during
   * mcs bootstrap.
   */
  real_code = mono_code_manager_reserve (domain->code_mp, TRAMPOLINE_SIZE);
  size = (char *)code - (char *)buf;
  mono_domain_unlock (domain);
  
  memcpy (real_code, buf, size);
  
  printf("mono_arch_create_specific_trampoline: Target: %p, Arg1: %p\n",
	 real_code, arg1);
  
  /* Fix up jump */
  //g_assert ((((gint64)tramp) >> 32) == 0);
  //code = (guint8*)real_code + jump_offset;
  //amd64_jump_disp (code, tramp - code);
  
  mono_jit_stats.method_trampolines++;
  
  if (code_len)
    *code_len = size;
  
  mono_arch_flush_icache (real_code, size);
  
  return real_code;
}
/*========================= End of Function ========================*/

void
mono_arch_nullify_class_init_trampoline (guint8 *code, gssize *regs)
{
  ALPHA_DEBUG("mono_arch_nullify_class_init_trampoline");
}


void
mono_arch_patch_delegate_trampoline (guint8 *code, guint8 *tramp,
				     gssize *regs, guint8 *addr)
{
  ALPHA_DEBUG("mono_arch_patch_delegate_trampoline");
}

void
mono_arch_patch_callsite (guint8 *code, guint8 *addr)
{
  unsigned long *p = (unsigned int *)(code-12);
  
  unsigned int *pcode = (unsigned int *)code;
  unsigned long gp = pcode;
  unsigned int call_addr_inst;
  short high_offset, low_offset;

  ALPHA_DEBUG("mono_arch_patch_callsite");

  // Code points to the next instruction after the "jsr"
  // In current implementation where we put jump addresses
  // inside the code - we need to put "new" address into
  // "code-12"

  // With new method of using GOT we need to find address
  // where function address is stored
  // code points to two insts:
  // ldah gp, high_offset(ra)
  // lda gp, low_offset(gp)
  // 

  high_offset = *((short *)pcode);
  low_offset = *((short *)(pcode + 1));

  gp += 65536 * high_offset + low_offset;

  call_addr_inst = *(pcode - 2);

  // Check for load address instruction
  // It should be ldq t12, offset(gp)
  if ((call_addr_inst & 0xFFFF0000) == 0xA77D0000)
  {
    gp += *((short *)(pcode - 2));

    p = gp;

    printf("Patch callsite at %p to %p\n", p, addr);

    // TODO - need to to interlocked update here
    *p = (unsigned long)addr;
  }
}

/*
 * mono_arch_get_unbox_trampoline:
 * @m: method pointer
 * @addr: pointer to native code for @m
 *
 * when value type methods are called through the vtable we need to unbox the
 * this argument. This method returns a pointer to a trampoline which does
 * unboxing before calling the method
 */
gpointer
mono_arch_get_unbox_trampoline (MonoMethod *m, gpointer addr)
{
  ALPHA_DEBUG("mono_arch_get_unbox_trampoline");

  return 0;
}

void
mono_arch_nullify_plt_entry (guint8 *code)
{
        g_assert_not_reached ();
}

void
mono_arch_patch_plt_entry (guint8 *code, guint8 *addr)
{
        g_assert_not_reached ();
}

