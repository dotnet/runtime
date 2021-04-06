// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB.CPP
//

#include <cordb-appdomain.h>
#include <cordb-assembly.h>
#include <cordb-breakpoint.h>
#include <cordb-code.h>
#include <cordb-eval.h>
#include <cordb-function.h>
#include <cordb-process.h>
#include <cordb-stepper.h>
#include <cordb-thread.h>
#include <cordb.h>
#include <socket.h>

#define DEBUG_ADDRESS "127.0.0.1"

MONO_API HRESULT CoreCLRCreateCordbObjectEx(
    int iDebuggerVersion, DWORD pid, LPCWSTR lpApplicationGroupId, HMODULE hmodTargetCLR, void** ppCordb)
{
    LOG((LF_CORDB, LL_INFO100000, "CoreCLRCreateCordbObjectEx\n"));
    *ppCordb = new Cordb(pid);
    return S_OK;
}

static void receive_thread(Connection* c)
{
    c->Receive();
}

static void debugger_thread(void* m_pProcess)
{
    Connection* connection = new Connection((CordbProcess*)m_pProcess, ((CordbProcess*)m_pProcess)->GetCordb());
    ((CordbProcess*)m_pProcess)->SetConnection(connection);
    connection->StartConnection();
    connection->TransportHandshake();
    connection->LoopSendReceive();
    connection->CloseConnection();
}

HRESULT Cordb::Initialize(void)
{
    LOG((LF_CORDB, LL_INFO100000, "Cordb - Initialize - NOT IMPLEMENTED\n"));
    return S_OK;
}

HRESULT Cordb::Terminate(void)
{
    LOG((LF_CORDB, LL_INFO100000, "Cordb - Terminate - NOT IMPLEMENTED\n"));
    return S_OK;
}

HRESULT Cordb::SetManagedHandler(ICorDebugManagedCallback* pCallback)
{
    LOG((LF_CORDB, LL_INFO1000000, "Cordb - SetManagedHandler - IMPLEMENTED\n"));
    this->m_pCallback = pCallback;
    this->GetCallback()->AddRef();
    return S_OK;
}

HRESULT Cordb::SetUnmanagedHandler(ICorDebugUnmanagedCallback* pCallback)
{
    LOG((LF_CORDB, LL_INFO100000, "Cordb - SetUnmanagedHandler - NOT IMPLEMENTED\n"));
    return S_OK;
}

HRESULT Cordb::CreateProcess(LPCWSTR                    lpApplicationName,
                             LPWSTR                     lpCommandLine,
                             LPSECURITY_ATTRIBUTES      lpProcessAttributes,
                             LPSECURITY_ATTRIBUTES      lpThreadAttributes,
                             BOOL                       bInheritHandles,
                             DWORD                      dwCreationFlags,
                             PVOID                      lpEnvironment,
                             LPCWSTR                    lpCurrentDirectory,
                             LPSTARTUPINFOW             lpStartupInfo,
                             LPPROCESS_INFORMATION      lpProcessInformation,
                             CorDebugCreateProcessFlags debuggingFlags,
                             ICorDebugProcess**         ppProcess)
{
    LOG((LF_CORDB, LL_INFO100000, "Cordb - CreateProcess - NOT IMPLEMENTED\n"));
    return S_OK;
}

HRESULT Cordb::DebugActiveProcess(DWORD id, BOOL win32Attach, ICorDebugProcess** ppProcess)
{
    LOG((LF_CORDB, LL_INFO1000000, "Cordb - DebugActiveProcess - IMPLEMENTED\n"));
    m_pProcess = new CordbProcess(this);
    m_pProcess->InternalAddRef();
    m_pProcess->QueryInterface(IID_ICorDebugProcess, (void**)ppProcess);

    DWORD thread_id;
    CreateThread(NULL, 0, (LPTHREAD_START_ROUTINE)debugger_thread, m_pProcess, 0, &thread_id);
    return S_OK;
}

HRESULT Cordb::EnumerateProcesses(ICorDebugProcessEnum** ppProcess)
{
    LOG((LF_CORDB, LL_INFO100000, "Cordb - EnumerateProcesses - NOT IMPLEMENTED\n"));
    return S_OK;
}

HRESULT Cordb::GetProcess(DWORD dwProcessId, ICorDebugProcess** ppProcess)
{
    m_pProcess->QueryInterface(IID_ICorDebugProcess, (void**)ppProcess);
    return S_OK;
}

HRESULT Cordb::CanLaunchOrAttach(DWORD dwProcessId, BOOL win32DebuggingEnabled)
{
    LOG((LF_CORDB, LL_INFO100000, "Cordb - CanLaunchOrAttach - NOT IMPLEMENTED\n"));
    return S_OK;
}

Cordb::Cordb(DWORD PID) : CordbBaseMono(NULL)
{
    m_pCallback     = NULL;
    m_pSemReadWrite = new UTSemReadWrite();
    m_nPID = PID;

#ifndef TARGET_WINDOWS
    PAL_InitializeDLL();
#endif

#ifdef LOGGING
    InitializeLogging();
#endif
}

Cordb::~Cordb()
{
    this->GetCallback()->Release();
    m_pProcess->InternalRelease();
    delete m_pSemReadWrite;
#ifdef LOGGING
    ShutdownLogging();
#endif
}

HRESULT
Cordb::QueryInterface(REFIID id, _COM_Outptr_ void __RPC_FAR* __RPC_FAR* pInterface)
{
    if (id == IID_ICorDebug)
        *pInterface = static_cast<ICorDebug*>(this);
    else if (id == IID_IUnknown)
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebug*>(this));
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }

    AddRef();
    return S_OK;
}

HRESULT Cordb::CreateProcessEx(ICorDebugRemoteTarget*     pRemoteTarget,
                               LPCWSTR                    lpApplicationName,
                               _In_ LPWSTR                lpCommandLine,
                               LPSECURITY_ATTRIBUTES      lpProcessAttributes,
                               LPSECURITY_ATTRIBUTES      lpThreadAttributes,
                               BOOL                       bInheritHandles,
                               DWORD                      dwCreationFlags,
                               PVOID                      lpEnvironment,
                               LPCWSTR                    lpCurrentDirectory,
                               LPSTARTUPINFOW             lpStartupInfo,
                               LPPROCESS_INFORMATION      lpProcessInformation,
                               CorDebugCreateProcessFlags debuggingFlags,
                               ICorDebugProcess**         ppProcess)
{
    LOG((LF_CORDB, LL_INFO100000, "Cordb - CreateProcessEx - NOT IMPLEMENTED\n"));
    return S_OK;
}

HRESULT Cordb::DebugActiveProcessEx(ICorDebugRemoteTarget* pRemoteTarget,
                                    DWORD                  dwProcessId,
                                    BOOL                   fWin32Attach,
                                    ICorDebugProcess**     ppProcess)
{
    LOG((LF_CORDB, LL_INFO100000, "Cordb - DebugActiveProcessEx - NOT IMPLEMENTED\n"));
    return S_OK;
}

ReceivedReplyPacket::ReceivedReplyPacket(int error, int error_2, int id, MdbgProtBuffer* buf)
{
    this->error   = error;
    this->error_2 = error_2;
    this->id      = id;
    this->buf     = buf;
}

ReceivedReplyPacket::~ReceivedReplyPacket()
{
    if (buf)
    {
        m_dbgprot_buffer_free(buf);
        delete buf;
    }
}

Connection::Connection(CordbProcess* proc, Cordb* cordb)
{
    m_pProcess                 = proc;
    m_pCordb                   = cordb;
    m_pReceiveReplies           = new ArrayList();
    m_pReceivedPacketsToProcess = new ArrayList();
}

Connection::~Connection()
{
    DWORD i = 0;
    while (i < m_pReceiveReplies->GetCount())
    {
        ReceivedReplyPacket* rrp = (ReceivedReplyPacket*)m_pReceiveReplies->Get(i);
        if (rrp)
        {
            delete rrp;
        }
        i++;
    }
    i = 0;
    while (i < m_pReceivedPacketsToProcess->GetCount())
    {
        MdbgProtBuffer* buf = (MdbgProtBuffer*)m_pReceivedPacketsToProcess->Get(i);
        if (buf)
        {
            m_dbgprot_buffer_free(buf);
            delete buf;
        }
        i++;
    }
    delete m_socket;
    delete m_pReceiveReplies;
    delete m_pReceivedPacketsToProcess;
}

void Connection::Receive()
{
    while (true)
    {
        MdbgProtBuffer recvbuf_header;
        m_dbgprot_buffer_init(&recvbuf_header, HEADER_LENGTH);

        int iResult = m_socket->Receive((char*)recvbuf_header.buf, HEADER_LENGTH);

        if (iResult == -1)
        {
            m_dbgprot_buffer_free(&recvbuf_header);
            m_pCordb->GetCallback()->ExitProcess(static_cast<ICorDebugProcess*>(GetProcess()));
            break;
        }
        while (iResult == 0)
        {
            LOG((LF_CORDB, LL_INFO100000, "transport_recv () sleep returned %d, expected %d.\n", iResult,
                 HEADER_LENGTH));
            iResult = m_socket->Receive((char*)recvbuf_header.buf, HEADER_LENGTH);
            Sleep(1000);
        }

        MdbgProtHeader  header;
        m_dbgprot_decode_command_header(&recvbuf_header, &header);
        m_dbgprot_buffer_free(&recvbuf_header);
        if (header.len < HEADER_LENGTH)
        {
            return;
        }

        MdbgProtBuffer* recvbuf = new MdbgProtBuffer();
        m_dbgprot_buffer_init(recvbuf, header.len - HEADER_LENGTH);
        if (header.len - HEADER_LENGTH != 0)
        {
            iResult       = m_socket->Receive((char*)recvbuf->p, header.len - HEADER_LENGTH);
            int totalRead = iResult;
            while (totalRead < header.len - HEADER_LENGTH)
            {
                iResult = m_socket->Receive((char*)recvbuf->p + totalRead, (header.len - HEADER_LENGTH) - totalRead);
                totalRead += iResult;
            }
        }

        dbg_lock();
        if (header.flags == REPLY_PACKET)
        {
            ReceivedReplyPacket* rp = new ReceivedReplyPacket(header.error, header.error_2, header.id, recvbuf);
            m_pReceiveReplies->Append(rp);
        }
        else
        {
            m_pReceivedPacketsToProcess->Append(recvbuf);
        }
        dbg_unlock();
    }
}

ReceivedReplyPacket* Connection::GetReplyWithError(int cmdId)
{
    ReceivedReplyPacket* rrp = NULL;
    while (rrp == NULL || rrp->Id() != cmdId)
    {
        dbg_lock();
        for (int i = m_pReceiveReplies->GetCount() - 1; i >= 0; i--)
        {
            rrp = (ReceivedReplyPacket*)m_pReceiveReplies->Get(i);
            if (rrp->Id() == cmdId)
                break;
        }
        dbg_unlock();
    }
    return rrp;
}

CordbAppDomain* Connection::GetCurrentAppDomain()
{
    return GetProcess()->GetCurrentAppDomain();
}

void Connection::ProcessPacketInternal(MdbgProtBuffer* recvbuf)
{
    int             spolicy            = m_dbgprot_decode_byte(recvbuf->p, &recvbuf->p, recvbuf->end);
    int             nevents            = m_dbgprot_decode_int(recvbuf->p, &recvbuf->p, recvbuf->end);
    CordbAppDomain* pCorDebugAppDomain = GetCurrentAppDomain();
    for (int i = 0; i < nevents; ++i)
    {

        int kind   = m_dbgprot_decode_byte(recvbuf->p, &recvbuf->p, recvbuf->end);
        int req_id = m_dbgprot_decode_int(recvbuf->p, &recvbuf->p, recvbuf->end);

        MdbgProtEventKind etype = (MdbgProtEventKind)kind;

        long thread_id = m_dbgprot_decode_id(recvbuf->p, &recvbuf->p, recvbuf->end);

        LOG((LF_CORDB, LL_INFO100000, "Received %d %d events %s, suspend=%d\n", i, nevents,
             m_dbgprot_event_to_string(etype), spolicy));

        switch (etype)
        {
            case MDBGPROT_EVENT_KIND_VM_START:
            {
                m_pCordb->GetCallback()->CreateProcess(static_cast<ICorDebugProcess*>(GetProcess()));
            }
            break;
            case MDBGPROT_EVENT_KIND_VM_DEATH:
            {
                m_pCordb->GetCallback()->ExitProcess(static_cast<ICorDebugProcess*>(GetProcess()));
            }
            break;
            case MDBGPROT_EVENT_KIND_THREAD_START:
            {
                CordbThread* thread = new CordbThread(this, GetProcess(), thread_id);
                m_pCordb->GetCallback()->CreateThread(pCorDebugAppDomain, thread);
            }
            break;
            case MDBGPROT_EVENT_KIND_APPDOMAIN_CREATE:
            {
            }
            break;
            case MDBGPROT_EVENT_KIND_ASSEMBLY_LOAD:
            {
                // all the callbacks call a resume, in this case that we are faking 2
                // callbacks without receive command, we should not send the continue
                int assembly_id = m_dbgprot_decode_id(recvbuf->p, &recvbuf->p, recvbuf->end);
                if (pCorDebugAppDomain == NULL)
                {
                    pCorDebugAppDomain = new CordbAppDomain(this, GetProcess());
                    GetProcess()->Stop(false);
                    m_pCordb->GetCallback()->CreateAppDomain(static_cast<ICorDebugProcess*>(GetProcess()),
                                                           pCorDebugAppDomain);
                }
                CordbAssembly* pAssembly = new CordbAssembly(this, GetProcess(), pCorDebugAppDomain, assembly_id);
                CordbModule*   pModule   = new CordbModule(this, GetProcess(), (CordbAssembly*)pAssembly, assembly_id);

                GetProcess()->Stop(false);
                m_pCordb->GetCallback()->LoadAssembly(pCorDebugAppDomain, pAssembly);

                m_pCordb->GetCallback()->LoadModule(pCorDebugAppDomain, pModule);
            }
            break;
            case MDBGPROT_EVENT_KIND_BREAKPOINT:
            {
                int          method_id = m_dbgprot_decode_id(recvbuf->p, &recvbuf->p, recvbuf->end);
                int64_t      offset    = m_dbgprot_decode_long(recvbuf->p, &recvbuf->p, recvbuf->end);
                CordbThread* thread    = GetProcess()->FindThread(thread_id);
                if (thread == NULL)
                {
                    thread = new CordbThread(this, GetProcess(), thread_id);
                    GetProcess()->Stop(false);
                    m_pCordb->GetCallback()->CreateThread(pCorDebugAppDomain, thread);
                }
                DWORD                    i          = 0;
                CordbFunctionBreakpoint* breakpoint = GetProcess()->GetBreakpoint(req_id);
                m_pCordb->GetCallback()->Breakpoint(pCorDebugAppDomain, thread,
                                                  static_cast<ICorDebugFunctionBreakpoint*>(breakpoint));
            }
            break;
            case MDBGPROT_EVENT_KIND_STEP:
            {
                int          method_id = m_dbgprot_decode_id(recvbuf->p, &recvbuf->p, recvbuf->end);
                int64_t      offset    = m_dbgprot_decode_long(recvbuf->p, &recvbuf->p, recvbuf->end);
                CordbThread* thread    = GetProcess()->FindThread(thread_id);
                if (thread == NULL)
                {
                    thread = new CordbThread(this, GetProcess(), thread_id);
                    GetProcess()->Stop(false);
                    m_pCordb->GetCallback()->CreateThread(pCorDebugAppDomain, thread);
                }
                CordbStepper* stepper = GetProcess()->GetStepper(req_id);
                stepper->Deactivate();
                m_pCordb->GetCallback()->StepComplete(pCorDebugAppDomain, thread, stepper, STEP_NORMAL);
            }
            break;
            default:
            {
                LOG((LF_CORDB, LL_INFO100000, "Not implemented - %s\n", m_dbgprot_event_to_string(etype)));
            }
        }
    }
    // m_dbgprot_buffer_free(&recvbuf);
}

int Connection::ProcessPacket(bool is_answer)
{
    if (!is_answer)
        ProcessPacketFromQueue();
    return 1;
}

void Connection::ProcessPacketFromQueue()
{
    DWORD i = 0;
    while (i < m_pReceivedPacketsToProcess->GetCount())
    {
        MdbgProtBuffer* req = (MdbgProtBuffer*)m_pReceivedPacketsToProcess->Get(i);
        if (req)
        {
            ProcessPacketInternal(req);
            dbg_lock();
            m_pReceivedPacketsToProcess->Set(i, NULL);
            dbg_unlock();
            m_dbgprot_buffer_free(req);
            delete req;
        }
        i++;
    }
    GetProcess()->CheckPendingEval();
}

void Connection::LoopSendReceive()
{
    DWORD thread_id;
    CreateThread(NULL, 0, (LPTHREAD_START_ROUTINE)receive_thread, this, 0, &thread_id);

    EnableEvent(MDBGPROT_EVENT_KIND_ASSEMBLY_LOAD);
    EnableEvent(MDBGPROT_EVENT_KIND_APPDOMAIN_CREATE);
    EnableEvent(MDBGPROT_EVENT_KIND_THREAD_START);
    EnableEvent(MDBGPROT_EVENT_KIND_THREAD_DEATH);
    EnableEvent(MDBGPROT_EVENT_KIND_APPDOMAIN_UNLOAD);
    EnableEvent(MDBGPROT_EVENT_KIND_USER_BREAK);
    EnableEvent(MDBGPROT_EVENT_KIND_USER_LOG);
    EnableEvent(MDBGPROT_EVENT_KIND_VM_DEATH);

    MdbgProtBuffer localbuf;
    m_dbgprot_buffer_init(&localbuf, 128);
    m_dbgprot_buffer_add_int(&localbuf, MAJOR_VERSION);
    m_dbgprot_buffer_add_int(&localbuf, MINOR_VERSION);
    m_dbgprot_buffer_add_byte(&localbuf, true);
    int cmdId = SendEvent(MDBGPROT_CMD_SET_VM, MDBGPROT_CMD_VM_SET_PROTOCOL_VERSION, &localbuf);
    m_dbgprot_buffer_free(&localbuf);

    m_dbgprot_buffer_init(&localbuf, 128);
    cmdId = SendEvent(MDBGPROT_CMD_SET_VM, MDBGPROT_CMD_VM_VERSION, &localbuf);
    m_dbgprot_buffer_free(&localbuf);

    ReceivedReplyPacket* received_reply_packet = GetReplyWithError(cmdId);
    MdbgProtBuffer*      pReply                = received_reply_packet->Buffer();

    char* vm_version    = m_dbgprot_decode_string(pReply->p, &pReply->p, pReply->end);
    int   major_version = m_dbgprot_decode_int(pReply->p, &pReply->p, pReply->end);
    int   minor_version = m_dbgprot_decode_int(pReply->p, &pReply->p, pReply->end);

    LOG((LF_CORDB, LL_INFO100000, "Protocol version %d.%d, server protocol version %d.%d.\n", MAJOR_VERSION,
         MINOR_VERSION, major_version, minor_version));
    free(vm_version);

    int iResult = 0;
    // Receive until the peer closes the connection
    do
    {
        iResult = ProcessPacket();
        Sleep(100);
    } while (iResult >= 0);
}

void Connection::EnableEvent(MdbgProtEventKind eventKind)
{
    MdbgProtBuffer sendbuf;
    int            buflen = 128;
    m_dbgprot_buffer_init(&sendbuf, buflen);
    m_dbgprot_buffer_add_byte(&sendbuf, eventKind);
    m_dbgprot_buffer_add_byte(&sendbuf, MDBGPROT_SUSPEND_POLICY_ALL);
    m_dbgprot_buffer_add_byte(&sendbuf, 0); // modifiers
    SendEvent(MDBGPROT_CMD_SET_EVENT_REQUEST, MDBGPROT_CMD_EVENT_REQUEST_SET, &sendbuf);
    m_dbgprot_buffer_free(&sendbuf);
}

void Connection::CloseConnection()
{
    m_socket->Close();
}

void Connection::StartConnection()
{
    LOG((LF_CORDB, LL_INFO100000, "Start Connection\n"));

    m_socket = new Socket();
    int port = 56000 + (m_pCordb->PID() % 1000);
    char* s_port = new char[10];
    sprintf_s(s_port, 10, "%d", port);
    LOG((LF_CORDB, LL_INFO100000, "Listening to %s:%s\n", DEBUG_ADDRESS, s_port));

    int ret = m_socket->OpenSocketAcceptConnection(DEBUG_ADDRESS, s_port);
    delete[] s_port;
    if (ret == -1)
        exit(1);

    LOG((LF_CORDB, LL_INFO100000, "Accepted connection from client.\n"));
}

void Connection::TransportHandshake()
{
    int buflen = 128;

    MdbgProtBuffer sendbuf;
    m_dbgprot_buffer_init(&sendbuf, buflen);

    MdbgProtBuffer recvbuf;
    m_dbgprot_buffer_init(&recvbuf, buflen);

    int iResult;
    iResult = m_socket->Receive((char*)recvbuf.buf, buflen);

    // Send an initial buffer
    m_dbgprot_buffer_add_data(&sendbuf, (uint8_t*)"DWP-Handshake", 13);
    SendPacket(sendbuf);
    m_dbgprot_buffer_free(&sendbuf);
    m_dbgprot_buffer_free(&recvbuf);
}

void Connection::SendPacket(MdbgProtBuffer& sendbuf)
{
    int iResult = m_socket->Send((const char*)sendbuf.buf, m_dbgprot_buffer_len(&sendbuf));
    if (iResult == -1)
    {
        return;
    }
}

int Connection::SendEvent(int cmd_set, int cmd, MdbgProtBuffer* sendbuf)
{
    MdbgProtBuffer outbuf;
    int ret = m_dbgprot_buffer_add_command_header(sendbuf, cmd_set, cmd, &outbuf);
    SendPacket(outbuf);
    m_dbgprot_buffer_free(&outbuf);
    return ret;
}

MONO_API HRESULT CoreCLRCreateCordbObject(int iDebuggerVersion, DWORD pid, HMODULE hmodTargetCLR, void** ppCordb)
{
    *ppCordb = new Cordb(pid);
    return S_OK;
}

MONO_API HRESULT CreateCordbObject(int iDebuggerVersion, void** ppCordb)
{
    *ppCordb = new Cordb(0);
    return S_OK;
}

CordbBaseMono::CordbBaseMono(Connection* conn)
{
    this->conn     = conn;
    m_cRef         = 0;
}

CordbBaseMono::~CordbBaseMono() {}

ULONG CordbBaseMono::InternalAddRef()
{
    return BaseAddRef();
}

ULONG CordbBaseMono::InternalRelease()
{
    return BaseRelease();
}

ULONG CordbBaseMono::BaseAddRef()
{
    return InterlockedIncrement((volatile LONG*)&m_cRef);
}

ULONG CordbBaseMono::BaseRelease()
{
    ULONG cRef = InterlockedDecrement((volatile LONG*)&m_cRef);
    if (cRef == 0)
    {
        delete this;
    }
    return cRef;
}

void CordbBaseMono::SetConnection(Connection* conn)
{
    this->conn = conn;
}
