/*
 * daemon-messages.h:  Communications to and from the handle daemon
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_DAEMON_MESSAGES_H_
#define _WAPI_DAEMON_MESSAGES_H_

#include <mono/io-layer/wapi-private.h>

typedef enum {
	WapiHandleRequestType_Error,
	WapiHandleRequestType_New,
	WapiHandleRequestType_Open,
	WapiHandleRequestType_Close,
	WapiHandleRequestType_Scratch,
	WapiHandleRequestType_ScratchFree,
	WapiHandleRequestType_ProcessFork,
} WapiHandleRequestType;

typedef struct 
{
	WapiHandleType type;
} WapiHandleRequest_New;

typedef struct 
{
	guint32 handle;
} WapiHandleRequest_Open;

typedef struct 
{
	guint32 handle;
} WapiHandleRequest_Close;

typedef struct
{
	guint32 length;
} WapiHandleRequest_Scratch;

typedef struct
{
	guint32 idx;
} WapiHandleRequest_ScratchFree;

typedef struct 
{
	guint32 cmd;
	guint32 args;
	guint32 env;
	guint32 dir;
	guint32 stdin_handle;
	guint32 stdout_handle;
	guint32 stderr_handle;
	gboolean inherit;
	guint32 flags;
} WapiHandleRequest_ProcessFork;

typedef struct 
{
	WapiHandleRequestType type;
	union 
	{
		WapiHandleRequest_New new;
		WapiHandleRequest_Open open;
		WapiHandleRequest_Close close;
		WapiHandleRequest_Scratch scratch;
		WapiHandleRequest_ScratchFree scratch_free;
		WapiHandleRequest_ProcessFork process_fork;
	} u;
} WapiHandleRequest;

typedef enum {
	WapiHandleResponseType_Error,
	WapiHandleResponseType_New,
	WapiHandleResponseType_Open,
	WapiHandleResponseType_Close,
	WapiHandleResponseType_Scratch,
	WapiHandleResponseType_ScratchFree,
	WapiHandleResponseType_ProcessFork,
} WapiHandleResponseType;

typedef struct 
{
	guint32 reason;
} WapiHandleResponse_Error;

typedef struct 
{
	WapiHandleType type;
	guint32 handle;
} WapiHandleResponse_New;

typedef struct 
{
	WapiHandleType type;
	guint32 handle;
} WapiHandleResponse_Open;

typedef struct 
{
	gboolean destroy;
} WapiHandleResponse_Close;

typedef struct
{
	guint32 idx;
} WapiHandleResponse_Scratch;

typedef struct
{
	guint32 dummy;
} WapiHandleResponse_ScratchFree;

typedef struct
{
	guint32 process_handle;
	guint32 thread_handle;
	guint32 pid;
	guint32 tid;
} WapiHandleResponse_ProcessFork;

typedef struct
{
	WapiHandleResponseType type;
	union
	{
		WapiHandleResponse_Error error;
		WapiHandleResponse_New new;
		WapiHandleResponse_Open open;
		WapiHandleResponse_Close close;
		WapiHandleResponse_Scratch scratch;
		WapiHandleResponse_ScratchFree scratch_free;
		WapiHandleResponse_ProcessFork process_fork;
	} u;
} WapiHandleResponse;

extern void _wapi_daemon_request_response (int fd, WapiHandleRequest *req,
					   WapiHandleResponse *resp);
extern void _wapi_daemon_request_response_with_fds (int fd,
						    WapiHandleRequest *req,
						    WapiHandleResponse *resp,
						    int in_fd, int out_fd,
						    int err_fd);
extern void _wapi_daemon_request (int fd, WapiHandleRequest *req, int *fds,
				  gboolean *has_fds);
extern void _wapi_daemon_response (int fd, WapiHandleResponse *resp);

#endif /* _WAPI_DAEMON_MESSAGES_H_ */
