/*
 * helper.c: Helper code shared between various profilers
 *
 *
 * Copyright 2019 Microsoft Corporation
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>

#if !defined (HOST_WASM)
#include <mono/utils/mono-logger-internals.h>
#endif

#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#include <errno.h>
#include <glib.h>

#ifndef HOST_WIN32
#include <netinet/in.h>
#endif
#ifndef HOST_WIN32
#include <sys/socket.h>
#endif
#ifdef HOST_WIN32
#include <winsock2.h>
#include <ws2tcpip.h>
#endif

#include "helper.h"

#include <mono/metadata/profiler.h>
#include <mono/metadata/callspec.h>
#include <mono/utils/mono-logger-internals.h>

void mono_profhelper_parse_profiler_args (const char *desc, MonoCallSpec *callspec, double *desired_sample_interval_ms);

void
mono_profhelper_close_socket_fd (SOCKET fd)
{
#ifdef HOST_WIN32
	closesocket (fd);
#else
	close (fd);
#endif
}

void
mono_profhelper_setup_command_server (SOCKET *server_socket, int *command_port, const char* profiler_name)
{
#if !defined (HOST_WASM)
	*server_socket = socket (PF_INET, SOCK_STREAM, 0);

	if (*server_socket == INVALID_SOCKET) {
		mono_profiler_printf_err ("Could not create log profiler server socket: %s", g_strerror (errno));
		exit (1);
	}

	struct sockaddr_in server_address;

	memset (&server_address, 0, sizeof (server_address));
	server_address.sin_family = AF_INET;
	server_address.sin_addr.s_addr = INADDR_ANY;
	server_address.sin_port = htons (GINT_TO_UINT16 (*command_port));

	if (bind (*server_socket, (struct sockaddr *) &server_address, sizeof (server_address)) == SOCKET_ERROR) {
		mono_profiler_printf_err ("Could not bind %s profiler server socket on port %d: %s", profiler_name, *command_port, g_strerror (errno));
		mono_profhelper_close_socket_fd (*server_socket);
		exit (1);
	}

	if (listen (*server_socket, 1) == SOCKET_ERROR) {
		mono_profiler_printf_err ("Could not listen on %s profiler server socket: %s", profiler_name, g_strerror (errno));
		mono_profhelper_close_socket_fd (*server_socket);
		exit (1);
	}

	socklen_t slen = sizeof (server_address);

	if (getsockname (*server_socket, (struct sockaddr *) &server_address, &slen)) {
		mono_profiler_printf_err ("Could not retrieve assigned port for %s profiler server socket: %s", profiler_name, g_strerror (errno));
		mono_profhelper_close_socket_fd (*server_socket);
		exit (1);
	}

	*command_port = ntohs (server_address.sin_port);
#endif
}

void
mono_profhelper_add_to_fd_set (fd_set *set, SOCKET fd, int *max_fd)
{
#if !defined (HOST_WASM)
	/*
	 * This should only trigger for the basic FDs (server socket, pipes) at
	 * startup if for some mysterious reason they're too large. In this case,
	 * the profiler really can't function, and we're better off printing an
	 * error and exiting.
	 */
#ifndef HOST_WIN32
	if (fd >= FD_SETSIZE) {
		mono_profiler_printf_err ("File descriptor is out of bounds for fd_set: %d", fd);
		exit (1);
	}
#endif

	FD_SET (fd, set);

	if (*max_fd < GUINT64_TO_INT(fd))
		*max_fd = (int)fd;
#endif
}

static gboolean
match_option (const char *arg, const char *opt_name, const char **rval)
{
	if (rval) {
		const char *end = strchr (arg, '=');

		*rval = NULL;
		if (!end)
			return !strcmp (arg, opt_name);

		if (strncmp (arg, opt_name, strlen (opt_name)) || (end - arg) > (ptrdiff_t)strlen (opt_name) + 1)
			return FALSE;
		*rval = end + 1;
		return TRUE;
	} else {
		//FIXME how should we handle passing a value to an arg that doesn't expect it?
		return !strcmp (arg, opt_name);
	}
}

static void
parse_arg (const char *arg, MonoCallSpec *callspec, double *desired_sample_interval_ms)
{
	const char *val;

	if (match_option (arg, "callspec", &val)) {
		if (!val)
			val = "";
		if (val[0] == '\"')
			++val;
		char *spec = g_strdup (val);
		size_t speclen = strlen (val);
		if (speclen > 0 && spec[speclen - 1] == '\"')
			spec[speclen - 1] = '\0';
		char *errstr;
		if (!mono_callspec_parse (spec, callspec, &errstr)) {
			mono_profiler_printf_err ("Could not parse callspec '%s': %s", spec, errstr);
			g_free (errstr);
			mono_callspec_cleanup (callspec);
		}
		g_free (spec);
	}
	else if (match_option (arg, "interval", &val)) {
		char *end;
		*desired_sample_interval_ms = strtod (val, &end);
	}
}

void
mono_profhelper_parse_profiler_args (const char *desc, MonoCallSpec *callspec, double *desired_sample_interval_ms)
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
					parse_arg (buffer, callspec, desired_sample_interval_ms);
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
		parse_arg (buffer, callspec, desired_sample_interval_ms);
	}

	g_free (buffer);
}
