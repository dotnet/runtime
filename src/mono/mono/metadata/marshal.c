/*
 * marshal.c: Routines for marshaling complex types in P/Invoke methods.
 * 
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2002 Ximian, Inc.  http://www.ximian.com
 *
 */

#include "config.h"
#include "object.h"
#include "loader.h"
#include "metadata/marshal.h"
#include "metadata/tabledefs.h"
#include "metadata/exception.h"
#include "metadata/appdomain.h"
#include "mono/metadata/debug-helpers.h"
#include "mono/metadata/threadpool.h"
#include "mono/metadata/threads.h"
#include "mono/metadata/monitor.h"
#include "mono/metadata/metadata-internals.h"
#include "mono/metadata/domain-internals.h"
#include "mono/metadata/gc-internal.h"
#include <mono/os/gc_wrapper.h>
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

struct _MonoMethodBuilder {
	MonoMethod *method;
	GList *locals_list;
	int locals;
	guint32 code_size, pos;
	unsigned char *code;
};

static void
emit_struct_conv (MonoMethodBuilder *mb, MonoClass *klass, gboolean to_object);

static MonoMethod *
mono_find_method_by_name (MonoClass *klass, const char *name, int param_count)
{
	MonoMethod *res = NULL;
	int i;

	mono_class_init (klass);

	for (i = 0; i < klass->method.count; ++i) {
		if (klass->methods [i]->name[0] == name [0] && 
		    !strcmp (name, klass->methods [i]->name) &&
		    klass->methods [i]->signature->param_count == param_count) {
			res = klass->methods [i];
			break;
		}
	}
	return res;
}

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

/* This mutex protects the various marshalling related caches in MonoImage */
static CRITICAL_SECTION marshal_mutex;

/* Maps wrapper methods to the methods they wrap */
static MonoGHashTable *wrapper_hash;

static guint32 last_error_tls_id;

static void mono_struct_delete_old (MonoClass *klass, char *ptr);
void * mono_marshal_string_to_utf16 (MonoString *s);

static void
register_icall (gpointer func, const char *name, const char *sigstr, gboolean save)
{
	MonoMethodSignature *sig = mono_create_icall_signature (sigstr);

	mono_register_jit_icall (func, name, sig, save);
}

void
mono_marshal_init (void)
{
	static gboolean module_initialized = FALSE;

	if (!module_initialized) {
		module_initialized = TRUE;
		InitializeCriticalSection (&marshal_mutex);
		MONO_GC_REGISTER_ROOT (wrapper_hash);
		wrapper_hash = mono_g_hash_table_new (NULL, NULL);
		last_error_tls_id = TlsAlloc ();

		register_icall (mono_marshal_string_to_utf16, "mono_marshal_string_to_utf16", "ptr obj", FALSE);
		register_icall (mono_string_to_utf16, "mono_string_to_utf16", "ptr obj", FALSE);
		register_icall (mono_string_from_utf16, "mono_string_from_utf16", "obj ptr", FALSE);
		register_icall (mono_string_new_wrapper, "mono_string_new_wrapper", "obj ptr", FALSE);
		register_icall (mono_string_to_utf8, "mono_string_to_utf8", "ptr obj", FALSE);
		register_icall (mono_string_to_bstr, "mono_string_to_bstr", "ptr obj", FALSE);
		register_icall (mono_string_to_ansibstr, "mono_string_to_ansibstr", "ptr object", FALSE);
		register_icall (mono_string_builder_to_utf8, "mono_string_builder_to_utf8", "ptr object", FALSE);
		register_icall (mono_string_builder_to_utf16, "mono_string_builder_to_utf16", "ptr object", FALSE);
		register_icall (mono_array_to_savearray, "mono_array_to_savearray", "ptr object", FALSE);
		register_icall (mono_array_to_lparray, "mono_array_to_lparray", "ptr object", FALSE);
		register_icall (mono_delegate_to_ftnptr, "mono_delegate_to_ftnptr", "ptr object", FALSE);
		register_icall (mono_ftnptr_to_delegate, "mono_ftnptr_to_delegate", "object ptr ptr", FALSE);
		register_icall (mono_marshal_asany, "mono_marshal_asany", "ptr object int32", FALSE);
		register_icall (mono_marshal_free_asany, "mono_marshal_free_asany", "void object ptr int32", FALSE);
		register_icall (mono_string_utf8_to_builder, "mono_string_utf8_to_builder", "void ptr ptr", FALSE);
		register_icall (mono_string_utf16_to_builder, "mono_string_utf16_to_builder", "void ptr ptr", FALSE);
		register_icall (mono_marshal_free_array, "mono_marshal_free_array", "void ptr int32", FALSE);
		register_icall (mono_string_to_byvalstr, "mono_string_to_byvalstr", "void ptr ptr int32", FALSE);
		register_icall (mono_string_to_byvalwstr, "mono_string_to_byvalwstr", "void ptr ptr int32", FALSE);
		register_icall (g_free, "g_free", "void ptr", FALSE);
		register_icall (mono_object_isinst, "mono_object_isinst", "object object ptr", FALSE);
		register_icall (mono_struct_delete_old, "mono_struct_delete_old", "void ptr ptr", FALSE);
	}
}

gpointer
mono_delegate_to_ftnptr (MonoDelegate *delegate)
{
	MonoMethod *method, *wrapper, *invoke;
	MonoMarshalSpec **mspecs;
	MonoClass *klass;
	int i;

	if (!delegate)
		return NULL;

	if (delegate->delegate_trampoline)
		return delegate->delegate_trampoline;

	klass = ((MonoObject *)delegate)->vtable->klass;
	g_assert (klass->delegate);

	method = delegate->method_info->method;
	invoke = mono_find_method_by_name (klass, "Invoke", method->signature->param_count);

	mspecs = g_new (MonoMarshalSpec*, invoke->signature->param_count + 1);
	mono_method_get_marshal_info (invoke, mspecs);

	wrapper = mono_marshal_get_managed_wrapper (method, delegate->target, mspecs);

	for (i = invoke->signature->param_count; i >= 0; i--)
		if (mspecs [i])
			mono_metadata_free_marshal_spec (mspecs [i]);
	g_free (mspecs);

	delegate->delegate_trampoline =  mono_compile_method (wrapper);

	return delegate->delegate_trampoline;
}

MonoDelegate*
mono_ftnptr_to_delegate (MonoClass *klass, gpointer ftn)
{
	MonoDelegate *d;
	MonoJitInfo *ji;

	d = (MonoDelegate*)mono_object_new (mono_domain_get (), klass);

	ji = mono_jit_info_table_find (mono_domain_get (), ftn);
	if (ji == NULL)
		mono_raise_exception (mono_get_exception_argument ("", "Function pointer was not created by a Delegate."));

	/* FIXME: discard the wrapper and call the original method */
	mono_delegate_ctor ((MonoObject*)d, NULL, ftn);

	return d;
}

void
mono_delegate_free_ftnptr (MonoDelegate *delegate)
{
	MonoJitInfo *ji;
	void *ptr, *ptr2;

	ptr2 = delegate->delegate_trampoline;

	ptr = InterlockedExchangePointer (&delegate->delegate_trampoline, NULL);
	ji = mono_jit_info_table_find (mono_domain_get (), ptr);
	g_assert (ji);

	if (!delegate->target) {
		/* The wrapper method is shared between delegates -> no need to free it */
		return;
	}

	mono_runtime_free_method (ji->method);
}

gpointer
mono_array_to_savearray (MonoArray *array)
{
	if (!array)
		return NULL;

	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_array_to_lparray (MonoArray *array)
{
	if (!array)
		return NULL;

	/* fixme: maybe we need to make a copy */
	return array->vector;
}

void
mono_string_utf8_to_builder (MonoStringBuilder *sb, char *text)
{
	GError *error = NULL;
	guint16 *ut;
	glong items_written;
	int l;

	if (!sb || !text)
		return;

	l = strlen (text);

	ut = g_utf8_to_utf16 (text, l, NULL, &items_written, &error);
	
	if (items_written > mono_stringbuilder_capacity (sb))
		items_written = mono_stringbuilder_capacity (sb);
	
	if (!error) {
		memcpy (mono_string_chars (sb->str), ut, items_written * 2);
		sb->length = items_written;
	} else 
		g_error_free (error);

	g_free (ut);
}

void
mono_string_utf16_to_builder (MonoStringBuilder *sb, gunichar2 *text)
{
	guint32 len;

	if (!sb || !text)
		return;

	g_assert (mono_string_chars (sb->str) == text);

	for (len = 0; text [len] != 0; ++len)
		;

	sb->length = len;
}

gpointer
mono_string_builder_to_utf8 (MonoStringBuilder *sb)
{
	GError *error = NULL;
	glong *res;
	gchar *tmp;

	if (!sb)
		return NULL;

	res = g_malloc0 (mono_stringbuilder_capacity (sb) + 1);

	tmp = g_utf16_to_utf8 (mono_string_chars (sb->str), sb->length, NULL, res, &error);
	if (error) {
		g_error_free (error);
		mono_raise_exception (mono_get_exception_execution_engine ("Failed to convert StringBuilder from utf16 to utf8"));
	}
	else {
		memcpy (res, tmp, sb->length + 1);
		g_free (tmp);
	}

	return res;
}

gpointer
mono_string_builder_to_utf16 (MonoStringBuilder *sb)
{
	if (!sb)
		return NULL;

	return mono_string_chars (sb->str);
}

gpointer
mono_string_to_ansibstr (MonoString *string_obj)
{
	g_error ("implement me");
	return NULL;
}

gpointer
mono_string_to_bstr (MonoString *string_obj)
{
	g_error ("implement me");
	return NULL;
}

void
mono_string_to_byvalstr (gpointer dst, MonoString *src, int size)
{
	char *s;
	int len;

	g_assert (dst != NULL);
	g_assert (size > 0);

	memset (dst, 0, size);
	
	if (!src)
		return;

	s = mono_string_to_utf8 (src);
	len = MIN (size, strlen (s));
	memcpy (dst, s, len);
	g_free (s);

	*((char *)dst + size - 1) = 0;
}

void
mono_string_to_byvalwstr (gpointer dst, MonoString *src, int size)
{
	int len;

	g_assert (dst != NULL);
	g_assert (size > 1);

	if (!src) {
		memset (dst, 0, size);
		return;
	}

	len = MIN (size, (mono_string_length (src) * 2));
	memcpy (dst, mono_string_chars (src), len);

	*((char *)dst + size - 1) = 0;
	*((char *)dst + size - 2) = 0;
}

void
mono_mb_free (MonoMethodBuilder *mb)
{
	g_list_free (mb->locals_list);
	g_free (mb);
}

MonoMethodBuilder *
mono_mb_new (MonoClass *klass, const char *name, MonoWrapperType type)
{
	MonoMethodBuilder *mb;
	MonoMethod *m;

	g_assert (klass != NULL);
	g_assert (name != NULL);

	mb = g_new0 (MonoMethodBuilder, 1);

	mb->method = m = (MonoMethod *)g_new0 (MonoMethodWrapper, 1);

	m->klass = klass;
	m->name = g_strdup (name);
	m->inline_info = 1;
	m->inline_count = -1;
	m->wrapper_type = type;

	mb->code_size = 256;
	mb->code = g_malloc (mb->code_size);
	
	return mb;
}

int
mono_mb_add_local (MonoMethodBuilder *mb, MonoType *type)
{
	int res = mb->locals;

	g_assert (mb != NULL);
	g_assert (type != NULL);

	mb->locals_list = g_list_append (mb->locals_list, type);
	mb->locals++;

	return res;
}

MonoMethod *
mono_mb_create_method (MonoMethodBuilder *mb, MonoMethodSignature *signature, int max_stack)
{
	MonoMethodHeader *header;
	GList *l;
	int i;

	g_assert (mb != NULL);

	((MonoMethodNormal *)mb->method)->header = header = (MonoMethodHeader *) 
		g_malloc0 (sizeof (MonoMethodHeader) + mb->locals * sizeof (MonoType *));

	if (max_stack < 8)
		max_stack = 8;

	header->max_stack = max_stack;

	for (i = 0, l = mb->locals_list; l; l = l->next, i++) {
		header->locals [i] = (MonoType *)l->data;
	}

	mb->method->signature = signature;
	header->code = mb->code;
	header->code_size = mb->pos;
	header->num_locals = mb->locals;

#ifdef DEBUG_RUNTIME_CODE
	printf ("RUNTIME CODE FOR %s\n", mono_method_full_name (mb->method, TRUE));
	printf ("%s\n", mono_disasm_code (&marshal_dh, mb->method, mb->code, mb->code + mb->pos));
#endif

	return mb->method;
}

guint32
mono_mb_add_data (MonoMethodBuilder *mb, gpointer data)
{
	MonoMethodWrapper *mw;

	g_assert (mb != NULL);

	mw = (MonoMethodWrapper *)mb->method;

	mw->data = g_list_append (mw->data, data);

	return g_list_length (mw->data);
}

void
mono_mb_patch_addr (MonoMethodBuilder *mb, int pos, int value)
{
	mb->code [pos] = value & 0xff;
	mb->code [pos + 1] = (value >> 8) & 0xff;
	mb->code [pos + 2] = (value >> 16) & 0xff;
	mb->code [pos + 3] = (value >> 24) & 0xff;
}

void
mono_mb_patch_addr_s (MonoMethodBuilder *mb, int pos, gint8 value)
{
	*((gint8 *)(&mb->code [pos])) = value;
}

void
mono_mb_emit_byte (MonoMethodBuilder *mb, guint8 op)
{
	if (mb->pos >= mb->code_size) {
		mb->code_size += 64;
		mb->code = g_realloc (mb->code, mb->code_size);
	}

	mb->code [mb->pos++] = op;
}

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

static int
mono_mb_emit_proxy_check (MonoMethodBuilder *mb, int branch_code)
{
	int pos;
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoObject, vtable));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoVTable, klass));
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_LDPTR);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_defaults.transparent_proxy_class));
	mono_mb_emit_byte (mb, branch_code);
	pos = mb->pos;
	mono_mb_emit_i4 (mb, 0);
	return pos;
}

void
mono_mb_emit_i4 (MonoMethodBuilder *mb, gint32 data)
{
	if ((mb->pos + 4) >= mb->code_size) {
		mb->code_size += 64;
		mb->code = g_realloc (mb->code, mb->code_size);
	}

	mono_mb_patch_addr (mb, mb->pos, data);
	mb->pos += 4;
}

void
mono_mb_emit_i2 (MonoMethodBuilder *mb, gint16 data)
{
	if ((mb->pos + 2) >= mb->code_size) {
		mb->code_size += 64;
		mb->code = g_realloc (mb->code, mb->code_size);
	}

	mb->code [mb->pos] = data & 0xff;
	mb->code [mb->pos + 1] = (data >> 8) & 0xff;
	mb->pos += 2;
}

void
mono_mb_emit_ldstr (MonoMethodBuilder *mb, char *str)
{
	mono_mb_emit_byte (mb, CEE_LDSTR);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, str));
}


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

guint32
mono_mb_emit_branch (MonoMethodBuilder *mb, guint8 op)
{
	guint32 res;
	mono_mb_emit_byte (mb, op);
	res = mb->pos;
	mono_mb_emit_i4 (mb, 0);
	return res;
}

static void
mono_mb_emit_calli (MonoMethodBuilder *mb, MonoMethodSignature *sig)
{
	mono_mb_emit_byte (mb, CEE_CALLI);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, sig));
}

void
mono_mb_emit_managed_call (MonoMethodBuilder *mb, MonoMethod *method, MonoMethodSignature *opt_sig)
{
	mono_mb_emit_byte (mb, CEE_PREFIX1);
	mono_mb_emit_byte (mb, CEE_LDFTN);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, method));
	mono_mb_emit_calli (mb, opt_sig ? opt_sig : method->signature);
}

void
mono_mb_emit_native_call (MonoMethodBuilder *mb, MonoMethodSignature *sig, gpointer func)
{
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_SAVE_LMF);
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_LDPTR);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, func));
	mono_mb_emit_calli (mb, sig);
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_RESTORE_LMF);
}

static void
mono_mb_emit_icall (MonoMethodBuilder *mb, gpointer func)
{
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_ICALL);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, func));
}

void
mono_mb_emit_exception (MonoMethodBuilder *mb, const char *exc_name, const char *msg)
{
	/* fixme: we need a better way to throw exception,
	 * supporting several exception types and messages */
	MonoMethod *ctor = NULL;

	MonoClass *mme = mono_class_from_name (mono_defaults.corlib, "System", exc_name);
	int i;
	mono_class_init (mme);
	for (i = 0; i < mme->method.count; ++i) {
		if (strcmp (mme->methods [i]->name, ".ctor") == 0 && mme->methods [i]->signature->param_count == 0) {
			ctor = mme->methods [i];
			break;
		}
	}
	g_assert (ctor);
	mono_mb_emit_byte (mb, CEE_NEWOBJ);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, ctor));
	if (msg != NULL) {
		mono_mb_emit_byte (mb, CEE_DUP);
		mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoException, message));
		mono_mb_emit_ldstr (mb, (char*)msg);
		mono_mb_emit_byte (mb, CEE_STIND_I);
	}
	mono_mb_emit_byte (mb, CEE_THROW);
}

void
mono_mb_emit_add_to_local (MonoMethodBuilder *mb, guint16 local, gint32 incr)
{
	mono_mb_emit_ldloc (mb, local); 
	mono_mb_emit_icon (mb, incr);
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_stloc (mb, local); 
}

static void
emit_ptr_to_str_conv (MonoMethodBuilder *mb, MonoType *type, MonoMarshalConv conv, 
		      int usize, int msize, MonoMarshalSpec *mspec)
{
	switch (conv) {
	case MONO_MARSHAL_CONV_BOOL_I4:
		mono_mb_emit_byte (mb, CEE_LDLOC_1);
		mono_mb_emit_byte (mb, CEE_LDLOC_0);
		mono_mb_emit_byte (mb, CEE_LDIND_I4);
		mono_mb_emit_byte (mb, CEE_BRFALSE_S);
		mono_mb_emit_byte (mb, 3);
		mono_mb_emit_byte (mb, CEE_LDC_I4_1);
		mono_mb_emit_byte (mb, CEE_BR_S);
		mono_mb_emit_byte (mb, 1);
		mono_mb_emit_byte (mb, CEE_LDC_I4_0);
		mono_mb_emit_byte (mb, CEE_STIND_I1);
		break;
	case MONO_MARSHAL_CONV_BOOL_VARIANTBOOL:
		mono_mb_emit_byte (mb, CEE_LDLOC_1);
		mono_mb_emit_byte (mb, CEE_LDLOC_0);
		mono_mb_emit_byte (mb, CEE_LDIND_I2);
		mono_mb_emit_byte (mb, CEE_BRFALSE_S);
		mono_mb_emit_byte (mb, 3);
		mono_mb_emit_byte (mb, CEE_LDC_I4_1);
		mono_mb_emit_byte (mb, CEE_BR_S);
		mono_mb_emit_byte (mb, 1);
		mono_mb_emit_byte (mb, CEE_LDC_I4_0);
		mono_mb_emit_byte (mb, CEE_STIND_I1);
		break;
	case MONO_MARSHAL_CONV_ARRAY_BYVALARRAY: {
		MonoClass *eclass = NULL;
		int esize;

		if (type->type == MONO_TYPE_SZARRAY) {
			eclass = type->data.klass;
		} else {
			g_assert_not_reached ();
		}

		if (eclass->valuetype)
			esize = mono_class_instance_size (eclass) - sizeof (MonoObject);
		else
			esize = sizeof (gpointer);

		/* create a new array */
		mono_mb_emit_byte (mb, CEE_LDLOC_1);
		mono_mb_emit_icon (mb, mspec->data.array_data.num_elem);
		mono_mb_emit_byte (mb, CEE_NEWARR);	
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, eclass));
		mono_mb_emit_byte (mb, CEE_STIND_I);

		/* copy the elements */
		mono_mb_emit_byte (mb, CEE_LDLOC_1);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoArray, vector));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDLOC_0);
		mono_mb_emit_icon (mb, mspec->data.array_data.num_elem * esize);
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_CPBLK);			

		break;
	}
	case MONO_MARSHAL_CONV_STR_BYVALSTR: 
		mono_mb_emit_byte (mb, CEE_LDLOC_1);
		mono_mb_emit_byte (mb, CEE_LDLOC_0);
		mono_mb_emit_icall (mb, mono_string_new_wrapper);
		mono_mb_emit_byte (mb, CEE_STIND_I);		
		break;
	case MONO_MARSHAL_CONV_STR_LPTSTR:
	case MONO_MARSHAL_CONV_STR_LPSTR:
		mono_mb_emit_byte (mb, CEE_LDLOC_1);
		mono_mb_emit_byte (mb, CEE_LDLOC_0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icall (mb, mono_string_new_wrapper);
		mono_mb_emit_byte (mb, CEE_STIND_I);		
		break;
	case MONO_MARSHAL_CONV_OBJECT_STRUCT: {
		MonoClass *klass = mono_class_from_mono_type (type);
		int src_var, dst_var;

		src_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		dst_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		
		/* *dst = new object */
		mono_mb_emit_byte (mb, CEE_LDLOC_1);
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_NEWOBJ);	
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, klass));
		mono_mb_emit_byte (mb, CEE_STIND_I);
	
		/* save the old src pointer */
		mono_mb_emit_byte (mb, CEE_LDLOC_0);
		mono_mb_emit_stloc (mb, src_var);
		/* save the old dst pointer */
		mono_mb_emit_byte (mb, CEE_LDLOC_1);
		mono_mb_emit_stloc (mb, dst_var);

		/* dst = pointer to newly created object data */
		mono_mb_emit_byte (mb, CEE_LDLOC_1);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icon (mb, sizeof (MonoObject));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_STLOC_1); 

		emit_struct_conv (mb, klass, TRUE);
		
		/* restore the old src pointer */
		mono_mb_emit_ldloc (mb, src_var);
		mono_mb_emit_byte (mb, CEE_STLOC_0);
		/* restore the old dst pointer */
		mono_mb_emit_ldloc (mb, dst_var);
		mono_mb_emit_byte (mb, CEE_STLOC_1);
		break;
	}
	case MONO_MARSHAL_CONV_DEL_FTN: {
		/* fixme: we never convert functions back to delegates, dont 
		// know if thats the correct behaviour
		*/
		break;
	}
	case MONO_MARSHAL_CONV_ARRAY_LPARRAY:
		g_error ("Structure field of type %s can't be marshalled as LPArray", mono_class_from_mono_type (type)->name);
		break;
	case MONO_MARSHAL_CONV_STR_LPWSTR:
	case MONO_MARSHAL_CONV_STR_BSTR:
	case MONO_MARSHAL_CONV_STR_ANSIBSTR:
	case MONO_MARSHAL_CONV_STR_TBSTR:
	case MONO_MARSHAL_CONV_ARRAY_SAVEARRAY:
	case MONO_MARSHAL_CONV_STR_BYVALWSTR: 
	default:
		g_warning ("marshaling conversion %d not implemented", conv);
		g_assert_not_reached ();
	}
}

static gpointer
conv_to_icall (MonoMarshalConv conv)
{
	switch (conv) {
	case MONO_MARSHAL_CONV_STR_LPWSTR:
		return mono_marshal_string_to_utf16;		
	case MONO_MARSHAL_CONV_LPWSTR_STR:
		return mono_string_from_utf16;
	case MONO_MARSHAL_CONV_LPSTR_STR:
		return mono_string_new_wrapper;
	case MONO_MARSHAL_CONV_STR_LPTSTR:
	case MONO_MARSHAL_CONV_STR_LPSTR:
		return mono_string_to_utf8;
	case MONO_MARSHAL_CONV_STR_BSTR:
		return mono_string_to_bstr;
	case MONO_MARSHAL_CONV_STR_TBSTR:
	case MONO_MARSHAL_CONV_STR_ANSIBSTR:
		return mono_string_to_ansibstr;
	case MONO_MARSHAL_CONV_SB_LPSTR:
	case MONO_MARSHAL_CONV_SB_LPTSTR:
		return mono_string_builder_to_utf8;
	case MONO_MARSHAL_CONV_SB_LPWSTR:
		return mono_string_builder_to_utf16;
	case MONO_MARSHAL_CONV_ARRAY_SAVEARRAY:
		return mono_array_to_savearray;
	case MONO_MARSHAL_CONV_ARRAY_LPARRAY:
		return mono_array_to_lparray;
	case MONO_MARSHAL_CONV_DEL_FTN:
		return mono_delegate_to_ftnptr;
	case MONO_MARSHAL_CONV_FTN_DEL:
		return mono_ftnptr_to_delegate;
	case MONO_MARSHAL_CONV_LPSTR_SB:
	case MONO_MARSHAL_CONV_LPTSTR_SB:
		return mono_string_utf8_to_builder;
	case MONO_MARSHAL_CONV_LPWSTR_SB:
		return mono_string_utf16_to_builder;
	case MONO_MARSHAL_FREE_ARRAY:
		return mono_marshal_free_array;
	case MONO_MARSHAL_CONV_STR_BYVALSTR:
		return mono_string_to_byvalstr;
	case MONO_MARSHAL_CONV_STR_BYVALWSTR:
		return mono_string_to_byvalwstr;
	default:
		g_assert_not_reached ();
	}

	return NULL;
}

static void
emit_str_to_ptr_conv (MonoMethodBuilder *mb, MonoType *type, MonoMarshalConv conv, int usize, int msize,
		      MonoMarshalSpec *mspec)
{
	int pos;

	switch (conv) {
	case MONO_MARSHAL_CONV_BOOL_I4:
		mono_mb_emit_byte (mb, CEE_LDLOC_1);
		mono_mb_emit_byte (mb, CEE_LDLOC_0);
		mono_mb_emit_byte (mb, CEE_LDIND_U1);
		mono_mb_emit_byte (mb, CEE_STIND_I4);
		break;
	case MONO_MARSHAL_CONV_BOOL_VARIANTBOOL:
		mono_mb_emit_byte (mb, CEE_LDLOC_1);
		mono_mb_emit_byte (mb, CEE_LDLOC_0);
		mono_mb_emit_byte (mb, CEE_LDIND_U1);
		mono_mb_emit_byte (mb, CEE_NEG);
		mono_mb_emit_byte (mb, CEE_STIND_I2);
		break;
	case MONO_MARSHAL_CONV_STR_LPWSTR:
	case MONO_MARSHAL_CONV_STR_LPSTR:
	case MONO_MARSHAL_CONV_STR_LPTSTR:
	case MONO_MARSHAL_CONV_STR_BSTR:
	case MONO_MARSHAL_CONV_STR_ANSIBSTR:
	case MONO_MARSHAL_CONV_STR_TBSTR: {
		int pos;

		/* free space if free == true */
		mono_mb_emit_byte (mb, CEE_LDLOC_2);
		mono_mb_emit_byte (mb, CEE_BRFALSE_S);
		pos = mb->pos;
		mono_mb_emit_byte (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDLOC_1);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icall (mb, g_free);
		mono_mb_patch_addr_s (mb, pos, mb->pos - pos - 1);

		mono_mb_emit_byte (mb, CEE_LDLOC_1);
		mono_mb_emit_byte (mb, CEE_LDLOC_0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icall (mb, conv_to_icall (conv));
		mono_mb_emit_byte (mb, CEE_STIND_I);	
		break;
	}
	case MONO_MARSHAL_CONV_ARRAY_SAVEARRAY:
	case MONO_MARSHAL_CONV_ARRAY_LPARRAY:
	case MONO_MARSHAL_CONV_DEL_FTN:
		mono_mb_emit_byte (mb, CEE_LDLOC_1);
		mono_mb_emit_byte (mb, CEE_LDLOC_0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icall (mb, conv_to_icall (conv));
		mono_mb_emit_byte (mb, CEE_STIND_I);	
		break;
	case MONO_MARSHAL_CONV_STR_BYVALSTR: 
	case MONO_MARSHAL_CONV_STR_BYVALWSTR: {
		if (!usize)
			break;

		mono_mb_emit_byte (mb, CEE_LDLOC_1); /* dst */
		mono_mb_emit_byte (mb, CEE_LDLOC_0);	
		mono_mb_emit_byte (mb, CEE_LDIND_I); /* src String */
		mono_mb_emit_icon (mb, usize);
		mono_mb_emit_icall (mb, conv_to_icall (conv));
		break;
	}
	case MONO_MARSHAL_CONV_ARRAY_BYVALARRAY: {
		MonoClass *eclass = NULL;
		int esize;

		if (type->type == MONO_TYPE_SZARRAY) {
			eclass = type->data.klass;
		} else {
			g_assert_not_reached ();
		}

		if (eclass->valuetype)
			esize = mono_class_native_size (eclass, NULL);
		else
			esize = sizeof (gpointer);

		if (!usize) 
			break;

		mono_mb_emit_byte (mb, CEE_LDLOC_0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);		
		mono_mb_emit_byte (mb, CEE_BRFALSE_S);
		pos = mb->pos;
		mono_mb_emit_byte (mb, 0);

		mono_mb_emit_byte (mb, CEE_LDLOC_1);
		mono_mb_emit_byte (mb, CEE_LDLOC_0);	
		mono_mb_emit_byte (mb, CEE_LDIND_I);	
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_OBJADDR);
		mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoArray, vector));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_icon (mb, mspec->data.array_data.num_elem * esize);
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_CPBLK);			
		mono_mb_patch_addr_s (mb, pos, mb->pos - pos - 1);
		break;
	}
	case MONO_MARSHAL_CONV_OBJECT_STRUCT: {
		int src_var, dst_var;

		src_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		dst_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		
		mono_mb_emit_byte (mb, CEE_LDLOC_0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);		
		mono_mb_emit_byte (mb, CEE_BRFALSE_S);
		pos = mb->pos;
		mono_mb_emit_byte (mb, 0);
		
		/* save the old src pointer */
		mono_mb_emit_byte (mb, CEE_LDLOC_0);
		mono_mb_emit_stloc (mb, src_var);
		/* save the old dst pointer */
		mono_mb_emit_byte (mb, CEE_LDLOC_1);
		mono_mb_emit_stloc (mb, dst_var);

		/* src = pointer to object data */
		mono_mb_emit_byte (mb, CEE_LDLOC_0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);		
		mono_mb_emit_icon (mb, sizeof (MonoObject));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_STLOC_0); 

		emit_struct_conv (mb, mono_class_from_mono_type (type), FALSE);
		
		/* restore the old src pointer */
		mono_mb_emit_ldloc (mb, src_var);
		mono_mb_emit_byte (mb, CEE_STLOC_0);
		/* restore the old dst pointer */
		mono_mb_emit_ldloc (mb, dst_var);
		mono_mb_emit_byte (mb, CEE_STLOC_1);

		mono_mb_patch_addr_s (mb, pos, mb->pos - pos - 1);
		break;
	}
	default: {
		char *msg = g_strdup_printf ("marshalling conversion %d not implemented", conv);
		MonoException *exc = mono_get_exception_not_implemented (msg);
		g_warning (msg);
		g_free (msg);
		mono_raise_exception (exc);
	}
	}
}

static void
emit_struct_conv (MonoMethodBuilder *mb, MonoClass *klass, gboolean to_object)
{
	MonoMarshalType *info;
	int i;

	if (klass->parent)
		emit_struct_conv(mb, klass->parent, to_object);

	info = mono_marshal_load_type_info (klass);

	if (info->native_size == 0)
		return;

	if (klass->blittable) {
		int msize = mono_class_value_size (klass, NULL);
		g_assert (msize == info->native_size);
		mono_mb_emit_byte (mb, CEE_LDLOC_1);
		mono_mb_emit_byte (mb, CEE_LDLOC_0);
		mono_mb_emit_icon (mb, msize);
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_CPBLK);

		mono_mb_emit_add_to_local (mb, 0, msize);
		mono_mb_emit_add_to_local (mb, 1, msize);
		return;
	}

	for (i = 0; i < info->num_fields; i++) {
		MonoMarshalNative ntype;
		MonoMarshalConv conv;
		MonoType *ftype = info->fields [i].field->type;
		int msize = 0;
		int usize = 0;
		gboolean last_field = i < (info->num_fields -1) ? 0 : 1;

		if (ftype->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;

		ntype = mono_type_to_unmanaged (ftype, info->fields [i].mspec, TRUE, klass->unicode, &conv);
			
		if (last_field) {
			msize = klass->instance_size - info->fields [i].field->offset;
			usize = info->native_size - info->fields [i].offset;
		} else {
			msize = info->fields [i + 1].field->offset - info->fields [i].field->offset;
			usize = info->fields [i + 1].offset - info->fields [i].offset;
		}
		if ((msize < 0) || (usize < 0))
			/* This happens with GC aware auto layout */
			g_error ("Type %s which is passed to unmanaged code must have a StructLayout attribute", mono_type_full_name (&klass->byval_arg));

		g_assert ((msize >= 0) && (usize >= 0));

		switch (conv) {
		case MONO_MARSHAL_CONV_NONE: {
			int t;

			if (ftype->byref || ftype->type == MONO_TYPE_I ||
			    ftype->type == MONO_TYPE_U) {
				mono_mb_emit_byte (mb, CEE_LDLOC_1);
				mono_mb_emit_byte (mb, CEE_LDLOC_0);
				mono_mb_emit_byte (mb, CEE_LDIND_I);
				mono_mb_emit_byte (mb, CEE_STIND_I);
				break;
			}

			t = ftype->type;
		handle_enum:
			switch (t) {
			case MONO_TYPE_I4:
			case MONO_TYPE_U4:
#if SIZEOF_VOID_P == 4
			case MONO_TYPE_PTR:
#endif
				mono_mb_emit_byte (mb, CEE_LDLOC_1);
				mono_mb_emit_byte (mb, CEE_LDLOC_0);
				mono_mb_emit_byte (mb, CEE_LDIND_I4);
				mono_mb_emit_byte (mb, CEE_STIND_I4);
				break;
			case MONO_TYPE_I1:
			case MONO_TYPE_U1:
			case MONO_TYPE_BOOLEAN:
				mono_mb_emit_byte (mb, CEE_LDLOC_1);
				mono_mb_emit_byte (mb, CEE_LDLOC_0);
				mono_mb_emit_byte (mb, CEE_LDIND_I1);
				mono_mb_emit_byte (mb, CEE_STIND_I1);
				break;
			case MONO_TYPE_I2:
			case MONO_TYPE_U2:
			case MONO_TYPE_CHAR:
				mono_mb_emit_byte (mb, CEE_LDLOC_1);
				mono_mb_emit_byte (mb, CEE_LDLOC_0);
				mono_mb_emit_byte (mb, CEE_LDIND_I2);
				mono_mb_emit_byte (mb, CEE_STIND_I2);
				break;
			case MONO_TYPE_I8:
			case MONO_TYPE_U8:
#if SIZEOF_VOID_P == 8
			case MONO_TYPE_PTR:
#endif
				mono_mb_emit_byte (mb, CEE_LDLOC_1);
				mono_mb_emit_byte (mb, CEE_LDLOC_0);
				mono_mb_emit_byte (mb, CEE_LDIND_I8);
				mono_mb_emit_byte (mb, CEE_STIND_I8);
				break;
			case MONO_TYPE_R4:
				mono_mb_emit_byte (mb, CEE_LDLOC_1);
				mono_mb_emit_byte (mb, CEE_LDLOC_0);
				mono_mb_emit_byte (mb, CEE_LDIND_R4);
				mono_mb_emit_byte (mb, CEE_STIND_R4);
				break;
			case MONO_TYPE_R8:
				mono_mb_emit_byte (mb, CEE_LDLOC_1);
				mono_mb_emit_byte (mb, CEE_LDLOC_0);
				mono_mb_emit_byte (mb, CEE_LDIND_R8);
				mono_mb_emit_byte (mb, CEE_STIND_R8);
				break;
			case MONO_TYPE_VALUETYPE:
				if (ftype->data.klass->enumtype) {
					t = ftype->data.klass->enum_basetype->type;
					goto handle_enum;
				}
				emit_struct_conv (mb, ftype->data.klass, to_object);
				continue;
			default:
				g_warning ("marshaling type %02x not implemented", ftype->type);
				g_assert_not_reached ();
			}
			break;
		}
		default:
			if (to_object) 
				emit_ptr_to_str_conv (mb, ftype, conv, usize, msize, info->fields [i].mspec);
			else
				emit_str_to_ptr_conv (mb, ftype, conv, usize, msize, info->fields [i].mspec);	
		}
		
		if (to_object) {
			mono_mb_emit_add_to_local (mb, 0, usize);
			mono_mb_emit_add_to_local (mb, 1, msize);
		} else {
			mono_mb_emit_add_to_local (mb, 0, msize);
			mono_mb_emit_add_to_local (mb, 1, usize);
		}				
	}
}

static void
emit_struct_free (MonoMethodBuilder *mb, MonoClass *klass, int struct_var)
{
	/* Call DestroyStructure */
	/* FIXME: Only do this if needed */
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_LDPTR);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, klass));
	mono_mb_emit_ldloc (mb, struct_var);
	mono_mb_emit_icall (mb, mono_struct_delete_old);
}

static void
emit_thread_interrupt_checkpoint (MonoMethodBuilder *mb)
{
	static MonoMethodSignature *state_check_sig = NULL;
	int pos_noabort;
	
	if (!state_check_sig) {
		state_check_sig = mono_metadata_signature_alloc (mono_defaults.corlib, 0);
		state_check_sig->ret = &mono_defaults.void_class->byval_arg;
		state_check_sig->pinvoke = 0;
	}
	
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_LDPTR);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, (gpointer) mono_thread_interruption_request_flag ()));
	mono_mb_emit_byte (mb, CEE_LDIND_I4);
	pos_noabort = mono_mb_emit_branch (mb, CEE_BRFALSE);

	mono_mb_emit_native_call (mb, state_check_sig, mono_thread_interruption_checkpoint);
	
	mono_mb_patch_addr (mb, pos_noabort, mb->pos - (pos_noabort + 4));
}

static MonoAsyncResult *
mono_delegate_begin_invoke (MonoDelegate *delegate, gpointer *params)
{
	MonoMethodMessage *msg;
	MonoDelegate *async_callback;
	MonoObject *state;
	MonoMethod *im;
	MonoClass *klass;
	MonoMethod *method = NULL;
	int i;

	g_assert (delegate);

	if (delegate->target && mono_object_class (delegate->target) == mono_defaults.transparent_proxy_class) {

		MonoTransparentProxy* tp = (MonoTransparentProxy *)delegate->target;
		if (!tp->remote_class->proxy_class->contextbound || tp->rp->context != (MonoObject *) mono_context_get ()) {

			/* If the target is a proxy, make a direct call. Is proxy's work
			// to make the call asynchronous.
			*/
			MonoAsyncResult *ares;
			MonoObject *exc;
			MonoArray *out_args;
			HANDLE handle;
			method = delegate->method_info->method;

			msg = mono_method_call_message_new (mono_marshal_method_from_wrapper (method), params, NULL, &async_callback, &state);
			handle = CreateEvent (NULL, TRUE, FALSE, NULL);
			ares = mono_async_result_new (mono_domain_get (), handle, state, handle);
			ares->async_delegate = (MonoObject *)delegate;
			ares->async_callback = (MonoObject *)async_callback;
			msg->async_result = ares;
			msg->call_type = CallType_BeginInvoke;

			mono_remoting_invoke ((MonoObject *)tp->rp, msg, &exc, &out_args);
			return ares;
		}
	}

	klass = delegate->object.vtable->klass;

	method = mono_get_delegate_invoke (klass);
	for (i = 0; i < klass->method.count; ++i) {
		if (klass->methods [i]->name[0] == 'B' && 
		    !strcmp ("BeginInvoke", klass->methods [i]->name)) {
			method = klass->methods [i];
			break;
		}
	}

	g_assert (method != NULL);

	im = mono_get_delegate_invoke (method->klass);
	msg = mono_method_call_message_new (method, params, im, &async_callback, &state);

	return mono_thread_pool_add ((MonoObject *)delegate, msg, async_callback, state);
}

static int
mono_mb_emit_save_args (MonoMethodBuilder *mb, MonoMethodSignature *sig, gboolean save_this)
{
	int i, params_var, tmp_var;

	/* allocate local (pointer) *params[] */
	params_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	/* allocate local (pointer) tmp */
	tmp_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

	/* alloate space on stack to store an array of pointers to the arguments */
	mono_mb_emit_icon (mb, sizeof (gpointer) * (sig->param_count + 1));
	mono_mb_emit_byte (mb, CEE_PREFIX1);
	mono_mb_emit_byte (mb, CEE_LOCALLOC);
	mono_mb_emit_stloc (mb, params_var);

	/* tmp = params */
	mono_mb_emit_ldloc (mb, params_var);
	mono_mb_emit_stloc (mb, tmp_var);

	if (save_this && sig->hasthis) {
		mono_mb_emit_ldloc (mb, tmp_var);
		mono_mb_emit_ldarg_addr (mb, 0);
		mono_mb_emit_byte (mb, CEE_STIND_I);
		/* tmp = tmp + sizeof (gpointer) */
		if (sig->param_count)
			mono_mb_emit_add_to_local (mb, tmp_var, sizeof (gpointer));

	}

	for (i = 0; i < sig->param_count; i++) {
		mono_mb_emit_ldloc (mb, tmp_var);
		mono_mb_emit_ldarg_addr (mb, i + sig->hasthis);
		mono_mb_emit_byte (mb, CEE_STIND_I);
		/* tmp = tmp + sizeof (gpointer) */
		if (i < (sig->param_count - 1))
			mono_mb_emit_add_to_local (mb, tmp_var, sizeof (gpointer));
	}

	return params_var;
}

static char*
mono_signature_to_name (MonoMethodSignature *sig, const char *prefix)
{
	int i;
	char *result;
	GString *res = g_string_new ("");

	if (prefix) {
		g_string_append (res, prefix);
		g_string_append_c (res, '_');
	}

	mono_type_get_desc (res, sig->ret, FALSE);

	for (i = 0; i < sig->param_count; ++i) {
		g_string_append_c (res, '_');
		mono_type_get_desc (res, sig->params [i], FALSE);
	}
	result = res->str;
	g_string_free (res, FALSE);
	return result;
}

/**
 * mono_marshal_get_string_encoding:
 *
 *  Return the string encoding which should be used for a given parameter.
 */
static MonoMarshalNative
mono_marshal_get_string_encoding (MonoMethodPInvoke *piinfo, MonoMarshalSpec *spec)
{
	/* First try the parameter marshal info */
	if (spec) {
		if (spec->native == MONO_NATIVE_LPARRAY) {
			if (spec->data.array_data.elem_type != 0)
				return spec->data.array_data.elem_type;
		}
		else
			return spec->native;
	}

	if (!piinfo)
		return MONO_NATIVE_LPSTR;

	/* Then try the method level marshal info */
	switch (piinfo->piflags & PINVOKE_ATTRIBUTE_CHAR_SET_MASK) {
	case PINVOKE_ATTRIBUTE_CHAR_SET_ANSI:
		return MONO_NATIVE_LPSTR;
	case PINVOKE_ATTRIBUTE_CHAR_SET_UNICODE:
		return MONO_NATIVE_LPWSTR;
	case PINVOKE_ATTRIBUTE_CHAR_SET_AUTO:
		return MONO_NATIVE_LPTSTR;
	default:
		return MONO_NATIVE_LPSTR;
	}
}

static MonoMarshalConv
mono_marshal_get_string_to_ptr_conv (MonoMethodPInvoke *piinfo, MonoMarshalSpec *spec)
{
	MonoMarshalNative encoding = mono_marshal_get_string_encoding (piinfo, spec);

	switch (encoding) {
	case MONO_NATIVE_LPWSTR:
		return MONO_MARSHAL_CONV_STR_LPWSTR;
	case MONO_NATIVE_LPSTR:
		return MONO_MARSHAL_CONV_STR_LPSTR;
	case MONO_NATIVE_LPTSTR:
		return MONO_MARSHAL_CONV_STR_LPTSTR;
	default:
		return -1;
	}
}

static MonoMarshalConv
mono_marshal_get_stringbuilder_to_ptr_conv (MonoMethodPInvoke *piinfo, MonoMarshalSpec *spec)
{
	MonoMarshalNative encoding = mono_marshal_get_string_encoding (piinfo, spec);

	switch (encoding) {
	case MONO_NATIVE_LPWSTR:
		return MONO_MARSHAL_CONV_SB_LPWSTR;
		break;
	case MONO_NATIVE_LPSTR:
		return MONO_MARSHAL_CONV_SB_LPSTR;
		break;
	case MONO_NATIVE_LPTSTR:
		return MONO_MARSHAL_CONV_SB_LPTSTR;
		break;
	default:
		return -1;
	}
}

static MonoMarshalConv
mono_marshal_get_ptr_to_stringbuilder_conv (MonoMethodPInvoke *piinfo, MonoMarshalSpec *spec, gboolean *need_free)
{
	MonoMarshalNative encoding = mono_marshal_get_string_encoding (piinfo, spec);

	*need_free = TRUE;

	switch (encoding) {
	case MONO_NATIVE_LPWSTR:
		/* 
		 * mono_string_builder_to_utf16 does not allocate a 
		 * new buffer, so no need to free it.
		 */
		*need_free = FALSE;
		return MONO_MARSHAL_CONV_LPWSTR_SB;
	case MONO_NATIVE_LPSTR:
		return MONO_MARSHAL_CONV_LPSTR_SB;
		break;
	case MONO_NATIVE_LPTSTR:
		return MONO_MARSHAL_CONV_LPTSTR_SB;
		break;
	default:
		return -1;
	}
}

/*
 * Return whenever a field of a native structure or an array member needs to 
 * be freed.
 */
static gboolean
mono_marshal_need_free (MonoType *t, MonoMethodPInvoke *piinfo, MonoMarshalSpec *spec)
{
	MonoMarshalNative encoding;
	MonoMarshalConv conv;

	switch (t->type) {
	case MONO_TYPE_VALUETYPE:
		/* FIXME: Optimize this */
		return TRUE;
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_CLASS:
		if (t->data.klass == mono_defaults.stringbuilder_class) {
			gboolean need_free;
			conv = mono_marshal_get_ptr_to_stringbuilder_conv (piinfo, spec, &need_free);
			return need_free;
		}
		return FALSE;
	case MONO_TYPE_STRING:
		encoding = mono_marshal_get_string_encoding (piinfo, spec);
		return (encoding == MONO_NATIVE_LPWSTR) ? FALSE : TRUE;
	default:
		return FALSE;
	}
}

static inline MonoMethod*
mono_marshal_find_in_cache (GHashTable *cache, gpointer key)
{
	MonoMethod *res;

	EnterCriticalSection (&marshal_mutex);
	res = g_hash_table_lookup (cache, key);
	LeaveCriticalSection (&marshal_mutex);
	return res;
}

/* Create the method from the builder and place it in the cache */
static inline MonoMethod*
mono_mb_create_and_cache (GHashTable *cache, gpointer key,
							   MonoMethodBuilder *mb, MonoMethodSignature *sig,
							   int max_stack)
{
	MonoMethod *res;

	EnterCriticalSection (&marshal_mutex);
	res = g_hash_table_lookup (cache, key);
	if (!res) {
		/* This does not acquire any locks */
		res = mono_mb_create_method (mb, sig, max_stack);
		g_hash_table_insert (cache, key, res);
		mono_g_hash_table_insert (wrapper_hash, res, key);
	}
	else
		/* Somebody created it before us */
		;
	LeaveCriticalSection (&marshal_mutex);

	return res;
}		

MonoMethod *
mono_marshal_method_from_wrapper (MonoMethod *wrapper)
{
	MonoMethod *res;

	if (wrapper->wrapper_type == MONO_WRAPPER_NONE)
		return wrapper;

	EnterCriticalSection (&marshal_mutex);
	res = mono_g_hash_table_lookup (wrapper_hash, wrapper);
	LeaveCriticalSection (&marshal_mutex);

	if (res && wrapper->wrapper_type == MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK)
		/* See mono_marshal_get_remoting_invoke_with_check */
		return (MonoMethod*)((char*)res - 1);
	else
		return res;
}

MonoMethod *
mono_marshal_get_delegate_begin_invoke (MonoMethod *method)
{
	MonoMethodSignature *sig;
	static MonoMethodSignature *csig = NULL;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	GHashTable *cache;
	int params_var;
	char *name;

	g_assert (method && method->klass->parent == mono_defaults.multicastdelegate_class &&
		  !strcmp (method->name, "BeginInvoke"));

	sig = method->signature;

	cache = method->klass->image->delegate_begin_invoke_cache;
	if ((res = mono_marshal_find_in_cache (cache, sig)))
		return res;

	g_assert (sig->hasthis);

	if (!csig) {
		csig = mono_metadata_signature_alloc (method->klass->image, 2);

		/* MonoAsyncResult * begin_invoke (MonoDelegate *delegate, gpointer params[]) */
		csig->ret = &mono_defaults.object_class->byval_arg;
		csig->params [0] = &mono_defaults.object_class->byval_arg;
		csig->params [1] = &mono_defaults.int_class->byval_arg;
	}

	name = mono_signature_to_name (sig, "begin_invoke");
	mb = mono_mb_new (mono_defaults.multicastdelegate_class, name, MONO_WRAPPER_DELEGATE_BEGIN_INVOKE);
	g_free (name);

	mb->method->save_lmf = 1;

	params_var = mono_mb_emit_save_args (mb, sig, FALSE);

	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldloc (mb, params_var);
	mono_mb_emit_native_call (mb, csig, mono_delegate_begin_invoke);
	emit_thread_interrupt_checkpoint (mb);
	mono_mb_emit_byte (mb, CEE_RET);

	res = mono_mb_create_and_cache (cache, sig, mb, sig, sig->param_count + 16);
	mono_mb_free (mb);
	return res;
}

static MonoObject *
mono_delegate_end_invoke (MonoDelegate *delegate, gpointer *params)
{
	MonoDomain *domain = mono_domain_get ();
	MonoAsyncResult *ares;
	MonoMethod *method = NULL;
	MonoMethodSignature *sig;
	MonoMethodMessage *msg;
	MonoObject *res, *exc;
	MonoArray *out_args;
	MonoClass *klass;
	int i;

	g_assert (delegate);

	if (!delegate->method_info || !delegate->method_info->method)
		g_assert_not_reached ();

	klass = delegate->object.vtable->klass;

	for (i = 0; i < klass->method.count; ++i) {
		if (klass->methods [i]->name[0] == 'E' && 
		    !strcmp ("EndInvoke", klass->methods [i]->name)) {
			method = klass->methods [i];
			break;
		}
	}

	g_assert (method != NULL);

	sig = method->signature;

	msg = mono_method_call_message_new (method, params, NULL, NULL, NULL);

	ares = mono_array_get (msg->args, gpointer, sig->param_count - 1);
	g_assert (ares);

	if (delegate->target && mono_object_class (delegate->target) == mono_defaults.transparent_proxy_class) {
		MonoTransparentProxy* tp = (MonoTransparentProxy *)delegate->target;
		msg = (MonoMethodMessage *)mono_object_new (domain, mono_defaults.mono_method_message_class);
		mono_message_init (domain, msg, delegate->method_info, NULL);
		msg->call_type = CallType_EndInvoke;
		msg->async_result = ares;
		res = mono_remoting_invoke ((MonoObject *)tp->rp, msg, &exc, &out_args);
	}
	else
		res = mono_thread_pool_finish (ares, &out_args, &exc);

	if (exc) {
		if (((MonoException*)exc)->stack_trace) {
			char *strace = mono_string_to_utf8 (((MonoException*)exc)->stack_trace);
			char  *tmp;
			tmp = g_strdup_printf ("%s\nException Rethrown at:\n", strace);
			g_free (strace);	
			((MonoException*)exc)->stack_trace = mono_string_new (domain, tmp);
			g_free (tmp);
		}
		mono_raise_exception ((MonoException*)exc);
	}

	mono_method_return_message_restore (method, params, out_args);
	return res;
}

static void
mono_mb_emit_restore_result (MonoMethodBuilder *mb, MonoType *return_type)
{
	if (return_type->byref)
		return_type = &mono_defaults.int_class->byval_arg;
	else if (return_type->type == MONO_TYPE_VALUETYPE && return_type->data.klass->enumtype)
		return_type = return_type->data.klass->enum_basetype;

	switch (return_type->type) {
	case MONO_TYPE_VOID:
		g_assert_not_reached ();
		break;
	case MONO_TYPE_PTR:
	case MONO_TYPE_STRING:
	case MONO_TYPE_CLASS: 
	case MONO_TYPE_OBJECT: 
	case MONO_TYPE_ARRAY: 
	case MONO_TYPE_SZARRAY: 
		/* nothing to do */
		break;
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
		mono_mb_emit_byte (mb, CEE_UNBOX);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_class_from_mono_type (return_type)));
		mono_mb_emit_byte (mb, CEE_LDIND_U1);
		break;
	case MONO_TYPE_I1:
		mono_mb_emit_byte (mb, CEE_UNBOX);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_class_from_mono_type (return_type)));
		mono_mb_emit_byte (mb, CEE_LDIND_I1);
		break;
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
		mono_mb_emit_byte (mb, CEE_UNBOX);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_class_from_mono_type (return_type)));
		mono_mb_emit_byte (mb, CEE_LDIND_U2);
		break;
	case MONO_TYPE_I2:
		mono_mb_emit_byte (mb, CEE_UNBOX);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_class_from_mono_type (return_type)));
		mono_mb_emit_byte (mb, CEE_LDIND_I2);
		break;
	case MONO_TYPE_I:
	case MONO_TYPE_U:
		mono_mb_emit_byte (mb, CEE_UNBOX);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_class_from_mono_type (return_type)));
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		break;
	case MONO_TYPE_I4:
		mono_mb_emit_byte (mb, CEE_UNBOX);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_class_from_mono_type (return_type)));
		mono_mb_emit_byte (mb, CEE_LDIND_I4);
		break;
	case MONO_TYPE_U4:
		mono_mb_emit_byte (mb, CEE_UNBOX);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_class_from_mono_type (return_type)));
		mono_mb_emit_byte (mb, CEE_LDIND_U4);
		break;
	case MONO_TYPE_U8:
	case MONO_TYPE_I8:
		mono_mb_emit_byte (mb, CEE_UNBOX);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_class_from_mono_type (return_type)));
		mono_mb_emit_byte (mb, CEE_LDIND_I8);
		break;
	case MONO_TYPE_R4:
		mono_mb_emit_byte (mb, CEE_UNBOX);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_class_from_mono_type (return_type)));
		mono_mb_emit_byte (mb, CEE_LDIND_R4);
		break;
	case MONO_TYPE_R8:
		mono_mb_emit_byte (mb, CEE_UNBOX);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_class_from_mono_type (return_type)));
		mono_mb_emit_byte (mb, CEE_LDIND_R8);
		break;
	case MONO_TYPE_VALUETYPE: {
		int class;
		mono_mb_emit_byte (mb, CEE_UNBOX);
		class = mono_mb_add_data (mb, mono_class_from_mono_type (return_type));
		mono_mb_emit_i4 (mb, class);
		mono_mb_emit_byte (mb, CEE_LDOBJ);
		mono_mb_emit_i4 (mb, class);
		break;
	}
	default:
		g_warning ("type 0x%x not handled", return_type->type);
		g_assert_not_reached ();
	}

	mono_mb_emit_byte (mb, CEE_RET);
}

MonoMethod *
mono_marshal_get_delegate_end_invoke (MonoMethod *method)
{
	MonoMethodSignature *sig;
	static MonoMethodSignature *csig = NULL;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	GHashTable *cache;
	int params_var;
	char *name;

	g_assert (method && method->klass->parent == mono_defaults.multicastdelegate_class &&
		  !strcmp (method->name, "EndInvoke"));

	sig = method->signature;

	cache = method->klass->image->delegate_end_invoke_cache;
	if ((res = mono_marshal_find_in_cache (cache, sig)))
		return res;

	g_assert (sig->hasthis);

	if (!csig) {
		csig = mono_metadata_signature_alloc (method->klass->image, 2);

		/* MonoObject *end_invoke (MonoDelegate *delegate, gpointer params[]) */
		csig->ret = &mono_defaults.object_class->byval_arg;
		csig->params [0] = &mono_defaults.object_class->byval_arg;
		csig->params [1] = &mono_defaults.int_class->byval_arg;
	}

	name = mono_signature_to_name (sig, "end_invoke");
	mb = mono_mb_new (mono_defaults.multicastdelegate_class, name, MONO_WRAPPER_DELEGATE_END_INVOKE);
	g_free (name);

	mb->method->save_lmf = 1;

	params_var = mono_mb_emit_save_args (mb, sig, FALSE);

	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldloc (mb, params_var);
	mono_mb_emit_native_call (mb, csig, mono_delegate_end_invoke);
	emit_thread_interrupt_checkpoint (mb);

	if (sig->ret->type == MONO_TYPE_VOID) {
		mono_mb_emit_byte (mb, CEE_POP);
		mono_mb_emit_byte (mb, CEE_RET);
	} else
		mono_mb_emit_restore_result (mb, sig->ret);

	res = mono_mb_create_and_cache (cache, sig,
										 mb, sig, sig->param_count + 16);
	mono_mb_free (mb);

	return res;
}

static MonoObject *
mono_remoting_wrapper (MonoMethod *method, gpointer *params)
{
	MonoMethodMessage *msg;
	MonoTransparentProxy *this;
	MonoObject *res, *exc;
	MonoArray *out_args;

	this = *((MonoTransparentProxy **)params [0]);

	g_assert (this);
	g_assert (((MonoObject *)this)->vtable->klass == mono_defaults.transparent_proxy_class);
	
	/* skip the this pointer */
	params++;

	if (this->remote_class->proxy_class->contextbound && this->rp->context == (MonoObject *) mono_context_get ())
	{
		int i;
		MonoMethodSignature *sig = method->signature;
		int count = sig->param_count;
		gpointer* mparams = (gpointer*) alloca(count*sizeof(gpointer));

		for (i=0; i<count; i++) {
			MonoClass *class = mono_class_from_mono_type (sig->params [i]);
			if (class->valuetype) {
				if (sig->params [i]->byref)
					mparams[i] = *((gpointer *)params [i]);
				else 
					mparams[i] = params [i];
			} else {
				mparams[i] = *((gpointer**)params [i]);
			}
		}

		return mono_runtime_invoke (method, method->klass->valuetype? mono_object_unbox ((MonoObject*)this): this, mparams, NULL);
	}

	msg = mono_method_call_message_new (method, params, NULL, NULL, NULL);

	res = mono_remoting_invoke ((MonoObject *)this->rp, msg, &exc, &out_args);

	if (exc)
		mono_raise_exception ((MonoException *)exc);

	mono_method_return_message_restore (method, params, out_args);

	return res;
} 

MonoMethod *
mono_marshal_get_remoting_invoke (MonoMethod *method)
{
	MonoMethodSignature *sig;
	static MonoMethodSignature *csig = NULL;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	GHashTable *cache;
	int params_var;

	g_assert (method);

	if (method->wrapper_type == MONO_WRAPPER_REMOTING_INVOKE)
		return method;

	sig = method->signature;

	/* we cant remote methods without this pointer */
	if (!sig->hasthis)
		return method;

	cache = method->klass->image->remoting_invoke_cache;
	if ((res = mono_marshal_find_in_cache (cache, method)))
		return res;

	if (!csig) {
		csig = mono_metadata_signature_alloc (mono_defaults.corlib, 2);
		csig->params [0] = &mono_defaults.int_class->byval_arg;
		csig->params [1] = &mono_defaults.int_class->byval_arg;
		csig->ret = &mono_defaults.object_class->byval_arg;
		csig->pinvoke = 1;
	}

	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_REMOTING_INVOKE);
	mb->method->save_lmf = 1;

	params_var = mono_mb_emit_save_args (mb, sig, TRUE);

	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_LDPTR);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, method));
	mono_mb_emit_ldloc (mb, params_var);
	mono_mb_emit_native_call (mb, csig, mono_remoting_wrapper);
	emit_thread_interrupt_checkpoint (mb);

	if (sig->ret->type == MONO_TYPE_VOID) {
		mono_mb_emit_byte (mb, CEE_POP);
		mono_mb_emit_byte (mb, CEE_RET);
	} else {
		 mono_mb_emit_restore_result (mb, sig->ret);
	}

	res = mono_mb_create_and_cache (cache, method, mb, sig, sig->param_count + 16);
	mono_mb_free (mb);

	return res;
}

MonoMethod *
mono_marshal_get_remoting_invoke_with_check (MonoMethod *method)
{
	MonoMethodSignature *sig;
	MonoMethodBuilder *mb;
	MonoMethod *res, *native;
	GHashTable *cache;
	int i, pos;

	g_assert (method);

	if (method->wrapper_type == MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK)
		return method;

	sig = method->signature;

	/* we cant remote methods without this pointer */
	g_assert (sig->hasthis);

	cache = method->klass->image->remoting_invoke_cache;
	if ((res = mono_marshal_find_in_cache (cache, (char *)method + 1)))
		return res;

	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK);

	mono_mb_emit_ldarg (mb, 0);
	pos = mono_mb_emit_proxy_check (mb, CEE_BNE_UN);

	native = mono_marshal_get_remoting_invoke (method);

	for (i = 0; i <= sig->param_count; i++)
		mono_mb_emit_ldarg (mb, i);
	
	mono_mb_emit_managed_call (mb, native, native->signature);
	mono_mb_emit_byte (mb, CEE_RET);

	mono_mb_patch_addr (mb, pos, mb->pos - (pos + 4));

	for (i = 0; i <= sig->param_count; i++)
		mono_mb_emit_ldarg (mb, i);
		
	mono_mb_emit_managed_call (mb, method, method->signature);
	mono_mb_emit_byte (mb, CEE_RET);

	res = mono_mb_create_and_cache (cache, (char*)method + 1,
										 mb, sig, sig->param_count + 16);
	mono_mb_free (mb);

	return res;
}

/*
 * the returned method invokes all methods in a multicast delegate 
 */
MonoMethod *
mono_marshal_get_delegate_invoke (MonoMethod *method)
{
	MonoMethodSignature *sig, *static_sig;
	int i;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	GHashTable *cache;
	int pos0, pos1;
	char *name;

	g_assert (method && method->klass->parent == mono_defaults.multicastdelegate_class &&
		  !strcmp (method->name, "Invoke"));
		
	sig = method->signature;

	cache = method->klass->image->delegate_invoke_cache;
	if ((res = mono_marshal_find_in_cache (cache, sig)))
		return res;

	static_sig = mono_metadata_signature_dup (sig);
	static_sig->hasthis = 0;

	name = mono_signature_to_name (sig, "invoke");
	mb = mono_mb_new (mono_defaults.multicastdelegate_class, name,  MONO_WRAPPER_DELEGATE_INVOKE);
	g_free (name);

	/* allocate local 0 (object) */
	mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);

	g_assert (sig->hasthis);
	
	/*
	 * if (prev != null)
         *	prev.Invoke( args .. );
	 * return this.<target>( args .. );
         */
	
	/* this wrapper can be used in unmanaged-managed transitions */
	emit_thread_interrupt_checkpoint (mb);
	
	/* get this->prev */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoMulticastDelegate, prev));
	mono_mb_emit_byte (mb, CEE_LDIND_I );
	mono_mb_emit_stloc (mb, 0);

	/* if prev != null */
	mono_mb_emit_ldloc (mb, 0);
	mono_mb_emit_byte (mb, CEE_BRFALSE);

	pos0 = mb->pos;
	mono_mb_emit_i4 (mb, 0);

	/* then recurse */
	mono_mb_emit_ldloc (mb, 0);
	for (i = 0; i < sig->param_count; i++)
		mono_mb_emit_ldarg (mb, i + 1);
	mono_mb_emit_managed_call (mb, method, method->signature);
	if (sig->ret->type != MONO_TYPE_VOID)
		mono_mb_emit_byte (mb, CEE_POP);

	/* continued or prev == null */
	mono_mb_patch_addr (mb, pos0, mb->pos - (pos0 + 4));

	/* get this->target */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoDelegate, target));
	mono_mb_emit_byte (mb, CEE_LDIND_I );
	mono_mb_emit_stloc (mb, 0);

	/* if target != null */
	mono_mb_emit_ldloc (mb, 0);
	mono_mb_emit_byte (mb, CEE_BRFALSE);
	pos0 = mb->pos;
	mono_mb_emit_i4 (mb, 0);
	
	/* then call this->method_ptr nonstatic */
	mono_mb_emit_ldloc (mb, 0); 
	for (i = 0; i < sig->param_count; ++i)
		mono_mb_emit_ldarg (mb, i + 1);
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoDelegate, method_ptr));
	mono_mb_emit_byte (mb, CEE_LDIND_I );
	mono_mb_emit_byte (mb, CEE_CALLI);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, sig));

	mono_mb_emit_byte (mb, CEE_BR);
	pos1 = mb->pos;
	mono_mb_emit_i4 (mb, 0);

	/* else [target == null] call this->method_ptr static */
	mono_mb_patch_addr (mb, pos0, mb->pos - (pos0 + 4));

	for (i = 0; i < sig->param_count; ++i)
		mono_mb_emit_ldarg (mb, i + 1);
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoDelegate, method_ptr));
	mono_mb_emit_byte (mb, CEE_LDIND_I );
	mono_mb_emit_byte (mb, CEE_CALLI);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, static_sig));

	/* return */
	mono_mb_patch_addr (mb, pos1, mb->pos - (pos1 + 4));
	mono_mb_emit_byte (mb, CEE_RET);

	res = mono_mb_create_and_cache (cache, sig,
										 mb, sig, sig->param_count + 16);
	mono_mb_free (mb);

	return res;	
}

/*
 * signature_dup_add_this:
 *
 *  Make a copy of @sig, adding an explicit this argument.
 */
static MonoMethodSignature*
signature_dup_add_this (MonoMethodSignature *sig, MonoClass *klass)
{
	MonoMethodSignature *res;
	int i;

	res = mono_metadata_signature_alloc (klass->image, sig->param_count + 1);
	memcpy (res, sig, sizeof (MonoMethodSignature));
	res->param_count = sig->param_count + 1;
	res->hasthis = FALSE;
	for (i = sig->param_count - 1; i >= 0; i --)
		res->params [i + 1] = sig->params [i];
	res->params [0] = &mono_ptr_class_get (&klass->byval_arg)->byval_arg;

	return res;
}

typedef struct {
	MonoMethod *ctor;
	MonoMethodSignature *sig;
} CtorSigPair;

/*
 * generates IL code for the runtime invoke function 
 * MonoObject *runtime_invoke (MonoObject *this, void **params, MonoObject **exc, void* method)
 *
 * we also catch exceptions if exc != null
 */
MonoMethod *
mono_marshal_get_runtime_invoke (MonoMethod *method)
{
	MonoMethodSignature *sig, *csig, *callsig;
	MonoExceptionClause *clause;
	MonoMethodHeader *header;
	MonoMethodBuilder *mb;
	GHashTable *cache = NULL;
	MonoClass *target_klass;
	MonoMethod *res = NULL;
	GSList *item;
	static MonoString *string_dummy = NULL;
	int i, pos;
	char *name;

	g_assert (method);

	target_klass = method->klass;
	
	EnterCriticalSection (&marshal_mutex);

	if (method->string_ctor) {
		static GSList *strsig_list = NULL;
		CtorSigPair *cs;

		callsig = NULL;
		for (item = strsig_list; item; item = item->next) {
			cs = item->data;
			if (mono_metadata_signature_equal (method->signature, cs->ctor->signature)) {
				callsig = cs->sig;
				break;
			}
		}
		if (!callsig) {
			callsig = mono_metadata_signature_dup (method->signature);
			callsig->ret = &mono_defaults.string_class->byval_arg;
			cs = g_new (CtorSigPair, 1);
			cs->sig = callsig;
			cs->ctor = method;
			strsig_list = g_slist_prepend (strsig_list, cs);
		}
	} else {
		if (method->klass->valuetype && method->signature->hasthis) {
			/* 
			 * Valuetype methods receive a managed pointer as the this argument.
			 * Create a new signature to reflect this.
			 */
			callsig = signature_dup_add_this (method->signature, method->klass);
		}
		else
			callsig = method->signature;
	}

	cache = method->klass->image->runtime_invoke_cache;

	/* from mono_marshal_find_in_cache */
	res = g_hash_table_lookup (cache, callsig);
	LeaveCriticalSection (&marshal_mutex);

	if (res) {
		return res;
	}

	target_klass = mono_defaults.object_class;

	/* to make it work with our special string constructors */
	if (!string_dummy) {
		MONO_GC_REGISTER_ROOT (string_dummy);
		string_dummy = mono_string_new_wrapper ("dummy");
	}
	
	sig = method->signature;

	csig = mono_metadata_signature_alloc (method->klass->image, 4);

	csig->ret = &mono_defaults.object_class->byval_arg;
	if (method->klass->valuetype && method->signature->hasthis)
		csig->params [0] = callsig->params [0];
	else
		csig->params [0] = &mono_defaults.object_class->byval_arg;
	csig->params [1] = &mono_defaults.int_class->byval_arg;
	csig->params [2] = &mono_defaults.int_class->byval_arg;
	csig->params [3] = &mono_defaults.int_class->byval_arg;

	name = mono_signature_to_name (callsig, "runtime_invoke");
	mb = mono_mb_new (target_klass, name,  MONO_WRAPPER_RUNTIME_INVOKE);
	g_free (name);

	/* allocate local 0 (object) tmp */
	mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);
	/* allocate local 1 (object) exc */
	mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);

	/* cond set *exc to null */
	mono_mb_emit_byte (mb, CEE_LDARG_2);
	mono_mb_emit_byte (mb, CEE_BRFALSE_S);
	mono_mb_emit_byte (mb, 3);	
	mono_mb_emit_byte (mb, CEE_LDARG_2);
	mono_mb_emit_byte (mb, CEE_LDNULL);
	mono_mb_emit_byte (mb, CEE_STIND_I);

	if (sig->hasthis) {
		if (method->string_ctor) {
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_byte (mb, CEE_MONO_LDPTR);
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, string_dummy));
		} else {
			mono_mb_emit_ldarg (mb, 0);
		}
	}

	for (i = 0; i < sig->param_count; i++) {
		MonoType *t = sig->params [i];
		int type;

		mono_mb_emit_ldarg (mb, 1);
		if (i) {
			mono_mb_emit_icon (mb, sizeof (gpointer) * i);
			mono_mb_emit_byte (mb, CEE_ADD);
		}
		mono_mb_emit_byte (mb, CEE_LDIND_I);

		if (t->byref)
			continue;

		type = sig->params [i]->type;
handle_enum:
		switch (type) {
		case MONO_TYPE_I1:
			mono_mb_emit_byte (mb, CEE_LDIND_I1);
			break;
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_U1:
			mono_mb_emit_byte (mb, CEE_LDIND_U1);
			break;
		case MONO_TYPE_I2:
			mono_mb_emit_byte (mb, CEE_LDIND_I2);
			break;
		case MONO_TYPE_U2:
		case MONO_TYPE_CHAR:
			mono_mb_emit_byte (mb, CEE_LDIND_U2);
			break;
		case MONO_TYPE_I:
		case MONO_TYPE_U:
			mono_mb_emit_byte (mb, CEE_LDIND_I);
			break;
		case MONO_TYPE_I4:
			mono_mb_emit_byte (mb, CEE_LDIND_I4);
			break;
		case MONO_TYPE_U4:
			mono_mb_emit_byte (mb, CEE_LDIND_U4);
			break;
		case MONO_TYPE_R4:
			mono_mb_emit_byte (mb, CEE_LDIND_R4);
			break;
		case MONO_TYPE_R8:
			mono_mb_emit_byte (mb, CEE_LDIND_R8);
			break;
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
			mono_mb_emit_byte (mb, CEE_LDIND_I8);
			break;
		case MONO_TYPE_STRING:
		case MONO_TYPE_CLASS:  
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_PTR:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_OBJECT:
			/* do nothing */
			break;
		case MONO_TYPE_VALUETYPE:
			if (t->data.klass->enumtype) {
				type = t->data.klass->enum_basetype->type;
				goto handle_enum;
			}
			mono_mb_emit_byte (mb, CEE_LDOBJ);
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, t->data.klass));
			break;
		default:
			g_assert_not_reached ();
		}		
	}
	
	mono_mb_emit_ldarg (mb, 3);
	mono_mb_emit_calli (mb, callsig);

	if (sig->ret->byref) {
		/* fixme: */
		g_assert_not_reached ();
	}


	switch (sig->ret->type) {
	case MONO_TYPE_VOID:
		if (!method->string_ctor)
			mono_mb_emit_byte (mb, CEE_LDNULL);
		break;
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_VALUETYPE:
		/* box value types */
		mono_mb_emit_byte (mb, CEE_BOX);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_class_from_mono_type (sig->ret)));
		break;
	case MONO_TYPE_STRING:
	case MONO_TYPE_CLASS:  
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_OBJECT:
		/* nothing to do */
		break;
	case MONO_TYPE_PTR:
	default:
		g_assert_not_reached ();
	}

	mono_mb_emit_stloc (mb, 0);
       		
	mono_mb_emit_byte (mb, CEE_LEAVE);
	pos = mb->pos;
	mono_mb_emit_i4 (mb, 0);

	clause = g_new0 (MonoExceptionClause, 1);
	clause->flags = MONO_EXCEPTION_CLAUSE_FILTER;
	clause->try_len = mb->pos;

	/* filter code */
	clause->token_or_filter = mb->pos;
	
	mono_mb_emit_byte (mb, CEE_POP);
	mono_mb_emit_byte (mb, CEE_LDARG_2);
	mono_mb_emit_byte (mb, CEE_LDC_I4_0);
	mono_mb_emit_byte (mb, CEE_PREFIX1);
	mono_mb_emit_byte (mb, CEE_CGT_UN);
	mono_mb_emit_byte (mb, CEE_PREFIX1);
	mono_mb_emit_byte (mb, CEE_ENDFILTER);

	clause->handler_offset = mb->pos;

	/* handler code */
	/* store exception */
	mono_mb_emit_stloc (mb, 1);
	
	mono_mb_emit_byte (mb, CEE_LDARG_2);
	mono_mb_emit_ldloc (mb, 1);
	mono_mb_emit_byte (mb, CEE_STIND_I);

	mono_mb_emit_byte (mb, CEE_LDC_I4_0);
	mono_mb_emit_stloc (mb, 0);

	mono_mb_emit_byte (mb, CEE_LEAVE);
	mono_mb_emit_i4 (mb, 0);

	clause->handler_len = mb->pos - clause->handler_offset;

	/* return result */
	mono_mb_patch_addr (mb, pos, mb->pos - (pos + 4));
	mono_mb_emit_ldloc (mb, 0);
	mono_mb_emit_byte (mb, CEE_RET);

	/* taken from mono_mb_create_and_cache */
	EnterCriticalSection (&marshal_mutex);

	res = g_hash_table_lookup (cache, callsig);
	/* Somebody may have created it before us */
	if (!res) {
		res = mono_mb_create_method (mb, csig, sig->param_count + 16);
		g_hash_table_insert (cache, callsig, res);
		mono_g_hash_table_insert (wrapper_hash, res, callsig);
	}

	LeaveCriticalSection (&marshal_mutex);
	/* end mono_mb_create_and_cache */

	mono_mb_free (mb);

	header = ((MonoMethodNormal *)res)->header;
	header->num_clauses = 1;
	header->clauses = clause;

	return res;	
}

static void
raise_auto_layout_exception (MonoClass *klass)
{
	char *msg = g_strdup_printf ("The type `%s.%s' layout needs to be Sequential or Explicit",
				     klass->name_space, klass->name, NULL);
				
	MonoException *e = mono_exception_from_name_msg (
		mono_get_corlib (), "System.Runtime.InteropServices",
		"MarshalDirectiveException", msg);
	g_free (msg);
	mono_raise_exception (e);
}
			     
/*
 * generates IL code to call managed methods from unmanaged code 
 */
MonoMethod *
mono_marshal_get_managed_wrapper (MonoMethod *method, MonoObject *this, MonoMarshalSpec **mspecs)
{
	MonoMethodSignature *sig, *csig;
	MonoMethodBuilder *mb;
	MonoClass *klass = NULL;
	MonoMethod *res;
	GHashTable *cache;
	int i, pos = 0, *tmp_locals;
	static MonoMethodSignature *alloc_sig = NULL;
	int retobj_var = 0;

	g_assert (method != NULL);
	g_assert (!method->signature->pinvoke);

	cache = method->klass->image->managed_wrapper_cache;
	if (!this && (res = mono_marshal_find_in_cache (cache, method)))
		return res;

	/* Under MS, the allocation should be done using CoTaskMemAlloc */
	if (!alloc_sig) {
		alloc_sig = mono_metadata_signature_alloc (mono_defaults.corlib, 1);
		alloc_sig->params [0] = &mono_defaults.int_class->byval_arg;
		alloc_sig->ret = &mono_defaults.int_class->byval_arg;
		alloc_sig->pinvoke = 1;
	}

	sig = method->signature;

	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_NATIVE_TO_MANAGED);

	/* allocate local 0 (pointer) src_ptr */
	mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	/* allocate local 1 (pointer) dst_ptr */
	mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	/* allocate local 2 (boolean) delete_old */
	mono_mb_add_local (mb, &mono_defaults.boolean_class->byval_arg);

	if (!MONO_TYPE_IS_VOID(sig->ret)) {
		/* allocate local 3 to store the return value */
		mono_mb_add_local (mb, sig->ret);
	}

	mono_mb_emit_icon (mb, 0);
	mono_mb_emit_byte (mb, CEE_STLOC_2);

	/* we copy the signature, so that we can modify it */
	csig = mono_metadata_signature_dup (sig);
	csig->hasthis = 0;
	csig->pinvoke = 1;

#ifdef PLATFORM_WIN32
	/* 
	 * Under windows, delegates passed to native code must use the STDCALL
	 * calling convention.
	 */
	csig->call_convention = MONO_CALL_STDCALL;
#endif

	/* fixme: howto handle this ? */
	if (sig->hasthis) {

		if (this) {
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_byte (mb, CEE_MONO_LDPTR);
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, this));


		} else {
			/* fixme: */
			g_assert_not_reached ();
		}
	} 


	/* we first do all conversions */
	tmp_locals = alloca (sizeof (int) * sig->param_count);
	for (i = 0; i < sig->param_count; i ++) {
		MonoType *t = sig->params [i];
		MonoMarshalSpec *spec = mspecs [i + 1];

		tmp_locals [i] = 0;
		
		/* Ensure that we have marshalling info for this param */
		mono_marshal_load_type_info (mono_class_from_mono_type (t));

		if (spec && spec->native == MONO_NATIVE_CUSTOM) {
			MonoType *mtype;
			MonoClass *mklass;
			MonoMethod *marshal_native_to_managed;
			MonoMethod *get_instance;

			mtype = mono_reflection_type_from_name (spec->data.custom_data.custom_name, method->klass->image);
			g_assert (mtype != NULL);
			mklass = mono_class_from_mono_type (mtype);
			g_assert (mklass != NULL);

			marshal_native_to_managed = mono_find_method_by_name (mklass, "MarshalNativeToManaged", 1);
			g_assert (marshal_native_to_managed);
			get_instance = mono_find_method_by_name (mklass, "GetInstance", 1);
			g_assert (get_instance);
			
			switch (t->type) {
			case MONO_TYPE_CLASS:
			case MONO_TYPE_OBJECT:
			case MONO_TYPE_STRING:
			case MONO_TYPE_ARRAY:
			case MONO_TYPE_SZARRAY:
				if (t->byref)
					break;

				tmp_locals [i] = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);

				mono_mb_emit_ldstr (mb, g_strdup (spec->data.custom_data.cookie));

				mono_mb_emit_byte (mb, CEE_CALL);
				mono_mb_emit_i4 (mb, mono_mb_add_data (mb, get_instance));
				
				mono_mb_emit_ldarg (mb, i);
				
				mono_mb_emit_byte (mb, CEE_CALLVIRT);
				mono_mb_emit_i4 (mb, mono_mb_add_data (mb, marshal_native_to_managed));
				
				mono_mb_emit_stloc (mb, tmp_locals [i]);
				break;
			default:
				g_warning ("custom marshalling of type %x is currently not supported", t->type);
				g_assert_not_reached ();
				break;
			}
			continue;
		}

		switch (t->type) {
		case MONO_TYPE_CLASS: {
			klass = t->data.klass;

			tmp_locals [i] = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

			if (klass->delegate) {
				g_assert (!t->byref);
				mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
				mono_mb_emit_byte (mb, CEE_MONO_LDPTR);
				mono_mb_emit_i4 (mb, mono_mb_add_data (mb, klass));
				mono_mb_emit_ldarg (mb, i);
				mono_mb_emit_icall (mb, conv_to_icall (MONO_MARSHAL_CONV_FTN_DEL));
				mono_mb_emit_stloc (mb, tmp_locals [i]);
				break;
			}

			/* The class can not have an automatic layout */
			if ((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_AUTO_LAYOUT)
				raise_auto_layout_exception (klass);

			if (t->attrs & PARAM_ATTRIBUTE_OUT) {
				mono_mb_emit_byte (mb, CEE_LDNULL);
				mono_mb_emit_stloc (mb, tmp_locals [i]);
				break;
			}

			/* Set src */
			mono_mb_emit_ldarg (mb, i);
			if (t->byref) {
				int pos2;

				/* Check for NULL and raise an exception */
				mono_mb_emit_byte (mb, CEE_BRTRUE);
				pos2 = mb->pos;
				mono_mb_emit_i4 (mb, 0);

				mono_mb_emit_exception (mb, "ArgumentNullException", NULL);

				mono_mb_patch_addr (mb, pos2, mb->pos - (pos2 + 4));
				mono_mb_emit_ldarg (mb, i);
				mono_mb_emit_byte (mb, CEE_LDIND_I);
			}				

			mono_mb_emit_byte (mb, CEE_STLOC_0);

			mono_mb_emit_byte (mb, CEE_LDC_I4_0);
			mono_mb_emit_stloc (mb, tmp_locals [i]);

			mono_mb_emit_byte (mb, CEE_LDLOC_0);
			mono_mb_emit_byte (mb, CEE_BRFALSE);
			pos = mb->pos;
			mono_mb_emit_i4 (mb, 0);

			/* Create and set dst */
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_byte (mb, CEE_MONO_NEWOBJ);	
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, klass));
			mono_mb_emit_stloc (mb, tmp_locals [i]);
			mono_mb_emit_ldloc (mb, tmp_locals [i]);
			mono_mb_emit_icon (mb, sizeof (MonoObject));
			mono_mb_emit_byte (mb, CEE_ADD);
			mono_mb_emit_byte (mb, CEE_STLOC_1); 

			/* emit valuetype conversion code */
			emit_struct_conv (mb, klass, TRUE);

			mono_mb_patch_addr (mb, pos, mb->pos - (pos + 4));
			break;
		}
		case MONO_TYPE_VALUETYPE:
			
			klass = sig->params [i]->data.klass;
			if (((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) ||
			    klass->blittable || klass->enumtype)
				break;

			tmp_locals [i] = mono_mb_add_local (mb, &klass->byval_arg);

			if (t->attrs & PARAM_ATTRIBUTE_OUT)
				break;

			if (t->byref) 
				mono_mb_emit_ldarg (mb, i);
			else
				mono_mb_emit_ldarg_addr (mb, i);
			mono_mb_emit_byte (mb, CEE_STLOC_0);

			if (t->byref) {
				mono_mb_emit_byte (mb, CEE_LDLOC_0);
				mono_mb_emit_byte (mb, CEE_BRFALSE);
				pos = mb->pos;
				mono_mb_emit_i4 (mb, 0);
			}			

			mono_mb_emit_ldloc_addr (mb, tmp_locals [i]);
			mono_mb_emit_byte (mb, CEE_STLOC_1);

			/* emit valuetype conversion code */
			emit_struct_conv (mb, klass, TRUE);

			if (t->byref)
				mono_mb_patch_addr (mb, pos, mb->pos - (pos + 4));
			break;
		case MONO_TYPE_STRING: {
			MonoMarshalNative encoding = mono_marshal_get_string_encoding (NULL, spec);

			if (t->byref)
				continue;

			tmp_locals [i] = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);
			csig->params [i] = &mono_defaults.int_class->byval_arg;

			mono_mb_emit_ldarg (mb, i);

			switch (encoding) {
			case MONO_NATIVE_LPWSTR:
				mono_mb_emit_icall (mb, conv_to_icall (MONO_MARSHAL_CONV_LPWSTR_STR));
				break;
			case MONO_NATIVE_LPSTR:
				mono_mb_emit_icall (mb, conv_to_icall (MONO_MARSHAL_CONV_LPSTR_STR));
				break;
			default: {
					char *msg = g_strdup_printf ("string marshalling conversion %d not implemented", encoding);
					MonoException *exc = mono_get_exception_not_implemented (msg);
					g_warning (msg);
					g_free (msg);
					mono_raise_exception (exc);
				}
			}

			mono_mb_emit_stloc (mb, tmp_locals [i]);
			break;	
		}
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
			if (t->byref)
				continue;

			klass = mono_class_from_mono_type (t);

			tmp_locals [i] = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);
			csig->params [i] = &mono_defaults.int_class->byval_arg;

			g_warning ("array marshaling not implemented");
			g_assert_not_reached ();
			break;
		}
	}

	for (i = 0; i < sig->param_count; i++) {
		MonoType *t = sig->params [i];

		switch (t->type) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_PTR:
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
			mono_mb_emit_ldarg (mb, i);
			break;
		case MONO_TYPE_STRING:
			if (t->byref) {
				mono_mb_emit_ldarg (mb, i);
			} else {
				g_assert (tmp_locals [i]);
				mono_mb_emit_ldloc (mb, tmp_locals [i]);
			}
			break;	
		case MONO_TYPE_CLASS:  
			if (t->byref)
				mono_mb_emit_ldloc_addr (mb, tmp_locals [i]);
			else
				mono_mb_emit_ldloc (mb, tmp_locals [i]);
			break;
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_OBJECT:
			if (tmp_locals [i])
				mono_mb_emit_ldloc (mb, tmp_locals [i]);
			else
				mono_mb_emit_ldarg (mb, i);
			break;
		case MONO_TYPE_VALUETYPE:
			klass = sig->params [i]->data.klass;
			if (((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) ||
			    klass->blittable || klass->enumtype) {
				mono_mb_emit_ldarg (mb, i);
				break;
			}

			g_assert (tmp_locals [i]);
			if (t->byref)
				mono_mb_emit_ldloc_addr (mb, tmp_locals [i]);
			else
				mono_mb_emit_ldloc (mb, tmp_locals [i]);
			break;
		default:
			g_warning ("type 0x%02x unknown", t->type);	
			g_assert_not_reached ();
		}
	}

	emit_thread_interrupt_checkpoint (mb);
	mono_mb_emit_managed_call (mb, method, NULL);

	/* Ensure that we have marshalling info for the return */
	mono_marshal_load_type_info (mono_class_from_mono_type (sig->ret));

	if (mspecs [0] && mspecs [0]->native == MONO_NATIVE_CUSTOM) {
		MonoMarshalSpec *spec = mspecs [0];
		MonoType *mtype;
		MonoClass *mklass;
		guint32 loc1;

		g_assert (!sig->ret->byref);

		mtype = mono_reflection_type_from_name (spec->data.custom_data.custom_name, method->klass->image);
		g_assert (mtype);
		mklass = mono_class_from_mono_type (mtype);
		g_assert (mklass);

		loc1 = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);
			
		switch (sig->ret->type) {
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_STRING:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
			mono_mb_emit_byte (mb, CEE_STLOC_3);
			
			mono_mb_emit_byte (mb, CEE_LDLOC_3);
			mono_mb_emit_stloc (mb, loc1);

			mono_mb_emit_ldstr (mb, g_strdup (spec->data.custom_data.cookie));
			mono_mb_emit_byte (mb, CEE_CALL);
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_find_method_by_name (mklass, "GetInstance", 1)));
			mono_mb_emit_byte (mb, CEE_DUP);

			mono_mb_emit_byte (mb, CEE_LDLOC_3);
			mono_mb_emit_byte (mb, CEE_CALLVIRT);
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_find_method_by_name (mklass, "MarshalManagedToNative", 1)));
			mono_mb_emit_byte (mb, CEE_STLOC_3);

			mono_mb_emit_ldloc (mb, loc1);
			mono_mb_emit_byte (mb, CEE_CALLVIRT);
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_find_method_by_name (mklass, "CleanUpManagedData", 1)));

			break;
		default:
			g_warning ("custom marshalling of type %x is currently not supported", sig->ret->type);
			g_assert_not_reached ();
			break;
		}
	}
	else
	if (!sig->ret->byref) { 
		switch (sig->ret->type) {
		case MONO_TYPE_VOID:
			break;
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_PTR:
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
		case MONO_TYPE_OBJECT:
			mono_mb_emit_byte (mb, CEE_STLOC_3);
			break;
		case MONO_TYPE_STRING:		
			csig->ret = &mono_defaults.int_class->byval_arg;

			mono_mb_emit_icall (mb, conv_to_icall (MONO_MARSHAL_CONV_STR_LPSTR));
			mono_mb_emit_byte (mb, CEE_STLOC_3);
			break;
		case MONO_TYPE_VALUETYPE:
			klass = sig->ret->data.klass;
			if (((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) ||
			    klass->blittable || klass->enumtype)
				break;
			
			/* load pointer to returned value type */
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_byte (mb, CEE_MONO_VTADDR);
			
			/* store the address of the source into local variable 0 */
			mono_mb_emit_byte (mb, CEE_STLOC_0);
			/* allocate space for the native struct and
			 * store the address into dst_ptr */
			retobj_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
			g_assert (retobj_var);
			mono_mb_emit_icon (mb, mono_class_native_size (klass, NULL));
			mono_mb_emit_byte (mb, CEE_CONV_I);
			mono_mb_emit_native_call (mb, alloc_sig, mono_marshal_alloc);
			emit_thread_interrupt_checkpoint (mb);
			mono_mb_emit_byte (mb, CEE_STLOC_1);
			mono_mb_emit_byte (mb, CEE_LDLOC_1);
			mono_mb_emit_stloc (mb, retobj_var);

			/* emit valuetype conversion code */
			emit_struct_conv (mb, klass, FALSE);
			break;
		case MONO_TYPE_CLASS: {
			int pos2;

			klass = sig->ret->data.klass;

			if (klass->delegate) {
				mono_mb_emit_icall (mb, conv_to_icall (MONO_MARSHAL_CONV_DEL_FTN));
				mono_mb_emit_byte (mb, CEE_STLOC_3);
				break;
			}

			/* The class can not have an automatic layout */
			if ((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_AUTO_LAYOUT)
				raise_auto_layout_exception (klass);

			mono_mb_emit_byte (mb, CEE_STLOC_0);
			/* Check for null */
			mono_mb_emit_byte (mb, CEE_LDLOC_0);
			pos = mono_mb_emit_branch (mb, CEE_BRTRUE);
			mono_mb_emit_byte (mb, CEE_LDNULL);
			mono_mb_emit_byte (mb, CEE_STLOC_3);
			pos2 = mono_mb_emit_branch (mb, CEE_BR);

			mono_mb_patch_addr (mb, pos, mb->pos - (pos + 4));

			/* Set src */
			mono_mb_emit_byte (mb, CEE_LDLOC_0);
			mono_mb_emit_icon (mb, sizeof (MonoObject));
			mono_mb_emit_byte (mb, CEE_ADD);
			mono_mb_emit_byte (mb, CEE_STLOC_0);

			/* Allocate and set dest */
			mono_mb_emit_icon (mb, mono_class_native_size (klass, NULL));
			mono_mb_emit_byte (mb, CEE_CONV_I);
			mono_mb_emit_native_call (mb, alloc_sig, mono_marshal_alloc);
			emit_thread_interrupt_checkpoint (mb);
			mono_mb_emit_byte (mb, CEE_DUP);
			mono_mb_emit_byte (mb, CEE_STLOC_1);
			mono_mb_emit_byte (mb, CEE_STLOC_3);

			emit_struct_conv (mb, klass, FALSE);

			mono_mb_patch_addr (mb, pos2, mb->pos - (pos2 + 4));
			break;
		}
		default:
			g_warning ("return type 0x%02x unknown", sig->ret->type);	
			g_assert_not_reached ();
		}
	} else {
		mono_mb_emit_byte (mb, CEE_STLOC_3);
	}

	/* Convert byref arguments back */
	for (i = 0; i < sig->param_count; i ++) {
		MonoType *t = sig->params [i];
		MonoMarshalSpec *spec = mspecs [i + 1];

		if (spec && spec->native == MONO_NATIVE_CUSTOM) {
			MonoType *mtype;
			MonoClass *mklass;
			MonoMethod *cleanup_managed;
			MonoMethod *get_instance;

			mtype = mono_reflection_type_from_name (spec->data.custom_data.custom_name, method->klass->image);
			g_assert (mtype != NULL);
			mklass = mono_class_from_mono_type (mtype);
			g_assert (mklass != NULL);

			get_instance = mono_find_method_by_name (mklass, "GetInstance", 1);
			g_assert (get_instance);
			cleanup_managed = mono_find_method_by_name (mklass, "CleanUpManagedData", 1);
			g_assert (cleanup_managed);

			switch (t->type) {
			case MONO_TYPE_CLASS:
			case MONO_TYPE_OBJECT:
			case MONO_TYPE_STRING:
			case MONO_TYPE_ARRAY:
			case MONO_TYPE_SZARRAY:
				if (t->byref)
					g_assert_not_reached ();

				/* Call CleanUpManagedData */

				mono_mb_emit_ldstr (mb, g_strdup (spec->data.custom_data.cookie));

				mono_mb_emit_byte (mb, CEE_CALL);
				mono_mb_emit_i4 (mb, mono_mb_add_data (mb, get_instance));
				
				mono_mb_emit_ldloc (mb, tmp_locals [i]);
				mono_mb_emit_byte (mb, CEE_CALLVIRT);
				mono_mb_emit_i4 (mb, mono_mb_add_data (mb, cleanup_managed));
				
				break;
			default:
				g_warning ("custom marshalling of type %x is currently not supported", t->type);
				g_assert_not_reached ();
				break;
			}
			continue;
		}

		if (!t->byref)
			continue;

		switch (t->type) {
		case MONO_TYPE_CLASS: {
			int pos2;

			klass = t->data.klass;

			/* Check for null */
			mono_mb_emit_ldloc (mb, tmp_locals [i]);
			pos = mono_mb_emit_branch (mb, CEE_BRTRUE);
			mono_mb_emit_ldarg (mb, i);
			mono_mb_emit_byte (mb, CEE_LDC_I4_0);
			mono_mb_emit_byte (mb, CEE_STIND_I);
			pos2 = mono_mb_emit_branch (mb, CEE_BR);

			mono_mb_patch_addr (mb, pos, mb->pos - (pos + 4));			
			
			/* Set src */
			mono_mb_emit_ldloc (mb, tmp_locals [i]);
			mono_mb_emit_icon (mb, sizeof (MonoObject));
			mono_mb_emit_byte (mb, CEE_ADD);
			mono_mb_emit_byte (mb, CEE_STLOC_0);

			/* Allocate and set dest */
			mono_mb_emit_icon (mb, mono_class_native_size (klass, NULL));
			mono_mb_emit_byte (mb, CEE_CONV_I);
			mono_mb_emit_native_call (mb, alloc_sig, mono_marshal_alloc);
			emit_thread_interrupt_checkpoint (mb);
			mono_mb_emit_byte (mb, CEE_STLOC_1);

			/* Update argument pointer */
			mono_mb_emit_ldarg (mb, i);
			mono_mb_emit_byte (mb, CEE_LDLOC_1);
			mono_mb_emit_byte (mb, CEE_STIND_I);

			/* emit valuetype conversion code */
			emit_struct_conv (mb, klass, FALSE);

			mono_mb_patch_addr (mb, pos2, mb->pos - (pos2 + 4));
			break;
		}
		case MONO_TYPE_VALUETYPE: {
			int pos2;

			klass = t->data.klass;

			if (((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) ||
			    klass->blittable || klass->enumtype) {
				break;
			}

			/* Check for null */
			mono_mb_emit_ldarg (mb, i);
			pos2 = mono_mb_emit_branch (mb, CEE_BRFALSE);

			/* Set src */
			mono_mb_emit_ldloc_addr (mb, tmp_locals [i]);
			mono_mb_emit_byte (mb, CEE_STLOC_0);

			/* Set dest */
			mono_mb_emit_ldarg (mb, i);
			mono_mb_emit_byte (mb, CEE_STLOC_1);

			/* emit valuetype conversion code */
			emit_struct_conv (mb, klass, FALSE);

			mono_mb_patch_addr (mb, pos2, mb->pos - (pos2 + 4));
			break;
		}
		}
	}

	if (retobj_var) {
		mono_mb_emit_ldloc (mb, retobj_var);
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_RETOBJ);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, klass));
	}
	else {
		if (!MONO_TYPE_IS_VOID(sig->ret))
			mono_mb_emit_byte (mb, CEE_LDLOC_3);
		mono_mb_emit_byte (mb, CEE_RET);
	}

	if (!this)
		res = mono_mb_create_and_cache (cache, method,
											 mb, csig, sig->param_count + 16);
	else {
		res = mono_mb_create_method (mb, csig, sig->param_count + 16);
		res->dynamic = 1;
	}
	mono_mb_free (mb);

	/* printf ("CODE FOR %s: \n%s.\n", mono_method_full_name (res, TRUE), mono_disasm_code (0, res, ((MonoMethodNormal*)res)->header->code, ((MonoMethodNormal*)res)->header->code + ((MonoMethodNormal*)res)->header->code_size)); */

	return res;
}

/*
 * mono_marshal_get_ldfld_wrapper:
 * @type: the type of the field
 *
 * This method generates a function which can be use to load a field with type
 * @type from an object. The generated function has the following signature:
 * <@type> ldfld_wrapper (MonoObject *this, MonoClass *class, MonoClassField *field, int offset)
 */
MonoMethod *
mono_marshal_get_ldfld_wrapper (MonoType *type)
{
	MonoMethodSignature *sig, *csig;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	MonoClass *klass;
	static GHashTable *ldfld_hash = NULL; 
	char *name;
	int t, pos0, pos1 = 0;

	t = type->type;

	if (!type->byref) {
		if (type->type == MONO_TYPE_SZARRAY) {
			klass = mono_defaults.array_class;
		} else if (type->type == MONO_TYPE_VALUETYPE) {
			klass = type->data.klass;
			if (klass->enumtype) {
				t = klass->enum_basetype->type;
				klass = mono_class_from_mono_type (klass->enum_basetype);
			}
		} else if (t == MONO_TYPE_OBJECT || t == MONO_TYPE_CLASS || t == MONO_TYPE_STRING ||
			   t == MONO_TYPE_CLASS) { 
			klass = mono_defaults.object_class;
		} else if (t == MONO_TYPE_PTR || t == MONO_TYPE_FNPTR) {
			klass = mono_defaults.int_class;
		} else {
			klass = mono_class_from_mono_type (type);			
		}
	} else {
		klass = mono_defaults.int_class;
	}

	EnterCriticalSection (&marshal_mutex);
	if (!ldfld_hash) 
		ldfld_hash = g_hash_table_new (NULL, NULL);
	res = g_hash_table_lookup (ldfld_hash, klass);
	LeaveCriticalSection (&marshal_mutex);
	if (res)
		return res;

	name = g_strdup_printf ("__ldfld_wrapper_%s.%s", klass->name_space, klass->name); 
	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_LDFLD);
	g_free (name);

	mb->method->save_lmf = 1;

	sig = mono_metadata_signature_alloc (mono_defaults.corlib, 4);
	sig->params [0] = &mono_defaults.object_class->byval_arg;
	sig->params [1] = &mono_defaults.int_class->byval_arg;
	sig->params [2] = &mono_defaults.int_class->byval_arg;
	sig->params [3] = &mono_defaults.int_class->byval_arg;
	sig->ret = &klass->byval_arg;

	mono_mb_emit_ldarg (mb, 0);
	pos0 = mono_mb_emit_proxy_check (mb, CEE_BNE_UN);

	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldarg (mb, 1);
	mono_mb_emit_ldarg (mb, 2);

	csig = mono_metadata_signature_alloc (mono_defaults.corlib, 3);
	csig->params [0] = &mono_defaults.object_class->byval_arg;
	csig->params [1] = &mono_defaults.int_class->byval_arg;
	csig->params [2] = &mono_defaults.int_class->byval_arg;
	csig->ret = &klass->this_arg;
	csig->pinvoke = 1;

	mono_mb_emit_native_call (mb, csig, mono_load_remote_field_new);
	emit_thread_interrupt_checkpoint (mb);

	if (klass->valuetype) {
		mono_mb_emit_byte (mb, CEE_UNBOX);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, klass));		
		mono_mb_emit_byte (mb, CEE_BR);
		pos1 = mb->pos;
		mono_mb_emit_i4 (mb, 0);
	} else {
		mono_mb_emit_byte (mb, CEE_RET);
	}


	mono_mb_patch_addr (mb, pos0, mb->pos - (pos0 + 4));

	mono_mb_emit_ldarg (mb, 0);
        mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
        mono_mb_emit_byte (mb, CEE_MONO_OBJADDR);
	mono_mb_emit_ldarg (mb, 3);
	mono_mb_emit_byte (mb, CEE_ADD);

	if (klass->valuetype)
		mono_mb_patch_addr (mb, pos1, mb->pos - (pos1 + 4));

	switch (t) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
		mono_mb_emit_byte (mb, CEE_LDIND_I1);
		break;
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
		mono_mb_emit_byte (mb, CEE_LDIND_I2);
		break;
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		mono_mb_emit_byte (mb, CEE_LDIND_I4);
		break;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		mono_mb_emit_byte (mb, CEE_LDIND_I8);
		break;
	case MONO_TYPE_R4:
		mono_mb_emit_byte (mb, CEE_LDIND_R4);
		break;
	case MONO_TYPE_R8:
		mono_mb_emit_byte (mb, CEE_LDIND_R8);
		break;
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_STRING:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		break;
	case MONO_TYPE_VALUETYPE:
		g_assert (!klass->enumtype);
		mono_mb_emit_byte (mb, CEE_LDOBJ);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, klass));
		break;
	default:
		g_warning ("type %x not implemented", type->type);
		g_assert_not_reached ();
	}

	mono_mb_emit_byte (mb, CEE_RET);
       
	res = mono_mb_create_and_cache (ldfld_hash, klass,
										 mb, sig, sig->param_count + 16);
	mono_mb_free (mb);
	
	return res;
}

/*
 * mono_marshal_get_stfld_wrapper:
 * @type: the type of the field
 *
 * This method generates a function which can be use to store a field with type
 * @type. The generated function has the following signature:
 * void stfld_wrapper (MonoObject *this, MonoClass *class, MonoClassField *field, int offset, <@type> val)
 */
MonoMethod *
mono_marshal_get_stfld_wrapper (MonoType *type)
{
	MonoMethodSignature *sig, *csig;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	MonoClass *klass;
	static GHashTable *stfld_hash = NULL; 
	char *name;
	int t, pos;

	t = type->type;

	if (!type->byref) {
		if (type->type == MONO_TYPE_SZARRAY) {
			klass = mono_defaults.array_class;
		} else if (type->type == MONO_TYPE_VALUETYPE) {
			klass = type->data.klass;
			if (klass->enumtype) {
				t = klass->enum_basetype->type;
				klass = mono_class_from_mono_type (klass->enum_basetype);
			}
		} else if (t == MONO_TYPE_OBJECT || t == MONO_TYPE_CLASS || t == MONO_TYPE_STRING ||
			   t == MONO_TYPE_CLASS) { 
			klass = mono_defaults.object_class;
		} else if (t == MONO_TYPE_PTR || t == MONO_TYPE_FNPTR) {
			klass = mono_defaults.int_class;
		} else {
			klass = mono_class_from_mono_type (type);			
		}
	} else {
		klass = mono_defaults.int_class;
	}

	EnterCriticalSection (&marshal_mutex);
	if (!stfld_hash) 
		stfld_hash = g_hash_table_new (NULL, NULL);
	res = g_hash_table_lookup (stfld_hash, klass);
	LeaveCriticalSection (&marshal_mutex);
	if (res)
		return res;

	name = g_strdup_printf ("__stfld_wrapper_%s.%s", klass->name_space, klass->name); 
	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_STFLD);
	g_free (name);

	mb->method->save_lmf = 1;

	sig = mono_metadata_signature_alloc (mono_defaults.corlib, 5);
	sig->params [0] = &mono_defaults.object_class->byval_arg;
	sig->params [1] = &mono_defaults.int_class->byval_arg;
	sig->params [2] = &mono_defaults.int_class->byval_arg;
	sig->params [3] = &mono_defaults.int_class->byval_arg;
	sig->params [4] = &klass->byval_arg;
	sig->ret = &mono_defaults.void_class->byval_arg;

	mono_mb_emit_ldarg (mb, 0);
	pos = mono_mb_emit_proxy_check (mb, CEE_BNE_UN);

	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldarg (mb, 1);
	mono_mb_emit_ldarg (mb, 2);
	mono_mb_emit_ldarg (mb, 4);

	if (klass->valuetype) {
		mono_mb_emit_byte (mb, CEE_BOX);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, klass));		
	}

	csig = mono_metadata_signature_alloc (mono_defaults.corlib, 4);
	csig->params [0] = &mono_defaults.object_class->byval_arg;
	csig->params [1] = &mono_defaults.int_class->byval_arg;
	csig->params [2] = &mono_defaults.int_class->byval_arg;
	csig->params [3] = &klass->this_arg;
	csig->ret = &mono_defaults.void_class->byval_arg;
	csig->pinvoke = 1;

	mono_mb_emit_native_call (mb, csig, mono_store_remote_field_new);
	emit_thread_interrupt_checkpoint (mb);

	mono_mb_emit_byte (mb, CEE_RET);

	mono_mb_patch_addr (mb, pos, mb->pos - (pos + 4));

	mono_mb_emit_ldarg (mb, 0);
        mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
        mono_mb_emit_byte (mb, CEE_MONO_OBJADDR);
	mono_mb_emit_ldarg (mb, 3);
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_ldarg (mb, 4);

	switch (t) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
		mono_mb_emit_byte (mb, CEE_STIND_I1);
		break;
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
		mono_mb_emit_byte (mb, CEE_STIND_I2);
		break;
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		mono_mb_emit_byte (mb, CEE_STIND_I4);
		break;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		mono_mb_emit_byte (mb, CEE_STIND_I8);
		break;
	case MONO_TYPE_R4:
		mono_mb_emit_byte (mb, CEE_STIND_R4);
		break;
	case MONO_TYPE_R8:
		mono_mb_emit_byte (mb, CEE_STIND_R8);
		break;
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_STRING:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
		mono_mb_emit_byte (mb, CEE_STIND_I);
		break;
	case MONO_TYPE_VALUETYPE:
		g_assert (!klass->enumtype);
		mono_mb_emit_byte (mb, CEE_STOBJ);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, klass));
		break;
	default:
		g_warning ("type %x not implemented", type->type);
		g_assert_not_reached ();
	}

	mono_mb_emit_byte (mb, CEE_RET);
       
	res = mono_mb_create_and_cache (stfld_hash, klass,
									mb, sig, sig->param_count + 16);
	mono_mb_free (mb);
	
	return res;
}

/*
 * generates IL code for the icall wrapper (the generated method
 * calls the unmanaged code in func)
 */
MonoMethod *
mono_marshal_get_icall_wrapper (MonoMethodSignature *sig, const char *name, gconstpointer func)
{
	MonoMethodSignature *csig;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	int i;
	
	g_assert (sig->pinvoke);

	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_MANAGED_TO_NATIVE);

	mb->method->save_lmf = 1;

	/* we copy the signature, so that we can modify it */

	if (sig->hasthis)
		mono_mb_emit_byte (mb, CEE_LDARG_0);

	for (i = 0; i < sig->param_count; i++)
		mono_mb_emit_ldarg (mb, i + sig->hasthis);

	mono_mb_emit_native_call (mb, sig, (gpointer) func);
	emit_thread_interrupt_checkpoint (mb);
	mono_mb_emit_byte (mb, CEE_RET);

	csig = mono_metadata_signature_dup (sig);
	csig->pinvoke = 0;
	if (csig->call_convention == MONO_CALL_VARARG)
		csig->call_convention = 0;

	res = mono_mb_create_method (mb, csig, csig->param_count + 16);
	mono_mb_free (mb);
	
	return res;
}

typedef struct {
	MonoMethodBuilder *mb;
	MonoMethodPInvoke *piinfo;
	int *orig_conv_args; /* Locals containing the original values of byref args */
} EmitMarshalContext;

typedef enum {
	MARSHAL_ACTION_CONV_IN,
	MARSHAL_ACTION_PUSH,
	MARSHAL_ACTION_CONV_OUT,
	MARSHAL_ACTION_CONV_RESULT
} MarshalAction;

static int
emit_marshal_custom (EmitMarshalContext *m, int argnum, MonoType *t,
					 MonoMarshalSpec *spec, 
					 int conv_arg, MonoType **conv_arg_type, 
					 MarshalAction action)
{
	MonoType *mtype;
	MonoClass *mklass;
	MonoMethod *get_instance, *cleanup_native;
	MonoMethod *marshal_managed_to_native, *marshal_native_to_managed;
	MonoMethodBuilder *mb = m->mb;
	guint32 loc1;

	mtype = mono_reflection_type_from_name (spec->data.custom_data.custom_name, mb->method->klass->image);
	g_assert (mtype != NULL);
	mklass = mono_class_from_mono_type (mtype);
	g_assert (mklass != NULL);

	get_instance = mono_find_method_by_name (mklass, "GetInstance", 1);
	g_assert (get_instance);
	cleanup_native = mono_find_method_by_name (mklass, "CleanUpNativeData", 1);
	g_assert (cleanup_native);
	marshal_managed_to_native = mono_find_method_by_name (mklass, "MarshalManagedToNative", 1);
	g_assert (marshal_managed_to_native);
	marshal_native_to_managed = mono_find_method_by_name (mklass, "MarshalNativeToManaged", 1);
	g_assert (marshal_native_to_managed);

	switch (action) {
	case MARSHAL_ACTION_CONV_IN:
		switch (t->type) {
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_STRING:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_VALUETYPE:
			if (t->byref)
				break;

			conv_arg = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

			mono_mb_emit_ldstr (mb, g_strdup (spec->data.custom_data.cookie));

			mono_mb_emit_byte (mb, CEE_CALL);
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, get_instance));
				
			mono_mb_emit_ldarg (mb, argnum);

			if (t->type == MONO_TYPE_VALUETYPE) {
				/*
				 * Since we can't determine the type of the argument, we
				 * will assume the unmanaged function takes a pointer.
				 */
				*conv_arg_type = &mono_defaults.int_class->byval_arg;

				mono_mb_emit_byte (mb, CEE_BOX);
				mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_class_from_mono_type (t)));
			}

			mono_mb_emit_byte (mb, CEE_CALLVIRT);
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, marshal_managed_to_native));
			mono_mb_emit_stloc (mb, conv_arg);
			break;

		default:
			g_warning ("custom marshalling of type %x is currently not supported", t->type);
			g_assert_not_reached ();
			break;
		}
		
		break;

	case MARSHAL_ACTION_CONV_OUT:
		switch (t->type) {
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_STRING:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_VALUETYPE:
			mono_mb_emit_ldstr (mb, g_strdup (spec->data.custom_data.cookie));
			
			mono_mb_emit_byte (mb, CEE_CALL);
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, get_instance));
				
			mono_mb_emit_ldloc (mb, conv_arg);

			mono_mb_emit_byte (mb, CEE_CALLVIRT);
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, cleanup_native));

			break;

		default:
			g_warning ("custom marshalling of type %x is currently not supported", t->type);
			g_assert_not_reached ();
			break;
		}
		break;

	case MARSHAL_ACTION_PUSH:
		mono_mb_emit_ldloc (mb, conv_arg);
		break;

	case MARSHAL_ACTION_CONV_RESULT:
		loc1 = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
			
		switch (t->type) {
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_STRING:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
			mono_mb_emit_byte (mb, CEE_STLOC_3);

			mono_mb_emit_byte (mb, CEE_LDLOC_3);
			mono_mb_emit_stloc (mb, loc1);

			mono_mb_emit_ldstr (mb, g_strdup (spec->data.custom_data.cookie));

			mono_mb_emit_byte (mb, CEE_CALL);
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, get_instance));
			mono_mb_emit_byte (mb, CEE_DUP);

			mono_mb_emit_byte (mb, CEE_LDLOC_3);
			mono_mb_emit_byte (mb, CEE_CALLVIRT);
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, marshal_native_to_managed));
			mono_mb_emit_byte (mb, CEE_STLOC_3);

			mono_mb_emit_ldloc (mb, loc1);
			mono_mb_emit_byte (mb, CEE_CALLVIRT);
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, cleanup_native));

			break;
		default:
			g_assert_not_reached ();
			break;
		}
		break;

	default:
		g_assert_not_reached ();
	}
		
	return conv_arg;
}

static int
emit_marshal_asany (EmitMarshalContext *m, int argnum, MonoType *t,
					MonoMarshalSpec *spec, 
					int conv_arg, MonoType **conv_arg_type, 
					MarshalAction action)
{
	MonoMethodBuilder *mb = m->mb;

	switch (action) {
	case MARSHAL_ACTION_CONV_IN: {
		MonoMarshalNative encoding = mono_marshal_get_string_encoding (m->piinfo, NULL);

		g_assert (t->type == MONO_TYPE_OBJECT);
		g_assert (!t->byref);

		conv_arg = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		mono_mb_emit_ldarg (mb, argnum);
		mono_mb_emit_icon (mb, encoding);
		mono_mb_emit_icall (mb, mono_marshal_asany);
		mono_mb_emit_stloc (mb, conv_arg);
		break;
	}

	case MARSHAL_ACTION_PUSH:
		mono_mb_emit_ldloc (mb, conv_arg);
		break;

	case MARSHAL_ACTION_CONV_OUT: {
		MonoMarshalNative encoding = mono_marshal_get_string_encoding (m->piinfo, NULL);

		mono_mb_emit_ldarg (mb, argnum);
		mono_mb_emit_ldloc (mb, conv_arg);
		mono_mb_emit_icon (mb, encoding);
		mono_mb_emit_icall (mb, mono_marshal_free_asany);
		break;
	}

	default:
		g_assert_not_reached ();
	}

	return conv_arg;
}

static int
emit_marshal_vtype (EmitMarshalContext *m, int argnum, MonoType *t,
					MonoMarshalSpec *spec, 
					int conv_arg, MonoType **conv_arg_type, 
					MarshalAction action)
{
	MonoMethodBuilder *mb = m->mb;
	MonoClass *klass;
	int pos = 0;

	klass = t->data.klass;

	switch (action) {
	case MARSHAL_ACTION_CONV_IN:
		if (((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) ||
			klass->blittable || klass->enumtype)
			break;

		conv_arg = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
			
		/* store the address of the source into local variable 0 */
		if (t->byref)
			mono_mb_emit_ldarg (mb, argnum);
		else
			mono_mb_emit_ldarg_addr (mb, argnum);
		
		mono_mb_emit_byte (mb, CEE_STLOC_0);
			
		/* allocate space for the native struct and
		 * store the address into local variable 1 (dest) */
		mono_mb_emit_icon (mb, mono_class_native_size (klass, NULL));
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_LOCALLOC);
		mono_mb_emit_stloc (mb, conv_arg);

		if (t->byref) {
			mono_mb_emit_byte (mb, CEE_LDLOC_0);
			mono_mb_emit_byte (mb, CEE_BRFALSE);
			pos = mb->pos;
			mono_mb_emit_i4 (mb, 0);
		}

		/* set dst_ptr */
		mono_mb_emit_ldloc (mb, conv_arg);
		mono_mb_emit_byte (mb, CEE_STLOC_1);

		/* emit valuetype conversion code */
		emit_struct_conv (mb, klass, FALSE);
			
		if (t->byref)
			mono_mb_patch_addr (mb, pos, mb->pos - (pos + 4));
		break;

	case MARSHAL_ACTION_PUSH:
		if (((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) ||
			klass->blittable || klass->enumtype) {
			mono_mb_emit_ldarg (mb, argnum);
			break;
		}			
		mono_mb_emit_ldloc (mb, conv_arg);
		if (!t->byref) {
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_byte (mb, CEE_MONO_LDNATIVEOBJ);
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, klass));
		}
		break;

	case MARSHAL_ACTION_CONV_OUT:
		if (((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) ||
			klass->blittable || klass->enumtype)
			break;

		if (t->byref) {
			/* dst = argument */
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_byte (mb, CEE_STLOC_1);

			mono_mb_emit_byte (mb, CEE_LDLOC_1);
			mono_mb_emit_byte (mb, CEE_BRFALSE);
			pos = mb->pos;
			mono_mb_emit_i4 (mb, 0);

			/* src = tmp_locals [i] */
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_byte (mb, CEE_STLOC_0);

			/* emit valuetype conversion code */
			emit_struct_conv (mb, klass, TRUE);
		}

		emit_struct_free (mb, klass, conv_arg);
		
		if (t->byref)
			mono_mb_patch_addr (mb, pos, mb->pos - (pos + 4));
		break;

	case MARSHAL_ACTION_CONV_RESULT:
		if (((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) ||
			klass->blittable) {
			mono_mb_emit_byte (mb, CEE_STLOC_3);
			break;
		}
		/* load pointer to returned value type */
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_VTADDR);
		/* store the address of the source into local variable 0 */
		mono_mb_emit_byte (mb, CEE_STLOC_0);
		/* set dst_ptr */
		mono_mb_emit_ldloc_addr (mb, 3);
		mono_mb_emit_byte (mb, CEE_STLOC_1);
				
		/* emit valuetype conversion code */
		emit_struct_conv (mb, klass, TRUE);
		break;

	default:
		g_assert_not_reached ();
	}

	return conv_arg;
}

static int
emit_marshal_string (EmitMarshalContext *m, int argnum, MonoType *t,
					 MonoMarshalSpec *spec, 
					 int conv_arg, MonoType **conv_arg_type, 
					 MarshalAction action)
{
	MonoMethodBuilder *mb = m->mb;
	MonoMarshalNative encoding = mono_marshal_get_string_encoding (m->piinfo, spec);
	MonoMarshalConv conv = mono_marshal_get_string_to_ptr_conv (m->piinfo, spec);

	switch (action) {
	case MARSHAL_ACTION_CONV_IN:
		*conv_arg_type = &mono_defaults.int_class->byval_arg;
		conv_arg = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

		if (t->byref) {
			if (t->attrs & PARAM_ATTRIBUTE_OUT)
				break;

			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_byte (mb, CEE_LDIND_I);				
		} else {
			mono_mb_emit_ldarg (mb, argnum);
		}

		if (conv == -1) {
			char *msg = g_strdup_printf ("string marshalling conversion %d not implemented", encoding);
			MonoException *exc = mono_get_exception_not_implemented (msg);
			g_warning (msg);
			g_free (msg);
			mono_raise_exception (exc);
		}
		else
			mono_mb_emit_icall (mb, conv_to_icall (conv));

		mono_mb_emit_stloc (mb, conv_arg);
		break;

	case MARSHAL_ACTION_CONV_OUT:
		if (t->byref && (t->attrs & PARAM_ATTRIBUTE_OUT)) {
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_icall (mb, conv_to_icall (MONO_MARSHAL_CONV_LPSTR_STR));
			mono_mb_emit_byte (mb, CEE_STIND_I);		
		} else {
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_icall (mb, g_free);
		}
		break;

	case MARSHAL_ACTION_CONV_RESULT:
		mono_mb_emit_byte (mb, CEE_STLOC_0);
		mono_mb_emit_byte (mb, CEE_LDLOC_0);
				
		if (spec) {
			switch (spec->native) {
			case MONO_NATIVE_LPWSTR:
				mono_mb_emit_icall (mb, conv_to_icall (MONO_MARSHAL_CONV_LPWSTR_STR));
				break;
			default:
				g_warning ("marshalling conversion not implemented");
				g_assert_not_reached ();
			}
		} else {
			mono_mb_emit_icall (mb, conv_to_icall (MONO_MARSHAL_CONV_LPSTR_STR));
		}
		mono_mb_emit_byte (mb, CEE_STLOC_3);

		/* free the string */
		mono_mb_emit_byte (mb, CEE_LDLOC_0);
		mono_mb_emit_icall (mb, g_free);
		break;

	default:
		g_assert_not_reached ();
	}

	return conv_arg;
}

static int
emit_marshal_object (EmitMarshalContext *m, int argnum, MonoType *t,
					 MonoMarshalSpec *spec, 
					 int conv_arg, MonoType **conv_arg_type, 
					 MarshalAction action)
{
	MonoMethodBuilder *mb = m->mb;
	MonoClass *klass = t->data.klass;
	int pos, pos2, loc;

	switch (action) {
	case MARSHAL_ACTION_CONV_IN:
		*conv_arg_type = &mono_defaults.int_class->byval_arg;
		conv_arg = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

		m->orig_conv_args [argnum] = 0;

		if (klass->delegate) {
			g_assert (!t->byref);
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_icall (mb, conv_to_icall (MONO_MARSHAL_CONV_DEL_FTN));
			mono_mb_emit_stloc (mb, conv_arg);
		} else if (klass == mono_defaults.stringbuilder_class) {
			MonoMarshalNative encoding = mono_marshal_get_string_encoding (m->piinfo, spec);
			MonoMarshalConv conv = mono_marshal_get_stringbuilder_to_ptr_conv (m->piinfo, spec);
			
			g_assert (!t->byref);
			mono_mb_emit_ldarg (mb, argnum);

			if (conv != -1)
				mono_mb_emit_icall (mb, conv_to_icall (conv));
			else {
				char *msg = g_strdup_printf ("stringbuilder marshalling conversion %d not implemented", encoding);
				MonoException *exc = mono_get_exception_not_implemented (msg);
				g_warning (msg);
				g_free (msg);
				mono_raise_exception (exc);
			}

			mono_mb_emit_stloc (mb, conv_arg);
		} else if (klass->blittable) {
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_byte (mb, CEE_MONO_OBJADDR);
			mono_mb_emit_icon (mb, sizeof (MonoObject));
			mono_mb_emit_byte (mb, CEE_ADD);
			mono_mb_emit_stloc (mb, conv_arg);
			break;
		} else {
			mono_mb_emit_byte (mb, CEE_LDNULL);
			mono_mb_emit_stloc (mb, conv_arg);

			if (t->byref) {
				/* we dont need any conversions for out parameters */
				if (t->attrs & PARAM_ATTRIBUTE_OUT)
					break;

				mono_mb_emit_ldarg (mb, argnum);				
				mono_mb_emit_byte (mb, CEE_LDIND_I);

			} else {
				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
				mono_mb_emit_byte (mb, CEE_MONO_OBJADDR);
			}
				
			/* store the address of the source into local variable 0 */
			mono_mb_emit_byte (mb, CEE_STLOC_0);
			mono_mb_emit_byte (mb, CEE_LDLOC_0);
			mono_mb_emit_byte (mb, CEE_BRFALSE);
			pos = mb->pos;
			mono_mb_emit_i4 (mb, 0);

			/* allocate space for the native struct and store the address */
			mono_mb_emit_icon (mb, mono_class_native_size (klass, NULL));
			mono_mb_emit_byte (mb, CEE_PREFIX1);
			mono_mb_emit_byte (mb, CEE_LOCALLOC);
			mono_mb_emit_stloc (mb, conv_arg);

			if (t->byref) {
				/* Need to store the original buffer so we can free it later */
				m->orig_conv_args [argnum] = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
				mono_mb_emit_ldloc (mb, conv_arg);
				mono_mb_emit_stloc (mb, m->orig_conv_args [argnum]);
			}

			/* set the src_ptr */
			mono_mb_emit_byte (mb, CEE_LDLOC_0);
			mono_mb_emit_icon (mb, sizeof (MonoObject));
			mono_mb_emit_byte (mb, CEE_ADD);
			mono_mb_emit_byte (mb, CEE_STLOC_0);

			/* set dst_ptr */
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_byte (mb, CEE_STLOC_1);

			/* emit valuetype conversion code */
			emit_struct_conv (mb, klass, FALSE);

			mono_mb_patch_addr (mb, pos, mb->pos - (pos + 4));
		}
		break;

	case MARSHAL_ACTION_CONV_OUT:
		if (klass == mono_defaults.stringbuilder_class) {
			gboolean need_free;
			MonoMarshalNative encoding;
			MonoMarshalConv conv;

			encoding = mono_marshal_get_string_encoding (m->piinfo, spec);
			conv = mono_marshal_get_ptr_to_stringbuilder_conv (m->piinfo, spec, &need_free);

			g_assert (!t->byref);
			g_assert (encoding != -1);

			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_ldloc (mb, conv_arg);

			mono_mb_emit_icall (mb, conv_to_icall (conv));

			if (need_free) {
				mono_mb_emit_ldloc (mb, conv_arg);
				mono_mb_emit_icall (mb, g_free);
			}
			break;
		}

		if (klass->delegate)
			break;

		if (t->byref && (t->attrs & PARAM_ATTRIBUTE_OUT)) {
			/* allocate a new object */
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_byte (mb, CEE_MONO_NEWOBJ);	
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, t->data.klass));
			mono_mb_emit_byte (mb, CEE_STIND_I);
		}

		/* dst = *argument */
		mono_mb_emit_ldarg (mb, argnum);

		if (t->byref)
			mono_mb_emit_byte (mb, CEE_LDIND_I);

		mono_mb_emit_byte (mb, CEE_STLOC_1);

		mono_mb_emit_byte (mb, CEE_LDLOC_1);
		mono_mb_emit_byte (mb, CEE_BRFALSE);
		pos = mb->pos;
		mono_mb_emit_i4 (mb, 0);

		if (t->byref || (t->attrs & PARAM_ATTRIBUTE_OUT)) {
			mono_mb_emit_byte (mb, CEE_LDLOC_1);
			mono_mb_emit_icon (mb, sizeof (MonoObject));
			mono_mb_emit_byte (mb, CEE_ADD);
			mono_mb_emit_byte (mb, CEE_STLOC_1);
			
			/* src = tmp_locals [i] */
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_byte (mb, CEE_STLOC_0);

			/* emit valuetype conversion code */
			emit_struct_conv (mb, t->data.klass, TRUE);

			/* Free the structure returned by the native code */
			emit_struct_free (mb, klass, conv_arg);

			/* 
			 * If the native function changed the pointer, then free
			 * the original structure plus the new pointer.
			 */
			mono_mb_emit_ldloc (mb, m->orig_conv_args [argnum]);
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_byte (mb, CEE_BEQ);
			pos2 = mb->pos;
			mono_mb_emit_i4 (mb, 0);

			if (!(t->attrs & PARAM_ATTRIBUTE_OUT)) {
				g_assert (m->orig_conv_args [argnum]);

				emit_struct_free (mb, klass, m->orig_conv_args [argnum]);
			}

			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_icall (mb, g_free);

			mono_mb_patch_addr (mb, pos2, mb->pos - (pos2 + 4));
		}
		else
			/* Free the original structure passed to native code */
			emit_struct_free (mb, klass, conv_arg);

		mono_mb_patch_addr (mb, pos, mb->pos - (pos + 4));
		break;

	case MARSHAL_ACTION_CONV_RESULT:
		/* set src */
		mono_mb_emit_byte (mb, CEE_STLOC_0);

		/* Make a copy since emit_conv modifies local 0 */
		loc = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		mono_mb_emit_byte (mb, CEE_LDLOC_0);
		mono_mb_emit_stloc (mb, loc);

		mono_mb_emit_byte (mb, CEE_LDNULL);
		mono_mb_emit_byte (mb, CEE_STLOC_3);

		mono_mb_emit_byte (mb, CEE_LDLOC_0);
		mono_mb_emit_byte (mb, CEE_BRFALSE);
		pos = mb->pos;
		mono_mb_emit_i4 (mb, 0);

		/* allocate result object */

		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_NEWOBJ);	
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, klass));
		mono_mb_emit_byte (mb, CEE_STLOC_3);
				
		/* set dst  */

		mono_mb_emit_byte (mb, CEE_LDLOC_3);
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_OBJADDR);
		mono_mb_emit_icon (mb, sizeof (MonoObject));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_STLOC_1);
							
		/* emit conversion code */
		emit_struct_conv (mb, klass, TRUE);

		emit_struct_free (mb, klass, loc);

		/* Free the pointer allocated by unmanaged code */
		mono_mb_emit_ldloc (mb, loc);
		mono_mb_emit_icall (mb, g_free);

		mono_mb_patch_addr (mb, pos, mb->pos - (pos + 4));
		break;

	default:
		g_assert_not_reached ();
	}

	return conv_arg;
}

static int
emit_marshal_array (EmitMarshalContext *m, int argnum, MonoType *t,
					MonoMarshalSpec *spec, 
					int conv_arg, MonoType **conv_arg_type, 
					MarshalAction action)
{
	MonoMethodBuilder *mb = m->mb;
	MonoClass *klass = mono_class_from_mono_type (t);
	gboolean need_convert, need_free;

	switch (action) {
	case MARSHAL_ACTION_CONV_IN:
		*conv_arg_type = &mono_defaults.int_class->byval_arg;
		conv_arg = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

		if (klass->element_class->blittable) {
			mono_mb_emit_ldarg (mb, argnum);
			if (t->byref)
				mono_mb_emit_byte (mb, CEE_LDIND_I);
			mono_mb_emit_icall (mb, conv_to_icall (MONO_MARSHAL_CONV_ARRAY_LPARRAY));
			mono_mb_emit_stloc (mb, conv_arg);
		}
		else {
			MonoClass *eklass;
			guint32 label1, label2, label3;
			int index_var, src_var, dest_ptr, esize;
			MonoMarshalNative encoding;
			MonoMarshalConv conv;
			gboolean is_string = FALSE;

			dest_ptr = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

			eklass = klass->element_class;

			encoding = mono_marshal_get_string_encoding (m->piinfo, spec);
			if (eklass == mono_defaults.string_class) {
				is_string = TRUE;
				conv = mono_marshal_get_string_to_ptr_conv (m->piinfo, spec);
			}
			else if (eklass == mono_defaults.stringbuilder_class) {
				is_string = TRUE;
				conv = mono_marshal_get_stringbuilder_to_ptr_conv (m->piinfo, spec);
			}
			else
				conv = -1;

			src_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
			mono_mb_emit_ldarg (mb, argnum);
			if (t->byref)
				mono_mb_emit_byte (mb, CEE_LDIND_I);
			mono_mb_emit_stloc (mb, src_var);

			/* Check null */
			mono_mb_emit_ldloc (mb, src_var);
			mono_mb_emit_stloc (mb, conv_arg);
			mono_mb_emit_ldloc (mb, src_var);
			mono_mb_emit_byte (mb, CEE_BRFALSE);
			label1 = mb->pos;
			mono_mb_emit_i4 (mb, 0);

			if (is_string) {
				if (conv == -1) {
					char *msg = g_strdup_printf ("string/stringbuilder marshalling conversion %d not implemented", encoding);
					MonoException *exc = mono_get_exception_not_implemented (msg);
					g_warning (msg);
					g_free (msg);
					mono_raise_exception (exc);
				}
			}

			if (is_string)
				esize = sizeof (gpointer);
			else
				esize = mono_class_native_size (eklass, NULL);

			/* allocate space for the native struct and store the address */
			mono_mb_emit_icon (mb, esize);
			mono_mb_emit_ldloc (mb, src_var);
			mono_mb_emit_byte (mb, CEE_LDLEN);
			mono_mb_emit_byte (mb, CEE_MUL);
			mono_mb_emit_byte (mb, CEE_PREFIX1);
			mono_mb_emit_byte (mb, CEE_LOCALLOC);
			mono_mb_emit_stloc (mb, conv_arg);

			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_stloc (mb, dest_ptr);

			/* Emit marshalling loop */
			index_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);				
			mono_mb_emit_byte (mb, CEE_LDC_I4_0);
			mono_mb_emit_stloc (mb, index_var);
			label2 = mb->pos;
			mono_mb_emit_ldloc (mb, index_var);
			mono_mb_emit_ldloc (mb, src_var);
			mono_mb_emit_byte (mb, CEE_LDLEN);
			mono_mb_emit_byte (mb, CEE_BGE);
			label3 = mb->pos;
			mono_mb_emit_i4 (mb, 0);

			/* Emit marshalling code */

			if (is_string) {
				mono_mb_emit_ldloc (mb, dest_ptr);
				mono_mb_emit_ldloc (mb, src_var);
				mono_mb_emit_ldloc (mb, index_var);
				mono_mb_emit_byte (mb, CEE_LDELEM_REF);
				mono_mb_emit_icall (mb, conv_to_icall (conv));
				mono_mb_emit_byte (mb, CEE_STIND_I);
			}
			else {
				/* set the src_ptr */
				mono_mb_emit_ldloc (mb, src_var);
				mono_mb_emit_ldloc (mb, index_var);
				mono_mb_emit_byte (mb, CEE_LDELEMA);
				mono_mb_emit_i4 (mb, mono_mb_add_data (mb, eklass));
				mono_mb_emit_byte (mb, CEE_STLOC_0);

				/* set dst_ptr */
				mono_mb_emit_ldloc (mb, dest_ptr);
				mono_mb_emit_byte (mb, CEE_STLOC_1);

				/* emit valuetype conversion code */
				emit_struct_conv (mb, eklass, FALSE);
			}

			mono_mb_emit_add_to_local (mb, index_var, 1);
			mono_mb_emit_add_to_local (mb, dest_ptr, esize);
			
			mono_mb_emit_byte (mb, CEE_BR);
			mono_mb_emit_i4 (mb, label2 - (mb->pos + 4));

			mono_mb_patch_addr (mb, label1, mb->pos - (label1 + 4));
			mono_mb_patch_addr (mb, label3, mb->pos - (label3 + 4));
		}

		break;

	case MARSHAL_ACTION_CONV_OUT:
		/* Character arrays are implicitly marshalled as [Out] */
		need_convert = (klass->element_class == mono_defaults.char_class) || (klass->element_class == mono_defaults.stringbuilder_class) || (t->attrs & PARAM_ATTRIBUTE_OUT);
		need_free = mono_marshal_need_free (&klass->element_class->byval_arg, 
											m->piinfo, spec);

		if (need_convert || need_free) {
			/* FIXME: Optimize blittable case */
			MonoClass *eklass;
			guint32 label1, label2, label3;
			int index_var, src_ptr, loc, esize;

			eklass = klass->element_class;
			if ((eklass == mono_defaults.stringbuilder_class) || (eklass == mono_defaults.string_class))
				esize = sizeof (gpointer);
			else
				esize = mono_class_native_size (eklass, NULL);
			src_ptr = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
			loc = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

			/* Check null */
			mono_mb_emit_ldarg (mb, argnum);
			if (t->byref)
				mono_mb_emit_byte (mb, CEE_LDIND_I);
			mono_mb_emit_byte (mb, CEE_BRFALSE);
			label1 = mb->pos;
			mono_mb_emit_i4 (mb, 0);

			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_stloc (mb, src_ptr);

			/* Emit marshalling loop */
			index_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);				
			mono_mb_emit_byte (mb, CEE_LDC_I4_0);
			mono_mb_emit_stloc (mb, index_var);
			label2 = mb->pos;
			mono_mb_emit_ldloc (mb, index_var);
			mono_mb_emit_ldarg (mb, argnum);
			if (t->byref)
				mono_mb_emit_byte (mb, CEE_LDIND_I);
			mono_mb_emit_byte (mb, CEE_LDLEN);
			mono_mb_emit_byte (mb, CEE_BGE);
			label3 = mb->pos;
			mono_mb_emit_i4 (mb, 0);

			/* Emit marshalling code */

			if (eklass == mono_defaults.stringbuilder_class) {
				gboolean need_free2;
				MonoMarshalConv conv = mono_marshal_get_ptr_to_stringbuilder_conv (m->piinfo, spec, &need_free2);

				g_assert (conv != -1);

				/* dest */
				mono_mb_emit_ldarg (mb, argnum);
				if (t->byref)
					mono_mb_emit_byte (mb, CEE_LDIND_I);
				mono_mb_emit_ldloc (mb, index_var);
				mono_mb_emit_byte (mb, CEE_LDELEM_REF);

				/* src */
				mono_mb_emit_ldloc (mb, src_ptr);
				mono_mb_emit_byte (mb, CEE_LDIND_I);

				mono_mb_emit_icall (mb, conv_to_icall (conv));

				if (need_free) {
					/* src */
					mono_mb_emit_ldloc (mb, src_ptr);
					mono_mb_emit_byte (mb, CEE_LDIND_I);

					mono_mb_emit_icall (mb, g_free);
				}
			}
			else if (eklass == mono_defaults.string_class) {
				if (need_free) {
					/* src */
					mono_mb_emit_ldloc (mb, src_ptr);
					mono_mb_emit_byte (mb, CEE_LDIND_I);

					mono_mb_emit_icall (mb, g_free);
				}
			}
			else {
				if (need_convert) {
					/* set the src_ptr */
					mono_mb_emit_ldloc (mb, src_ptr);
					mono_mb_emit_byte (mb, CEE_STLOC_0);

					/* set dst_ptr */
					mono_mb_emit_ldarg (mb, argnum);
					if (t->byref)
						mono_mb_emit_byte (mb, CEE_LDIND_I);
					mono_mb_emit_ldloc (mb, index_var);
					mono_mb_emit_byte (mb, CEE_LDELEMA);
					mono_mb_emit_i4 (mb, mono_mb_add_data (mb, eklass));
					mono_mb_emit_byte (mb, CEE_STLOC_1);

					/* emit valuetype conversion code */
					emit_struct_conv (mb, eklass, TRUE);
				}

				if (need_free) {
					mono_mb_emit_ldloc (mb, src_ptr);
					mono_mb_emit_stloc (mb, loc);
					mono_mb_emit_ldloc (mb, loc);

					emit_struct_free (mb, eklass, loc);
				}
			}

			mono_mb_emit_add_to_local (mb, index_var, 1);
			mono_mb_emit_add_to_local (mb, src_ptr, esize);

			mono_mb_emit_byte (mb, CEE_BR);
			mono_mb_emit_i4 (mb, label2 - (mb->pos + 4));

			mono_mb_patch_addr (mb, label1, mb->pos - (label1 + 4));
			mono_mb_patch_addr (mb, label3, mb->pos - (label3 + 4));
		}
		break;

	case MARSHAL_ACTION_CONV_RESULT:
		/* fixme: we need conversions here */
		mono_mb_emit_byte (mb, CEE_STLOC_3);

	default:
		g_assert_not_reached ();
	}

	return conv_arg;
}

static int
emit_marshal_boolean (EmitMarshalContext *m, int argnum, MonoType *t,
					  MonoMarshalSpec *spec, 
					  int conv_arg, MonoType **conv_arg_type, 
					  MarshalAction action)
{
	MonoMethodBuilder *mb = m->mb;

	switch (action) {
	case MARSHAL_ACTION_CONV_IN: {
		MonoType *local_type;
		int variant_bool = 0;
		if (!t->byref)
			break;
		if (spec == NULL) {
			local_type = &mono_defaults.int32_class->byval_arg;
		} else {
			switch (spec->native) {
			case MONO_NATIVE_I1:
				local_type = &mono_defaults.byte_class->byval_arg;
				break;
			case MONO_NATIVE_VARIANTBOOL:
				local_type = &mono_defaults.int16_class->byval_arg;
				variant_bool = 1;
				break;
			default:
				g_warning ("marshalling bool as native type %x is currently not supported", spec->native);
				break;
			}
		}
		*conv_arg_type = &mono_defaults.int_class->byval_arg;
		conv_arg = mono_mb_add_local (mb, local_type);
		mono_mb_emit_ldarg (mb, argnum);
		mono_mb_emit_byte (mb, CEE_LDIND_I1);
		if (variant_bool)
			mono_mb_emit_byte (mb, CEE_NEG);
		mono_mb_emit_stloc (mb, conv_arg);
		break;
	}

	case MARSHAL_ACTION_CONV_RESULT:
		/* maybe we need to make sure that it fits within 8 bits */
		mono_mb_emit_byte (mb, CEE_STLOC_3);
		break;

	default:
		g_assert_not_reached ();
	}

	return conv_arg;
}

static int
emit_marshal (EmitMarshalContext *m, int argnum, MonoType *t, 
			  MonoMarshalSpec *spec, int conv_arg, 
			  MonoType **conv_arg_type, MarshalAction action)
{
	/* Ensure that we have marshalling info for this param */
	mono_marshal_load_type_info (mono_class_from_mono_type (t));

	if (spec && spec->native == MONO_NATIVE_CUSTOM)
		return emit_marshal_custom (m, argnum, t, spec, conv_arg, conv_arg_type, action);

	if (spec && spec->native == MONO_NATIVE_ASANY)
		return emit_marshal_asany (m, argnum, t, spec, conv_arg, conv_arg_type, action);
			
	switch (t->type) {
	case MONO_TYPE_VALUETYPE:
		return emit_marshal_vtype (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_STRING:
		return emit_marshal_string (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
		return emit_marshal_object (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_SZARRAY:
		return emit_marshal_array (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_BOOLEAN:
		return emit_marshal_boolean (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	}

	return conv_arg;
}

/**
 * mono_marshal_get_native_wrapper:
 * @method: The MonoMethod to wrap.
 *
 * generates IL code for the pinvoke wrapper (the generated method
 * calls the unmanaged code in method->addr)
 */
MonoMethod *
mono_marshal_get_native_wrapper (MonoMethod *method)
{
	EmitMarshalContext m;
	MonoMethodSignature *sig, *csig;
	MonoMethodPInvoke *piinfo;
	MonoMethodBuilder *mb;
	MonoMarshalSpec **mspecs;
	MonoMethod *res;
	GHashTable *cache;
	MonoClass *klass;
	gboolean pinvoke = FALSE;
	int i, argnum, *tmp_locals;
	int type;
	const char *exc_class = "MissingMethodException";
	const char *exc_arg = NULL;

	g_assert (method != NULL);
	g_assert (method->signature->pinvoke);

	cache = method->klass->image->native_wrapper_cache;
	if ((res = mono_marshal_find_in_cache (cache, method)))
		return res;

	sig = method->signature;

	if (!(method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) &&
	    (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL))
		pinvoke = TRUE;

	if (!method->addr) {
		if (pinvoke)
			mono_lookup_pinvoke_call (method, &exc_class, &exc_arg);
		else
			method->addr = mono_lookup_internal_call (method);
	}

	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_MANAGED_TO_NATIVE);

	mb->method->save_lmf = 1;

	piinfo = (MonoMethodPInvoke *)method;

	if (!method->addr) {
		mono_mb_emit_exception (mb, exc_class, exc_arg);
		csig = mono_metadata_signature_dup (sig);
		csig->pinvoke = 0;
		res = mono_mb_create_and_cache (cache, method,
										mb, csig, csig->param_count + 16);
		mono_mb_free (mb);
		return res;
	}

	m.mb = mb;
	m.piinfo = piinfo;

	/* internal calls: we simply push all arguments and call the method (no conversions) */
	if (method->iflags & (METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL | METHOD_IMPL_ATTRIBUTE_RUNTIME)) {

		/* hack - string constructors returns a value */
		if (method->string_ctor) {
			csig = mono_metadata_signature_dup (sig);
			csig->ret = &mono_defaults.string_class->byval_arg;
		} else
			csig = sig;

		if (sig->hasthis)
			mono_mb_emit_byte (mb, CEE_LDARG_0);

		for (i = 0; i < sig->param_count; i++)
			mono_mb_emit_ldarg (mb, i + sig->hasthis);

		g_assert (method->addr);
		mono_mb_emit_native_call (mb, csig, method->addr);
		emit_thread_interrupt_checkpoint (mb);
		mono_mb_emit_byte (mb, CEE_RET);

		csig = mono_metadata_signature_dup (csig);
		csig->pinvoke = 0;
		res = mono_mb_create_and_cache (cache, method,
										mb, csig, csig->param_count + 16);
		mono_mb_free (mb);
		return res;
	}

	g_assert (pinvoke);

	mspecs = g_new (MonoMarshalSpec*, sig->param_count + 1);
	mono_method_get_marshal_info (method, mspecs);

	/* pinvoke: we need to convert the arguments if necessary */

	/* we copy the signature, so that we can set pinvoke to 0 */
	csig = mono_metadata_signature_dup (sig);
	csig->pinvoke = 1;

	/* we allocate local for use with emit_struct_conv() */
	/* allocate local 0 (pointer) src_ptr */
	mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	/* allocate local 1 (pointer) dst_ptr */
	mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	/* allocate local 2 (boolean) delete_old */
	mono_mb_add_local (mb, &mono_defaults.boolean_class->byval_arg);

	/* delete_old = FALSE */
	mono_mb_emit_icon (mb, 0);
	mono_mb_emit_byte (mb, CEE_STLOC_2);

	if (!MONO_TYPE_IS_VOID(sig->ret)) {
		/* allocate local 3 to store the return value */
		mono_mb_add_local (mb, sig->ret);
	}

	if (mspecs [0] && mspecs [0]->native == MONO_NATIVE_CUSTOM) {
		/* Return type custom marshaling */
		/*
		 * Since we can't determine the return type of the unmanaged function,
		 * we assume it returns a pointer, and pass that pointer to
		 * MarshalNativeToManaged.
		 */
		csig->ret = &mono_defaults.int_class->byval_arg;
	}

	/* we first do all conversions */
	tmp_locals = alloca (sizeof (int) * sig->param_count);
	m.orig_conv_args = alloca (sizeof (int) * (sig->param_count + 1));

	for (i = 0; i < sig->param_count; i ++) {
		tmp_locals [i] = emit_marshal (&m, i + sig->hasthis, sig->params [i], mspecs [i + 1], 0, &csig->params [i], MARSHAL_ACTION_CONV_IN);
	}

	/* push all arguments */

	if (sig->hasthis)
		mono_mb_emit_byte (mb, CEE_LDARG_0);

	for (i = 0; i < sig->param_count; i++) {
		MonoType *t = sig->params [i];
		MonoMarshalSpec *spec = mspecs [i + 1];

		if (spec && ((spec->native == MONO_NATIVE_CUSTOM) || (spec->native == MONO_NATIVE_ASANY)))
			emit_marshal (&m, i + sig->hasthis, t, spec, tmp_locals [i], NULL, MARSHAL_ACTION_PUSH);
		else {
			argnum = i + sig->hasthis;

			switch (t->type) {
			case MONO_TYPE_BOOLEAN:
				if (t->byref) {
					g_assert (tmp_locals [i]);
					mono_mb_emit_ldloc_addr (mb, tmp_locals [i]);
				} else
					mono_mb_emit_ldarg (mb, argnum);
				break;
			case MONO_TYPE_I1:
			case MONO_TYPE_U1:
			case MONO_TYPE_I2:
			case MONO_TYPE_U2:
			case MONO_TYPE_I4:
			case MONO_TYPE_U4:
			case MONO_TYPE_I:
			case MONO_TYPE_U:
			case MONO_TYPE_PTR:
			case MONO_TYPE_R4:
			case MONO_TYPE_R8:
			case MONO_TYPE_I8:
			case MONO_TYPE_U8:
				mono_mb_emit_ldarg (mb, argnum);
				break;
			case MONO_TYPE_VALUETYPE:
				emit_marshal (&m, i + sig->hasthis, t, spec, tmp_locals [i], NULL, MARSHAL_ACTION_PUSH);
				break;
			case MONO_TYPE_STRING:
			case MONO_TYPE_CLASS:
			case MONO_TYPE_OBJECT:
			case MONO_TYPE_ARRAY:
			case MONO_TYPE_SZARRAY:
				g_assert (tmp_locals [i]);
				if (t->byref) 
					mono_mb_emit_ldloc_addr (mb, tmp_locals [i]);
				else
					mono_mb_emit_ldloc (mb, tmp_locals [i]);
				break;
			case MONO_TYPE_CHAR:
				/* fixme: dont know how to marshal that. We cant simply
				 * convert it to a one byte UTF8 character, because an
				 * unicode character may need more that one byte in UTF8 */
				mono_mb_emit_ldarg (mb, argnum);
				break;
			case MONO_TYPE_TYPEDBYREF:
			case MONO_TYPE_FNPTR:
			default:
				g_warning ("type 0x%02x unknown", t->type);	
				g_assert_not_reached ();
			}
		}
	}			

	/* call the native method */
	mono_mb_emit_native_call (mb, csig, method->addr);

	/* Set LastError if needed */
	if (piinfo->piflags & PINVOKE_ATTRIBUTE_SUPPORTS_LAST_ERROR) {
		MonoMethodSignature *lasterr_sig;

		lasterr_sig = mono_metadata_signature_alloc (mono_defaults.corlib, 0);
		lasterr_sig->ret = &mono_defaults.void_class->byval_arg;
		lasterr_sig->pinvoke = 1;

		mono_mb_emit_native_call (mb, lasterr_sig, mono_marshal_set_last_error);
	}		

	/* convert the result */
	if (!sig->ret->byref) {
		MonoMarshalSpec *spec = mspecs [0];
		type = sig->ret->type;

		if (spec && spec->native == MONO_NATIVE_CUSTOM) {
			emit_marshal (&m, 0, sig->ret, spec, 0, NULL, MARSHAL_ACTION_CONV_RESULT);
		} else {

		handle_enum:
			switch (type) {
			case MONO_TYPE_VOID:
				break;
			case MONO_TYPE_I1:
			case MONO_TYPE_U1:
			case MONO_TYPE_I2:
			case MONO_TYPE_U2:
			case MONO_TYPE_I4:
			case MONO_TYPE_U4:
			case MONO_TYPE_I:
			case MONO_TYPE_U:
			case MONO_TYPE_PTR:
			case MONO_TYPE_R4:
			case MONO_TYPE_R8:
			case MONO_TYPE_I8:
			case MONO_TYPE_U8:
				/* no conversions necessary */
				mono_mb_emit_byte (mb, CEE_STLOC_3);
				break;
			case MONO_TYPE_VALUETYPE:
				klass = sig->ret->data.klass;
				if (klass->enumtype) {
					type = sig->ret->data.klass->enum_basetype->type;
					goto handle_enum;
				}
				emit_marshal (&m, 0, sig->ret, spec, 0, NULL, MARSHAL_ACTION_CONV_RESULT);
				break;
			case MONO_TYPE_STRING:
			case MONO_TYPE_CLASS:
			case MONO_TYPE_OBJECT:
			case MONO_TYPE_BOOLEAN:
			case MONO_TYPE_ARRAY:
			case MONO_TYPE_SZARRAY:
				emit_marshal (&m, 0, sig->ret, spec, 0, NULL, MARSHAL_ACTION_CONV_RESULT);
				break;
			case MONO_TYPE_CHAR:
				/* fixme: we need conversions here */
				mono_mb_emit_byte (mb, CEE_STLOC_3);
				break;
			case MONO_TYPE_TYPEDBYREF:
			case MONO_TYPE_FNPTR:
			default:
				g_warning ("return type 0x%02x unknown", sig->ret->type);	
				g_assert_not_reached ();
			}
		}
	} else {
		mono_mb_emit_byte (mb, CEE_STLOC_3);
	}

	/* 
	 * Need to call this after converting the result since MONO_VTADDR needs 
	 * to be adjacent to the call instruction.
	 */
	emit_thread_interrupt_checkpoint (mb);

	/* we need to convert byref arguments back and free string arrays */
	for (i = 0; i < sig->param_count; i++) {
		MonoType *t = sig->params [i];
		MonoMarshalSpec *spec = mspecs [i + 1];

		argnum = i + sig->hasthis;

		if (spec && ((spec->native == MONO_NATIVE_CUSTOM) || (spec->native == MONO_NATIVE_ASANY))) {
			emit_marshal (&m, argnum, t, spec, tmp_locals [i], NULL, MARSHAL_ACTION_CONV_OUT);
			continue;
		}

		switch (t->type) {
		case MONO_TYPE_STRING:
		case MONO_TYPE_VALUETYPE:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_SZARRAY:
			emit_marshal (&m, argnum, t, spec, tmp_locals [i], NULL, MARSHAL_ACTION_CONV_OUT);
			break;
		case MONO_TYPE_BOOLEAN:
			if (!t->byref)
				continue;
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_ldloc (mb, tmp_locals [i]);
			if (mspecs [i + 1] != NULL && mspecs [i + 1]->native == MONO_NATIVE_VARIANTBOOL)
				mono_mb_emit_byte (mb, CEE_NEG);
			mono_mb_emit_byte (mb, CEE_STIND_I1);
		}
	}

	if (!MONO_TYPE_IS_VOID(sig->ret))
		mono_mb_emit_byte (mb, CEE_LDLOC_3);

	mono_mb_emit_byte (mb, CEE_RET);

	csig = mono_metadata_signature_dup (sig);
	csig->pinvoke = 0;
	res = mono_mb_create_and_cache (cache, method,
									mb, csig, csig->param_count + 16);
	mono_mb_free (mb);

	for (i = sig->param_count; i >= 0; i--)
		g_free (mspecs [i]);
	g_free (mspecs);

	/* printf ("CODE FOR %s: \n%s.\n", mono_method_full_name (res, TRUE), mono_disasm_code (0, res, ((MonoMethodNormal*)res)->header->code, ((MonoMethodNormal*)res)->header->code + ((MonoMethodNormal*)res)->header->code_size)); */

	return res;
}

void
mono_upgrade_remote_class_wrapper (MonoReflectionType *rtype, MonoTransparentProxy *tproxy);

static MonoReflectionType *
type_from_handle (MonoType *handle)
{
	MonoDomain *domain = mono_domain_get (); 
	MonoClass *klass = mono_class_from_mono_type (handle);

	MONO_ARCH_SAVE_REGS;

	mono_class_init (klass);
	return mono_type_get_object (domain, handle);
}

/*
 * mono_marshal_get_isinst:
 * @klass: the type of the field
 *
 * This method generates a function which can be used to check if an object is
 * an instance of the given type, icluding the case where the object is a proxy.
 * The generated function has the following signature:
 * MonoObject* __isinst_wrapper_ (MonoObject *obj)
 */
MonoMethod *
mono_marshal_get_isinst (MonoClass *klass)
{
	static MonoMethodSignature *isint_sig = NULL;
	static GHashTable *isinst_hash = NULL; 
	MonoMethod *res;
	int pos_was_ok, pos_failed, pos_end, pos_end2;
	char *name;
	MonoMethodBuilder *mb;

	EnterCriticalSection (&marshal_mutex);
	if (!isinst_hash) 
		isinst_hash = g_hash_table_new (NULL, NULL);
	
	res = g_hash_table_lookup (isinst_hash, klass);
	LeaveCriticalSection (&marshal_mutex);
	if (res)
		return res;

	if (!isint_sig) {
		isint_sig = mono_metadata_signature_alloc (mono_defaults.corlib, 1);
		isint_sig->params [0] = &mono_defaults.object_class->byval_arg;
		isint_sig->ret = &mono_defaults.object_class->byval_arg;
		isint_sig->pinvoke = 0;
	}
	
	name = g_strdup_printf ("__isinst_wrapper_%s", klass->name); 
	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_ISINST);
	g_free (name);
	
	mb->method->save_lmf = 1;

	/* check if the object is a proxy that needs special cast */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_CISINST);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, klass));

	/* The result of MONO_ISINST can be:
	   	0) the type check succeeded
		1) the type check did not succeed
		2) a CanCastTo call is needed */
	
	mono_mb_emit_byte (mb, CEE_DUP);
	pos_was_ok = mono_mb_emit_branch (mb, CEE_BRFALSE);

	mono_mb_emit_byte (mb, CEE_LDC_I4_2);
	pos_failed = mono_mb_emit_branch (mb, CEE_BNE_UN);
	
	/* get the real proxy from the transparent proxy*/

	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_managed_call (mb, mono_marshal_get_proxy_cancast (klass), NULL);
	pos_end = mono_mb_emit_branch (mb, CEE_BR);
	
	/* fail */
	
	mono_mb_patch_addr (mb, pos_failed, mb->pos - (pos_failed + 4));
	mono_mb_emit_byte (mb, CEE_LDNULL);
	pos_end2 = mono_mb_emit_branch (mb, CEE_BR);
	
	/* success */
	
	mono_mb_patch_addr (mb, pos_was_ok, mb->pos - (pos_was_ok + 4));
	mono_mb_emit_byte (mb, CEE_POP);
	mono_mb_emit_ldarg (mb, 0);
	
	/* the end */
	
	mono_mb_patch_addr (mb, pos_end, mb->pos - (pos_end + 4));
	mono_mb_patch_addr (mb, pos_end2, mb->pos - (pos_end2 + 4));
	mono_mb_emit_byte (mb, CEE_RET);

	res = mono_mb_create_and_cache (isinst_hash, klass, mb, isint_sig, isint_sig->param_count + 16);
	mono_mb_free (mb);

	return res;
}

/*
 * mono_marshal_get_castclass:
 * @klass: the type of the field
 *
 * This method generates a function which can be used to cast an object to
 * an instance of the given type, icluding the case where the object is a proxy.
 * The generated function has the following signature:
 * MonoObject* __castclass_wrapper_ (MonoObject *obj)
 */
MonoMethod *
mono_marshal_get_castclass (MonoClass *klass)
{
	static MonoMethodSignature *castclass_sig = NULL;
	static GHashTable *castclass_hash = NULL; 
	MonoMethod *res;
	int pos_was_ok, pos_was_ok2;
	char *name;
	MonoMethodBuilder *mb;

	EnterCriticalSection (&marshal_mutex);
	if (!castclass_hash) 
		castclass_hash = g_hash_table_new (NULL, NULL);
	
	res = g_hash_table_lookup (castclass_hash, klass);
	LeaveCriticalSection (&marshal_mutex);
	if (res)
		return res;

	if (!castclass_sig) {
		castclass_sig = mono_metadata_signature_alloc (mono_defaults.corlib, 1);
		castclass_sig->params [0] = &mono_defaults.object_class->byval_arg;
		castclass_sig->ret = &mono_defaults.object_class->byval_arg;
		castclass_sig->pinvoke = 0;
	}
	
	name = g_strdup_printf ("__castclass_wrapper_%s", klass->name); 
	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_CASTCLASS);
	g_free (name);
	
	mb->method->save_lmf = 1;

	/* check if the object is a proxy that needs special cast */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_CCASTCLASS);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, klass));

	/* The result of MONO_ISINST can be:
	   	0) the cast is valid
		1) cast of unknown proxy type
		or an exception if the cast is is invalid
	*/
	
	pos_was_ok = mono_mb_emit_branch (mb, CEE_BRFALSE);

	/* get the real proxy from the transparent proxy*/

	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_managed_call (mb, mono_marshal_get_proxy_cancast (klass), NULL);
	pos_was_ok2 = mono_mb_emit_branch (mb, CEE_BRTRUE);
	
	/* fail */
	mono_mb_emit_exception (mb, "InvalidCastException", NULL);
	
	/* success */
	mono_mb_patch_addr (mb, pos_was_ok, mb->pos - (pos_was_ok + 4));
	mono_mb_patch_addr (mb, pos_was_ok2, mb->pos - (pos_was_ok2 + 4));
	mono_mb_emit_ldarg (mb, 0);
	
	/* the end */
	mono_mb_emit_byte (mb, CEE_RET);

	res = mono_mb_create_and_cache (castclass_hash, klass, mb, castclass_sig, castclass_sig->param_count + 16);
	mono_mb_free (mb);

	return res;
}

MonoMethod *
mono_marshal_get_proxy_cancast (MonoClass *klass)
{
	static MonoMethodSignature *from_handle_sig = NULL;
	static MonoMethodSignature *upgrade_proxy_sig = NULL;
	static MonoMethodSignature *isint_sig = NULL;
	static GHashTable *proxy_isinst_hash = NULL; 
	MonoMethod *res;
	int pos_failed, pos_end;
	char *name;
	MonoMethod *can_cast_to;
	MonoMethodDesc *desc;
	MonoMethodBuilder *mb;

	EnterCriticalSection (&marshal_mutex);
	if (!proxy_isinst_hash) 
		proxy_isinst_hash = g_hash_table_new (NULL, NULL);
	
	res = g_hash_table_lookup (proxy_isinst_hash, klass);
	LeaveCriticalSection (&marshal_mutex);
	if (res)
		return res;

	if (!isint_sig) {
		upgrade_proxy_sig = mono_metadata_signature_alloc (mono_defaults.corlib, 2);
		upgrade_proxy_sig->params [0] = &mono_defaults.object_class->byval_arg;
		upgrade_proxy_sig->params [1] = &mono_defaults.object_class->byval_arg;
		upgrade_proxy_sig->ret = &mono_defaults.void_class->byval_arg;
		upgrade_proxy_sig->pinvoke = 1;

		from_handle_sig = mono_metadata_signature_alloc (mono_defaults.corlib, 1);
		from_handle_sig->params [0] = &mono_defaults.object_class->byval_arg;
		from_handle_sig->ret = &mono_defaults.object_class->byval_arg;
	
		isint_sig = mono_metadata_signature_alloc (mono_defaults.corlib, 1);
		isint_sig->params [0] = &mono_defaults.object_class->byval_arg;
		isint_sig->ret = &mono_defaults.object_class->byval_arg;
		isint_sig->pinvoke = 0;
	}
	
	name = g_strdup_printf ("__proxy_isinst_wrapper_%s", klass->name); 
	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_PROXY_ISINST);
	g_free (name);
	
	mb->method->save_lmf = 1;

	/* get the real proxy from the transparent proxy*/
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoTransparentProxy, rp));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	
	/* get the refletion type from the type handle */
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_LDPTR);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, &klass->byval_arg));
	mono_mb_emit_native_call (mb, from_handle_sig, type_from_handle);
	
	mono_mb_emit_ldarg (mb, 0);
	
	/* make the call to CanCastTo (type, ob) */
	desc = mono_method_desc_new ("IRemotingTypeInfo:CanCastTo", FALSE);
	can_cast_to = mono_method_desc_search_in_class (desc, mono_defaults.iremotingtypeinfo_class);
	g_assert (can_cast_to);
	mono_method_desc_free (desc);
	mono_mb_emit_byte (mb, CEE_CALLVIRT);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, can_cast_to));

	
	pos_failed = mono_mb_emit_branch (mb, CEE_BRFALSE);

	/* Upgrade the proxy vtable by calling: mono_upgrade_remote_class_wrapper (type, ob)*/
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_LDPTR);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, &klass->byval_arg));
	mono_mb_emit_native_call (mb, from_handle_sig, type_from_handle);
	mono_mb_emit_ldarg (mb, 0);
	
	mono_mb_emit_native_call (mb, upgrade_proxy_sig, mono_upgrade_remote_class_wrapper);
	emit_thread_interrupt_checkpoint (mb);
	
	mono_mb_emit_ldarg (mb, 0);
	pos_end = mono_mb_emit_branch (mb, CEE_BR);
	
	/* fail */
	
	mono_mb_patch_addr (mb, pos_failed, mb->pos - (pos_failed + 4));
	mono_mb_emit_byte (mb, CEE_LDNULL);
	
	/* the end */
	
	mono_mb_patch_addr (mb, pos_end, mb->pos - (pos_end + 4));
	mono_mb_emit_byte (mb, CEE_RET);

	res = mono_mb_create_and_cache (proxy_isinst_hash, klass, mb, isint_sig, isint_sig->param_count + 16);
	mono_mb_free (mb);

	return res;
}

void
mono_upgrade_remote_class_wrapper (MonoReflectionType *rtype, MonoTransparentProxy *tproxy)
{
	MonoClass *klass;
	klass = mono_class_from_mono_type (rtype->type);
	mono_upgrade_remote_class (((MonoObject*)tproxy)->vtable->domain, tproxy->remote_class, klass);
	((MonoObject*)tproxy)->vtable = tproxy->remote_class->vtable;
}

/**
 * mono_marshal_get_struct_to_ptr:
 * @klass:
 *
 * generates IL code for StructureToPtr (object structure, IntPtr ptr, bool fDeleteOld)
 */
MonoMethod *
mono_marshal_get_struct_to_ptr (MonoClass *klass)
{
	MonoMethodBuilder *mb;
	static MonoMethod *stoptr = NULL;
	MonoMethod *res;

	g_assert (klass != NULL);

	if (klass->str_to_ptr)
		return klass->str_to_ptr;

	if (!stoptr) 
		stoptr = mono_find_method_by_name (mono_defaults.marshal_class, "StructureToPtr", 3);
	g_assert (stoptr);

	mb = mono_mb_new (klass, stoptr->name, MONO_WRAPPER_UNKNOWN);

	if (klass->blittable) {
		mono_mb_emit_byte (mb, CEE_LDARG_1);
		mono_mb_emit_byte (mb, CEE_LDARG_0);
		mono_mb_emit_ldflda (mb, sizeof (MonoObject));
		mono_mb_emit_icon (mb, mono_class_value_size (klass, NULL));
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_CPBLK);
	} else {

		/* allocate local 0 (pointer) src_ptr */
		mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		/* allocate local 1 (pointer) dst_ptr */
		mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		/* allocate local 2 (boolean) delete_old */
		mono_mb_add_local (mb, &mono_defaults.boolean_class->byval_arg);
		mono_mb_emit_byte (mb, CEE_LDARG_2);
		mono_mb_emit_byte (mb, CEE_STLOC_2);

		/* initialize src_ptr to point to the start of object data */
		mono_mb_emit_byte (mb, CEE_LDARG_0);
		mono_mb_emit_ldflda (mb, sizeof (MonoObject));
		mono_mb_emit_byte (mb, CEE_STLOC_0);

		/* initialize dst_ptr */
		mono_mb_emit_byte (mb, CEE_LDARG_1);
		mono_mb_emit_byte (mb, CEE_STLOC_1);

		emit_struct_conv (mb, klass, FALSE);
	}

	mono_mb_emit_byte (mb, CEE_RET);

	res = mono_mb_create_method (mb, stoptr->signature, 0);
	mono_mb_free (mb);

	klass->str_to_ptr = res;
	return res;
}

/**
 * mono_marshal_get_ptr_to_struct:
 * @klass:
 *
 * generates IL code for PtrToStructure (IntPtr src, object structure)
 */
MonoMethod *
mono_marshal_get_ptr_to_struct (MonoClass *klass)
{
	MonoMethodBuilder *mb;
	static MonoMethod *ptostr = NULL;
	MonoMethod *res;

	g_assert (klass != NULL);

	if (klass->ptr_to_str)
		return klass->ptr_to_str;

	if (!ptostr) 
		ptostr = mono_find_method_by_name (mono_defaults.marshal_class, "PtrToStructure", 2);
	g_assert (ptostr);

	mb = mono_mb_new (klass, ptostr->name, MONO_WRAPPER_UNKNOWN);

	if (((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) || klass->blittable) {
		mono_mb_emit_byte (mb, CEE_LDARG_1);
		mono_mb_emit_ldflda (mb, sizeof (MonoObject));
		mono_mb_emit_byte (mb, CEE_LDARG_0);
		mono_mb_emit_icon (mb, mono_class_value_size (klass, NULL));
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_CPBLK);
	} else {

		/* allocate local 0 (pointer) src_ptr */
		mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		/* allocate local 1 (pointer) dst_ptr */
		mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		
		/* initialize src_ptr to point to the start of object data */
		mono_mb_emit_byte (mb, CEE_LDARG_0);
		mono_mb_emit_byte (mb, CEE_STLOC_0);

		/* initialize dst_ptr */
		mono_mb_emit_byte (mb, CEE_LDARG_1);
		mono_mb_emit_ldflda (mb, sizeof (MonoObject));
		mono_mb_emit_byte (mb, CEE_STLOC_1);

		emit_struct_conv (mb, klass, TRUE);
	}

	mono_mb_emit_byte (mb, CEE_RET);

	res = mono_mb_create_method (mb, ptostr->signature, 0);
	mono_mb_free (mb);

	klass->ptr_to_str = res;
	return res;
}

/*
 * generates IL code for the synchronized wrapper: the generated method
 * calls METHOD while locking 'this' or the parent type.
 */
MonoMethod *
mono_marshal_get_synchronized_wrapper (MonoMethod *method)
{
	static MonoMethodSignature *from_handle_sig = NULL;
	static MonoMethod *enter_method, *exit_method;
	MonoMethodSignature *sig;
	MonoExceptionClause *clause;
	MonoMethodHeader *header;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	GHashTable *cache;
	int i, pos, this_local, ret_local;

	g_assert (method);

	if (method->wrapper_type == MONO_WRAPPER_SYNCHRONIZED)
		return method;

	cache = method->klass->image->synchronized_cache;
	if ((res = mono_marshal_find_in_cache (cache, method)))
		return res;

	sig = method->signature;

	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_SYNCHRONIZED);

	/* result */
	if (!MONO_TYPE_IS_VOID (sig->ret))
		ret_local = mono_mb_add_local (mb, sig->ret);

	/* this */
	this_local = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);

	clause = g_new0 (MonoExceptionClause, 1);
	clause->flags = MONO_EXCEPTION_CLAUSE_FINALLY;

	if (!enter_method) {
		MonoMethodDesc *desc;

		desc = mono_method_desc_new ("Monitor:Enter", FALSE);
		enter_method = mono_method_desc_search_in_class (desc, mono_defaults.monitor_class);
		g_assert (enter_method);
		mono_method_desc_free (desc);
		desc = mono_method_desc_new ("Monitor:Exit", FALSE);
		exit_method = mono_method_desc_search_in_class (desc, mono_defaults.monitor_class);
		g_assert (exit_method);
		mono_method_desc_free (desc);

		/*
		 * GetTypeFromHandle isn't called as a managed method because it has
		 * a funky calling sequence, e.g. ldtoken+GetTypeFromHandle gets
		 * transformed into something else by the JIT.
		 */
		from_handle_sig = mono_metadata_signature_alloc (mono_defaults.corlib, 1);
		from_handle_sig->params [0] = &mono_defaults.object_class->byval_arg;
		from_handle_sig->ret = &mono_defaults.object_class->byval_arg;
	}

	/* Push this or the type object */
	if (method->flags & METHOD_ATTRIBUTE_STATIC) {
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_LDPTR);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, &method->klass->byval_arg));
		mono_mb_emit_native_call (mb, from_handle_sig, type_from_handle);
	}
	else
		mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_stloc (mb, this_local);

	/* Call Monitor::Enter() */
	mono_mb_emit_ldloc (mb, this_local);
	mono_mb_emit_managed_call (mb, enter_method, NULL);

	clause->try_offset = mb->pos;

	/* Call the method */
	if (sig->hasthis)
		mono_mb_emit_ldarg (mb, 0);
	for (i = 0; i < sig->param_count; i++)
		mono_mb_emit_ldarg (mb, i + (sig->hasthis == TRUE));
	mono_mb_emit_managed_call (mb, method, method->signature);
	if (!MONO_TYPE_IS_VOID (sig->ret))
		mono_mb_emit_stloc (mb, ret_local);

	mono_mb_emit_byte (mb, CEE_LEAVE);
	pos = mb->pos;
	mono_mb_emit_i4 (mb, 0);

	clause->try_len = mb->pos - clause->try_offset;
	clause->handler_offset = mb->pos;

	/* Call Monitor::Exit() */
	mono_mb_emit_ldloc (mb, this_local);
/*	mono_mb_emit_native_call (mb, exit_sig, mono_monitor_exit); */
	mono_mb_emit_managed_call (mb, exit_method, NULL);
	mono_mb_emit_byte (mb, CEE_ENDFINALLY);

	clause->handler_len = mb->pos - clause->handler_offset;

	mono_mb_patch_addr (mb, pos, mb->pos - (pos + 4));
	if (!MONO_TYPE_IS_VOID (sig->ret))
		mono_mb_emit_ldloc (mb, ret_local);
	mono_mb_emit_byte (mb, CEE_RET);

	res = mono_mb_create_and_cache (cache, method,
									mb, sig, sig->param_count + 16);
	mono_mb_free (mb);

	header = ((MonoMethodNormal *)res)->header;
	header->num_clauses = 1;
	header->clauses = clause;

	return res;	
}

MonoMethod*
mono_marshal_get_stelemref ()
{
	static MonoMethod* ret = NULL;
	MonoMethodSignature *sig;
	MonoMethodBuilder *mb;
	
	guint32 b1, b2, b3, b4;
	guint32 copy_pos;
	int aklass, vklass;
	
	if (ret)
		return ret;
	
	mb = mono_mb_new (mono_defaults.object_class, "stelemref", MONO_WRAPPER_STELEMREF);
	

	sig = mono_metadata_signature_alloc (mono_defaults.corlib, 3);

	/* void stelemref (void* array, int idx, void* value) */
	sig->ret = &mono_defaults.void_class->byval_arg;
	sig->params [0] = &mono_defaults.int_class->byval_arg;
	sig->params [1] = &mono_defaults.int_class->byval_arg; /* this is a natural sized int */
	sig->params [2] = &mono_defaults.int_class->byval_arg;
		
	aklass = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	vklass = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	
	/*
	the method:
	<check ABC>
	if (!value)
		goto store;
	
	aklass = array->vtable->klass->element_class;
	vklass = value->vtable->klass;
	
	if (vklass->idepth < aklass->idepth)
		goto long;
	
	if (vklass->supertypes [aklass->idepth - 1] != aklass)
		goto long;
	
	store:
		array [idx] = value;
		return;
	
	long:
		if (mono_object_isinst (value, aklass))
			goto store;
		
		throw new ArrayTypeMismatchException ();
	*/
	
	/* ABC */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldarg (mb, 1);
	mono_mb_emit_byte (mb, CEE_LDELEMA);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_defaults.int_class));
	mono_mb_emit_byte (mb, CEE_POP);
		
	/* if (!value) goto do_store */
	mono_mb_emit_ldarg (mb, 2);
	b1 = mono_mb_emit_branch (mb, CEE_BRFALSE);
	
	/* aklass = array->vtable->klass->element_class */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoObject, vtable));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoVTable, klass));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoClass, element_class));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_stloc (mb, aklass);
	
	/* vklass = value->vtable->klass */
	mono_mb_emit_ldarg (mb, 2);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoObject, vtable));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoVTable, klass));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_stloc (mb, vklass);
	
	/* if (vklass->idepth < aklass->idepth) goto failue */
	mono_mb_emit_ldloc (mb, vklass);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoClass, idepth));
	mono_mb_emit_byte (mb, CEE_LDIND_I4);
	
	mono_mb_emit_ldloc (mb, aklass);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoClass, idepth));
	mono_mb_emit_byte (mb, CEE_LDIND_I4);
	
	b2 = mono_mb_emit_branch (mb, CEE_BLT_UN);
	
	/* if (vklass->supertypes [aklass->idepth - 1] != aklass) goto failure */
	mono_mb_emit_ldloc (mb, vklass);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoClass, supertypes));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	
	mono_mb_emit_ldloc (mb, aklass);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoClass, idepth));
	mono_mb_emit_byte (mb, CEE_LDIND_I4);
	mono_mb_emit_icon (mb, 1);
	mono_mb_emit_byte (mb, CEE_SUB);
	mono_mb_emit_icon (mb, sizeof (void*));
	mono_mb_emit_byte (mb, CEE_MUL);
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	
	mono_mb_emit_ldloc (mb, aklass);
	
	b3 = mono_mb_emit_branch (mb, CEE_BNE_UN);
	
	copy_pos = mb->pos;
	/* do_store */
	mono_mb_patch_addr (mb, b1, mb->pos - (b1 + 4));
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldarg (mb, 1);
	mono_mb_emit_ldarg (mb, 2);
	mono_mb_emit_byte (mb, CEE_STELEM_I);
	
	mono_mb_emit_byte (mb, CEE_RET);
	
	/* the hard way */
	mono_mb_patch_addr (mb, b2, mb->pos - (b2 + 4));
	mono_mb_patch_addr (mb, b3, mb->pos - (b3 + 4));
	
	mono_mb_emit_ldarg (mb, 2);
	mono_mb_emit_ldloc (mb, aklass);
	mono_mb_emit_icall (mb, mono_object_isinst);
	
	b4 = mono_mb_emit_branch (mb, CEE_BRTRUE);
	mono_mb_patch_addr (mb, b4, copy_pos - (b4 + 4));
	mono_mb_emit_exception (mb, "ArrayTypeMismatchException", NULL);
	
	mono_mb_emit_byte (mb, CEE_RET);
	ret = mono_mb_create_method (mb, sig, 4);
	return ret;
}

/* FIXME: on win32 we should probably use GlobalAlloc(). */
void*
mono_marshal_alloc (gpointer size) 
{
	gpointer res;

	MONO_ARCH_SAVE_REGS;

	if ((gulong)size == 0)
		/* This returns a valid pointer for size 0 on MS.NET */
		size = (gpointer)4;

	res = g_try_malloc ((gulong)size);
	if (!res)
		mono_gc_out_of_memory ((gulong)size);

	return res;
}

void
mono_marshal_free (gpointer ptr) 
{
	MONO_ARCH_SAVE_REGS;

	g_free (ptr);
}

void
mono_marshal_free_array (gpointer *ptr, int size) 
{
	int i;

	if (!ptr)
		return;

	for (i = 0; i < size; i++)
		if (ptr [i])
			g_free (ptr [i]);
}

void *
mono_marshal_realloc (gpointer ptr, gpointer size) 
{
	MONO_ARCH_SAVE_REGS;

	return g_try_realloc (ptr, (gulong)size);
}

void *
mono_marshal_string_to_utf16 (MonoString *s)
{
	return s ? mono_string_chars (s) : NULL;
}

/**
 * mono_marshal_set_last_error:
 *
 * This function is invoked to set the last error value from a P/Invoke call
 * which has SetLastError set.
 */
void
mono_marshal_set_last_error (void)
{
#ifdef WIN32
	TlsSetValue (last_error_tls_id, (gpointer)GetLastError ());
#else
	TlsSetValue (last_error_tls_id, (gpointer)errno);
#endif
}

void
ves_icall_System_Runtime_InteropServices_Marshal_copy_to_unmanaged (MonoArray *src, gint32 start_index,
								    gpointer dest, gint32 length)
{
	int element_size;
	void *source_addr;

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (src);
	MONO_CHECK_ARG_NULL (dest);

	g_assert (src->obj.vtable->klass->rank == 1);
	g_assert (start_index >= 0);
	g_assert (length >= 0);
	g_assert (start_index + length <= mono_array_length (src));

	element_size = mono_array_element_size (src->obj.vtable->klass);
	  
	source_addr = mono_array_addr_with_size (src, element_size, start_index);

	memcpy (dest, source_addr, length * element_size);
}

void
ves_icall_System_Runtime_InteropServices_Marshal_copy_from_unmanaged (gpointer src, gint32 start_index,
								      MonoArray *dest, gint32 length)
{
	int element_size;
	void *dest_addr;

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (src);
	MONO_CHECK_ARG_NULL (dest);

	g_assert (dest->obj.vtable->klass->rank == 1);
	g_assert (start_index >= 0);
	g_assert (length >= 0);
	g_assert (start_index + length <= mono_array_length (dest));

	element_size = mono_array_element_size (dest->obj.vtable->klass);
	  
	dest_addr = mono_array_addr_with_size (dest, element_size, start_index);

	memcpy (dest_addr, src, length * element_size);
}

#if NO_UNALIGNED_ACCESS
#define RETURN_UNALIGNED(type, addr) \
	{ \
		type val; \
		memcpy(&val, p + offset, sizeof(val)); \
		return val; \
	}
#define WRITE_UNALIGNED(type, addr, val) \
	memcpy(addr, &val, sizeof(type))
#else
#define RETURN_UNALIGNED(type, addr) \
	return *(type*)(p + offset);
#define WRITE_UNALIGNED(type, addr, val) \
	(*(type *)(addr) = (val))
#endif

gpointer
ves_icall_System_Runtime_InteropServices_Marshal_ReadIntPtr (gpointer ptr, gint32 offset)
{
	char *p = ptr;

	MONO_ARCH_SAVE_REGS;

	RETURN_UNALIGNED(gpointer, p + offset);
}

unsigned char
ves_icall_System_Runtime_InteropServices_Marshal_ReadByte (gpointer ptr, gint32 offset)
{
	char *p = ptr;

	MONO_ARCH_SAVE_REGS;

	return *(unsigned char*)(p + offset);
}

gint16
ves_icall_System_Runtime_InteropServices_Marshal_ReadInt16 (gpointer ptr, gint32 offset)
{
	char *p = ptr;

	MONO_ARCH_SAVE_REGS;

	RETURN_UNALIGNED(gint16, p + offset);
}

gint32
ves_icall_System_Runtime_InteropServices_Marshal_ReadInt32 (gpointer ptr, gint32 offset)
{
	char *p = ptr;

	MONO_ARCH_SAVE_REGS;

	RETURN_UNALIGNED(gint32, p + offset);
}

gint64
ves_icall_System_Runtime_InteropServices_Marshal_ReadInt64 (gpointer ptr, gint32 offset)
{
	char *p = ptr;

	MONO_ARCH_SAVE_REGS;

	RETURN_UNALIGNED(gint64, p + offset);
}

void
ves_icall_System_Runtime_InteropServices_Marshal_WriteByte (gpointer ptr, gint32 offset, unsigned char val)
{
	char *p = ptr;

	MONO_ARCH_SAVE_REGS;

	*(unsigned char*)(p + offset) = val;
}

void
ves_icall_System_Runtime_InteropServices_Marshal_WriteIntPtr (gpointer ptr, gint32 offset, gpointer val)
{
	char *p = ptr;

	MONO_ARCH_SAVE_REGS;

	WRITE_UNALIGNED(gpointer, p + offset, val);
}

void
ves_icall_System_Runtime_InteropServices_Marshal_WriteInt16 (gpointer ptr, gint32 offset, gint16 val)
{
	char *p = ptr;

	MONO_ARCH_SAVE_REGS;

	WRITE_UNALIGNED(gint16, p + offset, val);
}

void
ves_icall_System_Runtime_InteropServices_Marshal_WriteInt32 (gpointer ptr, gint32 offset, gint32 val)
{
	char *p = ptr;

	MONO_ARCH_SAVE_REGS;

	WRITE_UNALIGNED(gint32, p + offset, val);
}

void
ves_icall_System_Runtime_InteropServices_Marshal_WriteInt64 (gpointer ptr, gint32 offset, gint64 val)
{
	char *p = ptr;

	MONO_ARCH_SAVE_REGS;

	WRITE_UNALIGNED(gint64, p + offset, val);
}

MonoString *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringAnsi (char *ptr)
{
	MONO_ARCH_SAVE_REGS;

	if (ptr == NULL)
		return mono_string_new (mono_domain_get (), "");
	else
		return mono_string_new (mono_domain_get (), ptr);
}

MonoString *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringAnsi_len (char *ptr, gint32 len)
{
	MONO_ARCH_SAVE_REGS;

	if (ptr == NULL)
		return mono_string_new (mono_domain_get (), "");
	else
		return mono_string_new_len (mono_domain_get (), ptr, len);
}

MonoString *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringUni (guint16 *ptr)
{
	MonoDomain *domain = mono_domain_get (); 
	int len = 0;
	guint16 *t = ptr;

	MONO_ARCH_SAVE_REGS;

	if (ptr == NULL)
		return mono_string_new (mono_domain_get (), "");

	while (*t++)
		len++;

	return mono_string_new_utf16 (domain, ptr, len);
}

MonoString *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringUni_len (guint16 *ptr, gint32 len)
{
	MonoDomain *domain = mono_domain_get (); 

	MONO_ARCH_SAVE_REGS;

	if (ptr == NULL)
		return mono_string_new (mono_domain_get (), "");
	else
		return mono_string_new_utf16 (domain, ptr, len);
}

MonoString *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringBSTR (gpointer ptr)
{
	MONO_ARCH_SAVE_REGS;

	g_warning ("PtrToStringBSTR not implemented");
	g_assert_not_reached ();

	return NULL;
}

guint32 
ves_icall_System_Runtime_InteropServices_Marshal_GetLastWin32Error (void)
{
	MONO_ARCH_SAVE_REGS;

	return ((guint32)TlsGetValue (last_error_tls_id));
}

guint32 
ves_icall_System_Runtime_InteropServices_Marshal_SizeOf (MonoReflectionType *rtype)
{
	MonoClass *klass;
	MonoType *type;
	guint32 layout;

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (rtype);

	type = rtype->type;
	klass = mono_class_from_mono_type (type);
	layout = (klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK);

	if (layout == TYPE_ATTRIBUTE_AUTO_LAYOUT) {
		gchar *msg;
		MonoException *exc;

		msg = g_strdup_printf ("Type %s cannot be marshaled as an unmanaged structure.", klass->name);
		exc = mono_get_exception_argument ("t", msg);
		g_free (msg);
		mono_raise_exception (exc);
	}


	return mono_class_native_size (klass, NULL);
}

void
ves_icall_System_Runtime_InteropServices_Marshal_StructureToPtr (MonoObject *obj, gpointer dst, MonoBoolean delete_old)
{
	MonoMethod *method;
	gpointer pa [3];

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (obj);
	MONO_CHECK_ARG_NULL (dst);

	method = mono_marshal_get_struct_to_ptr (obj->vtable->klass);

	pa [0] = obj;
	pa [1] = &dst;
	pa [2] = &delete_old;

	mono_runtime_invoke (method, NULL, pa, NULL);
}

void
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStructure (gpointer src, MonoObject *dst)
{
	MonoMethod *method;
	gpointer pa [2];

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (src);
	MONO_CHECK_ARG_NULL (dst);

	method = mono_marshal_get_ptr_to_struct (dst->vtable->klass);

	pa [0] = &src;
	pa [1] = dst;

	mono_runtime_invoke (method, NULL, pa, NULL);
}

MonoObject *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStructure_type (gpointer src, MonoReflectionType *type)
{
	MonoDomain *domain = mono_domain_get (); 
	MonoObject *res;

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (src);
	MONO_CHECK_ARG_NULL (type);

	res = mono_object_new (domain, mono_class_from_mono_type (type->type));

	ves_icall_System_Runtime_InteropServices_Marshal_PtrToStructure (src, res);

	return res;
}

int
ves_icall_System_Runtime_InteropServices_Marshal_OffsetOf (MonoReflectionType *type, MonoString *field_name)
{
	MonoMarshalType *info;
	MonoClass *klass;
	char *fname;
	int i, match_index = -1;
	
	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (type);
	MONO_CHECK_ARG_NULL (field_name);

	fname = mono_string_to_utf8 (field_name);
	klass = mono_class_from_mono_type (type->type);

	while (klass && match_index == -1) {
		for (i = 0; i < klass->field.count; ++i) {
			if (*fname == *klass->fields [i].name && strcmp (fname, klass->fields [i].name) == 0) {
				match_index = i;
				break;
			}
		}

		if (match_index == -1)
			klass = klass->parent;
        }

	g_free (fname);

	if(match_index == -1) {
		MonoException* exc;
		gchar *tmp;

		/* Get back original class instance */
		klass = mono_class_from_mono_type (type->type);

		tmp = g_strdup_printf ("Field passed in is not a marshaled member of the type %s", klass->name);
		exc = mono_get_exception_argument ("fieldName", tmp);
		g_free (tmp);
 
		mono_raise_exception ((MonoException*)exc);
	}

	info = mono_marshal_load_type_info (klass);     
	return info->fields [match_index].offset;
}

gpointer
ves_icall_System_Runtime_InteropServices_Marshal_StringToHGlobalAnsi (MonoString *string)
{
	MONO_ARCH_SAVE_REGS;

	return mono_string_to_utf8 (string);
}

gpointer
ves_icall_System_Runtime_InteropServices_Marshal_StringToHGlobalUni (MonoString *string)
{
	MONO_ARCH_SAVE_REGS;

	if (string == NULL)
		return NULL;
	else
		return g_memdup (mono_string_chars (string), mono_string_length (string)*2);
}

static void
mono_struct_delete_old (MonoClass *klass, char *ptr)
{
	MonoMarshalType *info;
	int i;

	info = mono_marshal_load_type_info (klass);

	for (i = 0; i < info->num_fields; i++) {
		MonoMarshalNative ntype;
		MonoMarshalConv conv;
		MonoType *ftype = info->fields [i].field->type;
		char *cpos;

		if (ftype->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;

		ntype = mono_type_to_unmanaged (ftype, info->fields [i].mspec, TRUE, 
						klass->unicode, &conv);
			
		cpos = ptr + info->fields [i].offset;

		switch (conv) {
		case MONO_MARSHAL_CONV_NONE:
			if (MONO_TYPE_ISSTRUCT (ftype)) {
				mono_struct_delete_old (ftype->data.klass, cpos);
				continue;
			}
			break;
		case MONO_MARSHAL_CONV_STR_LPWSTR:
		case MONO_MARSHAL_CONV_STR_LPSTR:
		case MONO_MARSHAL_CONV_STR_LPTSTR:
		case MONO_MARSHAL_CONV_STR_BSTR:
		case MONO_MARSHAL_CONV_STR_ANSIBSTR:
		case MONO_MARSHAL_CONV_STR_TBSTR:
			g_free (*(gpointer *)cpos);
			break;
		default:
			continue;
		}
	}
}

void
ves_icall_System_Runtime_InteropServices_Marshal_DestroyStructure (gpointer src, MonoReflectionType *type)
{
	MonoClass *klass;

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (src);
	MONO_CHECK_ARG_NULL (type);

	klass = mono_class_from_mono_type (type->type);

	mono_struct_delete_old (klass, (char *)src);
}

void*
ves_icall_System_Runtime_InteropServices_Marshal_AllocCoTaskMem (int size)
{
	/* FIXME: Call AllocCoTaskMem under windows */
	MONO_ARCH_SAVE_REGS;

	return g_try_malloc ((gulong)size);
}

void
ves_icall_System_Runtime_InteropServices_Marshal_FreeCoTaskMem (void *ptr)
{
	/* FIXME: Call FreeCoTaskMem under windows */
	MONO_ARCH_SAVE_REGS;

	g_free (ptr);
}

void*
ves_icall_System_Runtime_InteropServices_Marshal_UnsafeAddrOfPinnedArrayElement (MonoArray *arrayobj, int index)
{
	return mono_array_addr_with_size (arrayobj, mono_array_element_size (arrayobj->obj.vtable->klass), index);
}

MonoMarshalType *
mono_marshal_load_type_info (MonoClass* klass)
{
	int i, j, count = 0, native_size = 0, min_align = 1;
	MonoMarshalType *info;
	guint32 layout;

	g_assert (klass != NULL);

	if (klass->marshal_info)
		return klass->marshal_info;

	if (!klass->inited)
		mono_class_init (klass);
	
	for (i = 0; i < klass->field.count; ++i) {
		if (klass->fields [i].type->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;
		if (mono_field_is_deleted (&klass->fields [i]))
			continue;
		count++;
	}

	layout = klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK;

	klass->marshal_info = info = g_malloc0 (sizeof (MonoMarshalType) + sizeof (MonoMarshalField) * count);
	info->num_fields = count;
	
	/* Try to find a size for this type in metadata */
	mono_metadata_packing_from_typedef (klass->image, klass->type_token, NULL, &native_size);

	if (klass->parent) {
		int parent_size = mono_class_native_size (klass->parent, NULL);

		/* Add parent size to real size */
		native_size += parent_size;
		info->native_size = parent_size;
	}
 
	for (j = i = 0; i < klass->field.count; ++i) {
		int size, align;
		
		if (klass->fields [i].type->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;

		if (mono_field_is_deleted (&klass->fields [i]))
			continue;
		if (klass->fields [i].type->attrs & FIELD_ATTRIBUTE_HAS_FIELD_MARSHAL)
			mono_metadata_field_info (klass->image, klass->field.first + i, 
						  NULL, NULL, &info->fields [j].mspec);

		info->fields [j].field = &klass->fields [i];

		if ((klass->field.count == 1) && (klass->instance_size == sizeof (MonoObject)) &&
			(strcmp (klass->fields [i].name, "$PRIVATE$") == 0)) {
			/* This field is a hack inserted by MCS to empty structures */
			continue;
		}

		switch (layout) {
		case TYPE_ATTRIBUTE_AUTO_LAYOUT:
		case TYPE_ATTRIBUTE_SEQUENTIAL_LAYOUT:
			size = mono_marshal_type_size (klass->fields [i].type, info->fields [j].mspec, 
						       &align, TRUE, klass->unicode);
			align = klass->packing_size ? MIN (klass->packing_size, align): align;
			min_align = MAX (align, min_align);
			info->fields [j].offset = info->native_size;
			info->fields [j].offset += align - 1;
			info->fields [j].offset &= ~(align - 1);
			info->native_size = info->fields [j].offset + size;
			break;
		case TYPE_ATTRIBUTE_EXPLICIT_LAYOUT:
			size = mono_marshal_type_size (klass->fields [i].type, info->fields [j].mspec, 
						       &align, TRUE, klass->unicode);
			align = klass->packing_size ? MIN (klass->packing_size, align): align;
			min_align = MAX (align, min_align);
			info->fields [j].offset = klass->fields [i].offset - sizeof (MonoObject);
			info->native_size = MAX (info->native_size, info->fields [j].offset + size);
			break;
		}	
		j++;
	}

	if(layout != TYPE_ATTRIBUTE_AUTO_LAYOUT) {
		info->native_size = MAX (native_size, info->native_size);
	}

	if (info->native_size & (min_align - 1)) {
		info->native_size += min_align - 1;
		info->native_size &= ~(min_align - 1);
	}

	/* Update the class's blittable info, if the layouts don't match */
	if (info->native_size != mono_class_value_size (klass, NULL))
		klass->blittable = FALSE;

	/* If this is an array type, ensure that we have element info */
	if (klass->element_class) {
		mono_marshal_load_type_info (klass->element_class);
	}

	return klass->marshal_info;
}

/**
 * mono_class_native_size:
 * @klass: a class 
 * 
 * Returns: the native size of an object instance (when marshaled 
 * to unmanaged code) 
 */
gint32
mono_class_native_size (MonoClass *klass, guint32 *align)
{
	
	if (!klass->marshal_info)
		mono_marshal_load_type_info (klass);

	if (align)
		*align = klass->min_align;

	return klass->marshal_info->native_size;
}

/*
 * mono_type_native_stack_size:
 * @t: the type to return the size it uses on the stack
 *
 * Returns: the number of bytes required to hold an instance of this
 * type on the native stack
 */
int
mono_type_native_stack_size (MonoType *t, gint *align)
{
	int tmp;

	g_assert (t != NULL);

	if (!align)
		align = &tmp;

	if (t->byref) {
		*align = 4;
		return 4;
	}

	switch (t->type){
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_TYPEDBYREF:
		*align = 4;
		return 4;
	case MONO_TYPE_R4:
		*align = 4;
		return 4;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_R8:
		*align = 4;
		return 8;
	case MONO_TYPE_VALUETYPE: {
		guint32 size;

		if (t->data.klass->enumtype)
			return mono_type_native_stack_size (t->data.klass->enum_basetype, align);
		else {
			size = mono_class_native_size (t->data.klass, align);
			*align = *align + 3;
			*align &= ~3;
			
			size +=  3;
			size &= ~3;

			return size;
		}
	}
	default:
		g_error ("type 0x%02x unknown", t->type);
	}
	return 0;
}

/* __alignof__ returns the preferred alignment of values not the actual alignment used by
   the compiler so is wrong e.g. for Linux where doubles are aligned on a 4 byte boundary
   but __alignof__ returns 8 - using G_STRUCT_OFFSET works better */
#define ALIGNMENT(type) G_STRUCT_OFFSET(struct { char c; type x; }, x)

gint32
mono_marshal_type_size (MonoType *type, MonoMarshalSpec *mspec, gint32 *align, 
			gboolean as_field, gboolean unicode)
{
	MonoMarshalNative native_type = mono_type_to_unmanaged (type, mspec, as_field, unicode, NULL);
	MonoClass *klass;

	switch (native_type) {
	case MONO_NATIVE_BOOLEAN:
		*align = 4;
		return 4;
	case MONO_NATIVE_I1:
	case MONO_NATIVE_U1:
		*align = 1;
		return 1;
	case MONO_NATIVE_I2:
	case MONO_NATIVE_U2:
	case MONO_NATIVE_VARIANTBOOL:
		*align = 2;
		return 2;
	case MONO_NATIVE_I4:
	case MONO_NATIVE_U4:
	case MONO_NATIVE_ERROR:
		*align = 4;
		return 4;
	case MONO_NATIVE_I8:
	case MONO_NATIVE_U8:
		*align = ALIGNMENT(guint64);
		return 8;
	case MONO_NATIVE_R4:
		*align = 4;
		return 4;
	case MONO_NATIVE_R8:
		*align = ALIGNMENT(double);
		return 8;
	case MONO_NATIVE_INT:
	case MONO_NATIVE_UINT:
	case MONO_NATIVE_LPSTR:
	case MONO_NATIVE_LPWSTR:
	case MONO_NATIVE_LPTSTR:
	case MONO_NATIVE_BSTR:
	case MONO_NATIVE_ANSIBSTR:
	case MONO_NATIVE_TBSTR:
	case MONO_NATIVE_LPARRAY:
	case MONO_NATIVE_SAFEARRAY:
	case MONO_NATIVE_IUNKNOWN:
	case MONO_NATIVE_IDISPATCH:
	case MONO_NATIVE_INTERFACE:
	case MONO_NATIVE_ASANY:
	case MONO_NATIVE_FUNC:
	case MONO_NATIVE_LPSTRUCT:
		*align = ALIGNMENT(gpointer);
		return sizeof (gpointer);
	case MONO_NATIVE_STRUCT: 
		klass = mono_class_from_mono_type (type);
		return mono_class_native_size (klass, align);
	case MONO_NATIVE_BYVALTSTR: {
		int esize = unicode ? 2: 1;
		g_assert (mspec);
		*align = esize;
		return mspec->data.array_data.num_elem * esize;
	}
	case MONO_NATIVE_BYVALARRAY: {
		int esize;
		klass = mono_class_from_mono_type (type);
		esize = mono_class_native_size (klass->element_class, align);
		g_assert (mspec);
		return mspec->data.array_data.num_elem * esize;
	}
	case MONO_NATIVE_CUSTOM:
		g_assert_not_reached ();
		break;
	case MONO_NATIVE_CURRENCY:
	case MONO_NATIVE_VBBYREFSTR:
	default:
		g_error ("native type %02x not implemented", native_type); 
		break;
	}
	g_assert_not_reached ();
	return 0;
}

gpointer
mono_marshal_asany (MonoObject *o, MonoMarshalNative string_encoding)
{
	MonoType *t;
	MonoClass *klass;

	if (o == NULL)
		return NULL;

	t = &o->vtable->klass->byval_arg;
	switch (t->type) {
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_PTR:
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
		return mono_object_unbox (o);
		break;
	case MONO_TYPE_STRING:
		switch (string_encoding) {
		case MONO_NATIVE_LPWSTR:
			return mono_string_to_utf16 ((MonoString*)o);
			break;
		case MONO_NATIVE_LPSTR:
			return mono_string_to_utf8 ((MonoString*)o);
			break;
		default:
			g_warning ("marshaling conversion %d not implemented", string_encoding);
			g_assert_not_reached ();
		}
		break;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_VALUETYPE: {
		MonoMethod *method;
		gpointer pa [3];
		gpointer res;
		MonoBoolean delete_old = FALSE;

		klass = t->data.klass;

		if ((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_AUTO_LAYOUT)
			break;

		if (((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) ||
			klass->blittable || klass->enumtype)
			return mono_object_unbox (o);

		res = g_malloc0 (mono_class_native_size (klass, NULL));

		method = mono_marshal_get_struct_to_ptr (o->vtable->klass);

		pa [0] = o;
		pa [1] = &res;
		pa [2] = &delete_old;

		mono_runtime_invoke (method, NULL, pa, NULL);

		return res;
	}
	}

	mono_raise_exception (mono_get_exception_argument ("", "No PInvoke conversion exists for value passed to Object-typed parameter."));

	return NULL;
}

void
mono_marshal_free_asany (MonoObject *o, gpointer ptr, MonoMarshalNative string_encoding)
{
	MonoType *t;
	MonoClass *klass;

	if (o == NULL)
		return;

	t = &o->vtable->klass->byval_arg;
	switch (t->type) {
	case MONO_TYPE_STRING:
		switch (string_encoding) {
		case MONO_NATIVE_LPWSTR:
			g_free (ptr);
			break;
		case MONO_NATIVE_LPSTR:
			g_free (ptr);
			break;
		default:
			g_warning ("marshaling conversion %d not implemented", string_encoding);
			g_assert_not_reached ();
		}
		break;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_VALUETYPE: {
		klass = t->data.klass;

		if (((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) ||
			klass->blittable || klass->enumtype)
			break;

		mono_struct_delete_old (klass, ptr);

		g_free (ptr);
		break;
	}
	default:
		break;
	}
}
