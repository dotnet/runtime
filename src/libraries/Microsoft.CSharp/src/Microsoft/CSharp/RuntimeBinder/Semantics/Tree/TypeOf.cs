// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprTypeOf : ExprWithType
    {
        public ExprTypeOf(CType type, CType sourceType)
            : base(ExpressionKind.TypeOf, type)
        {
            Flags = EXPRFLAG.EXF_CANTBENULL;
            SourceType = sourceType;
        }

        public CType SourceType { get; }
    }
}
