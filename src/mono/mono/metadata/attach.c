/**
 * \file
 * Support for attaching to the runtime from other processes.
 *
 * Author:
 *   Zoltan Varga (vargaz@gmail.com)
 *
 * Copyright 2007-2009 Novell, Inc (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <glib.h>
#include "attach.h"

#if defined(HOST_WIN32) && !defined(DISABLE_ATTACH)
#define DISABLE_ATTACH
#endif
#ifndef DISABLE_ATTACH

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <sys/stat.h>
#include <sys/un.h>
#include <netinet/in.h>
#include <fcntl.h>
#include <inttypes.h>
#include <pwd.h>
#include <errno.h>
#include <netdb.h>
#include <unistd.h>

#include <mono/metadata/assembly-internals.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/gc-internals.h>
#include <mono/utils/mono-threads.h>

#include <mono/utils/w32api.h>

/*
 * This module enables other processes to attach to a running mono process and
 * load agent assemblies. 
 * Communication is done through a UNIX Domain Socket located at
 * /tmp/mono-<USER>/.mono-<PID>.
 * We use a simplified version of the .net remoting protocol.
 * To increase security, and to avoid spinning up a listener thread on startup,
 * we follow the java implementation, and only start up the attach mechanism
 * when we receive a QUIT signal and there is a file named 
 * '.mono_attach_pid<PID>' in /tmp.
 *
 * SECURITY:
 * - This module allows loading of arbitrary code into a running mono runtime, so
 *   it is security critical.
 * - Security is based on controlling access to the unix file to which the unix 
 *   domain socket is bound. Permissions/ownership are set such that only the owner 
 *   of the process can access the socket.
 * - As an additional measure, the socket is only created when the process receives
 *   a SIGQUIT signal, which only its owner/root can send.
 * - The socket is kept in a directory whose ownership is checked before creating
 *   the socket. This could allow an attacker a kind of DOS attack by creating the 
 *   directory with the wrong permissions/ownership. However, the only thing such
 *   an attacker could prevent is the attaching of agents to the mono runtime.
 */

typedef struct {
	gboolean enabled;
} AgentConfig;

typedef struct {
	int bytes_sent;
} AgentStats;

/*******************************************************************/
/* Remoting Protocol type definitions from [MS-NRBF] and [MS-NRTP] */
/*******************************************************************/

typedef enum {
	PRIM_TYPE_INT32 = 8,
	PRIM_TYPE_INT64 = 9,
	PRIM_TYPE_NULL = 17,
	PRIM_TYPE_STRING = 18
} PrimitiveType;

static AgentConfig config;

static int listen_fd, conn_fd;

static char *ipc_filename;

static char *server_uri;

static MonoThreadHandle *receiver_thread_handle;

static volatile gboolean stop_receiver_thread;

static gboolean needs_to_start, started;

static void transport_connect (void);

static gsize WINAPI receiver_thread (void *arg);

static void transport_start_receive (void);

/*
 * Functions to decode protocol data
 */
static int
decode_byte (guint8 const *buf, guint8 const **endbuf, guint8 const *limit)
{
	*endbuf = buf + 1;
	g_assert (*endbuf <= limit);
	return buf [0];
}

static int
decode_int (const guint8 *buf)
{
	return (((int)buf [0]) << 0) | (((int)buf [1]) << 8) | (((int)buf [2]) << 16) | (((int)buf [3]) << 24);
}

static const char*
decode_string_value (guint8 const *buf, guint8 const **endbuf, guint8 const *limit)
{
	int type;
	gint32 length;
	guint8 const *p = buf;

	type = decode_byte (p, &p, limit);
	if (type == PRIM_TYPE_NULL) {
		*endbuf = p;
		return NULL;
	}
	g_assert (type == PRIM_TYPE_STRING);

	length = 0;
	while (TRUE) {
		guint8 b = decode_byte (p, &p, limit);
		
		length <<= 8;
		length += b;
		if (b <= 0x7f)
			break;
	}

	g_assert (length < (1 << 16));

	const char *s = (const char*)p;
	p += length + 1;

	g_assert (p <= limit);

	*endbuf = p;

	return s;
}

/********************************/
/*    AGENT IMPLEMENTATION      */
/********************************/

void
mono_attach_parse_options (char *options)
{
	if (!options)
		return;
	if (!strcmp (options, "disable"))
		config.enabled = FALSE;
}

void
mono_attach_init (void)
{
	config.enabled = TRUE;
}

/**
 * mono_attach_start:
 *
 * Start the attach mechanism if needed.  This is called from a signal handler so it must be signal safe.
 *
 * Returns: whenever it was started.
 */
gboolean
mono_attach_start (void)
{
	char path [256];
	int fd;

	if (started)
		return FALSE;

	/* Check for the existence of the trigger file */

	/* 
	 * We don't do anything with this file, and the only thing an attacker can do
	 * by creating it is to enable the attach mechanism if the process receives a 
	 * SIGQUIT signal, which can only be sent by the owner/root.
	 */
	snprintf (path, sizeof (path), "/tmp/.mono_attach_pid%" PRIdMAX, (intmax_t) getpid ());
	fd = open (path, O_RDONLY);
	if (fd == -1)
		return FALSE;
	close (fd);

	if (!config.enabled)
		/* Act like we started */
		return TRUE;

	if (started)
		return FALSE;

	/*
	 * Our startup includes non signal-safe code, so ask the finalizer thread to 
	 * do the actual startup.
	 */
	needs_to_start = TRUE;
	mono_gc_finalize_notify ();

	return TRUE;
}

/* Called by the finalizer thread when it is woken up */
void
mono_attach_maybe_start (void)
{
	if (!needs_to_start)
		return;

	needs_to_start = FALSE;
	if (!started) {
		transport_start_receive ();

		started = TRUE;
	}
}

void
mono_attach_cleanup (void)
{
	if (listen_fd)
		close (listen_fd);
	if (ipc_filename)
		unlink (ipc_filename);

	stop_receiver_thread = TRUE;
	if (conn_fd)
		/* This will cause receiver_thread () to break out of the read () call */
		close (conn_fd);

	/* Wait for the receiver thread to exit */
	if (receiver_thread_handle)
		mono_thread_info_wait_one_handle (receiver_thread_handle, 0, FALSE);
}

static int
mono_attach_load_agent (MonoDomain *domain, const char *agent, const char *args)
{
	HANDLE_FUNCTION_ENTER ();

	ERROR_DECL (error);
	MonoAssembly *agent_assembly;
	MonoImage *image;
	MonoMethod *method;
	guint32 entry;
	MonoArrayHandle main_args;
	gpointer pa [1];
	MonoImageOpenStatus open_status;
	int result = 0;

	MonoAssemblyOpenRequest req;
	mono_assembly_request_prepare_open (&req, MONO_ASMCTX_DEFAULT, mono_domain_default_alc (mono_domain_get ()));
	agent_assembly = mono_assembly_request_open (agent, &req, &open_status);
	if (!agent_assembly) {
		fprintf (stderr, "Cannot open agent assembly '%s': %s.\n", agent, mono_image_strerror (open_status));
		result = 2;
		goto exit;
	}

	/* 
	 * Can't use mono_jit_exec (), as it sets things which might confuse the
	 * real Main method.
	 */
	image = mono_assembly_get_image_internal (agent_assembly);
	entry = mono_image_get_entry_point (image);
	if (!entry) {
		g_print ("Assembly '%s' doesn't have an entry point.\n", mono_image_get_filename (image));
		result = 1;
		goto exit;
	}

	method = mono_get_method_checked (image, entry, NULL, NULL, error);
	if (method == NULL){
		g_print ("The entry point method of assembly '%s' could not be loaded due to %s\n", agent, mono_error_get_message (error));
		result = 1;
		goto exit;
	}
	
	main_args = mono_array_new_handle (domain, mono_defaults.string_class, (args == NULL) ? 0 : 1, error);
	if (MONO_HANDLE_IS_NULL (main_args)) {
		g_print ("Could not allocate main method args due to %s\n", mono_error_get_message (error));
		result = 1;
		goto exit;
	}

	if (args) {
		MonoStringHandle args_str = mono_string_new_handle (domain, args, error);
		if (!is_ok (error)) {
			g_print ("Could not allocate main method arg string due to %s\n", mono_error_get_message (error));
			result = 1;
			goto exit;
		}
		MONO_HANDLE_ARRAY_SETREF (main_args, 0, args_str);
	}

	pa [0] = MONO_HANDLE_RAW (main_args);
	MonoObject *exc;
	mono_runtime_try_invoke (method, NULL, pa, &exc, error);
	if (!is_ok (error)) {
		g_print ("The entry point method of assembly '%s' could not be executed due to %s\n", agent, mono_error_get_message (error));
		result = 1;
		goto exit;
	}

	result = 0;
exit:
	mono_error_cleanup (error);
	HANDLE_FUNCTION_RETURN_VAL (result);
}

/*
 * ipc_connect:
 *
 *   Create a UNIX domain socket and bind it to a file in /tmp.
 *
 * SECURITY: This routine is _very_ security critical since we depend on the UNIX
 * permissions system to prevent attackers from connecting to the socket.
 */
static void
ipc_connect (void)
{
	struct sockaddr_un name;
	int sock, res;
	size_t size;
	char *filename, *directory;
	struct stat stat;
	struct passwd pwbuf;
	char buf [1024];
	struct passwd *pw;

	if (getuid () != geteuid ()) {
		fprintf (stderr, "attach: disabled listening on an IPC socket when running in setuid mode.\n");
		return;
	}

	/* Create the socket.   */  
	sock = socket (PF_UNIX, SOCK_STREAM, 0);
	if (sock < 0) {
		perror ("attach: failed to create IPC socket");
		return;
	}

	/* 
	 * For security reasons, create a directory to hold the listening socket,
	 * since there is a race between bind () and chmod () below.
	 */
	/* FIXME: Use TMP ? */
	pw = NULL;
#ifdef HAVE_GETPWUID_R
	res = getpwuid_r (getuid (), &pwbuf, buf, sizeof (buf), &pw);
#else
	pw = getpwuid(getuid ());
	res = pw != NULL ? 0 : 1;
#endif
	if (res != 0) {
		fprintf (stderr, "attach: getpwuid_r () failed.\n");
		return;
	}
	g_assert (pw);
	directory = g_strdup_printf ("/tmp/mono-%s", pw->pw_name);
	res = mkdir (directory, S_IRUSR | S_IWUSR | S_IXUSR);
	if (res != 0) {
		if (errno == EEXIST) {
			/* Check type and permissions */
			res = lstat (directory, &stat);
			if (res != 0) {
				perror ("attach: lstat () failed");
				return;
			}
			if (!S_ISDIR (stat.st_mode)) {
				fprintf (stderr, "attach: path '%s' is not a directory.\n", directory);
				return;
			}
			if (stat.st_uid != getuid ()) {
				fprintf (stderr, "attach: directory '%s' is not owned by the current user.\n", directory);
				return;
			}
			if ((stat.st_mode & S_IRWXG) != 0 || (stat.st_mode & S_IRWXO) || ((stat.st_mode & S_IRWXU) != (S_IRUSR | S_IWUSR | S_IXUSR))) {
				fprintf (stderr, "attach: directory '%s' should have protection 0700.\n", directory);
				return;
			}
		} else {
			perror ("attach: mkdir () failed");
			return;
		}
	}

	filename = g_strdup_printf ("%s/.mono-%" PRIdMAX, directory, (intmax_t) getpid ());
	unlink (filename);

	/* Bind a name to the socket.   */
	name.sun_family = AF_UNIX;
	strcpy (name.sun_path, filename);

	size = (offsetof (struct sockaddr_un, sun_path)
			+ strlen (name.sun_path) + 1);

	if (bind (sock, (struct sockaddr *) &name, size) < 0) {
		fprintf (stderr, "attach: failed to bind IPC socket '%s': %s\n", filename, strerror (errno));
		close (sock);
		return;
	}

	/* Set permissions */
	res = chmod (filename, S_IRUSR | S_IWUSR);
	if (res != 0) {
		perror ("attach: failed to set permissions on IPC socket");
		close (sock);
		unlink (filename);
		return;
	}

	res = listen (sock, 16);
	if (res != 0) {
		fprintf (stderr, "attach: listen () failed: %s\n", strerror (errno));
		exit (1);
	}

	listen_fd = sock;

	ipc_filename = g_strdup (filename);

	server_uri = g_strdup_printf ("unix://%s/.mono-%" PRIdMAX "?/vm", directory, (intmax_t) getpid ());

	g_free (filename);
	g_free (directory);
}

static void
transport_connect (void)
{
	ipc_connect ();
}

#if 0

static void
transport_send (int fd, guint8 *data, int len)
{
	int res;

	stats.bytes_sent += len;
	//printf ("X: %d\n", stats.bytes_sent);

	res = write (fd, data, len);
	if (res != len) {
		/* FIXME: What to do here ? */
	}
}

#endif

static void
transport_start_receive (void)
{
	ERROR_DECL (error);
	MonoInternalThread *internal;

	transport_connect ();

	if (!listen_fd)
		return;

	internal = mono_thread_create_internal (mono_get_root_domain (), (gpointer)receiver_thread, NULL, MONO_THREAD_CREATE_FLAGS_NONE, error);
	mono_error_assert_ok (error);

	receiver_thread_handle = mono_threads_open_thread_handle (internal->handle);
	g_assert (receiver_thread_handle);
}

static gsize WINAPI
receiver_thread (void *arg)
{
	MonoInternalThread *internal = mono_thread_internal_current ();

	mono_thread_set_name_constant_ignore_error (internal, "Attach receiver", MonoSetThreadNameFlag_Permanent);

	/* Ask the runtime to not abort this thread */
	//internal->flags |= MONO_THREAD_FLAG_DONT_MANAGE;
	/* Ask the runtime to not wait for this thread */
	internal->state |= ThreadState_Background;

	printf ("attach: Listening on '%s'...\n", server_uri);

	while (TRUE) {
		conn_fd = accept (listen_fd, NULL, NULL);
		if (conn_fd == -1)
			/* Probably closed by mono_attach_cleanup () */
			return 0;

		printf ("attach: Connected.\n");

		guint8* body = NULL;

		while (TRUE) {
			guint8 buffer [6];

			/* Read Header */
			int res = read (conn_fd, buffer, 6);

			if (res == -1 && errno == EINTR)
				continue;

			if (res == -1 || stop_receiver_thread)
				break;

			if (res != 6)
				break;

			if (memcmp (buffer, "MONO", 4) != 0 || buffer [4] != 1 || buffer [5] != 0) {
				fprintf (stderr, "attach: message from server has unknown header.\n");
				break;
			}

			/* Read content length */
			res = read (conn_fd, buffer, 4);
			if (res != 4)
				break;

			const int content_len = decode_int (buffer);

			/* Read message body */
			body = (guint8 *)g_malloc (content_len);
			res = read (conn_fd, body, content_len);
			if (res != content_len)
				break;

			guint8 const * p = body;
			guint8 const * const p_end = body + content_len;

			char const * const cmd = decode_string_value (p, &p, p_end);
			if (cmd == NULL)
				break;

			// 10: 7:attach\0 + one byte each for the types of cmd, name, args.
			g_assert (content_len >= 10 && !memcmp (cmd, "attach", 7));

			char const * const agent_name = decode_string_value (p, &p, p_end);
			char const * const agent_args = decode_string_value (p, &p, p_end);

			printf ("attach: Loading agent '%s'.\n", agent_name);
			mono_attach_load_agent (mono_domain_get (), agent_name, agent_args);

			g_free (body);
			body = NULL;

			// FIXME: Send back a result
		}

		g_free (body);
		body = NULL;

		close (conn_fd);
		conn_fd = 0;

		printf ("attach: Disconnected.\n");

		if (stop_receiver_thread)
			break;
	}

	return 0;
}

#else /* DISABLE_ATTACH */

void
mono_attach_parse_options (char *options)
{
}

void
mono_attach_init (void)
{
}

gboolean
mono_attach_start (void)
{
	return FALSE;
}

void
mono_attach_maybe_start (void)
{
}

void
mono_attach_cleanup (void)
{
}

#endif /* DISABLE_ATTACH */
