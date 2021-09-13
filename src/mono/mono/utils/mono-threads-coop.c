/**
 * \file
 * Coop threading
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * Copyright 2015 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>

/* enable pthread extensions */
#ifdef TARGET_MACH
#define _DARWIN_C_SOURCE
#endif

#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-tls.h>
#include <mono/utils/hazard-pointer.h>
#include <mono/utils/mono-memory-model.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-time.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/mono-threads-coop.h>
#include <mono/utils/mono-threads-api.h>
#include <mono/utils/checked-build.h>
#include <mono/utils/mono-threads-debug.h>

#ifdef TARGET_OSX
#include <mono/utils/mach-support.h>
#endif

volatile size_t mono_polling_required;

// FIXME: This would be more efficient if instead of instantiating the stack it just pushed a simple depth counter up and down,
// perhaps with a per-thread cookie in the high bits.
#ifdef ENABLE_CHECKED_BUILD_GC

// Maintains a single per-thread stack of ints, used to ensure nesting is not violated
static MonoNativeTlsKey coop_reset_count_stack_key;

static void
coop_tls_push (gpointer cookie)
{
	GArray *stack;

	stack = (GArray*)mono_native_tls_get_value (coop_reset_count_stack_key);
	if (!stack) {
		stack = g_array_new (FALSE, FALSE, sizeof(gpointer));
		mono_native_tls_set_value (coop_reset_count_stack_key, stack);
	}

	g_array_append_val (stack, cookie);
}

static void
coop_tls_pop (gpointer received_cookie)
{
	GArray *stack;
	gpointer expected_cookie;

	stack = (GArray*)mono_native_tls_get_value (coop_reset_count_stack_key);
	if (!stack || 0 == stack->len)
		mono_fatal_with_history ("Received cookie %p but found no stack at all\n", received_cookie);

	expected_cookie = g_array_index (stack, gpointer, stack->len - 1);
	stack->len --;

	if (0 == stack->len) {
		g_array_free (stack,TRUE);
		mono_native_tls_set_value (coop_reset_count_stack_key, NULL);
	}

	if (expected_cookie != received_cookie)
		mono_fatal_with_history ("Received cookie %p but expected %p\n", received_cookie, expected_cookie);
}

#endif

static void
check_info (MonoThreadInfo *info, const gchar *action, const gchar *state, const char *func)
{
#ifdef ENABLE_CHECKED_BUILD_GC
	g_assertf (info, "%s Cannot %s GC %s region if the thread is not attached", func, action, state);
	g_assertf (mono_thread_info_is_current (info), "%s [%p] Cannot %s GC %s region on a different thread", func, mono_thread_info_get_tid (info), action, state);
	g_assertf (mono_thread_info_is_live (info), "%s [%p] Cannot %s GC %s region if the thread is not live", func, mono_thread_info_get_tid (info), action, state);
#endif
}

static int coop_reset_blocking_count;
static int coop_try_blocking_count;
static int coop_do_blocking_count;
static int coop_do_polling_count;
static int coop_save_count;

static void
mono_threads_state_poll_with_info (MonoThreadInfo *info);

void
mono_threads_state_poll (void)
{
	mono_threads_state_poll_with_info (mono_thread_info_current_unchecked ());
}

static void
mono_threads_state_poll_with_info (MonoThreadInfo *info)
{
	g_assert (mono_threads_is_blocking_transition_enabled ());

	++coop_do_polling_count;

	if (!info)
		return;

	THREADS_SUSPEND_DEBUG ("FINISH SELF SUSPEND OF %p\n", mono_thread_info_get_tid (info));

	/* Fast fail if no_safepoints is set */
	g_assert (!info->thread_state.no_safepoints);

	/* Fast check for pending suspend requests */
	if (info->thread_state.state != STATE_ASYNC_SUSPEND_REQUESTED)
		return;

#ifdef MONO_CROSS_COMPILE
	g_error ("Unreachable path on cross-compiler");
	return;
#else
	/* Spill all registers to the stack to make the GC aware of the references */
	MonoContext ctx;
	MONO_CONTEXT_GET_CURRENT (ctx);
#endif

	++coop_save_count;
	mono_threads_get_runtime_callbacks ()->thread_state_init (&info->thread_saved_state [SELF_SUSPEND_STATE_INDEX], info);

	/* commit the saved state and notify others if needed */
	switch (mono_threads_transition_state_poll (info)) {
	case SelfSuspendResumed:
		break;
	case SelfSuspendNotifyAndWait:
		mono_threads_notify_initiator_of_suspend (info);
		mono_thread_info_wait_for_resume (info);
		break;
	}

	if (info->async_target) {
		info->async_target (info->user_data);
		info->async_target = NULL;
		info->user_data = NULL;
	}
}

static gpointer
mono_threads_enter_gc_safe_region_unbalanced_with_info (MonoThreadInfo *info, MonoStackData *stackdata);

gpointer
mono_threads_enter_gc_safe_region_internal (MonoStackData *stackdata)
{
	return mono_threads_enter_gc_safe_region_with_info (mono_thread_info_current_unchecked (), stackdata);
}

gpointer
mono_threads_enter_gc_safe_region (gpointer *stackpointer)
{
	MONO_STACKDATA (stackdata);
	stackdata.stackpointer = stackpointer;
	return mono_threads_enter_gc_safe_region_internal (&stackdata);
}

gpointer
mono_threads_enter_gc_safe_region_with_info (MonoThreadInfo *info, MonoStackData *stackdata)
{
	gpointer cookie;

	if (!mono_threads_is_blocking_transition_enabled ())
		return NULL;

	cookie = mono_threads_enter_gc_safe_region_unbalanced_with_info (info, stackdata);

#ifdef ENABLE_CHECKED_BUILD_GC
	if (mono_check_mode_enabled (MONO_CHECK_MODE_GC))
		coop_tls_push (cookie);
#endif

	return cookie;
}

gpointer
mono_threads_enter_gc_safe_region_unbalanced_internal (MonoStackData *stackdata)
{
	return mono_threads_enter_gc_safe_region_unbalanced_with_info (mono_thread_info_current_unchecked (), stackdata);
}

gpointer
mono_threads_enter_gc_safe_region_unbalanced (gpointer *stackpointer)
{
	MONO_STACKDATA (stackdata);
	stackdata.stackpointer = stackpointer;
	return mono_threads_enter_gc_safe_region_unbalanced_internal (&stackdata);
}

static gpointer
mono_threads_enter_gc_safe_region_unbalanced_with_info (MonoThreadInfo *info, MonoStackData *stackdata)
{
	if (!mono_threads_is_blocking_transition_enabled ())
		return NULL;

	++coop_do_blocking_count;

	const char *function_name = mono_stackdata_get_function_name (stackdata);

	check_info (info, "enter", "safe", function_name);

retry:
	++coop_save_count;
	mono_threads_get_runtime_callbacks ()->thread_state_init (&info->thread_saved_state [SELF_SUSPEND_STATE_INDEX], info);

	switch (mono_threads_transition_do_blocking (info, function_name)) {
	case DoBlockingContinue:
		break;
	case DoBlockingPollAndRetry:
		mono_threads_state_poll_with_info (info);
		goto retry;
	}

	return info;
}

void
mono_threads_exit_gc_safe_region_internal (gpointer cookie, MonoStackData *stackdata)
{
	if (!mono_threads_is_blocking_transition_enabled ())
		return;

#ifdef ENABLE_CHECKED_BUILD_GC
	W32_DEFINE_LAST_ERROR_RESTORE_POINT;
	if (mono_check_mode_enabled (MONO_CHECK_MODE_GC))
		coop_tls_pop (cookie);
	W32_RESTORE_LAST_ERROR_FROM_RESTORE_POINT;
#endif

	mono_threads_exit_gc_safe_region_unbalanced_internal (cookie, stackdata);
}

void
mono_threads_exit_gc_safe_region (gpointer cookie, gpointer *stackpointer)
{
	MONO_STACKDATA (stackdata);
	stackdata.stackpointer = stackpointer;
	mono_threads_exit_gc_safe_region_internal (cookie, &stackdata);
}

void
mono_threads_exit_gc_safe_region_unbalanced_internal (gpointer cookie, MonoStackData *stackdata)
{
	MonoThreadInfo *info;

	if (!mono_threads_is_blocking_transition_enabled ())
		return;

	/* Common to use enter/exit gc safe around OS API's affecting last error. */
	/* This method can call OS API's that will reset last error on some platforms. */
	/* To reduce errors, we need to restore last error before exit gc safe. */
	W32_DEFINE_LAST_ERROR_RESTORE_POINT;

	info = (MonoThreadInfo *)cookie;

	const char *function_name = mono_stackdata_get_function_name (stackdata);

	check_info (info, "exit", "safe", function_name);

	switch (mono_threads_transition_done_blocking (info, function_name)) {
	case DoneBlockingOk:
		info->thread_saved_state [SELF_SUSPEND_STATE_INDEX].valid = FALSE;
		break;
	case DoneBlockingWait:
		/* If full coop suspend, we're just waiting for the initiator
		 * to resume us.  If hybrid suspend, we were either self
		 * suspended cooperatively from async_suspend_requested (same
		 * as full coop), or we were suspended preemptively while in
		 * blocking and we're waiting for two things: the suspend
		 * signal handler to run and notify the initiator and
		 * immediately return, and then for the resume. */
		THREADS_SUSPEND_DEBUG ("state polling done, notifying of resume\n");
		mono_thread_info_wait_for_resume (info);
		break;
	default:
		g_error ("Unknown thread state");
	}

	if (info->async_target) {
		info->async_target (info->user_data);
		info->async_target = NULL;
		info->user_data = NULL;
	}

	W32_RESTORE_LAST_ERROR_FROM_RESTORE_POINT;
}

void
mono_threads_exit_gc_safe_region_unbalanced (gpointer cookie, gpointer *stackpointer)
{
	MONO_STACKDATA (stackdata);
	stackdata.stackpointer = stackpointer;
	mono_threads_exit_gc_safe_region_unbalanced_internal (cookie, &stackdata);
}

void
mono_threads_assert_gc_safe_region (void)
{
	MONO_REQ_GC_SAFE_MODE;
}

gpointer
mono_threads_enter_gc_unsafe_region_internal (MonoStackData *stackdata)
{
	return mono_threads_enter_gc_unsafe_region_with_info (mono_thread_info_current_unchecked (), stackdata);
}

gpointer
mono_threads_enter_gc_unsafe_region (gpointer *stackpointer)
{
	MONO_STACKDATA (stackdata);
	stackdata.stackpointer = stackpointer;
	return mono_threads_enter_gc_unsafe_region_internal (&stackdata);
}

gpointer
mono_threads_enter_gc_unsafe_region_with_info (THREAD_INFO_TYPE *info, MonoStackData *stackdata)
{
	gpointer cookie;

	if (!mono_threads_is_blocking_transition_enabled ())
		return NULL;

	cookie = mono_threads_enter_gc_unsafe_region_unbalanced_with_info (info, stackdata);

#ifdef ENABLE_CHECKED_BUILD_GC
	if (mono_check_mode_enabled (MONO_CHECK_MODE_GC))
		coop_tls_push (cookie);
#endif

	return cookie;
}

gpointer
mono_threads_enter_gc_unsafe_region_unbalanced_internal (MonoStackData *stackdata)
{
	return mono_threads_enter_gc_unsafe_region_unbalanced_with_info (mono_thread_info_current_unchecked (), stackdata);
}

gpointer
mono_threads_enter_gc_unsafe_region_unbalanced (gpointer *stackpointer)
{
	MONO_STACKDATA (stackdata);
	stackdata.stackpointer = stackpointer;
	return mono_threads_enter_gc_unsafe_region_unbalanced_internal (&stackdata);
}

gpointer
mono_threads_enter_gc_unsafe_region_unbalanced_with_info (MonoThreadInfo *info, MonoStackData *stackdata)
{
	if (!mono_threads_is_blocking_transition_enabled ())
		return NULL;

	++coop_reset_blocking_count;

	const char *function_name = mono_stackdata_get_function_name (stackdata);

	check_info (info, "enter", "unsafe", function_name);

	switch (mono_threads_transition_abort_blocking (info, function_name)) {
	case AbortBlockingIgnore:
		info->thread_saved_state [SELF_SUSPEND_STATE_INDEX].valid = FALSE;
		return NULL;
	case AbortBlockingIgnoreAndPoll:
		mono_threads_state_poll_with_info (info);
		return NULL;
	case AbortBlockingOk:
		info->thread_saved_state [SELF_SUSPEND_STATE_INDEX].valid = FALSE;
		break;
	case AbortBlockingWait:
		/* If full coop suspend, we're just waiting for the initiator
		 * to resume us.  If hybrid suspend, we were either self
		 * suspended cooperatively from async_suspend_requested (same
		 * as full coop), or we were suspended preemptively while in
		 * blocking and we're waiting for two things: the suspend
		 * signal handler to run and notify the initiator and
		 * immediately return, and then for the resume. */
		mono_thread_info_wait_for_resume (info);
		break;
	default:
		g_error ("Unknown thread state %s", function_name);
	}

	if (info->async_target) {
		info->async_target (info->user_data);
		info->async_target = NULL;
		info->user_data = NULL;
	}

	return info;
}

gpointer
mono_threads_enter_gc_unsafe_region_cookie (void)
{
	MonoThreadInfo *info;

	g_assert (mono_threads_is_blocking_transition_enabled ());

	info = mono_thread_info_current_unchecked ();

	check_info (info, "enter (cookie)", "unsafe", "");

#ifdef ENABLE_CHECKED_BUILD_GC
	if (mono_check_mode_enabled (MONO_CHECK_MODE_GC))
		coop_tls_push (info);
#endif

	return info;
}

void
mono_threads_exit_gc_unsafe_region_internal (gpointer cookie, MonoStackData *stackdata)
{
	if (!mono_threads_is_blocking_transition_enabled ())
		return;

#ifdef ENABLE_CHECKED_BUILD_GC
	if (mono_check_mode_enabled (MONO_CHECK_MODE_GC))
		coop_tls_pop (cookie);
#endif

	mono_threads_exit_gc_unsafe_region_unbalanced_internal (cookie, stackdata);
}

void
mono_threads_exit_gc_unsafe_region (gpointer cookie, gpointer *stackpointer)
{
	MONO_STACKDATA (stackdata);
	stackdata.stackpointer = stackpointer;
	mono_threads_exit_gc_unsafe_region_internal (cookie, &stackdata);
}

void
mono_threads_exit_gc_unsafe_region_unbalanced_internal (gpointer cookie, MonoStackData *stackdata)
{
	if (!mono_threads_is_blocking_transition_enabled ())
		return;

	if (!cookie)
		return;

	mono_threads_enter_gc_safe_region_unbalanced_internal (stackdata);
}

void
mono_threads_exit_gc_unsafe_region_unbalanced (gpointer cookie, gpointer *stackpointer)
{
	MONO_STACKDATA (stackdata);
	stackdata.stackpointer = stackpointer;
	mono_threads_exit_gc_unsafe_region_unbalanced_internal (cookie, &stackdata);
}

void
mono_threads_assert_gc_unsafe_region (void)
{
	MONO_REQ_GC_UNSAFE_MODE;
}

static int
threads_suspend_policy_default (void)
{
#if defined (ENABLE_COOP_SUSPEND)
	return MONO_THREADS_SUSPEND_FULL_COOP;
#elif defined (ENABLE_HYBRID_SUSPEND)
	return MONO_THREADS_SUSPEND_HYBRID;
#else
	return 0; /* unset */
#endif
}

/* Look up whether an env var is set, warn that it's obsolete and offer a new
 * alternative
 */
static gboolean
hasenv_obsolete (const char *name, const char* newval)
{
	// If they already set MONO_THREADS_SUSPEND to something, maybe they're keeping
	// the old var set for compatability with old Mono - in that case don't nag.
	// FIXME: but maybe nag if MONO_THREADS_SUSPEND isn't set to "newval"?
	static int quiet = -1;
	if (g_hasenv (name)) {
		if (G_UNLIKELY (quiet == -1))
			quiet = g_hasenv ("MONO_THREADS_SUSPEND");
		if (!quiet)
			g_warning ("%s environment variable is obsolete.  Use MONO_THREADS_SUSPEND=%s", name, newval);
		return TRUE;
	}
	return FALSE;
}

static int
threads_suspend_policy_getenv_compat (void)
{
	int policy = 0;
	if (hasenv_obsolete ("MONO_ENABLE_COOP", "coop") || hasenv_obsolete ("MONO_ENABLE_COOP_SUSPEND", "coop")) {
		g_assertf (!hasenv_obsolete ("MONO_ENABLE_HYBRID_SUSPEND", "hybrid"),
			   "Environment variables set to enable both hybrid and cooperative suspend simultaneously");
		policy = MONO_THREADS_SUSPEND_FULL_COOP;
	} else if (hasenv_obsolete ("MONO_ENABLE_HYBRID_SUSPEND", "hybrid"))
		policy = MONO_THREADS_SUSPEND_HYBRID;
	return policy;
}

static int
threads_suspend_policy_getenv (void)
{
	int policy = 0;
	if (g_hasenv ("MONO_THREADS_SUSPEND")) {
		gchar *str = g_getenv ("MONO_THREADS_SUSPEND");
		if (!strcmp (str, "coop"))
			policy = MONO_THREADS_SUSPEND_FULL_COOP;
		else if (!strcmp (str, "hybrid"))
			policy = MONO_THREADS_SUSPEND_HYBRID;
		else if (!strcmp (str, "preemptive"))
			policy = MONO_THREADS_SUSPEND_FULL_PREEMPTIVE;
		else
			g_error ("MONO_THREADS_SUSPEND environment variable set to '%s', must be one of coop, hybrid, preemptive.", str);
		g_free (str);
	}
	return policy;
}

char mono_threads_suspend_policy_hidden_dont_modify;

void
mono_threads_suspend_policy_init (void)
{
	int policy = 0;
	{
		// thread suspend policy:
		// if the MONO_THREADS_SUSPEND env is set, use it.
		// otherwise if there's a compiled-in default, use it.
		// otherwise if one of the old environment variables is set, use that.
		// otherwise use full preemptive suspend.

		W32_DEFINE_LAST_ERROR_RESTORE_POINT;

		   (policy = threads_suspend_policy_getenv ())
		|| (policy = threads_suspend_policy_default ())
		|| (policy = threads_suspend_policy_getenv_compat ())
		|| (policy = MONO_THREADS_SUSPEND_FULL_PREEMPTIVE);

		W32_RESTORE_LAST_ERROR_FROM_RESTORE_POINT;

		g_assert (policy);
		mono_threads_suspend_policy_hidden_dont_modify = (char)policy;
	}
}

static MonoThreadsSuspendPolicy
mono_threads_suspend_validate_policy (MonoThreadsSuspendPolicy policy)
{
	switch (policy) {
	case MONO_THREADS_SUSPEND_FULL_COOP:
	case MONO_THREADS_SUSPEND_FULL_PREEMPTIVE:
	case MONO_THREADS_SUSPEND_HYBRID:
		return policy;
	default:
		g_error ("Invalid suspend policy %d.", (int)policy);
	}
}

/**
 * mono_threads_suspend_override_policy:
 *
 * Don't use this.  Provides a last resort escape hatch to override configure
 * and environment settings and use the given thread suspend policy.
 *
 */
void
mono_threads_suspend_override_policy (MonoThreadsSuspendPolicy new_policy)
{
	mono_threads_suspend_policy_hidden_dont_modify = (char)mono_threads_suspend_validate_policy (new_policy);
	g_warning ("Overriding suspend policy.  Using %s suspend.", mono_threads_suspend_policy_name (mono_threads_suspend_policy ()));
}

const char*
mono_threads_suspend_policy_name (MonoThreadsSuspendPolicy policy)
{
	switch (policy) {
	case MONO_THREADS_SUSPEND_FULL_COOP:
		return "cooperative";
	case MONO_THREADS_SUSPEND_FULL_PREEMPTIVE:
		return "preemptive";
	case MONO_THREADS_SUSPEND_HYBRID:
		return "hybrid";
	default:
		g_assert_not_reached ();
	}
}

gboolean
mono_threads_is_cooperative_suspension_enabled (void)
{
	return (mono_threads_suspend_policy () == MONO_THREADS_SUSPEND_FULL_COOP);
}

gboolean
mono_threads_is_hybrid_suspension_enabled (void)
{
	return (mono_threads_suspend_policy () == MONO_THREADS_SUSPEND_HYBRID);
}

void
mono_threads_coop_init (void)
{
	if (!mono_threads_are_safepoints_enabled () && !mono_threads_is_blocking_transition_enabled ())
		return;

	mono_counters_register ("Coop Reset Blocking", MONO_COUNTER_GC | MONO_COUNTER_INT, &coop_reset_blocking_count);
	mono_counters_register ("Coop Try Blocking", MONO_COUNTER_GC | MONO_COUNTER_INT, &coop_try_blocking_count);
	mono_counters_register ("Coop Do Blocking", MONO_COUNTER_GC | MONO_COUNTER_INT, &coop_do_blocking_count);
	mono_counters_register ("Coop Do Polling", MONO_COUNTER_GC | MONO_COUNTER_INT, &coop_do_polling_count);
	mono_counters_register ("Coop Save Count", MONO_COUNTER_GC | MONO_COUNTER_INT, &coop_save_count);
	//See the above for what's wrong here.

#ifdef ENABLE_CHECKED_BUILD_GC
	mono_native_tls_alloc (&coop_reset_count_stack_key, NULL);
#endif
}

void
mono_threads_coop_begin_global_suspend (void)
{
	if (mono_threads_are_safepoints_enabled ())
		mono_polling_required = 1;
}

void
mono_threads_coop_end_global_suspend (void)
{
	if (mono_threads_are_safepoints_enabled ())
		mono_polling_required = 0;
}

void
mono_threads_enter_no_safepoints_region (const char *func)
{
	MONO_REQ_GC_UNSAFE_MODE;
	mono_threads_transition_begin_no_safepoints (mono_thread_info_current (), func);
}

void
mono_threads_exit_no_safepoints_region (const char *func)
{
	MONO_REQ_GC_UNSAFE_MODE;
	mono_threads_transition_end_no_safepoints (mono_thread_info_current (), func);
}

void
mono_thread_set_coop_aware (void)
{
	MONO_ENTER_GC_UNSAFE;
	MonoThreadInfo *info = mono_thread_info_current_unchecked ();
	if (info)
		/* NOTE, this flag should only be changed while in unsafe mode. */
		/* It will make sure we won't get an async preemptive suspend */
		/* request against this thread while in the process of changing the flag */
		/* affecting the threads suspend/resume behavior. */
		mono_atomic_store_i32 (&(info->coop_aware_thread), TRUE);
	MONO_EXIT_GC_UNSAFE;
}

mono_bool
mono_thread_get_coop_aware (void)
{
	mono_bool result = FALSE;
	MONO_ENTER_GC_UNSAFE;
	MonoThreadInfo *info = mono_thread_info_current_unchecked ();
	if (info)
		result = (mono_bool)mono_atomic_load_i32 (&(info->coop_aware_thread));
	MONO_EXIT_GC_UNSAFE;

	return result;
}

char mono_threads_is_runtime_startup_finished_hidden_do_not_modify;

void
mono_threads_set_runtime_startup_finished (void)
{
	mono_threads_is_runtime_startup_finished_hidden_do_not_modify = 1;
}
