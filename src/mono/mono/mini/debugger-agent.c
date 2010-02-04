/*
 * debugger-agent.c: Debugger back-end module
 *
 * Author:
 *   Zoltan Varga (vargaz@gmail.com)
 *
 * (C) 2009 Novell, Inc.
 */

#include <config.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#ifdef HAVE_SYS_TYPES_H
#include <sys/types.h>
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
#ifdef HAVE_NETDB_H
#include <netdb.h>
#endif
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#include <errno.h>
#include <glib.h>

#ifdef HAVE_PTHREAD_H
#include <pthread.h>
#endif

#ifdef HAVE_UCONTEXT_H
#include <ucontext.h>
#endif

#ifdef HOST_WIN32
#ifdef _MSC_VER
#include <winsock2.h>
#endif
#include <ws2tcpip.h>
#ifdef __GNUC__
/* cygwin's headers do not seem to define these */
void WSAAPI freeaddrinfo (struct addrinfo*);
int WSAAPI getaddrinfo (const char*,const char*,const struct addrinfo*,
                        struct addrinfo**);
int WSAAPI getnameinfo(const struct sockaddr*,socklen_t,char*,DWORD,
                       char*,DWORD,int);
#endif
#endif

#ifdef PLATFORM_ANDROID
#include <linux/in.h>
#include <linux/tcp.h>
#include <sys/endian.h>
#endif

#include <mono/metadata/mono-debug.h>
#include <mono/metadata/mono-debug-debugger.h>
#include <mono/metadata/debug-mono-symfile.h>
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/socket-io.h>
#include <mono/utils/mono-semaphore.h>
#include "debugger-agent.h"
#include "mini.h"

#ifndef MONO_ARCH_SOFT_DEBUG_SUPPORTED
#define DISABLE_DEBUGGER_AGENT 1
#endif

#ifdef DISABLE_SOFT_DEBUG
#define DISABLE_DEBUGGER_AGENT 1
#endif

#ifndef DISABLE_DEBUGGER_AGENT
#include <mono/io-layer/mono-mutex.h>

/* Definitions to make backporting to 2.6 easier */
//#define MonoInternalThread MonoThread
//#define mono_thread_internal_current mono_thread_current
#define THREAD_TO_INTERNAL(thread) (thread)->internal_thread

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
} AgentConfig;

typedef struct
{
	int id;
	guint32 il_offset;
	MonoDomain *domain;
	MonoMethod *method;
	MonoContext ctx;
	MonoDebugMethodJitInfo *jit;
	int flags;
	/*
	 * Whenever ctx is set. This is FALSE for the last frame of running threads, since
	 * the frame can become invalid.
	 */
	gboolean has_ctx;
} StackFrame;

typedef struct
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
} InvokeData;

typedef struct {
	MonoContext ctx;
	MonoLMF *lmf;
	MonoDomain *domain;
	gboolean has_context;
	gpointer resume_event;
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
	InvokeData *invoke;
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

	/* Number of thread interruptions not yet processed */
	gint32 interrupt_count;

	/* Whenever to disable breakpoints (used during invokes) */
	gboolean disable_breakpoints;

	/*
	 * Number of times this thread has been resumed using resume_thread ().
	 */
	guint32 resume_count;

	MonoInternalThread *thread;

	/*
	 * Information about the frame which transitioned to native code for running
	 * threads.
	 */
	StackFrameInfo async_last_frame;

	/*
	 * The context where the stack walk can be started for running threads.
	 */
	MonoContext async_ctx;

	gboolean has_async_ctx;

	/*
	 * The lmf where the stack walk can be started for running threads.
	 */
	gpointer async_lmf;

	/*
 	 * The callee address of the last mono_runtime_invoke call
	 */
	gpointer invoke_addr;
} DebuggerTlsData;

/* 
 * Wire Protocol definitions
 */

#define HEADER_LENGTH 11

#define MAJOR_VERSION 2
#define MINOR_VERSION 0

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
	CMD_SET_EVENT = 64
} CommandSet;

typedef enum {
	EVENT_KIND_VM_START = 0,
	EVENT_KIND_VM_DEATH = 1,
	EVENT_KIND_THREAD_START = 2,
	EVENT_KIND_THREAD_DEATH = 3,
	EVENT_KIND_APPDOMAIN_CREATE = 4,
	EVENT_KIND_APPDOMAIN_UNLOAD = 5,
	EVENT_KIND_METHOD_ENTRY = 6,
	EVENT_KIND_METHOD_EXIT = 7,
	EVENT_KIND_ASSEMBLY_LOAD = 8,
	EVENT_KIND_ASSEMBLY_UNLOAD = 9,
	EVENT_KIND_BREAKPOINT = 10,
	EVENT_KIND_STEP = 11,
	EVENT_KIND_TYPE_LOAD = 12,
	EVENT_KIND_EXCEPTION = 13
} EventKind;

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
	ERR_UNLOADED = 103
} ErrorCode;

typedef enum {
	MOD_KIND_COUNT = 1,
	MOD_KIND_THREAD_ONLY = 3,
	MOD_KIND_LOCATION_ONLY = 7,
	MOD_KIND_EXCEPTION_ONLY = 8,
	MOD_KIND_STEP = 10,
	MOD_KIND_ASSEMBLY_ONLY = 11
} ModifierKind;

typedef enum {
	STEP_DEPTH_INTO = 0,
	STEP_DEPTH_OVER = 1,
	STEP_DEPTH_OUT = 2
} StepDepth;

typedef enum {
	STEP_SIZE_MIN = 0,
	STEP_SIZE_LINE = 1
} StepSize;

typedef enum {
	TOKEN_TYPE_STRING = 0,
	TOKEN_TYPE_TYPE = 1,
	TOKEN_TYPE_FIELD = 2,
	TOKEN_TYPE_METHOD = 3,
	TOKEN_TYPE_UNKNOWN = 4
} DebuggerTokenType;

typedef enum {
	VALUE_TYPE_ID_NULL = 0xf0,
	VALUE_TYPE_ID_TYPE = 0xf1
} ValueTypeId;

typedef enum {
	FRAME_FLAG_DEBUGGER_INVOKE = 1
} StackFrameFlags;

typedef enum {
	INVOKE_FLAG_DISABLE_BREAKPOINTS = 1,
	INVOKE_FLAG_SINGLE_THREADED = 2
} InvokeFlags;

typedef enum {
	CMD_VM_VERSION = 1,
	CMD_VM_ALL_THREADS = 2,
	CMD_VM_SUSPEND = 3,
	CMD_VM_RESUME = 4,
	CMD_VM_EXIT = 5,
	CMD_VM_DISPOSE = 6,
	CMD_VM_INVOKE_METHOD = 7
} CmdVM;

typedef enum {
	CMD_THREAD_GET_FRAME_INFO = 1,
	CMD_THREAD_GET_NAME = 2,
	CMD_THREAD_GET_STATE = 3,
	CMD_THREAD_GET_INFO = 4
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
	CMD_APPDOMAIN_GET_CORLIB = 6
} CmdAppDomain;

typedef enum {
	CMD_ASSEMBLY_GET_LOCATION = 1,
	CMD_ASSEMBLY_GET_ENTRY_POINT = 2,
	CMD_ASSEMBLY_GET_MANIFEST_MODULE = 3,
	CMD_ASSEMBLY_GET_OBJECT = 4,
	CMD_ASSEMBLY_GET_TYPE = 5,
	CMD_ASSEMBLY_GET_NAME = 6
} CmdAssembly;

typedef enum {
	CMD_MODULE_GET_INFO = 1,
} CmdModule;

typedef enum {
	CMD_METHOD_GET_NAME = 1,
	CMD_METHOD_GET_DECLARING_TYPE = 2,
	CMD_METHOD_GET_DEBUG_INFO = 3,
	CMD_METHOD_GET_PARAM_INFO = 4,
	CMD_METHOD_GET_LOCALS_INFO = 5,
	CMD_METHOD_GET_INFO = 6,
	CMD_METHOD_GET_BODY = 7,
	CMD_METHOD_RESOLVE_TOKEN = 8,
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
	CMD_TYPE_GET_PROPERTY_CATTRS = 12
} CmdType;

typedef enum {
	CMD_STACK_FRAME_GET_VALUES = 1,
	CMD_STACK_FRAME_GET_THIS = 2,
	CMD_STACK_FRAME_SET_VALUES = 3
} CmdStackFrame;

typedef enum {
	CMD_ARRAY_REF_GET_LENGTH = 1,
	CMD_ARRAY_REF_GET_VALUES = 2,
	CMD_ARRAY_REF_SET_VALUES = 3,
} CmdArray;

typedef enum {
	CMD_STRING_REF_GET_VALUE = 1,
} CmdString;

typedef enum {
	CMD_OBJECT_REF_GET_TYPE = 1,
	CMD_OBJECT_REF_GET_VALUES = 2,
	CMD_OBJECT_REF_IS_COLLECTED = 3,
	CMD_OBJECT_REF_GET_ADDRESS = 4,
	CMD_OBJECT_REF_GET_DOMAIN = 5,
	CMD_OBJECT_REF_SET_VALUES = 6
} CmdObject;

typedef struct {
	ModifierKind kind;
	union {
		int count; /* For kind == MOD_KIND_COUNT */
		MonoInternalThread *thread; /* For kind == MOD_KIND_THREAD_ONLY */
		MonoClass *exc_class; /* For kind == MONO_KIND_EXCEPTION_ONLY */
		MonoAssembly **assemblies; /* For kind == MONO_KIND_ASSEMBLY_ONLY */
	} data;
} Modifier;

typedef struct{
	int id;
	int event_kind;
	int suspend_policy;
	int nmodifiers;
	gpointer info;
	Modifier modifiers [MONO_ZERO_LEN_ARRAY];
} EventRequest;

/*
 * Describes a single step request.
 */
typedef struct {
	EventRequest *req;
	MonoInternalThread *thread;
	StepDepth depth;
	StepSize size;
	gpointer last_sp;
	gpointer start_sp;
	MonoMethod *last_method;
	int last_line;
	/* Whenever single stepping is performed using start/stop_single_stepping () */
	gboolean global;
	/* The list of breakpoints used to implement step-over */
	GSList *bps;
} SingleStepReq;

/* Dummy structure used for the profiler callbacks */
typedef struct {
	void* dummy;
} DebuggerProfiler;

#define DEBUG(level,s) do { if (G_UNLIKELY ((level) <= log_level)) { s; fflush (log_file); } } while (0)

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

static int conn_fd;

static int packet_id = 0;

static int objref_id = 0;

static int event_request_id = 0;

static int frame_id = 0;

static GPtrArray *event_requests;

static guint32 debugger_tls_id;

static gboolean vm_start_event_sent, vm_death_event_sent, disconnected;

/* Maps MonoInternalThread -> DebuggerTlsData */
static MonoGHashTable *thread_to_tls;

/* Maps tid -> MonoInternalThread */
static MonoGHashTable *tid_to_thread;

/* Maps tid -> MonoThread (not MonoInternalThread) */
static MonoGHashTable *tid_to_thread_obj;

static gsize debugger_thread_id;

static HANDLE debugger_thread_handle;

static int log_level;

static FILE *log_file;

/* Classes whose class load event has been sent */
static GHashTable *loaded_classes;

/* Assemblies whose assembly load event has no been sent yet */
static GPtrArray *pending_assembly_loads;

/* Whenever the debugger thread has exited */
static gboolean debugger_thread_exited;

/* Cond variable used to wait for debugger_thread_exited becoming true */
static mono_cond_t debugger_thread_exited_cond;

/* Mutex for the cond var above */
static mono_mutex_t debugger_thread_exited_mutex;

static DebuggerProfiler debugger_profiler;

/* The single step request instance */
static SingleStepReq *ss_req = NULL;
static gpointer ss_invoke_addr = NULL;

#ifdef MONO_ARCH_SOFT_DEBUG_SUPPORTED
/* Number of single stepping operations in progress */
static int ss_count;
#endif

static void transport_connect (const char *host, int port);

static guint32 WINAPI debugger_thread (void *arg);

static void runtime_initialized (MonoProfiler *prof);

static void runtime_shutdown (MonoProfiler *prof);

static void thread_startup (MonoProfiler *prof, gsize tid);

static void thread_end (MonoProfiler *prof, gsize tid);

static void appdomain_load (MonoProfiler *prof, MonoDomain *domain, int result);

static void appdomain_unload (MonoProfiler *prof, MonoDomain *domain);

static void assembly_load (MonoProfiler *prof, MonoAssembly *assembly, int result);

static void assembly_unload (MonoProfiler *prof, MonoAssembly *assembly);

static void start_runtime_invoke (MonoProfiler *prof, MonoMethod *method);

static void end_runtime_invoke (MonoProfiler *prof, MonoMethod *method);

static void jit_end (MonoProfiler *prof, MonoMethod *method, MonoJitInfo *jinfo, int result);

static void add_pending_breakpoints (MonoMethod *method, MonoJitInfo *jinfo);

static void start_single_stepping (void);

static void stop_single_stepping (void);

static void suspend_current (void);

/* Submodule init/cleanup */
static void breakpoints_init (void);
static void breakpoints_cleanup (void);

static void objrefs_init (void);
static void objrefs_cleanup (void);

static void ids_init (void);
static void ids_cleanup (void);

static void suspend_init (void);

static void ss_start (SingleStepReq *ss_req, MonoMethod *method, SeqPoint *sp, MonoSeqPointInfo *info, MonoContext *ctx, DebuggerTlsData *tls);
static ErrorCode ss_create (MonoInternalThread *thread, StepSize size, StepDepth depth, EventRequest *req);
static void ss_destroy (SingleStepReq *req);

static void start_debugger_thread (void);

static void finish_agent_init (gboolean on_startup);

static int
parse_address (char *address, char **host, int *port)
{
	char *pos = strchr (address, ':');

	if (pos == NULL || pos == address)
		return 1;

	*host = g_malloc (pos - address + 1);
	strncpy (*host, address, pos - address);
	(*host) [pos - address] = '\0';

	*port = atoi (pos + 1);

	return 0;
}

static void
print_usage (void)
{
	fprintf (stderr, "Usage: mono --debugger-agent=[<option>=<value>,...] ...\n");
	fprintf (stderr, "Available options:\n");
	fprintf (stderr, "  transport=<transport>\t\tTransport to use for connecting to the debugger (mandatory, possible values: 'dt_socket')\n");
	fprintf (stderr, "  address=<hostname>:<port>\tAddress to connect to (mandatory)\n");
	fprintf (stderr, "  loglevel=<n>\t\t\tLog level (defaults to 0)\n");
	fprintf (stderr, "  logfile=<file>\t\tFile to log to (defaults to stdout)\n");
	fprintf (stderr, "  suspend=y/n\t\t\tWhenever to suspend after startup.\n");
	fprintf (stderr, "  timeout=<n>\t\t\tTimeout for connecting in milliseconds.\n");
	fprintf (stderr, "  help\t\t\t\tPrint this help.\n");
}

static gboolean
parse_flag (const char *option, char *flag)
{
	if (!strcmp (flag, "y"))
		return TRUE;
	else if (!strcmp (flag, "n"))
		return FALSE;
	else {
		fprintf (stderr, "debugger-agent: The valid values for the '%s' option are 'y' and 'n'.\n", option);
		exit (1);
		return FALSE;
	}
}

void
mono_debugger_agent_parse_options (char *options)
{
	char **args, **ptr;
	char *host;
	int port;

#ifndef MONO_ARCH_SOFT_DEBUG_SUPPORTED
	fprintf (stderr, "--debugger-agent is not supported on this platform.\n");
	exit (1);
#endif

	agent_config.enabled = TRUE;
	agent_config.suspend = TRUE;
	agent_config.server = FALSE;

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
		} else {
			print_usage ();
			exit (1);
		}
	}

	if (agent_config.transport == NULL) {
		fprintf (stderr, "debugger-agent: The 'transport' option is mandatory.\n");
		exit (1);
	}
	if (strcmp (agent_config.transport, "dt_socket") != 0) {
		fprintf (stderr, "debugger-agent: The only supported value for the 'transport' option is 'dt_socket'.\n");
		exit (1);
	}

	if (agent_config.address == NULL && !agent_config.server) {
		fprintf (stderr, "debugger-agent: The 'address' option is mandatory.\n");
		exit (1);
	}

	if (agent_config.address && parse_address (agent_config.address, &host, &port)) {
		fprintf (stderr, "debugger-agent: The format of the 'address' options is '<host>:<port>'\n");
		exit (1);
	}
}

void
mono_debugger_agent_init (void)
{
	if (!agent_config.enabled)
		return;

	/* Need to know whenever a thread has acquired the loader mutex */
	mono_loader_lock_track_ownership (TRUE);

	event_requests = g_ptr_array_new ();

	mono_mutex_init (&debugger_thread_exited_mutex, NULL);
	mono_cond_init (&debugger_thread_exited_cond, NULL);

	mono_profiler_install ((MonoProfiler*)&debugger_profiler, runtime_shutdown);
	mono_profiler_set_events (MONO_PROFILE_APPDOMAIN_EVENTS | MONO_PROFILE_THREADS | MONO_PROFILE_ASSEMBLY_EVENTS | MONO_PROFILE_JIT_COMPILATION | MONO_PROFILE_METHOD_EVENTS);
	mono_profiler_install_runtime_initialized (runtime_initialized);
	mono_profiler_install_appdomain (NULL, appdomain_load, NULL, appdomain_unload);
	mono_profiler_install_thread (thread_startup, thread_end);
	mono_profiler_install_assembly (NULL, assembly_load, assembly_unload, NULL);
	mono_profiler_install_jit_end (jit_end);
	mono_profiler_install_method_invoke (start_runtime_invoke, end_runtime_invoke);

	debugger_tls_id = TlsAlloc ();

	thread_to_tls = mono_g_hash_table_new (NULL, NULL);
	MONO_GC_REGISTER_ROOT (thread_to_tls);

	tid_to_thread = mono_g_hash_table_new_type (NULL, NULL, MONO_HASH_VALUE_GC);
	MONO_GC_REGISTER_ROOT (tid_to_thread);

	tid_to_thread_obj = mono_g_hash_table_new_type (NULL, NULL, MONO_HASH_VALUE_GC);
	MONO_GC_REGISTER_ROOT (tid_to_thread_obj);

	loaded_classes = g_hash_table_new (mono_aligned_addr_hash, NULL);
	pending_assembly_loads = g_ptr_array_new ();

	log_level = agent_config.log_level;

	if (agent_config.log_file) {
		log_file = fopen (agent_config.log_file, "w+");
		if (!log_file) {
			fprintf (stderr, "Unable to create log file '%s': %s.\n", agent_config.log_file, strerror (errno));
			exit (1);
		}
	} else {
		log_file = stdout;
	}

	ids_init ();
	objrefs_init ();
	breakpoints_init ();
	suspend_init ();

	mini_get_debug_options ()->gen_seq_points = TRUE;
	/* 
	 * This is needed because currently we don't handle liveness info.
	 */
	mini_get_debug_options ()->mdb_optimizations = TRUE;

	/* This is needed because we can't set local variables in registers yet */
	mono_disable_optimizations (MONO_OPT_LINEARS);

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
	char *host;
	int port;
	int res;

	if (InterlockedCompareExchange (&inited, 1, 0) == 1)
		return;

	if (agent_config.launch) {
		char *argv [16];

		// FIXME: Generated address
		// FIXME: Races with transport_connect ()

		argv [0] = agent_config.launch;
		argv [1] = agent_config.transport;
		argv [2] = agent_config.address;
		argv [3] = NULL;

		res = g_spawn_async_with_pipes (NULL, argv, NULL, 0, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
		if (!res) {
			fprintf (stderr, "Failed to execute '%s'.\n", agent_config.launch);
			exit (1);
		}
	}

	if (agent_config.address) {
		res = parse_address (agent_config.address, &host, &port);
		g_assert (res == 0);
	} else {
		host = NULL;
		port = 0;
	}

	transport_connect (host, port);

	if (!on_startup) {
		/* Do some which is usually done after sending the VMStart () event */
		vm_start_event_sent = TRUE;
		start_debugger_thread ();
	}
}

static void
mono_debugger_agent_cleanup (void)
{
	if (!inited)
		return;

	/* This will interrupt the agent thread */
	/* Close the read part only so it can still send back replies */
#ifdef HOST_WIN32
	shutdown (conn_fd, SD_RECEIVE);
#else
	shutdown (conn_fd, SHUT_RD);
#endif

	/* 
	 * Wait for the thread to exit.
	 *
	 * If we continue with the shutdown without waiting for it, then the client might
	 * not receive an answer to its last command like a resume.
	 * The WaitForSingleObject infrastructure doesn't seem to work during shutdown, so
	 * use pthreads.
	 */
	//WaitForSingleObject (debugger_thread_handle, INFINITE);
	if (GetCurrentThreadId () != debugger_thread_id) {
		mono_mutex_lock (&debugger_thread_exited_mutex);
		if (!debugger_thread_exited) {
#ifdef HOST_WIN32
			if (WAIT_TIMEOUT == WaitForSingleObject(debugger_thread_exited_cond, 0)) {
				mono_mutex_unlock (&debugger_thread_exited_mutex);
				Sleep(0);
				mono_mutex_lock (&debugger_thread_exited_mutex);
			}
#else
			mono_cond_wait (&debugger_thread_exited_cond, &debugger_thread_exited_mutex);
#endif
		}
		mono_mutex_unlock (&debugger_thread_exited_mutex);
	}

	breakpoints_cleanup ();
	objrefs_cleanup ();
	ids_cleanup ();

#ifdef HOST_WIN32
	shutdown (conn_fd, SD_BOTH);
#else
	shutdown (conn_fd, SHUT_RDWR);
#endif
	
	mono_mutex_destroy (&debugger_thread_exited_mutex);
	mono_cond_destroy (&debugger_thread_exited_cond);
}

/*
 * transport_connect:
 *
 *   Connect/Listen on HOST:PORT. If HOST is NULL, generate an address and listen on it.
 */
static void
transport_connect (const char *host, int port)
{
	struct addrinfo hints;
	struct addrinfo *result, *rp;
	int sfd, s, res;
	char port_string [128];
	char handshake_msg [128];
	guint8 buf [128];

	conn_fd = -1;

	if (host) {
		sprintf (port_string, "%d", port);

		mono_network_init ();

		/* Obtain address(es) matching host/port */

		memset (&hints, 0, sizeof (struct addrinfo));
		hints.ai_family = AF_UNSPEC;    /* Allow IPv4 or IPv6 */
		hints.ai_socktype = SOCK_STREAM; /* Datagram socket */
		hints.ai_flags = 0;
		hints.ai_protocol = 0;          /* Any protocol */

		s = getaddrinfo (host, port_string, &hints, &result);
		if (s != 0) {
			fprintf (stderr, "debugger-agent: Unable to connect to %s:%d: %s\n", host, port, gai_strerror (s));
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
			g_assert (sfd);

			/* This will bind the socket to a random port */
			res = listen (sfd, 16);
			if (res == -1) {
				fprintf (stderr, "debugger-agent: Unable to setup listening socket: %s\n", strerror (errno));
				exit (1);
			}

			addrlen = sizeof (addr);
			memset (&addr, 0, sizeof (addr));
			res = getsockname (sfd, &addr, &addrlen);
			g_assert (res == 0);

			host = "127.0.0.1";
			port = ntohs (addr.sin_port);

			/* Emit the address to stdout */
			/* FIXME: Should print another interface, not localhost */
			printf ("%s:%d\n", host, port);
		} else {
			/* Listen on the provided address */
			for (rp = result; rp != NULL; rp = rp->ai_next) {
				sfd = socket (rp->ai_family, rp->ai_socktype,
							  rp->ai_protocol);
				if (sfd == -1)
					continue;

				res = bind (sfd, rp->ai_addr, rp->ai_addrlen);
				if (res == -1)
					continue;

				res = listen (sfd, 16);
				if (res == -1)
					continue;
				break;
			}

#ifndef HOST_WIN32
			/*
			 * this function is not present on win2000 which we still support, and the
			 * workaround described here:
			 * http://msdn.microsoft.com/en-us/library/ms737931(VS.85).aspx
			 * only works with MSVC.
			 */
			freeaddrinfo (result);
#endif
		}

		DEBUG (1, fprintf (log_file, "Listening on %s:%d (timeout=%d ms)...\n", host, port, agent_config.timeout));

		if (agent_config.timeout) {
			fd_set readfds;
			struct timeval tv;

			tv.tv_sec = 0;
			tv.tv_usec = agent_config.timeout * 1000;
			FD_ZERO (&readfds);
			FD_SET (sfd, &readfds);
			res = select (sfd + 1, &readfds, NULL, NULL, &tv);
			if (res == 0) {
				fprintf (stderr, "debugger-agent: Timed out waiting to connect.\n");
				exit (1);
			}
		}

		conn_fd = accept (sfd, NULL, NULL);
		if (conn_fd == -1) {
			fprintf (stderr, "debugger-agent: Unable to listen on %s:%d\n", host, port);
			exit (1);
		}

		DEBUG (1, fprintf (log_file, "Accepted connection from client, socket fd=%d.\n", conn_fd));
	} else {
		/* Connect to the specified address */
		/* FIXME: Respect the timeout */
		for (rp = result; rp != NULL; rp = rp->ai_next) {
			sfd = socket (rp->ai_family, rp->ai_socktype,
						  rp->ai_protocol);
			if (sfd == -1)
				continue;

			if (connect (sfd, rp->ai_addr, rp->ai_addrlen) != -1)
				break;       /* Success */
			
			close (sfd);
		}

		conn_fd = sfd;

#ifndef HOST_WIN32
		/* See the comment above */
		freeaddrinfo (result);
#endif

		if (rp == 0) {
			fprintf (stderr, "debugger-agent: Unable to connect to %s:%d\n", host, port);
			exit (1);
		}
	}
	
	/* Write handshake message */
	sprintf (handshake_msg, "DWP-Handshake");
	res = send (conn_fd, handshake_msg, strlen (handshake_msg), 0);
	g_assert (res != -1);

	/* Read answer */
	res = recv (conn_fd, buf, strlen (handshake_msg), 0);
	if ((res != strlen (handshake_msg)) || (memcmp (buf, handshake_msg, strlen (handshake_msg) != 0))) {
		fprintf (stderr, "debugger-agent: DWP handshake failed.\n");
		exit (1);
	}

	/* 
	 * Set TCP_NODELAY on the socket so the client receives events/command
	 * results immediately.
	 */
	{
		int flag = 1;
		int result = setsockopt(conn_fd,
                                 IPPROTO_TCP,
                                 TCP_NODELAY,
                                 (char *) &flag,
                                 sizeof(int));
		g_assert (result >= 0);
	}
}

static gboolean
transport_send (guint8 *data, int len)
{
	int res;

	res = send (conn_fd, data, len, 0);
	if (res != len)
		return FALSE;
	else
		return TRUE;
}

static void
start_debugger_thread (void)
{
	gsize tid;

	debugger_thread_handle = mono_create_thread (NULL, 0, debugger_thread, NULL, 0, &tid);
	g_assert (debugger_thread_handle);
}

/*
 * Functions to decode protocol data
 */

static inline int
decode_byte (guint8 *buf, guint8 **endbuf, guint8 *limit)
{
	*endbuf = buf + 1;
	g_assert (*endbuf <= limit);
	return buf [0];
}

static inline int
decode_int (guint8 *buf, guint8 **endbuf, guint8 *limit)
{
	*endbuf = buf + 4;
	g_assert (*endbuf <= limit);

	return (((int)buf [0]) << 24) | (((int)buf [1]) << 16) | (((int)buf [2]) << 8) | (((int)buf [3]) << 0);
}

static inline gint64
decode_long (guint8 *buf, guint8 **endbuf, guint8 *limit)
{
	guint32 high = decode_int (buf, &buf, limit);
	guint32 low = decode_int (buf, &buf, limit);

	*endbuf = buf;

	return ((((guint64)high) << 32) | ((guint64)low));
}

static inline int
decode_id (guint8 *buf, guint8 **endbuf, guint8 *limit)
{
	return decode_int (buf, endbuf, limit);
}

static inline char*
decode_string (guint8 *buf, guint8 **endbuf, guint8 *limit)
{
	int len = decode_int (buf, &buf, limit);
	char *s;

	s = g_malloc (len + 1);
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

typedef struct {
	guint8 *buf, *p, *end;
} Buffer;

static inline void
buffer_init (Buffer *buf, int size)
{
	buf->buf = g_malloc (size);
	buf->p = buf->buf;
	buf->end = buf->buf + size;
}

static inline void
buffer_make_room (Buffer *buf, int size)
{
	if (buf->end - buf->p < size) {
		int new_size = buf->end - buf->buf + size + 32;
		guint8 *p = g_realloc (buf->buf, new_size);
		size = buf->p - buf->buf;
		buf->buf = p;
		buf->p = p + size;
		buf->end = buf->buf + new_size;
	}
}

static inline void
buffer_add_byte (Buffer *buf, guint8 val)
{
	buffer_make_room (buf, 1);
	buf->p [0] = val;
	buf->p++;
}

static inline void
buffer_add_int (Buffer *buf, guint32 val)
{
	buffer_make_room (buf, 4);
	buf->p [0] = (val >> 24) & 0xff;
	buf->p [1] = (val >> 16) & 0xff;
	buf->p [2] = (val >> 8) & 0xff;
	buf->p [3] = (val >> 0) & 0xff;
	buf->p += 4;
}

static inline void
buffer_add_long (Buffer *buf, guint64 l)
{
	buffer_add_int (buf, (l >> 32) & 0xffffffff);
	buffer_add_int (buf, (l >> 0) & 0xffffffff);
}

static inline void
buffer_add_id (Buffer *buf, int id)
{
	buffer_add_int (buf, (guint64)id);
}

static inline void
buffer_add_data (Buffer *buf, guint8 *data, int len)
{
	buffer_make_room (buf, len);
	memcpy (buf->p, data, len);
	buf->p += len;
}

static inline void
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

static inline void
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

	id = InterlockedIncrement (&packet_id);

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
send_reply_packet (int id, int error, Buffer *data)
{
	Buffer buf;
	int len;
	gboolean res;
	
	len = data->p - data->buf + 11;
	buffer_init (&buf, len);
	buffer_add_int (&buf, len);
	buffer_add_int (&buf, id);
	buffer_add_byte (&buf, 0x80); /* flags */
	buffer_add_byte (&buf, (error >> 8) & 0xff);
	buffer_add_byte (&buf, error);
	memcpy (buf.buf + 11, data->buf, data->p - data->buf);

	res = transport_send (buf.buf, len);

	buffer_free (&buf);

	return res;
}

/*
 * OBJECT IDS
 */

/*
 * Represents an object accessible by the debugger client.
 */
typedef struct {
	/* Unique id used in the wire protocol to refer to objects */
	int id;
	/*
	 * A weakref gc handle pointing to the object. The gc handle is used to 
	 * detect if the object was garbage collected.
	 */
	guint32 handle;
} ObjRef;

/* Maps objid -> ObjRef */
static GHashTable *objrefs;

static void
free_objref (gpointer value)
{
	ObjRef *o = value;

	mono_gchandle_free (o->handle);

	g_free (o);
}

static void
objrefs_init (void)
{
	objrefs = g_hash_table_new_full (NULL, NULL, NULL, free_objref);
}

static void
objrefs_cleanup (void)
{
	g_hash_table_destroy (objrefs);
	objrefs = NULL;
}

static GHashTable *obj_to_objref;

/*
 * Return an ObjRef for OBJ.
 */
static ObjRef*
get_objref (MonoObject *obj)
{
	ObjRef *ref;

	if (obj == NULL)
		return 0;

#ifdef HAVE_SGEN_GC
	NOT_IMPLEMENTED;
#endif

	/* Use a hash table with masked pointers to internalize object references */
	/* FIXME: This can grow indefinitely */
	mono_loader_lock ();

	if (!obj_to_objref)
		obj_to_objref = g_hash_table_new (NULL, NULL);

	ref = g_hash_table_lookup (obj_to_objref, GINT_TO_POINTER (~((gsize)obj)));
	/* ref might refer to a different object with the same addr which was GCd */
	if (ref && mono_gchandle_get_target (ref->handle) == obj) {
		mono_loader_unlock ();
		return ref;
	}

	ref = g_new0 (ObjRef, 1);
	ref->id = InterlockedIncrement (&objref_id);
	ref->handle = mono_gchandle_new_weakref (obj, FALSE);

	g_hash_table_insert (objrefs, GINT_TO_POINTER (ref->id), ref);
	g_hash_table_insert (obj_to_objref, GINT_TO_POINTER (~((gsize)obj)), ref);

	mono_loader_unlock ();

	return ref;
}

static inline int
get_objid (MonoObject *obj)
{
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
		return 0;
	}

	if (!objrefs)
		return ERR_INVALID_OBJECT;

	mono_loader_lock ();

	ref = g_hash_table_lookup (objrefs, GINT_TO_POINTER (objid));

	if (ref) {
		*obj = mono_gchandle_get_target (ref->handle);
		mono_loader_unlock ();
		if (!(*obj))
			return ERR_INVALID_OBJECT;
		return 0;
	} else {
		mono_loader_unlock ();
		return ERR_INVALID_OBJECT;
	}
}

static ErrorCode
get_object (int objid, MonoObject **obj)
{
	int err = get_object_allow_null (objid, obj);

	if (err)
		return err;
	if (!(*obj))
		return ERR_INVALID_OBJECT;
	return 0;
}

static inline int
decode_objid (guint8 *buf, guint8 **endbuf, guint8 *limit)
{
	return decode_id (buf, endbuf, limit);
}

static inline void
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
	GHashTable *val_to_id [ID_NUM];
} AgentDomainInfo;

/* Maps id -> Id */
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

void
mono_debugger_agent_free_domain_info (MonoDomain *domain)
{
	AgentDomainInfo *info = domain_jit_info (domain)->agent_info;
	int i, j;

	if (info) {
		for (i = 0; i < ID_NUM; ++i)
			if (info->val_to_id [i])
				g_hash_table_destroy (info->val_to_id [i]);
		g_free (info);
	}

	domain_jit_info (domain)->agent_info = NULL;

	/* Clear ids referencing structures in the domain */
	for (i = 0; i < ID_NUM; ++i) {
		if (ids [i]) {
			for (j = 0; j < ids [i]->len; ++j) {
				Id *id = g_ptr_array_index (ids [i], j);
				if (id->domain == domain)
					id->domain = NULL;
			}
		}
	}
}

static int
get_id (MonoDomain *domain, IdType type, gpointer val)
{
	Id *id;
	AgentDomainInfo *info;

	if (val == NULL)
		return 0;

	mono_loader_lock ();

	mono_domain_lock (domain);

	if (!domain_jit_info (domain)->agent_info)
		domain_jit_info (domain)->agent_info = g_new0 (AgentDomainInfo, 1);
	info = domain_jit_info (domain)->agent_info;
	if (info->val_to_id [type] == NULL)
		info->val_to_id [type] = g_hash_table_new (mono_aligned_addr_hash, NULL);

	id = g_hash_table_lookup (info->val_to_id [type], val);
	if (id) {
		mono_domain_unlock (domain);
		mono_loader_unlock ();
		return id->id;
	}

	id = g_new0 (Id, 1);
	/* Reserve id 0 */
	id->id = ids [type]->len + 1;
	id->domain = domain;
	id->data.val = val;

	g_hash_table_insert (info->val_to_id [type], val, id);

	mono_domain_unlock (domain);

	g_ptr_array_add (ids [type], id);

	mono_loader_unlock ();

	return id->id;
}

static inline gpointer
decode_ptr_id (guint8 *buf, guint8 **endbuf, guint8 *limit, IdType type, MonoDomain **domain, int *err)
{
	Id *res;

	int id = decode_id (buf, endbuf, limit);

	*err = 0;
	if (domain)
		*domain = NULL;

	if (id == 0)
		return NULL;

	// FIXME: error handling
	mono_loader_lock ();
	g_assert (id > 0 && id <= ids [type]->len);

	res = g_ptr_array_index (ids [type], GPOINTER_TO_INT (id - 1));
	mono_loader_unlock ();

	if (res->domain == NULL) {
		*err = ERR_UNLOADED;
		return NULL;
	}

	if (domain)
		*domain = res->domain;

	return res->data.val;
}

static inline void
buffer_add_ptr_id (Buffer *buf, MonoDomain *domain, IdType type, gpointer val)
{
	buffer_add_id (buf, get_id (domain, type, val));
}

static inline MonoClass*
decode_typeid (guint8 *buf, guint8 **endbuf, guint8 *limit, MonoDomain **domain, int *err)
{
	return decode_ptr_id (buf, endbuf, limit, ID_TYPE, domain, err);
}

static inline MonoAssembly*
decode_assemblyid (guint8 *buf, guint8 **endbuf, guint8 *limit, MonoDomain **domain, int *err)
{
	return decode_ptr_id (buf, endbuf, limit, ID_ASSEMBLY, domain, err);
}

static inline MonoImage*
decode_moduleid (guint8 *buf, guint8 **endbuf, guint8 *limit, MonoDomain **domain, int *err)
{
	return decode_ptr_id (buf, endbuf, limit, ID_MODULE, domain, err);
}

static inline MonoMethod*
decode_methodid (guint8 *buf, guint8 **endbuf, guint8 *limit, MonoDomain **domain, int *err)
{
	return decode_ptr_id (buf, endbuf, limit, ID_METHOD, domain, err);
}

static inline MonoClassField*
decode_fieldid (guint8 *buf, guint8 **endbuf, guint8 *limit, MonoDomain **domain, int *err)
{
	return decode_ptr_id (buf, endbuf, limit, ID_FIELD, domain, err);
}

static inline MonoDomain*
decode_domainid (guint8 *buf, guint8 **endbuf, guint8 *limit, MonoDomain **domain, int *err)
{
	return decode_ptr_id (buf, endbuf, limit, ID_DOMAIN, domain, err);
}

static inline MonoProperty*
decode_propertyid (guint8 *buf, guint8 **endbuf, guint8 *limit, MonoDomain **domain, int *err)
{
	return decode_ptr_id (buf, endbuf, limit, ID_PROPERTY, domain, err);
}

static inline void
buffer_add_typeid (Buffer *buf, MonoDomain *domain, MonoClass *klass)
{
	buffer_add_ptr_id (buf, domain, ID_TYPE, klass);
}

static inline void
buffer_add_methodid (Buffer *buf, MonoDomain *domain, MonoMethod *method)
{
	buffer_add_ptr_id (buf, domain, ID_METHOD, method);
}

static inline void
buffer_add_assemblyid (Buffer *buf, MonoDomain *domain, MonoAssembly *assembly)
{
	buffer_add_ptr_id (buf, domain, ID_ASSEMBLY, assembly);
}

static inline void
buffer_add_moduleid (Buffer *buf, MonoDomain *domain, MonoImage *image)
{
	buffer_add_ptr_id (buf, domain, ID_MODULE, image);
}

static inline void
buffer_add_fieldid (Buffer *buf, MonoDomain *domain, MonoClassField *field)
{
	buffer_add_ptr_id (buf, domain, ID_FIELD, field);
}

static inline void
buffer_add_propertyid (Buffer *buf, MonoDomain *domain, MonoProperty *property)
{
	buffer_add_ptr_id (buf, domain, ID_PROPERTY, property);
}

static inline void
buffer_add_domainid (Buffer *buf, MonoDomain *domain)
{
	buffer_add_ptr_id (buf, domain, ID_DOMAIN, domain);
}

static void invoke_method (void);

/*
 * SUSPEND/RESUME
 */

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

	tls = TlsGetValue (debugger_tls_id);
	g_assert (tls);

	if (ctx) {
		memcpy (&tls->ctx, ctx, sizeof (MonoContext));
	} else {
#ifdef MONO_INIT_CONTEXT_FROM_CURRENT
		MONO_INIT_CONTEXT_FROM_CURRENT (&tls->ctx);
#else
		MONO_INIT_CONTEXT_FROM_FUNC (&tls->ctx, save_thread_context);
#endif
	}

	tls->lmf = mono_get_lmf ();
	tls->domain = mono_domain_get ();
	tls->has_context = TRUE;
}

/* The number of times the runtime is suspended */
static gint32 suspend_count;

/* Number of threads suspended */
/* 
 * If this is equal to the size of thread_to_tls, the runtime is considered
 * suspended.
 */
static gint32 threads_suspend_count;

static mono_mutex_t suspend_mutex;

/* Cond variable used to wait for suspend_count becoming 0 */
static mono_cond_t suspend_cond;

/* Semaphore used to wait for a thread becoming suspended */
static MonoSemType suspend_sem;

static void
suspend_init (void)
{
	mono_mutex_init (&suspend_mutex, NULL);
	mono_cond_init (&suspend_cond, NULL);	
	MONO_SEM_INIT (&suspend_sem, 0);
}

typedef struct
{
	StackFrameInfo last_frame;
	gboolean last_frame_set;
	MonoContext ctx;
	gpointer lmf;
} GetLastFrameUserData;

static gboolean
get_last_frame (StackFrameInfo *info, MonoContext *ctx, gpointer user_data)
{
	GetLastFrameUserData *data = user_data;

	if (info->type == FRAME_TYPE_MANAGED_TO_NATIVE)
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

		return TRUE;
	}
}

/*
 * mono_debugger_agent_thread_interrupt:
 *
 *   Called by the abort signal handler.
 * Should be signal safe.
 */
gboolean
mono_debugger_agent_thread_interrupt (void *sigctx, MonoJitInfo *ji)
{
	DebuggerTlsData *tls;

	if (!inited)
		return FALSE;

	tls = TlsGetValue (debugger_tls_id);
	if (!tls)
		return FALSE;

	/*
	 * OSX can (and will) coalesce signals, so sending multiple pthread_kills does not
	 * guarantee the signal handler will be called that many times.  Instead of tracking
	 * interrupt_count on osx, we use this as a boolean flag to determine if a interrupt
	 * has been requested that hasn't been handled yet, otherwise we can have threads
	 * refuse to die when VM_EXIT is called
	 */
#if defined(__APPLE__)
	if (InterlockedCompareExchange (&tls->interrupt_count, 0, 1) == 0)
		return FALSE;
#else
	/*
	 * We use interrupt_count to determine whenever this interrupt should be processed
	 * by us or the normal interrupt processing code in the signal handler.
	 * There is no race here with notify_thread (), since the signal is sent after
	 * incrementing interrupt_count.
	 */
	if (tls->interrupt_count == 0)
		return FALSE;

	InterlockedDecrement (&tls->interrupt_count);
#endif

	// FIXME: Races when the thread leaves managed code before hitting a single step
	// event.

	if (ji) {
		/* Running managed code, will be suspended by the single step code */
		DEBUG (1, printf ("[%p] Received interrupt while at %s(%p), continuing.\n", (gpointer)GetCurrentThreadId (), ji->method->name, mono_arch_ip_from_context (sigctx)));
		return TRUE;
	} else {
		/* 
		 * Running native code, will be suspended when it returns to/enters 
		 * managed code. Treat it as already suspended.
		 * This might interrupt the code in process_single_step_inner (), we use the
		 * tls->suspending flag to avoid races when that happens.
		 */
		if (!tls->suspended && !tls->suspending) {
			MonoContext ctx;
			GetLastFrameUserData data;

			// FIXME: printf is not signal safe, but this is only used during
			// debugger debugging
			DEBUG (1, printf ("[%p] Received interrupt while at %p, treating as suspended.\n", (gpointer)GetCurrentThreadId (), mono_arch_ip_from_context (sigctx)));
			//save_thread_context (&ctx);

			if (!tls->thread)
				/* Already terminated */
				return TRUE;

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
			data.last_frame_set = FALSE;
			if (sigctx) {
				mono_arch_sigctx_to_monoctx (sigctx, &ctx);
				mono_jit_walk_stack_from_ctx_in_thread (get_last_frame, mono_domain_get (), &ctx, FALSE, tls->thread, mono_get_lmf (), &data);
			}
			if (data.last_frame_set) {
				memcpy (&tls->async_last_frame, &data.last_frame, sizeof (StackFrameInfo));
				memcpy (&tls->async_ctx, &data.ctx, sizeof (MonoContext));
				tls->async_lmf = data.lmf;
				tls->has_async_ctx = TRUE;
				tls->domain = mono_domain_get ();
				memcpy (&tls->ctx, &ctx, sizeof (MonoContext));
			} else {
				tls->has_async_ctx = FALSE;
			}

			mono_memory_barrier ();

			tls->suspended = TRUE;
			MONO_SEM_POST (&suspend_sem);
		}
		return TRUE;
	}
}

#ifdef HOST_WIN32
static void CALLBACK notify_thread_apc (ULONG_PTR param)
{
	//DebugBreak ();
	mono_debugger_agent_thread_interrupt (NULL, NULL);
}
#endif /* HOST_WIN32 */

/*
 * reset_native_thread_suspend_state:
 * 
 *   Reset the suspended flag on native threads
 */
static void
reset_native_thread_suspend_state (gpointer key, gpointer value, gpointer user_data)
{
	DebuggerTlsData *tls = value;

	if (!tls->really_suspended && tls->suspended)
		tls->suspended = FALSE;
}

/*
 * notify_thread:
 *
 *   Notify a thread that it needs to suspend.
 */
static void
notify_thread (gpointer key, gpointer value, gpointer user_data)
{
	MonoInternalThread *thread = key;
	DebuggerTlsData *tls = value;
	gsize tid = thread->tid;

	if (GetCurrentThreadId () != tid) {
		DEBUG(1, fprintf (log_file, "[%p] Interrupting %p...\n", (gpointer)GetCurrentThreadId (), (gpointer)tid));

		/*
		 * OSX can (and will) coalesce signals, so sending multiple pthread_kills does not
		 * guarantee the signal handler will be called that many times.  Instead of tracking
		 * interrupt_count on osx, we use this as a boolean flag to determine if a interrupt
		 * has been requested that hasn't been handled yet, otherwise we can have threads
		 * refuse to die when VM_EXIT is called
		 */
#if defined(__APPLE__)
		if (InterlockedCompareExchange (&tls->interrupt_count, 1, 0) == 1)
			return;
#else
		/*
		 * Maybe we could use the normal interrupt infrastructure, but that does a lot
		 * of things like breaking waits etc. which we don't want.
		 */
		InterlockedIncrement (&tls->interrupt_count);
#endif

		/* This is _not_ equivalent to ves_icall_System_Threading_Thread_Abort () */
#ifdef HOST_WIN32
		QueueUserAPC (notify_thread_apc, thread->handle, NULL);
#else
		pthread_kill ((pthread_t) tid, mono_thread_get_abort_signal ());
#endif
	}
}

static void
process_suspend (DebuggerTlsData *tls, MonoContext *ctx)
{
	guint8 *ip = MONO_CONTEXT_GET_IP (ctx);
	MonoJitInfo *ji;

	if (debugger_thread_id == GetCurrentThreadId ())
		return;

	/* Prevent races with mono_debugger_agent_thread_interrupt () */
	if (suspend_count - tls->resume_count > 0)
		tls->suspending = TRUE;

	DEBUG(1, fprintf (log_file, "[%p] Received single step event for suspending.\n", (gpointer)GetCurrentThreadId ()));

	if (suspend_count - tls->resume_count == 0) {
		/* 
		 * We are executing a single threaded invoke but the single step for 
		 * suspending is still active.
		 * FIXME: This slows down single threaded invokes.
		 */
		DEBUG(1, fprintf (log_file, "[%p] Ignored during single threaded invoke.\n", (gpointer)GetCurrentThreadId ()));
		return;
	}

	ji = mono_jit_info_table_find (mono_domain_get (), (char*)ip);

	/* Can't suspend in these methods */
	if (ji->method->klass == mono_defaults.string_class && (!strcmp (ji->method->name, "memset") || strstr (ji->method->name, "memcpy")))
		return;

	save_thread_context (ctx);

	suspend_current ();
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
	mono_loader_lock ();

	mono_mutex_lock (&suspend_mutex);

	suspend_count ++;

	DEBUG(1, fprintf (log_file, "[%p] Suspending vm...\n", (gpointer)GetCurrentThreadId ()));

	if (suspend_count == 1) {
		// FIXME: Is it safe to call this inside the lock ?
		start_single_stepping ();
		mono_g_hash_table_foreach (thread_to_tls, notify_thread, NULL);
	}

	mono_mutex_unlock (&suspend_mutex);

	mono_loader_unlock ();
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
	int err;

	g_assert (debugger_thread_id == GetCurrentThreadId ());

	mono_loader_lock ();

	mono_mutex_lock (&suspend_mutex);

	g_assert (suspend_count > 0);
	suspend_count --;

	DEBUG(1, fprintf (log_file, "[%p] Resuming vm...\n", (gpointer)GetCurrentThreadId ()));

	if (suspend_count == 0) {
		// FIXME: Is it safe to call this inside the lock ?
		stop_single_stepping ();
		mono_g_hash_table_foreach (thread_to_tls, reset_native_thread_suspend_state, NULL);
	}

	/* Signal this even when suspend_count > 0, since some threads might have resume_count > 0 */
	err = mono_cond_broadcast (&suspend_cond);
	g_assert (err == 0);

	mono_mutex_unlock (&suspend_mutex);
	//g_assert (err == 0);

	mono_loader_unlock ();
}

/*
 * resume_thread:
 *
 *   Resume just one thread.
 */
static void
resume_thread (MonoInternalThread *thread)
{
	int err;
	DebuggerTlsData *tls;

	g_assert (debugger_thread_id == GetCurrentThreadId ());

	mono_loader_lock ();

	tls = mono_g_hash_table_lookup (thread_to_tls, thread);
	g_assert (tls);
	
	mono_mutex_lock (&suspend_mutex);

	g_assert (suspend_count > 0);

	DEBUG(1, fprintf (log_file, "[%p] Resuming thread...\n", (gpointer)(gssize)thread->tid));

	tls->resume_count += suspend_count;

	/* 
	 * Signal suspend_count without decreasing suspend_count, the threads will wake up
	 * but only the one whose resume_count field is > 0 will be resumed.
	 */
	err = mono_cond_broadcast (&suspend_cond);
	g_assert (err == 0);

	mono_mutex_unlock (&suspend_mutex);
	//g_assert (err == 0);

	mono_loader_unlock ();
}

static void
invalidate_frames (DebuggerTlsData *tls)
{
	int i;

	if (!tls)
		tls = TlsGetValue (debugger_tls_id);
	g_assert (tls);

	for (i = 0; i < tls->frame_count; ++i) {
		if (tls->frames [i]->jit)
			mono_debug_free_method_jit_info (tls->frames [i]->jit);
		g_free (tls->frames [i]);
	}
	g_free (tls->frames);
	tls->frame_count = 0;
	tls->frames = NULL;
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
	int err;
	DebuggerTlsData *tls;

	g_assert (debugger_thread_id != GetCurrentThreadId ());

	if (mono_loader_lock_is_owned_by_self ()) {
		/*
		 * If we own the loader mutex, can't suspend until we release it, since the
		 * whole runtime can deadlock otherwise.
		 */
		return;
	}

 	tls = TlsGetValue (debugger_tls_id);
	g_assert (tls);

	mono_mutex_lock (&suspend_mutex);

	tls->suspending = FALSE;
	tls->really_suspended = TRUE;

	if (!tls->suspended) {
		tls->suspended = TRUE;
		MONO_SEM_POST (&suspend_sem);
	}

	DEBUG(1, fprintf (log_file, "[%p] Suspended.\n", (gpointer)GetCurrentThreadId ()));

	while (suspend_count - tls->resume_count > 0) {
#ifdef HOST_WIN32
		if (WAIT_TIMEOUT == WaitForSingleObject(suspend_cond, 0))
		{
			mono_mutex_unlock (&suspend_mutex);
			Sleep(0);
			mono_mutex_lock (&suspend_mutex);
		}
		else
		{
		}
#else
		err = mono_cond_wait (&suspend_cond, &suspend_mutex);
		g_assert (err == 0);
#endif
	}

	tls->suspended = FALSE;
	tls->really_suspended = FALSE;

	threads_suspend_count --;

	mono_mutex_unlock (&suspend_mutex);

	DEBUG(1, fprintf (log_file, "[%p] Resumed.\n", (gpointer)GetCurrentThreadId ()));

	if (tls->invoke) {
		/* Save the original context */
		tls->invoke->has_ctx = TRUE;
		memcpy (&tls->invoke->ctx, &tls->ctx, sizeof (MonoContext));

		invoke_method ();
	}

	/* The frame info becomes invalid after a resume */
	tls->has_context = FALSE;
	tls->has_async_ctx = FALSE;
	invalidate_frames (NULL);
}

static void
count_thread (gpointer key, gpointer value, gpointer user_data)
{
	DebuggerTlsData *tls = value;

	if (!tls->suspended && !tls->terminated)
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
			DEBUG(1, fprintf (log_file, "Waiting for %d(%d) threads to suspend...\n", nwait, nthreads));
			err = MONO_SEM_WAIT (&suspend_sem);
			g_assert (err == 0);
			waited = TRUE;
		} else {
			break;
		}
	}

	if (waited)
		DEBUG(1, fprintf (log_file, "%d threads suspended.\n", nthreads));
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

/*
 * find_seq_point_for_native_offset:
 *
 *   Find the sequence point corresponding to the native offset NATIVE_OFFSET, which
 * should be the location of a sequence point.
 */
static SeqPoint*
find_seq_point_for_native_offset (MonoDomain *domain, MonoMethod *method, gint32 native_offset, MonoSeqPointInfo **info)
{
	MonoSeqPointInfo *seq_points;
	int i;

	mono_domain_lock (domain);
	seq_points = g_hash_table_lookup (domain_jit_info (domain)->seq_points, method);
	mono_domain_unlock (domain);
	g_assert (seq_points);

	*info = seq_points;

	for (i = 0; i < seq_points->len; ++i) {
		if (seq_points->seq_points [i].native_offset == native_offset)
			return &seq_points->seq_points [i];
	}

	return NULL;
}

/*
 * find_seq_point:
 *
 *   Find the sequence point corresponding to the IL offset IL_OFFSET, which
 * should be the location of a sequence point.
 */
static SeqPoint*
find_seq_point (MonoDomain *domain, MonoMethod *method, gint32 il_offset, MonoSeqPointInfo **info)
{
	MonoSeqPointInfo *seq_points;
	int i;

	mono_domain_lock (domain);
	seq_points = g_hash_table_lookup (domain_jit_info (domain)->seq_points, method);
	mono_domain_unlock (domain);
	g_assert (seq_points);

	*info = seq_points;

	for (i = 0; i < seq_points->len; ++i) {
		if (seq_points->seq_points [i].il_offset == il_offset)
			return &seq_points->seq_points [i];
	}

	return NULL;
}

/*
 * compute_il_offset:
 *
 *    Compute the IL offset corresponding to NATIVE_OFFSET, which should be
 * a location of a sequence point.
 * We use this function instead of mono_debug_il_offset_from_address () etc,
 * which doesn't seem to work in a lot of cases.
 */
static gint32
compute_il_offset (MonoDomain *domain, MonoMethod *method, gint32 native_offset)
{
	MonoSeqPointInfo *seq_points;
	int i, last_il_offset, seq_il_offset, seq_native_offset;

	mono_domain_lock (domain);
	seq_points = g_hash_table_lookup (domain_jit_info (domain)->seq_points, method);
	mono_domain_unlock (domain);
	g_assert (seq_points);

	last_il_offset = -1;

	/* Find the sequence point */
	for (i = 0; i < seq_points->len; ++i) {
		seq_il_offset = seq_points->seq_points [i].il_offset;
		seq_native_offset = seq_points->seq_points [i].native_offset;

		if (seq_native_offset > native_offset)
			break;
		last_il_offset = seq_il_offset;
	}

	return last_il_offset;
}

typedef struct {
	DebuggerTlsData *tls;
	GSList *frames;
} ComputeFramesUserData;

static gboolean
process_frame (StackFrameInfo *info, MonoContext *ctx, gpointer user_data)
{
	ComputeFramesUserData *ud = user_data;
	StackFrame *frame;
	MonoMethod *method;

	if (info->type != FRAME_TYPE_MANAGED) {
		if (info->type == FRAME_TYPE_DEBUGGER_INVOKE) {
			/* Mark the last frame as an invoke frame */
			if (ud->frames)
				((StackFrame*)ud->frames->data)->flags |= FRAME_FLAG_DEBUGGER_INVOKE;
		}
		return FALSE;
	}

	if (info->ji)
		method = info->ji->method;
	else
		method = info->method;

	if (!method || (method->wrapper_type && method->wrapper_type != MONO_WRAPPER_DYNAMIC_METHOD))
		return FALSE;

	if (info->il_offset == -1) {
		/* Can't use compute_il_offset () since ip doesn't point precisely at at a seq point */
		info->il_offset = mono_debug_il_offset_from_address (method, info->domain, info->native_offset);
	}

	DEBUG (1, fprintf (stderr, "\tFrame: %s %d %d %d\n", mono_method_full_name (method, TRUE), info->native_offset, info->il_offset, info->managed));

	if (!info->managed && method->wrapper_type != MONO_WRAPPER_DYNAMIC_METHOD) {
		/*
		 * mono_arch_find_jit_info () returns the context stored in the LMF for 
		 * native frames, but it should unwind once. This is why we have duplicate
		 * frames on the stack sometimes.
		 * !managed also seems to be set for dynamic methods.
		 */
		return FALSE;
	}

	frame = g_new0 (StackFrame, 1);
	frame->method = method;
	frame->il_offset = info->il_offset;
	if (ctx) {
		frame->ctx = *ctx;
		frame->has_ctx = TRUE;
	}
	frame->domain = info->domain;

	ud->frames = g_slist_append (ud->frames, frame);

	return FALSE;
}

static void
compute_frame_info (MonoInternalThread *thread, DebuggerTlsData *tls)
{
	ComputeFramesUserData user_data;
	GSList *tmp;
	int i, findex, new_frame_count;
	StackFrame **new_frames, *f;

	// FIXME: Locking on tls
	if (tls->frames && tls->frames_up_to_date)
		return;

	DEBUG(1, fprintf (log_file, "Frames for %p(tid=%lx):\n", thread, (glong)thread->tid));

	user_data.tls = tls;
	user_data.frames = NULL;
	if (tls->terminated) {
		tls->frame_count = 0;
		return;
	} if (!tls->really_suspended && tls->has_async_ctx) {
		/* Have to use the state saved by the signal handler */
		process_frame (&tls->async_last_frame, NULL, &user_data);
		mono_jit_walk_stack_from_ctx_in_thread (process_frame, tls->domain, &tls->async_ctx, FALSE, thread, tls->async_lmf, &user_data);
	} else if (tls->has_context) {
		mono_jit_walk_stack_from_ctx_in_thread (process_frame, tls->domain, &tls->ctx, FALSE, thread, tls->lmf, &user_data);
	} else {
		// FIXME:
		tls->frame_count = 0;
		return;
	}

	new_frame_count = g_slist_length (user_data.frames);
	new_frames = g_new0 (StackFrame*, new_frame_count);
	findex = 0;
	for (tmp = user_data.frames; tmp; tmp = tmp->next) {
		f = tmp->data;

		/* 
		 * Reuse the id for already existing stack frames, so invokes don't invalidate
		 * the still valid stack frames.
		 */
		for (i = 0; i < tls->frame_count; ++i) {
			if (MONO_CONTEXT_GET_SP (&tls->frames [i]->ctx) == MONO_CONTEXT_GET_SP (&f->ctx)) {
				f->id = tls->frames [i]->id;
				break;
			}
		}

		if (i >= tls->frame_count)
			f->id = InterlockedIncrement (&frame_id);

		new_frames [findex ++] = f;
	}

	g_slist_free (user_data.frames);

	invalidate_frames (tls);

	tls->frames = new_frames;
	tls->frame_count = new_frame_count;
	tls->frames_up_to_date = TRUE;
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
create_event_list (EventKind event, GPtrArray *reqs, MonoJitInfo *ji, MonoException *exc, int *suspend_policy)
{
	int i, j;
	GSList *events = NULL;

	*suspend_policy = SUSPEND_POLICY_NONE;

	if (!reqs)
		reqs = event_requests;

	if (!reqs)
		return NULL;

	for (i = 0; i < reqs->len; ++i) {
		EventRequest *req = g_ptr_array_index (reqs, i);
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
				} else if (mod->kind == MOD_KIND_EXCEPTION_ONLY && exc) {
					if (mod->data.exc_class && !mono_class_is_assignable_from (mod->data.exc_class, exc->object.vtable->klass))
						filtered = TRUE;
				} else if (mod->kind == MOD_KIND_ASSEMBLY_ONLY && ji) {
					int k;
					gboolean found = FALSE;
					MonoAssembly **assemblies = mod->data.assemblies;

					if (assemblies) {
						for (k = 0; assemblies [k]; ++k)
							if (assemblies [k] == ji->method->klass->image->assembly)
								found = TRUE;
					}
					if (!found)
						filtered = TRUE;
				}
			}

			if (!filtered) {
				*suspend_policy = MAX (*suspend_policy, req->suspend_policy);
				events = g_slist_append (events, GINT_TO_POINTER (req->id));
			}
		}
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
	default:
		g_assert_not_reached ();
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
	MonoThread *thread;

	if (!inited)
		return;

	if (!vm_start_event_sent && event != EVENT_KIND_VM_START)
		// FIXME: We miss those events
		return;

	if (vm_death_event_sent)
		return;

	if (mono_runtime_is_shutting_down () && event != EVENT_KIND_VM_DEATH)
		return;

	if (disconnected)
		return;

	if (events == NULL)
		return;

	if (debugger_thread_id == GetCurrentThreadId () && event != EVENT_KIND_VM_DEATH)
		// FIXME: Send these with a NULL thread, don't suspend the current thread
		return;

	buffer_init (&buf, 128);
	buffer_add_byte (&buf, suspend_policy);
	buffer_add_int (&buf, g_slist_length (events)); // n of events

	for (l = events; l; l = l->next) {
		buffer_add_byte (&buf, event); // event kind
		buffer_add_int (&buf, GPOINTER_TO_INT (l->data)); // request id

		thread = mono_thread_current ();

		if (event == EVENT_KIND_VM_START)
			thread = arg;
		else if (event == EVENT_KIND_THREAD_START)
			g_assert (mono_thread_internal_current () == arg);

		buffer_add_objid (&buf, (MonoObject*)thread); // thread

		switch (event) {
		case EVENT_KIND_THREAD_START:
		case EVENT_KIND_THREAD_DEATH:
			break;
		case EVENT_KIND_APPDOMAIN_CREATE:
		case EVENT_KIND_APPDOMAIN_UNLOAD:
			buffer_add_domainid (&buf, arg);
			break;
		case EVENT_KIND_METHOD_ENTRY:
		case EVENT_KIND_METHOD_EXIT:
			buffer_add_methodid (&buf, domain, arg);
			break;
		case EVENT_KIND_ASSEMBLY_LOAD:
		case EVENT_KIND_ASSEMBLY_UNLOAD:
			buffer_add_assemblyid (&buf, domain, arg);
			break;
		case EVENT_KIND_TYPE_LOAD:
			buffer_add_typeid (&buf, domain, arg);
			break;
		case EVENT_KIND_BREAKPOINT:
		case EVENT_KIND_STEP:
			buffer_add_methodid (&buf, domain, arg);
			buffer_add_long (&buf, il_offset);
			break;
		case EVENT_KIND_VM_START:
			buffer_add_domainid (&buf, mono_get_root_domain ());
			break;
		case EVENT_KIND_VM_DEATH:
			break;
		case EVENT_KIND_EXCEPTION:
			buffer_add_objid (&buf, (MonoObject*)arg);
			break;
		default:
			g_assert_not_reached ();
		}
	}

	if (event == EVENT_KIND_VM_START) {
		suspend_policy = agent_config.suspend ? SUSPEND_POLICY_ALL : SUSPEND_POLICY_NONE;
		start_debugger_thread ();
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
		suspend_vm ();
	}

	send_packet (CMD_SET_EVENT, CMD_COMPOSITE, &buf);

	g_slist_free (events);
	events = NULL;

	if (event == EVENT_KIND_VM_START)
		vm_start_event_sent = TRUE;

	DEBUG (1, fprintf (log_file, "[%p] Sent event %s, suspend=%d.\n", (gpointer)GetCurrentThreadId (), event_to_string (event), suspend_policy));

	buffer_free (&buf);

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

	mono_loader_lock ();
	events = create_event_list (event, NULL, NULL, NULL, &suspend_policy);
	mono_loader_unlock ();

	process_event (event, arg, 0, NULL, events, suspend_policy);
}

static void
runtime_initialized (MonoProfiler *prof)
{
	process_profiler_event (EVENT_KIND_VM_START, mono_thread_current ());
}	

static void
runtime_shutdown (MonoProfiler *prof)
{
	process_profiler_event (EVENT_KIND_VM_DEATH, mono_thread_current ());

	mono_debugger_agent_cleanup ();
}

static void
thread_startup (MonoProfiler *prof, gsize tid)
{
	MonoInternalThread *thread = mono_thread_internal_current ();
	MonoInternalThread *old_thread;
	DebuggerTlsData *tls;

	if (tid == debugger_thread_id)
		return;

	g_assert (thread->tid == tid);

	mono_loader_lock ();
	old_thread = mono_g_hash_table_lookup (tid_to_thread, (gpointer)tid);
	mono_loader_unlock ();
	if (old_thread) {
		if (thread == old_thread) {
			/* 
			 * For some reason, thread_startup () might be called for the same thread
			 * multiple times (attach ?).
			 */
			DEBUG (1, fprintf (log_file, "[%p] thread_start () called multiple times for %p, ignored.\n", (gpointer)tid, (gpointer)tid));
			return;
		} else {
			/*
			 * thread_end () might not be called for some threads, and the tid could
			 * get reused.
			 */
			DEBUG (1, fprintf (log_file, "[%p] Removing stale data for tid %p.\n", (gpointer)tid, (gpointer)tid));
			mono_loader_lock ();
			mono_g_hash_table_remove (thread_to_tls, old_thread);
			mono_g_hash_table_remove (tid_to_thread, (gpointer)tid);
			mono_g_hash_table_remove (tid_to_thread_obj, (gpointer)tid);
			mono_loader_unlock ();
		}
	}

	tls = TlsGetValue (debugger_tls_id);
	g_assert (!tls);
	// FIXME: Free this somewhere
	tls = g_new0 (DebuggerTlsData, 1);
	tls->resume_event = CreateEvent (NULL, FALSE, FALSE, NULL);
	MONO_GC_REGISTER_ROOT (tls->thread);
	tls->thread = thread;
	TlsSetValue (debugger_tls_id, tls);

	DEBUG (1, fprintf (log_file, "[%p] Thread started, obj=%p, tls=%p.\n", (gpointer)tid, thread, tls));

	mono_loader_lock ();
	mono_g_hash_table_insert (thread_to_tls, thread, tls);
	mono_g_hash_table_insert (tid_to_thread, (gpointer)tid, thread);
	mono_g_hash_table_insert (tid_to_thread_obj, (gpointer)tid, mono_thread_current ());
	mono_loader_unlock ();

	process_profiler_event (EVENT_KIND_THREAD_START, thread);

	/* 
	 * suspend_vm () could have missed this thread, so wait for a resume.
	 */
	suspend_current ();
}

static void
thread_end (MonoProfiler *prof, gsize tid)
{
	MonoInternalThread *thread;
	DebuggerTlsData *tls = NULL;

	mono_loader_lock ();
	thread = mono_g_hash_table_lookup (tid_to_thread, (gpointer)tid);
	if (thread) {
		tls = mono_g_hash_table_lookup (thread_to_tls, thread);
		/* FIXME: Maybe we need to free this instead, but some code can't handle that */
		tls->terminated = TRUE;
		mono_g_hash_table_remove (tid_to_thread_obj, (gpointer)tid);
		/* Can't remove from tid_to_thread, as that would defeat the check in thread_start () */
		MONO_GC_UNREGISTER_ROOT (tls->thread);
		tls->thread = NULL;
	}
	mono_loader_unlock ();

	/* We might be called for threads started before we registered the start callback */
	if (thread) {
		DEBUG (1, fprintf (log_file, "[%p] Thread terminated, obj=%p, tls=%p.\n", (gpointer)tid, thread, tls));
		process_profiler_event (EVENT_KIND_THREAD_DEATH, thread);
	}
}

static void
appdomain_load (MonoProfiler *prof, MonoDomain *domain, int result)
{
	process_profiler_event (EVENT_KIND_APPDOMAIN_CREATE, domain);
}

static void
appdomain_unload (MonoProfiler *prof, MonoDomain *domain)
{
	process_profiler_event (EVENT_KIND_APPDOMAIN_UNLOAD, domain);
}

static void
assembly_load (MonoProfiler *prof, MonoAssembly *assembly, int result)
{
	/* Sent later in jit_end () */
	mono_loader_lock ();
	g_ptr_array_add (pending_assembly_loads, assembly);
	mono_loader_unlock ();
}

static void
assembly_unload (MonoProfiler *prof, MonoAssembly *assembly)
{
	process_profiler_event (EVENT_KIND_ASSEMBLY_UNLOAD, assembly);
}

static void
start_runtime_invoke (MonoProfiler *prof, MonoMethod *method)
{
#if defined(HOST_WIN32) && !defined(__GNUC__)
	gpointer stackptr = ((guint64)_AddressOfReturnAddress () - sizeof (void*));
#else
	gpointer stackptr = __builtin_frame_address (1);
#endif
	MonoInternalThread *thread = mono_thread_internal_current ();
	DebuggerTlsData *tls;

	mono_loader_lock ();
	
	tls = mono_g_hash_table_lookup (thread_to_tls, thread);
	/* Could be the debugger thread with assembly/type load hooks */
	if (tls)
		tls->invoke_addr = stackptr;

	mono_loader_unlock ();
}

static void
end_runtime_invoke (MonoProfiler *prof, MonoMethod *method)
{
	int i;
#if defined(HOST_WIN32) && !defined(__GNUC__)
	gpointer stackptr = ((guint64)_AddressOfReturnAddress () - sizeof (void*));
#else
	gpointer stackptr = __builtin_frame_address (1);
#endif

	if (ss_req == NULL || stackptr != ss_invoke_addr || ss_req->thread != mono_thread_internal_current ())
		return;

	/*
	 * We need to stop single stepping when exiting a runtime invoke, since if it is
	 * a step out, it may return to native code, and thus never end.
	 */
	mono_loader_lock ();
	ss_invoke_addr = NULL;

	for (i = 0; i < event_requests->len; ++i) {
		EventRequest *req = g_ptr_array_index (event_requests, i);

		if (req->event_kind == EVENT_KIND_STEP) {
			ss_destroy (req->info);
			g_ptr_array_remove_index_fast (event_requests, i);
			g_free (req);
			break;
		}
	}
	mono_loader_unlock ();
}

static void
jit_end (MonoProfiler *prof, MonoMethod *method, MonoJitInfo *jinfo, int result)
{
	/*
	 * We emit type load events when the first method of the type is JITted,
	 * since the class load profiler callbacks might be called with the
	 * loader lock held. They could also occur in the debugger thread.
	 * Same for assembly load events.
	 */
	gboolean type_load = FALSE;

	while (TRUE) {
		MonoAssembly *assembly = NULL;

		// FIXME: Maybe store this in TLS so the thread of the event is correct ?
		mono_loader_lock ();
		if (pending_assembly_loads->len > 0) {
			assembly = g_ptr_array_index (pending_assembly_loads, 0);
			g_ptr_array_remove_index (pending_assembly_loads, 0);
		}
		mono_loader_unlock ();

		if (assembly)
			process_profiler_event (EVENT_KIND_ASSEMBLY_LOAD, assembly);
		else
			break;
	}

	mono_loader_lock ();
	if (!g_hash_table_lookup (loaded_classes, method->klass)) {
		type_load = TRUE;
		g_hash_table_insert (loaded_classes, method->klass, method->klass);
	}
	mono_loader_unlock ();
	if (type_load)
		process_profiler_event (EVENT_KIND_TYPE_LOAD, method->klass);

	if (!result)
		add_pending_breakpoints (method, jinfo);
}

/*
 * BREAKPOINTS/SINGLE STEPPING
 */

/* 
 * Contains information about an inserted breakpoint.
 */
typedef struct {
	long il_offset, native_offset;
	guint8 *ip;
	gboolean pending, entry;
	MonoJitInfo *ji;
} BreakpointInstance;

/*
 * Contains generic information about a breakpoint.
 */
typedef struct {
	/* 
	 * The method where the breakpoint is placed. Can be NULL in which case it 
	 * is inserted into every method. This is used to implement method entry/
	 * exit events. Can be a generic method definition, in which case the
	 * breakpoint is inserted into every instance.
	 */
	MonoMethod *method;
	long il_offset;
	gboolean pending, entry;
	EventRequest *req;
	/* 
	 * A list of BreakpointInstance structures describing where the breakpoint
	 * was inserted. There could be more than one because of 
	 * generics/appdomains/method entry/exit.
	 */
	GPtrArray *children;
} MonoBreakpoint;

/* List of breakpoints */
static GPtrArray *breakpoints;
/* Maps breakpoint locations to the number of breakpoints at that location */
static GHashTable *bp_locs;

static void
breakpoints_init (void)
{
	breakpoints = g_ptr_array_new ();
	bp_locs = g_hash_table_new (NULL, NULL);
}	

static void
breakpoints_cleanup (void)
{
	int i;

	mono_loader_lock ();

	for (i = 0; i < breakpoints->len; ++i)
		g_free (g_ptr_array_index (breakpoints, i));

	g_ptr_array_free (breakpoints, TRUE);
	g_hash_table_destroy (bp_locs);

	mono_loader_unlock ();
}

/*
 * insert_breakpoint:
 *
 *   Insert the breakpoint described by BP into the method described by
 * JI.
 */
static void
insert_breakpoint (MonoSeqPointInfo *seq_points, MonoJitInfo *ji, MonoBreakpoint *bp)
{
	int i, count;
	gint32 il_offset, native_offset;
	BreakpointInstance *inst;

	native_offset = 0;
	for (i = 0; i < seq_points->len; ++i) {
		il_offset = seq_points->seq_points [i].il_offset;
		native_offset = seq_points->seq_points [i].native_offset;

		if (il_offset == bp->il_offset)
			break;
	}

	if (i == seq_points->len)
		/* Have to handle this somehow */
		NOT_IMPLEMENTED;

	inst = g_new0 (BreakpointInstance, 1);
	inst->native_offset = native_offset;
	inst->ip = (guint8*)ji->code_start + native_offset;
	inst->ji = ji;

	mono_loader_lock ();

	g_ptr_array_add (bp->children, inst);

	count = GPOINTER_TO_INT (g_hash_table_lookup (bp_locs, inst->ip));
	g_hash_table_insert (bp_locs, inst->ip, GINT_TO_POINTER (count + 1));
	mono_loader_unlock ();

	if (count == 0) {
#ifdef MONO_ARCH_SOFT_DEBUG_SUPPORTED
		mono_arch_set_breakpoint (ji, inst->ip);
#else
		NOT_IMPLEMENTED;
#endif
	}
}

static void
remove_breakpoint (BreakpointInstance *inst)
{
#ifdef MONO_ARCH_SOFT_DEBUG_SUPPORTED
	int count;
	MonoJitInfo *ji = inst->ji;
	guint8 *ip = inst->ip;

	mono_loader_lock ();
	count = GPOINTER_TO_INT (g_hash_table_lookup (bp_locs, ip));
	g_hash_table_insert (bp_locs, ip, GINT_TO_POINTER (count - 1));
	mono_loader_unlock ();

	g_assert (count > 0);

	if (count == 1) {
		mono_arch_clear_breakpoint (ji, ip);
	}
#else
	NOT_IMPLEMENTED;
#endif
}	

/*
 * add_pending_breakpoints:
 *
 *   Insert pending breakpoints into the newly JITted method METHOD.
 */
static void
add_pending_breakpoints (MonoMethod *method, MonoJitInfo *ji)
{
	int i;
	MonoSeqPointInfo *seq_points;
	MonoDomain *domain;

	if (!breakpoints)
		return;

	domain = mono_domain_get ();

	mono_loader_lock ();

	for (i = 0; i < breakpoints->len; ++i) {
		MonoBreakpoint *bp = g_ptr_array_index (breakpoints, i);

		if (bp->pending && (bp->method == method || !bp->method || (method->is_inflated && ((MonoMethodInflated*)method)->declaring == bp->method))) {
			mono_domain_lock (domain);
			seq_points = g_hash_table_lookup (domain_jit_info (domain)->seq_points, ji->method);
			mono_domain_unlock (domain);
			if (!seq_points)
				/* Could be AOT code */
				continue;
			g_assert (seq_points);

			insert_breakpoint (seq_points, ji, bp);
		}
	}

	mono_loader_unlock ();
}

static void
set_bp_in_method (MonoDomain *domain, MonoMethod *method, MonoSeqPointInfo *seq_points, MonoBreakpoint *bp)
{
	gpointer code;
	MonoJitInfo *ji;

	code = mono_jit_find_compiled_method_with_jit_info (domain, method, &ji);
	if (!code) {
		/* Might be AOTed code */
		code = mono_aot_get_method (domain, method);
		g_assert (code);
		ji = mono_jit_info_table_find (domain, code);
		g_assert (ji);
	}
	g_assert (code);

	insert_breakpoint (seq_points, ji, bp);
}

static void
set_bp_in_method_cb (gpointer key, gpointer value, gpointer user_data)
{
	MonoMethod *method = key;
	MonoSeqPointInfo *seq_points = value;
	MonoBreakpoint *bp = user_data;
	MonoDomain *domain = mono_domain_get ();

	if (bp->method) {
		if (method->is_inflated && ((MonoMethodInflated*)method)->declaring == bp->method) {
			/* Generic instance */
			set_bp_in_method (domain, method, seq_points, bp);
		}
	} else {
		/* Method entry/exit */
		set_bp_in_method (domain, method, seq_points, bp);
	}
}

/*
 * set_breakpoint:
 *
 *   Set a breakpoint at IL_OFFSET in METHOD.
 * METHOD can be NULL, in which case a breakpoint is placed in all methods.
 * METHOD can also be a generic method definition, in which case a breakpoint
 * is placed in all instances of the method.
 */
static MonoBreakpoint*
set_breakpoint (MonoMethod *method, long il_offset, EventRequest *req)
{
	MonoSeqPointInfo *seq_points;
	MonoDomain *domain;
	MonoBreakpoint *bp;

	// FIXME: 
	// - suspend/resume the vm to prevent code patching problems
	// - appdomains
	// - multiple breakpoints on the same location
	// - dynamic methods
	// - races

	bp = g_new0 (MonoBreakpoint, 1);
	bp->method = method;
	bp->il_offset = il_offset;
	bp->req = req;
	bp->children = g_ptr_array_new ();

	DEBUG(1, fprintf (log_file, "[dbg] Setting %sbreakpoint at %s:0x%x.\n", (req->event_kind == EVENT_KIND_STEP) ? "single step " : "", method ? mono_method_full_name (method, TRUE) : "<all>", (int)il_offset));

	domain = mono_domain_get ();
	mono_domain_lock (domain);
	if (method) {
		seq_points = g_hash_table_lookup (domain_jit_info (domain)->seq_points, method);
		if (seq_points) {
			set_bp_in_method (domain, method, seq_points, bp);
		} else {
			if (method->is_generic)
				/* There might be already JITted instances */
				g_hash_table_foreach (domain_jit_info (domain)->seq_points, set_bp_in_method_cb, bp);

			/* Not yet JITted */
			bp->pending = TRUE;
		}
	} else {
		g_hash_table_foreach (domain_jit_info (domain)->seq_points, set_bp_in_method_cb, bp);
		bp->pending = TRUE;
	}
	mono_domain_unlock (domain);

	mono_loader_lock ();
	g_ptr_array_add (breakpoints, bp);
	mono_loader_unlock ();

	return bp;
}

static void
clear_breakpoint (MonoBreakpoint *bp)
{
	int i;

	// FIXME: locking, races
	for (i = 0; i < bp->children->len; ++i) {
		BreakpointInstance *inst = g_ptr_array_index (bp->children, i);

		remove_breakpoint (inst);

		g_free (inst);
	}

	mono_loader_lock ();
	g_ptr_array_remove (breakpoints, bp);
	mono_loader_unlock ();

	g_ptr_array_free (bp->children, TRUE);
	g_free (bp);
}

static void
process_breakpoint_inner (DebuggerTlsData *tls, MonoContext *ctx)
{
	MonoJitInfo *ji;
	guint8 *orig_ip, *ip;
	int i, j, suspend_policy;
	guint32 native_offset;
	MonoBreakpoint *bp;
	BreakpointInstance *inst;
	GPtrArray *bp_reqs, *ss_reqs_orig, *ss_reqs;
	GSList *bp_events = NULL, *ss_events = NULL, *enter_leave_events = NULL;
	EventKind kind = EVENT_KIND_BREAKPOINT;

	// FIXME: Speed this up

	orig_ip = ip = MONO_CONTEXT_GET_IP (ctx);
	ji = mono_jit_info_table_find (mono_domain_get (), (char*)ip);
	g_assert (ji);
	g_assert (ji->method);

	/* Compute the native offset of the breakpoint from the ip */
#ifdef MONO_ARCH_SOFT_DEBUG_SUPPORTED
	ip = mono_arch_get_ip_for_breakpoint (ji, ctx);
	native_offset = ip - (guint8*)ji->code_start;	
#else
	NOT_IMPLEMENTED;
#endif

	/* 
	 * Skip the instruction causing the breakpoint signal.
	 */
	mono_arch_skip_breakpoint (ctx);

	if (ji->method->wrapper_type || tls->disable_breakpoints)
		return;

	bp_reqs = g_ptr_array_new ();
	ss_reqs = g_ptr_array_new ();
	ss_reqs_orig = g_ptr_array_new ();

	DEBUG(1, fprintf (log_file, "[%p] Breakpoint hit, method=%s, offset=0x%x.\n", (gpointer)GetCurrentThreadId (), ji->method->name, native_offset));

	mono_loader_lock ();

	bp = NULL;
	for (i = 0; i < breakpoints->len; ++i) {
		bp = g_ptr_array_index (breakpoints, i);

		if (!bp->method)
			continue;

		for (j = 0; j < bp->children->len; ++j) {
			inst = g_ptr_array_index (bp->children, j);
			if (inst->ji == ji && inst->native_offset == native_offset) {
				if (bp->req->event_kind == EVENT_KIND_STEP) {
					g_ptr_array_add (ss_reqs_orig, bp->req);
				} else {
					g_ptr_array_add (bp_reqs, bp->req);
				}
			}
		}
	}
	if (bp_reqs->len == 0 && ss_reqs_orig->len == 0) {
		MonoSeqPointInfo *seq_points;
		int seq_il_offset, seq_native_offset;
		MonoDomain *domain = mono_domain_get ();

		/* Maybe a method entry/exit event */
		mono_domain_lock (domain);
		seq_points = g_hash_table_lookup (domain_jit_info (domain)->seq_points, ji->method);
		mono_domain_unlock (domain);
		if (!seq_points) {
			// FIXME: Generic sharing */
			mono_loader_unlock ();
			return;
		}
		g_assert (seq_points);

		for (i = 0; i < seq_points->len; ++i) {
			seq_il_offset = seq_points->seq_points [i].il_offset;
			seq_native_offset = seq_points->seq_points [i].native_offset;

			if (native_offset == seq_native_offset) {
				if (seq_il_offset == METHOD_ENTRY_IL_OFFSET)
					kind = EVENT_KIND_METHOD_ENTRY;
				else if (seq_il_offset == METHOD_EXIT_IL_OFFSET)
					kind = EVENT_KIND_METHOD_EXIT;
				break;
			}
		}
	}

	/* Process single step requests */
	for (i = 0; i < ss_reqs_orig->len; ++i) {
		EventRequest *req = g_ptr_array_index (ss_reqs_orig, i);
		SingleStepReq *ss_req = bp->req->info;
		gboolean hit = TRUE;
		MonoSeqPointInfo *info;
		SeqPoint *sp;

		sp = find_seq_point_for_native_offset (mono_domain_get (), ji->method, native_offset, &info);
		g_assert (sp);

		if (ss_req->size == STEP_SIZE_LINE) {
			/* Have to check whenever a different source line was reached */
			MonoDebugMethodInfo *minfo;
			MonoDebugSourceLocation *loc = NULL;

			minfo = mono_debug_lookup_method (ji->method);

			if (minfo)
				loc = mono_debug_symfile_lookup_location (minfo, sp->il_offset);

			if (!loc || (loc && ji->method == ss_req->last_method && loc->row == ss_req->last_line))
				/* Have to continue single stepping */
				hit = FALSE;
				
			if (loc) {
				ss_req->last_method = ji->method;
				ss_req->last_line = loc->row;
				mono_debug_free_source_location (loc);
			}
		}

		if (hit)
			g_ptr_array_add (ss_reqs, req);

		/* Start single stepping again from the current sequence point */
		ss_start (ss_req, ji->method, sp, info, ctx, NULL);
	}
	
	if (ss_reqs->len > 0)
		ss_events = create_event_list (EVENT_KIND_STEP, ss_reqs, ji, NULL, &suspend_policy);
	if (bp_reqs->len > 0)
		bp_events = create_event_list (EVENT_KIND_BREAKPOINT, bp_reqs, ji, NULL, &suspend_policy);
	if (kind != EVENT_KIND_BREAKPOINT)
		enter_leave_events = create_event_list (kind, NULL, ji, NULL, &suspend_policy);

	mono_loader_unlock ();

	g_ptr_array_free (bp_reqs, TRUE);
	g_ptr_array_free (ss_reqs, TRUE);

	/* 
	 * FIXME: The first event will suspend, so the second will only be sent after the
	 * resume.
	 */
	if (ss_events)
		process_event (EVENT_KIND_STEP, ji->method, 0, ctx, ss_events, suspend_policy);
	if (bp_events)
		process_event (kind, ji->method, 0, ctx, bp_events, suspend_policy);
	if (enter_leave_events)
		process_event (kind, ji->method, 0, ctx, enter_leave_events, suspend_policy);
}

static void
process_breakpoint (void)
{
	DebuggerTlsData *tls;
	MonoContext ctx;
	static void (*restore_context) (void *);

	if (!restore_context)
		restore_context = mono_get_restore_context ();

	tls = TlsGetValue (debugger_tls_id);
	memcpy (&ctx, &tls->handler_ctx, sizeof (MonoContext));

	process_breakpoint_inner (tls, &ctx);

	/* This is called when resuming from a signal handler, so it shouldn't return */
	restore_context (&ctx);
	g_assert_not_reached ();
}

static void
resume_from_signal_handler (void *sigctx, void *func)
{
	DebuggerTlsData *tls;
	MonoContext ctx;

	/* Save the original context in TLS */
	// FIXME: This might not work on an altstack ?
	tls = TlsGetValue (debugger_tls_id);
	g_assert (tls);

	// FIXME: MonoContext usually doesn't include the fp registers, so these are 
	// clobbered by a single step/breakpoint event. If this turns out to be a problem,
	// clob:c could be added to op_seq_point.

	mono_arch_sigctx_to_monoctx (sigctx, &ctx);
	memcpy (&tls->handler_ctx, &ctx, sizeof (MonoContext));
	MONO_CONTEXT_SET_IP (&ctx, func);
	mono_arch_monoctx_to_sigctx (&ctx, sigctx);

#ifdef PPC_USES_FUNCTION_DESCRIPTOR
	mono_ppc_set_func_into_sigctx (sigctx, func);
#endif
}

void
mono_debugger_agent_breakpoint_hit (void *sigctx)
{
	/*
	 * We are called from a signal handler, and running code there causes all kinds of
	 * problems, like the original signal is disabled, libgc can't handle altstack, etc.
	 * So set up the signal context to return to the real breakpoint handler function.
	 */

	resume_from_signal_handler (sigctx, process_breakpoint);
}

static void
process_single_step_inner (DebuggerTlsData *tls, MonoContext *ctx)
{
	MonoJitInfo *ji;
	guint8 *ip;
	GPtrArray *reqs;
	int il_offset, suspend_policy;
	MonoDomain *domain = mono_domain_get ();
	GSList *events;

	// FIXME: Speed this up

	ip = MONO_CONTEXT_GET_IP (ctx);

	/* Skip the instruction causing the single step */
	mono_arch_skip_single_step (ctx);

	if (suspend_count > 0) {
		process_suspend (tls, ctx);
		return;
	}

	if (!ss_req)
		// FIXME: A suspend race
		return;

	if (mono_thread_internal_current () != ss_req->thread)
		return;

	if (log_level > 0) {
		const char *depth = NULL;

		ji = mono_jit_info_table_find (mono_domain_get (), (char*)ip);

		switch (ss_req->depth) {
		case STEP_DEPTH_OVER:
			depth = "over";
			break;
		case STEP_DEPTH_OUT:
			depth = "out";
			break;
		case STEP_DEPTH_INTO:
			depth = "into";
			break;
		default:
			g_assert_not_reached ();
		}
			
		DEBUG (1, fprintf (log_file, "[%p] Single step event (depth=%s) at %s (%p), sp %p, last sp %p\n", (gpointer)GetCurrentThreadId (), ss_req->depth == STEP_DEPTH_OVER ? "over" : "out", mono_method_full_name (ji->method, TRUE), MONO_CONTEXT_GET_IP (ctx), MONO_CONTEXT_GET_SP (ctx), ss_req->last_sp));
	}

	/*
	 * We implement step over/out by single stepping until we reach the same 
	 * frame/parent frame.
	 * FIXME:
	 * - this is slow
	 * - stack growing upward
	 * - localloc
	 * - exceptions
	 */
	if (ss_req->depth != STEP_DEPTH_INTO) {
		if (ss_req->depth == STEP_DEPTH_OVER && MONO_CONTEXT_GET_SP (ctx) < ss_req->last_sp)
			return;
		if (ss_req->depth == STEP_DEPTH_OUT && MONO_CONTEXT_GET_SP (ctx) <= ss_req->last_sp)
			return;

		ss_req->last_sp = MONO_CONTEXT_GET_SP (ctx);
	}

	ji = mono_jit_info_table_find (mono_domain_get (), (char*)ip);
	g_assert (ji);
	g_assert (ji->method);

	if (ji->method->wrapper_type && ji->method->wrapper_type != MONO_WRAPPER_DYNAMIC_METHOD)
		return;

	/* 
	 * FIXME: 
	 * Stopping in memset makes half-initialized vtypes visible.
	 * Stopping in memcpy makes half-copied vtypes visible.
	 */
	if (ji->method->klass == mono_defaults.string_class && (!strcmp (ji->method->name, "memset") || strstr (ji->method->name, "memcpy")))
		return;

	/* 
	 * The ip points to the instruction causing the single step event, convert it
	 * to the offset stored in seq_points.
	 */
#ifdef MONO_ARCH_SOFT_DEBUG_SUPPORTED
	ip = mono_arch_get_ip_for_single_step (ji, ctx);
#else
	g_assert_not_reached ();
#endif

	/* 
	 * mono_debug_lookup_source_location () doesn't work for IL offset 0 for 
	 * example, so do things by hand.
	 */
	il_offset = compute_il_offset (domain, ji->method, (guint8*)ip - (guint8*)ji->code_start);

	if (il_offset == -1)
		return;

	if (ss_req->size == STEP_SIZE_LINE) {
		/* Step until a different source line is reached */
		MonoDebugMethodInfo *minfo;

		minfo = mono_debug_lookup_method (ji->method);

		if (minfo) {
			MonoDebugSourceLocation *loc = mono_debug_symfile_lookup_location (minfo, il_offset);

			if (loc && ji->method == ss_req->last_method && loc->row == ss_req->last_line) {
				mono_debug_free_source_location (loc);
				return;
			}
			if (!loc)
				/*
				 * Step until we reach a location with line number info, 
				 * otherwise the client can't show a location.
				 * This can happen for example with statics initialized inline
				 * outside of a cctor.
				 */
				return;

			if (loc) {
				ss_req->last_method = ji->method;
				ss_req->last_line = loc->row;
				mono_debug_free_source_location (loc);
			}
		}
	}

	// FIXME: Has to lock earlier

	reqs = g_ptr_array_new ();

	mono_loader_lock ();

	g_ptr_array_add (reqs, ss_req->req);

	events = create_event_list (EVENT_KIND_STEP, reqs, ji, NULL, &suspend_policy);

	g_ptr_array_free (reqs, TRUE);

	mono_loader_unlock ();

	process_event (EVENT_KIND_STEP, ji->method, il_offset, ctx, events, suspend_policy);
}

static void
process_single_step (void)
{
	DebuggerTlsData *tls;
	MonoContext ctx;
	static void (*restore_context) (void *);

	if (!restore_context)
		restore_context = mono_get_restore_context ();

	tls = TlsGetValue (debugger_tls_id);
	memcpy (&ctx, &tls->handler_ctx, sizeof (MonoContext));

	process_single_step_inner (tls, &ctx);

	/* This is called when resuming from a signal handler, so it shouldn't return */
	restore_context (&ctx);
	g_assert_not_reached ();
}

/*
 * mono_debugger_agent_single_step_event:
 *
 *   Called from a signal handler to handle a single step event.
 */
void
mono_debugger_agent_single_step_event (void *sigctx)
{
	/* Resume to process_single_step through the signal context */

	// FIXME: Since step out/over is implemented using step in, the step in case should
	// be as fast as possible. Move the relevant code from process_single_step_inner ()
	// here

	if (GetCurrentThreadId () == debugger_thread_id) {
		/* 
		 * This could happen despite our best effors when the runtime calls 
		 * assembly/type resolve hooks.
		 * FIXME: Breakpoints too.
		 */
		MonoContext ctx;

		mono_arch_sigctx_to_monoctx (sigctx, &ctx);
		mono_arch_skip_single_step (&ctx);
		mono_arch_monoctx_to_sigctx (&ctx, sigctx);
		return;
	}

	resume_from_signal_handler (sigctx, process_single_step);
}

/*
 * start_single_stepping:
 *
 *   Turn on single stepping. Can be called multiple times, for example,
 * by a single step event request + a suspend.
 */
static void
start_single_stepping (void)
{
#ifdef MONO_ARCH_SOFT_DEBUG_SUPPORTED
	int val = InterlockedIncrement (&ss_count);

	if (val == 1)
		mono_arch_start_single_stepping ();

	if (ss_req != NULL && ss_invoke_addr == NULL) {
		DebuggerTlsData *tls;
	
		mono_loader_lock ();
	
 		tls = mono_g_hash_table_lookup (thread_to_tls, ss_req->thread);
		ss_invoke_addr = tls->invoke_addr;
		
		mono_loader_unlock ();
	}
#else
	g_assert_not_reached ();
#endif
}

static void
stop_single_stepping (void)
{
#ifdef MONO_ARCH_SOFT_DEBUG_SUPPORTED
	int val = InterlockedDecrement (&ss_count);

	if (val == 0)
		mono_arch_stop_single_stepping ();
#else
	g_assert_not_reached ();
#endif
}

/*
 * ss_stop:
 *
 *   Stop the single stepping operation given by SS_REQ.
 */
static void
ss_stop (SingleStepReq *ss_req)
{
	gboolean use_bps = FALSE;

	if (ss_req->bps) {
		GSList *l;

		use_bps = TRUE;

		for (l = ss_req->bps; l; l = l->next) {
			clear_breakpoint (l->data);
		}
		g_slist_free (ss_req->bps);
		ss_req->bps = NULL;
	}

	if (ss_req->global) {
		stop_single_stepping ();
		ss_req->global = FALSE;
	}
}

/*
 * ss_start:
 *
 *   Start the single stepping operation given by SS_REQ from the sequence point SP.
 */
static void
ss_start (SingleStepReq *ss_req, MonoMethod *method, SeqPoint *sp, MonoSeqPointInfo *info, MonoContext *ctx, DebuggerTlsData *tls)
{
	gboolean use_bp = FALSE;
	int i, frame_index;
	SeqPoint *next_sp;
	MonoBreakpoint *bp;

	/* Stop the previous operation */
	ss_stop (ss_req);

	/*
	 * Implement single stepping using breakpoints if possible.
	 */
	if (ss_req->depth == STEP_DEPTH_OVER) {
		frame_index = 1;
		/*
		 * Find the first sequence point in the current or in a previous frame which
		 * is not the last in its method.
		 */
		while (sp && sp->next_len == 0) {
			sp = NULL;
			if (tls && frame_index < tls->frame_count) {
				StackFrame *frame = tls->frames [frame_index];

				method = frame->method;
				if (frame->il_offset != -1) {
					sp = find_seq_point (frame->domain, frame->method, frame->il_offset, &info);
				}
				frame_index ++;
			}
		}

		if (sp && sp->next_len > 0) {
			use_bp = TRUE;
			for (i = 0; i < sp->next_len; ++i) {
				next_sp = &info->seq_points [sp->next [i]];

				bp = set_breakpoint (method, next_sp->il_offset, ss_req->req);
				ss_req->bps = g_slist_append (ss_req->bps, bp);
			}
		}
	}

	if (!ss_req->bps) {
		ss_req->global = TRUE;
		start_single_stepping ();
	} else {
		ss_req->global = FALSE;
	}
}

/*
 * Start single stepping of thread THREAD
 */
static ErrorCode
ss_create (MonoInternalThread *thread, StepSize size, StepDepth depth, EventRequest *req)
{
	DebuggerTlsData *tls;
	MonoSeqPointInfo *info;
	SeqPoint *sp = NULL;
	MonoMethod *method = NULL;

	if (suspend_count == 0)
		return ERR_NOT_SUSPENDED;

	wait_for_suspend ();

	// FIXME: Multiple requests
	if (ss_req) {
		DEBUG (0, printf ("Received a single step request while the previous one was still active.\n"));
		return ERR_NOT_IMPLEMENTED;
	}

	ss_req = g_new0 (SingleStepReq, 1);
	ss_req->req = req;
	ss_req->thread = thread;
	ss_req->size = size;
	ss_req->depth = depth;
	req->info = ss_req;

	mono_loader_lock ();
	tls = mono_g_hash_table_lookup (thread_to_tls, thread);
	mono_loader_unlock ();
	g_assert (tls);
	g_assert (tls->has_context);
	ss_req->start_sp = ss_req->last_sp = MONO_CONTEXT_GET_SP (&tls->ctx);

	if (ss_req->size == STEP_SIZE_LINE) {
		StackFrame *frame;
		MonoDebugMethodInfo *minfo;

		/* Compute the initial line info */
		compute_frame_info (thread, tls);

		g_assert (tls->frame_count);
		frame = tls->frames [0];

		ss_req->last_method = frame->method;
		ss_req->last_line = -1;

		minfo = mono_debug_lookup_method (frame->method);
		if (minfo && frame->il_offset != -1) {
			MonoDebugSourceLocation *loc = mono_debug_symfile_lookup_location (minfo, frame->il_offset);

			if (loc) {
				ss_req->last_line = loc->row;
				g_free (loc);
			}
		}
	}

	if (ss_req->depth == STEP_DEPTH_OVER) {
		StackFrame *frame;

		compute_frame_info (thread, tls);

		g_assert (tls->frame_count);
		frame = tls->frames [0];

		if (frame->il_offset != -1) {
			/* FIXME: Sort the table and use a binary search */
			sp = find_seq_point (frame->domain, frame->method, frame->il_offset, &info);
			g_assert (sp);
			method = frame->method;
		}
	}

	ss_start (ss_req, method, sp, info, NULL, tls);

	return 0;
}

static void
ss_destroy (SingleStepReq *req)
{
	// FIXME: Locking
	g_assert (ss_req == req);

	ss_stop (ss_req);

	g_free (ss_req);
	ss_req = NULL;
}

void
mono_debugger_agent_handle_exception (MonoException *exc, MonoContext *ctx)
{
	int suspend_policy;
	GSList *events;
	MonoJitInfo *ji;

	/* Just-In-Time debugging */
	if (agent_config.onthrow && !inited) {
		GSList *l;
		gboolean found = FALSE;

		for (l = agent_config.onthrow; l; l = l->next) {
			char *ex_type = l->data;
			char *f = mono_type_full_name (&exc->object.vtable->klass->byval_arg);

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
			process_event (EVENT_KIND_EXCEPTION, exc, 0, ctx, events, SUSPEND_POLICY_ALL);
			return;
		}
	}

	if (!inited)
		return;

	ji = mini_jit_info_table_find (mono_domain_get (), MONO_CONTEXT_GET_IP (ctx), NULL);

	mono_loader_lock ();
	events = create_event_list (EVENT_KIND_EXCEPTION, NULL, ji, exc, &suspend_policy);
	mono_loader_unlock ();

	process_event (EVENT_KIND_EXCEPTION, exc, 0, ctx, events, suspend_policy);
}

void
mono_debugger_agent_handle_unhandled_exception (MonoException *exc, MonoContext *ctx)
{
	GSList *events;

	if (!agent_config.onuncaught)
		return;

	finish_agent_init (FALSE);

	/*
	 * Send an unsolicited EXCEPTION event with a dummy request id.
	 */
	events = g_slist_append (NULL, GUINT_TO_POINTER (0xffffff));
	process_event (EVENT_KIND_EXCEPTION, exc, 0, ctx, events, SUSPEND_POLICY_ALL);
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
					   gboolean as_vtype)
{
	MonoObject *obj;

	if (t->byref) {
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
			if (obj->vtable->klass->valuetype) {
				t = &obj->vtable->klass->byval_arg;
				addr = mono_object_unbox (obj);
				goto handle_vtype;
			} else if (obj->vtable->klass->rank) {
				buffer_add_byte (buf, obj->vtable->klass->byval_arg.type);
			} else if (obj->vtable->klass->byval_arg.type == MONO_TYPE_GENERICINST) {
				buffer_add_byte (buf, MONO_TYPE_CLASS);
			} else {
				buffer_add_byte (buf, obj->vtable->klass->byval_arg.type);
			}
			buffer_add_objid (buf, obj);
		}
		break;
	handle_vtype:
	case MONO_TYPE_VALUETYPE: {
		int nfields;
		gpointer iter;
		MonoClassField *f;
		MonoClass *klass = mono_class_from_mono_type (t);

		buffer_add_byte (buf, MONO_TYPE_VALUETYPE);
		buffer_add_byte (buf, klass->enumtype);
		buffer_add_typeid (buf, domain, klass);

		nfields = 0;
		iter = NULL;
		while ((f = mono_class_get_fields (klass, &iter))) {
			if (f->type->attrs & FIELD_ATTRIBUTE_STATIC)
				continue;
			if (mono_field_is_deleted (f))
				continue;
			nfields ++;
		}
		buffer_add_int (buf, nfields);

		iter = NULL;
		while ((f = mono_class_get_fields (klass, &iter))) {
			if (f->type->attrs & FIELD_ATTRIBUTE_STATIC)
				continue;
			if (mono_field_is_deleted (f))
				continue;
			buffer_add_value_full (buf, f->type, (guint8*)addr + f->offset - sizeof (MonoObject), domain, FALSE);
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
	buffer_add_value_full (buf, t, addr, domain, FALSE);
}

static ErrorCode
decode_value (MonoType *t, MonoDomain *domain, guint8 *addr, guint8 *buf, guint8 **endbuf, guint8 *limit)
{
	int err;
	int type = decode_byte (buf, &buf, limit);

	if (type != t->type && !MONO_TYPE_IS_REFERENCE (t) &&
		!(t->type == MONO_TYPE_I && type == MONO_TYPE_VALUETYPE) &&
		!(t->type == MONO_TYPE_U && type == MONO_TYPE_VALUETYPE) &&
		!(t->type == MONO_TYPE_PTR && type == MONO_TYPE_I8) &&
		!(t->type == MONO_TYPE_GENERICINST && type == MONO_TYPE_VALUETYPE)) {
		DEBUG(1, fprintf (log_file, "[%p] Expected value of type 0x%0x, got 0x%0x.\n", (gpointer)GetCurrentThreadId (), t->type, type));
		return ERR_INVALID_ARGUMENT;
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
	case MONO_TYPE_VALUETYPE: {
		gboolean is_enum = decode_byte (buf, &buf, limit);
		MonoClass *klass;
		MonoClassField *f;
		int nfields;
		gpointer iter = NULL;
		MonoDomain *d;

		/* Enums are sent as a normal vtype */
		if (is_enum)
			return ERR_NOT_IMPLEMENTED;
		klass = decode_typeid (buf, &buf, limit, &d, &err);
		if (err)
			return err;

		if (klass != mono_class_from_mono_type (t))
			return ERR_INVALID_ARGUMENT;

		nfields = decode_int (buf, &buf, limit);
		while ((f = mono_class_get_fields (klass, &iter))) {
			if (f->type->attrs & FIELD_ATTRIBUTE_STATIC)
				continue;
			if (mono_field_is_deleted (f))
				continue;
			err = decode_value (f->type, domain, (guint8*)addr + f->offset - sizeof (MonoObject), buf, &buf, limit);
			if (err)
				return err;
			nfields --;
		}
		g_assert (nfields == 0);
		break;
	}
	handle_ref:
	default:
		if (MONO_TYPE_IS_REFERENCE (t)) {
			if (type == MONO_TYPE_OBJECT) {
				int objid = decode_objid (buf, &buf, limit);
				int err;
				MonoObject *obj;

				err = get_object (objid, (MonoObject**)&obj);
				if (err)
					return err;

				if (obj && !mono_class_is_assignable_from (mono_class_from_mono_type (t), obj->vtable->klass))
					return ERR_INVALID_ARGUMENT;
				if (obj && obj->vtable->domain != domain)
					return ERR_INVALID_ARGUMENT;

				mono_gc_wbarrier_generic_store (addr, obj);
			} else if (type == VALUE_TYPE_ID_NULL) {
				*(MonoObject**)addr = NULL;
			} else {
				return ERR_INVALID_ARGUMENT;
			}
		} else {
			NOT_IMPLEMENTED;
		}
		break;
	}

	*endbuf = buf;

	return 0;
}

static void
add_var (Buffer *buf, MonoType *t, MonoDebugVarInfo *var, MonoContext *ctx, MonoDomain *domain, gboolean as_vtype)
{
	guint32 flags;
	int reg;
	guint8 *addr;
	gpointer reg_val;

	flags = var->index & MONO_DEBUG_VAR_ADDRESS_MODE_FLAGS;
	reg = var->index & ~MONO_DEBUG_VAR_ADDRESS_MODE_FLAGS;

	switch (flags) {
	case MONO_DEBUG_VAR_ADDRESS_MODE_REGISTER:
		reg_val = mono_arch_context_get_int_reg (ctx, reg);

		buffer_add_value_full (buf, t, &reg_val, domain, as_vtype);
		break;
	case MONO_DEBUG_VAR_ADDRESS_MODE_REGOFFSET:
		addr = mono_arch_context_get_int_reg (ctx, reg);
		addr += (gint32)var->offset;

		//printf ("[R%d+%d] = %p\n", reg, var->offset, addr);

		buffer_add_value_full (buf, t, addr, domain, as_vtype);
		break;
	case MONO_DEBUG_VAR_ADDRESS_MODE_DEAD:
		NOT_IMPLEMENTED;
		break;
	default:
		g_assert_not_reached ();
	}
}

static void
set_var (MonoType *t, MonoDebugVarInfo *var, MonoContext *ctx, MonoDomain *domain, guint8 *val)
{
	guint32 flags;
	int reg, size;
	guint8 *addr;

	flags = var->index & MONO_DEBUG_VAR_ADDRESS_MODE_FLAGS;
	reg = var->index & ~MONO_DEBUG_VAR_ADDRESS_MODE_FLAGS;

	if (MONO_TYPE_IS_REFERENCE (t))
		size = sizeof (gpointer);
	else
		size = mono_class_value_size (mono_class_from_mono_type (t), NULL);

	switch (flags) {
	case MONO_DEBUG_VAR_ADDRESS_MODE_REGISTER:
		// FIXME: Can't set registers, so we disable linears
		NOT_IMPLEMENTED;
		break;
	case MONO_DEBUG_VAR_ADDRESS_MODE_REGOFFSET:
		addr = mono_arch_context_get_int_reg (ctx, reg);
		addr += (gint32)var->offset;

		//printf ("[R%d+%d] = %p\n", reg, var->offset, addr);

		// FIXME: Write barriers
		memcpy (addr, val, size);
		break;
	case MONO_DEBUG_VAR_ADDRESS_MODE_DEAD:
		NOT_IMPLEMENTED;
		break;
	default:
		g_assert_not_reached ();
	}
}

static void
clear_event_request (int req_id, int etype)
{
	int i;

	mono_loader_lock ();
	for (i = 0; i < event_requests->len; ++i) {
		EventRequest *req = g_ptr_array_index (event_requests, i);

		if (req->id == req_id && req->event_kind == etype) {
			if (req->event_kind == EVENT_KIND_BREAKPOINT)
				clear_breakpoint (req->info);
			if (req->event_kind == EVENT_KIND_STEP)
				ss_destroy (req->info);
			if (req->event_kind == EVENT_KIND_METHOD_ENTRY)
				clear_breakpoint (req->info);
			if (req->event_kind == EVENT_KIND_METHOD_EXIT)
				clear_breakpoint (req->info);
			g_ptr_array_remove_index_fast (event_requests, i);
			g_free (req);
			break;
		}
	}
	mono_loader_unlock ();
}

static void
add_thread (gpointer key, gpointer value, gpointer user_data)
{
	MonoInternalThread *thread = value;
	Buffer *buf = user_data;

	buffer_add_objid (buf, (MonoObject*)thread);
}

static ErrorCode
do_invoke_method (DebuggerTlsData *tls, Buffer *buf, InvokeData *invoke)
{
	guint8 *p = invoke->p;
	guint8 *end = invoke->endp;
	MonoMethod *m;
	int i, err, nargs;
	MonoMethodSignature *sig;
	guint8 **arg_buf;
	void **args;
	MonoObject *this, *res, *exc;
	MonoDomain *domain;
	guint8 *this_buf;
#ifdef MONO_ARCH_HAVE_FIND_JIT_INFO_EXT
	MonoLMFExt ext;
#endif

	if (invoke->method) {
		/* 
		 * Invoke this method directly, currently only Environment.Exit () is supported.
		 */
		this = NULL;
		DEBUG (1, printf ("[%p] Invoking method '%s' on receiver '%s'.\n", (gpointer)GetCurrentThreadId (), mono_method_full_name (invoke->method, TRUE), this ? this->vtable->klass->name : "<null>"));
		mono_runtime_invoke (invoke->method, NULL, invoke->args, &exc);
		g_assert_not_reached ();
	}

	m = decode_methodid (p, &p, end, &domain, &err);
	if (err)
		return err;
	sig = mono_method_signature (m);

	if (m->klass->valuetype)
		this_buf = g_alloca (mono_class_instance_size (m->klass));
	else
		this_buf = g_alloca (sizeof (MonoObject*));
	err = decode_value (&m->klass->byval_arg, domain, this_buf, p, &p, end);
	if (err)
		return err;

	if (!m->klass->valuetype)
		this = *(MonoObject**)this_buf;
	else
		this = NULL;

	DEBUG (1, printf ("[%p] Invoking method '%s' on receiver '%s'.\n", (gpointer)GetCurrentThreadId (), mono_method_full_name (m, TRUE), this ? this->vtable->klass->name : "<null>"));

	if (this && this->vtable->domain != domain)
		NOT_IMPLEMENTED;

	if (!m->klass->valuetype && !(m->flags & METHOD_ATTRIBUTE_STATIC) && !this) {
		if (!strcmp (m->name, ".ctor")) {
			if (m->klass->flags & TYPE_ATTRIBUTE_ABSTRACT)
				return ERR_INVALID_ARGUMENT;
			else
				this = mono_object_new (domain, m->klass);
		} else {
			return ERR_INVALID_ARGUMENT;
		}
	}

	if (this && !mono_class_is_assignable_from (m->klass, this->vtable->klass))
		return ERR_INVALID_ARGUMENT;

	nargs = decode_int (p, &p, end);
	if (nargs != sig->param_count)
		return ERR_INVALID_ARGUMENT;
	/* Use alloca to get gc tracking */
	arg_buf = g_alloca (nargs * sizeof (gpointer));
	memset (arg_buf, 0, nargs * sizeof (gpointer));
	args = g_alloca (nargs * sizeof (gpointer));
	for (i = 0; i < nargs; ++i) {
		if (MONO_TYPE_IS_REFERENCE (sig->params [i])) {
			err = decode_value (sig->params [i], domain, (guint8*)&args [i], p, &p, end);
			if (err)
				break;

			if (args [i] && ((MonoObject*)args [i])->vtable->domain != domain)
				NOT_IMPLEMENTED;
		} else {
			arg_buf [i] = g_alloca (mono_class_instance_size (mono_class_from_mono_type (sig->params [i])));
			err = decode_value (sig->params [i], domain, arg_buf [i], p, &p, end);
			if (err)
				break;
			args [i] = arg_buf [i];
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
	/* FIXME: Move this to arch specific code */
#ifdef MONO_ARCH_HAVE_FIND_JIT_INFO_EXT
	if (invoke->has_ctx) {
		MonoLMF **lmf_addr;

		lmf_addr = mono_get_lmf_addr ();

		/* Setup our lmf */
		memset (&ext, 0, sizeof (ext));
#ifdef TARGET_AMD64
		ext.lmf.previous_lmf = *(lmf_addr);
		/* Mark that this is a MonoLMFExt */
		ext.lmf.previous_lmf = (gpointer)(((gssize)ext.lmf.previous_lmf) | 2);
		ext.lmf.rsp = (gssize)&ext;
#elif defined(TARGET_X86)
		ext.lmf.previous_lmf = (gsize)*(lmf_addr);
		/* Mark that this is a MonoLMFExt */
		ext.lmf.previous_lmf = (gsize)(gpointer)(((gssize)ext.lmf.previous_lmf) | 2);
		ext.lmf.ebp = (gssize)&ext;
#elif defined(TARGET_ARM)
		ext.lmf.previous_lmf = *(lmf_addr);
		/* Mark that this is a MonoLMFExt */
		ext.lmf.previous_lmf = (gpointer)(((gssize)ext.lmf.previous_lmf) | 2);
		ext.lmf.ebp = (gssize)&ext;
#elif defined(TARGET_POWERPC)
		ext.lmf.previous_lmf = *(lmf_addr);
		/* Mark that this is a MonoLMFExt */
		ext.lmf.previous_lmf = (gpointer)(((gssize)ext.lmf.previous_lmf) | 2);
		ext.lmf.ebp = (gssize)&ext;
#else
		g_assert_not_reached ();
#endif

		ext.debugger_invoke = TRUE;
		memcpy (&ext.ctx, &invoke->ctx, sizeof (MonoContext));

		mono_set_lmf ((MonoLMF*)&ext);
	}
#endif

	if (m->klass->valuetype)
		res = mono_runtime_invoke (m, this_buf, args, &exc);
	else
		res = mono_runtime_invoke (m, this, args, &exc);
	if (exc) {
		buffer_add_byte (buf, 0);
		buffer_add_value (buf, &mono_defaults.object_class->byval_arg, &exc, domain);
	} else {
		buffer_add_byte (buf, 1);
		if (sig->ret->type == MONO_TYPE_VOID) {
			if (!strcmp (m->name, ".ctor") && !m->klass->valuetype) {
				buffer_add_value (buf, &mono_defaults.object_class->byval_arg, &this, domain);
			}
			else
				buffer_add_value (buf, &mono_defaults.void_class->byval_arg, NULL, domain);
		} else if (MONO_TYPE_IS_REFERENCE (sig->ret)) {
			buffer_add_value (buf, sig->ret, &res, domain);
		} else if (mono_class_from_mono_type (sig->ret)->valuetype) {
			g_assert (res);
			buffer_add_value (buf, sig->ret, mono_object_unbox (res), domain);
		} else {
			NOT_IMPLEMENTED;
		}
	}

	tls->disable_breakpoints = FALSE;

#ifdef MONO_ARCH_HAVE_FIND_JIT_INFO_EXT
	if (invoke->has_ctx)
		mono_set_lmf ((gpointer)(((gssize)ext.lmf.previous_lmf) & ~3));
#endif

	// FIXME: byref arguments
	// FIXME: varargs
	return ERR_NONE;
}

/*
 * invoke_method:
 *
 *   Invoke the method given by tls->invoke in the current thread.
 */
static void
invoke_method (void)
{
	DebuggerTlsData *tls;
	InvokeData *invoke;
	int id;
	int err;
	Buffer buf;
	static void (*restore_context) (void *);
	MonoContext restore_ctx;

	if (!restore_context)
		restore_context = mono_get_restore_context ();

	tls = TlsGetValue (debugger_tls_id);
	g_assert (tls);

	invoke = tls->invoke;
	g_assert (invoke);
	tls->invoke = NULL;

	tls->frames_up_to_date = FALSE;

	id = invoke->id;

	buffer_init (&buf, 128);

	err = do_invoke_method (tls, &buf, invoke);

	/* Start suspending before sending the reply */
	if (!(invoke->flags & INVOKE_FLAG_SINGLE_THREADED))
		suspend_vm ();

	send_reply_packet (id, err, &buf);
	
	buffer_free (&buf);

	memcpy (&restore_ctx, &invoke->ctx, sizeof (MonoContext));

	if (invoke->has_ctx)
		save_thread_context (&restore_ctx);

	if (invoke->flags & INVOKE_FLAG_SINGLE_THREADED) {
		g_assert (tls->resume_count);
		tls->resume_count -= invoke->suspend_count;
	}

	DEBUG (1, printf ("[%p] Invoke finished, resume_count = %d.\n", (gpointer)GetCurrentThreadId (), tls->resume_count));

	g_free (invoke->p);
	g_free (invoke);

	suspend_current ();
}

static gboolean
is_really_suspended (gpointer key, gpointer value, gpointer user_data)
{
	MonoThread *thread = value;
	DebuggerTlsData *tls;
	gboolean res;

	mono_loader_lock ();
	tls = mono_g_hash_table_lookup (thread_to_tls, thread);
	g_assert (tls);
	res = tls->really_suspended;
	mono_loader_unlock ();

	return res;
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
	case CMD_VM_ALL_THREADS: {
		// FIXME: Domains
		mono_loader_lock ();
		buffer_add_int (buf, mono_g_hash_table_size (tid_to_thread_obj));
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
		break;
	case CMD_VM_DISPOSE:
		/* Clear all event requests */
		mono_loader_lock ();
		while (event_requests->len > 0) {
			EventRequest *req = g_ptr_array_index (event_requests, 0);

			clear_event_request (req->id, req->event_kind);
		}
		mono_loader_unlock ();

		// FIXME: Count resumes
		resume_vm ();
		disconnected = TRUE;
		break;
	case CMD_VM_EXIT: {
		MonoInternalThread *thread;
		DebuggerTlsData *tls;
		MonoClass *env_class;
		MonoMethod *exit_method;
		gpointer *args;
		int exit_code;

		exit_code = decode_int (p, &p, end);

		// FIXME: What if there is a VM_DEATH event request with SUSPEND_ALL ?

		/* Have to send a reply before exiting */
		send_reply_packet (id, 0, buf);

		/* Clear all event requests */
		mono_loader_lock ();
		while (event_requests->len > 0) {
			EventRequest *req = g_ptr_array_index (event_requests, 0);

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

		env_class = mono_class_from_name (mono_defaults.corlib, "System", "Environment");
		g_assert (env_class);
		exit_method = mono_class_get_method_from_name (env_class, "Exit", 1);
		g_assert (exit_method);

		mono_loader_lock ();
		thread = mono_g_hash_table_find (tid_to_thread, is_really_suspended, NULL);
		mono_loader_unlock ();

		if (thread) {
			mono_loader_lock ();
			tls = mono_g_hash_table_lookup (thread_to_tls, thread);
			mono_loader_unlock ();

			args = g_new0 (gpointer, 1);
			args [0] = g_malloc (sizeof (int));
			*(int*)(args [0]) = exit_code;

			tls->invoke = g_new0 (InvokeData, 1);
			tls->invoke->method = exit_method;
			tls->invoke->args = args;

			while (suspend_count > 0)
				resume_vm ();
		} else {
			/* 
			 * No thread found, do it ourselves.
			 * FIXME: This can race with normal shutdown etc.
			 */
			while (suspend_count > 0)
				resume_vm ();

			mono_runtime_set_shutting_down ();

			mono_threads_set_shutting_down ();

			/* Suspend all managed threads since the runtime is going away */
			DEBUG(1, fprintf (log_file, "Suspending all threads...\n"));
			mono_thread_suspend_all_other_threads ();
			DEBUG(1, fprintf (log_file, "Shutting down the runtime...\n"));
			mono_runtime_quit ();
#ifdef HOST_WIN32
			shutdown (conn_fd, SD_BOTH);
#else
			shutdown (conn_fd, SHUT_RDWR);
#endif
			DEBUG(1, fprintf (log_file, "Exiting...\n"));

			exit (exit_code);
		}
		break;
	}		
	case CMD_VM_INVOKE_METHOD: {
		int objid = decode_objid (p, &p, end);
		MonoThread *thread;
		DebuggerTlsData *tls;
		int err, flags;

		err = get_object (objid, (MonoObject**)&thread);
		if (err)
			return err;

		flags = decode_int (p, &p, end);

		// Wait for suspending if it already started
		if (suspend_count)
			wait_for_suspend ();
		if (!is_suspended ())
			return ERR_NOT_SUSPENDED;

		mono_loader_lock ();
		tls = mono_g_hash_table_lookup (thread_to_tls, THREAD_TO_INTERNAL (thread));
		mono_loader_unlock ();
		g_assert (tls);

		if (!tls->really_suspended)
			/* The thread is still running native code, can't do invokes */
			return ERR_NOT_SUSPENDED;

		/* 
		 * Store the invoke data into tls, the thread will execute it after it is
		 * resumed.
		 */
		if (tls->invoke)
			NOT_IMPLEMENTED;
		tls->invoke = g_new0 (InvokeData, 1);
		tls->invoke->id = id;
		tls->invoke->flags = flags;
		tls->invoke->p = g_malloc (end - p);
		memcpy (tls->invoke->p, p, end - p);
		tls->invoke->endp = tls->invoke->p + (end - p);
		tls->invoke->suspend_count = suspend_count;

		if (flags & INVOKE_FLAG_SINGLE_THREADED)
			resume_thread (THREAD_TO_INTERNAL (thread));
		else
			resume_vm ();
		break;
	}
	default:
		return ERR_NOT_IMPLEMENTED;
	}

	return ERR_NONE;
}

static ErrorCode
event_commands (int command, guint8 *p, guint8 *end, Buffer *buf)
{
	int err;

	switch (command) {
	case CMD_EVENT_REQUEST_SET: {
		EventRequest *req;
		int i, event_kind, suspend_policy, nmodifiers, mod;
		MonoMethod *method;
		long location = 0;
		MonoThread *step_thread;
		int size = 0, depth = 0, step_thread_id = 0;
		MonoDomain *domain;

		event_kind = decode_byte (p, &p, end);
		suspend_policy = decode_byte (p, &p, end);
		nmodifiers = decode_byte (p, &p, end);

		req = g_malloc0 (sizeof (EventRequest) + (nmodifiers * sizeof (Modifier)));
		req->id = InterlockedIncrement (&event_request_id);
		req->event_kind = event_kind;
		req->suspend_policy = suspend_policy;
		req->nmodifiers = nmodifiers;

		method = NULL;
		for (i = 0; i < nmodifiers; ++i) {
			mod = decode_byte (p, &p, end);

			req->modifiers [i].kind = mod;
			if (mod == MOD_KIND_COUNT) {
				req->modifiers [i].data.count = decode_int (p, &p, end);
			} else if (mod == MOD_KIND_LOCATION_ONLY) {
				method = decode_methodid (p, &p, end, &domain, &err);
				if (err)
					return err;
				location = decode_long (p, &p, end);
			} else if (mod == MOD_KIND_STEP) {
				step_thread_id = decode_id (p, &p, end);
				size = decode_int (p, &p, end);
				depth = decode_int (p, &p, end);
			} else if (mod == MOD_KIND_THREAD_ONLY) {
				int id = decode_id (p, &p, end);

				err = get_object (id, (MonoObject**)&req->modifiers [i].data.thread);
				if (err) {
					g_free (req);
					return err;
				}
			} else if (mod == MOD_KIND_EXCEPTION_ONLY) {
				MonoClass *exc_class = decode_typeid (p, &p, end, &domain, &err);

				if (err)
					return err;
				if (exc_class) {
					req->modifiers [i].data.exc_class = exc_class;

					if (!mono_class_is_assignable_from (mono_defaults.exception_class, exc_class)) {
						g_free (req);
						return ERR_INVALID_ARGUMENT;
					}
				}
			} else if (mod == MOD_KIND_ASSEMBLY_ONLY) {
				int n = decode_int (p, &p, end);
				int j;

				req->modifiers [i].data.assemblies = g_new0 (MonoAssembly*, n);
				for (j = 0; j < n; ++j) {
					req->modifiers [i].data.assemblies [j] = decode_assemblyid (p, &p, end, &domain, &err);
					if (err) {
						g_free (req->modifiers [i].data.assemblies);
						return err;
					}
				}
			} else {
				g_free (req);
				return ERR_NOT_IMPLEMENTED;
			}
		}

		if (req->event_kind == EVENT_KIND_BREAKPOINT) {
			g_assert (method);

			req->info = set_breakpoint (method, location, req);
		} else if (req->event_kind == EVENT_KIND_STEP) {
			g_assert (step_thread_id);

			err = get_object (step_thread_id, (MonoObject**)&step_thread);
			if (err) {
				g_free (req);
				return err;
			}

			err = ss_create (THREAD_TO_INTERNAL (step_thread), size, depth, req);
			if (err) {
				g_free (req);
				return err;
			}
		} else if (req->event_kind == EVENT_KIND_METHOD_ENTRY) {
			req->info = set_breakpoint (NULL, METHOD_ENTRY_IL_OFFSET, req);
		} else if (req->event_kind == EVENT_KIND_METHOD_EXIT) {
			req->info = set_breakpoint (NULL, METHOD_EXIT_IL_OFFSET, req);
		} else if (req->event_kind == EVENT_KIND_EXCEPTION) {
		} else {
			if (req->nmodifiers) {
				g_free (req);
				return ERR_NOT_IMPLEMENTED;
			}
		}

		mono_loader_lock ();
		g_ptr_array_add (event_requests, req);
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
			EventRequest *req = g_ptr_array_index (event_requests, i);

			if (req->event_kind == EVENT_KIND_BREAKPOINT) {
				clear_breakpoint (req->info);

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
	int err;
	MonoDomain *domain;

	switch (command) {
	case CMD_APPDOMAIN_GET_ROOT_DOMAIN: {
		buffer_add_domainid (buf, mono_get_root_domain ());
		break;
	}
	case CMD_APPDOMAIN_GET_FRIENDLY_NAME: {
		domain = decode_domainid (p, &p, end, NULL, &err);
		if (err)
			return err;
		buffer_add_string (buf, domain->friendly_name);
		break;
	}
	case CMD_APPDOMAIN_GET_ASSEMBLIES: {
		GSList *tmp;
		MonoAssembly *ass;
		int count;

		domain = decode_domainid (p, &p, end, NULL, &err);
		if (err)
			return err;
		mono_loader_lock ();
		count = 0;
		for (tmp = domain->domain_assemblies; tmp; tmp = tmp->next) {
			count ++;
		}
		buffer_add_int (buf, count);
		for (tmp = domain->domain_assemblies; tmp; tmp = tmp->next) {
			ass = tmp->data;
			buffer_add_assemblyid (buf, domain, ass);
		}
		mono_loader_unlock ();
		break;
	}
	case CMD_APPDOMAIN_GET_ENTRY_ASSEMBLY: {
		domain = decode_domainid (p, &p, end, NULL, &err);
		if (err)
			return err;

		buffer_add_assemblyid (buf, domain, domain->entry_assembly);
		break;
	}
	case CMD_APPDOMAIN_GET_CORLIB: {
		domain = decode_domainid (p, &p, end, NULL, &err);
		if (err)
			return err;

		buffer_add_assemblyid (buf, domain, domain->domain->mbr.obj.vtable->klass->image->assembly);
		break;
	}
	case CMD_APPDOMAIN_CREATE_STRING: {
		char *s;
		MonoString *o;

		domain = decode_domainid (p, &p, end, NULL, &err);
		if (err)
			return err;
		s = decode_string (p, &p, end);

		o = mono_string_new (domain, s);
		buffer_add_objid (buf, (MonoObject*)o);
		break;
	}
	default:
		return ERR_NOT_IMPLEMENTED;
	}

	return ERR_NONE;
}

static ErrorCode
assembly_commands (int command, guint8 *p, guint8 *end, Buffer *buf)
{
	int err;
	MonoAssembly *ass;
	MonoDomain *domain;

	ass = decode_assemblyid (p, &p, end, &domain, &err);
	if (err)
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
				m = mono_get_method (ass->image, token, NULL);
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
		MonoObject *o = (MonoObject*)mono_assembly_get_object (mono_domain_get (), ass);
		buffer_add_objid (buf, o);
		break;
	}
	case CMD_ASSEMBLY_GET_TYPE: {
		char *s = decode_string (p, &p, end);
		gboolean ignorecase = decode_byte (p, &p, end);
		MonoTypeNameParse info;
		MonoType *t;
		gboolean type_resolve;

		if (!mono_reflection_parse_type (s, &info)) {
			t = NULL;
		} else {
			if (info.assembly.name)
				NOT_IMPLEMENTED;
			t = mono_reflection_get_type (ass->image, &info, ignorecase, &type_resolve);
		}
		buffer_add_typeid (buf, domain, t ? mono_class_from_mono_type (t) : NULL);
		mono_reflection_free_type_info (&info);
		g_free (s);

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
	default:
		return ERR_NOT_IMPLEMENTED;
	}

	return ERR_NONE;
}

static ErrorCode
module_commands (int command, guint8 *p, guint8 *end, Buffer *buf)
{
	int err;
	MonoDomain *domain;

	switch (command) {
	case CMD_MODULE_GET_INFO: {
		MonoImage *image = decode_moduleid (p, &p, end, &domain, &err);
		char *basename;

		basename = g_path_get_basename (image->name);
		buffer_add_string (buf, basename); // name
		buffer_add_string (buf, image->module_name); // scopename
		buffer_add_string (buf, image->name); // fqname
		buffer_add_string (buf, mono_image_get_guid (image)); // guid
		buffer_add_assemblyid (buf, domain, image->assembly); // assembly
		g_free (basename);
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
	if (val && val->vtable->klass == mono_defaults.monotype_class) {
		/* Special case these so the client doesn't have to handle Type objects */
		
		buffer_add_byte (buf, VALUE_TYPE_ID_TYPE);
		buffer_add_typeid (buf, domain, mono_class_from_mono_type (((MonoReflectionType*)val)->type));
	} else if (MONO_TYPE_IS_REFERENCE (t))
		buffer_add_value (buf, t, &val, domain);
	else
		buffer_add_value (buf, t, mono_object_unbox (val), domain);
}

static void
buffer_add_cattrs (Buffer *buf, MonoDomain *domain, MonoImage *image, MonoClass *attr_klass, MonoCustomAttrInfo *cinfo)
{
	int i, j;
	int nattrs = 0;

	if (!cinfo) {
		buffer_add_int (buf, 0);
		return;
	}

	for (i = 0; i < cinfo->num_attrs; ++i) {
		if (!attr_klass || mono_class_has_parent (cinfo->attrs [i].ctor->klass, attr_klass))
			nattrs ++;
	}
	buffer_add_int (buf, nattrs);

	for (i = 0; i < cinfo->num_attrs; ++i) {
		MonoCustomAttrEntry *attr = &cinfo->attrs [i];
		if (!attr_klass || mono_class_has_parent (attr->ctor->klass, attr_klass)) {
			MonoArray *typed_args, *named_args;
			MonoType *t;
			CattrNamedArg *arginfo;

			mono_reflection_create_custom_attr_data_args (image, attr->ctor, attr->data, attr->data_size, &typed_args, &named_args, &arginfo);

			buffer_add_methodid (buf, domain, attr->ctor);

			/* Ctor args */
			if (typed_args) {
				buffer_add_int (buf, mono_array_length (typed_args));
				for (j = 0; j < mono_array_length (typed_args); ++j) {
					MonoObject *val = mono_array_get (typed_args, MonoObject*, j);

					t = mono_method_signature (attr->ctor)->params [j];

					buffer_add_cattr_arg (buf, t, domain, val);
				}
			} else {
				buffer_add_int (buf, 0);
			}

			/* Named args */
			if (named_args) {
				buffer_add_int (buf, mono_array_length (named_args));

				for (j = 0; j < mono_array_length (named_args); ++j) {
					MonoObject *val = mono_array_get (named_args, MonoObject*, j);

					if (arginfo [j].prop) {
						buffer_add_byte (buf, 0x54);
						buffer_add_propertyid (buf, domain, arginfo [j].prop);
					} else if (arginfo [j].field) {
						buffer_add_byte (buf, 0x53);
					} else {
						g_assert_not_reached ();
					}

					buffer_add_cattr_arg (buf, arginfo [j].type, domain, val);
				}
			} else {
				buffer_add_int (buf, 0);
			}
		}
	}
}

static ErrorCode
type_commands (int command, guint8 *p, guint8 *end, Buffer *buf)
{
	MonoClass *klass;
	MonoDomain *domain;
	MonoClass *nested;
	MonoType *type;
	gpointer iter;
	guint8 b;
	int err, nnested;
	char *name;

	klass = decode_typeid (p, &p, end, &domain, &err);
	if (err)
		return err;

	switch (command) {
	case CMD_TYPE_GET_INFO: {
		buffer_add_string (buf, klass->name_space);
		buffer_add_string (buf, klass->name);
		// FIXME: byref
		name = mono_type_get_name_full (&klass->byval_arg, MONO_TYPE_NAME_FORMAT_FULL_NAME);
		buffer_add_string (buf, name);
		g_free (name);
		buffer_add_assemblyid (buf, domain, klass->image->assembly);
		buffer_add_moduleid (buf, domain, klass->image);
		buffer_add_typeid (buf, domain, klass->parent);
		if (klass->rank || klass->byval_arg.type == MONO_TYPE_PTR)
			buffer_add_typeid (buf, domain, klass->element_class);
		else
			buffer_add_id (buf, 0);
		buffer_add_int (buf, klass->type_token);
		buffer_add_byte (buf, klass->rank);
		buffer_add_int (buf, klass->flags);
		b = 0;
		type = &klass->byval_arg;
		// FIXME: Can't decide whenever a class represents a byref type
		if (FALSE)
			b |= (1 << 0);
		if (type->type == MONO_TYPE_PTR)
			b |= (1 << 1);
		if (!type->byref && (((type->type >= MONO_TYPE_BOOLEAN) && (type->type <= MONO_TYPE_R8)) || (type->type == MONO_TYPE_I) || (type->type == MONO_TYPE_U)))
			b |= (1 << 2);
		if (type->type == MONO_TYPE_VALUETYPE)
			b |= (1 << 3);
		if (klass->enumtype)
			b |= (1 << 4);
		buffer_add_byte (buf, b);
		nnested = 0;
		iter = NULL;
		while ((nested = mono_class_get_nested_types (klass, &iter)))
			nnested ++;
		buffer_add_int (buf, nnested);
		iter = NULL;
		while ((nested = mono_class_get_nested_types (klass, &iter)))
			buffer_add_typeid (buf, domain, nested);
		break;
	}
	case CMD_TYPE_GET_METHODS: {
		int nmethods;
		int i = 0;
		gpointer iter = NULL;
		MonoMethod *m;

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

		while ((f = mono_class_get_fields (klass, &iter))) {
			buffer_add_fieldid (buf, domain, f);
			buffer_add_string (buf, f->name);
			buffer_add_typeid (buf, domain, mono_class_from_mono_type (f->type));
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
		MonoClass *attr_klass = decode_typeid (p, &p, end, NULL, &err);
		MonoCustomAttrInfo *cinfo;

		cinfo = mono_custom_attrs_from_class (klass);

		buffer_add_cattrs (buf, domain, klass->image, attr_klass, cinfo);
		break;
	}
	case CMD_TYPE_GET_FIELD_CATTRS: {
		MonoClass *attr_klass;
		MonoCustomAttrInfo *cinfo;
		MonoClassField *field;

		field = decode_fieldid (p, &p, end, NULL, &err);
		if (err)
			return err;
		attr_klass = decode_typeid (p, &p, end, NULL, &err);
		if (err)
			return err;

		cinfo = mono_custom_attrs_from_field (klass, field);

		buffer_add_cattrs (buf, domain, klass->image, attr_klass, cinfo);
		break;
	}
	case CMD_TYPE_GET_PROPERTY_CATTRS: {
		MonoClass *attr_klass;
		MonoCustomAttrInfo *cinfo;
		MonoProperty *prop;

		prop = decode_propertyid (p, &p, end, NULL, &err);
		if (err)
			return err;
		attr_klass = decode_typeid (p, &p, end, NULL, &err);
		if (err)
			return err;

		cinfo = mono_custom_attrs_from_property (klass, prop);

		buffer_add_cattrs (buf, domain, klass->image, attr_klass, cinfo);
		break;
	}
	case CMD_TYPE_GET_VALUES: {
		guint8 *val;
		MonoClassField *f;
		MonoVTable *vtable;
		MonoClass *k;
		int len, i;
		gboolean found;

		len = decode_int (p, &p, end);
		for (i = 0; i < len; ++i) {
			f = decode_fieldid (p, &p, end, NULL, &err);
			if (err)
				return err;

			if (!(f->type->attrs & FIELD_ATTRIBUTE_STATIC))
				return ERR_INVALID_FIELDID;
			if (mono_class_field_is_special_static (f))
				return ERR_INVALID_FIELDID;

			/* Check that the field belongs to the object */
			found = FALSE;
			for (k = klass; k; k = k->parent) {
				if (k == f->parent) {
					found = TRUE;
					break;
				}
			}
			if (!found)
				return ERR_INVALID_FIELDID;

			vtable = mono_class_vtable (domain, f->parent);
			val = g_malloc (mono_class_instance_size (mono_class_from_mono_type (f->type)));
			mono_field_static_get_value (vtable, f, val);
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
			if (err)
				return err;

			if (!(f->type->attrs & FIELD_ATTRIBUTE_STATIC))
				return ERR_INVALID_FIELDID;
			if (mono_class_field_is_special_static (f))
				return ERR_INVALID_FIELDID;

			/* Check that the field belongs to the object */
			found = FALSE;
			for (k = klass; k; k = k->parent) {
				if (k == f->parent) {
					found = TRUE;
					break;
				}
			}
			if (!found)
				return ERR_INVALID_FIELDID;

			// FIXME: Check for literal/const

			vtable = mono_class_vtable (domain, f->parent);
			val = g_malloc (mono_class_instance_size (mono_class_from_mono_type (f->type)));
			err = decode_value (f->type, domain, val, p, &p, end);
			if (err) {
				g_free (val);
				return err;
			}
			mono_field_static_set_value (vtable, f, val);
			g_free (val);
		}
		break;
	}
	case CMD_TYPE_GET_OBJECT: {
		MonoObject *o = (MonoObject*)mono_type_get_object (mono_domain_get (), &klass->byval_arg);
		buffer_add_objid (buf, o);
		break;
	}
	case CMD_TYPE_GET_SOURCE_FILES: {
		gpointer iter = NULL;
		MonoMethod *method;
		char *source_file, *base;
		GPtrArray *files;
		int i;

		files = g_ptr_array_new ();

		while ((method = mono_class_get_methods (klass, &iter))) {
			MonoDebugMethodInfo *minfo = mono_debug_lookup_method (method);

			if (minfo) {
				mono_debug_symfile_get_line_numbers (minfo, &source_file, NULL, NULL, NULL);

				for (i = 0; i < files->len; ++i)
					if (!strcmp (g_ptr_array_index (files, i), source_file))
						break;
				if (i == files->len)
					g_ptr_array_add (files, g_strdup (source_file));
				g_free (source_file);
			}
		}

		buffer_add_int (buf, files->len);
		for (i = 0; i < files->len; ++i) {
			source_file = g_ptr_array_index (files, i);
			base = g_path_get_basename (source_file);
			buffer_add_string (buf, base);
			g_free (base);
			g_free (source_file);
		}
		g_ptr_array_free (files, TRUE);
		break;
	}
	case CMD_TYPE_IS_ASSIGNABLE_FROM: {
		MonoClass *oklass = decode_typeid (p, &p, end, NULL, &err);

		if (err)
			return err;
		if (mono_class_is_assignable_from (klass, oklass))
			buffer_add_byte (buf, 1);
		else
			buffer_add_byte (buf, 0);
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
	int err;
	MonoDomain *domain;
	MonoMethod *method;
	MonoMethodHeader *header;

	method = decode_methodid (p, &p, end, &domain, &err);
	if (err)
		return err;

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
		MonoDebugMethodInfo *minfo;
		char *source_file;
		int i, n_il_offsets;
		int *il_offsets;
		int *line_numbers;

		header = mono_method_get_header (method);
		if (!header) {
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
			break;
		}

		mono_debug_symfile_get_line_numbers (minfo, &source_file, &n_il_offsets, &il_offsets, &line_numbers);
		buffer_add_int (buf, header->code_size);
		buffer_add_string (buf, source_file);
		buffer_add_int (buf, n_il_offsets);
		//printf ("Line number table for method %s:\n", mono_method_full_name (method,  TRUE));
		for (i = 0; i < n_il_offsets; ++i) {
			//printf ("IL%d -> %d\n", il_offsets [i], line_numbers [i]);
			buffer_add_int (buf, il_offsets [i]);
			buffer_add_int (buf, line_numbers [i]);
		}
		g_free (source_file);
		g_free (il_offsets);
		g_free (line_numbers);
		break;
	}
	case CMD_METHOD_GET_PARAM_INFO: {
		MonoMethodSignature *sig = mono_method_signature (method);
		guint32 i;
		char **names;

		/* FIXME: mono_class_from_mono_type () and byrefs */

		/* FIXME: Use a smaller encoding */
		buffer_add_int (buf, sig->call_convention);
		buffer_add_int (buf, sig->param_count);
		buffer_add_int (buf, sig->generic_param_count);
		buffer_add_typeid (buf, domain, mono_class_from_mono_type (sig->ret));
		for (i = 0; i < sig->param_count; ++i) {
			/* FIXME: vararg */
			buffer_add_typeid (buf, domain, mono_class_from_mono_type (sig->params [i]));
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
		int i, j, num_locals;
		char **local_names;
		int *local_indexes;

		header = mono_method_get_header (method);
		g_assert (header);

		buffer_add_int (buf, header->num_locals);

		/* Types */
		for (i = 0; i < header->num_locals; ++i)
			buffer_add_typeid (buf, domain, mono_class_from_mono_type (header->locals [i]));

		/* Names */
		num_locals = mono_debug_lookup_locals (method, &local_names, &local_indexes);
		for (i = 0; i < header->num_locals; ++i) {
			for (j = 0; j < num_locals; ++j)
				if (local_indexes [j] == i)
					break;
			if (j < num_locals)
				buffer_add_string (buf, local_names [j]);
			else
				buffer_add_string (buf, "");
		}
		g_free (local_names);
		g_free (local_indexes);

		/* Live ranges */
		/* FIXME: This works because we set debug_options.mdb_optimizations */
		for (i = 0; i < header->num_locals; ++i) {
			buffer_add_int (buf, 0);
			buffer_add_int (buf, header->code_size);
		}

		break;
	}
	case CMD_METHOD_GET_INFO:
		buffer_add_int (buf, method->flags);
		buffer_add_int (buf, method->iflags);
		buffer_add_int (buf, method->token);
		break;
	case CMD_METHOD_GET_BODY: {
		int i;

		header = mono_method_get_header (method);
		if (!header) {
			buffer_add_int (buf, 0);
		} else {
			buffer_add_int (buf, header->code_size);
			for (i = 0; i < header->code_size; ++i)
				buffer_add_byte (buf, header->code [i]);
		}
		break;
	}
	case CMD_METHOD_RESOLVE_TOKEN: {
		guint32 token = decode_int (p, &p, end);

		// FIXME: Generics
		switch (mono_metadata_token_code (token)) {
		case MONO_TOKEN_STRING: {
			MonoString *s;
			char *s2;

			s = mono_ldstr (domain, method->klass->image, mono_metadata_token_index (token));
			g_assert (s);

			s2 = mono_string_to_utf8 (s);

			buffer_add_byte (buf, TOKEN_TYPE_STRING);
			buffer_add_string (buf, s2);
			g_free (s2);
			break;
		}
		default: {
			gpointer val;
			MonoClass *handle_class;

			if (method->wrapper_type == MONO_WRAPPER_DYNAMIC_METHOD) {
				val = mono_method_get_wrapper_data (method, token);
				handle_class = mono_method_get_wrapper_data (method, token + 1);

				if (handle_class == NULL) {
					// Can't figure out the token type
					buffer_add_byte (buf, TOKEN_TYPE_UNKNOWN);
					break;
				}
			} else {
				val = mono_ldtoken (method->klass->image, token, &handle_class, NULL);
				g_assert (val);
			}

			if (handle_class == mono_defaults.typehandle_class) {
				buffer_add_byte (buf, TOKEN_TYPE_TYPE);
				buffer_add_typeid (buf, domain, mono_class_from_mono_type ((MonoType*)val));
			} else if (handle_class == mono_defaults.fieldhandle_class) {
				buffer_add_byte (buf, TOKEN_TYPE_FIELD);
				buffer_add_fieldid (buf, domain, val);
			} else if (handle_class == mono_defaults.methodhandle_class) {
				buffer_add_byte (buf, TOKEN_TYPE_METHOD);
				buffer_add_methodid (buf, domain, val);
			} else if (handle_class == mono_defaults.string_class) {
				char *s;

				s = mono_string_to_utf8 (val);
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
	default:
		return ERR_NOT_IMPLEMENTED;
	}

	return ERR_NONE;
}

static ErrorCode
thread_commands (int command, guint8 *p, guint8 *end, Buffer *buf)
{
	int objid = decode_objid (p, &p, end);
	int err;
	MonoThread *thread_obj;
	MonoInternalThread *thread;

	err = get_object (objid, (MonoObject**)&thread_obj);
	if (err)
		return err;

	thread = THREAD_TO_INTERNAL (thread_obj);
	   
	switch (command) {
	case CMD_THREAD_GET_NAME: {
		guint32 name_len;
		gunichar2 *s = mono_thread_get_name (thread, &name_len);

		if (!s) {
			buffer_add_int (buf, 0);
		} else {
			char *name;
			glong len;

			name = g_utf16_to_utf8 (s, name_len, NULL, &len, NULL);
			g_assert (name);
			buffer_add_int (buf, len);
			buffer_add_data (buf, (guint8*)name, len);
			g_free (s);
		}
		break;
	}
	case CMD_THREAD_GET_FRAME_INFO: {
		DebuggerTlsData *tls;
		int i, start_frame, length;

		// Wait for suspending if it already started
		if (suspend_count)
			wait_for_suspend ();
		if (!is_suspended ())
			return ERR_NOT_SUSPENDED;

		start_frame = decode_int (p, &p, end);
		length = decode_int (p, &p, end);

		if (start_frame != 0 || length != -1)
			return ERR_NOT_IMPLEMENTED;

		mono_loader_lock ();
		tls = mono_g_hash_table_lookup (thread_to_tls, thread);
		mono_loader_unlock ();
		g_assert (tls);

		compute_frame_info (thread, tls);

		buffer_add_int (buf, tls->frame_count);
		for (i = 0; i < tls->frame_count; ++i) {
			buffer_add_int (buf, tls->frames [i]->id);
			buffer_add_methodid (buf, tls->frames [i]->domain, tls->frames [i]->method);
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
	default:
		return ERR_NOT_IMPLEMENTED;
	}

	return ERR_NONE;
}

static ErrorCode
frame_commands (int command, guint8 *p, guint8 *end, Buffer *buf)
{
	int objid;
	int err;
	MonoThread *thread_obj;
	MonoInternalThread *thread;
	int pos, i, len;
	DebuggerTlsData *tls;
	StackFrame *frame;
	MonoDebugMethodJitInfo *jit;
	MonoDebugVarInfo *var;
	MonoMethodSignature *sig;
	gssize id;
	MonoMethodHeader *header;

	objid = decode_objid (p, &p, end);
	err = get_object (objid, (MonoObject**)&thread_obj);
	if (err)
		return err;

	thread = THREAD_TO_INTERNAL (thread_obj);

	id = decode_id (p, &p, end);

	mono_loader_lock ();
	tls = mono_g_hash_table_lookup (thread_to_tls, thread);
	mono_loader_unlock ();
	g_assert (tls);

	for (i = 0; i < tls->frame_count; ++i) {
		if (tls->frames [i]->id == id)
			break;
	}
	if (i == tls->frame_count)
		return ERR_INVALID_FRAMEID;

	frame = tls->frames [i];

	if (!frame->has_ctx)
		// FIXME:
		return ERR_INVALID_FRAMEID;

	if (!frame->jit) {
		frame->jit = mono_debug_find_method (frame->method, frame->domain);
		g_assert (frame->jit);
	}
	jit = frame->jit;

	sig = mono_method_signature (frame->method);

	switch (command) {
	case CMD_STACK_FRAME_GET_VALUES: {
		len = decode_int (p, &p, end);
		header = mono_method_get_header (frame->method);

		for (i = 0; i < len; ++i) {
			pos = decode_int (p, &p, end);

			if (pos < 0) {
				pos = - pos - 1;

				g_assert (pos >= 0 && pos < jit->num_params);

				var = &jit->params [pos];

				add_var (buf, sig->params [pos], &jit->params [pos], &frame->ctx, frame->domain, FALSE);
			} else {
				g_assert (pos >= 0 && pos < jit->num_locals);

				var = &jit->locals [pos];
				
				add_var (buf, header->locals [pos], &jit->locals [pos], &frame->ctx, frame->domain, FALSE);
			}
		}
		break;
	}
	case CMD_STACK_FRAME_GET_THIS: {
		if (frame->method->klass->valuetype) {
			if (!sig->hasthis) {
				MonoObject *p = NULL;
				buffer_add_value (buf, &mono_defaults.object_class->byval_arg, &p, frame->domain);
			} else {
				add_var (buf, &frame->method->klass->this_arg, jit->this_var, &frame->ctx, frame->domain, TRUE);
			}
		} else {
			if (!sig->hasthis) {
				MonoObject *p = NULL;
				buffer_add_value (buf, &frame->method->klass->byval_arg, &p, frame->domain);
			} else {
				add_var (buf, &frame->method->klass->byval_arg, jit->this_var, &frame->ctx, frame->domain, TRUE);
			}
		}
		break;
	}
	case CMD_STACK_FRAME_SET_VALUES: {
		guint8 *val_buf;
		MonoType *t;
		MonoDebugVarInfo *var;

		len = decode_int (p, &p, end);
		header = mono_method_get_header (frame->method);

		for (i = 0; i < len; ++i) {
			pos = decode_int (p, &p, end);

			if (pos < 0) {
				pos = - pos - 1;

				g_assert (pos >= 0 && pos < jit->num_params);

				t = sig->params [pos];
				var = &jit->params [pos];
			} else {
				g_assert (pos >= 0 && pos < jit->num_locals);

				t = header->locals [pos];
				var = &jit->locals [pos];
			}

			if (MONO_TYPE_IS_REFERENCE (t))
				val_buf = g_alloca (sizeof (MonoObject*));
			else
				val_buf = g_alloca (mono_class_instance_size (mono_class_from_mono_type (t)));
			err = decode_value (t, frame->domain, val_buf, p, &p, end);
			if (err)
				return err;

			set_var (t, var, &frame->ctx, frame->domain, val_buf);
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
	int objid, err, index, len, i, esize;
	gpointer elem;

	objid = decode_objid (p, &p, end);
	err = get_object (objid, (MonoObject**)&arr);
	if (err)
		return err;

	switch (command) {
	case CMD_ARRAY_REF_GET_LENGTH:
		buffer_add_int (buf, arr->obj.vtable->klass->rank);
		if (!arr->bounds) {
			buffer_add_int (buf, arr->max_length);
			buffer_add_int (buf, 0);
		} else {
			for (i = 0; i < arr->obj.vtable->klass->rank; ++i) {
				buffer_add_int (buf, arr->bounds [i].length);
				buffer_add_int (buf, arr->bounds [i].lower_bound);
			}
		}
		break;
	case CMD_ARRAY_REF_GET_VALUES:
		index = decode_int (p, &p, end);
		len = decode_int (p, &p, end);

		g_assert (index >= 0 && len >= 0);
		// Reordered to avoid integer overflow
		g_assert (!(index > arr->max_length - len));

		esize = mono_array_element_size (arr->obj.vtable->klass);
		for (i = index; i < index + len; ++i) {
			elem = (gpointer*)((char*)arr->vector + (i * esize));
			buffer_add_value (buf, &arr->obj.vtable->klass->element_class->byval_arg, elem, arr->obj.vtable->domain);
		}
		break;
	case CMD_ARRAY_REF_SET_VALUES:
		index = decode_int (p, &p, end);
		len = decode_int (p, &p, end);

		g_assert (index >= 0 && len >= 0);
		// Reordered to avoid integer overflow
		g_assert (!(index > arr->max_length - len));

		esize = mono_array_element_size (arr->obj.vtable->klass);
		for (i = index; i < index + len; ++i) {
			elem = (gpointer*)((char*)arr->vector + (i * esize));

			decode_value (&arr->obj.vtable->klass->element_class->byval_arg, arr->obj.vtable->domain, elem, p, &p, end);
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
	int objid, err;
	MonoString *str;
	char *s;

	objid = decode_objid (p, &p, end);
	err = get_object (objid, (MonoObject**)&str);
	if (err)
		return err;

	switch (command) {
	case CMD_STRING_REF_GET_VALUE:
		s = mono_string_to_utf8 (str);
		buffer_add_string (buf, s);
		g_free (s);
		break;
	default:
		return ERR_NOT_IMPLEMENTED;
	}

	return ERR_NONE;
}

static ErrorCode
object_commands (int command, guint8 *p, guint8 *end, Buffer *buf)
{
	int objid, err;
	MonoObject *obj;
	int len, i;
	MonoClassField *f;
	MonoClass *k;
	gboolean found;

	if (command == CMD_OBJECT_REF_IS_COLLECTED) {
		objid = decode_objid (p, &p, end);
		err = get_object (objid, &obj);
		if (err)
			buffer_add_int (buf, 1);
		else
			buffer_add_int (buf, 0);
		return 0;
	}

	objid = decode_objid (p, &p, end);
	err = get_object (objid, &obj);
	if (err)
		return err;

	switch (command) {
	case CMD_OBJECT_REF_GET_TYPE:
		buffer_add_typeid (buf, obj->vtable->domain, obj->vtable->klass);
		break;
	case CMD_OBJECT_REF_GET_VALUES:
		len = decode_int (p, &p, end);

		for (i = 0; i < len; ++i) {
			MonoClassField *f = decode_fieldid (p, &p, end, NULL, &err);
			if (err)
				return err;

			/* Check that the field belongs to the object */
			found = FALSE;
			for (k = obj->vtable->klass; k; k = k->parent) {
				if (k == f->parent) {
					found = TRUE;
					break;
				}
			}
			if (!found)
				return ERR_INVALID_FIELDID;

			if (f->type->attrs & FIELD_ATTRIBUTE_STATIC) {
				guint8 *val;
				MonoVTable *vtable;

				if (mono_class_field_is_special_static (f))
					return ERR_INVALID_FIELDID;

				g_assert (f->type->attrs & FIELD_ATTRIBUTE_STATIC);
				vtable = mono_class_vtable (obj->vtable->domain, f->parent);
				val = g_malloc (mono_class_instance_size (mono_class_from_mono_type (f->type)));
				mono_field_static_get_value (vtable, f, val);
				buffer_add_value (buf, f->type, val, obj->vtable->domain);
				g_free (val);
			} else {
				buffer_add_value (buf, f->type, (guint8*)obj + f->offset, obj->vtable->domain);
			}
		}
		break;
	case CMD_OBJECT_REF_SET_VALUES:
		len = decode_int (p, &p, end);

		for (i = 0; i < len; ++i) {
			f = decode_fieldid (p, &p, end, NULL, &err);
			if (err)
				return err;

			/* Check that the field belongs to the object */
			found = FALSE;
			for (k = obj->vtable->klass; k; k = k->parent) {
				if (k == f->parent) {
					found = TRUE;
					break;
				}
			}
			if (!found)
				return ERR_INVALID_FIELDID;

			if (f->type->attrs & FIELD_ATTRIBUTE_STATIC) {
				guint8 *val;
				MonoVTable *vtable;

				if (mono_class_field_is_special_static (f))
					return ERR_INVALID_FIELDID;

				g_assert (f->type->attrs & FIELD_ATTRIBUTE_STATIC);
				vtable = mono_class_vtable (obj->vtable->domain, f->parent);

				val = g_malloc (mono_class_instance_size (mono_class_from_mono_type (f->type)));
				err = decode_value (f->type, obj->vtable->domain, val, p, &p, end);
				if (err) {
					g_free (val);
					return err;
				}
				mono_field_static_set_value (vtable, f, val);
				g_free (val);
			} else {
				err = decode_value (f->type, obj->vtable->domain, (guint8*)obj + f->offset, p, &p, end);
				if (err)
					return err;
			}
		}
		break;
	case CMD_OBJECT_REF_GET_ADDRESS:
		buffer_add_long (buf, (gssize)obj);
		break;
	case CMD_OBJECT_REF_GET_DOMAIN:
		buffer_add_domainid (buf, obj->vtable->domain);
		break;
	default:
		return ERR_NOT_IMPLEMENTED;
	}

	return ERR_NONE;
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
	case CMD_SET_EVENT:
		return "EVENT"; 
	default:
		return "";
	}
}

/*
 * debugger_thread:
 *
 *   This thread handles communication with the debugger client using a JDWP
 * like protocol.
 */
static guint32 WINAPI
debugger_thread (void *arg)
{
	int res, len, id, flags, command_set, command;
	guint8 header [HEADER_LENGTH];
	guint8 *data, *p, *end;
	Buffer buf;
	ErrorCode err;
	gboolean no_reply;

	DEBUG (1, fprintf (log_file, "[dbg] Agent thread started, pid=%p\n", (gpointer)GetCurrentThreadId ()));

	debugger_thread_id = GetCurrentThreadId ();

	mono_jit_thread_attach (mono_get_root_domain ());

	mono_thread_internal_current ()->flags |= MONO_THREAD_FLAG_DONT_MANAGE;

	mono_set_is_debugger_attached (TRUE);

	while (TRUE) {
		res = recv (conn_fd, header, HEADER_LENGTH, 0);

		/* This will break if the socket is closed during shutdown too */
		if (res != HEADER_LENGTH)
			break;

		p = header;
		end = header + HEADER_LENGTH;

		len = decode_int (p, &p, end);
		id = decode_int (p, &p, end);
		flags = decode_byte (p, &p, end);
		command_set = decode_byte (p, &p, end);
		command = decode_byte (p, &p, end);

		g_assert (flags == 0);

		DEBUG (1, fprintf (log_file, "[dbg] Received command %s(%d), id=%d.\n", command_set_to_string (command_set), command, id));

		data = g_malloc (len - HEADER_LENGTH);
		if (len - HEADER_LENGTH > 0)
		{
			res = recv (conn_fd, data, len - HEADER_LENGTH, 0);
			if (res != len - HEADER_LENGTH)
				break;
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
			if (!err && command == CMD_VM_INVOKE_METHOD)
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
		case CMD_SET_OBJECT_REF:
			err = object_commands (command, p, end, &buf);
			break;
		default:
			err = ERR_NOT_IMPLEMENTED;
		}		

		if (!no_reply)
			send_reply_packet (id, err, &buf);

		g_free (data);
		buffer_free (&buf);

		if (command_set == CMD_SET_VM && command == CMD_VM_DISPOSE)
			break;
	}

	mono_set_is_debugger_attached (FALSE);

	mono_mutex_lock (&debugger_thread_exited_mutex);
	debugger_thread_exited = TRUE;
	mono_cond_signal (&debugger_thread_exited_cond);
	mono_mutex_unlock (&debugger_thread_exited_mutex);

	DEBUG (1, printf ("[dbg] Debugger thread exited.\n"));

	return 0;
}

#else /* DISABLE_DEBUGGER_AGENT */

void
mono_debugger_agent_parse_options (char *options)
{
	g_error ("This runtime is configure with the debugger agent disabled.");
}

void
mono_debugger_agent_init (void)
{
}

void
mono_debugger_agent_breakpoint_hit (void *sigctx)
{
}

void
mono_debugger_agent_single_step_event (void *sigctx)
{
}

void
mono_debugger_agent_free_domain_info (MonoDomain *domain)
{
}

gboolean
mono_debugger_agent_thread_interrupt (void *sigctx, MonoJitInfo *ji)
{
	return FALSE;
}

void
mono_debugger_agent_handle_exception (MonoException *ext, MonoContext *ctx)
{
}

void
mono_debugger_agent_handle_unhandled_exception (MonoException *exc, MonoContext *ctx)
{
	
}
#endif

