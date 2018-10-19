/**
 * \file
 */

#ifndef __MONO_DEBUGGER_ENGINE_H__
#define __MONO_DEBUGGER_ENGINE_H__

#include "mini.h"
#include <mono/metadata/seq-points-data.h>
#include <mono/mini/debugger-state-machine.h>

/*
FIXME:
- Move EventKind back to debugger-agent.c as it contains sdb wire protocol constants.
This is complicated because EventRequest has an event_kind field.

*/

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
	EVENT_KIND_EXCEPTION = 13,
	EVENT_KIND_KEEPALIVE = 14,
	EVENT_KIND_USER_BREAK = 15,
	EVENT_KIND_USER_LOG = 16,
	EVENT_KIND_CRASH = 17
} EventKind;

typedef enum {
	MOD_KIND_COUNT = 1,
	MOD_KIND_THREAD_ONLY = 3,
	MOD_KIND_LOCATION_ONLY = 7,
	MOD_KIND_EXCEPTION_ONLY = 8,
	MOD_KIND_STEP = 10,
	MOD_KIND_ASSEMBLY_ONLY = 11,
	MOD_KIND_SOURCE_FILE_ONLY = 12,
	MOD_KIND_TYPE_NAME_ONLY = 13,
	MOD_KIND_NONE = 14
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
	STEP_FILTER_NONE = 0,
	STEP_FILTER_STATIC_CTOR = 1,
	STEP_FILTER_DEBUGGER_HIDDEN = 2,
	STEP_FILTER_DEBUGGER_STEP_THROUGH = 4,
	STEP_FILTER_DEBUGGER_NON_USER_CODE = 8
} StepFilter;

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
	gboolean caught, uncaught, subclasses; /* For kind == MOD_KIND_EXCEPTION_ONLY */
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
	gboolean (*try_process_suspend) (void *tls, MonoContext *ctx);
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
void mono_de_cancel_ss (void);

#endif
