/*
 * mini-ppc.c: PowerPC backend for the Mono code generator
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2003 Ximian, Inc.
 */
#include "mini.h"
#include <string.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/debug-helpers.h>

#include "mini-ppc.h"
#include "inssel.h"
#include "cpu-g4.h"

int mono_exc_esp_offset = 0;

const char*
mono_arch_regname (int reg) {
	static const char * rnames[] = {
		"ppc_r0", "ppc_sp", "ppc_r2", "ppc_r3", "ppc_r4",
		"ppc_r5", "ppc_r6", "ppc_r7", "ppc_r8", "ppc_r9",
		"ppc_r10", "ppc_r11", "ppc_r12", "ppc_r13", "ppc_r14",
		"ppc_r15", "ppc_r16", "ppc_r17", "ppc_r18", "ppc_r19",
		"ppc_r20", "ppc_r21", "ppc_r22", "ppc_r23", "ppc_r24",
		"ppc_r25", "ppc_r26", "ppc_r27", "ppc_r28", "ppc_r29",
		"ppc_r30", "ppc_r31"
	};
	if (reg >= 0 && reg < 32)
		return rnames [reg];
	return "unknown";
}

typedef struct {
	guint16 size;
	guint16 offset;
	guint8  pad;
} MonoJitArgumentInfo;

/*
 * arch_get_argument_info:
 * @csig:  a method signature
 * @param_count: the number of parameters to consider
 * @arg_info: an array to store the result infos
 *
 * Gathers information on parameters such as size, alignment and
 * padding. arg_info should be large enought to hold param_count + 1 entries. 
 *
 * Returns the size of the activation frame.
 */
static int
arch_get_argument_info (MonoMethodSignature *csig, int param_count, MonoJitArgumentInfo *arg_info)
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

		/* ignore alignment for now */
		align = 1;

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

static int indent_level = 0;

static void indent (int diff) {
	int v = indent_level;
	while (v-- > 0) {
		printf (". ");
	}
	indent_level += diff;
}

static void
enter_method (MonoMethod *method, char *ebp)
{
	int i, j;
	MonoClass *class;
	MonoObject *o;
	MonoJitArgumentInfo *arg_info;
	MonoMethodSignature *sig;
	char *fname;

	fname = mono_method_full_name (method, TRUE);
	indent (1);
	printf ("ENTER: %s(", fname);
	g_free (fname);
	
	if (((int)ebp & (MONO_ARCH_FRAME_ALIGNMENT - 1)) != 0) {
		g_error ("unaligned stack detected (%p)", ebp);
	}

	sig = method->signature;

	arg_info = alloca (sizeof (MonoJitArgumentInfo) * (sig->param_count + 1));

	arch_get_argument_info (sig, sig->param_count, arg_info);

	if (MONO_TYPE_ISSTRUCT (method->signature->ret)) {
		g_assert (!method->signature->ret->byref);

		printf ("VALUERET:%p, ", *((gpointer *)(ebp + 8)));
	}

	if (method->signature->hasthis) {
		gpointer *this = (gpointer *)(ebp + arg_info [0].offset);
		if (method->klass->valuetype) {
			printf ("value:%p, ", *this);
		} else {
			o = *((MonoObject **)this);

			if (o) {
				class = o->vtable->klass;

				if (class == mono_defaults.string_class) {
					printf ("this:[STRING:%p:%s], ", o, mono_string_to_utf8 ((MonoString *)o));
				} else {
					printf ("this:%p[%s.%s], ", o, class->name_space, class->name);
				}
			} else 
				printf ("this:NULL, ");
		}
	}

	for (i = 0; i < method->signature->param_count; ++i) {
		gpointer *cpos = (gpointer *)(ebp + arg_info [i + 1].offset);
		int size = arg_info [i + 1].size;

		MonoType *type = method->signature->params [i];
		
		if (type->byref) {
			printf ("[BYREF:%p], ", *cpos); 
		} else switch (type->type) {
			
		case MONO_TYPE_I:
		case MONO_TYPE_U:
			printf ("%p, ", (gpointer)*((int *)(cpos)));
			break;
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
			printf ("%d, ", *((int *)(cpos)));
			break;
		case MONO_TYPE_STRING: {
			MonoString *s = *((MonoString **)cpos);
			if (s) {
				g_assert (((MonoObject *)s)->vtable->klass == mono_defaults.string_class);
				printf ("[STRING:%p:%s], ", s, mono_string_to_utf8 (s));
			} else 
				printf ("[STRING:null], ");
			break;
		}
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT: {
			o = *((MonoObject **)cpos);
			if (o) {
				class = o->vtable->klass;
		    
				if (class == mono_defaults.string_class) {
					printf ("[STRING:%p:%s], ", o, mono_string_to_utf8 ((MonoString *)o));
				} else if (class == mono_defaults.int32_class) {
					printf ("[INT32:%p:%d], ", o, *(gint32 *)((char *)o + sizeof (MonoObject)));
				} else
					printf ("[%s.%s:%p], ", class->name_space, class->name, o);
			} else {
				printf ("%p, ", *((gpointer *)(cpos)));				
			}
			break;
		}
		case MONO_TYPE_PTR:
		case MONO_TYPE_FNPTR:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
			printf ("%p, ", *((gpointer *)(cpos)));
			break;
		case MONO_TYPE_I8:
			printf ("%lld, ", *((gint64 *)(cpos)));
			break;
		case MONO_TYPE_R4:
			printf ("%f, ", *((float *)(cpos)));
			break;
		case MONO_TYPE_R8:
			printf ("%f, ", *((double *)(cpos)));
			break;
		case MONO_TYPE_VALUETYPE: 
			printf ("[");
			for (j = 0; j < size; j++)
				printf ("%02x,", *((guint8*)cpos +j));
			printf ("], ");
			break;
		default:
			printf ("XX, ");
		}
	}

	printf (")\n");
}

static void
leave_method (MonoMethod *method, ...)
{
	MonoType *type;
	char *fname;
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
		int eax = va_arg (ap, int);
		if (eax)
			printf ("TRUE:%d", eax);
		else 
			printf ("FALSE");
			
		break;
	}
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I:
	case MONO_TYPE_U: {
		int eax = va_arg (ap, int);
		printf ("EAX=%d", eax);
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
		printf ("result=%p", p);
		break;
	}
	case MONO_TYPE_I8: {
		gint64 l =  va_arg (ap, gint64);
		printf ("lresult=%lld", l);
		break;
	}
	case MONO_TYPE_R8: {
		double f = va_arg (ap, double);
		printf ("FP=%f\n", f);
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
		printf ("(unknown return type %x)", method->signature->ret->type);
	}

	printf ("\n");
}

/*
 * Initialize the cpu to execute managed code.
 */
void
mono_arch_cpu_init (void)
{
}

/*
 * This function returns the optimizations supported on this cpu.
 */
guint32
mono_arch_cpu_optimizazions (guint32 *exclude_mask)
{
	guint32 opts = 0;

	/* no ppc-specific optimizations yet */
	*exclude_mask = 0;
	return opts;
}

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

GList *
mono_arch_get_global_int_regs (MonoCompile *cfg)
{
	GList *regs = NULL;
	int i, top = 32;
	if (cfg->flags & MONO_CFG_HAS_ALLOCA)
		top = 31;
	for (i = 13; i < top; ++i)
		regs = g_list_prepend (regs, GUINT_TO_POINTER (i));

	return regs;
}

// code from ppc/tramp.c, try to keep in sync
#define MIN_CACHE_LINE 8

void
mono_arch_flush_icache (guint8 *code, gint size)
{
	guint i;
	guint8 *p;

	p = code;
	for (i = 0; i < size; i += MIN_CACHE_LINE, p += MIN_CACHE_LINE) {
		asm ("dcbst 0,%0;" : : "r"(p) : "memory");
	}
	asm ("sync");
	p = code;
	for (i = 0; i < size; i += MIN_CACHE_LINE, p += MIN_CACHE_LINE) {
		asm ("icbi 0,%0; sync;" : : "r"(p) : "memory");
	}
	asm ("sync");
	asm ("isync");
}

#define NOT_IMPLEMENTED(x) \
                g_error ("FIXME: %s is not yet implemented. (trampoline)", x);

#define PROLOG_INS 8
#define CALL_INS   2
#define EPILOG_INS 6
#define FLOAT_REGS 8
#define GENERAL_REGS 8
#ifdef __APPLE__
#define MINIMAL_STACK_SIZE 10
#define ALWAYS_ON_STACK(s) s
#define FP_ALSO_IN_REG(s) s
#define RET_ADDR_OFFSET 8
#define STACK_PARAM_OFFSET 24
#else
#define MINIMAL_STACK_SIZE 5
#define ALWAYS_ON_STACK(s)
#define FP_ALSO_IN_REG(s) s
#define ALIGN_DOUBLES
#define RET_ADDR_OFFSET 4
#define STACK_PARAM_OFFSET 8
#endif

typedef struct {
	gint16 offset;
	gint8  reg;
	gint8  regtype; /* 0 general, 1 basereg, 2 floating point register */
} ArgInfo;

typedef struct {
	int nargs;
	guint32 stack_usage;
	ArgInfo ret;
	ArgInfo args [1];
} CallInfo;

#define DEBUG(a)

static void inline
add_general (guint *gr, guint *stack_size, ArgInfo *ainfo, gboolean simple)
{
	if (simple) {
		if (*gr >= 3 + GENERAL_REGS) {
			ainfo->offset = *stack_size;
			ainfo->reg = ppc_sp; /* in the caller */
			ainfo->regtype = 1;
			*stack_size += 4;
		} else {
			ALWAYS_ON_STACK (*stack_size += 4);
			ainfo->reg = *gr;
		}
	} else {
		if (*gr >= 3 + GENERAL_REGS - 1) {
			ainfo->offset = *stack_size;
			ainfo->reg = ppc_sp; /* in the caller */
			ainfo->regtype = 1;
			*stack_size += 8;
#ifdef ALIGN_DOUBLES
			*stack_size += (*stack_size % 8);
#endif
		} else {
			ALWAYS_ON_STACK (*stack_size += 8);
			ainfo->reg = *gr;
		}
#ifdef ALIGN_DOUBLES
		if ((*gr) & 1)
			(*gr) ++;
#endif
		(*gr) ++;
	}
	(*gr) ++;
}

static CallInfo*
calculate_sizes (MonoMethodSignature *sig, gboolean is_pinvoke)
{
	guint i, fr, gr;
	int n = sig->hasthis + sig->param_count;
	guint32 simpletype;
	guint32 stack_size = 0;
	CallInfo *cinfo = g_malloc0 (sizeof (CallInfo) + sizeof (ArgInfo) * n);

	fr = 1;
	gr = 3;

	/* FIXME: handle returning a struct */

	n = 0;
	if (sig->hasthis) {
		add_general (&gr, &stack_size, cinfo->args + n, TRUE);
		n++;
	}
        DEBUG(printf("params: %d\n", sig->param_count));
	for (i = 0; i < sig->param_count; ++i) {
                DEBUG(printf("param %d: ", i));
		if (sig->params [i]->byref) {
                        DEBUG(printf("byref\n"));
			add_general (&gr, &stack_size, cinfo->args + n, TRUE);
			n++;
			continue;
		}
		simpletype = sig->params [i]->type;
	enum_calc_size:
		switch (simpletype) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_PTR:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_STRING:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_ARRAY:
			add_general (&gr, &stack_size, cinfo->args + n, TRUE);
			n++;
			break;
		case MONO_TYPE_VALUETYPE: {
			gint size;
			if (sig->params [i]->data.klass->enumtype) {
				simpletype = sig->params [i]->data.klass->enum_basetype->type;
				goto enum_calc_size;
			}
#if 0
			size = mono_class_value_size (sig->params [i]->data.klass, NULL);
			if (size != 4) {
				DEBUG(printf ("copy %d bytes struct on stack\n",
					      mono_class_value_size (sig->params [i]->data.klass, NULL)));
				*stack_size += (size + 3) & (~3);
				if (gr > 3 + GENERAL_REGS) {
					*stack_size += 4;
				}
			} else {
				DEBUG(printf ("load %d bytes struct\n",
					      mono_class_value_size (sig->params [i]->data.klass, NULL)));
				add_general (&gr, stack_size, code_size, TRUE);
			}
#endif
			g_assert_not_reached ();
			break;
		}
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
			add_general (&gr, &stack_size, cinfo->args + n, FALSE);
			n++;
			break;
		case MONO_TYPE_R4:
			if (fr < 7) {
				fr ++;
				FP_ALSO_IN_REG (gr ++);
				ALWAYS_ON_STACK (stack_size += 4);
			} else {
				NOT_IMPLEMENTED ("R4 arg");
			}
			n++;
			break;
		case MONO_TYPE_R8:
			if (fr < 7) {
				fr ++;
				FP_ALSO_IN_REG (gr += 2);
				ALWAYS_ON_STACK (stack_size += 8);
			} else {
				NOT_IMPLEMENTED ("R8 arg");
			}
			n++;
			break;
		default:
			g_error ("Can't trampoline 0x%x", sig->params [i]->type);
		}
	}

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
		case MONO_TYPE_STRING:
			cinfo->ret.reg = ppc_r3;
			break;
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
			cinfo->ret.reg = ppc_r3;
			break;
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
			cinfo->ret.reg = ppc_f1;
			cinfo->ret.regtype = 2;
			break;
		case MONO_TYPE_VALUETYPE:
			if (sig->ret->data.klass->enumtype) {
				simpletype = sig->ret->data.klass->enum_basetype->type;
				goto enum_retvalue;
			}
			break;
		case MONO_TYPE_VOID:
			break;
		default:
			g_error ("Can't handle as return value 0x%x", sig->ret->type);
		}
	}

	/* align stack size to 16 */
	DEBUG (printf ("      stack size: %d (%d)\n", (stack_size + 15) & ~15, stack_size));
	stack_size = (stack_size + 15) & ~15;

	cinfo->stack_usage = stack_size;
	return cinfo;
}


/*
 * Set var information according to the calling convention. ppc version.
 * The locals var stuff should most likely be split in another method.
 */
void
mono_arch_allocate_vars (MonoCompile *m)
{
	MonoMethodSignature *sig;
	MonoMethodHeader *header;
	MonoInst *inst;
	int i, offset, size, align, curinst;
	int frame_reg = ppc_sp;
 
	if (m->flags & MONO_CFG_HAS_ALLOCA)
		frame_reg = ppc_r31;
	m->frame_reg = frame_reg;

	header = ((MonoMethodNormal *)m->method)->header;

	sig = m->method->signature;
	
	offset = 0;
	curinst = 0;
	if (MONO_TYPE_ISSTRUCT (sig->ret)) {
		m->ret->opcode = OP_REGVAR;
		m->ret->inst_c0 = ppc_r3;
	} else {
		/* FIXME: handle long and FP values */
		switch (sig->ret->type) {
		case MONO_TYPE_VOID:
			break;
		default:
			m->ret->opcode = OP_REGVAR;
			m->ret->inst_c0 = ppc_r3;
			break;
		}
	}
	/* local vars are at a positive offset from the stack pointer */
	/* 
	 * also note that if the function uses alloca, we use ppc_r31
	 * to point at the local variables.
	 */
	offset = 24; /* linkage area */
	/* align the offset to 16 bytes: not sure this is needed here  */
	//offset += 16 - 1;
	//offset &= ~(16 - 1);

	/* add parameter area size for called functions */
	offset += m->param_area;
	offset += 16 - 1;
	offset &= ~(16 - 1);

	/* FIXME: check how to handle this stuff... reserve space to save LMF and caller saved registers */
	offset += sizeof (MonoLMF);

#if 0
	/* this stuff should not be needed on ppc and the new jit,
	 * because a call on ppc to the handlers doesn't change the 
	 * stack pointer and the jist doesn't manipulate the stack pointer
	 * for operations involving valuetypes.
	 */
	/* reserve space to store the esp */
	offset += sizeof (gpointer);

	/* this is a global constant */
	mono_exc_esp_offset = offset;
#endif

	curinst = m->locals_start;
	for (i = curinst; i < m->num_varinfo; ++i) {
		inst = m->varinfo [i];
		if (inst->opcode == OP_REGVAR)
			continue;

		/* inst->unused indicates native sized value types, this is used by the
		* pinvoke wrappers when they call functions returning structure */
		if (inst->unused && MONO_TYPE_ISSTRUCT (inst->inst_vtype))
			size = mono_class_native_size (inst->inst_vtype->data.klass, &align);
		else
			size = mono_type_size (inst->inst_vtype, &align);

		offset += align - 1;
		offset &= ~(align - 1);
		inst->inst_offset = offset;
		inst->opcode = OP_REGOFFSET;
		inst->inst_basereg = frame_reg;
		offset += size;
		//g_print ("allocating local %d to %d\n", i, inst->inst_offset);
	}

	curinst = 0;
	if (sig->hasthis) {
		inst = m->varinfo [curinst];
		if (inst->opcode != OP_REGVAR) {
			inst->opcode = OP_REGOFFSET;
			inst->inst_basereg = frame_reg;
			offset += sizeof (gpointer) - 1;
			offset &= ~(sizeof (gpointer) - 1);
			inst->inst_offset = offset;
			offset += sizeof (gpointer);
		}
		curinst++;
	}

	for (i = 0; i < sig->param_count; ++i) {
		inst = m->varinfo [curinst];
		if (inst->opcode != OP_REGVAR) {
			inst->opcode = OP_REGOFFSET;
			inst->inst_basereg = frame_reg;
			size = mono_type_size (sig->params [i], &align);
			offset += align - 1;
			offset &= ~(align - 1);
			inst->inst_offset = offset;
			offset += size;
		}
		curinst++;
	}

	/* align the offset to 16 bytes */
	offset += 16 - 1;
	offset &= ~(16 - 1);

	/* change sign? */
	m->stack_offset = offset;

}

/* Fixme: we need an alignment solution for enter_method and mono_arch_call_opcode,
 * currently alignment in mono_arch_call_opcode is computed without arch_get_argument_info 
 */

/* 
 * take the arguments and generate the arch-specific
 * instructions to properly call the function in call.
 * This includes pushing, moving arguments to the right register
 * etc.
 * Issue: who does the spilling if needed, and when?
 */
MonoCallInst*
mono_arch_call_opcode (MonoCompile *cfg, MonoBasicBlock* bb, MonoCallInst *call, int is_virtual) {
	MonoInst *arg, *in;
	MonoMethodSignature *sig;
	int i, n, type;
	MonoType *ptype;
	CallInfo *cinfo;
	ArgInfo *ainfo;

	sig = call->signature;
	n = sig->param_count + sig->hasthis;
	
	cinfo = calculate_sizes (sig, sig->pinvoke);

	for (i = 0; i < n; ++i) {
		ainfo = cinfo->args + i;
		if (is_virtual && i == 0) {
			/* the argument will be attached to the call instrucion */
			in = call->args [i];
		} else {
			MONO_INST_NEW (cfg, arg, OP_OUTARG);
			in = call->args [i];
			arg->cil_code = in->cil_code;
			arg->inst_left = in;
			arg->type = in->type;
			/* prepend, we'll need to reverse them later */
			arg->next = call->out_args;
			call->out_args = arg;
			if (ainfo->regtype == 0) {
				arg->unused = ainfo->reg;
				call->used_iregs |= 1 << ainfo->reg;
			} else if (ainfo->regtype == 1) {
				g_assert_not_reached ();
			} else if (ainfo->regtype == 2) {
				arg->opcode = OP_OUTARG_R8;
				arg->unused = ainfo->reg;
				call->used_fregs |= 1 << ainfo->reg;
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
	}
	call->stack_usage = cinfo->stack_usage;
	cfg->param_area = MAX (cfg->param_area, cinfo->stack_usage);
	cfg->flags |= MONO_CFG_HAS_CALLS;
	/* 
	 * should set more info in call, such as the stack space
	 * used by the args that needs to be added back to esp
	 */

	g_free (cinfo);
	return call;
}

/*
 * Allow tracing to work with this interface (with an optional argument)
 */

/*
 * This may be needed on some archs or for debugging support.
 */
void
mono_arch_instrument_mem_needs (MonoMethod *method, int *stack, int *code)
{
	/* no stack room needed now (may be needed for FASTCALL-trace support) */
	*stack = 0;
	/* split prolog-epilog requirements? */
	*code = 50; /* max bytes needed: check this number */
}

void*
mono_arch_instrument_prolog (MonoCompile *cfg, void *func, void *p, gboolean enable_arguments)
{
	guchar *code = p;
#if 0
	/* if some args are passed in registers, we need to save them here */
	x86_push_reg (code, X86_EBP);
	x86_push_imm (code, cfg->method);
	mono_add_patch_info (cfg, code-cfg->native_code, MONO_PATCH_INFO_ABS, func);
	x86_call_code (code, 0);
	x86_alu_reg_imm (code, X86_ADD, X86_ESP, 8);
#endif
	return code;
}

enum {
	SAVE_NONE,
	SAVE_STRUCT,
	SAVE_ONE,
	SAVE_TWO,
	SAVE_FP
};

void*
mono_arch_instrument_epilog (MonoCompile *cfg, void *func, void *p, gboolean enable_arguments)
{
	guchar *code = p;
	int arg_size = 0, save_mode = SAVE_NONE;
	MonoMethod *method = cfg->method;
	int rtype = method->signature->ret->type;
	
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
		//x86_push_reg (code, X86_EDX);
		//x86_push_reg (code, X86_EAX);
		if (enable_arguments) {
			//x86_push_reg (code, X86_EDX);
			//x86_push_reg (code, X86_EAX);
			arg_size = 8;
		}
		break;
	case SAVE_ONE:
		//x86_push_reg (code, X86_EAX);
		if (enable_arguments) {
			//x86_push_reg (code, X86_EAX);
			arg_size = 4;
		}
		break;
	case SAVE_FP:
		//x86_alu_reg_imm (code, X86_SUB, X86_ESP, 8);
		///x86_fst_membase (code, X86_ESP, 0, TRUE, TRUE);
		if (enable_arguments) {
			//x86_alu_reg_imm (code, X86_SUB, X86_ESP, 8);
			//x86_fst_membase (code, X86_ESP, 0, TRUE, TRUE);
			arg_size = 8;
		}
		break;
	case SAVE_STRUCT:
		if (enable_arguments) {
			//x86_push_membase (code, X86_EBP, 8);
			arg_size = 4;
		}
		break;
	case SAVE_NONE:
	default:
		break;
	}

	/*x86_push_imm (code, method);
	mono_add_patch_info (cfg, code-cfg->native_code, MONO_PATCH_INFO_ABS, func);
	x86_call_code (code, 0);
	x86_alu_reg_imm (code, X86_ADD, X86_ESP, arg_size + 4);
	*/

	switch (save_mode) {
	case SAVE_TWO:
		//x86_pop_reg (code, X86_EAX);
		//x86_pop_reg (code, X86_EDX);
		break;
	case SAVE_ONE:
		//x86_pop_reg (code, X86_EAX);
		break;
	case SAVE_FP:
		//x86_fld_membase (code, X86_ESP, 0, TRUE);
		//x86_alu_reg_imm (code, X86_ADD, X86_ESP, 8);
		break;
	case SAVE_NONE:
	default:
		break;
	}

	return code;
}

#define EMIT_COND_BRANCH(ins,cond) \
if (ins->flags & MONO_INST_BRLABEL) { \
        if (ins->inst_i0->inst_c0) { \
		ppc_bc (code, branch_b0_table [cond], branch_b1_table [cond], (code - cfg->native_code + ins->inst_i0->inst_c0) & 0xffff);	\
        } else { \
	        mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_LABEL, ins->inst_i0); \
		ppc_bc (code, branch_b0_table [cond], branch_b1_table [cond], 0);	\
        } \
} else { \
        if (0 && ins->inst_true_bb->native_offset) { \
		ppc_bc (code, branch_b0_table [cond], branch_b1_table [cond], (code - cfg->native_code + ins->inst_true_bb->native_offset) & 0xffff); \
        } else { \
	        mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_BB, ins->inst_true_bb); \
		ppc_bc (code, branch_b0_table [cond], branch_b1_table [cond], 0);	\
        } \
}

/* emit an exception if condition is fail */
#define EMIT_COND_SYSTEM_EXCEPTION(cond,signed,exc_name)            \
        do {                                                        \
		mono_add_patch_info (cfg, code - cfg->native_code,   \
				    MONO_PATCH_INFO_EXC, exc_name);  \
	        x86_branch32 (code, cond, 0, signed);               \
	} while (0); 

#define EMIT_FPCOMPARE(code) do { \
	x86_fcompp (code); \
	x86_fnstsw (code); \
	x86_alu_reg_imm (code, X86_AND, X86_EAX, 0x4500); \
} while (0); 

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
					//static int c = 0; printf ("MATCHX %s %d\n", cfg->method->name,c++);
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

/* 
 * the branch_b0_table should maintain the order of these
 * opcodes.
case CEE_BEQ:
case CEE_BGE:
case CEE_BGT:
case CEE_BLE:
case CEE_BLT:
case CEE_BNE_UN:
case CEE_BGE_UN:
case CEE_BGT_UN:
case CEE_BLE_UN:
case CEE_BLT_UN:
 */
static const guchar 
branch_b0_table [] = {
	PPC_BR_TRUE, 
	PPC_BR_FALSE, 
	PPC_BR_TRUE, 
	PPC_BR_FALSE, 
	PPC_BR_TRUE, 
	
	PPC_BR_FALSE, 
	PPC_BR_FALSE, 
	PPC_BR_TRUE, 
	PPC_BR_FALSE,
	PPC_BR_TRUE
};

static const guchar 
branch_b1_table [] = {
	PPC_BR_EQ, 
	PPC_BR_LT, 
	PPC_BR_GT, 
	PPC_BR_GT,
	PPC_BR_LT, 
	
	PPC_BR_EQ, 
	PPC_BR_LT, 
	PPC_BR_GT, 
	PPC_BR_GT,
	PPC_BR_LT 
};

#undef DEBUG
#define DEBUG(a) if (cfg->verbose_level > 1) a
//#define DEBUG(a)
#define reg_is_freeable(r) ((r) >= 3 && (r) <= 10)

typedef struct {
	int born_in;
	int killed_in;
	int last_use;
	int prev_use;
} RegTrack;

static const char*const * ins_spec = ppcg4;

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

typedef struct InstList InstList;

struct InstList {
	InstList *prev;
	InstList *next;
	MonoInst *data;
};

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

/*
 * Force the spilling of the variable in the symbolic register 'reg'.
 */
static int
get_register_force_spilling (MonoCompile *cfg, InstList *item, MonoInst *ins, int reg)
{
	MonoInst *load;
	int i, sel, spill;
	
	sel = cfg->rs->iassign [reg];
	/*i = cfg->rs->isymbolic [sel];
	g_assert (i == reg);*/
	i = reg;
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

static int
get_register_spilling (MonoCompile *cfg, InstList *item, MonoInst *ins, guint32 regmask, int reg)
{
	MonoInst *load;
	int i, sel, spill;

	DEBUG (g_print ("start regmask to assign R%d: 0x%08x (R%d <- R%d R%d)\n", reg, regmask, ins->dreg, ins->sreg1, ins->sreg2));
	/* exclude the registers in the current instruction */
	if (reg != ins->sreg1 && (reg_is_freeable (ins->sreg1) || (ins->sreg1 >= MONO_MAX_IREGS && cfg->rs->iassign [ins->sreg1] >= 0))) {
		if (ins->sreg1 >= MONO_MAX_IREGS)
			regmask &= ~ (1 << cfg->rs->iassign [ins->sreg1]);
		else
			regmask &= ~ (1 << ins->sreg1);
		DEBUG (g_print ("excluding sreg1 %s\n", mono_arch_regname (ins->sreg1)));
	}
	if (reg != ins->sreg2 && (reg_is_freeable (ins->sreg2) || (ins->sreg2 >= MONO_MAX_IREGS && cfg->rs->iassign [ins->sreg2] >= 0))) {
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
	DEBUG (g_print ("\tforced copy from %s to %s\n", mono_arch_regname (src), mono_arch_regname (dest)));
	return copy;
}

static MonoInst*
create_spilled_store (MonoCompile *cfg, int spill, int reg, int prev_reg, MonoInst *ins)
{
	MonoInst *store;
	MONO_INST_NEW (cfg, store, OP_STORE_MEMBASE_REG);
	store->sreg1 = reg;
	store->inst_destbasereg = ppc_r1;
	store->inst_offset = mono_spillvar_offset (cfg, spill);
	if (ins) {
		store->next = ins->next;
		ins->next = store;
	}
	DEBUG (g_print ("SPILLED STORE (%d at 0x%08x(%%sp)) R%d (from %s)\n", spill, store->inst_offset, prev_reg, mono_arch_regname (reg)));
	return store;
}

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

static int
alloc_int_reg (MonoCompile *cfg, InstList *curinst, MonoInst *ins, int sym_reg, guint32 allow_mask)
{
	int val = cfg->rs->iassign [sym_reg];
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
	cfg->rs->isymbolic [val] = sym_reg;
	return val;
}

/* use ppc_r3-ppc_310 as temp registers */
#define PPC_CALLER_REGS (0xf<<3)

/*
 * Local register allocation.
 * We first scan the list of instructions and we save the liveness info of
 * each register (when the register is first used, when it's value is set etc.).
 * We also reverse the list of instructions (in the InstList list) because assigning
 * registers backwards allows for more tricks to be used.
 */
void
mono_arch_local_regalloc (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins;
	MonoRegState *rs = cfg->rs;
	int i, val, fpcount;
	RegTrack *reginfo, *reginfof;
	RegTrack *reginfo1, *reginfo2, *reginfod;
	InstList *tmp, *reversed = NULL;
	const char *spec;
	guint32 src1_mask, src2_mask, dest_mask;

	if (!bb->code)
		return;
	rs->next_vireg = bb->max_ireg;
	rs->next_vfreg = bb->max_freg;
	mono_regstate_assign (rs);
	reginfo = mono_mempool_alloc0 (cfg->mempool, sizeof (RegTrack) * rs->next_vireg);
	reginfof = mono_mempool_alloc0 (cfg->mempool, sizeof (RegTrack) * rs->next_vfreg);
	rs->ifree_mask = PPC_CALLER_REGS;

	ins = bb->code;
	i = 1;
	fpcount = 0; /* FIXME: track fp stack utilization */
	DEBUG (g_print ("LOCAL regalloc: basic block: %d\n", bb->block_num));
	/* forward pass on the instructions to collect register liveness info */
	while (ins) {
		spec = ins_spec [ins->opcode];
		DEBUG (print_ins (i, ins));
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
				/* result in eax:edx, the virtual register is allocated sequentially */
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

	DEBUG (print_regtrack (reginfo, rs->next_vireg));
	DEBUG (print_regtrack (reginfof, rs->next_vfreg));
	tmp = reversed;
	while (tmp) {
		int prev_dreg, prev_sreg1, prev_sreg2;
		dest_mask = src1_mask = src2_mask = PPC_CALLER_REGS;
		--i;
		ins = tmp->data;
		spec = ins_spec [ins->opcode];
		DEBUG (g_print ("processing:"));
		DEBUG (print_ins (i, ins));
		/* update for use with FP regs... */
		if (spec [MONO_INST_DEST] != 'f' && ins->dreg >= MONO_MAX_IREGS) {
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
			DEBUG (g_print ("\tassigned dreg %s to dest R%d\n", mono_arch_regname (val), ins->dreg));
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
				if (ins->dreg == ppc_r4) {
					if (val != ppc_r3)
						create_copy_ins (cfg, val, ppc_r3, ins);
				} else if (ins->dreg == ppc_r3) {
					if (val == ppc_r4) {
						/* swap */
						create_copy_ins (cfg, ppc_r0, ppc_r3, ins);
						create_copy_ins (cfg, ppc_r3, ppc_r4, ins);
						create_copy_ins (cfg, ppc_r4, ppc_r0, ins);
					} else {
						/* two forced copies */
						create_copy_ins (cfg, val, ppc_r3, ins);
						create_copy_ins (cfg, ins->dreg, ppc_r4, ins);
					}
				} else {
					if (val == ppc_r3) {
						create_copy_ins (cfg, ins->dreg, ppc_r4, ins);
					} else {
						/* two forced copies */
						create_copy_ins (cfg, val, ppc_r3, ins);
						create_copy_ins (cfg, ins->dreg, ppc_r4, ins);
					}
				}
				if (reg_is_freeable (val) && hreg >= 0 && reginfo [hreg].born_in >= i) {
					DEBUG (g_print ("\tfreeable %s (R%d)\n", mono_arch_regname (val), hreg));
					mono_regstate_free_int (rs, val);
				}
			}
		} else {
			prev_dreg = -1;
		}
		if (spec [MONO_INST_DEST] != 'f' && reg_is_freeable (ins->dreg) && prev_dreg >= 0 && reginfo [prev_dreg].born_in >= i) {
			DEBUG (g_print ("\tfreeable %s (R%d) (born in %d)\n", mono_arch_regname (ins->dreg), prev_dreg, reginfo [prev_dreg].born_in));
			mono_regstate_free_int (rs, ins->dreg);
		}
		if (spec [MONO_INST_SRC1] != 'f' && ins->sreg1 >= MONO_MAX_IREGS) {
			val = rs->iassign [ins->sreg1];
			prev_sreg1 = ins->sreg1;
			if (val < 0) {
				int spill = 0;
				if (val < -1) {
					/* the register gets spilled after this inst */
					spill = -val -1;
				}
				if (0 && ins->opcode == OP_MOVE) {
					/* 
					 * small optimization: the dest register is already allocated
					 * but the src one is not: we can simply assign the same register
					 * here and peephole will get rid of the instruction later.
					 * This optimization may interfere with the clobbering handling:
					 * it removes a mov operation that will be added again to handle clobbering.
					 * There are also some other issues that should with make testjit.
					 */
					mono_regstate_alloc_int (rs, 1 << ins->dreg);
					val = rs->iassign [ins->sreg1] = ins->dreg;
					//g_assert (val >= 0);
					DEBUG (g_print ("\tfast assigned sreg1 %s to R%d\n", mono_arch_regname (val), ins->sreg1));
				} else {
					//g_assert (val == -1); /* source cannot be spilled */
					val = mono_regstate_alloc_int (rs, src1_mask);
					if (val < 0)
						val = get_register_spilling (cfg, tmp, ins, src1_mask, ins->sreg1);
					rs->iassign [ins->sreg1] = val;
					DEBUG (g_print ("\tassigned sreg1 %s to R%d\n", mono_arch_regname (val), ins->sreg1));
				}
				if (spill) {
					MonoInst *store = create_spilled_store (cfg, spill, val, prev_sreg1, NULL);
					insert_before_ins (ins, tmp, store);
				}
			}
			rs->isymbolic [val] = prev_sreg1;
			ins->sreg1 = val;
		} else {
			prev_sreg1 = -1;
		}
		if (spec [MONO_INST_SRC2] != 'f' && ins->sreg2 >= MONO_MAX_IREGS) {
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
			guint32 clob_mask = PPC_CALLER_REGS;
			for (j = 0; j < MONO_MAX_IREGS; ++j) {
				s = 1 << j;
				if ((clob_mask & s) && !(rs->ifree_mask & s) && j != ins->sreg1) {
					//g_warning ("register %s busy at call site\n", mono_arch_regname (j));
				}
			}
		}
		/*if (reg_is_freeable (ins->sreg1) && prev_sreg1 >= 0 && reginfo [prev_sreg1].born_in >= i) {
			DEBUG (g_print ("freeable %s\n", mono_arch_regname (ins->sreg1)));
			mono_regstate_free_int (rs, ins->sreg1);
		}
		if (reg_is_freeable (ins->sreg2) && prev_sreg2 >= 0 && reginfo [prev_sreg2].born_in >= i) {
			DEBUG (g_print ("freeable %s\n", mono_arch_regname (ins->sreg2)));
			mono_regstate_free_int (rs, ins->sreg2);
		}*/
		
		//DEBUG (print_ins (i, ins));
		tmp = tmp->next;
	}
}

static guchar*
emit_float_to_int (MonoCompile *cfg, guchar *code, int dreg, int size, gboolean is_signed)
{
	return code;
}

static unsigned char*
mono_emit_stack_alloc (guchar *code, MonoInst* tree)
{
#if 0
	int sreg = tree->sreg1;
	x86_alu_reg_reg (code, X86_SUB, X86_ESP, tree->sreg1);
	if (tree->flags & MONO_INST_INIT) {
		int offset = 0;
		if (tree->dreg != X86_EAX && sreg != X86_EAX) {
			x86_push_reg (code, X86_EAX);
			offset += 4;
		}
		if (tree->dreg != X86_ECX && sreg != X86_ECX) {
			x86_push_reg (code, X86_ECX);
			offset += 4;
		}
		if (tree->dreg != X86_EDI && sreg != X86_EDI) {
			x86_push_reg (code, X86_EDI);
			offset += 4;
		}
		
		x86_shift_reg_imm (code, X86_SHR, sreg, 2);
		if (sreg != X86_ECX)
			x86_mov_reg_reg (code, X86_ECX, sreg, 4);
		x86_alu_reg_reg (code, X86_XOR, X86_EAX, X86_EAX);
				
		x86_lea_membase (code, X86_EDI, X86_ESP, offset);
		x86_cld (code);
		x86_prefix (code, X86_REP_PREFIX);
		x86_stosl (code);
		
		if (tree->dreg != X86_EDI && sreg != X86_EDI)
			x86_pop_reg (code, X86_EDI);
		if (tree->dreg != X86_ECX && sreg != X86_ECX)
			x86_pop_reg (code, X86_ECX);
		if (tree->dreg != X86_EAX && sreg != X86_EAX)
			x86_pop_reg (code, X86_EAX);
	}
#endif
	return code;
}

void
ppc_patch (guchar *code, guchar *target)
{
	guint32 ins = *(guint32*)code;
	guint32 prim = ins >> 26;

//	g_print ("patching 0x%08x (0x%08x) to point to 0x%08x\n", code, ins, target);
	if (prim == 18) {
		// absolute address
		if (ins & 2) {
			guint32 li = (guint32)target;
			ins = prim << 26 | (ins & 3);
			ins |= li;
			// FIXME: assert the top bits of li are 0
		} else {
			gint diff = target - code;
			ins = prim << 26 | (ins & 3);
			diff &= ~3;
			diff &= ~(63 << 26);
			ins |= diff;
		}
		*(guint32*)code = ins;
	} else if (prim == 16) {
		// absolute address
		if (ins & 2) {
			guint32 li = (guint32)target;
			ins = (ins & 0xffff0000) | (ins & 3);
			li &= 0xffff;
			ins |= li;
			// FIXME: assert the top bits of li are 0
		} else {
			gint diff = target - code;
			ins = (ins & 0xffff0000) | (ins & 3);
			diff &= 0xffff;
			ins |= diff;
		}
		*(guint32*)code = ins;
	} else {
		g_assert_not_reached ();
	}
//	g_print ("patched with 0x%08x\n", ins);
}

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

	if (cfg->opt & MONO_OPT_PEEPHOLE)
		peephole_pass (cfg, bb);

#if 0
	/* 
	 * various stratgies to align BBs. Using real loop detection or simply
	 * aligning every block leads to more consistent benchmark results,
	 * but usually slows down the code
	 * we should do the alignment outside this function or we should adjust
	 * bb->native offset as well or the code is effectively slowed down!
	 */
	/* align all blocks */
//	if ((pad = (cfg->code_len & (align - 1)))) {
	/* poor man loop start detection */
//	if (bb->code && bb->in_count && bb->in_bb [0]->cil_code > bb->cil_code && (pad = (cfg->code_len & (align - 1)))) {
	/* consider real loop detection and nesting level */
//	if (bb->loop_blocks && bb->nesting < 3 && (pad = (cfg->code_len & (align - 1)))) {
	/* consider real loop detection */
	if (/*bb->loop_blocks &&*/ (pad = (cfg->code_len & (align - 1)))) {
		pad = align - pad;
		x86_padding (code, pad);
		cfg->code_len += pad;
		bb->native_offset = cfg->code_len;
	}
#endif

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
	//	if (ins->cil_code)
	//		g_print ("cil code\n");

		switch (ins->opcode) {
		case OP_STOREI1_MEMBASE_IMM:
			ppc_li (code, ppc_r11, ins->inst_imm);
			g_assert (ppc_is_imm16 (ins->inst_offset));
			ppc_stb (code, ppc_r11, ins->inst_offset, ins->inst_destbasereg);
			break;
		case OP_STOREI2_MEMBASE_IMM:
			ppc_li (code, ppc_r11, ins->inst_imm);
			g_assert (ppc_is_imm16 (ins->inst_offset));
			ppc_sth (code, ppc_r11, ins->inst_offset, ins->inst_destbasereg);
			break;
		case OP_STORE_MEMBASE_IMM:
		case OP_STOREI4_MEMBASE_IMM:
			ppc_load (code, ppc_r11, ins->inst_imm);
			g_assert (ppc_is_imm16 (ins->inst_offset));
			ppc_stw (code, ppc_r11, ins->inst_offset, ins->inst_destbasereg);
			break;
		case OP_STOREI1_MEMBASE_REG:
			g_assert (ppc_is_imm16 (ins->inst_offset));
			ppc_stb (code, ins->sreg1, ins->inst_offset, ins->inst_destbasereg);
			break;
		case OP_STOREI2_MEMBASE_REG:
			g_assert (ppc_is_imm16 (ins->inst_offset));
			ppc_sth (code, ins->sreg1, ins->inst_offset, ins->inst_destbasereg);
			break;
		case OP_STORE_MEMBASE_REG:
		case OP_STOREI4_MEMBASE_REG:
			g_assert (ppc_is_imm16 (ins->inst_offset));
			ppc_stw (code, ins->sreg1, ins->inst_offset, ins->inst_destbasereg);
			break;
		case CEE_LDIND_I:
		case CEE_LDIND_I4:
		case CEE_LDIND_U4:
			g_assert_not_reached ();
			//x86_mov_reg_mem (code, ins->dreg, ins->inst_p0, 4);
			break;
		case OP_LOADU4_MEM:
			g_assert_not_reached ();
			//x86_mov_reg_imm (code, ins->dreg, ins->inst_p0);
			//x86_mov_reg_membase (code, ins->dreg, ins->dreg, 0, 4);
			break;
		case OP_LOAD_MEMBASE:
		case OP_LOADI4_MEMBASE:
		case OP_LOADU4_MEMBASE:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_lwz (code, ins->dreg, ins->inst_offset, ins->inst_basereg);
			} else {
				ppc_load (code, ppc_r11, ins->inst_offset);
				ppc_lwzx (code, ins->dreg, ppc_r11, ins->inst_basereg);
			}
			break;
		case OP_LOADU1_MEMBASE:
			g_assert (ppc_is_imm16 (ins->inst_offset));
			ppc_lbz (code, ins->dreg, ins->inst_offset, ins->inst_basereg);
			break;
		case OP_LOADI1_MEMBASE:
			g_assert (ppc_is_imm16 (ins->inst_offset));
			// FIXME: sign extend
			ppc_lbz (code, ins->dreg, ins->inst_offset, ins->inst_basereg);
			break;
		case OP_LOADU2_MEMBASE:
			g_assert (ppc_is_imm16 (ins->inst_offset));
			ppc_lhz (code, ins->dreg, ins->inst_offset, ins->inst_basereg);
			break;
		case OP_LOADI2_MEMBASE:
			g_assert (ppc_is_imm16 (ins->inst_offset));
			ppc_lha (code, ins->dreg, ins->inst_basereg, ins->inst_offset);
			break;
		case CEE_CONV_I1:
			ppc_extsb (code, ins->dreg, ins->sreg1);
			break;
		case CEE_CONV_I2:
			ppc_extsh (code, ins->dreg, ins->sreg1);
			break;
		case CEE_CONV_U1:
			ppc_rlwinm (code, ins->dreg, ins->sreg1, 0, 24, 31);
			break;
		case CEE_CONV_U2:
			ppc_rlwinm (code, ins->dreg, ins->sreg1, 0, 16, 31);
			break;
		case OP_COMPARE:
			if (ins->next && ins->next->opcode >= CEE_BNE_UN && ins->next->opcode <= CEE_BLT_UN)
				ppc_cmpl (code, 0, 0, ins->sreg1, ins->sreg2);
			else
				ppc_cmp (code, 0, 0, ins->sreg1, ins->sreg2);
			break;
		case OP_COMPARE_IMM:
			if (ppc_is_imm16 (ins->inst_imm)) {
				if (ins->next && ins->next->opcode >= CEE_BNE_UN && ins->next->opcode <= CEE_BLT_UN)
					ppc_cmpli (code, 0, 0, ins->sreg1, (ins->inst_imm & 0xffff));
				else
					ppc_cmpi (code, 0, 0, ins->sreg1, (ins->inst_imm & 0xffff));
			} else {
				ppc_load (code, ppc_r11, ins->inst_imm);
				if (ins->next && ins->next->opcode >= CEE_BNE_UN && ins->next->opcode <= CEE_BLT_UN)
					ppc_cmp (code, 0, 0, ins->sreg1, ppc_r11);
				else
					ppc_cmpl (code, 0, 0, ins->sreg1, ppc_r11);
			}
			break;
		case OP_X86_TEST_NULL:
			ppc_cmpi (code, 0, 0, ins->sreg1, 0);
			break;
		case CEE_BREAK:
			ppc_break (code);
			break;
		case OP_ADDCC:
			ppc_addc (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case CEE_ADD:
			ppc_add (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_ADC:
			ppc_adde (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_ADD_IMM:
			if (ppc_is_imm16 (ins->inst_imm)) {
				ppc_addi (code, ins->dreg, ins->sreg1, ins->inst_imm);
			} else {
				ppc_load (code, ppc_r11, ins->inst_imm);
				ppc_add (code, ins->dreg, ins->sreg1, ppc_r11);
			}
			break;
		case OP_ADC_IMM:
			ppc_load (code, ppc_r11, ins->inst_imm);
			ppc_adde (code, ins->dreg, ins->sreg1, ppc_r11);
			break;
		case OP_SUBCC:
			ppc_subfc (code, ins->dreg, ins->sreg2, ins->sreg1);
			break;
		case CEE_SUB:
			ppc_subf (code, ins->dreg, ins->sreg2, ins->sreg1);
			break;
		case OP_SBB:
			ppc_subfe (code, ins->dreg, ins->sreg2, ins->sreg1);
			break;
		case OP_SUB_IMM:
			// we add the negated value
			g_assert (ppc_is_imm16 (-ins->inst_imm));
			ppc_addi (code, ins->dreg, ins->sreg1, -ins->inst_imm);
			break;
		case OP_SBB_IMM:
			ppc_load (code, ppc_r11, ins->inst_imm);
			ppc_subfe (code, ins->dreg, ins->sreg2, ppc_r11);
			break;
		case OP_PPC_SUBFIC:
			g_assert (ppc_is_imm16 (ins->inst_imm));
			ppc_subfic (code, ins->dreg, ins->sreg1, ins->inst_imm);
			break;
		case OP_PPC_SUBFZE:
			ppc_subfze (code, ins->dreg, ins->sreg1);
			break;
		case CEE_AND:
			/* FIXME: the ppc macros as inconsistent here: put dest as the first arg! */
			ppc_and (code, ins->sreg1, ins->dreg, ins->sreg2);
			break;
		case OP_AND_IMM:
			if (!(ins->inst_imm & 0xffff0000)) {
				ppc_andid (code, ins->sreg1, ins->dreg, ins->inst_imm);
			} else if (!(ins->inst_imm & 0xffff)) {
				ppc_andisd (code, ins->sreg1, ins->dreg, ((guint32)ins->inst_imm >> 16));
			} else {
				ppc_load (code, ppc_r11, ins->inst_imm);
				ppc_and (code, ins->sreg1, ins->dreg, ins->sreg2);
			}
			break;
		case CEE_DIV:
			ppc_divw (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case CEE_DIV_UN:
			ppc_divwu (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_DIV_IMM:
			ppc_load (code, ppc_r11, ins->inst_imm);
			ppc_divw (code, ins->dreg, ins->sreg1, ppc_r11);
			break;
		case CEE_REM:
			ppc_divw (code, ppc_r11, ins->sreg1, ins->sreg2);
			ppc_mullw (code, ppc_r11, ppc_r11, ins->sreg2);
			ppc_subf (code, ins->dreg, ppc_r11, ins->sreg1);
			break;
		case CEE_REM_UN:
			ppc_divwu (code, ppc_r11, ins->sreg1, ins->sreg2);
			ppc_mullw (code, ppc_r11, ppc_r11, ins->sreg2);
			ppc_subf (code, ins->dreg, ppc_r11, ins->sreg1);
			break;
		case OP_REM_IMM:
			ppc_load (code, ppc_r11, ins->inst_imm);
			ppc_divw (code, ins->dreg, ins->sreg1, ppc_r11);
			ppc_mullw (code, ins->dreg, ins->dreg, ppc_r11);
			ppc_subf (code, ins->dreg, ins->dreg, ins->sreg1);
			break;
		case CEE_OR:
			ppc_or (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_OR_IMM:
			if (!(ins->inst_imm & 0xffff0000)) {
				ppc_ori (code, ins->sreg1, ins->dreg, ins->inst_imm);
			} else if (!(ins->inst_imm & 0xffff)) {
				ppc_oris (code, ins->sreg1, ins->dreg, ((guint32)(ins->inst_imm) >> 16));
			} else {
				ppc_load (code, ppc_r11, ins->inst_imm);
				ppc_or (code, ins->sreg1, ins->dreg, ins->sreg2);
			}
			break;
		case CEE_XOR:
			ppc_xor (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_XOR_IMM:
			if (!(ins->inst_imm & 0xffff0000)) {
				ppc_xori (code, ins->sreg1, ins->dreg, ins->inst_imm);
			} else if (!(ins->inst_imm & 0xffff)) {
				ppc_xoris (code, ins->sreg1, ins->dreg, ((guint32)(ins->inst_imm) >> 16));
			} else {
				ppc_load (code, ppc_r11, ins->inst_imm);
				ppc_xor (code, ins->sreg1, ins->dreg, ins->sreg2);
			}
			break;
		case CEE_SHL:
			ppc_slw (code, ins->sreg1, ins->dreg, ins->sreg2);
			break;
		case OP_SHL_IMM:
			ppc_rlwinm (code, ins->dreg, ins->sreg1, (ins->inst_imm & 0xf), 0, (31 - (ins->inst_imm & 0xf)));
			//ppc_load (code, ppc_r11, ins->inst_imm);
			//ppc_slw (code, ins->sreg1, ins->dreg, ppc_r11);
			break;
		case CEE_SHR:
			ppc_sraw (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_SHR_IMM:
			// there is also ppc_srawi
			//ppc_load (code, ppc_r11, ins->inst_imm);
			//ppc_sraw (code, ins->dreg, ins->sreg1, ppc_r11);
			ppc_srawi (code, ins->dreg, ins->sreg1, (ins->inst_imm & 0x1f));
			break;
		case OP_SHR_UN_IMM:
			ppc_load (code, ppc_r11, ins->inst_imm);
			ppc_srw (code, ins->dreg, ins->sreg1, ppc_r11);
			//ppc_rlwinm (code, ins->dreg, ins->sreg1, (32 - (ins->inst_imm & 0xf)), (ins->inst_imm & 0xf), 31);
			break;
		case CEE_SHR_UN:
			ppc_srw (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case CEE_NOT:
			ppc_not (code, ins->dreg, ins->sreg1);
			break;
		case CEE_NEG:
			ppc_neg (code, ins->dreg, ins->sreg1);
			break;
		case CEE_MUL:
			ppc_mullw (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_MUL_IMM:
			ppc_load (code, ppc_r11, ins->inst_imm);
			ppc_mullw (code, ins->dreg, ins->sreg1, ppc_r11);
			break;
		case CEE_MUL_OVF:
			ppc_mullw (code, ins->dreg, ins->sreg1, ins->sreg2);
			//g_assert_not_reached ();
			//x86_imul_reg_reg (code, ins->sreg1, ins->sreg2);
			//EMIT_COND_SYSTEM_EXCEPTION (X86_CC_O, FALSE, "OverflowException");
			break;
		case CEE_MUL_OVF_UN:
			ppc_mullw (code, ins->dreg, ins->sreg1, ins->sreg2);
			//FIXME: g_assert_not_reached ();
			break;
		case OP_ICONST:
		case OP_SETREGIMM:
			ppc_load (code, ins->dreg, ins->inst_c0);
			break;
		/*case OP_CLASS:
			mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_CLASS, (gpointer)ins->inst_c0);
			ppc_load (code, ins->dreg, 0xff00ff00);
			break;
		case OP_IMAGE:
			mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_IMAGE, (gpointer)ins->inst_c0);
			ppc_load (code, ins->dreg, 0xff00ff00);
			break;*/
		case CEE_CONV_I4:
		case CEE_CONV_U4:
		case OP_MOVE:
		case OP_SETREG:
			ppc_mr (code, ins->dreg, ins->sreg1);
			break;
		case CEE_JMP:
			g_assert_not_reached ();
			break;
		case OP_CHECK_THIS:
			/* ensure ins->sreg1 is not NULL */
			g_assert_not_reached ();
			//x86_alu_membase_imm (code, X86_CMP, ins->sreg1, 0, 0);
			break;
		case OP_FCALL:
		case OP_LCALL:
		case OP_VCALL:
		case OP_VOIDCALL:
		case CEE_CALL:
			call = (MonoCallInst*)ins;
			if (ins->flags & MONO_INST_HAS_METHOD)
				mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_METHOD, call->method);
			else
				mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_ABS, call->fptr);
			ppc_bl (code, 0);
			break;
		case OP_FCALL_REG:
		case OP_LCALL_REG:
		case OP_VCALL_REG:
		case OP_VOIDCALL_REG:
		case OP_CALL_REG:
			ppc_mtlr (code, ins->sreg1);
			ppc_blrl (code);
			break;
		case OP_FCALL_MEMBASE:
		case OP_LCALL_MEMBASE:
		case OP_VCALL_MEMBASE:
		case OP_VOIDCALL_MEMBASE:
		case OP_CALL_MEMBASE:
			ppc_lwz (code, ppc_r0, ins->inst_offset, ins->sreg1);
			ppc_mtlr (code, ppc_r0);
			ppc_blrl (code);
			break;
		case OP_OUTARG:
			g_assert_not_reached ();
			break;
		case OP_LOCALLOC:
			/* keep alignment */
#define MONO_FRAME_ALIGNMENT 32
			ppc_addi (code, ppc_r0, ins->sreg1, MONO_FRAME_ALIGNMENT-1);
			ppc_rlwinm (code, ppc_r0, ppc_r0, 0, 0, 27);
			ppc_lwz (code, ppc_r11, 0, ppc_sp);
			ppc_neg (code, ppc_r0, ppc_r0);
			ppc_stwux (code, ppc_sp, ppc_r0, ppc_sp);
			ppc_mr (code, ins->dreg, ppc_sp);
			g_assert_not_reached ();
			break;
		case CEE_RET:
			ppc_blr (code);
			break;
		case CEE_THROW: {
			ppc_mr (code, ppc_r3, ins->sreg1);
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_INTERNAL_METHOD, 
					     (gpointer)"throw_exception");
			ppc_bl (code, 0);
			break;
		}
		case OP_ENDFILTER:
			if (ins->sreg1 != ppc_r3)
				ppc_mr (code, ppc_r3, ins->sreg1);
			ppc_blr (code);
			break;
		case CEE_ENDFINALLY:
			ppc_blr (code);
			break;
		case OP_CALL_HANDLER: 
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_BB, ins->inst_target_bb);
			ppc_bl (code, 0);
			break;
		case OP_LABEL:
			ins->inst_c0 = code - cfg->native_code;
			break;
		case CEE_BR:
			//g_print ("target: %p, next: %p, curr: %p, last: %p\n", ins->inst_target_bb, bb->next_bb, ins, bb->last_ins);
			//if ((ins->inst_target_bb == bb->next_bb) && ins == bb->last_ins)
			//break;
			if (ins->flags & MONO_INST_BRLABEL) {
				/*if (ins->inst_i0->inst_c0) {
					ppc_b (code, 0);
					//x86_jump_code (code, cfg->native_code + ins->inst_i0->inst_c0);
				} else*/ {
					mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_LABEL, ins->inst_i0);
					ppc_b (code, 0);
				}
			} else {
				/*if (ins->inst_target_bb->native_offset) {
					ppc_b (code, 0);
					//x86_jump_code (code, cfg->native_code + ins->inst_target_bb->native_offset); 
				} else*/ {
					mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_BB, ins->inst_target_bb);
					ppc_b (code, 0);
				} 
			}
			break;
		case OP_BR_REG:
			ppc_mtctr (code, ins->sreg1);
			ppc_bcctr (code, 20, 0);
			break;
		case OP_CEQ:
			ppc_li (code, ins->dreg, 0);
			ppc_bc (code, PPC_BR_FALSE, PPC_BR_EQ, 2);
			ppc_li (code, ins->dreg, 1);
			break;
		case OP_CLT:
		case OP_CLT_UN:
			ppc_li (code, ins->dreg, 1);
			ppc_bc (code, PPC_BR_TRUE, PPC_BR_LT, 2);
			ppc_li (code, ins->dreg, 0);
			break;
		case OP_CGT:
		case OP_CGT_UN:
			ppc_li (code, ins->dreg, 1);
			ppc_bc (code, PPC_BR_TRUE, PPC_BR_LT, 2);
			ppc_li (code, ins->dreg, 0);
			break;
		case OP_COND_EXC_EQ:
		case OP_COND_EXC_NE_UN:
		case OP_COND_EXC_LT:
		case OP_COND_EXC_LT_UN:
		case OP_COND_EXC_GT:
		case OP_COND_EXC_GT_UN:
		case OP_COND_EXC_GE:
		case OP_COND_EXC_GE_UN:
		case OP_COND_EXC_LE:
		case OP_COND_EXC_LE_UN:
		case OP_COND_EXC_OV:
		case OP_COND_EXC_NO:
		case OP_COND_EXC_C:
		case OP_COND_EXC_NC:
			//EMIT_COND_SYSTEM_EXCEPTION (branch_cc_table [ins->opcode - OP_COND_EXC_EQ], 
			//			    (ins->opcode < OP_COND_EXC_NE_UN), ins->inst_p1);
			break;
		case CEE_BEQ:
		case CEE_BNE_UN:
		case CEE_BLT:
		case CEE_BLT_UN:
		case CEE_BGT:
		case CEE_BGT_UN:
		case CEE_BGE:
		case CEE_BGE_UN:
		case CEE_BLE:
		case CEE_BLE_UN:
			EMIT_COND_BRANCH (ins, ins->opcode - CEE_BEQ);
			break;

		/* floating point opcodes */
		case OP_R8CONST:
			ppc_load (code, ppc_r11, ins->inst_p0);
			ppc_lfd (code, ins->dreg, 0, ppc_r11);
			break;
		case OP_R4CONST:
			ppc_load (code, ppc_r11, ins->inst_p0);
			ppc_lfs (code, ins->dreg, 0, ppc_r11);
			break;
		case OP_STORER8_MEMBASE_REG:
			ppc_stfd (code, ins->sreg1, ins->inst_offset, ins->inst_destbasereg);
			break;
		case OP_LOADR8_MEMBASE:
			ppc_lfd (code, ins->dreg, ins->inst_offset, ins->inst_basereg);
			break;
		case OP_STORER4_MEMBASE_REG:
			ppc_stfs (code, ins->sreg1, ins->inst_offset, ins->inst_destbasereg);
			break;
		case OP_LOADR4_MEMBASE:
			ppc_lfs (code, ins->dreg, ins->inst_offset, ins->inst_basereg);
			break;
		case CEE_CONV_R4: /* FIXME: change precision */
		case CEE_CONV_R8:
			g_assert_not_reached ();
			x86_push_reg (code, ins->sreg1);
			x86_fild_membase (code, X86_ESP, 0, FALSE);
			x86_alu_reg_imm (code, X86_ADD, X86_ESP, 4);
			break;
		case OP_X86_FP_LOAD_I8:
			g_assert_not_reached ();
			x86_fild_membase (code, ins->inst_basereg, ins->inst_offset, TRUE);
			break;
		case OP_X86_FP_LOAD_I4:
			g_assert_not_reached ();
			x86_fild_membase (code, ins->inst_basereg, ins->inst_offset, FALSE);
			break;
		case OP_FCONV_TO_I1:
			g_assert_not_reached ();
			code = emit_float_to_int (cfg, code, ins->dreg, 1, TRUE);
			break;
		case OP_FCONV_TO_U1:
			g_assert_not_reached ();
			code = emit_float_to_int (cfg, code, ins->dreg, 1, FALSE);
			break;
		case OP_FCONV_TO_I2:
			g_assert_not_reached ();
			code = emit_float_to_int (cfg, code, ins->dreg, 2, TRUE);
			break;
		case OP_FCONV_TO_U2:
			g_assert_not_reached ();
			code = emit_float_to_int (cfg, code, ins->dreg, 2, FALSE);
			break;
		case OP_FCONV_TO_I4:
		case OP_FCONV_TO_I:
			g_assert_not_reached ();
			code = emit_float_to_int (cfg, code, ins->dreg, 4, TRUE);
			break;
		case OP_FCONV_TO_U4:
		case OP_FCONV_TO_U:
			g_assert_not_reached ();
			code = emit_float_to_int (cfg, code, ins->dreg, 4, FALSE);
			break;
		case OP_FCONV_TO_I8:
		case OP_FCONV_TO_U8:
			g_assert_not_reached ();
			/*x86_alu_reg_imm (code, X86_SUB, X86_ESP, 4);
			x86_fnstcw_membase(code, X86_ESP, 0);
			x86_mov_reg_membase (code, ins->inst_dreg_low, X86_ESP, 0, 2);
			x86_alu_reg_imm (code, X86_OR, ins->inst_dreg_low, 0xc00);
			x86_mov_membase_reg (code, X86_ESP, 2, ins->inst_dreg_low, 2);
			x86_fldcw_membase (code, X86_ESP, 2);
			x86_alu_reg_imm (code, X86_SUB, X86_ESP, 8);
			x86_fist_pop_membase (code, X86_ESP, 0, TRUE);
			x86_pop_reg (code, ins->inst_dreg_low);
			x86_pop_reg (code, ins->inst_dreg_high);
			x86_fldcw_membase (code, X86_ESP, 0);
			x86_alu_reg_imm (code, X86_ADD, X86_ESP, 4);*/
			break;
		case OP_LCONV_TO_R_UN: { 
#if 0
			static guint8 mn[] = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x3f, 0x40 };
			guint8 *br;

			/* load 64bit integer to FP stack */
			x86_push_imm (code, 0);
			x86_push_reg (code, ins->sreg2);
			x86_push_reg (code, ins->sreg1);
			x86_fild_membase (code, X86_ESP, 0, TRUE);
			/* store as 80bit FP value */
			x86_fst80_membase (code, X86_ESP, 0);
			
			/* test if lreg is negative */
			x86_test_reg_reg (code, ins->sreg2, ins->sreg2);
			br = code; x86_branch8 (code, X86_CC_GEZ, 0, TRUE);
	
			/* add correction constant mn */
			x86_fld80_mem (code, mn);
			x86_fld80_membase (code, X86_ESP, 0);
			x86_fp_op_reg (code, X86_FADD, 1, TRUE);
			x86_fst80_membase (code, X86_ESP, 0);

			x86_patch (br, code);

			x86_fld80_membase (code, X86_ESP, 0);
			x86_alu_reg_imm (code, X86_ADD, X86_ESP, 12);
#endif
			g_assert_not_reached ();
			break;
		}
		case OP_LCONV_TO_OVF_I: {
#if 0
			guint8 *br [3], *label [1];

			/* 
			 * Valid ints: 0xffffffff:8000000 to 00000000:0x7f000000
			 */
			x86_test_reg_reg (code, ins->sreg1, ins->sreg1);

			/* If the low word top bit is set, see if we are negative */
			br [0] = code; x86_branch8 (code, X86_CC_LT, 0, TRUE);
			/* We are not negative (no top bit set, check for our top word to be zero */
			x86_test_reg_reg (code, ins->sreg2, ins->sreg2);
			br [1] = code; x86_branch8 (code, X86_CC_EQ, 0, TRUE);
			label [0] = code;

			/* throw exception */
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_EXC, "OverflowException");
		        x86_jump32 (code, 0);
	
			x86_patch (br [0], code);
			/* our top bit is set, check that top word is 0xfffffff */
			x86_alu_reg_imm (code, X86_CMP, ins->sreg2, 0xffffffff);
		
			x86_patch (br [1], code);
			/* nope, emit exception */
			br [2] = code; x86_branch8 (code, X86_CC_NE, 0, TRUE);
			x86_patch (br [2], label [0]);

			if (ins->dreg != ins->sreg1)
				x86_mov_reg_reg (code, ins->dreg, ins->sreg1, 4);
#endif
			g_assert_not_reached ();
			break;
		}
		case OP_FADD:
			ppc_fadd (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_FSUB:
			ppc_fsub (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;		
		case OP_FMUL:
			ppc_fmul (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;		
		case OP_FDIV:
			ppc_fdiv (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;		
		case OP_FNEG:
			ppc_fneg (code, ins->dreg, ins->sreg1);
			break;		
		case OP_FREM:
			g_assert_not_reached ();
			break;
		case OP_FCOMPARE:
			g_assert_not_reached ();
			/* this overwrites EAX */
			EMIT_FPCOMPARE(code);
			break;
		case OP_FCEQ:
			g_assert_not_reached ();
			/*if (ins->dreg != X86_EAX) 
				x86_push_reg (code, X86_EAX);

			EMIT_FPCOMPARE(code);
			x86_alu_reg_imm (code, X86_CMP, X86_EAX, 0x4000);
			x86_set_reg (code, X86_CC_EQ, ins->dreg, TRUE);
			x86_widen_reg (code, ins->dreg, ins->dreg, FALSE, FALSE);

			if (ins->dreg != X86_EAX) 
				x86_pop_reg (code, X86_EAX);*/
			break;
		case OP_FCLT:
		case OP_FCLT_UN:
			g_assert_not_reached ();
			/*if (ins->dreg != X86_EAX) 
				x86_push_reg (code, X86_EAX);

			EMIT_FPCOMPARE(code);
			x86_set_reg (code, X86_CC_EQ, ins->dreg, TRUE);
			x86_widen_reg (code, ins->dreg, ins->dreg, FALSE, FALSE);

			if (ins->dreg != X86_EAX) 
				x86_pop_reg (code, X86_EAX);*/
			break;
		case OP_FCGT:
		case OP_FCGT_UN:
			g_assert_not_reached ();
			/*if (ins->dreg != X86_EAX) 
				x86_push_reg (code, X86_EAX);

			EMIT_FPCOMPARE(code);
			x86_alu_reg_imm (code, X86_CMP, X86_EAX, 0x0100);
			x86_set_reg (code, X86_CC_EQ, ins->dreg, TRUE);
			x86_widen_reg (code, ins->dreg, ins->dreg, FALSE, FALSE);

			if (ins->dreg != X86_EAX) 
				x86_pop_reg (code, X86_EAX);*/
			break;
		case OP_FBEQ:
			g_assert_not_reached ();
			break;
		case OP_FBNE_UN:
			g_assert_not_reached ();
			break;
		case OP_FBLT:
		case OP_FBLT_UN:
			g_assert_not_reached ();
			break;
		case OP_FBGT:
		case OP_FBGT_UN:
			g_assert_not_reached ();
			break;
		case OP_FBGE:
		case OP_FBGE_UN:
			g_assert_not_reached ();
			break;
		case OP_FBLE:
		case OP_FBLE_UN:
			g_assert_not_reached ();
			break;
		case CEE_CKFINITE: {
			g_assert_not_reached ();
			x86_push_reg (code, X86_EAX);
			x86_fxam (code);
			x86_fnstsw (code);
			x86_alu_reg_imm (code, X86_AND, X86_EAX, 0x4100);
			x86_alu_reg_imm (code, X86_CMP, X86_EAX, 0x0100);
			x86_pop_reg (code, X86_EAX);
			EMIT_COND_SYSTEM_EXCEPTION (X86_CC_EQ, FALSE, "ArithmeticException");
			break;
		}
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

void
mono_arch_register_lowlevel_calls (void)
{
	mono_register_jit_icall (enter_method, "mono_enter_method", NULL, TRUE);
	mono_register_jit_icall (leave_method, "mono_leave_method", NULL, TRUE);
}

void
mono_arch_patch_code (MonoMethod *method, MonoDomain *domain, guint8 *code, MonoJumpInfo *ji)
{
	MonoJumpInfo *patch_info;

	for (patch_info = ji; patch_info; patch_info = patch_info->next) {
		unsigned char *ip = patch_info->ip.i + code;
		const unsigned char *target = NULL;

		switch (patch_info->type) {
		case MONO_PATCH_INFO_BB:
			target = patch_info->data.bb->native_offset + code;
			break;
		case MONO_PATCH_INFO_ABS:
			target = patch_info->data.target;
			break;
		case MONO_PATCH_INFO_LABEL:
			target = patch_info->data.inst->inst_c0 + code;
			break;
		case MONO_PATCH_INFO_IP:
			*((gpointer *)(ip)) = ip;
			continue;
		case MONO_PATCH_INFO_INTERNAL_METHOD: {
			MonoJitICallInfo *mi = mono_find_jit_icall_by_name (patch_info->data.name);
			if (!mi) {
				g_warning ("unknown MONO_PATCH_INFO_INTERNAL_METHOD %s", patch_info->data.name);
				g_assert_not_reached ();
			}
			target = mi->wrapper;
			break;
		}
		case MONO_PATCH_INFO_METHOD:
			if (patch_info->data.method == method) {
				target = code;
			} else {
				/* get the trampoline to the method from the domain */
				target = mono_arch_create_jit_trampoline (patch_info->data.method);
			}
			break;
		case MONO_PATCH_INFO_SWITCH: {
			gpointer *table = (gpointer *)patch_info->data.target;
			int i;

			// FIXME: inspect code to get the register
			ppc_load (ip, ppc_r11, patch_info->data.target);
			//*((gconstpointer *)(ip + 2)) = patch_info->data.target;

			for (i = 0; i < patch_info->table_size; i++) {
				table [i] = (int)patch_info->data.table [i] + code;
			}
			/* we put into the table the absolute address, no need for ppc_patch in this case */
			continue;
		}
		case MONO_PATCH_INFO_METHODCONST:
		case MONO_PATCH_INFO_CLASS:
		case MONO_PATCH_INFO_IMAGE:
		case MONO_PATCH_INFO_FIELD:
			g_assert_not_reached ();
			*((gconstpointer *)(ip + 1)) = patch_info->data.target;
			continue;
		case MONO_PATCH_INFO_R4:
		case MONO_PATCH_INFO_R8:
			g_assert_not_reached ();
			*((gconstpointer *)(ip + 2)) = patch_info->data.target;
			continue;
		default:
			g_assert_not_reached ();
		}
		ppc_patch (ip, target);
	}
}

int
mono_arch_max_epilog_size (MonoCompile *cfg)
{
	int exc_count = 0, max_epilog_size = 16 + 20*4;
	MonoJumpInfo *patch_info;
	
	if (cfg->method->save_lmf)
		max_epilog_size += 128;
	
	if (mono_jit_trace_calls != NULL)
		max_epilog_size += 50;

	if (cfg->prof_options & MONO_PROFILE_ENTER_LEAVE)
		max_epilog_size += 50;

	/* count the number of exception infos */
     
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		if (patch_info->type == MONO_PATCH_INFO_EXC)
			exc_count++;
	}

	/* 
	 * make sure we have enough space for exceptions
	 * 16 is the size of two push_imm instructions and a call
	 */
	max_epilog_size += exc_count*16;

	return max_epilog_size;
}

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

	cfg->code_size = 256;
	code = cfg->native_code = g_malloc (cfg->code_size);

	if (1 || cfg->flags & MONO_CFG_HAS_CALLS) {
		ppc_mflr (code, ppc_r0);
		ppc_stw (code, ppc_r0, 8, ppc_sp);
	}
	if (cfg->flags & MONO_CFG_HAS_ALLOCA) {
		cfg->used_int_regs |= 1 << 31;
	}

	alloc_size = cfg->stack_offset;
	pos = 0;

	if (method->save_lmf) {
#if 0
		pos += sizeof (MonoLMF);
		
		/* save the current IP */
		mono_add_patch_info (cfg, code + 1 - cfg->native_code, MONO_PATCH_INFO_IP, NULL);
		x86_push_imm (code, 0);

		/* save all caller saved regs */
		x86_push_reg (code, X86_EBX);
		x86_push_reg (code, X86_EDI);
		x86_push_reg (code, X86_ESI);
		x86_push_reg (code, X86_EBP);

		/* save method info */
		x86_push_imm (code, method);
	
		/* get the address of lmf for the current thread */
		mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_INTERNAL_METHOD, 
				     (gpointer)"get_lmf_addr");
		x86_call_code (code, 0);

		/* push lmf */
		x86_push_reg (code, X86_EAX); 
		/* push *lfm (previous_lmf) */
		x86_push_membase (code, X86_EAX, 0);
		/* *(lmf) = ESP */
		x86_mov_membase_reg (code, X86_EAX, 0, X86_ESP, 4);
#endif
	} else {

		for (i = 13; i < 32; ++i) {
			if (cfg->used_int_regs & (1 << i)) {
				pos += 4;
				ppc_stw (code, i, -pos, ppc_sp);
			}
		}
	}

	alloc_size += pos;
	// align to 16 bytes
	if (alloc_size & (16 - 1))
		alloc_size += 16 - (alloc_size & (16 - 1));

	cfg->stack_usage = alloc_size;
	if (alloc_size)
		ppc_stwu (code, ppc_sp, -alloc_size, ppc_sp);
	if (cfg->flags & MONO_CFG_HAS_ALLOCA)
		ppc_mr (code, ppc_r31, ppc_sp);

        /* compute max_offset in order to use short forward jumps */
	max_offset = 0;
	if (cfg->opt & MONO_OPT_BRANCH) {
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
	}

	if (mono_jit_trace_calls != NULL && mono_trace_eval (method))
		code = mono_arch_instrument_prolog (cfg, enter_method, code, TRUE);

	/* load arguments allocated to register from the stack */
	sig = method->signature;
	pos = 0;

	cinfo = calculate_sizes (sig, sig->pinvoke);

	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		ArgInfo *ainfo = cinfo->args + i;
		inst = cfg->varinfo [pos];
		
		if (inst->opcode == OP_REGVAR) {
			g_assert (!ainfo->regtype); // fine for now
			ppc_mr (code, inst->dreg, ainfo->reg);
			if (cfg->verbose_level > 2)
				g_print ("Argument %d assigned to register %s\n", pos, mono_arch_regname (inst->dreg));
		} else {
			/* the argument should be put on the stack: FIXME handle size != word  */
			ppc_stw (code, ainfo->reg, inst->inst_offset, inst->inst_basereg);
		}
		pos++;
	}

	cfg->code_len = code - cfg->native_code;

	return code;
}

void
mono_arch_emit_epilog (MonoCompile *cfg)
{
	MonoJumpInfo *patch_info;
	MonoMethod *method = cfg->method;
	int pos, i;
	guint8 *code;

	code = cfg->native_code + cfg->code_len;

	if (mono_jit_trace_calls != NULL && mono_trace_eval (method))
		code = mono_arch_instrument_epilog (cfg, leave_method, code, TRUE);

	
	pos = 0;
	
	if (method->save_lmf) {
		pos = -sizeof (MonoLMF);
	}

	if (method->save_lmf) {
#if 0
		/* ebx = previous_lmf */
		x86_pop_reg (code, X86_EBX);
		/* edi = lmf */
		x86_pop_reg (code, X86_EDI);
		/* *(lmf) = previous_lmf */
		x86_mov_membase_reg (code, X86_EDI, 0, X86_EBX, 4);

		/* discard method info */
		x86_pop_reg (code, X86_ESI);

		/* restore caller saved regs */
		x86_pop_reg (code, X86_EBP);
		x86_pop_reg (code, X86_ESI);
		x86_pop_reg (code, X86_EDI);
		x86_pop_reg (code, X86_EBX);
#endif
	}

	if (1 || cfg->flags & MONO_CFG_HAS_CALLS) {
		ppc_lwz (code, ppc_r0, cfg->stack_usage + 8, cfg->frame_reg);
		ppc_mtlr (code, ppc_r0);
	}
	ppc_addic (code, ppc_sp, cfg->frame_reg, cfg->stack_usage);
	for (i = 13; i < 32; ++i) {
		if (cfg->used_int_regs & (1 << i)) {
			pos += 4;
			ppc_lwz (code, i, -pos, cfg->frame_reg);
		}
	}
	ppc_blr (code);

	/* add code to raise exceptions */
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		switch (patch_info->type) {
		case MONO_PATCH_INFO_EXC:
			/*x86_patch (patch_info->ip.i + cfg->native_code, code);
			x86_push_imm (code, patch_info->data.target);
			x86_push_imm (code, patch_info->ip.i + cfg->native_code);
			patch_info->type = MONO_PATCH_INFO_INTERNAL_METHOD;
			patch_info->data.name = "throw_exception_by_name";
			patch_info->ip.i = code - cfg->native_code;
			x86_jump_code (code, 0);*/
			break;
		default:
			/* do nothing */
			break;
		}
	}

	cfg->code_len = code - cfg->native_code;

	g_assert (cfg->code_len < cfg->code_size);

}

void
mono_arch_setup_jit_tls_data (MonoJitTlsData *tls)
{
}
