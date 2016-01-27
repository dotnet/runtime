// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: rotor/pal/corunix/include/pal/dtrace_protocal.h
//

//
// Header file for the protocals between CLR and Dtrace server
//
// ======================================================================================

#ifndef DTRACE_PROTOCOL_H
#define DTRACE_PROTOCOL_H

// Start DTrace Consumer by Unix Domain App
#define kServerSocketPath "/Library/Application Support/com.microsoft.clr.CFDtraceServer/Socket"
#define kPacketTypeStartDtrace 1
#define kPacketTypeReply 3
#define kMaxMessageSize 318
#define kPacketMaximumSize 102400

struct PacketHeader {
    int          fType;              // for request from client to server, it should be kPacketTypeStartDtrace
                                     // for reply from server to client, it should be kPacketTypeReply
    unsigned int        fSize;       // includes size of header itself
};

struct PacketStartDTrace {               // reply: PacketReply
    PacketHeader    fHeader;             // fType is kPacketTypeStartDtrace
    char            fMessage[kMaxMessageSize];       // message to print
};

struct PacketReply {                    // reply: n/a
    PacketHeader    fHeader;            // fType is kPacketTypeReply
    int             fErr;               // result of operation, errno-style
};

#endif // DTRACE_PROTOCOL
