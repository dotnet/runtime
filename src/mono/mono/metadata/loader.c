/*
 * loader.c: Image Loader 
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
 *   Miguel de Icaza (miguel@ximian.com)
 *   Patrik Torstensson (patrik.torstensson@labs2.com)
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
#include <stdlib.h>
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
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/reflection.h>

static gboolean dummy_icall = TRUE;

MonoDefaults mono_defaults;

CRITICAL_SECTION loader_mutex;

static GHashTable *icall_hash = NULL;

void
mono_loader_init ()
{
	InitializeCriticalSection (&loader_mutex);
}

void
mono_add_internal_call (const char *name, gconstpointer method)
{
	mono_loader_lock ();

	if (!icall_hash) {
		dummy_icall = FALSE;
		icall_hash = g_hash_table_new (g_str_hash , g_str_equal);
	}

	g_hash_table_insert (icall_hash, g_strdup (name), (gpointer) method);

	mono_loader_unlock ();
}

static void
ves_icall_dummy (void)
{
	g_warning ("the mono runtime is not initialized");
	g_assert_not_reached ();
}

gpointer
mono_lookup_internal_call (MonoMethod *method)
{
	char *name;
	char *tmpsig;
	gpointer res;

	if (dummy_icall)
		return ves_icall_dummy;

	if (!method) {
		g_warning ("can't resolve internal call, method is null");
	}

	if (!icall_hash) {
		g_warning ("icall_hash not initialized");
		g_assert_not_reached ();
	}

	mono_loader_lock ();

	if (*method->klass->name_space)
		name = g_strconcat (method->klass->name_space, ".", method->klass->name, "::", method->name, NULL);
	else
		name = g_strconcat (method->klass->name, "::", method->name, NULL);
	if (!(res = g_hash_table_lookup (icall_hash, name))) {
		/* trying to resolve with full signature */
		g_free (name);
	
		tmpsig = mono_signature_get_desc(method->signature, TRUE);
		if (*method->klass->name_space)
			name = g_strconcat (method->klass->name_space, ".", method->klass->name, "::", method->name, "(", tmpsig, ")", NULL);
		else
			name = g_strconcat (method->klass->name, "::", method->name, "(", tmpsig, ")", NULL);
		if (!(res = g_hash_table_lookup (icall_hash, name))) {
			g_warning ("cant resolve internal call to \"%s\" (tested without signature also)", name);
			g_print ("\nYour mono runtime and corlib are out of sync.\n");
			g_print ("Corlib is: %s\n", method->klass->image->name);
			g_print ("\nWhen you update one from cvs you need to update, compile and install\nthe other too.\n");
			g_print ("Do not report this as a bug unless you're sure you have updated correctly:\nyou probably have a broken mono install.\n");
			g_print ("If you see other errors or faults after this message they are probably related\n");
			g_print ("and you need to fix your mono install first.\n");

			g_free (name);
			g_free (tmpsig);

			mono_loader_unlock ();

			return NULL;
		}

		g_free(tmpsig);
	}

	mono_loader_unlock ();

	g_free (name);

	return res;
}

MonoClassField*
mono_field_from_memberref (MonoImage *image, guint32 token, MonoClass **retklass)
{
	MonoClass *klass;
	MonoTableInfo *tables = image->tables;
	guint32 cols[6];
	guint32 nindex, class;
	const char *fname;
	const char *ptr;
	guint32 idx = mono_metadata_token_index (token);

	if (image->assembly->dynamic) {
		MonoClassField *result = mono_lookup_dynamic_token (image, token);
		*retklass = result->parent;
		return result;
	}

	mono_metadata_decode_row (&tables [MONO_TABLE_MEMBERREF], idx-1, cols, MONO_MEMBERREF_SIZE);
	nindex = cols [MONO_MEMBERREF_CLASS] >> MEMBERREF_PARENT_BITS;
	class = cols [MONO_MEMBERREF_CLASS] & MEMBERREF_PARENT_MASK;

	fname = mono_metadata_string_heap (image, cols [MONO_MEMBERREF_NAME]);
	
	ptr = mono_metadata_blob_heap (image, cols [MONO_MEMBERREF_SIGNATURE]);
	mono_metadata_decode_blob_size (ptr, &ptr);
	/* we may want to check the signature here... */

	switch (class) {
	case MEMBERREF_PARENT_TYPEREF:
		klass = mono_class_from_typeref (image, MONO_TOKEN_TYPE_REF | nindex);
		if (!klass) {
			g_warning ("Missing field %s in typeref index %d", fname, nindex);
			return NULL;
		}
		mono_class_init (klass);
		if (retklass)
			*retklass = klass;
		return mono_class_get_field_from_name (klass, fname);
	case MEMBERREF_PARENT_TYPESPEC: {
		/*guint32 bcols [MONO_TYPESPEC_SIZE];
		guint32 len;
		MonoType *type;

		mono_metadata_decode_row (&tables [MONO_TABLE_TYPESPEC], nindex - 1, 
					  bcols, MONO_TYPESPEC_SIZE);
		ptr = mono_metadata_blob_heap (image, bcols [MONO_TYPESPEC_SIGNATURE]);
		len = mono_metadata_decode_value (ptr, &ptr);	
		type = mono_metadata_parse_type (image, MONO_PARSE_TYPE, 0, ptr, &ptr);

		klass = mono_class_from_mono_type (type);
		mono_class_init (klass);
		g_print ("type in sig: %s\n", klass->name);*/
		klass = mono_class_get (image, MONO_TOKEN_TYPE_SPEC | nindex);
		mono_class_init (klass);
		if (retklass)
			*retklass = klass;
		return mono_class_get_field_from_name (klass, fname);
	}
	default:
		g_warning ("field load from %x", class);
		return NULL;
	}
}

MonoClassField*
mono_field_from_token (MonoImage *image, guint32 token, MonoClass **retklass)
{
	MonoClass *k;
	guint32 type;

	if (image->assembly->dynamic) {
		MonoClassField *result = mono_lookup_dynamic_token (image, token);
		*retklass = result->parent;
		return result;
	}

	if (mono_metadata_token_table (token) == MONO_TABLE_MEMBERREF)
		return mono_field_from_memberref (image, token, retklass);

	type = mono_metadata_typedef_from_field (image, mono_metadata_token_index (token));
	if (!type)
		return NULL;
	k = mono_class_get (image, MONO_TOKEN_TYPE_DEF | type);
	mono_class_init (k);
	if (!k)
		return NULL;
	if (retklass)
		*retklass = k;
	return mono_class_get_field (k, token);
}

static gboolean
mono_metadata_signature_vararg_match (MonoMethodSignature *sig1, MonoMethodSignature *sig2)
{
	int i;

	if (sig1->hasthis != sig2->hasthis ||
	    sig1->sentinelpos != sig2->sentinelpos)
		return FALSE;

	for (i = 0; i < sig1->sentinelpos; i++) { 
		MonoType *p1 = sig1->params[i];
		MonoType *p2 = sig2->params[i];
		
		//if (p1->attrs != p2->attrs)
		//	return FALSE;
		
		if (!mono_metadata_type_equal (p1, p2))
			return FALSE;
	}

	if (!mono_metadata_type_equal (sig1->ret, sig2->ret))
		return FALSE;
	return TRUE;
}

static MonoMethod *
find_method (MonoClass *klass, const char* name, MonoMethodSignature *sig)
{
	int i;
	MonoClass *sclass = klass;
	
	if (sig->call_convention == MONO_CALL_VARARG) {
		while (klass) {
			/* mostly dumb search for now */
			for (i = 0; i < klass->method.count; ++i) {
				MonoMethod *m = klass->methods [i];
				if (!strcmp (name, m->name)) {
					if (mono_metadata_signature_vararg_match (sig, m->signature))
						return m;
				}
			}
			if (name [0] == '.' && (strcmp (name, ".ctor") == 0 || strcmp (name, ".cctor") == 0))
				break;
			klass = klass->parent;
		}
		return NULL;
	}
	while (klass) {
		/* mostly dumb search for now */
		for (i = 0; i < klass->method.count; ++i) {
			MonoMethod *m = klass->methods [i];
			if (!strcmp (name, m->name)) {
				if (mono_metadata_signature_equal (sig, m->signature))
					return m;
			}
		}
		if (name [0] == '.' && (strcmp (name, ".ctor") == 0 || strcmp (name, ".cctor") == 0))
			break;
		klass = klass->parent;
	}
	if (sclass->generic_inst) {
		MonoClass *gclass = mono_class_from_mono_type (sclass->generic_inst->data.generic_inst->generic_type);
		MonoMethod *res = find_method (gclass, name, sig);
		if (!res)
			return NULL;
		for (i = 0; i < res->klass->method.count; ++i) {
			if (res == res->klass->methods [i]) {
				return sclass->methods [i];
			}
		}
	}
	return NULL;

}

/*
 * token is the method_ref or method_def token used in a call IL instruction.
 */
MonoMethodSignature*
mono_method_get_signature (MonoMethod *method, MonoImage *image, guint32 token)
{
	int table = mono_metadata_token_table (token);
	int idx = mono_metadata_token_index (token);
	guint32 cols [MONO_MEMBERREF_SIZE];
	MonoMethodSignature *sig;
	const char *ptr;

	/* !table is for wrappers: we should really assign their own token to them */
	if (!table || table == MONO_TABLE_METHOD)
		return method->signature;

	if (table == MONO_TABLE_METHODSPEC) {
		MonoMethodNormal *mn;

		g_assert (!(method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) &&
			  !(method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) &&
			  method->signature);
		mn = (MonoMethodNormal *) method;
		g_assert (mn->header->geninst);

		return method->signature;
	}

	if (method->klass->generic_inst)
		return method->signature;

	if (image->assembly->dynamic)
		/* FIXME: This might be incorrect for vararg methods */
		return method->signature;

	mono_metadata_decode_row (&image->tables [MONO_TABLE_MEMBERREF], idx-1, cols, MONO_MEMBERREF_SIZE);
	
	ptr = mono_metadata_blob_heap (image, cols [MONO_MEMBERREF_SIGNATURE]);
	mono_metadata_decode_blob_size (ptr, &ptr);
	sig = mono_metadata_parse_method_signature (image, 0, ptr, NULL);

	return sig;
}

static MonoMethod *
method_from_memberref (MonoImage *image, guint32 idx)
{
	MonoClass *klass;
	MonoMethod *method;
	MonoTableInfo *tables = image->tables;
	guint32 cols[6];
	guint32 nindex, class;
	const char *mname;
	MonoMethodSignature *sig;
	const char *ptr;

	mono_metadata_decode_row (&tables [MONO_TABLE_MEMBERREF], idx-1, cols, 3);
	nindex = cols [MONO_MEMBERREF_CLASS] >> MEMBERREF_PARENT_BITS;
	class = cols [MONO_MEMBERREF_CLASS] & MEMBERREF_PARENT_MASK;
	/*g_print ("methodref: 0x%x 0x%x %s\n", class, nindex,
		mono_metadata_string_heap (m, cols [MONO_MEMBERREF_NAME]));*/

	mname = mono_metadata_string_heap (image, cols [MONO_MEMBERREF_NAME]);
	
	ptr = mono_metadata_blob_heap (image, cols [MONO_MEMBERREF_SIGNATURE]);
	mono_metadata_decode_blob_size (ptr, &ptr);
	sig = mono_metadata_parse_method_signature (image, 0, ptr, NULL);

	switch (class) {
	case MEMBERREF_PARENT_TYPEREF:
		klass = mono_class_from_typeref (image, MONO_TOKEN_TYPE_REF | nindex);
		if (!klass) {
			g_warning ("Missing method %s in assembly %s typeref index %d", mname, image->name, nindex);
			mono_metadata_free_method_signature (sig);
			return NULL;
		}
		mono_class_init (klass);
		method = find_method (klass, mname, sig);
		if (!method)
			g_warning ("Missing method %s in assembly %s typeref index %d", mname, image->name, nindex);
		mono_metadata_free_method_signature (sig);
		return method;
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

		if (type->type != MONO_TYPE_ARRAY && type->type != MONO_TYPE_SZARRAY) {
			klass = mono_class_from_mono_type (type);
			mono_class_init (klass);
			method = find_method (klass, mname, sig);
			if (!method)
				g_warning ("Missing method %s in assembly %s typeref index %d", mname, image->name, nindex);
			else if (klass->generic_inst)
				method = mono_class_inflate_generic_method (method, klass->generic_inst->data.generic_inst);
			mono_metadata_free_method_signature (sig);
			return method;
		}

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
			result->iflags |= METHOD_IMPL_ATTRIBUTE_RUNTIME;
			result->addr = NULL;
			return result;
		}

		if (!strcmp (mname, "Get")) {
			g_assert (sig->hasthis);
			g_assert (type->data.array->rank == sig->param_count);
			result->iflags |= METHOD_IMPL_ATTRIBUTE_RUNTIME;
			result->addr = NULL;
			return result;
		}

		if (!strcmp (mname, "Address")) {
			g_assert (sig->hasthis);
			g_assert (type->data.array->rank == sig->param_count);
			result->iflags |= METHOD_IMPL_ATTRIBUTE_RUNTIME;
			result->addr = NULL;
			return result;
		}

		g_assert_not_reached ();
		break;
	}
	case MEMBERREF_PARENT_TYPEDEF:
		klass = mono_class_get (image, MONO_TOKEN_TYPE_DEF | nindex);
		if (!klass) {
			g_warning ("Missing method %s in assembly %s typedef index %d", mname, image->name, nindex);
			mono_metadata_free_method_signature (sig);
			return NULL;
		}
		mono_class_init (klass);
		method = find_method (klass, mname, sig);
		if (!method)
			g_warning ("Missing method %s in assembly %s typeref index %d", mname, image->name, nindex);
		mono_metadata_free_method_signature (sig);
		return method;
	case MEMBERREF_PARENT_METHODDEF:
		method = mono_get_method (image, MONO_TOKEN_METHOD_DEF | nindex, NULL);
		return method;
	default:
		g_error ("Memberref parent unknown: class: %d, index %d", class, nindex);
		g_assert_not_reached ();
	}

	return NULL;
}

static MonoMethod *
method_from_methodspec (MonoImage *image, guint32 idx)
{
	MonoMethod *method;
	MonoTableInfo *tables = image->tables;
	MonoGenericInst *ginst;
	const char *ptr;
	guint32 cols [MONO_METHODSPEC_SIZE];
	guint32 token, param_count, i;

	mono_metadata_decode_row (&tables [MONO_TABLE_METHODSPEC], idx - 1, cols, MONO_METHODSPEC_SIZE);
	token = cols [MONO_METHODSPEC_METHOD];
	if ((token & METHODDEFORREF_MASK) == METHODDEFORREF_METHODDEF)
		token = MONO_TOKEN_METHOD_DEF | (token >> METHODDEFORREF_BITS);
	else
		token = MONO_TOKEN_MEMBER_REF | (token >> METHODDEFORREF_BITS);

	method = mono_get_method (image, token, NULL);

	ptr = mono_metadata_blob_heap (image, cols [MONO_METHODSPEC_SIGNATURE]);
	
	mono_metadata_decode_value (ptr, &ptr);
	ptr++;
	param_count = mono_metadata_decode_value (ptr, &ptr);

	ginst = g_new0 (MonoGenericInst, 1);
	ginst->generic_method = method;
	ginst->type_argc = param_count;
	ginst->type_argv = g_new0 (MonoType *, param_count);
	
	for (i = 0; i < param_count; i++)
		ginst->type_argv [i] = mono_metadata_parse_type (image, MONO_PARSE_TYPE, 0, ptr, &ptr);

	return mono_class_inflate_generic_method (method, ginst);
}

typedef struct MonoDllMap MonoDllMap;

struct MonoDllMap {
	char *name;
	char *target;
	char *dll;
	MonoDllMap *next;
};

static GHashTable *dll_map;

int 
mono_dllmap_lookup (const char *dll, const char* func, const char **rdll, const char **rfunc) {
	MonoDllMap *map, *tmp;

	if (!dll_map)
		return 0;

	mono_loader_lock ();

	map = g_hash_table_lookup (dll_map, dll);
	if (!map) {
		mono_loader_unlock ();
		return 0;
	}
	*rdll = map->target? map->target: dll;
		
	for (tmp = map->next; tmp; tmp = tmp->next) {
		if (strcmp (func, tmp->name) == 0) {
			*rfunc = tmp->name;
			if (tmp->dll)
				*rdll = tmp->dll;
			mono_loader_unlock ();
			return 1;
		}
	}
	*rfunc = func;
	mono_loader_unlock ();
	return 1;
}

void
mono_dllmap_insert (const char *dll, const char *func, const char *tdll, const char *tfunc) {
	MonoDllMap *map, *entry;

	mono_loader_lock ();

	if (!dll_map)
		dll_map = g_hash_table_new (g_str_hash, g_str_equal);

	map = g_hash_table_lookup (dll_map, dll);
	if (!map) {
		map = g_new0 (MonoDllMap, 1);
		map->dll = g_strdup (dll);
		if (tdll)
			map->target = g_strdup (tdll);
		g_hash_table_insert (dll_map, map->dll, map);
	}
	if (func) {
		entry = g_new0 (MonoDllMap, 1);
		entry->name = g_strdup (func);
		if (tfunc)
			entry->target = g_strdup (tfunc);
		if (tdll && map->target && strcmp (map->target, tdll))
			entry->dll = g_strdup (tdll);
		entry->next = map->next;
		map->next = entry;
	}

	mono_loader_unlock ();
}

gpointer
mono_lookup_pinvoke_call (MonoMethod *method)
{
	MonoImage *image = method->klass->image;
	MonoMethodPInvoke *piinfo = (MonoMethodPInvoke *)method;
	MonoTableInfo *tables = image->tables;
	MonoTableInfo *im = &tables [MONO_TABLE_IMPLMAP];
	MonoTableInfo *mr = &tables [MONO_TABLE_MODULEREF];
	guint32 im_cols [MONO_IMPLMAP_SIZE];
	guint32 scope_token;
	const char *import = NULL;
	const char *scope = NULL;
	char *full_name;
	GModule *gmodule;

	g_assert (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL);

	if (method->addr)
		return method->addr;
	if (!piinfo->implmap_idx)
		return NULL;
	
	mono_metadata_decode_row (im, piinfo->implmap_idx - 1, im_cols, MONO_IMPLMAP_SIZE);

	piinfo->piflags = im_cols [MONO_IMPLMAP_FLAGS];
	import = mono_metadata_string_heap (image, im_cols [MONO_IMPLMAP_NAME]);
	scope_token = mono_metadata_decode_row_col (mr, im_cols [MONO_IMPLMAP_SCOPE] - 1, MONO_MODULEREF_NAME);
	scope = mono_metadata_string_heap (image, scope_token);

	mono_dllmap_lookup (scope, import, &scope, &import);

	full_name = g_module_build_path (NULL, scope);
	gmodule = g_module_open (full_name, G_MODULE_BIND_LAZY);

	if (!gmodule) {
		g_free (full_name);
		full_name = g_module_build_path (".", scope);
		gmodule = g_module_open (full_name, G_MODULE_BIND_LAZY);
	}

	if (!gmodule) {
		gchar *error = g_strdup (g_module_error ());
		if (!(gmodule=g_module_open (scope, G_MODULE_BIND_LAZY))) {
			g_warning ("Failed to load library %s (%s): %s", full_name, scope, error);
			g_free (error);
			g_free (full_name);
			return NULL;
		}
		g_free (error);
	}
	g_free (full_name);

	if (piinfo->piflags & PINVOKE_ATTRIBUTE_NO_MANGLE) {
		g_module_symbol (gmodule, import, &method->addr); 
	} else {
		char *mangled_name;

		switch (piinfo->piflags & PINVOKE_ATTRIBUTE_CHAR_SET_MASK) {
		case PINVOKE_ATTRIBUTE_CHAR_SET_UNICODE:
			mangled_name = g_strconcat (import, "W", NULL);
			g_module_symbol (gmodule, mangled_name, &method->addr); 
			g_free (mangled_name);

			if (!method->addr)
				g_module_symbol (gmodule, import, &method->addr); 
			break;
		case PINVOKE_ATTRIBUTE_CHAR_SET_AUTO:
			g_module_symbol (gmodule, import, &method->addr); 
			break;
		case PINVOKE_ATTRIBUTE_CHAR_SET_ANSI:
		default:
			mangled_name = g_strconcat (import, "A", NULL);
			g_module_symbol (gmodule, mangled_name, &method->addr); 
			g_free (mangled_name);

			if (!method->addr)
				g_module_symbol (gmodule, import, &method->addr); 
			       
			break;					
		}
	}

	if (!method->addr) {
		g_warning ("Failed to load function %s from %s", import, scope);
		return NULL;
	}
	return method->addr;
}

static MonoMethod *
mono_get_method_from_token (MonoImage *image, guint32 token, MonoClass *klass)
{
	MonoMethod *result;
	int table = mono_metadata_token_table (token);
	int idx = mono_metadata_token_index (token);
	MonoTableInfo *tables = image->tables;
	const char *loc, *sig = NULL;
	int size, i;
	guint32 cols [MONO_TYPEDEF_SIZE];

	if (image->assembly->dynamic)
		return mono_lookup_dynamic_token (image, token);

	if (table != MONO_TABLE_METHOD) {
		if (table == MONO_TABLE_METHODSPEC)
			return method_from_methodspec (image, idx);
		if (table != MONO_TABLE_MEMBERREF)
			g_print("got wrong token: 0x%08x\n", token);
		g_assert (table == MONO_TABLE_MEMBERREF);
		result = method_from_memberref (image, idx);

		return result;
	}

	mono_metadata_decode_row (&tables [table], idx - 1, cols, 6);

	if ((cols [2] & METHOD_ATTRIBUTE_PINVOKE_IMPL) ||
	    (cols [1] & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL))
		result = (MonoMethod *)g_new0 (MonoMethodPInvoke, 1);
	else 
		result = (MonoMethod *)g_new0 (MonoMethodNormal, 1);
	
	result->slot = -1;
	result->klass = klass;
	result->flags = cols [2];
	result->iflags = cols [1];
	result->token = token;
	result->name = mono_metadata_string_heap (image, cols [3]);

	if (!sig) /* already taken from the methodref */
		sig = mono_metadata_blob_heap (image, cols [4]);
	size = mono_metadata_decode_blob_size (sig, &sig);
	result->signature = mono_metadata_parse_method_signature (image, idx, sig, NULL);

	if (!result->klass) {
		guint32 type = mono_metadata_typedef_from_method (image, token);
		result->klass = mono_class_get (image, MONO_TOKEN_TYPE_DEF | type);
	}

	if (cols [1] & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) {
		if (result->klass == mono_defaults.string_class && !strcmp (result->name, ".ctor"))
			result->string_ctor = 1;

		result->signature->pinvoke = 1;
	} else if (cols [2] & METHOD_ATTRIBUTE_PINVOKE_IMPL) {
		MonoMethodPInvoke *piinfo = (MonoMethodPInvoke *)result;
		MonoTableInfo *im = &tables [MONO_TABLE_IMPLMAP];
		MonoCallConvention conv;

		result->signature->pinvoke = 1;
		piinfo->implmap_idx = mono_metadata_implmap_from_method (image, idx - 1);
		piinfo->piflags = mono_metadata_decode_row_col (im, piinfo->implmap_idx - 1, MONO_IMPLMAP_FLAGS);

		switch (piinfo->piflags & PINVOKE_ATTRIBUTE_CALL_CONV_MASK) {
		case PINVOKE_ATTRIBUTE_CALL_CONV_WINAPI:
			conv = MONO_CALL_DEFAULT;
			break;
		case PINVOKE_ATTRIBUTE_CALL_CONV_CDECL:
			conv = MONO_CALL_C;
			break;
		case PINVOKE_ATTRIBUTE_CALL_CONV_STDCALL:
			conv = MONO_CALL_STDCALL;
			break;
		case PINVOKE_ATTRIBUTE_CALL_CONV_THISCALL:
			conv = MONO_CALL_THISCALL;
			break;
		case PINVOKE_ATTRIBUTE_CALL_CONV_FASTCALL:
			conv = MONO_CALL_FASTCALL;
			break;
		case PINVOKE_ATTRIBUTE_CALL_CONV_GENERIC:
		case PINVOKE_ATTRIBUTE_CALL_CONV_GENERICINST:
		default:
			g_warning ("unsupported calling convention");
			g_assert_not_reached ();
		}	
		result->signature->call_convention = conv;
	} else {
		/* if this is a methodref from another module/assembly, this fails */
		loc = mono_cli_rva_map ((MonoCLIImageInfo *)image->image_info, cols [0]);

		if (!result->klass->dummy && !(result->flags & METHOD_ATTRIBUTE_ABSTRACT) &&
					!(result->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME)) {
			MonoMethodNormal *mn = (MonoMethodNormal *) result;

			g_assert (loc);
			mn->header = mono_metadata_parse_mh (image, loc);

			if (result->signature->generic_param_count) {
				mn->header->gen_params = mono_metadata_load_generic_params (image, token, NULL);

				for (i = 0; i < result->signature->generic_param_count; i++)
					mn->header->gen_params [i].method = result;
			}
		}
	}

	return result;
}

MonoMethod *
mono_get_method (MonoImage *image, guint32 token, MonoClass *klass)
{
	MonoMethod *result;

	/* We do everything inside the lock to prevent creation races */

	mono_loader_lock ();

	if ((result = g_hash_table_lookup (image->method_cache, GINT_TO_POINTER (token)))) {
		mono_loader_unlock ();
		return result;
	}

	result = mono_get_method_from_token (image, token, klass);

	g_hash_table_insert (image->method_cache, GINT_TO_POINTER (token), result);

	mono_loader_unlock ();

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
	MonoTableInfo *methodt;
	MonoTableInfo *paramt;

	if (!method->signature->param_count)
		return;
	for (i = 0; i < method->signature->param_count; ++i)
		names [i] = "";

	mono_class_init (klass);

	if (klass->wastypebuilder || klass->generic_inst) /* copy the names later */
		return;

	if (klass->image->assembly->dynamic) {
		MonoReflectionMethodAux *method_aux = 
			mono_g_hash_table_lookup (
				((MonoDynamicAssembly*)method->klass->image->assembly->dynamic)->method_aux_hash, method);
		if (method_aux && method_aux->param_names) {
			for (i = 0; i < method->signature->param_count; ++i)
				if (method_aux->param_names [i])
					names [i] = method_aux->param_names [i];
		}
		return;
	}

	methodt = &klass->image->tables [MONO_TABLE_METHOD];
	paramt = &klass->image->tables [MONO_TABLE_PARAM];
	for (i = 0; i < klass->method.count; ++i) {
		if (method == klass->methods [i]) {
			guint32 idx = klass->method.first + i;
			guint32 cols [MONO_PARAM_SIZE];
			guint param_index = mono_metadata_decode_row_col (methodt, idx, MONO_METHOD_PARAMLIST);

			if (idx + 1 < methodt->rows)
				lastp = mono_metadata_decode_row_col (methodt, idx + 1, MONO_METHOD_PARAMLIST);
			else
				lastp = paramt->rows + 1;
			for (i = param_index; i < lastp; ++i) {
				mono_metadata_decode_row (paramt, i -1, cols, MONO_PARAM_SIZE);
				if (cols [MONO_PARAM_SEQUENCE]) /* skip return param spec */
					names [cols [MONO_PARAM_SEQUENCE] - 1] = mono_metadata_string_heap (klass->image, cols [MONO_PARAM_NAME]);
			}
			return;
		}
	}
}

void
mono_method_get_marshal_info (MonoMethod *method, MonoMarshalSpec **mspecs)
{
	int i, lastp;
	MonoClass *klass = method->klass;
	MonoTableInfo *methodt;
	MonoTableInfo *paramt;

	for (i = 0; i < method->signature->param_count + 1; ++i)
		mspecs [i] = NULL;

	if (method->klass->image->assembly->dynamic) {
		MonoReflectionMethodAux *method_aux = 
			mono_g_hash_table_lookup (
				((MonoDynamicAssembly*)method->klass->image->assembly->dynamic)->method_aux_hash, method);
		if (method_aux && method_aux->param_marshall) {
			MonoMarshalSpec **dyn_specs = method_aux->param_marshall;
			for (i = 0; i < method->signature->param_count + 1; ++i)
				if (dyn_specs [i]) {
					mspecs [i] = g_new0 (MonoMarshalSpec, 1);
					memcpy (mspecs [i], dyn_specs [i], sizeof (MonoMarshalSpec));
				}
		}
		return;
	}

	mono_class_init (klass);

	methodt = &klass->image->tables [MONO_TABLE_METHOD];
	paramt = &klass->image->tables [MONO_TABLE_PARAM];

	for (i = 0; i < klass->method.count; ++i) {
		if (method == klass->methods [i]) {
			guint32 idx = klass->method.first + i;
			guint32 cols [MONO_PARAM_SIZE];
			guint param_index = mono_metadata_decode_row_col (methodt, idx, MONO_METHOD_PARAMLIST);

			if (idx + 1 < methodt->rows)
				lastp = mono_metadata_decode_row_col (methodt, idx + 1, MONO_METHOD_PARAMLIST);
			else
				lastp = paramt->rows + 1;

			for (i = param_index; i < lastp; ++i) {
				mono_metadata_decode_row (paramt, i -1, cols, MONO_PARAM_SIZE);

				if (cols [MONO_PARAM_FLAGS] & PARAM_ATTRIBUTE_HAS_FIELD_MARSHAL) {
					const char *tp;
					tp = mono_metadata_get_marshal_info (klass->image, i - 1, FALSE);
					g_assert (tp);
					mspecs [cols [MONO_PARAM_SEQUENCE]]= mono_metadata_parse_marshal_spec (klass->image, tp);
				}
			}

			return;
		}
	}
}

gboolean
mono_method_has_marshal_info (MonoMethod *method)
{
	int i, lastp;
	MonoClass *klass = method->klass;
	MonoTableInfo *methodt;
	MonoTableInfo *paramt;

	if (method->klass->image->assembly->dynamic) {
		MonoReflectionMethodAux *method_aux = 
			mono_g_hash_table_lookup (
				((MonoDynamicAssembly*)method->klass->image->assembly->dynamic)->method_aux_hash, method);
		MonoMarshalSpec **dyn_specs = method_aux->param_marshall;
		if (dyn_specs) {
			for (i = 0; i < method->signature->param_count + 1; ++i)
				if (dyn_specs [i])
					return TRUE;
		}
		return FALSE;
	}

	mono_class_init (klass);

	methodt = &klass->image->tables [MONO_TABLE_METHOD];
	paramt = &klass->image->tables [MONO_TABLE_PARAM];

	for (i = 0; i < klass->method.count; ++i) {
		if (method == klass->methods [i]) {
			guint32 idx = klass->method.first + i;
			guint32 cols [MONO_PARAM_SIZE];
			guint param_index = mono_metadata_decode_row_col (methodt, idx, MONO_METHOD_PARAMLIST);

			if (idx + 1 < methodt->rows)
				lastp = mono_metadata_decode_row_col (methodt, idx + 1, MONO_METHOD_PARAMLIST);
			else
				lastp = paramt->rows + 1;

			for (i = param_index; i < lastp; ++i) {
				mono_metadata_decode_row (paramt, i -1, cols, MONO_PARAM_SIZE);

				if (cols [MONO_PARAM_FLAGS] & PARAM_ATTRIBUTE_HAS_FIELD_MARSHAL)
					return TRUE;
			}
			return FALSE;
		}
	}
	return FALSE;
}

gpointer
mono_method_get_wrapper_data (MonoMethod *method, guint32 id)
{
	GList *l;
	g_assert (method != NULL);
	g_assert (method->wrapper_type != MONO_WRAPPER_NONE);

	if (!(l = g_list_nth (((MonoMethodWrapper *)method)->data, id - 1)))
		g_assert_not_reached ();

	return l->data;
}

static void
default_stack_walk (MonoStackWalk func, gpointer user_data) {
	g_error ("stack walk not installed");
}

static MonoStackWalkImpl stack_walk = default_stack_walk;

void
mono_stack_walk (MonoStackWalk func, gpointer user_data)
{
	stack_walk (func, user_data);
}

void
mono_install_stack_walk (MonoStackWalkImpl func)
{
	stack_walk = func;
}

static gboolean
last_managed (MonoMethod *m, gint no, gint ilo, gboolean managed, gpointer data)
{
	MonoMethod **dest = data;
	*dest = m;
	/*g_print ("In %s::%s [%d] [%d]\n", m->klass->name, m->name, no, ilo);*/

	return managed;
}

MonoMethod*
mono_method_get_last_managed (void)
{
	MonoMethod *m = NULL;
	stack_walk (last_managed, &m);
	return m;
}



