/*
 * jit-icalls.c: internal calls used by the JIT
 *
 * Author:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#include <math.h>

#include "jit-icalls.h"

void*
mono_ldftn (MonoMethod *method)
{
	gpointer addr;

	MONO_ARCH_SAVE_REGS;

	addr = mono_create_jump_trampoline (mono_domain_get (), method, TRUE);

	return mono_create_ftnptr (mono_domain_get (), addr);
}

/*
 * Same as mono_ldftn, but do not add a synchronized wrapper. Used in the
 * synchronized wrappers to avoid infinite recursion.
 */
void*
mono_ldftn_nosync (MonoMethod *method)
{
	gpointer addr;

	MONO_ARCH_SAVE_REGS;

	addr = mono_create_jump_trampoline (mono_domain_get (), method, FALSE);

	return mono_create_ftnptr (mono_domain_get (), addr);
}

void*
mono_ldvirtfn (MonoObject *obj, MonoMethod *method) 
{
	MONO_ARCH_SAVE_REGS;

	if (obj == NULL)
		mono_raise_exception (mono_get_exception_null_reference ());

	method = mono_object_get_virtual_method (obj, method);

	return mono_ldftn (method);
}

void
mono_helper_stelem_ref_check (MonoArray *array, MonoObject *val)
{
	MONO_ARCH_SAVE_REGS;

	if (val && !mono_object_isinst (val, array->obj.vtable->klass->element_class))
		mono_raise_exception (mono_get_exception_array_type_mismatch ());
}

#ifndef MONO_ARCH_NO_EMULATE_LONG_MUL_OPTS

gint64 
mono_llmult (gint64 a, gint64 b)
{
	/* no need, no exceptions: MONO_ARCH_SAVE_REGS;*/
	return a * b;
}

guint64  
mono_llmult_ovf_un (guint64 a, guint64 b)
{
	guint32 al = a;
	guint32 ah = a >> 32;
	guint32 bl = b;
	guint32 bh = b >> 32; 
	guint64 res, t1;

	MONO_ARCH_SAVE_REGS;

	// fixme: this is incredible slow

	if (ah && bh)
		goto raise_exception;

	res = (guint64)al * (guint64)bl;

	t1 = (guint64)ah * (guint64)bl + (guint64)al * (guint64)bh;

	if (t1 > 0xffffffff)
		goto raise_exception;

	res += ((guint64)t1) << 32; 

	return res;

 raise_exception:
	mono_raise_exception (mono_get_exception_overflow ());
	return 0;
}

guint64  
mono_llmult_ovf (gint64 a, gint64 b) 
{
	guint32 al = a;
	gint32 ah = a >> 32;
	guint32 bl = b;
	gint32 bh = b >> 32; 
	/*
	Use Karatsuba algorithm where:
		a*b is: AhBh(R^2+R)+(Ah-Al)(Bl-Bh)R+AlBl(R+1)
		where Ah is the "high half" (most significant 32 bits) of a and
		where Al is the "low half" (least significant 32 bits) of a and
		where  Bh is the "high half" of b and Bl is the "low half" and
		where R is the Radix or "size of the half" (in our case 32 bits)

	Note, for the product of two 64 bit numbers to fit into a 64
	result, ah and/or bh must be 0.  This will save us from doing
	the AhBh term at all.

	Also note that we refactor so that we don't overflow 64 bits with 
	intermediate results. So we use [(Ah-Al)(Bl-Bh)+AlBl]R+AlBl
	*/

	gint64 res, t1;
	gint32 sign;

	MONO_ARCH_SAVE_REGS;

	/* need to work with absoulte values, so find out what the
	   resulting sign will be and convert any negative numbers
	   from two's complement
	*/
	sign = ah ^ bh;
	if (ah < 0) {
		if (((guint32)ah == 0x80000000) && (al == 0)) {
			/* This has no two's complement */
			if (b == 0)
				return 0;
			else if (b == 1)
				return a;
			else
				goto raise_exception;
		}

		/* flip the bits and add 1 */
		ah ^= ~0;
		if (al ==  0)
			ah += 1;
		else {
			al ^= ~0;
			al +=1;
		}
	}

	if (bh < 0) {
		if (((guint32)bh == 0x80000000) && (bl == 0)) {
			/* This has no two's complement */
			if (a == 0)
				return 0;
			else if (a == 1)
				return b;
			else
				goto raise_exception;
		}

		/* flip the bits and add 1 */
		bh ^= ~0;
		if (bl ==  0)
			bh += 1;
		else {
			bl ^= ~0;
			bl +=1;
		}
	}
		
	/* we overflow for sure if both upper halves are greater 
	   than zero because we would need to shift their 
	   product 64 bits to the left and that will not fit
	   in a 64 bit result */
	if (ah && bh)
		goto raise_exception;
	if ((gint64)((gint64)ah * (gint64)bl) > (gint64)0x80000000 || (gint64)((gint64)al * (gint64)bh) > (gint64)0x80000000)
		goto raise_exception;

	/* do the AlBl term first */
	t1 = (gint64)al * (gint64)bl;

	res = t1;

	/* now do the [(Ah-Al)(Bl-Bh)+AlBl]R term */
	t1 += (gint64)(ah - al) * (gint64)(bl - bh);
	/* check for overflow */
	t1 <<= 32;
	if (t1 > (0x7FFFFFFFFFFFFFFFLL - res))
		goto raise_exception;

	res += t1;

	if (res < 0)
		goto raise_exception;

	if (sign < 0)
		return -res;
	else
		return res;

 raise_exception:
	mono_raise_exception (mono_get_exception_overflow ());
	return 0;
}

#if defined(MONO_ARCH_EMULATE_MUL_DIV) || defined(MONO_ARCH_EMULATE_DIV)

gint32
mono_idiv (gint32 a, gint32 b)
{
	MONO_ARCH_SAVE_REGS;

#ifdef MONO_ARCH_NEED_DIV_CHECK
	if (!b)
		mono_raise_exception (mono_get_exception_divide_by_zero ());
	else if (b == -1 && a == (0x80000000))
		mono_raise_exception (mono_get_exception_arithmetic ());
#endif
	return a / b;
}

guint32
mono_idiv_un (guint32 a, guint32 b)
{
	MONO_ARCH_SAVE_REGS;

#ifdef MONO_ARCH_NEED_DIV_CHECK
	if (!b)
		mono_raise_exception (mono_get_exception_divide_by_zero ());
#endif
	return a / b;
}

gint32
mono_irem (gint32 a, gint32 b)
{
	MONO_ARCH_SAVE_REGS;

#ifdef MONO_ARCH_NEED_DIV_CHECK
	if (!b)
		mono_raise_exception (mono_get_exception_divide_by_zero ());
	else if (b == -1 && a == (0x80000000))
		mono_raise_exception (mono_get_exception_arithmetic ());
#endif

	return a % b;
}

guint32
mono_irem_un (guint32 a, guint32 b)
{
	MONO_ARCH_SAVE_REGS;

#ifdef MONO_ARCH_NEED_DIV_CHECK
	if (!b)
		mono_raise_exception (mono_get_exception_divide_by_zero ());
#endif
	return a % b;
}

#endif

#ifdef MONO_ARCH_EMULATE_MUL_DIV

gint32
mono_imul (gint32 a, gint32 b)
{
	MONO_ARCH_SAVE_REGS;

	return a * b;
}

gint32
mono_imul_ovf (gint32 a, gint32 b)
{
	gint64 res;

	MONO_ARCH_SAVE_REGS;

	res = (gint64)a * (gint64)b;

	if ((res > 0x7fffffffL) || (res < -2147483648))
		mono_raise_exception (mono_get_exception_overflow ());

	return res;
}

gint32
mono_imul_ovf_un (guint32 a, guint32 b)
{
	guint64 res;

	MONO_ARCH_SAVE_REGS;

	res = (guint64)a * (guint64)b;

	if ((res >> 32))
		mono_raise_exception (mono_get_exception_overflow ());

	return res;
}
#endif

#if defined(MONO_ARCH_EMULATE_MUL_DIV) || defined(MONO_ARCH_SOFT_FLOAT)
double
mono_fdiv (double a, double b)
{
	MONO_ARCH_SAVE_REGS;

	return a / b;
}
#endif

gint64 
mono_lldiv (gint64 a, gint64 b)
{
	MONO_ARCH_SAVE_REGS;

#ifdef MONO_ARCH_NEED_DIV_CHECK
	if (!b)
		mono_raise_exception (mono_get_exception_divide_by_zero ());
	else if (b == -1 && a == (-9223372036854775807LL - 1LL))
		mono_raise_exception (mono_get_exception_arithmetic ());
#endif
	return a / b;
}

gint64 
mono_llrem (gint64 a, gint64 b)
{
	MONO_ARCH_SAVE_REGS;

#ifdef MONO_ARCH_NEED_DIV_CHECK
	if (!b)
		mono_raise_exception (mono_get_exception_divide_by_zero ());
	else if (b == -1 && a == (-9223372036854775807LL - 1LL))
		mono_raise_exception (mono_get_exception_arithmetic ());
#endif
	return a % b;
}

guint64 
mono_lldiv_un (guint64 a, guint64 b)
{
	MONO_ARCH_SAVE_REGS;

#ifdef MONO_ARCH_NEED_DIV_CHECK
	if (!b)
		mono_raise_exception (mono_get_exception_divide_by_zero ());
#endif
	return a / b;
}

guint64 
mono_llrem_un (guint64 a, guint64 b)
{
	MONO_ARCH_SAVE_REGS;

#ifdef MONO_ARCH_NEED_DIV_CHECK
	if (!b)
		mono_raise_exception (mono_get_exception_divide_by_zero ());
#endif
	return a % b;
}

#endif

#ifndef MONO_ARCH_NO_EMULATE_LONG_SHIFT_OPS

guint64 
mono_lshl (guint64 a, gint32 shamt)
{
	guint64 res;

	/* no need, no exceptions: MONO_ARCH_SAVE_REGS;*/
	res = a << shamt;

	/*printf ("TESTL %lld << %d = %lld\n", a, shamt, res);*/

	return res;
}

guint64 
mono_lshr_un (guint64 a, gint32 shamt)
{
	guint64 res;

	/* no need, no exceptions: MONO_ARCH_SAVE_REGS;*/
	res = a >> shamt;

	/*printf ("TESTR %lld >> %d = %lld\n", a, shamt, res);*/

	return res;
}

gint64 
mono_lshr (gint64 a, gint32 shamt)
{
	gint64 res;

	/* no need, no exceptions: MONO_ARCH_SAVE_REGS;*/
	res = a >> shamt;

	/*printf ("TESTR %lld >> %d = %lld\n", a, shamt, res);*/

	return res;
}

#endif

#ifdef MONO_ARCH_SOFT_FLOAT

double
mono_fsub (double a, double b)
{
	return a - b;
}

double
mono_fadd (double a, double b)
{
	return a + b;
}

double
mono_fmul (double a, double b)
{
	return a * b;
}

double
mono_fneg (double a)
{
	return -a;
}

double
mono_fconv_r4 (double a)
{
	return (float)a;
}

double
mono_conv_to_r8 (int a)
{
	return (double)a;
}

double
mono_conv_to_r4 (int a)
{
	return (double)(float)a;
}

gint8
mono_fconv_i1 (double a)
{
	return (gint8)a;
}

gint16
mono_fconv_i2 (double a)
{
	return (gint16)a;
}

gint32
mono_fconv_i4 (double a)
{
	return (gint32)a;
}

guint8
mono_fconv_u1 (double a)
{
	return (guint8)a;
}

guint16
mono_fconv_u2 (double a)
{
	return (guint16)a;
}

gboolean
mono_fcmp_eq (double a, double b)
{
	return a == b;
}

gboolean
mono_fcmp_ge (double a, double b)
{
	return a >= b;
}

gboolean
mono_fcmp_gt (double a, double b)
{
	return a > b;
}

gboolean
mono_fcmp_le (double a, double b)
{
	return a <= b;
}

gboolean
mono_fcmp_lt (double a, double b)
{
	return a < b;
}

gboolean
mono_fcmp_ne_un (double a, double b)
{
	return isunordered (a, b) || a != b;
}

gboolean
mono_fcmp_ge_un (double a, double b)
{
	return isunordered (a, b) || a >= b;
}

gboolean
mono_fcmp_gt_un (double a, double b)
{
	return isunordered (a, b) || a > b;
}

gboolean
mono_fcmp_le_un (double a, double b)
{
	return isunordered (a, b) || a <= b;
}

gboolean
mono_fcmp_lt_un (double a, double b)
{
	return isunordered (a, b) || a < b;
}

gboolean
mono_fceq (double a, double b)
{
	return a == b;
}

gboolean
mono_fcgt (double a, double b)
{
	return a > b;
}

gboolean
mono_fcgt_un (double a, double b)
{
	return a > b;
}

gboolean
mono_fclt (double a, double b)
{
	return a < b;
}

gboolean
mono_fclt_un (double a, double b)
{
	return a < b;
}

double
mono_fload_r4 (float *ptr)
{
	return *ptr;
}

void
mono_fstore_r4 (double val, float *ptr)
{
	*ptr = (float)val;
}

/* returns the integer bitpattern that is passed in the regs or stack */
guint32
mono_fload_r4_arg (double val)
{
	float v = (float)val;
	return *(guint32*)&v;
}

#endif

MonoArray *
mono_array_new_va (MonoMethod *cm, ...)
{
	MonoDomain *domain = mono_domain_get ();
	va_list ap;
	guint32 *lengths;
	guint32 *lower_bounds;
	int pcount;
	int rank;
	int i, d;

	MONO_ARCH_SAVE_REGS;

	pcount = mono_method_signature (cm)->param_count;
	rank = cm->klass->rank;

	va_start (ap, cm);
	
	lengths = alloca (sizeof (guint32) * pcount);
	for (i = 0; i < pcount; ++i)
		lengths [i] = d = va_arg(ap, int);

	if (rank == pcount) {
		/* Only lengths provided. */
		if (cm->klass->byval_arg.type == MONO_TYPE_ARRAY) {
			lower_bounds = alloca (sizeof (guint32) * rank);
			memset (lower_bounds, 0, sizeof (guint32) * rank);
		} else {
			lower_bounds = NULL;
		}
	} else {
		g_assert (pcount == (rank * 2));
		/* lower bounds are first. */
		lower_bounds = lengths;
		lengths += rank;
	}
	va_end(ap);

	return mono_array_new_full (domain, cm->klass, lengths, lower_bounds);
}

gpointer
mono_class_static_field_address (MonoDomain *domain, MonoClassField *field)
{
	MonoVTable *vtable;
	gpointer addr;
	
	MONO_ARCH_SAVE_REGS;

	//printf ("SFLDA0 %s.%s::%s %d\n", field->parent->name_space, field->parent->name, field->name, field->offset, field->parent->inited);

	mono_class_init (field->parent);

	vtable = mono_class_vtable (domain, field->parent);
	if (!vtable->initialized)
		mono_runtime_class_init (vtable);

	//printf ("SFLDA1 %p\n", (char*)vtable->data + field->offset);

	if (domain->special_static_fields && (addr = g_hash_table_lookup (domain->special_static_fields, field)))
		addr = mono_get_special_static_data (GPOINTER_TO_UINT (addr));
	else
		addr = (char*)vtable->data + field->offset;
	
	return addr;
}

gpointer
mono_ldtoken_wrapper (MonoImage *image, int token, MonoGenericContext *context)
{
	MonoClass *handle_class;
	gpointer res;

	MONO_ARCH_SAVE_REGS;
	res = mono_ldtoken (image, token, &handle_class, context);	
	mono_class_init (handle_class);

	return res;
}

gpointer
mono_ldtoken_wrapper_generic_shared (MonoImage *image, int token, MonoMethod *method)
{
	MonoMethodSignature *sig = mono_method_signature (method);
	MonoGenericContext *generic_context;

	if (sig->is_inflated) {
		generic_context = mono_method_get_context (method);
	} else {
		MonoGenericContainer *generic_container = mono_method_get_generic_container (method);
		g_assert (generic_container);
		generic_context = &generic_container->context;
	}

	return mono_ldtoken_wrapper (image, token, generic_context);
}

guint64
mono_fconv_u8 (double v)
{
	return (guint64)v;
}

#ifdef MONO_ARCH_EMULATE_FCONV_TO_I8
gint64
mono_fconv_i8 (double v)
{
	/* no need, no exceptions: MONO_ARCH_SAVE_REGS;*/
	return (gint64)v;
}
#endif

guint32
mono_fconv_u4 (double v)
{
	/* no need, no exceptions: MONO_ARCH_SAVE_REGS;*/
	return (guint32)v;
}

#ifndef HAVE_TRUNC
/* Solaris doesn't have trunc */
#ifdef HAVE_AINTL
extern long double aintl (long double);
#define trunc aintl
#else
/* FIXME: This means we will never throw overflow exceptions */
#define trunc(v) res
#endif
#endif /* HAVE_TRUNC */

gint64
mono_fconv_ovf_i8 (double v)
{
	gint64 res;

	MONO_ARCH_SAVE_REGS;

	res = (gint64)v;

	if (isnan(v) || trunc (v) != res) {
		mono_raise_exception (mono_get_exception_overflow ());
	}
	return res;
}

guint64
mono_fconv_ovf_u8 (double v)
{
	guint64 res;

	MONO_ARCH_SAVE_REGS;
    
	res = (guint64)v;

	if (isnan(v) || trunc (v) != res) {
		mono_raise_exception (mono_get_exception_overflow ());
	}
	return res;
}

#ifdef MONO_ARCH_EMULATE_LCONV_TO_R8
double
mono_lconv_to_r8 (gint64 a)
{
	return (double)a;
}
#endif

#ifdef MONO_ARCH_EMULATE_LCONV_TO_R4
float
mono_lconv_to_r4 (gint64 a)
{
	return (float)a;
}
#endif

#ifdef MONO_ARCH_EMULATE_CONV_R8_UN
double
mono_conv_to_r8_un (guint32 a)
{
	return (double)a;
}
#endif

#ifdef MONO_ARCH_EMULATE_LCONV_TO_R8_UN
double
mono_lconv_to_r8_un (guint64 a)
{
	return (double)a;
}
#endif

gpointer
mono_helper_compile_generic_method (MonoObject *obj, MonoMethod *method, MonoGenericContext *context, gpointer *this_arg)
{
	MonoMethod *vmethod, *inflated;
	gpointer addr;

	mono_jit_stats.generic_virtual_invocations++;

	if (obj == NULL)
		mono_raise_exception (mono_get_exception_null_reference ());
	vmethod = mono_object_get_virtual_method (obj, method);

	/* 'vmethod' is partially inflated.  All the blanks corresponding to the type parameters of the
	   declaring class have been inflated.  We still need to fully inflate the method parameters.

	   FIXME: This code depends on the declaring class being fully inflated, since we inflate it twice with 
	   the same context.
	*/
	g_assert (!vmethod->klass->generic_container);
	g_assert (!vmethod->klass->generic_class || !vmethod->klass->generic_class->context.class_inst->is_open);
	g_assert (!context->method_inst || !context->method_inst->is_open);
	inflated = mono_class_inflate_generic_method (vmethod, context);
	addr = mono_compile_method (inflated);

	/* Since this is a virtual call, have to unbox vtypes */
	if (obj->vtable->klass->valuetype)
		*this_arg = mono_object_unbox (obj);
	else
		*this_arg = obj;

	return addr;
}

MonoString*
mono_helper_ldstr (MonoImage *image, guint32 idx)
{
	return mono_ldstr (mono_domain_get (), image, idx);
}

MonoString*
mono_helper_ldstr_mscorlib (guint32 idx)
{
	return mono_ldstr (mono_domain_get (), mono_defaults.corlib, idx);
}

MonoObject*
mono_helper_newobj_mscorlib (guint32 idx)
{
	MonoClass *klass = mono_class_get (mono_defaults.corlib, MONO_TOKEN_TYPE_DEF | idx);
	
	g_assert (klass);

	return mono_object_new (mono_domain_get (), klass);
}

/*
 * On some architectures, gdb doesn't like encountering the cpu breakpoint instructions
 * in generated code. So instead we emit a call to this function and place a gdb
 * breakpoint here.
 */
void
mono_break (void)
{
}

MonoException *
mono_create_corlib_exception_0 (guint32 token)
{
	return mono_exception_from_token (mono_defaults.corlib, token);
}

MonoException *
mono_create_corlib_exception_1 (guint32 token, MonoString *arg)
{
	return mono_exception_from_token_two_strings (mono_defaults.corlib, token, arg, NULL);
}

MonoException *
mono_create_corlib_exception_2 (guint32 token, MonoString *arg1, MonoString *arg2)
{
	return mono_exception_from_token_two_strings (mono_defaults.corlib, token, arg1, arg2);
}
