// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private readonly BSYMMGR _symmgr;
        private readonly TypeManager _typeManager;

        public NullableType(CType underlyingType, BSYMMGR symmgr, TypeManager typeManager)
            : base(TypeKind.TK_NullableType)
        {
            UnderlyingType = underlyingType;
            _symmgr = symmgr;
            _typeManager = typeManager;
        }

        public AggregateType GetAts() => _ats ?? (_ats = _typeManager.GetAggregate(
                                             _typeManager.GetNullable(), _symmgr.AllocParams(UnderlyingType)));

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

        public override Type AssociatedSystemType => typeof(Nullable<>).MakeGenericType(UnderlyingType.AssociatedSystemType);

        public override CType BaseOrParameterOrElementType => UnderlyingType;

        public override FUNDTYPE FundamentalType => FUNDTYPE.FT_STRUCT;

        [ExcludeFromCodeCoverage] // Should be unreachable. Overload exists just to catch it being hit during debug.
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
