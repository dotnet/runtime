// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
internal static partial class Interop
{
    internal static partial class JavaScript
    {
        public class ArrayBuffer : CoreObject
        {

            public ArrayBuffer() : base(Runtime.New<ArrayBuffer>())
            { }

            public ArrayBuffer(int length) : base(Runtime.New<ArrayBuffer>(length))
            { }

            internal ArrayBuffer(IntPtr js_handle) : base(js_handle)
            { }

            public int ByteLength => (int)GetObjectProperty("byteLength");
            public bool IsView => (bool)GetObjectProperty("isView");
            public ArrayBuffer Slice(int begin) => (ArrayBuffer)Invoke("slice", begin);
            public ArrayBuffer Slice(int begin, int end) => (ArrayBuffer)Invoke("slice", begin, end);

        }
    }
}
