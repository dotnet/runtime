/*
 * PLEASE NOTE: This is a research prototype.
 *
 *
 * interp.c: Interpreter for CIL byte codes
 *
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */
#include <config.h>
#include <stdio.h>
#include <string.h>
#include <glib.h>

#ifdef HAVE_ALLOCA_H
#   include <alloca.h>
#else
#   ifdef __CYGWIN__
#      define alloca __builtin_alloca
#   endif
#endif

#include "interp.h"
/* trim excessive headers */
#include <mono/metadata/assembly.h>
#include <mono/metadata/cil-coff.h>
#include <mono/metadata/endian.h>
#include <mono/metadata/typeattr.h>
#include <mono/metadata/fieldattr.h>
#include <mono/metadata/methodattr.h>
#include <mono/metadata/eltype.h>
#include <mono/metadata/blobsig.h>
#include <mono/metadata/paramattr.h>

#define OPDEF(a,b,c,d,e,f,g,h,i,j) \
	a = i,

enum {
#include "mono/cil/opcode.def"
	LAST = 0xff
};
#undef OPDEF

/* this needs to be metadata,token indexed, not only token */
static GHashTable * method_cache = 0;

/* FIXME: check in configure */
typedef gint32 nati_t;

#define GET_NATI(sp) ((guint32)(sp).data.i)

static int count = 0;

/*
 * Attempt at using the goto label construct of GNU GCC:
 * it turns out this does give some benefit: 5-15% speedup.
 * Don't look at these macros, it hurts...
 */
#define GOTO_LABEL
#ifdef GOTO_LABEL
#define SWITCH(a) goto *goto_map [(a)];
#define BREAK SWITCH(*ip)
#define CASE(l)	l ## _LABEL:
#define SUB_SWITCH \
	CEE_PREFIX1_LABEL: \
	CEE_ARGLIST_LABEL: \
	CEE_CEQ_LABEL: \
	CEE_CGT_LABEL: \
	CEE_CGT_UN_LABEL: \
	CEE_CLT_LABEL: \
	CEE_CLT_UN_LABEL: \
	CEE_LDFTN_LABEL: \
	CEE_LDVIRTFTN_LABEL: \
	CEE_UNUSED56_LABEL: \
	CEE_LDARG_LABEL: \
	CEE_LDARGA_LABEL: \
	CEE_STARG_LABEL: \
	CEE_LDLOC_LABEL: \
	CEE_LDLOCA_LABEL: \
	CEE_STLOC_LABEL: \
	CEE_LOCALLOC_LABEL: \
	CEE_UNUSED57_LABEL: \
	CEE_ENDFILTER_LABEL: \
	CEE_UNALIGNED__LABEL: \
	CEE_VOLATILE__LABEL: \
	CEE_TAIL__LABEL: \
	CEE_INITOBJ_LABEL: \
	CEE_UNUSED68_LABEL: \
	CEE_CPBLK_LABEL: \
	CEE_INITBLK_LABEL: \
	CEE_UNUSED69_LABEL: \
	CEE_RETHROW_LABEL: \
	CEE_UNUSED_LABEL: \
	CEE_SIZEOF_LABEL: \
	CEE_REFANYTYPE_LABEL: \
	CEE_UNUSED52_LABEL: \
	CEE_UNUSED53_LABEL: \
	CEE_UNUSED54_LABEL: \
	CEE_UNUSED55_LABEL: \
	CEE_UNUSED70_LABEL:
#else
#define SWITCH(a) switch(a)
#define BREAK	break
#define CASE(l)	case l:
#define SUB_SWITCH case 0xFE:
#endif

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
void 
ves_exec_method (cli_image_info_t *iinfo, MonoMethod *mh, stackval *args)
{
	/*
	 * with alloca we get the expected huge performance gain
	 * stackval *stack = g_new0(stackval, mh->max_stack);
	 */
	stackval *stack = alloca (sizeof (stackval) * mh->header->max_stack);
	register const unsigned char *ip = mh->header->code;
	register stackval *sp = stack;
	/* FIXME: remove this hack */
	static int fake_field = 42;
	stackval *locals;

#ifdef GOTO_LABEL
	const static void * const goto_map [] = {
#define OPDEF(a,b,c,d,e,f,g,h,i,j) \
	&& a ## _LABEL,
#include "mono/cil/opcode.def"
#undef OPDEF
	&&START
	};
#endif

	if (mh->header->num_locals)
		locals = alloca (sizeof (stackval) * mh->header->num_locals);

	/*
	 * using while (ip < end) may result in a 15% performance drop, 
	 * but it may be useful for debug
	 */
	while (1) {
		/*count++;*/
#ifdef GOTO_LABEL
		START:
#endif
		/*g_print ("0x%04x %02x\n", ip-(unsigned char*)mh->header->code, *ip);
		if (sp > stack)
				printf ("\t[%d] %d 0x%08x %0.5f\n", sp-stack, sp[-1].type, sp[-1].data.i, sp[-1].data.f);
		*/
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
		CASE (CEE_LDLOC_3)
			*sp = locals [(*ip)-CEE_LDLOC_0];
			++ip;
			++sp;
			BREAK;
		CASE (CEE_STLOC_0)
		CASE (CEE_STLOC_1)
		CASE (CEE_STLOC_2)
		CASE (CEE_STLOC_3)
			--sp;
			locals [(*ip)-CEE_STLOC_0] = *sp;
			++ip;
			BREAK;
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
		CASE (CEE_STARG_S) g_assert_not_reached(); BREAK;
		CASE (CEE_LDLOC_S)
			++ip;
			*sp = locals [*ip];
			++sp;
			++ip;
			BREAK;
		CASE (CEE_LDLOCA_S) g_assert_not_reached(); BREAK;
		CASE (CEE_STLOC_S)
			++ip;
			--sp;
			locals [*ip] = *sp;
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
		CASE (CEE_UNUSED99) g_assert_not_reached (); BREAK;
		CASE (CEE_DUP) 
			*sp = sp [-1]; 
			++sp; 
			++ip; 
			BREAK;
		CASE (CEE_POP)
			++ip;
			--sp;
			BREAK;
		CASE (CEE_JMP) g_assert_not_reached(); BREAK;
		CASE (CEE_CALL) {
			MonoMethod *cmh;
			guint32 token;

			++ip;
			token = read32 (ip);
			ip += 4;
			if (!(cmh = g_hash_table_lookup (method_cache, GINT_TO_POINTER (token)))) {
				cmh = mono_get_method (iinfo, token);
				g_hash_table_insert (method_cache, GINT_TO_POINTER (token), cmh);
			}

			/* decrement by the actual number of args */
			sp -= cmh->signature->param_count;
			g_assert (cmh->signature->call_convention == MONO_CALL_DEFAULT);

			/* we need to truncate according to the type of args ... */
			ves_exec_method (iinfo, cmh, sp);

			/* need to handle typedbyref ... */
			if (cmh->signature->ret->type)
				sp++;
			BREAK;
		}
		CASE (CEE_CALLI) g_assert_not_reached(); BREAK;
		CASE (CEE_RET)
			--sp;
			*args = *sp;
			if (sp != stack)
				g_warning ("more values on stack: %d", sp-stack);

			/*if (sp->type == VAL_DOUBLE)
					g_print("%.9f\n", sp->data.f);*/
			/*g_free (stack);*/
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
		CASE (CEE_BNE_UN_S) g_assert_not_reached(); BREAK;
		CASE (CEE_BGE_UN_S) g_assert_not_reached(); BREAK;
		CASE (CEE_BGT_UN_S) g_assert_not_reached(); BREAK;
		CASE (CEE_BLE_UN_S) g_assert_not_reached(); BREAK;
		CASE (CEE_BLT_UN_S) g_assert_not_reached(); BREAK;
		CASE (CEE_BR) g_assert_not_reached(); BREAK;
		CASE (CEE_BRFALSE) g_assert_not_reached(); BREAK;
		CASE (CEE_BRTRUE) g_assert_not_reached(); BREAK;
		CASE (CEE_BEQ) g_assert_not_reached(); BREAK;
		CASE (CEE_BGE) g_assert_not_reached(); BREAK;
		CASE (CEE_BGT) g_assert_not_reached(); BREAK;
		CASE (CEE_BLE) g_assert_not_reached(); BREAK;
		CASE (CEE_BLT) g_assert_not_reached(); BREAK;
		CASE (CEE_BNE_UN) g_assert_not_reached(); BREAK;
		CASE (CEE_BGE_UN) g_assert_not_reached(); BREAK;
		CASE (CEE_BGT_UN) g_assert_not_reached(); BREAK;
		CASE (CEE_BLE_UN) g_assert_not_reached(); BREAK;
		CASE (CEE_BLT_UN) g_assert_not_reached(); BREAK;
		CASE (CEE_SWITCH) g_assert_not_reached(); BREAK;
		CASE (CEE_LDIND_I1) g_assert_not_reached(); BREAK;
		CASE (CEE_LDIND_U1) g_assert_not_reached(); BREAK;
		CASE (CEE_LDIND_I2) g_assert_not_reached(); BREAK;
		CASE (CEE_LDIND_U2) g_assert_not_reached(); BREAK;
		CASE (CEE_LDIND_I4) g_assert_not_reached(); BREAK;
		CASE (CEE_LDIND_U4) g_assert_not_reached(); BREAK;
		CASE (CEE_LDIND_I8) g_assert_not_reached(); BREAK;
		CASE (CEE_LDIND_I) g_assert_not_reached(); BREAK;
		CASE (CEE_LDIND_R4) g_assert_not_reached(); BREAK;
		CASE (CEE_LDIND_R8) g_assert_not_reached(); BREAK;
		CASE (CEE_LDIND_REF) g_assert_not_reached(); BREAK;
		CASE (CEE_STIND_REF) g_assert_not_reached(); BREAK;
		CASE (CEE_STIND_I1) g_assert_not_reached(); BREAK;
		CASE (CEE_STIND_I2) g_assert_not_reached(); BREAK;
		CASE (CEE_STIND_I4) g_assert_not_reached(); BREAK;
		CASE (CEE_STIND_I8) g_assert_not_reached(); BREAK;
		CASE (CEE_STIND_R4) g_assert_not_reached(); BREAK;
		CASE (CEE_STIND_R8) g_assert_not_reached(); BREAK;
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
		CASE (CEE_REM_UN) g_assert_not_reached(); BREAK;
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
		CASE (CEE_SHR_UN) g_assert_not_reached(); BREAK;
		CASE (CEE_NEG)
			++ip;
			if (sp->type == VAL_I32)
				sp->data.i = - sp->data.i;
			else if (sp->type == VAL_I64)
				sp->data.l = - sp->data.l;
			else if (sp->type == VAL_DOUBLE)
				sp->data.f = - sp->data.f;
			else if (sp->type == VAL_NATI)
				sp->data.p = (gpointer)(- (nati_t)sp->data.p);
			BREAK;
		CASE (CEE_NOT)
			++ip;
			if (sp->type == VAL_I32)
				sp->data.i = ~ sp->data.i;
			else if (sp->type == VAL_I64)
				sp->data.l = ~ sp->data.l;
			else if (sp->type == VAL_NATI)
				sp->data.p = (gpointer)(~ (nati_t)sp->data.p);
			BREAK;
		CASE (CEE_CONV_I1) g_assert_not_reached(); BREAK;
		CASE (CEE_CONV_I2) g_assert_not_reached(); BREAK;
		CASE (CEE_CONV_I4)
			++ip;
			/* FIXME: handle other cases. what about sign? */
			if (sp [-1].type == VAL_DOUBLE) {
				sp [-1].data.i = (gint32)sp [-1].data.f;
				sp [-1].type = VAL_I32;
			} else {
				g_assert_not_reached();
			}
			BREAK;
		CASE (CEE_CONV_I8) g_assert_not_reached(); BREAK;
		CASE (CEE_CONV_R4) g_assert_not_reached(); BREAK;
		CASE (CEE_CONV_R8)
			++ip;
			/* FIXME: handle other cases. what about sign? */
			if (sp [-1].type == VAL_I32) {
				sp [-1].data.f = (double)sp [-1].data.i;
				sp [-1].type = VAL_DOUBLE;
			} else {
				g_assert_not_reached();
			}
			BREAK;
		CASE (CEE_CONV_U4)
			++ip;
			/* FIXME: handle other cases. what about sign? */
			if (sp [-1].type == VAL_DOUBLE) {
				sp [-1].data.i = (guint32)sp [-1].data.f;
				sp [-1].type = VAL_I32;
			} else {
				g_assert_not_reached();
			}
			BREAK;
		CASE (CEE_CONV_U8) g_assert_not_reached(); BREAK;
		CASE (CEE_CALLVIRT) g_assert_not_reached(); BREAK;
		CASE (CEE_CPOBJ) g_assert_not_reached(); BREAK;
		CASE (CEE_LDOBJ) g_assert_not_reached(); BREAK;
		CASE (CEE_LDSTR) g_assert_not_reached(); BREAK;
		CASE (CEE_NEWOBJ) g_assert_not_reached(); BREAK;
		CASE (CEE_CASTCLASS) g_assert_not_reached(); BREAK;
		CASE (CEE_ISINST) g_assert_not_reached(); BREAK;
		CASE (CEE_CONV_R_UN) g_assert_not_reached(); BREAK;
		CASE (CEE_UNUSED58) g_assert_not_reached(); BREAK;
		CASE (CEE_UNUSED1) g_assert_not_reached(); BREAK;
		CASE (CEE_UNBOX) g_assert_not_reached(); BREAK;
		CASE (CEE_THROW) g_assert_not_reached(); BREAK;
		CASE (CEE_LDFLD) g_assert_not_reached(); BREAK;
		CASE (CEE_LDFLDA) g_assert_not_reached(); BREAK;
		CASE (CEE_STFLD) g_assert_not_reached(); BREAK;
		CASE (CEE_LDSFLD)
			/* FIXME: get the real field here */
			ip += 5;
			sp->type = VAL_I32;
			sp->data.i = fake_field;
			++sp;
			BREAK;
		CASE (CEE_LDSFLDA) g_assert_not_reached(); BREAK;
		CASE (CEE_STSFLD)
			/* FIXME: get the real field here */
			ip += 5;
			--sp;
			fake_field = sp->data.i;
			BREAK;
		CASE (CEE_STOBJ) g_assert_not_reached(); BREAK;
		CASE (CEE_CONV_OVF_I1_UN) g_assert_not_reached(); BREAK;
		CASE (CEE_CONV_OVF_I2_UN) g_assert_not_reached(); BREAK;
		CASE (CEE_CONV_OVF_I4_UN) g_assert_not_reached(); BREAK;
		CASE (CEE_CONV_OVF_I8_UN) g_assert_not_reached(); BREAK;
		CASE (CEE_CONV_OVF_U1_UN) g_assert_not_reached(); BREAK;
		CASE (CEE_CONV_OVF_U2_UN) g_assert_not_reached(); BREAK;
		CASE (CEE_CONV_OVF_U4_UN) g_assert_not_reached(); BREAK;
		CASE (CEE_CONV_OVF_U8_UN) g_assert_not_reached(); BREAK;
		CASE (CEE_CONV_OVF_I_UN) g_assert_not_reached(); BREAK;
		CASE (CEE_CONV_OVF_U_UN) g_assert_not_reached(); BREAK;
		CASE (CEE_BOX) g_assert_not_reached(); BREAK;
		CASE (CEE_NEWARR) g_assert_not_reached(); BREAK;
		CASE (CEE_LDLEN) g_assert_not_reached(); BREAK;
		CASE (CEE_LDELEMA) g_assert_not_reached(); BREAK;
		CASE (CEE_LDELEM_I1) g_assert_not_reached(); BREAK;
		CASE (CEE_LDELEM_U1) g_assert_not_reached(); BREAK;
		CASE (CEE_LDELEM_I2) g_assert_not_reached(); BREAK;
		CASE (CEE_LDELEM_U2) g_assert_not_reached(); BREAK;
		CASE (CEE_LDELEM_I4) g_assert_not_reached(); BREAK;
		CASE (CEE_LDELEM_U4) g_assert_not_reached(); BREAK;
		CASE (CEE_LDELEM_I8) g_assert_not_reached(); BREAK;
		CASE (CEE_LDELEM_I) g_assert_not_reached(); BREAK;
		CASE (CEE_LDELEM_R4) g_assert_not_reached(); BREAK;
		CASE (CEE_LDELEM_R8) g_assert_not_reached(); BREAK;
		CASE (CEE_LDELEM_REF) g_assert_not_reached(); BREAK;
		CASE (CEE_STELEM_I) g_assert_not_reached(); BREAK;
		CASE (CEE_STELEM_I1) g_assert_not_reached(); BREAK;
		CASE (CEE_STELEM_I2) g_assert_not_reached(); BREAK;
		CASE (CEE_STELEM_I4) g_assert_not_reached(); BREAK;
		CASE (CEE_STELEM_I8) g_assert_not_reached(); BREAK;
		CASE (CEE_STELEM_R4) g_assert_not_reached(); BREAK;
		CASE (CEE_STELEM_R8) g_assert_not_reached(); BREAK;
		CASE (CEE_STELEM_REF) g_assert_not_reached(); BREAK;
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
		CASE (CEE_UNUSED17) g_assert_not_reached(); BREAK;
		CASE (CEE_CONV_OVF_I1) g_assert_not_reached(); BREAK;
		CASE (CEE_CONV_OVF_U1) g_assert_not_reached(); BREAK;
		CASE (CEE_CONV_OVF_I2) g_assert_not_reached(); BREAK;
		CASE (CEE_CONV_OVF_U2) g_assert_not_reached(); BREAK;
		CASE (CEE_CONV_OVF_I4) g_assert_not_reached(); BREAK;
		CASE (CEE_CONV_OVF_U4) g_assert_not_reached(); BREAK;
		CASE (CEE_CONV_OVF_I8) g_assert_not_reached(); BREAK;
		CASE (CEE_CONV_OVF_U8) g_assert_not_reached(); BREAK;
		CASE (CEE_UNUSED50) 
		CASE (CEE_UNUSED18) 
		CASE (CEE_UNUSED19) 
		CASE (CEE_UNUSED20) 
		CASE (CEE_UNUSED21) 
		CASE (CEE_UNUSED22) 
		CASE (CEE_UNUSED23) g_assert_not_reached(); BREAK;
		CASE (CEE_REFANYVAL) g_assert_not_reached(); BREAK;
		CASE (CEE_CKFINITE) g_assert_not_reached(); BREAK;
		CASE (CEE_UNUSED24) g_assert_not_reached(); BREAK;
		CASE (CEE_UNUSED25) g_assert_not_reached(); BREAK;
		CASE (CEE_MKREFANY) g_assert_not_reached(); BREAK;
		CASE (CEE_UNUSED59) 
		CASE (CEE_UNUSED60) 
		CASE (CEE_UNUSED61) 
		CASE (CEE_UNUSED62) 
		CASE (CEE_UNUSED63) 
		CASE (CEE_UNUSED64) 
		CASE (CEE_UNUSED65) 
		CASE (CEE_UNUSED66) 
		CASE (CEE_UNUSED67) g_assert_not_reached(); BREAK;
		CASE (CEE_LDTOKEN) g_assert_not_reached(); BREAK;
		CASE (CEE_CONV_U2) g_assert_not_reached(); BREAK;
		CASE (CEE_CONV_U1) g_assert_not_reached(); BREAK;
		CASE (CEE_CONV_I) g_assert_not_reached(); BREAK;
		CASE (CEE_CONV_OVF_I) g_assert_not_reached(); BREAK;
		CASE (CEE_CONV_OVF_U) g_assert_not_reached(); BREAK;
		CASE (CEE_ADD_OVF) g_assert_not_reached(); BREAK;
		CASE (CEE_ADD_OVF_UN) g_assert_not_reached(); BREAK;
		CASE (CEE_MUL_OVF) g_assert_not_reached(); BREAK;
		CASE (CEE_MUL_OVF_UN) g_assert_not_reached(); BREAK;
		CASE (CEE_SUB_OVF) g_assert_not_reached(); BREAK;
		CASE (CEE_SUB_OVF_UN) g_assert_not_reached(); BREAK;
		CASE (CEE_ENDFINALLY) g_assert_not_reached(); BREAK;
		CASE (CEE_LEAVE) g_assert_not_reached(); BREAK;
		CASE (CEE_LEAVE_S) g_assert_not_reached(); BREAK;
		CASE (CEE_STIND_I) g_assert_not_reached(); BREAK;
		CASE (CEE_CONV_U) g_assert_not_reached(); BREAK;
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
		CASE (CEE_UNUSED48) g_assert_not_reached(); BREAK;
		CASE (CEE_PREFIX7) g_assert_not_reached(); BREAK;
		CASE (CEE_PREFIX6) g_assert_not_reached(); BREAK;
		CASE (CEE_PREFIX5) g_assert_not_reached(); BREAK;
		CASE (CEE_PREFIX4) g_assert_not_reached(); BREAK;
		CASE (CEE_PREFIX3) g_assert_not_reached(); BREAK;
		CASE (CEE_PREFIX2) g_assert_not_reached(); BREAK;
		CASE (CEE_PREFIXREF) g_assert_not_reached(); BREAK;
		SUB_SWITCH
			++ip;
			switch (*ip) {
			case CEE_ARGLIST: g_assert_not_reached(); break;
			case CEE_CEQ: g_assert_not_reached(); break;
			case CEE_CGT: g_assert_not_reached(); break;
			case CEE_CGT_UN: g_assert_not_reached(); break;
			case CEE_CLT: g_assert_not_reached(); break;
			case CEE_CLT_UN: g_assert_not_reached(); break;
			case CEE_LDFTN: g_assert_not_reached(); break;
			case CEE_LDVIRTFTN: g_assert_not_reached(); break;
			case CEE_UNUSED56: g_assert_not_reached(); break;
			case CEE_LDARG: g_assert_not_reached(); break;
			case CEE_LDARGA: g_assert_not_reached(); break;
			case CEE_STARG: g_assert_not_reached(); break;
			case CEE_LDLOC: g_assert_not_reached(); break;
			case CEE_LDLOCA: g_assert_not_reached(); break;
			case CEE_STLOC: g_assert_not_reached(); break;
			case CEE_LOCALLOC: g_assert_not_reached(); break;
			case CEE_UNUSED57: g_assert_not_reached(); break;
			case CEE_ENDFILTER: g_assert_not_reached(); break;
			case CEE_UNALIGNED_: g_assert_not_reached(); break;
			case CEE_VOLATILE_: g_assert_not_reached(); break;
			case CEE_TAIL_: g_assert_not_reached(); break;
			case CEE_INITOBJ: g_assert_not_reached(); break;
			case CEE_UNUSED68: g_assert_not_reached(); break;
			case CEE_CPBLK: g_assert_not_reached(); break;
			case CEE_INITBLK: g_assert_not_reached(); break;
			case CEE_UNUSED69: g_assert_not_reached(); break;
			case CEE_RETHROW: g_assert_not_reached(); break;
			case CEE_UNUSED: g_assert_not_reached(); break;
			case CEE_SIZEOF: g_assert_not_reached(); break;
			case CEE_REFANYTYPE: g_assert_not_reached(); break;
			case CEE_UNUSED52: 
			case CEE_UNUSED53: 
			case CEE_UNUSED54: 
			case CEE_UNUSED55: 
			case CEE_UNUSED70: 
			default:
#ifdef GOTO_LABEL
			CEE_ILLEGAL_LABEL:
			CEE_ENDMAC_LABEL:
#endif
				g_error ("Unimplemented opcode: 0xFE %02x at 0x%x\n", *ip, ip-(unsigned char*)mh->header->code);
			}
			continue;
#ifndef GOTO_LABEL
		default:
			g_error ("Unimplemented opcode: %x at 0x%x\n", *ip, ip-(unsigned char*)mh->header->code);
#endif
		}
	}

	g_assert_not_reached();
}

static int 
ves_exec (cli_image_info_t *iinfo)
{
	stackval result;
	MonoMethod *mh;

	/* we need to exec the class and object constructors... */
	method_cache = g_hash_table_new (g_direct_hash, g_direct_equal);

	mh = mono_get_method (iinfo, iinfo->cli_cli_header.ch_entry_point);
	ves_exec_method (iinfo, mh, &result);
	fprintf (stderr, "result: %d\n", result.data.i);
	mono_free_method (mh);

	return 0;
}

int 
main (int argc, char *argv [])
{
	cli_image_info_t *iinfo;
	MonoAssembly *assembly;
	int retval = 0;
	char *file = argv [1];

	assembly = mono_assembly_open (file, NULL);
	if (!assembly){
		fprintf (stderr, "Can not open assembly %s\n", file);
		exit (1);
	}
	iinfo = assembly->image_info;
	retval = ves_exec (iinfo);
	mono_assembly_close (assembly);
	printf("count: %d\n", count);

	return retval;
}



