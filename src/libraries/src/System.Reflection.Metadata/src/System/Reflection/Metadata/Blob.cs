// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Metadata
{
    public readonly struct Blob
    {
        internal readonly byte[] Buffer;
        internal readonly int Start;
        public int Length { get; }

        internal Blob(byte[] buffer, int start, int length)
        {
            Buffer = buffer;
            Start = start;
            Length = length;
        }

        public bool IsDefault => Buffer == null;

        public ArraySegment<byte> GetBytes() => new ArraySegment<byte>(Buffer, Start, Length);
    }
}
