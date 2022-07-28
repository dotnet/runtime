// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#pragma once
#include "ModuleHeaders.h"
#include "ICodeManager.h"

class DispatchMap;

class TypeManager
{
    // NOTE: Part of this layout is a contract with the managed side in TypeManagerHandle.cs
    HANDLE                      m_osModule;
    ReadyToRunHeader *          m_pHeader;
    DispatchMap**               m_pDispatchMapTable;
    uint8_t*                    m_pStaticsGCDataSection;
    uint8_t*                    m_pThreadStaticsDataSection;
    void**                      m_pClasslibFunctions;
    uint32_t                    m_nClasslibFunctions;

    TypeManager(HANDLE osModule, ReadyToRunHeader * pHeader, void** pClasslibFunctions, uint32_t nClasslibFunctions);

public:
    static TypeManager * Create(HANDLE osModule, void * pModuleHeader, void** pClasslibFunctions, uint32_t nClasslibFunctions);
    void * GetModuleSection(ReadyToRunSectionType sectionId, int * length);
    HANDLE GetOsModuleHandle();
    void* GetClasslibFunction(ClasslibFunctionId functionId);

private:

    struct ModuleInfoRow
    {
        int32_t SectionId;
        int32_t Flags;
        void * Start;
        void * End;

        bool HasEndPointer();
        int GetLength();
    };
};

// TypeManagerHandle represents an AOT module in MRT based runtimes.
// These handles are a pointer to a TypeManager.
struct TypeManagerHandle
{
    static TypeManagerHandle Null()
    {
        TypeManagerHandle handle;
        handle._value = nullptr;
        return handle;
    }

    static TypeManagerHandle Create(TypeManager * value)
    {
        TypeManagerHandle handle;
        handle._value = value;
        return handle;
    }

    void *_value;

    TypeManager* AsTypeManager();
};

