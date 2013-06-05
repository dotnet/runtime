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
#include <mono/metadata/monitor.h>
#include <mono/metadata/threads-types.h>

static gboolean shutting_down = FALSE;

/** 
 * mono_runtime_set_shutting_down:
 *
 * Invoked by System.Environment.Exit to flag that the runtime
 * is shutting down.
 */
void
mono_runtime_set_shutting_down (void)
{
	shutting_down = TRUE;
}

/**
 * mono_runtime_is_shutting_down:
 *
 * Returns whether the runtime has been flagged for shutdown.
 *
 * This is consumed by the P:System.Environment.HasShutdownStarted
 * property.
 *
 */
gboolean
mono_runtime_is_shutting_down (void)
{
	return shutting_down;
}

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

static void
mono_runtime_fire_process_exit_event (void)
{
#ifndef MONO_CROSS_COMPILE
	mono_domain_foreach (fire_process_exit_event, NULL);
#endif
}

/*
Initialize runtime shutdown.
After this call completes the thread pool will stop accepting new jobs and

*/
void
mono_runtime_shutdown (void)
{
	mono_runtime_fire_process_exit_event ();

	mono_threads_set_shutting_down ();

	/* No new threads will be created after this point */

	mono_runtime_set_shutting_down ();

}


gboolean
mono_runtime_is_critical_method (MonoMethod *method)
{
	if (mono_monitor_is_il_fastpath_wrapper (method))
		return TRUE;
	return FALSE;
}
