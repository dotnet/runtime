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

#include <mono/cli/cli.h>

#include "interp.h"

static void 
ves_icall_array_Set (MonoMethod *mh, stackval *sp)
{
	MonoObject *o;
	MonoArrayObject *ao;
	MonoArrayClass *ac;
	gint32 i, t, pos;
	gpointer ea;

	g_assert (sp [0].type == VAL_OBJ);

	o = sp [0].data.p;
	ao = (MonoArrayObject *)o;
	ac = (MonoArrayClass *)o->klass;

	g_assert (ac->rank >= 1);

	pos = sp [1].data.i - ao->bounds [0].lower_bound;
	for (i = 1; i < ac->rank; i++) {
		if ((t = sp [i + 1].data.i - ao->bounds [i].lower_bound) >= 
		    ao->bounds [i].length) {
			g_warning ("wrong array index");
			g_assert_not_reached ();
		}
		pos = pos*ao->bounds [i].length + sp [i + 1].data.i - 
			ao->bounds [i].lower_bound;
	}

	ea = ao->vector + (pos * ac->esize);
	memcpy (ea, &sp [ac->rank + 1].data.p, ac->esize);
}

static void 
ves_icall_array_Get (MonoMethod *mh, stackval *sp)
{
	MonoObject *o;
	MonoArrayObject *ao;
	MonoArrayClass *ac;
	gint32 i, pos;
	gpointer ea;

	g_assert (sp [0].type == VAL_OBJ);

	o = sp [0].data.p;
	ao = (MonoArrayObject *)o;
	ac = (MonoArrayClass *)o->klass;

	g_assert (ac->rank >= 1);

	pos = sp [1].data.i - ao->bounds [0].lower_bound;
	for (i = 1; i < ac->rank; i++)
		pos = pos*ao->bounds [i].length + sp [i + 1].data.i - 
			ao->bounds [i].lower_bound;

	ea = ao->vector + (pos * ac->esize);

	sp [0].type = VAL_I32; /* fixme: not really true */
	memcpy (&sp [0].data.p, ea, ac->esize);
}

static void 
ves_icall_System_Array_GetValue (MonoMethod *mh, stackval *sp)
{
	MonoArrayObject *ao, *io;
	MonoArrayClass *ac, *ic;
	gint32 i, pos, *ind;
	gpointer *ea;

	g_assert (sp [0].type == VAL_OBJ);
	g_assert (sp [1].type == VAL_OBJ); /* expect an array of integers */

	io = sp [1].data.p;
	ic = (MonoArrayClass *)io->obj.klass;
	
	ao = (MonoArrayObject *)sp [0].data.p;
	ac = (MonoArrayClass *)ao->obj.klass;

	g_assert (ic->rank == 1);
	g_assert (io->bounds [0].length == ac->rank);

	ind = (guint32 *)io->vector;

	pos = ind [0] - ao->bounds [0].lower_bound;
	for (i = 1; i < ac->rank; i++)
		pos = pos*ao->bounds [i].length + ind [i] - 
			ao->bounds [i].lower_bound;

	ea = ao->vector + (pos * ac->esize);

	sp [0].type = VAL_OBJ; 

	if (ac->class.evaltype)
		sp [0].data.p = mono_value_box (ac->class.image, 
						ac->etype_token, ea);
	else
		sp [0].data.p = ea;
}

static void 
ves_icall_System_Array_SetValue (MonoMethod *mh, stackval *sp)
{
	MonoArrayObject *ao, *io, *vo;
	MonoArrayClass *ac, *ic, *vc;
	gint32 i, pos, *ind;
	gpointer *ea;

	g_assert (sp [0].type == VAL_OBJ);
	g_assert (sp [1].type == VAL_OBJ); /* the value object */
	g_assert (sp [2].type == VAL_OBJ); /* expect an array of integers */

	vo = sp [1].data.p;
	vc = (MonoArrayClass *)vo->obj.klass;

	io = sp [2].data.p;
	ic = (MonoArrayClass *)io->obj.klass;
	
	ao = (MonoArrayObject *)sp [0].data.p;
	ac = (MonoArrayClass *)ao->obj.klass;

	g_assert (ic->rank == 1);
	g_assert (io->bounds [0].length == ac->rank);

	g_assert (ac->etype_token == vc->class.type_token);

	ind = (guint32 *)io->vector;

	pos = ind [0] - ao->bounds [0].lower_bound;
	for (i = 1; i < ac->rank; i++)
		pos = pos*ao->bounds [i].length + ind [i] - 
			ao->bounds [i].lower_bound;

	ea = ao->vector + (pos * ac->esize);

	if (ac->class.evaltype) {
		g_assert (vc->class.valuetype);

		memcpy (ea, (char *)vo + sizeof (MonoObject), ac->esize);
	} else
		ea = (gpointer)vo;
}

static void 
ves_icall_array_ctor (MonoMethod *mh, stackval *sp)
{
	MonoObject *o;
	MonoArrayObject *ao;
	MonoArrayClass *ac;
	gint32 i, len;

	g_assert (sp [0].type == VAL_OBJ);

	o = sp [0].data.p;
	ao = (MonoArrayObject *)o;
	ac = (MonoArrayClass *)o->klass;

	g_assert (ac->rank >= 1);

	len = sp [1].data.i;
	for (i = 1; i < ac->rank; i++)
		len *= sp [i + 1].data.i;

	ao->vector = g_malloc0 (len * ac->esize);
	ao->bounds = g_malloc0 (ac->rank * sizeof (MonoArrayBounds));

	for (i = 0; i < ac->rank; i++)
		ao->bounds [i].length = sp [i + 1].data.i;
}

static void 
ves_icall_array_bound_ctor (MonoMethod *mh, stackval *sp)
{
	MonoObject *o;
	MonoArrayClass *ac;

	g_assert (sp [0].type == VAL_OBJ);

	o = sp [0].data.p;
	ac = (MonoArrayClass *)o->klass;

	g_warning ("experimental implementation");
	g_assert_not_reached ();
}

static void 
ves_icall_System_Array_GetRank (MonoMethod *mh, stackval *sp)
{
	MonoObject *o;

	g_assert (sp [0].type == VAL_OBJ);

	o = sp [0].data.p;

	sp [0].data.i = ((MonoArrayClass *)o->klass)->rank;
	sp [0].type = VAL_I32;
}

static void 
ves_icall_System_Array_GetLength (MonoMethod *mh, stackval *sp)
{
	MonoObject *o;

	g_assert (sp [0].type == VAL_OBJ);

	o = sp [0].data.p;

	sp [0].data.i = ((MonoArrayObject *)o)->bounds [sp [1].data.i].length;
	sp [0].type = VAL_I32;
}

static void 
ves_icall_System_Array_GetLowerBound (MonoMethod *mh, stackval *sp)
{
	MonoArrayObject *ao;

	g_assert (sp [0].type == VAL_OBJ);

	ao = (MonoArrayObject *)sp [0].data.p;

	sp [0].data.i = ao->bounds [sp [1].data.i].lower_bound;
	sp [0].type = VAL_I32;
}

static void 
ves_icall_System_Object_MemberwiseClone (MonoMethod *mh, stackval *sp)
{
	MonoObject *o;

	g_assert (sp [0].type == VAL_OBJ);

	sp [0].data.p = mono_object_clone (sp [0].data.p);
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

	/*
	 * System.Object
	 */
	"System.Object::MemberwiseClone", ves_icall_System_Object_MemberwiseClone,

	/*
	 * add other internal calls here
	 */
	NULL, NULL
};

gpointer
mono_lookup_internal_call (const char *name)
{
	static GHashTable *icall_hash = NULL;
	gpointer res;

	if (!icall_hash) {
		char *n;
		int i = 0;

		icall_hash = g_hash_table_new (g_str_hash , g_str_equal);
		
		while (n = icall_map [i]) {
			g_hash_table_insert (icall_hash, n, icall_map [i+1]);
			i += 2;
		}
	}

	if (!(res = g_hash_table_lookup (icall_hash, name))) {
		g_warning ("cant resolve internal call to \"%s\"", name);
		g_assert_not_reached ();
	}

	return res;
}


