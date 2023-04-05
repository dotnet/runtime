#ifndef __EVENTPIPE_RT_TYPES_H__
#define __EVENTPIPE_RT_TYPES_H__

#ifdef ENABLE_PERFTRACING

#define EP_ASSERT(expr) ep_rt_redefine
#define EP_UNREACHABLE(msg) ep_rt_redefine
#define EP_LIKELY(expr) ep_rt_redefine
#define EP_UNLIKELY(expr) ep_rt_redefine

/*
 * EventPipe.
 */

#define ep_rt_method_desc_t ep_rt_redefine

/*
 * PAL.
 */

#define ep_rt_file_handle_t ep_rt_redefine

#define ep_rt_wait_event_handle_t ep_rt_redefine

#define ep_rt_lock_handle_t ep_rt_redefine

#define ep_rt_spin_lock_handle_t ep_rt_redefine

/*
 * Thread.
 */

#define ep_rt_thread_handle_t ep_rt_redefine;

#define ep_rt_thread_activity_id_handle_t ep_rt_redefine;

#define ep_rt_thread_id_t ep_rt_redefine;

#define ep_rt_thread_start_func ep_rt_redefine;

#define ep_rt_thread_start_func_return_t ep_rt_redefine

#define ep_rt_thread_params_t ep_rt_redefine

/*
 * ErrorHandling.
 */

#define ep_raise_error_if_nok(expr) do { if (EP_UNLIKELY(!(expr))) goto ep_on_error; } while (0)
#define ep_raise_error() do { goto ep_on_error; } while (0)
#define ep_exit_error_handler() do { goto ep_on_exit; } while (0)
#define ep_return_null_if_nok(expr) do { if (EP_UNLIKELY(!(expr))) return NULL; } while (0)
#define ep_return_void_if_nok(expr) do { if (EP_UNLIKELY(!(expr))) return; } while (0)
#define ep_return_false_if_nok(expr) do { if (EP_UNLIKELY(!(expr))) return false; } while (0)
#define ep_return_zero_if_nok(expr) do { if (EP_UNLIKELY(!(expr))) return 0; } while (0)

#ifndef EP_NO_RT_DEPENDENCY
#include EP_RT_TYPES_H
#endif

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_RT_TYPES_H__ */
