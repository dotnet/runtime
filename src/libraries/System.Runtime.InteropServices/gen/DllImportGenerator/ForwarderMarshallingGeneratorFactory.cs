using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Interop
{
    class ForwarderMarshallingGeneratorFactory : IMarshallingGeneratorFactory
    {
        private static readonly Forwarder Forwarder = new Forwarder();

        public IMarshallingGenerator Create(TypePositionInfo info, StubCodeContext context) => Forwarder;
    }
}
