/**
 * \file
 * Copyright 2018 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include "config.h"
#include "loader.h"
#include "mono/metadata/abi-details.h"
#include "mono/metadata/method-builder.h"
#include "mono/metadata/method-builder-ilgen.h"
#include "mono/metadata/method-builder-ilgen-internals.h"
#include "mono/metadata/tabledefs.h"
#include "mono/metadata/exception.h"
#include "mono/metadata/appdomain.h"
#include "mono/metadata/debug-helpers.h"
#include "mono/metadata/metadata-internals.h"
#include "mono/metadata/domain-internals.h"
#include <string.h>
#include <errno.h>
#include "class-init.h"

#define OPDEF(a,b,c,d,e,f,g,h,i,j) \
	a = i,

enum {
#include "mono/cil/opcode.def"
	LAST = 0xff
};
#undef OPDEF

static MonoMethodBuilder *
new_base_ilgen (MonoClass *klass, MonoWrapperType type)
{
	MonoMethodBuilder *mb;
	MonoMethod *m;

	g_assert (klass != NULL);

	mb = g_new0 (MonoMethodBuilder, 1);

	mb->method = m = (MonoMethod *)g_new0 (MonoMethodWrapper, 1);

	m->klass = klass;
	m->inline_info = 1;
	m->wrapper_type = type;

	mb->code_size = 40;
	mb->code = (unsigned char *)g_malloc (mb->code_size);
	mb->init_locals = TRUE;

	/* placeholder for the wrapper always at index 1 */
	mono_mb_add_data (mb, NULL);

	return mb;
}

static void
free_ilgen (MonoMethodBuilder *mb)
{
	GList *l;

	for (l = mb->locals_list; l; l = l->next) {
		/* Allocated in mono_mb_add_local () */
		g_free (l->data);
	}
	g_list_free (mb->locals_list);
	if (!mb->dynamic)
		g_free (mb->method);
	if (!mb->no_dup_name)
		g_free (mb->name);
	g_free (mb->code);
	g_free (mb);
}

static gpointer
mb_alloc0 (MonoMethodBuilder *mb, int size)
{
	if (mb->dynamic)
		return g_malloc0 (size);
	else if (mb->mem_manager)
		return mono_mem_manager_alloc0 (mb->mem_manager, size);
	else
		return mono_image_alloc0 (m_class_get_image (mb->method->klass), size);
}

static char*
mb_strdup (MonoMethodBuilder *mb, const char *s)
{
	if (mb->dynamic)
		return g_strdup (s);
	else if (mb->mem_manager)
		return mono_mem_manager_strdup (mb->mem_manager, s);
	else
		return mono_image_strdup (m_class_get_image (mb->method->klass), s);
}

static MonoMethod *
create_method_ilgen (MonoMethodBuilder *mb, MonoMethodSignature *signature, int max_stack)
{
	MonoMethodHeader *header;
	MonoMethodWrapper *mw;
	MonoImage *image;
	MonoMethod *method;
	GList *l;
	int i;

	g_assert (mb != NULL);

	image = m_class_get_image (mb->method->klass);

	if (mb->dynamic) {
		/* Allocated in reflection_methodbuilder_to_mono_method () */
		method = mb->method;
	} else {
		method = (MonoMethod *)mb_alloc0 (mb, sizeof (MonoMethodWrapper));
		memcpy (method, mb->method, sizeof (MonoMethodWrapper));
	}
	mw = (MonoMethodWrapper*) method;
	mw->mem_manager = mb->mem_manager;
	if (mb->no_dup_name)
		method->name = mb->name;
	else
		method->name = mb_strdup (mb, mb->name);
	method->dynamic = mb->dynamic;
	mw->header = header = (MonoMethodHeader *)
		mb_alloc0 (mb, MONO_SIZEOF_METHOD_HEADER + mb->locals * sizeof (MonoType *));
	header->code = (const unsigned char *)mb_alloc0 (mb, mb->pos);
	memcpy ((char*)header->code, mb->code, mb->pos);

	for (i = 0, l = mb->locals_list; l; l = l->next, i++) {
		MonoType *type = (MonoType*)l->data;
		if (mb->mem_manager) {
			/* Allocated in mono_mb_add_local () */
			int size = mono_sizeof_type (type);
			header->locals [i] = mono_mem_manager_alloc0 (mb->mem_manager, size);
			memcpy (header->locals [i], type, size);
			g_free (type);
		} else {
			header->locals [i] = type;
		}
	}

	/* Free the locals list so mono_mb_free () doesn't free the types twice */
	g_list_free (mb->locals_list);
	mb->locals_list = NULL;

	method->signature = signature;
	if (!signature->hasthis)
		method->flags |= METHOD_ATTRIBUTE_STATIC;

	if (max_stack < 8)
		max_stack = 8;

	header->max_stack = max_stack;

	header->code_size = mb->pos;
	header->num_locals = mb->locals;
	header->init_locals = mb->init_locals;
	header->volatile_args = mb->volatile_args;
	header->volatile_locals = mb->volatile_locals;
	mb->volatile_args = NULL;
	mb->volatile_locals = NULL;

	header->num_clauses = mb->num_clauses;
	header->clauses = mb->clauses;

	method->skip_visibility = mb->skip_visibility;

	i = g_list_length ((GList *)mw->method_data);
	if (i) {
		GList *tmp;
		void **data;
		l = g_list_reverse ((GList *)mw->method_data);
		data = (void **)mb_alloc0 (mb, sizeof (gpointer) * (i + 1));
		/* store the size in the first element */
		data [0] = GUINT_TO_POINTER (i);
		i = 1;
		for (tmp = l; tmp; tmp = tmp->next) {
			data [i++] = tmp->data;
		}
		g_list_free (l);

		mw->method_data = data;
	}

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
		char **param_names = (char **)mb_alloc0 (mb, signature->param_count * sizeof (gpointer));
		for (i = 0; i < signature->param_count; ++i)
			param_names [i] = mb_strdup (mb, mb->param_names [i]);

		// FIXME: Mem managers

		mono_image_lock (image);
		if (!image->wrapper_param_names)
			image->wrapper_param_names = g_hash_table_new (NULL, NULL);
		g_hash_table_insert (image->wrapper_param_names, method, param_names);
		mono_image_unlock (image);
	}

	return method;
}

void
mono_method_builder_ilgen_init (void)
{
	MonoMethodBuilderCallbacks cb;
	cb.version = MONO_METHOD_BUILDER_CALLBACKS_VERSION;
	cb.new_base = new_base_ilgen;
	cb.free = free_ilgen;
	cb.create_method = create_method_ilgen;
	mono_install_method_builder_callbacks (&cb);
}

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
mono_mb_emit_icall_id (MonoMethodBuilder *mb, MonoJitICallId jit_icall_id)
{
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_ICALL);
	mono_mb_emit_i4 (mb, jit_icall_id);
}

void
mono_mb_emit_exception_full (MonoMethodBuilder *mb, const char *exc_nspace, const char *exc_name, const char *msg)
{
	ERROR_DECL (error);
	MonoMethod *ctor = NULL;

	MonoClass *mme = mono_class_load_from_name (mono_defaults.corlib, exc_nspace, exc_name);
	mono_class_init_internal (mme);
	ctor = mono_class_get_method_from_name_checked (mme, ".ctor", 0, 0, error);
	mono_error_assert_ok (error);
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
	/* Have to copy the message because it will be referenced from JITed code while the MonoError may be freed. */
	char *msg = mono_mb_strdup (mb, mono_error_get_message (error));
	mono_mb_emit_exception_full (mb, "System", mono_error_get_exception_name (error), msg);
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
mono_mb_emit_no_nullcheck (MonoMethodBuilder *mb)
{
	mono_mb_emit_byte (mb, CEE_PREFIX1);
	mono_mb_emit_byte (mb, CEE_NO_);
	mono_mb_emit_byte (mb, CEE_NO_NULLCHECK);
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
