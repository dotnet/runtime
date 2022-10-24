// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    // Copied from https://github.com/dotnet/roslyn/blob/fdd40b21d59c13e8fa6c718c7aaf9d50634da754/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/Core/Extensions/BasicBlockExtensions.cs
    internal static partial class BasicBlockExtensions
    {
        public static IEnumerable<IOperation> DescendantOperations(this BasicBlock basicBlock)
        {
            foreach (var statement in basicBlock.Operations)
            {
                foreach (var operation in statement.DescendantsAndSelf())
                {
                    yield return operation;
                }
            }

            if (basicBlock.BranchValue != null)
            {
                foreach (var operation in basicBlock.BranchValue.DescendantsAndSelf())
                {
                    yield return operation;
                }
            }
        }
    }
}
