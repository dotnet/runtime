/**
 * \file
 * Runtime functions
 *
 * Authors:
 *  Jonathan Pryor
 *
 * Copyright 2010 Novell, Inc (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
#include <mono/utils/unlocked.h>

static gboolean shutting_down_inited = FALSE;
static gboolean shutting_down = FALSE;

/**
 * mono_runtime_set_shutting_down:
 * \deprecated This function can break the shutdown sequence.
 *
 * Invoked by \c System.Environment.Exit to flag that the runtime
 * is shutting down.
 */
void
mono_runtime_set_shutting_down (void)
{
	UnlockedWriteBool (&shutting_down, TRUE);
}

/**
 * mono_runtime_is_shutting_down:
 * This is consumed by the \c P:System.Environment.HasShutdownStarted property.
 * \returns whether the runtime has been flagged for shutdown.
 */
gboolean
mono_runtime_is_shutting_down (void)
{
	return UnlockedReadBool (&shutting_down);
}

static void
fire_process_exit_event (MonoDomain *domain, gpointer user_data)
{
	ERROR_DECL (error);
	MonoObject *exc;

#if ENABLE_NETCORE
	MONO_STATIC_POINTER_INIT (MonoMethod, procexit_method)

		procexit_method = mono_class_get_method_from_name_checked (mono_defaults.appcontext_class, "OnProcessExit", 0, 0, error);
		mono_error_assert_ok (error);

	MONO_STATIC_POINTER_INIT_END (MonoMethod, procexit_method)

	g_assert (procexit_method);
	
	mono_runtime_try_invoke (procexit_method, NULL, NULL, &exc, error);
#else
	MonoClassField *field;
	gpointer pa [2];
	MonoObject *delegate;

	field = mono_class_get_field_from_name_full (mono_defaults.appdomain_class, "ProcessExit", NULL);
	g_assert (field);

	delegate = *(MonoObject **)(((char *)domain->domain) + field->offset);
	if (delegate == NULL)
		return;

	pa [0] = domain->domain;
	pa [1] = NULL;
	mono_runtime_delegate_try_invoke (delegate, pa, &exc, error);
	mono_error_cleanup (error);
#endif
}

static void
mono_runtime_fire_process_exit_event (void)
{
#ifndef MONO_CROSS_COMPILE
	mono_domain_foreach (fire_process_exit_event, NULL);
#endif
}

/**
 * mono_runtime_try_shutdown:
 *
 * Try to initialize runtime shutdown.
 *
 * After this call completes the thread pool will stop accepting new jobs and no further threads will be created.
 *
 * Returns: TRUE if shutdown was initiated by this call or false is other thread beat this one.
 */
gboolean
mono_runtime_try_shutdown (void)
{
	if (mono_atomic_cas_i32 (&shutting_down_inited, TRUE, FALSE))
		return FALSE;

	mono_runtime_fire_process_exit_event ();

	mono_runtime_set_shutting_down ();

	mono_threads_set_shutting_down ();

	/* No new threads will be created after this point */

	/*TODO move the follow to here:
	mono_thread_suspend_all_other_threads (); OR  mono_thread_wait_all_other_threads

	mono_runtime_quit_internal ();
	*/

	return TRUE;
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
}

guint8*
mono_runtime_get_aotid_arr (void)
{
	int i;
	guint8 aotid_sum = 0;
	MonoDomain* domain = mono_domain_get ();

	if (!domain->entry_assembly || !domain->entry_assembly->image)
		return NULL;

	guint8 (*aotid)[16] = &domain->entry_assembly->image->aotid;
	for (i = 0; i < 16; ++i)
		aotid_sum |= (*aotid)[i];

	if (aotid_sum == 0)
		return NULL;

	return (guint8*)aotid;
}

char*
mono_runtime_get_aotid (void)
{
	guint8 *aotid = mono_runtime_get_aotid_arr ();

	if (!aotid)
		return NULL;

	return mono_guid_to_string (aotid);
}
