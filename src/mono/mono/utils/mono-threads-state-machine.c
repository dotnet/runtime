/**
 * \file
 */

#include <config.h>

#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-tls.h>
#include <mono/utils/mono-memory-model.h>
#include <mono/utils/atomic.h>
#include <mono/utils/checked-build.h>
#include <mono/utils/mono-threads-debug.h>

#include <errno.h>

/*thread state helpers*/
static inline int
get_thread_state (int thread_state)
{
	return thread_state & THREAD_STATE_MASK;
}

static inline int
get_thread_suspend_count (int thread_state)
{
	return (thread_state & THREAD_SUSPEND_COUNT_MASK) >> THREAD_SUSPEND_COUNT_SHIFT;
}

static inline int
build_thread_state (int thread_state, int suspend_count) 
{
	g_assert (suspend_count >= 0 && suspend_count <= THREAD_SUSPEND_COUNT_MAX);
	g_assert (thread_state >= 0 && thread_state <= STATE_MAX);

	return thread_state | (suspend_count << THREAD_SUSPEND_COUNT_SHIFT);
}

static const char*
state_name (int state)
{
	static const char *state_names [] = {
		"STARTING",
		"RUNNING",
		"DETACHED",
		"ASYNC_SUSPENDED",
		"SELF_SUSPENDED",
		"ASYNC_SUSPEND_REQUESTED",
		"SELF_SUSPEND_REQUESTED",
		"STATE_BLOCKING",
		"STATE_BLOCKING_AND_SUSPENDED",
	};
	return state_names [get_thread_state (state)];
}

#define UNWRAP_THREAD_STATE(RAW,CUR,COUNT,INFO) do {	\
	RAW = (INFO)->thread_state;	\
	CUR = get_thread_state (RAW);	\
	COUNT = get_thread_suspend_count (RAW);	\
} while (0)

static void
check_thread_state (MonoThreadInfo* info)
{
	int raw_state, cur_state, suspend_count;
	UNWRAP_THREAD_STATE (raw_state, cur_state, suspend_count, info);
	switch (cur_state) {
	case STATE_STARTING:
	case STATE_RUNNING:
	case STATE_DETACHED:
		g_assert (suspend_count == 0);
		break;
	case STATE_ASYNC_SUSPENDED:
	case STATE_SELF_SUSPENDED:
	case STATE_ASYNC_SUSPEND_REQUESTED:
	case STATE_SELF_SUSPEND_REQUESTED:
	case STATE_BLOCKING_AND_SUSPENDED:
		g_assert (suspend_count > 0);
		break;
	case STATE_BLOCKING: //this is a special state that can have zero or positive suspend count.
		break;
	default:
		g_error ("Invalid state %d", cur_state);
	}
}

static inline void
trace_state_change (const char *transition, MonoThreadInfo *info, int cur_raw_state, int next_state, int suspend_count_delta)
{
	check_thread_state (info);
	THREADS_STATE_MACHINE_DEBUG ("[%s][%p] %s -> %s (%d -> %d)\n",
		transition,
		mono_thread_info_get_tid (info),
		state_name (get_thread_state (cur_raw_state)),
		state_name (next_state),
		get_thread_suspend_count (cur_raw_state),
		get_thread_suspend_count (cur_raw_state) + suspend_count_delta);

	CHECKED_BUILD_THREAD_TRANSITION (transition, info, get_thread_state (cur_raw_state), get_thread_suspend_count (cur_raw_state), next_state, suspend_count_delta);
}

/*
This is the transition that signals that a thread is functioning.
Its main goal is to catch threads been witnessed before been fully registered.
*/
void
mono_threads_transition_attach (MonoThreadInfo* info)
{
	int raw_state, cur_state, suspend_count;

retry_state_change:
	UNWRAP_THREAD_STATE (raw_state, cur_state, suspend_count, info);
	switch (cur_state) {
	case STATE_STARTING:
		if (!(suspend_count == 0))
			mono_fatal_with_history ("suspend_count = %d, but should be == 0", suspend_count);
		if (mono_atomic_cas_i32 (&info->thread_state, STATE_RUNNING, raw_state) != raw_state)
			goto retry_state_change;
		trace_state_change ("ATTACH", info, raw_state, STATE_RUNNING, 0);
		break;
	default:
		mono_fatal_with_history ("Cannot transition current thread from %s with ATTACH", state_name (cur_state));
	}
}

/*
This is the transition that signals that a thread is no longer registered with the runtime.
Its main goal is to catch threads been witnessed after they detach.

This returns TRUE is the transition succeeded.
If it returns false it means that there's a pending suspend that should be acted upon.
*/
gboolean
mono_threads_transition_detach (MonoThreadInfo *info)
{
	int raw_state, cur_state, suspend_count;

retry_state_change:
	UNWRAP_THREAD_STATE (raw_state, cur_state, suspend_count, info);
	switch (cur_state) {
	case STATE_RUNNING:
	case STATE_BLOCKING: /* An OS thread on coop goes STARTING->BLOCKING->RUNNING->BLOCKING->DETACHED */
		if (!(suspend_count == 0))
			mono_fatal_with_history ("suspend_count = %d, but should be == 0", suspend_count);
		if (mono_atomic_cas_i32 (&info->thread_state, STATE_DETACHED, raw_state) != raw_state)
			goto retry_state_change;
		trace_state_change ("DETACH", info, raw_state, STATE_DETACHED, 0);
		return TRUE;
	case STATE_ASYNC_SUSPEND_REQUESTED: //Can't detach until whoever asked us to suspend to be happy with us
		return FALSE;

/*
STATE_ASYNC_SUSPENDED: Code should not be running while suspended.
STATE_SELF_SUSPENDED: Code should not be running while suspended.
STATE_SELF_SUSPEND_REQUESTED: This is a bug in the self suspend code that didn't execute the second part of it
STATE_BLOCKING_AND_SUSPENDED: This is a bug in coop x suspend that resulted the thread in an undetachable state.
*/
	default:
		mono_fatal_with_history ("Cannot transition current thread %p from %s with DETACH", info, state_name (cur_state));
	}
}

/*
This transition initiates the suspension of another thread.

Returns one of the following values:

- AsyncSuspendInitSuspend: Thread suspend requested, async suspend needs to be done.
- AsyncSuspendAlreadySuspended: Thread already suspended, nothing to do.
- AsyncSuspendWait: Self suspend in progress, asked it to notify us. Caller must add target to the notification set.
- AsyncSuspendBlocking: Thread in blocking state
*/
MonoRequestAsyncSuspendResult
mono_threads_transition_request_async_suspension (MonoThreadInfo *info)
{
	int raw_state, cur_state, suspend_count;
	g_assert (info != mono_thread_info_current ());

retry_state_change:
	UNWRAP_THREAD_STATE (raw_state, cur_state, suspend_count, info);

	switch (cur_state) {
	case STATE_RUNNING: //Post an async suspend request
		if (!(suspend_count == 0))
			mono_fatal_with_history ("suspend_count = %d, but should be == 0", suspend_count);
		if (mono_atomic_cas_i32 (&info->thread_state, build_thread_state (STATE_ASYNC_SUSPEND_REQUESTED, 1), raw_state) != raw_state)
			goto retry_state_change;
		trace_state_change ("ASYNC_SUSPEND_REQUESTED", info, raw_state, STATE_ASYNC_SUSPEND_REQUESTED, 1);
		return AsyncSuspendInitSuspend; //This is the first async suspend request against the target

	case STATE_ASYNC_SUSPENDED:
	case STATE_SELF_SUSPENDED: //Async suspend can suspend the same thread multiple times as it starts from the outside
	case STATE_BLOCKING_AND_SUSPENDED:
		if (!(suspend_count > 0 && suspend_count < THREAD_SUSPEND_COUNT_MAX))
			mono_fatal_with_history ("suspend_count = %d, but should be > 0 and < THREAD_SUSPEND_COUNT_MAX", suspend_count);
		if (mono_atomic_cas_i32 (&info->thread_state, build_thread_state (cur_state, suspend_count + 1), raw_state) != raw_state)
			goto retry_state_change;
		trace_state_change ("ASYNC_SUSPEND_REQUESTED", info, raw_state, cur_state, 1);
		return AsyncSuspendAlreadySuspended; //Thread is already suspended so we don't need to wait it to suspend

	case STATE_SELF_SUSPEND_REQUESTED: //This suspend needs to notify the initiator, so we need to promote the suspend to async
		if (!(suspend_count > 0 && suspend_count < THREAD_SUSPEND_COUNT_MAX))
			mono_fatal_with_history ("suspend_count = %d, but should be > 0 and < THREAD_SUSPEND_COUNT_MAX", suspend_count);
		if (mono_atomic_cas_i32 (&info->thread_state, build_thread_state (STATE_ASYNC_SUSPEND_REQUESTED, suspend_count + 1), raw_state) != raw_state)
			goto retry_state_change;
		trace_state_change ("ASYNC_SUSPEND_REQUESTED", info, raw_state, STATE_ASYNC_SUSPEND_REQUESTED, 1);
		return AsyncSuspendWait; //This is the first async suspend request, change the thread and let it notify us [1]

	case STATE_BLOCKING:
		if (!(suspend_count < THREAD_SUSPEND_COUNT_MAX))
			mono_fatal_with_history ("suspend_count = %d, but should be < THREAD_SUSPEND_COUNT_MAX", suspend_count);
		if (mono_atomic_cas_i32 (&info->thread_state, build_thread_state (cur_state, suspend_count + 1), raw_state) != raw_state)
			goto retry_state_change;
		trace_state_change ("ASYNC_SUSPEND_REQUESTED", info, raw_state, cur_state, 1);
		return AsyncSuspendBlocking; //A thread in the blocking state has its state saved so we can treat it as suspended.

/*

[1] It's questionable on what to do if we hit the beginning of a self suspend.
The expected behavior is that the target should poll its state very soon so the the suspend latency should be minimal.

STATE_ASYNC_SUSPEND_REQUESTED: Since there can only be one async suspend in progress and it must finish, it should not be possible to witness this.
*/
	default:
		mono_fatal_with_history ("Cannot transition thread %p from %s with ASYNC_SUSPEND_REQUESTED", mono_thread_info_get_tid (info), state_name (cur_state));
	}
	return (MonoRequestAsyncSuspendResult) FALSE;
}

/*
Check the current state of the thread and try to init a self suspend.
This must be called with self state saved.

Returns one of the following values:

- Resumed: Async resume happened and current thread should keep running
- Suspend: Caller should wait for a resume signal
- SelfSuspendNotifyAndWait: Notify the suspend initiator and wait for a resume signals
 suspend should start.

*/
MonoSelfSupendResult
mono_threads_transition_state_poll (MonoThreadInfo *info)
{
	int raw_state, cur_state, suspend_count;
	g_assert (mono_thread_info_is_current (info));

retry_state_change:
	UNWRAP_THREAD_STATE (raw_state, cur_state, suspend_count, info);
	switch (cur_state) {
	case STATE_RUNNING:
		if (!(suspend_count == 0))
			mono_fatal_with_history ("suspend_count = %d, but should be == 0", suspend_count);
		trace_state_change ("STATE_POLL", info, raw_state, cur_state, 0);
		return SelfSuspendResumed; //We're fine, don't suspend

	case STATE_ASYNC_SUSPEND_REQUESTED: //Async suspend requested, service it with a self suspend
	case STATE_SELF_SUSPEND_REQUESTED: //Start the self suspend process
		if (!(suspend_count > 0))
			mono_fatal_with_history ("suspend_count = %d, but should be > 0", suspend_count);
		if (mono_atomic_cas_i32 (&info->thread_state, build_thread_state (STATE_SELF_SUSPENDED, suspend_count), raw_state) != raw_state)
			goto retry_state_change;
		trace_state_change ("STATE_POLL", info, raw_state, STATE_SELF_SUSPENDED, 0);
		if (cur_state == STATE_SELF_SUSPEND_REQUESTED)
			return SelfSuspendWait; //Caller should wait for resume
		else
			return SelfSuspendNotifyAndWait; //Caller should notify suspend initiator and wait for resume

/*
STATE_ASYNC_SUSPENDED: Code should not be running while suspended.
STATE_SELF_SUSPENDED: Code should not be running while suspended.
STATE_BLOCKING:
STATE_BLOCKING_AND_SUSPENDED: Pool is a local state transition. No VM activities are allowed while in blocking mode.
*/
	default:
		mono_fatal_with_history ("Cannot transition thread %p from %s with STATE_POLL", mono_thread_info_get_tid (info), state_name (cur_state));
	}
}

/*
Try to resume a suspended thread.

Returns one of the following values:
- Sucess: The thread was resumed.
- Error: The thread was not suspended in the first place. [2]
- InitSelfResume: The thread is blocked on self suspend and should be resumed 
- InitAsycResume: The thread is blocked on async suspend and should be resumed
- ResumeInitBlockingResume: The thread was suspended on the exit path of blocking state and should be resumed

[2] This threading system uses an unsigned suspend count. Which means a resume cannot be
used as a suspend permit and cancel each other.

Suspend permits are really useful to implement managed synchronization structures that
don't consume native resources. The downside is that they further complicate the design of this
system as the RUNNING state now has a non zero suspend counter.

It can be implemented in the future if we find resume/suspend races that cannot be (efficiently) fixed by other means.

One major issue with suspend permits is runtime facilities (GC, debugger) that must have the target suspended when requested.
This would make permits really harder to add.
*/
MonoResumeResult
mono_threads_transition_request_resume (MonoThreadInfo* info)
{
	int raw_state, cur_state, suspend_count;
	g_assert (info != mono_thread_info_current ()); //One can't self resume [3]

retry_state_change:
	UNWRAP_THREAD_STATE (raw_state, cur_state, suspend_count, info);
	switch (cur_state) {
	case STATE_RUNNING: //Thread already running.
		if (!(suspend_count == 0))
			mono_fatal_with_history ("suspend_count = %d, but should be == 0", suspend_count);
		trace_state_change ("RESUME", info, raw_state, cur_state, 0);
		return ResumeError; //Resume failed because thread was not blocked

	case STATE_BLOCKING: //Blocking, might have a suspend count, we decrease if it's > 0
		if (suspend_count == 0) {
			trace_state_change ("RESUME", info, raw_state, cur_state, 0);
			return ResumeError;
		} else {
			if (mono_atomic_cas_i32 (&info->thread_state, build_thread_state (cur_state, suspend_count - 1), raw_state) != raw_state)
					goto retry_state_change;
			trace_state_change ("RESUME", info, raw_state, cur_state, -1);
			return ResumeOk; //Resume worked and there's nothing for the caller to do.
		}
		break;
	case STATE_ASYNC_SUSPENDED:
	case STATE_SELF_SUSPENDED:
	case STATE_BLOCKING_AND_SUSPENDED: //Decrease the suspend_count and maybe resume
		if (!(suspend_count > 0))
			mono_fatal_with_history ("suspend_count = %d, but should be > 0", suspend_count);
		if (suspend_count > 1) {
			if (mono_atomic_cas_i32 (&info->thread_state, build_thread_state (cur_state, suspend_count - 1), raw_state) != raw_state)
					goto retry_state_change;
			trace_state_change ("RESUME", info, raw_state, cur_state, -1);

			return ResumeOk; //Resume worked and there's nothing for the caller to do.
		} else {
			if (mono_atomic_cas_i32 (&info->thread_state, STATE_RUNNING, raw_state) != raw_state)
				goto retry_state_change;
			trace_state_change ("RESUME", info, raw_state, STATE_RUNNING, -1);

			if (cur_state == STATE_ASYNC_SUSPENDED)
				return ResumeInitAsyncResume; //Resume worked and caller must do async resume
			else if (cur_state == STATE_SELF_SUSPENDED)
				return ResumeInitSelfResume; //Resume worked and caller must do self resume
			else
				return ResumeInitBlockingResume; //Resume worked and caller must do blocking resume
		}

	case STATE_SELF_SUSPEND_REQUESTED: //Self suspend was requested but another thread decided to resume it.
		if (!(suspend_count > 0))
			mono_fatal_with_history ("suspend_count = %d, but should be > 0", suspend_count);
		if (suspend_count > 1) {
			if (mono_atomic_cas_i32 (&info->thread_state, build_thread_state (cur_state, suspend_count - 1), raw_state) != raw_state)
					goto retry_state_change;
			trace_state_change ("RESUME", info, raw_state, cur_state, -1);
		} else {
			if (mono_atomic_cas_i32 (&info->thread_state, STATE_RUNNING, raw_state) != raw_state)
				goto retry_state_change;
			trace_state_change ("RESUME", info, raw_state, STATE_RUNNING, -1);
		}
		return ResumeOk; //Resume worked and there's nothing for the caller to do (the target never actually suspend).
/*

STATE_ASYNC_SUSPEND_REQUESTED: Only one async suspend/resume operation can be in flight, so a resume cannot witness an internal state of suspend

[3] A self-resume makes no sense given it requires the thread to be running, which means its suspend count must be zero. A self resume would make
sense as a suspend permit, but as explained in [2] we don't support it so this is a bug.

[4] It's questionable on whether a resume (an async operation) should be able to cancel a self suspend. The scenario where this would happen
is similar to the one described in [2] when this is used for as a synchronization primitive.

If this turns to be a problem we should either implement [2] or make this an invalid transition.

*/
	default:
		mono_fatal_with_history ("Cannot transition thread %p from %s with REQUEST_RESUME", mono_thread_info_get_tid (info), state_name (cur_state));
	}
}

/*
This performs the last step of async suspend.

Returns TRUE if the caller should wait for resume.
*/
gboolean
mono_threads_transition_finish_async_suspend (MonoThreadInfo* info)
{
	int raw_state, cur_state, suspend_count;

retry_state_change:
	UNWRAP_THREAD_STATE (raw_state, cur_state, suspend_count, info);
	switch (cur_state) {

	case STATE_SELF_SUSPENDED: //async suspend raced with self suspend and lost
	case STATE_BLOCKING_AND_SUSPENDED: //async suspend raced with blocking and lost
		trace_state_change ("FINISH_ASYNC_SUSPEND", info, raw_state, cur_state, 0);
		return FALSE; //let self suspend wait

	case STATE_ASYNC_SUSPEND_REQUESTED:
		if (mono_atomic_cas_i32 (&info->thread_state, build_thread_state (STATE_ASYNC_SUSPENDED, suspend_count), raw_state) != raw_state)
			goto retry_state_change;
		trace_state_change ("FINISH_ASYNC_SUSPEND", info, raw_state, STATE_ASYNC_SUSPENDED, 0);
		return TRUE; //Async suspend worked, now wait for resume

/*
STATE_RUNNING: A thread cannot escape suspension once requested.
STATE_ASYNC_SUSPENDED: There can be only one suspend initiator at a given time, meaning this state should have been visible on the first stage of suspend.
STATE_SELF_SUSPEND_REQUESTED: When self suspend and async suspend happen together, they converge to async suspend so this state should not be visible.
STATE_BLOCKING: Async suspend only begins if a transition to async suspend requested happened. Blocking would have put us into blocking with positive suspend count if it raced with async finish.
*/
	default:
		mono_fatal_with_history ("Cannot transition thread %p from %s with FINISH_ASYNC_SUSPEND", mono_thread_info_get_tid (info), state_name (cur_state));
	}
}

/*
This transitions the thread into a cooperative state where it's assumed to be suspended but can continue.

Native runtime code might want to put itself into a state where the thread is considered suspended but can keep running.
That state only works as long as the only managed state touched is blitable and was pinned before the transition.

It returns the action the caller must perform:

- Continue: Entered blocking state sucessfully;
- PollAndRetry: Async suspend raced and won, try to suspend and then retry;

*/
MonoDoBlockingResult
mono_threads_transition_do_blocking (MonoThreadInfo* info)
{
	int raw_state, cur_state, suspend_count;

retry_state_change:
	UNWRAP_THREAD_STATE (raw_state, cur_state, suspend_count, info);
	switch (cur_state) {

	case STATE_RUNNING: //transition to blocked
		if (!(suspend_count == 0))
			mono_fatal_with_history ("suspend_count = %d, but should be == 0", suspend_count);
		if (mono_atomic_cas_i32 (&info->thread_state, build_thread_state (STATE_BLOCKING, suspend_count), raw_state) != raw_state)
			goto retry_state_change;
		trace_state_change ("DO_BLOCKING", info, raw_state, STATE_BLOCKING, 0);
		return DoBlockingContinue;

	case STATE_ASYNC_SUSPEND_REQUESTED:
		if (!(suspend_count > 0))
			mono_fatal_with_history ("suspend_count = %d, but should be > 0", suspend_count);
		trace_state_change ("DO_BLOCKING", info, raw_state, cur_state, 0);
		return DoBlockingPollAndRetry;
/*
STATE_ASYNC_SUSPENDED
STATE_SELF_SUSPENDED: Code should not be running while suspended.
STATE_SELF_SUSPEND_REQUESTED: A blocking operation must not be done while trying to self suspend
STATE_BLOCKING:
STATE_BLOCKING_AND_SUSPENDED: Blocking is not nestabled
*/
	default:
		mono_fatal_with_history ("Cannot transition thread %p from %s with DO_BLOCKING", mono_thread_info_get_tid (info), state_name (cur_state));
	}
}

/*
This is the exit transition from the blocking state. If this thread is logically async suspended it will have to wait
until its resumed before continuing.

It returns one of:
-Aborted: The blocking operation was aborted and not properly restored. Aborts can happen due to lazy loading and some n2m transitions;
-Ok: Done with blocking, just move on;
-Wait: This thread was async suspended, wait for resume

*/
MonoDoneBlockingResult
mono_threads_transition_done_blocking (MonoThreadInfo* info)
{
	int raw_state, cur_state, suspend_count;

retry_state_change:
	UNWRAP_THREAD_STATE (raw_state, cur_state, suspend_count, info);
	switch (cur_state) {
	case STATE_BLOCKING:
		if (suspend_count == 0) {
			if (mono_atomic_cas_i32 (&info->thread_state, build_thread_state (STATE_RUNNING, suspend_count), raw_state) != raw_state)
				goto retry_state_change;
			trace_state_change ("DONE_BLOCKING", info, raw_state, STATE_RUNNING, 0);
			return DoneBlockingOk;
		} else {
			if (!(suspend_count >= 0))
				mono_fatal_with_history ("suspend_count = %d, but should be >= 0", suspend_count);
			if (mono_atomic_cas_i32 (&info->thread_state, build_thread_state (STATE_BLOCKING_AND_SUSPENDED, suspend_count), raw_state) != raw_state)
				goto retry_state_change;
			trace_state_change ("DONE_BLOCKING", info, raw_state, STATE_BLOCKING_AND_SUSPENDED, 0);
			return DoneBlockingWait;
		}

/*
STATE_RUNNING: //Blocking was aborted and not properly restored
STATE_ASYNC_SUSPEND_REQUESTED: //Blocking was aborted, not properly restored and now there's a pending suspend
STATE_ASYNC_SUSPENDED
STATE_SELF_SUSPENDED: Code should not be running while suspended.
STATE_SELF_SUSPEND_REQUESTED: A blocking operation must not be done while trying to self suspend
STATE_BLOCKING_AND_SUSPENDED: This an exit state of done blocking
*/
	default:
		mono_fatal_with_history ("Cannot transition thread %p from %s with DONE_BLOCKING", mono_thread_info_get_tid (info), state_name (cur_state));
	}
}

/*
Transition a thread in what should be a blocking state back to running state.
This is different that done blocking because the goal is to get back to blocking once we're done.
This is required to be able to bail out of blocking in case we're back to inside the runtime.

It returns one of:
-Ignore: Thread was not in blocking, nothing to do;
-IgnoreAndPool: Thread was not blocking and there's a pending suspend that needs to be processed;
-Ok: Blocking state successfully aborted;
-Wait: Blocking state successfully aborted, there's a pending suspend to be processed though
*/
MonoAbortBlockingResult
mono_threads_transition_abort_blocking (THREAD_INFO_TYPE* info)
{
	int raw_state, cur_state, suspend_count;

retry_state_change:
	UNWRAP_THREAD_STATE (raw_state, cur_state, suspend_count, info);
	switch (cur_state) {
	case STATE_RUNNING: //thread already in runnable state
		trace_state_change ("ABORT_BLOCKING", info, raw_state, cur_state, 0);
		return AbortBlockingIgnore;

	case STATE_ASYNC_SUSPEND_REQUESTED: //thread is runnable and have a pending suspend
		trace_state_change ("ABORT_BLOCKING", info, raw_state, cur_state, 0);
		return AbortBlockingIgnoreAndPoll;

	case STATE_BLOCKING:
		if (suspend_count == 0) {
			if (mono_atomic_cas_i32 (&info->thread_state, build_thread_state (STATE_RUNNING, suspend_count), raw_state) != raw_state)
				goto retry_state_change;
			trace_state_change ("ABORT_BLOCKING", info, raw_state, STATE_RUNNING, 0);
			return AbortBlockingOk;
		} else {
			if (!(suspend_count > 0))
				mono_fatal_with_history ("suspend_count = %d, but should be > 0", suspend_count);
			if (mono_atomic_cas_i32 (&info->thread_state, build_thread_state (STATE_BLOCKING_AND_SUSPENDED, suspend_count), raw_state) != raw_state)
				goto retry_state_change;
			trace_state_change ("ABORT_BLOCKING", info, raw_state, STATE_BLOCKING_AND_SUSPENDED, 0);
			return AbortBlockingWait;
		}
/*
STATE_ASYNC_SUSPENDED:
STATE_SELF_SUSPENDED: Code should not be running while suspended.
STATE_SELF_SUSPEND_REQUESTED: A blocking operation must not be done while trying to self suspend.
STATE_BLOCKING_AND_SUSPENDED: This is an exit state of done blocking, can't happen here.
*/
	default:
		mono_fatal_with_history ("Cannot transition thread %p from %s with DONE_BLOCKING", mono_thread_info_get_tid (info), state_name (cur_state));
	}
}

MonoThreadUnwindState*
mono_thread_info_get_suspend_state (MonoThreadInfo *info)
{
	int raw_state, cur_state, suspend_count;
	UNWRAP_THREAD_STATE (raw_state, cur_state, suspend_count, info);
	switch (cur_state) {
	case STATE_ASYNC_SUSPENDED:
		return &info->thread_saved_state [ASYNC_SUSPEND_STATE_INDEX];
	case STATE_SELF_SUSPENDED:
	case STATE_BLOCKING_AND_SUSPENDED:
		return &info->thread_saved_state [SELF_SUSPEND_STATE_INDEX];
	case STATE_BLOCKING:
		if (suspend_count > 0)
			return &info->thread_saved_state [SELF_SUSPEND_STATE_INDEX];
	default:
/*
STATE_RUNNING
STATE_SELF_SUSPENDED
STATE_ASYNC_SUSPEND_REQUESTED
STATE_BLOCKING: All those are invalid suspend states.
*/
		g_error ("Cannot read suspend state when target %p is in the %s state", mono_thread_info_get_tid (info), state_name (cur_state));
	}
}

// State checking code
/**
 * Return TRUE is the thread is in a runnable state.
*/
gboolean
mono_thread_info_is_running (MonoThreadInfo *info)
{
	switch (get_thread_state (info->thread_state)) {
	case STATE_RUNNING:
	case STATE_ASYNC_SUSPEND_REQUESTED:
	case STATE_SELF_SUSPEND_REQUESTED:
	case STATE_BLOCKING:
		return TRUE;
	}
	return FALSE;
}

/**
 * Return TRUE is the thread is in an usable (suspendable) state
 */
gboolean
mono_thread_info_is_live (MonoThreadInfo *info)
{
	switch (get_thread_state (info->thread_state)) {
	case STATE_STARTING:
	case STATE_DETACHED:
		return FALSE;
	}
	return TRUE;
}

int
mono_thread_info_suspend_count (MonoThreadInfo *info)
{
	return get_thread_suspend_count (info->thread_state);
}

int
mono_thread_info_current_state (MonoThreadInfo *info)
{
	return get_thread_state (info->thread_state);
}

const char*
mono_thread_state_name (int state)
{
	return state_name (state);
}

gboolean
mono_thread_is_gc_unsafe_mode (void)
{
	MonoThreadInfo *cur = mono_thread_info_current ();

	if (!cur)
		return FALSE;

	switch (mono_thread_info_current_state (cur)) {
	case STATE_RUNNING:
	case STATE_ASYNC_SUSPEND_REQUESTED:
	case STATE_SELF_SUSPEND_REQUESTED:
		return TRUE;
	default:
		return FALSE;
	}
}
