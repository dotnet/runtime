// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Interop
{
    internal sealed class ForwarderMarshallingGeneratorFactory : IMarshallingGeneratorFactory
    {
        private static readonly Forwarder s_forwarder = new Forwarder();

        public IMarshallingGenerator Create(TypePositionInfo info, StubCodeContext context) => s_forwarder;
    }
}
