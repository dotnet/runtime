// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal abstract class ExprWithType : Expr
    {
        protected ExprWithType(ExpressionKind kind, CType type)
            : base(kind)
        {
            Type = type;
        }

        protected static bool TypesAreEqual(Type t1, Type t2) => t1 == t2 || t1.IsEquivalentTo(t2);
    }
}
