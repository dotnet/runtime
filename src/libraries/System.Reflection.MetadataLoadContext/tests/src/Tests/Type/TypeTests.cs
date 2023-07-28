// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SampleMetadata;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Reflection.Tests
{
    public static partial class TypeTests
    {
        [Theory]
        [MemberData(nameof(InvariantTheoryData))]
        public static void TestInvariants(TypeWrapper tw)
        {
            Type t = tw?.Type;
            t.TestTypeInvariants();
        }

        public static IEnumerable<object[]> InvariantTheoryData => InvariantTestData.Select(t => new object[] { t }).Wrap();

        private static IEnumerable<Type> InvariantTestData
        {
            get
            {
                yield return typeof(object).Project();
                yield return typeof(Span<>).Project();
#if false
                foreach (Type t in typeof(TopLevelType).Project().Assembly.GetTypes())
                {
                    yield return t;
                }
                foreach (Type t in typeof(object).Project().Assembly.GetTypes())
                {
                    yield return t;
                }
#endif
            }
        }

        [Fact]
        public static void TestIsAssignableFrom()
        {
            bool b;
            Type src, dst;

            // Compat: ok to pass null to IsAssignableFrom()
            dst = typeof(object).Project();
            src = null;
            b = dst.IsAssignableFrom(src);
            Assert.False(b);

            dst = typeof(Base1).Project();
            src = typeof(Derived1).Project();
            b = dst.IsAssignableFrom(src);
            Assert.True(b);

            dst = typeof(Derived1).Project();
            src = typeof(Base1).Project();
            b = dst.IsAssignableFrom(src);
            Assert.False(b);

            // Interfaces
            dst = typeof(IEnumerable).Project();
            src = typeof(IList).Project();
            b = dst.IsAssignableFrom(src);
            Assert.True(b);

            dst = typeof(IEnumerable<string>).Project();
            src = typeof(IList<string>).Project();
            b = dst.IsAssignableFrom(src);
            Assert.True(b);

            dst = typeof(IEnumerable).Project();
            src = typeof(IList<string>).Project();
            b = dst.IsAssignableFrom(src);
            Assert.True(b);

            // Arrays
            dst = typeof(Array).Project();
            src = typeof(string[]).Project();
            b = dst.IsAssignableFrom(src);
            Assert.True(b);

            dst = typeof(IList<string>).Project();
            src = typeof(string[]).Project();
            b = dst.IsAssignableFrom(src);
            Assert.True(b);

            // Generic typedefs
            dst = typeof(object).Project();
            src = typeof(GenericClass1<>).Project();
            b = dst.IsAssignableFrom(src);
            Assert.True(b);

            // Is this "true" because T is always assignable to T?
            // Or is this "false" because it's nonsensical to assign to a generic typedef?
            //
            // (Spoiler: The "trues" wins on the .NET Framework so they win here too.)
            dst = typeof(GenericClass1<>).Project();
            src = typeof(GenericClass1<>).Project();
            b = dst.IsAssignableFrom(src);
            Assert.True(b);

            return;
        }

        [Theory]
        [MemberData(nameof(IsByRefLikeTheoryData))]
        public static void TestIsByRefLike(TypeWrapper tw, bool expectedIsByRefLike)
        {
            Type t = tw?.Type;
            bool actualIsByRefLike = t.IsByRefLike();
            Assert.Equal(expectedIsByRefLike, actualIsByRefLike);
        }

        public static IEnumerable<object[]> IsByRefLikeTheoryData => IsByRefLikeTypeData.Wrap();

        public static IEnumerable<object[]> IsByRefLikeTypeData
        {
            get
            {
                yield return new object[] { typeof(Span<>).Project(), true };
                yield return new object[] { typeof(Span<int>).Project(), true };
                yield return new object[] { typeof(SampleByRefLikeStruct1).Project(), true };
                yield return new object[] { typeof(SampleByRefLikeStruct2<>).Project(), true };
                yield return new object[] { typeof(SampleByRefLikeStruct2<string>).Project(), true };
                yield return new object[] { typeof(SampleByRefLikeStruct3).Project(), true };
                yield return new object[] { typeof(int).Project(), false };
                yield return new object[] { typeof(int).Project().MakeArrayType(), false };
                yield return new object[] { typeof(IList<int>).Project(), false };
                yield return new object[] { typeof(IList<>).Project().GetGenericTypeParameters()[0], false };
                yield return new object[] { typeof(AttributeHolder1.N1).Project(), false };
            }
        }

        [Fact]
        public static void TestGuid()
        {
            Type t = typeof(ClassWithGuid).Project();
            Guid actualGuid = t.GUID;
            Assert.Equal(new Guid("E73CFD63-6BD8-432D-A71B-E1E54AD55914"), actualGuid);
        }

        [Fact]
        public static void TestArrayGetMethod()
        {
            bool expectedDefaultValue = true;

            Type et = typeof(long).Project();
            Type t = typeof(long[]).Project();
            TypeInfo ti = t.GetTypeInfo();
            MethodInfo m = ti.GetDeclaredMethod("Get");
            Assert.Equal(MethodAttributes.Public | MethodAttributes.PrivateScope, m.Attributes);
            Assert.Equal(CallingConventions.Standard | CallingConventions.HasThis, m.CallingConvention);
            Assert.Equal(t, m.DeclaringType);
            Assert.Equal(et, m.ReturnType);
            ParameterInfo[] p = m.GetParameters();
            Assert.Equal(1, p.Length);

            Assert.Equal(ParameterAttributes.None, p[0].Attributes);
            Assert.Equal(typeof(int).Project(), p[0].ParameterType);
            Assert.Equal(m, p[0].Member);
            Assert.Equal(0, p[0].Position);
            Assert.Null(p[0].Name);
            Assert.Equal(expectedDefaultValue, p[0].HasDefaultValue);
            Assert.Null(p[0].RawDefaultValue); //Legacy: This makes no sense

            return;
        }

        [Fact]
        public static void TestArraySetMethod()
        {
            bool expectedDefaultValue = true;

            Type et = typeof(long).Project();
            Type t = typeof(long[]).Project();
            TypeInfo ti = t.GetTypeInfo();
            MethodInfo m = ti.GetDeclaredMethod("Set");
            Assert.Equal(MethodAttributes.Public | MethodAttributes.PrivateScope, m.Attributes);
            Assert.Equal(CallingConventions.Standard | CallingConventions.HasThis, m.CallingConvention);

            Assert.Equal(t, m.DeclaringType);
            Assert.Equal(typeof(void).Project(), m.ReturnType);
            ParameterInfo[] p = m.GetParameters();
            Assert.Equal(2, p.Length);

            Assert.Equal(ParameterAttributes.None, p[0].Attributes);
            Assert.Equal(typeof(int).Project(), p[0].ParameterType);
            Assert.Null(p[0].Name);
            Assert.Equal(m, p[0].Member);
            Assert.Equal(0, p[0].Position);
            Assert.Equal(expectedDefaultValue, p[0].HasDefaultValue);  //Legacy: This makes no sense
            Assert.Null(p[0].RawDefaultValue); //Legacy: This makes no sense

            Assert.Equal(ParameterAttributes.None, p[1].Attributes);
            Assert.Equal(et, p[1].ParameterType);
            Assert.Null(p[1].Name);
            Assert.Equal(m, p[1].Member);
            Assert.Equal(1, p[1].Position);
            Assert.Equal(expectedDefaultValue, p[1].HasDefaultValue);  //Legacy: This makes no sense
            Assert.Null(p[1].RawDefaultValue); //Legacy: This makes no sense

            return;
        }

        [Fact]
        static void TestArrayMethodsGetSetAddressAreNotEquals()
        {
           void test(Type type)
            {
                MethodInfo v1 = type.GetMethod("Get");
                MethodInfo v2 = type.GetMethod("Set");
                MethodInfo v3 = type.GetMethod("Address");
                Assert.NotEqual(v1, v2);
                Assert.NotEqual(v1, v3);
                Assert.NotEqual(v2, v3);
            }

            test(typeof(int[]));
            test(typeof(int[]).Project());
        }

        [Fact]
        static void TestArrayMethodsGetSetAddressEqualityForDifferentTypes()
        {
            void testNotEqual(Type type1, Type type2)
            {
                Assert.NotEqual(type1.GetMethod("Get"), type2.GetMethod("Get"));
                Assert.NotEqual(type1.GetMethod("Set"), type2.GetMethod("Set"));
                Assert.NotEqual(type1.GetMethod("Address"), type2.GetMethod("Address"));
            }

            testNotEqual(typeof(int[]), typeof(long[]));
            testNotEqual(typeof(int[]).Project(), typeof(long[]).Project());
            testNotEqual(typeof(int[]).Project(), typeof(int[]));

            void testEqual(Type type1, Type type2)
            {
                Assert.Equal(type1.GetMethod("Get"), type2.GetMethod("Get"));
                Assert.Equal(type1.GetMethod("Set"), type2.GetMethod("Set"));
                Assert.Equal(type1.GetMethod("Address"), type2.GetMethod("Address"));
            }

            testEqual(typeof(int[]), typeof(int[]));
            testEqual(typeof(int[]).Project(), typeof(int[]).Project());
            testEqual(typeof(long[]).Project(), typeof(long[]).Project());
        }

        [Fact]
        static void TestArrayGetMethodsResultEqualsFilteredGetMethod()
        {
            Type type = typeof(int[]).Project();

            Assert.Equal(type.GetMethod("Get"), type.GetMethods().First(m => m.Name == "Get"));
            Assert.Equal(type.GetMethod("Set"), type.GetMethods().First(m => m.Name == "Set"));
            Assert.Equal(type.GetMethod("Address"), type.GetMethods().First(m => m.Name == "Address"));

            Assert.NotEqual(type.GetMethod("Get"), type.GetMethods().First(m => m.Name == "Set"));
            Assert.NotEqual(type.GetMethod("Set"), type.GetMethods().First(m => m.Name == "Address"));
            Assert.NotEqual(type.GetMethod("Address"), type.GetMethods().First(m => m.Name == "Get"));
        }

        [Fact]
        public static void TestArrayAddressMethod()
        {
            bool expectedDefaultValue = true;

            Type et = typeof(long).Project();
            Type t = typeof(long[]).Project();
            TypeInfo ti = t.GetTypeInfo();
            MethodInfo m = ti.GetDeclaredMethod("Address");
            Assert.Equal(MethodAttributes.Public | MethodAttributes.PrivateScope, m.Attributes);
            Assert.Equal(CallingConventions.Standard | CallingConventions.HasThis, m.CallingConvention);
            Assert.Equal(t, m.DeclaringType);
            Assert.Equal(et.MakeByRefType(), m.ReturnType);
            ParameterInfo[] p = m.GetParameters();
            Assert.Equal(1, p.Length);

            Assert.Equal(ParameterAttributes.None, p[0].Attributes);
            Assert.Equal(typeof(int).Project(), p[0].ParameterType);
            Assert.Null(p[0].Name);
            Assert.Equal(m, p[0].Member);
            Assert.Equal(0, p[0].Position);
            Assert.Equal(expectedDefaultValue, p[0].HasDefaultValue);
            Assert.Null(p[0].RawDefaultValue); //Legacy: This makes no sense

            return;
        }

        [Fact]
        public static void TestArrayCtor()
        {
            bool expectedDefaultValue = true;

            Type et = typeof(long).Project();
            Type t = typeof(long[]).Project();
            TypeInfo ti = t.GetTypeInfo();
            ConstructorInfo[] ctors = ti.DeclaredConstructors.ToArray();
            Assert.Equal(1, ctors.Length);
            ConstructorInfo m = ctors[0];
            Assert.Equal(MethodAttributes.Public | MethodAttributes.PrivateScope | MethodAttributes.RTSpecialName, m.Attributes);
            Assert.Equal(CallingConventions.Standard | CallingConventions.HasThis, m.CallingConvention);
            Assert.Equal(t, m.DeclaringType);
            ParameterInfo[] p = m.GetParameters();
            Assert.Equal(1, p.Length);

            Assert.Equal(ParameterAttributes.None, p[0].Attributes);
            Assert.Equal(typeof(int).Project(), p[0].ParameterType);
            Assert.Equal(m, p[0].Member);
            Assert.Equal(0, p[0].Position);
            Assert.Null(p[0].Name);
            Assert.Equal(expectedDefaultValue, p[0].HasDefaultValue);
            Assert.Null(p[0].RawDefaultValue); //Legacy: This makes no sense

            return;
        }

        [Theory]
        [MemberData(nameof(GetEnumUnderlyingTypeTheoryData))]
        public static void GetEnumUnderlyingType(TypeWrapper enumTypeW, TypeWrapper expectedUnderlyingTypeW)
        {
            Type enumType = enumTypeW?.Type;
            Type expectedUnderlyingType = expectedUnderlyingTypeW?.Type;

            if (expectedUnderlyingType == null)
            {
                Assert.Throws<ArgumentException>(() => enumType.GetEnumUnderlyingType());
            }
            else
            {
                Type actualUnderlyingType = enumType.GetEnumUnderlyingType();
                Assert.Equal(expectedUnderlyingType, actualUnderlyingType);
            }
        }

        public static IEnumerable<object[]> GetEnumUnderlyingTypeTheoryData => GetEnumUnderlyingTypeData.Wrap();
        public static IEnumerable<object[]> GetEnumUnderlyingTypeData
        {
            get
            {
                yield return new object[] { typeof(EU1).Project(), typeof(byte).Project() };
                yield return new object[] { typeof(EI1).Project(), typeof(sbyte).Project() };
                yield return new object[] { typeof(EU2).Project(), typeof(ushort).Project() };
                yield return new object[] { typeof(EI2).Project(), typeof(short).Project() };
                yield return new object[] { typeof(EU4).Project(), typeof(uint).Project() };
                yield return new object[] { typeof(EI4).Project(), typeof(int).Project() };
                yield return new object[] { typeof(EU8).Project(), typeof(ulong).Project() };
                yield return new object[] { typeof(EI8).Project(), typeof(long).Project() };
                yield return new object[] { typeof(GenericEnumContainer<>.GenericEnum).Project(), typeof(short).Project() };
                yield return new object[] { typeof(GenericEnumContainer<int>.GenericEnum).Project(), typeof(short).Project() };
                yield return new object[] { typeof(object).Project(), null };
                yield return new object[] { typeof(ValueType).Project(), null };
                yield return new object[] { typeof(Enum).Project(), null };
                yield return new object[] { typeof(EU1).MakeArrayType().Project(), null };
                yield return new object[] { typeof(EU1).MakeArrayType(1).Project(), null };
                yield return new object[] { typeof(EU1).MakeArrayType(3).Project(), null };
                yield return new object[] { typeof(EU1).MakeByRefType().Project(), null };
                yield return new object[] { typeof(EU1).MakePointerType().Project(), null };
                yield return new object[] { typeof(GenericEnumContainer<>).Project().GetGenericTypeParameters()[0], null };
            }
        }

#if NET7_0_OR_GREATER
        [Fact]
        public static void GetEnumValuesAsUnderlyingType()
        {
            var intEnumType = typeof(E_2_I4).Project();
            int[] expectedIntValues = { int.MinValue, 0, 1, int.MaxValue };
            Array intArr = intEnumType.GetEnumValuesAsUnderlyingType();
            for (int i = 0; i < intArr.Length; i++)
            {
                Assert.Equal(expectedIntValues[i], intArr.GetValue(i));
                Assert.Equal(Type.GetTypeCode(expectedIntValues[i].GetType()), Type.GetTypeCode(intArr.GetValue(i).GetType()));
            }

            var uintEnumType = typeof(E_2_U4).Project();
            uint[] expectesUIntValues = { uint.MinValue, 0, 1, uint.MaxValue };
            Array uintArr = uintEnumType.GetEnumValuesAsUnderlyingType();
            for (int i = 0; i < uintArr.Length; i++)
            {
                Assert.Equal(expectesUIntValues[i], uintArr.GetValue(i));
                Assert.Equal(Type.GetTypeCode(expectesUIntValues[i].GetType()), Type.GetTypeCode(uintArr.GetValue(i).GetType()));
            }
        }
#endif        

        [Theory]
        [MemberData(nameof(GetTypeCodeTheoryData))]
        public static void GettypeCode(TypeWrapper tw, TypeCode expectedTypeCode)
        {
            Type t = tw?.Type;
            TypeCode actualTypeCode = Type.GetTypeCode(t);
            Assert.Equal(expectedTypeCode, actualTypeCode);
        }

        public static IEnumerable<object[]> GetTypeCodeTheoryData => GetTypeCodeTypeData.Wrap();
        public static IEnumerable<object[]> GetTypeCodeTypeData
        {
            get
            {
                yield return new object[] { typeof(bool).Project(), TypeCode.Boolean };
                yield return new object[] { typeof(byte).Project(), TypeCode.Byte };
                yield return new object[] { typeof(char).Project(), TypeCode.Char };
                yield return new object[] { typeof(DateTime).Project(), TypeCode.DateTime };
                yield return new object[] { typeof(decimal).Project(), TypeCode.Decimal };
                yield return new object[] { typeof(double).Project(), TypeCode.Double };
                yield return new object[] { typeof(short).Project(), TypeCode.Int16 };
                yield return new object[] { typeof(int).Project(), TypeCode.Int32 };
                yield return new object[] { typeof(long).Project(), TypeCode.Int64 };
                yield return new object[] { typeof(object).Project(), TypeCode.Object };
                yield return new object[] { typeof(System.Nullable).Project(), TypeCode.Object };
                yield return new object[] { typeof(Nullable<int>).Project(), TypeCode.Object };
                yield return new object[] { typeof(Dictionary<,>).Project(), TypeCode.Object };
                yield return new object[] { typeof(Exception).Project(), TypeCode.Object };
                yield return new object[] { typeof(sbyte).Project(), TypeCode.SByte };
                yield return new object[] { typeof(float).Project(), TypeCode.Single };
                yield return new object[] { typeof(string).Project(), TypeCode.String };
                yield return new object[] { typeof(ushort).Project(), TypeCode.UInt16 };
                yield return new object[] { typeof(uint).Project(), TypeCode.UInt32 };
                yield return new object[] { typeof(ulong).Project(), TypeCode.UInt64 };
                yield return new object[] { typeof(DBNull).Project(), TypeCode.DBNull };
                yield return new object[] { typeof(EI1).Project(), TypeCode.SByte };
                yield return new object[] { typeof(EU1).Project(), TypeCode.Byte };
                yield return new object[] { typeof(EI2).Project(), TypeCode.Int16 };
                yield return new object[] { typeof(EU2).Project(), TypeCode.UInt16 };
                yield return new object[] { typeof(EI4).Project(), TypeCode.Int32 };
                yield return new object[] { typeof(EU4).Project(), TypeCode.UInt32 };
                yield return new object[] { typeof(EI8).Project(), TypeCode.Int64 };
                yield return new object[] { typeof(EU8).Project(), TypeCode.UInt64 };
                yield return new object[] { typeof(int).Project().MakeArrayType(), TypeCode.Object };
                yield return new object[] { typeof(int).Project().MakeArrayType(1), TypeCode.Object };
                yield return new object[] { typeof(int).Project().MakeArrayType(3), TypeCode.Object };
                yield return new object[] { typeof(int).Project().MakeByRefType(), TypeCode.Object };
                yield return new object[] { typeof(int).Project().MakePointerType(), TypeCode.Object };
                yield return new object[] { typeof(List<>).Project().GetGenericTypeParameters()[0], TypeCode.Object };
            }
        }

        [Fact]
        public static void TestIsPrimitive()
        {
            Assert.True(typeof(bool).Project().IsPrimitive);
            Assert.True(typeof(char).Project().IsPrimitive);
            Assert.True(typeof(sbyte).Project().IsPrimitive);
            Assert.True(typeof(byte).Project().IsPrimitive);
            Assert.True(typeof(short).Project().IsPrimitive);
            Assert.True(typeof(ushort).Project().IsPrimitive);
            Assert.True(typeof(int).Project().IsPrimitive);
            Assert.True(typeof(uint).Project().IsPrimitive);
            Assert.True(typeof(long).Project().IsPrimitive);
            Assert.True(typeof(ulong).Project().IsPrimitive);
            Assert.True(typeof(float).Project().IsPrimitive);
            Assert.True(typeof(double).Project().IsPrimitive);
            Assert.True(typeof(IntPtr).Project().IsPrimitive);
            Assert.True(typeof(UIntPtr).Project().IsPrimitive);

            Assert.False(typeof(void).Project().IsPrimitive);
            Assert.False(typeof(decimal).Project().IsPrimitive);
            Assert.False(typeof(BindingFlags).Project().IsPrimitive);
            Assert.False(typeof(int[]).Project().IsPrimitive);

            return;
        }


        [Fact]
        public static void TestIsValueType()
        {
            Assert.True(typeof(bool).Project().IsValueType);
            Assert.False(typeof(bool).Project().MakeArrayType().IsValueType);
            Assert.False(typeof(bool).Project().MakeArrayType(1).IsValueType);
            Assert.False(typeof(bool).Project().MakeByRefType().IsValueType);
            Assert.False(typeof(bool).Project().MakePointerType().IsValueType);
            Assert.True(typeof(KeyValuePair<,>).Project().IsValueType);
            Assert.True(typeof(KeyValuePair<object, object>).Project().IsValueType);
            Assert.False(typeof(object).Project().IsValueType);
            Assert.False(typeof(IEnumerable<>).Project().IsValueType);
            Assert.False(typeof(IEnumerable<int>).Project().IsValueType);
            Assert.False(typeof(ValueType).Project().IsValueType);
            Assert.False(typeof(Enum).Project().IsValueType);
            Assert.True(typeof(MyColor).Project().IsValueType);

            return;
        }

        public static IEnumerable<object[]> ByRefPonterTypes_IsPublicIsVisible_TestData()
        {
            yield return new object[] { typeof(int).Project().MakeByRefType(), true, true };
            yield return new object[] { typeof(int).Project().MakePointerType(), true, true };
            yield return new object[] { typeof(int).Project(), true, true };
            yield return new object[] { typeof(SampleMetadata.PublicClass.InternalNestedClass).Project().MakeByRefType(), true, false };
            yield return new object[] { typeof(SampleMetadata.PublicClass.InternalNestedClass).Project().MakePointerType(), true, false };
            yield return new object[] { typeof(SampleMetadata.PublicClass.InternalNestedClass).Project(), false, false };
            yield return new object[] { typeof(SampleMetadata.PublicClass).Project().MakeByRefType(), true, true };
            yield return new object[] { typeof(SampleMetadata.PublicClass).Project().MakePointerType(), true, true };
            yield return new object[] { typeof(SampleMetadata.PublicClass).Project(), true, true };
        }

        [Theory]
        [MemberData(nameof(ByRefPonterTypes_IsPublicIsVisible_TestData))]
        public static void ByRefPonterTypes_IsPublicIsVisible(Type type, bool isPublic, bool isVisible)
        {
            Assert.Equal(isPublic, type.IsPublic);
            Assert.Equal(!isPublic, type.IsNestedAssembly);
            Assert.Equal(isVisible, type.IsVisible);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/11354")]
        public static void FunctionPointerTypeIsPublic()
        {
            Assert.True(typeof(delegate*<string, int>).Project().IsPublic);
            Assert.True(typeof(delegate*<string, int>).Project().MakePointerType().IsPublic);
        }

        [Fact]
        public static void TestMethodSelection1()
        {
            Binder binder = null;
            const BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            Type t = typeof(MethodHolderDerived<>).Project();
            {
                Type[] types = { typeof(int).Project(), typeof(int).Project() };
                MethodInfo m = t.GetMethod("Hoo", bf, binder, types, null);
                Assert.Equal(10010, m.GetMark());
            }

            {
                Type[] types = { typeof(int).Project(), typeof(short).Project() };
                MethodInfo m = t.GetMethod("Hoo", bf, binder, types, null);
                Assert.Equal(10010, m.GetMark());
            }

            {
                Type[] types = { typeof(int).Project(), typeof(short).Project() };
                Type gi = t.MakeGenericType(typeof(int).Project()).BaseType;
                Assert.Throws<AmbiguousMatchException>(() => gi.GetMethod("Hoo", bf, binder, types, null));
            }

            {
                Type[] types = { typeof(int).Project(), typeof(short).Project() };
                MethodInfo m = t.GetMethod("Voo", bf, binder, types, null);
                Assert.Equal(10020, m.GetMark());
            }

            {
                Type[] types = { typeof(int).Project(), typeof(short).Project() };
                MethodInfo m = t.BaseType.GetMethod("Voo", bf, binder, types, null);
                Assert.Equal(20, m.GetMark());
            }

            {
                Type[] types = { typeof(int).Project(), typeof(int).Project() };
                MethodInfo m = t.GetMethod("Poo", bf, binder, types, null);
                Assert.Null(m);
            }

            {
                Type[] types = { typeof(int).Project(), typeof(int).Project() };
                MethodInfo m = t.BaseType.GetMethod("Poo", bf, binder, types, null);
                Assert.Equal(30, m.GetMark());
            }

            {
                Type[] types = { typeof(string).Project(), typeof(object).Project() };
                Type gi = t.MakeGenericType(typeof(object).Project()).BaseType;
                MethodInfo m = gi.GetMethod("Hoo", bf, binder, types, null);
                Assert.Equal(12, m.GetMark());
            }

            {
                Type[] types = { typeof(string).Project(), typeof(string).Project() };
                Type gi = t.MakeGenericType(typeof(object).Project()).BaseType;
                MethodInfo m = gi.GetMethod("Hoo", bf, binder, types, null);
                Assert.Equal(11, m.GetMark());
            }

            {
                Type mgc = typeof(MyGenericClass<>).Project();
                Type mgcClosed = mgc.MakeGenericType(typeof(int).Project());
                Assert.Equal(mgc, mgcClosed.GetGenericTypeDefinition());

                Type gi = t.MakeGenericType(typeof(int).Project());
                Type[] types = { mgcClosed, typeof(string).Project() };
                MethodInfo m = gi.GetMethod("Foo", bf, binder, types, null);
                Assert.Equal(10060, m.GetMark());
            }

            {
                Type[] types = { typeof(int).Project(), typeof(short).Project() };
                MethodInfo m = typeof(MethodHolderDerived<>).Project().GetMethod("Foo", bf, binder, types, null);
                Assert.Equal(10070, m.GetMark());
            }

            {
                Type[] types = { typeof(int).Project(), typeof(int).Project() };
                MethodInfo m = typeof(MethodHolderDerived<>).Project().GetMethod("Foo", bf, binder, types, null);
                Assert.Equal(10070, m.GetMark());
            }
        }

        [Fact]
        public static void TestComImportPseudoCustomAttribute()
        {
            Type t = typeof(ClassWithComImport).Project();
            CustomAttributeData cad = t.CustomAttributes.Single(c => c.AttributeType == typeof(ComImportAttribute).Project());
            Assert.Equal(0, cad.ConstructorArguments.Count);
            Assert.Equal(0, cad.NamedArguments.Count);
        }

        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/15340", TestRuntimes.Mono)]
        public static void TestExplicitOffsetPseudoCustomAttribute()
        {
            Type t = typeof(ExplicitFieldOffsets).Project();

            {
                FieldInfo f = t.GetField("X");
                CustomAttributeData cad = f.CustomAttributes.Single(c => c.AttributeType == typeof(FieldOffsetAttribute).Project());
                FieldOffsetAttribute foa = cad.UnprojectAndInstantiate<FieldOffsetAttribute>();
                Assert.Equal(42, foa.Value);
            }

            {
                FieldInfo f = t.GetField("Y");
                CustomAttributeData cad = f.CustomAttributes.Single(c => c.AttributeType == typeof(FieldOffsetAttribute).Project());
                FieldOffsetAttribute foa = cad.UnprojectAndInstantiate<FieldOffsetAttribute>();
                Assert.Equal(65, foa.Value);
            }
        }

        [Fact]
        public static void CoreGetTypeCacheCoverage1()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_SimpleAssemblyImage);
                // Create big hash collisions in GetTypeCoreCache.
                for (int i = 0; i < 1000; i++)
                {
                    string ns = "NS" + i;
                    string name = "NonExistent";
                    string fullName = ns + "." + name;
                    Type t = a.GetType(fullName, throwOnError: false);
                    Assert.Null(t);
                }
            }
        }

        [Fact]
        public static void CoreGetTypeCacheCoverage2()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromAssemblyPath(AssemblyPathHelper.GetAssemblyLocation(typeof(SampleMetadata.NS0.SameNamedType).Assembly));
                // Create big hash collisions in GetTypeCoreCache.
                for (int i = 0; i < 16; i++)
                {
                    string ns = "SampleMetadata.NS" + i;
                    string name = "SameNamedType";
                    string fullName = ns + "." + name;
                    Type t = a.GetType(fullName, throwOnError: true);
                    Assert.Equal(fullName, t.FullName);
                }
            }
        }

        [Fact]
        public static void CoreGetTypeCacheCoverage3()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                // Make sure the tricky corner case of a null/empty namespace is covered.
                Assembly a = lc.LoadFromAssemblyPath(AssemblyPathHelper.GetAssemblyLocation(typeof(TopLevelType).Assembly));
                Type t = a.GetType("TopLevelType", throwOnError: true, ignoreCase: false);
                Assert.Null(t.Namespace);
                Assert.Equal("TopLevelType", t.Name);
            }
        }

        [Fact]
        public static void GetDefaultMemberTest1()
        {
            Type t = typeof(ClassWithDefaultMember1<>).Project().GetTypeInfo().GenericTypeParameters[0];
            MemberInfo[] mems = t.GetDefaultMembers().OrderBy(m => m.Name).ToArray();
            Assert.Equal(1, mems.Length);
            MemberInfo mem = mems[0];
            Assert.Equal("Yes", mem.Name);
            Assert.Equal(typeof(ClassWithDefaultMember1<>).Project().MakeGenericType(t), mem.DeclaringType);
        }


        [Fact]
        public static void GetDefaultMemberTest2()
        {
            Type t = typeof(TopLevelType).Project();
            MemberInfo[] mems = t.GetDefaultMembers();
            Assert.Equal(0, mems.Length);
        }

        [Fact]
        public static void TypesWithStrangeCharacters()
        {
            // Make sure types with strange characters are escaped.
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_TypeWithStrangeCharacters);
                Type[] types = a.GetTypes();
                Assert.Equal(1, types.Length);
                Type t = types[0];
                string name = t.Name;
                Assert.Equal(TestData.s_NameOfTypeWithStrangeCharacters, name);
                string fullName = t.FullName;
                Assert.Equal(TestData.s_NameOfTypeWithStrangeCharacters, fullName);

                Type tRetrieved = a.GetType(fullName, throwOnError: true, ignoreCase: false);
                Assert.Equal(t, tRetrieved);
            }
        }
    }
}
