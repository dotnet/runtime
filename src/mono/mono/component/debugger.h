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
#include "debugger-protocol.h"
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
	int last_column;
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
	bool (*begin_breakpoint_processing) (void *tls, MonoContext *ctx, MonoJitInfo *ji, gboolean from_signal);
	void (*begin_single_step_processing) (MonoContext *ctx, gboolean from_signal);

	void (*ss_discard_frame_context) (void *tls);
	void (*ss_calculate_framecount) (void *tls, MonoContext *ctx, gboolean force_use_ctx, DbgEngineStackFrame ***frames, int *nframes);
	gboolean (*ensure_jit) (DbgEngineStackFrame *frame);
	int (*ensure_runtime_is_suspended) (void);
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

typedef struct _DebuggerTlsData DebuggerTlsData;

typedef struct MonoComponentDebugger {
	MonoComponent component;
	void (*init) (void);
	void (*user_break) (void);
	void (*parse_options) (char *options);
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
	void (*debug_log) (int level, MonoString *category, MonoString *message);
	gboolean (*debug_log_is_enabled) (void);
	gboolean (*transport_handshake) (void);

	//wasm
	void (*mono_wasm_breakpoint_hit) (void);
	void (*mono_wasm_single_step_hit) (void);

	//HotReload
	void (*send_enc_delta) (MonoImage *image, gconstpointer dmeta_bytes, int32_t dmeta_len, gconstpointer dpdb_bytes, int32_t dpdb_len);

	//wasi
	void (*receive_and_process_command_from_debugger_agent) (void);
	gboolean (*debugger_enabled) (void);
} MonoComponentDebugger;


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


MONO_COMPONENT_EXPORT_ENTRYPOINT
MonoComponentDebugger *
mono_component_debugger_init (void);

#define MONO_DBG_CALLBACKS_VERSION (4)


#endif/*_MONO_COMPONENT_DEBUGGER_H*/
