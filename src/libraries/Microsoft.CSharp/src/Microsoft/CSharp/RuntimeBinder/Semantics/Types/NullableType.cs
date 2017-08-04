// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CSharp.RuntimeBinder.Errors;

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
        private AggregateType ats;
        public BSYMMGR symmgr;
        public TypeManager typeManager;

        public AggregateType GetAts()
        {
            if (ats == null)
            {
                AggregateSymbol aggNullable = typeManager.GetNullable();
                CType typePar = GetUnderlyingType();
                CType[] typeParArray = { typePar };
                TypeArray ta = symmgr.AllocParams(1, typeParArray);
                ats = typeManager.GetAggregate(aggNullable, ta);
            }

            return ats;
        }
        public CType GetUnderlyingType() { return UnderlyingType; }

        public override CType StripNubs() => UnderlyingType;

        public override CType StripNubs(out bool wasNullable)
        {
            wasNullable = true;
            return UnderlyingType;
        }

        public void SetUnderlyingType(CType pType) { UnderlyingType = pType; }

        public CType UnderlyingType;
    }
}
