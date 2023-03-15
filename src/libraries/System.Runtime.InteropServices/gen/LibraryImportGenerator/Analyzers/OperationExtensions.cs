// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.Interop.Analyzers
{
    internal static class OperationExtensions
    {
        public static IArgumentOperation? GetArgumentByOrdinal(this IInvocationOperation operation, int ordinal)
        {
            if (operation.TargetMethod.Parameters.Length <= ordinal)
            {
                throw new ArgumentOutOfRangeException(nameof(ordinal));
            }

            foreach (IArgumentOperation argument in operation.Arguments)
            {
                if (argument.Parameter?.Ordinal == ordinal)
                {
                    return argument;
                }
            }
            return null;
        }
        public static IArgumentOperation? GetArgumentByOrdinal(this IObjectCreationOperation operation, int ordinal)
        {
            if (operation.Constructor.Parameters.Length <= ordinal)
            {
                throw new ArgumentOutOfRangeException(nameof(ordinal));
            }

            foreach (IArgumentOperation argument in operation.Arguments)
            {
                if (argument.Parameter?.Ordinal == ordinal)
                {
                    return argument;
                }
            }
            return null;
        }

        public static bool IsNullLiteralOperation(this IOperation operation)
        {
            return operation is { ConstantValue: { HasValue: true, Value: null } };
        }
    }
}
