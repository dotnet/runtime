// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB.H
//

#ifndef __MONO_DEBUGGER_CORDB_H__
#define __MONO_DEBUGGER_CORDB_H__

#include "cor.h"
#include "cordebug.h"
#include "corhdr.h"
#include "xcordebug.h"

#include <mono/metadata/blob.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/tokentype.h>
#include <mono/mini/debugger-protocol.h>
#include <mono/utils/mono-coop-mutex.h>
#include <mono/utils/mono-publib.h>

#include <stdio.h>

#ifdef HOST_WIN32
#include <windows.h>
#include <ws2tcpip.h>
#endif

#define return_if_nok(error)                                                   \
  do {                                                                         \
    if (!is_ok((error)))                                                       \
      return S_FALSE;                                                          \
  } while (0)

#define dbg_lock() mono_os_mutex_lock(&debug_mutex.m);
#define dbg_unlock() mono_os_mutex_unlock(&debug_mutex.m);
static MonoCoopMutex debug_mutex;

#ifdef _DEBUG
#define LOGGING
#include <log.h>
#endif

class Cordb;
class CordbProcess;
class CordbAppDomain;
class CordbAssembly;
class CordbCode;
class CordbThread;
class CordbFunction;
class CordbStepper;
class RegMeta;
class CordbRegisteSet;
class CordbClass;

typedef struct ReceivedReplyPacket {
  int error;
  int error_2;
  MdbgProtBuffer *buf;
} ReceivedReplyPacket;

int convert_mono_type_2_icordbg_size(int type);

class Cordb : public ICorDebug, public ICorDebugRemote {
public:
  GPtrArray *breakpoints;
  GPtrArray *threads;
  GPtrArray *functions;
  GHashTable *modules;

  ICorDebugManagedCallback *pCallback;
  Cordb();

  CordbFunction *findFunction(int id);
  CordbFunction *findFunctionByToken(int token);
  HRESULT Initialize(void);

  HRESULT Terminate(void);

  HRESULT SetManagedHandler(
      /* [in] */ ICorDebugManagedCallback *pCallback);

  HRESULT SetUnmanagedHandler(
      /* [in] */ ICorDebugUnmanagedCallback *pCallback);

  HRESULT CreateProcess(
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
      /* [out] */ ICorDebugProcess **ppProcess);

  HRESULT DebugActiveProcess(
      /* [in] */ DWORD id,
      /* [in] */ BOOL win32Attach,
      /* [out] */ ICorDebugProcess **ppProcess);
  HRESULT EnumerateProcesses(
      /* [out] */ ICorDebugProcessEnum **ppProcess);

  HRESULT GetProcess(
      /* [in] */ DWORD dwProcessId,
      /* [out] */ ICorDebugProcess **ppProcess);

  HRESULT CanLaunchOrAttach(
      /* [in] */ DWORD dwProcessId,
      /* [in] */ BOOL win32DebuggingEnabled);
  HRESULT QueryInterface(
      /* [in] */ REFIID riid,
      /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject);

  ULONG AddRef(void);
  ULONG Release(void);
  HRESULT CreateProcessEx(
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
      /* [out] */ ICorDebugProcess **ppProcess);

  HRESULT DebugActiveProcessEx(
      /* [in] */ ICorDebugRemoteTarget *pRemoteTarget,
      /* [in] */ DWORD dwProcessId,
      /* [in] */ BOOL fWin32Attach,
      /* [out] */ ICorDebugProcess **ppProcess);
};

class Connection {
  SOCKET connect_socket;
  bool is_answer_pending;

public:
  CordbProcess *ppProcess;
  Cordb *ppCordb;
  CordbAppDomain *pCorDebugAppDomain;
  GHashTable *received_replies;
  GPtrArray *pending_eval;
  GPtrArray *received_packets_to_process;
  Connection(CordbProcess *proc, Cordb *cordb);
  void loop_send_receive();
  void process_packet_internal(MdbgProtBuffer *recvbuf);
  void process_packet_from_queue();
  void enable_event(MdbgProtEventKind eventKind);
  void close_connection();
  void start_connection();
  void transport_handshake();
  void receive();
  void send_packet(MdbgProtBuffer &sendbuf);
  void receive_packet(MdbgProtBuffer &b, int len);
  void receive_header(MdbgProtHeader *header);
  int send_event(int cmd_set, int cmd, MdbgProtBuffer *sendbuf);
  int process_packet(bool is_answer = false);
  MdbgProtBuffer *get_answer(int cmdId);
  ReceivedReplyPacket *get_answer_with_error(int cmdId);
  CordbThread *findThread(GPtrArray *threads, long thread_id);
};

class CordbBaseMono {
protected:
  Connection *conn;

public:
  CordbBaseMono(Connection *conn);
  void SetConnection(Connection *conn);
};

#define CHECK_ERROR_RETURN_FALSE(localbuf)                                     \
  do {                                                                         \
    if (localbuf->error > 0 || localbuf->error_2 > 0) {                        \
      LOG((LF_CORDB, LL_INFO100000, "ERROR RECEIVED\n"));                      \
      return S_FALSE;                                                          \
    }                                                                          \
  } while (0)

#endif
