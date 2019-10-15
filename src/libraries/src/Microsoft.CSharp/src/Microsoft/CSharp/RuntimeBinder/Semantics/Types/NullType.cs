// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    // NullType - represents the null type -- the type of the "null constant".
    // ----------------------------------------------------------------------------

    internal sealed class NullType : CType
    {
        public static readonly NullType Instance = new NullType();

        private NullType()
            : base(TypeKind.TK_NullType)
        {
        }

        public override bool IsReferenceType => true;

        public override FUNDTYPE FundamentalType => FUNDTYPE.FT_REF;

        public override ConstValKind ConstValKind => ConstValKind.IntPtr;
    }
}
