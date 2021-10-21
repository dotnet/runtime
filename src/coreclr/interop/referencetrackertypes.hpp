// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _INTEROP_REFERENCETRACKERTYPES_H_
#define _INTEROP_REFERENCETRACKERTYPES_H_

#include <unknwn.h>

// Documentation found at https://docs.microsoft.com/windows/win32/api/windows.ui.xaml.hosting.referencetracker/

// 64bd43f8-bfee-4ec4-b7eb-2935158dae21
const GUID IID_IReferenceTrackerTarget = { 0x64bd43f8, 0xbfee, 0x4ec4, { 0xb7, 0xeb, 0x29, 0x35, 0x15, 0x8d, 0xae, 0x21} };

class DECLSPEC_UUID("64bd43f8-bfee-4ec4-b7eb-2935158dae21") IReferenceTrackerTarget : public IUnknown
{
public:
    STDMETHOD_(ULONG, AddRefFromReferenceTracker)() = 0;
    STDMETHOD_(ULONG, ReleaseFromReferenceTracker)() = 0;
    STDMETHOD(Peg)() = 0;
    STDMETHOD(Unpeg)() = 0;
};

class DECLSPEC_UUID("29a71c6a-3c42-4416-a39d-e2825a07a773") IReferenceTrackerHost : public IUnknown
{
public:
    STDMETHOD(DisconnectUnusedReferenceSources)(_In_ DWORD dwFlags) = 0;
    STDMETHOD(ReleaseDisconnectedReferenceSources)() = 0;
    STDMETHOD(NotifyEndOfReferenceTrackingOnThread)() = 0;
    STDMETHOD(GetTrackerTarget)(_In_ IUnknown* obj, _Outptr_ IReferenceTrackerTarget** ppNewReference) = 0;
    STDMETHOD(AddMemoryPressure)(_In_ UINT64 bytesAllocated) = 0;
    STDMETHOD(RemoveMemoryPressure)(_In_ UINT64 bytesAllocated) = 0;
};

class DECLSPEC_UUID("3cf184b4-7ccb-4dda-8455-7e6ce99a3298") IReferenceTrackerManager : public IUnknown
{
public:
    STDMETHOD(ReferenceTrackingStarted)() = 0;
    STDMETHOD(FindTrackerTargetsCompleted)(_In_ BOOL bWalkFailed) = 0;
    STDMETHOD(ReferenceTrackingCompleted)() = 0;
    STDMETHOD(SetReferenceTrackerHost)(_In_ IReferenceTrackerHost *pCLRServices) = 0;
};

class DECLSPEC_UUID("04b3486c-4687-4229-8d14-505ab584dd88") IFindReferenceTargetsCallback : public IUnknown
{
public:
    STDMETHOD(FoundTrackerTarget)(_In_ IReferenceTrackerTarget* target) = 0;
};

// 11d3b13a-180e-4789-a8be-7712882893e6
const GUID IID_IReferenceTracker = { 0x11d3b13a, 0x180e, 0x4789, { 0xa8, 0xbe, 0x77, 0x12, 0x88, 0x28, 0x93, 0xe6} };

class DECLSPEC_UUID("11d3b13a-180e-4789-a8be-7712882893e6") IReferenceTracker : public IUnknown
{
public:
    STDMETHOD(ConnectFromTrackerSource)() = 0;
    STDMETHOD(DisconnectFromTrackerSource)() = 0;
    STDMETHOD(FindTrackerTargets)(_In_ IFindReferenceTargetsCallback *pCallback) = 0;
    STDMETHOD(GetReferenceTrackerManager)(_Outptr_ IReferenceTrackerManager **ppTrackerManager) = 0;
    STDMETHOD(AddRefFromTrackerSource)() = 0;
    STDMETHOD(ReleaseFromTrackerSource)() = 0;
    STDMETHOD(PegFromTrackerSource)() = 0;
};

#endif // _INTEROP_REFERENCETRACKERTYPES_H_
