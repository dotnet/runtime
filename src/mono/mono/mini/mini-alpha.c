/*------------------------------------------------------------------*/
/*                                                                  */
/* Name        - mini-alpha.c                                       */
/*                                                                  */
/* Function    - Alpha backend for the Mono code generator.         */
/*                                                                  */
/* Name        - Sergey Tikhonov (tsv@solvo.ru)                     */
/*                                                                  */
/* Date        - January, 2006                                      */
/*                                                                  */
/* Derivation  - From mini-am64 & mini-ia64 & mini-s390 by -        */
/*               Paolo Molaro (lupus@ximian.com)                    */
/*               Dietmar Maurer (dietmar@ximian.com)                */
/*               Neale Ferguson (Neale.Ferguson@SoftwareAG-usa.com) */
/*                                                                  */
/*------------------------------------------------------------------*/

/*------------------------------------------------------------------*/
/*                 D e f i n e s                                    */
/*------------------------------------------------------------------*/
#define ALPHA_DEBUG(x) \
   if (mini_alpha_verbose_level) \
   	g_debug ("ALPHA_DEBUG: %s is called.", x);

#define ALPHA_PRINT if (mini_alpha_verbose_level)

#define NEW_INS(cfg,dest,op) do {       \
   (dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));       \
   (dest)->opcode = (op);  \
   insert_after_ins (bb, last_ins, (dest)); \
} while (0)

#undef DEBUG
#define DEBUG(a) if (cfg->verbose_level > 1) a

#define CFG_DEBUG(LVL) if (cfg->verbose_level > LVL)

//#define ALPHA_IS_CALLEE_SAVED_REG(reg) (MONO_ARCH_CALLEE_SAVED_REGS & (1 << (reg)))
#define ALPHA_ARGS_REGS ((regmask_t)0x03F0000)
#define ARGS_OFFSET 16

#define ALIGN_TO(val,align) ((((guint64)val) + ((align) - 1)) & ~((align) - 1))
#define alpha_is_imm(X) ((X >= 0 && X <= 255))
#define ALPHA_LOAD_GP(IP) { AlphaGotData ge_data;  add_got_entry(cfg, GT_LD_GTADDR, ge_data, IP, MONO_PATCH_INFO_NONE, 0); }

/*========================= End of Defines =========================*/

/*------------------------------------------------------------------*/
/*                 I n c l u d e s                                  */
/*------------------------------------------------------------------*/

#include "mini.h"
#include <string.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/profiler-private.h>
#include <mono/utils/mono-math.h>

#include "trace.h"
#include "mini-alpha.h"
#include "cpu-alpha.h"
#include "jit-icalls.h"

/*========================= End of Includes ========================*/

/*------------------------------------------------------------------*/
/*                 G l o b a l   V a r i a b l e s                  */
/*------------------------------------------------------------------*/
static int indent_level = 0;

int mini_alpha_verbose_level = 0;
static int bwx_supported = 0;

static int appdomain_tls_offset = -1,
  lmf_tls_offset = -1,
  thread_tls_offset = -1;

pthread_key_t lmf_addr_key;

gboolean lmf_addr_key_inited = FALSE;

MonoBreakpointInfo
mono_breakpoint_info [MONO_BREAKPOINT_ARRAY_SIZE];

/*====================== End of Global Variables ===================*/

gpointer mono_arch_get_lmf_addr (void);

typedef enum {
        ArgInIReg,
        ArgInFloatReg,
        ArgInDoubleReg,
        ArgOnStack,
	ArgValuetypeInReg, // ??
	ArgAggregate,
        ArgNone
} ArgStorage;


typedef struct {
  gint16 offset;
  gint8  reg;
  ArgStorage storage;

  /* Only if storage == ArgAggregate */
  int nregs, nslots;
  //AggregateType atype; // So far use only AggregateNormal
} ArgInfo;

typedef struct {
   int nargs;
   guint32 stack_usage;
//        guint32 struct_ret; /// ???

   guint32 reg_usage;
   guint32 freg_usage;
   gboolean need_stack_align;

   ArgInfo ret;
   ArgInfo sig_cookie;
   ArgInfo args [1];
} CallInfo;

static CallInfo* get_call_info (MonoGenericSharingContext *gsctx, MonoMethodSignature *sig, gboolean is_pinvoke);
static unsigned int *emit_call(MonoCompile *cfg, unsigned int *code,
			       guint32 patch_type, gconstpointer data);

#define PARAM_REGS 6
static int param_regs [] =
{ 
  alpha_a0, alpha_a1,
  alpha_a2, alpha_a3,
  alpha_a4, alpha_a5
};

//static AMD64_Reg_No return_regs [] = { AMD64_RAX, AMD64_RDX };

static void inline
add_general (guint32 *gr, guint32 *stack_size, ArgInfo *ainfo)
{
   ainfo->offset = *stack_size;

   if (*gr >= PARAM_REGS)
     {
       ainfo->storage = ArgOnStack;
       (*stack_size) += sizeof (gpointer);
     }
   else
     {
       ainfo->storage = ArgInIReg;
       ainfo->reg = param_regs [*gr];
       (*gr) ++;
     }
}

#define FLOAT_PARAM_REGS 6
static int fparam_regs [] = { alpha_fa0, alpha_fa1, alpha_fa2, alpha_fa3,
			     alpha_fa4, alpha_fa5 };

static void inline
add_float (guint32 *gr, guint32 *stack_size, ArgInfo *ainfo,
	   gboolean is_double)
{
   ainfo->offset = *stack_size;
   
   if (*gr >= FLOAT_PARAM_REGS) 
   {
     ainfo->storage = ArgOnStack;
     (*stack_size) += sizeof (gpointer);
   }
   else
   {
     /* A double register */
     if (is_double)
       ainfo->storage = ArgInDoubleReg;
     else
       ainfo->storage = ArgInFloatReg;

     ainfo->reg = fparam_regs [*gr];
     (*gr) += 1;
   }
}

static void
add_valuetype (MonoGenericSharingContext *gsctx, MonoMethodSignature *sig, ArgInfo *ainfo, MonoType *type,
               gboolean is_return,
               guint32 *gr, guint32 *fr, guint32 *stack_size)
{
  guint32 size;
  MonoClass *klass;
  MonoMarshalType *info;
  //gboolean is_hfa = TRUE;
  //guint32 hfa_type = 0;

  klass = mono_class_from_mono_type (type);
  if (type->type == MONO_TYPE_TYPEDBYREF)
    size = 3 * sizeof (gpointer);
  else if (sig->pinvoke)
    size = mono_type_native_stack_size (&klass->byval_arg, NULL);
  else
    size = mini_type_stack_size (gsctx, &klass->byval_arg, NULL);

  if (!sig->pinvoke || (size == 0) || is_return) {
    /* Allways pass in memory */
    ainfo->offset = *stack_size;
    *stack_size += ALIGN_TO (size, 8);
    ainfo->storage = ArgOnStack;

    return;
  }

  info = mono_marshal_load_type_info (klass);
  g_assert (info);

  ainfo->storage = ArgAggregate;
  //ainfo->atype = AggregateNormal;

#if 0
  /* This also handles returning of TypedByRef used by some icalls */
  if (is_return) {
    if (size <= 32) {
      ainfo->reg = IA64_R8;
      ainfo->nregs = (size + 7) / 8;
      ainfo->nslots = ainfo->nregs;
      return;
    }
    NOT_IMPLEMENTED;
  }
#endif

  ainfo->reg =  param_regs [*gr];
  ainfo->offset = *stack_size;
  ainfo->nslots = (size + 7) / 8;

  if (((*gr) + ainfo->nslots) <= 6) {
    /* Fits entirely in registers */
    ainfo->nregs = ainfo->nslots;
    (*gr) += ainfo->nregs;
    return;
  }

  ainfo->nregs = 6 - (*gr);
  (*gr) = 6;
  (*stack_size) += (ainfo->nslots - ainfo->nregs) * 8;

}

// This function is called from mono_arch_call_opcode and
// should determine which registers will be used to do the call
// For Alpha we could calculate number of parameter used for each
// call and allocate space in stack only for whose "a0-a5" registers
// that will be used in calls
static void
add_outarg_reg (MonoCompile *cfg, MonoCallInst *call, MonoInst *arg,
		ArgStorage storage, int reg, MonoInst *tree)
{
  switch (storage)
    {
    case ArgInIReg:
      arg->opcode = OP_OUTARG_REG;
      arg->inst_left = tree;
      arg->inst_right = (MonoInst*)call;
      arg->backend.reg3 = reg;
      call->used_iregs |= 1 << reg;
      break;
    case ArgInFloatReg:
      arg->opcode = OP_OUTARG_FREG;
      arg->inst_left = tree;
      arg->inst_right = (MonoInst*)call;
      arg->backend.reg3 = reg;
      call->used_fregs |= 1 << reg;
      break;
    case ArgInDoubleReg:
      arg->opcode = OP_OUTARG_FREG;
      arg->inst_left = tree;
      arg->inst_right = (MonoInst*)call;
      arg->backend.reg3 = reg;
      call->used_fregs |= 1 << reg;
      break;
    default:
      g_assert_not_reached ();
    }
}

static void
insert_after_ins (MonoBasicBlock *bb, MonoInst *ins, MonoInst *to_insert)
{
   if (ins == NULL)
     {
       ins = bb->code;
       bb->code = to_insert;
       to_insert->next = ins;
     }
   else
     {
       to_insert->next = ins->next;
       ins->next = to_insert;
     }
}

static void add_got_entry(MonoCompile *cfg, AlphaGotType ge_type,
			  AlphaGotData ge_data,
			  int ip, MonoJumpInfoType type, gconstpointer target)
{
  AlphaGotEntry *AGE = mono_mempool_alloc (cfg->mempool,
					   sizeof (AlphaGotEntry));

  AGE->type = ge_type;

  switch(ge_type)
    {
    case GT_INT:
      AGE->value.data.i = ge_data.data.i;
      break;
    case GT_LONG:
      AGE->value.data.l = ge_data.data.l;
      break;
    case GT_PTR:
      AGE->value.data.p = ge_data.data.p;
      break;
    case GT_FLOAT:
      AGE->value.data.f = ge_data.data.f;
      break;
    case GT_DOUBLE:
      AGE->value.data.d = ge_data.data.d;
      break;
    case GT_LD_GTADDR:
      AGE->value.data.l = ip;
      break;
    default:
      AGE->type = GT_NONE;
    }
  
  if (type != MONO_PATCH_INFO_NONE)
    {
      mono_add_patch_info(cfg, ip, type, target);
      AGE->patch_info = cfg->patch_info;
    }
  else
    AGE->patch_info = 0;

  if (AGE->type != GT_LD_GTADDR)
  {
      mono_add_patch_info(cfg, ip, MONO_PATCH_INFO_GOT_OFFSET, 0);
      AGE->got_patch_info = cfg->patch_info;
  }

  AGE->next = cfg->arch.got_data;

  cfg->arch.got_data = AGE;
}

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_create_vars                             */
/*                                                                  */
/* Function     -                                                   */
/*                                                                  */
/* Returns      - 
 * 
 * Params:
 *  cfg - pointer to compile unit
 *
 * TSV (guess)
 * This method is called right before starting converting compiled
 * method to IR. I guess we could find out how many arguments we
 * should expect, what type and what return value would be.
 * After that we could correct "cfg" structure, or "arch" part of
 * that structure.
 */
/*                                                                  */
/*------------------------------------------------------------------*/

void
mono_arch_create_vars (MonoCompile *cfg)
{   
  MonoMethodSignature *sig;
  CallInfo *cinfo;

  CFG_DEBUG(2) ALPHA_DEBUG("mono_arch_create_vars");
   
  sig = mono_method_signature (cfg->method);
   
  cinfo = get_call_info (cfg->generic_sharing_context, sig, FALSE);
   
  if (cinfo->ret.storage == ArgValuetypeInReg)
    cfg->ret_var_is_local = TRUE;
  
  g_free (cinfo);
}


/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_get_lmf_addr                            */
/*                                                                  */
/* Function     -                                                   */
/*                                                                  */
/* Returns      -                                                   */
/*                                                                  */
/*------------------------------------------------------------------*/

gpointer
mono_arch_get_lmf_addr (void)
{
  ALPHA_DEBUG("mono_arch_get_lmf_addr");
   
  return pthread_getspecific (lmf_addr_key);
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_free_jit_tls_data                       */
/*                                                                  */
/* Function     - Free tls data.                                    */
/*                                                                  */
/*------------------------------------------------------------------*/

void
mono_arch_free_jit_tls_data (MonoJitTlsData *tls)
{
  ALPHA_DEBUG("mono_arch_free_jit_tls_data");
}

/*========================= End of Function ========================*/

// This peephole function is called before "local_regalloc" method
// TSV_TODO - Check what we need to move here
void
mono_arch_peephole_pass_1 (MonoCompile *cfg, MonoBasicBlock *bb)
{
  CFG_DEBUG(3) g_print ("ALPHA: PEEPHOLE_1 pass\n");
}

// This peephole function is called after "local_regalloc" method
void
mono_arch_peephole_pass_2 (MonoCompile *cfg, MonoBasicBlock *bb)
{
  MonoInst *ins, *n, *last_ins = NULL;
  ins = bb->code;
   
  CFG_DEBUG(3) g_print ("ALPHA: PEEPHOLE_2 pass\n");

  MONO_BB_FOR_EACH_INS_SAFE (bb, n, ins) {
      switch (ins->opcode) 
	{	 
	case OP_MOVE:
	case OP_FMOVE:
	  /*
	   * Removes:
	   *
	   * OP_MOVE reg, reg except special case (mov at, at)
	   */
	  if (ins->dreg == ins->sreg1 &&
	      ins->dreg != alpha_at) 
	    {
	      MONO_DELETE_INS (bb, ins);
	      continue;
	    }
	  
	  /*
	   * Removes:
	   *
	   * OP_MOVE sreg, dreg
	   * OP_MOVE dreg, sreg
	   */
	  if (last_ins && last_ins->opcode == OP_MOVE &&
	      ins->sreg1 == last_ins->dreg &&
	      last_ins->dreg != alpha_at &&
	      ins->dreg == last_ins->sreg1) 
	    {
	      MONO_DELETE_INS (bb, ins);
	      continue;
	    }
	  
	  break;
	case OP_MUL_IMM:
	case OP_IMUL_IMM:
	  /* remove unnecessary multiplication with 1 */
	  if (ins->inst_imm == 1) 
	    {
	      if (ins->dreg != ins->sreg1) 
		{
		  ins->opcode = OP_MOVE;
		}
	      else 
		{
		  MONO_DELETE_INS (bb, ins);
		  continue;
		}
	    }
	  
	  break;

	case OP_LOADI8_MEMBASE:
	case OP_LOAD_MEMBASE:
          /*
           * Note: if reg1 = reg2 the load op is removed
           *
           * OP_STOREI8_MEMBASE_REG reg1, offset(basereg)
           * OP_LOADI8_MEMBASE offset(basereg), reg2
           * -->
           * OP_STOREI8_MEMBASE_REG reg1, offset(basereg)
           * OP_MOVE reg1, reg2
           */
          if (last_ins &&
	      (last_ins->opcode == OP_STOREI8_MEMBASE_REG ||
		last_ins->opcode == OP_STORE_MEMBASE_REG) &&
              ins->inst_basereg == last_ins->inst_destbasereg &&
              ins->inst_offset == last_ins->inst_offset)
            {
              if (ins->dreg == last_ins->sreg1)
                {
		  MONO_DELETE_INS (bb, ins);
                  continue;
                }
              else
                {
                  //static int c = 0; printf ("MATCHX %s %d\n", cfg->method->name,c++);
                  ins->opcode = OP_MOVE;
                  ins->sreg1 = last_ins->sreg1;
                }
            }
	  break;

#if 0
       	case OP_LOAD_MEMBASE:
	case OP_LOADI4_MEMBASE:
	  /*
	   * Note: if reg1 = reg2 the load op is removed
	   *
	   * OP_STORE_MEMBASE_REG reg1, offset(basereg)
	   * OP_LOAD_MEMBASE offset(basereg), reg2
	   * -->
	   * OP_STORE_MEMBASE_REG reg1, offset(basereg)
	   * OP_MOVE reg1, reg2
	   */
	  if (last_ins && (last_ins->opcode == OP_STOREI4_MEMBASE_REG
			   /*|| last_ins->opcode == OP_STORE_MEMBASE_REG*/) &&
	      ins->inst_basereg == last_ins->inst_destbasereg &&
	      ins->inst_offset == last_ins->inst_offset)
	    {
	      if (ins->dreg == last_ins->sreg1)
		{
		  MONO_DELETE_INS (bb, ins);
		  continue;
		}
	      else
		{
		  //static int c = 0; printf ("MATCHX %s %d\n", cfg->method->name,c++);
		  ins->opcode = OP_MOVE;
		  ins->sreg1 = last_ins->sreg1;
		}
	    }
	  /*
	   * Note: reg1 must be different from the basereg in the second load
	   * Note: if reg1 = reg2 is equal then second load is removed
	   *
	   * OP_LOAD_MEMBASE offset(basereg), reg1
	   * OP_LOAD_MEMBASE offset(basereg), reg2
	   * -->
	   * OP_LOAD_MEMBASE offset(basereg), reg1
	   * OP_MOVE reg1, reg2
	   */

	  if (last_ins && (last_ins->opcode == OP_LOADI4_MEMBASE
			   || last_ins->opcode == OP_LOAD_MEMBASE) &&
	      ins->inst_basereg != last_ins->dreg &&
	      ins->inst_basereg == last_ins->inst_basereg &&
	      ins->inst_offset == last_ins->inst_offset)
	    {
	      if (ins->dreg == last_ins->dreg)
		{
		  MONO_DELETE_INS (bb, ins);
		  continue;
		}
	      else
		{
		  ins->opcode = OP_MOVE;
		  ins->sreg1 = last_ins->dreg;
		}

	      //g_assert_not_reached ();
	    }
	  break;      
#endif
	}
      
      last_ins = ins;
      ins = ins->next;
    }
   
  bb->last_ins = last_ins;
}

// Convert to opposite branch opcode
static guint16 cvt_branch_opcode(guint16 opcode)
{
  switch (opcode)
    {
    case CEE_BEQ:
      ALPHA_PRINT g_debug("ALPHA: Branch cvt: CEE_BEQ -> CEE_BNE_UN\n");
      return CEE_BNE_UN;


      //case CEE_CGT_UN:
      //printf("ALPHA: Branch cvt: CEE_CGT_UN -> OP_IBEQ\n");
      //return OP_IBEQ;

      //case OP_LCGT_UN:
      //printf("ALPHA: Branch cvt: OP_LCGT_UN -> OP_IBEQ\n");
      //return OP_IBEQ;

      //    case OP_CGT_UN:
      //printf("ALPHA: Branch cvt: OP_CGT_UN -> OP_IBEQ\n");
      //return OP_IBEQ;

    case OP_IBEQ:
      ALPHA_PRINT g_debug("ALPHA: Branch cvt: OP_IBEQ -> OP_IBNE_UN\n");
      return OP_IBNE_UN;

    case OP_FBEQ:
      ALPHA_PRINT g_debug("ALPHA: Branch cvt: OP_FBEQ -> OP_FBNE_UN\n");
      return OP_FBNE_UN;

    case OP_FBNE_UN:
      ALPHA_PRINT g_debug("ALPHA: Branch cvt: OP_FBNE_UN -> OP_FBEQ\n");
      return OP_FBEQ;
		
    case OP_IBNE_UN:
      ALPHA_PRINT g_debug("ALPHA: Branch cvt: OP_IBNE_UN -> OP_IBEQ\n");
      return OP_IBEQ;
		
    case CEE_BNE_UN:
      ALPHA_PRINT g_debug("ALPHA: Branch cvt: CEE_BNE_UN -> OP_IBEQ\n");
      return OP_IBEQ;

    case OP_IBLE:
      ALPHA_PRINT g_debug("ALPHA: Branch cvt: OP_IBLE -> OP_IBNE_UN\n");
      return OP_IBNE_UN;

    case OP_IBLE_UN:
      ALPHA_PRINT g_debug("ALPHA: Branch cvt: OP_IBLE_UN -> OP_IBNE_UN\n");
      return OP_IBNE_UN;

    case CEE_BLE:
      ALPHA_PRINT g_debug("ALPHA: Branch cvt: CEE_BLE -> OP_IBNE_UN\n");
      return OP_IBNE_UN;

    case CEE_BLE_UN:
      ALPHA_PRINT g_debug("ALPHA: Branch cvt: CEE_BLE_UN -> OP_IBNE_UN\n");
      return OP_IBNE_UN;

    case OP_IBLT:
      ALPHA_PRINT g_debug("ALPHA: Branch cvt: OP_IBLT -> OP_IBNE_UN\n");
      return OP_IBNE_UN;

    case CEE_BLT:
      ALPHA_PRINT g_debug("ALPHA: Branch cvt: CEE_BLT -> OP_IBNE_UN\n");
      return OP_IBNE_UN;

    case CEE_BLT_UN:
      ALPHA_PRINT g_debug("ALPHA: Branch cvt: CEE_BLT_UN -> OP_IBNE_UN\n");
      return OP_IBNE_UN;

    case OP_IBLT_UN:
      ALPHA_PRINT g_debug("ALPHA: Branch cvt: OP_IBLT_UN -> OP_IBNE_UN\n");
      return OP_IBNE_UN;

    case OP_IBGE:
      ALPHA_PRINT g_debug("ALPHA: Branch cvt: OP_IBGE -> OP_IBEQ\n");
      return OP_IBEQ;

    case CEE_BGE:
      ALPHA_PRINT g_debug("ALPHA: Branch cvt: CEE_BGE -> OP_IBEQ\n");
      return OP_IBEQ;

    case CEE_BGT:
      ALPHA_PRINT g_debug("ALPHA: Branch cvt: CEE_BGT -> OP_IBEQ\n");
      return OP_IBEQ;

    case OP_IBGT:
      ALPHA_PRINT g_debug("ALPHA: Branch cvt: OP_IBGT -> OP_IBEQ\n");
      return OP_IBEQ;

    case CEE_BGT_UN:
      ALPHA_PRINT g_debug("ALPHA: Branch cvt: CEE_BGT_UN -> OP_IBEQ\n");
      return OP_IBEQ;

    case OP_IBGT_UN:
      ALPHA_PRINT g_debug("ALPHA: Branch cvt: OP_IBGT_UN -> OP_IBEQ\n");
      return OP_IBEQ;

    case CEE_BGE_UN:
      ALPHA_PRINT g_debug("ALPHA: Branch cvt: CEE_BGE_UN -> OP_IBEQ\n");
      return OP_IBEQ;

    case OP_IBGE_UN:
      ALPHA_PRINT g_debug("ALPHA: Branch cvt: OP_IBGE_UN -> OP_IBEQ\n");
      return OP_IBEQ;

    default:
      break;
    }

  ALPHA_PRINT g_debug("ALPHA: WARNING: No Branch cvt for: %d\n", opcode);
   
  return opcode;
}

typedef enum { EQ, ULE, LE, LT, ULT } ALPHA_CMP_OPS;

static guint16 cvt_cmp_opcode(guint16 opcode, ALPHA_CMP_OPS cond)
{
  guint16 ret_opcode;

  switch (opcode)
    {
      /* Use inssel-alpha.brg to handle cmp+b<cond> -> cmp<cond>+b<cond> cvt */
    case OP_FCOMPARE:
      break;

    case OP_COMPARE:
    case OP_ICOMPARE:
    case OP_LCOMPARE:
      {
	switch(cond)
	  {
	  case EQ:
	    return OP_ALPHA_CMP_EQ;
	  case ULE:
	    return OP_ALPHA_CMP_ULE;
	  case LE:
	    return OP_ALPHA_CMP_LE;
	  case LT:
	    return OP_ALPHA_CMP_LT;
	  case ULT:
	    return OP_ALPHA_CMP_ULT;
	  }
      }
      break;

    case OP_ICOMPARE_IMM:
    case OP_COMPARE_IMM:
      {
	switch(cond)
	  {
	  case EQ:
	    return OP_ALPHA_CMP_IMM_EQ;
          case ULE:
            return OP_ALPHA_CMP_IMM_ULE;
	  case LE:
	    return OP_ALPHA_CMP_IMM_LE;
	  case LT:
	    return OP_ALPHA_CMP_IMM_LT;
	  case ULT:
	    return OP_ALPHA_CMP_IMM_ULT;
	  }
      }
    }

  g_assert_not_reached();

  return ret_opcode;
}

static void cvt_cmp_branch(MonoInst *curr, MonoInst *next)
{
   // Instead of compare+b<cond>,
   // Alpha has compare<cond>+br<cond>
   // we need to convert
   // Handle floating compare here too
   
  switch(next->opcode)
    {
    case CEE_BEQ:
    case OP_IBEQ:
      // Convert cmp + beq -> cmpeq + bne
      curr->opcode = cvt_cmp_opcode(curr->opcode, EQ);
      next->opcode = cvt_branch_opcode(next->opcode);
      break;
		
    case OP_IBNE_UN:
    case CEE_BNE_UN:
      // cmp + ibne_un -> cmpeq + beq
      curr->opcode = cvt_cmp_opcode(curr->opcode, EQ);
      next->opcode = cvt_branch_opcode(next->opcode);
      break;

    case OP_IBLE:
    case CEE_BLE:
      // cmp + ible -> cmple + bne, lcmp + ble -> cmple + bne
      curr->opcode = cvt_cmp_opcode(curr->opcode, LE);
      next->opcode = cvt_branch_opcode(next->opcode);
      break;

    case CEE_BLE_UN:
    case OP_IBLE_UN:
      // cmp + ible_un -> cmpule + bne, lcmp + ble.un -> cmpule + bne
      curr->opcode = cvt_cmp_opcode(curr->opcode, ULE);
      next->opcode = cvt_branch_opcode(next->opcode);
      break;

    case OP_IBLT:
    case CEE_BLT:
      // cmp + iblt -> cmplt + bne, lcmp + blt -> cmplt + bne
      curr->opcode = cvt_cmp_opcode(curr->opcode, LT);
      next->opcode = cvt_branch_opcode(next->opcode);
      break;

    case CEE_BLT_UN:
    case OP_IBLT_UN:
      // lcmp + blt.un -> cmpult + bne
      curr->opcode = cvt_cmp_opcode(curr->opcode, ULT);
      next->opcode = cvt_branch_opcode(next->opcode);
      break;

    case OP_IBGE:
    case CEE_BGE:
      // cmp + ibge -> cmplt + beq, lcmp + bge -> cmplt + beq
      curr->opcode = cvt_cmp_opcode(curr->opcode, LT);
      next->opcode = cvt_branch_opcode(next->opcode);
      break;

    case CEE_BGE_UN:
    case OP_IBGE_UN:
      //lcmp + bge.un -> cmpult + beq
      curr->opcode = cvt_cmp_opcode(curr->opcode, ULT);
      next->opcode = cvt_branch_opcode(next->opcode);
      break;

    case OP_IBGT:
    case CEE_BGT:
      // lcmp + bgt -> cmple + beq, cmp + ibgt -> cmple + beq
      curr->opcode = cvt_cmp_opcode(curr->opcode, LE);
      next->opcode = cvt_branch_opcode(next->opcode);
      break;

    case CEE_BGT_UN:
    case OP_IBGT_UN:
      // lcmp + bgt -> cmpule + beq, cmp + ibgt -> cmpule + beq
      curr->opcode = cvt_cmp_opcode(curr->opcode, ULE);
      next->opcode = cvt_branch_opcode(next->opcode);
      break;

      //    case CEE_CGT_UN:
    case OP_CGT_UN:
    case OP_ICGT_UN:
      // cmp + cgt_un -> cmpule + beq
      curr->opcode = cvt_cmp_opcode(curr->opcode, ULE);
      break;

    case OP_ICEQ:
    case OP_CEQ:
      // cmp + iceq -> cmpeq + bne
      curr->opcode = cvt_cmp_opcode(curr->opcode, EQ);
      break;

    case OP_ICGT:
    case OP_CGT:
      // cmp + int_cgt -> cmple + beq
      curr->opcode = cvt_cmp_opcode(curr->opcode, LE);
      break;

    case OP_ICLT:
    case OP_CLT:
      // cmp + int_clt -> cmplt + bne
      curr->opcode = cvt_cmp_opcode(curr->opcode, LT);
      break;

    case OP_ICLT_UN:
    case OP_CLT_UN:
      // cmp + int_clt_un -> cmpult + bne
      curr->opcode = cvt_cmp_opcode(curr->opcode, ULT);
      break;


    // The conditional exceptions will be handled in
    // output_basic_blocks. Here we just determine correct
    // cmp
    case OP_COND_EXC_GT:
      curr->opcode = cvt_cmp_opcode(curr->opcode, LE);
      break;

    case OP_COND_EXC_GT_UN:
      curr->opcode = cvt_cmp_opcode(curr->opcode, ULE);
      break;

    case OP_COND_EXC_LT:
      curr->opcode = cvt_cmp_opcode(curr->opcode, LT);
      break;

    case OP_COND_EXC_LT_UN:
      curr->opcode = cvt_cmp_opcode(curr->opcode, ULT);
      break;

    case OP_COND_EXC_LE_UN:
      curr->opcode = cvt_cmp_opcode(curr->opcode, ULE);
      break;

    case OP_COND_EXC_NE_UN:
      curr->opcode = cvt_cmp_opcode(curr->opcode, EQ);
      break;

    case OP_COND_EXC_EQ:
      curr->opcode = cvt_cmp_opcode(curr->opcode, EQ);
      break;


    default:
      g_warning("cvt_cmp_branch called with %s(%0X) br opcode",
		mono_inst_name(next->opcode), next->opcode);

      //      g_assert_not_reached();

      break;
    }
}


/*
 * mono_arch_lowering_pass:
 *
 * Converts complex opcodes into simpler ones so that each IR instruction
 * corresponds to one machine instruction.
 */
void
mono_arch_lowering_pass (MonoCompile *cfg, MonoBasicBlock *bb)
{   
   MonoInst *ins, *n, *temp, *last_ins = NULL;
   MonoInst *next;
   
   ins = bb->code;
   
   /*
    * FIXME: Need to add more instructions, but the current machine
    * description can't model some parts of the composite instructions like
    * cdq.
    */
   
   MONO_BB_FOR_EACH_INS_SAFE (bb, n, ins) {
       switch (ins->opcode) 
	 {	 
	 case OP_DIV_IMM:
	 case OP_REM_IMM:
	 case OP_IDIV_IMM:
	 case OP_IREM_IMM:
	 case OP_MUL_IMM:
	   NEW_INS (cfg, temp, OP_I8CONST);
	   temp->inst_c0 = ins->inst_imm;
	   temp->dreg = mono_alloc_ireg (cfg);
	   
	   switch (ins->opcode) 
	     {
	     case OP_MUL_IMM:
	       ins->opcode = CEE_MUL;
	       break;
	     case OP_DIV_IMM:
	       ins->opcode = OP_LDIV;
	       break;
	     case OP_REM_IMM:
	       ins->opcode = OP_LREM;
	       break;
	     case OP_IDIV_IMM:
	       ins->opcode = OP_IDIV;
	       break;
	     case OP_IREM_IMM:
	       ins->opcode = OP_IREM;
	       break;
	     }
			 
	   ins->sreg2 = temp->dreg;
	   break;

	 case OP_COMPARE:
	 case OP_ICOMPARE:
	 case OP_LCOMPARE:
	 case OP_FCOMPARE:
	   {
	     // Instead of compare+b<cond>/fcompare+b<cond>,
	     // Alpha has compare<cond>+br<cond>/fcompare<cond>+br<cond>
	     // we need to convert
	     next = ins->next;

	     cvt_cmp_branch(ins, next);
	   }
	   break;

	 case OP_COMPARE_IMM:
	   if (!alpha_is_imm (ins->inst_imm)) 
	     {	  
	       NEW_INS (cfg, temp, OP_I8CONST);
	       temp->inst_c0 = ins->inst_imm;
	       temp->dreg = mono_alloc_ireg (cfg);
	       ins->opcode = OP_COMPARE;
	       ins->sreg2 = temp->dreg;
				  
	       // We should try to reevaluate new IR opcode
	       //continue;
	     }
	   
	   next = ins->next;
	   
	   cvt_cmp_branch(ins, next);
			 
	   break;

	 case OP_ICOMPARE_IMM:
           if (!alpha_is_imm (ins->inst_imm))
             {
               NEW_INS (cfg, temp, OP_ICONST);
               temp->inst_c0 = ins->inst_imm;
               temp->dreg = mono_alloc_ireg (cfg);
               ins->opcode = OP_ICOMPARE;
               ins->sreg2 = temp->dreg;

               // We should try to reevaluate new IR opcode
               //continue;
             }

           next = ins->next;

           cvt_cmp_branch(ins, next);

           break;

	 case OP_STORE_MEMBASE_IMM:
	 case OP_STOREI8_MEMBASE_IMM:
	   if (ins->inst_imm != 0) 
	     {	  
	       NEW_INS (cfg, temp, OP_I8CONST);
	       temp->inst_c0 = ins->inst_imm;
	       temp->dreg = mono_alloc_ireg (cfg);
	       ins->opcode = OP_STOREI8_MEMBASE_REG;
	       ins->sreg1 = temp->dreg;
	     }
	   break;

	 case OP_STOREI4_MEMBASE_IMM:
	   if (ins->inst_imm != 0)
	     {
	       MonoInst *temp;
	       NEW_INS (cfg, temp, OP_ICONST);
	       temp->inst_c0 = ins->inst_imm;
	       temp->dreg = mono_alloc_ireg (cfg);
	       ins->opcode = OP_STOREI4_MEMBASE_REG;
	       ins->sreg1 = temp->dreg;
	     }
	   break;

         case OP_STOREI1_MEMBASE_IMM:
	   if (ins->inst_imm != 0 || !bwx_supported)
             {
               MonoInst *temp;
               NEW_INS (cfg, temp, OP_ICONST);
               temp->inst_c0 = ins->inst_imm;
               temp->dreg = mono_alloc_ireg (cfg);
               ins->opcode = OP_STOREI1_MEMBASE_REG;
               ins->sreg1 = temp->dreg;
             }
           break;

         case OP_STOREI2_MEMBASE_IMM:
           if (ins->inst_imm != 0 || !bwx_supported)
	   {
	     MonoInst *temp;
	     NEW_INS (cfg, temp, OP_ICONST);
	     temp->inst_c0 = ins->inst_imm;
	     temp->dreg = mono_alloc_ireg (cfg);
	     ins->opcode = OP_STOREI2_MEMBASE_REG;
	     ins->sreg1 = temp->dreg;
	   }
           break;
			 
	 case OP_IADD_IMM:
	 case OP_ISUB_IMM:
	 case OP_IAND_IMM:
	 case OP_IOR_IMM:
	 case OP_IXOR_IMM:
	 case OP_ISHL_IMM:
	 case OP_ISHR_IMM:
	 case OP_ISHR_UN_IMM:
	   if (!alpha_is_imm(ins->inst_imm))
	     {
	       MonoInst *temp;
	       NEW_INS (cfg, temp, OP_ICONST);
	       temp->inst_c0 = ins->inst_imm;
	       temp->dreg = mono_alloc_ireg (cfg);
				  
	       switch(ins->opcode)
		 {
		 case OP_IADD_IMM:
		   ins->opcode = OP_IADD;
		   break;
		 case OP_ISUB_IMM:
		   ins->opcode = OP_ISUB;
		   break;
		 case OP_IAND_IMM:
		   ins->opcode = OP_IAND;
		   break;
		 case OP_IOR_IMM:
                   ins->opcode = OP_IOR;
                   break;
                 case OP_IXOR_IMM:
                   ins->opcode = OP_IXOR;
                   break;
		 case OP_ISHL_IMM:
		   ins->opcode = OP_ISHL;
		   break;
                 case OP_ISHR_IMM:
                   ins->opcode = OP_ISHR;
                   break;
		 case OP_ISHR_UN_IMM:
		   ins->opcode = OP_ISHR_UN;

		 default:
		   break;
		 }
	       
	       ins->sreg2 = temp->dreg;
	     }
	   break;
	 case OP_ADD_IMM:
	 case OP_AND_IMM:
	 case OP_SHL_IMM:
	   if (!alpha_is_imm(ins->inst_imm))
	     {
	       MonoInst *temp;
	       NEW_INS (cfg, temp, OP_ICONST);
	       temp->inst_c0 = ins->inst_imm;
	       temp->dreg = mono_alloc_ireg (cfg);
	       
	       switch(ins->opcode)
		 {
		 case OP_ADD_IMM:
		   ins->opcode = CEE_ADD;
		   break;
		 case OP_ISUB_IMM:
		   ins->opcode = CEE_SUB;
		   break;
		 case OP_AND_IMM:
		   ins->opcode = CEE_AND;
		   break;
		 case OP_SHL_IMM:
		   ins->opcode = CEE_SHL;
		   break;
		 default:
		   break;
		 }
	       
	       ins->sreg2 = temp->dreg;
	     }
	   break;
	 case OP_LSHR_IMM:
	   if (!alpha_is_imm(ins->inst_imm))
	     {
	       MonoInst *temp;
	       NEW_INS(cfg, temp, OP_ICONST);
	       temp->inst_c0 = ins->inst_imm;
	       temp->dreg = mono_alloc_ireg (cfg);
	       ins->sreg2 = temp->dreg;
	       ins->opcode = OP_LSHR;
	     }
	   break;
         case OP_LSHL_IMM:
           if (!alpha_is_imm(ins->inst_imm))
             {
               MonoInst *temp;
               NEW_INS(cfg, temp, OP_ICONST);
               temp->inst_c0 = ins->inst_imm;
               temp->dreg = mono_alloc_ireg (cfg);
               ins->sreg2 = temp->dreg;
               ins->opcode = OP_LSHL;
             }
           break;

	 default:
	   break;
	 }
		
       last_ins = ins;
       ins = ins->next;
     }
   
   bb->last_ins = last_ins;
   
   bb->max_vreg = cfg->next_vreg;
}

/*========================= End of Function ========================*/

#define AXP_GENERAL_REGS     6
#define AXP_MIN_STACK_SIZE   24

/* A typical Alpha stack frame looks like this */
/*
fun:                         // called from outside the module.
        ldgp gp,0(pv)        // load the global pointer
fun..ng:                     // called from inside the module.
        lda sp, -SIZE( sp )  // grow the stack downwards.

        stq ra, 0(sp)        // save the return address.

        stq s0, 8(sp)        // callee-saved registers.
        stq s1, 16(sp)       // ...

        // Move the arguments to the argument registers...

        mov addr, pv         // Load the callee address
        jsr  ra, (pv)        // call the method.
        ldgp gp, 0(ra)       // restore gp

        // return value is in v0

        ldq ra, 0(sp)        // free stack frame
        ldq s0, 8(sp)        // restore callee-saved registers.
        ldq s1, 16(sp)
        ldq sp, 32(sp)       // restore stack pointer

        ret zero, (ra), 1    // return.

// min SIZE = 48
// our call must look like this.

call_func:
        ldgp gp, 0(pv)
call_func..ng:
        .prologue
        lda sp, -SIZE(sp)  // grow stack SIZE bytes.
        stq ra, SIZE-48(sp)   // store ra
        stq fp, SIZE-40(sp)   // store fp (frame pointer)
        stq a0, SIZE-32(sp)   // store args. a0 = func
        stq a1, SIZE-24(sp)   // a1 = retval
        stq a2, SIZE-16(sp)   // a2 = this
        stq a3, SIZE-8(sp)    // a3 = args
        mov sp, fp            // set frame pointer
        mov pv, a0            // func

        .calling_arg_this
        mov a1, a2

        .calling_arg_6plus
        ldq t0, POS(a3)
        stq t0, 0(sp)
        ldq t1, POS(a3)
        stq t1, 8(sp)
        ... SIZE-56 ...

        mov zero,a1
        mov zero,a2
        mov zero,a3
        mov zero,a4
        mov zero,a5

        .do_call
        jsr ra, (pv)    // call func
        ldgp gp, 0(ra)  // restore gp.
        mov v0, t1      // move return value into t1

        .do_store_retval
        ldq t0, SIZE-24(fp) // load retval into t2
        stl t1, 0(t0)       // store value.

        .finished
        mov fp,sp
        ldq ra,SIZE-48(sp)
        ldq fp,SIZE-40(sp)
        lda sp,SIZE(sp)
        ret zero,(ra),1
*/

/*
 * emit_load_volatile_arguments:
 *
 *  Load volatile arguments from the stack to the original input registers.
 * Required before a tail call.
 */
static unsigned int*
emit_load_volatile_arguments (MonoCompile *cfg, unsigned int *code)
{
  MonoMethod *method = cfg->method;
  MonoMethodSignature *sig;
  MonoInst *inst;
  CallInfo *cinfo;
  guint32 i;

  /* FIXME: Generate intermediate code instead */

  sig = mono_method_signature (method);

  cinfo = get_call_info (cfg->generic_sharing_context, sig, FALSE);

  if (sig->ret->type != MONO_TYPE_VOID) {
    if ((cinfo->ret.storage == ArgInIReg) &&
	(cfg->ret->opcode != OP_REGVAR))
      {
	alpha_ldq(code, cinfo->ret.reg, cfg->ret->inst_basereg,
		  cfg->ret->inst_offset);
      }
  }

  for (i = 0; i < sig->param_count + sig->hasthis; ++i)
    {
      ArgInfo *ainfo = &cinfo->args [i];
      MonoInst *inst = cfg->args [i];

      switch(ainfo->storage)
	{
	case ArgInIReg:
	  // We need to save all used a0-a5 params
	  //for (i=0; i<PARAM_REGS; i++)
	  //  {
	  //    if (i < cinfo->reg_usage)
	  {
	    //alpha_stq(code, ainfo->reg, alpha_fp, offset);
	    alpha_ldq(code, ainfo->reg, inst->inst_basereg, inst->inst_offset);

	    CFG_DEBUG(3) g_print("ALPHA: Saved int arg reg %d at offset: %0lx\n",
				 ainfo->reg, inst->inst_offset/*offset*/);
	  }
	  //}
	  break;
	case ArgInDoubleReg:
	case ArgInFloatReg:
	  // We need to save all used af0-af5 params
	  //for (i=0; i<PARAM_REGS; i++)
	  //  {
	  //    if (i < cinfo->freg_usage)
	  {
	    switch(cinfo->args[i].storage)
	      {
	      case ArgInFloatReg:
		//alpha_sts(code, ainfo->reg, alpha_fp, offset);
		alpha_lds(code, ainfo->reg, inst->inst_basereg, inst->inst_offset);
		break;
	      case ArgInDoubleReg:
		//alpha_stt(code, ainfo->reg, alpha_fp, offset);
		alpha_ldt(code, ainfo->reg, inst->inst_basereg, inst->inst_offset);
		break;
	      default:
		;
	      }

	    CFG_DEBUG(3) g_print("ALPHA: Saved float arg reg %d at offset: %0lx\n",
				 ainfo->reg, /*offset*/inst->inst_offset);
	  }
	}
    }
  g_free (cinfo);

  return code;
}

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_emit_prolog                             */
/*                                                                  */
/* Function     - Create the instruction sequence for a function    */
/*                prolog.                                           */
/*
 * How to handle consts and method addreses: 
 * For method we will allocate array of qword after method epiloge.
 * These qword will hold readonly info to method to properly to run.
 * For example: qword constants, method addreses
 * GP will point to start of data. Offsets to the data will be equal
 * to "address" of data - start of GP. (GP = 0 during method jiting). 
 * GP is easily calculated from passed PV (method start address).
 * The patch will update GP loadings.
 * The GOT section should be more than 32Kb.
 * The patch code should put proper offset since the real position of
 * qword array will be known after the function epiloge.
 */
/*------------------------------------------------------------------*/

guint8 *
mono_arch_emit_prolog (MonoCompile *cfg)
{
   MonoMethod *method = cfg->method;
   MonoMethodSignature *sig = mono_method_signature (method);
   //int alloc_size, code_size, max_offset, quad;
   unsigned int *code;
   CallInfo *cinfo;
   int	i, stack_size, offset;
   gint32 lmf_offset = cfg->arch.lmf_offset;

   CFG_DEBUG(2) ALPHA_DEBUG("mono_arch_emit_prolog");
   
   // FIXME: Use just one field to hold calculated stack size
   cfg->arch.stack_size = stack_size = cfg->stack_offset;
   cfg->arch.got_data = 0;

   cfg->code_size = 512;
   
   code = (unsigned int *)g_malloc(cfg->code_size);
   cfg->native_code = (void *)code;
   
   // Emit method prolog
   // Calculate GP from passed PV, allocate stack
   ALPHA_LOAD_GP(0)
   alpha_ldah( code, alpha_gp, alpha_pv, 0 );
   alpha_lda( code, alpha_gp, alpha_gp, 0 );     // ldgp gp, 0(pv)
   alpha_lda( code, alpha_sp, alpha_sp, -(stack_size) );

   offset = cfg->arch.params_stack_size;

   /* store call convention parameters on stack */
   alpha_stq( code, alpha_ra, alpha_sp, (offset + 0) ); // RA
   alpha_stq( code, alpha_fp, alpha_sp, (offset + 8) ); // FP
   
   /* set the frame pointer */
   alpha_mov1( code, alpha_sp, alpha_fp );

   /* Save LMF */
   if (method->save_lmf)
     {
       // Save IP
       alpha_stq(code, alpha_pv, alpha_fp,
		 (lmf_offset + G_STRUCT_OFFSET(MonoLMF, eip)));
       // Save SP
       alpha_stq(code, alpha_sp, alpha_fp,
		 (lmf_offset + G_STRUCT_OFFSET(MonoLMF, rsp)));
       // Save FP
       alpha_stq(code, alpha_fp, alpha_fp,
                 (lmf_offset + G_STRUCT_OFFSET(MonoLMF, ebp)));
       // Save GP
       alpha_stq(code, alpha_gp, alpha_fp,
                 (lmf_offset + G_STRUCT_OFFSET(MonoLMF, rgp)));

       // Save method
       alpha_stq(code, alpha_pv, alpha_fp,
                 (lmf_offset + G_STRUCT_OFFSET(MonoLMF, method)));
     }

   /* Save (global) regs */
   offset = cfg->arch.reg_save_area_offset;

   for (i = 0; i < MONO_MAX_IREGS; ++i)
     if (ALPHA_IS_CALLEE_SAVED_REG (i) &&
         (cfg->used_int_regs & (1 << i)) &&
         !( ALPHA_ARGS_REGS & (1 << i)) )
       {
         alpha_stq(code, i, alpha_fp, offset);
         CFG_DEBUG(3) g_print("ALPHA: Saved caller reg %d at offset: %0x\n",
			      i, offset);
         offset += 8;
       }
   
   offset = cfg->arch.args_save_area_offset;

   cinfo = get_call_info (cfg->generic_sharing_context, sig, FALSE);

   if (sig->ret->type != MONO_TYPE_VOID)
     {
       if ((cinfo->ret.storage == ArgInIReg) &&
	   (cfg->ret->opcode != OP_REGVAR))
	 {
	   /* Save volatile arguments to the stack */
	   alpha_stq(code, cinfo->ret.reg, cfg->ret->inst_basereg,
		     cfg->ret->inst_offset);
	 }
     }

   /* Keep this in sync with emit_load_volatile_arguments */
   for (i = 0; i < sig->param_count + sig->hasthis; ++i)
     {
       ArgInfo *ainfo = &cinfo->args [i];
       MonoInst *inst = cfg->args [i];
       int j;

       switch(ainfo->storage)
	 {
	 case ArgInIReg:
	   // We need to save all used a0-a5 params
	   {
	     if (inst->opcode == OP_REGVAR)
	       {
		 alpha_mov1(code, ainfo->reg, inst->dreg);
		 CFG_DEBUG(3) g_print("ALPHA: Saved int arg reg %d in reg %d\n",
				      ainfo->reg, inst->dreg);
	       }
	     else
	       {
		 alpha_stq(code, ainfo->reg, inst->inst_basereg,
			   inst->inst_offset);
		   
		 CFG_DEBUG(3) g_print("ALPHA: Saved int arg reg %d at offset: %0lx\n",
				      ainfo->reg, inst->inst_offset);
		   
		 offset += 8;
	       }
	   }
	   break;
	 case ArgAggregate:
	   {
	     for(j=0; j<ainfo->nregs; j++)
	       {
		 CFG_DEBUG(3) g_print("ALPHA: Saved aggregate arg reg %d at offset: %0lx\n",
				      ainfo->reg + j, inst->inst_offset + (8*j));
		 alpha_stq(code, (ainfo->reg+j), inst->inst_basereg,
			   (inst->inst_offset + (8*j)));
		 offset += 8;
	       }
	   }
	   break;
	 case ArgInDoubleReg:
	 case ArgInFloatReg:
	   // We need to save all used af0-af5 params
	   {
	     switch(cinfo->args[i].storage)
	       {
	       case ArgInFloatReg:
		 //alpha_sts(code, ainfo->reg, alpha_fp, offset);
		 alpha_sts(code, ainfo->reg, inst->inst_basereg, inst->inst_offset);
		 break;
	       case ArgInDoubleReg:
		 //alpha_stt(code, ainfo->reg, alpha_fp, offset);
		 alpha_stt(code, ainfo->reg, inst->inst_basereg, inst->inst_offset);
		 break;
	       default:
		 ;
	       }
		   
	     CFG_DEBUG(3) g_print("ALPHA: Saved float arg reg %d at offset: %0lx\n",
				  ainfo->reg, /*offset*/inst->inst_offset);
		   
	     offset += 8;
	   }
	 }
     }

   offset = cfg->arch.reg_save_area_offset;

   /*   
   for (i = 0; i < MONO_MAX_VREGS; ++i)
     if (ALPHA_IS_CALLEE_SAVED_REG (i) &&
	 (cfg->used_int_regs & (1 << i)) &&
	 !( ALPHA_ARGS_REGS & (1 << i)) )
       {  
	 alpha_stq(code, i, alpha_fp, offset);
	 CFG_DEBUG(3) g_print("ALPHA: Saved caller reg %d at offset: %0x\n",
		i, offset);
	 offset += 8;
       }
   */
   // TODO - check amd64 code for "Might need to attach the thread to the JIT"

   if (method->save_lmf)
     {
       /*
	* The call might clobber argument registers, but they are already
	* saved to the stack/global regs.
	*/

       code = emit_call (cfg, code, MONO_PATCH_INFO_INTERNAL_METHOD,
			 (gpointer)"mono_get_lmf_addr");

       // Save lmf_addr
       alpha_stq(code, alpha_r0, alpha_fp,
		 (lmf_offset + G_STRUCT_OFFSET(MonoLMF, lmf_addr)));
       // Load "previous_lmf" member of MonoLMF struct
       alpha_ldq(code, alpha_r1, alpha_r0, 0);

       // Save it to MonoLMF struct
       alpha_stq(code, alpha_r1, alpha_fp,
                 (lmf_offset + G_STRUCT_OFFSET(MonoLMF, previous_lmf)));
       
       // Set new LMF
       alpha_lda(code, alpha_r1, alpha_fp, lmf_offset);
       alpha_stq(code, alpha_r1, alpha_r0, 0);
     }

   g_free (cinfo);

   if (mono_jit_trace_calls != NULL && mono_trace_eval (method))
     code = mono_arch_instrument_prolog (cfg, mono_trace_enter_method,
					 code, TRUE);
        
   cfg->code_len = ((char *)code) - ((char *)cfg->native_code);
   
   g_assert (cfg->code_len < cfg->code_size);
   
   return (gint8 *)code;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_flush_register_windows                  */
/*                                                                  */
/* Function     -                                                   */
/*                                                                  */
/* Returns      -                                                   */
/*                                                                  */
/*------------------------------------------------------------------*/

void
mono_arch_flush_register_windows (void)
{
   ALPHA_DEBUG("mono_arch_flush_register_windows");
}
/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_regalloc_cost                           */
/*                                                                  */
/* Function     - Determine the cost, in the number of memory       */
/*                references, of the action of allocating the var-  */
/*                iable VMV into a register during global register  */
/*                allocation.                                       */
/*                                                                  */
/* Returns      - Cost                                              */
/*                                                                  */
/*------------------------------------------------------------------*/

guint32
mono_arch_regalloc_cost (MonoCompile *cfg, MonoMethodVar *vmv)
{
  MonoInst *ins = cfg->varinfo [vmv->idx];

   /* FIXME: */
  CFG_DEBUG(2) ALPHA_DEBUG("mono_arch_regalloc_cost");

  if (cfg->method->save_lmf)
    /* The register is already saved */
    /* substract 1 for the invisible store in the prolog */
    return (ins->opcode == OP_ARG) ? 1 : 0;
  else
    /* push+pop */
    return (ins->opcode == OP_ARG) ? 2 : 1;
}

/*========================= End of Function ========================*/

/*
** 
** This method emits call sequience
**
*/
static unsigned int *
emit_call(MonoCompile *cfg, unsigned int *code,
	  guint32 patch_type, gconstpointer data)
{
  int offset;
  AlphaGotData ge_data;
  
  offset = (char *)code - (char *)cfg->native_code;

  ge_data.data.p = (void *)data;
  add_got_entry(cfg, GT_PTR, ge_data,
		offset, patch_type, data);

  // Load call address into PV
  alpha_ldq(code, alpha_pv, alpha_gp, 0);

  // Call method
  alpha_jsr(code, alpha_ra, alpha_pv, 0);
  
  offset = (char *)code - (char *)cfg->native_code;
  
  // Restore GP
  ALPHA_LOAD_GP(offset)
  alpha_ldah(code, alpha_gp, alpha_ra, 0);
  alpha_lda(code, alpha_gp, alpha_gp, 0);

  return code;
}

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - arch_get_argument_info                            */
/*                                                                  */
/* Function     - Gathers information on parameters such as size,   */
/*                alignment, and padding. arg_info should be large  */
/*                enough to hold param_count + 1 entries.           */
/*                                                                  */
/* Parameters   - @csig - Method signature                          */
/*                @param_count - No. of parameters to consider      */
/*                @arg_info - An array to store the result info     */
/*                                                                  */
/* Returns      - Size of the activation frame                      */
/*                                                                  */
/*------------------------------------------------------------------*/

int
mono_arch_get_argument_info (MonoMethodSignature *csig,
                             int param_count,
                             MonoJitArgumentInfo *arg_info)
{
  int k;
  CallInfo *cinfo = get_call_info (NULL, csig, FALSE);
  guint32 args_size = cinfo->stack_usage;

  ALPHA_DEBUG("mono_arch_get_argument_info");

  /* The arguments are saved to a stack area in mono_arch_instrument_prolog */
  if (csig->hasthis) 
    {
      arg_info [0].offset = 0;
    }

  for (k = 0; k < param_count; k++) 
    {
      arg_info [k + 1].offset = ((k + csig->hasthis) * 8);

      /* FIXME: */
      // Set size to 1
      // The size is checked only for valuetype in trace.c
      arg_info [k + 1].size = 8;
    }
  
  g_free (cinfo);
  
  return args_size;
}

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_emit_epilog                             */
/*                                                                  */
/* Function     - Emit the instructions for a function epilog.      */
/*                                                                  */
/*------------------------------------------------------------------*/

void
mono_arch_emit_epilog (MonoCompile *cfg)
{
  MonoMethod *method = cfg->method;
  int offset, i;
  unsigned int *code;
  int max_epilog_size = 128;
  int stack_size = cfg->arch.stack_size;
  //  CallInfo *cinfo;
  gint32 lmf_offset = cfg->arch.lmf_offset;
  
  CFG_DEBUG(2) ALPHA_DEBUG("mono_arch_emit_epilog");
   
  while (cfg->code_len + max_epilog_size > (cfg->code_size - 16))
    {
      cfg->code_size *= 2;
      cfg->native_code = g_realloc (cfg->native_code, cfg->code_size);
      cfg->stat_code_reallocs++;
    }
  
  code = (unsigned int *)(cfg->native_code + cfg->code_len);
  
  if (mono_jit_trace_calls != NULL && mono_trace_eval (method))
    code = mono_arch_instrument_epilog (cfg, mono_trace_leave_method,
					code, TRUE);

  if (method->save_lmf)
    {
      /* Restore previous lmf */
      alpha_ldq(code, alpha_at, alpha_fp,
		(lmf_offset + G_STRUCT_OFFSET (MonoLMF, previous_lmf)));
      alpha_ldq(code, alpha_ra, alpha_fp,
                (lmf_offset + G_STRUCT_OFFSET (MonoLMF, lmf_addr)));
      alpha_stq(code, alpha_at, alpha_ra, 0);
    }
  
  // 5 instructions.
  alpha_mov1( code, alpha_fp, alpha_sp );
   
  // Restore saved regs
  offset = cfg->arch.reg_save_area_offset;
   
  for (i = 0; i < MONO_MAX_IREGS; ++i)
    if (ALPHA_IS_CALLEE_SAVED_REG (i) &&
	(cfg->used_int_regs & (1 << i)) &&
	!( ALPHA_ARGS_REGS & (1 << i)) )
      {
	alpha_ldq(code, i, alpha_sp, offset);
	CFG_DEBUG(3) g_print("ALPHA: Restored caller reg %d at offset: %0x\n",
	       i, offset);
	offset += 8;
      }
  
  /* restore fp, ra, sp */
  offset = cfg->arch.params_stack_size;

  alpha_ldq( code, alpha_ra, alpha_sp, (offset + 0) );
  alpha_ldq( code, alpha_fp, alpha_sp, (offset + 8) );
  alpha_lda( code, alpha_sp, alpha_sp, stack_size );
  
  /* return */
  alpha_ret( code, alpha_ra, 1 );
  
  cfg->code_len = ((char *)code) - ((char *)cfg->native_code);
  
  g_assert (cfg->code_len < cfg->code_size);
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_emit_exceptions                         */
/*                                                                  */
/* Function     - Emit the blocks to handle exception conditions.   */
/*                                                                  */
/*------------------------------------------------------------------*/

void
mono_arch_emit_exceptions (MonoCompile *cfg)
{
  MonoJumpInfo *patch_info;
  int nthrows, i;
  unsigned int *code, *got_start;
  unsigned long *corlib_exc_adr;
  MonoClass *exc_classes [16];
  guint8 *exc_throw_start [16], *exc_throw_end [16];
  guint32 code_size = 8;  // Reserve space for address to mono_arch_throw_corlib_exception
  AlphaGotEntry *got_data;

  CFG_DEBUG(2) ALPHA_DEBUG("mono_arch_emit_exceptions");

  /* Compute needed space */
  for (patch_info = cfg->patch_info; patch_info;
       patch_info = patch_info->next)
    {
      if (patch_info->type == MONO_PATCH_INFO_EXC)
	code_size += 40;
      if (patch_info->type == MONO_PATCH_INFO_R8)
	code_size += 8 + 7; /* sizeof (double) + alignment */
      if (patch_info->type == MONO_PATCH_INFO_R4)
	code_size += 4 + 7; /* sizeof (float) + alignment */
    }

  // Reserve space for GOT entries
  for (got_data = cfg->arch.got_data; got_data;
       got_data = got_data->next)
    {
       // Reserve space for 8 byte const (for now)
       if (got_data->type != GT_LD_GTADDR)
      	   code_size += 8;
    }

  while (cfg->code_len + code_size > (cfg->code_size - 16))
    {
      cfg->code_size *= 2;
      cfg->native_code = g_realloc (cfg->native_code, cfg->code_size);
      cfg->stat_code_reallocs++;
    }
  
  code = (unsigned int *)((char *)cfg->native_code + cfg->code_len);

  // Set code alignment
  if (((unsigned long)code) % 8)
      code++;

  got_start = code; 

  /* Add code to store conts and modify patch into to store offset in got */
  for (got_data = cfg->arch.got_data; got_data;
       got_data = got_data->next)
    {
      unsigned long data = got_data->value.data.l;
      MonoJumpInfo *got_ref = got_data->got_patch_info;
      
      // Modify loading of GP
      if (got_data->type == GT_LD_GTADDR)
      {
	short high_off, low_off;
	unsigned int *ldgp_code =
		(unsigned int *)(cfg->native_code + got_data->value.data.l);
	unsigned int got_off = (char *)got_start - (char *)ldgp_code;

	high_off = got_off / 0x10000;
  	low_off = got_off % 0x10000;
	if (low_off < 0)
	  high_off++;
	
	// Set offset from current point to GOT array
	// modify the following code sequence
	// ldah gp, 0(pv) or ldah gp, 0(ra)
	// lda  gp, 0(gp) 
	*ldgp_code = (*ldgp_code | (high_off & 0xFFFF));
	ldgp_code++;
	*ldgp_code = (*ldgp_code | (low_off & 0xFFFF));

	continue;
      }

      patch_info = got_data->patch_info;

      // Check code alignment
      if (((unsigned long)code) % 8)
	code++;

      got_ref->data.offset = ((char *)code - (char *)got_start);

      if (patch_info)
	patch_info->ip.i = ((char *)code - (char *)cfg->native_code);

      *code = (unsigned int)(data & 0xFFFFFFFF);
      code++;
      *code = (unsigned int)((data >> 32) & 0xFFFFFFFF);
      code++;
      
    }

  corlib_exc_adr = (unsigned long *)code;

  /* add code to raise exceptions */
  nthrows = 0;
  for (patch_info = cfg->patch_info; patch_info;
       patch_info = patch_info->next)
    {
      switch (patch_info->type)
	{
	case MONO_PATCH_INFO_EXC:
	  {
	    MonoClass *exc_class;
	    unsigned int *buf, *buf2;
	    guint32 throw_ip;
	    
	    if (nthrows == 0)
	      {
		// Add patch info to call mono_arch_throw_corlib_exception
		// method to raise corlib exception
		// Will be added at the begining of the patch info list
		mono_add_patch_info(cfg,
				    ((char *)code - (char *)cfg->native_code),
				    MONO_PATCH_INFO_INTERNAL_METHOD,
				    "mono_arch_throw_corlib_exception");
		
		// Skip longword before starting the code
		*code++ = 0;
		*code++ = 0;
	      }
	    
	    exc_class = mono_class_from_name (mono_defaults.corlib,
					      "System", patch_info->data.name);

	    g_assert (exc_class);
	    throw_ip = patch_info->ip.i;
	    
	    //x86_breakpoint (code);
	    /* Find a throw sequence for the same exception class */
	    for (i = 0; i < nthrows; ++i)
	      if (exc_classes [i] == exc_class)
		break;

	    if (i < nthrows)
	      {
		int br_offset;

                // Patch original branch (patch info) to jump here
                patch_info->type = MONO_PATCH_INFO_METHOD_REL;
                patch_info->data.target = 
		  (char *)code - (char *)cfg->native_code;

		alpha_lda(code, alpha_a1, alpha_zero,
			  -((short)((((char *)exc_throw_end[i] -
				    (char *)cfg->native_code)) - throw_ip) - 4) );

		br_offset = ((char *)exc_throw_start[i] - (char *)code - 4)/4;

		alpha_bsr(code, alpha_zero, br_offset);
	      }
	    else
	      {
		buf = code;

		// Save exception token type as first 32bit word for new
		// exception handling jump code
		*code = exc_class->type_token;
		code++;

		// Patch original branch (patch info) to jump here
		patch_info->type = MONO_PATCH_INFO_METHOD_REL;
		patch_info->data.target = 
		  (char *)code - (char *)cfg->native_code;

		buf2 = code;
		alpha_lda(code, alpha_a1, alpha_zero, 0);

		if (nthrows < 16)
		  {
		    exc_classes [nthrows] = exc_class;
		    exc_throw_start [nthrows] = code;
		  }
		
		// Load exception token
		alpha_ldl(code, alpha_a0, alpha_gp,
			  ((char *)buf - (char *)got_start /*cfg->native_code*/));
		// Load corlib exception raiser code address
		alpha_ldq(code, alpha_pv, alpha_gp,
			  ((char *)corlib_exc_adr -
			   (char *)got_start /*cfg->native_code*/));

		//amd64_mov_reg_imm (code, AMD64_RDI, exc_class->type_token);
		//patch_info->data.name = "mono_arch_throw_corlib_exception";
		//**patch_info->type = MONO_PATCH_INFO_INTERNAL_METHOD;
		//patch_info->type = MONO_PATCH_INFO_NONE;
		//patch_info->ip.i = (char *)code - (char *)cfg->native_code;
		
		if (cfg->compile_aot)
		  {
		    // amd64_mov_reg_membase (code, GP_SCRATCH_REG, AMD64_RIP, 0, 8);
		    //amd64_call_reg (code, GP_SCRATCH_REG);
		  } else {
		  /* The callee is in memory allocated using
		     the code manager */
		  alpha_jsr(code, alpha_ra, alpha_pv, 0);
		}
		
                alpha_lda(buf2, alpha_a1, alpha_zero,
                          -((short)(((char *)code - (char *)cfg->native_code) -
				    throw_ip)-4) );

		if (nthrows < 16)
		  {
		    exc_throw_end [nthrows] = code;
		    nthrows ++;
		  }
	      }
	    break;
	  }
	default:
	  /* do nothing */
	  break;
	}
    }
  
  /* Handle relocations with RIP relative addressing */
  for (patch_info = cfg->patch_info; patch_info;
       patch_info = patch_info->next)
    {
      gboolean remove = FALSE;

      switch (patch_info->type)
	{
	case MONO_PATCH_INFO_R8:
	  {
	    guint8 *pos;

	    code = (guint8*)ALIGN_TO (code, 8);

	    pos = cfg->native_code + patch_info->ip.i;

	    *(double*)code = *(double*)patch_info->data.target;

	    //	    if (use_sse2)
	    //  *(guint32*)(pos + 4) = (guint8*)code - pos - 8;
	    //else
	      *(guint32*)(pos + 3) = (guint8*)code - pos - 7;
	    code += 8;
	    
	    remove = TRUE;
	    break;
	  }
	case MONO_PATCH_INFO_R4:
	  {
	    guint8 *pos;
	    
	    code = (guint8*)ALIGN_TO (code, 8);
	    
	    pos = cfg->native_code + patch_info->ip.i;
	    
	    *(float*)code = *(float*)patch_info->data.target;
	    
	    //if (use_sse2)
	    //  *(guint32*)(pos + 4) = (guint8*)code - pos - 8;
	    //else
	      *(guint32*)(pos + 3) = (guint8*)code - pos - 7;
	    code += 4;
	    
	    remove = TRUE;
	    break;
	  }
	default:
	  break;
	}
      
      if (remove)
	{
	  if (patch_info == cfg->patch_info)
	    cfg->patch_info = patch_info->next;
	  else
	    {
	      MonoJumpInfo *tmp;

	      for (tmp = cfg->patch_info; tmp->next != patch_info;
		   tmp = tmp->next)
		;
	      tmp->next = patch_info->next;
	    }
	}
    }
  
  cfg->code_len = (char *)code - (char *)cfg->native_code;

  g_assert (cfg->code_len < cfg->code_size);

}

/*========================= End of Function ========================*/

#define EMIT_ALPHA_BRANCH(Tins, PRED_REG, ALPHA_BR)	\
  offset = ((char *)code -                      \
	    (char *)cfg->native_code);          \
      if (Tins->inst_true_bb->native_offset)				\
	{								\
	  long br_offset = (char *)cfg->native_code +			\
	    Tins->inst_true_bb->native_offset - 4 - (char *)code;	\
	  CFG_DEBUG(3) g_print("jump to: native_offset: %0X, address %p]\n", \
		 Tins->inst_target_bb->native_offset,			\
		 cfg->native_code +					\
		 Tins->inst_true_bb->native_offset);			\
	  alpha_##ALPHA_BR (code, PRED_REG, br_offset/4);		\
	}								\
      else								\
	{								\
	  CFG_DEBUG(3) g_print("add patch info: MONO_PATCH_INFO_BB offset: %0X, target_bb: %p]\n", \
		 offset, Tins->inst_target_bb);				\
	  mono_add_patch_info (cfg, offset,				\
			       MONO_PATCH_INFO_BB,			\
			       Tins->inst_true_bb);			\
	  alpha_##ALPHA_BR (code, PRED_REG, 0);				\
	}


#define EMIT_COND_EXC_BRANCH(ALPHA_BR, PRED_REG, EXC_NAME)		\
  do									\
    {									\
      MonoInst *tins = mono_branch_optimize_exception_target (cfg,	\
							      bb,	\
							      EXC_NAME); \
      if (tins == NULL)							\
	{								\
	  mono_add_patch_info (cfg,					\
			       ((char *)code -				\
				(char *)cfg->native_code),		\
			       MONO_PATCH_INFO_EXC, EXC_NAME);		\
	  alpha_##ALPHA_BR(code, PRED_REG, 0);				\
	}								\
      else								\
	{								\
	  EMIT_ALPHA_BRANCH(tins, PRED_REG, ALPHA_BR);			\
	}								\
    } while(0);


/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_output_basic_block                      */
/*                                                                  */
/* Function     - Perform the "real" work of emitting instructions  */
/*                that will do the work of in the basic block.      */
/*                                                                  */
/*------------------------------------------------------------------*/

void
mono_arch_output_basic_block (MonoCompile *cfg, MonoBasicBlock *bb)
{
   MonoInst *ins;
   MonoCallInst *call;
   guint offset;
   unsigned int *code = (unsigned int *)(cfg->native_code + cfg->code_len);
   MonoInst *last_ins = NULL;
   guint last_offset = 0;
   int max_len, cpos;
   
   CFG_DEBUG(2) ALPHA_DEBUG("mono_arch_output_basic_block");
   
   CFG_DEBUG(2) g_print ("Basic block %d(%p) starting at offset 0x%x\n",
			 bb->block_num, bb, bb->native_offset);
   
   cpos = bb->max_offset;
   
   offset = ((char *)code) - ((char *)cfg->native_code);

   mono_debug_open_block (cfg, bb, offset);
   
   MONO_BB_FOR_EACH_INS (bb, ins) {
       offset = ((char *)code) - ((char *)cfg->native_code);
	  
       max_len = ((guint8 *)ins_get_spec (ins->opcode))[MONO_INST_LEN];
	  
       if (offset > (cfg->code_size - max_len - 16))
	 {
	   cfg->code_size *= 2;
	   cfg->native_code = g_realloc (cfg->native_code, cfg->code_size);
	   code = (unsigned int *)(cfg->native_code + offset);
	   cfg->stat_code_reallocs++;
	 }
	  
       mono_debug_record_line_number (cfg, ins, offset);

       CFG_DEBUG(3) g_print("ALPHA: Emiting [%s] opcode\n",
			    mono_inst_name(ins->opcode));
	  
       switch (ins->opcode)
	 {
	 case OP_RELAXED_NOP:
	 	break;
	 case OP_LSHR:
	   // Shift 64 bit value right
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [long_shr] dreg=%d, sreg1=%d, sreg2=%d\n",
		  ins->dreg, ins->sreg1, ins->sreg2);
	   alpha_sra(code, ins->sreg1, ins->sreg2, ins->dreg);
	   break;

	case OP_LSHR_UN:
	   // Shift 64 bit value right
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [long_shr_un] dreg=%d, sreg1=%d, sreg2=%d\n",
                  ins->dreg, ins->sreg1, ins->sreg2);
           alpha_srl(code, ins->sreg1, ins->sreg2, ins->dreg);
           break;

	 case OP_LSHR_IMM:
	   // Shift 64 bit value right by constant
	   g_assert(alpha_is_imm(ins->inst_imm));
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [long_shr_imm] dreg=%d, sreg1=%d, const=%ld\n",
		  ins->dreg, ins->sreg1, ins->inst_imm);
	   alpha_sra_(code, ins->sreg1, ins->inst_imm, ins->dreg);
	   break;

	 case OP_ISHL:
           // Shift 32 bit value left
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [int_shl] dreg=%d, sreg1=%d, sreg2=%d\n",
                  ins->dreg, ins->sreg1, ins->sreg2);
           alpha_sll(code, ins->sreg1, ins->sreg2, ins->dreg);
	   alpha_addl_(code, ins->dreg, 0, ins->dreg);
           break;

	 case OP_ISHL_IMM:
           // Shift 32 bit value left by constant
           g_assert(alpha_is_imm(ins->inst_imm));
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [int_shl_imm] dreg=%d, sreg1=%d, const=%ld\n",
                  ins->dreg, ins->sreg1, ins->inst_imm);
           alpha_sll_(code, ins->sreg1, ins->inst_imm, ins->dreg);
	   alpha_addl_(code, ins->dreg, 0, ins->dreg);
           break;

	 case OP_SHL_IMM:
	   g_assert(alpha_is_imm(ins->inst_imm));
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [shl_imm] dreg=%d, sreg1=%d, const=%ld\n",
                  ins->dreg, ins->sreg1, ins->inst_imm);
           alpha_sll_(code, ins->sreg1, ins->inst_imm, ins->dreg);
           break;

         case OP_LSHL_IMM:
           g_assert(alpha_is_imm(ins->inst_imm));
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [long_shl_imm] dreg=%d, sreg1=%d, const=%ld\n",
                  ins->dreg, ins->sreg1, ins->inst_imm);
           alpha_sll_(code, ins->sreg1, ins->inst_imm, ins->dreg);
           break;


	 case CEE_SHL:
           // Shift 32 bit value left
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [shl] dreg=%d, sreg1=%d, sreg2=%d\n",
                  ins->dreg, ins->sreg1, ins->sreg2);
           alpha_sll(code, ins->sreg1, ins->sreg2, ins->dreg);
           break;

         case OP_LSHL:
           // Shift 64 bit value left
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [long_shl] dreg=%d, sreg1=%d, sreg2=%d\n",
                  ins->dreg, ins->sreg1, ins->sreg2);
           alpha_sll(code, ins->sreg1, ins->sreg2, ins->dreg);
           break;


         case OP_ISHR:
           // Shift 32 bit value right
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [int_shr] dreg=%d, sreg1=%d, sreg2=%d\n",
                  ins->dreg, ins->sreg1, ins->sreg2);
	   //alpha_zap_(code, ins->sreg1, 0xF0, ins->dreg);
           alpha_sra(code, ins->sreg1, ins->sreg2, ins->dreg);
           break;

         case OP_ISHR_IMM:
           // Shift 32 bit value rigth by constant
           g_assert(alpha_is_imm(ins->inst_imm));
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [int_shr_imm] dreg=%d, sreg1=%d, const=%ld\n",
                  ins->dreg, ins->sreg1, ins->inst_imm);
	   //alpha_zap_(code, ins->sreg1, 0xF0, ins->dreg);
           alpha_sra_(code, ins->sreg1, ins->inst_imm, ins->dreg);
           break;

         case OP_ISHR_UN:
           // Shift 32 bit unsigned value right
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [int_shr_un] dreg=%d, sreg1=%d, sreg2=%d\n",
                  ins->dreg, ins->sreg1, ins->sreg2);
	   alpha_zap_(code, ins->sreg1, 0xF0, alpha_at /*ins->dreg*/);
           alpha_srl(code, alpha_at /*ins->dreg*/, ins->sreg2, ins->dreg);
           break;

         case OP_ISHR_UN_IMM:
           // Shift 32 bit unassigned value rigth by constant
           g_assert(alpha_is_imm(ins->inst_imm));
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [int_shr_un_imm] dreg=%d, sreg1=%d, const=%ld\n",
				ins->dreg, ins->sreg1, ins->inst_imm);
	   alpha_zap_(code, ins->sreg1, 0xF0, alpha_at /*ins->dreg*/);
           alpha_srl_(code, alpha_at /*ins->dreg*/, ins->inst_imm, ins->dreg);
           break;

         case OP_LSHR_UN_IMM:
           // Shift 64 bit unassigned value rigth by constant
           g_assert(alpha_is_imm(ins->inst_imm));
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [long_shr_un_imm] dreg=%d, sreg1=%d, const=%ld\n",
				ins->dreg, ins->sreg1, ins->inst_imm);
           alpha_srl_(code, ins->sreg1, ins->inst_imm, ins->dreg);
           break;

	 case CEE_ADD:
	   // Sum two 64 bits regs
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [add] dreg=%d, sreg1=%d, sreg2=%d\n",
		  ins->dreg, ins->sreg1, ins->sreg2);
	   alpha_addq(code, ins->sreg1, ins->sreg2, ins->dreg);
	   break;

	 case CEE_SUB:
	   // Subtract two 64 bit regs
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [sub] dreg=%d, sreg1=%d, sreg2=%d\n",
		  ins->dreg, ins->sreg1, ins->sreg2);
	   alpha_subq(code, ins->sreg1, ins->sreg2, ins->dreg);
	   break;

	 case OP_ADD_IMM:
	   // Add imm value to 64 bits int
	   g_assert(alpha_is_imm(ins->inst_imm));
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [add_imm] dreg=%d, sreg1=%d, imm=%ld\n",
		  ins->dreg, ins->sreg1, ins->inst_imm);
	   alpha_addq_(code, ins->sreg1, ins->inst_imm, ins->dreg);
	   break;

	 case OP_IADD:
	   // Add two 32 bit ints
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [iadd] dreg=%d, sreg1=%d, sreg2=%d\n",
		  ins->dreg, ins->sreg1, ins->sreg2);
	   alpha_addl(code, ins->sreg1, ins->sreg2, ins->dreg);
	   break;

	 case OP_IADDCC:
	   // Add two 32 bit ints with overflow detection
	   // Use AT to hold flag of signed overflow
	   // Use t12(PV) to hold unsigned overflow
	   // Use RA to hold intermediate result
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [iaddcc] dreg=%d, sreg1=%d, sreg2=%d\n",
                  ins->dreg, ins->sreg1, ins->sreg2);
           alpha_addl(code, ins->sreg1, ins->sreg2, alpha_ra);
	   alpha_ble(code, ins->sreg2, 2);

	   /* (sreg2 > 0) && (res < ins->sreg1) => signed overflow */
	   alpha_cmplt(code, alpha_ra, ins->sreg1, alpha_at);
	   alpha_br(code, alpha_zero, 1);

	   /* (sreg2 <= 0) && (res > ins->sreg1) => signed overflow */
	   alpha_cmplt(code, ins->sreg1, alpha_ra, alpha_at);

	   /* res <u sreg1 => unsigned overflow */
	   alpha_cmpult(code, alpha_ra, ins->sreg1, alpha_pv);

	   alpha_mov1(code, alpha_ra, ins->dreg);
           break;

         case OP_ADDCC:
           // Add two 64 bit ints with overflow detection
           // Use AT to hold flag of signed overflow
           // Use t12(PV) to hold unsigned overflow
           // Use RA to hold intermediate result
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [addcc] dreg=%d, sreg1=%d, sreg2=%d\n",
				ins->dreg, ins->sreg1, ins->sreg2);
           alpha_addq(code, ins->sreg1, ins->sreg2, alpha_ra);
           alpha_ble(code, ins->sreg2, 2);

           /* (sreg2 > 0) && (res < ins->sreg1) => signed overflow */
           alpha_cmplt(code, alpha_ra, ins->sreg1, alpha_at);
           alpha_br(code, alpha_zero, 1);

           /* (sreg2 <= 0) && (res > ins->sreg1) => signed overflow */
           alpha_cmplt(code, ins->sreg1, alpha_ra, alpha_at);

           /* res <u sreg1 => unsigned overflow */
           alpha_cmpult(code, alpha_ra, ins->sreg1, alpha_pv);

           alpha_mov1(code, alpha_ra, ins->dreg);
           break;

	 case OP_IADD_IMM:
	   // Add imm value to 32 bits int
	   g_assert(alpha_is_imm(ins->inst_imm));
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [iadd_imm] dreg=%d, sreg1=%d, imm=%ld\n",
		  ins->dreg, ins->sreg1, ins->inst_imm);
	   alpha_addl_(code, ins->sreg1, ins->inst_imm, ins->dreg);
	   break;
		 
	 case OP_ISUB:
	   // Substract to 32 bit ints
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [isub] dreg=%d, sreg1=%d, sreg2=%d\n",
		  ins->dreg, ins->sreg1, ins->sreg2);
	   alpha_subl(code, ins->sreg1, ins->sreg2, ins->dreg);
	   break;
		 
	 case OP_ISUB_IMM:
	   // Sub imm value from 32 bits int
	   g_assert(alpha_is_imm(ins->inst_imm));
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [isub_imm] dreg=%d, sreg1=%d, imm=%ld\n",
		  ins->dreg, ins->sreg1, ins->inst_imm);
	   alpha_subl_(code, ins->sreg1, ins->inst_imm, ins->dreg);
	   break;

	 case OP_ISUBCC:
	   // Sub to 32 bit ints with overflow detection
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [isubcc] dreg=%d, sreg1=%d, sreg2=%d\n",
                  ins->dreg, ins->sreg1, ins->sreg2);
           alpha_subl(code, ins->sreg1, ins->sreg2, alpha_ra);
	   alpha_ble(code, ins->sreg2, 2);

	   /* (sreg2 > 0) && (res > ins->sreg1) => signed overflow */
	   alpha_cmplt(code, ins->sreg1, alpha_ra, alpha_at);
           alpha_br(code, alpha_zero, 1);

	   /* (sreg2 <= 0) && (res < ins->sreg1) => signed overflow */
	   alpha_cmplt(code, alpha_ra, ins->sreg1, alpha_at);

	   /* sreg1 <u sreg2 => unsigned overflow */
	   alpha_cmpult(code, ins->sreg1, ins->sreg2, alpha_pv);

           alpha_mov1(code, alpha_ra, ins->dreg);
           break;

         case OP_SUBCC:
           // Sub to 64 bit ints with overflow detection
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [subcc] dreg=%d, sreg1=%d, sreg2=%d\n",
				ins->dreg, ins->sreg1, ins->sreg2);

           alpha_subq(code, ins->sreg1, ins->sreg2, alpha_ra);
           alpha_ble(code, ins->sreg2, 2);

           /* (sreg2 > 0) && (res > ins->sreg1) => signed overflow */
           alpha_cmplt(code, ins->sreg1, alpha_ra, alpha_at);
           alpha_br(code, alpha_zero, 1);

           /* (sreg2 <= 0) && (res < ins->sreg1) => signed overflow */
           alpha_cmplt(code, alpha_ra, ins->sreg1, alpha_at);

           /* sreg1 <u sreg2 => unsigned overflow */
           alpha_cmpult(code, ins->sreg1, ins->sreg2, alpha_pv);

           alpha_mov1(code, alpha_ra, ins->dreg);
           break;

	 case OP_IAND:
	 case CEE_AND:
	   // AND to 32 bit ints
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [iand/and] dreg=%d, sreg1=%d, sreg2=%d\n",
		  ins->dreg, ins->sreg1, ins->sreg2);
	   alpha_and(code, ins->sreg1, ins->sreg2, ins->dreg);
	   break;
	   
	 case OP_IAND_IMM:
	 case OP_AND_IMM:
	   // AND imm value with 32 bit int
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [iand_imm/and_imm] dreg=%d, sreg1=%d, imm=%ld\n",
		  ins->dreg, ins->sreg1, ins->inst_imm);

	   g_assert(alpha_is_imm(ins->inst_imm));
	   alpha_and_(code, ins->sreg1, ins->inst_imm, ins->dreg);

	   break;

         case OP_IOR:
	 case CEE_OR:
           // OR two 32/64 bit ints
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [ior/or] dreg=%d, sreg1=%d, sreg2=%d\n",
                  ins->dreg, ins->sreg1, ins->sreg2);
           alpha_bis(code, ins->sreg1, ins->sreg2, ins->dreg);
           break;

         case OP_IOR_IMM:
           // OR imm value with 32 bit int
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [ior_imm] dreg=%d, sreg1=%d, imm=%ld\n",
                  ins->dreg, ins->sreg1, ins->inst_imm);

           g_assert(alpha_is_imm(ins->inst_imm));
           alpha_bis_(code, ins->sreg1, ins->inst_imm, ins->dreg);

           break;

         case OP_IXOR:
	 case CEE_XOR:
           // XOR two 32/64 bit ints
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [ixor/xor] dreg=%d, sreg1=%d, sreg2=%d\n",
                  ins->dreg, ins->sreg1, ins->sreg2);
           alpha_xor(code, ins->sreg1, ins->sreg2, ins->dreg);
           break;

         case OP_IXOR_IMM:
           // XOR imm value with 32 bit int
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [ixor_imm] dreg=%d, sreg1=%d, imm=%ld\n",
                  ins->dreg, ins->sreg1, ins->inst_imm);

           g_assert(alpha_is_imm(ins->inst_imm));
           alpha_xor_(code, ins->sreg1, ins->inst_imm, ins->dreg);

           break;

	 case OP_INEG:
	   // NEG 32 bit reg
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [ineg] dreg=%d, sreg1=%d\n",
                  ins->dreg, ins->sreg1);
	   alpha_subl(code, alpha_zero, ins->sreg1, ins->dreg);
	   break;

         case CEE_NEG:
           // NEG 64 bit reg
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [neg] dreg=%d, sreg1=%d\n",
				ins->dreg, ins->sreg1);
           alpha_subq(code, alpha_zero, ins->sreg1, ins->dreg);
           break;

         case OP_INOT:
	 case CEE_NOT:
           // NOT 32/64 bit reg
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [inot/not] dreg=%d, sreg1=%d\n",
                  ins->dreg, ins->sreg1);
           alpha_not(code, ins->sreg1, ins->dreg);
           break;

	   
	 case OP_IDIV:
	 case OP_IREM:
	 case OP_IMUL:
	 case OP_IMUL_OVF:
	 case OP_IMUL_OVF_UN:
	 case OP_IDIV_UN:
	 case OP_IREM_UN:
	   CFG_DEBUG(4) g_print("ALPHA_TODO: [idiv/irem/imul/imul_ovf/imul_ovf_un/idiv_un/irem_un] dreg=%d, sreg1=%d, sreg2=%d\n",
		  ins->dreg, ins->sreg1, ins->sreg2);
	   break;
	   
	 case CEE_MUL:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [mul] dreg=%d, sreg1=%d, sreg2=%d\n",
		  ins->dreg, ins->sreg1, ins->sreg2);
	   alpha_mull(code, ins->sreg1, ins->sreg2, ins->dreg);
	   break;
	     
	 case OP_IMUL_IMM:
	   CFG_DEBUG(4) g_print("ALPHA_TODO: [imul_imm] dreg=%d, sreg1=%d, imm=%ld\n",
		  ins->dreg, ins->sreg1, ins->inst_imm);
	   break;
	   
	 case OP_CHECK_THIS:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [check_this] sreg1=%d\n",
		  ins->sreg1);
	   alpha_ldl(code, alpha_at, ins->sreg1, 0);
	   break;

	 case OP_SEXT_I1:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [sext_i1] dreg=%d, sreg1=%d\n",
		  ins->dreg, ins->sreg1);
           alpha_sll_(code, ins->sreg1, 56, ins->dreg);
           alpha_sra_(code, ins->dreg, 56, ins->dreg);
           break;

	 case OP_SEXT_I2:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [sext_i2] dreg=%d, sreg1=%d\n",
                  ins->dreg, ins->sreg1);
           alpha_sll_(code, ins->sreg1, 48, ins->dreg);
           alpha_sra_(code, ins->dreg, 48, ins->dreg);
           break;

	 case OP_SEXT_I4:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [sext_i4] dreg=%d, sreg1=%d\n",
                  ins->dreg, ins->sreg1);
	   alpha_sll_(code, ins->sreg1, 32, ins->dreg);
	   alpha_sra_(code, ins->dreg, 32, ins->dreg);
	   break;

	 case OP_ICONST:
	   // Actually ICONST is 32 bits long
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [iconst] dreg=%d, const=%0lX\n",
		  ins->dreg, ins->inst_c0);

	   // if const = 0
	   if (ins->inst_c0 == 0)
	     {
	       alpha_clr(code, ins->dreg);
	       break;
	     }

	   // if -32768 < const <= 32767
	   if (ins->inst_c0 > -32768 && ins->inst_c0 <= 32767)
	     {
	       alpha_lda( code, ins->dreg, alpha_zero, ins->inst_c0);
	       //if (ins->inst_c0 & 0xFFFFFFFF00000000L)
	       //	 alpha_zap_(code, ins->dreg, 0xF0, ins->dreg);
	     }
	   else
	     {
	       int lo = (char *)code - (char *)cfg->native_code;
	       AlphaGotData ge_data;

	       //ge_data.data.l = (ins->inst_c0 & 0xFFFFFFFF);
	       ge_data.data.l = ins->inst_c0;

	       add_got_entry(cfg, GT_LONG, ge_data,
			     lo, MONO_PATCH_INFO_NONE, 0);
	       //mono_add_patch_info(cfg, lo, MONO_PATCH_INFO_GOT_OFFSET,
	       //		   ins->inst_c0);
	       //alpha_ldl(code, ins->dreg, alpha_gp, 0);
	       alpha_ldq(code, ins->dreg, alpha_gp, 0);
	     }

	   break;
	 case OP_I8CONST:
	   {
	     int lo;
	     
	     // To load 64 bit values we will have to use ldah/lda combination
	     // and temporary register. As temporary register use r28
	     // Divide 64 bit value in two parts and load upper 32 bits into
	     // temp reg, lower 32 bits into dreg. Later set higher 32 bits in
	     // dreg from temp reg
	     // the 32 bit value could be loaded with ldah/lda
	     CFG_DEBUG(4) g_print("ALPHA_CHECK: [i8conts] dreg=%d, const=%0lX\n",
		    ins->dreg, ins->inst_c0);
	     
	     // if const = 0
	     if (ins->inst_c0 == 0)
	       {
		 alpha_clr(code, ins->dreg);
		 break;
	       }

	     // if -32768 < const <= 32767 
	     if (ins->inst_c0 > -32768 && ins->inst_c0 <= 32767)
	       alpha_lda( code, ins->dreg, alpha_zero, ins->inst_c0);
	     else
	       {
		 AlphaGotData ge_data;

		 lo = (char *)code - (char *)cfg->native_code;
		 
		 ge_data.data.l = ins->inst_c0;

		 add_got_entry(cfg, GT_LONG, ge_data,
			       lo, MONO_PATCH_INFO_NONE, 0);
		 alpha_ldq(code, ins->dreg, alpha_gp, 0);
	       }
	     break;
	   }

	 case OP_R8CONST:
	   {
	     double d = *(double *)ins->inst_p0;
	     AlphaGotData ge_data;

	     CFG_DEBUG(4) g_print("ALPHA_CHECK: [r8const] dreg=%d, r8const=%g\n",
		    ins->dreg, d);

	     ge_data.data.d = d;
	     add_got_entry(cfg, GT_DOUBLE, ge_data,
			   (char *)code - (char *)cfg->native_code,
			   MONO_PATCH_INFO_NONE, 0);
	     alpha_ldt(code, ins->dreg, alpha_gp, 0);

	     break;
	   }

	 case OP_R4CONST:
           {
             float d = *(float *)ins->inst_p0;
             AlphaGotData ge_data;

             CFG_DEBUG(4) g_print("ALPHA_CHECK: [r4const] dreg=%d, r4const=%f\n",
                    ins->dreg, d);

             ge_data.data.f = d;
             add_got_entry(cfg, GT_FLOAT, ge_data,
                           (char *)code - (char *)cfg->native_code,
                           MONO_PATCH_INFO_NONE, 0);
             alpha_lds(code, ins->dreg, alpha_gp, 0);

             break;
           }

	 case OP_LOADU4_MEMBASE:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [loadu4_membase] dreg=%d, basereg=%d, offset=%0lx\n",
		  ins->dreg, ins->inst_basereg, ins->inst_offset);

	   alpha_ldl(code, ins->dreg, ins->inst_basereg, ins->inst_offset);
	   //	   alpha_zapnot_(code, ins->dreg, 0x0F, ins->dreg);
	   break;
	   
	 case OP_LOADU1_MEMBASE:
	   // Load unassigned byte from REGOFFSET
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [loadu1_membase] dreg=%d, basereg=%d, offset=%0lx\n",
		  ins->dreg, ins->inst_basereg, ins->inst_offset);
	   if (bwx_supported)
	     alpha_ldbu(code, ins->dreg, ins->inst_basereg, ins->inst_offset);
	   else
	     {
	       alpha_ldq_u(code, alpha_r25, ins->inst_basereg,
			   ins->inst_offset);
	       alpha_lda(code, alpha_at, ins->inst_basereg, ins->inst_offset);
	       alpha_extbl(code, alpha_r25, alpha_at, ins->dreg);
	     }
	   break;
	   
	 case OP_LOADU2_MEMBASE:
	   // Load unassigned word from REGOFFSET
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [loadu2_membase] dreg=%d, basereg=%d, offset=%0lx\n",
		  ins->dreg, ins->inst_basereg, ins->inst_offset);

	   if (bwx_supported)
	     alpha_ldwu(code, ins->dreg, ins->inst_basereg, ins->inst_offset);
	   else
	     {
	       alpha_ldq_u(code, alpha_r24, ins->inst_basereg,
			   ins->inst_offset);
	       alpha_ldq_u(code, alpha_r25, ins->inst_basereg,
			   (ins->inst_offset+1));
	       alpha_lda(code, alpha_at, ins->inst_basereg, ins->inst_offset);
	       alpha_extwl(code, alpha_r24, alpha_at, ins->dreg);
	       alpha_extwh(code, alpha_r25, alpha_at, alpha_r25);
	       alpha_bis(code, alpha_r25, ins->dreg, ins->dreg);
	     }
	   break;
	   
	 case OP_LOAD_MEMBASE:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [load_membase] dreg=%d, basereg=%d, offset=%0lx\n",
		  ins->dreg, ins->inst_basereg, ins->inst_offset);
	   alpha_ldq( code, ins->dreg, ins->inst_basereg, ins->inst_offset);
	   break;

         case OP_LOADI8_MEMBASE:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [loadi8_membase] dreg=%d, basereg=%d, offset=%0lx\n",
                  ins->dreg, ins->inst_basereg, ins->inst_offset);
           alpha_ldq( code, ins->dreg, ins->inst_basereg, ins->inst_offset);
           break;

	 case OP_LOADI4_MEMBASE:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [loadi4_membase] dreg=%d, basereg=%d, offset=%0lx\n",
		  ins->dreg, ins->inst_basereg, ins->inst_offset);
	   alpha_ldl( code, ins->dreg, ins->inst_basereg, ins->inst_offset);
	   break;
	   
	 case OP_LOADI1_MEMBASE:
	   // Load sign-extended byte from REGOFFSET
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [loadi1_membase] dreg=%d, basereg=%d, offset=%0lx\n",
		  ins->dreg, ins->inst_basereg, ins->inst_offset);
	   if (bwx_supported)
	     {
	       alpha_ldbu(code, ins->dreg, ins->inst_basereg,
			  ins->inst_offset);
	       alpha_sextb(code, ins->dreg, ins->dreg);
	     }
	   else
	     {
	       alpha_ldq_u(code, alpha_r25, ins->inst_basereg,
			   ins->inst_offset);
	       alpha_lda(code, alpha_at, ins->inst_basereg,
			 (ins->inst_offset+1));
	       alpha_extqh(code, alpha_r25, alpha_at, ins->dreg);
	       alpha_sra_(code, ins->dreg, 56, ins->dreg);
	     }
	   break;
	   
	 case OP_LOADI2_MEMBASE:
	   // Load sign-extended word from REGOFFSET
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [loadi2_membase] dreg=%d, basereg=%d, offset=%0lx\n",
		  ins->dreg, ins->inst_basereg, ins->inst_offset);
	   if (bwx_supported)
	     {
	       alpha_ldwu(code, ins->dreg, ins->inst_basereg,
			  ins->inst_offset);
	       alpha_sextw(code, ins->dreg, ins->dreg);
	     }
	   else
	     {
	       alpha_ldq_u(code, alpha_r24, ins->inst_basereg,
			   ins->inst_offset);
	       alpha_ldq_u(code, alpha_r25, ins->inst_basereg,
			   (ins->inst_offset+1));
	       alpha_lda(code, alpha_at, ins->inst_basereg,
			 (ins->inst_offset+2));
	       alpha_extql(code, alpha_r24, alpha_at, ins->dreg);
	       alpha_extqh(code, alpha_r25, alpha_at, alpha_r25);
	       alpha_bis(code, alpha_r25, ins->dreg, ins->dreg);
	       alpha_sra_(code, ins->dreg, 48, ins->dreg);
	     }
	   break;			 
	   
	 case OP_STOREI1_MEMBASE_IMM:
	   // Store signed byte at REGOFFSET
	   // Valid only for storing 0
	   // storei1_membase_reg will do the rest
	   
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [storei1_membase_imm(0)] const=%0lx, destbasereg=%d, offset=%0lx\n",
		  ins->inst_imm, ins->inst_destbasereg, ins->inst_offset);
	   g_assert(ins->inst_imm == 0);

	   if (bwx_supported)
		alpha_stb(code, alpha_zero, ins->inst_destbasereg,
			ins->inst_offset);
	   else
		g_assert_not_reached();

	   break;

	 case OP_STOREI1_MEMBASE_REG:
	   // Store byte at REGOFFSET
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [storei1_membase_reg] sreg1=%d, destbasereg=%d, offset=%0lx\n",
		  ins->sreg1, ins->inst_destbasereg, ins->inst_offset);
	   if (bwx_supported)
	     {
	       alpha_stb(code, ins->sreg1, ins->inst_destbasereg,
			 ins->inst_offset);
	     }
	   else
	     {
	       alpha_lda(code, alpha_at, ins->inst_destbasereg,
			 ins->inst_offset);
	       alpha_ldq_u(code, alpha_r25, ins->inst_destbasereg,
			   ins->inst_offset);
	       alpha_insbl(code, ins->sreg1, alpha_at, alpha_r24);
	       alpha_mskbl(code, alpha_r25, alpha_at, alpha_r25);
	       alpha_bis(code, alpha_r25, alpha_r24, alpha_r25);
	       alpha_stq_u(code, alpha_r25, ins->inst_destbasereg,
			   ins->inst_offset);
	     }
	   break;
	   
	 case OP_STOREI2_MEMBASE_IMM:
           // Store signed word at REGOFFSET
	   // Now work only for storing 0
           // For now storei2_membase_reg will do the work
	   
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [storei2_membase_imm(0)] const=%0lx, destbasereg=%d, offset=%0lx\n",
		  ins->inst_imm, ins->inst_destbasereg, ins->inst_offset);
	   
	   g_assert(ins->inst_imm == 0);
	   
	   if (bwx_supported)
	   	alpha_stw(code, alpha_zero, ins->inst_destbasereg,
			ins->inst_offset);
	   else
		g_assert_not_reached();

	   break;
	   
	 case OP_STOREI2_MEMBASE_REG:
	   // Store signed word from reg to REGOFFSET
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [storei2_membase_reg] sreg1=%d, destbasereg=%d, offset=%0lx\n",
                  ins->sreg1, ins->inst_destbasereg, ins->inst_offset);
	   
	   if (bwx_supported)
	     {
	       alpha_stw(code, ins->sreg1, ins->inst_destbasereg,
			 ins->inst_offset);
	     }
	   else
	     {
	       alpha_lda(code, alpha_at, ins->inst_destbasereg,
			 ins->inst_offset);
	       alpha_ldq_u(code, alpha_r25, ins->inst_destbasereg,
			   (ins->inst_offset+1));
	       alpha_ldq_u(code, alpha_r24, ins->inst_destbasereg,
			   ins->inst_offset);
	       alpha_inswh(code, ins->sreg1, alpha_at, alpha_r23);
	       alpha_inswl(code, ins->sreg1, alpha_at, alpha_r22);
	       alpha_mskwh(code, alpha_r25, alpha_at, alpha_r25);
	       alpha_mskwl(code, alpha_r24, alpha_at, alpha_r24);
	       alpha_bis(code, alpha_r25, alpha_r23, alpha_r25);
	       alpha_bis(code, alpha_r24, alpha_r22, alpha_r24);
	       alpha_stq_u(code, alpha_r25, ins->inst_destbasereg,
			   (ins->inst_offset+1));
	       alpha_stq_u(code, alpha_r24, ins->inst_destbasereg,
			   ins->inst_offset);
	     }

	   break;
	   
	 case OP_STOREI4_MEMBASE_IMM:
	   // We will get here only with ins->inst_imm = 0
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [storei4_membase_imm(0)] const=%0lx, destbasereg=%d, offset=%0lx\n",
		  ins->inst_imm, ins->inst_destbasereg, ins->inst_offset);
	   
	   g_assert(ins->inst_imm == 0);
	   
	   alpha_stl(code, alpha_zero,
		     ins->inst_destbasereg, ins->inst_offset);
	   break;

	 case OP_STORER4_MEMBASE_REG:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [storer4_membase_reg] sreg1=%d, destbasereg=%d, offset=%0lX\n",
		  ins->sreg1, ins->inst_destbasereg, ins->inst_offset);
	   alpha_sts(code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset);
	   break;

         case OP_STORER8_MEMBASE_REG:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [storer8_membase_reg] sreg1=%d, destbasereg=%d, offset=%0lX\n",
                  ins->sreg1, ins->inst_destbasereg, ins->inst_offset);
           alpha_stt(code, ins->sreg1, ins->inst_destbasereg,
		     ins->inst_offset);
           break;
	  
	 case OP_LOADR4_MEMBASE:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [loadr4_membase] dreg=%d basereg=%d offset=%0lX\n",
		  ins->dreg, ins->inst_basereg, ins->inst_offset);
	   alpha_lds(code, ins->dreg, ins->inst_basereg, ins->inst_offset);
	   break;

         case OP_LOADR8_MEMBASE:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [loadr8_membase] dreg=%d basereg=%d offset=%0lX\n",
                  ins->dreg, ins->inst_basereg, ins->inst_offset);
           alpha_ldt(code, ins->dreg, ins->inst_basereg, ins->inst_offset);
           break;

	 case OP_FMOVE:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [fmove] sreg1=%d, dreg=%d\n",
		  ins->sreg1, ins->dreg);
	   alpha_cpys(code, ins->sreg1, ins->sreg1, ins->dreg);
	   break;

	 case OP_FADD:
	   // Later check different rounding and exc modes
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [float_add] sreg1=%d, sreg2=%d, dreg=%d\n",
		  ins->sreg1, ins->sreg2, ins->dreg);
	   alpha_addt_su(code, ins->sreg1, ins->sreg2, ins->dreg);
	   alpha_trapb(code);
	   break;

	 case OP_FSUB:
	   // Later check different rounding and exc modes
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [float_sub] sreg1=%d, sreg2=%d, dreg=%d\n",
				 ins->sreg1, ins->sreg2, ins->dreg);
	   alpha_subt_su(code, ins->sreg1, ins->sreg2, ins->dreg);
	   alpha_trapb(code);
           break;

	 case OP_FMUL:
	   // Later check different rounding and exc modes
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [float_sub] sreg1=%d, sreg2=%d, dreg=%d\n",
				 ins->sreg1, ins->sreg2, ins->dreg);
	   alpha_mult(code, ins->sreg1, ins->sreg2, ins->dreg);
	   break;

	 case OP_FNEG:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [float_neg] sreg1=%d, dreg=%d\n",
				 ins->sreg1, ins->dreg);
	   alpha_cpysn(code, ins->sreg1, ins->sreg1, ins->dreg);
	   break;

	 case OP_ALPHA_TRAPB:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [alpha_trapb]\n");
	   alpha_trapb(code);
	   break;

	 case OP_ABS:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [float_abs] sreg1=%d, dreg=%d\n",
				ins->sreg1, ins->dreg);
	   alpha_cpys(code, alpha_f31, ins->sreg1, ins->dreg);
           break;

	 case OP_STORE_MEMBASE_IMM:
	 case OP_STOREI8_MEMBASE_IMM:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [store_membase_imm/storei8_membase_imm] const=%0lx, destbasereg=%d, offset=%0lx\n",
		  ins->inst_imm, ins->inst_destbasereg, ins->inst_offset);
	   g_assert(ins->inst_imm == 0);

	   alpha_stq(code, alpha_zero,
		     ins->inst_destbasereg, ins->inst_offset); 

	   break;
	 case OP_STORE_MEMBASE_REG:
	 case OP_STOREI8_MEMBASE_REG:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [store_membase_reg/storei8_membase_reg] sreg1=%d, destbasereg=%d, offset=%0lx\n",
		  ins->sreg1, ins->inst_destbasereg, ins->inst_offset);
	   alpha_stq( code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset);
	   break;
	   
	 case OP_STOREI4_MEMBASE_REG:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [storei4_membase_reg] sreg1=%d, destbasereg=%d, offset=%0lx\n",
		  ins->sreg1, ins->inst_destbasereg, ins->inst_offset);
	   alpha_stl( code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset);
	   break;
	   
	 case OP_ICOMPARE_IMM:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [icompare_imm] sreg1=%d, dreg=%d, const=%0lX\n",
		  ins->sreg1, ins->dreg, ins->inst_imm);

	   g_assert_not_reached();
	   
	   break;
	   
	 case OP_COMPARE_IMM:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [compare_imm] sreg1=%d, dreg=%d, const=%0lX\n",
		  ins->sreg1, ins->dreg, ins->inst_imm);
	   
	   g_assert_not_reached();

	   break;
	   
	 case OP_COMPARE:  // compare two 32 bit regs
	 case OP_LCOMPARE: // compare two 64 bit regs
	 case OP_FCOMPARE: // compare two floats
	   CFG_DEBUG(4) g_print("ALPHA_FIX: [compare/lcompare/fcompare] sreg1=%d, sreg2=%d, dreg=%d\n",
		  ins->sreg1, ins->sreg2, ins->dreg);

	   g_assert_not_reached();
	   
	   break;

         case OP_ALPHA_CMPT_UN:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [alpha_cmpt_un] sreg1=%d, sreg2=%d, dreg=%d\n",
				ins->sreg1, ins->sreg2, ins->dreg);
           alpha_cmptun(code, ins->sreg1, ins->sreg2, (alpha_at+1));
           break;

         case OP_ALPHA_CMPT_UN_SU:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [alpha_cmpt_un_su] sreg1=%d, sreg2=%d, dreg=%d\n",
                                ins->sreg1, ins->sreg2, ins->dreg);
           alpha_cmptun_su(code, ins->sreg1, ins->sreg2, (alpha_at+1));
           break;

	 case OP_ALPHA_CMPT_EQ:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [alpha_cmpt_eq] sreg1=%d, sreg2=%d, dreg=%d\n",
				 ins->sreg1, ins->sreg2, ins->dreg);
	   alpha_cmpteq(code, ins->sreg1, ins->sreg2, alpha_at);
	   break;

         case OP_ALPHA_CMPT_EQ_SU:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [alpha_cmpt_eq_su] sreg1=%d, sreg2=%d, dreg=%d\n",
				ins->sreg1, ins->sreg2, ins->dreg);
           alpha_cmpteq_su(code, ins->sreg1, ins->sreg2, alpha_at);
           break;


         case OP_ALPHA_CMPT_LT:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [alpha_cmpt_lt] sreg1=%d, sreg2=%d, dreg=%d\n",
				ins->sreg1, ins->sreg2, ins->dreg);
           alpha_cmptlt(code, ins->sreg1, ins->sreg2, alpha_at);
           break;

         case OP_ALPHA_CMPT_LT_SU:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [alpha_cmpt_lt_su] sreg1=%d, sreg2=%d, dreg=%d\n",
                                ins->sreg1, ins->sreg2, ins->dreg);
           alpha_cmptlt_su(code, ins->sreg1, ins->sreg2, alpha_at);
           break;

         case OP_ALPHA_CMPT_LE:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [alpha_cmpt_le] sreg1=%d, sreg2=%d, dreg=%d\n",
                                ins->sreg1, ins->sreg2, ins->dreg);
           alpha_cmptle(code, ins->sreg1, ins->sreg2, alpha_at);
           break;

         case OP_ALPHA_CMPT_LE_SU:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [alpha_cmpt_le_su] sreg1=%d, sreg2=%d, dreg=%d\n",
                                ins->sreg1, ins->sreg2, ins->dreg);
           alpha_cmptle_su(code, ins->sreg1, ins->sreg2, alpha_at);
           break;

	 case OP_ALPHA_CMP_EQ:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [alpha_cmp_eq] sreg1=%d, sreg2=%d, dreg=%d\n",
		  ins->sreg1, ins->sreg2, ins->dreg);
	   alpha_cmpeq(code, ins->sreg1, ins->sreg2, alpha_at);
	   break;
	   
         case OP_ALPHA_CMP_IMM_EQ:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [alpha_cmp_imm_eq] sreg1=%d, const=%0lX, dreg=%d\n",
                  ins->sreg1, ins->inst_imm, ins->dreg);
           alpha_cmpeq_(code, ins->sreg1, ins->inst_imm, alpha_at);
           break;

         case OP_ALPHA_CMP_IMM_ULE:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [alpha_cmp_imm_ule] sreg1=%d, const=%0lX, dreg=%d\n",
                  ins->sreg1, ins->inst_imm, ins->dreg);
           alpha_cmpule_(code, ins->sreg1, ins->inst_imm, alpha_at);
           break;

         case OP_ALPHA_CMP_ULT:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [alpha_cmp_ult] sreg1=%d, sreg2=%d, dreg=%d\n",
                  ins->sreg1, ins->sreg2, ins->dreg);
           alpha_cmpult(code, ins->sreg1, ins->sreg2, alpha_at);
           break;

         case OP_ALPHA_CMP_IMM_ULT:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [alpha_cmp_imm_ult] sreg1=%d, const=%0lX, dreg=%d\n",
                  ins->sreg1, ins->inst_imm, ins->dreg);
           alpha_cmpult_(code, ins->sreg1, ins->inst_imm, alpha_at);
           break;

         case OP_ALPHA_CMP_LE:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [alpha_cmp_le] sreg1=%d, sreg2=%d, dreg=%d\n",
                  ins->sreg1, ins->sreg2, ins->dreg);
           alpha_cmple(code, ins->sreg1, ins->sreg2, alpha_at);
           break;

         case OP_ALPHA_CMP_ULE:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [alpha_cmp_ule] sreg1=%d, sreg2=%d, dreg=%d\n",
				ins->sreg1, ins->sreg2, ins->dreg);
           alpha_cmpule(code, ins->sreg1, ins->sreg2, alpha_at);
           break;


         case OP_ALPHA_CMP_IMM_LE:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [alpha_cmp_imm_le] sreg1=%d, const=%0lX, dreg=%d\n",
                  ins->sreg1, ins->inst_imm, ins->dreg);
           alpha_cmple_(code, ins->sreg1, ins->inst_imm, alpha_at);
           break;

         case OP_ALPHA_CMP_LT:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [alpha_cmp_lt] sreg1=%d, sreg2=%d, dreg=%d\n",
                  ins->sreg1, ins->sreg2, ins->dreg);
           alpha_cmplt(code, ins->sreg1, ins->sreg2, alpha_at);
           break;

         case OP_ALPHA_CMP_IMM_LT:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [alpha_cmp_imm_lt] sreg1=%d, const=%0lX, dreg=%d\n",
                  ins->sreg1, ins->inst_imm, ins->dreg);
           alpha_cmplt_(code, ins->sreg1, ins->inst_imm, alpha_at);
           break;

	 case OP_COND_EXC_GT:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [op_cond_exc_gt] (cmple + beq) Exc: %s\n",
				(char *)ins->inst_p1);

	   EMIT_COND_EXC_BRANCH(beq, alpha_at, ins->inst_p1);
           break;

	 case OP_COND_EXC_GT_UN:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [op_cond_exc_gt_un] (cmpule + beq) Exc: %s\n",
				(char *)ins->inst_p1);

	   EMIT_COND_EXC_BRANCH(beq, alpha_at, ins->inst_p1);
	   break;

	 case OP_COND_EXC_LT:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [op_cond_exc_lt] (cmplt + bne) Exc: %s\n",
				(char *)ins->inst_p1);

	   EMIT_COND_EXC_BRANCH(bne, alpha_at, ins->inst_p1);
	   break;

         case OP_COND_EXC_LT_UN:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [op_cond_exc_lt_un] (cmpult + bne) Exc: %s\n",
                                (char *)ins->inst_p1);

           EMIT_COND_EXC_BRANCH(bne, alpha_at, ins->inst_p1);
           break;


	 case OP_COND_EXC_LE_UN:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [op_cond_exc_le_un] (cmpule + bne) Exc: %s\n",
				(char *)ins->inst_p1);
	   EMIT_COND_EXC_BRANCH(bne, alpha_at, ins->inst_p1);
	   break;

	 case OP_COND_EXC_NE_UN:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [op_cond_exc_ne_un] (cmpeq + beq) Exc: %s\n",
				(char *)ins->inst_p1);
	   EMIT_COND_EXC_BRANCH(beq, alpha_at, ins->inst_p1);
	   break;

	 case OP_COND_EXC_EQ:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [op_cond_exc_eq] (cmpeq + bne) Exc: %s\n",
                                (char *)ins->inst_p1);
           EMIT_COND_EXC_BRANCH(bne, alpha_at, ins->inst_p1);
           break;

	 case OP_COND_EXC_IOV:
	 case OP_COND_EXC_OV:
	   EMIT_COND_EXC_BRANCH(bne, alpha_at, "OverflowException");
	   break;

	 case OP_COND_EXC_IC:
	 case OP_COND_EXC_C:
	   EMIT_COND_EXC_BRANCH(bne, alpha_pv, "OverflowException");
	   break;

	 case CEE_CONV_OVF_U4:
	   // Convert unsigned 32 bit value to 64 bit reg
	   // Check overflow
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [conv_ovf_u4] sreg=%d, dreg=%d\n",
				ins->sreg1, ins->dreg);
	   alpha_cmplt_(code, ins->sreg1, 0, alpha_at);
	   EMIT_COND_EXC_BRANCH(bne, alpha_at, "OverflowException");
	   alpha_mov1(code, ins->sreg1, ins->dreg);
	   break;

         case CEE_CONV_OVF_I4_UN:
           // Convert unsigned 32 bit value to 64 bit reg
           // Check overflow
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [conv_ovf_i4_un] sreg=%d, dreg=%d\n",
                                ins->sreg1, ins->dreg);
   	   alpha_zap_(code, ins->sreg1, 0x0F, alpha_at);

           EMIT_COND_EXC_BRANCH(bne, alpha_at, "OverflowException");
           alpha_mov1(code, ins->sreg1, ins->dreg);
           break;

	 case CEE_CONV_I1:
	   // Move I1 (byte) to dreg(64 bits) and sign extend it
	   // Read about sextb
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [conv_i1] sreg=%d, dreg=%d\n",
		  ins->sreg1, ins->dreg);
	   if (bwx_supported)
	     alpha_sextb(code, ins->sreg1, ins->dreg);
	   else
	     {
	       alpha_sll_(code, ins->sreg1, 24, alpha_at);
	       alpha_addl(code, alpha_at, alpha_zero, ins->dreg);
	       alpha_sra_(code, ins->dreg, 24, ins->dreg);
	     }
	   break;

	 case CEE_CONV_I2:
	   // Move I2 (word) to dreg(64 bits) and sign extend it
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [conv_i2] sreg=%d, dreg=%d\n",
		  ins->sreg1, ins->dreg);
	   if (bwx_supported)
	     alpha_sextw(code, ins->sreg1, ins->dreg);
	   else
	     {
	       alpha_sll_(code, ins->sreg1, 16, alpha_at);
	       alpha_addl(code, alpha_at, alpha_zero, ins->dreg);
	       alpha_sra_(code, ins->dreg, 16, ins->dreg);
	     }
	   break;
	   
	 case CEE_CONV_I4:
	   // Move I4 (long) to dreg(64 bits) and sign extend it
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [conv_i4] sreg=%d, dreg=%d\n",
		  ins->sreg1, ins->dreg);
	   alpha_addl(code, ins->sreg1, alpha_zero, ins->dreg);
	   break;

	 case CEE_CONV_I8:
	 case CEE_CONV_I:
	   // Convert I/I8 (64 bit) to dreg (64 bit) and sign extend it
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [conv_i8/conv_i] sreg=%d, dreg=%d\n",
		  ins->sreg1, ins->dreg);
	   //alpha_addl(code, ins->sreg1, alpha_zero, ins->dreg);
	   alpha_mov1(code, ins->sreg1, ins->dreg);
	   break;
	   
	 case CEE_CONV_U1:
	   // Move U1 (byte) to dreg(64 bits) don't sign extend it
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [conv_u1] sreg=%d, dreg=%d\n",
		  ins->sreg1, ins->dreg);
	   alpha_extbl_(code, ins->sreg1, 0, ins->dreg);
	   break;
	   
	 case CEE_CONV_U2:
	   // Move U2 (word) to dreg(64 bits) don't sign extend it
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [conv_u2] sreg=%d, dreg=%d\n",
		  ins->sreg1, ins->dreg);
	   alpha_extwl_(code, ins->sreg1, 0, ins->dreg);
	   break;
	   
	 case CEE_CONV_U4:
	   // Move U4 (long) to dreg(64 bits) don't sign extend it
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [conv_u4] sreg=%d, dreg=%d\n",
		  ins->sreg1, ins->dreg);
	   alpha_extll_(code, ins->sreg1, 0, ins->dreg);
	   break;
	   
         case CEE_CONV_U8:
	 case CEE_CONV_U:
           // Move U4 (long) to dreg(64 bits) don't sign extend it
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [conv_u8/conv_u] sreg=%d, dreg=%d\n",
                  ins->sreg1, ins->dreg);
           alpha_extll_(code, ins->sreg1, 0, ins->dreg);
           break;

	 case OP_FCONV_TO_I4:
	 case OP_FCONV_TO_I8:
	   // Move float to 32 bit reg
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [fconv_to_i4/fconv_to_i8] sreg=%d, dreg=%d\n",
				 ins->sreg1, ins->dreg);
	   //alpha_ftoit(code, ins->sreg1, ins->dreg); - 21264/EV6
	   alpha_cvttq_c(code, ins->sreg1, ins->sreg1);
	   alpha_lda(code, alpha_sp, alpha_sp, -8);
	   alpha_stt(code, ins->sreg1, alpha_sp, 0);
	   alpha_ldq(code, ins->dreg, alpha_sp, 0);
	   alpha_lda(code, alpha_sp, alpha_sp, 8);
	   break;

	 case OP_FCONV_TO_I2:
	   // Move float to 16 bit reg
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [fconv_to_i2] sreg=%d, dreg=%d\n",
				ins->sreg1, ins->dreg);
           //alpha_ftoit(code, ins->sreg1, ins->dreg); - 21264/EV6
           alpha_cvttq_c(code, ins->sreg1, ins->sreg1);
           alpha_lda(code, alpha_sp, alpha_sp, -8);
           alpha_stt(code, ins->sreg1, alpha_sp, 0);
           alpha_ldq(code, ins->dreg, alpha_sp, 0);
           alpha_lda(code, alpha_sp, alpha_sp, 8);
	   alpha_sll_(code, ins->dreg, 48, ins->dreg);
	   alpha_sra_(code, ins->dreg, 48, ins->dreg);
           break;

	 case OP_FCONV_TO_U2:
           // Move float to 16 bit reg as unsigned 
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [fconv_to_u2] sreg=%d, dreg=%d\n",
                                ins->sreg1, ins->dreg);
           //alpha_ftoit(code, ins->sreg1, ins->dreg); - 21264/EV6
           alpha_cvttq_c(code, ins->sreg1, ins->sreg1);
           alpha_lda(code, alpha_sp, alpha_sp, -8);
           alpha_stt(code, ins->sreg1, alpha_sp, 0);
           alpha_ldq(code, ins->dreg, alpha_sp, 0);
           alpha_lda(code, alpha_sp, alpha_sp, 8);
           alpha_zapnot_(code, ins->dreg, 3, ins->dreg);
	   break;

         case OP_FCONV_TO_U1:
           // Move float to 8 bit reg as unsigned
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [fconv_to_u1] sreg=%d, dreg=%d\n",
                                ins->sreg1, ins->dreg);
           //alpha_ftoit(code, ins->sreg1, ins->dreg); - 21264/EV6
           alpha_cvttq_c(code, ins->sreg1, ins->sreg1);
           alpha_lda(code, alpha_sp, alpha_sp, -8);
           alpha_stt(code, ins->sreg1, alpha_sp, 0);
           alpha_ldq(code, ins->dreg, alpha_sp, 0);
           alpha_lda(code, alpha_sp, alpha_sp, 8);
           alpha_and_(code, ins->dreg, 0xff, ins->dreg);
           break;

         case OP_FCONV_TO_I1:
           // Move float to 8 bit reg
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [fconv_to_i1] sreg=%d, dreg=%d\n",
                                ins->sreg1, ins->dreg);
           //alpha_ftoit(code, ins->sreg1, ins->dreg); - 21264/EV6
           alpha_cvttq_c(code, ins->sreg1, ins->sreg1);
           alpha_lda(code, alpha_sp, alpha_sp, -8);
           alpha_stt(code, ins->sreg1, alpha_sp, 0);
           alpha_ldq(code, ins->dreg, alpha_sp, 0);
           alpha_lda(code, alpha_sp, alpha_sp, 8);
           alpha_sll_(code, ins->dreg, 56, ins->dreg);
           alpha_sra_(code, ins->dreg, 56, ins->dreg);
           break;

	 case CEE_CONV_R4:
	 case OP_LCONV_TO_R4:
	   // Move 32/64 bit int into float
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [conv_r4/lconv_r4] sreg=%d, dreg=%d\n",
				 ins->sreg1, ins->dreg);
	   alpha_lda(code, alpha_sp, alpha_sp, -8);
	   alpha_stq(code, ins->sreg1, alpha_sp, 0);
	   alpha_ldt(code, ins->dreg, alpha_sp, 0);
	   alpha_lda(code, alpha_sp, alpha_sp, 8);
	   alpha_cvtqs(code, ins->dreg, ins->dreg);
	   alpha_trapb(code);
	   break;

         case CEE_CONV_R8:
	 case OP_LCONV_TO_R8:
           // Move 32/64 bit int into double
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [conv_r8/lconv_r8] sreg=%d, dreg=%d\n",
                                 ins->sreg1, ins->dreg);
           alpha_lda(code, alpha_sp, alpha_sp, -8);
           alpha_stq(code, ins->sreg1, alpha_sp, 0);
           alpha_ldt(code, ins->dreg, alpha_sp, 0);
           alpha_lda(code, alpha_sp, alpha_sp, 8);
           alpha_cvtqt(code, ins->dreg, ins->dreg);
	   alpha_trapb(code);
           break;

	 case OP_FCONV_TO_R4:
	   // Convert 64 bit float to 32 bit float (T -> S)
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [fconv_r4] sreg=%d, dreg=%d\n",
				 ins->sreg1, ins->dreg);
	   alpha_cvtts_su(code, ins->sreg1, ins->dreg);
	   alpha_trapb(code);
	   break;

	 case OP_LOCALLOC:
	   // Allocate sreg1 bytes on stack, round bytes by 8,
	   // modify SP, set dreg to end of current stack frame
	   // top of stack is used for call params
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [localloc] sreg=%d, dreg=%d\n",
				ins->sreg1, ins->dreg);

	   alpha_addq_(code, ins->sreg1, (MONO_ARCH_LOCALLOC_ALIGNMENT - 1), ins->sreg1);
	   alpha_bic_(code, ins->sreg1, (MONO_ARCH_LOCALLOC_ALIGNMENT - 1), ins->sreg1);
	   if (ins->flags & MONO_INST_INIT)
	       alpha_mov1(code, ins->sreg1, ins->sreg2);

	   alpha_subq(code, alpha_sp, ins->sreg1, alpha_sp);
	   if (cfg->arch.params_stack_size > 0)
	   {
	       alpha_lda(code, ins->dreg, alpha_zero,
			(cfg->arch.params_stack_size));
	       alpha_addq(code, alpha_sp, ins->dreg, ins->dreg);
	   }
	   else
	       alpha_mov1(code, alpha_sp, ins->dreg);

	   if (ins->flags & MONO_INST_INIT)
	   {
		// TODO: Optimize it later
		alpha_lda(code, ins->sreg2, ins->sreg2,
			-(MONO_ARCH_LOCALLOC_ALIGNMENT));
		alpha_blt(code, ins->sreg2, 3);
		alpha_addq(code, ins->sreg2, ins->dreg, alpha_at);
		alpha_stq(code, alpha_zero, alpha_at, 0);
		alpha_br(code, alpha_zero, -5);
	   }

	   break;

	 case OP_MOVE:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [move] sreg=%d, dreg=%d\n",
		  ins->sreg1, ins->dreg);
	   alpha_mov1(code, ins->sreg1, ins->dreg);
	   break;
	   
	 case OP_CGT_UN:
	 case OP_ICGT_UN:
	 case OP_ICGT:
	 case OP_CGT:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [cgt/cgt_un/icgt_un/int_cgt] dreg=%d\n",
		  ins->dreg);
	   alpha_clr(code, ins->dreg);
	   alpha_cmoveq_(code, alpha_at, 1, ins->dreg);
	   break;

	 case OP_ICLT:
	 case OP_ICLT_UN:
	 case OP_CLT:
	 case OP_CLT_UN:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [int_clt/int_clt_un/clt/clt_un] dreg=%d\n",
                  ins->dreg);
           alpha_clr(code, ins->dreg);
           alpha_cmovne_(code, alpha_at, 1, ins->dreg);
           break;

	 case OP_ICEQ:
	 case OP_CEQ:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [iceq/ceq] dreg=%d\n",
		  ins->dreg);
	   alpha_clr(code, ins->dreg);
	   alpha_cmovne_(code, alpha_at, 1, ins->dreg);
	   break;

	 case OP_FCEQ:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [fceq] dreg=%d\n",
				ins->dreg);
           alpha_clr(code, ins->dreg);
	   alpha_fbeq(code, alpha_at, 1);
	   alpha_lda(code, ins->dreg, alpha_zero, 1);

	   /*
	   alpha_cvttq_c(code, alpha_at, alpha_at);
           alpha_lda(code, alpha_sp, alpha_sp, -8);
           alpha_stt(code, alpha_at, alpha_sp, 0);
           alpha_ldq(code, alpha_at, alpha_sp, 0);
           alpha_lda(code, alpha_sp, alpha_sp, 8);

           alpha_cmovne_(code, alpha_at, 1, ins->dreg);
	   */
           break;

         case OP_FCGT:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [fcgt] dreg=%d\n",
                                ins->dreg);
           alpha_clr(code, ins->dreg);
           alpha_fbne(code, alpha_at, 1);
           alpha_lda(code, ins->dreg, alpha_zero, 1);

	   /*
           alpha_cvttq_c(code, alpha_at, alpha_at);
           alpha_lda(code, alpha_sp, alpha_sp, -8);
           alpha_stt(code, alpha_at, alpha_sp, 0);
           alpha_ldq(code, alpha_at, alpha_sp, 0);
           alpha_lda(code, alpha_sp, alpha_sp, 8);

           alpha_cmoveq_(code, alpha_at, 1, ins->dreg);
	   */
           break;


         case OP_FCLT:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [fclt] dreg=%d\n",
                                ins->dreg);
           alpha_clr(code, ins->dreg);
           alpha_fbeq(code, alpha_at, 1);
           alpha_lda(code, ins->dreg, alpha_zero, 1);
	   break;

         case OP_FCLT_UN:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [fclt_un] dreg=%d\n",
                                ins->dreg);
 
           alpha_clr(code, ins->dreg);
	   alpha_fbne(code, (alpha_at+1), 1);
           alpha_fbeq(code, alpha_at, 1);
           alpha_lda(code, ins->dreg, alpha_zero, 1);
           break;


	 case OP_IBNE_UN:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [ibne_un] [");
	   EMIT_ALPHA_BRANCH(ins, alpha_at, bne);
	   break;

	 case OP_FBNE_UN:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [fbne_un] [");
	   alpha_fbeq(code, (alpha_at+1), 1);
	   alpha_cpys(code, alpha_zero, alpha_zero, alpha_at);
	   EMIT_ALPHA_BRANCH(ins, alpha_at, fbeq);
	   break;

         case OP_FBGE_UN:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [fbge_un] [");
           alpha_fbeq(code, (alpha_at+1), 1);
           alpha_cpys(code, alpha_zero, alpha_zero, alpha_at);
           EMIT_ALPHA_BRANCH(ins, alpha_at, fbeq);
           break;

	 case OP_FBGE:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [fbge] [");
	   alpha_fbne(code, (alpha_at+1), 1);
	   EMIT_ALPHA_BRANCH(ins, alpha_at, fbeq);
           break;

         case OP_FBLE_UN:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [fble_un] [");
           alpha_fbeq(code, (alpha_at+1), 1);
           alpha_cpys(code, (alpha_at+1), (alpha_at+1), alpha_at);
           EMIT_ALPHA_BRANCH(ins, alpha_at, fbne);
           break;

         case OP_FBLE:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [fble] [");
           alpha_fbne(code, (alpha_at+1), 1);
           EMIT_ALPHA_BRANCH(ins, alpha_at, fbne);
           break;

         case OP_FBLT_UN:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [fblt_un] [");
           alpha_fbeq(code, (alpha_at+1), 1);
           alpha_cpys(code, (alpha_at+1), (alpha_at+1), alpha_at);
           EMIT_ALPHA_BRANCH(ins, alpha_at, fbne);
           break;

         case OP_FBLT:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [fblt] [");
           alpha_fbne(code, (alpha_at+1), 1);
           EMIT_ALPHA_BRANCH(ins, alpha_at, fbne);
           break;

         case OP_FBGT_UN:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [fbgt_un] [");
           alpha_fbeq(code, (alpha_at+1), 1);
           alpha_cpys(code, alpha_zero, alpha_zero, alpha_at);
           EMIT_ALPHA_BRANCH(ins, alpha_at, fbeq);
           break;

         case OP_FBGT:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [fbgt] [");
           alpha_fbne(code, (alpha_at+1), 1);
           EMIT_ALPHA_BRANCH(ins, alpha_at, fbeq);
           break;

	 case OP_IBEQ:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [ibeq] [");
	   EMIT_ALPHA_BRANCH(ins, alpha_at, beq);
	   break;

         case OP_FBEQ:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [fbeq] [");
           EMIT_ALPHA_BRANCH(ins, alpha_at, fbne);
           break;
	   
	 case CEE_BEQ:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [beq] [");
	   EMIT_ALPHA_BRANCH(ins, alpha_at, beq);
	   break;
	   
	 case CEE_BNE_UN:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [bne_un] [");
	   EMIT_ALPHA_BRANCH(ins, alpha_at, bne);
	   break;
	   
	 case OP_LABEL:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [label]\n");
	   ins->inst_c0 = (char *)code - (char *)cfg->native_code;
	   break;
	   
	 case OP_BR:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [br] target: %p, next: %p, curr: %p, last: %p [",
		  ins->inst_target_bb, bb->next_bb, ins, bb->last_ins);
	   
	   if (ins->inst_target_bb->native_offset)
	     {
	       // Somehow native offset is offset from
	       // start of the code. So convert it to
	       // offset branch
	       long br_offset = (char *)cfg->native_code +
		 ins->inst_target_bb->native_offset - 4 - (char *)code;

	       CFG_DEBUG(4) g_print("jump to: native_offset: %0X, address %p]\n",
		      ins->inst_target_bb->native_offset,
		      cfg->native_code +
		      ins->inst_target_bb->native_offset);
	       alpha_br(code, alpha_zero, br_offset/4);
	     }
	   else
	     {
	       CFG_DEBUG(4) g_print("add patch info: MONO_PATCH_INFO_BB offset: %0X, target_bb: %p]\n",
		      offset, ins->inst_target_bb);

	       mono_add_patch_info (cfg, offset,
				    MONO_PATCH_INFO_BB,
				    ins->inst_target_bb);
	       alpha_br(code, alpha_zero, 0);
	     }
	   
	   break;

	 case OP_BR_REG:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [br_reg] sreg1=%d\n",
		  ins->sreg1);

	   alpha_jmp(code, alpha_zero, ins->sreg1, 0);
	   break;
	   
	 case OP_FCALL:
	 case OP_LCALL:
	 case OP_VCALL:
	 case OP_VOIDCALL:
	 case OP_CALL:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [fcall/lcall/vcall/voidcall/call] Target: [");
	   call = (MonoCallInst*)ins;
	   
	   if (ins->flags & MONO_INST_HAS_METHOD)
	     {
	       CFG_DEBUG(4) g_print("MONO_PATCH_INFO_METHOD] %p\n", call->method);
	       code = emit_call (cfg, code,
				 MONO_PATCH_INFO_METHOD, call->method);
	     }
	   else
	     {
	       CFG_DEBUG(4) g_print("MONO_PATCH_INFO_ABS] %p\n", call->fptr);
	       code = emit_call (cfg, code,
				 MONO_PATCH_INFO_ABS, call->fptr);
	     }

	   //code = emit_move_return_value (cfg, ins, code);

	   break;
	   
	 case OP_FCALL_REG:
	 case OP_LCALL_REG:
	 case OP_VCALL_REG:
	 case OP_VOIDCALL_REG:
	 case OP_CALL_REG:
	   {
	     int offset;

	     CFG_DEBUG(4) g_print("ALPHA_CHECK: [fcall_reg/lcall_reg/vcall_reg/voidcall_reg/call_reg]: TargetReg: %d\n", ins->sreg1);
	     call = (MonoCallInst*)ins;
	   
	     alpha_mov1(code, ins->sreg1, alpha_pv);

	     alpha_jsr(code, alpha_ra, alpha_pv, 0);

	     offset = (char *)code - (char *)cfg->native_code;

             // Restore GP
	     ALPHA_LOAD_GP(offset)
             alpha_ldah(code, alpha_gp, alpha_ra, 0);
             alpha_lda(code, alpha_gp, alpha_gp, 0);
	   }
	   break;

	 case OP_FCALL_MEMBASE:
	 case OP_CALL_MEMBASE:
	 case OP_LCALL_MEMBASE:
	 case OP_VCALL_MEMBASE:
	   {
	     int offset;

	     CFG_DEBUG(4) g_print("ALPHA_CHECK: [(lvf)call_membase] basereg=%d, offset=%0lx\n",
		    ins->inst_basereg, ins->inst_offset);
             call = (MonoCallInst*)ins;

	     alpha_ldq(code, alpha_pv, ins->inst_basereg, ins->inst_offset);
             alpha_jsr(code, alpha_ra, alpha_pv, 0);

             offset = (char *)code - (char *)cfg->native_code;
           
             // Restore GP
	     ALPHA_LOAD_GP(offset)
             alpha_ldah(code, alpha_gp, alpha_ra, 0);
             alpha_lda(code, alpha_gp, alpha_gp, 0);
	   }
	   break;

         case OP_VOIDCALL_MEMBASE:
           {
             int offset;

             CFG_DEBUG(4) g_print("ALPHA_CHECK: [voidcall_membase] basereg=%d, offset=%0lx\n",
                    ins->inst_basereg, ins->inst_offset);
             call = (MonoCallInst*)ins;

             alpha_ldq(code, alpha_pv, ins->inst_basereg, ins->inst_offset);
             alpha_jsr(code, alpha_ra, alpha_pv, 0);

             offset = (char *)code - (char *)cfg->native_code;

	     // Restore GP
	     ALPHA_LOAD_GP(offset)
	     alpha_ldah(code, alpha_gp, alpha_ra, 0);
	     alpha_lda(code, alpha_gp, alpha_gp, 0);
           }
           break;

	 case OP_START_HANDLER:
	   {
	     // TODO - find out when we called by call_handler or resume_context
	     // of by call_filter. There should be difference. For now just
	     // handle - call_handler

	     CFG_DEBUG(4) g_print("ALPHA_CHECK: [start_handler] basereg=%d, offset=%0lx\n",
		ins->inst_left->inst_basereg, ins->inst_left->inst_offset);

	     alpha_stq(code, alpha_ra, ins->inst_left->inst_basereg, 
			ins->inst_left->inst_offset);
	   }
	   break;

	 case OP_ENDFINALLY:
	   {
             // Keep in sync with start_handler
             CFG_DEBUG(4) g_print("ALPHA_CHECK: [endfinally] basereg=%d, offset=%0lx\n",
                ins->inst_left->inst_basereg, ins->inst_left->inst_offset);

             alpha_ldq(code, alpha_ra, ins->inst_left->inst_basereg,
                        ins->inst_left->inst_offset);

             alpha_ret(code, alpha_ra, 1);

	   }
	   break;
	 case OP_ENDFILTER:
	   {
	     // Keep in sync with start_handler
	     CFG_DEBUG(4) g_print("ALPHA_CHECK: [endfilter] sreg1=%d, basereg=%d, offset=%0lx\n",
		ins->sreg1, ins->inst_left->inst_basereg, ins->inst_left->inst_offset);

	     alpha_ldq(code, alpha_ra, ins->inst_left->inst_basereg,
			ins->inst_left->inst_offset);

	     if (ins->sreg1 != -1 && ins->sreg1 != alpha_r0)
		alpha_mov1(code, ins->sreg1, alpha_r0);

	     alpha_ret(code, alpha_ra, 1);
	   }
	   break;
	 
	case OP_CALL_HANDLER:
	   {
	     int offset;

	     offset = (char *)code - (char *)cfg->native_code;

	     CFG_DEBUG(4) g_print("ALPHA_CHECK: [call_handler] add patch info: MONO_PATCH_INFO_BB offset: %0X, target_bb: %p]\n",
				  offset, ins->inst_target_bb);

	     mono_add_patch_info (cfg, offset,
				  MONO_PATCH_INFO_BB,
				  ins->inst_target_bb);
	     alpha_bsr(code, alpha_ra, 0);
	   }
	   break;

	 case OP_THROW:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [throw] sreg1=%0x\n",
				ins->sreg1);
	   alpha_mov1(code, ins->sreg1, alpha_a0);
	   code = emit_call (cfg, code, MONO_PATCH_INFO_INTERNAL_METHOD,
			     (gpointer)"mono_arch_throw_exception");
	   break;

         case OP_RETHROW:
           CFG_DEBUG(4) g_print("ALPHA_CHECK: [rethrow] sreg1=%0x\n",
                                ins->sreg1);
           alpha_mov1(code, ins->sreg1, alpha_a0);
           code = emit_call (cfg, code, MONO_PATCH_INFO_INTERNAL_METHOD,
                             (gpointer)"mono_arch_rethrow_exception");
           break;

	 case OP_JMP:
	   {
	     /*
	      * Note: this 'frame destruction' logic is useful for tail calls,
	      too. Keep in sync with the code in emit_epilog.
	      */
	     int offset;
	     AlphaGotData ge_data;

	     CFG_DEBUG(4) g_print("ALPHA_CHECK: [jmp] %p\n", ins->inst_p0);

	     /* FIXME: no tracing support... */
	     if (cfg->prof_options & MONO_PROFILE_ENTER_LEAVE)
	       code = mono_arch_instrument_epilog (cfg,
				   mono_profiler_method_leave, code, FALSE);
	     g_assert (!cfg->method->save_lmf);

	     alpha_mov1( code, alpha_fp, alpha_sp );

	     code = emit_load_volatile_arguments (cfg, code);

	     offset = cfg->arch.params_stack_size;

	     alpha_ldq( code, alpha_ra, alpha_sp, (offset + 0) );
	     alpha_ldq( code, alpha_fp, alpha_sp, (offset + 8) );
	     alpha_lda( code, alpha_sp, alpha_sp, cfg->arch.stack_size );

	     ge_data.data.p = ins->inst_p0;
	     add_got_entry(cfg, GT_PTR, ge_data,
			   (char *)code - (char *)cfg->native_code,
			   MONO_PATCH_INFO_METHOD_JUMP, ins->inst_p0);
	     alpha_ldq( code, alpha_pv, alpha_gp, 0);

	     alpha_jsr( code, alpha_zero, alpha_pv, 0);
	   }
	   break;

	 case OP_AOTCONST:
	   mono_add_patch_info (cfg, offset,
				(MonoJumpInfoType)ins->inst_i1, ins->inst_p0);
	   break;

	 case OP_MEMORY_BARRIER:
	   CFG_DEBUG(4) g_print("ALPHA_CHECK: [mb]\n");
	   alpha_mb(code);
	   break;
	  
	 case OP_CKFINITE:
	   // Float register contains a value which we need to check
	   {
		double	ni = -1.0 / 0.0;
		double	pi = 1.0 / 0.0;
		AlphaGotData	ge_data;

	   	CFG_DEBUG(4) g_print("ALPHA_TODO: [chfinite] sreg1=%d\n", ins->sreg1);
 	   	alpha_cmptun_su(code, ins->sreg1, ins->sreg1, alpha_at);
	   	alpha_trapb(code);
	   	EMIT_COND_EXC_BRANCH(fbne, alpha_at, "ArithmeticException");

		// Negative infinity
		ge_data.data.d = ni;
             	add_got_entry(cfg, GT_DOUBLE, ge_data,
                           (char *)code - (char *)cfg->native_code,
                           MONO_PATCH_INFO_NONE, 0);
             	alpha_ldt(code, alpha_at, alpha_gp, 0);

		alpha_cmpteq_su(code, ins->sreg1, alpha_at, alpha_at);
		alpha_trapb(code);

		EMIT_COND_EXC_BRANCH(fbne, alpha_at, "ArithmeticException");

                // Positive infinity
                ge_data.data.d = pi;
                add_got_entry(cfg, GT_DOUBLE, ge_data,
                           (char *)code - (char *)cfg->native_code,
                           MONO_PATCH_INFO_NONE, 0);
                alpha_ldt(code, alpha_at, alpha_gp, 0);

                alpha_cmpteq_su(code, ins->sreg1, alpha_at, alpha_at);
                alpha_trapb(code);

                EMIT_COND_EXC_BRANCH(fbne, alpha_at, "ArithmeticException");
	   }
	   break;
	 case OP_FDIV:
	   CFG_DEBUG(4) g_print("ALPHA_TODO: [fdiv] dest=%d, sreg1=%d, sreg2=%d\n",
		ins->dreg, ins->sreg1, ins->sreg2);
	   alpha_divt_su(code, ins->sreg1, ins->sreg2, ins->dreg);
	   alpha_trapb(code);

	   break;
	 default:
	   g_warning ("unknown opcode %s in %s()\n",
		      mono_inst_name (ins->opcode), __FUNCTION__);
	   alpha_nop(code);
	   //		 g_assert_not_reached ();
	   
	 }
       
       if ( (((char *)code) -
	     ((char *)cfg->native_code) -
	     offset) > max_len)
	 {
	   g_warning ("wrong maximal instruction length of instruction %s (expected %d, got %ld)",
		      mono_inst_name (ins->opcode), max_len,
		      ((char *)code) - ((char *)cfg->native_code) - offset );
	   //g_assert_not_reached ();
	 }
       
       cpos += max_len;
       
       last_ins = ins;
       last_offset = offset;
   }
   
   cfg->code_len = ((char *)code) - ((char *)cfg->native_code);
}

/*========================= End of Function ========================*/




/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_cpu_optimizations                       */
/*                                                                  */
/* Function     - Returns the optimizations supported on this CPU   */
/*                                                                  */
/*------------------------------------------------------------------*/

guint32
mono_arch_cpu_optimizations (guint32 *exclude_mask)
{
   guint32 opts = 0;

   if (getenv("MONO_ALPHA_DEBUG"))
	mini_alpha_verbose_level = 1;
   
   ALPHA_DEBUG("mono_arch_cpu_optimizations");
   
   /*----------------------------------------------------------*/
   /* no alpha-specific optimizations yet                       */
   /*----------------------------------------------------------*/
   *exclude_mask = MONO_OPT_LINEARS;
   //      *exclude_mask = MONO_OPT_INLINE|MONO_OPT_INLINE;

   return opts;
}
/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_cpu_enumerate_simd_versions             */
/*                                                                  */
/* Function     - Returns the SIMD instruction sets on this CPU     */
/*                                                                  */
/*------------------------------------------------------------------*/
guint32
mono_arch_cpu_enumerate_simd_versions (void)
{
	/* SIMD is currently unimplemented */
	return 0;
}
/*========================= End of Function ========================*/



/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         -  mono_arch_flush_icache                           */
/*                                                                  */
/* Function     -  Flush the CPU icache.                            */
/*                                                                  */
/*------------------------------------------------------------------*/

void
mono_arch_flush_icache (guint8 *code, gint size)
{
  //ALPHA_DEBUG("mono_arch_flush_icache");
   
   /* flush instruction cache to see trampoline code */
   asm volatile("imb":::"memory");
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_regname                                 */
/*                                                                  */
/* Function     - Returns the name of the register specified by     */
/*                the input parameter.                              */
/*                                                                  */
/*------------------------------------------------------------------*/

const char*
mono_arch_regname (int reg) {
  static const char * rnames[] = {
    "alpha_r0", "alpha_r1", "alpha_r2", "alpha_r3", "alpha_r4",
    "alpha_r5", "alpha_r6", "alpha_r7", "alpha_r8", "alpha_r9",
    "alpha_r10", "alpha_r11", "alpha_r12", "alpha_r13", "alpha_r14",
    "alpha_r15", "alpha_r16", "alpha_r17", "alpha_r18", "alpha_r19",
    "alpha_r20", "alpha_r21", "alpha_r22", "alpha_r23", "alpha_r24",
    "alpha_r25", "alpha_r26", "alpha_r27", "alpha_r28", "alpha_r29",
    "alpha_r30", "alpha_r31"
  };
   
  if (reg >= 0 && reg < 32)
    return rnames [reg];
   else
     return "unknown";
}
/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_fregname                                */
/*                                                                  */
/* Function     - Returns the name of the register specified by     */
/*                the input parameter.                              */
/*                                                                  */
/*------------------------------------------------------------------*/

const char*
mono_arch_fregname (int reg) {
  static const char * rnames[] = {
    "alpha_f0", "alpha_f1", "alpha_f2", "alpha_f3", "alpha_f4",
    "alpha_f5", "alpha_f6", "alpha_f7", "alpha_f8", "alpha_f9",
    "alpha_f10", "alpha_f11", "alpha_f12", "alpha_f13", "alpha_f14",
    "alpha_f15", "alpha_f16", "alpha_f17", "alpha_f18", "alpha_f19",
    "alpha_f20", "alpha_f21", "alpha_f22", "alpha_f23", "alpha_f24",
    "alpha_f25", "alpha_f26", "alpha_f27", "alpha_f28", "alpha_f29",
    "alpha_f30", "alpha_f31"
  };
   
  if (reg >= 0 && reg < 32)
    return rnames [reg];
  else
    return "unknown";
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_patch_code                              */
/*                                                                  */
/* Function     - Process the patch data created during the         */
/*                instruction build process. This resolves jumps,   */
/*                calls, variables etc.                             */
/*                                                                  */
/*------------------------------------------------------------------*/

void
mono_arch_patch_code (MonoMethod *method, MonoDomain *domain,
                      guint8 *code, MonoJumpInfo *ji, MonoCodeManager *dyn_code_mp, gboolean run_cctors)
{
  MonoJumpInfo *patch_info;
  gboolean compile_aot = !run_cctors;

  ALPHA_DEBUG("mono_arch_patch_code");
   
  for (patch_info = ji; patch_info; patch_info = patch_info->next) 
    {	
      unsigned char *ip = patch_info->ip.i + code;
      const unsigned char *target;
		
      target = mono_resolve_patch_target (method, domain,
					  code, patch_info, run_cctors);
		
      if (compile_aot) 
	{
	  switch (patch_info->type) 
	    {
				  
	    case MONO_PATCH_INFO_BB:
	    case MONO_PATCH_INFO_LABEL:
	      break;
	    default:
	      /* No need to patch these */
	      continue;
	    }
	}
		
      switch (patch_info->type) 
	{	 
	case MONO_PATCH_INFO_NONE:
	  continue;

	case MONO_PATCH_INFO_GOT_OFFSET:
	  {
	    unsigned int *ip2 = (unsigned int *)ip;
	    unsigned int inst = *ip2;
	    unsigned int off = patch_info->data.offset & 0xFFFFFFFF;

	    g_assert(!(off & 0xFFFF8000));

	    inst |= off;

	    *ip2 = inst;
	  }
	  continue;

	case MONO_PATCH_INFO_CLASS_INIT: 
	  {		  
	    /* Might already been changed to a nop */
	    unsigned int* ip2 = (unsigned int *)ip;
	   unsigned long t_addr = (unsigned long)target;
 
	    if (*ip2 != (t_addr & 0xFFFFFFFF) ||
		*(ip2+1) != ((t_addr>>32) & 0xFFFFFFFF))
			NOT_IMPLEMENTED;
	    //  amd64_call_code (ip2, 0);
	    break;
	  }
			 
	  //	case MONO_PATCH_INFO_METHOD_REL:
	case MONO_PATCH_INFO_R8:
	case MONO_PATCH_INFO_R4:
	  g_assert_not_reached ();
	  continue;
	case MONO_PATCH_INFO_BB:
	  break;

	case MONO_PATCH_INFO_METHOD:
	case MONO_PATCH_INFO_METHODCONST:
	case MONO_PATCH_INFO_INTERNAL_METHOD:
	case MONO_PATCH_INFO_METHOD_JUMP:
	  {
	    volatile unsigned int *p = (unsigned int *)ip;
	    unsigned long t_addr;

	    t_addr = *(p+1);
	    t_addr <<= 32;
	    t_addr |= *(p);

	    ALPHA_PRINT
	    g_debug("ALPHA_PATCH: MONO_PATCH_INFO_METHOD(CONST) calc target: %p, stored target: %0lX",
		   target, t_addr);
	    if (target != ((void *)t_addr))
	      {
		t_addr = (unsigned long)target;
		*p = (unsigned int)(t_addr & 0xFFFFFFFF);
		*(p+1) = (unsigned int)((t_addr >> 32) & 0xFFFFFFFF);
	      }
	  }
	  continue;

	case MONO_PATCH_INFO_ABS:
	  {
            volatile unsigned int *p = (unsigned int *)ip;
            unsigned long t_addr;

            t_addr = *(p+1);
            t_addr <<= 32;
	    t_addr += *(p);

            ALPHA_PRINT g_debug("ALPHA_PATCH: MONO_PATCH_INFO_ABS calc target: %p, stored target: %0lX",
                   target, t_addr);

	  }
	  continue;
	case MONO_PATCH_INFO_SWITCH:
	  {
	    unsigned int *pcode = (unsigned int *)ip;
	    unsigned long t_addr;

	    t_addr = (unsigned long)target;

	    if (((unsigned long)ip) % 8)
	      {
		alpha_nop(pcode);
		ip += 4;
	      }
	    
	    //alpha_ldq(pcode, alpha_at, alpha_gp, (ip - code + 8));
	    alpha_nop(pcode);        // TODO optimize later
	    alpha_bsr(pcode, alpha_at, 2);

	    *pcode = (unsigned int)(t_addr & 0xFFFFFFFF);
	    pcode++;
	    *pcode = (unsigned int)((t_addr >> 32) & 0xFFFFFFFF);
	    pcode++;

	    alpha_ldq(pcode, alpha_at, alpha_at, 0);

	  }
	  continue;

	default:
	  break;
	}
      
      {
	volatile unsigned int *p = (unsigned int *)ip;
	unsigned int alpha_ins = *p;
	unsigned int opcode;
	long br_offset;
			 
	opcode = (alpha_ins >> AXP_OP_SHIFT) & AXP_OFF6_MASK;
			 
	if (opcode >= 0x30 && opcode <= 0x3f)
	  {
	    // This is branch with offset instruction
	    br_offset = (target - ip - 4);
				  
	    g_assert(!(br_offset & 3));
				  
	    alpha_ins |= (br_offset/4) & AXP_OFF21_MASK;
				  
	    *p = alpha_ins;
	  }
      }
    }
}

/*========================= End of Function ========================*/
/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_emit_this_vret_args                     */
/*                                                                  */
/* Function     -                                                   */
/*                                                                  */
/*------------------------------------------------------------------*/

void
mono_arch_emit_this_vret_args (MonoCompile *cfg, MonoCallInst *inst,
	int this_reg, int this_type, int vt_reg)
{
  MonoCallInst *call = (MonoCallInst*)inst;
  CallInfo * cinfo = get_call_info (cfg->generic_sharing_context, inst->signature, FALSE);

  CFG_DEBUG(2) ALPHA_DEBUG("mono_arch_emit_this_vret_args");

  if (vt_reg != -1)
    {
      MonoInst *vtarg;

      if (cinfo->ret.storage == ArgValuetypeInReg)
	{
	  /*
	   * The valuetype is in RAX:RDX after the call, need to be copied to
	   * the stack. Push the address here, so the call instruction can
	   * access it.
	   */
	  //MONO_INST_NEW (cfg, vtarg, OP_X86_PUSH);
	  //vtarg->sreg1 = vt_reg;
	  //mono_bblock_add_inst (cfg->cbb, vtarg);

	  /* Align stack */
	  //MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SUB_IMM, X86_ESP, X86_E
	  //			   SP, 8);
	}
      else
	{
	  MONO_INST_NEW (cfg, vtarg, OP_MOVE);
	  vtarg->sreg1 = vt_reg;
	  vtarg->dreg = mono_alloc_ireg (cfg);
	  mono_bblock_add_inst (cfg->cbb, vtarg);

	  mono_call_inst_add_outarg_reg (cfg, call, vtarg->dreg,
					 cinfo->ret.reg, FALSE);
	}
    }

  /* add the this argument */
  if (this_reg != -1)
    {
      MonoInst *this;
      MONO_INST_NEW (cfg, this, OP_MOVE);
      this->type = this_type;
      this->sreg1 = this_reg;
      this->dreg = mono_alloc_ireg (cfg);
      mono_bblock_add_inst (cfg->cbb, this);

      mono_call_inst_add_outarg_reg (cfg, call, this->dreg,
				     cinfo->args [0].reg, FALSE);
    }

  g_free (cinfo);
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_is_inst_imm                             */
/*                                                                  */
/* Function     - Determine if operand qualifies as an immediate    */
/*                value. For Alpha this is a value 0 - 255          */
/*                                                                  */
/* Returns      - True|False - is [not] immediate value.            */
/*                                                                  */
/*------------------------------------------------------------------*/

gboolean
mono_arch_is_inst_imm (gint64 imm)
{
//   ALPHA_DEBUG("mono_arch_is_inst_imm");
	
   return (imm & ~(0x0FFL)) ? 0 : 1;
}

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_finish_init                             */
/*                                                                  */
/* Function     - Setup the JIT's Thread Level Specific Data.       */
/*                                                                  */
/*------------------------------------------------------------------*/

void
mono_arch_finish_init (void)
{
   ALPHA_DEBUG("mono_arch_finish_init");
   
   if (!lmf_addr_key_inited) {
	  lmf_addr_key_inited = TRUE;
	  pthread_key_create (&lmf_addr_key, NULL);
   }

   pthread_setspecific (lmf_addr_key, &tls->lmf);
}

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_cpu_init                                */
/*                                                                  */
/* Function     - Perform CPU specific initialization to execute    */
/*                managed code.                                     */
/*                                                                  */
/*------------------------------------------------------------------*/

void
mono_arch_cpu_init (void)
{
  unsigned long amask, implver;
  register long v0 __asm__("$0") = -1;
 
  ALPHA_DEBUG("mono_arch_cpu_init");

  __asm__ (".long 0x47e00c20" : "=r"(v0) : "0"(v0));
  amask = ~v0;
  __asm__ (".long 0x47e03d80" : "=r"(v0));
  implver = v0;

  if (amask & 1)
    bwx_supported = 1;

  //printf("amask: %x, implver: %x", amask, implver);
}

/*
 * Initialize architecture specific code.
 */
void
mono_arch_init (void)
{
}

/*
 * Cleanup architecture specific code.
 */
void
mono_arch_cleanup (void)
{
}

/*
 * get_call_info:
 *
 * Obtain information about a call according to the calling convention.
 *
 * For x86 ELF, see the "System V Application Binary Interface Intel386
 * Architecture Processor Supplment, Fourth Edition" document for more
 * information.
 * For x86 win32, see ???.
 */
static CallInfo*
get_call_info (MonoGenericSharingContext *gsctx, MonoMethodSignature *sig, gboolean is_pinvoke)
{
   guint32 i, gr, fr, *pgr, *pfr;
   MonoType *ret_type;
   int n = sig->hasthis + sig->param_count;
   guint32 stack_size = 0;
   CallInfo *cinfo;
   
   cinfo = g_malloc0 (sizeof (CallInfo) + (sizeof (ArgInfo) * n));
   
   gr = 0;
   fr = 0;
   
   if (is_pinvoke)
	pgr = pfr = &gr;
   else
   {
	pgr = &gr;
	pfr = &fr;
   }

   /* return value */
   {
     ret_type = mono_type_get_underlying_type (sig->ret);
     switch (ret_type->type) {
     case MONO_TYPE_BOOLEAN:
     case MONO_TYPE_I1:
     case MONO_TYPE_U1:
     case MONO_TYPE_I2:
     case MONO_TYPE_U2:
     case MONO_TYPE_CHAR:
     case MONO_TYPE_I4:
     case MONO_TYPE_U4:
     case MONO_TYPE_I:
     case MONO_TYPE_U:
     case MONO_TYPE_PTR:
     case MONO_TYPE_FNPTR:
     case MONO_TYPE_CLASS:
     case MONO_TYPE_OBJECT:
     case MONO_TYPE_SZARRAY:
     case MONO_TYPE_ARRAY:
     case MONO_TYPE_STRING:
       cinfo->ret.storage = ArgInIReg;
       cinfo->ret.reg = alpha_r0;
       break;
     case MONO_TYPE_U8:
     case MONO_TYPE_I8:
       cinfo->ret.storage = ArgInIReg;
       cinfo->ret.reg = alpha_r0;
       break;
     case MONO_TYPE_R4:
       cinfo->ret.storage = ArgInFloatReg;
       cinfo->ret.reg = alpha_f0;
       break;
     case MONO_TYPE_R8:
       cinfo->ret.storage = ArgInDoubleReg;
       cinfo->ret.reg = alpha_f0;
       break;
     case MONO_TYPE_GENERICINST:
       if (!mono_type_generic_inst_is_valuetype (ret_type))
	 {
	   cinfo->ret.storage = ArgInIReg;
	   cinfo->ret.reg = alpha_r0;
	   break;
	 }
       /* Fall through */
     case MONO_TYPE_VALUETYPE:
       {
	 guint32 tmp_gr = 0, tmp_fr = 0, tmp_stacksize = 0;
			
	 add_valuetype (gsctx, sig, &cinfo->ret, sig->ret, TRUE,
			&tmp_gr, &tmp_fr, &tmp_stacksize);
	 
	 if (cinfo->ret.storage == ArgOnStack)
	   /* The caller passes the address where the value
	      is stored */
	   add_general (pgr, &stack_size, &cinfo->ret);
	 break;
       }
     case MONO_TYPE_TYPEDBYREF:
       /* Same as a valuetype with size 24 */
       add_general (pgr, &stack_size, &cinfo->ret);
       ;
       break;
     case MONO_TYPE_VOID:
       break;
     default:
       g_error ("Can't handle as return value 0x%x", sig->ret->type);
     }
   }
   
   /* this */
   if (sig->hasthis)
     add_general (pgr, &stack_size, cinfo->args + 0);
   
   if (!sig->pinvoke &&
	   (sig->call_convention == MONO_CALL_VARARG) && (n == 0))
     {
       gr = PARAM_REGS;
       fr = FLOAT_PARAM_REGS;
		
       /* Emit the signature cookie just before the implicit arguments
	*/
       add_general (pgr, &stack_size, &cinfo->sig_cookie);
     }
   
   for (i = 0; i < sig->param_count; ++i)
     {
       ArgInfo *ainfo = &cinfo->args [sig->hasthis + i];
       MonoType *ptype;
       
       if (!sig->pinvoke &&
	   (sig->call_convention == MONO_CALL_VARARG) &&
	   (i == sig->sentinelpos))
	 {
	   /* We allways pass the sig cookie on the stack for simpl
	      icity */
	   /*
	    * Prevent implicit arguments + the sig cookie from being passed
	    * in registers.
	    */
	   gr = PARAM_REGS;
	   fr = FLOAT_PARAM_REGS;
			 
	   /* Emit the signature cookie just before the implicit arguments */
	   add_general (pgr, &stack_size, &cinfo->sig_cookie);
	 }
		
       if (sig->params [i]->byref) {
	 add_general (pgr, &stack_size, ainfo);
	 continue;
       }
       
       ptype = mono_type_get_underlying_type (sig->params [i]);
       
       switch (ptype->type) {
       case MONO_TYPE_BOOLEAN:
       case MONO_TYPE_I1:
       case MONO_TYPE_U1:
	 add_general (pgr, &stack_size, ainfo);
	 break;
       case MONO_TYPE_I2:
       case MONO_TYPE_U2:
       case MONO_TYPE_CHAR:
	 add_general (pgr, &stack_size, ainfo);
	 break;
       case MONO_TYPE_I4:
       case MONO_TYPE_U4:
	 add_general (pgr, &stack_size, ainfo);
	 break;
       case MONO_TYPE_I:
       case MONO_TYPE_U:
       case MONO_TYPE_PTR:
       case MONO_TYPE_FNPTR:
       case MONO_TYPE_CLASS:
       case MONO_TYPE_OBJECT:
       case MONO_TYPE_STRING:
       case MONO_TYPE_SZARRAY:
       case MONO_TYPE_ARRAY:
	 add_general (pgr, &stack_size, ainfo);
	 break;
       case MONO_TYPE_GENERICINST:
	 if (!mono_type_generic_inst_is_valuetype (ptype))
	   {
	     add_general (pgr, &stack_size, ainfo);
	     break;
	   }
	 /* Fall through */
       case MONO_TYPE_VALUETYPE:
	 /* FIXME: */
	 /* We allways pass valuetypes on the stack */
	 add_valuetype (gsctx, sig, ainfo, sig->params [i],
			FALSE, pgr, pfr, &stack_size);
	 break;
       case MONO_TYPE_TYPEDBYREF:
	 stack_size += sizeof (MonoTypedRef);
	 ainfo->storage = ArgOnStack;
	 break;
       case MONO_TYPE_U8:
       case MONO_TYPE_I8:
	 add_general (pgr, &stack_size, ainfo);
	 break;
       case MONO_TYPE_R4:
	 add_float (pfr, &stack_size, ainfo, FALSE);
	 break;
       case MONO_TYPE_R8:
	 add_float (pfr, &stack_size, ainfo, TRUE);
	 break;
       default:
	 g_assert_not_reached ();
       }
     }
   
   if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG) &&
       (n > 0) && (sig->sentinelpos == sig->param_count))
     {
       gr = PARAM_REGS;
       fr = FLOAT_PARAM_REGS;
		
       /* Emit the signature cookie just before the implicit arguments
	*/
       add_general (pgr, &stack_size, &cinfo->sig_cookie);
     }
   
   cinfo->stack_usage = stack_size;
   cinfo->reg_usage = gr;
   cinfo->freg_usage = fr;
   
   return cinfo;
}

static const char *CvtMonoType(MonoTypeEnum t)
{
  switch(t)
    {
    case MONO_TYPE_END:
      return "MONO_TYPE_END";
    case MONO_TYPE_VOID:
      return "MONO_TYPE_VOID";
    case MONO_TYPE_BOOLEAN:
      return "MONO_TYPE_BOOLEAN";
    case MONO_TYPE_CHAR:
      return "MONO_TYPE_CHAR";
    case MONO_TYPE_I1:
      return "MONO_TYPE_I1";
    case MONO_TYPE_U1:
      return "MONO_TYPE_U1";
    case MONO_TYPE_I2:
      return "MONO_TYPE_I2";
    case MONO_TYPE_U2:
      return "MONO_TYPE_U2";
    case MONO_TYPE_I4:
      return "MONO_TYPE_I4";
    case MONO_TYPE_U4:
      return "MONO_TYPE_U4";
    case MONO_TYPE_I8:
      return "MONO_TYPE_I8";
    case MONO_TYPE_U8:
      return "MONO_TYPE_U8";
    case MONO_TYPE_R4:
      return "MONO_TYPE_R4";
    case MONO_TYPE_R8:
      return "MONO_TYPE_R8";
    case MONO_TYPE_STRING:
      return "MONO_TYPE_STRING";
    case MONO_TYPE_PTR:
      return "MONO_TYPE_PTR";
    case MONO_TYPE_BYREF:
      return "MONO_TYPE_BYREF";
    case MONO_TYPE_VALUETYPE:
      return "MONO_TYPE_VALUETYPE";
    case MONO_TYPE_CLASS:
      return "MONO_TYPE_CLASS";
    case MONO_TYPE_VAR:
      return "MONO_TYPE_VAR";
    case MONO_TYPE_ARRAY:
      return "MONO_TYPE_ARRAY";
    case MONO_TYPE_GENERICINST:
      return "MONO_TYPE_GENERICINST";
    case MONO_TYPE_TYPEDBYREF:
      return "MONO_TYPE_TYPEDBYREF";
    case MONO_TYPE_I:
      return "MONO_TYPE_I";
    case MONO_TYPE_U:
      return "MONO_TYPE_U";
    case MONO_TYPE_FNPTR:
      return "MONO_TYPE_FNPTR";
    case MONO_TYPE_OBJECT:
      return "MONO_TYPE_OBJECT";
    case MONO_TYPE_SZARRAY:
      return "MONO_TYPE_SZARRAY";
    case MONO_TYPE_MVAR:
      return "MONO_TYPE_MVAR";
    case MONO_TYPE_CMOD_REQD:
      return "MONO_TYPE_CMOD_REQD";
    case MONO_TYPE_CMOD_OPT:
      return "MONO_TYPE_CMOD_OPT";
    case MONO_TYPE_INTERNAL:
      return "MONO_TYPE_INTERNAL";
    case MONO_TYPE_MODIFIER:
      return "MONO_TYPE_MODIFIER";
    case MONO_TYPE_SENTINEL:
      return "MONO_TYPE_SENTINEL";
    case MONO_TYPE_PINNED:
      return "MONO_TYPE_PINNED";
    default:
      ;
    }
  return "unknown";
}


/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_call_opcode                             */
/*                                                                  */
/* Function     - Take the arguments and generate the arch-specific */
/*                instructions to properly call the function. This  */
/*                includes pushing, moving argments to the correct  */
/*                etc.                                              */
/*
 * This method is called during converting method to IR
 * We need to generate IR ints to follow calling convention
 *  cfg - points to currently compiled unit
 *  bb - ???
 *  call - points to structure that describes what we are going to
 *         call (at least number of parameters required for the call)
 *
 * 
 * On return we need to pass back modified call structure
 */
/*------------------------------------------------------------------*/

MonoCallInst*
mono_arch_call_opcode (MonoCompile *cfg, MonoBasicBlock* bb,
                       MonoCallInst *call, int is_virtual)
{
   MonoInst *arg, *in;
   MonoMethodSignature *sig;
   int i, n;
   CallInfo *cinfo;
   int sentinelpos;
   
   CFG_DEBUG(2) ALPHA_DEBUG("mono_arch_call_opcode");
   
   sig = call->signature;
   n = sig->param_count + sig->hasthis;

   // Collect info about method we age going to call
   cinfo = get_call_info (cfg->generic_sharing_context, sig, sig->pinvoke);

   CFG_DEBUG(3) g_print("ALPHA: Will call %s method with %d(%d) params. RetType: %s(0x%X)\n",
			sig->pinvoke ? "PInvoke" : "Managed",
			sig->param_count, sig->hasthis,
			CvtMonoType(sig->ret->type), sig->ret->type);
  
   if (cinfo->stack_usage > cfg->arch.params_stack_size)
	cfg->arch.params_stack_size = cinfo->stack_usage;
 
   if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG))
	 sentinelpos = sig->sentinelpos + (is_virtual ? 1 : 0);
    
   for (i = 0; i < n; ++i)
     {
       ArgInfo *ainfo = cinfo->args + i;
		
       /* Emit the signature cookie just before the implicit arguments
	*/
       if (!sig->pinvoke &&
	   (sig->call_convention == MONO_CALL_VARARG) &&
	   (i == sentinelpos))
	 {
	   MonoMethodSignature *tmp_sig;
	   MonoInst *sig_arg;
			 
	   /* FIXME: Add support for signature tokens to AOT */
	   cfg->disable_aot = TRUE;
	   MONO_INST_NEW (cfg, arg, OP_OUTARG);
			 
	   /*
	    * mono_ArgIterator_Setup assumes the signature cookie is
	    * passed first and all the arguments which were before it are
	    * passed on the stack after the signature. So compensate by
	    * passing a different signature.
	    */
	   tmp_sig = mono_metadata_signature_dup (call->signature);
	   tmp_sig->param_count -= call->signature->sentinelpos;
	   tmp_sig->sentinelpos = 0;
	   memcpy (tmp_sig->params,
		   call->signature->params + call->signature->sentinelpos,
		   tmp_sig->param_count * sizeof (MonoType*));
	   
	   MONO_INST_NEW (cfg, sig_arg, OP_ICONST);
	   sig_arg->inst_p0 = tmp_sig;
	   
	   MONO_INST_NEW (cfg, arg, OP_OUTARG);
	   arg->inst_left = sig_arg;
	   arg->type = STACK_PTR;
	   
	   /* prepend, so they get reversed */
	   arg->next = call->out_args;
	   call->out_args = arg;
	 }
		
       if (is_virtual && i == 0) {
	 /* the argument will be attached to the call instrucion
	  */
	 in = call->args [i];
       } else {
	 MonoType *arg_type;

	 MONO_INST_NEW (cfg, arg, OP_OUTARG);
	 in = call->args [i];
	 arg->cil_code = in->cil_code;
	 arg->inst_left = in;
	 arg->type = in->type;
	 /* prepend, so they get reversed */
	 arg->next = call->out_args;
	 call->out_args = arg;

	 CFG_DEBUG(3) g_print("ALPHA: Param[%d] - ", i);

	 if (sig->hasthis && (i == 0))
	   arg_type = &mono_defaults.object_class->byval_arg;
	 else
	   arg_type = sig->params [i - sig->hasthis];
	 
	 if ((i >= sig->hasthis) &&
	     (MONO_TYPE_ISSTRUCT(arg_type)))
	   {
	     guint align;
	     guint32 size;
	     
	     if (arg_type->type == MONO_TYPE_TYPEDBYREF) {
	       size = sizeof (MonoTypedRef);
	       align = sizeof (gpointer);
	     }
	     else
	       if (sig->pinvoke)
		 size = mono_type_native_stack_size (&in->klass->byval_arg,
						     &align);
	       else
		 size = mini_type_stack_size (cfg->generic_sharing_context, &in->klass->byval_arg, &align);

	     if (ainfo->storage == ArgAggregate)
	       {
		 MonoInst *vtaddr, *load, *load2, *offset_ins, *set_reg;
		 int slot, j;

                 CFG_DEBUG(3) g_print("aggregate value type, size:%d\n", size);

		 vtaddr = mono_compile_create_var (cfg,
			      &mono_defaults.int_class->byval_arg, OP_LOCAL);

		 /*
		  * Part of the structure is passed in registers.
		  */
		 for (j = 0; j < ainfo->nregs; ++j)
		   {
		     int offset, load_op, dest_reg, arg_storage;

		     slot = ainfo->reg + j;
		     load_op = CEE_LDIND_I;
		     offset = j * 8;
		     dest_reg = ainfo->reg + j;
		     arg_storage = ArgInIReg;
		     
		     MONO_INST_NEW (cfg, load, CEE_LDIND_I);
		     load->ssa_op = MONO_SSA_LOAD;
		     load->inst_i0 = (cfg)->varinfo [vtaddr->inst_c0];

		     NEW_ICONST (cfg, offset_ins, offset);
		     MONO_INST_NEW (cfg, load2, CEE_ADD);
		     load2->inst_left = load;
		     load2->inst_right = offset_ins;

		     MONO_INST_NEW (cfg, load, load_op);
		     load->inst_left = load2;

		     if (j == 0)
		       set_reg = arg;
		     else
		       MONO_INST_NEW (cfg, set_reg, OP_OUTARG_REG);

		     add_outarg_reg (cfg, call, set_reg, arg_storage,
				     dest_reg, load);
		     if (set_reg != call->out_args)
		       {
			 set_reg->next = call->out_args;
			 call->out_args = set_reg;
		     }
		   }

		 /*
		  * Part of the structure is passed on the stack.
		  */
		 for (j = ainfo->nregs; j < ainfo->nslots; ++j)
		   {
		     MonoInst *outarg;

		     slot = ainfo->reg + j;

		     MONO_INST_NEW (cfg, load, CEE_LDIND_I);
		     load->ssa_op = MONO_SSA_LOAD;
		     load->inst_i0 = (cfg)->varinfo [vtaddr->inst_c0];

		     NEW_ICONST (cfg, offset_ins, (j * sizeof (gpointer)));
		     MONO_INST_NEW (cfg, load2, CEE_ADD);
		     load2->inst_left = load;
		     load2->inst_right = offset_ins;

		     MONO_INST_NEW (cfg, load, CEE_LDIND_I);
		     load->inst_left = load2;

		     if (j == 0)
		       outarg = arg;
		     else
		       MONO_INST_NEW (cfg, outarg, OP_OUTARG);
		     
		     outarg->inst_left = load;
		     //outarg->inst_imm = 16 + ainfo->offset + (slot - 8) * 8;
		     outarg->dreg = ainfo->offset + (slot - 22) * 8;

		     if (outarg != call->out_args)
		       {
			 outarg->next = call->out_args;
			 call->out_args = outarg;
		       }
		   }
		
		 /* Trees can't be shared so make a copy*/
		 MONO_INST_NEW (cfg, arg, CEE_STIND_I);
		 arg->cil_code = in->cil_code;
		 arg->ssa_op = MONO_SSA_STORE;
		 arg->inst_left = vtaddr;
		 arg->inst_right = in;
		 arg->type = in->type;

		 /* prepend, so they get reversed */
		 arg->next = call->out_args;
		 call->out_args = arg;
	       }
	     else
	       {
		 MonoInst *stack_addr;

		 CFG_DEBUG(3) g_print("value type, size:%d\n", size);

		 MONO_INST_NEW (cfg, stack_addr, OP_REGOFFSET);
		 stack_addr->inst_basereg = alpha_sp;
		 //stack_addr->inst_offset = -(cinfo->stack_usage - ainfo->offset);
		 stack_addr->inst_offset = ainfo->offset;
		 //stack_addr->inst_offset = 16 + ainfo->offset;
		 stack_addr->inst_imm = size;

		 arg->opcode = OP_OUTARG_VT;
		 arg->inst_right = stack_addr;
	       }

	     /*
	       arg->opcode = OP_OUTARG_VT;
	       arg->klass = in->klass;
	       arg->backend.is_pinvoke = sig->pinvoke;
	       arg->inst_imm = size; */
	   }
	 else
	   {
	     CFG_DEBUG(3) g_print("simple\n");

	     switch (ainfo->storage)
	       {
	       case ArgInIReg:
		 add_outarg_reg (cfg, call, arg, ainfo->storage, 
				 ainfo->reg, in);
		 break;
	       case ArgOnStack:
		 arg->opcode = OP_OUTARG;
		 //arg->dreg = -((n - i) * 8);
		 arg->dreg = ainfo->offset;
		 //arg->inst_left->inst_imm = (n - i - 1) * 8;

		 if (!sig->params[i-sig->hasthis]->byref) {
		   if (sig->params[i-sig->hasthis]->type == MONO_TYPE_R4)
		     arg->opcode = OP_OUTARG_R4;
		   else
		     if (sig->params[i-sig->hasthis]->type == MONO_TYPE_R8)
		       arg->opcode = OP_OUTARG_R8;
		 }
		 break;
		case ArgInFloatReg:
		case ArgInDoubleReg:
		  add_outarg_reg (cfg, call, arg, ainfo->storage, ainfo->reg, in);
		break;
	       default:
		 g_assert_not_reached ();
	       }
	   }
       }
     }

   if (sig->ret && MONO_TYPE_ISSTRUCT (sig->ret))
     {
       if (cinfo->ret.storage == ArgValuetypeInReg) {
	 MonoInst *zero_inst;
	 /*
	  * After the call, the struct is in registers, but needs to be saved
	  to the memory pointed
	  * to by vt_arg in this_vret_args. This means that vt_ar
	  g needs to be saved somewhere
	  * before calling the function. So we add a dummy instru
	  ction to represent pushing the
	  * struct return address to the stack. The return addres
	  s will be saved to this stack slot
	  * by the code emitted in this_vret_args.
	  */
	 MONO_INST_NEW (cfg, arg, OP_OUTARG);
	 MONO_INST_NEW (cfg, zero_inst, OP_ICONST);
	 zero_inst->inst_p0 = 0;
	 arg->inst_left = zero_inst;
	 arg->type = STACK_PTR;
	 /* prepend, so they get reversed */
	 arg->next = call->out_args;
	 call->out_args = arg;
       }
       else
	 /* if the function returns a struct, the called method a
	    lready does a ret $0x4 */
	 if (sig->ret && MONO_TYPE_ISSTRUCT (sig->ret))
	   ; //cinfo->stack_usage -= 4;
     }
   
   // stack_usage shows how much stack we would need to do the call
   // (for example for params that we pass on stack
   call->stack_usage = cinfo->stack_usage;

   // Save all used regs to do the call in compile unit structure
   cfg->used_int_regs |= call->used_iregs;
   
   g_free (cinfo);
   
   return call;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_register_lowlevel_calls                 */
/*                                                                  */
/* Function     - Register routines to help with --trace operation. */
/*                                                                  */
/*------------------------------------------------------------------*/

void
mono_arch_register_lowlevel_calls (void)
{
   ALPHA_DEBUG("mono_arch_register_lowlevel_calls");
   
   mono_register_jit_icall (mono_arch_get_lmf_addr, "mono_arch_get_lmf_addr",
							NULL, TRUE);
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_global_int_regs                         */
/*                                                                  */
/* Function     - Return a list of usable integer registers.        */
/*                                                                  */
/*------------------------------------------------------------------*/

GList *
mono_arch_get_global_int_regs (MonoCompile *cfg)
{
   GList *regs = NULL;
   
   CFG_DEBUG(2) ALPHA_DEBUG("mono_arch_get_global_int_regs");
   
//   regs = g_list_prepend (regs, (gpointer)alpha_r9);
//   regs = g_list_prepend (regs, (gpointer)alpha_r10);
//   regs = g_list_prepend (regs, (gpointer)alpha_r11);
   regs = g_list_prepend (regs, (gpointer)alpha_r12);
   regs = g_list_prepend (regs, (gpointer)alpha_r13);
   regs = g_list_prepend (regs, (gpointer)alpha_r14);

   return regs;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_get_allocatable_int_vars                */
/*                                                                  */
/* Function     -                                                   */
/*                                                                  */
/*------------------------------------------------------------------*/

GList *
mono_arch_get_allocatable_int_vars (MonoCompile *cfg)
{
   GList *vars = NULL;
   int i;
   MonoMethodSignature *sig;
   MonoMethodHeader *header;
   CallInfo *cinfo;

   CFG_DEBUG(2) ALPHA_DEBUG("mono_arch_get_allocatable_int_vars");

   header = cfg->header;

   sig = mono_method_signature (cfg->method);

   cinfo = get_call_info (cfg->generic_sharing_context, sig, FALSE);

   for (i = 0; i < sig->param_count + sig->hasthis; ++i)
     {
       MonoInst *ins = cfg->args [i];

       ArgInfo *ainfo = &cinfo->args [i];

       if (ins->flags &
	   (MONO_INST_IS_DEAD|MONO_INST_VOLATILE|MONO_INST_INDIRECT))
	 continue;

       // if (ainfo->storage == ArgInIReg) {
       //	 /* The input registers are non-volatile */
       // ins->opcode = OP_REGVAR;
       //ins->dreg = 32 + ainfo->reg;
       //   }
     }
   
   for (i = 0; i < cfg->num_varinfo; i++)
     {
       MonoInst *ins = cfg->varinfo [i];
       MonoMethodVar *vmv = MONO_VARINFO (cfg, i);

       /* unused vars */
       if (vmv->range.first_use.abs_pos >= vmv->range.last_use.abs_pos)
	 continue;

       if ((ins->flags &
	    (MONO_INST_IS_DEAD|MONO_INST_VOLATILE|MONO_INST_INDIRECT)) ||
	   (ins->opcode != OP_LOCAL && ins->opcode != OP_ARG))
	 continue;

       if (mono_is_regsize_var (ins->inst_vtype))
	 {
	   g_assert (MONO_VARINFO (cfg, i)->reg == -1);
	   g_assert (i == vmv->idx);
	   vars = g_list_prepend (vars, vmv);
	 }
     }
   
   vars = mono_varlist_sort (cfg, vars, 0);

   return vars;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_get_domain_intrinsic                    */
/*                                                                  */
/* Function     -                                                   */
/*                                                                  */
/* Returns      -                                                   */
/*                                                                  */
/*------------------------------------------------------------------*/

MonoInst *
mono_arch_get_domain_intrinsic (MonoCompile* cfg)
{
   MonoInst *ins;
   
   if (appdomain_tls_offset == -1)
	 return NULL;
   
   MONO_INST_NEW (cfg, ins, OP_TLS_GET);
   ins->inst_offset = appdomain_tls_offset;
   return (ins);
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_get_inst_for_method                   */
/*                                                                  */
/* Function     - Check for opcodes we can handle directly in       */
/*                hardware.                                         */
/*                                                                  */
/*------------------------------------------------------------------*/

MonoInst*
mono_arch_get_inst_for_method (MonoCompile *cfg, MonoMethod *cmethod,
                               MonoMethodSignature *fsig, MonoInst **args)
{
   MonoInst *ins = NULL;
   
   CFG_DEBUG(2) ALPHA_DEBUG("mono_arch_get_inst_for_method");
   
   CFG_DEBUG(3) g_print("mono_arch_get_inst_for_method: %s\n", cmethod->name);
   
   return ins;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_create_class_init_trampoline            */
/*                                                                  */
/* Function     - Creates a trampoline function to run a type init- */
/*                ializer. If the trampoline is called, it calls    */
/*                mono_runtime_class_init with the given vtable,    */
/*                then patches the caller code so it does not get   */
/*                called any more.                                  */
/*                                                                  */
/* Parameter    - vtable - The type to initialize                   */
/*                                                                  */
/* Returns      - A pointer to the newly created code               */
/*                                                                  */
/*------------------------------------------------------------------*/

gpointer
mono_arch_create_class_init_trampoline (MonoVTable *vtable)
{
   ALPHA_DEBUG("mono_arch_create_class_init_trampoline");
   
   NOT_IMPLEMENTED;
   
   return 0;
}

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_instrument_prolog                       */
/*                                                                  */
/* Function     - Create an "instrumented" prolog.                  */
/*                                                                  */
/*------------------------------------------------------------------*/

void*
mono_arch_instrument_prolog (MonoCompile *cfg, void *func, void *p,
                             gboolean enable_arguments)
{
  unsigned int *code = p;
  int offset;

  CallInfo *cinfo = NULL;
  MonoMethodSignature *sig;
  MonoInst *inst;
  int i, n, stack_area = 0;
  AlphaGotData ge_data;

  CFG_DEBUG(2) ALPHA_DEBUG("mono_arch_instrument_prolog");

  /* Keep this in sync with mono_arch_get_argument_info */
  if (enable_arguments)
    {
      /* Allocate a new area on the stack and save arguments there */
      sig = mono_method_signature (cfg->method);

      cinfo = get_call_info (cfg->generic_sharing_context, sig, FALSE);

      n = sig->param_count + sig->hasthis;

      stack_area = ALIGN_TO (n * 8, 8);

      // Correct stack by calculated value
      if (stack_area)
	alpha_lda(code, alpha_sp, alpha_sp, -stack_area);
      
      for (i = 0; i < n; ++i)
	{
	  inst = cfg->args [i];

	  if (inst->opcode == OP_REGVAR)
	    {
	      switch(cinfo->args[i].storage)
		{
		case ArgInDoubleReg:
		  alpha_stt(code, inst->dreg, alpha_sp, (i*8));
                  break;
		case ArgInFloatReg:
		  alpha_sts(code, inst->dreg, alpha_sp, (i*8));
		  break;
		default:
		  alpha_stq(code, inst->dreg, alpha_sp, (i*8));
		}
	    }
	  else
	    {
	      alpha_ldq(code, alpha_at, inst->inst_basereg, inst->inst_offset);
	      alpha_stq(code, alpha_at, alpha_sp, (i*8));
	    }
	}
    }
  
  offset = (char *)code - (char *)cfg->native_code;

  ge_data.data.p = cfg->method;

  add_got_entry(cfg, GT_PTR, ge_data,
		(char *)code - (char *)cfg->native_code, 
		MONO_PATCH_INFO_METHODCONST, cfg->method);
  alpha_ldq(code, alpha_a0, alpha_gp, 0);

  alpha_mov1(code, alpha_sp, alpha_a1);

  code = emit_call(cfg, code, MONO_PATCH_INFO_ABS, (gpointer)func);

  if (enable_arguments)
    {
      // Correct stack back by calculated value
      if (stack_area)
	alpha_lda(code, alpha_sp, alpha_sp, stack_area);
      
      g_free(cinfo);
    }

  return code;
}

/*========================= End of Function ========================*/

enum {
  SAVE_NONE,
  SAVE_STRUCT,
  SAVE_R0,
  SAVE_EAX_EDX,
  SAVE_XMM
};

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_instrument_epilog                       */
/*                                                                  */
/* Function     - Create an epilog that will handle the returned    */
/*                values used in instrumentation.                   */
/*                                                                  */
/*------------------------------------------------------------------*/

void*
mono_arch_instrument_epilog_full (MonoCompile *cfg, void *func, void *p, gboolean enable_arguments, gboolean preserve_argument_registers)
{
  unsigned int *code = p;
  int save_mode = SAVE_NONE;
  int offset;
  MonoMethod *method = cfg->method;
  AlphaGotData ge_data;
  int rtype = mono_type_get_underlying_type (mono_method_signature (method)->ret)->type;

  CFG_DEBUG(2) ALPHA_DEBUG("mono_arch_instrument_epilog");
   
   switch (rtype)
     {
     case MONO_TYPE_VOID:
       /* special case string .ctor icall */
       if (strcmp (".ctor", method->name) &&
	   method->klass == mono_defaults.string_class)
	 save_mode = SAVE_R0;
       else
	 save_mode = SAVE_NONE;
       break;
     case MONO_TYPE_I8:
     case MONO_TYPE_U8:
       save_mode = SAVE_R0;
       break;
     case MONO_TYPE_R4:
     case MONO_TYPE_R8:
       save_mode = SAVE_XMM;
       break;
     case MONO_TYPE_VALUETYPE:
       save_mode = SAVE_STRUCT;
       break;
     default:
       save_mode = SAVE_R0;
       break;
     }

   /* Save the result and copy it into the proper argument register */
   switch (save_mode)
     {
     case SAVE_R0:
       alpha_lda(code, alpha_sp, alpha_sp, -8);
       alpha_stq(code, alpha_r0, alpha_sp, 0);
       
       if (enable_arguments)
	 alpha_mov1(code, alpha_r0, alpha_a1);

       break;
     case SAVE_STRUCT:
       /* FIXME: */
       if (enable_arguments)
	 alpha_lda(code, alpha_a1, alpha_zero, 0);

       break;
     case SAVE_XMM:
       //amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, 8);
       //amd64_movsd_membase_reg (code, AMD64_RSP, 0, AMD64_XMM0);
       /* Align stack */
       //amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, 8);
       /*
	* The result is already in the proper argument register so no copying
	* needed.
	*/
       break;
     case SAVE_NONE:
       break;
     default:
       g_assert_not_reached ();
     }

  offset = (char *)code - (char *)cfg->native_code;

  ge_data.data.p = cfg->method;

  add_got_entry(cfg, GT_PTR, ge_data,
		(char *)code - (char *)cfg->native_code,
		MONO_PATCH_INFO_METHODCONST, cfg->method);
  
  alpha_ldq(code, alpha_a0, alpha_gp, 0);

  code = emit_call(cfg, code, MONO_PATCH_INFO_ABS, (gpointer)func);
   
  /* Restore result */
  switch (save_mode) 
     {
     case SAVE_R0:
       alpha_ldq(code, alpha_r0, alpha_sp, 0);
       alpha_lda(code, alpha_sp, alpha_sp, 8);
       break;
     case SAVE_STRUCT:
       /* FIXME: */
       break;
     case SAVE_XMM:
       //amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, 8);
       //amd64_movsd_reg_membase (code, AMD64_XMM0, AMD64_RSP, 0);
       //amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, 8);
       break;
     case SAVE_NONE:
       break;
     default:
       g_assert_not_reached ();
     }
   
   return code;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_allocate_vars                           */
/*                                                                  */
/* Function     - Set var information according to the calling      */
/*                convention for Alpha. The local var stuff should  */
/*                most likely be split in another method.           */
/*                                                                  */
/* Parameter    - @m - Compile unit.                                */
/*
 * This method is called right before working with BBs. Conversion to
 * IR was done and some analises what registers would be used.
 * Collect info about registers we used - if we want to use a register
 * we need to allocate space for it and save on the stack in method
 * prolog.
 * 
 * Alpha calling convertion:
 * FP -> Stack top <- SP
 * 0:    Stack params to call others
 *
 *       RA               <- arch.params_stack_size
 *       old FP
 * 
 *       [LMF info]       <- arch.lmf_offset
 * .
 *       [possible return values allocated on stack]
 *
 * .     [locals]
 * .
 * .     caller saved regs <- arch.reg_save_area_offset
 * .     a0                <- arch.args_save_area_offset
 * .     a1
 * .     a2
 * .     a3
 * .     a4
 * .     a5
 * ------------------------
 * .     a6 - passed args on stack
 * .
 */
/*------------------------------------------------------------------*/

void
mono_arch_allocate_vars (MonoCompile *cfg)
{
   MonoMethodSignature *sig;
   MonoMethodHeader *header;
   MonoInst *inst;
   int i, offset = 0, a_off = 0;
   guint32 locals_stack_size, locals_stack_align = 0;
   gint32 *offsets;
   CallInfo *cinfo;
   
   CFG_DEBUG(2) ALPHA_DEBUG("mono_arch_allocate_vars");
   
   header = cfg->header;
   
   sig = mono_method_signature (cfg->method);
   
   cinfo = get_call_info (cfg->generic_sharing_context, sig, FALSE);
   
   /* if (cfg->arch.omit_fp) {
      cfg->flags |= MONO_CFG_HAS_SPILLUP;
      cfg->frame_reg = AMD64_RSP;
      offset = 0;
      }
      else */
   {
     /* Locals are allocated forwards from FP. After
      * RA (offset 0), FP (offset 8) and ret value, locals, A0-A5
      * (starting from offset 16).
      * FIXME: Check there Arg6...Argn are supposed to be
      */
     cfg->frame_reg = alpha_fp;
     //     offset = MONO_ALPHA_VARS_OFFSET;
   }

   CFG_DEBUG(3) g_print ("ALPHA: Size for call params is %d(%x)\n",
                         cfg->arch.params_stack_size, cfg->arch.params_stack_size);
   offset += cfg->arch.params_stack_size;

   offset += 16;    // Size to save RA & FP

   if (cfg->method->save_lmf)
     {
       /* Reserve stack space for saving LMF + argument regs */
       guint32 size = sizeof (MonoLMF);

       //if (lmf_tls_offset == -1)
       //	 /* Need to save argument regs too */
       //	 size += (AMD64_NREG * 8) + (8 * 8);

       cfg->arch.lmf_offset = offset;
       offset += size;

       CFG_DEBUG(3) g_print ("ALPHA: Method %s needs LMF. Offset: %x, Size: %x\n",
			     cfg->method->name, cfg->arch.lmf_offset, size);
     }
   
   if (sig->ret->type != MONO_TYPE_VOID)
     {
       switch (cinfo->ret.storage)
	 {
	 case ArgInIReg:
	 case ArgInFloatReg:
	 case ArgInDoubleReg:
	   if ((MONO_TYPE_ISSTRUCT (sig->ret) &&
		!mono_class_from_mono_type (sig->ret)->enumtype) ||
	       (sig->ret->type == MONO_TYPE_TYPEDBYREF))
	     {
	       /* The register is volatile */
	       cfg->ret->opcode = OP_REGOFFSET;
	       cfg->ret->inst_basereg = cfg->frame_reg;

	       /*if (cfg->arch.omit_fp) {
		 cfg->ret->inst_offset = offset;
		 offset += 8;
		 } else */
	       {
		 cfg->ret->inst_offset = offset;
		 CFG_DEBUG(3) g_print ("ALPHA: Return offset is %x\n", offset);
		 offset += 8;
	       }
	     }
	   else
	     {
	       cfg->ret->opcode = OP_REGVAR;
	       cfg->ret->inst_c0 = cinfo->ret.reg;
	     }
	   break;
	 case ArgValuetypeInReg:
	   /* Allocate a local to hold the result, the epilog will
	      copy it to the correct place */
	   // g_assert (!cfg->arch.omit_fp);
	   offset += 16;
	   cfg->ret->opcode = OP_REGOFFSET;
	   cfg->ret->inst_basereg = cfg->frame_reg;
	   cfg->ret->inst_offset = offset;
	   break;
	 default:
	   g_assert_not_reached ();
	 }
       cfg->ret->dreg = cfg->ret->inst_c0;
     }
   
   /* Allocate locals */
   offsets = mono_allocate_stack_slots (cfg,
					     /*cfg->arch.omit_fp ? FALSE:*/ TRUE, 
					     &locals_stack_size,
					     &locals_stack_align);
   
   //g_assert((locals_stack_size % 8) == 0);
   if (locals_stack_size % 8)
     {
       locals_stack_size += 8 - (locals_stack_size % 8);
     }

   /*   if (locals_stack_align)
     {
       offset += (locals_stack_align - 1);
       offset &= ~(locals_stack_align - 1);
     }
   */

   cfg->arch.localloc_offset = offset;
   
   CFG_DEBUG(3) g_print ("ALPHA: Locals start offset is %d(%x)\n", offset, offset);
   CFG_DEBUG(3) g_print ("ALPHA: Locals size is %d(%x)\n",
			  locals_stack_size, locals_stack_size);

   for (i = cfg->locals_start; i < cfg->num_varinfo; i++)
     {
       if (offsets [i] != -1) {
	 MonoInst *inst = cfg->varinfo [i];
	 inst->opcode = OP_REGOFFSET;
	 inst->inst_basereg = cfg->frame_reg;
	 //if (cfg->arch.omit_fp)
	 //        inst->inst_offset = (offset + offsets [i]);
	 //else
	 inst->inst_offset = (offset + (locals_stack_size - offsets [i]));

	 CFG_DEBUG(3) g_print ("ALPHA: allocated local %d to ", i);
	 CFG_DEBUG(3) mono_print_tree_nl (inst);
       }
     }
   
   // TODO check how offsets[i] are calculated
   // it seems they are points to the end on data. Like 8, but it actually - 0

   offset += locals_stack_size; //+8;
   
   if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG)) {
     //                g_assert (!cfg->arch.omit_fp);
     g_assert (cinfo->sig_cookie.storage == ArgOnStack);
     cfg->sig_cookie = cinfo->sig_cookie.offset + ARGS_OFFSET;
   }

   // Save offset for caller saved regs
   cfg->arch.reg_save_area_offset = offset;

   CFG_DEBUG(3) g_print ("ALPHA: reg_save_area_offset at %d(%x)\n", offset, offset);
   
   // Reserve space for caller saved registers 
   for (i = 0; i < MONO_MAX_IREGS; ++i)
     if ((ALPHA_IS_CALLEE_SAVED_REG (i)) &&
	 (cfg->used_int_regs & (1 << i)))
       {
	 offset += sizeof (gpointer);
       }

   // Save offset to args regs
   cfg->arch.args_save_area_offset = offset;

   CFG_DEBUG(3) g_print ("ALPHA: args_save_area_offset at %d(%x)\n", offset, offset);

   for (i = 0; i < sig->param_count + sig->hasthis; ++i)
     {
       ArgInfo *ainfo = &cinfo->args [i];

       switch(ainfo->storage)
	 {
	 case ArgInIReg:
	 case ArgInFloatReg:
	 case ArgInDoubleReg:
	   offset += sizeof (gpointer);
	   break;
	 case ArgAggregate:
	   offset += ainfo->nregs * sizeof (gpointer);
	   break;
	 default:
	   ;
	 }
     }

   CFG_DEBUG(3) g_print ("ALPHA: Stack size is %d(%x)\n",
			  offset, offset);
   
   // Reserve space for method params
   for (i = 0; i < sig->param_count + sig->hasthis; ++i)
     {
       inst = cfg->args [i];

       if (inst->opcode != OP_REGVAR)
	 {
	   ArgInfo *ainfo = &cinfo->args [i];
	   gboolean inreg = TRUE;
	   MonoType *arg_type;
		 
	   if (sig->hasthis && (i == 0))
	     arg_type = &mono_defaults.object_class->byval_arg;
	   else
	     arg_type = sig->params [i - sig->hasthis];
		 
	   /* FIXME: Allocate volatile arguments to registers */
	   if (inst->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT))
	     inreg = FALSE;
		 
	   /*
	    * Under AMD64, all registers used to pass arguments to functions
	    * are volatile across calls. For Alpha too.
	    * FIXME: Optimize this.
	    */
		 
	   // Let's 
	   if (inreg && (ainfo->storage == ArgInIReg)
	       //&& cfg->used_int_regs & (1 << ainfo->reg)
	       )
	     inreg = FALSE;
		 
	   if (//(ainfo->storage == ArgInIReg) ||
	       (ainfo->storage == ArgInFloatReg) ||
	       (ainfo->storage == ArgInDoubleReg) ||
	       (ainfo->storage == ArgValuetypeInReg))
	     inreg = FALSE;
		 
	   inst->opcode = OP_REGOFFSET;
		 
	   switch (ainfo->storage)
	     {
	     case ArgInIReg:
	     case ArgInFloatReg:
	     case ArgInDoubleReg:
	       inst->opcode = OP_REGVAR;
	       inst->dreg = ainfo->reg;
	       break;
	     case ArgOnStack:
	       // g_assert (!cfg->arch.omit_fp);
	       inst->opcode = OP_REGOFFSET;
	       inst->inst_basereg = cfg->frame_reg;
	       
	       // "offset" here will point to the end of
	       // array of saved ret,locals, args
	       // Ideally it would point to "a7"
	       inst->inst_offset = ainfo->offset + offset;
	       break;
	     case ArgValuetypeInReg:
	       break;
	     case ArgAggregate:
	       inreg = FALSE;
	       break;

	     default:
	       NOT_IMPLEMENTED;
	     }

	   if (!inreg && (ainfo->storage != ArgOnStack))
	     {
	       inst->opcode = OP_REGOFFSET;
	       inst->inst_basereg = cfg->frame_reg;
	       
	       /* These arguments are saved to the stack in the prolog */
	       /*if (cfg->arch.omit_fp) {
		 inst->inst_offset = offset;
		 offset += (ainfo->storage == ArgValuetypeInReg) ?
		 2 * sizeof (gpointer) : sizeof (gpointer);
		 } else */
	       {
		 // offset += (ainfo->storage == ArgValuetypeInReg) ?
		 // 2 * sizeof (gpointer) : sizeof (gpointer);

		 inst->inst_offset = cfg->arch.args_save_area_offset + a_off;
		 switch(ainfo->storage)
		   {
		   case ArgAggregate:
		     a_off += ainfo->nslots * 8;
		     break;
		   default:
		     a_off += sizeof (gpointer);
		   }
		//   (/*(ainfo->reg - 16)*/ i * 8);
	       }
	     }
	 }
    }
   
   cfg->stack_offset = offset;
   
   g_free (cinfo);
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - mono_arch_print_tree                              */
/*                                                                  */
/* Function     - Print platform-specific opcode details.           */
/*                                                                  */
/* Returns      - 1 - opcode details have been printed              */
/*                0 - opcode details have not been printed          */
/*                                                                  */
/*------------------------------------------------------------------*/

gboolean
mono_arch_print_tree (MonoInst *tree, int arity)
{
   gboolean done;
   
   ALPHA_DEBUG("mono_arch_print_tree");
   
   switch (tree->opcode) {
	default:
	  done = 0;
   }
   return (done);
}

/*========================= End of Function ========================*/

/*
**
** mono_arch_get_vcall_slot_addr
**  is called by mono_magic_trampoline to determine that the JIT compiled
**  method is called via vtable slot. We need to analyze call sequence
**  and determine that. In case it is true - we need to return address
**  of vtable slot.
**
** code - points to the next instruction after call
** reg - points to saved regs before the call (this is done
**        by mono_magic_trampoline function
*/

gpointer*
mono_arch_get_vcall_slot_addr (guint8* code, mgreg_t *regs)
{
  unsigned int *pc = (unsigned int *)code;
  guint32 reg, disp;
  int     start_index = -2;

  ALPHA_PRINT g_debug("ALPHA_CHECK: [mono_arch_get_vcall_slot_addr] code: %p regs: %p",
	  pc, regs);

  // Check if we have parameters on stack
  if ((pc[-2] & 0xFFFF0000) == 0x23DE0000)     // lda     sp,-n(sp)
    start_index = -3;

  // Check for (call_membase):
  // -4: mov     v0,a0        - load this ???
  // -3: ldq     v0,0(v0)     - load vtable 
  // -2: ldq     t12,64(v0)   - load method (object->vtable->vtable[method->slot])
  if ((pc[start_index-1] & 0xFC00FFFF) == 0xA4000000 &&
      (pc[start_index] & 0xFFFF0000) == 0xA7600000
      )
    {
      disp = pc[start_index] & 0xFFFF;
      reg = (pc[start_index-1] >> AXP_REG1_SHIFT) & AXP_REG_MASK;
      //reg = 0; // For now

      ALPHA_PRINT g_debug("ALPHA_CHECK: [mono_arch_get_vcall_slot_addr callvirt] call_membase");

      return (gpointer)(((guint64)(regs [reg])) + disp);
    }

  // Check for interface call
  // -5: mov     v0,a0
  // -4: ldq     v0,0(v0)
  // -3: ldq     v0,-n(v0)
  // -2: ldq     t12,0(v0)
  if ((pc[start_index-2] & 0xFC00FFFF) == 0xA4000000 &&
      (pc[start_index-1] & 0xFFFF0000) == 0xA4000000 &&
      (pc[start_index] & 0xFFFF0000) == 0xA7600000
      )
    {
      disp = pc[start_index] & 0xFFFF;;
      reg = 0; // For now

      ALPHA_PRINT g_debug("ALPHA_CHECK: [mono_arch_get_vcall_slot_addr interf callvir] call_membase");

      return (gpointer)(((guint64)(regs [reg])) + disp);
    }

  return 0;
}

gpointer
mono_arch_get_this_arg_from_call (MonoGenericSharingContext *gsctx, MonoMethodSignature *sig, mgreg_t *regs, guint8 *code)
{
  unsigned int *pc = (unsigned int *)code;

  ALPHA_PRINT g_debug("ALPHA_CHECK: [mono_arch_get_this_arg_from_call] code: %p regs: %p",
          pc, regs);

        if (MONO_TYPE_ISSTRUCT (sig->ret))
                return (gpointer)regs [alpha_a1];
        else
                return (gpointer)regs [alpha_a0];
}

gpointer
mono_arch_get_delegate_invoke_impl (MonoMethodSignature *sig, gboolean has_target)
{
	unsigned int *code, *start;
        MonoDomain *domain = mono_domain_get ();
        int i;

  	ALPHA_PRINT g_debug("ALPHA_CHECK: [mono_arch_get_delegate_invoke_impl]");

        /* FIXME: Support more cases */
        if (MONO_TYPE_ISSTRUCT (sig->ret))
                return NULL;

	return NULL;
}

guint32
mono_arch_get_patch_offset (guint8 *code)
{
  return 3;
}

gpointer
mono_arch_context_get_int_reg (MonoContext *ctx, int reg)
{
	/* FIXME: implement */
	g_assert_not_reached ();
}
