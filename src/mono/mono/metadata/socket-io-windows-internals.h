/*
* socket-io-windows-internals.h: Windows specific socket code.
*
* Copyright 2016 Microsoft
* Licensed under the MIT license. See LICENSE file in the project root for full license information.
*/
#ifndef __MONO_METADATA_SOCKET_IO_WINDOWS_INTERNALS_H__
#define __MONO_METADATA_SOCKET_IO_WINDOWS_INTERNALS_H__

#include <config.h>
#include <glib.h>
#include <mono/io-layer/io-layer.h>

SOCKET alertable_accept (SOCKET s, struct sockaddr *addr, int *addrlen, gboolean blocking);
int alertable_connect (SOCKET s, const struct sockaddr *name, int namelen, gboolean blocking);
int alertable_recv (SOCKET s, char *buf, int len, int flags, gboolean blocking);
int alertable_recvfrom (SOCKET s, char *buf, int len, int flags, struct sockaddr *from, int *fromlen, gboolean blocking);
int alertable_WSARecv (SOCKET s, LPWSABUF lpBuffers, DWORD dwBufferCount, LPDWORD lpNumberOfBytesRecvd, LPDWORD lpFlags, LPWSAOVERLAPPED lpOverlapped, LPWSAOVERLAPPED_COMPLETION_ROUTINE lpCompletionRoutine, gboolean blocking);
int alertable_send (SOCKET s, char *buf, int len, int flags, gboolean blocking);
int alertable_sendto (SOCKET s, const char *buf, int len, int flags, const struct sockaddr *to, int tolen, gboolean blocking);
int alertable_WSASend (SOCKET s, LPWSABUF lpBuffers, DWORD dwBufferCount, LPDWORD lpNumberOfBytesRecvd, DWORD lpFlags, LPWSAOVERLAPPED lpOverlapped, LPWSAOVERLAPPED_COMPLETION_ROUTINE lpCompletionRoutine, gboolean blocking);
BOOL alertable_TransmitFile (SOCKET hSocket, HANDLE hFile, DWORD nNumberOfBytesToWrite, DWORD nNumberOfBytesPerSend, LPOVERLAPPED lpOverlapped, LPTRANSMIT_FILE_BUFFERS lpTransmitBuffers, DWORD dwReserved, gboolean blocking);

#endif // __MONO_METADATA_SOCKET_IO_WINDOWS_INTERNALS_H__
