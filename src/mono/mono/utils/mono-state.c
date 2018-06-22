/**
 * \file
 * Support for verbose unmanaged crash dumps
 *
 * Author:
 *   Alexander Kyte (alkyte@microsoft.com)
 *
 * (C) 2018 Microsoft, Inc.
 *
 */
#include <config.h>
#include <glib.h>
#include <mono/utils/json.h>
#include <mono/utils/mono-state.h>
#include <mono/metadata/object-internals.h>

#ifdef TARGET_OSX

extern GCStats mono_gc_stats;

// For AOT mode
#include <mono/mini/mini-runtime.h>

#ifdef TARGET_OSX
#include <mach/mach.h>
#include <mach/task_info.h>
#endif

#define MONO_MAX_SUMMARY_LEN 900
static JsonWriter writer;
static GString static_gstr;
static char output_dump_str [MONO_MAX_SUMMARY_LEN];

static void mono_json_writer_init_static (void) {
	static_gstr.len = 0;
	static_gstr.allocated_len = MONO_MAX_SUMMARY_LEN;
	static_gstr.str = output_dump_str;
	memset (output_dump_str, 0, sizeof (output_dump_str));

	writer.indent = 0;
	writer.text = &static_gstr;
}

static void
mono_native_state_add_ctx (JsonWriter *writer, MonoContext *ctx)
{
	// Context
	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "ctx");
	mono_json_writer_object_begin(writer);

	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "IP");
	mono_json_writer_printf (writer, "\"%p\",\n", (gpointer) MONO_CONTEXT_GET_IP (ctx));

	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "SP");
	mono_json_writer_printf (writer, "\"%p\",\n", (gpointer) MONO_CONTEXT_GET_SP (ctx));

	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "BP");
	mono_json_writer_printf (writer, "\"%p\"\n", (gpointer) MONO_CONTEXT_GET_BP (ctx));

	mono_json_writer_indent_pop (writer);
	mono_json_writer_indent (writer);
	mono_json_writer_object_end (writer);
	mono_json_writer_printf (writer, ",\n");
}

static void
mono_native_state_add_frame (JsonWriter *writer, MonoFrameSummary *frame)
{
	mono_json_writer_indent (writer);
	mono_json_writer_object_begin(writer);

	if (frame->is_managed) {
		mono_json_writer_indent (writer);
		mono_json_writer_object_key(writer, "is_managed");
		mono_json_writer_printf (writer, "\"%s\",\n", frame->is_managed ? "true" : "false");
	}

	if (frame->unmanaged_data.is_trampoline) {
		mono_json_writer_indent (writer);
		mono_json_writer_object_key(writer, "is_trampoline");
		mono_json_writer_printf (writer, "\"true\",");
	}

	if (frame->is_managed) {
		mono_json_writer_indent (writer);
		mono_json_writer_object_key(writer, "guid");
		mono_json_writer_printf (writer, "\"%s\",\n", frame->managed_data.guid);

		mono_json_writer_indent (writer);
		mono_json_writer_object_key(writer, "token");
		mono_json_writer_printf (writer, "\"0x%05x\",\n", frame->managed_data.token);

		mono_json_writer_indent (writer);
		mono_json_writer_object_key(writer, "native_offset");
		mono_json_writer_printf (writer, "\"0x%x\",\n", frame->managed_data.native_offset);

		mono_json_writer_indent (writer);
		mono_json_writer_object_key(writer, "il_offset");
		mono_json_writer_printf (writer, "\"0x%05x\"\n", frame->managed_data.il_offset);

	} else {
		mono_json_writer_indent (writer);
		mono_json_writer_object_key(writer, "native_address");
		if (frame->unmanaged_data.ip)
			mono_json_writer_printf (writer, "\"%p\"", (void *) frame->unmanaged_data.ip);
		else
			mono_json_writer_printf (writer, "\"outside mono-sgen\"");

		if (frame->unmanaged_data.has_name) {
			mono_json_writer_printf (writer, ",\n");

			mono_json_writer_indent (writer);
			mono_json_writer_object_key(writer, "unmanaged_name");
			mono_json_writer_printf (writer, "\"%s\"\n", frame->str_descr);
		} else {
			mono_json_writer_printf (writer, "\n");
		}
	}

	mono_json_writer_indent_pop (writer);
	mono_json_writer_indent (writer);
	mono_json_writer_object_end (writer);
}

static void
mono_native_state_add_frames (JsonWriter *writer, int num_frames, MonoFrameSummary *frames, const char *label)
{
	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, label);

	mono_json_writer_array_begin (writer);

	mono_native_state_add_frame (writer, &frames [0]);
	for (int i = 1; i < num_frames; ++i) {
		mono_json_writer_printf (writer, ",\n");
		mono_native_state_add_frame (writer, &frames [i]);
	}
	mono_json_writer_printf (writer, "\n");

	mono_json_writer_indent_pop (writer);
	mono_json_writer_indent (writer);
	mono_json_writer_array_end (writer);
}


static void
mono_native_state_add_thread (JsonWriter *writer, MonoThreadSummary *thread, MonoContext *ctx)
{
	static gboolean not_first_thread;

	if (not_first_thread) {
		mono_json_writer_printf (writer, ",\n");
	} else {
		not_first_thread = TRUE;
	}

	mono_json_writer_indent (writer);
	mono_json_writer_object_begin(writer);

	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "is_managed");
	mono_json_writer_printf (writer, "%s,\n", thread->is_managed ? "true" : "false");

	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "managed_thread_ptr");
	mono_json_writer_printf (writer, "\"0x%x\",\n", (gpointer) thread->managed_thread_ptr);

	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "thread_info_addr");
	mono_json_writer_printf (writer, "\"0x%x\",\n", (gpointer) thread->info_addr);

	if (thread->name) {
		mono_json_writer_indent (writer);
		mono_json_writer_object_key(writer, "thread_name");
		mono_json_writer_printf (writer, "\"%s\",\n", thread->name);
	}

	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "native_thread_id");
	mono_json_writer_printf (writer, "\"0x%x\",\n", (gpointer) thread->native_thread_id);

	mono_native_state_add_ctx (writer, ctx);

	if (thread->num_managed_frames > 0) {
		mono_native_state_add_frames (writer, thread->num_managed_frames, thread->managed_frames, "managed_frames");
	}
	if (thread->num_unmanaged_frames > 0) {
		if (thread->num_managed_frames > 0)
			mono_json_writer_printf (writer, ",\n");
		mono_native_state_add_frames (writer, thread->num_unmanaged_frames, thread->unmanaged_frames, "unmanaged_frames");
	}
	mono_json_writer_printf (writer, "\n");

	mono_json_writer_indent (writer);
	mono_json_writer_object_end (writer);
}

static void
mono_native_state_add_ee_info  (JsonWriter *writer)
{
	// FIXME: setup callbacks to enable
	/*const char *aot_mode;*/
	/*MonoAotMode mono_aot_mode = mono_jit_get_aot_mode ();*/
	/*switch (mono_aot_mode) {*/
		/*case MONO_AOT_MODE_NONE:*/
			/*aot_mode = "none";*/
			/*break;*/
		/*case MONO_AOT_MODE_NORMAL:*/
			/*aot_mode = "normal";*/
			/*break;*/
		/*case MONO_AOT_MODE_HYBRID:*/
			/*aot_mode = "hybrid";*/
			/*break;*/
		/*case MONO_AOT_MODE_FULL:*/
			/*aot_mode = "full";*/
			/*break;*/
		/*case MONO_AOT_MODE_LLVMONLY:*/
			/*aot_mode = "llvmonly";*/
			/*break;*/
		/*case MONO_AOT_MODE_INTERP:*/
			/*aot_mode = "interp";*/
			/*break;*/
		/*case MONO_AOT_MODE_INTERP_LLVMONLY:*/
			/*aot_mode = "interp_llvmonly";*/
			/*break;*/
		/*default:*/
			/*aot_mode = "error";*/
	/*}*/

	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "execution_context");
	mono_json_writer_object_begin(writer);

	/*mono_json_writer_indent (writer);*/
	/*mono_json_writer_object_key(writer, "aot_mode");*/
	/*mono_json_writer_printf (writer, "\"%s\",\n", aot_mode);*/

	/*mono_json_writer_indent (writer);*/
	/*mono_json_writer_object_key(writer, "mono_use_llvm");*/
	/*mono_json_writer_printf (writer, "\"%s\",\n", mono_use_llvm ? "true" : "false");*/

	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "coop-enabled");
	mono_json_writer_printf (writer, "\"%s\"\n", mono_threads_is_cooperative_suspension_enabled () ? "true" : "false");

	mono_json_writer_indent_pop (writer);
	mono_json_writer_indent (writer);
	mono_json_writer_object_end (writer);
	mono_json_writer_printf (writer, ",\n");
}

// Taken from driver.c
#if defined(MONO_ARCH_ARCHITECTURE)
/* Redefine MONO_ARCHITECTURE to include more information */
#undef MONO_ARCHITECTURE
#define MONO_ARCHITECTURE MONO_ARCH_ARCHITECTURE
#endif

static void
mono_native_state_add_version (JsonWriter *writer)
{
	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "configuration");
	mono_json_writer_object_begin(writer);

	char *build = mono_get_runtime_callbacks ()->get_runtime_build_info ();
	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "version");
	mono_json_writer_printf (writer, "\"%s\",\n", build);

	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "tlc");
#ifdef HAVE_KW_THREAD
	mono_json_writer_printf (writer, "\"__thread\",\n");
#else
	mono_json_writer_printf (writer, "\"normal\",\n");
#endif /* HAVE_KW_THREAD */

	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "sigsgev");
#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK
	mono_json_writer_printf (writer, "\"altstack\",\n");
#else
	mono_json_writer_printf (writer, "\"normal\",\n");
#endif

	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "notifications");
#ifdef HAVE_EPOLL
	mono_json_writer_printf (writer, "\"epoll\",\n");
#elif defined(HAVE_KQUEUE)
	mono_json_writer_printf (writer, "\"kqueue\",\n");
#else
	mono_json_writer_printf (writer, "\"thread+polling\",\n");
#endif

	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "architecture");
	mono_json_writer_printf (writer, "\"%s\",\n", MONO_ARCHITECTURE);

	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "disabled_features");
	mono_json_writer_printf (writer, "\"%s\",\n", DISABLED_FEATURES);

	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "smallconfig");
#ifdef MONO_SMALL_CONFIG
	mono_json_writer_printf (writer, "\"enabled\",\n");
#else
	mono_json_writer_printf (writer, "\"disabled\",\n");
#endif

	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "bigarrays");
#ifdef MONO_BIG_ARRAYS
	mono_json_writer_printf (writer, "\"enabled\",\n");
#else
	mono_json_writer_printf (writer, "\"disabled\",\n");
#endif

	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "softdebug");
#if !defined(DISABLE_SDB)
	mono_json_writer_printf (writer, "\"enabled\",\n");
#else
	mono_json_writer_printf (writer, "\"disabled\",\n");
#endif

	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "interpreter");
#ifndef DISABLE_INTERPRETER
	mono_json_writer_printf (writer, "\"enabled\",\n");
#else
	mono_json_writer_printf (writer, "\"disabled\",\n");
#endif

	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "llvm_support");
#ifdef MONO_ARCH_LLVM_SUPPORTED
#ifdef ENABLE_LLVM
	mono_json_writer_printf (writer, "\"%s\"\n", LLVM_VERSION);
#else
	mono_json_writer_printf (writer, "\"disabled\"\n");
#endif
#endif

	mono_json_writer_indent_pop (writer);
	mono_json_writer_indent (writer);
	mono_json_writer_object_end (writer);
	mono_json_writer_printf (writer, ",\n");
}

static void
mono_native_state_add_memory (JsonWriter *writer)
{
	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "memory");
	mono_json_writer_object_begin(writer);

#ifdef TARGET_OSX
	struct task_basic_info t_info;
	memset (&t_info, 0, sizeof (t_info));
	mach_msg_type_number_t t_info_count = TASK_BASIC_INFO_COUNT;
	task_name_t task = mach_task_self ();
	task_info(task, TASK_BASIC_INFO, (task_info_t) &t_info, &t_info_count);

	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "Resident Size");
	mono_json_writer_printf (writer, "\"%lu\",\n", t_info.resident_size);

	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "Virtual Size");
	mono_json_writer_printf (writer, "\"%lu\",\n", t_info.virtual_size);
#endif

	GCStats stats;
	memcpy (&stats, &mono_gc_stats, sizeof (GCStats));

	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "minor_gc_time");
	mono_json_writer_printf (writer, "\"%lu\",\n", stats.minor_gc_time);

	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "major_gc_time");
	mono_json_writer_printf (writer, "\"%lu\",\n", stats.major_gc_time);

	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "minor_gc_count");
	mono_json_writer_printf (writer, "\"%lu\",\n", stats.minor_gc_count);

	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "major_gc_count");
	mono_json_writer_printf (writer, "\"%lu\",\n", stats.major_gc_count);

	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "major_gc_time_concurrent");
	mono_json_writer_printf (writer, "\"%lu\"\n", stats.major_gc_time_concurrent);

	mono_json_writer_indent_pop (writer);
	mono_json_writer_indent (writer);
	mono_json_writer_object_end (writer);
	mono_json_writer_printf (writer, ",\n");
}

static void
mono_native_state_add_prologue (JsonWriter *writer)
{
	mono_json_writer_init (writer);
	mono_json_writer_object_begin(writer);

	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "protocol_version");
	mono_json_writer_printf (writer, "\"%s\",\n", MONO_NATIVE_STATE_PROTOCOL_VERSION);

	mono_native_state_add_version (writer);

#ifndef MONO_PRIVATE_CRASHES
	mono_native_state_add_ee_info (writer);
#endif

	mono_native_state_add_memory (writer);

	const char *assertion_msg = g_get_assertion_message ();
	if (assertion_msg != NULL) {
		mono_json_writer_indent (writer);
		mono_json_writer_object_key(writer, "assertion_message");

		char *pos;
		if ((pos = strchr (assertion_msg, '\n')) != NULL)
			*pos = '\0';

		mono_json_writer_printf (writer, "\"%s\",\n", assertion_msg);
	}

	// Start threads array
	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "threads");
	mono_json_writer_array_begin (writer);
}

static void
mono_native_state_add_epilogue (JsonWriter *writer)
{
	mono_json_writer_indent_pop (writer);
	mono_json_writer_printf (writer, "\n");
	mono_json_writer_indent (writer);
	mono_json_writer_array_end (writer);

	mono_json_writer_indent_pop (writer);
	mono_json_writer_indent (writer);
	mono_json_writer_object_end (writer);
}

void
mono_summarize_native_state_begin (void)
{
	mono_json_writer_init_static ();
	mono_native_state_add_prologue (&writer);
}

char *
mono_summarize_native_state_end (void)
{
	mono_native_state_add_epilogue (&writer);
	return writer.text->str;
}

void
mono_summarize_native_state_add_thread (MonoThreadSummary *thread, MonoContext *ctx)
{
	mono_native_state_add_thread (&writer, thread, ctx);
}

#endif // HOST_WIN32
