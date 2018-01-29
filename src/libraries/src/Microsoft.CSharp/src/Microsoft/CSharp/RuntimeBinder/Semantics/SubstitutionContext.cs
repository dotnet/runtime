// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class SubstContext
    {
        public readonly CType[] ClassTypes;
        public readonly CType[] MethodTypes;
        public readonly bool DenormMeth;

        public SubstContext(TypeArray typeArgsCls, TypeArray typeArgsMeth, bool denormMeth)
        {
            typeArgsCls?.AssertValid();
            ClassTypes = typeArgsCls?.Items ?? Array.Empty<CType>();
            typeArgsMeth?.AssertValid();
            MethodTypes = typeArgsMeth?.Items ?? Array.Empty<CType>();
            DenormMeth = denormMeth;
        }

        public SubstContext(AggregateType type)
            : this(type, null, false)
        {
        }

        public SubstContext(AggregateType type, TypeArray typeArgsMeth)
            : this(type, typeArgsMeth, false)
        {
        }

        private SubstContext(AggregateType type, TypeArray typeArgsMeth, bool denormMeth)
            : this(type?.TypeArgsAll, typeArgsMeth, denormMeth)
        {
        }

        public bool IsNop => ClassTypes.Length == 0 & MethodTypes.Length == 0;
    }
}
