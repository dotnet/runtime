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

#ifdef _MSC_VER
// TODO: Find MSVC replacement for __builtin_unwind_init
#define SAVE_REGS_ON_STACK g_assert_not_reached ();
#elif defined (HOST_WASM)
//TODO: figure out wasm stack scanning
#define SAVE_REGS_ON_STACK do {} while (0)
#else 
#define SAVE_REGS_ON_STACK __builtin_unwind_init ();
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

	stack = mono_native_tls_get_value (coop_reset_count_stack_key);
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

	stack = mono_native_tls_get_value (coop_reset_count_stack_key);
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
check_info (MonoThreadInfo *info, const gchar *action, const gchar *state)
{
	if (!info)
		g_error ("Cannot %s GC %s region if the thread is not attached", action, state);
	if (!mono_thread_info_is_current (info))
		g_error ("[%p] Cannot %s GC %s region on a different thread", mono_thread_info_get_tid (info), action, state);
	if (!mono_thread_info_is_live (info))
		g_error ("[%p] Cannot %s GC %s region if the thread is not live", mono_thread_info_get_tid (info), action, state);
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

	/* Fast check for pending suspend requests */
	if (!(info->thread_state & (STATE_ASYNC_SUSPEND_REQUESTED | STATE_SELF_SUSPEND_REQUESTED)))
		return;

	++coop_save_count;
	mono_threads_get_runtime_callbacks ()->thread_state_init (&info->thread_saved_state [SELF_SUSPEND_STATE_INDEX]);

	/* commit the saved state and notify others if needed */
	switch (mono_threads_transition_state_poll (info)) {
	case SelfSuspendResumed:
		break;
	case SelfSuspendWait:
		mono_thread_info_wait_for_resume (info);
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

static volatile gpointer* dummy_global;

static MONO_NEVER_INLINE
void*
return_stack_ptr (gpointer *i)
{
	dummy_global = i;
	return i;
}

static void
copy_stack_data (MonoThreadInfo *info, gpointer *stackdata_begin)
{
	MonoThreadUnwindState *state;
	int stackdata_size;
	gpointer dummy;
	void* stackdata_end = return_stack_ptr (&dummy);

	SAVE_REGS_ON_STACK;

	state = &info->thread_saved_state [SELF_SUSPEND_STATE_INDEX];

	stackdata_size = (char*)stackdata_begin - (char*)stackdata_end;

	if (((gsize) stackdata_begin & (SIZEOF_VOID_P - 1)) != 0)
		g_error ("stackdata_begin (%p) must be %d-byte aligned", stackdata_begin, SIZEOF_VOID_P);
	if (((gsize) stackdata_end & (SIZEOF_VOID_P - 1)) != 0)
		g_error ("stackdata_end (%p) must be %d-byte aligned", stackdata_end, SIZEOF_VOID_P);

	if (stackdata_size <= 0)
		g_error ("stackdata_size = %d, but must be > 0, stackdata_begin = %p, stackdata_end = %p", stackdata_size, stackdata_begin, stackdata_end);

	g_byte_array_set_size (info->stackdata, stackdata_size);
	state->gc_stackdata = info->stackdata->data;
	memcpy (state->gc_stackdata, stackdata_end, stackdata_size);

	state->gc_stackdata_size = stackdata_size;
}

static gpointer
mono_threads_enter_gc_safe_region_unbalanced_with_info (MonoThreadInfo *info, gpointer *stackdata);

gpointer
mono_threads_enter_gc_safe_region (gpointer *stackdata)
{
	return mono_threads_enter_gc_safe_region_with_info (mono_thread_info_current_unchecked (), stackdata);
}

gpointer
mono_threads_enter_gc_safe_region_with_info (MonoThreadInfo *info, gpointer *stackdata)
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
mono_threads_enter_gc_safe_region_unbalanced (gpointer *stackdata)
{
	return mono_threads_enter_gc_safe_region_unbalanced_with_info (mono_thread_info_current_unchecked (), stackdata);
}

static gpointer
mono_threads_enter_gc_safe_region_unbalanced_with_info (MonoThreadInfo *info, gpointer *stackdata)
{
	if (!mono_threads_is_blocking_transition_enabled ())
		return NULL;

	++coop_do_blocking_count;

	check_info (info, "enter", "safe");

	copy_stack_data (info, stackdata);

retry:
	++coop_save_count;
	mono_threads_get_runtime_callbacks ()->thread_state_init (&info->thread_saved_state [SELF_SUSPEND_STATE_INDEX]);

	switch (mono_threads_transition_do_blocking (info)) {
	case DoBlockingContinue:
		break;
	case DoBlockingPollAndRetry:
		mono_threads_state_poll_with_info (info);
		goto retry;
	}

	return info;
}

void
mono_threads_exit_gc_safe_region (gpointer cookie, gpointer *stackdata)
{
	if (!mono_threads_is_blocking_transition_enabled ())
		return;

#ifdef ENABLE_CHECKED_BUILD_GC
	if (mono_check_mode_enabled (MONO_CHECK_MODE_GC))
		coop_tls_pop (cookie);
#endif

	mono_threads_exit_gc_safe_region_unbalanced (cookie, stackdata);
}

void
mono_threads_exit_gc_safe_region_unbalanced (gpointer cookie, gpointer *stackdata)
{
	MonoThreadInfo *info;

	if (!mono_threads_is_blocking_transition_enabled ())
		return;

	info = (MonoThreadInfo *)cookie;

	check_info (info, "exit", "safe");

	switch (mono_threads_transition_done_blocking (info)) {
	case DoneBlockingOk:
		info->thread_saved_state [SELF_SUSPEND_STATE_INDEX].valid = FALSE;
		break;
	case DoneBlockingWait:
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
}

void
mono_threads_assert_gc_safe_region (void)
{
	MONO_REQ_GC_SAFE_MODE;
}

gpointer
mono_threads_enter_gc_unsafe_region (gpointer *stackdata)
{
	return mono_threads_enter_gc_unsafe_region_with_info (mono_thread_info_current_unchecked (), stackdata);
}

gpointer
mono_threads_enter_gc_unsafe_region_with_info (THREAD_INFO_TYPE *info, gpointer *stackdata)
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
mono_threads_enter_gc_unsafe_region_unbalanced (gpointer *stackdata)
{
	return mono_threads_enter_gc_unsafe_region_unbalanced_with_info (mono_thread_info_current_unchecked (), stackdata);
}

gpointer
mono_threads_enter_gc_unsafe_region_unbalanced_with_info (MonoThreadInfo *info, gpointer *stackdata)
{
	if (!mono_threads_is_blocking_transition_enabled ())
		return NULL;

	++coop_reset_blocking_count;

	check_info (info, "enter", "unsafe");

	copy_stack_data (info, stackdata);

	switch (mono_threads_transition_abort_blocking (info)) {
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

	return info;
}

gpointer
mono_threads_enter_gc_unsafe_region_cookie (void)
{
	MonoThreadInfo *info;

	g_assert (mono_threads_is_blocking_transition_enabled ());

	info = mono_thread_info_current_unchecked ();

	check_info (info, "enter (cookie)", "unsafe");

#ifdef ENABLE_CHECKED_BUILD_GC
	if (mono_check_mode_enabled (MONO_CHECK_MODE_GC))
		coop_tls_push (info);
#endif

	return info;
}

void
mono_threads_exit_gc_unsafe_region (gpointer cookie, gpointer *stackdata)
{
	if (!mono_threads_is_blocking_transition_enabled ())
		return;

#ifdef ENABLE_CHECKED_BUILD_GC
	if (mono_check_mode_enabled (MONO_CHECK_MODE_GC))
		coop_tls_pop (cookie);
#endif

	mono_threads_exit_gc_unsafe_region_unbalanced (cookie, stackdata);
}

void
mono_threads_exit_gc_unsafe_region_unbalanced (gpointer cookie, gpointer *stackdata)
{
	if (!mono_threads_is_blocking_transition_enabled ())
		return;

	if (!cookie)
		return;

	mono_threads_enter_gc_safe_region_unbalanced (stackdata);
}

void
mono_threads_assert_gc_unsafe_region (void)
{
	MONO_REQ_GC_UNSAFE_MODE;
}

gboolean
mono_threads_is_coop_enabled (void)
{
#if defined(USE_COOP_GC)
	return TRUE;
#else
	static int is_coop_enabled = -1;
	if (G_UNLIKELY (is_coop_enabled == -1))
		is_coop_enabled = g_hasenv ("MONO_ENABLE_COOP") ? 1 : 0;
	return is_coop_enabled == 1;
#endif
}

gboolean
mono_threads_is_blocking_transition_enabled (void)
{
#if defined(USE_COOP_GC)
	return TRUE;
#else
	static int is_blocking_transition_enabled = -1;
	if (G_UNLIKELY (is_blocking_transition_enabled == -1))
		is_blocking_transition_enabled = (g_hasenv ("MONO_ENABLE_COOP") || g_hasenv ("MONO_ENABLE_BLOCKING_TRANSITION")) ? 1 : 0;
	return is_blocking_transition_enabled == 1;
#endif
}


void
mono_threads_coop_init (void)
{
	if (!mono_threads_is_coop_enabled () && !mono_threads_is_blocking_transition_enabled ())
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
	if (mono_threads_is_coop_enabled ())
		mono_polling_required = 1;
}

void
mono_threads_coop_end_global_suspend (void)
{
	if (mono_threads_is_coop_enabled ())
		mono_polling_required = 0;
}
