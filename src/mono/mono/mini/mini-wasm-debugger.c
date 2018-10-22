#include "mini.h"
#include "mini-runtime.h"
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/seq-points-data.h>
#include <mono/mini/aot-runtime.h>
#include <mono/mini/seq-points.h>
#include <mono/mini/debugger-engine.h>

//XXX This is dirty, extend ee.h to support extracting info from MonoInterpFrameHandle
#include <mono/mini/interp/interp-internals.h>

#ifdef HOST_WASM

#include <emscripten.h>


static int log_level = 1;

#define DEBUG_PRINTF(level, ...) do { if (G_UNLIKELY ((level) <= log_level)) { fprintf (stdout, __VA_ARGS__); } } while (0)

//functions exported to be used by JS
G_BEGIN_DECLS

EMSCRIPTEN_KEEPALIVE int mono_wasm_set_breakpoint (const char *assembly_name, int method_token, int il_offset);
EMSCRIPTEN_KEEPALIVE int mono_wasm_remove_breakpoint (int bp_id);
EMSCRIPTEN_KEEPALIVE int mono_wasm_current_bp_id (void);
EMSCRIPTEN_KEEPALIVE void mono_wasm_enum_frames (void);
EMSCRIPTEN_KEEPALIVE void mono_wasm_get_var_info (int scope, int pos);
EMSCRIPTEN_KEEPALIVE void mono_wasm_clear_all_breakpoints (void);
EMSCRIPTEN_KEEPALIVE void mono_wasm_setup_single_step (int kind);

//JS functions imported that we use
extern void mono_wasm_add_frame (int il_offset, int method_token, const char *assembly_name);
extern void mono_wasm_fire_bp (void);
extern void mono_wasm_add_bool_var (gint8);
extern void mono_wasm_add_int_var (gint32);
extern void mono_wasm_add_long_var (gint64);
extern void mono_wasm_add_float_var (float);
extern void mono_wasm_add_double_var (double);
extern void mono_wasm_add_string_var (const char*);

G_END_DECLS

//FIXME move all of those fields to the profiler object
static gboolean debugger_enabled;

static int event_request_id;
#define THREAD_TO_INTERNAL(thread) (thread)->internal_thread

static void
inplace_tolower (char *c)
{
	int i;
	for (i = strlen (c) - 1; i >= 0; --i)
		c [i] = tolower (c [i]);
}

static void
jit_done (MonoProfiler *prof, MonoMethod *method, MonoJitInfo *jinfo)
{
	mono_de_add_pending_breakpoints (method, jinfo);
}

static void
appdomain_load (MonoProfiler *prof, MonoDomain *domain)
{
	mono_de_domain_add (domain);
}

/* Frame state handling */
static GPtrArray *frames;

static void
free_frame (DbgEngineStackFrame *frame)
{
	g_free (frame);
}

static gboolean
collect_frames (MonoStackFrameInfo *info, MonoContext *ctx, gpointer data)
{
	SeqPoint sp;
	MonoMethod *method;

	//skip wrappers
	if (info->type != FRAME_TYPE_MANAGED && info->type != FRAME_TYPE_INTERP)
		return FALSE;

	if (info->ji)
		method = jinfo_get_method (info->ji);
	else
		method = info->method;

	if (!method)
		return FALSE;

	DEBUG_PRINTF (2, "Reporting method %s native_offset %d\n", method->name, info->native_offset);

	if (!mono_find_prev_seq_point_for_native_offset (mono_get_root_domain (), method, info->native_offset, NULL, &sp))
		DEBUG_PRINTF (1, "Failed to lookup sequence point\n");

	DbgEngineStackFrame *frame = g_new0 (DbgEngineStackFrame, 1);

	frame->ji = info->ji;
	frame->domain = info->domain;
	frame->method = method;
	frame->native_offset = info->native_offset;

	g_ptr_array_add (frames, frame);

	return FALSE;
}

static void
free_frame_state (void)
{
	if (frames) {
		int i;
		for (i = 0; i < frames->len; ++i)
			free_frame ((DbgEngineStackFrame*)g_ptr_array_index (frames, i));
		g_ptr_array_set_size (frames, 0);
	}	
}

static void
compute_frames (void) {
	if (frames) {
		int i;
		for (i = 0; i < frames->len; ++i)
			free_frame ((DbgEngineStackFrame*)g_ptr_array_index (frames, i));
		g_ptr_array_set_size (frames, 0);
	} else {
		frames = g_ptr_array_new ();
	}

	mono_walk_stack_with_ctx (collect_frames, NULL, MONO_UNWIND_NONE, NULL);	
}
static MonoContext*
tls_get_restore_state (void *tls)
{
	return NULL;
}

static gboolean
try_process_suspend (void *tls, MonoContext *ctx)
{
	return FALSE;
}

static gboolean
begin_breakpoint_processing (void *tls, MonoContext *ctx, MonoJitInfo *ji, gboolean from_signal)
{
	return TRUE;
}

static void
begin_single_step_processing (MonoContext *ctx, gboolean from_signal)
{
}

static void
ss_discard_frame_context (void *the_tls)
{
	free_frame_state ();
}

static void
ss_calculate_framecount (void *tls, MonoContext *ctx, gboolean force_use_ctx, DbgEngineStackFrame ***out_frames, int *nframes)
{
	compute_frames ();
	if (out_frames)
		*out_frames = (DbgEngineStackFrame **)frames->pdata;
	if (nframes)
		*nframes = frames->len;
}

static gboolean
ensure_jit (DbgEngineStackFrame* the_frame)
{
	return TRUE;
}

static int
ensure_runtime_is_suspended (void)
{
	return DE_ERR_NONE;
}

static int
get_this_async_id (DbgEngineStackFrame *f)
{
	g_error ("get_this_async_id");
	return 0;
}

static gboolean
set_set_notification_for_wait_completion_flag (DbgEngineStackFrame *f)
{
	g_error ("set_set_notification_for_wait_completion_flag");
	return FALSE;
}

static MonoMethod*
get_notify_debugger_of_wait_completion_method (void)
{
	g_error ("get_notify_debugger_of_wait_completion_method");
}

typedef struct {
	gboolean is_ss; //do I need this?
} BpEvents;

static void*
create_breakpoint_events (GPtrArray *ss_reqs, GPtrArray *bp_reqs, MonoJitInfo *ji, EventKind kind)
{
	printf ("ss_reqs %d bp_reqs %d\n", ss_reqs->len, bp_reqs->len);
	if ((ss_reqs && ss_reqs->len) || (bp_reqs && bp_reqs->len)) {
		BpEvents *evts = g_new0 (BpEvents, 1); //just a non-null value to make sure we can raise it on process_breakpoint_events
		evts->is_ss = (ss_reqs && ss_reqs->len);
		return evts;
	}
	return NULL;
}

static void
process_breakpoint_events (void *_evts, MonoMethod *method, MonoContext *ctx, int il_offsets)
{
	BpEvents *evts = (BpEvents*)_evts;
	if (evts) {
		if (evts->is_ss)
			mono_de_cancel_ss ();
		mono_wasm_fire_bp ();
		g_free (evts);
	}
}

static void
no_seq_points_found (MonoMethod *method, int offset)
{
	/*
	 * This can happen in full-aot mode with assemblies AOTed without the 'soft-debug' option to save space.
	 */
	printf ("Unable to find seq points for method '%s', offset 0x%x.\n", mono_method_full_name (method, TRUE), offset);
}

#define DBG_NOT_SUSPENDED 1

static int
ss_create_init_args (SingleStepReq *ss_req, SingleStepArgs *ss_args)
{
	printf ("ss_create_init_args\n");
	int dummy = 0;
	ss_req->start_sp = ss_req->last_sp = &dummy;
	compute_frames ();
	memset (ss_args, 0, sizeof (*ss_args));

	//BIG WTF, should not happen maybe should assert?
	if (frames->len == 0) {
		DEBUG_PRINTF (1, "SINGLE STEPPING FOUND NO FRAMES");
		return DBG_NOT_SUSPENDED;
	}

	DbgEngineStackFrame *frame = (DbgEngineStackFrame*)g_ptr_array_index (frames, 0);
	ss_req->start_method = ss_args->method = frame->method;
	gboolean found_sp = mono_find_prev_seq_point_for_native_offset (frame->domain, frame->method, frame->native_offset, &ss_args->info, &ss_args->sp);
	if (!found_sp)
		no_seq_points_found (frame->method, frame->native_offset);
	g_assert (found_sp);

	ss_args->frames = (DbgEngineStackFrame**)frames->pdata;
	ss_args->nframes = frames->len;
	//XXX do sp

	return DE_ERR_NONE;
}

static void
ss_args_destroy (SingleStepArgs *ss_args)
{
	//nothing to do	
}

void
mono_wasm_debugger_init (void)
{
	if (!debugger_enabled)
		return;

	DebuggerEngineCallbacks cbs = {
		.tls_get_restore_state = tls_get_restore_state,
		.try_process_suspend = try_process_suspend,
		.begin_breakpoint_processing = begin_breakpoint_processing,
		.begin_single_step_processing = begin_single_step_processing,
		.ss_discard_frame_context = ss_discard_frame_context,
		.ss_calculate_framecount = ss_calculate_framecount,
		.ensure_jit = ensure_jit,
		.ensure_runtime_is_suspended = ensure_runtime_is_suspended,
		.get_this_async_id = get_this_async_id,
		.set_set_notification_for_wait_completion_flag = set_set_notification_for_wait_completion_flag,
		.get_notify_debugger_of_wait_completion_method = get_notify_debugger_of_wait_completion_method,
		.create_breakpoint_events = create_breakpoint_events,
		.process_breakpoint_events = process_breakpoint_events,
		.ss_create_init_args = ss_create_init_args,
		.ss_args_destroy = ss_args_destroy,
	};

	mono_debug_init (MONO_DEBUG_FORMAT_MONO);
	mono_de_init (&cbs);
	mono_de_set_log_level (1, stdout);

	mini_get_debug_options ()->gen_sdb_seq_points = TRUE;
	mini_get_debug_options ()->mdb_optimizations = TRUE;
	mono_disable_optimizations (MONO_OPT_LINEARS);
	mini_get_debug_options ()->load_aot_jit_info_eagerly = TRUE;

	MonoProfilerHandle prof = mono_profiler_create (NULL);
	mono_profiler_set_jit_done_callback (prof, jit_done);
	//FIXME support multiple appdomains
	mono_profiler_set_domain_loaded_callback (prof, appdomain_load);
}

MONO_API void
mono_wasm_enable_debugging (void)
{
	DEBUG_PRINTF (1, "DEBUGGING ENABLED\n");
	debugger_enabled = TRUE;
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_setup_single_step (int kind)
{
	int nmodifiers = 1;

	printf (">>>> mono_wasm_setup_single_step %d\n", kind);
	EventRequest *req = (EventRequest *)g_malloc0 (sizeof (EventRequest) + (nmodifiers * sizeof (Modifier)));
	req->id = ++event_request_id;
	req->event_kind = EVENT_KIND_STEP;
	// DE doesn't care about suspend_policy
	// req->suspend_policy = SUSPEND_POLICY_ALL;
	req->nmodifiers = nmodifiers;

	StepSize size = STEP_SIZE_MIN;

	//FIXME I DON'T KNOW WHAT I'M DOING!!!!! filter all the things.
	StepFilter filter = (StepFilter)(STEP_FILTER_STATIC_CTOR | STEP_FILTER_DEBUGGER_HIDDEN | STEP_FILTER_DEBUGGER_STEP_THROUGH | STEP_FILTER_DEBUGGER_NON_USER_CODE);
	req->modifiers [0].data.filter = filter;

	StepDepth depth;
	switch (kind) {
	case 0: //into
		depth = STEP_DEPTH_INTO;
		break;
	case 1: //out
		depth = STEP_DEPTH_OUT;
		break;
	case 2: //over
		depth = STEP_DEPTH_OVER;
		break;
	default:
		g_error ("dunno step kind %d", kind);
	}

	DbgEngineErrorCode err = mono_de_ss_create (THREAD_TO_INTERNAL (mono_thread_current ()), size, depth, filter, req);
	if (err != DE_ERR_NONE) {
		DEBUG_PRINTF (1, "[dbg] Failed to setup single step request");
	}
	printf ("ss is in place, now ahat?\n");
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_clear_all_breakpoints (void)
{
	DEBUG_PRINTF (1, "CLEAR BREAKPOINTS\n");
	mono_de_clear_all_breakpoints ();
}

EMSCRIPTEN_KEEPALIVE int
mono_wasm_set_breakpoint (const char *assembly_name, int method_token, int il_offset)
{
	int i;
	ERROR_DECL (error);
	DEBUG_PRINTF (1, "SET BREAKPOINT: assembly %s method %x offset %x\n", assembly_name, method_token, il_offset);


	//we get 'foo.dll' but mono_assembly_load expects 'foo' so we strip the last dot
	char *lookup_name = g_strdup (assembly_name);
	for (i = strlen (lookup_name) - 1; i >= 0; --i) {
		if (lookup_name [i] == '.') {
			lookup_name [i] = 0;
			break;
		}
	}

	//resolve the assembly
	MonoImageOpenStatus status;
	MonoAssemblyName* aname = mono_assembly_name_new (lookup_name);
	MonoAssembly *assembly = mono_assembly_load (aname, NULL, &status);
	g_free (lookup_name);
	if (!assembly) {
		DEBUG_PRINTF (1, "Could not resolve assembly %s\n", assembly_name);
		return -1;
	}

	mono_assembly_name_free (aname);

	MonoMethod *method = mono_get_method_checked (assembly->image, MONO_TOKEN_METHOD_DEF | method_token, NULL, NULL, error);
	if (!method) {
		//FIXME don't swallow the error
		DEBUG_PRINTF (1, "Could not find method due to %s\n", mono_error_get_message (error));
		mono_error_cleanup (error);
		return -1;
	}

	//FIXME right now none of the EventRequest fields are used by debugger-engine
	EventRequest *req = g_new0 (EventRequest, 1);
	req->id = ++event_request_id;
	req->event_kind = EVENT_KIND_BREAKPOINT;
	//DE doesn't care about suspend_policy
	// req->suspend_policy = SUSPEND_POLICY_ALL;
	req->nmodifiers = 0; //funny thing,

	// BreakPointRequest *req = breakpoint_request_new (assembly, method, il_offset);
	MonoBreakpoint *bp = mono_de_set_breakpoint (method, il_offset, req, error);

	if (!bp) {
		DEBUG_PRINTF (1, "Could not set breakpoint to %s\n", mono_error_get_message (error));
		mono_error_cleanup (error);
		return 0;
	}

	DEBUG_PRINTF (1, "NEW BP %p has id %d\n", req, req->id);
	return req->id;
}

EMSCRIPTEN_KEEPALIVE int
mono_wasm_remove_breakpoint (int bp_id)
{
	MonoBreakpoint *bp = mono_de_get_breakpoint_by_id (bp_id);
	if (!bp)
		return 0;

	mono_de_clear_breakpoint (bp);
	return 1;
}

void
mono_wasm_single_step_hit (void)
{
	mono_de_process_single_step (NULL, FALSE);
}

void
mono_wasm_breakpoint_hit (void)
{
	mono_de_process_breakpoint (NULL, FALSE);
	// mono_wasm_fire_bp ();
}

EMSCRIPTEN_KEEPALIVE int
mono_wasm_current_bp_id (void)
{
	DEBUG_PRINTF (1, "COMPUTING breapoint ID\n");
	//FIXME handle compiled case

	/* Interpreter */
	MonoLMF *lmf = mono_get_lmf ();

	g_assert (((guint64)lmf->previous_lmf) & 2);
	MonoLMFExt *ext = (MonoLMFExt*)lmf;

	g_assert (ext->kind == MONO_LMFEXT_INTERP_EXIT || ext->kind == MONO_LMFEXT_INTERP_EXIT_WITH_CTX);
	MonoInterpFrameHandle *frame = (MonoInterpFrameHandle*)ext->interp_exit_data;
	MonoJitInfo *ji = mini_get_interp_callbacks ()->frame_get_jit_info (frame);
	guint8 *ip = (guint8*)mini_get_interp_callbacks ()->frame_get_ip (frame);

	g_assert (ji && !ji->is_trampoline);
	MonoMethod *method = jinfo_get_method (ji);

	/* Compute the native offset of the breakpoint from the ip */
	guint32 native_offset = ip - (guint8*)ji->code_start;

	MonoSeqPointInfo *info = NULL;
	SeqPoint sp;
	gboolean found_sp = mono_find_prev_seq_point_for_native_offset (mono_domain_get (), method, native_offset, &info, &sp);
	if (!found_sp)
		DEBUG_PRINTF (1, "Could not find SP\n");


	GPtrArray *bp_reqs = g_ptr_array_new ();
	mono_de_collect_breakpoints_by_sp (&sp, ji, NULL, bp_reqs);

	if (bp_reqs->len == 0) {
		DEBUG_PRINTF (1, "BP NOT FOUND for method %s JI %p il_offset %d\n", method->name, ji, sp.il_offset);
		return -1;
	}

	if (bp_reqs->len > 1)
		DEBUG_PRINTF (1, "Multiple breakpoints (%d) at the same location, returning the first one.", bp_reqs->len);

	EventRequest *evt = (EventRequest *)g_ptr_array_index (bp_reqs, 0);
	g_ptr_array_free (bp_reqs, TRUE);

	DEBUG_PRINTF (1, "Found BP %p with id %d\n", evt, evt->id);
	return evt->id;
}

static gboolean
list_frames (MonoStackFrameInfo *info, MonoContext *ctx, gpointer data)
{
	SeqPoint sp;
	MonoMethod *method;

	//skip wrappers
	if (info->type != FRAME_TYPE_MANAGED && info->type != FRAME_TYPE_INTERP)
		return FALSE;


	if (info->ji)
		method = jinfo_get_method (info->ji);
	else
		method = info->method;

	if (!method)
		return FALSE;

	DEBUG_PRINTF (2, "Reporting method %s native_offset %d\n", method->name, info->native_offset);

	if (!mono_find_prev_seq_point_for_native_offset (mono_get_root_domain (), method, info->native_offset, NULL, &sp))
		DEBUG_PRINTF (1, "Failed to lookup sequence point\n");

	while (method->is_inflated)
		method = ((MonoMethodInflated*)method)->declaring;

	char *assembly_name = g_strdup (m_class_get_image (method->klass)->module_name);
	inplace_tolower (assembly_name);

	if (method->wrapper_type == MONO_WRAPPER_NONE) {
		DEBUG_PRINTF (2, "adding off %d token %d assembly name %s\n", sp.il_offset, mono_metadata_token_index (method->token), assembly_name);
		mono_wasm_add_frame (sp.il_offset, mono_metadata_token_index (method->token), assembly_name);
	}

	g_free (assembly_name);

	return FALSE;
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_enum_frames (void)
{
	mono_walk_stack_with_ctx (list_frames, NULL, MONO_UNWIND_NONE, NULL);
}

typedef struct {
	int cur_frame;
	int target_frame;
	int variable;
} FrameDescData;

static gboolean
describe_variable (MonoStackFrameInfo *info, MonoContext *ctx, gpointer ud)
{
	ERROR_DECL (error);
	MonoMethodHeader *header = NULL;

	FrameDescData *data = (FrameDescData*)ud;

	//skip wrappers
	if (info->type != FRAME_TYPE_MANAGED && info->type != FRAME_TYPE_INTERP) {
		return FALSE;
	}

	if (data->cur_frame < data->target_frame) {
		++data->cur_frame;
		return FALSE;
	}

	InterpFrame *frame = (InterpFrame*)info->interp_frame;
	g_assert (frame);
	MonoMethod *method = frame->imethod->method;
	g_assert (method);

	MonoType *type = NULL;
	gpointer addr = NULL;
	int pos = data->variable;
	if (pos < 0) {
		pos = -pos - 1;
		type = mono_method_signature_internal (method)->params [pos];
		addr = mini_get_interp_callbacks ()->frame_get_arg (frame, pos);
	} else {
		header = mono_method_get_header_checked (method, error);
		mono_error_assert_ok (error); /* FIXME report error */

		type = header->locals [pos];
		addr = mini_get_interp_callbacks ()->frame_get_local (frame, pos);
	}

	DEBUG_PRINTF (2, "adding val %p type [%p] %s\n", addr, type, mono_type_full_name (type));

	switch (type->type) {
		case MONO_TYPE_BOOLEAN:
			mono_wasm_add_bool_var (*(gint8*)addr);
			break;
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
			mono_wasm_add_int_var (*(gint8*)addr);
			break;
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
			mono_wasm_add_int_var (*(gint16*)addr);
			break;
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
			mono_wasm_add_int_var (*(gint32*)addr);
			break;
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
			mono_wasm_add_long_var (*(gint32*)addr);
			break;
		case MONO_TYPE_R4:
			mono_wasm_add_float_var (*(float*)addr);
			break;
		case MONO_TYPE_R8:
			mono_wasm_add_float_var (*(double*)addr);
			break;
		case MONO_TYPE_STRING: {
			MonoString *str_obj = *(MonoString **)addr;
			if (!str_obj)
				mono_wasm_add_string_var (NULL);
			char *str = mono_string_to_utf8_checked_internal (str_obj, error);
			mono_error_assert_ok (error); /* FIXME report error */

			mono_wasm_add_string_var (str);
			g_free (str);
			break;
		}
		default: {
			char *type_name = mono_type_full_name (type);
			char *msg = g_strdup_printf("can't handle type %s [%p, %x]", type_name, type, type->type);
			mono_wasm_add_string_var (msg);
			g_free (msg);
			g_free (type_name);
		}
	}
	if (header)
		mono_metadata_free_mh (header);

	return TRUE;
}

//FIXME this doesn't support getting the return value pseudo-var
EMSCRIPTEN_KEEPALIVE void
mono_wasm_get_var_info (int scope, int pos)
{
	DEBUG_PRINTF (2, "getting var %d of scope %d\n", pos, scope);

	FrameDescData data;
	data.cur_frame = 0;
	data.target_frame = scope;
	data.variable = pos;

	mono_walk_stack_with_ctx (describe_variable, NULL, MONO_UNWIND_NONE, &data);
}


// Functions required by debugger-state-machine.
gsize
mono_debugger_tls_thread_id (DebuggerTlsData *debuggerTlsData)
{
	return 1;
}

#else // HOST_WASM

void
mono_wasm_single_step_hit (void)
{
}

void
mono_wasm_breakpoint_hit (void)
{
}

void
mono_wasm_debugger_init (void)
{
}

#endif // HOST_WASM
