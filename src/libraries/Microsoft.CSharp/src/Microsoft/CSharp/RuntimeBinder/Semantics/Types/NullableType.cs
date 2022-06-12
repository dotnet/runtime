// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    //
    // NullableType
    //
    // A "derived" type representing Nullable<T>. The base type T is the parent.
    //
    // ----------------------------------------------------------------------------

    internal sealed class NullableType : CType
    {
        private AggregateType _ats;

        public NullableType(CType underlyingType)
            : base(TypeKind.TK_NullableType)
        {
            UnderlyingType = underlyingType;
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        public override AggregateType GetAts() =>
            _ats ??= TypeManager.GetAggregate(TypeManager.GetNullable(), TypeArray.Allocate(UnderlyingType));

        public override CType StripNubs() => UnderlyingType;

        public override CType StripNubs(out bool wasNullable)
        {
            wasNullable = true;
            return UnderlyingType;
        }

        public CType UnderlyingType { get; }

        public override bool IsValueType => true;

        public override bool IsStructOrEnum => true;

        public override bool IsStructType => true;

        public override Type AssociatedSystemType
        {
            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            get => typeof(Nullable<>).MakeGenericType(UnderlyingType.AssociatedSystemType);
        }

        public override CType BaseOrParameterOrElementType => UnderlyingType;

        public override FUNDTYPE FundamentalType => FUNDTYPE.FT_STRUCT;

        [ExcludeFromCodeCoverage(Justification = "Should be unreachable. Overload exists just to catch it being hit during debug.")]
        public override ConstValKind ConstValKind
        {
            get
            {
                Debug.Fail("Constant nullable?");
                return ConstValKind.Decimal; // Equivalent to previous code, so least change for this unreachable branch.
            }
        }
    }
}
