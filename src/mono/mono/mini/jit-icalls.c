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

static void*
mono_ldftn (MonoMethod *method)
{
	gpointer addr;

	MONO_ARCH_SAVE_REGS;

	EnterCriticalSection (metadata_section);
	addr = mono_compile_method (method);
	LeaveCriticalSection (metadata_section);

	return addr;
}

static void*
mono_ldvirtfn (MonoObject *obj, MonoMethod *method) 
{
	MONO_ARCH_SAVE_REGS;

	method = mono_object_get_virtual_method (obj, method);
	if (method->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED)
		method = mono_marshal_get_synchronized_wrapper (method);

	return mono_ldftn (method);
}

static void
helper_initobj (void *addr, int size)
{
	MONO_ARCH_SAVE_REGS;

	memset (addr, 0, size);
}

static void
helper_memcpy (void *addr, void *src, int size)
{
	MONO_ARCH_SAVE_REGS;

	memcpy (addr, src, size);
}

static void
helper_memset (void *addr, int val, int size)
{
	MONO_ARCH_SAVE_REGS;

	memset (addr, val, size);
}

static void
helper_stelem_ref (MonoArray *array, int index, MonoObject *val)
{
	MONO_ARCH_SAVE_REGS;

	if (index >= array->max_length)
		mono_raise_exception (mono_get_exception_index_out_of_range ());

	if (val && !mono_object_isinst (val, array->obj.vtable->klass->element_class))
		mono_raise_exception (mono_get_exception_array_type_mismatch ());

	mono_array_set (array, gpointer, index, val);
}

static gint64 
mono_llmult (gint64 a, gint64 b)
{
	MONO_ARCH_SAVE_REGS;
	return a * b;
}

static guint64  
mono_llmult_ovf_un (guint32 al, guint32 ah, guint32 bl, guint32 bh)
{
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


static guint64  
mono_llmult_ovf (guint32 al, gint32 ah, guint32 bl, gint32 bh) 
{
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

	/* do the AlBl term first */
	t1 = (gint64)al * (gint64)bl;

	res = t1;

	/* now do the [(Ah-Al)(Bl-Bh)+AlBl]R term */
	t1 += (gint64)(ah - al) * (gint64)(bl - bh);
	t1 <<= 32;
	/* check for overflow */
	if (t1 > (0x7FFFFFFFFFFFFFFF - res))
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

static gint64 
mono_lldiv (gint64 a, gint64 b)
{
	MONO_ARCH_SAVE_REGS;

	return a / b;
}

static gint64 
mono_llrem (gint64 a, gint64 b)
{
	MONO_ARCH_SAVE_REGS;

	return a % b;
}

static guint64 
mono_lldiv_un (guint64 a, guint64 b)
{
	MONO_ARCH_SAVE_REGS;

	return a / b;
}

static guint64 
mono_llrem_un (guint64 a, guint64 b)
{
	MONO_ARCH_SAVE_REGS;

	return a % b;
}

static guint64 
mono_lshl (guint64 a, gint32 shamt)
{
	guint64 res;

	MONO_ARCH_SAVE_REGS;
	res = a << shamt;

	/*printf ("TESTL %lld << %d = %lld\n", a, shamt, res);*/

	return res;
}

static guint64 
mono_lshr_un (guint64 a, gint32 shamt)
{
	guint64 res;

	MONO_ARCH_SAVE_REGS;
	res = a >> shamt;

	/*printf ("TESTR %lld >> %d = %lld\n", a, shamt, res);*/

	return res;
}

static gint64 
mono_lshr (gint64 a, gint32 shamt)
{
	gint64 res;

	MONO_ARCH_SAVE_REGS;
	res = a >> shamt;

	/*printf ("TESTR %lld >> %d = %lld\n", a, shamt, res);*/

	return res;
}

/**
 * ves_array_element_address:
 * @this: a pointer to the array object
 *
 * Returns: the address of an array element.
 */
static gpointer 
ves_array_element_address (MonoArray *this, ...)
{
	MonoClass *class;
	va_list ap;
	int i, ind, esize, realidx;
	gpointer ea;

	MONO_ARCH_SAVE_REGS;

	g_assert (this != NULL);

	va_start(ap, this);

	class = this->obj.vtable->klass;

	g_assert (this->bounds != NULL);

	esize = mono_array_element_size (class);
	ind = va_arg(ap, int);
	ind -= (int)this->bounds [0].lower_bound;
	if ((guint32)ind >= (guint32)this->bounds [0].length)
		mono_raise_exception (mono_get_exception_index_out_of_range ());
	for (i = 1; i < class->rank; i++) {
		realidx = va_arg(ap, int) - (int)this->bounds [i].lower_bound;
		if ((guint32)realidx >= (guint32)this->bounds [i].length)
			mono_raise_exception (mono_get_exception_index_out_of_range ());
		ind *= this->bounds [i].length;
		ind += realidx;
	}
	esize *= ind;

	ea = (gpointer*)((char*)this->vector + esize);

	va_end(ap);

	return ea;
}

static MonoArray *
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

	pcount = cm->signature->param_count;
	rank = cm->klass->rank;

	va_start (ap, cm);
	
	lengths = alloca (sizeof (guint32) * pcount);
	for (i = 0; i < pcount; ++i)
		lengths [i] = d = va_arg(ap, int);

	if (rank == pcount) {
		/* Only lengths provided. */
		lower_bounds = NULL;
	} else {
		g_assert (pcount == (rank * 2));
		/* lower bounds are first. */
		lower_bounds = lengths;
		lengths += rank;
	}
	va_end(ap);

	return mono_array_new_full (domain, cm->klass, lengths, lower_bounds);
}

static gpointer
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

	if (!domain->thread_static_fields || !(addr = g_hash_table_lookup (domain->thread_static_fields, field)))
		addr = (char*)vtable->data + field->offset;
	else
		addr = mono_threads_get_static_data (GPOINTER_TO_UINT (addr));
	
	return addr;
}

static gpointer
mono_ldtoken_wrapper (MonoImage *image, int token)
{
	MonoClass *handle_class;
	gpointer res;

	MONO_ARCH_SAVE_REGS;
	res = mono_ldtoken (image, token, &handle_class);	
	mono_class_init (handle_class);

	return res;
}

static guint64
mono_fconv_u8 (double v)
{
	MONO_ARCH_SAVE_REGS;
	return (guint64)v;
}

static guint32
mono_fconv_u4 (double v)
{
	MONO_ARCH_SAVE_REGS;
	return (guint32)v;
}

static gint64
mono_fconv_ovf_i8 (double v)
{
	gint64 res;

	MONO_ARCH_SAVE_REGS;

	res = (gint64)v;

	if (isnan(v) || v != res) {
		mono_raise_exception (mono_get_exception_overflow ());
	}
	return res;
}

static guint64
mono_fconv_ovf_u8 (double v)
{
	guint64 res;

	MONO_ARCH_SAVE_REGS;
    
	res = (guint64)v;

	if (isnan(v) || v != res) {
		mono_raise_exception (mono_get_exception_overflow ());
	}
	return res;
}
