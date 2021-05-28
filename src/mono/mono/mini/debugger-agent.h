/**
 * \file
 */

#ifndef __MONO_DEBUGGER_AGENT_H__
#define __MONO_DEBUGGER_AGENT_H__

#include "mini.h"
#include "debugger-protocol.h"

#include <mono/utils/mono-stack-unwinding.h>

#define MONO_DBG_CALLBACKS_VERSION (4)
// 2. debug_log parameters changed from MonoString* to MonoStringHandle
// 3. debug_log parameters changed from MonoStringHandle back to MonoString*

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

typedef struct {
	const char *name;
	void (*connect) (const char *address);
	void (*close1) (void);
	void (*close2) (void);
	gboolean (*send) (void *buf, int len);
	int (*recv) (void *buf, int len);
} DebuggerTransport;

struct _MonoDebuggerCallbacks {
	int version;
	void (*parse_options) (char *options);
	void (*init) (void);
	void (*breakpoint_hit) (void *sigctx);
	void (*single_step_event) (void *sigctx);
	void (*single_step_from_context) (MonoContext *ctx);
	void (*breakpoint_from_context) (MonoContext *ctx);
	void (*free_mem_manager) (gpointer mem_manager);
	void (*unhandled_exception) (MonoException *exc);
	void (*handle_exception) (MonoException *exc, MonoContext *throw_ctx,
							  MonoContext *catch_ctx, StackFrameInfo *catch_frame);
	void (*begin_exception_filter) (MonoException *exc, MonoContext *ctx, MonoContext *orig_ctx);
	void (*end_exception_filter) (MonoException *exc, MonoContext *ctx, MonoContext *orig_ctx);
	void (*user_break) (void);
	void (*debug_log) (int level, MonoString *category, MonoString *message);
	gboolean (*debug_log_is_enabled) (void);
	void (*send_crash) (char *json_dump, MonoStackHash *hashes, int pause);
};

typedef struct _DebuggerTlsData DebuggerTlsData;

MONO_API void
mono_debugger_agent_init (void);

MONO_API void
mono_debugger_agent_parse_options (char *options);

void
mono_debugger_agent_stub_init (void);

MONO_API MONO_RT_EXTERNAL_ONLY gboolean
mono_debugger_agent_transport_handshake (void);

MONO_API void
mono_debugger_agent_register_transport (DebuggerTransport *trans);

MdbgProtErrorCode
mono_process_dbg_packet (int id, MdbgProtCommandSet command_set, int command, gboolean *no_reply, guint8 *p, guint8 *end, MdbgProtBuffer *buf);

void
mono_init_debugger_agent_for_wasm (int log_level);

void*
mono_dbg_create_breakpoint_events (GPtrArray *ss_reqs, GPtrArray *bp_reqs, MonoJitInfo *ji, MdbgProtEventKind kind);

void
mono_dbg_process_breakpoint_events (void *_evts, MonoMethod *method, MonoContext *ctx, int il_offset);

void 
mono_dbg_debugger_agent_user_break (void);

void
mono_wasm_save_thread_context (void);

DebuggerTlsData*
mono_wasm_get_tls (void);

MdbgProtErrorCode
mono_do_invoke_method (DebuggerTlsData *tls, MdbgProtBuffer *buf, InvokeData *invoke, guint8 *p, guint8 **endp);

void
mono_debugger_agent_handle_exception (MonoException *exc, MonoContext *throw_ctx, MonoContext *catch_ctx, StackFrameInfo *catch_frame);

void 
mono_ss_discard_frame_context (void *the_tls);

#endif
