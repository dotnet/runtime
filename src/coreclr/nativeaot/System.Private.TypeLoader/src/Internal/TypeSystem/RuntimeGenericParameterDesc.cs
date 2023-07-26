// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    internal class RuntimeGenericParameterDesc : GenericParameterDesc
    {
        private readonly GenericParameterKind _kind;
        private readonly int _index;
        private readonly GenericVariance _variance;
        private readonly TypeSystemEntity _associatedTypeOrMethod;

        public RuntimeGenericParameterDesc(GenericParameterKind kind, int index, TypeSystemEntity associatedTypeOrMethod, GenericVariance variance)
        {
            _kind = kind;
            _index = index;
            _associatedTypeOrMethod = associatedTypeOrMethod;
            _variance = variance;
        }

        public override GenericParameterKind Kind => _kind;

        public override int Index => _index;

        public override TypeSystemContext Context => _associatedTypeOrMethod.Context;

        public override GenericVariance Variance => _variance;

        public override TypeSystemEntity AssociatedTypeOrMethod => _associatedTypeOrMethod;
    }
}
