// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    internal class RuntimeGenericParameterDesc : GenericParameterDesc
    {
        private readonly GenericParameterKind _kind;
        private readonly int _index;
        private readonly TypeSystemContext _context;
        private readonly GenericVariance _variance;

        public RuntimeGenericParameterDesc(GenericParameterKind kind, int index, TypeSystemContext context, GenericVariance variance)
        {
            _kind = kind;
            _index = index;
            _context = context;
            _variance = variance;
        }

        public override GenericParameterKind Kind => _kind;

        public override int Index => _index;

        public override TypeSystemContext Context => _context;

        public override GenericVariance Variance => _variance;
    }
}
