#ifndef __MONO_PROFHELPER_H__
#define __MONO_PROFHELPER_H__

#ifndef HOST_WIN32
#include <sys/select.h>
#endif
#ifdef HOST_WIN32
#include <winsock2.h>
#endif

void mono_profhelper_add_to_fd_set (fd_set *set, int fd, int *max_fd);
void mono_profhelper_close_socket_fd (int fd);
void mono_profhelper_setup_command_server (int *server_socket, int *command_port, const char* profiler_name);

#endif
