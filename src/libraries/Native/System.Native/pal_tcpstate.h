#pragma once

#include "pal_config.h"

enum TcpState
{
    Unknown,
    Closed,
    Listen,
    SynSent,
    SynReceived,
    Established,
    FinWait1,
    FinWait2,
    CloseWait,
    Closing,
    LastAck,
    TimeWait,
    DeleteTcb
};

extern "C" TcpState MapTcpState(int32_t tcpState);
