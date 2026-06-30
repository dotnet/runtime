// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

[ComVisible(true)]
[Guid("A1111111-0000-0000-0000-000000000001")]
public interface IFoo
{
    void DoWork();
}

[ComVisible(true)]
[Guid("A1111111-0000-0000-0000-000000000002")]
[ComDefaultInterface(typeof(IFoo))]
public class Foo : IFoo
{
    public virtual void DoWork() => VirtualMethodOverrideTest.LastCalledType = nameof(Foo);
}

[ComVisible(true)]
[Guid("A1111111-0000-0000-0000-000000000003")]
[ComDefaultInterface(typeof(IFoo))]
public class FooDerived : Foo
{
    public override void DoWork() => VirtualMethodOverrideTest.LastCalledType = nameof(FooDerived);
}

[ComVisible(true)]
[Guid("B2222222-0000-0000-0000-000000000001")]
public interface IBar
{
    void DoWork();
}

[ComVisible(true)]
[Guid("B2222222-0000-0000-0000-000000000002")]
[ComDefaultInterface(typeof(IBar))]
public class Bar : IBar
{
    public virtual void DoWork() => VirtualMethodOverrideTest.LastCalledType = nameof(Bar);
}

[ComVisible(true)]
[Guid("B2222222-0000-0000-0000-000000000003")]
[ComDefaultInterface(typeof(IBar))]
public class BarDerived : Bar
{
    public override void DoWork() => VirtualMethodOverrideTest.LastCalledType = nameof(BarDerived);
}

/// <summary>
/// Tests that COM-to-CLR dispatch correctly resolves virtual method overrides
/// regardless of whether the base or derived class is accessed via COM first.
/// </summary>
public class VirtualMethodOverrideTest
{
    internal static string? LastCalledType;

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate int DoWorkDelegate(IntPtr pThis);

    private static int CallDoWork(IntPtr pInterface, int slot)
    {
        IntPtr vtbl = Marshal.ReadIntPtr(pInterface);
        IntPtr fnPtr = Marshal.ReadIntPtr(vtbl, slot * IntPtr.Size);
        Assert.NotEqual(IntPtr.Zero, fnPtr);

        var fn = Marshal.GetDelegateForFunctionPointer<DoWorkDelegate>(fnPtr);
        return fn(pInterface);
    }

    [Fact]
    public static void DerivedFirst()
    {
        int doWorkSlot = Marshal.GetStartComSlot(typeof(IFoo));
        IntPtr pDerived = IntPtr.Zero;
        IntPtr pBase = IntPtr.Zero;
        try
        {
            pDerived = Marshal.GetComInterfaceForObject(new FooDerived(), typeof(IFoo));
            pBase = Marshal.GetComInterfaceForObject(new Foo(), typeof(IFoo));

            LastCalledType = null;
            Assert.True(CallDoWork(pDerived, doWorkSlot) >= 0);
            Assert.Equal(nameof(FooDerived), LastCalledType);

            LastCalledType = null;
            Assert.True(CallDoWork(pBase, doWorkSlot) >= 0);
            Assert.Equal(nameof(Foo), LastCalledType);
        }
        finally
        {
            if (pDerived != IntPtr.Zero)
                Marshal.Release(pDerived);

            if (pBase != IntPtr.Zero)
                Marshal.Release(pBase);
        }
    }

    [Fact]
    public static void BaseFirst()
    {
        int doWorkSlot = Marshal.GetStartComSlot(typeof(IBar));
        IntPtr pBase = IntPtr.Zero;
        IntPtr pDerived = IntPtr.Zero;
        try
        {
            pBase = Marshal.GetComInterfaceForObject(new Bar(), typeof(IBar));
            pDerived = Marshal.GetComInterfaceForObject(new BarDerived(), typeof(IBar));

            LastCalledType = null;
            Assert.True(CallDoWork(pBase, doWorkSlot) >= 0);
            Assert.Equal(nameof(Bar), LastCalledType);

            LastCalledType = null;
            Assert.True(CallDoWork(pDerived, doWorkSlot) >= 0);
            Assert.Equal(nameof(BarDerived), LastCalledType);
        }
        finally
        {
            if (pBase != IntPtr.Zero) Marshal.Release(pBase);
            if (pDerived != IntPtr.Zero) Marshal.Release(pDerived);
        }
    }
}
