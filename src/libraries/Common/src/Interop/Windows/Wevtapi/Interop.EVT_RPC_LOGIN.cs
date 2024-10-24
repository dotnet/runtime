// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Eventing.Reader;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices;
using System;

internal static partial class Interop
{
    internal static partial class Wevtapi
    {
#if NET
        [NativeMarshalling(typeof(Marshaller))]
#endif
        [StructLayout(LayoutKind.Sequential)]
        internal struct EVT_RPC_LOGIN
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Server;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string User;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Domain;
            public CoTaskMemUnicodeSafeHandle Password;
            public int Flags;
#if NET
            [CustomMarshaller(typeof(EVT_RPC_LOGIN), MarshalMode.ManagedToUnmanagedRef, typeof(ValueMarshaller))]
            public static class Marshaller
            {
                public struct ValueMarshaller
                {
                    public struct Native
                    {
                        public IntPtr Server;
                        public IntPtr User;
                        public IntPtr Domain;
                        public IntPtr Password;
                        public int Flags;
                    }

                    private CoTaskMemUnicodeSafeHandle _passwordHandle;
                    private IntPtr _originalHandleValue;
                    private Native _value;
                    private bool _passwordHandleAddRefd;

                    public void FromManaged(EVT_RPC_LOGIN managed)
                    {
                        _passwordHandleAddRefd = false;
                        _value.Server = Marshal.StringToCoTaskMemUni(managed.Server);
                        _value.User = Marshal.StringToCoTaskMemUni(managed.User);
                        _value.Domain = Marshal.StringToCoTaskMemUni(managed.Domain);
                        _passwordHandle = managed.Password;
                        _passwordHandle.DangerousAddRef(ref _passwordHandleAddRefd);
                        _value.Password = _originalHandleValue = _passwordHandle.DangerousGetHandle();
                        _value.Flags = managed.Flags;
                    }

                    public Native ToUnmanaged() => _value;

                    public void FromUnmanaged(Native value)
                    {
                        _value = value;
                    }

                    public EVT_RPC_LOGIN ToManaged()
                    {
                        // SafeHandle fields cannot change the underlying handle value during marshalling.
                        if (_value.Password != _originalHandleValue)
                        {
                            // Match the same exception type that the built-in marshalling throws.
                            throw new NotSupportedException();
                        }

                        return new EVT_RPC_LOGIN
                        {
                            Server = Marshal.PtrToStringUni(_value.Server),
                            User = Marshal.PtrToStringUni(_value.User),
                            Domain = Marshal.PtrToStringUni(_value.Domain),
                            Password = _passwordHandle,
                            Flags = _value.Flags
                        };
                    }

                    public void Free()
                    {
                        Marshal.FreeCoTaskMem(_value.Server);
                        Marshal.FreeCoTaskMem(_value.User);
                        Marshal.FreeCoTaskMem(_value.Domain);
                        if (_passwordHandleAddRefd)
                        {
                            _passwordHandle.DangerousRelease();
                        }
                    }
                }
            }
#endif
        }
    }
}
