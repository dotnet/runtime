/*
 * icall.c:
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <stdarg.h>

#include <mono/metadata/object.h>

static MonoObject *
ves_icall_System_Array_GetValue (MonoObject *this, MonoObject *idxs)
{
	MonoArrayObject *ao, *io;
	MonoArrayClass *ac, *ic;
	gint32 i, pos, *ind, esize;
	gpointer *ea;

	io = (MonoArrayObject *)idxs;
	ic = (MonoArrayClass *)io->obj.klass;
	
	ao = (MonoArrayObject *)this;
	ac = (MonoArrayClass *)ao->obj.klass;

	g_assert (ic->rank == 1);
	g_assert (io->bounds [0].length == ac->rank);

	ind = (guint32 *)io->vector;

	pos = ind [0] - ao->bounds [0].lower_bound;
	for (i = 1; i < ac->rank; i++)
		pos = pos*ao->bounds [i].length + ind [i] - 
			ao->bounds [i].lower_bound;

	esize = mono_array_element_size (ac);
	ea = ao->vector + (pos * esize);

	if (ac->element_class->valuetype)
		return mono_value_box (ac->element_class, ea);
	else
		return *ea;
}

static void 
ves_icall_System_Array_SetValue (MonoObject *this, MonoObject *value,
				 MonoObject *idxs)
{
	MonoArrayObject *ao, *io, *vo;
	MonoArrayClass *ac, *ic, *vc;
	gint32 i, pos, *ind, esize;
	gpointer *ea;

	vo = (MonoArrayObject *)value;
	vc = (MonoArrayClass *)vo->obj.klass;

	io = (MonoArrayObject *)idxs;
	ic = (MonoArrayClass *)io->obj.klass;
	
	ao = (MonoArrayObject *)this;
	ac = (MonoArrayClass *)ao->obj.klass;

	g_assert (ic->rank == 1);
	g_assert (io->bounds [0].length == ac->rank);
	g_assert (ac->element_class == vo->obj.klass);

	ind = (guint32 *)io->vector;

	pos = ind [0] - ao->bounds [0].lower_bound;
	for (i = 1; i < ac->rank; i++)
		pos = pos*ao->bounds [i].length + ind [i] - 
			ao->bounds [i].lower_bound;

	esize = mono_array_element_size (ac);
	ea = ao->vector + (pos * esize);

	if (ac->element_class->valuetype) {
		g_assert (vc->klass.valuetype);

		memcpy (ea, (char *)vo + sizeof (MonoObject), esize);
	} else
		*ea = (gpointer)vo;

}

static void 
ves_icall_array_ctor (MonoObject *this, gint32 n1, ...)
{
	va_list ap;
	MonoArrayObject *ao;
	MonoArrayClass *ac;
	gint32 i, s, len, esize;

	va_start (ap, n1);

	ao = (MonoArrayObject *)this;
	ac = (MonoArrayClass *)this->klass;

	g_assert (ac->rank >= 1);

	ao->bounds = g_malloc0 (ac->rank * sizeof (MonoArrayBounds));

	len = n1;
	ao->bounds [0].length = n1;
	for (i = 1; i < ac->rank; i++) {
		s = va_arg (ap, gint32);
		len *= s;
		ao->bounds [i].length = s;
	}

	esize = mono_array_element_size (ac);
	ao->vector = g_malloc0 (len * esize);
}

static void 
ves_icall_array_bound_ctor (MonoObject *this, gint32 n1, ...)
{
	va_list ap;
	MonoArrayObject *ao;
	MonoArrayClass *ac;
	gint32 i, s, len, esize;

	va_start (ap, n1);

	ao = (MonoArrayObject *)this;
	ac = (MonoArrayClass *)this->klass;

	g_assert (ac->rank >= 1);

	ao->bounds = g_malloc0 (ac->rank * sizeof (MonoArrayBounds));

	ao->bounds [0].lower_bound = n1;
	for (i = 1; i < ac->rank; i++)
		ao->bounds [i].lower_bound = va_arg (ap, gint32);

	len = va_arg (ap, gint32);
	ao->bounds [0].length = len;
	for (i = 1; i < ac->rank; i++) {
		s = va_arg (ap, gint32);
		len *= s;
		ao->bounds [i].length = s;
	}

	esize = mono_array_element_size (ac);
	ao->vector = g_malloc0 (len * esize);
}

static void
ves_icall_System_Array_CreateInstance ()
{
	g_warning ("not implemented");
	g_assert_not_reached ();
}


static gint32 
ves_icall_System_Array_GetRank (MonoObject *this)
{
	return ((MonoArrayClass *)this->klass)->rank;
}

static gint32
ves_icall_System_Array_GetLength (MonoObject *this, gint32 dimension)
{
	return ((MonoArrayObject *)this)->bounds [dimension].length;
}

static gint32
ves_icall_System_Array_GetLowerBound (MonoObject *this, gint32 dimension)
{
	return ((MonoArrayObject *)this)->bounds [dimension].lower_bound;
}

static MonoObject *
ves_icall_System_Object_MemberwiseClone (MonoObject *this)
{
	return mono_object_clone (this);
}

static gpointer icall_map [] = {
	/*
	 * System.Array
	 */
	"__array_ctor",                   ves_icall_array_ctor,
	"__array_bound_ctor",             ves_icall_array_bound_ctor,
	"System.Array::GetValue",         ves_icall_System_Array_GetValue,
	"System.Array::SetValue",         ves_icall_System_Array_SetValue,
	"System.Array::GetRank",          ves_icall_System_Array_GetRank,
	"System.Array::GetLength",        ves_icall_System_Array_GetLength,
	"System.Array::GetLowerBound",    ves_icall_System_Array_GetLowerBound,
	"System.Array::CreateInstance",   ves_icall_System_Array_CreateInstance,

	/*
	 * System.Object
	 */
	"System.Object::MemberwiseClone", ves_icall_System_Object_MemberwiseClone,

	/*
	 * add other internal calls here
	 */
	NULL, NULL
};

void
mono_init_icall ()
{
	char *n;
	int i = 0;

	while ((n = icall_map [i])) {
		mono_add_internal_call (n, icall_map [i+1]);
		i += 2;
	}
       
}


