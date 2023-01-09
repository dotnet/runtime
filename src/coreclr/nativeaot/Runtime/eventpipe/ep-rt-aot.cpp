#include <eventpipe/ep-rt-config.h>

#ifdef ENABLE_PERFTRACING
#include <eventpipe/ep-types.h>
#include <eventpipe/ep.h>
#include <eventpipe/ep-stack-contents.h>
#include <eventpipe/ep-rt.h>

ep_rt_lock_handle_t _ep_rt_aot_config_lock_handle;
CrstStatic _ep_rt_aot_config_lock;

thread_local EventPipeAotThreadHolderTLS EventPipeAotThreadHolderTLS::g_threadHolderTLS;

ep_char8_t *volatile _ep_rt_aot_diagnostics_cmd_line;

#ifndef TARGET_UNIX
uint32_t *_ep_rt_aot_proc_group_offsets;
#endif

/*
 * Forward declares of all static functions.
 */


static
void
walk_managed_stack_for_threads (
    ep_rt_thread_handle_t sampling_thread,
    EventPipeEvent *sampling_event);


bool
ep_rt_aot_walk_managed_stack_for_thread (
    ep_rt_thread_handle_t thread,
    EventPipeStackContents *stack_contents)
{
    __debugbreak();
    return false;
}

// The thread store lock must already be held by the thread before this function
// is called.  ThreadSuspend::SuspendEE acquires the thread store lock.
static
void
walk_managed_stack_for_threads (
    ep_rt_thread_handle_t sampling_thread,
    EventPipeEvent *sampling_event)
{
}

void
ep_rt_aot_sample_profiler_write_sampling_event_for_threads (
    ep_rt_thread_handle_t sampling_thread,
    EventPipeEvent *sampling_event)
{
}

#endif /* ENABLE_PERFTRACING */
