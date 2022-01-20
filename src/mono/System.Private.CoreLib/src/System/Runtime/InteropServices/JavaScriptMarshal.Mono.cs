// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.JavaScript
{
    [System.Runtime.Versioning.SupportedOSPlatformAttribute("browser")]
    [System.CLSCompliantAttribute(false)]
    public static partial class JavaScriptMarshal
    {
        public static InvokeJSResult InvokeJSFunctionByName (string internedFunctionName) {
            return (InvokeJSResult)JavaScriptMarshalImpl.InvokeJSFunction(
                internedFunctionName, 0,
                IntPtr.Zero, IntPtr.Zero,
                IntPtr.Zero, IntPtr.Zero,
                IntPtr.Zero, IntPtr.Zero
            );
        }

        public static InvokeJSResult InvokeJSFunctionByName<T1> (string internedFunctionName, T1 arg1) {
            return JavaScriptMarshalImpl.InvokeJSFunctionByName(internedFunctionName, ref arg1);
        }

        public static InvokeJSResult InvokeJSFunctionByName<T1, T2> (string internedFunctionName, T1 arg1, T2 arg2) {
            return JavaScriptMarshalImpl.InvokeJSFunctionByName(internedFunctionName, ref arg1, ref arg2);
        }

        public static InvokeJSResult InvokeJSFunctionByName<T1, T2, T3> (string internedFunctionName, T1 arg1, T2 arg2, T3 arg3) {
            return JavaScriptMarshalImpl.InvokeJSFunctionByName(internedFunctionName, ref arg1, ref arg2, ref arg3);
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatformAttribute("browser")]
    internal static class JavaScriptMarshalImpl
    {
        /// <summary>
        /// Invoke a JS function with a specified name, passing up to 3 argument(s)
        /// of a specified type at a specified address.
        /// NOTE: For reference types, argN must be the address of a reference to the object, not the
        /// address of the object itself. This ensures that the GC can safely move the object.
        /// For value types (including pointers, ints, etc) argN is the address of the value.
        /// </summary>
        internal static InvokeJSResult InvokeJSFunctionByName (
            string internedFunctionName, int argumentCount,
            Type type1, IntPtr address1,
            Type type2, IntPtr address2,
            Type type3, IntPtr address3
        ) {
            return (InvokeJSResult)InvokeJSFunction(
                internedFunctionName, argumentCount,
                type1?.TypeHandle.Value ?? IntPtr.Zero, address1,
                type2?.TypeHandle.Value ?? IntPtr.Zero, address2,
                type3?.TypeHandle.Value ?? IntPtr.Zero, address3
            );
        }

        public static unsafe InvokeJSResult InvokeJSFunctionByName<T1> (string internedFunctionName, ref T1 arg1) {
            var resultCode = InvokeJSFunction(
                internedFunctionName, 1,
                typeof(T1).TypeHandle.Value, (IntPtr)Unsafe.AsPointer(ref arg1),
                IntPtr.Zero, IntPtr.Zero,
                IntPtr.Zero, IntPtr.Zero
            );
            return (InvokeJSResult)resultCode;
        }

        public static unsafe InvokeJSResult InvokeJSFunctionByName<T1, T2> (string internedFunctionName, ref T1 arg1, ref T2 arg2) {
            var resultCode = InvokeJSFunction(
                internedFunctionName, 2,
                typeof(T1).TypeHandle.Value, (IntPtr)Unsafe.AsPointer(ref arg1),
                typeof(T2).TypeHandle.Value, (IntPtr)Unsafe.AsPointer(ref arg2),
                IntPtr.Zero, IntPtr.Zero
            );
            return (InvokeJSResult)resultCode;
        }

        public static unsafe InvokeJSResult InvokeJSFunctionByName<T1, T2, T3> (string internedFunctionName, ref T1 arg1, ref T2 arg2, ref T3 arg3) {
            var resultCode = InvokeJSFunction(
                internedFunctionName, 3,
                typeof(T1).TypeHandle.Value, (IntPtr)Unsafe.AsPointer(ref arg1),
                typeof(T2).TypeHandle.Value, (IntPtr)Unsafe.AsPointer(ref arg2),
                typeof(T3).TypeHandle.Value, (IntPtr)Unsafe.AsPointer(ref arg3)
            );
            return (InvokeJSResult)resultCode;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern int InvokeJSFunction(string internedFunctionName, int argumentCount,
                            IntPtr type1, IntPtr arg1,
                            IntPtr type2, IntPtr arg2,
                            IntPtr type3, IntPtr arg3
                        );
    }

    [System.Runtime.Versioning.SupportedOSPlatformAttribute("browser")]
    public enum InvokeJSResult : int
    {
        Success = 0,
        InvalidFunctionName,
        FunctionNotFound,
        InvalidArgumentCount,
        InvalidArgumentType,
        MissingArgumentType,
        NullArgumentPointer,
        FunctionHadReturnValue,
        FunctionThrewException,
        InternalError,
    }
}