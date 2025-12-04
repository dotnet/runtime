// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

[ComVisible(true)]
[Guid("923dd7d3-a3c0-4fe2-9222-170f9d983724")]
public interface ITaskComServer
{
    Task GetTask();
    int GetValue();
}

[ComVisible(true)]
[Guid("9ad32e71-347f-47e7-8fe2-096975e65b00")]
public class TaskComServer : ITaskComServer
{
    public const int ExpectedValue = 12345;

    public Task GetTask()
    {
        // Create a definitely-not-generic Task.
        // Non-blittable generics can't be marshalled,
        // so we can't have Task<T>.
        Task t = new(() => {});
        t.Start();
        return t;
    }

    public int GetValue()
    {
        return ExpectedValue;
    }

    public static IDisposable RegisterObject()
    {
        Guid clsid = typeof(TaskComServer).GUID;
        Factory factory = new Factory();
        Ole32.CoRegisterClassObject(
            ref clsid,
            factory,
            1, // CLSCTX_INPROC_SERVER
            1, // REGCLS_MULTIPLEUSE
            out uint cookie);
        return new Token(cookie);
    }

    private class Token(uint token) : IDisposable
    {
        public void Dispose()
        {
            Ole32.CoRevokeClassObject(token);
        }
    }
}

[ComImport]
[Guid("923dd7d3-a3c0-4fe2-9222-170f9d983724")]
[CoClass(typeof(TaskComServer_Imported))]
public interface ITaskComServer_Imported
{
    Task GetTask();
    int GetValue();
}

[ComImport]
[Guid("9ad32e71-347f-47e7-8fe2-096975e65b00")]
public class TaskComServer_Imported
{
}

[ComImport]
[ComVisible(false)]
[Guid("00000001-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IClassFactory
{
    void CreateInstance(
        [MarshalAs(UnmanagedType.Interface)] object? pUnkOuter,
        ref Guid riid,
        out IntPtr ppvObject);

    void LockServer([MarshalAs(UnmanagedType.Bool)] bool fLock);
}

internal sealed class Factory : IClassFactory
{
    public void CreateInstance(object? pUnkOuter, ref Guid riid, out IntPtr ppvObject)
    {
        if (pUnkOuter != null)
        {
            const int CLASS_E_NOAGGREGATION = unchecked((int)0x80040110);
            Marshal.ThrowExceptionForHR(CLASS_E_NOAGGREGATION);
        }

        const string IID_IUNKNOWN = "00000000-0000-0000-C000-000000000046";
        if (riid == typeof(ITaskComServer).GUID || riid == Guid.Parse(IID_IUNKNOWN))
        {
            TaskComServer server = new();
            ppvObject = Marshal.GetComInterfaceForObject(server, typeof(ITaskComServer));
        }
        else
        {
            const int E_NOINTERFACE = unchecked((int)0x80004002);
            Marshal.ThrowExceptionForHR(E_NOINTERFACE);
            ppvObject = IntPtr.Zero; // Unreachable
        }
    }

    public void LockServer(bool fLock)
    {
        // No-op
    }
}

internal static class Ole32
{
    [DllImport(nameof(Ole32))]
    public static extern int CoRegisterClassObject(
        [In] ref Guid rclsid,
        [MarshalAs(UnmanagedType.Interface)] IClassFactory pUnk,
        uint dwClsContext,
        uint flags,
        out uint lpdwRegister);

    [DllImport(nameof(Ole32))]
    public static extern int CoRevokeClassObject(uint dwRegister);
}
