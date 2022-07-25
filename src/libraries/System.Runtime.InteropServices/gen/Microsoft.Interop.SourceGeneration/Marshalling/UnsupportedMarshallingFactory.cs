// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    public sealed class UnsupportedMarshallingFactory : IMarshallingGeneratorFactory
    {
        public IMarshallingGenerator Create(TypePositionInfo info, StubCodeContext context) =>
            throw new MarshallingNotSupportedException(info, context);
    }
}
