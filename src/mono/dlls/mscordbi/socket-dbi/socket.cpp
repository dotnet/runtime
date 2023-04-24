// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: SOCKET.CPP
//

#include "socket.h"

#ifdef WIN32
#include <winsock2.h>
#include <ws2tcpip.h>
#else
#include <netdb.h>
#include <netinet/in.h>
#include <netinet/tcp.h>
#include <net/if.h>
#include <stdlib.h>
#include <string.h>
#include <sys/types.h>
#include <sys/select.h>
#include <sys/ioctl.h>
#include <sys/socket.h>
#include <unistd.h>
#ifdef HAVE_SYS_SOCKIO_H
#include <sys/sockio.h>
#endif
#include <sys/un.h>
#if defined(__APPLE__)
#include <sys/socketvar.h>
#endif
#include <errno.h>
#include <stdio.h>
#define INVALID_SOCKET -1
#define SOCKET_ERROR -1
#endif

Socket::~Socket()
{
    Close();
}

int Socket::OpenSocketAcceptConnection(const char *address, const char *port) {
    socketId = INVALID_SOCKET;

#ifdef WIN32
    WSADATA wsadata;
    int err;

    err = WSAStartup (2, &wsadata);
    if (err) {
        return -1;
    }
#endif

    struct addrinfo *result = NULL, *ptr = NULL, hints;
    int iResult;

    memset(&hints, 0, sizeof(hints));
    hints.ai_family = AF_UNSPEC;
    hints.ai_socktype = SOCK_STREAM;
    hints.ai_protocol = IPPROTO_TCP;

    // Resolve the server address and port
    iResult = getaddrinfo(address, port, &hints, &result);
    if (iResult != 0) {
        return -1;
    }

    // Attempt to connect to an address until one succeeds
    for (ptr = result; ptr != NULL; ptr = ptr->ai_next) {

        // Create a SOCKET for connecting to server
        socketId = socket(ptr->ai_family, ptr->ai_socktype, ptr->ai_protocol);

        if (socketId == INVALID_SOCKET) {
            return -1;
        }

        int flag = 1;
        if (setsockopt(socketId, SOL_SOCKET, SO_REUSEADDR, (char *)&flag, sizeof(int)))
            continue;

        iResult = bind(socketId, ptr->ai_addr, (int)ptr->ai_addrlen);
        if (iResult == SOCKET_ERROR)
            continue;

        iResult = listen(socketId, 16);
        if (iResult == SOCKET_ERROR)
            continue;

        break;
    }

    if (iResult != SOCKET_ERROR)
        socketId = accept(socketId, NULL, NULL);

    freeaddrinfo(result);

    return 1;
}

int Socket::Receive(char *buff, int buflen) {
    return recv(socketId, buff, buflen, 0);
}

void Socket::Close() {
#ifdef WIN32
  closesocket (socketId);
#else
  close (socketId);
#endif
}

int Socket::Send(const char *buff, int buflen) {
    return send(socketId, buff, buflen, 0);
}
