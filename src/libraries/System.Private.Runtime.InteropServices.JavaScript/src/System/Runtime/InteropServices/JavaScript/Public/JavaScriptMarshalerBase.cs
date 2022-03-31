// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.JavaScript
{
    [SupportedOSPlatform("browser")]
    public abstract class JavaScriptMarshalerBase
    {
        internal IntPtr MarshallerJSHandle;

        internal abstract Type MarshaledType { get; }
        protected virtual int FixedBufferLength => 0;
        protected virtual bool UseRoot => false;

        /// <summary>
        /// See MarshallerFactory type:
        /// toManaged : (arg: JavaScriptMarshalerArg, value: any) => void
        /// toJavaScript : (arg: JavaScriptMarshalerArg) => any
        /// embedded resource name format: "{Namespace}.{Folder}.{filename}.{Extension}"
        /// </summary>
        protected abstract string? JavaScriptCode { get; }

        // this would only get called when the call is not roslyn generated
        internal abstract object ToManagedDynamic(JavaScriptMarshalerArg arg);
        // this would only get called when the call is not roslyn generated
        internal abstract void ToJavaScriptDynamic(object value, JavaScriptMarshalerArg arg);

        internal int GetFixedBufferLength()
        {
            return FixedBufferLength;
        }

        internal bool GetUseRoot()
        {
            return UseRoot;
        }

        internal string? GetJavaScriptCode()
        {
            return JavaScriptCode;
        }
    }

    [SupportedOSPlatform("browser")]
    public abstract class JavaScriptMarshalerBase<T> : JavaScriptMarshalerBase
    {
        internal override Type MarshaledType => typeof(T);
        protected abstract MarshalToManagedDelegate<T> ToManaged { get; }
        protected abstract MarshalToJavaScriptDelegate<T> ToJavaScript { get; }
        protected virtual MarshalToJavaScriptDelegate<T>? AfterToJavaScript => null;
        //TODO protected virtual BeforeMarshalToManagedDelegate<T>? BeforeToManaged => null;

        // this would only get called when the call is not roslyn generated
        internal override object ToManagedDynamic(JavaScriptMarshalerArg arg)
        {
#pragma warning disable CS8603 // Possible null reference return.
            return ToManaged(arg);
#pragma warning restore CS8603 // Possible null reference return.
        }
        // this would only get called when the call is not roslyn generated
        internal override void ToJavaScriptDynamic(object value, JavaScriptMarshalerArg arg)
        {
            T valueT = (T)value;
            ToJavaScript(ref valueT, arg);
        }
    }

    //TODO public delegate void BeforeMarshalToManagedDelegate<T>(JavaScriptMarshalerArg arg);
    public delegate T MarshalToManagedDelegate<T>(JavaScriptMarshalerArg arg);
    public delegate void MarshalToJavaScriptDelegate<T>(ref T value, JavaScriptMarshalerArg arg);

}
