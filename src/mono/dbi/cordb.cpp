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

int convert_mono_type_2_icordbg_size(int type) {
  switch (type) {
  case ELEMENT_TYPE_VOID:
    return 0;
  case ELEMENT_TYPE_BOOLEAN:
  case ELEMENT_TYPE_I1:
  case ELEMENT_TYPE_U1:
    return 1;
    break;
  case ELEMENT_TYPE_CHAR:
  case ELEMENT_TYPE_I2:
  case ELEMENT_TYPE_U2:
    return 2;
  case ELEMENT_TYPE_I4:
  case ELEMENT_TYPE_U4:
  case ELEMENT_TYPE_R4:
    return 4;
  case ELEMENT_TYPE_I8:
  case ELEMENT_TYPE_U8:
  case ELEMENT_TYPE_R8:
    return 8;
  }
  return 0;
}

MONO_API HRESULT CoreCLRCreateCordbObjectEx(int iDebuggerVersion, DWORD pid,
                                            LPCWSTR lpApplicationGroupId,
                                            HMODULE hmodTargetCLR,
                                            void **ppCordb) {
  LOG((LF_CORDB, LL_INFO100000, "CoreCLRCreateCordbObjectEx\n"));
  *ppCordb = new Cordb();
  return S_OK;
}

static void receive_thread(Connection *c) { c->receive(); }

static void debugger_thread(void *ppProcess) {
  Connection *connection = new Connection((CordbProcess *)ppProcess,
                                          ((CordbProcess *)ppProcess)->cordb);
  ((CordbProcess *)ppProcess)->SetConnection(connection);
  connection->start_connection();
  connection->transport_handshake();
  connection->loop_send_receive();
  connection->close_connection();
}

HRESULT Cordb::Initialize(void) {
  LOG((LF_CORDB, LL_INFO100000, "Cordb - Initialize - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT Cordb::Terminate(void) {
  LOG((LF_CORDB, LL_INFO100000, "Cordb - Terminate - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT Cordb::SetManagedHandler(
    /* [in] */ ICorDebugManagedCallback *pCallback) {
  LOG((LF_CORDB, LL_INFO1000000, "Cordb - SetManagedHandler - IMPLEMENTED\n"));
  this->pCallback = pCallback;
  this->pCallback->AddRef();

  return S_OK;
}

HRESULT Cordb::SetUnmanagedHandler(
    /* [in] */ ICorDebugUnmanagedCallback *pCallback) {
  LOG((LF_CORDB, LL_INFO100000,
       "Cordb - SetUnmanagedHandler - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT Cordb::CreateProcess(
    /* [in] */ LPCWSTR lpApplicationName,
    /* [in] */ LPWSTR lpCommandLine,
    /* [in] */ LPSECURITY_ATTRIBUTES lpProcessAttributes,
    /* [in] */ LPSECURITY_ATTRIBUTES lpThreadAttributes,
    /* [in] */ BOOL bInheritHandles,
    /* [in] */ DWORD dwCreationFlags,
    /* [in] */ PVOID lpEnvironment,
    /* [in] */ LPCWSTR lpCurrentDirectory,
    /* [in] */ LPSTARTUPINFOW lpStartupInfo,
    /* [in] */ LPPROCESS_INFORMATION lpProcessInformation,
    /* [in] */ CorDebugCreateProcessFlags debuggingFlags,
    /* [out] */ ICorDebugProcess **ppProcess) {
  LOG((LF_CORDB, LL_INFO100000, "Cordb - CreateProcess - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT Cordb::DebugActiveProcess(
    /* [in] */ DWORD id,
    /* [in] */ BOOL win32Attach,
    /* [out] */ ICorDebugProcess **ppProcess) {
  LOG((LF_CORDB, LL_INFO1000000, "Cordb - DebugActiveProcess - IMPLEMENTED\n"));
  *ppProcess = new CordbProcess();
  ((CordbProcess *)*ppProcess)->cordb = this;

  DWORD thread_id;
  CreateThread(NULL, 0, (LPTHREAD_START_ROUTINE)debugger_thread,
               ((CordbProcess *)*ppProcess), 0, &thread_id);
  return S_OK;
}

HRESULT Cordb::EnumerateProcesses(
    /* [out] */ ICorDebugProcessEnum **ppProcess) {
  LOG((LF_CORDB, LL_INFO100000,
       "Cordb - EnumerateProcesses - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT Cordb::GetProcess(
    /* [in] */ DWORD dwProcessId,
    /* [out] */ ICorDebugProcess **ppProcess) {
  LOG((LF_CORDB, LL_INFO100000, "Cordb - GetProcess - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT Cordb::CanLaunchOrAttach(
    /* [in] */ DWORD dwProcessId,
    /* [in] */ BOOL win32DebuggingEnabled) {
  LOG((LF_CORDB, LL_INFO100000,
       "Cordb - CanLaunchOrAttach - NOT IMPLEMENTED\n"));
  return S_OK;
}

Cordb::Cordb() {
  pCallback = NULL;
  breakpoints = new ArrayList();
  threads = new ArrayList();
  functions = new ArrayList();
  modules = new ArrayList();
#ifdef LOGGING
  InitializeLogging();
#endif
}

CordbFunction *Cordb::findFunction(int id) {
  int i = 0;
  while (i < functions->GetCount()) {
    CordbFunction *function = (CordbFunction *)functions->Get(i);
    if (function->id == id) {
      return function;
    }
    i++;
  }
  return NULL;
}

CordbFunction *Cordb::findFunctionByToken(int token) {
  int i = 0;
  while (i < functions->GetCount()) {
    CordbFunction *function = (CordbFunction*)functions->Get(i);
    if (function->token == token) {
      return function;
    }
    i++;
  }
  return NULL;
}

HRESULT Cordb::QueryInterface(
    /* [in] */ REFIID riid,
    /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject) {
  LOG((LF_CORDB, LL_INFO100000, "Cordb - QueryInterface - NOT IMPLEMENTED\n"));
  return S_OK;
}

ULONG Cordb::AddRef(void) { return S_OK; }

ULONG Cordb::Release(void) { return S_OK; }

HRESULT Cordb::CreateProcessEx(
    /* [in] */ ICorDebugRemoteTarget *pRemoteTarget,
    /* [in] */ LPCWSTR lpApplicationName,
    /* [annotation][in] */
    _In_ LPWSTR lpCommandLine,
    /* [in] */ LPSECURITY_ATTRIBUTES lpProcessAttributes,
    /* [in] */ LPSECURITY_ATTRIBUTES lpThreadAttributes,
    /* [in] */ BOOL bInheritHandles,
    /* [in] */ DWORD dwCreationFlags,
    /* [in] */ PVOID lpEnvironment,
    /* [in] */ LPCWSTR lpCurrentDirectory,
    /* [in] */ LPSTARTUPINFOW lpStartupInfo,
    /* [in] */ LPPROCESS_INFORMATION lpProcessInformation,
    /* [in] */ CorDebugCreateProcessFlags debuggingFlags,
    /* [out] */ ICorDebugProcess **ppProcess) {
  LOG((LF_CORDB, LL_INFO100000, "Cordb - CreateProcessEx - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT Cordb::DebugActiveProcessEx(
    /* [in] */ ICorDebugRemoteTarget *pRemoteTarget,
    /* [in] */ DWORD dwProcessId,
    /* [in] */ BOOL fWin32Attach,
    /* [out] */ ICorDebugProcess **ppProcess) {
  LOG((LF_CORDB, LL_INFO100000,
       "Cordb - DebugActiveProcessEx - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT Cordb::GetModule(int module_id, ICorDebugModule** pModule)
{
    for (int i = 0; i < modules->GetCount(); i++) {
        CordbModule* module = (CordbModule*)modules->Get(i);
        if (module->id == module_id) {
            *pModule = module;
            return S_OK;
        }
    }
    return S_FALSE;
}

Connection::Connection(CordbProcess *proc, Cordb *cordb) {
  ppProcess = proc;
  ppCordb = cordb;
  pCorDebugAppDomain = NULL;
  received_replies = new ArrayList();
  received_packets_to_process = new ArrayList();
  is_answer_pending = false;
  pending_eval = new ArrayList();
}

CordbThread *Connection::findThread(ArrayList *threads, long thread_id) {
  int i = 0;
  while (i < threads->GetCount()) {
    CordbThread *thread = (CordbThread *)threads->Get(i);
    if (thread->thread_id == thread_id) {
      return thread;
    }
    i++;
  }
  return NULL;
}

void Connection::receive() {
  while (true) {
    MdbgProtBuffer recvbuf_header;
    m_dbgprot_buffer_init(&recvbuf_header, HEADER_LENGTH);

    int iResult =
        connect_socket->Receive((char *)recvbuf_header.buf, HEADER_LENGTH);

    if (iResult == -1) {
      ppCordb->pCallback->ExitProcess(
          static_cast<ICorDebugProcess *>(ppProcess));
      break;
    }
    while (iResult == 0) {
      LOG((LF_CORDB, LL_INFO100000,
           "transport_recv () sleep returned %d, expected %d.\n", iResult,
           HEADER_LENGTH));
      iResult =
          connect_socket->Receive((char *)recvbuf_header.buf, HEADER_LENGTH);
      Sleep(1000);
    }

    MdbgProtHeader header;
    MdbgProtBuffer *recvbuf = new MdbgProtBuffer();
    m_dbgprot_decode_command_header(&recvbuf_header, &header);

    m_dbgprot_buffer_init(recvbuf, header.len - HEADER_LENGTH);

    if (header.len < HEADER_LENGTH) {
      return;
    }

    if (header.len - HEADER_LENGTH != 0) {
      iResult = connect_socket->Receive((char *)recvbuf->buf,
                     header.len - HEADER_LENGTH);
      int totalRead = iResult;
      while (totalRead < header.len - HEADER_LENGTH) {
        iResult = connect_socket->Receive((char *)recvbuf->buf + totalRead,
                       (header.len - HEADER_LENGTH) - totalRead);
        totalRead += iResult;
      }
    }

    dbg_lock();
    if (header.flags == REPLY_PACKET) {
      ReceivedReplyPacket *rp =
          (ReceivedReplyPacket *)malloc(sizeof(ReceivedReplyPacket));
      rp->error = header.error;
      rp->error_2 = header.error_2;
      rp->id = header.id;
      rp->buf = recvbuf;
      received_replies->Append(rp);
    } else {
      received_packets_to_process->Append(recvbuf);
    }
    dbg_unlock();
  }
}

MdbgProtBuffer *Connection::get_answer(int cmdId) {
  ReceivedReplyPacket * rrp = NULL;
  while (rrp == NULL || rrp->id != cmdId) {
    dbg_lock();
    for (int i = received_replies->GetCount() - 1; i >= 0; i--) {
        rrp = (ReceivedReplyPacket*)received_replies->Get(i);
        if (rrp->id == cmdId)
            break;
    }
    dbg_unlock();
  }
  return rrp->buf;
}

ReceivedReplyPacket *Connection::get_answer_with_error(int cmdId) {
  ReceivedReplyPacket * rrp = NULL;
  while (rrp == NULL || rrp->id != cmdId) {
    dbg_lock();
    for (int i = received_replies->GetCount() - 1; i >= 0; i--) {
        rrp = (ReceivedReplyPacket*)received_replies->Get(i);
        if (rrp->id == cmdId)
            break;
    }
    dbg_unlock();
  }
  return rrp;
}

void Connection::process_packet_internal(MdbgProtBuffer *recvbuf) {
  int spolicy =
      m_dbgprot_decode_byte(recvbuf->buf, &recvbuf->buf, recvbuf->end);
  int nevents = m_dbgprot_decode_int(recvbuf->buf, &recvbuf->buf, recvbuf->end);

  for (int i = 0; i < nevents; ++i) {

    int kind = m_dbgprot_decode_byte(recvbuf->buf, &recvbuf->buf, recvbuf->end);
    int req_id =
        m_dbgprot_decode_int(recvbuf->buf, &recvbuf->buf, recvbuf->end);

    MdbgProtEventKind etype = (MdbgProtEventKind)kind;

    long thread_id =
        m_dbgprot_decode_id(recvbuf->buf, &recvbuf->buf, recvbuf->end);

    LOG((LF_CORDB, LL_INFO100000, "Received %d %d events %s, suspend=%d\n", i,
         nevents, m_dbgprot_event_to_string(etype), spolicy));

    switch (etype) {
    case MDBGPROT_EVENT_KIND_VM_START: {
      ppProcess->suspended = true;
      ppCordb->pCallback->CreateProcess(
          static_cast<ICorDebugProcess *>(ppProcess));
    } break;
    case MDBGPROT_EVENT_KIND_VM_DEATH: {
      ppCordb->pCallback->ExitProcess(
          static_cast<ICorDebugProcess *>(ppProcess));
    } break;
    case MDBGPROT_EVENT_KIND_THREAD_START: {
      CordbThread *thread = new CordbThread(this, ppProcess, thread_id);
      ppCordb->threads->Append(thread);
      ppCordb->pCallback->CreateThread(pCorDebugAppDomain, thread);
    } break;
    case MDBGPROT_EVENT_KIND_APPDOMAIN_CREATE: {

    } break;
    case MDBGPROT_EVENT_KIND_ASSEMBLY_LOAD: {
      // all the callbacks call a resume, in this case that we are faking 2
      // callbacks without receive command, we should not send the continue
      int assembly_id =
          m_dbgprot_decode_id(recvbuf->buf, &recvbuf->buf, recvbuf->end);
      if (pCorDebugAppDomain == NULL) {
        pCorDebugAppDomain = new CordbAppDomain(
            this, static_cast<ICorDebugProcess *>(ppProcess));
        ppProcess->Stop(false);
        ppCordb->pCallback->CreateAppDomain(
            static_cast<ICorDebugProcess *>(ppProcess), pCorDebugAppDomain);
      }
      ICorDebugAssembly *pAssembly =
          new CordbAssembly(this, ppProcess, pCorDebugAppDomain, assembly_id);
      ppProcess->Stop(false);
      ppCordb->pCallback->LoadAssembly(pCorDebugAppDomain, pAssembly);

      ppProcess->suspended = true;
      ICorDebugModule *pModule = new CordbModule(
          this, ppProcess, (CordbAssembly *)pAssembly, assembly_id);
      ppCordb->modules->Append(pModule);
      ppCordb->pCallback->LoadModule(pCorDebugAppDomain, pModule);
    } break;
    case MDBGPROT_EVENT_KIND_BREAKPOINT: {
      int method_id =
          m_dbgprot_decode_id(recvbuf->buf, &recvbuf->buf, recvbuf->end);
      long offset =
          m_dbgprot_decode_long(recvbuf->buf, &recvbuf->buf, recvbuf->end);
      CordbThread *thread = findThread(ppCordb->threads, thread_id);
      if (thread == NULL) {
        thread = new CordbThread(this, ppProcess, thread_id);
        ppCordb->threads->Append(thread);
        ppProcess->Stop(false);
        ppCordb->pCallback->CreateThread(pCorDebugAppDomain, thread);
      }
      int i = 0;
      CordbFunctionBreakpoint *breakpoint;
      while (i < ppCordb->breakpoints->GetCount()) {
          breakpoint = (CordbFunctionBreakpoint*)ppCordb->breakpoints->Get(i);
        if (breakpoint->offset == offset &&
            breakpoint->code->func->id == method_id) {
          ppCordb->pCallback->Breakpoint(
              pCorDebugAppDomain, thread,
              static_cast<ICorDebugFunctionBreakpoint *>(breakpoint));
          break;
        }
        i++;
      }
    } break;
    case MDBGPROT_EVENT_KIND_STEP: {
      int method_id =
          m_dbgprot_decode_id(recvbuf->buf, &recvbuf->buf, recvbuf->end);
      long offset =
          m_dbgprot_decode_long(recvbuf->buf, &recvbuf->buf, recvbuf->end);
      CordbThread *thread = findThread(ppCordb->threads, thread_id);
      if (thread == NULL) {
        thread = new CordbThread(this, ppProcess, thread_id);
        ppCordb->threads->Append(thread);
        ppProcess->Stop(false);
        ppCordb->pCallback->CreateThread(pCorDebugAppDomain, thread);
      }
      ppCordb->pCallback->StepComplete(pCorDebugAppDomain, thread,
                                       thread->stepper, STEP_NORMAL);
    } break;
    default: {
        LOG((LF_CORDB, LL_INFO100000, "Not implemented - %s\n", m_dbgprot_event_to_string(etype)));
    }
    }
  }
  // m_dbgprot_buffer_free(&recvbuf);
}

int Connection::process_packet(bool is_answer) {
  if (!is_answer)
    process_packet_from_queue();
  return 1;
}

void Connection::process_packet_from_queue() {
  int i = 0;
  while (i < received_packets_to_process->GetCount()) {
    MdbgProtBuffer *req =
        (MdbgProtBuffer *)received_packets_to_process->Get(i);
    if (req) {
        process_packet_internal(req);
        dbg_lock();
        received_packets_to_process->Set(i, NULL);
        dbg_unlock();
        delete req;
    }
    i++;
  }
  while (i < pending_eval->GetCount()) {
    CordbEval *eval = (CordbEval *)pending_eval->Get(i);
      if (eval) {
          ReceivedReplyPacket* recvbuf = get_answer_with_error(eval->cmdId);
          if (recvbuf)
              eval->EvalComplete(recvbuf->buf);
          dbg_lock();
          pending_eval->Set(i, NULL);
          dbg_unlock();
      }
    i--;
  }
}

void Connection::loop_send_receive() {
  DWORD thread_id;
  CreateThread(NULL, 0, (LPTHREAD_START_ROUTINE)receive_thread, this, 0,
               &thread_id);

  enable_event(MDBGPROT_EVENT_KIND_ASSEMBLY_LOAD);
  enable_event(MDBGPROT_EVENT_KIND_APPDOMAIN_CREATE);
  enable_event(MDBGPROT_EVENT_KIND_THREAD_START);
  enable_event(MDBGPROT_EVENT_KIND_THREAD_DEATH);
  enable_event(MDBGPROT_EVENT_KIND_APPDOMAIN_UNLOAD);
  enable_event(MDBGPROT_EVENT_KIND_USER_BREAK);
  enable_event(MDBGPROT_EVENT_KIND_USER_LOG);
  enable_event(MDBGPROT_EVENT_KIND_VM_DEATH);

  MdbgProtBuffer localbuf;
  m_dbgprot_buffer_init(&localbuf, 128);
  m_dbgprot_buffer_add_int(&localbuf, MAJOR_VERSION);
  m_dbgprot_buffer_add_int(&localbuf, MINOR_VERSION);
  int cmdId = send_event(MDBGPROT_CMD_SET_VM,
                         MDBGPROT_CMD_VM_SET_PROTOCOL_VERSION, &localbuf);
  m_dbgprot_buffer_free(&localbuf);

  m_dbgprot_buffer_init(&localbuf, 128);
  cmdId = send_event(MDBGPROT_CMD_SET_VM, MDBGPROT_CMD_VM_VERSION, &localbuf);
  m_dbgprot_buffer_free(&localbuf);

  MdbgProtBuffer *bAnswer = get_answer(cmdId);
  char *vm_version =
      m_dbgprot_decode_string(bAnswer->buf, &bAnswer->buf, bAnswer->end);
  int major_version =
      m_dbgprot_decode_int(bAnswer->buf, &bAnswer->buf, bAnswer->end);
  int minor_version =
      m_dbgprot_decode_int(bAnswer->buf, &bAnswer->buf, bAnswer->end);

  LOG((LF_CORDB, LL_INFO100000,
       "Protocol version %d.%d, server protocol version %d.%d.\n",
       MAJOR_VERSION, MINOR_VERSION, major_version, minor_version));

  int iResult = 0;
  // Receive until the peer closes the connection
  do {
    iResult = process_packet();
    Sleep(100);
  } while (iResult >= 0);
}

void Connection::enable_event(MdbgProtEventKind eventKind) {
  MdbgProtBuffer sendbuf;
  int buflen = 128;
  m_dbgprot_buffer_init(&sendbuf, buflen);
  m_dbgprot_buffer_add_byte(&sendbuf, eventKind);
  m_dbgprot_buffer_add_byte(&sendbuf, MDBGPROT_SUSPEND_POLICY_ALL);
  m_dbgprot_buffer_add_byte(&sendbuf, 0); // modifiers
  send_event(MDBGPROT_CMD_SET_EVENT_REQUEST, MDBGPROT_CMD_EVENT_REQUEST_SET,
             &sendbuf);
  m_dbgprot_buffer_free(&sendbuf);
}

void Connection::close_connection() {
    connect_socket->Close();
}

void Connection::start_connection() {
  LOG((LF_CORDB, LL_INFO100000, "Start Connection\n"));

  connect_socket = new Socket();

  LOG((LF_CORDB, LL_INFO100000, "Listening to 127.0.0.1:1003\n"));

  int ret = connect_socket->OpenSocketAcceptConnection("127.0.0.1", "1003");
  if (ret == -1)
    exit(1);

  LOG((LF_CORDB, LL_INFO100000, "Accepted connection from client.\n"));
}

void Connection::transport_handshake() {
  int buflen = 128;

  MdbgProtBuffer sendbuf;
  m_dbgprot_buffer_init(&sendbuf, buflen);

  MdbgProtBuffer recvbuf;
  m_dbgprot_buffer_init(&recvbuf, buflen);

  int iResult;
  iResult = connect_socket->Receive((char *)recvbuf.buf, buflen);

  // Send an initial buffer
  m_dbgprot_buffer_add_data(&sendbuf, (uint8_t *)"DWP-Handshake", 13);
  send_packet(sendbuf);
}

void Connection::send_packet(MdbgProtBuffer &sendbuf) {
  int iResult = connect_socket->Send((const char *)sendbuf.buf, m_dbgprot_buffer_len(&sendbuf));
  if (iResult == -1) {
    return;
  }
}

void Connection::receive_packet(MdbgProtBuffer &recvbuf, int len) {
  m_dbgprot_buffer_init(&recvbuf, len);
  int iResult;
  iResult = connect_socket->Receive((char *)recvbuf.buf, len);
}

void Connection::receive_header(MdbgProtHeader *header) {
  MdbgProtBuffer recvbuf;
  m_dbgprot_buffer_init(&recvbuf, 11);
  int iResult;
  iResult = connect_socket->Receive((char *)recvbuf.buf, HEADER_LENGTH);
  m_dbgprot_decode_command_header(&recvbuf, header);
}

int Connection::send_event(int cmd_set, int cmd, MdbgProtBuffer *sendbuf) {
  MdbgProtBuffer outbuf;
  int ret = m_dbgprot_buffer_add_command_header(sendbuf, cmd_set, cmd, &outbuf);
  send_packet(outbuf);
  return ret;
}

MONO_API HRESULT CoreCLRCreateCordbObject(int iDebuggerVersion, DWORD pid,
                                          HMODULE hmodTargetCLR,
                                          void **ppCordb) {
  LOG((LF_CORDB, LL_INFO100000, "CoreCLRCreateCordbObject\n"));
  *ppCordb = new Cordb();
  return S_OK;
}

MONO_API HRESULT CreateCordbObject(int iDebuggerVersion, void **ppCordb) {
  LOG((LF_CORDB, LL_INFO100000, "CreateCordbObject\n"));
  *ppCordb = new Cordb();
  return S_OK;
}

HRESULT CordbModule::GetAssembly(
    /* [out] */ ICorDebugAssembly **ppAssembly) {
  LOG((LF_CORDB, LL_INFO1000000, "CordbModule - GetAssembly - IMPLEMENTED\n"));
  *ppAssembly = static_cast<ICorDebugAssembly *>(pAssembly);
  return S_OK;
}

HRESULT CordbAssembly::GetProcess(
    /* [out] */ ICorDebugProcess **ppProcess) {
  LOG((LF_CORDB, LL_INFO1000000,
       "CorDebugAssembly - GetProcess - IMPLEMENTED\n"));
  *ppProcess = static_cast<ICorDebugProcess *>(pProcess);
  return S_OK;
}

CordbBaseMono::CordbBaseMono(Connection *conn) { this->conn = conn; }

void CordbBaseMono::SetConnection(Connection *conn) { this->conn = conn; }
