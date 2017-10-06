/**
 * \file
 * Functions for creating IL methods at runtime.
 * 
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * Copyright 2002-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "config.h"
#include "loader.h"
#include "mono/metadata/abi-details.h"
#include "mono/metadata/method-builder.h"
#include "mono/metadata/tabledefs.h"
#include "mono/metadata/exception.h"
#include "mono/metadata/appdomain.h"
#include "mono/metadata/debug-helpers.h"
#include "mono/metadata/metadata-internals.h"
#include "mono/metadata/domain-internals.h"
#include <string.h>
#include <errno.h>

/* #define DEBUG_RUNTIME_CODE */

#define OPDEF(a,b,c,d,e,f,g,h,i,j) \
	a = i,

enum {
#include "mono/cil/opcode.def"
	LAST = 0xff
};
#undef OPDEF

#ifdef DEBUG_RUNTIME_CODE
static char*
indenter (MonoDisHelper *dh, MonoMethod *method, guint32 ip_offset)
{
	return g_strdup (" ");
}

static MonoDisHelper marshal_dh = {
	"\n",
	"IL_%04x: ",
	"IL_%04x",
	indenter, 
	NULL,
	NULL
};
#endif 

static MonoMethodBuilder *
mono_mb_new_base (MonoClass *klass, MonoWrapperType type)
{
	MonoMethodBuilder *mb;
	MonoMethod *m;

	g_assert (klass != NULL);

	mb = g_new0 (MonoMethodBuilder, 1);

	mb->method = m = (MonoMethod *)g_new0 (MonoMethodWrapper, 1);

	m->klass = klass;
	m->inline_info = 1;
	m->wrapper_type = type;

#ifdef ENABLE_ILGEN
	mb->code_size = 40;
	mb->code = (unsigned char *)g_malloc (mb->code_size);
	mb->init_locals = TRUE;
#endif
	/* placeholder for the wrapper always at index 1 */
	mono_mb_add_data (mb, NULL);

	return mb;
}

MonoMethodBuilder *
mono_mb_new_no_dup_name (MonoClass *klass, const char *name, MonoWrapperType type)
{
	MonoMethodBuilder *mb = mono_mb_new_base (klass, type);
	mb->name = (char*)name;
	mb->no_dup_name = TRUE;
	return mb;
}

/**
 * mono_mb_new:
 */
MonoMethodBuilder *
mono_mb_new (MonoClass *klass, const char *name, MonoWrapperType type)
{
	MonoMethodBuilder *mb = mono_mb_new_base (klass, type);
	mb->name = g_strdup (name);
	return mb;
}

/**
 * mono_mb_free:
 */
void
mono_mb_free (MonoMethodBuilder *mb)
{
#ifdef ENABLE_ILGEN
	GList *l;

	for (l = mb->locals_list; l; l = l->next) {
		/* Allocated in mono_mb_add_local () */
		g_free (l->data);
	}
	g_list_free (mb->locals_list);
	if (!mb->dynamic) {
		g_free (mb->method);
		if (!mb->no_dup_name)
			g_free (mb->name);
		g_free (mb->code);
	}
#else
	g_free (mb->method);
	if (!mb->no_dup_name)
		g_free (mb->name);
#endif
	g_free (mb);
}

/**
 * mono_mb_create_method:
 * Create a \c MonoMethod from this method builder.
 * \returns the newly created method.
 */
MonoMethod *
mono_mb_create_method (MonoMethodBuilder *mb, MonoMethodSignature *signature, int max_stack)
{
#ifdef ENABLE_ILGEN
	MonoMethodHeader *header;
#endif
	MonoMethodWrapper *mw;
	MonoImage *image;
	MonoMethod *method;
	GList *l;
	int i;

	g_assert (mb != NULL);

	image = mb->method->klass->image;

#ifdef ENABLE_ILGEN
	if (mb->dynamic) {
		method = mb->method;
		mw = (MonoMethodWrapper*)method;

		method->name = mb->name;
		method->dynamic = TRUE;

		mw->header = header = (MonoMethodHeader *) 
			g_malloc0 (MONO_SIZEOF_METHOD_HEADER + mb->locals * sizeof (MonoType *));

		header->code = mb->code;

		for (i = 0, l = mb->locals_list; l; l = l->next, i++) {
			header->locals [i] = (MonoType*)l->data;
		}
	} else
#endif
	{
		/* Realloc the method info into a mempool */

		method = (MonoMethod *)mono_image_alloc0 (image, sizeof (MonoMethodWrapper));
		memcpy (method, mb->method, sizeof (MonoMethodWrapper));
		mw = (MonoMethodWrapper*) method;

		if (mb->no_dup_name)
			method->name = mb->name;
		else
			method->name = mono_image_strdup (image, mb->name);

#ifdef ENABLE_ILGEN
		mw->header = header = (MonoMethodHeader *) 
			mono_image_alloc0 (image, MONO_SIZEOF_METHOD_HEADER + mb->locals * sizeof (MonoType *));

		header->code = (const unsigned char *)mono_image_alloc (image, mb->pos);
		memcpy ((char*)header->code, mb->code, mb->pos);

		for (i = 0, l = mb->locals_list; l; l = l->next, i++) {
			header->locals [i] = (MonoType*)l->data;
		}
#endif
	}

#ifdef ENABLE_ILGEN
	/* Free the locals list so mono_mb_free () doesn't free the types twice */
	g_list_free (mb->locals_list);
	mb->locals_list = NULL;
#endif

	method->signature = signature;
	if (!signature->hasthis)
		method->flags |= METHOD_ATTRIBUTE_STATIC;

#ifdef ENABLE_ILGEN
	if (max_stack < 8)
		max_stack = 8;

	header->max_stack = max_stack;

	header->code_size = mb->pos;
	header->num_locals = mb->locals;
	header->init_locals = mb->init_locals;

	header->num_clauses = mb->num_clauses;
	header->clauses = mb->clauses;

	method->skip_visibility = mb->skip_visibility;
#endif

	i = g_list_length ((GList *)mw->method_data);
	if (i) {
		GList *tmp;
		void **data;
		l = g_list_reverse ((GList *)mw->method_data);
		if (method_is_dynamic (method))
			data = (void **)g_malloc (sizeof (gpointer) * (i + 1));
		else
			data = (void **)mono_image_alloc (image, sizeof (gpointer) * (i + 1));
		/* store the size in the first element */
		data [0] = GUINT_TO_POINTER (i);
		i = 1;
		for (tmp = l; tmp; tmp = tmp->next) {
			data [i++] = tmp->data;
		}
		g_list_free (l);

		mw->method_data = data;
	}

#ifdef ENABLE_ILGEN
	/*{
		static int total_code = 0;
		static int total_alloc = 0;
		total_code += mb->pos;
		total_alloc += mb->code_size;
		g_print ("code size: %d of %d (allocated: %d)\n", mb->pos, total_code, total_alloc);
	}*/

#ifdef DEBUG_RUNTIME_CODE
	printf ("RUNTIME CODE FOR %s\n", mono_method_full_name (method, TRUE));
	printf ("%s\n", mono_disasm_code (&marshal_dh, method, mb->code, mb->code + mb->pos));
#endif

	if (mb->param_names) {
		char **param_names = (char **)mono_image_alloc0 (image, signature->param_count * sizeof (gpointer));
		for (i = 0; i < signature->param_count; ++i)
			param_names [i] = mono_image_strdup (image, mb->param_names [i]);

		mono_image_lock (image);
		if (!image->wrapper_param_names)
			image->wrapper_param_names = g_hash_table_new (NULL, NULL);
		g_hash_table_insert (image->wrapper_param_names, method, param_names);
		mono_image_unlock (image);
	}
#endif

	return method;
}

/**
 * mono_mb_add_data:
 */
guint32
mono_mb_add_data (MonoMethodBuilder *mb, gpointer data)
{
	MonoMethodWrapper *mw;

	g_assert (mb != NULL);

	mw = (MonoMethodWrapper *)mb->method;

	/* one O(n) is enough */
	mw->method_data = g_list_prepend ((GList *)mw->method_data, data);

	return g_list_length ((GList *)mw->method_data);
}

#ifdef ENABLE_ILGEN

/**
 * mono_mb_add_local:
 */
int
mono_mb_add_local (MonoMethodBuilder *mb, MonoType *type)
{
	int res;
	MonoType *t;

	/*
	 * Have to make a copy early since type might be sig->ret,
	 * which is transient, see mono_metadata_signature_dup_internal_with_padding ().
	 */
	t = mono_metadata_type_dup (NULL, type);

	g_assert (mb != NULL);
	g_assert (type != NULL);

	res = mb->locals;
	mb->locals_list = g_list_append (mb->locals_list, t);
	mb->locals++;

	return res;
}

/**
 * mono_mb_patch_addr:
 */
void
mono_mb_patch_addr (MonoMethodBuilder *mb, int pos, int value)
{
	mb->code [pos] = value & 0xff;
	mb->code [pos + 1] = (value >> 8) & 0xff;
	mb->code [pos + 2] = (value >> 16) & 0xff;
	mb->code [pos + 3] = (value >> 24) & 0xff;
}

/**
 * mono_mb_patch_addr_s:
 */
void
mono_mb_patch_addr_s (MonoMethodBuilder *mb, int pos, gint8 value)
{
	*((gint8 *)(&mb->code [pos])) = value;
}

/**
 * mono_mb_emit_byte:
 */
void
mono_mb_emit_byte (MonoMethodBuilder *mb, guint8 op)
{
	if (mb->pos >= mb->code_size) {
		mb->code_size += mb->code_size >> 1;
		mb->code = (unsigned char *)g_realloc (mb->code, mb->code_size);
	}

	mb->code [mb->pos++] = op;
}

/**
 * mono_mb_emit_ldflda:
 */
void
mono_mb_emit_ldflda (MonoMethodBuilder *mb, gint32 offset)
{
        mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
        mono_mb_emit_byte (mb, CEE_MONO_OBJADDR);

	if (offset) {
		mono_mb_emit_icon (mb, offset);
		mono_mb_emit_byte (mb, CEE_ADD);
	}
}

/**
 * mono_mb_emit_i4:
 */
void
mono_mb_emit_i4 (MonoMethodBuilder *mb, gint32 data)
{
	if ((mb->pos + 4) >= mb->code_size) {
		mb->code_size += mb->code_size >> 1;
		mb->code = (unsigned char *)g_realloc (mb->code, mb->code_size);
	}

	mono_mb_patch_addr (mb, mb->pos, data);
	mb->pos += 4;
}

void
mono_mb_emit_i8 (MonoMethodBuilder *mb, gint64 data)
{
	if ((mb->pos + 8) >= mb->code_size) {
		mb->code_size += mb->code_size >> 1;
		mb->code = (unsigned char *)g_realloc (mb->code, mb->code_size);
	}

	mono_mb_patch_addr (mb, mb->pos, data);
	mono_mb_patch_addr (mb, mb->pos + 4, data >> 32);
	mb->pos += 8;
}

/**
 * mono_mb_emit_i2:
 */
void
mono_mb_emit_i2 (MonoMethodBuilder *mb, gint16 data)
{
	if ((mb->pos + 2) >= mb->code_size) {
		mb->code_size += mb->code_size >> 1;
		mb->code = (unsigned char *)g_realloc (mb->code, mb->code_size);
	}

	mb->code [mb->pos] = data & 0xff;
	mb->code [mb->pos + 1] = (data >> 8) & 0xff;
	mb->pos += 2;
}

void
mono_mb_emit_op (MonoMethodBuilder *mb, guint8 op, gpointer data)
{
	mono_mb_emit_byte (mb, op);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, data));
}

/**
 * mono_mb_emit_ldstr:
 */
void
mono_mb_emit_ldstr (MonoMethodBuilder *mb, char *str)
{
	mono_mb_emit_op (mb, CEE_LDSTR, str);
}

/**
 * mono_mb_emit_ldarg:
 */
void
mono_mb_emit_ldarg (MonoMethodBuilder *mb, guint argnum)
{
	if (argnum < 4) {
 		mono_mb_emit_byte (mb, CEE_LDARG_0 + argnum);
	} else if (argnum < 256) {
		mono_mb_emit_byte (mb, CEE_LDARG_S);
		mono_mb_emit_byte (mb, argnum);
	} else {
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_LDARG);
		mono_mb_emit_i2 (mb, argnum);
	}
}

/**
 * mono_mb_emit_ldarg_addr:
 */
void
mono_mb_emit_ldarg_addr (MonoMethodBuilder *mb, guint argnum)
{
	if (argnum < 256) {
		mono_mb_emit_byte (mb, CEE_LDARGA_S);
		mono_mb_emit_byte (mb, argnum);
	} else {
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_LDARGA);
		mono_mb_emit_i2 (mb, argnum);
	}
}

/**
 * mono_mb_emit_ldloc_addr:
 */
void
mono_mb_emit_ldloc_addr (MonoMethodBuilder *mb, guint locnum)
{
	if (locnum < 256) {
		mono_mb_emit_byte (mb, CEE_LDLOCA_S);
		mono_mb_emit_byte (mb, locnum);
	} else {
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_LDLOCA);
		mono_mb_emit_i2 (mb, locnum);
	}
}

/**
 * mono_mb_emit_ldloc:
 */
void
mono_mb_emit_ldloc (MonoMethodBuilder *mb, guint num)
{
	if (num < 4) {
 		mono_mb_emit_byte (mb, CEE_LDLOC_0 + num);
	} else if (num < 256) {
		mono_mb_emit_byte (mb, CEE_LDLOC_S);
		mono_mb_emit_byte (mb, num);
	} else {
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_LDLOC);
		mono_mb_emit_i2 (mb, num);
	}
}

/**
 * mono_mb_emit_stloc:
 */
void
mono_mb_emit_stloc (MonoMethodBuilder *mb, guint num)
{
	if (num < 4) {
 		mono_mb_emit_byte (mb, CEE_STLOC_0 + num);
	} else if (num < 256) {
		mono_mb_emit_byte (mb, CEE_STLOC_S);
		mono_mb_emit_byte (mb, num);
	} else {
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_STLOC);
		mono_mb_emit_i2 (mb, num);
	}
}

/**
 * mono_mb_emit_icon:
 */
void
mono_mb_emit_icon (MonoMethodBuilder *mb, gint32 value)
{
	if (value >= -1 && value < 8) {
		mono_mb_emit_byte (mb, CEE_LDC_I4_0 + value);
	} else if (value >= -128 && value <= 127) {
		mono_mb_emit_byte (mb, CEE_LDC_I4_S);
		mono_mb_emit_byte (mb, value);
	} else {
		mono_mb_emit_byte (mb, CEE_LDC_I4);
		mono_mb_emit_i4 (mb, value);
	}
}

void
mono_mb_emit_icon8 (MonoMethodBuilder *mb, gint64 value)
{
	mono_mb_emit_byte (mb, CEE_LDC_I8);
	mono_mb_emit_i8 (mb, value);
}

int
mono_mb_get_label (MonoMethodBuilder *mb)
{
	return mb->pos;
}

int
mono_mb_get_pos (MonoMethodBuilder *mb)
{
	return mb->pos;
}

/**
 * mono_mb_emit_branch:
 */
guint32
mono_mb_emit_branch (MonoMethodBuilder *mb, guint8 op)
{
	guint32 res;
	mono_mb_emit_byte (mb, op);
	res = mb->pos;
	mono_mb_emit_i4 (mb, 0);
	return res;
}

guint32
mono_mb_emit_short_branch (MonoMethodBuilder *mb, guint8 op)
{
	guint32 res;
	mono_mb_emit_byte (mb, op);
	res = mb->pos;
	mono_mb_emit_byte (mb, 0);

	return res;
}

void
mono_mb_emit_branch_label (MonoMethodBuilder *mb, guint8 op, guint32 label)
{
	mono_mb_emit_byte (mb, op);
	mono_mb_emit_i4 (mb, label - (mb->pos + 4));
}

void
mono_mb_patch_branch (MonoMethodBuilder *mb, guint32 pos)
{
	mono_mb_patch_addr (mb, pos, mb->pos - (pos + 4));
}

void
mono_mb_patch_short_branch (MonoMethodBuilder *mb, guint32 pos)
{
	mono_mb_patch_addr_s (mb, pos, mb->pos - (pos + 1));
}

void
mono_mb_emit_ptr (MonoMethodBuilder *mb, gpointer ptr)
{
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_op (mb, CEE_MONO_LDPTR, ptr);
}

void
mono_mb_emit_calli (MonoMethodBuilder *mb, MonoMethodSignature *sig)
{
	mono_mb_emit_op (mb, CEE_CALLI, sig);
}

/**
 * mono_mb_emit_managed_call:
 */
void
mono_mb_emit_managed_call (MonoMethodBuilder *mb, MonoMethod *method, MonoMethodSignature *opt_sig)
{
	mono_mb_emit_op (mb, CEE_CALL, method);
}

/**
 * mono_mb_emit_native_call:
 */
void
mono_mb_emit_native_call (MonoMethodBuilder *mb, MonoMethodSignature *sig, gpointer func)
{
	mono_mb_emit_ptr (mb, func);
	mono_mb_emit_calli (mb, sig);
}

void
mono_mb_emit_icall (MonoMethodBuilder *mb, gpointer func)
{
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_op (mb, CEE_MONO_ICALL, func);
}

void
mono_mb_emit_exception_full (MonoMethodBuilder *mb, const char *exc_nspace, const char *exc_name, const char *msg)
{
	MonoMethod *ctor = NULL;

	MonoClass *mme = mono_class_load_from_name (mono_defaults.corlib, exc_nspace, exc_name);
	mono_class_init (mme);
	ctor = mono_class_get_method_from_name (mme, ".ctor", 0);
	g_assert (ctor);
	mono_mb_emit_op (mb, CEE_NEWOBJ, ctor);
	if (msg != NULL) {
		mono_mb_emit_byte (mb, CEE_DUP);
		mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoException, message));
		mono_mb_emit_ldstr (mb, (char*)msg);
		mono_mb_emit_byte (mb, CEE_STIND_REF);
	}
	mono_mb_emit_byte (mb, CEE_THROW);
}

/**
 * mono_mb_emit_exception:
 */
void
mono_mb_emit_exception (MonoMethodBuilder *mb, const char *exc_name, const char *msg)
{
	mono_mb_emit_exception_full (mb, "System", exc_name, msg);
}

/**
 * mono_mb_emit_exception_for_error:
 */
void
mono_mb_emit_exception_for_error (MonoMethodBuilder *mb, MonoError *error)
{
	/*
	 * If at some point there is need to support other types of errors,
	 * the behaviour should conform with mono_error_prepare_exception().
	 */
	g_assert (mono_error_get_error_code (error) == MONO_ERROR_GENERIC && "Unsupported error code.");
	mono_mb_emit_exception_full (mb, "System", mono_error_get_exception_name (error), mono_error_get_message (error));
}

/**
 * mono_mb_emit_add_to_local:
 */
void
mono_mb_emit_add_to_local (MonoMethodBuilder *mb, guint16 local, gint32 incr)
{
	mono_mb_emit_ldloc (mb, local); 
	mono_mb_emit_icon (mb, incr);
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_stloc (mb, local); 
}

void
mono_mb_set_clauses (MonoMethodBuilder *mb, int num_clauses, MonoExceptionClause *clauses)
{
	mb->num_clauses = num_clauses;
	mb->clauses = clauses;
}

/*
 * mono_mb_set_param_names:
 *
 *   PARAM_NAMES should have length equal to the sig->param_count, the caller retains
 * ownership of the array, and its entries.
 */
void
mono_mb_set_param_names (MonoMethodBuilder *mb, const char **param_names)
{
	mb->param_names = param_names;
}

#endif /* DISABLE_JIT */
