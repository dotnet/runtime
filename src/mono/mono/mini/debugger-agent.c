/**
 * \file
 * Soft Debugger back-end module
 *
 * Author:
 *   Zoltan Varga (vargaz@gmail.com)
 *
 * Copyright 2009-2010 Novell, Inc.
 * Copyright 2011 Xamarin Inc.
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#ifdef HAVE_SYS_TYPES_H
#include <sys/types.h>
#endif
#ifdef HAVE_SYS_SELECT_H
#include <sys/select.h>
#endif
#ifdef HAVE_SYS_SOCKET_H
#include <sys/socket.h>
#endif
#ifdef HAVE_NETINET_TCP_H
#include <netinet/tcp.h>
#endif
#ifdef HAVE_NETINET_IN_H
#include <netinet/in.h>
#endif
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#include <errno.h>
#include <glib.h>

#ifdef HAVE_PTHREAD_H
#include <pthread.h>
#endif

#ifdef HOST_WIN32
#ifdef _MSC_VER
#include <winsock2.h>
#include <process.h>
#endif
#include <ws2tcpip.h>
#include <windows.h>
#endif

#ifdef HOST_ANDROID
#include <linux/in.h>
#include <linux/tcp.h>
#include <sys/endian.h>
#endif

#include <mono/metadata/mono-debug.h>
#include <mono/metadata/debug-internals.h>
#include <mono/metadata/domain-internals.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/environment.h>
#include <mono/metadata/mono-hash-internals.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/threadpool.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/assembly-internals.h>
#include <mono/metadata/runtime.h>
#include <mono/metadata/verify-internals.h>
#include <mono/metadata/reflection-internals.h>
#include <mono/metadata/w32socket.h>
#include <mono/utils/mono-coop-mutex.h>
#include <mono/utils/mono-coop-semaphore.h>
#include <mono/utils/mono-error-internals.h>
#include <mono/utils/mono-stack-unwinding.h>
#include <mono/utils/mono-time.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/networking.h>
#include <mono/utils/mono-proclib.h>
#include <mono/utils/w32api.h>
#include <mono/utils/mono-logger-internals.h>
#include "debugger-state-machine.h"
#include "debugger-agent.h"
#include "mini.h"
#include "seq-points.h"
#include "aot-runtime.h"
#include "mini-runtime.h"
#include "interp/interp.h"
#include "debugger-engine.h"
#include "mono/metadata/debug-mono-ppdb.h"
#include "mono/metadata/custom-attrs-internals.h"

/*
 * On iOS we can't use System.Environment.Exit () as it will do the wrong
 * shutdown sequence.
*/
#if !defined (TARGET_IOS)
#define TRY_MANAGED_SYSTEM_ENVIRONMENT_EXIT
#endif

#if DISABLE_SOCKETS
#define DISABLE_SOCKET_TRANSPORT
#endif

#ifndef DISABLE_SDB

#include <mono/utils/mono-os-mutex.h>

#include <fcntl.h>
#include <sys/stat.h>

#ifndef S_IWUSR
	#define S_IWUSR S_IWRITE
#endif

#define THREAD_TO_INTERNAL(thread) (thread)->internal_thread

#if _MSC_VER
#pragma warning(disable:4312) // FIXME pointer cast to different size
#endif

typedef struct {
	gboolean enabled;
	char *transport;
	char *address;
	int log_level;
	char *log_file;
	gboolean suspend;
	gboolean server;
	gboolean onuncaught;
	GSList *onthrow;
	int timeout;
	char *launch;
	gboolean embedding;
	gboolean defer;
	int keepalive;
	gboolean setpgid;
} AgentConfig;

typedef struct
{
	//Must be the first field to ensure pointer equivalence
	DbgEngineStackFrame de;
	int id;
	guint32 il_offset;
	/*
	 * If method is gshared, this is the actual instance, otherwise this is equal to
	 * method.
	 */
	MonoMethod *actual_method;
	/*
	 * This is the method which is visible to debugger clients. Same as method,
	 * except for native-to-managed wrappers.
	 */
	MonoMethod *api_method;
	MonoContext ctx;
	MonoDebugMethodJitInfo *jit;
	MonoInterpFrameHandle interp_frame;
	gpointer frame_addr;
	int flags;
	host_mgreg_t *reg_locations [MONO_MAX_IREGS];
	/*
	 * Whenever ctx is set. This is FALSE for the last frame of running threads, since
	 * the frame can become invalid.
	 */
	gboolean has_ctx;
} StackFrame;

typedef struct _InvokeData InvokeData;

struct _InvokeData
{
	int id;
	int flags;
	guint8 *p;
	guint8 *endp;
	/* This is the context which needs to be restored after the invoke */
	MonoContext ctx;
	gboolean has_ctx;
	/*
	 * If this is set, invoke this method with the arguments given by ARGS.
	 */
	MonoMethod *method;
	gpointer *args;
	guint32 suspend_count;
	int nmethods;

	InvokeData *last_invoke;
};

struct _DebuggerTlsData {
	MonoThreadUnwindState context;

	/* This is computed on demand when it is requested using the wire protocol */
	/* It is freed up when the thread is resumed */
	int frame_count;
	StackFrame **frames;
	/* 
	 * Whenever the frame info is up-to-date. If not, compute_frame_info () will need to
	 * re-compute it.
	 */
	gboolean frames_up_to_date;
	/* 
	 * Points to data about a pending invoke which needs to be executed after the thread
	 * resumes.
	 */
	InvokeData *pending_invoke;
	/*
	 * Set to TRUE if this thread is suspended in suspend_current () or it is executing
	 * native code.
	 */
	gboolean suspended;
	/*
	 * Signals whenever the thread is in the process of suspending, i.e. it will suspend
	 * within a finite amount of time.
	 */
	gboolean suspending;
	/*
	 * Set to TRUE if this thread is suspended in suspend_current ().
	 */
	gboolean really_suspended;
	/* Used to pass the context to the breakpoint/single step handler */
	MonoContext handler_ctx;
	/* Whenever thread_stop () was called for this thread */
	gboolean terminated;

	/* Whenever to disable breakpoints (used during invokes) */
	gboolean disable_breakpoints;

	/*
	 * Number of times this thread has been resumed using resume_thread ().
	 */
	guint32 resume_count;
	guint32 resume_count_internal;
	guint32 suspend_count;

	MonoInternalThread *thread;
	intptr_t thread_id;

	/*
	 * Information about the frame which transitioned to native code for running
	 * threads.
	 */
	StackFrameInfo async_last_frame;

	/*
	 * The context where the stack walk can be started for running threads.
	 */
	MonoThreadUnwindState async_state;

	/*
     * The context used for filter clauses
     */
	MonoThreadUnwindState filter_state;

	gboolean abort_requested;

	/*
	 * The current mono_runtime_invoke_checked invocation.
	 */
	InvokeData *invoke;

	StackFrameInfo catch_frame;
	gboolean has_catch_frame;

	/*
	 * The context which needs to be restored after handling a single step/breakpoint
	 * event. This is the same as the ctx at step/breakpoint site, but includes changes
	 * to caller saved registers done by set_var ().
	 */
	MonoThreadUnwindState restore_state;
	/* Frames computed from restore_state */
	int restore_frame_count;
	StackFrame **restore_frames;

	/* The currently unloading appdomain */
	MonoDomain *domain_unloading;

	// The state that the debugger expects the thread to be in
	MonoDebuggerThreadState thread_state;
	MonoStopwatch step_time;

	gboolean gc_finalizing;
};

typedef struct {
	const char *name;
	void (*connect) (const char *address);
	void (*close1) (void);
	void (*close2) (void);
	gboolean (*send) (void *buf, int len);
	int (*recv) (void *buf, int len);
} DebuggerTransport;

/* 
 * Wire Protocol definitions
 */

#define HEADER_LENGTH 11

#define MAJOR_VERSION 2
#define MINOR_VERSION 57

typedef enum {
	CMD_SET_VM = 1,
	CMD_SET_OBJECT_REF = 9,
	CMD_SET_STRING_REF = 10,
	CMD_SET_THREAD = 11,
	CMD_SET_ARRAY_REF = 13,
	CMD_SET_EVENT_REQUEST = 15,
	CMD_SET_STACK_FRAME = 16,
	CMD_SET_APPDOMAIN = 20,
	CMD_SET_ASSEMBLY = 21,
	CMD_SET_METHOD = 22,
	CMD_SET_TYPE = 23,
	CMD_SET_MODULE = 24,
	CMD_SET_FIELD = 25,
	CMD_SET_EVENT = 64,
	CMD_SET_POINTER = 65
} CommandSet;

typedef enum {
	SUSPEND_POLICY_NONE = 0,
	SUSPEND_POLICY_EVENT_THREAD = 1,
	SUSPEND_POLICY_ALL = 2
} SuspendPolicy;

typedef enum {
	ERR_NONE = 0,
	ERR_INVALID_OBJECT = 20,
	ERR_INVALID_FIELDID = 25,
	ERR_INVALID_FRAMEID = 30,
	ERR_NOT_IMPLEMENTED = 100,
	ERR_NOT_SUSPENDED = 101,
	ERR_INVALID_ARGUMENT = 102,
	ERR_UNLOADED = 103,
	ERR_NO_INVOCATION = 104,
	ERR_ABSENT_INFORMATION = 105,
	ERR_NO_SEQ_POINT_AT_IL_OFFSET = 106,
	ERR_INVOKE_ABORTED = 107,
	ERR_LOADER_ERROR = 200, /*XXX extend the protocol to pass this information down the pipe */
} ErrorCode;

typedef enum {
	TOKEN_TYPE_STRING = 0,
	TOKEN_TYPE_TYPE = 1,
	TOKEN_TYPE_FIELD = 2,
	TOKEN_TYPE_METHOD = 3,
	TOKEN_TYPE_UNKNOWN = 4
} DebuggerTokenType;

typedef enum {
	VALUE_TYPE_ID_NULL = 0xf0,
	VALUE_TYPE_ID_TYPE = 0xf1,
	VALUE_TYPE_ID_PARENT_VTYPE = 0xf2,
	VALUE_TYPE_ID_FIXED_ARRAY = 0xf3
} ValueTypeId;

typedef enum {
	FRAME_FLAG_DEBUGGER_INVOKE = 1,
	FRAME_FLAG_NATIVE_TRANSITION = 2
} StackFrameFlags;

typedef enum {
	INVOKE_FLAG_DISABLE_BREAKPOINTS = 1,
	INVOKE_FLAG_SINGLE_THREADED = 2,
	INVOKE_FLAG_RETURN_OUT_THIS = 4,
	INVOKE_FLAG_RETURN_OUT_ARGS = 8,
	INVOKE_FLAG_VIRTUAL = 16
} InvokeFlags;

typedef enum {
	BINDING_FLAGS_IGNORE_CASE = 0x70000000,
} BindingFlagsExtensions;

typedef enum {
	CMD_VM_VERSION = 1,
	CMD_VM_ALL_THREADS = 2,
	CMD_VM_SUSPEND = 3,
	CMD_VM_RESUME = 4,
	CMD_VM_EXIT = 5,
	CMD_VM_DISPOSE = 6,
	CMD_VM_INVOKE_METHOD = 7,
	CMD_VM_SET_PROTOCOL_VERSION = 8,
	CMD_VM_ABORT_INVOKE = 9,
	CMD_VM_SET_KEEPALIVE = 10,
	CMD_VM_GET_TYPES_FOR_SOURCE_FILE = 11,
	CMD_VM_GET_TYPES = 12,
	CMD_VM_INVOKE_METHODS = 13,
	CMD_VM_START_BUFFERING = 14,
	CMD_VM_STOP_BUFFERING = 15
} CmdVM;

typedef enum {
	CMD_THREAD_GET_FRAME_INFO = 1,
	CMD_THREAD_GET_NAME = 2,
	CMD_THREAD_GET_STATE = 3,
	CMD_THREAD_GET_INFO = 4,
	CMD_THREAD_GET_ID = 5,
	CMD_THREAD_GET_TID = 6,
	CMD_THREAD_SET_IP = 7,
	CMD_THREAD_ELAPSED_TIME = 8
} CmdThread;

typedef enum {
	CMD_EVENT_REQUEST_SET = 1,
	CMD_EVENT_REQUEST_CLEAR = 2,
	CMD_EVENT_REQUEST_CLEAR_ALL_BREAKPOINTS = 3
} CmdEvent;

typedef enum {
	CMD_COMPOSITE = 100
} CmdComposite;

typedef enum {
	CMD_APPDOMAIN_GET_ROOT_DOMAIN = 1,
	CMD_APPDOMAIN_GET_FRIENDLY_NAME = 2,
	CMD_APPDOMAIN_GET_ASSEMBLIES = 3,
	CMD_APPDOMAIN_GET_ENTRY_ASSEMBLY = 4,
	CMD_APPDOMAIN_CREATE_STRING = 5,
	CMD_APPDOMAIN_GET_CORLIB = 6,
	CMD_APPDOMAIN_CREATE_BOXED_VALUE = 7,
	CMD_APPDOMAIN_CREATE_BYTE_ARRAY = 8,
} CmdAppDomain;

typedef enum {
	CMD_ASSEMBLY_GET_LOCATION = 1,
	CMD_ASSEMBLY_GET_ENTRY_POINT = 2,
	CMD_ASSEMBLY_GET_MANIFEST_MODULE = 3,
	CMD_ASSEMBLY_GET_OBJECT = 4,
	CMD_ASSEMBLY_GET_TYPE = 5,
	CMD_ASSEMBLY_GET_NAME = 6,
	CMD_ASSEMBLY_GET_DOMAIN = 7,
	CMD_ASSEMBLY_GET_METADATA_BLOB = 8,
	CMD_ASSEMBLY_GET_IS_DYNAMIC = 9,
	CMD_ASSEMBLY_GET_PDB_BLOB = 10,
	CMD_ASSEMBLY_GET_TYPE_FROM_TOKEN = 11,
	CMD_ASSEMBLY_GET_METHOD_FROM_TOKEN = 12,
	CMD_ASSEMBLY_HAS_DEBUG_INFO = 13
} CmdAssembly;

typedef enum {
	CMD_MODULE_GET_INFO = 1,
} CmdModule;

typedef enum {
	CMD_FIELD_GET_INFO = 1,
} CmdField;

typedef enum {
	CMD_METHOD_GET_NAME = 1,
	CMD_METHOD_GET_DECLARING_TYPE = 2,
	CMD_METHOD_GET_DEBUG_INFO = 3,
	CMD_METHOD_GET_PARAM_INFO = 4,
	CMD_METHOD_GET_LOCALS_INFO = 5,
	CMD_METHOD_GET_INFO = 6,
	CMD_METHOD_GET_BODY = 7,
	CMD_METHOD_RESOLVE_TOKEN = 8,
	CMD_METHOD_GET_CATTRS = 9,
	CMD_METHOD_MAKE_GENERIC_METHOD = 10
} CmdMethod;

typedef enum {
	CMD_TYPE_GET_INFO = 1,
	CMD_TYPE_GET_METHODS = 2,
	CMD_TYPE_GET_FIELDS = 3,
	CMD_TYPE_GET_VALUES = 4,
	CMD_TYPE_GET_OBJECT = 5,
	CMD_TYPE_GET_SOURCE_FILES = 6,
	CMD_TYPE_SET_VALUES = 7,
	CMD_TYPE_IS_ASSIGNABLE_FROM = 8,
	CMD_TYPE_GET_PROPERTIES = 9,
	CMD_TYPE_GET_CATTRS = 10,
	CMD_TYPE_GET_FIELD_CATTRS = 11,
	CMD_TYPE_GET_PROPERTY_CATTRS = 12,
	CMD_TYPE_GET_SOURCE_FILES_2 = 13,
	CMD_TYPE_GET_VALUES_2 = 14,
	CMD_TYPE_GET_METHODS_BY_NAME_FLAGS = 15,
	CMD_TYPE_GET_INTERFACES = 16,
	CMD_TYPE_GET_INTERFACE_MAP = 17,
	CMD_TYPE_IS_INITIALIZED = 18,
	CMD_TYPE_CREATE_INSTANCE = 19,
	CMD_TYPE_GET_VALUE_SIZE = 20
} CmdType;

typedef enum {
	CMD_STACK_FRAME_GET_VALUES = 1,
	CMD_STACK_FRAME_GET_THIS = 2,
	CMD_STACK_FRAME_SET_VALUES = 3,
	CMD_STACK_FRAME_GET_DOMAIN = 4,
	CMD_STACK_FRAME_SET_THIS = 5,
} CmdStackFrame;

typedef enum {
	CMD_ARRAY_REF_GET_LENGTH = 1,
	CMD_ARRAY_REF_GET_VALUES = 2,
	CMD_ARRAY_REF_SET_VALUES = 3,
} CmdArray;

typedef enum {
	CMD_STRING_REF_GET_VALUE = 1,
	CMD_STRING_REF_GET_LENGTH = 2,
	CMD_STRING_REF_GET_CHARS = 3
} CmdString;

typedef enum {
	CMD_POINTER_GET_VALUE = 1
} CmdPointer;

typedef enum {
	CMD_OBJECT_REF_GET_TYPE = 1,
	CMD_OBJECT_REF_GET_VALUES = 2,
	CMD_OBJECT_REF_IS_COLLECTED = 3,
	CMD_OBJECT_REF_GET_ADDRESS = 4,
	CMD_OBJECT_REF_GET_DOMAIN = 5,
	CMD_OBJECT_REF_SET_VALUES = 6,
	CMD_OBJECT_REF_GET_INFO = 7,
} CmdObject;

/*
 * Contains additional information for an event
 */
typedef struct {
	/* For EVENT_KIND_EXCEPTION */
	MonoObject *exc;
	MonoContext catch_ctx;
	gboolean caught;
	/* For EVENT_KIND_USER_LOG */
	int level;
	char *category, *message;
	/* For EVENT_KIND_TYPE_LOAD */
	MonoClass *klass;
	/* For EVENT_KIND_CRASH  */
	char *dump;
	MonoStackHash *hashes;
} EventInfo;

typedef struct {
	guint8 *buf, *p, *end;
} Buffer;

typedef struct ReplyPacket {
	int id;
	int error;
	Buffer *data;
} ReplyPacket;

#define DEBUG(level,s) do { if (G_UNLIKELY ((level) <= log_level)) { s; fflush (log_file); } } while (0)

#ifdef HOST_ANDROID
#define DEBUG_PRINTF(level, ...) do { if (G_UNLIKELY ((level) <= log_level)) { g_print (__VA_ARGS__); } } while (0)
#else
#define DEBUG_PRINTF(level, ...) do { if (G_UNLIKELY ((level) <= log_level)) { fprintf (log_file, __VA_ARGS__); fflush (log_file); } } while (0)
#endif

#ifdef HOST_WIN32
#define get_last_sock_error() WSAGetLastError()
#define MONO_EWOULDBLOCK WSAEWOULDBLOCK
#define MONO_EINTR WSAEINTR
#else
#define get_last_sock_error() errno
#define MONO_EWOULDBLOCK EWOULDBLOCK
#define MONO_EINTR EINTR
#endif

#define CHECK_PROTOCOL_VERSION(major,minor) \
	(protocol_version_set && (major_version > (major) || (major_version == (major) && minor_version >= (minor))))

/*
 * Globals
 */

static AgentConfig agent_config;

/* 
 * Whenever the agent is fully initialized.
 * When using the onuncaught or onthrow options, only some parts of the agent are
 * initialized on startup, and the full initialization which includes connection
 * establishment and the startup of the agent thread is only done in response to
 * an event.
 */
static gint32 inited;

#ifndef DISABLE_SOCKET_TRANSPORT
static int conn_fd;
static int listen_fd;
#endif

static int packet_id = 0;

static int objref_id = 0;

static int event_request_id = 0;

static int frame_id = 0;

static GPtrArray *event_requests;

static MonoNativeTlsKey debugger_tls_id;

static gboolean vm_start_event_sent, vm_death_event_sent, disconnected;

/* Maps MonoInternalThread -> DebuggerTlsData */
/* Protected by the loader lock */
static MonoGHashTable *thread_to_tls;

/* Maps tid -> MonoInternalThread */
/* Protected by the loader lock */
static MonoGHashTable *tid_to_thread;

/* Maps tid -> MonoThread (not MonoInternalThread) */
/* Protected by the loader lock */
static MonoGHashTable *tid_to_thread_obj;

static MonoNativeThreadId debugger_thread_id;

static MonoThreadHandle *debugger_thread_handle;

static int log_level;

static int file_check_valid_memory = -1;

static char* filename_check_valid_memory;

static gboolean embedding;

static FILE *log_file;

/* Assemblies whose assembly load event has no been sent yet */
/* Protected by the dbg lock */
static GPtrArray *pending_assembly_loads;

/* Whenever the debugger thread has exited */
static gboolean debugger_thread_exited;

/* Cond variable used to wait for debugger_thread_exited becoming true */
static MonoCoopCond debugger_thread_exited_cond;

/* Mutex for the cond var above */
static MonoCoopMutex debugger_thread_exited_mutex;

/* The protocol version of the client */
static int major_version, minor_version;

/* Whenever the variables above are set by the client */
static gboolean protocol_version_set;

/* The number of times the runtime is suspended */
static gint32 suspend_count;

/* Whenever to buffer reply messages and send them together */
static gboolean buffer_replies;

/* Buffered reply packets */
static ReplyPacket reply_packets [128];
static int nreply_packets;

#define dbg_lock mono_de_lock
#define dbg_unlock mono_de_unlock

static void transport_init (void);
static void transport_connect (const char *address);
static gboolean transport_handshake (void);
static void register_transport (DebuggerTransport *trans);

static gsize WINAPI debugger_thread (void *arg);

static void runtime_initialized (MonoProfiler *prof);

static void runtime_shutdown (MonoProfiler *prof);

static void thread_startup (MonoProfiler *prof, uintptr_t tid);

static void thread_end (MonoProfiler *prof, uintptr_t tid);

static void appdomain_load (MonoProfiler *prof, MonoDomain *domain);

static void appdomain_start_unload (MonoProfiler *prof, MonoDomain *domain);

static void appdomain_unload (MonoProfiler *prof, MonoDomain *domain);

static void emit_appdomain_load (gpointer key, gpointer value, gpointer user_data);

static void emit_thread_start (gpointer key, gpointer value, gpointer user_data);

static void invalidate_each_thread (gpointer key, gpointer value, gpointer user_data);

static void assembly_load (MonoProfiler *prof, MonoAssembly *assembly);

static void assembly_unload (MonoProfiler *prof, MonoAssembly *assembly);

static void gc_finalizing (MonoProfiler *prof);

static void gc_finalized (MonoProfiler *prof);

static void emit_assembly_load (gpointer assembly, gpointer user_data);

static void emit_type_load (gpointer key, gpointer type, gpointer user_data);

static void jit_done (MonoProfiler *prof, MonoMethod *method, MonoJitInfo *jinfo);

static void jit_failed (MonoProfiler *prof, MonoMethod *method);

static void jit_end (MonoProfiler *prof, MonoMethod *method, MonoJitInfo *jinfo);

static void suspend_current (void);

static void clear_event_requests_for_assembly (MonoAssembly *assembly);

static void clear_types_for_assembly (MonoAssembly *assembly);

static void process_profiler_event (EventKind event, gpointer arg);

/* Submodule init/cleanup */
static void event_requests_cleanup (void);

static void objrefs_init (void);
static void objrefs_cleanup (void);

static void ids_init (void);
static void ids_cleanup (void);

static void suspend_init (void);

static void start_debugger_thread (MonoError *error);
static void stop_debugger_thread (void);

static void finish_agent_init (gboolean on_startup);

static void process_profiler_event (EventKind event, gpointer arg);

static void invalidate_frames (DebuggerTlsData *tls);

/* Callbacks used by debugger-engine */
static MonoContext* tls_get_restore_state (void *the_tls);
static gboolean try_process_suspend (void *tls, MonoContext *ctx, gboolean from_breakpoint);
static gboolean begin_breakpoint_processing (void *tls, MonoContext *ctx, MonoJitInfo *ji, gboolean from_signal);
static void begin_single_step_processing (MonoContext *ctx, gboolean from_signal);
static void ss_discard_frame_context (void *the_tls);
static void ss_calculate_framecount (void *tls, MonoContext *ctx, gboolean force_use_ctx, DbgEngineStackFrame ***frames, int *nframes);
static gboolean ensure_jit (DbgEngineStackFrame* the_frame);
static int ensure_runtime_is_suspended (void);
static int get_this_async_id (DbgEngineStackFrame *frame);
static gboolean set_set_notification_for_wait_completion_flag (DbgEngineStackFrame *frame);
static MonoMethod* get_notify_debugger_of_wait_completion_method (void);
static void* create_breakpoint_events (GPtrArray *ss_reqs, GPtrArray *bp_reqs, MonoJitInfo *ji, EventKind kind);
static void process_breakpoint_events (void *_evts, MonoMethod *method, MonoContext *ctx, int il_offset);
static int ss_create_init_args (SingleStepReq *ss_req, SingleStepArgs *args);
static void ss_args_destroy (SingleStepArgs *ss_args);
static int handle_multiple_ss_requests (void);

static GENERATE_TRY_GET_CLASS_WITH_CACHE (fixed_buffer, "System.Runtime.CompilerServices", "FixedBufferAttribute")

#ifndef DISABLE_SOCKET_TRANSPORT
static void
register_socket_transport (void);
#endif

static gboolean
is_debugger_thread (void)
{
	MonoInternalThread *internal;

	internal = mono_thread_internal_current ();
	if (!internal)
		return FALSE;

	return internal->debugger_thread;
}

static int
parse_address (char *address, char **host, int *port)
{
	char *pos = strchr (address, ':');

	if (pos == NULL || pos == address)
		return 1;

	size_t len = pos - address;
	*host = (char *)g_malloc (len + 1);
	memcpy (*host, address, len);
	(*host) [len] = '\0';

	*port = atoi (pos + 1);

	return 0;
}

static void
print_usage (void)
{
	g_printerr ("Usage: mono --debugger-agent=[<option>=<value>,...] ...\n");
	g_printerr ("Available options:\n");
	g_printerr ("  transport=<transport>\t\tTransport to use for connecting to the debugger (mandatory, possible values: 'dt_socket')\n");
	g_printerr ("  address=<hostname>:<port>\tAddress to connect to (mandatory)\n");
	g_printerr ("  loglevel=<n>\t\t\tLog level (defaults to 0)\n");
	g_printerr ("  logfile=<file>\t\tFile to log to (defaults to stdout)\n");
	g_printerr ("  suspend=y/n\t\t\tWhether to suspend after startup.\n");
	g_printerr ("  timeout=<n>\t\t\tTimeout for connecting in milliseconds.\n");
	g_printerr ("  server=y/n\t\t\tWhether to listen for a client connection.\n");
	g_printerr ("  keepalive=<n>\t\t\tSend keepalive events every n milliseconds.\n");
	g_printerr ("  setpgid=y/n\t\t\tWhether to call setpid(0, 0) after startup.\n");
	g_printerr ("  help\t\t\t\tPrint this help.\n");
}

static gboolean
parse_flag (const char *option, char *flag)
{
	if (!strcmp (flag, "y"))
		return TRUE;
	else if (!strcmp (flag, "n"))
		return FALSE;
	else {
		g_printerr ("debugger-agent: The valid values for the '%s' option are 'y' and 'n'.\n", option);
		exit (1);
		return FALSE;
	}
}

static void
debugger_agent_parse_options (char *options)
{
	char **args, **ptr;
	char *host;
	int port;
	char *extra;

#ifndef MONO_ARCH_SOFT_DEBUG_SUPPORTED
	g_printerr ("--debugger-agent is not supported on this platform.\n");
	exit (1);
#endif

	extra = g_getenv ("MONO_SDB_ENV_OPTIONS");
	if (extra) {
		options = g_strdup_printf ("%s,%s", options, extra);
		g_free (extra);
	}

	agent_config.enabled = TRUE;
	agent_config.suspend = TRUE;
	agent_config.server = FALSE;
	agent_config.defer = FALSE;
	agent_config.address = NULL;

	//agent_config.log_level = 10;

	args = g_strsplit (options, ",", -1);
	for (ptr = args; ptr && *ptr; ptr ++) {
		char *arg = *ptr;

		if (strncmp (arg, "transport=", 10) == 0) {
			agent_config.transport = g_strdup (arg + 10);
		} else if (strncmp (arg, "address=", 8) == 0) {
			agent_config.address = g_strdup (arg + 8);
		} else if (strncmp (arg, "loglevel=", 9) == 0) {
			agent_config.log_level = atoi (arg + 9);
		} else if (strncmp (arg, "logfile=", 8) == 0) {
			agent_config.log_file = g_strdup (arg + 8);
		} else if (strncmp (arg, "suspend=", 8) == 0) {
			agent_config.suspend = parse_flag ("suspend", arg + 8);
		} else if (strncmp (arg, "server=", 7) == 0) {
			agent_config.server = parse_flag ("server", arg + 7);
		} else if (strncmp (arg, "onuncaught=", 11) == 0) {
			agent_config.onuncaught = parse_flag ("onuncaught", arg + 11);
		} else if (strncmp (arg, "onthrow=", 8) == 0) {
			/* We support multiple onthrow= options */
			agent_config.onthrow = g_slist_append (agent_config.onthrow, g_strdup (arg + 8));
		} else if (strncmp (arg, "onthrow", 7) == 0) {
			agent_config.onthrow = g_slist_append (agent_config.onthrow, g_strdup (""));
		} else if (strncmp (arg, "help", 4) == 0) {
			print_usage ();
			exit (0);
		} else if (strncmp (arg, "timeout=", 8) == 0) {
			agent_config.timeout = atoi (arg + 8);
		} else if (strncmp (arg, "launch=", 7) == 0) {
			agent_config.launch = g_strdup (arg + 7);
		} else if (strncmp (arg, "embedding=", 10) == 0) {
			agent_config.embedding = atoi (arg + 10) == 1;
		} else if (strncmp (arg, "keepalive=", 10) == 0) {
			agent_config.keepalive = atoi (arg + 10);
		} else if (strncmp (arg, "setpgid=", 8) == 0) {
			agent_config.setpgid = parse_flag ("setpgid", arg + 8);
		} else {
			print_usage ();
			exit (1);
		}
	}

	if (agent_config.server && !agent_config.suspend) {
		/* Waiting for deferred attachment */
		agent_config.defer = TRUE;
		if (agent_config.address == NULL) {
			agent_config.address = g_strdup_printf ("0.0.0.0:%u", 56000 + (mono_process_current_pid () % 1000));
		}
	}

	//agent_config.log_level = 0;

	if (agent_config.transport == NULL) {
		g_printerr ("debugger-agent: The 'transport' option is mandatory.\n");
		exit (1);
	}

	if (agent_config.address == NULL && !agent_config.server) {
		g_printerr ("debugger-agent: The 'address' option is mandatory.\n");
		exit (1);
	}

	// FIXME:
	if (!strcmp (agent_config.transport, "dt_socket")) {
		if (agent_config.address && parse_address (agent_config.address, &host, &port)) {
			g_printerr ("debugger-agent: The format of the 'address' options is '<host>:<port>'\n");
			exit (1);
		}
	}
}

void
mono_debugger_set_thread_state (DebuggerTlsData *tls, MonoDebuggerThreadState expected, MonoDebuggerThreadState set)
{
	g_assertf (tls, "Cannot get state of null thread", NULL);

	g_assert (tls->thread_state == expected);

	tls->thread_state = set;
}

MonoDebuggerThreadState
mono_debugger_get_thread_state (DebuggerTlsData *tls)
{
	g_assertf (tls, "Cannot get state of null thread", NULL);

	return tls->thread_state;
}

gsize
mono_debugger_tls_thread_id (DebuggerTlsData *tls)
{
	if (!tls)
		return 0;

	return tls->thread_id;
}

// Only call this function with the loader lock held
MonoGHashTable *
mono_debugger_get_thread_states (void)
{
	return thread_to_tls;
}

gboolean
mono_debugger_is_disconnected (void)
{
	return disconnected;
}

static void
debugger_agent_init (void)
{
	if (!agent_config.enabled)
		return;

	DebuggerEngineCallbacks cbs;
	memset (&cbs, 0, sizeof (cbs));
	cbs.tls_get_restore_state = tls_get_restore_state;
	cbs.try_process_suspend = try_process_suspend;
	cbs.begin_breakpoint_processing = begin_breakpoint_processing;
	cbs.begin_single_step_processing = begin_single_step_processing;
	cbs.ss_discard_frame_context = ss_discard_frame_context;
	cbs.ss_calculate_framecount = ss_calculate_framecount;
	cbs.ensure_jit = ensure_jit;
	cbs.ensure_runtime_is_suspended = ensure_runtime_is_suspended;
	cbs.get_this_async_id = get_this_async_id;
	cbs.set_set_notification_for_wait_completion_flag = set_set_notification_for_wait_completion_flag;
	cbs.get_notify_debugger_of_wait_completion_method = get_notify_debugger_of_wait_completion_method;
	cbs.create_breakpoint_events = create_breakpoint_events;
	cbs.process_breakpoint_events = process_breakpoint_events;
	cbs.ss_create_init_args = ss_create_init_args;
	cbs.ss_args_destroy = ss_args_destroy;
	cbs.handle_multiple_ss_requests = handle_multiple_ss_requests;

	mono_de_init (&cbs);

	transport_init ();

	/* Need to know whenever a thread has acquired the loader mutex */
	mono_loader_lock_track_ownership (TRUE);

	event_requests = g_ptr_array_new ();

	mono_coop_mutex_init (&debugger_thread_exited_mutex);
	mono_coop_cond_init (&debugger_thread_exited_cond);

	MonoProfilerHandle prof = mono_profiler_create (NULL);
	mono_profiler_set_runtime_shutdown_end_callback (prof, runtime_shutdown);
	mono_profiler_set_runtime_initialized_callback (prof, runtime_initialized);
	mono_profiler_set_domain_loaded_callback (prof, appdomain_load);
	mono_profiler_set_domain_unloading_callback (prof, appdomain_start_unload);
	mono_profiler_set_domain_unloaded_callback (prof, appdomain_unload);
	mono_profiler_set_thread_started_callback (prof, thread_startup);
	mono_profiler_set_thread_stopped_callback (prof, thread_end);
	mono_profiler_set_assembly_loaded_callback (prof, assembly_load);
	mono_profiler_set_assembly_unloading_callback (prof, assembly_unload);
	mono_profiler_set_jit_done_callback (prof, jit_done);
	mono_profiler_set_jit_failed_callback (prof, jit_failed);
	mono_profiler_set_gc_finalizing_callback (prof, gc_finalizing);
	mono_profiler_set_gc_finalized_callback (prof, gc_finalized);
	
	mono_native_tls_alloc (&debugger_tls_id, NULL);

	/* Needed by the hash_table_new_type () call below */
	mono_gc_base_init ();

	thread_to_tls = mono_g_hash_table_new_type_internal ((GHashFunc)mono_object_hash_internal, NULL, MONO_HASH_KEY_GC, MONO_ROOT_SOURCE_DEBUGGER, NULL, "Debugger TLS Table");

	tid_to_thread = mono_g_hash_table_new_type_internal (NULL, NULL, MONO_HASH_VALUE_GC, MONO_ROOT_SOURCE_DEBUGGER, NULL, "Debugger Thread Table");

	tid_to_thread_obj = mono_g_hash_table_new_type_internal (NULL, NULL, MONO_HASH_VALUE_GC, MONO_ROOT_SOURCE_DEBUGGER, NULL, "Debugger Thread Object Table");

	pending_assembly_loads = g_ptr_array_new ();

	log_level = agent_config.log_level;

	embedding = agent_config.embedding;
	disconnected = TRUE;

	if (agent_config.log_file) {
		log_file = fopen (agent_config.log_file, "w+");
		if (!log_file) {
			g_printerr ("Unable to create log file '%s': %s.\n", agent_config.log_file, strerror (errno));
			exit (1);
		}
	} else {
		log_file = stdout;
	}
	mono_de_set_log_level (log_level, log_file);

	ids_init ();
	objrefs_init ();
	suspend_init ();

	mini_get_debug_options ()->gen_sdb_seq_points = TRUE;
	/* 
	 * This is needed because currently we don't handle liveness info.
	 */
	mini_get_debug_options ()->mdb_optimizations = TRUE;

#ifndef MONO_ARCH_HAVE_CONTEXT_SET_INT_REG
	/* This is needed because we can't set local variables in registers yet */
	mono_disable_optimizations (MONO_OPT_LINEARS);
#endif

	/*
	 * The stack walk done from thread_interrupt () needs to be signal safe, but it
	 * isn't, since it can call into mono_aot_find_jit_info () which is not signal
	 * safe (#3411). So load AOT info eagerly when the debugger is running as a
	 * workaround.
	 */
	mini_get_debug_options ()->load_aot_jit_info_eagerly = TRUE;

#ifdef HAVE_SETPGID
	if (agent_config.setpgid)
		setpgid (0, 0);
#endif

	if (!agent_config.onuncaught && !agent_config.onthrow)
		finish_agent_init (TRUE);
}

/*
 * finish_agent_init:
 *
 *   Finish the initialization of the agent. This involves connecting the transport
 * and starting the agent thread. This is either done at startup, or
 * in response to some event like an unhandled exception.
 */
static void
finish_agent_init (gboolean on_startup)
{
	if (mono_atomic_cas_i32 (&inited, 1, 0) == 1)
		return;

	if (agent_config.launch) {

		// FIXME: Generated address
		// FIXME: Races with transport_connect ()

#ifdef G_OS_WIN32
		// Nothing. FIXME? g_spawn_async_with_pipes is easy enough to provide for Windows if needed.
#elif !HAVE_G_SPAWN
		g_printerr ("g_spawn_async_with_pipes not supported on this platform\n");
		exit (1);
#else
		char *argv [ ] = {
			agent_config.launch,
			agent_config.transport,
			agent_config.address,
			NULL
		};
		int res = g_spawn_async_with_pipes (NULL, argv, NULL, (GSpawnFlags)0, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
		if (!res) {
			g_printerr ("Failed to execute '%s'.\n", agent_config.launch);
			exit (1);
		}
#endif
	}

	transport_connect (agent_config.address);

	if (!on_startup) {
		/* Do some which is usually done after sending the VMStart () event */
		vm_start_event_sent = TRUE;
		ERROR_DECL (error);
		start_debugger_thread (error);
		mono_error_assert_ok (error);
	}
}

static void
mono_debugger_agent_cleanup (void)
{
	if (!inited)
		return;

	stop_debugger_thread ();

	event_requests_cleanup ();
	objrefs_cleanup ();
	ids_cleanup ();

	mono_de_cleanup ();

	if (file_check_valid_memory != -1) {
		remove (filename_check_valid_memory);
		g_free (filename_check_valid_memory);
		close (file_check_valid_memory);
	}
}

/*
 * SOCKET TRANSPORT
 */

#ifndef DISABLE_SOCKET_TRANSPORT

/*
 * recv_length:
 *
 * recv() + handle incomplete reads and EINTR
 */
static int
socket_transport_recv (void *buf, int len)
{
	int res;
	int total = 0;
	int fd = conn_fd;
	int flags = 0;
	static gint64 last_keepalive;
	gint64 msecs;

	MONO_ENTER_GC_SAFE;

	do {
	again:
		res = recv (fd, (char *) buf + total, len - total, flags);
		if (res > 0)
			total += res;
		if (agent_config.keepalive) {
			gboolean need_keepalive = FALSE;
			if (res == -1 && get_last_sock_error () == MONO_EWOULDBLOCK) {
				need_keepalive = TRUE;
			} else if (res == -1) {
				/* This could happen if recv () is interrupted repeatedly */
				msecs = mono_msec_ticks ();
				if (msecs - last_keepalive >= agent_config.keepalive) {
					need_keepalive = TRUE;
					last_keepalive = msecs;
				}
			}
			if (need_keepalive) {
				process_profiler_event (EVENT_KIND_KEEPALIVE, NULL);
				goto again;
			}
		}
	} while ((res > 0 && total < len) || (res == -1 && get_last_sock_error () == MONO_EINTR));

	MONO_EXIT_GC_SAFE;

	return total;
}
 
static void
set_keepalive (void)
{
	struct timeval tv;
	int result;

	if (!agent_config.keepalive || !conn_fd)
		return;

	tv.tv_sec = agent_config.keepalive / 1000;
	tv.tv_usec = (agent_config.keepalive % 1000) * 1000;

	result = setsockopt (conn_fd, SOL_SOCKET, SO_RCVTIMEO, (char *) &tv, sizeof(struct timeval));
	g_assert (result >= 0);
}

static int
socket_transport_accept (int socket_fd)
{
	MONO_ENTER_GC_SAFE;
	conn_fd = accept (socket_fd, NULL, NULL);
	MONO_EXIT_GC_SAFE;

	if (conn_fd == -1) {
		g_printerr ("debugger-agent: Unable to listen on %d\n", socket_fd);
	} else {
		DEBUG_PRINTF (1, "Accepted connection from client, connection fd=%d.\n", conn_fd);
	}
	
	return conn_fd;
}

static gboolean
socket_transport_send (void *data, int len)
{
	int res;

	MONO_ENTER_GC_SAFE;

	do {
		res = send (conn_fd, (const char*)data, len, 0);
	} while (res == -1 && get_last_sock_error () == MONO_EINTR);

	MONO_EXIT_GC_SAFE;

	if (res != len)
		return FALSE;
	else
		return TRUE;
}

/*
 * socket_transport_connect:
 *
 *   Connect/Listen on HOST:PORT. If HOST is NULL, generate an address and listen on it.
 */
static void
socket_transport_connect (const char *address)
{
	MonoAddressInfo *result;
	MonoAddressEntry *rp;
	int sfd = -1, s, res;
	char *host;
	int port;

	if (agent_config.address) {
		res = parse_address (agent_config.address, &host, &port);
		g_assert (res == 0);
	} else {
		host = NULL;
		port = 0;
	}

	conn_fd = -1;
	listen_fd = -1;

	if (host) {
		int hints[] = {
			MONO_HINT_IPV4 | MONO_HINT_NUMERIC_HOST,
			MONO_HINT_IPV6 | MONO_HINT_NUMERIC_HOST,
			MONO_HINT_UNSPECIFIED
		};

		mono_network_init ();

		for (int i = 0; i < sizeof(hints) / sizeof(int); i++) {
			/* Obtain address(es) matching host/port */
			s = mono_get_address_info (host, port, hints[i], &result);
			if (s == 0)
				break;
		}
		if (s != 0) {
			g_printerr ("debugger-agent: Unable to resolve %s:%d: %d\n", host, port, s); // FIXME add portable error conversion functions
			exit (1);
		}
	}

	if (agent_config.server) {
		/* Wait for a connection */
		if (!host) {
			struct sockaddr_in addr;
			socklen_t addrlen;

			/* No address, generate one */
			sfd = socket (AF_INET, SOCK_STREAM, 0);
			if (sfd == -1) {
				g_printerr ("debugger-agent: Unable to create a socket: %s\n", strerror (get_last_sock_error ()));
				exit (1);
			}

			/* This will bind the socket to a random port */
			res = listen (sfd, 16);
			if (res == -1) {
				g_printerr ("debugger-agent: Unable to setup listening socket: %s\n", strerror (get_last_sock_error ()));
				exit (1);
			}
			listen_fd = sfd;

			addrlen = sizeof (addr);
			memset (&addr, 0, sizeof (addr));
			res = getsockname (sfd, (struct sockaddr*)&addr, &addrlen);
			g_assert (res == 0);

			host = (char*)"127.0.0.1";
			port = ntohs (addr.sin_port);

			/* Emit the address to stdout */
			/* FIXME: Should print another interface, not localhost */
			printf ("%s:%d\n", host, port);
		} else {
			/* Listen on the provided address */
			for (rp = result->entries; rp != NULL; rp = rp->next) {
				MonoSocketAddress sockaddr;
				socklen_t sock_len;
				int n = 1;

				mono_socket_address_init (&sockaddr, &sock_len, rp->family, &rp->address, port);

				sfd = socket (rp->family, rp->socktype,
							  rp->protocol);
				if (sfd == -1)
					continue;

				if (setsockopt (sfd, SOL_SOCKET, SO_REUSEADDR, (const char*)&n, sizeof(n)) == -1)
					continue;

				res = bind (sfd, &sockaddr.addr, sock_len);
				if (res == -1)
					continue;

				res = listen (sfd, 16);
				if (res == -1)
					continue;
				listen_fd = sfd;
				break;
			}

			mono_free_address_info (result);
		}

		if (agent_config.defer)
			return;

		DEBUG_PRINTF (1, "Listening on %s:%d (timeout=%d ms)...\n", host, port, agent_config.timeout);

		if (agent_config.timeout) {
			fd_set readfds;
			struct timeval tv;

			tv.tv_sec = 0;
			tv.tv_usec = agent_config.timeout * 1000;
			FD_ZERO (&readfds);
			FD_SET (sfd, &readfds);

			MONO_ENTER_GC_SAFE;
			res = select (sfd + 1, &readfds, NULL, NULL, &tv);
			MONO_EXIT_GC_SAFE;

			if (res == 0) {
				g_printerr ("debugger-agent: Timed out waiting to connect.\n");
				exit (1);
			}
		}

		conn_fd = socket_transport_accept (sfd);
		if (conn_fd == -1)
			exit (1);

		DEBUG_PRINTF (1, "Accepted connection from client, socket fd=%d.\n", conn_fd);
	} else {
		/* Connect to the specified address */
		/* FIXME: Respect the timeout */
		for (rp = result->entries; rp != NULL; rp = rp->next) {
			MonoSocketAddress sockaddr;
			socklen_t sock_len;

			mono_socket_address_init (&sockaddr, &sock_len, rp->family, &rp->address, port);

			sfd = socket (rp->family, rp->socktype,
						  rp->protocol);
			if (sfd == -1)
				continue;

			MONO_ENTER_GC_SAFE;
			res = connect (sfd, &sockaddr.addr, sock_len);
			MONO_EXIT_GC_SAFE;

			if (res != -1)
				break;       /* Success */
			
			MONO_ENTER_GC_SAFE;
#ifdef HOST_WIN32
			closesocket (sfd);
#else
			close (sfd);
#endif
			MONO_EXIT_GC_SAFE;
		}

		if (rp == 0) {
			g_printerr ("debugger-agent: Unable to connect to %s:%d\n", host, port);
			exit (1);
		}

		conn_fd = sfd;

		mono_free_address_info (result);
	}
	
	if (!transport_handshake ())
		exit (1);
}

static void
socket_transport_close1 (void)
{
	/* This will interrupt the agent thread */
	/* Close the read part only so it can still send back replies */
	/* Also shut down the connection listener so that we can exit normally */
#ifdef HOST_WIN32
	/* SD_RECEIVE doesn't break the recv in the debugger thread */
	shutdown (conn_fd, SD_BOTH);
	shutdown (listen_fd, SD_BOTH);
	closesocket (listen_fd);
#else
	shutdown (conn_fd, SHUT_RD);
	shutdown (listen_fd, SHUT_RDWR);
	MONO_ENTER_GC_SAFE;
	close (listen_fd);
	MONO_EXIT_GC_SAFE;
#endif
}

static void
socket_transport_close2 (void)
{
#ifdef HOST_WIN32
	shutdown (conn_fd, SD_BOTH);
#else
	shutdown (conn_fd, SHUT_RDWR);
#endif
}

static void
register_socket_transport (void)
{
	DebuggerTransport trans;

	trans.name = "dt_socket";
	trans.connect = socket_transport_connect;
	trans.close1 = socket_transport_close1;
	trans.close2 = socket_transport_close2;
	trans.send = socket_transport_send;
	trans.recv = socket_transport_recv;

	register_transport (&trans);
}

/*
 * socket_fd_transport_connect:
 *
 */
static void
socket_fd_transport_connect (const char *address)
{
	int res;

	res = sscanf (address, "%d", &conn_fd);
	if (res != 1) {
		g_printerr ("debugger-agent: socket-fd transport address is invalid: '%s'\n", address);
		exit (1);
	}

	if (!transport_handshake ())
		exit (1);
}

static void
register_socket_fd_transport (void)
{
	DebuggerTransport trans;

	/* This is the same as the 'dt_socket' transport, but receives an already connected socket fd */
	trans.name = "socket-fd";
	trans.connect = socket_fd_transport_connect;
	trans.close1 = socket_transport_close1;
	trans.close2 = socket_transport_close2;
	trans.send = socket_transport_send;
	trans.recv = socket_transport_recv;

	register_transport (&trans);
}

#endif /* DISABLE_SOCKET_TRANSPORT */

/*
 * TRANSPORT CODE
 */

#define MAX_TRANSPORTS 16

static DebuggerTransport *transport;

static DebuggerTransport transports [MAX_TRANSPORTS];
static int ntransports;

MONO_API void
mono_debugger_agent_register_transport (DebuggerTransport *trans);

void
mono_debugger_agent_register_transport (DebuggerTransport *trans)
{
	register_transport (trans);
}

static void
register_transport (DebuggerTransport *trans)
{
	g_assert (ntransports < MAX_TRANSPORTS);

	memcpy (&transports [ntransports], trans, sizeof (DebuggerTransport));
	ntransports ++;
}

static void
transport_init (void)
{
	int i;

#ifndef DISABLE_SOCKET_TRANSPORT
	register_socket_transport ();
	register_socket_fd_transport ();
#endif

	for (i = 0; i < ntransports; ++i) {
		if (!strcmp (agent_config.transport, transports [i].name))
			break;
	}
	if (i == ntransports) {
		g_printerr ("debugger-agent: The supported values for the 'transport' option are: ");
		for (i = 0; i < ntransports; ++i)
			g_printerr ("%s'%s'", i > 0 ? ", " : "", transports [i].name);
		g_printerr ("\n");
		exit (1);
	}
	transport = &transports [i];
}

void
transport_connect (const char *address)
{
	transport->connect (address);
}

static void
transport_close1 (void)
{
	transport->close1 ();
}

static void
transport_close2 (void)
{
	transport->close2 ();
}

static int
transport_send (void *buf, int len)
{
	return transport->send (buf, len);
}

static int
transport_recv (void *buf, int len)
{
	return transport->recv (buf, len);
}

gboolean
mono_debugger_agent_transport_handshake (void)
{
	return transport_handshake ();
}

static gboolean
transport_handshake (void)
{
	char handshake_msg [128];
	guint8 buf [128];
	int res;
	
	disconnected = TRUE;
	
	/* Write handshake message */
	sprintf (handshake_msg, "DWP-Handshake");

	do {
		res = transport_send (handshake_msg, strlen (handshake_msg));
	} while (res == -1 && get_last_sock_error () == MONO_EINTR);

	g_assert (res != -1);

	/* Read answer */
	res = transport_recv (buf, strlen (handshake_msg));
	if ((res != strlen (handshake_msg)) || (memcmp (buf, handshake_msg, strlen (handshake_msg)) != 0)) {
		g_printerr ("debugger-agent: DWP handshake failed.\n");
		return FALSE;
	}

	/*
	 * To support older clients, the client sends its protocol version after connecting
	 * using a command. Until that is received, default to our protocol version.
	 */
	major_version = MAJOR_VERSION;
	minor_version = MINOR_VERSION;
	protocol_version_set = FALSE;

#ifndef DISABLE_SOCKET_TRANSPORT
	// FIXME: Move this somewhere else
	/* 
	 * Set TCP_NODELAY on the socket so the client receives events/command
	 * results immediately.
	 */
	if (conn_fd) {
		int flag = 1;
		int result = setsockopt (conn_fd,
                                 IPPROTO_TCP,
                                 TCP_NODELAY,
                                 (char *) &flag,
                                 sizeof(int));
		g_assert (result >= 0);
	}

	set_keepalive ();
#endif
	
	disconnected = FALSE;
	return TRUE;
}

static void
stop_debugger_thread (void)
{
	if (!inited)
		return;

	transport_close1 ();

	/* 
	 * Wait for the thread to exit.
	 *
	 * If we continue with the shutdown without waiting for it, then the client might
	 * not receive an answer to its last command like a resume.
	 */
	if (!is_debugger_thread ()) {
		do {
			mono_coop_mutex_lock (&debugger_thread_exited_mutex);
			if (!debugger_thread_exited)
				mono_coop_cond_wait (&debugger_thread_exited_cond, &debugger_thread_exited_mutex);
			mono_coop_mutex_unlock (&debugger_thread_exited_mutex);
		} while (!debugger_thread_exited);

		if (debugger_thread_handle)
			mono_thread_info_wait_one_handle (debugger_thread_handle, MONO_INFINITE_WAIT, TRUE);
	}

	transport_close2 ();
}

static void
start_debugger_thread (MonoError *error)
{
	MonoInternalThread *thread;

	thread = mono_thread_create_internal (mono_get_root_domain (), (gpointer)debugger_thread, NULL, MONO_THREAD_CREATE_FLAGS_DEBUGGER, error);
	return_if_nok (error);

	/* Is it possible for the thread to be dead alreay ? */
	debugger_thread_handle = mono_threads_open_thread_handle (thread->handle);
	g_assert (debugger_thread_handle);
	
}

/*
 * Functions to decode protocol data
 */

static int
decode_byte (guint8 *buf, guint8 **endbuf, guint8 *limit)
{
	*endbuf = buf + 1;
	g_assert (*endbuf <= limit);
	return buf [0];
}

static int
decode_int (guint8 *buf, guint8 **endbuf, guint8 *limit)
{
	*endbuf = buf + 4;
	g_assert (*endbuf <= limit);

	return (((int)buf [0]) << 24) | (((int)buf [1]) << 16) | (((int)buf [2]) << 8) | (((int)buf [3]) << 0);
}

static gint64
decode_long (guint8 *buf, guint8 **endbuf, guint8 *limit)
{
	guint32 high = decode_int (buf, &buf, limit);
	guint32 low = decode_int (buf, &buf, limit);

	*endbuf = buf;

	return ((((guint64)high) << 32) | ((guint64)low));
}

static int
decode_id (guint8 *buf, guint8 **endbuf, guint8 *limit)
{
	return decode_int (buf, endbuf, limit);
}

static char*
decode_string (guint8 *buf, guint8 **endbuf, guint8 *limit)
{
	int len = decode_int (buf, &buf, limit);
	char *s;

	if (len < 0) {
		*endbuf = buf;
		return NULL;
	}

	s = (char *)g_malloc (len + 1);
	g_assert (s);

	memcpy (s, buf, len);
	s [len] = '\0';
	buf += len;
	*endbuf = buf;

	return s;
}

/*
 * Functions to encode protocol data
 */

static void
buffer_init (Buffer *buf, int size)
{
	buf->buf = (guint8 *)g_malloc (size);
	buf->p = buf->buf;
	buf->end = buf->buf + size;
}

static int
buffer_len (Buffer *buf)
{
	return buf->p - buf->buf;
}

static void
buffer_make_room (Buffer *buf, int size)
{
	if (buf->end - buf->p < size) {
		int new_size = buf->end - buf->buf + size + 32;
		guint8 *p = (guint8 *)g_realloc (buf->buf, new_size);
		size = buf->p - buf->buf;
		buf->buf = p;
		buf->p = p + size;
		buf->end = buf->buf + new_size;
	}
}

static void
buffer_add_byte (Buffer *buf, guint8 val)
{
	buffer_make_room (buf, 1);
	buf->p [0] = val;
	buf->p++;
}

static void
buffer_add_short (Buffer *buf, guint32 val)
{
	buffer_make_room (buf, 2);
	buf->p [0] = (val >> 8) & 0xff;
	buf->p [1] = (val >> 0) & 0xff;
	buf->p += 2;
}

static void
buffer_add_int (Buffer *buf, guint32 val)
{
	buffer_make_room (buf, 4);
	buf->p [0] = (val >> 24) & 0xff;
	buf->p [1] = (val >> 16) & 0xff;
	buf->p [2] = (val >> 8) & 0xff;
	buf->p [3] = (val >> 0) & 0xff;
	buf->p += 4;
}

static void
buffer_add_long (Buffer *buf, guint64 l)
{
	buffer_add_int (buf, (l >> 32) & 0xffffffff);
	buffer_add_int (buf, (l >> 0) & 0xffffffff);
}

static void
buffer_add_id (Buffer *buf, int id)
{
	buffer_add_int (buf, (guint64)id);
}

static void
buffer_add_data (Buffer *buf, guint8 *data, int len)
{
	buffer_make_room (buf, len);
	memcpy (buf->p, data, len);
	buf->p += len;
}

static void
buffer_add_utf16 (Buffer *buf, guint8 *data, int len)
{
#if G_BYTE_ORDER == G_LITTLE_ENDIAN
	buffer_make_room (buf, len);
	memcpy (buf->p, data, len);
#else
	for (int i=0; i<len; i +=2) {
		buf->p[i] = data[i+1];
		buf->p[i+1] = data[i];
	}
#endif
	buf->p += len;
}

static void
buffer_add_string (Buffer *buf, const char *str)
{
	int len;

	if (str == NULL) {
		buffer_add_int (buf, 0);
	} else {
		len = strlen (str);
		buffer_add_int (buf, len);
		buffer_add_data (buf, (guint8*)str, len);
	}
}

static void
buffer_add_byte_array (Buffer *buf, guint8 *bytes, guint32 arr_len)
{
    buffer_add_int (buf, arr_len);
    buffer_add_data (buf, bytes, arr_len);
}

static void
buffer_add_buffer (Buffer *buf, Buffer *data)
{
	buffer_add_data (buf, data->buf, buffer_len (data));
}

static void
buffer_free (Buffer *buf)
{
	g_free (buf->buf);
}

static gboolean
send_packet (int command_set, int command, Buffer *data)
{
	Buffer buf;
	int len, id;
	gboolean res;

	id = mono_atomic_inc_i32 (&packet_id);

	len = data->p - data->buf + 11;
	buffer_init (&buf, len);
	buffer_add_int (&buf, len);
	buffer_add_int (&buf, id);
	buffer_add_byte (&buf, 0); /* flags */
	buffer_add_byte (&buf, command_set);
	buffer_add_byte (&buf, command);
	memcpy (buf.buf + 11, data->buf, data->p - data->buf);

	res = transport_send (buf.buf, len);

	buffer_free (&buf);

	return res;
}

static gboolean
send_reply_packets (int npackets, ReplyPacket *packets)
{
	Buffer buf;
	int i, len;
	gboolean res;

	len = 0;
	for (i = 0; i < npackets; ++i)
		len += buffer_len (packets [i].data) + 11;
	buffer_init (&buf, len);
	for (i = 0; i < npackets; ++i) {
		buffer_add_int (&buf, buffer_len (packets [i].data) + 11);
		buffer_add_int (&buf, packets [i].id);
		buffer_add_byte (&buf, 0x80); /* flags */
		buffer_add_byte (&buf, (packets [i].error >> 8) & 0xff);
		buffer_add_byte (&buf, packets [i].error);
		buffer_add_buffer (&buf, packets [i].data);
	}

	res = transport_send (buf.buf, len);

	buffer_free (&buf);

	return res;
}

static gboolean
send_reply_packet (int id, int error, Buffer *data)
{
	ReplyPacket packet;

	memset (&packet, 0, sizeof (packet));
	packet.id = id;
	packet.error = error;
	packet.data = data;

	return send_reply_packets (1, &packet);
}

static void
send_buffered_reply_packets (void)
{
	int i;

	send_reply_packets (nreply_packets, reply_packets);
	for (i = 0; i < nreply_packets; ++i)
		buffer_free (reply_packets [i].data);
	DEBUG_PRINTF (1, "[dbg] Sent %d buffered reply packets [at=%lx].\n", nreply_packets, (long)mono_100ns_ticks () / 10000);
	nreply_packets = 0;
}

static void
buffer_reply_packet (int id, int error, Buffer *data)
{
	ReplyPacket *p;

	if (nreply_packets == 128)
		send_buffered_reply_packets ();

	p = &reply_packets [nreply_packets];
	p->id = id;
	p->error = error;
	p->data = g_new0 (Buffer, 1);
	buffer_init (p->data, buffer_len (data));
	buffer_add_buffer (p->data, data);
	nreply_packets ++;
}


/* Maps objid -> ObjRef */
/* Protected by the loader lock */
static GHashTable *objrefs;
/* Protected by the loader lock */
static GHashTable *obj_to_objref;
/* Protected by the dbg lock */
static MonoGHashTable *suspended_objs;



static void
objrefs_init (void)
{
	objrefs = g_hash_table_new_full (NULL, NULL, NULL, mono_debugger_free_objref);
	obj_to_objref = g_hash_table_new (NULL, NULL);
	suspended_objs = mono_g_hash_table_new_type_internal ((GHashFunc)mono_object_hash_internal, NULL, MONO_HASH_KEY_GC, MONO_ROOT_SOURCE_DEBUGGER, NULL, "Debugger Suspended Object Table");
}

static void
objrefs_cleanup (void)
{
	g_hash_table_destroy (objrefs);
	objrefs = NULL;
}

/*
 * Return an ObjRef for OBJ.
 */
static ObjRef*
get_objref (MonoObject *obj)
{
	ObjRef *ref;
	GSList *reflist = NULL, *l;
	int hash = 0;

	if (obj == NULL)
		return NULL;

	if (suspend_count) {
		/*
		 * Have to keep object refs created during suspensions alive for the duration of the suspension, so GCs during invokes don't collect them.
		 */
		dbg_lock ();
		mono_g_hash_table_insert_internal (suspended_objs, obj, NULL);
		dbg_unlock ();
	}

	mono_loader_lock ();
	
	/* FIXME: The tables can grow indefinitely */

	if (mono_gc_is_moving ()) {
		/*
		 * Objects can move, so use a hash table mapping hash codes to lists of
		 * ObjRef structures.
		 */
		hash = mono_object_hash_internal (obj);

		reflist = (GSList *)g_hash_table_lookup (obj_to_objref, GINT_TO_POINTER (hash));
		for (l = reflist; l; l = l->next) {
			ref = (ObjRef *)l->data;
			if (ref && mono_gchandle_get_target_internal (ref->handle) == obj) {
				mono_loader_unlock ();
				return ref;
			}
		}
	} else {
		/* Use a hash table with masked pointers to internalize object references */
		ref = (ObjRef *)g_hash_table_lookup (obj_to_objref, GINT_TO_POINTER (~((gsize)obj)));
		/* ref might refer to a different object with the same addr which was GCd */
		if (ref && mono_gchandle_get_target_internal (ref->handle) == obj) {
			mono_loader_unlock ();
			return ref;
		}
	}

	ref = g_new0 (ObjRef, 1);
	ref->id = mono_atomic_inc_i32 (&objref_id);
	ref->handle = mono_gchandle_new_weakref_internal (obj, FALSE);

	g_hash_table_insert (objrefs, GINT_TO_POINTER (ref->id), ref);

	if (mono_gc_is_moving ()) {
		reflist = g_slist_append (reflist, ref);
		g_hash_table_insert (obj_to_objref, GINT_TO_POINTER (hash), reflist);
	} else {
		g_hash_table_insert (obj_to_objref, GINT_TO_POINTER (~((gsize)obj)), ref);
	}

	mono_loader_unlock ();

	return ref;
}

static gboolean
true_pred (gpointer key, gpointer value, gpointer user_data)
{
	return TRUE;
}

static void
clear_suspended_objs (void)
{
	dbg_lock ();
	mono_g_hash_table_foreach_remove (suspended_objs, true_pred, NULL);
	dbg_unlock ();
}

static int
get_objid (MonoObject *obj)
{
	if (!obj)
		return 0;
	else
		return get_objref (obj)->id;
}

/*
 * Set OBJ to the object identified by OBJID.
 * Returns 0 or an error code if OBJID is invalid or the object has been garbage
 * collected.
 */
static ErrorCode
get_object_allow_null (int objid, MonoObject **obj)
{
	ObjRef *ref;

	if (objid == 0) {
		*obj = NULL;
		return ERR_NONE;
	}

	if (!objrefs)
		return ERR_INVALID_OBJECT;

	mono_loader_lock ();

	ref = (ObjRef *)g_hash_table_lookup (objrefs, GINT_TO_POINTER (objid));

	if (ref) {
		*obj = mono_gchandle_get_target_internal (ref->handle);
		mono_loader_unlock ();
		if (!(*obj))
			return ERR_INVALID_OBJECT;
		return ERR_NONE;
	} else {
		mono_loader_unlock ();
		return ERR_INVALID_OBJECT;
	}
}

static ErrorCode
get_object (int objid, MonoObject **obj)
{
	ErrorCode err = get_object_allow_null (objid, obj);

	if (err != ERR_NONE)
		return err;
	if (!(*obj))
		return ERR_INVALID_OBJECT;
	return ERR_NONE;
}

static int
decode_objid (guint8 *buf, guint8 **endbuf, guint8 *limit)
{
	return decode_id (buf, endbuf, limit);
}

static void
buffer_add_objid (Buffer *buf, MonoObject *o)
{
	buffer_add_id (buf, get_objid (o));
}

/*
 * IDS
 */

typedef enum {
	ID_ASSEMBLY = 0,
	ID_MODULE = 1,
	ID_TYPE = 2,
	ID_METHOD = 3,
	ID_FIELD = 4,
	ID_DOMAIN = 5,
	ID_PROPERTY = 6,
	ID_NUM
} IdType;

/*
 * Represents a runtime structure accessible to the debugger client
 */
typedef struct {
	/* Unique id used in the wire protocol */
	int id;
	/* Domain of the runtime structure, NULL if the domain was unloaded */
	MonoDomain *domain;
	union {
		gpointer val;
		MonoClass *klass;
		MonoMethod *method;
		MonoImage *image;
		MonoAssembly *assembly;
		MonoClassField *field;
		MonoDomain *domain;
		MonoProperty *property;
	} data;
} Id;

typedef struct {
	/* Maps runtime structure -> Id */
	/* Protected by the dbg lock */
	GHashTable *val_to_id [ID_NUM];
	/* Classes whose class load event has been sent */
	/* Protected by the loader lock */
	GHashTable *loaded_classes;
	/* Maps MonoClass->GPtrArray of file names */
	GHashTable *source_files;
	/* Maps source file basename -> GSList of classes */
	GHashTable *source_file_to_class;
	/* Same with ignore-case */
	GHashTable *source_file_to_class_ignorecase;
} AgentDomainInfo;

/* Maps id -> Id */
/* Protected by the dbg lock */
static GPtrArray *ids [ID_NUM];

static void
ids_init (void)
{
	int i;

	for (i = 0; i < ID_NUM; ++i)
		ids [i] = g_ptr_array_new ();
}

static void
ids_cleanup (void)
{
	int i, j;

	for (i = 0; i < ID_NUM; ++i) {
		if (ids [i]) {
			for (j = 0; j < ids [i]->len; ++j)
				g_free (g_ptr_array_index (ids [i], j));
			g_ptr_array_free (ids [i], TRUE);
		}
		ids [i] = NULL;
	}
}

static void
debugger_agent_free_domain_info (MonoDomain *domain)
{
	AgentDomainInfo *info = (AgentDomainInfo *)domain_jit_info (domain)->agent_info;
	int i, j;
	GHashTableIter iter;
	GPtrArray *file_names;
	char *basename;
	GSList *l;

	if (info) {
		for (i = 0; i < ID_NUM; ++i)
			g_hash_table_destroy (info->val_to_id [i]);
		g_hash_table_destroy (info->loaded_classes);

		g_hash_table_iter_init (&iter, info->source_files);
		while (g_hash_table_iter_next (&iter, NULL, (void**)&file_names)) {
			for (i = 0; i < file_names->len; ++i)
				g_free (g_ptr_array_index (file_names, i));
			g_ptr_array_free (file_names, TRUE);
		}

		g_hash_table_iter_init (&iter, info->source_file_to_class);
		while (g_hash_table_iter_next (&iter, (void**)&basename, (void**)&l)) {
			g_free (basename);
			g_slist_free (l);
		}

		g_hash_table_iter_init (&iter, info->source_file_to_class_ignorecase);
		while (g_hash_table_iter_next (&iter, (void**)&basename, (void**)&l)) {
			g_free (basename);
			g_slist_free (l);
		}

		g_free (info);
	}

	domain_jit_info (domain)->agent_info = NULL;

	/* Clear ids referencing structures in the domain */
	dbg_lock ();
	for (i = 0; i < ID_NUM; ++i) {
		if (ids [i]) {
			for (j = 0; j < ids [i]->len; ++j) {
				Id *id = (Id *)g_ptr_array_index (ids [i], j);
				if (id->domain == domain)
					id->domain = NULL;
			}
		}
	}
	dbg_unlock ();

	mono_de_domain_remove (domain);
}

static AgentDomainInfo*
get_agent_domain_info (MonoDomain *domain)
{
	AgentDomainInfo *info = NULL;
	MonoJitDomainInfo *jit_info = domain_jit_info (domain);

	info = (AgentDomainInfo *)jit_info->agent_info;

	if (info) {
		mono_memory_read_barrier ();
		return info;
	}

	info = g_new0 (AgentDomainInfo, 1);
	info->loaded_classes = g_hash_table_new (mono_aligned_addr_hash, NULL);
	info->source_files = g_hash_table_new (mono_aligned_addr_hash, NULL);
	info->source_file_to_class = g_hash_table_new (g_str_hash, g_str_equal);
	info->source_file_to_class_ignorecase = g_hash_table_new (g_str_hash, g_str_equal);

	mono_memory_write_barrier ();

	gpointer other_info = mono_atomic_cas_ptr (&jit_info->agent_info, info, NULL);

	if (other_info != NULL) {
		g_hash_table_destroy (info->loaded_classes);
		g_hash_table_destroy (info->source_files);
		g_hash_table_destroy (info->source_file_to_class);
		g_hash_table_destroy (info->source_file_to_class_ignorecase);
		g_free (info);
	}

	return (AgentDomainInfo *)jit_info->agent_info;
}

static int
get_id (MonoDomain *domain, IdType type, gpointer val)
{
	Id *id;
	AgentDomainInfo *info;

	if (val == NULL)
		return 0;

	info = get_agent_domain_info (domain);

	dbg_lock ();

	if (info->val_to_id [type] == NULL)
		info->val_to_id [type] = g_hash_table_new (mono_aligned_addr_hash, NULL);

	id = (Id *)g_hash_table_lookup (info->val_to_id [type], val);
	if (id) {
		dbg_unlock ();
		return id->id;
	}

	id = g_new0 (Id, 1);
	/* Reserve id 0 */
	id->id = ids [type]->len + 1;
	id->domain = domain;
	id->data.val = val;

	g_hash_table_insert (info->val_to_id [type], val, id);
	g_ptr_array_add (ids [type], id);

	dbg_unlock ();

	return id->id;
}

static gpointer
decode_ptr_id (guint8 *buf, guint8 **endbuf, guint8 *limit, IdType type, MonoDomain **domain, ErrorCode *err)
{
	Id *res;

	int id = decode_id (buf, endbuf, limit);

	*err = ERR_NONE;
	if (domain)
		*domain = NULL;

	if (id == 0)
		return NULL;

	// FIXME: error handling
	dbg_lock ();
	g_assert (id > 0 && id <= ids [type]->len);

	res = (Id *)g_ptr_array_index (ids [type], GPOINTER_TO_INT (id - 1));
	dbg_unlock ();

	if (res->domain == NULL || res->domain->state == MONO_APPDOMAIN_UNLOADED) {
		DEBUG_PRINTF (1, "ERR_UNLOADED, id=%d, type=%d.\n", id, type);
		*err = ERR_UNLOADED;
		return NULL;
	}

	if (domain)
		*domain = res->domain;

	return res->data.val;
}

static int
buffer_add_ptr_id (Buffer *buf, MonoDomain *domain, IdType type, gpointer val)
{
	int id = get_id (domain, type, val);

	buffer_add_id (buf, id);
	return id;
}

static MonoClass*
decode_typeid (guint8 *buf, guint8 **endbuf, guint8 *limit, MonoDomain **domain, ErrorCode *err)
{
	MonoClass *klass;

	klass = (MonoClass *)decode_ptr_id (buf, endbuf, limit, ID_TYPE, domain, err);
	if (G_UNLIKELY (log_level >= 2) && klass) {
		char *s;

		s = mono_type_full_name (m_class_get_byval_arg (klass));
		DEBUG_PRINTF (2, "[dbg]   recv class [%s]\n", s);
		g_free (s);
	}
	return klass;
}

static MonoAssembly*
decode_assemblyid (guint8 *buf, guint8 **endbuf, guint8 *limit, MonoDomain **domain, ErrorCode *err)
{
	return (MonoAssembly *)decode_ptr_id (buf, endbuf, limit, ID_ASSEMBLY, domain, err);
}

static MonoImage*
decode_moduleid (guint8 *buf, guint8 **endbuf, guint8 *limit, MonoDomain **domain, ErrorCode *err)
{
	return (MonoImage *)decode_ptr_id (buf, endbuf, limit, ID_MODULE, domain, err);
}

static MonoMethod*
decode_methodid (guint8 *buf, guint8 **endbuf, guint8 *limit, MonoDomain **domain, ErrorCode *err)
{
	MonoMethod *m;

	m = (MonoMethod *)decode_ptr_id (buf, endbuf, limit, ID_METHOD, domain, err);
	if (G_UNLIKELY (log_level >= 2) && m) {
		char *s;

		s = mono_method_full_name (m, TRUE);
		DEBUG_PRINTF (2, "[dbg]   recv method [%s]\n", s);
		g_free (s);
	}
	return m;
}

static MonoClassField*
decode_fieldid (guint8 *buf, guint8 **endbuf, guint8 *limit, MonoDomain **domain, ErrorCode *err)
{
	return (MonoClassField *)decode_ptr_id (buf, endbuf, limit, ID_FIELD, domain, err);
}

static MonoDomain*
decode_domainid (guint8 *buf, guint8 **endbuf, guint8 *limit, MonoDomain **domain, ErrorCode *err)
{
	return (MonoDomain *)decode_ptr_id (buf, endbuf, limit, ID_DOMAIN, domain, err);
}

static MonoProperty*
decode_propertyid (guint8 *buf, guint8 **endbuf, guint8 *limit, MonoDomain **domain, ErrorCode *err)
{
	return (MonoProperty *)decode_ptr_id (buf, endbuf, limit, ID_PROPERTY, domain, err);
}

static void
buffer_add_typeid (Buffer *buf, MonoDomain *domain, MonoClass *klass)
{
	buffer_add_ptr_id (buf, domain, ID_TYPE, klass);
	if (G_UNLIKELY (log_level >= 2) && klass) {
		char *s;

		s = mono_type_full_name (m_class_get_byval_arg (klass));
		if (is_debugger_thread ())
			DEBUG_PRINTF (2, "[dbg]   send class [%s]\n", s);
		else
			DEBUG_PRINTF (2, "[%p]   send class [%s]\n", (gpointer) (gsize) mono_native_thread_id_get (), s);
		g_free (s);
	}
}

static void
buffer_add_methodid (Buffer *buf, MonoDomain *domain, MonoMethod *method)
{
	buffer_add_ptr_id (buf, domain, ID_METHOD, method);
	if (G_UNLIKELY (log_level >= 2) && method) {
		char *s;

		s = mono_method_full_name (method, 1);
		if (is_debugger_thread ())
			DEBUG_PRINTF (2, "[dbg]   send method [%s]\n", s);
		else
			DEBUG_PRINTF (2, "[%p]   send method [%s]\n", (gpointer) (gsize) mono_native_thread_id_get (), s);
		g_free (s);
	}
}

static void
buffer_add_assemblyid (Buffer *buf, MonoDomain *domain, MonoAssembly *assembly)
{
	int id;

	id = buffer_add_ptr_id (buf, domain, ID_ASSEMBLY, assembly);
	if (G_UNLIKELY (log_level >= 2) && assembly)
		DEBUG_PRINTF (2, "[dbg]   send assembly [%s][%s][%d]\n", assembly->aname.name, domain->friendly_name, id);
}

static void
buffer_add_moduleid (Buffer *buf, MonoDomain *domain, MonoImage *image)
{
	buffer_add_ptr_id (buf, domain, ID_MODULE, image);
}

static void
buffer_add_fieldid (Buffer *buf, MonoDomain *domain, MonoClassField *field)
{
	buffer_add_ptr_id (buf, domain, ID_FIELD, field);
}

static void
buffer_add_propertyid (Buffer *buf, MonoDomain *domain, MonoProperty *property)
{
	buffer_add_ptr_id (buf, domain, ID_PROPERTY, property);
}

static void
buffer_add_domainid (Buffer *buf, MonoDomain *domain)
{
	buffer_add_ptr_id (buf, domain, ID_DOMAIN, domain);
}

static void invoke_method (void);

/*
 * SUSPEND/RESUME
 */

static MonoJitInfo*
get_top_method_ji (gpointer ip, MonoDomain **domain, gpointer *out_ip)
{
	MonoJitInfo *ji;

	if (out_ip)
		*out_ip = ip;

	ji = mini_jit_info_table_find (mono_domain_get (), (char*)ip, domain);
	if (!ji) {
		/* Could be an interpreter method */

		MonoLMF *lmf = mono_get_lmf ();
		MonoInterpFrameHandle *frame;

		g_assert (((gsize)lmf->previous_lmf) & 2);
		MonoLMFExt *ext = (MonoLMFExt*)lmf;

		g_assert (ext->kind == MONO_LMFEXT_INTERP_EXIT || ext->kind == MONO_LMFEXT_INTERP_EXIT_WITH_CTX);
		frame = (MonoInterpFrameHandle*)ext->interp_exit_data;
		ji = mini_get_interp_callbacks ()->frame_get_jit_info (frame);
		if (domain)
			*domain = mono_domain_get ();
		if (out_ip)
			*out_ip = mini_get_interp_callbacks ()->frame_get_ip (frame);
	}
	return ji;
}

/*
 * save_thread_context:
 *
 *   Set CTX as the current threads context which is used for computing stack traces.
 * This function is signal-safe.
 */
static void
save_thread_context (MonoContext *ctx)
{
	DebuggerTlsData *tls;

	tls = (DebuggerTlsData *)mono_native_tls_get_value (debugger_tls_id);
	g_assert (tls);

	if (ctx)
		mono_thread_state_init_from_monoctx (&tls->context, ctx);
	else
		mono_thread_state_init_from_current (&tls->context);
}

static MonoCoopMutex suspend_mutex;

/* Cond variable used to wait for suspend_count becoming 0 */
static MonoCoopCond suspend_cond;

/* Semaphore used to wait for a thread becoming suspended */
static MonoCoopSem suspend_sem;

static void
suspend_init (void)
{
	mono_coop_mutex_init (&suspend_mutex);
	mono_coop_cond_init (&suspend_cond);	
	mono_coop_sem_init (&suspend_sem, 0);
}

typedef struct
{
	StackFrameInfo last_frame;
	gboolean last_frame_set;
	MonoContext ctx;
	gpointer lmf;
	MonoDomain *domain;
} GetLastFrameUserData;

static gboolean
get_last_frame (StackFrameInfo *info, MonoContext *ctx, gpointer user_data)
{
	GetLastFrameUserData *data = (GetLastFrameUserData *)user_data;

	if (info->type == FRAME_TYPE_MANAGED_TO_NATIVE || info->type == FRAME_TYPE_TRAMPOLINE)
		return FALSE;

	if (!data->last_frame_set) {
		/* Store the last frame */
		memcpy (&data->last_frame, info, sizeof (StackFrameInfo));
		data->last_frame_set = TRUE;
		return FALSE;
	} else {
		/* Store the context/lmf for the frame above the last frame */
		memcpy (&data->ctx, ctx, sizeof (MonoContext));
		data->lmf = info->lmf;
		data->domain = info->domain;
		return TRUE;
	}
}

static void
copy_unwind_state_from_frame_data (MonoThreadUnwindState *to, GetLastFrameUserData *data, gpointer jit_tls)
{
	memcpy (&to->ctx, &data->ctx, sizeof (MonoContext));

	to->unwind_data [MONO_UNWIND_DATA_DOMAIN] = data->domain;
	to->unwind_data [MONO_UNWIND_DATA_LMF] = data->lmf;
	to->unwind_data [MONO_UNWIND_DATA_JIT_TLS] = jit_tls;
	to->valid = TRUE;
}

/*
 * thread_interrupt:
 *
 *   Process interruption of a thread. This should be signal safe.
 *
 * This always runs in the debugger thread.
 */
static void
thread_interrupt (DebuggerTlsData *tls, MonoThreadInfo *info, MonoJitInfo *ji)
{
	gpointer ip;
	MonoNativeThreadId tid;

	g_assert (info);

	ip = MONO_CONTEXT_GET_IP (&mono_thread_info_get_suspend_state (info)->ctx);
	tid = mono_thread_info_get_tid (info);

	// FIXME: Races when the thread leaves managed code before hitting a single step
	// event.

	if (ji && !ji->is_trampoline) {
		/* Running managed code, will be suspended by the single step code */
		DEBUG_PRINTF (1, "[%p] Received interrupt while at %s(%p), continuing.\n", (gpointer)(gsize)tid, jinfo_get_method (ji)->name, ip);
	} else {
		/* 
		 * Running native code, will be suspended when it returns to/enters 
		 * managed code. Treat it as already suspended.
		 * This might interrupt the code in mono_de_process_single_step (), we use the
		 * tls->suspending flag to avoid races when that happens.
		 */
		if (!tls->suspended && !tls->suspending) {
			GetLastFrameUserData data;

			// FIXME: printf is not signal safe, but this is only used during
			// debugger debugging
			if (ip)
				DEBUG_PRINTF (1, "[%p] Received interrupt while at %p, treating as suspended.\n", (gpointer)(gsize)tid, ip);
			//save_thread_context (&ctx);

			if (!tls->thread)
				/* Already terminated */
				return;

			/*
			 * We are in a difficult position: we want to be able to provide stack
			 * traces for this thread, but we can't use the current ctx+lmf, since
			 * the thread is still running, so it might return to managed code,
			 * making these invalid.
			 * So we start a stack walk and save the first frame, along with the
			 * parent frame's ctx+lmf. This (hopefully) works because the thread will be 
			 * suspended when it returns to managed code, so the parent's ctx should
			 * remain valid.
			 */
			MonoThreadUnwindState *state = mono_thread_info_get_suspend_state (info);

			data.last_frame_set = FALSE;
			mono_get_eh_callbacks ()->mono_walk_stack_with_state (get_last_frame, state, MONO_UNWIND_SIGNAL_SAFE, &data);
			if (data.last_frame_set) {
				gpointer jit_tls = tls->thread->thread_info->jit_data;

				memcpy (&tls->async_last_frame, &data.last_frame, sizeof (StackFrameInfo));

				if (data.last_frame.type == FRAME_TYPE_INTERP_TO_MANAGED || data.last_frame.type == FRAME_TYPE_INTERP_TO_MANAGED_WITH_CTX) {
					/*
					 * Store the current lmf instead of the parent one, since that
					 * contains the interp exit data.
					 */
					data.lmf = state->unwind_data [MONO_UNWIND_DATA_LMF];
				}

				copy_unwind_state_from_frame_data (&tls->async_state, &data, jit_tls);
				/* Don't set tls->context, it could race with the thread processing a breakpoint etc. */
			} else {
				tls->async_state.valid = FALSE;
			}

			mono_memory_barrier ();

			tls->suspended = TRUE;
			mono_coop_sem_post (&suspend_sem);
		}
	}
}

/*
 * reset_native_thread_suspend_state:
 * 
 *   Reset the suspended flag and state on native threads
 */
static void
reset_native_thread_suspend_state (gpointer key, gpointer value, gpointer user_data)
{
	DebuggerTlsData *tls = (DebuggerTlsData *)value;

	if (!tls->really_suspended && tls->suspended) {
		tls->suspended = FALSE;
		/*
		 * The thread might still be running if it was executing native code, so the state won't be invalided by
		 * suspend_current ().
		 */
		tls->context.valid = FALSE;
		tls->async_state.valid = FALSE;
		invalidate_frames (tls);
	}
	tls->resume_count_internal++;

}

typedef struct {
	DebuggerTlsData *tls;
	gboolean valid_info;
} InterruptData;

static SuspendThreadResult
debugger_interrupt_critical (MonoThreadInfo *info, gpointer user_data)
{
	InterruptData *data = (InterruptData *)user_data;
	MonoJitInfo *ji;

	data->valid_info = TRUE;
	MonoDomain *domain = (MonoDomain *) mono_thread_info_get_suspend_state (info)->unwind_data [MONO_UNWIND_DATA_DOMAIN];
	if (!domain) {
		/* not attached */
		ji = NULL;
	} else {
		ji = mono_jit_info_table_find_internal ( domain, MONO_CONTEXT_GET_IP (&mono_thread_info_get_suspend_state (info)->ctx), TRUE, TRUE);
	}

	/* This is signal safe */
	thread_interrupt (data->tls, info, ji);
	return MonoResumeThread;
}

/*
 * notify_thread:
 *
 *   Notify a thread that it needs to suspend.
 */
static void
notify_thread (gpointer key, gpointer value, gpointer user_data)
{
	MonoInternalThread *thread = (MonoInternalThread *)key;
	DebuggerTlsData *tls = (DebuggerTlsData *)value;
	MonoNativeThreadId tid = MONO_UINT_TO_NATIVE_THREAD_ID (thread->tid);

	if (mono_thread_internal_is_current (thread) || tls->terminated)
		return;

	DEBUG_PRINTF (1, "[%p] Interrupting %p...\n", (gpointer) (gsize) mono_native_thread_id_get (), (gpointer)(gsize)tid);

	/* This is _not_ equivalent to mono_thread_internal_abort () */
	InterruptData interrupt_data = { 0 };
	interrupt_data.tls = tls;

	mono_thread_info_safe_suspend_and_run ((MonoNativeThreadId)(gsize)thread->tid, FALSE, debugger_interrupt_critical, &interrupt_data);
	if (!interrupt_data.valid_info) {
		DEBUG_PRINTF (1, "[%p] mono_thread_info_suspend_sync () failed for %p...\n", (gpointer) (gsize) mono_native_thread_id_get (), (gpointer)tid);
		/* 
		 * Attached thread which died without detaching.
		 */
		tls->terminated = TRUE;
	}
}

static void
process_suspend (DebuggerTlsData *tls, MonoContext *ctx)
{
	guint8 *ip = (guint8 *)MONO_CONTEXT_GET_IP (ctx);
	MonoJitInfo *ji;
	MonoMethod *method;

	if (mono_loader_lock_is_owned_by_self ()) {
		/*
		 * Shortcut for the check in suspend_current (). This speeds up processing
		 * when executing long running code inside the loader lock, i.e. assembly load
		 * hooks.
		 */
		return;
	}

	if (is_debugger_thread ())
		return;

	/* Prevent races with mono_debugger_agent_thread_interrupt () */
	if (suspend_count - tls->resume_count > 0)
		tls->suspending = TRUE;

	DEBUG_PRINTF (1, "[%p] Received single step event for suspending.\n", (gpointer) (gsize) mono_native_thread_id_get ());

	if (suspend_count - tls->resume_count == 0) {
		/* 
		 * We are executing a single threaded invoke but the single step for 
		 * suspending is still active.
		 * FIXME: This slows down single threaded invokes.
		 */
		DEBUG_PRINTF (1, "[%p] Ignored during single threaded invoke.\n", (gpointer) (gsize) mono_native_thread_id_get ());
		return;
	}

	ji = get_top_method_ji (ip, NULL, NULL);
	g_assert (ji);
	/* Can't suspend in these methods */
	method = jinfo_get_method (ji);
	if (method->klass == mono_defaults.string_class && (!strcmp (method->name, "memset") || strstr (method->name, "memcpy")))
		return;

	save_thread_context (ctx);

	suspend_current ();
}


/* Conditionally call process_suspend depending oh the current state */
static gboolean
try_process_suspend (void *the_tls, MonoContext *ctx, gboolean from_breakpoint)
{
	DebuggerTlsData *tls = (DebuggerTlsData*)the_tls;
	/* if there is a suspend pending that is not executed yes */
	if (suspend_count > 0) { 
		/* Fastpath during invokes, see in process_suspend () */
		/* if there is a suspend pending but this thread is already resumed, we shouldn't suspend it again and the breakpoint/ss can run */
		if (suspend_count - tls->resume_count == 0) 
			return FALSE;
		/* if there is in a invoke the breakpoint/step should be executed even with the suspend pending */
		if (tls->invoke) 
			return FALSE;
		/* with the multithreaded single step check if there is a suspend_count pending in the current thread and not in the vm */
		if (from_breakpoint && tls->suspend_count <= tls->resume_count_internal)
			return FALSE;
		process_suspend (tls, ctx);
		return TRUE;
	} /* if there isn't any suspend pending, the breakpoint/ss will be executed and will suspend then vm when the event is sent */
	return FALSE;
}

/*
 * suspend_vm:
 *
 * Increase the suspend count of the VM. While the suspend count is greater 
 * than 0, runtime threads are suspended at certain points during execution.
 */
static void
suspend_vm (void)
{
	gboolean tp_suspend = FALSE;
	mono_loader_lock ();

	mono_coop_mutex_lock (&suspend_mutex);

	suspend_count ++;

	DEBUG_PRINTF (1, "[%p] Suspending vm...\n", (gpointer) (gsize) mono_native_thread_id_get ());

	if (suspend_count == 1) {
		// FIXME: Is it safe to call this inside the lock ?
		mono_de_start_single_stepping ();
		mono_g_hash_table_foreach (thread_to_tls, notify_thread, NULL);
	}

	mono_coop_mutex_unlock (&suspend_mutex);

	if (suspend_count == 1)
		/*
		 * Suspend creation of new threadpool threads, since they cannot run
		 */
		tp_suspend = TRUE;
	mono_loader_unlock ();

#ifndef ENABLE_NETCORE
	if (tp_suspend)
		mono_threadpool_suspend ();
#endif
}

/*
 * resume_vm:
 *
 * Decrease the suspend count of the VM. If the count reaches 0, runtime threads
 * are resumed.
 */
static void
resume_vm (void)
{
	g_assert (is_debugger_thread ());
	gboolean tp_resume = FALSE;

	mono_loader_lock ();

	mono_coop_mutex_lock (&suspend_mutex);

	g_assert (suspend_count > 0);
	suspend_count --;

	DEBUG_PRINTF (1, "[%p] Resuming vm, suspend count=%d...\n", (gpointer) (gsize) mono_native_thread_id_get (), suspend_count);

	if (suspend_count == 0) {
		// FIXME: Is it safe to call this inside the lock ?
		mono_de_stop_single_stepping ();
		mono_g_hash_table_foreach (thread_to_tls, reset_native_thread_suspend_state, NULL);
	}

	/* Signal this even when suspend_count > 0, since some threads might have resume_count > 0 */
	mono_coop_cond_broadcast (&suspend_cond);

	mono_coop_mutex_unlock (&suspend_mutex);
	//g_assert (err == 0);

	if (suspend_count == 0)
		tp_resume = TRUE;
	mono_loader_unlock ();

#ifndef ENABLE_NETCORE
	if (tp_resume)
		mono_threadpool_resume ();
#endif
}

/*
 * resume_thread:
 *
 *   Resume just one thread.
 */
static void
resume_thread (MonoInternalThread *thread)
{
	DebuggerTlsData *tls;

	g_assert (is_debugger_thread ());

	mono_loader_lock ();

	tls = (DebuggerTlsData *)mono_g_hash_table_lookup (thread_to_tls, thread);
	g_assert (tls);

	mono_coop_mutex_lock (&suspend_mutex);

	g_assert (suspend_count > 0);

	DEBUG_PRINTF (1, "[sdb] Resuming thread %p...\n", (gpointer)(gssize)thread->tid);

	tls->resume_count += suspend_count;
	tls->resume_count_internal += tls->suspend_count;
	tls->suspend_count = 0;

	/* 
	 * Signal suspend_count without decreasing suspend_count, the threads will wake up
	 * but only the one whose resume_count field is > 0 will be resumed.
	 */
	mono_coop_cond_broadcast (&suspend_cond);

	mono_coop_mutex_unlock (&suspend_mutex);
	//g_assert (err == 0);

	mono_loader_unlock ();
}

static void
free_frames (StackFrame **frames, int nframes)
{
	int i;

	for (i = 0; i < nframes; ++i) {
		if (frames [i]->jit)
			mono_debug_free_method_jit_info (frames [i]->jit);
		g_free (frames [i]);
	}
	g_free (frames);
}

static void
invalidate_frames (DebuggerTlsData *tls)
{
	mono_loader_lock ();

	if (!tls)
		tls = (DebuggerTlsData *)mono_native_tls_get_value (debugger_tls_id);
	g_assert (tls);

	free_frames (tls->frames, tls->frame_count);
	tls->frame_count = 0;
	tls->frames = NULL;

	free_frames (tls->restore_frames, tls->restore_frame_count);
	tls->restore_frame_count = 0;
	tls->restore_frames = NULL;

	mono_loader_unlock ();
}

/*
 * suspend_current:
 *
 *   Suspend the current thread until the runtime is resumed. If the thread has a 
 * pending invoke, then the invoke is executed before this function returns. 
 */
static void
suspend_current (void)
{
	DebuggerTlsData *tls;

	g_assert (!is_debugger_thread ());

	if (mono_loader_lock_is_owned_by_self ()) {
		/*
		 * If we own the loader mutex, can't suspend until we release it, since the
		 * whole runtime can deadlock otherwise.
		 */
		return;
	}

 	tls = (DebuggerTlsData *)mono_native_tls_get_value (debugger_tls_id);
	g_assert (tls);

	gboolean do_resume = FALSE;
	while (!do_resume) {
		mono_coop_mutex_lock (&suspend_mutex);

		tls->suspending = FALSE;
		tls->really_suspended = TRUE;

		if (!tls->suspended) {
			tls->suspended = TRUE;
			mono_coop_sem_post (&suspend_sem);
		}

		mono_debugger_log_suspend (tls);
		DEBUG_PRINTF (1, "[%p] Suspended.\n", (gpointer) (gsize) mono_native_thread_id_get ());

		while (suspend_count - tls->resume_count > 0) {
			mono_coop_cond_wait (&suspend_cond, &suspend_mutex);
		}

		tls->suspended = FALSE;
		tls->really_suspended = FALSE;

		mono_coop_mutex_unlock (&suspend_mutex);

		mono_debugger_log_resume (tls);
		DEBUG_PRINTF (1, "[%p] Resumed.\n", (gpointer) (gsize) mono_native_thread_id_get ());

		if (tls->pending_invoke) {
			/* Save the original context */
			tls->pending_invoke->has_ctx = TRUE;
			tls->pending_invoke->ctx = tls->context.ctx;

			invoke_method ();

			/* Have to suspend again */
		} else {
			do_resume = TRUE;
		}
	}

	/* The frame info becomes invalid after a resume */
	tls->context.valid = FALSE;
	tls->async_state.valid = FALSE;
	invalidate_frames (tls);
	mono_stopwatch_start (&tls->step_time);
}

static void
count_thread (gpointer key, gpointer value, gpointer user_data)
{
	DebuggerTlsData *tls = (DebuggerTlsData *)value;

	if (!tls->suspended && !tls->terminated && !mono_thread_internal_is_current (tls->thread))
		*(int*)user_data = *(int*)user_data + 1;
}

static int
count_threads_to_wait_for (void)
{
	int count = 0;

	mono_loader_lock ();
	mono_g_hash_table_foreach (thread_to_tls, count_thread, &count);
	mono_loader_unlock ();

	return count;
}	

/*
 * wait_for_suspend:
 *
 *   Wait until the runtime is completely suspended.
 */
static void
wait_for_suspend (void)
{
	int nthreads, nwait, err;
	gboolean waited = FALSE;

	// FIXME: Threads starting/stopping ?
	mono_loader_lock ();
	nthreads = mono_g_hash_table_size (thread_to_tls);
	mono_loader_unlock ();

	while (TRUE) {
		nwait = count_threads_to_wait_for ();
		if (nwait) {
			DEBUG_PRINTF (1, "Waiting for %d(%d) threads to suspend...\n", nwait, nthreads);
			err = mono_coop_sem_wait (&suspend_sem, MONO_SEM_FLAGS_NONE);
			g_assert (err == 0);
			waited = TRUE;
		} else {
			break;
		}
	}

	if (waited)
		DEBUG_PRINTF (1, "%d threads suspended.\n", nthreads);
}

/*
 * is_suspended:
 *
 *   Return whenever the runtime is suspended.
 */
static gboolean
is_suspended (void)
{
	return count_threads_to_wait_for () == 0;
}

static void
no_seq_points_found (MonoMethod *method, int offset)
{
	/*
	 * This can happen in full-aot mode with assemblies AOTed without the 'soft-debug' option to save space.
	 */
	printf ("Unable to find seq points for method '%s', offset 0x%x.\n", mono_method_full_name (method, TRUE), offset);
}

static int
calc_il_offset (MonoDomain *domain, MonoMethod *method, int native_offset, gboolean is_top_frame)
{
	int ret = -1;
	if (is_top_frame) {
		SeqPoint sp;
		/* mono_debug_il_offset_from_address () doesn't seem to be precise enough (#2092) */
		if (mono_find_prev_seq_point_for_native_offset (domain, method, native_offset, NULL, &sp))
			ret = sp.il_offset;
	}
	if (ret == -1)
		ret = mono_debug_il_offset_from_address (method, domain, native_offset);
	return ret;
}

typedef struct {
	DebuggerTlsData *tls;
	GSList *frames;
	gboolean set_debugger_flag;
} ComputeFramesUserData;

static gboolean
process_frame (StackFrameInfo *info, MonoContext *ctx, gpointer user_data)
{
	ComputeFramesUserData *ud = (ComputeFramesUserData *)user_data;
	StackFrame *frame;
	MonoMethod *method, *actual_method, *api_method;
	int flags = 0;

	mono_loader_lock ();
	if (info->type != FRAME_TYPE_MANAGED && info->type != FRAME_TYPE_INTERP && info->type != FRAME_TYPE_MANAGED_TO_NATIVE) {
		if (info->type == FRAME_TYPE_DEBUGGER_INVOKE) {
			/* Mark the last frame as an invoke frame */
			if (ud->frames)
				((StackFrame*)g_slist_last (ud->frames)->data)->flags |= FRAME_FLAG_DEBUGGER_INVOKE;
			else
				ud->set_debugger_flag = TRUE;
		}
		mono_loader_unlock ();
		return FALSE;
	}

	if (info->ji)
		method = jinfo_get_method (info->ji);
	else
		method = info->method;
	actual_method = info->actual_method;
	api_method = method;

	if (!method) {
		mono_loader_unlock ();
		return FALSE;
	}

	if (!method || (method->wrapper_type && method->wrapper_type != MONO_WRAPPER_DYNAMIC_METHOD && method->wrapper_type != MONO_WRAPPER_MANAGED_TO_NATIVE)) {
		mono_loader_unlock ();
		return FALSE;
	}

	if (info->il_offset == -1) {
		info->il_offset = calc_il_offset (info->domain, method, info->native_offset, ud->frames == NULL);
	}

	DEBUG_PRINTF (1, "\tFrame: %s:[il=0x%x, native=0x%x] %d\n", mono_method_full_name (method, TRUE), info->il_offset, info->native_offset, info->managed);

	if (method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE) {
		if (!CHECK_PROTOCOL_VERSION (2, 17)) {
			/* Older clients can't handle this flag */
			mono_loader_unlock ();
			return FALSE;
		}
		api_method = mono_marshal_method_from_wrapper (method);
		if (!api_method) {
			mono_loader_unlock ();
			return FALSE;
		}
		actual_method = api_method;
		flags |= FRAME_FLAG_NATIVE_TRANSITION;
	}

	if (ud->set_debugger_flag) {
		g_assert (g_slist_length (ud->frames) == 0);
		flags |= FRAME_FLAG_DEBUGGER_INVOKE;
		ud->set_debugger_flag = FALSE;
	}

	frame = g_new0 (StackFrame, 1);
	frame->de.ji = info->ji;
	frame->de.domain = info->domain;
	frame->de.method = method;
	frame->de.native_offset = info->native_offset;

	frame->actual_method = actual_method;
	frame->api_method = api_method;
	frame->il_offset = info->il_offset;
	frame->flags = flags;
	frame->interp_frame = info->interp_frame;
	frame->frame_addr = info->frame_addr;
	if (info->reg_locations)
		memcpy (frame->reg_locations, info->reg_locations, MONO_MAX_IREGS * sizeof (host_mgreg_t*));
	if (ctx) {
		frame->ctx = *ctx;
		frame->has_ctx = TRUE;
	}

	ud->frames = g_slist_append (ud->frames, frame);

	mono_loader_unlock ();
	return FALSE;
}

static gint32 isFixedSizeArray (MonoClassField *f)
{
	ERROR_DECL (error);
	if (!CHECK_PROTOCOL_VERSION (2, 53) || f->type->type != MONO_TYPE_VALUETYPE) {
		return 1;
	}
	MonoCustomAttrInfo *cinfo;
	MonoCustomAttrEntry *attr;
	int aindex;
	gint32 ret = 1;
	cinfo = mono_custom_attrs_from_field_checked (f->parent, f, error);
	goto_if_nok (error, leave);
	attr = NULL;
	if (cinfo) {
		for (aindex = 0; aindex < cinfo->num_attrs; ++aindex) {
			MonoClass *ctor_class = cinfo->attrs [aindex].ctor->klass;
			MonoClass *fixed_size_class = mono_class_try_get_fixed_buffer_class ();
			if (fixed_size_class != NULL && mono_class_has_parent (ctor_class, fixed_size_class)) {
				attr = &cinfo->attrs [aindex];
				gpointer *typed_args, *named_args;
				CattrNamedArg *arginfo;
				int num_named_args;

				mono_reflection_create_custom_attr_data_args_noalloc (mono_defaults.corlib, attr->ctor, attr->data, attr->data_size,
																	&typed_args, &named_args, &num_named_args, &arginfo, error);
				if (!is_ok (error)) {
					ret = 0;
					goto leave;
				}
				ret = *(gint32*)typed_args [1];
				g_free (typed_args [1]);
				g_free (typed_args);
				g_free (named_args);
				g_free (arginfo);
				return ret;
			}
		}
	}
leave:
	mono_error_cleanup (error);
	return ret;
}

static gboolean
process_filter_frame (StackFrameInfo *info, MonoContext *ctx, gpointer user_data)
{
	ComputeFramesUserData *ud = (ComputeFramesUserData *)user_data;

	/*
	 * 'tls->filter_ctx' is the location of the throw site.
	 *
	 * mono_walk_stack() will never actually hit the throw site, but unwind
	 * directly from the filter to the call site; we abort stack unwinding here
	 * once this happens and resume from the throw site.
	 */
	if (info->frame_addr >= MONO_CONTEXT_GET_SP (&ud->tls->filter_state.ctx))
		return TRUE;

	return process_frame (info, ctx, user_data);
}

/*
 * Return a malloc-ed list of StackFrame structures.
 */
static StackFrame**
compute_frame_info_from (MonoInternalThread *thread, DebuggerTlsData *tls, MonoThreadUnwindState *state, int *out_nframes)
{
	ComputeFramesUserData user_data;
	MonoUnwindOptions opts = (MonoUnwindOptions)(MONO_UNWIND_DEFAULT | MONO_UNWIND_REG_LOCATIONS);
	StackFrame **res;
	int i, nframes;
	GSList *l;

	user_data.tls = tls;
	user_data.frames = NULL;

	mono_walk_stack_with_state (process_frame, state, opts, &user_data);

	nframes = g_slist_length (user_data.frames);
	res = g_new0 (StackFrame*, nframes);
	l = user_data.frames;
	for (i = 0; i < nframes; ++i) {
		res [i] = (StackFrame *)l->data;
		l = l->next;
	}
	*out_nframes = nframes;

	return res;
}

static void
compute_frame_info (MonoInternalThread *thread, DebuggerTlsData *tls, gboolean force_update)
{
	ComputeFramesUserData user_data;
	GSList *tmp;
	int i, findex, new_frame_count;
	StackFrame **new_frames, *f;
	MonoUnwindOptions opts = (MonoUnwindOptions)(MONO_UNWIND_DEFAULT | MONO_UNWIND_REG_LOCATIONS);

	// FIXME: Locking on tls
	if (tls->frames && tls->frames_up_to_date && !force_update)
		return;

	DEBUG_PRINTF (1, "Frames for %p(tid=%lx):\n", thread, (glong)thread->tid);

	if (CHECK_PROTOCOL_VERSION (2, 52)) {
		if (tls->restore_state.valid && MONO_CONTEXT_GET_IP (&tls->context.ctx) != MONO_CONTEXT_GET_IP (&tls->restore_state.ctx)) {
			new_frames = compute_frame_info_from (thread, tls, &tls->restore_state, &new_frame_count);
			invalidate_frames (tls);

			tls->frames = new_frames;
			tls->frame_count = new_frame_count;
			tls->frames_up_to_date = TRUE;
			return;
		}
	}

	user_data.tls = tls;
	user_data.frames = NULL;
	if (tls->terminated) {
		tls->frame_count = 0;
		return;
	} if (!tls->really_suspended && tls->async_state.valid) {
		/* Have to use the state saved by the signal handler */
		process_frame (&tls->async_last_frame, NULL, &user_data);
		mono_walk_stack_with_state (process_frame, &tls->async_state, opts, &user_data);
	} else if (tls->filter_state.valid) {
		/*
		 * We are inside an exception filter.
		 *
		 * First we add all the frames from inside the filter; 'tls->ctx' has the current context.
		 */
		if (tls->context.valid) {
			mono_walk_stack_with_state (process_filter_frame, &tls->context, opts, &user_data);
			DEBUG_PRINTF (1, "\tFrame: <call filter>\n");
		}
		/*
		 * After that, we resume unwinding from the location where the exception has been thrown.
		 */
		mono_walk_stack_with_state (process_frame, &tls->filter_state, opts, &user_data);
	} else if (tls->context.valid) {
		mono_walk_stack_with_state (process_frame, &tls->context, opts, &user_data);
	} else {
		// FIXME:
		tls->frame_count = 0;
		return;
	}

	new_frame_count = g_slist_length (user_data.frames);
	new_frames = g_new0 (StackFrame*, new_frame_count);
	findex = 0;
	for (tmp = user_data.frames; tmp; tmp = tmp->next) {
		f = (StackFrame *)tmp->data;

		/* 
		 * Reuse the id for already existing stack frames, so invokes don't invalidate
		 * the still valid stack frames.
		 */
		for (i = 0; i < tls->frame_count; ++i) {
			if (tls->frames [i]->frame_addr == f->frame_addr) {
				f->id = tls->frames [i]->id;
				break;
			}
		}

		if (i >= tls->frame_count)
			f->id = mono_atomic_inc_i32 (&frame_id);

		new_frames [findex ++] = f;
	}

	g_slist_free (user_data.frames);

	invalidate_frames (tls);

	tls->frames = new_frames;
	tls->frame_count = new_frame_count;
	tls->frames_up_to_date = TRUE;

	if (CHECK_PROTOCOL_VERSION (2, 52)) {
		MonoJitTlsData *jit_data = thread->thread_info->jit_data;
		gboolean has_interp_resume_state = FALSE;
		MonoInterpFrameHandle interp_resume_frame = NULL;
		gpointer interp_resume_ip = 0;
		mini_get_interp_callbacks ()->get_resume_state (jit_data, &has_interp_resume_state, &interp_resume_frame, &interp_resume_ip);
		if (has_interp_resume_state && tls->frame_count > 0) {
			StackFrame *top_frame = tls->frames [0];
			if (interp_resume_frame == top_frame->interp_frame) {
				int native_offset = (int) ((uintptr_t) interp_resume_ip - (uintptr_t) top_frame->de.ji->code_start);
				top_frame->il_offset = calc_il_offset (top_frame->de.domain, top_frame->de.method, native_offset, TRUE);
			}
		}
	}
}

/*
 * GHFunc to emit an appdomain creation event
 * @param key Don't care
 * @param value A loaded appdomain
 * @param user_data Don't care
 */
static void
emit_appdomain_load (gpointer key, gpointer value, gpointer user_data)
{
	process_profiler_event (EVENT_KIND_APPDOMAIN_CREATE, value);
	g_hash_table_foreach (get_agent_domain_info ((MonoDomain *)value)->loaded_classes, emit_type_load, NULL);
}

/*
 * GHFunc to emit a thread start event
 * @param key A thread id
 * @param value A thread object
 * @param user_data Don't care
 */
static void
emit_thread_start (gpointer key, gpointer value, gpointer user_data)
{
	g_assert (!mono_native_thread_id_equals (MONO_UINT_TO_NATIVE_THREAD_ID (GPOINTER_TO_UINT (key)), debugger_thread_id));
	process_profiler_event (EVENT_KIND_THREAD_START, value);
}

/*
 * GFunc to emit an assembly load event
 * @param value A loaded assembly
 * @param user_data Don't care
 */
static void
emit_assembly_load (gpointer value, gpointer user_data)
{
	process_profiler_event (EVENT_KIND_ASSEMBLY_LOAD, value);
}

/*
 * GFunc to emit a type load event
 * @param value A loaded type
 * @param user_data Don't care
 */
static void
emit_type_load (gpointer key, gpointer value, gpointer user_data)
{
	process_profiler_event (EVENT_KIND_TYPE_LOAD, value);
}


static void gc_finalizing (MonoProfiler *prof)
{
	DebuggerTlsData *tls;

	if (is_debugger_thread ())
		return;

	tls = (DebuggerTlsData *)mono_native_tls_get_value (debugger_tls_id);
	g_assert (tls);
	tls->gc_finalizing = TRUE;
}

static void gc_finalized (MonoProfiler *prof)
{
	DebuggerTlsData *tls;

	if (is_debugger_thread ())
		return;

	tls = (DebuggerTlsData *)mono_native_tls_get_value (debugger_tls_id);
	g_assert (tls);
	tls->gc_finalizing = FALSE;
}


static char*
strdup_tolower (char *s)
{
	char *s2, *p;

	s2 = g_strdup (s);
	for (p = s2; *p; ++p)
		*p = tolower (*p);
	return s2;
}

/*
 * Same as g_path_get_basename () but handles windows paths as well,
 * which can occur in .mdb files created by pdb2mdb.
 */
static char*
dbg_path_get_basename (const char *filename)
{
	char *r;

	if (!filename || strchr (filename, '/') || !strchr (filename, '\\'))
		return g_path_get_basename (filename);

	/* From gpath.c */

	/* No separator -> filename */
	r = (char*)strrchr (filename, '\\');
	if (r == NULL)
		return g_strdup (filename);

	/* Trailing slash, remove component */
	if (r [1] == 0){
		char *copy = g_strdup (filename);
		copy [r-filename] = 0;
		r = strrchr (copy, '\\');

		if (r == NULL){
			g_free (copy);
			return g_strdup ("/");
		}
		r = g_strdup (&r[1]);
		g_free (copy);
		return r;
	}

	return g_strdup (&r[1]);
}

static GENERATE_TRY_GET_CLASS_WITH_CACHE(hidden_klass, "System.Diagnostics", "DebuggerHiddenAttribute")
static GENERATE_TRY_GET_CLASS_WITH_CACHE(step_through_klass, "System.Diagnostics", "DebuggerStepThroughAttribute")
static GENERATE_TRY_GET_CLASS_WITH_CACHE(non_user_klass, "System.Diagnostics", "DebuggerNonUserCodeAttribute")

static void
init_jit_info_dbg_attrs (MonoJitInfo *ji)
{
	ERROR_DECL (error);
	MonoCustomAttrInfo *ainfo;

	if (ji->dbg_attrs_inited)
		return;

	// NOTE: The following Debugger attributes may not exist if they are trimmed away by the ILLinker
	MonoClass *hidden_klass = mono_class_try_get_hidden_klass_class ();
	MonoClass *step_through_klass = mono_class_try_get_step_through_klass_class ();
	MonoClass *non_user_klass = mono_class_try_get_non_user_klass_class ();

	ainfo = mono_custom_attrs_from_method_checked (jinfo_get_method (ji), error);
	mono_error_cleanup (error); /* FIXME don't swallow the error? */
	if (ainfo) {
		if (hidden_klass && mono_custom_attrs_has_attr (ainfo, hidden_klass))
			ji->dbg_hidden = TRUE;
		if (step_through_klass && mono_custom_attrs_has_attr (ainfo, step_through_klass))
			ji->dbg_step_through = TRUE;
		if (non_user_klass && mono_custom_attrs_has_attr (ainfo, non_user_klass))
			ji->dbg_non_user_code = TRUE;
		mono_custom_attrs_free (ainfo);
	}

	ainfo = mono_custom_attrs_from_class_checked (jinfo_get_method (ji)->klass, error);
	mono_error_cleanup (error); /* FIXME don't swallow the error? */
	if (ainfo) {
		if (step_through_klass && mono_custom_attrs_has_attr (ainfo, step_through_klass))
			ji->dbg_step_through = TRUE;
		if (non_user_klass && mono_custom_attrs_has_attr (ainfo, non_user_klass))
			ji->dbg_non_user_code = TRUE;
		mono_custom_attrs_free (ainfo);
	}

	mono_memory_barrier ();
	ji->dbg_attrs_inited = TRUE;
}

/*
 * EVENT HANDLING
 */

/*
 * create_event_list:
 *
 *   Return a list of event request ids matching EVENT, starting from REQS, which
 * can be NULL to include all event requests. Set SUSPEND_POLICY to the suspend
 * policy.
 * We return request ids, instead of requests, to simplify threading, since 
 * requests could be deleted anytime when the loader lock is not held.
 * LOCKING: Assumes the loader lock is held.
 */
static GSList*
create_event_list (EventKind event, GPtrArray *reqs, MonoJitInfo *ji, EventInfo *ei, int *suspend_policy)
{
	int i, j;
	GSList *events = NULL;

	*suspend_policy = SUSPEND_POLICY_NONE;

	if (!reqs)
		reqs = event_requests;

	if (!reqs)
		return NULL;
	gboolean has_everything_else = FALSE;
	gboolean is_new_filtered_exception = FALSE;
	gboolean filteredException = TRUE;
	gint filtered_suspend_policy = 0;
	gint filtered_req_id = 0;
	gint everything_else_suspend_policy = 0;
	gint everything_else_req_id = 0;
	gboolean is_already_filtered = FALSE;
	for (i = 0; i < reqs->len; ++i) {
		EventRequest *req = (EventRequest *)g_ptr_array_index (reqs, i);
		if (req->event_kind == event) {
			gboolean filtered = FALSE;

			/* Apply filters */
			for (j = 0; j < req->nmodifiers; ++j) {
				Modifier *mod = &req->modifiers [j];

				if (mod->kind == MOD_KIND_COUNT) {
					filtered = TRUE;
					if (mod->data.count > 0) {
						if (mod->data.count > 0) {
							mod->data.count --;
							if (mod->data.count == 0)
								filtered = FALSE;
						}
					}
				} else if (mod->kind == MOD_KIND_THREAD_ONLY) {
					if (mod->data.thread != mono_thread_internal_current ())
						filtered = TRUE;
				} else if (mod->kind == MOD_KIND_EXCEPTION_ONLY && !mod->not_filtered_feature && ei) {
					if (mod->data.exc_class && mod->subclasses && !mono_class_is_assignable_from_internal (mod->data.exc_class, ei->exc->vtable->klass))
						filtered = TRUE;
					if (mod->data.exc_class && !mod->subclasses && mod->data.exc_class != ei->exc->vtable->klass)
						filtered = TRUE;
					if (ei->caught && !mod->caught)
						filtered = TRUE;
					if (!ei->caught && !mod->uncaught)
						filtered = TRUE;
				} else if (mod->kind == MOD_KIND_EXCEPTION_ONLY && mod->not_filtered_feature && ei) {
					is_new_filtered_exception = TRUE;
					if ((mod->data.exc_class && mod->subclasses && mono_class_is_assignable_from_internal (mod->data.exc_class, ei->exc->vtable->klass)) ||
					    (mod->data.exc_class && !mod->subclasses && mod->data.exc_class != ei->exc->vtable->klass)) {
						is_already_filtered = TRUE;
						if ((ei->caught && mod->caught) || (!ei->caught && mod->uncaught)) {
							filteredException = FALSE; 
							filtered_suspend_policy = req->suspend_policy;
							filtered_req_id = req->id;
						}
					}
					if (!mod->data.exc_class && mod->everything_else) {
						if ((ei->caught && mod->caught) || (!ei->caught && mod->uncaught)) {
							has_everything_else = TRUE;
							everything_else_req_id = req->id;
							everything_else_suspend_policy = req->suspend_policy;
						}
					}
					if (!mod->data.exc_class && !mod->everything_else) {
						if ((ei->caught && mod->caught) || (!ei->caught && mod->uncaught)) {
							filteredException = FALSE; 
							filtered_suspend_policy = req->suspend_policy;
							filtered_req_id = req->id;
						}
					}
				} else if (mod->kind == MOD_KIND_ASSEMBLY_ONLY && ji) {
					int k;
					gboolean found = FALSE;
					MonoAssembly **assemblies = mod->data.assemblies;

					if (assemblies) {
						for (k = 0; assemblies [k]; ++k)
							if (assemblies [k] == m_class_get_image (jinfo_get_method (ji)->klass)->assembly)
								found = TRUE;
					}
					if (!found)
						filtered = TRUE;
				} else if (mod->kind == MOD_KIND_SOURCE_FILE_ONLY && ei && ei->klass) {
					gpointer iter = NULL;
					MonoMethod *method;
					MonoDebugSourceInfo *sinfo;
					char *s;
					gboolean found = FALSE;
					int i;
					GPtrArray *source_file_list;

					while ((method = mono_class_get_methods (ei->klass, &iter))) {
						MonoDebugMethodInfo *minfo = mono_debug_lookup_method (method);

						if (minfo) {
							mono_debug_get_seq_points (minfo, NULL, &source_file_list, NULL, NULL, NULL);
							for (i = 0; i < source_file_list->len; ++i) {
								sinfo = (MonoDebugSourceInfo *)g_ptr_array_index (source_file_list, i);
								/*
								 * Do a case-insesitive match by converting the file name to
								 * lowercase.
								 */
								s = strdup_tolower (sinfo->source_file);
								if (g_hash_table_lookup (mod->data.source_files, s))
									found = TRUE;
								else {
									char *s2 = dbg_path_get_basename (sinfo->source_file);
									char *s3 = strdup_tolower (s2);

									if (g_hash_table_lookup (mod->data.source_files, s3))
										found = TRUE;
									g_free (s2);
									g_free (s3);
								}
								g_free (s);
							}
							g_ptr_array_free (source_file_list, TRUE);
						}
					}
					if (!found)
						filtered = TRUE;
				} else if (mod->kind == MOD_KIND_TYPE_NAME_ONLY && ei && ei->klass) {
					char *s;

					s = mono_type_full_name (m_class_get_byval_arg (ei->klass));
					if (!g_hash_table_lookup (mod->data.type_names, s))
						filtered = TRUE;
					g_free (s);
				} else if (mod->kind == MOD_KIND_STEP) {
					if ((mod->data.filter & STEP_FILTER_STATIC_CTOR) && ji &&
						(jinfo_get_method (ji)->flags & METHOD_ATTRIBUTE_SPECIAL_NAME) &&
						!strcmp (jinfo_get_method (ji)->name, ".cctor") &&
						(jinfo_get_method (ji) != ((SingleStepReq*)req->info)->start_method))
						filtered = TRUE;
					if ((mod->data.filter & STEP_FILTER_DEBUGGER_HIDDEN) && ji) {
						init_jit_info_dbg_attrs (ji);
						if (ji->dbg_hidden)
							filtered = TRUE;
					}
					if ((mod->data.filter & STEP_FILTER_DEBUGGER_STEP_THROUGH) && ji) {
						init_jit_info_dbg_attrs (ji);
						if (ji->dbg_step_through)
							filtered = TRUE;
					}
					if ((mod->data.filter & STEP_FILTER_DEBUGGER_NON_USER_CODE) && ji) {
						init_jit_info_dbg_attrs (ji);
						if (ji->dbg_non_user_code)
							filtered = TRUE;
					}
				}
			}

			if (!filtered && !is_new_filtered_exception) {
				*suspend_policy = MAX (*suspend_policy, req->suspend_policy);
				events = g_slist_append (events, GINT_TO_POINTER (req->id));
			}
		}
	}

	if (has_everything_else && !is_already_filtered) {
		filteredException = FALSE; 
		filtered_suspend_policy = everything_else_suspend_policy;
		filtered_req_id = everything_else_req_id;
	}

	if (!filteredException) {
		*suspend_policy = MAX (*suspend_policy, filtered_suspend_policy);
		events = g_slist_append (events, GINT_TO_POINTER (filtered_req_id));
	}

	/* Send a VM START/DEATH event by default */
	if (event == EVENT_KIND_VM_START)
		events = g_slist_append (events, GINT_TO_POINTER (0));
	if (event == EVENT_KIND_VM_DEATH)
		events = g_slist_append (events, GINT_TO_POINTER (0));

	return events;
}

static G_GNUC_UNUSED const char*
event_to_string (EventKind event)
{
	switch (event) {
	case EVENT_KIND_VM_START: return "VM_START";
	case EVENT_KIND_VM_DEATH: return "VM_DEATH";
	case EVENT_KIND_THREAD_START: return "THREAD_START";
	case EVENT_KIND_THREAD_DEATH: return "THREAD_DEATH";
	case EVENT_KIND_APPDOMAIN_CREATE: return "APPDOMAIN_CREATE";
	case EVENT_KIND_APPDOMAIN_UNLOAD: return "APPDOMAIN_UNLOAD";
	case EVENT_KIND_METHOD_ENTRY: return "METHOD_ENTRY";
	case EVENT_KIND_METHOD_EXIT: return "METHOD_EXIT";
	case EVENT_KIND_ASSEMBLY_LOAD: return "ASSEMBLY_LOAD";
	case EVENT_KIND_ASSEMBLY_UNLOAD: return "ASSEMBLY_UNLOAD";
	case EVENT_KIND_BREAKPOINT: return "BREAKPOINT";
	case EVENT_KIND_STEP: return "STEP";
	case EVENT_KIND_TYPE_LOAD: return "TYPE_LOAD";
	case EVENT_KIND_EXCEPTION: return "EXCEPTION";
	case EVENT_KIND_KEEPALIVE: return "KEEPALIVE";
	case EVENT_KIND_USER_BREAK: return "USER_BREAK";
	case EVENT_KIND_USER_LOG: return "USER_LOG";
	case EVENT_KIND_CRASH: return "CRASH";
	default:
		g_assert_not_reached ();
		return "";
	}
}

/*
 * process_event:
 *
 *   Send an event to the client, suspending the vm if needed.
 * LOCKING: Since this can suspend the calling thread, no locks should be held
 * by the caller.
 * The EVENTS list is freed by this function.
 */
static void
process_event (EventKind event, gpointer arg, gint32 il_offset, MonoContext *ctx, GSList *events, int suspend_policy)
{
	Buffer buf;
	GSList *l;
	MonoDomain *domain = mono_domain_get ();
	MonoThread *thread = NULL;
	MonoObject *keepalive_obj = NULL;
	gboolean send_success = FALSE;
	static int ecount;
	int nevents;

	if (!inited) {
		DEBUG_PRINTF (2, "Debugger agent not initialized yet: dropping %s\n", event_to_string (event));
		return;
	}

	if (!vm_start_event_sent && event != EVENT_KIND_VM_START) {
		// FIXME: We miss those events
		DEBUG_PRINTF (2, "VM start event not sent yet: dropping %s\n", event_to_string (event));
		return;
	}

	if (vm_death_event_sent) {
		DEBUG_PRINTF (2, "VM death event has been sent: dropping %s\n", event_to_string (event));
		return;
	}

	if (mono_runtime_is_shutting_down () && event != EVENT_KIND_VM_DEATH) {
		DEBUG_PRINTF (2, "Mono runtime is shutting down: dropping %s\n", event_to_string (event));
		return;
	}

	if (disconnected) {
		DEBUG_PRINTF (2, "Debugger client is not connected: dropping %s\n", event_to_string (event));
		return;
	}

	if (event == EVENT_KIND_KEEPALIVE)
		suspend_policy = SUSPEND_POLICY_NONE;
	else {
		if (events == NULL)
			return;

		if (agent_config.defer) {
			if (is_debugger_thread ()) {
				/* Don't suspend on events from the debugger thread */
				suspend_policy = SUSPEND_POLICY_NONE;
			}
		} else {
			if (is_debugger_thread () && event != EVENT_KIND_VM_DEATH)
				// FIXME: Send these with a NULL thread, don't suspend the current thread
				return;
		}
	}
	
	if (event == EVENT_KIND_VM_START) 
		suspend_policy = agent_config.suspend ? SUSPEND_POLICY_ALL : SUSPEND_POLICY_NONE;	
	
	nevents = g_slist_length (events);
	buffer_init (&buf, 128);
	buffer_add_byte (&buf, suspend_policy);
	buffer_add_int (&buf, nevents);

	for (l = events; l; l = l->next) {
		buffer_add_byte (&buf, event); // event kind
		buffer_add_int (&buf, GPOINTER_TO_INT (l->data)); // request id

		ecount ++;

		if (event == EVENT_KIND_VM_DEATH) {
			thread = NULL;
		} else {
			if (!thread)
				thread = is_debugger_thread () ? mono_thread_get_main () : mono_thread_current ();

			if (event == EVENT_KIND_VM_START && arg != NULL)
				thread = (MonoThread *)arg;
		}

		buffer_add_objid (&buf, (MonoObject*)thread); // thread

		switch (event) {
		case EVENT_KIND_THREAD_START:
		case EVENT_KIND_THREAD_DEATH:
			break;
		case EVENT_KIND_APPDOMAIN_CREATE:
		case EVENT_KIND_APPDOMAIN_UNLOAD:
			buffer_add_domainid (&buf, (MonoDomain *)arg);
			break;
		case EVENT_KIND_METHOD_ENTRY:
		case EVENT_KIND_METHOD_EXIT:
			buffer_add_methodid (&buf, domain, (MonoMethod *)arg);
			break;
		case EVENT_KIND_ASSEMBLY_LOAD:
			buffer_add_assemblyid (&buf, domain, (MonoAssembly *)arg);
			break;
		case EVENT_KIND_ASSEMBLY_UNLOAD: {
			DebuggerTlsData *tls;

			/* The domain the assembly belonged to is not equal to the current domain */
			tls = (DebuggerTlsData *)mono_native_tls_get_value (debugger_tls_id);
			g_assert (tls);
			g_assert (tls->domain_unloading);

			buffer_add_assemblyid (&buf, tls->domain_unloading, (MonoAssembly *)arg);
			break;
		}
		case EVENT_KIND_TYPE_LOAD:
			buffer_add_typeid (&buf, domain, (MonoClass *)arg);
			break;
		case EVENT_KIND_BREAKPOINT:
		case EVENT_KIND_STEP: {
			DebuggerTlsData *tls;
			tls = (DebuggerTlsData *)mono_native_tls_get_value (debugger_tls_id);
			g_assert (tls);
			mono_stopwatch_stop (&tls->step_time);
			MonoMethod *method = (MonoMethod *)arg;

			buffer_add_methodid (&buf, domain, method);
			buffer_add_long (&buf, il_offset);
			break;
		}
		case EVENT_KIND_VM_START:
			buffer_add_domainid (&buf, mono_get_root_domain ());
			break;
		case EVENT_KIND_VM_DEATH:
			if (CHECK_PROTOCOL_VERSION (2, 27))
				buffer_add_int (&buf, mono_environment_exitcode_get ());
			break;
		case EVENT_KIND_CRASH: {
			EventInfo *ei = (EventInfo *)arg;
			buffer_add_long (&buf, ei->hashes->offset_free_hash);
			buffer_add_string (&buf, ei->dump);
			break;
		}
		case EVENT_KIND_EXCEPTION: {
			EventInfo *ei = (EventInfo *)arg;
			buffer_add_objid (&buf, ei->exc);
			/*
			 * We are not yet suspending, so get_objref () will not keep this object alive. So we need to do it
			 * later after the suspension. (#12494).
			 */
			keepalive_obj = ei->exc;
			break;
		}
		case EVENT_KIND_USER_BREAK: {
			DebuggerTlsData *tls;
			tls = (DebuggerTlsData *)mono_native_tls_get_value (debugger_tls_id);
			g_assert (tls);
			// We are already processing a breakpoint event
			if (tls->disable_breakpoints)
				return;
			mono_stopwatch_stop (&tls->step_time);
			break;
		}
		case EVENT_KIND_USER_LOG: {
			EventInfo *ei = (EventInfo *)arg;
			buffer_add_int (&buf, ei->level);
			buffer_add_string (&buf, ei->category ? ei->category : "");
			buffer_add_string (&buf, ei->message ? ei->message : "");
			break;
		}
		case EVENT_KIND_KEEPALIVE:
			suspend_policy = SUSPEND_POLICY_NONE;
			break;
		default:
			g_assert_not_reached ();
		}
	}

	if (event == EVENT_KIND_VM_START) {
		if (!agent_config.defer) {
			ERROR_DECL (error);
			start_debugger_thread (error);
			mono_error_assert_ok (error);
		}
	}
   
	if (event == EVENT_KIND_VM_DEATH) {
		vm_death_event_sent = TRUE;
		suspend_policy = SUSPEND_POLICY_NONE;
	}

	if (mono_runtime_is_shutting_down ())
		suspend_policy = SUSPEND_POLICY_NONE;

	if (suspend_policy != SUSPEND_POLICY_NONE) {
		/* 
		 * Save the thread context and start suspending before sending the packet,
		 * since we could be receiving the resume request before send_packet ()
		 * returns.
		 */
		save_thread_context (ctx);
		DebuggerTlsData *tls = (DebuggerTlsData *)mono_g_hash_table_lookup (thread_to_tls,  mono_thread_internal_current ());
		tls->suspend_count++;
		suspend_vm ();

		if (keepalive_obj)
			/* This will keep this object alive */
			get_objref (keepalive_obj);
	}

	send_success = send_packet (CMD_SET_EVENT, CMD_COMPOSITE, &buf);

	if (send_success) {
		DebuggerTlsData *tls = (DebuggerTlsData *)mono_native_tls_get_value (debugger_tls_id);
		mono_debugger_log_event (tls, event_to_string (event), buf.buf, buffer_len (&buf));
	}

	buffer_free (&buf);

	g_slist_free (events);
	events = NULL;

	if (!send_success) {
		DEBUG_PRINTF (2, "Sending command %s failed.\n", event_to_string (event));
		return;
	}
	
	if (event == EVENT_KIND_VM_START) {
		vm_start_event_sent = TRUE;
	}

	DEBUG_PRINTF (1, "[%p] Sent %d events %s(%d), suspend=%d.\n", (gpointer) (gsize) mono_native_thread_id_get (), nevents, event_to_string (event), ecount, suspend_policy);

	switch (suspend_policy) {
	case SUSPEND_POLICY_NONE:
		break;
	case SUSPEND_POLICY_ALL:
		suspend_current ();
		break;
	case SUSPEND_POLICY_EVENT_THREAD:
		NOT_IMPLEMENTED;
		break;
	default:
		g_assert_not_reached ();
	}
}

static void
process_profiler_event (EventKind event, gpointer arg)
{
	int suspend_policy;
	GSList *events;
	EventInfo ei, *ei_arg = NULL;

	if (event == EVENT_KIND_TYPE_LOAD) {
		ei.klass = (MonoClass *)arg;
		ei_arg = &ei;
	}

	mono_loader_lock ();
	events = create_event_list (event, NULL, NULL, ei_arg, &suspend_policy);
	mono_loader_unlock ();

	process_event (event, arg, 0, NULL, events, suspend_policy);
}

static void
runtime_initialized (MonoProfiler *prof)
{
	process_profiler_event (EVENT_KIND_VM_START, mono_thread_current ());
	if (agent_config.defer) {
		ERROR_DECL (error);
		start_debugger_thread (error);
		mono_error_assert_ok (error);
	}
}

static void
runtime_shutdown (MonoProfiler *prof)
{
	process_profiler_event (EVENT_KIND_VM_DEATH, NULL);

	mono_debugger_agent_cleanup ();
}

static void
thread_startup (MonoProfiler *prof, uintptr_t tid)
{
	MonoInternalThread *thread = mono_thread_internal_current ();
	MonoInternalThread *old_thread;
	DebuggerTlsData *tls;

	if (is_debugger_thread ())
		return;

	g_assert (mono_native_thread_id_equals (MONO_UINT_TO_NATIVE_THREAD_ID (tid), MONO_UINT_TO_NATIVE_THREAD_ID (thread->tid)));

	mono_loader_lock ();
	old_thread = (MonoInternalThread *)mono_g_hash_table_lookup (tid_to_thread, GUINT_TO_POINTER (tid));
	mono_loader_unlock ();
	if (old_thread) {
		if (thread == old_thread) {
			/* 
			 * For some reason, thread_startup () might be called for the same thread
			 * multiple times (attach ?).
			 */
			DEBUG_PRINTF (1, "[%p] thread_start () called multiple times for %p, ignored.\n", GUINT_TO_POINTER (tid), GUINT_TO_POINTER (tid));
			return;
		} else {
			/*
			 * thread_end () might not be called for some threads, and the tid could
			 * get reused.
			 */
			DEBUG_PRINTF (1, "[%p] Removing stale data for tid %p.\n", GUINT_TO_POINTER (tid), GUINT_TO_POINTER (tid));
			mono_loader_lock ();
			mono_g_hash_table_remove (thread_to_tls, old_thread);
			mono_g_hash_table_remove (tid_to_thread, GUINT_TO_POINTER (tid));
			mono_g_hash_table_remove (tid_to_thread_obj, GUINT_TO_POINTER (tid));
			mono_loader_unlock ();
		}
	}

	tls = (DebuggerTlsData *)mono_native_tls_get_value (debugger_tls_id);
	g_assert (!tls);
	// FIXME: Free this somewhere
	tls = g_new0 (DebuggerTlsData, 1);
	MONO_GC_REGISTER_ROOT_SINGLE (tls->thread, MONO_ROOT_SOURCE_DEBUGGER, NULL, "Debugger Thread Reference");
	tls->thread = thread;
	// Do so we have thread id even after termination
	tls->thread_id = (intptr_t) thread->tid;
	mono_native_tls_set_value (debugger_tls_id, tls);

	DEBUG_PRINTF (1, "[%p] Thread started, obj=%p, tls=%p.\n", (gpointer)tid, thread, tls);

	mono_loader_lock ();
	mono_g_hash_table_insert_internal (thread_to_tls, thread, tls);
	mono_g_hash_table_insert_internal (tid_to_thread, (gpointer)tid, thread);
	mono_g_hash_table_insert_internal (tid_to_thread_obj, GUINT_TO_POINTER (tid), mono_thread_current ());
	mono_loader_unlock ();

	process_profiler_event (EVENT_KIND_THREAD_START, thread);

	/* 
	 * suspend_vm () could have missed this thread, so wait for a resume.
	 */
	
	suspend_current ();
}

static void
thread_end (MonoProfiler *prof, uintptr_t tid)
{
	MonoInternalThread *thread;
	DebuggerTlsData *tls = NULL;

	mono_loader_lock ();
	thread = (MonoInternalThread *)mono_g_hash_table_lookup (tid_to_thread, GUINT_TO_POINTER (tid));
	if (thread) {
		mono_g_hash_table_remove (tid_to_thread_obj, GUINT_TO_POINTER (tid));
		tls = (DebuggerTlsData *)mono_g_hash_table_lookup (thread_to_tls, thread);
		if (tls) {
			/* FIXME: Maybe we need to free this instead, but some code can't handle that */
			tls->terminated = TRUE;
			/* Can't remove from tid_to_thread, as that would defeat the check in thread_start () */
			MONO_GC_UNREGISTER_ROOT (tls->thread);
			tls->thread = NULL;
		}
	}
	mono_loader_unlock ();

	/* We might be called for threads started before we registered the start callback */
	if (thread) {
		DEBUG_PRINTF (1, "[%p] Thread terminated, obj=%p, tls=%p (domain=%p).\n", (gpointer)tid, thread, tls, (gpointer)mono_domain_get ());

		if (mono_thread_internal_is_current (thread) &&
		    (!mono_native_tls_get_value (debugger_tls_id) ||
		     !mono_domain_get ())
		) {
			/*
			 * This can happen on darwin and android since we
			 * deregister threads using pthread dtors.
			 * process_profiler_event () and the code it calls
			 * cannot handle a null TLS value.
			 */
			return;
		}

		process_profiler_event (EVENT_KIND_THREAD_DEATH, thread);
	}
}

static void
appdomain_load (MonoProfiler *prof, MonoDomain *domain)
{
	mono_de_domain_add (domain);

	process_profiler_event (EVENT_KIND_APPDOMAIN_CREATE, domain);
}

static void
appdomain_start_unload (MonoProfiler *prof, MonoDomain *domain)
{
	DebuggerTlsData *tls;

	/* This might be called during shutdown on the debugger thread from the CMD_VM_EXIT code */
	if (is_debugger_thread ())
		return;

	/*
	 * Remember the currently unloading appdomain as it is needed to generate
	 * proper ids for unloading assemblies.
	 */
	tls = (DebuggerTlsData *)mono_native_tls_get_value (debugger_tls_id);
	g_assert (tls);
	tls->domain_unloading = domain;
}

static void
appdomain_unload (MonoProfiler *prof, MonoDomain *domain)
{
	DebuggerTlsData *tls;

	if (is_debugger_thread ())
		return;

	tls = (DebuggerTlsData *)mono_native_tls_get_value (debugger_tls_id);
	g_assert (tls);
	tls->domain_unloading = NULL;

	mono_de_clear_breakpoints_for_domain (domain);
	
	mono_loader_lock ();
	/* Invalidate each thread's frame stack */
	mono_g_hash_table_foreach (thread_to_tls, invalidate_each_thread, NULL);
	mono_loader_unlock ();
	
	process_profiler_event (EVENT_KIND_APPDOMAIN_UNLOAD, domain);
}

/*
 * invalidate_each_thread:
 *
 *   A GHFunc to invalidate frames.
 *   value must be a DebuggerTlsData*
 */
static void
invalidate_each_thread (gpointer key, gpointer value, gpointer user_data)
{
	invalidate_frames ((DebuggerTlsData *)value);
}

static void
assembly_load (MonoProfiler *prof, MonoAssembly *assembly)
{
	/* Sent later in jit_end () */
	dbg_lock ();
	g_ptr_array_add (pending_assembly_loads, assembly);
	dbg_unlock ();
}

static void
assembly_unload (MonoProfiler *prof, MonoAssembly *assembly)
{
	if (is_debugger_thread ())
		return;

	process_profiler_event (EVENT_KIND_ASSEMBLY_UNLOAD, assembly);

	clear_event_requests_for_assembly (assembly);
	clear_types_for_assembly (assembly);
}

static void
send_type_load (MonoClass *klass)
{
	gboolean type_load = FALSE;
	MonoDomain *domain = mono_domain_get ();
	AgentDomainInfo *info = NULL;

	info = get_agent_domain_info (domain);

	mono_loader_lock ();

	if (!g_hash_table_lookup (info->loaded_classes, klass)) {
		type_load = TRUE;
		g_hash_table_insert (info->loaded_classes, klass, klass);
	}

	mono_loader_unlock ();

	if (type_load)
		emit_type_load (klass, klass, NULL);
}

/*
 * Emit load events for all types currently loaded in the domain.
 * Takes the loader and domain locks.
 * user_data is unused.
 */
static void
send_types_for_domain (MonoDomain *domain, void *user_data)
{
	MonoDomain* old_domain;
	AgentDomainInfo *info = NULL;

	if (mono_domain_is_unloading (domain))
		return;

	info = get_agent_domain_info (domain);
	g_assert (info);

	old_domain = mono_domain_get ();

	mono_domain_set_fast (domain, TRUE);
	
	mono_loader_lock ();
	g_hash_table_foreach (info->loaded_classes, emit_type_load, NULL);
	mono_loader_unlock ();

	mono_domain_set_fast (old_domain, TRUE);
}

static void
send_assemblies_for_domain (MonoDomain *domain, void *user_data)
{
	GSList *tmp;
	MonoDomain* old_domain;

	if (mono_domain_is_unloading (domain))
		return;

	old_domain = mono_domain_get ();

	mono_domain_set_fast (domain, TRUE);

	mono_domain_assemblies_lock (domain);
	for (tmp = domain->domain_assemblies; tmp; tmp = tmp->next) {
		MonoAssembly* ass = (MonoAssembly *)tmp->data;
		emit_assembly_load (ass, NULL);
	}
	mono_domain_assemblies_unlock (domain);

	mono_domain_set_fast (old_domain, TRUE);
}

static void
jit_done (MonoProfiler *prof, MonoMethod *method, MonoJitInfo *jinfo)
{
	jit_end (prof, method, jinfo);
}

static void
jit_failed (MonoProfiler *prof, MonoMethod *method)
{
	jit_end (prof, method, NULL);
}

static void
jit_end (MonoProfiler *prof, MonoMethod *method, MonoJitInfo *jinfo)
{
	/*
	 * We emit type load events when the first method of the type is JITted,
	 * since the class load profiler callbacks might be called with the
	 * loader lock held. They could also occur in the debugger thread.
	 * Same for assembly load events.
	 */
	while (TRUE) {
		MonoAssembly *assembly = NULL;

		// FIXME: Maybe store this in TLS so the thread of the event is correct ?
		dbg_lock ();
		if (pending_assembly_loads->len > 0) {
			assembly = (MonoAssembly *)g_ptr_array_index (pending_assembly_loads, 0);
			g_ptr_array_remove_index (pending_assembly_loads, 0);
		}
		dbg_unlock ();

		if (assembly) {
			process_profiler_event (EVENT_KIND_ASSEMBLY_LOAD, assembly);
		} else {
			break;
		}
	}

	send_type_load (method->klass);

	if (jinfo)
		mono_de_add_pending_breakpoints (method, jinfo);
}

/*
 * SINGLE STEPPING
 */

static void
event_requests_cleanup (void)
{
	mono_loader_lock ();
	int i = 0;
	while (i < event_requests->len) {
		EventRequest *req = (EventRequest *)g_ptr_array_index (event_requests, i);

		if (req->event_kind == EVENT_KIND_BREAKPOINT) {
			mono_de_clear_breakpoint ((MonoBreakpoint *)req->info);
			g_ptr_array_remove_index_fast (event_requests, i);
			g_free (req);
		} else {
			i ++;
		}
	}
	mono_loader_unlock ();
}

/*
 * ss_calculate_framecount:
 *
 * Ensure DebuggerTlsData fields are filled out.
 */
static void
ss_calculate_framecount (void *the_tls, MonoContext *ctx, gboolean force_use_ctx, DbgEngineStackFrame ***frames, int *nframes)
{
	DebuggerTlsData *tls = (DebuggerTlsData*)the_tls;

	if (force_use_ctx || !tls->context.valid)
		mono_thread_state_init_from_monoctx (&tls->context, ctx);
	compute_frame_info (tls->thread, tls, FALSE);
	if (frames)
		*frames = (DbgEngineStackFrame**)tls->frames;
	if (nframes)
		*nframes = tls->frame_count;
}

/*
 * ss_discard_frame_data:
 *
 * Discard frame data and invalidate any context
 */
static void
ss_discard_frame_context (void *the_tls)
{
	DebuggerTlsData *tls = (DebuggerTlsData*)the_tls;
	tls->context.valid = FALSE;
	tls->async_state.valid = FALSE;
	invalidate_frames (tls);
}

static MonoContext*
tls_get_restore_state (void *the_tls)
{
	DebuggerTlsData *tls = (DebuggerTlsData*)the_tls;

	return &tls->restore_state.ctx;
}

static gboolean
ensure_jit (DbgEngineStackFrame* the_frame)
{
	StackFrame *frame = (StackFrame*)the_frame;
	if (!frame->jit) {
		frame->jit = mono_debug_find_method (frame->api_method, frame->de.domain);
		if (!frame->jit && frame->api_method->is_inflated)
			frame->jit = mono_debug_find_method(mono_method_get_declaring_generic_method (frame->api_method), frame->de.domain);
		if (!frame->jit) {
			char *s;

			/* This could happen for aot images with no jit debug info */
			s = mono_method_full_name (frame->api_method, TRUE);
			DEBUG_PRINTF(1, "[dbg] No debug information found for '%s'.\n", s);
			g_free (s);
			return FALSE;
		}
	}
	return TRUE;
}

static gboolean
breakpoint_matches_assembly (MonoBreakpoint *bp, MonoAssembly *assembly)
{
	return bp->method && m_class_get_image (bp->method->klass)->assembly == assembly;
}

static gpointer
get_this_addr (DbgEngineStackFrame *the_frame)
{
	StackFrame *frame = (StackFrame *)the_frame;
	if (frame->de.ji->is_interp)
		return mini_get_interp_callbacks ()->frame_get_this (frame->interp_frame);

	MonoDebugVarInfo *var = frame->jit->this_var;
	if ((var->index & MONO_DEBUG_VAR_ADDRESS_MODE_FLAGS) != MONO_DEBUG_VAR_ADDRESS_MODE_REGOFFSET)
		return NULL;

	guint8 *addr = (guint8 *)mono_arch_context_get_int_reg (&frame->ctx, var->index & ~MONO_DEBUG_VAR_ADDRESS_MODE_FLAGS);
	addr += (gint32)var->offset;
	return addr;
}

static MonoMethod*
get_set_notification_method (MonoClass* async_builder_class)
{
	ERROR_DECL (error);
	GPtrArray* array = mono_class_get_methods_by_name (async_builder_class, "SetNotificationForWaitCompletion", 0x24, 1, FALSE, error);
	mono_error_assert_ok (error);
	if (array->len == 0) {
		g_ptr_array_free (array, TRUE);
		return NULL;
	}
	MonoMethod* set_notification_method = (MonoMethod *)g_ptr_array_index (array, 0);
	g_ptr_array_free (array, TRUE);
	return set_notification_method;
}

static MonoMethod*
get_object_id_for_debugger_method (MonoClass* async_builder_class)
{
	ERROR_DECL (error);
	GPtrArray *array = mono_class_get_methods_by_name (async_builder_class, "get_ObjectIdForDebugger", 0x24, 1, FALSE, error);
	mono_error_assert_ok (error);
	if (array->len != 1) {
		g_ptr_array_free (array, TRUE);
		//if we don't find method get_ObjectIdForDebugger we try to find the property Task to continue async debug.
		MonoProperty *prop = mono_class_get_property_from_name_internal (async_builder_class, "Task");
		if (!prop) {
			DEBUG_PRINTF (1, "Impossible to debug async methods.\n");
			return NULL;
		}
		return prop->get;
	}
	MonoMethod *method = (MonoMethod *)g_ptr_array_index (array, 0);
	g_ptr_array_free (array, TRUE);
	return method;
}

static MonoClass *
get_class_to_get_builder_field(DbgEngineStackFrame *frame)
{
	ERROR_DECL (error);
	gpointer this_addr = get_this_addr (frame);
	MonoClass *original_class = frame->method->klass;
	MonoClass *ret;
	if (!m_class_is_valuetype (original_class) && mono_class_is_open_constructed_type (m_class_get_byval_arg (original_class))) {
		MonoObject *this_obj = *(MonoObject**)this_addr;
		MonoGenericContext context;
		MonoType *inflated_type;

		if (!this_obj)
			return NULL;
			
		context = mono_get_generic_context_from_stack_frame (frame->ji, this_obj->vtable);
		inflated_type = mono_class_inflate_generic_type_checked (m_class_get_byval_arg (original_class), &context, error);
		mono_error_assert_ok (error); /* FIXME don't swallow the error */

		ret = mono_class_from_mono_type_internal (inflated_type);
		mono_metadata_free_type (inflated_type);
		return ret;
	}
	return original_class;
}


/* Return the address of the AsyncMethodBuilder struct belonging to the state machine method pointed to by FRAME */
static gpointer
get_async_method_builder (DbgEngineStackFrame *frame)
{
	MonoObject *this_obj;
	MonoClassField *builder_field;
	gpointer builder;
	gpointer this_addr;
	MonoClass* klass = frame->method->klass;

	klass = get_class_to_get_builder_field(frame);
	builder_field = mono_class_get_field_from_name_full (klass, "<>t__builder", NULL);
	if (!builder_field)
		return NULL;

	this_addr = get_this_addr (frame);
	if (!this_addr)
		return NULL;

	if (m_class_is_valuetype (klass)) {
		builder = mono_vtype_get_field_addr (*(guint8**)this_addr, builder_field);
	} else {
		this_obj = *(MonoObject**)this_addr;
		builder = (char*)this_obj + builder_field->offset;
	}

	return builder;
}

//This ID is used to figure out if breakpoint hit on resumeOffset belongs to us or not
//since thread probably changed...
static int
get_this_async_id (DbgEngineStackFrame *frame)
{
	MonoClassField *builder_field;
	gpointer builder;
	MonoMethod *method;
	MonoObject *ex;
	ERROR_DECL (error);
	MonoObject *obj;
	gboolean old_disable_breakpoints = FALSE;
	DebuggerTlsData *tls;

	/*
	 * FRAME points to a method in a state machine class/struct.
	 * Call the ObjectIdForDebugger method of the associated method builder type.
	 */
	builder = get_async_method_builder (frame);
	if (!builder)
		return 0;

	builder_field = mono_class_get_field_from_name_full (get_class_to_get_builder_field(frame), "<>t__builder", NULL);
	if (!builder_field)
		return 0;

	tls = (DebuggerTlsData *)mono_native_tls_get_value (debugger_tls_id);
	if (tls) {
		old_disable_breakpoints = tls->disable_breakpoints;
		tls->disable_breakpoints = TRUE;
	}

	method = get_object_id_for_debugger_method (mono_class_from_mono_type_internal (builder_field->type));
	if (!method) {
		if (tls)
			tls->disable_breakpoints = old_disable_breakpoints;
		return 0;
	}
	obj = mono_runtime_try_invoke (method, builder, NULL, &ex, error);
	mono_error_assert_ok (error);

	if (tls)
		tls->disable_breakpoints = old_disable_breakpoints;

	return get_objid (obj);
}

// Returns true if TaskBuilder has NotifyDebuggerOfWaitCompletion method
// false if not(AsyncVoidBuilder)
static gboolean
set_set_notification_for_wait_completion_flag (DbgEngineStackFrame *frame)
{
	MonoClassField *builder_field = mono_class_get_field_from_name_full (get_class_to_get_builder_field(frame), "<>t__builder", NULL);
	if (!builder_field)
		return FALSE;
	gpointer builder = get_async_method_builder (frame);
	if (!builder)
		return FALSE;

	MonoMethod* method = get_set_notification_method (mono_class_from_mono_type_internal (builder_field->type));
	if (method == NULL)
		return FALSE;
	gboolean arg = TRUE;
	ERROR_DECL (error);
	void *args [ ] = { &arg };
	mono_runtime_invoke_checked (method, builder, args, error);
	mono_error_assert_ok (error);
	return TRUE;
}

static MonoMethod* notify_debugger_of_wait_completion_method_cache;

static MonoMethod*
get_notify_debugger_of_wait_completion_method (void)
{
	if (notify_debugger_of_wait_completion_method_cache != NULL)
		return notify_debugger_of_wait_completion_method_cache;
	ERROR_DECL (error);
	MonoClass* task_class = mono_class_load_from_name (mono_defaults.corlib, "System.Threading.Tasks", "Task");
	GPtrArray* array = mono_class_get_methods_by_name (task_class, "NotifyDebuggerOfWaitCompletion", 0x24, 1, FALSE, error);
	mono_error_assert_ok (error);
	g_assert (array->len == 1);
	notify_debugger_of_wait_completion_method_cache = (MonoMethod *)g_ptr_array_index (array, 0);
	g_ptr_array_free (array, TRUE);
	return notify_debugger_of_wait_completion_method_cache;
}

static gboolean
begin_breakpoint_processing (void *the_tls, MonoContext *ctx, MonoJitInfo *ji, gboolean from_signal)
{
	DebuggerTlsData *tls = (DebuggerTlsData*)the_tls;

	/*
	 * Skip the instruction causing the breakpoint signal.
	 */
	if (from_signal)
		mono_arch_skip_breakpoint (ctx, ji);

	if (tls->disable_breakpoints)
		return FALSE;
	return TRUE;
}

typedef struct {
	GSList *bp_events, *ss_events, *enter_leave_events;
	EventKind kind;
	int suspend_policy;
} BreakPointEvents;

static void*
create_breakpoint_events (GPtrArray *ss_reqs, GPtrArray *bp_reqs, MonoJitInfo *ji, EventKind kind)
{
	int suspend_policy = 0;
	BreakPointEvents *evts = g_new0 (BreakPointEvents, 1);
	if (ss_reqs && ss_reqs->len > 0)
		evts->ss_events = create_event_list (EVENT_KIND_STEP, ss_reqs, ji, NULL, &suspend_policy);
	else if (bp_reqs && bp_reqs->len > 0)
		evts->bp_events = create_event_list (EVENT_KIND_BREAKPOINT, bp_reqs, ji, NULL, &suspend_policy);
	else if (kind != EVENT_KIND_BREAKPOINT)
		evts->enter_leave_events = create_event_list (kind, NULL, ji, NULL, &suspend_policy);

	evts->kind = kind;
	evts->suspend_policy = suspend_policy;
	return evts;
}

static void
process_breakpoint_events (void *_evts, MonoMethod *method, MonoContext *ctx, int il_offset)
{
	BreakPointEvents *evts = (BreakPointEvents*)_evts;
	/*
	 * FIXME: The first event will suspend, so the second will only be sent after the
	 * resume.
	 */
	if (evts->ss_events)
		process_event (EVENT_KIND_STEP, method, il_offset, ctx, evts->ss_events, evts->suspend_policy);
	if (evts->bp_events)
		process_event (evts->kind, method, il_offset, ctx, evts->bp_events, evts->suspend_policy);
	if (evts->enter_leave_events)
		process_event (evts->kind, method, il_offset, ctx, evts->enter_leave_events, evts->suspend_policy);

	g_free (evts);
}

/* Process a breakpoint/single step event after resuming from a signal handler */
static void
process_signal_event (void (*func) (void*, gboolean))
{
	DebuggerTlsData *tls;
	MonoThreadUnwindState orig_restore_state;
	MonoContext ctx;

	tls = (DebuggerTlsData *)mono_native_tls_get_value (debugger_tls_id);
	/* Have to save/restore the restore_ctx as we can be called recursively during invokes etc. */
	memcpy (&orig_restore_state, &tls->restore_state, sizeof (MonoThreadUnwindState));
	mono_thread_state_init_from_monoctx (&tls->restore_state, &tls->handler_ctx);

	func (tls, TRUE);

	/* This is called when resuming from a signal handler, so it shouldn't return */
	memcpy (&ctx, &tls->restore_state.ctx, sizeof (MonoContext));
	memcpy (&tls->restore_state, &orig_restore_state, sizeof (MonoThreadUnwindState));
	mono_restore_context (&ctx);
	g_assert_not_reached ();
}

static void
process_breakpoint_from_signal (void)
{
	process_signal_event (mono_de_process_breakpoint);
}

static void
resume_from_signal_handler (void *sigctx, void *func)
{
	DebuggerTlsData *tls;
	MonoContext ctx;

	/* Save the original context in TLS */
	// FIXME: This might not work on an altstack ?
	tls = (DebuggerTlsData *)mono_native_tls_get_value (debugger_tls_id);
	if (!tls)
		g_printerr ("Thread %p is not attached to the JIT.\n", (gpointer) (gsize) mono_native_thread_id_get ());
	g_assert (tls);

	// FIXME: MonoContext usually doesn't include the fp registers, so these are 
	// clobbered by a single step/breakpoint event. If this turns out to be a problem,
	// clob:c could be added to op_seq_point.

	mono_sigctx_to_monoctx (sigctx, &ctx);
	memcpy (&tls->handler_ctx, &ctx, sizeof (MonoContext));
#ifdef MONO_ARCH_HAVE_SETUP_RESUME_FROM_SIGNAL_HANDLER_CTX
	mono_arch_setup_resume_sighandler_ctx (&ctx, func);
#else
	MONO_CONTEXT_SET_IP (&ctx, func);
#endif
	mono_monoctx_to_sigctx (&ctx, sigctx);
}

static void
debugger_agent_breakpoint_hit (void *sigctx)
{
	/*
	 * We are called from a signal handler, and running code there causes all kinds of
	 * problems, like the original signal is disabled, libgc can't handle altstack, etc.
	 * So set up the signal context to return to the real breakpoint handler function.
	 */
	resume_from_signal_handler (sigctx, (gpointer)process_breakpoint_from_signal);
}

typedef struct {
	gboolean found;
	MonoContext *ctx;
} UserBreakCbData;

static gboolean
user_break_cb (StackFrameInfo *frame, MonoContext *ctx, gpointer user_data)
{
	UserBreakCbData *data = (UserBreakCbData*)user_data;

	if (frame->type == FRAME_TYPE_INTERP_TO_MANAGED || frame->type == FRAME_TYPE_INTERP_TO_MANAGED_WITH_CTX) {
		data->found = TRUE;
		return TRUE;
	}
	if (frame->managed) {
		data->found = TRUE;
		*data->ctx = *ctx;

		return TRUE;
	}
	return FALSE;
}

/*
 * Called by System.Diagnostics.Debugger:Break ().
 */
static void
debugger_agent_user_break (void)
{
	if (agent_config.enabled) {
		MonoContext ctx;
		int suspend_policy;
		GSList *events;
		UserBreakCbData data;

		memset (&data, 0, sizeof (data));
		data.ctx = &ctx;

		/* Obtain a context */
		MONO_CONTEXT_SET_IP (&ctx, NULL);
		mono_walk_stack_with_ctx (user_break_cb, NULL, (MonoUnwindOptions)0, &data);
		g_assert (data.found);

		mono_loader_lock ();
		events = create_event_list (EVENT_KIND_USER_BREAK, NULL, NULL, NULL, &suspend_policy);
		mono_loader_unlock ();

		process_event (EVENT_KIND_USER_BREAK, NULL, 0, &ctx, events, suspend_policy);
	} else if (mini_debug_options.native_debugger_break) {
		G_BREAKPOINT ();
	}
}

static void
begin_single_step_processing (MonoContext *ctx, gboolean from_signal)
{
	if (from_signal)
		mono_arch_skip_single_step (ctx);
}

static void
process_single_step (void)
{
	process_signal_event (mono_de_process_single_step);
}

/*
 * debugger_agent_single_step_event:
 *
 *   Called from a signal handler to handle a single step event.
 */
static void
debugger_agent_single_step_event (void *sigctx)
{
	/* Resume to process_single_step through the signal context */

	// FIXME: Since step out/over is implemented using step in, the step in case should
	// be as fast as possible. Move the relevant code from mono_de_process_single_step ()
	// here

	if (is_debugger_thread ()) {
		/* 
		 * This could happen despite our best effors when the runtime calls 
		 * assembly/type resolve hooks.
		 * FIXME: Breakpoints too.
		 */
		MonoContext ctx;

		mono_sigctx_to_monoctx (sigctx, &ctx);
		mono_arch_skip_single_step (&ctx);
		mono_monoctx_to_sigctx (&ctx, sigctx);
		return;
	}

	resume_from_signal_handler (sigctx, (gpointer)process_single_step);
}

static void
debugger_agent_single_step_from_context (MonoContext *ctx)
{
	DebuggerTlsData *tls;
	MonoThreadUnwindState orig_restore_state;

	tls = (DebuggerTlsData *)mono_native_tls_get_value (debugger_tls_id);
	/* Fastpath during invokes, see in process_suspend () */
	if (tls && suspend_count && suspend_count - tls->resume_count == 0)
		return;

	if (is_debugger_thread ())
		return;

	g_assert (tls);

	tls->terminated = FALSE;

	/* Have to save/restore the restore_ctx as we can be called recursively during invokes etc. */
	memcpy (&orig_restore_state, &tls->restore_state, sizeof (MonoThreadUnwindState));
	mono_thread_state_init_from_monoctx (&tls->restore_state, ctx);
	memcpy (&tls->handler_ctx, ctx, sizeof (MonoContext));

	mono_de_process_single_step (tls, FALSE);

	memcpy (ctx, &tls->restore_state.ctx, sizeof (MonoContext));
	memcpy (&tls->restore_state, &orig_restore_state, sizeof (MonoThreadUnwindState));
}

static void
debugger_agent_breakpoint_from_context (MonoContext *ctx)
{
	DebuggerTlsData *tls;
	MonoThreadUnwindState orig_restore_state;
	guint8 *orig_ip;

	if (is_debugger_thread ())
		return;

	orig_ip = (guint8 *)MONO_CONTEXT_GET_IP (ctx);
	MONO_CONTEXT_SET_IP (ctx, orig_ip - 1);

	tls = (DebuggerTlsData *)mono_native_tls_get_value (debugger_tls_id);
	g_assert (tls);
	
	//if a thread was suspended and doesn't have any managed stack, it was considered as terminated, 
	//but it wasn't really terminated because it can execute managed code again, and stop in a breakpoint so here we set terminated as FALSE
	tls->terminated = FALSE;

	memcpy (&orig_restore_state, &tls->restore_state, sizeof (MonoThreadUnwindState));
	mono_thread_state_init_from_monoctx (&tls->restore_state, ctx);
	memcpy (&tls->handler_ctx, ctx, sizeof (MonoContext));

	mono_de_process_breakpoint (tls, FALSE);

	memcpy (ctx, &tls->restore_state.ctx, sizeof (MonoContext));
	memcpy (&tls->restore_state, &orig_restore_state, sizeof (MonoThreadUnwindState));
	if (MONO_CONTEXT_GET_IP (ctx) == orig_ip - 1)
		MONO_CONTEXT_SET_IP (ctx, orig_ip);
}
static void
ss_args_destroy (SingleStepArgs *ss_args)
{
	if (ss_args->frames)
		free_frames ((StackFrame**)ss_args->frames, ss_args->nframes);
}

static int
handle_multiple_ss_requests (void)
{
	if (!CHECK_PROTOCOL_VERSION (2, 57))
		return DE_ERR_NOT_IMPLEMENTED;
	return 1;
}

static int
ensure_runtime_is_suspended (void)
{
	if (suspend_count == 0)
		return ERR_NOT_SUSPENDED;

	wait_for_suspend ();

	return ERR_NONE;
}

static int
ss_create_init_args (SingleStepReq *ss_req, SingleStepArgs *args)
{
	MonoSeqPointInfo *info = NULL;
	gboolean found_sp;
	MonoMethod *method = NULL;
	MonoDebugMethodInfo *minfo;
	gboolean step_to_catch = FALSE;
	gboolean set_ip = FALSE;
	StackFrame **frames = NULL;
	int nframes = 0;

	mono_loader_lock ();
	DebuggerTlsData *tls = (DebuggerTlsData *)mono_g_hash_table_lookup (thread_to_tls, ss_req->thread);
	mono_loader_unlock ();
	g_assert (tls);
	if (!tls->context.valid) {
		DEBUG_PRINTF (1, "Received a single step request on a thread with no managed frames.\n");
		return ERR_INVALID_ARGUMENT;
	}

	if (tls->restore_state.valid && MONO_CONTEXT_GET_IP (&tls->context.ctx) != MONO_CONTEXT_GET_IP (&tls->restore_state.ctx)) {
		/*
		 * Need to start single stepping from restore_state and not from the current state
		 */
		set_ip = TRUE;
		frames = compute_frame_info_from (ss_req->thread, tls, &tls->restore_state, &nframes);
	}

	ss_req->start_sp = ss_req->last_sp = MONO_CONTEXT_GET_SP (&tls->context.ctx);

	if (tls->has_catch_frame) {
		StackFrameInfo frame;

		/*
		 * We are stopped at a throw site. Stepping should go to the catch site.
		 */
		frame = tls->catch_frame;
		if (frame.type != FRAME_TYPE_MANAGED && frame.type != FRAME_TYPE_INTERP) {
			DEBUG_PRINTF (1, "Current frame is not managed nor interpreter.\n");
			return ERR_INVALID_ARGUMENT;
		}

		/*
		 * Find the seq point corresponding to the landing site ip, which is the first seq
		 * point after ip.
		 */
		found_sp = mono_find_next_seq_point_for_native_offset (frame.domain, frame.method, frame.native_offset, &info, &args->sp);
		if (!found_sp)
			no_seq_points_found (frame.method, frame.native_offset);
		if (!found_sp) {
			DEBUG_PRINTF (1, "Could not find next sequence point.\n");
			return ERR_INVALID_ARGUMENT;
		}

		method = frame.method;

		step_to_catch = TRUE;
		/* This make sure the seq point is not skipped by process_single_step () */
		ss_req->last_sp = NULL;
	}

	if (!step_to_catch) {
		StackFrame *frame = NULL;

		if (set_ip) {
			if (frames && nframes)
				frame = frames [0];
		} else {
			compute_frame_info (ss_req->thread, tls, FALSE);

			if (tls->frame_count)
				frame = tls->frames [0];
		}

		if (ss_req->size == STEP_SIZE_LINE) {
			if (frame) {
				ss_req->last_method = frame->de.method;
				ss_req->last_line = -1;

				minfo = mono_debug_lookup_method (frame->de.method);
				if (minfo && frame->il_offset != -1) {
					MonoDebugSourceLocation *loc = mono_debug_method_lookup_location (minfo, frame->il_offset);

					if (loc) {
						ss_req->last_line = loc->row;
						g_free (loc);
					}
				}
			}
		}

		if (frame) {
			if (!method && frame->il_offset != -1) {
				/* FIXME: Sort the table and use a binary search */
				found_sp = mono_find_prev_seq_point_for_native_offset (frame->de.domain, frame->de.method, frame->de.native_offset, &info, &args->sp);
				if (!found_sp)
					no_seq_points_found (frame->de.method, frame->de.native_offset);
				if (!found_sp) {
					DEBUG_PRINTF (1, "Could not find next sequence point.\n");
					return ERR_INVALID_ARGUMENT;
				}
				method = frame->de.method;
			}
		}
	}

	ss_req->start_method = method;

	args->method = method;
	args->ctx = set_ip ? &tls->restore_state.ctx : &tls->context.ctx;
	args->tls = tls;
	args->step_to_catch = step_to_catch;
	args->info = info;
	args->frames = (DbgEngineStackFrame**)frames;
	args->nframes = nframes;

	return ERR_NONE;
}

static void
ss_clear_for_assembly (SingleStepReq *req, MonoAssembly *assembly)
{
	GSList *l;
	gboolean found = TRUE;

	while (found) {
		found = FALSE;
		for (l = req->bps; l; l = l->next) {
			if (breakpoint_matches_assembly ((MonoBreakpoint *)l->data, assembly)) {
				mono_de_clear_breakpoint ((MonoBreakpoint *)l->data);
				req->bps = g_slist_delete_link (req->bps, l);
				found = TRUE;
				break;
			}
		}
	}
}

/*
 * This takes a lot of locks and stuff. Do this at the end, after
 * other things have dumped us, so that getting stuck here won't
 * prevent seeing other crash information
 */
static void
mono_debugger_agent_send_crash (char *json_dump, MonoStackHash *hashes, int pause)
{
	MONO_ENTER_GC_UNSAFE;
#ifndef DISABLE_CRASH_REPORTING
	int suspend_policy;
	GSList *events;
	EventInfo ei;

	if (!agent_config.enabled)
		return;

	// Don't send the event if the client doesn't expect it
	if (!CHECK_PROTOCOL_VERSION (2, 49))
		return;

	// It doesn't make sense to wait for lldb/gdb to finish if we're not
	// actually enabled. Therefore we do the wait here.
	sleep (pause);

	// Don't heap allocate when we can avoid it
	EventRequest request;
	memset (&request, 0, sizeof (request));
	request.event_kind = EVENT_KIND_CRASH;

	gpointer pdata [1];
	pdata [0] = &request;
	GPtrArray array;
	memset (&array, 0, sizeof (array));
	array.pdata = pdata;
	array.len = 1;

	mono_loader_lock ();
	events = create_event_list (EVENT_KIND_CRASH, &array, NULL, NULL, &suspend_policy);
	mono_loader_unlock ();

	ei.dump = json_dump;
	ei.hashes = hashes;

	g_assert (events != NULL);

	process_event (EVENT_KIND_CRASH, &ei, 0, NULL, events, suspend_policy);

	// Don't die before it is sent.
	sleep (4);
#endif
	MONO_EXIT_GC_UNSAFE;
}

/*
 * Called from metadata by the icall for System.Diagnostics.Debugger:Log ().
 */
static void
debugger_agent_debug_log (int level, MonoString *category, MonoString *message)
{
	ERROR_DECL (error);
	int suspend_policy;
	GSList *events;
	EventInfo ei;

	if (!agent_config.enabled)
		return;

	memset (&ei, 0, sizeof (ei));

	mono_loader_lock ();
	events = create_event_list (EVENT_KIND_USER_LOG, NULL, NULL, NULL, &suspend_policy);
	mono_loader_unlock ();

	ei.level = level;
	if (category) {
		ei.category = mono_string_to_utf8_checked_internal (category, error);
		mono_error_cleanup (error);
		error_init (error);
	}
	if (message) {
		ei.message = mono_string_to_utf8_checked_internal (message, error);
		mono_error_cleanup  (error);
	}

	process_event (EVENT_KIND_USER_LOG, &ei, 0, NULL, events, suspend_policy);

	g_free (ei.category);
	g_free (ei.message);
}

static gboolean
debugger_agent_debug_log_is_enabled (void)
{
	/* Treat this as true even if there is no event request for EVENT_KIND_USER_LOG */
	return agent_config.enabled;
}

static void
debugger_agent_unhandled_exception (MonoException *exc)
{
	int suspend_policy;
	GSList *events;
	EventInfo ei;

	if (!inited)
		return;

	memset (&ei, 0, sizeof (ei));
	ei.exc = (MonoObject*)exc;

	mono_loader_lock ();
	events = create_event_list (EVENT_KIND_EXCEPTION, NULL, NULL, &ei, &suspend_policy);
	mono_loader_unlock ();

	process_event (EVENT_KIND_EXCEPTION, &ei, 0, NULL, events, suspend_policy);
}

static void
debugger_agent_handle_exception (MonoException *exc, MonoContext *throw_ctx,
									  MonoContext *catch_ctx, StackFrameInfo *catch_frame)
{
	if (catch_ctx == NULL && catch_frame == NULL && mini_debug_options.suspend_on_unhandled && mono_object_class (exc) != mono_defaults.threadabortexception_class) {
		mono_runtime_printf_err ("Unhandled exception, suspending...");
		while (1)
			;
	}
	
	int i, j, suspend_policy;
	GSList *events;
	MonoJitInfo *ji, *catch_ji;
	EventInfo ei;
	DebuggerTlsData *tls = NULL;

	if (thread_to_tls != NULL) {
		MonoInternalThread *thread = mono_thread_internal_current ();

		mono_loader_lock ();
		tls = (DebuggerTlsData *)mono_g_hash_table_lookup (thread_to_tls, thread);
		mono_loader_unlock ();

		if (tls && tls->abort_requested)
			return;
		if (tls && tls->disable_breakpoints)
			return;
	}

	memset (&ei, 0, sizeof (ei));

	/* Just-In-Time debugging */
	if (!catch_ctx) {
		if (agent_config.onuncaught && !inited) {
			finish_agent_init (FALSE);

			/*
			 * Send an unsolicited EXCEPTION event with a dummy request id.
			 */
			events = g_slist_append (NULL, GUINT_TO_POINTER (0xffffff));
			ei.exc = (MonoObject*)exc;
			process_event (EVENT_KIND_EXCEPTION, &ei, 0, throw_ctx, events, SUSPEND_POLICY_ALL);
			return;
		}
	} else if (agent_config.onthrow && !inited) {
		GSList *l;
		gboolean found = FALSE;

		for (l = agent_config.onthrow; l; l = l->next) {
			char *ex_type = (char *)l->data;
			char *f = mono_type_full_name (m_class_get_byval_arg (exc->object.vtable->klass));

			if (!strcmp (ex_type, "") || !strcmp (ex_type, f))
				found = TRUE;

			g_free (f);
		}

		if (found) {
			finish_agent_init (FALSE);

			/*
			 * Send an unsolicited EXCEPTION event with a dummy request id.
			 */
			events = g_slist_append (NULL, GUINT_TO_POINTER (0xffffff));
			ei.exc = (MonoObject*)exc;
			process_event (EVENT_KIND_EXCEPTION, &ei, 0, throw_ctx, events, SUSPEND_POLICY_ALL);
			return;
		}
	}

	if (!inited)
		return;

	ji = mini_jit_info_table_find (mono_domain_get (), (char *)MONO_CONTEXT_GET_IP (throw_ctx), NULL);
	if (catch_frame)
		catch_ji = catch_frame->ji;
	else
		catch_ji = NULL;

	ei.exc = (MonoObject*)exc;
	ei.caught = catch_ctx != NULL;

	mono_loader_lock ();

	/* Treat exceptions which are caught in non-user code as unhandled */
	for (i = 0; i < event_requests->len; ++i) {
		EventRequest *req = (EventRequest *)g_ptr_array_index (event_requests, i);
		if (req->event_kind != EVENT_KIND_EXCEPTION)
			continue;

		for (j = 0; j < req->nmodifiers; ++j) {
			Modifier *mod = &req->modifiers [j];

			if (mod->kind == MOD_KIND_ASSEMBLY_ONLY && catch_ji) {
				int k;
				gboolean found = FALSE;
				MonoAssembly **assemblies = mod->data.assemblies;

				if (assemblies) {
					for (k = 0; assemblies [k]; ++k)
						if (assemblies [k] == m_class_get_image (jinfo_get_method (catch_ji)->klass)->assembly)
							found = TRUE;
				}
				if (!found)
					ei.caught = FALSE;
			}
		}
	}

	events = create_event_list (EVENT_KIND_EXCEPTION, NULL, ji, &ei, &suspend_policy);
	mono_loader_unlock ();

	if (tls && ei.caught && catch_ctx) {
		if (catch_frame) {
			tls->has_catch_frame = TRUE;
			tls->catch_frame = *catch_frame;
		} else {
			memset (&tls->catch_frame, 0, sizeof (tls->catch_frame));
		}
	}

	process_event (EVENT_KIND_EXCEPTION, &ei, 0, throw_ctx, events, suspend_policy);

	if (tls)
		tls->has_catch_frame = FALSE;
}

static void
debugger_agent_begin_exception_filter (MonoException *exc, MonoContext *ctx, MonoContext *orig_ctx)
{
	DebuggerTlsData *tls;

	if (!inited)
		return;

	tls = (DebuggerTlsData *)mono_native_tls_get_value (debugger_tls_id);
	if (!tls)
		return;

	/*
	 * We're about to invoke an exception filter during the first pass of exception handling.
	 *
	 * 'ctx' is the context that'll get passed to the filter ('call_filter (ctx, ei->data.filter)'),
	 * 'orig_ctx' is the context where the exception has been thrown.
	 *
	 *
	 * See mcs/class/Mono.Debugger.Soft/Tests/dtest-excfilter.il for an example.
	 *
	 * If we're stopped in Filter(), normal stack unwinding would first unwind to
	 * the call site (line 37) and then continue to Main(), but it would never
	 * include the throw site (line 32).
	 *
	 * Since exception filters are invoked during the first pass of exception handling,
	 * the stack frames of the throw site are still intact, so we should include them
	 * in a stack trace.
	 *
	 * We do this here by saving the context of the throw site in 'tls->filter_state'.
	 *
	 * Exception filters are used by MonoDroid, where we want to stop inside a call filter,
	 * but report the location of the 'throw' to the user.
	 *
	 */

	g_assert (mono_thread_state_init_from_monoctx (&tls->filter_state, orig_ctx));
}

static void
debugger_agent_end_exception_filter (MonoException *exc, MonoContext *ctx, MonoContext *orig_ctx)
{
	DebuggerTlsData *tls;

	if (!inited)
		return;

	tls = (DebuggerTlsData *)mono_native_tls_get_value (debugger_tls_id);
	if (!tls)
		return;

	tls->filter_state.valid = FALSE;
}

static void 
buffer_add_fixed_array (Buffer *buf, MonoType *t, void *addr, MonoDomain *domain,
					   gboolean as_vtype, GHashTable *parent_vtypes, gint32 len_fixed_array)
{
	buffer_add_byte (buf, VALUE_TYPE_ID_FIXED_ARRAY);
	buffer_add_byte (buf, t->type);
	buffer_add_int (buf, len_fixed_array );
	for (int i = 0; i < len_fixed_array; i++) {
		switch (t->type) {
			case MONO_TYPE_BOOLEAN:
			case MONO_TYPE_I1:
			case MONO_TYPE_U1:
				buffer_add_int (buf, ((gint8*)addr)[i]);
				break;
			case MONO_TYPE_CHAR:
			case MONO_TYPE_I2:
			case MONO_TYPE_U2:
				buffer_add_int (buf, ((gint16*)addr)[i]);
				break;
			case MONO_TYPE_I4:
			case MONO_TYPE_U4:
			case MONO_TYPE_R4:
				buffer_add_int (buf, ((gint32*)addr)[i]);
				break;
			case MONO_TYPE_I8:
			case MONO_TYPE_U8:
			case MONO_TYPE_R8:
				buffer_add_long (buf, ((gint64*)addr)[i]);
				break;
			case MONO_TYPE_PTR: {
				gssize val = *(gssize*)addr;
				
				buffer_add_byte (buf, t->type);
				buffer_add_long (buf, val);
				if (CHECK_PROTOCOL_VERSION(2, 46))
					buffer_add_typeid (buf, domain, mono_class_from_mono_type_internal (t));
				break;
			}
		}
	}
}
/*
 * buffer_add_value_full:
 *
 *   Add the encoding of the value at ADDR described by T to the buffer.
 * AS_VTYPE determines whenever to treat primitive types as primitive types or
 * vtypes.
 */
static void
buffer_add_value_full (Buffer *buf, MonoType *t, void *addr, MonoDomain *domain,
					   gboolean as_vtype, GHashTable *parent_vtypes, gint32 len_fixed_array)
{
	MonoObject *obj;
	gboolean boxed_vtype = FALSE;

	if (t->byref) {
		if (!(*(void**)addr)) {
			/* This can happen with compiler generated locals */
			//printf ("%s\n", mono_type_full_name (t));
			buffer_add_byte (buf, VALUE_TYPE_ID_NULL);
			return;
		}
		g_assert (*(void**)addr);
		addr = *(void**)addr;
	}

	if (as_vtype) {
		switch (t->type) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_R4:
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
		case MONO_TYPE_R8:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_PTR:
			goto handle_vtype;
			break;
		default:
			break;
		}
	}
	
	if (len_fixed_array > 1 && t->type != MONO_TYPE_VALUETYPE && CHECK_PROTOCOL_VERSION (2, 53))
	{
		buffer_add_fixed_array(buf, t, addr, domain, as_vtype, parent_vtypes, len_fixed_array);
		return;
	}
	switch (t->type) {
	case MONO_TYPE_VOID:
		buffer_add_byte (buf, t->type);
		break;
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
		buffer_add_byte (buf, t->type);
		buffer_add_int (buf, *(gint8*)addr);
		break;
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
		buffer_add_byte (buf, t->type);
		buffer_add_int (buf, *(gint16*)addr);
		break;
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_R4:
		buffer_add_byte (buf, t->type);
		buffer_add_int (buf, *(gint32*)addr);
		break;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_R8:
		buffer_add_byte (buf, t->type);
		buffer_add_long (buf, *(gint64*)addr);
		break;
	case MONO_TYPE_I:
	case MONO_TYPE_U:
		/* Treat it as a vtype */
		goto handle_vtype;
	case MONO_TYPE_PTR: {
		gssize val = *(gssize*)addr;
		
		buffer_add_byte (buf, t->type);
		buffer_add_long (buf, val);
		if (CHECK_PROTOCOL_VERSION(2, 46))
			buffer_add_typeid (buf, domain, mono_class_from_mono_type_internal (t));
		break;
	}
	handle_ref:
	case MONO_TYPE_STRING:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_ARRAY:
		obj = *(MonoObject**)addr;

		if (!obj) {
			buffer_add_byte (buf, VALUE_TYPE_ID_NULL);
		} else {
			if (m_class_is_valuetype (obj->vtable->klass)) {
				t = m_class_get_byval_arg (obj->vtable->klass);
				addr = mono_object_unbox_internal (obj);
				boxed_vtype = TRUE;
				goto handle_vtype;
			} else if (m_class_get_rank (obj->vtable->klass)) {
				buffer_add_byte (buf, m_class_get_byval_arg (obj->vtable->klass)->type);
			} else if (m_class_get_byval_arg (obj->vtable->klass)->type == MONO_TYPE_GENERICINST) {
				buffer_add_byte (buf, MONO_TYPE_CLASS);
			} else {
				buffer_add_byte (buf, m_class_get_byval_arg (obj->vtable->klass)->type);
			}
			buffer_add_objid (buf, obj);
		}
		break;
	handle_vtype:
	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_TYPEDBYREF: {
		int nfields;
		gpointer iter;
		MonoClassField *f;
		MonoClass *klass = mono_class_from_mono_type_internal (t);
		int vtype_index;

		if (boxed_vtype) {
			/*
			* Handle boxed vtypes recursively referencing themselves using fields.
			*/
			if (!parent_vtypes)
				parent_vtypes = g_hash_table_new (NULL, NULL);
			vtype_index = GPOINTER_TO_INT (g_hash_table_lookup (parent_vtypes, addr));
			if (vtype_index) {
				if (CHECK_PROTOCOL_VERSION (2, 33)) {
					buffer_add_byte (buf, VALUE_TYPE_ID_PARENT_VTYPE);
					buffer_add_int (buf, vtype_index - 1);
				} else {
					/* The client can't handle PARENT_VTYPE */
					buffer_add_byte (buf, VALUE_TYPE_ID_NULL);
				}
				break;
			} else {
				g_hash_table_insert (parent_vtypes, addr, GINT_TO_POINTER (g_hash_table_size (parent_vtypes) + 1));
			}
		}

		buffer_add_byte (buf, MONO_TYPE_VALUETYPE);
		buffer_add_byte (buf, m_class_is_enumtype (klass));
		buffer_add_typeid (buf, domain, klass);

		nfields = 0;
		iter = NULL;
		while ((f = mono_class_get_fields_internal (klass, &iter))) {
			if (f->type->attrs & FIELD_ATTRIBUTE_STATIC)
				continue;
			if (mono_field_is_deleted (f))
				continue;
			nfields ++;
		}
		buffer_add_int (buf, nfields);

		iter = NULL;
		while ((f = mono_class_get_fields_internal (klass, &iter))) {
			if (f->type->attrs & FIELD_ATTRIBUTE_STATIC)
				continue;
			if (mono_field_is_deleted (f))
				continue;
			buffer_add_value_full (buf, f->type, mono_vtype_get_field_addr (addr, f), domain, FALSE, parent_vtypes, len_fixed_array != 1 ? len_fixed_array : isFixedSizeArray(f));
		}

		if (boxed_vtype) {
			g_hash_table_remove (parent_vtypes, addr);
			if (g_hash_table_size (parent_vtypes) == 0) {
				g_hash_table_destroy (parent_vtypes);
				parent_vtypes = NULL;
			}
		}
		break;
	}
	case MONO_TYPE_GENERICINST:
		if (mono_type_generic_inst_is_valuetype (t)) {
			goto handle_vtype;
		} else {
			goto handle_ref;
		}
		break;
	default:
		NOT_IMPLEMENTED;
	}
}

static void
buffer_add_value (Buffer *buf, MonoType *t, void *addr, MonoDomain *domain)
{
	buffer_add_value_full (buf, t, addr, domain, FALSE, NULL, 1);
}

static gboolean
obj_is_of_type (MonoObject *obj, MonoType *t)
{
	MonoClass *klass = obj->vtable->klass;
	if (!mono_class_is_assignable_from_internal (mono_class_from_mono_type_internal (t), klass)) {
		if (mono_class_is_transparent_proxy (klass)) {
			klass = ((MonoTransparentProxy *)obj)->remote_class->proxy_class;
			if (mono_class_is_assignable_from_internal (mono_class_from_mono_type_internal (t), klass)) {
				return TRUE;
			}
		}
		return FALSE;
	}
	return TRUE;
}

static ErrorCode
decode_value (MonoType *t, MonoDomain *domain, gpointer void_addr, gpointer void_buf, guint8 **endbuf, guint8 *limit, gboolean check_field_datatype);

static ErrorCode
decode_vtype (MonoType *t, MonoDomain *domain, gpointer void_addr, gpointer void_buf, guint8 **endbuf, guint8 *limit, gboolean check_field_datatype)
{
	guint8 *addr = (guint8*)void_addr;
	guint8 *buf = (guint8*)void_buf;
	gboolean is_enum;
	MonoClass *klass;
	MonoClassField *f;
	int nfields;
	gpointer iter = NULL;
	MonoDomain *d;
	ErrorCode err;

	is_enum = decode_byte (buf, &buf, limit);
	/* Enums are sent as a normal vtype */
	if (is_enum)
		return ERR_NOT_IMPLEMENTED;
	klass = decode_typeid (buf, &buf, limit, &d, &err);
	if (err != ERR_NONE)
		return err;

	if (t && klass != mono_class_from_mono_type_internal (t)) {
		char *name = mono_type_full_name (t);
		char *name2 = mono_type_full_name (m_class_get_byval_arg (klass));
		DEBUG_PRINTF (1, "[%p] Expected value of type %s, got %s.\n", (gpointer) (gsize) mono_native_thread_id_get (), name, name2);
		g_free (name);
		g_free (name2);
		return ERR_INVALID_ARGUMENT;
	}

	nfields = decode_int (buf, &buf, limit);
	while ((f = mono_class_get_fields_internal (klass, &iter))) {
		if (f->type->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;
		if (mono_field_is_deleted (f))
			continue;
		err = decode_value (f->type, domain, mono_vtype_get_field_addr (addr, f), buf, &buf, limit, check_field_datatype);
		if (err != ERR_NONE)
			return err;
		nfields --;
	}
	g_assert (nfields == 0);

	*endbuf = buf;

	return ERR_NONE;
}
static ErrorCode decode_fixed_size_array_internal (MonoType *t, int type, MonoDomain *domain, guint8 *addr, guint8 *buf, guint8 **endbuf, guint8 *limit, gboolean check_field_datatype)
{
	ErrorCode err = ERR_NONE;
	int fixedSizeLen = 1;
	int newType = MONO_TYPE_END;
	if (CHECK_PROTOCOL_VERSION (2, 53)) {
		newType = decode_byte (buf, &buf, limit);
		fixedSizeLen = decode_int (buf, &buf, limit);
		//t->type = newType;
	}
	for (int i = 0 ; i < fixedSizeLen; i++) {
		switch (newType) {
		case MONO_TYPE_BOOLEAN:
			((guint8*)addr)[i] = decode_int (buf, &buf, limit);
			break;
		case MONO_TYPE_CHAR:
			((gunichar2*)addr)[i] = decode_int (buf, &buf, limit);
			break;
		case MONO_TYPE_I1:
			((gint8*)addr)[i] = decode_int (buf, &buf, limit);
			break;
		case MONO_TYPE_U1:
			((guint8*)addr)[i] = decode_int (buf, &buf, limit);
			break;
		case MONO_TYPE_I2:
			((gint16*)addr)[i] = decode_int (buf, &buf, limit);
			break;
		case MONO_TYPE_U2:
			((guint16*)addr)[i]  = decode_int (buf, &buf, limit);
			break;
		case MONO_TYPE_I4:
			((gint32*)addr)[i]  = decode_int (buf, &buf, limit);
			break;
		case MONO_TYPE_U4:
			((guint32*)addr)[i]  = decode_int (buf, &buf, limit);
			break;
		case MONO_TYPE_I8:
			((gint64*)addr)[i]  = decode_long (buf, &buf, limit);
			break;
		case MONO_TYPE_U8:
			((guint64*)addr)[i]  = decode_long (buf, &buf, limit);
			break;
		case MONO_TYPE_R4:
			((guint32*)addr)[i]  = decode_int (buf, &buf, limit);
			break;
		case MONO_TYPE_R8:
			((guint64*)addr)[i]  = decode_long (buf, &buf, limit);
			break;
		}
	}
	*endbuf = buf;
	return err;
}
static ErrorCode
decode_value_internal (MonoType *t, int type, MonoDomain *domain, guint8 *addr, guint8 *buf, guint8 **endbuf, guint8 *limit, gboolean check_field_datatype)
{
	ErrorCode err;
	if (type != t->type && !MONO_TYPE_IS_REFERENCE (t) &&
		!(t->type == MONO_TYPE_I && type == MONO_TYPE_VALUETYPE) &&
		!(type == VALUE_TYPE_ID_FIXED_ARRAY) &&
		!(t->type == MONO_TYPE_U && type == MONO_TYPE_VALUETYPE) &&
		!(t->type == MONO_TYPE_PTR && type == MONO_TYPE_I8) &&
		!(t->type == MONO_TYPE_GENERICINST && type == MONO_TYPE_VALUETYPE) &&
		!(t->type == MONO_TYPE_VALUETYPE && type == MONO_TYPE_OBJECT)) {
		char *name = mono_type_full_name (t);
		DEBUG_PRINTF (1, "[%p] Expected value of type %s, got 0x%0x.\n", (gpointer) (gsize) mono_native_thread_id_get (), name, type);
		g_free (name);
		return ERR_INVALID_ARGUMENT;
	}
	if (type == VALUE_TYPE_ID_FIXED_ARRAY && t->type != MONO_TYPE_VALUETYPE) {
		decode_fixed_size_array_internal (t, type, domain, addr, buf, endbuf, limit, check_field_datatype);
		return ERR_NONE;
	}

	switch (t->type) {
	case MONO_TYPE_BOOLEAN:
		*(guint8*)addr = decode_int (buf, &buf, limit);
		break;
	case MONO_TYPE_CHAR:
		*(gunichar2*)addr = decode_int (buf, &buf, limit);
		break;
	case MONO_TYPE_I1:
		*(gint8*)addr = decode_int (buf, &buf, limit);
		break;
	case MONO_TYPE_U1:
		*(guint8*)addr = decode_int (buf, &buf, limit);
		break;
	case MONO_TYPE_I2:
		*(gint16*)addr = decode_int (buf, &buf, limit);
		break;
	case MONO_TYPE_U2:
		*(guint16*)addr = decode_int (buf, &buf, limit);
		break;
	case MONO_TYPE_I4:
		*(gint32*)addr = decode_int (buf, &buf, limit);
		break;
	case MONO_TYPE_U4:
		*(guint32*)addr = decode_int (buf, &buf, limit);
		break;
	case MONO_TYPE_I8:
		*(gint64*)addr = decode_long (buf, &buf, limit);
		break;
	case MONO_TYPE_U8:
		*(guint64*)addr = decode_long (buf, &buf, limit);
		break;
	case MONO_TYPE_R4:
		*(guint32*)addr = decode_int (buf, &buf, limit);
		break;
	case MONO_TYPE_R8:
		*(guint64*)addr = decode_long (buf, &buf, limit);
		break;
	case MONO_TYPE_PTR:
		/* We send these as I8, so we get them back as such */
		g_assert (type == MONO_TYPE_I8);
		*(gssize*)addr = decode_long (buf, &buf, limit);
		break;
	case MONO_TYPE_GENERICINST:
		if (MONO_TYPE_ISSTRUCT (t)) {
			/* The client sends these as a valuetype */
			goto handle_vtype;
		} else {
			goto handle_ref;
		}
		break;
	case MONO_TYPE_I:
	case MONO_TYPE_U:
		/* We send these as vtypes, so we get them back as such */
		g_assert (type == MONO_TYPE_VALUETYPE);
		/* Fall through */
		handle_vtype:
	case MONO_TYPE_VALUETYPE:
		if (type == MONO_TYPE_OBJECT) {
			/* Boxed vtype */
			int objid = decode_objid (buf, &buf, limit);
			ErrorCode err;
			MonoObject *obj;

			err = get_object (objid, (MonoObject**)&obj);
			if (err != ERR_NONE)
				return err;
			if (!obj)
				return ERR_INVALID_ARGUMENT;
			if (obj->vtable->klass != mono_class_from_mono_type_internal (t)) {
				DEBUG_PRINTF (1, "Expected type '%s', got object '%s'\n", mono_type_full_name (t), m_class_get_name (obj->vtable->klass));
				return ERR_INVALID_ARGUMENT;
			}
			memcpy (addr, mono_object_unbox_internal (obj), mono_class_value_size (obj->vtable->klass, NULL));
		} else {
			err = decode_vtype (t, domain, addr, buf, &buf, limit, check_field_datatype);
			if (err != ERR_NONE)
				return err;
		}
		break;
	handle_ref:
	default:
		if (MONO_TYPE_IS_REFERENCE (t)) {
			if (type == MONO_TYPE_OBJECT) {
				int objid = decode_objid (buf, &buf, limit);
				ErrorCode err;
				MonoObject *obj;

				err = get_object (objid, (MonoObject**)&obj);
				if (err != ERR_NONE)
					return err;

				if (obj) {
					if (!obj_is_of_type (obj, t)) {
						if (check_field_datatype) { //if it's not executing a invoke method check the datatypes.
							DEBUG_PRINTF (1, "Expected type '%s', got '%s'\n", mono_type_full_name (t), m_class_get_name (obj->vtable->klass));
							return ERR_INVALID_ARGUMENT;
						}
					}
				}
				if (obj && obj->vtable->domain != domain)
					return ERR_INVALID_ARGUMENT;

				mono_gc_wbarrier_generic_store_internal (addr, obj);
			} else if (type == VALUE_TYPE_ID_NULL) {
				*(MonoObject**)addr = NULL;
			} else if (type == MONO_TYPE_VALUETYPE) {
				ERROR_DECL (error);
				guint8 *buf2;
				gboolean is_enum;
				MonoClass *klass;
				MonoDomain *d;
				guint8 *vtype_buf;
				int vtype_buf_size;

				/* This can happen when round-tripping boxed vtypes */
				/*
				* Obtain vtype class.
				* Same as the beginning of the handle_vtype case above.
				*/
				buf2 = buf;
				is_enum = decode_byte (buf, &buf, limit);
				if (is_enum)
					return ERR_NOT_IMPLEMENTED;
				klass = decode_typeid (buf, &buf, limit, &d, &err);
				if (err != ERR_NONE)
					return err;

				/* Decode the vtype into a temporary buffer, then box it. */
				vtype_buf_size = mono_class_value_size (klass, NULL);
				vtype_buf = (guint8 *)g_malloc0 (vtype_buf_size);
				g_assert (vtype_buf);

				buf = buf2;
				err = decode_vtype (NULL, domain, vtype_buf, buf, &buf, limit, check_field_datatype);
				if (err != ERR_NONE) {
					g_free (vtype_buf);
					return err;
				}
				*(MonoObject**)addr = mono_value_box_checked (d, klass, vtype_buf, error);
				mono_error_cleanup (error);
				g_free (vtype_buf);
			} else {
				char *name = mono_type_full_name (t);
				DEBUG_PRINTF (1, "[%p] Expected value of type %s, got 0x%0x.\n", (gpointer) (gsize) mono_native_thread_id_get (), name, type);
				g_free (name);
				return ERR_INVALID_ARGUMENT;
			}
		} else if ((t->type == MONO_TYPE_GENERICINST) && 
					mono_metadata_generic_class_is_valuetype (t->data.generic_class) &&
					m_class_is_enumtype (t->data.generic_class->container_class)){
			err = decode_vtype (t, domain, addr, buf, &buf, limit, check_field_datatype);
			if (err != ERR_NONE)
				return err;
		} else {
			NOT_IMPLEMENTED;
		}
		break;
	}


	*endbuf = buf;

	return ERR_NONE;
}

static ErrorCode
decode_value (MonoType *t, MonoDomain *domain, gpointer void_addr, gpointer void_buf, guint8 **endbuf, guint8 *limit, gboolean check_field_datatype)
{
	guint8 *addr = (guint8*)void_addr;
	guint8 *buf = (guint8*)void_buf;

	ERROR_DECL (error);
	ErrorCode err;
	int type = decode_byte (buf, &buf, limit);

	if (t->type == MONO_TYPE_GENERICINST && mono_class_is_nullable (mono_class_from_mono_type_internal (t))) {
		MonoType *targ = t->data.generic_class->context.class_inst->type_argv [0];
		guint8 *nullable_buf;

		/*
		 * First try decoding it as a Nullable`1
		 */
		err = decode_value_internal (t, type, domain, addr, buf, endbuf, limit, check_field_datatype);
		if (err == ERR_NONE)
			return err;

		/*
		 * Then try decoding as a primitive value or null.
		 */
		if (targ->type == type) {
			nullable_buf = (guint8 *)g_malloc (mono_class_instance_size (mono_class_from_mono_type_internal (targ)));
			err = decode_value_internal (targ, type, domain, nullable_buf, buf, endbuf, limit, check_field_datatype);
			if (err != ERR_NONE) {
				g_free (nullable_buf);
				return err;
			}
			MonoObject *boxed = mono_value_box_checked (domain, mono_class_from_mono_type_internal (targ), nullable_buf, error);
			if (!is_ok (error)) {
				mono_error_cleanup (error);
				return ERR_INVALID_OBJECT;
			}
			mono_nullable_init (addr, boxed, mono_class_from_mono_type_internal (t));
			g_free (nullable_buf);
			*endbuf = buf;
			return ERR_NONE;
		} else if (type == VALUE_TYPE_ID_NULL) {
			mono_nullable_init (addr, NULL, mono_class_from_mono_type_internal (t));
			*endbuf = buf;
			return ERR_NONE;
		}
	}

	return decode_value_internal (t, type, domain, addr, buf, endbuf, limit, check_field_datatype);
}

static void
add_var (Buffer *buf, MonoDebugMethodJitInfo *jit, MonoType *t, MonoDebugVarInfo *var, MonoContext *ctx, MonoDomain *domain, gboolean as_vtype)
{
	guint32 flags;
	int reg;
	guint8 *addr, *gaddr;
	host_mgreg_t reg_val;

	flags = var->index & MONO_DEBUG_VAR_ADDRESS_MODE_FLAGS;
	reg = var->index & ~MONO_DEBUG_VAR_ADDRESS_MODE_FLAGS;

	switch (flags) {
	case MONO_DEBUG_VAR_ADDRESS_MODE_REGISTER:
		reg_val = mono_arch_context_get_int_reg (ctx, reg);

		buffer_add_value_full (buf, t, &reg_val, domain, as_vtype, NULL, 1);
		break;
	case MONO_DEBUG_VAR_ADDRESS_MODE_REGOFFSET:
		addr = (guint8 *)mono_arch_context_get_int_reg (ctx, reg);
		addr += (gint32)var->offset;

		//printf ("[R%d+%d] = %p\n", reg, var->offset, addr);

		buffer_add_value_full (buf, t, addr, domain, as_vtype, NULL, 1);
		break;
	case MONO_DEBUG_VAR_ADDRESS_MODE_DEAD:
		NOT_IMPLEMENTED;
		break;
	case MONO_DEBUG_VAR_ADDRESS_MODE_REGOFFSET_INDIR:
	case MONO_DEBUG_VAR_ADDRESS_MODE_VTADDR:
		/* Same as regoffset, but with an indirection */
		addr = (guint8 *)mono_arch_context_get_int_reg (ctx, reg);
		addr += (gint32)var->offset;

		gaddr = (guint8 *)*(gpointer*)addr;
		g_assert (gaddr);
		buffer_add_value_full (buf, t, gaddr, domain, as_vtype, NULL, 1);
		break;
	case MONO_DEBUG_VAR_ADDRESS_MODE_GSHAREDVT_LOCAL: {
		MonoDebugVarInfo *info_var = jit->gsharedvt_info_var;
		MonoDebugVarInfo *locals_var = jit->gsharedvt_locals_var;
		MonoGSharedVtMethodRuntimeInfo *info;
		guint8 *locals;
		int idx;

		idx = reg;

		g_assert (info_var);
		g_assert (locals_var);

		flags = info_var->index & MONO_DEBUG_VAR_ADDRESS_MODE_FLAGS;
		reg = info_var->index & ~MONO_DEBUG_VAR_ADDRESS_MODE_FLAGS;
		if (flags == MONO_DEBUG_VAR_ADDRESS_MODE_REGOFFSET) {
			addr = (guint8 *)mono_arch_context_get_int_reg (ctx, reg);
			addr += (gint32)info_var->offset;
			info = (MonoGSharedVtMethodRuntimeInfo *)*(gpointer*)addr;
		} else if (flags == MONO_DEBUG_VAR_ADDRESS_MODE_REGISTER) {
			info = (MonoGSharedVtMethodRuntimeInfo *)mono_arch_context_get_int_reg (ctx, reg);
		} else {
			g_assert_not_reached ();
		}
		g_assert (info);

		flags = locals_var->index & MONO_DEBUG_VAR_ADDRESS_MODE_FLAGS;
		reg = locals_var->index & ~MONO_DEBUG_VAR_ADDRESS_MODE_FLAGS;
		if (flags == MONO_DEBUG_VAR_ADDRESS_MODE_REGOFFSET) {
			addr = (guint8 *)mono_arch_context_get_int_reg (ctx, reg);
			addr += (gint32)locals_var->offset;
			locals = (guint8 *)*(gpointer*)addr;
		} else if (flags == MONO_DEBUG_VAR_ADDRESS_MODE_REGISTER) {
			locals = (guint8 *)mono_arch_context_get_int_reg (ctx, reg);
		} else {
			g_assert_not_reached ();
		}
		g_assert (locals);

		addr = locals + GPOINTER_TO_INT (info->entries [idx]);

		buffer_add_value_full (buf, t, addr, domain, as_vtype, NULL, 1);
		break;
	}

	default:
		g_assert_not_reached ();
	}
}

static void
set_var (MonoType *t, MonoDebugVarInfo *var, MonoContext *ctx, MonoDomain *domain, guint8 *val, host_mgreg_t **reg_locations, MonoContext *restore_ctx)
{
	guint32 flags;
	int reg, size;
	guint8 *addr, *gaddr;

	flags = var->index & MONO_DEBUG_VAR_ADDRESS_MODE_FLAGS;
	reg = var->index & ~MONO_DEBUG_VAR_ADDRESS_MODE_FLAGS;

	if (MONO_TYPE_IS_REFERENCE (t))
		size = sizeof (gpointer);
	else
		size = mono_class_value_size (mono_class_from_mono_type_internal (t), NULL);

	switch (flags) {
	case MONO_DEBUG_VAR_ADDRESS_MODE_REGISTER: {
#ifdef MONO_ARCH_HAVE_CONTEXT_SET_INT_REG
		host_mgreg_t v;
		gboolean is_signed = FALSE;

		if (t->byref) {
			addr = (guint8 *)mono_arch_context_get_int_reg (ctx, reg);

			if (addr) {
				// FIXME: Write barriers
				mono_gc_memmove_atomic (addr, val, size);
			}
			break;
		}

		if (!t->byref && (t->type == MONO_TYPE_I1 || t->type == MONO_TYPE_I2 || t->type == MONO_TYPE_I4 || t->type == MONO_TYPE_I8))
			is_signed = TRUE;

		switch (size) {
		case 1:
			v = is_signed ? *(gint8*)val : *(guint8*)val;
			break;
		case 2:
			v = is_signed ? *(gint16*)val : *(guint16*)val;
			break;
		case 4:
			v = is_signed ? *(gint32*)val : *(guint32*)val;
			break;
		case 8:
			v = is_signed ? *(gint64*)val : *(guint64*)val;
			break;
		default:
			g_assert_not_reached ();
		}

		/* Set value on the stack or in the return ctx */
		if (reg_locations [reg]) {
			/* Saved on the stack */
			DEBUG_PRINTF (1, "[dbg] Setting stack location %p for reg %x to %p.\n", reg_locations [reg], reg, (gpointer)v);
			*(reg_locations [reg]) = v;
		} else {
			/* Not saved yet */
			DEBUG_PRINTF (1, "[dbg] Setting context location for reg %x to %p.\n", reg, (gpointer)v);
			mono_arch_context_set_int_reg (restore_ctx, reg, v);
		}			

		// FIXME: Move these to mono-context.h/c.
		mono_arch_context_set_int_reg (ctx, reg, v);
#else
		// FIXME: Can't set registers, so we disable linears
		NOT_IMPLEMENTED;
#endif
		break;
	}
	case MONO_DEBUG_VAR_ADDRESS_MODE_REGOFFSET:
		addr = (guint8 *)mono_arch_context_get_int_reg (ctx, reg);
		addr += (gint32)var->offset;

		//printf ("[R%d+%d] = %p\n", reg, var->offset, addr);

		if (t->byref) {
			addr = *(guint8**)addr;

			if (!addr)
				break;
		}
			
		// FIXME: Write barriers
		mono_gc_memmove_atomic (addr, val, size);
		break;
	case MONO_DEBUG_VAR_ADDRESS_MODE_REGOFFSET_INDIR:
		/* Same as regoffset, but with an indirection */
		addr = (guint8 *)mono_arch_context_get_int_reg (ctx, reg);
		addr += (gint32)var->offset;

		gaddr = (guint8 *)*(gpointer*)addr;
		g_assert (gaddr);
		// FIXME: Write barriers
		mono_gc_memmove_atomic (gaddr, val, size);
		break;
	case MONO_DEBUG_VAR_ADDRESS_MODE_DEAD:
		NOT_IMPLEMENTED;
		break;
	default:
		g_assert_not_reached ();
	}
}

static void
set_interp_var (MonoType *t, gpointer addr, guint8 *val_buf)
{
	int size;

	if (t->byref) {
		addr = *(gpointer*)addr;
		g_assert (addr);
	}

	if (MONO_TYPE_IS_REFERENCE (t))
		size = sizeof (gpointer);
	else
		size = mono_class_value_size (mono_class_from_mono_type_internal (t), NULL);

	memcpy (addr, val_buf, size);
}

static void
clear_event_request (int req_id, int etype)
{
	int i;

	mono_loader_lock ();
	for (i = 0; i < event_requests->len; ++i) {
		EventRequest *req = (EventRequest *)g_ptr_array_index (event_requests, i);

		if (req->id == req_id && req->event_kind == etype) {
			if (req->event_kind == EVENT_KIND_BREAKPOINT)
				mono_de_clear_breakpoint ((MonoBreakpoint *)req->info);
			if (req->event_kind == EVENT_KIND_STEP) {
				mono_de_cancel_ss ((SingleStepReq *)req->info);
			}
			if (req->event_kind == EVENT_KIND_METHOD_ENTRY)
				mono_de_clear_breakpoint ((MonoBreakpoint *)req->info);
			if (req->event_kind == EVENT_KIND_METHOD_EXIT)
				mono_de_clear_breakpoint ((MonoBreakpoint *)req->info);
			g_ptr_array_remove_index_fast (event_requests, i);
			g_free (req);
			break;
		}
	}
	mono_loader_unlock ();
}

static void
clear_assembly_from_modifier (EventRequest *req, Modifier *m, MonoAssembly *assembly)
{
	int i;

	if (m->kind == MOD_KIND_EXCEPTION_ONLY && m->data.exc_class && m_class_get_image (m->data.exc_class)->assembly == assembly)
		m->kind = MOD_KIND_NONE;
	if (m->kind == MOD_KIND_ASSEMBLY_ONLY && m->data.assemblies) {
		int count = 0, match_count = 0, pos;
		MonoAssembly **newassemblies;

		for (i = 0; m->data.assemblies [i]; ++i) {
			count ++;
			if (m->data.assemblies [i] == assembly)
				match_count ++;
		}

		if (match_count) {
			// +1 because we don't know length and we use last element to check for end
			newassemblies = g_new0 (MonoAssembly*, count - match_count + 1);

			pos = 0;
			for (i = 0; i < count; ++i)
				if (m->data.assemblies [i] != assembly)
					newassemblies [pos ++] = m->data.assemblies [i];
			g_assert (pos == count - match_count);
			g_free (m->data.assemblies);
			m->data.assemblies = newassemblies;
		}
	}
}

static void
clear_assembly_from_modifiers (EventRequest *req, MonoAssembly *assembly)
{
	int i;

	for (i = 0; i < req->nmodifiers; ++i) {
		Modifier *m = &req->modifiers [i];

		clear_assembly_from_modifier (req, m, assembly);
	}
}

/*
 * clear_event_requests_for_assembly:
 *
 *   Clear all events requests which reference ASSEMBLY.
 */
static void
clear_event_requests_for_assembly (MonoAssembly *assembly)
{
	int i;
	gboolean found;

	mono_loader_lock ();
	found = TRUE;
	while (found) {
		found = FALSE;
		for (i = 0; i < event_requests->len; ++i) {
			EventRequest *req = (EventRequest *)g_ptr_array_index (event_requests, i);

			clear_assembly_from_modifiers (req, assembly);

			if (req->event_kind == EVENT_KIND_BREAKPOINT && breakpoint_matches_assembly ((MonoBreakpoint *)req->info, assembly)) {
				clear_event_request (req->id, req->event_kind);
				found = TRUE;
				break;
			}

			if (req->event_kind == EVENT_KIND_STEP)
				ss_clear_for_assembly ((SingleStepReq *)req->info, assembly);
		}
	}
	mono_loader_unlock ();
}

/*
 * type_comes_from_assembly:
 *
 *   GHRFunc that returns TRUE if klass comes from assembly
 */
static gboolean
type_comes_from_assembly (gpointer klass, gpointer also_klass, gpointer assembly)
{
	return mono_type_in_image (m_class_get_byval_arg ((MonoClass*)klass), mono_assembly_get_image_internal ((MonoAssembly*)assembly));
}

/*
 * clear_types_for_assembly:
 *
 *   Clears types from loaded_classes for a given assembly
 */
static void
clear_types_for_assembly (MonoAssembly *assembly)
{
	MonoDomain *domain = mono_domain_get ();
	AgentDomainInfo *info = NULL;

	if (!domain || !domain_jit_info (domain))
		/* Can happen during shutdown */
		return;

	info = get_agent_domain_info (domain);

	mono_loader_lock ();
	g_hash_table_foreach_remove (info->loaded_classes, type_comes_from_assembly, assembly);
	mono_loader_unlock ();
}

static void
dispose_vm (void)
{
	/* Clear all event requests */
	mono_loader_lock ();
	while (event_requests->len > 0) {
		EventRequest *req = (EventRequest *)g_ptr_array_index (event_requests, 0);

		clear_event_request (req->id, req->event_kind);
	}
	mono_loader_unlock ();

	while (suspend_count > 0)
		resume_vm ();
	disconnected = TRUE;
	vm_start_event_sent = FALSE;
}

static void
count_thread_check_gc_finalizer (gpointer key, gpointer value, gpointer user_data)
{
	MonoThread *thread = (MonoThread *)value;
	gboolean *ret = (gboolean *)user_data;
	if (mono_gc_is_finalizer_internal_thread(thread->internal_thread)) {
		DebuggerTlsData *tls = (DebuggerTlsData *)mono_g_hash_table_lookup (thread_to_tls, thread->internal_thread);
		if (!tls->gc_finalizing) { //GC Finalizer is not running some finalizer code, so ignore it
			*ret = TRUE;
			return;
		}
	}
}

static void
add_thread (gpointer key, gpointer value, gpointer user_data)
{
	MonoThread *thread = (MonoThread *)value;
	Buffer *buf = (Buffer *)user_data;
	if (mono_gc_is_finalizer_internal_thread(thread->internal_thread)) {
		DebuggerTlsData *tls = (DebuggerTlsData *)mono_g_hash_table_lookup (thread_to_tls, thread->internal_thread);
		if (!tls->gc_finalizing) //GC Finalizer is not running some finalizer code, so ignore it
			return;
	}
	buffer_add_objid (buf, (MonoObject*)thread);
}


static ErrorCode
do_invoke_method (DebuggerTlsData *tls, Buffer *buf, InvokeData *invoke, guint8 *p, guint8 **endp)
{
	ERROR_DECL (error);
	guint8 *end = invoke->endp;
	MonoMethod *m;
	int i, nargs;
	ErrorCode err;
	MonoMethodSignature *sig;
	guint8 **arg_buf;
	void **args;
	MonoObject *this_arg, *res, *exc = NULL;
	MonoDomain *domain;
	guint8 *this_buf;
#ifdef MONO_ARCH_SOFT_DEBUG_SUPPORTED
	MonoLMFExt ext;
#endif
	MonoStopwatch watch;

	if (invoke->method) {
		/* 
		 * Invoke this method directly, currently only Environment.Exit () is supported.
		 */
		this_arg = NULL;
		DEBUG_PRINTF (1, "[%p] Invoking method '%s' on receiver '%s'.\n", (gpointer) (gsize) mono_native_thread_id_get (), mono_method_full_name (invoke->method, TRUE), this_arg ? m_class_get_name (this_arg->vtable->klass) : "<null>");

		mono_runtime_try_invoke (invoke->method, NULL, invoke->args, &exc, error);
		mono_error_assert_ok (error);
		g_assert_not_reached ();
	}

	m = decode_methodid (p, &p, end, &domain, &err);
	if (err != ERR_NONE)
		return err;
	sig = mono_method_signature_internal (m);

	if (m_class_is_valuetype (m->klass))
		this_buf = (guint8 *)g_alloca (mono_class_instance_size (m->klass));
	else
		this_buf = (guint8 *)g_alloca (sizeof (MonoObject*));

	if (m->is_generic) {
		DEBUG_PRINTF (1, "[%p] Error: Attempting to invoke uninflated generic method %s.\n", (gpointer)(gsize)mono_native_thread_id_get (), mono_method_full_name (m, TRUE));
		return ERR_INVALID_ARGUMENT;
	} else if (m_class_is_valuetype (m->klass) && (m->flags & METHOD_ATTRIBUTE_STATIC)) {
		/* Should be null */
		int type = decode_byte (p, &p, end);
		if (type != VALUE_TYPE_ID_NULL) {
			DEBUG_PRINTF (1, "[%p] Error: Static vtype method invoked with this argument.\n", (gpointer) (gsize) mono_native_thread_id_get ());
			return ERR_INVALID_ARGUMENT;
		}
		memset (this_buf, 0, mono_class_instance_size (m->klass));
	} else if (m_class_is_valuetype (m->klass) && !strcmp (m->name, ".ctor")) {
			/* Could be null */
			guint8 *tmp_p;

			int type = decode_byte (p, &tmp_p, end);
			if (type == VALUE_TYPE_ID_NULL) {
				memset (this_buf, 0, mono_class_instance_size (m->klass));
				p = tmp_p;
			} else {
				err = decode_value (m_class_get_byval_arg (m->klass), domain, this_buf, p, &p, end, FALSE);
				if (err != ERR_NONE)
					return err;
			}
	} else {
		err = decode_value (m_class_get_byval_arg (m->klass), domain, this_buf, p, &p, end, FALSE);
		if (err != ERR_NONE)
			return err;
	}

	if (!m_class_is_valuetype (m->klass))
		this_arg = *(MonoObject**)this_buf;
	else
		this_arg = NULL;

	if (MONO_CLASS_IS_INTERFACE_INTERNAL (m->klass)) {
		if (!this_arg) {
			DEBUG_PRINTF (1, "[%p] Error: Interface method invoked without this argument.\n", (gpointer) (gsize) mono_native_thread_id_get ());
			return ERR_INVALID_ARGUMENT;
		}
		m = mono_object_get_virtual_method_internal (this_arg, m);
		/* Transform this to the format the rest of the code expects it to be */
		if (m_class_is_valuetype (m->klass)) {
			this_buf = (guint8 *)g_alloca (mono_class_instance_size (m->klass));
			memcpy (this_buf, mono_object_unbox_internal (this_arg), mono_class_instance_size (m->klass));
		}
	} else if ((m->flags & METHOD_ATTRIBUTE_VIRTUAL) && !m_class_is_valuetype (m->klass) && invoke->flags & INVOKE_FLAG_VIRTUAL) {
		if (!this_arg) {
			DEBUG_PRINTF (1, "[%p] Error: invoke with INVOKE_FLAG_VIRTUAL flag set without this argument.\n", (gpointer) (gsize) mono_native_thread_id_get ());
			return ERR_INVALID_ARGUMENT;
		}
		m = mono_object_get_virtual_method_internal (this_arg, m);
		if (m_class_is_valuetype (m->klass)) {
			this_buf = (guint8 *)g_alloca (mono_class_instance_size (m->klass));
			memcpy (this_buf, mono_object_unbox_internal (this_arg), mono_class_instance_size (m->klass));
		}
	}

	DEBUG_PRINTF (1, "[%p] Invoking method '%s' on receiver '%s'.\n", (gpointer) (gsize) mono_native_thread_id_get (), mono_method_full_name (m, TRUE), this_arg ? m_class_get_name (this_arg->vtable->klass) : "<null>");

	if (this_arg && this_arg->vtable->domain != domain)
		NOT_IMPLEMENTED;

	if (!m_class_is_valuetype (m->klass) && !(m->flags & METHOD_ATTRIBUTE_STATIC) && !this_arg) {
		if (!strcmp (m->name, ".ctor")) {
			if (mono_class_is_abstract (m->klass))
				return ERR_INVALID_ARGUMENT;
			else {
				ERROR_DECL (error);
				this_arg = mono_object_new_checked (domain, m->klass, error);
				if (!is_ok (error)) {
					mono_error_cleanup (error);
					return ERR_INVALID_ARGUMENT;
				}
			}
		} else {
			return ERR_INVALID_ARGUMENT;
		}
	}

	if (this_arg && !obj_is_of_type (this_arg, m_class_get_byval_arg (m->klass)))
		return ERR_INVALID_ARGUMENT;

	nargs = decode_int (p, &p, end);
	if (nargs != sig->param_count)
		return ERR_INVALID_ARGUMENT;
	/* Use alloca to get gc tracking */
	arg_buf = (guint8 **)g_alloca (nargs * sizeof (gpointer));
	memset (arg_buf, 0, nargs * sizeof (gpointer));
	args = (gpointer *)g_alloca (nargs * sizeof (gpointer));
	for (i = 0; i < nargs; ++i) {
		if (MONO_TYPE_IS_REFERENCE (sig->params [i])) {
			err = decode_value (sig->params [i], domain, (guint8*)&args [i], p, &p, end, TRUE);
			if (err != ERR_NONE)
				break;
			if (args [i] && ((MonoObject*)args [i])->vtable->domain != domain)
				NOT_IMPLEMENTED;

			if (sig->params [i]->byref) {
				arg_buf [i] = g_newa (guint8, sizeof (gpointer));
				*(gpointer*)arg_buf [i] = args [i];
				args [i] = arg_buf [i];
			}
		} else {
			MonoClass *arg_class = mono_class_from_mono_type_internal (sig->params [i]);
			arg_buf [i] = (guint8 *)g_alloca (mono_class_instance_size (arg_class));
			err = decode_value (sig->params [i], domain, arg_buf [i], p, &p, end, TRUE);
			if (err != ERR_NONE)
				break;
			if (mono_class_is_nullable (arg_class)) {
				args [i] = mono_nullable_box (arg_buf [i], arg_class, error);
				mono_error_assert_ok (error);
			} else {
				args [i] = arg_buf [i];
			}
		}
	}

	if (i < nargs)
		return err;

	if (invoke->flags & INVOKE_FLAG_DISABLE_BREAKPOINTS)
		tls->disable_breakpoints = TRUE;
	else
		tls->disable_breakpoints = FALSE;

	/* 
	 * Add an LMF frame to link the stack frames on the invoke method with our caller.
	 */
#ifdef MONO_ARCH_SOFT_DEBUG_SUPPORTED
	if (invoke->has_ctx) {
		/* Setup our lmf */
		memset (&ext, 0, sizeof (ext));
		ext.kind = MONO_LMFEXT_DEBUGGER_INVOKE;
		memcpy (&ext.ctx, &invoke->ctx, sizeof (MonoContext));

		mono_push_lmf (&ext);
	}
#endif

	mono_stopwatch_start (&watch);
	res = mono_runtime_try_invoke (m, m_class_is_valuetype (m->klass) ? (gpointer) this_buf : (gpointer) this_arg, args, &exc, error);
	if (!is_ok (error) && exc == NULL) {
		exc = (MonoObject*) mono_error_convert_to_exception (error);
	} else {
		mono_error_cleanup (error); /* FIXME report error */
	}
	mono_stopwatch_stop (&watch);
	DEBUG_PRINTF (1, "[%p] Invoke result: %p, exc: %s, time: %ld ms.\n", (gpointer) (gsize) mono_native_thread_id_get (), res, exc ? m_class_get_name (exc->vtable->klass) : NULL, (long)mono_stopwatch_elapsed_ms (&watch));
	if (exc) {
		buffer_add_byte (buf, 0);
		buffer_add_value (buf, mono_get_object_type (), &exc, domain);
	} else {
		gboolean out_this = FALSE;
		gboolean out_args = FALSE;

		if ((invoke->flags & INVOKE_FLAG_RETURN_OUT_THIS) && CHECK_PROTOCOL_VERSION (2, 35))
			out_this = TRUE;
		if ((invoke->flags & INVOKE_FLAG_RETURN_OUT_ARGS) && CHECK_PROTOCOL_VERSION (2, 35))
			out_args = TRUE;
		buffer_add_byte (buf, 1 + (out_this ? 2 : 0) + (out_args ? 4 : 0));
		if (m->string_ctor) {
			buffer_add_value (buf, m_class_get_byval_arg (mono_get_string_class ()), &res, domain);
		} else if (sig->ret->type == MONO_TYPE_VOID && !m->string_ctor) {
			if (!strcmp (m->name, ".ctor")) {
				if (!m_class_is_valuetype (m->klass))
					buffer_add_value (buf, mono_get_object_type (), &this_arg, domain);
				else
					buffer_add_value (buf, m_class_get_byval_arg (m->klass), this_buf, domain);
			} else {
				buffer_add_value (buf, mono_get_void_type (), NULL, domain);
			}
		} else if (MONO_TYPE_IS_REFERENCE (sig->ret)) {
			if (sig->ret->byref) {
				MonoType* ret_byval = m_class_get_byval_arg (mono_class_from_mono_type_internal (sig->ret));
				buffer_add_value (buf, ret_byval, &res, domain);
			} else {
				buffer_add_value (buf, sig->ret, &res, domain);
			}
		} else if (m_class_is_valuetype (mono_class_from_mono_type_internal (sig->ret)) || sig->ret->type == MONO_TYPE_PTR || sig->ret->type == MONO_TYPE_FNPTR) {
			if (mono_class_is_nullable (mono_class_from_mono_type_internal (sig->ret))) {
				MonoClass *k = mono_class_from_mono_type_internal (sig->ret);
				guint8 *nullable_buf = (guint8 *)g_alloca (mono_class_value_size (k, NULL));

				g_assert (nullable_buf);
				mono_nullable_init (nullable_buf, res, k);
				buffer_add_value (buf, sig->ret, nullable_buf, domain);
			} else {
				g_assert (res);

				if (sig->ret->byref) {
					MonoType* ret_byval = m_class_get_byval_arg (mono_class_from_mono_type_internal (sig->ret));
					buffer_add_value (buf, ret_byval, mono_object_unbox_internal (res), domain);
				} else {
					buffer_add_value (buf, sig->ret, mono_object_unbox_internal (res), domain);
				}
			}
		} else {
			NOT_IMPLEMENTED;
		}
		if (out_this)
			/* Return the new value of the receiver after the call */
			buffer_add_value (buf, m_class_get_byval_arg (m->klass), this_buf, domain);
		if (out_args) {
			buffer_add_int (buf, nargs);
			for (i = 0; i < nargs; ++i) {
				if (MONO_TYPE_IS_REFERENCE (sig->params [i]))
					buffer_add_value (buf, sig->params [i], &args [i], domain);
				else if (sig->params [i]->byref)
					/* add_value () does an indirection */
					buffer_add_value (buf, sig->params [i], &arg_buf [i], domain);
				else
					buffer_add_value (buf, sig->params [i], arg_buf [i], domain);
			}
		}
	}

	tls->disable_breakpoints = FALSE;

#ifdef MONO_ARCH_SOFT_DEBUG_SUPPORTED
	if (invoke->has_ctx)
		mono_pop_lmf ((MonoLMF*)&ext);
#endif

	*endp = p;
	// FIXME: byref arguments
	// FIXME: varargs
	return ERR_NONE;
}

/*
 * invoke_method:
 *
 *   Invoke the method given by tls->pending_invoke in the current thread.
 */
static void
invoke_method (void)
{
	DebuggerTlsData *tls;
	InvokeData *invoke;
	int id;
	int i, mindex;
	ErrorCode err;
	Buffer buf;
	MonoContext restore_ctx;
	guint8 *p;

	tls = (DebuggerTlsData *)mono_native_tls_get_value (debugger_tls_id);
	g_assert (tls);

	/*
	 * Store the `InvokeData *' in `tls->invoke' until we're done with
	 * the invocation, so CMD_VM_ABORT_INVOKE can check it.
	 */

	mono_loader_lock ();

	invoke = tls->pending_invoke;
	g_assert (invoke);
	tls->pending_invoke = NULL;

	invoke->last_invoke = tls->invoke;
	tls->invoke = invoke;

	mono_loader_unlock ();

	tls->frames_up_to_date = FALSE;

	id = invoke->id;

	p = invoke->p;
	err = ERR_NONE;
	for (mindex = 0; mindex < invoke->nmethods; ++mindex) {
		buffer_init (&buf, 128);

		if (err) {
			/* Fail the other invokes as well */
		} else {
			err = do_invoke_method (tls, &buf, invoke, p, &p);
		}

		if (tls->abort_requested) {
			if (CHECK_PROTOCOL_VERSION (2, 42))
				err = ERR_INVOKE_ABORTED;
		}

		/* Start suspending before sending the reply */
		if (mindex == invoke->nmethods - 1) {
			if (!(invoke->flags & INVOKE_FLAG_SINGLE_THREADED)) {
				for (i = 0; i < invoke->suspend_count; ++i)
					suspend_vm ();
			}
		}

		send_reply_packet (id, err, &buf);
	
		buffer_free (&buf);
	}

	memcpy (&restore_ctx, &invoke->ctx, sizeof (MonoContext));

	if (invoke->has_ctx)
		save_thread_context (&restore_ctx);

	if (invoke->flags & INVOKE_FLAG_SINGLE_THREADED) {
		g_assert (tls->resume_count);
		tls->resume_count -= invoke->suspend_count;
	}

	DEBUG_PRINTF (1, "[%p] Invoke finished (%d), resume_count = %d.\n", (gpointer) (gsize) mono_native_thread_id_get (), err, tls->resume_count);

	/*
	 * Take the loader lock to avoid race conditions with CMD_VM_ABORT_INVOKE:
	 *
	 * It is possible that mono_thread_internal_abort () was called
	 * after the mono_runtime_invoke_checked() already returned, but it doesn't matter
	 * because we reset the abort here.
	 */

	mono_loader_lock ();

	if (tls->abort_requested)
		mono_thread_internal_reset_abort (tls->thread);

	tls->invoke = tls->invoke->last_invoke;
	tls->abort_requested = FALSE;

	mono_loader_unlock ();

	g_free (invoke->p);
	g_free (invoke);
}

static gboolean
is_really_suspended (gpointer key, gpointer value, gpointer user_data)
{
	MonoThread *thread = (MonoThread *)value;
	DebuggerTlsData *tls;
	gboolean res;

	mono_loader_lock ();
	tls = (DebuggerTlsData *)mono_g_hash_table_lookup (thread_to_tls, thread);
	g_assert (tls);
	res = tls->really_suspended;
	mono_loader_unlock ();

	return res;
}

static GPtrArray*
get_source_files_for_type (MonoClass *klass)
{
	gpointer iter = NULL;
	MonoMethod *method;
	MonoDebugSourceInfo *sinfo;
	GPtrArray *files;
	int i, j;

	files = g_ptr_array_new ();

	while ((method = mono_class_get_methods (klass, &iter))) {
		MonoDebugMethodInfo *minfo = mono_debug_lookup_method (method);
		GPtrArray *source_file_list;

		if (minfo) {
			mono_debug_get_seq_points (minfo, NULL, &source_file_list, NULL, NULL, NULL);
			for (j = 0; j < source_file_list->len; ++j) {
				sinfo = (MonoDebugSourceInfo *)g_ptr_array_index (source_file_list, j);
				for (i = 0; i < files->len; ++i)
					if (!strcmp ((const char*)g_ptr_array_index (files, i), (const char*)sinfo->source_file))
						break;
				if (i == files->len)
					g_ptr_array_add (files, g_strdup (sinfo->source_file));
			}
			g_ptr_array_free (source_file_list, TRUE);
		}
	}

	return files;
}


typedef struct {
	MonoTypeNameParse *info;
	gboolean ignore_case;
	GPtrArray *res_classes;
	GPtrArray *res_domains;
} GetTypesArgs;

static void
get_types (gpointer key, gpointer value, gpointer user_data)
{
	MonoAssembly *ass;
	gboolean type_resolve;
	MonoType *t;
	GSList *tmp;
	MonoDomain *domain = (MonoDomain*)key;

	if (mono_domain_is_unloading (domain))
		return;

	MonoAssemblyLoadContext *alc = mono_domain_default_alc (domain);
	GetTypesArgs *ud = (GetTypesArgs*)user_data;

	mono_domain_assemblies_lock (domain);
	for (tmp = domain->domain_assemblies; tmp; tmp = tmp->next) {
		ass = (MonoAssembly *)tmp->data;

		if (ass->image) {
			ERROR_DECL (probe_type_error);
			/* FIXME really okay to call while holding locks? */
			t = mono_reflection_get_type_checked (alc, ass->image, ass->image, ud->info, ud->ignore_case, TRUE, &type_resolve, probe_type_error);
			mono_error_cleanup (probe_type_error);
			if (t) {
				g_ptr_array_add (ud->res_classes, mono_type_get_class_internal (t));
				g_ptr_array_add (ud->res_domains, domain);
			}
		}
	}
	mono_domain_assemblies_unlock (domain);
}

typedef struct {
	gboolean ignore_case;
	char *basename;
	GPtrArray *res_classes;
	GPtrArray *res_domains;
} GetTypesForSourceFileArgs;

static void
get_types_for_source_file (gpointer key, gpointer value, gpointer user_data)
{
	GHashTableIter iter;
	GSList *class_list = NULL;
	MonoClass *klass = NULL;
	GPtrArray *files = NULL;

	GetTypesForSourceFileArgs *ud = (GetTypesForSourceFileArgs*)user_data;
	MonoDomain *domain = (MonoDomain*)key;

	if (mono_domain_is_unloading (domain))
		return;

	AgentDomainInfo *info = (AgentDomainInfo *)domain_jit_info (domain)->agent_info;

	/* Update 'source_file_to_class' cache */
	g_hash_table_iter_init (&iter, info->loaded_classes);
	while (g_hash_table_iter_next (&iter, NULL, (void**)&klass)) {
		if (!g_hash_table_lookup (info->source_files, klass)) {
			files = get_source_files_for_type (klass);
			g_hash_table_insert (info->source_files, klass, files);

			for (int i = 0; i < files->len; ++i) {
				char *s = (char *)g_ptr_array_index (files, i);
				char *s2 = dbg_path_get_basename (s);
				char *s3;

				class_list = (GSList *)g_hash_table_lookup (info->source_file_to_class, s2);
				if (!class_list) {
					class_list = g_slist_prepend (class_list, klass);
					g_hash_table_insert (info->source_file_to_class, g_strdup (s2), class_list);
				} else {
					class_list = g_slist_prepend (class_list, klass);
					g_hash_table_insert (info->source_file_to_class, s2, class_list);
				}

				/* The _ignorecase hash contains the lowercase path */
				s3 = strdup_tolower (s2);
				class_list = (GSList *)g_hash_table_lookup (info->source_file_to_class_ignorecase, s3);
				if (!class_list) {
					class_list = g_slist_prepend (class_list, klass);
					g_hash_table_insert (info->source_file_to_class_ignorecase, g_strdup (s3), class_list);
				} else {
					class_list = g_slist_prepend (class_list, klass);
					g_hash_table_insert (info->source_file_to_class_ignorecase, s3, class_list);
				}

				g_free (s2);
				g_free (s3);
			}
		}
	}

	if (ud->ignore_case) {
		char *s;

		s = strdup_tolower (ud->basename);
		class_list = (GSList *)g_hash_table_lookup (info->source_file_to_class_ignorecase, s);
		g_free (s);
	} else {
		class_list = (GSList *)g_hash_table_lookup (info->source_file_to_class, ud->basename);
	}

	for (GSList *l = class_list; l; l = l->next) {
		klass = (MonoClass *)l->data;

		g_ptr_array_add (ud->res_classes, klass);
		g_ptr_array_add (ud->res_domains, domain);
	}
}

static void add_error_string (Buffer *buf, const char *str) 
{
	if (CHECK_PROTOCOL_VERSION (2, 56)) 
		buffer_add_string (buf, str);
}

static ErrorCode
vm_commands (int command, int id, guint8 *p, guint8 *end, Buffer *buf)
{
	switch (command) {
	case CMD_VM_VERSION: {
		char *build_info, *version;

		build_info = mono_get_runtime_build_info ();
		version = g_strdup_printf ("mono %s", build_info);

		buffer_add_string (buf, version); /* vm version */
		buffer_add_int (buf, MAJOR_VERSION);
		buffer_add_int (buf, MINOR_VERSION);
		g_free (build_info);
		g_free (version);
		break;
	}
	case CMD_VM_SET_PROTOCOL_VERSION: {
		major_version = decode_int (p, &p, end);
		minor_version = decode_int (p, &p, end);
		protocol_version_set = TRUE;
		DEBUG_PRINTF (1, "[dbg] Protocol version %d.%d, client protocol version %d.%d.\n", MAJOR_VERSION, MINOR_VERSION, major_version, minor_version);
		break;
	}
	case CMD_VM_ALL_THREADS: {
		// FIXME: Domains
		gboolean remove_gc_finalizing = FALSE;
		mono_loader_lock ();
		int count = mono_g_hash_table_size (tid_to_thread_obj);
		mono_g_hash_table_foreach (tid_to_thread_obj, count_thread_check_gc_finalizer, &remove_gc_finalizing);
		if (remove_gc_finalizing)
			count--;
		buffer_add_int (buf, count);
		mono_g_hash_table_foreach (tid_to_thread_obj, add_thread, buf);
		
		mono_loader_unlock ();
		break;
	}
	case CMD_VM_SUSPEND:
		suspend_vm ();
		wait_for_suspend ();
		break;
	case CMD_VM_RESUME:
		if (suspend_count == 0)
			return ERR_NOT_SUSPENDED;
		resume_vm ();
		clear_suspended_objs ();
		break;
	case CMD_VM_DISPOSE:
		dispose_vm ();
		break;
	case CMD_VM_EXIT: {
		MonoInternalThread *thread;
		DebuggerTlsData *tls;
#ifdef TRY_MANAGED_SYSTEM_ENVIRONMENT_EXIT
		MonoClass *env_class;
#endif
		MonoMethod *exit_method = NULL;
		gpointer *args;
		int exit_code;

		exit_code = decode_int (p, &p, end);

		// FIXME: What if there is a VM_DEATH event request with SUSPEND_ALL ?

		/* Have to send a reply before exiting */
		send_reply_packet (id, 0, buf);

		/* Clear all event requests */
		mono_loader_lock ();
		while (event_requests->len > 0) {
			EventRequest *req = (EventRequest *)g_ptr_array_index (event_requests, 0);

			clear_event_request (req->id, req->event_kind);
		}
		mono_loader_unlock ();

		/*
		 * The JDWP documentation says that the shutdown is not orderly. It doesn't
		 * specify whenever a VM_DEATH event is sent. We currently do an orderly
		 * shutdown by hijacking a thread to execute Environment.Exit (). This is
		 * better than doing the shutdown ourselves, since it avoids various races.
		 */

		suspend_vm ();
		wait_for_suspend ();

#ifdef TRY_MANAGED_SYSTEM_ENVIRONMENT_EXIT
		env_class = mono_class_try_load_from_name (mono_defaults.corlib, "System", "Environment");
		if (env_class) {
			ERROR_DECL (error);
			exit_method = mono_class_get_method_from_name_checked (env_class, "Exit", 1, 0, error);
			mono_error_assert_ok (error);
		}
#endif

		mono_loader_lock ();
		thread = (MonoInternalThread *)mono_g_hash_table_find (tid_to_thread, is_really_suspended, NULL);
		mono_loader_unlock ();

		if (thread && exit_method) {
			mono_loader_lock ();
			tls = (DebuggerTlsData *)mono_g_hash_table_lookup (thread_to_tls, thread);
			mono_loader_unlock ();

			args = g_new0 (gpointer, 1);
			args [0] = g_malloc (sizeof (int));
			*(int*)(args [0]) = exit_code;

			tls->pending_invoke = g_new0 (InvokeData, 1);
			tls->pending_invoke->method = exit_method;
			tls->pending_invoke->args = args;
			tls->pending_invoke->nmethods = 1;

			while (suspend_count > 0)
				resume_vm ();
		} else {
			/* 
			 * No thread found, do it ourselves.
			 * FIXME: This can race with normal shutdown etc.
			 */
			while (suspend_count > 0)
				resume_vm ();

			if (!mono_runtime_try_shutdown ())
				break;

			mono_environment_exitcode_set (exit_code);

			/* Suspend all managed threads since the runtime is going away */
#ifndef ENABLE_NETCORE
			DEBUG_PRINTF (1, "Suspending all threads...\n");
			mono_thread_suspend_all_other_threads ();
#endif
			DEBUG_PRINTF (1, "Shutting down the runtime...\n");
			mono_runtime_quit_internal ();
			transport_close2 ();
			DEBUG_PRINTF (1, "Exiting...\n");

			exit (exit_code);
		}
		break;
	}		
	case CMD_VM_INVOKE_METHOD:
	case CMD_VM_INVOKE_METHODS: {
		int objid = decode_objid (p, &p, end);
		MonoThread *thread;
		DebuggerTlsData *tls;
		int i, count, flags, nmethods;
		ErrorCode err;

		err = get_object (objid, (MonoObject**)&thread);
		if (err != ERR_NONE)
			return err;

		flags = decode_int (p, &p, end);

		if (command == CMD_VM_INVOKE_METHODS)
			nmethods = decode_int (p, &p, end);
		else
			nmethods = 1;

		// Wait for suspending if it already started
		if (suspend_count)
			wait_for_suspend ();
		if (!is_suspended ())
			return ERR_NOT_SUSPENDED;

		mono_loader_lock ();
		tls = (DebuggerTlsData *)mono_g_hash_table_lookup (thread_to_tls, THREAD_TO_INTERNAL (thread));
		mono_loader_unlock ();
		g_assert (tls);

		if (!tls->really_suspended)
			/* The thread is still running native code, can't do invokes */
			return ERR_NOT_SUSPENDED;

		/* 
		 * Store the invoke data into tls, the thread will execute it after it is
		 * resumed.
		 */
		if (tls->pending_invoke)
			return ERR_NOT_SUSPENDED;
		tls->pending_invoke = g_new0 (InvokeData, 1);
		tls->pending_invoke->id = id;
		tls->pending_invoke->flags = flags;
		tls->pending_invoke->p = (guint8 *)g_malloc (end - p);
		memcpy (tls->pending_invoke->p, p, end - p);
		tls->pending_invoke->endp = tls->pending_invoke->p + (end - p);
		tls->pending_invoke->suspend_count = suspend_count;
		tls->pending_invoke->nmethods = nmethods;

		if (flags & INVOKE_FLAG_SINGLE_THREADED) {
			resume_thread (THREAD_TO_INTERNAL (thread));
		}
		else {
			count = suspend_count;
			for (i = 0; i < count; ++i)
				resume_vm ();
		}
		break;
	}
	case CMD_VM_ABORT_INVOKE: {
		int objid = decode_objid (p, &p, end);
		MonoThread *thread;
		DebuggerTlsData *tls;
		int invoke_id;
		ErrorCode err;

		err = get_object (objid, (MonoObject**)&thread);
		if (err != ERR_NONE)
			return err;

		invoke_id = decode_int (p, &p, end);

		mono_loader_lock ();
		tls = (DebuggerTlsData *)mono_g_hash_table_lookup (thread_to_tls, THREAD_TO_INTERNAL (thread));
		g_assert (tls);

		if (tls->abort_requested) {
			DEBUG_PRINTF (1, "Abort already requested.\n");
			mono_loader_unlock ();
			break;
		}

		/*
		 * Check whether we're still inside the mono_runtime_invoke_checked() and that it's
		 * actually the correct invocation.
		 *
		 * Careful, we do not stop the thread that's doing the invocation, so we can't
		 * inspect its stack.  However, invoke_method() also acquires the loader lock
		 * when it's done, so we're safe here.
		 *
		 */

		if (!tls->invoke || (tls->invoke->id != invoke_id)) {
			mono_loader_unlock ();
			return ERR_NO_INVOCATION;
		}

		tls->abort_requested = TRUE;

		mono_thread_internal_abort (THREAD_TO_INTERNAL (thread), FALSE);
		mono_loader_unlock ();
		break;
	}

	case CMD_VM_SET_KEEPALIVE: {
		int timeout = decode_int (p, &p, end);
		agent_config.keepalive = timeout;
		// FIXME:
#ifndef DISABLE_SOCKET_TRANSPORT
		set_keepalive ();
#else
		NOT_IMPLEMENTED;
#endif
		break;
	}
	case CMD_VM_GET_TYPES_FOR_SOURCE_FILE: {
		int i;
		char *fname, *basename;
		gboolean ignore_case;
		GPtrArray *res_classes, *res_domains;

		fname = decode_string (p, &p, end);
		ignore_case = decode_byte (p, &p, end);

		basename = dbg_path_get_basename (fname);

		res_classes = g_ptr_array_new ();
		res_domains = g_ptr_array_new ();

		mono_loader_lock ();
		GetTypesForSourceFileArgs args;
		memset (&args, 0, sizeof (args));
		args.ignore_case = ignore_case;
		args.basename = basename;
		args.res_classes  = res_classes;
		args.res_domains = res_domains;
		mono_de_foreach_domain (get_types_for_source_file, &args);
		mono_loader_unlock ();

		g_free (fname);
		g_free (basename);

		buffer_add_int (buf, res_classes->len);
		for (i = 0; i < res_classes->len; ++i)
			buffer_add_typeid (buf, (MonoDomain *)g_ptr_array_index (res_domains, i), (MonoClass *)g_ptr_array_index (res_classes, i));
		g_ptr_array_free (res_classes, TRUE);
		g_ptr_array_free (res_domains, TRUE);
		break;
	}
	case CMD_VM_GET_TYPES: {
		ERROR_DECL (error);
		int i;
		char *name;
		gboolean ignore_case;
		GPtrArray *res_classes, *res_domains;
		MonoTypeNameParse info;

		name = decode_string (p, &p, end);
		ignore_case = decode_byte (p, &p, end);

		if (!mono_reflection_parse_type_checked (name, &info, error)) {
			add_error_string (buf, mono_error_get_message (error));
			mono_error_cleanup (error);
			g_free (name);
			mono_reflection_free_type_info (&info);
			return ERR_INVALID_ARGUMENT;
		}

		res_classes = g_ptr_array_new ();
		res_domains = g_ptr_array_new ();

		mono_loader_lock ();

		GetTypesArgs args;
		memset (&args, 0, sizeof (args));
		args.info = &info;
		args.ignore_case = ignore_case;
		args.res_classes = res_classes;
		args.res_domains = res_domains;

		mono_de_foreach_domain (get_types, &args);

		mono_loader_unlock ();

		g_free (name);
		mono_reflection_free_type_info (&info);

		buffer_add_int (buf, res_classes->len);
		for (i = 0; i < res_classes->len; ++i)
			buffer_add_typeid (buf, (MonoDomain *)g_ptr_array_index (res_domains, i), (MonoClass *)g_ptr_array_index (res_classes, i));
		g_ptr_array_free (res_classes, TRUE);
		g_ptr_array_free (res_domains, TRUE);
		break;
	}
	case CMD_VM_START_BUFFERING:
	case CMD_VM_STOP_BUFFERING:
		/* Handled in the main loop */
		break;
	default:
		return ERR_NOT_IMPLEMENTED;
	}

	return ERR_NONE;
}

static ErrorCode
event_commands (int command, guint8 *p, guint8 *end, Buffer *buf)
{
	ErrorCode err;
	ERROR_DECL (error);

	switch (command) {
	case CMD_EVENT_REQUEST_SET: {
		EventRequest *req;
		int i, event_kind, suspend_policy, nmodifiers;
		ModifierKind mod;
		MonoMethod *method;
		long location = 0;
		MonoThread *step_thread;
		int step_thread_id = 0;
		StepDepth depth = STEP_DEPTH_INTO;
		StepSize size = STEP_SIZE_MIN;
		StepFilter filter = STEP_FILTER_NONE;
		MonoDomain *domain;
		Modifier *modifier;

		event_kind = decode_byte (p, &p, end);
		suspend_policy = decode_byte (p, &p, end);
		nmodifiers = decode_byte (p, &p, end);

		req = (EventRequest *)g_malloc0 (sizeof (EventRequest) + (nmodifiers * sizeof (Modifier)));
		req->id = mono_atomic_inc_i32 (&event_request_id);
		req->event_kind = event_kind;
		req->suspend_policy = suspend_policy;
		req->nmodifiers = nmodifiers;

		method = NULL;
		for (i = 0; i < nmodifiers; ++i) {
			mod = (ModifierKind)decode_byte (p, &p, end);

			req->modifiers [i].kind = mod;
			if (mod == MOD_KIND_COUNT) {
				req->modifiers [i].data.count = decode_int (p, &p, end);
			} else if (mod == MOD_KIND_LOCATION_ONLY) {
				method = decode_methodid (p, &p, end, &domain, &err);
				if (err != ERR_NONE)
					return err;
				location = decode_long (p, &p, end);
			} else if (mod == MOD_KIND_STEP) {
				step_thread_id = decode_id (p, &p, end);
				size = (StepSize)decode_int (p, &p, end);
				depth = (StepDepth)decode_int (p, &p, end);
				if (CHECK_PROTOCOL_VERSION (2, 16))
					filter = (StepFilter)decode_int (p, &p, end);
				req->modifiers [i].data.filter = filter;
				if (!CHECK_PROTOCOL_VERSION (2, 26) && (req->modifiers [i].data.filter & STEP_FILTER_DEBUGGER_HIDDEN))
					/* Treat STEP_THOUGH the same as HIDDEN */
					req->modifiers [i].data.filter = (StepFilter)(req->modifiers [i].data.filter | STEP_FILTER_DEBUGGER_STEP_THROUGH);
			} else if (mod == MOD_KIND_THREAD_ONLY) {
				int id = decode_id (p, &p, end);

				err = get_object (id, (MonoObject**)&req->modifiers [i].data.thread);
				if (err != ERR_NONE) {
					g_free (req);
					return err;
				}
			} else if (mod == MOD_KIND_EXCEPTION_ONLY) {
				MonoClass *exc_class = decode_typeid (p, &p, end, &domain, &err);

				if (err != ERR_NONE)
					return err;
				req->modifiers [i].caught = decode_byte (p, &p, end);
				req->modifiers [i].uncaught = decode_byte (p, &p, end);
				if (CHECK_PROTOCOL_VERSION (2, 25))
					req->modifiers [i].subclasses = decode_byte (p, &p, end);
				else
					req->modifiers [i].subclasses = TRUE;
				if (exc_class) {
					req->modifiers [i].data.exc_class = exc_class;

					if (!mono_class_is_assignable_from_internal (mono_defaults.exception_class, exc_class)) {
						g_free (req);
						return ERR_INVALID_ARGUMENT;
					}
				}
				if (CHECK_PROTOCOL_VERSION (2, 54)) {
					req->modifiers [i].not_filtered_feature = decode_byte (p, &p, end);
					req->modifiers [i].everything_else  = decode_byte (p, &p, end);
					DEBUG_PRINTF (1, "[dbg] \tEXCEPTION_ONLY 2 filter (%s%s%s%s).\n", exc_class ? m_class_get_name (exc_class) : (req->modifiers [i].everything_else ? "everything else" : "all"), req->modifiers [i].caught ? ", caught" : "", req->modifiers [i].uncaught ? ", uncaught" : "", req->modifiers [i].subclasses ? ", include-subclasses" : "");
				} else {
					req->modifiers [i].not_filtered_feature = FALSE;
					req->modifiers [i].everything_else = FALSE;
					DEBUG_PRINTF (1, "[dbg] \tEXCEPTION_ONLY filter (%s%s%s%s).\n", exc_class ? m_class_get_name (exc_class) : "all", req->modifiers [i].caught ? ", caught" : "", req->modifiers [i].uncaught ? ", uncaught" : "", req->modifiers [i].subclasses ? ", include-subclasses" : "");
				}
				
			} else if (mod == MOD_KIND_ASSEMBLY_ONLY) {
				int n = decode_int (p, &p, end);
				int j;

				// +1 because we don't know length and we use last element to check for end
				req->modifiers [i].data.assemblies = g_new0 (MonoAssembly*, n + 1);
				for (j = 0; j < n; ++j) {
					req->modifiers [i].data.assemblies [j] = decode_assemblyid (p, &p, end, &domain, &err);
					if (err != ERR_NONE) {
						g_free (req->modifiers [i].data.assemblies);
						return err;
					}
				}
			} else if (mod == MOD_KIND_SOURCE_FILE_ONLY) {
				int n = decode_int (p, &p, end);
				int j;

				modifier = &req->modifiers [i];
				modifier->data.source_files = g_hash_table_new (g_str_hash, g_str_equal);
				for (j = 0; j < n; ++j) {
					char *s = decode_string (p, &p, end);
					char *s2;

					if (s) {
						s2 = strdup_tolower (s);
						g_hash_table_insert (modifier->data.source_files, s2, s2);
						g_free (s);
					}
				}
			} else if (mod == MOD_KIND_TYPE_NAME_ONLY) {
				int n = decode_int (p, &p, end);
				int j;

				modifier = &req->modifiers [i];
				modifier->data.type_names = g_hash_table_new (g_str_hash, g_str_equal);
				for (j = 0; j < n; ++j) {
					char *s = decode_string (p, &p, end);

					if (s)
						g_hash_table_insert (modifier->data.type_names, s, s);
				}
			} else {
				g_free (req);
				return ERR_NOT_IMPLEMENTED;
			}
		}

		if (req->event_kind == EVENT_KIND_BREAKPOINT) {
			g_assert (method);

			req->info = mono_de_set_breakpoint (method, location, req, error);
			if (!is_ok (error)) {
				g_free (req);
				DEBUG_PRINTF (1, "[dbg] Failed to set breakpoint: %s\n", mono_error_get_message (error));
				mono_error_cleanup (error);
				return ERR_NO_SEQ_POINT_AT_IL_OFFSET;
			}
		} else if (req->event_kind == EVENT_KIND_STEP) {
			g_assert (step_thread_id);

			err = get_object (step_thread_id, (MonoObject**)&step_thread);
			if (err != ERR_NONE) {
				g_free (req);
				return err;
			}
			
			mono_loader_lock ();
			DebuggerTlsData *tls = (DebuggerTlsData *)mono_g_hash_table_lookup (thread_to_tls, THREAD_TO_INTERNAL(step_thread));
			mono_loader_unlock ();
			g_assert (tls);
		
			if (tls->terminated) { 
				/* if the thread is already terminated ignore the single step */
				buffer_add_int (buf, req->id);
				return ERR_NONE;
			}

			err = (ErrorCode)mono_de_ss_create (THREAD_TO_INTERNAL (step_thread), size, depth, filter, req);
			if (err != ERR_NONE) {
				g_free (req);
				return err;
			}
		} else if (req->event_kind == EVENT_KIND_METHOD_ENTRY) {
			req->info = mono_de_set_breakpoint (NULL, METHOD_ENTRY_IL_OFFSET, req, NULL);
		} else if (req->event_kind == EVENT_KIND_METHOD_EXIT) {
			req->info = mono_de_set_breakpoint (NULL, METHOD_EXIT_IL_OFFSET, req, NULL);
		} else if (req->event_kind == EVENT_KIND_EXCEPTION) {
		} else if (req->event_kind == EVENT_KIND_TYPE_LOAD) {
		} else {
			if (req->nmodifiers) {
				g_free (req);
				return ERR_NOT_IMPLEMENTED;
			}
		}

		mono_loader_lock ();
		g_ptr_array_add (event_requests, req);
		
		if (agent_config.defer) {
			/* Transmit cached data to the client on receipt of the event request */
			switch (req->event_kind) {
			case EVENT_KIND_APPDOMAIN_CREATE:
				/* Emit load events for currently loaded domains */
				mono_de_foreach_domain (emit_appdomain_load, NULL);
				break;
			case EVENT_KIND_ASSEMBLY_LOAD:
				/* Emit load events for currently loaded assemblies */
				mono_domain_foreach (send_assemblies_for_domain, NULL);
				break;
			case EVENT_KIND_THREAD_START:
				/* Emit start events for currently started threads */
				mono_g_hash_table_foreach (tid_to_thread, emit_thread_start, NULL);
				break;
			case EVENT_KIND_TYPE_LOAD:
				/* Emit type load events for currently loaded types */
				mono_domain_foreach (send_types_for_domain, NULL);
				break;
			default:
				break;
			}
		}
		mono_loader_unlock ();

		buffer_add_int (buf, req->id);
		break;
	}
	case CMD_EVENT_REQUEST_CLEAR: {
		int etype = decode_byte (p, &p, end);
		int req_id = decode_int (p, &p, end);

		// FIXME: Make a faster mapping from req_id to request
		mono_loader_lock ();
		clear_event_request (req_id, etype);
		mono_loader_unlock ();
		break;
	}
	case CMD_EVENT_REQUEST_CLEAR_ALL_BREAKPOINTS: {
		int i;

		mono_loader_lock ();
		i = 0;
		while (i < event_requests->len) {
			EventRequest *req = (EventRequest *)g_ptr_array_index (event_requests, i);

			if (req->event_kind == EVENT_KIND_BREAKPOINT) {
				mono_de_clear_breakpoint ((MonoBreakpoint *)req->info);

				g_ptr_array_remove_index_fast (event_requests, i);
				g_free (req);
			} else {
				i ++;
			}
		}
		mono_loader_unlock ();
		break;
	}
	default:
		return ERR_NOT_IMPLEMENTED;
	}

	return ERR_NONE;
}

static ErrorCode
domain_commands (int command, guint8 *p, guint8 *end, Buffer *buf)
{
	ErrorCode err;
	MonoDomain *domain;

	switch (command) {
	case CMD_APPDOMAIN_GET_ROOT_DOMAIN: {
		buffer_add_domainid (buf, mono_get_root_domain ());
		break;
	}
	case CMD_APPDOMAIN_GET_FRIENDLY_NAME: {
		domain = decode_domainid (p, &p, end, NULL, &err);
		if (err != ERR_NONE)
			return err;
		buffer_add_string (buf, domain->friendly_name);
		break;
	}
	case CMD_APPDOMAIN_GET_ASSEMBLIES: {
		GSList *tmp;
		MonoAssembly *ass;
		int count;

		domain = decode_domainid (p, &p, end, NULL, &err);
		if (err != ERR_NONE)
			return err;
		mono_domain_assemblies_lock (domain);
		count = 0;
		for (tmp = domain->domain_assemblies; tmp; tmp = tmp->next) {
			count ++;
		}
		buffer_add_int (buf, count);
		for (tmp = domain->domain_assemblies; tmp; tmp = tmp->next) {
			ass = (MonoAssembly *)tmp->data;
			buffer_add_assemblyid (buf, domain, ass);
		}
		mono_domain_assemblies_unlock (domain);
		break;
	}
	case CMD_APPDOMAIN_GET_ENTRY_ASSEMBLY: {
		domain = decode_domainid (p, &p, end, NULL, &err);
		if (err != ERR_NONE)
			return err;

		buffer_add_assemblyid (buf, domain, domain->entry_assembly);
		break;
	}
	case CMD_APPDOMAIN_GET_CORLIB: {
		domain = decode_domainid (p, &p, end, NULL, &err);
		if (err != ERR_NONE)
			return err;

		buffer_add_assemblyid (buf, domain, m_class_get_image (domain->domain->mbr.obj.vtable->klass)->assembly);
		break;
	}
	case CMD_APPDOMAIN_CREATE_STRING: {
		char *s;
		MonoString *o;
		ERROR_DECL (error);

		domain = decode_domainid (p, &p, end, NULL, &err);
		if (err != ERR_NONE)
			return err;
		s = decode_string (p, &p, end);

		o = mono_string_new_checked (domain, s, error);
		if (!is_ok (error)) {
			DEBUG_PRINTF (1, "[dbg] Failed to allocate String object '%s': %s\n", s, mono_error_get_message (error));
			mono_error_cleanup (error);
			return ERR_INVALID_OBJECT;
		}
		buffer_add_objid (buf, (MonoObject*)o);
		break;
	}
	case CMD_APPDOMAIN_CREATE_BYTE_ARRAY: {
		ERROR_DECL (error);
		MonoArray *arr;
		gpointer elem;
		domain = decode_domainid (p, &p, end, NULL, &err);
		uintptr_t size = 0;
		int len = decode_int (p, &p, end);
		size = len;
		arr = mono_array_new_full_checked (mono_domain_get (), mono_class_create_array (mono_get_byte_class(), 1), &size, NULL, error);
		elem = mono_array_addr_internal (arr, guint8, 0);
		memcpy (elem, p, len);
		p += len;
		buffer_add_objid (buf, (MonoObject*) arr);
		break;
	}
	case CMD_APPDOMAIN_CREATE_BOXED_VALUE: {
		ERROR_DECL (error);
		MonoClass *klass;
		MonoDomain *domain2;
		MonoObject *o;

		domain = decode_domainid (p, &p, end, NULL, &err);
		if (err != ERR_NONE)
			return err;
		klass = decode_typeid (p, &p, end, &domain2, &err);
		if (err != ERR_NONE)
			return err;

		// FIXME:
		g_assert (domain == domain2);

		o = mono_object_new_checked (domain, klass, error);
		mono_error_assert_ok (error);

		err = decode_value (m_class_get_byval_arg (klass), domain, (guint8 *)mono_object_unbox_internal (o), p, &p, end, TRUE);
		if (err != ERR_NONE)
			return err;

		buffer_add_objid (buf, o);
		break;
	}
	default:
		return ERR_NOT_IMPLEMENTED;
	}

	return ERR_NONE;
}

static ErrorCode
get_assembly_object_command (MonoDomain *domain, MonoAssembly *ass, Buffer *buf, MonoError *error)
{
	HANDLE_FUNCTION_ENTER();
	ErrorCode err = ERR_NONE;
	error_init (error);
	MonoReflectionAssemblyHandle o = mono_assembly_get_object_handle (domain, ass, error);
	if (MONO_HANDLE_IS_NULL (o)) {
		err = ERR_INVALID_OBJECT;
		goto leave;
	}
	buffer_add_objid (buf, MONO_HANDLE_RAW (MONO_HANDLE_CAST (MonoObject, o)));
leave:
	HANDLE_FUNCTION_RETURN_VAL (err);
}


static ErrorCode
assembly_commands (int command, guint8 *p, guint8 *end, Buffer *buf)
{
	ErrorCode err;
	MonoAssembly *ass;
	MonoDomain *domain;

	ass = decode_assemblyid (p, &p, end, &domain, &err);
	if (err != ERR_NONE)
		return err;

	switch (command) {
	case CMD_ASSEMBLY_GET_LOCATION: {
		buffer_add_string (buf, mono_image_get_filename (ass->image));
		break;			
	}
	case CMD_ASSEMBLY_GET_ENTRY_POINT: {
		guint32 token;
		MonoMethod *m;

		if (ass->image->dynamic) {
			buffer_add_id (buf, 0);
		} else {
			token = mono_image_get_entry_point (ass->image);
			if (token == 0) {
				buffer_add_id (buf, 0);
			} else {
				ERROR_DECL (error);
				m = mono_get_method_checked (ass->image, token, NULL, NULL, error);
				if (!m)
					mono_error_cleanup (error); /* FIXME don't swallow the error */
				buffer_add_methodid (buf, domain, m);
			}
		}
		break;			
	}
	case CMD_ASSEMBLY_GET_MANIFEST_MODULE: {
		buffer_add_moduleid (buf, domain, ass->image);
		break;
	}
	case CMD_ASSEMBLY_GET_OBJECT: {
		ERROR_DECL (error);
		err = get_assembly_object_command (domain, ass, buf, error);
		mono_error_cleanup (error);
		return err;
	}
	case CMD_ASSEMBLY_GET_DOMAIN: {
		buffer_add_domainid (buf, domain);
		break;
	}
	case CMD_ASSEMBLY_GET_TYPE: {
		ERROR_DECL (error);
		char *s = decode_string (p, &p, end);
		char* original_s = g_strdup_printf ("\"%s\"", s);

		gboolean ignorecase = decode_byte (p, &p, end);
		MonoTypeNameParse info;
		MonoType *t;
		gboolean type_resolve, res;
		MonoDomain *d = mono_domain_get ();
		MonoAssemblyLoadContext *alc = mono_domain_default_alc (d);

		/* This is needed to be able to find referenced assemblies */
		res = mono_domain_set_fast (domain, FALSE);
		g_assert (res);

		if (!mono_reflection_parse_type_checked (s, &info, error)) {
			mono_error_cleanup (error);
			t = NULL;
		} else {
			if (info.assembly.name) {
				mono_reflection_free_type_info (&info);
				g_free (s);
				mono_domain_set_fast (d, TRUE);
				char* error_msg =  g_strdup_printf ("Unexpected assembly-qualified type %s was provided", original_s);
				add_error_string (buf, error_msg);
				g_free (error_msg);
				g_free (original_s);
				return ERR_INVALID_ARGUMENT;
			}
			t = mono_reflection_get_type_checked (alc, ass->image, ass->image, &info, ignorecase, TRUE, &type_resolve, error);
			if (!is_ok (error)) {
				mono_error_cleanup (error); /* FIXME don't swallow the error */
				mono_reflection_free_type_info (&info);
				g_free (s);
				mono_domain_set_fast (d, TRUE);
				char* error_msg =  g_strdup_printf ("Invalid type name %s", original_s);
				add_error_string (buf, error_msg);
				g_free (error_msg);
				g_free (original_s);
				return ERR_INVALID_ARGUMENT;
			}
		}
		buffer_add_typeid (buf, domain, t ? mono_class_from_mono_type_internal (t) : NULL);
		mono_reflection_free_type_info (&info);
		g_free (s);
		g_free (original_s);
		mono_domain_set_fast (d, TRUE);

		break;
	}
	case CMD_ASSEMBLY_GET_NAME: {
		gchar *name;
		MonoAssembly *mass = ass;

		name = g_strdup_printf (
		  "%s, Version=%d.%d.%d.%d, Culture=%s, PublicKeyToken=%s%s",
		  mass->aname.name,
		  mass->aname.major, mass->aname.minor, mass->aname.build, mass->aname.revision,
		  mass->aname.culture && *mass->aname.culture? mass->aname.culture: "neutral",
		  mass->aname.public_key_token [0] ? (char *)mass->aname.public_key_token : "null",
		  (mass->aname.flags & ASSEMBLYREF_RETARGETABLE_FLAG) ? ", Retargetable=Yes" : "");

		buffer_add_string (buf, name);
		g_free (name);
		break;
	}
    case CMD_ASSEMBLY_GET_METADATA_BLOB: {
        MonoImage* image = ass->image;
        if (ass->dynamic) {
            return ERR_NOT_IMPLEMENTED;
        }
        buffer_add_byte_array (buf, (guint8*)image->raw_data, image->raw_data_len);
        break;
    }
    case CMD_ASSEMBLY_GET_IS_DYNAMIC: {
        buffer_add_byte (buf, ass->dynamic);
        break;
    }
    case CMD_ASSEMBLY_GET_PDB_BLOB: {
        MonoImage* image = ass->image;
        MonoDebugHandle* handle = mono_debug_get_handle (image); 
        if (!handle) {
            return ERR_INVALID_ARGUMENT;
        }
        MonoPPDBFile* ppdb = handle->ppdb;
        if (ppdb) {
            image = mono_ppdb_get_image (ppdb);
            buffer_add_byte_array (buf, (guint8*)image->raw_data, image->raw_data_len);
        } else {
            buffer_add_byte_array (buf, NULL, 0);
        }
        break;
    }
    case CMD_ASSEMBLY_GET_TYPE_FROM_TOKEN: {
        if (ass->dynamic) {
            return ERR_NOT_IMPLEMENTED;
        }
        guint32 token = decode_int (p, &p, end);
        ERROR_DECL (error);
        error_init (error);
        MonoClass* mono_class = mono_class_get_checked (ass->image, token, error);
        if (!is_ok (error)) {
            add_error_string (buf, mono_error_get_message (error));
            mono_error_cleanup (error);
            return ERR_INVALID_ARGUMENT;
        }
        buffer_add_typeid (buf, domain, mono_class);
        mono_error_cleanup (error);
        break;
    }
    case CMD_ASSEMBLY_GET_METHOD_FROM_TOKEN: {
        if (ass->dynamic) {
            return ERR_NOT_IMPLEMENTED;
        }
        guint32 token = decode_int (p, &p, end);
        ERROR_DECL (error);
        error_init (error);
        MonoMethod* mono_method = mono_get_method_checked (ass->image, token, NULL, NULL, error);
        if (!is_ok (error)) {
            add_error_string (buf, mono_error_get_message (error));
            mono_error_cleanup (error);
            return ERR_INVALID_ARGUMENT;
        }
        buffer_add_methodid (buf, domain, mono_method);
        mono_error_cleanup (error);
        break;
    }
	case CMD_ASSEMBLY_HAS_DEBUG_INFO: {
		buffer_add_byte (buf, !ass->dynamic && mono_debug_image_has_debug_info (ass->image));
		break;
	}
	default:
		return ERR_NOT_IMPLEMENTED;
	}

	return ERR_NONE;
}

static ErrorCode
module_commands (int command, guint8 *p, guint8 *end, Buffer *buf)
{
	ErrorCode err;
	MonoDomain *domain;

	switch (command) {
	case CMD_MODULE_GET_INFO: {
		MonoImage *image = decode_moduleid (p, &p, end, &domain, &err);
		char *basename, *sourcelink = NULL;

		if (CHECK_PROTOCOL_VERSION (2, 48))
			sourcelink = mono_debug_image_get_sourcelink (image);

		basename = g_path_get_basename (image->name);
		buffer_add_string (buf, basename); // name
		buffer_add_string (buf, image->module_name); // scopename
		buffer_add_string (buf, image->name); // fqname
		buffer_add_string (buf, mono_image_get_guid (image)); // guid
		buffer_add_assemblyid (buf, domain, image->assembly); // assembly
		if (CHECK_PROTOCOL_VERSION (2, 48))
			buffer_add_string (buf, sourcelink);
		g_free (basename);
		g_free (sourcelink);
		break;			
	}
	default:
		return ERR_NOT_IMPLEMENTED;
	}

	return ERR_NONE;
}

static ErrorCode
field_commands (int command, guint8 *p, guint8 *end, Buffer *buf)
{
	ErrorCode err;
	MonoDomain *domain;

	switch (command) {
	case CMD_FIELD_GET_INFO: {
		MonoClassField *f = decode_fieldid (p, &p, end, &domain, &err);

		buffer_add_string (buf, f->name);
		buffer_add_typeid (buf, domain, f->parent);
		buffer_add_typeid (buf, domain, mono_class_from_mono_type_internal (f->type));
		buffer_add_int (buf, f->type->attrs);
		break;
	}
	default:
		return ERR_NOT_IMPLEMENTED;
	}

	return ERR_NONE;
}

static void
buffer_add_cattr_arg (Buffer *buf, MonoType *t, MonoDomain *domain, MonoObject *val)
{
	if (val && val->vtable->klass == mono_defaults.runtimetype_class) {
		/* Special case these so the client doesn't have to handle Type objects */
		
		buffer_add_byte (buf, VALUE_TYPE_ID_TYPE);
		buffer_add_typeid (buf, domain, mono_class_from_mono_type_internal (((MonoReflectionType*)val)->type));
	} else if (MONO_TYPE_IS_REFERENCE (t))
		buffer_add_value (buf, t, &val, domain);
	else
		buffer_add_value (buf, t, mono_object_unbox_internal (val), domain);
}

static ErrorCode
buffer_add_cattrs (Buffer *buf, MonoDomain *domain, MonoImage *image, MonoClass *attr_klass, MonoCustomAttrInfo *cinfo)
{
	int i, j;
	int nattrs = 0;

	if (!cinfo) {
		buffer_add_int (buf, 0);
		return ERR_NONE;
	}

	SETUP_ICALL_FUNCTION;

	for (i = 0; i < cinfo->num_attrs; ++i) {
		if (!attr_klass || mono_class_has_parent (cinfo->attrs [i].ctor->klass, attr_klass))
			nattrs ++;
	}
	buffer_add_int (buf, nattrs);

	for (i = 0; i < cinfo->num_attrs; ++i) {
		MonoCustomAttrEntry *attr = &cinfo->attrs [i];
		if (!attr_klass || mono_class_has_parent (attr->ctor->klass, attr_klass)) {
			MonoArray *typed_args, *named_args;
			MonoArrayHandleOut typed_args_h, named_args_h;
			MonoObjectHandle val_h;
			MonoType *t;
			CattrNamedArg *arginfo = NULL;
			ERROR_DECL (error);

			SETUP_ICALL_FRAME;
			typed_args_h = MONO_HANDLE_NEW (MonoArray, NULL);
			named_args_h = MONO_HANDLE_NEW (MonoArray, NULL);
			val_h = MONO_HANDLE_NEW (MonoObject, NULL);

			mono_reflection_create_custom_attr_data_args (image, attr->ctor, attr->data, attr->data_size, typed_args_h, named_args_h, &arginfo, error);
			if (!is_ok (error)) {
				DEBUG_PRINTF (2, "[dbg] mono_reflection_create_custom_attr_data_args () failed with: '%s'\n", mono_error_get_message (error));
				mono_error_cleanup (error);
				CLEAR_ICALL_FRAME;
				return ERR_LOADER_ERROR;
			}
			typed_args = MONO_HANDLE_RAW (typed_args_h);
			named_args = MONO_HANDLE_RAW (named_args_h);

			buffer_add_methodid (buf, domain, attr->ctor);

			/* Ctor args */
			if (typed_args) {
				buffer_add_int (buf, mono_array_length_internal (typed_args));
				for (j = 0; j < mono_array_length_internal (typed_args); ++j) {
					MonoObject *val = mono_array_get_internal (typed_args, MonoObject*, j);
					MONO_HANDLE_ASSIGN_RAW (val_h, val);

					t = mono_method_signature_internal (attr->ctor)->params [j];

					buffer_add_cattr_arg (buf, t, domain, val);
				}
			} else {
				buffer_add_int (buf, 0);
			}

			/* Named args */
			if (named_args) {
				buffer_add_int (buf, mono_array_length_internal (named_args));

				for (j = 0; j < mono_array_length_internal (named_args); ++j) {
					MonoObject *val = mono_array_get_internal (named_args, MonoObject*, j);
					MONO_HANDLE_ASSIGN_RAW (val_h, val);

					if (arginfo [j].prop) {
						buffer_add_byte (buf, 0x54);
						buffer_add_propertyid (buf, domain, arginfo [j].prop);
					} else if (arginfo [j].field) {
						buffer_add_byte (buf, 0x53);
						buffer_add_fieldid (buf, domain, arginfo [j].field);
					} else {
						g_assert_not_reached ();
					}

					buffer_add_cattr_arg (buf, arginfo [j].type, domain, val);
				}
			} else {
				buffer_add_int (buf, 0);
			}
			g_free (arginfo);

			CLEAR_ICALL_FRAME;
		}
	}

	return ERR_NONE;
}

/* FIXME: Code duplication with icall.c */
static void
collect_interfaces (MonoClass *klass, GHashTable *ifaces, MonoError *error)
{
	int i;
	MonoClass *ic;

	mono_class_setup_interfaces (klass, error);
	if (!is_ok (error))
		return;

	int klass_interface_count = m_class_get_interface_count (klass);
	MonoClass **klass_interfaces = m_class_get_interfaces (klass);
	for (i = 0; i < klass_interface_count; i++) {
		ic = klass_interfaces [i];
		g_hash_table_insert (ifaces, ic, ic);

		collect_interfaces (ic, ifaces, error);
		if (!is_ok (error))
			return;
	}
}

static ErrorCode
type_commands_internal (int command, MonoClass *klass, MonoDomain *domain, guint8 *p, guint8 *end, Buffer *buf)
{
	HANDLE_FUNCTION_ENTER ();

	ERROR_DECL (error);
	MonoClass *nested;
	MonoType *type;
	gpointer iter;
	guint8 b;
	int nnested;
	ErrorCode err;
	char *name;
	MonoStringHandle string_handle = MONO_HANDLE_NEW (MonoString, NULL); // FIXME? Not always needed.

	switch (command) {
	case CMD_TYPE_GET_INFO: {
		buffer_add_string (buf, m_class_get_name_space (klass));
		buffer_add_string (buf, m_class_get_name (klass));
		// FIXME: byref
		name = mono_type_get_name_full (m_class_get_byval_arg (klass), MONO_TYPE_NAME_FORMAT_FULL_NAME);
		buffer_add_string (buf, name);
		g_free (name);
		buffer_add_assemblyid (buf, domain, m_class_get_image (klass)->assembly);
		buffer_add_moduleid (buf, domain, m_class_get_image (klass));
		buffer_add_typeid (buf, domain, m_class_get_parent (klass));
		if (m_class_get_rank (klass) || m_class_get_byval_arg (klass)->type == MONO_TYPE_PTR)
			buffer_add_typeid (buf, domain, m_class_get_element_class (klass));
		else
			buffer_add_id (buf, 0);
		buffer_add_int (buf, m_class_get_type_token (klass));
		buffer_add_byte (buf, m_class_get_rank (klass));
		buffer_add_int (buf, mono_class_get_flags (klass));
		b = 0;
		type = m_class_get_byval_arg (klass);
		// FIXME: Can't decide whenever a class represents a byref type
		if (FALSE)
			b |= (1 << 0);
		if (type->type == MONO_TYPE_PTR)
			b |= (1 << 1);
		if (!type->byref && (((type->type >= MONO_TYPE_BOOLEAN) && (type->type <= MONO_TYPE_R8)) || (type->type == MONO_TYPE_I) || (type->type == MONO_TYPE_U)))
			b |= (1 << 2);
		if (type->type == MONO_TYPE_VALUETYPE)
			b |= (1 << 3);
		if (m_class_is_enumtype (klass))
			b |= (1 << 4);
		if (mono_class_is_gtd (klass))
			b |= (1 << 5);
		if (mono_class_is_gtd (klass) || mono_class_is_ginst (klass))
			b |= (1 << 6);
		buffer_add_byte (buf, b);
		nnested = 0;
		iter = NULL;
		while ((nested = mono_class_get_nested_types (klass, &iter)))
			nnested ++;
		buffer_add_int (buf, nnested);
		iter = NULL;
		while ((nested = mono_class_get_nested_types (klass, &iter)))
			buffer_add_typeid (buf, domain, nested);
		if (CHECK_PROTOCOL_VERSION (2, 12)) {
			if (mono_class_is_gtd (klass))
				buffer_add_typeid (buf, domain, klass);
			else if (mono_class_is_ginst (klass))
				buffer_add_typeid (buf, domain, mono_class_get_generic_class (klass)->container_class);
			else
				buffer_add_id (buf, 0);
		}
		if (CHECK_PROTOCOL_VERSION (2, 15)) {
			int count, i;

			if (mono_class_is_ginst (klass)) {
				MonoGenericInst *inst = mono_class_get_generic_class (klass)->context.class_inst;

				count = inst->type_argc;
				buffer_add_int (buf, count);
				for (i = 0; i < count; i++)
					buffer_add_typeid (buf, domain, mono_class_from_mono_type_internal (inst->type_argv [i]));
			} else if (mono_class_is_gtd (klass)) {
				MonoGenericContainer *container = mono_class_get_generic_container (klass);
				MonoClass *pklass;

				count = container->type_argc;
				buffer_add_int (buf, count);
				for (i = 0; i < count; i++) {
					pklass = mono_class_create_generic_parameter (mono_generic_container_get_param (container, i));
					buffer_add_typeid (buf, domain, pklass);
				}
			} else {
				buffer_add_int (buf, 0);
			}
		}
		break;
	}
	case CMD_TYPE_GET_METHODS: {
		int nmethods;
		int i = 0;
		gpointer iter = NULL;
		MonoMethod *m;

		mono_class_setup_methods (klass);

		nmethods = mono_class_num_methods (klass);

		buffer_add_int (buf, nmethods);

		while ((m = mono_class_get_methods (klass, &iter))) {
			buffer_add_methodid (buf, domain, m);
			i ++;
		}
		g_assert (i == nmethods);
		break;
	}
	case CMD_TYPE_GET_FIELDS: {
		int nfields;
		int i = 0;
		gpointer iter = NULL;
		MonoClassField *f;

		nfields = mono_class_num_fields (klass);

		buffer_add_int (buf, nfields);

		while ((f = mono_class_get_fields_internal (klass, &iter))) {
			buffer_add_fieldid (buf, domain, f);
			buffer_add_string (buf, f->name);
			buffer_add_typeid (buf, domain, mono_class_from_mono_type_internal (f->type));
			buffer_add_int (buf, f->type->attrs);
			i ++;
		}
		g_assert (i == nfields);
		break;
	}
	case CMD_TYPE_GET_PROPERTIES: {
		int nprops;
		int i = 0;
		gpointer iter = NULL;
		MonoProperty *p;

		nprops = mono_class_num_properties (klass);

		buffer_add_int (buf, nprops);

		while ((p = mono_class_get_properties (klass, &iter))) {
			buffer_add_propertyid (buf, domain, p);
			buffer_add_string (buf, p->name);
			buffer_add_methodid (buf, domain, p->get);
			buffer_add_methodid (buf, domain, p->set);
			buffer_add_int (buf, p->attrs);
			i ++;
		}
		g_assert (i == nprops);
		break;
	}
	case CMD_TYPE_GET_CATTRS: {
		MonoClass *attr_klass;
		MonoCustomAttrInfo *cinfo;

		attr_klass = decode_typeid (p, &p, end, NULL, &err);
		/* attr_klass can be NULL */
		if (err != ERR_NONE)
			goto exit;

		cinfo = mono_custom_attrs_from_class_checked (klass, error);
		if (!is_ok (error)) {
			mono_error_cleanup (error); /* FIXME don't swallow the error message */
			goto loader_error;
		}

		err = buffer_add_cattrs (buf, domain, m_class_get_image (klass), attr_klass, cinfo);
		if (err != ERR_NONE)
			goto exit;
		break;
	}
	case CMD_TYPE_GET_FIELD_CATTRS: {
		MonoClass *attr_klass;
		MonoCustomAttrInfo *cinfo;
		MonoClassField *field;

		field = decode_fieldid (p, &p, end, NULL, &err);
		if (err != ERR_NONE)
			goto exit;
		attr_klass = decode_typeid (p, &p, end, NULL, &err);
		if (err != ERR_NONE)
			goto exit;

		cinfo = mono_custom_attrs_from_field_checked (klass, field, error);
		if (!is_ok (error)) {
			mono_error_cleanup (error); /* FIXME don't swallow the error message */
			goto loader_error;
		}

		err = buffer_add_cattrs (buf, domain, m_class_get_image (klass), attr_klass, cinfo);
		if (err != ERR_NONE)
			goto exit;
		break;
	}
	case CMD_TYPE_GET_PROPERTY_CATTRS: {
		MonoClass *attr_klass;
		MonoCustomAttrInfo *cinfo;
		MonoProperty *prop;

		prop = decode_propertyid (p, &p, end, NULL, &err);
		if (err != ERR_NONE)
			goto exit;
		attr_klass = decode_typeid (p, &p, end, NULL, &err);
		if (err != ERR_NONE)
			goto exit;

		cinfo = mono_custom_attrs_from_property_checked (klass, prop, error);
		if (!is_ok (error)) {
			mono_error_cleanup (error); /* FIXME don't swallow the error message */
			goto loader_error;
		}

		err = buffer_add_cattrs (buf, domain, m_class_get_image (klass), attr_klass, cinfo);
		if (err != ERR_NONE)
			goto exit;
		break;
	}
	case CMD_TYPE_GET_VALUES:
	case CMD_TYPE_GET_VALUES_2: {
		guint8 *val;
		MonoClassField *f;
		MonoVTable *vtable;
		MonoClass *k;
		int len, i;
		gboolean found;
		MonoThread *thread_obj;
		MonoInternalThread *thread = NULL;
		guint32 special_static_type;

		if (command == CMD_TYPE_GET_VALUES_2) {
			int objid = decode_objid (p, &p, end);

			err = get_object (objid, (MonoObject**)&thread_obj);
			if (err != ERR_NONE)
				goto exit;

			thread = THREAD_TO_INTERNAL (thread_obj);
		}

		len = decode_int (p, &p, end);
		for (i = 0; i < len; ++i) {
			f = decode_fieldid (p, &p, end, NULL, &err);
			if (err != ERR_NONE)
				goto exit;

			if (!(f->type->attrs & FIELD_ATTRIBUTE_STATIC))
				goto invalid_fieldid;

			special_static_type = mono_class_field_get_special_static_type (f);
			if (special_static_type != SPECIAL_STATIC_NONE) {
				if (!(thread && special_static_type == SPECIAL_STATIC_THREAD))
					goto invalid_fieldid;
			}

			/* Check that the field belongs to the object */
			found = FALSE;
			for (k = klass; k; k = m_class_get_parent (k)) {
				if (k == f->parent) {
					found = TRUE;
					break;
				}
			}
			if (!found)
				goto invalid_fieldid;

			vtable = mono_class_vtable_checked (domain, f->parent, error);
			goto_if_nok (error, invalid_fieldid);

			val = (guint8 *)g_malloc (mono_class_instance_size (mono_class_from_mono_type_internal (f->type)));
			mono_field_static_get_value_for_thread (thread ? thread : mono_thread_internal_current (), vtable, f, val, string_handle, error);
			goto_if_nok (error, invalid_fieldid);

			buffer_add_value (buf, f->type, val, domain);
			g_free (val);
		}
		break;
	}
	case CMD_TYPE_SET_VALUES: {
		guint8 *val;
		MonoClassField *f;
		MonoVTable *vtable;
		MonoClass *k;
		int len, i;
		gboolean found;

		len = decode_int (p, &p, end);
		for (i = 0; i < len; ++i) {
			f = decode_fieldid (p, &p, end, NULL, &err);
			if (err != ERR_NONE)
				goto exit;

			if (!(f->type->attrs & FIELD_ATTRIBUTE_STATIC))
				goto invalid_fieldid;

			if (mono_class_field_is_special_static (f))
				goto invalid_fieldid;

			/* Check that the field belongs to the object */
			found = FALSE;
			for (k = klass; k; k = m_class_get_parent (k)) {
				if (k == f->parent) {
					found = TRUE;
					break;
				}
			}
			if (!found)
				goto invalid_fieldid;

			// FIXME: Check for literal/const

			vtable = mono_class_vtable_checked (domain, f->parent, error);
			goto_if_nok (error, invalid_fieldid);

			val = (guint8 *)g_malloc (mono_class_instance_size (mono_class_from_mono_type_internal (f->type)));
			err = decode_value (f->type, domain, val, p, &p, end, TRUE);
			if (err != ERR_NONE) {
				g_free (val);
				goto exit;
			}
			if (MONO_TYPE_IS_REFERENCE (f->type))
				mono_field_static_set_value_internal (vtable, f, *(gpointer*)val);
			else
				mono_field_static_set_value_internal (vtable, f, val);
			g_free (val);
		}
		break;
	}
	case CMD_TYPE_GET_OBJECT: {
		MonoObject *o = (MonoObject*)mono_type_get_object_checked (domain, m_class_get_byval_arg (klass), error);
		if (!is_ok (error)) {
			mono_error_cleanup (error);
			goto invalid_object;
		}
		buffer_add_objid (buf, o);
		break;
	}
	case CMD_TYPE_GET_SOURCE_FILES:
	case CMD_TYPE_GET_SOURCE_FILES_2: {
		char *source_file, *base;
		GPtrArray *files;
		int i;

		files = get_source_files_for_type (klass);

		buffer_add_int (buf, files->len);
		for (i = 0; i < files->len; ++i) {
			source_file = (char *)g_ptr_array_index (files, i);
			if (command == CMD_TYPE_GET_SOURCE_FILES_2) {
				buffer_add_string (buf, source_file);
			} else {
				base = dbg_path_get_basename (source_file);
				buffer_add_string (buf, base);
				g_free (base);
			}
			g_free (source_file);
		}
		g_ptr_array_free (files, TRUE);
		break;
	}
	case CMD_TYPE_IS_ASSIGNABLE_FROM: {
		MonoClass *oklass = decode_typeid (p, &p, end, NULL, &err);

		if (err != ERR_NONE)
			goto exit;
		if (mono_class_is_assignable_from_internal (klass, oklass))
			buffer_add_byte (buf, 1);
		else
			buffer_add_byte (buf, 0);
		break;
	}
	case CMD_TYPE_GET_METHODS_BY_NAME_FLAGS: {
		char *name = decode_string (p, &p, end);
		int i, flags = decode_int (p, &p, end);
		int mlisttype;
		if (CHECK_PROTOCOL_VERSION (2, 48))
			mlisttype = decode_int (p, &p, end);
		else
			mlisttype = 0; // MLISTTYPE_All
		ERROR_DECL (error);
		GPtrArray *array;

		error_init (error);
		array = mono_class_get_methods_by_name (klass, name, flags & ~BINDING_FLAGS_IGNORE_CASE, mlisttype, TRUE, error);
		if (!is_ok (error)) {
			mono_error_cleanup (error);
			goto loader_error;
		}
		buffer_add_int (buf, array->len);
		for (i = 0; i < array->len; ++i) {
			MonoMethod *method = (MonoMethod *)g_ptr_array_index (array, i);
			buffer_add_methodid (buf, domain, method);
		}

		g_ptr_array_free (array, TRUE);
		g_free (name);
		break;
	}
	case CMD_TYPE_GET_INTERFACES: {
		MonoClass *parent;
		GHashTable *iface_hash = g_hash_table_new (NULL, NULL);
		MonoClass *tclass, *iface;
		GHashTableIter iter;

		tclass = klass;

		for (parent = tclass; parent; parent = m_class_get_parent (parent)) {
			mono_class_setup_interfaces (parent, error);
			goto_if_nok (error, loader_error);

			collect_interfaces (parent, iface_hash, error);
			goto_if_nok (error, loader_error);
		}

		buffer_add_int (buf, g_hash_table_size (iface_hash));

		g_hash_table_iter_init (&iter, iface_hash);
		while (g_hash_table_iter_next (&iter, NULL, (void**)&iface))
			buffer_add_typeid (buf, domain, iface);
		g_hash_table_destroy (iface_hash);
		break;
	}
	case CMD_TYPE_GET_INTERFACE_MAP: {
		int tindex, ioffset;
		gboolean variance_used;
		MonoClass *iclass;
		int len, nmethods, i;
		gpointer iter;
		MonoMethod *method;

		len = decode_int (p, &p, end);
		mono_class_setup_vtable (klass);

		for (tindex = 0; tindex < len; ++tindex) {
			iclass = decode_typeid (p, &p, end, NULL, &err);
			if (err != ERR_NONE)
				goto exit;

			ioffset = mono_class_interface_offset_with_variance (klass, iclass, &variance_used);
			if (ioffset == -1)
				goto invalid_argument;

			nmethods = mono_class_num_methods (iclass);
			buffer_add_int (buf, nmethods);

			iter = NULL;
			while ((method = mono_class_get_methods (iclass, &iter))) {
				buffer_add_methodid (buf, domain, method);
			}
			MonoMethod **klass_vtable = m_class_get_vtable (klass);
			for (i = 0; i < nmethods; ++i)
				buffer_add_methodid (buf, domain, klass_vtable [i + ioffset]);
		}
		break;
	}
	case CMD_TYPE_IS_INITIALIZED: {
		MonoVTable *vtable = mono_class_vtable_checked (domain, klass, error);
		goto_if_nok (error, loader_error);

		if (vtable)
			buffer_add_int (buf, (vtable->initialized || vtable->init_failed) ? 1 : 0);
		else
			buffer_add_int (buf, 0);
		break;
	}
	case CMD_TYPE_CREATE_INSTANCE: {
		ERROR_DECL (error);
		MonoObject *obj;

		obj = mono_object_new_checked (domain, klass, error);
		mono_error_assert_ok (error);
		buffer_add_objid (buf, obj);
		break;
	}
	case CMD_TYPE_GET_VALUE_SIZE: {
		int32_t value_size;

		value_size = mono_class_value_size (klass, NULL);
		buffer_add_int (buf, value_size);
		break;
	}
	default:
		err = ERR_NOT_IMPLEMENTED;
		goto exit;
	}

	err = ERR_NONE;
	goto exit;
invalid_argument:
	err = ERR_INVALID_ARGUMENT;
	goto exit;
invalid_fieldid:
	err = ERR_INVALID_FIELDID;
	goto exit;
invalid_object:
	err = ERR_INVALID_OBJECT;
	goto exit;
loader_error:
	err = ERR_LOADER_ERROR;
	goto exit;
exit:
	HANDLE_FUNCTION_RETURN_VAL (err);
}

static ErrorCode
type_commands (int command, guint8 *p, guint8 *end, Buffer *buf)
{
	MonoClass *klass;
	MonoDomain *old_domain;
	MonoDomain *domain;
	ErrorCode err;

	klass = decode_typeid (p, &p, end, &domain, &err);
	if (err != ERR_NONE)
		return err;

	old_domain = mono_domain_get ();

	mono_domain_set_fast (domain, TRUE);

	err = type_commands_internal (command, klass, domain, p, end, buf);

	mono_domain_set_fast (old_domain, TRUE);

	return err;
}

static ErrorCode
method_commands_internal (int command, MonoMethod *method, MonoDomain *domain, guint8 *p, guint8 *end, Buffer *buf)
{
	MonoMethodHeader *header;
	ErrorCode err;

	switch (command) {
	case CMD_METHOD_GET_NAME: {
		buffer_add_string (buf, method->name);
		break;			
	}
	case CMD_METHOD_GET_DECLARING_TYPE: {
		buffer_add_typeid (buf, domain, method->klass);
		break;
	}
	case CMD_METHOD_GET_DEBUG_INFO: {
		ERROR_DECL (error);
		MonoDebugMethodInfo *minfo;
		char *source_file;
		int i, j, n_il_offsets;
		int *source_files;
		GPtrArray *source_file_list;
		MonoSymSeqPoint *sym_seq_points;

		header = mono_method_get_header_checked (method, error);
		if (!header) {
			mono_error_cleanup (error); /* FIXME don't swallow the error */
			buffer_add_int (buf, 0);
			buffer_add_string (buf, "");
			buffer_add_int (buf, 0);
			break;
		}

		minfo = mono_debug_lookup_method (method);
		if (!minfo) {
			buffer_add_int (buf, header->code_size);
			buffer_add_string (buf, "");
			buffer_add_int (buf, 0);
			mono_metadata_free_mh (header);
			break;
		}

		mono_debug_get_seq_points (minfo, &source_file, &source_file_list, &source_files, &sym_seq_points, &n_il_offsets);
		buffer_add_int (buf, header->code_size);
		if (CHECK_PROTOCOL_VERSION (2, 13)) {
			buffer_add_int (buf, source_file_list->len);
			for (i = 0; i < source_file_list->len; ++i) {
				MonoDebugSourceInfo *sinfo = (MonoDebugSourceInfo *)g_ptr_array_index (source_file_list, i);
				buffer_add_string (buf, sinfo->source_file);
				if (CHECK_PROTOCOL_VERSION (2, 14)) {
					for (j = 0; j < 16; ++j)
						buffer_add_byte (buf, sinfo->hash [j]);
				}
			}
		} else {
			buffer_add_string (buf, source_file);
		}
		buffer_add_int (buf, n_il_offsets);
		DEBUG_PRINTF (10, "Line number table for method %s:\n", mono_method_full_name (method,  TRUE));
		for (i = 0; i < n_il_offsets; ++i) {
			MonoSymSeqPoint *sp = &sym_seq_points [i];
			const char *srcfile = "";

			if (source_files [i] != -1) {
				MonoDebugSourceInfo *sinfo = (MonoDebugSourceInfo *)g_ptr_array_index (source_file_list, source_files [i]);
				srcfile = sinfo->source_file;
			}
			DEBUG_PRINTF (10, "IL%x -> %s:%d %d %d %d\n", sp->il_offset, srcfile, sp->line, sp->column, sp->end_line, sp->end_column);
			buffer_add_int (buf, sp->il_offset);
			buffer_add_int (buf, sp->line);
			if (CHECK_PROTOCOL_VERSION (2, 13))
				buffer_add_int (buf, source_files [i]);
			if (CHECK_PROTOCOL_VERSION (2, 19))
				buffer_add_int (buf, sp->column);
			if (CHECK_PROTOCOL_VERSION (2, 32)) {
				buffer_add_int (buf, sp->end_line);
				buffer_add_int (buf, sp->end_column);
			}
		}
		g_free (source_file);
		g_free (source_files);
		g_free (sym_seq_points);
		g_ptr_array_free (source_file_list, TRUE);
		mono_metadata_free_mh (header);
		break;
	}
	case CMD_METHOD_GET_PARAM_INFO: {
		MonoMethodSignature *sig = mono_method_signature_internal (method);
		guint32 i;
		char **names;

		/* FIXME: mono_class_from_mono_type_internal () and byrefs */

		/* FIXME: Use a smaller encoding */
		buffer_add_int (buf, sig->call_convention);
		buffer_add_int (buf, sig->param_count);
		buffer_add_int (buf, sig->generic_param_count);
		buffer_add_typeid (buf, domain, mono_class_from_mono_type_internal (sig->ret));
		for (i = 0; i < sig->param_count; ++i) {
			/* FIXME: vararg */
			buffer_add_typeid (buf, domain, mono_class_from_mono_type_internal (sig->params [i]));
		}

		/* Emit parameter names */
		names = g_new (char *, sig->param_count);
		mono_method_get_param_names (method, (const char **) names);
		for (i = 0; i < sig->param_count; ++i)
			buffer_add_string (buf, names [i]);
		g_free (names);

		break;
	}
	case CMD_METHOD_GET_LOCALS_INFO: {
		ERROR_DECL (error);
		int i, num_locals;
		MonoDebugLocalsInfo *locals;
		int *locals_map = NULL;

		header = mono_method_get_header_checked (method, error);
		if (!header) {
			add_error_string (buf, mono_error_get_message (error));
			mono_error_cleanup (error); /* FIXME don't swallow the error */
			return ERR_INVALID_ARGUMENT;
		}

		locals = mono_debug_lookup_locals (method);
		if (!locals) {
			if (CHECK_PROTOCOL_VERSION (2, 43)) {
				/* Scopes */
				buffer_add_int (buf, 1);
				buffer_add_int (buf, 0);
				buffer_add_int (buf, header->code_size);
			}
			buffer_add_int (buf, header->num_locals);
			/* Types */
			for (i = 0; i < header->num_locals; ++i) {
				buffer_add_typeid (buf, domain, mono_class_from_mono_type_internal (header->locals [i]));
			}
			/* Names */
			for (i = 0; i < header->num_locals; ++i) {
				char lname [128];
				sprintf (lname, "V_%d", i);
				buffer_add_string (buf, lname);
			}
			/* Scopes */
			for (i = 0; i < header->num_locals; ++i) {
				buffer_add_int (buf, 0);
				buffer_add_int (buf, header->code_size);
			}
		} else {
			if (CHECK_PROTOCOL_VERSION (2, 43)) {
				/* Scopes */
				buffer_add_int (buf, locals->num_blocks);
				int last_start = 0;
				for (i = 0; i < locals->num_blocks; ++i) {
					buffer_add_int (buf, locals->code_blocks [i].start_offset - last_start);
					buffer_add_int (buf, locals->code_blocks [i].end_offset - locals->code_blocks [i].start_offset);
					last_start = locals->code_blocks [i].start_offset;
				}
			}

			num_locals = locals->num_locals;
			buffer_add_int (buf, num_locals);

			/* Types */
			for (i = 0; i < num_locals; ++i) {
				g_assert (locals->locals [i].index < header->num_locals);
				buffer_add_typeid (buf, domain, mono_class_from_mono_type_internal (header->locals [locals->locals [i].index]));
			}
			/* Names */
			for (i = 0; i < num_locals; ++i)
				buffer_add_string (buf, locals->locals [i].name);
			/* Scopes */
			for (i = 0; i < num_locals; ++i) {
				if (locals->locals [i].block) {
					buffer_add_int (buf, locals->locals [i].block->start_offset);
					buffer_add_int (buf, locals->locals [i].block->end_offset);
				} else {
					buffer_add_int (buf, 0);
					buffer_add_int (buf, header->code_size);
				}
			}
		}
		mono_metadata_free_mh (header);

		if (locals)
			mono_debug_free_locals (locals);
		g_free (locals_map);

		break;
	}
	case CMD_METHOD_GET_INFO:
		buffer_add_int (buf, method->flags);
		buffer_add_int (buf, method->iflags);
		buffer_add_int (buf, method->token);
		if (CHECK_PROTOCOL_VERSION (2, 12)) {
			guint8 attrs = 0;
			if (method->is_generic)
				attrs |= (1 << 0);
			if (mono_method_signature_internal (method)->generic_param_count)
				attrs |= (1 << 1);
			buffer_add_byte (buf, attrs);
			if (method->is_generic || method->is_inflated) {
				MonoMethod *result;

				if (method->is_generic) {
					result = method;
				} else {
					MonoMethodInflated *imethod = (MonoMethodInflated *)method;
					
					result = imethod->declaring;
					if (imethod->context.class_inst) {
						MonoClass *klass = ((MonoMethod *) imethod)->klass;
						/*Generic methods gets the context of the GTD.*/
						if (mono_class_get_context (klass)) {
							ERROR_DECL (error);
							result = mono_class_inflate_generic_method_full_checked (result, klass, mono_class_get_context (klass), error);
							if (!is_ok (error)) {
								add_error_string (buf, mono_error_get_message (error));
								mono_error_cleanup (error);
								return ERR_INVALID_ARGUMENT;
							}
						}
					}
				}

				buffer_add_methodid (buf, domain, result);
			} else {
				buffer_add_id (buf, 0);
			}
			if (CHECK_PROTOCOL_VERSION (2, 15)) {
				if (mono_method_signature_internal (method)->generic_param_count) {
					int count, i;

					if (method->is_inflated) {
						MonoGenericInst *inst = mono_method_get_context (method)->method_inst;
						if (inst) {
							count = inst->type_argc;
							buffer_add_int (buf, count);

							for (i = 0; i < count; i++)
								buffer_add_typeid (buf, domain, mono_class_from_mono_type_internal (inst->type_argv [i]));
						} else {
							buffer_add_int (buf, 0);
						}
					} else if (method->is_generic) {
						MonoGenericContainer *container = mono_method_get_generic_container (method);

						count = mono_method_signature_internal (method)->generic_param_count;
						buffer_add_int (buf, count);
						for (i = 0; i < count; i++) {
							MonoGenericParam *param = mono_generic_container_get_param (container, i);
							MonoClass *pklass = mono_class_create_generic_parameter (param);
							buffer_add_typeid (buf, domain, pklass);
						}
					} else {
						buffer_add_int (buf, 0);
					}
				} else {
					buffer_add_int (buf, 0);
				}
			}
		}
		break;
	case CMD_METHOD_GET_BODY: {
		ERROR_DECL (error);
		int i;

		header = mono_method_get_header_checked (method, error);
		if (!header) {
			mono_error_cleanup (error); /* FIXME don't swallow the error */
			buffer_add_int (buf, 0);

			if (CHECK_PROTOCOL_VERSION (2, 18))
				buffer_add_int (buf, 0);
		} else {
			buffer_add_int (buf, header->code_size);
			for (i = 0; i < header->code_size; ++i)
				buffer_add_byte (buf, header->code [i]);

			if (CHECK_PROTOCOL_VERSION (2, 18)) {
				buffer_add_int (buf, header->num_clauses);
				for (i = 0; i < header->num_clauses; ++i) {
					MonoExceptionClause *clause = &header->clauses [i];

					buffer_add_int (buf, clause->flags);
					buffer_add_int (buf, clause->try_offset);
					buffer_add_int (buf, clause->try_len);
					buffer_add_int (buf, clause->handler_offset);
					buffer_add_int (buf, clause->handler_len);
					if (clause->flags == MONO_EXCEPTION_CLAUSE_NONE)
						buffer_add_typeid (buf, domain, clause->data.catch_class);
					else if (clause->flags == MONO_EXCEPTION_CLAUSE_FILTER)
						buffer_add_int (buf, clause->data.filter_offset);
				}
			}

			mono_metadata_free_mh (header);
		}

		break;
	}
	case CMD_METHOD_RESOLVE_TOKEN: {
		guint32 token = decode_int (p, &p, end);

		// FIXME: Generics
		switch (mono_metadata_token_code (token)) {
		case MONO_TOKEN_STRING: {
			ERROR_DECL (error);
			MonoString *s;
			char *s2;

			s = mono_ldstr_checked (domain, m_class_get_image (method->klass), mono_metadata_token_index (token), error);
			mono_error_assert_ok (error); /* FIXME don't swallow the error */

			s2 = mono_string_to_utf8_checked_internal (s, error);
			mono_error_assert_ok (error);

			buffer_add_byte (buf, TOKEN_TYPE_STRING);
			buffer_add_string (buf, s2);
			g_free (s2);
			break;
		}
		default: {
			ERROR_DECL (error);
			gpointer val;
			MonoClass *handle_class;

			if (method->wrapper_type == MONO_WRAPPER_DYNAMIC_METHOD) {
				val = mono_method_get_wrapper_data (method, token);
				handle_class = (MonoClass *)mono_method_get_wrapper_data (method, token + 1);

				if (handle_class == NULL) {
					// Can't figure out the token type
					buffer_add_byte (buf, TOKEN_TYPE_UNKNOWN);
					break;
				}
			} else {
				val = mono_ldtoken_checked (m_class_get_image (method->klass), token, &handle_class, NULL, error);
				if (!val)
					g_error ("Could not load token due to %s", mono_error_get_message (error));
			}

			if (handle_class == mono_defaults.typehandle_class) {
				buffer_add_byte (buf, TOKEN_TYPE_TYPE);
				if (method->wrapper_type == MONO_WRAPPER_DYNAMIC_METHOD)
					buffer_add_typeid (buf, domain, (MonoClass *) val);
				else
					buffer_add_typeid (buf, domain, mono_class_from_mono_type_internal ((MonoType*)val));
			} else if (handle_class == mono_defaults.fieldhandle_class) {
				buffer_add_byte (buf, TOKEN_TYPE_FIELD);
				buffer_add_fieldid (buf, domain, (MonoClassField *)val);
			} else if (handle_class == mono_defaults.methodhandle_class) {
				buffer_add_byte (buf, TOKEN_TYPE_METHOD);
				buffer_add_methodid (buf, domain, (MonoMethod *)val);
			} else if (handle_class == mono_defaults.string_class) {
				char *s;

				s = mono_string_to_utf8_checked_internal ((MonoString *)val, error);
				if (!is_ok (error)) {
					add_error_string (buf, mono_error_get_message (error));
					mono_error_cleanup (error);
					g_free (s);
					return ERR_INVALID_ARGUMENT;
				}
				buffer_add_byte (buf, TOKEN_TYPE_STRING);
				buffer_add_string (buf, s);
				g_free (s);
			} else {
				g_assert_not_reached ();
			}
			break;
		}
		}
		break;
	}
	case CMD_METHOD_GET_CATTRS: {
		ERROR_DECL (error);
		MonoClass *attr_klass;
		MonoCustomAttrInfo *cinfo;

		attr_klass = decode_typeid (p, &p, end, NULL, &err);
		/* attr_klass can be NULL */
		if (err != ERR_NONE)
			return err;

		cinfo = mono_custom_attrs_from_method_checked (method, error);
		if (!is_ok (error)) {
			mono_error_cleanup (error); /* FIXME don't swallow the error message */
			return ERR_LOADER_ERROR;
		}

		err = buffer_add_cattrs (buf, domain, m_class_get_image (method->klass), attr_klass, cinfo);
		if (err != ERR_NONE)
			return err;
		break;
	}
	case CMD_METHOD_MAKE_GENERIC_METHOD: {
		ERROR_DECL (error);
		MonoType **type_argv;
		int i, type_argc;
		MonoDomain *d;
		MonoClass *klass;
		MonoGenericInst *ginst;
		MonoGenericContext tmp_context;
		MonoMethod *inflated;

		type_argc = decode_int (p, &p, end);
		type_argv = g_new0 (MonoType*, type_argc);
		for (i = 0; i < type_argc; ++i) {
			klass = decode_typeid (p, &p, end, &d, &err);
			if (err != ERR_NONE) {
				g_free (type_argv);
				return err;
			}
			if (domain != d) {
				g_free (type_argv);
				return ERR_INVALID_ARGUMENT;
			}
			type_argv [i] = m_class_get_byval_arg (klass);
		}
		ginst = mono_metadata_get_generic_inst (type_argc, type_argv);
		g_free (type_argv);
		tmp_context.class_inst = mono_class_is_ginst (method->klass) ? mono_class_get_generic_class (method->klass)->context.class_inst : NULL;
		tmp_context.method_inst = ginst;

		inflated = mono_class_inflate_generic_method_checked (method, &tmp_context, error);
		if (!is_ok (error)) {
			add_error_string (buf, mono_error_get_message (error));
			mono_error_cleanup (error);
			return ERR_INVALID_ARGUMENT;
		}
		if (!mono_verifier_is_method_valid_generic_instantiation (inflated))
			return ERR_INVALID_ARGUMENT;
		buffer_add_methodid (buf, domain, inflated);
		break;
	}
	default:
		return ERR_NOT_IMPLEMENTED;
	}

	return ERR_NONE;
}

static ErrorCode
method_commands (int command, guint8 *p, guint8 *end, Buffer *buf)
{
	ErrorCode err;
	MonoDomain *old_domain;
	MonoDomain *domain;
	MonoMethod *method;

	method = decode_methodid (p, &p, end, &domain, &err);
	if (err != ERR_NONE)
		return err;

	old_domain = mono_domain_get ();

	mono_domain_set_fast (domain, TRUE);

	err = method_commands_internal (command, method, domain, p, end, buf);

	mono_domain_set_fast (old_domain, TRUE);

	return err;
}

static ErrorCode
thread_commands (int command, guint8 *p, guint8 *end, Buffer *buf)
{
	int objid = decode_objid (p, &p, end);
	ErrorCode err;
	MonoThread *thread_obj;
	MonoInternalThread *thread;

	err = get_object (objid, (MonoObject**)&thread_obj);
	if (err != ERR_NONE)
		return err;

	thread = THREAD_TO_INTERNAL (thread_obj);
	   
	switch (command) {
	case CMD_THREAD_GET_NAME: {
		char *s = mono_thread_get_name_utf8 (thread_obj);

		if (!s) {
			buffer_add_int (buf, 0);
		} else {
			const size_t len = strlen (s);
			buffer_add_int (buf, len);
			buffer_add_data (buf, (guint8*)s, len);
			g_free (s);
		}
		break;
	}
	case CMD_THREAD_GET_FRAME_INFO: {
		DebuggerTlsData *tls;
		int i, start_frame, length;

		// Wait for suspending if it already started
		// FIXME: Races with suspend_count
		while (!is_suspended ()) {
			if (suspend_count)
				wait_for_suspend ();
		}
		/*
		if (suspend_count)
			wait_for_suspend ();
		if (!is_suspended ())
			return ERR_NOT_SUSPENDED;
		*/

		start_frame = decode_int (p, &p, end);
		length = decode_int (p, &p, end);

		if (start_frame != 0 || length != -1)
			return ERR_NOT_IMPLEMENTED;

		mono_loader_lock ();
		tls = (DebuggerTlsData *)mono_g_hash_table_lookup (thread_to_tls, thread);
		mono_loader_unlock ();
		if (tls == NULL)
			return ERR_UNLOADED;

		compute_frame_info (thread, tls, TRUE); //the last parameter is TRUE to force that the frame info that will be send is synchronised with the debugged thread

		buffer_add_int (buf, tls->frame_count);
		for (i = 0; i < tls->frame_count; ++i) {
			buffer_add_int (buf, tls->frames [i]->id);
			buffer_add_methodid (buf, tls->frames [i]->de.domain, tls->frames [i]->actual_method);
			buffer_add_int (buf, tls->frames [i]->il_offset);
			/*
			 * Instead of passing the frame type directly to the client, we associate
			 * it with the previous frame using a set of flags. This avoids lots of
			 * conditional code in the client, since a frame whose type isn't 
			 * FRAME_TYPE_MANAGED has no method, location, etc.
			 */
			buffer_add_byte (buf, tls->frames [i]->flags);
		}

		break;
	}
	case CMD_THREAD_GET_STATE:
		buffer_add_int (buf, thread->state);
		break;
	case CMD_THREAD_GET_INFO:
		buffer_add_byte (buf, thread->threadpool_thread);
		break;
	case CMD_THREAD_GET_ID:
		buffer_add_long (buf, (guint64)(gsize)thread);
		break;
	case CMD_THREAD_GET_TID:
		buffer_add_long (buf, (guint64)thread->tid);
		break;
	case CMD_THREAD_SET_IP: {
		DebuggerTlsData *tls;
		MonoMethod *method;
		MonoDomain *domain;
		MonoSeqPointInfo *seq_points;
		SeqPoint sp;
		gboolean found_sp;
		gint64 il_offset;

		method = decode_methodid (p, &p, end, &domain, &err);
		if (err != ERR_NONE)
			return err;
		il_offset = decode_long (p, &p, end);

		while (!is_suspended ()) {
			if (suspend_count)
				wait_for_suspend ();
		}

		mono_loader_lock ();
		tls = (DebuggerTlsData *)mono_g_hash_table_lookup (thread_to_tls, thread);
		mono_loader_unlock ();
		g_assert (tls);

		compute_frame_info (thread, tls, FALSE);
		if (tls->frame_count == 0 || tls->frames [0]->actual_method != method)
			return ERR_INVALID_ARGUMENT;

		found_sp = mono_find_seq_point (domain, method, il_offset, &seq_points, &sp);

		g_assert (seq_points);

		if (!found_sp)
			return ERR_INVALID_ARGUMENT;

		// FIXME: Check that the ip change is safe

		DEBUG_PRINTF (1, "[dbg] Setting IP to %s:0x%0x(0x%0x)\n", tls->frames [0]->actual_method->name, (int)sp.il_offset, (int)sp.native_offset);

		if (tls->frames [0]->de.ji->is_interp) {
			MonoJitTlsData *jit_data = thread->thread_info->jit_data;
			mini_get_interp_callbacks ()->set_resume_state (jit_data, NULL, NULL, tls->frames [0]->interp_frame, (guint8*)tls->frames [0]->de.ji->code_start + sp.native_offset);
		} else {
			MONO_CONTEXT_SET_IP (&tls->restore_state.ctx, (guint8*)tls->frames [0]->de.ji->code_start + sp.native_offset);
		}
		break;
	}
	case CMD_THREAD_ELAPSED_TIME: {
		DebuggerTlsData *tls;
		mono_loader_lock ();
		tls = (DebuggerTlsData *)mono_g_hash_table_lookup (thread_to_tls, thread);
		mono_loader_unlock ();
		g_assert (tls);
		buffer_add_long (buf, (long)mono_stopwatch_elapsed_ms (&tls->step_time));
		break;
	}
	default:
		return ERR_NOT_IMPLEMENTED;
	}

	return ERR_NONE;
}

static ErrorCode
frame_commands (int command, guint8 *p, guint8 *end, Buffer *buf)
{
	int objid;
	ErrorCode err;
	MonoThread *thread_obj;
	MonoInternalThread *thread;
	int pos, i, len, frame_idx;
	DebuggerTlsData *tls;
	StackFrame *frame;
	MonoDebugMethodJitInfo *jit;
	MonoMethodSignature *sig;
	gssize id;
	MonoMethodHeader *header;

	objid = decode_objid (p, &p, end);
	err = get_object (objid, (MonoObject**)&thread_obj);
	if (err != ERR_NONE)
		return err;

	thread = THREAD_TO_INTERNAL (thread_obj);

	id = decode_id (p, &p, end);

	mono_loader_lock ();
	tls = (DebuggerTlsData *)mono_g_hash_table_lookup (thread_to_tls, thread);
	mono_loader_unlock ();
	g_assert (tls);

	for (i = 0; i < tls->frame_count; ++i) {
		if (tls->frames [i]->id == id)
			break;
	}
	if (i == tls->frame_count)
		return ERR_INVALID_FRAMEID;

	/* The thread is still running native code, can't get frame variables info */
	if (!tls->really_suspended && !tls->async_state.valid) 
		return ERR_NOT_SUSPENDED;
	frame_idx = i;
	frame = tls->frames [frame_idx];

	/* This is supported for frames without has_ctx etc. set */
	if (command == CMD_STACK_FRAME_GET_DOMAIN) {
		if (CHECK_PROTOCOL_VERSION (2, 38))
			buffer_add_domainid (buf, frame->de.domain);
		return ERR_NONE;
	}

	if (!frame->has_ctx)
		return ERR_ABSENT_INFORMATION;

	if (!ensure_jit ((DbgEngineStackFrame*)frame))
		return ERR_ABSENT_INFORMATION;

	jit = frame->jit;

	sig = mono_method_signature_internal (frame->actual_method);

	if (!(jit->has_var_info || frame->de.ji->is_interp) || !mono_get_seq_points (frame->de.domain, frame->actual_method))
		/*
		 * The method is probably from an aot image compiled without soft-debug, variables might be dead, etc.
		 */
		return ERR_ABSENT_INFORMATION;

	switch (command) {
	case CMD_STACK_FRAME_GET_VALUES: {
		ERROR_DECL (error);
		len = decode_int (p, &p, end);
		header = mono_method_get_header_checked (frame->actual_method, error);
		mono_error_assert_ok (error); /* FIXME report error */

		for (i = 0; i < len; ++i) {
			pos = decode_int (p, &p, end);

			if (pos < 0) {
				pos = - pos - 1;

				DEBUG_PRINTF (4, "[dbg]   send arg %d.\n", pos);

				if (frame->de.ji->is_interp) {
					guint8 *addr;

					addr = (guint8*)mini_get_interp_callbacks ()->frame_get_arg (frame->interp_frame, pos);

					buffer_add_value_full (buf, sig->params [pos], addr, frame->de.domain, FALSE, NULL, 1);
				} else {
					g_assert (pos >= 0 && pos < jit->num_params);

					add_var (buf, jit, sig->params [pos], &jit->params [pos], &frame->ctx, frame->de.domain, FALSE);
				}
			} else {
				MonoDebugLocalsInfo *locals;

				locals = mono_debug_lookup_locals (frame->de.method);
				if (locals) {
					g_assert (pos < locals->num_locals);
					pos = locals->locals [pos].index;
					mono_debug_free_locals (locals);
				}

				DEBUG_PRINTF (4, "[dbg]   send local %d.\n", pos);

				if (frame->de.ji->is_interp) {
					guint8 *addr;

					addr = (guint8*)mini_get_interp_callbacks ()->frame_get_local (frame->interp_frame, pos);

					buffer_add_value_full (buf, header->locals [pos], addr, frame->de.domain, FALSE, NULL, 1);
				} else {
					g_assert (pos >= 0 && pos < jit->num_locals);

					add_var (buf, jit, header->locals [pos], &jit->locals [pos], &frame->ctx, frame->de.domain, FALSE);
				}
			}
		}
		mono_metadata_free_mh (header);
		break;
	}
	case CMD_STACK_FRAME_GET_THIS: {
		if (frame->de.method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE)
			return ERR_ABSENT_INFORMATION;
		if (m_class_is_valuetype (frame->api_method->klass)) {
			if (!sig->hasthis) {
				MonoObject *p = NULL;
				buffer_add_value (buf, mono_get_object_type (), &p, frame->de.domain);
			} else {
				if (frame->de.ji->is_interp) {
					guint8 *addr;

					addr = (guint8*)mini_get_interp_callbacks ()->frame_get_this (frame->interp_frame);

					buffer_add_value_full (buf, m_class_get_this_arg (frame->actual_method->klass), addr, frame->de.domain, FALSE, NULL, 1);
				} else {
					add_var (buf, jit, m_class_get_this_arg (frame->actual_method->klass), jit->this_var, &frame->ctx, frame->de.domain, TRUE);
				}
			}
		} else {
			if (!sig->hasthis) {
				MonoObject *p = NULL;
				buffer_add_value (buf, m_class_get_byval_arg (frame->actual_method->klass), &p, frame->de.domain);
			} else {
				if (frame->de.ji->is_interp) {
					guint8 *addr;

					addr = (guint8*)mini_get_interp_callbacks ()->frame_get_this (frame->interp_frame);

					buffer_add_value_full (buf, m_class_get_byval_arg (frame->api_method->klass), addr, frame->de.domain, FALSE, NULL, 1);
				} else {
					add_var (buf, jit, m_class_get_byval_arg (frame->api_method->klass), jit->this_var, &frame->ctx, frame->de.domain, TRUE);
				}
			}
		}
		break;
	}
	case CMD_STACK_FRAME_SET_VALUES: {
		ERROR_DECL (error);
		guint8 *val_buf;
		MonoType *t;
		MonoDebugVarInfo *var = NULL;
		gboolean is_arg = FALSE;

		len = decode_int (p, &p, end);
		header = mono_method_get_header_checked (frame->actual_method, error);
		mono_error_assert_ok (error); /* FIXME report error */

		for (i = 0; i < len; ++i) {
			pos = decode_int (p, &p, end);

			if (pos < 0) {
				pos = - pos - 1;

				g_assert (pos >= 0 && pos < jit->num_params);

				t = sig->params [pos];
				var = &jit->params [pos];
				is_arg = TRUE;
			} else {
				MonoDebugLocalsInfo *locals;

				locals = mono_debug_lookup_locals (frame->de.method);
				if (locals) {
					g_assert (pos < locals->num_locals);
					pos = locals->locals [pos].index;
					mono_debug_free_locals (locals);
				}
				g_assert (pos >= 0 && pos < jit->num_locals);

				t = header->locals [pos];
				var = &jit->locals [pos];
			}

			if (MONO_TYPE_IS_REFERENCE (t))
				val_buf = (guint8 *)g_alloca (sizeof (MonoObject*));
			else
				val_buf = (guint8 *)g_alloca (mono_class_instance_size (mono_class_from_mono_type_internal (t)));
			err = decode_value (t, frame->de.domain, val_buf, p, &p, end, TRUE);
			if (err != ERR_NONE)
				return err;

			if (frame->de.ji->is_interp) {
				guint8 *addr;

				if (is_arg)
					addr = (guint8*)mini_get_interp_callbacks ()->frame_get_arg (frame->interp_frame, pos);
				else
					addr = (guint8*)mini_get_interp_callbacks ()->frame_get_local (frame->interp_frame, pos);
				set_interp_var (t, addr, val_buf);
			} else {
				set_var (t, var, &frame->ctx, frame->de.domain, val_buf, frame->reg_locations, &tls->restore_state.ctx);
			}
		}
		mono_metadata_free_mh (header);
		break;
	}
	case CMD_STACK_FRAME_GET_DOMAIN: {
		if (CHECK_PROTOCOL_VERSION (2, 38))
			buffer_add_domainid (buf, frame->de.domain);
		break;
	}
	case CMD_STACK_FRAME_SET_THIS: {
		guint8 *val_buf;
		MonoType *t;
		MonoDebugVarInfo *var;

		t = m_class_get_byval_arg (frame->actual_method->klass);
		/* Checked by the sender */
		g_assert (MONO_TYPE_ISSTRUCT (t));

		val_buf = (guint8 *)g_alloca (mono_class_instance_size (mono_class_from_mono_type_internal (t)));
		err = decode_value (t, frame->de.domain, val_buf, p, &p, end, TRUE);
		if (err != ERR_NONE)
			return err;

		if (frame->de.ji->is_interp) {
			guint8 *addr;

			addr = (guint8*)mini_get_interp_callbacks ()->frame_get_this (frame->interp_frame);
			set_interp_var (m_class_get_this_arg (frame->actual_method->klass), addr, val_buf);
		} else {
			var = jit->this_var;
			if (!var) {
				add_error_string (buf, "Invalid this object");
				return ERR_INVALID_ARGUMENT;
			}

			set_var (m_class_get_this_arg (frame->actual_method->klass), var, &frame->ctx, frame->de.domain, val_buf, frame->reg_locations, &tls->restore_state.ctx);
		}
		break;
	}
	default:
		return ERR_NOT_IMPLEMENTED;
	}

	return ERR_NONE;
}

static ErrorCode
array_commands (int command, guint8 *p, guint8 *end, Buffer *buf)
{
	MonoArray *arr;
	int objid, index, len, i, esize;
	ErrorCode err;
	gpointer elem;

	objid = decode_objid (p, &p, end);
	err = get_object (objid, (MonoObject**)&arr);
	if (err != ERR_NONE)
		return err;

	switch (command) {
	case CMD_ARRAY_REF_GET_LENGTH:
		buffer_add_int (buf, m_class_get_rank (arr->obj.vtable->klass));
		if (!arr->bounds) {
			buffer_add_int (buf, arr->max_length);
			buffer_add_int (buf, 0);
		} else {
			for (i = 0; i < m_class_get_rank (arr->obj.vtable->klass); ++i) {
				buffer_add_int (buf, arr->bounds [i].length);
				buffer_add_int (buf, arr->bounds [i].lower_bound);
			}
		}
		break;
	case CMD_ARRAY_REF_GET_VALUES:
		index = decode_int (p, &p, end);
		len = decode_int (p, &p, end);

		if (index < 0 || len < 0)
			return ERR_INVALID_ARGUMENT;
		// Reordered to avoid integer overflow
		if (index > arr->max_length - len)
			return ERR_INVALID_ARGUMENT;

		esize = mono_array_element_size (arr->obj.vtable->klass);
		for (i = index; i < index + len; ++i) {
			elem = (gpointer*)((char*)arr->vector + (i * esize));
			buffer_add_value (buf, m_class_get_byval_arg (m_class_get_element_class (arr->obj.vtable->klass)), elem, arr->obj.vtable->domain);
		}
		break;
	case CMD_ARRAY_REF_SET_VALUES:
		index = decode_int (p, &p, end);
		len = decode_int (p, &p, end);

		if (index < 0 || len < 0)
			return ERR_INVALID_ARGUMENT;
		// Reordered to avoid integer overflow
		if (index > arr->max_length - len)
			return ERR_INVALID_ARGUMENT;

		esize = mono_array_element_size (arr->obj.vtable->klass);
		for (i = index; i < index + len; ++i) {
			elem = (gpointer*)((char*)arr->vector + (i * esize));

			decode_value (m_class_get_byval_arg (m_class_get_element_class (arr->obj.vtable->klass)), arr->obj.vtable->domain, (guint8 *)elem, p, &p, end, TRUE);
		}
		break;
	default:
		return ERR_NOT_IMPLEMENTED;
	}

	return ERR_NONE;
}

static ErrorCode
string_commands (int command, guint8 *p, guint8 *end, Buffer *buf)
{
	int objid;
	ErrorCode err;
	MonoString *str;
	char *s;
	int i, index, length;
	gunichar2 *c;
	gboolean use_utf16 = FALSE;

	objid = decode_objid (p, &p, end);
	err = get_object (objid, (MonoObject**)&str);
	if (err != ERR_NONE)
		return err;

	switch (command) {
	case CMD_STRING_REF_GET_VALUE:
		if (CHECK_PROTOCOL_VERSION (2, 41)) {
			for (i = 0; i < mono_string_length_internal (str); ++i)
				if (mono_string_chars_internal (str)[i] == 0)
					use_utf16 = TRUE;
			buffer_add_byte (buf, use_utf16 ? 1 : 0);
		}
		if (use_utf16) {
			buffer_add_int (buf, mono_string_length_internal (str) * 2);
			buffer_add_utf16 (buf, (guint8*)mono_string_chars_internal (str), mono_string_length_internal (str) * 2);
		} else {
			ERROR_DECL (error);
			s = mono_string_to_utf8_checked_internal (str, error);
			if (!is_ok (error)) {
				if (s)
					g_free (s);
				add_error_string (buf, mono_error_get_message (error));
				return ERR_INVALID_ARGUMENT;
			}
			buffer_add_string (buf, s);
			g_free (s);
		}
		break;
	case CMD_STRING_REF_GET_LENGTH:
		buffer_add_long (buf, mono_string_length_internal (str));
		break;
	case CMD_STRING_REF_GET_CHARS:
		index = decode_long (p, &p, end);
		length = decode_long (p, &p, end);
		if (index > mono_string_length_internal (str) - length)
			return ERR_INVALID_ARGUMENT;
		c = mono_string_chars_internal (str) + index;
		for (i = 0; i < length; ++i)
			buffer_add_short (buf, c [i]);
		break;
	default:
		return ERR_NOT_IMPLEMENTED;
	}

	return ERR_NONE;
}

static void 
create_file_to_check_memory_address (void) 
{
	if (file_check_valid_memory != -1)
		return;
	char *file_name = g_strdup_printf ("debugger_check_valid_memory.%d", getpid());
	filename_check_valid_memory = g_build_filename (g_get_tmp_dir (), file_name, (const char*)NULL);
	file_check_valid_memory = open(filename_check_valid_memory, O_CREAT | O_WRONLY | O_APPEND, S_IWUSR);
	g_free (file_name);
}

static gboolean 
valid_memory_address (gpointer addr, gint size)
{
#ifndef _MSC_VER
	gboolean ret = TRUE;
	create_file_to_check_memory_address ();
	if(file_check_valid_memory < 0) {
		return TRUE;
	}
	write (file_check_valid_memory,  (gpointer)addr, 1);
	if (errno == EFAULT) {
		ret = FALSE;
	}
#else
	int i = 0;
	gboolean ret = FALSE;
	__try {
		for (i = 0; i < size; i++)
			*((volatile char*)addr+i);
		ret = TRUE;
	} __except(1) {
		return ret;
	}
#endif	
	return ret;
}

static ErrorCode
pointer_commands (int command, guint8 *p, guint8 *end, Buffer *buf)
{
	ErrorCode err;
	gint64 addr;
	MonoClass* klass;
	MonoDomain* domain = NULL;
	MonoType *type = NULL;
	int align;
	int size = 0;

	switch (command) {
	case CMD_POINTER_GET_VALUE:
		addr = decode_long (p, &p, end);
		klass = decode_typeid (p, &p, end, &domain, &err);
		if (err != ERR_NONE)
			return err;

		if (m_class_get_byval_arg (klass)->type != MONO_TYPE_PTR)
			return ERR_INVALID_ARGUMENT;

		type =  m_class_get_byval_arg (m_class_get_element_class (klass));
		size = mono_type_size (type, &align);
		
		if (!valid_memory_address((gpointer)addr, size))
			return ERR_INVALID_ARGUMENT;

		buffer_add_value (buf, type, (gpointer)addr, domain);

		break;
	default:
		return ERR_NOT_IMPLEMENTED;
	}

	return ERR_NONE;
}

static ErrorCode
object_commands (int command, guint8 *p, guint8 *end, Buffer *buf)
{
	HANDLE_FUNCTION_ENTER ();

	ERROR_DECL (error);
	int objid;
	ErrorCode err;
	MonoObject *obj;
	int len, i;
	MonoClassField *f;
	MonoClass *k;
	gboolean found;
	gboolean remote_obj = FALSE;
	MonoStringHandle string_handle = MONO_HANDLE_NEW (MonoString, NULL); // FIXME? Not always needed.

	if (command == CMD_OBJECT_REF_IS_COLLECTED) {
		objid = decode_objid (p, &p, end);
		err = get_object (objid, &obj);
		if (err != ERR_NONE)
			buffer_add_int (buf, 1);
		else
			buffer_add_int (buf, 0);
		err = ERR_NONE;
		goto exit;
	}

	objid = decode_objid (p, &p, end);
	err = get_object (objid, &obj);
	if (err != ERR_NONE)
		goto exit;

	MonoClass *obj_type;

	obj_type = obj->vtable->klass;
	if (mono_class_is_transparent_proxy (obj_type)) {
		obj_type = ((MonoTransparentProxy *)obj)->remote_class->proxy_class;
		remote_obj = TRUE;
	}

	g_assert (obj_type);

	switch (command) {
	case CMD_OBJECT_REF_GET_TYPE:
		/* This handles transparent proxies too */
		buffer_add_typeid (buf, obj->vtable->domain, mono_class_from_mono_type_internal (((MonoReflectionType*)obj->vtable->type)->type));
		break;
	case CMD_OBJECT_REF_GET_VALUES:
		len = decode_int (p, &p, end);

		for (i = 0; i < len; ++i) {
			MonoClassField *f = decode_fieldid (p, &p, end, NULL, &err);
			if (err != ERR_NONE)
				goto exit;

			/* Check that the field belongs to the object */
			found = FALSE;
			for (k = obj_type; k; k = m_class_get_parent (k)) {
				if (k == f->parent) {
					found = TRUE;
					break;
				}
			}
			if (!found)
				goto invalid_fieldid;

			if (f->type->attrs & FIELD_ATTRIBUTE_STATIC) {
				guint8 *val;
				MonoVTable *vtable;

				if (mono_class_field_is_special_static (f))
					goto invalid_fieldid;

				g_assert (f->type->attrs & FIELD_ATTRIBUTE_STATIC);
				vtable = mono_class_vtable_checked (obj->vtable->domain, f->parent, error);
				if (!is_ok (error)) {
					mono_error_cleanup (error);
					goto invalid_object;
				}
				val = (guint8 *)g_malloc (mono_class_instance_size (mono_class_from_mono_type_internal (f->type)));
				mono_field_static_get_value_checked (vtable, f, val, string_handle, error);
				if (!is_ok (error)) {
					mono_error_cleanup (error); /* FIXME report the error */
					goto invalid_object;
				}
				buffer_add_value (buf, f->type, val, obj->vtable->domain);
				g_free (val);
			} else {
				void *field_value = NULL;
#ifndef DISABLE_REMOTING
				void *field_storage = NULL;
#endif
				if (remote_obj) {
#ifndef DISABLE_REMOTING
					field_value = mono_load_remote_field_checked(obj, obj_type, f, &field_storage, error);
					if (!is_ok (error)) {
						mono_error_cleanup (error); /* FIXME report the error */
						goto invalid_object;
					}
#else
					g_assert_not_reached ();
#endif
				} else
					field_value = (guint8*)obj + f->offset;

				buffer_add_value (buf, f->type, field_value, obj->vtable->domain);
			}
		}
		break;
	case CMD_OBJECT_REF_SET_VALUES:
		len = decode_int (p, &p, end);

		for (i = 0; i < len; ++i) {
			f = decode_fieldid (p, &p, end, NULL, &err);
			if (err != ERR_NONE)
				goto exit;

			/* Check that the field belongs to the object */
			found = FALSE;
			for (k = obj_type; k; k = m_class_get_parent (k)) {
				if (k == f->parent) {
					found = TRUE;
					break;
				}
			}
			if (!found)
				goto invalid_fieldid;

			if (f->type->attrs & FIELD_ATTRIBUTE_STATIC) {
				guint8 *val;
				MonoVTable *vtable;

				if (mono_class_field_is_special_static (f))
					goto invalid_fieldid;

				g_assert (f->type->attrs & FIELD_ATTRIBUTE_STATIC);
				vtable = mono_class_vtable_checked (obj->vtable->domain, f->parent, error);
				if (!is_ok (error)) {
					mono_error_cleanup (error);
					goto invalid_fieldid;
				}

				val = (guint8 *)g_malloc (mono_class_instance_size (mono_class_from_mono_type_internal (f->type)));
				err = decode_value (f->type, obj->vtable->domain, val, p, &p, end, TRUE);
				if (err != ERR_NONE) {
					g_free (val);
					goto exit;
				}
				mono_field_static_set_value_internal (vtable, f, val);
				g_free (val);
			} else {
				err = decode_value (f->type, obj->vtable->domain, (guint8*)obj + f->offset, p, &p, end, TRUE);
				if (err != ERR_NONE)
					goto exit;
			}
		}
		break;
	case CMD_OBJECT_REF_GET_ADDRESS:
		buffer_add_long (buf, (gssize)obj);
		break;
	case CMD_OBJECT_REF_GET_DOMAIN:
		buffer_add_domainid (buf, obj->vtable->domain);
		break;
	case CMD_OBJECT_REF_GET_INFO:
		buffer_add_typeid (buf, obj->vtable->domain, mono_class_from_mono_type_internal (((MonoReflectionType*)obj->vtable->type)->type));
		buffer_add_domainid (buf, obj->vtable->domain);
		break;
	default:
		err = ERR_NOT_IMPLEMENTED;
		goto exit;
	}

	err = ERR_NONE;
	goto exit;
invalid_fieldid:
	err = ERR_INVALID_FIELDID;
	goto exit;
invalid_object:
	err = ERR_INVALID_OBJECT;
	goto exit;
exit:
	HANDLE_FUNCTION_RETURN_VAL (err);
}

static const char*
command_set_to_string (CommandSet command_set)
{
	switch (command_set) {
	case CMD_SET_VM:
		return "VM";
	case CMD_SET_OBJECT_REF:
		return "OBJECT_REF";
	case CMD_SET_STRING_REF:
		return "STRING_REF";
	case CMD_SET_THREAD:
		return "THREAD";
	case CMD_SET_ARRAY_REF:
		return "ARRAY_REF";
	case CMD_SET_EVENT_REQUEST:
		return "EVENT_REQUEST";
	case CMD_SET_STACK_FRAME:
		return "STACK_FRAME";
	case CMD_SET_APPDOMAIN:
		return "APPDOMAIN";
	case CMD_SET_ASSEMBLY:
		return "ASSEMBLY";
	case CMD_SET_METHOD:
		return "METHOD";
	case CMD_SET_TYPE:
		return "TYPE";
	case CMD_SET_MODULE:
		return "MODULE";
	case CMD_SET_FIELD:
		return "FIELD";
	case CMD_SET_EVENT:
		return "EVENT";
	case CMD_SET_POINTER:
		return "POINTER";
	default:
		return "";
	}
}

static const char* vm_cmds_str [] = {
	"VERSION",
	"ALL_THREADS",
	"SUSPEND",
	"RESUME",
	"EXIT",
	"DISPOSE",
	"INVOKE_METHOD",
	"SET_PROTOCOL_VERSION",
	"ABORT_INVOKE",
	"SET_KEEPALIVE"
	"GET_TYPES_FOR_SOURCE_FILE",
	"GET_TYPES",
	"INVOKE_METHODS"
};

static const char* thread_cmds_str[] = {
	"GET_FRAME_INFO",
	"GET_NAME",
	"GET_STATE",
	"GET_INFO",
	"GET_ID",
	"GET_TID",
	"SET_IP"
};

static const char* event_cmds_str[] = {
	"REQUEST_SET",
	"REQUEST_CLEAR",
	"REQUEST_CLEAR_ALL_BREAKPOINTS"
};

static const char* appdomain_cmds_str[] = {
	"GET_ROOT_DOMAIN",
	"GET_FRIENDLY_NAME",
	"GET_ASSEMBLIES",
	"GET_ENTRY_ASSEMBLY",
	"CREATE_STRING",
	"GET_CORLIB",
	"CREATE_BOXED_VALUE",
	"CREATE_BYTE_ARRAY",
};

static const char* assembly_cmds_str[] = {
	"GET_LOCATION",
	"GET_ENTRY_POINT",
	"GET_MANIFEST_MODULE",
	"GET_OBJECT",
	"GET_TYPE",
	"GET_NAME",
	"GET_DOMAIN",
	"HAS_DEBUG_INFO"
};

static const char* module_cmds_str[] = {
	"GET_INFO",
};

static const char* field_cmds_str[] = {
	"GET_INFO",
};

static const char* method_cmds_str[] = {
	"GET_NAME",
	"GET_DECLARING_TYPE",
	"GET_DEBUG_INFO",
	"GET_PARAM_INFO",
	"GET_LOCALS_INFO",
	"GET_INFO",
	"GET_BODY",
	"RESOLVE_TOKEN",
	"GET_CATTRS ",
	"MAKE_GENERIC_METHOD"
};

static const char* type_cmds_str[] = {
	"GET_INFO",
	"GET_METHODS",
	"GET_FIELDS",
	"GET_VALUES",
	"GET_OBJECT",
	"GET_SOURCE_FILES",
	"SET_VALUES",
	"IS_ASSIGNABLE_FROM",
	"GET_PROPERTIES ",
	"GET_CATTRS",
	"GET_FIELD_CATTRS",
	"GET_PROPERTY_CATTRS",
	"GET_SOURCE_FILES_2",
	"GET_VALUES_2",
	"GET_METHODS_BY_NAME_FLAGS",
	"GET_INTERFACES",
	"GET_INTERFACE_MAP",
	"IS_INITIALIZED",
	"CREATE_INSTANCE",
	"GET_VALUE_SIZE"
};

static const char* stack_frame_cmds_str[] = {
	"GET_VALUES",
	"GET_THIS",
	"SET_VALUES",
	"GET_DOMAIN",
	"SET_THIS"
};

static const char* array_cmds_str[] = {
	"GET_LENGTH",
	"GET_VALUES",
	"SET_VALUES",
};

static const char* string_cmds_str[] = {
	"GET_VALUE",
	"GET_LENGTH",
	"GET_CHARS"
};

static const char* pointer_cmds_str[] = {
	"GET_VALUE"
};

static const char* object_cmds_str[] = {
	"GET_TYPE",
	"GET_VALUES",
	"IS_COLLECTED",
	"GET_ADDRESS",
	"GET_DOMAIN",
	"SET_VALUES",
	"GET_INFO",
};

static const char*
cmd_to_string (CommandSet set, int command)
{
	const char **cmds;
	int cmds_len = 0;

	switch (set) {
	case CMD_SET_VM:
		cmds = vm_cmds_str;
		cmds_len = G_N_ELEMENTS (vm_cmds_str);
		break;
	case CMD_SET_OBJECT_REF:
		cmds = object_cmds_str;
		cmds_len = G_N_ELEMENTS (object_cmds_str);
		break;
	case CMD_SET_STRING_REF:
		cmds = string_cmds_str;
		cmds_len = G_N_ELEMENTS (string_cmds_str);
		break;
	case CMD_SET_THREAD:
		cmds = thread_cmds_str;
		cmds_len = G_N_ELEMENTS (thread_cmds_str);
		break;
	case CMD_SET_ARRAY_REF:
		cmds = array_cmds_str;
		cmds_len = G_N_ELEMENTS (array_cmds_str);
		break;
	case CMD_SET_EVENT_REQUEST:
		cmds = event_cmds_str;
		cmds_len = G_N_ELEMENTS (event_cmds_str);
		break;
	case CMD_SET_STACK_FRAME:
		cmds = stack_frame_cmds_str;
		cmds_len = G_N_ELEMENTS (stack_frame_cmds_str);
		break;
	case CMD_SET_APPDOMAIN:
		cmds = appdomain_cmds_str;
		cmds_len = G_N_ELEMENTS (appdomain_cmds_str);
		break;
	case CMD_SET_ASSEMBLY:
		cmds = assembly_cmds_str;
		cmds_len = G_N_ELEMENTS (assembly_cmds_str);
		break;
	case CMD_SET_METHOD:
		cmds = method_cmds_str;
		cmds_len = G_N_ELEMENTS (method_cmds_str);
		break;
	case CMD_SET_TYPE:
		cmds = type_cmds_str;
		cmds_len = G_N_ELEMENTS (type_cmds_str);
		break;
	case CMD_SET_MODULE:
		cmds = module_cmds_str;
		cmds_len = G_N_ELEMENTS (module_cmds_str);
		break;
	case CMD_SET_FIELD:
		cmds = field_cmds_str;
		cmds_len = G_N_ELEMENTS (field_cmds_str);
		break;
	case CMD_SET_EVENT:
		cmds = event_cmds_str;
		cmds_len = G_N_ELEMENTS (event_cmds_str);
		break;
	case CMD_SET_POINTER:
		cmds = pointer_cmds_str;
		cmds_len = G_N_ELEMENTS (pointer_cmds_str);
		break;
	default:
		return NULL;
	}
	if (command > 0 && command <= cmds_len)
		return cmds [command - 1];
	else
		return NULL;
}

static gboolean
wait_for_attach (void)
{
#ifndef DISABLE_SOCKET_TRANSPORT
	if (listen_fd == -1) {
		DEBUG_PRINTF (1, "[dbg] Invalid listening socket\n");
		return FALSE;
	}

	/* Block and wait for client connection */
	conn_fd = socket_transport_accept (listen_fd);

	DEBUG_PRINTF (1, "Accepted connection on %d\n", conn_fd);
	if (conn_fd == -1) {
		DEBUG_PRINTF (1, "[dbg] Bad client connection\n");
		return FALSE;
	}
#else
	g_assert_not_reached ();
#endif

	/* Handshake */
	disconnected = !transport_handshake ();
	if (disconnected) {
		DEBUG_PRINTF (1, "Transport handshake failed!\n");
		return FALSE;
	}
	
	return TRUE;
}

/*
 * debugger_thread:
 *
 *   This thread handles communication with the debugger client using a JDWP
 * like protocol.
 */
static gsize WINAPI
debugger_thread (void *arg)
{
	int res, len, id, flags, command = 0;
	CommandSet command_set = (CommandSet)0;
	guint8 header [HEADER_LENGTH];
	guint8 *data, *p, *end;
	Buffer buf;
	ErrorCode err;
	gboolean no_reply;
	gboolean attach_failed = FALSE;

	DEBUG_PRINTF (1, "[dbg] Agent thread started, pid=%p\n", (gpointer) (gsize) mono_native_thread_id_get ());

	gboolean log_each_step = g_hasenv ("MONO_DEBUGGER_LOG_AFTER_COMMAND");

	debugger_thread_id = mono_native_thread_id_get ();

	MonoInternalThread *internal = mono_thread_internal_current ();
	mono_thread_set_name_constant_ignore_error (internal, "Debugger agent", MonoSetThreadNameFlag_Permanent);

	internal->state |= ThreadState_Background;
	internal->flags |= MONO_THREAD_FLAG_DONT_MANAGE;

	if (agent_config.defer) {
		if (!wait_for_attach ()) {
			DEBUG_PRINTF (1, "[dbg] Can't attach, aborting debugger thread.\n");
			attach_failed = TRUE; // Don't abort process when we can't listen
		} else {
			mono_set_is_debugger_attached (TRUE);
			/* Send start event to client */
			process_profiler_event (EVENT_KIND_VM_START, mono_thread_get_main ());
		}
	} else {
		mono_set_is_debugger_attached (TRUE);
	}
	
	while (!attach_failed) {
		res = transport_recv (header, HEADER_LENGTH);

		/* This will break if the socket is closed during shutdown too */
		if (res != HEADER_LENGTH) {
			DEBUG_PRINTF (1, "[dbg] transport_recv () returned %d, expected %d.\n", res, HEADER_LENGTH);
			command_set = (CommandSet)0;
			command = 0;
			dispose_vm ();
			break;
		} else {
			p = header;
			end = header + HEADER_LENGTH;

			len = decode_int (p, &p, end);
			id = decode_int (p, &p, end);
			flags = decode_byte (p, &p, end);
			command_set = (CommandSet)decode_byte (p, &p, end);
			command = decode_byte (p, &p, end);
		}

		g_assert (flags == 0);
		const char *cmd_str;
		char cmd_num [256];

		cmd_str = cmd_to_string (command_set, command);
		if (!cmd_str) {
			sprintf (cmd_num, "%d", command);
			cmd_str = cmd_num;
		}

		if (log_level) {
			DEBUG_PRINTF (1, "[dbg] Command %s(%s) [%d][at=%lx].\n", command_set_to_string (command_set), cmd_str, id, (long)mono_100ns_ticks () / 10000);
		}

		data = (guint8 *)g_malloc (len - HEADER_LENGTH);
		if (len - HEADER_LENGTH > 0)
		{
			res = transport_recv (data, len - HEADER_LENGTH);
			if (res != len - HEADER_LENGTH) {
				DEBUG_PRINTF (1, "[dbg] transport_recv () returned %d, expected %d.\n", res, len - HEADER_LENGTH);
				break;
			}
		}

		p = data;
		end = data + (len - HEADER_LENGTH);

		buffer_init (&buf, 128);

		err = ERR_NONE;
		no_reply = FALSE;

		/* Process the request */
		switch (command_set) {
		case CMD_SET_VM:
			err = vm_commands (command, id, p, end, &buf);
			if (err == ERR_NONE && command == CMD_VM_INVOKE_METHOD)
				/* Sent after the invoke is complete */
				no_reply = TRUE;
			break;
		case CMD_SET_EVENT_REQUEST:
			err = event_commands (command, p, end, &buf);
			break;
		case CMD_SET_APPDOMAIN:
			err = domain_commands (command, p, end, &buf);
			break;
		case CMD_SET_ASSEMBLY:
			err = assembly_commands (command, p, end, &buf);
			break;
		case CMD_SET_MODULE:
			err = module_commands (command, p, end, &buf);
			break;
		case CMD_SET_FIELD:
			err = field_commands (command, p, end, &buf);
			break;
		case CMD_SET_TYPE:
			err = type_commands (command, p, end, &buf);
			break;
		case CMD_SET_METHOD:
			err = method_commands (command, p, end, &buf);
			break;
		case CMD_SET_THREAD:
			err = thread_commands (command, p, end, &buf);
			break;
		case CMD_SET_STACK_FRAME:
			err = frame_commands (command, p, end, &buf);
			break;
		case CMD_SET_ARRAY_REF:
			err = array_commands (command, p, end, &buf);
			break;
		case CMD_SET_STRING_REF:
			err = string_commands (command, p, end, &buf);
			break;
		case CMD_SET_POINTER:
			err = pointer_commands (command, p, end, &buf);
			break;
		case CMD_SET_OBJECT_REF:
			err = object_commands (command, p, end, &buf);
			break;
		default:
			err = ERR_NOT_IMPLEMENTED;
		}		

		if (command_set == CMD_SET_VM && command == CMD_VM_START_BUFFERING) {
			buffer_replies = TRUE;
		}

		if (!no_reply) {
			if (buffer_replies) {
				buffer_reply_packet (id, err, &buf);
			} else {
				send_reply_packet (id, err, &buf);
				//DEBUG_PRINTF (1, "[dbg] Sent reply to %d [at=%lx].\n", id, (long)mono_100ns_ticks () / 10000);
			}
		}

		mono_debugger_log_command (command_set_to_string (command_set), cmd_str, buf.buf, buffer_len (&buf));

		if (err == ERR_NONE && command_set == CMD_SET_VM && command == CMD_VM_STOP_BUFFERING) {
			send_buffered_reply_packets ();
			buffer_replies = FALSE;
		}

		g_free (data);
		buffer_free (&buf);

		if (log_each_step) {
			char *debugger_log = mono_debugger_state_str ();
			if (debugger_log) {
				fprintf (stderr, "Debugger state: %s\n", debugger_log);
				g_free (debugger_log);
			}
		}

		if (command_set == CMD_SET_VM && (command == CMD_VM_DISPOSE || command == CMD_VM_EXIT))
			break;
	}

	mono_set_is_debugger_attached (FALSE);

	mono_coop_mutex_lock (&debugger_thread_exited_mutex);
	debugger_thread_exited = TRUE;
	mono_coop_cond_signal (&debugger_thread_exited_cond);
	mono_coop_mutex_unlock (&debugger_thread_exited_mutex);

	DEBUG_PRINTF (1, "[dbg] Debugger thread exited.\n");
	
	if (!attach_failed && command_set == CMD_SET_VM && command == CMD_VM_DISPOSE && !(vm_death_event_sent || mono_runtime_is_shutting_down ())) {
		DEBUG_PRINTF (2, "[dbg] Detached - restarting clean debugger thread.\n");
		ERROR_DECL (error);
		start_debugger_thread (error);
		mono_error_cleanup (error);
	}

	return 0;
}

void
mono_debugger_agent_init (void)
{
	MonoDebuggerCallbacks cbs;

	memset (&cbs, 0, sizeof (cbs));
	cbs.version = MONO_DBG_CALLBACKS_VERSION;
	cbs.parse_options = debugger_agent_parse_options;
	cbs.init = debugger_agent_init;
	cbs.breakpoint_hit = debugger_agent_breakpoint_hit;
	cbs.single_step_event = debugger_agent_single_step_event;
	cbs.single_step_from_context = debugger_agent_single_step_from_context;
	cbs.breakpoint_from_context = debugger_agent_breakpoint_from_context;
	cbs.free_domain_info = debugger_agent_free_domain_info;
	cbs.unhandled_exception = debugger_agent_unhandled_exception;
	cbs.handle_exception = debugger_agent_handle_exception;
	cbs.begin_exception_filter = debugger_agent_begin_exception_filter;
	cbs.end_exception_filter = debugger_agent_end_exception_filter;
	cbs.user_break = debugger_agent_user_break;
	cbs.debug_log = debugger_agent_debug_log;
	cbs.debug_log_is_enabled = debugger_agent_debug_log_is_enabled;
	cbs.send_crash = mono_debugger_agent_send_crash;

	mini_install_dbg_callbacks (&cbs);
}

void
mono_debugger_agent_parse_options (char *options)
{
	sdb_options = options;
}

#endif /* DISABLE_SDB */
