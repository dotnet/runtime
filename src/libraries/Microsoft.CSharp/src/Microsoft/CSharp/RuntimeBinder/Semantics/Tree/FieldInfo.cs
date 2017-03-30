// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprFieldInfo : Expr
    {
        public ExprFieldInfo(FieldSymbol f, AggregateType ft)
        {
            Field = f;
            FieldType = ft;
        }

        public FieldSymbol Field { get; }

        public AggregateType FieldType { get; }
    }
}
