// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// dbgutil.h
//

//
//*****************************************************************************

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

HRESULT GetResourceRvaFromResourceSectionRva(ICorDebugDataTarget* pDataTarget,
    ULONG64 moduleBaseAddress,
    DWORD resourceSectionRva,
    DWORD type,
    DWORD name,
    DWORD language,
    DWORD* pResourceRva,
    DWORD* pResourceSize);

HRESULT GetResourceRvaFromResourceSectionRvaByName(ICorDebugDataTarget* pDataTarget,
    ULONG64 moduleBaseAddress,
    DWORD resourceSectionRva,
    DWORD type,
    LPCWSTR pwszName,
    DWORD language,
    DWORD* pResourceRva,
    DWORD* pResourceSize);

// Traverses down one level in the PE resource tree structure
//
// Arguments:
//   pDataTarget - the data target for inspecting this process
//   id - the id of the next node in the resource tree you want
//   moduleBaseAddress - the base address of the module being inspected
//   resourceDirectoryRVA - the base address of the beginning of the resource directory for this
//                          level of the tree
//   pNextLevelRVA - out - The RVA for the next level tree directory or the RVA of the resource entry
//
// Returns:
//   S_OK if successful or an appropriate failing HRESULT
HRESULT GetNextLevelResourceEntryRVA(ICorDebugDataTarget* pDataTarget,
    DWORD id,
    ULONG64 moduleBaseAddress,
    DWORD resourceDirectoryRVA,
    DWORD* pNextLevelRVA);

// Traverses down one level in the PE resource tree structure
//
// Arguments:
//   pDataTarget - the data target for inspecting this process
//   name - the name of the next node in the resource tree you want
//   moduleBaseAddress - the base address of the module being inspected
//   resourceDirectoryRVA - the base address of the beginning of the resource directory for this
//                          level of the tree
//   resourceSectionRVA - the rva of the beginning of the resource section of the PE file
//   pNextLevelRVA - out - The RVA for the next level tree directory or the RVA of the resource entry
//
// Returns:
//   S_OK if successful or an appropriate failing HRESULT
HRESULT GetNextLevelResourceEntryRVAByName(ICorDebugDataTarget* pDataTarget,
    LPCWSTR pwzName,
    ULONG64 moduleBaseAddress,
    DWORD resourceDirectoryRva,
    DWORD resourceSectionRva,
    DWORD* pNextLevelRva);

// A small wrapper that reads from the data target and throws on error
HRESULT ReadFromDataTarget(ICorDebugDataTarget* pDataTarget,
    ULONG64 addr,
    BYTE* pBuffer,
    ULONG32 bytesToRead);
