/*
 * Licensed to the .NET Foundation under one or more agreements.
 * The .NET Foundation licenses this file to you under the MIT license.
 */

#include <config.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/profiler-legacy.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/debug-internals.h>
#include <mono/utils/mono-dl.h>
#include <mono/utils/mono-error-internals.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/w32subset.h>

MonoProfilerState mono_profiler_state;

typedef void (*MonoProfilerInitializer) (const char *);

#define OLD_INITIALIZER_NAME "mono_profiler_startup"
#define NEW_INITIALIZER_NAME "mono_profiler_init"

static gboolean
load_profiler (MonoDl *module, const char *name, const char *desc)
{
	g_assert (module);

	char *old_name = g_strdup_printf (OLD_INITIALIZER_NAME);
	MonoProfilerInitializer func;

	ERROR_DECL (symbol_error);
	func = (MonoProfilerInitializer)mono_dl_symbol (module, old_name, symbol_error);
	mono_error_cleanup (symbol_error);

	if (func) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_PROFILER, "Found old-style startup symbol '%s' for the '%s' profiler; it has not been migrated to the new API.", old_name, name);
		g_free (old_name);
		return FALSE;
	}

	g_free (old_name);

	char *new_name = g_strdup_printf (NEW_INITIALIZER_NAME "_%s", name);

	error_init_reuse (symbol_error);
	func = (MonoProfilerInitializer)mono_dl_symbol (module, new_name, symbol_error);
	mono_error_cleanup (symbol_error);

	if (!func) {
		g_free (new_name);
		return FALSE;
	}

	g_free (new_name);

	func (desc);

	return TRUE;
}

static gboolean
load_profiler_from_executable (const char *name, const char *desc)
{
	ERROR_DECL (load_error);

	/*
	 * Some profilers (such as ours) may need to call back into the runtime
	 * from their sampling callback (which is called in async-signal context).
	 * They need to be able to know that all references back to the runtime
	 * have been resolved; otherwise, calling runtime functions may result in
	 * invoking the dynamic linker which is not async-signal-safe. Passing
	 * MONO_DL_EAGER will ask the dynamic linker to resolve everything upfront.
	 */
	MonoDl *module = mono_dl_open (NULL, MONO_DL_EAGER, load_error);

	if (!module) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_PROFILER, "Could not open main executable: %s", mono_error_get_message_without_fields (load_error));
		mono_error_cleanup (load_error);
		return FALSE;
	}

	mono_error_assert_ok (load_error);
	return load_profiler (module, name, desc);
}

static gboolean
load_profiler_from_directory (const char *directory, const char *libname, const char *name, const char *desc)
{
	char *path;
	void *iter = NULL;

	while ((path = mono_dl_build_path (directory, libname, &iter))) {
		ERROR_DECL (load_error);
		MonoDl *module = mono_dl_open (path, MONO_DL_EAGER, load_error);

		if (!module) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_PROFILER, "Could not open from directory \"%s\": %s", path, mono_error_get_message_without_fields (load_error));
			mono_error_cleanup (load_error);
			g_free (path);
			continue;
		}
		mono_error_assert_ok (load_error);

		g_free (path);
		return load_profiler (module, name, desc);
	}

	return FALSE;
}

static gboolean
load_profiler_from_installation (const char *libname, const char *name, const char *desc)
{
	ERROR_DECL (load_error);

	MonoDl *module = mono_dl_open_runtime_lib (libname, MONO_DL_EAGER, load_error);

	if (!module) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_PROFILER, "Could not open from installation: %s", mono_error_get_message_without_fields (load_error));
		mono_error_cleanup (load_error);
		return FALSE;
	}

	mono_error_assert_ok (load_error);
	return load_profiler (module, name, desc);
}

/**
 * mono_profiler_load:
 *
 * Loads a profiler module based on the specified description. \p desc can be
 * of the form \c name:args or just \c name. For example, \c log:sample and
 * \c log will both load \c libmono-profiler-log.so. The description is passed
 * to the module after it has been loaded. If the specified module has already
 * been loaded, this function has no effect.
 *
 * A module called \c foo should declare an entry point like so:
 *
 * \code
 * void mono_profiler_init_foo (const char *desc)
 * {
 * }
 * \endcode
 *
 * This function is \b not async safe.
 *
 * This function may \b only be called by embedders prior to running managed
 * code.
 */
void
mono_profiler_load (const char *desc)
{
	const char *col;
	char *mname, *libname;

	mname = libname = NULL;

	if (!desc || !strcmp ("default", desc))
#if HAVE_API_SUPPORT_WIN32_PIPE_OPEN_CLOSE && !defined (HOST_WIN32)
		desc = "log:report";
#else
		desc = "log";
#endif

	if ((col = strchr (desc, ':')) != NULL) {
		mname = (char *) g_memdup (desc, GPTRDIFF_TO_UINT (col - desc + 1));
		mname [col - desc] = 0;
	} else {
		mname = g_strdup (desc);
	}

	if (load_profiler_from_executable (mname, desc))
		goto done;

	libname = g_strdup_printf ("mono-profiler-%s", mname);

	if (load_profiler_from_installation (libname, mname, desc))
		goto done;

	if (load_profiler_from_directory (NULL, libname, mname, desc))
		goto done;

	mono_trace (G_LOG_LEVEL_CRITICAL, MONO_TRACE_PROFILER, "The '%s' profiler wasn't found in the main executable nor could it be loaded from '%s'.", mname, libname);

done:
	g_free (mname);
	g_free (libname);
}

/**
 * mono_profiler_create:
 *
 * Installs a profiler and returns a handle for it. The handle is used with the
 * other functions in the profiler API (e.g. for setting up callbacks). The
 * given structure pointer, \p prof, will be passed to all callbacks from the
 * profiler API. It can be \c NULL.
 *
 * Example usage:
 *
 * \code
 * struct _MonoProfiler {
 * 	int my_stuff;
 * 	// ...
 * };
 *
 * MonoProfiler *prof = malloc (sizeof (MonoProfiler));
 * prof->my_stuff = 42;
 * MonoProfilerHandle handle = mono_profiler_create (prof);
 * mono_profiler_set_shutdown_callback (handle, my_shutdown_cb);
 * \endcode
 *
 * This function is \b not async safe.
 *
 * This function may \b only be called from a profiler's init function or prior
 * to running managed code.
 */
MonoProfilerHandle
mono_profiler_create (MonoProfiler *prof)
{
	MonoProfilerHandle handle = g_new0 (struct _MonoProfilerDesc, 1);

	handle->prof = prof;
	handle->next = mono_profiler_state.profilers;

	mono_profiler_state.profilers = handle;

	return handle;
}

/**
 * mono_profiler_set_cleanup_callback:
 *
 * Sets a profiler cleanup function. This function will be invoked at shutdown
 * when the profiler API is cleaning up its internal structures. It's mainly
 * intended for a profiler to free the structure pointer that was passed to
 * \c mono_profiler_create, if necessary.
 *
 * This function is async safe.
 */
void
mono_profiler_set_cleanup_callback (MonoProfilerHandle handle, MonoProfilerCleanupCallback cb)
{
	mono_atomic_store_ptr (&handle->cleanup_callback, (gpointer) cb);
}

/**
 * mono_profiler_enable_coverage:
 *
 * Enables support for code coverage instrumentation. At the moment, this means
 * enabling the debug info subsystem. If this function is not called, it will
 * not be possible to use \c mono_profiler_get_coverage_data. Returns \c TRUE
 * if code coverage support was enabled, or \c FALSE if the function was called
 * too late for this to be possible.
 *
 * This function is \b not async safe.
 *
 * This function may \b only be called from a profiler's init function or prior
 * to running managed code.
 */
mono_bool
mono_profiler_enable_coverage (void)
{
	if (mono_profiler_state.startup_done)
		return FALSE;

	mono_os_mutex_init (&mono_profiler_state.coverage_mutex);
	mono_profiler_state.coverage_hash = g_hash_table_new (NULL, NULL);

	if (!mono_debug_enabled ())
		mono_debug_init (MONO_DEBUG_FORMAT_MONO);

	return mono_profiler_state.code_coverage = TRUE;
}

/**
 * mono_profiler_set_coverage_filter_callback:
 *
 * Sets a code coverage filter function. The profiler API will invoke filter
 * functions from all installed profilers. If any of them return \c TRUE, then
 * the given method will be instrumented for coverage analysis. All filters are
 * guaranteed to be called at least once per method, even if an earlier filter
 * has already returned \c TRUE.
 *
 * Note that filter functions must be installed before a method is compiled in
 * order to have any effect, i.e. a filter should be registered in a profiler's
 * init function or prior to running managed code (if embedding).
 *
 * This function is async safe.
 */
void
mono_profiler_set_coverage_filter_callback (MonoProfilerHandle handle, MonoProfilerCoverageFilterCallback cb)
{
	mono_atomic_store_ptr (&handle->coverage_filter, (gpointer) cb);
}

static void
coverage_lock (void)
{
	mono_os_mutex_lock (&mono_profiler_state.coverage_mutex);
}

static void
coverage_unlock (void)
{
	mono_os_mutex_unlock (&mono_profiler_state.coverage_mutex);
}

/**
 * mono_profiler_get_coverage_data:
 *
 * Retrieves all coverage data for \p method and invokes \p cb for each entry.
 * Source location information will only be filled out if \p method has debug
 * info available. Returns \c TRUE if \p method was instrumented for code
 * coverage; otherwise, \c FALSE.
 *
 * Please note that the structure passed to \p cb is only valid for the
 * duration of the callback.
 *
 * This function is \b not async safe.
 */
mono_bool
mono_profiler_get_coverage_data (MonoProfilerHandle handle, MonoMethod *method, MonoProfilerCoverageCallback cb)
{
	if (!mono_profiler_state.code_coverage)
		return FALSE;

	if ((method->flags & METHOD_ATTRIBUTE_ABSTRACT) || (method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) || (method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) || (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL))
		return FALSE;

	coverage_lock ();

	MonoProfilerCoverageInfo *info = (MonoProfilerCoverageInfo*)g_hash_table_lookup (mono_profiler_state.coverage_hash, method);

	coverage_unlock ();

	MonoMethodHeaderSummary header;

	g_assert (mono_method_get_header_summary (method, &header));

	guint32 size = header.code_size;
	const unsigned char *start = header.code;
	const unsigned char *end = start + size;
	MonoDebugMethodInfo *minfo = mono_debug_lookup_method (method);

	if (!info) {
		int i, n_il_offsets;
		int *source_files;
		GPtrArray *source_file_list;
		MonoSymSeqPoint *sym_seq_points;

		if (!minfo)
			return TRUE;

		/* Return 0 counts for all locations */

		mono_debug_get_seq_points (minfo, NULL, &source_file_list, &source_files, &sym_seq_points, &n_il_offsets);
		for (i = 0; i < n_il_offsets; ++i) {
			MonoSymSeqPoint *sp = &sym_seq_points [i];
			const char *srcfile = "";

			if (source_files [i] != -1) {
				MonoDebugSourceInfo *sinfo = (MonoDebugSourceInfo *)g_ptr_array_index (source_file_list, source_files [i]);
				srcfile = sinfo->source_file;
			}

			MonoProfilerCoverageData data;
			memset (&data, 0, sizeof (data));
			data.method = method;
			data.il_offset = sp->il_offset;
			data.counter = 0;
			data.file_name = srcfile;
			data.line = sp->line;
			data.column = 0;

			cb (handle->prof, &data);
		}

		g_free (source_files);
		g_free (sym_seq_points);
		g_ptr_array_free (source_file_list, TRUE);

		return TRUE;
	}

	for (guint32 i = 0; i < info->entries; i++) {
		guchar *cil_code = info->data [i].cil_code;

		if (cil_code && cil_code >= start && cil_code < end) {
			guint32 offset = GPTRDIFF_TO_UINT32 (cil_code - start);

			MonoProfilerCoverageData data;
			memset (&data, 0, sizeof (data));
			data.method = method;
			data.il_offset = offset;
			data.counter = info->data [i].count;
			data.line = 1;
			data.column = 1;

			if (minfo) {
				MonoDebugSourceLocation *loc = mono_debug_method_lookup_location (minfo, offset);

				if (loc) {
					data.file_name = g_strdup (loc->source_file);
					data.line = loc->row;
					data.column = loc->column;

					mono_debug_free_source_location (loc);
				}
			}

			cb (handle->prof, &data);

			g_free ((char *) data.file_name);
		}
	}

	return TRUE;
}

gboolean
mono_profiler_coverage_instrumentation_enabled (MonoMethod *method)
{
	gboolean cover = FALSE;

	for (MonoProfilerHandle handle = mono_profiler_state.profilers; handle; handle = handle->next) {
		MonoProfilerCoverageFilterCallback cb = (MonoProfilerCoverageFilterCallback)handle->coverage_filter;

		if (cb)
			cover |= cb (handle->prof, method);
	}

	return cover;
}

MonoProfilerCoverageInfo *
mono_profiler_coverage_alloc (MonoMethod *method, guint32 entries)
{
	if (!mono_profiler_state.code_coverage)
		return NULL;

	if (!mono_profiler_coverage_instrumentation_enabled (method))
		return NULL;

	coverage_lock ();

	MonoProfilerCoverageInfo *info = g_malloc0 (sizeof (MonoProfilerCoverageInfo) + sizeof (MonoProfilerCoverageInfoEntry) * entries);

	info->entries = entries;

	g_hash_table_insert (mono_profiler_state.coverage_hash, method, info);

	coverage_unlock ();

	return info;
}

/**
 * mono_profiler_enable_sampling:
 *
 * Enables the sampling thread. Users must call this function if they intend
 * to use statistical sampling; \c mono_profiler_set_sample_mode will have no
 * effect if this function has not been called. The first profiler to call this
 * function will get ownership over sampling settings (mode and frequency) so
 * that no other profiler can change those settings. Returns \c TRUE if the
 * sampling thread was enabled, or \c FALSE if the function was called too late
 * for this to be possible.
 *
 * Note that \c mono_profiler_set_sample_mode must still be called with a mode
 * other than \c MONO_PROFILER_SAMPLE_MODE_NONE to actually start sampling.
 *
 * This function is \b not async safe.
 *
 * This function may \b only be called from a profiler's init function or prior
 * to running managed code.
 */
mono_bool
mono_profiler_enable_sampling (MonoProfilerHandle handle)
{
	if (mono_profiler_state.startup_done)
		return FALSE;

	if (mono_profiler_state.sampling_owner)
		return TRUE;

	mono_profiler_state.sampling_owner = handle;
	mono_profiler_state.sample_mode = MONO_PROFILER_SAMPLE_MODE_NONE;
	mono_profiler_state.sample_freq = 100;
	mono_os_sem_init (&mono_profiler_state.sampling_semaphore, 0);

	return TRUE;
}

/**
 * mono_profiler_set_sample_mode:
 *
 * Sets the sampling mode and frequency (in Hz). \p freq must be a positive
 * number. If the calling profiler has ownership over sampling settings, the
 * settings will be changed and this function will return \c TRUE; otherwise,
 * it returns \c FALSE without changing any settings.
 *
 * This function is async safe.
 */
mono_bool
mono_profiler_set_sample_mode (MonoProfilerHandle handle, MonoProfilerSampleMode mode, uint32_t freq)
{
	if (handle != mono_profiler_state.sampling_owner)
		return FALSE;

	mono_profiler_state.sample_mode = mode;
	mono_profiler_state.sample_freq = freq;

	mono_profiler_sampling_thread_post ();

	return TRUE;
}

/**
 * mono_profiler_get_sample_mode:
 *
 * Retrieves the current sampling mode and/or frequency (in Hz). Returns
 * \c TRUE if the calling profiler is allowed to change the sampling settings;
 * otherwise, \c FALSE.
 *
 * This function is async safe.
 */
mono_bool
mono_profiler_get_sample_mode (MonoProfilerHandle handle, MonoProfilerSampleMode *mode, uint32_t *freq)
{
	if (mode)
		*mode = mono_profiler_state.sample_mode;

	if (freq)
		*freq = mono_profiler_state.sample_freq;

	return handle == mono_profiler_state.sampling_owner;
}

gboolean
mono_profiler_sampling_enabled (void)
{
	return !!mono_profiler_state.sampling_owner;
}

void
mono_profiler_sampling_thread_post (void)
{
	mono_os_sem_post (&mono_profiler_state.sampling_semaphore);
}

void
mono_profiler_sampling_thread_wait (void)
{
	mono_os_sem_wait (&mono_profiler_state.sampling_semaphore, MONO_SEM_FLAGS_NONE);
}

/**
 * mono_profiler_enable_allocations:
 *
 * Enables instrumentation of GC allocations. This is necessary so that managed
 * allocators can be instrumented with a call into the profiler API.
 * Allocations will not be reported unless this function is called. Returns
 * \c TRUE if allocation instrumentation was enabled, or \c FALSE if the
 * function was called too late for this to be possible.
 *
 * This function is \b not async safe.
 *
 * This function may \b only be called from a profiler's init function or prior
 * to running managed code.
 */
mono_bool
mono_profiler_enable_allocations (void)
{
	if (mono_profiler_state.startup_done)
		return FALSE;

	return mono_profiler_state.allocations = TRUE;
}

/**
 * mono_profiler_enable_clauses:
 *
 * Enables instrumentation of exception clauses. This is necessary so that CIL
 * \c leave instructions can be instrumented with a call into the profiler API.
 * Exception clauses will not be reported unless this function is called.
 * Returns \c TRUE if exception clause instrumentation was enabled, or \c FALSE
 * if the function was called too late for this to be possible.
 *
 * This function is \b not async safe.
 *
 * This function may \b only be called from a profiler's init function or prior
 * to running managed code.
 */
mono_bool
mono_profiler_enable_clauses (void)
{
	if (mono_profiler_state.startup_done)
		return FALSE;

	return mono_profiler_state.clauses = TRUE;
}

gboolean
mono_component_profiler_clauses_enabled (void)
{
	return mono_profiler_clauses_enabled ();
}

/**
 * mono_profiler_set_call_instrumentation_filter_callback:
 *
 * Sets a call instrumentation filter function. The profiler API will invoke
 * filter functions from all installed profilers. If any of them return flags
 * other than \c MONO_PROFILER_CALL_INSTRUMENTATION_NONE, then the given method
 * will be instrumented as requested. All filters are guaranteed to be called
 * at least once per method, even if earlier filters have already specified all
 * flags.
 *
 * Note that filter functions must be installed before a method is compiled in
 * order to have any effect, i.e. a filter should be registered in a profiler's
 * init function or prior to running managed code (if embedding). Also, to
 * instrument a method that's going to be AOT-compiled, a filter must be
 * installed at AOT time. This can be done in exactly the same way as one would
 * normally, i.e. by passing the \c --profile option on the command line, by
 * calling \c mono_profiler_load, or simply by using the profiler API as an
 * embedder.
 *
 * Indiscriminate method instrumentation is extremely heavy and will slow down
 * most applications to a crawl. Users should consider sampling as a possible
 * alternative to such heavy-handed instrumentation.
 *
 * This function is async safe.
 */
void
mono_profiler_set_call_instrumentation_filter_callback (MonoProfilerHandle handle, MonoProfilerCallInstrumentationFilterCallback cb)
{
	mono_atomic_store_ptr (&handle->call_instrumentation_filter, (gpointer) cb);
}

/**
 * mono_profiler_enable_call_context_introspection:
 *
 * Enables support for retrieving stack frame data from a call context. At the
 * moment, this means enabling the debug info subsystem. If this function is not
 * called, it will not be possible to use the call context introspection
 * functions (they will simply return \c NULL). Returns \c TRUE if call context
 * introspection was enabled, or \c FALSE if the function was called too late for
 * this to be possible.
 *
 * This function is \b not async safe.
 *
 * This function may \b only be called from a profiler's init function or prior
 * to running managed code.
 */
mono_bool
mono_profiler_enable_call_context_introspection (void)
{
	if (mono_profiler_state.startup_done)
		return FALSE;

	mono_profiler_state.context_enable ();

	return mono_profiler_state.call_contexts = TRUE;
}

/**
 * mono_profiler_call_context_get_this:
 *
 * Given a valid call context from an enter/leave event, retrieves a pointer to
 * the \c this reference for the method. Returns \c NULL if none exists (i.e.
 * it's a static method) or if call context introspection was not enabled.
 *
 * The buffer returned by this function must be freed with
 * \c mono_profiler_call_context_free_buffer.
 *
 * Please note that a call context is only valid for the duration of the
 * enter/leave callback it was passed to.
 *
 * This function is \b not async safe.
 */
void *
mono_profiler_call_context_get_this (MonoProfilerCallContext *context)
{
	if (!mono_profiler_state.call_contexts)
		return NULL;

	return mono_profiler_state.context_get_this (context);
}

/**
 * mono_profiler_call_context_get_argument:
 *
 * Given a valid call context from an enter/leave event, retrieves a pointer to
 * the method argument at the given position. Returns \c NULL if \p position is
 * out of bounds or if call context introspection was not enabled.
 *
 * The buffer returned by this function must be freed with
 * \c mono_profiler_call_context_free_buffer.
 *
 * Please note that a call context is only valid for the duration of the
 * enter/leave callback it was passed to.
 *
 * This function is \b not async safe.
 */
void *
mono_profiler_call_context_get_argument (MonoProfilerCallContext *context, uint32_t position)
{
	if (!mono_profiler_state.call_contexts)
		return NULL;

	return mono_profiler_state.context_get_argument (context, position);
}

/**
 * mono_profiler_call_context_get_local:
 *
 * Given a valid call context from an enter/leave event, retrieves a pointer to
 * the local variable at the given position. Returns \c NULL if \p position is
 * out of bounds or if call context introspection was not enabled.
 *
 * The buffer returned by this function must be freed with
 * \c mono_profiler_call_context_free_buffer.
 *
 * Please note that a call context is only valid for the duration of the
 * enter/leave callback it was passed to.
 *
 * This function is \b not async safe.
 */
void *
mono_profiler_call_context_get_local (MonoProfilerCallContext *context, uint32_t position)
{
	if (!mono_profiler_state.call_contexts)
		return NULL;

	return mono_profiler_state.context_get_local (context, position);
}

/**
 * mono_profiler_call_context_get_result:
 *
 * Given a valid call context from an enter/leave event, retrieves a pointer to
 * return value of a method. Returns \c NULL if the method has no return value
 * (i.e. it returns \c void), if the leave event was the result of a tail call,
 * if the function is called on a context from an enter event, or if call
 * context introspection was not enabled.
 *
 * The buffer returned by this function must be freed with
 * \c mono_profiler_call_context_free_buffer.
 *
 * Please note that a call context is only valid for the duration of the
 * enter/leave callback it was passed to.
 *
 * This function is \b not async safe.
 */
void *
mono_profiler_call_context_get_result (MonoProfilerCallContext *context)
{
	if (!mono_profiler_state.call_contexts)
		return NULL;

	return mono_profiler_state.context_get_result (context);
}

/**
 * mono_profiler_call_context_free_buffer:
 *
 * Frees a buffer returned by one of the call context introspection functions.
 * Passing a \c NULL value for \p buffer is allowed, which makes this function
 * a no-op.
 *
 * This function is \b not async safe.
 */
void
mono_profiler_call_context_free_buffer (void *buffer)
{
	mono_profiler_state.context_free_buffer (buffer);
}

G_ENUM_FUNCTIONS (MonoProfilerCallInstrumentationFlags)

MonoProfilerCallInstrumentationFlags
mono_profiler_get_call_instrumentation_flags (MonoMethod *method)
{
	MonoProfilerCallInstrumentationFlags flags = MONO_PROFILER_CALL_INSTRUMENTATION_NONE;

	for (MonoProfilerHandle handle = mono_profiler_state.profilers; handle; handle = handle->next) {
		MonoProfilerCallInstrumentationFilterCallback cb = (MonoProfilerCallInstrumentationFilterCallback)handle->call_instrumentation_filter;

		if (cb)
			flags |= cb (handle->prof, method);
	}

	return flags;
}

void
mono_profiler_started (void)
{
	mono_profiler_state.startup_done = TRUE;
}

static void
update_callback (volatile gpointer *location, gpointer new_, volatile gint32 *counter)
{
	gpointer old;

	do {
		old = mono_atomic_load_ptr (location);
	} while (mono_atomic_cas_ptr (location, new_, old) != old);

	/*
	 * At this point, we could have installed a NULL callback while the counter
	 * is still non-zero, i.e. setting the callback and modifying the counter
	 * is not a single atomic operation. This is fine as we make sure callbacks
	 * are non-NULL before invoking them (see the code below that generates the
	 * raise functions), and besides, updating callbacks at runtime is an
	 * inherently racy operation.
	 */

	if (old)
		mono_atomic_dec_i32 (counter);

	if (new_)
		mono_atomic_inc_i32 (counter);
}

#define _MONO_PROFILER_EVENT(name, type) \
	void \
	mono_profiler_set_ ## name ## _callback (MonoProfilerHandle handle, MonoProfiler ## type ## Callback cb) \
	{ \
		update_callback (&handle->name ## _cb, (gpointer) cb, &mono_profiler_state.name ## _count); \
	}
#define MONO_PROFILER_EVENT_0(name, type) \
	_MONO_PROFILER_EVENT(name, type)
#define MONO_PROFILER_EVENT_1(name, type, arg1_type, arg1_name) \
	_MONO_PROFILER_EVENT(name, type)
#define MONO_PROFILER_EVENT_2(name, type, arg1_type, arg1_name, arg2_type, arg2_name) \
	_MONO_PROFILER_EVENT(name, type)
#define MONO_PROFILER_EVENT_3(name, type, arg1_type, arg1_name, arg2_type, arg2_name, arg3_type, arg3_name) \
	_MONO_PROFILER_EVENT(name, type)
#define MONO_PROFILER_EVENT_4(name, type, arg1_type, arg1_name, arg2_type, arg2_name, arg3_type, arg3_name, arg4_type, arg4_name) \
	_MONO_PROFILER_EVENT(name, type)
#define MONO_PROFILER_EVENT_5(name, type, arg1_type, arg1_name, arg2_type, arg2_name, arg3_type, arg3_name, arg4_type, arg4_name, arg5_type, arg5_name)	\
	_MONO_PROFILER_EVENT(name, type)
#include <mono/metadata/profiler-events.h>
#undef MONO_PROFILER_EVENT_0
#undef MONO_PROFILER_EVENT_1
#undef MONO_PROFILER_EVENT_2
#undef MONO_PROFILER_EVENT_3
#undef MONO_PROFILER_EVENT_4
#undef MONO_PROFILER_EVENT_5
#undef _MONO_PROFILER_EVENT

#define _MONO_PROFILER_EVENT(name, type, params, args) \
	void \
	mono_profiler_raise_ ## name params \
	{ \
		if (!mono_profiler_state.startup_done) return;	\
		for (MonoProfilerHandle h = mono_profiler_state.profilers; h; h = h->next) { \
			MonoProfiler ## type ## Callback cb = (MonoProfiler ## type ## Callback)h->name ## _cb; \
			if (cb) \
				cb args; \
		} \
	}
#define MONO_PROFILER_EVENT_0(name, type) \
	_MONO_PROFILER_EVENT(name, type, (void), (h->prof))
#define MONO_PROFILER_EVENT_1(name, type, arg1_type, arg1_name) \
	_MONO_PROFILER_EVENT(name, type, (arg1_type arg1_name), (h->prof, arg1_name))
#define MONO_PROFILER_EVENT_2(name, type, arg1_type, arg1_name, arg2_type, arg2_name) \
	_MONO_PROFILER_EVENT(name, type, (arg1_type arg1_name, arg2_type arg2_name), (h->prof, arg1_name, arg2_name))
#define MONO_PROFILER_EVENT_3(name, type, arg1_type, arg1_name, arg2_type, arg2_name, arg3_type, arg3_name) \
	_MONO_PROFILER_EVENT(name, type, (arg1_type arg1_name, arg2_type arg2_name, arg3_type arg3_name), (h->prof, arg1_name, arg2_name, arg3_name))
#define MONO_PROFILER_EVENT_4(name, type, arg1_type, arg1_name, arg2_type, arg2_name, arg3_type, arg3_name, arg4_type, arg4_name) \
	_MONO_PROFILER_EVENT(name, type, (arg1_type arg1_name, arg2_type arg2_name, arg3_type arg3_name, arg4_type arg4_name), (h->prof, arg1_name, arg2_name, arg3_name, arg4_name))
#define MONO_PROFILER_EVENT_5(name, type, arg1_type, arg1_name, arg2_type, arg2_name, arg3_type, arg3_name, arg4_type, arg4_name, arg5_type, arg5_name)	\
	_MONO_PROFILER_EVENT(name, type, (arg1_type arg1_name, arg2_type arg2_name, arg3_type arg3_name, arg4_type arg4_name, arg5_type arg5_name), (h->prof, arg1_name, arg2_name, arg3_name, arg4_name, arg5_name))
#include <mono/metadata/profiler-events.h>
#undef MONO_PROFILER_EVENT_0
#undef MONO_PROFILER_EVENT_1
#undef MONO_PROFILER_EVENT_2
#undef MONO_PROFILER_EVENT_3
#undef MONO_PROFILER_EVENT_4
#undef MONO_PROFILER_EVENT_5
#undef _MONO_PROFILER_EVENT


struct _MonoProfiler {
	MonoProfilerHandle handle;
	MonoLegacyProfiler *profiler;
	MonoLegacyProfileFunc shutdown_callback;
	MonoLegacyProfileThreadFunc thread_start, thread_end;
	MonoLegacyProfileGCFunc gc_event;
	MonoLegacyProfileGCResizeFunc gc_heap_resize;
	MonoLegacyProfileJitResult jit_end2;
	MonoLegacyProfileAllocFunc allocation;
	MonoLegacyProfileMethodFunc enter;
	MonoLegacyProfileMethodFunc leave;
	MonoLegacyProfileExceptionFunc throw_callback;
	MonoLegacyProfileMethodFunc exc_method_leave;
	MonoLegacyProfileExceptionClauseFunc clause_callback;
};

static MonoProfiler *current;

void
mono_profiler_install (MonoLegacyProfiler *prof, MonoLegacyProfileFunc callback)
{
	current = g_new0 (MonoProfiler, 1);
	current->handle = mono_profiler_create (current);
	current->profiler = prof;
	current->shutdown_callback = callback;
}

static void
thread_start_cb (MonoProfiler *prof, uintptr_t tid)
{
	prof->thread_start (prof->profiler, tid);
}

static void
thread_stop_cb (MonoProfiler *prof, uintptr_t tid)
{
	prof->thread_end (prof->profiler, tid);
}

void
mono_profiler_install_thread (MonoLegacyProfileThreadFunc start, MonoLegacyProfileThreadFunc end)
{
	current->thread_start = start;
	current->thread_end = end;

	if (start)
		mono_profiler_set_thread_started_callback (current->handle, thread_start_cb);

	if (end)
		mono_profiler_set_thread_stopped_callback (current->handle, thread_stop_cb);
}

static void
gc_event_cb (MonoProfiler *prof, MonoProfilerGCEvent event, uint32_t generation, gboolean is_serial)
{
	prof->gc_event (prof->profiler, event, generation);
}

static void
gc_resize_cb (MonoProfiler *prof, uintptr_t size)
{
	prof->gc_heap_resize (prof->profiler, size);
}

void
mono_profiler_install_gc (MonoLegacyProfileGCFunc callback, MonoLegacyProfileGCResizeFunc heap_resize_callback)
{
	current->gc_event = callback;
	current->gc_heap_resize = heap_resize_callback;

	if (callback)
		mono_profiler_set_gc_event_callback (current->handle, gc_event_cb);

	if (heap_resize_callback)
		mono_profiler_set_gc_resize_callback (current->handle, gc_resize_cb);
}

static void
jit_done_cb (MonoProfiler *prof, MonoMethod *method, MonoJitInfo *jinfo)
{
	prof->jit_end2 (prof->profiler, method, jinfo, 0);
}

static void
jit_failed_cb (MonoProfiler *prof, MonoMethod *method)
{
	prof->jit_end2 (prof->profiler, method, NULL, 1);
}

void
mono_profiler_install_jit_end (MonoLegacyProfileJitResult end)
{
	current->jit_end2 = end;

	if (end) {
		mono_profiler_set_jit_done_callback (current->handle, jit_done_cb);
		mono_profiler_set_jit_failed_callback (current->handle, jit_failed_cb);
	}
}

void
mono_profiler_set_events (int flags)
{
	/* Do nothing. */
}

static void
allocation_cb (MonoProfiler *prof, MonoObject* object)
{
	prof->allocation (prof->profiler, object, object->vtable->klass);
}

void
mono_profiler_install_allocation (MonoLegacyProfileAllocFunc callback)
{
	current->allocation = callback;

	if (callback)
		mono_profiler_set_gc_allocation_callback (current->handle, allocation_cb);
}

static void
enter_cb (MonoProfiler *prof, MonoMethod *method, MonoProfilerCallContext *context)
{
	prof->enter (prof->profiler, method);
}

static void
leave_cb (MonoProfiler *prof, MonoMethod *method, MonoProfilerCallContext *context)
{
	prof->leave (prof->profiler, method);
}

static void
tail_call_cb (MonoProfiler *prof, MonoMethod *method, MonoMethod *target)
{
	prof->leave (prof->profiler, method);
}

void
mono_profiler_install_enter_leave (MonoLegacyProfileMethodFunc enter, MonoLegacyProfileMethodFunc fleave)
{
	current->enter = enter;
	current->leave = fleave;

	if (enter)
		mono_profiler_set_method_enter_callback (current->handle, enter_cb);

	if (fleave) {
		mono_profiler_set_method_leave_callback (current->handle, leave_cb);
		mono_profiler_set_method_tail_call_callback (current->handle, tail_call_cb);
	}
}

static void
throw_callback_cb (MonoProfiler *prof, MonoObject *exception)
{
	prof->throw_callback (prof->profiler, exception);
}

static void
exc_method_leave_cb (MonoProfiler *prof, MonoMethod *method, MonoObject *exception)
{
	prof->exc_method_leave (prof->profiler, method);
}

static void
clause_callback_cb (MonoProfiler *prof, MonoMethod *method, uint32_t index, MonoExceptionEnum type, MonoObject *exception)
{
	prof->clause_callback (prof->profiler, method, type, index);
}

void
mono_profiler_install_exception (MonoLegacyProfileExceptionFunc throw_callback, MonoLegacyProfileMethodFunc exc_method_leave, MonoLegacyProfileExceptionClauseFunc clause_callback)
{
	current->throw_callback = throw_callback;
	current->exc_method_leave = exc_method_leave;
	current->clause_callback = clause_callback;

	if (throw_callback)
		mono_profiler_set_exception_throw_callback (current->handle, throw_callback_cb);

	if (exc_method_leave)
		mono_profiler_set_method_exception_leave_callback (current->handle, exc_method_leave_cb);

	if (clause_callback)
		mono_profiler_set_exception_clause_callback (current->handle, clause_callback_cb);
}
