#ifndef __DIAGNOSTICS_IPC_PAL_WEB_SOCKET_H__
#define __DIAGNOSTICS_IPC_PAL_WEB_SOCKET_H__

#include "ds-rt-config.h"

#ifdef ENABLE_PERFTRACING
#include "ds-ipc-pal.h"

#undef DS_IMPL_GETTER_SETTER
#ifdef DS_IMPL_IPC_PAL_SOCKET_GETTER_SETTER
#define DS_IMPL_GETTER_SETTER
#endif
#include "ds-getter-setter.h"

typedef int ds_ipc_websocket_t;

/*
 * DiagnosticsIpc.
 */

#if defined(DS_INLINE_GETTER_SETTER) || defined(DS_IMPL_IPC_PAL_SOCKET_GETTER_SETTER)
struct _DiagnosticsIpc {
#else
struct _DiagnosticsIpc_Internal {
#endif
	ep_char8_t *server_url;
	ds_ipc_websocket_t server_socket;
	bool is_closed;
};

#if !defined(DS_INLINE_GETTER_SETTER) && !defined(DS_IMPL_IPC_PAL_SOCKET_GETTER_SETTER)
struct _DiagnosticsIpc {
	uint8_t _internal [sizeof (struct _DiagnosticsIpc_Internal)];
};
#endif

/*
 * DiagnosticsIpcStream.
 */

#if defined(DS_INLINE_GETTER_SETTER) || defined(DS_IMPL_IPC_PAL_SOCKET_GETTER_SETTER)
struct _DiagnosticsIpcStream {
#else
struct _DiagnosticsIpcStream_Internal {
#endif
	IpcStream stream;
	ds_ipc_websocket_t client_socket;
};

#if !defined(DS_INLINE_GETTER_SETTER) && !defined(DS_IMPL_IPC_PAL_SOCKET_GETTER_SETTER)
struct _DiagnosticsIpcStream {
	uint8_t _internal [sizeof (struct _DiagnosticsIpcStream_Internal)];
};
#endif

extern int ds_rt_websocket_poll (int client_socket);
extern int ds_rt_websocket_create (const char* url);
extern int ds_rt_websocket_recv (int client_socket, const uint8_t* buffer, uint32_t bytes_to_read);
extern int ds_rt_websocket_send (int client_socket, const uint8_t* buffer, uint32_t bytes_to_write);
extern int ds_rt_websocket_close(int client_socket);

#endif /* ENABLE_PERFTRACING */
#endif /* __DIAGNOSTICS_IPC_PAL_WEB_SOCKET_H__ */
