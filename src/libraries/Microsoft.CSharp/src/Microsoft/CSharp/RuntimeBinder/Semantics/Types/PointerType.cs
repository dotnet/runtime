// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class PointerType : CType
    {
        public PointerType(CType referentType)
            : base(TypeKind.TK_PointerType)
        {
            ReferentType = referentType;
        }

        public CType ReferentType { get; }

        public override bool IsUnsafe() => true;

        public override Type AssociatedSystemType
        {
            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            get => ReferentType.AssociatedSystemType.MakePointerType();
        }

        public override CType BaseOrParameterOrElementType => ReferentType;

        public override FUNDTYPE FundamentalType => FUNDTYPE.FT_PTR;

        [ExcludeFromCodeCoverage(Justification = "Dynamic code can't contain constant pointers")]
        public override ConstValKind ConstValKind
        {
            get
            {
                Debug.Fail("Constant of pointer in dynamic code?");
                return ConstValKind.IntPtr;
            }
        }
    }
}
