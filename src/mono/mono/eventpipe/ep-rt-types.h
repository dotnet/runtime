#ifndef __EVENTPIPE_RT_TYPES_H__
#define __EVENTPIPE_RT_TYPES_H__

#include <config.h>

#ifdef ENABLE_PERFTRACING

#define EP_ASSERT(expr) ep_rt_redefine
#define EP_LIKELY(expr) ep_rt_redefine
#define EP_UNLIKELY(expr) ep_rt_redefine

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

#include "ep-rt-types-mono.h"

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_RT_TYPES_H__ */
