// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Interop
{
    internal class NoPreserveSigMarshallingGeneratorFactory : IMarshallingGeneratorFactory
    {
        private static readonly HResultExceptionMarshaller s_hResultException = new HResultExceptionMarshaller();
        private readonly IMarshallingGeneratorFactory _inner;

        public NoPreserveSigMarshallingGeneratorFactory(IMarshallingGeneratorFactory inner)
        {
            _inner = inner;
        }

        public IMarshallingGenerator Create(TypePositionInfo info, StubCodeContext context)
        {
            if (info.IsNativeReturnPosition && !info.IsManagedReturnPosition)
            {
                // Use marshaller for native HRESULT return / exception throwing
                System.Diagnostics.Debug.Assert(info.ManagedType.Equals(SpecialTypeInfo.Int32));
                return s_hResultException;
            }
            return _inner.Create(info, context);
        }
    }
}
