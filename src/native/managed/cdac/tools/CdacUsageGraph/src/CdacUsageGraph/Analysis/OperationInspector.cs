// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace CdacUsageGraph.Analysis;

/// <summary>Stateless helpers for inspecting <see cref="IOperation"/> shapes.</summary>
internal static class OperationInspector
{
    /// <summary>Strips implicit conversion wrappers.</summary>
    public static IOperation Unwrap(IOperation op)
    {
        while (op is IConversionOperation { Operand: { } operand })
            op = operand;
        return op;
    }

}
