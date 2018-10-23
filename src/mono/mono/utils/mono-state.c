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
#include <mono/utils/mono-state.h>
#include <mono/utils/mono-threads-coop.h>
#include <mono/metadata/object-internals.h>

#ifndef DISABLE_CRASH_REPORTING

extern GCStats mono_gc_stats;

// For AOT mode
#include <mono/mini/mini-runtime.h>

#ifdef TARGET_OSX
#include <mach/mach.h>
#include <mach/task_info.h>
#endif

#define MONO_MAX_SUMMARY_LEN 500000
static gchar output_dump_str [MONO_MAX_SUMMARY_LEN];

static JsonWriter writer;
static GString static_gstr;

static void mono_json_writer_init_memory (gchar *output_dump_str, int len)
{
	memset (&static_gstr, 0, sizeof (static_gstr));
	memset (&writer, 0, sizeof (writer));
	memset (output_dump_str, 0, len * sizeof (gchar));

	static_gstr.len = 0;
	static_gstr.allocated_len = len;
	static_gstr.str = output_dump_str;

	writer.indent = 0;
	writer.text = &static_gstr;
}

static void mono_json_writer_init_with_static (void) 
{
	return mono_json_writer_init_memory (output_dump_str, MONO_MAX_SUMMARY_LEN);
}

static void assert_has_space (void)
{
	// Each individual key/value append should be roughly less than this many characters
	const int margin = 35;

	// Not using static, exit
	if (static_gstr.allocated_len == 0)
		return;

	if (static_gstr.allocated_len - static_gstr.len < margin)
		g_error ("Ran out of memory to create crash dump json blob. Current state:\n%s\n", static_gstr.str);
}

static void
mono_native_state_add_ctx (JsonWriter *writer, MonoContext *ctx)
{
	// Context
	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "ctx");
	mono_json_writer_object_begin(writer);

	assert_has_space ();
	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "IP");
	mono_json_writer_printf (writer, "\"%p\",\n", (gpointer) MONO_CONTEXT_GET_IP (ctx));

	assert_has_space ();
	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "SP");
	mono_json_writer_printf (writer, "\"%p\",\n", (gpointer) MONO_CONTEXT_GET_SP (ctx));

	assert_has_space ();
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
		assert_has_space ();
		mono_json_writer_indent (writer);
		mono_json_writer_object_key(writer, "is_managed");
		mono_json_writer_printf (writer, "\"%s\",\n", frame->is_managed ? "true" : "false");
	}

	if (frame->unmanaged_data.is_trampoline) {
		assert_has_space ();
		mono_json_writer_indent (writer);
		mono_json_writer_object_key(writer, "is_trampoline");
		mono_json_writer_printf (writer, "\"true\",");
	}

	if (frame->is_managed) {
		assert_has_space ();
		mono_json_writer_indent (writer);
		mono_json_writer_object_key(writer, "guid");
		mono_json_writer_printf (writer, "\"%s\",\n", frame->managed_data.guid);

		assert_has_space ();
		mono_json_writer_indent (writer);
		mono_json_writer_object_key(writer, "token");
		mono_json_writer_printf (writer, "\"0x%05x\",\n", frame->managed_data.token);

		assert_has_space ();
		mono_json_writer_indent (writer);
		mono_json_writer_object_key(writer, "native_offset");
		mono_json_writer_printf (writer, "\"0x%x\",\n", frame->managed_data.native_offset);

#ifndef MONO_PRIVATE_CRASHES
		if (frame->managed_data.name != NULL) {
			assert_has_space ();
			mono_json_writer_indent (writer);
			mono_json_writer_object_key(writer, "method_name");
			mono_json_writer_printf (writer, "\"%s\",\n", frame->managed_data.name);
		}
#endif

		assert_has_space ();
		mono_json_writer_indent (writer);
		mono_json_writer_object_key(writer, "il_offset");
		mono_json_writer_printf (writer, "\"0x%05x\"\n", frame->managed_data.il_offset);

		assert_has_space ();
		mono_json_writer_indent (writer);
		mono_json_writer_object_key(writer, "il_offset");
		mono_json_writer_printf (writer, "\"0x%05x\"\n", frame->managed_data.il_offset);

	} else {
		assert_has_space ();
		mono_json_writer_indent (writer);
		mono_json_writer_object_key (writer, "native_address");
		if (frame->unmanaged_data.ip) {
			mono_json_writer_printf (writer, "\"0x%" PRIx64 "\"", frame->unmanaged_data.ip);
		} else
			mono_json_writer_printf (writer, "\"unregistered\"");

		if (frame->unmanaged_data.ip) {
			mono_json_writer_printf (writer, ",\n");

			assert_has_space ();
			mono_json_writer_indent (writer);
			mono_json_writer_object_key (writer, "native_offset");
			mono_json_writer_printf (writer, "\"0x%05x\"", frame->unmanaged_data.offset);
		}

		if (frame->unmanaged_data.module) {
			mono_json_writer_printf (writer, ",\n");

			assert_has_space ();
			mono_json_writer_indent (writer);
			mono_json_writer_object_key (writer, "native_module");
			mono_json_writer_printf (writer, "\"%s\"", frame->unmanaged_data.module);
		}

		if (frame->unmanaged_data.has_name) {
			mono_json_writer_printf (writer, ",\n");

			assert_has_space ();
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

void
mono_native_state_add_thread (JsonWriter *writer, MonoThreadSummary *thread, MonoContext *ctx, gboolean first_thread, gboolean crashing_thread)
{
	assert_has_space ();

	if (!first_thread) {
		mono_json_writer_printf (writer, ",\n");
	}

	mono_json_writer_indent (writer);
	mono_json_writer_object_begin(writer);

	assert_has_space ();
	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "is_managed");
	mono_json_writer_printf (writer, "%s,\n", thread->is_managed ? "true" : "false");

	assert_has_space ();
	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "crashed");
	mono_json_writer_printf (writer, "%s,\n", crashing_thread ? "true" : "false");

	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "managed_thread_ptr");
	mono_json_writer_printf (writer, "\"0x%x\",\n", (gpointer) thread->managed_thread_ptr);

	assert_has_space ();
	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "thread_info_addr");
	mono_json_writer_printf (writer, "\"0x%x\",\n", (gpointer) thread->info_addr);

	if (thread->error_msg != NULL) {
		assert_has_space ();
		mono_json_writer_indent (writer);
		mono_json_writer_object_key(writer, "dumping_error");
		mono_json_writer_printf (writer, "\"%s\",\n", thread->error_msg);
	}

	if (thread->name [0] != '\0') {
		assert_has_space ();
		mono_json_writer_indent (writer);
		mono_json_writer_object_key(writer, "thread_name");
		mono_json_writer_printf (writer, "\"%s\",\n", thread->name);
	}

	assert_has_space ();
	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "native_thread_id");
	mono_json_writer_printf (writer, "\"0x%x\",\n", (gpointer) thread->native_thread_id);

	if (ctx)
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

	assert_has_space ();
	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "execution_context");
	mono_json_writer_object_begin(writer);

	/*mono_json_writer_indent (writer);*/
	/*mono_json_writer_object_key(writer, "aot_mode");*/
	/*mono_json_writer_printf (writer, "\"%s\",\n", aot_mode);*/

	/*mono_json_writer_indent (writer);*/
	/*mono_json_writer_object_key(writer, "mono_use_llvm");*/
	/*mono_json_writer_printf (writer, "\"%s\",\n", mono_use_llvm ? "true" : "false");*/

	assert_has_space ();
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
	assert_has_space ();
	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "configuration");
	mono_json_writer_object_begin(writer);

	assert_has_space ();
	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "version");

	char *build = mono_get_runtime_callbacks ()->get_runtime_build_info ();
	mono_json_writer_printf (writer, "\"%s\",\n", build);
	g_free (build);

	assert_has_space ();
	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "tlc");
#ifdef MONO_KEYWORD_THREAD
	mono_json_writer_printf (writer, "\"__thread\",\n");
#else
	mono_json_writer_printf (writer, "\"normal\",\n");
#endif /* MONO_KEYWORD_THREAD */

	assert_has_space ();
	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "sigsgev");
#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK
	mono_json_writer_printf (writer, "\"altstack\",\n");
#else
	mono_json_writer_printf (writer, "\"normal\",\n");
#endif

	assert_has_space ();
	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "notifications");
#ifdef HAVE_EPOLL
	mono_json_writer_printf (writer, "\"epoll\",\n");
#elif defined(HAVE_KQUEUE)
	mono_json_writer_printf (writer, "\"kqueue\",\n");
#else
	mono_json_writer_printf (writer, "\"thread+polling\",\n");
#endif

	assert_has_space ();
	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "architecture");
	mono_json_writer_printf (writer, "\"%s\",\n", MONO_ARCHITECTURE);

	assert_has_space ();
	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "disabled_features");
	mono_json_writer_printf (writer, "\"%s\",\n", DISABLED_FEATURES);

	assert_has_space ();
	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "smallconfig");
#ifdef MONO_SMALL_CONFIG
	mono_json_writer_printf (writer, "\"enabled\",\n");
#else
	mono_json_writer_printf (writer, "\"disabled\",\n");
#endif

	assert_has_space ();
	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "bigarrays");
#ifdef MONO_BIG_ARRAYS
	mono_json_writer_printf (writer, "\"enabled\",\n");
#else
	mono_json_writer_printf (writer, "\"disabled\",\n");
#endif

	assert_has_space ();
	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "softdebug");
#if !defined(DISABLE_SDB)
	mono_json_writer_printf (writer, "\"enabled\",\n");
#else
	mono_json_writer_printf (writer, "\"disabled\",\n");
#endif

	assert_has_space ();
	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "interpreter");
#ifndef DISABLE_INTERPRETER
	mono_json_writer_printf (writer, "\"enabled\",\n");
#else
	mono_json_writer_printf (writer, "\"disabled\",\n");
#endif

	assert_has_space ();
	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "llvm_support");
#ifdef MONO_ARCH_LLVM_SUPPORTED
#ifdef ENABLE_LLVM
	mono_json_writer_printf (writer, "\"%d\",\n", LLVM_API_VERSION);
#else
	mono_json_writer_printf (writer, "\"disabled\",\n");
#endif
#endif

	const char *susp_policy = mono_threads_suspend_policy_name ();
	assert_has_space ();
	mono_json_writer_indent (writer);
	mono_json_writer_object_key (writer, "suspend");
	mono_json_writer_printf (writer, "\"%s\"\n", susp_policy);


	assert_has_space ();
	mono_json_writer_indent_pop (writer);
	mono_json_writer_indent (writer);
	mono_json_writer_object_end (writer);
	mono_json_writer_printf (writer, ",\n");
}

static void
mono_native_state_add_memory (JsonWriter *writer)
{
	assert_has_space ();
	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "memory");
	mono_json_writer_object_begin(writer);

#ifdef TARGET_OSX
	struct task_basic_info t_info;
	memset (&t_info, 0, sizeof (t_info));
	mach_msg_type_number_t t_info_count = TASK_BASIC_INFO_COUNT;
	task_name_t task = mach_task_self ();
	task_info(task, TASK_BASIC_INFO, (task_info_t) &t_info, &t_info_count);

	assert_has_space ();
	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "Resident Size");
	mono_json_writer_printf (writer, "\"%lu\",\n", t_info.resident_size);

	assert_has_space ();
	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "Virtual Size");
	mono_json_writer_printf (writer, "\"%lu\",\n", t_info.virtual_size);
#endif

	GCStats stats;
	memcpy (&stats, &mono_gc_stats, sizeof (GCStats));

	assert_has_space ();
	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "minor_gc_time");
	mono_json_writer_printf (writer, "\"%lu\",\n", stats.minor_gc_time);

	assert_has_space ();
	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "major_gc_time");
	mono_json_writer_printf (writer, "\"%lu\",\n", stats.major_gc_time);

	assert_has_space ();
	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "minor_gc_count");
	mono_json_writer_printf (writer, "\"%lu\",\n", stats.minor_gc_count);

	assert_has_space ();
	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "major_gc_count");
	mono_json_writer_printf (writer, "\"%lu\",\n", stats.major_gc_count);

	assert_has_space ();
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
	mono_json_writer_object_begin(writer);

	assert_has_space ();
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
		assert_has_space ();
		mono_json_writer_indent (writer);
		mono_json_writer_object_key(writer, "assertion_message");

		size_t length;
		const char *pos;
		if ((pos = strchr (assertion_msg, '\n')) != NULL)
			length = (size_t)(pos - assertion_msg);
		else
			length = strlen (assertion_msg);
		length = MIN (length, INT_MAX);

		mono_json_writer_printf (writer, "\"%.*s\",\n", (int)length, assertion_msg);
	}

	// Start threads array
	assert_has_space ();
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
mono_native_state_init (JsonWriter *writer)
{
	mono_native_state_add_prologue (writer);
}

char *
mono_native_state_emit (JsonWriter *writer)
{
	mono_native_state_add_epilogue (writer);
	return writer->text->str;
}

char *
mono_native_state_free (JsonWriter *writer, gboolean free_data)
{
	mono_native_state_add_epilogue (writer);
	char *output = NULL;

	// Make this interface work like the g_string free does
	if (!free_data)
		output = g_strdup (writer->text->str);

	mono_json_writer_destroy (writer);
	return output;
}

void
mono_summarize_native_state_begin (gchar *mem, int size)
{
	// Shared global mutable memory, only use when VM crashing
	if (!mem)
		mono_json_writer_init_with_static ();
	else
		mono_json_writer_init_memory (mem, size);

	mono_native_state_init (&writer);
}

char *
mono_summarize_native_state_end (void)
{
	return mono_native_state_emit (&writer);
}

void
mono_summarize_native_state_add_thread (MonoThreadSummary *thread, MonoContext *ctx, gboolean crashing_thread)
{
	static gboolean not_first_thread = FALSE;
	mono_native_state_add_thread (&writer, thread, ctx, !not_first_thread, crashing_thread);
	not_first_thread = TRUE;
}

void
mono_crash_dump (const char *jsonFile, MonoStackHash *hashes)
{
	size_t size = strlen (jsonFile);

	gboolean success = FALSE;

	// Save up to 100 dump files for a given stacktrace hash
	for (int increment = 0; increment < 100; increment++) {
		FILE* fp;
		char *name = g_strdup_printf ("mono_crash.%" PRIx64 ".%d.json", hashes->offset_free_hash, increment);

		if ((fp = fopen (name, "ab"))) {
			if (ftell (fp) == 0) {
				fwrite (jsonFile, size, 1, fp);
				success = TRUE;
			}
		} else {
			// Couldn't make file and file doesn't exist
			g_warning ("Didn't have permission to access %s for file dump\n", name);
		}

		/*cleanup*/
		if (fp)
			fclose (fp);

		g_free (name);

		if (success)
			return;
	}

	return;
}

#endif // DISABLE_CRASH_REPORTING
