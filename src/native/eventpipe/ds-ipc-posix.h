#ifndef __DIAGNOSTICS_IPC_POSIX_H__
#define __DIAGNOSTICS_IPC_POSIX_H__

#include "ds-rt-config.h"

#ifdef ENABLE_PERFTRACING
#ifndef HOST_WIN32
#include "ds-ipc-pal.h"

#undef DS_IMPL_GETTER_SETTER
#ifdef DS_IMPL_IPC_POSIX_GETTER_SETTER
#define DS_IMPL_GETTER_SETTER
#endif
#include "ds-getter-setter.h"

/*
 * DiagnosticsIpc.
 */

#if defined(DS_INLINE_GETTER_SETTER) || defined(DS_IMPL_IPC_POSIX_GETTER_SETTER)
struct _DiagnosticsIpc {
#else
struct _DiagnosticsIpc_Internal {
#endif
	struct sockaddr_un *server_address;
	int server_socket;
	bool is_listening;
	bool is_closed;
	DiagnosticsIpcConnectionMode mode;
};

#if !defined(DS_INLINE_GETTER_SETTER) && !defined(DS_IMPL_IPC_POSIX_GETTER_SETTER)
struct _DiagnosticsIpc {
	uint8_t _internal [sizeof (struct _DiagnosticsIpc_Internal)];
};
#endif

/*
 * DiagnosticsIpcStream.
 */

#if defined(DS_INLINE_GETTER_SETTER) || defined(DS_IMPL_IPC_POSIX_GETTER_SETTER)
struct _DiagnosticsIpcStream {
#else
struct _DiagnosticsIpcStream_Internal {
#endif
	IpcStream stream;
	int client_socket;
	DiagnosticsIpcConnectionMode mode;
};

#if !defined(DS_INLINE_GETTER_SETTER) && !defined(DS_IMPL_IPC_POSIX_GETTER_SETTER)
struct _DiagnosticsIpcStream {
	uint8_t _internal [sizeof (struct _DiagnosticsIpcStream_Internal)];
};
#endif

#endif /* !HOST_WIN32 */
#endif /* ENABLE_PERFTRACING */
#endif /* __DIAGNOSTICS_IPC_POSIX_H__ */
