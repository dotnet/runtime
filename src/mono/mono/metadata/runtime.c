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
#include <mono/metadata/threadpool.h>
#include <mono/metadata/marshal.h>
#include <mono/utils/atomic.h>

static gboolean shutting_down_inited = FALSE;
static gboolean shutting_down = FALSE;

/** 
 * mono_runtime_set_shutting_down:
 *
 * Invoked by System.Environment.Exit to flag that the runtime
 * is shutting down.
 *
 * Deprecated. This function can break the shutdown sequence.
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
 * Try to initialize runtime shutdown.
 * After this call completes the thread pool will stop accepting new jobs and no further threads will be created.
 *
 * @return true if shutdown was initiated by this call or false is other thread beat this one
 */
gboolean
mono_runtime_try_shutdown (void)
{
	if (InterlockedCompareExchange (&shutting_down_inited, TRUE, FALSE))
		return FALSE;

	mono_runtime_fire_process_exit_event ();

	shutting_down = TRUE;

	mono_threads_set_shutting_down ();

	/* No new threads will be created after this point */

	mono_runtime_set_shutting_down ();

	/* This will kill the tp threads which cannot be suspended */
	mono_thread_pool_cleanup ();

	/*TODO move the follow to here:
	mono_thread_suspend_all_other_threads (); OR  mono_thread_wait_all_other_threads

	mono_runtime_quit ();
	*/

	return TRUE;
}


gboolean
mono_runtime_is_critical_method (MonoMethod *method)
{
	if (mono_monitor_is_il_fastpath_wrapper (method))
		return TRUE;
	return FALSE;
}

/*
Coordinate the creation of all remaining TLS slots in the runtime.
No further TLS slots should be created after this function finishes.
This restriction exists because AOT requires offsets to be constant
across runs.
*/
void
mono_runtime_init_tls (void)
{
	mono_marshal_init_tls ();
	mono_thread_pool_init_tls ();
	mono_thread_init_tls ();
}
