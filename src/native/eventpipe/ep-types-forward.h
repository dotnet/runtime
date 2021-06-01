#ifndef __EVENTPIPE_TYPES_FORWARD_H__
#define __EVENTPIPE_TYPES_FORWARD_H__

#ifdef ENABLE_PERFTRACING

/*
 * EventPipe Structs.
 */

typedef struct _EventData EventData;
typedef struct _EventFilterDescriptor EventFilterDescriptor;
typedef struct _EventPipeBuffer EventPipeBuffer;
typedef struct _EventPipeBufferList EventPipeBufferList;
typedef struct _EventPipeBufferManager EventPipeBufferManager;
typedef struct _EventPipeBlock EventPipeBlock;
typedef struct _EventPipeBlockVtable EventPipeBlockVtable;
typedef struct _EventPipeConfiguration EventPipeConfiguration;
typedef struct _EventPipeEvent EventPipeEvent;
typedef struct _EventPipeEventBlockBase EventPipeEventBlockBase;
typedef struct _EventPipeEventBlock EventPipeEventBlock;
typedef struct _EventPipeEventHeader EventPipeEventHeader;
typedef struct _EventPipeEventInstance EventPipeEventInstance;
typedef struct _EventPipeEventMetadataEvent EventPipeEventMetadataEvent;
typedef struct _EventPipeEventPayload EventPipeEventPayload;
typedef struct _EventPipeEventSource EventPipeEventSource;
typedef struct _EventPipeFile EventPipeFile;
typedef struct _EventPipeJsonFile EventPipeJsonFile;
typedef struct _EventPipeMetadataBlock EventPipeMetadataBlock;
typedef struct _EventPipeParameterDesc EventPipeParameterDesc;
typedef struct _EventPipeProvider EventPipeProvider;
typedef struct _EventPipeProviderCallbackData EventPipeProviderCallbackData;
typedef struct _EventPipeProviderCallbackDataQueue EventPipeProviderCallbackDataQueue;
typedef struct _EventPipeProviderConfiguration EventPipeProviderConfiguration;
typedef struct _EventPipeSession EventPipeSession;
typedef struct _EventPipeSessionProvider EventPipeSessionProvider;
typedef struct _EventPipeSessionProviderList EventPipeSessionProviderList;
typedef struct _EventPipeSequencePoint EventPipeSequencePoint;
typedef struct _EventPipeSequencePointBlock EventPipeSequencePointBlock;
typedef struct _EventPipeStackBlock EventPipeStackBlock;
typedef struct _EventPipeStackContents EventPipeStackContents;
typedef struct _EventPipeSystemTime EventPipeSystemTime;
typedef struct _EventPipeThread EventPipeThread;
typedef struct _EventPipeThreadHolder EventPipeThreadHolder;
typedef struct _EventPipeThreadSessionState EventPipeThreadSessionState;
typedef struct _FastSerializableObject FastSerializableObject;
typedef struct _FastSerializableObjectVtable FastSerializableObjectVtable;
typedef struct _FastSerializer FastSerializer;
typedef struct _FileStream FileStream;
typedef struct _FileStreamWriter FileStreamWriter;
typedef struct _IpcStreamWriter IpcStreamWriter;
typedef struct _StackHashEntry StackHashEntry;
typedef struct _StackHashKey StackHashKey;
typedef struct _StreamWriter StreamWriter;
typedef struct _StreamWriterVtable StreamWriterVtable;

#define EP_MAX_NUMBER_OF_SESSIONS 64

#define EP_GUID_SIZE 16

#define EP_ACTIVITY_ID_SIZE EP_GUID_SIZE

#define EP_MAX_STACK_DEPTH 100

/*
 * EventPipe Enums.
 */

typedef enum {
	EP_BUFFER_STATE_WRITABLE = 0,
	EP_BUFFER_STATE_READ_ONLY = 1
} EventPipeBufferState;

typedef enum {
	EP_EVENT_LEVEL_LOGALWAYS,
	EP_EVENT_LEVEL_CRITICAL,
	EP_EVENT_LEVEL_ERROR,
	EP_EVENT_LEVEL_WARNING,
	EP_EVENT_LEVEL_INFORMATIONAL,
	EP_EVENT_LEVEL_VERBOSE
} EventPipeEventLevel;

typedef enum {
	EP_FILE_FLUSH_FLAGS_EVENT_BLOCK = 1,
	EP_FILE_FLUSH_FLAGS_METADATA_BLOCK = 2,
	EP_FILE_FLUSH_FLAGS_STACK_BLOCK = 4,
	EP_FILE_FLUSH_FLAGS_ALL_BLOCKS = EP_FILE_FLUSH_FLAGS_EVENT_BLOCK | EP_FILE_FLUSH_FLAGS_METADATA_BLOCK | EP_FILE_FLUSH_FLAGS_STACK_BLOCK
} EventPipeFileFlushFlags;

// Represents the type of an event parameter.
// This enum is derived from the managed TypeCode type, though
// not all of these values are available in TypeCode.
// For example, Guid does not exist in TypeCode.
// Keep this in sync with COR_PRF_EVENTPIPE_PARAM_TYPE defined in
// corprof.idl
typedef enum {
	EP_PARAMETER_TYPE_EMPTY = 0,		// Null reference
	EP_PARAMETER_TYPE_OBJECT = 1,		// Instance that isn't a value
	EP_PARAMETER_TYPE_DB_NULL = 2,		// Database null value
	EP_PARAMETER_TYPE_BOOLEAN = 3,		// Boolean
	EP_PARAMETER_TYPE_CHAR = 4,		// Unicode character
	EP_PARAMETER_TYPE_SBYTE = 5,		// Signed 8-bit integer
	EP_PARAMETER_TYPE_BYTE = 6,		// Unsigned 8-bit integer
	EP_PARAMETER_TYPE_INT16 = 7,		// Signed 16-bit integer
	EP_PARAMETER_TYPE_UINT16 = 8,		// Unsigned 16-bit integer
	EP_PARAMETER_TYPE_INT32 = 9,		// Signed 32-bit integer
	EP_PARAMETER_TYPE_UINT32 = 10,		// Unsigned 32-bit integer
	EP_PARAMETER_TYPE_INT64 = 11,		// Signed 64-bit integer
	EP_PARAMETER_TYPE_UINT64 = 12,		// Unsigned 64-bit integer
	EP_PARAMETER_TYPE_SINGLE = 13,		// IEEE 32-bit float
	EP_PARAMETER_TYPE_DOUBLE = 14,		// IEEE 64-bit double
	EP_PARAMETER_TYPE_DECIMAL = 15,		// Decimal
	EP_PARAMETER_TYPE_DATE_TIME = 16,	// DateTime
	EP_PARAMETER_TYPE_GUID = 17,		// Guid
	EP_PARAMETER_TYPE_STRING = 18,		// Unicode character string
	EP_PARAMETER_TYPE_ARRAY = 19		// Indicates the type is an arbitrary sized array
} EventPipeParameterType;

typedef enum {
	EP_METADATA_TAG_OPCODE = 1,
	EP_METADATA_TAG_PARAMETER_PAYLOAD = 2
} EventPipeMetadataTag;

typedef enum {
	EP_SAMPLE_PROFILER_SAMPLE_TYPE_ERROR = 0,
	EP_SAMPLE_PROFILER_SAMPLE_TYPE_EXTERNAL = 1,
	EP_SAMPLE_PROFILER_SAMPLE_TYPE_MANAGED = 2
} EventPipeSampleProfilerSampleType;

typedef enum {
	// Default format used in .Net Core 2.0-3.0 Preview 6
	// TBD - it may remain the default format .Net Core 3.0 when
	// used with private EventPipe managed API via reflection.
	// This format had limited official exposure in documented
	// end-user RTM scenarios, but it is supported by PerfView,
	// TraceEvent, and was used by AI profiler.
	EP_SERIALIZATION_FORMAT_NETPERF_V3,
	// Default format we plan to use in .Net Core 3 Preview7+
	// for most if not all scenarios.
	EP_SERIALIZATION_FORMAT_NETTRACE_V4,
	EP_SERIALIZATION_FORMAT_COUNT
} EventPipeSerializationFormat;

typedef enum {
	EP_SESSION_TYPE_FILE,
	EP_SESSION_TYPE_LISTENER,
	EP_SESSION_TYPE_IPCSTREAM,
	EP_SESSION_TYPE_SYNCHRONOUS
} EventPipeSessionType ;

typedef enum {
	EP_STATE_NOT_INITIALIZED,
	EP_STATE_INITIALIZED,
	EP_STATE_SHUTTING_DOWN
} EventPipeState;

typedef enum {
	EP_THREAD_TYPE_SERVER,
	EP_THREAD_TYPE_SESSION,
	EP_THREAD_TYPE_SAMPLING
} EventPipeThreadType;

/*
 * EventPipe Basic Types.
 */

typedef intptr_t EventPipeWaitHandle;
typedef uint64_t EventPipeSessionID;
typedef unsigned short ep_char16_t;
typedef int64_t ep_timestamp_t;
typedef int64_t ep_system_timestamp_t;

/*
 * EventPipe Callbacks.
 */

// Define the event pipe callback to match the ETW callback signature.
typedef void (*EventPipeCallback)(
	const uint8_t *source_id,
	unsigned long is_enabled,
	uint8_t level,
	uint64_t match_any_keywords,
	uint64_t match_all_keywords,
	EventFilterDescriptor *filter_data,
	void *callback_data);

typedef void (*EventPipeCallbackDataFree)(
	EventPipeCallback callback,
	void *callback_data);

typedef void (*EventPipeSessionSynchronousCallback)(
	EventPipeProvider *provider,
	uint32_t event_id,
	uint32_t event_version,
	uint32_t metadata_blob_len,
	const uint8_t *metadata_blob,
	uint32_t event_data_len,
	const uint8_t *event_data,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id,
	/*ep_rt_thread_handle_t*/ void *event_thread,
	uint32_t stack_frames_len,
	uintptr_t *stack_frames);

typedef bool (*EventPipeIpcStreamFactorySuspendedPortsCallback)(void);

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_TYPES_FORWARD_H__ */
