// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

using JSObject = System.Runtime.InteropServices.JavaScript.JSObject;
using JSException = System.Runtime.InteropServices.JavaScript.JSException;
using Uint8Array = System.Runtime.InteropServices.JavaScript.Uint8Array;

internal static partial class Interop
{
    internal static partial class Runtime
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern string InvokeJS(string str, out int exceptionalResult);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object CompileFunction(string str, out int exceptionalResult);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object InvokeJSWithArgs(int jsHandle, string method, object?[] parms, out int exceptionalResult);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object GetObjectProperty(int jsHandle, string propertyName, out int exceptionalResult);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object SetObjectProperty(int jsHandle, string propertyName, object value, bool createIfNotExists, bool hasOwnProperty, out int exceptionalResult);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object GetByIndex(int jsHandle, int index, out int exceptionalResult);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object SetByIndex(int jsHandle, int index, object? value, out int exceptionalResult);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object GetGlobalObject(string? globalName, out int exceptionalResult);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object ReleaseCSOwnedObject(int jsHandle);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object CreateCSOwnedObject(string className, object[] parms, out int exceptionalResult);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object TypedArrayToArray(int jsHandle, out int exceptionalResult);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object TypedArrayCopyTo(int jsHandle, int arrayPtr, int begin, int end, int bytesPerElement, out int exceptionalResult);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object TypedArrayFrom(int arrayPtr, int begin, int end, int bytesPerElement, int type, out int exceptionalResult);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object TypedArrayCopyFrom(int jsHandle, int arrayPtr, int begin, int end, int bytesPerElement, out int exceptionalResult);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern string? AddEventListener(int jsHandle, string name, int gcHandle, int optionsJsHandle);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern string? RemoveEventListener(int jsHandle, string name, int gcHandle, bool capture);

        // / <summary>
        // / Execute the provided string in the JavaScript context
        // / </summary>
        // / <returns>The js.</returns>
        // / <param name="str">String.</param>
        public static string InvokeJS(string str)
        {
            string res = InvokeJS(str, out int exception);
            if (exception != 0)
                throw new JSException(res);
            return res;
        }

        public static System.Runtime.InteropServices.JavaScript.Function? CompileFunction(string snippet)
        {
            object res = CompileFunction(snippet, out int exception);
            if (exception != 0)
                throw new JSException((string)res);
            ReleaseInFlight(res);
            return res as System.Runtime.InteropServices.JavaScript.Function;
        }

        public static object GetGlobalObject(string? str = null)
        {
            int exception;
            object jsObj = GetGlobalObject(str, out exception);

            if (exception != 0)
                throw new JSException($"Error obtaining a handle to global {str}");

            ReleaseInFlight(jsObj);
            return jsObj;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void StopProfile()
        {
        }

        // Called by the AOT profiler to save profile data into Module.aot_profile_data
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static unsafe void DumpAotProfileData(ref byte buf, int len, string extraArg)
        {
            if (len == 0)
                throw new JSException("Profile data length is 0");

            var arr = new byte[len];
            fixed (void *p = &buf)
            {
                var span = new ReadOnlySpan<byte>(p, len);
                // Send it to JS
                var module = (JSObject)Runtime.GetGlobalObject("Module");
                module.SetObjectProperty("aot_profile_data", Uint8Array.From(span));
            }
        }

        internal static void ReleaseInFlight(object? obj)
        {
            JSObject? jsObj = obj as JSObject;
            jsObj?.ReleaseInFlight();
        }
    }
}
