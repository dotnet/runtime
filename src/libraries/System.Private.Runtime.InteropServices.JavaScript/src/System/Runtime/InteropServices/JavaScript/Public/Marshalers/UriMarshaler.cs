// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.JavaScript
{
    public class UriMarshaler : JavaScriptMarshalerBase<Uri>
    {
        protected override bool UseRoot => true;
        protected override MarshalToManagedDelegate<Uri> ToManaged => MarshalToManaged;
        protected override MarshalToJavaScriptDelegate<Uri> ToJavaScript => MarshalToJs;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Uri MarshalToManaged(JavaScriptMarshalerArg arg)
        {
            if (arg.TypeHandle == IntPtr.Zero)
            {
                return null;
            }
            var value = Unsafe.AsRef<string>(arg.RootRef.ToPointer());
            return new Uri(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void MarshalToJs(ref Uri value, JavaScriptMarshalerArg arg)
        {
            if (value == null)
            {
                arg.TypeHandle = IntPtr.Zero;
            }
            else
            {
                var uriValue = value.ToString();
                JavaScriptMarshal.SetRootRef(ref uriValue, arg);
            }
        }

        protected override string JavaScriptCode => @"function createUriMarshaller() {
        return {
            toManaged: (arg, value) => {
                if (!value) {
                    set_arg_type(arg, 0);
                }
                else {
                    set_arg_type(arg, mono_type);
                    const pStr = BINDING.js_string_to_mono_string(value);
                    set_root_ref(arg, pStr);
                }
            },
            toJavaScript: (arg) => {
                const type = get_arg_type(arg);
                if (!type) {
                    return null;
                }
                const ref = get_root_ref(arg);
                const value = BINDING.conv_string(ref);
                return value;
            }
        }}";
    }
}
