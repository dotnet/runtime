/*
 * icall.c:
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Paolo Molaro (lupus@ximian.com)
 *	 Patrik Torstensson (patrik.torstensson@labs2.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <stdarg.h>
#include <string.h>
#include <sys/time.h>
#include <unistd.h>
#if defined (PLATFORM_WIN32)
#include <stdlib.h>
#endif

#include <mono/metadata/object.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/monitor.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/file-io.h>
#include <mono/metadata/socket-io.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/unicode.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/rand.h>
#include <mono/metadata/sysmath.h>
#include <mono/metadata/string-icalls.h>
#include <mono/metadata/mono-debug-debugger.h>
#include <mono/metadata/process.h>
#include <mono/metadata/environment.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/locales.h>
#include <mono/io-layer/io-layer.h>
#include <mono/utils/strtod.h>
#include <mono/utils/monobitset.h>

#if defined (PLATFORM_WIN32)
#include <windows.h>
#endif
#include "decimal.h"

static MonoReflectionAssembly* ves_icall_System_Reflection_Assembly_GetCallingAssembly (void);


/*
 * We expect a pointer to a char, not a string
 */
static double
mono_double_ParseImpl (char *ptr)
{
	gchar *endptr = NULL;
	gdouble result = 0.0;

	MONO_ARCH_SAVE_REGS;

	if (*ptr)
		result = bsd_strtod (ptr, &endptr);

	if (!*ptr || (endptr && *endptr))
		mono_raise_exception (mono_exception_from_name (mono_defaults.corlib,
								"System",
								"FormatException"));
	
	return result;
}

static void
ves_icall_System_Double_AssertEndianity (double *value)
{
	MONO_ARCH_SAVE_REGS;

	MONO_DOUBLE_ASSERT_ENDIANITY (value);
}

static MonoObject *
ves_icall_System_Array_GetValueImpl (MonoObject *this, guint32 pos)
{
	MonoClass *ac;
	MonoArray *ao;
	gint32 esize;
	gpointer *ea;

	MONO_ARCH_SAVE_REGS;

	ao = (MonoArray *)this;
	ac = (MonoClass *)ao->obj.vtable->klass;

	esize = mono_array_element_size (ac);
	ea = (gpointer*)((char*)ao->vector + (pos * esize));

	if (ac->element_class->valuetype)
		return mono_value_box (this->vtable->domain, ac->element_class, ea);
	else
		return *ea;
}

static MonoObject *
ves_icall_System_Array_GetValue (MonoObject *this, MonoObject *idxs)
{
	MonoClass *ac, *ic;
	MonoArray *ao, *io;
	gint32 i, pos, *ind;

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (idxs);

	io = (MonoArray *)idxs;
	ic = (MonoClass *)io->obj.vtable->klass;
	
	ao = (MonoArray *)this;
	ac = (MonoClass *)ao->obj.vtable->klass;

	g_assert (ic->rank == 1);
	if (io->bounds != NULL || io->max_length !=  ac->rank)
		mono_raise_exception (mono_get_exception_argument (NULL, NULL));

	ind = (guint32 *)io->vector;

	if (ao->bounds == NULL) {
		if (*ind < 0 || *ind >= ao->max_length)
			mono_raise_exception (mono_get_exception_index_out_of_range ());

		return ves_icall_System_Array_GetValueImpl (this, *ind);
	}
	
	for (i = 0; i < ac->rank; i++)
		if ((ind [i] < ao->bounds [i].lower_bound) ||
		    (ind [i] >= ao->bounds [i].length + ao->bounds [i].lower_bound))
			mono_raise_exception (mono_get_exception_index_out_of_range ());

	pos = ind [0] - ao->bounds [0].lower_bound;
	for (i = 1; i < ac->rank; i++)
		pos = pos*ao->bounds [i].length + ind [i] - 
			ao->bounds [i].lower_bound;

	return ves_icall_System_Array_GetValueImpl (this, pos);
}

static void
ves_icall_System_Array_SetValueImpl (MonoArray *this, MonoObject *value, guint32 pos)
{
	MonoClass *ac, *vc, *ec;
	gint32 esize, vsize;
	gpointer *ea, *va;

	guint64 u64 = 0;
	gint64 i64 = 0;
	gdouble r64 = 0;

	MONO_ARCH_SAVE_REGS;

	if (value)
		vc = value->vtable->klass;
	else
		vc = NULL;

	ac = this->obj.vtable->klass;
	ec = ac->element_class;

	esize = mono_array_element_size (ac);
	ea = (gpointer*)((char*)this->vector + (pos * esize));
	va = (gpointer*)((char*)value + sizeof (MonoObject));

	if (!value) {
		memset (ea, 0,  esize);
		return;
	}

#define NO_WIDENING_CONVERSION G_STMT_START{\
	mono_raise_exception (mono_get_exception_argument ( \
		"value", "not a widening conversion")); \
}G_STMT_END

#define CHECK_WIDENING_CONVERSION(extra) G_STMT_START{\
	if (esize < vsize + (extra)) \
		mono_raise_exception (mono_get_exception_argument ( \
			"value", "not a widening conversion")); \
}G_STMT_END

#define INVALID_CAST G_STMT_START{\
	mono_raise_exception (mono_get_exception_invalid_cast ()); \
}G_STMT_END

	/* Check element (destination) type. */
	switch (ec->byval_arg.type) {
	case MONO_TYPE_STRING:
		switch (vc->byval_arg.type) {
		case MONO_TYPE_STRING:
			break;
		default:
			INVALID_CAST;
		}
		break;
	case MONO_TYPE_BOOLEAN:
		switch (vc->byval_arg.type) {
		case MONO_TYPE_BOOLEAN:
			break;
		case MONO_TYPE_CHAR:
		case MONO_TYPE_U1:
		case MONO_TYPE_U2:
		case MONO_TYPE_U4:
		case MONO_TYPE_U8:
		case MONO_TYPE_I1:
		case MONO_TYPE_I2:
		case MONO_TYPE_I4:
		case MONO_TYPE_I8:
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
			NO_WIDENING_CONVERSION;
		default:
			INVALID_CAST;
		}
		break;
	}

	if (!ec->valuetype) {
		if (!mono_object_isinst (value, ec))
			INVALID_CAST;
		*ea = (gpointer)value;
		return;
	}

	if (mono_object_isinst (value, ec)) {
		memcpy (ea, (char *)value + sizeof (MonoObject), esize);
		return;
	}

	if (!vc->valuetype)
		INVALID_CAST;

	vsize = mono_class_instance_size (vc) - sizeof (MonoObject);

#if 0
	g_message (G_STRLOC ": %d (%d) <= %d (%d)",
		   ec->byval_arg.type, esize,
		   vc->byval_arg.type, vsize);
#endif

#define ASSIGN_UNSIGNED(etype) G_STMT_START{\
	switch (vc->byval_arg.type) { \
	case MONO_TYPE_U1: \
	case MONO_TYPE_U2: \
	case MONO_TYPE_U4: \
	case MONO_TYPE_U8: \
	case MONO_TYPE_CHAR: \
		CHECK_WIDENING_CONVERSION(0); \
		*(etype *) ea = (etype) u64; \
		return; \
	/* You can't assign a signed value to an unsigned array. */ \
	case MONO_TYPE_I1: \
	case MONO_TYPE_I2: \
	case MONO_TYPE_I4: \
	case MONO_TYPE_I8: \
	/* You can't assign a floating point number to an integer array. */ \
	case MONO_TYPE_R4: \
	case MONO_TYPE_R8: \
		NO_WIDENING_CONVERSION; \
	} \
}G_STMT_END

#define ASSIGN_SIGNED(etype) G_STMT_START{\
	switch (vc->byval_arg.type) { \
	case MONO_TYPE_I1: \
	case MONO_TYPE_I2: \
	case MONO_TYPE_I4: \
	case MONO_TYPE_I8: \
		CHECK_WIDENING_CONVERSION(0); \
		*(etype *) ea = (etype) i64; \
		return; \
	/* You can assign an unsigned value to a signed array if the array's */ \
	/* element size is larger than the value size. */ \
	case MONO_TYPE_U1: \
	case MONO_TYPE_U2: \
	case MONO_TYPE_U4: \
	case MONO_TYPE_U8: \
	case MONO_TYPE_CHAR: \
		CHECK_WIDENING_CONVERSION(1); \
		*(etype *) ea = (etype) u64; \
		return; \
	/* You can't assign a floating point number to an integer array. */ \
	case MONO_TYPE_R4: \
	case MONO_TYPE_R8: \
		NO_WIDENING_CONVERSION; \
	} \
}G_STMT_END

#define ASSIGN_REAL(etype) G_STMT_START{\
	switch (vc->byval_arg.type) { \
	case MONO_TYPE_R4: \
	case MONO_TYPE_R8: \
		CHECK_WIDENING_CONVERSION(0); \
		*(etype *) ea = (etype) r64; \
		return; \
	/* All integer values fit into a floating point array, so we don't */ \
	/* need to CHECK_WIDENING_CONVERSION here. */ \
	case MONO_TYPE_I1: \
	case MONO_TYPE_I2: \
	case MONO_TYPE_I4: \
	case MONO_TYPE_I8: \
		*(etype *) ea = (etype) i64; \
		return; \
	case MONO_TYPE_U1: \
	case MONO_TYPE_U2: \
	case MONO_TYPE_U4: \
	case MONO_TYPE_U8: \
	case MONO_TYPE_CHAR: \
		*(etype *) ea = (etype) u64; \
		return; \
	} \
}G_STMT_END

	switch (vc->byval_arg.type) {
	case MONO_TYPE_U1:
		u64 = *(guint8 *) va;
		break;
	case MONO_TYPE_U2:
		u64 = *(guint16 *) va;
		break;
	case MONO_TYPE_U4:
		u64 = *(guint32 *) va;
		break;
	case MONO_TYPE_U8:
		u64 = *(guint64 *) va;
		break;
	case MONO_TYPE_I1:
		i64 = *(gint8 *) va;
		break;
	case MONO_TYPE_I2:
		i64 = *(gint16 *) va;
		break;
	case MONO_TYPE_I4:
		i64 = *(gint32 *) va;
		break;
	case MONO_TYPE_I8:
		i64 = *(gint64 *) va;
		break;
	case MONO_TYPE_R4:
		r64 = *(gfloat *) va;
		break;
	case MONO_TYPE_R8:
		r64 = *(gdouble *) va;
		break;
	case MONO_TYPE_CHAR:
		u64 = *(guint16 *) va;
		break;
	case MONO_TYPE_BOOLEAN:
		/* Boolean is only compatible with itself. */
		switch (ec->byval_arg.type) {
		case MONO_TYPE_CHAR:
		case MONO_TYPE_U1:
		case MONO_TYPE_U2:
		case MONO_TYPE_U4:
		case MONO_TYPE_U8:
		case MONO_TYPE_I1:
		case MONO_TYPE_I2:
		case MONO_TYPE_I4:
		case MONO_TYPE_I8:
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
			NO_WIDENING_CONVERSION;
		default:
			INVALID_CAST;
		}
		break;
	}

	/* If we can't do a direct copy, let's try a widening conversion. */
	switch (ec->byval_arg.type) {
	case MONO_TYPE_CHAR:
		ASSIGN_UNSIGNED (guint16);
	case MONO_TYPE_U1:
		ASSIGN_UNSIGNED (guint8);
	case MONO_TYPE_U2:
		ASSIGN_UNSIGNED (guint16);
	case MONO_TYPE_U4:
		ASSIGN_UNSIGNED (guint32);
	case MONO_TYPE_U8:
		ASSIGN_UNSIGNED (guint64);
	case MONO_TYPE_I1:
		ASSIGN_SIGNED (gint8);
	case MONO_TYPE_I2:
		ASSIGN_SIGNED (gint16);
	case MONO_TYPE_I4:
		ASSIGN_SIGNED (gint32);
	case MONO_TYPE_I8:
		ASSIGN_SIGNED (gint64);
	case MONO_TYPE_R4:
		ASSIGN_REAL (gfloat);
	case MONO_TYPE_R8:
		ASSIGN_REAL (gdouble);
	}

	INVALID_CAST;
	/* Not reached, INVALID_CAST does not return. Just to avoid a compiler warning ... */
	return;

#undef INVALID_CAST
#undef NO_WIDENING_CONVERSION
#undef CHECK_WIDENING_CONVERSION
#undef ASSIGN_UNSIGNED
#undef ASSIGN_SIGNED
#undef ASSIGN_REAL
}

static void 
ves_icall_System_Array_SetValue (MonoArray *this, MonoObject *value,
				 MonoArray *idxs)
{
	MonoClass *ac, *ic;
	gint32 i, pos, *ind;

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (idxs);

	ic = idxs->obj.vtable->klass;
	ac = this->obj.vtable->klass;

	g_assert (ic->rank == 1);
	if (idxs->bounds != NULL || idxs->max_length != ac->rank)
		mono_raise_exception (mono_get_exception_argument (NULL, NULL));

	ind = (guint32 *)idxs->vector;

	if (this->bounds == NULL) {
		if (*ind < 0 || *ind >= this->max_length)
			mono_raise_exception (mono_get_exception_index_out_of_range ());

		ves_icall_System_Array_SetValueImpl (this, value, *ind);
		return;
	}
	
	for (i = 0; i < ac->rank; i++)
		if ((ind [i] < this->bounds [i].lower_bound) ||
		    (ind [i] >= this->bounds [i].length + this->bounds [i].lower_bound))
			mono_raise_exception (mono_get_exception_index_out_of_range ());

	pos = ind [0] - this->bounds [0].lower_bound;
	for (i = 1; i < ac->rank; i++)
		pos = pos * this->bounds [i].length + ind [i] - 
			this->bounds [i].lower_bound;

	ves_icall_System_Array_SetValueImpl (this, value, pos);
}

static MonoArray *
ves_icall_System_Array_CreateInstanceImpl (MonoReflectionType *type, MonoArray *lengths, MonoArray *bounds)
{
	MonoClass *aklass;
	MonoArray *array;
	gint32 *sizes, i;

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (type);
	MONO_CHECK_ARG_NULL (lengths);

	MONO_CHECK_ARG (lengths, mono_array_length (lengths) > 0);
	if (bounds)
		MONO_CHECK_ARG (bounds, mono_array_length (lengths) == mono_array_length (bounds));

	for (i = 0; i < mono_array_length (lengths); i++)
		if (mono_array_get (lengths, gint32, i) < 0)
			mono_raise_exception (mono_get_exception_argument_out_of_range (NULL));

	aklass = mono_array_class_get (mono_class_from_mono_type (type->type), mono_array_length (lengths));

	sizes = alloca (aklass->rank * sizeof(guint32) * 2);
	for (i = 0; i < aklass->rank; ++i) {
		sizes [i] = mono_array_get (lengths, gint32, i);
		if (bounds)
			sizes [i + aklass->rank] = mono_array_get (bounds, gint32, i);
		else
			sizes [i + aklass->rank] = 0;
	}

	array = mono_array_new_full (mono_object_domain (type), aklass, sizes, sizes + aklass->rank);

	return array;
}

static gint32 
ves_icall_System_Array_GetRank (MonoObject *this)
{
	MONO_ARCH_SAVE_REGS;

	return this->vtable->klass->rank;
}

static gint32
ves_icall_System_Array_GetLength (MonoArray *this, gint32 dimension)
{
	gint32 rank = ((MonoObject *)this)->vtable->klass->rank;

	MONO_ARCH_SAVE_REGS;

	if ((dimension < 0) || (dimension >= rank))
		mono_raise_exception (mono_get_exception_index_out_of_range ());
	
	if (this->bounds == NULL)
		return this->max_length;
	
	return this->bounds [dimension].length;
}

static gint32
ves_icall_System_Array_GetLowerBound (MonoArray *this, gint32 dimension)
{
	gint32 rank = ((MonoObject *)this)->vtable->klass->rank;

	MONO_ARCH_SAVE_REGS;

	if ((dimension < 0) || (dimension >= rank))
		mono_raise_exception (mono_get_exception_index_out_of_range ());
	
	if (this->bounds == NULL)
		return 0;
	
	return this->bounds [dimension].lower_bound;
}

static gboolean
ves_icall_System_Array_FastCopy (MonoArray *source, int source_idx, MonoArray* dest, int dest_idx, int length)
{
	int element_size;
	void * dest_addr;
	void * source_addr;
	MonoClass *src_class;
	MonoClass *dest_class;
	int i;

	MONO_ARCH_SAVE_REGS;

	if (source->obj.vtable->klass->rank != dest->obj.vtable->klass->rank)
		return FALSE;

	if (source->bounds || dest->bounds)
		return FALSE;

	if ((dest_idx + length > mono_array_length (dest)) ||
		(source_idx + length > mono_array_length (source)))
		return FALSE;

	element_size = mono_array_element_size (source->obj.vtable->klass);
	dest_addr = mono_array_addr_with_size (dest, element_size, dest_idx);
	source_addr = mono_array_addr_with_size (source, element_size, source_idx);

	src_class = source->obj.vtable->klass->element_class;
	dest_class = dest->obj.vtable->klass->element_class;

	/*
	 * Handle common cases.
	 */

	/* Case1: object[] -> valuetype[] (ArrayList::ToArray) */
	if (src_class == mono_defaults.object_class && dest_class->valuetype) {
		for (i = source_idx; i < source_idx + length; ++i) {
			MonoObject *elem = mono_array_get (source, MonoObject*, i);
			if (elem && !mono_object_isinst (elem, dest_class))
				return FALSE;
		}

		element_size = mono_array_element_size (dest->obj.vtable->klass);
		for (i = 0; i < length; ++i) {
			MonoObject *elem = mono_array_get (source, MonoObject*, source_idx + i);
			void *addr = mono_array_addr_with_size (dest, element_size, dest_idx + i);
			if (!elem)
				memset (addr, 0, element_size);
			else
				memcpy (addr, (char *)elem + sizeof (MonoObject), element_size);
		}
		return TRUE;
	}

	if (src_class != dest_class) {
		if (dest_class->valuetype || dest_class->enumtype || src_class->valuetype || src_class->enumtype)
			return FALSE;

		if (mono_class_is_subclass_of (src_class, dest_class, FALSE))
			;
		/* Case2: object[] -> reftype[] (ArrayList::ToArray) */
		else if (mono_class_is_subclass_of (dest_class, src_class, FALSE))
			for (i = source_idx; i < source_idx + length; ++i) {
				MonoObject *elem = mono_array_get (source, MonoObject*, i);
				if (elem && !mono_object_isinst (elem, dest_class))
					return FALSE;
			}
		else
			return FALSE;
	}

	memmove (dest_addr, source_addr, element_size * length);

	return TRUE;
}

static void
ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_InitializeArray (MonoArray *array, MonoClassField *field_handle)
{
	MonoClass *klass = array->obj.vtable->klass;
	guint32 size = mono_array_element_size (klass);
	int i;

	MONO_ARCH_SAVE_REGS;

	if (array->bounds == NULL)
		size *= array->max_length;
	else
		for (i = 0; i < klass->rank; ++i) 
			size *= array->bounds [i].length;

	memcpy (mono_array_addr (array, char, 0), field_handle->data, size);

#if G_BYTE_ORDER != G_LITTLE_ENDIAN
#define SWAP(n) {\
	gint i; \
	guint ## n tmp; \
	guint ## n *data = (guint ## n *) mono_array_addr (array, char, 0); \
\
	for (i = 0; i < size; i += n/8, data++) { \
		tmp = read ## n (data); \
		*data = tmp; \
	} \
}

	/* printf ("Initialize array with elements of %s type\n", klass->element_class->name); */

	switch (klass->element_class->byval_arg.type) {
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
		SWAP (16);
		break;
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		SWAP (32);
		break;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		SWAP (64);
		break;
	}
		 
#endif
}

static gint
ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_GetOffsetToStringData (void)
{
	MONO_ARCH_SAVE_REGS;

	return offsetof (MonoString, chars);
}

static MonoObject *
ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_GetObjectValue (MonoObject *obj)
{
	MONO_ARCH_SAVE_REGS;

	if ((obj == NULL) || (! (obj->vtable->klass->valuetype)))
		return obj;
	else
		return mono_object_clone (obj);
}

static void
ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_RunClassConstructor (MonoType *handle)
{
	MonoClass *klass;

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (handle);

	klass = mono_class_from_mono_type (handle);
	MONO_CHECK_ARG (handle, klass);

	/* This will call the type constructor */
	if (! (klass->flags & TYPE_ATTRIBUTE_INTERFACE))
		mono_runtime_class_init (mono_class_vtable (mono_domain_get (), klass));
}

static MonoObject *
ves_icall_System_Object_MemberwiseClone (MonoObject *this)
{
	MONO_ARCH_SAVE_REGS;

	return mono_object_clone (this);
}

#if HAVE_BOEHM_GC
#define MONO_OBJECT_ALIGNMENT_SHIFT	3
#else
#define MONO_OBJECT_ALIGNMENT_SHIFT	2
#endif

/*
 * Return hashcode based on object address. This function will need to be
 * smarter in the presence of a moving garbage collector, which will cache
 * the address hash before relocating the object.
 *
 * Wang's address-based hash function:
 *   http://www.concentric.net/~Ttwang/tech/addrhash.htm
 */
static gint32
ves_icall_System_Object_GetHashCode (MonoObject *this)
{
	register guint32 key;

	MONO_ARCH_SAVE_REGS;

	key = (GPOINTER_TO_UINT (this) >> MONO_OBJECT_ALIGNMENT_SHIFT) * 2654435761u;

	return key & 0x7fffffff;
}

static gint32
ves_icall_System_ValueType_InternalGetHashCode (MonoObject *this, MonoArray **fields)
{
	int i;
	MonoClass *klass;
	MonoObject **values = NULL;
	MonoObject *o;
	int count = 0;
	gint32 result = 0;

	MONO_ARCH_SAVE_REGS;

	klass = this->vtable->klass;

	if (klass->field.count == 0)
		return ves_icall_System_Object_GetHashCode (this);

	/*
	 * Compute the starting value of the hashcode for fields of primitive
	 * types, and return the remaining fields in an array to the managed side.
	 * This way, we can avoid costly reflection operations in managed code.
	 */
	for (i = 0; i < klass->field.count; ++i) {
		MonoClassField *field = &klass->fields [i];
		if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;
		/* FIXME: Add more types */
		switch (field->type->type) {
		case MONO_TYPE_I4:
			result ^= *(gint32*)((guint8*)this + field->offset);
			break;
		case MONO_TYPE_STRING: {
			MonoString *s;
			s = *(MonoString**)((guint8*)this + field->offset);
			if (s != NULL)
				result ^= ves_icall_System_String_GetHashCode (s);
			break;
		}
		default:
			if (!values)
				values = alloca (klass->field.count * sizeof (MonoObject*));
			o = mono_field_get_value_object (mono_object_domain (this), field, this);
			values [count++] = o;
		}
	}

	if (values) {
		*fields = mono_array_new (mono_domain_get (), mono_defaults.object_class, count);
		memcpy (mono_array_addr (*fields, MonoObject*, 0), values, count * sizeof (MonoObject*));
	}
	else
		*fields = NULL;
	return result;
}

static MonoBoolean
ves_icall_System_ValueType_Equals (MonoObject *this, MonoObject *that, MonoArray **fields)
{
	int i;
	MonoClass *klass;
	MonoObject **values = NULL;
	MonoObject *o;
	int count = 0;

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (that);

	if (this->vtable != that->vtable)
		return FALSE;

	klass = this->vtable->klass;

	/*
	 * Do the comparison for fields of primitive type and return a result if
	 * possible. Otherwise, return the remaining fields in an array to the 
     * managed side. This way, we can avoid costly reflection operations in 
     * managed code.
	 */
	*fields = NULL;
	for (i = 0; i < klass->field.count; ++i) {
		MonoClassField *field = &klass->fields [i];
		if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;
		/* FIXME: Add more types */
		switch (field->type->type) {
		case MONO_TYPE_I4:
			if (*(gint32*)((guint8*)this + field->offset) != *(gint32*)((guint8*)that + field->offset))
				return FALSE;
			break;
		case MONO_TYPE_STRING: {
			MonoString *s1, *s2;
			guint32 s1len, s2len;
			s1 = *(MonoString**)((guint8*)this + field->offset);
			s2 = *(MonoString**)((guint8*)that + field->offset);
			if (s1 == s2)
				break;
			if ((s1 == NULL) || (s2 == NULL))
				return FALSE;
			s1len = mono_string_length (s1);
			s2len = mono_string_length (s2);
			if (s1len != s2len)
				return FALSE;

			if (memcmp (mono_string_chars (s1), mono_string_chars (s2), s1len * sizeof (gunichar2)) != 0)
				return FALSE;
			break;
		}
		default:
			if (!values)
				values = alloca (klass->field.count * 2 * sizeof (MonoObject*));
			o = mono_field_get_value_object (mono_object_domain (this), field, this);
			values [count++] = o;
			o = mono_field_get_value_object (mono_object_domain (this), field, that);
			values [count++] = o;
		}
	}

	if (values) {
		*fields = mono_array_new (mono_domain_get (), mono_defaults.object_class, count);
		memcpy (mono_array_addr (*fields, MonoObject*, 0), values, count * sizeof (MonoObject*));

		return FALSE;
	}
	else
		return TRUE;
}

static MonoReflectionType *
ves_icall_System_Object_GetType (MonoObject *obj)
{
	MONO_ARCH_SAVE_REGS;

	return mono_type_get_object (mono_object_domain (obj), &obj->vtable->klass->byval_arg);
}

static void
mono_type_type_from_obj (MonoReflectionType *mtype, MonoObject *obj)
{
	MONO_ARCH_SAVE_REGS;

	mtype->type = &obj->vtable->klass->byval_arg;
	g_assert (mtype->type->type);
}

static gint32
ves_icall_ModuleBuilder_getToken (MonoReflectionModuleBuilder *mb, MonoObject *obj)
{
	MONO_ARCH_SAVE_REGS;

	return mono_image_create_token (mb->dynamic_image, obj);
}

static gint32
ves_icall_ModuleBuilder_getDataChunk (MonoReflectionModuleBuilder *mb, MonoArray *buf, gint32 offset)
{
	int count;
	MonoDynamicImage *image = mb->dynamic_image;
	char *p = mono_array_addr (buf, char, 0);

	MONO_ARCH_SAVE_REGS;

	mono_image_create_pefile (mb);

	if (offset >= image->pefile.index)
		return 0;
	count = mono_array_length (buf);
	count = MIN (count, image->pefile.index - offset);
	
	memcpy (p, image->pefile.data + offset, count);

	return count;
}

static void
ves_icall_ModuleBuilder_build_metadata (MonoReflectionModuleBuilder *mb)
{
	MONO_ARCH_SAVE_REGS;

	mono_image_build_metadata (mb);
}

static MonoReflectionType*
ves_icall_type_from_name (MonoString *name,
			  MonoBoolean throwOnError,
			  MonoBoolean ignoreCase)
{
	gchar *str;
	MonoType *type = NULL;
	MonoAssembly *assembly;
	MonoTypeNameParse info;

	MONO_ARCH_SAVE_REGS;

	str = mono_string_to_utf8 (name);
	if (!mono_reflection_parse_type (str, &info)) {
		g_free (str);
		g_list_free (info.modifiers);
		g_list_free (info.nested);
		if (throwOnError) /* uhm: this is a parse error, though... */
			mono_raise_exception (mono_get_exception_type_load (name));

		return NULL;
	}

	if (info.assembly.name) {
		assembly = mono_assembly_load (&info.assembly, NULL, NULL);
	} else {
		MonoReflectionAssembly *refass;

		refass = ves_icall_System_Reflection_Assembly_GetCallingAssembly  ();
		assembly = refass->assembly;
	}

	if (assembly)
		type = mono_reflection_get_type (assembly->image, &info, ignoreCase);
	
	if (!info.assembly.name && !type) /* try mscorlib */
		type = mono_reflection_get_type (NULL, &info, ignoreCase);

	g_free (str);
	g_list_free (info.modifiers);
	g_list_free (info.nested);
	if (!type) {
		if (throwOnError)
			mono_raise_exception (mono_get_exception_type_load (name));

		return NULL;
	}

	return mono_type_get_object (mono_domain_get (), type);
}

static MonoReflectionType*
ves_icall_type_from_handle (MonoType *handle)
{
	MonoDomain *domain = mono_domain_get (); 
	MonoClass *klass = mono_class_from_mono_type (handle);

	MONO_ARCH_SAVE_REGS;

	mono_class_init (klass);
	return mono_type_get_object (domain, handle);
}

static guint32
ves_icall_type_Equals (MonoReflectionType *type, MonoReflectionType *c)
{
	MONO_ARCH_SAVE_REGS;

	if (type->type && c->type)
		return mono_metadata_type_equal (type->type, c->type);
	g_print ("type equals\n");
	return 0;
}

/* System.TypeCode */
typedef enum {
	TYPECODE_EMPTY,
	TYPECODE_OBJECT,
	TYPECODE_DBNULL,
	TYPECODE_BOOLEAN,
	TYPECODE_CHAR,
	TYPECODE_SBYTE,
	TYPECODE_BYTE,
	TYPECODE_INT16,
	TYPECODE_UINT16,
	TYPECODE_INT32,
	TYPECODE_UINT32,
	TYPECODE_INT64,
	TYPECODE_UINT64,
	TYPECODE_SINGLE,
	TYPECODE_DOUBLE,
	TYPECODE_DECIMAL,
	TYPECODE_DATETIME,
	TYPECODE_STRING = 18
} TypeCode;

static guint32
ves_icall_type_GetTypeCode (MonoReflectionType *type)
{
	int t = type->type->type;

	MONO_ARCH_SAVE_REGS;

handle_enum:
	switch (t) {
	case MONO_TYPE_VOID:
		return TYPECODE_OBJECT;
	case MONO_TYPE_BOOLEAN:
		return TYPECODE_BOOLEAN;
	case MONO_TYPE_U1:
		return TYPECODE_BYTE;
	case MONO_TYPE_I1:
		return TYPECODE_SBYTE;
	case MONO_TYPE_U2:
		return TYPECODE_UINT16;
	case MONO_TYPE_I2:
		return TYPECODE_INT16;
	case MONO_TYPE_CHAR:
		return TYPECODE_CHAR;
	case MONO_TYPE_PTR:
	case MONO_TYPE_U:
	case MONO_TYPE_I:
		return TYPECODE_OBJECT;
	case MONO_TYPE_U4:
		return TYPECODE_UINT32;
	case MONO_TYPE_I4:
		return TYPECODE_INT32;
	case MONO_TYPE_U8:
		return TYPECODE_UINT64;
	case MONO_TYPE_I8:
		return TYPECODE_INT64;
	case MONO_TYPE_R4:
		return TYPECODE_SINGLE;
	case MONO_TYPE_R8:
		return TYPECODE_DOUBLE;
	case MONO_TYPE_VALUETYPE:
		if (type->type->data.klass->enumtype) {
			t = type->type->data.klass->enum_basetype->type;
			goto handle_enum;
		} else {
			MonoClass *k =  type->type->data.klass;
			if (strcmp (k->name_space, "System") == 0) {
				if (strcmp (k->name, "Decimal") == 0)
					return TYPECODE_DECIMAL;
				else if (strcmp (k->name, "DateTime") == 0)
					return TYPECODE_DATETIME;
			}
		}
		return TYPECODE_OBJECT;
	case MONO_TYPE_STRING:
		return TYPECODE_STRING;
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_OBJECT:
		return TYPECODE_OBJECT;
	case MONO_TYPE_CLASS:
		{
			MonoClass *k =  type->type->data.klass;
			if (strcmp (k->name_space, "System") == 0) {
				if (strcmp (k->name, "DBNull") == 0)
					return TYPECODE_DBNULL;
			}
		}
		return TYPECODE_OBJECT;
	default:
		g_error ("type 0x%02x not handled in GetTypeCode()", t);
	}
	return 0;
}

static guint32
ves_icall_type_is_subtype_of (MonoReflectionType *type, MonoReflectionType *c, MonoBoolean check_interfaces)
{
	MonoDomain *domain; 
	MonoClass *klass;
	MonoClass *klassc;

	MONO_ARCH_SAVE_REGS;

	g_assert (type != NULL);
	
	domain = ((MonoObject *)type)->vtable->domain;

	if (!c) /* FIXME: dont know what do do here */
		return 0;

	klass = mono_class_from_mono_type (type->type);
	klassc = mono_class_from_mono_type (c->type);

	return mono_class_is_subclass_of (klass, klassc, check_interfaces);
}

static guint32
ves_icall_type_is_assignable_from (MonoReflectionType *type, MonoReflectionType *c)
{
	MonoDomain *domain; 
	MonoClass *klass;
	MonoClass *klassc;

	MONO_ARCH_SAVE_REGS;

	g_assert (type != NULL);
	
	domain = ((MonoObject *)type)->vtable->domain;

	klass = mono_class_from_mono_type (type->type);
	klassc = mono_class_from_mono_type (c->type);

	return mono_class_is_assignable_from (klass, klassc);
}

static guint32
ves_icall_get_attributes (MonoReflectionType *type)
{
	MonoClass *klass = mono_class_from_mono_type (type->type);

	MONO_ARCH_SAVE_REGS;

	return klass->flags;
}

static MonoReflectionField*
ves_icall_System_Reflection_FieldInfo_internal_from_handle (MonoClassField *handle)
{
	MONO_ARCH_SAVE_REGS;

	g_assert (handle);

	return mono_field_get_object (mono_domain_get (), handle->parent, handle);
}

static void
ves_icall_get_method_info (MonoMethod *method, MonoMethodInfo *info)
{
	MonoDomain *domain = mono_domain_get ();

	MONO_ARCH_SAVE_REGS;

	info->parent = mono_type_get_object (domain, &method->klass->byval_arg);
	info->ret = mono_type_get_object (domain, method->signature->ret);
	info->attrs = method->flags;
	info->implattrs = method->iflags;
	if (method->signature->call_convention == MONO_CALL_DEFAULT)
		info->callconv = 1;
	else
		if (method->signature->call_convention == MONO_CALL_VARARG)
			info->callconv = 2;
		else
			info->callconv = 0;
	info->callconv |= (method->signature->hasthis << 5) | (method->signature->explicit_this << 6); 
}

static MonoArray*
ves_icall_get_parameter_info (MonoMethod *method)
{
	MonoDomain *domain = mono_domain_get (); 

	MONO_ARCH_SAVE_REGS;

	return mono_param_get_objects (domain, method);
}

static MonoReflectionType*
ves_icall_MonoField_GetParentType (MonoReflectionField *field, MonoBoolean declaring)
{
	MonoClass *parent;
	MONO_ARCH_SAVE_REGS;

	parent = declaring? field->field->parent: field->klass;

	return mono_type_get_object (mono_object_domain (field), &parent->byval_arg);
}

static MonoObject *
ves_icall_MonoField_GetValueInternal (MonoReflectionField *field, MonoObject *obj)
{	
	MonoObject *o;
	MonoClassField *cf = field->field;
	MonoClass *klass;
	MonoVTable *vtable;
	MonoDomain *domain = mono_object_domain (field); 
	gchar *v;
	gboolean is_static = FALSE;
	gboolean is_ref = FALSE;

	MONO_ARCH_SAVE_REGS;

	mono_class_init (field->klass);

	switch (cf->type->type) {
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_SZARRAY:
		is_ref = TRUE;
		break;
	case MONO_TYPE_U1:
	case MONO_TYPE_I1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_U2:
	case MONO_TYPE_I2:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_U:
	case MONO_TYPE_I:
	case MONO_TYPE_U4:
	case MONO_TYPE_I4:
	case MONO_TYPE_R4:
	case MONO_TYPE_U8:
	case MONO_TYPE_I8:
	case MONO_TYPE_R8:
	case MONO_TYPE_VALUETYPE:
		is_ref = cf->type->byref;
		break;
	default:
		g_error ("type 0x%x not handled in "
			 "ves_icall_Monofield_GetValue", cf->type->type);
		return NULL;
	}

	vtable = NULL;
	if (cf->type->attrs & FIELD_ATTRIBUTE_STATIC) {
		is_static = TRUE;
		vtable = mono_class_vtable (domain, field->klass);
		if (!vtable->initialized)
			mono_runtime_class_init (vtable);
	}
	
	if (is_ref) {
		if (is_static) {
			mono_field_static_get_value (vtable, cf, &o);
		} else {
			mono_field_get_value (obj, cf, &o);
		}
		return o;
	}

	/* boxed value type */
	klass = mono_class_from_mono_type (cf->type);
	o = mono_object_new (domain, klass);
	v = ((gchar *) o) + sizeof (MonoObject);
	if (is_static) {
		mono_field_static_get_value (vtable, cf, v);
	} else {
		mono_field_get_value (obj, cf, v);
	}

	return o;
}

static void
ves_icall_FieldInfo_SetValueInternal (MonoReflectionField *field, MonoObject *obj, MonoObject *value)
{
	MonoClassField *cf = field->field;
	gchar *v;

	MONO_ARCH_SAVE_REGS;

	v = (gchar *) value;
	if (!cf->type->byref) {
		switch (cf->type->type) {
		case MONO_TYPE_U1:
		case MONO_TYPE_I1:
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_U2:
		case MONO_TYPE_I2:
		case MONO_TYPE_CHAR:
		case MONO_TYPE_U:
		case MONO_TYPE_I:
		case MONO_TYPE_U4:
		case MONO_TYPE_I4:
		case MONO_TYPE_R4:
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
		case MONO_TYPE_R8:
		case MONO_TYPE_VALUETYPE:
			v += sizeof (MonoObject);
			break;
		case MONO_TYPE_STRING:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
			/* Do nothing */
			break;
		default:
			g_error ("type 0x%x not handled in "
				 "ves_icall_FieldInfo_SetValueInternal", cf->type->type);
			return;
		}
	}

	if (cf->type->attrs & FIELD_ATTRIBUTE_STATIC) {
		MonoVTable *vtable = mono_class_vtable (mono_object_domain (field), field->klass);
		if (!vtable->initialized)
			mono_runtime_class_init (vtable);
		mono_field_static_set_value (vtable, cf, v);
	} else {
		mono_field_set_value (obj, cf, v);
	}
}

static void
ves_icall_get_property_info (MonoReflectionProperty *property, MonoPropertyInfo *info)
{
	MonoDomain *domain = mono_object_domain (property); 

	MONO_ARCH_SAVE_REGS;

	info->parent = mono_type_get_object (domain, &property->klass->byval_arg);
	info->name = mono_string_new (domain, property->property->name);
	info->attrs = property->property->attrs;
	info->get = property->property->get ? mono_method_get_object (domain, property->property->get, NULL): NULL;
	info->set = property->property->set ? mono_method_get_object (domain, property->property->set, NULL): NULL;
	/* 
	 * There may be other methods defined for properties, though, it seems they are not exposed 
	 * in the reflection API 
	 */
}

static void
ves_icall_get_event_info (MonoReflectionEvent *event, MonoEventInfo *info)
{
	MonoDomain *domain = mono_object_domain (event); 

	MONO_ARCH_SAVE_REGS;

	info->parent = mono_type_get_object (domain, &event->klass->byval_arg);
	info->name = mono_string_new (domain, event->event->name);
	info->attrs = event->event->attrs;
	info->add_method = event->event->add ? mono_method_get_object (domain, event->event->add, NULL): NULL;
	info->remove_method = event->event->remove ? mono_method_get_object (domain, event->event->remove, NULL): NULL;
	info->raise_method = event->event->raise ? mono_method_get_object (domain, event->event->raise, NULL): NULL;
}

static MonoArray*
ves_icall_Type_GetInterfaces (MonoReflectionType* type)
{
	MonoDomain *domain = mono_object_domain (type); 
	MonoArray *intf;
	int ninterf, i;
	MonoClass *class = mono_class_from_mono_type (type->type);
	MonoClass *parent;
	MonoBitSet *slots = mono_bitset_new (class->max_interface_id + 1, 0);

	MONO_ARCH_SAVE_REGS;

	if (class->rank) {
		/* GetInterfaces() returns an empty array in MS.NET (this may be a bug) */
		return mono_array_new (domain, mono_defaults.monotype_class, 0);
	}

	ninterf = 0;
	for (parent = class; parent; parent = parent->parent) {
		for (i = 0; i < parent->interface_count; ++i) {
			if (mono_bitset_test (slots, parent->interfaces [i]->interface_id))
				continue;

			mono_bitset_set (slots, parent->interfaces [i]->interface_id);
			++ninterf;
		}
	}

	intf = mono_array_new (domain, mono_defaults.monotype_class, ninterf);
	ninterf = 0;
	for (parent = class; parent; parent = parent->parent) {
		for (i = 0; i < parent->interface_count; ++i) {
			if (!mono_bitset_test (slots, parent->interfaces [i]->interface_id))
				continue;

			mono_bitset_clear (slots, parent->interfaces [i]->interface_id);
			mono_array_set (intf, gpointer, ninterf,
					mono_type_get_object (domain, &parent->interfaces [i]->byval_arg));
			++ninterf;
		}
	}

	mono_bitset_free (slots);
	return intf;
}

static void
ves_icall_Type_GetInterfaceMapData (MonoReflectionType *type, MonoReflectionType *iface, MonoArray **targets, MonoArray **methods)
{
	MonoClass *class = mono_class_from_mono_type (type->type);
	MonoClass *iclass = mono_class_from_mono_type (iface->type);
	MonoReflectionMethod *member;
	int i, len, ioffset;
	MonoDomain *domain;

	MONO_ARCH_SAVE_REGS;

	/* type doesn't implement iface: the exception is thrown in managed code */
	if ((iclass->interface_id > class->max_interface_id) || !class->interface_offsets [iclass->interface_id])
			return;

	len = iclass->method.count;
	ioffset = class->interface_offsets [iclass->interface_id];
	domain = mono_object_domain (type);
	*targets = mono_array_new (domain, mono_defaults.method_info_class, len);
	*methods = mono_array_new (domain, mono_defaults.method_info_class, len);
	for (i = 0; i < len; ++i) {
		member = mono_method_get_object (domain, iclass->methods [i], iclass);
		mono_array_set (*methods, gpointer, i, member);
		member = mono_method_get_object (domain, class->vtable [i + ioffset], class);
		mono_array_set (*targets, gpointer, i, member);
	}
}

static MonoReflectionType*
ves_icall_MonoType_GetElementType (MonoReflectionType *type)
{
	MonoClass *class = mono_class_from_mono_type (type->type);

	MONO_ARCH_SAVE_REGS;

	if (type->type->byref)
		return mono_type_get_object (mono_object_domain (type), &class->byval_arg);
	if (class->enumtype && class->enum_basetype) /* types that are modifierd typebuilkders may not have enum_basetype set */
		return mono_type_get_object (mono_object_domain (type), class->enum_basetype);
	else if (class->element_class)
		return mono_type_get_object (mono_object_domain (type), &class->element_class->byval_arg);
	else
		return NULL;
}

static MonoReflectionType*
ves_icall_get_type_parent (MonoReflectionType *type)
{
	MonoClass *class = mono_class_from_mono_type (type->type);

	MONO_ARCH_SAVE_REGS;

	return class->parent ? mono_type_get_object (mono_object_domain (type), &class->parent->byval_arg): NULL;
}

static MonoBoolean
ves_icall_type_ispointer (MonoReflectionType *type)
{
	MONO_ARCH_SAVE_REGS;

	return type->type->type == MONO_TYPE_PTR;
}

static MonoBoolean
ves_icall_type_isprimitive (MonoReflectionType *type)
{
	MONO_ARCH_SAVE_REGS;

	return (!type->type->byref && (type->type->type >= MONO_TYPE_BOOLEAN) && (type->type->type <= MONO_TYPE_R8));
}

static MonoBoolean
ves_icall_type_isbyref (MonoReflectionType *type)
{
	MONO_ARCH_SAVE_REGS;

	return type->type->byref;
}

static MonoReflectionModule*
ves_icall_MonoType_get_Module (MonoReflectionType *type)
{
	MonoClass *class = mono_class_from_mono_type (type->type);

	MONO_ARCH_SAVE_REGS;

	return mono_module_get_object (mono_object_domain (type), class->image);
}

static MonoReflectionAssembly*
ves_icall_MonoType_get_Assembly (MonoReflectionType *type)
{
	MonoDomain *domain = mono_domain_get (); 
	MonoClass *class = mono_class_from_mono_type (type->type);

	MONO_ARCH_SAVE_REGS;

	return mono_assembly_get_object (domain, class->image->assembly);
}

static MonoReflectionType*
ves_icall_MonoType_get_DeclaringType (MonoReflectionType *type)
{
	MonoDomain *domain = mono_domain_get (); 
	MonoClass *class = mono_class_from_mono_type (type->type);

	MONO_ARCH_SAVE_REGS;

	return class->nested_in ? mono_type_get_object (domain, &class->nested_in->byval_arg) : NULL;
}

static MonoReflectionType*
ves_icall_MonoType_get_UnderlyingSystemType (MonoReflectionType *type)
{
	MonoDomain *domain = mono_domain_get (); 
	MonoClass *class = mono_class_from_mono_type (type->type);

	MONO_ARCH_SAVE_REGS;

	if (class->enumtype && class->enum_basetype) /* types that are modified typebuilders may not have enum_basetype set */
		return mono_type_get_object (domain, class->enum_basetype);
	else if (class->element_class)
		return mono_type_get_object (domain, &class->element_class->byval_arg);
	else
		return NULL;
}

static MonoString*
ves_icall_MonoType_get_Name (MonoReflectionType *type)
{
	MonoDomain *domain = mono_domain_get (); 
	MonoClass *class = mono_class_from_mono_type (type->type);

	MONO_ARCH_SAVE_REGS;

	return mono_string_new (domain, class->name);
}

static MonoString*
ves_icall_MonoType_get_Namespace (MonoReflectionType *type)
{
	MonoDomain *domain = mono_domain_get (); 
	MonoClass *class = mono_class_from_mono_type (type->type);

	MONO_ARCH_SAVE_REGS;

	while (class->nested_in)
		class = class->nested_in;

	return mono_string_new (domain, class->name_space);
}

static gint32
ves_icall_MonoType_GetArrayRank (MonoReflectionType *type)
{
	MonoClass *class = mono_class_from_mono_type (type->type);

	MONO_ARCH_SAVE_REGS;

	return class->rank;
}

static MonoArray*
ves_icall_Type_GetGenericArguments (MonoReflectionType *type)
{
	MonoArray *res;
	MonoClass *klass, *pklass;
	int i;
	MONO_ARCH_SAVE_REGS;

	klass = mono_class_from_mono_type (type->type);

	if (type->type->byref) {
		res = mono_array_new (mono_object_domain (type), mono_defaults.monotype_class, 0);
	} else if (klass->gen_params) {
		res = mono_array_new (mono_object_domain (type), mono_defaults.monotype_class, klass->num_gen_params);
		for (i = 0; i < klass->num_gen_params; ++i) {
			pklass = mono_class_from_generic_parameter (&klass->gen_params [i], klass->image, FALSE);
			mono_array_set (res, gpointer, i, mono_type_get_object (mono_object_domain (type), &pklass->byval_arg));
		}
	} else if (klass->generic_inst) {
		MonoGenericInst *inst = klass->generic_inst->data.generic_inst;
		res = mono_array_new (mono_object_domain (type), mono_defaults.monotype_class, inst->type_argc);
		for (i = 0; i < inst->type_argc; ++i) {
			mono_array_set (res, gpointer, i, mono_type_get_object (mono_object_domain (type), inst->type_argv [i]));
		}
	} else {
		res = mono_array_new (mono_object_domain (type), mono_defaults.monotype_class, 0);
	}
	return res;
}

static gboolean
ves_icall_Type_get_IsGenericTypeDefinition (MonoReflectionType *type)
{
	MonoClass *klass;
	MONO_ARCH_SAVE_REGS;

	if (type->type->byref)
		return FALSE;
	klass = mono_class_from_mono_type (type->type);

	return klass->gen_params != NULL;
}

static MonoReflectionType*
ves_icall_Type_GetGenericTypeDefinition_impl (MonoReflectionType *type)
{
	MonoClass *klass;
	MONO_ARCH_SAVE_REGS;

	if (type->type->byref)
		return NULL;
	klass = mono_class_from_mono_type (type->type);
	if (klass->gen_params) {
		return type; /* check this one */
	}
	if (klass->generic_inst) {
		MonoType *generic_type = klass->generic_inst->data.generic_inst->generic_type;
		MonoClass *generic_class = mono_class_from_mono_type (generic_type);

		if (generic_class->wastypebuilder && generic_class->reflection_info)
			return generic_class->reflection_info;
		else
			return mono_type_get_object (mono_object_domain (type), generic_type);
	}
	return NULL;
}

static MonoReflectionGenericInst*
ves_icall_Type_BindGenericParameters (MonoReflectionType *type, MonoArray *types)
{
	MONO_ARCH_SAVE_REGS;

	if (type->type->byref)
		return NULL;

	return mono_reflection_bind_generic_parameters (type, types);
}

static gboolean
ves_icall_Type_get_IsGenericInstance (MonoReflectionType *type)
{
	MonoClass *klass;
	MONO_ARCH_SAVE_REGS;

	if (type->type->byref)
		return FALSE;
	klass = mono_class_from_mono_type (type->type);
	return klass->generic_inst != NULL;
}

static gint32
ves_icall_Type_GetGenericParameterPosition (MonoReflectionType *type)
{
	MONO_ARCH_SAVE_REGS;

	if (type->type->byref)
		return -1;
	if (type->type->type == MONO_TYPE_VAR || type->type->type == MONO_TYPE_MVAR)
		return type->type->data.generic_param->num;
	return -1;
}

static MonoBoolean
ves_icall_MonoType_get_HasGenericArguments (MonoReflectionType *type)
{
	MonoClass *klass;
	MONO_ARCH_SAVE_REGS;

	if (type->type->byref)
		return FALSE;
	klass = mono_class_from_mono_type (type->type);
	if (klass->gen_params || klass->generic_inst)
		return TRUE;
	return FALSE;
}

static MonoBoolean
ves_icall_MonoType_get_IsGenericParameter (MonoReflectionType *type)
{
	MONO_ARCH_SAVE_REGS;

	if (type->type->byref)
		return FALSE;
	if (type->type->type == MONO_TYPE_VAR || type->type->type == MONO_TYPE_MVAR)
		return TRUE;
	return FALSE;
}

static MonoBoolean
ves_icall_TypeBuilder_get_IsGenericParameter (MonoReflectionTypeBuilder *tb)
{
	MONO_ARCH_SAVE_REGS;

	if (tb->type.type->byref)
		return FALSE;
	if (tb->type.type->type == MONO_TYPE_VAR || tb->type.type->type == MONO_TYPE_MVAR)
		return TRUE;
	return FALSE;
}

static MonoReflectionType*
ves_icall_TypeBuilder_define_generic_parameter (MonoReflectionTypeBuilder *tb, MonoReflectionGenericParam *gparam)
{
	guint32 index;

	MONO_ARCH_SAVE_REGS;

	index = mono_array_length (tb->generic_params) - 1;
	return mono_reflection_define_generic_parameter (tb, NULL, index, gparam);
}

static MonoReflectionType*
ves_icall_MethodBuilder_define_generic_parameter (MonoReflectionMethodBuilder *mb, MonoReflectionGenericParam *gparam)
{
	guint32 index;

	MONO_ARCH_SAVE_REGS;

	index = mono_array_length (mb->generic_params) - 1;
	return mono_reflection_define_generic_parameter (NULL, mb, index, gparam);
}

static MonoReflectionMethod *
ves_icall_MonoType_get_DeclaringMethod (MonoReflectionType *type)
{
	MonoMethod *method;
	MonoClass *klass;

	MONO_ARCH_SAVE_REGS;

	if (type->type->byref)
		return FALSE;

	method = type->type->data.generic_param->method;
	if (!method)
		return NULL;

	klass = mono_class_from_mono_type (type->type);
	return mono_method_get_object (mono_object_domain (type), method, klass);
}

static gboolean
ves_icall_MethodInfo_get_IsGenericMethodDefinition (MonoReflectionMethod *method)
{
	MonoMethodNormal *mn;
	MONO_ARCH_SAVE_REGS;

	if ((method->method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) ||
	    (method->method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL))
		return FALSE;

	mn = (MonoMethodNormal *) method->method;
	return mn->header->gen_params != NULL;
}

static MonoArray*
ves_icall_MonoMethod_GetGenericArguments (MonoReflectionMethod *method)
{
	MonoMethodNormal *mn;
	MonoArray *res;
	int count, i;
	MONO_ARCH_SAVE_REGS;

	if ((method->method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) ||
	    (method->method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL))
		return mono_array_new (mono_object_domain (method), mono_defaults.monotype_class, 0);

	mn = (MonoMethodNormal *) method->method;
	count = method->method->signature->generic_param_count;
	res = mono_array_new (mono_object_domain (method), mono_defaults.monotype_class, count);

	for (i = 0; i < count; i++) {
		MonoGenericParam *param = &mn->header->gen_params [i];
		MonoClass *pklass = mono_class_from_generic_parameter (param, method->method->klass->image, TRUE);
		mono_array_set (res, gpointer, i, mono_type_get_object (mono_object_domain (method), &pklass->byval_arg));
	}

	return res;
}

static MonoObject *
ves_icall_InternalInvoke (MonoReflectionMethod *method, MonoObject *this, MonoArray *params) 
{
	/* 
	 * Invoke from reflection is supposed to always be a virtual call (the API
	 * is stupid), mono_runtime_invoke_*() calls the provided method, allowing
	 * greater flexibility.
	 */
	MonoMethod *m = method->method;
	int pcount;

	MONO_ARCH_SAVE_REGS;

	if (this) {
		if (!mono_object_isinst (this, m->klass))
			mono_raise_exception (mono_exception_from_name (mono_defaults.corlib, "System.Reflection", "TargetException"));
		m = mono_object_get_virtual_method (this, m);
	} else if (!(m->flags & METHOD_ATTRIBUTE_STATIC) && strcmp (m->name, ".ctor") && !m->wrapper_type)
		mono_raise_exception (mono_exception_from_name (mono_defaults.corlib, "System.Reflection", "TargetException"));

	pcount = params? mono_array_length (params): 0;
	if (pcount != m->signature->param_count)
		mono_raise_exception (mono_exception_from_name (mono_defaults.corlib, "System.Reflection", "TargetParameterCountException"));

	if (m->klass->rank && !strcmp (m->name, ".ctor")) {
		int i;
		guint32 *lengths;
		guint32 *lower_bounds;
		pcount = mono_array_length (params);
		lengths = alloca (sizeof (guint32) * pcount);
		for (i = 0; i < pcount; ++i)
			lengths [i] = *(gint32*) ((char*)mono_array_get (params, gpointer, i) + sizeof (MonoObject));

		if (m->klass->rank == pcount) {
			/* Only lengths provided. */
			lower_bounds = NULL;
		} else {
			g_assert (pcount == (m->klass->rank * 2));
			/* lower bounds are first. */
			lower_bounds = lengths;
			lengths += m->klass->rank;
		}

		return (MonoObject*)mono_array_new_full (mono_object_domain (params), m->klass, lengths, lower_bounds);
	}
	return mono_runtime_invoke_array (m, this, params, NULL);
}

static MonoObject *
ves_icall_InternalExecute (MonoReflectionMethod *method, MonoObject *this, MonoArray *params, MonoArray **outArgs) 
{
	MonoDomain *domain = mono_object_domain (method); 
	MonoMethod *m = method->method;
	MonoMethodSignature *sig = m->signature;
	MonoArray *out_args;
	MonoObject *result;
	int i, j, outarg_count = 0;

	MONO_ARCH_SAVE_REGS;

	if (m->klass == mono_defaults.object_class) {

		if (!strcmp (m->name, "FieldGetter")) {
			MonoClass *k = this->vtable->klass;
			MonoString *name = mono_array_get (params, MonoString *, 1);
			char *str;

			str = mono_string_to_utf8 (name);
		
			for (i = 0; i < k->field.count; i++) {
				if (!strcmp (k->fields [i].name, str)) {
					MonoClass *field_klass =  mono_class_from_mono_type (k->fields [i].type);
					if (field_klass->valuetype)
						result = mono_value_box (domain, field_klass,
									 (char *)this + k->fields [i].offset);
					else 
						result = *((gpointer *)((char *)this + k->fields [i].offset));
				
					g_assert (result);
					out_args = mono_array_new (domain, mono_defaults.object_class, 1);
					*outArgs = out_args;
					mono_array_set (out_args, gpointer, 0, result);
					g_free (str);
					return NULL;
				}
			}

			g_free (str);
			g_assert_not_reached ();

		} else if (!strcmp (m->name, "FieldSetter")) {
			MonoClass *k = this->vtable->klass;
			MonoString *name = mono_array_get (params, MonoString *, 1);
			int size, align;
			char *str;

			str = mono_string_to_utf8 (name);
		
			for (i = 0; i < k->field.count; i++) {
				if (!strcmp (k->fields [i].name, str)) {
					MonoClass *field_klass =  mono_class_from_mono_type (k->fields [i].type);
					MonoObject *val = mono_array_get (params, gpointer, 2);

					if (field_klass->valuetype) {
						size = mono_type_size (k->fields [i].type, &align);
						memcpy ((char *)this + k->fields [i].offset, 
							((char *)val) + sizeof (MonoObject), size);
					} else 
						*(MonoObject**)((char *)this + k->fields [i].offset) = val;
				
					out_args = mono_array_new (domain, mono_defaults.object_class, 0);
					*outArgs = out_args;

					g_free (str);
					return NULL;
				}
			}

			g_free (str);
			g_assert_not_reached ();

		}
	}

	for (i = 0; i < mono_array_length (params); i++) {
		if (sig->params [i]->byref) 
			outarg_count++;
	}

	out_args = mono_array_new (domain, mono_defaults.object_class, outarg_count);
	
	/* fixme: handle constructors? */
	if (!strcmp (method->method->name, ".ctor"))
		g_assert_not_reached ();

	result = mono_runtime_invoke_array (method->method, this, params, NULL);

	for (i = 0, j = 0; i < mono_array_length (params); i++) {
		if (sig->params [i]->byref) {
			gpointer arg;
			arg = mono_array_get (params, gpointer, i);
			mono_array_set (out_args, gpointer, j, arg);
			j++;
		}
	}

	*outArgs = out_args;

	return result;
}

static MonoObject *
ves_icall_System_Enum_ToObject (MonoReflectionType *type, MonoObject *obj)
{
	MonoDomain *domain; 
	MonoClass *enumc, *objc;
	gint32 s1, s2;
	MonoObject *res;
	
	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (type);
	MONO_CHECK_ARG_NULL (obj);

	domain = mono_object_domain (type); 
	enumc = mono_class_from_mono_type (type->type);
	objc = obj->vtable->klass;

	MONO_CHECK_ARG (obj, enumc->enumtype == TRUE);
	MONO_CHECK_ARG (obj, (objc->enumtype) || (objc->byval_arg.type >= MONO_TYPE_I1 &&
						  objc->byval_arg.type <= MONO_TYPE_U8));
	
	s1 = mono_class_value_size (enumc, NULL);
	s2 = mono_class_value_size (objc, NULL);

	res = mono_object_new (domain, enumc);

#if G_BYTE_ORDER == G_LITTLE_ENDIAN
	memcpy ((char *)res + sizeof (MonoObject), (char *)obj + sizeof (MonoObject), MIN (s1, s2));
#else
	memcpy ((char *)res + sizeof (MonoObject) + (s1 > s2 ? s1 - s2 : 0),
		(char *)obj + sizeof (MonoObject) + (s2 > s1 ? s2 - s1 : 0),
		MIN (s1, s2));
#endif
	return res;
}

static MonoObject *
ves_icall_System_Enum_get_value (MonoObject *this)
{
	MonoObject *res;
	MonoClass *enumc;
	gpointer dst;
	gpointer src;
	int size;

	MONO_ARCH_SAVE_REGS;

	if (!this)
		return NULL;

	g_assert (this->vtable->klass->enumtype);
	
	enumc = mono_class_from_mono_type (this->vtable->klass->enum_basetype);
	res = mono_object_new (mono_object_domain (this), enumc);
	dst = (char *)res + sizeof (MonoObject);
	src = (char *)this + sizeof (MonoObject);
	size = mono_class_value_size (enumc, NULL);

	memcpy (dst, src, size);

	return res;
}

static void
ves_icall_get_enum_info (MonoReflectionType *type, MonoEnumInfo *info)
{
	MonoDomain *domain = mono_object_domain (type); 
	MonoClass *enumc = mono_class_from_mono_type (type->type);
	guint i, j, nvalues, crow;
	MonoClassField *field;
	
	MONO_ARCH_SAVE_REGS;

	info->utype = mono_type_get_object (domain, enumc->enum_basetype);
	nvalues = enumc->field.count - 1;
	info->names = mono_array_new (domain, mono_defaults.string_class, nvalues);
	info->values = mono_array_new (domain, enumc, nvalues);
	
	crow = -1;
	for (i = 0, j = 0; i < enumc->field.count; ++i) {
		field = &enumc->fields [i];
		if (strcmp ("value__", field->name) == 0)
			continue;
		mono_array_set (info->names, gpointer, j, mono_string_new (domain, field->name));
		if (!field->data) {
			crow = mono_metadata_get_constant_index (enumc->image, MONO_TOKEN_FIELD_DEF | (i+enumc->field.first+1), crow + 1);
			crow = mono_metadata_decode_row_col (&enumc->image->tables [MONO_TABLE_CONSTANT], crow-1, MONO_CONSTANT_VALUE);
			/* 1 is the length of the blob */
			field->data = 1 + mono_metadata_blob_heap (enumc->image, crow);
		}
		switch (enumc->enum_basetype->type) {
		case MONO_TYPE_U1:
		case MONO_TYPE_I1:
			mono_array_set (info->values, gchar, j, *field->data);
			break;
		case MONO_TYPE_CHAR:
		case MONO_TYPE_U2:
		case MONO_TYPE_I2:
			mono_array_set (info->values, gint16, j, read16 (field->data));
			break;
		case MONO_TYPE_U4:
		case MONO_TYPE_I4:
			mono_array_set (info->values, gint32, j, read32 (field->data));
			break;
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
			mono_array_set (info->values, gint64, j, read64 (field->data));
			break;
		default:
			g_error ("Implement type 0x%02x in get_enum_info", enumc->enum_basetype->type);
		}
		++j;
	}
}

enum {
	BFLAGS_IgnoreCase = 1,
	BFLAGS_DeclaredOnly = 2,
	BFLAGS_Instance = 4,
	BFLAGS_Static = 8,
	BFLAGS_Public = 0x10,
	BFLAGS_NonPublic = 0x20,
	BFLAGS_InvokeMethod = 0x100,
	BFLAGS_CreateInstance = 0x200,
	BFLAGS_GetField = 0x400,
	BFLAGS_SetField = 0x800,
	BFLAGS_GetProperty = 0x1000,
	BFLAGS_SetProperty = 0x2000,
	BFLAGS_ExactBinding = 0x10000,
	BFLAGS_SuppressChangeType = 0x20000,
	BFLAGS_OptionalParamBinding = 0x40000
};

static MonoReflectionField *
ves_icall_Type_GetField (MonoReflectionType *type, MonoString *name, guint32 bflags)
{
	MonoDomain *domain; 
	MonoClass *startklass, *klass;
	int i, match;
	MonoClassField *field;
	char *utf8_name;
	domain = ((MonoObject *)type)->vtable->domain;
	klass = startklass = mono_class_from_mono_type (type->type);

	MONO_ARCH_SAVE_REGS;

	if (!name)
		mono_raise_exception (mono_get_exception_argument_null ("name"));

handle_parent:	
	for (i = 0; i < klass->field.count; ++i) {
		match = 0;
		field = &klass->fields [i];
		if ((field->type->attrs & FIELD_ATTRIBUTE_FIELD_ACCESS_MASK) == FIELD_ATTRIBUTE_PUBLIC) {
			if (bflags & BFLAGS_Public)
				match++;
		} else {
			if (bflags & BFLAGS_NonPublic)
				match++;
		}
		if (!match)
			continue;
		match = 0;
		if (field->type->attrs & FIELD_ATTRIBUTE_STATIC) {
			if (bflags & BFLAGS_Static)
				match++;
		} else {
			if (bflags & BFLAGS_Instance)
				match++;
		}

		if (!match)
			continue;
		
		utf8_name = mono_string_to_utf8 (name);

		if (strcmp (field->name, utf8_name)) {
			g_free (utf8_name);
			continue;
		}
		g_free (utf8_name);
		
		return mono_field_get_object (domain, klass, field);
	}
	if (!(bflags & BFLAGS_DeclaredOnly) && (klass = klass->parent))
		goto handle_parent;

	return NULL;
}

static MonoArray*
ves_icall_Type_GetFields (MonoReflectionType *type, guint32 bflags)
{
	MonoDomain *domain; 
	GSList *l = NULL, *tmp;
	MonoClass *startklass, *klass;
	MonoArray *res;
	MonoObject *member;
	int i, len, match;
	MonoClassField *field;

	MONO_ARCH_SAVE_REGS;

	domain = ((MonoObject *)type)->vtable->domain;
	klass = startklass = mono_class_from_mono_type (type->type);

handle_parent:	
	for (i = 0; i < klass->field.count; ++i) {
		match = 0;
		field = &klass->fields [i];
		if ((field->type->attrs & FIELD_ATTRIBUTE_FIELD_ACCESS_MASK) == FIELD_ATTRIBUTE_PUBLIC) {
			if (bflags & BFLAGS_Public)
				match++;
		} else {
			if (bflags & BFLAGS_NonPublic)
				match++;
		}
		if (!match)
			continue;
		match = 0;
		if (field->type->attrs & FIELD_ATTRIBUTE_STATIC) {
			if (bflags & BFLAGS_Static)
				match++;
		} else {
			if (bflags & BFLAGS_Instance)
				match++;
		}

		if (!match)
			continue;
		member = (MonoObject*)mono_field_get_object (domain, klass, field);
		l = g_slist_prepend (l, member);
	}
	if (!(bflags & BFLAGS_DeclaredOnly) && (klass = klass->parent))
		goto handle_parent;
	len = g_slist_length (l);
	res = mono_array_new (domain, mono_defaults.field_info_class, len);
	i = 0;
	tmp = l = g_slist_reverse (l);
	for (; tmp; tmp = tmp->next, ++i)
		mono_array_set (res, gpointer, i, tmp->data);
	g_slist_free (l);
	return res;
}

static MonoArray*
ves_icall_Type_GetMethods (MonoReflectionType *type, guint32 bflags)
{
	MonoDomain *domain; 
	GSList *l = NULL, *tmp;
	MonoClass *startklass, *klass;
	MonoArray *res;
	MonoMethod *method;
	MonoObject *member;
	int i, len, match;
	GHashTable *method_slots = g_hash_table_new (NULL, NULL);
		
	MONO_ARCH_SAVE_REGS;

	domain = ((MonoObject *)type)->vtable->domain;
	klass = startklass = mono_class_from_mono_type (type->type);
	len = 0;

handle_parent:
	for (i = 0; i < klass->method.count; ++i) {
		match = 0;
		method = klass->methods [i];
		if (strcmp (method->name, ".ctor") == 0 || strcmp (method->name, ".cctor") == 0)
			continue;
		if ((method->flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) == METHOD_ATTRIBUTE_PUBLIC) {
			if (bflags & BFLAGS_Public)
				match++;
		} else {
			if (bflags & BFLAGS_NonPublic)
				match++;
		}
		if (!match)
			continue;
		match = 0;
		if (method->flags & METHOD_ATTRIBUTE_STATIC) {
			if (bflags & BFLAGS_Static)
				match++;
		} else {
			if (bflags & BFLAGS_Instance)
				match++;
		}

		if (!match)
			continue;
		match = 0;
		if (g_hash_table_lookup (method_slots, GUINT_TO_POINTER (method->slot)))
			continue;
		g_hash_table_insert (method_slots, GUINT_TO_POINTER (method->slot), method);
		member = (MonoObject*)mono_method_get_object (domain, method, startklass);
		
		l = g_slist_prepend (l, member);
		len++;
	}
	if (!(bflags & BFLAGS_DeclaredOnly) && (klass = klass->parent))
		goto handle_parent;
	res = mono_array_new (domain, mono_defaults.method_info_class, len);
	i = 0;

	tmp = l = g_slist_reverse (l);

	for (; tmp; tmp = tmp->next, ++i)
		mono_array_set (res, gpointer, i, tmp->data);
	g_slist_free (l);
	g_hash_table_destroy (method_slots);
	return res;
}

static MonoArray*
ves_icall_Type_GetConstructors (MonoReflectionType *type, guint32 bflags)
{
	MonoDomain *domain; 
	GSList *l = NULL, *tmp;
	static MonoClass *System_Reflection_ConstructorInfo;
	MonoClass *startklass, *klass;
	MonoArray *res;
	MonoMethod *method;
	MonoObject *member;
	int i, len, match;

	MONO_ARCH_SAVE_REGS;

	domain = ((MonoObject *)type)->vtable->domain;
	klass = startklass = mono_class_from_mono_type (type->type);

	for (i = 0; i < klass->method.count; ++i) {
		match = 0;
		method = klass->methods [i];
		if (strcmp (method->name, ".ctor") && strcmp (method->name, ".cctor"))
			continue;
		if ((method->flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) == METHOD_ATTRIBUTE_PUBLIC) {
			if (bflags & BFLAGS_Public)
				match++;
		} else {
			if (bflags & BFLAGS_NonPublic)
				match++;
		}
		if (!match)
			continue;
		match = 0;
		if (method->flags & METHOD_ATTRIBUTE_STATIC) {
			if (bflags & BFLAGS_Static)
				match++;
		} else {
			if (bflags & BFLAGS_Instance)
				match++;
		}

		if (!match)
			continue;
		member = (MonoObject*)mono_method_get_object (domain, method, startklass);
			
		l = g_slist_prepend (l, member);
	}
	len = g_slist_length (l);
	if (!System_Reflection_ConstructorInfo)
		System_Reflection_ConstructorInfo = mono_class_from_name (
			mono_defaults.corlib, "System.Reflection", "ConstructorInfo");
	res = mono_array_new (domain, System_Reflection_ConstructorInfo, len);
	i = 0;
	tmp = l = g_slist_reverse (l);
	for (; tmp; tmp = tmp->next, ++i)
		mono_array_set (res, gpointer, i, tmp->data);
	g_slist_free (l);
	return res;
}

static MonoArray*
ves_icall_Type_GetProperties (MonoReflectionType *type, guint32 bflags)
{
	MonoDomain *domain; 
	GSList *l = NULL, *tmp;
	static MonoClass *System_Reflection_PropertyInfo;
	MonoClass *startklass, *klass;
	MonoArray *res;
	MonoMethod *method;
	MonoProperty *prop;
	int i, match;
	int len = 0;
	GHashTable *method_slots = g_hash_table_new (NULL, NULL);

	MONO_ARCH_SAVE_REGS;

	domain = ((MonoObject *)type)->vtable->domain;
	klass = startklass = mono_class_from_mono_type (type->type);

handle_parent:
	for (i = 0; i < klass->property.count; ++i) {
		prop = &klass->properties [i];
		match = 0;
		method = prop->get;
		if (!method)
			method = prop->set;
		if ((method->flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) == METHOD_ATTRIBUTE_PUBLIC) {
			if (bflags & BFLAGS_Public)
				match++;
		} else {
			if (bflags & BFLAGS_NonPublic)
				match++;
		}
		if (!match)
			continue;
		match = 0;
		if (method->flags & METHOD_ATTRIBUTE_STATIC) {
			if (bflags & BFLAGS_Static)
				match++;
		} else {
			if (bflags & BFLAGS_Instance)
				match++;
		}

		if (!match)
			continue;
		match = 0;

		if (g_hash_table_lookup (method_slots, GUINT_TO_POINTER (method->slot)))
			continue;
		g_hash_table_insert (method_slots, GUINT_TO_POINTER (method->slot), prop);

		l = g_slist_prepend (l, mono_property_get_object (domain, klass, prop));
		len++;
	}
	if ((!(bflags & BFLAGS_DeclaredOnly) && (klass = klass->parent)))
		goto handle_parent;
	if (!System_Reflection_PropertyInfo)
		System_Reflection_PropertyInfo = mono_class_from_name (
			mono_defaults.corlib, "System.Reflection", "PropertyInfo");
	res = mono_array_new (domain, System_Reflection_PropertyInfo, len);
	i = 0;

	tmp = l = g_slist_reverse (l);

	for (; tmp; tmp = tmp->next, ++i)
		mono_array_set (res, gpointer, i, tmp->data);
	g_slist_free (l);
	g_hash_table_destroy (method_slots);
	return res;
}

static MonoReflectionEvent *
ves_icall_MonoType_GetEvent (MonoReflectionType *type, MonoString *name, guint32 bflags)
{
	MonoDomain *domain;
	MonoClass *klass;
	gint i;
	MonoEvent *event;
	MonoMethod *method;
	gchar *event_name;

	MONO_ARCH_SAVE_REGS;

	event_name = mono_string_to_utf8 (name);
	klass = mono_class_from_mono_type (type->type);
	domain = mono_object_domain (type);

handle_parent:	
	for (i = 0; i < klass->event.count; i++) {
		event = &klass->events [i];
		if (strcmp (event->name, event_name))
			continue;

		method = event->add;
		if (!method)
			method = event->remove;

		if ((method->flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) == METHOD_ATTRIBUTE_PUBLIC) {
			if (!(bflags & BFLAGS_Public))
				continue;
		} else {
			if (!(bflags & BFLAGS_NonPublic))
				continue;
		}

		g_free (event_name);
		return mono_event_get_object (domain, klass, event);
	}

	if (!(bflags & BFLAGS_DeclaredOnly) && (klass = klass->parent))
		goto handle_parent;

	g_free (event_name);
	return NULL;
}

static MonoArray*
ves_icall_Type_GetEvents (MonoReflectionType *type, guint32 bflags)
{
	MonoDomain *domain; 
	GSList *l = NULL, *tmp;
	static MonoClass *System_Reflection_EventInfo;
	MonoClass *startklass, *klass;
	MonoArray *res;
	MonoMethod *method;
	MonoEvent *event;
	int i, len, match;

	MONO_ARCH_SAVE_REGS;

	domain = ((MonoObject *)type)->vtable->domain;
	klass = startklass = mono_class_from_mono_type (type->type);

handle_parent:	
	for (i = 0; i < klass->event.count; ++i) {
		event = &klass->events [i];
		match = 0;
		method = event->add;
		if (!method)
			method = event->remove;
		if ((method->flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) == METHOD_ATTRIBUTE_PUBLIC) {
			if (bflags & BFLAGS_Public)
				match++;
		} else {
			if (bflags & BFLAGS_NonPublic)
				match++;
		}
		if (!match)
			continue;
		match = 0;
		if (method->flags & METHOD_ATTRIBUTE_STATIC) {
			if (bflags & BFLAGS_Static)
				match++;
		} else {
			if (bflags & BFLAGS_Instance)
				match++;
		}

		if (!match)
			continue;
		match = 0;
		l = g_slist_prepend (l, mono_event_get_object (domain, klass, event));
	}
	if (!(bflags & BFLAGS_DeclaredOnly) && (klass = klass->parent))
		goto handle_parent;
	len = g_slist_length (l);
	if (!System_Reflection_EventInfo)
		System_Reflection_EventInfo = mono_class_from_name (
			mono_defaults.corlib, "System.Reflection", "EventInfo");
	res = mono_array_new (domain, System_Reflection_EventInfo, len);
	i = 0;

	tmp = l = g_slist_reverse (l);

	for (; tmp; tmp = tmp->next, ++i)
		mono_array_set (res, gpointer, i, tmp->data);
	g_slist_free (l);
	return res;
}

static MonoReflectionType *
ves_icall_Type_GetNestedType (MonoReflectionType *type, MonoString *name, guint32 bflags)
{
	MonoDomain *domain; 
	MonoClass *startklass, *klass;
	MonoClass *nested;
	GList *tmpn;
	char *str;
	
	MONO_ARCH_SAVE_REGS;

	domain = ((MonoObject *)type)->vtable->domain;
	klass = startklass = mono_class_from_mono_type (type->type);
	str = mono_string_to_utf8 (name);

 handle_parent:
	for (tmpn = klass->nested_classes; tmpn; tmpn = tmpn->next) {
		int match = 0;
		nested = tmpn->data;
		if ((nested->flags & TYPE_ATTRIBUTE_VISIBILITY_MASK) == TYPE_ATTRIBUTE_NESTED_PUBLIC) {
			if (bflags & BFLAGS_Public)
				match++;
		} else {
			if (bflags & BFLAGS_NonPublic)
				match++;
		}
		if (!match)
			continue;
		if (strcmp (nested->name, str) == 0){
			g_free (str);
			return mono_type_get_object (domain, &nested->byval_arg);
		}
	}
	if (!(bflags & BFLAGS_DeclaredOnly) && (klass = klass->parent))
		goto handle_parent;
	g_free (str);
	return NULL;
}

static MonoArray*
ves_icall_Type_GetNestedTypes (MonoReflectionType *type, guint32 bflags)
{
	MonoDomain *domain; 
	GSList *l = NULL, *tmp;
	GList *tmpn;
	MonoClass *startklass, *klass;
	MonoArray *res;
	MonoObject *member;
	int i, len, match;
	MonoClass *nested;

	MONO_ARCH_SAVE_REGS;

	domain = ((MonoObject *)type)->vtable->domain;
	klass = startklass = mono_class_from_mono_type (type->type);

	for (tmpn = klass->nested_classes; tmpn; tmpn = tmpn->next) {
		match = 0;
		nested = tmpn->data;
		if ((nested->flags & TYPE_ATTRIBUTE_VISIBILITY_MASK) == TYPE_ATTRIBUTE_NESTED_PUBLIC) {
			if (bflags & BFLAGS_Public)
				match++;
		} else {
			if (bflags & BFLAGS_NonPublic)
				match++;
		}
		if (!match)
			continue;
		member = (MonoObject*)mono_type_get_object (domain, &nested->byval_arg);
		l = g_slist_prepend (l, member);
	}
	len = g_slist_length (l);
	res = mono_array_new (domain, mono_defaults.monotype_class, len);
	i = 0;
	tmp = l = g_slist_reverse (l);
	for (; tmp; tmp = tmp->next, ++i)
		mono_array_set (res, gpointer, i, tmp->data);
	g_slist_free (l);
	return res;
}

static MonoReflectionType*
ves_icall_System_Reflection_Assembly_InternalGetType (MonoReflectionAssembly *assembly, MonoReflectionModule *module, MonoString *name, MonoBoolean throwOnError, MonoBoolean ignoreCase)
{
	gchar *str;
	MonoType *type = NULL;
	MonoTypeNameParse info;

	MONO_ARCH_SAVE_REGS;

	str = mono_string_to_utf8 (name);
	/*g_print ("requested type %s in %s\n", str, assembly->assembly->aname.name);*/
	if (!mono_reflection_parse_type (str, &info)) {
		g_free (str);
		g_list_free (info.modifiers);
		g_list_free (info.nested);
		if (throwOnError) /* uhm: this is a parse error, though... */
			mono_raise_exception (mono_get_exception_type_load (name));
		/*g_print ("failed parse\n");*/
		return NULL;
	}

	if (module != NULL) {
		if (module->image)
			type = mono_reflection_get_type (module->image, &info, ignoreCase);
		else
			type = NULL;
	}
	else
		if (assembly->assembly->dynamic) {
			/* Enumerate all modules */
			MonoReflectionAssemblyBuilder *abuilder = (MonoReflectionAssemblyBuilder*)assembly;
			int i;

			if (!abuilder->modules)
				type = NULL;
			else {
				for (i = 0; i < mono_array_length (abuilder->modules); ++i) {
					MonoReflectionModuleBuilder *mb = mono_array_get (abuilder->modules, MonoReflectionModuleBuilder*, i);
					type = mono_reflection_get_type (&mb->dynamic_image->image, &info, ignoreCase);
					if (type)
						break;
				}
			}
		}
		else
			type = mono_reflection_get_type (assembly->assembly->image, &info, ignoreCase);
	g_free (str);
	g_list_free (info.modifiers);
	g_list_free (info.nested);
	if (!type) {
		if (throwOnError)
			mono_raise_exception (mono_get_exception_type_load (name));
		/* g_print ("failed find\n"); */
		return NULL;
	}
	/* g_print ("got it\n"); */
	return mono_type_get_object (mono_object_domain (assembly), type);

}

static MonoString *
ves_icall_System_Reflection_Assembly_get_code_base (MonoReflectionAssembly *assembly)
{
	MonoDomain *domain = mono_object_domain (assembly); 
	MonoAssembly *mass = assembly->assembly;
	MonoString *res;
	gchar *uri;
	gchar *absolute;
	
	MONO_ARCH_SAVE_REGS;

	absolute = g_build_filename (mass->basedir, mass->image->module_name, NULL);
	uri = g_filename_to_uri (absolute, NULL, NULL);
	res = mono_string_new (domain, uri);
	g_free (uri);
	g_free (absolute);
	return res;
}

static MonoString *
ves_icall_System_Reflection_Assembly_get_location (MonoReflectionAssembly *assembly)
{
	MonoDomain *domain = mono_object_domain (assembly); 
	MonoString *res;
	char *name = g_build_filename (
		assembly->assembly->basedir,
		assembly->assembly->image->module_name, NULL);

	MONO_ARCH_SAVE_REGS;

	res = mono_string_new (domain, name);
	g_free (name);
	return res;
}

static MonoString *
ves_icall_System_Reflection_Assembly_InternalImageRuntimeVersion (MonoReflectionAssembly *assembly)
{
	MonoDomain *domain = mono_object_domain (assembly); 

	MONO_ARCH_SAVE_REGS;

	return mono_string_new (domain, assembly->assembly->image->version);
}

static MonoReflectionMethod*
ves_icall_System_Reflection_Assembly_get_EntryPoint (MonoReflectionAssembly *assembly) 
{
	guint32 token = mono_image_get_entry_point (assembly->assembly->image);

	MONO_ARCH_SAVE_REGS;

	if (!token)
		return NULL;
	return mono_method_get_object (mono_object_domain (assembly), mono_get_method (assembly->assembly->image, token, NULL), NULL);
}

static MonoArray*
ves_icall_System_Reflection_Assembly_GetManifestResourceNames (MonoReflectionAssembly *assembly) 
{
	MonoTableInfo *table = &assembly->assembly->image->tables [MONO_TABLE_MANIFESTRESOURCE];
	MonoArray *result = mono_array_new (mono_object_domain (assembly), mono_defaults.string_class, table->rows);
	int i;
	const char *val;

	MONO_ARCH_SAVE_REGS;

	for (i = 0; i < table->rows; ++i) {
		val = mono_metadata_string_heap (assembly->assembly->image, mono_metadata_decode_row_col (table, i, MONO_MANIFEST_NAME));
		mono_array_set (result, gpointer, i, mono_string_new (mono_object_domain (assembly), val));
	}
	return result;
}

static MonoArray*
ves_icall_System_Reflection_Assembly_GetReferencedAssemblies (MonoReflectionAssembly *assembly) 
{
	static MonoClass *System_Reflection_AssemblyName;
	MonoArray *result;
	MonoAssembly **ptr;
	MonoDomain *domain = mono_object_domain (assembly);
	int i, count = 0;

	MONO_ARCH_SAVE_REGS;

	if (!System_Reflection_AssemblyName)
		System_Reflection_AssemblyName = mono_class_from_name (
			mono_defaults.corlib, "System.Reflection", "AssemblyName");

	for (ptr = assembly->assembly->image->references; ptr && *ptr; ptr++)
		count++;

	result = mono_array_new (mono_object_domain (assembly), System_Reflection_AssemblyName, count);

	for (i = 0; i < count; i++) {
		MonoAssembly *assem = assembly->assembly->image->references [i];
		MonoReflectionAssemblyName *aname;
		char *codebase, *absolute;

		aname = (MonoReflectionAssemblyName *) mono_object_new (
			domain, System_Reflection_AssemblyName);

		if (strcmp (assem->aname.name, "corlib") == 0)
			aname->name = mono_string_new (domain, "mscorlib");
		else
			aname->name = mono_string_new (domain, assem->aname.name);
		aname->major = assem->aname.major;

		absolute = g_build_filename (assem->basedir, assem->image->module_name, NULL);
		codebase = g_filename_to_uri (absolute, NULL, NULL);
		aname->codebase = mono_string_new (domain, codebase);
		g_free (codebase);
		g_free (absolute);
		mono_array_set (result, gpointer, i, aname);
	}
	return result;
}

typedef struct {
	MonoArray *res;
	int idx;
} NameSpaceInfo;

static void
foreach_namespace (const char* key, gconstpointer val, NameSpaceInfo *info)
{
	MonoString *name = mono_string_new (mono_object_domain (info->res), key);

	mono_array_set (info->res, gpointer, info->idx, name);
	info->idx++;
}

static MonoArray*
ves_icall_System_Reflection_Assembly_GetNamespaces (MonoReflectionAssembly *assembly) 
{
	MonoImage *img = assembly->assembly->image;
	int n;
	MonoArray *res;
	NameSpaceInfo info;
	MonoTableInfo  *t = &img->tables [MONO_TABLE_EXPORTEDTYPE];
	int i;

	MONO_ARCH_SAVE_REGS;

	n = g_hash_table_size (img->name_cache);
	res = mono_array_new (mono_object_domain (assembly), mono_defaults.string_class, n);
	info.res = res;
	info.idx = 0;
	g_hash_table_foreach (img->name_cache, (GHFunc)foreach_namespace, &info);

	/* Add namespaces from the EXPORTEDTYPES table as well */
	if (t->rows) {
		MonoArray *res2;
		GPtrArray *nspaces = g_ptr_array_new ();
		for (i = 0; i < t->rows; ++i) {
			const char *nspace = mono_metadata_string_heap (img, mono_metadata_decode_row_col (t, i, MONO_EXP_TYPE_NAMESPACE));
			if (!g_hash_table_lookup (img->name_cache, nspace)) {
				g_ptr_array_add (nspaces, (char*)nspace);
			}
		}
		if (nspaces->len > 0) {
			res2 = mono_array_new (mono_object_domain (assembly), mono_defaults.string_class, n + nspaces->len);
			memcpy (mono_array_addr (res2, MonoString*, 0),
					mono_array_addr (res, MonoString*, 0),
					n * sizeof (MonoString*));
			for (i = 0; i < nspaces->len; ++i)
				mono_array_set (res2, MonoString*, n + i, 
								mono_string_new (mono_object_domain (assembly),
												 g_ptr_array_index (nspaces, i)));
			res = res2;
		}
		g_ptr_array_free (nspaces, TRUE);
	}

	return res;
}

/* move this in some file in mono/util/ */
static char *
g_concat_dir_and_file (const char *dir, const char *file)
{
	g_return_val_if_fail (dir != NULL, NULL);
	g_return_val_if_fail (file != NULL, NULL);

        /*
	 * If the directory name doesn't have a / on the end, we need
	 * to add one so we get a proper path to the file
	 */
	if (dir [strlen(dir) - 1] != G_DIR_SEPARATOR)
		return g_strconcat (dir, G_DIR_SEPARATOR_S, file, NULL);
	else
		return g_strconcat (dir, file, NULL);
}

static void *
ves_icall_System_Reflection_Assembly_GetManifestResourceInternal (MonoReflectionAssembly *assembly, MonoString *name, gint32 *size) 
{
	char *n = mono_string_to_utf8 (name);
	MonoTableInfo *table = &assembly->assembly->image->tables [MONO_TABLE_MANIFESTRESOURCE];
	guint32 i;
	guint32 cols [MONO_MANIFEST_SIZE];
	const char *val;

	MONO_ARCH_SAVE_REGS;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, cols, MONO_MANIFEST_SIZE);
		val = mono_metadata_string_heap (assembly->assembly->image, cols [MONO_MANIFEST_NAME]);
		if (strcmp (val, n) == 0)
			break;
	}
	g_free (n);
	if (i == table->rows)
		return NULL;
	/* FIXME */
	if (cols [MONO_MANIFEST_IMPLEMENTATION]) {
		/*
		 * this code should only be called after obtaining the 
		 * ResourceInfo and handling the other cases.
		 */
		g_assert_not_reached ();
		return NULL;
	}

	return (void*)mono_image_get_resource (assembly->assembly->image, cols [MONO_MANIFEST_OFFSET], size);
}

static gboolean
ves_icall_System_Reflection_Assembly_GetManifestResourceInfoInternal (MonoReflectionAssembly *assembly, MonoString *name, MonoManifestResourceInfo *info)
{
	MonoTableInfo *table = &assembly->assembly->image->tables [MONO_TABLE_MANIFESTRESOURCE];
	int i;
	guint32 cols [MONO_MANIFEST_SIZE];
	guint32 file_cols [MONO_FILE_SIZE];
	const char *val;
	char *n;

	MONO_ARCH_SAVE_REGS;

	n = mono_string_to_utf8 (name);
	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, cols, MONO_MANIFEST_SIZE);
		val = mono_metadata_string_heap (assembly->assembly->image, cols [MONO_MANIFEST_NAME]);
		if (strcmp (val, n) == 0)
			break;
	}
	g_free (n);
	if (i == table->rows)
		return FALSE;

	if (!cols [MONO_MANIFEST_IMPLEMENTATION]) {
		info->location = RESOURCE_LOCATION_EMBEDDED | RESOURCE_LOCATION_IN_MANIFEST;
	}
	else {
		switch (cols [MONO_MANIFEST_IMPLEMENTATION] & IMPLEMENTATION_MASK) {
		case IMPLEMENTATION_FILE:
			i = cols [MONO_MANIFEST_IMPLEMENTATION] >> IMPLEMENTATION_BITS;
			table = &assembly->assembly->image->tables [MONO_TABLE_FILE];
			mono_metadata_decode_row (table, i - 1, file_cols, MONO_FILE_SIZE);
			val = mono_metadata_string_heap (assembly->assembly->image, file_cols [MONO_FILE_NAME]);
			info->filename = mono_string_new (mono_object_domain (assembly), val);
			if (file_cols [MONO_FILE_FLAGS] && FILE_CONTAINS_NO_METADATA)
				info->location = 0;
			else
				info->location = RESOURCE_LOCATION_EMBEDDED;
			break;

		case IMPLEMENTATION_ASSEMBLYREF:
			i = cols [MONO_MANIFEST_IMPLEMENTATION] >> IMPLEMENTATION_BITS;
			info->assembly = mono_assembly_get_object (mono_domain_get (), assembly->assembly->image->references [i - 1]);

			// Obtain info recursively
			ves_icall_System_Reflection_Assembly_GetManifestResourceInfoInternal (info->assembly, name, info);
			info->location |= RESOURCE_LOCATION_ANOTHER_ASSEMBLY;
			break;

		case IMPLEMENTATION_EXP_TYPE:
			g_assert_not_reached ();
			break;
		}
	}

	return TRUE;
}

static MonoObject*
ves_icall_System_Reflection_Assembly_GetFilesInternal (MonoReflectionAssembly *assembly, MonoString *name) 
{
	MonoTableInfo *table = &assembly->assembly->image->tables [MONO_TABLE_FILE];
	MonoArray *result = NULL;
	int i;
	const char *val;
	char *n;

	MONO_ARCH_SAVE_REGS;

	/* check hash if needed */
	if (name) {
		n = mono_string_to_utf8 (name);
		for (i = 0; i < table->rows; ++i) {
			val = mono_metadata_string_heap (assembly->assembly->image, mono_metadata_decode_row_col (table, i, MONO_FILE_NAME));
			if (strcmp (val, n) == 0) {
				MonoString *fn;
				g_free (n);
				n = g_concat_dir_and_file (assembly->assembly->basedir, val);
				fn = mono_string_new (mono_object_domain (assembly), n);
				g_free (n);
				return (MonoObject*)fn;
			}
		}
		g_free (n);
		return NULL;
	}

	for (i = 0; i < table->rows; ++i) {
		result = mono_array_new (mono_object_domain (assembly), mono_defaults.string_class, table->rows);
		val = mono_metadata_string_heap (assembly->assembly->image, mono_metadata_decode_row_col (table, i, MONO_FILE_NAME));
		n = g_concat_dir_and_file (assembly->assembly->basedir, val);
		mono_array_set (result, gpointer, i, mono_string_new (mono_object_domain (assembly), n));
		g_free (n);
	}
	return (MonoObject*)result;
}

static MonoArray*
ves_icall_System_Reflection_Assembly_GetModulesInternal (MonoReflectionAssembly *assembly)
{
	MonoDomain *domain = mono_domain_get();
	MonoArray *res;
	MonoClass *klass;
	int i, module_count = 0, file_count = 0;
	MonoImage **modules = assembly->assembly->image->modules;
	MonoTableInfo *table;

	if (modules) {
		while (modules[module_count])
			++module_count;
	}

	table = &assembly->assembly->image->tables [MONO_TABLE_FILE];
	file_count = table->rows;

	g_assert( assembly->assembly->image != NULL);
	++module_count;

	klass = mono_class_from_name ( mono_defaults.corlib, "System.Reflection", "Module");
	res = mono_array_new (domain, klass, module_count + file_count);

	mono_array_set (res, gpointer, 0, mono_module_get_object (domain, assembly->assembly->image));
	for ( i = 1; i < module_count; ++i )
		mono_array_set (res, gpointer, i, mono_module_get_object (domain, modules[i]));

	for (i = 0; i < table->rows; ++i)
		mono_array_set (res, gpointer, module_count + i, mono_module_file_get_object (domain, assembly->assembly->image, i));

	return res;
}

static MonoReflectionMethod*
ves_icall_GetCurrentMethod (void) 
{
	MonoMethod *m = mono_method_get_last_managed ();

	MONO_ARCH_SAVE_REGS;

	return mono_method_get_object (mono_domain_get (), m, NULL);
}

static MonoReflectionAssembly*
ves_icall_System_Reflection_Assembly_GetExecutingAssembly (void)
{
	MonoMethod *m = mono_method_get_last_managed ();

	MONO_ARCH_SAVE_REGS;

	return mono_assembly_get_object (mono_domain_get (), m->klass->image->assembly);
}


static gboolean
get_caller (MonoMethod *m, gint32 no, gint32 ilo, gboolean managed, gpointer data)
{
	MonoMethod **dest = data;

	/* skip unmanaged frames */
	if (!managed)
		return FALSE;

	if (m == *dest) {
		*dest = NULL;
		return FALSE;
	}
	if (!(*dest)) {
		*dest = m;
		return TRUE;
	}
	return FALSE;
}

static MonoReflectionAssembly*
ves_icall_System_Reflection_Assembly_GetEntryAssembly (void)
{
	MonoDomain* domain = mono_domain_get ();

	MONO_ARCH_SAVE_REGS;

	if (!domain->entry_assembly)
		domain = mono_root_domain;

	return mono_assembly_get_object (domain, domain->entry_assembly);
}


static MonoReflectionAssembly*
ves_icall_System_Reflection_Assembly_GetCallingAssembly (void)
{
	MonoMethod *m = mono_method_get_last_managed ();
	MonoMethod *dest = m;

	MONO_ARCH_SAVE_REGS;

	mono_stack_walk (get_caller, &dest);
	if (!dest)
		dest = m;
	return mono_assembly_get_object (mono_domain_get (), dest->klass->image->assembly);
}

static MonoString *
ves_icall_System_MonoType_getFullName (MonoReflectionType *object)
{
	MonoDomain *domain = mono_object_domain (object); 
	MonoString *res;
	gchar *name;

	MONO_ARCH_SAVE_REGS;

	name = mono_type_get_name (object->type);
	res = mono_string_new (domain, name);
	g_free (name);

	return res;
}

static void
fill_reflection_assembly_name (MonoDomain *domain, MonoReflectionAssemblyName *aname, MonoAssemblyName *name, const char *absolute)
{
	static MonoMethod *create_culture = NULL;
    gpointer args [1];
	guint32 pkey_len;
	const char *pkey_ptr;
	gchar *codebase;

	MONO_ARCH_SAVE_REGS;

	if (strcmp (name->name, "corlib") == 0)
		aname->name = mono_string_new (domain, "mscorlib");
	else
		aname->name = mono_string_new (domain, name->name);

	aname->major = name->major;
	aname->minor = name->minor;
	aname->build = name->build;
	aname->revision = name->revision;
	aname->hashalg = name->hash_alg;

	codebase = g_filename_to_uri (absolute, NULL, NULL);
	if (codebase) {
		aname->codebase = mono_string_new (domain, codebase);
		g_free (codebase);
	}

	if (!create_culture) {
		MonoMethodDesc *desc = mono_method_desc_new ("System.Globalization.CultureInfo:CreateSpecificCulture(string)", TRUE);
		create_culture = mono_method_desc_search_in_image (desc, mono_defaults.corlib);
		g_assert (create_culture);
		mono_method_desc_free (desc);
	}

	args [0] = mono_string_new (domain, name->culture);
	aname->cultureInfo = 
		mono_runtime_invoke (create_culture, NULL, args, NULL);

	if (name->public_key) {
		pkey_ptr = name->public_key;
		pkey_len = mono_metadata_decode_blob_size (pkey_ptr, &pkey_ptr);

		aname->publicKey = mono_array_new (domain, mono_defaults.byte_class, pkey_len);
		memcpy (mono_array_addr (aname->publicKey, guint8, 0), pkey_ptr, pkey_len);
	}
}

static void
ves_icall_System_Reflection_Assembly_FillName (MonoReflectionAssembly *assembly, MonoReflectionAssemblyName *aname)
{
	gchar *absolute;

	MONO_ARCH_SAVE_REGS;

	absolute = g_build_filename (assembly->assembly->basedir, assembly->assembly->image->module_name, NULL);

	fill_reflection_assembly_name (mono_object_domain (assembly), aname, 
								   &assembly->assembly->aname, absolute);

	g_free (absolute);
}

static void
ves_icall_System_Reflection_Assembly_InternalGetAssemblyName (MonoString *fname, MonoReflectionAssemblyName *aname)
{
	char *filename;
	MonoImageOpenStatus status = MONO_IMAGE_OK;
	gboolean res;
	MonoImage *image;
	MonoAssemblyName name;

	MONO_ARCH_SAVE_REGS;

	filename = mono_string_to_utf8 (fname);

	image = mono_image_open (filename, &status);
	
	if (!image){
		MonoException *exc;

		g_free (filename);
		exc = mono_get_exception_file_not_found (fname);
		mono_raise_exception (exc);
	}

	res = mono_assembly_fill_assembly_name (image, &name);
	if (!res) {
		mono_image_close (image);
		g_free (filename);
		mono_raise_exception (mono_get_exception_argument ("assemblyFile", "The file does not contain a manifest"));
	}

	fill_reflection_assembly_name (mono_domain_get (), aname, &name, filename);

	g_free (filename);
	mono_image_close (image);
}

static MonoArray*
mono_module_get_types (MonoDomain *domain, MonoImage *image, 
					   MonoBoolean exportedOnly)
{
	MonoArray *res;
	MonoClass *klass;
	MonoTableInfo *tdef = &image->tables [MONO_TABLE_TYPEDEF];
	int i, count;
	guint32 attrs, visibility;

	/* we start the count from 1 because we skip the special type <Module> */
	if (exportedOnly) {
		count = 0;
		for (i = 1; i < tdef->rows; ++i) {
			attrs = mono_metadata_decode_row_col (tdef, i, MONO_TYPEDEF_FLAGS);
			visibility = attrs & TYPE_ATTRIBUTE_VISIBILITY_MASK;
			if (visibility == TYPE_ATTRIBUTE_PUBLIC || visibility == TYPE_ATTRIBUTE_NESTED_PUBLIC)
				count++;
		}
	} else {
		count = tdef->rows - 1;
	}
	res = mono_array_new (domain, mono_defaults.monotype_class, count);
	count = 0;
	for (i = 1; i < tdef->rows; ++i) {
		attrs = mono_metadata_decode_row_col (tdef, i, MONO_TYPEDEF_FLAGS);
		visibility = attrs & TYPE_ATTRIBUTE_VISIBILITY_MASK;
		if (!exportedOnly || (visibility == TYPE_ATTRIBUTE_PUBLIC || visibility == TYPE_ATTRIBUTE_NESTED_PUBLIC)) {
			klass = mono_class_get (image, (i + 1) | MONO_TOKEN_TYPE_DEF);
			mono_array_set (res, gpointer, count, mono_type_get_object (domain, &klass->byval_arg));
			count++;
		}
	}
	
	return res;
}

static MonoArray*
ves_icall_System_Reflection_Assembly_GetTypes (MonoReflectionAssembly *assembly, MonoBoolean exportedOnly)
{
	MonoArray *res;
	MonoImage *image = assembly->assembly->image;
	MonoTableInfo *table = &image->tables [MONO_TABLE_FILE];
	MonoDomain *domain;
	int i;

	MONO_ARCH_SAVE_REGS;

	domain = mono_object_domain (assembly);
	res = mono_module_get_types (domain, image, exportedOnly);

	/* Append data from all modules in the assembly */
	for (i = 0; i < table->rows; ++i) {
		if (!(mono_metadata_decode_row_col (table, i, MONO_FILE_FLAGS) & FILE_CONTAINS_NO_METADATA)) {
			MonoImage *loaded_image = mono_assembly_load_module (image->assembly, i + 1);
			if (loaded_image) {
				MonoArray *res2 = mono_module_get_types (domain, loaded_image, exportedOnly);
				/* Append the new types to the end of the array */
				if (mono_array_length (res2) > 0) {
					guint32 len1, len2;
					MonoArray *res3;

					len1 = mono_array_length (res);
					len2 = mono_array_length (res2);
					res3 = mono_array_new (domain, mono_defaults.monotype_class, len1 + len2);
					memcpy (mono_array_addr (res3, MonoReflectionType*, 0),
							mono_array_addr (res, MonoReflectionType*, 0),
							len1 * sizeof (MonoReflectionType*));
					memcpy (mono_array_addr (res3, MonoReflectionType*, len1),
							mono_array_addr (res2, MonoReflectionType*, 0),
							len2 * sizeof (MonoReflectionType*));
					res = res3;
				}
			}
		}
	}		

	return res;
}

static MonoReflectionType*
ves_icall_System_Reflection_Module_GetGlobalType (MonoReflectionModule *module)
{
	MonoDomain *domain = mono_object_domain (module); 
	MonoClass *klass;

	MONO_ARCH_SAVE_REGS;

	g_assert (module->image);
	klass = mono_class_get (module->image, 1 | MONO_TOKEN_TYPE_DEF);
	return mono_type_get_object (domain, &klass->byval_arg);
}

static MonoString*
ves_icall_System_Reflection_Module_GetGuidInternal (MonoReflectionModule *module)
{
	MonoDomain *domain = mono_object_domain (module); 

	MONO_ARCH_SAVE_REGS;

	g_assert (module->image);
	return mono_string_new (domain, module->image->guid);
}

static MonoArray*
ves_icall_System_Reflection_Module_InternalGetTypes (MonoReflectionModule *module)
{
	MONO_ARCH_SAVE_REGS;

	if (!module->image)
		return mono_array_new (mono_object_domain (module), mono_defaults.monotype_class, 0);
	else
		return mono_module_get_types (mono_object_domain (module), module->image, FALSE);
}

static MonoReflectionType*
ves_icall_ModuleBuilder_create_modified_type (MonoReflectionTypeBuilder *tb, MonoString *smodifiers)
{
	MonoClass *klass;
	int isbyref = 0, rank;
	char *str = mono_string_to_utf8 (smodifiers);
	char *p;

	MONO_ARCH_SAVE_REGS;

	klass = mono_class_from_mono_type (tb->type.type);
	p = str;
	/* logic taken from mono_reflection_parse_type(): keep in sync */
	while (*p) {
		switch (*p) {
		case '&':
			if (isbyref) { /* only one level allowed by the spec */
				g_free (str);
				return NULL;
			}
			isbyref = 1;
			p++;
			g_free (str);
			return mono_type_get_object (mono_object_domain (tb), &klass->this_arg);
			break;
		case '*':
			klass = mono_ptr_class_get (&klass->byval_arg);
			mono_class_init (klass);
			p++;
			break;
		case '[':
			rank = 1;
			p++;
			while (*p) {
				if (*p == ']')
					break;
				if (*p == ',')
					rank++;
				else if (*p != '*') { /* '*' means unknown lower bound */
					g_free (str);
					return NULL;
				}
				++p;
			}
			if (*p != ']') {
				g_free (str);
				return NULL;
			}
			p++;
			klass = mono_array_class_get (klass, rank);
			mono_class_init (klass);
			break;
		default:
			break;
		}
	}
	g_free (str);
	return mono_type_get_object (mono_object_domain (tb), &klass->byval_arg);
}

static MonoBoolean
ves_icall_Type_IsArrayImpl (MonoReflectionType *t)
{
	MonoType *type;
	MonoBoolean res;

	MONO_ARCH_SAVE_REGS;

	type = t->type;
	res = !type->byref && (type->type == MONO_TYPE_ARRAY || type->type == MONO_TYPE_SZARRAY);

	return res;
}

static MonoObject *
ves_icall_System_Delegate_CreateDelegate_internal (MonoReflectionType *type, MonoObject *target,
						   MonoReflectionMethod *info)
{
	MonoClass *delegate_class = mono_class_from_mono_type (type->type);
	MonoObject *delegate;
	gpointer func;

	MONO_ARCH_SAVE_REGS;

	mono_assert (delegate_class->parent == mono_defaults.multicastdelegate_class);

	delegate = mono_object_new (mono_object_domain (type), delegate_class);

	func = mono_compile_method (info->method);

	mono_delegate_ctor (delegate, target, func);

	return delegate;
}

/*
 * Magic number to convert a time which is relative to
 * Jan 1, 1970 into a value which is relative to Jan 1, 0001.
 */
#define	EPOCH_ADJUST	((guint64)62135596800LL)

/*
 * Magic number to convert FILETIME base Jan 1, 1601 to DateTime - base Jan, 1, 0001
 */
#define FILETIME_ADJUST ((guint64)504911232000000000LL)

/*
 * This returns Now in UTC
 */
static gint64
ves_icall_System_DateTime_GetNow (void)
{
#ifdef PLATFORM_WIN32
	SYSTEMTIME st;
	FILETIME ft;
	
	GetLocalTime (&st);
	SystemTimeToFileTime (&st, &ft);
	return (gint64) FILETIME_ADJUST + ((((gint64)ft.dwHighDateTime)<<32) | ft.dwLowDateTime);
#else
	/* FIXME: put this in io-layer and call it GetLocalTime */
	struct timeval tv;
	gint64 res;

	MONO_ARCH_SAVE_REGS;

	if (gettimeofday (&tv, NULL) == 0) {
		res = (((gint64)tv.tv_sec + EPOCH_ADJUST)* 1000000 + tv.tv_usec)*10;
		return res;
	}
	/* fixme: raise exception */
	return 0;
#endif
}

#ifdef PLATFORM_WIN32
/* convert a SYSTEMTIME which is of the form "last thursday in october" to a real date */
static void
convert_to_absolute_date(SYSTEMTIME *date)
{
#define IS_LEAP(y) ((y % 4) == 0 && ((y % 100) != 0 || (y % 400) == 0))
	static int days_in_month[] = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31};
	static int leap_days_in_month[] = { 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31};
	/* from the calendar FAQ */
	int a = (14 - date->wMonth) / 12;
	int y = date->wYear - a;
	int m = date->wMonth + 12 * a - 2;
	int d = (1 + y + y/4 - y/100 + y/400 + (31*m)/12) % 7;

	/* d is now the day of the week for the first of the month (0 == Sunday) */

	int day_of_week = date->wDayOfWeek;

	/* set day_in_month to the first day in the month which falls on day_of_week */    
	int day_in_month = 1 + (day_of_week - d);
	if (day_in_month <= 0)
		day_in_month += 7;

	/* wDay is 1 for first weekday in month, 2 for 2nd ... 5 means last - so work that out allowing for days in the month */
	date->wDay = day_in_month + (date->wDay - 1) * 7;
	if (date->wDay > (IS_LEAP(date->wYear) ? leap_days_in_month[date->wMonth - 1] : days_in_month[date->wMonth - 1]))
		date->wDay -= 7;
}
#endif

#ifndef PLATFORM_WIN32
/*
 * Return's the offset from GMT of a local time.
 * 
 *  tm is a local time
 *  t  is the same local time as seconds.
 */
static int 
gmt_offset(struct tm *tm, time_t t)
{
#if defined (HAVE_TM_GMTOFF)
	return tm->tm_gmtoff;
#else
	struct tm g;
	time_t t2;
	g = *gmtime(&t);
	g.tm_isdst = tm->tm_isdst;
	t2 = mktime(&g);
	return (int)difftime(t, t2);
#endif
}
#endif
/*
 * This is heavily based on zdump.c from glibc 2.2.
 *
 *  * data[0]:  start of daylight saving time (in DateTime ticks).
 *  * data[1]:  end of daylight saving time (in DateTime ticks).
 *  * data[2]:  utcoffset (in TimeSpan ticks).
 *  * data[3]:  additional offset when daylight saving (in TimeSpan ticks).
 *  * name[0]:  name of this timezone when not daylight saving.
 *  * name[1]:  name of this timezone when daylight saving.
 *
 *  FIXME: This only works with "standard" Unix dates (years between 1900 and 2100) while
 *         the class library allows years between 1 and 9999.
 *
 *  Returns true on success and zero on failure.
 */
static guint32
ves_icall_System_CurrentTimeZone_GetTimeZoneData (guint32 year, MonoArray **data, MonoArray **names)
{
#ifndef PLATFORM_WIN32
	MonoDomain *domain = mono_domain_get ();
	struct tm start, tt;
	time_t t;

	long int gmtoff;
	int is_daylight = 0, day;
	char tzone [64];

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (data);
	MONO_CHECK_ARG_NULL (names);

	(*data) = mono_array_new (domain, mono_defaults.int64_class, 4);
	(*names) = mono_array_new (domain, mono_defaults.string_class, 2);

	/* 
	 * no info is better than crashing: we'll need our own tz data to make 
	 * this work properly, anyway. The range is reduced to 1970 .. 2037 because
	 * that is what mktime is guaranteed to support (we get into an infinite loop 
	 * otherwise).
	 */
	if ((year < 1970) || (year > 2037)) {
		t = time (NULL);
		tt = *localtime (&t);
		strftime (tzone, sizeof (tzone), "%Z", &tt);
		mono_array_set ((*names), gpointer, 0, mono_string_new (domain, tzone));
		mono_array_set ((*names), gpointer, 1, mono_string_new (domain, tzone));
		return 1;
	}

	memset (&start, 0, sizeof (start));

	start.tm_mday = 1;
	start.tm_year = year-1900;

	t = mktime (&start);
	gmtoff = gmt_offset (&start, t);

	/* For each day of the year, calculate the tm_gmtoff. */
	for (day = 0; day < 365; day++) {

		t += 3600*24;
		tt = *localtime (&t);

		/* Daylight saving starts or ends here. */
		if (gmt_offset (&tt, t) != gmtoff) {
			struct tm tt1;
			time_t t1;

			/* Try to find the exact hour when daylight saving starts/ends. */
			t1 = t;
			do {
				t1 -= 3600;
				tt1 = *localtime (&t1);
			} while (gmt_offset (&tt1, t1) != gmtoff);

			/* Try to find the exact minute when daylight saving starts/ends. */
			do {
				t1 += 60;
				tt1 = *localtime (&t1);
			} while (gmt_offset (&tt1, t1) == gmtoff);
			
			strftime (tzone, sizeof (tzone), "%Z", &tt);
			
			/* Write data, if we're already in daylight saving, we're done. */
			if (is_daylight) {
				mono_array_set ((*names), gpointer, 0, mono_string_new (domain, tzone));
				mono_array_set ((*data), gint64, 1, ((gint64)t1 + EPOCH_ADJUST) * 10000000L);
				return 1;
			} else {
				mono_array_set ((*names), gpointer, 1, mono_string_new (domain, tzone));
				mono_array_set ((*data), gint64, 0, ((gint64)t1 + EPOCH_ADJUST) * 10000000L);
				is_daylight = 1;
			}

			/* This is only set once when we enter daylight saving. */
			mono_array_set ((*data), gint64, 2, (gint64)gmtoff * 10000000L);
			mono_array_set ((*data), gint64, 3, (gint64)(gmt_offset (&tt, t) - gmtoff) * 10000000L);

			gmtoff = gmt_offset (&tt, t);
		}
	}

	if (!is_daylight) {
		strftime (tzone, sizeof (tzone), "%Z", &tt);
		mono_array_set ((*names), gpointer, 0, mono_string_new (domain, tzone));
		mono_array_set ((*names), gpointer, 1, mono_string_new (domain, tzone));
		mono_array_set ((*data), gint64, 0, 0);
		mono_array_set ((*data), gint64, 1, 0);
		mono_array_set ((*data), gint64, 2, (gint64) gmtoff * 10000000L);
		mono_array_set ((*data), gint64, 3, 0);
	}

	return 1;
#else
	MonoDomain *domain = mono_domain_get ();
	TIME_ZONE_INFORMATION tz_info;
	FILETIME ft;
	int i;
	int err, tz_id;

	tz_id = GetTimeZoneInformation (&tz_info);
	if (tz_id == TIME_ZONE_ID_INVALID)
		return 0;

	MONO_CHECK_ARG_NULL (data);
	MONO_CHECK_ARG_NULL (names);

	(*data) = mono_array_new (domain, mono_defaults.int64_class, 4);
	(*names) = mono_array_new (domain, mono_defaults.string_class, 2);

	for (i = 0; i < 32; ++i)
		if (!tz_info.DaylightName [i])
			break;
	mono_array_set ((*names), gpointer, 1, mono_string_new_utf16 (domain, tz_info.DaylightName, i));
	for (i = 0; i < 32; ++i)
		if (!tz_info.StandardName [i])
			break;
	mono_array_set ((*names), gpointer, 0, mono_string_new_utf16 (domain, tz_info.StandardName, i));

	if ((year <= 1601) || (year > 30827)) {
		/*
		 * According to MSDN, the MS time functions can't handle dates outside
		 * this interval.
		 */
		return 1;
	}

	/* even if the timezone has no daylight savings it may have Bias (e.g. GMT+13 it seems) */
	if (tz_id != TIME_ZONE_ID_UNKNOWN) {
		tz_info.StandardDate.wYear = year;
		convert_to_absolute_date(&tz_info.StandardDate);
		err = SystemTimeToFileTime (&tz_info.StandardDate, &ft);
		g_assert(err);
		mono_array_set ((*data), gint64, 1, FILETIME_ADJUST + (((guint64)ft.dwHighDateTime<<32) | ft.dwLowDateTime));
		tz_info.DaylightDate.wYear = year;
		convert_to_absolute_date(&tz_info.DaylightDate);
		err = SystemTimeToFileTime (&tz_info.DaylightDate, &ft);
		g_assert(err);
		mono_array_set ((*data), gint64, 0, FILETIME_ADJUST + (((guint64)ft.dwHighDateTime<<32) | ft.dwLowDateTime));
	}
	mono_array_set ((*data), gint64, 2, (tz_info.Bias + tz_info.StandardBias) * -600000000LL);
	mono_array_set ((*data), gint64, 3, (tz_info.DaylightBias - tz_info.StandardBias) * -600000000LL);

	return 1;
#endif
}

static gpointer
ves_icall_System_Object_obj_address (MonoObject *this) 
{
	MONO_ARCH_SAVE_REGS;

	return this;
}

/* System.Buffer */

static gint32 
ves_icall_System_Buffer_ByteLengthInternal (MonoArray *array) 
{
	MonoClass *klass;
	MonoTypeEnum etype;
	int length, esize;
	int i;

	MONO_ARCH_SAVE_REGS;

	klass = array->obj.vtable->klass;
	etype = klass->element_class->byval_arg.type;
	if (etype < MONO_TYPE_BOOLEAN || etype > MONO_TYPE_R8)
		return -1;

	if (array->bounds == NULL)
		length = array->max_length;
	else {
		length = 1;
		for (i = 0; i < klass->rank; ++ i)
			length *= array->bounds [i].length;
	}

	esize = mono_array_element_size (klass);
	return length * esize;
}

static gint8 
ves_icall_System_Buffer_GetByteInternal (MonoArray *array, gint32 idx) 
{
	MONO_ARCH_SAVE_REGS;

	return mono_array_get (array, gint8, idx);
}

static void 
ves_icall_System_Buffer_SetByteInternal (MonoArray *array, gint32 idx, gint8 value) 
{
	MONO_ARCH_SAVE_REGS;

	mono_array_set (array, gint8, idx, value);
}

static void 
ves_icall_System_Buffer_BlockCopyInternal (MonoArray *src, gint32 src_offset, MonoArray *dest, gint32 dest_offset, gint32 count) 
{
	char *src_buf, *dest_buf;

	MONO_ARCH_SAVE_REGS;

	src_buf = (gint8 *)src->vector + src_offset;
	dest_buf = (gint8 *)dest->vector + dest_offset;

	memcpy (dest_buf, src_buf, count);
}

static MonoObject *
ves_icall_Remoting_RealProxy_GetTransparentProxy (MonoObject *this)
{
	MonoDomain *domain = mono_object_domain (this); 
	MonoObject *res;
	MonoRealProxy *rp = ((MonoRealProxy *)this);
	MonoType *type;
	MonoClass *klass;

	MONO_ARCH_SAVE_REGS;

	res = mono_object_new (domain, mono_defaults.transparent_proxy_class);
	
	((MonoTransparentProxy *)res)->rp = rp;
	type = ((MonoReflectionType *)rp->class_to_proxy)->type;
	klass = mono_class_from_mono_type (type);

	if (klass->flags & TYPE_ATTRIBUTE_INTERFACE)
		((MonoTransparentProxy *)res)->klass = mono_defaults.marshalbyrefobject_class;
	else
		((MonoTransparentProxy *)res)->klass = klass;

	res->vtable = mono_class_proxy_vtable (domain, klass);

	return res;
}

/* System.Environment */

static MonoString *
ves_icall_System_Environment_get_MachineName (void)
{
#if defined (PLATFORM_WIN32)
	gunichar2 *buf;
	guint32 len;
	MonoString *result;

	len = MAX_COMPUTERNAME_LENGTH + 1;
	buf = g_new (gunichar2, len);

	result = NULL;
	if (GetComputerName (buf, (PDWORD) &len))
		result = mono_string_new_utf16 (mono_domain_get (), buf, len);

	g_free (buf);
	return result;
#else
	gchar *buf;
	int len;
	MonoString *result;

	MONO_ARCH_SAVE_REGS;

	len = 256;
	buf = g_new (gchar, len);

	result = NULL;
	if (gethostname (buf, len) == 0)
		result = mono_string_new (mono_domain_get (), buf);
	
	g_free (buf);
	return result;
#endif
}

static int
ves_icall_System_Environment_get_Platform (void)
{
	MONO_ARCH_SAVE_REGS;

#if defined (PLATFORM_WIN32)
	/* Win32NT */
	return 2;
#else
	/* Unix */
	return 128;
#endif
}

static MonoString *
ves_icall_System_Environment_get_NewLine (void)
{
	MONO_ARCH_SAVE_REGS;

#if defined (PLATFORM_WIN32)
	return mono_string_new (mono_domain_get (), "\r\n");
#else
	return mono_string_new (mono_domain_get (), "\n");
#endif
}

static MonoString *
ves_icall_System_Environment_GetEnvironmentVariable (MonoString *name)
{
	const gchar *value;
	gchar *utf8_name;

	MONO_ARCH_SAVE_REGS;

	if (name == NULL)
		return NULL;

	utf8_name = mono_string_to_utf8 (name);	/* FIXME: this should be ascii */
	value = g_getenv (utf8_name);
	g_free (utf8_name);

	if (value == 0)
		return NULL;
	
	return mono_string_new (mono_domain_get (), value);
}

/*
 * There is no standard way to get at environ.
 */
#ifndef _MSC_VER
extern
#endif
char **environ;

static MonoArray *
ves_icall_System_Environment_GetEnvironmentVariableNames (void)
{
	MonoArray *names;
	MonoDomain *domain;
	MonoString *str;
	gchar **e, **parts;
	int n;

	MONO_ARCH_SAVE_REGS;

	n = 0;
	for (e = environ; *e != 0; ++ e)
		++ n;

	domain = mono_domain_get ();
	names = mono_array_new (domain, mono_defaults.string_class, n);

	n = 0;
	for (e = environ; *e != 0; ++ e) {
		parts = g_strsplit (*e, "=", 2);
		if (*parts != 0) {
			str = mono_string_new (domain, *parts);
			mono_array_set (names, MonoString *, n, str);
		}

		g_strfreev (parts);

		++ n;
	}

	return names;
}

/*
 * Returns the number of milliseconds elapsed since the system started.
 */
static gint32
ves_icall_System_Environment_get_TickCount (void)
{
#if defined (PLATFORM_WIN32)
	return GetTickCount();
#else
	struct timeval tv;
	struct timezone tz;
	gint32 res;

	MONO_ARCH_SAVE_REGS;

	res = (gint32) gettimeofday (&tv, &tz);

	if (res != -1)
		res = (gint32) ((tv.tv_sec & 0xFFFFF) * 1000 + (tv.tv_usec / 1000));
	return res;
#endif
}


static void
ves_icall_System_Environment_Exit (int result)
{
	MONO_ARCH_SAVE_REGS;

	mono_runtime_quit ();

	/* we may need to do some cleanup here... */
	exit (result);
}

static MonoString*
ves_icall_System_Text_Encoding_InternalCodePage (void) 
{
	const char *cset;

	MONO_ARCH_SAVE_REGS;

	g_get_charset (&cset);
	/* g_print ("charset: %s\n", cset); */
	/* handle some common aliases */
	switch (*cset) {
	case 'A':
		if (strcmp (cset, "ANSI_X3.4-1968") == 0)
			cset = "us-ascii";
		break;
	}
	return mono_string_new (mono_domain_get (), cset);
}

static void
ves_icall_MonoMethodMessage_InitMessage (MonoMethodMessage *this, 
					 MonoReflectionMethod *method,
					 MonoArray *out_args)
{
	MONO_ARCH_SAVE_REGS;

	mono_message_init (mono_object_domain (this), this, method, out_args);
}

static MonoBoolean
ves_icall_IsTransparentProxy (MonoObject *proxy)
{
	MONO_ARCH_SAVE_REGS;

	if (!proxy)
		return 0;

	if (proxy->vtable->klass == mono_defaults.transparent_proxy_class)
		return 1;

	return 0;
}

static void
ves_icall_System_Runtime_Activation_ActivationServices_EnableProxyActivation (MonoReflectionType *type, MonoBoolean enable)
{
	MonoClass *klass;
	MonoVTable* vtable;

	MONO_ARCH_SAVE_REGS;

	klass = mono_class_from_mono_type (type->type);
	vtable = mono_class_vtable (mono_domain_get (), klass);

	if (enable) vtable->remote = 1;
	else vtable->remote = 0;
}

static MonoObject *
ves_icall_System_Runtime_Activation_ActivationServices_AllocateUninitializedClassInstance (MonoReflectionType *type)
{
	MonoClass *klass;
	MonoDomain *domain;
	
	MONO_ARCH_SAVE_REGS;

	domain = mono_object_domain (type);
	klass = mono_class_from_mono_type (type->type);

	if (klass->rank >= 1) {
		g_assert (klass->rank == 1);
		return (MonoObject *) mono_array_new (domain, klass->element_class, 0);
	} else {
		// Bypass remoting object creation check
		return mono_object_new_alloc_specific (mono_class_vtable (domain, klass));
	}
}

static MonoString *
ves_icall_System_IO_get_temp_path (void)
{
	MONO_ARCH_SAVE_REGS;

	return mono_string_new (mono_domain_get (), g_get_tmp_dir ());
}

static gpointer
ves_icall_RuntimeMethod_GetFunctionPointer (MonoMethod *method)
{
	MONO_ARCH_SAVE_REGS;

	return mono_compile_method (method);
}

char const * mono_cfg_dir = "";

void    
mono_install_get_config_dir (void)
{
#ifdef PLATFORM_WIN32
  int i;
#endif

  mono_cfg_dir = getenv ("MONO_CFG_DIR");

  if (!mono_cfg_dir) {
#ifndef PLATFORM_WIN32
    mono_cfg_dir = MONO_CFG_DIR;
#else
    mono_cfg_dir = g_strdup (MONO_CFG_DIR);
    for (i = strlen (mono_cfg_dir) - 1; i >= 0; i--) {
        if (mono_cfg_dir [i] == '/')
            ((char*) mono_cfg_dir) [i] = '\\';
    }
#endif
  }
}


static MonoString *
ves_icall_System_Configuration_DefaultConfig_get_machine_config_path (void)
{
	MonoString *mcpath;
	gchar *path;

	MONO_ARCH_SAVE_REGS;

	path = g_build_path (G_DIR_SEPARATOR_S, mono_cfg_dir, "mono", "machine.config", NULL);

#if defined (PLATFORM_WIN32)
	/* Avoid mixing '/' and '\\' */
	{
		gint i;
		for (i = strlen (path) - 1; i >= 0; i--)
			if (path [i] == '/')
				path [i] = '\\';
	}
#endif
	mcpath = mono_string_new (mono_domain_get (), path);
	g_free (path);

	return mcpath;
}

static MonoString *
ves_icall_System_Web_Util_ICalls_get_machine_install_dir (void)
{
	MonoString *ipath;
	gchar *path;

	MONO_ARCH_SAVE_REGS;

	path = g_path_get_dirname (mono_cfg_dir);

#if defined (PLATFORM_WIN32)
	/* Avoid mixing '/' and '\\' */
	{
		gint i;
		for (i = strlen (path) - 1; i >= 0; i--)
			if (path [i] == '/')
				path [i] = '\\';
	}
#endif
	ipath = mono_string_new (mono_domain_get (), path);
	g_free (path);

	return ipath;
}

static void
ves_icall_System_Diagnostics_DefaultTraceListener_WriteWindowsDebugString (MonoString *message)
{
#if defined (PLATFORM_WIN32)
	static void (*output_debug) (gchar *);
	static gboolean tried_loading = FALSE;

	MONO_ARCH_SAVE_REGS;

	if (!tried_loading && output_debug == NULL) {
		GModule *k32;

		tried_loading = TRUE;
		k32 = g_module_open ("kernel32", G_MODULE_BIND_LAZY);
		if (!k32) {
			gchar *error = g_strdup (g_module_error ());
			g_warning ("Failed to load kernel32.dll: %s\n", error);
			g_free (error);
			return;
		}

		g_module_symbol (k32, "OutputDebugStringW", (gpointer *) &output_debug);
		if (!output_debug) {
			gchar *error = g_strdup (g_module_error ());
			g_warning ("Failed to load OutputDebugStringW: %s\n", error);
			g_free (error);
			return;
		}
	}

	if (output_debug == NULL)
		return;
	
	output_debug (mono_string_chars (message));
#else
	g_warning ("WriteWindowsDebugString called and PLATFORM_WIN32 not defined!\n");
#endif
}

/* Only used for value types */
static MonoObject *
ves_icall_System_Activator_CreateInstanceInternal (MonoReflectionType *type)
{
	MonoClass *klass;
	MonoDomain *domain;
	
	MONO_ARCH_SAVE_REGS;

	domain = mono_object_domain (type);
	klass = mono_class_from_mono_type (type->type);

	return mono_object_new (domain, klass);
}

static MonoReflectionMethod *
ves_icall_MonoMethod_get_base_definition (MonoReflectionMethod *m)
{
	MonoClass *klass;
	MonoMethod *method = m->method;
	MonoMethod *result = NULL;

	MONO_ARCH_SAVE_REGS;

	if (!(method->flags & METHOD_ATTRIBUTE_VIRTUAL) ||
	     method->klass->flags & TYPE_ATTRIBUTE_INTERFACE ||
	     method->flags & METHOD_ATTRIBUTE_NEW_SLOT)
		return m;

	if (method->klass == NULL || (klass = method->klass->parent) == NULL)
		return m;

	while (result == NULL && klass != NULL && (klass->vtable_size > method->slot))
	{
		result = klass->vtable [method->slot];
		if (result == NULL) {
			/* It is an abstract method */
			int i;
			for (i=0; i<klass->method.count; i++) {
				if (klass->methods [i]->slot == method->slot) {
					result = klass->methods [i];
					break;
				}
			}
		}
		klass = klass->parent;
	}

	if (result == NULL)
		return m;

	return mono_method_get_object (mono_domain_get (), result, NULL);
}

static void
mono_ArgIterator_Setup (MonoArgIterator *iter, char* argsp, char* start)
{
	MONO_ARCH_SAVE_REGS;

	iter->sig = *(MonoMethodSignature**)argsp;
	
	g_assert (iter->sig->sentinelpos <= iter->sig->param_count);
	g_assert (iter->sig->call_convention == MONO_CALL_VARARG);

	iter->next_arg = 0;
	/* FIXME: it's not documented what start is exactly... */
	iter->args = start? start: argsp + sizeof (gpointer);
	iter->num_args = iter->sig->param_count - iter->sig->sentinelpos;

	// g_print ("sig %p, param_count: %d, sent: %d\n", iter->sig, iter->sig->param_count, iter->sig->sentinelpos);
}

static MonoTypedRef
mono_ArgIterator_IntGetNextArg (MonoArgIterator *iter)
{
	gint i, align, arg_size;
	MonoTypedRef res;
	MONO_ARCH_SAVE_REGS;

	i = iter->sig->sentinelpos + iter->next_arg;

	g_assert (i < iter->sig->param_count);

	res.type = iter->sig->params [i];
	res.klass = mono_class_from_mono_type (res.type);
	/* FIXME: endianess issue... */
	res.value = iter->args;
	arg_size = mono_type_stack_size (res.type, &align);
	iter->args = (char*)iter->args + arg_size;
	iter->next_arg++;

	//g_print ("returning arg %d, type 0x%02x of size %d at %p\n", i, res.type->type, arg_size, res.value);

	return res;
}

static MonoTypedRef
mono_ArgIterator_IntGetNextArgT (MonoArgIterator *iter, MonoType *type)
{
	gint i, align, arg_size;
	MonoTypedRef res;
	MONO_ARCH_SAVE_REGS;

	i = iter->sig->sentinelpos + iter->next_arg;

	g_assert (i < iter->sig->param_count);

	while (i < iter->sig->param_count) {
		if (!mono_metadata_type_equal (type, iter->sig->params [i]))
			continue;
		res.type = iter->sig->params [i];
		res.klass = mono_class_from_mono_type (res.type);
		/* FIXME: endianess issue... */
		res.value = iter->args;
		arg_size = mono_type_stack_size (res.type, &align);
		iter->args = (char*)iter->args + arg_size;
		iter->next_arg++;
		//g_print ("returning arg %d, type 0x%02x of size %d at %p\n", i, res.type->type, arg_size, res.value);
		return res;
	}
	//g_print ("arg type 0x%02x not found\n", res.type->type);

	res.type = NULL;
	res.value = NULL;
	res.klass = NULL;
	return res;
}

static MonoType*
mono_ArgIterator_IntGetNextArgType (MonoArgIterator *iter)
{
	gint i;
	MONO_ARCH_SAVE_REGS;
	
	i = iter->sig->sentinelpos + iter->next_arg;

	g_assert (i < iter->sig->param_count);

	return iter->sig->params [i];
}

static MonoObject*
mono_TypedReference_ToObject (MonoTypedRef tref)
{
	MONO_ARCH_SAVE_REGS;

	if (MONO_TYPE_IS_REFERENCE (tref.type)) {
		MonoObject** objp = tref.value;
		return *objp;
	}

	return mono_value_box (mono_domain_get (), tref.klass, tref.value);
}

static void
prelink_method (MonoMethod *method)
{
	if (!(method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL))
		return;
	mono_lookup_pinvoke_call (method);
	/* create the wrapper, too? */
}

static void
ves_icall_System_Runtime_InteropServices_Marshal_Prelink (MonoReflectionMethod *method)
{
	MONO_ARCH_SAVE_REGS;
	prelink_method (method->method);
}

static void
ves_icall_System_Runtime_InteropServices_Marshal_PrelinkAll (MonoReflectionType *type)
{
	MonoClass *klass = mono_class_from_mono_type (type->type);
	int i;
	MONO_ARCH_SAVE_REGS;

	mono_class_init (klass);
	for (i = 0; i < klass->method.count; ++i)
		prelink_method (klass->methods [i]);
}

/* icall map */

static gconstpointer icall_map [] = {
	/*
	 * System.Array
	 */
	"System.Array::GetValue",         ves_icall_System_Array_GetValue,
	"System.Array::SetValue",         ves_icall_System_Array_SetValue,
	"System.Array::GetValueImpl",     ves_icall_System_Array_GetValueImpl,
	"System.Array::SetValueImpl",     ves_icall_System_Array_SetValueImpl,
	"System.Array::GetRank",          ves_icall_System_Array_GetRank,
	"System.Array::GetLength",        ves_icall_System_Array_GetLength,
	"System.Array::GetLowerBound",    ves_icall_System_Array_GetLowerBound,
	"System.Array::CreateInstanceImpl",   ves_icall_System_Array_CreateInstanceImpl,
	"System.Array::FastCopy",         ves_icall_System_Array_FastCopy,
	"System.Array::Clone",            mono_array_clone,

	/*
	 * System.ArgIterator
	 */
	"System.ArgIterator::Setup",                            mono_ArgIterator_Setup,
	"System.ArgIterator::IntGetNextArg()",                  mono_ArgIterator_IntGetNextArg,
	"System.ArgIterator::IntGetNextArg(intptr)", mono_ArgIterator_IntGetNextArgT,
	"System.ArgIterator::IntGetNextArgType",                mono_ArgIterator_IntGetNextArgType,

	/*
	 * System.TypedReference
	 */
	"System.TypedReference::ToObject",                      mono_TypedReference_ToObject,

	/*
	 * System.Object
	 */
	"System.Object::MemberwiseClone", ves_icall_System_Object_MemberwiseClone,
	"System.Object::GetType", ves_icall_System_Object_GetType,
	"System.Object::InternalGetHashCode", ves_icall_System_Object_GetHashCode,
	"System.Object::obj_address", ves_icall_System_Object_obj_address,

	/*
	 * System.ValueType
	 */
	"System.ValueType::InternalGetHashCode", ves_icall_System_ValueType_InternalGetHashCode,
	"System.ValueType::InternalEquals", ves_icall_System_ValueType_Equals,

	/*
	 * System.String
	 */
	
	"System.String::.ctor(char*)", ves_icall_System_String_ctor_charp,
	"System.String::.ctor(char*,int,int)", ves_icall_System_String_ctor_charp_int_int,
	"System.String::.ctor(sbyte*)", ves_icall_System_String_ctor_sbytep,
	"System.String::.ctor(sbyte*,int,int)", ves_icall_System_String_ctor_sbytep_int_int,
	"System.String::.ctor(sbyte*,int,int,System.Text.Encoding)", ves_icall_System_String_ctor_encoding,
	"System.String::.ctor(char[])", ves_icall_System_String_ctor_chara,
	"System.String::.ctor(char[],int,int)", ves_icall_System_String_ctor_chara_int_int,
	"System.String::.ctor(char,int)", ves_icall_System_String_ctor_char_int,
	"System.String::InternalJoin", ves_icall_System_String_InternalJoin,
	"System.String::InternalInsert", ves_icall_System_String_InternalInsert,
	"System.String::InternalReplace(char,char)", ves_icall_System_String_InternalReplace_Char,
	"System.String::InternalRemove", ves_icall_System_String_InternalRemove,
	"System.String::InternalCopyTo", ves_icall_System_String_InternalCopyTo,
	"System.String::InternalSplit", ves_icall_System_String_InternalSplit,
	"System.String::InternalTrim", ves_icall_System_String_InternalTrim,
	"System.String::InternalIndexOfAny", ves_icall_System_String_InternalIndexOfAny,
	"System.String::InternalLastIndexOfAny", ves_icall_System_String_InternalLastIndexOfAny,
	"System.String::InternalPad", ves_icall_System_String_InternalPad,
	"System.String::InternalAllocateStr", ves_icall_System_String_InternalAllocateStr,
	"System.String::InternalStrcpy(string,int,string)", ves_icall_System_String_InternalStrcpy_Str,
	"System.String::InternalStrcpy(string,int,string,int,int)", ves_icall_System_String_InternalStrcpy_StrN,
	"System.String::InternalIntern", ves_icall_System_String_InternalIntern,
	"System.String::InternalIsInterned", ves_icall_System_String_InternalIsInterned,
	"System.String::GetHashCode", ves_icall_System_String_GetHashCode,
	"System.String::get_Chars", ves_icall_System_String_get_Chars,

	/*
	 * System.AppDomain
	 */
	"System.AppDomain::createDomain", ves_icall_System_AppDomain_createDomain,
	"System.AppDomain::getCurDomain", ves_icall_System_AppDomain_getCurDomain,
	"System.AppDomain::GetData", ves_icall_System_AppDomain_GetData,
	"System.AppDomain::SetData", ves_icall_System_AppDomain_SetData,
	"System.AppDomain::getSetup", ves_icall_System_AppDomain_getSetup,
	"System.AppDomain::getFriendlyName", ves_icall_System_AppDomain_getFriendlyName,
	"System.AppDomain::GetAssemblies", ves_icall_System_AppDomain_GetAssemblies,
	"System.AppDomain::LoadAssembly", ves_icall_System_AppDomain_LoadAssembly,
 	"System.AppDomain::LoadAssemblyRaw", ves_icall_System_AppDomain_LoadAssemblyRaw,
	"System.AppDomain::InternalIsFinalizingForUnload", ves_icall_System_AppDomain_InternalIsFinalizingForUnload,
	"System.AppDomain::InternalUnload", ves_icall_System_AppDomain_InternalUnload,
	"System.AppDomain::ExecuteAssembly", ves_icall_System_AppDomain_ExecuteAssembly,
	"System.AppDomain::InternalSetDomain", ves_icall_System_AppDomain_InternalSetDomain,
	"System.AppDomain::InternalSetDomainByID", ves_icall_System_AppDomain_InternalSetDomainByID,
	"System.AppDomain::InternalPushDomainRef", ves_icall_System_AppDomain_InternalPushDomainRef,
	"System.AppDomain::InternalPushDomainRefByID", ves_icall_System_AppDomain_InternalPushDomainRefByID,
	"System.AppDomain::InternalPopDomainRef", ves_icall_System_AppDomain_InternalPopDomainRef,
	"System.AppDomain::InternalSetContext", ves_icall_System_AppDomain_InternalSetContext,
	"System.AppDomain::InternalGetContext", ves_icall_System_AppDomain_InternalGetContext,
	"System.AppDomain::InternalGetDefaultContext", ves_icall_System_AppDomain_InternalGetDefaultContext,
	"System.AppDomain::InternalGetProcessGuid", ves_icall_System_AppDomain_InternalGetProcessGuid,

	/*
	 * System.AppDomainSetup
	 */
	"System.AppDomainSetup::InitAppDomainSetup", ves_icall_System_AppDomainSetup_InitAppDomainSetup,

	/*
	 * System.Double
	 */
	"System.Double::ParseImpl",    mono_double_ParseImpl,
	"System.Double::AssertEndianity", ves_icall_System_Double_AssertEndianity,

	/*
	 * System.Decimal
	 */
	"System.Decimal::decimal2UInt64", mono_decimal2UInt64,
	"System.Decimal::decimal2Int64", mono_decimal2Int64,
	"System.Decimal::double2decimal", mono_double2decimal, /* FIXME: wrong signature. */
	"System.Decimal::decimalIncr", mono_decimalIncr,
	"System.Decimal::decimalSetExponent", mono_decimalSetExponent,
	"System.Decimal::decimal2double", mono_decimal2double,
	"System.Decimal::decimalFloorAndTrunc", mono_decimalFloorAndTrunc,
	"System.Decimal::decimalRound", mono_decimalRound,
	"System.Decimal::decimalMult", mono_decimalMult,
	"System.Decimal::decimalDiv", mono_decimalDiv,
	"System.Decimal::decimalIntDiv", mono_decimalIntDiv,
	"System.Decimal::decimalCompare", mono_decimalCompare,
	"System.Decimal::string2decimal", mono_string2decimal,
	"System.Decimal::decimal2string", mono_decimal2string,

	/*
	 * ModuleBuilder
	 */
	"System.Reflection.Emit.ModuleBuilder::getUSIndex", mono_image_insert_string,
	"System.Reflection.Emit.ModuleBuilder::getToken", ves_icall_ModuleBuilder_getToken,
	"System.Reflection.Emit.ModuleBuilder::create_modified_type", ves_icall_ModuleBuilder_create_modified_type,
	"System.Reflection.Emit.ModuleBuilder::basic_init", mono_image_module_basic_init,
	"System.Reflection.Emit.ModuleBuilder::build_metadata", ves_icall_ModuleBuilder_build_metadata,
	"System.Reflection.Emit.ModuleBuilder::getDataChunk", ves_icall_ModuleBuilder_getDataChunk,
	
	/*
	 * AssemblyBuilder
	 */
	"System.Reflection.Emit.AssemblyBuilder::basic_init", mono_image_basic_init,

	/*
	 * Reflection stuff.
	 */
	"System.Reflection.MonoMethodInfo::get_method_info", ves_icall_get_method_info,
	"System.Reflection.MonoMethodInfo::get_parameter_info", ves_icall_get_parameter_info,
	"System.Reflection.MonoPropertyInfo::get_property_info", ves_icall_get_property_info,
	"System.Reflection.MonoEventInfo::get_event_info", ves_icall_get_event_info,
	"System.Reflection.MonoMethod::InternalInvoke", ves_icall_InternalInvoke,
	"System.Reflection.MonoCMethod::InternalInvoke", ves_icall_InternalInvoke,
	"System.Reflection.MethodBase::GetCurrentMethod", ves_icall_GetCurrentMethod,
	"System.MonoCustomAttrs::GetCustomAttributes", mono_reflection_get_custom_attrs,
	"System.Reflection.Emit.CustomAttributeBuilder::GetBlob", mono_reflection_get_custom_attrs_blob,
	"System.Reflection.MonoField::GetParentType", ves_icall_MonoField_GetParentType,
	"System.Reflection.MonoField::GetValueInternal", ves_icall_MonoField_GetValueInternal,
	"System.Reflection.MonoField::SetValueInternal", ves_icall_FieldInfo_SetValueInternal,
	"System.Reflection.Emit.SignatureHelper::get_signature_local", mono_reflection_sighelper_get_signature_local,
	"System.Reflection.Emit.SignatureHelper::get_signature_field", mono_reflection_sighelper_get_signature_field,

	"System.RuntimeMethodHandle::GetFunctionPointer", ves_icall_RuntimeMethod_GetFunctionPointer,
	"System.Reflection.MonoMethod::get_base_definition", ves_icall_MonoMethod_get_base_definition,
	
	/* System.Enum */

	"System.MonoEnumInfo::get_enum_info", ves_icall_get_enum_info,
	"System.Enum::get_value", ves_icall_System_Enum_get_value,
	"System.Enum::ToObject", ves_icall_System_Enum_ToObject,

	/*
	 * TypeBuilder
	 */
	"System.Reflection.Emit.TypeBuilder::setup_internal_class", mono_reflection_setup_internal_class,
	"System.Reflection.Emit.TypeBuilder::create_internal_class", mono_reflection_create_internal_class,
	"System.Reflection.Emit.TypeBuilder::create_runtime_class", mono_reflection_create_runtime_class,
	"System.Reflection.Emit.TypeBuilder::setup_generic_class", mono_reflection_setup_generic_class,

	/*
	 * DynamicMethod
	 */
	"System.Reflection.Emit.DynamicMethod::create_dynamic_method", mono_reflection_create_dynamic_method,

	/*
	 * TypeBuilder generics icalls.
	 */
	"System.Reflection.Emit.TypeBuilder::get_IsGenericParameter", ves_icall_TypeBuilder_get_IsGenericParameter,
	"System.Reflection.Emit.TypeBuilder::define_generic_parameter", ves_icall_TypeBuilder_define_generic_parameter,
	
	/*
	 * MethodBuilder generic icalls.
	 */
	"System.Reflection.Emit.MethodBuilder::define_generic_parameter", ves_icall_MethodBuilder_define_generic_parameter,

	/*
	 * MonoGenericInst generic icalls.
	 */
	"System.Reflection.MonoGenericInst::inflate_method", mono_reflection_inflate_method_or_ctor,
	"System.Reflection.MonoGenericInst::inflate_ctor", mono_reflection_inflate_method_or_ctor,
	"System.Reflection.MonoGenericInst::inflate_field", mono_reflection_inflate_field,
	
	/*
	 * System.Type
	 */
	"System.Type::internal_from_name", ves_icall_type_from_name,
	"System.Type::internal_from_handle", ves_icall_type_from_handle,
	"System.MonoType::get_attributes", ves_icall_get_attributes,
	"System.Type::type_is_subtype_of", ves_icall_type_is_subtype_of,
	"System.Type::type_is_assignable_from", ves_icall_type_is_assignable_from,
	"System.Type::Equals", ves_icall_type_Equals,
	"System.Type::GetTypeCode", ves_icall_type_GetTypeCode,
	"System.Type::GetInterfaceMapData", ves_icall_Type_GetInterfaceMapData,
	"System.Type::IsArrayImpl", ves_icall_Type_IsArrayImpl,

	/* Type generics icalls */
	"System.Type::GetGenericArguments", ves_icall_Type_GetGenericArguments,
	"System.Type::GetGenericParameterPosition", ves_icall_Type_GetGenericParameterPosition,
	"System.Type::get_IsGenericTypeDefinition", ves_icall_Type_get_IsGenericTypeDefinition,
	"System.Type::GetGenericTypeDefinition_impl", ves_icall_Type_GetGenericTypeDefinition_impl,
	"System.Type::BindGenericParameters", ves_icall_Type_BindGenericParameters,
	"System.Type::get_IsGenericInstance", ves_icall_Type_get_IsGenericInstance,
	
	"System.MonoType::get_HasGenericArguments", ves_icall_MonoType_get_HasGenericArguments,
	"System.MonoType::get_IsGenericParameter", ves_icall_MonoType_get_IsGenericParameter,
	"System.MonoType::get_DeclaringMethod", ves_icall_MonoType_get_DeclaringMethod,

	/* Method generics icalls */
	"System.Reflection.MethodInfo::get_IsGenericMethodDefinition", ves_icall_MethodInfo_get_IsGenericMethodDefinition,
	"System.Reflection.MethodInfo::BindGenericParameters", mono_reflection_bind_generic_method_parameters,
	"System.Reflection.MonoMethod::GetGenericArguments", ves_icall_MonoMethod_GetGenericArguments,


	/*
	 * System.Reflection.FieldInfo
	 */
	"System.Reflection.FieldInfo::internal_from_handle", ves_icall_System_Reflection_FieldInfo_internal_from_handle,

	/*
	 * System.Runtime.CompilerServices.RuntimeHelpers
	 */
	"System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray", ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_InitializeArray,
	"System.Runtime.CompilerServices.RuntimeHelpers::GetOffsetToStringData", ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_GetOffsetToStringData,
	"System.Runtime.CompilerServices.RuntimeHelpers::GetObjectValue", ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_GetObjectValue,
	"System.Runtime.CompilerServices.RuntimeHelpers::RunClassConstructor", ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_RunClassConstructor,
	
	/*
	 * System.Threading
	 */
	"System.Threading.Thread::Abort_internal(object)", ves_icall_System_Threading_Thread_Abort,
	"System.Threading.Thread::ResetAbort_internal()", ves_icall_System_Threading_Thread_ResetAbort,
	"System.Threading.Thread::Thread_internal", ves_icall_System_Threading_Thread_Thread_internal,
	"System.Threading.Thread::Thread_free_internal", ves_icall_System_Threading_Thread_Thread_free_internal,
	"System.Threading.Thread::Start_internal", ves_icall_System_Threading_Thread_Start_internal,
	"System.Threading.Thread::Sleep_internal", ves_icall_System_Threading_Thread_Sleep_internal,
	"System.Threading.Thread::CurrentThread_internal", mono_thread_current,
	"System.Threading.Thread::Join_internal", ves_icall_System_Threading_Thread_Join_internal,
	"System.Threading.Thread::SlotHash_lookup", ves_icall_System_Threading_Thread_SlotHash_lookup,
	"System.Threading.Thread::SlotHash_store", ves_icall_System_Threading_Thread_SlotHash_store,
	"System.Threading.Thread::GetDomainID", ves_icall_System_Threading_Thread_GetDomainID,
	"System.Threading.Monitor::Monitor_exit", ves_icall_System_Threading_Monitor_Monitor_exit,
	"System.Threading.Monitor::Monitor_test_owner", ves_icall_System_Threading_Monitor_Monitor_test_owner,
	"System.Threading.Monitor::Monitor_test_synchronised", ves_icall_System_Threading_Monitor_Monitor_test_synchronised,
	"System.Threading.Monitor::Monitor_pulse", ves_icall_System_Threading_Monitor_Monitor_pulse,
	"System.Threading.Monitor::Monitor_pulse_all", ves_icall_System_Threading_Monitor_Monitor_pulse_all,
	"System.Threading.Monitor::Monitor_try_enter", ves_icall_System_Threading_Monitor_Monitor_try_enter,
	"System.Threading.Monitor::Monitor_wait", ves_icall_System_Threading_Monitor_Monitor_wait,
	"System.Threading.Mutex::CreateMutex_internal", ves_icall_System_Threading_Mutex_CreateMutex_internal,
	"System.Threading.Mutex::ReleaseMutex_internal", ves_icall_System_Threading_Mutex_ReleaseMutex_internal,
	"System.Threading.NativeEventCalls::CreateEvent_internal", ves_icall_System_Threading_Events_CreateEvent_internal,
	"System.Threading.NativeEventCalls::SetEvent_internal",    ves_icall_System_Threading_Events_SetEvent_internal,
	"System.Threading.NativeEventCalls::ResetEvent_internal",  ves_icall_System_Threading_Events_ResetEvent_internal,
	"System.Threading.NativeEventCalls::CloseEvent_internal", ves_icall_System_Threading_Events_CloseEvent_internal,
	"System.Threading.ThreadPool::GetAvailableThreads", ves_icall_System_Threading_ThreadPool_GetAvailableThreads,
	"System.Threading.ThreadPool::GetMaxThreads", ves_icall_System_Threading_ThreadPool_GetMaxThreads,
	"System.Threading.Thread::VolatileRead(byte&)", ves_icall_System_Threading_Thread_VolatileRead1,
	"System.Threading.Thread::VolatileRead(double&)", ves_icall_System_Threading_Thread_VolatileRead8,
	"System.Threading.Thread::VolatileRead(short&)", ves_icall_System_Threading_Thread_VolatileRead2,
	"System.Threading.Thread::VolatileRead(int&)", ves_icall_System_Threading_Thread_VolatileRead4,
	"System.Threading.Thread::VolatileRead(long&)", ves_icall_System_Threading_Thread_VolatileRead8,
	"System.Threading.Thread::VolatileRead(IntPtr&)", ves_icall_System_Threading_Thread_VolatileReadIntPtr,
	"System.Threading.Thread::VolatileRead(object&)", ves_icall_System_Threading_Thread_VolatileReadIntPtr,
	"System.Threading.Thread::VolatileRead(sbyte&)", ves_icall_System_Threading_Thread_VolatileRead1,
	"System.Threading.Thread::VolatileRead(float&)", ves_icall_System_Threading_Thread_VolatileRead4,
	"System.Threading.Thread::VolatileRead(ushort&)", ves_icall_System_Threading_Thread_VolatileRead2,
	"System.Threading.Thread::VolatileRead(uint&)", ves_icall_System_Threading_Thread_VolatileRead2,
	"System.Threading.Thread::VolatileRead(ulong&)", ves_icall_System_Threading_Thread_VolatileRead8,
	"System.Threading.Thread::VolatileRead(UIntPtr&)", ves_icall_System_Threading_Thread_VolatileReadIntPtr,
	"System.Threading.Thread::VolatileWrite(byte&,byte)", ves_icall_System_Threading_Thread_VolatileWrite1,
	"System.Threading.Thread::VolatileWrite(double&,double)", ves_icall_System_Threading_Thread_VolatileWrite8,
	"System.Threading.Thread::VolatileWrite(short&,short)", ves_icall_System_Threading_Thread_VolatileWrite2,
	"System.Threading.Thread::VolatileWrite(int&,int)", ves_icall_System_Threading_Thread_VolatileWrite4,
	"System.Threading.Thread::VolatileWrite(long&,long)", ves_icall_System_Threading_Thread_VolatileWrite8,
	"System.Threading.Thread::VolatileWrite(IntPtr&,IntPtr)", ves_icall_System_Threading_Thread_VolatileWriteIntPtr,
	"System.Threading.Thread::VolatileWrite(object&,object)", ves_icall_System_Threading_Thread_VolatileWriteIntPtr,
	"System.Threading.Thread::VolatileWrite(sbyte&,sbyte)", ves_icall_System_Threading_Thread_VolatileWrite1,
	"System.Threading.Thread::VolatileWrite(float&,float)", ves_icall_System_Threading_Thread_VolatileWrite4,
	"System.Threading.Thread::VolatileWrite(ushort&,ushort)", ves_icall_System_Threading_Thread_VolatileWrite2,
	"System.Threading.Thread::VolatileWrite(uint&,uint)", ves_icall_System_Threading_Thread_VolatileWrite2,
	"System.Threading.Thread::VolatileWrite(ulong&,ulong)", ves_icall_System_Threading_Thread_VolatileWrite8,
	"System.Threading.Thread::VolatileWrite(UIntPtr&,UIntPtr)", ves_icall_System_Threading_Thread_VolatileWriteIntPtr,

	/*
	 * System.Threading.WaitHandle
	 */
	"System.Threading.WaitHandle::WaitAll_internal", ves_icall_System_Threading_WaitHandle_WaitAll_internal,
	"System.Threading.WaitHandle::WaitAny_internal", ves_icall_System_Threading_WaitHandle_WaitAny_internal,
	"System.Threading.WaitHandle::WaitOne_internal", ves_icall_System_Threading_WaitHandle_WaitOne_internal,

	/*
	 * System.Runtime.InteropServices.Marshal
	 */
	"System.Runtime.InteropServices.Marshal::ReadIntPtr", ves_icall_System_Runtime_InteropServices_Marshal_ReadIntPtr,
	"System.Runtime.InteropServices.Marshal::ReadByte", ves_icall_System_Runtime_InteropServices_Marshal_ReadByte,
	"System.Runtime.InteropServices.Marshal::ReadInt16", ves_icall_System_Runtime_InteropServices_Marshal_ReadInt16,
	"System.Runtime.InteropServices.Marshal::ReadInt32", ves_icall_System_Runtime_InteropServices_Marshal_ReadInt32,
	"System.Runtime.InteropServices.Marshal::ReadInt64", ves_icall_System_Runtime_InteropServices_Marshal_ReadInt64,
	"System.Runtime.InteropServices.Marshal::WriteIntPtr", ves_icall_System_Runtime_InteropServices_Marshal_WriteIntPtr,
	"System.Runtime.InteropServices.Marshal::WriteByte", ves_icall_System_Runtime_InteropServices_Marshal_WriteByte,
	"System.Runtime.InteropServices.Marshal::WriteInt16", ves_icall_System_Runtime_InteropServices_Marshal_WriteInt16,
	"System.Runtime.InteropServices.Marshal::WriteInt32", ves_icall_System_Runtime_InteropServices_Marshal_WriteInt32,
	"System.Runtime.InteropServices.Marshal::WriteInt64", ves_icall_System_Runtime_InteropServices_Marshal_WriteInt64,

	"System.Runtime.InteropServices.Marshal::PtrToStringAnsi(intptr)", ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringAnsi,
	"System.Runtime.InteropServices.Marshal::PtrToStringAnsi(intptr,int)", ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringAnsi_len,
	"System.Runtime.InteropServices.Marshal::PtrToStringAuto(intptr)", ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringAnsi,
	"System.Runtime.InteropServices.Marshal::PtrToStringAuto(intptr,int)", ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringAnsi_len,
	"System.Runtime.InteropServices.Marshal::PtrToStringUni(intptr)", ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringUni,
	"System.Runtime.InteropServices.Marshal::PtrToStringUni(intptr,int)", ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringUni_len,
	"System.Runtime.InteropServices.Marshal::PtrToStringBSTR", ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringBSTR,

	"System.Runtime.InteropServices.Marshal::GetLastWin32Error", ves_icall_System_Runtime_InteropServices_Marshal_GetLastWin32Error,
	"System.Runtime.InteropServices.Marshal::AllocHGlobal", mono_marshal_alloc,
	"System.Runtime.InteropServices.Marshal::FreeHGlobal", mono_marshal_free,
	"System.Runtime.InteropServices.Marshal::ReAllocHGlobal", mono_marshal_realloc,
	"System.Runtime.InteropServices.Marshal::copy_to_unmanaged", ves_icall_System_Runtime_InteropServices_Marshal_copy_to_unmanaged,
	"System.Runtime.InteropServices.Marshal::copy_from_unmanaged", ves_icall_System_Runtime_InteropServices_Marshal_copy_from_unmanaged,
	"System.Runtime.InteropServices.Marshal::SizeOf", ves_icall_System_Runtime_InteropServices_Marshal_SizeOf,
	"System.Runtime.InteropServices.Marshal::StructureToPtr", ves_icall_System_Runtime_InteropServices_Marshal_StructureToPtr,
	"System.Runtime.InteropServices.Marshal::PtrToStructure(intptr,object)", ves_icall_System_Runtime_InteropServices_Marshal_PtrToStructure,
	"System.Runtime.InteropServices.Marshal::PtrToStructure(intptr,System.Type)", ves_icall_System_Runtime_InteropServices_Marshal_PtrToStructure_type,
	"System.Runtime.InteropServices.Marshal::OffsetOf", ves_icall_System_Runtime_InteropServices_Marshal_OffsetOf,
	"System.Runtime.InteropServices.Marshal::StringToHGlobalAnsi", ves_icall_System_Runtime_InteropServices_Marshal_StringToHGlobalAnsi,
	"System.Runtime.InteropServices.Marshal::StringToHGlobalAuto", ves_icall_System_Runtime_InteropServices_Marshal_StringToHGlobalAnsi,
	"System.Runtime.InteropServices.Marshal::StringToHGlobalUni", ves_icall_System_Runtime_InteropServices_Marshal_StringToHGlobalUni,
	"System.Runtime.InteropServices.Marshal::DestroyStructure", ves_icall_System_Runtime_InteropServices_Marshal_DestroyStructure,
	"System.Runtime.InteropServices.Marshal::Prelink", ves_icall_System_Runtime_InteropServices_Marshal_Prelink,
	"System.Runtime.InteropServices.Marshal::PrelinkAll", ves_icall_System_Runtime_InteropServices_Marshal_PrelinkAll,


	"System.Reflection.Assembly::LoadFrom", ves_icall_System_Reflection_Assembly_LoadFrom,
	"System.Reflection.Assembly::InternalGetType", ves_icall_System_Reflection_Assembly_InternalGetType,
	"System.Reflection.Assembly::GetTypes", ves_icall_System_Reflection_Assembly_GetTypes,
	"System.Reflection.Assembly::FillName", ves_icall_System_Reflection_Assembly_FillName,
	"System.Reflection.Assembly::InternalGetAssemblyName", ves_icall_System_Reflection_Assembly_InternalGetAssemblyName,
	"System.Reflection.Assembly::get_code_base", ves_icall_System_Reflection_Assembly_get_code_base,
	"System.Reflection.Assembly::get_location", ves_icall_System_Reflection_Assembly_get_location,
	"System.Reflection.Assembly::InternalImageRuntimeVersion", ves_icall_System_Reflection_Assembly_InternalImageRuntimeVersion,
	"System.Reflection.Assembly::GetExecutingAssembly", ves_icall_System_Reflection_Assembly_GetExecutingAssembly,
	"System.Reflection.Assembly::GetEntryAssembly", ves_icall_System_Reflection_Assembly_GetEntryAssembly,
	"System.Reflection.Assembly::GetCallingAssembly", ves_icall_System_Reflection_Assembly_GetCallingAssembly,
	"System.Reflection.Assembly::get_EntryPoint", ves_icall_System_Reflection_Assembly_get_EntryPoint,
	"System.Reflection.Assembly::GetManifestResourceNames", ves_icall_System_Reflection_Assembly_GetManifestResourceNames,
	"System.Reflection.Assembly::GetManifestResourceInternal", ves_icall_System_Reflection_Assembly_GetManifestResourceInternal,
	"System.Reflection.Assembly::GetManifestResourceInfoInternal", ves_icall_System_Reflection_Assembly_GetManifestResourceInfoInternal,
	"System.Reflection.Assembly::GetFilesInternal", ves_icall_System_Reflection_Assembly_GetFilesInternal,
	"System.Reflection.Assembly::GetReferencedAssemblies", ves_icall_System_Reflection_Assembly_GetReferencedAssemblies,
	"System.Reflection.Assembly::GetNamespaces", ves_icall_System_Reflection_Assembly_GetNamespaces,
	"System.Reflection.Assembly::GetModulesInternal", ves_icall_System_Reflection_Assembly_GetModulesInternal,

	/*
	 * System.Reflection.Module
	 */
	"System.Reflection.Module::GetGlobalType", ves_icall_System_Reflection_Module_GetGlobalType,
	"System.Reflection.Module::GetGuidInternal", ves_icall_System_Reflection_Module_GetGuidInternal,
	"System.Reflection.Module::InternalGetTypes", ves_icall_System_Reflection_Module_InternalGetTypes,

	/*
	 * System.MonoType.
	 */
	"System.MonoType::getFullName", ves_icall_System_MonoType_getFullName,
	"System.MonoType::type_from_obj", mono_type_type_from_obj,
	"System.MonoType::GetElementType", ves_icall_MonoType_GetElementType,
	"System.MonoType::GetArrayRank", ves_icall_MonoType_GetArrayRank,
	"System.MonoType::get_BaseType", ves_icall_get_type_parent,
	"System.MonoType::get_Module", ves_icall_MonoType_get_Module,
	"System.MonoType::get_Assembly", ves_icall_MonoType_get_Assembly,
	"System.MonoType::get_DeclaringType", ves_icall_MonoType_get_DeclaringType,
	"System.MonoType::get_UnderlyingSystemType", ves_icall_MonoType_get_UnderlyingSystemType,
	"System.MonoType::get_Name", ves_icall_MonoType_get_Name,
	"System.MonoType::get_Namespace", ves_icall_MonoType_get_Namespace,
	"System.MonoType::IsPointerImpl", ves_icall_type_ispointer,
	"System.MonoType::IsPrimitiveImpl", ves_icall_type_isprimitive,
	"System.MonoType::IsByRefImpl", ves_icall_type_isbyref,
	"System.MonoType::GetField", ves_icall_Type_GetField,
	"System.MonoType::GetFields", ves_icall_Type_GetFields,
	"System.MonoType::GetMethods", ves_icall_Type_GetMethods,
	"System.MonoType::GetConstructors", ves_icall_Type_GetConstructors,
	"System.MonoType::GetProperties", ves_icall_Type_GetProperties,
	"System.MonoType::GetEvents", ves_icall_Type_GetEvents,
	"System.MonoType::InternalGetEvent", ves_icall_MonoType_GetEvent,
	"System.MonoType::GetInterfaces", ves_icall_Type_GetInterfaces,
	"System.MonoType::GetNestedTypes", ves_icall_Type_GetNestedTypes,
	"System.MonoType::GetNestedType", ves_icall_Type_GetNestedType,

	/*
	 * System.Net.Sockets I/O Services
	 */
	"System.Net.Sockets.Socket::Socket_internal", ves_icall_System_Net_Sockets_Socket_Socket_internal,
	"System.Net.Sockets.Socket::Close_internal", ves_icall_System_Net_Sockets_Socket_Close_internal,
	"System.Net.Sockets.SocketException::WSAGetLastError_internal", ves_icall_System_Net_Sockets_SocketException_WSAGetLastError_internal,
	"System.Net.Sockets.Socket::Available_internal", ves_icall_System_Net_Sockets_Socket_Available_internal,
	"System.Net.Sockets.Socket::Blocking_internal", ves_icall_System_Net_Sockets_Socket_Blocking_internal,
	"System.Net.Sockets.Socket::Accept_internal", ves_icall_System_Net_Sockets_Socket_Accept_internal,
	"System.Net.Sockets.Socket::Listen_internal", ves_icall_System_Net_Sockets_Socket_Listen_internal,
	"System.Net.Sockets.Socket::LocalEndPoint_internal", ves_icall_System_Net_Sockets_Socket_LocalEndPoint_internal,
	"System.Net.Sockets.Socket::RemoteEndPoint_internal", ves_icall_System_Net_Sockets_Socket_RemoteEndPoint_internal,
	"System.Net.Sockets.Socket::Bind_internal", ves_icall_System_Net_Sockets_Socket_Bind_internal,
	"System.Net.Sockets.Socket::Connect_internal", ves_icall_System_Net_Sockets_Socket_Connect_internal,
	"System.Net.Sockets.Socket::Receive_internal", ves_icall_System_Net_Sockets_Socket_Receive_internal,
	"System.Net.Sockets.Socket::RecvFrom_internal", ves_icall_System_Net_Sockets_Socket_RecvFrom_internal,
	"System.Net.Sockets.Socket::Send_internal", ves_icall_System_Net_Sockets_Socket_Send_internal,
	"System.Net.Sockets.Socket::SendTo_internal", ves_icall_System_Net_Sockets_Socket_SendTo_internal,
	"System.Net.Sockets.Socket::Select_internal", ves_icall_System_Net_Sockets_Socket_Select_internal,
	"System.Net.Sockets.Socket::Shutdown_internal", ves_icall_System_Net_Sockets_Socket_Shutdown_internal,
	"System.Net.Sockets.Socket::GetSocketOption_obj_internal", ves_icall_System_Net_Sockets_Socket_GetSocketOption_obj_internal,
	"System.Net.Sockets.Socket::GetSocketOption_arr_internal", ves_icall_System_Net_Sockets_Socket_GetSocketOption_arr_internal,
	"System.Net.Sockets.Socket::SetSocketOption_internal", ves_icall_System_Net_Sockets_Socket_SetSocketOption_internal,
	"System.Net.Dns::GetHostByName_internal(string,string&,string[]&,string[]&)", ves_icall_System_Net_Dns_GetHostByName_internal,
	"System.Net.Dns::GetHostByAddr_internal(string,string&,string[]&,string[]&)", ves_icall_System_Net_Dns_GetHostByAddr_internal,
	"System.Net.Dns::GetHostName_internal(string&)", ves_icall_System_Net_Dns_GetHostName_internal,

	/*
	 * System.Char
	 */
	"System.Char::GetNumericValue", ves_icall_System_Char_GetNumericValue,
	"System.Char::GetUnicodeCategory", ves_icall_System_Char_GetUnicodeCategory,
	"System.Char::IsControl", ves_icall_System_Char_IsControl,
	"System.Char::IsDigit", ves_icall_System_Char_IsDigit,
	"System.Char::IsLetter", ves_icall_System_Char_IsLetter,
	"System.Char::IsLower", ves_icall_System_Char_IsLower,
	"System.Char::IsUpper", ves_icall_System_Char_IsUpper,
	"System.Char::IsNumber", ves_icall_System_Char_IsNumber,
	"System.Char::IsPunctuation", ves_icall_System_Char_IsPunctuation,
	"System.Char::IsSeparator", ves_icall_System_Char_IsSeparator,
	"System.Char::IsSurrogate", ves_icall_System_Char_IsSurrogate,
	"System.Char::IsSymbol", ves_icall_System_Char_IsSymbol,
	"System.Char::IsWhiteSpace", ves_icall_System_Char_IsWhiteSpace,
	"System.Char::ToLower", ves_icall_System_Char_ToLower,
	"System.Char::ToUpper", ves_icall_System_Char_ToUpper,

	/*
	 * System.Text.Encoding
	 */
	"System.Text.Encoding::InternalCodePage", ves_icall_System_Text_Encoding_InternalCodePage,

	"System.DateTime::GetNow", ves_icall_System_DateTime_GetNow,
	"System.CurrentTimeZone::GetTimeZoneData", ves_icall_System_CurrentTimeZone_GetTimeZoneData,

	/*
	 * System.GC
	 */
	"System.GC::InternalCollect", ves_icall_System_GC_InternalCollect,
	"System.GC::GetTotalMemory", ves_icall_System_GC_GetTotalMemory,
	"System.GC::KeepAlive", ves_icall_System_GC_KeepAlive,
	"System.GC::ReRegisterForFinalize", ves_icall_System_GC_ReRegisterForFinalize,
	"System.GC::SuppressFinalize", ves_icall_System_GC_SuppressFinalize,
	"System.GC::WaitForPendingFinalizers", ves_icall_System_GC_WaitForPendingFinalizers,
	"System.Runtime.InteropServices.GCHandle::GetTarget", ves_icall_System_GCHandle_GetTarget,
	"System.Runtime.InteropServices.GCHandle::GetTargetHandle", ves_icall_System_GCHandle_GetTargetHandle,
	"System.Runtime.InteropServices.GCHandle::FreeHandle", ves_icall_System_GCHandle_FreeHandle,
	"System.Runtime.InteropServices.GCHandle::GetAddrOfPinnedObject", ves_icall_System_GCHandle_GetAddrOfPinnedObject,

	/*
	 * System.Security.Cryptography calls
	 */

	 "System.Security.Cryptography.RNGCryptoServiceProvider::InternalGetBytes", ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_InternalGetBytes,
	 "System.Security.Cryptography.RNGCryptoServiceProvider::InternalGetNonZeroBytes", ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_InternalGetNonZeroBytes,
	
	/*
	 * System.Buffer
	 */
	"System.Buffer::ByteLengthInternal", ves_icall_System_Buffer_ByteLengthInternal,
	"System.Buffer::GetByteInternal", ves_icall_System_Buffer_GetByteInternal,
	"System.Buffer::SetByteInternal", ves_icall_System_Buffer_SetByteInternal,
	"System.Buffer::BlockCopyInternal", ves_icall_System_Buffer_BlockCopyInternal,
	
	/*
	 * System.IO.MonoIO
	 */
	"System.IO.MonoIO::CreateDirectory(string,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_CreateDirectory,
	"System.IO.MonoIO::RemoveDirectory(string,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_RemoveDirectory,
	"System.IO.MonoIO::FindFirstFile(string,System.IO.MonoIOStat&,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_FindFirstFile,
	"System.IO.MonoIO::FindNextFile(intptr,System.IO.MonoIOStat&,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_FindNextFile,
	"System.IO.MonoIO::FindClose(intptr,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_FindClose,
	"System.IO.MonoIO::GetCurrentDirectory(System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_GetCurrentDirectory,
	"System.IO.MonoIO::SetCurrentDirectory(string,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_SetCurrentDirectory,
	"System.IO.MonoIO::MoveFile(string,string,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_MoveFile,
	"System.IO.MonoIO::CopyFile(string,string,bool,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_CopyFile,
	"System.IO.MonoIO::DeleteFile(string,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_DeleteFile,
	"System.IO.MonoIO::GetFileAttributes(string,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_GetFileAttributes,
	"System.IO.MonoIO::SetFileAttributes(string,System.IO.FileAttributes,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_SetFileAttributes,
	"System.IO.MonoIO::GetFileType(intptr,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_GetFileType,
	"System.IO.MonoIO::GetFileStat(string,System.IO.MonoIOStat&,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_GetFileStat,
	"System.IO.MonoIO::Open(string,System.IO.FileMode,System.IO.FileAccess,System.IO.FileShare,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_Open,
	"System.IO.MonoIO::Close(intptr,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_Close,
	"System.IO.MonoIO::Read(intptr,byte[],int,int,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_Read,
	"System.IO.MonoIO::Write(intptr,byte[],int,int,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_Write,
	"System.IO.MonoIO::Seek(intptr,long,System.IO.SeekOrigin,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_Seek,
	"System.IO.MonoIO::GetLength(intptr,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_GetLength,
	"System.IO.MonoIO::SetLength(intptr,long,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_SetLength,
	"System.IO.MonoIO::SetFileTime(intptr,long,long,long,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_SetFileTime,
	"System.IO.MonoIO::Flush(intptr,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_Flush,
	"System.IO.MonoIO::get_ConsoleOutput", ves_icall_System_IO_MonoIO_get_ConsoleOutput,
	"System.IO.MonoIO::get_ConsoleInput", ves_icall_System_IO_MonoIO_get_ConsoleInput,
	"System.IO.MonoIO::get_ConsoleError", ves_icall_System_IO_MonoIO_get_ConsoleError,
	"System.IO.MonoIO::CreatePipe(intptr&,intptr&)", ves_icall_System_IO_MonoIO_CreatePipe,
	"System.IO.MonoIO::get_VolumeSeparatorChar", ves_icall_System_IO_MonoIO_get_VolumeSeparatorChar,
	"System.IO.MonoIO::get_DirectorySeparatorChar", ves_icall_System_IO_MonoIO_get_DirectorySeparatorChar,
	"System.IO.MonoIO::get_AltDirectorySeparatorChar", ves_icall_System_IO_MonoIO_get_AltDirectorySeparatorChar,
	"System.IO.MonoIO::get_PathSeparator", ves_icall_System_IO_MonoIO_get_PathSeparator,
	"System.IO.MonoIO::get_InvalidPathChars", ves_icall_System_IO_MonoIO_get_InvalidPathChars,
	"System.IO.MonoIO::GetTempPath(string&)", ves_icall_System_IO_MonoIO_GetTempPath,

	/*
	 * System.Math
	 */
	"System.Math::Floor", ves_icall_System_Math_Floor,
	"System.Math::Round", ves_icall_System_Math_Round,
	"System.Math::Round2", ves_icall_System_Math_Round2,
	"System.Math::Sin", ves_icall_System_Math_Sin,
        "System.Math::Cos", ves_icall_System_Math_Cos,
        "System.Math::Tan", ves_icall_System_Math_Tan,
        "System.Math::Sinh", ves_icall_System_Math_Sinh,
        "System.Math::Cosh", ves_icall_System_Math_Cosh,
        "System.Math::Tanh", ves_icall_System_Math_Tanh,
        "System.Math::Acos", ves_icall_System_Math_Acos,
        "System.Math::Asin", ves_icall_System_Math_Asin,
        "System.Math::Atan", ves_icall_System_Math_Atan,
        "System.Math::Atan2", ves_icall_System_Math_Atan2,
        "System.Math::Exp", ves_icall_System_Math_Exp,
        "System.Math::Log", ves_icall_System_Math_Log,
        "System.Math::Log10", ves_icall_System_Math_Log10,
        "System.Math::Pow", ves_icall_System_Math_Pow,
        "System.Math::Sqrt", ves_icall_System_Math_Sqrt,

	/*
	 * System.Environment
	 */
	"System.Environment::get_MachineName", ves_icall_System_Environment_get_MachineName,
	"System.Environment::get_NewLine", ves_icall_System_Environment_get_NewLine,
	"System.Environment::GetEnvironmentVariable", ves_icall_System_Environment_GetEnvironmentVariable,
	"System.Environment::GetEnvironmentVariableNames", ves_icall_System_Environment_GetEnvironmentVariableNames,
	"System.Environment::GetCommandLineArgs", mono_runtime_get_main_args,
	"System.Environment::get_TickCount", ves_icall_System_Environment_get_TickCount,
	"System.Environment::Exit", ves_icall_System_Environment_Exit,
	"System.Environment::get_Platform", ves_icall_System_Environment_get_Platform,
	"System.Environment::get_ExitCode", mono_environment_exitcode_get,
	"System.Environment::set_ExitCode", mono_environment_exitcode_set,
	"System.Environment::GetMachineConfigPath",	ves_icall_System_Configuration_DefaultConfig_get_machine_config_path,

	/*
	 * System.Runtime.Remoting
	 */	
	"System.Runtime.Remoting.RemotingServices::InternalExecute",
	ves_icall_InternalExecute,
	"System.Runtime.Remoting.RemotingServices::IsTransparentProxy",
	ves_icall_IsTransparentProxy,

	/*
	 * System.Runtime.Remoting.Activation
	 */	
	"System.Runtime.Remoting.Activation.ActivationServices::AllocateUninitializedClassInstance",
	ves_icall_System_Runtime_Activation_ActivationServices_AllocateUninitializedClassInstance,
	"System.Runtime.Remoting.Activation.ActivationServices::EnableProxyActivation",
	ves_icall_System_Runtime_Activation_ActivationServices_EnableProxyActivation,

	/*
	 * System.Runtime.Remoting.Messaging
	 */	
	"System.Runtime.Remoting.Messaging.MonoMethodMessage::InitMessage",
	ves_icall_MonoMethodMessage_InitMessage,
	
	/*
	 * System.Runtime.Remoting.Proxies
	 */	
	"System.Runtime.Remoting.Proxies.RealProxy::InternalGetTransparentProxy", 
	ves_icall_Remoting_RealProxy_GetTransparentProxy,

	/*
	 * System.Threading.Interlocked
	 */
	"System.Threading.Interlocked::Increment(int&)", ves_icall_System_Threading_Interlocked_Increment_Int,
	"System.Threading.Interlocked::Increment(long&)", ves_icall_System_Threading_Interlocked_Increment_Long,
	"System.Threading.Interlocked::Decrement(int&)", ves_icall_System_Threading_Interlocked_Decrement_Int,
	"System.Threading.Interlocked::Decrement(long&)", ves_icall_System_Threading_Interlocked_Decrement_Long,
	"System.Threading.Interlocked::CompareExchange(int&,int,int)", ves_icall_System_Threading_Interlocked_CompareExchange_Int,
	"System.Threading.Interlocked::CompareExchange(object&,object,object)", ves_icall_System_Threading_Interlocked_CompareExchange_Object,
	"System.Threading.Interlocked::CompareExchange(single&,single,single)", ves_icall_System_Threading_Interlocked_CompareExchange_Single,
	"System.Threading.Interlocked::Exchange(int&,int)", ves_icall_System_Threading_Interlocked_Exchange_Int,
	"System.Threading.Interlocked::Exchange(object&,object)", ves_icall_System_Threading_Interlocked_Exchange_Object,
	"System.Threading.Interlocked::Exchange(single&,single)", ves_icall_System_Threading_Interlocked_Exchange_Single,
	"System.Threading.Thread::current_lcid()", ves_icall_System_Threading_Thread_current_lcid,

	/*
	 * System.Diagnostics.Process
	 */
	"System.Diagnostics.Process::GetProcess_internal(int)", ves_icall_System_Diagnostics_Process_GetProcess_internal,
	"System.Diagnostics.Process::GetProcesses_internal()", ves_icall_System_Diagnostics_Process_GetProcesses_internal,
	"System.Diagnostics.Process::GetPid_internal()", ves_icall_System_Diagnostics_Process_GetPid_internal,
	"System.Diagnostics.Process::Process_free_internal(intptr)", ves_icall_System_Diagnostics_Process_Process_free_internal,
	"System.Diagnostics.Process::GetModules_internal()", ves_icall_System_Diagnostics_Process_GetModules_internal,
	"System.Diagnostics.Process::Start_internal(string,string,intptr,intptr,intptr,System.Diagnostics.Process/ProcInfo&)", ves_icall_System_Diagnostics_Process_Start_internal,
	"System.Diagnostics.Process::WaitForExit_internal(intptr,int)", ves_icall_System_Diagnostics_Process_WaitForExit_internal,
	"System.Diagnostics.Process::ExitTime_internal(intptr)", ves_icall_System_Diagnostics_Process_ExitTime_internal,
	"System.Diagnostics.Process::StartTime_internal(intptr)", ves_icall_System_Diagnostics_Process_StartTime_internal,
	"System.Diagnostics.Process::ExitCode_internal(intptr)", ves_icall_System_Diagnostics_Process_ExitCode_internal,
	"System.Diagnostics.Process::ProcessName_internal(intptr)", ves_icall_System_Diagnostics_Process_ProcessName_internal,
	"System.Diagnostics.Process::GetWorkingSet_internal(intptr,int&,int&)", ves_icall_System_Diagnostics_Process_GetWorkingSet_internal,
	"System.Diagnostics.Process::SetWorkingSet_internal(intptr,int,int,bool)", ves_icall_System_Diagnostics_Process_SetWorkingSet_internal,
	"System.Diagnostics.FileVersionInfo::GetVersionInfo_internal(string)", ves_icall_System_Diagnostics_FileVersionInfo_GetVersionInfo_internal,

	/* 
	 * System.Delegate
	 */
	"System.Delegate::CreateDelegate_internal", ves_icall_System_Delegate_CreateDelegate_internal,

	/*
	 * System.IO.Path
	 */
	"System.IO.Path::get_temp_path", ves_icall_System_IO_get_temp_path,

	/*
	 * Private icalls for the Mono Debugger
	 */
	"System.Reflection.Assembly::MonoDebugger_GetMethod",
	ves_icall_MonoDebugger_GetMethod,

	"System.Reflection.Assembly::MonoDebugger_GetMethodToken",
	ves_icall_MonoDebugger_GetMethodToken,

	"System.Reflection.Assembly::MonoDebugger_GetLocalTypeFromSignature",
	ves_icall_MonoDebugger_GetLocalTypeFromSignature,

	"System.Reflection.Assembly::MonoDebugger_GetType",
	ves_icall_MonoDebugger_GetType,

	/*
	 * System.Configuration
	 */
	"System.Configuration.DefaultConfig::get_machine_config_path",
	ves_icall_System_Configuration_DefaultConfig_get_machine_config_path,

	/*
	 * System.Diagnostics.DefaultTraceListener
	 */
	"System.Diagnostics.DefaultTraceListener::WriteWindowsDebugString",
	ves_icall_System_Diagnostics_DefaultTraceListener_WriteWindowsDebugString,
	/*
	 * System.Activator
	 */
	"System.Activator::CreateInstanceInternal",
	ves_icall_System_Activator_CreateInstanceInternal,

	/* 
	 * System.Web
	 */
	"System.Web.Util.ICalls::GetMachineConfigPath",
	ves_icall_System_Configuration_DefaultConfig_get_machine_config_path,

	"System.Web.Util.ICalls::GetMachineInstallDirectory",
	ves_icall_System_Web_Util_ICalls_get_machine_install_dir,

	/*
	 * System.Globalization
	 */
	"System.Globalization.CultureInfo::construct_internal_locale(string)", ves_icall_System_Globalization_CultureInfo_construct_internal_locale,
	"System.Globalization.CultureInfo::construct_compareinfo(object,string)", ves_icall_System_Globalization_CultureInfo_construct_compareinfo,
	"System.Globalization.CompareInfo::internal_compare(string,string,System.Globalization.CompareOptions)", ves_icall_System_Globalization_CompareInfo_internal_compare,
	"System.Globalization.CompareInfo::free_internal_collator()", ves_icall_System_Globalization_CompareInfo_free_internal_collator,
	"System.Globalization.CompareInfo::assign_sortkey(object,string,System.Globalization.CompareOptions)", ves_icall_System_Globalization_CompareInfo_assign_sortkey,
	"System.Globalization.CompareInfo::internal_index(string,int,int,string,System.Globalization.CompareOptions,bool)", ves_icall_System_Globalization_CompareInfo_internal_index,
	"System.String::InternalReplace(string,string,System.Globalization.CompareInfo)", ves_icall_System_String_InternalReplace_Str_Comp,
	"System.String::InternalToLower(System.Globalization.CultureInfo)", ves_icall_System_String_InternalToLower_Comp,
	"System.String::InternalToUpper(System.Globalization.CultureInfo)", ves_icall_System_String_InternalToUpper_Comp,

	/*
	 * add other internal calls here
	 */

	/* These will be deleted after the next release */
	"System.String::InternalEquals", ves_icall_System_String_InternalEquals,
	"System.String::InternalIndexOf(char,int,int)", ves_icall_System_String_InternalIndexOf_Char,
	"System.String::InternalIndexOf(string,int,int)", ves_icall_System_String_InternalIndexOf_Str,
	"System.String::InternalLastIndexOf(char,int,int)", ves_icall_System_String_InternalLastIndexOf_Char,
	"System.String::InternalLastIndexOf(string,int,int)", ves_icall_System_String_InternalLastIndexOf_Str,
	"System.String::InternalCompare(string,int,string,int,int,int)", ves_icall_System_String_InternalCompareStr_N,
	"System.String::InternalReplace(string,string)", ves_icall_System_String_InternalReplace_Str,
	"System.String::InternalToLower()", ves_icall_System_String_InternalToLower,
	"System.String::InternalToUpper()", ves_icall_System_String_InternalToUpper,

	NULL, NULL
};

void
mono_init_icall (void)
{
	const char *name;
	int i = 0;

	while ((name = icall_map [i])) {
		mono_add_internal_call (name, icall_map [i+1]);
		i += 2;
	}
       
}


