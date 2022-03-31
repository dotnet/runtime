// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable


using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.JavaScript.Private
{
    internal sealed class StringMarshaler : JavaScriptMarshalerBase<string>
    {
        protected override string JavaScriptCode => null;
        protected override bool UseRoot => true;
        protected override MarshalToManagedDelegate<string> ToManaged => JavaScriptMarshal.MarshalToManagedString;
        protected override MarshalToJavaScriptDelegate<string> ToJavaScript => JavaScriptMarshal.MarshalStringToJs;
    }
}

namespace System.Runtime.InteropServices.JavaScript
{
    public partial class JavaScriptMarshal
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe string MarshalToManagedString(JavaScriptMarshalerArg arg)
        {
            // We marshal the string by passing MonoString* to JS side and convert it there.
            // MonoString* is rooted on C# stack because string is ref type
            // We don't need GCHandle here because we then covert it by value (or reuse interned JS string).
            if (arg.TypeHandle == IntPtr.Zero)
            {
                return null;
            }
            return Unsafe.AsRef<string>(arg.RootRef.ToPointer());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MarshalStringToJs(ref string value, JavaScriptMarshalerArg arg)
        {
            if (value == null)
            {
                arg.TypeHandle = IntPtr.Zero;
            }
            else
            {
                SetRootRef(ref value, arg);
            }
        }
    }
}
