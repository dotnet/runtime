//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


#ifndef __DBG_TRANSPORT_PROXY_INCLUDED
#define __DBG_TRANSPORT_PROXY_INCLUDED

#ifdef FEATURE_DBGIPC_TRANSPORT_VM

#include "dbgproxy.h"

//
// Provides access to the debugging proxy process from the left side.
//

// The answers the proxy can give to us during runtime startup.
enum DbgProxyResult
{
    RequestSuccessful,      // Successfully registered the runtime, no debugger is currently interested in us
    RequestTimedOut,        // Timed-out trying to reach the proxy (it's probably not configured or started)
    PendingDebuggerAttach   // Successfully registered the runtime, a debugger wishes to attach before code is run
};

class DbgTransportProxy
{
public:
    DbgTransportProxy();

    // Startup and shutdown. Initialization takes the port number (in host byte order) that the left side
    // will wait on for debugger connections.
    HRESULT Init(unsigned short usPort);
    void Shutdown();

    // Talk with the proxy process and register this instantiation of the runtime with it. The reply from the
    // proxy will indicate whether a debugger wishes to attach to us before any managed code is allowed to
    // run. This method is synchronous and will wait for the reply from the proxy (or a timeout).
    DbgProxyResult RegisterWithProxy();

private:
    unsigned int    m_uiPID;                            // PID of the current process
    unsigned short  m_usPort;                           // Port the LS waits on for debugger connections
    unsigned short  m_usProxyPort;                      // Port the proxy waits on for requests

    SecConnMgr     *m_pConnectionManager;               // Factory for network connections
};

#endif // FEATURE_DBGIPC_TRANSPORT_VM

#endif // __DBG_TRANSPORT_PROXY_INCLUDED
