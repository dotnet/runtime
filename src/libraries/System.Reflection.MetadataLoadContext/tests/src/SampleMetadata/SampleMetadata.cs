// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable 0169 // Field never used
#pragma warning disable 0649 // Field never assigned
#pragma warning disable 0618 // Obsolete
#pragma warning disable 0067 // Event is never used

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
public class TopLevelType
{
}

namespace SampleMetadata
{
    public interface Interface1 { }
    public interface Interface2 { }
    public interface Interface3 { }
    public interface Interface4 { }
    public interface Interface5 { }
    public interface Interface123 : Interface1, Interface2, Interface3 { }
    public interface InterfaceI5 : Interface5 { }
    public interface InterfaceII5 : InterfaceI5 { }
    public interface IGeneric1<T> { }
    public interface IGeneric2<T> { }

    public class GenericClass1<T> { }
    public class GenericClass2<T, U> { }
    public class GenericClass3<T, U, V> { }
    public class GenericClass4<T, U> : IGeneric1<T>, IGeneric2<U> { }
    public class GenericClass5<T, U, V, W, X> { }

    public class OuterType1
    {
        public class InnerType1 { }
    }

    public enum MyColor
    {
        Red = 1,
        Green = 2,
        Blue = 3,
    }

    public class Base1 { }
    public class Derived1 : Base1 { }
    public class Derived2 : GenericClass1<int> { }
    public class Derived3<T, U> : GenericClass2<U, T> { }

    public class CInterfaceImplementerI1I2 : Interface1, Interface2 { }
    public class CInterfaceImplementerC12I2I3 : CInterfaceImplementerI1I2, Interface2, Interface3 { }
    public class CInterfaceImplementerI123 : Interface123 { }
    public class CInterfaceImplementerII5 : InterfaceII5 { }

    public interface IConstrained1 { }
    public interface IConstrained2<I> { }
    public class CConstrained1 { }

    public class GenericClassWithNoConstraint<T> { }
    public class GenericClassWithClassConstraint<T> where T : class { }
    public class GenericClassWithStructConstraint<T> where T : struct { }
    public class GenericClassWithNewConstraint<T> where T : new() { }
    public class GenericClassWithEnumConstraint<T> where T : Enum { }
    public class GenericClassWithTypeConstraints<T> where T : CConstrained1, IConstrained1, IConstrained2<T> { }
    public class GenericClassWithInterfaceConstraints<T> where T : IConstrained1, IConstrained2<T> { }
    public class GenericClassWithQuirkyConstraints1<T, U> where T : U where U : CConstrained1, IConstrained1 { }
    public class GenericClassWithQuirkyConstraints2<T, U> where T : U where U : class, IConstrained1 { }

    public class GenericMethodWithTypeConstraints<T>
    {
        public void Foo<M, N>() where M : IConstrained2<N> where N : IConstrained2<T> { }
    }

    public enum EU1 : byte { }
    public enum EI1 : sbyte { }
    public enum EU2 : ushort { }
    public enum EI2 : short { }
    public enum EU4 : uint { }
    public enum EI4 : int { }
    public enum EU8 : ulong { }
    public enum EI8 : long { }

    public enum E_2_I4 : int { min=int.MinValue, zero=0, one=1, max=int.MaxValue}
    public enum E_2_U4 : uint { min = uint.MinValue, zero = 0, one = 1, max = uint.MaxValue }

    public class GenericEnumContainer<T>
    {
        public enum GenericEnum : short { }
    }

    public unsafe class ClassWithFields1<T>
    {
        public int Field1;
        protected GenericClass2<int, string> Field2;
        private Interface1 Field3;
        internal int[] Field4;
        protected internal int* Field5;
        private protected T Field6;

        public static byte SField1;
        protected static GenericClass2<int, T> SField2;
        private static Interface1 SField3;
        internal static int[,] SField4;
        protected internal static int* SField5;
        private protected static T SField6;

        private readonly IEnumerable<int> ReadOnlyField1;

        private const int ConstField1 = 42;
        public volatile int VolatileField1;
    }

    public class ClassWithConstructor1<T>
    {
        public ClassWithConstructor1(int x, T t) => throw null!;
    }

    public class ClassWithMethods1<T>
    {
        public bool Method1(int x, T t) => throw null!;

        public void TestPrimitives1(bool bo, byte b, char c, short s, int i, long l, IntPtr ip, sbyte sb, ushort us, uint ui, ulong ul, UIntPtr uip, float fl, double db, object o, string str, TypedReference tr) => throw null!;
    }

    public class ClassWithGenericMethods1
    {
        public void GenericMethod1<M, N>() => throw null!;
    }

    public class GenericClassWithGenericMethods1<T, U>
    {
        public void GenericMethod1<M, N>(GenericClass5<N, M[], IEnumerable<U>, T[,], int> g) => throw null!;
    }

    public class ClassWithLiteralFields
    {
        public static int NotLiteral;
        public static readonly int NotLiteralJustReadOnly = 3;

        public const bool LitBool1 = false;
        public const bool LitBool2 = true;

        public const char LitChar1 = char.MinValue;
        public const char LitChar2 = 'A';
        public const char LitChar3 = char.MaxValue;

        public const byte LitByte1 = byte.MinValue;
        public const byte LitByte2 = 14;
        public const byte LitByte3 = byte.MaxValue;

        public const sbyte LitSByte1 = sbyte.MinValue;
        public const sbyte LitSByte2 = 23;
        public const sbyte LitSByte3 = sbyte.MaxValue;

        public const short LitShort1 = short.MinValue;
        public const short LitShort2 = 1973;
        public const short LitShort3 = short.MaxValue;

        public const ushort LitUShort1 = ushort.MinValue;
        public const ushort LitUShort2 = 59392;
        public const ushort LitUShort3 = ushort.MaxValue;

        public const int LitInt1 = int.MinValue;
        public const int LitInt2 = 4837878;
        public const int LitInt3 = int.MaxValue;

        public const uint LitUInt1 = uint.MinValue;
        public const uint LitUInt2 = 12334432;
        public const uint LitUInt3 = uint.MaxValue;

        public const long LitLong1 = long.MinValue;
        public const long LitLong2 = 737687687687;
        public const long LitLong3 = long.MaxValue;

        public const ulong LitULong1 = ulong.MinValue;
        public const ulong LitULong2 = 878784583839;
        public const ulong LitULong3 = ulong.MaxValue;

        public const float LitSingle1 = 3.4f;
        public const float LitSingle2 = -3.4f;

        public const double LitDouble1 = 847.33;
        public const double LitDouble2 = -847.33;

        public const string LitString1 = "Hello";
        public const string LitString2 = null;

        public const object LitObject = null;

        public const MyColor LitMyColor1 = MyColor.Green;
    }

    public class ParametersWithDefaultValues
    {
        public void Foo1(int i) { }
        public void Foo2([Optional] int i) { }
        public void Foo3(int i = 42) { }
        public void Foo4(short s = -34) { }
        public void Foo5(decimal d = 1234m) { }
        public void Foo6([DateTimeConstant(ticks: 8736726782)] DateTime dt) { }
        public void Foo7(string s1 = "foo", string s2 = "", string s3 = null) { }
        public void Foo8(Action a = null) { }
    }

    public class ParametersWithPseudoCustomtAttributes
    {
        public void Foo([In] int i, [Out] out object o, [Optional] object opt, [MarshalAs(UnmanagedType.I4)] int fa) => throw null!;
    }

    public class SampleCustomAttribute : Attribute
    {
        public SampleCustomAttribute(int x) { Argument = x; }
        public SampleCustomAttribute(string x) { Argument = x; }
        public SampleCustomAttribute(Type x) { Argument = x; }
        public SampleCustomAttribute(object x) { Argument = x; }
        public SampleCustomAttribute(object[] x) { Argument = x; }

        public object Argument;
    }

    public class AttributeHolder1
    {
        [SampleCustom(42)]
        public class N1 { }

        [SampleCustom((object)42)]
        public class N2 { }

        [SampleCustom((object)(typeof(IList<string>)))]
        public class N3 { }

        [SampleCustom((object)MyColor.Green)]
        public class N4 { }

        [SampleCustom((object)(new int[] { 6, 7, 8 }))]
        public class N5 { }

        [SampleCustom((object)null)]
        public class N6 { }

        [SampleCustom((string)null)]
        public class N7 { }

        [SampleCustom((Type)null)]
        public class N8 { }

        [SampleCustom((object[])null)]
        public class N9 { }

        [SampleCustom(new BindingFlags[] { BindingFlags.DeclaredOnly, BindingFlags.ExactBinding })]
        public class N10 { }

        [SampleCustom(new object[] { 42, "Hello", typeof(IList<string>), BindingFlags.ExactBinding })]
        public class N11 { }

        [SampleCustom("Yeah")]
        public class N12 { }

        [SampleCustom(default(EU1))]
        public class N13 { }

        [SampleCustom(default(EI1))]
        public class N14 { }

        [SampleCustom(default(EU2))]
        public class N15 { }

        [SampleCustom(default(EI2))]
        public class N16 { }

        [SampleCustom(default(EU4))]
        public class N17 { }

        [SampleCustom(default(EI4))]
        public class N18 { }

        [SampleCustom(default(EU8))]
        public class N19 { }

        [SampleCustom(default(EI8))]
        public class N20 { }
    }

    [Guid("90B3D33A-4E96-49B9-8912-4D957AB45461")]
    public class HoldsAttributeDefinedInAnotherAssembly { }

    public class CaWithNamedArguments : Attribute
    {
        public int MyField;
        public int MyProperty { get; set; }
    }

    public class HoldsCaWithNamedArguments
    {
        [CaWithNamedArguments(MyField = 4)]
        public class N1 { }

        [CaWithNamedArguments(MyProperty = 8)]
        public class N2 { }
    }

    public ref struct SampleByRefLikeStruct1 { }
    public ref struct SampleByRefLikeStruct2<T> { }
    [SampleCustom("Yeah")]
    public ref struct SampleByRefLikeStruct3 { }

    [Guid("E73CFD63-6BD8-432D-A71B-E1E54AD55914")]
    public class ClassWithGuid { };

    [ComImport]
    [Guid("E73CFD63-6BD8-432D-A71B-E1E54AD55914")]
    public class ClassWithComImport { };

    public class DllImportHolders
    {
        [DllImport("Foo.dll")]
        public static extern void Foo1();

        [DllImport("Foo.dll", CharSet = CharSet.Ansi)]
        public static extern void Foo2();

        [DllImport("Foo.dll", CharSet = CharSet.Auto)]
        public static extern void Foo3();

        [DllImport("Foo.dll", CharSet = CharSet.Unicode)]
        public static extern void Foo4();

        [DllImport("Foo.dll", EntryPoint = "GoHereInstead")]
        public static extern void Foo5();

        [DllImport("Foo.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Foo6();

        [DllImport("Foo.dll", CallingConvention = CallingConvention.FastCall)]
        public static extern void Foo7();

        [DllImport("Foo.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern void Foo8();

        [DllImport("Foo.dll", CallingConvention = CallingConvention.ThisCall)]
        public static extern void Foo9();

        [DllImport("Foo.dll", CallingConvention = CallingConvention.Winapi)]
        public static extern void Foo10();

        [DllImport("Foo.dll", EntryPoint = "GoHereInstead", ExactSpelling = true)]
        public static extern void Foo11();

        [DllImport("Foo.dll", EntryPoint = "GoHereInstead", PreserveSig = false)]
        public static extern void Foo12();

        [DllImport("Foo.dll", EntryPoint = "GoHereInstead", SetLastError = true)]
        public static extern void Foo13();

        [DllImport("Foo.dll", EntryPoint = "GoHereInstead", ThrowOnUnmappableChar = true)]
        public static extern void Foo14();

        [DllImport("Foo.dll", EntryPoint = "GoHereInstead", BestFitMapping = true)]
        public static extern void Foo15();
    }

    public class MarshalAsHolders
    {
        [MarshalAs(UnmanagedType.BStr)]
        public int F1;

        [MarshalAs(UnmanagedType.Currency)]
        public int F2;

        [MarshalAs(UnmanagedType.IDispatch)]
        public int F3;

        [MarshalAs(UnmanagedType.IDispatch, IidParameterIndex = 42)]
        public int F4;

        [MarshalAs(UnmanagedType.ByValArray)]
        public int F5;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public int F6;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.FunctionPtr)]
        public int F7;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 87, ArraySubType = UnmanagedType.FunctionPtr)]
        public int F8;

        [MarshalAs(UnmanagedType.SafeArray)]
        public int F9;

        [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)]
        public int F10;

        [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_RECORD, SafeArrayUserDefinedSubType = typeof(MyUdt))]
        public int F11;

        [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_RECORD, SafeArrayUserDefinedSubType = typeof(DateTime))]
        public int F12;

        [MarshalAs(UnmanagedType.LPArray)]
        public int F13;

        [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)]
        public int F14;

        [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStruct, SizeParamIndex = 677)]
        public int F15;

        [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStruct, SizeParamIndex = 677, SizeConst = 87)]
        public int F16;

        [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(MyUdt))]
        public int F17;

        [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(DateTime))]
        public int F18;

        [MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "Blah")]
        public int F19;

        [MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "Blah", MarshalCookie = "YumYum")]
        public int F20;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 65)]
        public int F21;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct ExplicitFieldOffsets
    {
        [FieldOffset(42)]
        public int X;

        [FieldOffset(65)]
        public int Y;
    }

    public struct MyUdt { }

    public class ExerciseCallingConventions
    {
        public ExerciseCallingConventions() { }

        public void InstanceMethod() { }
        public static void StaticMethod() { }
        public virtual void VirtualMethod() { }

        static ExerciseCallingConventions()
        {
        }
    }

    public class MarkAttribute : Attribute
    {
        public MarkAttribute(int mark) { }
    }

    public class MyGenericClass<T> { }

    public class MethodHolderBase<T>
    {
        [Mark(10)]
        public void Hoo(int x, int y) { }

        [Mark(11)]
        public void Hoo(string x, string y) { }

        [Mark(12)]
        public void Hoo(T x, T y) { }

        [Mark(20)]
        public virtual void Voo(int x, int y) { }

        [Mark(30)]
        private void Poo(int x, int y) { }

        [Mark(40)]
        public virtual void Foo<K>(MyGenericClass<K> x, int y) { }

        [Mark(50)]
        public virtual void Foo<K>(MyGenericClass<T> x, int y) { }

        [Mark(60)]
        public virtual void Foo(MyGenericClass<T> x, string y) { }

        [Mark(70)]
        public virtual void Foo(int x, int y) { }
    }

    public class MethodHolderDerived<T> : MethodHolderBase<T>
    {
        [Mark(10010)]
        public new void Hoo(int x, int y) { }

        [Mark(10020)]
        public override void Voo(int x, int y) { }

        [Mark(10040)]
        public override void Foo<K>(MyGenericClass<K> x, int y) { }

        [Mark(10050)]
        public virtual void Foo<K>(MyGenericClass<int> x, int y) { }

        [Mark(10060)]
        public override void Foo(MyGenericClass<T> x, string y) { }

        [Mark(10070)]
        public override void Foo(int x, int y) { }
    }

    public class PropertyHolder1<T>
    {
        public int ReadOnlyProp { get; }
        public T ReadWriteProp { get; set; }

        public GenericClass1<T> PublicPrivateProp { get; private set; }
        public int PublicProtectedProp { get; protected set; }
        public int PublicInternalProp { get; internal set; }

        public string this[int i, T t] => throw null!;
    }

    public class DerivedFromPropertyHolder1<T> : PropertyHolder1<T> { }

    public class EventHolder1<T>
    {
        public event Action<T> MyEvent { add { throw null!; } remove { throw null!; } }
    }

    public class DerivedFromEventHolder1<T> : EventHolder1<T>
    {
    }

    namespace NS0 { public class SameNamedType { } }
    namespace NS1 { public class SameNamedType { } }
    namespace NS2 { public class SameNamedType { } }
    namespace NS3 { public class SameNamedType { } }
    namespace NS4 { public class SameNamedType { } }
    namespace NS5 { public class SameNamedType { } }
    namespace NS6 { public class SameNamedType { } }
    namespace NS7 { public class SameNamedType { } }
    namespace NS8 { public class SameNamedType { } }
    namespace NS9 { public class SameNamedType { } }
    namespace NS10 { public class SameNamedType { } }
    namespace NS11 { public class SameNamedType { } }
    namespace NS12 { public class SameNamedType { } }
    namespace NS13 { public class SameNamedType { } }
    namespace NS14 { public class SameNamedType { } }
    namespace NS15 { public class SameNamedType { } }

    [DefaultMember("Yes")]
    public class ClassWithDefaultMember1<T> where T : ClassWithDefaultMember1<T>
    {
        public int Yes;
    }

    public class PublicClass
    {
        internal class InternalNestedClass { }
    }
}
