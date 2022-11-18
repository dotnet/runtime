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
static int
get_thread_state (int thread_state)
{
	const MonoThreadStateMachine state = {thread_state};
	return state.state;
}

#if defined (THREADS_STATE_MACHINE_DEBUG_ENABLED) || defined (ENABLE_CHECKED_BUILD_THREAD)
static int
get_thread_suspend_count (int thread_state)
{
	const MonoThreadStateMachine state = {thread_state};
	return state.suspend_count;
}
#endif

#ifdef THREADS_STATE_MACHINE_DEBUG_ENABLED
static gboolean
get_thread_no_safepoints (int thread_state)
{
	const MonoThreadStateMachine state = {thread_state};
	return state.no_safepoints;
}
#endif

static MonoThreadStateMachine
build_thread_state (int thread_state, int suspend_count, gboolean no_safepoints)
{
	g_assert (suspend_count >= 0 && suspend_count <= THREAD_SUSPEND_COUNT_MAX);
	g_assert (thread_state >= 0 && thread_state <= STATE_MAX);
	no_safepoints = !!no_safepoints; // ensure it's 0 or 1

	/* need a predictable value for the unused bits so that
	 * thread_state_cas does not fail.
	 */
	MonoThreadStateMachine state = { 0 };
	state.state = thread_state;
	state.no_safepoints = no_safepoints;
	state.suspend_count = suspend_count;
	return state;
}

static int
thread_state_cas (MonoThreadStateMachine *state, MonoThreadStateMachine new_value, int old_raw)
{
	return mono_atomic_cas_i32 (&state->raw, new_value.raw, old_raw);
}

static const char*
state_name (int state)
{
	static const char *state_names [] = {
		"STARTING",
		"DETACHED",

		"RUNNING",
		"ASYNC_SUSPENDED",
		"SELF_SUSPENDED",
		"ASYNC_SUSPEND_REQUESTED",

		"STATE_BLOCKING",
		"STATE_BLOCKING_ASYNC_SUSPENDED",
		"STATE_BLOCKING_SELF_SUSPENDED",
		"STATE_BLOCKING_SUSPEND_REQUESTED",
	};
	return state_names [get_thread_state (state)];
}

static void
unwrap_thread_state (MonoThreadInfo* info,
		     int *raw,
		     int *cur,
		     int *count,
		     int *blk)
{
	g_static_assert (sizeof (MonoThreadStateMachine) == sizeof (int32_t));
	const MonoThreadStateMachine state = {mono_atomic_load_i32 (&info->thread_state.raw)};
	// Read once from info and then read from local to get consistent values.
	*raw = state.raw;
	*cur = state.state;
	*count = state.suspend_count;
	*blk = state.no_safepoints;
}

#define UNWRAP_THREAD_STATE(RAW,CUR,COUNT,BLK,INFO) \
	unwrap_thread_state ((INFO), &(RAW), &(CUR), &(COUNT), &(BLK))

static void
check_thread_state (MonoThreadInfo* info)
{
#ifdef ENABLE_CHECKED_BUILD
	int raw_state, cur_state, suspend_count;
	gboolean no_safepoints;
	UNWRAP_THREAD_STATE (raw_state, cur_state, suspend_count, no_safepoints, info);
	switch (cur_state) {
	case STATE_STARTING:
	case STATE_DETACHED:
		g_assert (!no_safepoints);
		/* fallthru */
	case STATE_RUNNING:
		g_assert (suspend_count == 0);
		break;
	case STATE_BLOCKING_SELF_SUSPENDED:
	case STATE_BLOCKING_SUSPEND_REQUESTED:
	case STATE_BLOCKING_ASYNC_SUSPENDED:
	case STATE_ASYNC_SUSPENDED:
	case STATE_SELF_SUSPENDED:
		g_assert (!no_safepoints);
		/* fallthru */
	case STATE_ASYNC_SUSPEND_REQUESTED:
		g_assertf (suspend_count > 0, "expected suspend_count > 0 in current state: %s, suspend_count == %d", state_name(cur_state), suspend_count);
		break;
	case STATE_BLOCKING:
		g_assert (!no_safepoints);
		g_assert (suspend_count == 0);
		break;
	default:
		g_error ("Invalid state %d", cur_state);
	}
#endif
}

static void
trace_state_change_with_func (const char *transition, MonoThreadInfo *info, int cur_raw_state, int next_state, gboolean next_no_safepoints, int suspend_count_delta, const char *func)
{
	check_thread_state (info);
	THREADS_STATE_MACHINE_DEBUG ("[%s][%p] %s %s -> %s %s (%d -> %d) %s\n",
		transition,
		mono_thread_info_get_tid (info),
		state_name (get_thread_state (cur_raw_state)),
		(get_thread_no_safepoints (cur_raw_state) ? "X" : "."),
		state_name (next_state),
		(next_no_safepoints ? "X" : "."),
		get_thread_suspend_count (cur_raw_state),
		get_thread_suspend_count (cur_raw_state) + suspend_count_delta,
		func);

	CHECKED_BUILD_THREAD_TRANSITION (transition, info, get_thread_state (cur_raw_state), get_thread_suspend_count (cur_raw_state), next_state, suspend_count_delta);
}

static void
trace_state_change_sigsafe (const char *transition, MonoThreadInfo *info, int cur_raw_state, int next_state, gboolean next_no_safepoints, int suspend_count_delta, const char *func)
{
	check_thread_state (info);
	THREADS_STATE_MACHINE_DEBUG ("[%s][%p] %s %s -> %s %s (%d -> %d) %s\n",
		transition,
		mono_thread_info_get_tid (info),
		state_name (get_thread_state (cur_raw_state)),
		(get_thread_no_safepoints (cur_raw_state) ? "X" : "."),
		state_name (next_state),
		(next_no_safepoints ? "X" : "."),
		get_thread_suspend_count (cur_raw_state),
		get_thread_suspend_count (cur_raw_state) + suspend_count_delta,
		func);

	CHECKED_BUILD_THREAD_TRANSITION_NOBT (transition, info, get_thread_state (cur_raw_state), get_thread_suspend_count (cur_raw_state), next_state, suspend_count_delta);
}

static void
trace_state_change (const char *transition, MonoThreadInfo *info, int cur_raw_state, int next_state, gboolean next_no_safepoints, int suspend_count_delta)
// FIXME migrate all uses
{
	trace_state_change_with_func (transition, info, cur_raw_state, next_state, next_no_safepoints, suspend_count_delta, "");
}

/*
This is the transition that signals that a thread is functioning.
Its main goal is to catch threads been witnessed before been fully registered.
*/
void
mono_threads_transition_attach (MonoThreadInfo* info)
{
	int raw_state, cur_state, suspend_count;
	gboolean no_safepoints;

retry_state_change:
	UNWRAP_THREAD_STATE (raw_state, cur_state, suspend_count, no_safepoints, info);
	switch (cur_state) {
	case STATE_STARTING:
		if (!(suspend_count == 0))
			mono_fatal_with_history ("suspend_count = %d, but should be == 0", suspend_count);
		if (no_safepoints)
			mono_fatal_with_history ("no_safepoints = TRUE, but should be FALSE");
		if (thread_state_cas (&info->thread_state, build_thread_state (STATE_RUNNING, 0, 0), raw_state) != raw_state)
			goto retry_state_change;
		trace_state_change ("ATTACH", info, raw_state, STATE_RUNNING, FALSE, 0);
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
	gboolean no_safepoints;

retry_state_change:
	UNWRAP_THREAD_STATE (raw_state, cur_state, suspend_count, no_safepoints, info);
	switch (cur_state) {
	case STATE_RUNNING:
	case STATE_BLOCKING: /* An OS thread on coop goes STARTING->BLOCKING->RUNNING->BLOCKING->DETACHED */
		if (!(suspend_count == 0))
			mono_fatal_with_history ("suspend_count = %d, but should be == 0", suspend_count);
		if (no_safepoints)
			mono_fatal_with_history ("no_safepoints = TRUE, but should be FALSE");
		if (thread_state_cas (&info->thread_state, build_thread_state (STATE_DETACHED, 0, 0), raw_state) != raw_state)
			goto retry_state_change;
		trace_state_change ("DETACH", info, raw_state, STATE_DETACHED, FALSE, 0);
		return TRUE;
	case STATE_ASYNC_SUSPEND_REQUESTED: //Can't detach until whoever asked us to suspend to be happy with us
	case STATE_BLOCKING_SUSPEND_REQUESTED:
		return FALSE;

/*
STATE_ASYNC_SUSPENDED: Code should not be running while suspended.
STATE_SELF_SUSPENDED: Code should not be running while suspended.
STATE_BLOCKING_SELF_SUSPENDED: This is a bug in coop x suspend that resulted the thread in an undetachable state.
STATE_BLOCKING_ASYNC_SUSPENDED: Same as BLOCKING_SELF_SUSPENDED
*/
	default:
		mono_fatal_with_history ("Cannot transition current thread %p from %s with DETACH", info, state_name (cur_state));
	}
}

/*
This transition initiates the suspension of another thread.

Returns one of the following values:

- ReqSuspendInitSuspendRunning: Thread suspend requested, caller must initiate suspend.
- ReqSuspendInitSuspendBlocking: Thread in blocking state, caller may initiate suspend.
- ReqSuspendAlreadySuspended: Thread was already suspended and not executing, nothing to do.
- ReqSuspendAlreadySuspendedBlocking: Thread was already in blocking and a suspend was requested
                                      and the thread is still executing (perhaps in a syscall),
                                      nothing to do.
*/
MonoRequestSuspendResult
mono_threads_transition_request_suspension (MonoThreadInfo *info)
{
	int raw_state, cur_state, suspend_count;
	gboolean no_safepoints;
	g_assert (info != mono_thread_info_current ());

retry_state_change:
	UNWRAP_THREAD_STATE (raw_state, cur_state, suspend_count, no_safepoints, info);

	switch (cur_state) {
	case STATE_RUNNING: //Post an async suspend request
		if (!(suspend_count == 0))
			mono_fatal_with_history ("suspend_count = %d, but should be == 0", suspend_count);
		if (thread_state_cas (&info->thread_state, build_thread_state (STATE_ASYNC_SUSPEND_REQUESTED, 1, no_safepoints), raw_state) != raw_state)
			goto retry_state_change;
		trace_state_change ("SUSPEND_INIT_REQUESTED", info, raw_state, STATE_ASYNC_SUSPEND_REQUESTED, no_safepoints, 1);
		return ReqSuspendInitSuspendRunning; //This is the first async suspend request against the target

	case STATE_BLOCKING_SELF_SUSPENDED:
	case STATE_BLOCKING_ASYNC_SUSPENDED:
	case STATE_ASYNC_SUSPENDED:
	case STATE_SELF_SUSPENDED:
		if (no_safepoints)
			mono_fatal_with_history ("no_safepoints = TRUE, but should be FALSE");
		if (!(suspend_count > 0 && suspend_count < THREAD_SUSPEND_COUNT_MAX))
			mono_fatal_with_history ("suspend_count = %d, but should be > 0 and < THREAD_SUSPEND_COUNT_MAX, for thread %d", suspend_count, mono_thread_info_get_tid (info));
		if (thread_state_cas (&info->thread_state, build_thread_state (cur_state, suspend_count + 1, no_safepoints), raw_state) != raw_state)
			goto retry_state_change;
		trace_state_change ("SUSPEND_INIT_REQUESTED", info, raw_state, cur_state, no_safepoints, 1);
		return ReqSuspendAlreadySuspended; //Thread is already suspended so we don't need to wait it to suspend

	case STATE_BLOCKING:
		if (!(suspend_count == 0))
			mono_fatal_with_history ("suspend_count = %d, but should be == 0", suspend_count);
		if (no_safepoints)
			mono_fatal_with_history ("no_safepoints = TRUE, but should be FALSE");
		if (thread_state_cas (&info->thread_state, build_thread_state (STATE_BLOCKING_SUSPEND_REQUESTED, 1, no_safepoints), raw_state) != raw_state)
			goto retry_state_change;
		trace_state_change ("SUSPEND_INIT_REQUESTED", info, raw_state, STATE_BLOCKING_SUSPEND_REQUESTED, no_safepoints, 1);
		return ReqSuspendInitSuspendBlocking; //A thread in the blocking state has its state saved so we can treat it as suspended.
	case STATE_BLOCKING_SUSPEND_REQUESTED:
		/* This should only be happening if we're doing a cooperative suspend of a blocking thread.
		 * In which case we could be in BLOCKING_SUSPEND_REQUESTED until we execute a done or abort blocking.
		 * In preemptive suspend of a blocking thread since there's a single suspend initiator active at a time,
		 * we would expect a finish_async_suspension or a done/abort blocking before the next suspension request
		 */
		if (!(suspend_count > 0 && suspend_count < THREAD_SUSPEND_COUNT_MAX))
			mono_fatal_with_history ("suspend_count = %d, but should be > 0 and < THREAD_SUSPEND_COUNT_MAX", suspend_count);
		if (no_safepoints)
			mono_fatal_with_history ("no_safepoints = TRUE, but should be FALSE");
		if (thread_state_cas (&info->thread_state, build_thread_state (cur_state, suspend_count + 1, no_safepoints), raw_state) != raw_state)
			goto retry_state_change;
		trace_state_change ("SUSPEND_INIT_REQUESTED", info, raw_state, cur_state, no_safepoints, 1);
		return ReqSuspendAlreadySuspendedBlocking;

/*

[1] It's questionable on what to do if we hit the beginning of a self suspend.
The expected behavior is that the target should poll its state very soon so the suspend latency should be minimal.

STATE_ASYNC_SUSPEND_REQUESTED: Since there can only be one async suspend in progress and it must finish, it should not be possible to witness this.
*/
	default:
		mono_fatal_with_history ("Cannot transition thread %p from %s with SUSPEND_INIT_REQUESTED", mono_thread_info_get_tid (info), state_name (cur_state));
	}
	return (MonoRequestSuspendResult) FALSE;
}


/*
Peek at the thread state and return whether it's BLOCKING_SUSPEND_REQUESTED or not.

Assumes that it is called in the second phase of a two-phase suspend, so the
thread is either some flavor of suspended or else blocking suspend requested.
All other states can't happen.
 */
gboolean
mono_threads_transition_peek_blocking_suspend_requested (MonoThreadInfo *info)
{
	int raw_state, cur_state, suspend_count;
	gboolean no_safepoints;
	g_assert (info != mono_thread_info_current ());

	UNWRAP_THREAD_STATE (raw_state, cur_state, suspend_count, no_safepoints, info);

	switch (cur_state) {
	case STATE_ASYNC_SUSPENDED:
	case STATE_SELF_SUSPENDED:
		return FALSE; /*ReqPeekBlockingSuspendRequestedRunningSuspended;*/
	case STATE_BLOCKING_SELF_SUSPENDED:
	case STATE_BLOCKING_ASYNC_SUSPENDED:
	case STATE_BLOCKING_SUSPEND_REQUESTED:
		if (!(suspend_count > 0 && suspend_count < THREAD_SUSPEND_COUNT_MAX))
			mono_fatal_with_history ("suspend_count = %d, but should be > 0 and < THREAD_SUSPEND_COUNT_MAX", suspend_count);
		if (no_safepoints)
			mono_fatal_with_history ("no_safepoints = TRUE, but should be FALSE");
		if (cur_state == STATE_BLOCKING_SUSPEND_REQUESTED)
			return TRUE; /*ReqPeekBlockingSuspendRequestedBlockingSuspendRequested;*/
		else
			return FALSE; /*ReqPeekBlockingSuspendRequestedBlockingSuspended;*/
/*
 STATE_RUNNING:
   Can't happen - should have been suspended in the first phase.
 STATE_ASYNC_SUSPEND_REQUESTED
   Can't happen - first phase should've waited until the thread self-suspended
 STATE_BLOCKING:
   Can't happen - should've had a suspension request in the first phase.
 */
	default:
		mono_fatal_with_history ("Thread %p in unexpected state %s with PEEK_BLOCKING_SUSPEND_REQUESTED", mono_thread_info_get_tid (info), state_name (cur_state));
	}
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
	gboolean no_safepoints;
	g_assert (mono_thread_info_is_current (info));

retry_state_change:
	UNWRAP_THREAD_STATE (raw_state, cur_state, suspend_count, no_safepoints, info);
	switch (cur_state) {
	case STATE_RUNNING:
		if (no_safepoints)
			mono_fatal_with_history ("no_safepoints = TRUE, but should be FALSE in RUNNING with STATE_POLL");
		if (!(suspend_count == 0))
			mono_fatal_with_history ("suspend_count = %d, but should be == 0", suspend_count);
		trace_state_change ("STATE_POLL", info, raw_state, cur_state, no_safepoints, 0);
		return SelfSuspendResumed; //We're fine, don't suspend

	case STATE_ASYNC_SUSPEND_REQUESTED: //Async suspend requested, service it with a self suspend
		if (no_safepoints)
			mono_fatal_with_history ("no_safepoints = TRUE, but should be FALSE in ASYNS_SUSPEND_REQUESTED with STATE_POLL");
		if (!(suspend_count > 0))
			mono_fatal_with_history ("suspend_count = %d, but should be > 0", suspend_count);
		if (thread_state_cas (&info->thread_state, build_thread_state (STATE_SELF_SUSPENDED, suspend_count, no_safepoints), raw_state) != raw_state)
			goto retry_state_change;
		trace_state_change ("STATE_POLL", info, raw_state, STATE_SELF_SUSPENDED, no_safepoints, 0);
		return SelfSuspendNotifyAndWait; //Caller should notify suspend initiator and wait for resume

/*
STATE_ASYNC_SUSPENDED: Code should not be running while suspended.
STATE_SELF_SUSPENDED: Code should not be running while suspended.
STATE_BLOCKING:
STATE_BLOCKING_SUSPEND_REQUESTED:
STATE_BLOCKING_ASYNC_SUSPENDED:
STATE_BLOCKING_SELF_SUSPENDED: Poll is a local state transition. No VM activities are allowed while in blocking mode.
      (In all the blocking states - the local thread has no checkpoints, hence
      no polling, it can only do abort blocking or done blocking on itself).
*/
	default:
		mono_fatal_with_history ("Cannot transition thread %p from %s with STATE_POLL", mono_thread_info_get_tid (info), state_name (cur_state));
	}
}

/*
Try to resume a suspended thread.

Returns one of the following values:
- Success: The thread was resumed.
- Error: The thread was not suspended in the first place. [2]
- InitSelfResume: The thread is blocked on self suspend and should be resumed
- InitAsyncResume: The thread is blocked on async suspend and should be resumed
- ResumeInitBlockingResume: The thread was suspended on the exit path of blocking state and should be resumed
      FIXME: ResumeInitBlockingResume is just InitSelfResume by a different name.

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
	gboolean no_safepoints;
	g_assert (info != mono_thread_info_current ()); //One can't self resume [3]

retry_state_change:
	UNWRAP_THREAD_STATE (raw_state, cur_state, suspend_count, no_safepoints, info);
	switch (cur_state) {
	case STATE_RUNNING: //Thread already running.
		if (!(suspend_count == 0))
			mono_fatal_with_history ("suspend_count = %d, but should be == 0", suspend_count);
		trace_state_change ("RESUME", info, raw_state, cur_state, no_safepoints, 0);
		return ResumeError; //Resume failed because thread was not blocked

	case STATE_BLOCKING: //Blocking, might have a suspend count, we decrease if it's > 0
		if (!(suspend_count == 0))
			mono_fatal_with_history ("suspend_count = %d, but should be == 0", suspend_count);
		if (no_safepoints)
			mono_fatal_with_history ("no_safepoints = TRUE, but should be FALSE");
		trace_state_change ("RESUME", info, raw_state, cur_state, no_safepoints, 0);
		return ResumeError;
	case STATE_BLOCKING_SUSPEND_REQUESTED:
		if (!(suspend_count > 0))
			mono_fatal_with_history ("suspend_count = %d, but should be > 0", suspend_count);
		if (no_safepoints)
			mono_fatal_with_history ("no_safepoints = TRUE, but should be FALSE");
		if (suspend_count > 1) {
			if (thread_state_cas (&info->thread_state, build_thread_state (cur_state, suspend_count - 1, no_safepoints), raw_state) != raw_state)
				goto retry_state_change;
			trace_state_change ("RESUME", info, raw_state, cur_state, no_safepoints, -1);
			return ResumeOk; //Resume worked and there's nothing for the caller to do.
		} else {
			if (thread_state_cas (&info->thread_state, build_thread_state (STATE_BLOCKING, 0, 0), raw_state) != raw_state)
				goto retry_state_change;
			trace_state_change ("RESUME", info, raw_state, STATE_BLOCKING, no_safepoints, -1);
			return ResumeOk; // Resume worked, back in blocking, nothing for the caller to do.
		}
	case STATE_BLOCKING_ASYNC_SUSPENDED:
		if (!(suspend_count > 0))
			mono_fatal_with_history ("suspend_count = %d, but should be > 0", suspend_count);
		if (no_safepoints)
			mono_fatal_with_history ("no_safepoints = TRUE, but should be FALSE");
		if (suspend_count > 1) {
			if (thread_state_cas (&info->thread_state, build_thread_state (cur_state, suspend_count - 1, no_safepoints), raw_state) != raw_state)
				goto retry_state_change;
			trace_state_change ("RESUME", info, raw_state, cur_state, no_safepoints, -1);
			return ResumeOk; // Resume worked, there's nothing else for the caller to do.
		} else {
			if (thread_state_cas (&info->thread_state, build_thread_state (STATE_BLOCKING, 0, 0), raw_state) != raw_state)
				goto retry_state_change;
			trace_state_change ("RESUME", info, raw_state, STATE_BLOCKING, no_safepoints, -1);
			return ResumeInitAsyncResume; // Resume worked and caller must do async resume, thread resumes in BLOCKING
		}
	case STATE_BLOCKING_SELF_SUSPENDED: //Decrease the suspend_count and maybe resume
	case STATE_ASYNC_SUSPENDED:
	case STATE_SELF_SUSPENDED:
		if (no_safepoints)
			mono_fatal_with_history ("no_safepoints = TRUE, but should be FALSE");
		if (!(suspend_count > 0))
			mono_fatal_with_history ("suspend_count = %d, but should be > 0", suspend_count);
		if (suspend_count > 1) {
			if (thread_state_cas (&info->thread_state, build_thread_state (cur_state, suspend_count - 1, no_safepoints), raw_state) != raw_state)
					goto retry_state_change;
			trace_state_change ("RESUME", info, raw_state, cur_state, no_safepoints, -1);

			return ResumeOk; //Resume worked and there's nothing for the caller to do.
		} else {
			if (thread_state_cas (&info->thread_state, build_thread_state (STATE_RUNNING, 0, no_safepoints), raw_state) != raw_state)
				goto retry_state_change;
			trace_state_change ("RESUME", info, raw_state, STATE_RUNNING, no_safepoints, -1);

			if (cur_state == STATE_ASYNC_SUSPENDED)
				return ResumeInitAsyncResume; //Resume worked and caller must do async resume
			else if (cur_state == STATE_SELF_SUSPENDED)
				return ResumeInitSelfResume; //Resume worked and caller must do self resume
			else
				return ResumeInitBlockingResume; //Resume worked and caller must do blocking resume
		}

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
Try to resume a suspended thread and atomically request that it suspend again.

Returns one of the following values:
- InitAsyncPulse: The thread is suspended with preemptive suspend and should be resumed.
*/
MonoPulseResult
mono_threads_transition_request_pulse (MonoThreadInfo* info)
{
	int raw_state, cur_state, suspend_count;
	gboolean no_safepoints;
	g_assert (info != mono_thread_info_current ()); //One can't self pulse [3]

retry_state_change:
	UNWRAP_THREAD_STATE (raw_state, cur_state, suspend_count, no_safepoints, info);
	switch (cur_state) {
	case STATE_BLOCKING_ASYNC_SUSPENDED:
		if (!(suspend_count == 1))
			mono_fatal_with_history ("suspend_count = %d, but should be == 1", suspend_count);
		if (no_safepoints)
			mono_fatal_with_history ("no_safepoints = TRUE, but should be FALSE");
		if (thread_state_cas (&info->thread_state, build_thread_state (STATE_BLOCKING_SUSPEND_REQUESTED, suspend_count, no_safepoints), raw_state) != raw_state)
			goto retry_state_change;
		trace_state_change ("PULSE", info, raw_state, STATE_BLOCKING_SUSPEND_REQUESTED, no_safepoints, -1);
		return PulseInitAsyncPulse; // Pulse worked and caller must do async pulse, thread pulses in BLOCKING
/*

STATE_RUNNING:
STATE_BLOCKING:
Only one suspend initiator at a time.  Current STW stopped the
thread and now needs to resume it.  So thread must be in one of the suspended
states if we get here.

STATE_BLOCKING_SUSPEND_REQUESTED:
STATE_ASYNC_SUSPEND_REQUESTED:
Only one pulse operation can be in flight, so a pulse cannot witness an
internal state of suspend

STATE_ASYNC_SUSPENDED:
Hybrid suspend shouldn't put GC Unsafe threads into async suspended state.

STATE_BLOCKING_SELF_SUSPENDED:
STATE_SELF_SUSPENDED:
Don't expect these to be pulsed - they're not problematic.
*/
	default:
		mono_fatal_with_history ("Cannot transition thread %p from %s with REQUEST_PULSE", mono_thread_info_get_tid (info), state_name (cur_state));
	}
}

/*
Abort last step of preemptive suspend in case of failure to async suspend thread.
This function makes sure state machine reflects current state of thread (running/suspended)
in case of failure to complete async suspend of thread. NOTE, thread can still have reached
a suspend state (in case of self-suspend).

Returns TRUE if async suspend request was successfully aborted. Thread should be in STATE_RUNNING.
Returns FALSE if async suspend request was successfully aborted but thread already reached self-suspended.
*/
gboolean
mono_threads_transition_abort_async_suspend (MonoThreadInfo* info)
{
	int raw_state, cur_state, suspend_count;
	gboolean no_safepoints;

retry_state_change:
	UNWRAP_THREAD_STATE (raw_state, cur_state, suspend_count, no_safepoints, info);
	switch (cur_state) {
	case STATE_SELF_SUSPENDED: //async suspend raced with self suspend and lost
	case STATE_BLOCKING_SELF_SUSPENDED: //async suspend raced with blocking and lost
		if (no_safepoints)
			mono_fatal_with_history ("no_safepoints = TRUE, but should be FALSE");
		trace_state_change_sigsafe ("ABORT_ASYNC_SUSPEND", info, raw_state, cur_state, no_safepoints, 0, "");
		return FALSE; //thread successfully reached suspend state.
	case STATE_ASYNC_SUSPEND_REQUESTED:
	case STATE_BLOCKING_SUSPEND_REQUESTED:
		if (!(suspend_count > 0))
			mono_fatal_with_history ("suspend_count = %d, but should be > 0", suspend_count);
		if (no_safepoints)
			mono_fatal_with_history ("no_safepoints = TRUE, but should be FALSE");
		if (suspend_count > 1) {
			if (thread_state_cas (&info->thread_state, build_thread_state (cur_state, suspend_count - 1, no_safepoints), raw_state) != raw_state)
				goto retry_state_change;
			trace_state_change ("ABORT_ASYNC_SUSPEND", info, raw_state, cur_state, no_safepoints, -1);
		} else {
			if (thread_state_cas (&info->thread_state, build_thread_state (STATE_RUNNING, 0, no_safepoints), raw_state) != raw_state)
				goto retry_state_change;
			trace_state_change ("ABORT_ASYNC_SUSPEND", info, raw_state, STATE_RUNNING, no_safepoints, -1);
		}
		return TRUE; //aborting thread suspend request succeeded, thread is running.

/*
STATE_RUNNING: A thread cannot escape suspension once requested.
STATE_ASYNC_SUSPENDED: There can be only one suspend initiator at a given time, meaning this state should have been visible on the first stage of suspend.
STATE_BLOCKING: If a thread is subject to preemptive suspend, there is no race as the resume initiator should have suspended the thread to STATE_BLOCKING_ASYNC_SUSPENDED or STATE_BLOCKING_SELF_SUSPENDED before resuming.
				With cooperative suspend, there are no finish_async_suspend transitions since there's no path back from asyns_suspend requested to running.
STATE_BLOCKING_ASYNC_SUSPENDED: There can only be one suspend initiator at a given time, meaning this state should have ben visible on the first stage of suspend.
*/
	default:
		mono_fatal_with_history ("Cannot transition thread %p from %s with ABORT_ASYNC_SUSPEND", mono_thread_info_get_tid (info), state_name (cur_state));
	}
}

/*
This performs the last step of preemptive suspend.

Returns TRUE if the caller should wait for resume.
*/
gboolean
mono_threads_transition_finish_async_suspend (MonoThreadInfo* info)
{
	int raw_state, cur_state, suspend_count;
	gboolean no_safepoints;

retry_state_change:
	UNWRAP_THREAD_STATE (raw_state, cur_state, suspend_count, no_safepoints, info);
	switch (cur_state) {

	case STATE_SELF_SUSPENDED: //async suspend raced with self suspend and lost
	case STATE_BLOCKING_SELF_SUSPENDED: //async suspend raced with blocking and lost
		if (no_safepoints)
			mono_fatal_with_history ("no_safepoints = TRUE, but should be FALSE");
		trace_state_change_sigsafe ("FINISH_ASYNC_SUSPEND", info, raw_state, cur_state, no_safepoints, 0, "");
		return FALSE; //let self suspend wait

	case STATE_ASYNC_SUSPEND_REQUESTED:
		if (!(suspend_count > 0))
			mono_fatal_with_history ("suspend_count = %d, but should be > 0", suspend_count);
		/* Don't expect to see no_safepoints, ever, with async */
		if (no_safepoints)
			mono_fatal_with_history ("no_safepoints = TRUE, but should be FALSE in ASYNC_SUSPEND_REQUESTED with FINISH_ASYNC_SUSPEND");
		if (thread_state_cas (&info->thread_state, build_thread_state (STATE_ASYNC_SUSPENDED, suspend_count, FALSE), raw_state) != raw_state)
			goto retry_state_change;
		trace_state_change_sigsafe ("FINISH_ASYNC_SUSPEND", info, raw_state, STATE_ASYNC_SUSPENDED, FALSE, 0, "");
		return TRUE; //Async suspend worked, now wait for resume
	case STATE_BLOCKING_SUSPEND_REQUESTED:
		if (!(suspend_count > 0))
			mono_fatal_with_history ("suspend_count = %d, but should be > 0", suspend_count);
		if (no_safepoints)
			mono_fatal_with_history ("no_safepoints = TRUE, but should be FALSE");
		if (thread_state_cas (&info->thread_state, build_thread_state (STATE_BLOCKING_ASYNC_SUSPENDED, suspend_count, FALSE), raw_state) != raw_state)
			goto retry_state_change;
		trace_state_change_sigsafe ("FINISH_ASYNC_SUSPEND", info, raw_state, STATE_BLOCKING_ASYNC_SUSPENDED, FALSE, 0, "");
		return TRUE; //Async suspend of blocking thread worked, now wait for resume

/*
STATE_RUNNING: A thread cannot escape suspension once requested.
STATE_ASYNC_SUSPENDED: There can be only one suspend initiator at a given time, meaning this state should have been visible on the first stage of suspend.
STATE_BLOCKING: If a thread is subject to preemptive suspend, there is no race as the resume initiator should have suspended the thread to STATE_BLOCKING_ASYNC_SUSPENDED or STATE_BLOCKING_SELF_SUSPENDED before resuming.
                With cooperative suspend, there are no finish_async_suspend transitions since there's no path back from asyns_suspend requested to running.
STATE_BLOCKING_ASYNC_SUSPENDED: There can only be one suspend initiator at a given time, meaning this state should have ben visible on the first stage of suspend.
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

- Continue: Entered blocking state successfully;
- PollAndRetry: Async suspend raced and won, try to suspend and then retry;

*/
MonoDoBlockingResult
mono_threads_transition_do_blocking (MonoThreadInfo* info, const char *func)
{
	int raw_state, cur_state, suspend_count;
	gboolean no_safepoints;

retry_state_change:
	UNWRAP_THREAD_STATE (raw_state, cur_state, suspend_count, no_safepoints, info);
	switch (cur_state) {

	case STATE_RUNNING: //transition to blocked
		if (!(suspend_count == 0))
			mono_fatal_with_history ("suspend_count = %d, but should be == 0", suspend_count);
		if (no_safepoints)
			mono_fatal_with_history ("no_safepoints = TRUE, but should be FALSE in state RUNNING with DO_BLOCKING");
		if (thread_state_cas (&info->thread_state, build_thread_state (STATE_BLOCKING, suspend_count, no_safepoints), raw_state) != raw_state)
			goto retry_state_change;
		trace_state_change ("DO_BLOCKING", info, raw_state, STATE_BLOCKING, no_safepoints, 0);
		return DoBlockingContinue;

	case STATE_ASYNC_SUSPEND_REQUESTED:
		if (!(suspend_count > 0))
			mono_fatal_with_history ("suspend_count = %d, but should be > 0", suspend_count);
		if (no_safepoints)
			mono_fatal_with_history ("no_safepoints = TRUE, but should be FALSE in state ASYNC_SUSPEND_REQUESTED with DO_BLOCKING");
		trace_state_change ("DO_BLOCKING", info, raw_state, cur_state, no_safepoints, 0);
		return DoBlockingPollAndRetry;
/*
STATE_ASYNC_SUSPENDED
STATE_SELF_SUSPENDED: Code should not be running while suspended.
STATE_BLOCKING:
STATE_BLOCKING_SUSPEND_REQUESTED:
STATE_BLOCKING_SELF_SUSPENDED: Blocking is not nestabled
STATE_BLOCKING_ASYNC_SUSPENDED: Blocking is not nestable _and_ code should not be running while suspended
*/
	default:
		mono_fatal_with_history ("%s Cannot transition thread %p from %s with DO_BLOCKING", func, mono_thread_info_get_tid (info), state_name (cur_state));
	}
}

/*
This is the exit transition from the blocking state. If this thread is logically async suspended it will have to wait
until its resumed before continuing.

It returns one of:
-Ok: Done with blocking, just move on;
-Wait: This thread was suspended while in blocking, wait for resume.
*/
MonoDoneBlockingResult
mono_threads_transition_done_blocking (MonoThreadInfo* info, const char *func)
{
	int raw_state, cur_state, suspend_count;
	gboolean no_safepoints;

retry_state_change:
	UNWRAP_THREAD_STATE (raw_state, cur_state, suspend_count, no_safepoints, info);
	switch (cur_state) {
	case STATE_BLOCKING:
		if (!(suspend_count == 0))
			mono_fatal_with_history ("%s suspend_count = %d, but should be == 0", func, suspend_count);
		if (no_safepoints)
			mono_fatal_with_history ("no_safepoints = TRUE, but should be FALSE");
		if (thread_state_cas (&info->thread_state, build_thread_state (STATE_RUNNING, suspend_count, no_safepoints), raw_state) != raw_state)
			goto retry_state_change;
		trace_state_change_sigsafe ("DONE_BLOCKING", info, raw_state, STATE_RUNNING, no_safepoints, 0, func);
		return DoneBlockingOk;
	case STATE_BLOCKING_SUSPEND_REQUESTED:
		if (!(suspend_count > 0))
			mono_fatal_with_history ("suspend_count = %d, but should be > 0", suspend_count);
		if (no_safepoints)
			mono_fatal_with_history ("no_safepoints = TRUE, but should be FALSE");
		if (thread_state_cas (&info->thread_state, build_thread_state (STATE_BLOCKING_SELF_SUSPENDED, suspend_count, no_safepoints), raw_state) != raw_state)
			goto retry_state_change;
		trace_state_change_with_func ("DONE_BLOCKING", info, raw_state, STATE_BLOCKING_SELF_SUSPENDED, no_safepoints, 0, func);
		return DoneBlockingWait;
/*
STATE_RUNNING: //Blocking was aborted and not properly restored
STATE_ASYNC_SUSPEND_REQUESTED: //Blocking was aborted, not properly restored and now there's a pending suspend
STATE_ASYNC_SUSPENDED
STATE_SELF_SUSPENDED: Code should not be running while suspended.
STATE_BLOCKING_SELF_SUSPENDED: This an exit state of done blocking
STATE_BLOCKING_ASYNC_SUSPENDED: This is an exit state of done blocking
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
-IgnoreAndPoll: Thread was not blocking and there's a pending suspend that needs to be processed;
-Ok: Blocking state successfully aborted;
-Wait: Blocking state successfully aborted, there's a pending suspend to be processed though, wait for resume.
*/
MonoAbortBlockingResult
mono_threads_transition_abort_blocking (THREAD_INFO_TYPE* info, const char *func)
{
	int raw_state, cur_state, suspend_count;
	gboolean no_safepoints;

retry_state_change:
	UNWRAP_THREAD_STATE (raw_state, cur_state, suspend_count, no_safepoints, info);
	switch (cur_state) {
	case STATE_RUNNING: //thread already in runnable state
		/* Even though we're going to ignore this transition, still
		 * assert about no_safepoints.  Rationale: make it easier to catch
		 * cases where we would be in ASYNC_SUSPEND_REQUESTED with
		 * no_safepoints set, since those are polling points.
		 */
		if (no_safepoints) {
			/* reset the state to no safepoints and then abort. If a
			 * thread asserts somewhere because no_safepoints was set when it
			 * shouldn't have been, we would get a second assertion here while
			 * unwinding if we hadn't reset the no_safepoints flag.
			 */
			if (thread_state_cas (&info->thread_state, build_thread_state (STATE_RUNNING, suspend_count, FALSE), raw_state) != raw_state)
				goto retry_state_change;

			/* record the current transition, in order to grab a backtrace */
			trace_state_change_with_func ("ABORT_BLOCKING", info, raw_state, STATE_RUNNING, FALSE, 0, func);

			mono_fatal_with_history ("no_safepoints = TRUE, but should be FALSE in state RUNNING with ABORT_BLOCKING");
		}
		trace_state_change_sigsafe ("ABORT_BLOCKING", info, raw_state, cur_state, no_safepoints, 0, func);
		return AbortBlockingIgnore;

	case STATE_ASYNC_SUSPEND_REQUESTED: //thread is runnable and have a pending suspend
		if (no_safepoints)
			mono_fatal_with_history ("no_safepoints = TRUE, but should be FALSE in state ASYNC_SUSPEND_REQUESTED with ABORT_BLOCKING");
		trace_state_change_sigsafe ("ABORT_BLOCKING", info, raw_state, cur_state, no_safepoints, 0, func);
		return AbortBlockingIgnoreAndPoll;

	case STATE_BLOCKING:
		if (!(suspend_count == 0))
			mono_fatal_with_history ("suspend_count = %d,  but should be == 0", suspend_count);
		if (no_safepoints)
			mono_fatal_with_history ("no_safepoints = TRUE, but should be FALSE");
		if (thread_state_cas (&info->thread_state, build_thread_state (STATE_RUNNING, suspend_count, FALSE), raw_state) != raw_state)
			goto retry_state_change;
		trace_state_change_sigsafe ("ABORT_BLOCKING", info, raw_state, STATE_RUNNING, FALSE, 0, func);
		return AbortBlockingOk;
	case STATE_BLOCKING_SUSPEND_REQUESTED:
		if (!(suspend_count > 0))
			mono_fatal_with_history ("suspend_count = %d, but should be > 0", suspend_count);
		if (no_safepoints)
			mono_fatal_with_history ("no_safepoints = TRUE, but should be FALSE");
		if (thread_state_cas (&info->thread_state, build_thread_state (STATE_BLOCKING_SELF_SUSPENDED, suspend_count, FALSE), raw_state) != raw_state)
			goto retry_state_change;
		trace_state_change_with_func ("ABORT_BLOCKING", info, raw_state, STATE_BLOCKING_SELF_SUSPENDED, FALSE, 0, func);
		return AbortBlockingWait;
/*
STATE_ASYNC_SUSPENDED:
STATE_SELF_SUSPENDED: Code should not be running while suspended.
STATE_BLOCKING_SELF_SUSPENDED: This is an exit state of done blocking, can't happen here.
STATE_BLOCKING_ASYNC_SUSPENDED: This is an exit state of abort blocking, can't happen here.
*/
	default:
		mono_fatal_with_history ("Cannot transition thread %p from %s with ABORT_BLOCKING", mono_thread_info_get_tid (info), state_name (cur_state));
	}
}

/*
Set the no_safepoints flag on an executing GC Unsafe thread.
The no_safepoints bit prevents polling (hence self-suspending) and transitioning from GC Unsafe to GC Safe.
Thus the thread will not be (cooperatively) interrupted while the bit is set.

We don't allow nesting no_safepoints regions, so the flag must be initially unset.

Since a suspend initiator may at any time request that a thread should suspend,
ASYNC_SUSPEND_REQUESTED is allowed to have the no_safepoints bit set, too.
(Future: We could augment this function to return a return value that tells the
thread to poll and retry the transition since if we enter here in the
ASYNC_SUSPEND_REQUESTED state).
 */
void
mono_threads_transition_begin_no_safepoints (MonoThreadInfo *info, const char *func)
{
	int raw_state, cur_state, suspend_count;
	gboolean no_safepoints;

retry_state_change:
	UNWRAP_THREAD_STATE (raw_state, cur_state, suspend_count, no_safepoints, info);
	switch (cur_state) {
	case STATE_RUNNING:
	case STATE_ASYNC_SUSPEND_REQUESTED:
		/* Maybe revisit this.  But for now, don't allow nesting. */
		if (no_safepoints)
			mono_fatal_with_history ("no_safepoints = TRUE, but should be FALSE with BEGIN_NO_SAFEPOINTS.  Can't nest no safepointing regions");
		if (thread_state_cas (&info->thread_state, build_thread_state (cur_state, suspend_count, TRUE), raw_state) != raw_state)
			goto retry_state_change;
		trace_state_change_with_func ("BEGIN_NO_SAFEPOINTS", info, raw_state, cur_state, TRUE, 0, func);
		return;
/*
STATE_STARTING:
STATE_DETACHED:
STATE_SELF_SUSPENDED:
STATE_ASYNC_SUSPENDED:
STATE_BLOCKING:
STATE_BLOCKING_ASYNC_SUSPENDED:
STATE_BLOCKING_SELF_SUSPENDED:
STATE_BLOCKING_SUSPEND_REQUESTED:
	no_safepoints only allowed for threads that are executing and GC Unsafe.
*/
	default:
		mono_fatal_with_history ("Cannot transition thread %p from %s with BEGIN_NO_SAFEPOINTS", mono_thread_info_get_tid (info), state_name (cur_state));
	}
}

/*
Unset the no_safepoints flag on an executing GC Unsafe thread.
The no_safepoints bit prevents polling (hence self-suspending) and transitioning from GC Unsafe to GC Safe.
Thus the thread will not be (cooperatively) interrupted while the bit is set.

We don't allow nesting no_safepoints regions, so the flag must be initially set.

Since a suspend initiator may at any time request that a thread should suspend,
ASYNC_SUSPEND_REQUESTED is allowed to have the no_safepoints bit set, too.
(Future: We could augment this function to perform the transition and then
return a return value that tells the thread to poll (and safepoint) if we enter
here in the ASYNC_SUSPEND_REQUESTED state).
 */
void
mono_threads_transition_end_no_safepoints (MonoThreadInfo *info, const char *func)
{
	int raw_state, cur_state, suspend_count;
	gboolean no_safepoints;

retry_state_change:
	UNWRAP_THREAD_STATE (raw_state, cur_state, suspend_count, no_safepoints, info);
	switch (cur_state) {
	case STATE_RUNNING:
	case STATE_ASYNC_SUSPEND_REQUESTED:
		if (!no_safepoints)
			mono_fatal_with_history ("no_safepoints = FALSE, but should be TRUE with END_NO_SAFEPOINTS.  Unbalanced no safepointing region");
		if (thread_state_cas (&info->thread_state, build_thread_state (cur_state, suspend_count, FALSE), raw_state) != raw_state)
			goto retry_state_change;
		trace_state_change_with_func ("END_NO_SAFEPOINTS", info, raw_state, cur_state, FALSE, 0, func);
		return;
/*
STATE_STARTING:
STATE_DETACHED:
STATE_SELF_SUSPENDED:
STATE_ASYNC_SUSPENDED:
STATE_BLOCKING:
STATE_BLOCKING_ASYNC_SUSPENDED:
STATE_BLOCKING_SELF_SUSPENDED:
STATE_BLOCKING_SUSPEND_REQUESTED:
	no_safepoints only allowed for threads that are executing and GC Unsafe.
*/
	default:
		mono_fatal_with_history ("Cannot transition thread %p from %s with END_NO_SAFEPOINTS", mono_thread_info_get_tid (info), state_name (cur_state));
	}
}

// State checking code
/**
 * Return TRUE is the thread is in a runnable state.
*/
gboolean
mono_thread_info_is_running (MonoThreadInfo *info)
{
	switch (mono_thread_info_current_state (info)) {
	case STATE_RUNNING:
	case STATE_ASYNC_SUSPEND_REQUESTED:
	case STATE_BLOCKING_SUSPEND_REQUESTED:
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
	switch (mono_thread_info_current_state (info)) {
	case STATE_STARTING:
	case STATE_DETACHED:
		return FALSE;
	}
	return TRUE;
}

int
mono_thread_info_suspend_count (MonoThreadInfo *info)
{
	return info->thread_state.suspend_count;
}

int
mono_thread_info_current_state (MonoThreadInfo *info)
{
	return info->thread_state.state;
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
		return TRUE;
	default:
		return FALSE;
	}
}

gboolean
mono_thread_info_will_not_safepoint (MonoThreadInfo *info)
{
	return info->thread_state.no_safepoints;
}
