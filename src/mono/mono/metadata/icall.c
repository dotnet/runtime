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
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/unicode.h>
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
	else {
		return *ea;
	}
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
	if (!mono_object_isinst (value, ac->element_class)) {
		g_error ("Array not compatible: %s <= %s", ac->element_class->name, vo->obj.klass->name);
	}

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
	MonoClass *klass = array->obj.klass;
	guint32 size = mono_array_element_size (klass);
	int i;

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

	printf ("Initialize array with elements of %s type\n", klass->element_class->name);

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
	g_assert (mtype->type->type);
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

static MonoReflectionType*
ves_icall_type_from_name (MonoString *name)
{
	MonoClass *klass;
	MonoImage *image;
	MonoTypeNameParse info;
	gchar *str;
	
	str = mono_string_to_utf8 (name);
	/*g_print ("requested type %s\n", str);*/
	if (!mono_reflection_parse_type (str, &info)) {
		g_free (str);
		return NULL;
	}

	if (info.assembly) {
		image = mono_image_loaded (info.assembly);
		/* do we need to load if it's not already loaded? */
		if (!image) {
			g_free (str);
			return NULL;
		}
	} else
		image = mono_defaults.corlib;
	if (info.nest_name) {
		klass = mono_class_from_name (image, info.nest_name_space, info.nest_name);
		if (klass) {
			GList *nested;
			mono_class_init (klass);
			nested = klass->nested_classes;
			while (nested) {
				klass = nested->data;
				if (strcmp (klass->name, info.nest_name) == 0 &&
						strcmp (klass->name_space, info.nest_name_space) == 0)
					break;
				klass = NULL;
			}
		}
	} else {
		klass = mono_class_from_name (image, info.name_space, info.name);
	}
	g_free (str);
	if (!klass)
		return NULL;
	mono_class_init (klass);
	if (info.rank)
		klass = mono_array_class_get (klass, info.rank);
	
	if (info.isbyref || info.ispointer) /* hack */
		return mono_type_get_object (&klass->this_arg);
	else
		return mono_type_get_object (&klass->byval_arg);
}

static MonoReflectionType*
ves_icall_type_from_handle (MonoType *handle)
{
	mono_class_init (handle->data.klass);
	return mono_type_get_object (handle);
}

static guint32
ves_icall_type_is_subtype_of (MonoReflectionType *type, MonoReflectionType *c)
{
	MonoClass *klass;
	MonoClass *klassc;

	g_assert (type != NULL);
	

	if (!c) /* FIXME: dont know what do do here */
		return 0;
	
	while (!type->type) { /* FIXME: hack for TypeBuilder */
		MonoReflectionTypeBuilder *tb = (MonoReflectionTypeBuilder *)type;
		type = tb->parent;
	}

	while (!c->type) { /* FIXME: hack for TypeBuilder */
		MonoReflectionTypeBuilder *tb = (MonoReflectionTypeBuilder *)c;
		c = tb->parent;
	}

	klass = mono_class_from_mono_type (type->type);
	klassc = mono_class_from_mono_type (c->type);

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

static MonoArray*
ves_icall_get_parameter_info (MonoMethod *method) {
	MonoArray *res;
	MonoClass *oklass;
	MonoReflectionParameter** args;
	int i;

	args = mono_param_get_objects (method);
	oklass = mono_class_from_name (mono_defaults.corlib, "System.Reflection", "ParameterInfo");
	res = mono_array_new (oklass, method->signature->param_count);
	for (i = 0; i < method->signature->param_count; ++i) {
		mono_array_set (res, gpointer, i, args [i]);
	}
	return res;
}

static void
ves_icall_get_field_info (MonoReflectionField *field, MonoFieldInfo *info)
{
	info->parent = mono_type_get_object (&field->klass->byval_arg);
	info->type = mono_type_get_object (field->field->type);
	info->name = mono_string_new (field->field->name);
	info->attrs = field->field->type->attrs;
}

static void
ves_icall_get_property_info (MonoReflectionProperty *property, MonoPropertyInfo *info)
{
	info->parent = mono_type_get_object (&property->klass->byval_arg);
	info->name = mono_string_new (property->property->name);
	info->attrs = property->property->attrs;
	info->get = property->property->get ? mono_method_get_object (property->property->get): NULL;
	info->set = property->property->set ? mono_method_get_object (property->property->set): NULL;
	/* 
	 * There may be other methods defined for properties, though, it seems they are not exposed 
	 * in the reflection API 
	 */
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
	info->rank = class->rank;
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

static void
ves_icall_get_enum_info (MonoReflectionType *type, MonoEnumInfo *info)
{
	MonoClass *enumc = mono_class_from_mono_type (type->type);
	guint i, j, nvalues, crow;
	MonoClassField *field;
	
	info->utype = mono_type_get_object (enumc->enum_basetype);
	nvalues = enumc->field.count - 1;
	info->names = mono_array_new (mono_defaults.string_class, nvalues);
	info->values = mono_array_new (mono_class_from_mono_type (enumc->enum_basetype), nvalues);
	
	for (i = 0, j = 0; i < enumc->field.count; ++i) {
		field = &enumc->fields [i];
		if (strcmp ("value__", field->name) == 0)
			continue;
		mono_array_set (info->names, gpointer, j, mono_string_new (field->name));
		if (!field->data) {
			crow = mono_metadata_get_constant_index (enumc->image, MONO_TOKEN_FIELD_DEF | (i+enumc->field.first+1));
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

static MonoMethod*
search_method (MonoReflectionType *type, char *name, guint32 flags, MonoArray *args)
{
	MonoClass *klass;
	MonoMethod *m;
	MonoReflectionType *paramt;
	int i, j;

	klass = mono_class_from_mono_type (type->type);
	while (klass) {
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
		klass = klass->parent;
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

static MonoProperty*
search_property (MonoClass *klass, char* name, MonoArray *args) {
	int i;
	MonoProperty *p;

	/* FIXME: handle args */
	for (i = 0; i < klass->property.count; ++i) {
		p = &klass->properties [i];
		if (strcmp (p->name, name) == 0)
			return p;
	}
	return NULL;
}

static MonoReflectionProperty*
ves_icall_get_property (MonoReflectionType *type, MonoString *name, MonoArray *args)
{
	MonoProperty *p;
	MonoClass *class = mono_class_from_mono_type (type->type);
	char *n = mono_string_to_utf8 (name);

	p = search_property (class, n, args);
	g_free (n);
	if (p)
		return mono_property_get_object (class, p);
	return NULL;
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


/*
 * Note: the filter is applied from within corlib.
 */
static MonoArray*
ves_icall_type_find_members (MonoReflectionType *type, guint32 membertypes, guint32 bflags)
{
	GSList *l = NULL, *tmp;
	MonoClass *startklass, *klass;
	MonoArray *res;
	MonoMethod *method;
	MonoObject *member;
	int i, is_ctor, len, match;

	klass = startklass = mono_class_from_mono_type (type->type);

	/* FIXME: check the bindingflags */
handle_parent:	
	if (membertypes & (1|8)) { /* constructors and methods */
		for (i = 0; i < klass->method.count; ++i) {
			match = 0;
			method = klass->methods [i];
			if ((method->flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) == METHOD_ATTRIBUTE_PUBLIC) {
				if (bflags & BFLAGS_Public)
					match++;
			} else {
				if (bflags & BFLAGS_NonPublic)
					match++;
			}
			if (method->flags & METHOD_ATTRIBUTE_STATIC) {
				if (bflags & BFLAGS_Static)
					match++;
			} else {
				if (bflags & BFLAGS_Instance)
					match++;
			}

			if (!match)
				continue;
			member = (MonoObject*)mono_method_get_object (method);
			
			is_ctor = strcmp (method->name, ".ctor") == 0 ||
					strcmp (method->name, ".cctor") == 0;
			if (is_ctor && (membertypes & 1)) {
				l = g_slist_prepend (l, member);
				continue;
			}
			if (!is_ctor && (membertypes & 8)) {
				l = g_slist_prepend (l, member);
			}
		}
	}
	if (membertypes & 4) { /* fields */
		MonoClassField *field;
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
			if (field->type->attrs & FIELD_ATTRIBUTE_STATIC) {
				if (bflags & BFLAGS_Static)
					match++;
			} else {
				if (bflags & BFLAGS_Instance)
					match++;
			}

			if (!match)
				continue;
			member = (MonoObject*)mono_field_get_object (klass, field);
			l = g_slist_prepend (l, member);
		}
	}
	if (membertypes & 16) { /* properties */
		MonoProperty *prop;
		for (i = 0; i < klass->property.count; ++i) {
			match = 0;
			prop = &klass->properties [i];
			/* FIXME: handle bflags */
			l = g_slist_prepend (l, mono_property_get_object (klass, prop));
		}
	}
	if (!(bflags & BFLAGS_DeclaredOnly) && (klass = klass->parent))
		goto handle_parent;
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
	char *byref, *isarray, *ispointer;
	guint rank;
	MonoClass *klass;

	str = namespace = mono_string_to_utf8 (type);
	/*g_print ("requested type %s\n", str);*/

	name = strrchr (str, '.');
	byref = strrchr (str, '&');
	ispointer = strrchr (str, '*');
	if (byref)
		*byref = 0;
	if (ispointer)
		*ispointer = 0;
	isarray = strrchr (str, '[');
	if (isarray) {
		rank = 1;
		*isarray = 0;
		while (*isarray) {
			if (*isarray == ',')
				rank++;
			if (*isarray == ']')
				break;
			++isarray;
		}
	}

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

	if (isarray) {
		klass = mono_array_class_get (klass, rank);
		mono_class_init (klass);
		/*g_print ("got array class %s [%d] (0x%x)\n", klass->element_class->name, klass->rank, klass->this_arg.type);*/
	}

	if (byref || ispointer)
		return mono_type_get_object (&klass->this_arg);
	else
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

/*
 * Magic number to convert a time which is relative to
 * Jan 1, 1970 into a value which is relative to Jan 1, 0001.
 */
#define	EPOCH_ADJUST	((gint64)62135596800L)

static gint64
ves_icall_System_DateTime_GetNow ()
{
	struct timeval tv;
	gint64 res;

	if (gettimeofday (&tv, NULL) == 0) {
		res = (((gint64)tv.tv_sec + EPOCH_ADJUST)* 1000000 + tv.tv_usec)*10;
		return res;
	}

	/* fixme: raise exception */
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
	"System.Reflection.MonoMethodInfo::get_parameter_info", ves_icall_get_parameter_info,
	"System.Reflection.MonoFieldInfo::get_field_info", ves_icall_get_field_info,
	"System.Reflection.MonoPropertyInfo::get_property_info", ves_icall_get_property_info,
	"System.MonoEnumInfo::get_enum_info", ves_icall_get_enum_info,
	
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
	"System.Type::get_property", ves_icall_get_property,
	"System.Type::get_method", ves_icall_get_method,
	"System.MonoType::get_attributes", ves_icall_get_attributes,
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
	"System.Threading.Mutex::CreateMutex_internal", ves_icall_System_Threading_Mutex_CreateMutex_internal,
	"System.Threading.Mutex::ReleaseMutex_internal", ves_icall_System_Threading_Mutex_ReleaseMutex_internal,
	"System.Threading.NativeEventCalls::CreateEvent_internal", ves_icall_System_Threading_Events_CreateEvent_internal,
	"System.Threading.NativeEventCalls::SetEvent_internal",    ves_icall_System_Threading_Events_SetEvent_internal,
	"System.Threading.NativeEventCalls::ResetEvent_internal",  ves_icall_System_Threading_Events_ResetEvent_internal,

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

	"System.Text.Encoding::IConvNewEncoder", ves_icall_iconv_new_encoder,
	"System.Text.Encoding::IConvNewDecoder", ves_icall_iconv_new_decoder,
	"System.Text.Encoding::IConvReset", ves_icall_iconv_reset,
	"System.Text.Encoding::IConvGetByteCount", ves_icall_iconv_get_byte_count,
	"System.Text.Encoding::IConvGetBytes", ves_icall_iconv_get_bytes,
	"System.Text.Encoding::IConvGetCharCount", ves_icall_iconv_get_char_count,
	"System.Text.Encoding::IConvGetChars", ves_icall_iconv_get_chars,

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


