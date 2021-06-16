// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Drawing.Imaging
{
    internal unsafe struct PropertyItemInternal
    {
        public int id;
        public int len;
        public short type;
        public byte* value;

        public Span<byte> Value => new Span<byte>(value, len);
    }
}
