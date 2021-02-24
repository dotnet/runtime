/**
 * @file
 * @author     - Neale Ferguson (Neale.Ferguson@SoftwareAG-usa.com)
 *
 * @section description
 * Function    - S/390 backend for the Mono code generator.
 *
 * Date        - January, 2004
 *
 * Derivation  - From mini-x86 & mini-ppc by -
 * 	         Paolo Molaro (lupus@ximian.com)
 * 		 Dietmar Maurer (dietmar@ximian.com)
 *
 */

/*------------------------------------------------------------------*/
/*                 D e f i n e s                                    */
/*------------------------------------------------------------------*/

#define MAX_ARCH_DELEGATE_PARAMS 10

#define EMIT_COND_BRANCH(ins,cond) 						\
{										\
if (ins->inst_true_bb->native_offset) { 					\
	int displace;								\
	displace = ((cfg->native_code + 					\
		    ins->inst_true_bb->native_offset) - code) / 2;		\
	if (s390_is_imm16(displace)) {						\
		s390_brc (code, cond, displace);				\
	} else { 								\
		s390_jcl (code, cond, displace); 				\
	}									\
} else { 									\
	mono_add_patch_info (cfg, code - cfg->native_code, 			\
			     MONO_PATCH_INFO_BB, ins->inst_true_bb); 		\
	s390_jcl (code, cond, 0);						\
} 										\
}

#define EMIT_UNCOND_BRANCH(ins) 						\
{										\
if (ins->inst_target_bb->native_offset) { 					\
	int displace;								\
	displace = ((cfg->native_code + 					\
		    ins->inst_target_bb->native_offset) - code) / 2;		\
	if (s390_is_imm16(displace)) {						\
		s390_brc (code, S390_CC_UN, displace);				\
	} else { 								\
		s390_jcl (code, S390_CC_UN, displace); 				\
	}									\
} else { 									\
	mono_add_patch_info (cfg, code - cfg->native_code, 			\
			     MONO_PATCH_INFO_BB, ins->inst_target_bb); 		\
	s390_jcl (code, S390_CC_UN, 0);						\
} 										\
}

#define EMIT_COND_SYSTEM_EXCEPTION(cond,exc_name)            		\
        do {                                                        	\
		mono_add_patch_info (cfg, code - cfg->native_code,   	\
				     MONO_PATCH_INFO_EXC, exc_name);  	\
		s390_jcl (code, cond, 0);				\
	} while (0); 

#define EMIT_COMP_AND_BRANCH(ins, cab, cmp)					\
{										\
if (ins->inst_true_bb->native_offset) { 					\
	int displace;								\
	displace = ((cfg->native_code + 					\
		    ins->inst_true_bb->native_offset) - code) / 2;		\
	if (s390_is_imm16(displace)) {						\
		s390_##cab (code, ins->sreg1, ins->sreg2, 			\
			    ins->sreg3, displace);				\
	} else { 								\
		s390_##cmp (code, ins->sreg1, ins->sreg2);			\
		displace = ((cfg->native_code + 				\
			    ins->inst_true_bb->native_offset) - code) / 2;	\
		s390_jcl (code, ins->sreg3, displace); 				\
	}									\
} else { 									\
	s390_##cmp (code, ins->sreg1, ins->sreg2);				\
	mono_add_patch_info (cfg, code - cfg->native_code, 			\
			     MONO_PATCH_INFO_BB, ins->inst_true_bb); 		\
	s390_jcl (code, ins->sreg3, 0);						\
} 										\
}

#define EMIT_COMP_AND_BRANCH_IMM(ins, cab, cmp, lat, logical)			\
{										\
if (ins->inst_true_bb->native_offset) { 					\
	int displace;								\
	if ((ins->backend.data == 0) && (!logical)) {				\
		s390_##lat (code, ins->sreg1, ins->sreg1);			\
		displace = ((cfg->native_code + 				\
			    ins->inst_true_bb->native_offset) - code) / 2;	\
		if (s390_is_imm16(displace)) {					\
			s390_brc (code, ins->sreg3, displace); 			\
		} else {							\
			s390_jcl (code, ins->sreg3, displace); 			\
		}								\
	} else { 								\
		S390_SET (code, s390_r0, ins->backend.data);			\
		displace = ((cfg->native_code + 				\
			    ins->inst_true_bb->native_offset) - code) / 2;	\
		if (s390_is_imm16(displace)) {					\
			s390_##cab (code, ins->sreg1, s390_r0,    		\
				    ins->sreg3, displace);			\
		} else { 							\
			s390_##cmp (code, ins->sreg1, s390_r0);			\
			displace = ((cfg->native_code + 			\
			    ins->inst_true_bb->native_offset) - code) / 2;	\
			s390_jcl (code, ins->sreg3, displace); 			\
		}								\
	}									\
} else { 									\
	if ((ins->backend.data == 0) && (!logical)) {				\
		s390_##lat (code, ins->sreg1, ins->sreg1);			\
	} else {								\
		S390_SET (code, s390_r0, ins->backend.data);			\
		s390_##cmp (code, ins->sreg1, s390_r0);				\
	}									\
	mono_add_patch_info (cfg, code - cfg->native_code, 			\
			     MONO_PATCH_INFO_BB, ins->inst_true_bb); 		\
	s390_jcl (code, ins->sreg3, 0);						\
} 										\
}

#define CHECK_SRCDST_COM						\
	if (ins->dreg == ins->sreg2) {					\
		src2 = ins->sreg1;					\
	} else {							\
		src2 = ins->sreg2;					\
		if (ins->dreg != ins->sreg1) {				\
			s390_lgr (code, ins->dreg, ins->sreg1);		\
		}							\
	}

#define CHECK_SRCDST_NCOM						\
	if (ins->dreg == ins->sreg2) {					\
		src2 = s390_r13;					\
		s390_lgr (code, s390_r13, ins->sreg2);			\
	} else {							\
		src2 = ins->sreg2;					\
	}								\
	if (ins->dreg != ins->sreg1) {					\
		s390_lgr (code, ins->dreg, ins->sreg1);			\
	}

#define CHECK_SRCDST_COM_I						\
	if (ins->dreg == ins->sreg2) {					\
		src2 = ins->sreg1;					\
	} else {							\
		src2 = ins->sreg2;					\
		if (ins->dreg != ins->sreg1) {				\
			s390_lgfr (code, ins->dreg, ins->sreg1);	\
		}							\
	}

#define CHECK_SRCDST_NCOM_I						\
	if (ins->dreg == ins->sreg2) {					\
		src2 = s390_r13;					\
		s390_lgfr (code, s390_r13, ins->sreg2);			\
	} else {							\
		src2 = ins->sreg2;					\
	}								\
	if (ins->dreg != ins->sreg1) {					\
		s390_lgfr (code, ins->dreg, ins->sreg1);		\
	}

#define CHECK_SRCDST_COM_F						\
	if (ins->dreg == ins->sreg2) {					\
		src2 = ins->sreg1;					\
	} else {							\
		src2 = ins->sreg2;					\
		if (ins->dreg != ins->sreg1) {				\
			s390_ldr (code, ins->dreg, ins->sreg1);		\
		}							\
	}

#define CHECK_SRCDST_NCOM_F(op)						\
	if (ins->dreg == ins->sreg2) {					\
		s390_lgdr (code, s390_r0, s390_f15);			\
		s390_ldr (code, s390_f15, ins->sreg2);			\
		if (ins->dreg != ins->sreg1) {				\
			s390_ldr (code, ins->dreg, ins->sreg1);		\
		}							\
		s390_ ## op (code, ins->dreg, s390_f15);		\
		s390_ldgr (code, s390_f15, s390_r0);			\
	} else {							\
		if (ins->dreg != ins->sreg1) {				\
			s390_ldr (code, ins->dreg, ins->sreg1);		\
		}							\
		s390_ ## op (code, ins->dreg, ins->sreg2);		\
	}

#define CHECK_SRCDST_NCOM_FR(op, m)					\
	s390_lgdr (code, s390_r1, s390_f14);				\
	if (ins->dreg == ins->sreg2) {					\
		s390_lgdr (code, s390_r0, s390_f15);			\
		s390_ldr (code, s390_f15, ins->sreg2);			\
		if (ins->dreg != ins->sreg1) {				\
			s390_ldr (code, ins->dreg, ins->sreg1);		\
		}							\
		s390_ ## op (code, ins->dreg, s390_f15, m, s390_f14);	\
		s390_ldgr (code, s390_f15, s390_r0);			\
	} else {							\
		if (ins->dreg != ins->sreg1) {				\
			s390_ldr (code, ins->dreg, ins->sreg1);		\
		}							\
		s390_ ## op (code, ins->dreg, ins->sreg2, m, s390_f14); \
	}								\
	s390_ldgr (code, s390_f14, s390_r1);			

#undef DEBUG
#define DEBUG(a) if (cfg->verbose_level > 1) a

#define MAX_EXC	16

#define S390_TRACE_STACK_SIZE (5*sizeof(gpointer)+4*sizeof(gdouble))

#define MAX(a, b) ((a) > (b) ? (a) : (b))

/*
 * imt trampoline size values
 */
#define CMP_SIZE 	24
#define LOADCON_SIZE	20
#define LOAD_SIZE	6
#define BR_SIZE		2
#define JUMP_SIZE	6
#define ENABLE_WRONG_METHOD_CHECK 0

/*========================= End of Defines =========================*/

/*------------------------------------------------------------------*/
/*                 I n c l u d e s                                  */
/*------------------------------------------------------------------*/

#include "mini.h"
#include <string.h>
#include <sys/types.h>
#include <unistd.h>

#include <mono/metadata/abi-details.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/profiler-private.h>
#include <mono/utils/mono-error.h>
#include <mono/utils/mono-error-internals.h>
#include <mono/utils/mono-math.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/mono-hwcap.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/unlocked.h>

#include "mini-s390x.h"
#include "cpu-s390x.h"
#include "support-s390x.h"
#include "jit-icalls.h"
#include "ir-emit.h"
#include "mini-gc.h"
#include "aot-runtime.h"
#include "mini-runtime.h"

/*========================= End of Includes ========================*/

/*------------------------------------------------------------------*/
/*                 T y p e d e f s                                  */
/*------------------------------------------------------------------*/

/**
 * Track stack use
 */
typedef struct {
	guint stack_size,
	      code_size,
	      parm_size,
	      retStruct;
} size_data;	

/**
 * ABI - register use in calls etc.
 */
typedef enum {
	RegTypeGeneral,
	RegTypeBase,
	RegTypeFP,
	RegTypeFPR4,
	RegTypeStructByVal,
	RegTypeStructByValInFP,
	RegTypeStructByAddr
} ArgStorage;

/**
 * Track method arguments
 */
typedef struct {
	gint32  offset;		/* offset from caller's stack */
	guint16 vtsize; 	/* in param area */
	guint8  reg;
	ArgStorage regtype;
	guint32 size;        	/* Size of structure used by RegTypeStructByVal */
	gint32  type;		/* Data type of argument */
} ArgInfo;

/**
 * Call information - parameters and stack use for s390x ABI
 */
struct CallInfo {
	int nargs;
	int lastgr;
	guint32 stack_usage;
	guint32 struct_ret;
	ArgInfo ret;
	ArgInfo sigCookie;
	size_data sz;
	int vret_arg_index;
	MonoMethodSignature *sig;
	ArgInfo args [1];
};

/**
 * Registers used in parameter passing
 */
typedef struct {
	gint64	gr[5];		/* R2-R6			    */
	gdouble fp[3];		/* F0-F2			    */
} __attribute__ ((__packed__)) RegParm;

/*========================= End of Typedefs ========================*/

/*------------------------------------------------------------------*/
/*                   P r o t o t y p e s                            */
/*------------------------------------------------------------------*/

static guint8 * backUpStackPtr(MonoCompile *, guint8 *);
static void add_general (guint *, size_data *, ArgInfo *);
static void add_stackParm (guint *, size_data *, ArgInfo *, gint, ArgStorage);
static void add_float (guint *, size_data *, ArgInfo *, gboolean);
static CallInfo * get_call_info (MonoMemPool *, MonoMethodSignature *);
static guchar * emit_float_to_int (MonoCompile *, guchar *, int, int, int, gboolean);
static __inline__ void emit_unwind_regs(MonoCompile *, guint8 *, int, int, long);
static void compare_and_branch(MonoBasicBlock *, MonoInst *, int, gboolean);

/*========================= End of Prototypes ======================*/

/*------------------------------------------------------------------*/
/*                 G l o b a l   V a r i a b l e s                  */
/*------------------------------------------------------------------*/

/**
 * The single-step trampoline
 */
static gpointer ss_trampoline;

/**
 * The breakpoint trampoline
 */
static gpointer bp_trampoline;

/**
 * Constants used in debugging - map general register names
 */
static const char * grNames[] = {
	"s390_r0", "s390_sp", "s390_r2", "s390_r3", "s390_r4",
	"s390_r5", "s390_r6", "s390_r7", "s390_r8", "s390_r9",
	"s390_r10", "s390_r11", "s390_r12", "s390_r13", "s390_r14",
	"s390_r15"
};

/**
 * Constants used in debugging - map floating point register names
 */
static const char * fpNames[] = {
	"s390_f0", "s390_f1", "s390_f2", "s390_f3", "s390_f4",
	"s390_f5", "s390_f6", "s390_f7", "s390_f8", "s390_f9",
	"s390_f10", "s390_f11", "s390_f12", "s390_f13", "s390_f14",
	"s390_f15"
};

/**
 * Constants used in debugging - map vector register names
 */
static const char * vrNames[] = {
	"vr0",  "vr1",  "vr2",  "vr3",  "vr4",  "vr5",  "vr6",  "vr7", 
	"vr8",  "vr9",  "vr10", "vr11", "vr12", "vr13", "vr14", "vr15",
	"vr16", "vr17", "vr18", "vr19", "vr20", "vr21", "vr22", "vr23",
	"vr24", "vr25", "vr26", "vr27", "vr28", "vr29", "vr30", "vr31"
};

#if 0
/**
 * Constants used in debugging - ABI register types
 */
static const char *typeParm[] = { "General", "Base", "FPR8", "FPR4", "StructByVal", 
                                  "StructByValInFP", "ByAddr"};
#endif

/*====================== End of Global Variables ===================*/

/**
 *  
 * @brief Return general register name
 * 
 * @param[in] register number
 * @returns Name of register
 *
 * Returns the name of the general register specified by the input parameter.
 */

const char*
mono_arch_regname (int reg) 
{
	if (reg >= 0 && reg < 16)
		return grNames [reg];
	else
		return "unknown";
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Return floating point register name
 * 
 * @param[in] register number
 * @returns Name of register
 *
 * Returns the name of the FP register specified by the input parameter.
 */

const char*
mono_arch_fregname (int reg) 
{
	if (reg >= 0 && reg < 16)
		return fpNames [reg];
	else
		return "unknown";
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Return vector register name
 * 
 * @param[in] register number
 * @returns Name of register
 *
 * Returns the name of the vector register specified by the input parameter.
 */

const char *
mono_arch_xregname (int reg)
{
	if (reg < s390_VR_NREG)
		return vrNames [reg];
	else
		return "unknown";
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecture-specific return argument information
 * 
 * @param[in] @csig - Method signature
 * @param[in] @param_count - Number of parameters to consider
 * @param[out] @arg_info - An array in which to store results
 * @returns Size of the activation frame
 *
 * Gathers information on parameters such as size, alignment, and padding. 
 * arg_info should be large * enough to hold param_count + 1 entries.	    
 */

int
mono_arch_get_argument_info (MonoMethodSignature *csig, 
			     int param_count, 
			     MonoJitArgumentInfo *arg_info)
{
	int k, frame_size = 0;
	int size, align, pad;
	int offset = 8;

	if (MONO_TYPE_ISSTRUCT (csig->ret)) { 
		frame_size += sizeof (target_mgreg_t);
		offset += 8;
	}

	arg_info [0].offset = offset;

	if (csig->hasthis) {
		frame_size += sizeof (target_mgreg_t);
		offset += 8;
	}

	arg_info [0].size = frame_size;

	for (k = 0; k < param_count; k++) {
		
		if (csig->pinvoke)
			size = mono_type_native_stack_size (csig->params [k], (guint32 *) &align);
		else
			size = mini_type_stack_size (csig->params [k], &align);

		frame_size += pad = (align - (frame_size & (align - 1))) & (align - 1);	
		arg_info [k].pad = pad;
		frame_size += size;
		arg_info [k + 1].pad = 0;
		arg_info [k + 1].size = size;
		offset += pad;
		arg_info [k + 1].offset = offset;
		offset += size;
	}

	align = MONO_ARCH_FRAME_ALIGNMENT;
	frame_size += pad = (align - (frame_size & (align - 1))) & (align - 1);
	arg_info [k].pad = pad;

	return frame_size;
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Emit an s390x move operation
 * 
 * @param[in] @cfg - MonoCompile control block
 * @param[in] @dr - Destination register
 * @param[in] @ins - Current instruction
 * @param[in] @src - Instruction representing the source of move
 *
 * Emit a move instruction for VT parameters
 */

static void __inline__
emit_new_move(MonoCompile *cfg, int dr, MonoInst *ins, MonoInst *src)
{
	MonoCallInst *call = (MonoCallInst *) ins->inst_p0;
	ArgInfo *ainfo = (ArgInfo *) ins->inst_p1;
	MonoInst *vtcopy = mono_compile_create_var (cfg, m_class_get_byval_arg (src->klass), OP_LOCAL);
	MonoInst *load;
	MonoInst *move; 						
	int size;
	
	if (call->signature->pinvoke) {
		size = mono_type_native_stack_size (m_class_get_byval_arg (src->klass), NULL);
		vtcopy->backend.is_pinvoke = 1;
	} else {
		size = ins->backend.size;
	}

	EMIT_NEW_VARLOADA (cfg, load, vtcopy, vtcopy->inst_vtype);

	MONO_INST_NEW (cfg, move, OP_S390_MOVE);
	move->sreg2	       = load->dreg;
	move->inst_offset  = 0;
	move->sreg1	       = src->dreg;
	move->inst_imm	   = 0;
	move->backend.size = size;	
	MONO_ADD_INS (cfg->cbb, move);	
	if (dr != 0)
		MONO_EMIT_NEW_UNALU(cfg, OP_MOVE, dr, load->dreg);
	else
		MONO_EMIT_NEW_STORE_MEMBASE(cfg, OP_STORE_MEMBASE_REG,
			ainfo->reg, ainfo->offset, load->dreg);
} 

/*========================= End of Function ========================*/

/**
 *  
 * @brief Generate output sequence for VT register parameters
 * 
 * @param[in] @cfg - MonoCompile control block
 * @param[in] @dr - Destination register
 * @param[in] @ins - Current instruction
 * @param[in] @src - Instruction representing the source 
 * 
 * Emit the output of structures for calls whose address is placed in a register.
 */

static void __inline__
emit_outarg_vtr(MonoCompile *cfg, MonoInst *ins, MonoInst *src)
{
	MonoCallInst *call = (MonoCallInst *) ins->inst_p0;
	ArgInfo *ainfo = (ArgInfo *) ins->inst_p1;
	int reg = mono_alloc_preg (cfg);

	switch (ins->backend.size) {
		case 0:
			MONO_EMIT_NEW_ICONST(cfg, reg, 0);
		break;
		case 1:
			MONO_EMIT_NEW_LOAD_MEMBASE_OP(cfg, OP_LOADU1_MEMBASE,
				reg, src->dreg, 0);
		break;
		case 2:
			MONO_EMIT_NEW_LOAD_MEMBASE_OP(cfg, OP_LOADU2_MEMBASE,
				reg, src->dreg, 0);
		break;
		case 4:
			MONO_EMIT_NEW_LOAD_MEMBASE_OP(cfg, OP_LOADI4_MEMBASE,
				reg, src->dreg, 0);
		break;
		case 8:
			MONO_EMIT_NEW_LOAD_MEMBASE_OP(cfg, OP_LOADI8_MEMBASE,
				reg, src->dreg, 0);
		break;
		default: 
			emit_new_move (cfg, reg, ins, src);
	}
	mono_call_inst_add_outarg_reg(cfg, call, reg, ainfo->reg, FALSE);
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Generate output sequence for VT stack parameters
 * 
 * @param[in] @cfg - MonoCompile control block
 * @param[in] @dr - Destination register
 * @param[in] @ins - Current instruction
 * @param[in] @src - Instruction representing the source 
 * 
 * Emit the output of structures for calls whose address is placed on the stack
 */

static void __inline__
emit_outarg_vts(MonoCompile *cfg, MonoInst *ins, MonoInst *src)
{
	ArgInfo *ainfo = (ArgInfo *) ins->inst_p1;
	int tmpr = mono_alloc_preg (cfg); 

	switch (ins->backend.size) {
		case 0:
			MONO_EMIT_NEW_ICONST(cfg, tmpr, 0);
			MONO_EMIT_NEW_STORE_MEMBASE(cfg, OP_STORE_MEMBASE_REG, 
				ainfo->reg, ainfo->offset, tmpr);
		break;
		case 1:
			MONO_EMIT_NEW_LOAD_MEMBASE_OP(cfg, OP_LOADU1_MEMBASE,
				tmpr, src->dreg, 0);
			MONO_EMIT_NEW_STORE_MEMBASE(cfg, OP_STORE_MEMBASE_REG,
				ainfo->reg, ainfo->offset, tmpr);
		break;
		case 2:
			MONO_EMIT_NEW_LOAD_MEMBASE_OP(cfg, OP_LOADU2_MEMBASE,
				tmpr, src->dreg, 0);
			MONO_EMIT_NEW_STORE_MEMBASE(cfg, OP_STORE_MEMBASE_REG,
				ainfo->reg, ainfo->offset, tmpr);
		break;
		case 4:
			MONO_EMIT_NEW_LOAD_MEMBASE_OP(cfg, OP_LOADI4_MEMBASE,
				tmpr, src->dreg, 0);
			MONO_EMIT_NEW_STORE_MEMBASE(cfg, OP_STORE_MEMBASE_REG,
				ainfo->reg, ainfo->offset, tmpr);
		break;
		case 8:
			MONO_EMIT_NEW_LOAD_MEMBASE_OP(cfg, OP_LOADI8_MEMBASE,
				tmpr, src->dreg, 0);
			MONO_EMIT_NEW_STORE_MEMBASE(cfg, OP_STORE_MEMBASE_REG,
				ainfo->reg, ainfo->offset, tmpr);
		break;
		default: {
			emit_new_move (cfg, 0, ins, src);
		}
	}
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Generate unwind information for range of registers
 * 
 * @param[in] @cfg - MonoCompile control block
 * @param[in] @code - Location of code
 * @param[in] @start - Starting register
 * @param[in] @end - Ending register
 * @param[in] @offset - Offset in stack
 * 
 * Emit unwind information for a range of registers.
 */

static void __inline__
emit_unwind_regs(MonoCompile *cfg, guint8 *code, int start, int end, long offset)
{
	int i;

	for (i = start; i <= end; i++) {
		mono_emit_unwind_op_offset (cfg, code, i, offset);
		mini_gc_set_slot_type_from_cfa (cfg, offset, SLOT_NOREF);
		offset += sizeof(gulong);
	}
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Get previous stack frame pointer
 * 
 * @param[in] @cfg - MonoCompile control block
 * @param[in] @code - Location of code
 * @returns Previous stack pointer
 * 
 * Retrieve the stack pointer of the previous frame
 */

static guint8 *
backUpStackPtr(MonoCompile *cfg, guint8 *code)
{
	int stackSize = cfg->stack_usage;

	if (cfg->flags & MONO_CFG_HAS_ALLOCA) {
		s390_lg  (code, STK_BASE, 0, STK_BASE, 0);
	} else {
		if (cfg->frame_reg != STK_BASE)
			s390_lgr (code, STK_BASE, cfg->frame_reg);
		if (s390_is_imm16 (stackSize)) {
			s390_aghi  (code, STK_BASE, stackSize);
		} else if (s390_is_imm32 (stackSize)) {
                        s390_agfi  (code, STK_BASE, stackSize);
        } else {
			while (stackSize > INT_MAX) {
				s390_aghi  (code, STK_BASE, INT_MAX);
				stackSize -= INT_MAX;
			}
			s390_agfi  (code, STK_BASE, stackSize);
		}
	}

	return (code);
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecture-specific CPU initialization
 * 
 * Perform CPU specific initialization to execute managed code.
 */

void
mono_arch_cpu_init (void)
{
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Archictecture specific initialization
 * 
 * 
 * Initialize architecture specific code:
 * - Define trigger pages for debugger
 * - Generate breakpoint code stub
 */

void
mono_arch_init (void)
{
	mono_set_partial_sharing_supported (FALSE);

	if (!mono_aot_only)
		bp_trampoline = mini_get_breakpoint_trampoline();
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecture-specific cleaup code
 * 
 * 
 * Clean up before termination:
 * - Free the trigger pages
 */

void
mono_arch_cleanup (void)
{
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecture-specific check for fast TLS access
 * 
 * @returns True
 * 
 * Returns whether we use fast inlined thread local storage managed access, 
 * instead of falling back to native code.
 */

gboolean
mono_arch_have_fast_tls (void)
{
	return TRUE;
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecture-specific check of mono optimizations
 * 
 * @param[out] @exclude_mask - Optimization exclusion mask
 * @returns Optimizations supported on this CPU
 * 
 * Returns the optimizations supported on this CPU
 */

guint32
mono_arch_cpu_optimizations (guint32 *exclude_mask)
{
	guint32 opts = 0;

	/*
         * No s390-specific optimizations yet
	 */
	*exclude_mask = MONO_OPT_LINEARS;
	return opts;
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecture-specific allocation of integer variables
 * 
 * @param[in] @cfg - MonoCompile control block
 * @returns A list of integer variables
 * 
 * Returns a list of allocatable integer variables
 */

GList *
mono_arch_get_allocatable_int_vars (MonoCompile *cfg)
{
	GList *vars = NULL;
	int i;

	for (i = 0; i < cfg->num_varinfo; i++) {
		MonoInst *ins = cfg->varinfo [i];
		MonoMethodVar *vmv = MONO_VARINFO (cfg, i);

		/* unused vars */
		if (vmv->range.first_use.abs_pos >= vmv->range.last_use.abs_pos)
			continue;

		if (ins->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT) || 
		    (ins->opcode != OP_LOCAL && ins->opcode != OP_ARG))
			continue;

		/* we can only allocate 32 bit values */
		if (mono_is_regsize_var(ins->inst_vtype)) {
			g_assert (MONO_VARINFO (cfg, i)->reg == -1);
			g_assert (i == vmv->idx);
			vars = mono_varlist_insert_sorted (cfg, vars, vmv, FALSE);
		}
	}

	return vars;
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecture-specific determination of usable integer registers
 * 
 * @param[in] @cfg - MonoCompile control block
 * @returns A list of allocatable registers
 * 
 * Returns a list of usable integer registers
 */

GList *
mono_arch_get_global_int_regs (MonoCompile *cfg)
{
	GList *regs = NULL;
	MonoMethodHeader *header;
	int i, top = 13;

	header = cfg->header;
	if ((cfg->flags & MONO_CFG_HAS_ALLOCA) || header->num_clauses)
		cfg->frame_reg = s390_r11;


	/* FIXME: s390_r12 is reserved for bkchain_reg. Only reserve it if needed */
	top = 12;
	for (i = 8; i < top; ++i) {
		if ((cfg->frame_reg != i) && 
		    //!((cfg->uses_rgctx_reg) && (i == MONO_ARCH_IMT_REG)))
		    (i != MONO_ARCH_IMT_REG))
			regs = g_list_prepend (regs, GUINT_TO_POINTER (i));
	}

	return regs;
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecture-specific flush of instruction cache
 * 
 * @param[in] @code - Start of code
 * @param[in] @size - Amount to be flushed
 * 
 * Flush the CPU icache.
 */

void
mono_arch_flush_icache (guint8 *code, gint size)
{
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Add an integer register parameter 
 * 
 * @param[in] @gr - Address of current register number
 * @param[in] @sz - Stack size data
 * @param[in] @ainfo - Parameter information
 * 
 * Assign a parameter to a general register or spill it onto the stack
 */

static void inline
add_general (guint *gr, size_data *sz, ArgInfo *ainfo)
{
	if (*gr > S390_LAST_ARG_REG) {
		sz->stack_size  = S390_ALIGN(sz->stack_size, sizeof(long));
		ainfo->offset   = sz->stack_size;
		ainfo->reg	    = STK_BASE;
		ainfo->regtype  = RegTypeBase;
		sz->stack_size += sizeof(long);
		sz->code_size  += 12;    
	} else {
		ainfo->reg      = *gr;
		ainfo->regtype  = RegTypeGeneral;
		sz->code_size  += 8;    
	}
	(*gr) ++;
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Add a structure variable to parameter list
 * 
 * @param[in] @gr - Address of current register number
 * @param[in] @sz - Stack size data
 * @param[in] @ainfo - Parameter information
 * @param[in] @size - Size of parameter
 * @param[in] @type - Type of stack parameter (reference or value)
 * 
 * Assign a structure address to a register or spill it onto the stack
 */

static void inline
add_stackParm (guint *gr, size_data *sz, ArgInfo *ainfo, gint size, ArgStorage type)
{
	if (*gr > S390_LAST_ARG_REG) {
		sz->stack_size  = S390_ALIGN(sz->stack_size, sizeof(long));
		ainfo->reg	= STK_BASE;
                ainfo->offset   = sz->stack_size;
                sz->stack_size += sizeof (target_mgreg_t);
		sz->parm_size  += sizeof(gpointer);
	} else {
		ainfo->reg      = *gr;
	}
	(*gr) ++;
	ainfo->regtype  = type;
	ainfo->size     = size;
	ainfo->vtsize   = size;
	sz->parm_size  += size;
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Add a floating point register parameter
 * 
 * @param[in] @fr - Address of current register number
 * @param[in] @sz - Stack size data
 * @param[in] @ainfo - Parameter information
 * @param[in] @isDouble - Precision of parameter
 * 
 * Assign a parameter to a FP register or spill it onto the stack
 */

static void inline
add_float (guint *fr,  size_data *sz, ArgInfo *ainfo, gboolean isDouble)
{
	if ((*fr) <= S390_LAST_FPARG_REG) {
		if (isDouble)
			ainfo->regtype = RegTypeFP;
		else
			ainfo->regtype = RegTypeFPR4;
		ainfo->reg     = *fr;
		sz->code_size += 4;
		(*fr) += 2;
	}
	else {
		ainfo->offset   = sz->stack_size;
		ainfo->reg      = STK_BASE;
		sz->code_size  += 4;
		sz->stack_size += sizeof(double);
		ainfo->regtype  = RegTypeBase;
	}
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Extract information about call parameters and stack use
 * 
 * @param[in] @mp - Mono Memory Pool
 * @param[in] @sig - Mono Method Signature
 * @returns Information about the parameters and stack usage for a call
 * 
 * Determine the amount of space required for code and stack. In addition 
 * determine starting points for stack-based parameters, and area for 
 * structures being returned on the stack.
 */

static CallInfo *
get_call_info (MonoMemPool *mp, MonoMethodSignature *sig)
{
	guint i, fr, gr, size, pstart;
	int nParm = sig->hasthis + sig->param_count;
	MonoType *ret_type;
	guint32 simpleType, align;
	gboolean is_pinvoke = sig->pinvoke;
	CallInfo *cinfo;
	size_data *sz;

	if (mp)
		cinfo = (CallInfo *) mono_mempool_alloc0 (mp, sizeof (CallInfo) + sizeof (ArgInfo) * nParm);
	else
		cinfo = (CallInfo *) g_malloc0 (sizeof (CallInfo) + sizeof (ArgInfo) * nParm);

	fr                = 0;
	gr                = s390_r2;
	nParm 		  = 0;
	cinfo->struct_ret = 0;
	cinfo->sig	  = sig;
	sz                = &cinfo->sz;
	sz->retStruct     = 0;
	sz->stack_size    = S390_MINIMAL_STACK_SIZE;
	sz->code_size     = 0;
	sz->parm_size     = 0;
	align		  = 0;
	size		  = 0;

	/*----------------------------------------------------------*/
	/* We determine the size of the return code/stack in case we*/
	/* need to reserve a register to be used to address a stack */
	/* area that the callee will use.			    */
	/*----------------------------------------------------------*/

	ret_type = mini_get_underlying_type (sig->ret);
	simpleType = ret_type->type;
enum_retvalue:
	switch (simpleType) {
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_PTR:
		case MONO_TYPE_FNPTR:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_STRING:
			cinfo->ret.reg = s390_r2;
			sz->code_size += 4;
			break;
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
			cinfo->ret.reg = s390_f0;
			sz->code_size += 4;
			break;
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
			cinfo->ret.reg = s390_r2;
			sz->code_size += 4;
			break;
		case MONO_TYPE_GENERICINST:
			if (!mono_type_generic_inst_is_valuetype (sig->ret)) {
				cinfo->ret.reg = s390_r2;
				sz->code_size += 4;
				break;
			}
			/* Fall through */
		case MONO_TYPE_VALUETYPE: {
			MonoClass *klass = mono_class_from_mono_type_internal (sig->ret);
			if (m_class_is_enumtype (klass)) {
				simpleType = mono_class_enum_basetype_internal (klass)->type;
				goto enum_retvalue;
			}
			size = mini_type_stack_size_full (m_class_get_byval_arg (klass), NULL, sig->pinvoke);
	
			cinfo->struct_ret = 1;
			cinfo->ret.size   = size;
			cinfo->ret.vtsize = size;
                        break;
		}
		case MONO_TYPE_TYPEDBYREF: {
			MonoClass *klass = mono_class_from_mono_type_internal (sig->ret);
			size = mini_type_stack_size_full (m_class_get_byval_arg (klass), NULL, sig->pinvoke);
	
			cinfo->struct_ret = 1;
			cinfo->ret.size   = size;
			cinfo->ret.vtsize = size;
			// cinfo->ret.reg = s390_r2;
			// sz->code_size += 4;
	    }
			break;
		case MONO_TYPE_VOID:
			break;
		default:
			g_error ("Can't handle as return value 0x%x", sig->ret->type);
	}


	pstart = 0;
	/*
	 * To simplify get_this_arg_reg () and LLVM integration, emit the vret arg after
	 * the first argument, allowing 'this' to be always passed in the first arg reg.
	 * Also do this if the first argument is a reference type, since virtual calls
	 * are sometimes made using calli without sig->hasthis set, like in the delegate
	 * invoke wrappers.
	 */
	if (cinfo->struct_ret && !is_pinvoke && 
	    (sig->hasthis || 
             (sig->param_count > 0 && 
	      MONO_TYPE_IS_REFERENCE (mini_get_underlying_type (sig->params [0]))))) {
		if (sig->hasthis) {
			cinfo->args[nParm].size = sizeof (target_mgreg_t);
			add_general (&gr, sz, cinfo->args + nParm);
		} else {
			cinfo->args[nParm].size = sizeof (target_mgreg_t);
			add_general (&gr, sz, &cinfo->args [sig->hasthis + nParm]);
			pstart = 1;
		}
		nParm ++;
		cinfo->vret_arg_index = 1;
		cinfo->ret.reg = gr;
		gr ++;
	} else {
		/* this */
		if (sig->hasthis) {
			cinfo->args[nParm].size = sizeof (target_mgreg_t);
			add_general (&gr, sz, cinfo->args + nParm);
			nParm ++;
		}

		if (cinfo->struct_ret) {
			cinfo->ret.reg = gr;
			gr++;
		}
	}

	if ((sig->call_convention == MONO_CALL_VARARG) && (sig->param_count == 0)) {
		gr = S390_LAST_ARG_REG + 1;
		fr = S390_LAST_FPARG_REG + 1;

		/* Emit the signature cookie just before the implicit arguments */
		add_general (&gr, sz, &cinfo->sigCookie);
	}

	/*----------------------------------------------------------*/
	/* We determine the size of the parameter code and stack    */
	/* requirements by checking the types and sizes of the      */
	/* parameters.						    */
	/*----------------------------------------------------------*/

	for (i = pstart; i < sig->param_count; ++i) {
		MonoType *ptype;

		/*--------------------------------------------------*/
		/* Handle vararg type calls. All args are put on    */
		/* the stack.                                       */
		/*--------------------------------------------------*/
		if ((sig->call_convention == MONO_CALL_VARARG) &&
		    (i == sig->sentinelpos)) {
			gr = S390_LAST_ARG_REG + 1;
			fr = S390_LAST_FPARG_REG + 1;
			add_general (&gr, sz, &cinfo->sigCookie);
		}

		if (sig->params [i]->byref) {
			add_general (&gr, sz, cinfo->args+nParm);
			cinfo->args[nParm].size = sizeof(gpointer);
			nParm++;
			continue;
		}

		ptype = mini_get_underlying_type (sig->params [i]);
		simpleType = ptype->type;
		cinfo->args[nParm].type = simpleType;
		switch (simpleType) {
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
			cinfo->args[nParm].size = sizeof(char);
			add_general (&gr, sz, cinfo->args+nParm);
			nParm++;
			break;
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
			cinfo->args[nParm].size = sizeof(short);
			add_general (&gr, sz, cinfo->args+nParm);
			nParm++;
			break;
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
			cinfo->args[nParm].size = sizeof(int);
			add_general (&gr, sz, cinfo->args+nParm);
			nParm++;
			break;
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_PTR:
		case MONO_TYPE_FNPTR:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_STRING:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_ARRAY:
			cinfo->args[nParm].size = sizeof(gpointer);
			add_general (&gr, sz, cinfo->args+nParm);
			nParm++;
			break;
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
			cinfo->args[nParm].size = sizeof(long long);
			add_general (&gr, sz, cinfo->args+nParm);
			nParm++;
			break;
		case MONO_TYPE_R4:
			cinfo->args[nParm].size = sizeof(float);
			add_float (&fr, sz, cinfo->args+nParm, FALSE);
			nParm++;
			break;
		case MONO_TYPE_R8:
			cinfo->args[nParm].size = sizeof(double);
			add_float (&fr, sz, cinfo->args+nParm, TRUE);
			nParm++;
			break;
		case MONO_TYPE_GENERICINST:
			if (!mono_type_generic_inst_is_valuetype (ptype)) {
				cinfo->args[nParm].size = sizeof(gpointer);
				add_general (&gr, sz, cinfo->args+nParm);
				nParm++;
				break;
			}
			/* Fall through */
		case MONO_TYPE_VALUETYPE: {
			MonoMarshalType *info;
			MonoClass *klass = mono_class_from_mono_type_internal (ptype);

			if (sig->pinvoke)
				size = mono_class_native_size(klass, NULL);
			else
				size = mono_class_value_size(klass, NULL);

			if (simpleType != MONO_TYPE_GENERICINST) {
				info = mono_marshal_load_type_info(klass);

				if ((info->native_size == sizeof(float)) &&
				    (info->num_fields  == 1) &&
				    (info->fields[0].field->type->type == MONO_TYPE_R4)) {
					cinfo->args[nParm].size = sizeof(float);
					add_float(&fr, sz, cinfo->args+nParm, FALSE);
					nParm ++;
					break;
				}

				if ((info->native_size == sizeof(double)) &&
				    (info->num_fields  == 1) &&
				    (info->fields[0].field->type->type == MONO_TYPE_R8)) {
					cinfo->args[nParm].size = sizeof(double);
					add_float(&fr, sz, cinfo->args+nParm, TRUE);
					nParm ++;
					break;
				}
			}

			cinfo->args[nParm].vtsize  = 0;
			cinfo->args[nParm].size    = 0;

			switch (size) {
				/*----------------------------------*/
				/* On S/390, structures of size 1,  */
				/* 2, 4, and 8 bytes are passed in  */
				/* (a) register(s).		    */
				/*----------------------------------*/
				case 0:
				case 1:
				case 2:
				case 4:
				case 8:
					add_general(&gr, sz, cinfo->args+nParm);
					cinfo->args[nParm].size    = size;
					cinfo->args[nParm].regtype = RegTypeStructByVal; 
					nParm++;
					break;
				default:
					add_stackParm(&gr, sz, cinfo->args+nParm, size, RegTypeStructByVal);
					nParm++;
			}
		}
			break;
		case MONO_TYPE_TYPEDBYREF: {
			add_stackParm(&gr, sz, cinfo->args+nParm, sizeof(uintptr_t), RegTypeStructByAddr);
			nParm++;
		}
			break;
		default:
			g_error ("Can't trampoline 0x%x", ptype);
		}
	}

	/*----------------------------------------------------------*/
	/* Handle the case where there are no implicit arguments    */
	/*----------------------------------------------------------*/
	if ((sig->call_convention == MONO_CALL_VARARG) &&
	    (nParm > 0) &&
	    (!sig->pinvoke) &&
	    (sig->param_count == sig->sentinelpos)) {
		gr = S390_LAST_ARG_REG + 1;
		fr = S390_LAST_FPARG_REG + 1;
		add_general (&gr, sz, &cinfo->sigCookie);
	}

	/*----------------------------------------------------------*/
	/* If we are passing a structure back then if it won't be   */
	/* in a register(s) then we make room at the end of the     */
	/* parameters that may have been placed on the stack        */
	/*----------------------------------------------------------*/
	if (cinfo->struct_ret) {
		cinfo->ret.offset = sz->stack_size;
		switch (cinfo->ret.size) {
		case 0:
		case 1:
		case 2:
		case 4:
		case 8:
			break;
		default:
			sz->stack_size   += S390_ALIGN(cinfo->ret.size, align);
		}
	}

	cinfo->lastgr   = gr;
	sz->stack_size  = sz->stack_size + sz->parm_size;
	sz->stack_size  = S390_ALIGN(sz->stack_size, sizeof(long));

	return (cinfo);
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecture-specific allocation of variables
 * 
 * @param[in] @cfg - Compile control block
 * 
 * Set var information according to the calling convention for s390x. 
 * 
 */

void
mono_arch_allocate_vars (MonoCompile *cfg)
{
	MonoMethodSignature *sig;
	MonoMethodHeader *header;
	MonoInst *inst;
	CallInfo *cinfo;
	int iParm, iVar, offset, align, size, curinst;
	int frame_reg = STK_BASE;
	int sArg, eArg;

	header  = cfg->header;

	cfg->flags |= MONO_CFG_HAS_SPILLUP;

	/*---------------------------------------------------------*/	 
	/* We use the frame register also for any method that has  */ 
	/* filter clauses. This way, when the handlers are called, */
	/* the code will reference local variables using the frame */
	/* reg instead of the stack pointer: if we had to restore  */
	/* the stack pointer, we'd corrupt the method frames that  */
	/* are already on the stack (since filters get called      */
	/* before stack unwinding happens) when the filter code    */
	/* would call any method.				   */
	/*---------------------------------------------------------*/	 
	if ((cfg->flags & MONO_CFG_HAS_ALLOCA) || header->num_clauses)
		frame_reg = s390_r11;

	cfg->frame_reg = frame_reg;

	cfg->arch.bkchain_reg = -1;

	if (frame_reg != STK_BASE) 
		cfg->used_int_regs |= (1LL << frame_reg);		

	sig   = mono_method_signature_internal (cfg->method);
	
	cinfo = get_call_info (cfg->mempool, sig);

	/*--------------------------------------------------------------*/
	/* local vars are at a positive offset from the stack pointer 	*/
	/* also note that if the function uses alloca, we use s390_r11	*/
	/* to point at the local variables.				*/
	/* add parameter area size for called functions 		*/
	/*--------------------------------------------------------------*/
	if (cfg->param_area == 0)
		offset = S390_MINIMAL_STACK_SIZE;
	else
		offset = cfg->param_area;

	cfg->sig_cookie = 0;

        if (MONO_TYPE_ISSTRUCT(sig->ret)) {
                cfg->ret->opcode = OP_REGVAR;
                cfg->ret->inst_c0 = cfg->ret->dreg = cinfo->ret.reg;
        } else {
                switch (mini_get_underlying_type (sig->ret)->type) {
                case MONO_TYPE_VOID:
                break;
                default:
                        cfg->ret->opcode = OP_REGVAR;
                        cfg->ret->inst_c0 = cfg->ret->dreg = cinfo->ret.reg;
                }
        }

	if (sig->hasthis) {
		inst = cfg->args [0];
		if (inst->opcode != OP_REGVAR) {
			inst->opcode 	   = OP_REGOFFSET;
			inst->inst_basereg = frame_reg;
			offset 		       = S390_ALIGN(offset, sizeof(gpointer));
			inst->inst_offset  = offset;
			offset 		      += sizeof (target_mgreg_t);
		}
		curinst = sArg = 1;
	} else {
		curinst = sArg = 0;
	}

	eArg = sig->param_count + sArg;

	if (sig->call_convention == MONO_CALL_VARARG)
		cfg->sig_cookie += S390_MINIMAL_STACK_SIZE;

	for (iParm = sArg; iParm < eArg; ++iParm) {
		inst = cfg->args [curinst];
		if (inst->opcode != OP_REGVAR) {
			switch (cinfo->args[iParm].regtype) {
			case RegTypeStructByAddr : {
				MonoInst *indir;

				size = sizeof (target_mgreg_t);

				if (cinfo->args [iParm].reg == STK_BASE) {

					/* Similar to the == STK_BASE case below */
					cfg->arch.bkchain_reg = s390_r12;
					cfg->used_int_regs |= 1 << cfg->arch.bkchain_reg;

					inst->opcode = OP_REGOFFSET;
					inst->dreg = mono_alloc_preg (cfg);
					inst->inst_basereg = cfg->arch.bkchain_reg;
					inst->inst_offset = cinfo->args [iParm].offset;
				} else {
					inst->opcode = OP_REGOFFSET;
					inst->dreg   = cinfo->args [iParm].reg;
					inst->opcode = OP_REGOFFSET;
					inst->dreg = mono_alloc_preg (cfg);
					inst->inst_basereg = cfg->frame_reg;
					// inst->inst_offset = cinfo->args [iParm].offset;
					inst->inst_offset = offset;
				}

				/* Add a level of indirection */
				MONO_INST_NEW (cfg, indir, 0);
				*indir = *inst;
				inst->opcode = OP_VTARG_ADDR;
				inst->inst_left = indir;
			}
				break;
			case RegTypeStructByVal : {
				MonoInst *indir;

				cfg->arch.bkchain_reg = s390_r12;
				cfg->used_int_regs |= 1 << cfg->arch.bkchain_reg;
				size = cinfo->args[iParm].size;

				if (cinfo->args [iParm].reg == STK_BASE) {
                                        int offStruct = 0;
                                        switch(size) {
                                        case 0: case 1: case 2: case 4: case 8:
                                                offStruct = (size < 8 ? sizeof(uintptr_t) - size : 0);
                                        default: 
                                                inst->opcode = OP_REGOFFSET;
                                                inst->dreg = mono_alloc_preg (cfg);
                                                inst->inst_basereg = cfg->arch.bkchain_reg;
                                                inst->inst_offset = cinfo->args [iParm].offset + offStruct;
                                        }
				} else {
					offset		   = S390_ALIGN(offset, sizeof(uintptr_t));
					inst->opcode       = OP_REGOFFSET;
					inst->inst_basereg = cfg->frame_reg;
					inst->inst_offset  = offset;
				}
				switch (size) {
				case 0 : case 1 : case 2 : case 4 : case 8 :
					break;
				default :
					/* Add a level of indirection */
					MONO_INST_NEW (cfg, indir, 0);
					*indir = *inst;
					inst->opcode = OP_VTARG_ADDR;
					inst->inst_left = indir;
				}
			}
			break;
			default :
				if (cinfo->args [iParm].reg == STK_BASE) {
					/*
					 * These arguments are in the previous frame, so we can't 
					 * compute their offset from the current frame pointer right
					 * now, since cfg->stack_offset is not yet known, so dedicate a 
					 * register holding the previous frame pointer.
					 */
					cfg->arch.bkchain_reg = s390_r12;
					cfg->used_int_regs |= 1 << cfg->arch.bkchain_reg;

					inst->opcode 	   = OP_REGOFFSET;
					inst->inst_basereg = cfg->arch.bkchain_reg;
					size = (cinfo->args[iParm].size < 8
                                                  ? 8 - cinfo->args[iParm].size
                                                  : 0);
					inst->inst_offset  = cinfo->args [iParm].offset + size;
					size = sizeof (long);
				} else {
					inst->opcode 	   = OP_REGOFFSET;
					inst->inst_basereg = frame_reg;
					size = (cinfo->args[iParm].size < 8
					        ? sizeof(int)  
						: sizeof(long));
					offset = S390_ALIGN(offset, size);
					if (cfg->method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE) 
						inst->inst_offset  = offset;
					else
						inst->inst_offset  = offset + (8 - size);
				}
			}
			offset += MAX(size, 8);
		}
		curinst++;
	}

	cfg->locals_min_stack_offset = offset;

	curinst = cfg->locals_start;
	for (iVar = curinst; iVar < cfg->num_varinfo; ++iVar) {
		inst = cfg->varinfo [iVar];
		if ((inst->flags & MONO_INST_IS_DEAD) || 
		    (inst->opcode == OP_REGVAR))
			continue;

		/*--------------------------------------------------*/
		/* inst->backend.is_pinvoke indicates native sized  */
		/* value types this is used by the pinvoke wrappers */
		/* when they call functions returning structure     */
		/*--------------------------------------------------*/
		if (inst->backend.is_pinvoke && MONO_TYPE_ISSTRUCT (inst->inst_vtype))
			size = mono_class_native_size (mono_class_from_mono_type_internal (inst->inst_vtype), 
						       (guint32 *) &align);
		else
			size = mono_type_size (inst->inst_vtype, &align);

		offset 		   = S390_ALIGN(offset, align);
		inst->inst_offset  = offset;
		inst->opcode 	   = OP_REGOFFSET;
		inst->inst_basereg = frame_reg;
		offset 		  += size;
		DEBUG (g_print("allocating local %d to %ld, size: %d\n", 
				iVar, inst->inst_offset, size));
	}
	offset = S390_ALIGN(offset, sizeof(uintptr_t));

	cfg->locals_max_stack_offset = offset;

	/*------------------------------------------------------*/
	/* Reserve space to save LMF and caller saved registers */
	/*------------------------------------------------------*/
	if (cfg->method->save_lmf)
                offset += sizeof (MonoLMF);

	/*------------------------------------------------------*/
	/* align the offset 					*/
	/*------------------------------------------------------*/
	cfg->stack_offset = S390_ALIGN(offset, S390_STACK_ALIGNMENT);

	/*------------------------------------------------------*/
	/* Fix offsets for args whose value is in parent frame  */
	/*------------------------------------------------------*/
	for (iParm = sArg; iParm < eArg; ++iParm) {
		inst = cfg->args [iParm];

		if (inst->opcode == OP_S390_STKARG) {
			inst->opcode = OP_REGOFFSET;
			inst->inst_offset += cfg->stack_offset;
		}
	}
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecture-specific creation of variables
 * 
 * @param[in] @cfg - Compile control block
 * 
 * Create variables for the method.
 * 
 */

void
mono_arch_create_vars (MonoCompile *cfg)
{
	MonoMethodSignature *sig = mono_method_signature_internal (cfg->method);

	if (MONO_TYPE_ISSTRUCT (sig->ret)) {
		cfg->vret_addr = mono_compile_create_var (cfg, mono_get_int_type (), OP_ARG);
		if (G_UNLIKELY (cfg->verbose_level > 1)) {
			printf ("vret_addr = ");
			mono_print_ins (cfg->vret_addr);
		}
	}

	if (cfg->gen_sdb_seq_points) {
		MonoInst *ins;

		ins = mono_compile_create_var (cfg, mono_get_int_type (), OP_LOCAL);
		ins->flags |= MONO_INST_VOLATILE;
		cfg->arch.ss_tramp_var = ins;

		ins = mono_compile_create_var (cfg, mono_get_int_type (), OP_LOCAL);
		ins->flags |= MONO_INST_VOLATILE;
		cfg->arch.bp_tramp_var = ins;
	}
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Add a register to the call operation
 * 
 * @param[in] @cfg - Compile control block
 * @param[in] @call - Call Instruction
 * @param[in] @storage - Register use type
 * @param[in] @reg - Register number
 * @param[in] @tree - Call arguments
 * 
 * Add register use information to the call sequence
 */

static void
add_outarg_reg2 (MonoCompile *cfg, MonoCallInst *call, ArgStorage storage, int reg, MonoInst *tree)
{
	MonoInst *ins;

	switch (storage) {
	case RegTypeGeneral:
		MONO_INST_NEW (cfg, ins, OP_MOVE);
		ins->dreg = mono_alloc_ireg (cfg);
		ins->sreg1 = tree->dreg;
		MONO_ADD_INS (cfg->cbb, ins);
		mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, reg, FALSE);
		break;
	case RegTypeFP:
		MONO_INST_NEW (cfg, ins, OP_FMOVE);
		ins->dreg = mono_alloc_freg (cfg);
		ins->sreg1 = tree->dreg;
		MONO_ADD_INS (cfg->cbb, ins);
		mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, reg, TRUE);
		break;
	case RegTypeFPR4:
		MONO_INST_NEW (cfg, ins, OP_S390_SETF4RET);
		ins->dreg = mono_alloc_freg (cfg);
		ins->sreg1 = tree->dreg;
		MONO_ADD_INS (cfg->cbb, ins);
		mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, reg, TRUE);
		break;
	default:
		g_assert_not_reached ();
	}
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Emit a signature cookine
 * 
 * @param[in] @cfg - Compile control block
 * @param[in] @call - Call Instruction
 * @param[in] @cinfo - Call Information
 * 
 * Emit the signature cooke as a parameter
 */

static void
emit_sig_cookie (MonoCompile *cfg, MonoCallInst *call, CallInfo *cinfo)
{
	MonoMethodSignature *tmpSig;
	MonoInst *sig_arg;
			
	cfg->disable_aot = TRUE;

	/*
	 * mono_ArgIterator_Setup assumes the signature cookie is
	 * passed first and all the arguments which were before it
	 * passed on the stack after the signature. So compensate
	 * by passing a different signature.
	 */
	tmpSig = mono_metadata_signature_dup (call->signature);
	tmpSig->param_count -= call->signature->sentinelpos;
	tmpSig->sentinelpos  = 0;
	if (tmpSig->param_count > 0)
		memcpy (tmpSig->params, 
			call->signature->params + call->signature->sentinelpos, 
			tmpSig->param_count * sizeof(MonoType *));

	MONO_INST_NEW (cfg, sig_arg, OP_ICONST);
	sig_arg->dreg = mono_alloc_ireg (cfg);
	sig_arg->inst_p0 = tmpSig;
	MONO_ADD_INS (cfg->cbb, sig_arg);

	MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, STK_BASE, 
				     cinfo->sigCookie.offset, sig_arg->dreg);
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecture-specific emission of a call operation
 * 
 * @param[in] @cfg - Compile control block
 * @param[in] @call - Call Instruction
 * 
 * Process all parameters for a call and generate the sequence of 
 * operations to perform the call according to the s390x ABI.
 */

void
mono_arch_emit_call (MonoCompile *cfg, MonoCallInst *call)
{
	MonoInst *in;
	MonoMethodSignature *sig;
	MonoInst *ins;
	int i, n, lParamArea;
	CallInfo *cinfo;
	ArgInfo *ainfo = NULL;
	int stackSize;    

	sig = call->signature;
	n = sig->param_count + sig->hasthis;
	DEBUG (g_print ("Call requires: %d parameters\n",n));
	
	cinfo = get_call_info (cfg->mempool, sig);

	stackSize         = cinfo->sz.stack_size + cinfo->sz.parm_size;
	call->stack_usage = MAX(stackSize, call->stack_usage);
	lParamArea        = MAX((call->stack_usage-S390_MINIMAL_STACK_SIZE-cinfo->sz.parm_size), 0);
	cfg->param_area   = MAX(((signed) cfg->param_area), lParamArea); /* FIXME */
	cfg->flags       |= MONO_CFG_HAS_CALLS;

	if (cinfo->struct_ret) {
		MONO_INST_NEW (cfg, ins, OP_MOVE);
		ins->sreg1 = call->vret_var->dreg;
		ins->dreg = mono_alloc_preg (cfg);
		MONO_ADD_INS (cfg->cbb, ins);
		mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, cinfo->ret.reg, FALSE);
	}

	for (i = 0; i < n; ++i) {
		MonoType *t;

		ainfo = cinfo->args + i;
		if (i >= sig->hasthis)
			t = sig->params [i - sig->hasthis];
		else
			t = mono_get_int_type ();
		t = mini_get_underlying_type (t);

		in = call->args [i];

		if ((sig->call_convention == MONO_CALL_VARARG) &&
		    (!sig->pinvoke) &&
		    (i == sig->sentinelpos)) {
			emit_sig_cookie (cfg, call, cinfo);
		}

		switch (ainfo->regtype) {
		case RegTypeGeneral :
			add_outarg_reg2 (cfg, call, ainfo->regtype, ainfo->reg, in);
			break;
		case RegTypeFP :
		case RegTypeFPR4 :
			if (MONO_TYPE_ISSTRUCT (t)) {
				/* Valuetype passed in one fp register */
				ainfo->regtype = RegTypeStructByValInFP;
				/* Fall through */
			} else {
				add_outarg_reg2 (cfg, call, ainfo->regtype, ainfo->reg, in);
				break;
			}
		case RegTypeStructByVal :
		case RegTypeStructByAddr : {

			g_assert (in->klass);

			MONO_INST_NEW (cfg, ins, OP_OUTARG_VT);
			ins->sreg1 = in->dreg;
			ins->klass = in->klass;
			ins->backend.size = ainfo->size;
			ins->inst_p0 = call;
			ins->inst_p1 = mono_mempool_alloc (cfg->mempool, sizeof (ArgInfo));
			memcpy (ins->inst_p1, ainfo, sizeof (ArgInfo));

			MONO_ADD_INS (cfg->cbb, ins);

			break;
		}
		case RegTypeBase :
			if (!t->byref && t->type == MONO_TYPE_R4) {
				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORER4_MEMBASE_REG, 
							     STK_BASE, ainfo->offset + 4,
						  	     in->dreg);
			} else if (!t->byref && (t->type == MONO_TYPE_R8)) {
				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORER8_MEMBASE_REG, 
						  	     STK_BASE, ainfo->offset,
							     in->dreg);
			} else {
				MONO_INST_NEW (cfg, ins, OP_STORE_MEMBASE_REG);
				ins->inst_destbasereg = STK_BASE;
				ins->inst_offset = ainfo->offset;
				ins->sreg1 = in->dreg;
				MONO_ADD_INS (cfg->cbb, ins);
			}
			break;
		default:
			g_assert_not_reached ();
			break;
		}
	}

	/*
	 * Handle the case where there are no implicit arguments 
	 */
	if ((sig->call_convention == MONO_CALL_VARARG) &&
	    (!sig->pinvoke) &&
	    (i == sig->sentinelpos)) {
		emit_sig_cookie (cfg, call, cinfo);
	}

}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecture-specific Value Type parameter processing
 * 
 * @param[in] @cfg - Compile control block
 * @param[in] @call - Call Instruction
 * @param[in] @src - Source parameter
 * 
 * Process value type parameters for a call operation
 */

void
mono_arch_emit_outarg_vt (MonoCompile *cfg, MonoInst *ins, MonoInst *src)
{
	MonoCallInst *call = (MonoCallInst*) ins->inst_p0;
	ArgInfo *ainfo = (ArgInfo *) ins->inst_p1;

	if (ainfo->regtype == RegTypeStructByVal) {
		if (ainfo->reg != STK_BASE) {
			emit_outarg_vtr (cfg, ins, src);
		} else {
			emit_outarg_vts (cfg, ins, src);
		}	
	} else if (ainfo->regtype == RegTypeStructByValInFP) {
		int dreg = mono_alloc_freg (cfg);

		if (ainfo->size == 4) {
			MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADR4_MEMBASE, dreg, src->dreg, 0);
			MONO_EMIT_NEW_UNALU (cfg, OP_S390_SETF4RET, dreg, dreg);
		} else {
			g_assert (ainfo->size == 8);

			MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADR8_MEMBASE, dreg, src->dreg, 0);
		}

		mono_call_inst_add_outarg_reg (cfg, call, dreg, ainfo->reg, TRUE);
	} else {
		ERROR_DECL (error);
		MonoMethodHeader *header;
		MonoInst *vtcopy = mono_compile_create_var (cfg, m_class_get_byval_arg (src->klass), OP_LOCAL);
		MonoInst *load;
		int ovf_size = ainfo->vtsize,
		    srcReg;
		guint32 size;

		/* FIXME: alignment? */
		if (call->signature->pinvoke) {
			size = mono_type_native_stack_size (m_class_get_byval_arg (src->klass), NULL);
			vtcopy->backend.is_pinvoke = 1;
		} else {
			size = mini_type_stack_size (m_class_get_byval_arg (src->klass), NULL);
		}
		if (size > 0)
			g_assert (ovf_size > 0);

		header = mono_method_get_header_checked (cfg->method, error);
		mono_error_assert_ok (error); /* FIXME don't swallow the error */
		if ((cfg->flags & MONO_CFG_HAS_ALLOCA) || header->num_clauses)
			srcReg = s390_r11;
		else
			srcReg = STK_BASE;

		EMIT_NEW_VARLOADA (cfg, load, vtcopy, vtcopy->inst_vtype);
		mini_emit_memcpy (cfg, load->dreg, 0, src->dreg, 0, size, TARGET_SIZEOF_VOID_P);

		if (ainfo->reg == STK_BASE) {
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, srcReg, ainfo->offset, load->dreg);

			if (cfg->compute_gc_maps) {
				MonoInst *def;

				EMIT_NEW_GC_PARAM_SLOT_LIVENESS_DEF (cfg, def, ainfo->offset, m_class_get_byval_arg (ins->klass));
			}
		} else
			mono_call_inst_add_outarg_reg (cfg, call, load->dreg, ainfo->reg, FALSE);
	}
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecture-specific call value return processing
 * 
 * @param[in] @cfg - Compile control block
 * @param[in] @method - Method
 * @param[in] @val - Instruction representing the result returned to method
 * 
 * Create the sequence to unload the value returned from a call
 */

void
mono_arch_emit_setret (MonoCompile *cfg, MonoMethod *method, MonoInst *val)
{
	MonoType *ret = mini_get_underlying_type (mono_method_signature_internal (method)->ret);

	if (!ret->byref) {
		if (ret->type == MONO_TYPE_R4) {
			MONO_EMIT_NEW_UNALU (cfg, OP_S390_SETF4RET, s390_f0, val->dreg);
			return;
		} else if (ret->type == MONO_TYPE_R8) {
			MONO_EMIT_NEW_UNALU (cfg, OP_FMOVE, s390_f0, val->dreg);
			return;
		}
	}
			
	MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, cfg->ret->dreg, val->dreg);
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Replace compound compare/branch operations with single operation
 * 
 * @param[in] @bb - Basic block
 * @param[in] @ins - Current instruction
 * @param[in] @cc - Condition code of branch
 * @param[in] @logical - Whether comparison is signed or logical
 * 
 * Form a peephole pass at the code looking for simple optimizations
 * that will combine compare/branch instructions into a single operation.
 */

static void
compare_and_branch(MonoBasicBlock *bb, MonoInst *ins, int cc, gboolean logical)
{
	MonoInst *last;

	if (mono_hwcap_s390x_has_gie) {
		last = mono_inst_prev (ins, FILTER_IL_SEQ_POINT);
		ins->sreg1 = last->sreg1;
		ins->sreg2 = last->sreg2;
		ins->sreg3 = cc;
		switch(last->opcode) {
		case OP_ICOMPARE:
			if (logical)
				ins->opcode = OP_S390_CLRJ;
			else
				ins->opcode = OP_S390_CRJ;
			MONO_DELETE_INS(bb, last);
			break;
		case OP_COMPARE:
		case OP_LCOMPARE:
			if (logical)
				ins->opcode = OP_S390_CLGRJ;
			else
				ins->opcode = OP_S390_CGRJ;
			MONO_DELETE_INS(bb, last);
			break;
		case OP_ICOMPARE_IMM:
			ins->backend.data = (gpointer) last->inst_imm;
			if (logical)
				ins->opcode = OP_S390_CLIJ;
			else
				ins->opcode = OP_S390_CIJ;
			MONO_DELETE_INS(bb, last);
			break;
		case OP_COMPARE_IMM:
		case OP_LCOMPARE_IMM:
			ins->backend.data = (gpointer) last->inst_imm;
			if (logical)
				ins->opcode = OP_S390_CLGIJ;
			else
				ins->opcode = OP_S390_CGIJ;
			MONO_DELETE_INS(bb, last);
			break;
		}
	}
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecure-specific peephole pass 1 processing
 * 
 * @param[in] @cfg - Compile control block
 * @param[in] @bb - Basic block
 * 
 * Form a peephole pass at the code looking for compare and branch
 * optimizations.
 */

void
mono_arch_peephole_pass_1 (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins, *n;

	MONO_BB_FOR_EACH_INS_SAFE (bb, n, ins) {
		switch (ins->opcode) {
		case OP_IBEQ:
		case OP_LBEQ:
			compare_and_branch(bb, ins, S390_CC_EQ, FALSE);
			break;
		case OP_LBNE_UN:
		case OP_IBNE_UN:
			compare_and_branch(bb, ins, S390_CC_NE, TRUE);
			break;
		case OP_LBLT:
		case OP_IBLT:
			compare_and_branch(bb, ins, S390_CC_LT, FALSE);
			break;
		case OP_LBLT_UN:
		case OP_IBLT_UN:
			compare_and_branch(bb, ins, S390_CC_LT, TRUE);
			break;
		case OP_LBGT:
		case OP_IBGT:
			compare_and_branch(bb, ins, S390_CC_GT, FALSE);
			break;
		case OP_LBGT_UN:
		case OP_IBGT_UN:
			compare_and_branch(bb, ins, S390_CC_GT, TRUE);
			break;
		case OP_LBGE:
		case OP_IBGE:
			compare_and_branch(bb, ins, S390_CC_GE, FALSE);
			break;
		case OP_LBGE_UN:
		case OP_IBGE_UN:
			compare_and_branch(bb, ins, S390_CC_GE, TRUE);
			break;
		case OP_LBLE:
		case OP_IBLE:
			compare_and_branch(bb, ins, S390_CC_LE, FALSE);
			break;
		case OP_LBLE_UN:
		case OP_IBLE_UN:
			compare_and_branch(bb, ins, S390_CC_LE, TRUE);
			break;

		// default:
		//	mono_peephole_ins (bb, ins);
		}
	}
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecure-specific peephole pass 2 processing
 * 
 * @param[in] @cfg - Compile control block
 * @param[in] @bb - Basic block
 * 
 * Form a peephole pass at the code looking for simple optimizations.
 */

void
mono_arch_peephole_pass_2 (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins, *n, *last_ins = NULL;

	MONO_BB_FOR_EACH_INS_SAFE (bb, n, ins) {
		switch (ins->opcode) {
		case OP_LOADU4_MEMBASE:
		case OP_LOADI4_MEMBASE:
			if (last_ins && (last_ins->opcode == OP_STOREI4_MEMBASE_REG) &&
					ins->inst_basereg == last_ins->inst_destbasereg &&
					ins->inst_offset == last_ins->inst_offset) {
				ins->opcode = (ins->opcode == OP_LOADI4_MEMBASE) ? OP_ICONV_TO_I4 : OP_ICONV_TO_U4;
				ins->sreg1 = last_ins->sreg1;
			}
			break;
		}
		mono_peephole_ins (bb, ins);
	}
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecure-specific lowering pass processing
 * 
 * @param[in] @cfg - Compile control block
 * @param[in] @bb - Basic block
 * 
 * Form a lowering pass at the code looking for simple optimizations.
 */

void
mono_arch_lowering_pass (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins, *next;

	MONO_BB_FOR_EACH_INS_SAFE (bb, next, ins) {
		switch (ins->opcode) {
		case OP_DIV_IMM:
		case OP_REM_IMM:
		case OP_IDIV_IMM:
		case OP_IREM_IMM:
		case OP_IDIV_UN_IMM:
		case OP_IREM_UN_IMM:
		case OP_LAND_IMM:
		case OP_LOR_IMM:
		case OP_LREM_IMM:
		case OP_LXOR_IMM:
		case OP_LOCALLOC_IMM:
			mono_decompose_op_imm (cfg, bb, ins);
			break;
		case OP_LADD_IMM:
			if (!s390_is_imm16 (ins->inst_imm))
				/* This is created by the memcpy code which ignores is_inst_imm */
				mono_decompose_op_imm (cfg, bb, ins);
			break;
		default:
			break;
		}
	}

	bb->max_vreg = cfg->next_vreg;
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Emit float-to-int sequence
 * 
 * @param[in] @cfg - Compile control block
 * @param[in] @code - Current instruction area
 * @param[in] @dreg - Destination general register
 * @param[in] @sreg - Source floating point register
 * @param[in] @size - Size of destination
 * @param[in] @is_signed - Destination is signed/unsigned
 * @returns Next instruction location
 * 
 * Emit instructions to convert a single precision floating point value to an integer
 */

static guchar *
emit_float_to_int (MonoCompile *cfg, guchar *code, int dreg, int sreg, int size, gboolean is_signed)
{
	/* sreg is a float, dreg is an integer reg. */
	if (is_signed) {
		s390_cgebr (code, dreg, 5, sreg);
		switch (size) {
		case 1:
			s390_ltgr (code, dreg, dreg);
			s390_jnl  (code, 4);
			s390_oill (code, dreg, 0x80);
			s390_lghi (code, s390_r0, 0xff);
			s390_ngr  (code, dreg, s390_r0);
			break;
		case 2:
			s390_ltgr (code, dreg, dreg);
			s390_jnl  (code, 4);
			s390_oill (code, dreg, 0x8000);
			s390_llill(code, s390_r0, 0xffff);
			s390_ngr  (code, dreg, s390_r0);
			break;
		}
	} else {
		short *o[1];
		s390_lgdr   (code, s390_r14, s390_f14);
		s390_lgdr   (code, s390_r13, s390_f15);
		S390_SET    (code, s390_r0, 0x4f000000u);
		s390_ldgr   (code, s390_f14, s390_r0);
		s390_ler    (code, s390_f15, sreg);
		s390_cebr   (code, s390_f15, s390_f14);
		s390_jl     (code, 0); CODEPTR (code, o[0]);
		S390_SET    (code, s390_r0, 0x4f800000u);
		s390_ldgr   (code, s390_f14, s390_r0);
		s390_sebr   (code, s390_f15, s390_f14);
		s390_cfebr  (code, dreg, 7, s390_f15);
		s390_j      (code, 4);
		PTRSLOT (code, o[0]);
		s390_cfebr  (code, dreg, 5, sreg);
		switch (size) {
		case 1: 
			s390_lghi (code, s390_r0, 0xff);
			s390_ngr  (code, dreg, s390_r0);
			break;
		case 2:
			s390_llill(code, s390_r0, 0xffff);
			s390_ngr  (code, dreg, s390_r0);
			break;
		}
		s390_ldgr   (code, s390_f14, s390_r14);
		s390_ldgr   (code, s390_f15, s390_r13);
	}
	return code;
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Emit double-to-int sequence
 * 
 * @param[in] @cfg - Compile control block
 * @param[in] @code - Current instruction area
 * @param[in] @dreg - Destination general register
 * @param[in] @sreg - Source floating point register
 * @param[in] @size - Size of destination
 * @param[in] @is_signed - Destination is signed/unsigned
 * @returns Next instruction location
 * 
 * Emit instructions to convert a single precision floating point value to an integer
 */

static guchar*
emit_double_to_int (MonoCompile *cfg, guchar *code, int dreg, int sreg, int size, gboolean is_signed)
{
	/* sreg is a float, dreg is an integer reg. */
	if (is_signed) {
		s390_cgdbr (code, dreg, 5, sreg);
		switch (size) {
		case 1:
			s390_ltgr (code, dreg, dreg);
			s390_jnl  (code, 4);
			s390_oill (code, dreg, 0x80);
			s390_lghi (code, s390_r0, 0xff);
			s390_ngr  (code, dreg, s390_r0);
			break;
		case 2:
			s390_ltgr (code, dreg, dreg);
			s390_jnl  (code, 4);
			s390_oill (code, dreg, 0x8000);
			s390_llill(code, s390_r0, 0xffff);
			s390_ngr  (code, dreg, s390_r0);
			break;
		}
	} else {
		short *o[1];
		s390_lgdr   (code, s390_r14, s390_f14);
		s390_lgdr   (code, s390_r13, s390_f15);
		S390_SET    (code, s390_r0, 0x41e0000000000000llu);
		s390_ldgr   (code, s390_f14, s390_r0);
		s390_ldr    (code, s390_f15, sreg);
		s390_cdbr   (code, s390_f15, s390_f14);
		s390_jl     (code, 0); CODEPTR (code, o[0]);
		S390_SET    (code, s390_r0, 0x41f0000000000000llu);
		s390_ldgr   (code, s390_f14, s390_r0);
		s390_sdbr   (code, s390_f15, s390_f14);
		s390_cfdbr  (code, dreg, 7, s390_f15);
		s390_j      (code, 4);
		PTRSLOT (code, o[0]);
		s390_cfdbr  (code, dreg, 5, sreg);
		switch (size) {
		case 1: 
			s390_lghi (code, s390_r0, 0xff);
			s390_ngr  (code, dreg, s390_r0);
			break;
		case 2:
			s390_llill(code, s390_r0, 0xffff);
			s390_ngr  (code, dreg, s390_r0);
			break;
		}
		s390_ldgr   (code, s390_f14, s390_r14);
		s390_ldgr   (code, s390_f15, s390_r13);
	}
	return code;
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Check if branch is for unsigned comparison
 * 
 * @param[in] @next - Next instruction
 * @returns True if the branch is for an unsigned comparison
 * 
 * Determine if next instruction is a branch for an unsigned comparison
 */

static gboolean 
is_unsigned (MonoInst *next)
{
	if ((next) && 
		(((next->opcode >= OP_IBNE_UN) &&
		  (next->opcode <= OP_IBLT_UN)) || 
		 ((next->opcode >= OP_LBNE_UN) &&
		  (next->opcode <= OP_LBLT_UN)) ||
		 ((next->opcode >= OP_COND_EXC_NE_UN) &&
		  (next->opcode <= OP_COND_EXC_LT_UN)) ||
		 ((next->opcode >= OP_COND_EXC_INE_UN) &&
		  (next->opcode <= OP_COND_EXC_ILT_UN)) ||
		 ((next->opcode == OP_CLT_UN) ||
		  (next->opcode == OP_CGT_UN) ||
		  (next->opcode == OP_ICGE_UN)  ||
		  (next->opcode == OP_ICLE_UN)) ||
		 ((next->opcode == OP_ICLT_UN) ||
		  (next->opcode == OP_ICGT_UN) ||
		  (next->opcode == OP_LCLT_UN) ||
		  (next->opcode == OP_LCGT_UN))))
		return TRUE;
	else
		return FALSE;
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecutre-specific processing of a basic block
 * 
 * @param[in] @cfg - Compile control block
 * @param[in] @bb - Basic block
 * 
 * Process instructions within basic block emitting s390x instructions
 * based on the VM operation codes
 */

void
mono_arch_output_basic_block (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins;
	MonoCallInst *call;
	guint8 *code = cfg->native_code + cfg->code_len;
	int src2;

	/* we don't align basic blocks of loops on s390 */

	if (cfg->verbose_level > 2)
		g_print ("Basic block %d starting at offset 0x%x\n", bb->block_num, bb->native_offset);

	MONO_BB_FOR_EACH_INS (bb, ins) {
		const guint offset = code - cfg->native_code;
		set_code_cursor (cfg, code);
		int max_len = ins_get_size (ins->opcode);
		code = realloc_code (cfg, max_len);

		mono_debug_record_line_number (cfg, ins, offset);

		switch (ins->opcode) {
		case OP_STOREI1_MEMBASE_IMM: {
			s390_lghi (code, s390_r0, ins->inst_imm);
			S390_LONG (code, stcy, stc, s390_r0, 0, 
				   ins->inst_destbasereg, ins->inst_offset);
		}
			break;
		case OP_STOREI2_MEMBASE_IMM: {
			s390_lghi (code, s390_r0, ins->inst_imm);
			S390_LONG (code, sthy, sth, s390_r0, 0, 
				   ins->inst_destbasereg, ins->inst_offset);
		}
			break;
		case OP_STOREI4_MEMBASE_IMM: {
			s390_lgfi (code, s390_r0, ins->inst_imm);
			S390_LONG (code, sty, st, s390_r0, 0, 
				   ins->inst_destbasereg, ins->inst_offset);
		}
			break;
		case OP_STORE_MEMBASE_IMM:
		case OP_STOREI8_MEMBASE_IMM: {
			S390_SET (code, s390_r0, ins->inst_imm);
			S390_LONG (code, stg, stg, s390_r0, 0, 
				   ins->inst_destbasereg, ins->inst_offset);
		}
			break;
		case OP_STOREI1_MEMBASE_REG: {
			S390_LONG (code, stcy, stc, ins->sreg1, 0, 
				   ins->inst_destbasereg, ins->inst_offset);
		}
			break;
		case OP_STOREI2_MEMBASE_REG: {
			S390_LONG (code, sthy, sth, ins->sreg1, 0, 
				   ins->inst_destbasereg, ins->inst_offset);
		}
			break;
		case OP_STOREI4_MEMBASE_REG: {
			S390_LONG (code, sty, st, ins->sreg1, 0, 
				   ins->inst_destbasereg, ins->inst_offset);
		}
			break;
		case OP_STORE_MEMBASE_REG:
		case OP_STOREI8_MEMBASE_REG: {
			S390_LONG (code, stg, stg, ins->sreg1, 0, 
				   ins->inst_destbasereg, ins->inst_offset);
		}
			break;
		case OP_LOADU4_MEM:
			g_assert_not_reached ();
			break;
		case OP_LOAD_MEMBASE:
		case OP_LOADI8_MEMBASE: {
			S390_LONG (code, lg, lg, ins->dreg, 0, 
				   ins->inst_basereg, ins->inst_offset);
		}
			break;
		case OP_LOADI4_MEMBASE: {
			S390_LONG (code, lgf, lgf, ins->dreg, 0, 
				   ins->inst_basereg, ins->inst_offset);
		}
			break;
		case OP_LOADU4_MEMBASE: {
			S390_LONG (code, llgf, llgf, ins->dreg, 0, 
				   ins->inst_basereg, ins->inst_offset);
		}
			break;
		case OP_LOADU1_MEMBASE: {
			S390_LONG (code, llgc, llgc, ins->dreg, 0, 
				   ins->inst_basereg, ins->inst_offset);
		}
			break;
		case OP_LOADI1_MEMBASE: {
			S390_LONG (code, lgb, lgb, ins->dreg, 0, 
				   ins->inst_basereg, ins->inst_offset);
		}
			break;
		case OP_LOADU2_MEMBASE: {
			S390_LONG (code, llgh, llgh, ins->dreg, 0, 
				   ins->inst_basereg, ins->inst_offset);
		}
			break;
		case OP_LOADI2_MEMBASE: {
			S390_LONG (code, lgh, lgh, ins->dreg, 0, 
				   ins->inst_basereg, ins->inst_offset);
		}
			break;
		case OP_LCONV_TO_I1: {
			s390_lgbr (code, ins->dreg, ins->sreg1);
		}
			break;
		case OP_LCONV_TO_I2: {
			s390_lghr (code, ins->dreg, ins->sreg1);
		}
			break;
		case OP_LCONV_TO_U1: {
			s390_llgcr (code, ins->dreg, ins->sreg1);
		}
			break;
		case OP_LCONV_TO_U2: {
			s390_llghr (code, ins->dreg, ins->sreg1);
		}
			break;
		case OP_ICONV_TO_I1: {
			s390_lgbr  (code, ins->dreg, ins->sreg1);
		}
			break;
		case OP_ICONV_TO_I2: {
			s390_lghr  (code, ins->dreg, ins->sreg1);
		}
			break;
		case OP_ICONV_TO_U1: {
			s390_llgcr (code, ins->dreg, ins->sreg1);
		}
			break;
		case OP_ICONV_TO_U2: {
			s390_llghr (code, ins->dreg, ins->sreg1);
		}
			break;
		case OP_ICONV_TO_U4: {
			s390_llgfr (code, ins->dreg, ins->sreg1);
		}
			break;
		case OP_ICONV_TO_I4: {
			s390_lgfr (code, ins->dreg, ins->sreg1);
		}
			break;
		case OP_COMPARE: 
		case OP_LCOMPARE: {
			if (is_unsigned (ins->next))
				s390_clgr (code, ins->sreg1, ins->sreg2);
			else
				s390_cgr  (code, ins->sreg1, ins->sreg2);
		}
			break;
		case OP_ICOMPARE: {
			if (is_unsigned (ins->next))
				s390_clr  (code, ins->sreg1, ins->sreg2);
			else
				s390_cr   (code, ins->sreg1, ins->sreg2);
		}
			break;
		case OP_COMPARE_IMM:
		case OP_LCOMPARE_IMM: {
			gboolean branchUn = is_unsigned (ins->next);
			if ((ins->inst_imm == 0) && (!branchUn)) {
				s390_ltgr (code, ins->sreg1, ins->sreg1);
			} else {
				S390_SET (code, s390_r0, ins->inst_imm);
				if (branchUn)
					s390_clgr (code, ins->sreg1, s390_r0);
				else
					s390_cgr  (code, ins->sreg1, s390_r0);
			}
		}
			break;
		case OP_ICOMPARE_IMM: {
			gboolean branchUn = is_unsigned (ins->next);
			if ((ins->inst_imm == 0) && (!branchUn)) {
				s390_ltr (code, ins->sreg1, ins->sreg1);
			} else {
				S390_SET (code, s390_r0, ins->inst_imm);
				if (branchUn)
					s390_clr  (code, ins->sreg1, s390_r0);
				else
					s390_cr   (code, ins->sreg1, s390_r0);
			}
		}
			break;
		case OP_BREAK: {
			mono_add_patch_info (cfg, code - cfg->native_code,
					     MONO_PATCH_INFO_JIT_ICALL_ID,
					     GUINT_TO_POINTER (MONO_JIT_ICALL_mono_break));
			S390_CALL_TEMPLATE (code, s390_r14);
		}
			break;
		case OP_ADDCC: {
			if (mono_hwcap_s390x_has_mlt) {
				s390_agrk (code, ins->dreg, ins->sreg1, ins->sreg2);
			} else {
				CHECK_SRCDST_COM;
				s390_agr  (code, ins->dreg, src2);
			}
		}
			break;
		case OP_LADD: {
			if (mono_hwcap_s390x_has_mlt) {
				s390_agrk (code, ins->dreg, ins->sreg1, ins->sreg2);
			} else {
				CHECK_SRCDST_COM;
				s390_agr   (code, ins->dreg, src2);
			}
		}
			break;
		case OP_ADC: {
			CHECK_SRCDST_COM;
			s390_alcgr (code, ins->dreg, src2);
		}
			break;
		case OP_ADD_IMM: {
			if (mono_hwcap_s390x_has_mlt) {
				if (s390_is_imm16 (ins->inst_imm)) {
					s390_aghik(code, ins->dreg, ins->sreg1, ins->inst_imm);
				} else {
					S390_SET  (code, s390_r0, ins->inst_imm);
					s390_agrk (code, ins->dreg, ins->sreg1, s390_r0);
				}
			} else {
				if (ins->dreg != ins->sreg1) {
					s390_lgr  (code, ins->dreg, ins->sreg1);
				}
				if (s390_is_imm16 (ins->inst_imm)) {
					s390_aghi (code, ins->dreg, ins->inst_imm);
				} else if (s390_is_imm32 (ins->inst_imm)) {
					s390_agfi (code, ins->dreg, ins->inst_imm);
				} else {
					S390_SET  (code, s390_r0, ins->inst_imm);
					s390_agr  (code, ins->dreg, s390_r0);
				}
			}
		}
			break;
		case OP_LADD_IMM: {
			if (mono_hwcap_s390x_has_mlt) {
				if (s390_is_imm16 (ins->inst_imm)) {
					s390_aghik(code, ins->dreg, ins->sreg1, ins->inst_imm);
				} else {
					S390_SET  (code, s390_r0, ins->inst_imm);
					s390_agrk (code, ins->dreg, ins->sreg1, s390_r0);
				}
			} else { 	
				if (ins->dreg != ins->sreg1) {
					s390_lgr  (code, ins->dreg, ins->sreg1);
				}
				if (s390_is_imm32 (ins->inst_imm)) {
					s390_agfi (code, ins->dreg, ins->inst_imm);
				} else {
					S390_SET  (code, s390_r0, ins->inst_imm);
					s390_agr  (code, ins->dreg, s390_r0);
				}
			}
		}
			break;
		case OP_ADC_IMM: {
			if (ins->dreg != ins->sreg1) {
				s390_lgr  (code, ins->dreg, ins->sreg1);
			}
			if (s390_is_imm16 (ins->inst_imm)) {
				s390_lghi  (code, s390_r0, ins->inst_imm);
				s390_alcgr (code, ins->dreg, s390_r0);
			} else {
				S390_SET   (code, s390_r0, ins->inst_imm);
				s390_alcgr (code, ins->dreg, s390_r0);
			}
		}
			break;
		case OP_IADD_OVF:
		case OP_S390_IADD_OVF: {
			CHECK_SRCDST_COM;
			s390_ar    (code, ins->dreg, src2);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_OV, "OverflowException");
			s390_lgfr  (code, ins->dreg, ins->dreg);
		}
			break;
		case OP_IADD_OVF_UN:
		case OP_S390_IADD_OVF_UN: {
			CHECK_SRCDST_COM;
			s390_alr   (code, ins->dreg, src2);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_CY, "OverflowException");
			s390_llgfr (code, ins->dreg, ins->dreg);
		}
			break;
		case OP_ADD_OVF_CARRY: {
			CHECK_SRCDST_COM;
			s390_lghi  (code, s390_r0, 0);
			s390_lgr   (code, s390_r1, s390_r0);
			s390_alcgr (code, s390_r0, s390_r1);
			s390_agr   (code, ins->dreg, src2);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_OV, "OverflowException");
			s390_agr   (code, ins->dreg, s390_r0);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_OV, "OverflowException");
		}
			break;
		case OP_ADD_OVF_UN_CARRY: {
			CHECK_SRCDST_COM;
			s390_alcgr (code, ins->dreg, src2);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_CY, "OverflowException");
		}
			break;
		case OP_SUBCC: {
			if (mono_hwcap_s390x_has_mlt) {
			    s390_sgrk (code, ins->dreg, ins->sreg1, ins->sreg2);
			} else {
			    CHECK_SRCDST_NCOM;
			    s390_sgr (code, ins->dreg, src2);
			}
		}
			break;
		case OP_LSUB: {
			if (mono_hwcap_s390x_has_mlt) {
			    s390_sgrk (code, ins->dreg, ins->sreg1, ins->sreg2);
			} else {
			    CHECK_SRCDST_NCOM;
			    s390_sgr  (code, ins->dreg, src2);
			}
		}
			break;
		case OP_SBB: {
			CHECK_SRCDST_NCOM;
			s390_slbgr(code, ins->dreg, src2);
		}
			break;
		case OP_SUB_IMM: {
			if (ins->dreg != ins->sreg1) {
				s390_lgr   (code, ins->dreg, ins->sreg1);
			}
			if (s390_is_imm16 (-ins->inst_imm)) {
				s390_aghi  (code, ins->dreg, -ins->inst_imm);
			} else if (s390_is_imm32 (-ins->inst_imm)) {
				s390_slgfi  (code, ins->dreg, ins->inst_imm);
			} else {
				S390_SET  (code, s390_r0, ins->inst_imm);
				s390_slgr (code, ins->dreg, s390_r0);
			}
		}
			break;
		case OP_LSUB_IMM: {
			if (ins->dreg != ins->sreg1) {
				s390_lgr   (code, ins->dreg, ins->sreg1);
			}
			if (s390_is_imm16 (-ins->inst_imm)) {
				s390_aghi  (code, ins->dreg, -ins->inst_imm);
			} else if (s390_is_imm32 (-ins->inst_imm)) {
				s390_slgfi (code, ins->dreg, ins->inst_imm);
			} else {
				S390_SET  (code, s390_r0, ins->inst_imm);
				s390_slgr (code, ins->dreg, s390_r0);
			}
		}
			break;
		case OP_SBB_IMM: {
			if (ins->dreg != ins->sreg1) {
				s390_lgr   (code, ins->dreg, ins->sreg1);
			}
			if (s390_is_imm16 (-ins->inst_imm)) {
				s390_lghi  (code, s390_r0, ins->inst_imm);
				s390_slbgr (code, ins->dreg, s390_r0);
			} else {
				S390_SET  (code, s390_r0, ins->inst_imm);
				s390_slbgr(code, ins->dreg, s390_r0);
			}
		}
			break;
		case OP_SUB_OVF_CARRY: {
			CHECK_SRCDST_NCOM;
			s390_lghi  (code, s390_r0, 0);
			s390_lgr   (code, s390_r1, s390_r0);
			s390_slbgr (code, s390_r0, s390_r1);
			s390_sgr   (code, ins->dreg, src2);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_OV, "OverflowException");
			s390_agr   (code, ins->dreg, s390_r0);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_OV, "OverflowException");
		}
			break;
		case OP_SUB_OVF_UN_CARRY: {
			CHECK_SRCDST_NCOM;
			s390_slbgr (code, ins->dreg, src2);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_NC, "OverflowException");
		}
			break;
		case OP_LAND: {
			if (mono_hwcap_s390x_has_mlt) {
				s390_ngrk (code, ins->dreg, ins->sreg1, ins->sreg2);
			} else {
				if (ins->sreg1 == ins->dreg) {
					s390_ngr  (code, ins->dreg, ins->sreg2);
				} else { 
					if (ins->sreg2 == ins->dreg) { 
						s390_ngr (code, ins->dreg, ins->sreg1);
					} else { 
						s390_lgr (code, ins->dreg, ins->sreg1);
						s390_ngr (code, ins->dreg, ins->sreg2);
					}
				}
			}
		}
			break;
		case OP_AND_IMM: {
			S390_SET_MASK (code, s390_r0, ins->inst_imm);
			if (mono_hwcap_s390x_has_mlt) {
				s390_ngrk (code, ins->dreg, ins->sreg1, s390_r0);
			} else {
				if (ins->dreg != ins->sreg1) {
					s390_lgr  (code, ins->dreg, ins->sreg1);
				}
				s390_ngr (code, ins->dreg, s390_r0);
			}
		}
			break;
		case OP_LDIV: {
			s390_lgr  (code, s390_r1, ins->sreg1);
			s390_dsgr (code, s390_r0, ins->sreg2);
			s390_lgr  (code, ins->dreg, s390_r1);
		}
			break;
		case OP_LDIV_UN: {
			s390_lgr   (code, s390_r1, ins->sreg1);
			s390_lghi  (code, s390_r0, 0);
			s390_dlgr  (code, s390_r0, ins->sreg2);
			s390_lgr   (code, ins->dreg, s390_r1);
		}
			break;
		case OP_LREM: {
			s390_lgr  (code, s390_r1, ins->sreg1);
			s390_dsgr (code, s390_r0, ins->sreg2);
			s390_lgr  (code, ins->dreg, s390_r0);
			break;
		}
		case OP_LREM_IMM: {
			if (s390_is_imm16 (ins->inst_imm)) {
				s390_lghi (code, s390_r13, ins->inst_imm);
			} else {
				s390_lgfi (code, s390_r13, ins->inst_imm);
			}
			s390_lgr  (code, s390_r0, ins->sreg1);
			s390_dsgr (code, s390_r0, s390_r13);
			s390_lgfr (code, ins->dreg, s390_r0);
		}
			break;
		case OP_LREM_UN: {
			s390_lgr   (code, s390_r1, ins->sreg1);
			s390_lghi  (code, s390_r0, 0);
			s390_dlgr  (code, s390_r0, ins->sreg2);
			s390_lgr   (code, ins->dreg, s390_r0);
		}
			break;
		case OP_LOR: {
			if (mono_hwcap_s390x_has_mlt) {
				s390_ogrk (code, ins->dreg, ins->sreg1, ins->sreg2);
			} else {
				if (ins->sreg1 == ins->dreg) {
					s390_ogr  (code, ins->dreg, ins->sreg2);
				} else { 
					if (ins->sreg2 == ins->dreg) { 
						s390_ogr (code, ins->dreg, ins->sreg1);
					} else { 
						s390_lgr (code, ins->dreg, ins->sreg1);
						s390_ogr (code, ins->dreg, ins->sreg2);
					}
				}
			}
		}
			break;
		case OP_OR_IMM: {
			S390_SET_MASK(code, s390_r0, ins->inst_imm);
			if (mono_hwcap_s390x_has_mlt) {
				s390_ogrk (code, ins->dreg, ins->sreg1, s390_r0);
			} else {
				if (ins->dreg != ins->sreg1) {
					s390_lgr  (code, ins->dreg, ins->sreg1);
				}
				s390_ogr (code, ins->dreg, s390_r0);
			}
		}
			break;
		case OP_LXOR: {
			if (mono_hwcap_s390x_has_mlt) {
				s390_xgrk (code, ins->dreg, ins->sreg1, ins->sreg2);
			} else {
				if (ins->sreg1 == ins->dreg) {
					s390_xgr  (code, ins->dreg, ins->sreg2);
				} 
				else { 
					if (ins->sreg2 == ins->dreg) { 
						s390_xgr (code, ins->dreg, ins->sreg1);
					}
					else { 
						s390_lgr (code, ins->dreg, ins->sreg1);
						s390_xgr (code, ins->dreg, ins->sreg2);
					}
				}
			}
		}
			break;
		case OP_XOR_IMM: {
			S390_SET_MASK(code, s390_r0, ins->inst_imm);
			if (mono_hwcap_s390x_has_mlt) {
				s390_xgrk (code, ins->dreg, ins->sreg1, s390_r0);
			} else {
				if (ins->dreg != ins->sreg1) {
					s390_lgr  (code, ins->dreg, ins->sreg1);
				}
				s390_xgr (code, ins->dreg, s390_r0);
			}
		}
			break;
		case OP_LSHL: {
			CHECK_SRCDST_NCOM;
			s390_sllg (code, ins->dreg, ins->dreg, src2, 0);
		}
			break;
		case OP_SHL_IMM: 
		case OP_LSHL_IMM: {
			if (ins->sreg1 != ins->dreg) {
				s390_lgr   (code, ins->dreg, ins->sreg1);
			}
			s390_sllg (code, ins->dreg, ins->dreg, 0, (ins->inst_imm & 0x3f));
		}
			break;
		case OP_LSHR: {
			CHECK_SRCDST_NCOM;
			s390_srag  (code, ins->dreg, ins->dreg, src2, 0);
		}
			break;
		case OP_SHR_IMM:
		case OP_LSHR_IMM: {
			if (ins->sreg1 != ins->dreg) {
				s390_lgr  (code, ins->dreg, ins->sreg1);
			}
			s390_srag  (code, ins->dreg, ins->dreg, 0, (ins->inst_imm & 0x3f));
		}
			break;
		case OP_SHR_UN_IMM: 
		case OP_LSHR_UN_IMM: {
			if (ins->sreg1 != ins->dreg) {
				s390_lgr   (code, ins->dreg, ins->sreg1);
			}
			s390_srlg (code, ins->dreg, ins->dreg, 0, (ins->inst_imm & 0x3f));
		}
			break;
		case OP_LSHR_UN: {
			CHECK_SRCDST_NCOM;
			s390_srlg (code, ins->dreg, ins->dreg, src2, 0);
		}
			break;
		case OP_LNOT: {
			if (ins->sreg1 != ins->dreg) {
				s390_lgr  (code, ins->dreg, ins->sreg1);
			}
			s390_lghi (code, s390_r0, -1);
			s390_xgr  (code, ins->dreg, s390_r0);
		}
			break;
		case OP_LNEG: {
			s390_lcgr (code, ins->dreg, ins->sreg1);
		}
			break;
		case OP_LMUL: {
			CHECK_SRCDST_COM;
			s390_msgr (code, ins->dreg, src2);
		}
			break;
		case OP_MUL_IMM: 
		case OP_LMUL_IMM: {
			if (ins->dreg != ins->sreg1) {
				s390_lgr  (code, ins->dreg, ins->sreg1);
			}
			if ((mono_hwcap_s390x_has_gie) &&
			    (s390_is_imm32 (ins->inst_imm))) {
				s390_msgfi (code, ins->dreg, ins->inst_imm);
			} else {
				if (s390_is_imm16 (ins->inst_imm)) {
					s390_lghi (code, s390_r13, ins->inst_imm);
				} else if (s390_is_imm32 (ins->inst_imm)) {
					s390_lgfi (code, s390_r13, ins->inst_imm);
				} else {
					S390_SET (code, s390_r13, ins->inst_imm);
				}
				s390_msgr (code, ins->dreg, s390_r13);
			}
		}
			break;
		case OP_LMUL_OVF: {
			short int *o[2];
			if (mono_hwcap_s390x_has_mie2) {
				s390_msgrkc (code, ins->dreg, ins->sreg1, ins->sreg2);
				EMIT_COND_SYSTEM_EXCEPTION (S390_CC_OV, "OverflowException");
			} else {
				s390_ltgr (code, s390_r1, ins->sreg1);
				s390_jz   (code, 0); CODEPTR(code, o[0]);
				s390_ltgr (code, s390_r0, ins->sreg2);
				s390_jnz  (code, 6);
				s390_lghi (code, s390_r1, 0);
				s390_j    (code, 0); CODEPTR(code, o[1]);
				s390_xgr  (code, s390_r0, s390_r1);
				s390_msgr (code, s390_r1, ins->sreg2);
				s390_xgr  (code, s390_r0, s390_r1);
				s390_srlg (code, s390_r0, s390_r0, 0, 63);
				s390_ltgr (code, s390_r0, s390_r0);
				EMIT_COND_SYSTEM_EXCEPTION (S390_CC_NZ, "OverflowException");
				PTRSLOT	  (code, o[0]); 
				PTRSLOT   (code, o[1]);
				s390_lgr  (code, ins->dreg, s390_r1);
			}
		}
			break;
		case OP_LMUL_OVF_UN: {
			s390_lghi  (code, s390_r0, 0);
			s390_lgr   (code, s390_r1, ins->sreg1);
			s390_mlgr  (code, s390_r0, ins->sreg2);
			s390_ltgr  (code, s390_r0, s390_r0);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_NZ, "OverflowException");
			s390_lgr   (code, ins->dreg, s390_r1);
		}
			break;
		case OP_IADDCC: {
			g_assert_not_reached ();
			CHECK_SRCDST_COM_I;
			s390_algr (code, ins->dreg, src2);
		}
			break;
		case OP_IADD: {
			CHECK_SRCDST_COM_I;
			s390_agr  (code, ins->dreg, src2);
		}
			break;
		case OP_IADC: {
			g_assert_not_reached ();
			CHECK_SRCDST_COM_I;
			s390_alcgr (code, ins->dreg, src2);
		}
			break;
		case OP_IADD_IMM: {
			if (ins->dreg != ins->sreg1) {
				s390_lgfr (code, ins->dreg, ins->sreg1);
			}
			if (s390_is_imm16 (ins->inst_imm)) {
				s390_aghi (code, ins->dreg, ins->inst_imm);
			} else {
				s390_afi  (code, ins->dreg, ins->inst_imm);
			}
		}
			break;
		case OP_IADC_IMM: {
			if (ins->dreg != ins->sreg1) {
				s390_lgfr (code, ins->dreg, ins->sreg1);
			} 
			if (s390_is_imm16 (ins->inst_imm)) {
				s390_lghi  (code, s390_r0, ins->inst_imm);
				s390_alcgr (code, ins->dreg, s390_r0);
			} else {
				S390_SET   (code, s390_r0, ins->inst_imm);
				s390_alcgr (code, ins->dreg, s390_r0);
			}
		}
			break;
		case OP_LADD_OVF:
		case OP_S390_LADD_OVF: {
			if (mono_hwcap_s390x_has_mlt) {
				s390_agrk (code, ins->dreg, ins->sreg1, ins->sreg2);
			} else { 
				CHECK_SRCDST_COM;
				s390_agr    (code, ins->dreg, src2);
			}
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_OV, "OverflowException");
		}
			break;
		case OP_LADD_OVF_UN:
		case OP_S390_LADD_OVF_UN: {
			if (mono_hwcap_s390x_has_mlt) {
				s390_algrk (code, ins->dreg, ins->sreg1, ins->sreg2);
			} else { 
				CHECK_SRCDST_COM;
				s390_algr  (code, ins->dreg, src2);
			}
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_CY, "OverflowException");
		}
			break;
		case OP_ISUBCC: {
			if (mono_hwcap_s390x_has_mlt) {
				s390_slgrk (code, ins->dreg, ins->sreg1, ins->sreg2);
			} else {
				CHECK_SRCDST_NCOM_I;
				s390_slgr (code, ins->dreg, src2);
			}
		}
			break;
		case OP_ISUB: {
			if (mono_hwcap_s390x_has_mlt) {
				s390_sgrk (code, ins->dreg, ins->sreg1, ins->sreg2);
			} else {
				CHECK_SRCDST_NCOM_I;
				s390_sgr  (code, ins->dreg, src2);
			}
		}
			break;
		case OP_ISBB: {
			CHECK_SRCDST_NCOM_I;
			s390_slbgr (code, ins->dreg, src2);
		}
			break;
		case OP_ISUB_IMM: {
			if (ins->dreg != ins->sreg1) {
				s390_lgfr (code, ins->dreg, ins->sreg1);
			}
			if (s390_is_imm16 (-ins->inst_imm)) {
				s390_aghi (code, ins->dreg, -ins->inst_imm);
			} else {
				s390_agfi (code, ins->dreg, -ins->inst_imm);
			}
		}
			break;
		case OP_ISBB_IMM: {
			S390_SET (code, s390_r0, ins->inst_imm);
			s390_slgfr (code, ins->dreg, s390_r0);
		}
			break;
		case OP_ISUB_OVF:
		case OP_S390_ISUB_OVF: {
			if (mono_hwcap_s390x_has_mlt) {
				s390_srk (code, ins->dreg, ins->sreg1, ins->sreg2);
				EMIT_COND_SYSTEM_EXCEPTION (S390_CC_OV, "OverflowException");
			} else { 
				CHECK_SRCDST_NCOM;
				s390_sr   (code, ins->dreg, src2);
				EMIT_COND_SYSTEM_EXCEPTION (S390_CC_OV, "OverflowException");
				s390_lgfr (code, ins->dreg, ins->dreg);
			}
		}
			break;
		case OP_ISUB_OVF_UN:
		case OP_S390_ISUB_OVF_UN: {
			if (mono_hwcap_s390x_has_mlt) {
				s390_slrk  (code, ins->dreg, ins->sreg1, ins->sreg2);
			} else {
				CHECK_SRCDST_NCOM;
				s390_slr  (code, ins->dreg, src2);
			}
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_NC, "OverflowException");
			s390_llgfr(code, ins->dreg, ins->dreg);
		}
			break;
		case OP_LSUB_OVF:
		case OP_S390_LSUB_OVF: {
			if (mono_hwcap_s390x_has_mlt) {
				s390_sgrk  (code, ins->dreg, ins->sreg1, ins->sreg2);
			} else {
				CHECK_SRCDST_NCOM;
				s390_sgr   (code, ins->dreg, src2);
			}
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_OV, "OverflowException");
		}
			break;
		case OP_LSUB_OVF_UN:
		case OP_S390_LSUB_OVF_UN: {
			CHECK_SRCDST_NCOM;
			s390_slgr  (code, ins->dreg, src2);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_NC, "OverflowException");
		}
			break;
		case OP_IAND: {
			if (mono_hwcap_s390x_has_mlt) {
				s390_ngrk (code, ins->dreg, ins->sreg1, ins->sreg2);
			} else {
				CHECK_SRCDST_NCOM_I;
				s390_ngr (code, ins->dreg, src2);
			}
		}
			break;
		case OP_IAND_IMM: {
			S390_SET_MASK (code, s390_r0, ins->inst_imm);
			if (mono_hwcap_s390x_has_mlt) {
				s390_ngrk (code, ins->dreg, ins->sreg1, s390_r0);
			} else {
				if (ins->dreg != ins->sreg1) {
					s390_lgfr (code, ins->dreg, ins->sreg1);
				}
				s390_ngr  (code, ins->dreg, s390_r0);
			}
		}
			break;
		case OP_IDIV: {
			s390_lgfr (code, s390_r0, ins->sreg1);
			s390_srda (code, s390_r0, 0, 32);
			s390_dr   (code, s390_r0, ins->sreg2);
			s390_lgfr (code, ins->dreg, s390_r1);
		}
			break;
		case OP_IDIV_UN: {
			s390_lgfr (code, s390_r0, ins->sreg1);
			s390_srdl (code, s390_r0, 0, 32);
			s390_dlr  (code, s390_r0, ins->sreg2);
			s390_lgfr (code, ins->dreg, s390_r1);
		}
			break;
		case OP_IDIV_IMM: {
			if (s390_is_imm16 (ins->inst_imm)) {
				s390_lghi (code, s390_r13, ins->inst_imm);
			} else {
				s390_lgfi (code, s390_r13, ins->inst_imm);
			}
			s390_lgfr (code, s390_r0, ins->sreg1);
			s390_srda (code, s390_r0, 0, 32);
			s390_dr   (code, s390_r0, ins->sreg2);
			s390_lgfr (code, ins->dreg, s390_r1);
		}
			break;
		case OP_IREM: {
			s390_lgfr (code, s390_r0, ins->sreg1);
			s390_srda (code, s390_r0, 0, 32);
			s390_dr   (code, s390_r0, ins->sreg2);
			s390_lgfr (code, ins->dreg, s390_r0);
			break;
		case OP_IREM_UN:
			s390_lgfr (code, s390_r0, ins->sreg1);
			s390_srdl (code, s390_r0, 0, 32);
			s390_dlr  (code, s390_r0, ins->sreg2);
			s390_lgfr (code, ins->dreg, s390_r0);
		}
			break;
		case OP_IREM_IMM: {
			if (s390_is_imm16 (ins->inst_imm)) {
				s390_lghi (code, s390_r13, ins->inst_imm);
			} else {
				s390_lgfi (code, s390_r13, ins->inst_imm);
			}
			s390_lgfr (code, s390_r0, ins->sreg1);
			s390_srda (code, s390_r0, 0, 32);
			s390_dr   (code, s390_r0, ins->sreg2);
			s390_lgfr (code, ins->dreg, s390_r0);
		}
			break;
		case OP_IOR: {
			if (mono_hwcap_s390x_has_mlt) {
				s390_ogrk (code, ins->dreg, ins->sreg1, ins->sreg2);
			} else {
				CHECK_SRCDST_COM_I;
				s390_ogr (code, ins->dreg, src2);
			}
		}
			break;
		case OP_IOR_IMM: {
			S390_SET_MASK (code, s390_r0, ins->inst_imm);
			if (mono_hwcap_s390x_has_mlt) {
				s390_ogrk (code, ins->dreg, ins->sreg1, s390_r0);
			} else {
				if (ins->dreg != ins->sreg1) {
					s390_lgfr (code, ins->dreg, ins->sreg1);
				}
				s390_ogr  (code, ins->dreg, s390_r0);
			}
		}
			break;
		case OP_IXOR: {
			if (mono_hwcap_s390x_has_mlt) {
				s390_xgrk (code, ins->dreg, ins->sreg1, ins->sreg2);
			} else {
				CHECK_SRCDST_COM_I;
				s390_xgr (code, ins->dreg, src2);
			}
		}
			break;
		case OP_IXOR_IMM: {
			S390_SET_MASK (code, s390_r0, ins->inst_imm);
			if (mono_hwcap_s390x_has_mlt) {
				s390_xgrk (code, ins->dreg, ins->sreg1, s390_r0);
			} else {
				if (ins->dreg != ins->sreg1) {
					s390_lgfr (code, ins->dreg, ins->sreg1);
				}
				s390_xgr  (code, ins->dreg, s390_r0);
			}
		}
			break;
		case OP_ISHL: {
			CHECK_SRCDST_NCOM;
			s390_sll  (code, ins->dreg, src2, 0);
		}
			break;
		case OP_ISHL_IMM: {
			if (ins->sreg1 != ins->dreg) {
				s390_lgfr (code, ins->dreg, ins->sreg1);
			}
			s390_sll (code, ins->dreg, 0, (ins->inst_imm & 0x1f));
		}
			break;
		case OP_ISHR: {
			CHECK_SRCDST_NCOM;
			s390_sra (code, ins->dreg, src2, 0);
		}
			break;
		case OP_ISHR_IMM: {
			if (ins->sreg1 != ins->dreg) {
				s390_lgfr (code, ins->dreg, ins->sreg1);
			}
			s390_sra (code, ins->dreg, 0, (ins->inst_imm & 0x1f));
		}
			break;
		case OP_ISHR_UN_IMM: {
			if (ins->sreg1 != ins->dreg) {
				s390_lgfr (code, ins->dreg, ins->sreg1);
			}
			s390_srl (code, ins->dreg, 0, (ins->inst_imm & 0x1f));
		}
			break;
		case OP_ISHR_UN: {
			CHECK_SRCDST_NCOM;
			s390_srl  (code, ins->dreg, src2, 0);
		}
			break;
		case OP_INOT: {
			if (ins->sreg1 != ins->dreg) {
				s390_lgfr (code, ins->dreg, ins->sreg1);
			}
			s390_lghi (code, s390_r0, -1);
			s390_xgr  (code, ins->dreg, s390_r0);
		}
			break;
		case OP_INEG: {
			s390_lcgr (code, ins->dreg, ins->sreg1);
		}
			break;
		case OP_IMUL: {
			CHECK_SRCDST_COM_I;
			s390_msr (code, ins->dreg, src2);
		}
			break;
		case OP_IMUL_IMM: {
			if (ins->dreg != ins->sreg1) {
				s390_lgfr (code, ins->dreg, ins->sreg1);
			}
			if (s390_is_imm16 (ins->inst_imm)) {
				s390_lghi (code, s390_r0, ins->inst_imm);
			} else {
				s390_lgfi (code, s390_r0, ins->inst_imm);
			}
			s390_msr  (code, ins->dreg, s390_r0);
		}
			break;
		case OP_IMUL_OVF: {
			short int *o[2];
			if (mono_hwcap_s390x_has_mie2) {
				s390_msrkc (code, ins->dreg, ins->sreg1, ins->sreg2);
				EMIT_COND_SYSTEM_EXCEPTION (S390_CC_OV, "OverflowException");
				s390_lgfr (code, ins->dreg, ins->dreg);
			} else {
				s390_ltr  (code, s390_r1, ins->sreg1);
				s390_jz   (code, 0); CODEPTR(code, o[0]);
				s390_ltr  (code, s390_r0, ins->sreg2);
				s390_jnz  (code, 6);
				s390_lhi  (code, s390_r1, 0);
				s390_j    (code, 0); CODEPTR(code, o[1]);
				s390_xr	  (code, s390_r0, s390_r1);
				s390_msr  (code, s390_r1, ins->sreg2);
				s390_xr   (code, s390_r0, s390_r1);
				s390_srl  (code, s390_r0, 0, 31);
				s390_ltr  (code, s390_r0, s390_r0);
				EMIT_COND_SYSTEM_EXCEPTION (S390_CC_NZ, "OverflowException");
				PTRSLOT	  (code, o[0]);
				PTRSLOT   (code, o[1]);
				s390_lgfr (code, ins->dreg, s390_r1);
			}
		}
			break;
		case OP_IMUL_OVF_UN: {
			s390_lhi  (code, s390_r0, 0);
			s390_lr   (code, s390_r1, ins->sreg1);
			s390_mlr  (code, s390_r0, ins->sreg2);
			s390_ltr  (code, s390_r0, s390_r0);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_NZ, "OverflowException");
			s390_lgfr (code, ins->dreg, s390_r1);
		}
			break;
		case OP_ICONST: 
		case OP_I8CONST: {
			S390_SET (code, ins->dreg, ins->inst_c0);
		}
			break;
		case OP_AOTCONST: {
			mono_add_patch_info (cfg, code - cfg->native_code, 
				(MonoJumpInfoType)ins->inst_i1, ins->inst_p0);
			S390_LOAD_TEMPLATE (code, ins->dreg);
		}
			break;
		case OP_JUMP_TABLE: {
			mono_add_patch_info (cfg, code - cfg->native_code, 
				(MonoJumpInfoType)ins->inst_i1, ins->inst_p0);
			S390_LOAD_TEMPLATE (code, ins->dreg);
		}
			break;
		case OP_MOVE:
			if (ins->dreg != ins->sreg1) {
				s390_lgr (code, ins->dreg, ins->sreg1);
			}
			break;
		case OP_LCONV_TO_I:
		case OP_LCONV_TO_I8:
		case OP_SEXT_I4:
			s390_lgfr (code, ins->dreg, ins->sreg1);
			break;
		case OP_LCONV_TO_I4:
			s390_lgfr (code, ins->dreg, ins->sreg1);
			break;
		case OP_LCONV_TO_U:
		case OP_LCONV_TO_U8:
		case OP_LCONV_TO_U4:
		case OP_ZEXT_I4:
			s390_llgfr (code, ins->dreg, ins->sreg1);
			break;
		case OP_LCONV_TO_OVF_U4:
			S390_SET  (code, s390_r0, 4294967295);
			s390_clgr (code, ins->sreg1, s390_r0);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_GT, "OverflowException");
			s390_ltgr (code, ins->sreg1, ins->sreg1);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_LT, "OverflowException");
			s390_llgfr(code, ins->dreg, ins->sreg1);
			break;
		case OP_LCONV_TO_OVF_I4_UN:
			S390_SET  (code, s390_r0, 2147483647);
			s390_cgr  (code, ins->sreg1, s390_r0);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_GT, "OverflowException");
			s390_ltgr (code, ins->sreg1, ins->sreg1);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_LT, "OverflowException");
			s390_lgfr (code, ins->dreg, ins->sreg1);
			break;
		case OP_FMOVE:
			if (ins->dreg != ins->sreg1) {
				s390_ldr   (code, ins->dreg, ins->sreg1);
			}
			break;
		case OP_MOVE_F_TO_I8: 
			s390_lgdr (code, ins->dreg, ins->sreg1);
			break;
		case OP_MOVE_I8_TO_F: 
			s390_ldgr (code, ins->dreg, ins->sreg1);
			break;
		case OP_MOVE_F_TO_I4:
			s390_ledbr (code, s390_f0, ins->sreg1);
			s390_lgdr (code, ins->dreg, s390_f0);
			s390_srag (code, ins->dreg, ins->dreg, 0, 32);
			break;
		case OP_MOVE_I4_TO_F: 
			s390_slag (code, s390_r0, ins->sreg1, 0, 32);
			s390_ldgr (code, ins->dreg, s390_r0);
			if (!cfg->r4fp)
				s390_ldebr (code, ins->dreg, ins->dreg);
			break;
		case OP_FCONV_TO_R4:
			s390_ledbr (code, ins->dreg, ins->sreg1);
			if (!cfg->r4fp)
				s390_ldebr (code, ins->dreg, ins->dreg);
			break;
		case OP_S390_SETF4RET:
			if (!cfg->r4fp)
				s390_ledbr (code, ins->dreg, ins->sreg1);
			break;
        case OP_TLS_GET: {
			if (s390_is_imm16 (ins->inst_offset)) {
				s390_lghi (code, s390_r13, ins->inst_offset);
			} else if (s390_is_imm32 (ins->inst_offset)) {
				s390_lgfi (code, s390_r13, ins->inst_offset);
			} else {
				S390_SET  (code, s390_r13, ins->inst_offset);
			}
			s390_ear (code, s390_r1, 0);
			s390_sllg(code, s390_r1, s390_r1, 0, 32);
			s390_ear (code, s390_r1, 1);
			s390_lg  (code, ins->dreg, s390_r13, s390_r1, 0);
			}
			break;
        	case OP_TLS_SET: {
			if (s390_is_imm16 (ins->inst_offset)) {
				s390_lghi (code, s390_r13, ins->inst_offset);
			} else if (s390_is_imm32 (ins->inst_offset)) {
				s390_lgfi (code, s390_r13, ins->inst_offset);
			} else {
				S390_SET  (code, s390_r13, ins->inst_offset);
			}
			s390_ear (code, s390_r1, 0);
			s390_sllg(code, s390_r1, s390_r1, 0, 32);
			s390_ear (code, s390_r1, 1);
			s390_stg (code, ins->sreg1, s390_r13, s390_r1, 0);
			}
			break;
		case OP_TAILCALL_PARAMETER :
			// This opcode helps compute sizes, i.e.
			// of the subsequent OP_TAILCALL, but contributes no code.
			g_assert (ins->next);
			break;
		case OP_TAILCALL :
		case OP_TAILCALL_REG :
		case OP_TAILCALL_MEMBASE : {
			MonoCallInst *call = (MonoCallInst *) ins;

			/*
			 * Restore SP to caller's SP
			 */ 
			code = backUpStackPtr(cfg, code);

			/*
			 * If the destination is specified as a register or membase then
			 * save destination so it doesn't get overwritten by the restores
			 */ 
			if (ins->opcode != OP_TAILCALL)
				s390_lgr (code, s390_r1, ins->sreg1);

			/*
			 * We have to restore R6, so it cannot be used as argument register.
			 * This is ensured by mono_arch_tailcall_supported, but verify here.
			 */
			g_assert (!(call->used_iregs & (1 << S390_LAST_ARG_REG)));

			/*
			 * Likewise for the IMT/RGCTX register
			 */
			g_assert (!(call->used_iregs & (1 << MONO_ARCH_RGCTX_REG)));
			g_assert (!(call->rgctx_reg));

			/*
			 * Restore all general registers
			 */
			s390_lmg (code, s390_r6, s390_r14, STK_BASE, S390_REG_SAVE_OFFSET);

			/*
			 * Restore any FP registers that have been altered
			 */ 
			if (cfg->arch.fpSize != 0) {
				int fpOffset = -cfg->arch.fpSize;
				for (int i = 8; i < 16; i++) {
					if (cfg->arch.used_fp_regs & (1 << i)) {
						s390_ldy (code, i, 0, STK_BASE, fpOffset);
						fpOffset += sizeof(double);
					}
				}
			}

			if (ins->opcode == OP_TAILCALL_REG) {
				s390_br (code, s390_r1);
			} else { 
				if (ins->opcode == OP_TAILCALL_MEMBASE) {
					if (mono_hwcap_s390x_has_mie2) {
						s390_bi (code, 0, s390_r1, ins->inst_offset);
					} else {
						s390_lg (code, s390_r1, 0, s390_r1, ins->inst_offset);
						s390_br (code, s390_r1);
					}
				} else {
					mono_add_patch_info (cfg, code - cfg->native_code, 
						 MONO_PATCH_INFO_METHOD_JUMP, 
						 call->method);
					S390_BR_TEMPLATE (code, s390_r1);
				}
			}
		}
			break;
		case OP_CHECK_THIS: {
			/* ensure ins->sreg1 is not NULL */
			s390_lg   (code, s390_r0, 0, ins->sreg1, 0);
			s390_ltgr (code, s390_r0, s390_r0);
		}
			break;
		case OP_ARGLIST: {
			const int offset = cfg->sig_cookie + cfg->stack_usage;

			S390_SET  (code, s390_r0, offset);
			s390_agr  (code, s390_r0, cfg->frame_reg);
			s390_stg  (code, s390_r0, 0, ins->sreg1, 0);
		}
			break;
		case OP_FCALL: {
			call = (MonoCallInst*)ins;

			mono_call_add_patch_info (cfg, call, code - cfg->native_code);
			S390_CALL_TEMPLATE (code, s390_r14);
			if (!cfg->r4fp && call->signature->ret->type == MONO_TYPE_R4)
				s390_ldebr (code, s390_f0, s390_f0);
		}
			break;
		case OP_LCALL:
		case OP_VCALL:
		case OP_VCALL2:
		case OP_VOIDCALL:
		case OP_RCALL:
		case OP_CALL: {
			call = (MonoCallInst*)ins;
			mono_call_add_patch_info (cfg, call, code - cfg->native_code);
			S390_CALL_TEMPLATE (code, s390_r14);
		}
			break;
		case OP_FCALL_REG: {
			call = (MonoCallInst*)ins;
			s390_lgr  (code, s390_r1, ins->sreg1);
			s390_basr (code, s390_r14, s390_r1);
			if (!cfg->r4fp && call->signature->ret->type == MONO_TYPE_R4)
				s390_ldebr (code, s390_f0, s390_f0);
		}
			break;
		case OP_LCALL_REG:
		case OP_VCALL_REG:
		case OP_VCALL2_REG:
		case OP_VOIDCALL_REG:
		case OP_RCALL_REG:
		case OP_CALL_REG: {
			s390_lgr  (code, s390_r1, ins->sreg1);
			s390_basr (code, s390_r14, s390_r1);
		}
			break;
		case OP_FCALL_MEMBASE: {
			call = (MonoCallInst*)ins;
			s390_lg   (code, s390_r1, 0, ins->sreg1, ins->inst_offset);
			s390_basr (code, s390_r14, s390_r1);
			if (!cfg->r4fp && call->signature->ret->type == MONO_TYPE_R4)
				s390_ldebr (code, s390_f0, s390_f0);
		}
			break;
		case OP_LCALL_MEMBASE:
		case OP_VCALL_MEMBASE:
		case OP_VCALL2_MEMBASE:
		case OP_VOIDCALL_MEMBASE:
		case OP_RCALL_MEMBASE:
		case OP_CALL_MEMBASE: {
			s390_lg   (code, s390_r1, 0, ins->sreg1, ins->inst_offset);
			s390_basr (code, s390_r14, s390_r1);
		}
			break;
		case OP_LOCALLOC: {
			int area_offset;

			if (cfg->param_area == 0)
				area_offset = S390_MINIMAL_STACK_SIZE;
			else
				area_offset = cfg->param_area;

			area_offset = S390_ALIGN(area_offset, S390_STACK_ALIGNMENT);

                        /*
                         * Get alloc size and round to doubleword
                         */  
			s390_lgr  (code, s390_r1, ins->sreg1);
			s390_aghi (code, s390_r1, 14);
			s390_srlg (code, s390_r1, s390_r1, 0, 3);
			s390_sllg (code, s390_r1, s390_r1, 0, 3);

                        /*
                         * If we need to initialize then hold on to the length
                         */ 
			if (ins->flags & MONO_INST_INIT) 
                                s390_lgr  (code, s390_r0, s390_r1);

                        /*
                         * Adjust the stack pointer and save the backchain
                         */ 
			s390_lg   (code, s390_r13, 0, STK_BASE, 0);
			s390_sgr  (code, STK_BASE, s390_r1);
			s390_stg  (code, s390_r13, 0, STK_BASE, 0);

                        /*
                         * Skip the stack save requirements and point to localloc area 
                         * and ensure it's correctly aligned
                         */
			s390_la   (code, ins->dreg, 0, STK_BASE, area_offset);
			s390_aghi (code, ins->dreg, 7);
			s390_srlg (code, ins->dreg, ins->dreg, 0, 3);
			s390_sllg (code, ins->dreg, ins->dreg, 0, 3);

                        /*
                         * If we need to zero the area then clear from localloc start
                         * using the length we saved earlier
                         */ 
			if (ins->flags & MONO_INST_INIT) {
				s390_lgr  (code, s390_r1, s390_r0);
				s390_lgr  (code, s390_r0, ins->dreg);
				s390_lgr  (code, s390_r14, s390_r12);
				s390_lghi (code, s390_r13, 0);
				s390_mvcle(code, s390_r0, s390_r12, 0, 0);
				s390_jo   (code, -2);
				s390_lgr  (code, s390_r12, s390_r14);
			}

                        /*
                         * If we have an LMF then we have to adjust its BP 
                         */
			if (cfg->method->save_lmf) {
				int lmfOffset = cfg->stack_usage - sizeof(MonoLMF);

				if (s390_is_imm16(lmfOffset)) {
					s390_lghi (code, s390_r13, lmfOffset);
				} else if (s390_is_imm32(lmfOffset)) {
					s390_lgfi (code, s390_r13, lmfOffset);
				} else {
					S390_SET  (code, s390_r13, lmfOffset);
				}
				s390_stg (code, s390_r15, s390_r13, cfg->frame_reg,
                                          MONO_STRUCT_OFFSET(MonoLMF, ebp));
			}
		}
			break;
		case OP_THROW: {
			s390_lgr  (code, s390_r2, ins->sreg1);
			mono_add_patch_info (cfg, code-cfg->native_code, MONO_PATCH_INFO_JIT_ICALL_ID,
					     GUINT_TO_POINTER (MONO_JIT_ICALL_mono_arch_throw_exception));
			S390_CALL_TEMPLATE(code, s390_r14);
		}
			break;
		case OP_RETHROW: {
			s390_lgr  (code, s390_r2, ins->sreg1);
			mono_add_patch_info (cfg, code-cfg->native_code, MONO_PATCH_INFO_JIT_ICALL_ID,
					     GUINT_TO_POINTER (MONO_JIT_ICALL_mono_arch_rethrow_exception));
			S390_CALL_TEMPLATE(code, s390_r14);
		}
			break;
		case OP_START_HANDLER: {
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);

			S390_LONG (code, stg, stg, s390_r14, 0,
				   spvar->inst_basereg, 
				   spvar->inst_offset);
		}
			break;
		case OP_ENDFILTER: {
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);

			if (ins->sreg1 != s390_r2)
				s390_lgr(code, s390_r2, ins->sreg1);
			S390_LONG (code, lg, lg, s390_r14, 0,
				   spvar->inst_basereg, 
				   spvar->inst_offset);
			s390_br  (code, s390_r14);
		}
			break;
		case OP_ENDFINALLY: {
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);

			S390_LONG (code, lg, lg, s390_r14, 0,
				   spvar->inst_basereg, 
				   spvar->inst_offset);
			s390_br  (code, s390_r14);
		}
			break;
		case OP_CALL_HANDLER: {
			mono_add_patch_info (cfg, code-cfg->native_code, 
					     MONO_PATCH_INFO_BB, ins->inst_target_bb);
			s390_brasl (code, s390_r14, 0);
			for (GList *tmp = ins->inst_eh_blocks; tmp != bb->clause_holes; tmp = tmp->prev)
				mono_cfg_add_try_hole (cfg, ((MonoLeaveClause *) tmp->data)->clause, code, bb);
		}
			break;
		case OP_LABEL: {
			ins->inst_c0 = code - cfg->native_code;
		}
			break;
		case OP_RELAXED_NOP:
		case OP_NOP:
		case OP_DUMMY_USE:
		case OP_DUMMY_ICONST:
		case OP_DUMMY_I8CONST:
		case OP_DUMMY_R8CONST:
		case OP_DUMMY_R4CONST:
		case OP_NOT_REACHED:
		case OP_NOT_NULL: {
		}
			break;
		case OP_IL_SEQ_POINT:
			mono_add_seq_point (cfg, bb, ins, code - cfg->native_code);
			break;
		case OP_SEQ_POINT: {

			MonoInst *var;
			RI_Format *o[2];
			guint16 displace;

			if (cfg->compile_aot)
				NOT_IMPLEMENTED;

 			if (ins->flags & MONO_INST_SINGLE_STEP_LOC) {
				var = cfg->arch.ss_tramp_var;
				s390_lg (code, s390_r1, 0, var->inst_basereg, var->inst_offset);
				if (mono_hwcap_s390x_has_eif) {
					s390_ltg (code, s390_r14, 0, s390_r1, 0);
				} else {
					s390_lg (code, s390_r14, 0, s390_r1, 0);
					s390_ltgr (code, s390_r14, s390_r14);
				}
				o[0] = (RI_Format *) code;
				s390_jz (code, 4);
				s390_lgr (code, s390_r1, cfg->frame_reg);
				s390_basr (code, s390_r14, s390_r14);
				displace = ((uintptr_t) code - (uintptr_t) o[0]) / 2;
				o[0]->i2 = displace;
 			}
 
			/* 
			 * This is the address which is saved in seq points, 
			 */
			mono_add_seq_point (cfg, bb, ins, code - cfg->native_code);

			var = cfg->arch.bp_tramp_var;
			s390_lghi (code, s390_r1, 0);
			s390_ltgr (code, s390_r1, s390_r1);
			o[0] = (RI_Format *) code;
			s390_jz   (code, 0);
			s390_lg (code, s390_r1, 0, var->inst_basereg, var->inst_offset);
			if (mono_hwcap_s390x_has_eif) {
				s390_ltg (code, s390_r14, 0, s390_r1, 0);
			} else {
				s390_lg (code, s390_r1, 0, s390_r1, 0);
				s390_ltgr (code, s390_r14, s390_r1);
			}
			o[1] = (RI_Format *) code;
			s390_jz (code, 4);
			s390_lgr (code, s390_r1, cfg->frame_reg);
			s390_basr (code, s390_r14, s390_r14);
			displace = ((uintptr_t) code - (uintptr_t) o[0]) / 2;
			o[0]->i2 = displace;
			displace = ((uintptr_t) code - (uintptr_t) o[1]) / 2;
			o[1]->i2 = displace;

			/*
			 * Add an additional nop so skipping the bp doesn't cause the ip to point
			 * to another IL offset.
			 */
			s390_nop (code);

			break;
		}
		case OP_GENERIC_CLASS_INIT: {
			static int byte_offset = -1;
			static guint8 bitmask;
			short int *jump;

			g_assert (ins->sreg1 == S390_FIRST_ARG_REG);

			if (byte_offset < 0)
				mono_marshal_find_bitfield_offset (MonoVTable, initialized, &byte_offset, &bitmask);

			s390_tm (code, ins->sreg1, byte_offset, bitmask);
			s390_jo (code, 0); CODEPTR(code, jump);

			mono_add_patch_info (cfg, code-cfg->native_code, MONO_PATCH_INFO_JIT_ICALL_ID,
			           			 GUINT_TO_POINTER (MONO_JIT_ICALL_mono_generic_class_init));
			S390_CALL_TEMPLATE(code, s390_r14);

			PTRSLOT (code, jump);

			ins->flags |= MONO_INST_GC_CALLSITE;
			ins->backend.pc_offset = code - cfg->native_code;
			break;
		}
		case OP_BR: 
			EMIT_UNCOND_BRANCH(ins);
			break;
		case OP_BR_REG: {
			s390_br	 (code, ins->sreg1);
		}
			break;
		case OP_CEQ: 
		case OP_ICEQ:
		case OP_LCEQ: {
			s390_lghi(code, ins->dreg, 1);
			s390_jz  (code, 4);
			s390_lghi(code, ins->dreg, 0);
		}
			break;
		case OP_CLT: 
		case OP_ICLT:
		case OP_LCLT: {
			s390_lghi(code, ins->dreg, 1);
			s390_jl  (code, 4);
			s390_lghi(code, ins->dreg, 0);
		}
			break;
		case OP_CLT_UN:
		case OP_ICLT_UN:
		case OP_LCLT_UN: {
			s390_lghi(code, ins->dreg, 1);
			s390_jlo (code, 4);
			s390_lghi(code, ins->dreg, 0);
		}
			break;
		case OP_CGT: 
		case OP_ICGT:
		case OP_LCGT: {
			s390_lghi(code, ins->dreg, 1);
			s390_jh  (code, 4);
			s390_lghi(code, ins->dreg, 0);
		}
			break;
		case OP_CGT_UN:
		case OP_ICGT_UN:
		case OP_LCGT_UN: {
			s390_lghi(code, ins->dreg, 1);
			s390_jho (code, 4);
			s390_lghi(code, ins->dreg, 0);
		}
			break;
		case OP_ICNEQ: {
			s390_lghi(code, ins->dreg, 1);
			s390_jne (code, 4);
			s390_lghi(code, ins->dreg, 0);
		}
			break;
		case OP_ICGE: {
			s390_lghi(code, ins->dreg, 1);
			s390_jhe (code, 4);
			s390_lghi(code, ins->dreg, 0);
		}
			break;
		case OP_ICLE: {
			s390_lghi(code, ins->dreg, 1);
			s390_jle (code, 4);
			s390_lghi(code, ins->dreg, 0);
		}
			break;
		case OP_ICGE_UN: {
			s390_lghi(code, ins->dreg, 1);
			s390_jhe (code, 4);
			s390_lghi(code, ins->dreg, 0);
		}
			break;
		case OP_ICLE_UN: {
			s390_lghi(code, ins->dreg, 1);
			s390_jle (code, 4);
			s390_lghi(code, ins->dreg, 0);
		}
			break;
		case OP_COND_EXC_EQ:
		case OP_COND_EXC_IEQ:
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_EQ, ins->inst_p1);
			break;
		case OP_COND_EXC_NE_UN:
		case OP_COND_EXC_INE_UN:
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_NE, ins->inst_p1);
			break;
		case OP_COND_EXC_LT:
		case OP_COND_EXC_ILT:
		case OP_COND_EXC_LT_UN:
		case OP_COND_EXC_ILT_UN:
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_LT, ins->inst_p1);
			break;
		case OP_COND_EXC_GT:
		case OP_COND_EXC_IGT:
		case OP_COND_EXC_GT_UN:
		case OP_COND_EXC_IGT_UN:
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_GT, ins->inst_p1);
			break;
		case OP_COND_EXC_GE:
		case OP_COND_EXC_IGE:
		case OP_COND_EXC_GE_UN:
		case OP_COND_EXC_IGE_UN:
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_GE, ins->inst_p1);
			break;
		case OP_COND_EXC_LE:
		case OP_COND_EXC_ILE:
		case OP_COND_EXC_LE_UN:
		case OP_COND_EXC_ILE_UN:
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_LE, ins->inst_p1);
			break;
		case OP_COND_EXC_OV:
		case OP_COND_EXC_IOV:
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_OV, ins->inst_p1);
			break;
		case OP_COND_EXC_NO:
		case OP_COND_EXC_INO:
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_NO, ins->inst_p1);
			break;
		case OP_COND_EXC_C:
		case OP_COND_EXC_IC:
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_CY, ins->inst_p1);
			break;
		case OP_COND_EXC_NC:
		case OP_COND_EXC_INC:
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_NC, ins->inst_p1);
			break;
		case OP_LBEQ:
		case OP_IBEQ:
			EMIT_COND_BRANCH (ins, S390_CC_EQ);
			break;	
		case OP_LBNE_UN:
		case OP_IBNE_UN:
			EMIT_COND_BRANCH (ins, S390_CC_NE);
			break;	
		case OP_LBLT:
		case OP_LBLT_UN:
		case OP_IBLT:
		case OP_IBLT_UN:
			EMIT_COND_BRANCH (ins, S390_CC_LT);
			break;	
		case OP_LBGT:
		case OP_LBGT_UN:
		case OP_IBGT:
		case OP_IBGT_UN:
			EMIT_COND_BRANCH (ins, S390_CC_GT);
			break;	
		case OP_LBGE:
		case OP_LBGE_UN:
		case OP_IBGE:
		case OP_IBGE_UN:
			EMIT_COND_BRANCH (ins, S390_CC_GE);
			break;	
		case OP_LBLE:
		case OP_LBLE_UN:
		case OP_IBLE:
		case OP_IBLE_UN:
			EMIT_COND_BRANCH (ins, S390_CC_LE);
			break;

		case OP_S390_CRJ:
			EMIT_COMP_AND_BRANCH(ins, crj, cr);
			break;

		case OP_S390_CLRJ:
			EMIT_COMP_AND_BRANCH(ins, clrj, clr);
			break;

		case OP_S390_CGRJ:
			EMIT_COMP_AND_BRANCH(ins, cgrj, cgr);
			break;

		case OP_S390_CLGRJ:
			EMIT_COMP_AND_BRANCH(ins, clgrj, clgr);
			break;

		case OP_S390_CIJ:
			EMIT_COMP_AND_BRANCH_IMM(ins, crj, cr, ltr, FALSE);
			break;

		case OP_S390_CLIJ:
			EMIT_COMP_AND_BRANCH_IMM(ins, clrj, clr, ltr, TRUE);
			break;

		case OP_S390_CGIJ:
			EMIT_COMP_AND_BRANCH_IMM(ins, cgrj, cgr, ltgr, FALSE);
			break;

		case OP_S390_CLGIJ:
			EMIT_COMP_AND_BRANCH_IMM(ins, clgrj, clgr, ltgr, TRUE);
			break;

		/* floating point opcodes */
		case OP_R8CONST: {
			double d = *(double *) ins->inst_p0;
			if (d == 0) {
				s390_lzdr (code, ins->dreg);
				if (mono_signbit (d) != 0)
					s390_lndbr (code, ins->dreg, ins->dreg);
			} else {
				S390_SET  (code, s390_r13, ins->inst_p0);
				s390_ld (code, ins->dreg, 0, s390_r13, 0);
			}
		}
			break;
		case OP_R4CONST: {
			float f = *(float *) ins->inst_p0;
			if (f == 0) {
				if (cfg->r4fp) {
					s390_lzer (code, ins->dreg);
					if (mono_signbit (f) != 0)
						s390_lnebr (code, ins->dreg, ins->dreg);
				} else {
					s390_lzdr (code, ins->dreg);
					if (mono_signbit (f) != 0)
						s390_lndbr (code, ins->dreg, ins->dreg);
				}
			} else {
				S390_SET (code, s390_r13, ins->inst_p0);
				s390_le (code, ins->dreg, 0, s390_r13, 0);
				if (!cfg->r4fp) {
					s390_ldebr (code, ins->dreg, ins->dreg);
				}
			}
		}
			break;
		case OP_STORER8_MEMBASE_REG: {
			S390_LONG (code, stdy, std, ins->sreg1, 0, 
				   ins->inst_destbasereg, ins->inst_offset);
		}
			break;
		case OP_LOADR8_MEMBASE: {
			S390_LONG (code, ldy, ld, ins->dreg, 0, 
				   ins->inst_basereg, ins->inst_offset);
		}
			break;
		case OP_STORER4_MEMBASE_REG: {
			if (cfg->r4fp) {
				S390_LONG (code, stey, ste, ins->sreg1, 0, 
					   ins->inst_destbasereg, ins->inst_offset);
			} else {
				s390_ledbr (code, ins->sreg1, ins->sreg1);
				S390_LONG (code, stey, ste, ins->sreg1, 0, 
					   ins->inst_destbasereg, ins->inst_offset);
				s390_ldebr (code, ins->sreg1, ins->sreg1);
			}
		}
			break;
		case OP_LOADR4_MEMBASE: {
			if (cfg->r4fp) {
				S390_LONG (code, ley, le, ins->dreg, 0, 
					   ins->inst_basereg, ins->inst_offset);
			} else {
				S390_LONG (code, ley, le, ins->dreg, 0, 
					   ins->inst_basereg, ins->inst_offset);
				s390_ldebr (code, ins->dreg, ins->dreg);
			}
		}
			break;
		case OP_ICONV_TO_R_UN: {
			if (mono_hwcap_s390x_has_fpe) {
				s390_cdlfbr (code, ins->dreg, 5, ins->sreg1, 0);
			} else {
				s390_llgfr (code, s390_r0, ins->sreg1);
				s390_cdgbr (code, ins->dreg, s390_r0);
			}
		}
			break;
		case OP_LCONV_TO_R_UN: {
			if (mono_hwcap_s390x_has_fpe) {
				s390_cdlgbr (code, ins->dreg, 5, ins->sreg1, 0);
			} else {
				short int *jump;
				s390_lgdr  (code, s390_r0, s390_r15);
				s390_lgdr  (code, s390_r1, s390_r13);
				s390_lgdr  (code, s390_r14, s390_r12);
				s390_cxgbr (code, s390_f12, ins->sreg1);
				s390_ltgr  (code, ins->sreg1, ins->sreg1);
				s390_jnl   (code, 0); CODEPTR(code, jump);
				S390_SET   (code, s390_r13, 0x403f000000000000llu);
				s390_lgdr  (code, s390_f13, s390_r13);
				s390_lzdr  (code, s390_f15);
				s390_axbr  (code, s390_f12, s390_f13);
				PTRSLOT(code, jump);
				s390_ldxbr (code, s390_f13, s390_f12);
				s390_ldr   (code, ins->dreg, s390_f13);
				s390_ldgr  (code, s390_f12, s390_r14);
				s390_ldgr  (code, s390_f13, s390_r1);
				s390_ldgr  (code, s390_f15, s390_r0);
			}
		}
			break;
		case OP_ICONV_TO_R4:
			s390_cefbr (code, ins->dreg, ins->sreg1);
			if (!cfg->r4fp)
				s390_ldebr (code, ins->dreg, ins->dreg);
			break;
		case OP_LCONV_TO_R4:
			s390_cegbr (code, ins->dreg, ins->sreg1);
			if (!cfg->r4fp)
				s390_ldebr (code, ins->dreg, ins->dreg);
			break;
		case OP_ICONV_TO_R8:
			s390_cdfbr (code, ins->dreg, ins->sreg1);
			break;
		case OP_LCONV_TO_R8:
			s390_cdgbr (code, ins->dreg, ins->sreg1);
			break;
		case OP_FCONV_TO_I1:
			s390_cgdbr (code, ins->dreg, 5, ins->sreg1);
			s390_ltgr  (code, ins->dreg, ins->dreg);
			s390_jnl   (code, 4);
			s390_oill  (code, ins->dreg, 0x80);
			s390_lghi  (code, s390_r0, 0xff);
			s390_ngr   (code, ins->dreg, s390_r0);
			break;
		case OP_FCONV_TO_U1:
			if (mono_hwcap_s390x_has_fpe) {
				s390_clgdbr (code, ins->dreg, 5, ins->sreg1, 0);
				s390_lghi  (code, s390_r0, 0xff);
				s390_ngr   (code, ins->dreg, s390_r0);
			} else {
				code = emit_double_to_int (cfg, code, ins->dreg, ins->sreg1, 1, FALSE);
			}
			break;
		case OP_FCONV_TO_I2:
			s390_cgdbr (code, ins->dreg, 5, ins->sreg1);
			s390_ltgr  (code, ins->dreg, ins->dreg);
			s390_jnl   (code, 4);
			s390_oill  (code, ins->dreg, 0x8000);
			s390_llill (code, s390_r0, 0xffff);
			s390_ngr   (code, ins->dreg, s390_r0);
			break;
		case OP_FCONV_TO_U2:
			if (mono_hwcap_s390x_has_fpe) {
				s390_clgdbr (code, ins->dreg, 5, ins->sreg1, 0);
				s390_llill  (code, s390_r0, 0xffff);
				s390_ngr    (code, ins->dreg, s390_r0);
			} else {
				code = emit_double_to_int (cfg, code, ins->dreg, ins->sreg1, 2, FALSE);
			}
			break;
		case OP_FCONV_TO_I4:
		case OP_FCONV_TO_I:
			s390_cfdbr (code, ins->dreg, 5, ins->sreg1);
			break;
		case OP_FCONV_TO_U4:
		case OP_FCONV_TO_U:
			if (mono_hwcap_s390x_has_fpe) {
				s390_clfdbr (code, ins->dreg, 5, ins->sreg1, 0);
			} else {
				code = emit_double_to_int (cfg, code, ins->dreg, ins->sreg1, 4, FALSE);
			}
			break;
		case OP_FCONV_TO_I8:
			s390_cgdbr (code, ins->dreg, 5, ins->sreg1);
			break;
		case OP_FCONV_TO_U8:
			if (mono_hwcap_s390x_has_fpe) {
				s390_clgdbr (code, ins->dreg, 5, ins->sreg1, 0);
			} else {
				code = emit_double_to_int (cfg, code, ins->dreg, ins->sreg1, 8, FALSE);
			}
			break;
		case OP_RCONV_TO_I1:
			s390_cgebr (code, ins->dreg, 5, ins->sreg1);
			s390_ltgr  (code, ins->dreg, ins->dreg);
			s390_jnl   (code, 4);
			s390_oill  (code, ins->dreg, 0x80);
			s390_lghi  (code, s390_r0, 0xff);
			s390_ngr   (code, ins->dreg, s390_r0);
			break;
		case OP_RCONV_TO_U1:
			if (mono_hwcap_s390x_has_fpe) {
				s390_clgebr (code, ins->dreg, 5, ins->sreg1, 0);
				s390_lghi  (code, s390_r0, 0xff);
				s390_ngr   (code, ins->dreg, s390_r0);
			} else {
				code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 1, FALSE);
			}
			break;
		case OP_RCONV_TO_I2:
			s390_cgebr (code, ins->dreg, 5, ins->sreg1);
			s390_ltgr  (code, ins->dreg, ins->dreg);
			s390_jnl   (code, 4);
			s390_oill  (code, ins->dreg, 0x8000);
			s390_llill (code, s390_r0, 0xffff);
			s390_ngr   (code, ins->dreg, s390_r0);
			break;
		case OP_RCONV_TO_U2:
			if (mono_hwcap_s390x_has_fpe) {
				s390_clgebr (code, ins->dreg, 5, ins->sreg1, 0);
				s390_llill  (code, s390_r0, 0xffff);
				s390_ngr    (code, ins->dreg, s390_r0);
			} else {
				code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 2, FALSE);
			}
			break;
		case OP_RCONV_TO_I4:
		case OP_RCONV_TO_I:
			s390_cfebr (code, ins->dreg, 5, ins->sreg1);
			break;
		case OP_RCONV_TO_U4:
			if (mono_hwcap_s390x_has_fpe) {
				s390_clfebr (code, ins->dreg, 5, ins->sreg1, 0);
			} else {
				code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 4, FALSE);
			}
			break;
		case OP_RCONV_TO_I8:
			s390_cgebr (code, ins->dreg, 5, ins->sreg1);
			break;
		case OP_RCONV_TO_U8:
			if (mono_hwcap_s390x_has_fpe) {
				s390_clgebr (code, ins->dreg, 5, ins->sreg1, 0);
			} else {
				code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 8, FALSE);
			}
			break;
		case OP_LCONV_TO_OVF_I: {
			/* Valid ints: 0xffffffff:8000000 to 00000000:0x7f000000 */
			short int *o[5];
			s390_ltgr (code, ins->sreg2, ins->sreg2);
			s390_jnl  (code, 0); CODEPTR(code, o[0]);
			s390_ltgr (code, ins->sreg1, ins->sreg1);
			s390_jnl  (code, 0); CODEPTR(code, o[1]);
			s390_lhi  (code, s390_r13, -1);
			s390_cgr  (code, ins->sreg1, s390_r13);
			s390_jnz  (code, 0); CODEPTR(code, o[2]);
			if (ins->dreg != ins->sreg2)
				s390_lgr  (code, ins->dreg, ins->sreg2);
			s390_j	  (code, 0); CODEPTR(code, o[3]);
			PTRSLOT(code, o[0]);
			s390_jz   (code, 0); CODEPTR(code, o[4]);
			PTRSLOT(code, o[1]);
			PTRSLOT(code, o[2]);
			mono_add_patch_info (cfg, code - cfg->native_code, 
					     MONO_PATCH_INFO_EXC, "OverflowException");
			s390_brasl (code, s390_r14, 0);
			PTRSLOT(code, o[3]);
			PTRSLOT(code, o[4]);
		}
			break;
		case OP_ABS: {
			s390_lpdbr (code, ins->dreg, ins->sreg1);
		}
			break;
		case OP_SQRT: {
			s390_sqdbr (code, ins->dreg, ins->sreg1);
		}
			break;
		case OP_FADD: {
			CHECK_SRCDST_COM_F;
			s390_adbr (code, ins->dreg, src2);
		}
			break;
		case OP_RADD: {
			CHECK_SRCDST_COM_F;
			s390_aebr (code, ins->dreg, src2);
		}
			break;
		case OP_FSUB: {
			CHECK_SRCDST_NCOM_F(sdbr);
		}
			break;		
		case OP_RSUB: {
			CHECK_SRCDST_NCOM_F(sebr);
		}
			break;		
		case OP_FMUL: {
			CHECK_SRCDST_COM_F;
			s390_mdbr (code, ins->dreg, src2);
		}
			break;		
		case OP_RMUL: {
			CHECK_SRCDST_COM_F;
			s390_meer (code, ins->dreg, src2);
		}
			break;		
		case OP_FDIV: {
			CHECK_SRCDST_NCOM_F(ddbr);
		}
			break;		
		case OP_RDIV: {
			CHECK_SRCDST_NCOM_F(debr);
		}
			break;		
		case OP_FNEG: {
			s390_lcdbr (code, ins->dreg, ins->sreg1);
		}
			break;		
		case OP_RNEG: {
			s390_lcebr (code, ins->dreg, ins->sreg1);
		}
			break;		
		case OP_FREM: {
			CHECK_SRCDST_NCOM_FR(didbr, 5);
		}
			break;
		case OP_RREM: {
			CHECK_SRCDST_NCOM_FR(diebr, 5);
		}
			break;
		case OP_FCOMPARE: {
			s390_cdbr (code, ins->sreg1, ins->sreg2);
		}
			break;
		case OP_RCOMPARE: {
			s390_cebr (code, ins->sreg1, ins->sreg2);
		}
			break;
		case OP_FCEQ: {
			s390_cdbr  (code, ins->sreg1, ins->sreg2);
			s390_lghi  (code, ins->dreg, 1);
			s390_je    (code, 4);
			s390_lghi  (code, ins->dreg, 0);
		}
			break;
		case OP_FCLT: {
			s390_cdbr  (code, ins->sreg1, ins->sreg2);
			s390_lghi  (code, ins->dreg, 1);
			s390_jl    (code, 4);
			s390_lghi  (code, ins->dreg, 0);
		}
			break;
		case OP_FCLT_UN: {
			s390_cdbr  (code, ins->sreg1, ins->sreg2);
			s390_lghi  (code, ins->dreg, 1);
			s390_jlo   (code, 4);
			s390_lghi  (code, ins->dreg, 0);
		}
			break;
		case OP_FCGT: {
			s390_cdbr  (code, ins->sreg1, ins->sreg2);
			s390_lghi  (code, ins->dreg, 1);
			s390_jh    (code, 4);
			s390_lghi  (code, ins->dreg, 0);
		}
			break;
		case OP_FCGT_UN: {
			s390_cdbr  (code, ins->sreg1, ins->sreg2);
			s390_lghi  (code, ins->dreg, 1);
			s390_jho   (code, 4);
			s390_lghi  (code, ins->dreg, 0);
		}
			break;
		case OP_FCNEQ: {
			s390_cdbr  (code, ins->sreg1, ins->sreg2);
			s390_lghi  (code, ins->dreg, 1);
			s390_jne   (code, 4);
			s390_lghi  (code, ins->dreg, 0);
		}
			break;
		case OP_FCGE: {
			s390_cdbr  (code, ins->sreg1, ins->sreg2);
			s390_lghi  (code, ins->dreg, 1);
			s390_jhe   (code, 4);
			s390_lghi  (code, ins->dreg, 0);
		}
			break;
		case OP_FCLE: {
			s390_cdbr  (code, ins->sreg1, ins->sreg2);
			s390_lghi  (code, ins->dreg, 1);
			s390_jle   (code, 4);
			s390_lghi  (code, ins->dreg, 0);
		}
			break;
		case OP_RCEQ: {
			s390_cdbr  (code, ins->sreg1, ins->sreg2);
			s390_lghi  (code, ins->dreg, 1);
			s390_je    (code, 4);
			s390_lghi  (code, ins->dreg, 0);
		}
			break;
		case OP_RCLT: {
			s390_cdbr  (code, ins->sreg1, ins->sreg2);
			s390_lghi  (code, ins->dreg, 1);
			s390_jl    (code, 4);
			s390_lghi  (code, ins->dreg, 0);
		}
			break;
		case OP_RCLT_UN: {
			s390_cdbr  (code, ins->sreg1, ins->sreg2);
			s390_lghi  (code, ins->dreg, 1);
			s390_jlo   (code, 4);
			s390_lghi  (code, ins->dreg, 0);
		}
			break;
		case OP_RCGT: {
			s390_cdbr  (code, ins->sreg1, ins->sreg2);
			s390_lghi  (code, ins->dreg, 1);
			s390_jh    (code, 4);
			s390_lghi  (code, ins->dreg, 0);
		}
			break;
		case OP_RCGT_UN: {
			s390_cdbr  (code, ins->sreg1, ins->sreg2);
			s390_lghi  (code, ins->dreg, 1);
			s390_jho   (code, 4);
			s390_lghi  (code, ins->dreg, 0);
		}
			break;
		case OP_RCNEQ: {
			s390_cdbr  (code, ins->sreg1, ins->sreg2);
			s390_lghi  (code, ins->dreg, 1);
			s390_jne   (code, 4);
			s390_lghi  (code, ins->dreg, 0);
		}
			break;
		case OP_RCGE: {
			s390_cdbr  (code, ins->sreg1, ins->sreg2);
			s390_lghi  (code, ins->dreg, 1);
			s390_jhe   (code, 4);
			s390_lghi  (code, ins->dreg, 0);
		}
			break;
		case OP_RCLE: {
			s390_cebr  (code, ins->sreg1, ins->sreg2);
			s390_lghi  (code, ins->dreg, 1);
			s390_jle   (code, 4);
			s390_lghi  (code, ins->dreg, 0);
		}
			break;
		case OP_FBEQ: {
			short *o;
			s390_jo (code, 0); CODEPTR(code, o);
			EMIT_COND_BRANCH (ins, S390_CC_EQ);
			PTRSLOT (code, o);
		}
			break;
		case OP_FBNE_UN:
			EMIT_COND_BRANCH (ins, S390_CC_NE|S390_CC_OV);
			break;
		case OP_FBLT: {
			short *o;
			s390_jo (code, 0); CODEPTR(code, o);
			EMIT_COND_BRANCH (ins, S390_CC_LT);
			PTRSLOT (code, o);
		}
			break;
		case OP_FBLT_UN:
			EMIT_COND_BRANCH (ins, S390_CC_LT|S390_CC_OV);
			break;
		case OP_FBGT: {
			short *o;
			s390_jo (code, 0); CODEPTR(code, o);
			EMIT_COND_BRANCH (ins, S390_CC_GT);
			PTRSLOT (code, o);
		}
			break;
		case OP_FBGT_UN:
			EMIT_COND_BRANCH (ins, S390_CC_GT|S390_CC_OV);
			break;
		case OP_FBGE: {
			short *o;
			s390_jo (code, 0); CODEPTR(code, o);
			EMIT_COND_BRANCH (ins, S390_CC_GE);
			PTRSLOT (code, o);
		}
			break;
		case OP_FBGE_UN:
			EMIT_COND_BRANCH (ins, S390_CC_GE|S390_CC_OV);
			break;
		case OP_FBLE: {
			short *o;
			s390_jo (code, 0); CODEPTR(code, o);
			EMIT_COND_BRANCH (ins, S390_CC_LE);
			PTRSLOT (code, o);
		}
			break;
		case OP_FBLE_UN:
			EMIT_COND_BRANCH (ins, S390_CC_LE|S390_CC_OV);
			break;
		case OP_CKFINITE: {
			short *o;
			s390_lhi  (code, s390_r13, 0x7f);
			s390_tcdb (code, ins->sreg1, 0, s390_r13, 0);
			s390_jz   (code, 0); CODEPTR(code, o);
			mono_add_patch_info (cfg, code - cfg->native_code, 
					     MONO_PATCH_INFO_EXC, "OverflowException");
			s390_brasl (code, s390_r14,0);
			PTRSLOT(code, o);
		}
			break;
		case OP_S390_MOVE: {
			if (ins->backend.size > 0) {
				if (ins->backend.size <= 256) {
					s390_mvc  (code, ins->backend.size, ins->sreg2, 
						   ins->inst_offset, ins->sreg1, ins->inst_imm);
				} else {
					s390_lgr  (code, s390_r0, ins->sreg2);
					if (ins->inst_offset > 0) {
						if (s390_is_imm16 (ins->inst_offset)) {
							s390_aghi (code, s390_r0, ins->inst_offset);
						} else if (s390_is_imm32 (ins->inst_offset)) {
							s390_agfi (code, s390_r0, ins->inst_offset);
						} else {
							S390_SET  (code, s390_r13, ins->inst_offset);
							s390_agr  (code, s390_r0, s390_r13);
						}
					}
					s390_lgr  (code, s390_r12, ins->sreg1);
					if (ins->inst_imm > 0) {
						if (s390_is_imm16 (ins->inst_imm)) {
							s390_aghi (code, s390_r12, ins->inst_imm);
						} else if (s390_is_imm32 (ins->inst_imm)) {
							s390_agfi (code, s390_r12, ins->inst_imm);
						} else {
							S390_SET  (code, s390_r13, ins->inst_imm);
							s390_agr  (code, s390_r12, s390_r13);
						}
					}
					if (s390_is_imm16 (ins->backend.size)) {
						s390_lghi (code, s390_r1, ins->backend.size);
					} else if (s390_is_imm32 (ins->inst_offset)) {
						s390_agfi (code, s390_r1, ins->backend.size);
					} else {
						S390_SET  (code, s390_r13, ins->backend.size);
						s390_agr  (code, s390_r1, s390_r13);
					}
					s390_lgr  (code, s390_r13, s390_r1);
					s390_mvcle(code, s390_r0, s390_r12, 0, 0);
					s390_jo   (code, -2);
				}
			}
		}
			break;
		case OP_ATOMIC_ADD_I8: {
			if (mono_hwcap_s390x_has_ia) {
				s390_laag(code, s390_r0, ins->sreg2, ins->inst_basereg, ins->inst_offset);
				if (mono_hwcap_s390x_has_mlt) {
				    s390_agrk(code, ins->dreg, s390_r0, ins->sreg2);
				} else {
				    s390_agr (code, s390_r0, ins->sreg2);
				    s390_lgr (code, ins->dreg, s390_r0);
				}
			} else {
				s390_lgr (code, s390_r1, ins->sreg2);
				s390_lg  (code, s390_r0, 0, ins->inst_basereg, ins->inst_offset);
				s390_agr (code, s390_r1, s390_r0);
				s390_csg (code, s390_r0, s390_r1, ins->inst_basereg, ins->inst_offset);
				s390_jnz (code, -10);
				s390_lgr (code, ins->dreg, s390_r1);
			}
		}
			break;	
		case OP_ATOMIC_EXCHANGE_I8: {
			s390_lg  (code, s390_r0, 0, ins->inst_basereg, ins->inst_offset);
			s390_csg (code, s390_r0, ins->sreg2, ins->inst_basereg, ins->inst_offset);
			s390_jnz (code, -6);
			s390_lgr (code, ins->dreg, s390_r0);
		}
			break;	
		case OP_ATOMIC_ADD_I4: {
			if (mono_hwcap_s390x_has_ia) {
				s390_laa (code, s390_r0, ins->sreg2, ins->inst_basereg, ins->inst_offset);
				s390_ar  (code, s390_r0, ins->sreg2);
				s390_lgfr(code, ins->dreg, s390_r0);
			} else {
				s390_lgfr(code, s390_r1, ins->sreg2);
				s390_lgf (code, s390_r0, 0, ins->inst_basereg, ins->inst_offset);
				s390_agr (code, s390_r1, s390_r0);
				s390_cs  (code, s390_r0, s390_r1, ins->inst_basereg, ins->inst_offset);
				s390_jnz (code, -9);
				s390_lgfr(code, ins->dreg, s390_r1);
			}
		}
			break;	
		case OP_ATOMIC_EXCHANGE_I4: {
			s390_l   (code, s390_r0, 0, ins->inst_basereg, ins->inst_offset);
			s390_cs  (code, s390_r0, ins->sreg2, ins->inst_basereg, ins->inst_offset);
			s390_jnz (code, -4);
			s390_lgfr(code, ins->dreg, s390_r0);
		}
			break;	
		case OP_S390_BKCHAIN: {
			s390_lgr  (code, ins->dreg, ins->sreg1);
			if (s390_is_imm16 (cfg->stack_offset)) {
				s390_aghi (code, ins->dreg, cfg->stack_offset);
			} else if (s390_is_imm32 (cfg->stack_offset)) {
				s390_agfi (code, ins->dreg, cfg->stack_offset);
			} else {
				S390_SET  (code, s390_r13, cfg->stack_offset);
				s390_agr  (code, ins->dreg, s390_r13);
			}
		}
			break;	
		case OP_MEMORY_BARRIER:
			s390_mem (code);
			break;
		case OP_LIVERANGE_START: {
			if (cfg->verbose_level > 1)
				printf ("R%d START=0x%x\n", MONO_VARINFO (cfg, ins->inst_c0)->vreg, (int)(code - cfg->native_code));
			MONO_VARINFO (cfg, ins->inst_c0)->live_range_start = code - cfg->native_code;
			break;
		}
		case OP_LIVERANGE_END: {
			if (cfg->verbose_level > 1)
				printf ("R%d END=0x%x\n", MONO_VARINFO (cfg, ins->inst_c0)->vreg, (int)(code - cfg->native_code));
			MONO_VARINFO (cfg, ins->inst_c0)->live_range_end = code - cfg->native_code;
			break;
		}
		case OP_GC_SAFE_POINT: {
			short *br;

			s390_ltg (code, s390_r0, 0, ins->sreg1, 0);	
			s390_jz  (code, 0); CODEPTR(code, br);
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_JIT_ICALL_ID,
					     GUINT_TO_POINTER (MONO_JIT_ICALL_mono_threads_state_poll));
			S390_CALL_TEMPLATE (code, s390_r14);
			PTRSLOT (code, br);
			break;
		}
		case OP_GC_LIVENESS_DEF:
		case OP_GC_LIVENESS_USE:
		case OP_GC_PARAM_SLOT_LIVENESS_DEF:
			ins->backend.pc_offset = code - cfg->native_code;
			break;
		case OP_GC_SPILL_SLOT_LIVENESS_DEF:
			ins->backend.pc_offset = code - cfg->native_code;
			bb->spill_slot_defs = g_slist_prepend_mempool (cfg->mempool, bb->spill_slot_defs, ins);
			break;
#ifdef MONO_ARCH_SIMD_INTRINSICS
		case OP_ADDPS:
			s390x_addps (code, ins->sreg1, ins->sreg2);
			break;
		case OP_DIVPS:
			s390x_divps (code, ins->sreg1, ins->sreg2);
			break;
		case OP_MULPS:
			s390x_mulps (code, ins->sreg1, ins->sreg2);
			break;
		case OP_SUBPS:
			s390x_subps (code, ins->sreg1, ins->sreg2);
			break;
		case OP_MAXPS:
			s390x_maxps (code, ins->sreg1, ins->sreg2);
			break;
		case OP_MINPS:
			s390x_minps (code, ins->sreg1, ins->sreg2);
			break;
		case OP_COMPPS:
			g_assert (ins->inst_c0 >= 0 && ins->inst_c0 <= 7);
			s390x_cmpps_imm (code, ins->sreg1, ins->sreg2, ins->inst_c0);
			break;
		case OP_ANDPS:
			s390x_andps (code, ins->sreg1, ins->sreg2);
			break;
		case OP_ANDNPS:
			s390x_andnps (code, ins->sreg1, ins->sreg2);
			break;
		case OP_ORPS:
			s390x_orps (code, ins->sreg1, ins->sreg2);
			break;
		case OP_XORPS:
			s390x_xorps (code, ins->sreg1, ins->sreg2);
			break;
		case OP_SQRTPS:
			s390x_sqrtps (code, ins->dreg, ins->sreg1);
			break;
		case OP_RSQRTPS:
			s390x_rsqrtps (code, ins->dreg, ins->sreg1);
			break;
		case OP_RCPPS:
			s390x_rcpps (code, ins->dreg, ins->sreg1);
			break;
		case OP_ADDSUBPS:
			s390x_addsubps (code, ins->sreg1, ins->sreg2);
			break;
		case OP_HADDPS:
			s390x_haddps (code, ins->sreg1, ins->sreg2);
			break;
		case OP_HSUBPS:
			s390x_hsubps (code, ins->sreg1, ins->sreg2);
			break;
		case OP_DUPPS_HIGH:
			s390x_movshdup (code, ins->dreg, ins->sreg1);
			break;
		case OP_DUPPS_LOW:
			s390x_movsldup (code, ins->dreg, ins->sreg1);
			break;

		case OP_PSHUFLEW_HIGH:
			g_assert (ins->inst_c0 >= 0 && ins->inst_c0 <= 0xFF);
			s390x_pshufhw_imm (code, ins->dreg, ins->sreg1, ins->inst_c0);
			break;
		case OP_PSHUFLEW_LOW:
			g_assert (ins->inst_c0 >= 0 && ins->inst_c0 <= 0xFF);
			s390x_pshuflw_imm (code, ins->dreg, ins->sreg1, ins->inst_c0);
			break;
		case OP_PSHUFLED:
			g_assert (ins->inst_c0 >= 0 && ins->inst_c0 <= 0xFF);
			s390x_pshufd_imm (code, ins->dreg, ins->sreg1, ins->inst_c0);
			break;
		case OP_SHUFPS:
			g_assert (ins->inst_c0 >= 0 && ins->inst_c0 <= 0xFF);
			s390x_shufps_imm (code, ins->sreg1, ins->sreg2, ins->inst_c0);
			break;
		case OP_SHUFPD:
			g_assert (ins->inst_c0 >= 0 && ins->inst_c0 <= 0x3);
			s390x_shufpd_imm (code, ins->sreg1, ins->sreg2, ins->inst_c0);
			break;

		case OP_ADDPD:
			s390x_addpd (code, ins->sreg1, ins->sreg2);
			break;
		case OP_DIVPD:
			s390x_divpd (code, ins->sreg1, ins->sreg2);
			break;
		case OP_MULPD:
			s390x_mulpd (code, ins->sreg1, ins->sreg2);
			break;
		case OP_SUBPD:
			s390x_subpd (code, ins->sreg1, ins->sreg2);
			break;
		case OP_MAXPD:
			s390x_maxpd (code, ins->sreg1, ins->sreg2);
			break;
		case OP_MINPD:
			s390x_minpd (code, ins->sreg1, ins->sreg2);
			break;
		case OP_COMPPD:
			g_assert (ins->inst_c0 >= 0 && ins->inst_c0 <= 7);
			s390x_cmppd_imm (code, ins->sreg1, ins->sreg2, ins->inst_c0);
			break;
		case OP_ANDPD:
			s390x_andpd (code, ins->sreg1, ins->sreg2);
			break;
		case OP_ANDNPD:
			s390x_andnpd (code, ins->sreg1, ins->sreg2);
			break;
		case OP_ORPD:
			s390x_orpd (code, ins->sreg1, ins->sreg2);
			break;
		case OP_XORPD:
			s390x_xorpd (code, ins->sreg1, ins->sreg2);
			break;
		case OP_SQRTPD:
			s390x_sqrtpd (code, ins->dreg, ins->sreg1);
			break;
		case OP_ADDSUBPD:
			s390x_addsubpd (code, ins->sreg1, ins->sreg2);
			break;
		case OP_HADDPD:
			s390x_haddpd (code, ins->sreg1, ins->sreg2);
			break;
		case OP_HSUBPD:
			s390x_hsubpd (code, ins->sreg1, ins->sreg2);
			break;
		case OP_DUPPD:
			s390x_movddup (code, ins->dreg, ins->sreg1);
			break;

		case OP_EXTRACT_MASK:
			s390x_pmovmskb (code, ins->dreg, ins->sreg1);
			break;

		case OP_PAND:
			s390x_pand (code, ins->sreg1, ins->sreg2);
			break;
		case OP_POR:
			s390x_por (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PXOR:
			s390x_pxor (code, ins->sreg1, ins->sreg2);
			break;

		case OP_PADDB:
			s390x_paddb (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PADDW:
			s390x_paddw (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PADDD:
			s390x_paddd (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PADDQ:
			s390x_paddq (code, ins->sreg1, ins->sreg2);
			break;

		case OP_PSUBB:
			s390x_psubb (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PSUBW:
			s390x_psubw (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PSUBD:
			s390x_psubd (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PSUBQ:
			s390x_psubq (code, ins->sreg1, ins->sreg2);
			break;

		case OP_PMAXB_UN:
			s390x_pmaxub (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PMAXW_UN:
			s390x_pmaxuw (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PMAXD_UN:
			s390x_pmaxud (code, ins->sreg1, ins->sreg2);
			break;
		
		case OP_PMAXB:
			s390x_pmaxsb (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PMAXW:
			s390x_pmaxsw (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PMAXD:
			s390x_pmaxsd (code, ins->sreg1, ins->sreg2);
			break;

		case OP_PAVGB_UN:
			s390x_pavgb (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PAVGW_UN:
			s390x_pavgw (code, ins->sreg1, ins->sreg2);
			break;

		case OP_PMINB_UN:
			s390x_pminub (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PMINW_UN:
			s390x_pminuw (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PMIND_UN:
			s390x_pminud (code, ins->sreg1, ins->sreg2);
			break;

		case OP_PMINB:
			s390x_pminsb (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PMINW:
			s390x_pminsw (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PMIND:
			s390x_pminsd (code, ins->sreg1, ins->sreg2);
			break;

		case OP_PCMPEQB:
			s390x_pcmpeqb (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PCMPEQW:
			s390x_pcmpeqw (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PCMPEQD:
			s390x_pcmpeqd (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PCMPEQQ:
			s390x_pcmpeqq (code, ins->sreg1, ins->sreg2);
			break;

		case OP_PCMPGTB:
			s390x_pcmpgtb (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PCMPGTW:
			s390x_pcmpgtw (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PCMPGTD:
			s390x_pcmpgtd (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PCMPGTQ:
			s390x_pcmpgtq (code, ins->sreg1, ins->sreg2);
			break;

		case OP_PSUM_ABS_DIFF:
			s390x_psadbw (code, ins->sreg1, ins->sreg2);
			break;

		case OP_UNPACK_LOWB:
			s390x_punpcklbw (code, ins->sreg1, ins->sreg2);
			break;
		case OP_UNPACK_LOWW:
			s390x_punpcklwd (code, ins->sreg1, ins->sreg2);
			break;
		case OP_UNPACK_LOWD:
			s390x_punpckldq (code, ins->sreg1, ins->sreg2);
			break;
		case OP_UNPACK_LOWQ:
			s390x_punpcklqdq (code, ins->sreg1, ins->sreg2);
			break;
		case OP_UNPACK_LOWPS:
			s390x_unpcklps (code, ins->sreg1, ins->sreg2);
			break;
		case OP_UNPACK_LOWPD:
			s390x_unpcklpd (code, ins->sreg1, ins->sreg2);
			break;

		case OP_UNPACK_HIGHB:
			s390x_punpckhbw (code, ins->sreg1, ins->sreg2);
			break;
		case OP_UNPACK_HIGHW:
			s390x_punpckhwd (code, ins->sreg1, ins->sreg2);
			break;
		case OP_UNPACK_HIGHD:
			s390x_punpckhdq (code, ins->sreg1, ins->sreg2);
			break;
		case OP_UNPACK_HIGHQ:
			s390x_punpckhqdq (code, ins->sreg1, ins->sreg2);
			break;
		case OP_UNPACK_HIGHPS:
			s390x_unpckhps (code, ins->sreg1, ins->sreg2);
			break;
		case OP_UNPACK_HIGHPD:
			s390x_unpckhpd (code, ins->sreg1, ins->sreg2);
			break;

		case OP_PACKW:
			s390x_packsswb (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PACKD:
			s390x_packssdw (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PACKW_UN:
			s390x_packuswb (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PACKD_UN:
			s390x_packusdw (code, ins->sreg1, ins->sreg2);
			break;

		case OP_PADDB_SAT_UN:
			s390x_paddusb (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PSUBB_SAT_UN:
			s390x_psubusb (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PADDW_SAT_UN:
			s390x_paddusw (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PSUBW_SAT_UN:
			s390x_psubusw (code, ins->sreg1, ins->sreg2);
			break;

		case OP_PADDB_SAT:
			s390x_paddsb (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PSUBB_SAT:
			s390x_psubsb (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PADDW_SAT:
			s390x_paddsw (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PSUBW_SAT:
			s390x_psubsw (code, ins->sreg1, ins->sreg2);
			break;
			
		case OP_PMULW:
			s390x_pmullw (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PMULD:
			s390x_pmulld (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PMULQ:
			s390x_pmuludq (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PMULW_HIGH_UN:
			s390x_pmulhuw (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PMULW_HIGH:
			s390x_pmulhw (code, ins->sreg1, ins->sreg2);
			break;

		case OP_PSHRW:
			s390x_psrlw_reg_imm (code, ins->dreg, ins->inst_imm);
			break;
		case OP_PSHRW_REG:
			s390x_psrlw (code, ins->dreg, ins->sreg2);
			break;

		case OP_PSARW:
			s390x_psraw_reg_imm (code, ins->dreg, ins->inst_imm);
			break;
		case OP_PSARW_REG:
			s390x_psraw (code, ins->dreg, ins->sreg2);
			break;

		case OP_PSHLW:
			s390x_psllw_reg_imm (code, ins->dreg, ins->inst_imm);
			break;
		case OP_PSHLW_REG:
			s390x_psllw (code, ins->dreg, ins->sreg2);
			break;

		case OP_PSHRD:
			s390x_psrld_reg_imm (code, ins->dreg, ins->inst_imm);
			break;
		case OP_PSHRD_REG:
			s390x_psrld (code, ins->dreg, ins->sreg2);
			break;

		case OP_PSARD:
			s390x_psrad_reg_imm (code, ins->dreg, ins->inst_imm);
			break;
		case OP_PSARD_REG:
			s390x_psrad (code, ins->dreg, ins->sreg2);
			break;

		case OP_PSHLD:
			s390x_pslld_reg_imm (code, ins->dreg, ins->inst_imm);
			break;
		case OP_PSHLD_REG:
			s390x_pslld (code, ins->dreg, ins->sreg2);
			break;

		case OP_PSHRQ:
			s390x_psrlq_reg_imm (code, ins->dreg, ins->inst_imm);
			break;
		case OP_PSHRQ_REG:
			s390x_psrlq (code, ins->dreg, ins->sreg2);
			break;
		
		/*TODO: This is appart of the sse spec but not added
		case OP_PSARQ:
			s390x_psraq_reg_imm (code, ins->dreg, ins->inst_imm);
			break;
		case OP_PSARQ_REG:
			s390x_psraq (code, ins->dreg, ins->sreg2);
			break;	
		*/
	
		case OP_PSHLQ:
			s390x_psllq_reg_imm (code, ins->dreg, ins->inst_imm);
			break;
		case OP_PSHLQ_REG:
			s390x_psllq (code, ins->dreg, ins->sreg2);
			break;	
		case OP_CVTDQ2PD:
			s390x_cvtdq2pd (code, ins->dreg, ins->sreg1);
			break;
		case OP_CVTDQ2PS:
			s390x_cvtdq2ps (code, ins->dreg, ins->sreg1);
			break;
		case OP_CVTPD2DQ:
			s390x_cvtpd2dq (code, ins->dreg, ins->sreg1);
			break;
		case OP_CVTPD2PS:
			s390x_cvtpd2ps (code, ins->dreg, ins->sreg1);
			break;
		case OP_CVTPS2DQ:
			s390x_cvtps2dq (code, ins->dreg, ins->sreg1);
			break;
		case OP_CVTPS2PD:
			s390x_cvtps2pd (code, ins->dreg, ins->sreg1);
			break;
		case OP_CVTTPD2DQ:
			s390x_cvttpd2dq (code, ins->dreg, ins->sreg1);
			break;
		case OP_CVTTPS2DQ:
			s390x_cvttps2dq (code, ins->dreg, ins->sreg1);
			break;

		case OP_ICONV_TO_X:
			amd64_movd_xreg_reg_size (code, ins->dreg, ins->sreg1, 4);
			break;
		case OP_EXTRACT_I4:
			amd64_movd_reg_xreg_size (code, ins->dreg, ins->sreg1, 4);
			break;
		case OP_EXTRACT_I8:
			if (ins->inst_c0) {
				amd64_movhlps (code, MONO_ARCH_FP_SCRATCH_REG, ins->sreg1);
				amd64_movd_reg_xreg_size (code, ins->dreg, MONO_ARCH_FP_SCRATCH_REG, 8);
			} else {
				amd64_movd_reg_xreg_size (code, ins->dreg, ins->sreg1, 8);
			}
			break;
		case OP_EXTRACT_I1:
		case OP_EXTRACT_U1:
			amd64_movd_reg_xreg_size (code, ins->dreg, ins->sreg1, 4);
			if (ins->inst_c0)
				amd64_shift_reg_imm (code, X86_SHR, ins->dreg, ins->inst_c0 * 8);
			amd64_widen_reg (code, ins->dreg, ins->dreg, ins->opcode == OP_EXTRACT_I1, FALSE);
			break;
		case OP_EXTRACT_I2:
		case OP_EXTRACT_U2:
			/*amd64_movd_reg_xreg_size (code, ins->dreg, ins->sreg1, 4);
			if (ins->inst_c0)
				amd64_shift_reg_imm_size (code, X86_SHR, ins->dreg, 16, 4);*/
			s390x_pextrw_imm (code, ins->dreg, ins->sreg1, ins->inst_c0);
			amd64_widen_reg_size (code, ins->dreg, ins->dreg, ins->opcode == OP_EXTRACT_I2, TRUE, 4);
			break;
		case OP_EXTRACT_R8:
			if (ins->inst_c0)
				amd64_movhlps (code, ins->dreg, ins->sreg1);
			else
				s390x_movsd (code, ins->dreg, ins->sreg1);
			break;
		case OP_INSERT_I2:
			s390x_pinsrw_imm (code, ins->sreg1, ins->sreg2, ins->inst_c0);
			break;
		case OP_EXTRACTX_U2:
			s390x_pextrw_imm (code, ins->dreg, ins->sreg1, ins->inst_c0);
			break;
		case OP_INSERTX_U1_SLOW:
			/*sreg1 is the extracted ireg (scratch)
			/sreg2 is the to be inserted ireg (scratch)
			/dreg is the xreg to receive the value*/

			/*clear the bits from the extracted word*/
			amd64_alu_reg_imm (code, X86_AND, ins->sreg1, ins->inst_c0 & 1 ? 0x00FF : 0xFF00);
			/*shift the value to insert if needed*/
			if (ins->inst_c0 & 1)
				amd64_shift_reg_imm_size (code, X86_SHL, ins->sreg2, 8, 4);
			/*join them together*/
			amd64_alu (code, X86_OR, ins->sreg1, ins->sreg2);
			s390x_pinsrw_imm (code, ins->dreg, ins->sreg1, ins->inst_c0 / 2);
			break;
		case OP_INSERTX_I4_SLOW:
			s390x_pinsrw_imm (code, ins->dreg, ins->sreg2, ins->inst_c0 * 2);
			amd64_shift_reg_imm (code, X86_SHR, ins->sreg2, 16);
			s390x_pinsrw_imm (code, ins->dreg, ins->sreg2, ins->inst_c0 * 2 + 1);
			break;
		case OP_INSERTX_I8_SLOW:
			amd64_movd_xreg_reg_size(code, MONO_ARCH_FP_SCRATCH_REG, ins->sreg2, 8);
			if (ins->inst_c0)
				amd64_movlhps (code, ins->dreg, MONO_ARCH_FP_SCRATCH_REG);
			else
				s390x_movsd (code, ins->dreg, MONO_ARCH_FP_SCRATCH_REG);
			break;

		case OP_INSERTX_R4_SLOW:
			switch (ins->inst_c0) {
			case 0:
				if (cfg->r4fp)
					s390x_movss (code, ins->dreg, ins->sreg2);
				else
					s390x_cvtsd2ss (code, ins->dreg, ins->sreg2);
				break;
			case 1:
				s390x_pshufd_imm (code, ins->dreg, ins->dreg, mono_simd_shuffle_mask(1, 0, 2, 3));
				if (cfg->r4fp)
					s390x_movss (code, ins->dreg, ins->sreg2);
				else
					s390x_cvtsd2ss (code, ins->dreg, ins->sreg2);
				s390x_pshufd_imm (code, ins->dreg, ins->dreg, mono_simd_shuffle_mask(1, 0, 2, 3));
				break;
			case 2:
				s390x_pshufd_imm (code, ins->dreg, ins->dreg, mono_simd_shuffle_mask(2, 1, 0, 3));
				if (cfg->r4fp)
					s390x_movss (code, ins->dreg, ins->sreg2);
				else
					s390x_cvtsd2ss (code, ins->dreg, ins->sreg2);
				s390x_pshufd_imm (code, ins->dreg, ins->dreg, mono_simd_shuffle_mask(2, 1, 0, 3));
				break;
			case 3:
				s390x_pshufd_imm (code, ins->dreg, ins->dreg, mono_simd_shuffle_mask(3, 1, 2, 0));
				if (cfg->r4fp)
					s390x_movss (code, ins->dreg, ins->sreg2);
				else
					s390x_cvtsd2ss (code, ins->dreg, ins->sreg2);
				s390x_pshufd_imm (code, ins->dreg, ins->dreg, mono_simd_shuffle_mask(3, 1, 2, 0));
				break;
			}
			break;
		case OP_INSERTX_R8_SLOW:
			if (ins->inst_c0)
				amd64_movlhps (code, ins->dreg, ins->sreg2);
			else
				s390x_movsd (code, ins->dreg, ins->sreg2);
			break;
		case OP_STOREX_MEMBASE_REG:
		case OP_STOREX_MEMBASE:
			s390x_movups_membase_reg (code, ins->dreg, ins->inst_offset, ins->sreg1);
			break;
		case OP_LOADX_MEMBASE:
			s390x_movups_reg_membase (code, ins->dreg, ins->sreg1, ins->inst_offset);
			break;
		case OP_LOADX_ALIGNED_MEMBASE:
			s390x_movaps_reg_membase (code, ins->dreg, ins->sreg1, ins->inst_offset);
			break;
		case OP_STOREX_ALIGNED_MEMBASE_REG:
			s390x_movaps_membase_reg (code, ins->dreg, ins->inst_offset, ins->sreg1);
			break;
		case OP_STOREX_NTA_MEMBASE_REG:
			s390x_movntps_reg_membase (code, ins->dreg, ins->sreg1, ins->inst_offset);
			break;
		case OP_PREFETCH_MEMBASE:
			s390x_prefetch_reg_membase (code, ins->backend.arg_info, ins->sreg1, ins->inst_offset);
			break;

		case OP_XMOVE:
			/*FIXME the peephole pass should have killed this*/
			if (ins->dreg != ins->sreg1)
				s390x_movaps (code, ins->dreg, ins->sreg1);
			break;		
		case OP_XZERO:
			s390x_pxor (code, ins->dreg, ins->dreg);
			break;
		case OP_ICONV_TO_R4_RAW:
			amd64_movd_xreg_reg_size (code, ins->dreg, ins->sreg1, 4);
			break;

		case OP_FCONV_TO_R8_X:
			s390x_movsd (code, ins->dreg, ins->sreg1);
			break;

		case OP_XCONV_R8_TO_I4:
			s390x_cvttsd2si_reg_xreg_size (code, ins->dreg, ins->sreg1, 4);
			switch (ins->backend.source_opcode) {
			case OP_FCONV_TO_I1:
				amd64_widen_reg (code, ins->dreg, ins->dreg, TRUE, FALSE);
				break;
			case OP_FCONV_TO_U1:
				amd64_widen_reg (code, ins->dreg, ins->dreg, FALSE, FALSE);
				break;
			case OP_FCONV_TO_I2:
				amd64_widen_reg (code, ins->dreg, ins->dreg, TRUE, TRUE);
				break;
			case OP_FCONV_TO_U2:
				amd64_widen_reg (code, ins->dreg, ins->dreg, FALSE, TRUE);
				break;
			}			
			break;

		case OP_EXPAND_I2:
			s390x_pinsrw_imm (code, ins->dreg, ins->sreg1, 0);
			s390x_pinsrw_imm (code, ins->dreg, ins->sreg1, 1);
			s390x_pshufd_imm (code, ins->dreg, ins->dreg, 0);
			break;
		case OP_EXPAND_I4:
			amd64_movd_xreg_reg_size (code, ins->dreg, ins->sreg1, 4);
			s390x_pshufd_imm (code, ins->dreg, ins->dreg, 0);
			break;
		case OP_EXPAND_I8:
			amd64_movd_xreg_reg_size (code, ins->dreg, ins->sreg1, 8);
			s390x_pshufd_imm (code, ins->dreg, ins->dreg, 0x44);
			break;
		case OP_EXPAND_R4:
			if (cfg->r4fp) {
				s390x_movsd (code, ins->dreg, ins->sreg1);
			} else {
				s390x_movsd (code, ins->dreg, ins->sreg1);
				s390x_cvtsd2ss (code, ins->dreg, ins->dreg);
			}
			s390x_pshufd_imm (code, ins->dreg, ins->dreg, 0);
			break;
		case OP_EXPAND_R8:
			s390x_movsd (code, ins->dreg, ins->sreg1);
			s390x_pshufd_imm (code, ins->dreg, ins->dreg, 0x44);
			break;
#endif
		default:
			g_warning ("unknown opcode %s in %s()\n", mono_inst_name (ins->opcode), __FUNCTION__);
			g_assert_not_reached ();
		}

		if ((cfg->opt & MONO_OPT_BRANCH) && ((code - cfg->native_code - offset) > max_len)) {
			g_warning ("wrong maximal instruction length of instruction %s (expected %d, got %ld)",
				   mono_inst_name (ins->opcode), max_len, code - cfg->native_code - offset);
			g_assert_not_reached ();
		}
	}

	set_code_cursor (cfg, code);
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecture-specific registration of lowlevel calls
 * 
 * Register routines to register optimized lowlevel operations
 */

void
mono_arch_register_lowlevel_calls (void)
{
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecture-specific patching of instructions and data
 * 
 * @param[in] @cfg - Compile control block
 * @param[in] @method - Current method
 * @param[in] @code - Current innstruction pointer
 * @param[in] @ji - Jump information 
 * @param[in] @run_cctors - Whether class constructors need to be initialized 
 * @param[in] @error - Error control block 
 *
 * Process the patch data created during the instruction build process. 
 * This resolves jumps, calls, variables etc.
 */

void
mono_arch_patch_code_new (MonoCompile *cfg,
						  guint8 *code, MonoJumpInfo *ji, gpointer target)
{
	unsigned char *ip = ji->ip.i + code;
	gint64 displace;

	switch (ji->type) {
	case MONO_PATCH_INFO_IP:
	case MONO_PATCH_INFO_LDSTR:
	case MONO_PATCH_INFO_TYPE_FROM_HANDLE: 
	case MONO_PATCH_INFO_LDTOKEN: 
	case MONO_PATCH_INFO_EXC:
		s390_patch_addr (ip, (guint64) target);
		break;
	case MONO_PATCH_INFO_SPECIFIC_TRAMPOLINE_LAZY_FETCH_ADDR:
	case MONO_PATCH_INFO_METHOD:
	case MONO_PATCH_INFO_JIT_ICALL_ID:
	case MONO_PATCH_INFO_JIT_ICALL_ADDR:
	case MONO_PATCH_INFO_RGCTX_FETCH:
	case MONO_PATCH_INFO_ABS: {
		S390_EMIT_CALL (ip, target);
		break;
	}
	case MONO_PATCH_INFO_SWITCH: 
		/*----------------------------------*/
		/* ip points at the basr r13,0/j +4 */
		/* instruction the vtable value     */
		/* follows this (i.e. ip+6)	    */
		/*----------------------------------*/
		S390_EMIT_LOAD (ip, target);
		break;
	case MONO_PATCH_INFO_METHODCONST:
	case MONO_PATCH_INFO_CLASS:
	case MONO_PATCH_INFO_IMAGE:
	case MONO_PATCH_INFO_FIELD:
	case MONO_PATCH_INFO_IID:
	case MONO_PATCH_INFO_EXC_NAME:
		target = S390_RELATIVE(target, ip);
		s390_patch_rel (ip, (guint64) target);
		break;
	case MONO_PATCH_INFO_R4:
	case MONO_PATCH_INFO_R8:
		g_assert_not_reached ();
		break;
	case MONO_PATCH_INFO_METHOD_JUMP:
		displace = (gint64) S390_RELATIVE(target, ip);
		if ((displace >= INT_MIN) && (displace <= INT_MAX)) 
			s390_jg (ip, (gint32) displace);
		else {
			S390_SET (ip, s390_r1, target);
			s390_br  (ip, s390_r1);
		}
		break;
	case MONO_PATCH_INFO_NONE:
		break;
	default:
		target = S390_RELATIVE(target, ip);
		ip += 2;
		s390_patch_rel (ip, (guint64) target);
	}
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecture-specific prolog generation
 * 
 * @param[in] @cfg - Compile control block
 * @returns Location of code code generated
 *
 * Create the instruction sequence for entry into a method:
 * - Determine stack size
 * - Save preserved registers
 * - Unload parameters
 * - Determine if LMF needs saving and generate that sequence
 */

guint8 *
mono_arch_emit_prolog (MonoCompile *cfg)
{
	MonoMethod *method = cfg->method;
	MonoBasicBlock *bb;
	MonoMethodSignature *sig;
	MonoInst *inst;
	long alloc_size, pos, max_offset, i, cfa_offset = 0;
	guint8 *code;
	guint32 size;
	CallInfo *cinfo;
	int argsClobbered = 0,
	    lmfOffset,
	    fpOffset = 0;

	cfg->code_size   = 512;

	if (method->save_lmf)
		cfg->code_size += 200;

	cfg->native_code = code = (guint8 *) g_malloc (cfg->code_size);

	/**
	 * Create unwind information
	 */ 
	mono_emit_unwind_op_def_cfa (cfg, code, STK_BASE, S390_CFA_OFFSET);
	s390_stmg (code, s390_r6, s390_r15, STK_BASE, S390_REG_SAVE_OFFSET);
	emit_unwind_regs(cfg, code, s390_r6, s390_r15, S390_REG_SAVE_OFFSET - S390_CFA_OFFSET);
	if (cfg->arch.bkchain_reg != -1)
		s390_lgr (code, cfg->arch.bkchain_reg, STK_BASE);

	/*
	 * If there are local allocations the R11 becomes the frame register
	 */ 
	if (cfg->flags & MONO_CFG_HAS_ALLOCA) {
		cfg->used_int_regs |= 1 << s390_r11;
	}

	/*
	 * Check if FP registers need preserving
	 */ 
	if ((cfg->arch.used_fp_regs & S390_FP_SAVE_MASK) != 0) {
		for (int i = s390_f8; i <= s390_f15; i++) {
			if (cfg->arch.used_fp_regs & (1 << i)) 
				fpOffset += sizeof(double);
		}
		fpOffset = S390_ALIGN(fpOffset, sizeof(double));
	}
	cfg->arch.fpSize = fpOffset;

	/*
	 * Calculate stack requirements
	 */ 
	alloc_size = cfg->stack_offset + fpOffset;

	cfg->stack_usage = cfa_offset = alloc_size;
	s390_lgr  (code, s390_r11, STK_BASE);
	if (s390_is_imm16 (alloc_size)) {
		s390_aghi (code, STK_BASE, -alloc_size);
	} else if (s390_is_imm32 (alloc_size)) { 
		s390_agfi (code, STK_BASE, -alloc_size);
	} else {
		int stackSize = alloc_size;
		while (stackSize > INT_MAX) {
			s390_agfi (code, STK_BASE, -INT_MAX);
			stackSize -= INT_MAX;
		}
		s390_agfi (code, STK_BASE, -stackSize);
	}
	mono_emit_unwind_op_def_cfa_offset (cfg, code, alloc_size + S390_CFA_OFFSET);
	s390_stg  (code, s390_r11, 0, STK_BASE, 0);

	if (fpOffset > 0) {
		int stkOffset = 0;

		s390_lgr (code, s390_r1, s390_r11);
		s390_aghi (code, s390_r1, -fpOffset);
		for (int i = s390_f8; i <= s390_f15; i++) {
			if (cfg->arch.used_fp_regs & (1 << i)) {
				s390_std (code, i, 0, s390_r1, stkOffset);
				emit_unwind_regs(cfg, code, 16+i, 16+i, stkOffset+fpOffset - S390_CFA_OFFSET); 
				stkOffset += sizeof(double);
			}
		}
	}

	if (cfg->frame_reg != STK_BASE) {
		s390_lgr (code, s390_r11, STK_BASE);
		mono_emit_unwind_op_def_cfa_reg (cfg, code, cfg->frame_reg);
	}

	/* store runtime generic context */
	if (cfg->rgctx_var) {
		g_assert (cfg->rgctx_var->opcode == OP_REGOFFSET);

		s390_stg  (code, MONO_ARCH_RGCTX_REG, 0, 
			       cfg->rgctx_var->inst_basereg, 
    			   cfg->rgctx_var->inst_offset);
	}

#if 0
printf("ns: %s k: %s m: %s\n",method->klass->name_space,method->klass->name,method->name);fflush(stdout);
// Tests:set_ip
if ((strcmp(method->klass->name_space,"") == 0) && 
    (strcmp(method->klass->name,"Tests") == 0) &&
    (strcmp(method->name, "set_ip") == 0)) {
    // (strcmp("CancellationToken,TaskCreationOptions,TaskContinuationOptions,TaskScheduler",mono_signature_get_desc(method->signature, FALSE)) != 0))  {
 printf("SIGNATURE: %s\n",mono_signature_get_desc(method->signature, FALSE)); fflush(stdout);
 s390_j (code, 0);
}
#endif

	/* compute max_offset in order to use short forward jumps
	 * we always do it on s390 because the immediate displacement
	 * for jumps is too small 
	 */
	max_offset = 0;
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoInst *ins;
		bb->max_offset = max_offset;

		MONO_BB_FOR_EACH_INS (bb, ins)
			max_offset += ins_get_size (ins->opcode);
	}

	/* load arguments allocated to register from the stack */
	sig = mono_method_signature_internal (method);
	pos = 0;

	cinfo = get_call_info (cfg->mempool, sig);

	if (cinfo->struct_ret) {
		ArgInfo *ainfo     = &cinfo->ret;
		inst               = cfg->vret_addr;
		inst->backend.size = ainfo->vtsize;
		s390_stg (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
	}

	/**
	 * Process the arguments passed to the method
	 */

	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		ArgInfo *ainfo = cinfo->args + i;
		inst = cfg->args [pos];
		
		if (inst->opcode == OP_VTARG_ADDR)
			inst = inst->inst_left;

		if (inst->opcode == OP_REGVAR) {
			if (ainfo->regtype == RegTypeGeneral)
				s390_lgr (code, inst->dreg, ainfo->reg);
			else if (ainfo->regtype == RegTypeFP) {
				if (inst->dreg != ainfo->reg) {
					s390_ldr   (code, inst->dreg, ainfo->reg);
				}
			} else if (ainfo->regtype == RegTypeFPR4) {
					if (!cfg->r4fp) 
						s390_ledbr (code, inst->dreg, ainfo->reg);
			} else if (ainfo->regtype == RegTypeBase) {
				s390_lgr  (code, s390_r13, STK_BASE);
				s390_aghi (code, s390_r13, alloc_size);
				s390_lg   (code, inst->dreg, 0, s390_r13, ainfo->offset);
			} else
				g_assert_not_reached ();

			if (cfg->verbose_level > 2)
				g_print ("Argument %d assigned to register %s\n", 
					 pos, mono_arch_regname (inst->dreg));
		} else {
			if (ainfo->regtype == RegTypeGeneral) {
				if (!((ainfo->reg >= 2) && (ainfo->reg <= 6)))
					g_assert_not_reached();
				switch (ainfo->size) {
				case 1:
					s390_stc (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
					break;
				case 2:
					s390_sth (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
					break;
				case 4: 
					s390_st (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
					break;
				case 8:
					s390_stg (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
					break;
				}
			} else if (ainfo->regtype == RegTypeBase) {
			} else if (ainfo->regtype == RegTypeFP) {
				s390_std (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
			} else if (ainfo->regtype == RegTypeFPR4) {
				s390_ste (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
			} else if (ainfo->regtype == RegTypeStructByVal) {
				int doffset = inst->inst_offset;

				size = (method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE  
					? mono_class_native_size(mono_class_from_mono_type_internal (inst->inst_vtype), NULL)
					: ainfo->size);

				switch (size) {
					case 1:
						if (ainfo->reg != STK_BASE)
							s390_stc (code, ainfo->reg, 0, inst->inst_basereg, doffset);
						break;
					case 2:
						if (ainfo->reg != STK_BASE)
							s390_sth (code, ainfo->reg, 0, inst->inst_basereg, doffset);
						break;
					case 4:
						if (ainfo->reg != STK_BASE)
							s390_st (code, ainfo->reg, 0, inst->inst_basereg, doffset);
						break;
					case 8:
						if (ainfo->reg != STK_BASE)
							s390_stg (code, ainfo->reg, 0, inst->inst_basereg, doffset);
						break;
					default:
						if (ainfo->reg != STK_BASE)
							s390_stg (code, ainfo->reg, 0, STK_BASE, doffset);
				}
			} else if (ainfo->regtype == RegTypeStructByAddr) {
				s390_stg (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
			} else
				g_assert_not_reached ();
		}
		pos++;
	}

	if (method->save_lmf) {
		/**
		 * Build the MonoLMF structure on the stack - see mini-s390x.h  
		 */
		lmfOffset = alloc_size - sizeof(MonoLMF);	
											
		s390_lgr   (code, s390_r13, cfg->frame_reg);		
		s390_aghi  (code, s390_r13, lmfOffset);					
											
		/*
		 * Preserve the parameter registers while we fix up the lmf	
		 */
		s390_stmg  (code, s390_r2, s390_r6, s390_r13,
			    MONO_STRUCT_OFFSET(MonoLMF, pregs));

		for (i = 0; i < 5; i++)
			mini_gc_set_slot_type_from_fp (cfg, lmfOffset + MONO_STRUCT_OFFSET (MonoLMF, pregs) + i * sizeof(gulong), SLOT_NOREF);

		/*
		 * On return from this call r2 have the address of the &lmf
		 */
		mono_add_patch_info (cfg, code - cfg->native_code, 
				MONO_PATCH_INFO_JIT_ICALL_ID,
				GUINT_TO_POINTER (MONO_JIT_ICALL_mono_tls_get_lmf_addr_extern));
		S390_CALL_TEMPLATE(code, s390_r1);

		/*
		 * Set lmf.lmf_addr = jit_tls->lmf
		 */
		s390_stg   (code, s390_r2, 0, s390_r13, 				
			    MONO_STRUCT_OFFSET(MonoLMF, lmf_addr));			
		mini_gc_set_slot_type_from_fp (cfg, lmfOffset + MONO_STRUCT_OFFSET (MonoLMF, lmf_addr), SLOT_NOREF);
											
		/*
		 * Get current lmf
		 */
		s390_lg    (code, s390_r0, 0, s390_r2, 0);				
											
		/*
		 * Set our lmf as the current lmf
		 */
		s390_stg   (code, s390_r13, 0, s390_r2, 0);				
											
		/*
		 * Have our lmf.previous_lmf point to the last lmf
		 */
		s390_stg   (code, s390_r0, 0, s390_r13, 				
			    MONO_STRUCT_OFFSET(MonoLMF, previous_lmf));			
		mini_gc_set_slot_type_from_fp (cfg, lmfOffset + MONO_STRUCT_OFFSET (MonoLMF, previous_lmf), SLOT_NOREF);
											
		/*
		 * Save method info
		 */
		S390_SET   (code, s390_r1, method);
		s390_stg   (code, s390_r1, 0, s390_r13, 				
			    MONO_STRUCT_OFFSET(MonoLMF, method));				
		mini_gc_set_slot_type_from_fp (cfg, lmfOffset + MONO_STRUCT_OFFSET (MonoLMF, method), SLOT_NOREF);
										
		/*
		 * Save the current IP
		 */
		s390_stg   (code, STK_BASE, 0, s390_r13, MONO_STRUCT_OFFSET(MonoLMF, ebp));
		s390_basr  (code, s390_r1, 0);
		s390_stg   (code, s390_r1, 0, s390_r13, MONO_STRUCT_OFFSET(MonoLMF, eip));	
		mini_gc_set_slot_type_from_fp (cfg, lmfOffset + MONO_STRUCT_OFFSET (MonoLMF, ebp), SLOT_NOREF);
		mini_gc_set_slot_type_from_fp (cfg, lmfOffset + MONO_STRUCT_OFFSET (MonoLMF, eip), SLOT_NOREF);
											
		/*
		 * Save general and floating point registers
		 */
		s390_stmg  (code, s390_r2, s390_r12, s390_r13, 				
			    MONO_STRUCT_OFFSET(MonoLMF, gregs) + 2 * sizeof(gulong));	
		for (i = 0; i < 11; i++)
			mini_gc_set_slot_type_from_fp (cfg, lmfOffset + MONO_STRUCT_OFFSET (MonoLMF, gregs) + i * sizeof(gulong), SLOT_NOREF);

		fpOffset = lmfOffset + MONO_STRUCT_OFFSET (MonoLMF, fregs);
		for (i = 0; i < 16; i++) {						
			s390_std  (code, i, 0, s390_r13, 				
				   MONO_STRUCT_OFFSET(MonoLMF, fregs) + i * sizeof(gulong));
			mini_gc_set_slot_type_from_fp (cfg, fpOffset, SLOT_NOREF);
			fpOffset += sizeof(double);
		}									

		/*
		 * Restore the parameter registers now that we've set up the lmf
		 */
		s390_lmg   (code, s390_r2, s390_r6, s390_r13, 				
			    MONO_STRUCT_OFFSET(MonoLMF, pregs));	
	}

	if (cfg->method->save_lmf)
		argsClobbered = TRUE;

	/*
	 * Optimize the common case of the first bblock making a call with the same
	 * arguments as the method. This works because the arguments are still in their
	 * original argument registers.
	 */
	if (!argsClobbered) {
		MonoBasicBlock *first_bb = cfg->bb_entry;
		MonoInst *next;
		int filter = FILTER_IL_SEQ_POINT;

		next = mono_bb_first_inst (first_bb, filter);
		if (!next && first_bb->next_bb) {
			first_bb = first_bb->next_bb;
			next = mono_bb_first_inst (first_bb, filter);
		}

		if (first_bb->in_count > 1)
			next = NULL;

		for (i = 0; next && i < sig->param_count + sig->hasthis; ++i) {
			ArgInfo *ainfo = cinfo->args + i;
			gboolean match = FALSE;

			inst = cfg->args [i];
			if (inst->opcode != OP_REGVAR) {
				switch (ainfo->regtype) {
				case RegTypeGeneral: {
					if (((next->opcode == OP_LOAD_MEMBASE) || 
					     (next->opcode == OP_LOADI4_MEMBASE)) && 
					     next->inst_basereg == inst->inst_basereg && 
					     next->inst_offset == inst->inst_offset) {
						if (next->dreg == ainfo->reg) {
							NULLIFY_INS (next);
							match = TRUE;
						} else {
							next->opcode = OP_MOVE;
							next->sreg1 = ainfo->reg;
							/* Only continue if the instruction doesn't change argument regs */
							if (next->dreg == ainfo->reg)
								match = TRUE;
						}
					}
					break;
				}
				default:
					break;
				}
			} else {
				/* Argument allocated to (non-volatile) register */
				switch (ainfo->regtype) {
				case RegTypeGeneral:
					if (next->opcode == OP_MOVE && 
					    next->sreg1 == inst->dreg && 
					    next->dreg == ainfo->reg) {
						NULLIFY_INS (next);
						match = TRUE;
					}
					break;
				default:
					break;
				}
			}

			if (match) {
				next = mono_inst_next (next, filter);
				if (!next)
					break;
			}
		}
	}

	if (cfg->gen_sdb_seq_points) {
		MonoInst *seq;

		/* Initialize ss_tramp_var */
		seq = cfg->arch.ss_tramp_var;
		g_assert (seq->opcode == OP_REGOFFSET);

		S390_SET (code, s390_r1, (guint64) &ss_trampoline);
		s390_stg (code, s390_r1, 0, seq->inst_basereg, seq->inst_offset);

		/* Initialize bp_tramp_var */
		seq = cfg->arch.bp_tramp_var;
		g_assert (seq->opcode == OP_REGOFFSET);

		S390_SET (code, s390_r1, (guint64) &bp_trampoline);
		s390_stg (code, s390_r1, 0, seq->inst_basereg, seq->inst_offset);
	}

	set_code_cursor (cfg, code);

	return code;
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecutre-specific epilog generation
 * 
 * @param[in] @cfg - Compile control block
 *
 * Create the instruction sequence for exit from a method
 */

void
mono_arch_emit_epilog (MonoCompile *cfg)
{
	MonoMethod *method = cfg->method;
	guint8 *code;
	int max_epilog_size = 96, i;
	int fpOffset = 0;
	
	if (cfg->method->save_lmf)
		max_epilog_size += 128;
	
	code = realloc_code (cfg, max_epilog_size);

	cfg->has_unwind_info_for_epilog = TRUE;

	/* Mark the start of the epilog */
	mono_emit_unwind_op_mark_loc (cfg, code, 0);

	/* Save the uwind state which is needed by the out-of-line code */
	mono_emit_unwind_op_remember_state (cfg, code);

	if (method->save_lmf) 
		restoreLMF(code, cfg->frame_reg, cfg->stack_usage);

	code = backUpStackPtr(cfg, code);
	mono_emit_unwind_op_def_cfa (cfg, code, STK_BASE, S390_CFA_OFFSET);
	mono_emit_unwind_op_same_value (cfg, code, STK_BASE);

	if (cfg->arch.fpSize != 0) {
		fpOffset = -cfg->arch.fpSize;
		for (int i=8; i<16; i++) {
			if (cfg->arch.used_fp_regs & (1 << i)) {
				s390_ldy (code, i, 0, STK_BASE, fpOffset);
				mono_emit_unwind_op_same_value (cfg, code, 16+i);
				fpOffset += sizeof(double);
			}
		}
	}

	s390_lmg (code, s390_r6, s390_r14, STK_BASE, S390_REG_SAVE_OFFSET);
	for (i = s390_r6; i < s390_r15; i++) 
		mono_emit_unwind_op_same_value (cfg, code, i);
	s390_br  (code, s390_r14);

	/* Restore the unwind state to be the same as before the epilog */
	mono_emit_unwind_op_restore_state (cfg, code);

	set_code_cursor (cfg, code);

}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecture-specific exception emission
 * 
 * @param[in] @cfg - Compile control block
 *
 * Create the instruction sequence for exception handling
 */

void
mono_arch_emit_exceptions (MonoCompile *cfg) 
{
	MonoJumpInfo 	*patch_info;
	guint8		*code;
	int		nThrows = 0,
			exc_count = 0,
			iExc;
	guint32		code_size;
	MonoClass	*exc_classes [MAX_EXC];
	guint8		*exc_throw_start [MAX_EXC];

	for (patch_info = cfg->patch_info; 
	     patch_info; 
	     patch_info = patch_info->next) {
		if (patch_info->type == MONO_PATCH_INFO_EXC)
			exc_count++;
	}

	code_size = exc_count * 48;

	code = realloc_code (cfg, code_size);

	/*
	 * Add code to raise exceptions 
	 */
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		switch (patch_info->type) {
		case MONO_PATCH_INFO_EXC: {
			guint8 *ip = patch_info->ip.i + cfg->native_code;
			MonoClass *exc_class;

			/*
			 * Patch the branch in epilog to come here
			 */
			s390_patch_rel (ip + 2, (guint64) S390_RELATIVE(code,ip));

			exc_class = mono_class_load_from_name (mono_defaults.corlib,
							  "System", 
							  patch_info->data.name);

			for (iExc = 0; iExc < nThrows; ++iExc)
				if (exc_classes [iExc] == exc_class)
					break;
		
			if (iExc < nThrows) {
				s390_jcl (code, S390_CC_UN, 
					  (guint64) exc_throw_start [iExc]);
				patch_info->type = MONO_PATCH_INFO_NONE;
			} else {
	
				if (nThrows < MAX_EXC) {
					exc_classes [nThrows]     = exc_class;
					exc_throw_start [nThrows] = code;
				}
	
				/*
				 * Patch the parameter passed to the handler
				 */
				S390_SET  (code, s390_r2, m_class_get_type_token (exc_class));
				/*
				 * Load return address & parameter register
				 */
				s390_larl (code, s390_r14, (guint64)S390_RELATIVE((patch_info->ip.i +
							   cfg->native_code + 8), code));
				/*
				 * Reuse the current patch to set the jump
				 */
				patch_info->type              = MONO_PATCH_INFO_JIT_ICALL_ID;
				patch_info->data.jit_icall_id = MONO_JIT_ICALL_mono_arch_throw_corlib_exception;
				patch_info->ip.i	      = code - cfg->native_code;
				S390_BR_TEMPLATE (code, s390_r1);
			}
			break;
		}
		default:
			/* do nothing */
			break;
		}
	}
	set_code_cursor (cfg, code);
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecture-specific finishing of initialization
 * 
 * Perform any architectural-specific operations at the conclusion of
 * the initialization phase
 */

void
mono_arch_finish_init (void)
{
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecture-specific instruction emission for method
 *
 * @param[in] @cfg - Compile Control block
 * @param[in] @cmethod - Current method
 * @param[in] @fsig - Method signature
 * @param[in] @args - Arguments to method
 * @returns Instruction(s) required for architecture
 * 
 * Provide any architectural shortcuts for specific methods.
 */

MonoInst *
mono_arch_emit_inst_for_method (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	MonoInst *ins = NULL;

	return ins;
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Decompose opcode into a System z operation
 *
 * @param[in] @cfg - Compile Control block
 * @param[in] @ins - Mono Instruction
 * 
 * Substitute a System z instruction for a Mono operation.
 */

void
mono_arch_decompose_opts (MonoCompile *cfg, MonoInst *ins)
{
	/* 
	 * Have to rename these to avoid being decomposed normally, since the normal 
	 * decomposition does not work on S390.
	 */
	switch (ins->opcode) {
	case OP_ISUB_OVF:
		ins->opcode = OP_S390_ISUB_OVF;
		break;
	case OP_ISUB_OVF_UN:
		ins->opcode = OP_S390_ISUB_OVF_UN;
		break;
	case OP_IADD_OVF:
		ins->opcode = OP_S390_IADD_OVF;
		break;
	case OP_IADD_OVF_UN:
		ins->opcode = OP_S390_IADD_OVF_UN;
		break;
	case OP_LADD_OVF:
		ins->opcode = OP_S390_LADD_OVF;
		break;
	case OP_LADD_OVF_UN:
		ins->opcode = OP_S390_LADD_OVF_UN;
		break;
	case OP_LSUB_OVF:
		ins->opcode = OP_S390_LSUB_OVF;
		break;
	case OP_LSUB_OVF_UN:
		ins->opcode = OP_S390_LSUB_OVF_UN;
		break;
	default:
		break;
	}
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Determine the cost of allocation a variable
 *
 * @param[in] @cfg - Compile Control block
 * @param[in] @vmv - Mono Method Variable
 * @returns Cost (hardcoded on s390x to 2)
 * 
 * Determine the cost, in the number of memory references, of the action 
 * of allocating the variable VMV into a register during global register  
 * allocation.
 *
 */

guint32
mono_arch_regalloc_cost (MonoCompile *cfg, MonoMethodVar *vmv)
{
	/* FIXME: */
	return 2;
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architectural specific register window flushing
 *
 * Not applicable for s390x so we just do nothing
 *
 */

void 
mono_arch_flush_register_windows (void)
{
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architectural specific check if value may be immediate
 *
 * @param[in] @opcode - Operation code
 * @param[in] @imm_opcode - Immediate operation code
 * @param[in] @imm - Value to be examined
 * @returns True if it is a valid immediate value
 * 
 * Determine if operand qualifies as an immediate value. For s390x
 * this is a value in the range -2**32/2**32-1
 *
 */

gboolean 
mono_arch_is_inst_imm (int opcode, int imm_opcode, gint64 imm)
{
	return s390_is_imm32 (imm);
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architectural specific patch offset value for AOT
 *
 * @param[in] @code - Location of code to check
 * @returns Offset
 * 
 * Dummy entry point if/when s390x supports AOT.
 */

guint32
mono_arch_get_patch_offset (guint8 *code)
{
	return 0;
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architectural specific returning of register from context
 *
 * @param[in] @ctx - Mono context
 * @param[in] @reg - Register number to be returned
 * @returns Contents of the register from the context
 * 
 * Return a register from the context.
 */

host_mgreg_t
mono_arch_context_get_int_reg (MonoContext *ctx, int reg)
{
	return ctx->uc_mcontext.gregs[reg];
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architectural specific setting of a register in the context
 *
 * @param[in] @ctx - Mono context
 * @param[in] @reg - Register number to be returned
 * @param[in] @val - Value to be set
 * 
 * Set the specified register in the context with the value passed
 */

void
mono_arch_context_set_int_reg (MonoContext *ctx, int reg, host_mgreg_t val)
{
	ctx->uc_mcontext.gregs[reg] = val;
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architectural specific returning of the "this" value from context
 *
 * @param[in] @ctx - Mono context
 * @param[in] @code - Current location
 * @returns Pointer to the "this" object
 * 
 * Extract register 2 from the context as for s390x this is where the 
 * this parameter is passed
 */

gpointer
mono_arch_get_this_arg_from_call (host_mgreg_t *regs, guint8 *code)
{
	return (gpointer) regs [s390_r2];
}

/*========================= End of Function ========================*/
 
/**
 *  
 * @brief Delegation trampoline processing
 *
 * @param[in] @info - Trampoline information
 * @param[in] @has_target - Use target from delegation
 * @param[in] @param_count - Count of parameters
 * @param[in] @aot - AOT indicator
 * @returns Next instruction location
 * 
 * Process the delegation trampolines
 */

static guint8 *
get_delegate_invoke_impl (MonoTrampInfo **info, gboolean has_target, MonoMethodSignature *sig, gboolean aot)
{
	guint8 *code, *start;

	if (has_target) {
		int size = 32;

		start = code = (guint8 *) mono_global_codeman_reserve (size);

		/* Replace the this argument with the target */
		s390_lg   (code, s390_r1, 0, s390_r2, MONO_STRUCT_OFFSET (MonoDelegate, method_ptr));
		s390_lg   (code, s390_r2, 0, s390_r2, MONO_STRUCT_OFFSET (MonoDelegate, target));
		s390_br   (code, s390_r1);
		g_assert ((code - start) <= size);

		mono_arch_flush_icache (start, size);
		MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_DELEGATE_INVOKE, NULL));
	} else {
		int size, i, offset = S390_MINIMAL_STACK_SIZE, iReg = s390_r2;
		CallInfo *cinfo = get_call_info (NULL, sig);

		size = 32 + sig->param_count * 8;
		start = code = (guint8 *) mono_global_codeman_reserve (size);

		s390_lg (code, s390_r1, 0, s390_r2, MONO_STRUCT_OFFSET (MonoDelegate, method_ptr));
		/* slide down the arguments */
		for (i = 0; i < sig->param_count; ++i) {
			switch(cinfo->args[i].regtype) {
			case RegTypeGeneral :
				if (iReg < S390_LAST_ARG_REG) {
					s390_lgr (code, iReg, (iReg + 1));
				} else {
					s390_lg (code, iReg, 0, STK_BASE, offset);
				}
				iReg++;
				break;
			default :
				s390_mvc (code, sizeof(uintptr_t), STK_BASE, offset, STK_BASE, offset+sizeof(uintptr_t)); 
				offset += sizeof(uintptr_t);
			}
		}
		s390_br (code, s390_r1);

		g_free (cinfo);

		g_assert ((code - start) <= size);

		mono_arch_flush_icache (start, size);
		MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_DELEGATE_INVOKE, NULL));
	}

	if (has_target) {
		*info = mono_tramp_info_create ("delegate_invoke_impl_has_target", start, code - start, NULL, NULL);
	} else {
		char *name = g_strdup_printf ("delegate_invoke_impl_target_%d", sig->param_count);
		*info = mono_tramp_info_create (name, start, code - start, NULL, NULL);
		g_free (name);
	}

	return start;
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecture-specific delegation trampolines processing
 *
 * @returns List of trampolines
 * 
 * Return a list of MonoTrampInfo structures for the delegate invoke impl trampolines.
 */

GSList*
mono_arch_get_delegate_invoke_impls (void)
{
	GSList *res = NULL;
	MonoTrampInfo *info;

	get_delegate_invoke_impl (&info, TRUE, 0, TRUE);
	res = g_slist_prepend (res, info);

#if 0
	for (int i = 0; i <= MAX_ARCH_DELEGATE_PARAMS; ++i) {
		get_delegate_invoke_impl (&info, FALSE, NULL, TRUE);
		res = g_slist_prepend (res, info);
	}
#endif

	return res;
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecture-specific delegation trampoline processing
 *
 * @param[in] @sig - Method signature
 * @param[in] @has_target - Whether delegation contains a target
 * @returns Trampoline
 * 
 * Return a pointer to a delegation trampoline
 */

gpointer
mono_arch_get_delegate_invoke_impl (MonoMethodSignature *sig, gboolean has_target)
{
	guint8 *code, *start;

	if (sig->param_count > MAX_ARCH_DELEGATE_PARAMS)
		return NULL;

	/* FIXME: Support more cases */
	if (MONO_TYPE_ISSTRUCT (mini_get_underlying_type (sig->ret)))
		return NULL;

	if (has_target) {
		static guint8* cached = NULL;

		if (cached)
			return cached;

		if (mono_ee_features.use_aot_trampolines) {
			start = (guint8 *) mono_aot_get_trampoline ("delegate_invoke_impl_has_target");
		} else {
			MonoTrampInfo *info;
			start = get_delegate_invoke_impl (&info, TRUE, sig, FALSE);
			mono_tramp_info_register (info, NULL);
		}

		mono_memory_barrier ();

		cached = start;
	} else {
		static guint8* cache [MAX_ARCH_DELEGATE_PARAMS + 1] = {NULL};
		int i;

		if (sig->param_count > MAX_ARCH_DELEGATE_PARAMS)
			return NULL;
		for (i = 0; i < sig->param_count; ++i)
			if (!mono_is_regsize_var (sig->params [i]))
				return NULL;

		code = cache [sig->param_count];
		if (code)
			return code;

		if (mono_ee_features.use_aot_trampolines) {
			char *name = g_strdup_printf ("delegate_invoke_impl_target_%d", sig->param_count);
			start = (guint8 *) mono_aot_get_trampoline (name);
			g_free (name);
		} else {
			MonoTrampInfo *info;
			start = get_delegate_invoke_impl (&info, FALSE, sig, FALSE);
			mono_tramp_info_register (info, NULL);
		}

		mono_memory_barrier ();

		cache [sig->param_count] = start;
	}
	return start;
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecture-specific delegation virtual trampoline processing
 *
 * @param[in] @sig - Method signature
 * @param[in] @method - Method
 * @param[in] @offset - Offset into vtable
 * @param[in] @load_imt_reg - Whether to load the LMT register
 * @returns Trampoline
 * 
 * Return a pointer to a delegation virtual trampoline
 */

gpointer
mono_arch_get_delegate_virtual_invoke_impl (MonoMethodSignature *sig, MonoMethod *method, 
					    int offset, gboolean load_imt_reg)
{
	guint8 *code, *start;
	int size = 40;

	start = code = (guint8 *) mono_global_codeman_reserve (size);

	/*
	 * Replace the "this" argument with the target
	 */
	s390_lgr  (code, s390_r1, s390_r2);
	s390_lg   (code, s390_r2, 0, s390_r1, MONO_STRUCT_OFFSET(MonoDelegate, target));        

	/*
	 * Load the IMT register, if needed
	 */
	if (load_imt_reg) {
		s390_lg  (code, MONO_ARCH_IMT_REG, 0, s390_r1, MONO_STRUCT_OFFSET(MonoDelegate, method));
	}

	/*
	 * Load the vTable
	 */
	s390_lg  (code, s390_r1, 0, s390_r2, MONO_STRUCT_OFFSET(MonoObject, vtable));
	if (offset != 0) {
		s390_agfi(code, s390_r1, offset);
	}
	s390_lg  (code, s390_r1, 0, s390_r1, 0);
	s390_br  (code, s390_r1);

	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_DELEGATE_INVOKE, NULL));

	return(start);
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecture-specific build of IMT trampoline
 *
 * @param[in] @vtable - Mono VTable
 * @param[in] @domain - Mono Domain
 * @param[in] @imt_entries - List of IMT check items
 * @param[in] @count - Count of items
 * @param[in] @fail_tramp - Pointer to a failure trampoline
 * @returns Trampoline
 * 
 * Return a pointer to an IMT trampoline
 */

gpointer
mono_arch_build_imt_trampoline (MonoVTable *vtable,
								MonoIMTCheckItem **imt_entries, int count,
								gpointer fail_tramp)
{
	int i;
	int size = 0;
	guchar *code, *start;

	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		if (item->is_equals) {
			if (item->check_target_idx) {
				if (!item->compare_done)
					item->chunk_size += CMP_SIZE + JUMP_SIZE;
				if (item->has_target_code)
					item->chunk_size += BR_SIZE + JUMP_SIZE + LOADCON_SIZE;
				else
					item->chunk_size += BR_SIZE + JUMP_SIZE + LOADCON_SIZE + 
							    LOAD_SIZE;
			} else {
				if (fail_tramp) {
					item->chunk_size += CMP_SIZE + 2 * BR_SIZE + JUMP_SIZE + 
							    2 * LOADCON_SIZE;
					if (!item->has_target_code)
						item->chunk_size += LOAD_SIZE;
				} else {
					item->chunk_size += LOADCON_SIZE + LOAD_SIZE + BR_SIZE;
#if ENABLE_WRONG_METHOD_CHECK
					item->chunk_size += CMP_SIZE + JUMP_SIZE;
#endif
				}
			}
		} else {
			item->chunk_size += CMP_SIZE + JUMP_SIZE;
			imt_entries [item->check_target_idx]->compare_done = TRUE;
		}
		size += item->chunk_size;
	}

	if (fail_tramp) {
		code = (guint8 *)mini_alloc_generic_virtual_trampoline (vtable, size);
	} else {
		MonoMemoryManager *mem_manager = m_class_get_mem_manager (vtable->klass);
		code = mono_mem_manager_code_reserve (mem_manager, size);
	}

	start = code;

	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		item->code_target = (guint8 *) code;
		if (item->is_equals) {
			if (item->check_target_idx) {
				if (!item->compare_done) {
					S390_SET  (code, s390_r0, item->key);
					s390_cgr  (code, s390_r0, MONO_ARCH_IMT_REG);
				}
				item->jmp_code = (guint8*) code;
				s390_jcl (code, S390_CC_NE, 0);
				
				if (item->has_target_code) {
					S390_SET (code, s390_r1, item->value.target_code);
				} else {
					S390_SET (code, s390_r1, (&(vtable->vtable [item->value.vtable_slot])));
					s390_lg	 (code, s390_r1, 0, s390_r1, 0);
				}
				s390_br	  (code, s390_r1);
			} else {
				if (fail_tramp) {
					gint64  target;

					S390_SET  (code, s390_r0, item->key);
					s390_cgr  (code, s390_r0, MONO_ARCH_IMT_REG);
					item->jmp_code = (guint8*) code;
					s390_jcl  (code, S390_CC_NE, 0);
					if (item->has_target_code) {
						S390_SET (code, s390_r1, item->value.target_code);
					} else {
						g_assert (vtable);
						S390_SET  (code, s390_r1, 
							   (&(vtable->vtable [item->value.vtable_slot])));
						s390_lg	  (code, s390_r1, 0, s390_r1, 0);
					}
					s390_br	  (code, s390_r1);
					target = (gint64) S390_RELATIVE(code, item->jmp_code);
					s390_patch_rel(item->jmp_code+2, target);
					S390_SET  (code, s390_r1, fail_tramp);
					s390_br	  (code, s390_r1);
					item->jmp_code = NULL;
				} else {
				/* enable the commented code to assert on wrong method */
#if ENABLE_WRONG_METHOD_CHECK
					g_assert_not_reached ();
#endif
					S390_SET (code, s390_r1, (&(vtable->vtable [item->value.vtable_slot])));
					s390_lg	  (code, s390_r1, 0, s390_r1, 0);
					s390_br	  (code, s390_r1);
				}
			}
		} else {
			S390_SET  (code, s390_r0, item->key);
			s390_cgr  (code, MONO_ARCH_IMT_REG, s390_r0);
			item->jmp_code = (guint8 *) code;
			s390_jcl  (code, S390_CC_GE, 0);
		}
	}
	/* 
	 * patch the branches to get to the target items 
	 */
	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		if (item->jmp_code) {
			if (item->check_target_idx) {
				gint64 offset;
				offset = (gint64) S390_RELATIVE(imt_entries [item->check_target_idx]->code_target,
						       item->jmp_code);
				s390_patch_rel ((guchar *) item->jmp_code + 2, (guint64) offset);
			}
		}
	}

	mono_arch_flush_icache ((guint8*)start, (code - start));
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_IMT_TRAMPOLINE, NULL));

	if (!fail_tramp) 
		UnlockedAdd (&mono_stats.imt_trampolines_size, code - start);

	g_assert (code - start <= size);

	mono_tramp_info_register (mono_tramp_info_create (NULL, start, code - start, NULL, NULL), NULL);

	return (start);
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecture-specific return of pointer to IMT method
 *
 * @param[in] @regs - Context registers
 * @param[in] @code - Current location
 * @returns Pointer to IMT method
 * 
 * Extract the value of the IMT register from the context
 */

MonoMethod*
mono_arch_find_imt_method (host_mgreg_t *regs, guint8 *code)
{
	return ((MonoMethod *) regs [MONO_ARCH_IMT_REG]);
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecture-specific return of pointer static call vtable.
 *
 * @param[in] @regs - Context registers
 * @param[in] @code - Current location
 * @returns Pointer to static call vtable
 * 
 * Extract the value of the RGCTX register from the context which
 * points to the static call vtable.
 */

MonoVTable*
mono_arch_find_static_call_vtable (host_mgreg_t *regs, guint8 *code)
{
	return (MonoVTable*)(gsize) regs [MONO_ARCH_RGCTX_REG];
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecture-specific return of unwind bytecode for DWARF CIE
 *
 * @returns Unwind byte code
 *
 * Returns the unwind bytecode for DWARF CIE
 */

GSList*
mono_arch_get_cie_program (void)
{
	GSList *l = NULL;

	mono_add_unwind_op_def_cfa (l, 0, 0, STK_BASE, S390_CFA_OFFSET);

	return(l);
}

/*========================= End of Function ========================*/

#ifdef MONO_ARCH_SOFT_DEBUG_SUPPORTED

/**
 *  
 * @brief Architecture-specific setting of a breakpoint
 *
 * @param[in] @ji - Mono JIT Information
 * @param[in] @ip - Insruction pointer
 *
 * Set a breakpoint at the native code corresponding to JI at NATIVE_OFFSET.  
 * The location should contain code emitted by OP_SEQ_POINT.
 */

void
mono_arch_set_breakpoint (MonoJitInfo *ji, guint8 *ip)
{
	guint8 *bp = ip;

	/* IP should point to a LGHI R1,0 */
	g_assert (bp[0] == 0xa7);

	/* Replace it with a LGHI R1,1 */
	s390_lghi (bp, s390_r1, 1);
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecture-specific clearing of a breakpoint
 *
 * @param[in] @ji - Mono JIT Information
 * @param[in] @ip - Insruction pointer
 *
 * Replace the breakpoint with a no-operation.
 */

void
mono_arch_clear_breakpoint (MonoJitInfo *ji, guint8 *ip)
{
	guint8 *bp = ip;

	/* IP should point to a LGHI R1,1 */
	g_assert (bp[0] == 0xa7);

	/* Replace it with a LGHI R1,0 */
	s390_lghi (bp, s390_r1, 0);
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecture-specific check if this is a breakpoint event
 *
 * @param[in] @info - Signal information
 * @param[in] @sigctx - Signal context
 * @returns True if this is a breakpoint event
 *
 * We use soft breakpoints so always return FALSE
 */

gboolean
mono_arch_is_breakpoint_event (void *info, void *sigctx)
{
	/* We use soft breakpoints on s390x */
	return FALSE;
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecture-specific skip of a breakpoint
 *
 * @param[in] @ctx - Mono Context
 * @param[in] @ji - Mono JIT information
 *
 * We use soft breakpoints so this is a no-op
 */

void
mono_arch_skip_breakpoint (MonoContext *ctx, MonoJitInfo *ji)
{
	g_assert_not_reached ();
}

/*========================= End of Function ========================*/
	
/**
 *  
 * @brief Architecture-specific start of single stepping
 *
 * Unprotect the trigger page to enable single stepping
 */

void
mono_arch_start_single_stepping (void)
{
	ss_trampoline = mini_get_single_step_trampoline();
}

/*========================= End of Function ========================*/
	
/**
 *  
 * @brief Architecture-specific stop of single stepping
 *
 * Write-protect the trigger page to disable single stepping
 */

void
mono_arch_stop_single_stepping (void)
{
	ss_trampoline = NULL;
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecture-specific check if single stepping event
 *
 * @param[in] @info - Signal information
 * @param[in] @sigctx - Signal context
 * @returns True if this is a single stepping event
 *
 * Return whether the machine state in sigctx corresponds to a single step event.
 * On s390x we use soft breakpoints so return FALSE
 */

gboolean
mono_arch_is_single_step_event (void *info, void *sigctx)
{
	/* We use soft breakpoints on s390x */
	return FALSE;
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecture-specific skip of a single stepping event
 *
 * @param[in] @ctx - Mono Context
 *
 * Modify the ctx so the IP is placed after the single step trigger
 * instruction, so that the instruction is not executed again.
 * On s390x we use soft breakpoints so we shouldn't get here
 */

void
mono_arch_skip_single_step (MonoContext *ctx)
{
	g_assert_not_reached();
}

/*========================= End of Function ========================*/

/**
 *  
 * @brief Architecture-specific creation of sequence point information
 *
 * @param[in] @domain - Mono Domain
 * @param[in] @code - Current location pointer
 * @returns Sequence Point Information
 *
 * Return a pointer to a data struction which is used by the sequence 
 * point implementation in AOTed code. A no-op on s390x until AOT is
 * ever supported.
 */

SeqPointInfo *
mono_arch_get_seq_point_info (MonoDomain *domain, guint8 *code)
{
	SeqPointInfo *info;
	MonoJitInfo *ji;
	MonoJitMemoryManager *jit_mm;

	jit_mm = get_default_jit_mm ();

	jit_mm_lock (jit_mm);
	info = (SeqPointInfo *)g_hash_table_lookup (jit_mm->arch_seq_points, code);
	jit_mm_unlock (jit_mm);

	if (!info) {
		ji = mini_jit_info_table_find (code);
		g_assert (ji);

		// FIXME: Optimize the size
		info = (SeqPointInfo *)g_malloc0 (sizeof (SeqPointInfo) + (ji->code_size * sizeof (gpointer)));

		info->ss_tramp_addr = &ss_trampoline;

		jit_mm_lock (jit_mm);
		g_hash_table_insert (jit_mm->arch_seq_points, code, info);
		jit_mm_unlock (jit_mm);
	}

	return info;

}

/*========================= End of Function ========================*/

#endif

/**
 *  
 * @brief Architecture-specific check of supported operation codes
 *
 * @param[in] @opcode - Operation code to be checked
 * @returns True if operation code is supported
 *
 * Check if a mono operation is supported in hardware.
 */

gboolean
mono_arch_opcode_supported (int opcode)
{
	switch (opcode) {
	case OP_ATOMIC_ADD_I4:
	case OP_ATOMIC_ADD_I8:
	case OP_ATOMIC_EXCHANGE_I4:
	case OP_ATOMIC_EXCHANGE_I8:
		return TRUE;
	default:
		return FALSE;
	}
}

/*========================= End of Function ========================*/

#ifndef DISABLE_JIT

/**
 *  
 * @brief Architecture-specific check of tailcall support
 *
 * @param[in] @cfg - Mono Compile control block
 * @param[in] @caller_sig - Signature of caller
 * @param[in] @callee_sig - Signature of callee
 * @param[in] @virtual_ - Whether this a virtual call
 * @returns True if the tailcall operation is supported
 *
 * Check if a tailcall may be made from caller to callee based on a
 * number of conditions including parameter types and stack sizes
 */

gboolean
mono_arch_tailcall_supported (MonoCompile *cfg, MonoMethodSignature *caller_sig, MonoMethodSignature *callee_sig, gboolean virtual_)
{
	g_assert (caller_sig);
	g_assert (callee_sig);

	CallInfo *caller_info = get_call_info (NULL, caller_sig);
	CallInfo *callee_info = get_call_info (NULL, callee_sig);

	gboolean res = IS_SUPPORTED_TAILCALL (callee_info->stack_usage <= caller_info->stack_usage);

	// Any call that would result in parameters being placed on the stack cannot be "tailed" as it may 
	// result in the callers parameter variables being overwritten.
	ArgInfo const * const ainfo = callee_info->args + callee_sig->hasthis;
	for (int i = 0; res && i < callee_sig->param_count; ++i) {
		switch(ainfo[i].regtype) {
		case RegTypeGeneral :
			// R6 is both used as argument register and call-saved
			// This means we cannot use a tail call if R6 is needed
			if (ainfo[i].reg == S390_LAST_ARG_REG)
				res = FALSE;
			else
				res = TRUE;
			break;
		case RegTypeFP :
		case RegTypeFPR4 :
		case RegTypeStructByValInFP :
			res = TRUE;
			break;
		case RegTypeBase :
			res = FALSE;
			break;
		case RegTypeStructByAddr :
			if (ainfo[i].reg == STK_BASE) 
				res = FALSE;
			else
				res = TRUE;
			break;
		case RegTypeStructByVal :
			if (ainfo[i].reg == STK_BASE) 
				res = FALSE;
			else {
				switch(ainfo[i].size) {
				case 0: case 1: case 2: case 4: case 8:
				    res = TRUE;
				    break;
				default:
				    res = FALSE;
				}
			}
			break;
		}
	}

	g_free (caller_info);
	g_free (callee_info);

	return(res);
}

/*========================= End of Function ========================*/

#endif

/**
 *  
 * @brief Architecture-specific load function
 *
 * @param[in] @jit_call_id - JIT callee identifier
 * @returns Pointer to load function trampoline
 *
 * A no-operation on s390x until if/when it supports AOT.
 */

gpointer
mono_arch_load_function (MonoJitICallId jit_icall_id)
{
	return NULL;
}

/*========================= End of Function ========================*/
