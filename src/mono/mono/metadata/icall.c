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

#include <mono/metadata/loader.h>

#include "interp.h"

static void 
ves_icall_array_Set (MonoInvocation *frame)
{
	stackval *sp = frame->stack_args;
	MonoObject *o;
	MonoArrayObject *ao;
	MonoArrayClass *ac;
	gint32 i, t, pos, esize;
	gpointer ea;

	o = frame->obj;
	ao = (MonoArrayObject *)o;
	ac = (MonoArrayClass *)o->klass;

	g_assert (ac->rank >= 1);

	pos = sp [0].data.i - ao->bounds [0].lower_bound;
	for (i = 1; i < ac->rank; i++) {
		if ((t = sp [i].data.i - ao->bounds [i].lower_bound) >= 
		    ao->bounds [i].length) {
			g_warning ("wrong array index");
			g_assert_not_reached ();
		}
		pos = pos*ao->bounds [i].length + sp [i].data.i - 
			ao->bounds [i].lower_bound;
	}

	esize = mono_array_element_size (ac);
	ea = ao->vector + (pos * esize);
	memcpy (ea, &sp [ac->rank].data.p, esize);
}

static void 
ves_icall_array_Get (MonoInvocation *frame)
{
	stackval *sp = frame->stack_args;
	MonoObject *o;
	MonoArrayObject *ao;
	MonoArrayClass *ac;
	gint32 i, pos, esize;
	gpointer ea;

	o = frame->obj;
	ao = (MonoArrayObject *)o;
	ac = (MonoArrayClass *)o->klass;

	g_assert (ac->rank >= 1);

	pos = sp [0].data.i - ao->bounds [0].lower_bound;
	for (i = 1; i < ac->rank; i++)
		pos = pos*ao->bounds [i].length + sp [i].data.i - 
			ao->bounds [i].lower_bound;

	esize = mono_array_element_size (ac);
	ea = ao->vector + (pos * esize);

	frame->retval->type = VAL_I32; /* fixme: not really true */
	memcpy (&frame->retval->data.p, ea, esize);
}

static void 
ves_icall_System_Array_GetValue (MonoInvocation *frame)
{
	stackval *sp = frame->stack_args;
	MonoArrayObject *ao, *io;
	MonoArrayClass *ac, *ic;
	gint32 i, pos, *ind, esize;
	gpointer *ea;

	g_assert (sp [0].type == VAL_OBJ); /* expect an array of integers */

	io = sp [0].data.p;
	ic = (MonoArrayClass *)io->obj.klass;
	
	ao = (MonoArrayObject *)frame->obj;
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

	frame->retval->type = VAL_OBJ; 

	if (ac->element_class->valuetype)
		frame->retval->data.p = mono_value_box (ac->element_class, ea);
	else
		frame->retval->data.p = ea;
}

static void 
ves_icall_System_Array_SetValue (MonoInvocation *frame)
{
	stackval *sp = frame->stack_args;
	MonoArrayObject *ao, *io, *vo;
	MonoArrayClass *ac, *ic, *vc;
	gint32 i, pos, *ind, esize;
	gpointer *ea;

	g_assert (sp [0].type == VAL_OBJ); /* the value object */
	g_assert (sp [1].type == VAL_OBJ); /* expect an array of integers */

	vo = sp [0].data.p;
	vc = (MonoArrayClass *)vo->obj.klass;

	io = sp [1].data.p;
	ic = (MonoArrayClass *)io->obj.klass;
	
	ao = (MonoArrayObject *)frame->obj;
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
		g_assert (vc->class.valuetype);

		memcpy (ea, (char *)vo + sizeof (MonoObject), esize);
	} else
		ea = (gpointer)vo;
}

static void 
ves_icall_array_ctor (MonoInvocation *frame)
{
	stackval *sp = frame->stack_args;
	MonoObject *o;
	MonoArrayObject *ao;
	MonoArrayClass *ac;
	gint32 i, len, esize;

	o = frame->obj;
	ao = (MonoArrayObject *)o;
	ac = (MonoArrayClass *)o->klass;

	g_assert (ac->rank >= 1);

	len = sp [0].data.i;
	for (i = 1; i < ac->rank; i++)
		len *= sp [i].data.i;

	esize = mono_array_element_size (ac);
	ao->vector = g_malloc0 (len * esize);
	ao->bounds = g_malloc0 (ac->rank * sizeof (MonoArrayBounds));
	
	for (i = 0; i < ac->rank; i++)
		ao->bounds [i].length = sp [i].data.i;
}

static void 
ves_icall_array_bound_ctor (MonoInvocation *frame)
{
	MonoObject *o;
	MonoArrayClass *ac;

	o = frame->obj;
	ac = (MonoArrayClass *)o->klass;

	g_warning ("experimental implementation");
	g_assert_not_reached ();
}

static void 
ves_icall_System_Array_CreateInstance (MonoInvocation *frame)
{
	g_warning ("not implemented");
	g_assert_not_reached ();
}

static void 
ves_icall_System_Array_GetRank (MonoInvocation *frame)
{
	MonoObject *o;

	o = frame->obj;

	frame->retval->data.i = ((MonoArrayClass *)o->klass)->rank;
	frame->retval->type = VAL_I32;
}

static void 
ves_icall_System_Array_GetLength (MonoInvocation *frame)
{
	stackval *sp = frame->stack_args;
	MonoObject *o;

	o = frame->obj;

	frame->retval->data.i = ((MonoArrayObject *)o)->bounds [sp [0].data.i].length;
	frame->retval->type = VAL_I32;
}

static void 
ves_icall_System_Array_GetLowerBound (MonoInvocation *frame)
{
	stackval *sp = frame->stack_args;
	MonoArrayObject *ao;

	ao = (MonoArrayObject *)frame->obj;

	frame->retval->data.i = ao->bounds [sp [0].data.i].lower_bound;
	frame->retval->type = VAL_I32;
}

static void 
ves_icall_System_Object_MemberwiseClone (MonoInvocation *frame)
{
	frame->retval->type = VAL_OBJ;
	frame->retval->data.p = mono_object_clone (frame->obj);
}

static gpointer icall_map [] = {
	/*
	 * System.Array
	 */
	"__array_Set",                    ves_icall_array_Set,
	"__array_Get",                    ves_icall_array_Get,
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


