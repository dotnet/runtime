// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// ProfAttachServer.inl
// 

// 
// Inlined implementations of portions of ProfilingAPIAttachServer and helpers, which are
// used by the AttachThread running in the target profilee (server end of the pipe) to
// receive and carry out requests that are sent by the trigger (client end of the pipe).
// 

// ======================================================================================


// ----------------------------------------------------------------------------
// RequestMessageVerifier::RequestMessageVerifier()
//
// Description: 
//    Constructor that takes stream of bytes read by the target profilee on its pipe. 
//    After construction, call Verify() to verify the stream of bytes makes a
//    well-formed message.
//
// Arguments:
//    * pbRequestMessage - Bytes read from pipe
//    * cbRequestMessage - Number of bytes read from pipe.
//

inline RequestMessageVerifier::RequestMessageVerifier(
    LPCBYTE pbRequestMessage,
    DWORD cbRequestMessage) :
        m_pbRequestMessage(pbRequestMessage),
        m_cbRequestMessage(cbRequestMessage)
{
    LIMITED_METHOD_CONTRACT;

    INDEBUG(m_fVerified = FALSE);
}
        
// ----------------------------------------------------------------------------
// ProfilingAPIAttachServer::ProfilingAPIAttachServer()
//
// Description: 
//     Constructor for ProfilingAPIAttachServer, which owns the server end of the pipe
//     running in the target profilee

inline ProfilingAPIAttachServer::ProfilingAPIAttachServer() :
        m_dwMillisecondsMaxPerWait(0)
{
    LIMITED_METHOD_CONTRACT;
}

inline ProfilingAPIAttachServer::~ProfilingAPIAttachServer()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    if (IsValidHandle(m_hPipeServer))
    {
        // m_hPipeServer's destructor is about to destroy the pipe
        LOG((
            LF_CORPROF, 
            LL_INFO10, 
            "**PROF: Finished communication; closing attach pipe server.\n"));
    }
}


// ----------------------------------------------------------------------------
// ProfilingAPIAttachServer::WriteResponseToPipe
// 
// Description:
//    Sends response bytes across pipe to trigger process.
//    
// Arguments:
//    * pvResponse - Pointer to bytes to send
//    * cbResponse - How many bytes to send
//        
// Return Value:
//    HRESULT indicating success or failure.
//    
// Notes:
//    * Purposely does NOT log an event on failure, as an event at this stage would be
//        confusing to the user. The requested operation (e.g., Attach) has already been
//        performed; this is just the part that communicates the result back to the
//        trigger. There's nothing the user could (or would want to) do if response
//        communication failed. Either the attach worked or not, and that's already been
//        logged to the event log.
//        

inline HRESULT ProfilingAPIAttachServer::WriteResponseToPipe(
    LPVOID pvResponse,
    DWORD cbResponse)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    _ASSERTE(IsValidHandle(m_hPipeServer));
    _ASSERTE(pvResponse != NULL);

    DWORD cbWritten;

    HRESULT hr = WriteResponseToPipeNoBufferSizeCheck(
        pvResponse,
        cbResponse,
        &cbWritten);

    // Check the buffer size against what was written
    if (SUCCEEDED(hr) && (cbResponse != cbWritten))
    {
        // Partial response sent.  Be sure hr reflects there was a problem
        hr = E_UNEXPECTED;
    }

    return hr;
}
