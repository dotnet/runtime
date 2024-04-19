#ifndef __DIAGNOSTICS_TYPES_H__
#define __DIAGNOSTICS_TYPES_H__

#ifdef ENABLE_PERFTRACING
#include "ep-types.h"
#include "ds-ipc-pal-types.h"

#undef DS_IMPL_GETTER_SETTER
#ifdef DS_IMPL_IPC_GETTER_SETTER
#define DS_IMPL_GETTER_SETTER
#endif
#include "ds-getter-setter.h"

/*
 * Diagnostics Structs.
 */

typedef struct _DiagnosticsAttachProfilerCommandPayload DiagnosticsAttachProfilerCommandPayload;
typedef struct _DiagnosticsStartupProfilerCommandPayload DiagnosticsStartupProfilerCommandPayload;
typedef struct _DiagnosticsConnectPort DiagnosticsConnectPort;
typedef struct _DiagnosticsEnvironmentInfoPayload DiagnosticsEnvironmentInfoPayload;
typedef struct _DiagnosticsGenerateCoreDumpCommandPayload DiagnosticsGenerateCoreDumpCommandPayload;
typedef struct _DiagnosticsGenerateCoreDumpResponsePayload DiagnosticsGenerateCoreDumpResponsePayload;
typedef struct _DiagnosticsSetEnvironmentVariablePayload DiagnosticsSetEnvironmentVariablePayload;
typedef struct _DiagnosticsGetEnvironmentVariablePayload DiagnosticsGetEnvironmentVariablePayload;
typedef struct _DiagnosticsEnablePerfmapPayload DiagnosticsEnablePerfmapPayload;
typedef struct _DiagnosticsApplyStartupHookPayload DiagnosticsApplyStartupHookPayload;
typedef struct _DiagnosticsIpcHeader DiagnosticsIpcHeader;
typedef struct _DiagnosticsIpcMessage DiagnosticsIpcMessage;
typedef struct _DiagnosticsListenPort DiagnosticsListenPort;
typedef struct _DiagnosticsPort DiagnosticsPort;
typedef struct _DiagnosticsPortBuilder DiagnosticsPortBuilder;
typedef struct _DiagnosticsPortVtable DiagnosticsPortVtable;
typedef struct _DiagnosticsProcessInfoPayload DiagnosticsProcessInfoPayload;
typedef struct _DiagnosticsProcessInfo2Payload DiagnosticsProcessInfo2Payload;
typedef struct _DiagnosticsProcessInfo3Payload DiagnosticsProcessInfo3Payload;
typedef struct _EventPipeCollectTracingCommandPayload EventPipeCollectTracingCommandPayload;
typedef struct _EventPipeStopTracingCommandPayload EventPipeStopTracingCommandPayload;

#include "ds-rt-types.h"

/*
 * Diagnostics Enums.
 */

// The Diagnostic command set is 0x01
typedef enum {
	DS_DUMP_COMMANDID_RESERVED = 0x00,
	DS_DUMP_COMMANDID_GENERATE_CORE_DUMP = 0x01,
	DS_DUMP_COMMANDID_GENERATE_CORE_DUMP2 = 0x02,
	DS_DUMP_COMMANDID_GENERATE_CORE_DUMP3 = 0x03,
	// future
} DiagnosticsDumpCommandId;

typedef enum {
	DS_IPC_MAGIC_VERSION_DOTNET_IPC_V1 = 0x01,
	// FUTURE
} DiagnosticsIpcMagicVersion;

typedef enum {
	// reserved   = 0x00,
	DS_SERVER_COMMANDSET_DUMP = 0x01,
	DS_SERVER_COMMANDSET_EVENTPIPE = 0x02,
	DS_SERVER_COMMANDSET_PROFILER = 0x03,
	DS_SERVER_COMMANDSET_PROCESS = 0x04,
	DS_SERVER_COMMANDSET_SERVER = 0xFF
} DiagnosticsServerCommandSet;

// The event pipe command set is 0x02
// see ds-ipc.h and ds-server.h for more details
typedef enum {
	DS_PROCESS_COMMANDID_GET_PROCESS_INFO = 0x00,
	DS_PROCESS_COMMANDID_RESUME_RUNTIME = 0x01,
	DS_PROCESS_COMMANDID_GET_PROCESS_ENV = 0x02,
	DS_PROCESS_COMMANDID_SET_ENV_VAR = 0x03,
	DS_PROCESS_COMMANDID_GET_PROCESS_INFO_2 = 0x04,
	DS_PROCESS_COMMANDID_ENABLE_PERFMAP = 0x05,
	DS_PROCESS_COMMANDID_DISABLE_PERFMAP = 0x06,
	DS_PROCESS_COMMANDID_APPLY_STARTUP_HOOK = 0x07,
	DS_PROCESS_COMMANDID_GET_PROCESS_INFO_3 = 0x08
	// future
} DiagnosticsProcessCommandId;

// The Diagnostic command set is 0x01
typedef enum {
	DS_PROFILER_COMMANDID_RESERVED = 0x00,
	DS_PROFILER_COMMANDID_ATTACH_PROFILER = 0x01,
	DS_PROFILER_COMMANDID_STARTUP_PROFILER = 0x02,
	// future
} DiagnosticsProfilerCommandId;

// Overlaps with DiagnosticsServerCommandId
// DON'T create overlapping values
typedef enum {
	DS_SERVER_RESPONSEID_OK = 0x00,
	// future
	DS_SERVER_RESPONSEID_ERROR = 0xFF,
} DiagnosticsServerResponseId;

// The event pipe command set is 0x02
// see ds-ipc.h and ds-server.h for more details
typedef enum {
	EP_COMMANDID_STOP_TRACING = 0x01,
	EP_COMMANDID_COLLECT_TRACING  = 0x02,
	EP_COMMANDID_COLLECT_TRACING_2 = 0x03,
	EP_COMMANDID_COLLECT_TRACING_3 = 0x04,
	// future
} EventPipeCommandId;

typedef enum {
	DS_PORT_TYPE_LISTEN = 0,
	DS_PORT_TYPE_CONNECT = 1
} DiagnosticsPortType;

typedef enum {
	DS_PORT_SUSPEND_MODE_NOSUSPEND = 0,
	DS_PORT_SUSPEND_MODE_SUSPEND = 1
} DiagnosticsPortSuspendMode;

#define DOTNET_IPC_V1_MAGIC "DOTNET_IPC_V1"
#define DOTNET_IPC_V1_ADVERTISE_MAGIC "ADVR_V1"
#define DOTNET_IPC_V1_ADVERTISE_SIZE 34

typedef int32_t ds_ipc_result_t;

#define DS_IPC_S_OK ((ds_ipc_result_t)(0L))
#define DS_IPC_E_BAD_ENCODING ((ds_ipc_result_t)(0x80131384L))
#define DS_IPC_E_UNKNOWN_COMMAND ((ds_ipc_result_t)(0x80131385L))
#define DS_IPC_E_UNKNOWN_MAGIC ((ds_ipc_result_t)(0x80131386L))
#define DS_IPC_E_NOTSUPPORTED ((ds_ipc_result_t)(0x80131515L))
#define DS_IPC_E_FAIL ((ds_ipc_result_t)(0x80004005L))
#define DS_IPC_E_NOT_YET_AVAILABLE ((ds_ipc_result_t)(0x8013135bL))
#define DS_IPC_E_RUNTIME_UNINITIALIZED ((ds_ipc_result_t)(0x80131371L))
#define DS_IPC_E_INVALIDARG ((ds_ipc_result_t)(0x80070057L))
#define DS_IPC_E_INSUFFICIENT_BUFFER ((ds_ipc_result_t)(0x8007007A))
#define DS_IPC_E_ENVVAR_NOT_FOUND ((ds_ipc_result_t)(0x800000CB))

#endif /* ENABLE_PERFTRACING */
#endif /* __DIAGNOSTICS_TYPES_H__ */
