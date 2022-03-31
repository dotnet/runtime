// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable


using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript.Private;

namespace System.Runtime.InteropServices.JavaScript.Private
{
    internal sealed class JSObjectMarshaler : JavaScriptMarshalerBase<IJSObject>
    {
        protected override string JavaScriptCode => null;
        protected override MarshalToManagedDelegate<IJSObject> ToManaged => JavaScriptMarshal.MarshalToManagedIJSObject;
        protected override MarshalToJavaScriptDelegate<IJSObject> ToJavaScript => JavaScriptMarshal.MarshalIJSObjectToJs;
    }
}

namespace System.Runtime.InteropServices.JavaScript
{
    public partial class JavaScriptMarshal
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe IJSObject MarshalToManagedIJSObject(JavaScriptMarshalerArg arg)
        {
            if (arg.TypeHandle == IntPtr.Zero)
            {
                return null;
            }

            return Runtime.CreateCSOwnedProxy(arg.JSHandle, Runtime.MappedType.JSObject, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MarshalIJSObjectToJs(ref IJSObject value, JavaScriptMarshalerArg arg)
        {
            if (value == null)
            {
                arg.TypeHandle = IntPtr.Zero;
            }
            else
            {
                arg.TypeHandle = JavaScriptMarshalImpl.ijsObjectType;
                arg.JSHandle = (IntPtr)((JSObject)value).JSHandle;
            }
        }
    }
}
