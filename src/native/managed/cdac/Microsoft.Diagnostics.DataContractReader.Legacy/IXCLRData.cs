// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

// This file contains managed declarations for the IXCLRData interfaces.
// See src/coreclr/inc/xclrdata.idl

public struct CLRDataModuleExtent
{
    public ClrDataAddress baseAddress;
    public uint length;
    public uint /* CLRDataModuleExtentType */ type;
}

public struct DacpGetModuleAddress
{
    public ClrDataAddress ModulePtr;
}

public struct DacpGetModuleData
{
    public uint IsDynamic;
    public uint IsInMemory;
    public uint IsFileLayout;
    public ClrDataAddress PEAssembly; // Actually the module address in .NET 9+
    public ClrDataAddress LoadedPEAddress;
    public ulong LoadedPESize;
    public ClrDataAddress InMemoryPdbAddress;
    public ulong InMemoryPdbSize;
}

public enum CLRDataByNameFlag : uint
{
    CLRDATA_BYNAME_CASE_SENSITIVE = 0,
    CLRDATA_BYNAME_CASE_INSENSITIVE = 1
}

[Flags]
public enum CLRDataMethodCodeNotification : uint
{
    CLRDATA_METHNOTIFY_NONE      = 0x00000000,
    CLRDATA_METHNOTIFY_GENERATED = 0x00000001,
    CLRDATA_METHNOTIFY_DISCARDED = 0x00000002,
}

public unsafe struct EXCEPTION_RECORD64
{
    public const int ExceptionMaximumParameters = 15;

    public uint ExceptionCode;
    public uint ExceptionFlags;
    public ulong ExceptionRecord;
    public ulong ExceptionAddress;
    public uint NumberParameters;
    public uint _unusedAlignment;
    public fixed ulong ExceptionInformation[ExceptionMaximumParameters];
}

[GeneratedComInterface]
[Guid("88E32849-0A0A-4cb0-9022-7CD2E9E139E2")]
public unsafe partial interface IXCLRDataModule
{
    [PreserveSig]
    int StartEnumAssemblies(ulong* handle);
    [PreserveSig]
    int EnumAssembly(ulong* handle, DacComNullableByRef<IXCLRDataAssembly> assembly);
    [PreserveSig]
    int EndEnumAssemblies(ulong handle);

    [PreserveSig]
    int StartEnumTypeDefinitions(ulong* handle);
    [PreserveSig]
    int EnumTypeDefinition(ulong* handle, DacComNullableByRef<IXCLRDataTypeDefinition> typeDefinition);
    [PreserveSig]
    int EndEnumTypeDefinitions(ulong handle);

    [PreserveSig]
    int StartEnumTypeInstances(IXCLRDataAppDomain? appDomain, ulong* handle);
    [PreserveSig]
    int EnumTypeInstance(ulong* handle, DacComNullableByRef<IXCLRDataTypeInstance> typeInstance);
    [PreserveSig]
    int EndEnumTypeInstances(ulong handle);

    [PreserveSig]
    int StartEnumTypeDefinitionsByName(char* name, uint flags, ulong* handle);
    [PreserveSig]
    int EnumTypeDefinitionByName(ulong* handle, DacComNullableByRef<IXCLRDataTypeDefinition> type);
    [PreserveSig]
    int EndEnumTypeDefinitionsByName(ulong handle);

    [PreserveSig]
    int StartEnumTypeInstancesByName(char* name, uint flags, IXCLRDataAppDomain? appDomain, ulong* handle);
    [PreserveSig]
    int EnumTypeInstanceByName(ulong* handle, DacComNullableByRef<IXCLRDataTypeInstance> type);
    [PreserveSig]
    int EndEnumTypeInstancesByName(ulong handle);

    [PreserveSig]
    int GetTypeDefinitionByToken(/*mdTypeDef*/ uint token, DacComNullableByRef<IXCLRDataTypeDefinition> typeDefinition);

    [PreserveSig]
    int StartEnumMethodDefinitionsByName(char* name, uint flags, ulong* handle);
    [PreserveSig]
    int EnumMethodDefinitionByName(ulong* handle, DacComNullableByRef<IXCLRDataMethodDefinition> method);
    [PreserveSig]
    int EndEnumMethodDefinitionsByName(ulong handle);

    [PreserveSig]
    int StartEnumMethodInstancesByName(char* name, uint flags, IXCLRDataAppDomain? appDomain, ulong* handle);
    [PreserveSig]
    int EnumMethodInstanceByName(ulong* handle, DacComNullableByRef<IXCLRDataMethodInstance> method);
    [PreserveSig]
    int EndEnumMethodInstancesByName(ulong handle);

    [PreserveSig]
    int GetMethodDefinitionByToken(/*mdMethodDef*/ uint token, DacComNullableByRef<IXCLRDataMethodDefinition> methodDefinition);

    [PreserveSig]
    int StartEnumDataByName(char* name, uint flags, IXCLRDataAppDomain? appDomain, IXCLRDataTask? tlsTask, ulong* handle);
    [PreserveSig]
    int EnumDataByName(ulong* handle, DacComNullableByRef<IXCLRDataValue> value);
    [PreserveSig]
    int EndEnumDataByName(ulong handle);

    [PreserveSig]
    int GetName(uint bufLen, uint* nameLen, char* name);
    [PreserveSig]
    int GetFileName(uint bufLen, uint* nameLen, char* name);

    [PreserveSig]
    int GetFlags(uint* flags);

    [PreserveSig]
    int IsSameObject(IXCLRDataModule* mod);

    [PreserveSig]
    int StartEnumExtents(ulong* handle);
    [PreserveSig]
    int EnumExtent(ulong* handle, /*CLRDATA_MODULE_EXTENT*/ void* extent);
    [PreserveSig]
    int EndEnumExtents(ulong handle);

    [PreserveSig]
    int Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer);

    [PreserveSig]
    int StartEnumAppDomains(ulong* handle);
    [PreserveSig]
    int EnumAppDomain(ulong* handle, /*IXCLRDataAppDomain*/ void** appDomain);
    [PreserveSig]
    int EndEnumAppDomains(ulong handle);

    [PreserveSig]
    int GetVersionId(Guid* vid);
}

[GeneratedComInterface]
[Guid("34625881-7EB3-4524-817B-8DB9D064C760")]
public unsafe partial interface IXCLRDataModule2
{
    [PreserveSig]
    int SetJITCompilerFlags(uint flags);
}

[GeneratedComInterface]
[Guid("5c552ab6-fc09-4cb3-8e36-22fa03c798b7")]
public unsafe partial interface IXCLRDataProcess
{
    [PreserveSig]
    int Flush();

    [PreserveSig]
    int StartEnumTasks(ulong* handle);
    [PreserveSig]
    int EnumTask(ulong* handle, DacComNullableByRef<IXCLRDataTask> task);
    [PreserveSig]
    int EndEnumTasks(ulong handle);

    [PreserveSig]
    int GetTaskByOSThreadID(uint osThreadID, DacComNullableByRef<IXCLRDataTask> task);
    [PreserveSig]
    int GetTaskByUniqueID(ulong taskID, DacComNullableByRef<IXCLRDataTask> task);

    [PreserveSig]
    int GetFlags(uint* flags);

    [PreserveSig]
    int IsSameObject(IXCLRDataProcess* process);

    [PreserveSig]
    int GetManagedObject(DacComNullableByRef<IXCLRDataValue> value);

    [PreserveSig]
    int GetDesiredExecutionState(uint* state);
    [PreserveSig]
    int SetDesiredExecutionState(uint state);

    [PreserveSig]
    int GetAddressType(ClrDataAddress address, /*CLRDataAddressType*/ uint* type);

    [PreserveSig]
    int GetRuntimeNameByAddress(
        ClrDataAddress address,
        uint flags,
        uint bufLen,
        uint* nameLen,
        char* nameBuf,
        ClrDataAddress* displacement);

    [PreserveSig]
    int StartEnumAppDomains(ulong* handle);
    [PreserveSig]
    int EnumAppDomain(ulong* handle, /*IXCLRDataAppDomain*/ void** appDomain);
    [PreserveSig]
    int EndEnumAppDomains(ulong handle);
    [PreserveSig]
    int GetAppDomainByUniqueID(ulong id, /*IXCLRDataAppDomain*/ void** appDomain);

    [PreserveSig]
    int StartEnumAssemblies(ulong* handle);
    [PreserveSig]
    int EnumAssembly(ulong* handle, DacComNullableByRef<IXCLRDataAssembly> assembly);
    [PreserveSig]
    int EndEnumAssemblies(ulong handle);

    [PreserveSig]
    int StartEnumModules(ulong* handle);
    [PreserveSig]
    int EnumModule(ulong* handle, DacComNullableByRef<IXCLRDataModule> mod);
    [PreserveSig]
    int EndEnumModules(ulong handle);
    [PreserveSig]
    int GetModuleByAddress(ClrDataAddress address, DacComNullableByRef<IXCLRDataModule> mod);

    [PreserveSig]
    int StartEnumMethodInstancesByAddress(ClrDataAddress address, IXCLRDataAppDomain? appDomain, ulong* handle);
    [PreserveSig]
    int EnumMethodInstanceByAddress(ulong* handle, DacComNullableByRef<IXCLRDataMethodInstance> method);
    [PreserveSig]
    int EndEnumMethodInstancesByAddress(ulong handle);

    [PreserveSig]
    int GetDataByAddress(
        ClrDataAddress address,
        uint flags,
        IXCLRDataAppDomain? appDomain,
        IXCLRDataTask? tlsTask,
        uint bufLen,
        uint* nameLen,
        char* nameBuf,
        DacComNullableByRef<IXCLRDataValue> value,
        ClrDataAddress* displacement);

    [PreserveSig]
    int GetExceptionStateByExceptionRecord(EXCEPTION_RECORD64* record, DacComNullableByRef<IXCLRDataExceptionState> exState);
    [PreserveSig]
    int TranslateExceptionRecordToNotification(EXCEPTION_RECORD64* record, [MarshalUsing(typeof(UniqueComInterfaceMarshaller<IXCLRDataExceptionNotification>))] IXCLRDataExceptionNotification notify);

    [PreserveSig]
    int Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer);

    [PreserveSig]
    int CreateMemoryValue(
        IXCLRDataAppDomain? appDomain,
        IXCLRDataTask? tlsTask,
        IXCLRDataTypeInstance? type,
        ClrDataAddress addr,
        DacComNullableByRef<IXCLRDataValue> value);

    [PreserveSig]
    int SetAllTypeNotifications(IXCLRDataModule? mod, uint flags);
    [PreserveSig]
    int SetAllCodeNotifications(IXCLRDataModule? mod, uint flags);
    [PreserveSig]
    int GetTypeNotifications(
        uint numTokens,
        /*IXCLRDataModule*/ void** mods,
        IXCLRDataModule? singleMod,
        [In, MarshalUsing(CountElementName = nameof(numTokens))] /*mdTypeDef*/ uint[]? tokens,
        [In, Out, MarshalUsing(CountElementName = nameof(numTokens))] uint[]? flags);
    [PreserveSig]
    int SetTypeNotifications(
        uint numTokens,
        /*IXCLRDataModule*/ void** mods,
        IXCLRDataModule? singleMod,
        [In, MarshalUsing(CountElementName = nameof(numTokens))] /*mdTypeDef*/ uint[]? tokens,
        [In, MarshalUsing(CountElementName = nameof(numTokens))] uint[]? flags,
        uint singleFlags);
    [PreserveSig]
    int GetCodeNotifications(
        uint numTokens,
        /*IXCLRDataModule*/ void** mods,
        IXCLRDataModule? singleMod,
        [In, MarshalUsing(CountElementName = nameof(numTokens))] /*mdMethodDef*/ uint[]? tokens,
        [In, Out, MarshalUsing(CountElementName = nameof(numTokens))] uint[]? flags);
    [PreserveSig]
    int SetCodeNotifications(
        uint numTokens,
        /*IXCLRDataModule*/ void** mods,
        IXCLRDataModule? singleMod,
        [In, MarshalUsing(CountElementName = nameof(numTokens))] /*mdMethodDef */ uint[]? tokens,
        [In, MarshalUsing(CountElementName = nameof(numTokens))] uint[]? flags,
        uint singleFlags);
    [PreserveSig]
    int GetOtherNotificationFlags(uint* flags);
    [PreserveSig]
    int SetOtherNotificationFlags(uint flags);

    [PreserveSig]
    int StartEnumMethodDefinitionsByAddress(ClrDataAddress address, ulong* handle);
    [PreserveSig]
    int EnumMethodDefinitionByAddress(ulong* handle, DacComNullableByRef<IXCLRDataMethodDefinition> method);
    [PreserveSig]
    int EndEnumMethodDefinitionsByAddress(ulong handle);

    [PreserveSig]
    int FollowStub(
        uint inFlags,
        ClrDataAddress inAddr,
        /*struct CLRDATA_FOLLOW_STUB_BUFFER*/ void* inBuffer,
        ClrDataAddress* outAddr,
        /*struct CLRDATA_FOLLOW_STUB_BUFFER*/ void* outBuffer,
        uint* outFlags);
    [PreserveSig]
    int FollowStub2(
        IXCLRDataTask? task,
        uint inFlags,
        ClrDataAddress inAddr,
        /*struct CLRDATA_FOLLOW_STUB_BUFFER*/ void* inBuffer,
        ClrDataAddress* outAddr,
        /*struct CLRDATA_FOLLOW_STUB_BUFFER*/ void* outBuffer,
        uint* outFlags);

    [PreserveSig]
    int DumpNativeImage(
        ClrDataAddress loadedBase,
        char* name,
        /*IXCLRDataDisplay*/ void* display,
        /*IXCLRLibrarySupport*/ void* libSupport,
        /*IXCLRDisassemblySupport*/ void* dis);
}

public struct GcEvtArgs
{
    public enum GcEvt_t
    {
        GC_MARK_END = 1,
        GC_EVENT_TYPE_MAX = GC_MARK_END + 1
    };
    public GcEvt_t type;
    public int condemnedGeneration;
}

[GeneratedComInterface]
[Guid("5c552ab6-fc09-4cb3-8e36-22fa03c798b8")]
public unsafe partial interface IXCLRDataProcess2 : IXCLRDataProcess
{
    [PreserveSig]
    int GetGcNotification(GcEvtArgs* gcEvtArgs);
    [PreserveSig]
    int SetGcNotification(GcEvtArgs gcEvtArgs);
}

[GeneratedComInterface]
[Guid("E59D8D22-ADA7-49a2-89B5-A415AFCFC95F")]
public unsafe partial interface IXCLRDataStackWalk
{
    [PreserveSig]
    int GetContext(
        uint contextFlags,
        uint contextBufSize,
        uint* contextSize,
        [Out, MarshalUsing(CountElementName = nameof(contextBufSize))] byte[] contextBuf);

    [PreserveSig]
    int SetContext(uint contextSize, [In, MarshalUsing(CountElementName = nameof(contextSize))] byte[] context);

    [PreserveSig]
    int Next();

    [PreserveSig]
    int GetStackSizeSkipped(ulong* stackSizeSkipped);

    [PreserveSig]
    int GetFrameType(/*CLRDataSimpleFrameType*/ uint* simpleType, /*CLRDataDetailedFrameType*/ uint* detailedType);

    [PreserveSig]
    int GetFrame(DacComNullableByRef<IXCLRDataFrame> frame);

    [PreserveSig]
    int Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer);

    [PreserveSig]
    int SetContext2(uint flags, uint contextSize, [In, MarshalUsing(CountElementName = nameof(contextSize))] byte[] context);
}

[GeneratedComInterface]
[Guid("271498C2-4085-4766-BC3A-7F8ED188A173")]
public unsafe partial interface IXCLRDataFrame
{
    [PreserveSig]
    int GetFrameType(uint* simpleType, uint* detailedType);

    [PreserveSig]
    int GetContext(
        uint contextFlags,
        uint contextBufSize,
        uint* contextSize,
        [Out, MarshalUsing(CountElementName = nameof(contextBufSize))] byte[] contextBuf);

    [PreserveSig]
    int GetAppDomain(DacComNullableByRef<IXCLRDataAppDomain> appDomain);

    [PreserveSig]
    int GetNumArguments(uint* numArgs);

    [PreserveSig]
    int GetArgumentByIndex(
        uint index,
        DacComNullableByRef<IXCLRDataValue> arg,
        uint bufLen,
        uint* nameLen,
        char* name);

    [PreserveSig]
    int GetNumLocalVariables(uint* numLocals);

    [PreserveSig]
    int GetLocalVariableByIndex(
        uint index,
        DacComNullableByRef<IXCLRDataValue> localVariable,
        uint bufLen,
        uint* nameLen,
        char* name);

    [PreserveSig]
    int GetCodeName(
        uint flags,
        uint bufLen,
        uint* nameLen,
        char* nameBuf);

    [PreserveSig]
    int GetMethodInstance(DacComNullableByRef<IXCLRDataMethodInstance> method);

    [PreserveSig]
    int Request(
        uint reqCode,
        uint inBufferSize,
        byte* inBuffer,
        uint outBufferSize,
        byte* outBuffer);

    [PreserveSig]
    int GetNumTypeArguments(uint* numTypeArgs);

    [PreserveSig]
    int GetTypeArgumentByIndex(uint index, DacComNullableByRef<IXCLRDataTypeInstance> typeArg);
}

[GeneratedComInterface]
[Guid("1C4D9A4B-702D-4CF6-B290-1DB6F43050D0")]
public unsafe partial interface IXCLRDataFrame2
{
    [PreserveSig]
    int GetExactGenericArgsToken(DacComNullableByRef<IXCLRDataValue> genericToken);
}

[GeneratedComInterface]
[Guid("A5B0BEEA-EC62-4618-8012-A24FFC23934C")]
public unsafe partial interface IXCLRDataTask
{
    [PreserveSig]
    int GetProcess(/*IXCLRDataProcess*/ void** process);

    [PreserveSig]
    int GetCurrentAppDomain(DacComNullableByRef<IXCLRDataAppDomain> appDomain);

    [PreserveSig]
    int GetUniqueID(ulong* id);

    [PreserveSig]
    int GetFlags(uint* flags);

    [PreserveSig]
    int IsSameObject(IXCLRDataTask* task);

    [PreserveSig]
    int GetManagedObject(DacComNullableByRef<IXCLRDataValue> value);

    [PreserveSig]
    int GetDesiredExecutionState(uint* state);

    [PreserveSig]
    int SetDesiredExecutionState(uint state);

    [PreserveSig]
    int CreateStackWalk(uint flags, DacComNullableByRef<IXCLRDataStackWalk> stackWalk);

    [PreserveSig]
    int GetOSThreadID(uint* id);

    [PreserveSig]
    int GetContext(uint contextFlags, uint contextBufSize, uint* contextSize, byte* contextBuffer);

    [PreserveSig]
    int SetContext(uint contextSize, byte* context);

    [PreserveSig]
    int GetCurrentExceptionState(DacComNullableByRef<IXCLRDataExceptionState> exception);

    [PreserveSig]
    int Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer);

    [PreserveSig]
    int GetName(uint bufLen, uint* nameLen, char* nameBuffer);

    [PreserveSig]
    int GetLastExceptionState(DacComNullableByRef<IXCLRDataExceptionState> exception);
}

public enum ClrDataSourceType : uint
{
    CLRDATA_SOURCE_TYPE_INVALID = 0,
}

// CLRDATA_IL_ADDRESS_MAP
public struct ClrDataILAddressMap
{
    public uint ilOffset;
    public ClrDataAddress startAddress;
    public ClrDataAddress endAddress;
    public ClrDataSourceType type;
}

[GeneratedComInterface]
[Guid("ECD73800-22CA-4b0d-AB55-E9BA7E6318A5")]
public unsafe partial interface IXCLRDataMethodInstance
{
    [PreserveSig]
    int GetTypeInstance(DacComNullableByRef<IXCLRDataTypeInstance> typeInstance);

    [PreserveSig]
    int GetDefinition(DacComNullableByRef<IXCLRDataMethodDefinition> methodDefinition);

    [PreserveSig]
    int GetTokenAndScope(uint* token, DacComNullableByRef<IXCLRDataModule> mod);

    [PreserveSig]
    int GetName(
        uint flags,
        uint bufLen,
        uint* nameLen,
        char* nameBuf);

    [PreserveSig]
    int GetFlags(uint* flags);

    [PreserveSig]
    int IsSameObject(IXCLRDataMethodInstance* method);

    [PreserveSig]
    int GetEnCVersion(uint* version);

    [PreserveSig]
    int GetNumTypeArguments(uint* numTypeArgs);

    [PreserveSig]
    int GetTypeArgumentByIndex(uint index, DacComNullableByRef<IXCLRDataTypeInstance> typeArg);

    [PreserveSig]
    int GetILOffsetsByAddress(
        ClrDataAddress address,
        uint offsetsLen,
        uint* offsetsNeeded,
        uint* ilOffsets);

    [PreserveSig]
    int GetAddressRangesByILOffset(
        uint ilOffset,
        uint rangesLen,
        uint* rangesNeeded,
        /*CLRDATA_ADDRESS_RANGE* */ void* addressRanges);

    [PreserveSig]
    int GetILAddressMap(
        uint mapLen,
        uint* mapNeeded,
        [In, Out, MarshalUsing(CountElementName = nameof(mapLen))] ClrDataILAddressMap[]? maps);

    [PreserveSig]
    int StartEnumExtents(ulong* handle);

    [PreserveSig]
    int EnumExtent(ulong* handle, /*CLRDATA_ADDRESS_RANGE*/ void* extent);

    [PreserveSig]
    int EndEnumExtents(ulong handle);

    [PreserveSig]
    int Request(
        uint reqCode,
        uint inBufferSize,
        byte* inBuffer,
        uint outBufferSize,
        byte* outBuffer);

    [PreserveSig]
    int GetRepresentativeEntryAddress(ClrDataAddress* addr);
}

[GeneratedComInterface]
[Guid("7CA04601-C702-4670-A63C-FA44F7DA7BD5")]
public unsafe partial interface IXCLRDataAppDomain
{
    [PreserveSig]
    int GetProcess(DacComNullableByRef<IXCLRDataProcess> process);
    [PreserveSig]
    int GetName(uint bufLen, uint* nameLen, char* name);
    [PreserveSig]
    int GetUniqueID(ulong* id);
    [PreserveSig]
    int GetFlags(uint* flags);
    [PreserveSig]
    int IsSameObject(IXCLRDataAppDomain* appDomain);
    [PreserveSig]
    int GetManagedObject(DacComNullableByRef<IXCLRDataValue> value);
    [PreserveSig]
    int Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer);
}

[GeneratedComInterface]
[Guid("2FA17588-43C2-46ab-9B51-C8F01E39C9AC")]
public unsafe partial interface IXCLRDataAssembly
{
    [PreserveSig]
    int StartEnumModules(ulong* handle);
    [PreserveSig]
    int EnumModule(ulong* handle, DacComNullableByRef<IXCLRDataModule> mod);
    [PreserveSig]
    int EndEnumModules(ulong handle);

    [PreserveSig]
    int GetName(uint bufLen, uint* nameLen, char* name);
    [PreserveSig]
    int GetFileName(uint bufLen, uint* nameLen, char* name);
    [PreserveSig]
    int GetFlags(uint* flags);
    [PreserveSig]
    int IsSameObject(IXCLRDataAssembly? assembly);

    [PreserveSig]
    int Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer);

    [PreserveSig]
    int StartEnumAppDomains(ulong* handle);
    [PreserveSig]
    int EnumAppDomain(ulong* handle, /*IXCLRDataAppDomain*/ void** appDomain);
    [PreserveSig]
    int EndEnumAppDomains(ulong handle);

    [PreserveSig]
    int GetDisplayName(uint bufLen, uint* nameLen, char* name);
}

[GeneratedComInterface]
[Guid("4675666C-C275-45b8-9F6C-AB165D5C1E09")]
public unsafe partial interface IXCLRDataTypeDefinition
{
    [PreserveSig]
    int GetModule(DacComNullableByRef<IXCLRDataModule> mod);

    [PreserveSig]
    int StartEnumMethodDefinitions(ulong* handle);
    [PreserveSig]
    int EnumMethodDefinition(ulong* handle, DacComNullableByRef<IXCLRDataMethodDefinition> methodDefinition);
    [PreserveSig]
    int EndEnumMethodDefinitions(ulong handle);

    [PreserveSig]
    int StartEnumMethodDefinitionsByName(char* name, uint flags, ulong* handle);
    [PreserveSig]
    int EnumMethodDefinitionByName(ulong* handle, DacComNullableByRef<IXCLRDataMethodDefinition> method);
    [PreserveSig]
    int EndEnumMethodDefinitionsByName(ulong handle);

    [PreserveSig]
    int GetMethodDefinitionByToken(/*mdMethodDef*/ uint token, DacComNullableByRef<IXCLRDataMethodDefinition> methodDefinition);

    [PreserveSig]
    int StartEnumInstances(IXCLRDataAppDomain? appDomain, ulong* handle);
    [PreserveSig]
    int EnumInstance(ulong* handle, DacComNullableByRef<IXCLRDataTypeInstance> instance);
    [PreserveSig]
    int EndEnumInstances(ulong handle);

    [PreserveSig]
    int GetName(uint flags, uint bufLen, uint* nameLen, char* nameBuf);
    [PreserveSig]
    int GetTokenAndScope(/*mdTypeDef*/ uint* token, DacComNullableByRef<IXCLRDataModule> mod);
    [PreserveSig]
    int GetCorElementType(/*CorElementType*/ uint* type);
    [PreserveSig]
    int GetFlags(uint* flags);
    [PreserveSig]
    int IsSameObject(IXCLRDataTypeDefinition? type);

    [PreserveSig]
    int Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer);

    [PreserveSig]
    int GetArrayRank(uint* rank);
    [PreserveSig]
    int GetBase(DacComNullableByRef<IXCLRDataTypeDefinition> @base);
    [PreserveSig]
    int GetNumFields(uint flags, uint* numFields);

    [PreserveSig]
    int StartEnumFields(uint flags, ulong* handle);
    [PreserveSig]
    int EnumField(
        ulong* handle,
        uint nameBufLen,
        uint* nameLen,
        char* nameBuf,
        DacComNullableByRef<IXCLRDataTypeDefinition> type,
        uint* flags,
        /*mdFieldDef*/ uint* token);
    [PreserveSig]
    int EndEnumFields(ulong handle);

    [PreserveSig]
    int StartEnumFieldsByName(char* name, uint nameFlags, uint fieldFlags, ulong* handle);
    [PreserveSig]
    int EnumFieldByName(ulong* handle, DacComNullableByRef<IXCLRDataTypeDefinition> type, uint* flags, /*mdFieldDef*/ uint* token);
    [PreserveSig]
    int EndEnumFieldsByName(ulong handle);

    [PreserveSig]
    int GetFieldByToken(
        /*mdFieldDef*/ uint token,
        uint nameBufLen,
        uint* nameLen,
        char* nameBuf,
        DacComNullableByRef<IXCLRDataTypeDefinition> type,
        uint* flags);

    [PreserveSig]
    int GetTypeNotification(uint* flags);
    [PreserveSig]
    int SetTypeNotification(uint flags);

    [PreserveSig]
    int EnumField2(
        ulong* handle,
        uint nameBufLen,
        uint* nameLen,
        char* nameBuf,
        DacComNullableByRef<IXCLRDataTypeDefinition> type,
        uint* flags,
        DacComNullableByRef<IXCLRDataModule> tokenScope,
        /*mdFieldDef*/ uint* token);
    [PreserveSig]
    int EnumFieldByName2(
        ulong* handle,
        DacComNullableByRef<IXCLRDataTypeDefinition> type,
        uint* flags,
        DacComNullableByRef<IXCLRDataModule> tokenScope,
        /*mdFieldDef*/ uint* token);
    [PreserveSig]
    int GetFieldByToken2(
        IXCLRDataModule? tokenScope,
        /*mdFieldDef*/ uint token,
        uint nameBufLen,
        uint* nameLen,
        char* nameBuf,
        DacComNullableByRef<IXCLRDataTypeDefinition> type,
        uint* flags);
}

[GeneratedComInterface]
[Guid("4D078D91-9CB3-4b0d-97AC-28C8A5A82597")]
public unsafe partial interface IXCLRDataTypeInstance
{
    [PreserveSig]
    int StartEnumMethodInstances(ulong* handle);
    [PreserveSig]
    int EnumMethodInstance(ulong* handle, DacComNullableByRef<IXCLRDataMethodInstance> methodInstance);
    [PreserveSig]
    int EndEnumMethodInstances(ulong handle);

    [PreserveSig]
    int StartEnumMethodInstancesByName(char* name, uint flags, ulong* handle);
    [PreserveSig]
    int EnumMethodInstanceByName(ulong* handle, DacComNullableByRef<IXCLRDataMethodInstance> method);
    [PreserveSig]
    int EndEnumMethodInstancesByName(ulong handle);

    [PreserveSig]
    int GetNumStaticFields(uint* numFields);
    [PreserveSig]
    int GetStaticFieldByIndex(
        uint index,
        IXCLRDataTask? tlsTask,
        DacComNullableByRef<IXCLRDataValue> field,
        uint bufLen,
        uint* nameLen,
        char* nameBuf,
        /*mdFieldDef*/ uint* token);

    [PreserveSig]
    int StartEnumStaticFieldsByName(char* name, uint flags, IXCLRDataTask? tlsTask, ulong* handle);
    [PreserveSig]
    int EnumStaticFieldByName(ulong* handle, DacComNullableByRef<IXCLRDataValue> value);
    [PreserveSig]
    int EndEnumStaticFieldsByName(ulong handle);

    [PreserveSig]
    int GetNumTypeArguments(uint* numTypeArgs);
    [PreserveSig]
    int GetTypeArgumentByIndex(uint index, DacComNullableByRef<IXCLRDataTypeInstance> typeArg);

    [PreserveSig]
    int GetName(uint flags, uint bufLen, uint* nameLen, char* nameBuf);
    [PreserveSig]
    int GetModule(DacComNullableByRef<IXCLRDataModule> mod);
    [PreserveSig]
    int GetDefinition(DacComNullableByRef<IXCLRDataTypeDefinition> typeDefinition);
    [PreserveSig]
    int GetFlags(uint* flags);
    [PreserveSig]
    int IsSameObject(IXCLRDataTypeInstance? type);

    [PreserveSig]
    int Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer);

    [PreserveSig]
    int GetNumStaticFields2(uint flags, uint* numFields);

    [PreserveSig]
    int StartEnumStaticFields(uint flags, IXCLRDataTask? tlsTask, ulong* handle);
    [PreserveSig]
    int EnumStaticField(ulong* handle, DacComNullableByRef<IXCLRDataValue> value);
    [PreserveSig]
    int EndEnumStaticFields(ulong handle);

    [PreserveSig]
    int StartEnumStaticFieldsByName2(char* name, uint nameFlags, uint fieldFlags, IXCLRDataTask? tlsTask, ulong* handle);
    [PreserveSig]
    int EnumStaticFieldByName2(ulong* handle, DacComNullableByRef<IXCLRDataValue> value);
    [PreserveSig]
    int EndEnumStaticFieldsByName2(ulong handle);

    [PreserveSig]
    int GetStaticFieldByToken(
        /*mdFieldDef*/ uint token,
        IXCLRDataTask? tlsTask,
        DacComNullableByRef<IXCLRDataValue> field,
        uint bufLen,
        uint* nameLen,
        char* nameBuf);

    [PreserveSig]
    int GetBase(DacComNullableByRef<IXCLRDataTypeInstance> @base);

    [PreserveSig]
    int EnumStaticField2(
        ulong* handle,
        DacComNullableByRef<IXCLRDataValue> value,
        uint bufLen,
        uint* nameLen,
        char* nameBuf,
        DacComNullableByRef<IXCLRDataModule> tokenScope,
        /*mdFieldDef*/ uint* token);
    [PreserveSig]
    int EnumStaticFieldByName3(
        ulong* handle,
        DacComNullableByRef<IXCLRDataValue> value,
        DacComNullableByRef<IXCLRDataModule> tokenScope,
        /*mdFieldDef*/ uint* token);
    [PreserveSig]
    int GetStaticFieldByToken2(
        IXCLRDataModule? tokenScope,
        /*mdFieldDef*/ uint token,
        IXCLRDataTask? tlsTask,
        DacComNullableByRef<IXCLRDataValue> field,
        uint bufLen,
        uint* nameLen,
        char* nameBuf);
}

public struct ClrDataMethodDefinitionExtent
{
    public ClrDataAddress startAddress;
    public ClrDataAddress endAddress;
    public uint enCVersion;
    public uint /* CLRDataMethodDefinitionExtentType */ type;
}

[GeneratedComInterface]
[Guid("AAF60008-FB2C-420b-8FB1-42D244A54A97")]
public unsafe partial interface IXCLRDataMethodDefinition
{
    [PreserveSig]
    int GetTypeDefinition(DacComNullableByRef<IXCLRDataTypeDefinition> typeDefinition);

    [PreserveSig]
    int StartEnumInstances(IXCLRDataAppDomain? appDomain, ulong* handle);
    [PreserveSig]
    int EnumInstance(ulong* handle, DacComNullableByRef<IXCLRDataMethodInstance> instance);
    [PreserveSig]
    int EndEnumInstances(ulong handle);

    [PreserveSig]
    int GetName(uint flags, uint bufLen, uint* nameLen, char* name);
    [PreserveSig]
    int GetTokenAndScope(/*mdMethodDef*/ uint* token, DacComNullableByRef<IXCLRDataModule> mod);
    [PreserveSig]
    int GetFlags(uint* flags);
    [PreserveSig]
    int IsSameObject(IXCLRDataMethodDefinition? method);
    [PreserveSig]
    int GetLatestEnCVersion(uint* version);

    [PreserveSig]
    int StartEnumExtents(ulong* handle);
    [PreserveSig]
    int EnumExtent(ulong* handle, ClrDataMethodDefinitionExtent* extent);
    [PreserveSig]
    int EndEnumExtents(ulong handle);

    [PreserveSig]
    int GetCodeNotification(uint* flags);
    [PreserveSig]
    int SetCodeNotification(uint flags);

    [PreserveSig]
    int Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer);

    [PreserveSig]
    int GetRepresentativeEntryAddress(ClrDataAddress* addr);
    [PreserveSig]
    int HasClassOrMethodInstantiation(int* bGeneric);
}

public enum CLRDataGeneralRequest : uint
{
    CLRDATA_REQUEST_REVISION = 0xe0000000,
}

[Flags]
public enum CLRDataExceptionStateFlag : uint
{
    CLRDATA_EXCEPTION_DEFAULT = 0,
    CLRDATA_EXCEPTION_NESTED = 0x1,
    CLRDATA_EXCEPTION_PARTIAL = 0x2,
}

[GeneratedComInterface]
[Guid("75DA9E4C-BD33-43C8-8F5C-96E8A5241F57")]
public unsafe partial interface IXCLRDataExceptionState
{
    [PreserveSig]
    int GetFlags(uint* flags);
    [PreserveSig]
    int GetPrevious(DacComNullableByRef<IXCLRDataExceptionState> exState);
    [PreserveSig]
    int GetManagedObject(DacComNullableByRef<IXCLRDataValue> value);
    [PreserveSig]
    int GetBaseType(/*CLRDataBaseExceptionType*/ uint* type);
    [PreserveSig]
    int GetCode(uint* code);
    [PreserveSig]
    int GetString(uint bufLen, uint* strLen, char* str);

    [PreserveSig]
    int Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer);

    [PreserveSig]
    int IsSameState(EXCEPTION_RECORD64* exRecord, uint contextSize, byte* cxRecord);
    [PreserveSig]
    int IsSameState2(uint flags, EXCEPTION_RECORD64* exRecord, uint contextSize, byte* cxRecord);
    [PreserveSig]
    int GetTask(DacComNullableByRef<IXCLRDataTask> task);
}

[Flags]
public enum ClrDataValueFlag : uint
{
    DEFAULT = 0x00000000,
    IS_PRIMITIVE = 0x00000001,
    IS_VALUE_TYPE = 0x00000002,
    IS_STRING = 0x00000004,
    IS_ARRAY = 0x00000008,
    IS_REFERENCE = 0x00000010,
    IS_POINTER = 0x00000020,
    IS_ENUM = 0x00000040,
}

public static class ClrDataVLocFlag
{
    public const uint CLRDATA_VLOC_MEMORY = 0x00;
    public const uint CLRDATA_VLOC_REGISTER = 0x01;
}

[GeneratedComInterface]
[Guid("96EC93C7-1000-4e93-8991-98D8766E6666")]
public unsafe partial interface IXCLRDataValue
{
    [PreserveSig]
    int GetFlags(uint* flags);
    [PreserveSig]
    int GetAddress(ClrDataAddress* address);
    [PreserveSig]
    int GetSize(ulong* size);

    [PreserveSig]
    int GetBytes(uint bufLen, uint* dataSize, byte* buffer);
    [PreserveSig]
    int SetBytes(uint bufLen, uint* dataSize, byte* buffer);

    [PreserveSig]
    int GetType(DacComNullableByRef<IXCLRDataTypeInstance> typeInstance);

    [PreserveSig]
    int GetNumFields(uint* numFields);
    [PreserveSig]
    int GetFieldByIndex(
        uint index,
        DacComNullableByRef<IXCLRDataValue> field,
        uint bufLen,
        uint* nameLen,
        char* nameBuf,
        /*mdFieldDef*/ uint* token);

    [PreserveSig]
    int Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer);

    [PreserveSig]
    int GetNumFields2(uint flags, IXCLRDataTypeInstance? fromType, uint* numFields);

    [PreserveSig]
    int StartEnumFields(uint flags, IXCLRDataTypeInstance? fromType, ulong* handle);
    [PreserveSig]
    int EnumField(
        ulong* handle,
        DacComNullableByRef<IXCLRDataValue> field,
        uint nameBufLen,
        uint* nameLen,
        char* nameBuf,
        /*mdFieldDef*/ uint* token);
    [PreserveSig]
    int EndEnumFields(ulong handle);

    [PreserveSig]
    int StartEnumFieldsByName(char* name, uint nameFlags, uint fieldFlags, IXCLRDataTypeInstance? fromType, ulong* handle);
    [PreserveSig]
    int EnumFieldByName(ulong* handle, DacComNullableByRef<IXCLRDataValue> field, /*mdFieldDef*/ uint* token);
    [PreserveSig]
    int EndEnumFieldsByName(ulong handle);

    [PreserveSig]
    int GetFieldByToken(
        /*mdFieldDef*/ uint token,
        DacComNullableByRef<IXCLRDataValue> field,
        uint bufLen,
        uint* nameLen,
        char* nameBuf);

    [PreserveSig]
    int GetAssociatedValue(DacComNullableByRef<IXCLRDataValue> assocValue);
    [PreserveSig]
    int GetAssociatedType(DacComNullableByRef<IXCLRDataTypeInstance> assocType);

    [PreserveSig]
    int GetString(uint bufLen, uint* strLen, char* str);

    [PreserveSig]
    int GetArrayProperties(uint* rank, uint* totalElements, uint numDim, uint* dims, uint numBases, int* bases);
    [PreserveSig]
    int GetArrayElement(uint numInd, int* indices, DacComNullableByRef<IXCLRDataValue> value);

    [PreserveSig]
    int EnumField2(
        ulong* handle,
        DacComNullableByRef<IXCLRDataValue> field,
        uint nameBufLen,
        uint* nameLen,
        char* nameBuf,
        DacComNullableByRef<IXCLRDataModule> tokenScope,
        /*mdFieldDef*/ uint* token);
    [PreserveSig]
    int EnumFieldByName2(
        ulong* handle,
        DacComNullableByRef<IXCLRDataValue> field,
        DacComNullableByRef<IXCLRDataModule> tokenScope,
        /*mdFieldDef*/ uint* token);
    [PreserveSig]
    int GetFieldByToken2(
        IXCLRDataModule? tokenScope,
        /*mdFieldDef*/ uint token,
        DacComNullableByRef<IXCLRDataValue> field,
        uint bufLen,
        uint* nameLen,
        char* nameBuf);

    [PreserveSig]
    int GetNumLocations(uint* numLocs);
    [PreserveSig]
    int GetLocationByIndex(uint loc, uint* flags, ClrDataAddress* arg);
}

[GeneratedComInterface]
[Guid("2D95A079-42A1-4837-818F-0B97D7048E0E")]
public unsafe partial interface IXCLRDataExceptionNotification
{
    [PreserveSig]
    int OnCodeGenerated(IXCLRDataMethodInstance? method);
    [PreserveSig]
    int OnCodeDiscarded(IXCLRDataMethodInstance? method);
    [PreserveSig]
    int OnProcessExecution(uint state);
    [PreserveSig]
    int OnTaskExecution(/*IXCLRDataTask*/ void* task, uint state);
    [PreserveSig]
    int OnModuleLoaded(IXCLRDataModule? mod);
    [PreserveSig]
    int OnModuleUnloaded(IXCLRDataModule? mod);
    [PreserveSig]
    int OnTypeLoaded(/*IXCLRDataTypeInstance*/ void* typeInst);
    [PreserveSig]
    int OnTypeUnloaded(/*IXCLRDataTypeInstance*/ void* typeInst);
}

[GeneratedComInterface]
[Guid("31201a94-4337-49b7-aef7-0c755054091f")]
public unsafe partial interface IXCLRDataExceptionNotification2 : IXCLRDataExceptionNotification
{
    [PreserveSig]
    int OnAppDomainLoaded(/*IXCLRDataAppDomain*/ void* domain);
    [PreserveSig]
    int OnAppDomainUnloaded(/*IXCLRDataAppDomain*/ void* domain);
    [PreserveSig]
    int OnException(IXCLRDataExceptionState? exception);
}

[GeneratedComInterface]
[Guid("31201a94-4337-49b7-aef7-0c7550540920")]
public unsafe partial interface IXCLRDataExceptionNotification3 : IXCLRDataExceptionNotification2
{
    [PreserveSig]
    int OnGcEvent(GcEvtArgs gcEvtArgs);
}

[GeneratedComInterface]
[Guid("C25E926E-5F09-4AA2-BBAD-B7FC7F10CFD7")]
public unsafe partial interface IXCLRDataExceptionNotification4 : IXCLRDataExceptionNotification3
{
    [PreserveSig]
    int ExceptionCatcherEnter(IXCLRDataMethodInstance? catchingMethod, uint catcherNativeOffset);
}

[GeneratedComInterface]
[Guid("e77a39ea-3548-44d9-b171-8569ed1a9423")]
public unsafe partial interface IXCLRDataExceptionNotification5 : IXCLRDataExceptionNotification4
{
    [PreserveSig]
    int OnCodeGenerated2(IXCLRDataMethodInstance? method, ClrDataAddress nativeCodeLocation);
}

// IXCLRDataTarget3 extends ICLRDataTarget2 which extends ICLRDataTarget (defined in ICLRData.cs).
// See src/coreclr/inc/xclrdata.idl
[GeneratedComInterface]
[Guid("59d9b5e1-4a6f-4531-84c3-51d12da22fd4")]
public unsafe partial interface IXCLRDataTarget3 : ICLRDataTarget2
{
    [PreserveSig]
    int GetMetaData(
        char* imagePath,
        uint imageTimestamp,
        uint imageSize,
        Guid* mvid,
        uint mdRva,
        uint flags,
        uint bufferSize,
        byte* buffer,
        uint* dataSize);
}

[GeneratedComInterface]
[Guid("E5F3039D-2C0C-4230-A69E-12AF1C3E563C")]
public unsafe partial interface IXCLRLibrarySupport
{
    [PreserveSig]
    int LoadHardboundDependency(char* name, Guid* mvid, nuint* loadedBase);
    [PreserveSig]
    int LoadSoftboundDependency(char* name, byte* assemblymetadataBinding, byte* hash, uint hashLength, nuint* loadedBase);
}

// IXCLRDisassemblySupport and IXCLRDataDisplay are omitted because they use
// varargs (...) and non-HRESULT return types (SIZE_T, BOOL, void*) that are
// not expressible with [GeneratedComInterface]. These are NativeImageDumper
// tooling interfaces and are not needed by the cDAC diagnostic path.
