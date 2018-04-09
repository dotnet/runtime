/**
 * \file
 */

#ifndef __MONO_DEBUGGER_AGENT_H__
#define __MONO_DEBUGGER_AGENT_H__

#include "mini.h"
#include <mono/utils/mono-stack-unwinding.h>

#define MONO_DBG_CALLBACKS_VERSION 0x1

struct _MonoDebuggerCallbacks {
	int version;
	void (*parse_options) (char *options);
	void (*init) (void);
	void (*breakpoint_hit) (void *sigctx);
	void (*single_step_event) (void *sigctx);
	void (*single_step_from_context) (MonoContext *ctx);
	void (*breakpoint_from_context) (MonoContext *ctx);
	void (*free_domain_info) (MonoDomain *domain);
	void (*unhandled_exception) (MonoException *exc);
	void (*handle_exception) (MonoException *exc, MonoContext *throw_ctx,
							  MonoContext *catch_ctx, StackFrameInfo *catch_frame);
	void (*begin_exception_filter) (MonoException *exc, MonoContext *ctx, MonoContext *orig_ctx);
	void (*end_exception_filter) (MonoException *exc, MonoContext *ctx, MonoContext *orig_ctx);
	void (*user_break) (void);
	void (*debug_log) (int level, MonoString *category, MonoString *message);
	gboolean (*debug_log_is_enabled) (void);
};

MONO_API void
mono_debugger_agent_init (void);

void
mono_debugger_agent_stub_init (void);

MONO_API gboolean
mono_debugger_agent_transport_handshake (void);

#endif
