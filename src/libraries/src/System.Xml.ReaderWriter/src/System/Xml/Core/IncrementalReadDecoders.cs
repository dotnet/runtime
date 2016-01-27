// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Xml
{
    //
    //  IncrementalReadDecoder abstract class
    //
    internal abstract class IncrementalReadDecoder
    {
        internal abstract int DecodedCount { get; }
        internal abstract bool IsFull { get; }
        internal abstract void SetNextOutputBuffer(Array array, int offset, int len);
        internal abstract int Decode(char[] chars, int startPos, int len);
        internal abstract int Decode(string str, int startPos, int len);
        internal abstract void Reset();
    }
}
