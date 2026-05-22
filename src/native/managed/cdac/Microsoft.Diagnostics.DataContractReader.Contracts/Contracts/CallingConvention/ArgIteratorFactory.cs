// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.CallingConventionHelpers;

/// <summary>
/// Factory to create the appropriate per-arch <see cref="ArgIteratorBase"/>
/// subclass for the target architecture.
/// </summary>
internal static class ArgIteratorFactory
{
    public static ArgIteratorBase Create(
        TransitionBlockLayout layout,
        ArgIteratorData argData,
        bool hasParamType,
        bool hasAsyncContinuation)
    {
        return layout.Architecture switch
        {
            RuntimeInfoArchitecture.X64 => layout.OperatingSystem != RuntimeInfoOperatingSystem.Windows
                ? new AMD64UnixArgIterator(
                    layout, argData, hasParamType, hasAsyncContinuation)
                : new AMD64WindowsArgIterator(
                    layout, argData, hasParamType, hasAsyncContinuation),
            _ => throw new NotSupportedException(layout.Architecture.ToString()),
        };
    }
}
