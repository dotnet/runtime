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

#define NOT_IMPLEMENTED(x) \
        g_error ("FIXME: %s is not yet implemented.", x);

#define EMIT_COND_BRANCH(ins,cond) 							\
{											\
if (ins->flags & MONO_INST_BRLABEL) { 							\
        if (ins->inst_i0->inst_c0) { 							\
		int displace;								\
		displace = ((cfg->native_code + ins->inst_i0->inst_c0) - code) / 2;	\
		if (s390_is_imm16(displace)) {						\
			s390_brc (code, cond, displace);				\
		} else { 								\
			s390_jcl (code, cond, displace); 				\
		}									\
        } else { 									\
	        mono_add_patch_info (cfg, code - cfg->native_code, 			\
				     MONO_PATCH_INFO_LABEL, ins->inst_i0); 		\
		s390_jcl (code, cond, 0);						\
        } 										\
} else { 										\
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
} 											\
}

#define EMIT_UNCOND_BRANCH(ins) 							\
{											\
if (ins->flags & MONO_INST_BRLABEL) { 							\
        if (ins->inst_i0->inst_c0) { 							\
		int displace;								\
		displace = ((cfg->native_code + ins->inst_i0->inst_c0) - code) / 2;	\
		if (s390_is_imm16(displace)) {						\
			s390_brc (code, S390_CC_UN, displace);				\
		} else { 								\
			s390_jcl (code, S390_CC_UN, displace); 				\
		}									\
        } else { 									\
	        mono_add_patch_info (cfg, code - cfg->native_code, 			\
				     MONO_PATCH_INFO_LABEL, ins->inst_i0); 		\
		s390_jcl (code, S390_CC_UN, 0);						\
        } 										\
} else { 										\
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
}											\
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
			s390_lr  (code, ins->dreg, ins->sreg1);		\
		}							\
	}

#define CHECK_SRCDST_NCOM						\
	if (ins->dreg == ins->sreg2) {					\
		src2 = s390_r13;					\
		s390_lr  (code, s390_r13, ins->sreg2);			\
	} else {							\
		src2 = ins->sreg2;					\
	}								\
	if (ins->dreg != ins->sreg1) {					\
		s390_lr  (code, ins->dreg, ins->sreg1);			\
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

#undef DEBUG
#define DEBUG(a) if (cfg->verbose_level > 1) a

#define MAX_EXC	16

#define S390_TRACE_STACK_SIZE (5*sizeof(gint32)+3*sizeof(gdouble))

#define MAX (a, b) ((a) > (b) ? (a) : (b))

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

#include "mini-s390.h"
#include "inssel.h"
#include "cpu-s390.h"

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

enum {
	RegTypeGeneral,
	RegTypeBase,
	RegTypeFP,
	RegTypeStructByVal,
	RegTypeStructByAddr
};

typedef struct {
	gint32  offset;		/* offset from caller's stack */
	gint32  offparm;	/* offset from callee's stack */
	guint16 vtsize; 	/* in param area */
	guint8  reg;
	guint8  regtype;     	/* See RegType* */
	guint32 size;        	/* Size of structure used by RegTypeStructByVal */
} ArgInfo;

typedef struct {
	int nargs;
	guint32 stack_usage;
	guint32 struct_ret;
	ArgInfo ret;
	ArgInfo sigCookie;
	ArgInfo args [1];
} CallInfo;

typedef struct {
	gint32	gr[5];		/* R2-R6			    */
	gdouble fp[3];		/* F0-F2			    */
} __attribute__ ((packed)) RegParm;

/*========================= End of Typedefs ========================*/

/*------------------------------------------------------------------*/
/*                   P r o t o t y p e s                            */
/*------------------------------------------------------------------*/

static guint8 * emit_memcpy (guint8 *, int, int, int, int, int);
static void indent (int);
static guint8 * backUpStackPtr(MonoCompile *, guint8 *, gint);
static void decodeParm (MonoType *, void *, int);
static void enter_method (MonoMethod *, RegParm *, char *);
static void leave_method (MonoMethod *, ...);
static gboolean is_regsize_var (MonoType *);
static inline void add_general (guint *, size_data *, ArgInfo *, gboolean);
static inline void add_stackParm (guint *, size_data *, ArgInfo *, gint);
static inline void add_float (guint *, size_data *, ArgInfo *);
static CallInfo * calculate_sizes (MonoMethodSignature *, size_data *, gboolean);
static void peephole_pass (MonoCompile *, MonoBasicBlock *);
static guchar * emit_float_to_int (MonoCompile *, guchar *, int, int, int, gboolean);
static void mono_arch_break(void);
gpointer mono_arch_get_lmf_addr (void);
static guint8 * emit_load_volatile_registers(guint8 *, MonoCompile *);

/*========================= End of Prototypes ======================*/

/*------------------------------------------------------------------*/
/*                 G l o b a l   V a r i a b l e s                  */
/*------------------------------------------------------------------*/

int mono_exc_esp_offset = 0;

static int indent_level = 0;

static const char*const * ins_spec = s390_cpu_desc;

static gboolean tls_offset_inited = FALSE;

static int appdomain_tls_offset = -1,
       	   lmf_tls_offset = -1,
           thread_tls_offset = -1;

pthread_key_t lmf_addr_key;

gboolean lmf_addr_key_inited = FALSE; 

#if 0

extern __thread MonoDomain *tls_appdomain;
extern __thread MonoThread *tls_current_object;
extern __thread gpointer   mono_lmf_addr;
		
#endif

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
/* Name		- emit_memcpy                                       */
/*                                                                  */
/* Function	- Emit code to move from memory-to-memory based on  */
/*		  the size of the variable. r0 is overwritten.      */
/*                                                                  */
/*------------------------------------------------------------------*/

static guint8 *
emit_memcpy (guint8 *code, int size, int dreg, int doffset, int sreg, int soffset)
{
	switch (size) {
		case 4 :
			s390_l  (code, s390_r0, 0, sreg, soffset);
			s390_st (code, s390_r0, 0, dreg, doffset);
			break;

		case 3 : 
			s390_icm  (code, s390_r0, 14, sreg, soffset);
			s390_stcm (code, s390_r0, 14, dreg, doffset);
			break;

		case 2 : 
			s390_lh  (code, s390_r0, 0, sreg, soffset);
			s390_sth (code, s390_r0, 0, dreg, doffset);
			break;

		case 1 : 
			s390_ic  (code, s390_r0, 0, sreg, soffset);
		 	s390_stc (code, s390_r0, 0, dreg, doffset);
			break;
	
		default : 
			while (size > 0) {
				int len;

				if (size > 256) 
					len = 256;
				else
					len = size;
				s390_mvc (code, len, dreg, doffset, sreg, soffset);
				size -= len;
			}
	}
	return code;
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
		offset += 4;
	}

	arg_info [0].offset = offset;

	if (csig->hasthis) {
		frame_size += sizeof (gpointer);
		offset += 4;
	}

	arg_info [0].size = frame_size;

	for (k = 0; k < param_count; k++) {
		
		if (csig->pinvoke)
			size = mono_type_native_stack_size (csig->params [k], &align);
		else
			size = mono_type_stack_size (csig->params [k], &align);

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
backUpStackPtr(MonoCompile *cfg, guint8 *code, gint framePtr)
{
	int stackSize = cfg->stack_usage;

	if (s390_is_uimm16 (cfg->stack_usage)) {
		s390_ahi  (code, framePtr, cfg->stack_usage);
	} else { 
		while (stackSize > 32767) {
			s390_ahi  (code, framePtr, 32767);
			stackSize -= 32767;
		}
		s390_ahi  (code, framePtr, stackSize);
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
				printf ("[BOOL:%d], ", *((int *) curParm));
				break;
			case MONO_TYPE_CHAR :
				printf ("[CHAR:%c], ", *((int *) curParm));
				break;
			case MONO_TYPE_I1 :
				printf ("[INT1:%d], ", *((int *) curParm));
				break; 
			case MONO_TYPE_I2 :
				printf ("[INT2:%d], ", *((int *) curParm));
				break; 
			case MONO_TYPE_I4 :
				printf ("[INT4:%d], ", *((int *) curParm));
				break; 
			case MONO_TYPE_U1 :
				printf ("[UINT1:%u], ", *((unsigned int *) curParm));
				break; 
			case MONO_TYPE_U2 :
				printf ("[UINT2:%u], ", *((guint16 *) curParm));
				break; 
			case MONO_TYPE_U4 :
				printf ("[UINT4:%u], ", *((guint32 *) curParm));
				break; 
			case MONO_TYPE_U8 :
				printf ("[UINT8:%llu], ", *((guint64 *) curParm));
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
//					if (class == mono_defaults.string_class) {
//						printf("[STRING:%p:%s]", 
//						       *obj, mono_string_to_utf8 (obj));
//					} else if (class == mono_defaults.int32_class) { 
//						printf("[INT32:%p:%d]", 
//							obj, *(gint32 *)((char *)obj + sizeof (MonoObject)));
//					} else
//						printf("[%s.%s:%p]", 
//						       class->name_space, class->name, obj);
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
				printf("[INT8:%lld], ", *((gint64 *) (curParm)));
				break;
			case MONO_TYPE_R4 :
				printf("[FLOAT4:%g], ", *((double *) (curParm)));
				break;
			case MONO_TYPE_R8 :
				printf("[FLOAT8:%g], ", *((double *) (curParm)));
				break;
			case MONO_TYPE_VALUETYPE : {
				int i;
				MonoMarshalType *info;

				if (type->data.klass->enumtype) {
					simpleType = type->data.klass->enum_basetype->type;
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
				printf("]");
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
				printf("[?? - %d], ",simpleType);
		}
	}
}

/*========================= End of Function ========================*/

static int lc = 0;
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
	guint32 ip;
	CallInfo *cinfo;
	ArgInfo *ainfo;
	size_data sz;
	void *curParm;


lc++;
if (lc > 5000000) {
fseek(stdout, 0L, SEEK_SET);
lc = 0;
}
	fname = mono_method_full_name (method, TRUE);
	indent (1);
	printf ("ENTER: %s(", fname);
	g_free (fname);

	ip  = (*(guint32 *) (sp+S390_RET_ADDR_OFFSET)) & 0x7fffffff;
	printf (") ip: %p sp: %p - ", (gpointer) ip, sp); 

	if (rParm == NULL)
		return;
	
	sig = mono_method_signature (method);
	
	cinfo = calculate_sizes (sig, &sz, sig->pinvoke);

	if (cinfo->struct_ret) {
		printf ("[STRUCTRET:%p], ", (gpointer) rParm->gr[0]);
		iParm = 1;
	}

	if (sig->hasthis) {
		gpointer *this = (gpointer *) rParm->gr[iParm];
		obj = (MonoObject *) this;
		if (method->klass->valuetype) { 
			if (obj) {
				printf("this:[value:%p:%08x], ", 
				       this, *((guint32 *)(this+sizeof(MonoObject))));
			} else 
				printf ("this:[NULL], ");
		} else {
			if (obj) {
//				class = obj->vtable->klass;
//				if (class == mono_defaults.string_class) {
//					printf ("this:[STRING:%p:%s], ", 
//						obj, mono_string_to_utf8 ((MonoString *)obj));
//				} else {
//					printf ("this:%p[%s.%s], ", 
//						obj, class->name_space, class->name);
//				}
printf("this:%p, ",obj);
			} else 
				printf ("this:NULL, ");
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
				if (ainfo->reg != STK_BASE) 
					curParm = &(rParm->gr[ainfo->reg-2]);
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
	guint32 ip;
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
		int val = va_arg (ap, int);
		printf ("[INT:%d]", val);
		printf("]");
		break;
	}
	case MONO_TYPE_U: {
		int val = va_arg (ap, int);
		printf ("[UINT:%d]", val);
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

//		if ((o) && (o->vtable)) {
//			if (o->vtable->klass == mono_defaults.boolean_class) {
//				printf ("[BOOLEAN:%p:%d]", o, *((guint8 *)o + sizeof (MonoObject)));		
//			} else if  (o->vtable->klass == mono_defaults.int32_class) {
//				printf ("[INT32:%p:%d]", o, *((gint32 *)((char *)o + sizeof (MonoObject))));	
//			} else if  (o->vtable->klass == mono_defaults.int64_class) {
//				printf ("[INT64:%p:%lld]", o, *((gint64 *)((char *)o + sizeof (MonoObject))));	
//			} else
//				printf ("[%s.%s:%p]", o->vtable->klass->name_space, o->vtable->klass->name, o);
//		} else
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
		printf ("[LONG:%lld]", l);
		break;
	}
	case MONO_TYPE_U8: {
		guint64 l =  va_arg (ap, guint64);
		printf ("[ULONG:%llu]", l);
		break;
	}
	case MONO_TYPE_R4: {
		double f;
		f = va_arg (ap, double);
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
			type = type->data.klass->enum_basetype;
			goto handle_enum;
		} else {
			guint8 *p = va_arg (ap, gpointer);
			int j, size, align;

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
				printf ("[VALUERET]\n");
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

	ip = ((gint32) __builtin_return_address (0)) & 0x7fffffff;
	printf (" ip: %p\n", (gpointer) ip);
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
	guint mode = 1;

	/*--------------------------------------*/	
	/* Set default rounding mode for FP	*/
	/*--------------------------------------*/	
	__asm__ ("SRNM\t%0\n\t"
		: : "m" (mode));
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
	/* no s390-specific optimizations yet 			    */
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
	case MONO_TYPE_U:
		return TRUE;
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_STRING:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:
		return FALSE;
	case MONO_TYPE_VALUETYPE:
		if (t->data.klass->enumtype)
			return is_regsize_var (t->data.klass->enum_basetype);
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

	header = mono_method_get_header (cfg->method);
	if ((cfg->flags & MONO_CFG_HAS_ALLOCA) || header->num_clauses)
		cfg->frame_reg = s390_r11;

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
add_general (guint *gr, size_data *sz, ArgInfo *ainfo, gboolean simple)
{
	if (simple) {
		if (*gr > S390_LAST_ARG_REG) {
			sz->stack_size  = S390_ALIGN(sz->stack_size, sizeof(long));
			ainfo->offset   = sz->stack_size;
			ainfo->reg	= STK_BASE;
			ainfo->regtype  = RegTypeBase;
			sz->stack_size += sizeof(int);
			sz->local_size += sizeof(int);
			sz->offStruct  += sizeof(int);
			sz->code_size  += 12;    
		} else {
			ainfo->reg      = *gr;
			sz->code_size  += 8;    
		}
	} else {
		if (*gr > S390_LAST_ARG_REG - 1) {
			sz->stack_size  = S390_ALIGN(sz->stack_size, S390_STACK_ALIGNMENT);
			ainfo->offset   = sz->stack_size;
			ainfo->reg	= STK_BASE;
			ainfo->regtype  = RegTypeBase;
			sz->stack_size += sizeof(long long);
			sz->local_size += sizeof(long long);
			sz->offStruct  += sizeof(long long);
			sz->code_size  += 10;   
		} else {
			ainfo->reg      = *gr;
			sz->code_size  += 8;
		}
		(*gr) ++;
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
		ainfo->reg	= STK_BASE;
		sz->parm_size  += sizeof(gpointer);
		sz->offStruct  += sizeof(gpointer);
	} else {
		ainfo->reg      = *gr;
	}
	(*gr) ++;
	ainfo->offset   = sz->stack_size;
	ainfo->offparm  = sz->offset;
	sz->offset      = S390_ALIGN(sz->offset+size, sizeof(long));
	ainfo->size     = size;
	ainfo->regtype  = RegTypeStructByAddr; 
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
		sz->stack_size += ainfo->size;
		sz->local_size += ainfo->size;
		sz->offStruct  += ainfo->size;
	}
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- calculate_sizes                                   */
/*                                                                  */
/* Function	- Determine the amount of space required for code   */
/* 		  and stack. In addition determine starting points  */
/*		  for stack-based parameters, and area for struct-  */
/*		  ures being returned on the stack.		    */
/*                                                                  */
/*------------------------------------------------------------------*/

static CallInfo *
calculate_sizes (MonoMethodSignature *sig, size_data *sz, 
		 gboolean string_ctor)
{
	guint i, fr, gr, size;
	int nParm = sig->hasthis + sig->param_count;
	guint32 simpletype, align;
	CallInfo *cinfo = g_malloc0 (sizeof (CallInfo) + sizeof (ArgInfo) * nParm);

	fr                = 0;
	gr                = s390_r2;
	nParm 		  = 0;
	cinfo->struct_ret = 0;
	sz->offset	  = 0;
	sz->offStruct     = S390_MINIMAL_STACK_SIZE;
	sz->retStruct     = 0;
	sz->stack_size    = S390_MINIMAL_STACK_SIZE;
	sz->code_size     = 0;
	sz->parm_size     = 0;
	sz->local_size    = 0;

	/*----------------------------------------------------------*/
	/* We determine the size of the return code/stack in case we*/
	/* need to reserve a register to be used to address a stack */
	/* area that the callee will use.			    */
	/*----------------------------------------------------------*/

	simpletype = mono_type_get_underlying_type (sig->ret)->type;
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
		case MONO_TYPE_VALUETYPE: {
			MonoClass *klass = mono_class_from_mono_type (sig->ret);
			if (sig->ret->data.klass->enumtype) {
				simpletype = sig->ret->data.klass->enum_basetype->type;
				goto enum_retvalue;
			}
			if (sig->pinvoke)
				size = mono_class_native_size (klass, &align);
			else
                        	size = mono_class_value_size (klass, &align);
	
			cinfo->ret.reg    = s390_r2;
			cinfo->struct_ret = 1;
			cinfo->ret.size   = size;
			cinfo->ret.vtsize = size;
			gr++;
                        break;
		}
		case MONO_TYPE_TYPEDBYREF:
			size = sizeof (MonoTypedRef);
			cinfo->ret.reg    = s390_r2;
			cinfo->struct_ret = 1;
			cinfo->ret.size   = size;
			cinfo->ret.vtsize = size;
			gr++;
			break;
		case MONO_TYPE_VOID:
			break;
		default:
			g_error ("Can't handle as return value 0x%x", sig->ret->type);
	}

	if (sig->hasthis) {
		add_general (&gr, sz, cinfo->args+nParm, TRUE);
		cinfo->args[nParm].size = sizeof(gpointer);
		nParm++;
	}

	/*----------------------------------------------------------*/
	/* We determine the size of the parameter code and stack    */
	/* requirements by checking the types and sizes of the      */
	/* parameters.						    */
	/*----------------------------------------------------------*/

	for (i = 0; i < sig->param_count; ++i) {
		/*--------------------------------------------------*/
		/* Handle vararg type calls. All args are put on    */
		/* the stack.                                       */
		/*--------------------------------------------------*/
		if ((sig->call_convention == MONO_CALL_VARARG) &&
		    (i == sig->sentinelpos)) {
			gr = S390_LAST_ARG_REG + 1;
			add_general (&gr, sz, &cinfo->sigCookie, TRUE);
		}

		if (sig->params [i]->byref) {
			add_general (&gr, sz, cinfo->args+nParm, TRUE);
			cinfo->args[nParm].size = sizeof(gpointer);
			nParm++;
			continue;
		}

		simpletype = mono_type_get_underlying_type(sig->params [i])->type;
		switch (simpletype) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
			cinfo->args[nParm].size = sizeof(char);
			add_general (&gr, sz, cinfo->args+nParm, TRUE);
			nParm++;
			break;
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_CHAR:
			cinfo->args[nParm].size = sizeof(short);
			add_general (&gr, sz, cinfo->args+nParm, TRUE);
			nParm++;
			break;
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
			cinfo->args[nParm].size = sizeof(int);
			add_general (&gr, sz, cinfo->args+nParm, TRUE);
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
			add_general (&gr, sz, cinfo->args+nParm, TRUE);
			nParm++;
			break;
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
			cinfo->args[nParm].size = sizeof(long long);
			add_general (&gr, sz, cinfo->args+nParm, FALSE);
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
				break;
			}

			if ((info->native_size == sizeof(double)) &&
			    (info->num_fields  == 1) &&
			    (info->fields[0].field->type->type == MONO_TYPE_R8)) {
				cinfo->args[nParm].size = sizeof(double);
				add_float(&fr, sz, cinfo->args+nParm);
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
					add_general(&gr, sz, cinfo->args+nParm, TRUE);
					cinfo->args[nParm].size    = size;
					cinfo->args[nParm].regtype = RegTypeStructByVal; 
					nParm++;
					sz->local_size 		  += sizeof(long);
					break;
				case 8:
					add_general(&gr, sz, cinfo->args+nParm, FALSE);
					cinfo->args[nParm].size    = sizeof(long long);
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
					add_general(&gr, sz, cinfo->args+nParm, TRUE);
					cinfo->args[nParm].size    = size;
					cinfo->args[nParm].regtype = RegTypeStructByVal; 
					nParm++;
					sz->local_size 		  += sizeof(long);
					break;
				case 8:
					add_general(&gr, sz, cinfo->args+nParm, FALSE);
					cinfo->args[nParm].size    = sizeof(long long);
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
	size_data sz;
	int iParm, iVar, offset, size, align, curinst;
	int frame_reg = STK_BASE;
	int sArg, eArg;

	header  = mono_method_get_header (cfg->method);

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

	if (frame_reg != STK_BASE) 
		cfg->used_int_regs |= 1 << frame_reg;		

	sig     = mono_method_signature (cfg->method);
	
	cinfo   = calculate_sizes (sig, &sz, sig->pinvoke);

	if (cinfo->struct_ret) {
		cfg->ret->opcode = OP_REGVAR;
		cfg->ret->inst_c0 = s390_r2;
	} else {
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
	offset		= (cfg->param_area + S390_MINIMAL_STACK_SIZE);
	cfg->sig_cookie = 0;

	if (cinfo->struct_ret) {
		inst 		   = cfg->ret;
		offset 		   = S390_ALIGN(offset, sizeof(gpointer));
		inst->inst_offset  = offset;
		inst->opcode 	   = OP_REGOFFSET;
		inst->inst_basereg = frame_reg;
		offset 		  += sizeof(gpointer);
		if ((sig->call_convention == MONO_CALL_VARARG) &&
		    (!retFitsInReg (cinfo->ret.size)))
			cfg->sig_cookie += cinfo->ret.size;
	}

	if (sig->hasthis) {
		inst = cfg->varinfo [0];
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
		inst = cfg->varinfo [curinst];
		if (inst->opcode != OP_REGVAR) {
			switch (cinfo->args[iParm].regtype) {
				case RegTypeStructByAddr :
				if (cinfo->args[iParm].reg == STK_BASE) {
					inst->opcode       = OP_S390_LOADARG;
					inst->inst_basereg = frame_reg;
					size		   = abs(cinfo->args[iParm].vtsize);
					offset 		   = S390_ALIGN(offset, sizeof(long));
					inst->inst_offset  = offset; 
					inst->unused       = cinfo->args[iParm].offset;
				} else {
					inst->opcode 	   = OP_S390_ARGREG;
					inst->inst_basereg = frame_reg;
					size		   = sizeof(gpointer);
					offset		   = S390_ALIGN(offset, size);
					inst->inst_offset  = offset;
					inst->unused       = cinfo->args[iParm].offset;
				}
					break;
				case RegTypeStructByVal :
					inst->opcode	   = OP_S390_ARGPTR;
					inst->inst_basereg = frame_reg;
					size		   = cinfo->args[iParm].size;
					offset		   = S390_ALIGN(offset, size);
					inst->inst_offset  = offset;
					inst->unused       = cinfo->args[iParm].offset;
					break;
				default :
				if (cinfo->args[iParm].reg != STK_BASE) {
					inst->opcode 	   = OP_REGOFFSET;
					inst->inst_basereg = frame_reg;
					size		   = (cinfo->args[iParm].size < 8
							      ? sizeof(long)  
							      : sizeof(long long));
					offset		   = S390_ALIGN(offset, size);
					inst->inst_offset  = offset;
				} else {
					inst->opcode 	   = OP_S390_STKARG;
					inst->inst_basereg = frame_reg;
					size		   = (cinfo->args[iParm].size < 4
							      ? 4 - cinfo->args[iParm].size
							      : 0);
					inst->inst_offset  = cinfo->args[iParm].offset + 
							     size;
					inst->unused       = 0;
					size		   = sizeof(long);
				} 
			}
			if ((sig->call_convention == MONO_CALL_VARARG) && 
			    (cinfo->args[iParm].regtype != RegTypeGeneral) &&
			    (iParm < sig->sentinelpos)) 
				cfg->sig_cookie += size;

			offset += size;
		}
		curinst++;
	}

	curinst = cfg->locals_start;
	for (iVar = curinst; iVar < cfg->num_varinfo; ++iVar) {
		inst = cfg->varinfo [iVar];
		if ((inst->flags & MONO_INST_IS_DEAD) || 
		    (inst->opcode == OP_REGVAR))
			continue;

		/*--------------------------------------------------*/
		/* inst->unused indicates native sized value types, */
		/* this is used by the pinvoke wrappers when they   */
		/* call functions returning structure 		    */
		/*--------------------------------------------------*/
		if (inst->unused && MONO_TYPE_ISSTRUCT (inst->inst_vtype))
			size = mono_class_native_size (mono_class_from_mono_type(inst->inst_vtype), &align);
		else
			size = mono_type_size (inst->inst_vtype, &align);

		offset 		   = S390_ALIGN(offset, align);
		inst->inst_offset  = offset;
		inst->opcode 	   = OP_REGOFFSET;
		inst->inst_basereg = frame_reg;
		offset 		  += size;
		DEBUG (g_print("allocating local %d to %ld\n", iVar, inst->inst_offset));
	}

	/*------------------------------------------------------*/
	/* Allow space for the trace method stack area if needed*/
	/*------------------------------------------------------*/
	if (mono_jit_trace_calls != NULL && mono_trace_eval (cfg->method)) {
		offset += S390_TRACE_STACK_SIZE;
	}

	/*------------------------------------------------------*/
	/* Reserve space to save LMF and caller saved registers */
	/*------------------------------------------------------*/
	if (cfg->method->save_lmf)
		offset += sizeof (MonoLMF);

	/*------------------------------------------------------*/
	/* align the offset 					*/
	/*------------------------------------------------------*/
	cfg->stack_offset = S390_ALIGN(offset, S390_STACK_ALIGNMENT);

}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_call_opcode                             */
/*                                                                  */
/* Function	- Take the arguments and generate the arch-specific */
/*		  instructions to properly call the function. This  */
/*		  includes pushing, moving argments to the correct  */
/*		  etc.                                              */
/*		                               			    */
/*------------------------------------------------------------------*/

MonoCallInst*
mono_arch_call_opcode (MonoCompile *cfg, MonoBasicBlock* bb, 
		       MonoCallInst *call, int is_virtual) {
	MonoInst *in;
	MonoCallArgParm *arg;
	MonoMethodSignature *sig;
	int i, n, lParamArea;
	CallInfo *cinfo;
	ArgInfo *ainfo;
	size_data sz;
	int stackSize;

	sig = call->signature;
	n = sig->param_count + sig->hasthis;
	DEBUG (g_print ("Call requires: %d parameters\n",n));
	
	cinfo = calculate_sizes (sig, &sz, sig->pinvoke);

	stackSize         = sz.stack_size + sz.local_size + sz.parm_size + sz.offset;
	call->stack_usage = MAX(stackSize, call->stack_usage);
	lParamArea        = MAX((call->stack_usage-S390_MINIMAL_STACK_SIZE-sz.parm_size), 0);
	cfg->param_area   = MAX(((signed) cfg->param_area), lParamArea);
	cfg->flags       |= MONO_CFG_HAS_CALLS;

	if (cinfo->struct_ret)
		call->used_iregs |= 1 << cinfo->ret.reg;

	for (i = 0; i < n; ++i) {
		ainfo = cinfo->args + i;

		if ((sig->call_convention == MONO_CALL_VARARG) &&
		    (i == sig->sentinelpos)) {
			MonoInst *sigArg;
			
			cfg->disable_aot = TRUE;
			MONO_INST_NEW (cfg, sigArg, OP_ICONST);
			sigArg->inst_p0 = call->signature;

			MONO_INST_NEW_CALL_ARG (cfg, arg, OP_OUTARG_MEMBASE);
			arg->ins.inst_left  = sigArg;
			arg->ins.inst_right = (MonoInst *) call;
			arg->size           = ainfo->size;
			arg->offset         = cinfo->sigCookie.offset;
			call->used_iregs   |= 1 << ainfo->reg;
			arg->ins.next       = call->out_args;
			call->out_args      = (MonoInst *) arg;
		}

		if (is_virtual && i == 0) {
			/* the argument will be attached to the call instrucion */
			in = call->args [i];
			call->used_iregs |= 1 << ainfo->reg;
		} else {
			MONO_INST_NEW_CALL_ARG (cfg, arg, OP_OUTARG);
			in                  = call->args [i];
			arg->ins.cil_code   = in->cil_code;
			arg->ins.inst_left  = in;
			arg->ins.type       = in->type;
			/* prepend, we'll need to reverse them later */
			arg->ins.next       = call->out_args;
			call->out_args      = (MonoInst *) arg;
			arg->ins.inst_right = (MonoInst *) call;
			if (ainfo->regtype == RegTypeGeneral) {
				arg->ins.unused   = ainfo->reg;
				call->used_iregs |= 1 << ainfo->reg;
				if (arg->ins.type == STACK_I8)
					call->used_iregs |= 1 << (ainfo->reg + 1);
			} else if (ainfo->regtype == RegTypeStructByAddr) {
				call->used_iregs |= 1 << ainfo->reg;
				arg->ins.sreg1    = ainfo->reg;
				arg->ins.opcode   = OP_OUTARG_VT;
				arg->size         = -ainfo->vtsize;
				arg->offset       = ainfo->offset;
				arg->offPrm       = ainfo->offparm + sz.offStruct;
			} else if (ainfo->regtype == RegTypeStructByVal) {
				if (ainfo->reg != STK_BASE) {
					switch (ainfo->size) {
					case 0:
					case 1:
					case 2:
					case 4:
						call->used_iregs |= 1 << ainfo->reg;
						break;
					case 8:
						call->used_iregs |= 1 << ainfo->reg;
						call->used_iregs |= 1 << (ainfo->reg+1);
						break;
					default:
						call->used_iregs |= 1 << ainfo->reg;
					}
				} 
				arg->ins.sreg1  = ainfo->reg;
				arg->ins.opcode = OP_OUTARG_VT;
				arg->size       = ainfo->size;
				arg->offset     = ainfo->offset;
				arg->offPrm     = ainfo->offparm + sz.offStruct;
			} else if (ainfo->regtype == RegTypeBase) {
				arg->ins.opcode   = OP_OUTARG_MEMBASE;
				arg->ins.sreg1    = ainfo->reg;
				arg->size         = ainfo->size;
				arg->offset       = ainfo->offset;
				call->used_iregs |= 1 << ainfo->reg;
			} else if (ainfo->regtype == RegTypeFP) {
				arg->ins.unused   = ainfo->reg;
				call->used_fregs |= 1 << ainfo->reg;
				if (ainfo->size == 4)
					arg->ins.opcode = OP_OUTARG_R4;
				else
					arg->ins.opcode = OP_OUTARG_R8;
			} else {
				g_assert_not_reached ();
			}
		}
	}
	/*
	 * Reverse the call->out_args list.
	 */
	{
		MonoInst *prev = NULL, *list = call->out_args, *next;
		while (list) {
			next = list->next;
			list->next = prev;
			prev = list;
			list = next;
		}
		call->out_args = prev;
	}

	g_free (cinfo);
	return call;
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
	fpOffset   = parmOffset + (5*sizeof(gint32));
	if (fpOffset > 4096) {
		s390_lr (code, s390_r12, STK_BASE);
		baseReg = s390_r12;
		while (fpOffset > 4096) {
			s390_ahi (code, baseReg, 4096);
			fpOffset   -= 4096;
			parmOffset -= 4096;
		}
	} else {
		baseReg = STK_BASE;
	}	

	s390_stm  (code, s390_r2, s390_r6, baseReg, parmOffset);
	s390_std  (code, s390_f0, 0, baseReg, fpOffset);
	s390_std  (code, s390_f1, 0, baseReg, fpOffset+sizeof(gdouble));
	s390_std  (code, s390_f2, 0, baseReg, fpOffset+2*sizeof(gdouble));
	s390_basr (code, s390_r13, 0);
	s390_j    (code, 6);
	s390_word (code, cfg->method);
	s390_word (code, func);
	s390_l    (code, s390_r2, 0, s390_r13, 4);
	s390_la   (code, s390_r3, 0, baseReg, parmOffset);
	s390_lr   (code, s390_r4, STK_BASE);
	s390_ahi  (code, s390_r4, cfg->stack_usage);
	s390_l	  (code, s390_r1, 0, s390_r13, 8);
	s390_basr (code, s390_r14, s390_r1);
	s390_ld   (code, s390_f2, 0, baseReg, fpOffset+2*sizeof(gdouble));
	s390_ld   (code, s390_f1, 0, baseReg, fpOffset+sizeof(gdouble));
	s390_ld   (code, s390_f0, 0, baseReg, fpOffset);
	s390_lm   (code, s390_r2, s390_r6, baseReg, parmOffset);

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
mono_arch_instrument_epilog (MonoCompile *cfg, void *func, void *p, gboolean enable_arguments)
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
		save_mode = SAVE_TWO;
		break;
	case MONO_TYPE_R4:
		save_mode = SAVE_R4;
		break;
	case MONO_TYPE_R8:
		save_mode = SAVE_R8;
		break;
	case MONO_TYPE_VALUETYPE:
		if (mono_method_signature (method)->ret->data.klass->enumtype) {
			rtype = mono_method_signature (method)->ret->data.klass->enum_basetype->type;
			goto handle_enum;
		}
		save_mode = SAVE_STRUCT;
		break;
	default:
		save_mode = SAVE_ONE;
		break;
	}

	switch (save_mode) {
	case SAVE_TWO:
		s390_stm (code, s390_r2, s390_r3, cfg->frame_reg, saveOffset);
		if (enable_arguments) {
			s390_lr (code, s390_r4, s390_r3);
			s390_lr (code, s390_r3, s390_r2);
		}
		break;
	case SAVE_ONE:
		s390_st (code, s390_r2, 0, cfg->frame_reg, saveOffset);
		if (enable_arguments) {
			s390_lr (code, s390_r3, s390_r2);
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
		s390_st (code, s390_r2, 0, cfg->frame_reg, saveOffset);
		if (enable_arguments) {
			s390_l (code, s390_r3, 0, cfg->frame_reg, 
				S390_MINIMAL_STACK_SIZE+cfg->param_area);
		}
		break;
	case SAVE_NONE:
	default:
		break;
	}

	s390_basr (code, s390_r13, 0);
	s390_j	  (code, 6);
	s390_word (code, cfg->method);
	s390_word (code, func);
	s390_l    (code, s390_r2, 0, s390_r13, 4);
	s390_l	  (code, s390_r1, 0, s390_r13, 8);
	s390_basr (code, s390_r14, s390_r1);

	switch (save_mode) {
	case SAVE_TWO:
		s390_lm  (code, s390_r2, s390_r3, cfg->frame_reg, saveOffset);
		break;
	case SAVE_ONE:
		s390_l   (code, s390_r2, 0, cfg->frame_reg, saveOffset);
		break;
	case SAVE_R4:
	case SAVE_R8:
		s390_ld  (code, s390_f0, 0, cfg->frame_reg, saveOffset);
		break;
	case SAVE_STRUCT:
		s390_l   (code, s390_r2, 0, cfg->frame_reg, saveOffset);
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
/* Name		- peephole_pass                                     */
/*                                                                  */
/* Function	- Form a peephole pass at the code looking for      */
/*		  simple optimizations.        			    */
/*		                               			    */
/*------------------------------------------------------------------*/

static void
peephole_pass (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins, *last_ins = NULL;
	ins = bb->code;

	while (ins) {

		switch (ins->opcode) {
		case OP_MUL_IMM: 
			/* remove unnecessary multiplication with 1 */
			if (ins->inst_imm == 1) {
				if (ins->dreg != ins->sreg1) {
					ins->opcode = OP_MOVE;
				} else {
					last_ins->next = ins->next;				
					ins = ins->next;				
					continue;
				}
			}
			break;
		case OP_LOAD_MEMBASE:
		case OP_LOADI4_MEMBASE:
			/* 
			 * OP_STORE_MEMBASE_REG reg, offset(basereg) 
			 * OP_LOAD_MEMBASE offset(basereg), reg
			 */
			if (last_ins && (last_ins->opcode == OP_STOREI4_MEMBASE_REG 
					 || last_ins->opcode == OP_STORE_MEMBASE_REG) &&
			    ins->inst_basereg == last_ins->inst_destbasereg &&
			    ins->inst_offset == last_ins->inst_offset) {
				if (ins->dreg == last_ins->sreg1) {
					last_ins->next = ins->next;				
					ins = ins->next;				
					continue;
				} else {
					ins->opcode = OP_MOVE;
					ins->sreg1 = last_ins->sreg1;
				}

			/* 
			 * Note: reg1 must be different from the basereg in the second load
			 * OP_LOAD_MEMBASE offset(basereg), reg1
			 * OP_LOAD_MEMBASE offset(basereg), reg2
			 * -->
			 * OP_LOAD_MEMBASE offset(basereg), reg1
			 * OP_MOVE reg1, reg2
			 */
			} if (last_ins && (last_ins->opcode == OP_LOADI4_MEMBASE
					   || last_ins->opcode == OP_LOAD_MEMBASE) &&
			      ins->inst_basereg != last_ins->dreg &&
			      ins->inst_basereg == last_ins->inst_basereg &&
			      ins->inst_offset == last_ins->inst_offset) {

				if (ins->dreg == last_ins->dreg) {
					last_ins->next = ins->next;				
					ins = ins->next;				
					continue;
				} else {
					ins->opcode = OP_MOVE;
					ins->sreg1 = last_ins->dreg;
				}

				//g_assert_not_reached ();

#if 0
			/* 
			 * OP_STORE_MEMBASE_IMM imm, offset(basereg) 
			 * OP_LOAD_MEMBASE offset(basereg), reg
			 * -->
			 * OP_STORE_MEMBASE_IMM imm, offset(basereg) 
			 * OP_ICONST reg, imm
			 */
			} else if (last_ins && (last_ins->opcode == OP_STOREI4_MEMBASE_IMM
						|| last_ins->opcode == OP_STORE_MEMBASE_IMM) &&
				   ins->inst_basereg == last_ins->inst_destbasereg &&
				   ins->inst_offset == last_ins->inst_offset) {
				//static int c = 0; printf ("MATCHX %s %d\n", cfg->method->name,c++);
				ins->opcode = OP_ICONST;
				ins->inst_c0 = last_ins->inst_imm;
				g_assert_not_reached (); // check this rule
#endif
			}
			break;
		case OP_LOADU1_MEMBASE:
		case OP_LOADI1_MEMBASE:
			if (last_ins && (last_ins->opcode == OP_STOREI1_MEMBASE_REG) &&
					ins->inst_basereg == last_ins->inst_destbasereg &&
					ins->inst_offset == last_ins->inst_offset) {
				if (ins->dreg == last_ins->sreg1) {
					last_ins->next = ins->next;				
					ins = ins->next;				
					continue;
				} else {
					//static int c = 0; printf ("MATCHX %s %d\n", cfg->method->name,c++);
					ins->opcode = OP_MOVE;
					ins->sreg1 = last_ins->sreg1;
				}
			}
			break;
		case OP_LOADU2_MEMBASE:
		case OP_LOADI2_MEMBASE:
			if (last_ins && (last_ins->opcode == OP_STOREI2_MEMBASE_REG) &&
					ins->inst_basereg == last_ins->inst_destbasereg &&
					ins->inst_offset == last_ins->inst_offset) {
				if (ins->dreg == last_ins->sreg1) {
					last_ins->next = ins->next;				
					ins = ins->next;				
					continue;
				} else {
					ins->opcode = OP_MOVE;
					ins->sreg1 = last_ins->sreg1;
				}
			}
			break;
		case CEE_CONV_I4:
		case CEE_CONV_U4:
		case OP_MOVE:
			/* 
			 * OP_MOVE reg, reg 
			 */
			if (ins->dreg == ins->sreg1) {
				if (last_ins)
					last_ins->next = ins->next;				
				ins = ins->next;
				continue;
			}
			/* 
			 * OP_MOVE sreg, dreg 
			 * OP_MOVE dreg, sreg
			 */
			if (last_ins && last_ins->opcode == OP_MOVE &&
			    ins->sreg1 == last_ins->dreg &&
			    ins->dreg == last_ins->sreg1) {
				last_ins->next = ins->next;				
				ins = ins->next;				
				continue;
			}
			break;
		}
		last_ins = ins;
		ins = ins->next;
	}
	bb->last_ins = last_ins;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_local_regalloc.                         */
/*                                                                  */
/* Function	- We first scan the list of instructions and we     */
/*                save the liveness information of each register    */
/*                (when the register is first used, when its value  */
/*                is set etc.). We also reverse the list of instr-  */
/*                uctions (in the InstList list) because assigning  */
/*                registers backwards allows for more tricks to be  */
/*		  used.                        			    */
/*		                               			    */
/*------------------------------------------------------------------*/

void
mono_arch_local_regalloc (MonoCompile *cfg, MonoBasicBlock *bb)
{
	mono_local_regalloc(cfg, bb);
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
		s390_cfdbr (code, dreg, 5, sreg);
		switch (size) {
			case 1:
				s390_lhi  (code, s390_r0, 0);
				s390_lhi  (code, s390_r13, 0xff);
				s390_ltr  (code, dreg, dreg);
				s390_jnl  (code, 4);
				s390_lhi  (code, s390_r0, 0x80);
				s390_nr   (code, dreg, s390_r13);
				s390_or   (code, dreg, s390_r0);
				break;
		}
	} else {
		short *o[1];
		s390_basr   (code, s390_r13, 0);
		s390_j	    (code, 10);
		s390_llong  (code, 0x41e0000000000000);
		s390_llong  (code, 0x41f0000000000000);
		s390_ldr    (code, s390_f15, sreg);
		s390_cdb    (code, s390_f15, 0, s390_r13, 4);
		s390_jl     (code, 0); CODEPTR(code, o[0]);
		s390_sdb    (code, s390_f15, 0, s390_r13, 12);
		s390_cfdbr  (code, dreg, 7, s390_f15);
		s390_j      (code, 4);
		PTRSLOT(code, o[0]);
		s390_cfdbr  (code, dreg, 5, sreg);
		switch (size) {
			case 1: 
				s390_lhi  (code, s390_r0, 0xff);
				s390_nr   (code, dreg, s390_r0);
				break;
			case 2:
				s390_lhi  (code, s390_r0, -1);
				s390_srl  (code, s390_r0, 0, 16);
				s390_nr   (code, dreg, s390_r0);
				break;
		}
	}
	return code;
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
	MonoInst *last_ins = NULL;
	guint last_offset = 0;
	int max_len, cpos, src2;

	if (cfg->opt & MONO_OPT_PEEPHOLE)
		peephole_pass (cfg, bb);

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

	ins = bb->code;
	while (ins) {
		offset = code - cfg->native_code;

		max_len = ((guint8 *)ins_spec [ins->opcode])[MONO_INST_LEN];

		if (offset > (cfg->code_size - max_len - 16)) {
			cfg->code_size *= 2;
			cfg->native_code = g_realloc (cfg->native_code, cfg->code_size);
			code = cfg->native_code + offset;
		}

		mono_debug_record_line_number (cfg, ins, offset);

		switch (ins->opcode) {
		case OP_STOREI1_MEMBASE_IMM: {
			s390_lhi (code, s390_r0, ins->inst_imm);
			if (s390_is_uimm12(ins->inst_offset))
				s390_stc (code, s390_r0, 0, ins->inst_destbasereg, ins->inst_offset);
			else {
				s390_basr (code, s390_r13, 0);
				s390_j    (code, 4);
				s390_word (code, ins->inst_offset);
				s390_l    (code, s390_r13, 0, s390_r13, 4);
				s390_stc  (code, s390_r0, s390_r13, ins->inst_destbasereg, 0);
			}
		}
			break;
		case OP_STOREI2_MEMBASE_IMM: {
			s390_lhi (code, s390_r0, ins->inst_imm);
			if (s390_is_uimm12(ins->inst_offset)) {
				s390_sth (code, s390_r0, 0, ins->inst_destbasereg, ins->inst_offset);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j    (code, 4);
				s390_word (code, ins->inst_offset);
				s390_l    (code, s390_r13, 0, s390_r13, 4);
				s390_sth  (code, s390_r0, s390_r13, ins->inst_destbasereg, 0);
			}
		}
			break;
		case OP_STORE_MEMBASE_IMM:
		case OP_STOREI4_MEMBASE_IMM: {
			if (s390_is_imm16(ins->inst_imm)) {
				s390_lhi  (code, s390_r0, ins->inst_imm);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 4);
				s390_word (code, ins->inst_imm);
				s390_l	  (code, s390_r0, 0, s390_r13, 4);
			}
			if (s390_is_uimm12(ins->inst_offset)) {
				s390_st  (code, s390_r0, 0, ins->inst_destbasereg, ins->inst_offset);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j    (code, 4);
				s390_word (code, ins->inst_offset);
				s390_l    (code, s390_r13, 0, s390_r13, 4);
				s390_st   (code, s390_r0, s390_r13, ins->inst_destbasereg, 0);
			}
		}
			break;
		case OP_STOREI1_MEMBASE_REG: {
			if (s390_is_uimm12(ins->inst_offset)) {
				s390_stc  (code, ins->sreg1, 0, ins->inst_destbasereg, ins->inst_offset);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j    (code, 4);
				s390_word (code, ins->inst_offset);
				s390_l    (code, s390_r13, 0, s390_r13, 4);
				s390_stc  (code, ins->sreg1, s390_r13, ins->inst_destbasereg, 0);
			}
		}
			break;
		case OP_STOREI2_MEMBASE_REG: {
			if (s390_is_uimm12(ins->inst_offset)) {
				s390_sth  (code, ins->sreg1, 0, ins->inst_destbasereg, ins->inst_offset);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j    (code, 4);
				s390_word (code, ins->inst_offset);
				s390_l    (code, s390_r13, 0, s390_r13, 4);
				s390_sth  (code, ins->sreg1, s390_r13, ins->inst_destbasereg, 0);
			}
		}
			break;
		case OP_STORE_MEMBASE_REG:
		case OP_STOREI4_MEMBASE_REG: {
			if (s390_is_uimm12(ins->inst_offset)) {
				s390_st   (code, ins->sreg1, 0, ins->inst_destbasereg, ins->inst_offset);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j    (code, 4);
				s390_word (code, ins->inst_offset);
				s390_l    (code, s390_r13, 0, s390_r13, 4);
				s390_st   (code, ins->sreg1, s390_r13, ins->inst_destbasereg, 0);
			}
		}
			break;
		case CEE_LDIND_I:
		case CEE_LDIND_I4:
		case CEE_LDIND_U4: {
			s390_basr (code, s390_r13, 0);
			s390_j	  (code, 4);
			s390_word (code, ins->inst_p0);
			s390_l	  (code, s390_r13, 0, s390_r13, 4);
			s390_l	  (code, ins->dreg, 0, s390_r13, 0);
		}
			break;
		case OP_LOADU4_MEM:
			g_assert_not_reached ();
			break;
		case OP_LOAD_MEMBASE:
		case OP_LOADI4_MEMBASE:
		case OP_LOADU4_MEMBASE: {
			if (s390_is_uimm12(ins->inst_offset))
				s390_l    (code, ins->dreg, 0, ins->inst_basereg, ins->inst_offset);
			else {
				if (s390_is_imm16(ins->inst_offset)) {
					s390_lhi (code, s390_r13, ins->inst_offset);
					s390_l   (code, ins->dreg, s390_r13, ins->inst_basereg, 0);
				} else {
					s390_basr (code, s390_r13, 0);
					s390_j    (code, 4);
					s390_word (code, ins->inst_offset);
					s390_l    (code, s390_r13, 0, s390_r13, 4);
					s390_l    (code, ins->dreg, s390_r13, ins->inst_basereg, 0);
				}
			}
		}
			break;
		case OP_LOADU1_MEMBASE: {
			s390_lhi (code, s390_r0, 0);
			if (s390_is_uimm12(ins->inst_offset))
				s390_ic   (code, s390_r0, 0, ins->inst_basereg, ins->inst_offset);
			else {
				s390_basr (code, s390_r13, 0);
				s390_j    (code, 4);
				s390_word (code, ins->inst_offset);
				s390_l    (code, s390_r13, 0, s390_r13, 4);
				s390_ic   (code, s390_r0, s390_r13, ins->inst_basereg, 0);
			}
			s390_lr   (code, ins->dreg, s390_r0);
		}
			break;
		case OP_LOADI1_MEMBASE: {
			s390_lhi (code, s390_r0, 0);
			if (s390_is_uimm12(ins->inst_offset))
				s390_ic   (code, s390_r0, 0, ins->inst_basereg, ins->inst_offset);
			else {
				s390_basr (code, s390_r13, 0);
				s390_j    (code, 4);
				s390_word (code, ins->inst_offset);
				s390_l    (code, s390_r13, 0, s390_r13, 4);
				s390_ic   (code, s390_r0, s390_r13, ins->inst_basereg, 0);
			}
			s390_lhi  (code, s390_r13, 0x80);
			s390_nr   (code, s390_r13, s390_r0);
			s390_jz   (code, 5);
			s390_lhi  (code, s390_r13, 0xff00);
			s390_or   (code, s390_r0, s390_r13);
			s390_lr	  (code, ins->dreg, s390_r0);
		}
			break;
		case OP_LOADU2_MEMBASE: {
			s390_lhi (code, s390_r0, 0);
			if (s390_is_uimm12(ins->inst_offset))
				s390_icm  (code, s390_r0, 3, ins->inst_basereg, ins->inst_offset);
			else {
				s390_basr (code, s390_r13, 0);
				s390_j    (code, 4);
				s390_word (code, ins->inst_offset);
				s390_l    (code, s390_r13, 0, s390_r13, 4);
				s390_ar   (code, s390_r13, ins->inst_basereg);
				s390_icm  (code, s390_r0, 3, s390_r13, 0);
			}
			s390_lr  (code, ins->dreg, s390_r0);
		}
			break;
		case OP_LOADI2_MEMBASE: {
			s390_lhi (code, s390_r0, 0);
			if (s390_is_uimm12(ins->inst_offset))
				s390_lh   (code, s390_r0, 0, ins->inst_basereg, ins->inst_offset);
			else {
				s390_basr (code, s390_r13, 0);
				s390_j    (code, 4);
				s390_word (code, ins->inst_offset);
				s390_l    (code, s390_r13, 0, s390_r13, 4);
				s390_lh   (code, s390_r0, s390_r13, ins->inst_basereg, 0);
			}
			s390_lr  (code, ins->dreg, s390_r0);
		}
			break;
		case CEE_CONV_I1: {
			s390_lhi  (code, s390_r0, 0x80);
			if (ins->dreg != ins->sreg1) {
				s390_lr	  (code, ins->dreg, ins->sreg1);
			}
			s390_nr   (code, s390_r0, ins->sreg1);
			s390_jz   (code, 7);
			s390_lhi  (code, s390_r13, -1);
			s390_sll  (code, s390_r13, 0, 8);
			s390_or	  (code, ins->dreg, s390_r13);
		}
			break;
		case CEE_CONV_I2: {
			s390_lhi  (code, s390_r0, 0x80);
			s390_sll  (code, s390_r0, 0, 8);
			if (ins->dreg != ins->sreg1) {
				s390_lr   (code, ins->dreg, ins->sreg1);
			}
			s390_nr   (code, s390_r0, ins->sreg1);
			s390_jz   (code, 7);
			s390_lhi  (code, s390_r13, -1);
			s390_sll  (code, s390_r13, 0, 16);
			s390_or	  (code, ins->dreg, s390_r13);
		}
			break;
		case CEE_CONV_U1: {
			s390_lhi  (code, s390_r0, 0xff);
			if (ins->dreg != ins->sreg1) {
				s390_lr	  (code, ins->dreg, ins->sreg1);
			}
			s390_nr	  (code, ins->dreg, s390_r0);
		}
			break;
		case CEE_CONV_U2: {
			s390_lhi  (code, s390_r0, -1);
			s390_sll  (code, s390_r0, 0, 16);
			s390_srl  (code, s390_r0, 0, 16);
			if (ins->dreg != ins->sreg1) {
				s390_lr	  (code, ins->dreg, ins->sreg1);
			}
			s390_nr	  (code, ins->dreg, s390_r0);
		}
			break;
		case OP_COMPARE: {
			if ((ins->next) && 
			    (((ins->next->opcode >= CEE_BNE_UN) &&
			      (ins->next->opcode <= CEE_BLT_UN)) || 
			     ((ins->next->opcode >= OP_COND_EXC_NE_UN) &&
			      (ins->next->opcode <= OP_COND_EXC_LT_UN)) ||
			     ((ins->next->opcode == OP_CLT_UN) ||
			      (ins->next->opcode == OP_CGT_UN))))
				s390_clr  (code, ins->sreg1, ins->sreg2);
			else
				s390_cr   (code, ins->sreg1, ins->sreg2);
		}
			break;
		case OP_COMPARE_IMM: {
			if (s390_is_imm16 (ins->inst_imm)) {
				s390_lhi  (code, s390_r0, ins->inst_imm);
				if ((ins->next) && 
				    (((ins->next->opcode >= CEE_BNE_UN) &&
				      (ins->next->opcode <= CEE_BLT_UN)) || 
				     ((ins->next->opcode >= OP_COND_EXC_NE_UN) &&
				      (ins->next->opcode <= OP_COND_EXC_LT_UN)) ||
				     ((ins->next->opcode == OP_CLT_UN) ||
				      (ins->next->opcode == OP_CGT_UN))))
					s390_clr  (code, ins->sreg1, s390_r0);
				else
					s390_cr   (code, ins->sreg1, s390_r0);
			}
			else {
				s390_basr (code, s390_r13, 0);
				s390_j    (code, 4);
				s390_word (code, ins->inst_imm);
				if ((ins->next) && 
				    (((ins->next->opcode >= CEE_BNE_UN) &&
				      (ins->next->opcode <= CEE_BLT_UN)) || 
				     ((ins->next->opcode >= OP_COND_EXC_NE_UN) &&
				      (ins->next->opcode <= OP_COND_EXC_LT_UN)) ||
				     ((ins->next->opcode == OP_CLT_UN) ||
				      (ins->next->opcode == OP_CGT_UN))))
					s390_cl   (code, ins->sreg1, 0, s390_r13, 4);
				else
					s390_c 	  (code, ins->sreg1, 0, s390_r13, 4);
			}
		}
			break;
		case OP_X86_TEST_NULL: {
			s390_ltr (code, ins->sreg1, ins->sreg1);
		}
			break;
		case CEE_BREAK: {
			mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_ABS, mono_arch_break);
                        s390_brasl (code, s390_r14, 0);
		}
			break;
		case OP_ADDCC: {
			CHECK_SRCDST_COM;
			s390_alr  (code, ins->dreg, src2);
		}
			break;
		case CEE_ADD: {
			CHECK_SRCDST_COM;
			s390_ar   (code, ins->dreg, src2);
		}
			break;
		case OP_ADC: {
			CHECK_SRCDST_COM;
			s390_alcr (code, ins->dreg, src2);
		}
			break;
		case OP_ADD_IMM: {
			if (ins->dreg != ins->sreg1) {
				s390_lr	  (code, ins->dreg, ins->sreg1);
			}
			if ((ins->next) &&
			    (ins->next->opcode == OP_ADC_IMM)) {
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 4);
				s390_word (code, ins->inst_imm);
				s390_a (code, ins->dreg, 0, s390_r13, 4);
			} else {
				if (s390_is_imm16 (ins->inst_imm)) {
					s390_ahi  (code, ins->dreg, ins->inst_imm);
				} else {
					s390_basr (code, s390_r13, 0);
					s390_j	  (code, 4);
					s390_word (code, ins->inst_imm);
					s390_a (code, ins->dreg, 0, s390_r13, 4);
				}
			}
		}
			break;
		case OP_ADDCC_IMM: {
			if ((ins->next) &&
			    (ins->next->opcode == OP_ADC_IMM)) {
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 4);
				s390_word (code, ins->inst_imm);
				if (ins->dreg != ins->sreg1) {
					s390_lr	  (code, ins->dreg, ins->sreg1);
				}
				s390_al   (code, ins->dreg, 0, s390_r13, 4);
			} else {
				if (s390_is_imm16 (ins->inst_imm)) {
					if (ins->dreg != ins->sreg1) {
						s390_lr	  (code, ins->dreg, ins->sreg1);
					}
					s390_lhi  (code, s390_r0, ins->inst_imm);
					s390_alcr (code, ins->dreg, s390_r0);
				} else {
					s390_basr (code, s390_r13, 0);
					s390_j	  (code, 4);
					s390_word (code, ins->inst_imm);
					if (ins->dreg != ins->sreg1) {
						s390_lr	  (code, ins->dreg, ins->sreg1);
					}
					s390_al   (code, ins->dreg, 0, s390_r13, 4);
				}
			}
		}
			break;
		case OP_ADC_IMM: {
			if (s390_is_imm16 (ins->inst_imm)) {
				if (ins->dreg != ins->sreg1) {
					s390_lr   (code, ins->dreg, ins->sreg1);
				} 
				s390_lhi  (code, s390_r0, ins->inst_imm);
				s390_alcr (code, ins->dreg, s390_r0);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 4);
				s390_word (code, ins->inst_imm);
				s390_l    (code, s390_r13, 0, s390_r13, 4);
				s390_alcr (code, ins->dreg, s390_r13);
			}
		}
			break;
		case CEE_ADD_OVF: {
			CHECK_SRCDST_COM;
			s390_ar   (code, ins->dreg, src2);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_OV, "OverflowException");
		}
			break;
		case CEE_ADD_OVF_UN: {
			CHECK_SRCDST_COM;
			s390_alr  (code, ins->dreg, src2);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_CY, "OverflowException");
		}
			break;
		case OP_LADD: {
			short int *o[1];
			s390_alr  (code, s390_r0, ins->sreg1);
			s390_jnc  (code, 4);
			s390_ahi  (code, s390_r1, 1);
			s390_ar   (code, s390_r1, ins->sreg2);
			s390_lr   (code, ins->dreg, s390_r0);
			s390_lr   (code, ins->dreg+1, s390_r1);
		}
			break;
		case OP_LADD_OVF: {
			short int *o[1];
			s390_alr  (code, s390_r0, ins->sreg1);
			s390_jnc  (code, 0); CODEPTR(code, o[0]);
			s390_ahi  (code, s390_r1, 1);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_OV, "OverflowException");
			PTRSLOT   (code, o[0]);
			s390_ar   (code, s390_r1, ins->sreg2);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_OV, "OverflowException");
			s390_lr   (code, ins->dreg, s390_r0);
			s390_lr   (code, ins->dreg+1, s390_r1);
		}
			break;
		case OP_LADD_OVF_UN: {
			s390_alr  (code, s390_r0, ins->sreg1);
			s390_alcr (code, s390_r1, ins->sreg2);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_CY, "OverflowException");
			s390_lr   (code, ins->dreg, s390_r0);
			s390_lr   (code, ins->dreg+1, s390_r1);
		}
			break;
		case OP_ADD_OVF_CARRY: {
			CHECK_SRCDST_COM;
			s390_lhi  (code, s390_r0, 0);
			s390_lr   (code, s390_r1, s390_r0);
			s390_alcr (code, s390_r0, s390_r1);
			s390_ar   (code, ins->dreg, src2);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_OV, "OverflowException");
			s390_ar   (code, ins->dreg, s390_r0);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_OV, "OverflowException");
		}
			break;
		case OP_ADD_OVF_UN_CARRY: {
			CHECK_SRCDST_COM;
			s390_alcr (code, ins->dreg, src2);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_CY, "OverflowException");
		}
			break;
		case OP_SUBCC: {
			CHECK_SRCDST_NCOM;
			s390_slr (code, ins->dreg, src2);
		}
			break;
		case CEE_SUB: {
			CHECK_SRCDST_NCOM;
			s390_sr   (code, ins->dreg, src2);
		}
			break;
		case OP_SBB: {
			CHECK_SRCDST_NCOM;
			s390_slbr (code, ins->dreg, src2);
		}
			break;
		case OP_SUBCC_IMM: {
			if (s390_is_imm16 (-ins->inst_imm)) {
				if (ins->dreg != ins->sreg1) {
					s390_lr   (code, ins->dreg, ins->sreg1);
				}
				s390_lhi  (code, s390_r0, ins->inst_imm);
				s390_slr  (code, ins->dreg, s390_r0);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 4);
				s390_word (code, ins->inst_imm);
				if (ins->dreg != ins->sreg1) {
					s390_lr	  (code, ins->dreg, ins->sreg1);
				}
				s390_sl   (code, ins->dreg, 0, s390_r13, 4);
			}
		}
			break;
		case OP_SUB_IMM: {
			if (s390_is_imm16 (-ins->inst_imm)) {
				if (ins->dreg != ins->sreg1) {
					s390_lr   (code, ins->dreg, ins->sreg1);
				}
				s390_ahi  (code, ins->dreg, -ins->inst_imm);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 4);
				s390_word (code, ins->inst_imm);
				if (ins->dreg != ins->sreg1) {
					s390_lr	  (code, ins->dreg, ins->sreg1);
				}
				s390_s    (code, ins->dreg, 0, s390_r13, 4);
			}
		}
			break;
		case OP_SBB_IMM: {
			if (ins->dreg != ins->sreg1) {
				s390_lr    (code, ins->dreg, ins->sreg1);
			}
			if (s390_is_imm16 (-ins->inst_imm)) {
				s390_lhi   (code, s390_r0, ins->inst_imm);
				s390_slbr  (code, ins->dreg, s390_r0);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 4);
				s390_word (code, ins->inst_imm);
				s390_slb  (code, ins->dreg, 0, s390_r13, 4);
			}
		}
			break;
		case CEE_SUB_OVF: {
			CHECK_SRCDST_NCOM;
			s390_sr   (code, ins->dreg, src2);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_OV, "OverflowException");
		}
			break;
		case CEE_SUB_OVF_UN: {
			CHECK_SRCDST_NCOM;
			s390_slr  (code, ins->dreg, src2);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_NC, "OverflowException");
		}
			break;
		case OP_LSUB: {
			s390_lr   (code, s390_r14, ins->sreg2);
			s390_slr  (code, s390_r0, ins->sreg1);
			s390_jnl  (code, 4);
			s390_ahi  (code, s390_r14, 1);
			s390_sr   (code, s390_r1, s390_r14);
			s390_lr   (code, ins->dreg, s390_r0);
			s390_lr   (code, ins->dreg+1, s390_r1);
		}
			break;
		case OP_LSUB_OVF: {
			short int *o[1];
			s390_lr   (code, s390_r14, ins->sreg2);
			s390_slr  (code, s390_r0, ins->sreg1);
			s390_jnl  (code, 0); CODEPTR(code, o[0]);
			s390_ahi  (code, s390_r14, 1);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_OV, "OverflowException");
			PTRSLOT   (code, o[0]);
			s390_sr   (code, s390_r1, s390_r14);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_OV, "OverflowException");
			s390_lr   (code, ins->dreg, s390_r0);
			s390_lr   (code, ins->dreg+1, s390_r1);
		}
			break;
		case OP_LSUB_OVF_UN: {
			s390_slr  (code, s390_r0, ins->sreg1);
			s390_slbr (code, s390_r1, ins->sreg2);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_LT, "OverflowException");
			s390_lr   (code, ins->dreg, s390_r0);
			s390_lr   (code, ins->dreg+1, s390_r1);
		}
			break;
		case OP_SUB_OVF_CARRY: {
			CHECK_SRCDST_NCOM;
			s390_lhi  (code, s390_r0, 0);
			s390_lr   (code, s390_r1, s390_r0);
			s390_slbr (code, s390_r0, s390_r1);
			s390_sr   (code, ins->dreg, src2);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_OV, "OverflowException");
			s390_ar   (code, ins->dreg, s390_r0);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_OV, "OverflowException");
		}
			break;
		case OP_SUB_OVF_UN_CARRY: {
			CHECK_SRCDST_NCOM;
			s390_slbr (code, ins->dreg, src2);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_NC, "OverflowException");
		}
			break;
		case CEE_AND: {
			if (ins->sreg1 == ins->dreg) {
				s390_nr   (code, ins->dreg, ins->sreg2);
			} 
			else { 
				if (ins->sreg2 == ins->dreg) { 
					s390_nr  (code, ins->dreg, ins->sreg1);
				}
				else { 
					s390_lr  (code, ins->dreg, ins->sreg1);
					s390_nr  (code, ins->dreg, ins->sreg2);
				}
			}
		}
			break;
		case OP_AND_IMM: {
			if (s390_is_imm16 (ins->inst_imm)) {
				s390_lhi  (code, s390_r0, ins->inst_imm);
				if (ins->dreg != ins->sreg1) {
					s390_lr	  (code, ins->dreg, ins->sreg1);
				}
				s390_nr	  (code, ins->dreg, s390_r0);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 4);
				s390_word (code, ins->inst_imm);
				if (ins->dreg != ins->sreg1) {
					s390_lr	  (code, ins->dreg, ins->sreg1);
				}
				s390_n 	  (code, ins->dreg, 0, s390_r13, 4);
			}
		}
			break;
		case CEE_DIV: {
			s390_lr	  (code, s390_r0, ins->sreg1);
			s390_srda (code, s390_r0, 0, 32);
			s390_dr   (code, s390_r0, ins->sreg2);
			s390_lr   (code, ins->dreg, s390_r1);
		}
			break;
		case CEE_DIV_UN: {
			s390_lr	  (code, s390_r0, ins->sreg1);
			s390_srdl (code, s390_r0, 0, 32);
			s390_dlr  (code, s390_r0, ins->sreg2);
			s390_lr   (code, ins->dreg, s390_r1);
		}
			break;
		case OP_DIV_IMM: {
			if (s390_is_imm16 (ins->inst_imm)) {
				s390_lhi  (code, s390_r13, ins->inst_imm);
				s390_lr   (code, s390_r0, ins->sreg1);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j    (code, 4);
				s390_word (code, ins->inst_imm);
				s390_lr   (code, s390_r0, ins->sreg1);
				s390_l	  (code, s390_r13, 0, s390_r13, 4);
			}
			s390_srda (code, s390_r0, 0, 32);
			s390_dr   (code, s390_r0, s390_r13);
			s390_lr   (code, ins->dreg, s390_r1);
		}
			break;
		case CEE_REM: {
			s390_lr	  (code, s390_r0, ins->sreg1);
			s390_srda (code, s390_r0, 0, 32);
			s390_dr   (code, s390_r0, ins->sreg2);
			s390_lr   (code, ins->dreg, s390_r0);
			break;
		case CEE_REM_UN:
			s390_lr	  (code, s390_r0, ins->sreg1);
			s390_srdl (code, s390_r0, 0, 32);
			s390_dlr  (code, s390_r0, ins->sreg2);
			s390_lr   (code, ins->dreg, s390_r0);
		}
			break;
		case OP_REM_IMM: {
			if (s390_is_imm16 (ins->inst_imm)) {
				s390_lhi  (code, s390_r13, ins->inst_imm);
				s390_lr   (code, s390_r0, ins->sreg1);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 4);
				s390_word (code, ins->inst_imm);
				s390_lr   (code, s390_r0, ins->sreg1);
				s390_l	  (code, s390_r13, 0, s390_r13, 4);
			}
			s390_srda (code, s390_r0, 0, 32);
			s390_dr   (code, s390_r0, s390_r13);
			s390_lr   (code, ins->dreg, s390_r0);
		}
			break;
		case CEE_OR: {
			if (ins->sreg1 == ins->dreg) {
				s390_or   (code, ins->dreg, ins->sreg2);
			} 
			else { 
				if (ins->sreg2 == ins->dreg) { 
					s390_or  (code, ins->dreg, ins->sreg1);
				}
				else { 
					s390_lr  (code, ins->dreg, ins->sreg1);
					s390_or  (code, ins->dreg, ins->sreg2);
				}
			}
		}
			break;
		case OP_OR_IMM: {
			if (s390_is_imm16 (ins->inst_imm)) {
				s390_lhi  (code, s390_r0, ins->inst_imm);
				if (ins->dreg != ins->sreg1) {
					s390_lr	  (code, ins->dreg, ins->sreg1);
				}
				s390_or	  (code, ins->dreg, s390_r0);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j    (code, 4);
				s390_word (code, ins->inst_imm);
				if (ins->dreg != ins->sreg1) {
					s390_lr	  (code, ins->dreg, ins->sreg1);
				}
				s390_o 	  (code, ins->dreg, 0, s390_r13, 4);
			}
		}
			break;
		case CEE_XOR: {
			if (ins->sreg1 == ins->dreg) {
				s390_xr   (code, ins->dreg, ins->sreg2);
			} 
			else { 
				if (ins->sreg2 == ins->dreg) { 
					s390_xr  (code, ins->dreg, ins->sreg1);
				}
				else { 
					s390_lr  (code, ins->dreg, ins->sreg1);
					s390_xr  (code, ins->dreg, ins->sreg2);
				}
			}
		}
			break;
		case OP_XOR_IMM: {
			if (s390_is_imm16 (ins->inst_imm)) {
				s390_lhi  (code, s390_r0, ins->inst_imm);
				if (ins->dreg != ins->sreg1) {
					s390_lr	  (code, ins->dreg, ins->sreg1);
				}
				s390_xr	  (code, ins->dreg, s390_r0);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 4);
				s390_word (code, ins->inst_imm);
				if (ins->dreg != ins->sreg1) {
					s390_lr	  (code, ins->dreg, ins->sreg1);
				}
				s390_x 	  (code, ins->dreg, 0, s390_r13, 4);
			}
		}
			break;
		case CEE_SHL: {
			CHECK_SRCDST_NCOM;
			s390_sll  (code, ins->dreg, src2, 0);
		}
			break;
		case OP_SHL_IMM: {
			if (ins->sreg1 != ins->dreg) {
				s390_lr   (code, ins->dreg, ins->sreg1);
			}
			s390_sll  (code, ins->dreg, 0, (ins->inst_imm & 0x1f));
		}
			break;
		case CEE_SHR: {
			CHECK_SRCDST_NCOM;
			s390_sra  (code, ins->dreg, src2, 0);
		}
			break;
		case OP_SHR_IMM: {
			if (ins->sreg1 != ins->dreg) {
				s390_lr   (code, ins->dreg, ins->sreg1);
			}
			s390_sra  (code, ins->dreg, 0, (ins->inst_imm & 0x1f));
		}
			break;
		case OP_SHR_UN_IMM: {
			if (ins->sreg1 != ins->dreg) {
				s390_lr   (code, ins->dreg, ins->sreg1);
			}
			s390_srl  (code, ins->dreg, 0, (ins->inst_imm & 0x1f));
		}
			break;
		case CEE_SHR_UN: {
			CHECK_SRCDST_NCOM;
			s390_srl  (code, ins->dreg, src2, 0);
		}
			break;
		case CEE_NOT: {
			if (ins->sreg1 != ins->dreg) {
				s390_lr   (code, ins->dreg, ins->sreg1);
			}
			s390_lhi (code, s390_r0, -1);
			s390_xr  (code, ins->dreg, s390_r0);
		}
			break;
		case CEE_NEG: {
			s390_lcr (code, ins->dreg, ins->sreg1);
		}
			break;
		case CEE_MUL: {
			if (ins->sreg1 == ins->dreg) {
				s390_msr  (code, ins->dreg, ins->sreg2);
			} 
			else { 
				if (ins->sreg2 == ins->dreg) { 
					s390_msr (code, ins->dreg, ins->sreg1);
				}
				else { 
					s390_lr  (code, ins->dreg, ins->sreg1);
					s390_msr (code, ins->dreg, ins->sreg2);
				}
			}
		}
			break;
		case OP_MUL_IMM: {
			if (s390_is_imm16 (ins->inst_imm)) {
				s390_lhi  (code, s390_r13, ins->inst_imm);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 4);
				s390_word (code, ins->inst_imm);
//				if (ins->dreg != ins->sreg1) {
//					s390_lr   (code, ins->dreg, ins->sreg1);
//				}
				s390_l    (code, s390_r13, 0, s390_r13, 4);
			}
			if (ins->dreg != ins->sreg1) {
				s390_lr   (code, ins->dreg, ins->sreg1);
			}
			s390_msr  (code, ins->dreg, s390_r13);
		}
			break;
		case CEE_MUL_OVF: {
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
			s390_lr   (code, ins->dreg, s390_r1);
		}
			break;
		case CEE_MUL_OVF_UN: {
			s390_lhi  (code, s390_r0, 0);
			s390_lr   (code, s390_r1, ins->sreg1);
			s390_mlr  (code, s390_r0, ins->sreg2);
			s390_ltr  (code, s390_r0, s390_r0);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_NZ, "OverflowException");
			s390_lr   (code, ins->dreg, s390_r1);
		}
			break;
		case OP_LMUL: {
			s390_l    (code, s390_r0, 0, ins->sreg1, 4);
			s390_srda (code, s390_r0, 0, 32);
			s390_m	  (code, s390_r0, 0, ins->sreg2, 4);
			s390_l    (code, s390_r0, 0, ins->sreg1, 4);
			s390_srl  (code, s390_r0, 0, 31);
			s390_a    (code, s390_r0, 0, ins->sreg1, 0);
			s390_l    (code, s390_r13, 0, ins->sreg2, 0);
			s390_srl  (code, s390_r13, 0, 31);
			s390_ms   (code, s390_r13, 0, ins->sreg1, 4);
			s390_ar   (code, s390_r0, s390_r13);
			s390_st   (code, s390_r0, 0, ins->dreg, 0);
			s390_st   (code, s390_r1, 0, ins->dreg, 4);
		}
			break;	
		case OP_ICONST:
		case OP_SETREGIMM: {
			if (s390_is_imm16(ins->inst_c0)) {
				s390_lhi  (code, ins->dreg, ins->inst_c0);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 4);
				s390_word (code, ins->inst_c0);
				s390_l    (code, ins->dreg, 0, s390_r13, 4);
			}
		}
			break;
		case OP_AOTCONST: {
			s390_basr (code, s390_r13, 0);
			s390_j	  (code, 4);
			mono_add_patch_info (cfg, code - cfg->native_code, 
				(MonoJumpInfoType)ins->inst_i1, ins->inst_p0);
			s390_word (code, 0);
			s390_l    (code,ins->dreg, 0, s390_r13, 4);
		}
			break;
		case CEE_CONV_I4:
		case CEE_CONV_U4:
		case OP_MOVE:
		case OP_SETREG: {
			if (ins->dreg != ins->sreg1) {
				s390_lr (code, ins->dreg, ins->sreg1);
			}
		}
			break;
		case OP_SETLRET: {
			int saved = ins->sreg1;
			if (ins->sreg1 == s390_r2) {
				s390_lr (code, s390_r0, ins->sreg1);
				saved = s390_r0;
			}
			if (ins->sreg2 != s390_r2)
				s390_lr (code, s390_r2, ins->sreg2);
			if (saved != s390_r3)
				s390_lr (code, s390_r3, saved);
			break;
		}
		case OP_SETFREG:
		case OP_FMOVE: {
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
				s390_lhi (code, s390_r13, ins->inst_offset);
			} else {
				s390_bras (code, s390_r13, 0);
				s390_j	  (code, 4);
				s390_word (code, ins->inst_offset);
				s390_l    (code, s390_r13, 0, s390_r13, 4);
			}
			s390_ear (code, s390_r1, 0);
			s390_l   (code, ins->dreg, s390_r13, s390_r1, 0);
		}
			break;
		case OP_FCONV_TO_R4: {
			NOT_IMPLEMENTED("OP_FCONV_TO_R4");
			if ((ins->next) &&
			     (ins->next->opcode != OP_FMOVE) &&
			     (ins->next->opcode != OP_STORER4_MEMBASE_REG))
				s390_ledbr (code, ins->dreg, ins->sreg1);
		}
			break;
		case CEE_JMP: {
			if (cfg->method->save_lmf)
				restoreLMF(code, cfg->frame_reg, cfg->stack_usage);

			if (cfg->flags & MONO_CFG_HAS_TAIL) {
				code = emit_load_volatile_registers(code, cfg);
			}

			code = backUpStackPtr(cfg, code, STK_BASE);
			s390_l   (code, s390_r14, 0, STK_BASE, S390_RET_ADDR_OFFSET);
			mono_add_patch_info (cfg, code - cfg->native_code,
					     MONO_PATCH_INFO_METHOD_JUMP,
					     ins->inst_p0);
			s390_jcl (code, S390_CC_UN, 0);
		}
			break;
		case OP_CHECK_THIS: {
			/* ensure ins->sreg1 is not NULL */
			s390_icm (code, s390_r0, 15, ins->sreg1, 0);
		}
			break;
		case OP_ARGLIST: {
			int offset = cfg->sig_cookie + cfg->stack_usage;

			if (s390_is_imm16 (offset))
				s390_lhi  (code, s390_r0, offset);
			else {
				s390_basr (code, s390_r13, 0);
				s390_j    (code, 4);
				s390_word (code, offset);
				s390_l    (code, s390_r0, 0, s390_r13, 0);
			}
			s390_ar   (code, s390_r0, cfg->frame_reg);
			s390_st	  (code, s390_r0, 0, ins->sreg1, 0);
		}
			break;
		case OP_FCALL: {
			call = (MonoCallInst*)ins;
			if (ins->flags & MONO_INST_HAS_METHOD)
				mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_METHOD, 
						     call->method);
			else
				mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_ABS, 
						     call->fptr);
			s390_brasl (code, s390_r14, 0);
			if (call->signature->ret->type == MONO_TYPE_R4)
				s390_ldebr (code, s390_f0, s390_f0);
		}
			break;
		case OP_LCALL:
		case OP_VCALL:
		case OP_VOIDCALL:
		case CEE_CALL: {
			call = (MonoCallInst*)ins;
			if (ins->flags & MONO_INST_HAS_METHOD)
				mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_METHOD, call->method);
			else
				mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_ABS, call->fptr);
			s390_brasl (code, s390_r14, 0);
		}
			break;
		case OP_FCALL_REG: {
			call = (MonoCallInst*)ins;
			s390_lr   (code, s390_r1, ins->sreg1);
			s390_basr (code, s390_r14, s390_r1);
			if (call->signature->ret->type == MONO_TYPE_R4)
				s390_ldebr (code, s390_f0, s390_f0);
		}
			break;
		case OP_LCALL_REG:
		case OP_VCALL_REG:
		case OP_VOIDCALL_REG:
		case OP_CALL_REG: {
			s390_lr   (code, s390_r1, ins->sreg1);
			s390_basr (code, s390_r14, s390_r1);
		}
			break;
		case OP_FCALL_MEMBASE: {
			call = (MonoCallInst*)ins;
			s390_l    (code, s390_r1, 0, ins->sreg1, ins->inst_offset);
			s390_basr (code, s390_r14, s390_r1);
			if (call->signature->ret->type == MONO_TYPE_R4)
				s390_ldebr (code, s390_f0, s390_f0);
		}
			break;
		case OP_LCALL_MEMBASE:
		case OP_VCALL_MEMBASE:
		case OP_VOIDCALL_MEMBASE:
		case OP_CALL_MEMBASE: {
			s390_l    (code, s390_r1, 0, ins->sreg1, ins->inst_offset);
			s390_basr (code, s390_r14, s390_r1);
		}
			break;
		case OP_OUTARG: 
			g_assert_not_reached ();
			break;
		case OP_LOCALLOC: {
			/*------------------------------------------*/
			/* To allocate space on the stack we have   */
			/* to allow room for parameters passed in   */
			/* calls, the backchain pointer and round   */
			/* it to our stack alignment requirements   */
			/*------------------------------------------*/
			int alloca_skip = S390_MINIMAL_STACK_SIZE + cfg->param_area;
			int area_offset = S390_ALIGN(alloca_skip, S390_STACK_ALIGNMENT);
			s390_lr   (code, s390_r1, ins->sreg1);
			if (ins->flags & MONO_INST_INIT)
				s390_lr   (code, s390_r0, ins->sreg1);
			s390_ahi  (code, s390_r1, 14);
			s390_srl  (code, s390_r1, 0, 3);
			s390_sll  (code, s390_r1, 0, 3);
			if (cfg->method->save_lmf) {
				/*----------------------------------*/
				/* we have to adjust lmf ebp value  */ 
				/*----------------------------------*/
				int lmfOffset = cfg->stack_usage - sizeof(MonoLMF);	
											
				s390_lr (code, s390_r13, cfg->frame_reg);
				if (s390_is_uimm16(lmfOffset))
					s390_ahi   (code, s390_r13, lmfOffset);	
				else {
					s390_basr (code, s390_r14, 0);
					s390_j    (code, 4);
					s390_word (code, lmfOffset);
					s390_a    (code, s390_r13, 0, s390_r14, 4);
				}
				s390_lr (code, s390_r14, STK_BASE);
				s390_sr (code, s390_r14, s390_r1);
				s390_st (code, s390_r14, 0, s390_r13, 
					 G_STRUCT_OFFSET(MonoLMF, ebp));	
			}
			s390_l	  (code, s390_r13, 0, STK_BASE, 0);
			s390_sr	  (code, STK_BASE, s390_r1);
			s390_st   (code, s390_r13, 0, STK_BASE, 0);
			s390_la   (code, ins->dreg, 0, STK_BASE, area_offset);
			s390_srl  (code, ins->dreg, 0, 3);
			s390_sll  (code, ins->dreg, 0, 3);
			if (ins->flags & MONO_INST_INIT) {
				s390_lr   (code, s390_r1, s390_r0);
				s390_lr   (code, s390_r0, ins->dreg);
				s390_lr	  (code, s390_r14, s390_r12);
				s390_lhi  (code, s390_r13, 0);
				s390_mvcle(code, s390_r0, s390_r12, 0, 0);
				s390_jo   (code, -2);
				s390_lr   (code, s390_r12, s390_r14);
			}
		}
			break;
		case CEE_RET: {
			s390_br  (code, s390_r14);
		}
			break;
		case CEE_THROW: {
			s390_lr (code, s390_r2, ins->sreg1);
			mono_add_patch_info (cfg, code-cfg->native_code, MONO_PATCH_INFO_INTERNAL_METHOD, 
					     (gpointer)"mono_arch_throw_exception");
			s390_brasl (code, s390_r14, 0);
		}
			break;
		case OP_RETHROW: {
			s390_lr (code, s390_r2, ins->sreg1);
			mono_add_patch_info (cfg, code-cfg->native_code, MONO_PATCH_INFO_INTERNAL_METHOD, 
					     (gpointer)"mono_arch_rethrow_exception");
			s390_brasl (code, s390_r14, 0);
		}
			break;
		case OP_START_HANDLER: {
			if (s390_is_uimm12 (ins->inst_left->inst_offset)) {
				s390_st   (code, s390_r14, 0, 
					   ins->inst_left->inst_basereg, 
					   ins->inst_left->inst_offset);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j    (code, 4);
				s390_word (code, ins->inst_left->inst_offset);
				s390_l    (code, s390_r13, 0, s390_r13, 4);
				s390_st   (code, s390_r14, s390_r13, 
					   ins->inst_left->inst_basereg, 0);
			}
		}
			break;
		case OP_ENDFILTER: {
			if (ins->sreg1 != s390_r2)
				s390_lr (code, s390_r2, ins->sreg1);
			if (s390_is_uimm12 (ins->inst_left->inst_offset)) {
				s390_l  (code, s390_r14, 0, ins->inst_left->inst_basereg,
					 ins->inst_left->inst_offset);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j    (code, 4);
				s390_word (code, ins->inst_left->inst_offset);
				s390_l    (code, s390_r13, 0, s390_r13, 4);
				s390_l    (code, s390_r14, s390_r13, 
					   ins->inst_left->inst_basereg, 0);
			}
			s390_br  (code, s390_r14);
		}
			break;
		case CEE_ENDFINALLY: {
			if (s390_is_uimm12 (ins->inst_left->inst_offset)) {
				s390_l  (code, s390_r14, 0, ins->inst_left->inst_basereg,
					 ins->inst_left->inst_offset);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j    (code, 4);
				s390_word (code, ins->inst_left->inst_offset);
				s390_l    (code, s390_r13, 0, s390_r13, 4);
				s390_l    (code, s390_r14, s390_r13, 
					   ins->inst_left->inst_basereg, 0);
			}
			s390_br  (code, s390_r14);
		}
			break;
		case OP_CALL_HANDLER: {
			mono_add_patch_info (cfg, code-cfg->native_code, 
					     MONO_PATCH_INFO_BB, ins->inst_target_bb);
			s390_brasl (code, s390_r14, 0);
		}
			break;
		case OP_LABEL: {
			ins->inst_c0 = code - cfg->native_code;
		}
			break;
		case CEE_BR: 
			EMIT_UNCOND_BRANCH(ins);
			break;
		case OP_BR_REG: {
			s390_br	 (code, ins->sreg1);
		}
			break;
		case OP_CEQ: {
			s390_lhi (code, ins->dreg, 1);
			s390_jz  (code, 4);
			s390_lhi (code, ins->dreg, 0);
		}
			break;
		case OP_CLT: {
			s390_lhi (code, ins->dreg, 1);
			s390_jl  (code, 4);
			s390_lhi (code, ins->dreg, 0);
		}
			break;
		case OP_CLT_UN: {
			s390_lhi (code, ins->dreg, 1);
			s390_jlo (code, 4);
			s390_lhi (code, ins->dreg, 0);
		}
			break;
		case OP_CGT: {
			s390_lhi (code, ins->dreg, 1);
			s390_jh  (code, 4);
			s390_lhi (code, ins->dreg, 0);
		}
			break;
		case OP_CGT_UN: {
			s390_lhi (code, ins->dreg, 1);
			s390_jho (code, 4);
			s390_lhi (code, ins->dreg, 0);
		}
			break;
		case OP_COND_EXC_EQ:
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_EQ, ins->inst_p1);
			break;
		case OP_COND_EXC_NE_UN:
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_NE, ins->inst_p1);
			break;
		case OP_COND_EXC_LT:
		case OP_COND_EXC_LT_UN:
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_LT, ins->inst_p1);
			break;
		case OP_COND_EXC_GT:
		case OP_COND_EXC_GT_UN:
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_GT, ins->inst_p1);
			break;
		case OP_COND_EXC_GE:
		case OP_COND_EXC_GE_UN:
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_GE, ins->inst_p1);
			break;
		case OP_COND_EXC_LE:
		case OP_COND_EXC_LE_UN:
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_LE, ins->inst_p1);
			break;
		case OP_COND_EXC_OV:
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_OV, ins->inst_p1);
			break;
		case OP_COND_EXC_NO:
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_NO, ins->inst_p1);
			break;
		case OP_COND_EXC_C:
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_CY, ins->inst_p1);
			break;
		case OP_COND_EXC_NC:
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_NC, ins->inst_p1);
			break;
		case CEE_BEQ:
			EMIT_COND_BRANCH (ins, S390_CC_EQ);
			break;	
		case CEE_BNE_UN:
			EMIT_COND_BRANCH (ins, S390_CC_NE);
			break;	
		case CEE_BLT:
		case CEE_BLT_UN:
			EMIT_COND_BRANCH (ins, S390_CC_LT);
			break;	
		case CEE_BGT:
		case CEE_BGT_UN:
			EMIT_COND_BRANCH (ins, S390_CC_GT);
			break;	
		case CEE_BGE:
		case CEE_BGE_UN:
			EMIT_COND_BRANCH (ins, S390_CC_GE);
			break;	
		case CEE_BLE:
		case CEE_BLE_UN:
			EMIT_COND_BRANCH (ins, S390_CC_LE);
			break;

		/* floating point opcodes */
		case OP_R8CONST: {
			if (*((float *) ins->inst_p0) == 0) {
				s390_lzdr (code, ins->dreg);
			} else {
				s390_basr  (code, s390_r13, 0);
				s390_j	   (code, 4);
				s390_word  (code, ins->inst_p0);
				s390_l	   (code, s390_r13, 0, s390_r13, 4);
				s390_ld    (code, ins->dreg, 0, s390_r13, 0);
			}
		}
			break;
		case OP_R4CONST: {
			if (*((float *) ins->inst_p0) == 0) {
				s390_lzdr (code, ins->dreg);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 4);
				s390_word (code, ins->inst_p0);
				s390_l    (code, s390_r13, 0, s390_r13, 4);
				s390_ldeb (code, ins->dreg, 0, s390_r13, 0);
			}
		}
			break;
		case OP_STORER8_MEMBASE_REG: {
			if (s390_is_uimm12(ins->inst_offset)) {
				s390_std  (code, ins->sreg1, 0, ins->inst_destbasereg, ins->inst_offset);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 4);
				s390_word (code, ins->inst_offset);
				s390_l    (code, s390_r13, 0, s390_r13, 4);
				s390_std  (code, ins->sreg1, s390_r13, ins->inst_destbasereg, 0);
			}
		}
			break;
		case OP_LOADR8_MEMBASE: {
			if (s390_is_uimm12(ins->inst_offset)) {
				s390_ld   (code, ins->dreg, 0, ins->inst_basereg, ins->inst_offset);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 4);
				s390_word (code, ins->inst_offset);
				s390_l    (code, s390_r13, 0, s390_r13, 4);
				s390_ld   (code, ins->dreg, s390_r13, ins->inst_basereg, 0);
			}
		}
			break;
		case OP_STORER4_MEMBASE_REG: {
			if (s390_is_uimm12(ins->inst_offset)) {
				s390_ledbr(code, s390_f15, ins->sreg1);
				s390_ste  (code, s390_f15, 0, ins->inst_destbasereg, ins->inst_offset);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 4);
				s390_word (code, ins->inst_offset);
				s390_l    (code, s390_r13, 0, s390_r13, 4);
				s390_ledbr(code, s390_f15, ins->sreg1);
				s390_ste  (code, s390_f15, s390_r13, ins->inst_destbasereg, 0);
			}
		}
			break;
		case OP_LOADR4_MEMBASE: {
			if (s390_is_uimm12(ins->inst_offset)) {
				s390_ldeb (code, ins->dreg, 0, ins->inst_basereg, ins->inst_offset);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j	  (code, 4);
				s390_word (code, ins->inst_offset);
				s390_l    (code, s390_r13, 0, s390_r13, 4);
				s390_ldeb (code, ins->dreg, s390_r13, ins->inst_basereg, 0);
			}
		}
			break;
		case CEE_CONV_R_UN: {
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
		case CEE_CONV_R4: {
			s390_cdfbr (code, ins->dreg, ins->sreg1);
		}
			break;
		case CEE_CONV_R8: {
			s390_cdfbr (code, ins->dreg, ins->sreg1);
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
		case OP_FCONV_TO_U8:
			g_assert_not_reached ();
			/* Implemented as helper calls */
			break;
		case OP_LCONV_TO_R_UN:
			g_assert_not_reached ();
			/* Implemented as helper calls */
			break;
		case OP_LCONV_TO_OVF_I: {
			/* Valid ints: 0xffffffff:8000000 to 00000000:0x7f000000 */
			short int *o[5];
			s390_ltr  (code, ins->sreg1, ins->sreg1);
			s390_jnl  (code, 0); CODEPTR(code, o[0]);
			s390_ltr  (code, ins->sreg2, ins->sreg2);
			s390_jnl  (code, 0); CODEPTR(code, o[1]);
			s390_lhi  (code, s390_r13, -1);
			s390_cr   (code, ins->sreg2, s390_r13);
			s390_jnz  (code, 0); CODEPTR(code, o[2]);
			if (ins->dreg != ins->sreg1)
				s390_lr   (code, ins->dreg, ins->sreg1);
			s390_j	  (code, 0); CODEPTR(code, o[3]);
			PTRSLOT(code, o[0]);
			s390_ltr  (code, ins->sreg2, ins->sreg2);
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
			s390_lhi   (code, ins->dreg, 1);
			s390_je    (code, 4);
			s390_lhi   (code, ins->dreg, 0);
		}
			break;
		case OP_FCLT: {
			s390_cdbr  (code, ins->sreg1, ins->sreg2);
			s390_lhi   (code, ins->dreg, 1);
			s390_jl    (code, 4);
			s390_lhi   (code, ins->dreg, 0);
		}
			break;
		case OP_FCLT_UN: {
			s390_cdbr  (code, ins->sreg1, ins->sreg2);
			s390_lhi   (code, ins->dreg, 1);
			s390_jlo   (code, 4);
			s390_lhi   (code, ins->dreg, 0);
		}
			break;
		case OP_FCGT: {
			s390_cdbr  (code, ins->sreg1, ins->sreg2);
			s390_lhi   (code, ins->dreg, 1);
			s390_jh    (code, 4);
			s390_lhi   (code, ins->dreg, 0);
		}
			break;
		case OP_FCGT_UN: {
			s390_cdbr  (code, ins->sreg1, ins->sreg2);
			s390_lhi   (code, ins->dreg, 1);
			s390_jho   (code, 4);
			s390_lhi   (code, ins->dreg, 0);
		}
			break;
		case OP_FBEQ:
			EMIT_COND_BRANCH (ins, S390_CC_EQ|S390_CC_OV);
			break;
		case OP_FBNE_UN:
			EMIT_COND_BRANCH (ins, S390_CC_NE|S390_CC_OV);
			break;
		case OP_FBLT:
			EMIT_COND_BRANCH (ins, S390_CC_LT);
			break;
		case OP_FBLT_UN:
			EMIT_COND_BRANCH (ins, S390_CC_LT|S390_CC_OV);
			break;
		case OP_FBGT:
			EMIT_COND_BRANCH (ins, S390_CC_GT);
			break;
		case OP_FBGT_UN:
			EMIT_COND_BRANCH (ins, S390_CC_GT|S390_CC_OV);
			break;
		case OP_FBGE:
			EMIT_COND_BRANCH (ins, S390_CC_GE);
			break;
		case OP_FBGE_UN:
			EMIT_COND_BRANCH (ins, S390_CC_GE|S390_CC_OV);
			break;
		case OP_FBLE:
			EMIT_COND_BRANCH (ins, S390_CC_LE);
			break;
		case OP_FBLE_UN:
			EMIT_COND_BRANCH (ins, S390_CC_LE|S390_CC_OV);
			break;
		case CEE_CKFINITE: {
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
			if (ins->unused > 0) {
				if (ins->unused <= 256) {
					s390_mvc  (code, ins->unused, ins->dreg, 
						   ins->inst_offset, ins->sreg1, ins->inst_imm);
				} else {
					s390_lr   (code, s390_r0, ins->dreg);
					if (s390_is_imm16 (ins->inst_offset)) {
						s390_ahi  (code, s390_r0, ins->inst_offset);
					} else {
						s390_basr (code, s390_r13, 0);
						s390_j    (code, 4);
						s390_word (code, ins->inst_offset);
						s390_a    (code, s390_r0, 0, s390_r13, 4);
					}
					s390_lr	  (code, s390_r14, s390_r12);
					s390_lr   (code, s390_r12, ins->sreg1);
					if (s390_is_imm16 (ins->inst_imm)) {
						s390_ahi  (code, s390_r12, ins->inst_imm);
					} else {
						s390_basr (code, s390_r13, 0);
						s390_j    (code, 4);
						s390_word (code, ins->inst_imm);
						s390_a    (code, s390_r12, 0, s390_r13, 4);
					}
					s390_lr   (code, s390_r1, ins->sreg1);
					s390_lr   (code, s390_r13, s390_r1);
					s390_mvcle(code, s390_r0, s390_r12, 0, 0);
					s390_jo   (code, -2);
					s390_lr	  (code, s390_r12, s390_r14);
				}
			}
		}
			break;
		case OP_ATOMIC_ADD_I4: {
			s390_lr  (code, s390_r1, ins->sreg2);
			s390_l   (code, s390_r0, 0, ins->inst_basereg, ins->inst_offset);
			s390_a	 (code, s390_r1, 0, ins->inst_basereg, ins->inst_offset);
			s390_cs  (code, s390_r0, s390_r0, ins->inst_basereg, ins->inst_offset);
			s390_jnz (code, -7);
			s390_lr  (code, ins->dreg, s390_r1);
		}
			break;	
		case OP_ATOMIC_ADD_NEW_I4: {
			s390_lr  (code, s390_r1, ins->sreg2);
			s390_l   (code, s390_r0, 0, ins->inst_basereg, ins->inst_offset);
			s390_a	 (code, s390_r1, 0, ins->inst_basereg, ins->inst_offset);
			s390_cs  (code, s390_r0, s390_r1, ins->inst_basereg, ins->inst_offset);
			s390_jnz (code, -7);
			s390_lr  (code, ins->dreg, s390_r1);
		}
			break;	
		case OP_ATOMIC_EXCHANGE_I4: {
			s390_l   (code, s390_r0, 0, ins->inst_basereg, ins->inst_offset);
			s390_cs  (code, s390_r0, ins->sreg2, ins->inst_basereg, ins->inst_offset);
			s390_jnz (code, -4);
			s390_lr  (code, ins->dreg, s390_r0);
		}
			break;	
		case OP_S390_BKCHAIN: {
			s390_lr  (code, ins->dreg, ins->sreg1);
			if (s390_is_imm16 (cfg->stack_offset)) {
				s390_ahi (code, ins->dreg, cfg->stack_offset);
			} else {
				s390_basr (code, s390_r13, 0);
				s390_j    (code, 6);
				s390_word (code, cfg->stack_offset);
				s390_a    (code, ins->dreg, 0, s390_r13, 4);
			}
		}
		case OP_MEMORY_BARRIER: {
		}
			break;	
		default:
			g_warning ("unknown opcode %s in %s()\n", mono_inst_name (ins->opcode), __FUNCTION__);
			g_assert_not_reached ();
		}

		if ((cfg->opt & MONO_OPT_BRANCH) && ((code - cfg->native_code - offset) > max_len)) {
			g_warning ("wrong maximal instruction length of instruction %s (expected %d, got %d)",
				   mono_inst_name (ins->opcode), max_len, code - cfg->native_code - offset);
			g_assert_not_reached ();
		}
	       
		cpos += max_len;

		last_ins = ins;
		last_offset = offset;
		
		ins = ins->next;
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
	mono_register_jit_icall (mono_arch_break, "mono_arch_break", NULL, TRUE);
	mono_register_jit_icall (mono_arch_get_lmf_addr, "mono_arch_get_lmf_addr", NULL, TRUE);
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

static void
mono_arch_break(void) {
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
		      guint8 *code, MonoJumpInfo *ji, gboolean run_cctors)
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
				continue;
			case MONO_PATCH_INFO_SWITCH: 
				/*----------------------------------*/
				/* ip points at the basr r13,0/j +4 */
				/* instruction the vtable value     */
				/* follows this (i.e. ip+6)	    */
				/*----------------------------------*/
				*((gconstpointer *)(ip+6)) = target;
				target = NULL;
				continue;
			case MONO_PATCH_INFO_METHODCONST:
			case MONO_PATCH_INFO_CLASS:
			case MONO_PATCH_INFO_IMAGE:
			case MONO_PATCH_INFO_FIELD:
			case MONO_PATCH_INFO_IID:
				target = S390_RELATIVE(target, ip);
				continue;
			case MONO_PATCH_INFO_R4:
			case MONO_PATCH_INFO_R8:
			case MONO_PATCH_INFO_METHOD_REL:
				g_assert_not_reached ();
				continue;
			default:
				target = S390_RELATIVE(target, ip);
				ip += 2;
		}
		s390_patch (ip, (guint32) target);
	}
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- emit_load_volatile_registers                      */
/*                                                                  */
/* Function	- Create the instruction sequence for loading the   */
/*		  parameter registers for use with the 'tail' op.   */
/*		                               			    */
/*		  The register loading operations performed here    */
/*		  are the mirror of the store operations performed  */
/*		  in mono_arch_emit_prolog and need to be kept in   */
/*		  synchronization with it.     			    */
/*		                               			    */
/*------------------------------------------------------------------*/

guint8 *
emit_load_volatile_registers(guint8 * code, MonoCompile *cfg)
{
	MonoMethod *method = cfg->method;
	MonoMethodSignature *sig;
	MonoInst *inst;
	int pos, i;
	CallInfo *cinfo;
	size_data sz;

	sig = mono_method_signature (method);
	pos = 0;

	cinfo = calculate_sizes (sig, &sz, sig->pinvoke);

	if (cinfo->struct_ret) {
		ArgInfo *ainfo = &cinfo->ret;
		inst         = cfg->ret;
		s390_l (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
	}

	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		ArgInfo *ainfo = cinfo->args + i;
		inst = cfg->varinfo [pos];
		
		if (inst->opcode == OP_REGVAR) {
			if (ainfo->regtype == RegTypeGeneral)
				s390_lr (code, ainfo->reg, inst->dreg);
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
					s390_ic (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
					break;
				case 2:
					s390_lh (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
					break;
				case 8:
					s390_lm (code, ainfo->reg, ainfo->reg + 1, 
						  inst->inst_basereg, inst->inst_offset);
					break;
				default:
					s390_l  (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
				}
			} else if (ainfo->regtype == RegTypeBase) {
			} else if (ainfo->regtype == RegTypeFP) {
				if (ainfo->size == 8)
					s390_ld (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
				else if (ainfo->size == 4)
					s390_le (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
				else
					g_assert_not_reached ();
			} else if (ainfo->regtype == RegTypeStructByVal) {
				if (ainfo->reg != STK_BASE) {
					switch (ainfo->size) {
					case 1:
						s390_ic (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
						break;
					case 2:
						s390_lh (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
						break;
					case 4:
						s390_l  (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
						break;
					case 8:
						s390_lm (code, ainfo->reg, ainfo->reg+1, inst->inst_basereg, inst->inst_offset);
						break;
					}
				}
			} else if (ainfo->regtype == RegTypeStructByAddr) {
				if (ainfo->reg != STK_BASE) {
					s390_l  (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
				}
			} else
				g_assert_not_reached ();
		}
		pos++;
	}

	g_free (cinfo);

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
	int alloc_size, pos, max_offset, i;
	guint8 *code;
	CallInfo *cinfo;
	size_data sz;
	int tracing = 0;
	int lmfOffset;								\

	if (mono_jit_trace_calls != NULL && mono_trace_eval (method))
		tracing = 1;

	cfg->code_size   = 512;
	cfg->native_code = code = g_malloc (cfg->code_size);

	s390_stm  (code, s390_r6, s390_r14, STK_BASE, S390_REG_SAVE_OFFSET);

	if (cfg->flags & MONO_CFG_HAS_ALLOCA) {
		cfg->used_int_regs |= 1 << 11;
	}

	alloc_size = cfg->stack_offset;

	cfg->stack_usage = alloc_size;
	s390_lr   (code, s390_r11, STK_BASE);
	if (s390_is_uimm16 (alloc_size)) {
		s390_ahi  (code, STK_BASE, -alloc_size);
	} else { 
		int stackSize = alloc_size;
		while (stackSize > 32767) {
			s390_ahi  (code, STK_BASE, -32767);
			stackSize -= 32767;
		}
		s390_ahi  (code, STK_BASE, -stackSize);
	}
	s390_st   (code, s390_r11, 0, STK_BASE, 0);

	if (cfg->frame_reg != STK_BASE)
		s390_lr (code, s390_r11, STK_BASE);

        /* compute max_offset in order to use short forward jumps
	 * we always do it on s390 because the immediate displacement
	 * for jumps is too small 
	 */
	max_offset = 0;
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoInst *ins = bb->code;
		bb->max_offset = max_offset;

		if (cfg->prof_options & MONO_PROFILE_COVERAGE)
			max_offset += 6; 

		while (ins) {
			max_offset += ((guint8 *)ins_spec [ins->opcode])[MONO_INST_LEN];
			ins = ins->next;
		}
	}

	/* load arguments allocated to register from the stack */
	sig = mono_method_signature (method);
	pos = 0;

	cinfo = calculate_sizes (sig, &sz, sig->pinvoke);

	if (cinfo->struct_ret) {
		ArgInfo *ainfo = &cinfo->ret;
		inst         = cfg->ret;
		inst->unused = ainfo->vtsize;
		s390_st (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
	}

	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		ArgInfo *ainfo = cinfo->args + i;
		inst = cfg->varinfo [pos];
		
		if (inst->opcode == OP_REGVAR) {
			if (ainfo->regtype == RegTypeGeneral)
				s390_lr (code, inst->dreg, ainfo->reg);
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
				s390_lr  (code, s390_r13, STK_BASE);
				s390_ahi (code, s390_r13, alloc_size);
				s390_l   (code, inst->dreg, 0, s390_r13, ainfo->offset);
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
				case 8:
					s390_stm (code, ainfo->reg, ainfo->reg + 1, 
						  inst->inst_basereg, inst->inst_offset);
					break;
				default:
					s390_st  (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
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
					s390_lr  (code, s390_r13, STK_BASE);
					s390_ahi (code, s390_r13, alloc_size);
				}
				switch (ainfo->size) {
					case 1:
						if (ainfo->reg == STK_BASE)
				                	s390_ic  (code, reg, 0, s390_r13, ainfo->offset+3);
						s390_stc (code, reg, 0, inst->inst_basereg, doffset);
						break;
					case 2:
						if (ainfo->reg == STK_BASE)
				                	s390_lh  (code, reg, 0, s390_r13, ainfo->offset+2);
						s390_sth (code, reg, 0, inst->inst_basereg, doffset);
						break;
					case 4:
						if (ainfo->reg == STK_BASE)
				                	s390_l   (code, reg, 0, s390_r13, ainfo->offset);
						s390_st  (code, reg, 0, inst->inst_basereg, doffset);
						break;
					case 8:
						if (ainfo->reg == STK_BASE)
				                	s390_lm  (code, s390_r0, s390_r1, s390_r13, ainfo->offset);
						s390_stm (code, reg, reg+1, inst->inst_basereg, doffset);
						break;
				}
			} else if (ainfo->regtype == RegTypeStructByAddr) {
				if (ainfo->reg != STK_BASE) 
					s390_st  (code, ainfo->reg, 0, inst->inst_basereg, inst->inst_offset);
			} else
				g_assert_not_reached ();
		}
		pos++;
	}

	if (method->save_lmf) {
		/*---------------------------------------------------------------*/
		/* we build the MonoLMF structure on the stack - see mini-s390.h */
		/*---------------------------------------------------------------*/
		lmfOffset = alloc_size - sizeof(MonoLMF);	
											
		s390_lr    (code, s390_r13, cfg->frame_reg);		
		if (s390_is_uimm16(lmfOffset))
			s390_ahi   (code, s390_r13, lmfOffset);	
		else {
			s390_basr (code, s390_r14, 0);
			s390_j    (code, 4);
			s390_word (code, lmfOffset);
			s390_a    (code, s390_r13, 0, s390_r14, 4);
		}
											
		/*---------------------------------------------------------------*/
		/* Preserve the parameter registers while we fix up the lmf	 */
		/*---------------------------------------------------------------*/
		s390_stm   (code, s390_r2, s390_r6, s390_r13,
			    G_STRUCT_OFFSET(MonoLMF, pregs[0]));

		/*---------------------------------------------------------------*/
		/* On return from this call r2 have the address of the &lmf	 */
		/*---------------------------------------------------------------*/
		mono_add_patch_info (cfg, code - cfg->native_code, 
				     MONO_PATCH_INFO_INTERNAL_METHOD, 
				     (gpointer)"mono_get_lmf_addr");
		s390_brasl (code, s390_r14, 0);

		/*---------------------------------------------------------------*/	
		/* Set lmf.lmf_addr = jit_tls->lmf				 */	
		/*---------------------------------------------------------------*/	
		s390_st    (code, s390_r2, 0, s390_r13, 				
			    G_STRUCT_OFFSET(MonoLMF, lmf_addr));			
											
		/*---------------------------------------------------------------*/	
		/* Get current lmf						 */	
		/*---------------------------------------------------------------*/	
		s390_l     (code, s390_r0, 0, s390_r2, 0);				
											
		/*---------------------------------------------------------------*/	
		/* Set our lmf as the current lmf				 */	
		/*---------------------------------------------------------------*/	
		s390_st	   (code, s390_r13, 0, s390_r2, 0);				
											
		/*---------------------------------------------------------------*/	
		/* Have our lmf.previous_lmf point to the last lmf		 */	
		/*---------------------------------------------------------------*/	
		s390_st	   (code, s390_r0, 0, s390_r13, 				
			    G_STRUCT_OFFSET(MonoLMF, previous_lmf));			
											
		/*---------------------------------------------------------------*/	
		/* save method info						 */	
		/*---------------------------------------------------------------*/	
		s390_basr  (code, s390_r1, 0);						
		s390_j	   (code, 4);							
		s390_word  (code, method);						
		s390_l	   (code, s390_r1, 0, s390_r1, 4);			
		s390_st    (code, s390_r1, 0, s390_r13, 				
			    G_STRUCT_OFFSET(MonoLMF, method));				
										
		/*---------------------------------------------------------------*/	
		/* save the current IP						 */	
		/*---------------------------------------------------------------*/	
		s390_st	   (code, STK_BASE, 0, s390_r13, G_STRUCT_OFFSET(MonoLMF, ebp));
		s390_basr  (code, s390_r1, 0);
		s390_la    (code, s390_r1, 0, s390_r1, 0);				
		s390_st    (code, s390_r1, 0, s390_r13, G_STRUCT_OFFSET(MonoLMF, eip));	
											
		/*---------------------------------------------------------------*/	
		/* Save general and floating point registers			 */	
		/*---------------------------------------------------------------*/	
		s390_stm   (code, s390_r2, s390_r12, s390_r13, 				
			    G_STRUCT_OFFSET(MonoLMF, gregs[2]));			
		for (i = 0; i < 16; i++) {						
			s390_std  (code, i, 0, s390_r13, 				
				   G_STRUCT_OFFSET(MonoLMF, fregs[i]));			
		}									

		/*---------------------------------------------------------------*/
		/* Restore the parameter registers now that we've set up the lmf */
		/*---------------------------------------------------------------*/
		s390_lm    (code, s390_r2, s390_r6, s390_r13, 				
			    G_STRUCT_OFFSET(MonoLMF, pregs[0]));			
	}

	if (tracing)
		code = mono_arch_instrument_prolog(cfg, enter_method, code, TRUE);

	cfg->code_len = code - cfg->native_code;
	g_free (cinfo);

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
		mono_jit_stats.code_reallocs++;
	}

	code = cfg->native_code + cfg->code_len;

	if (mono_jit_trace_calls != NULL && mono_trace_eval (method)) {
		code = mono_arch_instrument_epilog (cfg, leave_method, code, TRUE);
		tracing = 1;
	}
	
	if (method->save_lmf) 
		restoreLMF(code, cfg->frame_reg, cfg->stack_usage);

	if (cfg->flags & MONO_CFG_HAS_ALLOCA) 
		s390_l (code, STK_BASE, 0, STK_BASE, 0);
	else
		code = backUpStackPtr(cfg, code, STK_BASE);

	s390_lm  (code, s390_r6, s390_r14, STK_BASE, S390_REG_SAVE_OFFSET);
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
	guint8		*exc_throw_start [MAX_EXC], 
			*exc_throw_end [MAX_EXC];

	for (patch_info = cfg->patch_info; 
	     patch_info; 
	     patch_info = patch_info->next) {
		if (patch_info->type == MONO_PATCH_INFO_EXC)
			exc_count++;
	}

	code_size = exc_count * 26;

	while ((cfg->code_len + code_size) > (cfg->code_size - 16)) {
		cfg->code_size  *= 2;
		cfg->native_code = g_realloc (cfg->native_code, cfg->code_size);
		mono_jit_stats.code_reallocs++; 
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
			guint32 throw_ip;

			/*-----------------------------------------------------*/
			/* Patch the branch in epilog to come here	       */
			/*-----------------------------------------------------*/
			s390_patch (ip + 2, (guint32) (S390_RELATIVE(code,ip)));

			exc_class = mono_class_from_name (mono_defaults.corlib, 
							  "System", 
							  patch_info->data.name);
			g_assert (exc_class);
			throw_ip = patch_info->ip.i;

			for (iExc = 0; iExc < nThrows; ++iExc)
				if (exc_classes [iExc] == exc_class)
					break;
		
			if (iExc < nThrows) {
				s390_jcl (code, S390_CC_UN, (guint32) exc_throw_start [iExc]);
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
				s390_j	  (code, 4);
				s390_word (code, patch_info->data.target);
				/*---------------------------------------------*/
				/* Load return address & parameter register    */
				/*---------------------------------------------*/
				s390_larl (code, s390_r14, S390_RELATIVE((patch_info->ip.i +
							   cfg->native_code + 8), code));
				s390_l    (code, s390_r2, 0, s390_r13, 4);
				/*---------------------------------------------*/
				/* Reuse the current patch to set the jump     */
				/*---------------------------------------------*/
				patch_info->type      = MONO_PATCH_INFO_INTERNAL_METHOD;
				patch_info->data.name = "mono_arch_throw_exception_by_name";
				patch_info->ip.i      = code - cfg->native_code;
				s390_jcl  (code, S390_CC_UN, 0);
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
/* Name		- mono_arch_setup_jit_tls_data                      */
/*                                                                  */
/* Function	- Setup the JIT's Thread Level Specific Data.       */
/*		                               			    */
/*------------------------------------------------------------------*/

void
mono_arch_setup_jit_tls_data (MonoJitTlsData *tls)
{

	if (!tls_offset_inited) {
		tls_offset_inited = TRUE;

#if HAVE_KW_THREAD
# if 0
	__asm__ ("\tear\t%r1,0\n"
		 "\tlr\t%0,%3\n"
		 "\tsr\t%0,%r1\n"
		 "\tlr\t%1,%4\n"
		 "\tsr\t%1,%r1\n"
		 "\tlr\t%2,%5\n"
		 "\tsr\t%2,%r1\n"
		 : "=r" (appdomain_tls_offset),
		   "=r" (thread_tls_offset),
		   "=r" (lmf_tls_offset)
		 : "r" (&tls_appdomain),
		   "r" (&tls_current_object),
		   "r" (&mono_lmf_addr)
		 : "1", "cc");
# endif
#endif
	}		

	if (!lmf_addr_key_inited) {
		lmf_addr_key_inited = TRUE;
		pthread_key_create (&lmf_addr_key, NULL);
	}
	pthread_setspecific (lmf_addr_key, &tls->lmf);

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
/* Name		- mono_arch_emit_this_vret_args                     */
/*                                                                  */
/* Function	-                                                   */
/*		                               			    */
/*------------------------------------------------------------------*/

void
mono_arch_emit_this_vret_args (MonoCompile *cfg, MonoCallInst *inst, int this_reg, int this_type, int vt_reg)
{
	int this_dreg = s390_r2;
	
	if (vt_reg != -1)
		this_dreg = s390_r3;

	/* add the this argument */
	if (this_reg != -1) {
		MonoInst *this;
		MONO_INST_NEW (cfg, this, OP_SETREG);
		this->type  = this_type;
		this->sreg1 = this_reg;
		this->dreg  = mono_regstate_next_int (cfg->rs);
		mono_bblock_add_inst (cfg->cbb, this);
		mono_call_inst_add_outarg_reg (inst, this->dreg, this_dreg, FALSE);
	}

	if (vt_reg != -1) {
		MonoInst *vtarg;
		MONO_INST_NEW (cfg, vtarg, OP_SETREG);
		vtarg->type  = STACK_MP;
		vtarg->sreg1 = vt_reg;
		vtarg->dreg  = mono_regstate_next_int (cfg->rs);
		mono_bblock_add_inst (cfg->cbb, vtarg);
		mono_call_inst_add_outarg_reg (inst, vtarg->dreg, s390_r2, FALSE);
	}
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_get_inst_for_method                   */
/*                                                                  */
/* Function	- Check for opcodes we can handle directly in       */
/*		  hardware.                    			    */
/*		                               			    */
/*------------------------------------------------------------------*/

MonoInst*
mono_arch_get_inst_for_method (MonoCompile *cfg, MonoMethod *cmethod, 
			       MonoMethodSignature *fsig, MonoInst **args)
{
	MonoInst *ins = NULL;

	if (cmethod->klass == mono_defaults.math_class) {
		if (strcmp (cmethod->name, "Sqrt") == 0) {
			MONO_INST_NEW (cfg, ins, OP_SQRT);
			ins->inst_i0 = args [0];
		}
	} else if (cmethod->klass == mono_defaults.thread_class &&
			   strcmp (cmethod->name, "MemoryBarrier") == 0) {
		MONO_INST_NEW (cfg, ins, OP_MEMORY_BARRIER);
	} else if(cmethod->klass->image == mono_defaults.corlib &&
			   (strcmp (cmethod->klass->name_space, "System.Threading") == 0) &&
			   (strcmp (cmethod->klass->name, "Interlocked") == 0)) {

		if (strcmp (cmethod->name, "Increment") == 0 && 
		    fsig->params [0]->type == MONO_TYPE_I4) {
			MonoInst *ins_iconst;

			MONO_INST_NEW (cfg, ins, OP_ATOMIC_ADD_NEW_I4);
			MONO_INST_NEW (cfg, ins_iconst, OP_ICONST);
			ins_iconst->inst_c0 = 1;

			ins->inst_i0 = args [0];
			ins->inst_i1 = ins_iconst;
		} else if (strcmp (cmethod->name, "Decrement") == 0 && 
			   fsig->params [0]->type == MONO_TYPE_I4) {
			MonoInst *ins_iconst;

			MONO_INST_NEW (cfg, ins, OP_ATOMIC_ADD_NEW_I4);
			MONO_INST_NEW (cfg, ins_iconst, OP_ICONST);
			ins_iconst->inst_c0 = -1;

			ins->inst_i0 = args [0];
			ins->inst_i1 = ins_iconst;
		} else if (strcmp (cmethod->name, "Exchange") == 0 && 
			   fsig->params [0]->type == MONO_TYPE_I4) {
			MONO_INST_NEW (cfg, ins, OP_ATOMIC_EXCHANGE_I4);

			ins->inst_i0 = args [0];
			ins->inst_i1 = args [1];
		} else if (strcmp (cmethod->name, "Add") == 0 && 
			   fsig->params [0]->type == MONO_TYPE_I4) {
			MONO_INST_NEW (cfg, ins, OP_ATOMIC_ADD_I4);

			ins->inst_i0 = args [0];
			ins->inst_i1 = args [1];
		}
	}
	return ins;
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
				tree->inst_offset, tree->unused,
				mono_arch_regname(tree->dreg), tree->inst_imm, 
				mono_arch_regname(tree->sreg1));
			done = 1;
			break;
		case OP_S390_SETF4RET:
			printf ("[f%ld,f%ld]", 
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
			break;
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
/* Name		- mono_arch_get_thread_intrinsic                    */
/*                                                                  */
/* Function	- 						    */
/*		                               			    */
/* Returns	-     						    */
/*                                                                  */
/*------------------------------------------------------------------*/

MonoInst * 
mono_arch_get_thread_intrinsic (MonoCompile* cfg)
{
	MonoInst *ins;

	if (thread_tls_offset == -1)
		return NULL;
	
	MONO_INST_NEW (cfg, ins, OP_TLS_GET);
	ins->inst_offset = thread_tls_offset;
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
/* Name		- mono_arch_get_lmf_addr                            */
/*                                                                  */
/* Function	- 						    */
/*		                               			    */
/* Returns	-     						    */
/*                                                                  */
/*------------------------------------------------------------------*/

gpointer
mono_arch_get_lmf_addr (void)
{
        return pthread_getspecific (lmf_addr_key);
}


/*========================= End of Function ========================*/
