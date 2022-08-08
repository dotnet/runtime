/*
 * helper.c: Helper code shared between various profilers
 *
 *
 * Copyright 2019 Microsoft Corporation
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>

#include <mono/utils/mono-logger-internals.h>

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
}

void
mono_profhelper_add_to_fd_set (fd_set *set, SOCKET fd, int *max_fd)
{
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
}
