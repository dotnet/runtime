#include <glib.h>
#include "mini.h"
#include "mini-runtime.h"
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/assembly-internals.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/seq-points-data.h>
#include <mono/mini/aot-runtime.h>
#include <mono/mini/seq-points.h>
#include <mono/mini/debugger-engine.h>

//XXX This is dirty, extend ee.h to support extracting info from MonoInterpFrameHandle
#include <mono/mini/interp/interp-internals.h>

#ifdef HOST_WASM

#include <emscripten.h>

#include "mono/metadata/assembly-internals.h"
#include "mono/metadata/debug-mono-ppdb.h"

static int log_level = 1;

enum {
	EXCEPTION_MODE_NONE,
	EXCEPTION_MODE_UNCAUGHT,
	EXCEPTION_MODE_ALL
};

// Flags for get_*_properties
#define GPFLAG_NONE               0x0000
#define GPFLAG_OWN_PROPERTIES     0x0001
#define GPFLAG_ACCESSORS_ONLY     0x0002
#define GPFLAG_EXPAND_VALUETYPES  0x0004

//functions exported to be used by JS
G_BEGIN_DECLS

EMSCRIPTEN_KEEPALIVE int mono_wasm_set_breakpoint (const char *assembly_name, int method_token, int il_offset);
EMSCRIPTEN_KEEPALIVE int mono_wasm_remove_breakpoint (int bp_id);
EMSCRIPTEN_KEEPALIVE int mono_wasm_current_bp_id (void);
EMSCRIPTEN_KEEPALIVE void mono_wasm_enum_frames (void);
EMSCRIPTEN_KEEPALIVE gboolean mono_wasm_get_local_vars (int scope, int* pos, int len);
EMSCRIPTEN_KEEPALIVE void mono_wasm_clear_all_breakpoints (void);
EMSCRIPTEN_KEEPALIVE int mono_wasm_setup_single_step (int kind);
EMSCRIPTEN_KEEPALIVE int mono_wasm_pause_on_exceptions (int state);
EMSCRIPTEN_KEEPALIVE gboolean mono_wasm_get_object_properties (int object_id, int gpflags);
EMSCRIPTEN_KEEPALIVE gboolean mono_wasm_get_array_values (int object_id, int start_idx, int count, int gpflags);
EMSCRIPTEN_KEEPALIVE gboolean mono_wasm_invoke_getter_on_object (int object_id, const char* name);
EMSCRIPTEN_KEEPALIVE gboolean mono_wasm_invoke_getter_on_value (void *value, MonoClass *klass, const char *name);
EMSCRIPTEN_KEEPALIVE gboolean mono_wasm_get_deref_ptr_value (void *value_addr, MonoClass *klass);
EMSCRIPTEN_KEEPALIVE void mono_wasm_set_is_debugger_attached (gboolean is_attached);
EMSCRIPTEN_KEEPALIVE gboolean mono_wasm_set_variable_on_frame (int scope, int index, const char* name, const char* value);
EMSCRIPTEN_KEEPALIVE gboolean mono_wasm_set_value_on_object (int object_id, const char* name, const char* value);

//JS functions imported that we use
extern void mono_wasm_add_frame (int il_offset, int method_token, int frame_id, const char *assembly_name, const char *method_name);
extern void mono_wasm_fire_bp (void);
extern void mono_wasm_fire_exception (int exception_obj_id, const char* message, const char* class_name, gboolean uncaught);
extern void mono_wasm_add_obj_var (const char*, const char*, guint64);
extern void mono_wasm_add_enum_var (const char*, const char*, guint64);
extern void mono_wasm_add_func_var (const char*, const char*, guint64);
extern void mono_wasm_add_properties_var (const char*, gint32);
extern void mono_wasm_add_array_item (int);
extern void mono_wasm_set_is_async_method (guint64);
extern void mono_wasm_add_typed_value (const char *type, const char *str_value, double value);
extern void mono_wasm_asm_loaded (const char *asm_name, const char *assembly_data, guint32 assembly_len, const char *pdb_data, guint32 pdb_len);

G_END_DECLS

static void describe_object_properties_for_klass (void *obj, MonoClass *klass, gboolean isAsyncLocalThis, int gpflags);
static void handle_exception (MonoException *exc, MonoContext *throw_ctx, MonoContext *catch_ctx, StackFrameInfo *catch_frame);
static void assembly_loaded (MonoProfiler *prof, MonoAssembly *assembly);
static MonoObject* mono_runtime_try_invoke_internal (MonoMethod *method, void *obj, void **params, MonoObject **exc, MonoError* error);

//FIXME move all of those fields to the profiler object
static gboolean debugger_enabled;

static gboolean has_pending_lazy_loaded_assemblies;

static int event_request_id;
static GHashTable *objrefs;
static GHashTable *obj_to_objref;
static int objref_id = 0;
static int pause_on_exc = EXCEPTION_MODE_NONE;
static MonoObject* exception_on_runtime_invoke = NULL;

static const char*
all_getters_allowed_class_names[] = {
	"System.DateTime",
	"System.DateTimeOffset",
	"System.TimeSpan"
};

static const char*
to_string_as_descr_names[] = {
	"System.DateTime",
	"System.DateTimeOffset",
	"System.Decimal",
	"System.TimeSpan"
};

#define THREAD_TO_INTERNAL(thread) (thread)->internal_thread

void wasm_debugger_log (int level, const gchar *format, ...)
{
	va_list args;
	char *mesg;

	va_start (args, format);
	mesg = g_strdup_vprintf (format, args);
	va_end (args);

	EM_ASM ({
		var level = $0;
		var message = Module.UTF8ToString ($1);
		var namespace = "Debugger.Debug";

		if (MONO["logging"] && MONO.logging["debugger"]) {
			MONO.logging.debugger (level, message);
			return;
		}

		console.debug("%s: %s", namespace, message);
	}, level, mesg);
	g_free (mesg);
}

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

	PRINT_DEBUG_MSG (2, "collect_frames: Reporting method %s native_offset %d, wrapper_type: %d\n", method->name, info->native_offset, method->wrapper_type);

	if (!mono_find_prev_seq_point_for_native_offset (method, info->native_offset, NULL, &sp))
		PRINT_DEBUG_MSG (2, "collect_frames: Failed to lookup sequence point. method: %s, native_offset: %d \n", method->name, info->native_offset);

 
	StackFrame *frame = g_new0 (StackFrame, 1);
	frame->de.ji = info->ji;
	frame->de.domain = mono_get_root_domain ();
	frame->de.method = method;
	frame->de.native_offset = info->native_offset;

	frame->il_offset = info->il_offset;
	frame->interp_frame = info->interp_frame;
	frame->frame_addr = info->frame_addr;
	
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
try_process_suspend (void *tls, MonoContext *ctx, gboolean from_breakpoint)
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
get_object_id (MonoObject *obj) 
{
	ObjRef *ref;
	if (!obj)
		return 0;

	ref = (ObjRef *)g_hash_table_lookup (obj_to_objref, GINT_TO_POINTER (~((gsize)obj)));
	if (ref)
		return ref->id;
	ref = g_new0 (ObjRef, 1);
	ref->id = mono_atomic_inc_i32 (&objref_id);
	ref->handle = mono_gchandle_new_weakref_internal (obj, FALSE);
	g_hash_table_insert (objrefs, GINT_TO_POINTER (ref->id), ref);
	g_hash_table_insert (obj_to_objref, GINT_TO_POINTER (~((gsize)obj)), ref);
	return ref->id;
}


static int
get_this_async_id (DbgEngineStackFrame *frame)
{
	MonoClassField *builder_field;
	gpointer builder;
	MonoMethod *method;
	MonoObject *ex;
	ERROR_DECL (error);
	MonoObject *obj;
	
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

	method = get_object_id_for_debugger_method (mono_class_from_mono_type_internal (builder_field->type));
	if (!method) {
		return 0;
	}

	obj = mono_runtime_try_invoke_internal (method, builder, NULL, &ex, error);
	mono_error_assert_ok (error);

	return get_object_id (obj);
}

typedef struct {
	gboolean is_ss; //do I need this?
} BpEvents;

static void*
create_breakpoint_events (GPtrArray *ss_reqs, GPtrArray *bp_reqs, MonoJitInfo *ji, EventKind kind)
{
	PRINT_DEBUG_MSG (1, "ss_reqs %d bp_reqs %d\n", ss_reqs->len, bp_reqs->len);
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
			mono_de_cancel_all_ss ();
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
	PRINT_DEBUG_MSG (1, "Unable to find seq points for method '%s', offset 0x%x.\n", mono_method_full_name (method, TRUE), offset);
}

#define DBG_NOT_SUSPENDED 1

static int
ss_create_init_args (SingleStepReq *ss_req, SingleStepArgs *ss_args)
{
	PRINT_DEBUG_MSG (1, "ss_create_init_args\n");
	int dummy = 0;
	ss_req->start_sp = ss_req->last_sp = &dummy;
	compute_frames ();
	memset (ss_args, 0, sizeof (*ss_args));

	// This shouldn't happen - maybe should assert here ?
	if (frames->len == 0) {
		PRINT_DEBUG_MSG (1, "SINGLE STEPPING FOUND NO FRAMES");
		return DBG_NOT_SUSPENDED;
	}

	DbgEngineStackFrame *frame = (DbgEngineStackFrame*)g_ptr_array_index (frames, 0);
	ss_req->start_method = ss_args->method = frame->method;
	gboolean found_sp = mono_find_prev_seq_point_for_native_offset (frame->method, frame->native_offset, &ss_args->info, &ss_args->sp);
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

static int
handle_multiple_ss_requests (void) {
	mono_de_cancel_all_ss ();
	return 1;
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
		.handle_multiple_ss_requests = handle_multiple_ss_requests,
	};

	mono_debug_init (MONO_DEBUG_FORMAT_MONO);
	mono_de_init (&cbs);
	mono_de_set_log_level (log_level, stdout);

	mini_debug_options.gen_sdb_seq_points = TRUE;
	mini_debug_options.mdb_optimizations = TRUE;
	mono_disable_optimizations (MONO_OPT_LINEARS);
	mini_debug_options.load_aot_jit_info_eagerly = TRUE;

	MonoProfilerHandle prof = mono_profiler_create (NULL);
	mono_profiler_set_jit_done_callback (prof, jit_done);
	//FIXME support multiple appdomains
	mono_profiler_set_domain_loaded_callback (prof, appdomain_load);
	mono_profiler_set_assembly_loaded_callback (prof, assembly_loaded);

	obj_to_objref = g_hash_table_new (NULL, NULL);
	objrefs = g_hash_table_new_full (NULL, NULL, NULL, mono_debugger_free_objref);

	mini_get_dbg_callbacks ()->handle_exception = handle_exception;
	mini_get_dbg_callbacks ()->user_break = mono_wasm_user_break;
}

MONO_API void
mono_wasm_enable_debugging (int debug_level)
{
	PRINT_DEBUG_MSG (1, "DEBUGGING ENABLED\n");
	debugger_enabled = TRUE;
	log_level = debug_level;
}

EMSCRIPTEN_KEEPALIVE int
mono_wasm_pause_on_exceptions (int state)
{
	pause_on_exc = state;
	PRINT_DEBUG_MSG (1, "setting pause on exception: %d\n", pause_on_exc);
	return 1;
}

EMSCRIPTEN_KEEPALIVE int
mono_wasm_setup_single_step (int kind)
{
	int nmodifiers = 1;

	PRINT_DEBUG_MSG (2, ">>>> mono_wasm_setup_single_step %d\n", kind);
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
		g_error ("[dbg] unknown step kind %d", kind);
	}

	DbgEngineErrorCode err = mono_de_ss_create (THREAD_TO_INTERNAL (mono_thread_current ()), size, depth, filter, req);
	if (err != DE_ERR_NONE) {
		PRINT_DEBUG_MSG (1, "[dbg] Failed to setup single step request");
	}
	PRINT_DEBUG_MSG (1, "[dbg] single step is in place, now what?\n");
	SingleStepReq *ss_req = req->info;
	int isBPOnNativeCode = 0;
	if (ss_req && ss_req->bps) {
		GSList *l;

		for (l = ss_req->bps; l; l = l->next) {
			if (((MonoBreakpoint *)l->data)->method->wrapper_type != MONO_WRAPPER_RUNTIME_INVOKE)
				isBPOnNativeCode = 1;
		}
	}
	if (!isBPOnNativeCode) {
		mono_de_cancel_all_ss ();
	}
	return isBPOnNativeCode;
}

static void
assembly_loaded (MonoProfiler *prof, MonoAssembly *assembly)
{
	PRINT_DEBUG_MSG (2, "assembly_loaded callback called for %s\n", assembly->aname.name);
	MonoImage *assembly_image = assembly->image;
	MonoImage *pdb_image = NULL;

	if (!mono_is_debugger_attached ()) {
		has_pending_lazy_loaded_assemblies = TRUE;
		return;
	}

	if (mono_wasm_assembly_already_added(assembly->aname.name))
		return;

	if (mono_has_pdb_checksum ((char *) assembly_image->raw_data, assembly_image->raw_data_len)) { //if it's a release assembly we don't need to send to DebuggerProxy
		MonoDebugHandle *handle = mono_debug_get_handle (assembly_image);
		if (handle) {
			MonoPPDBFile *ppdb = handle->ppdb;
			if (!mono_ppdb_is_embedded (ppdb)) { //if it's an embedded pdb we don't need to send pdb extrated to DebuggerProxy. 
				pdb_image = mono_ppdb_get_image (ppdb);
				mono_wasm_asm_loaded (assembly_image->assembly_name, assembly_image->raw_data, assembly_image->raw_data_len, pdb_image->raw_data, pdb_image->raw_data_len);
				return;
			}
		}
		mono_wasm_asm_loaded (assembly_image->assembly_name, assembly_image->raw_data, assembly_image->raw_data_len, NULL, 0);
	}
}

static void
handle_exception (MonoException *exc, MonoContext *throw_ctx, MonoContext *catch_ctx, StackFrameInfo *catch_frame)
{
	ERROR_DECL (error);
	const char *default_error_message = "Failed to get exception message.";

	PRINT_DEBUG_MSG (1, "handle exception - %d - %p - %p - %p\n", pause_on_exc, exc, throw_ctx, catch_ctx);
	
    //normal mono_runtime_try_invoke does not capture the exception and this is a temporary workaround.
	exception_on_runtime_invoke = (MonoObject*)exc;

	if (pause_on_exc == EXCEPTION_MODE_NONE)
		return;
	if (pause_on_exc == EXCEPTION_MODE_UNCAUGHT && catch_ctx != NULL)
		return;

	int obj_id = get_object_id ((MonoObject *)exc);
	char *error_message = mono_string_to_utf8_checked_internal (exc->message, error);

	const char *class_name = mono_class_full_name (mono_object_class (exc));
	PRINT_DEBUG_MSG (2, "handle exception - calling mono_wasm_fire_exc(): %d - message - %s, class_name: %s\n", obj_id,  !is_ok (error) ? error_message : default_error_message, class_name);

	mono_wasm_fire_exception (obj_id, !is_ok (error) ? error_message : default_error_message, class_name, !catch_ctx);

	if (error_message != NULL)
		g_free (error_message);
	
	PRINT_DEBUG_MSG (2, "handle exception - done\n");
}


EMSCRIPTEN_KEEPALIVE void
mono_wasm_clear_all_breakpoints (void)
{
	PRINT_DEBUG_MSG (1, "CLEAR BREAKPOINTS\n");
	mono_de_clear_all_breakpoints ();
}

EMSCRIPTEN_KEEPALIVE int
mono_wasm_set_breakpoint (const char *assembly_name, int method_token, int il_offset)
{
	int i;
	ERROR_DECL (error);
	PRINT_DEBUG_MSG (1, "SET BREAKPOINT: assembly %s method %x offset %x\n", assembly_name, method_token, il_offset);


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
	MonoAssemblyByNameRequest byname_req;
	mono_assembly_request_prepare_byname (&byname_req, MONO_ASMCTX_DEFAULT, mono_alc_get_default ());
	MonoAssembly *assembly = mono_assembly_request_byname (aname, &byname_req, &status);
	g_free (lookup_name);
	if (!assembly) {
		PRINT_DEBUG_MSG (1, "Could not resolve assembly %s\n", assembly_name);
		return -1;
	}

	mono_assembly_name_free_internal (aname);

	MonoMethod *method = mono_get_method_checked (assembly->image, MONO_TOKEN_METHOD_DEF | method_token, NULL, NULL, error);
	if (!method) {
		//FIXME don't swallow the error
		PRINT_DEBUG_MSG (1, "Could not find method due to %s\n", mono_error_get_message (error));
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
		PRINT_DEBUG_MSG (1, "Could not set breakpoint to %s\n", mono_error_get_message (error));
		mono_error_cleanup (error);
		return 0;
	}

	PRINT_DEBUG_MSG (1, "NEW BP %p has id %d\n", req, req->id);
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

void
mono_wasm_user_break (void)
{
	mono_wasm_fire_bp ();
}

EMSCRIPTEN_KEEPALIVE int
mono_wasm_current_bp_id (void)
{
	PRINT_DEBUG_MSG (2, "COMPUTING breakpoint ID\n");
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
	gboolean found_sp = mono_find_prev_seq_point_for_native_offset (method, native_offset, &info, &sp);
	if (!found_sp)
		PRINT_DEBUG_MSG (1, "Could not find SP\n");


	GPtrArray *bp_reqs = g_ptr_array_new ();
	mono_de_collect_breakpoints_by_sp (&sp, ji, NULL, bp_reqs);

	if (bp_reqs->len == 0) {
		PRINT_DEBUG_MSG (1, "BP NOT FOUND for method %s JI %p il_offset %d\n", method->name, ji, sp.il_offset);
		return -1;
	}

	if (bp_reqs->len > 1)
		PRINT_DEBUG_MSG (1, "Multiple breakpoints (%d) at the same location, returning the first one.", bp_reqs->len);

	EventRequest *evt = (EventRequest *)g_ptr_array_index (bp_reqs, 0);
	g_ptr_array_free (bp_reqs, TRUE);

	PRINT_DEBUG_MSG (1, "Found BP %p with id %d\n", evt, evt->id);
	return evt->id;
}

static MonoObject*
get_object_from_id (int objectId)
{
	ObjRef *ref = (ObjRef *)g_hash_table_lookup (objrefs, GINT_TO_POINTER (objectId));
	if (!ref) {
		PRINT_DEBUG_MSG (2, "get_object_from_id !ref: %d\n", objectId);
		return NULL;
	}

	MonoObject *obj = mono_gchandle_get_target_internal (ref->handle);
	if (!obj)
		PRINT_DEBUG_MSG (2, "get_object_from_id !obj: %d\n", objectId);

	return obj;
}

static gboolean
list_frames (MonoStackFrameInfo *info, MonoContext *ctx, gpointer data)
{
	SeqPoint sp;
	MonoMethod *method;
	char *method_full_name;

	int* frame_id_p = (int*)data;
	(*frame_id_p)++;

	//skip wrappers
	if (info->type != FRAME_TYPE_MANAGED && info->type != FRAME_TYPE_INTERP)
		return FALSE;

	if (info->ji)
		method = jinfo_get_method (info->ji);
	else
		method = info->method;

	if (!method || method->wrapper_type != MONO_WRAPPER_NONE)
		return FALSE;

	PRINT_DEBUG_MSG (2, "list_frames: Reporting method %s native_offset %d, wrapper_type: %d\n", method->name, info->native_offset, method->wrapper_type);

	if (!mono_find_prev_seq_point_for_native_offset (method, info->native_offset, NULL, &sp))
		PRINT_DEBUG_MSG (2, "list_frames: Failed to lookup sequence point. method: %s, native_offset: %d\n", method->name, info->native_offset);

	method_full_name = mono_method_full_name (method, FALSE);
	while (method->is_inflated)
		method = ((MonoMethodInflated*)method)->declaring;

	char *assembly_name = g_strdup (m_class_get_image (method->klass)->module_name);
	inplace_tolower (assembly_name);

	PRINT_DEBUG_MSG (2, "adding off %d token %d assembly name %s\n", sp.il_offset, mono_metadata_token_index (method->token), assembly_name);
	mono_wasm_add_frame (sp.il_offset, mono_metadata_token_index (method->token), *frame_id_p, assembly_name, method_full_name);

	g_free (assembly_name);

	return FALSE;
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_enum_frames (void)
{
	int frame_id = -1;
	mono_walk_stack_with_ctx (list_frames, NULL, MONO_UNWIND_NONE, &frame_id);
}

static char*
invoke_to_string (const char *class_name, MonoClass *klass, gpointer addr)
{
	MonoObject *exc;
	MonoString *mstr;
	char *ret_str;
	ERROR_DECL (error);
	MonoObject *obj;

	// TODO: this is for a specific use case right now,
	//       (invoke ToString() get a preview/description for *some* types)
	//       and we don't want to report errors for that.
	if (m_class_is_valuetype (klass)) {
		MonoMethod *method;

		MONO_STATIC_POINTER_INIT (MonoMethod, to_string)
			to_string = mono_class_get_method_from_name_checked (mono_get_object_class (), "ToString", 0, METHOD_ATTRIBUTE_VIRTUAL | METHOD_ATTRIBUTE_PUBLIC, error);
			mono_error_assert_ok (error);
		MONO_STATIC_POINTER_INIT_END (MonoMethod, to_string)

		method = mono_class_get_virtual_method (klass, to_string, error);
		if (!method)
			return NULL;

		MonoString *mstr = (MonoString*) mono_runtime_try_invoke_internal (method, addr , NULL, &exc, error);
		if (exc || !is_ok (error)) {
			PRINT_DEBUG_MSG (1, "Failed to invoke ToString for %s\n", class_name);
			return NULL;
		}

		return mono_string_to_utf8_checked_internal (mstr, error);
	}

	obj = *(MonoObject**)addr;
	if (!obj)
		return NULL;

	mstr = mono_object_try_to_string (obj, &exc, error);
	if (exc || !is_ok (error))
		return NULL;

	ret_str = mono_string_to_utf8_checked_internal (mstr, error);
	if (!is_ok (error))
		return NULL;

	return ret_str;
}

static char*
get_to_string_description (const char* class_name, MonoClass *klass, gpointer addr)
{
	if (!class_name || !klass || !addr)
		return NULL;

	if (strcmp (class_name, "System.Guid") == 0)
		return mono_guid_to_string (addr);

	for (int i = 0; i < G_N_ELEMENTS (to_string_as_descr_names); i ++) {
		if (strcmp (to_string_as_descr_names [i], class_name) == 0) {
			return invoke_to_string (class_name, klass, addr);
		}
	}

	return NULL;
}

typedef struct {
	int cur_frame;
	int target_frame;
	int len;
	int *pos;
	gboolean found;
} FrameDescData;


typedef struct {
	int cur_frame;
	int target_frame;
	int pos;
	const char* new_value;
	gboolean found;
	gboolean error;
} SetVariableValueData;

/*
 * this returns a string formatted like
 *
 *  <ret_type>:[<comma separated list of arg types>]:<method name>
 *
 *  .. which is consumed by `mono_wasm_add_func_var`. It is used for
 *  generating this for the delegate, and it's target.
 */
static char*
mono_method_to_desc_for_js (MonoMethod *method, gboolean include_namespace)
{
	MonoMethodSignature *sig = mono_method_signature_internal (method);
	char *ret_desc = mono_type_full_name (sig->ret);
	char *args_desc = mono_signature_get_desc (sig, include_namespace);

	char *sig_desc = g_strdup_printf ("%s:%s:%s", ret_desc, args_desc, method->name);

	g_free (ret_desc);
	g_free (args_desc);
	return sig_desc;
}

static guint64
read_enum_value (const char *mem, int type)
{
	switch (type) {
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_U1:
		return *(guint8*)mem;
	case MONO_TYPE_I1:
		return *(gint8*)mem;
	case MONO_TYPE_CHAR:
	case MONO_TYPE_U2:
		return read16 (mem);
	case MONO_TYPE_I2:
		return (gint16) read16 (mem);
	case MONO_TYPE_U4:
	case MONO_TYPE_R4:
		return read32 (mem);
	case MONO_TYPE_I4:
		return (gint32) read32 (mem);
	case MONO_TYPE_U8:
	case MONO_TYPE_I8:
	case MONO_TYPE_R8:
		return read64 (mem);
	case MONO_TYPE_U:
	case MONO_TYPE_I:
#if SIZEOF_REGISTER == 8
		return read64 (mem);
#else
		return read32 (mem);
#endif
	default:
		g_assert_not_reached ();
	}
	return 0;
}

static gboolean
nullable_try_get_value (guint8 *nullable, MonoClass *klass, gpointer* out_value)
{
	mono_class_setup_fields (klass);
	g_assert (m_class_is_fields_inited (klass));

	*out_value = NULL;
	MonoClassField *klass_fields = m_class_get_fields (klass);
	gpointer addr_for_has_value = mono_vtype_get_field_addr (nullable, &klass_fields[0]);
	if (0 == *(guint8*)addr_for_has_value)
		return FALSE;

	*out_value = mono_vtype_get_field_addr (nullable, &klass_fields[1]);
	return TRUE;
}

static gboolean
describe_value(MonoType * type, gpointer addr, int gpflags)
{
	ERROR_DECL (error);
	switch (type->type) {
		case MONO_TYPE_BOOLEAN:
			mono_wasm_add_typed_value ("bool", NULL, *(gint8*)addr);
			break;
		case MONO_TYPE_I1:
			mono_wasm_add_typed_value ("number", NULL, *(gint8*)addr);
			break;
		case MONO_TYPE_U1:
			mono_wasm_add_typed_value ("number", NULL, *(guint8*)addr);
			break;
		case MONO_TYPE_CHAR:
			mono_wasm_add_typed_value ("char", NULL, *(guint16*)addr);
			break;
		case MONO_TYPE_U2:
			mono_wasm_add_typed_value ("number", NULL, *(guint16*)addr);
			break;
		case MONO_TYPE_I2:
			mono_wasm_add_typed_value ("number", NULL, *(gint16*)addr);
			break;
		case MONO_TYPE_I4:
		case MONO_TYPE_I:
			mono_wasm_add_typed_value ("number", NULL, *(gint32*)addr);
			break;
		case MONO_TYPE_U4:
		case MONO_TYPE_U:
			mono_wasm_add_typed_value ("number", NULL, *(guint32*)addr);
			break;
		case MONO_TYPE_I8:
			mono_wasm_add_typed_value ("number", NULL, *(gint64*)addr);
			break;
		case MONO_TYPE_U8:
			mono_wasm_add_typed_value ("number", NULL, *(guint64*)addr);
			break;
		case MONO_TYPE_R4:
			mono_wasm_add_typed_value ("number", NULL, *(float*)addr);
			break;
		case MONO_TYPE_R8:
			mono_wasm_add_typed_value ("number", NULL, *(double*)addr);
			break;
		case MONO_TYPE_PTR:
		case MONO_TYPE_FNPTR: {
			char *class_name = mono_type_full_name (type);
			const void *val = *(const void **)addr;
			char *descr = g_strdup_printf ("(%s) %p", class_name, val);

			EM_ASM ({
				MONO.mono_wasm_add_typed_value ('pointer', $0, { ptr_addr: $1, klass_addr: $2 });
			}, descr, val ? addr : 0, val ? mono_class_from_mono_type_internal (type) : 0);

			g_free (descr);
			g_free (class_name);
			break;
		}

		case MONO_TYPE_STRING: {
			MonoString *str_obj = *(MonoString **)addr;
			if (!str_obj) {
				mono_wasm_add_typed_value ("string", NULL, 0);
			} else {
				char *str = mono_string_to_utf8_checked_internal (str_obj, error);
				mono_error_assert_ok (error); /* FIXME report error */
				mono_wasm_add_typed_value ("string", str, 0);
				g_free (str);
			}
			break;
		}

		case MONO_TYPE_OBJECT: {
			MonoObject *obj = *(MonoObject**)addr;
			MonoClass *klass = obj->vtable->klass;
			if (!klass) {
				// boxed null
				mono_wasm_add_obj_var ("object", NULL, 0);
				break;
			}

			type = m_class_get_byval_arg (klass);
			if (type->type == MONO_TYPE_OBJECT) {
				mono_wasm_add_obj_var ("object", "object", get_object_id (obj));
				break;
			}

			// Boxed valuetype
			if (m_class_is_valuetype (klass))
				addr = mono_object_unbox_internal (obj);

			return describe_value (type, addr, gpflags);
		}

		case MONO_TYPE_GENERICINST: {
			MonoClass *klass = mono_class_from_mono_type_internal (type);
			if (mono_class_is_nullable (klass)) {
				MonoType *targ = type->data.generic_class->context.class_inst->type_argv [0];

				gpointer nullable_value = NULL;
				if (nullable_try_get_value (addr, klass, &nullable_value)) {
					return describe_value (targ, nullable_value, gpflags);
				} else {
					char* class_name = mono_type_full_name (type);
					mono_wasm_add_obj_var (class_name, NULL, 0);
					g_free (class_name);
					break;
				}
			}

			if (mono_type_generic_inst_is_valuetype (type))
				goto handle_vtype;
			/*
			 * else fallthrough
			 */
		}

		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_CLASS: {
			MonoObject *obj = *(MonoObject**)addr;
			MonoClass *klass = type->data.klass;

			if (m_class_is_valuetype (mono_object_class (obj))) {
				addr = mono_object_unbox_internal (obj);
				type = m_class_get_byval_arg (mono_object_class (obj));
				goto handle_vtype;
			}

			char *class_name = mono_type_full_name (type);
			int obj_id = get_object_id (obj);

			if (type-> type == MONO_TYPE_ARRAY || type->type == MONO_TYPE_SZARRAY) {
				MonoArray *array = (MonoArray *)obj;
				EM_ASM ({
					MONO.mono_wasm_add_typed_value ('array', $0, { objectId: $1, length: $2 });
				}, class_name, obj_id, mono_array_length_internal (array));
			} else if (m_class_is_delegate (klass) || (type->type == MONO_TYPE_GENERICINST && m_class_is_delegate (type->data.generic_class->container_class))) {
				MonoMethod *method;

				if (type->type == MONO_TYPE_GENERICINST)
					klass = type->data.generic_class->container_class;

				method = mono_get_delegate_invoke_internal (klass);
				if (!method) {
					mono_wasm_add_func_var (class_name, NULL, -1);
				} else {
					MonoMethod *tm = ((MonoDelegate *)obj)->method;
					char *tm_desc = NULL;
					if (tm)
						tm_desc = mono_method_to_desc_for_js (tm, FALSE);

					mono_wasm_add_func_var (class_name, tm_desc, obj_id);
					g_free (tm_desc);
				}
			} else {
				char *to_string_val = get_to_string_description (class_name, klass, addr);
				mono_wasm_add_obj_var (class_name, to_string_val, obj_id);
				g_free (to_string_val);
			}
			g_free (class_name);
			break;
		}

		handle_vtype:
		case MONO_TYPE_VALUETYPE: {
			g_assert (addr);
			MonoClass *klass = mono_class_from_mono_type_internal (type);
			char *class_name = mono_type_full_name (type);

			if (m_class_is_enumtype (klass)) {
				MonoClassField *field;
				gpointer iter = NULL;
				const char *p;
				MonoTypeEnum def_type;
				guint64 field_value;
				guint64 value__ = 0xDEAD;
				GString *enum_members = g_string_new ("");
				int base_type = mono_class_enum_basetype_internal (klass)->type;

				while ((field = mono_class_get_fields_internal (klass, &iter))) {
					if (strcmp ("value__", mono_field_get_name (field)) == 0) {
						value__ = read_enum_value (mono_vtype_get_field_addr (addr, field), base_type);
						continue;
					}

					if (!(field->type->attrs & FIELD_ATTRIBUTE_STATIC))
						continue;
					if (mono_field_is_deleted (field))
						continue;

					p = mono_class_get_field_default_value (field, &def_type);
					/* this is to correctly increment `p` in the blob */
					/* len = */ mono_metadata_decode_blob_size (p, &p);

					field_value = read_enum_value (p, base_type);

					g_string_append_printf (enum_members, ",%s:%llu", mono_field_get_name (field), field_value);
				}

				mono_wasm_add_enum_var (class_name, enum_members->str, value__);
				g_string_free (enum_members, TRUE);
			} else {
				char *to_string_val = get_to_string_description (class_name, klass, addr);

				if (gpflags & GPFLAG_EXPAND_VALUETYPES) {
					int32_t size = mono_class_value_size (klass, NULL);
					void *value_buf = g_malloc0 (size);
					mono_value_copy_internal (value_buf, addr, klass);

					EM_ASM ({
						MONO.mono_wasm_add_typed_value ($0, $1, { toString: $2, value_addr: $3, value_size: $4, klass: $5 });
					}, "begin_vt", class_name, to_string_val, value_buf, size, klass);

					g_free (value_buf);

					// FIXME: isAsyncLocalThis
					describe_object_properties_for_klass (addr, klass, FALSE, gpflags);
					mono_wasm_add_typed_value ("end_vt", NULL, 0);
				} else {
					EM_ASM ({
						MONO.mono_wasm_add_typed_value ($0, $1, { toString: $2 });
					}, "unexpanded_vt", class_name, to_string_val);
				}
				g_free (to_string_val);
			}
			g_free (class_name);
			break;
		}
		default: {
			char *type_name = mono_type_full_name (type);
			char *msg = g_strdup_printf("can't handle type %s [%p, %x]", type_name, type, type->type);
			mono_wasm_add_typed_value ("string", msg, 0);
			g_free (msg);
			g_free (type_name);
		}
	}
	return TRUE;
}

static gboolean
are_getters_allowed (const char *class_name)
{
	for (int i = 0; i < G_N_ELEMENTS (all_getters_allowed_class_names); i ++) {
		if (strcmp (class_name, all_getters_allowed_class_names [i]) == 0)
			return TRUE;
	}

	return FALSE;
}

static gboolean
invoke_and_describe_getter_value (MonoObject *obj, MonoProperty *p)
{
	ERROR_DECL (error);
	MonoObject *res;
	MonoObject *exc;

	MonoMethodSignature *sig = mono_method_signature_internal (p->get);

	res = mono_runtime_try_invoke_internal (p->get, obj, NULL, &exc, error);
	if (!is_ok (error) && exc == NULL)
		exc = (MonoObject *) mono_error_convert_to_exception (error);
	if (exc)
	{
		const char *class_name = mono_class_full_name (mono_object_class (exc));
		ERROR_DECL (local_error);
		char *str = mono_string_to_utf8_checked_internal (((MonoException*)exc)->message, local_error);
		mono_error_assert_ok (local_error); /* FIXME report error */
		char *msg = g_strdup_printf("%s: %s", class_name, str);
		mono_wasm_add_typed_value ("string", msg, 0);
		g_free (msg);
		return TRUE;
	}
	else if (!res || !m_class_is_valuetype (mono_object_class (res)))
		return describe_value (sig->ret, &res, GPFLAG_EXPAND_VALUETYPES);
	else
		return describe_value (sig->ret, mono_object_unbox_internal (res), GPFLAG_EXPAND_VALUETYPES);
}

static MonoObject* mono_runtime_try_invoke_internal (MonoMethod *method, void *obj, void **params, MonoObject **exc, MonoError* error)
{
	exception_on_runtime_invoke = NULL;
	MonoObject* res = mono_runtime_try_invoke (method, obj, params, exc, error);
	if (exception_on_runtime_invoke != NULL)
		*exc = exception_on_runtime_invoke;
	exception_on_runtime_invoke = NULL;
	return res;
}

static void
describe_object_properties_for_klass (void *obj, MonoClass *klass, gboolean isAsyncLocalThis, int gpflags)
{
	MonoClassField *f;
	MonoProperty *p;
	MonoMethodSignature *sig;
	gboolean is_valuetype;
	int pnum;
	char *klass_name;
	gboolean auto_invoke_getters;
	gboolean is_own;
	gboolean only_backing_fields;

	g_assert (klass);
	MonoClass *start_klass = klass;

	only_backing_fields = gpflags & GPFLAG_ACCESSORS_ONLY;
	is_valuetype = m_class_is_valuetype(klass);
	if (is_valuetype)
		gpflags |= GPFLAG_EXPAND_VALUETYPES;

handle_parent:
	is_own = (start_klass == klass);
	klass_name = mono_class_full_name (klass);
	gpointer iter = NULL;
	while (obj && (f = mono_class_get_fields_internal (klass, &iter))) {
		if (isAsyncLocalThis && f->name[0] == '<' && f->name[1] == '>') {
			if (g_str_has_suffix (f->name, "__this")) {
				mono_wasm_add_properties_var ("this", f->offset);
				gpointer field_value = (guint8*)obj + f->offset;

				describe_value (f->type, field_value, gpflags);
			}

			continue;
		}
		if (f->type->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;
		if (mono_field_is_deleted (f))
			continue;

		if (only_backing_fields && !g_str_has_suffix(f->name, "k__BackingField"))
			continue;

		EM_ASM ({
			MONO.mono_wasm_add_properties_var ($0, { field_offset: $1, is_own: $2, attr: $3, owner_class: $4 });
		}, f->name, f->offset, is_own, f->type->attrs, klass_name);

		gpointer field_addr;
		if (is_valuetype)
			field_addr = mono_vtype_get_field_addr (obj, f);
		else
			field_addr = (guint8*)obj + f->offset;

		describe_value (f->type, field_addr, gpflags);
	}

	auto_invoke_getters = are_getters_allowed (klass_name);
	iter = NULL;
	pnum = 0;
	while ((p = mono_class_get_properties (klass, &iter))) {
		if (p->get->name) { //if get doesn't have name means that doesn't have a getter implemented and we don't want to show value, like VS debug
			if (isAsyncLocalThis && (p->name[0] != '<' || (p->name[0] == '<' &&  p->name[1] == '>')))
				continue;

			sig = mono_method_signature_internal (p->get);
			if (sig->param_count != 0) {
				// getters with params are not shown
				continue;
			}

			if (p->get->flags & METHOD_ATTRIBUTE_STATIC)
				continue;

			EM_ASM ({
				MONO.mono_wasm_add_properties_var ($0, { field_offset: $1, is_own: $2, attr: $3, owner_class: $4 });
			}, p->name, pnum, is_own, p->attrs, klass_name);

			gboolean vt_self_type_getter = is_valuetype && mono_class_from_mono_type_internal (sig->ret) == klass;
			if (auto_invoke_getters && !vt_self_type_getter) {
				invoke_and_describe_getter_value (obj, p);
			} else {
				// not allowed to call the getter here
				char *ret_class_name = mono_class_full_name (mono_class_from_mono_type_internal (sig->ret));

				mono_wasm_add_typed_value ("getter", ret_class_name, -1);

				g_free (ret_class_name);
				continue;
			}
		}
		pnum ++;
	}

	g_free (klass_name);

	// ownProperties
	// Note: ownProperties should mean that we return members of the klass itself,
	// but we are going to ignore that here, because otherwise vscode/chrome don't
	// seem to ask for inherited fields at all.
	// if (!is_valuetype && !(gpflags & GPFLAG_OWN_PROPERTIES) && (klass = m_class_get_parent (klass)))
	if (!is_valuetype && (klass = m_class_get_parent (klass)))
		goto handle_parent;
}

/*
 * We return a `Target` property only for now.
 * In future, we could add a `MethodInfo` too.
 */
static gboolean
describe_delegate_properties (MonoObject *obj)
{
	MonoClass *klass = mono_object_class(obj);
	if (!m_class_is_delegate (klass))
		return FALSE;

	// Target, like in VS - what is this field supposed to be, anyway??
	MonoMethod *tm = ((MonoDelegate *)obj)->method;
	char * sig_desc = mono_method_to_desc_for_js (tm, FALSE);

	mono_wasm_add_properties_var ("Target", -1);
	mono_wasm_add_func_var (NULL, sig_desc, -1);

	g_free (sig_desc);
	return TRUE;
}

static gboolean
describe_object_properties (guint64 objectId, gboolean isAsyncLocalThis, int gpflags)
{
	PRINT_DEBUG_MSG (2, "describe_object_properties %llu, gpflags: %d\n", objectId, gpflags);

	MonoObject *obj = get_object_from_id (objectId);
	if (!obj)
		return FALSE;

	if (m_class_is_delegate (mono_object_class (obj))) {
		// delegates get the same id format as regular objects
		describe_delegate_properties (obj);
	} else {
		describe_object_properties_for_klass (obj, obj->vtable->klass, isAsyncLocalThis, gpflags);
	}

	return TRUE;
}

static gboolean
invoke_getter (void *obj_or_value, MonoClass *klass, const char *name)
{
	if (!obj_or_value || !klass || !name) {
		PRINT_DEBUG_MSG (2, "invoke_getter: none of the arguments can be null");
		return FALSE;
	}

	gpointer iter;
handle_parent:
	iter = NULL;
	MonoProperty *p;
	while ((p = mono_class_get_properties (klass, &iter))) {
		//if get doesn't have name means that doesn't have a getter implemented and we don't want to show value, like VS debug
		if (!p->get->name || strcasecmp (p->name, name) != 0)
			continue;

		invoke_and_describe_getter_value (obj_or_value, p);
		return TRUE;
	}

	if ((klass = m_class_get_parent(klass)))
		goto handle_parent;

	return FALSE;
}

static gboolean
describe_array_values (guint64 objectId, int startIdx, int count, int gpflags)
{
	if (count == 0)
		return TRUE;

	int esize;
	gpointer elem;
	MonoArray *arr = (MonoArray*) get_object_from_id (objectId);
	if (!arr)
		return FALSE;

	MonoClass *klass = mono_object_class (arr);
	MonoTypeEnum type = m_class_get_byval_arg (klass)->type;
	if (type != MONO_TYPE_SZARRAY && type != MONO_TYPE_ARRAY) {
		PRINT_DEBUG_MSG (1, "describe_array_values: object is not an array. type: 0x%x\n", type);
		return FALSE;
	}

	int len = arr->max_length;
	if (len == 0 && startIdx == 0 && count <= 0) {
		// Nothing to do
		return TRUE;
	}

	if (startIdx < 0 || (len > 0 && startIdx >= len)) {
		PRINT_DEBUG_MSG (1, "describe_array_values: invalid startIdx (%d) for array of length %d\n", startIdx, len);
		return FALSE;
	}

	if (count > 0 && (startIdx + count) > len) {
		PRINT_DEBUG_MSG (1, "describe_array_values: invalid count (%d) for startIdx: %d, and array of length %d\n", count, startIdx, len);
		return FALSE;
	}

	esize = mono_array_element_size (klass);
	int endIdx = count < 0 ? len : startIdx + count;

	for (int i = startIdx; i < endIdx; i ++) {
		mono_wasm_add_array_item(i);
		elem = (gpointer*)((char*)arr->vector + (i * esize));
		describe_value (m_class_get_byval_arg (m_class_get_element_class (klass)), elem, gpflags);
	}
	return TRUE;
}

static void
describe_async_method_locals (InterpFrame *frame, MonoMethod *method)
{
	//Async methods are special in the way that local variables can be lifted to generated class fields
	gpointer addr = NULL;
	if (mono_debug_lookup_method_async_debug_info (method)) {
		addr = mini_get_interp_callbacks ()->frame_get_this (frame);
		MonoObject *obj = *(MonoObject**)addr;
		int objId = get_object_id (obj);
		mono_wasm_set_is_async_method (objId);
		describe_object_properties (objId, TRUE, GPFLAG_NONE);
	}
}

static void
describe_non_async_this (InterpFrame *frame, MonoMethod *method)
{
	gpointer addr = NULL;
	if (mono_debug_lookup_method_async_debug_info (method))
		return;

	if (mono_method_signature_internal (method)->hasthis) {
		addr = mini_get_interp_callbacks ()->frame_get_this (frame);
		MonoObject *obj = *(MonoObject**)addr;
		MonoClass *klass = method->klass;
		MonoType *type = m_class_get_byval_arg (method->klass);

		mono_wasm_add_properties_var ("this", -1);

		if (m_class_is_valuetype (klass)) {
			describe_value (type, obj, GPFLAG_EXPAND_VALUETYPES);
		} else {
			// this is an object, and we can retrieve the valuetypes in it later
			// through the object id
			describe_value (type, addr, GPFLAG_NONE);
		}
	}
}

static gboolean
describe_variable (InterpFrame *frame, MonoMethod *method, MonoMethodHeader *header, int pos, int gpflags)
{
	MonoType *type = NULL;
	gpointer addr = NULL;
	if (pos < 0) {
		MonoMethodSignature *sig = mono_method_signature_internal (method);
		pos = -pos - 1;

		if (pos >= sig->param_count) {
			PRINT_DEBUG_MSG(1, "BUG: describe_variable, trying to access param indexed %d, but the method (%s) has only %d params\n", pos, method->name, sig->param_count);
			return FALSE;
		}

		type = sig->params [pos];
		addr = mini_get_interp_callbacks ()->frame_get_arg (frame, pos);
	} else {
		if (pos >= header->num_locals) {
			PRINT_DEBUG_MSG(1, "BUG: describe_variable, trying to access local indexed %d, but the method (%s) has only %d locals\n", pos, method->name, header->num_locals);
			return FALSE;
		}

		type = header->locals [pos];
		addr = mini_get_interp_callbacks ()->frame_get_local (frame, pos);
	}

	PRINT_DEBUG_MSG (2, "adding val %p type [%p] %s\n", addr, type, mono_type_full_name (type));

	return describe_value(type, addr, gpflags);
}

static gboolean
decode_value (MonoType *t, guint8 *addr, const char* variableValue)
{
	char* endptr;
	errno = 0;
	switch (t->type) {
		case MONO_TYPE_BOOLEAN:
			if (!strcasecmp (variableValue, "True"))
				*(guint8*)addr = 1;
			else if (!strcasecmp (variableValue, "False"))
				*(guint8*)addr = 0;
			else 
				return FALSE;
			break;
		case MONO_TYPE_CHAR:
			if (strlen (variableValue) > 1)
				return FALSE;
			*(gunichar2*)addr = variableValue [0];
			break;
		case MONO_TYPE_I1: {
			intmax_t val = strtoimax (variableValue, &endptr, 10);
			if (errno != 0)
				return FALSE;
			if (val >= -128 && val <= 127)
				*(gint8*)addr = val;
			else 
				return FALSE;
			break;
		}
		case MONO_TYPE_U1: {
			intmax_t val = strtoimax (variableValue, &endptr, 10);
			if (errno != 0)
				return FALSE;
			if (val >= 0 && val <= 255)
				*(guint8*)addr = val;
			else 
				return FALSE;
			break;
		}
		case MONO_TYPE_I2: {
			intmax_t val = strtoimax (variableValue, &endptr, 10);
			if (errno != 0)
				return FALSE;
			if (val >= -32768 && val <= 32767)
				*(gint16*)addr = val;
			else 
				return FALSE;
			break;
		}
		case MONO_TYPE_U2: {
			intmax_t val = strtoimax (variableValue, &endptr, 10);
			if (errno != 0)
				return FALSE;
			if (val >= 0 && val <= 65535)
				*(guint16*)addr = val;
			else 
				return FALSE;
			break;
		}
		case MONO_TYPE_I4: {
			intmax_t val = strtoimax (variableValue, &endptr, 10);
			if (errno != 0)
				return FALSE;
			if (val >= -2147483648 && val <= 2147483647)
				*(gint32*)addr = val;
			else 
				return FALSE;
			break;
		}
		case MONO_TYPE_U4: {
			intmax_t val = strtoimax (variableValue, &endptr, 10);
			if (errno != 0)
				return FALSE;
			if (val >= 0 && val <= 4294967295)				
				*(guint32*)addr = val;
			else 
				return FALSE;
			break;
		}
		case MONO_TYPE_I8: {
			long long val = strtoll (variableValue, &endptr, 10);
			if (errno != 0)
				return FALSE;
			*(gint64*)addr = val;
			break;
		}
		case MONO_TYPE_U8: {
			long long val = strtoll (variableValue, &endptr, 10);
			if (errno != 0)
				return FALSE;
			*(guint64*)addr = val;
			break;
		}
		case MONO_TYPE_R4: {
			gfloat val = strtof (variableValue, &endptr);
			if (errno != 0)
				return FALSE;
			*(gfloat*)addr = val;
			break;
		}
		case MONO_TYPE_R8: {
			gdouble val = strtof (variableValue, &endptr);
			if (errno != 0)
				return FALSE;
			*(gdouble*)addr = val;
			break;
		}
		default:
			return FALSE;
	}
	return TRUE;
}

static gboolean
set_variable_value_on_frame (MonoStackFrameInfo *info, MonoContext *ctx, gpointer ud)
{
	ERROR_DECL (error);
	SetVariableValueData *data = (SetVariableValueData*)ud;
	gboolean is_arg = FALSE;
	MonoType *t = NULL;
	guint8 *val_buf = NULL;

	++data->cur_frame;

	//skip wrappers
	if (info->type != FRAME_TYPE_MANAGED && info->type != FRAME_TYPE_INTERP) {
		return FALSE;
	}

	if (data->cur_frame != data->target_frame)
		return FALSE;

	data->found = TRUE;

	InterpFrame *frame = (InterpFrame*)info->interp_frame;
	MonoMethod *method = frame->imethod->method;
	MonoMethodSignature *sig = mono_method_signature_internal (method);
	MonoMethodHeader *header = mono_method_get_header_checked (method, error);
	
	if (!header) {
		mono_error_cleanup(error);
		data->error = TRUE;
		return TRUE;
	}

	if (!sig)
		goto exit_with_error;

	int pos = data->pos;
	
	if (pos < 0) {
		pos = - pos - 1;
		if (pos >= sig->param_count) 
			goto exit_with_error;
		is_arg = TRUE;
		t = sig->params [pos];
	}
	else {
		if (pos >= header->num_locals)
			goto exit_with_error;
		t = header->locals [pos];
	}
	
	guint8 *addr;
	if (is_arg)
		addr = (guint8*)mini_get_interp_callbacks ()->frame_get_arg (frame, pos);
	else
		addr = (guint8*)mini_get_interp_callbacks ()->frame_get_local (frame, pos);
	
	val_buf = (guint8 *)g_alloca (mono_class_instance_size (mono_class_from_mono_type_internal (t)));
	
	if (!decode_value(t, val_buf, data->new_value))
		goto exit_with_error;

	DbgEngineErrorCode errorCode = mono_de_set_interp_var (t, addr, val_buf);
	if (errorCode != ERR_NONE) {
		goto exit_with_error;
	}

	mono_metadata_free_mh (header);
	return TRUE;

exit_with_error:	
	data->error = TRUE;
	mono_metadata_free_mh (header);
	return TRUE;
}

static gboolean
describe_variables_on_frame (MonoStackFrameInfo *info, MonoContext *ctx, gpointer ud)
{
	ERROR_DECL (error);
	FrameDescData *data = (FrameDescData*)ud;

	++data->cur_frame;

	//skip wrappers
	if (info->type != FRAME_TYPE_MANAGED && info->type != FRAME_TYPE_INTERP) {
		return FALSE;
	}

	if (data->cur_frame != data->target_frame)
		return FALSE;

	data->found = TRUE;

	InterpFrame *frame = (InterpFrame*)info->interp_frame;
	g_assert (frame);
	MonoMethod *method = frame->imethod->method;
	g_assert (method);

	MonoMethodHeader *header = mono_method_get_header_checked (method, error);
	mono_error_assert_ok (error); /* FIXME report error */

	for (int i = 0; i < data->len; i++)
	{
		if (!describe_variable (frame, method, header, data->pos[i], GPFLAG_EXPAND_VALUETYPES))
			mono_wasm_add_typed_value("symbol", "<unreadable value>", 0);
	}

	describe_async_method_locals (frame, method);
	describe_non_async_this (frame, method);

	mono_metadata_free_mh (header);
	return TRUE;
}

EMSCRIPTEN_KEEPALIVE gboolean
mono_wasm_set_variable_on_frame (int scope, int index, const char* name, const char* value)
{
	if (scope < 0)
		return FALSE;

	SetVariableValueData data;
	data.target_frame = scope;
	data.cur_frame = -1;
	data.pos = index;
	data.found = FALSE;
	data.new_value = value;
	data.error = FALSE;

	mono_walk_stack_with_ctx (set_variable_value_on_frame, NULL, MONO_UNWIND_NONE, &data);
	return !data.error;
}

EMSCRIPTEN_KEEPALIVE gboolean
mono_wasm_get_deref_ptr_value (void *value_addr, MonoClass *klass)
{
	MonoType *type = m_class_get_byval_arg (klass);
	if (type->type != MONO_TYPE_PTR && type->type != MONO_TYPE_FNPTR) {
		PRINT_DEBUG_MSG (2, "BUG: mono_wasm_get_deref_ptr_value: Expected to get a ptr type, but got 0x%x\n", type->type);
		return FALSE;
	}

	mono_wasm_add_properties_var ("deref", -1);
	return describe_value (type->data.type, value_addr, GPFLAG_EXPAND_VALUETYPES);
}

//FIXME this doesn't support getting the return value pseudo-var
EMSCRIPTEN_KEEPALIVE gboolean
mono_wasm_get_local_vars (int scope, int* pos, int len)
{
	if (scope < 0)
		return FALSE;

	FrameDescData data;
	data.target_frame = scope;
	data.cur_frame = -1;
	data.len = len;
	data.pos = pos;
	data.found = FALSE;

	mono_walk_stack_with_ctx (describe_variables_on_frame, NULL, MONO_UNWIND_NONE, &data);

	return data.found;
}

EMSCRIPTEN_KEEPALIVE gboolean
mono_wasm_get_object_properties (int object_id, int gpflags)
{
	PRINT_DEBUG_MSG (2, "getting properties of object %d, gpflags: %d\n", object_id, gpflags);

	return describe_object_properties (object_id, FALSE, gpflags);
}

EMSCRIPTEN_KEEPALIVE gboolean
mono_wasm_get_array_values (int object_id, int start_idx, int count, int gpflags)
{
	PRINT_DEBUG_MSG (2, "getting array values %d, startIdx: %d, count: %d, gpflags: 0x%x\n", object_id, start_idx, count, gpflags);

	return describe_array_values (object_id, start_idx, count, gpflags);
}

EMSCRIPTEN_KEEPALIVE gboolean
mono_wasm_invoke_getter_on_object (int object_id, const char* name)
{
	MonoObject *obj = get_object_from_id (object_id);
	if (!obj)
		return FALSE;

	return invoke_getter (obj, mono_object_class (obj), name);
}

EMSCRIPTEN_KEEPALIVE gboolean
mono_wasm_set_value_on_object (int object_id, const char* name, const char* value)
{
	PRINT_DEBUG_MSG (1,  "mono_wasm_set_value_on_object %d, name: %s, value: %s\n", object_id, name, value);
	MonoObject *obj = get_object_from_id (object_id);
	
	if (!obj || !name) {
		PRINT_DEBUG_MSG (2, "mono_wasm_set_value_on_object: none of the arguments can be null");
		return FALSE;
	}
	MonoClass* klass = mono_object_class (obj);

	gpointer iter;
handle_parent:
	iter = NULL;
	MonoClassField *f;
	while ((f = mono_class_get_fields_internal (klass, &iter))) {
		if (!f->name || strcasecmp (f->name, name) != 0)
			continue;
		guint8 *val_buf = (guint8 *)g_alloca (mono_class_instance_size (mono_class_from_mono_type_internal (f->type)));
	
		if (!decode_value(f->type, val_buf, value)) {
			return FALSE;
		}		
		DbgEngineErrorCode errorCode = mono_de_set_interp_var (f->type, (guint8*)obj + f->offset, val_buf);
		if (errorCode != ERR_NONE) {
			return FALSE;
		}
		return TRUE;
	}

	iter = NULL;
	MonoProperty *p;
	MonoObject *exc;
	ERROR_DECL (error);
	while ((p = mono_class_get_properties (klass, &iter))) {
		if (!p->name || strcasecmp (p->name, name) != 0)
			continue;
		if (!p->set)
			break;
		MonoType *type = mono_method_signature_internal (p->set)->params [0];
		guint8 *val_buf = (guint8 *)g_alloca (mono_class_instance_size (mono_class_from_mono_type_internal (type)));
	
		if (!decode_value(type, val_buf, value)) {
			return FALSE;
		}					
		mono_runtime_try_invoke (p->set, obj, (void **)&val_buf, &exc, error);
		if (!is_ok (error) && exc == NULL)
			exc = (MonoObject*) mono_error_convert_to_exception (error);
		if (exc) {
			char *error_message = mono_string_to_utf8_checked_internal (((MonoException *)exc)->message, error);
			if (is_ok (error)) {
				PRINT_DEBUG_MSG (2, "mono_wasm_set_value_on_object exception: %s\n", error_message);
				g_free (error_message);
				mono_error_cleanup (error);			
			}
			else {
				PRINT_DEBUG_MSG (2, "mono_wasm_set_value_on_object exception\n");
			}
			return FALSE;
		}
		return TRUE;
	}

	if ((klass = m_class_get_parent(klass)))
		goto handle_parent;
	return FALSE;
}

EMSCRIPTEN_KEEPALIVE gboolean
mono_wasm_invoke_getter_on_value (void *value, MonoClass *klass, const char *name)
{
	PRINT_DEBUG_MSG (2, "mono_wasm_invoke_getter_on_value: v: %p klass: %p, name: %s\n", value, klass, name);
	if (!klass || !value)
		return FALSE;

	if (!m_class_is_valuetype (klass)) {
		PRINT_DEBUG_MSG (2, "mono_wasm_invoke_getter_on_value: klass is not a valuetype. name: %s\n", mono_class_full_name (klass));
		return FALSE;
	}

	return invoke_getter (value, klass, name);
}

EMSCRIPTEN_KEEPALIVE void 
mono_wasm_set_is_debugger_attached (gboolean is_attached)
{
	mono_set_is_debugger_attached (is_attached);
	if (is_attached && has_pending_lazy_loaded_assemblies)
	{
		GPtrArray *assemblies = mono_alc_get_all_loaded_assemblies ();
		for (int i = 0; i < assemblies->len; ++i) {
			MonoAssembly *ass = (MonoAssembly*)g_ptr_array_index (assemblies, i);
			assembly_loaded (NULL, ass);
		}
		g_ptr_array_free (assemblies, TRUE);
		has_pending_lazy_loaded_assemblies = FALSE;
	}
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
