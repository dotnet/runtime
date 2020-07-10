// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprClass : ExprWithType
    {
        public ExprClass(CType type)
            : base(ExpressionKind.Class, type)
        {
            Debug.Assert(type != null);
        }
    }
}
