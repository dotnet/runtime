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
		if (s390_is_uimm16(displace)) {						\
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
		if (s390_is_uimm16(displace)) {						\
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
		if (s390_is_uimm16(displace)) {						\
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
		if (s390_is_uimm16(displace)) {						\
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

#undef DEBUG
#define DEBUG(a) if (cfg->verbose_level > 1) a

/*----------------------------------------*/
/* use s390_r2-s390_r5 as temp registers  */
/*----------------------------------------*/
#define S390_CALLER_REGS  (0x10fc)
#define reg_is_freeable(r) (S390_CALLER_REGS & 1 << (r))

/*----------------------------------------*/
/* use s390_f1/s390_f3-s390_f15 as temps  */
/*----------------------------------------*/
#define S390_CALLER_FREGS (0xfffa)
#define freg_is_freeable(r) ((r) >= 1 && (r) <= 14)

#define S390_TRACE_STACK_SIZE (5*sizeof(gint32)+3*sizeof(gdouble))

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
	SAVE_FP
};

typedef struct {
	int born_in;
	int killed_in;
	int last_use;
	int prev_use;
} RegTrack;

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
	gint32	offparm;	/* offset on callee's stack */
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

static guint32 * emit_memcpy (guint8 *, int, int, int, int, int);
static void indent (int);
static guint8 * restoreLMF(MonoCompile *, guint8 *);
static guint8 * backUpStackPtr(MonoCompile *, guint8 *);
static void decodeParm (MonoType *, void *, int);
static void enter_method (MonoMethod *, RegParm *, char *);
static void leave_method (MonoMethod *, ...);
static gboolean is_regsize_var (MonoType *);
static void add_general (guint *, size_data *, ArgInfo *, gboolean);
static CallInfo * calculate_sizes (MonoMethodSignature *, size_data *, gboolean);
static void peephole_pass (MonoCompile *, MonoBasicBlock *);
static int mono_spillvar_offset (MonoCompile *, int);
static int mono_spillvar_offset_float (MonoCompile *, int);
static void print_ins (int, MonoInst *);
static void print_regtrack (RegTrack *, int);
static InstList * inst_list_prepend (MonoMemPool *, InstList *, MonoInst *);
static int get_register_force_spilling (MonoCompile *, InstList *, MonoInst *, int);
static int get_register_spilling (MonoCompile *, InstList *, MonoInst *, guint32, int);
static int get_float_register_spilling (MonoCompile *, InstList *, MonoInst *, guint32, int);
static MonoInst * create_copy_ins (MonoCompile *, int, int, MonoInst *);
static MonoInst * create_copy_ins_float (MonoCompile *, int, int, MonoInst *);
static MonoInst * create_spilled_store (MonoCompile *, int, int, int, MonoInst *);
static MonoInst * create_spilled_store_float (MonoCompile *, int, int, int, MonoInst *);
static void insert_before_ins (MonoInst *, InstList *, MonoInst *);
static int alloc_int_reg (MonoCompile *, InstList *, MonoInst *, int, guint32);
static guchar * emit_float_to_int (MonoCompile *, guchar *, int, int, int, gboolean);
static unsigned char * mono_emit_stack_alloc (guchar *, MonoInst *);

/*========================= End of Prototypes ======================*/

/*------------------------------------------------------------------*/
/*                 G l o b a l   V a r i a b l e s                  */
/*------------------------------------------------------------------*/

int mono_exc_esp_offset = 0;

static int indent_level = 0;

static const char*const * ins_spec = s390;

static gboolean tls_offset_inited = FALSE;

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

static guint32*
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
/* Name		- restoreLMF                                        */
/*                                                                  */
/* Function	- Restore the LMF state prior to exiting a method.  */
/*                                                                  */
/*------------------------------------------------------------------*/

static inline guint8 * 
restoreLMF(MonoCompile *cfg, guint8 *code)
{
	int lmfOffset = 0;

	s390_lr  (code, s390_r13, cfg->frame_reg);

	lmfOffset = cfg->stack_usage -  sizeof(MonoLMF);

	/*-------------------------------------------------*/
	/* r13 = my lmf					   */
	/*-------------------------------------------------*/
	s390_ahi (code, s390_r13, lmfOffset);

	/*-------------------------------------------------*/
	/* r6 = &jit_tls->lmf				   */
	/*-------------------------------------------------*/
	s390_l   (code, s390_r6, 0, s390_r13, G_STRUCT_OFFSET(MonoLMF, lmf_addr));

	/*-------------------------------------------------*/
	/* r0 = lmf.previous_lmf			   */
	/*-------------------------------------------------*/
	s390_l   (code, s390_r0, 0, s390_r13, G_STRUCT_OFFSET(MonoLMF, previous_lmf));

	/*-------------------------------------------------*/
	/* jit_tls->lmf = previous_lmf			   */
	/*-------------------------------------------------*/
	s390_l   (code, s390_r13, 0, s390_r6, 0);
	s390_st  (code, s390_r0, 0, s390_r6, 0);
	return (code);
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

	if (s390_is_imm16 (cfg->stack_usage)) {
		s390_ahi  (code, STK_BASE, cfg->stack_usage);
	} else { 
		while (stackSize > 32767) {
			s390_ahi  (code, STK_BASE, 32767);
			stackSize -= 32767;
		}
		s390_ahi  (code, STK_BASE, stackSize);
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
		simpleType = type->type;
enum_parmtype:
		switch (simpleType) {
			case MONO_TYPE_I :
				printf ("[INTPTR:%p], ", *((int **) curParm));
				break;
			case MONO_TYPE_U :
				printf ("[UINTPTR:%p], ", *((int **) curParm));
				break;
			case MONO_TYPE_BOOLEAN :
				printf ("[BOOL:%p], ", *((int *) curParm));
				break;
			case MONO_TYPE_CHAR :
				printf ("[CHAR:%p], ", *((int *) curParm));
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
				printf ("[UINT1:%ud], ", *((unsigned int *) curParm));
				break; 
			case MONO_TYPE_U2 :
				printf ("[UINT2:%ud], ", *((guint16 *) curParm));
				break; 
			case MONO_TYPE_U4 :
				printf ("[UINT4:%ud], ", *((guint32 *) curParm));
				break; 
			case MONO_TYPE_U8 :
				printf ("[UINT8:%ul], ", *((guint64 *) curParm));
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
				if (obj) {
					printf("[CLASS/OBJ:");
					class = obj->vtable->klass;
					if (class == mono_defaults.string_class) {
						printf("[STRING:%p:%s]", 
						       *obj, mono_string_to_utf8 (obj));
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
				printf("[INT8:%lld], ", *((gint64 *) (curParm)));
				break;
			case MONO_TYPE_R4 :
				printf("[FLOAT4:%f], ", *((float *) (curParm)));
				break;
			case MONO_TYPE_R8 :
				printf("[FLOAT8:%g], ", *((double *) (curParm)));
				break;
			case MONO_TYPE_VALUETYPE : {
				int i;
				if (type->data.klass->enumtype) {
					simpleType = type->data.klass->enum_basetype->type;
					printf("{VALUETYPE} - ");
					goto enum_parmtype;
				}
				printf("[VALUETYPE:");
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
	MonoJitArgumentInfo *arg_info;
	MonoMethodSignature *sig;
	char *fname;
	guint32 ip;
	CallInfo *cinfo;
	ArgInfo *ainfo;
	size_data sz;
	void *curParm;

	fname = mono_method_full_name (method, TRUE);
	indent (1);
	printf ("ENTER: %s(", fname);
	g_free (fname);

	ip  = (*(guint32 *) (sp+S390_RET_ADDR_OFFSET)) & 0x7fffffff;
	printf (") ip: %p sp: %p - ", ip, sp); 

	if (rParm == NULL)
		return;
	
	sig = method->signature;
	
	cinfo = calculate_sizes (sig, &sz, sig->pinvoke);

	if (cinfo->struct_ret) {
		printf ("[VALUERET:%p], ", rParm->gr[0]);
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
				class = obj->vtable->klass;
				if (class == mono_defaults.string_class) {
					printf ("this:[STRING:%p:%s], ", 
						obj, mono_string_to_utf8 ((MonoString *)obj));
				} else {
					printf ("this:%p[%s.%s], ", 
						obj, class->name_space, class->name);
				}
			} else 
				printf ("this:NULL, ");
		}
		oParm++;
	}
					
	for (i = 0; i < sig->param_count; ++i) {
		ainfo = cinfo->args + (i + oParm);
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

				switch (ainfo->vtsize) {
					case 0:
					case 1:
					case 2:
					case 4:
					case 8:
						decodeParm(sig->params[i], 
				 	           curParm,
					           ainfo->size);
						break;
					default:
						decodeParm(sig->params[i], 
				 	           *((char **) curParm),
					           ainfo->vtsize);
					}
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

	type = method->signature->ret;

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
		int *val = va_arg (ap, int*);
		printf ("[INT:%d]", val);
		printf("]");
		break;
	}
	case MONO_TYPE_U: {
		int *val = va_arg (ap, int*);
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

		if (o) {
			if (o->vtable->klass == mono_defaults.boolean_class) {
				printf ("[BOOLEAN:%p:%d]", o, *((guint8 *)o + sizeof (MonoObject)));		
			} else if  (o->vtable->klass == mono_defaults.int32_class) {
				printf ("[INT32:%p:%d]", o, *((gint32 *)((char *)o + sizeof (MonoObject))));	
			} else if  (o->vtable->klass == mono_defaults.int64_class) {
				printf ("[INT64:%p:%lld]", o, *((gint64 *)((char *)o + sizeof (MonoObject))));	
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
		printf ("[LONG:%lld]", l);
		break;
	}
	case MONO_TYPE_R4: {
		double f = va_arg (ap, double);
		printf ("[FLOAT4:%f]\n", (float) f);
		break;
	}
	case MONO_TYPE_R8: {
		double f = va_arg (ap, double);
		printf ("[FLOAT8:%g]\n", f);
		break;
	}
	case MONO_TYPE_VALUETYPE: 
		if (type->data.klass->enumtype) {
			type = type->data.klass->enum_basetype;
			goto handle_enum;
		} else {
			guint8 *p = va_arg (ap, gpointer);
			int j, size, align;
			size = mono_type_size (type, &align);
			printf ("[");
			for (j = 0; p && j < size; j++)
				printf ("%02x,", p [j]);
			printf ("]");
		}
		break;
	default:
		printf ("(unknown return type %x)", 
			method->signature->ret->type);
	}

	ip = ((gint32) __builtin_return_address (0)) & 0x7fffffff;
	printf (" ip: %p\n", ip);
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
	switch (t->type) {
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
		if (vmv->range.first_use.abs_pos > vmv->range.last_use.abs_pos)
			continue;

		if (ins->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT) || (ins->opcode != OP_LOCAL && ins->opcode != OP_ARG))
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
	int i, top = 13;

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
	guint i, fr, gr, size, nWords;
	int nParm = sig->hasthis + sig->param_count;
	guint32 simpletype, align;
	CallInfo *cinfo = g_malloc0 (sizeof (CallInfo) + sizeof (ArgInfo) * nParm);

	fr                = 0;
	gr                = s390_r2;
	nParm 		  = 0;
	cinfo->struct_ret = 0;
	sz->retStruct     = 0;
	sz->stack_size    = S390_MINIMAL_STACK_SIZE;
	sz->code_size     = 0;
	sz->local_size    = 0;

	/*----------------------------------------------------------*/
	/* We determine the size of the return code/stack in case we*/
	/* need to reserve a register to be used to address a stack */
	/* area that the callee will use.			    */
	/*----------------------------------------------------------*/

//	if (sig->ret->byref || string_ctor) {
//		sz->code_size += 8;
//		add_general (&gr, sz, cinfo->args+nParm, TRUE);
//		cinfo->args[nParm].size = sizeof(gpointer);
//		nParm++;
//	} else {
	{
		simpletype = sig->ret->type;
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
		case MONO_TYPE_VALUETYPE:
			if (sig->ret->data.klass->enumtype) {
				simpletype = sig->ret->data.klass->enum_basetype->type;
				goto enum_retvalue;
			}
			if (sig->pinvoke)
				size = mono_class_native_size (sig->ret->data.klass, &align);
			else
                        	size = mono_class_value_size (sig->ret->data.klass, &align);
			cinfo->ret.reg    = s390_r2;
			cinfo->struct_ret = 1;
			cinfo->ret.size   = size;
			cinfo->ret.vtsize = size;
			cinfo->ret.offset = sz->stack_size;
			sz->stack_size   += S390_ALIGN(size, align);
			gr++;
                        break;
		case MONO_TYPE_TYPEDBYREF:
		case MONO_TYPE_VOID:
			break;
		default:
			g_error ("Can't handle as return value 0x%x", sig->ret->type);
		}
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
		if (sig->params [i]->byref) {
			add_general (&gr, sz, cinfo->args+nParm, TRUE);
			cinfo->args[nParm].size = sizeof(gpointer);
			nParm++;
			continue;
		}
		simpletype = sig->params [i]->type;
	enum_calc_size:
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
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_STRING:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_ARRAY:
			cinfo->args[nParm].size = sizeof(gpointer);
			add_general (&gr, sz, cinfo->args+nParm, TRUE);
			nParm++;
			break;
		case MONO_TYPE_VALUETYPE:
			if (sig->params [i]->data.klass->enumtype) {
				simpletype = sig->params [i]->data.klass->enum_basetype->type;
				goto enum_calc_size;
			}
			if (sig->pinvoke)
				size = mono_class_native_size (sig->params [i]->data.klass, &align);
			else
				size = mono_class_value_size (sig->params [i]->data.klass, &align);
			nWords = (size + sizeof(gpointer) - 1) /
			         sizeof(gpointer);

			cinfo->args[nParm].vtsize  = 0;
			cinfo->args[nParm].size    = 0;
			cinfo->args[nParm].offparm = sz->local_size;

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
					add_general(&gr, sz, cinfo->args+nParm, TRUE);
					cinfo->args[nParm].size    = sizeof(int);
					cinfo->args[nParm].regtype = RegTypeStructByAddr; 
					cinfo->args[nParm].vtsize  = size;
					sz->code_size  		  += 40;
					sz->local_size 		  += size;
					if (cinfo->args[nParm].reg == STK_BASE)
						sz->local_size += sizeof(gpointer);
					nParm++;
			}
			break;
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
			cinfo->args[nParm].size = sizeof(long long);
			add_general (&gr, sz, cinfo->args+nParm, FALSE);
			nParm++;
			break;
		case MONO_TYPE_R4:
			cinfo->args[nParm].size = sizeof(float);
			if (fr <= S390_LAST_FPARG_REG) {
				cinfo->args[nParm].regtype = RegTypeFP;
				cinfo->args[nParm].reg	   = fr;
				sz->code_size += 4;
				fr += 2;
			}
			else {
				cinfo->args[nParm].offset  = sz->stack_size;
				cinfo->args[nParm].reg	   = STK_BASE;
				cinfo->args[nParm].regtype = RegTypeBase;
				sz->code_size  += 4;
				sz->stack_size += sizeof(float);
			}
			nParm++;
			break;
		case MONO_TYPE_R8:
			cinfo->args[nParm].size = sizeof(double);
			if (fr <= S390_LAST_FPARG_REG) {
				cinfo->args[nParm].regtype = RegTypeFP;
				cinfo->args[nParm].reg	   = fr;
				sz->code_size += 4;
				fr += 2;
			} else {
				cinfo->args[nParm].offset  = sz->stack_size;
				cinfo->args[nParm].reg	   = STK_BASE;
				cinfo->args[nParm].regtype = RegTypeBase;
				sz->code_size  += 4;
				sz->stack_size += sizeof(double);
			}
			nParm++;
			break;
		default:
			g_error ("Can't trampoline 0x%x", sig->params [i]->type);
		}
	}

	cinfo->stack_usage = S390_ALIGN(sz->stack_size+sz->local_size, 
					S390_STACK_ALIGNMENT);
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

	sig     = cfg->method->signature;
	
	cinfo   = calculate_sizes (sig, &sz, sig->pinvoke);

	if (cinfo->struct_ret) {
		cfg->ret->opcode = OP_REGVAR;
		cfg->ret->inst_c0 = s390_r2;
	} else {
		/* FIXME: handle long and FP values */
		switch (sig->ret->type) {
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
	/* 								*/
	/* also note that if the function uses alloca, we use s390_r11	*/
	/* to point at the local variables.				*/
	/* add parameter area size for called functions 		*/
	/*--------------------------------------------------------------*/
	offset = (cfg->param_area + S390_MINIMAL_STACK_SIZE);

	if (cinfo->struct_ret) {
		inst 		   = cfg->ret;
		offset 		   = S390_ALIGN(offset, sizeof(gpointer));
		inst->inst_offset  = offset;
		inst->opcode 	   = OP_REGOFFSET;
		inst->inst_basereg = frame_reg;
		offset 		  += sizeof(gpointer);
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

	for (iParm = sArg; iParm < eArg; ++iParm) {
		inst = cfg->varinfo [curinst];
		if (inst->opcode != OP_REGVAR) {
			switch (cinfo->args[iParm].regtype) {
				case RegTypeStructByAddr :
					inst->opcode       = OP_S390_LOADARG;
					inst->inst_basereg = frame_reg;
					size		   = abs(cinfo->args[iParm].vtsize);
					offset 		   = S390_ALIGN(offset, size);
					inst->inst_offset  = offset; 
					break;
				case RegTypeStructByVal :
					inst->opcode	   = OP_S390_ARGPTR;
					inst->inst_basereg = frame_reg;
					size		   = cinfo->args[iParm].size;
					offset		   = S390_ALIGN(offset, size);
					inst->inst_offset  = offset;
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
			size = mono_class_native_size (inst->inst_vtype->data.klass, &align);
		else
			size = mono_type_size (inst->inst_vtype, &align);

		offset 		   = S390_ALIGN(offset, align);
		inst->inst_offset  = offset;
		inst->opcode 	   = OP_REGOFFSET;
		inst->inst_basereg = frame_reg;
		offset 		  += size;
		DEBUG (g_print("allocating local %d to %d\n", iVar, inst->inst_offset));
	}

	/*------------------------------------------------------*/
	/* Allow space for the trace method stack area if needed*/
	/*------------------------------------------------------*/
	if (mono_jit_trace_calls != NULL && mono_trace_eval (cfg)) 
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
/* Note         - FIXME: We need an alignment solution for 	    */
/*		  enter_method and mono_arch_call_opcode, currently */
/*		  alignment in mono_arch_call_opcode is computed    */
/*		  without arch_get_argument_info.                   */
/*		                               			    */
/*------------------------------------------------------------------*/

MonoCallInst*
mono_arch_call_opcode (MonoCompile *cfg, MonoBasicBlock* bb, 
		       MonoCallInst *call, int is_virtual) {
	MonoInst *arg, *in;
	MonoMethodSignature *sig;
	int i, n, lParamArea;
	CallInfo *cinfo;
	ArgInfo *ainfo;
	size_data sz;

	sig = call->signature;
	n = sig->param_count + sig->hasthis;
	DEBUG (g_print ("Call requires: %d parameters\n",n));
	
	cinfo = calculate_sizes (sig, &sz, sig->pinvoke);

	call->stack_usage = cinfo->stack_usage;
	lParamArea        = cinfo->stack_usage - S390_MINIMAL_STACK_SIZE;
	cfg->param_area   = MAX (cfg->param_area, lParamArea);
	cfg->flags       |= MONO_CFG_HAS_CALLS;

	if (cinfo->struct_ret)
		call->used_iregs |= 1 << cinfo->ret.reg;

	for (i = 0; i < n; ++i) {
		ainfo = cinfo->args + i;
		DEBUG (g_print ("Parameter %d - Register: %d Type: %d\n",
				i+1,ainfo->reg,ainfo->regtype));
		if (is_virtual && i == 0) {
			/* the argument will be attached to the call instrucion */
			in = call->args [i];
			call->used_iregs |= 1 << ainfo->reg;
		} else {
			MONO_INST_NEW (cfg, arg, OP_OUTARG);
			in = call->args [i];
			arg->cil_code  = in->cil_code;
			arg->inst_left = in;
			arg->type      = in->type;
			/* prepend, we'll need to reverse them later */
			arg->next      = call->out_args;
			call->out_args = arg;
			if (ainfo->regtype == RegTypeGeneral) {
				arg->unused = ainfo->reg;
				call->used_iregs |= 1 << ainfo->reg;
				if (arg->type == STACK_I8)
					call->used_iregs |= 1 << (ainfo->reg + 1);
			} else if (ainfo->regtype == RegTypeStructByAddr) {
				call->used_iregs |= 1 << ainfo->reg;
				arg->sreg1     = ainfo->reg;
				arg->opcode    = OP_OUTARG_VT;
				arg->unused    = -ainfo->vtsize;
				arg->inst_imm  = ainfo->offset;
				arg->sreg2     = ainfo->offparm + S390_MINIMAL_STACK_SIZE;
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
				arg->sreg1     = ainfo->reg;
				arg->opcode    = OP_OUTARG_VT;
				arg->unused    = ainfo->size;
				arg->inst_imm  = ainfo->offset;
				arg->sreg2     = ainfo->offparm + S390_MINIMAL_STACK_SIZE;
			} else if (ainfo->regtype == RegTypeBase) {
				arg->opcode = OP_OUTARG;
				arg->unused = ainfo->reg | (ainfo->size << 8);
				arg->inst_imm = ainfo->offset;
				call->used_fregs |= 1 << ainfo->reg;
			} else if (ainfo->regtype == RegTypeFP) {
				arg->unused = ainfo->reg;
				call->used_fregs |= 1 << ainfo->reg;
				if (ainfo->size == 4) {
					MonoInst *conv;
					arg->opcode     = OP_OUTARG_R4;
					MONO_INST_NEW (cfg, conv, OP_FCONV_TO_R4);
					conv->inst_left = arg->inst_left;
					arg->inst_left  = conv;
				}
				else
					arg->opcode = OP_OUTARG_R8;
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
	guchar *code = p;
	int 	parmOffset, 
	    	fpOffset;

	parmOffset = cfg->stack_usage - S390_TRACE_STACK_SIZE;
	if (cfg->method->save_lmf)
		parmOffset -= sizeof(MonoLMF);
	fpOffset   = parmOffset + (5*sizeof(gint32));

	s390_stm  (code, s390_r2, s390_r6, STK_BASE, parmOffset);
	s390_std  (code, s390_f0, 0, STK_BASE, fpOffset);
	s390_std  (code, s390_f1, 0, STK_BASE, fpOffset+sizeof(gdouble));
	s390_std  (code, s390_f2, 0, STK_BASE, fpOffset+2*sizeof(gdouble));
	s390_basr (code, s390_r13, 0);
	s390_j    (code, 6);
	s390_word (code, cfg->method);
	s390_word (code, func);
	s390_l    (code, s390_r2, 0, s390_r13, 4);
	s390_la   (code, s390_r3, 0, STK_BASE, parmOffset);
	s390_lr   (code, s390_r4, STK_BASE);
	s390_ahi  (code, s390_r4, cfg->stack_usage);
	s390_l	  (code, s390_r1, 0, s390_r13, 8);
	s390_basr (code, s390_r14, s390_r1);
	s390_ld   (code, s390_f2, 0, STK_BASE, fpOffset+2*sizeof(gdouble));
	s390_ld   (code, s390_f1, 0, STK_BASE, fpOffset+sizeof(gdouble));
	s390_ld   (code, s390_f0, 0, STK_BASE, fpOffset);
	s390_lm   (code, s390_r2, s390_r6, STK_BASE, parmOffset);

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
	int        rtype = method->signature->ret->type;

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
	case MONO_TYPE_R8:
		save_mode = SAVE_FP;
		break;
	case MONO_TYPE_VALUETYPE:
		if (method->signature->ret->data.klass->enumtype) {
			rtype = method->signature->ret->data.klass->enum_basetype->type;
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
	case SAVE_FP:
		s390_std (code, s390_f0, 0, cfg->frame_reg, saveOffset);
		if (enable_arguments) {
			/* FIXME: what reg?  */
			s390_ldr (code, s390_f2, s390_f0);
			s390_lm  (code, s390_r3, s390_r4, cfg->frame_reg, saveOffset);
		}
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
	case SAVE_FP:
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
					//static int c = 0; printf ("MATCHX %s %d\n", cfg->method->name,c++);
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
/* Name		- mono_spillvar_offset                              */
/*                                                                  */
/* Function	- Returns the offset used by spillvar. It allocates */
/*		  a new spill variable if necessary.		    */
/*		                               			    */
/*------------------------------------------------------------------*/

static int
mono_spillvar_offset (MonoCompile *cfg, int spillvar)
{
	MonoSpillInfo **si, *info;
	int i = 0;

	si = &cfg->spill_info; 
	
	while (i <= spillvar) {

		if (!*si) {
			*si = info = mono_mempool_alloc (cfg->mempool, sizeof (MonoSpillInfo));
			info->next = NULL;
			info->offset = cfg->stack_offset;
			cfg->stack_offset += sizeof (gpointer);
		}

		if (i == spillvar)
			return (*si)->offset;

		i++;
		si = &(*si)->next;
	}

	g_assert_not_reached ();
	return 0;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_spillvar_offset_float                        */
/*                                                                  */
/* Function	-                                                   */
/*		                               			    */
/*------------------------------------------------------------------*/

static int
mono_spillvar_offset_float (MonoCompile *cfg, int spillvar)
{
	MonoSpillInfo **si, *info;
	int i = 0;

	si = &cfg->spill_info_float; 
	
	while (i <= spillvar) {

		if (!*si) {
			*si = info = mono_mempool_alloc (cfg->mempool, sizeof (MonoSpillInfo));
			info->next 	   = NULL;
			cfg->stack_offset  = S390_ALIGN(cfg->stack_offset, S390_STACK_ALIGNMENT);
			info->offset       = cfg->stack_offset;
			cfg->stack_offset += sizeof (double);
		}

		if (i == spillvar)
			return (*si)->offset;

		i++;
		si = &(*si)->next;
	}

	g_assert_not_reached ();
	return 0;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- print_ins                                         */
/*                                                                  */
/* Function	- Decode and print the instruction for tracing.     */
/*		                               			    */
/*------------------------------------------------------------------*/

static void
print_ins (int i, MonoInst *ins)
{
	const char *spec = ins_spec [ins->opcode];
	g_print ("\t%-2d %s", i, mono_inst_name (ins->opcode));
	if (spec [MONO_INST_DEST]) {
		if (ins->dreg >= MONO_MAX_IREGS)
			g_print (" R%d <-", ins->dreg);
		else
			g_print (" %s <-", mono_arch_regname (ins->dreg));
	}
	if (spec [MONO_INST_SRC1]) {
		if (ins->sreg1 >= MONO_MAX_IREGS)
			g_print (" R%d", ins->sreg1);
		else
			g_print (" %s", mono_arch_regname (ins->sreg1));
	}
	if (spec [MONO_INST_SRC2]) {
		if (ins->sreg2 >= MONO_MAX_IREGS)
			g_print (" R%d", ins->sreg2);
		else
			g_print (" %s", mono_arch_regname (ins->sreg2));
	}
	if (spec [MONO_INST_CLOB])
		g_print (" clobbers: %c", spec [MONO_INST_CLOB]);
	g_print ("\n");
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- print_regtrack.                                   */
/*                                                                  */
/* Function	-                                                   */
/*		                               			    */
/*------------------------------------------------------------------*/

static void
print_regtrack (RegTrack *t, int num)
{
	int i;
	char buf [32];
	const char *r;
	
	for (i = 0; i < num; ++i) {
		if (!t [i].born_in)
			continue;
		if (i >= MONO_MAX_IREGS) {
			g_snprintf (buf, sizeof(buf), "R%d", i);
			r = buf;
		} else
			r = mono_arch_regname (i);
		g_print ("liveness: %s [%d - %d]\n", r, t [i].born_in, t[i].last_use);
	}
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- inst_list_prepend                                 */
/*                                                                  */
/* Function	- Prepend an instruction to the list.               */
/*		                               			    */
/*------------------------------------------------------------------*/

static inline InstList*
inst_list_prepend (MonoMemPool *pool, InstList *list, MonoInst *data)
{
	InstList *item = mono_mempool_alloc (pool, sizeof (InstList));
	item->data = data;
	item->prev = NULL;
	item->next = list;
	if (list)
		list->prev = item;
	return item;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- get_register_force_spilling                       */
/*                                                                  */
/* Function	- Force the spilling of the variable in the         */
/*		  symbolic register 'reg'.     			    */
/*		                               			    */
/*------------------------------------------------------------------*/

static int
get_register_force_spilling (MonoCompile *cfg, InstList *item, MonoInst *ins, int reg)
{
	MonoInst *load;
	int i, sel, spill;
	
	sel = cfg->rs->iassign [reg];
	i = reg;
	spill = ++cfg->spill_count;
	cfg->rs->iassign [i] = -spill - 1;
	mono_regstate_free_int (cfg->rs, sel);
	/*----------------------------------------------------------*/
	/* we need to create a spill var and insert a load to sel   */
	/* after the current instruction 			    */
	/*----------------------------------------------------------*/
	MONO_INST_NEW (cfg, load, OP_LOAD_MEMBASE);
	load->dreg = sel;
	load->inst_basereg = cfg->frame_reg;
	load->inst_offset = mono_spillvar_offset (cfg, spill);
	if (item->prev) {
		while (ins->next != item->prev->data)
			ins = ins->next;
	}
	load->next = ins->next;
	ins->next  = load;
	DEBUG (g_print ("SPILLED LOAD (%d at 0x%08x(%%sp)) R%d (freed %s)\n", 
			spill, load->inst_offset, i, mono_arch_regname (sel)));
	i = mono_regstate_alloc_int (cfg->rs, 1 << sel);
	g_assert (i == sel);

	return sel;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		-  get_register_spilling                            */
/*                                                                  */
/* Function	-                                                   */
/*		                               			    */
/*------------------------------------------------------------------*/

static int
get_register_spilling (MonoCompile *cfg, InstList *item, MonoInst *ins, guint32 regmask, int reg)
{
	MonoInst *load;
	int i, sel, spill;

	DEBUG (g_print ("start regmask to assign R%d: 0x%08x (R%d <- R%d R%d)\n", reg, regmask, ins->dreg, ins->sreg1, ins->sreg2));
	/* exclude the registers in the current instruction */
	if (reg != ins->sreg1 && 
	    (reg_is_freeable (ins->sreg1) || 
	     (ins->sreg1 >= MONO_MAX_IREGS && 
	      cfg->rs->iassign [ins->sreg1] >= 0))) {
		if (ins->sreg1 >= MONO_MAX_IREGS)
			regmask &= ~ (1 << cfg->rs->iassign [ins->sreg1]);
		else
			regmask &= ~ (1 << ins->sreg1);
		DEBUG (g_print ("excluding sreg1 %s\n", mono_arch_regname (ins->sreg1)));
	}
	if (reg != ins->sreg2 && 
	    (reg_is_freeable (ins->sreg2) || 
             (ins->sreg2 >= MONO_MAX_IREGS && 
              cfg->rs->iassign [ins->sreg2] >= 0))) {
		if (ins->sreg2 >= MONO_MAX_IREGS)
			regmask &= ~ (1 << cfg->rs->iassign [ins->sreg2]);
		else
			regmask &= ~ (1 << ins->sreg2);
		DEBUG (g_print ("excluding sreg2 %s %d\n", mono_arch_regname (ins->sreg2), ins->sreg2));
	}
	if (reg != ins->dreg && reg_is_freeable (ins->dreg)) {
		regmask &= ~ (1 << ins->dreg);
		DEBUG (g_print ("excluding dreg %s\n", mono_arch_regname (ins->dreg)));
	}

	DEBUG (g_print ("available regmask: 0x%08x\n", regmask));
	g_assert (regmask); /* need at least a register we can free */
	sel = -1;
	/* we should track prev_use and spill the register that's farther */
	for (i = 0; i < MONO_MAX_IREGS; ++i) {
		if (regmask & (1 << i)) {
			sel = i;
			DEBUG (g_print ("selected register %s has assignment %d\n", mono_arch_regname (sel), cfg->rs->iassign [sel]));
			break;
		}
	}
	i = cfg->rs->isymbolic [sel];
	spill = ++cfg->spill_count;
	cfg->rs->iassign [i] = -spill - 1;
	mono_regstate_free_int (cfg->rs, sel);
	/* we need to create a spill var and insert a load to sel after the current instruction */
	MONO_INST_NEW (cfg, load, OP_LOAD_MEMBASE);
	load->dreg = sel;
	load->inst_basereg = cfg->frame_reg;
	load->inst_offset = mono_spillvar_offset (cfg, spill);
	if (item->prev) {
		while (ins->next != item->prev->data)
			ins = ins->next;
	}
	load->next = ins->next;
	ins->next = load;
	DEBUG (g_print ("SPILLED LOAD (%d at 0x%08x(%%sp)) R%d (freed %s)\n", spill, load->inst_offset, i, mono_arch_regname (sel)));
	i = mono_regstate_alloc_int (cfg->rs, 1 << sel);
	g_assert (i == sel);
	
	return sel;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- get_float_register_spilling                       */
/*                                                                  */
/* Function	-                                                   */
/*		                               			    */
/*------------------------------------------------------------------*/

static int
get_float_register_spilling (MonoCompile *cfg, InstList *item, MonoInst *ins, guint32 regmask, int reg)
{
	MonoInst *load;
	int i, sel, spill;

	DEBUG (g_print ("start regmask to assign R%d: 0x%08x (R%d <- R%d R%d)\n", reg, regmask, ins->dreg, ins->sreg1, ins->sreg2));
	/* exclude the registers in the current instruction */
	if (reg != ins->sreg1 && 
	    (freg_is_freeable (ins->sreg1) || 
             (ins->sreg1 >= MONO_MAX_FREGS && 
              cfg->rs->fassign [ins->sreg1] >= 0))) {
		if (ins->sreg1 >= MONO_MAX_FREGS)
			regmask &= ~ (1 << cfg->rs->fassign [ins->sreg1]);
		else
			regmask &= ~ (1 << ins->sreg1);
		DEBUG (g_print ("excluding sreg1 %s\n", mono_arch_regname (ins->sreg1)));
	}
	if (reg != ins->sreg2 && 
            (freg_is_freeable (ins->sreg2) || 
             (ins->sreg2 >= MONO_MAX_FREGS &&
              cfg->rs->fassign [ins->sreg2] >= 0))) {
		if (ins->sreg2 >= MONO_MAX_FREGS)
			regmask &= ~ (1 << cfg->rs->fassign [ins->sreg2]);
		else
			regmask &= ~ (1 << ins->sreg2);
		DEBUG (g_print ("excluding sreg2 %s %d\n", mono_arch_regname (ins->sreg2), ins->sreg2));
	}
	if (reg != ins->dreg && freg_is_freeable (ins->dreg)) {
		regmask &= ~ (1 << ins->dreg);
		DEBUG (g_print ("excluding dreg %s\n", mono_arch_regname (ins->dreg)));
	}

	DEBUG (g_print ("available regmask: 0x%08x\n", regmask));
	g_assert (regmask); /* need at least a register we can free */
	sel = -1;
	/* we should track prev_use and spill the register that's farther */
	for (i = 0; i < MONO_MAX_FREGS; ++i) {
		if (regmask & (1 << i)) {
			sel = i;
			DEBUG (g_print ("selected register %s has assignment %d\n", 
					mono_arch_regname (sel), cfg->rs->fassign [sel]));
			break;
		}
	}
	i = cfg->rs->fsymbolic [sel];
	spill = ++cfg->spill_count;
	cfg->rs->fassign [i] = -spill - 1;
	mono_regstate_free_float(cfg->rs, sel);
	/* we need to create a spill var and insert a load to sel after the current instruction */
	MONO_INST_NEW (cfg, load, OP_LOADR8_MEMBASE);
	load->dreg = sel;
	load->inst_basereg = cfg->frame_reg;
	load->inst_offset = mono_spillvar_offset_float (cfg, spill);
	if (item->prev) {
		while (ins->next != item->prev->data)
			ins = ins->next;
	}
	load->next = ins->next;
	ins->next = load;
	DEBUG (g_print ("SPILLED LOAD (%d at 0x%08x(%%sp)) R%d (freed %s)\n", spill, load->inst_offset, i, mono_arch_regname (sel)));
	i = mono_regstate_alloc_float (cfg->rs, 1 << sel);
	g_assert (i == sel);
	
	return sel;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- create_copy_ins                                   */
/*                                                                  */
/* Function	- Create an instruction to copy from reg to reg.    */
/*		                               			    */
/*------------------------------------------------------------------*/

static MonoInst*
create_copy_ins (MonoCompile *cfg, int dest, int src, MonoInst *ins)
{
	MonoInst *copy;
	MONO_INST_NEW (cfg, copy, OP_MOVE);
	copy->dreg = dest;
	copy->sreg1 = src;
	if (ins) {
		copy->next = ins->next;
		ins->next = copy;
	}
	DEBUG (g_print ("\tforced copy from %s to %s\n", 
			mono_arch_regname (src), mono_arch_regname (dest)));
	return copy;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- create_copy_ins_float                             */
/*                                                                  */
/* Function	- Create an instruction to copy from float reg to   */
/*		  float reg.                   			    */
/*		                               			    */
/*------------------------------------------------------------------*/

static MonoInst*
create_copy_ins_float (MonoCompile *cfg, int dest, int src, MonoInst *ins)
{
	MonoInst *copy;
	MONO_INST_NEW (cfg, copy, OP_FMOVE);
	copy->dreg = dest;
	copy->sreg1 = src;
	if (ins) {
		copy->next = ins->next;
		ins->next = copy;
	}
	DEBUG (g_print ("\tforced copy from %s to %s\n", 
			mono_arch_regname (src), mono_arch_regname (dest)));
	return copy;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- create_spilled_store                              */
/*                                                                  */
/* Function	- Spill register to storage.                        */
/*		                               			    */
/*------------------------------------------------------------------*/

static MonoInst*
create_spilled_store (MonoCompile *cfg, int spill, int reg, int prev_reg, MonoInst *ins)
{
	MonoInst *store;
	MONO_INST_NEW (cfg, store, OP_STORE_MEMBASE_REG);
	store->sreg1 = reg;
	store->inst_destbasereg = cfg->frame_reg;
	store->inst_offset = mono_spillvar_offset (cfg, spill);
	if (ins) {
		store->next = ins->next;
		ins->next = store;
	}
	DEBUG (g_print ("SPILLED STORE (%d at 0x%08x(%%sp)) R%d (from %s)\n", 
			spill, store->inst_offset, prev_reg, mono_arch_regname (reg)));
	return store;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- create_spilled_store_float                        */
/*                                                                  */
/* Function	- Spill floating point register to storage.         */
/*		                               			    */
/*------------------------------------------------------------------*/

static MonoInst*
create_spilled_store_float (MonoCompile *cfg, int spill, int reg, int prev_reg, MonoInst *ins)
{
	MonoInst *store;
	MONO_INST_NEW (cfg, store, OP_STORER8_MEMBASE_REG);
	store->sreg1 = reg;
	store->inst_destbasereg = cfg->frame_reg;
	store->inst_offset = mono_spillvar_offset_float (cfg, spill);
	if (ins) {
		store->next = ins->next;
		ins->next = store;
	}
	DEBUG (g_print ("SPILLED STORE (%d at 0x%08x(%%sp)) R%d (from %s)\n", 
			spill, store->inst_offset, prev_reg, mono_arch_regname (reg)));
	return store;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- insert_before_ins                                 */
/*                                                                  */
/* Function	- Insert an instruction before another.             */
/*		                               			    */
/*------------------------------------------------------------------*/

static void
insert_before_ins (MonoInst *ins, InstList *item, MonoInst* to_insert)
{
	MonoInst *prev;
	g_assert (item->next);
	prev = item->next->data;

	while (prev->next != ins)
		prev = prev->next;
	to_insert->next = ins;
	prev->next = to_insert;
	/* 
	 * needed otherwise in the next instruction we can add an ins to the 
	 * end and that would get past this instruction.
	 */
	item->data = to_insert; 
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- alloc_int_reg                                     */
/*                                                                  */
/* Function	- Allocate a general register.                      */
/*		                               			    */
/*------------------------------------------------------------------*/

static int
alloc_int_reg (MonoCompile *cfg, InstList *curinst, MonoInst *ins, int sym_reg, guint32 allow_mask)
{
	int val = cfg->rs->iassign [sym_reg];
	DEBUG (g_print ("Allocating a general register for %d (%d) with mask %08x\n",val,sym_reg,allow_mask));
	if (val < 0) {
		int spill = 0;
		if (val < -1) {
			/* the register gets spilled after this inst */
			spill = -val -1;
		}
		val = mono_regstate_alloc_int (cfg->rs, allow_mask);
		if (val < 0)
			val = get_register_spilling (cfg, curinst, ins, allow_mask, sym_reg);
		cfg->rs->iassign [sym_reg] = val;
		/* add option to store before the instruction for src registers */
		if (spill)
			create_spilled_store (cfg, spill, val, sym_reg, ins);
	}
	DEBUG (g_print ("Allocated %d for %d\n",val,sym_reg));
	cfg->rs->isymbolic [val] = sym_reg;
	return val;
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
	MonoInst *ins;
	MonoRegState *rs = cfg->rs;
	int i, val;
	RegTrack *reginfo, *reginfof;
	RegTrack *reginfo1, *reginfo2, *reginfod;
	InstList *tmp, *reversed = NULL;
	const char *spec;
	guint32 src1_mask, src2_mask, dest_mask;
	guint32 cur_iregs, cur_fregs;

	if (!bb->code)
		return;
	rs->next_vireg = bb->max_ireg;
	rs->next_vfreg = bb->max_freg;
	mono_regstate_assign (rs);
	reginfo = mono_mempool_alloc0 (cfg->mempool, sizeof (RegTrack) * rs->next_vireg);
	reginfof = mono_mempool_alloc0 (cfg->mempool, sizeof (RegTrack) * rs->next_vfreg);
	rs->ifree_mask = S390_CALLER_REGS;
	rs->ffree_mask = S390_CALLER_FREGS;

	ins = bb->code;
	i = 1;
	DEBUG (g_print ("LOCAL regalloc: basic block: %d\n", bb->block_num));
	/* forward pass on the instructions to collect register liveness info */
	while (ins) {
		spec = ins_spec [ins->opcode];
		DEBUG (print_ins (i, ins));
//		if (spec [MONO_INST_CLOB] == 'c') {
//			MonoCallInst * call = (MonoCallInst*)ins;
//			int j;
//		}
		if (spec [MONO_INST_SRC1]) {
			if (spec [MONO_INST_SRC1] == 'f')
				reginfo1 = reginfof;
			else
				reginfo1 = reginfo;
			reginfo1 [ins->sreg1].prev_use = reginfo1 [ins->sreg1].last_use;
			reginfo1 [ins->sreg1].last_use = i;
		} else {
			ins->sreg1 = -1;
		}
		if (spec [MONO_INST_SRC2]) {
			if (spec [MONO_INST_SRC2] == 'f')
				reginfo2 = reginfof;
			else
				reginfo2 = reginfo;
			reginfo2 [ins->sreg2].prev_use = reginfo2 [ins->sreg2].last_use;
			reginfo2 [ins->sreg2].last_use = i;
		} else {
			ins->sreg2 = -1;
		}
		if (spec [MONO_INST_DEST]) {
			if (spec [MONO_INST_DEST] == 'f')
				reginfod = reginfof;
			else
				reginfod = reginfo;
			if (spec [MONO_INST_DEST] != 'b') /* it's not just a base register */
				reginfod [ins->dreg].killed_in = i;
			reginfod [ins->dreg].prev_use = reginfod [ins->dreg].last_use;
			reginfod [ins->dreg].last_use = i;
			if (reginfod [ins->dreg].born_in == 0 || reginfod [ins->dreg].born_in > i)
				reginfod [ins->dreg].born_in = i;
			if (spec [MONO_INST_DEST] == 'l') {
				/* result in R2/R3, the virtual register is allocated sequentially */
				reginfod [ins->dreg + 1].prev_use = reginfod [ins->dreg + 1].last_use;
				reginfod [ins->dreg + 1].last_use = i;
				if (reginfod [ins->dreg + 1].born_in == 0 || reginfod [ins->dreg + 1].born_in > i)
					reginfod [ins->dreg + 1].born_in = i;
			}
		} else {
			ins->dreg = -1;
		}
		reversed = inst_list_prepend (cfg->mempool, reversed, ins);
		++i;
		ins = ins->next;
	}

	cur_iregs = S390_CALLER_REGS;
	cur_fregs = S390_CALLER_FREGS;

	DEBUG (print_regtrack (reginfo, rs->next_vireg));
	DEBUG (print_regtrack (reginfof, rs->next_vfreg));
	tmp = reversed;
	while (tmp) {
		int prev_dreg, prev_sreg1, prev_sreg2;
		--i;
		ins = tmp->data;
		spec = ins_spec [ins->opcode];
		DEBUG (g_print ("processing:"));
		DEBUG (print_ins (i, ins));
		/* make the register available for allocation: FIXME add fp reg */
		if (ins->opcode == OP_SETREG || ins->opcode == OP_SETREGIMM) {
			cur_iregs |= 1 << ins->dreg;
			DEBUG (g_print ("adding %d to cur_iregs\n", ins->dreg));
		} else if (ins->opcode == OP_SETFREG) {
			cur_fregs |= 1 << ins->dreg;
			DEBUG (g_print ("adding %d to cur_fregs\n", ins->dreg));
		} else if (spec [MONO_INST_CLOB] == 'c') {
			MonoCallInst *cinst = (MonoCallInst*)ins;
			DEBUG (g_print ("excluding regs 0x%x from cur_iregs (0x%x)\n", 
					cinst->used_iregs, cur_iregs));
			DEBUG (g_print ("excluding fpregs 0x%x from cur_fregs (0x%x)\n", 
					cinst->used_fregs, cur_fregs));
			cur_iregs &= ~cinst->used_iregs;
			cur_fregs &= ~cinst->used_fregs;
			DEBUG (g_print ("available cur_iregs: 0x%x\n", cur_iregs));
			DEBUG (g_print ("available cur_fregs: 0x%x\n", cur_fregs));
			/*------------------------------------------------------------*/
			/* registers used by the calling convention are excluded from */ 
			/* allocation: they will be selectively enabled when they are */ 
			/* assigned by the special SETREG opcodes.		      */
			/*------------------------------------------------------------*/
		}
		dest_mask = src1_mask = src2_mask = cur_iregs;
		/*------------------------------------------------------*/
		/* update for use with FP regs... 			*/
		/*------------------------------------------------------*/
		if (spec [MONO_INST_DEST] == 'f') {
			dest_mask = cur_fregs;
			if (ins->dreg >= MONO_MAX_FREGS) {
				val = rs->fassign [ins->dreg];
				prev_dreg = ins->dreg;
				if (val < 0) {
					int spill = 0;
					if (val < -1) {
						/* the register gets spilled after this inst */
						spill = -val -1;
					}
					val = mono_regstate_alloc_float (rs, dest_mask);
					if (val < 0)
						val = get_float_register_spilling (cfg, tmp, ins, dest_mask, ins->dreg);
					rs->fassign [ins->dreg] = val;
					if (spill)
						create_spilled_store_float (cfg, spill, val, prev_dreg, ins);
				}
				DEBUG (g_print ("\tassigned dreg %s to dest R%d\n", 
						mono_arch_regname (val), ins->dreg));
				rs->fsymbolic [val] = prev_dreg;
				ins->dreg = val;
				if (spec [MONO_INST_CLOB] == 'c' && ins->dreg != s390_f0) {
					/* this instruction only outputs to s390_f0, need to copy */
					create_copy_ins_float (cfg, ins->dreg, s390_f0, ins);
				}
			} else {
				prev_dreg = -1;
			}
			if (freg_is_freeable (ins->dreg) && prev_dreg >= 0 && (reginfof [prev_dreg].born_in >= i || !(cur_fregs & (1 << ins->dreg)))) {
				DEBUG (g_print ("\tfreeable %s (R%d) (born in %d)\n", mono_arch_regname (ins->dreg), prev_dreg, reginfo [prev_dreg].born_in));
				mono_regstate_free_float (rs, ins->dreg);
			}
		} else if (ins->dreg >= MONO_MAX_IREGS) {
			val = rs->iassign [ins->dreg];
			prev_dreg = ins->dreg;
			if (val < 0) {
				int spill = 0;
				if (val < -1) {
					/* the register gets spilled after this inst */
					spill = -val -1;
				}
				val = mono_regstate_alloc_int (rs, dest_mask);
				if (val < 0)
					val = get_register_spilling (cfg, tmp, ins, dest_mask, ins->dreg);
				rs->iassign [ins->dreg] = val;
				if (spill)
					create_spilled_store (cfg, spill, val, prev_dreg, ins);
			}
			DEBUG (g_print ("\tassigned dreg %s to dest R%d (prev: R%d)\n", 
					mono_arch_regname (val), ins->dreg, prev_dreg));
			rs->isymbolic [val] = prev_dreg;
			ins->dreg = val;
			if (spec [MONO_INST_DEST] == 'l') {
				int hreg = prev_dreg + 1;
				val = rs->iassign [hreg];
				if (val < 0) {
					int spill = 0;
					if (val < -1) {
						/* the register gets spilled after this inst */
						spill = -val -1;
					}
					val = mono_regstate_alloc_int (rs, dest_mask);
					if (val < 0)
						val = get_register_spilling (cfg, tmp, ins, dest_mask, hreg);
					rs->iassign [hreg] = val;
					if (spill)
						create_spilled_store (cfg, spill, val, hreg, ins);
				}
				DEBUG (g_print ("\tassigned hreg %s to dest R%d\n", mono_arch_regname (val), hreg));
				rs->isymbolic [val] = hreg;
				/* FIXME:? ins->dreg = val; */
				if (ins->dreg == s390_r3) {
					if (val != s390_r2)
						create_copy_ins (cfg, val, s390_r2, ins);
				} else if (ins->dreg == s390_r2) {
					if (val == s390_r3) {
						/* swap */
						create_copy_ins (cfg, s390_r3, s390_r0, ins);
						create_copy_ins (cfg, s390_r2, s390_r3, ins);
						create_copy_ins (cfg, s390_r0, s390_r2, ins);
					} else {
						/* two forced copies */
						create_copy_ins (cfg, ins->dreg, s390_r3, ins);
						create_copy_ins (cfg, val, s390_r2, ins);
					}
				} else {
					if (val == s390_r2) {
						create_copy_ins (cfg, ins->dreg, s390_r2, ins);
					} else {
						/* two forced copies */
						create_copy_ins (cfg, val, s390_r2, ins);
						create_copy_ins (cfg, ins->dreg, s390_r3, ins);
					}
				}
				if (reg_is_freeable (val) && 
				    hreg >= 0 && 
                                    (reginfo [hreg].born_in >= i && 
                                     !(cur_iregs & (1 << val)))) {
					DEBUG (g_print ("\tfreeable %s (R%d)\n", mono_arch_regname (val), hreg));
					mono_regstate_free_int (rs, val);
				}
			} else if (spec [MONO_INST_DEST] == 'a' && ins->dreg != s390_r2 && spec [MONO_INST_CLOB] != 'd') {
				/* this instruction only outputs to s390_r2, need to copy */
				create_copy_ins (cfg, ins->dreg, s390_r2, ins);
			}
		} else {
			prev_dreg = -1;
		}
		if (spec [MONO_INST_DEST] == 'f' && 
		    freg_is_freeable (ins->dreg) && 
		    prev_dreg >= 0 && (reginfof [prev_dreg].born_in >= i)) {
			DEBUG (g_print ("\tfreeable %s (R%d) (born in %d)\n", mono_arch_regname (ins->dreg), prev_dreg, reginfo [prev_dreg].born_in));
			mono_regstate_free_float (rs, ins->dreg);
		} else if (spec [MONO_INST_DEST] != 'f' && 
			   reg_is_freeable (ins->dreg) && 
			   prev_dreg >= 0 && (reginfo [prev_dreg].born_in >= i)) {
			DEBUG (g_print ("\tfreeable %s (R%d) (born in %d)\n", mono_arch_regname (ins->dreg), prev_dreg, reginfo [prev_dreg].born_in));
			 mono_regstate_free_int (rs, ins->dreg);
		}
		if (spec [MONO_INST_SRC1] == 'f') {
			src1_mask = cur_fregs;
			if (ins->sreg1 >= MONO_MAX_FREGS) {
				val = rs->fassign [ins->sreg1];
				prev_sreg1 = ins->sreg1;
				if (val < 0) {
					int spill = 0;
					if (val < -1) {
						/* the register gets spilled after this inst */
						spill = -val -1;
					}
					val = mono_regstate_alloc_float (rs, src1_mask);
					if (val < 0)
						val = get_float_register_spilling (cfg, tmp, ins, src1_mask, ins->sreg1);
					rs->fassign [ins->sreg1] = val;
					DEBUG (g_print ("\tassigned sreg1 %s to R%d\n", mono_arch_regname (val), ins->sreg1));
					if (spill) {
						MonoInst *store = create_spilled_store_float (cfg, spill, val, prev_sreg1, NULL);
						insert_before_ins (ins, tmp, store);
					}
				}
				rs->fsymbolic [val] = prev_sreg1;
				ins->sreg1 = val;
			} else {
				prev_sreg1 = -1;
			}
		} else if (ins->sreg1 >= MONO_MAX_IREGS) {
			val = rs->iassign [ins->sreg1];
			prev_sreg1 = ins->sreg1;
			if (val < 0) {
				int spill = 0;
				if (val < -1) {
					/* the register gets spilled after this inst */
					spill = -val -1;
				}
				val = mono_regstate_alloc_int (rs, src1_mask);
				if (val < 0)
					val = get_register_spilling (cfg, tmp, ins, 
								     src1_mask, 
								     ins->sreg1);
				rs->iassign [ins->sreg1] = val;
				DEBUG (g_print ("\tassigned sreg1 %s to R%d\n", 
						mono_arch_regname (val), ins->sreg1));
				if (spill) {
					MonoInst *store; 
					store = create_spilled_store (cfg, spill, val, 
								      prev_sreg1, NULL);
					insert_before_ins (ins, tmp, store);
				}
			}
			rs->isymbolic [val] = prev_sreg1;
			ins->sreg1 = val;
		} else {
			prev_sreg1 = -1;
		}
		/*----------------------------------------------*/
		/* handle clobbering of sreg1 			*/
		/*----------------------------------------------*/
		if ((spec [MONO_INST_CLOB] == '1' || 
		     spec [MONO_INST_CLOB] == 's') && 
                    ins->dreg != ins->sreg1) {
			MonoInst *copy; 
			copy = create_copy_ins (cfg, ins->dreg, ins->sreg1, NULL);
			DEBUG (g_print ("\tneed to copy sreg1 %s to dreg %s\n", 
					mono_arch_regname (ins->sreg1), 
					mono_arch_regname (ins->dreg)));
			if (ins->sreg2 == -1 || spec [MONO_INST_CLOB] == 's') {
				/* note: the copy is inserted before the current instruction! */
				insert_before_ins (ins, tmp, copy);
				/* we set sreg1 to dest as well */
				prev_sreg1 = ins->sreg1 = ins->dreg;
			} else {
				/* inserted after the operation */
				copy->next = ins->next;
				ins->next  = copy;
			}
		}

		if (spec [MONO_INST_SRC2] == 'f') {
			src2_mask = cur_fregs;
			if (ins->sreg2 >= MONO_MAX_FREGS) {
				val = rs->fassign [ins->sreg2];
				prev_sreg2 = ins->sreg2;
				if (val < 0) {
					int spill = 0;
					if (val < -1) {
						/* the register gets spilled after this inst */
						spill = -val -1;
					}
					val = mono_regstate_alloc_float (rs, src2_mask);
					if (val < 0)
						val = get_float_register_spilling (cfg, tmp, ins, src2_mask, ins->sreg2);
					rs->fassign [ins->sreg2] = val;
					DEBUG (g_print ("\tassigned sreg2 %s to R%d\n", mono_arch_regname (val), ins->sreg2));
					if (spill)
						create_spilled_store_float (cfg, spill, val, prev_sreg2, ins);
				}
				rs->fsymbolic [val] = prev_sreg2;
				ins->sreg2 = val;
			} else {
				prev_sreg2 = -1;
			}
		} else if (ins->sreg2 >= MONO_MAX_IREGS) {
			val = rs->iassign [ins->sreg2];
			prev_sreg2 = ins->sreg2;
			if (val < 0) {
				int spill = 0;
				if (val < -1) {
					/* the register gets spilled after this inst */
					spill = -val -1;
				}
				val = mono_regstate_alloc_int (rs, src2_mask);
				if (val < 0)
					val = get_register_spilling (cfg, tmp, ins, src2_mask, ins->sreg2);
				rs->iassign [ins->sreg2] = val;
				DEBUG (g_print ("\tassigned sreg2 %s to R%d\n", mono_arch_regname (val), ins->sreg2));
				if (spill)
					create_spilled_store (cfg, spill, val, prev_sreg2, ins);
			}
			rs->isymbolic [val] = prev_sreg2;
			ins->sreg2 = val;
		} else {
			prev_sreg2 = -1;
		}

		if (spec [MONO_INST_CLOB] == 'c') {
			int j, s;
			guint32 clob_mask = S390_CALLER_REGS;
			for (j = 0; j < MONO_MAX_IREGS; ++j) {
				s = 1 << j;
				if ((clob_mask & s) && !(rs->ifree_mask & s) && j != ins->sreg1) {
					//g_warning ("register %s busy at call site\n", mono_arch_regname (j));
				}
			}
		}
		tmp = tmp->next;
	}
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
		s390_basr   (code, s390_r13, 0);
		s390_j	    (code, 10);
		s390_llong  (code, 0x41e0000000000000);
		s390_llong  (code, 0x41f0000000000000);
		s390_ldr    (code, s390_f15, sreg);
		s390_cdb    (code, s390_f15, 0, s390_r13, 0);
		s390_jl     (code, 10);
		s390_sdb    (code, s390_f15, 0, s390_r13, 8);
		s390_cfdbr  (code, dreg, 7, s390_f15);
		s390_j      (code, 4);
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
/* Name		- mono_emit_stack_alloc                             */
/*                                                                  */
/* Function	-                                                   */
/*		                               			    */
/*------------------------------------------------------------------*/

static unsigned char*
mono_emit_stack_alloc (guchar *code, MonoInst* tree)
{
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
	int max_len, cpos;
guint8 cond;

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
			    ((ins->next->opcode >= CEE_BNE_UN) &&
			     (ins->next->opcode <= CEE_BLT_UN)) || 
			    ((ins->next->opcode >= OP_COND_EXC_NE_UN) &&
			     (ins->next->opcode <= OP_COND_EXC_LT_UN)) ||
			    ((ins->next->opcode == OP_CLT_UN) ||
			     (ins->next->opcode == OP_CGT_UN)))
				s390_clr  (code, ins->sreg1, ins->sreg2);
			else
				s390_cr   (code, ins->sreg1, ins->sreg2);
		}
			break;
		case OP_COMPARE_IMM: {
			if (s390_is_imm16 (ins->inst_imm)) {
				s390_lhi  (code, s390_r0, ins->inst_imm);
				if ((ins->next) && 
				    ((ins->next->opcode >= CEE_BNE_UN) &&
				     (ins->next->opcode <= CEE_BLT_UN)) || 
				    ((ins->next->opcode >= OP_COND_EXC_NE_UN) &&
				     (ins->next->opcode <= OP_COND_EXC_LT_UN)) ||
				    ((ins->next->opcode == OP_CLT_UN) ||
				     (ins->next->opcode == OP_CGT_UN)))
					s390_clr  (code, ins->sreg1, s390_r0);
				else
					s390_cr   (code, ins->sreg1, s390_r0);
			}
			else {
				s390_basr (code, s390_r13, 0);
				s390_j    (code, 4);
				s390_word (code, ins->inst_imm);
				if ((ins->next) && 
				    ((ins->next->opcode >= CEE_BNE_UN) &&
				     (ins->next->opcode <= CEE_BLT_UN)) || 
				    ((ins->next->opcode >= OP_COND_EXC_NE_UN) &&
				     (ins->next->opcode <= OP_COND_EXC_LT_UN)) ||
				    ((ins->next->opcode == OP_CLT_UN) &&
				     (ins->next->opcode == OP_CGT_UN)))
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
			s390_break (code);
		}
			break;
		case OP_ADDCC: {
			if (ins->dreg != ins->sreg1) {
				s390_lr	  (code, ins->dreg, ins->sreg1);
			}
			s390_alr  (code, ins->dreg, ins->sreg2);
		}
			break;
		case CEE_ADD: {
			if (ins->dreg != ins->sreg1) {
				s390_lr	  (code, ins->dreg, ins->sreg1);
			}
			s390_ar   (code, ins->dreg, ins->sreg2);
		}
			break;
		case OP_ADC: {
			if (ins->dreg != ins->sreg1) {
				s390_lr	  (code, ins->dreg, ins->sreg1);
			}
			s390_alcr (code, ins->dreg, ins->sreg2);
		}
			break;
		case OP_ADDCC_IMM:
		case OP_ADD_IMM: {
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
					s390_ahi (code, ins->dreg, ins->inst_imm);
				} else {
					s390_basr (code, s390_r13, 0);
					s390_j	  (code, 4);
					s390_word (code, ins->inst_imm);
					if (ins->dreg != ins->sreg1) {
						s390_lr	  (code, ins->dreg, ins->sreg1);
					}
					s390_a    (code, ins->dreg, 0, s390_r13, 4);
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
			if (ins->dreg != ins->sreg1) {
				s390_lr	  (code, ins->dreg, ins->sreg1);
			}
			s390_ar   (code, ins->dreg, ins->sreg2);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_OV, "OverflowException");
		}
			break;
		case CEE_ADD_OVF_UN: {
			if (ins->dreg != ins->sreg1) {
				s390_lr	  (code, ins->dreg, ins->sreg1);
			}
			s390_alr  (code, ins->dreg, ins->sreg2);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_CY, "OverflowException");
		}
			break;
		case OP_ADD_OVF_CARRY: {
			if (ins->dreg != ins->sreg1) {
				s390_lr	  (code, ins->dreg, ins->sreg1);
			}
			s390_lhi  (code, s390_r0, 0);
			s390_lr   (code, s390_r1, s390_r0);
			s390_alcr (code, s390_r0, s390_r1);
			s390_ar   (code, ins->dreg, ins->sreg2);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_OV, "OverflowException");
			s390_ar   (code, ins->dreg, s390_r0);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_OV, "OverflowException");
		}
			break;
		case OP_ADD_OVF_UN_CARRY: {
			if (ins->dreg != ins->sreg1) {
				s390_lr	  (code, ins->dreg, ins->sreg1);
			}
			s390_alcr (code, ins->dreg, ins->sreg2);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_CY, "OverflowException");
		}
			break;
		case OP_SUBCC: {
			if (ins->dreg != ins->sreg1) {
				s390_lr	  (code, ins->dreg, ins->sreg1);
			}
			s390_slr (code, ins->dreg, ins->sreg2);
		}
			break;
		case CEE_SUB: {
			if (ins->dreg != ins->sreg1) {
				s390_lr	  (code, ins->dreg, ins->sreg1);
			}
			s390_sr   (code, ins->dreg, ins->sreg2);
		}
			break;
		case OP_SBB: {
			if (ins->dreg != ins->sreg1) {
				s390_lr	  (code, ins->dreg, ins->sreg1);
			}
			s390_slbr (code, ins->dreg, ins->sreg2);
		}
			break;
		case OP_SUBCC_IMM:
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
			s390_basr (code, s390_r13, 0);
			s390_j	  (code, 4);
			s390_word (code, ins->inst_imm);
			s390_sl   (code, ins->dreg, 0, s390_r13, 4);
		}
			break;
		case CEE_SUB_OVF: {
			if (ins->dreg != ins->sreg1) {
				s390_lr	  (code, ins->dreg, ins->sreg1);
			}
			s390_sr   (code, ins->dreg, ins->sreg2);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_OV, "OverflowException");
		}
			break;
		case CEE_SUB_OVF_UN: {
			if (ins->dreg != ins->sreg1) {
				s390_lr	  (code, ins->dreg, ins->sreg1);
			}
			s390_slr  (code, ins->dreg, ins->sreg2);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_NC, "OverflowException");
		}
			break;
		case OP_SUB_OVF_CARRY: {
			if (ins->dreg != ins->sreg1) {
				s390_lr	  (code, ins->dreg, ins->sreg1);
			}
			s390_lhi  (code, s390_r0, 0);
			s390_lr   (code, s390_r1, s390_r0);
			s390_slbr (code, s390_r0, s390_r1);
			s390_sr   (code, ins->dreg, ins->sreg2);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_OV, "OverflowException");
			s390_ar   (code, ins->dreg, s390_r0);
			EMIT_COND_SYSTEM_EXCEPTION (S390_CC_OV, "OverflowException");
		}
			break;
		case OP_SUB_OVF_UN_CARRY: {
			if (ins->dreg != ins->sreg1) {
				s390_lr	  (code, ins->dreg, ins->sreg1);
			}
			s390_slbr (code, ins->dreg, ins->sreg2);
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
			if (ins->sreg1 != ins->dreg) {
				s390_lr   (code, ins->dreg, ins->sreg1);
			}
			s390_sll  (code, ins->dreg, ins->sreg2, 0);
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
			if (ins->sreg1 != ins->dreg) {
				s390_lr   (code, ins->dreg, ins->sreg1);
			}
			s390_sra  (code, ins->dreg, ins->sreg2, 0);
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
			if (ins->sreg1 != ins->dreg) {
				s390_lr   (code, ins->dreg, ins->sreg1);
			}
			s390_srl  (code, ins->dreg, ins->sreg2, 0);
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
				if (ins->dreg != ins->sreg1) {
					s390_lr   (code, ins->dreg, ins->sreg1);
				}
				s390_l    (code, s390_r13, 0, s390_r13, 4);
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
		case OP_FCONV_TO_R4: {
			if ((ins->next) &&
			    (ins->next->opcode != OP_STORER4_MEMBASE_REG))
				s390_ledbr (code, ins->dreg, ins->sreg1);
		}
			break;
		case CEE_JMP: {
			int fParm;
			if (cfg->method->save_lmf)
				code = restoreLMF(cfg, code);

			if (cfg->flags & MONO_CFG_HAS_TAIL) {
				s390_lm (code, s390_r2, s390_r5, STK_BASE, 
					 S390_PARM_SAVE_OFFSET);
				for (fParm = 0; fParm < 4; fParm++)
					s390_ld (code, fParm, 0, STK_BASE,
					   S390_FLOAT_SAVE_OFFSET+fParm*sizeof(double));
			}

			code = backUpStackPtr(cfg, code);
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
			NOT_IMPLEMENTED("OP_ARGLIST");
			s390_basr (code, s390_r13, 0);
			s390_j    (code, 4);
			s390_word (code, cfg->sig_cookie);
			s390_mvc  (code, 4, ins->sreg1, 0, s390_r13, 4);
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
			int alloca_skip = S390_MINIMAL_STACK_SIZE + cfg->param_area + 
					  S390_STACK_ALIGNMENT - 1;
			int area_offset = S390_ALIGN(alloca_skip, S390_STACK_ALIGNMENT);
			s390_lr   (code, s390_r1, ins->sreg1);
			s390_ahi  (code, s390_r1, 14);
			s390_srl  (code, s390_r1, 0, 3);
			s390_sll  (code, s390_r1, 0, 3);
			s390_l	  (code, s390_r13, 0, STK_BASE, 0);
			s390_lcr  (code, s390_r1, s390_r1);
			s390_la	  (code, STK_BASE, STK_BASE, s390_r1, 0);
			s390_st   (code, s390_r13, 0, STK_BASE, 0);
			s390_la   (code, ins->dreg, 0, STK_BASE, area_offset);
			s390_srl  (code, ins->dreg, 0, 3);
			s390_sll  (code, ins->dreg, 0, 3);
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
		case CEE_RETHROW: {
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
			s390_ltr  (code, ins->sreg2, ins->sreg2);
			s390_jnl  (code, 0); CODEPTR(code, o[0]);
			s390_ltr  (code, ins->sreg1, ins->sreg1);
			s390_jnl  (code, 0); CODEPTR(code, o[1]);
			s390_lhi  (code, s390_r13, -1);
			s390_cr   (code, ins->sreg1, s390_r13);
			s390_jnz  (code, 0); CODEPTR(code, o[2]);
			if (ins->dreg != ins->sreg2)
				s390_lr   (code, ins->dreg, ins->sreg2);
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
		case OP_SQRT: {
			s390_sqdbr (code, ins->dreg, ins->sreg1);
		}
			break;
		case OP_FADD: {
			if (ins->dreg == ins->sreg1)
				s390_adbr (code, ins->dreg, ins->sreg2);
			else {
				if (ins->dreg == ins->sreg2)
					s390_adbr (code, ins->dreg, ins->sreg1);
				else {
					s390_ldr  (code, ins->dreg, ins->sreg1);
					s390_adbr (code, ins->dreg, ins->sreg2);
				}
			}
		}
			break;
		case OP_FSUB: {
			if (ins->dreg == ins->sreg1)
				s390_sdbr (code, ins->dreg, ins->sreg2);
			else {
				s390_ldr  (code, ins->dreg, ins->sreg1);
				s390_sdbr (code, ins->dreg, ins->sreg2);
			}
		}
			break;		
		case OP_FMUL: {
			if (ins->dreg == ins->sreg1)
				s390_mdbr (code, ins->dreg, ins->sreg2);
			else {
				if (ins->dreg == ins->sreg2)
					s390_mdbr (code, ins->dreg, ins->sreg1);
				else {
					s390_ldr  (code, ins->dreg, ins->sreg1);
					s390_mdbr (code, ins->dreg, ins->sreg2);
				}
			}
		}
			break;		
		case OP_FDIV: {
			if (ins->dreg == ins->sreg1)
				s390_ddbr (code, ins->dreg, ins->sreg2);
			else {
				s390_ldr  (code, ins->dreg, ins->sreg1);
				s390_ddbr (code, ins->dreg, ins->sreg2);
			}
		}
			break;		
		case OP_FNEG: {
			s390_lcdbr (code, ins->dreg, ins->sreg1);
		}
			break;		
		case OP_FREM: {
			if (ins->dreg != ins->sreg1) {
				s390_ldr  (code, ins->dreg, ins->sreg1);
			}
			s390_didbr (code, ins->dreg, ins->sreg2, 5, s390_f15);
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
	mono_register_jit_icall (enter_method, "mono_enter_method", NULL, TRUE);
	mono_register_jit_icall (leave_method, "mono_leave_method", NULL, TRUE);
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
mono_arch_patch_code (MonoMethod *method, MonoDomain *domain, guint8 *code, MonoJumpInfo *ji, gboolean run_cctors)
{
	MonoJumpInfo *patch_info;

	for (patch_info = ji; patch_info; patch_info = patch_info->next) {
		unsigned char *ip = patch_info->ip.i + code;
		gint32 target = 0;

		switch (patch_info->type) {
		case MONO_PATCH_INFO_BB:
			target = S390_RELATIVE((patch_info->data.bb->native_offset+code),
					       ip);
			ip    += 2;	/* Skip over op-code */
			break;
		case MONO_PATCH_INFO_ABS:
			target = S390_RELATIVE(patch_info->data.target, ip);
			ip    += 2;	/* Skip over op-code */
			break;
		case MONO_PATCH_INFO_LABEL:
			target = S390_RELATIVE((patch_info->data.inst->inst_c0+code),ip);
			ip    += 2;	/* Skip over op-code */
			break;
		case MONO_PATCH_INFO_IP:
			target = ip;
			continue;
		case MONO_PATCH_INFO_METHOD_REL:
			g_assert_not_reached ();
			*((gpointer *)(ip)) = code + patch_info->data.offset;
			continue;
		case MONO_PATCH_INFO_INTERNAL_METHOD: {
			MonoJitICallInfo *mi = mono_find_jit_icall_by_name (patch_info->data.name);
			if (!mi) {
				g_warning ("unknown MONO_PATCH_INFO_INTERNAL_METHOD %s", patch_info->data.name);
				g_assert_not_reached ();
			}
			target = S390_RELATIVE(mono_icall_get_wrapper (mi), ip);
			ip    += 2;	/* Skip over op-code */
			break;
		}
		case MONO_PATCH_INFO_METHOD_JUMP: {
			GSList *list;

			/*------------------------------------------------------*/
			/* get the trampoline to the method from the domain 	*/
			/*------------------------------------------------------*/
			target = mono_create_jump_trampoline (domain, 
						      patch_info->data.method, 
						      TRUE);
			target = S390_RELATIVE(target, ip);
			if (!domain->jump_target_hash)
				domain->jump_target_hash = g_hash_table_new (NULL, NULL);
			list = g_hash_table_lookup (domain->jump_target_hash, 
						    patch_info->data.method);
			list = g_slist_prepend (list, ip);
			g_hash_table_insert (domain->jump_target_hash, 
					     patch_info->data.method, list);
			ip  +=2;
			break;
		}
		case MONO_PATCH_INFO_METHOD:
			if (patch_info->data.method == method) {
				target = S390_RELATIVE(code, ip);
			} else {
				/* get the trampoline to the method from the domain */
				target = S390_RELATIVE(mono_arch_create_jit_trampoline (patch_info->data.method), ip);
				target = mono_arch_create_jit_trampoline(patch_info->data.method);
				target = S390_RELATIVE(target, ip);
			}
			ip    += 2;	/* Skip over op-code */
			break;
		case MONO_PATCH_INFO_SWITCH: {
			gpointer *table = (gpointer *)patch_info->data.target;
			int i;
			/*------------------------------------------------------*/
			/* ip is pointing at the basr r13,0/j +4 instruction    */
			/* the vtable value follows this (i.e. ip+6)		*/
			/*------------------------------------------------------*/
			*((gconstpointer *)(ip+6)) = table;

			for (i = 0; i < patch_info->table_size; i++) {
				table [i] = (int)patch_info->data.table [i] + code;
			}
			continue;
		}
		case MONO_PATCH_INFO_METHODCONST:
		case MONO_PATCH_INFO_CLASS:
		case MONO_PATCH_INFO_IMAGE:
		case MONO_PATCH_INFO_FIELD:
			target = S390_RELATIVE(patch_info->data.target, ip);
			continue;
		case MONO_PATCH_INFO_R4:
		case MONO_PATCH_INFO_R8:
			g_assert_not_reached ();
			*((gconstpointer *)(ip + 2)) = patch_info->data.target;
			continue;
		case MONO_PATCH_INFO_IID:
			mono_class_init (patch_info->data.klass);
			target = S390_RELATIVE(patch_info->data.klass->interface_id, ip);
			continue;			
		case MONO_PATCH_INFO_VTABLE:
			target = S390_RELATIVE(mono_class_vtable (domain, patch_info->data.klass),ip);
			ip += 2;
			continue;
		case MONO_PATCH_INFO_CLASS_INIT:
			target = S390_RELATIVE(mono_create_class_init_trampoline (mono_class_vtable (domain, patch_info->data.klass)), ip);
			ip += 2;
			break;
		case MONO_PATCH_INFO_SFLDA: {
			MonoVTable *vtable = mono_class_vtable (domain, patch_info->data.field->parent);
			if (!vtable->initialized && !(vtable->klass->flags & TYPE_ATTRIBUTE_BEFORE_FIELD_INIT) && mono_class_needs_cctor_run (vtable->klass, method))
				/* Done by the generated code */
				;
			else {
				if (run_cctors)
					mono_runtime_class_init (vtable);
			}
			target = S390_RELATIVE((char*)vtable->data + patch_info->data.field->offset, ip);
			ip += 2;
			continue;
		}
		case MONO_PATCH_INFO_EXC_NAME:
			*((gconstpointer *)(ip)) = patch_info->data.name;
			continue;
		case MONO_PATCH_INFO_LDSTR:
			target = mono_ldstr (domain, patch_info->data.token->image, 
				 	     mono_metadata_token_index (patch_info->data.token->token));
			continue;
		case MONO_PATCH_INFO_TYPE_FROM_HANDLE: {
			gpointer handle;
			MonoClass *handle_class;

			handle = mono_ldtoken (patch_info->data.token->image, 
	  				       patch_info->data.token->token, 
					       &handle_class, NULL);
			mono_class_init (handle_class);
			mono_class_init (mono_class_from_mono_type (handle));

			target = handle;
			continue;
		}
		case MONO_PATCH_INFO_LDTOKEN: {
			gpointer handle;
			MonoClass *handle_class;

			handle = mono_ldtoken (patch_info->data.token->image,
					       patch_info->data.token->token, 
					       &handle_class, NULL);
			mono_class_init (handle_class);

			target = handle;
			continue;
		}
		case MONO_PATCH_INFO_EXC:
			/* everything is dealt with at epilog output time */
			continue;
		default:
			g_assert_not_reached ();
		}
		s390_patch (ip, target);
	}
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_max_epilog_size                         */
/*                                                                  */
/* Function	- Determine the maximum size of the epilog code.    */
/*		                               			    */
/*------------------------------------------------------------------*/

int
mono_arch_max_epilog_size (MonoCompile *cfg)
{
	int max_epilog_size = 96;
	MonoJumpInfo *patch_info;
	
	if (cfg->method->save_lmf)
		max_epilog_size += 128;
	
	if (mono_jit_trace_calls != NULL)
		max_epilog_size += 128;

	if (cfg->prof_options & MONO_PROFILE_ENTER_LEAVE)
		max_epilog_size += 128;

	/* count the number of exception infos */
     
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		if (patch_info->type == MONO_PATCH_INFO_EXC)
			max_epilog_size += 26;
	}

	return max_epilog_size;
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
	int alloc_size, pos, max_offset, i, lmfOffset;
	guint8 *code;
	CallInfo *cinfo;
	size_data sz;
	int tracing = 0;

	if (mono_jit_trace_calls != NULL && mono_trace_eval (method))
		tracing = 1;

	cfg->code_size   = 512;
	cfg->native_code = code = g_malloc (cfg->code_size);

	if (cfg->flags & MONO_CFG_HAS_TAIL) {
		s390_stm (code, s390_r2, s390_r14, STK_BASE, S390_PARM_SAVE_OFFSET);
		for (pos = 0; pos < 4; pos++)
			s390_std (code, pos, 0, STK_BASE, 
				  S390_FLOAT_SAVE_OFFSET+pos*sizeof(double));
	} else { 
		s390_stm  (code, s390_r6, s390_r14, STK_BASE, S390_REG_SAVE_OFFSET);
        }

	if (cfg->flags & MONO_CFG_HAS_ALLOCA) {
		cfg->used_int_regs |= 1 << 11;
	}

	alloc_size = cfg->stack_offset;

	cfg->stack_usage = alloc_size;
	s390_lr   (code, s390_r11, STK_BASE);
	if (s390_is_imm16 (-alloc_size)) {
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
	sig = method->signature;
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
				if (ainfo->reg == STK_BASE) {
					s390_lr  (code, s390_r13, ainfo->reg);
					s390_ahi (code, s390_r13, alloc_size);
					s390_l   (code, s390_r13, 0, s390_r13, 
						  ainfo->offparm + S390_MINIMAL_STACK_SIZE);
					code = emit_memcpy (code, abs(ainfo->vtsize), 
							    inst->inst_basereg, 
							    inst->inst_offset, s390_r13, 0);
				} else {
					code = emit_memcpy (code, abs(ainfo->vtsize), 
							    inst->inst_basereg, 
							    inst->inst_offset, 
						    	    ainfo->reg, 0);
				}
			} else
				g_assert_not_reached ();
		}
		pos++;
	}

	if (method->save_lmf) {
		/*---------------------------------------------------------------*/
		/* Preserve the parameter registers while we fix up the lmf	 */
		/*---------------------------------------------------------------*/
		s390_lr (code, s390_r7, s390_r2);
		s390_lr (code, s390_r8, s390_r3);
		s390_lr (code, s390_r9, s390_r4);
		s390_lr (code, s390_r10, s390_r5);

		mono_add_patch_info (cfg, code - cfg->native_code, 
				     MONO_PATCH_INFO_INTERNAL_METHOD, 
				     (gpointer)"mono_get_lmf_addr");
		/*---------------------------------------------------------------*/
		/* On return from this call r2 have the address of the &lmf	 */
		/*---------------------------------------------------------------*/
		s390_brasl (code, s390_r14, 0);

		/*---------------------------------------------------------------*/
		/* we build the MonoLMF structure on the stack - see mini-s390.h */
		/*---------------------------------------------------------------*/
		lmfOffset = alloc_size - sizeof(MonoLMF);

		s390_lr    (code, s390_r13, cfg->frame_reg);
		s390_ahi   (code, s390_r13, lmfOffset);

		/*---------------------------------------------------------------*/
		/* Set lmf.lmf_addr = jit_tls->lmf				 */
		/*---------------------------------------------------------------*/
		s390_st    (code, s390_r2, 0, s390_r13, G_STRUCT_OFFSET(MonoLMF, lmf_addr));

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
		s390_st	   (code, s390_r0, 0, s390_r13, G_STRUCT_OFFSET(MonoLMF, previous_lmf));

		/*---------------------------------------------------------------*/
		/* save method info						 */
		/*---------------------------------------------------------------*/
		s390_basr  (code, s390_r1, 0);
		s390_j	   (code, 4);
		s390_word  (code, method);
		s390_l	   (code, s390_r1, 0, s390_r1, 4);
		s390_st    (code, s390_r1, 0, s390_r13, G_STRUCT_OFFSET(MonoLMF, method));

		/*---------------------------------------------------------------*/
		/* save the current IP						 */
		/*---------------------------------------------------------------*/
		s390_lr    (code, s390_r1, cfg->frame_reg);
		s390_st	   (code, s390_r1, 0, s390_r13, G_STRUCT_OFFSET(MonoLMF, ebp));
		s390_l     (code, s390_r1, 0, s390_r1, S390_RET_ADDR_OFFSET);
		s390_la    (code, s390_r1, 0, s390_r1, 0);
		s390_st    (code, s390_r1, 0, s390_r13, G_STRUCT_OFFSET(MonoLMF, eip));

		/*---------------------------------------------------------------*/
		/* Save general and floating point registers			 */
		/*---------------------------------------------------------------*/
		s390_stm   (code, s390_r2, s390_r12, s390_r13, G_STRUCT_OFFSET(MonoLMF, gregs[2]));
		for (i = 0; i < 16; i++) {
			s390_std  (code, i, 0, s390_r13, G_STRUCT_OFFSET(MonoLMF, fregs[i]));
		}

		/*---------------------------------------------------------------*/
		/* Restore the parameter registers now that we've set up the lmf */
		/*---------------------------------------------------------------*/
		s390_lr (code, s390_r2, s390_r7);
		s390_lr (code, s390_r3, s390_r8);
		s390_lr (code, s390_r4, s390_r9);
		s390_lr (code, s390_r5, s390_r10);
	}

	if (tracing)
		code = mono_arch_instrument_prolog (cfg, enter_method, code, TRUE);

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
	MonoJumpInfo *patch_info;
	MonoMethod *method = cfg->method;
	MonoMethodSignature *sig = method->signature;
	MonoInst *inst;
	int i, tracing = 0;
	guint8 *code;

	code = cfg->native_code + cfg->code_len;

	if (mono_jit_trace_calls != NULL && mono_trace_eval (method)) {
		code = mono_arch_instrument_epilog (cfg, leave_method, code, TRUE);
		tracing = 1;
	}
	
	if (method->save_lmf) 
		code = restoreLMF(cfg, code);

	if (cfg->flags & MONO_CFG_HAS_ALLOCA) 
		s390_l	 (code, STK_BASE, 0, STK_BASE, 0);
	else
		code = backUpStackPtr(cfg, code);

	s390_lm  (code, s390_r6, s390_r14, STK_BASE, S390_REG_SAVE_OFFSET);
	s390_br  (code, s390_r14);

	/* add code to raise exceptions */
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		switch (patch_info->type) {
		case MONO_PATCH_INFO_EXC: {
			/*-----------------------------------------------------*/
			/* Patch the branch in epilog to come here	       */
			/*-----------------------------------------------------*/
			s390_patch (patch_info->ip.i+cfg->native_code+2, 
				    S390_RELATIVE(code,patch_info->ip.i+cfg->native_code));
			/*-----------------------------------------------------*/
			/* Patch the parameter passed to the handler	       */ 
			/*-----------------------------------------------------*/
			s390_basr (code, s390_r13, 0);
			s390_j	  (code, 4);
			mono_add_patch_info (cfg, code - cfg->native_code,
					     MONO_PATCH_INFO_EXC_NAME,
					     patch_info->data.target);
			s390_word (code, 0);
			/*-----------------------------------------------------*/
			/* Load the return address and the parameter register  */
			/*-----------------------------------------------------*/
			s390_larl (code, s390_r14, S390_RELATIVE((patch_info->ip.i +
						   cfg->native_code + 8), code));
			s390_l    (code, s390_r2, 0, s390_r13, 4);
			/*-----------------------------------------------------*/
			/* Reuse the current patch to set the jump	       */
			/*-----------------------------------------------------*/
			patch_info->type      = MONO_PATCH_INFO_INTERNAL_METHOD;
			patch_info->data.name = "mono_arch_throw_exception_by_name";
			patch_info->ip.i      = code - cfg->native_code;
			s390_jcl  (code, S390_CC_UN, 0);
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
#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK
	pthread_t 	self = pthread_self();
	pthread_attr_t 	attr;
	void 		*stAddr = NULL;
	size_t 		stSize  = 0;
	struct sigaltstack sa;
#endif

	if (!tls_offset_inited) {
		tls_offset_inited = TRUE;

//		lmf_tls_offset = read_tls_offset_from_method (mono_get_lmf_addr);
//		appdomain_tls_offset = read_tls_offset_from_method (mono_domain_get);
//		thread_tls_offset = read_tls_offset_from_method (mono_thread_current);
	}		

#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK

	/*----------------------------------------------------------*/
	/* Determine stack boundaries 				    */
	/*----------------------------------------------------------*/
	if (!mono_running_on_valgrind ()) {
#ifdef HAVE_PTHREAD_GETATTR_NP
		pthread_getattr_np( self, &attr );
#elif HAVE_PTHREAD_ATTR_GET_NP
		pthread_attr_get_np( self, &attr );
#endif
		pthread_attr_getstack( &attr, &stAddr, &stSize );
	}


	/*----------------------------------------------------------*/
	/* Setup an alternate signal stack 			    */
	/*----------------------------------------------------------*/
	tls->stack_size	       = stSize;
	tls->signal_stack      = g_malloc (SIGNAL_STACK_SIZE);
	tls->signal_stack_size = SIGNAL_STACK_SIZE;

	sa.ss_sp    = tls->signal_stack;
	sa.ss_size  = SIGNAL_STACK_SIZE;
	sa.ss_flags = SS_ONSTACK;
	sigaltstack (&sa, NULL);
#endif

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
#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK
	struct sigaltstack sa;

	sa.ss_sp    = tls->signal_stack;
	sa.ss_size  = SIGNAL_STACK_SIZE;
	sa.ss_flags = SS_DISABLE;
	sigaltstack (&sa, NULL);

	if (tls->signal_stack)
		g_free (tls->signal_stack);
#endif

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
		this->type = this_type;
		this->sreg1 = this_reg;
		this->dreg = this_dreg;
		mono_bblock_add_inst (cfg->cbb, this);
	}

	if (vt_reg != -1) {
		MonoInst *vtarg;
		MONO_INST_NEW (cfg, vtarg, OP_SETREG);
		vtarg->type = STACK_MP;
		vtarg->sreg1 = vt_reg;
		vtarg->dreg = s390_r2;
		mono_bblock_add_inst (cfg->cbb, vtarg);
	}
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_get_opcode_for_method                   */
/*                                                                  */
/* Function	- Check for opcodes we can handle directly in       */
/*		  hardware.                    			    */
/*		                               			    */
/*------------------------------------------------------------------*/

gint
mono_arch_get_opcode_for_method (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	if (cmethod->klass == mono_defaults.math_class) {
		if (strcmp (cmethod->name, "Sqrt") == 0)
			return OP_SQRT;
	}
	return -1;
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
		case OP_S390_ARGPTR:
			printf ("[0x%x(%s)]", tree->inst_offset, 
				mono_arch_regname (tree->inst_basereg));
			done = 1;
			break;
		case OP_S390_STKARG:
			printf ("[0x%x(previous_frame)]", 
				tree->inst_offset); 
			done = 1;
			break;
		case OP_S390_MOVE:
			printf ("[0x%x(%d,%s),0x%x(%s)]",
				tree->inst_offset, tree->unused,
				tree->dreg, tree->inst_imm, 
				tree->sreg1);
			done = 1;
			break;
		case OP_S390_SETF4RET:
			printf ("[f%d,f%d]", 
				mono_arch_regname (tree->dreg),
				mono_arch_regname (tree->sreg1));
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

MonoInst* mono_arch_get_domain_intrinsic (MonoCompile* cfg)
{
	return NULL;
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

MonoInst* mono_arch_get_thread_intrinsic (MonoCompile* cfg)
{
	return NULL;
}

/*========================= End of Function ========================*/
