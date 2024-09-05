// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Melanzana.CodeSign.Requirements
{
    public abstract partial class Expression
    {
        public abstract int Size { get; }

        public abstract void Write(Span<byte> buffer, out int bytesWritten);
    }
}
