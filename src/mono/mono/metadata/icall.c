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
#include <string.h>

#include <mono/metadata/object.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/reflection.h>

static MonoObject *
ves_icall_System_Array_GetValue (MonoObject *this, MonoObject *idxs)
{
	MonoClass *ac, *ic;
	MonoArray *ao, *io;
	gint32 i, pos, *ind, esize;
	gpointer *ea;

	io = (MonoArray *)idxs;
	ic = (MonoClass *)io->obj.klass;
	
	ao = (MonoArray *)this;
	ac = (MonoClass *)ao->obj.klass;

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
	MonoArray *ao, *io, *vo;
	MonoClass *ac, *ic, *vc;
	gint32 i, pos, *ind, esize;
	gpointer *ea;

	vo = (MonoArray *)value;
	vc = (MonoClass *)vo->obj.klass;

	io = (MonoArray *)idxs;
	ic = (MonoClass *)io->obj.klass;
	
	ao = (MonoArray *)this;
	ac = (MonoClass *)ao->obj.klass;

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
		g_assert (vc->valuetype);

		memcpy (ea, (char *)vo + sizeof (MonoObject), esize);
	} else
		*ea = (gpointer)vo;

}

static void 
ves_icall_array_ctor (MonoObject *this, gint32 n1, ...)
{
	va_list ap;
	MonoArray *ao;
	MonoClass *ac;
	gint32 i, s, len, esize;

	va_start (ap, n1);

	ao = (MonoArray *)this;
	ac = (MonoClass *)this->klass;

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
	MonoArray *ao;
	MonoClass *ac;
	gint32 i, s, len, esize;

	va_start (ap, n1);

	ao = (MonoArray *)this;
	ac = (MonoClass *)this->klass;

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
	return this->klass->rank;
}

static gint32
ves_icall_System_Array_GetLength (MonoObject *this, gint32 dimension)
{
	return ((MonoArray *)this)->bounds [dimension].length;
}

static gint32
ves_icall_System_Array_GetLowerBound (MonoObject *this, gint32 dimension)
{
	return ((MonoArray *)this)->bounds [dimension].lower_bound;
}

static MonoObject *
ves_icall_System_Object_MemberwiseClone (MonoObject *this)
{
	return mono_object_clone (this);
}

static MonoObject *
ves_icall_app_get_cur_domain ()
{
	MonoClass *klass = mono_class_from_name (mono_defaults.corlib, "System", "AppDomain");

	return mono_object_new (klass);
}

static MonoObject *
my_mono_new_object (MonoClass *klass, gpointer data)
{
	MonoClassField *field;
	MonoObject *res = mono_object_new (klass);
	gpointer *slot;

	field = mono_class_get_field_from_name (klass, "_impl");
	slot = (gpointer*)((char*)res + field->offset);
	*slot = data;
	return res;
}

static gpointer
object_field_pointer (MonoObject *obj, char *name)
{
	MonoClassField *field;
	gpointer *slot;

	field = mono_class_get_field_from_name (obj->klass, name);
	slot = (gpointer*)((char*)obj + field->offset);
	return *slot;
}

static gpointer
object_impl_pointer (MonoObject *obj)
{
	return object_field_pointer (obj, "_impl");
}

static MonoObject *
ves_icall_app_define_assembly (MonoObject *appdomain, MonoObject *assembly_name, int access)
{
	MonoClass *klass = mono_class_from_name (mono_defaults.corlib, "System.Reflection.Emit", "AssemblyBuilder");
	MonoDynamicAssembly *ass = g_new0 (MonoDynamicAssembly, 1);
	MonoClassField *field;
	MonoObject *name;

	field = mono_class_get_field_from_name (assembly_name->klass, "name");
	name = *(MonoObject**)((char*)assembly_name + field->offset);

	ass->name = mono_string_to_utf8 (name);

	return my_mono_new_object (klass, ass);
}

static gint32
ves_icall_get_data_chunk (MonoObject *assb, gint32 type, MonoArray *buf)
{
	MonoDynamicAssembly *ass = object_impl_pointer (assb);
	MonoObject *ep;
	int count;
	/*
	 * get some info from the object
	 */
	ep = object_field_pointer (assb, "entry_point");
	if (ep)
		ass->entry_point = object_impl_pointer (ep);
	else
		ass->entry_point = 0;

	if (type == 0) { /* get the header */
		count = mono_image_get_header (ass, buf->vector, buf->bounds->length);
		if (count != -1)
			return count;
	} else {
		count = ass->code.index + ass->meta_size;
		if (count > buf->bounds->length)
			return 0;
		memcpy (buf->vector, ass->code.data, ass->code.index);
		memcpy (buf->vector + ass->code.index, ass->assembly.image->raw_metadata, ass->meta_size);
		return count;
	}
	
	return 0;
}

static MonoObject *
ves_icall_define_module (MonoObject *assb, MonoObject *name, MonoObject *fname)
{
	MonoClass *klass = mono_class_from_name (mono_defaults.corlib, "System.Reflection.Emit", "ModuleBuilder");
	MonoModuleBuilder *mb = g_new0 (MonoModuleBuilder, 1);
	MonoDynamicAssembly *ass = object_impl_pointer (assb);
	ass->modules = g_list_prepend (ass->modules, mb);

	mb->name = mono_string_to_utf8 (name);
	mb->fname = mono_string_to_utf8 (fname);

	return my_mono_new_object (klass, mb);
}

static MonoObject *
ves_icall_define_type (MonoObject *moduleb, MonoObject *name, int attrs)
{
	MonoClass *klass = mono_class_from_name (mono_defaults.corlib, "System.Reflection.Emit", "TypeBuilder");
	MonoTypeBuilder *tb = g_new0 (MonoTypeBuilder, 1);
	MonoModuleBuilder *mb = object_impl_pointer (moduleb);
	char *nspace = mono_string_to_utf8 (name);
	char *tname = strrchr (nspace, '.');
	
	if (tname) {
		*tname = 0;
		tname++;
	} else {
		nspace = "";
	}
	
	tb->name = tname;
	tb->nspace = nspace;
	tb->attrs = attrs;
	mb->types = g_list_prepend (mb->types, tb);

	return my_mono_new_object (klass, tb);
}

static MonoObject *
ves_icall_define_method (MonoObject *typeb, MonoObject *name, int attrs, int callconv, MonoObject *rettype, MonoArray *paramtypes)
{
	MonoClass *klass = mono_class_from_name (mono_defaults.corlib, "System.Reflection.Emit", "MethodBuilder");
	MonoMethodBuilder *mb = g_new0 (MonoMethodBuilder, 1);
	MonoTypeBuilder *tb = object_impl_pointer (typeb);

	mb->name = mono_string_to_utf8 (name);
	mb->attrs = attrs;
	mb->callconv = callconv;
	mb->ret = object_impl_pointer (rettype);
	/* ignore params ... */

	if (!tb->has_default_ctor) {
		if (strcmp (".ctor", mb->name) == 0 && paramtypes->bounds->length == 0)
			tb->has_default_ctor = 1;
	}
	
	tb->methods = g_list_prepend (tb->methods, mb);

	return my_mono_new_object (klass, mb);
}

static void
ves_icall_set_method_body (MonoObject *methodb, MonoArray *code, gint32 count)
{
	MonoMethodBuilder *mb = object_impl_pointer (methodb);
	if (code->bounds->length < count) {
		g_warning ("code len is less than count");
		return;
	}
	g_free (mb->code);
	mb->code = g_malloc (count);
	mb->code_size = count;
	memcpy (mb->code, code->vector, count);
}

static MonoObject*
ves_icall_type_from_name (MonoObject *name)
{
	return NULL;
}

static MonoObject*
ves_icall_type_from_handle (gpointer handle)
{
	return my_mono_new_object (mono_defaults.type_class, handle);
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
	 * System.String
	 */
	"System.String::IsInterned", mono_string_is_interned,
	"System.String::Intern", mono_string_intern,

	/*
	 * System.AppDomain
	 */
	"System.AppDomain::getCurDomain", ves_icall_app_get_cur_domain,
	"System.AppDomain::defineAssembly", ves_icall_app_define_assembly,

	/*
	 * ModuleBuilder
	 */
	"System.Reflection.Emit.ModuleBuilder::defineType", ves_icall_define_type,
	
	/*
	 * AssemblyBuilder
	 */
	"System.Reflection.Emit.AssemblyBuilder::defineModule", ves_icall_define_module,
	"System.Reflection.Emit.AssemblyBuilder::getDataChunk", ves_icall_get_data_chunk,
	
	/*
	 * TypeBuilder
	 */
	"System.Reflection.Emit.TypeBuilder::defineMethod", ves_icall_define_method,
	
	/*
	 * MethodBuilder
	 */
	"System.Reflection.Emit.MethodBuilder::set_method_body", ves_icall_set_method_body,
	
	/*
	 * System.Type
	 */
	"System.Type::internal_from_name", ves_icall_type_from_name,
	"System.Type::internal_from_handle", ves_icall_type_from_handle,
	
	/*
	 * System.Threading
	 */
	"System.Threading.Thread::Thread_internal", ves_icall_System_Threading_Thread_Thread_internal,
	"System.Threading.Thread::Start_internal", ves_icall_System_Threading_Thread_Start_internal,
	"System.Threading.Thread::Sleep_internal", ves_icall_System_Threading_Thread_Sleep_internal,
	"System.Threading.Thread::Schedule_internal", ves_icall_System_Threading_Thread_Schedule_internal,
	"System.Threading.Thread::CurrentThread_internal", ves_icall_System_Threading_Thread_CurrentThread_internal,
	"System.Threading.Thread::Join_internal", ves_icall_System_Threading_Thread_Join_internal,
	"System.Threading.Thread::DataSlot_register", ves_icall_System_Threading_Thread_DataSlot_register,
	"System.Threading.Thread::DataSlot_store", ves_icall_System_Threading_Thread_DataSlot_store,
	"System.Threading.Thread::DataSlot_retrieve", ves_icall_System_Threading_Thread_DataSlot_retrieve,
	/* Not in the System.Threading namespace, but part of the same code */
	"System.LocalDataStoreSlot::DataSlot_unregister", ves_icall_System_LocalDataStoreSlot_DataSlot_unregister,
	"System.Threading.Monitor::Monitor_enter", ves_icall_System_Threading_Monitor_Monitor_enter,
	"System.Threading.Monitor::Monitor_exit", ves_icall_System_Threading_Monitor_Monitor_exit,
	"System.Threading.Monitor::Monitor_test_owner", ves_icall_System_Threading_Monitor_Monitor_test_owner,
	"System.Threading.Monitor::Monitor_test_synchronised", ves_icall_System_Threading_Monitor_Monitor_test_synchronised,
	"System.Threading.Monitor::Monitor_pulse", ves_icall_System_Threading_Monitor_Monitor_pulse,
	"System.Threading.Monitor::Monitor_pulse_all", ves_icall_System_Threading_Monitor_Monitor_pulse_all,
	"System.Threading.Monitor::Monitor_try_enter", ves_icall_System_Threading_Monitor_Monitor_try_enter,
	"System.Threading.Monitor::Monitor_wait", ves_icall_System_Threading_Monitor_Monitor_wait,

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


