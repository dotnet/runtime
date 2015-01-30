//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


#include "stdafx.h"
#include "dbgtransportsession.h"
#include "dbgtransportproxy.h"
#include "dbgproxy.h"

#ifdef FEATURE_DBGIPC_TRANSPORT_VM

//
// Provides access to the debugging proxy process from the left side.
//

DbgTransportProxy::DbgTransportProxy()
{
    memset(this, 0, sizeof(*this));
}

// Startup and shutdown. Initialization takes the port number (in host byte order) that the left side will
// wait on for debugger connections.
HRESULT DbgTransportProxy::Init(unsigned short usPort)
{
    // Query the debugger configuration for the current user, this will give us the port number the proxy is
    // using. By the time the this method is called we know that debugging is configured on for this process.
    DbgConfiguration sDbgConfig;
    if (!GetDebuggerConfiguration(&sDbgConfig))
        return E_OUTOFMEMORY;
    _ASSERTE(sDbgConfig.m_fEnabled);
    m_usProxyPort = sDbgConfig.m_usProxyPort;

    m_usPort = usPort;

    // Initialize some data the proxy needs when we register.
    m_uiPID = GetCurrentProcessId();

    // Allocate the connection manager and initialize it.
    m_pConnectionManager = AllocateSecConnMgr();
    if (m_pConnectionManager == NULL)
        return E_OUTOFMEMORY;

    SecConnStatus eStatus = m_pConnectionManager->Initialize();
    if (eStatus != SCS_Success)
        return eStatus == SCS_OutOfMemory ? E_OUTOFMEMORY : E_FAIL;

    return S_OK;
}

void DbgTransportProxy::Shutdown()
{
    if (m_pConnectionManager)
        m_pConnectionManager->Destroy();
}

// Talk with the proxy process and register this instantiation of the runtime with it. The reply from the
// proxy will indicate whether a debugger wishes to attach to us before any managed code is allowed to
// run. This method is synchronous and will wait for the reply from the proxy (or a timeout).
DbgProxyResult DbgTransportProxy::RegisterWithProxy()
{
    // Attempt a connection to the proxy. Any failure is treated as the proxy not being there. No time for
    // retries and timeouts, we're holding up process startup.
    SecConn *pConnection = NULL;
    SecConnStatus eStatus = m_pConnectionManager->AllocateConnection(DBGIPC_NTOHL(inet_addr("127.0.0.1")), 
                                                                     m_usProxyPort,
                                                                     &pConnection);
    if (eStatus == SCS_Success)
        eStatus = pConnection->Connect();

    if (eStatus != SCS_Success)
    {
        DbgTransportLog(LC_Proxy, "DbgTransportProxy::RegisterWithProxy(): failed to connect to proxy");
        if (pConnection)
            pConnection->Destroy();
        return RequestTimedOut;
    }

    // Format a registration message for the proxy.
    DbgProxyRegisterRuntimeMessage sRequest;
    sRequest.m_sHeader.m_eType = DPMT_RegisterRuntime;
    sRequest.m_sHeader.m_uiRequestID = 0;
    sRequest.m_sHeader.m_uiMagic = DBGPROXY_MAGIC_VALUE(&sRequest.m_sHeader);
    sRequest.m_sHeader.m_uiReserved = 0;
    sRequest.m_uiMajorVersion = kCurrentMajorVersion;
    sRequest.m_uiMinorVersion = kCurrentMinorVersion;
    sRequest.m_uiPID = m_uiPID;
    sRequest.m_usPort = m_usPort;

    // Send the message. If we can't even do that we act as though the proxy timed out on us (runtime startup
    // will continue and this process will not be debuggable).
    if (!pConnection->Send((unsigned char*)&sRequest, sizeof(sRequest)))
    {
        DbgTransportLog(LC_Proxy, "DbgTransportProxy::RegisterWithProxy(): failed to send registration to proxy");
        return RequestTimedOut;
    }

    // Wait for the reply.
    DbgProxyMessageHeader sReply;
    if (!pConnection->Receive((unsigned char*)&sReply, sizeof(sReply)))
    {
        DbgTransportLog(LC_Proxy, "DbgTransportProxy::RegisterWithProxy(): failed to receive reply from proxy");
        return RequestTimedOut;
    }

    // Validate reply.
    if (sReply.m_eType != DPMT_RuntimeRegistered ||
        sReply.VariantData.RuntimeRegistered.m_uiMajorVersion != (unsigned)kCurrentMajorVersion ||
        sReply.m_uiMagic != DBGPROXY_MAGIC_VALUE(&sReply))
    {
        DbgTransportLog(LC_Proxy, "DbgTransportProxy::RegisterWithProxy(): bad reply from the proxy");
        return RequestTimedOut;
    }

    bool fWaitForDebugger = sReply.VariantData.RuntimeRegistered.m_fWaitForDebuggerAttach;
    DbgTransportLog(LC_Proxy, "DbgTransportProxy::RegisterWithProxy(): %s for the debugger",
                    fWaitForDebugger ? "Waiting" : "Not waiting");
    return fWaitForDebugger ? PendingDebuggerAttach : RequestSuccessful;
}

#endif // FEATURE_DBGIPC_TRANSPORT_VM
