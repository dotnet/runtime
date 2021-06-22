// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef _MONO_COMPONENT_DEBUGGER_H
#define _MONO_COMPONENT_DEBUGGER_H

#include <glib.h>
#include "mono/metadata/threads-types.h"
#include "mono/utils/mono-error.h"
#include "mono/utils/mono-context.h"
#include "mono/utils/mono-stack-unwinding.h"
#include "mono/component/component.h"
#include "mono/mini/debugger-protocol.h"
#include "mono/metadata/seq-points-data.h"

typedef struct {
	MdbgProtModifierKind kind;
	union {
		int count; /* For kind == MOD_KIND_COUNT */
		MonoInternalThread *thread; /* For kind == MOD_KIND_THREAD_ONLY */
		MonoClass *exc_class; /* For kind == MONO_KIND_EXCEPTION_ONLY */
		MonoAssembly **assemblies; /* For kind == MONO_KIND_ASSEMBLY_ONLY */
		GHashTable *source_files; /* For kind == MONO_KIND_SOURCE_FILE_ONLY */
		GHashTable *type_names; /* For kind == MONO_KIND_TYPE_NAME_ONLY */
		MdbgProtStepFilter filter; /* For kind == MOD_KIND_STEP */
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

typedef struct {
	const char* name;
	void (*connect) (const char* address);
	void (*close1) (void);
	void (*close2) (void);
	gboolean(*send) (void* buf, int len);
	int (*recv) (void* buf, int len);
} DebuggerTransport;

typedef struct {
	EventRequest *req;
	MonoInternalThread *thread;
	MdbgProtStepDepth depth;
	MdbgProtStepSize size;
	MdbgProtStepFilter filter;
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

	void* (*create_breakpoint_events) (GPtrArray *ss_reqs, GPtrArray *bp_reqs, MonoJitInfo *ji, MdbgProtEventKind kind);
	void (*process_breakpoint_events) (void *_evts, MonoMethod *method, MonoContext *ctx, int il_offset);

	gboolean (*set_set_notification_for_wait_completion_flag) (DbgEngineStackFrame *f);
	MonoMethod* (*get_notify_debugger_of_wait_completion_method)(void);

	int (*ss_create_init_args) (SingleStepReq *ss_req, SingleStepArgs *args);
	void (*ss_args_destroy) (SingleStepArgs *ss_args);
	int (*handle_multiple_ss_requests)(void);
} DebuggerEngineCallbacks;

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
	MonoMethod* method;
	long il_offset;
	EventRequest* req;
	/*
	 * A list of BreakpointInstance structures describing where the breakpoint
	 * was inserted. There could be more than one because of
	 * generics/appdomains/method entry/exit.
	 */
	GPtrArray* children;
} MonoBreakpoint;

typedef int DbgEngineErrorCode;

typedef struct MonoComponentDebugger {
	MonoComponent component;
	void (*parse_options) (char *options);
	void (*init) (void);
	void (*breakpoint_hit) (void *sigctx);
	void (*single_step_event) (void *sigctx);
	void (*single_step_from_context) (MonoContext *ctx);
	void (*breakpoint_from_context) (MonoContext *ctx);
	void (*free_mem_manager) (gpointer mem_manager);
	void (*unhandled_exception) (MonoException *exc);
	void (*handle_exception) (MonoException *exc, MonoContext *throw_ctx,
							  MonoContext *catch_ctx, MonoStackFrameInfo *catch_frame);
	void (*begin_exception_filter) (MonoException *exc, MonoContext *ctx, MonoContext *orig_ctx);
	void (*end_exception_filter) (MonoException *exc, MonoContext *ctx, MonoContext *orig_ctx);
	void (*user_break) (void);
	void (*debug_log) (int level, MonoString *category, MonoString *message);
	gboolean (*debug_log_is_enabled) (void);
	void (*send_crash) (char *json_dump, MonoStackHash *hashes, int pause);
	void (*register_transport) (DebuggerTransport* trans); //debugger-agent
	gboolean (*mono_debugger_agent_transport_handshake) (void);
	void (*mono_debugger_agent_parse_options) (char* options);
	void (*mono_de_init) (DebuggerEngineCallbacks *cbs); //debugger-engine
	void (*mono_debugger_free_objref) (gpointer value); //debugger-engine removeAfterMergeWasmPR
	void (*mono_de_set_log_level) (int level, FILE* file); //debugger-engine removeAfterMergeWasmPR
	void (*mono_de_add_pending_breakpoints) (MonoMethod* method, MonoJitInfo* ji); //debugger-engine removeAfterMergeWasmPR
	void (*mono_de_clear_breakpoint) (MonoBreakpoint* bp); //debugger-engine removeAfterMergeWasmPR
	void (*mono_de_process_single_step) (void* tls, gboolean from_signal); //debugger-engine removeAfterMergeWasmPR
	void (*mono_de_process_breakpoint) (void* tls, gboolean from_signal); //debugger-engine removeAfterMergeWasmPR
	MonoBreakpoint* (*mono_de_set_breakpoint) (MonoMethod* method, long il_offset, EventRequest* req, MonoError* error); //debugger-engine removeAfterMergeWasmPR
	void (*mono_de_cancel_all_ss) (void); //debugger-engine removeAfterMergeWasmPR
	DbgEngineErrorCode (*mono_de_ss_create) (MonoInternalThread* thread, MdbgProtStepSize size, MdbgProtStepDepth depth, MdbgProtStepFilter filter, EventRequest* req); //debugger-engine removeAfterMergeWasmPR
	void (*mono_de_domain_add) (MonoDomain* domain); //debugger-engine removeAfterMergeWasmPR
	void (*mono_de_collect_breakpoints_by_sp) (SeqPoint* sp, MonoJitInfo* ji, GPtrArray* ss_reqs, GPtrArray* bp_reqs); //debugger-engine removeAfterMergeWasmPR
	MonoBreakpoint* (*mono_de_get_breakpoint_by_id) (int id); //debugger-engine removeAfterMergeWasmPR
	DbgEngineErrorCode (*mono_de_set_interp_var) (MonoType* t, gpointer addr, guint8* val_buf); //debugger-engine removeAfterMergeWasmPR
	gboolean (*set_set_notification_for_wait_completion_flag) (DbgEngineStackFrame* frame); //debugger-engine removeAfterMergeWasmPR
	MonoMethod* (*get_notify_debugger_of_wait_completion_method) (void); //debugger-engine removeAfterMergeWasmPR
	MonoClass* (*get_class_to_get_builder_field) (DbgEngineStackFrame* frame); //debugger-engine removeAfterMergeWasmPR
	MonoMethod* (*get_object_id_for_debugger_method) (MonoClass* async_builder_class); //debugger-engine removeAfterMergeWasmPR
} MonoComponentDebugger;


typedef int DbgEngineErrorCodeTHAYS;
#define DE_ERR_NONE 0
// WARNING WARNING WARNING
// Error codes MUST match those of sdb for now
#define DE_ERR_NOT_IMPLEMENTED 100

#if defined(HOST_WIN32) && !HAVE_API_SUPPORT_WIN32_CONSOLE
void win32_debugger_log(FILE *stream, const gchar *format, ...);
#define PRINT_ERROR_MSG(...) win32_debugger_log (log_file, __VA_ARGS__)
#define PRINT_MSG(...) win32_debugger_log (log_file, __VA_ARGS__)
#else
#define PRINT_ERROR_MSG(...) g_printerr (__VA_ARGS__)
#define PRINT_MSG(...) g_print (__VA_ARGS__)
#endif

typedef struct _DebuggerTlsData DebuggerTlsDataTHAYS;

MONO_COMPONENT_EXPORT_ENTRYPOINT
MonoComponentDebugger *
mono_component_debugger_init (void);

#define MONO_DBG_CALLBACKS_VERSION (4)


#endif/*_MONO_COMPONENT_DEBUGGER_H*/
