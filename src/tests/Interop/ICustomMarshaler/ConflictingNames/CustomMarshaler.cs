// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

class WrappedString
{
    public WrappedString(string str)
    {
        _str = str;
    }

    internal string _str;
}

class WrappedStringCustomMarshaler : ICustomMarshaler
{
    public void CleanUpManagedData(object ManagedObj) { }
    public void CleanUpNativeData(IntPtr pNativeData) { Marshal.ZeroFreeCoTaskMemAnsi(pNativeData); }

    public int GetNativeDataSize() => IntPtr.Size;

    public IntPtr MarshalManagedToNative(object ManagedObj) => Marshal.StringToCoTaskMemAnsi(((WrappedString)ManagedObj)._str);
    public object MarshalNativeToManaged(IntPtr pNativeData) => new WrappedString(Marshal.PtrToStringAnsi(pNativeData));

    public static ICustomMarshaler GetInstance(string cookie) => new WrappedStringCustomMarshaler();
}

// Use an ifdef here to give us two separate public API surfaces to call while allowing us to have the same implementation code
// as well as allowing us to share the custom marshaler implementations above.
// If we wanted to add more tests here, we would want to put the public API surface in the namespace and the private
// details and marshalers in the global scope as done above.
#if CUSTOMMARSHALERS2
namespace CustomMarshalers2
#else
namespace CustomMarshalers
#endif
{
    public class CustomMarshalerTest
    {
        [DllImport("CustomMarshalerNative", CharSet = CharSet.Ansi)]
        private static extern int NativeParseInt([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(WrappedStringCustomMarshaler))] WrappedString str);

        public int ParseInt(string str)
        {
            return NativeParseInt(new WrappedString(str));
        }
    }
}

