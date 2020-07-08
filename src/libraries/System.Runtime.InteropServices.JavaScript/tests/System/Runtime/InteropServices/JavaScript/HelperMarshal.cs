// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices.JavaScript;
using Xunit;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public static class HelperMarshal
    {
        public static int i32_res;
        public static void InvokeI32(int a, int b)
        {
            i32_res = a + b;
        }

        public static float f32_res;
        public static void InvokeFloat(float f)
        {
            f32_res = f;
        }

        public static double f64_res;
        public static void InvokeDouble(double d)
        {
            f64_res = d;
        }

        public static long i64_res;
        public static void InvokeLong(long l)
        {
            i64_res = l;
        }
        internal static byte[] byteBuffer;
        private static void MarshalArrayBuffer(ArrayBuffer buffer)
        {
            using (var bytes = new Uint8Array(buffer))
                byteBuffer = bytes.ToArray();
        }

        private static void MarshalByteBuffer(Uint8Array buffer)
        {
            byteBuffer = buffer.ToArray();
        }
        internal static int[] intBuffer;
        private static void MarshalArrayBufferToInt32Array(ArrayBuffer buffer)
        {
            using (var ints = new Int32Array(buffer))
                intBuffer = ints.ToArray();
        }

        internal static string _stringResource;
        private static void InvokeString(string s)
        {
            _stringResource = s;
        }
        internal static string _marshalledString;
        private static string InvokeMarshalString()
        {
            _marshalledString = "Hic Sunt Dracones";
            return _marshalledString;
        }
    }
}
