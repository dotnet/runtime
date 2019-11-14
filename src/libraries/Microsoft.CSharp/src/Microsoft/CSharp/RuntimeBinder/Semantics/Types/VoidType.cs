// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    // VoidType - represents the type "void".
    // ----------------------------------------------------------------------------

    internal sealed class VoidType : CType
    {
        public static readonly VoidType Instance = new VoidType();

        private VoidType()
            : base(TypeKind.TK_VoidType)
        {
        }

        public override bool IsPredefType(PredefinedType pt) => pt == PredefinedType.PT_VOID;
    }
}
