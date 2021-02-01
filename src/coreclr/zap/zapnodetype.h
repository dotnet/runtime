// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// ZapNoteType.h
//

//
// Enum for ZapNode types
//
// ======================================================================================

#ifndef __ZAPNODETYPE_H__
#define __ZAPNODETYPE_H__

enum ZapNodeType {

// System types

    ZapNodeType_Unknown,

    ZapNodeType_PhysicalSection,
    ZapNodeType_VirtualSection,
    ZapNodeType_Blob,
    ZapNodeType_InnerPtr,

    ZapNodeType_Relocs,

// Headers

    ZapNodeType_CorHeader,
    ZapNodeType_NativeHeader,
    ZapNodeType_VersionInfo,
    ZapNodeType_Dependencies,
    ZapNodeType_CodeManagerEntry,
    ZapNodeType_MetaData,
    ZapNodeType_DebugDirectory,
    ZapNodeType_Win32Resources,

// PlaceHolders

    ZapNodeType_MethodEntryPoint,
    ZapNodeType_ClassHandle,
    ZapNodeType_MethodHandle,
    ZapNodeType_FieldHandle,
    ZapNodeType_AddrOfPInvokeFixup,
    ZapNodeType_GenericHandle,
    ZapNodeType_ModuleIDHandle,

// Code references

    ZapNodeType_MethodHeader,
    ZapNodeType_CodeManagerMap,
    ZapNodeType_UnwindInfo,
    ZapNodeType_UnwindData,
    ZapNodeType_UnwindDataAndGCInfo,
    ZapNodeType_FilterFuncletUnwindData,

    ZapNodeType_ProfileData,
    ZapNodeType_VirtualSectionsTable,

    ZapNodeType_DebugInfoTable,
    ZapNodeType_DebugInfoLabelledEntry,

    ZapNodeType_HelperThunk,
    ZapNodeType_LazyHelperThunk,
    ZapNodeType_IndirectHelperThunk,

    ZapNodeType_ExceptionInfoTable,
    ZapNodeType_UnwindInfoLookupTable,
    ZapNodeType_ColdCodeMap,

// Wrappers

    ZapNodeType_Stub,

// Imports

    ZapNodeType_ExternalMethodThunk,
    ZapNodeType_VirtualMethodThunk,

    ZapNodeType_ExternalMethodCell,
    ZapNodeType_StubDispatchCell,
    ZapNodeType_DynamicHelperCell,

    ZapNodeType_Import_FunctionEntry,
    ZapNodeType_Import_ModuleHandle,
    ZapNodeType_Import_ClassHandle,
    ZapNodeType_Import_MethodHandle,
    ZapNodeType_Import_FieldHandle,
    ZapNodeType_Import_IndirectPInvokeTarget,
    ZapNodeType_Import_PInvokeTarget,
    ZapNodeType_Import_StringHandle,
    ZapNodeType_Import_StaticFieldAddress,
    ZapNodeType_Import_ClassDomainId,
    ZapNodeType_Import_ModuleDomainId,
    ZapNodeType_Import_SyncLock,
    ZapNodeType_Import_ProfilingHandle,
    ZapNodeType_Import_VarArg,
    ZapNodeType_Import_ActiveDependency,
    ZapNodeType_Import_Helper,

    ZapNodeType_GenericSignature,

    ZapNodeType_ImportTable,

    ZapNodeType_ImportSectionsTable,
    ZapNodeType_ImportSectionSignatures,

    ZapNodeType_GCRefMapTable,

    ZapNodeType_RVAFieldData,
    ZapNodeType_EntryPointsTable,

    ZapNodeType_StoredStructure,            // The ZapNodeTypes of the legacy stored structures start here
};

#endif // __ZAPNODETYPE_H__
