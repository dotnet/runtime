/**
 * \file
 */

#ifndef __MONO_DEBUGGER_ENGINE_H__
#define __MONO_DEBUGGER_ENGINE_H__

#include "mini.h"
#include <mono/metadata/seq-points-data.h>
#include <mono/mini/debugger-state-machine.h>
#include <mono/metadata/mono-debug.h>
#include <mono/mini/interp/interp-internals.h>
#include <mono/mini/debugger-protocol.h>

#define ModifierKind MdbgProtModifierKind
#define StepDepth MdbgProtStepDepth
#define StepSize MdbgProtStepSize
#define StepFilter MdbgProtStepFilter
#define EventKind MdbgProtEventKind
#define CommandSet MdbgProtCommandSet

#define EVENT_KIND_BREAKPOINT MDBGPROT_EVENT_KIND_BREAKPOINT
#define EVENT_KIND_STEP MDBGPROT_EVENT_KIND_STEP
#define EVENT_KIND_KEEPALIVE MDBGPROT_EVENT_KIND_KEEPALIVE
#define EVENT_KIND_METHOD_ENTRY MDBGPROT_EVENT_KIND_METHOD_ENTRY
#define EVENT_KIND_METHOD_EXIT MDBGPROT_EVENT_KIND_METHOD_EXIT
#define EVENT_KIND_APPDOMAIN_CREATE MDBGPROT_EVENT_KIND_APPDOMAIN_CREATE
#define EVENT_KIND_APPDOMAIN_UNLOAD MDBGPROT_EVENT_KIND_APPDOMAIN_UNLOAD
#define EVENT_KIND_THREAD_START MDBGPROT_EVENT_KIND_THREAD_START
#define EVENT_KIND_THREAD_DEATH MDBGPROT_EVENT_KIND_THREAD_DEATH
#define EVENT_KIND_ASSEMBLY_LOAD MDBGPROT_EVENT_KIND_ASSEMBLY_LOAD
#define EVENT_KIND_ASSEMBLY_UNLOAD MDBGPROT_EVENT_KIND_ASSEMBLY_UNLOAD
#define EVENT_KIND_TYPE_LOAD MDBGPROT_EVENT_KIND_TYPE_LOAD
#define EVENT_KIND_VM_START MDBGPROT_EVENT_KIND_VM_START
#define EVENT_KIND_VM_DEATH MDBGPROT_EVENT_KIND_VM_DEATH
#define EVENT_KIND_CRASH MDBGPROT_EVENT_KIND_CRASH
#define EVENT_KIND_EXCEPTION MDBGPROT_EVENT_KIND_EXCEPTION
#define EVENT_KIND_USER_BREAK MDBGPROT_EVENT_KIND_USER_BREAK
#define EVENT_KIND_USER_LOG MDBGPROT_EVENT_KIND_USER_LOG
#define CMD_EVENT_REQUEST_SET MDBGPROT_CMD_EVENT_REQUEST_SET
#define CMD_EVENT_REQUEST_CLEAR MDBGPROT_CMD_EVENT_REQUEST_CLEAR
#define CMD_EVENT_REQUEST_CLEAR_ALL_BREAKPOINTS MDBGPROT_CMD_EVENT_REQUEST_CLEAR_ALL_BREAKPOINTS

#define CMD_VM_VERSION MDBGPROT_CMD_VM_VERSION
#define CMD_VM_SET_PROTOCOL_VERSION MDBGPROT_CMD_VM_SET_PROTOCOL_VERSION
#define CMD_VM_ALL_THREADS MDBGPROT_CMD_VM_ALL_THREADS
#define CMD_VM_SUSPEND MDBGPROT_CMD_VM_SUSPEND
#define CMD_VM_RESUME MDBGPROT_CMD_VM_RESUME
#define CMD_VM_DISPOSE MDBGPROT_CMD_VM_DISPOSE
#define CMD_VM_EXIT MDBGPROT_CMD_VM_EXIT
#define CMD_VM_INVOKE_METHOD MDBGPROT_CMD_VM_INVOKE_METHOD
#define CMD_VM_INVOKE_METHODS MDBGPROT_CMD_VM_INVOKE_METHODS
#define CMD_VM_ABORT_INVOKE MDBGPROT_CMD_VM_ABORT_INVOKE
#define CMD_VM_SET_KEEPALIVE MDBGPROT_CMD_VM_SET_KEEPALIVE
#define CMD_VM_GET_TYPES_FOR_SOURCE_FILE MDBGPROT_CMD_VM_GET_TYPES_FOR_SOURCE_FILE
#define CMD_VM_GET_TYPES MDBGPROT_CMD_VM_GET_TYPES
#define CMD_VM_START_BUFFERING MDBGPROT_CMD_VM_START_BUFFERING
#define CMD_VM_STOP_BUFFERING MDBGPROT_CMD_VM_STOP_BUFFERING

#define CMD_APPDOMAIN_GET_ROOT_DOMAIN MDBGPROT_CMD_APPDOMAIN_GET_ROOT_DOMAIN
#define CMD_APPDOMAIN_GET_FRIENDLY_NAME MDBGPROT_CMD_APPDOMAIN_GET_FRIENDLY_NAME
#define CMD_APPDOMAIN_GET_ASSEMBLIES MDBGPROT_CMD_APPDOMAIN_GET_ASSEMBLIES
#define CMD_APPDOMAIN_GET_ENTRY_ASSEMBLY MDBGPROT_CMD_APPDOMAIN_GET_ENTRY_ASSEMBLY
#define CMD_APPDOMAIN_GET_CORLIB MDBGPROT_CMD_APPDOMAIN_GET_CORLIB
#define CMD_APPDOMAIN_CREATE_STRING MDBGPROT_CMD_APPDOMAIN_CREATE_STRING
#define CMD_APPDOMAIN_CREATE_BYTE_ARRAY MDBGPROT_CMD_APPDOMAIN_CREATE_BYTE_ARRAY
#define CMD_APPDOMAIN_CREATE_BOXED_VALUE MDBGPROT_CMD_APPDOMAIN_CREATE_BOXED_VALUE

#define CMD_ASSEMBLY_GET_LOCATION MDBGPROT_CMD_ASSEMBLY_GET_LOCATION
#define CMD_ASSEMBLY_GET_ENTRY_POINT MDBGPROT_CMD_ASSEMBLY_GET_ENTRY_POINT
#define CMD_ASSEMBLY_GET_MANIFEST_MODULE MDBGPROT_CMD_ASSEMBLY_GET_MANIFEST_MODULE
#define CMD_ASSEMBLY_GET_OBJECT MDBGPROT_CMD_ASSEMBLY_GET_OBJECT
#define CMD_ASSEMBLY_GET_DOMAIN MDBGPROT_CMD_ASSEMBLY_GET_DOMAIN
#define CMD_ASSEMBLY_GET_TYPE MDBGPROT_CMD_ASSEMBLY_GET_TYPE
#define CMD_ASSEMBLY_GET_NAME MDBGPROT_CMD_ASSEMBLY_GET_NAME
#define CMD_ASSEMBLY_GET_METADATA_BLOB MDBGPROT_CMD_ASSEMBLY_GET_METADATA_BLOB
#define CMD_ASSEMBLY_GET_IS_DYNAMIC MDBGPROT_CMD_ASSEMBLY_GET_IS_DYNAMIC
#define CMD_ASSEMBLY_GET_PDB_BLOB MDBGPROT_CMD_ASSEMBLY_GET_PDB_BLOB
#define CMD_ASSEMBLY_GET_TYPE_FROM_TOKEN MDBGPROT_CMD_ASSEMBLY_GET_TYPE_FROM_TOKEN
#define CMD_ASSEMBLY_GET_METHOD_FROM_TOKEN MDBGPROT_CMD_ASSEMBLY_GET_METHOD_FROM_TOKEN
#define CMD_ASSEMBLY_HAS_DEBUG_INFO MDBGPROT_CMD_ASSEMBLY_HAS_DEBUG_INFO
#define CMD_ASSEMBLY_GET_CATTRS MDBGPROT_CMD_ASSEMBLY_GET_CATTRS

#define CMD_MODULE_GET_INFO MDBGPROT_CMD_MODULE_GET_INFO

#define CMD_FIELD_GET_INFO MDBGPROT_CMD_FIELD_GET_INFO

#define CMD_TYPE_GET_INFO MDBGPROT_CMD_TYPE_GET_INFO
#define CMD_TYPE_GET_METHODS MDBGPROT_CMD_TYPE_GET_METHODS
#define CMD_TYPE_GET_FIELDS MDBGPROT_CMD_TYPE_GET_FIELDS
#define CMD_TYPE_GET_PROPERTIES MDBGPROT_CMD_TYPE_GET_PROPERTIES
#define CMD_TYPE_GET_CATTRS MDBGPROT_CMD_TYPE_GET_CATTRS
#define CMD_TYPE_GET_FIELD_CATTRS MDBGPROT_CMD_TYPE_GET_FIELD_CATTRS
#define CMD_TYPE_GET_PROPERTY_CATTRS MDBGPROT_CMD_TYPE_GET_PROPERTY_CATTRS
#define CMD_TYPE_GET_VALUES MDBGPROT_CMD_TYPE_GET_VALUES
#define CMD_TYPE_GET_VALUES_2 MDBGPROT_CMD_TYPE_GET_VALUES_2
#define CMD_TYPE_SET_VALUES MDBGPROT_CMD_TYPE_SET_VALUES
#define CMD_TYPE_GET_OBJECT MDBGPROT_CMD_TYPE_GET_OBJECT
#define CMD_TYPE_GET_SOURCE_FILES MDBGPROT_CMD_TYPE_GET_SOURCE_FILES
#define CMD_TYPE_GET_SOURCE_FILES_2 MDBGPROT_CMD_TYPE_GET_SOURCE_FILES_2
#define CMD_TYPE_IS_ASSIGNABLE_FROM MDBGPROT_CMD_TYPE_IS_ASSIGNABLE_FROM
#define CMD_TYPE_GET_METHODS_BY_NAME_FLAGS MDBGPROT_CMD_TYPE_GET_METHODS_BY_NAME_FLAGS
#define CMD_TYPE_GET_INTERFACES MDBGPROT_CMD_TYPE_GET_INTERFACES
#define CMD_TYPE_GET_INTERFACE_MAP MDBGPROT_CMD_TYPE_GET_INTERFACE_MAP
#define CMD_TYPE_IS_INITIALIZED MDBGPROT_CMD_TYPE_IS_INITIALIZED
#define CMD_TYPE_CREATE_INSTANCE MDBGPROT_CMD_TYPE_CREATE_INSTANCE
#define CMD_TYPE_GET_VALUE_SIZE MDBGPROT_CMD_TYPE_GET_VALUE_SIZE

#define CMD_METHOD_GET_NAME MDBGPROT_CMD_METHOD_GET_NAME
#define CMD_METHOD_GET_DECLARING_TYPE MDBGPROT_CMD_METHOD_GET_DECLARING_TYPE 
#define CMD_METHOD_GET_DEBUG_INFO MDBGPROT_CMD_METHOD_GET_DEBUG_INFO
#define CMD_METHOD_GET_PARAM_INFO MDBGPROT_CMD_METHOD_GET_PARAM_INFO
#define CMD_METHOD_GET_LOCALS_INFO MDBGPROT_CMD_METHOD_GET_LOCALS_INFO
#define CMD_METHOD_GET_INFO MDBGPROT_CMD_METHOD_GET_INFO
#define CMD_METHOD_GET_BODY MDBGPROT_CMD_METHOD_GET_BODY
#define CMD_METHOD_RESOLVE_TOKEN MDBGPROT_CMD_METHOD_RESOLVE_TOKEN
#define CMD_METHOD_GET_CATTRS MDBGPROT_CMD_METHOD_GET_CATTRS
#define CMD_METHOD_MAKE_GENERIC_METHOD MDBGPROT_CMD_METHOD_MAKE_GENERIC_METHOD
#define CMD_METHOD_TOKEN MDBGPROT_CMD_METHOD_TOKEN
#define CMD_METHOD_ASSEMBLY MDBGPROT_CMD_METHOD_ASSEMBLY

#define CMD_THREAD_GET_NAME MDBGPROT_CMD_THREAD_GET_NAME
#define CMD_THREAD_GET_FRAME_INFO MDBGPROT_CMD_THREAD_GET_FRAME_INFO
#define CMD_THREAD_GET_STATE MDBGPROT_CMD_THREAD_GET_STATE
#define CMD_THREAD_GET_INFO MDBGPROT_CMD_THREAD_GET_INFO
#define CMD_THREAD_GET_ID MDBGPROT_CMD_THREAD_GET_ID
#define CMD_THREAD_GET_TID MDBGPROT_CMD_THREAD_GET_TID
#define CMD_THREAD_SET_IP MDBGPROT_CMD_THREAD_SET_IP
#define CMD_THREAD_ELAPSED_TIME MDBGPROT_CMD_THREAD_ELAPSED_TIME

#define CMD_STACK_FRAME_GET_DOMAIN MDBGPROT_CMD_STACK_FRAME_GET_DOMAIN
#define CMD_STACK_FRAME_GET_ARGUMENT MDBGPROT_CMD_STACK_FRAME_GET_ARGUMENT
#define CMD_STACK_FRAME_GET_VALUES MDBGPROT_CMD_STACK_FRAME_GET_VALUES
#define CMD_STACK_FRAME_GET_THIS MDBGPROT_CMD_STACK_FRAME_GET_THIS
#define CMD_STACK_FRAME_SET_VALUES MDBGPROT_CMD_STACK_FRAME_SET_VALUES
#define CMD_STACK_FRAME_GET_DOMAIN MDBGPROT_CMD_STACK_FRAME_GET_DOMAIN
#define CMD_STACK_FRAME_SET_THIS MDBGPROT_CMD_STACK_FRAME_SET_THIS

#define CMD_ARRAY_REF_GET_TYPE MDBGPROT_CMD_ARRAY_REF_GET_TYPE
#define CMD_ARRAY_REF_GET_LENGTH MDBGPROT_CMD_ARRAY_REF_GET_LENGTH
#define CMD_ARRAY_REF_GET_VALUES MDBGPROT_CMD_ARRAY_REF_GET_VALUES
#define CMD_ARRAY_REF_SET_VALUES MDBGPROT_CMD_ARRAY_REF_SET_VALUES

#define CMD_STRING_REF_GET_VALUE MDBGPROT_CMD_STRING_REF_GET_VALUE
#define CMD_STRING_REF_GET_LENGTH MDBGPROT_CMD_STRING_REF_GET_LENGTH
#define CMD_STRING_REF_GET_CHARS MDBGPROT_CMD_STRING_REF_GET_CHARS

#define CMD_POINTER_GET_VALUE MDBGPROT_CMD_POINTER_GET_VALUE

#define CMD_OBJECT_REF_IS_COLLECTED MDBGPROT_CMD_OBJECT_REF_IS_COLLECTED
#define CMD_OBJECT_REF_GET_TYPE MDBGPROT_CMD_OBJECT_REF_GET_TYPE
#define CMD_OBJECT_REF_GET_VALUES_ICORDBG MDBGPROT_CMD_OBJECT_REF_GET_VALUES_ICORDBG
#define CMD_OBJECT_REF_GET_VALUES MDBGPROT_CMD_OBJECT_REF_GET_VALUES
#define CMD_OBJECT_REF_SET_VALUES MDBGPROT_CMD_OBJECT_REF_SET_VALUES
#define CMD_OBJECT_REF_GET_ADDRESS MDBGPROT_CMD_OBJECT_REF_GET_ADDRESS
#define CMD_OBJECT_REF_GET_DOMAIN MDBGPROT_CMD_OBJECT_REF_GET_DOMAIN
#define CMD_OBJECT_REF_GET_INFO MDBGPROT_CMD_OBJECT_REF_GET_INFO

#define TOKEN_TYPE_METHOD MDBGPROT_TOKEN_TYPE_METHOD
#define TOKEN_TYPE_UNKNOWN MDBGPROT_TOKEN_TYPE_UNKNOWN
#define TOKEN_TYPE_FIELD MDBGPROT_TOKEN_TYPE_FIELD
#define TOKEN_TYPE_METHOD MDBGPROT_TOKEN_TYPE_METHOD
#define TOKEN_TYPE_STRING MDBGPROT_TOKEN_TYPE_STRING
#define TOKEN_TYPE_TYPE MDBGPROT_TOKEN_TYPE_TYPE

#define STEP_FILTER_STATIC_CTOR MDBGPROT_STEP_FILTER_STATIC_CTOR
#define STEP_DEPTH_OVER MDBGPROT_STEP_DEPTH_OVER
#define STEP_DEPTH_OUT MDBGPROT_STEP_DEPTH_OUT
#define STEP_DEPTH_INTO MDBGPROT_STEP_DEPTH_INTO
#define STEP_SIZE_MIN MDBGPROT_STEP_SIZE_MIN
#define STEP_SIZE_LINE MDBGPROT_STEP_SIZE_LINE

#define SUSPEND_POLICY_NONE MDBGPROT_SUSPEND_POLICY_NONE
#define SUSPEND_POLICY_ALL MDBGPROT_SUSPEND_POLICY_ALL
#define SUSPEND_POLICY_EVENT_THREAD MDBGPROT_SUSPEND_POLICY_EVENT_THREAD

#define CMD_COMPOSITE MDBGPROT_CMD_COMPOSITE

#define INVOKE_FLAG_SINGLE_THREADED MDBGPROT_INVOKE_FLAG_SINGLE_THREADED
#define INVOKE_FLAG_VIRTUAL MDBGPROT_INVOKE_FLAG_VIRTUAL
#define INVOKE_FLAG_DISABLE_BREAKPOINTS MDBGPROT_INVOKE_FLAG_DISABLE_BREAKPOINTS
#define INVOKE_FLAG_RETURN_OUT_THIS MDBGPROT_INVOKE_FLAG_RETURN_OUT_THIS
#define INVOKE_FLAG_RETURN_OUT_ARGS MDBGPROT_INVOKE_FLAG_RETURN_OUT_ARGS

#define MOD_KIND_ASSEMBLY_ONLY MDBGPROT_MOD_KIND_ASSEMBLY_ONLY
#define MOD_KIND_EXCEPTION_ONLY MDBGPROT_MOD_KIND_EXCEPTION_ONLY
#define MOD_KIND_NONE MDBGPROT_MOD_KIND_NONE
#define MOD_KIND_COUNT MDBGPROT_MOD_KIND_COUNT
#define MOD_KIND_THREAD_ONLY MDBGPROT_MOD_KIND_THREAD_ONLY
#define MOD_KIND_SOURCE_FILE_ONLY MDBGPROT_MOD_KIND_SOURCE_FILE_ONLY
#define MOD_KIND_TYPE_NAME_ONLY MDBGPROT_MOD_KIND_TYPE_NAME_ONLY
#define MOD_KIND_STEP MDBGPROT_MOD_KIND_STEP
#define MOD_KIND_LOCATION_ONLY MDBGPROT_MOD_KIND_LOCATION_ONLY


#define STEP_FILTER_DEBUGGER_HIDDEN MDBGPROT_STEP_FILTER_DEBUGGER_HIDDEN
#define STEP_FILTER_DEBUGGER_NON_USER_CODE MDBGPROT_STEP_FILTER_DEBUGGER_NON_USER_CODE
#define STEP_FILTER_DEBUGGER_STEP_THROUGH MDBGPROT_STEP_FILTER_DEBUGGER_STEP_THROUGH
#define STEP_FILTER_NONE MDBGPROT_STEP_FILTER_NONE

#define ERR_NONE MDBGPROT_ERR_NONE
#define ERR_INVOKE_ABORTED MDBGPROT_ERR_INVOKE_ABORTED
#define ERR_NOT_SUSPENDED MDBGPROT_ERR_NOT_SUSPENDED
#define ERR_INVALID_ARGUMENT MDBGPROT_ERR_INVALID_ARGUMENT
#define ERR_INVALID_OBJECT MDBGPROT_ERR_INVALID_OBJECT
#define ERR_UNLOADED MDBGPROT_ERR_UNLOADED
#define ERR_NOT_IMPLEMENTED MDBGPROT_ERR_NOT_IMPLEMENTED
#define ERR_LOADER_ERROR MDBGPROT_ERR_LOADER_ERROR
#define ERR_NO_INVOCATION MDBGPROT_ERR_NO_INVOCATION
#define ERR_NO_SEQ_POINT_AT_IL_OFFSET MDBGPROT_ERR_NO_SEQ_POINT_AT_IL_OFFSET
#define ERR_INVALID_FIELDID MDBGPROT_ERR_INVALID_FIELDID
#define ERR_INVALID_FRAMEID MDBGPROT_ERR_INVALID_FRAMEID
#define ERR_ABSENT_INFORMATION MDBGPROT_ERR_ABSENT_INFORMATION

#define VALUE_TYPE_ID_FIXED_ARRAY MDBGPROT_VALUE_TYPE_ID_FIXED_ARRAY
#define VALUE_TYPE_ID_NULL MDBGPROT_VALUE_TYPE_ID_NULL
#define VALUE_TYPE_ID_PARENT_VTYPE MDBGPROT_VALUE_TYPE_ID_PARENT_VTYPE
#define VALUE_TYPE_ID_TYPE MDBGPROT_VALUE_TYPE_ID_TYPE

#define CMD_SET_VM MDBGPROT_CMD_SET_VM
#define CMD_SET_OBJECT_REF MDBGPROT_CMD_SET_OBJECT_REF
#define CMD_SET_STRING_REF MDBGPROT_CMD_SET_STRING_REF
#define CMD_SET_THREAD MDBGPROT_CMD_SET_THREAD
#define CMD_SET_ARRAY_REF MDBGPROT_CMD_SET_ARRAY_REF
#define CMD_SET_EVENT_REQUEST MDBGPROT_CMD_SET_EVENT_REQUEST
#define CMD_SET_STACK_FRAME MDBGPROT_CMD_SET_STACK_FRAME
#define CMD_SET_APPDOMAIN MDBGPROT_CMD_SET_APPDOMAIN
#define CMD_SET_ASSEMBLY MDBGPROT_CMD_SET_ASSEMBLY
#define CMD_SET_METHOD MDBGPROT_CMD_SET_METHOD
#define CMD_SET_TYPE MDBGPROT_CMD_SET_TYPE
#define CMD_SET_MODULE MDBGPROT_CMD_SET_MODULE
#define CMD_SET_FIELD MDBGPROT_CMD_SET_FIELD
#define CMD_SET_POINTER MDBGPROT_CMD_SET_POINTER
#define CMD_SET_EVENT MDBGPROT_CMD_SET_EVENT

#define Buffer MdbgProtBuffer
#define ReplyPacket MdbgProtReplyPacket

#define buffer_init m_dbgprot_buffer_init
#define buffer_free m_dbgprot_buffer_free
#define buffer_add_int m_dbgprot_buffer_add_int
#define buffer_add_long m_dbgprot_buffer_add_long
#define buffer_add_string m_dbgprot_buffer_add_string
#define buffer_add_id m_dbgprot_buffer_add_id
#define buffer_add_byte m_dbgprot_buffer_add_byte
#define buffer_len m_dbgprot_buffer_len
#define buffer_add_buffer m_dbgprot_buffer_add_buffer
#define buffer_add_data m_dbgprot_buffer_add_data
#define buffer_add_utf16 m_dbgprot_buffer_add_utf16
#define buffer_add_byte_array m_dbgprot_buffer_add_byte_array
#define buffer_add_short m_dbgprot_buffer_add_short

#define decode_id m_dbgprot_decode_id
#define decode_int m_dbgprot_decode_int
#define decode_byte m_dbgprot_decode_byte
#define decode_long m_dbgprot_decode_long
#define decode_string m_dbgprot_decode_string

#define event_to_string m_dbgprot_event_to_string

#define ErrorCode MdbgProtErrorCode

#define FRAME_FLAG_DEBUGGER_INVOKE MDBGPROT_FRAME_FLAG_DEBUGGER_INVOKE
#define FRAME_FLAG_NATIVE_TRANSITION MDBGPROT_FRAME_FLAG_NATIVE_TRANSITION

typedef struct {
	ModifierKind kind;
	union {
		int count; /* For kind == MOD_KIND_COUNT */
		MonoInternalThread *thread; /* For kind == MOD_KIND_THREAD_ONLY */
		MonoClass *exc_class; /* For kind == MONO_KIND_EXCEPTION_ONLY */
		MonoAssembly **assemblies; /* For kind == MONO_KIND_ASSEMBLY_ONLY */
		GHashTable *source_files; /* For kind == MONO_KIND_SOURCE_FILE_ONLY */
		GHashTable *type_names; /* For kind == MONO_KIND_TYPE_NAME_ONLY */
		StepFilter filter; /* For kind == MOD_KIND_STEP */
	} data;
	gboolean caught, uncaught, subclasses, not_filtered_feature, everything_else; /* For kind == MOD_KIND_EXCEPTION_ONLY */
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
	StepFilter filter;
	gpointer last_sp;
	gpointer start_sp;
	MonoMethod *start_method;
	MonoMethod *last_method;
	int last_line;
	/* Whenever single stepping is performed using start/stop_single_stepping () */
	gboolean global;
	/* The list of breakpoints used to implement step-over */
	GSList *bps;
	/* The number of frames at the start of a step-over */
	int nframes;
	/* If set, don't stop in methods that are not part of user assemblies */
	MonoAssembly** user_assemblies;
	/* Used to distinguish stepping breakpoint hits in parallel tasks executions */
	int async_id;
	/* Used to know if we are in process of async step-out and distishing from exception breakpoints */
	MonoMethod* async_stepout_method;
	int refcount;
} SingleStepReq;


/* 
 * Contains information about an inserted breakpoint.
 */
typedef struct {
	long il_offset, native_offset;
	guint8 *ip;
	MonoJitInfo *ji;
	MonoDomain *domain;
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
	EventRequest *req;
	/* 
	 * A list of BreakpointInstance structures describing where the breakpoint
	 * was inserted. There could be more than one because of 
	 * generics/appdomains/method entry/exit.
	 */
	GPtrArray *children;
} MonoBreakpoint;

typedef struct {
	MonoJitInfo *ji;
	MonoDomain *domain;
	MonoMethod *method;
	guint32 native_offset;
} DbgEngineStackFrame;

typedef struct {
	/*
	 * Method where to start single stepping
	 */
	MonoMethod *method;

	/*
	* If ctx is set, tls must belong to the same thread.
	*/
	MonoContext *ctx;
	void *tls;

	/*
	 * Stopped at a throw site
	*/
	gboolean step_to_catch;

	/*
	 * Sequence point to start from.
	*/
	SeqPoint sp;
	MonoSeqPointInfo *info;

	/*
	 * Frame data, will be freed at the end of ss_start if provided
	 */
	DbgEngineStackFrame **frames;
	int nframes;
} SingleStepArgs;

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
	MonoGCHandle handle;
} ObjRef;

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

void mono_debugger_free_objref (gpointer value);

typedef int DbgEngineErrorCode;
#define DE_ERR_NONE 0
// WARNING WARNING WARNING
// Error codes MUST match those of sdb for now
#define DE_ERR_NOT_IMPLEMENTED 100

MonoGHashTable *
mono_debugger_get_thread_states (void);

gboolean
mono_debugger_is_disconnected (void);

gsize
mono_debugger_tls_thread_id (DebuggerTlsData *debuggerTlsData);

void
mono_debugger_set_thread_state (DebuggerTlsData *ref, MonoDebuggerThreadState expected, MonoDebuggerThreadState set);

MonoDebuggerThreadState
mono_debugger_get_thread_state (DebuggerTlsData *ref);

typedef struct {
	MonoContext *(*tls_get_restore_state) (void *tls);
	gboolean (*try_process_suspend) (void *tls, MonoContext *ctx, gboolean from_breakpoint);
	gboolean (*begin_breakpoint_processing) (void *tls, MonoContext *ctx, MonoJitInfo *ji, gboolean from_signal);
	void (*begin_single_step_processing) (MonoContext *ctx, gboolean from_signal);

	void (*ss_discard_frame_context) (void *tls);
	void (*ss_calculate_framecount) (void *tls, MonoContext *ctx, gboolean force_use_ctx, DbgEngineStackFrame ***frames, int *nframes);
	gboolean (*ensure_jit) (DbgEngineStackFrame *frame);
	int (*ensure_runtime_is_suspended) (void);

	int (*get_this_async_id) (DbgEngineStackFrame *frame);

	void* (*create_breakpoint_events) (GPtrArray *ss_reqs, GPtrArray *bp_reqs, MonoJitInfo *ji, EventKind kind);
	void (*process_breakpoint_events) (void *_evts, MonoMethod *method, MonoContext *ctx, int il_offset);

	gboolean (*set_set_notification_for_wait_completion_flag) (DbgEngineStackFrame *f);
	MonoMethod* (*get_notify_debugger_of_wait_completion_method)(void);

	int (*ss_create_init_args) (SingleStepReq *ss_req, SingleStepArgs *args);
	void (*ss_args_destroy) (SingleStepArgs *ss_args);
	int (*handle_multiple_ss_requests)(void);
} DebuggerEngineCallbacks;


void mono_de_init (DebuggerEngineCallbacks *cbs);
void mono_de_cleanup (void);
void mono_de_set_log_level (int level, FILE *file);

//locking - we expose the lock object from the debugging engine to ensure we keep the same locking semantics of sdb.
void mono_de_lock (void);
void mono_de_unlock (void);

// domain handling
void mono_de_foreach_domain (GHFunc func, gpointer user_data);
void mono_de_domain_add (MonoDomain *domain);
void mono_de_domain_remove (MonoDomain *domain);

//breakpoints
void mono_de_clear_breakpoint (MonoBreakpoint *bp);
MonoBreakpoint* mono_de_set_breakpoint (MonoMethod *method, long il_offset, EventRequest *req, MonoError *error);
void mono_de_collect_breakpoints_by_sp (SeqPoint *sp, MonoJitInfo *ji, GPtrArray *ss_reqs, GPtrArray *bp_reqs);
void mono_de_clear_breakpoints_for_domain (MonoDomain *domain);
void mono_de_add_pending_breakpoints (MonoMethod *method, MonoJitInfo *ji);
void mono_de_clear_all_breakpoints (void);
MonoBreakpoint * mono_de_get_breakpoint_by_id (int id);

//single stepping
void mono_de_start_single_stepping (void);
void mono_de_stop_single_stepping (void);

void mono_de_process_breakpoint (void *tls, gboolean from_signal);
void mono_de_process_single_step (void *tls, gboolean from_signal);
DbgEngineErrorCode mono_de_ss_create (MonoInternalThread *thread, StepSize size, StepDepth depth, StepFilter filter, EventRequest *req);
void mono_de_cancel_ss (SingleStepReq *req);
void mono_de_cancel_all_ss (void);

DbgEngineErrorCode mono_de_set_interp_var (MonoType *t, gpointer addr, guint8 *val_buf);

gboolean set_set_notification_for_wait_completion_flag (DbgEngineStackFrame *frame);
MonoClass * get_class_to_get_builder_field(DbgEngineStackFrame *frame);
gpointer get_this_addr (DbgEngineStackFrame *the_frame);
gpointer get_async_method_builder (DbgEngineStackFrame *frame);
MonoMethod* get_set_notification_method (MonoClass* async_builder_class);
MonoMethod* get_notify_debugger_of_wait_completion_method (void);
MonoMethod* get_object_id_for_debugger_method (MonoClass* async_builder_class);

#ifdef HOST_ANDROID
#define PRINT_DEBUG_MSG(level, ...) do { if (G_UNLIKELY ((level) <= log_level)) { g_print (__VA_ARGS__); } } while (0)
#define DEBUG(level,s) do { if (G_UNLIKELY ((level) <= log_level)) { s; } } while (0)
#elif HOST_WASM
void wasm_debugger_log(int level, const gchar *format, ...);
#define PRINT_DEBUG_MSG(level, ...) do { if (G_UNLIKELY ((level) <= log_level)) { wasm_debugger_log (level, __VA_ARGS__); } } while (0)
#define DEBUG(level,s) do { if (G_UNLIKELY ((level) <= log_level)) { s; } } while (0)
#elif defined(HOST_WIN32) && !HAVE_API_SUPPORT_WIN32_CONSOLE
void win32_debugger_log(FILE *stream, const gchar *format, ...);
#define PRINT_DEBUG_MSG(level, ...) do { if (G_UNLIKELY ((level) <= log_level)) { win32_debugger_log (log_file, __VA_ARGS__); } } while (0)
#define DEBUG(level,s) do { if (G_UNLIKELY ((level) <= log_level)) { s; } } while (0)
#else
#define PRINT_DEBUG_MSG(level, ...) do { if (G_UNLIKELY ((level) <= log_level)) { fprintf (log_file, __VA_ARGS__); fflush (log_file); } } while (0)
#define DEBUG(level,s) do { if (G_UNLIKELY ((level) <= log_level)) { s; fflush (log_file); } } while (0)
#endif
#endif

#if defined(HOST_WIN32) && !HAVE_API_SUPPORT_WIN32_CONSOLE
void win32_debugger_log(FILE *stream, const gchar *format, ...);
#define PRINT_ERROR_MSG(...) win32_debugger_log (log_file, __VA_ARGS__)
#define PRINT_MSG(...) win32_debugger_log (log_file, __VA_ARGS__)
#else
#define PRINT_ERROR_MSG(...) g_printerr (__VA_ARGS__)
#define PRINT_MSG(...) g_print (__VA_ARGS__)
#endif
