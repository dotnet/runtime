// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable


using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript.Private;

namespace System.Runtime.InteropServices.JavaScript.Private
{
    internal sealed class ExceptionMarshaler : JavaScriptMarshalerBase<Exception>
    {
        protected override string JavaScriptCode => null;
        protected override bool UseRoot => true;
        protected override MarshalToManagedDelegate<Exception> ToManaged => JavaScriptMarshal.MarshalToManagedException;
        protected override MarshalToJavaScriptDelegate<Exception> ToJavaScript => JavaScriptMarshal.MarshalExceptionToJs;
    }
}
namespace System.Runtime.InteropServices.JavaScript
{
    public partial class JavaScriptMarshal
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Exception MarshalToManagedException(JavaScriptMarshalerArg arg)
        {
            // Always creating stack trace is more expensive than passing js_handle and GC of JSObject.
            // Same in reverse direcion with gc_handle and ManagedObject.
            // The Exception/JSException instances roundtip and keep identity.

            if (arg.TypeHandle == IntPtr.Zero)
            {
                return null;
            }
            if (arg.TypeHandle == JavaScriptMarshalImpl.exceptionType)
            {
                // this is managed exception round-trip
                return (Exception)((GCHandle)arg.GCHandle).Target;
            }

            JSObject jsException = null;
            if (arg.JSHandle != IntPtr.Zero)
            {
                // this is JSException round-trip
                jsException = Runtime.CreateCSOwnedProxy(arg.JSHandle, Runtime.MappedType.JSObject, 0);

                if (jsException == null)
                {
                    jsException = new JSObject(arg.JSHandle); ;
                }
            }

            var message = Unsafe.AsRef<string>(arg.RootRef.ToPointer());
            var ex = new JSException(message, jsException);
            return ex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MarshalExceptionToJs(ref Exception value, JavaScriptMarshalerArg arg)
        {
            if (value == null)
            {
                arg.TypeHandle = IntPtr.Zero;
            }
            else
            {
                var jse = value as JSException;
                if (jse != null && jse.jsException != null)
                {
                    // this is JSException roundtrip
                    arg.TypeHandle = JavaScriptMarshalImpl.jsExceptionType;
                    arg.JSHandle = (IntPtr)jse.jsException.JSHandle;
                }
                else
                {
                    arg.TypeHandle = JavaScriptMarshalImpl.exceptionType;
                    arg.GCHandle = (IntPtr)Runtime.GetJSOwnedObjectGCHandle(value);
                }
            }
        }
    }
}
