 /*
 * mono-threads.c: Coop threading
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * Copyright 2015 Xamarin, Inc (http://www.xamarin.com)
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

#ifdef TARGET_OSX
#include <mono/utils/mach-support.h>
#endif

#ifdef _MSC_VER
// TODO: Find MSVC replacement for __builtin_unwind_init
#define SAVE_REGS_ON_STACK g_assert_not_reached ();
#else 
#define SAVE_REGS_ON_STACK __builtin_unwind_init ();
#endif

volatile size_t mono_polling_required;

static int coop_reset_blocking_count;
static int coop_try_blocking_count;
static int coop_do_blocking_count;
static int coop_do_polling_count;
static int coop_save_count;

void
mono_threads_state_poll (void)
{
	MonoThreadInfo *info;

	g_assert (mono_threads_is_coop_enabled ());

	++coop_do_polling_count;

	info = mono_thread_info_current_unchecked ();
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
		return;
	case SelfSuspendWait:
		mono_thread_info_wait_for_resume (info);
		break;
	case SelfSuspendNotifyAndWait:
		mono_threads_notify_initiator_of_suspend (info);
		mono_thread_info_wait_for_resume (info);
		break;
	}
}

static void *
return_stack_ptr ()
{
	gpointer i;
	return &i;
}

static void
copy_stack_data (MonoThreadInfo *info, void* stackdata_begin)
{
	MonoThreadUnwindState *state;
	int stackdata_size;
	void* stackdata_end = return_stack_ptr ();

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

void*
mono_threads_prepare_blocking (void* stackdata)
{
	MonoThreadInfo *info;

	if (!mono_threads_is_coop_enabled ())
		return NULL;

	++coop_do_blocking_count;

	info = mono_thread_info_current_unchecked ();
	/* If the thread is not attached, it doesn't make sense prepare for suspend. */
	if (!info || !mono_thread_info_is_live (info)) {
		THREADS_SUSPEND_DEBUG ("PREPARE-BLOCKING failed %p\n", mono_thread_info_get_tid (info));
		return NULL;
	}

	copy_stack_data (info, stackdata);

retry:
	++coop_save_count;
	mono_threads_get_runtime_callbacks ()->thread_state_init (&info->thread_saved_state [SELF_SUSPEND_STATE_INDEX]);

	switch (mono_threads_transition_do_blocking (info)) {
	case DoBlockingContinue:
		break;
	case DoBlockingPollAndRetry:
		mono_threads_state_poll ();
		goto retry;
	}

	return info;
}

void
mono_threads_finish_blocking (void *cookie, void* stackdata)
{
	static gboolean warned_about_bad_transition;
	MonoThreadInfo *info;

	if (!mono_threads_is_coop_enabled ())
		return;

	info = (MonoThreadInfo *)cookie;
	if (!info)
		return;

	g_assert (info == mono_thread_info_current_unchecked ());

	switch (mono_threads_transition_done_blocking (info)) {
	case DoneBlockingAborted:
		if (!warned_about_bad_transition) {
			warned_about_bad_transition = TRUE;
			g_warning ("[%p] Blocking call ended in running state for, this might lead to unbound GC pauses.", mono_thread_info_get_tid (info));
		}
		mono_threads_state_poll ();
		break;
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
}


void*
mono_threads_reset_blocking_start (void* stackdata)
{
	MonoThreadInfo *info;

	if (!mono_threads_is_coop_enabled ())
		return NULL;

	++coop_reset_blocking_count;

	info = mono_thread_info_current_unchecked ();
	/* If the thread is not attached, it doesn't make sense prepare for suspend. */
	if (!info || !mono_thread_info_is_live (info))
		return NULL;

	copy_stack_data (info, stackdata);

	switch (mono_threads_transition_abort_blocking (info)) {
	case AbortBlockingIgnore:
		info->thread_saved_state [SELF_SUSPEND_STATE_INDEX].valid = FALSE;
		return NULL;
	case AbortBlockingIgnoreAndPoll:
		mono_threads_state_poll ();
		return NULL;
	case AbortBlockingOk:
		info->thread_saved_state [SELF_SUSPEND_STATE_INDEX].valid = FALSE;
		return info;
	case AbortBlockingOkAndPool:
		mono_threads_state_poll ();
		return info;
	default:
		g_error ("Unknown thread state");
	}
}

void
mono_threads_reset_blocking_end (void *cookie, void* stackdata)
{
	MonoThreadInfo *info;

	if (!mono_threads_is_coop_enabled ())
		return;

	info = (MonoThreadInfo *)cookie;
	if (!info)
		return;

	g_assert (info == mono_thread_info_current_unchecked ());
	mono_threads_prepare_blocking (stackdata);
}

void
mono_threads_init_coop (void)
{
	if (!mono_threads_is_coop_enabled ())
		return;

	mono_counters_register ("Coop Reset Blocking", MONO_COUNTER_GC | MONO_COUNTER_INT, &coop_reset_blocking_count);
	mono_counters_register ("Coop Try Blocking", MONO_COUNTER_GC | MONO_COUNTER_INT, &coop_try_blocking_count);
	mono_counters_register ("Coop Do Blocking", MONO_COUNTER_GC | MONO_COUNTER_INT, &coop_do_blocking_count);
	mono_counters_register ("Coop Do Polling", MONO_COUNTER_GC | MONO_COUNTER_INT, &coop_do_polling_count);
	mono_counters_register ("Coop Save Count", MONO_COUNTER_GC | MONO_COUNTER_INT, &coop_save_count);
	//See the above for what's wrong here.
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

gpointer
mono_threads_enter_gc_unsafe_region (gpointer* stackdata)
{
	if (!mono_threads_is_coop_enabled ())
		return NULL;

	return mono_threads_reset_blocking_start (stackdata);
}

void
mono_threads_exit_gc_unsafe_region (gpointer cookie, gpointer* stackdata)
{
	if (!mono_threads_is_coop_enabled ())
		return;

	mono_threads_reset_blocking_end (cookie, stackdata);
}

void
mono_threads_assert_gc_unsafe_region (void)
{
	MONO_REQ_GC_UNSAFE_MODE;
}

gpointer
mono_threads_enter_gc_safe_region (gpointer *stackdata)
{
	if (!mono_threads_is_coop_enabled ())
		return NULL;

	return mono_threads_prepare_blocking (stackdata);
}

void
mono_threads_exit_gc_safe_region (gpointer cookie, gpointer *stackdata)
{
	if (!mono_threads_is_coop_enabled ())
		return;

	mono_threads_finish_blocking (cookie, stackdata);
}

void
mono_threads_assert_gc_safe_region (void)
{
	MONO_REQ_GC_SAFE_MODE;
}
