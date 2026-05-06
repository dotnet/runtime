#ifndef __EVENTPIPE_SAMPLE_PROFILER_H__
#define __EVENTPIPE_SAMPLE_PROFILER_H__

#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#include "ep-types.h"

#undef EP_IMPL_GETTER_SETTER
#ifdef EP_IMPL_SAMPLE_PROFILER_GETTER_SETTER
#define EP_IMPL_GETTER_SETTER
#endif
#include "ep-getter-setter.h"

/*
 * EventPipeSampleProfiler.
 */

void
ep_sample_profiler_init (EventPipeProviderCallbackDataQueue *provider_callback_data_queue);

void
ep_sample_profiler_shutdown (void);

void
ep_sample_profiler_enable (void);

void
ep_sample_profiler_disable (void);

void
ep_sample_profiler_can_start_sampling (void);

void
ep_sample_profiler_set_sampling_rate (uint64_t nanoseconds);

uint64_t
ep_sample_profiler_get_sampling_rate (void);

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_SAMPLE_PROFILER_H__ */
