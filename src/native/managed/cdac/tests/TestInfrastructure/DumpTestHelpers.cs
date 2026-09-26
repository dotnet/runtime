// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.TestInfrastructure;

/// <summary>
/// Helpers for resolving method names and other metadata from cDAC contracts.
/// </summary>
public static class DumpTestHelpers
{
    /// <summary>
    /// Resolves the unqualified method name for a <see cref="MethodDescHandle"/> using
    /// <see cref="ISOSDacInterface.GetMethodDescName"/>.
    /// </summary>
    public static unsafe string? GetMethodName(ContractDescriptorTarget target, MethodDescHandle mdHandle)
    {
        ISOSDacInterface sosDac = new SOSDacImpl(target, legacyObj: null, new());
        ClrDataAddress methodDesc = mdHandle.Address.ToClrDataAddress(target);
        uint requiredLength;
        int hr = sosDac.GetMethodDescName(methodDesc, 0, null, &requiredLength);
        if (hr < 0 || requiredLength <= 1)
            return null;

        char[] nameBuffer = new char[requiredLength];
        fixed (char* name = nameBuffer)
        {
            hr = sosDac.GetMethodDescName(methodDesc, requiredLength, name, &requiredLength);
        }

        if (hr < 0 || requiredLength <= 1)
            return null;

        return GetSimpleMethodName(new string(nameBuffer, 0, checked((int)requiredLength - 1)));
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
    /// Finds a thread that has a frame whose method name contains the given
    /// <paramref name="methodNameSubstring"/>. Asserts if no such thread is found.
    /// </summary>
    public static ThreadData FindThreadWithMethod(ContractDescriptorTarget target, string methodNameSubstring)
    {
        IThread threadContract = target.Contracts.Thread;
        IStackWalk stackWalk = target.Contracts.StackWalk;
        ThreadStoreData storeData = threadContract.GetThreadStoreData();

        TargetPointer currentThreadPtr = storeData.FirstThread;
        while (currentThreadPtr != TargetPointer.Null)
        {
            ThreadData threadData = threadContract.GetThreadData(currentThreadPtr);

            foreach (IStackDataFrameHandle frame in DumpTestStackWalker.LegacyVisibleFrames(stackWalk, threadData))
            {
                TargetPointer methodDescPtr = stackWalk.GetMethodDescPtr(frame);
                string? name = GetMethodName(target, methodDescPtr);
                if (name is not null && name.Contains(methodNameSubstring))
                    return threadData;
            }

            currentThreadPtr = threadData.NextThread;
        }

        Assert.Fail($"Could not find a thread with '{methodNameSubstring}' on the stack");
        return default;
    }

    /// <summary>
    /// Finds the thread that called FailFast by walking each thread's stack and looking
    /// for a frame whose method name contains "FailFast". Asserts if no such thread is found.
    /// </summary>
    public static ThreadData FindFailFastThread(ContractDescriptorTarget target)
    {
        return FindThreadWithMethod(target, "FailFast");
    }

    /// <summary>
    /// Extracts the unqualified method name without generic arguments or parameter types
    /// from a name formatted by <see cref="ISOSDacInterface.GetMethodDescName"/>.
    /// </summary>
    public static string GetSimpleMethodName(string formattedName)
    {
        ReadOnlySpan<char> name = formattedName;
        int parametersStart = name.IndexOf('(');
        if (parametersStart >= 0)
            name = name[..parametersStart];

        if (name.EndsWith("]", StringComparison.Ordinal))
        {
            int depth = 0;
            for (int i = name.Length - 1; i >= 0; i--)
            {
                if (name[i] == ']')
                    depth++;
                else if (name[i] == '[' && --depth == 0)
                {
                    name = name[..i];
                    break;
                }
            }
        }

        int methodStart = name.LastIndexOf('.') + 1;
        // Constructors have a leading dot in addition to the declaring-type separator.
        if (methodStart > 0 && name[methodStart..] is "ctor" or "cctor" &&
            (methodStart == 1 || name[methodStart - 2] == '.'))
        {
            methodStart--;
        }

        return name[methodStart..].ToString();
    }
}
