// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Runtime.InteropServices.JavaScript.Private
{
    internal partial class JavaScriptMarshalImpl
    {
        internal static void RegisterCustomMarshalers(JavaScriptMarshalerBase[] customMarshallers)
        {
            if (customMarshallers == null)
            {
                return;
            }
            for (int i = 0; i < customMarshallers.Length; i++)
            {
                JavaScriptMarshalerBase marshaler = customMarshallers[i];
                if (marshaler.MarshallerJSHandle == IntPtr.Zero)
                {
                    string? code = marshaler.GetJavaScriptCode();
                    if (code == null) throw new ArgumentException("JavaScriptCode");

                    var type = marshaler.MarshaledType.TypeHandle.Value;
                    var exceptionMessage = _RegisterCustomMarshaller(code, type, out var jsHandle, out var isException);
                    if (isException != 0)
                    {
                        throw new JSException(exceptionMessage);
                    }
                    marshaler.MarshallerJSHandle = jsHandle;
                }
            }
        }
    }
}
