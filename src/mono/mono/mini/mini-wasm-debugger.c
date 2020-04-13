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

static int log_level = 1;

#define DEBUG_PRINTF(level, ...) do { if (G_UNLIKELY ((level) <= log_level)) { fprintf (stdout, __VA_ARGS__); } } while (0)

//functions exported to be used by JS
G_BEGIN_DECLS

EMSCRIPTEN_KEEPALIVE int mono_wasm_set_breakpoint (const char *assembly_name, int method_token, int il_offset);
EMSCRIPTEN_KEEPALIVE int mono_wasm_remove_breakpoint (int bp_id);
EMSCRIPTEN_KEEPALIVE int mono_wasm_current_bp_id (void);
EMSCRIPTEN_KEEPALIVE void mono_wasm_enum_frames (void);
EMSCRIPTEN_KEEPALIVE void mono_wasm_get_var_info (int scope, int* pos, int len);
EMSCRIPTEN_KEEPALIVE void mono_wasm_clear_all_breakpoints (void);
EMSCRIPTEN_KEEPALIVE int mono_wasm_setup_single_step (int kind);
EMSCRIPTEN_KEEPALIVE void mono_wasm_get_object_properties (int object_id, gboolean expand_value_types);
EMSCRIPTEN_KEEPALIVE void mono_wasm_get_array_values (int object_id);
EMSCRIPTEN_KEEPALIVE void mono_wasm_get_array_value_expanded (int object_id, int idx);

//JS functions imported that we use
extern void mono_wasm_add_frame (int il_offset, int method_token, const char *assembly_name);
extern void mono_wasm_fire_bp (void);
extern void mono_wasm_add_bool_var (gint8);
extern void mono_wasm_add_number_var (double);
extern void mono_wasm_add_string_var (const char*);
extern void mono_wasm_add_obj_var (const char*, guint64);
extern void mono_wasm_add_value_type_unexpanded_var (const char*);
extern void mono_wasm_begin_value_type_var (const char*);
extern void mono_wasm_end_value_type_var (void);
extern void mono_wasm_add_enum_var (const char*, const char*, guint64);
extern void mono_wasm_add_func_var (const char*, guint64);
extern void mono_wasm_add_array_var (const char*, guint64);
extern void mono_wasm_add_properties_var (const char*, gint32);
extern void mono_wasm_add_array_item (int);
extern void mono_wasm_set_is_async_method (guint64);

G_END_DECLS

static void describe_object_properties_for_klass (void *obj, MonoClass *klass, gboolean isAsyncLocalThis, gboolean expandValueType);

//FIXME move all of those fields to the profiler object
static gboolean debugger_enabled;

static int event_request_id;
static GHashTable *objrefs;
static GHashTable *obj_to_objref;
static int objref_id = 0;

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
		DEBUG_PRINTF (2, "Failed to lookup sequence point\n");

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

	obj_to_objref = g_hash_table_new (NULL, NULL);
	objrefs = g_hash_table_new_full (NULL, NULL, NULL, mono_debugger_free_objref);
}

MONO_API void
mono_wasm_enable_debugging (int debug_level)
{
	DEBUG_PRINTF (1, "DEBUGGING ENABLED\n");
	debugger_enabled = TRUE;
	log_level = debug_level;
}

EMSCRIPTEN_KEEPALIVE int
mono_wasm_setup_single_step (int kind)
{
	int nmodifiers = 1;

	DEBUG_PRINTF (2, ">>>> mono_wasm_setup_single_step %d\n", kind);
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
		DEBUG_PRINTF (1, "[dbg] Failed to setup single step request");
	}
	DEBUG_PRINTF (1, "[dbg] single step is in place, now what?\n");
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
	MonoAssemblyByNameRequest byname_req;
	mono_assembly_request_prepare_byname (&byname_req, MONO_ASMCTX_DEFAULT, mono_domain_default_alc (mono_get_root_domain ()));
	MonoAssembly *assembly = mono_assembly_request_byname (aname, &byname_req, &status);
	g_free (lookup_name);
	if (!assembly) {
		DEBUG_PRINTF (1, "Could not resolve assembly %s\n", assembly_name);
		return -1;
	}

	mono_assembly_name_free_internal (aname);

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

static int get_object_id(MonoObject *obj) 
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
	int len;
	int *pos;
} FrameDescData;

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

static gboolean describe_value(MonoType * type, gpointer addr, gboolean expandValueType)
{
	ERROR_DECL (error);
	switch (type->type) {
		case MONO_TYPE_BOOLEAN:
			mono_wasm_add_bool_var (*(gint8*)addr);
			break;
		case MONO_TYPE_I1:
			mono_wasm_add_number_var (*(gint8*)addr);
			break;
		case MONO_TYPE_U1:
			mono_wasm_add_number_var (*(guint8*)addr);
			break;
		case MONO_TYPE_CHAR:
		case MONO_TYPE_U2:
			mono_wasm_add_number_var (*(guint16*)addr);
			break;
		case MONO_TYPE_I2:
			mono_wasm_add_number_var (*(gint16*)addr);
			break;
		case MONO_TYPE_I4:
		case MONO_TYPE_I:
			mono_wasm_add_number_var (*(gint32*)addr);
			break;
		case MONO_TYPE_U4:
		case MONO_TYPE_U:
			mono_wasm_add_number_var (*(guint32*)addr);
			break;
		case MONO_TYPE_I8:
			mono_wasm_add_number_var (*(gint64*)addr);
			break;
		case MONO_TYPE_U8:
			mono_wasm_add_number_var (*(guint64*)addr);
			break;
		case MONO_TYPE_R4:
			mono_wasm_add_number_var (*(float*)addr);
			break;
		case MONO_TYPE_R8:
			mono_wasm_add_number_var (*(double*)addr);
			break;
		case MONO_TYPE_STRING: {
			MonoString *str_obj = *(MonoString **)addr;
			if (!str_obj) {
				mono_wasm_add_string_var (NULL);
			} else {
				char *str = mono_string_to_utf8_checked_internal (str_obj, error);
				mono_error_assert_ok (error); /* FIXME report error */
				mono_wasm_add_string_var (str);
				g_free (str);
			}
			break;
		}
		case MONO_TYPE_GENERICINST: {
			if (mono_type_generic_inst_is_valuetype (type))
				goto handle_vtype;
			/*
			 * else fallthrough
			 */
		}

		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_CLASS: {
			MonoObject *obj = *(MonoObject**)addr;
			MonoClass *klass = type->data.klass;

			char *class_name = mono_type_full_name (type);
			int obj_id = get_object_id (obj);

			if (type->type == MONO_TYPE_SZARRAY || type->type == MONO_TYPE_ARRAY) {
				mono_wasm_add_array_var (class_name, obj_id);
			} else if (m_class_is_delegate (klass)) {
				mono_wasm_add_func_var (class_name, obj_id);
			} else {
				mono_wasm_add_obj_var (class_name, obj_id);
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
			} else if (expandValueType) {
				mono_wasm_begin_value_type_var (class_name);
				// FIXME: isAsyncLocalThis
				describe_object_properties_for_klass ((MonoObject*)addr, klass, FALSE, expandValueType);
				mono_wasm_end_value_type_var ();
			} else {
				mono_wasm_add_value_type_unexpanded_var (class_name);
			}
			g_free (class_name);
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
	return TRUE;
}

static void
describe_object_properties_for_klass (void *obj, MonoClass *klass, gboolean isAsyncLocalThis, gboolean expandValueType)
{
	MonoClassField *f;
	MonoProperty *p;
	MonoMethodSignature *sig;
	gpointer iter = NULL;
	ERROR_DECL (error);
	gboolean is_valuetype;
	int pnum;

	g_assert (klass);
	is_valuetype = m_class_is_valuetype(klass);

	while (obj && (f = mono_class_get_fields_internal (klass, &iter))) {
		if (isAsyncLocalThis && f->name[0] == '<' && f->name[1] == '>') {
			if (g_str_has_suffix (f->name, "__this")) {
				mono_wasm_add_properties_var ("this", f->offset);
				gpointer field_value = (guint8*)obj + f->offset;

				describe_value (f->type, field_value, is_valuetype | expandValueType);
			}

			continue;
		}
		if (f->type->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;
		if (mono_field_is_deleted (f))
			continue;

		mono_wasm_add_properties_var (f->name, f->offset);

		gpointer field_addr;
		if (is_valuetype)
			field_addr = mono_vtype_get_field_addr (obj, f);
		else
			field_addr = (guint8*)obj + f->offset;
		
		describe_value (f->type, field_addr, is_valuetype | expandValueType);
	}

	iter = NULL;
	pnum = 0;
	while ((p = mono_class_get_properties (klass, &iter))) {
		DEBUG_PRINTF (2, "mono_class_get_properties - %s - %s\n", p->name, p->get->name);
		if (p->get->name) { //if get doesn't have name means that doesn't have a getter implemented and we don't want to show value, like VS debug
			char *class_name;
			if (isAsyncLocalThis && (p->name[0] != '<' || (p->name[0] == '<' &&  p->name[1] == '>')))
				continue;

			mono_wasm_add_properties_var (p->name, pnum);
			sig = mono_method_signature_internal (p->get);

			// automatic properties will get skipped
			class_name = mono_class_full_name (mono_class_from_mono_type_internal (sig->ret));
			mono_wasm_add_string_var (class_name);
			g_free (class_name);

#if false // Disabled for now, as we don't want to invoke getters
			if (is_valuetype && mono_class_from_mono_type_internal (sig->ret) == klass) {
				// Property of the same valuetype, avoid endlessly recursion!
				mono_wasm_add_string_var (mono_class_full_name (klass));
				continue;
			}

			res = mono_runtime_try_invoke (p->get, obj, NULL, &exc, error);
			if (!is_ok (error) && exc == NULL)
				exc = (MonoObject*) mono_error_convert_to_exception (error);
			if (exc)
				describe_value (mono_get_object_type (), &exc, TRUE);
			else if (!res || !m_class_is_valuetype (mono_object_class (res)))
				describe_value (sig->ret, &res, TRUE);
			else
				describe_value (sig->ret, mono_object_unbox_internal (res), TRUE);
#endif
		}
		pnum ++;
	}
}

static gboolean
describe_object_properties (guint64 objectId, gboolean isAsyncLocalThis, gboolean expandValueType)
{
	DEBUG_PRINTF (2, "describe_object_properties %llu\n", objectId);
	ObjRef *ref = (ObjRef *)g_hash_table_lookup (objrefs, GINT_TO_POINTER (objectId));
	if (!ref) {
		DEBUG_PRINTF (2, "describe_object_properties !ref\n");
		return FALSE;
	}

	MonoObject *obj = mono_gchandle_get_target_internal (ref->handle);
	if (!obj) {
		DEBUG_PRINTF (2, "describe_object_properties !obj\n");
		return FALSE;
	}

	describe_object_properties_for_klass (obj, obj->vtable->klass, isAsyncLocalThis, expandValueType);
	return TRUE;
}

static gboolean 
describe_array_values (guint64 objectId)
{
	int esize;
	gpointer elem;
	ObjRef *ref = (ObjRef *)g_hash_table_lookup (objrefs, GINT_TO_POINTER (objectId));
	if (!ref) {
		return FALSE;
	}
	MonoArray *arr = (MonoArray *)mono_gchandle_get_target_internal (ref->handle);
	MonoObject *obj = &arr->obj;
	if (!obj) {
		return FALSE;
	}
	esize = mono_array_element_size (obj->vtable->klass);
	for (int i = 0; i < arr->max_length; i++) {
		mono_wasm_add_array_item(i);
		elem = (gpointer*)((char*)arr->vector + (i * esize));
		describe_value (m_class_get_byval_arg (m_class_get_element_class (arr->obj.vtable->klass)), elem, FALSE);
	}
	return TRUE;
}

/* Expands valuetypes */
static gboolean
describe_array_value_expanded (guint64 objectId, guint64 idx)
{
	int esize;
	gpointer elem;
	ObjRef *ref = (ObjRef *)g_hash_table_lookup (objrefs, GINT_TO_POINTER (objectId));
	if (!ref) {
		return FALSE;
	}
	MonoArray *arr = (MonoArray *)mono_gchandle_get_target_internal (ref->handle);
	MonoObject *obj = &arr->obj;
	if (!obj) {
		return FALSE;
	}
	if (idx >= arr->max_length)
		return FALSE;

	esize = mono_array_element_size (obj->vtable->klass);
	elem = (gpointer*)((char*)arr->vector + (idx * esize));
	describe_value (m_class_get_byval_arg (m_class_get_element_class (arr->obj.vtable->klass)), elem, TRUE);

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
		describe_object_properties (objId, TRUE, FALSE);
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
		char *class_name = mono_class_full_name (obj->vtable->klass);

		mono_wasm_add_properties_var ("this", -1);
		mono_wasm_add_obj_var (class_name, get_object_id(obj));
		g_free (class_name);
	}
}

static gboolean
describe_variable (InterpFrame *frame, MonoMethod *method, int pos, gboolean expandValueType)
{
	ERROR_DECL (error);
	MonoMethodHeader *header = NULL;

	MonoType *type = NULL;
	gpointer addr = NULL;
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

	describe_value(type, addr, expandValueType);
	if (header)
		mono_metadata_free_mh (header);

	return TRUE;
}

static gboolean
describe_variables_on_frame (MonoStackFrameInfo *info, MonoContext *ctx, gpointer ud)
{
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

	for (int i = 0; i < data->len; i++)
	{
		describe_variable (frame, method, data->pos[i], TRUE);
	}

	describe_async_method_locals (frame, method);
	describe_non_async_this (frame, method);

	return TRUE;
}

//FIXME this doesn't support getting the return value pseudo-var
EMSCRIPTEN_KEEPALIVE void
mono_wasm_get_var_info (int scope, int* pos, int len)
{
	FrameDescData data;
	data.target_frame = scope;
	data.cur_frame = 0;
	data.len = len;
	data.pos = pos;

	mono_walk_stack_with_ctx (describe_variables_on_frame, NULL, MONO_UNWIND_NONE, &data);
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_get_object_properties (int object_id, gboolean expand_value_types)
{
	DEBUG_PRINTF (2, "getting properties of object %d\n", object_id);

	describe_object_properties (object_id, FALSE, expand_value_types);
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_get_array_values (int object_id)
{
	DEBUG_PRINTF (2, "getting array values %d\n", object_id);

	describe_array_values(object_id);
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_get_array_value_expanded (int object_id, int idx)
{
	DEBUG_PRINTF (2, "getting array value %d for idx %d\n", object_id, idx);

	describe_array_value_expanded (object_id, idx);
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
