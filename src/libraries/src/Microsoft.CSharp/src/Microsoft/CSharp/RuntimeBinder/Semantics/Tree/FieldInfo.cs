// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprFieldInfo : ExprWithType
    {
        public ExprFieldInfo(FieldSymbol field, AggregateType fieldType, CType type)
            : base(ExpressionKind.FieldInfo, type)
        {
            Debug.Assert(field != null);
            Debug.Assert(fieldType != null);
            Field = field;
            FieldType = fieldType;
        }

        public FieldSymbol Field { get; }

        public AggregateType FieldType { get; }
    }
}
