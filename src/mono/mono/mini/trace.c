/**
 * \file
 * Tracing facilities for the Mono Runtime.
 *
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#ifdef HAVE_ALLOCA_H
#include <alloca.h>
#endif
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#include <string.h>
#include "mini.h"
#include "mini-runtime.h"
#include <mono/metadata/debug-helpers.h>
#include <mono/utils/mono-time.h>
#include <mono/utils/mono-memory-model.h>
#include "trace.h"
#include <mono/metadata/callspec.h>

#if _MSC_VER
#pragma warning(disable:4312) // FIXME pointer cast to different size
#endif

#if defined (HOST_ANDROID) || defined (TARGET_IOS)
#  undef printf
#  define printf(...) g_log("mono", G_LOG_LEVEL_MESSAGE, __VA_ARGS__)
#  undef fprintf
#  define fprintf(__ignore, ...) g_log ("mono-gc", G_LOG_LEVEL_MESSAGE, __VA_ARGS__)
#endif

static MonoCallSpec trace_spec;

static volatile gint32 output_lock = 0;

gboolean mono_trace_eval_exception (MonoClass *klass)
{
	return mono_callspec_eval_exception (klass, &trace_spec);
}

gboolean mono_trace_eval (MonoMethod *method)
{
	return mono_callspec_eval (method, &trace_spec);
}

MonoCallSpec *mono_trace_set_options (const char *options)
{
	char *errstr;
	if (!mono_callspec_parse (options, &trace_spec, &errstr)) {
		fprintf (stderr, "%s\n", errstr);
		g_free (errstr);
		return NULL;
	}

	return &trace_spec;
}

static
#ifdef MONO_KEYWORD_THREAD
MONO_KEYWORD_THREAD
#endif
int indent_level = 0;
static guint64 start_time = 0;

static double seconds_since_start (void)
{
	guint64 diff = mono_100ns_ticks () - start_time;
	return diff/10000000.0;
}

static void indent (int diff) {
	if (diff < 0)
		indent_level += diff;
	if (start_time == 0)
		start_time = mono_100ns_ticks ();
	printf ("[%p: %.5f %d] ", (void*)mono_native_thread_id_get (), seconds_since_start (), indent_level);
	if (diff > 0)
		indent_level += diff;
}

static char *
string_to_utf8 (MonoString *s)
{
	char *as;
	GError *gerror = NULL;

	g_assert (s);

	if (!s->length)
		return g_strdup ("");

	as = g_utf16_to_utf8 (mono_string_chars_internal (s), s->length, NULL, NULL, &gerror);
	if (gerror) {
		/* Happens with StringBuilders */
		g_error_free (gerror);
		return g_strdup ("<INVALID UTF8>");
	}
	else
		return as;
}

/*
 * This used to be endianness sensitive due to the stack, but since the change
 * to using the profiler to get an argument, it can be dereferenced as a
 * pointer of the specified type, regardless of endian.
 */
#define arg_in_stack_slot(cpos, type) ((type *)(cpos))

static gboolean
is_gshared_vt_wrapper (MonoMethod *m)
{
	if (m->wrapper_type != MONO_WRAPPER_OTHER)
		return FALSE;
	return !strcmp (m->name, "interp_in") || !strcmp (m->name, "gsharedvt_out_sig");
}

/* ENTER:i <- interp
 * ENTER:c <- compiled (JIT or AOT)
 * ENTER:u <- no JitInfo available
 */
static char
frame_kind (MonoJitInfo *ji)
{
	if (!ji)
		return 'u';

	if (ji->is_interp)
		return 'i';

	return 'c';
}

void
mono_trace_enter_method (MonoMethod *method, MonoJitInfo *ji, MonoProfilerCallContext *ctx)
{
	int i;
	MonoClass *klass;
	MonoObject *o;
	MonoMethodSignature *sig;
	char *fname;
	MonoGenericSharingContext *gsctx = NULL;

	if (!trace_spec.enabled)
		return;

	fname = mono_method_full_name (method, TRUE);
	indent (1);

	while (output_lock != 0 || mono_atomic_cas_i32 (&output_lock, 1, 0) != 0)
		mono_thread_info_yield ();


	/* FIXME: Might be better to pass the ji itself */
	if (!ji)
		ji = mini_jit_info_table_find (mono_domain_get (), (char *)MONO_RETURN_ADDRESS (), NULL);

	printf ("ENTER:%c %s(", frame_kind (ji), fname);
	g_free (fname);

	sig = mono_method_signature_internal (method);

	if (method->is_inflated && ji) {
		gsctx = mono_jit_info_get_generic_sharing_context (ji);
		if (gsctx && gsctx->is_gsharedvt) {
			/* Needs a ctx to get precise method */
			printf (") <gsharedvt>\n");
			mono_atomic_store_release (&output_lock, 0);
			return;
		}
	}

	if (sig->hasthis) {
		void *this_buf = mini_profiler_context_get_this (ctx);
		if (m_class_is_valuetype (method->klass) || is_gshared_vt_wrapper (method)) {
			printf ("value:%p", this_buf);
		} else {
			MonoObject *o = *(MonoObject**)this_buf;

			if (o) {
				klass = o->vtable->klass;

				if (klass == mono_defaults.string_class) {
					MonoString *s = (MonoString*)o;
					char *as = string_to_utf8 (s);

					printf ("this:[STRING:%p:%s]", o, as);
					g_free (as);
				} else if (klass == mono_defaults.runtimetype_class) {
					printf ("[this:[TYPE:%p:%s]]", o, mono_type_full_name (((MonoReflectionType*)o)->type));
				} else {
					printf ("this:%p[%s.%s %s]", o, m_class_get_name_space (klass), m_class_get_name (klass), o->vtable->domain->friendly_name);
				}
			} else {
				printf ("this:NULL");
			}
		}
		if (sig->param_count)
			printf (", ");
		mini_profiler_context_free_buffer (this_buf);
	}

	for (i = 0; i < sig->param_count; ++i) {
		gpointer buf = mini_profiler_context_get_argument (ctx, i);

		MonoType *type = sig->params [i];

		if (type->byref) {
			printf ("[BYREF:%p]", *(gpointer*)buf);
			mini_profiler_context_free_buffer (buf);
			break;
		}

		switch (mini_get_underlying_type (type)->type) {
		case MONO_TYPE_I:
		case MONO_TYPE_U:
			printf ("%p", *arg_in_stack_slot(buf, gpointer *));
			break;
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
			printf ("%d", *arg_in_stack_slot(buf, gint8));
			break;
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
			printf ("%d", *arg_in_stack_slot(buf, gint16));
			break;
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
			printf ("%d", *arg_in_stack_slot(buf, int));
			break;
		case MONO_TYPE_STRING: {
			MonoString *s = *arg_in_stack_slot(buf, MonoString *);
			if (s) {
				char *as;

				g_assert (((MonoObject *)s)->vtable->klass == mono_defaults.string_class);
				as = string_to_utf8 (s);

				printf ("[STRING:%p:%s]", s, as);
				g_free (as);
			} else 
				printf ("[STRING:null]");
			break;
		}
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT: {
			o = *arg_in_stack_slot(buf, MonoObject *);
			if (o) {
				klass = o->vtable->klass;

				gpointer data = mono_object_get_data (o);
				if (klass == mono_defaults.string_class) {
					char *as = string_to_utf8 ((MonoString*)o);

					printf ("[STRING:%p:%s]", o, as);
					g_free (as);
				} else if (klass == mono_defaults.int32_class) {
					printf ("[INT32:%p:%d]", o, *(gint32 *)data);
				} else if (klass == mono_defaults.runtimetype_class) {
					printf ("[TYPE:%s]", mono_type_full_name (((MonoReflectionType*)o)->type));
				} else if (m_class_get_rank (klass)) {
					MonoArray *arr = (MonoArray*)o;
					printf ("[%s.%s:[%d]%p]", m_class_get_name_space (klass), m_class_get_name (klass), mono_array_length_internal (arr), o);
				} else
					printf ("[%s.%s:%p]", m_class_get_name_space (klass), m_class_get_name (klass), o);
			} else {
				printf ("%p", *arg_in_stack_slot(buf, gpointer));
			}
			break;
		}
		case MONO_TYPE_PTR:
		case MONO_TYPE_FNPTR:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
			printf ("%p", *arg_in_stack_slot(buf, gpointer));
			break;
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
			printf ("0x%016" PRIx64, (gint64)*arg_in_stack_slot(buf, gint64));
			break;
		case MONO_TYPE_R4:
			printf ("%f", *arg_in_stack_slot(buf, float));
			break;
		case MONO_TYPE_R8:
			printf ("%f", *arg_in_stack_slot(buf, double));
			break;
		case MONO_TYPE_VALUETYPE: {
			int j, size, align;
			size = mono_type_size (type, &align);
			printf ("[");
			for (j = 0; j < size; j++)
				printf ("%02x,", *((guint8*)buf +j));
			printf ("]");
			break;
		}
		default:
			printf ("XX");
		}
		if (i + 1 < sig->param_count)
			printf (", ");
		mini_profiler_context_free_buffer (buf);
	}

	printf (")\n");
	fflush (stdout);

	mono_atomic_store_release (&output_lock, 0);
}

void
mono_trace_leave_method (MonoMethod *method, MonoJitInfo *ji, MonoProfilerCallContext *ctx)
{
	MonoType *type;
	char *fname;
	MonoGenericSharingContext *gsctx;

	if (!trace_spec.enabled)
		return;

	fname = mono_method_full_name (method, TRUE);
	indent (-1);

	while (output_lock != 0 || mono_atomic_cas_i32 (&output_lock, 1, 0) != 0)
		mono_thread_info_yield ();

	/* FIXME: Might be better to pass the ji itself from the JIT */
	if (!ji)
		ji = mini_jit_info_table_find (mono_domain_get (), (char *)MONO_RETURN_ADDRESS (), NULL);

	printf ("LEAVE:%c %s(", frame_kind (ji), fname);
	g_free (fname);

	if (method->is_inflated && ji) {
		gsctx = mono_jit_info_get_generic_sharing_context (ji);
		if (gsctx && gsctx->is_gsharedvt) {
			/* Needs a ctx to get precise method */
			printf (") <gsharedvt>\n");
			mono_atomic_store_release (&output_lock, 0);
			return;
		}
	}

	type = mini_get_underlying_type (mono_method_signature_internal (method)->ret);

	gpointer buf = mini_profiler_context_get_result (ctx);
	switch (type->type) {
	case MONO_TYPE_VOID:
		break;
	case MONO_TYPE_I1:
	case MONO_TYPE_U1: {
		gint8 res = *arg_in_stack_slot (buf, gint8);
		printf ("result=%d", res);
		break;
	}
	case MONO_TYPE_I2:
	case MONO_TYPE_U2: {
		gint16 res = *arg_in_stack_slot (buf, gint16);
		printf ("result=%d", res);
		break;
	}
	case MONO_TYPE_I4:
	case MONO_TYPE_U4: {
		int res = *arg_in_stack_slot (buf, int);
		printf ("result=%d", res);
		break;
	}
	case MONO_TYPE_PTR:
	case MONO_TYPE_I:
	case MONO_TYPE_U: {
		gpointer res = *arg_in_stack_slot (buf, gpointer);
		printf ("result=%p", res);
		break;
	}
	case MONO_TYPE_OBJECT: {
		MonoObject *o = *arg_in_stack_slot (buf, MonoObject*);

		if (o) {
			gpointer data = mono_object_get_data (o);
			if (o->vtable->klass == mono_defaults.boolean_class) {
				printf ("[BOOLEAN:%p:%d]", o, *(guint8 *)data);
			} else if  (o->vtable->klass == mono_defaults.int32_class) {
				printf ("[INT32:%p:%d]", o, *(gint32 *)data);
			} else if  (o->vtable->klass == mono_defaults.int64_class) {
				printf ("[INT64:%p:%" PRId64 "]", o, *(gint64 *)data);
			} else if (o->vtable->klass == mono_defaults.string_class) {
				char *as;
				as = string_to_utf8 ((MonoString*)o);
				printf ("[STRING:%p:%s]", o, as);
			} else {
				printf ("[%s.%s:%p]", m_class_get_name_space (mono_object_class (o)), m_class_get_name (mono_object_class (o)), o);
			}
		} else
			printf ("[OBJECT:%p]", o);
	       
		break;
	}
	case MONO_TYPE_I8: {
		gint64 l =  *arg_in_stack_slot (buf, gint64);
		printf ("lresult=0x%16" PRIx64, l);
		break;
	}
	case MONO_TYPE_U8: {
		gint64 l =  *arg_in_stack_slot (buf, gint64);
		printf ("lresult=0x%16" PRIx64, l);
		break;
	}
	case MONO_TYPE_R4:
	case MONO_TYPE_R8: {
		double f = *arg_in_stack_slot (buf, double);
		printf ("FP=%f", f);
		break;
	}
	case MONO_TYPE_VALUETYPE:  {
		guint8 *p = (guint8 *)buf;
		int j, size, align;
		size = mono_type_size (type, &align);
		printf ("[");
		for (j = 0; p && j < size; j++)
			printf ("%02x,", p [j]);
		printf ("]");
		break;
	}
	default:
		printf ("(unknown return type %x)", mono_method_signature_internal (method)->ret->type);
	}
	mini_profiler_context_free_buffer (buf);

	//printf (" ip: %p\n", MONO_RETURN_ADDRESS_N (1));
	printf ("\n");
	fflush (stdout);

	mono_atomic_store_release (&output_lock, 0);
}

void
mono_trace_tail_method (MonoMethod *method, MonoJitInfo *ji, MonoMethod *target)
{
	char *fname, *tname;

	if (!trace_spec.enabled)
		return;

	fname = mono_method_full_name (method, TRUE);
	tname = mono_method_full_name (target, TRUE);
	indent (-1);

	while (output_lock != 0 || mono_atomic_cas_i32 (&output_lock, 1, 0) != 0)
		mono_thread_info_yield ();

	/* FIXME: Might be better to pass the ji itself from the JIT */
	if (!ji)
		ji = mini_jit_info_table_find (mono_domain_get (), (char *)MONO_RETURN_ADDRESS (), NULL);

	printf ("TAILC:%c %s->%s\n", frame_kind (ji), fname, tname);
	fflush (stdout);

	g_free (fname);
	g_free (tname);

	mono_atomic_store_release (&output_lock, 0);
}

void
mono_trace_enable (gboolean enable)
{
	trace_spec.enabled = enable;
}

gboolean
mono_trace_is_enabled ()
{
	return trace_spec.enabled;
}
