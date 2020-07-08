// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices.JavaScript;
using Xunit;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public static class HelperMarshal
    {
        public static byte[] byteBuffer;
        private static void MarshalArrayBuffer(ArrayBuffer buffer)
        {
            using (var bytes = new Uint8Array(buffer))
                byteBuffer = bytes.ToArray();
        }

        private static void MarshalByteBuffer(Uint8Array buffer)
        {
            byteBuffer = buffer.ToArray();
        }
    }
}
