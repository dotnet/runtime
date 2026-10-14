// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once
#include <cor.h>
#include <cordebug.h>
#include <metahost.h>

//
// Various common helpers used by multiple debug components.
//

// Returns the RVA of the resource section for the module specified by the given data target and module base.
// Returns failure if the module doesn't have a resource section.
//
// Arguments
//   pDataTarget - dataTarget for the process we are inspecting
//   moduleBaseAddress - base address of a module we should inspect
//   pwImageFileMachine - updated with the Machine from the IMAGE_FILE_HEADER
//   pdwResourceSectionRVA - updated with the resultant RVA on success
HRESULT GetMachineAndResourceSectionRVA(ICorDebugDataTarget* pDataTarget,
    ULONG64 moduleBaseAddress,
    WORD* pwImageFileMachine,
    DWORD* pdwResourceSectionRVA);

// A small wrapper that reads from the data target and throws on error
HRESULT ReadFromDataTarget(ICorDebugDataTarget* pDataTarget,
    ULONG64 addr,
    BYTE* pBuffer,
    ULONG32 bytesToRead);
