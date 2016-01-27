// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//+------------------------------------------------------------------------
//
//  Declare DLL entry points for Cor API to threadpool
//
//-------------------------------------------------------------------------

#ifdef EXPORTING_THREADPOOL_API
#define DllExportOrImport extern "C" __declspec (dllexport)
#else
#define DllExportOrImport extern "C" 
#endif

typedef VOID (__stdcall *WAITORTIMERCALLBACK)(PVOID, BOOL); 

DllExportOrImport  BOOL __cdecl CorRegisterWaitForSingleObject(PHANDLE phNewWaitObject,
                                                      HANDLE hWaitObject,
                                                      WAITORTIMERCALLBACK Callback,
                                                      PVOID Context,
                                                      ULONG timeout,
                                                      BOOL  executeOnlyOnce );



DllExportOrImport BOOL __cdecl CorUnregisterWait(HANDLE hWaitObject,HANDLE CompletionEvent);

DllExportOrImport BOOL __cdecl CorQueueUserWorkItem(LPTHREAD_START_ROUTINE Function,
                                          PVOID Context,
                                          BOOL executeOnlyOnce );


DllExportOrImport BOOL __cdecl CorCreateTimer(PHANDLE phNewTimer,
                                     WAITORTIMERCALLBACK Callback,
                                     PVOID Parameter,
                                     DWORD DueTime,
                                     DWORD Period);

DllExportOrImport BOOL __cdecl CorChangeTimer(HANDLE Timer,
                                              ULONG DueTime,
                                              ULONG Period);

DllExportOrImport BOOL __cdecl CorDeleteTimer(HANDLE Timer,
                                              HANDLE CompletionEvent);

DllExportOrImport  VOID __cdecl CorBindIoCompletionCallback(HANDLE fileHandle, LPOVERLAPPED_COMPLETION_ROUTINE callback); 


DllExportOrImport  VOID __cdecl CorDoDelegateInvocation(int cookie); 
