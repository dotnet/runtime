/*
 * delegate.c: delegate support functions
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>

#include <mono/arch/x86/x86-codegen.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threadpool.h>

#include "jit.h"
#include "codegen.h"

gpointer 
arch_begin_invoke (MonoDelegate *delegate, ...)
{
	MonoMethodMessage *msg;
	MonoDelegate *async_callback;
	MonoObject *state;
	MonoMethod *im;
	MonoClass *klass;
	MonoMethod *method = NULL;
	int i;

	g_assert (delegate);

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
	msg = arch_method_call_message_new (method, &delegate, im, &async_callback, &state);

	return mono_thread_pool_add ((MonoObject *)delegate, msg, async_callback, state);
}

static void
real_arch_end_invoke (MonoDelegate *delegate, gpointer first_arg)
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

	msg = arch_method_call_message_new (method, first_arg, NULL, NULL, NULL);

	ares = mono_array_get (msg->args, gpointer, sig->param_count - 1);
	g_assert (ares);

	res = mono_thread_pool_finish (ares, &out_args, &exc);

	if (exc) {
		char *strace = mono_string_to_utf8 (((MonoException*)exc)->stack_trace);
		char  *tmp;
		tmp = g_strdup_printf ("%s\nException Rethrown at:\n", strace);
		g_free (strace);	
		((MonoException*)exc)->stack_trace = mono_string_new (domain, tmp);
		g_free (tmp);
		mono_raise_exception ((MonoException*)exc);
	}

	/* restore return value */
	if (method->signature->ret->type != MONO_TYPE_VOID) {
		g_assert (res);
		arch_method_return_message_restore (method, first_arg, res, out_args);
	}
}

void
arch_end_invoke (gpointer first_arg, ...)
{
	return real_arch_end_invoke ((MonoDelegate *)first_arg, &first_arg);
}

void
arch_end_invoke_vt (gpointer first_arg, MonoObject *sec_arg, ...)
{
	return real_arch_end_invoke ((MonoDelegate *)sec_arg, &first_arg);
}
