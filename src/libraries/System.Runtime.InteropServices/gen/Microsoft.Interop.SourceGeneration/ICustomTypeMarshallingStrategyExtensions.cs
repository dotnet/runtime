// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop
{
    internal static class ICustomTypeMarshallingStrategyExtensions
    {
        public static IEnumerable<ExpressionStatementSyntax> GenerateDefaultAssignOutStatement(this ICustomTypeMarshallingStrategy _, TypePositionInfo info, AssignOutContext context)
        {
            if (MarshallerHelpers.MarshalsOutToLocal(info, context))
            {
                var (_, native) = context.GetIdentifiers(info);
                return ImmutableArray.Create(MarshallerHelpers.GenerateAssignmentToPointerValue(context.ParameterIdentifier, native));
            }
            return ImmutableArray<ExpressionStatementSyntax>.Empty;
        }
    }
}
