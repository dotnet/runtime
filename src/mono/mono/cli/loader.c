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
#include "cli.h"


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

/**
 * mono_get_string_class_info:
 * @ttoken: pointer to location to store type definition token
 * @cl: pointer where image will be stored
 *
 * This routine locates information about the System.String class. A reference
 * to the image containing the class is returned in @cl. The type definition 
 * token is returned in @ttoken. 
 *
 * Returns: the method definition token for System.String::.ctor (char *)
 */

guint32
mono_get_string_class_info (guint *ttoken, MonoImage **cl)
{
	static guint32 ctor = 0, tt = 0;
	enum MonoImageOpenStatus status = MONO_IMAGE_OK; 
	MonoAssembly *ass;
	static MonoImage *corlib;
	MonoMetadata *m;
	MonoTableInfo *t;
	guint32 cols [MAX (MONO_TYPEDEF_SIZE, MONO_METHOD_SIZE)];
	guint32 ncols [MONO_TYPEDEF_SIZE];
	guint32 i, first = 0, last = 0;
	const char *name, *nspace;

	if (ctor) {
		*ttoken = tt;
		*cl = corlib;
		return ctor;
	}

	ass = mono_assembly_open (CORLIB_NAME, NULL, &status);
	g_assert (status == MONO_IMAGE_OK);
	g_assert (ass != NULL);
	
	*cl = corlib = ass->image;
	g_assert (corlib != NULL);
       
	m = &corlib->metadata;
	t = &m->tables [MONO_TABLE_TYPEDEF];

	for (i = 0; i < t->rows; i++) {
		mono_metadata_decode_row (t, i, cols, MONO_TYPEDEF_SIZE);
		name = mono_metadata_string_heap (m, cols[1]);
		nspace = mono_metadata_string_heap (m, cols[2]);

		if (((cols [0] & TYPE_ATTRIBUTE_CLASS_SEMANTIC_MASK) == TYPE_ATTRIBUTE_CLASS) &&
		    !strcmp (nspace, "System") && !strcmp (name, "String")) {

			*ttoken = tt = MONO_TOKEN_TYPE_DEF | (i + 1);

			first = cols [5] - 1;

			if (i + 1 < t->rows) {
				mono_metadata_decode_row (t, i + 1, ncols, 
							  MONO_TYPEDEF_SIZE);
				last =  ncols [5] - 1;
			} else
				last = m->tables [MONO_TABLE_METHOD].rows;
			break;
		}
	}

	g_assert (last - first > 0);

	t = &m->tables [MONO_TABLE_METHOD];
	g_assert (last < t->rows);

	for (i = first; i < last; i++) {
		const char *ptr;
		int len;
		guint8 sig[] = { 0x20, 0x01, 0x01, 0x0f, 0x03 };
		mono_metadata_decode_row (t, i, cols, MONO_METHOD_SIZE);

		if (!strcmp (mono_metadata_string_heap (m, cols [3]), 
			     ".ctor") &&
		    (cols [2] & METHOD_ATTRIBUTE_SPECIAL_NAME)) {

			ptr = mono_metadata_blob_heap (m, cols [4]);
			len = mono_metadata_decode_value (ptr, &ptr);

			if (len == 5 && !memcmp (ptr, sig, len)) {
				ctor = MONO_TOKEN_METHOD_DEF | (i + 1);
				break;
			}
		} 

	}

	return ctor;
}

static guint32
typedef_from_name (MonoImage *image, const char *name, const char *nspace, guint32 *mlist)
{
	MonoMetadata *m = &image->metadata;
	MonoTableInfo *t = &m->tables [MONO_TABLE_TYPEDEF];
	guint32 i;
	guint32 cols [MONO_TYPEDEF_SIZE];

	for (i=0; i < t->rows; ++i) {
		mono_metadata_decode_row (t, i, cols, MONO_TYPEDEF_SIZE);
		if (strcmp (name, mono_metadata_string_heap (m, cols [MONO_TYPEDEF_NAME])) == 0 
				&& strcmp (nspace, mono_metadata_string_heap (m, cols [MONO_TYPEDEF_NAMESPACE])) == 0) {
			*mlist = cols [MONO_TYPEDEF_METHOD_LIST];
			return i + 1;
		}
	}
	g_assert_not_reached ();
	return 0;
}

static void
methoddef_from_memberref (MonoImage *image, guint32 index, MonoImage **rimage, guint32 *rindex)
{
	MonoMetadata *m = &image->metadata;
	MonoTableInfo *tables = m->tables;
	guint32 cols[6];
	guint32 nindex, sig_len, msig_len, class, i;
	const char *sig, *msig, *mname, *name, *nspace;

	mono_metadata_decode_row (&tables [MONO_TABLE_MEMBERREF], index-1, cols, 3);
	nindex = cols [MONO_MEMBERREF_CLASS] >> MEMBERREF_PARENT_BITS;
	class = cols [MONO_MEMBERREF_CLASS] & MEMBERREF_PARENT_MASK;
	/*g_print ("methodref: 0x%x 0x%x %s\n", class, nindex,
		mono_metadata_string_heap (m, cols [MONO_MEMBERREF_NAME]));*/
	sig = mono_metadata_blob_heap (m, cols [MONO_MEMBERREF_SIGNATURE]);
	sig_len = mono_metadata_decode_blob_size (sig, &sig);
	mname = mono_metadata_string_heap (m, cols [MONO_MEMBERREF_NAME]);

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
			nspace = mono_metadata_string_heap (m, cols [MONO_TYPEREF_NAMESPACE]);
			name = mono_metadata_string_heap (m, cols [MONO_TYPEREF_NAME]);

			/* this will triggered by references to mscorlib */
			g_assert (image->references [scopeindex-1] != NULL);

			image = image->references [scopeindex-1]->image;

			m = &image->metadata;
			tables = &m->tables [MONO_TABLE_METHOD];
			typedef_from_name (image, name, nspace, &i);
			/* mostly dumb search for now */
			for (;i < tables->rows; ++i) {
				mono_metadata_decode_row (tables, i, cols, MONO_METHOD_SIZE);
				msig = mono_metadata_blob_heap (m, cols [MONO_METHOD_SIGNATURE]);
				msig_len = mono_metadata_decode_blob_size (msig, &msig);
				
				if (strcmp (mname, mono_metadata_string_heap (m, cols [MONO_METHOD_NAME])) == 0 
						&& sig_len == msig_len
						&& strncmp (sig, msig, sig_len) == 0) {
					*rimage = image;
					*rindex = i + 1;
					return;
				}
			}
			g_assert_not_reached ();
			break;
		default:
			g_assert_not_reached ();
		}
		break;
	}
	default:
		g_assert_not_reached ();
	}
}

static ffi_type *
ves_map_ffi_type (MonoType *type)
{
	ffi_type *rettype;

	if (!type)
		return &ffi_type_void;

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
	default:
		g_warning ("not implemented");
		g_assert_not_reached ();
	}

	return rettype;
}

extern cos();
static void
fill_pinvoke_info (MonoImage *image, MonoMethodPInvoke *piinfo, int index)
{
	MonoMethod *mh = &piinfo->method;
	MonoTableInfo *tables = image->metadata.tables;
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

			import = mono_metadata_string_heap (&image->metadata, 
							    im_cols [2]);

			mono_metadata_decode_row (mr, im_cols [3] - 1, mr_cols,
						  1);
			
			scope = mono_metadata_string_heap (&image->metadata, 
							   mr_cols [0]);
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

	g_module_symbol (gmodule, import, &piinfo->addr); 

	g_assert (piinfo->addr);

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
mono_get_method (MonoImage *image, guint32 token)
{
	MonoMethod *result;
	int table = mono_metadata_token_table (token);
	int index = mono_metadata_token_index (token);
	MonoTableInfo *tables = image->metadata.tables;
	const char *loc;
	const char *sig = NULL;
	int size;
	guint32 cols[6];

	if (table == MONO_TABLE_METHOD && (result = g_hash_table_lookup (image->method_cache, GINT_TO_POINTER (token))))
			return result;
	
	if (table != MONO_TABLE_METHOD) {
		g_assert (table == MONO_TABLE_MEMBERREF);
		methoddef_from_memberref (image, index, &image, &token);
		return mono_get_method (image, MONO_TOKEN_METHOD_DEF | token);
	}

	mono_metadata_decode_row (&tables [table], index - 1, cols, 6);

	if (cols [2] & METHOD_ATTRIBUTE_PINVOKE_IMPL)
		result = (MonoMethod *)g_new0 (MonoMethodPInvoke, 1);
	else
		result = (MonoMethod *)g_new0 (MonoMethodManaged, 1);

	result->image = image;
	result->flags = cols [2];
	result->iflags = cols [1];
	result->name = mono_metadata_string_heap (&image->metadata, cols [3]);

	if (!sig) /* already taken from the methodref */
		sig = mono_metadata_blob_heap (&image->metadata, cols [4]);
	size = mono_metadata_decode_blob_size (sig, &sig);
	result->signature = mono_metadata_parse_method_signature (&image->metadata, 0, sig, NULL);


	if (result->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) {
		fill_pinvoke_info (image, (MonoMethodPInvoke *)result, 
				   index - 1);
	} else {
		/* if this is a methodref from another module/assembly, this fails */
		loc = mono_cli_rva_map ((MonoCLIImageInfo *)image->image_info, cols [0]);
		g_assert (loc);
		((MonoMethodManaged *)result)->header = 
			mono_metadata_parse_mh (&image->metadata, loc);
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
	} else {
		mono_metadata_free_mh (((MonoMethodManaged *)method)->header);
	}

	g_free (method);
}

