#include <glib.h>
#include <mono/mini/mini.h>
#include <mono/mini/mini-runtime.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/assembly-internals.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/seq-points-data.h>
#include <mono/mini/aot-runtime.h>
#include <mono/mini/seq-points.h>
#include <mono/component/debugger-engine.h>
#include "debugger-protocol.h"
#include "debugger-agent.h"
#include <mono/metadata/components.h>

//XXX This is dirty, extend ee.h to support extracting info from MonoInterpFrameHandle
#include <mono/mini/interp/interp-internals.h>

#ifdef HOST_WASM

#include <emscripten.h>

#include "mono/metadata/assembly-internals.h"
#include "mono/metadata/debug-mono-ppdb.h"

#include <mono/mini/debugger-agent-external.h>

static int log_level = 1;

//functions exported to be used by JS
G_BEGIN_DECLS

EMSCRIPTEN_KEEPALIVE void mono_wasm_set_is_debugger_attached (gboolean is_attached);
EMSCRIPTEN_KEEPALIVE gboolean mono_wasm_send_dbg_command (int id, MdbgProtCommandSet command_set, int command, guint8* data, unsigned int size);
EMSCRIPTEN_KEEPALIVE gboolean mono_wasm_send_dbg_command_with_parms (int id, MdbgProtCommandSet command_set, int command, guint8* data, unsigned int size, int valtype, char* newvalue);


//JS functions imported that we use
extern void mono_wasm_fire_debugger_agent_message (void);
extern void mono_wasm_asm_loaded (const char *asm_name, const char *assembly_data, guint32 assembly_len, const char *pdb_data, guint32 pdb_len);

G_END_DECLS

static gboolean receive_debugger_agent_message (void *data, int len);
static void assembly_loaded (MonoProfiler *prof, MonoAssembly *assembly);

//FIXME move all of those fields to the profiler object
static gboolean debugger_enabled;

static gboolean has_pending_lazy_loaded_assemblies;


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
jit_done (MonoProfiler *prof, MonoMethod *method, MonoJitInfo *jinfo)
{
	mono_de_add_pending_breakpoints (method, jinfo);
}

static void
appdomain_load (MonoProfiler *prof, MonoDomain *domain)
{
	mono_de_domain_add (domain);
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
	mono_ss_discard_frame_context (mono_wasm_get_tls ());
}

static void
ss_calculate_framecount (void *tls, MonoContext *ctx, gboolean force_use_ctx, DbgEngineStackFrame ***out_frames, int *nframes)
{
	mono_wasm_save_thread_context ();
	mono_ss_calculate_framecount (mono_wasm_get_tls (), NULL, force_use_ctx, out_frames, nframes);
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

#define DBG_NOT_SUSPENDED 1

static int
handle_multiple_ss_requests (void) {
	mono_de_cancel_all_ss ();
	return 1;
}

static void
mono_wasm_enable_debugging_internal (int debug_level)
{
	PRINT_DEBUG_MSG (1, "DEBUGGING ENABLED\n");
	debugger_enabled = TRUE;
	log_level = debug_level;
}

static void
mono_wasm_debugger_init (MonoDefaults *mono_defaults)
{
	int debug_level = mono_wasm_get_debug_level();
	mono_wasm_enable_debugging_internal (debug_level);

	if (!debugger_enabled)
		return;

	mdbg_mono_defaults = mono_defaults;

	DebuggerEngineCallbacks cbs = {
		.tls_get_restore_state = tls_get_restore_state,
		.try_process_suspend = try_process_suspend,
		.begin_breakpoint_processing = begin_breakpoint_processing,
		.begin_single_step_processing = begin_single_step_processing,
		.ss_discard_frame_context = ss_discard_frame_context,
		.ss_calculate_framecount = ss_calculate_framecount,
		.ensure_jit = ensure_jit,
		.ensure_runtime_is_suspended = ensure_runtime_is_suspended,
		.handle_multiple_ss_requests = handle_multiple_ss_requests,
	};
	mono_debug_init (MONO_DEBUG_FORMAT_MONO);
	mono_de_init (&cbs);
	mono_de_set_log_level (log_level, stdout);

	get_mini_debug_options ()->gen_sdb_seq_points = TRUE;
	get_mini_debug_options ()->mdb_optimizations = TRUE;
	mono_disable_optimizations (MONO_OPT_LINEARS);
	get_mini_debug_options ()->load_aot_jit_info_eagerly = TRUE;

	MonoProfilerHandle prof = mono_profiler_create (NULL);
	mono_profiler_set_jit_done_callback (prof, jit_done);
	//FIXME support multiple appdomains
	mono_profiler_set_domain_loaded_callback (prof, appdomain_load);
	mono_profiler_set_assembly_loaded_callback (prof, assembly_loaded);

//debugger-agent initialization	
	DebuggerTransport trans;
	trans.name = "buffer-wasm-communication";
	trans.send = receive_debugger_agent_message;

	mono_debugger_agent_register_transport (&trans);
	mono_init_debugger_agent_for_wasm (log_level);
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
			if (ppdb && !mono_ppdb_is_embedded (ppdb)) { //if it's an embedded pdb we don't need to send pdb extrated to DebuggerProxy. 
				pdb_image = mono_ppdb_get_image (ppdb);
				mono_wasm_asm_loaded (assembly_image->assembly_name, assembly_image->raw_data, assembly_image->raw_data_len, pdb_image->raw_data, pdb_image->raw_data_len);
				return;
			}
		}
		mono_wasm_asm_loaded (assembly_image->assembly_name, assembly_image->raw_data, assembly_image->raw_data_len, NULL, 0);
	}
}

static void
mono_wasm_single_step_hit (void)
{
	mono_de_process_single_step (mono_wasm_get_tls (), FALSE);
}

static void
mono_wasm_breakpoint_hit (void)
{
	mono_de_process_breakpoint (mono_wasm_get_tls (), FALSE);
}

static gboolean
write_value_to_buffer (MdbgProtBuffer *buf, MonoTypeEnum type, const char* variableValue)
{
	char* endptr = NULL;
	const char *variableValueEnd = variableValue + strlen(variableValue);
	errno = 0;
	buffer_add_byte (buf, type);
	switch (type) {
		case MONO_TYPE_BOOLEAN:
			if (!strcasecmp (variableValue, "True"))
				buffer_add_int (buf, 1);
			else if (!strcasecmp (variableValue, "False"))
				buffer_add_int (buf, 0);
			else
				return FALSE;
			break;
		case MONO_TYPE_CHAR:
			if (strlen (variableValue) > 1)
				return FALSE;
			buffer_add_int (buf, (variableValue [0]));
			break;
		case MONO_TYPE_I1: {
			intmax_t val = strtoimax (variableValue, &endptr, 10);
			if (errno != 0 || variableValue == endptr || endptr != variableValueEnd)
				return FALSE;
			if (val >= -128 && val <= 127)
				buffer_add_int (buf, val);
			else
				return FALSE;
			break;
		}
		case MONO_TYPE_U1: {
			intmax_t val = strtoimax (variableValue, &endptr, 10);
			if (errno != 0 || variableValue == endptr || endptr != variableValueEnd)
				return FALSE;
			if (val >= 0 && val <= 255)
				buffer_add_int (buf, val);
			else
				return FALSE;
			break;
		}
		case MONO_TYPE_I2: {
			intmax_t val = strtoimax (variableValue, &endptr, 10);
			if (errno != 0 || variableValue == endptr || endptr != variableValueEnd)
				return FALSE;
			if (val >= -32768 && val <= 32767)
				buffer_add_int (buf, val);
			else
				return FALSE;
			break;
		}
		case MONO_TYPE_U2: {
			intmax_t val = strtoimax (variableValue, &endptr, 10);
			if (errno != 0 || variableValue == endptr || endptr != variableValueEnd)
				return FALSE;
			if (val >= 0 && val <= 65535)
				buffer_add_int (buf, val);
			else
				return FALSE;
			break;
		}
		case MONO_TYPE_I4: {
			intmax_t val = strtoimax (variableValue, &endptr, 10);
			if (errno != 0 || variableValue == endptr || endptr != variableValueEnd)
				return FALSE;
			if (val >= -2147483648 && val <= 2147483647)
				buffer_add_int (buf, val);
			else
				return FALSE;
			break;
		}
		case MONO_TYPE_U4: {
			intmax_t val = strtoimax (variableValue, &endptr, 10);
			if (errno != 0 || variableValue == endptr || endptr != variableValueEnd)
				return FALSE;
			if (val >= 0 && val <= 4294967295)
				buffer_add_int (buf, val);
			else
				return FALSE;
			break;
		}
		case MONO_TYPE_I8: {
			long long val = strtoll (variableValue, &endptr, 10);
			if (errno != 0 || variableValue == endptr || endptr != variableValueEnd)
				return FALSE;
			buffer_add_long (buf, val);
			break;
		}
		case MONO_TYPE_U8: {
			long long val = strtoll (variableValue, &endptr, 10);
			if (errno != 0 || variableValue == endptr || endptr != variableValueEnd)
				return FALSE;
			buffer_add_long (buf, val);
			break;
		}
		case MONO_TYPE_R4: {
			gfloat val = strtof (variableValue, &endptr);
			if (errno != 0 || variableValue == endptr || endptr != variableValueEnd)
				return FALSE;
			buffer_add_int (buf, *((gint32*)(&val)));
			break;
		}
		case MONO_TYPE_R8: {
			gdouble val = strtof (variableValue, &endptr);
			if (errno != 0 || variableValue == endptr || endptr != variableValueEnd)
				return FALSE;
			buffer_add_long (buf, *((guint64*)(&val)));
			break;
		}
		default:
			return FALSE;
	}
	return TRUE;
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

EMSCRIPTEN_KEEPALIVE gboolean 
mono_wasm_send_dbg_command_with_parms (int id, MdbgProtCommandSet command_set, int command, guint8* data, unsigned int size, int valtype, char* newvalue)
{
	MdbgProtBuffer bufWithParms;
	buffer_init (&bufWithParms, 128);
	m_dbgprot_buffer_add_data (&bufWithParms, data, size);
	if (!write_value_to_buffer(&bufWithParms, valtype, newvalue)) {
		EM_ASM ({
			MONO.mono_wasm_add_dbg_command_received ($0, $1, $2, $3);
		}, 0, id, 0, 0);
		return TRUE;
	}
	mono_wasm_send_dbg_command(id, command_set, command, bufWithParms.buf, m_dbgprot_buffer_len(&bufWithParms));
	buffer_free (&bufWithParms);
	return TRUE;
}

EMSCRIPTEN_KEEPALIVE gboolean 
mono_wasm_send_dbg_command (int id, MdbgProtCommandSet command_set, int command, guint8* data, unsigned int size)
{
	ss_calculate_framecount (NULL, NULL, TRUE, NULL, NULL);
	MdbgProtBuffer buf;
	buffer_init (&buf, 128);
	gboolean no_reply;
	MdbgProtErrorCode error = 0;
	if (command_set == MDBGPROT_CMD_SET_VM && command == MDBGPROT_CMD_VM_INVOKE_METHOD ) 
	{
		DebuggerTlsData* tls = mono_wasm_get_tls ();
		InvokeData invoke_data;
		memset (&invoke_data, 0, sizeof (InvokeData));
		invoke_data.endp = data + size;
		error = mono_do_invoke_method (tls, &buf, &invoke_data, data, &data);
	}
	else
		error = mono_process_dbg_packet (id, command_set, command, &no_reply, data, data + size, &buf);
	EM_ASM ({
		MONO.mono_wasm_add_dbg_command_received ($0, $1, $2, $3);
	}, error == MDBGPROT_ERR_NONE, id, buf.buf, buf.p-buf.buf);
	
	buffer_free (&buf);
	return TRUE;
}

static gboolean 
receive_debugger_agent_message (void *data, int len)
{
	EM_ASM ({
		MONO.mono_wasm_add_dbg_command_received (1, -1, $0, $1);
	}, data, len);
	mono_wasm_save_thread_context();
	mono_wasm_fire_debugger_agent_message ();	
	return FALSE;
}

#else // HOST_WASM

static void
mono_wasm_single_step_hit (void)
{
}

static void
mono_wasm_breakpoint_hit (void)
{
}

static void
mono_wasm_debugger_init (MonoDefaults *mono_defaults)
{
}


#endif // HOST_WASM

void
mini_wasm_debugger_add_function_pointers (MonoComponentDebugger* fn_table)
{
	fn_table->init = mono_wasm_debugger_init;
	fn_table->mono_wasm_breakpoint_hit = mono_wasm_breakpoint_hit;
	fn_table->mono_wasm_single_step_hit = mono_wasm_single_step_hit;
}
