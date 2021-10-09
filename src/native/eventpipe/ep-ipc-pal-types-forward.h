#ifndef __EVENTPIPE_IPC_PAL_TYPES_FORWARD_H__
#define __EVENTPIPE_IPC_PAL_TYPES_FORWARD_H__

#ifdef ENABLE_PERFTRACING

#include <stdlib.h>
#include <stdint.h>
#ifndef __cplusplus
#include <stdbool.h>
#endif  // __cplusplus

typedef char ep_char8_t;

/*
 * IPC Stream Structs.
 */

typedef struct _IpcStream IpcStream;
typedef struct _IpcStreamVtable IpcStreamVtable;

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_IPC_PAL_TYPES_FORWARD_H__ */
