// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;

namespace Microsoft.Interop.JavaScript
{
    internal record JSMarshallingInfo(MarshallingInfo Inner, JSTypeInfo TypeInfo) : MarshallingInfo
    {
        protected JSMarshallingInfo()
            :this(NoMarshallingInfo.Instance, new JSInvalidTypeInfo())
        {
            Inner = null;
        }

        public JSTypeFlags JSType { get; init; }
        public JSTypeFlags[] JSTypeArguments { get; init; }

        private ImmutableArray<TypePositionInfo> _elementDependencies = ImmutableArray<TypePositionInfo>.Empty;
        public override IEnumerable<TypePositionInfo> ElementDependencies => _elementDependencies;

        public JSMarshallingInfo AddElementDependencies(IEnumerable<TypePositionInfo> elementDependencies)
        {
            return this with { _elementDependencies = _elementDependencies.AddRange(elementDependencies) };
        }

        public virtual bool Equals(JSMarshallingInfo? other)
        {
            return other is not null
                && EqualityContract == other.EqualityContract
                && Inner == other.Inner
                && TypeInfo == other.TypeInfo
                && JSType == other.JSType
                && (JSTypeArguments is null
                    ? other.JSTypeArguments is null
                    : other.JSTypeArguments is not null && JSTypeArguments.SequenceEqual(other.JSTypeArguments))
                && _elementDependencies.SequenceEqual(other._elementDependencies);
        }

        public override int GetHashCode()
        {
            throw new UnreachableException();
        }
    }

    internal sealed record JSMissingMarshallingInfo : JSMarshallingInfo
    {
        public JSMissingMarshallingInfo(JSTypeInfo typeInfo)
        {
            JSType = JSTypeFlags.Missing;
            TypeInfo = typeInfo;
        }
    }
}
