// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class NewTests
    {
        #region Test methods

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewCustomTest(bool useInterpreter)
        {
            Expression<Func<C>> e =
                Expression.Lambda<Func<C>>(
                    Expression.New(typeof(C)),
                    Enumerable.Empty<ParameterExpression>());
            Func<C> f = e.Compile(useInterpreter);

            Assert.Equal(new C(), f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewEnumTest(bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.New(typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            Assert.Equal(new E(), f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewNullableEnumTest(bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.New(typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal(new E?(), f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewNullableIntTest(bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.New(typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(new int?(), f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewStructTest(bool useInterpreter)
        {
            Expression<Func<S>> e =
                Expression.Lambda<Func<S>>(
                    Expression.New(typeof(S)),
                    Enumerable.Empty<ParameterExpression>());
            Func<S> f = e.Compile(useInterpreter);

            Assert.Equal(new S(), f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewStructWithParameterlessConstructorByTypeTest(bool useInterpreter)
        {
            Expression<Func<ValueTypeWithParameterlessConstructor>> e =
                Expression.Lambda<Func<ValueTypeWithParameterlessConstructor>>(
                        Expression.New(typeof(ValueTypeWithParameterlessConstructor)));
            Func<ValueTypeWithParameterlessConstructor> f = e.Compile(useInterpreter);

            ValueTypeWithParameterlessConstructor newValue = f();
            Assert.True(newValue.ConstructorWasRun);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewStructWithParameterlessConstructorByConstructorInfoTest(bool useInterpreter)
        {
            ConstructorInfo ctorInfo = typeof(ValueTypeWithParameterlessConstructor).GetConstructor(Type.EmptyTypes);
            Assert.NotNull(ctorInfo);

            Expression<Func<ValueTypeWithParameterlessConstructor>> e =
                Expression.Lambda<Func<ValueTypeWithParameterlessConstructor>>(
                        Expression.New(ctorInfo));
            Func<ValueTypeWithParameterlessConstructor> f = e.Compile(useInterpreter);

            ValueTypeWithParameterlessConstructor newValue = f();
            Assert.True(newValue.ConstructorWasRun);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewNullableStructWithParameterlessConstructorTest(bool useInterpreter)
        {
            Expression<Func<ValueTypeWithParameterlessConstructorThatThrows?>> e =
                Expression.Lambda<Func<ValueTypeWithParameterlessConstructorThatThrows?>>(
                        Expression.New(typeof(ValueTypeWithParameterlessConstructorThatThrows?)));
            Func<ValueTypeWithParameterlessConstructorThatThrows?> f = e.Compile(useInterpreter);

            ValueTypeWithParameterlessConstructorThatThrows? newValue = f();
            Assert.Null(newValue);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewNullableStructTest(bool useInterpreter)
        {
            Expression<Func<S?>> e =
                Expression.Lambda<Func<S?>>(
                    Expression.New(typeof(S?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<S?> f = e.Compile(useInterpreter);

            Assert.Equal(new S?(), f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewStructWithStringTest(bool useInterpreter)
        {
            Expression<Func<Sc>> e =
                Expression.Lambda<Func<Sc>>(
                    Expression.New(typeof(Sc)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc> f = e.Compile(useInterpreter);

            Assert.Equal(new Sc(), f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewNullableStructWithStringTest(bool useInterpreter)
        {
            Expression<Func<Sc?>> e =
                Expression.Lambda<Func<Sc?>>(
                    Expression.New(typeof(Sc?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc?> f = e.Compile(useInterpreter);

            Assert.Equal(new Sc?(), f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewStructWithStringAndFieldTest(bool useInterpreter)
        {
            Expression<Func<Scs>> e =
                Expression.Lambda<Func<Scs>>(
                    Expression.New(typeof(Scs)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs> f = e.Compile(useInterpreter);

            Assert.Equal(new Scs(), f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewNullableStructWithStringAndFieldTest(bool useInterpreter)
        {
            Expression<Func<Scs?>> e =
                Expression.Lambda<Func<Scs?>>(
                    Expression.New(typeof(Scs?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs?> f = e.Compile(useInterpreter);

            Assert.Equal(new Scs?(), f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewStructWithTwoValuesTest(bool useInterpreter)
        {
            Expression<Func<Sp>> e =
                Expression.Lambda<Func<Sp>>(
                    Expression.New(typeof(Sp)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp> f = e.Compile(useInterpreter);

            Assert.Equal(new Sp(), f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewNullableStructWithTwoValuesTest(bool useInterpreter)
        {
            Expression<Func<Sp?>> e =
                Expression.Lambda<Func<Sp?>>(
                    Expression.New(typeof(Sp?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp?> f = e.Compile(useInterpreter);

            Assert.Equal(new Sp?(), f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewGenericWithStructRestrictionWithEnumTest(bool useInterpreter)
        {
            CheckNewGenericWithStructRestrictionHelper<E>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewGenericWithStructRestrictionWithStructTest(bool useInterpreter)
        {
            CheckNewGenericWithStructRestrictionHelper<S>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewGenericWithStructRestrictionWithStructWithStringAndFieldTest(bool useInterpreter)
        {
            CheckNewGenericWithStructRestrictionHelper<Scs>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewNullableGenericWithStructRestrictionWithEnumTest(bool useInterpreter)
        {
            CheckNewNullableGenericWithStructRestrictionHelper<E>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewNullableGenericWithStructRestrictionWithStructTest(bool useInterpreter)
        {
            CheckNewNullableGenericWithStructRestrictionHelper<S>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewNullableGenericWithStructRestrictionWithStructWithStringAndFieldTest(bool useInterpreter)
        {
            CheckNewNullableGenericWithStructRestrictionHelper<Scs>(useInterpreter);
        }

        #endregion

        #region Generic helpers

        private static void CheckNewGenericWithStructRestrictionHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            Expression<Func<Ts>> e =
                Expression.Lambda<Func<Ts>>(
                    Expression.New(typeof(Ts)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts> f = e.Compile(useInterpreter);

            Assert.Equal(new Ts(), f());
        }

        private static void CheckNewNullableGenericWithStructRestrictionHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            Expression<Func<Ts?>> e =
                Expression.Lambda<Func<Ts?>>(
                    Expression.New(typeof(Ts?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts?> f = e.Compile(useInterpreter);

            Assert.Equal(new Ts?(), f());
        }

        #endregion

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void PrivateDefaultConstructor(bool useInterpreter)
        {
            Assert.Equal("Test instance", TestPrivateDefaultConstructor.GetInstanceFunc(useInterpreter)().ToString());
        }

        class TestPrivateDefaultConstructor
        {
            private TestPrivateDefaultConstructor() { }

            public static Func<TestPrivateDefaultConstructor> GetInstanceFunc(bool useInterpreter)
            {
                Expression<Func<TestPrivateDefaultConstructor>> lambda = Expression.Lambda<Func<TestPrivateDefaultConstructor>>(Expression.New(typeof(TestPrivateDefaultConstructor)), new ParameterExpression[] { });
                return lambda.Compile(useInterpreter);
            }

            public override string ToString() => "Test instance";
        }

        [Fact]
        public static void New_NullConstructor_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("constructor", () => Expression.New((ConstructorInfo)null));
            AssertExtensions.Throws<ArgumentNullException>("constructor", () => Expression.New(null, new Expression[0]));
            AssertExtensions.Throws<ArgumentNullException>("constructor", () => Expression.New(null, (IEnumerable<Expression>)new Expression[0]));

            AssertExtensions.Throws<ArgumentNullException>("constructor", () => Expression.New(null, new Expression[0], new MemberInfo[0]));
            AssertExtensions.Throws<ArgumentNullException>("constructor", () => Expression.New(null, new Expression[0], (IEnumerable<MemberInfo>)new MemberInfo[0]));
        }

        [Fact]
        public static void StaticConstructor_ThrowsArgumentException()
        {
            ConstructorInfo cctor = typeof(StaticCtor).GetTypeInfo().DeclaredConstructors.Single(c => c.IsStatic);

            AssertExtensions.Throws<ArgumentException>("constructor", () => Expression.New(cctor));
            AssertExtensions.Throws<ArgumentException>("constructor", () => Expression.New(cctor, new Expression[0]));
            AssertExtensions.Throws<ArgumentException>("constructor", () => Expression.New(cctor, (IEnumerable<Expression>)new Expression[0]));

            AssertExtensions.Throws<ArgumentException>("constructor", () => Expression.New(cctor, new Expression[0], new MemberInfo[0]));
            AssertExtensions.Throws<ArgumentException>("constructor", () => Expression.New(cctor, new Expression[0], (IEnumerable<MemberInfo>)new MemberInfo[0]));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void Compile_AbstractCtor_ThrowsInvalidOperationException(bool useInterpretation)
        {
            ConstructorInfo ctor = typeof(AbstractCtor).GetTypeInfo().DeclaredConstructors.Single();
            Expression<Func<AbstractCtor>> f = Expression.Lambda<Func<AbstractCtor>>(Expression.New(ctor));

            Assert.Throws<InvalidOperationException>(() => f.Compile(useInterpretation));
        }

        [Fact]
        public static void ConstructorDeclaringType_GenericTypeDefinition_ThrowsArgumentException()
        {
            ConstructorInfo constructor = typeof(GenericClass<>).GetConstructor(new Type[0]);

            AssertExtensions.Throws<ArgumentException>("constructor", () => Expression.New(constructor));
            AssertExtensions.Throws<ArgumentException>("constructor", () => Expression.New(constructor, new Expression[0]));
            AssertExtensions.Throws<ArgumentException>("constructor", () => Expression.New(constructor, (IEnumerable<Expression>)new Expression[0]));

            AssertExtensions.Throws<ArgumentException>("constructor", () => Expression.New(constructor, new Expression[0], new MemberInfo[0]));
            AssertExtensions.Throws<ArgumentException>("constructor", () => Expression.New(constructor, new Expression[0], (IEnumerable<MemberInfo>)new MemberInfo[0]));
        }

        public static IEnumerable<object[]> ConstructorAndArguments_DifferentLengths_TestData()
        {
            yield return new object[] { typeof(ClassWithCtors).GetConstructor(new Type[0]), new Expression[2] };
            yield return new object[] { typeof(ClassWithCtors).GetConstructor(new Type[] { typeof(string) }), new Expression[0] };
            yield return new object[] { typeof(ClassWithCtors).GetConstructor(new Type[] { typeof(string) }), new Expression[2] };
        }

        [Theory]
        [MemberData(nameof(ConstructorAndArguments_DifferentLengths_TestData))]
        public static void ConstructorAndArguments_DifferentLengths_ThrowsArgumentException(ConstructorInfo constructor, Expression[] expressions)
        {
            if (expressions.Length == 0)
            {
                AssertExtensions.Throws<ArgumentException>(null, () => Expression.New(constructor));
            }
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.New(constructor, expressions));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.New(constructor, (IEnumerable<Expression>)expressions));

            AssertExtensions.Throws<ArgumentException>(null, () => Expression.New(constructor, expressions, new MemberInfo[expressions.Length]));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.New(constructor, expressions, (IEnumerable<MemberInfo>)new MemberInfo[expressions.Length]));
        }

        [Fact]
        public static void Arguments_ExpressionNotReadable_ThrowsArgumentException()
        {
            ConstructorInfo constructor = typeof(ClassWithCtors).GetConstructor(new Type[] { typeof(string) });
            Expression[] expressions = new Expression[] { Expression.Property(null, typeof(Unreachable<string>), nameof(Unreachable<string>.WriteOnly)) };

            AssertExtensions.Throws<ArgumentException>("arguments[0]", () => Expression.New(constructor, expressions));
            AssertExtensions.Throws<ArgumentException>("arguments[0]", () => Expression.New(constructor, (IEnumerable<Expression>)expressions));

            AssertExtensions.Throws<ArgumentException>("arguments[0]", () => Expression.New(constructor, expressions, new MemberInfo[1]));
            AssertExtensions.Throws<ArgumentException>("arguments[0]", () => Expression.New(constructor, expressions, (IEnumerable<MemberInfo>)new MemberInfo[1]));
        }

        [Fact]
        public static void ConstructorAndArguments_IncompatibleTypes_ThrowsArgumentException()
        {
            ConstructorInfo constructor = typeof(ClassWithCtors).GetConstructor(new Type[] { typeof(string) });
            Expression[] expressions = new Expression[] { Expression.Constant(5) };

            AssertExtensions.Throws<ArgumentException>("arguments[0]", () => Expression.New(constructor, expressions));
            AssertExtensions.Throws<ArgumentException>("arguments[0]", () => Expression.New(constructor, (IEnumerable<Expression>)expressions));

            MemberInfo[] members = new MemberInfo[] { typeof(ClassWithCtors).GetProperty(nameof(ClassWithCtors.IntProperty)) };
            AssertExtensions.Throws<ArgumentException>("arguments[0]", () => Expression.New(constructor, expressions, members));
            AssertExtensions.Throws<ArgumentException>("arguments[0]", () => Expression.New(constructor, expressions, members));
        }

        public static IEnumerable<object[]> ArgumentsAndMembers_DifferentLengths_TestData()
        {
            yield return new object[] { typeof(ClassWithCtors).GetConstructor(new Type[0]), new Expression[0], new MemberInfo[1] };
            yield return new object[] { typeof(ClassWithCtors).GetConstructor(new Type[] { typeof(string) }), new Expression[1], new MemberInfo[0] };
            yield return new object[] { typeof(ClassWithCtors).GetConstructor(new Type[] { typeof(string) }), new Expression[1], new MemberInfo[2] };
        }

        [Theory]
        [MemberData(nameof(ArgumentsAndMembers_DifferentLengths_TestData))]
        public static void ArgumentsAndMembers_DifferentLengths_ThrowsArgumentException(ConstructorInfo constructor, Expression[] arguments, MemberInfo[] members)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.New(constructor, arguments, members));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.New(constructor, arguments, (IEnumerable<MemberInfo>)members));
        }

        [Fact]
        public static void Members_MemberNotOnDeclaringType_ThrowsArgumentException()
        {
            ConstructorInfo constructor = typeof(ClassWithCtors).GetConstructor(new Type[] { typeof(string) });
            Expression[] arguments = new Expression[] { Expression.Constant("hello") };
            MemberInfo[] members = new MemberInfo[] { typeof(Unreachable<string>).GetProperty(nameof(Unreachable<string>.WriteOnly)) };

            AssertExtensions.Throws<ArgumentException>("members[0]", () => Expression.New(constructor, arguments, members));
            AssertExtensions.Throws<ArgumentException>("members[0]", () => Expression.New(constructor, arguments, (IEnumerable<MemberInfo>)members));
        }

        [Theory]
        [InlineData(nameof(ClassWithCtors.s_field))]
        [InlineData(nameof(ClassWithCtors.StaticProperty))]
        [InlineData(nameof(ClassWithCtors.StaticMethod))]
        public static void Members_StaticMember_ThrowsArgumentException(string memberName)
        {
            ConstructorInfo constructor = typeof(ClassWithCtors).GetConstructor(new Type[] { typeof(string) });
            Expression[] arguments = new Expression[] { Expression.Constant("hello") };
            MemberInfo[] members = new MemberInfo[] { typeof(ClassWithCtors).GetMember(memberName).First() };

            AssertExtensions.Throws<ArgumentException>("members[0]", () => Expression.New(constructor, arguments, members));
            AssertExtensions.Throws<ArgumentException>("members[0]", () => Expression.New(constructor, arguments, (IEnumerable<MemberInfo>)members));
        }

        [Fact]
        public static void Members_MemberWriteOnly_ThrowsArgumentException()
        {
            ConstructorInfo constructor = typeof(ClassWithCtors).GetConstructor(new Type[] { typeof(string) });
            Expression[] arguments = new Expression[] { Expression.Constant("hello") };
            MemberInfo[] members = new MemberInfo[] { typeof(ClassWithCtors).GetProperty(nameof(ClassWithCtors.WriteOnlyProperty)) };

            AssertExtensions.Throws<ArgumentException>("members[0]", () => Expression.New(constructor, arguments, members));
            AssertExtensions.Throws<ArgumentException>("members[0]", () => Expression.New(constructor, arguments, (IEnumerable<MemberInfo>)members));
        }

        [Fact]
        public static void Members_MemberNotPropertyAccessor_ThrowsArgumentException()
        {
            ConstructorInfo constructor = typeof(ClassWithCtors).GetConstructor(new Type[] { typeof(string) });
            Expression[] arguments = new Expression[] { Expression.Constant("hello") };
            MemberInfo[] members = new MemberInfo[] { typeof(ClassWithCtors).GetMethod(nameof(ClassWithCtors.InstanceMethod)) };

            AssertExtensions.Throws<ArgumentException>("members[0]", () => Expression.New(constructor, arguments, members));
            AssertExtensions.Throws<ArgumentException>("members[0]", () => Expression.New(constructor, arguments, (IEnumerable<MemberInfo>)members));
        }

        [Fact]
        public static void Members_MemberNotFieldPropertyOrMethod_ThrowsArgumentException()
        {
            ConstructorInfo constructor = typeof(ClassWithCtors).GetConstructor(new Type[] { typeof(string) });
            Expression[] arguments = new Expression[] { Expression.Constant("hello") };
            MemberInfo[] members = new MemberInfo[] { constructor };

            AssertExtensions.Throws<ArgumentException>("members[0]", () => Expression.New(constructor, arguments, members));
            AssertExtensions.Throws<ArgumentException>("members[0]", () => Expression.New(constructor, arguments, (IEnumerable<MemberInfo>)members));
        }

        [Fact]
        public static void Members_ArgumentTypeAndMemberTypeDontMatch_ThrowsArgumentException()
        {
            ConstructorInfo constructor = typeof(ClassWithCtors).GetConstructor(new Type[] { typeof(string) });
            Expression[] arguments = new Expression[] { Expression.Constant("hello") };
            MemberInfo[] members = new MemberInfo[] { typeof(ClassWithCtors).GetField(nameof(ClassWithCtors._field)) };

            AssertExtensions.Throws<ArgumentException>("arguments[0]", () => Expression.New(constructor, arguments, members));
            AssertExtensions.Throws<ArgumentException>("arguments[0]", () => Expression.New(constructor, arguments, (IEnumerable<MemberInfo>)members));
        }

        [Fact]
        public static void Type_Null_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("type", () => Expression.New((Type)null));
        }

        [Fact]
        public static void ToStringTest()
        {
            NewExpression e1 = Expression.New(typeof(Bar).GetConstructor(Type.EmptyTypes));
            Assert.Equal("new Bar()", e1.ToString());

            NewExpression e2 = Expression.New(typeof(Bar).GetConstructor(new[] { typeof(int) }), Expression.Parameter(typeof(int), "foo"));
            Assert.Equal("new Bar(foo)", e2.ToString());

            NewExpression e3 = Expression.New(typeof(Bar).GetConstructor(new[] { typeof(int), typeof(int) }), Expression.Parameter(typeof(int), "foo"), Expression.Parameter(typeof(int), "qux"));
            Assert.Equal("new Bar(foo, qux)", e3.ToString());

            NewExpression e4 = Expression.New(typeof(Bar).GetConstructor(new[] { typeof(int), typeof(int) }), new[] { Expression.Parameter(typeof(int), "foo"), Expression.Parameter(typeof(int), "qux") }, new[] { typeof(Bar).GetProperty(nameof(Bar.Foo)), typeof(Bar).GetProperty(nameof(Bar.Qux)) });
            Assert.Equal("new Bar(Foo = foo, Qux = qux)", e4.ToString());
        }

        [Fact]
        public static void NullUpdateValidForEmptyParameters()
        {
            NewExpression newExp = Expression.New(typeof(Bar).GetConstructor(Type.EmptyTypes));
            Assert.Same(newExp, newExp.Update(null));
        }

        public static IEnumerable<object[]> Type_InvalidType_TestData()
        {
            yield return new object[] { typeof(void) };
            yield return new object[] { typeof(int).MakeByRefType() };
            yield return new object[] { typeof(StaticCtor) };
            yield return new object[] { typeof(ClassWithNoDefaultCtor) };
            yield return new object[] { typeof(int).MakePointerType() };

            Type listType = typeof(List<>);
            yield return new object[] { listType };
            yield return new object[] { listType.MakeGenericType(listType) };
        }

        [Theory]
        [MemberData(nameof(Type_InvalidType_TestData))]
        public static void Type_InvalidType_ThrowsArgumentException(Type type)
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.New(type));
        }

        public static IEnumerable<object[]> OpenGenericConstructors()
        {
            Type listType = typeof(List<>);
            foreach (Type t in new[] {listType, listType.MakeGenericType(listType)})
            {
                foreach (ConstructorInfo ctor in t.GetConstructors())
                {
                    IEnumerable<Type> types = ctor.GetParameters().Select(p => p.ParameterType);
                    if (!types.Any(pt => pt.ContainsGenericParameters))
                    {
                        yield return new object[] {ctor, types.Select(pt => Expression.Default(pt))};
                    }
                }
            }
        }

        [Theory, MemberData(nameof(OpenGenericConstructors))]
        public static void OpenGenericConstructorsInvalid(ConstructorInfo ctor, Expression[] arguments)
        {
            AssertExtensions.Throws<ArgumentException>("constructor", () => Expression.New(ctor, arguments));
            if (arguments.Length == 0)
            {
                AssertExtensions.Throws<ArgumentException>("constructor", () => Expression.New(ctor));
            }
        }

#if FEATURE_COMPILE
        [Fact]
        public static void GlobalMethodInMembers()
        {
            ModuleBuilder module = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), AssemblyBuilderAccess.RunAndCollect).DefineDynamicModule("Module");
            MethodBuilder globalMethod = module.DefineGlobalMethod("GlobalMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(int), Type.EmptyTypes);
            globalMethod.GetILGenerator().Emit(OpCodes.Ret);
            module.CreateGlobalFunctions();
            MethodInfo globalMethodInfo = module.GetMethod(globalMethod.Name);
            ConstructorInfo constructor = typeof(ClassWithCtors).GetConstructor(new Type[] { typeof(string) });
            Expression[] arguments = { Expression.Constant(5) };
            MemberInfo[] members = { globalMethodInfo };
            AssertExtensions.Throws<ArgumentException>("members[0]", () => Expression.New(constructor, arguments, members));
        }

        [Fact]
        public static void GlobalFieldInMembers()
        {
            ModuleBuilder module = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), AssemblyBuilderAccess.RunAndCollect).DefineDynamicModule("Module");
            FieldBuilder fieldBuilder = module.DefineInitializedData("GlobalField", new byte[1], FieldAttributes.Public);
            module.CreateGlobalFunctions();
            FieldInfo globalField = module.GetField(fieldBuilder.Name);
            ConstructorInfo constructor = typeof(ClassWithCtors).GetConstructor(new Type[] { typeof(string) });
            Expression[] arguments = { Expression.Constant(5) };
            MemberInfo[] members = { globalField };
            AssertExtensions.Throws<ArgumentException>("members[0]", () => Expression.New(constructor, arguments, members));
        }
#endif

        static class StaticCtor
        {
            static StaticCtor() { }
        }

        abstract class AbstractCtor
        {
            public AbstractCtor() { }
        }

        class GenericClass<T>
        {
            public GenericClass() { }
        }

        class ClassWithCtors
        {
            public ClassWithCtors() { }
            public ClassWithCtors(string obj) { }

            public string StringProperty { get; set; }
            public int IntProperty { get; set; }
            public int WriteOnlyProperty { set { } }

#pragma warning disable 0649
            public int _field;
            public static int s_field;
#pragma warning restore 0649

            public static string StaticProperty { get; set; }
            public static void StaticMethod() { }

            public void InstanceMethod() { }
        }

        class ClassWithNoDefaultCtor
        {
            public ClassWithNoDefaultCtor(string s) { }
        }

        static class Unreachable<T>
        {
            public static T WriteOnly { set { } }
        }

        class Bar
        {
            public Bar()
            {
            }

            public Bar(int foo)
            {
            }

            public Bar(int foo, int qux)
            {
            }

            public int Foo { get; set; }
            public int Qux { get; set; }
        }
    }
}
