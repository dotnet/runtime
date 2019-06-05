// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using TestLibrary;

class FakeHandle : SafeHandle
{
    public override bool IsInvalid => handle == IntPtr.Zero;

    public FakeHandle()
        : base(IntPtr.Zero, true)
    {
    }

    override protected bool ReleaseHandle()
    {
        handle = IntPtr.Zero;
        return true;
    }
}

public class ThrowingCustomMarshaler : ICustomMarshaler
{
    public void CleanUpManagedData(object ManagedObj) { }
    public void CleanUpNativeData(IntPtr pNativeData) { }

    public int GetNativeDataSize() => IntPtr.Size;

    public IntPtr MarshalManagedToNative(object ManagedObj) => throw new NotImplementedException();
    public object MarshalNativeToManaged(IntPtr pNativeData)
    {
        // Cause an exception during the unmarshal phase of the IL stub.
        throw new InvalidOperationException();
    }

    public static ICustomMarshaler GetInstance(string cookie) => new ThrowingCustomMarshaler();
}

internal class ReliableUnmarshalNative
{
    // We're using a custom marshaler here to cause an exception during the unmarshal phase of the IL stub after successfully returning from native code.
    [DllImport(nameof(ReliableUnmarshalNative))]
    public static extern void GetFakeHandle(IntPtr value, out FakeHandle handle, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ThrowingCustomMarshaler), MarshalCookie = "")] out object cookie);
}

public class ReliableUnmarshalTest
{
    public static int Main()
    {
        try
        {
            // Test that our SafeHandle-derived object has its underlying handle set after a P/Invoke
            // even if there's an exception during the unmarshal phase.
            IntPtr value = (IntPtr)123;
            FakeHandle h = new FakeHandle();

            Assert.Throws<InvalidOperationException>(() => ReliableUnmarshalNative.GetFakeHandle(value, out h, out var cookie));

            Assert.AreEqual(value, h.DangerousGetHandle());
        }
        catch (System.Exception ex)
        {
            Console.WriteLine(ex);
            return 101;
        }
        return 100;
    }
}
