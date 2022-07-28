// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: SOCKET.H
//

#ifndef __SOCKET_DBI_H__
#define __SOCKET_DBI_H__

#ifdef WIN32
#include <winsock2.h>
#endif

class Socket {
#ifdef WIN32
    SOCKET socketId;
#else
    long long socketId;
#endif

public:
    ~Socket();
    int OpenSocketAcceptConnection(const char *address, const char *port);
    void Close();
    int Receive(char *buff, int buflen);
    int Send(const char *buff, int buflen);
};

#endif
