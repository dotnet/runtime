/*
 * mono-threads.c: Coop threading
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * Copyright 2015 Xamarin, Inc (http://www.xamarin.com)
 */

#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-semaphore.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-tls.h>
#include <mono/utils/hazard-pointer.h>
#include <mono/utils/mono-memory-model.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-time.h>

#ifdef USE_COOP_BACKEND

volatile size_t mono_polling_required;

void
mono_threads_state_poll (void)
{
	MonoThreadInfo *info;

	info = mono_thread_info_current_unchecked ();
	if (!info)
		return;
	THREADS_SUSPEND_DEBUG ("FINISH SELF SUSPEND OF %p\n", mono_thread_info_get_tid (info));

	/* Fast check for pending suspend requests */
	if (!(info->thread_state & (STATE_ASYNC_SUSPEND_REQUESTED | STATE_SELF_SUSPEND_REQUESTED)))
		return;

	g_assert (mono_threads_get_runtime_callbacks ()->thread_state_init_from_sigctx (&info->thread_saved_state [SELF_SUSPEND_STATE_INDEX], NULL));

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

void*
mono_threads_prepare_blocking (void)
{
	MonoThreadInfo *info;

	info = mono_thread_info_current_unchecked ();
	/* If the thread is not attached, it doesn't make sense prepare for suspend. */
	if (!info || !mono_thread_info_is_live (info)) {
		THREADS_SUSPEND_DEBUG ("PREPARE-BLOCKING failed %p\n", mono_thread_info_get_tid (info));
		return NULL;
	}

retry:
	/*The JIT might not be able to save*/
	if (!mono_threads_get_runtime_callbacks ()->thread_state_init_from_sigctx (&info->thread_saved_state [SELF_SUSPEND_STATE_INDEX], NULL)) {
		THREADS_SUSPEND_DEBUG ("PREPARE-BLOCKING failed %p to save thread state\n", mono_thread_info_get_tid (info));
		return NULL;
	}

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
mono_threads_finish_blocking (void *cookie)
{
	static gboolean warned_about_bad_transition;
	MonoThreadInfo *info = cookie;

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
mono_threads_reset_blocking_start (void)
{
	MonoThreadInfo *info = mono_thread_info_current_unchecked ();

	/* If the thread is not attached, it doesn't make sense prepare for suspend. */
	if (!info || !mono_thread_info_is_live (info))
		return NULL;

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
mono_threads_reset_blocking_end (void *cookie)
{
	MonoThreadInfo *info = cookie;

	if (!info)
		return;

	g_assert (info == mono_thread_info_current_unchecked ());
	mono_threads_prepare_blocking ();
}

void*
mono_threads_try_prepare_blocking (void)
{
	MonoThreadInfo *info;

	info = mono_thread_info_current_unchecked ();
	/* If the thread is not attached, it doesn't make sense prepare for suspend. */
	if (!info || !mono_thread_info_is_live (info) || mono_thread_info_current_state (info) == STATE_BLOCKING) {
		THREADS_SUSPEND_DEBUG ("PREPARE-TRY-BLOCKING failed %p\n", mono_thread_info_get_tid (info));
		return NULL;
	}

retry:
	/*The JIT might not be able to save*/
	if (!mono_threads_get_runtime_callbacks ()->thread_state_init_from_sigctx (&info->thread_saved_state [SELF_SUSPEND_STATE_INDEX], NULL)) {
		THREADS_SUSPEND_DEBUG ("PREPARE-TRY-BLOCKING failed %p to save thread state\n", mono_thread_info_get_tid (info));
		return NULL;
	}

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
mono_threads_finish_try_blocking (void* cookie)
{
	mono_threads_finish_blocking (cookie);
}

void
mono_threads_core_abort_syscall (MonoThreadInfo *info)
{
	g_error ("FIXME");
}

gboolean
mono_threads_core_begin_async_resume (MonoThreadInfo *info)
{
	g_error ("FIXME");
	return FALSE;
}

gboolean
mono_threads_core_begin_async_suspend (MonoThreadInfo *info, gboolean interrupt_kernel)
{
	mono_threads_add_to_pending_operation_set (info);
	/* There's nothing else to do after we async request the thread to suspend */
	return TRUE;
}

gboolean
mono_threads_core_check_suspend_result (MonoThreadInfo *info)
{
	/* Async suspend can't async fail on coop */
	return TRUE;
}

gboolean
mono_threads_core_needs_abort_syscall (void)
{
	/*
	Small digression.
	Syscall abort can't be handled by the suspend machinery even though it's kind of implemented
	in a similar way (with, like, signals).

	So, having it here is wrong, it should be on mono-threads-(mach|posix|windows).
	Ideally we would slice this in (coop|preemp) and target. Then have this file set:
	mono-threads-mach, mono-threads-mach-preempt and mono-threads-mach-coop.
	More files, less ifdef hell.
	*/
	return FALSE;
}

void
mono_threads_init_platform (void)
{
	//See the above for what's wrong here.
}

void
mono_threads_platform_free (MonoThreadInfo *info)
{
	//See the above for what's wrong here.
}

void
mono_threads_platform_register (MonoThreadInfo *info)
{
	//See the above for what's wrong here.
}

void
mono_threads_core_begin_global_suspend (void)
{
	mono_polling_required = 1;
}

void
mono_threads_core_end_global_suspend (void)
{
	mono_polling_required = 0;
}


#endif