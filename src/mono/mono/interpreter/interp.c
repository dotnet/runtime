/*
 * PLEASE NOTE: This is a research prototype.
 *
 *
 * interp.c: Interpreter for CIL byte codes
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
 *   Miguel de Icaza (miguel@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */
#define _ISOC99_SOURCE

#include <config.h>
#include <stdio.h>
#include <string.h>
#include <glib.h>
#include <ffi.h>
#include <math.h>


#ifdef HAVE_ALLOCA_H
#   include <alloca.h>
#else
#   ifdef __CYGWIN__
#      define alloca __builtin_alloca
#   endif
#endif

#include "interp.h"
/* trim excessive headers */
#include <mono/metadata/image.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/cil-coff.h>
#include <mono/metadata/endian.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/blob.h>
#include <mono/metadata/tokentype.h>
#include <mono/cli/cli.h>
#include <mono/cli/types.h>
#include "hacks.h"

#define OPDEF(a,b,c,d,e,f,g,h,i,j) \
	a = i,

enum {
#include "mono/cil/opcode.def"
	LAST = 0xff
};
#undef OPDEF

static int debug_indent_level = 0;

#define GET_NATI(sp) ((guint32)(sp).data.i)
#define CSIZE(x) (sizeof (x) / 4)

static void ves_exec_method (MonoMethod *mh, stackval *args);

static void
ves_real_abort (int line, MonoMethod *mh,
		const unsigned char *ip, stackval *stack, stackval *sp)
{
	MonoMethodManaged *mm = (MonoMethodManaged *)mh;
	fprintf (stderr, "Execution aborted in method: %s\n", mh->name);
	fprintf (stderr, "Line=%d IP=0x%04x, Aborted execution\n", line,
		 ip-(unsigned char *)mm->header->code);
	g_print ("0x%04x %02x\n",
		 ip-(unsigned char*)mm->header->code, *ip);
	if (sp > stack)
		printf ("\t[%d] %d 0x%08x %0.5f\n", sp-stack, sp[-1].type, sp[-1].data.i, sp[-1].data.f);
	exit (1);
}
#define ves_abort() ves_real_abort(__LINE__, mh, ip, stack, sp)

/*
 * init_class:
 * @klass: klass that needs to be initialized
 *
 * This routine calls the class constructor for @class if it needs it.
 */
static void
init_class (MonoClass *klass)
{
	guint32 cols [MONO_METHOD_SIZE];
	MonoMetadata *m;
	MonoTableInfo *t;
	int i;
	
	if (klass->inited)
		return;
	if (klass->parent)
		init_class (klass->parent);
	
	m = &klass->image->metadata;
	t = &m->tables [MONO_TABLE_METHOD];

	for (i = klass->method.first; i < klass->method.last; ++i) {
		mono_metadata_decode_row (t, i, cols, MONO_METHOD_SIZE);
		
		if (strcmp (".cctor", mono_metadata_string_heap (m, cols [MONO_METHOD_NAME])) == 0) {
			MonoMethod *cctor = mono_get_method (klass->image, MONO_TOKEN_METHOD_DEF | (i + 1));
			ves_exec_method (cctor, NULL);
			mono_free_method (cctor);
			klass->inited = 1;
			return;
		}
	}
	/* No class constructor found */
	klass->inited = 1;
}

/*
 * newobj:
 * @image: image where the object is being referenced
 * @token: method token to invoke
 *
 * This routine creates a new object based on the class where the
 * constructor lives.x
 */
static MonoObject *
newobj (MonoImage *image, guint32 token)
{
	MonoMetadata *m = &image->metadata;
	MonoObject *result = NULL;
	
	switch (mono_metadata_token_code (token)){
	case MONO_TOKEN_METHOD_DEF: {
		guint32 idx = mono_metadata_typedef_from_method (m, token);
		result = mono_object_new (image, MONO_TOKEN_TYPE_DEF | idx);
		break;
	}
	case MONO_TOKEN_MEMBER_REF: {
		guint32 member_cols [MONO_MEMBERREF_SIZE];
		guint32 mpr_token, table, idx;
		
		mono_metadata_decode_row (
			&m->tables [MONO_TABLE_MEMBERREF],
			mono_metadata_token_index (token) - 1,
			member_cols, CSIZE (member_cols));
		mpr_token = member_cols [MONO_MEMBERREF_CLASS];
		table = mpr_token & 7;
		idx = mpr_token >> 3;
		
		switch (table){
		case 0: /* TypeDef */
			result = mono_object_new (image, MONO_TOKEN_TYPE_DEF | idx);
			break;
		case 1: /* TypeRef */
			result = mono_object_new (image, MONO_TOKEN_TYPE_REF | idx);
			break;
		case 2: /* ModuleRef */
			g_error ("Unhandled: ModuleRef");
			
		case 3: /* MethodDef */
			g_error ("Unhandled: MethodDef");
			
		case 4: /* TypeSpec */
			g_error ("Unhandled: TypeSepc");
		}
		break;
	}
	}
	
	if (result)
		init_class (result->klass);
	return result;
}

static MonoMethod*
get_virtual_method (MonoImage *image, guint32 token, stackval *args)
{
	switch (mono_metadata_token_table (token)) {
	case MONO_TABLE_METHOD:
		return mono_get_method (image, token);
	}
	g_error ("got virtual method: 0x%x\n", token);
	return NULL;
}

static stackval
stackval_from_data (MonoType *type, const char *data, guint offset)
{
	stackval result;
	switch (type->type) {
	case MONO_TYPE_I1:
		result.type = VAL_I32;
		result.data.i = *(gint8*)(data + offset);
		break;
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
		result.type = VAL_I32;
		result.data.i = *(guint8*)(data + offset);
		break;
	case MONO_TYPE_I2:
		result.type = VAL_I32;
		result.data.i = *(gint16*)(data + offset);
		break;
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
		result.type = VAL_I32;
		result.data.i = *(guint16*)(data + offset);
		break;
	case MONO_TYPE_I4:
		result.type = VAL_I32;
		result.data.i = *(gint32*)(data + offset);
		break;
	case MONO_TYPE_U4:
		result.type = VAL_I32;
		result.data.i = *(guint32*)(data + offset);
		break;
	case MONO_TYPE_R4:
		result.type = VAL_DOUBLE;
		result.data.f = *(float*)(data + offset);
		break;
	case MONO_TYPE_R8:
		result.type = VAL_DOUBLE;
		result.data.f = *(double*)(data + offset);
		break;
	case MONO_TYPE_CLASS:
		result.type = VAL_OBJ;
		result.data.p = *(gpointer*)(data + offset);
		break;
	default:
		g_warning ("got type %x", type->type);
		g_assert_not_reached ();
	}
	return result;
}

static void
stackval_to_data (MonoType *type, stackval *val, char *data, guint offset)
{
	switch (type->type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
		*(guint8*)(data + offset) = val->data.i;
		break;
	case MONO_TYPE_BOOLEAN:
		*(guint8*)(data + offset) = (val->data.i != 0);
		break;
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
		*(guint16*)(data + offset) = val->data.i;
		break;
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		*(gint32*)(data + offset) = val->data.i;
		break;
	case MONO_TYPE_R4:
		*(float*)(data + offset) = val->data.f;
		break;
	case MONO_TYPE_R8:
		*(double*)(data + offset) = val->data.f;
		break;
	case MONO_TYPE_CLASS:
		*(gpointer*)(data + offset) = val->data.p;
		break;
	default:
		g_warning ("got type %x", type->type);
		g_assert_not_reached ();
	}
}

static void 
ves_pinvoke_method (MonoMethod *mh, stackval *sp)
{
	MonoMethodPInvoke *piinfo = (MonoMethodPInvoke *)mh;
	static void *values[256];
	static float tmp_float[256];
	int i, acount;
	static char res[256]; /* fixme */

	acount = mh->signature->param_count;

	/* hardcoded limit of max 256 parameter - this prevents us from 
	   dynamic memory alocation for args, values, ... */

	g_assert (acount < 256);

	/* fixme: only works on little endian mashines */

	for (i = 0; i < acount; i++) {

		switch (mh->signature->params [i]->type->type) {

		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
			values[i] = &sp [i].data.i;
			break;
		case MONO_TYPE_R4:
			tmp_float [i] = sp [i].data.f;
			values[i] = &tmp_float [i];
			break;
		case MONO_TYPE_R8:
			values[i] = &sp [i].data.f;
			break;
		case MONO_TYPE_STRING: /* fixme: this is wrong ? */
			values[i] = &sp [i].data.p;
			break;
 		default:
			g_warning ("not implemented %x", 
				   mh->signature->params [i]->type->type);
			g_assert_not_reached ();
		}

	}

	ffi_call (piinfo->cif, piinfo->addr, res, values);
		
	if (mh->signature->ret->type)
		*sp = stackval_from_data (mh->signature->ret->type, res, 0);
}

#define DEBUG_INTERP 0
#if DEBUG_INTERP
#define OPDEF(a,b,c,d,e,f,g,h,i,j)  b,
static char *opcode_names[] = {
#include "mono/cil/opcode.def"
	NULL
};

static void
output_indent (void)
{
	int h;

	for (h = 0; h < debug_indent_level; h++)
		g_print ("  ");
}

static void
dump_stack (stackval *stack, stackval *sp)
{
	stackval *s = stack;
	
	if (sp == stack)
		return;
	
	output_indent ();
	g_print ("stack: ");
		
	while (s < sp) {
		switch (s->type) {
		case VAL_I32: g_print ("[%d] ", s->data.i); break;
		case VAL_I64: g_print ("[%lld] ", s->data.l); break;
		case VAL_DOUBLE: g_print ("[%0.5f] ", s->data.f); break;
		default: g_print ("[%p] ", s->data.p); break;
		}
		++s;
	}
}

#endif

#define DEFAULT_LOCALS_SIZE	64

/*
 * Need to optimize ALU ops when natural int == int32 
 *
 * Need to design how exceptions are supposed to work...
 *
 * IDEA: if we maintain a stack of ip, sp to be checked
 * in the return opcode, we could inline simple methods that don't
 * use the stack or local variables....
 * 
 * The {,.S} versions of many opcodes can/should be merged to reduce code
 * duplication.
 * 
 * -fomit-frame-pointer gives about 2% speedup. 
 */
static void 
ves_exec_method (MonoMethod *mh, stackval *args)
{
	MonoMethodManaged *mm = (MonoMethodManaged *)mh;
	stackval *stack;
	register const unsigned char *ip;
	register stackval *sp;
	char locals_store [DEFAULT_LOCALS_SIZE];
	char *locals = locals_store;
	GOTO_LABEL_VARS;

	if (mh->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) {
		ves_pinvoke_method (mh, args);
		return;
	}

	ip = mm->header->code;

#if DEBUG_INTERP
	debug_indent_level++;
	output_indent ();
	g_print ("Entering %s\n", mh->name);
#endif

	/*
	 * with alloca we get the expected huge performance gain
	 * stackval *stack = g_new0(stackval, mh->max_stack);
	 */
	/* We allocate one more stack val and increase stack temporarily to handle
	 * passing this to instance methods: this needs to be removed when we'll
	 * use a different argument passing mechanism.
 	 */

	stack = alloca (sizeof (stackval) * (mm->header->max_stack + 1));
	sp = stack + 1;
	++stack;

	if (mm->header->num_locals && mm->header->locals_offsets [0] > DEFAULT_LOCALS_SIZE) {
		locals = alloca (mm->header->locals_offsets [0]);
	}

	/*
	 * using while (ip < end) may result in a 15% performance drop, 
	 * but it may be useful for debug
	 */
	while (1) {
		/*g_assert (sp >= stack);*/
#if DEBUG_INTERP
		dump_stack (stack, sp);
		g_print ("\n");
		output_indent ();
		g_print ("0x%04x: %s\n",
			 ip-(unsigned char*)mm->header->code,
			 *ip == 0xfe ? opcode_names [256 + ip [1]] : opcode_names [*ip]);
#endif
		
		SWITCH (*ip) {
		CASE (CEE_NOP) 
			++ip;
			BREAK;
		CASE (CEE_BREAK)
			++ip;
			G_BREAKPOINT (); /* this is not portable... */
			BREAK;
		CASE (CEE_LDARG_0)
		CASE (CEE_LDARG_1)
		CASE (CEE_LDARG_2)
		CASE (CEE_LDARG_3)
			*sp = args [(*ip)-CEE_LDARG_0];
			++sp;
			++ip;
			BREAK;
		CASE (CEE_LDLOC_0)
		CASE (CEE_LDLOC_1)
		CASE (CEE_LDLOC_2)
		CASE (CEE_LDLOC_3) {
			int n = (*ip)-CEE_LDLOC_0;
			++ip;
			*sp = stackval_from_data (mm->header->locals [n], locals,
				n ? mm->header->locals_offsets [n]: 0);
			++sp;
			BREAK;
		}
		CASE (CEE_STLOC_0)
		CASE (CEE_STLOC_1)
		CASE (CEE_STLOC_2)
		CASE (CEE_STLOC_3) {
			int n = (*ip)-CEE_STLOC_0;
			++ip;
			--sp;
			stackval_to_data (mm->header->locals [n], sp, locals,
				n ? mm->header->locals_offsets [n]: 0);
			BREAK;
		}
		CASE (CEE_LDARG_S)
			++ip;
			*sp = args [*ip];
			++sp;
			++ip;
			BREAK;
		CASE (CEE_LDARGA_S)
			++ip;
			sp->type = VAL_TP;
			sp->data.p = &(args [*ip]);
			++sp;
			++ip;
			BREAK;
		CASE (CEE_STARG_S) ves_abort(); BREAK;
		CASE (CEE_LDLOC_S)
			++ip;
			*sp = stackval_from_data (mm->header->locals [*ip], locals,
				*ip ? mm->header->locals_offsets [*ip]: 0);
			++ip;
			++sp;
			BREAK;
		CASE (CEE_LDLOCA_S)
			++ip;
			sp->type = VAL_TP;
			sp->data.p = locals + (*ip ? mm->header->locals_offsets [*ip]: 0);
			++sp;
			++ip;
			BREAK;
		CASE (CEE_STLOC_S)
			++ip;
			--sp;
			stackval_to_data (mm->header->locals [*ip], sp, locals,
				*ip ? mm->header->locals_offsets [*ip]: 0);
			++ip;
			BREAK;
		CASE (CEE_LDNULL) 
			++ip;
			sp->type = VAL_OBJ;
			sp->data.p = NULL;
			++sp;
			BREAK;
		CASE (CEE_LDC_I4_M1)
			++ip;
			sp->type = VAL_I32;
			sp->data.i = -1;
			++sp;
			BREAK;
		CASE (CEE_LDC_I4_0)
		CASE (CEE_LDC_I4_1)
		CASE (CEE_LDC_I4_2)
		CASE (CEE_LDC_I4_3)
		CASE (CEE_LDC_I4_4)
		CASE (CEE_LDC_I4_5)
		CASE (CEE_LDC_I4_6)
		CASE (CEE_LDC_I4_7)
		CASE (CEE_LDC_I4_8)
			sp->type = VAL_I32;
			sp->data.i = (*ip) - CEE_LDC_I4_0;
			++sp;
			++ip;
			BREAK;
		CASE (CEE_LDC_I4_S) 
			++ip;
			sp->type = VAL_I32;
			sp->data.i = *ip; /* FIXME: signed? */
			++ip;
			++sp;
			BREAK;
		CASE (CEE_LDC_I4)
			++ip;
			sp->type = VAL_I32;
			sp->data.i = read32 (ip);
			ip += 4;
			++sp;
			BREAK;
		CASE (CEE_LDC_I8)
			++ip;
			sp->type = VAL_I64;
			sp->data.i = read64 (ip);
			ip += 8;
			++sp;
			BREAK;
		CASE (CEE_LDC_R4)
			++ip;
			sp->type = VAL_DOUBLE;
			/* FIXME: ENOENDIAN */
			sp->data.f = *(float*)(ip);
			ip += sizeof (float);
			++sp;
			BREAK;
		CASE (CEE_LDC_R8) 
			++ip;
			sp->type = VAL_DOUBLE;
			/* FIXME: ENOENDIAN */
			sp->data.f = *(double*) (ip);
			ip += sizeof (double);
			++sp;
			BREAK;
		CASE (CEE_UNUSED99) ves_abort (); BREAK;
		CASE (CEE_DUP) 
			*sp = sp [-1]; 
			++sp; 
			++ip; 
			BREAK;
		CASE (CEE_POP)
			++ip;
			--sp;
			BREAK;
		CASE (CEE_JMP) ves_abort(); BREAK;
		CASE (CEE_CALLVIRT) /* Fall through */
		CASE (CEE_CALL) {
			MonoMethod *cmh;
			guint32 token;
			int virtual = *ip == CEE_CALLVIRT;

			++ip;
			token = read32 (ip);
			ip += 4;
			if (virtual)
				cmh = get_virtual_method (mh->image, token, sp);
			else
				cmh = mono_get_method (mh->image, token);
			g_assert (cmh->signature->call_convention == MONO_CALL_DEFAULT);

			/* decrement by the actual number of args */
			sp -= cmh->signature->param_count;
			if (cmh->signature->hasthis) {
				g_assert (sp >= stack);
				--sp;
				g_assert (sp->type == VAL_OBJ);
			}

			/* we need to truncate according to the type of args ... */
			ves_exec_method (cmh, sp);

			/* need to handle typedbyref ... */
			if (cmh->signature->ret->type)
				sp++;
			BREAK;
		}
		CASE (CEE_CALLI) ves_abort(); BREAK;
		CASE (CEE_RET)
			if (mh->signature->ret->type) {
				--sp;
				*args = *sp;
			}
			if (sp > stack)
				g_warning ("more values on stack: %d", sp-stack);

			/* g_free (stack); */
#if DEBUG_INTERP
			debug_indent_level--;
#endif
			return;
		CASE (CEE_BR_S)
			++ip;
			ip += (signed char) *ip;
			++ip;
			BREAK;
		CASE (CEE_BRFALSE_S) {
			int result;
			++ip;
			--sp;
			switch (sp->type) {
			case VAL_I32: result = sp->data.i == 0; break;
			case VAL_I64: result = sp->data.l == 0; break;
			case VAL_DOUBLE: result = sp->data.f ? 0: 1; break;
			default: result = sp->data.p == NULL; break;
			}
			if (result)
				ip += (signed char)*ip;
			++ip;
			BREAK;
		}
		CASE (CEE_BRTRUE_S) {
			int result;
			++ip;
			--sp;
			switch (sp->type) {
			case VAL_I32: result = sp->data.i != 0; break;
			case VAL_I64: result = sp->data.l != 0; break;
			case VAL_DOUBLE: result = sp->data.f ? 1 : 0; break;
			default: result = sp->data.p != NULL; break;
			}
			if (result)
				ip += (signed char)*ip;
			++ip;
			BREAK;
		}
		CASE (CEE_BEQ_S) {
			int result;
			++ip;
			sp -= 2;
			if (sp->type == VAL_I32)
				result = sp [0].data.i == GET_NATI (sp [1]);
			else if (sp->type == VAL_I64)
				result = sp [0].data.l == sp [1].data.l;
			else if (sp->type == VAL_DOUBLE)
				result = sp [0].data.f == sp [1].data.f;
			else
				result = GET_NATI (sp [0]) == GET_NATI (sp [1]);
			if (result)
				ip += (signed char)*ip;
			++ip;
			BREAK;
		}
		CASE (CEE_BGE_S) {
			int result;
			++ip;
			sp -= 2;
			if (sp->type == VAL_I32)
				result = sp [0].data.i >= GET_NATI (sp [1]);
			else if (sp->type == VAL_I64)
				result = sp [0].data.l >= sp [1].data.l;
			else if (sp->type == VAL_DOUBLE)
				result = sp [0].data.f >= sp [1].data.f;
			else
				result = GET_NATI (sp [0]) >= GET_NATI (sp [1]);
			if (result)
				ip += (signed char)*ip;
			++ip;
			BREAK;
		}
		CASE (CEE_BGT_S) {
			int result;
			++ip;
			sp -= 2;
			if (sp->type == VAL_I32)
				result = sp [0].data.i > GET_NATI (sp [1]);
			else if (sp->type == VAL_I64)
				result = sp [0].data.l > sp [1].data.l;
			else if (sp->type == VAL_DOUBLE)
				result = sp [0].data.f > sp [1].data.f;
			else
				result = GET_NATI (sp [0]) > GET_NATI (sp [1]);
			if (result)
				ip += (signed char)*ip;
			++ip;
			BREAK;
		}
		CASE (CEE_BLT_S) {
			int result;
			++ip;
			sp -= 2;
			if (sp->type == VAL_I32)
				result = sp[0].data.i < GET_NATI(sp[1]);
			else if (sp->type == VAL_I64)
				result = sp[0].data.l < sp[1].data.l;
			else if (sp->type == VAL_DOUBLE)
				result = sp[0].data.f < sp[1].data.f;
			else
				result = GET_NATI(sp[0]) < GET_NATI(sp[1]);
			if (result)
				ip += (signed char)*ip;
			++ip;
			BREAK;
		}
		CASE (CEE_BLE_S) {
			int result;
			++ip;
			sp -= 2;

			if (sp->type == VAL_I32)
				result = sp [0].data.i <= GET_NATI (sp [1]);
			else if (sp->type == VAL_I64)
				result = sp [0].data.l <= sp [1].data.l;
			else if (sp->type == VAL_DOUBLE)
				result = sp [0].data.f <= sp [1].data.f;
			else {
				/*
				 * FIXME: here and in other places GET_NATI on the left side 
				 * _will_ be wrong when we change the macro to work on 64 buts 
				 * systems.
				 */
				result = GET_NATI (sp [0]) <= GET_NATI (sp [1]);
			}
			if (result)
				ip += (signed char)*ip;
			++ip;
			BREAK;
		}
		CASE (CEE_BNE_UN_S) {
			int result;
			++ip;
			sp -= 2;
			if (sp->type == VAL_I32)
				result = (guint32)sp [0].data.i != (guint32)GET_NATI (sp [1]);
			else if (sp->type == VAL_I64)
				result = (guint64)sp [0].data.l != (guint64)sp [1].data.l;
			else if (sp->type == VAL_DOUBLE)
				result = isunordered (sp [0].data.f, sp [1].data.f) ||
					(sp [0].data.f != sp [1].data.f);
			else
				result = GET_NATI (sp [0]) != GET_NATI (sp [1]);
			if (result)
				ip += (signed char)*ip;
			++ip;
			BREAK;
		}
		CASE (CEE_BGE_UN_S) {
			int result;
			++ip;
			sp -= 2;
			if (sp->type == VAL_I32)
				result = (guint32)sp [0].data.i >= (guint32)GET_NATI (sp [1]);
			else if (sp->type == VAL_I64)
				result = (guint64)sp [0].data.l >= (guint64)sp [1].data.l;
			else if (sp->type == VAL_DOUBLE)
				result = !isless (sp [0].data.f,sp [1].data.f);
			else
				result = GET_NATI (sp [0]) >= GET_NATI (sp [1]);
			if (result)
				ip += (signed char)*ip;
			++ip;
			BREAK;
		}
		CASE (CEE_BGT_UN_S) {
			int result;
			++ip;
			sp -= 2;
			if (sp->type == VAL_I32)
				result = (guint32)sp [0].data.i > (guint32)GET_NATI (sp [1]);
			else if (sp->type == VAL_I64)
				result = (guint64)sp [0].data.l > (guint64)sp [1].data.l;
			else if (sp->type == VAL_DOUBLE)
				result = isgreater (sp [0].data.f, sp [1].data.f);
			else
				result = GET_NATI (sp [0]) > GET_NATI (sp [1]);
			if (result)
				ip += (signed char)*ip;
			++ip;
			BREAK;
		}
		CASE (CEE_BLE_UN_S) {
			int result;
			++ip;
			sp -= 2;
			if (sp->type == VAL_I32)
				result = (guint32)sp [0].data.i <= (guint32)GET_NATI (sp [1]);
			else if (sp->type == VAL_I64)
				result = (guint64)sp [0].data.l <= (guint64)sp [1].data.l;
			else if (sp->type == VAL_DOUBLE)
				result = islessequal (sp [0].data.f, sp [1].data.f);
			else
				result = GET_NATI (sp [0]) <= GET_NATI (sp [1]);
			if (result)
				ip += (signed char)*ip;
			++ip;
			BREAK;
		}
		CASE (CEE_BLT_UN_S) {
			int result;
			++ip;
			sp -= 2;
			if (sp->type == VAL_I32)
				result = (guint32)sp[0].data.i < (guint32)GET_NATI(sp[1]);
			else if (sp->type == VAL_I64)
				result = (guint64)sp[0].data.l < (guint64)sp[1].data.l;
			else if (sp->type == VAL_DOUBLE)
				isunordered (sp [0].data.f, sp [1].data.f) ||
					(sp [0].data.f < sp [1].data.f);
			else
				result = GET_NATI(sp[0]) < GET_NATI(sp[1]);
			if (result)
				ip += (signed char)*ip;
			++ip;
			BREAK;
		}
		CASE (CEE_BR) ves_abort(); BREAK;
		CASE (CEE_BRFALSE) ves_abort(); BREAK;
		CASE (CEE_BRTRUE) ves_abort(); BREAK;
		CASE (CEE_BEQ) ves_abort(); BREAK;
		CASE (CEE_BGE) ves_abort(); BREAK;
		CASE (CEE_BGT) ves_abort(); BREAK;
		CASE (CEE_BLE) ves_abort(); BREAK;
		CASE (CEE_BLT) ves_abort(); BREAK;
		CASE (CEE_BNE_UN) ves_abort(); BREAK;
		CASE (CEE_BGE_UN) ves_abort(); BREAK;
		CASE (CEE_BGT_UN) ves_abort(); BREAK;
		CASE (CEE_BLE_UN) ves_abort(); BREAK;
		CASE (CEE_BLT_UN) ves_abort(); BREAK;
		CASE (CEE_SWITCH) {
			guint32 n;
			const unsigned char *st;
			++ip;
			n = read32 (ip);
			ip += 4;
			st = ip + sizeof (gint32) * n;
			--sp;
			if ((guint32)sp->data.i < n) {
				gint offset;
				ip += sizeof (gint32) * (guint32)sp->data.i;
				offset = read32 (ip);
				ip = st + offset;
			} else {
				ip = st;
			}
			BREAK;
		}
		CASE (CEE_LDIND_I1)
			++ip;
			sp[-1].type = VAL_I32;
			sp[-1].data.i = *(gint8*)sp[-1].data.p;
			BREAK;
		CASE (CEE_LDIND_U1)
			++ip;
			sp[-1].type = VAL_I32;
			sp[-1].data.i = *(guint8*)sp[-1].data.p;
			BREAK;
		CASE (CEE_LDIND_I2)
			++ip;
			sp[-1].type = VAL_I32;
			sp[-1].data.i = *(gint16*)sp[-1].data.p;
			BREAK;
		CASE (CEE_LDIND_U2)
			++ip;
			sp[-1].type = VAL_I32;
			sp[-1].data.i = *(guint16*)sp[-1].data.p;
			BREAK;
		CASE (CEE_LDIND_I4) /* Fall through */
		CASE (CEE_LDIND_U4)
			++ip;
			sp[-1].type = VAL_I32;
			sp[-1].data.i = *(gint32*)sp[-1].data.p;
			BREAK;
		CASE (CEE_LDIND_I8)
			++ip;
			sp[-1].type = VAL_I64;
			sp[-1].data.l = *(gint64*)sp[-1].data.p;
			BREAK;
		CASE (CEE_LDIND_I)
			++ip;
			sp[-1].type = VAL_NATI;
			sp[-1].data.p = *(gpointer*)sp[-1].data.p;
			BREAK;
		CASE (CEE_LDIND_R4)
			++ip;
			sp[-1].type = VAL_DOUBLE;
			sp[-1].data.i = *(gfloat*)sp[-1].data.p;
			BREAK;
		CASE (CEE_LDIND_R8)
			++ip;
			sp[-1].type = VAL_DOUBLE;
			sp[-1].data.i = *(gdouble*)sp[-1].data.p;
			BREAK;
		CASE (CEE_LDIND_REF)
			++ip;
			sp[-1].type = VAL_OBJ;
			sp[-1].data.p = *(gpointer*)sp[-1].data.p;
			BREAK;
		CASE (CEE_STIND_REF)
			++ip;
			sp -= 2;
			*(gpointer*)sp->data.p = sp[1].data.p;
			BREAK;
		CASE (CEE_STIND_I1)
			++ip;
			sp -= 2;
			*(gint8*)sp->data.p = (gint8)sp[1].data.i;
			BREAK;
		CASE (CEE_STIND_I2)
			++ip;
			sp -= 2;
			*(gint16*)sp->data.p = (gint16)sp[1].data.i;
			BREAK;
		CASE (CEE_STIND_I4)
			++ip;
			sp -= 2;
			*(gint32*)sp->data.p = sp[1].data.i;
			BREAK;
		CASE (CEE_STIND_I8)
			++ip;
			sp -= 2;
			*(gint64*)sp->data.p = sp[1].data.l;
			BREAK;
		CASE (CEE_STIND_R4)
			++ip;
			sp -= 2;
			*(gfloat*)sp->data.p = (gfloat)sp[1].data.f;
			BREAK;
		CASE (CEE_STIND_R8)
			++ip;
			sp -= 2;
			*(gdouble*)sp->data.p = sp[1].data.f;
			BREAK;
		CASE (CEE_ADD)
			++ip;
			--sp;
			/* should probably consider the pointers as unsigned */
			if (sp->type == VAL_I32)
				sp [-1].data.i += GET_NATI (sp [0]);
			else if (sp->type == VAL_I64)
				sp [-1].data.l += sp [0].data.l;
			else if (sp->type == VAL_DOUBLE)
				sp [-1].data.f += sp [0].data.f;
			else
				(char*)sp [-1].data.p += GET_NATI (sp [0]);
			BREAK;
		CASE (CEE_SUB)
			++ip;
			--sp;
			/* should probably consider the pointers as unsigned */
			if (sp->type == VAL_I32)
				sp [-1].data.i -= GET_NATI (sp [0]);
			else if (sp->type == VAL_I64)
				sp [-1].data.l -= sp [0].data.l;
			else if (sp->type == VAL_DOUBLE)
				sp [-1].data.f -= sp [0].data.f;
			else
				(char*)sp [-1].data.p -= GET_NATI (sp [0]);
			BREAK;
		CASE (CEE_MUL)
			++ip;
			--sp;
			if (sp->type == VAL_I32)
				sp [-1].data.i *= GET_NATI (sp [0]);
			else if (sp->type == VAL_I64)
				sp [-1].data.l *= sp [0].data.l;
			else if (sp->type == VAL_DOUBLE)
				sp [-1].data.f *= sp [0].data.f;
			BREAK;
		CASE (CEE_DIV)
			++ip;
			--sp;
			if (sp->type == VAL_I32)
				sp [-1].data.i /= GET_NATI (sp [0]);
			else if (sp->type == VAL_I64)
				sp [-1].data.l /= sp [0].data.l;
			else if (sp->type == VAL_DOUBLE)
				sp [-1].data.f /= sp [0].data.f;
			BREAK;
		CASE (CEE_DIV_UN)
			++ip;
			--sp;
			if (sp->type == VAL_I32)
				(guint32)sp [-1].data.i /= (guint32)GET_NATI (sp [0]);
			else if (sp->type == VAL_I64)
				(guint64)sp [-1].data.l /= (guint64)sp [0].data.l;
			else if (sp->type == VAL_NATI)
				(gulong)sp [-1].data.p /= (gulong)sp [0].data.p;
			BREAK;
		CASE (CEE_REM)
			++ip;
			--sp;
			if (sp->type == VAL_I32)
				sp [-1].data.i %= GET_NATI (sp [0]);
			else if (sp->type == VAL_I64)
				sp [-1].data.l %= sp [0].data.l;
			else if (sp->type == VAL_DOUBLE)
				/* FIXME: what do we actually fo here? */
				sp [-1].data.f = 0;
			else
				GET_NATI (sp [-1]) %= GET_NATI (sp [0]);
			BREAK;
		CASE (CEE_REM_UN) ves_abort(); BREAK;
		CASE (CEE_AND)
			++ip;
			--sp;
			if (sp->type == VAL_I32)
				sp [-1].data.i &= GET_NATI (sp [0]);
			else if (sp->type == VAL_I64)
				sp [-1].data.l &= sp [0].data.l;
			else
				GET_NATI (sp [-1]) &= GET_NATI (sp [0]);
			BREAK;
		CASE (CEE_OR)
			++ip;
			--sp;
			if (sp->type == VAL_I32)
				sp [-1].data.i |= GET_NATI (sp [0]);
			else if (sp->type == VAL_I64)
				sp [-1].data.l |= sp [0].data.l;
			else
				GET_NATI (sp [-1]) |= GET_NATI (sp [0]);
			BREAK;
		CASE (CEE_XOR)
			++ip;
			--sp;
			if (sp->type == VAL_I32)
				sp [-1].data.i ^= GET_NATI (sp [0]);
			else if (sp->type == VAL_I64)
				sp [-1].data.l ^= sp [0].data.l;
			else
				GET_NATI (sp [-1]) ^= GET_NATI (sp [0]);
			BREAK;
		CASE (CEE_SHL)
			++ip;
			--sp;
			if (sp->type == VAL_I32)
				sp [-1].data.i <<= GET_NATI (sp [0]);
			else if (sp->type == VAL_I64)
				sp [-1].data.l <<= GET_NATI (sp [0]);
			else
				GET_NATI (sp [-1]) <<= GET_NATI (sp [0]);
			BREAK;
		CASE (CEE_SHR)
			++ip;
			--sp;
			if (sp->type == VAL_I32)
				sp [-1].data.i >>= GET_NATI (sp [0]);
			else if (sp->type == VAL_I64)
				sp [-1].data.l >>= GET_NATI (sp [0]);
			else
				GET_NATI (sp [-1]) >>= GET_NATI (sp [0]);
			BREAK;
		CASE (CEE_SHR_UN) ves_abort(); BREAK;
		CASE (CEE_NEG)
			++ip;
			if (sp->type == VAL_I32)
				sp->data.i = - sp->data.i;
			else if (sp->type == VAL_I64)
				sp->data.l = - sp->data.l;
			else if (sp->type == VAL_DOUBLE)
				sp->data.f = - sp->data.f;
			else if (sp->type == VAL_NATI)
				sp->data.p = (gpointer)(- (m_i)sp->data.p);
			BREAK;
		CASE (CEE_NOT)
			++ip;
			if (sp->type == VAL_I32)
				sp->data.i = ~ sp->data.i;
			else if (sp->type == VAL_I64)
				sp->data.l = ~ sp->data.l;
			else if (sp->type == VAL_NATI)
				sp->data.p = (gpointer)(~ (m_i)sp->data.p);
			BREAK;
		CASE (CEE_CONV_I1) ves_abort(); BREAK;
		CASE (CEE_CONV_I2) ves_abort(); BREAK;
		CASE (CEE_CONV_I4)
			++ip;
			/* FIXME: handle other cases. what about sign? */
			if (sp [-1].type == VAL_DOUBLE) {
				sp [-1].data.i = (gint32)sp [-1].data.f;
				sp [-1].type = VAL_I32;
			} else {
				ves_abort();
			}
			BREAK;
		CASE (CEE_CONV_I8) ves_abort(); BREAK;
		CASE (CEE_CONV_R4) ves_abort(); BREAK;
		CASE (CEE_CONV_R8)
			++ip;
			/* FIXME: handle other cases. what about sign? */
			if (sp [-1].type == VAL_I32) {
				sp [-1].data.f = (double)sp [-1].data.i;
				sp [-1].type = VAL_DOUBLE;
			} else {
				ves_abort();
			}
			BREAK;
		CASE (CEE_CONV_U4)
			++ip;
			/* FIXME: handle other cases. what about sign? */
			if (sp [-1].type == VAL_DOUBLE) {
				sp [-1].data.i = (guint32)sp [-1].data.f;
				sp [-1].type = VAL_I32;
			} else {
				ves_abort();
			}
			BREAK;
		CASE (CEE_CONV_U8) ves_abort(); BREAK;
		CASE (CEE_CPOBJ) ves_abort(); BREAK;
		CASE (CEE_LDOBJ) ves_abort(); BREAK;
		CASE (CEE_LDSTR) {
			guint32 ctor, ttoken;
			MonoMetadata *m = &mh->image->metadata;
			MonoMethod *cmh;
			MonoObject *o;
			MonoImage *cl;
		        const char *name;
			int len;
			guint32 index;
			guint16 *data;

			ip++;
			index = mono_metadata_token_index (read32 (ip));
			name = mono_metadata_user_string (m, index);
			len = mono_metadata_decode_blob_size (name, &name);

			/* 
			 * terminate with 0, maybe we should use another 
			 * constructor and pass the len
			 */
			data = g_malloc (len + 2);
			memcpy (data, name, len + 2);
			data [len/2 + 1] = 0;

			ctor = mono_get_string_class_info (&ttoken, &cl);
			o = mono_object_new (cl, ttoken);
		
			g_assert (o != NULL);

			cmh = mono_get_method (cl, ctor);

			sp->type = VAL_OBJ;
			sp->data.p = o;
			
			sp[1].type = VAL_TP;
			sp[1].data.p = data;

			g_assert (cmh->signature->call_convention == MONO_CALL_DEFAULT);
			ves_exec_method (cmh, sp);

			g_free (data);

			sp->type = VAL_OBJ;
			sp->data.p = o;
			++sp;
			BREAK;
		}
		CASE (CEE_NEWOBJ) {
			MonoObject *o;
			MonoMethod *cmh;
			guint32 token;
			int pc;

			ip++;
			token = read32 (ip);
			o = newobj (mh->image, token);
			ip += 4;
			
			/* call the contructor */
			cmh = mono_get_method (mh->image, token);

			/*
			 * decrement by the actual number of args 
			 * need to pass object as first arg: we may overflow the stack here
			 * until we use a different argument passing mechanism. 
			 * we shift the args to make room for the object reference
			 */
			for (pc = 0; pc < cmh->signature->param_count; ++pc)
				sp [-pc] = sp [-pc-1];
			sp -= cmh->signature->param_count + 1;
			sp->type = VAL_OBJ;
			sp->data.p = o;

			g_assert (cmh->signature->call_convention == MONO_CALL_DEFAULT);

			/*
			 * we need to truncate according to the type of args ...
			 */
			ves_exec_method (cmh, sp);
			/*
			 * a constructor returns void, but we need to return the object we created
			 */
			sp->type = VAL_OBJ;
			sp->data.p = o;
			++sp;
			BREAK;
		}
		
		CASE (CEE_CASTCLASS) ves_abort(); BREAK;
		CASE (CEE_ISINST) ves_abort(); BREAK;
		CASE (CEE_CONV_R_UN) ves_abort(); BREAK;
		CASE (CEE_UNUSED58) ves_abort(); BREAK;
		CASE (CEE_UNUSED1) ves_abort(); BREAK;
		CASE (CEE_UNBOX) ves_abort(); BREAK;
		CASE (CEE_THROW) ves_abort(); BREAK;
		CASE (CEE_LDFLD) {
			MonoObject *obj;
			MonoClassField *field;
			guint32 token;

			++ip;
			token = read32 (ip);
			ip += 4;
			
			g_assert (sp [-1].type == VAL_OBJ);
			obj = sp [-1].data.p;
			field = mono_class_get_field (obj->klass, token);
			g_assert (field);
			sp [-1] = stackval_from_data (field->type->type, (char*)obj, field->offset);
			BREAK;
		}
		CASE (CEE_LDFLDA) ves_abort(); BREAK;
		CASE (CEE_STFLD) {
			MonoObject *obj;
			MonoClassField *field;
			guint32 token;

			++ip;
			token = read32 (ip);
			ip += 4;
			
			sp -= 2;
			
			g_assert (sp [0].type == VAL_OBJ);
			obj = sp [0].data.p;
			field = mono_class_get_field (obj->klass, token);
			g_assert (field);
			stackval_to_data (field->type->type, &sp [1], (char*)obj, field->offset);
			BREAK;
		}
		CASE (CEE_LDSFLD) {
			MonoClass *klass;
			MonoClassField *field;
			guint32 token;

			++ip;
			token = read32 (ip);
			ip += 4;
			
			/* need to handle fieldrefs */
			klass = mono_class_get (mh->image, 
				MONO_TOKEN_TYPE_DEF | mono_metadata_typedef_from_field (&mh->image->metadata, token & 0xffffff));
			field = mono_class_get_field (klass, token);
			g_assert (field);
			*sp = stackval_from_data (field->type->type, (char*)klass, field->offset);
			++sp;
			BREAK;
		}
		CASE (CEE_LDSFLDA) ves_abort(); BREAK;
		CASE (CEE_STSFLD) {
			MonoClass *klass;
			MonoClassField *field;
			guint32 token;

			++ip;
			token = read32 (ip);
			ip += 4;
			--sp;

			/* need to handle fieldrefs */
			klass = mono_class_get (mh->image, 
				MONO_TOKEN_TYPE_DEF | mono_metadata_typedef_from_field (&mh->image->metadata, token & 0xffffff));
			field = mono_class_get_field (klass, token);
			g_assert (field);
			stackval_to_data (field->type->type, sp, (char*)klass, field->offset);
			BREAK;
		}
		CASE (CEE_STOBJ) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_I1_UN) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_I2_UN) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_I4_UN) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_I8_UN) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_U1_UN) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_U2_UN) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_U4_UN) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_U8_UN) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_I_UN) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_U_UN) ves_abort(); BREAK;
		CASE (CEE_BOX) ves_abort(); BREAK;
		CASE (CEE_NEWARR) ves_abort(); BREAK;
		CASE (CEE_LDLEN) ves_abort(); BREAK;
		CASE (CEE_LDELEMA) ves_abort(); BREAK;
		CASE (CEE_LDELEM_I1) ves_abort(); BREAK;
		CASE (CEE_LDELEM_U1) ves_abort(); BREAK;
		CASE (CEE_LDELEM_I2) ves_abort(); BREAK;
		CASE (CEE_LDELEM_U2) ves_abort(); BREAK;
		CASE (CEE_LDELEM_I4) ves_abort(); BREAK;
		CASE (CEE_LDELEM_U4) ves_abort(); BREAK;
		CASE (CEE_LDELEM_I8) ves_abort(); BREAK;
		CASE (CEE_LDELEM_I) ves_abort(); BREAK;
		CASE (CEE_LDELEM_R4) ves_abort(); BREAK;
		CASE (CEE_LDELEM_R8) ves_abort(); BREAK;
		CASE (CEE_LDELEM_REF) ves_abort(); BREAK;
		CASE (CEE_STELEM_I) ves_abort(); BREAK;
		CASE (CEE_STELEM_I1) ves_abort(); BREAK;
		CASE (CEE_STELEM_I2) ves_abort(); BREAK;
		CASE (CEE_STELEM_I4) ves_abort(); BREAK;
		CASE (CEE_STELEM_I8) ves_abort(); BREAK;
		CASE (CEE_STELEM_R4) ves_abort(); BREAK;
		CASE (CEE_STELEM_R8) ves_abort(); BREAK;
		CASE (CEE_STELEM_REF) ves_abort(); BREAK;
		CASE (CEE_UNUSED2) 
		CASE (CEE_UNUSED3) 
		CASE (CEE_UNUSED4) 
		CASE (CEE_UNUSED5) 
		CASE (CEE_UNUSED6) 
		CASE (CEE_UNUSED7) 
		CASE (CEE_UNUSED8) 
		CASE (CEE_UNUSED9) 
		CASE (CEE_UNUSED10) 
		CASE (CEE_UNUSED11) 
		CASE (CEE_UNUSED12) 
		CASE (CEE_UNUSED13) 
		CASE (CEE_UNUSED14) 
		CASE (CEE_UNUSED15) 
		CASE (CEE_UNUSED16) 
		CASE (CEE_UNUSED17) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_I1) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_U1) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_I2) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_U2) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_I4) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_U4) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_I8) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_U8) ves_abort(); BREAK;
		CASE (CEE_UNUSED50) 
		CASE (CEE_UNUSED18) 
		CASE (CEE_UNUSED19) 
		CASE (CEE_UNUSED20) 
		CASE (CEE_UNUSED21) 
		CASE (CEE_UNUSED22) 
		CASE (CEE_UNUSED23) ves_abort(); BREAK;
		CASE (CEE_REFANYVAL) ves_abort(); BREAK;
		CASE (CEE_CKFINITE) ves_abort(); BREAK;
		CASE (CEE_UNUSED24) ves_abort(); BREAK;
		CASE (CEE_UNUSED25) ves_abort(); BREAK;
		CASE (CEE_MKREFANY) ves_abort(); BREAK;
		CASE (CEE_UNUSED59) 
		CASE (CEE_UNUSED60) 
		CASE (CEE_UNUSED61) 
		CASE (CEE_UNUSED62) 
		CASE (CEE_UNUSED63) 
		CASE (CEE_UNUSED64) 
		CASE (CEE_UNUSED65) 
		CASE (CEE_UNUSED66) 
		CASE (CEE_UNUSED67) ves_abort(); BREAK;
		CASE (CEE_LDTOKEN) ves_abort(); BREAK;
		CASE (CEE_CONV_U2) ves_abort(); BREAK;
		CASE (CEE_CONV_U1) ves_abort(); BREAK;
		CASE (CEE_CONV_I) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_I) ves_abort(); BREAK;
		CASE (CEE_CONV_OVF_U) ves_abort(); BREAK;
		CASE (CEE_ADD_OVF) ves_abort(); BREAK;
		CASE (CEE_ADD_OVF_UN) ves_abort(); BREAK;
		CASE (CEE_MUL_OVF) ves_abort(); BREAK;
		CASE (CEE_MUL_OVF_UN) ves_abort(); BREAK;
		CASE (CEE_SUB_OVF) ves_abort(); BREAK;
		CASE (CEE_SUB_OVF_UN) ves_abort(); BREAK;
		CASE (CEE_ENDFINALLY) ves_abort(); BREAK;
		CASE (CEE_LEAVE) ves_abort(); BREAK;
		CASE (CEE_LEAVE_S) ves_abort(); BREAK;
		CASE (CEE_STIND_I) ves_abort(); BREAK;
		CASE (CEE_CONV_U) ves_abort(); BREAK;
		CASE (CEE_UNUSED26) 
		CASE (CEE_UNUSED27) 
		CASE (CEE_UNUSED28) 
		CASE (CEE_UNUSED29) 
		CASE (CEE_UNUSED30) 
		CASE (CEE_UNUSED31) 
		CASE (CEE_UNUSED32) 
		CASE (CEE_UNUSED33) 
		CASE (CEE_UNUSED34) 
		CASE (CEE_UNUSED35) 
		CASE (CEE_UNUSED36) 
		CASE (CEE_UNUSED37) 
		CASE (CEE_UNUSED38) 
		CASE (CEE_UNUSED39) 
		CASE (CEE_UNUSED40) 
		CASE (CEE_UNUSED41) 
		CASE (CEE_UNUSED42) 
		CASE (CEE_UNUSED43) 
		CASE (CEE_UNUSED44) 
		CASE (CEE_UNUSED45) 
		CASE (CEE_UNUSED46) 
		CASE (CEE_UNUSED47) 
		CASE (CEE_UNUSED48) ves_abort(); BREAK;
		CASE (CEE_PREFIX7) ves_abort(); BREAK;
		CASE (CEE_PREFIX6) ves_abort(); BREAK;
		CASE (CEE_PREFIX5) ves_abort(); BREAK;
		CASE (CEE_PREFIX4) ves_abort(); BREAK;
		CASE (CEE_PREFIX3) ves_abort(); BREAK;
		CASE (CEE_PREFIX2) ves_abort(); BREAK;
		CASE (CEE_PREFIXREF) ves_abort(); BREAK;
		SUB_SWITCH
			++ip;
			switch (*ip) {
			case CEE_ARGLIST: ves_abort(); break;
			case CEE_CEQ: ves_abort(); break;
			case CEE_CGT: ves_abort(); break;
			case CEE_CGT_UN: ves_abort(); break;
			case CEE_CLT: ves_abort(); break;
			case CEE_CLT_UN: ves_abort(); break;
			case CEE_LDFTN: ves_abort(); break;
			case CEE_LDVIRTFTN: ves_abort(); break;
			case CEE_UNUSED56: ves_abort(); break;
			case CEE_LDARG: ves_abort(); break;
			case CEE_LDARGA: ves_abort(); break;
			case CEE_STARG: ves_abort(); break;
			case CEE_LDLOC: ves_abort(); break;
			case CEE_LDLOCA: ves_abort(); break;
			case CEE_STLOC: ves_abort(); break;
			case CEE_LOCALLOC: ves_abort(); break;
			case CEE_UNUSED57: ves_abort(); break;
			case CEE_ENDFILTER: ves_abort(); break;
			case CEE_UNALIGNED_: ves_abort(); break;
			case CEE_VOLATILE_: ves_abort(); break;
			case CEE_TAIL_: ves_abort(); break;
			case CEE_INITOBJ: ves_abort(); break;
			case CEE_UNUSED68: ves_abort(); break;
			case CEE_CPBLK: ves_abort(); break;
			case CEE_INITBLK: ves_abort(); break;
			case CEE_UNUSED69: ves_abort(); break;
			case CEE_RETHROW: ves_abort(); break;
			case CEE_UNUSED: ves_abort(); break;
			case CEE_SIZEOF: ves_abort(); break;
			case CEE_REFANYTYPE: ves_abort(); break;
			case CEE_UNUSED52: 
			case CEE_UNUSED53: 
			case CEE_UNUSED54: 
			case CEE_UNUSED55: 
			case CEE_UNUSED70: 
			default:
				g_error ("Unimplemented opcode: 0xFE %02x at 0x%x\n", *ip, ip-(unsigned char*)mm->header->code);
			}
			continue;
		DEFAULT;
		}
	}

	g_assert_not_reached ();
}

static int 
ves_exec (MonoAssembly *assembly)
{
	MonoImage *image = assembly->image;
	MonoCLIImageInfo *iinfo;
	stackval result;
	MonoMethod *mh;

	iinfo = image->image_info;
	
	/* we need to exec the class and object constructors... */
	mh = mono_get_method (image, iinfo->cli_cli_header.ch_entry_point);
	ves_exec_method (mh, &result);
	fprintf (stderr, "result: %d\n", result.data.i);
	mono_free_method (mh);

	return result.data.i;
}

static void
usage (void)
{
	fprintf (stderr, "Usage is: mono-int executable args...");
	exit (1);
}

int 
main (int argc, char *argv [])
{
	MonoAssembly *assembly;
	int retval = 0;
	char *file;

	if (argc < 2)
		usage ();

	file = argv [1];

	assembly = mono_assembly_open (file, NULL, NULL);
	if (!assembly){
		fprintf (stderr, "Can not open image %s\n", file);
		exit (1);
	}
	retval = ves_exec (assembly);
	mono_assembly_close (assembly);

	return retval;
}



