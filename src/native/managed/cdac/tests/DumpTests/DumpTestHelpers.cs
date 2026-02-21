// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Helpers for resolving method names and other metadata from cDAC contracts.
/// </summary>
internal static class DumpTestHelpers
{
    /// <summary>
    /// Resolves the method name for a <see cref="MethodDescHandle"/> using the
    /// RuntimeTypeSystem, Loader, and EcmaMetadata contracts. Returns <c>null</c>
    /// if the name cannot be resolved (e.g., missing metadata).
    /// </summary>
    public static string? GetMethodName(ContractDescriptorTarget target, MethodDescHandle mdHandle)
    {
        IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;

        if (rts.IsNoMetadataMethod(mdHandle, out string dynamicName))
            return dynamicName;

        uint token = rts.GetMethodToken(mdHandle);
        TargetPointer mt = rts.GetMethodTable(mdHandle);
        TargetPointer modulePtr = rts.GetModule(rts.GetTypeHandle(mt));

        ILoader loader = target.Contracts.Loader;
        ModuleHandle moduleHandle = loader.GetModuleHandleFromModulePtr(modulePtr);

        IEcmaMetadata ecmaMetadata = target.Contracts.EcmaMetadata;
        MetadataReader? reader = ecmaMetadata.GetMetadata(moduleHandle);
        if (reader is null)
            return null;

        MethodDefinitionHandle methodDef = MetadataTokens.MethodDefinitionHandle((int)(token & 0x00FFFFFF));

        return reader.GetString(reader.GetMethodDefinition(methodDef).Name);
    }

    /// <summary>
    /// Resolves the method name for a stack frame's MethodDesc pointer.
    /// Returns <c>null</c> if the frame has no MethodDesc or the name cannot be resolved.
    /// </summary>
    public static string? GetMethodName(ContractDescriptorTarget target, TargetPointer methodDescPtr)
    {
        if (methodDescPtr == TargetPointer.Null)
            return null;

        MethodDescHandle mdHandle = target.Contracts.RuntimeTypeSystem.GetMethodDescHandle(methodDescPtr);

        return GetMethodName(target, mdHandle);
    }

    /// <summary>
    /// Finds the thread that called FailFast by walking each thread's stack and looking
    /// for a frame whose method name contains "FailFast". Asserts if no such thread is found.
    /// </summary>
    public static ThreadData FindFailFastThread(ContractDescriptorTarget target)
    {
        IThread threadContract = target.Contracts.Thread;
        IStackWalk stackWalk = target.Contracts.StackWalk;
        ThreadStoreData storeData = threadContract.GetThreadStoreData();

        TargetPointer currentThreadPtr = storeData.FirstThread;
        while (currentThreadPtr != TargetPointer.Null)
        {
            ThreadData threadData = threadContract.GetThreadData(currentThreadPtr);

            foreach (IStackDataFrameHandle frame in stackWalk.CreateStackWalk(threadData))
            {
                TargetPointer methodDescPtr = stackWalk.GetMethodDescPtr(frame);
                string? name = GetMethodName(target, methodDescPtr);
                if (name is not null && name.Contains("FailFast"))
                    return threadData;
            }

            currentThreadPtr = threadData.NextThread;
        }

        Assert.Fail("Could not find a thread with FailFast on the stack");
        return default;
    }
}
