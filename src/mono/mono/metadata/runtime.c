/*
 * runtime.c: Runtime functions
 *
 * Authors:
 *  Jonathan Pryor 
 *
 * Copyright 2010 Novell, Inc (http://www.novell.com)
 */

#include <config.h>

#include <glib.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/class.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/runtime.h>

static void
fire_process_exit_event (MonoDomain *domain, gpointer user_data)
{
	MonoClassField *field;
	gpointer pa [2];
	MonoObject *delegate, *exc;

	field = mono_class_get_field_from_name (mono_defaults.appdomain_class, "ProcessExit");
	g_assert (field);

	delegate = *(MonoObject **)(((char *)domain->domain) + field->offset); 
	if (delegate == NULL)
		return;

	pa [0] = domain;
	pa [1] = NULL;
	mono_runtime_delegate_invoke (delegate, pa, &exc);
}

void
mono_runtime_shutdown (void)
{
	mono_domain_foreach (fire_process_exit_event, NULL);
}

