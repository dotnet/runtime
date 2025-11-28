// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Contracts.Extensions;

public static class IExecutionManagerExtensions
{
    public static bool IsFunclet(this IExecutionManager eman, CodeBlockHandle codeBlockHandle)
    {
        return eman.GetStartAddress(codeBlockHandle) != eman.GetFuncletStartAddress(codeBlockHandle);
    }
}
