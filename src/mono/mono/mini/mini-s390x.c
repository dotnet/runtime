/*------------------------------------------------------------------*/
/* 								    */
/* Name        - mini-s390.c					    */
/* 								    */
/* Function    - S/390 backend for the Mono code generator.         */
/* 								    */
/* Name	       - Neale Ferguson (Neale.Ferguson@SoftwareAG-usa.com) */
/* 								    */
/* Date        - January, 2004					    */
/* 								    */
/* Derivation  - From mini-x86 & mini-ppc by -			    */
/* 	         Paolo Molaro (lupus@ximian.com) 		    */
/* 		 Dietmar Maurer (dietmar@ximian.com)		    */
/* 								    */
/*------------------------------------------------------------------*/

/*------------------------------------------------------------------*/
/*                 D e f i n e s                                    */
/*------------------------------------------------------------------*/

#define MAX_ARCH_DELEGATE_PARAMS 7

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

#define CHECK_SRCDST_NCOM_F						\
	if (ins->dreg == ins->sreg2) {					\
		src2 = s390_f15;					\
		s390_ldr (code, s390_r13, ins->sreg2);			\
	} else {							\
		src2 = ins->sreg2;					\
	}								\
	if (ins->dreg != ins->sreg1) {					\
		s390_ldr (code, ins->dreg, ins->sreg1);			\
	}

#define MONO_EMIT_NEW_MOVE(cfg,dest,offset,src,imm,size) do { 			\
                MonoInst *inst; 						\
		int sReg, dReg;							\
		MONO_INST_NEW (cfg, inst, OP_NOP);				\
		if (size > 256) {						\
			inst->dreg	  = dest;				\
			inst->inst_offset = offset;				\
			inst->sreg1	  = src;				\
			inst->inst_imm	  = imm;				\
		} else {							\
			if (s390_is_uimm12(offset)) {				\
				inst->dreg	  = dest;			\
				inst->inst_offset = offset;			\
			} else {						\
				dReg = mono_alloc_preg (cfg);			\
				MONO_EMIT_NEW_BIALU_IMM(cfg, OP_ADD_IMM,	\
					dReg, dest, offset);			\
				inst->dreg	  = dReg;			\
				inst->inst_offset = 0;				\
			}							\
			if (s390_is_uimm12(imm)) {  				\
				inst->sreg1	  = src; 			\
				inst->inst_imm    = imm;   			\
			} else {						\
				sReg = mono_alloc_preg (cfg); 			\
				MONO_EMIT_NEW_BIALU_IMM(cfg, OP_ADD_IMM,	\
					sReg, src, imm);   			\
				inst->sreg1	  = sReg;			\
				inst->inst_imm    = 0;				\
			}							\
		}								\
                inst->opcode 	  	= OP_S390_MOVE; 			\
		inst->backend.size	= size;					\
        MONO_ADD_INS (cfg->cbb, inst);		 				\
	} while (0)

#define MONO_OUTPUT_VTR(cfg, size, dr, sr, so) do {				\
	int reg = mono_alloc_preg (cfg); \
	switch (size) {								\
		case 0: 							\
			MONO_EMIT_NEW_ICONST(cfg, reg, 0);			\
		break;								\
		case 1:								\
			MONO_EMIT_NEW_LOAD_MEMBASE_OP(cfg, OP_LOADU1_MEMBASE,	\
				reg, sr, so);					\
		break;								\
		case 2:								\
			MONO_EMIT_NEW_LOAD_MEMBASE_OP(cfg, OP_LOADU2_MEMBASE,	\
				reg, sr, so);					\
		break;								\
		case 4:								\
			MONO_EMIT_NEW_LOAD_MEMBASE_OP(cfg, OP_LOADI4_MEMBASE,	\
				reg, sr, so);					\
		break;								\
		case 8:								\
			MONO_EMIT_NEW_LOAD_MEMBASE_OP(cfg, OP_LOADI8_MEMBASE,	\
				reg, sr, so);					\
		break;								\
	}									\
	mono_call_inst_add_outarg_reg(cfg, call, reg, dr, FALSE);		\
} while (0)

#define MONO_OUTPUT_VTS(cfg, size, dr, dx, sr, so) do {				\
	int tmpr;								\
	switch (size) {								\
		case 0: 							\
			tmpr = mono_alloc_preg (cfg); \
			MONO_EMIT_NEW_ICONST(cfg, tmpr, 0);			\
			MONO_EMIT_NEW_STORE_MEMBASE(cfg, OP_STORE_MEMBASE_REG,  \
				dr, dx, tmpr);					\
		break;								\
		case 1:								\
			tmpr = mono_alloc_preg (cfg); \
			MONO_EMIT_NEW_LOAD_MEMBASE_OP(cfg, OP_LOADU1_MEMBASE,	\
				tmpr, sr, so);					\
			MONO_EMIT_NEW_STORE_MEMBASE(cfg, OP_STORE_MEMBASE_REG,  \
				dr, dx, tmpr);					\
		break;								\
		case 2:								\
			tmpr = mono_alloc_preg (cfg); 				\
			MONO_EMIT_NEW_LOAD_MEMBASE_OP(cfg, OP_LOADU2_MEMBASE,	\
				tmpr, sr, so);					\
			MONO_EMIT_NEW_STORE_MEMBASE(cfg, OP_STORE_MEMBASE_REG,  \
				dr, dx, tmpr);					\
		break;								\
		case 4:								\
			tmpr = mono_alloc_preg (cfg);   			\
			MONO_EMIT_NEW_LOAD_MEMBASE_OP(cfg, OP_LOADI4_MEMBASE,	\
				tmpr, sr, so);					\
			MONO_EMIT_NEW_STORE_MEMBASE(cfg, OP_STORE_MEMBASE_REG,  \
				dr, dx, tmpr);					\
		break;								\
		case 8:								\
			MONO_EMIT_NEW_MOVE (cfg, dr, dx, sr, so, size);		\
		break;								\
	}									\
} while (0)

#undef DEBUG
#define DEBUG(a) if (cfg->verbose_level > 1) a

#define MAX_EXC	16

#define S390_TRACE_STACK_SIZE (5*sizeof(gpointer)+4*sizeof(gdouble))

#define BREAKPOINT_SIZE		sizeof(breakpoint_t)
#define S390X_NOP_SIZE	 	sizeof(I_Format)

#define MAX(a, b) ((a) > (b) ? (a) : (b))

/*
 * imt thunking size values
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

#include <mono/metadata/appdomain.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/profiler-private.h>
#include <mono/utils/mono-math.h>
#include <mono/utils/mono-mmap.h>

#include "mini-s390x.h"
#include "cpu-s390x.h"
#include "jit-icalls.h"
#include "ir-emit.h"
#include "trace.h"

/*========================= End of Includes ========================*/

/*------------------------------------------------------------------*/
/*                 T y p e d e f s                                  */
/*------------------------------------------------------------------*/

typedef struct {
	guint stack_size,
	      local_size,
	      code_size,
	      parm_size,
	      offset,
	      offStruct,
	      retStruct;
} size_data;	

/*------------------------------------------------------------------*/
/* Used by the instrument_emit_epilog 		                    */
/*------------------------------------------------------------------*/

enum {
	SAVE_NONE,
	SAVE_STRUCT,
	SAVE_ONE,
	SAVE_TWO,
	SAVE_R4,
	SAVE_R8
};

typedef struct InstList InstList;

struct InstList {
	InstList *prev;
	InstList *next;
	MonoInst *data;
};

typedef enum {
	RegTypeGeneral,
	RegTypeBase,
	RegTypeFP,
	RegTypeFPR4,
	RegTypeStructByVal,
	RegTypeStructByValInFP,
	RegTypeStructByAddr,
	RegTypeStructByAddrOnStack
} ArgStorage;

typedef struct {
	gint32  offset;		/* offset from caller's stack */
	gint32  offparm;	/* offset from callee's stack */
	guint16 vtsize; 	/* in param area */
	guint8  reg;
	ArgStorage regtype;
	guint32 size;        	/* Size of structure used by RegTypeStructByVal */
	gint32  type;		/* Data type of argument */
} ArgInfo;

typedef struct {
	int nargs;
	int lastgr;
	guint32 stack_usage;
	guint32 struct_ret;
	ArgInfo ret;
	ArgInfo sigCookie;
	size_data sz;
	int vret_arg_index;
	ArgInfo args [1];
} CallInfo;

typedef struct {
	gint64	gr[5];		/* R2-R6			    */
	gdouble fp[3];		/* F0-F2			    */
} __attribute__ ((packed)) RegParm;

typedef struct {
	RR_Format  basr;
	RI_Format  j;
	void	   *pTrigger;
	RXY_Format lg;
	RXY_Format trigger;
} __attribute__ ((packed)) breakpoint_t;

/*========================= End of Typedefs ========================*/

/*------------------------------------------------------------------*/
/*                   P r o t o t y p e s                            */
/*------------------------------------------------------------------*/

static void indent (int);
static guint8 * backUpStackPtr(MonoCompile *, guint8 *);
static void decodeParm (MonoType *, void *, int);
static void enter_method (MonoMethod *, RegParm *, char *);
static void leave_method (MonoMethod *, ...);
static gboolean is_regsize_var (MonoType *);
static inline void add_general (guint *, size_data *, ArgInfo *);
static inline void add_stackParm (guint *, size_data *, ArgInfo *, gint);
static inline void add_float (guint *, size_data *, ArgInfo *);
static CallInfo * get_call_info (MonoCompile *, MonoMemPool *, MonoMethodSignature *, gboolean);
static guchar * emit_float_to_int (MonoCompile *, guchar *, int, int, int, gboolean);
static guint8 * emit_load_volatile_arguments (guint8 *, MonoCompile *);
static void catch_SIGILL(int, siginfo_t *, void *);
static __inline__ void emit_unwind_regs(MonoCompile *, guint8 *, int, int, long);

/*========================= End of Prototypes ======================*/

/*------------------------------------------------------------------*/
/*                 G l o b a l   V a r i a b l e s                  */
/*------------------------------------------------------------------*/

int mono_exc_esp_offset = 0;

static int indent_level = 0;

int has_ld = 0;

static gint appdomain_tls_offset = -1,
	    lmf_tls_offset = -1,
	    lmf_addr_tls_offset = -1;

pthread_key_t lmf_addr_key;

gboolean lmf_addr_key_inited = FALSE; 

facilityList_t facs;

#if 0

extern __thread MonoDomain *tls_appdomain;
extern __thread MonoThread *tls_current_object;
extern __thread gpointer   mono_lmf_addr;
		
#endif

/*
 * The code generated for sequence points reads from this location, 
 * which is made read-only when single stepping is enabled.
 */
static gpointer ss_trigger_page;

/*
 * Enabled breakpoints read from this trigger page
 */
static gpointer bp_trigger_page;

breakpoint_t breakpointCode;

/*====================== End of Global Variables ===================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_regname                                 */
/*                                                                  */
/* Function	- Returns the name of the register specified by     */
/*		  the input parameter.         		 	    */
/*		                               		 	    */
/*------------------------------------------------------------------*/

const char*
mono_arch_regname (int reg) {
	static const char * rnames[] = {
		"s390_r0", "s390_sp", "s390_r2", "s390_r3", "s390_r4",
		"s390_r5", "s390_r6", "s390_r7", "s390_r8", "s390_r9",
		"s390_r10", "s390_r11", "s390_r12", "s390_r13", "s390_r14",
		"s390_r15"
	};

	if (reg >= 0 && reg < 16)
		return rnames [reg];
	else
		return "unknown";
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_fregname                                */
/*                                                                  */
/* Function	- Returns the name of the register specified by     */
/*		  the input parameter.         		 	    */
/*		                               		 	    */
/*------------------------------------------------------------------*/

const char*
mono_arch_fregname (int reg) {
	static const char * rnames[] = {
		"s390_f0", "s390_f1", "s390_f2", "s390_f3", "s390_f4",
		"s390_f5", "s390_f6", "s390_f7", "s390_f8", "s390_f9",
		"s390_f10", "s390_f11", "s390_f12", "s390_f13", "s390_f14",
		"s390_f15"
	};

	if (reg >= 0 && reg < 16)
		return rnames [reg];
	else
		return "unknown";
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- arch_get_argument_info                            */
/*                                                                  */
/* Function	- Gathers information on parameters such as size,   */
/*		  alignment, and padding. arg_info should be large  */
/*		  enough to hold param_count + 1 entries.	    */
/*		                               			    */
/* Parameters   - @csig - Method signature     			    */
/*		  @param_count - No. of parameters to consider      */
/*		  @arg_info - An array to store the result info	    */
/*		                               			    */
/* Returns      - Size of the activation frame 			    */
/*		                               			    */
/*------------------------------------------------------------------*/

int
mono_arch_get_argument_info (MonoMethodSignature *csig, 
			     int param_count, 
			     MonoJitArgumentInfo *arg_info)
{
	int k, frame_size = 0;
	int size, align, pad;
	int offset = 8;

	if (MONO_TYPE_ISSTRUCT (csig->ret)) { 
		frame_size += sizeof (gpointer);
		offset += 8;
	}

	arg_info [0].offset = offset;

	if (csig->hasthis) {
		frame_size += sizeof (gpointer);
		offset += 8;
	}

	arg_info [0].size = frame_size;

	for (k = 0; k < param_count; k++) {
		
		if (csig->pinvoke)
			size = mono_type_native_stack_size (csig->params [k], (guint32 *) &align);
		else
			size = mini_type_stack_size (NULL, csig->params [k], &align);

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

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- emit_unwind_regs.                                 */
/*                                                                  */
/* Function	- Determines if a value can be returned in one or   */
/*                two registers.                                    */
/*                                                                  */
/*------------------------------------------------------------------*/

static void __inline__
emit_unwind_regs(MonoCompile *cfg, guint8 *code, int start, int end, long offset)
{
	int i;

	for (i = start; i < end; i++) {
		mono_emit_unwind_op_offset (cfg, code, i, offset);
		offset += sizeof(gulong);
	}
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- retFitsInReg.                                     */
/*                                                                  */
/* Function	- Determines if a value can be returned in one or   */
/*                two registers.                                    */
/*                                                                  */
/*------------------------------------------------------------------*/

static inline gboolean
retFitsInReg(guint32 size)
{
	switch (size) {
		case 0:
		case 1:
		case 2:
		case 4:
		case 8:
			return (TRUE);
		break;
		default:
			return (FALSE);
	}
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- backStackPtr.                                     */
/*                                                                  */
/* Function	- Restore Stack Pointer to previous frame.          */
/*                                                                  */
/*------------------------------------------------------------------*/

static inline guint8 *
backUpStackPtr(MonoCompile *cfg, guint8 *code)
{
	int stackSize = cfg->stack_usage;

	if (cfg->frame_reg != STK_BASE)
		s390_lgr (code, STK_BASE, cfg->frame_reg);

	if (s390_is_imm16 (stackSize)) {
		s390_aghi  (code, STK_BASE, stackSize);
	} else { 
		while (stackSize > 32767) {
			s390_aghi  (code, STK_BASE, 32767);
			stackSize -= 32767;
		}
		s390_aghi  (code, STK_BASE, stackSize);
	}
	return (code);
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- indent                                            */
/*                                                                  */
/* Function	- Perform nice indenting to current level           */
/*                                                                  */
/*------------------------------------------------------------------*/

static void 
indent (int diff) {
	int v;
	if (diff < 0)
		indent_level += diff;
	v = indent_level;
	printf("[%3d] ",v);
	while (v-- > 0) {
		printf (". ");
	}
	if (diff > 0) 
		indent_level += diff;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- cvtMonoType                                       */
/*                                                                  */
/* Function	- Convert a mono-type to a string.                  */
/*		                               			    */
/*------------------------------------------------------------------*/

static const char *
cvtMonoType(MonoTypeEnum t)
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

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- decodeParm                                        */
/*                                                                  */
/* Function	- Decode a parameter for the trace.                 */
/*		                               			    */
/*------------------------------------------------------------------*/

static void 
decodeParm(MonoType *type, void *curParm, int size)
{
	guint32 simpleType;

	if (type->byref) {
		printf("[BYREF:%p], ", *((char **) curParm));
	} else {
		simpleType = mono_type_get_underlying_type(type)->type;
enum_parmtype:
		switch (simpleType) {
			case MONO_TYPE_I :
				printf ("[INTPTR:%p], ", *((int **) curParm));
				break;
			case MONO_TYPE_U :
				printf ("[UINTPTR:%p], ", *((int **) curParm));
				break;
			case MONO_TYPE_BOOLEAN :
				printf ("[BOOL:%ld], ", *((gint64 *) curParm));
				break;
			case MONO_TYPE_CHAR :
				printf ("[CHAR:%c], ", *((int  *) curParm));
				break;
			case MONO_TYPE_I1 :
				printf ("[INT1:%ld], ", *((gint64 *) curParm));
				break; 
			case MONO_TYPE_I2 :
				printf ("[INT2:%ld], ", *((gint64 *) curParm));
				break; 
			case MONO_TYPE_I4 :
				printf ("[INT4:%ld], ", *((gint64 *) curParm));
				break; 
			case MONO_TYPE_U1 :
				printf ("[UINT1:%lu], ", *((guint64 *) curParm));
				break; 
			case MONO_TYPE_U2 :
				printf ("[UINT2:%lu], ", *((guint64 *) curParm));
				break; 
			case MONO_TYPE_U4 :
				printf ("[UINT4:%lu], ", *((guint64 *) curParm));
				break; 
			case MONO_TYPE_U8 :
				printf ("[UINT8:%lu], ", *((guint64 *) curParm));
				break; 
			case MONO_TYPE_STRING : {
				MonoString *s = *((MonoString **) curParm);
				if (s) {
					g_assert (((MonoObject *) s)->vtable->klass == mono_defaults.string_class);
					printf("[STRING:%p:%s], ", s, mono_string_to_utf8(s));
				} else {
					printf("[STRING:null], ");
				}
				break;
			}
			case MONO_TYPE_CLASS :
			case MONO_TYPE_OBJECT : {
				MonoObject *obj = *((MonoObject **) curParm);
				MonoClass *class;
				if ((obj) && (obj->vtable)) {
					printf("[CLASS/OBJ:");
					class = obj->vtable->klass;
					printf("%p [%p] ",obj,curParm);
					if (class == mono_defaults.string_class) {
						printf("[STRING:%p:%s]", 
						       obj, mono_string_to_utf8 ((MonoString *) obj));
					} else if (class == mono_defaults.int32_class) { 
						printf("[INT32:%p:%d]", 
							obj, *(gint32 *)((char *)obj + sizeof (MonoObject)));
					} else
						printf("[%s.%s:%p]", 
						       class->name_space, class->name, obj);
					printf("], ");
				} else {
					printf("[OBJECT:null], ");
				}
				break;
			}
			case MONO_TYPE_PTR :
				printf("[PTR:%p], ", *((gpointer **) (curParm)));
				break;
			case MONO_TYPE_FNPTR :
				printf("[FNPTR:%p], ", *((gpointer **) (curParm)));
				break;
			case MONO_TYPE_ARRAY :
				printf("[ARRAY:%p], ", *((gpointer **) (curParm)));
				break;
			case MONO_TYPE_SZARRAY :
				printf("[SZARRAY:%p], ", *((gpointer **) (curParm)));
				break;
			case MONO_TYPE_I8 :
				printf("[INT8:%ld], ", *((gint64 *) (curParm)));
				break;
			case MONO_TYPE_R4 :
				printf("[FLOAT4:%g], ", *((float *) (curParm)));
				break;
			case MONO_TYPE_R8 :
				printf("[FLOAT8:%g], ", *((double *) (curParm)));
				break;
			case MONO_TYPE_VALUETYPE : {
				int i;
				MonoMarshalType *info;

				if (type->data.klass->enumtype) {
					simpleType = mono_class_enum_basetype (type->data.klass)->type;
					printf("{VALUETYPE} - ");
					goto enum_parmtype;
				}

				info = mono_marshal_load_type_info (type->data.klass);

				if ((info->native_size == sizeof(float)) &&
				    (info->num_fields  == 1) &&
				    (info->fields[0].field->type->type == MONO_TYPE_R4)) {
						printf("[FLOAT4:%f], ", *((float *) (curParm)));
					break;
				}

				if ((info->native_size == sizeof(double)) &&
				    (info->num_fields  == 1) &&
				    (info->fields[0].field->type->type == MONO_TYPE_R8)) {
					printf("[FLOAT8:%g], ", *((double *) (curParm)));
					break;
				}

				printf("[VALUETYPE:");
				for (i = 0; i < size; i++)
					printf("%02x,", *((guint8 *)curParm+i));
				printf("], ");
				break;
			}
			case MONO_TYPE_TYPEDBYREF: {
				int i;
				printf("[TYPEDBYREF:");
				for (i = 0; i < size; i++)
					printf("%02x,", *((guint8 *)curParm+i));
				printf("]");
				break;
			}
			default :
				printf("[%s], ",cvtMonoType(simpleType));
		}
	}
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- enter_method                                      */
/*                                                                  */
/* Function	- Perform tracing of the entry to the current       */
/*		  method.                      			    */
/*		                               			    */
/*------------------------------------------------------------------*/

static void
enter_method (MonoMethod *method, RegParm *rParm, char *sp)
{
	int i, oParm = 0, iParm = 0;
	MonoClass *class;
	MonoObject *obj;
	MonoMethodSignature *sig;
	char *fname;
	guint64 ip;
	CallInfo *cinfo;
	ArgInfo *ainfo;
	void *curParm;

	fname = mono_method_full_name (method, TRUE);
	indent (1);
	printf ("ENTER: %s ", fname);
	g_free (fname);

	ip  = (*(guint64 *) (sp+S390_RET_ADDR_OFFSET));
	printf ("ip: %p sp: %p - ", (gpointer) ip, sp); 

	if (rParm == NULL)
		return;
	
	sig = mono_method_signature (method);
	
	cinfo = get_call_info (NULL, NULL, sig, sig->pinvoke);

	if (cinfo->struct_ret) {
		printf ("[STRUCTRET:%p], ", (gpointer) rParm->gr[0]);
		iParm = 1;
	}

	if (sig->hasthis) {
		gpointer *this = (gpointer *) rParm->gr[iParm];
		obj = (MonoObject *) this;
		switch(method->klass->this_arg.type) {
		case MONO_TYPE_VALUETYPE:
			if (obj) {
				guint64 *value = (guint64 *) ((uintptr_t)this + sizeof(MonoObject));
				printf("this:[value:%p:%016lx], ", this, *value);
			} else 
				printf ("this:[NULL], ");
			break;
		case MONO_TYPE_STRING:
			if (obj) {
				if (obj->vtable) {
					class = obj->vtable->klass;
					if (class == mono_defaults.string_class) {
						printf ("this:[STRING:%p:%s], ", 
							obj, mono_string_to_utf8 ((MonoString *)obj));
					} else {
						printf ("this:%p[%s.%s], ", 
							obj, class->name_space, class->name);
					}
				} else 
					printf("vtable:[NULL], ");
			} else 
				printf ("this:[NULL], ");
			break;
		default :
			printf("this[%s]: %p, ",cvtMonoType(method->klass->this_arg.type),this);
		}
		oParm++;
	}
					
	for (i = 0; i < sig->param_count; ++i) {
		ainfo = &cinfo->args[i + oParm];
		switch (ainfo->regtype) {
			case RegTypeGeneral :
				decodeParm(sig->params[i], &(rParm->gr[ainfo->reg-2]), ainfo->size);
				break;
			case RegTypeFP :
				decodeParm(sig->params[i], &(rParm->fp[ainfo->reg]), ainfo->size);
				break;
			case RegTypeBase :
				decodeParm(sig->params[i], sp+ainfo->offset, ainfo->size);
				break;
			case RegTypeStructByVal :
				if (ainfo->reg != STK_BASE) {
					int offset = sizeof(glong) - ainfo->size;
					curParm = &(rParm->gr[ainfo->reg-2])+offset;
				}
				else
					curParm = sp+ainfo->offset;

				if (retFitsInReg (ainfo->vtsize)) 
					decodeParm(sig->params[i], 
				 	           curParm,
					           ainfo->size);
				else
					decodeParm(sig->params[i], 
				 	           *((char **) curParm),
					           ainfo->vtsize);
				break;
			case RegTypeStructByAddr :
				if (ainfo->reg != STK_BASE) 
					curParm = &(rParm->gr[ainfo->reg-2]);
				else
					curParm = sp+ainfo->offset;

				decodeParm(sig->params[i], 
				           *((char **) curParm),
				           ainfo->vtsize);
				break;
				
			default :
				printf("???, ");
		}
	}	
	printf("\n");
	g_free(cinfo);
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- leave_method                                      */
/*                                                                  */
/* Function	-                                                   */
/*		                               			    */
/*------------------------------------------------------------------*/

static void
leave_method (MonoMethod *method, ...)
{
	MonoType *type;
	char *fname;
	guint64 ip;
	va_list ap;

	va_start(ap, method);

	fname = mono_method_full_name (method, TRUE);
	indent (-1);
	printf ("LEAVE: %s", fname);
	g_free (fname);

	type = mono_method_signature (method)->ret;

handle_enum:
	switch (type->type) {
	case MONO_TYPE_VOID:
		break;
	case MONO_TYPE_BOOLEAN: {
		int val = va_arg (ap, int);
		if (val)
			printf ("[TRUE:%d]", val);
		else 
			printf ("[FALSE]");
			
		break;
	}
	case MONO_TYPE_CHAR: {
		int val = va_arg (ap, int);
		printf ("[CHAR:%d]", val);
		break;
	}
	case MONO_TYPE_I1: {
		int val = va_arg (ap, int);
		printf ("[INT1:%d]", val);
		break;
	}
	case MONO_TYPE_U1: {
		int val = va_arg (ap, int);
		printf ("[UINT1:%d]", val);
		break;
	}
	case MONO_TYPE_I2: {
		int val = va_arg (ap, int);
		printf ("[INT2:%d]", val);
		break;
	}
	case MONO_TYPE_U2: {
		int val = va_arg (ap, int);
		printf ("[UINT2:%d]", val);
		break;
	}
	case MONO_TYPE_I4: {
		int val = va_arg (ap, int);
		printf ("[INT4:%d]", val);
		break;
	}
	case MONO_TYPE_U4: {
		int val = va_arg (ap, int);
		printf ("[UINT4:%d]", val);
		break;
	}
	case MONO_TYPE_I: {
		gint64 val = va_arg (ap, gint64);
		printf ("[INT:%ld]", val);
		printf("]");
		break;
	}
	case MONO_TYPE_U: {
		gint64 val = va_arg (ap, gint64);
		printf ("[UINT:%lu]", val);
		printf("]");
		break;
	}
	case MONO_TYPE_STRING: {
		MonoString *s = va_arg (ap, MonoString *);
;
		if (s) {
			g_assert (((MonoObject *)s)->vtable->klass == mono_defaults.string_class);
			printf ("[STRING:%p:%s]", s, mono_string_to_utf8 (s));
		} else 
			printf ("[STRING:null], ");
		break;
	}
	case MONO_TYPE_CLASS: 
	case MONO_TYPE_OBJECT: {
		MonoObject *o = va_arg (ap, MonoObject *);

		if ((o) && (o->vtable)) {
			if (o->vtable->klass == mono_defaults.boolean_class) {
				printf ("[BOOLEAN:%p:%d]", o, *((guint8 *)o + sizeof (MonoObject)));		
			} else if  (o->vtable->klass == mono_defaults.int32_class) {
				printf ("[INT32:%p:%d]", o, *((gint32 *)((char *)o + sizeof (MonoObject))));	
			} else if  (o->vtable->klass == mono_defaults.int64_class) {
				printf ("[INT64:%p:%ld]", o, *((gint64 *)((char *)o + sizeof (MonoObject))));	
			} else
				printf ("[%s.%s:%p]", o->vtable->klass->name_space, o->vtable->klass->name, o);
		} else
			printf ("[OBJECT:%p]", o);
	       
		break;
	}
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_SZARRAY: {
		gpointer p = va_arg (ap, gpointer);
		printf ("[result=%p]", p);
		break;
	}
	case MONO_TYPE_I8: {
		gint64 l =  va_arg (ap, gint64);
		printf ("[LONG:%ld]", l);
		break;
	}
	case MONO_TYPE_U8: {
		guint64 l =  va_arg (ap, guint64);
		printf ("[ULONG:%lu]", l);
		break;
	}
	case MONO_TYPE_R4: {
		double f = va_arg (ap, double);
		printf ("[FLOAT4:%g]\n", f);
		break;
	}
	case MONO_TYPE_R8: {
		double f = va_arg (ap, double);
		printf ("[FLOAT8:%g]\n", f);
		break;
	}
	case MONO_TYPE_VALUETYPE: {
		MonoMarshalType *info;
		if (type->data.klass->enumtype) {
			type = mono_class_enum_basetype (type->data.klass);
			goto handle_enum;
		} else {
			int size, align;

			info = mono_marshal_load_type_info (type->data.klass);

			if ((info->native_size == sizeof(float)) &&
			    (info->num_fields  == 1) &&
			    (info->fields[0].field->type->type == MONO_TYPE_R4)) {
				double f = va_arg (ap, double);
				printf("[FLOAT4:%g]\n", (double) f);
				break;
			}

			if ((info->native_size == sizeof(double)) &&
			    (info->num_fields  == 1) &&
			    (info->fields[0].field->type->type == MONO_TYPE_R8)) {
				double f = va_arg (ap, double);
				printf("[FLOAT8:%g]\n", f);
				break;
			}

			size = mono_type_size (type, &align);
			switch (size) {
				case 1: {
					guint32 p = va_arg (ap, guint32);
					printf ("[%02x]\n",p);
					break;
				}
				case 2: {
					guint32 p = va_arg (ap, guint32);
					printf ("[%04x]\n",p);
					break;
				}
				case 4: {
					guint32 p = va_arg (ap, guint32);
					printf ("[%08x]\n",p);
					break;
				}
				case 8: {
					guint64 p = va_arg (ap, guint64);
					printf ("[%016lx]\n",p);
					break;
				}
				default: {
					gpointer p = va_arg (ap, gpointer);
					printf ("[VALUETYPE] %p\n",p);
				}
			}
		}
		break;
	}
	case MONO_TYPE_TYPEDBYREF: {
		guint8 *p = va_arg (ap, gpointer);
		int j, size, align;
		size = mono_type_size (type, &align);
		switch (size) {
		case 1:
		case 2:
		case 4:
		case 8:
			printf ("[");
			for (j = 0; p && j < size; j++)
				printf ("%02x,", p [j]);
			printf ("]\n");
			break;
		default:
			printf ("[TYPEDBYREF]\n");
		}
	}
		break;
	default:
		printf ("(unknown return type %x)", 
			mono_method_signature (method)->ret->type);
	}

	ip = ((gint64) __builtin_return_address (0));
	printf (" ip: %p\n", (gpointer) ip);
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- catch_SIGILL					    */
/*                                                                  */
/* Function	- Catch SIGILL as a result of testing for long      */
/*		  displacement facility.      			    */
/*		                               			    */
/*------------------------------------------------------------------*/

void
catch_SIGILL(int sigNo, siginfo_t *info, void *act) {

	has_ld = 0;

}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_cpu_init                                */
/*                                                                  */
/* Function	- Perform CPU specific initialization to execute    */
/*		  managed code.               			    */
/*		                               			    */
/*------------------------------------------------------------------*/

void
mono_arch_cpu_init (void)
{
	struct sigaction sa,
			 *oldSa = NULL;
	guint mode = 1;

	/*--------------------------------------*/	
	/* Set default rounding mode for FP	*/
	/*--------------------------------------*/	
	__asm__ ("SRNM\t%0\n\t"
		: : "m" (mode));

	/*--------------------------------------*/	
	/* Determine if we have long displace-  */
	/* ment facility on this processor	*/
	/*--------------------------------------*/	
	sa.sa_sigaction = catch_SIGILL;
	sigemptyset (&sa.sa_mask);
	sa.sa_flags = SA_SIGINFO;

	sigaction (SIGILL, &sa, oldSa);

	/*--------------------------------------*/
	/* We test by executing the STY inst    */
	/*--------------------------------------*/
	__asm__ ("LGHI\t0,1\n\t"
		 "LA\t1,%0\n\t"
		 ".byte\t0xe3,0x00,0x10,0x00,0x00,0x50\n\t"
		: "=m" (has_ld) : : "0", "1");

	sigaction (SIGILL, oldSa, NULL);
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_init.                                   */
/*                                                                  */
/* Function	- Initialize architecture specific code.	    */
/*		                               			    */
/*------------------------------------------------------------------*/

void
mono_arch_init (void)
{
	guint8 *code;

#if 0
	/*
	 * When we do an architectural level set at z9 or better 
	 * we can use the STFLE instruction to show us
	 * what hardware facilities are available
	 */
	int lFacility = sizeof(facs) % 8;

	memset((char *) &facs, 0, sizeof(facs));

	__asm__ ("	lgfr	0,%1\n"
		 "	stfle	%0\n"
		 : "=m" (facs) : "r" (lFacility) : "0", "cc");
#endif

	ss_trigger_page = mono_valloc (NULL, mono_pagesize (), MONO_MMAP_READ);
	bp_trigger_page = mono_valloc (NULL, mono_pagesize (), MONO_MMAP_READ);
	mono_mprotect (bp_trigger_page, mono_pagesize (), 0);
	
	code = (guint8 *) &breakpointCode;
	s390_basr(code, s390_r13, 0);
	s390_j(code, 6);
	s390_llong(code, 0);
	s390_lg(code, s390_r13, 0, s390_r13, 4);
	s390_lg(code, s390_r0, 0, s390_r13, 0);
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_cleanup.                                */
/*                                                                  */
/* Function	- Cleanup architecture specific code	.	    */
/*		                               			    */
/*------------------------------------------------------------------*/

void
mono_arch_cleanup (void)
{
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_cpu_optimizazions                       */
/*                                                                  */
/* Function	- Returns the optimizations supported on this CPU   */
/*		                               			    */
/*------------------------------------------------------------------*/

guint32
mono_arch_cpu_optimizazions (guint32 *exclude_mask)
{
	guint32 opts = 0;

	/*----------------------------------------------------------*/
	/* No s390-specific optimizations yet 			    */
	/*----------------------------------------------------------*/
	*exclude_mask = MONO_OPT_INLINE|MONO_OPT_LINEARS;
//	*exclude_mask = MONO_OPT_INLINE;
	return opts;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		-                                                   */
/*                                                                  */
/* Function	-                                                   */
/*		                               			    */
/*------------------------------------------------------------------*/

static gboolean
is_regsize_var (MonoType *t) {
	if (t->byref)
		return TRUE;
	switch (mono_type_get_underlying_type (t)->type) {
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I:
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
		return TRUE;
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_STRING:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:
		return FALSE;
	case MONO_TYPE_VALUETYPE:
		if (t->data.klass->enumtype)
			return is_regsize_var (mono_class_enum_basetype (t->data.klass));
		return FALSE;
	}
	return FALSE;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_get_allocatable_int_vars                */
/*                                                                  */
/* Function	-                                                   */
/*		                               			    */
/*------------------------------------------------------------------*/

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
		if (is_regsize_var (ins->inst_vtype)) {
			g_assert (MONO_VARINFO (cfg, i)->reg == -1);
			g_assert (i == vmv->idx);
			vars = mono_varlist_insert_sorted (cfg, vars, vmv, FALSE);
		}
	}

	return vars;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_global_int_regs                         */
/*                                                                  */
/* Function	- Return a list of usable integer registers.        */
/*		                               			    */
/*------------------------------------------------------------------*/

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
		if (cfg->frame_reg != i)
			regs = g_list_prepend (regs, GUINT_TO_POINTER (i));
	}

	return regs;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		-  mono_arch_flush_icache                           */
/*                                                                  */
/* Function	-  Flush the CPU icache.                            */
/*		                               			    */
/*------------------------------------------------------------------*/

void
mono_arch_flush_icache (guint8 *code, gint size)
{
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- add_general                                       */
/*                                                                  */
/* Function	- Determine code and stack size incremements for a  */
/* 		  parameter.                                        */
/*                                                                  */
/*------------------------------------------------------------------*/

static void inline
add_general (guint *gr, size_data *sz, ArgInfo *ainfo)
{
	if (*gr > S390_LAST_ARG_REG) {
		sz->stack_size  = S390_ALIGN(sz->stack_size, sizeof(long));
		ainfo->offset   = sz->stack_size;
		ainfo->reg	= STK_BASE;
		ainfo->regtype  = RegTypeBase;
		sz->stack_size += sizeof(long);
		sz->local_size += sizeof(long);
		sz->offStruct  += sizeof(long);
		sz->code_size  += 12;    
	} else {
		ainfo->reg      = *gr;
		sz->code_size  += 8;    
	}
	(*gr) ++;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- add_stackParm                                     */
/*                                                                  */
/* Function	- Determine code and stack size incremements for a  */
/* 		  parameter.                                        */
/*                                                                  */
/*------------------------------------------------------------------*/

static void inline
add_stackParm (guint *gr, size_data *sz, ArgInfo *ainfo, gint size)
{
	if (*gr > S390_LAST_ARG_REG) {
		sz->stack_size  = S390_ALIGN(sz->stack_size, sizeof(long));
		ainfo->reg	    = STK_BASE;
		ainfo->offset   = sz->stack_size;
		ainfo->regtype  = RegTypeStructByAddrOnStack; 
		sz->stack_size += sizeof (gpointer);
		sz->parm_size  += sizeof(gpointer);
		sz->offStruct  += sizeof(gpointer);
	} else {
		ainfo->reg      = *gr;
		ainfo->offset   = sz->stack_size;
		ainfo->regtype  = RegTypeStructByAddr; 
	}
	(*gr) ++;
	ainfo->offparm  = sz->offset;
	sz->offset      = S390_ALIGN(sz->offset+size, sizeof(long));
	ainfo->size     = size;
	ainfo->vtsize   = size;
	sz->parm_size  += size;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- add_float                                         */
/*                                                                  */
/* Function	- Determine code and stack size incremements for a  */
/* 		  float parameter.                                  */
/*                                                                  */
/*------------------------------------------------------------------*/

static void inline
add_float (guint *fr,  size_data *sz, ArgInfo *ainfo)
{
	if ((*fr) <= S390_LAST_FPARG_REG) {
		ainfo->regtype = RegTypeFP;
		ainfo->reg     = *fr;
		sz->code_size += 4;
		(*fr) += 2;
	}
	else {
		ainfo->offset   = sz->stack_size;
		ainfo->reg      = STK_BASE;
		ainfo->regtype  = RegTypeBase;
		sz->code_size  += 4;
		sz->stack_size += sizeof(double);
		sz->local_size += sizeof(double);
		sz->offStruct  += sizeof(double);
	}
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- get_call_info                                     */
/*                                                                  */
/* Function	- Determine the amount of space required for code   */
/* 		  and stack. In addition determine starting points  */
/*		  for stack-based parameters, and area for struct-  */
/*		  ures being returned on the stack.		    */
/*                                                                  */
/*------------------------------------------------------------------*/

static CallInfo *
get_call_info (MonoCompile *cfg, MonoMemPool *mp, MonoMethodSignature *sig, gboolean is_pinvoke)
{
	guint i, fr, gr, size, pstart;
	int nParm = sig->hasthis + sig->param_count;
	MonoType *ret_type;
	guint32 simpletype, align;
	CallInfo *cinfo;
	size_data *sz;
	MonoGenericSharingContext *gsctx = cfg ? cfg->generic_sharing_context : NULL;

	if (mp)
		cinfo = mono_mempool_alloc0 (mp, sizeof (CallInfo) + sizeof (ArgInfo) * nParm);
	else
		cinfo = g_malloc0 (sizeof (CallInfo) + sizeof (ArgInfo) * nParm);

	fr                = 0;
	gr                = s390_r2;
	nParm 		  = 0;
	cinfo->struct_ret = 0;
	sz                = &cinfo->sz;
	sz->retStruct     = 0;
	sz->offset        = 0;
	sz->offStruct     = S390_MINIMAL_STACK_SIZE;
	sz->stack_size    = S390_MINIMAL_STACK_SIZE;
	sz->code_size     = 0;
	sz->parm_size     = 0;
	sz->local_size    = 0;
	align		  = 0;
	size		  = 0;

	/*----------------------------------------------------------*/
	/* We determine the size of the return code/stack in case we*/
	/* need to reserve a register to be used to address a stack */
	/* area that the callee will use.			    */
	/*----------------------------------------------------------*/

	ret_type = mono_type_get_underlying_type (sig->ret);
	ret_type = mini_get_basic_type_from_generic (gsctx, ret_type);
	simpletype = ret_type->type;
enum_retvalue:
	switch (simpletype) {
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
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_PTR:
		case MONO_TYPE_FNPTR:
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
			MonoClass *klass = mono_class_from_mono_type (sig->ret);
			if (klass->enumtype) {
				simpletype = mono_class_enum_basetype (klass)->type;
				goto enum_retvalue;
			}
			if (sig->pinvoke)
				size = mono_class_native_size (klass, &align);
			else
				size = mono_class_value_size (klass, &align);
	
			cinfo->struct_ret = 1;
			cinfo->ret.size   = size;
			cinfo->ret.vtsize = size;
                        break;
		}
		case MONO_TYPE_TYPEDBYREF:
			size = sizeof (MonoTypedRef);
			cinfo->struct_ret = 1;
			cinfo->ret.size   = size;
			cinfo->ret.vtsize = size;
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
	      MONO_TYPE_IS_REFERENCE (mini_type_get_underlying_type (gsctx, sig->params [0]))))) {
		if (sig->hasthis) {
			cinfo->args[nParm].size = sizeof (gpointer);
			add_general (&gr, sz, cinfo->args + nParm);
		} else {
			cinfo->args[nParm].size = sizeof (gpointer);
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
			cinfo->args[nParm].size = sizeof (gpointer);
			add_general (&gr, sz, cinfo->args + nParm);
			nParm ++;
		}

		if (cinfo->struct_ret) {
			cinfo->ret.reg = gr;
			gr ++;
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

		ptype = mono_type_get_underlying_type (sig->params [i]);
		ptype = mini_get_basic_type_from_generic (gsctx, ptype);
		simpletype = ptype->type;
		cinfo->args[nParm].type = simpletype;
		switch (simpletype) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
			cinfo->args[nParm].size = sizeof(char);
			add_general (&gr, sz, cinfo->args+nParm);
			nParm++;
			break;
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_CHAR:
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
		case MONO_TYPE_CLASS:
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
			add_float (&fr, sz, cinfo->args+nParm);
			nParm++;
			break;
		case MONO_TYPE_R8:
			cinfo->args[nParm].size = sizeof(double);
			add_float (&fr, sz, cinfo->args+nParm);
			nParm++;
			break;
		case MONO_TYPE_GENERICINST:
			if (!mono_type_generic_inst_is_valuetype (sig->params [i])) {
				cinfo->args[nParm].size = sizeof(gpointer);
				add_general (&gr, sz, cinfo->args+nParm);
				nParm++;
				break;
			}
			/* Fall through */
		case MONO_TYPE_VALUETYPE: {
			MonoMarshalType *info;
			MonoClass *klass = mono_class_from_mono_type (sig->params [i]);
			if (sig->pinvoke)
				size = mono_class_native_size (klass, &align);
			else
				size = mono_class_value_size (klass, &align);
	
			info = mono_marshal_load_type_info (klass);

			if ((info->native_size == sizeof(float)) &&
			    (info->num_fields  == 1) &&
			    (info->fields[0].field->type->type == MONO_TYPE_R4)) {
				cinfo->args[nParm].size = sizeof(float);
				add_float(&fr, sz, cinfo->args+nParm);
				nParm ++;
				break;
			}

			if ((info->native_size == sizeof(double)) &&
			    (info->num_fields  == 1) &&
			    (info->fields[0].field->type->type == MONO_TYPE_R8)) {
				cinfo->args[nParm].size = sizeof(double);
				add_float(&fr, sz, cinfo->args+nParm);
				nParm ++;
				break;
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
					sz->local_size 		  += sizeof(long);
					break;
				default:
					add_stackParm(&gr, sz, cinfo->args+nParm, size);
					nParm++;
			}
		}
			break;
		case MONO_TYPE_TYPEDBYREF: {
			int size = sizeof (MonoTypedRef);

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
					sz->local_size 		  += sizeof(long);
					break;
				default:
					add_stackParm(&gr, sz, cinfo->args+nParm, size);
					nParm++;
			}
		}
			break;
		default:
			g_error ("Can't trampoline 0x%x", sig->params [i]->type);
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
	sz->stack_size  = sz->stack_size + sz->local_size + sz->parm_size + 
			  sz->offset;
	sz->stack_size  = S390_ALIGN(sz->stack_size, sizeof(long));

	return (cinfo);
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_allocate_vars                           */
/*                                                                  */
/* Function	- Set var information according to the calling      */
/*		  convention for S/390. The local var stuff should  */
/*		  most likely be split in another method.	    */
/*		                               			    */
/* Parameter    - @m - Compile unit.          			    */
/*		                               			    */
/*------------------------------------------------------------------*/

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
		cfg->used_int_regs |= 1 << frame_reg;		

	sig     = mono_method_signature (cfg->method);
	
	cinfo   = get_call_info (cfg, cfg->mempool, sig, sig->pinvoke);

	if (!cinfo->struct_ret) {
		switch (mono_type_get_underlying_type (sig->ret)->type) {
		case MONO_TYPE_VOID:
			break;
		default:
			cfg->ret->opcode = OP_REGVAR;
			cfg->ret->dreg   = s390_r2;
			break;
		}
	}

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

	if (cinfo->struct_ret) {
		inst 		   = cfg->vret_addr;
		offset 		   = S390_ALIGN(offset, sizeof(gpointer));
		inst->inst_offset  = offset;
		inst->opcode 	   = OP_REGOFFSET;
		inst->inst_basereg = frame_reg;
		offset 		  += sizeof(gpointer);
		if (G_UNLIKELY (cfg->verbose_level > 1)) {
			printf ("vret_addr =");
			mono_print_ins (cfg->vret_addr);
		}
	}

	if (sig->hasthis) {
		inst = cfg->args [0];
		if (inst->opcode != OP_REGVAR) {
			inst->opcode 	   = OP_REGOFFSET;
			inst->inst_basereg = frame_reg;
			offset 		   = S390_ALIGN(offset, sizeof(gpointer));
			inst->inst_offset  = offset;
			offset 		  += sizeof (gpointer);
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

				size = sizeof (gpointer);

				inst->opcode       = OP_REGOFFSET;
				inst->inst_basereg = frame_reg;
				offset             = S390_ALIGN (offset, sizeof (gpointer));
				inst->inst_offset  = offset;

				/* Add a level of indirection */
				MONO_INST_NEW (cfg, indir, 0);
				*indir          = *inst;
				inst->opcode    = OP_VTARG_ADDR;
				inst->inst_left = indir;
			}
				break;
			case RegTypeStructByAddrOnStack : {
				MonoInst *indir;

				size = sizeof (gpointer);

				/* Similar to the == STK_BASE case below */
				cfg->arch.bkchain_reg = s390_r12;
				cfg->used_int_regs |= 1 << cfg->arch.bkchain_reg;

				inst->opcode = OP_REGOFFSET;
				inst->dreg = mono_alloc_preg (cfg);
				inst->inst_basereg = cfg->arch.bkchain_reg;
				inst->inst_offset = cinfo->args [iParm].offset;

				/* Add a level of indirection */
				MONO_INST_NEW (cfg, indir, 0);
				*indir = *inst;
				inst->opcode = OP_VTARG_ADDR;
				inst->inst_left = indir;
				break;
			}
			case RegTypeStructByVal :
				size		   = cinfo->args[iParm].size;
				offset		   = S390_ALIGN(offset, size);
				inst->opcode       = OP_REGOFFSET;
				inst->inst_basereg = frame_reg;
				inst->inst_offset  = offset;
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
					size		   = (cinfo->args[iParm].size < 8
									  ? 8 - cinfo->args[iParm].size
									  : 0);
					inst->inst_offset  = cinfo->args [iParm].offset + size;
					size = sizeof (long);
				} else {
					inst->opcode 	   = OP_REGOFFSET;
					inst->inst_basereg = frame_reg;
					size		   = (cinfo->args[iParm].size < 8
									  ? sizeof(int)  
									  : sizeof(long));
					offset		   = S390_ALIGN(offset, size);
					if (cfg->method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE) 
						inst->inst_offset  = offset;
					else
						inst->inst_offset  = offset + (8 - size);
				}
				break;
			}
#if 0
			if ((sig->call_convention == MONO_CALL_VARARG) && 
			    (cinfo->args[iParm].regtype != RegTypeGeneral) &&
			    (iParm < sig->sentinelpos)) 
				cfg->sig_cookie += size;
printf("%s %4d cookine %x\n",__FUNCTION__,__LINE__,cfg->sig_cookie);
#endif

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
		/* value typs this is used by the pinvoke wrappers  */
		/* when they call functions returning structure     */
		/*--------------------------------------------------*/
		if (inst->backend.is_pinvoke && MONO_TYPE_ISSTRUCT (inst->inst_vtype))
			size = mono_class_native_size (mono_class_from_mono_type(inst->inst_vtype), 
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

	cfg->locals_max_stack_offset = offset;

	/*------------------------------------------------------*/
	/* Allow space for the trace method stack area if needed*/
	/*------------------------------------------------------*/
	if (mono_jit_trace_calls != NULL && mono_trace_eval (cfg->method)) 
		offset += S390_TRACE_STACK_SIZE;

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

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_create_vars                             */
/*                                                                  */
/*------------------------------------------------------------------*/

void
mono_arch_create_vars (MonoCompile *cfg)
{
	MonoMethodSignature *sig;
	CallInfo *cinfo;

	sig = mono_method_signature (cfg->method);

	cinfo = get_call_info (cfg, cfg->mempool, sig, sig->pinvoke);

	if (cinfo->struct_ret) {
		cfg->vret_addr = mono_compile_create_var (cfg, &mono_defaults.int_class->byval_arg, OP_ARG);
		if (G_UNLIKELY (cfg->verbose_level > 1)) {
			printf ("vret_addr = ");
			mono_print_ins (cfg->vret_addr);
		}
	}
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- add_outarg_reg2.                                  */
/*                                                                  */
/*------------------------------------------------------------------*/

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

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- emit_sig_cookie.                                  */
/*                                                                  */
/*------------------------------------------------------------------*/

static void
emit_sig_cookie (MonoCompile *cfg, MonoCallInst *call, CallInfo *cinfo)
{
	MonoMethodSignature *tmpSig;
	MonoInst *sig_arg;
			
	cfg->disable_aot = TRUE;

	/*----------------------------------------------------------*/
	/* mono_ArgIterator_Setup assumes the signature cookie is   */
	/* passed first and all the arguments which were before it  */
	/* passed on the stack after the signature. So compensate   */
	/* by passing a different signature.			    */
	/*----------------------------------------------------------*/
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

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_emit_call                               */
/*                                                                  */
/*------------------------------------------------------------------*/

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
	MonoMethodHeader *header;
	int frmReg;

	sig = call->signature;
	n = sig->param_count + sig->hasthis;
	DEBUG (g_print ("Call requires: %d parameters\n",n));
	
	cinfo = get_call_info (cfg, cfg->mempool, sig, sig->pinvoke);

	stackSize         = cinfo->sz.stack_size + cinfo->sz.local_size + 
			    cinfo->sz.parm_size + cinfo->sz.offset;
	call->stack_usage = MAX(stackSize, call->stack_usage);
	lParamArea        = MAX((call->stack_usage-S390_MINIMAL_STACK_SIZE-cinfo->sz.parm_size), 0);
	cfg->param_area   = MAX(((signed) cfg->param_area), lParamArea);
	cfg->flags       |= MONO_CFG_HAS_CALLS;

	if (cinfo->struct_ret) {
		MONO_INST_NEW (cfg, ins, OP_MOVE);
		ins->sreg1 = call->vret_var->dreg;
		ins->dreg = mono_alloc_preg (cfg);
		MONO_ADD_INS (cfg->cbb, ins);
		mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, cinfo->ret.reg, FALSE);
	}

	header = cfg->header;
	if ((cfg->flags & MONO_CFG_HAS_ALLOCA) || header->num_clauses)
		frmReg = s390_r11;
	else
		frmReg = STK_BASE;

	for (i = 0; i < n; ++i) {
		MonoType *t;

		ainfo = cinfo->args + i;
		if (i >= sig->hasthis)
			t = sig->params [i - sig->hasthis];
		else
			t = &mono_defaults.int_class->byval_arg;
		t = mono_type_get_underlying_type (t);

		in = call->args [i];

		if ((sig->call_convention == MONO_CALL_VARARG) &&
		    (!sig->pinvoke) &&
		    (i == sig->sentinelpos)) {
			emit_sig_cookie (cfg, call, cinfo);
		}

		switch (ainfo->regtype) {
		case RegTypeGeneral:
			add_outarg_reg2 (cfg, call, ainfo->regtype, ainfo->reg, in);
			break;
		case RegTypeFP:
			if (MONO_TYPE_ISSTRUCT (t)) {
				/* Valuetype passed in one fp register */
				ainfo->regtype = RegTypeStructByValInFP;
				/* Fall through */
			} else {
				if (ainfo->size == 4)
					ainfo->regtype = RegTypeFPR4;
				add_outarg_reg2 (cfg, call, ainfo->regtype, ainfo->reg, in);
				break;
			}
		case RegTypeStructByVal:
		case RegTypeStructByAddr:
		case RegTypeStructByAddrOnStack: {
			guint32 align;
			guint32 size;

			if (sig->params [i - sig->hasthis]->type == MONO_TYPE_TYPEDBYREF) {
				size = sizeof (MonoTypedRef);
				align = sizeof (gpointer);
			}
			else
				if (sig->pinvoke)
					size = mono_type_native_stack_size (&in->klass->byval_arg, &align);
				else {
					/* 
					 * Other backends use mono_type_stack_size (), but that
					 * aligns the size to 8, which is larger than the size of
					 * the source, leading to reads of invalid memory if the
					 * source is at the end of address space.
					 */
					size = mono_class_value_size (in->klass, &align);
				}

			g_assert (in->klass);

			ainfo->offparm += cinfo->sz.offStruct;

			MONO_INST_NEW (cfg, ins, OP_OUTARG_VT);
			ins->sreg1 = in->dreg;
			ins->klass = in->klass;
			ins->backend.size = ainfo->size;
			ins->inst_p0 = call;
			ins->inst_p1 = mono_mempool_alloc (cfg->mempool, sizeof (ArgInfo));
			memcpy (ins->inst_p1, ainfo, sizeof (ArgInfo));

			MONO_ADD_INS (cfg->cbb, ins);

			if (ainfo->regtype == RegTypeStructByAddr) {
				/* 
				 * We use OP_OUTARG_VT to copy the valuetype to a stack location, then
				 * use the normal OUTARG opcodes to pass the address of the location to
				 * the callee.
				 */
				int treg = mono_alloc_preg (cfg);
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ADD_IMM, treg, 
							 frmReg, ainfo->offparm);
				mono_call_inst_add_outarg_reg (cfg, call, treg, ainfo->reg, FALSE);
			} else if (ainfo->regtype == RegTypeStructByAddrOnStack) {
				/* The address of the valuetype is passed on the stack */
				int treg = mono_alloc_preg (cfg);
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ADD_IMM, treg, 
							 frmReg, ainfo->offparm);
				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG,
							     ainfo->reg, ainfo->offset, treg);

				if (cfg->compute_gc_maps) {
					MonoInst *def;

					EMIT_NEW_GC_PARAM_SLOT_LIVENESS_DEF (cfg, def, ainfo->offset, t);
				}
			}
			break;
		}
		case RegTypeBase:
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

#if 0
				/* This is needed by MonoTypedRef->value to point to the correct data */
				if ((sig->call_convention == MONO_CALL_VARARG) &&
					(i >= sig->sentinelpos)) {
					switch (ainfo->size) {
					case 1:
						ins->opcode = OP_STOREI1_MEMBASE_REG;
						break;
					case 2:
						ins->opcode = OP_STOREI2_MEMBASE_REG;
						break;
					case 4:
						ins->opcode = OP_STOREI4_MEMBASE_REG;
						break;
					default:
						break;
					}
				}
#endif

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

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_emit_outarg_vt                          */
/*                                                                  */
/*------------------------------------------------------------------*/

void
mono_arch_emit_outarg_vt (MonoCompile *cfg, MonoInst *ins, MonoInst *src)
{
	MonoCallInst *call = (MonoCallInst*)ins->inst_p0;
	ArgInfo *ainfo = (ArgInfo*)ins->inst_p1;
	int size = ins->backend.size;

	if (ainfo->regtype == RegTypeStructByVal) {
		/*
				arg->ins.sreg1  = ainfo->reg;
				arg->ins.opcode = OP_OUTARG_VT;
				arg->size       = ainfo->size;
				arg->offset     = ainfo->offset;
				arg->offPrm     = ainfo->offparm + cinfo->sz.offStruct;
		*/
		if (ainfo->reg != STK_BASE) {
			MONO_OUTPUT_VTR (cfg, size, ainfo->reg, src->dreg, 0);
		} else {
			MONO_OUTPUT_VTS (cfg, size, ainfo->reg, ainfo->offset,
							  src->dreg, 0);
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
		MonoMethodHeader *header;
		int srcReg;

		header = mono_method_get_header (cfg->method);
		if ((cfg->flags & MONO_CFG_HAS_ALLOCA) || header->num_clauses)
			srcReg = s390_r11;
		else
			srcReg = STK_BASE;

		MONO_EMIT_NEW_MOVE (cfg, srcReg, ainfo->offparm,
							 src->dreg, 0, size);

		if (cfg->compute_gc_maps) {
			MonoInst *def;

			EMIT_NEW_GC_PARAM_SLOT_LIVENESS_DEF (cfg, def, ainfo->offset, &ins->klass->byval_arg);
		}
	}
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_emit_setret                             */
/*                                                                  */
/*------------------------------------------------------------------*/

void
mono_arch_emit_setret (MonoCompile *cfg, MonoMethod *method, MonoInst *val)
{
	MonoType *ret = mono_type_get_underlying_type (mono_method_signature (method)->ret);

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

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_instrument_mem_needs                    */
/*                                                                  */
/* Function	- Allow tracing to work with this interface (with   */
/*		  an optional argument).       			    */
/*		                               			    */
/*------------------------------------------------------------------*/

void
mono_arch_instrument_mem_needs (MonoMethod *method, int *stack, int *code)
{
	/* no stack room needed now (may be needed for FASTCALL-trace support) */
	*stack = 0;
	/* split prolog-epilog requirements? */
	*code = 50; /* max bytes needed: check this number */
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_instrument_prolog                       */
/*                                                                  */
/* Function	- Create an "instrumented" prolog.                  */
/*		                               			    */
/*------------------------------------------------------------------*/

void*
mono_arch_instrument_prolog (MonoCompile *cfg, void *func, void *p, 
			     gboolean enable_arguments)
{
	guchar 	*code = p;
	int 	parmOffset, 
	    	fpOffset,
		baseReg;

	parmOffset = cfg->stack_usage - S390_TRACE_STACK_SIZE;
	if (cfg->method->save_lmf)
		parmOffset -= sizeof(MonoLMF);
	fpOffset   = parmOffset + (5*sizeof(gpointer));
	if ((!has_ld) && (fpOffset > 4096)) {
		s390_lgr (code, s390_r12, STK_BASE);
		baseReg = s390_r12;
		while (fpOffset > 4096) {
			s390_aghi (code, baseReg, 4096);
			fpOffset   -= 4096;
			parmOffset -= 4096;
		}
	} else {
		baseReg = STK_BASE;
	}	

	s390_stmg (code, s390_r2, s390_r6, STK_BASE, parmOffset);
	if (has_ld) {
		s390_stdy (code, s390_f0, 0, STK_BASE, fpOffset);
		s390_stdy (code, s390_f2, 0, STK_BASE, fpOffset+sizeof(gdouble));
		s390_stdy (code, s390_f4, 0, STK_BASE, fpOffset+2*sizeof(gdouble));
		s390_stdy (code, s390_f6, 0, STK_BASE, fpOffset+3*sizeof(gdouble));
	} else {
		s390_std  (code, s390_f0, 0, baseReg, fpOffset);
		s390_std  (code, s390_f2, 0, baseReg, fpOffset+sizeof(gdouble));
		s390_std  (code, s390_f4, 0, baseReg, fpOffset+2*sizeof(gdouble));
		s390_std  (code, s390_f6, 0, baseReg, fpOffset+3*sizeof(gdouble));
	}
	s390_basr (code, s390_r13, 0);
	s390_j    (code, 10);
	s390_llong(code, cfg->method);
	s390_llong(code, func);
	s390_lg   (code, s390_r2, 0, s390_r13, 4);
	if (has_ld)
		s390_lay  (code, s390_r3, 0, STK_BASE, parmOffset);
	else
		s390_la   (code, s390_r3, 0, baseReg, parmOffset);
	s390_lgr  (code, s390_r4, STK_BASE);
	s390_aghi (code, s390_r4, cfg->stack_usage);
	s390_lg   (code, s390_r1, 0, s390_r13, 12);
	s390_basr (code, s390_r14, s390_r1);
	if (has_ld) {
		s390_ldy  (code, s390_f6, 0, STK_BASE, fpOffset+3*sizeof(gdouble));
		s390_ldy  (code, s390_f4, 0, STK_BASE, fpOffset+2*sizeof(gdouble));
		s390_ldy  (code, s390_f2, 0, STK_BASE, fpOffset+sizeof(gdouble));
		s390_ldy  (code, s390_f0, 0, STK_BASE, fpOffset);
	} else {
		s390_ld   (code, s390_f6, 0, baseReg, fpOffset+3*sizeof(gdouble));
		s390_ld   (code, s390_f4, 0, baseReg, fpOffset+2*sizeof(gdouble));
		s390_ld   (code, s390_f2, 0, baseReg, fpOffset+sizeof(gdouble));
		s390_ld   (code, s390_f0, 0, baseReg, fpOffset);
	}
	s390_lmg  (code, s390_r2, s390_r6, STK_BASE, parmOffset);

	return code;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_instrument_epilog                       */
/*                                                                  */
/* Function	- Create an epilog that will handle the returned    */
/*		  values used in instrumentation.		    */
/*		                               			    */
/*------------------------------------------------------------------*/

void*
mono_arch_instrument_epilog_full (MonoCompile *cfg, void *func, void *p, gboolean enable_arguments, gboolean preserve_argument_registers)
{
	guchar 	   *code = p;
	int   	   save_mode = SAVE_NONE,
		   saveOffset;
	MonoMethod *method = cfg->method;
	int        rtype = mono_type_get_underlying_type (mono_method_signature (method)->ret)->type;

	saveOffset = cfg->stack_usage - S390_TRACE_STACK_SIZE;
	if (method->save_lmf)
		saveOffset -= sizeof(MonoLMF);

handle_enum:
	switch (rtype) {
	case MONO_TYPE_VOID:
		/* special case string .ctor icall */
		if (strcmp (".ctor", method->name) && method->klass == mono_defaults.string_class)
			save_mode = SAVE_ONE;
		else
			save_mode = SAVE_NONE;
		break;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		save_mode = SAVE_ONE;
		break;
	case MONO_TYPE_R4:
		save_mode = SAVE_R4;
		break;
	case MONO_TYPE_R8:
		save_mode = SAVE_R8;
		break;
	case MONO_TYPE_VALUETYPE:
		if (mono_method_signature (method)->ret->data.klass->enumtype) {
			rtype = mono_class_enum_basetype (mono_method_signature (method)->ret->data.klass)->type;
			goto handle_enum;
		}
		save_mode = SAVE_STRUCT;
		break;
	default:
		save_mode = SAVE_ONE;
		break;
	}

	switch (save_mode) {
	case SAVE_ONE:
		s390_stg (code, s390_r2, 0, cfg->frame_reg, saveOffset);
		if (enable_arguments) {
			s390_lgr (code, s390_r3, s390_r2);
		}
		break;
	case SAVE_R4:
		s390_std (code, s390_f0, 0, cfg->frame_reg, saveOffset);
		if (enable_arguments) {
			s390_ldebr (code, s390_f0, s390_f0);
		}
		break;
	case SAVE_R8:
		s390_std (code, s390_f0, 0, cfg->frame_reg, saveOffset);
		break;
	case SAVE_STRUCT:
		s390_stg (code, s390_r2, 0, cfg->frame_reg, saveOffset);
		if (enable_arguments) {
			s390_lg (code, s390_r3, 0, cfg->frame_reg, 
				 S390_MINIMAL_STACK_SIZE+cfg->param_area);
		}
		break;
	case SAVE_NONE:
	default:
		break;
	}

	s390_basr (code, s390_r13, 0);
	s390_j	  (code, 10);
	s390_llong(code, cfg->method);
	s390_llong(code, func);
	s390_lg   (code, s390_r2, 0, s390_r13, 4);
	s390_lg	  (code, s390_r1, 0, s390_r13, 12);
	s390_basr (code, s390_r14, s390_r1);

	switch (save_mode) {
	case SAVE_ONE:
		s390_lg  (code, s390_r2, 0, cfg->frame_reg, saveOffset);
		break;
	case SAVE_R4:
	case SAVE_R8:
		s390_ld  (code, s390_f0, 0, cfg->frame_reg, saveOffset);
		break;
	case SAVE_STRUCT:
		s390_lg  (code, s390_r2, 0, cfg->frame_reg, saveOffset);
		break;
	case SAVE_NONE:
	default:
		break;
	}

	return code;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_peephole_pass_1                         */
/*                                                                  */
/* Function	- Form a peephole pass at the code looking for      */
/*		  simple optimizations.        			    */
/*		                               			    */
/*------------------------------------------------------------------*/

void
mono_arch_peephole_pass_1 (MonoCompile *cfg, MonoBasicBlock *bb)
{
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_peephole_pass_2                         */
/*                                                                  */
/* Function	- Form a peephole pass at the code looking for      */
/*		  simple optimizations.        			    */
/*		                               			    */
/*------------------------------------------------------------------*/

void
mono_arch_peephole_pass_2 (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins, *n;

	MONO_BB_FOR_EACH_INS_SAFE (bb, n, ins) {
		mono_peephole_ins (bb, ins);
	}
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_lowering_pass.                          */
/*                                                                  */
/*------------------------------------------------------------------*/

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

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- emit_float_to_int                                 */
/*                                                                  */
/* Function	- Create instructions which will convert a floating */
/*		  point value to integer.      			    */
/*		                               			    */
/*------------------------------------------------------------------*/

static guchar*
emit_float_to_int (MonoCompile *cfg, guchar *code, int dreg, int sreg, int size, gboolean is_signed)
{
	/* sreg is a float, dreg is an integer reg. */
	if (is_signed) {
		s390_cgdbr (code, dreg, 5, sreg);
		switch (size) {
			case 1:
				s390_lghi (code, s390_r0, 0);
				s390_lghi (code, s390_r13, 0xff);
				s390_ltgr (code, dreg, dreg);
				s390_jnl  (code, 4);
				s390_lghi (code, s390_r0, 0x80);
				s390_ngr  (code, dreg, s390_r13);
				s390_ogr  (code, dreg, s390_r0);
				break;
		}
	} else {
		short *o[1];
		s390_basr   (code, s390_r13, 0);
		s390_j	    (code, 10);
		s390_llong  (code, 0x41e0000000000000llu);
		s390_llong  (code, 0x41f0000000000000llu);
		s390_ldr    (code, s390_f15, sreg);
		s390_cdb    (code, s390_f15, 0, s390_r13, 4);
		s390_jl     (code, 0); CODEPTR (code, o[0]);
		s390_sdb    (code, s390_f15, 0, s390_r13, 12);
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
				s390_lghi (code, s390_r0, -1);
				s390_srlg (code, s390_r0, s390_r0, 0, 16);
				s390_ngr  (code, dreg, s390_r0);
				break;
		}
	}
	return code;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- gboolean_is_unsigned.                             */
/*                                                                  */
/* Function	- Return TRUE if next opcode is checking for un-    */
/*		  signed value.					    */
/*		                               			    */
/*------------------------------------------------------------------*/

static 
gboolean is_unsigned (MonoInst *next)
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
		  (next->opcode == OP_CGT_UN)) ||
		 ((next->opcode == OP_ICLT_UN) ||
		  (next->opcode == OP_ICGT_UN) ||
		  (next->opcode == OP_LCLT_UN) ||
		  (next->opcode == OP_LCGT_UN))))
		return TRUE;
	else
		return FALSE;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_output_basic_block                      */
/*                                                                  */
/* Function	- Perform the "real" work of emitting instructions  */
/*		  that will do the work of in the basic block.      */
/*		                               			    */
/*------------------------------------------------------------------*/

void
mono_arch_output_basic_block (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins;
	MonoCallInst *call;
	guint offset;
	guint8 *code = cfg->native_code + cfg->code_len;
	guint last_offset = 0;
	int max_len, cpos, src2;

	/* we don't align basic blocks of loops on s390 */

	if (cfg->verbose_level > 2)
		g_print ("Basic block %d starting at offset 0x%x\n", bb->block_num, bb->native_offset);

	cpos = bb->max_offset;

	if (cfg->prof_options & MONO_PROFILE_COVERAGE) {
		//MonoCoverageInfo *cov = mono_get_coverage_info (cfg->method);
		//g_assert (!mono_compile_aot);
		//cpos += 6;
		//if (bb->cil_code)
		//	cov->data [bb->dfn].iloffset = bb->cil_code - cfg->cil_code;
		/* this is not thread save, but good enough */
		/* fixme: howto handle overflows? */
		//x86_inc_mem (code, &cov->data [bb->dfn].count); 
	}

	MONO_BB_FOR_EACH_INS (bb, ins) {
		offset = code - cfg->native_code;

		max_len = ((guint8 *)ins_get_spec (ins->opcode))[MONO_INST_LEN];

		if (offset > (cfg->code_size - max_len - 16)) {
			cfg->code_size *= 2;
			cfg->native_code = g_realloc (cfg->native_code, cfg->code_size);
			code = cfg->native_code + offset;
		}

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
			if (s390_is_imm16(ins->inst_imm)) {
				s390_lghi (code, s390_r0, ins->inst_imm);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 6);
				s390_llong(code, ins->inst_imm);
				s390_lg	  (code, s390_r0, 0, s390_r13, 4);
			}
			S390_LONG (code, sty, st, s390_r0, 0, 
				   ins->inst_destbasereg, ins->inst_offset);
		}
			break;
		case OP_STORE_MEMBASE_IMM:
		case OP_STOREI8_MEMBASE_IMM: {
			if (s390_is_imm16(ins->inst_imm)) {
				s390_lghi (code, s390_r0, ins->inst_imm);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 6);
				s390_llong(code, ins->inst_imm);
				s390_lg	  (code, s390_r0, 0, s390_r13, 4);
			}
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
#if 0
			s390_lgbr (code, ins->dreg, ins->sreg1);
#else
			s390_sllg (code, ins->dreg, ins->sreg1, 0, 56);
			s390_srag (code, ins->dreg, ins->dreg, 0, 56);
#endif
		}
			break;
		case OP_LCONV_TO_I2: {
#if 0
			s390_lghr (code, ins->dreg, ins->sreg1);
#else
			s390_sllg (code, ins->dreg, ins->sreg1, 0, 48);
			s390_srag (code, ins->dreg, ins->dreg, 0, 48);
#endif
		}
			break;
		case OP_LCONV_TO_U1: {
#if 0
			s390_llghr (code, ins->dreg, ins->sreg1);
#else
			if (ins->dreg != ins->sreg1)
				s390_lgr  (code, ins->dreg, ins->sreg1);
			s390_lghi  (code, s390_r0, 0xff);
			s390_ngr   (code, ins->dreg, s390_r0);
#endif
		}
			break;
		case OP_LCONV_TO_U2: {
#if 0
			s390_llghr (code, ins->dreg, ins->sreg1);
#else
			if (ins->dreg != ins->sreg1)
				s390_lgr  (code, ins->dreg, ins->sreg1);
			s390_lghi  (code, s390_r0, -1);
			s390_srlg  (code, s390_r0, s390_r0, 0, 48);
			s390_ngr   (code, ins->dreg, s390_r0);
#endif
		}
			break;
		case OP_ICONV_TO_I1: {
#if 0
			s390_lbr  (code, ins->dreg, ins->sreg1);
#else
			if (ins->dreg != ins->sreg1)
				s390_lr  (code, ins->dreg, ins->sreg1);
			s390_sll (code, ins->dreg, 0, 24);
			s390_sra (code, ins->dreg, 0, 24);
			
#endif
		}
			break;
		case OP_ICONV_TO_I2: {
#if 0
			s390_lhr  (code, ins->dreg, ins->sreg1);
#else
			if (ins->dreg != ins->sreg1)
				s390_lr  (code, ins->dreg, ins->sreg1);
			s390_sll (code, ins->dreg, 0, 16);
			s390_sra (code, ins->dreg, 0, 16);
#endif
		}
			break;
		case OP_ICONV_TO_U1: {
#if 0
			s390_llcr (code, ins->dreg, ins->sreg1);
#else
			if (ins->dreg != ins->sreg1)
				s390_lr  (code, ins->dreg, ins->sreg1);
			s390_lhi  (code, s390_r0, 0xff);
			s390_nr   (code, ins->dreg, s390_r0);
#endif
		}
			break;
		case OP_ICONV_TO_U2: {
#if 0
			s390_llhr (code, ins->dreg, ins->sreg1);
#else
			if (ins->dreg != ins->sreg1)
				s390_lr  (code, ins->dreg, ins->sreg1);
			s390_lhi  (code, s390_r0, -1);
			s390_srl  (code, s390_r0, 0, 16);
			s390_nr   (code, ins->dreg, s390_r0);
#endif
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
			if (s390_is_imm16 (ins->inst_imm)) {
				s390_lghi (code, s390_r0, ins->inst_imm);
				if (is_unsigned (ins->next))
					s390_clgr (code, ins->sreg1, s390_r0);
				else
					s390_cgr  (code, ins->sreg1, s390_r0);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j    (code, 6);
				s390_llong(code, ins->inst_imm);
				if (is_unsigned (ins->next))
					s390_clg  (code, ins->sreg1, 0, s390_r13, 4);
				else
					s390_cg	  (code, ins->sreg1, 0, s390_r13, 4);
			}
		}
			break;
		case OP_ICOMPARE_IMM: {
			if (s390_is_imm16 (ins->inst_imm)) {
				s390_lghi (code, s390_r0, ins->inst_imm);
				if (is_unsigned (ins->next))
					s390_clr  (code, ins->sreg1, s390_r0);
				else
					s390_cr   (code, ins->sreg1, s390_r0);
			}
			else {
				s390_basr (code, s390_r13, 0);
				s390_j    (code, 4);
				s390_word (code, ins->inst_imm);
				if (is_unsigned (ins->next))
					s390_cl  (code, ins->sreg1, 0, s390_r13, 4);
				else
					s390_c   (code, ins->sreg1, 0, s390_r13, 4);
			}
		}
			break;
		case OP_BREAK: {
			s390_basr  (code, s390_r13, 0);
			s390_j	   (code, 6);
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_ABS, 
					     mono_break);
			s390_llong (code, mono_break);
			s390_lg    (code, s390_r14, 0, s390_r13, 4);
                        s390_basr  (code, s390_r14, s390_r14);
		}
			break;
		case OP_ADDCC: {
			CHECK_SRCDST_COM;
			s390_agr  (code, ins->dreg, src2);
		}
			break;
		case OP_LADD: {
			CHECK_SRCDST_COM;
			s390_agr   (code, ins->dreg, src2);
		}
			break;
		case OP_ADC: {
			CHECK_SRCDST_COM;
			s390_alcgr (code, ins->dreg, src2);
		}
			break;
		case OP_ADD_IMM: {
			if (ins->dreg != ins->sreg1) {
				s390_lgr  (code, ins->dreg, ins->sreg1);
			}
			if (s390_is_imm16 (ins->inst_imm)) {
				s390_aghi (code, ins->dreg, ins->inst_imm);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 6);
				s390_llong(code, ins->inst_imm);
				s390_ag   (code, ins->dreg, 0, s390_r13, 4);
			}
		}
			break;
		case OP_LADD_IMM: {
			if (ins->dreg != ins->sreg1) {
				s390_lgr  (code, ins->dreg, ins->sreg1);
			}
			g_assert (s390_is_imm16 (ins->inst_imm));
			s390_aghi (code, ins->dreg, ins->inst_imm);
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
				s390_basr  (code, s390_r13, 0);
				s390_j	   (code, 6);
				s390_llong (code, ins->inst_imm);
				s390_lg    (code, s390_r13, 0, s390_r13, 4);
				s390_alcgr (code, ins->dreg, s390_r13);
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
			s390_algr  (code, ins->dreg, src2);
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
			CHECK_SRCDST_NCOM;
			s390_sgr (code, ins->dreg, src2);
		}
			break;
		case OP_LSUB: {
			CHECK_SRCDST_NCOM;
			s390_sgr  (code, ins->dreg, src2);
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
				s390_lghi  (code, s390_r0, ins->inst_imm);
				s390_slgr  (code, ins->dreg, s390_r0);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 6);
				s390_llong(code, ins->inst_imm);
				s390_slg  (code, ins->dreg, 0, s390_r13, 4);
			}
		}
			break;
		case OP_LSUB_IMM: {
			if (ins->dreg != ins->sreg1) {
				s390_lgr   (code, ins->dreg, ins->sreg1);
			}
			if (s390_is_imm16 (-ins->inst_imm)) {
				s390_lghi  (code, s390_r0, ins->inst_imm);
				s390_slgr  (code, ins->dreg, s390_r0);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 6);
				s390_llong(code, ins->inst_imm);
				s390_slg  (code, ins->dreg, 0, s390_r13, 4);
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
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 6);
				s390_llong(code, ins->inst_imm);
				s390_slbg (code, ins->dreg, 0, s390_r13, 4);
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
			if (ins->sreg1 == ins->dreg) {
				s390_ngr  (code, ins->dreg, ins->sreg2);
			} 
			else { 
				if (ins->sreg2 == ins->dreg) { 
					s390_ngr (code, ins->dreg, ins->sreg1);
				}
				else { 
					s390_lgr (code, ins->dreg, ins->sreg1);
					s390_ngr (code, ins->dreg, ins->sreg2);
				}
			}
		}
			break;
		case OP_AND_IMM: {
			if (ins->dreg != ins->sreg1) {
				s390_lgr  (code, ins->dreg, ins->sreg1);
			}
			if (s390_is_imm16 (ins->inst_imm)) {
				s390_lghi  (code, s390_r0, ins->inst_imm);
				s390_ngr  (code, ins->dreg, s390_r0);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 6);
				s390_llong(code, ins->inst_imm);
				s390_ng	  (code, ins->dreg, 0, s390_r13, 4);
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
		case OP_LREM_UN: {
			s390_lgr   (code, s390_r1, ins->sreg1);
			s390_lghi  (code, s390_r0, 0);
			s390_dlgr  (code, s390_r0, ins->sreg2);
			s390_lgr   (code, ins->dreg, s390_r0);
		}
			break;
		case OP_LOR: {
			if (ins->sreg1 == ins->dreg) {
				s390_ogr  (code, ins->dreg, ins->sreg2);
			} 
			else { 
				if (ins->sreg2 == ins->dreg) { 
					s390_ogr (code, ins->dreg, ins->sreg1);
				}
				else { 
					s390_lgr (code, ins->dreg, ins->sreg1);
					s390_ogr (code, ins->dreg, ins->sreg2);
				}
			}
		}
			break;
		case OP_OR_IMM: {
			if (ins->dreg != ins->sreg1) {
				s390_lgr  (code, ins->dreg, ins->sreg1);
			}
			if (s390_is_imm16 (ins->inst_imm)) {
				s390_lghi (code, s390_r0, ins->inst_imm);
				s390_ogr  (code, ins->dreg, s390_r0);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j    (code, 6);
				s390_llong(code, ins->inst_imm);
				s390_og	  (code, ins->dreg, 0, s390_r13, 4);
			}
		}
			break;
		case OP_LXOR: {
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
			break;
		case OP_XOR_IMM: {
			if (ins->dreg != ins->sreg1) {
				s390_lgr  (code, ins->dreg, ins->sreg1);
			}
			if (s390_is_imm16 (ins->inst_imm)) {
				s390_lghi  (code, s390_r0, ins->inst_imm);
				s390_xgr  (code, ins->dreg, s390_r0);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 6);
				s390_llong(code, ins->inst_imm);
				s390_xg	  (code, ins->dreg, 0, s390_r13, 4);
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
			if (s390_is_imm16 (ins->inst_imm)) {
				s390_lghi (code, s390_r13, ins->inst_imm);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 6);
				s390_llong(code, ins->inst_imm);
				s390_lg   (code, s390_r13, 0, s390_r13, 4);
			}
			s390_msgr (code, ins->dreg, s390_r13);
		}
			break;
		case OP_LMUL_OVF: {
			short int *o[2];
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
				s390_aghi(code, ins->dreg, ins->inst_imm);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 4);
				s390_word (code, ins->inst_imm);
				s390_agf  (code, ins->dreg, 0, s390_r13, 4);
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
				s390_basr  (code, s390_r13, 0);
				s390_j	   (code, 4);
				s390_word  (code, ins->inst_imm);
				s390_lgf   (code, s390_r13, 0, s390_r13, 4);
				s390_alcgr (code, ins->dreg, s390_r13);
			}
		}
			break;
		case OP_LADD_OVF:
		case OP_S390_LADD_OVF: {
			CHECK_SRCDST_COM;
			s390_agr    (code, ins->dreg, src2);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_OV, "OverflowException");
		}
			break;
		case OP_LADD_OVF_UN:
		case OP_S390_LADD_OVF_UN: {
			CHECK_SRCDST_COM;
			s390_algr  (code, ins->dreg, src2);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_CY, "OverflowException");
		}
			break;
		case OP_ISUBCC: {
			CHECK_SRCDST_NCOM_I;
			s390_slgr (code, ins->dreg, src2);
		}
			break;
		case OP_ISUB: {
			CHECK_SRCDST_NCOM_I;
			s390_sgr  (code, ins->dreg, src2);
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
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 4);
				s390_word (code, ins->inst_imm);
				s390_sgf  (code, ins->dreg, 0, s390_r13, 4);
			}
		}
			break;
		case OP_ISBB_IMM: {
			s390_basr (code, s390_r13, 0);
			s390_j	  (code, 4);
			s390_word (code, ins->inst_imm);
			s390_slgf (code, ins->dreg, 0, s390_r13, 4);
		}
			break;
		case OP_ISUB_OVF:
		case OP_S390_ISUB_OVF: {
			CHECK_SRCDST_NCOM;
			s390_sr   (code, ins->dreg, src2);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_OV, "OverflowException");
			s390_lgfr (code, ins->dreg, ins->dreg);
		}
			break;
		case OP_ISUB_OVF_UN:
		case OP_S390_ISUB_OVF_UN: {
			CHECK_SRCDST_NCOM;
			s390_slr  (code, ins->dreg, src2);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_NC, "OverflowException");
			s390_llgfr(code, ins->dreg, ins->dreg);
		}
			break;
		case OP_LSUB_OVF:
		case OP_S390_LSUB_OVF: {
			CHECK_SRCDST_NCOM;
			s390_sgr   (code, ins->dreg, src2);
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
			CHECK_SRCDST_NCOM_I;
			s390_ngr (code, ins->dreg, src2);
		}
			break;
		case OP_IAND_IMM: {
			if (ins->dreg != ins->sreg1) {
				s390_lgfr (code, ins->dreg, ins->sreg1);
			}
			if (s390_is_imm16 (ins->inst_imm)) {
				s390_lghi (code, s390_r0, ins->inst_imm);
				s390_ngr  (code, ins->dreg, s390_r0);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 6);
				s390_llong(code, ins->inst_imm);
				s390_ng	  (code, ins->dreg, 0, s390_r13, 4);
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
				s390_lgfr (code, s390_r0, ins->sreg1);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j    (code, 4);
				s390_word (code, ins->inst_imm);
				s390_lgfr (code, s390_r0, ins->sreg1);
				s390_lgf  (code, s390_r13, 0, s390_r13, 4);
			}
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
				s390_lgfr (code, s390_r0, ins->sreg1);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 4);
				s390_word (code, ins->inst_imm);
				s390_lgfr (code, s390_r0, ins->sreg1);
				s390_lgf  (code, s390_r13, 0, s390_r13, 4);
			}
			s390_srda (code, s390_r0, 0, 32);
			s390_dr   (code, s390_r0, ins->sreg2);
			s390_lgfr (code, ins->dreg, s390_r0);
		}
			break;
		case OP_IOR: {
			CHECK_SRCDST_COM_I;
			s390_ogr (code, ins->dreg, src2);
		}
			break;
		case OP_IOR_IMM: {
			if (ins->dreg != ins->sreg1) {
				s390_lgfr (code, ins->dreg, ins->sreg1);
			}
			if (s390_is_imm16 (ins->inst_imm)) {
				s390_lghi (code, s390_r0, ins->inst_imm);
				s390_ogr  (code, ins->dreg, s390_r0);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j    (code, 6);
				s390_llong(code, ins->inst_imm);
				s390_og	  (code, ins->dreg, 0, s390_r13, 4);
			}
		}
			break;
		case OP_IXOR: {
			CHECK_SRCDST_COM_I;
			s390_xgr (code, ins->dreg, src2);
		}
			break;
		case OP_IXOR_IMM: {
			if (ins->dreg != ins->sreg1) {
				s390_lgfr (code, ins->dreg, ins->sreg1);
			}
			if (s390_is_imm16 (ins->inst_imm)) {
				s390_lghi (code, s390_r0, ins->inst_imm);
				s390_xgr  (code, ins->dreg, s390_r0);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 6);
				s390_llong(code, ins->inst_imm);
				s390_xg	  (code, ins->dreg, 0, s390_r13, 4);
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
				s390_lghi (code, s390_r13, ins->inst_imm);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 4);
				s390_word (code, ins->inst_imm);
				s390_lgf  (code, s390_r13, 0, s390_r13, 4);
			}
			s390_msr  (code, ins->dreg, s390_r13);
		}
			break;
		case OP_IMUL_OVF: {
			short int *o[2];
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
			if (s390_is_imm16(ins->inst_c0)) {
				s390_lghi (code, ins->dreg, ins->inst_c0);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 6);
				s390_llong(code, ins->inst_c0);
				s390_lg   (code, ins->dreg, 0, s390_r13, 4);
			}
		}
			break;
		case OP_AOTCONST: {
			s390_basr (code, s390_r13, 0);
			s390_j	  (code, 6);
			mono_add_patch_info (cfg, code - cfg->native_code, 
				(MonoJumpInfoType)ins->inst_i1, ins->inst_p0);
			s390_llong(code, 0);
			s390_lg   (code,ins->dreg, 0, s390_r13, 4);
		}
			break;
		case OP_JUMP_TABLE: {
			mono_add_patch_info (cfg, code - cfg->native_code, 
				(MonoJumpInfoType)ins->inst_i1, ins->inst_p0);
			s390_basr  (code, s390_r13, 0);
			s390_j	   (code, 6);
			s390_llong (code, 0);
			s390_lg    (code, ins->dreg, 0, s390_r13, 4);
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
			s390_basr (code, s390_r13, 0);
			s390_j    (code, 6);
			s390_llong(code, 4294967295);
			s390_clg  (code, ins->sreg1, 0, s390_r13, 4);	
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_GT, "OverflowException");
			s390_ltgr (code, ins->sreg1, ins->sreg1);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_LT, "OverflowException");
			s390_llgfr(code, ins->dreg, ins->sreg1);
			break;
		case OP_LCONV_TO_OVF_I4_UN:
			s390_basr (code, s390_r13, 0);
			s390_j    (code, 6);
			s390_llong(code, 2147483647);
			s390_cg   (code, ins->sreg1, 0, s390_r13, 4);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_GT, "OverflowException");
			s390_ltgr (code, ins->sreg1, ins->sreg1);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_LT, "OverflowException");
			s390_lgfr (code, ins->dreg, ins->sreg1);
			break;
		case OP_FMOVE:
		case OP_FCONV_TO_R4: {
			if (ins->dreg != ins->sreg1) {
				s390_ldr   (code, ins->dreg, ins->sreg1);
			}
		}
			break;
		case OP_S390_SETF4RET: {
			s390_ledbr (code, ins->dreg, ins->sreg1);
		}
			break;
		case OP_TLS_GET: {
			if (s390_is_imm16 (ins->inst_offset)) {
				s390_lghi (code, s390_r13, ins->inst_offset);
			} else {
				s390_bras (code, s390_r13, 0);
				s390_j	  (code, 4);
				s390_llong(code, ins->inst_offset);
				s390_lg   (code, s390_r13, 0, s390_r13, 4);
			}
			s390_ear (code, s390_r1, 0);
			s390_sllg(code, s390_r1, s390_r1, 0, 32);
			s390_ear (code, s390_r1, 1);
			s390_lg  (code, ins->dreg, s390_r13, s390_r1, 0);
		}
			break;
		case OP_JMP: {
			if (cfg->method->save_lmf)
				restoreLMF(code, cfg->frame_reg, cfg->stack_usage);

			if (cfg->flags & MONO_CFG_HAS_TAIL) {
				code =  emit_load_volatile_arguments (code, cfg);
			}

			code = backUpStackPtr(cfg, code);
			s390_lg  (code, s390_r14, 0, cfg->frame_reg, S390_RET_ADDR_OFFSET);
			mono_add_patch_info (cfg, code - cfg->native_code,
					     MONO_PATCH_INFO_METHOD_JUMP,
					     ins->inst_p0);
			s390_jcl (code, S390_CC_UN, 0);
		}
			break;
		case OP_CHECK_THIS: {
			/* ensure ins->sreg1 is not NULL */
			s390_lg   (code, s390_r0, 0, ins->sreg1, 0);
			s390_ltgr (code, s390_r0, s390_r0);
		}
			break;
		case OP_ARGLIST: {
			int offset = cfg->sig_cookie + cfg->stack_usage;

			if (s390_is_imm16 (offset))
				s390_lghi (code, s390_r0, offset);
			else {
				s390_basr (code, s390_r13, 0);
				s390_j    (code, 6);
				s390_llong(code, offset);
				s390_lg   (code, s390_r0, 0, s390_r13, 0);
			}
			s390_agr  (code, s390_r0, cfg->frame_reg);
			s390_stg  (code, s390_r0, 0, ins->sreg1, 0);
		}
			break;
		case OP_FCALL: {
			s390_basr (code, s390_r13, 0);
			s390_j    (code, 6);
			call = (MonoCallInst*)ins;
			if (ins->flags & MONO_INST_HAS_METHOD)
				mono_add_patch_info (cfg, code-cfg->native_code,
						     MONO_PATCH_INFO_METHOD, 
						     call->method);
			else
				mono_add_patch_info (cfg, code-cfg->native_code,
						     MONO_PATCH_INFO_ABS, 
						     call->fptr);
			s390_llong(code, 0);
			s390_lg   (code, s390_r14, 0, s390_r13, 4);
			s390_basr (code, s390_r14, s390_r14);
			if (call->signature->ret->type == MONO_TYPE_R4)
				s390_ldebr (code, s390_f0, s390_f0);
		}
			break;
		case OP_LCALL:
		case OP_VCALL:
		case OP_VCALL2:
		case OP_VOIDCALL:
		case OP_CALL: {
			s390_basr (code, s390_r13, 0);
			s390_j    (code, 6);
			call = (MonoCallInst*)ins;
			if (ins->flags & MONO_INST_HAS_METHOD)
				mono_add_patch_info (cfg, code-cfg->native_code,
						     MONO_PATCH_INFO_METHOD, 
						     call->method);
			else
				mono_add_patch_info (cfg, code-cfg->native_code,
						     MONO_PATCH_INFO_ABS, 
						     call->fptr);
			s390_llong(code, 0);
			s390_lg   (code, s390_r14, 0, s390_r13, 4);
			s390_basr (code, s390_r14, s390_r14);
		}
			break;
		case OP_FCALL_REG: {
			call = (MonoCallInst*)ins;
			s390_lgr  (code, s390_r1, ins->sreg1);
			s390_basr (code, s390_r14, s390_r1);
			if (call->signature->ret->type == MONO_TYPE_R4)
				s390_ldebr (code, s390_f0, s390_f0);
		}
			break;
		case OP_LCALL_REG:
		case OP_VCALL_REG:
		case OP_VCALL2_REG:
		case OP_VOIDCALL_REG:
		case OP_CALL_REG: {
			s390_lgr  (code, s390_r1, ins->sreg1);
			s390_basr (code, s390_r14, s390_r1);
		}
			break;
		case OP_FCALL_MEMBASE: {
			call = (MonoCallInst*)ins;
			s390_lg   (code, s390_r1, 0, ins->sreg1, ins->inst_offset);
			s390_basr (code, s390_r14, s390_r1);
			if (call->signature->ret->type == MONO_TYPE_R4)
				s390_ldebr (code, s390_f0, s390_f0);
		}
			break;
		case OP_LCALL_MEMBASE:
		case OP_VCALL_MEMBASE:
		case OP_VCALL2_MEMBASE:
		case OP_VOIDCALL_MEMBASE:
		case OP_CALL_MEMBASE: {
			s390_lg   (code, s390_r1, 0, ins->sreg1, ins->inst_offset);
			s390_basr (code, s390_r14, s390_r1);
		}
			break;
		case OP_LOCALLOC: {
			int alloca_skip;
			int area_offset;

			if (cfg->param_area == 0)
				alloca_skip = S390_MINIMAL_STACK_SIZE;
			else
				alloca_skip = cfg->param_area;

			area_offset = S390_ALIGN(alloca_skip, S390_STACK_ALIGNMENT);
			s390_lgr  (code, s390_r1, ins->sreg1);
			if (ins->flags & MONO_INST_INIT)
				s390_lgr  (code, s390_r0, ins->sreg1);
			s390_aghi (code, s390_r1, 14);
			s390_srlg (code, s390_r1, s390_r1, 0, 3);
			s390_sllg (code, s390_r1, s390_r1, 0, 3);
			if (cfg->method->save_lmf) {
				/*----------------------------------*/
				/* we have to adjust lmf ebp value  */
				/*----------------------------------*/
				int lmfOffset = cfg->stack_usage - sizeof(MonoLMF);

				s390_lgr (code, s390_r13, cfg->frame_reg);
				if (s390_is_imm16(lmfOffset))
					s390_aghi (code, s390_r13, lmfOffset);
				else {
					s390_basr (code, s390_r14, 0);
					s390_j    (code, 4);
					s390_word (code, lmfOffset);
					s390_agf  (code, s390_r13, 0, s390_r14, 4);
				}
				s390_lgr (code, s390_r14, STK_BASE);
				s390_sgr (code, s390_r14, s390_r1);
				s390_stg (code, s390_r14, 0, s390_r13,
					  G_STRUCT_OFFSET(MonoLMF, ebp));
                        }
			s390_lg   (code, s390_r13, 0, STK_BASE, 0);
			s390_sgr  (code, STK_BASE, s390_r1);
			s390_stg  (code, s390_r13, 0, STK_BASE, 0);
			s390_la   (code, ins->dreg, 0, STK_BASE, area_offset);
			s390_srlg (code, ins->dreg, ins->dreg, 0, 3);
			s390_sllg (code, ins->dreg, ins->dreg, 0, 3);
			if (ins->flags & MONO_INST_INIT) {
				s390_lgr  (code, s390_r1, s390_r0);
				s390_lgr  (code, s390_r0, ins->dreg);
				s390_lgr  (code, s390_r14, s390_r12);
				s390_lghi (code, s390_r13, 0);
				s390_mvcle(code, s390_r0, s390_r12, 0, 0);
				s390_jo   (code, -2);
				s390_lgr  (code, s390_r12, s390_r14);
			}
		}
			break;
		case OP_THROW: {
			s390_lgr  (code, s390_r2, ins->sreg1);
			s390_basr (code, s390_r13, 0);
			s390_j    (code, 6);
			mono_add_patch_info (cfg, code-cfg->native_code, MONO_PATCH_INFO_INTERNAL_METHOD, 
					     (gpointer) "mono_arch_throw_exception");
			s390_llong(code, 0);
			s390_lg   (code, s390_r14, 0, s390_r13, 4);
			s390_basr (code, s390_r14, s390_r14);
		}
			break;
		case OP_RETHROW: {
			s390_lgr  (code, s390_r2, ins->sreg1);
			s390_basr (code, s390_r13, 0);
			s390_j    (code, 6);
			mono_add_patch_info (cfg, code-cfg->native_code, MONO_PATCH_INFO_INTERNAL_METHOD, 
					     (gpointer) "mono_arch_rethrow_exception");
			s390_llong(code, 0);
			s390_lg   (code, s390_r14, 0, s390_r13, 4);
			s390_basr (code, s390_r14, s390_r14);
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
			mono_cfg_add_try_hole (cfg, ins->inst_eh_block, code, bb);
		}
			break;
		case OP_LABEL: {
			ins->inst_c0 = code - cfg->native_code;
		}
			break;
		case OP_RELAXED_NOP:
		case OP_NOP:
		case OP_DUMMY_USE:
		case OP_DUMMY_STORE:
		case OP_NOT_REACHED:
		case OP_NOT_NULL: {
		}
			break;
		case OP_SEQ_POINT: {
			int i;

			if (cfg->compile_aot)
				NOT_IMPLEMENTED;

			/* 
			 * Read from the single stepping trigger page. This will cause a
			 * SIGSEGV when single stepping is enabled.
			 * We do this _before_ the breakpoint, so single stepping after
			 * a breakpoint is hit will step to the next IL offset.
			 */
			if (ins->flags & MONO_INST_SINGLE_STEP_LOC) {
				breakpointCode.pTrigger = ss_trigger_page;
				memcpy(code, (void *) &breakpointCode, BREAKPOINT_SIZE);
				code += BREAKPOINT_SIZE;
			}

			mono_add_seq_point (cfg, bb, ins, code - cfg->native_code);

			/* 
			 * A placeholder for a possible breakpoint inserted by
			 * mono_arch_set_breakpoint ().
			 */
			for (i = 0; i < (BREAKPOINT_SIZE / S390X_NOP_SIZE); ++i)
				s390_nop (code);
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

		/* floating point opcodes */
		case OP_R8CONST: {
			if (*((double *) ins->inst_p0) == 0) {
				s390_lzdr (code, ins->dreg);
			} else {
				s390_basr  (code, s390_r13, 0);
				s390_j	   (code, 6);
				s390_llong (code, ins->inst_p0);
				s390_lg    (code, s390_r13, 0, s390_r13, 4);
				s390_ld    (code, ins->dreg, 0, s390_r13, 0);
			}
		}
			break;
		case OP_R4CONST: {
			if (*((float *) ins->inst_p0) == 0) {
				s390_lzdr (code, ins->dreg);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 6);
				s390_llong(code, ins->inst_p0);
				s390_lg   (code, s390_r13, 0, s390_r13, 4);
				s390_ldeb (code, ins->dreg, 0, s390_r13, 0);
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
			s390_ledbr (code, s390_f15, ins->sreg1);
			S390_LONG (code, stey, ste, s390_f15, 0, 
				   ins->inst_destbasereg, ins->inst_offset);
		}
			break;
		case OP_LOADR4_MEMBASE: {
			S390_LONG (code, ldy, ld, s390_f15, 0, 
				   ins->inst_basereg, ins->inst_offset);
			s390_ldebr (code, ins->dreg, s390_f15);
		}
			break;
		case OP_ICONV_TO_R_UN: {
			s390_cdfbr (code, ins->dreg, ins->sreg1);
			s390_ltr   (code, ins->sreg1, ins->sreg1);
			s390_jnl   (code, 12);
			s390_basr  (code, s390_r13, 0);
			s390_j	   (code, 6);
			s390_word  (code, 0x41f00000);
			s390_word  (code, 0);
			s390_adb   (code, ins->dreg, 0, s390_r13, 4);
		}
			break;
		case OP_LCONV_TO_R_UN: {
			s390_cdgbr (code, ins->dreg, ins->sreg1);
			s390_ltgr  (code, ins->sreg1, ins->sreg1);
			s390_jnl   (code, 12);
			s390_basr  (code, s390_r13, 0);
			s390_j	   (code, 6);
			s390_word  (code, 0x41f00000);
			s390_word  (code, 0);
			s390_adb   (code, ins->dreg, 0, s390_r13, 4);
		}
			break;
		case OP_LCONV_TO_R4:
		case OP_ICONV_TO_R4: {
			s390_cdgbr (code, ins->dreg, ins->sreg1);
		}
			break;
		case OP_LCONV_TO_R8:
		case OP_ICONV_TO_R8: {
			s390_cdgbr (code, ins->dreg, ins->sreg1);
		}
			break;
		case OP_FCONV_TO_I1:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 1, TRUE);
			break;
		case OP_FCONV_TO_U1:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 1, FALSE);
			break;
		case OP_FCONV_TO_I2:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 2, TRUE);
			break;
		case OP_FCONV_TO_U2:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 2, FALSE);
			break;
		case OP_FCONV_TO_I4:
		case OP_FCONV_TO_I:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 4, TRUE);
			break;
		case OP_FCONV_TO_U4:
		case OP_FCONV_TO_U:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 4, FALSE);
			break;
		case OP_FCONV_TO_I8:
			s390_cgdbr (code, ins->dreg, 5, ins->sreg1);
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
		case OP_FSUB: {
			CHECK_SRCDST_NCOM_F;
			s390_sdbr (code, ins->dreg, src2);
		}
			break;		
		case OP_FMUL: {
			CHECK_SRCDST_COM_F;
			s390_mdbr (code, ins->dreg, src2);
		}
			break;		
		case OP_FDIV: {
			CHECK_SRCDST_NCOM_F;
			s390_ddbr (code, ins->dreg, src2);
		}
			break;		
		case OP_FNEG: {
			s390_lcdbr (code, ins->dreg, ins->sreg1);
		}
			break;		
		case OP_FREM: {
			CHECK_SRCDST_NCOM_F;
			s390_didbr (code, ins->dreg, src2, 5, s390_f15);
		}
			break;
		case OP_FCOMPARE: {
			s390_cdbr (code, ins->sreg1, ins->sreg2);
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
					     MONO_PATCH_INFO_EXC, "ArithmeticException");
			s390_brasl (code, s390_r14,0);
			PTRSLOT(code, o);
		}
			break;
		case OP_S390_MOVE: {
			if (ins->backend.size > 0) {
				if (ins->backend.size <= 256) {
					s390_mvc  (code, ins->backend.size, ins->dreg, 
						   ins->inst_offset, ins->sreg1, ins->inst_imm);
				} else {
					s390_lgr  (code, s390_r0, ins->dreg);
					if (ins->inst_offset > 0) {
						if (s390_is_imm16 (ins->inst_offset)) {
							s390_aghi (code, s390_r0, ins->inst_offset);
						} else {
							s390_basr (code, s390_r13, 0);
							s390_j    (code, 6);
							s390_llong(code, ins->inst_offset);
							s390_ag   (code, s390_r0, 0, s390_r13, 4);
						}
					}
					s390_lgr  (code, s390_r12, ins->sreg1);
					if (ins->inst_imm > 0) {
						if (s390_is_imm16 (ins->inst_imm)) {
							s390_aghi (code, s390_r12, ins->inst_imm);
						} else {
							s390_basr (code, s390_r13, 0);
							s390_j    (code, 6);
							s390_llong(code, ins->inst_imm);
							s390_ag   (code, s390_r12, 0, s390_r13, 4);
						}
					}
					if (s390_is_imm16 (ins->backend.size)) {
						s390_lghi (code, s390_r1, ins->backend.size);
					} else {
						s390_basr (code, s390_r13, 0);
						s390_j    (code, 6);
						s390_llong(code, ins->backend.size);
						s390_lg   (code, s390_r1, 0, s390_r13, 4);
					}
					s390_lgr  (code, s390_r13, s390_r1);
					s390_mvcle(code, s390_r0, s390_r12, 0, 0);
					s390_jo   (code, -2);
				}
			}
		}
			break;
		case OP_ATOMIC_ADD_I8: {
			s390_lgr (code, s390_r1, ins->sreg2);
			s390_lg  (code, s390_r0, 0, ins->inst_basereg, ins->inst_offset);
			s390_agr (code, s390_r1, s390_r0);
			s390_csg (code, s390_r0, s390_r1, ins->inst_basereg, ins->inst_offset);
			s390_jnz (code, -10);
			s390_lgr (code, ins->dreg, s390_r1);
		}
			break;	
		case OP_ATOMIC_ADD_NEW_I8: {
			s390_lgr (code, s390_r1, ins->sreg2);
			s390_lg  (code, s390_r0, 0, ins->inst_basereg, ins->inst_offset);
			s390_agr (code, s390_r1, s390_r0);
			s390_csg (code, s390_r0, s390_r1, ins->inst_basereg, ins->inst_offset);
			s390_jnz (code, -10);
			s390_lgr (code, ins->dreg, s390_r1);
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
			s390_lgfr(code, s390_r1, ins->sreg2);
			s390_lgf (code, s390_r0, 0, ins->inst_basereg, ins->inst_offset);
			s390_agr (code, s390_r1, s390_r0);
			s390_cs  (code, s390_r0, s390_r1, ins->inst_basereg, ins->inst_offset);
			s390_jnz (code, -9);
			s390_lgfr(code, ins->dreg, s390_r1);
		}
			break;	
		case OP_ATOMIC_ADD_NEW_I4: {
			s390_lgfr(code, s390_r1, ins->sreg2);
			s390_lgf (code, s390_r0, 0, ins->inst_basereg, ins->inst_offset);
			s390_agr (code, s390_r1, s390_r0);
			s390_cs  (code, s390_r0, s390_r1, ins->inst_basereg, ins->inst_offset);
			s390_jnz (code, -9);
			s390_lgfr(code, ins->dreg, s390_r1);
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
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j    (code, 6);
				s390_llong(code, cfg->stack_offset);
				s390_ag   (code, ins->dreg, 0, s390_r13, 4);
			}
		}
			break;	
		case OP_MEMORY_BARRIER: {
		}
			break;
		case OP_GC_LIVENESS_DEF:
		case OP_GC_LIVENESS_USE:
		case OP_GC_PARAM_SLOT_LIVENESS_DEF:
			ins->backend.pc_offset = code - cfg->native_code;
			break;
		case OP_GC_SPILL_SLOT_LIVENESS_DEF:
			ins->backend.pc_offset = code - cfg->native_code;
			bb->spill_slot_defs = g_slist_prepend_mempool (cfg->mempool, bb->spill_slot_defs, ins);
			break;
		default:
			g_warning ("unknown opcode %s in %s()\n", mono_inst_name (ins->opcode), __FUNCTION__);
			g_assert_not_reached ();
		}

		if ((cfg->opt & MONO_OPT_BRANCH) && ((code - cfg->native_code - offset) > max_len)) {
			g_warning ("wrong maximal instruction length of instruction %s (expected %d, got %ld)",
				   mono_inst_name (ins->opcode), max_len, code - cfg->native_code - offset);
			g_assert_not_reached ();
		}
	       
		cpos += max_len;

		last_offset = offset;
	}

	cfg->code_len = code - cfg->native_code;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_register_lowlevel_calls                 */
/*                                                                  */
/* Function	- Register routines to help with --trace operation. */
/*		                               			    */
/*------------------------------------------------------------------*/

void
mono_arch_register_lowlevel_calls (void)
{
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_patch_code                              */
/*                                                                  */
/* Function	- Process the patch data created during the         */
/*		  instruction build process. This resolves jumps,   */
/*		  calls, variables etc.        			    */
/*		                               			    */
/*------------------------------------------------------------------*/

void
mono_arch_patch_code (MonoMethod *method, MonoDomain *domain, 
		      guint8 *code, MonoJumpInfo *ji, MonoCodeManager *dyn_code_mp, gboolean run_cctors)
{
	MonoJumpInfo *patch_info;

	for (patch_info = ji; patch_info; patch_info = patch_info->next) {
		unsigned char *ip = patch_info->ip.i + code;
		gconstpointer target = NULL;

		target = mono_resolve_patch_target (method, domain, code, 
						    patch_info, run_cctors);

		switch (patch_info->type) {
			case MONO_PATCH_INFO_IP:
			case MONO_PATCH_INFO_EXC_NAME:
			case MONO_PATCH_INFO_LDSTR:
			case MONO_PATCH_INFO_TYPE_FROM_HANDLE: 
			case MONO_PATCH_INFO_LDTOKEN: 
			case MONO_PATCH_INFO_EXC:
			case MONO_PATCH_INFO_ABS:
			case MONO_PATCH_INFO_METHOD:
			case MONO_PATCH_INFO_INTERNAL_METHOD:
			case MONO_PATCH_INFO_CLASS_INIT:
				s390_patch_addr (ip, (guint64) target);
				continue;
			case MONO_PATCH_INFO_SWITCH: 
				/*----------------------------------*/
				/* ip points at the basr r13,0/j +4 */
				/* instruction the vtable value     */
				/* follows this (i.e. ip+6)	    */
				/*----------------------------------*/
				*((gconstpointer *)(ip+6)) = target;
				continue;
			case MONO_PATCH_INFO_METHODCONST:
			case MONO_PATCH_INFO_CLASS:
			case MONO_PATCH_INFO_IMAGE:
			case MONO_PATCH_INFO_FIELD:
			case MONO_PATCH_INFO_IID:
				target = S390_RELATIVE(target, ip);
				s390_patch_rel (ip, (guint64) target);
				continue;
			case MONO_PATCH_INFO_R4:
			case MONO_PATCH_INFO_R8:
			case MONO_PATCH_INFO_METHOD_REL:
				g_assert_not_reached ();
				continue;
			default:
				target = S390_RELATIVE(target, ip);
				ip += 2;
				s390_patch_rel (ip, (guint64) target);
		}
	}
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name         - emit_load_volatile_arguments                      */
/*                                                                  */
/* Function     - Emit the instructions to reload parameter regist- */
/*                registers for use with "tail" operations.         */
/*                                                                  */
/*                The register loading operations performed here    */
/*                are the mirror of the store operations performed  */
/*                in mono_arch_emit_prolog and need to be kept in   */
/*                synchronization with it.                          */
/*                                                                  */
/*------------------------------------------------------------------*/

guint8 *
emit_load_volatile_arguments (guint8 *code, MonoCompile *cfg)
{
	MonoInst *inst;
	MonoMethod *method = cfg->method;
	MonoMethodSignature *sig = mono_method_signature(method);
	int pos = 0, i;
	CallInfo *cinfo;

	cinfo = get_call_info (NULL, NULL, sig, sig->pinvoke);

	if (cinfo->struct_ret) {
		ArgInfo *ainfo = &cinfo->ret;
		inst         = cfg->vret_addr;
		s390_lg (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
	}

	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		ArgInfo *ainfo = cinfo->args + i;
		inst = cfg->args [pos];

		if (inst->opcode == OP_REGVAR) {
			if (ainfo->regtype == RegTypeGeneral)
				s390_lgr (code, ainfo->reg, inst->dreg);
			else if (ainfo->regtype == RegTypeFP) {
				if (inst->dreg != ainfo->reg) {
					if (ainfo->size == 4) {
						s390_ldebr (code, ainfo->reg, inst->dreg);
					} else {
						s390_ldr   (code, ainfo->reg, inst->dreg);
					}
				}
			}
			else if (ainfo->regtype == RegTypeBase) {
			} else
				g_assert_not_reached ();
		} else {
			if (ainfo->regtype == RegTypeGeneral) {
				if (!((ainfo->reg >= 2) && (ainfo->reg <= 6)))
					g_assert_not_reached();
				switch (ainfo->size) {
				case 1:
					s390_llgc (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
					break;
				case 2:
					s390_lgh  (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
					break;
				case 4: 
					s390_lgf (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
					break;
				case 8:
					s390_lg  (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
					break;
				}
			} else if (ainfo->regtype == RegTypeBase) {
			} else if (ainfo->regtype == RegTypeFP) {
				if (ainfo->size == 8)
					s390_ld  (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
				else if (ainfo->size == 4)
					s390_le  (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
				else
					g_assert_not_reached ();
			} else if (ainfo->regtype == RegTypeStructByVal) {
				if (ainfo->reg != STK_BASE) {
					switch (ainfo->size) {
					case 1:
						s390_llgc (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
						break;
					case 2:
						s390_lgh (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
						break;
					case 4:
						s390_lgf (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
						break;
					case 8:
						s390_lg  (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
						break;
					}
				}
			} else if (ainfo->regtype == RegTypeStructByAddr) {
				if (ainfo->reg != STK_BASE) {
					s390_lg (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
				}
			} else
				g_assert_not_reached ();
		}
		pos++;
	}

	return code;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_emit_prolog                             */
/*                                                                  */
/* Function	- Create the instruction sequence for a function    */
/*		  prolog.                      			    */
/*		                               			    */
/*------------------------------------------------------------------*/

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
	int tracing = 0;
	int lmfOffset;

	cfg->code_size   = 512;

	if (mono_jit_trace_calls != NULL && mono_trace_eval (method)) {
		tracing         = 1;
		cfg->code_size += 256;
	}

	if (method->save_lmf)
		cfg->code_size += 200;

	cfg->native_code = code = g_malloc (cfg->code_size);

	mono_emit_unwind_op_def_cfa (cfg, code, STK_BASE, 0);
	emit_unwind_regs(cfg, code, s390_r6, s390_r14, S390_REG_SAVE_OFFSET);
	s390_stmg (code, s390_r6, s390_r14, STK_BASE, S390_REG_SAVE_OFFSET);
	mono_emit_unwind_op_offset (cfg, code, s390_r14, S390_RET_ADDR_OFFSET);

	if (cfg->arch.bkchain_reg != -1)
		s390_lgr (code, cfg->arch.bkchain_reg, STK_BASE);

	if (cfg->flags & MONO_CFG_HAS_ALLOCA) {
		cfg->used_int_regs |= 1 << 11;
	}

	alloc_size = cfg->stack_offset;

	cfg->stack_usage = cfa_offset = alloc_size;
	mono_emit_unwind_op_def_cfa_offset (cfg, code, alloc_size);
	s390_lgr  (code, s390_r11, STK_BASE);
	if (s390_is_imm16 (alloc_size)) {
		s390_aghi (code, STK_BASE, -alloc_size);
	} else { 
		int stackSize = alloc_size;
		while (stackSize > 32767) {
			s390_aghi (code, STK_BASE, -32767);
			stackSize -= 32767;
		}
		s390_aghi (code, STK_BASE, -stackSize);
	}
	s390_stg  (code, s390_r11, 0, STK_BASE, 0);

	if (cfg->frame_reg != STK_BASE)
		s390_lgr (code, s390_r11, STK_BASE);

	mono_emit_unwind_op_def_cfa_reg (cfg, code, cfg->frame_reg);

        /* compute max_offset in order to use short forward jumps
	 * we always do it on s390 because the immediate displacement
	 * for jumps is too small 
	 */
	max_offset = 0;
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoInst *ins;
		bb->max_offset = max_offset;

		if (cfg->prof_options & MONO_PROFILE_COVERAGE)
			max_offset += 6; 

		MONO_BB_FOR_EACH_INS (bb, ins)
			max_offset += ((guint8 *)ins_get_spec (ins->opcode))[MONO_INST_LEN];
	}

	/* load arguments allocated to register from the stack */
	sig = mono_method_signature (method);
	pos = 0;

	cinfo = get_call_info (cfg, cfg->mempool, sig, sig->pinvoke);

	if (cinfo->struct_ret) {
		ArgInfo *ainfo     = &cinfo->ret;
		inst               = cfg->vret_addr;
		inst->backend.size = ainfo->vtsize;
		s390_stg (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
	}

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
					if (ainfo->size == 4) {
						s390_ledbr (code, inst->dreg, ainfo->reg);
					} else {
						s390_ldr   (code, inst->dreg, ainfo->reg);
					}
				}
			}
			else if (ainfo->regtype == RegTypeBase) {
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
				if (ainfo->size == 8)
					s390_std (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
				else if (ainfo->size == 4)
					s390_ste (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
				else
					g_assert_not_reached ();
			} else if (ainfo->regtype == RegTypeStructByVal) {
				int doffset = inst->inst_offset;
				int reg;
				if (ainfo->reg != STK_BASE)
					reg = ainfo->reg;
				else {
					reg = s390_r0;
					s390_lgr  (code, s390_r13, STK_BASE);
					s390_aghi (code, s390_r13, alloc_size);
				}

				size = (method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE  
					? mono_class_native_size(mono_class_from_mono_type(inst->inst_vtype), NULL)
					: ainfo->size);

				switch (size) {
					case 1:
						if (ainfo->reg == STK_BASE)
				                	s390_ic (code, reg, 0, s390_r13, ainfo->offset+7);
						s390_stc (code, reg, 0, inst->inst_basereg, doffset);
						break;
					case 2:
						if (ainfo->reg == STK_BASE)
				                	s390_lh (code, reg, 0, s390_r13, ainfo->offset+6);
						s390_sth (code, reg, 0, inst->inst_basereg, doffset);
						break;
					case 4:
						if (ainfo->reg == STK_BASE)
				                	s390_l  (code, reg, 0, s390_r13, ainfo->offset+4);
						s390_st (code, reg, 0, inst->inst_basereg, doffset);
						break;
					case 8:
						if (ainfo->reg == STK_BASE)
				                	s390_lg  (code, reg, 0, s390_r13, ainfo->offset);
						s390_stg (code, reg, 0, inst->inst_basereg, doffset);
						break;
				}
			} else if (ainfo->regtype == RegTypeStructByAddr) {
				s390_stg (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
			} else if (ainfo->regtype == RegTypeStructByAddrOnStack) {
			} else
				g_assert_not_reached ();
		}
		pos++;
	}

	if (method->save_lmf) {
		/*---------------------------------------------------------------*/
		/* build the MonoLMF structure on the stack - see mini-s390x.h   */
		/*---------------------------------------------------------------*/
		lmfOffset = alloc_size - sizeof(MonoLMF);	
											
		s390_lgr   (code, s390_r13, cfg->frame_reg);		
		s390_aghi  (code, s390_r13, lmfOffset);					
											
		/*---------------------------------------------------------------*/
		/* Preserve the parameter registers while we fix up the lmf	 */
		/*---------------------------------------------------------------*/
		s390_stmg  (code, s390_r2, s390_r6, s390_r13,
			    G_STRUCT_OFFSET(MonoLMF, pregs[0]));

		/*---------------------------------------------------------------*/
		/* On return from this call r2 have the address of the &lmf	 */
		/*---------------------------------------------------------------*/
		if (lmf_addr_tls_offset == -1) {
			s390_basr(code, s390_r14, 0);
			s390_j   (code, 6);
			mono_add_patch_info (cfg, code - cfg->native_code, 
					     MONO_PATCH_INFO_INTERNAL_METHOD, 
					     (gpointer)"mono_get_lmf_addr");
			s390_llong(code, 0);
			s390_lg   (code, s390_r1, 0, s390_r14, 4);
			s390_basr (code, s390_r14, s390_r1);
		} else {
			/*-------------------------------------------------------*/
			/* Get LMF by getting value from thread level storage    */
			/*-------------------------------------------------------*/
			s390_ear (code, s390_r1, 0);
			s390_sllg(code, s390_r1, s390_r1, 0, 32);
			s390_ear (code, s390_r1, 1);
			s390_lg  (code, s390_r2, 0, s390_r1, lmf_addr_tls_offset);
		}

		/*---------------------------------------------------------------*/	
		/* Set lmf.lmf_addr = jit_tls->lmf				 */	
		/*---------------------------------------------------------------*/	
		s390_stg   (code, s390_r2, 0, s390_r13, 				
			    G_STRUCT_OFFSET(MonoLMF, lmf_addr));			
											
		/*---------------------------------------------------------------*/	
		/* Get current lmf						 */	
		/*---------------------------------------------------------------*/	
		s390_lg    (code, s390_r0, 0, s390_r2, 0);				
											
		/*---------------------------------------------------------------*/	
		/* Set our lmf as the current lmf				 */	
		/*---------------------------------------------------------------*/	
		s390_stg   (code, s390_r13, 0, s390_r2, 0);				
											
		/*---------------------------------------------------------------*/	
		/* Have our lmf.previous_lmf point to the last lmf		 */	
		/*---------------------------------------------------------------*/	
		s390_stg   (code, s390_r0, 0, s390_r13, 				
			    G_STRUCT_OFFSET(MonoLMF, previous_lmf));			
											
		/*---------------------------------------------------------------*/	
		/* save method info						 */	
		/*---------------------------------------------------------------*/	
		s390_basr  (code, s390_r1, 0);						
		s390_j	   (code, 6);
		s390_llong (code, method);						
		s390_lg    (code, s390_r1, 0, s390_r1, 4);			
		s390_stg   (code, s390_r1, 0, s390_r13, 				
			    G_STRUCT_OFFSET(MonoLMF, method));				
										
		/*---------------------------------------------------------------*/	
		/* save the current IP						 */	
		/*---------------------------------------------------------------*/	
		s390_stg   (code, STK_BASE, 0, s390_r13, G_STRUCT_OFFSET(MonoLMF, ebp));
		s390_basr  (code, s390_r1, 0);
		s390_stg   (code, s390_r1, 0, s390_r13, G_STRUCT_OFFSET(MonoLMF, eip));	
											
		/*---------------------------------------------------------------*/	
		/* Save general and floating point registers			 */	
		/*---------------------------------------------------------------*/	
		s390_stmg  (code, s390_r2, s390_r12, s390_r13, 				
			    G_STRUCT_OFFSET(MonoLMF, gregs[2]));			
		for (i = 0; i < 16; i++) {						
			s390_std  (code, i, 0, s390_r13, 				
				   G_STRUCT_OFFSET(MonoLMF, fregs[i]));			
		}									

		/*---------------------------------------------------------------*/
		/* Restore the parameter registers now that we've set up the lmf */
		/*---------------------------------------------------------------*/
		s390_lmg   (code, s390_r2, s390_r6, s390_r13, 				
			    G_STRUCT_OFFSET(MonoLMF, pregs[0]));			
	}

	if (tracing)
		code = mono_arch_instrument_prolog(cfg, enter_method, code, TRUE);

	cfg->code_len = code - cfg->native_code;
	g_assert (cfg->code_len < cfg->code_size);

	return code;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_emit_epilog                             */
/*                                                                  */
/* Function	- Emit the instructions for a function epilog.      */
/*		                               			    */
/*------------------------------------------------------------------*/

void
mono_arch_emit_epilog (MonoCompile *cfg)
{
	MonoMethod *method = cfg->method;
	int tracing = 0;
	guint8 *code;
	int max_epilog_size = 96;
	
	if (cfg->method->save_lmf)
		max_epilog_size += 128;
	
	if (mono_jit_trace_calls != NULL)
		max_epilog_size += 128;

	if (cfg->prof_options & MONO_PROFILE_ENTER_LEAVE)
		max_epilog_size += 128;
	
	while ((cfg->code_len + max_epilog_size) > (cfg->code_size - 16)) {
		cfg->code_size  *= 2;
		cfg->native_code = g_realloc (cfg->native_code, cfg->code_size);
		cfg->stat_code_reallocs++;
	}

	code = cfg->native_code + cfg->code_len;

	if (mono_jit_trace_calls != NULL && mono_trace_eval (method)) {
		code = mono_arch_instrument_epilog (cfg, leave_method, code, TRUE);
		tracing = 1;
	}
	
	if (method->save_lmf) 
		restoreLMF(code, cfg->frame_reg, cfg->stack_usage);

	if (cfg->flags & MONO_CFG_HAS_ALLOCA) {
//		if (cfg->frame_reg != STK_BASE)
//			s390_lgr (code, STK_BASE, cfg->frame_reg);
		s390_lg  (code, STK_BASE, 0, STK_BASE, 0);
	} else
		code = backUpStackPtr(cfg, code);

	s390_lmg (code, s390_r6, s390_r14, STK_BASE, S390_REG_SAVE_OFFSET);
	s390_br  (code, s390_r14);

	cfg->code_len = code - cfg->native_code;

	g_assert (cfg->code_len < cfg->code_size);

}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_emit_exceptions                         */
/*                                                                  */
/* Function	- Emit the blocks to handle exception conditions.   */
/*		                               			    */
/*------------------------------------------------------------------*/

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

	while ((cfg->code_len + code_size) > (cfg->code_size - 16)) {
		cfg->code_size  *= 2;
		cfg->native_code = g_realloc (cfg->native_code, cfg->code_size);
		cfg->stat_code_reallocs++; 
	}

	code = cfg->native_code + cfg->code_len;

	/*---------------------------------------------------------------------*/
	/* Add code to raise exceptions 				       */
	/*---------------------------------------------------------------------*/
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		switch (patch_info->type) {
		case MONO_PATCH_INFO_EXC: {
			guint8 *ip = patch_info->ip.i + cfg->native_code;
			MonoClass *exc_class;
			guint64 throw_ip;

			/*-----------------------------------------------------*/
			/* Patch the branch in epilog to come here	       */
			/*-----------------------------------------------------*/
			s390_patch_rel (ip + 2, (guint64) S390_RELATIVE(code,ip));

			exc_class = mono_class_from_name (mono_defaults.corlib, 
							  "System", 
							  patch_info->data.name);
			g_assert (exc_class);
			throw_ip = patch_info->ip.i;

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
	
				/*---------------------------------------------*/
				/* Patch the parameter passed to the handler   */ 
				/*---------------------------------------------*/
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 6);
//				s390_llong(code, patch_info->data.target);
				s390_llong(code, exc_class->type_token);
				/*---------------------------------------------*/
				/* Load return address & parameter register    */
				/*---------------------------------------------*/
				s390_larl (code, s390_r14, (guint64)S390_RELATIVE((patch_info->ip.i +
							   cfg->native_code + 8), code));
				s390_lg   (code, s390_r2, 0, s390_r13, 4);
				/*---------------------------------------------*/
				/* Reuse the current patch to set the jump     */
				/*---------------------------------------------*/
				s390_basr (code, s390_r13, 0);
				s390_j    (code, 6);
				patch_info->type      = MONO_PATCH_INFO_INTERNAL_METHOD;
				patch_info->data.name = "mono_arch_throw_corlib_exception";
				patch_info->ip.i      = code - cfg->native_code;
				s390_llong(code, 0);
				s390_lg   (code, s390_r1, 0, s390_r13, 4);
				s390_br   (code, s390_r1);
			}
			break;
		}
		default:
			/* do nothing */
			break;
		}
	}

	cfg->code_len = code - cfg->native_code;

	g_assert (cfg->code_len < cfg->code_size);

}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_finish_init                                 */
/*                                                                  */
/* Function	- Setup the JIT's Thread Level Specific Data.       */
/*		                               			    */
/*------------------------------------------------------------------*/

void
mono_arch_finish_init (void)
{
	appdomain_tls_offset = mono_domain_get_tls_offset();
	lmf_tls_offset = mono_get_lmf_tls_offset();
	lmf_addr_tls_offset = mono_get_lmf_addr_tls_offset();
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_free_jit_tls_data                       */
/*                                                                  */
/* Function	- Free tls data.                                    */
/*		                               			    */
/*------------------------------------------------------------------*/

void
mono_arch_free_jit_tls_data (MonoJitTlsData *tls)
{
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_emit_inst_for_method                        */
/*                                                                  */
/*------------------------------------------------------------------*/

MonoInst*
mono_arch_emit_inst_for_method (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	return NULL;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_decompose_opts                          */
/*                                                                  */
/* Function	- Decompose opcode into a System z opcode.          */
/*		                               			    */
/*------------------------------------------------------------------*/

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

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_print_tree                              */
/*                                                                  */
/* Function	- Print platform-specific opcode details.           */
/*		                               			    */
/* Returns	- 1 - opcode details have been printed		    */
/*		  0 - opcode details have not been printed	    */
/*                                                                  */
/*------------------------------------------------------------------*/

gboolean
mono_arch_print_tree (MonoInst *tree, int arity)
{
	gboolean done;

	switch (tree->opcode) {
		case OP_S390_LOADARG:
		case OP_S390_ARGREG:
		case OP_S390_ARGPTR:
			printf ("[0x%lx(%s)]", tree->inst_offset, 
				mono_arch_regname (tree->inst_basereg));
			done = 1;
			break;
		case OP_S390_STKARG:
			printf ("[0x%lx(previous_frame)]", 
				tree->inst_offset); 
			done = 1;
			break;
		case OP_S390_MOVE:
			printf ("[0x%lx(%d,%s),0x%lx(%s)]",
				tree->inst_offset, tree->backend.size,
				mono_arch_regname(tree->dreg), 
				tree->inst_imm, 
				mono_arch_regname(tree->sreg1));
			done = 1;
			break;
		case OP_S390_SETF4RET:
			printf ("[f%s,f%s]", 
				mono_arch_regname (tree->dreg),
				mono_arch_regname (tree->sreg1));
			done = 1;
			break;
		case OP_TLS_GET:
			printf ("[0x%lx(0x%lx,%s)]", tree->inst_offset,
			        tree->inst_imm,
				mono_arch_regname (tree->sreg1));
			done = 1;
			break;
		case OP_S390_BKCHAIN:
			printf ("[previous_frame(%s)]", 
				mono_arch_regname (tree->sreg1));
			done = 1;
		default:
			done = 0;
	}
	return (done);
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_regalloc_cost                           */
/*                                                                  */
/* Function	- Determine the cost, in the number of memory       */
/*		  references, of the action of allocating the var-  */
/*		  iable VMV into a register during global register  */
/*		  allocation.					    */
/*		                               			    */
/* Returns	- Cost						    */
/*                                                                  */
/*------------------------------------------------------------------*/

guint32
mono_arch_regalloc_cost (MonoCompile *cfg, MonoMethodVar *vmv)
{
	/* FIXME: */
	return 2;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_get_domain_intrinsic                    */
/*                                                                  */
/* Function	- 						    */
/*		                               			    */
/* Returns	-     						    */
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
/* Name		- mono_arch_flush_register_windows                  */
/*                                                                  */
/* Function	- 						    */
/*		                               			    */
/* Returns	-     						    */
/*                                                                  */
/*------------------------------------------------------------------*/

void 
mono_arch_flush_register_windows (void)
{
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_is_inst_imm                             */
/*                                                                  */
/* Function	- Determine if operand qualifies as an immediate    */
/*		  value. For s390 this is a value -32768-32768      */
/*		                               			    */
/* Returns	- True|False - is [not] immediate value.	    */
/*                                                                  */
/*------------------------------------------------------------------*/

gboolean 
mono_arch_is_inst_imm (gint64 imm)
{
	return s390_is_imm16 (imm);
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_get_patch_offset                        */
/*                                                                  */
/* Function	- Dummy entry point until s390x supports aot.       */
/*		                               			    */
/* Returns	- Offset for patch.				    */
/*                                                                  */
/*------------------------------------------------------------------*/

guint32
mono_arch_get_patch_offset (guint8 *code)
{
	return 0;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_context_get_int_reg.                    */
/*                                                                  */
/* Function	- 						    */
/*		                               			    */
/* Returns	- Return a register from the context.		    */
/*                                                                  */
/*------------------------------------------------------------------*/

mgreg_t
mono_arch_context_get_int_reg (MonoContext *ctx, int reg)
{
	return ((mgreg_t) ctx->uc_mcontext.gregs[reg]);
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_context_set_int_reg.                    */
/*                                                                  */
/* Function	- Set a value in a specified register.              */
/*		                               			    */
/*------------------------------------------------------------------*/

void
mono_arch_context_set_int_reg (MonoContext *ctx, int reg, mgreg_t val)
{
	ctx->uc_mcontext.gregs[reg] = val;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_get_this_arg_from_call.                 */
/*                                                                  */
/* Function	- 						    */
/*		                               			    */
/*------------------------------------------------------------------*/

gpointer
mono_arch_get_this_arg_from_call (mgreg_t *regs, guint8 *code)
{
	MonoLMF *lmf = (MonoLMF *) ((gchar *) regs - sizeof(MonoLMF));

	return (gpointer) lmf->gregs [s390_r2];
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- get_delegate_invoke_impl.                         */
/*                                                                  */
/* Function	- 						    */
/*		                               			    */
/*------------------------------------------------------------------*/

static gpointer
get_delegate_invoke_impl (gboolean has_target, guint32 param_count, guint32 *code_len, gboolean aot)
{
	guint8 *code, *start;

	if (has_target) {
		int size = 32;

		start = code = mono_global_codeman_reserve (size);

		/* Replace the this argument with the target */
		s390_lg   (code, s390_r1, 0, s390_r2, G_STRUCT_OFFSET(MonoDelegate, method_ptr));
		s390_lg   (code, s390_r2, 0, s390_r2, G_STRUCT_OFFSET(MonoDelegate, target));
		s390_br   (code, s390_r1);
		g_assert ((code - start) <= size);

		mono_arch_flush_icache (start, size);
	} else {
		int size, i;

		size = 32 + param_count * 8;
		start = code = mono_global_codeman_reserve (size);

		s390_lg   (code, s390_r1, 0, s390_r2, G_STRUCT_OFFSET(MonoDelegate, method_ptr));
		/* slide down the arguments */
		for (i = 0; i < param_count; ++i) {
			s390_lgr (code, (s390_r2 + i), (s390_r2 + i + 1));
		}
		s390_br   (code, s390_r1);

		g_assert ((code - start) <= size);

		mono_arch_flush_icache (start, size);
	}

	if (code_len)
		*code_len = code - start;

	return start;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_get_delegate_invoke_impls.              */
/*                                                                  */
/* Function	- 						    */
/*		                               			    */
/*------------------------------------------------------------------*/

GSList*
mono_arch_get_delegate_invoke_impls (void)
{
	GSList *res = NULL;
	guint8 *code;
	guint32 code_len;
	int i;

	code = get_delegate_invoke_impl (TRUE, 0, &code_len, TRUE);
	res = g_slist_prepend (res, mono_tramp_info_create (g_strdup ("delegate_invoke_impl_has_target"), code, code_len, NULL, NULL));

	for (i = 0; i < MAX_ARCH_DELEGATE_PARAMS; ++i) {
		code = get_delegate_invoke_impl (FALSE, i, &code_len, TRUE);
		res = g_slist_prepend (res, mono_tramp_info_create (g_strdup_printf ("delegate_invoke_impl_target_%d", i), code, code_len, NULL, NULL));
	}

	return res;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_get_delegate_invoke_impl.               */
/*                                                                  */
/* Function	- 						    */
/*		                               			    */
/*------------------------------------------------------------------*/

gpointer
mono_arch_get_delegate_invoke_impl (MonoMethodSignature *sig, gboolean has_target)
{
	guint8 *code, *start;

	/* FIXME: Support more cases */
	if (MONO_TYPE_ISSTRUCT (sig->ret))
		return NULL;

	if (has_target) {
		static guint8* cached = NULL;

		if (cached)
			return cached;

		if (mono_aot_only)
			start = mono_aot_get_trampoline ("delegate_invoke_impl_has_target");
		else
			start = get_delegate_invoke_impl (TRUE, 0, NULL, FALSE);

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

		if (mono_aot_only) {
			char *name = g_strdup_printf ("delegate_invoke_impl_target_%d", sig->param_count);
			start = mono_aot_get_trampoline (name);
			g_free (name);
		} else {
			start = get_delegate_invoke_impl (FALSE, sig->param_count, NULL, FALSE);
		}

		mono_memory_barrier ();

		cache [sig->param_count] = start;
	}
	return start;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_build_imt_thunk.                        */
/*                                                                  */
/* Function	- 						    */
/*		                               			    */
/*------------------------------------------------------------------*/

gpointer
mono_arch_build_imt_thunk (MonoVTable *vtable, MonoDomain *domain, 
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

	if (fail_tramp)
		code = mono_method_alloc_generic_virtual_thunk (domain, size);
	else
		code = mono_domain_code_reserve (domain, size);

	start = code;

	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		item->code_target = (guint8 *) code;
		if (item->is_equals) {
			if (item->check_target_idx) {
				if (!item->compare_done) {
					s390_basr (code, s390_r13, s390_r0);
					s390_j	  (code, 6);
					s390_llong(code, item->key);
					s390_lg	  (code, s390_r0, 0, s390_r13, 4);
					s390_cgr  (code, s390_r0, MONO_ARCH_IMT_REG);
				}
				item->jmp_code = (guint8*) code;
				s390_jcl (code, S390_CC_NE, 0);
				
				s390_basr (code, s390_r13, s390_r0);
				s390_j	  (code, 6);
				if (item->has_target_code)  {
					s390_llong(code, item->value.target_code);
					s390_lg	  (code, s390_r1, 0, s390_r13, 4);
				} else {	
					s390_llong(code, (&(vtable->vtable [item->value.vtable_slot])));
					s390_lg	  (code, s390_r1, 0, s390_r13, 4);
					s390_lg	  (code, s390_r1, 0, s390_r1, 0);
				}
				s390_br	  (code, s390_r1);
			} else {
				if (fail_tramp) {
					gint64  target;

					s390_basr (code, s390_r13, s390_r0);
					s390_j	  (code, 6);
					s390_llong(code, item->key);
					s390_lg	  (code, s390_r0, 0, s390_r13, 4);
					s390_cgr  (code, s390_r0, MONO_ARCH_IMT_REG);
					item->jmp_code = (guint8*) code;
					s390_jcl  (code, S390_CC_NE, 0);
					s390_basr (code, s390_r13, s390_r0);
					s390_j	  (code, 6);
					if (item->has_target_code) {
						s390_llong(code, item->value.target_code);
						s390_lg	  (code, s390_r1, 0, s390_r13, 4);
					} else {
						g_assert (vtable);
						s390_llong(code, (&(vtable->vtable [item->value.vtable_slot])));
						s390_lg	  (code, s390_r1, 0, s390_r13, 4);
						s390_lg	  (code, s390_r1, 0, s390_r1, 0);
					}
					s390_br	  (code, s390_r1);
					target = S390_RELATIVE(code, item->jmp_code);
					s390_patch_rel(item->jmp_code+2, target);
					s390_basr (code, s390_r13, s390_r0);
					s390_j	  (code, 6);
					s390_llong(code, fail_tramp);
					s390_lg	  (code, s390_r1, 0, s390_r13, 4);
					s390_br	  (code, s390_r1);
					item->jmp_code = NULL;
				} else {
				/* enable the commented code to assert on wrong method */
#if ENABLE_WRONG_METHOD_CHECK
					g_assert_not_reached ();
#endif
					s390_basr (code, s390_r13, s390_r0);
					s390_j	  (code, 6);
					s390_llong(code, (&(vtable->vtable [item->value.vtable_slot])));
					s390_lg	  (code, s390_r1, 0, s390_r13, 4);
					s390_lg	  (code, s390_r1, 0, s390_r1, 0);
					s390_br	  (code, s390_r1);
#if ENABLE_WRONG_METHOD_CHECK
					g_assert_not_reached ();
#endif
				}
			}
		} else {
			s390_basr (code, s390_r13, s390_r0);
			s390_j	  (code, 6);
			s390_llong(code, item->key);
			s390_lg	  (code, s390_r0, 0, s390_r13, 4);
			s390_cgr  (code, MONO_ARCH_IMT_REG, s390_r0);
			item->jmp_code = (guint8 *) code;
			s390_jcl  (code, S390_CC_GE, 0);
		}
	}
	/* patch the branches to get to the target items */
	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		if (item->jmp_code) {
			if (item->check_target_idx) {
				gint64 offset;
				offset = S390_RELATIVE(imt_entries [item->check_target_idx]->code_target,
						       item->jmp_code);
				s390_patch_rel ((guchar *) item->jmp_code + 2, (guint64) offset);
			}
		}
	}

	mono_arch_flush_icache ((guint8*)start, (code - start));

	if (!fail_tramp)
		mono_stats.imt_thunks_size += (code - start);

	g_assert (code - start <= size);

	return (start);
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_find_imt_method.                        */
/*                                                                  */
/* Function	- Get the method address from MONO_ARCH_IMT_REG     */
/*		  found in the save area.      			    */
/*		                               			    */
/*------------------------------------------------------------------*/

MonoMethod*
mono_arch_find_imt_method (mgreg_t *regs, guint8 *code)
{
	MonoLMF *lmf = (MonoLMF *) ((gchar *) regs - sizeof(MonoLMF));

	return ((MonoMethod *) lmf->gregs [MONO_ARCH_IMT_REG]);
}

/*========================= End of Function ========================*/

#ifdef MONO_ARCH_SOFT_DEBUG_SUPPORTED

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_set_breakpoint.                         */
/*                                                                  */
/* Function	- Set a breakpoint at the native code corresponding */
/*		  to JI at NATIVE_OFFSET.  The location should 	    */
/*		  contain code emitted by OP_SEQ_POINT.		    */
/*		                               			    */
/*------------------------------------------------------------------*/

void
mono_arch_set_breakpoint (MonoJitInfo *ji, guint8 *ip)
{
	guint8 *code = ip;

	breakpointCode.pTrigger = bp_trigger_page;
	memcpy(code, (void *) &breakpointCode, BREAKPOINT_SIZE);
	code += BREAKPOINT_SIZE;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_clear_breakpoint.                       */
/*                                                                  */
/* Function	- Clear the breakpoint at IP.			    */
/*		                               			    */
/*------------------------------------------------------------------*/

void
mono_arch_clear_breakpoint (MonoJitInfo *ji, guint8 *ip)
{
	guint8 *code = ip;
	int i;

	for (i = 0; i < (BREAKPOINT_SIZE / S390X_NOP_SIZE); i++)
		s390_nop(code);
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_is_breakpoint_event.                    */
/*                                                                  */
/* Function	- 						    */
/*		                               			    */
/*------------------------------------------------------------------*/

gboolean
mono_arch_is_breakpoint_event (void *info, void *sigctx)
{
	siginfo_t* sinfo = (siginfo_t*) info;
	/* Sometimes the address is off by 4 */
	if (sinfo->si_addr >= bp_trigger_page && (guint8*)sinfo->si_addr <= (guint8*)bp_trigger_page + 128)
		return TRUE;
	else
		return FALSE;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_skip_breakpoint.                        */
/*                                                                  */
/* Function	- Modify the CTX so the IP is placed after the 	    */
/*                breakpoint instruction, so when we resume, the    */
/*		  instruction is not executed again.		    */
/*		                               			    */
/*------------------------------------------------------------------*/

void
mono_arch_skip_breakpoint (MonoContext *ctx, MonoJitInfo *ji)
{
	MONO_CONTEXT_SET_IP (ctx, (guint8*)MONO_CONTEXT_GET_IP (ctx) + BREAKPOINT_SIZE);
}

/*========================= End of Function ========================*/
	
/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_start_single_stepping.                  */
/*                                                                  */
/* Function	- Start single stepping.			    */
/*		                               			    */
/*------------------------------------------------------------------*/

void
mono_arch_start_single_stepping (void)
{
	mono_mprotect (ss_trigger_page, mono_pagesize (), 0);
}

/*========================= End of Function ========================*/
	
/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_stop_single_stepping.                   */
/*                                                                  */
/* Function	- Stop single stepping.			   	    */
/*		                               			    */
/*------------------------------------------------------------------*/

void
mono_arch_stop_single_stepping (void)
{
	mono_mprotect (ss_trigger_page, mono_pagesize (), MONO_MMAP_READ);
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_is_single_step_event.                   */
/*                                                                  */
/* Function	- Return whether the machine state in sigctx cor-   */
/*		  responds to a single step event.		    */
/*		                               			    */
/*------------------------------------------------------------------*/

gboolean
mono_arch_is_single_step_event (void *info, void *sigctx)
{
	siginfo_t* sinfo = (siginfo_t*) info;

	/* Sometimes the address is off by 4 */
	if (sinfo->si_addr >= ss_trigger_page && (guint8*)sinfo->si_addr <= (guint8*)ss_trigger_page + 128)
		return TRUE;
	else
		return FALSE;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_skip_single_step.                       */
/*                                                                  */
/* Function	- Modify the ctx so the IP is placed after the      */
/*		  single step trigger instruction, so that the 	    */
/*		  instruction is not executed again.		    */
/*		                               			    */
/*------------------------------------------------------------------*/

void
mono_arch_skip_single_step (MonoContext *ctx)
{
	MONO_CONTEXT_SET_IP (ctx, (guint8*)MONO_CONTEXT_GET_IP (ctx) + BREAKPOINT_SIZE);
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_create_seq_point_info.                  */
/*                                                                  */
/* Function	- Return a pointer to a data struction which is     */
/*		  used by the sequence point implementation in      */
/*		  AOTed code.                       	 	    */
/*		                               			    */
/*------------------------------------------------------------------*/

gpointer
mono_arch_get_seq_point_info (MonoDomain *domain, guint8 *code)
{
	NOT_IMPLEMENTED;
	return NULL;
}

/*========================= End of Function ========================*/

#endif
