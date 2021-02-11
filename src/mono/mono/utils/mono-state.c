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
#include <mono/utils/atomic.h>

#ifndef DISABLE_CRASH_REPORTING

#include <mono/utils/mono-threads-coop.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/mono-config-dirs.h>

#include <sys/param.h>
#include <fcntl.h>
#ifdef HAVE_SYS_STAT_H
#include <sys/stat.h>
#endif
#include <utils/mono-threads-debug.h>

extern GCStats mono_gc_stats;

// For AOT mode
#include <mono/mini/mini-runtime.h>
#include <mono/utils/mono-threads-debug.h>
#include <mono/utils/mono-merp.h>

#ifdef TARGET_OSX
#include <mach/mach.h>
#include <mach/task_info.h>
#endif

#ifdef HAVE_SYS_SYSCTL_H
#include <sys/sysctl.h>
#endif

#ifdef HAVE_SYS_MMAN_H
#include <sys/mman.h>
#endif

#ifdef TARGET_OSX
// OSX 10.9 does not have MAP_ANONYMOUS
#if !defined(MAP_ANONYMOUS)
  #define NO_MAP_ANONYMOUS
  #if defined(MAP_ANON)
    #define MAP_ANONYMOUS MAP_ANON
  #else
    #define MAP_ANONYMOUS 0
  #endif
#endif
#endif

#ifdef HAVE_EXECINFO_H
#include <execinfo.h>
#endif

#if defined(ENABLE_CHECKED_BUILD_CRASH_REPORTING) && defined (ENABLE_OVERRIDABLE_ALLOCATORS)
// Fixme: put behind preprocessor symbol?
static void
assert_not_reached_mem (const char *msg)
{
	g_async_safe_printf ("%s\n", msg);

#if 0
	pid_t crashed_pid = getpid ();
	// Break here
	g_async_safe_printf ("Attach to PID %d. Supervisor thread will signal us shortly.\n", crashed_pid);
	while (TRUE) {
		// Sleep for 1 second.
		g_usleep (1000 * 1000);
	}
#endif

	g_error (msg);
}

static void
assert_not_reached_fn_ptr_free (gpointer ptr)
{
	// Wrap the macro to provide as a function pointer
	assert_not_reached_mem ("Attempted to call free during merp dump");
}

static gpointer
assert_not_reached_fn_ptr_malloc (gsize size)
{
	// Wrap the macro to provide as a function pointer
	assert_not_reached_mem ("Attempted to call malloc during merp dump");
	return NULL;
}

static gpointer
assert_not_reached_fn_ptr_realloc (gpointer obj, gsize size)
{
	// Wrap the macro to provide as a function pointer
	assert_not_reached_mem ("Attempted to call realloc during merp dump");
	return NULL;
}

static gpointer
assert_not_reached_fn_ptr_calloc (gsize n, gsize x)
{
	// Wrap the macro to provide as a function pointer
	assert_not_reached_mem ("Attempted to call calloc during merp dump");
	return NULL;
}
#endif /* defined(ENABLE_CHECKED_BUILD_CRASH_REPORTING) && defined (ENABLE_OVERRIDABLE_ALLOCATORS) */

void
mono_summarize_toggle_assertions (gboolean enable)
{
#if defined(ENABLE_CHECKED_BUILD_CRASH_REPORTING) && defined (ENABLE_OVERRIDABLE_ALLOCATORS)
	static GMemVTable g_mem_vtable_backup;
	static gboolean saved;

	if (enable) {
		g_mem_get_vtable (&g_mem_vtable_backup);
		saved = TRUE;

		GMemVTable g_mem_vtable_assert = { assert_not_reached_fn_ptr_malloc, assert_not_reached_fn_ptr_realloc, assert_not_reached_fn_ptr_free, assert_not_reached_fn_ptr_calloc };
		g_mem_set_vtable (&g_mem_vtable_assert);
	} else if (saved) {
		g_mem_set_vtable (&g_mem_vtable_backup);
		saved = FALSE;
	}

	mono_memory_barrier ();
#endif
}

typedef struct {
	const char *directory;
	MonoSummaryStage level;
} MonoSummaryTimeline;

static const char *configured_timeline_dir;
static MonoSummaryTimeline mlog;

static void
file_for_stage_breadcrumb (const char *directory, MonoSummaryStage stage, gchar *buff, size_t sizeof_buff)
{
	g_snprintf (buff, sizeof_buff, "%s%scrash_stage_%d", directory, G_DIR_SEPARATOR_S, stage);
}

static void
file_for_dump_reason_breadcrumb (const char *directory, const char *dump_reason, gchar *buff, size_t sizeof_buff)
{
	g_snprintf (buff, sizeof_buff, "%s%scrash_reason_%s", directory, G_DIR_SEPARATOR_S, dump_reason);
}

static void
file_for_hash_breadcrumb (const char *directory, MonoStackHash hashes, gchar *buff, size_t sizeof_buff)
{
	g_snprintf (buff, sizeof_buff, "%s%scrash_hash_0x%" PRIx64 "", directory, G_DIR_SEPARATOR_S, (uint64_t)hashes.offset_rich_hash);
}

static void create_breadcrumb (const char *path)
{
	int handle = g_open (path, O_WRONLY | O_CREAT, S_IWUSR | S_IRUSR | S_IRGRP | S_IROTH);
	if (handle < 0) {
		g_async_safe_printf ("Failed to create breadcrumb file %s\n", path);
		return;
	}

	if (close(handle) < 0)
		g_async_safe_printf ("Failed to close breadcrumb file %s\n", path);
}

static void
create_stage_breadcrumb (void)
{
	char out_file [200];
	file_for_stage_breadcrumb (mlog.directory, mlog.level, out_file, sizeof(out_file));
	create_breadcrumb (out_file);
}

static void
create_dump_reason_breadcrumb (const char *dump_reason)
{
	char out_file [200];
	file_for_dump_reason_breadcrumb (mlog.directory, dump_reason, out_file, sizeof(out_file));
	create_breadcrumb (out_file);
}

void
mono_create_crash_hash_breadcrumb (MonoThreadSummary *thread)
{
	char out_file [200];
	file_for_hash_breadcrumb (mlog.directory, thread->hashes, out_file, sizeof(out_file));
	create_breadcrumb (out_file);
}

gboolean
mono_summarize_set_timeline_dir (const char *directory)
{
	if (directory) {
		configured_timeline_dir = strdup (directory);
		return g_ensure_directory_exists (directory);
	} else {
		configured_timeline_dir = NULL;
		return TRUE;
	}
}

void
mono_summarize_timeline_start (const char *dump_reason)
{
	memset (&mlog, 0, sizeof (mlog));

	if (!configured_timeline_dir)
		return;

	mlog.directory = configured_timeline_dir;
	create_dump_reason_breadcrumb (dump_reason);
	mono_summarize_timeline_phase_log (MonoSummarySetup);
}

void
mono_summarize_double_fault_log (void)
{
	mono_summarize_timeline_phase_log (MonoSummaryDoubleFault);
}

void
mono_summarize_timeline_phase_log (MonoSummaryStage next)
{
	if (!mlog.directory)
		return;

	MonoSummaryStage out_level;
	switch (mlog.level) {
		case MonoSummaryNone:
			out_level = MonoSummarySetup;
			break;
		case MonoSummarySetup:
			out_level = MonoSummarySuspendHandshake;
			break;
		case MonoSummarySuspendHandshake:
			out_level = MonoSummaryUnmanagedStacks;
			break;
		case MonoSummaryUnmanagedStacks:
			out_level = MonoSummaryManagedStacks;
			break;
		case MonoSummaryManagedStacks:
			out_level = MonoSummaryStateWriter;
			break;
		case MonoSummaryStateWriter:
			out_level = MonoSummaryStateWriterDone;
			break;
		case MonoSummaryStateWriterDone:
#ifdef TARGET_OSX
			if (mono_merp_enabled ()) {
				out_level = MonoSummaryMerpWriter;
			} else
#endif
			{
				out_level = MonoSummaryCleanup;
			}
			break;
		case MonoSummaryMerpWriter:
			out_level = MonoSummaryMerpInvoke;
			break;
		case MonoSummaryMerpInvoke:
			out_level = MonoSummaryCleanup;
			break;
		case MonoSummaryCleanup:
			out_level = MonoSummaryDone;
			break;

		case MonoSummaryDone:
			g_async_safe_printf ("Trying to log crash reporter timeline, already at done %d\n", mlog.level);
			return;
		default:
			g_async_safe_printf ("Trying to log crash reporter timeline, illegal state %d\n", mlog.level);
			return;
	}

	g_assertf(out_level == next || next == MonoSummaryDoubleFault, "Log Error: Log transition to %d, actual expected next step is %d\n", next, out_level);

	mlog.level = out_level;
	create_stage_breadcrumb ();
	// To check, comment out normally
	// DO NOT MERGE UNCOMMENTED
	// As this does a lot of FILE io
	//
	// g_assert (out_level == mono_summarize_timeline_read_level (mlog.directory,  FALSE));

	if (out_level == MonoSummaryDone)
		memset (&mlog, 0, sizeof (mlog));

	return;
}

static void
mem_file_name (long tag, char *name, size_t limit)
{
	name [0] = '\0';
	pid_t pid = getpid ();
	g_snprintf (name, limit, "mono_crash.mem.%d.%lx.blob", pid, tag);
}

gboolean
mono_state_alloc_mem (MonoStateMem *mem, long tag, size_t size)
{
	char name [100];
	mem_file_name (tag, name, sizeof (name));

	memset (mem, 0, sizeof (*mem));
	mem->tag = tag;
	mem->size = size;
	mem->handle = 0;

	if (!g_hasenv ("MONO_CRASH_NOFILE"))
		mem->handle = g_open (name, O_RDWR | O_CREAT | O_EXCL, S_IWUSR | S_IRUSR | S_IRGRP | S_IROTH);

	if (mem->handle < 1) {
		mem->mem = (gpointer *) mmap (0, mem->size, PROT_READ | PROT_WRITE, MAP_PRIVATE | MAP_ANONYMOUS, -1, 0);
	} else {
		lseek (mem->handle, mem->size, SEEK_SET);
		g_write (mem->handle, "", 1);

		mem->mem = (gpointer *) mmap (0, mem->size, PROT_READ | PROT_WRITE, MAP_SHARED, mem->handle, 0);
	}
	if (mem->mem == GINT_TO_POINTER (-1))
		return FALSE;

	return TRUE;
}

void
mono_state_free_mem (MonoStateMem *mem)
{
	if (!mem->mem)
		return;

	munmap (mem->mem, mem->size);

	// Note: We aren't calling msync on this file.
	// There is no guarantee that we're going to persist
	// changes to it at all, in the case that we fail before
	// removing it. Don't try to debug where in the crash we were
	// by the file contents.
	if (mem->handle)
		close (mem->handle);
	else
		g_async_safe_printf ("NULL handle mono-state mem on freeing\n");

	char name [100];
	mem_file_name (mem->tag, name, sizeof (name));
	unlink (name);
}

static gboolean 
timeline_has_level (const char *directory, char *log_file, size_t log_file_size, gboolean clear, MonoSummaryStage stage)
{
	memset (log_file, 0, log_file_size);
	file_for_stage_breadcrumb (directory, stage, log_file, log_file_size);
	gboolean exists = g_file_test (log_file, G_FILE_TEST_EXISTS);
	if (clear && exists) 
		remove (log_file);

	return exists;
}

MonoSummaryStage
mono_summarize_timeline_read_level (const char *directory, gboolean clear)
{
	char out_file [200];

	if (!directory)
		directory = mlog.directory;

	if (!directory)
		return MonoSummaryNone;

	// Make sure that clear gets to erase all of these files if they exist
	gboolean has_level_done = timeline_has_level (directory, out_file, sizeof(out_file), clear, MonoSummaryDone);
	gboolean has_level_cleanup = timeline_has_level (directory, out_file, sizeof(out_file), clear, MonoSummaryCleanup);
	gboolean has_level_merp_invoke = timeline_has_level (directory, out_file, sizeof(out_file), clear, MonoSummaryMerpInvoke);
	gboolean has_level_merp_writer = timeline_has_level (directory, out_file, sizeof(out_file), clear, MonoSummaryMerpWriter);
	gboolean has_level_state_writer = timeline_has_level (directory, out_file, sizeof(out_file), clear, MonoSummaryStateWriter);
	gboolean has_level_state_writer_done = timeline_has_level (directory, out_file, sizeof(out_file), clear, MonoSummaryStateWriterDone);
	gboolean has_level_managed_stacks = timeline_has_level (directory, out_file, sizeof(out_file), clear, MonoSummaryManagedStacks);
	gboolean has_level_unmanaged_stacks = timeline_has_level (directory, out_file, sizeof(out_file), clear, MonoSummaryUnmanagedStacks);
	gboolean has_level_suspend_handshake = timeline_has_level (directory, out_file, sizeof(out_file), clear, MonoSummarySuspendHandshake);
	gboolean has_level_setup = timeline_has_level (directory, out_file, sizeof(out_file), clear, MonoSummarySetup);

	if (has_level_done)
		return MonoSummaryDone;
	else if (has_level_cleanup)
		return MonoSummaryCleanup;
	else if (has_level_merp_invoke)
		return MonoSummaryMerpInvoke;
	else if (has_level_merp_writer)
		return MonoSummaryMerpWriter;
	else if (has_level_state_writer_done)
		return MonoSummaryStateWriterDone;
	else if (has_level_state_writer)
		return MonoSummaryStateWriter;
	else if (has_level_managed_stacks)
		return MonoSummaryManagedStacks;
	else if (has_level_unmanaged_stacks)
		return MonoSummaryUnmanagedStacks;
	else if (has_level_suspend_handshake)
		return MonoSummarySuspendHandshake;
	else if (has_level_setup)
		return MonoSummarySetup;
	else
		return MonoSummaryNone;
}

static void 
assert_has_space (MonoStateWriter *writer)
{
	// Each individual key/value append should be roughly less than this many characters
	const int margin = 35;

	// Not using static, exit
	if (writer->allocated_len == 0)
		return;

	g_assertf (writer->allocated_len - writer->len >= margin, "Ran out of memory to create crash dump json blob. Current state:\n%s\n", writer->output_str);
}

static void 
mono_state_writer_printf (MonoStateWriter *writer, const gchar *format, ...)
{
	g_assert (writer->len == strlen(writer->output_str));

	va_list args;
	va_start (args, format);
	int written = vsnprintf (&writer->output_str [writer->len], writer->allocated_len - writer->len, format, args);
	va_end (args);

	if (written > 0) writer->len += written;
	g_assert (writer->len == strlen (writer->output_str));
}

static void 
mono_state_writer_indent (MonoStateWriter *writer)
{
	for (int i = 0; i < writer->indent; ++i)
		mono_state_writer_printf(writer, " ");
}

static void 
mono_state_writer_object_key (MonoStateWriter *writer, const char *key) 
{
	mono_state_writer_indent (writer);
	mono_state_writer_printf(writer, "\"%s\" : ", key);
}

static void
mono_native_state_add_ctx (MonoStateWriter *writer, MonoContext *ctx)
{
	// Context
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "ctx");
	mono_state_writer_printf(writer, "{\n");
	writer->indent++;

	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "IP");
	mono_state_writer_printf(writer, "\"%p\",\n", (gpointer) MONO_CONTEXT_GET_IP (ctx));

	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "SP");
	mono_state_writer_printf(writer, "\"%p\",\n", (gpointer) MONO_CONTEXT_GET_SP (ctx));

	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "BP");
	mono_state_writer_printf(writer, "\"%p\"\n", (gpointer) MONO_CONTEXT_GET_BP (ctx));

	writer->indent--;
	mono_state_writer_indent (writer);
	mono_state_writer_printf(writer, "}");
}

static void
mono_native_state_add_frame (MonoStateWriter *writer, MonoFrameSummary *frame)
{
	mono_state_writer_indent (writer);
	mono_state_writer_printf(writer, "{\n");
	writer->indent++;

	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "is_managed");
	mono_state_writer_printf(writer, "\"%s\",", frame->is_managed ? "true" : "false");

	if (frame->unmanaged_data.is_trampoline) {
		mono_state_writer_printf(writer, "\n");
		assert_has_space (writer);
		mono_state_writer_indent (writer);
		mono_state_writer_object_key (writer, "is_trampoline");
		mono_state_writer_printf(writer, "\"true\",");
	}

	if (frame->is_managed) {
		mono_state_writer_printf(writer, "\n");
		assert_has_space (writer);
		mono_state_writer_indent (writer);
		mono_state_writer_object_key (writer, "guid");
		mono_state_writer_printf(writer, "\"%s\",\n", frame->managed_data.guid);

		assert_has_space (writer);
		mono_state_writer_indent (writer);
		mono_state_writer_object_key (writer, "token");
		mono_state_writer_printf(writer, "\"0x%05x\",\n", frame->managed_data.token);

		assert_has_space (writer);
		mono_state_writer_indent (writer);
		mono_state_writer_object_key (writer, "native_offset");
		mono_state_writer_printf(writer, "\"0x%x\",\n", frame->managed_data.native_offset);

#ifndef MONO_PRIVATE_CRASHES
		if (frame->managed_data.name != NULL) {
			assert_has_space (writer);
			mono_state_writer_indent (writer);
			mono_state_writer_object_key (writer, "method_name");
			mono_state_writer_printf(writer, "\"%s\",\n", frame->managed_data.name);
		}
#endif

		assert_has_space (writer);
		mono_state_writer_indent (writer);
		mono_state_writer_object_key (writer, "filename");
		mono_state_writer_printf(writer, "\"%s\",\n", frame->managed_data.filename);

		assert_has_space (writer);
		mono_state_writer_indent (writer);
		mono_state_writer_object_key (writer, "sizeofimage");
		mono_state_writer_printf(writer, "\"0x%x\",\n", frame->managed_data.image_size);

		assert_has_space (writer);
		mono_state_writer_indent (writer);
		mono_state_writer_object_key (writer, "timestamp");
		mono_state_writer_printf(writer, "\"0x%x\",\n", frame->managed_data.time_date_stamp);

		assert_has_space (writer);
		mono_state_writer_indent (writer);
		mono_state_writer_object_key (writer, "il_offset");
		mono_state_writer_printf(writer, "\"0x%05x\"\n", frame->managed_data.il_offset);

	} else {
		mono_state_writer_printf(writer, "\n");
		assert_has_space (writer);
		mono_state_writer_indent (writer);
		mono_state_writer_object_key (writer, "native_address");
		if (frame->unmanaged_data.ip) {
			mono_state_writer_printf(writer, "\"0x%" PRIx64 "\"", (guint64) frame->unmanaged_data.ip);
		} else
			mono_state_writer_printf(writer, "\"unregistered\"");

		if (frame->unmanaged_data.ip) {
			mono_state_writer_printf(writer, ",\n");

			assert_has_space (writer);
			mono_state_writer_indent (writer);
			mono_state_writer_object_key (writer, "native_offset");
			mono_state_writer_printf(writer, "\"0x%05x\"", frame->unmanaged_data.offset);
		}

		if (frame->unmanaged_data.module [0] != '\0') {
			mono_state_writer_printf(writer, ",\n");

			assert_has_space (writer);
			mono_state_writer_indent (writer);
			mono_state_writer_object_key (writer, "native_module");
			mono_state_writer_printf(writer, "\"%s\"", frame->unmanaged_data.module);
		}

		if (frame->unmanaged_data.has_name) {
			mono_state_writer_printf(writer, ",\n");

			assert_has_space (writer);
			mono_state_writer_indent (writer);
			mono_state_writer_object_key (writer, "unmanaged_name");
			mono_state_writer_printf(writer, "\"%s\"\n", frame->str_descr);
		} else {
			mono_state_writer_printf(writer, "\n");
		}
	}

	mono_state_writer_indent (writer);
	writer->indent--;
	mono_state_writer_printf(writer, "}\n");
}

static void
mono_native_state_add_frames (MonoStateWriter *writer, int num_frames, MonoFrameSummary *frames, const char *label)
{
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, label);

	mono_state_writer_printf(writer, "[\n");

	for (int i = 0; i < num_frames; ++i) {
		if (i > 0)
			mono_state_writer_printf(writer, ",\n");
		mono_native_state_add_frame (writer, &frames [i]);
	}
	mono_state_writer_printf(writer, "\n");

	mono_state_writer_indent (writer);
	writer->indent--;
	mono_state_writer_printf(writer, "]");
}

static void
mono_native_state_add_managed_exc (MonoStateWriter *writer, MonoExcSummary *exc)
{
	mono_state_writer_indent (writer);
	mono_state_writer_printf(writer, "{\n");
	writer->indent++;

	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "type");
	mono_state_writer_printf(writer, "\"%s.%s\",\n", m_class_get_name_space (exc->managed_exc_type), m_class_get_name (exc->managed_exc_type));

	mono_native_state_add_frames (writer, exc->num_managed_frames, exc->managed_frames, "managed_frames");

	mono_state_writer_indent (writer);
	writer->indent--;
	mono_state_writer_printf(writer, "}\n");
}

static void
mono_native_state_add_managed_excs (MonoStateWriter *writer, int num_excs, MonoExcSummary *excs)
{
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "exceptions");

	mono_state_writer_printf(writer, "[\n");

	for (int i = 0; i < num_excs; ++i) {
		if (i > 0)
			mono_state_writer_printf(writer, ",\n");
		mono_native_state_add_managed_exc (writer, &excs [i]);
	}

	mono_state_writer_indent (writer);
	writer->indent--;
	mono_state_writer_printf(writer, "]");
}


void
mono_native_state_add_thread (MonoStateWriter *writer, MonoThreadSummary *thread, MonoContext *ctx, gboolean first_thread, gboolean crashing_thread)
{
	assert_has_space (writer);

	if (!first_thread) {
		mono_state_writer_printf(writer, ",\n");
	}

	mono_state_writer_indent (writer);
	mono_state_writer_printf(writer, "{\n");
	writer->indent++;

	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "is_managed");
	mono_state_writer_printf(writer, "%s,\n", thread->is_managed ? "true" : "false");

	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "offset_free_hash");
	mono_state_writer_printf(writer, "\"0x%" PRIx64 "\",\n", thread->hashes.offset_free_hash);

	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "offset_rich_hash");
	mono_state_writer_printf(writer, "\"0x%" PRIx64 "\",\n", thread->hashes.offset_rich_hash);

	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "crashed");
	mono_state_writer_printf(writer, "%s,\n", crashing_thread ? "true" : "false");

	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "native_thread_id");
	mono_state_writer_printf(writer, "\"0x%" PRIx64 "\",\n", (guint64) thread->native_thread_id);

	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "thread_info_addr");
	mono_state_writer_printf(writer, "\"0x%" PRIx64 "\"", (guint64) thread->info_addr);

	if (thread->error_msg != NULL) {
		mono_state_writer_printf(writer, ",\n");
		assert_has_space (writer);
		mono_state_writer_indent (writer);
		mono_state_writer_object_key (writer, "dumping_error");
		mono_state_writer_printf(writer, "\"%s\"", thread->error_msg);
	}

	if (thread->name [0] != '\0') {
		mono_state_writer_printf(writer, ",\n");
		assert_has_space (writer);
		mono_state_writer_indent (writer);
		mono_state_writer_object_key (writer, "thread_name");
		mono_state_writer_printf(writer, "\"%s\"", thread->name);
	}

	if (ctx) {
		mono_state_writer_printf(writer, ",\n");
		mono_native_state_add_ctx (writer, ctx);
	}

	if (thread->num_exceptions > 0) {
		mono_state_writer_printf(writer, ",\n");
		mono_native_state_add_managed_excs (writer, thread->num_exceptions, thread->exceptions);
	}

	if (thread->num_managed_frames > 0) {
		mono_state_writer_printf(writer, ",\n");
		mono_native_state_add_frames (writer, thread->num_managed_frames, thread->managed_frames, "managed_frames");
	}

	if (thread->num_unmanaged_frames > 0) {
		mono_state_writer_printf(writer, ",\n");
		mono_native_state_add_frames (writer, thread->num_unmanaged_frames, thread->unmanaged_frames, "unmanaged_frames");
	}

	mono_state_writer_printf(writer, "\n");

	mono_state_writer_indent (writer);
	mono_state_writer_printf(writer, "}");
}

static void
mono_native_state_add_ee_info  (MonoStateWriter *writer)
{
#ifndef MONO_PRIVATE_CRASHES
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

	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "execution_context");
	mono_state_writer_printf(writer, "{\n");
	writer->indent++;

	/*mono_state_writer_indent (writer);*/
	/*mono_state_writer_object_key (writer, "aot_mode");*/
	/*mono_state_writer_printf(writer, "\"%s\",\n", aot_mode);*/

	/*mono_state_writer_indent (writer);*/
	/*mono_state_writer_object_key (writer, "mono_use_llvm");*/
	/*mono_state_writer_printf(writer, "\"%s\",\n", mono_use_llvm ? "true" : "false");*/

	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "coop-enabled");
	mono_state_writer_printf(writer, "\"%s\"\n", mono_threads_is_cooperative_suspension_enabled () ? "true" : "false");

	writer->indent--;
	mono_state_writer_indent (writer);
	mono_state_writer_printf(writer, "},\n");
#endif
}

// Taken from driver.c
#if defined(MONO_ARCH_ARCHITECTURE)
/* Redefine MONO_ARCHITECTURE to include more information */
#undef MONO_ARCHITECTURE
#define MONO_ARCHITECTURE MONO_ARCH_ARCHITECTURE
#endif

static void
mono_native_state_add_version (MonoStateWriter *writer)
{
	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "configuration");
	mono_state_writer_printf(writer, "{\n");
	writer->indent++;

	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "version");
	mono_state_writer_printf(writer, "\"(%s) (%s)\",\n", VERSION, mono_get_runtime_callbacks ()->get_runtime_build_version ());

	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "tlc");
#ifdef MONO_KEYWORD_THREAD
	mono_state_writer_printf(writer, "\"__thread\",\n");
#else
	mono_state_writer_printf(writer, "\"normal\",\n");
#endif /* MONO_KEYWORD_THREAD */

	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "sigsgev");
#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK
	mono_state_writer_printf(writer, "\"altstack\",\n");
#else
	mono_state_writer_printf(writer, "\"normal\",\n");
#endif

	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "notifications");
#ifdef HAVE_EPOLL
	mono_state_writer_printf(writer, "\"epoll\",\n");
#elif defined(HAVE_KQUEUE)
	mono_state_writer_printf(writer, "\"kqueue\",\n");
#else
	mono_state_writer_printf(writer, "\"thread+polling\",\n");
#endif

	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "architecture");
	mono_state_writer_printf(writer, "\"%s\",\n", MONO_ARCHITECTURE);

	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "disabled_features");
	mono_state_writer_printf(writer, "\"%s\",\n", DISABLED_FEATURES);

	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "smallconfig");
#ifdef MONO_SMALL_CONFIG
	mono_state_writer_printf(writer, "\"enabled\",\n");
#else
	mono_state_writer_printf(writer, "\"disabled\",\n");
#endif

	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "bigarrays");
#ifdef MONO_BIG_ARRAYS
	mono_state_writer_printf(writer, "\"enabled\",\n");
#else
	mono_state_writer_printf(writer, "\"disabled\",\n");
#endif

	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "softdebug");
#if !defined(DISABLE_SDB)
	mono_state_writer_printf(writer, "\"enabled\",\n");
#else
	mono_state_writer_printf(writer, "\"disabled\",\n");
#endif

	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "interpreter");
#ifndef DISABLE_INTERPRETER
	mono_state_writer_printf(writer, "\"enabled\",\n");
#else
	mono_state_writer_printf(writer, "\"disabled\",\n");
#endif

	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "llvm_support");
#ifdef MONO_ARCH_LLVM_SUPPORTED
#ifdef ENABLE_LLVM
	mono_state_writer_printf(writer, "\"%d\",\n", LLVM_API_VERSION);
#else
	mono_state_writer_printf(writer, "\"disabled\",\n");
#endif
#endif

	const char *susp_policy = mono_threads_suspend_policy_name (mono_threads_suspend_policy ());
	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "suspend");
	mono_state_writer_printf(writer, "\"%s\"\n", susp_policy);

	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_printf(writer, "},\n");
	writer->indent--;
}

static void
mono_native_state_add_memory (MonoStateWriter *writer)
{
	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "memory");
	mono_state_writer_printf(writer, "{\n");
	writer->indent++;

#ifdef TARGET_OSX
	struct task_basic_info t_info;
	memset (&t_info, 0, sizeof (t_info));
	mach_msg_type_number_t t_info_count = TASK_BASIC_INFO_COUNT;
	task_name_t task = mach_task_self ();
	task_info(task, TASK_BASIC_INFO, (task_info_t) &t_info, &t_info_count);

	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "Resident Size");
	mono_state_writer_printf(writer, "\"%lu\",\n", t_info.resident_size);

	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "Virtual Size");
	mono_state_writer_printf(writer, "\"%lu\",\n", t_info.virtual_size);
#endif

	GCStats stats;
	memcpy (&stats, &mono_gc_stats, sizeof (GCStats));

	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "minor_gc_time");
	mono_state_writer_printf(writer, "\"%" PRId64 "\",\n", (gint64)stats.minor_gc_time);

	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "major_gc_time");
	mono_state_writer_printf(writer, "\"%" PRId64 "\",\n", (gint64)stats.major_gc_time);

	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "minor_gc_count");
	mono_state_writer_printf(writer, "\"%d\",\n", stats.minor_gc_count);

	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "major_gc_count");
	mono_state_writer_printf(writer, "\"%d\",\n", stats.major_gc_count);

	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "major_gc_time_concurrent");
	mono_state_writer_printf(writer, "\"%" PRId64 "\"\n", (gint64)stats.major_gc_time_concurrent);

	writer->indent--;
	mono_state_writer_indent (writer);
	mono_state_writer_printf(writer, "},\n");
}

#define MONO_CRASH_REPORTING_MAPPING_LINE_LIMIT 30

#if !MONO_PRIVATE_CRASHES

static void
mono_native_state_add_process_map (MonoStateWriter *writer)
{
#if defined(__linux__) && !defined(HOST_ANDROID)
	int handle = g_open ("/proc/self/maps", O_RDONLY, S_IWUSR | S_IRUSR | S_IRGRP | S_IROTH);
	if (handle == -1) {
		g_async_safe_printf ("Couldn't find /proc/self/maps on Linux system. Continuing.");
		return;
	}

	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "process_map");
	mono_state_writer_printf(writer, "[\n");

	int mapping = 0;
	while (mapping < MONO_CRASH_REPORTING_MAPPING_LINE_LIMIT) {
		if (mapping > 0)
			mono_state_writer_printf (writer, "\",\n");

		mono_state_writer_printf (writer, "\t\"");

		while (TRUE) {
			char line [10];
			gboolean newline = FALSE;
			int charsCopied = g_async_safe_fgets (line, sizeof (line), handle, &newline);

			if (charsCopied == 0)
				break;

			for (int i=0; i < charsCopied; i++)
				g_assert (isprint (line [i]));

			g_assert (line [charsCopied] == '\0');

			mono_state_writer_printf (writer, "%s", line);

			if (newline)
				break;
		}

		mapping++;
	}

	if (mapping > 0)
		mono_state_writer_printf (writer, "\"");

	mono_state_writer_indent (writer);
	writer->indent--;
	mono_state_writer_printf(writer, "],\n");

	close (handle);
#endif
}

#endif

static void
mono_native_state_add_logged_message (MonoStateWriter *writer, const char *object_key, const char *msg)
{
	if (msg != NULL) {
		assert_has_space (writer);
		mono_state_writer_indent (writer);
		mono_state_writer_object_key (writer, object_key);

		size_t length;
		const char *pos;
		if ((pos = strchr (msg, '\n')) != NULL)
			length = (size_t)(pos - msg);
		else
			length = strlen (msg);
		length = MIN (length, INT_MAX);

		mono_state_writer_printf(writer, "\"%.*s\",\n", (int)length, msg);
	}
}

static void
mono_native_state_add_prologue (MonoStateWriter *writer)
{
	mono_state_writer_printf(writer, "{\n");
	writer->indent++;

	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "protocol_version");
	mono_state_writer_printf(writer, "\"%s\",\n", MONO_NATIVE_STATE_PROTOCOL_VERSION);

	mono_native_state_add_version (writer);

	mono_native_state_add_ee_info (writer);

	mono_native_state_add_memory (writer);

	const char *assertion_msg = g_get_assertion_message ();
	mono_native_state_add_logged_message (writer, "assertion_message", assertion_msg);

	const char *failfast_msg = mono_crash_get_failfast_msg ();
	mono_native_state_add_logged_message (writer, "failfast_message", failfast_msg);
	

#ifndef MONO_PRIVATE_CRASHES
	mono_native_state_add_process_map (writer);
#endif

	// Start threads array
	assert_has_space (writer);
	mono_state_writer_indent (writer);
	mono_state_writer_object_key (writer, "threads");
	mono_state_writer_printf(writer, "[\n");
}

static void
mono_native_state_add_epilogue (MonoStateWriter *writer)
{
	mono_state_writer_printf(writer, "\n");
	mono_state_writer_indent (writer);
	mono_state_writer_printf(writer, "]\n");
	writer->indent--;

	writer->indent--;
	mono_state_writer_indent (writer);
	mono_state_writer_printf(writer, "}");
}

void
mono_native_state_init (MonoStateWriter *writer)
{
	mono_native_state_add_prologue (writer);
}

char *
mono_native_state_emit (MonoStateWriter *writer)
{
	mono_native_state_add_epilogue (writer);
	return writer->output_str;
}

char *
mono_native_state_free (MonoStateWriter *writer, gboolean free_data)
{
	mono_native_state_add_epilogue (writer);
	char *output = NULL;

	// Make this interface work like the g_string free does
	if (!free_data)
		output = g_strdup (writer->output_str);

	return output;
}

void 
mono_state_writer_init (MonoStateWriter *writer, gchar *output_str, int len)
{
	memset(writer, 0, sizeof(*writer));
	memset(output_str, 0, len * sizeof(gchar));

	writer->output_str = output_str;
	writer->allocated_len = len;
	writer->len = 0;
	writer->indent = 0;
}

void
mono_summarize_native_state_begin (MonoStateWriter *writer, gchar *mem, int size)
{
	g_assert (mem);
	mono_state_writer_init (writer, mem, size);
	mono_native_state_init (writer);
}

char *
mono_summarize_native_state_end (MonoStateWriter *writer)
{
	return mono_native_state_emit (writer);
}

void
mono_summarize_native_state_add_thread (MonoStateWriter *writer, MonoThreadSummary *thread, MonoContext *ctx, gboolean crashing_thread)
{
	static gboolean not_first_thread = FALSE;
	mono_native_state_add_thread (writer, thread, ctx, !not_first_thread, crashing_thread);
	not_first_thread = TRUE;
}

void
mono_crash_dump (const char *jsonFile, MonoStackHash *hashes)
{
	if (g_hasenv ("MONO_CRASH_NOFILE"))
		return;

	size_t size = strlen (jsonFile);

	gboolean success = FALSE;

	// Save up to 100 dump files for a given stacktrace hash
	for (int increment = 0; increment < 100; increment++) {
		char name [100]; 
		name [0] = '\0';
		g_snprintf (name, sizeof (name), "mono_crash.%" PRIx64 ".%d.json", hashes->offset_free_hash, increment);

		int handle = g_open (name, O_WRONLY | O_CREAT | O_EXCL, S_IWUSR | S_IRUSR | S_IRGRP | S_IROTH);
		if (handle != -1) {
			g_write (handle, jsonFile, (guint32) size);
			success = TRUE;
		}

		/*cleanup*/
		if (handle)
			close (handle);

		if (success)
			return;
	}

	g_assertf (!success, "Couldn't create any of (many) attempted crash files\n");
	return;
}

static char *saved_failfast_msg;

/**
 * mono_crash_save_failfast_msg:
 * \param msg the message to save.  Takes ownership, caller shouldn't free
 *
 * \returns the previous message - caller is responsible for freeing.
 */
char*
mono_crash_save_failfast_msg (char *msg)
{
	return (char*) mono_atomic_xchg_ptr ((gpointer*)&saved_failfast_msg, (void*)msg);
}

const char*
mono_crash_get_failfast_msg (void)
{
	return saved_failfast_msg;
}

#endif // DISABLE_CRASH_REPORTING

static volatile int32_t dump_status;

gboolean
mono_dump_start (void)
{
	return (mono_atomic_xchg_i32(&dump_status, 1) == 0);  // return true if we started the dump
}

gboolean
mono_dump_complete (void)
{
	return (mono_atomic_xchg_i32(&dump_status, 0) == 1);  // return true if we completed the dump
}
