// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class OffsetOfTests
    {
        [Fact]
        public void OffsetOf_StructField_ReturnsExpected()
        {
            Assert.Equal(new IntPtr(4), Marshal.OffsetOf(typeof(SomeStruct), nameof(SomeStruct.var)));
        }

        [Fact]
        public void OffsetOf_ClassWithExplicitLayout_ReturnsExpected()
        {
            Assert.Equal(new IntPtr(0), Marshal.OffsetOf(typeof(MySystemTime), nameof(MySystemTime.wYear)));
            Assert.Equal(new IntPtr(8), Marshal.OffsetOf(typeof(MySystemTime), nameof(MySystemTime.wHour)));
            Assert.Equal(new IntPtr(14), Marshal.OffsetOf(typeof(MySystemTime), nameof(MySystemTime.wMilliseconds)));
        }

        [Fact]
        public void OffsetOf_ClassWithSequentialLayout_ReturnsExpected()
        {
            Assert.Equal(new IntPtr(0), Marshal.OffsetOf(typeof(MyPoint), nameof(MyPoint.x)));
            Assert.Equal(new IntPtr(4), Marshal.OffsetOf(typeof(MyPoint), nameof(MyPoint.y)));
        }

        [Fact]
        public void OffsetOf_ExplicitLayout_ReturnsExpected()
        {
            Type t = typeof(ExplicitLayoutTest);
            Assert.Equal(OperatingSystem.IsAndroid() && RuntimeInformation.ProcessArchitecture == Architecture.X86 ? 52 : 56, Marshal.SizeOf(t));
            Assert.Equal(new IntPtr(0), Marshal.OffsetOf(t, nameof(ExplicitLayoutTest.m_short1)));
            Assert.Equal(new IntPtr(2), Marshal.OffsetOf(t, nameof(ExplicitLayoutTest.m_short2)));

            Assert.Equal(new IntPtr(4), Marshal.OffsetOf(t, nameof(ExplicitLayoutTest.union1_byte1)));
            Assert.Equal(new IntPtr(5), Marshal.OffsetOf(t, nameof(ExplicitLayoutTest.union1_byte2)));
            Assert.Equal(new IntPtr(6), Marshal.OffsetOf(t, nameof(ExplicitLayoutTest.union1_short1)));
            Assert.Equal(new IntPtr(8), Marshal.OffsetOf(t, nameof(ExplicitLayoutTest.union1_int1)));
            Assert.Equal(new IntPtr(12), Marshal.OffsetOf(t, nameof(ExplicitLayoutTest.union1_int2)));
            Assert.Equal(new IntPtr(16), Marshal.OffsetOf(t, nameof(ExplicitLayoutTest.union1_double1)));

            Assert.Equal(new IntPtr(4), Marshal.OffsetOf(t, nameof(ExplicitLayoutTest.union2_ushort1)));
            Assert.Equal(new IntPtr(6), Marshal.OffsetOf(t, nameof(ExplicitLayoutTest.union2_ushort2)));
            Assert.Equal(new IntPtr(8), Marshal.OffsetOf(t, nameof(ExplicitLayoutTest.union3_int1)));
            Assert.Equal(new IntPtr(8), Marshal.OffsetOf(t, nameof(ExplicitLayoutTest.union3_decimal1)));

            Assert.Equal(new IntPtr(24), Marshal.OffsetOf(t, nameof(ExplicitLayoutTest.m_ushort1)));
            Assert.Equal(new IntPtr(32), Marshal.OffsetOf(t, nameof(ExplicitLayoutTest.m_decimal1)));
            Assert.Equal(new IntPtr(48), Marshal.OffsetOf(t, nameof(ExplicitLayoutTest.m_char1)));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void OffsetOf_ValidField_ReturnsExpected()
        {
            Type t = typeof(FieldAlignmentTest);
            if (OperatingSystem.IsWindows() || (RuntimeInformation.ProcessArchitecture != Architecture.X86))
            {
                Assert.Equal(80, Marshal.SizeOf(t));
            }
            else
            {
                Assert.Equal(72, Marshal.SizeOf(t));
            }

            Assert.Equal(new IntPtr(0), Marshal.OffsetOf(t, nameof(FieldAlignmentTest.m_byte1)));
            Assert.Equal(new IntPtr(2), Marshal.OffsetOf(t, nameof(FieldAlignmentTest.m_short1)));
            Assert.Equal(new IntPtr(4), Marshal.OffsetOf(t, nameof(FieldAlignmentTest.m_short2)));
            Assert.Equal(new IntPtr(8), Marshal.OffsetOf(t, nameof(FieldAlignmentTest.m_int1)));
            Assert.Equal(new IntPtr(12), Marshal.OffsetOf(t, nameof(FieldAlignmentTest.m_byte2)));
            Assert.Equal(new IntPtr(16), Marshal.OffsetOf(t, nameof(FieldAlignmentTest.m_int2)));

            if (OperatingSystem.IsWindows() || (RuntimeInformation.ProcessArchitecture != Architecture.X86))
            {
                Assert.Equal(new IntPtr(24), Marshal.OffsetOf(t, nameof(FieldAlignmentTest.m_double1)));
                Assert.Equal(new IntPtr(32), Marshal.OffsetOf(t, nameof(FieldAlignmentTest.m_char1)));
                Assert.Equal(new IntPtr(33), Marshal.OffsetOf(t, nameof(FieldAlignmentTest.m_char2)));
                Assert.Equal(new IntPtr(34), Marshal.OffsetOf(t, nameof(FieldAlignmentTest.m_char3)));
                Assert.Equal(new IntPtr(40), Marshal.OffsetOf(t, nameof(FieldAlignmentTest.m_double2)));
                Assert.Equal(new IntPtr(48), Marshal.OffsetOf(t, nameof(FieldAlignmentTest.m_byte3)));
                Assert.Equal(new IntPtr(49), Marshal.OffsetOf(t, nameof(FieldAlignmentTest.m_byte4)));
                Assert.Equal(new IntPtr(56), Marshal.OffsetOf(t, nameof(FieldAlignmentTest.m_decimal1)));
                Assert.Equal(new IntPtr(72), Marshal.OffsetOf(t, nameof(FieldAlignmentTest.m_char4)));
            }
            else
            {
                Assert.Equal(new IntPtr(20), Marshal.OffsetOf(t, nameof(FieldAlignmentTest.m_double1)));
                Assert.Equal(new IntPtr(28), Marshal.OffsetOf(t, nameof(FieldAlignmentTest.m_char1)));
                Assert.Equal(new IntPtr(29), Marshal.OffsetOf(t, nameof(FieldAlignmentTest.m_char2)));
                Assert.Equal(new IntPtr(30), Marshal.OffsetOf(t, nameof(FieldAlignmentTest.m_char3)));
                Assert.Equal(new IntPtr(32), Marshal.OffsetOf(t, nameof(FieldAlignmentTest.m_double2)));
                Assert.Equal(new IntPtr(40), Marshal.OffsetOf(t, nameof(FieldAlignmentTest.m_byte3)));
                Assert.Equal(new IntPtr(41), Marshal.OffsetOf(t, nameof(FieldAlignmentTest.m_byte4)));
                Assert.Equal(new IntPtr(48), Marshal.OffsetOf(t, nameof(FieldAlignmentTest.m_decimal1)));
                Assert.Equal(new IntPtr(64), Marshal.OffsetOf(t, nameof(FieldAlignmentTest.m_char4)));
            }
        }

        [Fact]
        public void OffsetOf_Decimal_ReturnsExpected()
        {
            Type t = typeof(FieldAlignmentTest_Decimal);

            if (OperatingSystem.IsWindows() || (RuntimeInformation.ProcessArchitecture != Architecture.X86))
            {
                Assert.Equal(96, Marshal.SizeOf(t));
            }
            else if (OperatingSystem.IsAndroid())
            {
                Assert.Equal(72, Marshal.SizeOf(t));
            }
            else
            {
                Assert.Equal(88, Marshal.SizeOf(t));
            }

            Assert.Equal(new IntPtr(0), Marshal.OffsetOf(t, nameof(FieldAlignmentTest_Decimal.b)));
            Assert.Equal(OperatingSystem.IsAndroid() && RuntimeInformation.ProcessArchitecture == Architecture.X86 ? new IntPtr(4) : new IntPtr(8), Marshal.OffsetOf(t, nameof(FieldAlignmentTest_Decimal.p)));

            if (OperatingSystem.IsWindows() || (RuntimeInformation.ProcessArchitecture != Architecture.X86))
            {
                Assert.Equal(new IntPtr(88), Marshal.OffsetOf(t, nameof(FieldAlignmentTest_Decimal.s)));
            }
            else if (OperatingSystem.IsAndroid())
            {
                Assert.Equal(new IntPtr(68), Marshal.OffsetOf(t, nameof(FieldAlignmentTest_Decimal.s)));
            }
            else
            {
                Assert.Equal(new IntPtr(80), Marshal.OffsetOf(t, nameof(FieldAlignmentTest_Decimal.s)));
            }
        }

        [Fact]
        public void OffsetOf_Guid_ReturnsExpected()
        {
            Type t = typeof(FieldAlignmentTest_Guid);
            Assert.Equal(24, Marshal.SizeOf(t));

            Assert.Equal(new IntPtr(0), Marshal.OffsetOf(t, nameof(FieldAlignmentTest_Guid.b)));
            Assert.Equal(new IntPtr(4), Marshal.OffsetOf(t, nameof(FieldAlignmentTest_Guid.g)));
            Assert.Equal(new IntPtr(20), Marshal.OffsetOf(t, nameof(FieldAlignmentTest_Guid.s)));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void OffsetOf_Variant_ReturnsExpected()
        {
            Type t = typeof(FieldAlignmentTest_Variant);

            Assert.Equal(new IntPtr(0), Marshal.OffsetOf(t, nameof(FieldAlignmentTest_Variant.b)));
            Assert.Equal(new IntPtr(8), Marshal.OffsetOf(t, nameof(FieldAlignmentTest_Variant.v)));

            if (IntPtr.Size == 4)
            {
                Assert.Equal(new IntPtr(24), Marshal.OffsetOf(t, nameof(FieldAlignmentTest_Variant.s)));
                Assert.Equal(32, Marshal.SizeOf(t));
            }
            else if (IntPtr.Size == 8)
            {
                Assert.Equal(new IntPtr(32), Marshal.OffsetOf(t, nameof(FieldAlignmentTest_Variant.s)));
                Assert.Equal(40, Marshal.SizeOf(t));
            }
            else
            {
                Assert.Fail(string.Format("Unexpected value '{0}' for IntPtr.Size", IntPtr.Size));
            }
        }

        [Fact]
        public void OffsetOf_Generic_MatchedNonGeneric()
        {
            IntPtr nonGenericOffsetCall = Marshal.OffsetOf(typeof(SomeStruct), "var");
            IntPtr genericOffsetCall = Marshal.OffsetOf<SomeStruct>("var");
            Assert.Equal(nonGenericOffsetCall, genericOffsetCall);
        }

        [Fact]
        public void OffsetOf_NullType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("t", () => Marshal.OffsetOf(null, null));
            AssertExtensions.Throws<ArgumentNullException>("t", () => Marshal.OffsetOf(null, "abcd"));
        }

        [Fact]
        public void OffsetOf_NullFieldName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Marshal.OffsetOf(new object().GetType(), null));
            Assert.Throws<ArgumentNullException>(() => Marshal.OffsetOf<object>(null));
        }

        [Fact]
        public void OffsetOf_NoSuchFieldName_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => Marshal.OffsetOf(typeof(NonExistField), "NonExistField"));
            Assert.Throws<ArgumentException>(() => Marshal.OffsetOf<NonExistField>("NonExistField"));
        }

        [Fact]
        public void OffsetOf_NonRuntimeField_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("fieldName", () => Marshal.OffsetOf(new NonRuntimeType(), "Field"));
        }

        public static IEnumerable<object[]> OffsetOf_NotMarshallable_TestData()
        {
            // Ensure AOT compilers generate the marshalling data
            if (string.Empty.Length > 0)
                Marshal.SizeOf<StructWithFxdLPSTRSAFld>();

            yield return new object[] { typeof(StructWithFxdLPSTRSAFld), nameof(StructWithFxdLPSTRSAFld.Arr) };
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/75666", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        [ActiveIssue("https://github.com/mono/mono/issues/15087", TestRuntimes.Mono)]
        [MemberData(nameof(OffsetOf_NotMarshallable_TestData))]
        public void OffsetOf_NotMarshallable_ThrowsArgumentException(Type t, string fieldName)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Marshal.OffsetOf(t, fieldName));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/75666", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public void OffsetOf_NoLayoutPoint_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Marshal.OffsetOf(typeof(NoLayoutPoint), nameof(NoLayoutPoint.x)));
            AssertExtensions.Throws<ArgumentException>(null, () => Marshal.OffsetOf<NoLayoutPoint>(nameof(NoLayoutPoint.x)));
        }

        [Fact]
        public void OffsetOf_Field_ValueType_Generic()
        {
            Assert.Equal((IntPtr)4, Marshal.OffsetOf<Point2<int>>(nameof(Point2<int>.y)));
            Assert.Equal((IntPtr)8, Marshal.OffsetOf<Point2<ulong>>(nameof(Point2<int>.y)));
            Assert.Equal((IntPtr)4, Marshal.OffsetOf<Point2<float>>(nameof(Point2<int>.y)));
            Assert.Equal((IntPtr)8, Marshal.OffsetOf<Point2<double>>(nameof(Point2<int>.y)));

            // [COMPAT] Non-blittable generic types with value-type generic arguments are supported in OffsetOf since they've always been allowed
            // and it likely doesn't meet the bar to break back-compat.
            Assert.Equal((IntPtr)1, Marshal.OffsetOf<Point2<char>>(nameof(Point2<int>.y)));
            Assert.Equal((IntPtr)1, Marshal.OffsetOf<Point2<byte>>(nameof(Point2<int>.y)));
        }

        [Fact]
        public void OffsetOf_Field_ReferenceType_Generic()
        {
            // [COMPAT] Generic types with value-type generic arguments are supported in OffsetOf since they've always been allowed
            // and it likely doesn't meet the bar to break back-compat.
            Assert.Equal((IntPtr)4, Marshal.OffsetOf<Point2Class<int>>(nameof(Point2Class<int>.y)));
            Assert.Equal((IntPtr)8, Marshal.OffsetOf<Point2Class<ulong>>(nameof(Point2Class<int>.y)));
            Assert.Equal((IntPtr)4, Marshal.OffsetOf<Point2Class<float>>(nameof(Point2Class<int>.y)));
            Assert.Equal((IntPtr)8, Marshal.OffsetOf<Point2Class<double>>(nameof(Point2Class<int>.y)));

            Assert.Equal((IntPtr)1, Marshal.OffsetOf<Point2Class<char>>(nameof(Point2Class<int>.y)));
            Assert.Equal((IntPtr)1, Marshal.OffsetOf<Point2Class<byte>>(nameof(Point2Class<int>.y)));
        }

        public class NonRuntimeType : Type
        {
            public override FieldInfo GetField(string name, BindingFlags bindingAttr)
            {
                return new NonRuntimeFieldInfo();
            }

            public override Assembly Assembly => throw new NotImplementedException();

            public override string AssemblyQualifiedName => throw new NotImplementedException();

            public override Type BaseType => throw new NotImplementedException();

            public override string FullName => throw new NotImplementedException();

            public override Guid GUID => throw new NotImplementedException();

            public override Module Module => throw new NotImplementedException();

            public override string Namespace => throw new NotImplementedException();

            public override Type UnderlyingSystemType => throw new NotImplementedException();

            public override string Name => throw new NotImplementedException();

            public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) => throw new NotImplementedException();

            public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();

            public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotImplementedException();

            public override Type GetElementType() => throw new NotImplementedException();

            public override EventInfo GetEvent(string name, BindingFlags bindingAttr) => throw new NotImplementedException();

            public override EventInfo[] GetEvents(BindingFlags bindingAttr) => throw new NotImplementedException();

            public override FieldInfo[] GetFields(BindingFlags bindingAttr) => throw new NotImplementedException();

            public override Type GetInterface(string name, bool ignoreCase) => throw new NotImplementedException();

            public override Type[] GetInterfaces() => throw new NotImplementedException();

            public override MemberInfo[] GetMembers(BindingFlags bindingAttr) => throw new NotImplementedException();

            public override MethodInfo[] GetMethods(BindingFlags bindingAttr) => throw new NotImplementedException();

            public override Type GetNestedType(string name, BindingFlags bindingAttr) => throw new NotImplementedException();

            public override Type[] GetNestedTypes(BindingFlags bindingAttr) => throw new NotImplementedException();

            public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) => throw new NotImplementedException();

            public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters) => throw new NotImplementedException();

            public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();

            protected override TypeAttributes GetAttributeFlagsImpl() => throw new NotImplementedException();

            protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) => throw new NotImplementedException();

            protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) => throw new NotImplementedException();

            protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers) => throw new NotImplementedException();

            protected override bool HasElementTypeImpl() => throw new NotImplementedException();

            protected override bool IsArrayImpl() => throw new NotImplementedException();

            protected override bool IsByRefImpl() => throw new NotImplementedException();

            protected override bool IsCOMObjectImpl() => throw new NotImplementedException();

            protected override bool IsPointerImpl() => throw new NotImplementedException();

            protected override bool IsPrimitiveImpl() => throw new NotImplementedException();
        }

        public class NonRuntimeFieldInfo : FieldInfo
        {
            public override FieldAttributes Attributes => throw new NotImplementedException();

            public override RuntimeFieldHandle FieldHandle => throw new NotImplementedException();

            public override Type FieldType => throw new NotImplementedException();

            public override Type DeclaringType => throw new NotImplementedException();

            public override string Name => throw new NotImplementedException();

            public override Type ReflectedType => throw new NotImplementedException();

            public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();

            public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotImplementedException();

            public override object GetValue(object obj) => throw new NotImplementedException();

            public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();

            public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture) => throw new NotImplementedException();
        }
    }

#pragma warning disable 169, 649, 618
    [StructLayout(LayoutKind.Sequential)]
    public struct StructWithFxdLPSTRSAFld
    {
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.LPStr, SizeConst = 0)]
        public string[] Arr;
    }

    public struct SomeStruct
    {
        public bool p;
        public int var;
    }

    [StructLayout(LayoutKind.Explicit)]
    public class MySystemTime
    {
        [FieldOffset(0)]
        public ushort wYear;
        [FieldOffset(2)]
        public ushort wMonth;
        [FieldOffset(4)]
        public ushort wDayOfWeek;
        [FieldOffset(6)]
        public ushort wDay;
        [FieldOffset(8)]
        public ushort wHour;
        [FieldOffset(10)]
        public ushort wMinute;
        [FieldOffset(12)]
        public ushort wSecond;
        [FieldOffset(14)]
        public ushort wMilliseconds;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class MyPoint
    {
        public int x;
        public int y;
    }

    public class NoLayoutPoint
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class NonExistField
    {

    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct ExplicitLayoutTest
    {
        [FieldOffset(0)]
        public short m_short1; // 2 bytes
        [FieldOffset(2)]
        public short m_short2; // 2 bytes

        [FieldOffset(4)]
        public byte union1_byte1; // 1 byte
        [FieldOffset(5)]
        public byte union1_byte2; // 1 byte
        [FieldOffset(6)]
        public short union1_short1; // 2 bytes
        [FieldOffset(8)]
        public int union1_int1; // 4 bytes
        [FieldOffset(12)]
        public int union1_int2; // 4 bytes
        [FieldOffset(16)]
        public double union1_double1; // 8 bytes

        [FieldOffset(4)]
        public ushort union2_ushort1; // 2 bytes
        [FieldOffset(6)]
        public ushort union2_ushort2; // 2 bytes
        [FieldOffset(8)]
        public int union3_int1; // 4 bytes
        [FieldOffset(8)]
        public decimal union3_decimal1; // 16 bytes

        [FieldOffset(24)]
        public ushort m_ushort1; // 2 bytes
                                 // 6 bytes of padding

        [FieldOffset(32)]
        public decimal m_decimal1; // 16 bytes

        [FieldOffset(48)]
        public char m_char1; // 1 byte
                             // 7 bytes of padding
    }

    internal struct FieldAlignmentTest
    {
        public byte m_byte1; // 1 byte
                             // 1 byte of padding

        public short m_short1; // 2 bytes
        public short m_short2; // 2 bytes
                               // 2 bytes of padding

        public int m_int1; // 4 bytes
        public byte m_byte2; // 1 byte
                             // 3 bytes of padding

        public int m_int2; // 4 bytes
                             // 4 bytes of padding (0 bytes on x86/Unix according System V ABI as double 4-byte aligned)

        public double m_double1; // 8 bytes
        public char m_char1; // 1 byte
        public char m_char2; // 1 byte
        public char m_char3; // 1 byte
                             // 5 bytes of padding (1 byte on x86/Unix according System V ABI as double 4-byte aligned)

        public double m_double2; // 8 bytes
        public byte m_byte3; // 1 byte
        public byte m_byte4; // 1 byte
                             // 6 bytes of padding (2 bytes on Linux x86)

        public decimal m_decimal1; // 16 bytes
        public char m_char4; // 1 byte
                             // 7 bytes of padding (3 bytes on Linux x86)
    }
    struct FieldAlignmentTest_Decimal
    {
        public byte b; // 1 byte
                       // 7 bytes of padding (3 bytes on Linux x86)

        // The largest field in below struct is decimal (16 bytes wide).
        // However, alignment requirement for the below struct should be only  8 bytes (not 16).
        // This is because unlike fields of other types well known to mcg (like long, char etc.)
        // which need to be aligned according to their byte size, decimal is really a struct
        // with 8 byte alignment requirement.
        public FieldAlignmentTest p; // 80 bytes (72 bytes on Win x86/Unix) (64 bytes on Linux x86)

        public short s; // 2 bytes
                        // 6 bytes of padding (2 bytes on Linux x86)
    }

    struct FieldAlignmentTest_Guid
    {
        public byte b; // 1 byte
                       // 3 bytes of padding

        // Guid is really a struct with 4 byte alignment requirement (which is less than its byte size of 16 bytes).
        public Guid g; // 16 bytes

        public short s; // 2 bytes
                        // 2 bytes of padding
    }

    struct FieldAlignmentTest_Variant
    {
        public byte b; // 1 byte
                       // 7 bytes of padding

        // Using [MarshalAs(UnmanagedType.Struct)] means that the Variant type will be used for field 'v' on native side.
        // Variant is really a struct with 8 byte alignment requirement (which is less than its byte size of 24 / 16 bytes).
        [MarshalAs(UnmanagedType.Struct)]
        public object v; // 16 bytes on 32-bit, 24 bytes on 64-bit

        public short s; // 2 bytes
                        // 6 bytes of padding
    }

    struct Point2<T>
    {
        public T x;
        public T y;
    }

    [StructLayout(LayoutKind.Sequential)]
    class Point2Class<T>
    {
        public T x;
        public T y;
    }
#pragma warning restore 169, 649, 618
}
