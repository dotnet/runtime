using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Interop
{
    class NoPreserveSigMarshallingGeneratorFactory : IMarshallingGeneratorFactory
    {
        private static readonly HResultExceptionMarshaller HResultException = new HResultExceptionMarshaller();
        private readonly IMarshallingGeneratorFactory inner;

        public NoPreserveSigMarshallingGeneratorFactory(IMarshallingGeneratorFactory inner)
        {
            this.inner = inner;
        }

        public IMarshallingGenerator Create(TypePositionInfo info, StubCodeContext context)
        {
            if (info.IsNativeReturnPosition && !info.IsManagedReturnPosition)
            {
                // Use marshaller for native HRESULT return / exception throwing
                System.Diagnostics.Debug.Assert(info.ManagedType.Equals(SpecialTypeInfo.Int32));
                return HResultException;
            }
            return inner.Create(info, context);
        }
    }
}
