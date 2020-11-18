#ifndef __DIAGNOSTICS_IPC_WIN32_H__
#define __DIAGNOSTICS_IPC_WIN32_H__

#include <config.h>

#ifdef ENABLE_PERFTRACING
#ifdef HOST_WIN32
#include "ds-rt-config.h"
#include "ds-types.h"
#include "ds-ipc.h"
#include "ep-stream.h"

#undef DS_IMPL_GETTER_SETTER
#ifdef DS_IMPL_IPC_WIN32_GETTER_SETTER
#define DS_IMPL_GETTER_SETTER
#endif
#include "ds-getter-setter.h"

#include <Windows.h>

/*
 * DiagnosticsIpc.
 */

#define DS_IPC_WIN32_MAX_NAMED_PIPE_LEN 256
#define DS_IPC_WIN32_INFINITE_TIMEOUT INFINITE

#if defined(DS_INLINE_GETTER_SETTER) || defined(DS_IMPL_IPC_WIN32_GETTER_SETTER)
struct _DiagnosticsIpc {
#else
struct _DiagnosticsIpc_Internal {
#endif
	ep_char8_t pipe_name [DS_IPC_WIN32_MAX_NAMED_PIPE_LEN];
	OVERLAPPED overlap;
	HANDLE pipe;
	bool is_listening;
	DiagnosticsIpcConnectionMode mode;
};

#if !defined(DS_INLINE_GETTER_SETTER) && !defined(DS_IMPL_IPC_WIN32_GETTER_SETTER)
struct _DiagnosticsIpc {
	uint8_t _internal [sizeof (struct _DiagnosticsIpc_Internal)];
};
#endif

/*
 * DiagnosticsIpcStream.
 */

#if defined(DS_INLINE_GETTER_SETTER) || defined(DS_IMPL_IPC_WIN32_GETTER_SETTER)
struct _DiagnosticsIpcStream {
#else
struct _DiagnosticsIpcStream_Internal {
#endif
	IpcStream stream;
	OVERLAPPED overlap;
	HANDLE pipe;
	bool is_test_reading;
	DiagnosticsIpcConnectionMode mode;
};

#if !defined(DS_INLINE_GETTER_SETTER) && !defined(DS_IMPL_IPC_WIN32_GETTER_SETTER)
struct _DiagnosticsIpcStream {
	uint8_t _internal [sizeof (struct _DiagnosticsIpcStream_Internal)];
};
#endif

#endif /* HOST_WIN32 */
#endif /* ENABLE_PERFTRACING */
#endif /* __DIAGNOSTICS_IPC_WIN32_H__ */
