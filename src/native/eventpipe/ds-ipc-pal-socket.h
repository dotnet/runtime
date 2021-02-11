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
#define DS_IPC_INVALID_SOCKET INVALID_SOCKET
#define DS_IPC_SOCKET_ERROR SOCKET_ERROR
#define DS_IPC_SOCKET_ERROR_WOULDBLOCK WSAEWOULDBLOCK
typedef ADDRINFOA ds_ipc_addrinfo_t;
typedef SOCKET ds_ipc_socket_t;
typedef SOCKADDR ds_ipc_socket_address_t;
typedef ADDRESS_FAMILY ds_ipc_socket_family_t;
typedef WSAPOLLFD ds_ipc_pollfd_t;
typedef int ds_ipc_mode_t;
#else
#define DS_IPC_INVALID_SOCKET -1
#define DS_IPC_SOCKET_ERROR -1
#define DS_IPC_SOCKET_ERROR_WOULDBLOCK EINPROGRESS
typedef struct addrinfo ds_ipc_addrinfo_t;
typedef int ds_ipc_socket_t;
typedef struct socketaddr ds_ipc_socket_address_t;
typedef int ds_ipc_socket_family_t;
typedef struct pollfd ds_ipc_pollfd_t;
typedef mode_t ds_ipc_mode_t;
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
	size_t server_address_len;
	ds_ipc_socket_family_t server_address_family;
	ds_ipc_socket_t server_socket;
	bool is_listening;
	bool is_closed;
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
