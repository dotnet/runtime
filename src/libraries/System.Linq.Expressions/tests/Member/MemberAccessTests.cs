// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class MemberAccessTests
    {
        private class UnreadableIndexableClass
        {
            public int this[int index]
            {
                set { }
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckMemberAccessStructInstanceFieldTest(bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Field(
                        Expression.Constant(new FS() { II = 42 }),
                        "II"),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(42, f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckMemberAccessStructStaticFieldTest(bool useInterpreter)
        {
            FS.SI = 42;
            try
            {
                Expression<Func<int>> e =
                    Expression.Lambda<Func<int>>(
                        Expression.Field(
                            null,
                            typeof(FS),
                            "SI"),
                        Enumerable.Empty<ParameterExpression>());
                Func<int> f = e.Compile(useInterpreter);

                Assert.Equal(42, f());
            }
            finally
            {
                FS.SI = 0;
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckMemberAccessStructConstFieldTest(bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Field(
                        null,
                        typeof(FS),
                        "CI"),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(42, f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckMemberAccessStructStaticReadOnlyFieldTest(bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Field(
                        null,
                        typeof(FS),
                        "RI"),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(42, f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckMemberAccessStructInstancePropertyTest(bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Property(
                        Expression.Constant(new PS() { II = 42 }),
                        "II"),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(42, f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckMemberAccessStructStaticPropertyTest(bool useInterpreter)
        {
            PS.SI = 42;
            try
            {
                Expression<Func<int>> e =
                    Expression.Lambda<Func<int>>(
                        Expression.Property(
                            null,
                            typeof(PS),
                            "SI"),
                        Enumerable.Empty<ParameterExpression>());
                Func<int> f = e.Compile(useInterpreter);

                Assert.Equal(42, f());
            }
            finally
            {
                PS.SI = 0;
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void NullNullableValueException(bool useInterpreter)
        {
            string localizedMessage = null;
            try
            {
                int dummy = default(int?).Value;
            }
            catch (InvalidOperationException ioe)
            {
                localizedMessage = ioe.Message;
            }

            Expression<Func<long>> e = () => default(long?).Value;
            Func<long> f = e.Compile(useInterpreter);
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => f());
            Assert.Equal(localizedMessage, exception.Message);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckMemberAccessClassInstanceFieldTest(bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Field(
                        Expression.Constant(new FC() { II = 42 }),
                        "II"),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(42, f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckMemberAccessClassStaticFieldTest(bool useInterpreter)
        {
            FC.SI = 42;
            try
            {
                Expression<Func<int>> e =
                    Expression.Lambda<Func<int>>(
                        Expression.Field(
                            null,
                            typeof(FC),
                            "SI"),
                        Enumerable.Empty<ParameterExpression>());
                Func<int> f = e.Compile(useInterpreter);

                Assert.Equal(42, f());
            }
            finally
            {
                FC.SI = 0;
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckMemberAccessClassConstFieldTest(bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Field(
                        null,
                        typeof(FC),
                        "CI"),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(42, f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckMemberAccessClassStaticReadOnlyFieldTest(bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Field(
                        null,
                        typeof(FC),
                        "RI"),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(42, f());
        }

        [Fact]
        public static void Field_NullField_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("field", () => Expression.Field(null, (FieldInfo)null));
            AssertExtensions.Throws<ArgumentNullException>("fieldName", () => Expression.Field(Expression.Constant(new FC()), (string)null));
            AssertExtensions.Throws<ArgumentNullException>("fieldName", () => Expression.Field(Expression.Constant(new FC()), typeof(FC), (string)null));
        }

        [Fact]
        public static void Field_NullType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("type", () => Expression.Field(Expression.Constant(new FC()), null, "AField"));
        }

        [Fact]
        public static void Field_StaticField_NonNullExpression_ThrowsArgumentException()
        {
            Expression expression = Expression.Constant(new FC());
            AssertExtensions.Throws<ArgumentException>("expression", () => Expression.Field(expression, typeof(FC), nameof(FC.SI)));
            AssertExtensions.Throws<ArgumentException>("expression", () => Expression.Field(expression, typeof(FC).GetField(nameof(FC.SI))));

            AssertExtensions.Throws<ArgumentException>("expression", () => Expression.MakeMemberAccess(expression, typeof(FC).GetField(nameof(FC.SI))));
        }

        [Fact]
        public static void Field_ByrefTypeFieldAccessor_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("propertyName", () => Expression.Property(null, typeof(GenericClass<string>).MakeByRefType(), nameof(GenericClass<string>.Field)));
        }

        [Fact]
        public static void Field_GenericFieldAccessor_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("propertyName", () => Expression.Property(null, typeof(GenericClass<>), nameof(GenericClass<string>.Field)));
        }

        [Fact]
        public static void Field_InstanceField_NullExpression_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentNullException>("expression", () => Expression.Field(null, "fieldName"));
            AssertExtensions.Throws<ArgumentException>("field", () => Expression.Field(null, typeof(FC), nameof(FC.II)));
            AssertExtensions.Throws<ArgumentException>("field", () => Expression.Field(null, typeof(FC).GetField(nameof(FC.II))));

            AssertExtensions.Throws<ArgumentException>("field", () => Expression.MakeMemberAccess(null, typeof(FC).GetField(nameof(FC.II))));
        }

        [Fact]
        public static void Field_ExpressionNotReadable_ThrowsArgumentException()
        {
            Expression expression = Expression.Property(null, typeof(Unreadable<string>), nameof(Unreadable<string>.WriteOnly));

            AssertExtensions.Throws<ArgumentException>("expression", () => Expression.Field(expression, "fieldName"));
            AssertExtensions.Throws<ArgumentException>("expression", () => Expression.Field(expression, typeof(FC), nameof(FC.SI)));
            AssertExtensions.Throws<ArgumentException>("expression", () => Expression.Field(expression, typeof(FC).GetField(nameof(FC.SI))));

            AssertExtensions.Throws<ArgumentException>("expression", () => Expression.MakeMemberAccess(expression, typeof(FC).GetField(nameof(FC.SI))));
        }

        [Fact]
        public static void Field_ExpressionNotTypeOfDeclaringType_ThrowsArgumentException()
        {
            Expression expression = Expression.Constant(new PC());

            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Field(expression, typeof(FC), nameof(FC.II)));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Field(expression, typeof(FC).GetField(nameof(FC.II))));

            AssertExtensions.Throws<ArgumentException>(null, () => Expression.MakeMemberAccess(expression, typeof(FC).GetField(nameof(FC.II))));
        }

        [Fact]
        public static void Field_NoSuchFieldName_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Field(Expression.Constant(new FC()), "NoSuchField"));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Field(Expression.Constant(new FC()), typeof(FC), "NoSuchField"));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckMemberAccessClassInstancePropertyTest(bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Property(
                        Expression.Constant(new PC() { II = 42 }),
                        "II"),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(42, f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckMemberAccessClassStaticPropertyTest(bool useInterpreter)
        {
            PC.SI = 42;
            try
            {
                Expression<Func<int>> e =
                    Expression.Lambda<Func<int>>(
                        Expression.Property(
                            null,
                            typeof(PC),
                            "SI"),
                        Enumerable.Empty<ParameterExpression>());
                Func<int> f = e.Compile(useInterpreter);

                Assert.Equal(42, f());
            }
            finally
            {
                PC.SI = 0;
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckMemberAccessClassInstanceFieldNullReferenceTest(bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Field(
                        Expression.Constant(null, typeof(FC)),
                        "II"),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Throws<NullReferenceException>(() => f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckMemberAccessClassInstanceFieldAssignNullReferenceTest(bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Assign(
                        Expression.Field(
                            Expression.Constant(null, typeof(FC)),
                            "II"),
                        Expression.Constant(1)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Throws<NullReferenceException>(() => f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckMemberAccessClassInstancePropertyNullReferenceTest(bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Property(
                        Expression.Constant(null, typeof(PC)),
                        "II"),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Throws<NullReferenceException>(() => f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckMemberAccessClassInstanceIndexerNullReferenceTest(bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Property(
                        Expression.Constant(null, typeof(PC)),
                        "Item",
                        Expression.Constant(1)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Throws<NullReferenceException>(() => f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckMemberAccessClassInstanceIndexerAssignNullReferenceTest(bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Assign(
                        Expression.Property(
                            Expression.Constant(null, typeof(PC)),
                            "Item",
                            Expression.Constant(1)),
                        Expression.Constant(1)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Throws<NullReferenceException>(() => f());
        }

        [Fact]
        public static void AccessIndexedPropertyWithoutIndex()
        {
            AssertExtensions.Throws<ArgumentException>("property", () => Expression.Property(Expression.Default(typeof(List<int>)), typeof(List<int>).GetProperty("Item")));
        }

        [Fact]
        public static void AccessIndexedPropertyWithoutIndexWriteOnly()
        {
            AssertExtensions.Throws<ArgumentException>("property", () => Expression.Property(Expression.Default(typeof(UnreadableIndexableClass)), typeof(UnreadableIndexableClass).GetProperty("Item")));
        }

        [Fact]
        public static void Property_NullProperty_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("property", () => Expression.Property(null, (PropertyInfo)null));
            AssertExtensions.Throws<ArgumentNullException>("propertyName", () => Expression.Property(Expression.Constant(new PC()), (string)null));
        }

        [Fact]
        public static void Property_NullType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("type", () => Expression.Property(Expression.Constant(new PC()), null, "AProperty"));
        }

        [Fact]
        public static void Property_StaticProperty_NonNullExpression_ThrowsArgumentException()
        {
            Expression expression = Expression.Constant(new PC());
            AssertExtensions.Throws<ArgumentException>("expression", () => Expression.Property(expression, typeof(PC), nameof(PC.SI)));
            AssertExtensions.Throws<ArgumentException>("expression", () => Expression.Property(expression, typeof(PC).GetProperty(nameof(PC.SI))));
            AssertExtensions.Throws<ArgumentException>("expression", () => Expression.Property(expression, typeof(PC).GetProperty(nameof(PC.SI)).GetGetMethod()));

            AssertExtensions.Throws<ArgumentException>("expression", () => Expression.MakeMemberAccess(expression, typeof(PC).GetProperty(nameof(PC.SI))));
        }

        [Fact]
        public static void Property_InstanceProperty_NullExpression_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentNullException>("expression", () => Expression.Property(null, "propertyName"));
            AssertExtensions.Throws<ArgumentException>("property", () => Expression.Property(null, typeof(PC), nameof(PC.II)));
            AssertExtensions.Throws<ArgumentException>("property", () => Expression.Property(null, typeof(PC).GetProperty(nameof(PC.II))));
            AssertExtensions.Throws<ArgumentException>("property", () => Expression.Property(null, typeof(PC).GetProperty(nameof(PC.II)).GetGetMethod()));

            AssertExtensions.Throws<ArgumentException>("property", () => Expression.MakeMemberAccess(null, typeof(PC).GetProperty(nameof(PC.II))));
        }

        [Fact]
        public static void Property_ExpressionNotReadable_ThrowsArgumentException()
        {
            Expression expression = Expression.Property(null, typeof(Unreadable<string>), nameof(Unreadable<string>.WriteOnly));

            AssertExtensions.Throws<ArgumentException>("expression", () => Expression.Property(expression, "fieldName"));
            AssertExtensions.Throws<ArgumentException>("expression", () => Expression.Property(expression, typeof(PC), nameof(PC.SI)));
            AssertExtensions.Throws<ArgumentException>("expression", () => Expression.Property(expression, typeof(PC).GetProperty(nameof(PC.SI))));
            AssertExtensions.Throws<ArgumentException>("expression", () => Expression.Property(expression, typeof(PC).GetProperty(nameof(PC.SI)).GetGetMethod()));
        }

        [Fact]
        public static void Property_ExpressionNotTypeOfDeclaringType_ThrowsArgumentException()
        {
            Expression expression = Expression.Constant(new FC());

            AssertExtensions.Throws<ArgumentException>("property", () => Expression.Property(expression, typeof(PC), nameof(PC.II)));
            AssertExtensions.Throws<ArgumentException>("property", () => Expression.Property(expression, typeof(PC).GetProperty(nameof(PC.II))));
            AssertExtensions.Throws<ArgumentException>("property", () => Expression.Property(expression, typeof(PC).GetProperty(nameof(PC.II)).GetGetMethod()));

            AssertExtensions.Throws<ArgumentException>("property", () => Expression.MakeMemberAccess(expression, typeof(PC).GetProperty(nameof(PC.II))));
        }

        [Fact]
        public static void Property_NoSuchPropertyName_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("propertyName", () => Expression.Property(Expression.Constant(new PC()), "NoSuchProperty"));
            AssertExtensions.Throws<ArgumentException>("propertyName", () => Expression.Property(Expression.Constant(new PC()), typeof(PC), "NoSuchProperty"));
        }

        [Fact]
        public static void Property_NullPropertyAccessor_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("propertyAccessor", () => Expression.Property(Expression.Constant(new PC()), (MethodInfo)null));
        }

        [Fact]
        public static void Property_GenericPropertyAccessor_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("propertyAccessor", () => Expression.Property(null, typeof(GenericClass<>).GetProperty(nameof(GenericClass<string>.Property)).GetGetMethod()));
            AssertExtensions.Throws<ArgumentException>("propertyAccessor", () => Expression.Property(null, typeof(NonGenericClass).GetMethod(nameof(NonGenericClass.GenericMethod))));
            AssertExtensions.Throws<ArgumentException>("property", () => Expression.Property(null, typeof(GenericClass<>).GetProperty(nameof(GenericClass<string>.Property))));
        }

        [Fact]
        public static void Property_PropertyAccessorNotFromProperty_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("propertyAccessor", () => Expression.Property(null, typeof(NonGenericClass).GetMethod(nameof(NonGenericClass.StaticMethod))));
        }

        [Fact]
        public static void Property_ByRefStaticAccess_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("propertyName", () => Expression.Property(null, typeof(NonGenericClass).MakeByRefType(), nameof(NonGenericClass.NonGenericProperty)));
        }

        [Fact]
        public static void PropertyOrField_NullExpression_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("expression", () => Expression.PropertyOrField(null, "APropertyOrField"));
        }

        [Fact]
        public static void PropertyOrField_ExpressionNotReadable_ThrowsArgumentNullException()
        {
            Expression expression = Expression.Property(null, typeof(Unreadable<string>), nameof(Unreadable<string>.WriteOnly));

            AssertExtensions.Throws<ArgumentException>("expression", () => Expression.PropertyOrField(expression, "APropertyOrField"));
        }

        [Fact]
        public static void PropertyOrField_NoSuchPropertyOrField_ThrowsArgumentException()
        {
            Expression expression = Expression.Constant(new PC());
            AssertExtensions.Throws<ArgumentException>("propertyOrFieldName", () => Expression.PropertyOrField(expression, "NoSuchPropertyOrField"));
        }

        [Fact]
        public static void MakeMemberAccess_NullMember_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("member", () => Expression.MakeMemberAccess(Expression.Constant(new PC()), null));
        }

        [Fact]
        public static void MakeMemberAccess_MemberNotFieldOrProperty_ThrowsArgumentException()
        {
            MemberInfo member = typeof(NonGenericClass).GetEvent("Event");

            AssertExtensions.Throws<ArgumentException>("member", () => Expression.MakeMemberAccess(Expression.Constant(new PC()), member));
        }

#if FEATURE_COMPILE
        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/14920", TestRuntimes.Mono)]
        public static void Property_NoGetOrSetAccessors_ThrowsArgumentException()
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder module = assembly.DefineDynamicModule("Module");

            TypeBuilder type = module.DefineType("Type");
            PropertyBuilder property = type.DefineProperty("Property", PropertyAttributes.None, typeof(void), new Type[0]);

            TypeInfo createdType = type.CreateTypeInfo();
            PropertyInfo createdProperty = createdType.DeclaredProperties.First();

            Expression expression = Expression.Constant(Activator.CreateInstance(createdType));

            AssertExtensions.Throws<ArgumentException>("property", () => Expression.Property(expression, createdProperty));
            AssertExtensions.Throws<ArgumentException>("property", () => Expression.Property(expression, createdProperty.Name));

            AssertExtensions.Throws<ArgumentException>("property", () => Expression.PropertyOrField(expression, createdProperty.Name));

            AssertExtensions.Throws<ArgumentException>("property", () => Expression.MakeMemberAccess(expression, createdProperty));
        }
#endif

        [Fact]
        public static void ToStringTest()
        {
            MemberExpression e1 = Expression.Property(null, typeof(DateTime).GetProperty(nameof(DateTime.Now)));
            Assert.Equal("DateTime.Now", e1.ToString());

            MemberExpression e2 = Expression.Property(Expression.Parameter(typeof(DateTime), "d"), typeof(DateTime).GetProperty(nameof(DateTime.Year)));
            Assert.Equal("d.Year", e2.ToString());
        }

        [Fact]
        public static void UpdateSameResturnsSame()
        {
            var exp = Expression.Constant(new PS {II = 42});
            var pro = Expression.Property(exp, nameof(PS.II));
            Assert.Same(pro, pro.Update(exp));
        }

        [Fact]
        public static void UpdateStaticResturnsSame()
        {
            var pro = Expression.Property(null, typeof(PS), nameof(PS.SI));
            Assert.Same(pro, pro.Update(null));
        }

        [Fact]
        public static void UpdateDifferentResturnsDifferent()
        {
            var pro = Expression.Property(Expression.Constant(new PS {II = 42}), nameof(PS.II));
            Assert.NotSame(pro, pro.Update(Expression.Constant(new PS {II = 42})));
        }
    }
}
