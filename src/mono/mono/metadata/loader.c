/*
 * loader.c: Image Loader 
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
 *   Miguel de Icaza (miguel@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 *
 * This file is used by the interpreter and the JIT engine to locate
 * assemblies.  Used to load AssemblyRef and later to resolve various
 * kinds of `Refs'.
 *
 * TODO:
 *   This should keep track of the assembly versions that we are loading.
 *
 */
#include <config.h>
#include <glib.h>
#include <gmodule.h>
#include <stdio.h>
#include <string.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/image.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/cil-coff.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/class.h>

static gboolean dummy_icall = TRUE;

MonoDefaults mono_defaults;

#ifdef __CYGWIN__
#define mono_map_dll(name) (name)
#else
static char *dll_map[] = {
	"libc", "libc.so.6",
	"libm", "libm.so.6",
	"cygwin1.dll", "libc.so.6", 
	NULL, NULL
};

static const char *
mono_map_dll (const char *name)
{
	int i = 0;

	while (dll_map [i]) {
		if (!strcmp (dll_map [i], name))
			return  dll_map [i + 1];
		i += 2;
	}

	return name;
}
#endif

void
mono_init (void)
{
	static gboolean initialized = FALSE;
	MonoAssembly *ass;
	enum MonoImageOpenStatus status = MONO_IMAGE_OK;

	if (initialized)
		return;

	/* find the corlib */
	ass = mono_assembly_open (CORLIB_NAME, NULL, &status);
	g_assert (status == MONO_IMAGE_OK);
	g_assert (ass != NULL);
	mono_defaults.corlib = ass->image;

	mono_defaults.object_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Object");
	g_assert (mono_defaults.object_class != 0);

	mono_defaults.void_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Void");
	g_assert (mono_defaults.void_class != 0);

	mono_defaults.boolean_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Boolean");
	g_assert (mono_defaults.boolean_class != 0);

	mono_defaults.byte_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Byte");
	g_assert (mono_defaults.byte_class != 0);

	mono_defaults.sbyte_class = mono_class_from_name (
                mono_defaults.corlib, "System", "SByte");
	g_assert (mono_defaults.sbyte_class != 0);

	mono_defaults.int16_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Int16");
	g_assert (mono_defaults.int16_class != 0);

	mono_defaults.uint16_class = mono_class_from_name (
                mono_defaults.corlib, "System", "UInt16");
	g_assert (mono_defaults.uint16_class != 0);

	mono_defaults.int32_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Int32");
	g_assert (mono_defaults.int32_class != 0);

	mono_defaults.uint32_class = mono_class_from_name (
                mono_defaults.corlib, "System", "UInt32");
	g_assert (mono_defaults.uint32_class != 0);

	mono_defaults.uint_class = mono_class_from_name (
                mono_defaults.corlib, "System", "UIntPtr");
	g_assert (mono_defaults.uint_class != 0);

	mono_defaults.int_class = mono_class_from_name (
                mono_defaults.corlib, "System", "IntPtr");
	g_assert (mono_defaults.int_class != 0);

	mono_defaults.int64_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Int64");
	g_assert (mono_defaults.int64_class != 0);

	mono_defaults.uint64_class = mono_class_from_name (
                mono_defaults.corlib, "System", "UInt64");
	g_assert (mono_defaults.uint64_class != 0);

	mono_defaults.single_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Single");
	g_assert (mono_defaults.single_class != 0);

	mono_defaults.double_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Double");
	g_assert (mono_defaults.double_class != 0);

	mono_defaults.char_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Char");
	g_assert (mono_defaults.char_class != 0);

	mono_defaults.string_class = mono_class_from_name (
                mono_defaults.corlib, "System", "String");
	g_assert (mono_defaults.string_class != 0);

	mono_defaults.enum_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Enum");
	g_assert (mono_defaults.enum_class != 0);

	mono_defaults.array_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Array");
	g_assert (mono_defaults.array_class != 0);

	mono_defaults.delegate_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Delegate");
	g_assert (mono_defaults.delegate_class != 0);

	mono_defaults.typehandle_class = mono_class_from_name (
                mono_defaults.corlib, "System", "RuntimeTypeHandle");
	g_assert (mono_defaults.typehandle_class != 0);

	mono_defaults.methodhandle_class = mono_class_from_name (
                mono_defaults.corlib, "System", "RuntimeMethodHandle");
	g_assert (mono_defaults.methodhandle_class != 0);

	mono_defaults.fieldhandle_class = mono_class_from_name (
                mono_defaults.corlib, "System", "RuntimeFieldHandle");
	g_assert (mono_defaults.fieldhandle_class != 0);

	mono_defaults.monotype_class = mono_class_from_name (
                mono_defaults.corlib, "System", "MonoType");
	g_assert (mono_defaults.monotype_class != 0);
}

static GHashTable *icall_hash = NULL;

void
mono_add_internal_call (const char *name, gpointer method)
{
	if (!icall_hash) {
		dummy_icall = FALSE;
		icall_hash = g_hash_table_new (g_str_hash , g_str_equal);
	}

	g_hash_table_insert (icall_hash, g_strdup (name), method);
}

static void
ves_icall_dummy ()
{
	g_warning ("the mono runtime is not initialized");
	g_assert_not_reached ();
}

gpointer
mono_lookup_internal_call (const char *name)
{
	gpointer res;

	if (dummy_icall)
		return ves_icall_dummy;

	if (!icall_hash) {
		g_warning ("icall_hash not initialized");
		g_assert_not_reached ();
	}

	if (!(res = g_hash_table_lookup (icall_hash, name))) {
		g_warning ("cant resolve internal call to \"%s\"", name);
		return NULL;
	}

	return res;
}

MonoClassField*
mono_field_from_memberref (MonoImage *image, guint32 token, MonoClass **retklass)
{
	MonoImage *mimage;
	MonoClass *klass;
	MonoTableInfo *tables = image->tables;
	guint32 cols[6];
	guint32 nindex, class, i;
	const char *fname, *name, *nspace;
	const char *ptr;
	guint32 index = mono_metadata_token_index (token);

	mono_metadata_decode_row (&tables [MONO_TABLE_MEMBERREF], index-1, cols, MONO_MEMBERREF_SIZE);
	nindex = cols [MONO_MEMBERREF_CLASS] >> MEMBERREF_PARENT_BITS;
	class = cols [MONO_MEMBERREF_CLASS] & MEMBERREF_PARENT_MASK;

	fname = mono_metadata_string_heap (image, cols [MONO_MEMBERREF_NAME]);
	
	ptr = mono_metadata_blob_heap (image, cols [MONO_MEMBERREF_SIGNATURE]);
	mono_metadata_decode_blob_size (ptr, &ptr);
	/* we may want to check the signature here... */

	switch (class) {
	case MEMBERREF_PARENT_TYPEREF: {
		guint32 scopeindex, scopetable;

		mono_metadata_decode_row (&tables [MONO_TABLE_TYPEREF], nindex-1, cols, MONO_TYPEREF_SIZE);
		scopeindex = cols [MONO_TYPEREF_SCOPE] >> RESOLTION_SCOPE_BITS;
		scopetable = cols [MONO_TYPEREF_SCOPE] & RESOLTION_SCOPE_MASK;
		/*g_print ("typeref: 0x%x 0x%x %s.%s\n", scopetable, scopeindex,
			mono_metadata_string_heap (m, cols [MONO_TYPEREF_NAMESPACE]),
			mono_metadata_string_heap (m, cols [MONO_TYPEREF_NAME]));*/
		switch (scopetable) {
		case RESOLTION_SCOPE_ASSEMBLYREF:
			/*
			 * To find the field we have the following info:
			 * *) name and namespace of the class from the TYPEREF table
			 * *) name and signature of the field from the MEMBERREF table
			 */
			nspace = mono_metadata_string_heap (image, cols [MONO_TYPEREF_NAMESPACE]);
			name = mono_metadata_string_heap (image, cols [MONO_TYPEREF_NAME]);

			/* this will triggered by references to mscorlib */
			if (image->references [scopeindex-1] == NULL)
				g_error ("Reference to mscorlib? Probably need to implement %s.%s::%s in corlib", nspace, name, fname);

			mimage = image->references [scopeindex-1]->image;

			klass = mono_class_from_name (mimage, nspace, name);
			mono_class_metadata_init (klass);

			/* mostly dumb search for now */
			for (i = 0; i < klass->field.count; ++i) {
				MonoClassField *f = &klass->fields [i];
				if (!strcmp (fname, f->name)) {
					if (retklass)
						*retklass = klass;
					return f;
				}
			}
			g_warning ("Missing field %s.%s::%s", nspace, name, fname);
			return NULL;
		default:
			return NULL;
		}
		break;
	}
	default:
		return NULL;
	}
}

static MonoMethod *
method_from_memberref (MonoImage *image, guint32 index)
{
	MonoImage *mimage;
	MonoClass *klass;
	MonoTableInfo *tables = image->tables;
	guint32 cols[6];
	guint32 nindex, class, i;
	const char *mname, *name, *nspace;
	MonoMethodSignature *sig;
	const char *ptr;

	mono_metadata_decode_row (&tables [MONO_TABLE_MEMBERREF], index-1, cols, 3);
	nindex = cols [MONO_MEMBERREF_CLASS] >> MEMBERREF_PARENT_BITS;
	class = cols [MONO_MEMBERREF_CLASS] & MEMBERREF_PARENT_MASK;
	/*g_print ("methodref: 0x%x 0x%x %s\n", class, nindex,
		mono_metadata_string_heap (m, cols [MONO_MEMBERREF_NAME]));*/

	mname = mono_metadata_string_heap (image, cols [MONO_MEMBERREF_NAME]);
	
	ptr = mono_metadata_blob_heap (image, cols [MONO_MEMBERREF_SIGNATURE]);
	mono_metadata_decode_blob_size (ptr, &ptr);
	sig = mono_metadata_parse_method_signature (image, 0, ptr, NULL);

	switch (class) {
	case MEMBERREF_PARENT_TYPEREF: {
		guint32 scopeindex, scopetable;

		mono_metadata_decode_row (&tables [MONO_TABLE_TYPEREF], nindex-1, cols, MONO_TYPEREF_SIZE);
		scopeindex = cols [MONO_TYPEREF_SCOPE] >> RESOLTION_SCOPE_BITS;
		scopetable = cols [MONO_TYPEREF_SCOPE] & RESOLTION_SCOPE_MASK;
		/*g_print ("typeref: 0x%x 0x%x %s.%s\n", scopetable, scopeindex,
			mono_metadata_string_heap (m, cols [MONO_TYPEREF_NAMESPACE]),
			mono_metadata_string_heap (m, cols [MONO_TYPEREF_NAME]));*/
		switch (scopetable) {
		case RESOLTION_SCOPE_ASSEMBLYREF:
			/*
			 * To find the method we have the following info:
			 * *) name and namespace of the class from the TYPEREF table
			 * *) name and signature of the method from the MEMBERREF table
			 */
			nspace = mono_metadata_string_heap (image, cols [MONO_TYPEREF_NAMESPACE]);
			name = mono_metadata_string_heap (image, cols [MONO_TYPEREF_NAME]);

			/* this will triggered by references to mscorlib */
			if (image->references [scopeindex-1] == NULL)
				g_error ("Reference to mscorlib? Probably need to implement %s.%s::%s in corlib", nspace, name, mname);

			mimage = image->references [scopeindex-1]->image;

			klass = mono_class_from_name (mimage, nspace, name);
			mono_class_metadata_init (klass);

			/* 
			 * FIXME: this is a workaround for the different signatures
			 * in delegates constructors you get in user code (native int)
			 * and in mscorlib (native unsigned int)
			 */
			if (klass->parent && klass->parent->parent == mono_defaults.delegate_class) {
				for (i = 0; i < klass->method.count; ++i) {
					MonoMethod *m = klass->methods [i];
					if (!strcmp (mname, m->name)) {
						if (!strcmp (mname, ".ctor")) {
							/* we assume signature is correct */
							mono_metadata_free_method_signature (sig);
							return m;
						}
						if (mono_metadata_signature_equal (sig, m->signature)) {
							mono_metadata_free_method_signature (sig);
							return m;
						}
					}
				}
			}
			/* mostly dumb search for now */
			for (i = 0; i < klass->method.count; ++i) {
				MonoMethod *m = klass->methods [i];
				if (!strcmp (mname, m->name)) {
					if (mono_metadata_signature_equal (sig, m->signature)) {
						mono_metadata_free_method_signature (sig);
						return m;
					}
				}
			}
			g_warning ("Missing method %s.%s::%s", nspace, name, mname);
			mono_metadata_free_method_signature (sig);
			return NULL;
		default:
			mono_metadata_free_method_signature (sig);
			return NULL;
		}
		break;
	}
	case MEMBERREF_PARENT_TYPESPEC: {
		guint32 bcols [MONO_TYPESPEC_SIZE];
		guint32 len;
		MonoType *type;
		MonoMethod *result;

		mono_metadata_decode_row (&tables [MONO_TABLE_TYPESPEC], nindex - 1, 
					  bcols, MONO_TYPESPEC_SIZE);
		ptr = mono_metadata_blob_heap (image, bcols [MONO_TYPESPEC_SIGNATURE]);
		len = mono_metadata_decode_value (ptr, &ptr);	
		type = mono_metadata_parse_type (image, MONO_PARSE_TYPE, 0, ptr, &ptr);

		if (type->type != MONO_TYPE_ARRAY)
			g_assert_not_reached ();		

		result = (MonoMethod *)g_new0 (MonoMethodPInvoke, 1);
		result->klass = mono_class_get (image, MONO_TOKEN_TYPE_SPEC | nindex);
		result->iflags = METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL;
		result->signature = sig;
		result->name = mname;

		if (!strcmp (mname, ".ctor")) {
			/* we special-case this in the runtime. */
			result->addr = NULL;
			return result;
		}
		
		if (!strcmp (mname, "Set")) {
			g_assert (sig->hasthis);
			g_assert (type->data.array->rank + 1 == sig->param_count);

			result->addr = mono_lookup_internal_call ("__array_Set");
			return result;
		}

		if (!strcmp (mname, "Get")) {
			g_assert (sig->hasthis);
			g_assert (type->data.array->rank == sig->param_count);

			result->addr = mono_lookup_internal_call ("__array_Get");
			return result;
		}

		g_assert_not_reached ();
		break;
	}
	default:
		g_assert_not_reached ();
	}

	return NULL;
}

static void
fill_pinvoke_info (MonoImage *image, MonoMethodPInvoke *piinfo, int index)
{
	MonoMethod *mh = &piinfo->method;
	MonoTableInfo *tables = image->tables;
	MonoTableInfo *im = &tables [MONO_TABLE_IMPLMAP];
	MonoTableInfo *mr = &tables [MONO_TABLE_MODULEREF];
	guint32 im_cols [4];
	guint32 mr_cols [1];
	const char *import = NULL;
	const char *scope = NULL;
	char *full_name;
	GModule *gmodule;
	int i;

	for (i = 0; i < im->rows; i++) {
			
		mono_metadata_decode_row (im, i, im_cols, 4);

		if ((im_cols[1] >> 1) == index + 1) {

			import = mono_metadata_string_heap (image, im_cols [2]);

			mono_metadata_decode_row (mr, im_cols [3] - 1, mr_cols,
						  1);
			
			scope = mono_metadata_string_heap (image, mr_cols [0]);
		}
	}

	piinfo->piflags = im_cols [0];

	g_assert (import && scope);

	scope = mono_map_dll (scope);
	full_name = g_module_build_path (NULL, scope);
	gmodule = g_module_open (full_name, G_MODULE_BIND_LAZY);

	mh->addr = NULL;
	if (!gmodule) {
		if (!(gmodule=g_module_open (scope, G_MODULE_BIND_LAZY))) {
			g_warning ("Failed to load library %s (%s)", full_name, scope);
			g_free (full_name);
			return;
		}
	}
	g_free (full_name);

	g_module_symbol (gmodule, import, &mh->addr); 

	if (!mh->addr) {
		g_warning ("Failed to load function %s from %s", import, scope);
		return;
	}

	mh->flags |= METHOD_ATTRIBUTE_PINVOKE_IMPL;
}

MonoMethod *
mono_get_method (MonoImage *image, guint32 token, MonoClass *klass)
{
	MonoMethod *result;
	int table = mono_metadata_token_table (token);
	int index = mono_metadata_token_index (token);
	MonoTableInfo *tables = image->tables;
	const char *loc, *sig = NULL;
	char *name;
	int size;
	guint32 cols [MONO_TYPEDEF_SIZE];

	if ((result = g_hash_table_lookup (image->method_cache, GINT_TO_POINTER (token))))
			return result;

	if (table != MONO_TABLE_METHOD) {
		g_assert (table == MONO_TABLE_MEMBERREF);
		result = method_from_memberref (image, index);
		g_hash_table_insert (image->method_cache, GINT_TO_POINTER (token), result);
		return result;
	}

	mono_metadata_decode_row (&tables [table], index - 1, cols, 6);

	if ((cols [2] & METHOD_ATTRIBUTE_PINVOKE_IMPL) ||
	    (cols [1] & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL))
		result = (MonoMethod *)g_new0 (MonoMethodPInvoke, 1);
	else 
		result = (MonoMethod *)g_new0 (MonoMethodNormal, 1);
	
	result->slot = -1;
	result->klass = klass;
	result->flags = cols [2];
	result->iflags = cols [1];
	result->name = mono_metadata_string_heap (image, cols [3]);

	if (!sig) /* already taken from the methodref */
		sig = mono_metadata_blob_heap (image, cols [4]);
	size = mono_metadata_decode_blob_size (sig, &sig);
	result->signature = mono_metadata_parse_method_signature (image, 0, sig, NULL);

	if (!result->klass) {
		guint32 type = mono_metadata_typedef_from_method (image, token);
		result->klass = mono_class_get (image, MONO_TOKEN_TYPE_DEF | type);
	}

	if (cols [1] & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) {
		name = g_strconcat (result->klass->name_space, ".", result->klass->name, "::", 
				    mono_metadata_string_heap (image, cols [MONO_METHOD_NAME]), NULL);
		result->addr = mono_lookup_internal_call (name);
		g_free (name);
		result->flags |= METHOD_ATTRIBUTE_PINVOKE_IMPL;
	} else if (cols [2] & METHOD_ATTRIBUTE_PINVOKE_IMPL) {
		fill_pinvoke_info (image, (MonoMethodPInvoke *)result, index - 1);
	} else {
		/* if this is a methodref from another module/assembly, this fails */
		loc = mono_cli_rva_map ((MonoCLIImageInfo *)image->image_info, cols [0]);

		if (!result->klass->dummy && !(result->flags & METHOD_ATTRIBUTE_ABSTRACT) &&
					!(result->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME)) {
			g_assert (loc);
			((MonoMethodNormal *)result)->header = mono_metadata_parse_mh (image, loc);
		}
	}

	g_hash_table_insert (image->method_cache, GINT_TO_POINTER (token), result);

	return result;
}

void
mono_free_method  (MonoMethod *method)
{
	mono_metadata_free_method_signature (method->signature);
	if (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) {
		MonoMethodPInvoke *piinfo = (MonoMethodPInvoke *)method;
		g_free (piinfo->code);
	} else if (!(method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL)) {
		mono_metadata_free_mh (((MonoMethodNormal *)method)->header);
	}

	g_free (method);
}

void
mono_method_get_param_names (MonoMethod *method, const char **names)
{
	int i, lastp;
	MonoClass *klass = method->klass;
	MonoTableInfo *methodt = &klass->image->tables [MONO_TABLE_METHOD];
	MonoTableInfo *paramt = &klass->image->tables [MONO_TABLE_PARAM];

	if (!method->signature->param_count)
		return;
	for (i = 0; i < method->signature->param_count; ++i)
		names [i] = "";

	mono_class_metadata_init (klass);
	if (!klass->methods)
		return;

	for (i = 0; i < klass->method.count; ++i) {
		if (method == klass->methods [i]) {
			guint32 index = klass->method.first + i;
			guint32 cols [MONO_PARAM_SIZE];
			guint param_index = mono_metadata_decode_row_col (methodt, index, MONO_METHOD_PARAMLIST);

			if (index < methodt->rows)
				lastp = mono_metadata_decode_row_col (methodt, index + 1, MONO_METHOD_PARAMLIST);
			else
				lastp = paramt->rows;
			for (i = param_index; i < lastp; ++i) {
				mono_metadata_decode_row (paramt, i -1, cols, MONO_PARAM_SIZE);
				if (cols [MONO_PARAM_SEQUENCE]) /* skip return param spec */
					names [cols [MONO_PARAM_SEQUENCE] - 1] = mono_metadata_string_heap (klass->image, cols [MONO_PARAM_NAME]);
			}
			return;
		}
	}
}

