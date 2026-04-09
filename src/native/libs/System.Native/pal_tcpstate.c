// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"
#include "pal_types.h"
#include "pal_tcpstate.h"

#if HAVE_TCP_FSM_H
#include <netinet/tcp_fsm.h>
#elif HAVE_IOS_NETINET_TCPFSM_H
#include "ios/netinet/tcp_fsm.h"
#elif HAVE_TCP_H_TCPSTATE_ENUM
#include <netinet/tcp.h>
#endif

int32_t SystemNative_MapTcpState(int32_t tcpState)
{
    switch (tcpState)
    {
#if HAVE_TCP_FSM_H || HAVE_IOS_NETINET_TCPFSM_H
        case TCPS_CLOSED:
            return TcpState_Closed;
        case TCPS_LISTEN:
            return TcpState_Listen;
        case TCPS_SYN_SENT:
            return TcpState_SynSent;
        case TCPS_SYN_RECEIVED:
            return TcpState_SynReceived;
        case TCPS_ESTABLISHED:
            return TcpState_Established;
        case TCPS_CLOSE_WAIT:
            return TcpState_CloseWait;
        case TCPS_FIN_WAIT_1:
            return TcpState_FinWait1;
        case TCPS_CLOSING:
            return TcpState_Closing;
        case TCPS_FIN_WAIT_2:
            return TcpState_FinWait2;
        case TCPS_TIME_WAIT:
            return TcpState_TimeWait;
#elif HAVE_TCP_H_TCPSTATE_ENUM
        case TCP_ESTABLISHED:
            return TcpState_Established;
        case TCP_SYN_SENT:
            return TcpState_SynSent;
        case TCP_SYN_RECV:
            return TcpState_SynReceived;
        case TCP_FIN_WAIT1:
            return TcpState_FinWait1;
        case TCP_FIN_WAIT2:
            return TcpState_FinWait2;
        case TCP_TIME_WAIT:
            return TcpState_TimeWait;
        case TCP_CLOSE:
            return TcpState_Closing;
        case TCP_CLOSE_WAIT:
            return TcpState_CloseWait;
        case TCP_LAST_ACK:
            return TcpState_LastAck;
        case TCP_LISTEN:
            return TcpState_Listen;
        case TCP_CLOSING:
            return TcpState_Closing;
#endif
        default:
            // System does not have TCP states defined in either tcp.h or tcp_fsm.h; falling back to always returning unknown.
            return TcpState_Unknown;
    }
}
