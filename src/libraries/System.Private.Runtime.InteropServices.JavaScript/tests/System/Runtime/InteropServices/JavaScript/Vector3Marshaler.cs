// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vector3
    {
        public double X;
        public double Y;
        public double Z;
    }

    public class Vector3Marshaler : JavaScriptMarshalerBase<Vector3>
    {
        protected override int FixedBufferLength => 3 * sizeof(double);
        protected override MarshalToManagedDelegate<Vector3> ToManaged => MarshalToManaged;
        protected override MarshalToJavaScriptDelegate<Vector3> ToJavaScript => MarshalToJs;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector3 MarshalToManaged(JavaScriptMarshalerArg arg)
        {
            var ptr = (Vector3*)arg.ExtraBufferPtr;
            return *ptr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static void MarshalToJs(ref Vector3 value, JavaScriptMarshalerArg arg)
        {
            var ptr = (Vector3*)arg.ExtraBufferPtr;
            *ptr = value;
        }
        protected override string JavaScriptCode => @"function createMarshaller(helpers) {
    const { MONO, get_extra_buffer } = helpers;
    return {
        toManaged: (arg, value) => {
            const buf = get_extra_buffer(arg);
            MONO.setF64(buf, value.X);
            MONO.setF64(buf + 8, value.Y);
            MONO.setF64(buf + 16, value.Z);
        },
        toJavaScript: (arg) => {
            const buf = get_extra_buffer(arg);
            const res = {
                X: MONO.getF64(buf),
                Y: MONO.getF64(buf + 8),
                Z: MONO.getF64(buf + 16),
            }
            return res;
        }
    }}";
    }
}
