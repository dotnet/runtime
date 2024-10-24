// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

// This file contains managed declarations for the IXCLRData interfaces.
// See src/coreclr/inc/xclrdata.idl

[GeneratedComInterface]
[Guid("88E32849-0A0A-4cb0-9022-7CD2E9E139E2")]
internal unsafe partial interface IXCLRDataModule
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
    int StartEnumTypeInstances(/*IXCLRDataAppDomain*/ void* appDomain, ulong* handle);
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
    int StartEnumTypeInstancesByName(char* name, uint flags, /*IXCLRDataAppDomain*/ void* appDomain, ulong* handle);
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
    int StartEnumMethodInstancesByName(char* name, uint flags, /*IXCLRDataAppDomain*/ void* appDomain, ulong* handle);
    [PreserveSig]
    int EnumMethodInstanceByName(ulong* handle, /*IXCLRDataMethodInstance*/ void** method);
    [PreserveSig]
    int EndEnumMethodInstancesByName(ulong handle);

    [PreserveSig]
    int GetMethodDefinitionByToken(/*mdMethodDef*/ uint token, /*IXCLRDataMethodDefinition*/ void** methodDefinition);

    [PreserveSig]
    int StartEnumDataByName(char* name, uint flags, /*IXCLRDataAppDomain*/ void* appDomain, /*IXCLRDataTask*/ void* tlsTask, ulong* handle);
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
    int EnumAppDomain(ulong* handle, /*IXCLRDataAppDomain*/ void** appDomain);
    [PreserveSig]
    int EndEnumAppDomains(ulong handle);

    [PreserveSig]
    int GetVersionId(Guid* vid);
}

[GeneratedComInterface]
[Guid("34625881-7EB3-4524-817B-8DB9D064C760")]
internal unsafe partial interface IXCLRDataModule2
{
    [PreserveSig]
    int SetJITCompilerFlags(uint flags);
}

[GeneratedComInterface]
[Guid("5c552ab6-fc09-4cb3-8e36-22fa03c798b7")]
internal unsafe partial interface IXCLRDataProcess
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
    int GetTaskByOSThreadID(uint osThreadID, /*IXCLRDataTask*/ void** task);
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
    int GetAddressType(ulong address, /*CLRDataAddressType*/ uint* type);

    [PreserveSig]
    int GetRuntimeNameByAddress(
        ulong address,
        uint flags,
        uint bufLen,
        uint* nameLen,
        char* nameBuf,
        ulong* displacement);

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
    int GetModuleByAddress(ulong address, /*IXCLRDataModule*/ void** mod);

    [PreserveSig]
    int StartEnumMethodInstancesByAddress(ulong address, /*IXCLRDataAppDomain*/ void* appDomain, ulong* handle);
    [PreserveSig]
    int EnumMethodInstanceByAddress(ulong* handle, /*IXCLRDataMethodInstance*/ void** method);
    [PreserveSig]
    int EndEnumMethodInstancesByAddress(ulong handle);

    [PreserveSig]
    int GetDataByAddress(
        ulong address,
        uint flags,
        /*IXCLRDataAppDomain*/ void* appDomain,
        /*IXCLRDataTask*/ void* tlsTask,
        uint bufLen,
        uint* nameLen,
        char* nameBuf,
        /*IXCLRDataValue*/ void** value,
        ulong* displacement);

    [PreserveSig]
    int GetExceptionStateByExceptionRecord(/*struct EXCEPTION_RECORD64*/ void* record, /*IXCLRDataExceptionState*/ void** exState);
    [PreserveSig]
    int TranslateExceptionRecordToNotification(/*struct EXCEPTION_RECORD64*/ void* record, /*IXCLRDataExceptionNotification*/ void* notify);

    [PreserveSig]
    int Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer);

    [PreserveSig]
    int CreateMemoryValue(
        /*IXCLRDataAppDomain*/ void* appDomain,
        /*IXCLRDataTask*/ void* tlsTask,
        /*IXCLRDataTypeInstance*/ void* type,
        ulong addr,
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
    int StartEnumMethodDefinitionsByAddress(ulong address, ulong* handle);
    [PreserveSig]
    int EnumMethodDefinitionByAddress(ulong* handle, /*IXCLRDataMethodDefinition*/ void** method);
    [PreserveSig]
    int EndEnumMethodDefinitionsByAddress(ulong handle);

    [PreserveSig]
    int FollowStub(
        uint inFlags,
        ulong inAddr,
        /*struct CLRDATA_FOLLOW_STUB_BUFFER*/ void* inBuffer,
        ulong* outAddr,
        /*struct CLRDATA_FOLLOW_STUB_BUFFER*/ void* outBuffer,
        uint* outFlags);
    [PreserveSig]
    int FollowStub2(
        /*IXCLRDataTask*/ void* task,
        uint inFlags,
        ulong inAddr,
        /*struct CLRDATA_FOLLOW_STUB_BUFFER*/ void* inBuffer,
        ulong* outAddr,
        /*struct CLRDATA_FOLLOW_STUB_BUFFER*/ void* outBuffer,
        uint* outFlags);

    [PreserveSig]
    int DumpNativeImage(
        ulong loadedBase,
        char* name,
        /*IXCLRDataDisplay*/ void* display,
        /*IXCLRLibrarySupport*/ void* libSupport,
        /*IXCLRDisassemblySupport*/ void* dis);
}

internal struct GcEvtArgs
{
    public /*GcEvt_t*/ uint type;
    public int condemnedGeneration;
}

[GeneratedComInterface]
[Guid("5c552ab6-fc09-4cb3-8e36-22fa03c798b8")]
internal unsafe partial interface IXCLRDataProcess2 : IXCLRDataProcess
{
    [PreserveSig]
    int GetGcNotification(GcEvtArgs* gcEvtArgs);
    [PreserveSig]
    int SetGcNotification(GcEvtArgs gcEvtArgs);
}
