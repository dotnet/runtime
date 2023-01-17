// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.JavaScript
{
    internal static partial class JSHostImplementation
    {
        internal unsafe delegate void ToManagedCallback(JSMarshalerArgument* arguments_buffer);

        public sealed class TaskCallback
        {
            public ToManagedCallback? Callback;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct IntPtrAndHandle
        {
            [FieldOffset(0)]
            internal IntPtr ptr;

            [FieldOffset(0)]
            internal RuntimeMethodHandle methodHandle;

            [FieldOffset(0)]
            internal RuntimeTypeHandle typeHandle;
        }
    }
}
