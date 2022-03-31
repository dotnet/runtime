// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript.Private;

namespace System.Runtime.InteropServices.JavaScript
{
    public partial class JavaScriptMarshal
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool MarshalToManagedBoolean(JavaScriptMarshalerArg arg)
        {
            return arg.BooleanValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MarshalBooleanToJs(ref bool value, JavaScriptMarshalerArg arg)
        {
            arg.BooleanValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe byte MarshalToManagedByte(JavaScriptMarshalerArg arg)
        {
            return arg.ByteValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MarshalByteToJs(ref byte value, JavaScriptMarshalerArg arg)
        {
            arg.ByteValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe short MarshalToManagedInt16(JavaScriptMarshalerArg arg)
        {
            return arg.Int16Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MarshalInt16ToJs(ref short value, JavaScriptMarshalerArg arg)
        {
            arg.Int16Value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int MarshalToManagedInt32(JavaScriptMarshalerArg arg)
        {
            return arg.Int32Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MarshalInt32ToJs(ref int value, JavaScriptMarshalerArg arg)
        {
            arg.Int32Value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe IntPtr MarshalToManagedIntPtr(JavaScriptMarshalerArg arg)
        {
            return arg.IntPtrValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MarshalIntPtrToJs(ref IntPtr value, JavaScriptMarshalerArg arg)
        {
            arg.IntPtrValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe long MarshalToManagedInt64(JavaScriptMarshalerArg arg)
        {
            return arg.Int64Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MarshalInt64ToJs(ref long value, JavaScriptMarshalerArg arg)
        {
            arg.Int64Value = value;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe float MarshalToManagedSingle(JavaScriptMarshalerArg arg)
        {
            return arg.SingleValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MarshalSingleToJs(ref float value, JavaScriptMarshalerArg arg)
        {
            arg.SingleValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe double MarshalToManagedDouble(JavaScriptMarshalerArg arg)
        {
            return arg.DoubleValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MarshalDoubleToJs(ref double value, JavaScriptMarshalerArg arg)
        {
            arg.DoubleValue = value;
        }
    }
}
