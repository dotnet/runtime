// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

using Xunit;

#pragma warning disable 0649 // Yes, we know - test class have fields we don't assign to.

namespace System.Reflection.Tests
{
    public static class MemberInfoNetCoreAppTests
    {
        [Fact]
        public static void HasSameMetadataDefinitionAs_GenericClassMembers()
        {
            Type tGeneric = typeof(GenericTestClass<>);
            IEnumerable<MethodInfo> methodsOnGeneric = tGeneric.GetTypeInfo().GetDeclaredMethods(nameof(GenericTestClass<object>.Foo));

            List<Type> typeInsts = new List<Type>();
            foreach (MethodInfo method in methodsOnGeneric)
            {
                Debug.Assert(method.GetParameters().Length == 1);
                Type parameterType = method.GetParameters()[0].ParameterType;
                typeInsts.Add(tGeneric.MakeGenericType(parameterType));
            }
            typeInsts.Add(tGeneric);
            CrossTestHasSameMethodDefinitionAs(typeInsts.ToArray());
        }

        private static void CrossTestHasSameMethodDefinitionAs(params Type[] types)
        {
            Assert.All(types,
                delegate (Type type1)
                {
                    Assert.All(type1.GenerateTestMemberList(),
                        delegate (MemberInfo m1)
                        {
                            MarkerAttribute mark1 = m1.GetCustomAttribute<MarkerAttribute>();
                            if (mark1 == null)
                                return;

                            Assert.All(types,
                                delegate (Type type2)
                                {
                                    Assert.All(type2.GenerateTestMemberList(),
                                        delegate (MemberInfo m2)
                                        {
                                            MarkerAttribute mark2 = m2.GetCustomAttribute<MarkerAttribute>();
                                            if (mark2 == null)
                                                return;

                                            bool hasSameMetadata = m1.HasSameMetadataDefinitionAs(m2);
                                            Assert.Equal(hasSameMetadata, m2.HasSameMetadataDefinitionAs(m1));

                                            if (hasSameMetadata)
                                            {
                                                Assert.Equal(mark1.Mark, mark2.Mark);
                                            }
                                            else
                                            {
                                                Assert.NotEqual(mark1.Mark, mark2.Mark);
                                            }
                                        }
                                    );
                                }
                            );
                        }
                    );
                }
            );
        }

        [Fact]
        public static void HasSameMetadataDefinitionAs_ReflectedTypeNotPartOfComparison()
        {
            Type tBase = typeof(GenericTestClass<>);
            Type tDerived = typeof(DerivedFromGenericTestClass<int>);
            BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

            IEnumerable<MemberInfo> baseMembers = tBase.GetMembers(bf).Where(m => m.IsDefined(typeof(MarkerAttribute)));
            baseMembers = baseMembers.Where(bm => !(bm is ConstructorInfo)); // Constructors cannot be seen from derived types.
            IEnumerable<MemberInfo> derivedMembers = tDerived.GetMembers(bf).Where(m => m.IsDefined(typeof(MarkerAttribute)));
            Assert.All(baseMembers,
                delegate (MemberInfo baseMember)
                {
                    MemberInfo matchingDerivedMember = derivedMembers.Single(dm => dm.HasSameMarkAs(baseMember));
                    Assert.True(baseMember.HasSameMetadataDefinitionAs(matchingDerivedMember));
                    Assert.True(matchingDerivedMember.HasSameMetadataDefinitionAs(matchingDerivedMember));
                }
            );
        }

        [Fact]
        public static void HasSameMetadataDefinitionAs_ConstructedGenericMethods()
        {
            Type t1 = typeof(TestClassWithGenericMethod<>);
            Type theT = t1.GetTypeInfo().GenericTypeParameters[0];

            MethodInfo mooNormal = t1.GetConfirmedMethod(nameof(TestClassWithGenericMethod<object>.Moo), theT);
            MethodInfo mooGeneric = t1.GetTypeInfo().GetDeclaredMethods("Moo").Single(m => m.IsGenericMethod);

            MethodInfo mooInst = mooGeneric.MakeGenericMethod(typeof(int));
            Assert.True(mooGeneric.HasSameMetadataDefinitionAs(mooInst));
            Assert.True(mooInst.HasSameMetadataDefinitionAs(mooGeneric));

            MethodInfo mooInst2 = mooGeneric.MakeGenericMethod(typeof(double));
            Assert.True(mooInst2.HasSameMetadataDefinitionAs(mooInst));
            Assert.True(mooInst.HasSameMetadataDefinitionAs(mooInst2));

            Type t2 = typeof(TestClassWithGenericMethod<int>);
            MethodInfo mooNormalOnT2 = t2.GetConfirmedMethod(nameof(TestClassWithGenericMethod<object>.Moo), typeof(int));
            Assert.False(mooInst.HasSameMetadataDefinitionAs(mooNormalOnT2));
        }

        [Fact]
        public static void HasSameMetadataDefinitionAs_NamedAndGenericTypes()
        {
            Type tnong = typeof(TestClass);
            Type tnong2 = typeof(TestClass2);
            Type tg = typeof(GenericTestClass<>);
            Type tginst1 = typeof(GenericTestClass<int>);
            Type tginst2 = typeof(GenericTestClass<string>);


            Assert.True(tnong.HasSameMetadataDefinitionAs(tnong));
            Assert.True(tnong2.HasSameMetadataDefinitionAs(tnong2));
            Assert.True(tg.HasSameMetadataDefinitionAs(tg));
            Assert.True(tginst1.HasSameMetadataDefinitionAs(tginst1));
            Assert.True(tginst2.HasSameMetadataDefinitionAs(tginst2));

            Assert.True(tg.HasSameMetadataDefinitionAs(tginst1));
            Assert.True(tginst1.HasSameMetadataDefinitionAs(tginst1));
            Assert.True(tginst1.HasSameMetadataDefinitionAs(tginst2));

            Assert.False(tnong.HasSameMetadataDefinitionAs(tnong2));
            Assert.False(tnong.HasSameMetadataDefinitionAs(tg));
            Assert.False(tg.HasSameMetadataDefinitionAs(tnong));
            Assert.False(tnong.HasSameMetadataDefinitionAs(tginst1));
            Assert.False(tginst1.HasSameMetadataDefinitionAs(tnong));
        }

        [Fact]
        public static void HasSameMetadataDefinitionAs_GenericTypeParameters()
        {
            Type theT = typeof(GenericTestClass<>).GetTypeInfo().GenericTypeParameters[0];
            Type theT2 = typeof(TestClassWithGenericMethod<>).GetTypeInfo().GenericTypeParameters[0];

            BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.ExactBinding;
            MethodInfo mooNong = theT2.GetMethod("Moo", bf, null, new Type[] { theT2 }, null);
            MethodInfo mooGeneric = typeof(TestClassWithGenericMethod<>).GetTypeInfo().GetDeclaredMethods("Moo").Single(m => m.IsGenericMethod);
            Type theM = mooGeneric.GetGenericArguments()[0];

            Assert.True(theT.HasSameMetadataDefinitionAs(theT));
            Assert.False(theT2.HasSameMetadataDefinitionAs(theT));
            Assert.False(theT.HasSameMetadataDefinitionAs(theT2));
            Assert.False(theM.HasSameMetadataDefinitionAs(theT));
            Assert.False(theM.HasSameMetadataDefinitionAs(theT2));
            Assert.False(theT2.HasSameMetadataDefinitionAs(theM));
            Assert.False(theT.HasSameMetadataDefinitionAs(theM));
        }

        [Fact]
        public static void HasSameMetadataDefinitionAs_Twins()
        {
            // This situation is particularly treacherous for CoreRT as the .Net Native toolchain can and does assign
            // the same native metadata tokens to identically structured members in unrelated types.
            Type twin1 = typeof(Twin1);
            Type twin2 = typeof(Twin2);

            Assert.All(twin1.GenerateTestMemberList(),
                delegate (MemberInfo m1)
                {
                    Assert.All(twin2.GenerateTestMemberList(),
                        delegate (MemberInfo m2)
                        {
                            Assert.False(m1.HasSameMetadataDefinitionAs(m2));
                        }
                     );
                }
            );
        }

        private class Twin1
        {
            public Twin1() { }
            public int Field1;
            public Action Event1;
            public void Method1() { }
            public int Property1 { get; set; }
        }

        private class Twin2
        {
            public Twin2() { }
            public int Field1;
            public Action Event1;
            public void Method1() { }
            public int Property1 { get; set; }
        }

        [Fact]
        [OuterLoop] // Time-consuming.
        public static void HasSameMetadataDefinitionAs_CrossAssembly()
        {
            // Make sure that identical tokens in different assemblies don't confuse the api.
            foreach (Type t1 in typeof(object).Assembly.DefinedTypes)
            {
                foreach (Type t2 in typeof(MemberInfoNetCoreAppTests).Assembly.DefinedTypes)
                {
                    Assert.False(t1.HasSameMetadataDefinitionAs(t2));
                }
            }
        }

        [Theory]
        [MemberData(nameof(NegativeTypeData))]
        public static void HasSameMetadataDefinitionAs_Negative_NonRuntimeType(Type type)
        {
            Type mockType = new MockType();
            Assert.False(type.HasSameMetadataDefinitionAs(mockType));
            Assert.All(type.GenerateTestMemberList(),
                delegate (MemberInfo member)
                {
                    Assert.False(member.HasSameMetadataDefinitionAs(mockType));
                }
            );
        }

        [Theory]
        [MemberData(nameof(NegativeTypeData))]
        public static void HasSameMetadataDefinitionAs_Negative_Null(Type type)
        {
            AssertExtensions.Throws<ArgumentNullException>("other", () => type.HasSameMetadataDefinitionAs(null));
            Assert.All(type.GenerateTestMemberList(),
                delegate (MemberInfo member)
                {
                    AssertExtensions.Throws<ArgumentNullException>("other", () => member.HasSameMetadataDefinitionAs(null));
                }
            );
        }

        public static IEnumerable<object[]> NegativeTypeData => NegativeTypeDataRaw.Select(t => new object[] { t });
        private static IEnumerable<Type> NegativeTypeDataRaw
        {
            get
            {
                yield return typeof(TestClass);
                yield return typeof(GenericTestClass<>);
                yield return typeof(GenericTestClass<int>);

                yield return typeof(int[]);
                yield return typeof(int).MakeArrayType(1);
                yield return typeof(int[,]);
                yield return typeof(int).MakeByRefType();
                yield return typeof(int).MakePointerType();

                yield return typeof(GenericTestClass<>).GetTypeInfo().GenericTypeParameters[0];
                if (PlatformDetection.IsWindows)
                    yield return Type.GetTypeFromCLSID(new Guid("DCA66D18-E253-4695-9E08-35B54420AFA2"));
            }
        }

        [Fact]
        public static void HasSameMetadataDefinitionAs__CornerCase_HasElementTypes()
        {
            // HasSameMetadataDefinitionAs on an array/byref/pointer type is uninteresting (they'll never be an actual member of a type)
            // but for future compat, we'll establish their known behavior here. Since these types all return a MetadataToken of 0x02000000,
            // they'll all "match" each other.

            Type[] types =
            {
                typeof(int[]),
                typeof(double[]),
                typeof(int).MakeArrayType(1),
                typeof(double).MakeArrayType(1),
                typeof(int[,]),
                typeof(double[,]),
                typeof(int).MakeByRefType(),
                typeof(double).MakeByRefType(),
                typeof(int).MakePointerType(),
                typeof(double).MakePointerType(),
            };

            Assert.All(types,
                delegate (Type t1)
                {
                    Assert.All(types, t2 => Assert.True(t1.HasSameMetadataDefinitionAs(t2)));
                }
            );
        }

        [Fact]
        public static void HasSameMetadataDefinitionAs_CornerCase_ArrayMethods()
        {
            // The magic methods and constructors exposed on array types do not have metadata backing and report a MetadataToken of 0x06000000
            // and hence compare identically with each other. This may be surprising but this test records that fact for future compat.
            //
            Type[] arrayTypes =
            {
                typeof(int[]),
                typeof(double[]),
                typeof(int).MakeArrayType(1),
                typeof(double).MakeArrayType(1),
                typeof(int[,]),
                typeof(double[,]),
            };

            List<MemberInfo> members = new List<MemberInfo>();
            foreach (Type arrayType in arrayTypes)
            {
                foreach (MemberInfo member in arrayType.GetMembers(BindingFlags.Public|BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (member is MethodBase)
                    {
                        members.Add(member);
                    }
                }
            }

            Assert.All(members,
                delegate (MemberInfo member1)
                {
                    Assert.All(members,
                        delegate (MemberInfo member2)
                        {
                            if (member1.MemberType == member2.MemberType)
                                Assert.True(member1.HasSameMetadataDefinitionAs(member2));
                            else
                                Assert.False(member1.HasSameMetadataDefinitionAs(member2));
                        }
                    );
                }
            );
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void HasSameMetadataDefinitionAs_CornerCase_CLSIDConstructor()
        {
            // HasSameMetadataDefinitionAs on a GetTypeFromCLSID type is uninteresting (they'll never be an actual member of a type)
            // but for future compat, we'll establish their known behavior here. Since these types and their constructors all return the same 
            // MetadataToken, they'll all "match" each other.

            Type t1 = Type.GetTypeFromCLSID(new Guid("23A54889-7738-4F16-8AB2-CB23F8E756BE"));
            Type t2 = Type.GetTypeFromCLSID(new Guid("DCA66D18-E253-4695-9E08-35B54420AFA2"));

            Assert.True(t1.HasSameMetadataDefinitionAs(t2));

            ConstructorInfo c1 = t1.GetConfirmedConstructor();
            ConstructorInfo c2 = t2.GetConfirmedConstructor();

            Assert.True(c1.HasSameMetadataDefinitionAs(c2));
        }


        private static bool HasSameMarkAs(this MemberInfo m1, MemberInfo m2)
        {
            MarkerAttribute marker1 = m1.GetCustomAttribute<MarkerAttribute>();
            Assert.NotNull(marker1);

            MarkerAttribute marker2 = m2.GetCustomAttribute<MarkerAttribute>();
            Assert.NotNull(marker2);

            return marker1.Mark == marker2.Mark;
        }

        private static MethodInfo GetConfirmedMethod(this Type t, string name, params Type[] parameterTypes)
        {
            BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.ExactBinding;
            MethodInfo method = t.GetMethod(name, bf, null, parameterTypes, null);
            Assert.NotNull(method);
            return method;
        }

        private static ConstructorInfo GetConfirmedConstructor(this Type t, params Type[] parameterTypes)
        {
            BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.ExactBinding;
            ConstructorInfo ctor = t.GetConstructor(bf, null, parameterTypes, null);
            Assert.NotNull(ctor);
            return ctor;
        }

        private static IEnumerable<MemberInfo> GenerateTestMemberList(this Type t)
        {
            if (t.IsGenericTypeDefinition)
            {
                foreach (Type gp in t.GetTypeInfo().GenericTypeParameters)
                {
                    yield return gp;
                }
            }
            foreach (MemberInfo m in t.GetTypeInfo().DeclaredMembers)
            {
                yield return m;
                MethodInfo method = m as MethodInfo;
                if (method != null && method.IsGenericMethodDefinition)
                {
                    foreach (Type mgp in method.GetGenericArguments())
                    {
                        yield return mgp;
                    }
                    yield return method.MakeGenericMethod(method.GetGenericArguments().Select(ga => typeof(object)).ToArray());
                }
            }
        }

        private class TestClassWithGenericMethod<T>
        {
            public void Moo(T t) { }
            public void Moo<M>(M m) { }
        }

        private class TestClass
        {
            public void Foo() { }
            public void Foo(object o) { }
            public void Foo(string s) { }
            public void Bar() { }
        }

        private class TestClass2 { }


        private class GenericTestClass<T>
        {
            [Marker(1)]
            public void Foo(object o) { }
            [Marker(2)]
            public void Foo(int i) { }
            [Marker(3)]
            public void Foo(T t) { }
            [Marker(4)]
            public void Foo(double d) { }
            [Marker(5)]
            public void Foo(string s) { }
            [Marker(6)]
            public void Foo(int[] s) { }
            [Marker(7)]
            public void Foo<U>(U t) { }
            [Marker(8)]
            public void Foo<U>(T t) { }
            [Marker(9)]
            public void Foo<U, V>(T t) { }

            [Marker(101)]
            public GenericTestClass() { }
            [Marker(102)]
            public GenericTestClass(T t) { }
            [Marker(103)]
            public GenericTestClass(int t) { }

            [Marker(201)]
            public int Field1;
            [Marker(202)]
            public int Field2;
            [Marker(203)]
            public T Field3;

            [Marker(301)]
            public int Property1 { get { throw null; } set { throw null; } }
            [Marker(302)]
            public int Property2 { get { throw null; } set { throw null; } }
            [Marker(303)]
            public T Property3 { get { throw null; } set { throw null; } }

            [Marker(401)]
            public event Action Event1 { add { } remove { } }
            [Marker(402)]
            public event Action<int> Event2 { add { } remove { } }
            [Marker(403)]
            public event Action<T> Event3 { add { } remove { } }
        }

        private class DerivedFromGenericTestClass<T> : GenericTestClass<T> { }

        [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
        public class MarkerAttribute : Attribute
        {
            public MarkerAttribute(int mark)
            {
                Mark = mark;
            }

            public readonly int Mark;
        }
    }
}

