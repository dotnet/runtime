// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CSharp.RuntimeBinder.Errors;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    //
    // NullableType
    //
    // A "derived" type representing Nullable<T>. The base type T is the parent.
    //
    // ----------------------------------------------------------------------------

    internal class NullableType : CType
    {
        public AggregateType ats;
        public BSYMMGR symmgr;
        public TypeManager typeManager;

        public AggregateType GetAts(ErrorHandling errorContext)
        {
            AggregateSymbol aggNullable = typeManager.GetNullable();
            if (aggNullable == null)
            {
                throw Error.InternalCompilerError();
            }

            if (ats == null)
            {
                if (aggNullable == null)
                {
                    typeManager.ReportMissingPredefTypeError(errorContext, PredefinedType.PT_G_OPTIONAL);
                    return null;
                }

                CType typePar = GetUnderlyingType();
                CType[] typeParArray = new CType[] { typePar };
                TypeArray ta = symmgr.AllocParams(1, typeParArray);
                ats = typeManager.GetAggregate(aggNullable, ta);
            }
            return ats;
        }
        public CType GetUnderlyingType() { return UnderlyingType; }
        public void SetUnderlyingType(CType pType) { UnderlyingType = pType; }

        public CType UnderlyingType;
    }
}
