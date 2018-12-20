// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            // (Spoiler: The "trues" wins on the desktop so they win here too.)
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
            Assert.Equal(m.Attributes, MethodAttributes.Public | MethodAttributes.PrivateScope);
            Assert.Equal(m.CallingConvention, CallingConventions.Standard | CallingConventions.HasThis);
            Assert.Equal(m.DeclaringType, t);
            Assert.Equal(m.ReturnType, et);
            ParameterInfo[] p = m.GetParameters();
            Assert.Equal(p.Length, 1);

            Assert.Equal(p[0].Attributes, ParameterAttributes.None);
            Assert.Equal(p[0].ParameterType, typeof(int).Project());
            Assert.Equal(p[0].Member, m);
            Assert.Equal(p[0].Position, 0);
            Assert.Equal(p[0].Name, null);
            Assert.Equal(p[0].HasDefaultValue, expectedDefaultValue);
            Assert.Equal(p[0].RawDefaultValue, null); //Legacy: This makes no sense

            return;
        }

        [Fact]
        public static void TestArraySetMethod()
        {
            bool expectedDefaultValue = true;

            Type et = typeof(long).Project();
            Type t = typeof(long[]).Project(); ;
            TypeInfo ti = t.GetTypeInfo();
            MethodInfo m = ti.GetDeclaredMethod("Set");
            Assert.Equal(m.Attributes, MethodAttributes.Public | MethodAttributes.PrivateScope);
            Assert.Equal(m.CallingConvention, CallingConventions.Standard | CallingConventions.HasThis);

            Assert.Equal(m.DeclaringType, t);
            Assert.Equal(m.ReturnType, typeof(void).Project());
            ParameterInfo[] p = m.GetParameters();
            Assert.Equal(p.Length, 2);

            Assert.Equal(p[0].Attributes, ParameterAttributes.None);
            Assert.Equal(p[0].ParameterType, typeof(int).Project());
            Assert.Equal(p[0].Name, null);
            Assert.Equal(p[0].Member, m);
            Assert.Equal(p[0].Position, 0);
            Assert.Equal(p[0].HasDefaultValue, expectedDefaultValue);  //Legacy: This makes no sense
            Assert.Equal(p[0].RawDefaultValue, null); //Legacy: This makes no sense

            Assert.Equal(p[1].Attributes, ParameterAttributes.None);
            Assert.Equal(p[1].ParameterType, et);
            Assert.Equal(p[1].Name, null);
            Assert.Equal(p[1].Member, m);
            Assert.Equal(p[1].Position, 1);
            Assert.Equal(p[1].HasDefaultValue, expectedDefaultValue);  //Legacy: This makes no sense
            Assert.Equal(p[1].RawDefaultValue, null); //Legacy: This makes no sense

            return;
        }

        [Fact]
        public static void TestArrayAddressMethod()
        {
            bool expectedDefaultValue = true;

            Type et = typeof(long).Project(); ;
            Type t = typeof(long[]).Project(); ;
            TypeInfo ti = t.GetTypeInfo();
            MethodInfo m = ti.GetDeclaredMethod("Address");
            Assert.Equal(m.Attributes, MethodAttributes.Public | MethodAttributes.PrivateScope);
            Assert.Equal(m.CallingConvention, CallingConventions.Standard | CallingConventions.HasThis);
            Assert.Equal(m.DeclaringType, t);
            Assert.Equal(m.ReturnType, et.MakeByRefType());
            ParameterInfo[] p = m.GetParameters();
            Assert.Equal(p.Length, 1);

            Assert.Equal(p[0].Attributes, ParameterAttributes.None);
            Assert.Equal(p[0].ParameterType, typeof(int).Project());
            Assert.Equal(p[0].Name, null);
            Assert.Equal(p[0].Member, m);
            Assert.Equal(p[0].Position, 0);
            Assert.Equal(p[0].HasDefaultValue, expectedDefaultValue);
            Assert.Equal(p[0].RawDefaultValue, null); //Legacy: This makes no sense

            return;
        }

        [Fact]
        public static void TestArrayCtor()
        {
            bool expectedDefaultValue = true;

            Type et = typeof(long).Project(); ;
            Type t = typeof(long[]).Project(); ;
            TypeInfo ti = t.GetTypeInfo();
            ConstructorInfo[] ctors = ti.DeclaredConstructors.ToArray();
            Assert.Equal(ctors.Length, 1);
            ConstructorInfo m = ctors[0];
            Assert.Equal(m.Attributes, MethodAttributes.Public | MethodAttributes.PrivateScope | MethodAttributes.RTSpecialName);
            Assert.Equal(m.CallingConvention, CallingConventions.Standard | CallingConventions.HasThis);
            Assert.Equal(m.DeclaringType, t);
            ParameterInfo[] p = m.GetParameters();
            Assert.Equal(p.Length, 1);

            Assert.Equal(p[0].Attributes, ParameterAttributes.None);
            Assert.Equal(p[0].ParameterType, typeof(int).Project());
            Assert.Equal(p[0].Member, m);
            Assert.Equal(p[0].Position, 0);
            Assert.Equal(p[0].Name, null);
            Assert.Equal(p[0].HasDefaultValue, expectedDefaultValue);
            Assert.Equal(p[0].RawDefaultValue, null); //Legacy: This makes no sense

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
            Assert.True(typeof(Boolean).Project().IsPrimitive);
            Assert.True(typeof(Char).Project().IsPrimitive);
            Assert.True(typeof(SByte).Project().IsPrimitive);
            Assert.True(typeof(Byte).Project().IsPrimitive);
            Assert.True(typeof(Int16).Project().IsPrimitive);
            Assert.True(typeof(UInt16).Project().IsPrimitive);
            Assert.True(typeof(Int32).Project().IsPrimitive);
            Assert.True(typeof(UInt32).Project().IsPrimitive);
            Assert.True(typeof(Int64).Project().IsPrimitive);
            Assert.True(typeof(UInt64).Project().IsPrimitive);
            Assert.True(typeof(Single).Project().IsPrimitive);
            Assert.True(typeof(Double).Project().IsPrimitive);
            Assert.True(typeof(IntPtr).Project().IsPrimitive);
            Assert.True(typeof(UIntPtr).Project().IsPrimitive);

            Assert.False(typeof(void).Project().IsPrimitive);
            Assert.False(typeof(Decimal).Project().IsPrimitive);
            Assert.False(typeof(BindingFlags).Project().IsPrimitive);
            Assert.False(typeof(Int32[]).Project().IsPrimitive);

            return;
        }


        [Fact]
        public static void TestIsValueType()
        {
            Assert.True(typeof(Boolean).Project().IsValueType);
            Assert.False(typeof(Boolean).Project().MakeArrayType().IsValueType);
            Assert.False(typeof(Boolean).Project().MakeArrayType(1).IsValueType);
            Assert.False(typeof(Boolean).Project().MakeByRefType().IsValueType);
            Assert.False(typeof(Boolean).Project().MakePointerType().IsValueType);
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
                Assembly a = lc.LoadFromAssemblyPath(typeof(SampleMetadata.NS0.SameNamedType).Assembly.Location);
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
                Assembly a = lc.LoadFromAssemblyPath(typeof(TopLevelType).Assembly.Location);
                Type t = a.GetType("TopLevelType", throwOnError: true, ignoreCase: false);
                Assert.Equal(null, t.Namespace);
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
