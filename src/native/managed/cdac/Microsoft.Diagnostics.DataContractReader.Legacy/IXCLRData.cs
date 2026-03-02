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

[GeneratedComInterface]
[Guid("88E32849-0A0A-4cb0-9022-7CD2E9E139E2")]
public unsafe partial interface IXCLRDataModule
{
    [PreserveSig]
    int StartEnumAssemblies(ulong* handle);
    [PreserveSig]
    int EnumAssembly(ulong* handle, /*IXCLRDataAssembly*/ void** assembly);
    [PreserveSig]
    int EndEnumAssemblies(ulong handle);

    [PreserveSig]
    int StartEnumTypeDefinitions(ulong* handle);
    [PreserveSig]
    int EnumTypeDefinition(ulong* handle, /*IXCLRDataTypeDefinition*/ void** typeDefinition);
    [PreserveSig]
    int EndEnumTypeDefinitions(ulong handle);

    [PreserveSig]
    int StartEnumTypeInstances(IXCLRDataAppDomain* appDomain, ulong* handle);
    [PreserveSig]
    int EnumTypeInstance(ulong* handle, /*IXCLRDataTypeInstance*/ void** typeInstance);
    [PreserveSig]
    int EndEnumTypeInstances(ulong handle);

    [PreserveSig]
    int StartEnumTypeDefinitionsByName(char* name, uint flags, ulong* handle);
    [PreserveSig]
    int EnumTypeDefinitionByName(ulong* handle, /*IXCLRDataTypeDefinition*/ void** type);
    [PreserveSig]
    int EndEnumTypeDefinitionsByName(ulong handle);

    [PreserveSig]
    int StartEnumTypeInstancesByName(char* name, uint flags, IXCLRDataAppDomain* appDomain, ulong* handle);
    [PreserveSig]
    int EnumTypeInstanceByName(ulong* handle, /*IXCLRDataTypeInstance*/ void** type);
    [PreserveSig]
    int EndEnumTypeInstancesByName(ulong handle);

    [PreserveSig]
    int GetTypeDefinitionByToken(/*mdTypeDef*/ uint token, /*IXCLRDataTypeDefinition*/ void** typeDefinition);

    [PreserveSig]
    int StartEnumMethodDefinitionsByName(char* name, uint flags, ulong* handle);
    [PreserveSig]
    int EnumMethodDefinitionByName(ulong* handle, /*IXCLRDataMethodDefinition*/ void** method);
    [PreserveSig]
    int EndEnumMethodDefinitionsByName(ulong handle);

    [PreserveSig]
    int StartEnumMethodInstancesByName(char* name, uint flags, IXCLRDataAppDomain* appDomain, ulong* handle);
    [PreserveSig]
    int EnumMethodInstanceByName(ulong* handle, out IXCLRDataMethodInstance? method);
    [PreserveSig]
    int EndEnumMethodInstancesByName(ulong handle);

    [PreserveSig]
    int GetMethodDefinitionByToken(/*mdMethodDef*/ uint token, /*IXCLRDataMethodDefinition*/ void** methodDefinition);

    [PreserveSig]
    int StartEnumDataByName(char* name, uint flags, IXCLRDataAppDomain* appDomain, /*IXCLRDataTask*/ void* tlsTask, ulong* handle);
    [PreserveSig]
    int EnumDataByName(ulong* handle, /*IXCLRDataValue*/ void** value);
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
    int EnumAppDomain(ulong* handle, IXCLRDataAppDomain** appDomain);
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
    int EnumTask(ulong* handle, /*IXCLRDataTask*/ void** task);
    [PreserveSig]
    int EndEnumTasks(ulong handle);

    [PreserveSig]
    int GetTaskByOSThreadID(uint osThreadID, out IXCLRDataTask? task);
    [PreserveSig]
    int GetTaskByUniqueID(ulong taskID, /*IXCLRDataTask*/ void** task);

    [PreserveSig]
    int GetFlags(uint* flags);

    [PreserveSig]
    int IsSameObject(IXCLRDataProcess* process);

    [PreserveSig]
    int GetManagedObject(/*IXCLRDataValue*/ void** value);

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
    int EnumAppDomain(ulong* handle, IXCLRDataAppDomain** appDomain);
    [PreserveSig]
    int EndEnumAppDomains(ulong handle);
    [PreserveSig]
    int GetAppDomainByUniqueID(ulong id, IXCLRDataAppDomain** appDomain);

    [PreserveSig]
    int StartEnumAssemblies(ulong* handle);
    [PreserveSig]
    int EnumAssembly(ulong* handle, /*IXCLRDataAssembly*/ void** assembly);
    [PreserveSig]
    int EndEnumAssemblies(ulong handle);

    [PreserveSig]
    int StartEnumModules(ulong* handle);
    [PreserveSig]
    int EnumModule(ulong* handle, /*IXCLRDataModule*/ void** mod);
    [PreserveSig]
    int EndEnumModules(ulong handle);
    [PreserveSig]
    int GetModuleByAddress(ClrDataAddress address, /*IXCLRDataModule*/ void** mod);

    [PreserveSig]
    int StartEnumMethodInstancesByAddress(ClrDataAddress address, IXCLRDataAppDomain* appDomain, ulong* handle);
    [PreserveSig]
    int EnumMethodInstanceByAddress(ulong* handle, out IXCLRDataMethodInstance? method);
    [PreserveSig]
    int EndEnumMethodInstancesByAddress(ulong handle);

    [PreserveSig]
    int GetDataByAddress(
        ClrDataAddress address,
        uint flags,
        IXCLRDataAppDomain* appDomain,
        /*IXCLRDataTask*/ void* tlsTask,
        uint bufLen,
        uint* nameLen,
        char* nameBuf,
        /*IXCLRDataValue*/ void** value,
        ClrDataAddress* displacement);

    [PreserveSig]
    int GetExceptionStateByExceptionRecord(/*struct EXCEPTION_RECORD64*/ void* record, /*IXCLRDataExceptionState*/ void** exState);
    [PreserveSig]
    int TranslateExceptionRecordToNotification(/*struct EXCEPTION_RECORD64*/ void* record, /*IXCLRDataExceptionNotification*/ void* notify);

    [PreserveSig]
    int Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer);

    [PreserveSig]
    int CreateMemoryValue(
        IXCLRDataAppDomain* appDomain,
        /*IXCLRDataTask*/ void* tlsTask,
        /*IXCLRDataTypeInstance*/ void* type,
        ClrDataAddress addr,
        /*IXCLRDataValue*/ void** value);

    [PreserveSig]
    int SetAllTypeNotifications(/*IXCLRDataModule*/ void* mod, uint flags);
    [PreserveSig]
    int SetAllCodeNotifications(/*IXCLRDataModule*/ void* mod, uint flags);
    [PreserveSig]
    int GetTypeNotifications(
        uint numTokens,
        /*IXCLRDataModule*/ void** mods,
        /*IXCLRDataModule*/ void* singleMod,
        [In, MarshalUsing(CountElementName = nameof(numTokens))] /*mdTypeDef*/ uint[] tokens,
        [In, Out, MarshalUsing(CountElementName = nameof(numTokens))] uint[] flags);
    [PreserveSig]
    int SetTypeNotifications(
        uint numTokens,
        /*IXCLRDataModule*/ void** mods,
        /*IXCLRDataModule*/ void* singleMod,
        [In, MarshalUsing(CountElementName = nameof(numTokens))] /*mdTypeDef*/ uint[] tokens,
        [In, MarshalUsing(CountElementName = nameof(numTokens))] uint[] flags,
        uint singleFlags);
    [PreserveSig]
    int GetCodeNotifications(
        uint numTokens,
        /*IXCLRDataModule*/ void** mods,
        /*IXCLRDataModule*/ void* singleMod,
        [In, MarshalUsing(CountElementName = nameof(numTokens))] /*mdMethodDef*/ uint[] tokens,
        [In, Out, MarshalUsing(CountElementName = nameof(numTokens))] uint[] flags);
    [PreserveSig]
    int SetCodeNotifications(
        uint numTokens,
        /*IXCLRDataModule*/ void** mods,
        /*IXCLRDataModule*/ void* singleMod,
        [In, MarshalUsing(CountElementName = nameof(numTokens))] /*mdMethodDef */ uint[] tokens,
        [In, MarshalUsing(CountElementName = nameof(numTokens))] uint[] flags,
        uint singleFlags);
    [PreserveSig]
    int GetOtherNotificationFlags(uint* flags);
    [PreserveSig]
    int SetOtherNotificationFlags(uint flags);

    [PreserveSig]
    int StartEnumMethodDefinitionsByAddress(ClrDataAddress address, ulong* handle);
    [PreserveSig]
    int EnumMethodDefinitionByAddress(ulong* handle, /*IXCLRDataMethodDefinition*/ void** method);
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
        /*IXCLRDataTask*/ void* task,
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
[Guid("7CA04601-C702-4670-A63C-FA44F7DA7BD5")]
public unsafe partial interface IXCLRDataAppDomain
{
    [PreserveSig]
    int GetProcess(IXCLRDataProcess** process);

    [PreserveSig]
    int GetName(uint bufLen, uint* nameLen, char* name);

    [PreserveSig]
    int GetUniqueID(ulong* id);

    [PreserveSig]
    int GetFlags(uint* flags);

    [PreserveSig]
    int IsSameObject(IXCLRDataAppDomain* appDomain);

    [PreserveSig]
    int GetManagedObject(/*IXCLRDataValue*/ void** value);

    [PreserveSig]
    int Request(uint reqCode,
                uint inBufferSize,
                [In, MarshalUsing(CountElementName = nameof(inBufferSize))] byte[] inBuffer,
                uint outBufferSize,
                [Out, MarshalUsing(CountElementName = nameof(outBufferSize))] byte[] outBuffer);
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
    int GetFrame(out IXCLRDataFrame? frame);

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
    int GetAppDomain(/*IXCLRDataAppDomain*/ void** appDomain);

    [PreserveSig]
    int GetNumArguments(uint* numArgs);

    [PreserveSig]
    int GetArgumentByIndex(
        uint index,
        /*IXCLRDataValue*/ void** arg,
        uint bufLen,
        uint* nameLen,
        char* name);

    [PreserveSig]
    int GetNumLocalVariables(uint* numLocals);

    [PreserveSig]
    int GetLocalVariableByIndex(
        uint index,
        /*IXCLRDataValue*/ void** localVariable,
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
    int GetMethodInstance(out IXCLRDataMethodInstance? method);

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
    int GetTypeArgumentByIndex(uint index, /*IXCLRDataTypeInstance*/ void** typeArg);
}

[GeneratedComInterface]
[Guid("1C4D9A4B-702D-4CF6-B290-1DB6F43050D0")]
public unsafe partial interface IXCLRDataFrame2
{
    [PreserveSig]
    int GetExactGenericArgsToken(/*IXCLRDataValue*/ void** genericToken);
}

[GeneratedComInterface]
[Guid("A5B0BEEA-EC62-4618-8012-A24FFC23934C")]
public unsafe partial interface IXCLRDataTask
{
    [PreserveSig]
    int GetProcess(/*IXCLRDataProcess*/ void** process);

    [PreserveSig]
    int GetCurrentAppDomain(out IXCLRDataAppDomain? appDomain);

    [PreserveSig]
    int GetUniqueID(ulong* id);

    [PreserveSig]
    int GetFlags(uint* flags);

    [PreserveSig]
    int IsSameObject(IXCLRDataTask* task);

    [PreserveSig]
    int GetManagedObject(/*IXCLRDataValue*/ void** value);

    [PreserveSig]
    int GetDesiredExecutionState(uint* state);

    [PreserveSig]
    int SetDesiredExecutionState(uint state);

    [PreserveSig]
    int CreateStackWalk(uint flags, out IXCLRDataStackWalk? stackWalk);

    [PreserveSig]
    int GetOSThreadID(uint* id);

    [PreserveSig]
    int GetContext(uint contextFlags, uint contextBufSize, uint* contextSize, byte* contextBuffer);

    [PreserveSig]
    int SetContext(uint contextSize, byte* context);

    [PreserveSig]
    int GetCurrentExceptionState(/*IXCLRDataExceptionState*/ void** exception);

    [PreserveSig]
    int Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer);

    [PreserveSig]
    int GetName(uint bufLen, uint* nameLen, char* nameBuffer);

    [PreserveSig]
    int GetLastExceptionState(/*IXCLRDataExceptionState*/ void** exception);
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
    int GetTypeInstance(/*IXCLRDataTypeInstance*/ void** typeInstance);

    [PreserveSig]
    int GetDefinition(/*IXCLRDataMethodDefinition*/ void** methodDefinition);

    [PreserveSig]
    int GetTokenAndScope(uint* token, void** /*IXCLRDataModule*/ mod);

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
    int GetTypeArgumentByIndex(uint index, /*IXCLRDataTypeInstance*/ void** typeArg);

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
    int GetProcess(/*IXCLRDataProcess*/ void** process);
    [PreserveSig]
    int GetName(uint bufLen, uint* nameLen, char* name);
    [PreserveSig]
    int GetUniqueID(ulong* id);
    [PreserveSig]
    int GetFlags(uint* flags);
    [PreserveSig]
    int IsSameObject(/*IXCLRDataAppDomain*/ void* appDomain);
    [PreserveSig]
    int GetManagedObject(/*IXCLRDataValue*/ void** value);
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
    int EnumModule(ulong* handle, /*IXCLRDataModule*/ void** mod);
    [PreserveSig]
    int EndEnumModules(ulong handle);

    [PreserveSig]
    int GetName(uint bufLen, uint* nameLen, char* name);
    [PreserveSig]
    int GetFileName(uint bufLen, uint* nameLen, char* name);
    [PreserveSig]
    int GetFlags(uint* flags);
    [PreserveSig]
    int IsSameObject(/*IXCLRDataAssembly*/ void* assembly);

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
    int GetModule(/*IXCLRDataModule*/ void** mod);

    [PreserveSig]
    int StartEnumMethodDefinitions(ulong* handle);
    [PreserveSig]
    int EnumMethodDefinition(ulong* handle, /*IXCLRDataMethodDefinition*/ void** methodDefinition);
    [PreserveSig]
    int EndEnumMethodDefinitions(ulong handle);

    [PreserveSig]
    int StartEnumMethodDefinitionsByName(char* name, uint flags, ulong* handle);
    [PreserveSig]
    int EnumMethodDefinitionByName(ulong* handle, /*IXCLRDataMethodDefinition*/ void** method);
    [PreserveSig]
    int EndEnumMethodDefinitionsByName(ulong handle);

    [PreserveSig]
    int GetMethodDefinitionByToken(/*mdMethodDef*/ uint token, /*IXCLRDataMethodDefinition*/ void** methodDefinition);

    [PreserveSig]
    int StartEnumInstances(/*IXCLRDataAppDomain*/ void* appDomain, ulong* handle);
    [PreserveSig]
    int EnumInstance(ulong* handle, /*IXCLRDataTypeInstance*/ void** instance);
    [PreserveSig]
    int EndEnumInstances(ulong handle);

    [PreserveSig]
    int GetName(uint flags, uint bufLen, uint* nameLen, char* nameBuf);
    [PreserveSig]
    int GetTokenAndScope(/*mdTypeDef*/ uint* token, /*IXCLRDataModule*/ void** mod);
    [PreserveSig]
    int GetCorElementType(/*CorElementType*/ uint* type);
    [PreserveSig]
    int GetFlags(uint* flags);
    [PreserveSig]
    int IsSameObject(/*IXCLRDataTypeDefinition*/ void* type);

    [PreserveSig]
    int Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer);

    [PreserveSig]
    int GetArrayRank(uint* rank);
    [PreserveSig]
    int GetBase(/*IXCLRDataTypeDefinition*/ void** @base);
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
        /*IXCLRDataTypeDefinition*/ void** type,
        uint* flags,
        /*mdFieldDef*/ uint* token);
    [PreserveSig]
    int EndEnumFields(ulong handle);

    [PreserveSig]
    int StartEnumFieldsByName(char* name, uint nameFlags, uint fieldFlags, ulong* handle);
    [PreserveSig]
    int EnumFieldByName(ulong* handle, /*IXCLRDataTypeDefinition*/ void** type, uint* flags, /*mdFieldDef*/ uint* token);
    [PreserveSig]
    int EndEnumFieldsByName(ulong handle);

    [PreserveSig]
    int GetFieldByToken(
        /*mdFieldDef*/ uint token,
        uint nameBufLen,
        uint* nameLen,
        char* nameBuf,
        /*IXCLRDataTypeDefinition*/ void** type,
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
        /*IXCLRDataTypeDefinition*/ void** type,
        uint* flags,
        /*IXCLRDataModule*/ void** tokenScope,
        /*mdFieldDef*/ uint* token);
    [PreserveSig]
    int EnumFieldByName2(
        ulong* handle,
        /*IXCLRDataTypeDefinition*/ void** type,
        uint* flags,
        /*IXCLRDataModule*/ void** tokenScope,
        /*mdFieldDef*/ uint* token);
    [PreserveSig]
    int GetFieldByToken2(
        /*IXCLRDataModule*/ void* tokenScope,
        /*mdFieldDef*/ uint token,
        uint nameBufLen,
        uint* nameLen,
        char* nameBuf,
        /*IXCLRDataTypeDefinition*/ void** type,
        uint* flags);
}

[GeneratedComInterface]
[Guid("4D078D91-9CB3-4b0d-97AC-28C8A5A82597")]
public unsafe partial interface IXCLRDataTypeInstance
{
    [PreserveSig]
    int StartEnumMethodInstances(ulong* handle);
    [PreserveSig]
    int EnumMethodInstance(ulong* handle, /*IXCLRDataMethodInstance*/ void** methodInstance);
    [PreserveSig]
    int EndEnumMethodInstances(ulong handle);

    [PreserveSig]
    int StartEnumMethodInstancesByName(char* name, uint flags, ulong* handle);
    [PreserveSig]
    int EnumMethodInstanceByName(ulong* handle, /*IXCLRDataMethodInstance*/ void** method);
    [PreserveSig]
    int EndEnumMethodInstancesByName(ulong handle);

    [PreserveSig]
    int GetNumStaticFields(uint* numFields);
    [PreserveSig]
    int GetStaticFieldByIndex(
        uint index,
        /*IXCLRDataTask*/ void* tlsTask,
        /*IXCLRDataValue*/ void** field,
        uint bufLen,
        uint* nameLen,
        char* nameBuf,
        /*mdFieldDef*/ uint* token);

    [PreserveSig]
    int StartEnumStaticFieldsByName(char* name, uint flags, /*IXCLRDataTask*/ void* tlsTask, ulong* handle);
    [PreserveSig]
    int EnumStaticFieldByName(ulong* handle, /*IXCLRDataValue*/ void** value);
    [PreserveSig]
    int EndEnumStaticFieldsByName(ulong handle);

    [PreserveSig]
    int GetNumTypeArguments(uint* numTypeArgs);
    [PreserveSig]
    int GetTypeArgumentByIndex(uint index, /*IXCLRDataTypeInstance*/ void** typeArg);

    [PreserveSig]
    int GetName(uint flags, uint bufLen, uint* nameLen, char* nameBuf);
    [PreserveSig]
    int GetModule(/*IXCLRDataModule*/ void** mod);
    [PreserveSig]
    int GetDefinition(/*IXCLRDataTypeDefinition*/ void** typeDefinition);
    [PreserveSig]
    int GetFlags(uint* flags);
    [PreserveSig]
    int IsSameObject(/*IXCLRDataTypeInstance*/ void* type);

    [PreserveSig]
    int Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer);

    [PreserveSig]
    int GetNumStaticFields2(uint flags, uint* numFields);

    [PreserveSig]
    int StartEnumStaticFields(uint flags, /*IXCLRDataTask*/ void* tlsTask, ulong* handle);
    [PreserveSig]
    int EnumStaticField(ulong* handle, /*IXCLRDataValue*/ void** value);
    [PreserveSig]
    int EndEnumStaticFields(ulong handle);

    [PreserveSig]
    int StartEnumStaticFieldsByName2(char* name, uint nameFlags, uint fieldFlags, /*IXCLRDataTask*/ void* tlsTask, ulong* handle);
    [PreserveSig]
    int EnumStaticFieldByName2(ulong* handle, /*IXCLRDataValue*/ void** value);
    [PreserveSig]
    int EndEnumStaticFieldsByName2(ulong handle);

    [PreserveSig]
    int GetStaticFieldByToken(
        /*mdFieldDef*/ uint token,
        /*IXCLRDataTask*/ void* tlsTask,
        /*IXCLRDataValue*/ void** field,
        uint bufLen,
        uint* nameLen,
        char* nameBuf);

    [PreserveSig]
    int GetBase(/*IXCLRDataTypeInstance*/ void** @base);

    [PreserveSig]
    int EnumStaticField2(
        ulong* handle,
        /*IXCLRDataValue*/ void** value,
        uint bufLen,
        uint* nameLen,
        char* nameBuf,
        /*IXCLRDataModule*/ void** tokenScope,
        /*mdFieldDef*/ uint* token);
    [PreserveSig]
    int EnumStaticFieldByName3(
        ulong* handle,
        /*IXCLRDataValue*/ void** value,
        /*IXCLRDataModule*/ void** tokenScope,
        /*mdFieldDef*/ uint* token);
    [PreserveSig]
    int GetStaticFieldByToken2(
        /*IXCLRDataModule*/ void* tokenScope,
        /*mdFieldDef*/ uint token,
        /*IXCLRDataTask*/ void* tlsTask,
        /*IXCLRDataValue*/ void** field,
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
    int GetTypeDefinition(/*IXCLRDataTypeDefinition*/ void** typeDefinition);

    [PreserveSig]
    int StartEnumInstances(/*IXCLRDataAppDomain*/ void* appDomain, ulong* handle);
    [PreserveSig]
    int EnumInstance(ulong* handle, /*IXCLRDataMethodInstance*/ void** instance);
    [PreserveSig]
    int EndEnumInstances(ulong handle);

    [PreserveSig]
    int GetName(uint flags, uint bufLen, uint* nameLen, char* name);
    [PreserveSig]
    int GetTokenAndScope(/*mdMethodDef*/ uint* token, /*IXCLRDataModule*/ void** mod);
    [PreserveSig]
    int GetFlags(uint* flags);
    [PreserveSig]
    int IsSameObject(/*IXCLRDataMethodDefinition*/ void* method);
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

[GeneratedComInterface]
[Guid("75DA9E4C-BD33-43C8-8F5C-96E8A5241F57")]
public unsafe partial interface IXCLRDataExceptionState
{
    [PreserveSig]
    int GetFlags(uint* flags);
    [PreserveSig]
    int GetPrevious(/*IXCLRDataExceptionState*/ void** exState);
    [PreserveSig]
    int GetManagedObject(/*IXCLRDataValue*/ void** value);
    [PreserveSig]
    int GetBaseType(/*CLRDataBaseExceptionType*/ uint* type);
    [PreserveSig]
    int GetCode(uint* code);
    [PreserveSig]
    int GetString(uint bufLen, uint* strLen, char* str);

    [PreserveSig]
    int Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer);

    [PreserveSig]
    int IsSameState(/*EXCEPTION_RECORD64*/ void* exRecord, uint contextSize, byte* cxRecord);
    [PreserveSig]
    int IsSameState2(uint flags, /*EXCEPTION_RECORD64*/ void* exRecord, uint contextSize, byte* cxRecord);
    [PreserveSig]
    int GetTask(/*IXCLRDataTask*/ void** task);
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
    int GetType(/*IXCLRDataTypeInstance*/ void** typeInstance);

    [PreserveSig]
    int GetNumFields(uint* numFields);
    [PreserveSig]
    int GetFieldByIndex(
        uint index,
        /*IXCLRDataValue*/ void** field,
        uint bufLen,
        uint* nameLen,
        char* nameBuf,
        /*mdFieldDef*/ uint* token);

    [PreserveSig]
    int Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer);

    [PreserveSig]
    int GetNumFields2(uint flags, /*IXCLRDataTypeInstance*/ void* fromType, uint* numFields);

    [PreserveSig]
    int StartEnumFields(uint flags, /*IXCLRDataTypeInstance*/ void* fromType, ulong* handle);
    [PreserveSig]
    int EnumField(
        ulong* handle,
        /*IXCLRDataValue*/ void** field,
        uint nameBufLen,
        uint* nameLen,
        char* nameBuf,
        /*mdFieldDef*/ uint* token);
    [PreserveSig]
    int EndEnumFields(ulong handle);

    [PreserveSig]
    int StartEnumFieldsByName(char* name, uint nameFlags, uint fieldFlags, /*IXCLRDataTypeInstance*/ void* fromType, ulong* handle);
    [PreserveSig]
    int EnumFieldByName(ulong* handle, /*IXCLRDataValue*/ void** field, /*mdFieldDef*/ uint* token);
    [PreserveSig]
    int EndEnumFieldsByName(ulong handle);

    [PreserveSig]
    int GetFieldByToken(
        /*mdFieldDef*/ uint token,
        /*IXCLRDataValue*/ void** field,
        uint bufLen,
        uint* nameLen,
        char* nameBuf);

    [PreserveSig]
    int GetAssociatedValue(/*IXCLRDataValue*/ void** assocValue);
    [PreserveSig]
    int GetAssociatedType(/*IXCLRDataTypeInstance*/ void** assocType);

    [PreserveSig]
    int GetString(uint bufLen, uint* strLen, char* str);

    [PreserveSig]
    int GetArrayProperties(uint* rank, uint* totalElements, uint numDim, uint* dims, uint numBases, int* bases);
    [PreserveSig]
    int GetArrayElement(uint numInd, int* indices, /*IXCLRDataValue*/ void** value);

    [PreserveSig]
    int EnumField2(
        ulong* handle,
        /*IXCLRDataValue*/ void** field,
        uint nameBufLen,
        uint* nameLen,
        char* nameBuf,
        /*IXCLRDataModule*/ void** tokenScope,
        /*mdFieldDef*/ uint* token);
    [PreserveSig]
    int EnumFieldByName2(
        ulong* handle,
        /*IXCLRDataValue*/ void** field,
        /*IXCLRDataModule*/ void** tokenScope,
        /*mdFieldDef*/ uint* token);
    [PreserveSig]
    int GetFieldByToken2(
        /*IXCLRDataModule*/ void* tokenScope,
        /*mdFieldDef*/ uint token,
        /*IXCLRDataValue*/ void** field,
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
    int OnCodeGenerated(IXCLRDataMethodInstance* method);
    [PreserveSig]
    int OnCodeDiscarded(IXCLRDataMethodInstance* method);
    [PreserveSig]
    int OnProcessExecution(uint state);
    [PreserveSig]
    int OnTaskExecution(/*IXCLRDataTask*/ void* task, uint state);
    [PreserveSig]
    int OnModuleLoaded(/*IXCLRDataModule*/ void* mod);
    [PreserveSig]
    int OnModuleUnloaded(/*IXCLRDataModule*/ void* mod);
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
    int OnException(/*IXCLRDataExceptionState*/ void* exception);
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
    int ExceptionCatcherEnter(IXCLRDataMethodInstance* catchingMethod, uint catcherNativeOffset);
}

[GeneratedComInterface]
[Guid("e77a39ea-3548-44d9-b171-8569ed1a9423")]
public unsafe partial interface IXCLRDataExceptionNotification5 : IXCLRDataExceptionNotification4
{
    [PreserveSig]
    int OnCodeGenerated2(IXCLRDataMethodInstance* method, ClrDataAddress nativeCodeLocation);
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
