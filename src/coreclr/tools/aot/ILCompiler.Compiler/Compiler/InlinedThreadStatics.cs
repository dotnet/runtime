// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Internal.TypeSystem;

namespace ILCompiler
{
    public class InlinedThreadStatics
    {
        internal virtual bool IsComputed() => false;

        internal virtual int GetSize() => throw new InvalidOperationException();
        internal virtual List<MetadataType> GetTypes() => throw new InvalidOperationException();
        internal virtual Dictionary<MetadataType, int> GetOffsets() => throw new InvalidOperationException();
    }
}
