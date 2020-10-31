// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable 0183

using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
// primitives / CLR Types

// interfaces 
public interface IEmpty { }
public interface INotEmpty
{
    void DoNothing();
}

// generic interfaces 
public interface IEmptyGen<T> { }
public interface INotEmptyGen<T>
{
    void DoNothing();
}

// struct 
public struct EmptyStruct { }
public struct NotEmptyStruct
{
    public int Field;
}

public struct NotEmptyStructQ
{
    public int? Field;
}

public struct NotEmptyStructA
{
    public int[] Field;
}

public struct NotEmptyStructQA
{
    public int?[] Field;
}

// generic structs 
public struct EmptyStructGen<T> { }
public struct NotEmptyStructGen<T>
{
    public T Field;
}

public struct NotEmptyStructConstrainedGen<T> where T : struct
{
    public T Field;
}

public struct NotEmptyStructConstrainedGenA<T> where T : struct
{
    public T[] Field;
}

public struct NotEmptyStructConstrainedGenQ<T> where T : struct
{
    public T? Field;
}

public struct NotEmptyStructConstrainedGenQA<T> where T : struct
{
    public T?[] Field;
}

// nested struct 
public struct NestedStruct
{
    public struct Nested { }
}

public struct NestedStructGen<T>
{
    public struct Nested { }
}


// struct with Field Offset
[StructLayout(LayoutKind.Explicit)]
public struct ExplicitFieldOffsetStruct
{
    [FieldOffset(0)]
    public int Field00;
    [FieldOffset(0x0f)]
    public int Field15;
}

// struct with Attributes
internal struct MarshalAsStruct
{
    [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
    public string StringField;
}

// struct implement interfaces
internal struct ImplementOneInterface : IEmpty { }

internal struct ImplementTwoInterface : IEmpty, INotEmpty
{
    public void DoNothing() { }
}

internal struct ImplementOneInterfaceGen<T> : IEmptyGen<T> { }

internal struct ImplementTwoInterfaceGen<T> : IEmptyGen<T>, INotEmptyGen<T>
{
    public void DoNothing() { }
}

internal struct ImplementAllInterface<T> : IEmpty, INotEmpty, IEmptyGen<T>, INotEmptyGen<T>
{
    public void DoNothing() { }
    void INotEmptyGen<T>.DoNothing() { }
}

// enums
public enum IntE { start = 1, }
public enum ByteE : byte { start = 1, }
public enum LongE : long { start = 1, }


// other intersting structs
public struct WithMultipleGCHandleStruct
{
    public GCHandle H1;
    public GCHandle H2;
    public GCHandle H3;
    public GCHandle H4;
    public GCHandle H5;
}

public struct WithOnlyFXTypeStruct
{
    public Guid GUID;
    public decimal DECIMAL;
}


public struct MixedAllStruct
{
    public int INT;
    public int? IntQ;
    public int?[] IntQA;
    public string STRING;
    public IntE INTE;
    public EmptyClass EMPTYCLASS;
    public IEmpty IEMPTY;
    public EmptyStruct EMPTYSTRUCT;
    public IEmptyGen<int> IEMPTYGEN;
    public EmptyStructGen<int> EMPTYSTRUCTGEN;
    public WithOnlyFXTypeStruct WITHONLYFXTYPESTRUCT;
    public GCHandle GCHANDLE;
}


// other types
public struct EmptyClass { }
public struct NotEmptyClass
{
    public int Field;
}

public struct EmptyClassGen<T> { }
public struct NotEmptyClassGen<T>
{
    public T Field;
}

public struct NotEmptyClassConstrainedGen<T> where T : class
{
    public T Field;
}
public struct NestedClass
{
    public struct Nested { }
}

public struct NestedClassGen<T>
{
    public struct Nested { }
}

internal class ImplementOneInterfaceC : IEmpty { }

internal class ImplementTwoInterfaceC : IEmpty, INotEmpty
{
    public void DoNothing() { }
}

internal class ImplementOneInterfaceGenC<T> : IEmptyGen<T> { }

internal class ImplementTwoInterfaceGenC<T> : IEmptyGen<T>, INotEmptyGen<T>
{
    public void DoNothing() { }
}

internal class ImplementAllInterfaceC<T> : IEmpty, INotEmpty, IEmptyGen<T>, INotEmptyGen<T>
{
    public void DoNothing() { }
    void INotEmptyGen<T>.DoNothing() { }
}

public sealed class SealedClass { }

public delegate void SimpleDelegate();
public delegate void GenericDelegate<T>();


// ExitCode
public static class ExitCode
{
    public static int Failed = 101;
    public static int Passed = 100;
}

// Create Value Instance
internal static class Helper
{
    public static GCHandle GCHANDLE;

    static Helper()
    {
        GCHANDLE = GCHandle.Alloc(Console.Out);

        AssemblyLoadContext currentContext = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly());
        if (currentContext != null)
        {
            currentContext.Unloading += Context_Unloading;
        }
    }

    private static void Context_Unloading(AssemblyLoadContext obj)
    {
        GCHANDLE.Free();
    }

    public static char Create(char val) { return 'c'; }
    public static bool Create(bool val) { return true; }
    public static byte Create(byte val) { return 0x08; }
    public static sbyte Create(sbyte val) { return -0x0e; }
    public static short Create(short val) { return -0x0f; }
    public static ushort Create(ushort val) { return 0xff; }
    public static int Create(int val) { return 100; }
    public static uint Create(uint val) { return 200; }
    public static long Create(long val) { return int.MaxValue; }
    public static ulong Create(ulong val) { return 300; }
    public static float Create(float val) { return 1.15f; }
    public static double Create(double val) { return 0.05; }
    public static decimal Create(decimal val) { return 1.0M; }

    public static IntPtr Create(IntPtr val) { return (IntPtr)1000; }
    public static UIntPtr Create(UIntPtr val) { return (UIntPtr)2000; }
    public static Guid Create(Guid val) { return new Guid("00020810-0001-0000-C000-000000000046"); }
    public static GCHandle Create(GCHandle val) { return GCHANDLE; }
    public static ByteE Create(ByteE val) { return (ByteE)9; }
    public static IntE Create(IntE val) { return (IntE)55; }
    public static LongE Create(LongE val) { return (LongE)34; }
    public static EmptyStruct Create(EmptyStruct val) { return new EmptyStruct(); }
    public static NotEmptyStruct Create(NotEmptyStruct val) { NotEmptyStruct ne = new NotEmptyStruct(); ne.Field = 100; return ne; }
    public static NotEmptyStructQ Create(NotEmptyStructQ val) { NotEmptyStructQ neq = new NotEmptyStructQ(); neq.Field = 101; return neq; }
    public static NotEmptyStructA Create(NotEmptyStructA val) { NotEmptyStructA nea = new NotEmptyStructA(); nea.Field = new int[] { 10 }; return nea; }
    public static NotEmptyStructQA Create(NotEmptyStructQA val) { NotEmptyStructQA neq = new NotEmptyStructQA(); neq.Field = new int?[] { 10 }; return neq; }
    public static EmptyStructGen<int> Create(EmptyStructGen<int> val) { return new EmptyStructGen<int>(); }
    public static NotEmptyStructGen<int> Create(NotEmptyStructGen<int> val) { NotEmptyStructGen<int> ne = new NotEmptyStructGen<int>(); ne.Field = 88; return ne; }
    public static NotEmptyStructConstrainedGen<int> Create(NotEmptyStructConstrainedGen<int> val) { NotEmptyStructConstrainedGen<int> ne = new NotEmptyStructConstrainedGen<int>(); ne.Field = 1010; return ne; }
    public static NotEmptyStructConstrainedGenA<int> Create(NotEmptyStructConstrainedGenA<int> val) { NotEmptyStructConstrainedGenA<int> neq = new NotEmptyStructConstrainedGenA<int>(); neq.Field = new int[] { 11 }; return neq; }
    public static NotEmptyStructConstrainedGenQ<int> Create(NotEmptyStructConstrainedGenQ<int> val) { NotEmptyStructConstrainedGenQ<int> neq = new NotEmptyStructConstrainedGenQ<int>(); neq.Field = 12; return neq; }
    public static NotEmptyStructConstrainedGenQA<int> Create(NotEmptyStructConstrainedGenQA<int> val) { NotEmptyStructConstrainedGenQA<int> neq = new NotEmptyStructConstrainedGenQA<int>(); neq.Field = new int?[] { 17 }; return neq; }
    public static NestedStruct Create(NestedStruct val) { NestedStruct ns = new NestedStruct(); return ns; }
    public static NestedStructGen<int> Create(NestedStructGen<int> val) { NestedStructGen<int> nsg = new NestedStructGen<int>(); return nsg; }
    public static ExplicitFieldOffsetStruct Create(ExplicitFieldOffsetStruct val) { ExplicitFieldOffsetStruct epl = new ExplicitFieldOffsetStruct(); epl.Field00 = 40; epl.Field15 = 15; return epl; }
    public static MarshalAsStruct Create(MarshalAsStruct val) { MarshalAsStruct ma = new MarshalAsStruct(); ma.StringField = "Nullable"; return ma; }
    public static ImplementOneInterface Create(ImplementOneInterface val) { ImplementOneInterface imp = new ImplementOneInterface(); return imp; }
    public static ImplementTwoInterface Create(ImplementTwoInterface val) { ImplementTwoInterface imp = new ImplementTwoInterface(); return imp; }
    public static ImplementOneInterfaceGen<int> Create(ImplementOneInterfaceGen<int> val) { ImplementOneInterfaceGen<int> imp = new ImplementOneInterfaceGen<int>(); return imp; }
    public static ImplementTwoInterfaceGen<int> Create(ImplementTwoInterfaceGen<int> val) { ImplementTwoInterfaceGen<int> imp = new ImplementTwoInterfaceGen<int>(); return imp; }
    public static ImplementAllInterface<int> Create(ImplementAllInterface<int> val) { ImplementAllInterface<int> imp = new ImplementAllInterface<int>(); return imp; }
    public static WithMultipleGCHandleStruct Create(WithMultipleGCHandleStruct val)
    { WithMultipleGCHandleStruct mgch = new WithMultipleGCHandleStruct(); mgch.H1 = GCHANDLE; mgch.H2 = GCHANDLE; mgch.H3 = GCHANDLE; mgch.H4 = GCHANDLE; mgch.H5 = GCHANDLE; return mgch; }
    public static WithOnlyFXTypeStruct Create(WithOnlyFXTypeStruct val) { WithOnlyFXTypeStruct wofx = new WithOnlyFXTypeStruct(); wofx.DECIMAL = 50.0m; wofx.GUID = Create(default(Guid)); return wofx; }
    public static MixedAllStruct Create(MixedAllStruct val)
    {
        MixedAllStruct mas;
        mas.INT = 10;
        mas.IntQ = null;
        mas.IntQA = new int?[] { 10 };
        mas.STRING = "Nullable";
        mas.INTE = Create(default(IntE));
        mas.EMPTYCLASS = new EmptyClass();
        mas.IEMPTY = Create(default(ImplementOneInterface));
        mas.EMPTYSTRUCT = Create(default(EmptyStruct));
        mas.IEMPTYGEN = Create(default(ImplementOneInterfaceGen<int>));
        mas.EMPTYSTRUCTGEN = Create(default(EmptyStructGen<int>));
        mas.WITHONLYFXTYPESTRUCT = Create(default(WithOnlyFXTypeStruct));
        mas.GCHANDLE = Create(default(GCHandle));

        return mas;
    }

    public static bool Compare(char val, char val1) { return val == val1; }
    public static bool Compare(bool val, bool val1) { return val == val1; }
    public static bool Compare(byte val, byte val1) { return val == val1; }
    public static bool Compare(sbyte val, sbyte val1) { return val == val1; }
    public static bool Compare(short val, short val1) { return val == val1; }
    public static bool Compare(ushort val, ushort val1) { return val == val1; }
    public static bool Compare(int val, int val1) { return val == val1; }
    public static bool Compare(uint val, uint val1) { return val == val1; }
    public static bool Compare(long val, long val1) { return val == val1; }
    public static bool Compare(ulong val, ulong val1) { return val == val1; }
    public static bool Compare(float val, float val1) { return val == val1; }
    public static bool Compare(double val, double val1) { return val == val1; }
    public static bool Compare(decimal val, decimal val1) { return val == val1; }

    public static bool Compare(IntPtr val, IntPtr val1) { return val == val1; }
    public static bool Compare(UIntPtr val, UIntPtr val1) { return val == val1; }
    public static bool Compare(Guid val, Guid val1) { return val == val1; }
    public static bool Compare(GCHandle val, GCHandle val1) { return val == val1; }
    public static bool Compare(ByteE val, ByteE val1) { return val == val1; }
    public static bool Compare(IntE val, IntE val1) { return val == val1; }
    public static bool Compare(LongE val, LongE val1) { return val == val1; }
    public static bool Compare(EmptyStruct val, EmptyStruct val1) { return val.Equals(val1); }
    public static bool Compare(NotEmptyStruct val, NotEmptyStruct val1) { return val.Field == val1.Field; }
    public static bool Compare(NotEmptyStructQ val, NotEmptyStructQ val1) { return val.Field == val1.Field; }
    public static bool Compare(NotEmptyStructA val, NotEmptyStructA val1) { return val.Field[0] == val1.Field[0]; }
    public static bool Compare(NotEmptyStructQA val, NotEmptyStructQA val1) { return val.Field[0] == val1.Field[0]; }
    public static bool Compare(EmptyStructGen<int> val, EmptyStructGen<int> val1) { return val.Equals(val1); }
    public static bool Compare(NotEmptyStructGen<int> val, NotEmptyStructGen<int> val1) { return val.Field == val1.Field; }
    public static bool Compare(NotEmptyStructConstrainedGen<int> val, NotEmptyStructConstrainedGen<int> val1) { return val.Field == val1.Field; }
    public static bool Compare(NotEmptyStructConstrainedGenA<int> val, NotEmptyStructConstrainedGenA<int> val1) { return val.Field[0] == val1.Field[0]; }
    public static bool Compare(NotEmptyStructConstrainedGenQ<int> val, NotEmptyStructConstrainedGenQ<int> val1) { return val.Field == val1.Field; }
    public static bool Compare(NotEmptyStructConstrainedGenQA<int> val, NotEmptyStructConstrainedGenQA<int> val1) { return val.Field[0] == val1.Field[0]; }
    public static bool Compare(NestedStruct val, NestedStruct val1) { return val.Equals(val1); }
    public static bool Compare(NestedStructGen<int> val, NestedStructGen<int> val1) { return val.Equals(val1); }
    public static bool Compare(ExplicitFieldOffsetStruct val, ExplicitFieldOffsetStruct val1) { return (val.Field00 == val1.Field00) && (val.Field15 == val1.Field15); }
    public static bool Compare(MarshalAsStruct val, MarshalAsStruct val1) { return val.Equals(val1); }
    public static bool Compare(ImplementOneInterface val, ImplementOneInterface val1) { return (val is IEmpty) && val.Equals(val1); }
    public static bool Compare(ImplementTwoInterface val, ImplementTwoInterface val1) { return (val is IEmpty) && val is INotEmpty && val.Equals(val1); }
    public static bool Compare(ImplementOneInterfaceGen<int> val, ImplementOneInterfaceGen<int> val1) { return val is IEmptyGen<int> && val.Equals(val1); }
    public static bool Compare(ImplementTwoInterfaceGen<int> val, ImplementTwoInterfaceGen<int> val1) { return val is IEmptyGen<int> && val is INotEmptyGen<int> && val.Equals(val1); }
    public static bool Compare(ImplementAllInterface<int> val, ImplementAllInterface<int> val1) { return val is IEmpty && val is INotEmpty && val is IEmptyGen<int> && val is INotEmptyGen<int> && val.Equals(val1); }
    public static bool Compare(WithMultipleGCHandleStruct val, WithMultipleGCHandleStruct val1)
    { return val.H1 == val1.H1 && val.H2 == val1.H2 && val.H3 == val1.H3 && val.H4 == val1.H4 && val.H5 == val1.H5; }
    public static bool Compare(WithOnlyFXTypeStruct val, WithOnlyFXTypeStruct val1) { return val.GUID == val1.GUID && val.DECIMAL == val1.DECIMAL; }
    public static bool Compare(MixedAllStruct val, MixedAllStruct val1)
    {
        return val.INT == val1.INT &&
        val.IntQ == val1.IntQ &&
        val.IntQA[0] == val1.IntQA[0] &&
        val.STRING == val1.STRING &&
        val.INTE == val1.INTE &&
        val.EMPTYCLASS.Equals(val1.EMPTYCLASS) &&
        val.IEMPTY.Equals(val1.IEMPTY) &&
        Compare(val.EMPTYSTRUCT, val1.EMPTYSTRUCT) &&
        val.IEMPTYGEN.Equals(val1.IEMPTYGEN) &&
        Compare(val.EMPTYSTRUCTGEN, val1.EMPTYSTRUCTGEN) &&
        Compare(val.WITHONLYFXTYPESTRUCT, val1.WITHONLYFXTYPESTRUCT) &&
       Compare(val.GCHANDLE, val1.GCHANDLE);
    }

    public static bool Compare(char? val, char val1) { return Compare(val.Value, val1); }
    public static bool Compare(bool? val, bool val1) { return Compare(val.Value, val1); }
    public static bool Compare(byte? val, byte val1) { return Compare(val.Value, val1); }
    public static bool Compare(sbyte? val, sbyte val1) { return Compare(val.Value, val1); }
    public static bool Compare(short? val, short val1) { return Compare(val.Value, val1); }
    public static bool Compare(ushort? val, ushort val1) { return Compare(val.Value, val1); }
    public static bool Compare(int? val, int val1) { return Compare(val.Value, val1); }
    public static bool Compare(uint? val, uint val1) { return Compare(val.Value, val1); }
    public static bool Compare(long? val, long val1) { return Compare(val.Value, val1); }
    public static bool Compare(ulong? val, ulong val1) { return Compare(val.Value, val1); }
    public static bool Compare(float? val, float val1) { return Compare(val.Value, val1); }
    public static bool Compare(double? val, double val1) { return Compare(val.Value, val1); }
    public static bool Compare(decimal? val, decimal val1) { return Compare(val.Value, val1); }

    public static bool Compare(IntPtr? val, IntPtr val1) { return Compare(val.Value, val1); }
    public static bool Compare(UIntPtr? val, UIntPtr val1) { return Compare(val.Value, val1); }
    public static bool Compare(Guid? val, Guid val1) { return Compare(val.Value, val1); }
    public static bool Compare(GCHandle? val, GCHandle val1) { return Compare(val.Value, val1); }
    public static bool Compare(ByteE? val, ByteE val1) { return Compare(val.Value, val1); }
    public static bool Compare(IntE? val, IntE val1) { return Compare(val.Value, val1); }
    public static bool Compare(LongE? val, LongE val1) { return Compare(val.Value, val1); }
    public static bool Compare(EmptyStruct? val, EmptyStruct val1) { return Compare(val.Value, val1); }
    public static bool Compare(NotEmptyStruct? val, NotEmptyStruct val1) { return Compare(val.Value, val1); }
    public static bool Compare(NotEmptyStructQ? val, NotEmptyStructQ val1) { return Compare(val.Value, val1); }
    public static bool Compare(NotEmptyStructA? val, NotEmptyStructA val1) { return Compare(val.Value, val1); }
    public static bool Compare(NotEmptyStructQA? val, NotEmptyStructQA val1) { return Compare(val.Value, val1); }
    public static bool Compare(EmptyStructGen<int>? val, EmptyStructGen<int> val1) { return Compare(val.Value, val1); }
    public static bool Compare(NotEmptyStructGen<int>? val, NotEmptyStructGen<int> val1) { return Compare(val.Value, val1); }
    public static bool Compare(NotEmptyStructConstrainedGen<int>? val, NotEmptyStructConstrainedGen<int> val1) { return Compare(val.Value, val1); }
    public static bool Compare(NotEmptyStructConstrainedGenA<int>? val, NotEmptyStructConstrainedGenA<int> val1) { return Compare(val.Value, val1); }
    public static bool Compare(NotEmptyStructConstrainedGenQ<int>? val, NotEmptyStructConstrainedGenQ<int> val1) { return Compare(val.Value, val1); }
    public static bool Compare(NotEmptyStructConstrainedGenQA<int>? val, NotEmptyStructConstrainedGenQA<int> val1) { return Compare(val.Value, val1); }
    public static bool Compare(NestedStruct? val, NestedStruct val1) { return Compare(val.Value, val1); }
    public static bool Compare(NestedStructGen<int>? val, NestedStructGen<int> val1) { return Compare(val.Value, val1); }
    public static bool Compare(ExplicitFieldOffsetStruct? val, ExplicitFieldOffsetStruct val1) { return Compare(val.Value, val1); }
    public static bool Compare(MarshalAsStruct? val, MarshalAsStruct val1) { return Compare(val.Value, val1); }
    public static bool Compare(ImplementOneInterface? val, ImplementOneInterface val1) { return Compare(val.Value, val1); }
    public static bool Compare(ImplementTwoInterface? val, ImplementTwoInterface val1) { return Compare(val.Value, val1); }
    public static bool Compare(ImplementOneInterfaceGen<int>? val, ImplementOneInterfaceGen<int> val1) { return Compare(val.Value, val1); }
    public static bool Compare(ImplementTwoInterfaceGen<int>? val, ImplementTwoInterfaceGen<int> val1) { return Compare(val.Value, val1); }
    public static bool Compare(ImplementAllInterface<int>? val, ImplementAllInterface<int> val1) { return Compare(val.Value, val1); }
    public static bool Compare(WithMultipleGCHandleStruct? val, WithMultipleGCHandleStruct val1) { return Compare(val.Value, val1); }
    public static bool Compare(WithOnlyFXTypeStruct? val, WithOnlyFXTypeStruct val1) { return Compare(val.Value, val1); }
    public static bool Compare(MixedAllStruct? val, MixedAllStruct val1) { return Compare(val.Value, val1); }
}
