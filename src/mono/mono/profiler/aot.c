/*
 * aot.c: Ahead of Time Compiler Profiler for Mono.
 *
 *
 * Copyright 2008-2009 Novell, Inc (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>

#include "aot.h"
#include "helper.h"

#include <mono/metadata/object-internals.h>
#include <mono/metadata/profiler.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/utils/mono-publib.h>
#include <mono/jit/jit.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-os-mutex.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-proclib.h>
#include <string.h>
#include <errno.h>
#include <stdlib.h>
#ifndef HOST_WIN32
#include <sys/socket.h>
#else
#define sleep(t)                 Sleep((t) * 1000)
#endif
#include <glib.h>

struct _MonoProfiler {
	GHashTable *classes;
	GHashTable *images;
	GPtrArray *methods;
	FILE *outfile;
	int id;
	char *outfile_name;
	mono_mutex_t mutex;
	gboolean verbose;
	int duration;
	MonoMethodDesc *write_at;
	MonoMethodDesc *send_to;
	char *send_to_arg;
	char *send_to_str;
	guint8 *buf;
	gboolean disable;
	int buf_pos, buf_len;
	int command_port;
	SOCKET server_socket;
};

static MonoProfiler aot_profiler;

static void
prof_shutdown (MonoProfiler *prof);

static void
prof_jit_done (MonoProfiler *prof, MonoMethod *method, MonoJitInfo *jinfo)
{
	MonoImage *image = mono_class_get_image (mono_method_get_class (method));

	if (!image->assembly || method->wrapper_type || !prof->methods || prof->disable)
		return;

	if (prof->write_at && mono_method_desc_match (prof->write_at, method)) {
		printf ("aot-profiler | Writing data at: '%s'.\n", mono_method_full_name (method, 1));
		prof_shutdown (prof);
		return;
	}

	mono_os_mutex_lock (&prof->mutex);
	if (prof->methods)
		g_ptr_array_add (prof->methods, method);
	mono_os_mutex_unlock (&prof->mutex);
}

static void
prof_inline_method (MonoProfiler *prof, MonoMethod *method, MonoMethod *inlined_method)
{
	prof_jit_done (prof, inlined_method, NULL);
}

static void
usage (void)
{
	mono_profiler_printf ("AOT profiler.");
	mono_profiler_printf ("Usage: mono --profile=aot[:OPTION1[,OPTION2...]] program.exe\n");
	mono_profiler_printf ("Options:");
	mono_profiler_printf ("\tduration=NUM         profile only NUM seconds of runtime and write the data");
	mono_profiler_printf ("\thelp                 show this usage info");
	mono_profiler_printf ("\toutput=FILENAME      write the data to file FILENAME");
	mono_profiler_printf ("\tport=PORT            use PORT to listen for command server connections");
	mono_profiler_printf ("\twrite-at-method=METHOD       write the data when METHOD is compiled.");
	mono_profiler_printf ("\tsend-to-method=METHOD       call METHOD with the collected data.");
	mono_profiler_printf ("\tsend-to-arg=STR      extra argument to pass to METHOD.");
	mono_profiler_printf ("\tverbose              print diagnostic info");

	exit (0);
}

static gboolean
match_option (const char *arg, const char *opt_name, const char **rval)
{
	if (rval) {
		const char *end = strchr (arg, '=');

		*rval = NULL;
		if (!end)
			return !strcmp (arg, opt_name);

		if (strncmp (arg, opt_name, strlen (opt_name)) || (end - arg) > strlen (opt_name) + 1)
			return FALSE;
		*rval = end + 1;
		return TRUE;
	} else {
		//FIXME how should we handle passing a value to an arg that doesn't expect it?
		return !strcmp (arg, opt_name);
	}
}

static void
parse_arg (const char *arg)
{
	const char *val;

	if (match_option (arg, "help", NULL)) {
		usage ();
	} else if (match_option (arg, "duration", &val)) {
		char *end;
		aot_profiler.duration = strtoul (val, &end, 10);
	} else if (match_option (arg, "write-at-method", &val)) {
		aot_profiler.write_at = mono_method_desc_new (val, TRUE);
		if (!aot_profiler.write_at) {
			mono_profiler_printf_err ("Could not parse method description: %s", val);
			exit (1);
		}
	} else if (match_option (arg, "send-to-method", &val)) {
		aot_profiler.send_to = mono_method_desc_new (val, TRUE);
		if (!aot_profiler.send_to) {
			mono_profiler_printf_err ("Could not parse method description: %s", val);
			exit (1);
		}
		aot_profiler.send_to_str = strdup (val);
	} else if (match_option (arg, "send-to-arg", &val)) {
		aot_profiler.send_to_arg = strdup (val);
	} else if (match_option (arg, "output", &val)) {
		aot_profiler.outfile_name = g_strdup (val);
	} else if (match_option (arg, "port", &val)) {
		char *end;
		aot_profiler.command_port = strtoul (val, &end, 10);
	} else if (match_option (arg, "verbose", NULL)) {
		aot_profiler.verbose = TRUE;
	} else {
		mono_profiler_printf_err ("Could not parse argument: %s", arg);
	}
}

static void
parse_args (const char *desc)
{
	const char *p;
	gboolean in_quotes = FALSE;
	char quote_char = '\0';
	char *buffer = g_malloc (strlen (desc) + 1);
	int buffer_pos = 0;

	for (p = desc; *p; p++){
		switch (*p){
		case ',':
			if (!in_quotes) {
				if (buffer_pos != 0){
					buffer [buffer_pos] = 0;
					parse_arg (buffer);
					buffer_pos = 0;
				}
			} else {
				buffer [buffer_pos++] = *p;
			}
			break;

		case '\\':
			if (p [1]) {
				buffer [buffer_pos++] = p[1];
				p++;
			}
			break;
		case '\'':
		case '"':
			if (in_quotes) {
				if (quote_char == *p)
					in_quotes = FALSE;
				else
					buffer [buffer_pos++] = *p;
			} else {
				in_quotes = TRUE;
				quote_char = *p;
			}
			break;
		default:
			buffer [buffer_pos++] = *p;
			break;
		}
	}

	if (buffer_pos != 0) {
		buffer [buffer_pos] = 0;
		parse_arg (buffer);
	}

	g_free (buffer);
}

static void prof_save (MonoProfiler *prof, FILE* file);

static void *
helper_thread (void *arg)
{
	mono_thread_internal_attach (mono_get_root_domain ());

	mono_thread_set_name_constant_ignore_error (mono_thread_internal_current (), "AOT Profiler Helper", MonoSetThreadNameFlag_None);

	mono_thread_info_set_flags (MONO_THREAD_INFO_FLAGS_NO_GC | MONO_THREAD_INFO_FLAGS_NO_SAMPLE);

	if (aot_profiler.duration >= 0) {
		sleep (aot_profiler.duration);
	} else if (aot_profiler.command_port >= 0) {
		GArray *command_sockets = g_array_new (FALSE, FALSE, sizeof (int));

		while (1) {
			fd_set rfds;
			int max_fd = -1;
			int quit_command_received = 0;

			FD_ZERO (&rfds);

			mono_profhelper_add_to_fd_set (&rfds, aot_profiler.server_socket, &max_fd);

			for (gint i = 0; i < command_sockets->len; i++)
				mono_profhelper_add_to_fd_set (&rfds, g_array_index (command_sockets, int, i), &max_fd);

			struct timeval tv = { .tv_sec = 1, .tv_usec = 0 };

			// Sleep for 1sec or until a file descriptor has data.
			if (select (max_fd + 1, &rfds, NULL, NULL, &tv) == SOCKET_ERROR) {
				if (errno == EINTR)
					continue;

				mono_profiler_printf_err ("Could not poll in aot profiler helper thread: %s", g_strerror (errno));
				exit (1);
			}

			for (gint i = 0; i < command_sockets->len; i++) {
				int fd = g_array_index (command_sockets, int, i);

				if (!FD_ISSET (fd, &rfds))
					continue;

				char buf [64];
				int len = read (fd, buf, sizeof (buf) - 1);

				if (len == SOCKET_ERROR)
					continue;

				if (!len) {
					// The other end disconnected.
					g_array_remove_index (command_sockets, i);
					i--;
					mono_profhelper_close_socket_fd (fd);

					continue;
				}

				buf [len] = 0;

				if (!strcmp (buf, "save\n")) {
					FILE* file = fdopen (fd, "w");

					prof_save (&aot_profiler, file);

					fclose (file);

					mono_profiler_printf_err ("aot profiler data saved to the socket");

					g_array_remove_index (command_sockets, i);
					i--;

					continue;
				} else if (!strcmp (buf, "quit\n")) {
					quit_command_received = 1;
				}
			}

			if (quit_command_received)
				break;

			if (FD_ISSET (aot_profiler.server_socket, &rfds)) {
				SOCKET fd = accept (aot_profiler.server_socket, NULL, NULL);

				if (fd != INVALID_SOCKET) {
					if (fd >= FD_SETSIZE)
						mono_profhelper_close_socket_fd (fd);
					else
						g_array_append_val (command_sockets, fd);
				}
			}
		}

		for (gint i = 0; i < command_sockets->len; i++)
			mono_profhelper_close_socket_fd (g_array_index (command_sockets, int, i));

		g_array_free (command_sockets, TRUE);
	}

	prof_shutdown (&aot_profiler);

	mono_thread_info_set_flags (MONO_THREAD_INFO_FLAGS_NONE);
	mono_thread_internal_detach (mono_thread_current ());

	return NULL;
}

static void
start_helper_thread (void)
{
	if (aot_profiler.command_port >= 0)
		mono_profhelper_setup_command_server (&aot_profiler.server_socket, &aot_profiler.command_port, "aot");

	MonoNativeThreadId thread_id;

	if (!mono_native_thread_create (&thread_id, helper_thread, NULL)) {
		mono_profiler_printf_err ("Could not start aot profiler helper thread");
		exit (1);
	}
}

static void
runtime_initialized (MonoProfiler *profiler)
{
	if (profiler->duration >= 0 || aot_profiler.command_port >= 0)
		start_helper_thread ();
}

MONO_API void
mono_profiler_init_aot (const char *desc);

/**
 * mono_profiler_init_aot:
 * the entry point
 */
void
mono_profiler_init_aot (const char *desc)
{
	if (mono_jit_aot_compiling ()) {
		mono_profiler_printf_err ("The AOT profiler is not meant to be run during AOT compilation.");
		exit (1);
	}

	aot_profiler.duration = -1;
	aot_profiler.command_port = -1;
	aot_profiler.outfile_name = NULL;
	aot_profiler.outfile = NULL;

	parse_args (desc [strlen ("aot")] == ':' ? desc + strlen ("aot") + 1 : "");

	if (!aot_profiler.send_to) {
		if (!aot_profiler.outfile_name)
			aot_profiler.outfile_name = g_strdup ("output.aotprofile");
		else if (*aot_profiler.outfile_name == '+')
			aot_profiler.outfile_name = g_strdup_printf ("%s.%d", aot_profiler.outfile_name + 1, mono_process_current_pid ());

		if (*aot_profiler.outfile_name == '|') {
#ifdef HAVE_POPEN
			aot_profiler.outfile = popen (aot_profiler.outfile_name + 1, "w");
#else
			g_assert_not_reached ();
#endif
		}  else if (*aot_profiler.outfile_name == '#') {
			aot_profiler.outfile = fdopen (strtol (aot_profiler.outfile_name + 1, NULL, 10), "a");
		} else {
			aot_profiler.outfile = fopen (aot_profiler.outfile_name, "w");
		}

		if (!aot_profiler.outfile && aot_profiler.outfile_name) {
			mono_profiler_printf_err ("Could not create AOT profiler output file '%s': %s", aot_profiler.outfile_name, g_strerror (errno));
			exit (1);
		}
	}

	aot_profiler.images = g_hash_table_new (NULL, NULL);
	aot_profiler.classes = g_hash_table_new (NULL, NULL);
	aot_profiler.methods = g_ptr_array_new ();

	mono_os_mutex_init (&aot_profiler.mutex);

	MonoProfilerHandle handle = mono_profiler_create (&aot_profiler);
	mono_profiler_set_runtime_initialized_callback (handle, runtime_initialized);
	mono_profiler_set_jit_done_callback (handle, prof_jit_done);
	mono_profiler_set_inline_method_callback (handle, prof_inline_method);
}

static void
make_room (MonoProfiler *prof, int n)
{
	if (prof->buf_pos + n >= prof->buf_len) {
		int new_len = prof->buf_len * 2;
		guint8 *new_buf = g_malloc0 (new_len);
		memcpy (new_buf, prof->buf, prof->buf_pos);
		g_free (prof->buf);
		prof->buf = new_buf;
		prof->buf_len = new_len;
	}
}

static void
emit_bytes (MonoProfiler *prof, guint8 *bytes, int len)
{
	make_room (prof, len);
	memcpy (prof->buf + prof->buf_pos, bytes, len);
	prof->buf_pos += len;
}

static void
emit_byte (MonoProfiler *prof, guint8 value)
{
	emit_bytes (prof, &value, 1);
}

static void
emit_int32 (MonoProfiler *prof, gint32 value)
{
	for (int i = 0; i < sizeof (gint32); ++i) {
		guint8 b = GINT32_TO_UINT8 (value);
		emit_bytes (prof, &b, 1);
		value >>= 8;
	}
}

static void
emit_string (MonoProfiler *prof, const char *str)
{
	size_t len = strlen (str);

	emit_int32 (prof, (gint32)len);
	emit_bytes (prof, (guint8*)str, (int)len);
}

static void
emit_record (MonoProfiler *prof, AotProfRecordType type, int id)
{
	emit_byte (prof, (guint8)type);
	emit_int32 (prof, id);
}

static int
add_image (MonoProfiler *prof, MonoImage *image)
{
	int id = GPOINTER_TO_INT (g_hash_table_lookup (prof->images, image));
	if (id)
		return id - 1;

	// Dynamic images don't have a GUID set.  Moreover, we won't
	// have a chance to AOT them.  (But perhaps they should be
	// included in the profile, or logged, for diagnostic purposes?)
	if (!image->guid)
		return -1;

	id = prof->id ++;
	emit_record (prof, AOTPROF_RECORD_IMAGE, id);
	emit_string (prof, image->assembly->aname.name);
	emit_string (prof, image->guid);
	g_hash_table_insert (prof->images, image, GINT_TO_POINTER (id + 1));
	return id;
}

static int
add_class (MonoProfiler *prof, MonoClass *klass);

static int
add_type (MonoProfiler *prof, MonoType *type)
{
	switch (type->type) {
#if 0
	case MONO_TYPE_SZARRAY: {
		int eid = add_type (prof, m_class_get_byval_arg (type->data.klass));
		if (eid == -1)
			return -1;
		int id = prof->id ++;
		emit_record (prof, AOTPROF_RECORD_TYPE, id);
		emit_byte (prof, MONO_TYPE_SZARRAY);
		emit_int32 (prof, id);
		return id;
	}
#endif
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_STRING:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_GENERICINST:
		return add_class (prof, mono_class_from_mono_type_internal (type));
	default:
		return -1;
	}
}

static int
add_ginst (MonoProfiler *prof, MonoGenericInst *inst)
{
	int i, id;
	int *ids;

	// FIXME: Cache
	ids = g_malloc0 (inst->type_argc * sizeof (int));
	for (i = 0; i < inst->type_argc; ++i) {
		MonoType *t = inst->type_argv [i];
		ids [i] = add_type (prof, t);
		if (ids [i] == -1) {
			g_free (ids);
			return -1;
		}
	}
	id = prof->id ++;
	emit_record (prof, AOTPROF_RECORD_GINST, id);
	emit_int32 (prof, inst->type_argc);
	for (i = 0; i < inst->type_argc; ++i)
		emit_int32 (prof, ids [i]);
	g_free (ids);

	return id;
}

static int
add_class (MonoProfiler *prof, MonoClass *klass)
{
	int id, inst_id = -1, image_id;
	char *name;

	id = GPOINTER_TO_INT (g_hash_table_lookup (prof->classes, klass));
	if (id)
		return id - 1;

	image_id = add_image (prof, mono_class_get_image (klass));
	if (image_id == -1)
		return -1;

	if (mono_class_is_ginst (klass)) {
		MonoGenericContext *ctx = mono_class_get_context (klass);
		inst_id = add_ginst (prof, ctx->class_inst);
		if (inst_id == -1)
			return -1;
	}

	MonoClass *klass_nested_in = mono_class_get_nesting_type (klass);
	if (klass_nested_in)
		name = g_strdup_printf ("%s.%s/%s", m_class_get_name_space (klass_nested_in), m_class_get_name (klass_nested_in), m_class_get_name (klass));
	else
		name = g_strdup_printf ("%s.%s", m_class_get_name_space (klass), m_class_get_name (klass));

	id = prof->id ++;
	emit_record (prof, AOTPROF_RECORD_TYPE, id);
	emit_byte (prof, MONO_TYPE_CLASS);
	emit_int32 (prof, image_id);
	emit_int32 (prof, inst_id);
	emit_string (prof, name);
	g_free (name);
	g_hash_table_insert (prof->classes, klass, GINT_TO_POINTER (id + 1));
	return id;
}

static void
add_method (MonoProfiler *prof, MonoMethod *m)
{
	ERROR_DECL (error);
	MonoMethodSignature *sig;
	char *s;

	sig = mono_method_signature_checked (m, error);
	g_assert (is_ok (error));

	int class_id = add_class (prof, m->klass);
	if (class_id == -1)
		return;
	int inst_id = -1;

	if (m->is_inflated) {
		MonoGenericContext *ctx = mono_method_get_context (m);
		if (ctx->method_inst)
			inst_id = add_ginst (prof, ctx->method_inst);
	}
	int id = prof->id ++;
	emit_record (prof, AOTPROF_RECORD_METHOD, id);
	emit_int32 (prof, class_id);
	emit_int32 (prof, inst_id);
	emit_int32 (prof, sig->param_count);
	emit_string (prof, m->name);
	s = mono_signature_full_name (sig);
	emit_string (prof, s);
	g_free (s);

	if (prof->verbose)
		mono_profiler_printf ("%s %d", mono_method_full_name (m, 1), id);
}

static void
prof_save (MonoProfiler *prof, FILE* file)
{
	mono_os_mutex_lock (&prof->mutex);
	int already_shutdown = prof->methods == NULL;
	mono_os_mutex_unlock (&prof->mutex);

	if (already_shutdown)
		return;

	int mindex;
	char magic [32];

	prof->buf_len = 4096;
	prof->buf = g_malloc0 (prof->buf_len);
	prof->buf_pos = 0;

	gint32 version = (AOT_PROFILER_MAJOR_VERSION << 16) | AOT_PROFILER_MINOR_VERSION;
	sprintf (magic, AOT_PROFILER_MAGIC);
	emit_bytes (prof, (guint8*)magic, (int)strlen (magic));
	emit_int32 (prof, version);

	GHashTable *all_methods = g_hash_table_new (NULL, NULL);
	mono_os_mutex_lock (&prof->mutex);
	for (mindex = 0; mindex < prof->methods->len; ++mindex) {
	    MonoMethod *m = (MonoMethod*)g_ptr_array_index (prof->methods, mindex);

		if (!mono_method_get_token (m))
			continue;

		if (g_hash_table_lookup (all_methods, m))
			continue;
		g_hash_table_insert (all_methods, m, m);

		add_method (prof, m);
	}
	emit_record (prof, AOTPROF_RECORD_NONE, 0);

	if (prof->send_to) {
		GHashTableIter iter;
		gpointer id;
		MonoImage *image;
		MonoMethod *send_method = NULL;
		MonoMethodSignature *sig;
		ERROR_DECL (error);

		g_hash_table_iter_init (&iter, prof->images);
		while (g_hash_table_iter_next (&iter, (void**)&image, (void**)&id)) {
			send_method = mono_method_desc_search_in_image (prof->send_to, image);
			if (send_method)
				break;
		}
		if (!send_method) {
			mono_profiler_printf_err ("Cannot find method in loaded assemblies: '%s'.", prof->send_to_str);
			exit (1);
		}

		sig = mono_method_signature_checked (send_method, error);
		mono_error_assert_ok (error);
		if (sig->param_count != 3 || !m_type_is_byref (sig->params [0]) || sig->params [0]->type != MONO_TYPE_U1 || sig->params [1]->type != MONO_TYPE_I4 || sig->params [2]->type != MONO_TYPE_STRING) {
			mono_profiler_printf_err ("Method '%s' should have signature void (byte&,int,string).", prof->send_to_str);
			exit (1);
		}

		// Don't collect data from the call
		prof->disable = TRUE;

		MonoString *extra_arg = NULL;
		if (prof->send_to_arg) {
			extra_arg = mono_string_new_checked (prof->send_to_arg, error);
			mono_error_assert_ok (error);
		}

		MonoObject *exc;
		gpointer args [3];
		int len = prof->buf_pos;
		void *ptr = prof->buf;
		args [0] = ptr;
		args [1] = &len;
		args [2] = extra_arg;

		printf ("aot-profiler | Passing data to '%s': %p %d %s\n", mono_method_full_name (send_method, 1), args [0], len, prof->send_to_arg ? prof->send_to_arg : "(null)");
		mono_runtime_try_invoke (send_method, NULL, args, &exc, error);
		mono_error_assert_ok (error);
		g_assert (exc == NULL);
	} else {
		g_assert (file);
		fwrite (prof->buf, 1, prof->buf_pos, file);
		fclose (file);

		mono_profiler_printf ("AOT profiler data written to '%s'", prof->command_port >= 0 ? "socket" : prof->outfile_name);
	}

	g_hash_table_destroy (all_methods);

	g_hash_table_remove_all (prof->classes);
	g_hash_table_remove_all (prof->images);

	mono_os_mutex_unlock (&prof->mutex);
}

/* called at the end of the program */
static void
prof_shutdown (MonoProfiler *prof)
{
	if (prof->outfile || prof->send_to) {
		prof_save (prof, prof->outfile);
		if (prof->outfile)
			fclose (prof->outfile);
	}

	mono_os_mutex_lock (&prof->mutex);

	g_hash_table_destroy (prof->classes);
	g_hash_table_destroy (prof->images);
	g_ptr_array_free (prof->methods, TRUE);
	g_free (prof->outfile_name);

	prof->methods = NULL;
	mono_os_mutex_unlock (&prof->mutex);
}
