// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_config.h"

typedef enum
{
    TcpState_Unknown,
    TcpState_Closed,
    TcpState_Listen,
    TcpState_SynSent,
    TcpState_SynReceived,
    TcpState_Established,
    TcpState_FinWait1,
    TcpState_FinWait2,
    TcpState_CloseWait,
    TcpState_Closing,
    TcpState_LastAck,
    TcpState_TimeWait,
    TcpState_DeleteTcb
} TcpState;

PALEXPORT int32_t SystemNative_MapTcpState(int32_t tcpState);
