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
#include <sys/time.h>
#include <unistd.h>

#include <mono/metadata/object.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/file-io.h>
#include "decimal.h"

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
	ea = (gpointer*)((char*)ao->vector + (pos * esize));

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
	ea = (gpointer*)((char*)ao->vector + (pos * esize));

	if (ac->element_class->valuetype) {
		g_assert (vc->valuetype);

		memcpy (ea, (char *)vo + sizeof (MonoObject), esize);
	} else
		*ea = (gpointer)vo;

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

static void
ves_icall_InitializeArray (MonoArray *array, MonoClassField *field_handle)
{
		guint32 size = mono_array_element_size (((MonoObject*) array)->klass) * mono_array_length (array);
		/*
		 * FIXME: ENOENDIAN: we need to byteswap as needed.
		 */
		memcpy (mono_array_addr (array, char, 0), field_handle->data, size);
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

static void
mono_type_type_from_obj (MonoReflectionType *mtype, MonoObject *obj)
{
	mtype->type = &obj->klass->byval_arg;
}

static gint32
ves_icall_get_data_chunk (MonoReflectionAssemblyBuilder *assb, gint32 type, MonoArray *buf)
{
	int count;

	if (type == 0) { /* get the header */
		count = mono_image_get_header (assb, (char*)buf->vector, buf->bounds->length);
		if (count != -1)
			return count;
	} else {
		MonoDynamicAssembly *ass = assb->dynamic_assembly;
		char *p = mono_array_addr (buf, char, 0);
		count = ass->code.index + ass->meta_size;
		if (count > buf->bounds->length)
			return 0;
		memcpy (p, ass->code.data, ass->code.index);
		memcpy (p + ass->code.index, ass->assembly.image->raw_metadata, ass->meta_size);
		return count;
	}
	
	return 0;
}

static MonoObject*
ves_icall_type_from_name (MonoObject *name)
{
	return NULL;
}

static MonoReflectionType*
ves_icall_type_from_handle (MonoType *handle)
{
	return mono_type_get_object (handle);
}

static guint32
ves_icall_type_is_subtype_of (MonoReflectionType *type, MonoReflectionType *c)
{
	MonoClass *klass = mono_class_from_mono_type (type->type);
	MonoClass *klassc = mono_class_from_mono_type (c->type);

	/* cut&paste from mono_object_isinst (): keep in sync */
	if (klassc->flags & TYPE_ATTRIBUTE_INTERFACE) {
		if ((klassc->interface_id <= klass->max_interface_id) &&
		    klass->interface_offsets [klassc->interface_id])
			return 1;
	} else {
		if ((klass->baseval - klassc->baseval) <= klassc->diffval)
			return 1;
	}
	return 0;
}

static guint32
ves_icall_get_attributes (MonoReflectionType *type)
{
	MonoClass *klass = mono_class_from_mono_type (type->type);

	return klass->flags;
}

static void
ves_icall_get_method_info (MonoMethod *method, MonoMethodInfo *info)
{
	info->parent = mono_type_get_object (&method->klass->byval_arg);
	info->ret = mono_type_get_object (method->signature->ret);
	info->name = mono_string_new (method->name);
	info->attrs = method->flags;
	info->implattrs = method->iflags;
}

static void
ves_icall_get_field_info (MonoReflectionField *field, MonoFieldInfo *info)
{
	info->parent = mono_type_get_object (field->klass);
	info->type = mono_type_get_object (field->field->type);
	info->name = mono_string_new (field->field->name);
	info->attrs = field->field->type->attrs;
}

static void
ves_icall_get_type_info (MonoType *type, MonoTypeInfo *info)
{
	MonoClass *class = mono_class_from_mono_type (type);
	MonoClass *parent;
	MonoArray *intf;
	int ninterf, i;
	
	info->parent = class->parent ? mono_type_get_object (&class->parent->byval_arg): NULL;
	info->name = mono_string_new (class->name);
	info->name_space = mono_string_new (class->name_space);
	info->attrs = class->flags;
	info->assembly = NULL; /* FIXME */
	if (class->enumtype)
		info->etype = mono_type_get_object (class->enum_basetype);
	else if (class->element_class)
		info->etype = mono_type_get_object (&class->element_class->byval_arg);
	else
		info->etype = NULL;

	ninterf = 0;
	for (parent = class; parent; parent = parent->parent) {
		ninterf += parent->interface_count;
	}
	intf = mono_array_new (mono_defaults.monotype_class, ninterf);
	ninterf = 0;
	for (parent = class; parent; parent = parent->parent) {
		for (i = 0; i < parent->interface_count; ++i) {
			mono_array_set (intf, gpointer, ninterf, mono_type_get_object (&parent->interfaces [i]->byval_arg));
			++ninterf;
		}
	}
	info->interfaces = intf;
}

static MonoMethod*
search_method (MonoReflectionType *type, char *name, guint32 flags, MonoArray *args)
{
	MonoClass *klass;
	MonoMethod *m;
	MonoReflectionType *paramt;
	int i, j;

	klass = mono_class_from_mono_type (type->type);
	for (i = 0; i < klass->method.count; ++i) {
		m = klass->methods [i];
		if (!((m->flags & flags) == flags))
			continue;
		if (strcmp(m->name, name))
			continue;
		if (m->signature->param_count != mono_array_length (args))
			continue;
		for (j = 0; j < m->signature->param_count; ++j) {
			paramt = mono_array_get (args, MonoReflectionType*, j);
			if (!mono_metadata_type_equal (paramt->type, m->signature->params [j]))
				break;
		}
		if (j == m->signature->param_count)
			return m;
	}
	g_print ("Method %s::%s (%d) not found\n", klass->name, name, mono_array_length (args));
	return NULL;
}

static MonoReflectionMethod*
ves_icall_get_constructor (MonoReflectionType *type, MonoArray *args)
{
	MonoMethod *m;

	m = search_method (type, ".ctor", METHOD_ATTRIBUTE_RT_SPECIAL_NAME, args);
	if (m)
		return mono_method_get_object (m);
	return NULL;
}

static MonoReflectionMethod*
ves_icall_get_method (MonoReflectionType *type, MonoString *name, MonoArray *args)
{
	MonoMethod *m;
	char *n = mono_string_to_utf8 (name);

	m = search_method (type, n, 0, args);
	g_free (n);
	if (m)
		return mono_method_get_object (m);
	return NULL;
}

typedef int (*MemberFilter) (MonoObject *member, MonoObject *criteria);

static MonoArray*
ves_icall_type_find_members (MonoReflectionType *type, guint32 membertypes, guint32 bflags, MemberFilter filter, MonoObject *criteria)
{
	GSList *l = NULL, *tmp;
	MonoClass *klass;
	MonoArray *res;
	int i, is_ctor, len;

	klass = mono_class_from_mono_type (type->type);

	/* FIXME: check the bindingflags */
	
	if (membertypes & (1|8)) { /* constructors and methods */
		for (i = 0; i < klass->method.count; ++i) {
			is_ctor = strcmp (klass->methods [i]->name, ".ctor") == 0 ||
					strcmp (klass->methods [i]->name, ".cctor") == 0;
			if (is_ctor && (membertypes & 1))
				l = g_slist_prepend (l, mono_method_get_object (klass->methods [i]));
			if (klass->methods [i]->flags & METHOD_ATTRIBUTE_SPECIAL_NAME)
				continue;
			if (!is_ctor && (membertypes & 8))
				l = g_slist_prepend (l, mono_method_get_object (klass->methods [i]));
		}
	}
	if (membertypes & 4) { /* fields */
		for (i = 0; i < klass->field.count; ++i) {
			l = g_slist_prepend (l, mono_field_get_object (klass, &klass->fields [i]));
		}
	}
	len = g_slist_length (l);
	klass = mono_class_from_name (mono_defaults.corlib, "System.Reflection", "MemberInfo");
	res = mono_array_new (klass, len);
	i = 0;
	tmp = l;
	for (; tmp; tmp = tmp->next, ++i)
		mono_array_set (res, gpointer, i, tmp->data);
	g_slist_free (l);
	return res;
}

static gpointer
ves_icall_System_Runtime_InteropServices_Marshal_ReadIntPtr (gpointer ptr)
{
	return (gpointer)(*(int *)ptr);
}

static MonoString*
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringAuto (gpointer ptr)
{
	return mono_string_new ((char *)ptr);
}

static MonoReflectionAssembly*
ves_icall_System_Reflection_Assembly_LoadFrom (MonoString *assName, MonoObject *evidence)
{
	/* FIXME : examine evidence? */
	char *name = mono_string_to_utf8 (assName);
	enum MonoImageOpenStatus status = MONO_IMAGE_OK;
	MonoAssembly *ass = mono_assembly_open (name, NULL, &status);

	if (!ass)
		mono_raise_exception (mono_exception_from_name (mono_defaults.corlib, "System.IO", "FileNotFoundException"));

	return mono_assembly_get_object (ass);
}

static MonoReflectionType*
ves_icall_System_Reflection_Assembly_GetType (MonoReflectionAssembly *assembly, MonoString *type) /* , char throwOnError, char ignoreCase) */
{
	/* FIXME : use throwOnError and ignoreCase */
	gchar *name, *namespace, *str;
	MonoClass *klass;
	int j = 0;

	str = namespace = mono_string_to_utf8 (type);

	name = strrchr (str, '.');
	if (name) {
		*name = 0;
		++name;
	} else {
		namespace = "";
		name = str;
	}
	klass = mono_class_from_name (assembly->assembly->image, namespace, name);
	g_free (str);
	if (!klass)
		return NULL;
	if (!klass->inited)
		mono_class_init (klass);

	return mono_type_get_object (&klass->byval_arg);
}

static MonoString *
ves_icall_System_MonoType_assQualifiedName (MonoReflectionType *object)
{
	/* FIXME : real rules are more complicated (internal classes,
	  reference types, array types, etc. */
	MonoString *res;
	gchar *fullname;
	MonoClass *klass;
	char *append = NULL;

	switch (object->type->type) {
	case MONO_TYPE_SZARRAY:
		klass = object->type->data.type->data.klass;
		append = "[]";
		break;
	default:
		klass = object->type->data.klass;
		break;
	}

	fullname = g_strconcat (klass->name_space, ".",
	                           klass->name, ",",
	                           klass->image->assembly_name, append, NULL);
	res = mono_string_new (fullname);
	g_free (fullname);

	return res;
}

static MonoString *
ves_icall_System_PAL_GetCurrentDirectory (MonoObject *object)
{
	MonoString *res;
	gchar *path = g_get_current_dir ();
	res = mono_string_new (path);
	g_free (path);
	return res;
}

static gint64
ves_icall_System_DateTime_GetNow ()
{
	struct timeval tv;
	gint64 res;

	// fixme: it seems that .Net has another base time than Unix??
	if (gettimeofday (&tv, NULL) == 0) {
		res = ((gint64)tv.tv_sec * 1000000 + tv.tv_usec)*10;
		return res;
	}

	
	return 0;
}

static gpointer icall_map [] = {
	/*
	 * System.Array
	 */
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
	
	/*
	 * AssemblyBuilder
	 */
	"System.Reflection.Emit.AssemblyBuilder::getDataChunk", ves_icall_get_data_chunk,
	"System.Reflection.Emit.AssemblyBuilder::getUSIndex", mono_image_insert_string,
	"System.Reflection.Emit.AssemblyBuilder::getToken", mono_image_create_token,

	/*
	 * Reflection stuff.
	 */
	"System.Reflection.MonoMethodInfo::get_method_info", ves_icall_get_method_info,
	"System.Reflection.MonoFieldInfo::get_field_info", ves_icall_get_field_info,
	
	/*
	 * TypeBuilder
	 */
	
	/*
	 * MethodBuilder
	 */
	
	/*
	 * System.Type
	 */
	"System.Type::internal_from_name", ves_icall_type_from_name,
	"System.Type::internal_from_handle", ves_icall_type_from_handle,
	"System.Type::get_constructor", ves_icall_get_constructor,
	"System.Type::get_method", ves_icall_get_method,
	"System.Type::get_attributes", ves_icall_get_attributes,
	"System.Type::type_is_subtype_of", ves_icall_type_is_subtype_of,
	"System.Type::FindMembers", ves_icall_type_find_members,

	/*
	 * System.Runtime.CompilerServices.RuntimeHelpers
	 */
	"System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray", ves_icall_InitializeArray,
	
	/*
	 * System.Threading
	 */
	"System.Threading.Thread::Thread_internal", ves_icall_System_Threading_Thread_Thread_internal,
	"System.Threading.Thread::Start_internal", ves_icall_System_Threading_Thread_Start_internal,
	"System.Threading.Thread::Sleep_internal", ves_icall_System_Threading_Thread_Sleep_internal,
	"System.Threading.Thread::CurrentThread_internal", ves_icall_System_Threading_Thread_CurrentThread_internal,
	"System.Threading.Thread::Join_internal", ves_icall_System_Threading_Thread_Join_internal,
	"System.Threading.Thread::SlotHash_lookup", ves_icall_System_Threading_Thread_SlotHash_lookup,
	"System.Threading.Thread::SlotHash_store", ves_icall_System_Threading_Thread_SlotHash_store,
	"System.Threading.Monitor::Monitor_exit", ves_icall_System_Threading_Monitor_Monitor_exit,
	"System.Threading.Monitor::Monitor_test_owner", ves_icall_System_Threading_Monitor_Monitor_test_owner,
	"System.Threading.Monitor::Monitor_test_synchronised", ves_icall_System_Threading_Monitor_Monitor_test_synchronised,
	"System.Threading.Monitor::Monitor_pulse", ves_icall_System_Threading_Monitor_Monitor_pulse,
	"System.Threading.Monitor::Monitor_pulse_all", ves_icall_System_Threading_Monitor_Monitor_pulse_all,
	"System.Threading.Monitor::Monitor_try_enter", ves_icall_System_Threading_Monitor_Monitor_try_enter,
	"System.Threading.Monitor::Monitor_wait", ves_icall_System_Threading_Monitor_Monitor_wait,

	/*
	 * System.Threading.WaitHandle
	 */
	"System.Threading.WaitHandle::WaitAll_internal", ves_icall_System_Threading_WaitHandle_WaitAll_internal,
	"System.Threading.WaitHandle::WaitAny_internal", ves_icall_System_Threading_WaitHandle_WaitAny_internal,
	"System.Threading.WaitHandle::WaitOne_internal", ves_icall_System_Threading_WaitHandle_WaitOne_internal,

	"System.Runtime.InteropServices.Marshal::ReadIntPtr", ves_icall_System_Runtime_InteropServices_Marshal_ReadIntPtr,
	"System.Runtime.InteropServices.Marshal::PtrToStringAuto", ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringAuto,

	"System.Reflection.Assembly::LoadFrom", ves_icall_System_Reflection_Assembly_LoadFrom,
	"System.Reflection.Assembly::GetType", ves_icall_System_Reflection_Assembly_GetType,

	/*
	 * System.MonoType.
	 */
	"System.MonoType::assQualifiedName", ves_icall_System_MonoType_assQualifiedName,
	"System.MonoType::type_from_obj", mono_type_type_from_obj,
	"System.MonoType::get_type_info", ves_icall_get_type_info,

	"System.PAL.OpSys::GetCurrentDirectory", ves_icall_System_PAL_GetCurrentDirectory,

	/*
	 * System.PAL.OpSys I/O Services
	 */
	"System.PAL.OpSys::GetStdHandle", ves_icall_System_PAL_OpSys_GetStdHandle,
	"System.PAL.OpSys::ReadFile", ves_icall_System_PAL_OpSys_ReadFile,
	"System.PAL.OpSys::WriteFile", ves_icall_System_PAL_OpSys_WriteFile,
	"System.PAL.OpSys::SetLengthFile", ves_icall_System_PAL_OpSys_SetLengthFile,
	"System.PAL.OpSys::OpenFile", ves_icall_System_PAL_OpSys_OpenFile,
	"System.PAL.OpSys::CloseFile", ves_icall_System_PAL_OpSys_CloseFile,
	"System.PAL.OpSys::SeekFile", ves_icall_System_PAL_OpSys_SeekFile,
	"System.PAL.OpSys::DeleteFile", ves_icall_System_PAL_OpSys_DeleteFile,
	"System.PAL.OpSys::ExistsFile", ves_icall_System_PAL_OpSys_ExistsFile,
	"System.PAL.OpSys::GetFileTime", ves_icall_System_PAL_OpSys_GetFileTime,
	"System.PAL.OpSys::SetFileTime", ves_icall_System_PAL_OpSys_SetFileTime,

	"System.DateTime::GetNow", ves_icall_System_DateTime_GetNow,
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


