// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

public static class ComActivationHelpers
{
    public static IDisposable RegisterTypeForActivation<T>()
        where T: new()
    {
        Factory factory = new(() => new T());
        CoRegisterClassObject(
            typeof(T).GUID,
            factory,
            1, // CLSCTX_INPROC_SERVER
            1, // REGCLS_MULTIPLEUSE
            out uint cookie);
        return new RegistrationToken(cookie);
    }

    private class RegistrationToken(uint token) : IDisposable
    {
        public void Dispose()
        {
            CoRevokeClassObject(token);
        }
    }

    [DllImport("ole32")]
    private static extern int CoRegisterClassObject(
        in Guid rclsid,
        [MarshalAs(UnmanagedType.Interface)] IClassFactory pUnk,
        uint dwClsContext,
        uint flags,
        out uint lpdwRegister);

    [DllImport("ole32")]
    private static extern int CoRevokeClassObject(uint dwRegister);

    [ComImport]
    [ComVisible(false)]
    [Guid("00000001-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IClassFactory
    {
        void CreateInstance(
            [MarshalAs(UnmanagedType.Interface)] object? pUnkOuter,
            in Guid riid,
            out nint ppvObject);

        void LockServer([MarshalAs(UnmanagedType.Bool)] bool fLock);
    }

    private sealed class Factory(Func<object> factory) : IClassFactory
    {
        public void CreateInstance(object? pUnkOuter, in Guid riid, out nint ppvObject)
        {
            if (pUnkOuter != null)
            {
                const int CLASS_E_NOAGGREGATION = unchecked((int)0x80040110);
                Marshal.ThrowExceptionForHR(CLASS_E_NOAGGREGATION);
            }

            nint ccw = Marshal.GetIUnknownForObject(factory());
            try
            {
                int hr = Marshal.QueryInterface(ccw, in riid, out ppvObject);
                Marshal.ThrowExceptionForHR(hr);
            }
            finally
            {
                Marshal.Release(ccw);
            }
        }

        public void LockServer(bool fLock)
        {
            // No-op
        }
    }
}
