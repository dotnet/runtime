// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

internal static partial class Interop
{
    internal static partial class JavaScript
    {
        public class SharedArrayBuffer : CoreObject
        {
            public SharedArrayBuffer(int length) : base(Runtime.New<SharedArrayBuffer>(length))
            { }

            internal SharedArrayBuffer(IntPtr js_handle) : base(js_handle)
            { }

            public int ByteLength => (int)GetObjectProperty("byteLength");
            public SharedArrayBuffer Slice(int begin, int end) => (SharedArrayBuffer)Invoke("slice", begin, end);
        }
    }
}
