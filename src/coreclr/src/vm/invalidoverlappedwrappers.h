// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// InvalidOverlappedWrappers.h
//

//


CREATE_WRAPPER_FUNCTION(HttpApi, ULONG, WINAPI, HttpReceiveHttpRequest, 
    ( HANDLE ReqQueueHandle, ULONGLONG RequestId, ULONG Flags, LPVOID pRequestBuffer, ULONG RequestBufferLength, PULONG pBytesReceived, LPOVERLAPPED overlapped),
    ( ReqQueueHandle, RequestId, Flags, pRequestBuffer, RequestBufferLength, pBytesReceived, overlapped))

CREATE_WRAPPER_FUNCTION(IpHlpApi, DWORD, WINAPI, NotifyAddrChange,
    (PHANDLE Handle,LPOVERLAPPED overlapped),
    (Handle, overlapped))

CREATE_WRAPPER_FUNCTION(IpHlpApi, DWORD, WINAPI, NotifyRouteChange,
    (PHANDLE Handle,LPOVERLAPPED overlapped),
    (Handle, overlapped))

CREATE_WRAPPER_FUNCTION(kernel32, BOOL, WINAPI, ReadFile,
    (HANDLE hFile, LPVOID lpBuffer, DWORD nNumberOfBytesToRead, LPDWORD lpNumberOfBytesRead, LPOVERLAPPED overlapped),
    (hFile, lpBuffer, nNumberOfBytesToRead, lpNumberOfBytesRead, overlapped))

CREATE_WRAPPER_FUNCTION(kernel32, BOOL, WINAPI, ReadFileEx,
    (HANDLE hFile, LPVOID lpBuffer, DWORD nNumberOfBytesToRead, LPOVERLAPPED overlapped, LPOVERLAPPED_COMPLETION_ROUTINE lpCompletionRoutine),
    (hFile, lpBuffer, nNumberOfBytesToRead, overlapped, lpCompletionRoutine))

CREATE_WRAPPER_FUNCTION(kernel32, BOOL, WINAPI, WriteFile,
    (HANDLE hFile, LPCVOID lpBuffer, DWORD nNumberOfBytesToWrite, LPDWORD lpNumberOfBytesWritten, LPOVERLAPPED overlapped),
    (hFile, lpBuffer, nNumberOfBytesToWrite, lpNumberOfBytesWritten, overlapped))

CREATE_WRAPPER_FUNCTION(kernel32, BOOL, WINAPI, WriteFileEx,
    (HANDLE hFile, LPCVOID lpBuffer, DWORD nNumberOfBytesToWrite, LPOVERLAPPED overlapped, LPOVERLAPPED_COMPLETION_ROUTINE lpCompletionRoutine),
    (hFile, lpBuffer, nNumberOfBytesToWrite, overlapped, lpCompletionRoutine))

CREATE_WRAPPER_FUNCTION(kernel32, BOOL, WINAPI, ReadDirectoryChangesW,
    (HANDLE hDirectory, LPVOID lpBuffer, DWORD nBufferLength, BOOL bWatchSubtree, DWORD dwNotifyFilter, LPDWORD lpBytesReturned, LPOVERLAPPED overlapped, LPOVERLAPPED_COMPLETION_ROUTINE lpCompletionRoutine),
    (hDirectory, lpBuffer, nBufferLength, bWatchSubtree, dwNotifyFilter, lpBytesReturned, overlapped, lpCompletionRoutine))

CREATE_WRAPPER_FUNCTION(kernel32, BOOL, WINAPI, PostQueuedCompletionStatus,
    (HANDLE CompletionPort, DWORD dwNumberOfBytesTransferred, ULONG_PTR dwCompletionKey, LPOVERLAPPED overlapped),
    (CompletionPort, dwNumberOfBytesTransferred, dwCompletionKey, overlapped))

CREATE_WRAPPER_FUNCTION(MSWSock, BOOL, PASCAL, ConnectEx,
    (UINT_PTR s, LPVOID name, int namelen, PVOID lpSendBuffer, DWORD dwSendDataLength, LPDWORD lpdwBytesSent, LPOVERLAPPED overlapped),
    (s, name, namelen, lpSendBuffer, dwSendDataLength, lpdwBytesSent, overlapped))

CREATE_WRAPPER_FUNCTION(WS2_32, int, PASCAL, WSASend,
    (UINT_PTR s, LPVOID lpBuffers, DWORD dwBufferCount, LPDWORD lpNumberOfBytesSent, DWORD dwFlags, LPOVERLAPPED overlapped, LPVOID lpCompletionRoutine),
    (s, lpBuffers, dwBufferCount, lpNumberOfBytesSent, dwFlags, overlapped, lpCompletionRoutine))

CREATE_WRAPPER_FUNCTION(WS2_32, int, PASCAL, WSASendTo,
    (UINT_PTR s, LPVOID lpBuffers, DWORD dwBufferCount, LPDWORD lpNumberOfBytesSent, DWORD dwFlags, LPVOID lpTo, int iToLen, LPOVERLAPPED overlapped, LPVOID lpCompletionRoutine),
    (s, lpBuffers, dwBufferCount, lpNumberOfBytesSent, dwFlags, lpTo, iToLen, overlapped, lpCompletionRoutine))

CREATE_WRAPPER_FUNCTION(WS2_32, int, PASCAL, WSARecv,
    (UINT_PTR s, LPVOID lpBuffers, DWORD dwBufferCount, LPDWORD lpNumberOfBytesRecvd, LPDWORD lpFlags, LPOVERLAPPED overlapped, LPVOID lpCompletionRoutine),
    (s, lpBuffers, dwBufferCount, lpNumberOfBytesRecvd, lpFlags, overlapped, lpCompletionRoutine))

CREATE_WRAPPER_FUNCTION(WS2_32, int, PASCAL, WSARecvFrom,
    (UINT_PTR s, LPVOID lpBuffers, DWORD dwBufferCount, LPDWORD lpNumberOfBytesRecvd, LPDWORD lpFlags, LPVOID lpFrom, LPINT lpFromlen, LPOVERLAPPED overlapped, LPVOID lpCompletionRoutine),
    (s, lpBuffers, dwBufferCount, lpNumberOfBytesRecvd, lpFlags, lpFrom, lpFromlen, overlapped, lpCompletionRoutine))

CREATE_WRAPPER_FUNCTION(MQRT, int, PASCAL, MQReceiveMessage,
    (HANDLE hSource, DWORD dwTimeout, DWORD dwAction, LPVOID pMessageProps, LPOVERLAPPED overlapped, LPVOID fnReceiveCallback, HANDLE hCursor, LPVOID pTransaction),
    (hSource, dwTimeout, dwAction, pMessageProps, overlapped, fnReceiveCallback, hCursor, pTransaction))

