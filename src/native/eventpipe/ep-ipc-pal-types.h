#ifndef __EVENTPIPE_IPC_PAL_TYPES_H__
#define __EVENTPIPE_IPC_PAL_TYPES_H__

#ifdef ENABLE_PERFTRACING

#undef EP_IMPL_GETTER_SETTER
#ifdef EP_IMPL_IPC_PAL_GETTER_SETTER
#define EP_IMPL_GETTER_SETTER
#endif
#include "ep-getter-setter.h"

#include "ep-ipc-pal-types-forward.h"

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_IPC_PAL_TYPES_H__ */
