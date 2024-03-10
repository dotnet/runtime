// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using NativeCallManagedComVisible;
using Xunit;

// Don't set ComVisible.
// [assembly: ComVisible(true)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("41DDB0BD-1E88-4B0C-BD23-FD3B7E4037A8")]

/// <summary>
/// Interface with ComImport.
/// </summary>
[ComImport]
[Guid("52E5F852-BD3E-4DF2-8826-E1EC39557943")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IInterfaceComImport
{
    int Foo();
}

/// <summary>
/// Interface visible with ComVisible(true).
/// </summary>
[ComVisible(true)]
[Guid("8FDE13DC-F917-44FF-AAC8-A638FD27D647")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IInterfaceVisibleTrue
{
    int Foo();
}

/// <summary>
/// Interface not visible with ComVisible(false).
/// </summary>
[ComVisible(false)]
[Guid("0A2EF649-371D-4480-B0C7-07F455C836D3")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IInterfaceVisibleFalse
{
    int Foo();
}

/// <summary>
/// Interface not visible without ComVisible().
/// </summary>
[Guid("FB504D72-39C4-457F-ACF4-3E5D8A31AAE4")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IInterfaceWithoutVisible
{
    int Foo();
}

/// <summary>
/// Interface not public.
/// </summary>
[ComVisible(true)]
[Guid("11320010-13FA-4B40-8580-8CF92EE70774")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IInterfaceNotPublic
{
    int Foo();
}

/// <summary>
/// Generic interface with ComVisible(true).
/// </summary>
[ComVisible(true)]
[Guid("BA4B32D4-1D73-4605-AD0A-900A31E75BC3")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IInterfaceGenericVisibleTrue<T>
{
    T Foo();
}

/// <summary>
/// Generic class for guid generator.
/// </summary>
public class GenericClassW2Pars<T1, T2>
{
    T1 Foo(T2 a) { return default(T1); }
}

/// <summary>
/// Derived interface visible with ComVisible(true) and GUID.
/// </summary>
[ComVisible(true)]
[Guid("FE62A5B9-34C4-4EAF-AF0A-1AD390B15BDB")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IDerivedInterfaceVisibleTrueGuid
{
    int Foo();
}

/// <summary>
/// Derived interface visible with ComVisible(true) wothout GUID.
/// </summary>
[ComVisible(true)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IDerivedInterfaceVisibleTrueNoGuid
{
    int Foo1(UInt16 int16Val, bool boolVal);
    int Foo5(Int32 int32Val);
}

/// <summary>
/// Derived interface without visibility and without GUID.
/// </summary>
public interface IDerivedInterfaceWithoutVisibleNoGuid
{
    int Foo7(Int32 int32Val);
}

/// <summary>
/// Interface visible with ComVisible(true) and without Custom Attribute Guid.
/// Note that in this test, change the method sequence in the interface will
///  change the GUID and could reduce the test efficiency.
/// </summary>
[ComVisible(true)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IInterfaceVisibleTrueNoGuid : IDerivedInterfaceVisibleTrueGuid, IDerivedInterfaceVisibleTrueNoGuid, IDerivedInterfaceWithoutVisibleNoGuid
{
    new int Foo1(UInt16 int16Val, bool boolVal);
    new int Foo();
    int Foo2(string str, out int outIntVal, IntPtr intPtrVal, int[] arrayVal, byte inByteVal = 0, int inIntVal = 0);
    int Foo3(ref short refShortVal, params byte[] paramsList);
    int Foo4(ref List<short> refShortVal, GenericClassW2Pars<int, short> genericClass, params object[] paramsList);
}

/// <summary>
/// Interface not visible and without Custom Attribute Guid.
/// </summary>
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IInterfaceNotVisibleNoGuid
{
    int Foo();
}

/// <summary>
/// Interface visible with ComVisible(true), without Custom Attribute Guid and a generic method.
/// </summary>
[ComVisible(true)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IInterfaceVisibleTrueNoGuidGeneric
{
    int Foo<T>(T genericVal);
}

/// <summary>
/// Interface with ComImport derived from an interface with ComImport.
/// </summary>
[ComImport]
[Guid("943759D7-3552-43AD-9C4D-CC2F787CF36E")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IInterfaceComImport_ComImport : IInterfaceComImport
{
    new int Foo();
}

/// <summary>
/// Interface with ComVisible(true) derived from an interface with ComImport.
/// </summary>
[ComVisible(true)]
[Guid("75DE245B-0CE3-4B07-8761-328906C750B7")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IInterfaceVisibleTrue_ComImport : IInterfaceComImport
{
    new int Foo();
}

/// <summary>
/// Interface with ComVisible(false) derived from an interface with ComImport.
/// </summary>
[ComVisible(false)]
[Guid("C73D96C3-B005-42D6-93F5-E30AEE08C66C")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IInterfaceVisibleFalse_ComImport : IInterfaceComImport
{
    new int Foo();
}

/// <summary>
/// Interface with ComVisible(true) derived from an interface with ComVisible(true).
/// </summary>
[ComVisible(true)]
[Guid("60B3917B-9CC2-40F2-A975-CD6898DA697F")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IInterfaceVisibleTrue_VisibleTrue : IInterfaceVisibleTrue
{
    new int Foo();
}

/// <summary>
/// Interface with ComVisible(false) derived from an interface with ComVisible(true).
/// </summary>
[ComVisible(false)]
[Guid("2FC59DDB-B1D0-4678-93AF-6A48E838B705")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IInterfaceVisibleFalse_VisibleTrue : IInterfaceVisibleTrue
{
    new int Foo();
}

/// <summary>
/// Interface with ComVisible(true) derived from an interface with ComVisible(false).
/// </summary>
[ComVisible(true)]
[Guid("C82C25FC-FBAD-4EA9-BED1-343C887464B5")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IInterfaceVisibleTrue_VisibleFalse : IInterfaceVisibleFalse
{
    new int Foo();
}

/// <summary>
/// Interface with ComVisible(true) derived from an not public interface.
/// </summary>
[ComVisible(true)]
[Guid("8A4C1691-5615-4762-8568-481DC671F9CE")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IInterfaceNotPublic_VisibleTrue : IInterfaceVisibleTrue
{
    new int Foo();
}


/// <summary>
/// Class visible with ComVisible(true).
/// </summary>
[ComVisible(true)]
[Guid("48FC2EFC-C7ED-4E02-8D02-F05B6A439FC9")]
public sealed class ClassVisibleTrueServer :
    IInterfaceComImport, IInterfaceVisibleTrue, IInterfaceVisibleFalse, IInterfaceWithoutVisible, IInterfaceNotPublic,
    IInterfaceVisibleTrueNoGuid, IInterfaceNotVisibleNoGuid,
    IInterfaceComImport_ComImport, IInterfaceVisibleTrue_ComImport, IInterfaceVisibleFalse_ComImport,
    IInterfaceVisibleTrue_VisibleTrue, IInterfaceVisibleFalse_VisibleTrue, IInterfaceVisibleTrue_VisibleFalse, IInterfaceNotPublic_VisibleTrue
{
    int IInterfaceComImport.Foo() { return 1; }
    int IInterfaceVisibleTrue.Foo() { return 2; }
    int IInterfaceVisibleFalse.Foo() { return 3; }
    int IInterfaceWithoutVisible.Foo() { return 4; }
    int IInterfaceNotPublic.Foo() { return 5; }

    int IInterfaceVisibleTrueNoGuid.Foo() { return 6; }
    int IInterfaceVisibleTrueNoGuid.Foo1(UInt16 int16Val, bool boolVal) { return 7; }
    int IInterfaceVisibleTrueNoGuid.Foo2(string str, out int outIntVal, IntPtr intPtrVal, int[] arrayVal, byte inByteVal, int inIntVal)
    {
        outIntVal = 10;
        return 8;
    }
    int IInterfaceVisibleTrueNoGuid.Foo3(ref short refShortVal, params byte[] paramsList) { return 9; }
    int IInterfaceVisibleTrueNoGuid.Foo4(ref List<short> refShortVal, GenericClassW2Pars<int, short> genericClass, params object[] paramsList) { return 10; }
    int IDerivedInterfaceVisibleTrueGuid.Foo() { return 12; }
    int IDerivedInterfaceVisibleTrueNoGuid.Foo1(UInt16 int16Val, bool boolVal) { return 13; }
    int IDerivedInterfaceVisibleTrueNoGuid.Foo5(Int32 int32Val) { return 14; }
    int IDerivedInterfaceWithoutVisibleNoGuid.Foo7(Int32 int32Val) { return 15; }
    int IInterfaceNotVisibleNoGuid.Foo() { return 16; }

    int IInterfaceComImport_ComImport.Foo() { return 101; }
    int IInterfaceVisibleTrue_ComImport.Foo() { return 102; }
    int IInterfaceVisibleFalse_ComImport.Foo() { return 103; }
    int IInterfaceVisibleTrue_VisibleTrue.Foo() { return 104; }
    int IInterfaceVisibleFalse_VisibleTrue.Foo() { return 105; }
    int IInterfaceVisibleTrue_VisibleFalse.Foo() { return 106; }
    int IInterfaceNotPublic_VisibleTrue.Foo() { return 107; }

    int Foo() { return 9; }
}

/// <summary>
/// Class not visible with ComVisible(false).
/// </summary>
[ComVisible(false)]
[Guid("6DF17EC1-A8F4-4693-B195-EDB27DF00170")]
public sealed class ClassVisibleFalseServer :
    IInterfaceComImport, IInterfaceVisibleTrue, IInterfaceVisibleFalse, IInterfaceWithoutVisible, IInterfaceNotPublic
{
    int IInterfaceComImport.Foo() { return 120; }
    int IInterfaceVisibleTrue.Foo() { return 121; }
    int IInterfaceVisibleFalse.Foo() { return 122; }
    int IInterfaceWithoutVisible.Foo() { return 123; }
    int IInterfaceNotPublic.Foo() { return 124; }
    int Foo() { return 129; }
}

/// <summary>
/// Class not visible without ComVisible().
/// </summary>
[Guid("A57430B8-E0C1-486E-AE57-A15D6A729F99")]
public sealed class ClassWithoutVisibleServer :
    IInterfaceComImport, IInterfaceVisibleTrue, IInterfaceVisibleFalse, IInterfaceWithoutVisible, IInterfaceNotPublic
{
    int IInterfaceComImport.Foo() { return 130; }
    int IInterfaceVisibleTrue.Foo() { return 131; }
    int IInterfaceVisibleFalse.Foo() { return 132; }
    int IInterfaceWithoutVisible.Foo() { return 133; }
    int IInterfaceNotPublic.Foo() { return 134; }
    int Foo() { return 139; }
}

/// <summary>
/// Generic visible class with ComVisible(true).
/// </summary>
[ComVisible(true)]
[Guid("3CD290FA-1CD0-4370-B8E6-5A573F78C9F7")]
public sealed class ClassGenericServer<T> : IInterfaceVisibleTrue, IInterfaceGenericVisibleTrue<T>, IInterfaceComImport
{
    int IInterfaceComImport.Foo() { return 140; }
    int IInterfaceVisibleTrue.Foo() { return 141; }
    T IInterfaceGenericVisibleTrue<T>.Foo() { return default(T); }
    T Foo() { return default(T); }
}


public class ComVisibleServer
{
    /// <summary>
    /// Nested interface with ComImport.
    /// </summary>
    [ComImport]
    [Guid("1D927BC5-1530-4B8E-A183-995425CE4A0A")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface INestedInterfaceComImport
    {
        int Foo();
    }

    /// <summary>
    /// Nested interface visible with ComVisible(true).
    /// </summary>
    [ComVisible(true)]
    [Guid("39209692-2568-4B1E-A6C8-A5C7F141D278")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface INestedInterfaceVisibleTrue
    {
        int Foo();
    }

    /// <summary>
    /// Nested interface not visible with ComVisible(false).
    /// </summary>
    [ComVisible(false)]
    [Guid("1CE4B033-4927-447A-9F91-998357B32ADF")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface INestedInterfaceVisibleFalse
    {
        int Foo();
    }

    /// <summary>
    /// Nested interface not visible without ComVisible().
    /// </summary>
    [Guid("C770422A-C363-49F1-AAA1-3EC81A452816")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface INestedInterfaceWithoutVisible
    {
        int Foo();
    }

    /// <summary>
    /// Nested interface not public.
    /// </summary>
    [ComVisible(true)]
    [Guid("F776FF8A-0673-49C2-957A-33C2576062ED")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface INestedInterfaceNotPublic
    {
        int Foo();
    }

    /// <summary>
    /// Nested visible interface with ComVisible(true).
    /// </summary>
    public class NestedClass
    {
        [ComVisible(true)]
        [Guid("B31B4EC1-3B59-41C4-B3A0-CF89638CB837")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface INestedInterfaceNestedInClass
        {
            int Foo();
        }
    }

    /// <summary>
    /// Generic interface with ComVisible(true).
    /// </summary>
    [ComVisible(true)]
    [Guid("D7A8A196-5D85-4C85-94E4-8344ED2C7277")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface INestedInterfaceGenericVisibleTrue<T>
    {
        T Foo();
    }

    /// <summary>
    /// Nested interface with ComImport derived from an interface with ComImport.
    /// </summary>
    [ComImport]
    [Guid("C57D849A-A1A9-4CDC-A609-789D79F9332C")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface INestedInterfaceComImport_ComImport : INestedInterfaceComImport
    {
        new int Foo();
    }

    /// <summary>
    /// Nested interface with ComVisible(true) derived from an interface with ComImport.
    /// </summary>
    [ComVisible(true)]
    [Guid("81F28686-F257-4B7E-A47F-57C9775BE2CE")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface INestedInterfaceVisibleTrue_ComImport : INestedInterfaceComImport
    {
        new int Foo();
    }

    /// <summary>
    /// Nested interface with ComVisible(false) derived from an interface with ComImport.
    /// </summary>
    [ComVisible(false)]
    [Guid("FAAB7E6C-8548-429F-AD34-0CEC3EBDD7B7")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface INestedInterfaceVisibleFalse_ComImport : INestedInterfaceComImport
    {
        new int Foo();
    }

    /// <summary>
    /// Nested interface with ComVisible(true) derived from an interface with ComVisible(true).
    /// </summary>
    [ComVisible(true)]
    [Guid("BEFD79A9-D8E6-42E4-8228-1892298460D7")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface INestedInterfaceVisibleTrue_VisibleTrue : INestedInterfaceVisibleTrue
    {
        new int Foo();
    }

    /// <summary>
    /// Nested interface with ComVisible(false) derived from an interface with ComVisible(true).
    /// </summary>
    [ComVisible(false)]
    [Guid("5C497454-EA83-4F79-B990-4EB28505E801")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface INestedInterfaceVisibleFalse_VisibleTrue : INestedInterfaceVisibleTrue
    {
        new int Foo();
    }

    /// <summary>
    /// Nested interface with ComVisible(true) derived from an interface with ComVisible(false).
    /// </summary>
    [ComVisible(true)]
    [Guid("A17CF08F-EEC4-4EA5-B12C-5A603101415D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface INestedInterfaceVisibleTrue_VisibleFalse : INestedInterfaceVisibleFalse
    {
        new int Foo();
    }

    /// <summary>
    /// Nested interface with ComVisible(true) derived from an not public interface.
    /// </summary>
    [ComVisible(true)]
    [Guid("40B723E9-E1BE-4F55-99CD-D2590D191A53")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface INestedInterfaceNotPublic_VisibleTrue : INestedInterfaceVisibleTrue
    {
        new int Foo();
    }

    /// <summary>
    /// Nested class visible with ComVisible(true).
    /// </summary>
    [ComVisible(true)]
    [Guid("CF681980-CE6D-421E-8B21-AEAE3F1B7DAC")]
    public sealed class NestedClassVisibleTrueServer :
        INestedInterfaceComImport, INestedInterfaceVisibleTrue, INestedInterfaceVisibleFalse, INestedInterfaceWithoutVisible, INestedInterfaceNotPublic,
        NestedClass.INestedInterfaceNestedInClass, INestedInterfaceComImport_ComImport, INestedInterfaceVisibleTrue_ComImport, INestedInterfaceVisibleFalse_ComImport,
        INestedInterfaceVisibleTrue_VisibleTrue, INestedInterfaceVisibleFalse_VisibleTrue, INestedInterfaceVisibleTrue_VisibleFalse, INestedInterfaceNotPublic_VisibleTrue
    {
        int INestedInterfaceComImport.Foo() { return 10; }
        int INestedInterfaceVisibleTrue.Foo() { return 11; }
        int INestedInterfaceVisibleFalse.Foo() { return 12; }
        int INestedInterfaceWithoutVisible.Foo() { return 13; }
        int INestedInterfaceNotPublic.Foo() { return 14; }

        int NestedClass.INestedInterfaceNestedInClass.Foo() { return 110; }
        int INestedInterfaceComImport_ComImport.Foo() { return 111; }
        int INestedInterfaceVisibleTrue_ComImport.Foo() { return 112; }
        int INestedInterfaceVisibleFalse_ComImport.Foo() { return 113; }
        int INestedInterfaceVisibleTrue_VisibleTrue.Foo() { return 114; }
        int INestedInterfaceVisibleFalse_VisibleTrue.Foo() { return 115; }
        int INestedInterfaceVisibleTrue_VisibleFalse.Foo() { return 116; }
        int INestedInterfaceNotPublic_VisibleTrue.Foo() { return 117; }

        int Foo() { return 19; }
    }

    /// <summary>
    /// Nested class not visible with ComVisible(false).
    /// </summary>
    [ComVisible(false)]
    [Guid("6DF17EC1-A8F4-4693-B195-EDB27DF00170")]
    public sealed class NestedClassVisibleFalseServer :
        INestedInterfaceComImport, INestedInterfaceVisibleTrue, INestedInterfaceVisibleFalse, INestedInterfaceWithoutVisible, INestedInterfaceNotPublic
    {
        int INestedInterfaceComImport.Foo() { return 20; }
        int INestedInterfaceVisibleTrue.Foo() { return 21; }
        int INestedInterfaceVisibleFalse.Foo() { return 22; }
        int INestedInterfaceWithoutVisible.Foo() { return 23; }
        int INestedInterfaceNotPublic.Foo() { return 24; }
        int Foo() { return 29; }
    }

    /// <summary>
    /// Nested class not visible without ComVisible().
    /// </summary>
    [Guid("A57430B8-E0C1-486E-AE57-A15D6A729F99")]
    public sealed class NestedClassWithoutVisibleServer :
        INestedInterfaceComImport, INestedInterfaceVisibleTrue, INestedInterfaceVisibleFalse, INestedInterfaceWithoutVisible, INestedInterfaceNotPublic
    {
        int INestedInterfaceComImport.Foo() { return 30; }
        int INestedInterfaceVisibleTrue.Foo() { return 31; }
        int INestedInterfaceVisibleFalse.Foo() { return 32; }
        int INestedInterfaceWithoutVisible.Foo() { return 33; }
        int INestedInterfaceNotPublic.Foo() { return 34; }
        int Foo() { return 39; }
    }

    /// <summary>
    /// Generic visible nested class with ComVisible(true).
    /// </summary>
    [ComVisible(true)]
    [Guid("CAFBD2FF-710A-4E83-9229-42FA16963424")]
    public sealed class NestedClassGenericServer<T> : INestedInterfaceVisibleTrue, INestedInterfaceGenericVisibleTrue<T>, INestedInterfaceComImport
    {
        int INestedInterfaceComImport.Foo() { return 40; }
        int INestedInterfaceVisibleTrue.Foo() { return 41; }
        T INestedInterfaceGenericVisibleTrue<T>.Foo() { return default(T); }
        T Foo() { return default(T); }
    }


    [DllImport("ComVisibleNative")]
    public static extern int CCWTest_InterfaceComImport([MarshalAs(UnmanagedType.IUnknown)] object unk, out int fooSuccessVal);

    [DllImport("ComVisibleNative")]
    public static extern int CCWTest_InterfaceVisibleTrue([MarshalAs(UnmanagedType.IUnknown)] object unk, out int fooSuccessVal);

    [DllImport("ComVisibleNative")]
    public static extern int CCWTest_InterfaceVisibleFalse([MarshalAs(UnmanagedType.IUnknown)] object unk, out int fooSuccessVal);

    [DllImport("ComVisibleNative")]
    public static extern int CCWTest_InterfaceWithoutVisible([MarshalAs(UnmanagedType.IUnknown)] object unk, out int fooSuccessVal);

    [DllImport("ComVisibleNative")]
    public static extern int CCWTest_InterfaceNotPublic([MarshalAs(UnmanagedType.IUnknown)] object unk, out int fooSuccessVal);

    [DllImport("ComVisibleNative")]
    public static extern int CCWTest_InterfaceVisibleTrueNoGuid([MarshalAs(UnmanagedType.IUnknown)] object unk, out int fooSuccessVal);

    [DllImport("ComVisibleNative")]
    public static extern int CCWTest_InterfaceNotVisibleNoGuid([MarshalAs(UnmanagedType.IUnknown)] object unk, out int fooSuccessVal);

    [DllImport("ComVisibleNative")]
    public static extern int CCWTest_InterfaceGenericVisibleTrue([MarshalAs(UnmanagedType.IUnknown)] object unk, out int fooSuccessVal);

    [DllImport("ComVisibleNative")]
    public static extern int CCWTest_InterfaceComImport_ComImport([MarshalAs(UnmanagedType.IUnknown)] object unk, out int fooSuccessVal);

    [DllImport("ComVisibleNative")]
    public static extern int CCWTest_InterfaceVisibleTrue_ComImport([MarshalAs(UnmanagedType.IUnknown)] object unk, out int fooSuccessVal);

    [DllImport("ComVisibleNative")]
    public static extern int CCWTest_InterfaceVisibleFalse_ComImport([MarshalAs(UnmanagedType.IUnknown)] object unk, out int fooSuccessVal);

    [DllImport("ComVisibleNative")]
    public static extern int CCWTest_InterfaceVisibleTrue_VisibleTrue([MarshalAs(UnmanagedType.IUnknown)] object unk, out int fooSuccessVal);

    [DllImport("ComVisibleNative")]
    public static extern int CCWTest_InterfaceVisibleFalse_VisibleTrue([MarshalAs(UnmanagedType.IUnknown)] object unk, out int fooSuccessVal);

    [DllImport("ComVisibleNative")]
    public static extern int CCWTest_InterfaceVisibleTrue_VisibleFalse([MarshalAs(UnmanagedType.IUnknown)] object unk, out int fooSuccessVal);

    [DllImport("ComVisibleNative")]
    public static extern int CCWTest_InterfaceNotPublic_VisibleTrue([MarshalAs(UnmanagedType.IUnknown)] object unk, out int fooSuccessVal);

    [DllImport("ComVisibleNative")]
    public static extern int CCWTest_NestedInterfaceComImport([MarshalAs(UnmanagedType.IUnknown)] object unk, out int fooSuccessVal);

    [DllImport("ComVisibleNative")]
    public static extern int CCWTest_NestedInterfaceVisibleTrue([MarshalAs(UnmanagedType.IUnknown)] object unk, out int fooSuccessVal);

    [DllImport("ComVisibleNative")]
    public static extern int CCWTest_NestedInterfaceVisibleFalse([MarshalAs(UnmanagedType.IUnknown)] object unk, out int fooSuccessVal);

    [DllImport("ComVisibleNative")]
    public static extern int CCWTest_NestedInterfaceWithoutVisible([MarshalAs(UnmanagedType.IUnknown)] object unk, out int fooSuccessVal);

    [DllImport("ComVisibleNative")]
    public static extern int CCWTest_NestedInterfaceNotPublic([MarshalAs(UnmanagedType.IUnknown)] object unk, out int fooSuccessVal);

    [DllImport("ComVisibleNative")]
    public static extern int CCWTest_NestedInterfaceNestedInClass([MarshalAs(UnmanagedType.IUnknown)] object unk, out int fooSuccessVal);

    [DllImport("ComVisibleNative")]
    public static extern int CCWTest_NestedInterfaceGenericVisibleTrue([MarshalAs(UnmanagedType.IUnknown)] object unk, out int fooSuccessVal);

    [DllImport("ComVisibleNative")]
    public static extern int CCWTest_NestedInterfaceComImport_ComImport([MarshalAs(UnmanagedType.IUnknown)] object unk, out int fooSuccessVal);

    [DllImport("ComVisibleNative")]
    public static extern int CCWTest_NestedInterfaceVisibleTrue_ComImport([MarshalAs(UnmanagedType.IUnknown)] object unk, out int fooSuccessVal);

    [DllImport("ComVisibleNative")]
    public static extern int CCWTest_NestedInterfaceVisibleFalse_ComImport([MarshalAs(UnmanagedType.IUnknown)] object unk, out int fooSuccessVal);

    [DllImport("ComVisibleNative")]
    public static extern int CCWTest_NestedInterfaceVisibleTrue_VisibleTrue([MarshalAs(UnmanagedType.IUnknown)] object unk, out int fooSuccessVal);

    [DllImport("ComVisibleNative")]
    public static extern int CCWTest_NestedInterfaceVisibleFalse_VisibleTrue([MarshalAs(UnmanagedType.IUnknown)] object unk, out int fooSuccessVal);

    [DllImport("ComVisibleNative")]
    public static extern int CCWTest_NestedInterfaceVisibleTrue_VisibleFalse([MarshalAs(UnmanagedType.IUnknown)] object unk, out int fooSuccessVal);

    [DllImport("ComVisibleNative")]
    public static extern int CCWTest_NestedInterfaceNotPublic_VisibleTrue([MarshalAs(UnmanagedType.IUnknown)] object unk, out int fooSuccessVal);

    /// <summary>
    /// Test case set for ComVisible. The assembly is set as [assembly: ComVisible(false)]
    /// </summary>
    /// <returns></returns>
    ///
    [ConditionalFact(typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNotNativeAot))]
    [PlatformSpecific(TestPlatforms.Windows)]
    [SkipOnMono("Requires COM support")]
    public static void RunComVisibleTests()
    {
        int fooSuccessVal = 0;
        //
        // Tests for class with ComVisible(true)
        //
        Console.WriteLine("Class with ComVisible(true)");
        ClassVisibleTrueServer visibleBaseClass = new ClassVisibleTrueServer();

        Console.WriteLine("CCWTest_InterfaceComImport");
        Assert.Equal(Helpers.S_OK, CCWTest_InterfaceComImport((object)visibleBaseClass, out fooSuccessVal));
        Assert.Equal(1, fooSuccessVal);

        Console.WriteLine("CCWTest_InterfaceVisibleTrue");
        Assert.Equal(Helpers.S_OK, CCWTest_InterfaceVisibleTrue((object)visibleBaseClass, out fooSuccessVal));
        Assert.Equal(2, fooSuccessVal);

        Console.WriteLine("CCWTest_InterfaceVisibleFalse");
        Assert.Equal(Helpers.E_NOINTERFACE, CCWTest_InterfaceVisibleFalse((object)visibleBaseClass, out fooSuccessVal));

        Console.WriteLine("CCWTest_InterfaceWithoutVisible");
        Assert.Equal(Helpers.S_OK, CCWTest_InterfaceWithoutVisible((object)visibleBaseClass, out fooSuccessVal));
        Assert.Equal(4, fooSuccessVal);

        Console.WriteLine("CCWTest_InterfaceNotPublic");
        Assert.Equal(Helpers.E_NOINTERFACE, CCWTest_InterfaceNotPublic((object)visibleBaseClass, out fooSuccessVal));

        Console.WriteLine("CCWTest_InterfaceVisibleTrueNoGuid");
        Assert.Equal(Helpers.S_OK, CCWTest_InterfaceVisibleTrueNoGuid((object)visibleBaseClass, out fooSuccessVal));
        Assert.Equal(6, fooSuccessVal);

        Console.WriteLine("CCWTest_InterfaceNotVisibleNoGuid");
        Assert.Equal(Helpers.S_OK, CCWTest_InterfaceNotVisibleNoGuid((object)visibleBaseClass, out fooSuccessVal));
        Assert.Equal(16, fooSuccessVal);

        //
        // Tests for nested Interface in a class with ComVisible(true)
        //
        Console.WriteLine("Nested Interface in a class with ComVisible(true)");

        Console.WriteLine("CCWTest_InterfaceComImport_ComImport");
        Assert.Equal(Helpers.S_OK, CCWTest_InterfaceComImport_ComImport((object)visibleBaseClass, out fooSuccessVal));
        Assert.Equal(101, fooSuccessVal);

        Console.WriteLine("CCWTest_InterfaceVisibleTrue_ComImport");
        Assert.Equal(Helpers.S_OK, CCWTest_InterfaceVisibleTrue_ComImport((object)visibleBaseClass, out fooSuccessVal));
        Assert.Equal(102, fooSuccessVal);

        Console.WriteLine("CCWTest_InterfaceVisibleFalse_ComImport");
        Assert.Equal(Helpers.E_NOINTERFACE, CCWTest_InterfaceVisibleFalse_ComImport((object)visibleBaseClass, out fooSuccessVal));

        Console.WriteLine("CCWTest_InterfaceVisibleTrue_VisibleTrue");
        Assert.Equal(Helpers.S_OK, CCWTest_InterfaceVisibleTrue_VisibleTrue((object)visibleBaseClass, out fooSuccessVal));
        Assert.Equal(104, fooSuccessVal);

        Console.WriteLine("CCWTest_InterfaceVisibleFalse_VisibleTrue");
        Assert.Equal(Helpers.E_NOINTERFACE, CCWTest_InterfaceVisibleFalse_VisibleTrue((object)visibleBaseClass, out fooSuccessVal));

        Console.WriteLine("CCWTest_InterfaceVisibleTrue_VisibleFalse");
        Assert.Equal(Helpers.S_OK, CCWTest_InterfaceVisibleTrue_VisibleFalse((object)visibleBaseClass, out fooSuccessVal));
        Assert.Equal(106, fooSuccessVal);

        Console.WriteLine("CCWTest_InterfaceNotPublic_VisibleTrue");
        Assert.Equal(Helpers.E_NOINTERFACE, CCWTest_InterfaceNotPublic_VisibleTrue((object)visibleBaseClass, out fooSuccessVal));

        //
        // Tests for class with ComVisible(false)
        //
        Console.WriteLine("Class with ComVisible(false)");
        ClassVisibleFalseServer visibleFalseBaseClass = new ClassVisibleFalseServer();

        Console.WriteLine("CCWTest_InterfaceComImport");
        Assert.Equal(Helpers.S_OK, CCWTest_InterfaceComImport((object)visibleFalseBaseClass, out fooSuccessVal));
        Assert.Equal(120, fooSuccessVal);

        Console.WriteLine("CCWTest_InterfaceVisibleTrue");
        Assert.Equal(Helpers.S_OK, CCWTest_InterfaceVisibleTrue((object)visibleFalseBaseClass, out fooSuccessVal));
        Assert.Equal(121, fooSuccessVal);

        Console.WriteLine("CCWTest_InterfaceVisibleFalse");
        Assert.Equal(Helpers.E_NOINTERFACE, CCWTest_InterfaceVisibleFalse((object)visibleFalseBaseClass, out fooSuccessVal));

        Console.WriteLine("CCWTest_InterfaceWithoutVisible");
        Assert.Equal(Helpers.S_OK, CCWTest_InterfaceWithoutVisible((object)visibleFalseBaseClass, out fooSuccessVal));
        Assert.Equal(123, fooSuccessVal);

        Console.WriteLine("CCWTest_InterfaceNotPublic");
        Assert.Equal(Helpers.E_NOINTERFACE, CCWTest_InterfaceNotPublic((object)visibleFalseBaseClass, out fooSuccessVal));

        //
        // Tests for class without ComVisible()
        //
        Console.WriteLine("Class without ComVisible()");
        ClassWithoutVisibleServer withoutVisibleBaseClass = new ClassWithoutVisibleServer();

        Console.WriteLine("CCWTest_InterfaceComImport");
        Assert.Equal(Helpers.S_OK, CCWTest_InterfaceComImport((object)withoutVisibleBaseClass, out fooSuccessVal));
        Assert.Equal(130, fooSuccessVal);

        Console.WriteLine("CCWTest_InterfaceVisibleTrue");
        Assert.Equal(Helpers.S_OK, CCWTest_InterfaceVisibleTrue((object)withoutVisibleBaseClass, out fooSuccessVal));
        Assert.Equal(131, fooSuccessVal);

        Console.WriteLine("CCWTest_InterfaceVisibleFalse");
        Assert.Equal(Helpers.E_NOINTERFACE, CCWTest_InterfaceVisibleFalse((object)withoutVisibleBaseClass, out fooSuccessVal));

        Console.WriteLine("CCWTest_InterfaceWithoutVisible");
        Assert.Equal(Helpers.S_OK, CCWTest_InterfaceWithoutVisible((object)withoutVisibleBaseClass, out fooSuccessVal));
        Assert.Equal(133, fooSuccessVal);

        Console.WriteLine("CCWTest_InterfaceNotPublic");
        Assert.Equal(Helpers.E_NOINTERFACE, CCWTest_InterfaceNotPublic((object)withoutVisibleBaseClass, out fooSuccessVal));


        //
        // Tests for generic class with ComVisible(true)
        //
        Console.WriteLine("Generic class with ComVisible(true)");
        ClassGenericServer<int> genericServer = new ClassGenericServer<int>();

        Console.WriteLine("CCWTest_InterfaceComImport");
        Assert.Equal(Helpers.S_OK, CCWTest_InterfaceComImport((object)genericServer, out fooSuccessVal));
        Assert.Equal(140, fooSuccessVal);

        Console.WriteLine("CCWTest_InterfaceVisibleTrue");
        Assert.Equal(Helpers.S_OK, CCWTest_InterfaceVisibleTrue((object)genericServer, out fooSuccessVal));
        Assert.Equal(141, fooSuccessVal);

        Console.WriteLine("CCWTest_InterfaceGenericVisibleTrue");
        Assert.Equal(Helpers.COR_E_INVALIDOPERATION, CCWTest_InterfaceGenericVisibleTrue((object)genericServer, out fooSuccessVal));

        //
        // Tests for nested class with ComVisible(true)
        //
        Console.WriteLine("Nested class with ComVisible(true)");
        NestedClassVisibleTrueServer visibleNestedBaseClass = new NestedClassVisibleTrueServer();

        Console.WriteLine("CCWTest_NestedInterfaceComImport");
        Assert.Equal(Helpers.S_OK, CCWTest_NestedInterfaceComImport((object)visibleNestedBaseClass, out fooSuccessVal));
        Assert.Equal(10, fooSuccessVal);

        Console.WriteLine("CCWTest_NestedInterfaceVisibleTrue");
        Assert.Equal(Helpers.S_OK, CCWTest_NestedInterfaceVisibleTrue((object)visibleNestedBaseClass, out fooSuccessVal));
        Assert.Equal(11, fooSuccessVal);

        Console.WriteLine("CCWTest_NestedInterfaceVisibleFalse");
        Assert.Equal(Helpers.E_NOINTERFACE, CCWTest_NestedInterfaceVisibleFalse((object)visibleNestedBaseClass, out fooSuccessVal));

        Console.WriteLine("CCWTest_NestedInterfaceWithoutVisible");
        Assert.Equal(Helpers.S_OK, CCWTest_NestedInterfaceWithoutVisible((object)visibleNestedBaseClass, out fooSuccessVal));
        Assert.Equal(13, fooSuccessVal);

        Console.WriteLine("CCWTest_NestedInterfaceNotPublic");
        Assert.Equal(Helpers.E_NOINTERFACE, CCWTest_NestedInterfaceNotPublic((object)visibleNestedBaseClass, out fooSuccessVal));

        //
        // Tests for nested Interface in a nested class with ComVisible(true)
        //
        Console.WriteLine("Nested Interface in a nested class with ComVisible(true)");

        Console.WriteLine("CCWTest_NestedInterfaceNestedInClass");
        Assert.Equal(Helpers.S_OK, CCWTest_NestedInterfaceNestedInClass((object)visibleNestedBaseClass, out fooSuccessVal));
        Assert.Equal(110, fooSuccessVal);

        Console.WriteLine("CCWTest_NestedInterfaceComImport_ComImport");
        Assert.Equal(Helpers.S_OK, CCWTest_NestedInterfaceComImport_ComImport((object)visibleNestedBaseClass, out fooSuccessVal));
        Assert.Equal(111, fooSuccessVal);

        Console.WriteLine("CCWTest_NestedInterfaceVisibleTrue_ComImport");
        Assert.Equal(Helpers.S_OK, CCWTest_NestedInterfaceVisibleTrue_ComImport((object)visibleNestedBaseClass, out fooSuccessVal));
        Assert.Equal(112, fooSuccessVal);

        Console.WriteLine("CCWTest_NestedInterfaceVisibleFalse_ComImport");
        Assert.Equal(Helpers.E_NOINTERFACE, CCWTest_NestedInterfaceVisibleFalse_ComImport((object)visibleNestedBaseClass, out fooSuccessVal));

        Console.WriteLine("CCWTest_NestedInterfaceVisibleTrue_VisibleTrue");
        Assert.Equal(Helpers.S_OK, CCWTest_NestedInterfaceVisibleTrue_VisibleTrue((object)visibleNestedBaseClass, out fooSuccessVal));
        Assert.Equal(114, fooSuccessVal);

        Console.WriteLine("CCWTest_NestedInterfaceVisibleFalse_VisibleTrue");
        Assert.Equal(Helpers.E_NOINTERFACE, CCWTest_NestedInterfaceVisibleFalse_VisibleTrue((object)visibleNestedBaseClass, out fooSuccessVal));

        Console.WriteLine("CCWTest_NestedInterfaceVisibleTrue_VisibleFalse");
        Assert.Equal(Helpers.S_OK, CCWTest_NestedInterfaceVisibleTrue_VisibleFalse((object)visibleNestedBaseClass, out fooSuccessVal));
        Assert.Equal(116, fooSuccessVal);

        Console.WriteLine("CCWTest_NestedInterfaceNotPublic_VisibleTrue");
        Assert.Equal(Helpers.E_NOINTERFACE, CCWTest_NestedInterfaceNotPublic_VisibleTrue((object)visibleNestedBaseClass, out fooSuccessVal));

        //
        // Tests for nested class with ComVisible(false)
        //
        Console.WriteLine("Nested class with ComVisible(false)");
        NestedClassVisibleFalseServer visibleFalseNestedBaseClass = new NestedClassVisibleFalseServer();

        Console.WriteLine("CCWTest_NestedInterfaceComImport");
        Assert.Equal(Helpers.S_OK, CCWTest_NestedInterfaceComImport((object)visibleFalseNestedBaseClass, out fooSuccessVal));
        Assert.Equal(20, fooSuccessVal);

        Console.WriteLine("CCWTest_NestedInterfaceVisibleTrue");
        Assert.Equal(Helpers.S_OK, CCWTest_NestedInterfaceVisibleTrue((object)visibleFalseNestedBaseClass, out fooSuccessVal));
        Assert.Equal(21, fooSuccessVal);

        Console.WriteLine("CCWTest_NestedInterfaceVisibleFalse");
        Assert.Equal(Helpers.E_NOINTERFACE, CCWTest_NestedInterfaceVisibleFalse((object)visibleFalseNestedBaseClass, out fooSuccessVal));

        Console.WriteLine("CCWTest_NestedInterfaceWithoutVisible");
        Assert.Equal(Helpers.S_OK, CCWTest_NestedInterfaceWithoutVisible((object)visibleFalseNestedBaseClass, out fooSuccessVal));
        Assert.Equal(23, fooSuccessVal);

        Console.WriteLine("CCWTest_NestedInterfaceNotPublic");
        Assert.Equal(Helpers.E_NOINTERFACE, CCWTest_NestedInterfaceNotPublic((object)visibleFalseNestedBaseClass, out fooSuccessVal));

        //
        // Tests for nested class without ComVisible()
        //
        Console.WriteLine("Nested class without ComVisible()");
        NestedClassWithoutVisibleServer withoutVisibleNestedBaseClass = new NestedClassWithoutVisibleServer();

        Console.WriteLine("CCWTest_NestedInterfaceComImport");
        Assert.Equal(Helpers.S_OK, CCWTest_NestedInterfaceComImport((object)withoutVisibleNestedBaseClass, out fooSuccessVal));
        Assert.Equal(30, fooSuccessVal);

        Console.WriteLine("CCWTest_NestedInterfaceVisibleTrue");
        Assert.Equal(Helpers.S_OK, CCWTest_NestedInterfaceVisibleTrue((object)withoutVisibleNestedBaseClass, out fooSuccessVal));
        Assert.Equal(31, fooSuccessVal);

        Console.WriteLine("CCWTest_NestedInterfaceVisibleFalse");
        Assert.Equal(Helpers.E_NOINTERFACE, CCWTest_NestedInterfaceVisibleFalse((object)withoutVisibleNestedBaseClass, out fooSuccessVal));

        Console.WriteLine("CCWTest_NestedInterfaceWithoutVisible");
        Assert.Equal(Helpers.S_OK, CCWTest_NestedInterfaceWithoutVisible((object)withoutVisibleNestedBaseClass, out fooSuccessVal));
        Assert.Equal(33, fooSuccessVal);

        Console.WriteLine("CCWTest_NestedInterfaceNotPublic");
        Assert.Equal(Helpers.E_NOINTERFACE, CCWTest_NestedInterfaceNotPublic((object)withoutVisibleNestedBaseClass, out fooSuccessVal));

        //
        // Tests for generic nested class with ComVisible(true)
        //
        Console.WriteLine("Nested generic class with ComVisible(true)");
        NestedClassGenericServer<int> nestedGenericServer = new NestedClassGenericServer<int>();

        Console.WriteLine("CCWTest_NestedInterfaceComImport");
        Assert.Equal(Helpers.S_OK, CCWTest_NestedInterfaceComImport((object)nestedGenericServer, out fooSuccessVal));
        Assert.Equal(40, fooSuccessVal);

        Console.WriteLine("CCWTest_NestedInterfaceVisibleTrue");
        Assert.Equal(Helpers.S_OK, CCWTest_NestedInterfaceVisibleTrue((object)nestedGenericServer, out fooSuccessVal));
        Assert.Equal(41, fooSuccessVal);

        Console.WriteLine("CCWTest_NestedInterfaceGenericVisibleTrue");
        Assert.Equal(Helpers.E_NOINTERFACE, CCWTest_NestedInterfaceGenericVisibleTrue((object)nestedGenericServer, out fooSuccessVal));
    }
}
