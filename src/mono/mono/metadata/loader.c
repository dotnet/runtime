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

MonoDefaults mono_defaults;

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

void
mono_init ()
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

	mono_defaults.array_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Array");
	g_assert (mono_defaults.array_class != 0);

	mono_defaults.char_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Char");
	g_assert (mono_defaults.char_class != 0);

	mono_defaults.string_class = mono_class_from_name (
                mono_defaults.corlib, "System", "String");
	g_assert (mono_defaults.string_class != 0);

}

static GHashTable *icall_hash = NULL;

void
mono_add_internal_call (const char *name, gpointer method)
{
	if (!icall_hash)
		icall_hash = g_hash_table_new (g_str_hash , g_str_equal);
	
	g_hash_table_insert (icall_hash, g_strdup (name), method);
}

gpointer
mono_lookup_internal_call (const char *name)
{
	gpointer res;

	if (!icall_hash) {
		g_warning ("icall_hash not initialized");
		g_assert_not_reached ();
	}

	if (!(res = g_hash_table_lookup (icall_hash, name))) {
		g_warning ("cant resolve internal call to \"%s\"", name);
		g_assert_not_reached ();
	}

	return res;
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
			g_assert (image->references [scopeindex-1] != NULL);

			mimage = image->references [scopeindex-1]->image;

			klass = mono_class_from_name (mimage, nspace, name);
			mono_class_metadata_init (klass);

			/* mostly dumb search for now */
			for (i = 0; i < klass->method.count; ++i) {
				MonoMethod *m = klass->methods [i];
				if (!strcmp (mname, m->name)) {
					if (mono_metadata_signature_equal (image, sig, mimage, m->signature)) {
						mono_metadata_free_method_signature (sig);
						return m;
					}
				}
			}
			g_warning ("can't find method %s.%s::%s", nspace, name, mname);
			g_assert_not_reached ();
			break;
		default:
			g_assert_not_reached ();
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
		type = mono_metadata_parse_type (image, ptr, &ptr);

		if (type->type != MONO_TYPE_ARRAY)
			g_assert_not_reached ();		

		result = (MonoMethod *)g_new0 (MonoMethod, 1);
		result->klass = mono_defaults.array_class;
		result->iflags = METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL;
		result->signature = sig;
		
		if (!strcmp (mname, ".ctor")) { 
			g_assert (sig->hasthis);
			if (type->data.array->rank == sig->param_count) {
				result->addr = mono_lookup_internal_call ("__array_ctor");
				return result;
			} else if ((type->data.array->rank * 2) == sig->param_count) {
				result->addr = mono_lookup_internal_call ("__array_bound_ctor");
				return result;			
			} else 
				g_assert_not_reached ();
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

static ffi_type *
ves_map_ffi_type (MonoType *type)
{
	ffi_type *rettype;

	switch (type->type) {
	case MONO_TYPE_I1:
		rettype = &ffi_type_sint8;
		break;
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_U1:
		rettype = &ffi_type_uint8;
		break;
	case MONO_TYPE_I2:
		rettype = &ffi_type_sint16;
		break;
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
		rettype = &ffi_type_uint16;
		break;
	case MONO_TYPE_I4:
		rettype = &ffi_type_sint32;
		break;
	case MONO_TYPE_U4:
		rettype = &ffi_type_sint32;
		break;
	case MONO_TYPE_R4:
		rettype = &ffi_type_float;
		break;
	case MONO_TYPE_R8:
		rettype = &ffi_type_double;
		break;
	case MONO_TYPE_STRING:
		rettype = &ffi_type_pointer;
		break;
	case MONO_TYPE_VOID:
		rettype = &ffi_type_void;
		break;
	default:
		g_warning ("not implemented");
		g_assert_not_reached ();
	}

	return rettype;
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
	ffi_type **args, *rettype;
	int i, acount;

	for (i = 0; i < im->rows; i++) {
			
		mono_metadata_decode_row (im, i, im_cols, 4);

		if ((im_cols[1] >> 1) == index + 1) {

			import = mono_metadata_string_heap (image, im_cols [2]);

			mono_metadata_decode_row (mr, im_cols [3] - 1, mr_cols,
						  1);
			
			scope = mono_metadata_string_heap (image, mr_cols [0]);
		}
	}

	g_assert (import && scope);

	scope = mono_map_dll (scope);
	full_name = g_module_build_path (NULL, scope);
	gmodule = g_module_open (full_name, G_MODULE_BIND_LAZY);
	g_free (full_name);

	g_assert (gmodule);

	piinfo->cif = g_new (ffi_cif , 1);
	piinfo->piflags = im_cols [0];

	g_module_symbol (gmodule, import, &mh->addr); 

	g_assert (mh->addr);

	acount = mh->signature->param_count;

	args = g_new (ffi_type *, acount);

	for (i = 0; i < acount; i++)
		args[i] = ves_map_ffi_type (mh->signature->params [i]->type);

	rettype = ves_map_ffi_type (mh->signature->ret->type);
	
	if (!ffi_prep_cif (piinfo->cif, FFI_DEFAULT_ABI, acount, rettype, 
			   args) == FFI_OK) {
		g_warning ("prepare pinvoke failed");
 		g_assert_not_reached ();
	}
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

	if (table == MONO_TABLE_METHOD && (result = g_hash_table_lookup (image->method_cache, GINT_TO_POINTER (token))))
			return result;

	if (table != MONO_TABLE_METHOD) {
		g_assert (table == MONO_TABLE_MEMBERREF);
		return method_from_memberref (image, index);
	}

	mono_metadata_decode_row (&tables [table], index - 1, cols, 6);

	if (cols [1] & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) {
		MonoTableInfo *t = &image->tables [MONO_TABLE_TYPEDEF];
		MonoAssembly *corlib;
		guint32 tdef;
		guint32 tdcols [MONO_TYPEDEF_SIZE];

		tdef = mono_metadata_typedef_from_method (image, index - 1) - 1;

		mono_metadata_decode_row (t, tdef, tdcols, MONO_TYPEDEF_SIZE);

		name = g_strconcat (mono_metadata_string_heap (image, tdcols [MONO_TYPEDEF_NAMESPACE]), ".",
				    mono_metadata_string_heap (image, tdcols [MONO_TYPEDEF_NAME]), "::", 
				    mono_metadata_string_heap (image, cols [MONO_METHOD_NAME]), NULL);

		corlib = mono_assembly_open (CORLIB_NAME, NULL, NULL);

		/* all internal calls must be inside corlib */
		g_assert (corlib->image == image);

		result = (MonoMethod *)g_new0 (MonoMethod, 1);

		result->addr = mono_lookup_internal_call (name);

		g_free (name);

		g_assert (result->addr != NULL);

	} else if (cols [2] & METHOD_ATTRIBUTE_PINVOKE_IMPL) {

		result = (MonoMethod *)g_new0 (MonoMethodPInvoke, 1);
	} else {

		result = (MonoMethod *)g_new0 (MonoMethodNormal, 1);
	}

	result->klass = klass;
	result->flags = cols [2];
	result->iflags = cols [1];
	result->name = mono_metadata_string_heap (image, cols [3]);

	if (!sig) /* already taken from the methodref */
		sig = mono_metadata_blob_heap (image, cols [4]);
	size = mono_metadata_decode_blob_size (sig, &sig);
	result->signature = mono_metadata_parse_method_signature (image, 0, sig, NULL);

	if (result->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) {
		fill_pinvoke_info (image, (MonoMethodPInvoke *)result, 
				   index - 1);
	} else if (!(result->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL)) {
		/* if this is a methodref from another module/assembly, this fails */
		loc = mono_cli_rva_map ((MonoCLIImageInfo *)image->image_info, cols [0]);

		if (!result->klass) {
			guint32 type = mono_metadata_typedef_from_method (image, token);
			result->klass = mono_class_get (image, MONO_TOKEN_TYPE_DEF | type);
		}

		g_assert (loc);

		((MonoMethodNormal *)result)->header = mono_metadata_parse_mh (image, loc);
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
		g_free (piinfo->cif->arg_types);
		g_free (piinfo->cif);
	} else if (!(method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL)) {
		mono_metadata_free_mh (((MonoMethodNormal *)method)->header);
	}

	g_free (method);
}

