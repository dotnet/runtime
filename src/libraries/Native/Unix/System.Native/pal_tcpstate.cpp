// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_config.h"
#include "pal_utilities.h"
#include "pal_networkstatistics.h"

#if HAVE_TCP_FSM_H
#include <netinet/tcp_fsm.h>
#elif HAVE_TCP_H_TCPSTATE_ENUM
#include <netinet/tcp.h>
#else
#error System must have TCP states defined in either tcp.h or tcp_fsm.h.
#endif

extern "C" TcpState SystemNative_MapTcpState(int32_t tcpState)
{
    switch (tcpState)
    {
#if HAVE_TCP_FSM_H
        case TCPS_CLOSED:
            return Closed;
        case TCPS_LISTEN:
            return Listen;
        case TCPS_SYN_SENT:
            return SynSent;
        case TCPS_SYN_RECEIVED:
            return SynReceived;
        case TCPS_ESTABLISHED:
            return Established;
        case TCPS_CLOSE_WAIT:
            return CloseWait;
        case TCPS_FIN_WAIT_1:
            return FinWait1;
        case TCPS_CLOSING:
            return Closing;
        case TCPS_FIN_WAIT_2:
            return FinWait2;
        case TCPS_TIME_WAIT:
            return TimeWait;
        default:
            return Unknown;
#elif HAVE_TCP_H_TCPSTATE_ENUM
        case TCP_ESTABLISHED:
            return Established;
        case TCP_SYN_SENT:
            return SynSent;
        case TCP_SYN_RECV:
            return SynReceived;
        case TCP_FIN_WAIT1:
            return FinWait1;
        case TCP_FIN_WAIT2:
            return FinWait2;
        case TCP_TIME_WAIT:
            return TimeWait;
        case TCP_CLOSE:
            return Closing;
        case TCP_CLOSE_WAIT:
            return CloseWait;
        case TCP_LAST_ACK:
            return LastAck;
        case TCP_LISTEN:
            return Listen;
        case TCP_CLOSING:
            return Closing;
        default:
            return Unknown;
#endif
    }
}
