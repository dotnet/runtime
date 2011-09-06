/*------------------------------------------------------------------*/
/*                                                                  */
/* Name        - exceptions-alpha.c                                 */
/*                                                                  */
/* Function    - Exception support for Alpha.                       */
/*                                                                  */
/* Name        - Sergey Tikhonov (tsv@solvo.ru)                     */
/*                                                                  */
/* Date        - January, 2006                                      */
/*                                                                  */
/* Derivation  - From exceptions-amd64 & exceptions-ia64            */
/*               Paolo Molaro (lupus@ximian.com)                    */
/*               Dietmar Maurer (dietmar@ximian.com)                */
/*               Zoltan Varga (vargaz@gmail.com)                    */
/*                                                                  */
/*                                                                  */
/*------------------------------------------------------------------*/

/*------------------------------------------------------------------*/
/*                 D e f i n e s                                    */
/*------------------------------------------------------------------*/
#define ALPHA_DEBUG(x) \
  if (mini_alpha_verbose_level)	\
    g_debug ("ALPHA_DEBUG: %s is called.", x);

#define ALPHA_PRINT if (mini_alpha_verbose_level)

#define SZ_THROW     384

extern int mini_alpha_verbose_level;

/*========================= End of Defines =========================*/


/*------------------------------------------------------------------*/
/*                 I n c l u d e s                                  */
/*------------------------------------------------------------------*/

#include <config.h>
#include <glib.h>
#include <signal.h>
#include <string.h>
#include <ucontext.h>

#include <mono/arch/alpha/alpha-codegen.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/mono-debug.h>

#include "mini.h"
#include "mini-alpha.h"

/*========================= End of Includes ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_get_call_filter                         */
/*                                                                  */
/* Function     - Return a pointer to a method which calls an       */
/*                exception filter. We also use this function to    */
/*                call finally handlers (we pass NULL as @exc       */
/*                object in this case).                             */
/*                                                                  */
/*------------------------------------------------------------------*/

gpointer
mono_arch_get_call_filter (void)
{
  static gboolean inited = FALSE;
  static unsigned int *start_code;
  unsigned int *code;

  ALPHA_DEBUG("mono_arch_get_call_filter");

  if (inited)
    return start_code;

  start_code = code = mono_global_codeman_reserve (128 * 4);

  /* call_filter (MonoContext *ctx, unsigned long eip) */
  code = start_code;

  alpha_ldah( code, alpha_gp, alpha_pv, 0 );
  alpha_lda( code, alpha_gp, alpha_gp, 0 );     // ldgp gp, 0(pv)

  /* store call convention parameters on stack */
  alpha_lda(code, alpha_sp, alpha_sp, -(8*25)); // Save 22 regs + RA, FP
  alpha_stq(code, alpha_ra, alpha_sp, 0);
  alpha_stq(code, alpha_fp, alpha_sp, 8);

  /* set the frame pointer */
  alpha_mov1( code, alpha_sp, alpha_fp );

  /* Save registers */
  alpha_stq(code, alpha_r1, alpha_fp, (16+(8*0)));
  alpha_stq(code, alpha_r2, alpha_fp, (16+(8*1)));
  alpha_stq(code, alpha_r3, alpha_fp, (16+(8*2)));
  alpha_stq(code, alpha_r4, alpha_fp, (16+(8*3)));
  alpha_stq(code, alpha_r5, alpha_fp, (16+(8*4)));
  alpha_stq(code, alpha_r6, alpha_fp, (16+(8*5)));
  alpha_stq(code, alpha_r7, alpha_fp, (16+(8*6)));
  alpha_stq(code, alpha_r8, alpha_fp, (16+(8*7)));
  alpha_stq(code, alpha_r9, alpha_fp, (16+(8*8)));
  alpha_stq(code, alpha_r10, alpha_fp, (16+(8*9)));
  alpha_stq(code, alpha_r11, alpha_fp, (16+(8*10)));
  alpha_stq(code, alpha_r12, alpha_fp, (16+(8*11)));
  alpha_stq(code, alpha_r13, alpha_fp, (16+(8*12)));
  alpha_stq(code, alpha_r14, alpha_fp, (16+(8*13)));
  alpha_stq(code, alpha_r22, alpha_fp, (16+(8*14)));
  alpha_stq(code, alpha_r23, alpha_fp, (16+(8*15)));
  alpha_stq(code, alpha_r24, alpha_fp, (16+(8*16)));
  alpha_stq(code, alpha_r25, alpha_fp, (16+(8*17)));
  alpha_stq(code, alpha_r26, alpha_fp, (16+(8*18)));
  alpha_stq(code, alpha_r27, alpha_fp, (16+(8*19)));
  alpha_stq(code, alpha_r28, alpha_fp, (16+(8*20)));
  alpha_stq(code, alpha_r29, alpha_fp, (16+(8*21)));

  /* Load regs from ctx */

  alpha_ldq(code, alpha_r1, alpha_a0,
	    G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r1]));
  alpha_ldq(code, alpha_r2, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r2]));
  alpha_ldq(code, alpha_r3, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r3]));
  alpha_ldq(code, alpha_r4, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r4]));
  alpha_ldq(code, alpha_r5, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r5]));
  alpha_ldq(code, alpha_r6, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r6]));
  alpha_ldq(code, alpha_r7, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r7]));
  alpha_ldq(code, alpha_r8, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r8]));
  alpha_ldq(code, alpha_r9, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r9]));
  alpha_ldq(code, alpha_r10, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r10]));
  alpha_ldq(code, alpha_r11, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r11]));
  alpha_ldq(code, alpha_r12, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r12]));
  alpha_ldq(code, alpha_r13, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r13]));
  alpha_ldq(code, alpha_r14, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r14]));
  alpha_ldq(code, alpha_r15, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r15]));
  alpha_ldq(code, alpha_r22, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r22]));
  alpha_ldq(code, alpha_r23, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r23]));
  alpha_ldq(code, alpha_r24, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r24]));
  alpha_ldq(code, alpha_r25, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r25]));
  alpha_ldq(code, alpha_r26, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r26]));
  alpha_ldq(code, alpha_r27, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r27]));
  alpha_ldq(code, alpha_r28, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r28]));
  alpha_ldq(code, alpha_r29, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r29]));

  alpha_mov1(code, alpha_a1, alpha_pv);

  /* call the handler */
  alpha_jsr(code, alpha_ra, alpha_pv, 0);

  /* restore saved regs */
  alpha_ldq(code, alpha_r1, alpha_sp, (16+(8*0)));
  alpha_ldq(code, alpha_r2, alpha_sp, (16+(8*1)));
  alpha_ldq(code, alpha_r3, alpha_sp, (16+(8*2)));
  alpha_ldq(code, alpha_r4, alpha_sp, (16+(8*3)));
  alpha_ldq(code, alpha_r5, alpha_sp, (16+(8*4)));
  alpha_ldq(code, alpha_r6, alpha_sp, (16+(8*5)));
  alpha_ldq(code, alpha_r7, alpha_sp, (16+(8*6)));
  alpha_ldq(code, alpha_r8, alpha_sp, (16+(8*7)));
  alpha_ldq(code, alpha_r9, alpha_sp, (16+(8*8)));
  alpha_ldq(code, alpha_r10, alpha_sp, (16+(8*9)));
  alpha_ldq(code, alpha_r11, alpha_sp, (16+(8*10)));
  alpha_ldq(code, alpha_r12, alpha_sp, (16+(8*11)));
  alpha_ldq(code, alpha_r13, alpha_sp, (16+(8*12)));
  alpha_ldq(code, alpha_r14, alpha_sp, (16+(8*13)));
  alpha_ldq(code, alpha_r22, alpha_sp, (16+(8*14)));
  alpha_ldq(code, alpha_r23, alpha_sp, (16+(8*15)));
  alpha_ldq(code, alpha_r24, alpha_sp, (16+(8*16)));
  alpha_ldq(code, alpha_r25, alpha_sp, (16+(8*17)));
  alpha_ldq(code, alpha_r26, alpha_sp, (16+(8*18)));
  alpha_ldq(code, alpha_r27, alpha_sp, (16+(8*19)));
  alpha_ldq(code, alpha_r28, alpha_sp, (16+(8*20)));
  alpha_ldq(code, alpha_r29, alpha_sp, (16+(8*21)));

  alpha_ldq(code, alpha_ra, alpha_sp, 0);
  alpha_ldq(code, alpha_fp, alpha_sp, 8);
  alpha_lda(code, alpha_sp, alpha_sp, (8*25)); // Save 22 regs + RA, FP
  
  alpha_ret(code, alpha_ra, 1);

  inited = TRUE;

  g_assert (( ((char *)code) - (char *)start_code) < 128 * 4);

  return start_code;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - arch_get_throw_exception                          */
/*                                                                  */
/* Function     - Return a function pointer which can be used to    */
/*                raise exceptions. The returned function has the   */
/*                following signature:                              */
/*                void (*func) (MonoException *exc);                */
/*                                                                  */
/*------------------------------------------------------------------*/

static void throw_exception(MonoException *exc, unsigned long RA,
			    unsigned long *SP, unsigned long rethrow)
{
  static void (*restore_context) (MonoContext *);
  MonoContext ctx;
  unsigned long *LSP = SP - 24;

  //g_print("ALPHA: throw_exception - Exc: %p, RA: %0lX, SP: %p\n",
  //	  exc, RA, SP);

  if (!restore_context)
    restore_context = mono_arch_get_restore_context ();

  // Save stored regs into context
  ctx.uc_mcontext.sc_regs[alpha_r0] = LSP[0];
  ctx.uc_mcontext.sc_regs[alpha_r1] = LSP[1];
  ctx.uc_mcontext.sc_regs[alpha_r2] = LSP[2];
  ctx.uc_mcontext.sc_regs[alpha_r3] = LSP[3];
  ctx.uc_mcontext.sc_regs[alpha_r4] = LSP[4];
  ctx.uc_mcontext.sc_regs[alpha_r5] = LSP[5];
  ctx.uc_mcontext.sc_regs[alpha_r6] = LSP[6];
  ctx.uc_mcontext.sc_regs[alpha_r7] = LSP[7];
  ctx.uc_mcontext.sc_regs[alpha_r8] = LSP[8];
  ctx.uc_mcontext.sc_regs[alpha_r9] = LSP[9];
  ctx.uc_mcontext.sc_regs[alpha_r10] = LSP[10];
  ctx.uc_mcontext.sc_regs[alpha_r11] = LSP[11];
  ctx.uc_mcontext.sc_regs[alpha_r12] = LSP[12];
  ctx.uc_mcontext.sc_regs[alpha_r13] = LSP[13];
  ctx.uc_mcontext.sc_regs[alpha_r14] = LSP[14];
  ctx.uc_mcontext.sc_regs[alpha_r15] = LSP[15];
  ctx.uc_mcontext.sc_regs[alpha_r22] = LSP[16];
  ctx.uc_mcontext.sc_regs[alpha_r23] = LSP[17];
  ctx.uc_mcontext.sc_regs[alpha_r24] = LSP[18];
  ctx.uc_mcontext.sc_regs[alpha_r25] = LSP[19];
  ctx.uc_mcontext.sc_regs[alpha_r26] = LSP[20];
  ctx.uc_mcontext.sc_regs[alpha_r27] = LSP[21];
  ctx.uc_mcontext.sc_regs[alpha_r28] = LSP[22];
  ctx.uc_mcontext.sc_regs[alpha_r29] = LSP[23];

  ctx.uc_mcontext.sc_regs[alpha_r30] = (unsigned long)SP;
  ctx.uc_mcontext.sc_pc = RA;

  if (mono_object_isinst (exc, mono_defaults.exception_class))
    {
      MonoException *mono_ex = (MonoException*)exc;
      if (!rethrow)
	mono_ex->stack_trace = NULL;
    }

  mono_handle_exception (&ctx, exc);

  restore_context(&ctx);

  g_assert_not_reached ();
}

/*
** This trampoline code is called from the code as action on
** throw opcode. It should save all necessary regs somethere and
** call the C function to do the rest.
** For Alpha trampoline code should allocate space on stack and 
**  save all registers into it. Then call "throw_exception"
**  function with "exc" info and saved registers. The "throw_exception"
**  should handle the rest. The "throw_exception" has signature
**  void (*throw_exception)(MonoException *, long PC, long SP)
** The stack layout is:
** R0, R1, R2, R3, R4, R5, R6, R7, R8, R9, R10, R11, R12, R13, R14,
** R15, R22, R23, R24, R25, R26, R27, R28, R29
**
**
*/

static gpointer
get_throw_trampoline (gboolean rethrow)
{
  guint8 *start_code;
  unsigned int *code;

  start_code = mono_global_codeman_reserve (46*4);

  code = (unsigned int *)start_code;

  /* Exception is in a0 already */
  alpha_mov1(code, alpha_ra, alpha_a1);  // Return address
  alpha_mov1(code, alpha_sp, alpha_a2);  // Stack pointer

  if (rethrow)
    alpha_lda(code, alpha_a3, alpha_zero, 1);
  else
    alpha_mov1(code, alpha_zero, alpha_a3);

  alpha_lda(code, alpha_sp, alpha_sp, -(24*8)); // Allocate stack for regs

  alpha_stq(code, alpha_r0, alpha_sp, 0*8);
  alpha_stq(code, alpha_r1, alpha_sp, 1*8);
  alpha_stq(code, alpha_r2, alpha_sp, 2*8);
  alpha_stq(code, alpha_r3, alpha_sp, 3*8);
  alpha_stq(code, alpha_r4, alpha_sp, 4*8);
  alpha_stq(code, alpha_r5, alpha_sp, 5*8);
  alpha_stq(code, alpha_r6, alpha_sp, 6*8);
  alpha_stq(code, alpha_r7, alpha_sp, 7*8);
  alpha_stq(code, alpha_r8, alpha_sp, 8*8);
  alpha_stq(code, alpha_r9, alpha_sp, 9*8);
  alpha_stq(code, alpha_r10, alpha_sp, 10*8);
  alpha_stq(code, alpha_r11, alpha_sp, 11*8);
  alpha_stq(code, alpha_r12, alpha_sp, 12*8);
  alpha_stq(code, alpha_r13, alpha_sp, 13*8);
  alpha_stq(code, alpha_r14, alpha_sp, 14*8);
  alpha_stq(code, alpha_r15, alpha_sp, 15*8);
  alpha_stq(code, alpha_r22, alpha_sp, 16*8);
  alpha_stq(code, alpha_r23, alpha_sp, 17*8);
  alpha_stq(code, alpha_r24, alpha_sp, 18*8);
  alpha_stq(code, alpha_r25, alpha_sp, 19*8);
  alpha_stq(code, alpha_r26, alpha_sp, 20*8);
  alpha_stq(code, alpha_r27, alpha_sp, 21*8);
  alpha_stq(code, alpha_r28, alpha_sp, 22*8);
  alpha_stq(code, alpha_r29, alpha_sp, 23*8);

  alpha_mov1(code, alpha_zero, alpha_pv);
  alpha_lda(code, alpha_r1, alpha_zero,
	    ((unsigned long)throw_exception)&0xFFFF);
  alpha_lda(code, alpha_r2, alpha_zero,
            (((unsigned long)throw_exception) >> 16)&0xFFFF);
  alpha_lda(code, alpha_r3, alpha_zero,
            (((unsigned long)throw_exception) >> 32)&0xFFFF);
  alpha_lda(code, alpha_r4, alpha_zero,
            (((unsigned long)throw_exception) >> 48)&0xFFFF);
  alpha_zapnot_(code, alpha_r1, 0x3, alpha_r1);
  alpha_bis(code, alpha_r1, alpha_pv, alpha_pv);

  alpha_zapnot_(code, alpha_r2, 0x3, alpha_r2);
  alpha_sll_(code, alpha_r2, 16, alpha_r2);
  alpha_bis(code, alpha_r2, alpha_pv, alpha_pv);

  alpha_zapnot_(code, alpha_r3, 0x3, alpha_r3);
  alpha_sll_(code, alpha_r3, 32, alpha_r3);
  alpha_bis(code, alpha_r3, alpha_pv, alpha_pv);

  alpha_zapnot_(code, alpha_r4, 0x3, alpha_r4);
  alpha_sll_(code, alpha_r4, 48, alpha_r4);
  alpha_bis(code, alpha_r4, alpha_pv, alpha_pv); // pv - handle_exception addr

  alpha_jmp(code, alpha_zero, alpha_pv, 0);

  // alpha_break(code);

  g_assert (( ((char *)code) - (char *)start_code) < 46 * 4);

  return start_code;
}

/**
 * mono_arch_get_throw_exception:
 *
 * Returns a function pointer which can be used to raise
 * exceptions. The returned function has the following
 * signature: void (*func) (MonoException *exc);
 *
 */
gpointer
mono_arch_get_throw_exception (void)
{
  static guint8* start;
  static gboolean inited = FALSE;

  ALPHA_DEBUG("mono_arch_get_throw_exception");

  if (inited)
    return start;

  start = get_throw_trampoline (FALSE);

  inited = TRUE;

  return start;
}
/*========================= End of Function ========================*/


/**
 * mono_arch_get_throw_corlib_exception:
 *
 * Returns a function pointer which can be used to raise
 * corlib exceptions. The returned function has the following
 * signature: void (*func) (guint32 ex_token, guint32 offset);
 * Here, offset is the offset which needs to be substracted from the caller IP
 * to get the IP of the throw. Passing the offset has the advantage that it
 * needs no relocations in the caller.
 */
gpointer
mono_arch_get_throw_corlib_exception (void)
{
  static guint8* start;
  static gboolean inited = FALSE;
  unsigned int *code;
  guint64 throw_ex;

  ALPHA_DEBUG("mono_arch_get_throw_corlib_exception");

  if (inited)
    return start;

  start = mono_global_codeman_reserve (512);

  code = (unsigned int *)start;
  // Logic
  // Expect exception token as parameter
  // call mono_exception_from_token(void *, uint32 token)
  // Get result and call "throw_ex" (got from mono_arch_get_throw_exception)
  // Throw exception

  // The trampoline code will be called with PV set
  //  so expect correct ABI handling

  //alpha_ldah(code, alpha_gp, alpha_pv, 0);
  //alpha_lda(code, alpha_gp, alpha_gp, 0);
  alpha_lda(code, alpha_sp, alpha_sp, -(8*4));

  // Save caller GP
  alpha_stq(code, alpha_gp, alpha_sp, 24);

  /* store call convention parameters on stack */
  alpha_stq( code, alpha_ra, alpha_sp, 0 ); // ra
  alpha_stq( code, alpha_fp, alpha_sp, 8 ); // fp

  /* set the frame pointer */
  alpha_mov1(code, alpha_sp, alpha_fp );

  // Store throw_ip offset
  alpha_stq(code, alpha_a1, alpha_fp, 16);

  // Prepare to call "mono_exception_from_token (MonoImage *image, guint32 token)"
  // Move token to a1 reg
  alpha_mov1(code, alpha_a0, alpha_a1);

  alpha_mov1(code, alpha_zero, alpha_a0);
  alpha_lda(code, alpha_r1, alpha_zero,
            ((unsigned long)mono_defaults.exception_class->image)&0xFFFF);
  alpha_lda(code, alpha_r2, alpha_zero,
            (((unsigned long)mono_defaults.exception_class->image) >> 16)&0xFFFF);
  alpha_lda(code, alpha_r3, alpha_zero,
            (((unsigned long)mono_defaults.exception_class->image) >> 32)&0xFFFF);
  alpha_lda(code, alpha_r4, alpha_zero,
            (((unsigned long)mono_defaults.exception_class->image) >> 48)&0xFFFF);
  alpha_zapnot_(code, alpha_r1, 0x3, alpha_r1);
  alpha_bis(code, alpha_r1, alpha_a0, alpha_a0);

  alpha_zapnot_(code, alpha_r2, 0x3, alpha_r2);
  alpha_sll_(code, alpha_r2, 16, alpha_r2);
  alpha_bis(code, alpha_r2, alpha_a0, alpha_a0);

  alpha_zapnot_(code, alpha_r3, 0x3, alpha_r3);
  alpha_sll_(code, alpha_r3, 32, alpha_r3);
  alpha_bis(code, alpha_r3, alpha_a0, alpha_a0);

  alpha_zapnot_(code, alpha_r4, 0x3, alpha_r4);
  alpha_sll_(code, alpha_r4, 48, alpha_r4);
  alpha_bis(code, alpha_r4, alpha_a0, alpha_a0); // a0 - mono_defaults.exception_class->image

  alpha_mov1(code, alpha_zero, alpha_pv);
  alpha_lda(code, alpha_r1, alpha_zero,
            ((unsigned long)mono_exception_from_token)&0xFFFF);
  alpha_lda(code, alpha_r2, alpha_zero,
            (((unsigned long)mono_exception_from_token) >> 16)&0xFFFF);
  alpha_lda(code, alpha_r3, alpha_zero,
            (((unsigned long)mono_exception_from_token) >> 32)&0xFFFF);
  alpha_lda(code, alpha_r4, alpha_zero,
            (((unsigned long)mono_exception_from_token) >> 48)&0xFFFF);
  alpha_zapnot_(code, alpha_r1, 0x3, alpha_r1);
  alpha_bis(code, alpha_r1, alpha_pv, alpha_pv);

  alpha_zapnot_(code, alpha_r2, 0x3, alpha_r2);
  alpha_sll_(code, alpha_r2, 16, alpha_r2);
  alpha_bis(code, alpha_r2, alpha_pv, alpha_pv);

  alpha_zapnot_(code, alpha_r3, 0x3, alpha_r3);
  alpha_sll_(code, alpha_r3, 32, alpha_r3);
  alpha_bis(code, alpha_r3, alpha_pv, alpha_pv);

  alpha_zapnot_(code, alpha_r4, 0x3, alpha_r4);
  alpha_sll_(code, alpha_r4, 48, alpha_r4);
  alpha_bis(code, alpha_r4, alpha_pv, alpha_pv); // pv - mono_exception_from_token addr

  alpha_jsr(code, alpha_ra, alpha_pv, 0);

  // R0 holds pointer to initialised exception object

  throw_ex = (guint64)mono_arch_get_throw_exception ();

  alpha_mov1(code, alpha_r0, alpha_a0);

  // Calc return address
  alpha_mov1(code, alpha_fp, alpha_sp);
  alpha_ldq(code, alpha_ra, alpha_sp, 0);
  alpha_ldq(code, alpha_fp, alpha_sp, 8);
  alpha_ldq(code, alpha_a1, alpha_sp, 16);
  alpha_addq(code, alpha_ra, alpha_a1, alpha_ra);
  alpha_ldq(code, alpha_gp, alpha_sp, 24);

  // Modify stack to point to exception caller
  alpha_lda(code, alpha_sp, alpha_sp, (8*4));

  alpha_mov1(code, alpha_zero, alpha_pv);
  alpha_lda(code, alpha_r1, alpha_zero,
            ((unsigned long)throw_ex)&0xFFFF);
  alpha_lda(code, alpha_r2, alpha_zero,
            (((unsigned long)throw_ex) >> 16)&0xFFFF);
  alpha_lda(code, alpha_r3, alpha_zero,
            (((unsigned long)throw_ex) >> 32)&0xFFFF);
  alpha_lda(code, alpha_r4, alpha_zero,
            (((unsigned long)throw_ex) >> 48)&0xFFFF);
  alpha_zapnot_(code, alpha_r1, 0x3, alpha_r1);
  alpha_bis(code, alpha_r1, alpha_pv, alpha_pv);

  alpha_zapnot_(code, alpha_r2, 0x3, alpha_r2);
  alpha_sll_(code, alpha_r2, 16, alpha_r2);
  alpha_bis(code, alpha_r2, alpha_pv, alpha_pv);

  alpha_zapnot_(code, alpha_r3, 0x3, alpha_r3);
  alpha_sll_(code, alpha_r3, 32, alpha_r3);
  alpha_bis(code, alpha_r3, alpha_pv, alpha_pv);

  alpha_zapnot_(code, alpha_r4, 0x3, alpha_r4);
  alpha_sll_(code, alpha_r4, 48, alpha_r4);
  alpha_bis(code, alpha_r4, alpha_pv, alpha_pv); // pv - handle_exception addr

  alpha_jmp(code, alpha_zero, alpha_pv, 0);

  g_assert (((char *)code - (char *)start) < 512);

  inited = TRUE;

  return start;
}

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_handle_exception                        */
/*                                                                  */
/* Function     - Handle an exception raised by the JIT code.       */
/*                                                                  */
/* Parameters   - ctx       - Saved processor state                 */
/*                obj       - The exception object                  */
/*                                                                  */
/*------------------------------------------------------------------*/

gboolean
mono_arch_handle_exception (void *uc, gpointer obj)
{
  ALPHA_DEBUG("mono_arch_handle_exception");

  return mono_handle_exception (uc, obj);
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_get_restore_context                     */
/*                                                                  */
/* Function     - Return the address of the routine that will rest- */
/*                ore the context.                                  */
/*                                                                  */
/*------------------------------------------------------------------*/

gpointer
mono_arch_get_restore_context ()
{
  static guint8 *start_code = NULL;
  static gboolean inited = FALSE;
  unsigned int *code;

  ALPHA_DEBUG("mono_arch_get_restore_context");

  if (inited)
    return start_code;

  /* restore_contect (MonoContext *ctx) */

  start_code = mono_global_codeman_reserve (30*4);

  code = (unsigned int *)start_code;

  alpha_ldq(code, alpha_r0, alpha_a0,
	    G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r0]));
  alpha_ldq(code, alpha_r1, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r1]));
  alpha_ldq(code, alpha_r2, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r2]));
  alpha_ldq(code, alpha_r3, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r3]));
  alpha_ldq(code, alpha_r4, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r4]));
  alpha_ldq(code, alpha_r5, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r5]));
  alpha_ldq(code, alpha_r6, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r6]));
  alpha_ldq(code, alpha_r7, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r7]));
  alpha_ldq(code, alpha_r8, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r8]));
  alpha_ldq(code, alpha_r9, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r9]));
  alpha_ldq(code, alpha_r10, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r10]));
  alpha_ldq(code, alpha_r11, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r11]));
  alpha_ldq(code, alpha_r12, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r12]));
  alpha_ldq(code, alpha_r13, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r13]));
  alpha_ldq(code, alpha_r14, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r14]));
  alpha_ldq(code, alpha_r15, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r15]));
  alpha_ldq(code, alpha_r22, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r22]));
  alpha_ldq(code, alpha_r23, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r23]));
  alpha_ldq(code, alpha_r24, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r24]));
  alpha_ldq(code, alpha_r25, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r25]));
  alpha_ldq(code, alpha_r26, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r26]));
  alpha_ldq(code, alpha_r27, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r27]));
  alpha_ldq(code, alpha_r28, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r28]));
  alpha_ldq(code, alpha_r29, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r29]));
  alpha_ldq(code, alpha_r30, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_regs[alpha_r30]));

  alpha_ldq(code, alpha_ra, alpha_a0,
            G_STRUCT_OFFSET(MonoContext, uc_mcontext.sc_pc));

  alpha_ret(code, alpha_ra, 1);

  inited = TRUE;

  return start_code;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_ip_from_context                         */
/*                                                                  */
/* Function     - Return the instruction pointer from the context.  */
/*                                                                  */
/* Parameters   - sigctx    - Saved processor state                 */
/*                                                                  */
/*------------------------------------------------------------------*/

gpointer
mono_arch_ip_from_context (void *sigctx)
{
  gpointer ip;
  ALPHA_DEBUG("mono_arch_ip_from_context");

  ip = (gpointer) MONO_CONTEXT_GET_IP(((MonoContext *) sigctx));

  printf("ip_from_context = %p\n", ip);

  return ip;
}


/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - arch_get_rethrow_exception                        */
/*                                                                  */
/* Function     - Return a function pointer which can be used to    */
/*                raise exceptions. The returned function has the   */
/*                following signature:                              */
/*                void (*func) (MonoException *exc);                */
/*                                                                  */
/*------------------------------------------------------------------*/

gpointer
mono_arch_get_rethrow_exception (void)
{
  static guint8 *start;
  static int inited = 0;

  ALPHA_DEBUG("mono_arch_get_rethrow_exception");

  if (inited)
    return start;

  start = get_throw_trampoline (TRUE);

  inited = 1;

  return start;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - arch_get_throw_exception_by_name                  */
/*                                                                  */
/* Function     - Return a function pointer which can be used to    */
/*                raise corlib exceptions. The return function has  */
/*                the following signature:                          */
/*                void (*func) (char *exc_name);                    */
/*                                                                  */
/*------------------------------------------------------------------*/

gpointer
mono_arch_get_throw_exception_by_name (void)
{
  static guint8 *start;
  static int inited = 0;
  unsigned int *code;
  
  if (inited)
    return start;

  start = mono_global_codeman_reserve (SZ_THROW);
  //        get_throw_exception_generic (start, SZ_THROW, TRUE, FALSE);
  inited = 1;

  code = (unsigned int *)start;

  alpha_call_pal(code, 0x80);

  return start;
}
/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_find_jit_info                           */
/*                                                                  */
/* Function     - This function is used to gather informatoin from  */
/*                @ctx. It returns the MonoJitInfo of the corres-   */
/*                ponding function, unwinds one stack frame and     */
/*                stores the resulting context into @new_ctx. It    */
/*                also stores a string describing the stack location*/
/*                into @trace (if not NULL), and modifies the @lmf  */
/*                if necessary. @native_offset returns the IP off-  */
/*                set from the start of the function or -1 if that  */
/*                information is not available.                     */
/*                                                                  */
/*------------------------------------------------------------------*/

MonoJitInfo *
mono_arch_find_jit_info (MonoDomain *domain, MonoJitTlsData *jit_tls,
                         MonoJitInfo *res, MonoJitInfo *prev_ji,
						 MonoContext *ctx,
                         MonoContext *new_ctx, MonoLMF **lmf,
						 mgreg_t **save_locations,
                         gboolean *managed)
{
  MonoJitInfo *ji;
  int i;
  gpointer ip = MONO_CONTEXT_GET_IP (ctx);

  ALPHA_DEBUG("mono_arch_find_jit_info");

  /* Avoid costly table lookup during stack overflow */
  if (prev_ji &&
      (ip > prev_ji->code_start &&
       ((guint8*)ip < ((guint8*)prev_ji->code_start) + prev_ji->code_size)))
    ji = prev_ji;
  else
	  ji = mini_jit_info_table_find (domain, ip, NULL);

  if (managed)
    *managed = FALSE;

  if (ji != NULL)
    {
      int offset;
      gboolean omit_fp = 0; //(ji->used_regs & (1 << 31)) > 0;

      *new_ctx = *ctx;

      if (managed)
	if (!ji->method->wrapper_type)
	  *managed = TRUE;

      /*
       * Some managed methods like pinvoke wrappers might have save_lmf set.
       * In this case, register save/restore code is not generated by the
       * JIT, so we have to restore callee saved registers from the lmf.
       */

      if (ji->method->save_lmf)
	{
	  /*
	   * We only need to do this if the exception was raised in managed
	   * code, since otherwise the lmf was already popped of the stack.
	   */
	  if (*lmf && ((*lmf) != jit_tls->first_lmf) &&
	      (MONO_CONTEXT_GET_SP (ctx) >= (gpointer)(*lmf)->rsp))
	    {
	      new_ctx->uc_mcontext.sc_regs[alpha_fp] = (*lmf)->ebp;
	      new_ctx->uc_mcontext.sc_regs[alpha_sp] = (*lmf)->rsp;
	      new_ctx->uc_mcontext.sc_regs[alpha_gp] = (*lmf)->rgp;

	      /*
	      new_ctx->rbp = (*lmf)->ebp;
	      new_ctx->rbx = (*lmf)->rbx;
	      new_ctx->rsp = (*lmf)->rsp;
	      new_ctx->r12 = (*lmf)->r12;
	      new_ctx->r13 = (*lmf)->r13;
	      new_ctx->r14 = (*lmf)->r14;
	      new_ctx->r15 = (*lmf)->r15;
	      */
	    }
	}
      else
	{
	  offset = omit_fp ? 0 : 2;

	  /* restore caller saved registers */
	  for (i = 0; i < MONO_MAX_IREGS; i++)
	    if (ALPHA_IS_CALLEE_SAVED_REG(i) &&
		(ji->used_regs & (1 << i)))
	      {

		guint64 reg;
#if 0
		if (omit_fp)
		  {
		    reg = *((guint64*)ctx->rsp + offset);
		    offset++;
		  }
		else
		  {
		    //reg = *((guint64 *)ctx->SC_EBP + offset);
		    //offset--;
		  }

		switch (i)
		  {
		  case AMD64_RBX:
		    new_ctx->rbx = reg;
		    break;
		  case AMD64_R12:
		    new_ctx->r12 = reg;
		    break;
		  case AMD64_R13:
		    new_ctx->r13 = reg;
		    break;
		  case AMD64_R14:
		    new_ctx->r14 = reg;
		    break;
		  case AMD64_R15:
		    new_ctx->r15 = reg;
		    break;
		  case AMD64_RBP:
		    new_ctx->rbp = reg;
		    break;
		  default:
		    g_assert_not_reached ();
		  }
#endif
	      }
	}

      if (*lmf && ((*lmf) != jit_tls->first_lmf) &&
	  (MONO_CONTEXT_GET_SP (ctx) >= (gpointer)(*lmf)->rsp)) {
	/* remove any unused lmf */
	*lmf = (*lmf)->previous_lmf;
      }

#if 0
      if (omit_fp)
	{
	  /* Pop frame */
	  new_ctx->rsp += (ji->used_regs >> 16) & (0x7fff);
	  new_ctx->SC_EIP = *((guint64 *)new_ctx->rsp) - 1;
	  /* Pop return address */
	  new_ctx->rsp += 8;
	}
      else
#endif
	{

	  /* Pop FP and the RA */
	  /* Some how we should find size of frame. One way:
	   read 3rd instruction (alpha_lda(alpha_sp, alpha_sp, -stack_size ))
	   and extract "stack_size" from there
	   read 4th and 5th insts to get offsets to saved RA & FP
	  */
	  unsigned int *code = (unsigned int *)ji->code_start;
	  short stack_size = -((short)(code[2] & 0xFFFF));
	  short ra_off = code[3] & 0xFFFF;
	  short fp_off = code[4] & 0xFFFF;

	  /* Restore stack - value of FP reg + stack_size */
	  new_ctx->uc_mcontext.sc_regs[alpha_sp] =
	    ctx->uc_mcontext.sc_regs[alpha_r15] + stack_size;

	  /* we substract 1, so that the IP points into the call instruction */
	  /* restore PC - @FP + 0 */
	  new_ctx->uc_mcontext.sc_pc = 
	    *((guint64 *)(ctx->uc_mcontext.sc_regs[alpha_r15] + ra_off));
	  
	  /* Restore FP reg - @FP + 8 */
	  new_ctx->uc_mcontext.sc_regs[alpha_r15] = 
	    *((guint64 *)(ctx->uc_mcontext.sc_regs[alpha_r15] + fp_off));

	  /* Restore GP - read two insts that restore GP from sc_pc and */
	  /* do the same. Use sc_pc as RA */
	  code = (unsigned int *)new_ctx->uc_mcontext.sc_pc;
	  if ((code[0] & 0xFFFF0000) == 0x27ba0000 &&   // ldah    gp,high_off(ra)
	      (code[1] & 0xFFFF0000) == 0x23bd0000)     // lda     gp,low_off(gp)
	    {
	      short high_off = (short)(code[0] & 0xFFFF);
	      short low_off = (short)(code[1] & 0xFFFF);

	      long rgp = new_ctx->uc_mcontext.sc_pc +
		(65536 * high_off) + low_off;

	      new_ctx->uc_mcontext.sc_regs[alpha_gp] = rgp;
	    }
	}

#if 0
      /* Pop arguments off the stack */
      // No poping args off stack on Alpha
      // We use fixed place
      {
	MonoJitArgumentInfo *arg_info =
	  g_newa (MonoJitArgumentInfo,
		  mono_method_signature (ji->method)->param_count + 1);

	guint32 stack_to_pop =
	  mono_arch_get_argument_info (mono_method_signature (ji->method),
				       mono_method_signature (ji->method)->param_count,
				       arg_info);
	new_ctx->uc_mcontext.sc_regs[alpha_sp] += stack_to_pop;
      }
#endif
      return ji;
    }
  else if (*lmf)
    {
      // Unwind based on LMF info
      if (!(*lmf)->method)
	return (gpointer)-1;

      if ((ji = mini_jit_info_table_find (domain, (gpointer)(*lmf)->eip, NULL))) {
      } else {
	memset (res, 0, MONO_SIZEOF_JIT_INFO);
	res->method = (*lmf)->method;
      }

      new_ctx->uc_mcontext.sc_regs[alpha_fp] = (*lmf)->ebp;
      new_ctx->uc_mcontext.sc_regs[alpha_sp] = (*lmf)->rsp;
      new_ctx->uc_mcontext.sc_regs[alpha_gp] = (*lmf)->rgp;
      new_ctx->uc_mcontext.sc_pc = (*lmf)->eip;

      *lmf = (*lmf)->previous_lmf;

      return ji ? ji : res;
    }

  return NULL;
}

/*========================= End of Function ========================*/


