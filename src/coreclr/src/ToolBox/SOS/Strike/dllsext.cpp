// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
// 
 
// 
// ==--==
#include "strike.h"
#include "data.h"
#include "util.h"
#include "platformspecific.h"

typedef struct _PRIVATE_LDR_DATA_TABLE_ENTRY {
    LIST_ENTRY InLoadOrderLinks;
    LIST_ENTRY InMemoryOrderLinks;
    LIST_ENTRY InInitializationOrderLinks;
    PVOID DllBase;
    PVOID EntryPoint;
    ULONG SizeOfImage;
    UNICODE_STRING FullDllName;
    UNICODE_STRING BaseDllName;
    ULONG Flags;
    USHORT LoadCount;
    USHORT TlsIndex;
    union _LDR_DATA_TABLE_ENTRY_UNION1 {    //DevDiv LKG RC Changes: Added union name to avoid warning C4408
        LIST_ENTRY HashLinks;
        struct _LDR_DATA_TABLE_ENTRY_STRUCT1 {  //DevDiv LKG RC Changes: Added struct name to avoid warning C4201
            PVOID SectionPointer;
        ULONG CheckSum;
        };
    };
    union _LDR_DATA_TABLE_ENTRY_UNION2 {    //DevDiv LKG RC Changes: Added union name to avoid warning C4408
        struct _LDR_DATA_TABLE_ENTRY_STRUCT2 {  //DevDiv LKG RC Changes: Added struct name to avoid warning C4201
    ULONG TimeDateStamp;
        };
        struct _LDR_DATA_TABLE_ENTRY_STRUCT3 {  //DevDiv LKG RC Changes: Added struct name to avoid warning C4201
            PVOID LoadedImports;
        };
    };
    struct _ACTIVATION_CONTEXT * EntryPointActivationContext;
    
    PVOID PatchInformation; 
    
} PRIVATE_LDR_DATA_TABLE_ENTRY, *PRIVATE_PLDR_DATA_TABLE_ENTRY;


#ifndef FEATURE_PAL
static void DllsNameFromPeb(
    ULONG_PTR addrContaining,
    __out_ecount (MAX_LONGPATH) WCHAR *dllName
	)
{
    ULONG64 ProcessPeb;
    g_ExtSystem->GetCurrentProcessPeb (&ProcessPeb);

    ULONG64 pLdrEntry;
    ULONG64 PebLdrAddress;
    ULONG64 Next;
    ULONG64 OrderModuleListStart;
    
    //
    // Capture PebLdrData
    //

    static ULONG Offset_Ldr = -1;
    if (Offset_Ldr == -1)
    {
        ULONG TypeId;
        ULONG64 NtDllBase;
        if (SUCCEEDED(g_ExtSymbols->GetModuleByModuleName ("ntdll",0,NULL,
                                                           &NtDllBase))
            && SUCCEEDED(g_ExtSymbols->GetTypeId (NtDllBase, "PEB", &TypeId)))
        {
            if (FAILED (g_ExtSymbols->GetFieldOffset(NtDllBase, TypeId,
                                                     "Ldr", &Offset_Ldr)))
                Offset_Ldr = -1;
        }
    }
    // We can not get it from PDB.  Use the fixed one.
    if (Offset_Ldr == -1)
        Offset_Ldr = offsetof (DT_PEB, Ldr);

    DT_PEB peb = {0};
    if (FAILED(g_ExtData->ReadVirtual(ProcessPeb+Offset_Ldr, &peb.Ldr,
                                      sizeof(peb.Ldr), NULL)))
    {
        ExtOut ( "    Unable to read PEB_LDR_DATA address at %p\n", SOS_PTR(ProcessPeb+Offset_Ldr));
        return;
    }

    PebLdrAddress = (ULONG64)peb.Ldr;
    
    //
    // Walk through the loaded module table and display all ldr data
    //

    static ULONG Offset_ModuleList = -1;
    if (Offset_ModuleList == -1)
    {
        ULONG TypeId;
        ULONG64 NtDllBase;
        if (SUCCEEDED(g_ExtSymbols->GetModuleByModuleName ("ntdll",0,NULL,
                                                           &NtDllBase))
            && SUCCEEDED(g_ExtSymbols->GetTypeId (NtDllBase, "PEB_LDR_DATA",
                                                  &TypeId)))
        {
            if (FAILED (g_ExtSymbols->GetFieldOffset(NtDllBase, TypeId,
                                                     "InMemoryOrderModuleList",
                                                     &Offset_ModuleList)))
                Offset_ModuleList = -1;
        }
    }
    // We can not get it from PDB.  Use the fixed one.
    if (Offset_ModuleList == -1)
        Offset_ModuleList = offsetof (DT_PEB_LDR_DATA, InMemoryOrderModuleList);
    
    OrderModuleListStart = PebLdrAddress + Offset_ModuleList;
    DT_PEB_LDR_DATA Ldr = {0};
    if (FAILED(g_ExtData->ReadVirtual(OrderModuleListStart,
                                      &Ldr.InMemoryOrderModuleList,
                                      sizeof(Ldr.InMemoryOrderModuleList),
                                      NULL)))
    {
        ExtOut ( "    Unable to read InMemoryOrderModuleList address at %p\n", SOS_PTR(OrderModuleListStart));
        return;
    }
    Next = (ULONG64)Ldr.InMemoryOrderModuleList.Flink;

    static ULONG Offset_OrderLinks = -1;
    static ULONG Offset_FullDllName = -1;
    static ULONG Offset_DllBase = -1;
    static ULONG Offset_SizeOfImage = -1;
    if (Offset_OrderLinks == -1)
    {
        ULONG TypeId;
        ULONG64 NtDllBase;
        if (SUCCEEDED(g_ExtSymbols->GetModuleByModuleName ("ntdll",0,NULL,
                                                           &NtDllBase))
            && SUCCEEDED(g_ExtSymbols->GetTypeId (NtDllBase, "LDR_DATA_TABLE_ENTRY",
                                                  &TypeId)))
        {
            if (FAILED (g_ExtSymbols->GetFieldOffset(NtDllBase, TypeId,
                                                     "InMemoryOrderLinks",
                                                     &Offset_OrderLinks)))
                Offset_OrderLinks = -1;
            if (FAILED (g_ExtSymbols->GetFieldOffset(NtDllBase, TypeId,
                                                     "FullDllName",
                                                     &Offset_FullDllName)))
                Offset_FullDllName = -1;
            if (FAILED (g_ExtSymbols->GetFieldOffset(NtDllBase, TypeId,
                                                     "DllBase",
                                                     &Offset_DllBase)))
                Offset_DllBase = -1;
            if (FAILED (g_ExtSymbols->GetFieldOffset(NtDllBase, TypeId,
                                                     "SizeOfImage",
                                                     &Offset_SizeOfImage)))
                Offset_SizeOfImage = -1;
        }
    }

    // We can not get it from PDB.  Use the fixed one.
    if (Offset_OrderLinks == -1 || Offset_OrderLinks == 0)
    {
        Offset_OrderLinks = offsetof (PRIVATE_LDR_DATA_TABLE_ENTRY,
                                      InMemoryOrderLinks);
        Offset_FullDllName = offsetof (PRIVATE_LDR_DATA_TABLE_ENTRY,
                                       FullDllName);
        Offset_DllBase = offsetof (PRIVATE_LDR_DATA_TABLE_ENTRY,
                                   DllBase);
        Offset_SizeOfImage = offsetof (PRIVATE_LDR_DATA_TABLE_ENTRY,
                                       SizeOfImage);
    }

    _UNICODE_STRING FullDllName;
    __try {
        while (Next != OrderModuleListStart) {
            if (IsInterrupt())
                return;
            
            pLdrEntry = Next - Offset_OrderLinks;
    
            //
            // Capture LdrEntry
            //
            if (FAILED(g_ExtData->ReadVirtual(pLdrEntry + Offset_FullDllName,
                                              &FullDllName,
                                              sizeof(FullDllName),
                                              NULL)))
            {
                ExtOut ( "    Unable to read FullDllName address at %p\n",
                         pLdrEntry + Offset_FullDllName);
                return;
            }
            ZeroMemory( dllName, MAX_LONGPATH * sizeof (WCHAR) );
            if (FAILED(g_ExtData->ReadVirtual((ULONG64)FullDllName.Buffer,
                                              dllName,
                                              MAX_LONGPATH < FullDllName.Length ? MAX_LONGPATH : FullDllName.Length,
                                              NULL)))
            {
#if 0
                ExtOut ( "    Unable to read FullDllName.Buffer address at %p\n",
                         SOS_PTR(FullDllName.Buffer));
#endif
                ZeroMemory( dllName, MAX_LONGPATH * sizeof (WCHAR) );
            }
    
            //
            // Dump the ldr entry data
            // (dump all the entries if no containing address specified)
            //
            PRIVATE_LDR_DATA_TABLE_ENTRY LdrEntry = {0};
            if (SUCCEEDED(g_ExtData->ReadVirtual(pLdrEntry + Offset_DllBase,
                                                 &LdrEntry.DllBase,
                                                 sizeof(LdrEntry.DllBase),
                                                 NULL))
                &&
                SUCCEEDED(g_ExtData->ReadVirtual(pLdrEntry + Offset_SizeOfImage,
                                                 &LdrEntry.SizeOfImage,
                                                 sizeof(LdrEntry.SizeOfImage),
                                                 NULL))
                )
            {
                if (((ULONG_PTR)LdrEntry.DllBase <= addrContaining) &&
                    (addrContaining <= (ULONG_PTR)LdrEntry.DllBase + (ULONG_PTR)LdrEntry.SizeOfImage))
                    break;
            }
    
            ZeroMemory( dllName, MAX_LONGPATH * sizeof (WCHAR) );
            if (FAILED(g_ExtData->ReadVirtual(pLdrEntry + Offset_OrderLinks,
                                              &LdrEntry.InMemoryOrderLinks,
                                              sizeof(LdrEntry.InMemoryOrderLinks),
                                              NULL)))
                break;
            
            Next = (ULONG64)LdrEntry.InMemoryOrderLinks.Flink;
        }
    } __except (EXCEPTION_EXECUTE_HANDLER)
    {
        ExtOut ("exception during reading PEB\n");
        return;
    }
}
#endif

HRESULT
DllsName(
    ULONG_PTR addrContaining,
    __out_ecount (MAX_LONGPATH) WCHAR *dllName
    )
{
    dllName[0] = L'\0';
    
    ULONG Index;
    ULONG64 base;
    HRESULT hr = g_ExtSymbols->GetModuleByOffset(addrContaining, 0, &Index, &base);
    if (FAILED(hr))
        return hr;
    
    CHAR name[MAX_LONGPATH+1];
    ULONG length;
    
    hr = g_ExtSymbols->GetModuleNames(Index,base,name,MAX_LONGPATH,&length,NULL,0,NULL,NULL,0,NULL);
    
    if (SUCCEEDED(hr))
    {
        MultiByteToWideChar (CP_ACP,0,name,-1,dllName,MAX_LONGPATH);
    }
    
#ifndef FEATURE_PAL
    if (_wcsrchr (dllName, '\\') == NULL) {
        DllsNameFromPeb (addrContaining,dllName);
    }
#endif

    return hr;
}
