#ifndef _WAPI_THREADS_H_
#define _WAPI_THREADS_H_

#include <glib.h>

#include "mono/io-layer/handles.h"
#include "mono/io-layer/io.h"
#include "mono/io-layer/status.h"

#define TLS_MINIMUM_AVAILABLE 64
#define TLS_OUT_OF_INDEXES 0xFFFFFFFF

#define STILL_ACTIVE STATUS_PENDING

#define DEBUG_PROCESS 0x00000001
#define DEBUG_ONLY_THIS_PROCESS 0x00000002
#define CREATE_SUSPENDED 0x00000004
#define DETACHED_PROCESS 0x00000008
#define CREATE_NEW_CONSOLE 0x00000010
#define NORMAL_PRIORITY_CLASS 0x00000020
#define IDLE_PRIORITY_CLASS 0x00000040
#define HIGH_PRIORITY_CLASS 0x00000080
#define REALTIME_PRIORITY_CLASS 0x00000100
#define CREATE_NEW_PROCESS_GROUP 0x00000200
#define CREATE_UNICODE_ENVIRONMENT 0x00000400
#define CREATE_SEPARATE_WOW_VDM 0x00000800
#define CREATE_SHARED_WOW_VDM 0x00001000
#define CREATE_FORCEDOS 0x00002000
#define BELOW_NORMAL_PRIORITY_CLASS 0x00004000
#define ABOVE_NORMAL_PRIORITY_CLASS 0x00008000
#define CREATE_BREAKAWAY_FROM_JOB 0x01000000
#define CREATE_WITH_USERPROFILE 0x02000000
#define CREATE_DEFAULT_ERROR_MODE 0x04000000
#define CREATE_NO_WINDOW 0x08000000


typedef guint32 (*WapiThreadStart)(gpointer);

extern WapiHandle *CreateThread(WapiSecurityAttributes *security, guint32 stacksize, WapiThreadStart start, gpointer param, guint32 create, guint32 *tid);
extern void ExitThread(guint32 exitcode) G_GNUC_NORETURN;
extern gboolean GetExitCodeThread(WapiHandle *handle, guint32 *exitcode);
extern guint32 GetCurrentThreadId(void);
extern WapiHandle *GetCurrentThread(void);
extern guint32 ResumeThread(WapiHandle *handle);
extern guint32 SuspendThread(WapiHandle *handle);
extern guint32 TlsAlloc(void);
extern gboolean TlsFree(guint32 idx);
extern gpointer TlsGetValue(guint32 idx);
extern gboolean TlsSetValue(guint32 idx, gpointer value);
extern void Sleep(guint32 ms);

#endif /* _WAPI_THREADS_H_ */
