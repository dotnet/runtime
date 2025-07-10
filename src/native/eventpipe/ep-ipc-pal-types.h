#ifndef __EVENTPIPE_IPC_PAL_TYPES_H__
#define __EVENTPIPE_IPC_PAL_TYPES_H__

#ifdef ENABLE_PERFTRACING

#undef EP_IMPL_GETTER_SETTER
#ifdef EP_IMPL_IPC_PAL_GETTER_SETTER
#define EP_IMPL_GETTER_SETTER
#endif
#include "ep-getter-setter.h"

#include "ep-ipc-pal-types-forward.h"

/*
 * Shared Diagnostics/EventPipe IPC PAL Enums.
 */

 typedef enum {
	DS_IPC_POLL_EVENTS_NONE = 0x00, // no events
	DS_IPC_POLL_EVENTS_SIGNALED = 0x01, // ready for use
	DS_IPC_POLL_EVENTS_HANGUP = 0x02, // connection remotely closed
	DS_IPC_POLL_EVENTS_ERR = 0x04, // error
	DS_IPC_POLL_EVENTS_UNKNOWN = 0x80 // unknown state
} DiagnosticsIpcPollEvents;

#define DS_IPC_TIMEOUT_INFINITE (uint32_t)-1

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_IPC_PAL_TYPES_H__ */
