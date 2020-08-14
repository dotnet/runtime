// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    // ArgumentListType - a placeholder typesym used only as the type of a C-style varargs.
    // There is exactly one of these.
    // ----------------------------------------------------------------------------

    internal sealed class ArgumentListType : CType
    {
        public static readonly ArgumentListType Instance = new ArgumentListType();

        private ArgumentListType()
            : base(TypeKind.TK_ArgumentListType)
        {
        }
    }
}
