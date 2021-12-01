#ifndef __DIAGNOSTICS_IPC_PAL_SOCKET_H__
#define __DIAGNOSTICS_IPC_PAL_SOCKET_H__

#include "ds-rt-config.h"

#ifdef ENABLE_PERFTRACING
#include "ds-ipc-pal.h"

#undef DS_IMPL_GETTER_SETTER
#ifdef DS_IMPL_IPC_PAL_SOCKET_GETTER_SETTER
#define DS_IMPL_GETTER_SETTER
#endif
#include "ds-getter-setter.h"

#ifdef HOST_WIN32
#include <winsock2.h>
typedef SOCKET ds_ipc_socket_t;
typedef SOCKADDR ds_ipc_socket_address_t;
typedef ADDRESS_FAMILY ds_ipc_socket_family_t;
typedef int ds_ipc_socket_len_t;
#else
#include <sys/socket.h>
typedef int ds_ipc_socket_t;
typedef struct sockaddr ds_ipc_socket_address_t;
typedef int ds_ipc_socket_family_t;
typedef socklen_t ds_ipc_socket_len_t;
#endif

/*
 * DiagnosticsIpc.
 */

#if defined(DS_INLINE_GETTER_SETTER) || defined(DS_IMPL_IPC_PAL_SOCKET_GETTER_SETTER)
struct _DiagnosticsIpc {
#else
struct _DiagnosticsIpc_Internal {
#endif
	ds_ipc_socket_address_t *server_address;
	ds_ipc_socket_len_t server_address_len;
	ds_ipc_socket_family_t server_address_family;
	ds_ipc_socket_t server_socket;
	bool is_listening;
	bool is_closed;
	bool is_dual_mode;
	DiagnosticsIpcConnectionMode mode;
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
	ds_ipc_socket_t client_socket;
	DiagnosticsIpcConnectionMode mode;
};

#if !defined(DS_INLINE_GETTER_SETTER) && !defined(DS_IMPL_IPC_PAL_SOCKET_GETTER_SETTER)
struct _DiagnosticsIpcStream {
	uint8_t _internal [sizeof (struct _DiagnosticsIpcStream_Internal)];
};
#endif

#endif /* ENABLE_PERFTRACING */
#endif /* __DIAGNOSTICS_IPC_PAL_SOCKET_H__ */
