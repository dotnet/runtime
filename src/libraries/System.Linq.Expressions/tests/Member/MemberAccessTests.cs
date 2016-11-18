// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
            Assert.Throws<ArgumentNullException>("field", () => Expression.Field(null, (FieldInfo)null));
            Assert.Throws<ArgumentNullException>("fieldName", () => Expression.Field(Expression.Constant(new FC()), (string)null));
        }

        [Fact]
        public static void Field_NullType_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("type", () => Expression.Field(Expression.Constant(new FC()), null, "AField"));
        }

        [Fact]
        public static void Field_StaticField_NonNullExpression_ThrowsArgumentException()
        {
            Expression expression = Expression.Constant(new FC());
            Assert.Throws<ArgumentException>("expression", () => Expression.Field(expression, typeof(FC), nameof(FC.SI)));
            Assert.Throws<ArgumentException>("expression", () => Expression.Field(expression, typeof(FC).GetField(nameof(FC.SI))));

            Assert.Throws<ArgumentException>("expression", () => Expression.MakeMemberAccess(expression, typeof(FC).GetField(nameof(FC.SI))));
        }

        [Fact]
        public static void Field_InstanceField_NullExpression_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentNullException>("expression", () => Expression.Field(null, "fieldName"));
            Assert.Throws<ArgumentException>("field", () => Expression.Field(null, typeof(FC), nameof(FC.II)));
            Assert.Throws<ArgumentException>("field", () => Expression.Field(null, typeof(FC).GetField(nameof(FC.II))));

            Assert.Throws<ArgumentException>("field", () => Expression.MakeMemberAccess(null, typeof(FC).GetField(nameof(FC.II))));
        }

        [Fact]
        public static void Field_ExpressionNotReadable_ThrowsArgumentException()
        {
            Expression expression = Expression.Property(null, typeof(Unreadable<string>), nameof(Unreadable<string>.WriteOnly));

            Assert.Throws<ArgumentException>("expression", () => Expression.Field(expression, "fieldName"));
            Assert.Throws<ArgumentException>("expression", () => Expression.Field(expression, typeof(FC), nameof(FC.SI)));
            Assert.Throws<ArgumentException>("expression", () => Expression.Field(expression, typeof(FC).GetField(nameof(FC.SI))));

            Assert.Throws<ArgumentException>("expression", () => Expression.MakeMemberAccess(expression, typeof(FC).GetField(nameof(FC.SI))));
        }

        [Fact]
        public static void Field_ExpressionNotTypeOfDeclaringType_ThrowsArgumentException()
        {
            Expression expression = Expression.Constant(new PC());

            Assert.Throws<ArgumentException>(null, () => Expression.Field(expression, typeof(FC), nameof(FC.II)));
            Assert.Throws<ArgumentException>(null, () => Expression.Field(expression, typeof(FC).GetField(nameof(FC.II))));

            Assert.Throws<ArgumentException>(null, () => Expression.MakeMemberAccess(expression, typeof(FC).GetField(nameof(FC.II))));
        }

        [Fact]
        public static void Field_NoSuchFieldName_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(null, () => Expression.Field(Expression.Constant(new FC()), "NoSuchField"));
            Assert.Throws<ArgumentException>(null, () => Expression.Field(Expression.Constant(new FC()), typeof(FC), "NoSuchField"));
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
            Assert.Throws<ArgumentException>("property", () => Expression.Property(Expression.Default(typeof(List<int>)), typeof(List<int>).GetProperty("Item")));
        }

        [Fact]
        public static void AccessIndexedPropertyWithoutIndexWriteOnly()
        {
            Assert.Throws<ArgumentException>("property", () => Expression.Property(Expression.Default(typeof(UnreadableIndexableClass)), typeof(UnreadableIndexableClass).GetProperty("Item")));
        }

        [Fact]
        public static void Property_NullProperty_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("property", () => Expression.Property(null, (PropertyInfo)null));
            Assert.Throws<ArgumentNullException>("propertyName", () => Expression.Property(Expression.Constant(new PC()), (string)null));
        }

        [Fact]
        public static void Property_NullType_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("type", () => Expression.Property(Expression.Constant(new PC()), null, "AProperty"));
        }

        [Fact]
        public static void Property_StaticProperty_NonNullExpression_ThrowsArgumentException()
        {
            Expression expression = Expression.Constant(new PC());
            Assert.Throws<ArgumentException>("expression", () => Expression.Property(expression, typeof(PC), nameof(PC.SI)));
            Assert.Throws<ArgumentException>("expression", () => Expression.Property(expression, typeof(PC).GetProperty(nameof(PC.SI))));
            Assert.Throws<ArgumentException>("expression", () => Expression.Property(expression, typeof(PC).GetProperty(nameof(PC.SI)).GetGetMethod()));

            Assert.Throws<ArgumentException>("expression", () => Expression.MakeMemberAccess(expression, typeof(PC).GetProperty(nameof(PC.SI))));
        }

        [Fact]
        public static void Property_InstanceProperty_NullExpression_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentNullException>("expression", () => Expression.Property(null, "propertyName"));
            Assert.Throws<ArgumentException>("property", () => Expression.Property(null, typeof(PC), nameof(PC.II)));
            Assert.Throws<ArgumentException>("property", () => Expression.Property(null, typeof(PC).GetProperty(nameof(PC.II))));
            Assert.Throws<ArgumentException>("property", () => Expression.Property(null, typeof(PC).GetProperty(nameof(PC.II)).GetGetMethod()));

            Assert.Throws<ArgumentException>("property", () => Expression.MakeMemberAccess(null, typeof(PC).GetProperty(nameof(PC.II))));
        }

        [Fact]
        public static void Property_ExpressionNotReadable_ThrowsArgumentException()
        {
            Expression expression = Expression.Property(null, typeof(Unreadable<string>), nameof(Unreadable<string>.WriteOnly));

            Assert.Throws<ArgumentException>("expression", () => Expression.Property(expression, "fieldName"));
            Assert.Throws<ArgumentException>("expression", () => Expression.Property(expression, typeof(PC), nameof(PC.SI)));
            Assert.Throws<ArgumentException>("expression", () => Expression.Property(expression, typeof(PC).GetProperty(nameof(PC.SI))));
            Assert.Throws<ArgumentException>("expression", () => Expression.Property(expression, typeof(PC).GetProperty(nameof(PC.SI)).GetGetMethod()));
        }

        [Fact]
        public static void Property_ExpressionNotTypeOfDeclaringType_ThrowsArgumentException()
        {
            Expression expression = Expression.Constant(new FC());

            Assert.Throws<ArgumentException>("property", () => Expression.Property(expression, typeof(PC), nameof(PC.II)));
            Assert.Throws<ArgumentException>("property", () => Expression.Property(expression, typeof(PC).GetProperty(nameof(PC.II))));
            Assert.Throws<ArgumentException>("property", () => Expression.Property(expression, typeof(PC).GetProperty(nameof(PC.II)).GetGetMethod()));

            Assert.Throws<ArgumentException>("property", () => Expression.MakeMemberAccess(expression, typeof(PC).GetProperty(nameof(PC.II))));
        }

        [Fact]
        public static void Property_NoSuchPropertyName_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>("propertyName", () => Expression.Property(Expression.Constant(new PC()), "NoSuchProperty"));
            Assert.Throws<ArgumentException>("propertyName", () => Expression.Property(Expression.Constant(new PC()), typeof(PC), "NoSuchProperty"));
        }

        [Fact]
        public static void Property_NullPropertyAccessor_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("propertyAccessor", () => Expression.Property(Expression.Constant(new PC()), (MethodInfo)null));
        }

        [Fact]
        public static void Property_GenericPropertyAccessor_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>("propertyAccessor", () => Expression.Property(null, typeof(GenericClass<>).GetMethod(nameof(GenericClass<string>.Method))));
            Assert.Throws<ArgumentException>("propertyAccessor", () => Expression.Property(null, typeof(NonGenericClass).GetMethod(nameof(NonGenericClass.GenericMethod))));
        }

        [Fact]
        public static void Property_PropertyAccessorNotFromProperty_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>("propertyAccessor", () => Expression.Property(null, typeof(NonGenericClass).GetMethod(nameof(NonGenericClass.StaticMethod))));
        }

        [Fact]
        public static void PropertyOrField_NullExpression_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("expression", () => Expression.PropertyOrField(null, "APropertyOrField"));
        }

        [Fact]
        public static void PropertyOrField_ExpressionNotReadable_ThrowsArgumentNullException()
        {
            Expression expression = Expression.Property(null, typeof(Unreadable<string>), nameof(Unreadable<string>.WriteOnly));

            Assert.Throws<ArgumentException>("expression", () => Expression.PropertyOrField(expression, "APropertyOrField"));
        }

        [Fact]
        public static void PropertyOrField_NoSuchPropertyOrField_ThrowsArgumentException()
        {
            Expression expression = Expression.Constant(new PC());
            Assert.Throws<ArgumentException>("propertyOrFieldName", () => Expression.PropertyOrField(expression, "NoSuchPropertyOrField"));
        }

        [Fact]
        public static void MakeMemberAccess_NullMember_ThrowsArgumentNullExeption()
        {
            Assert.Throws<ArgumentNullException>("member", () => Expression.MakeMemberAccess(Expression.Constant(new PC()), null));
        }

        [Fact]
        public static void MakeMemberAccess_MemberNotFieldOrProperty_ThrowsArgumentExeption()
        {
            MemberInfo member = typeof(NonGenericClass).GetEvent("Event");

            Assert.Throws<ArgumentException>("member", () => Expression.MakeMemberAccess(Expression.Constant(new PC()), member));
        }

        [Fact]
        public static void Property_NoGetOrSetAccessors_ThrowsArgumentException()
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), AssemblyBuilderAccess.Run);
            ModuleBuilder module = assembly.DefineDynamicModule("Module");

            TypeBuilder type = module.DefineType("Type");
            PropertyBuilder property = type.DefineProperty("Property", PropertyAttributes.None, typeof(void), new Type[0]);

            TypeInfo createdType = type.CreateTypeInfo();
            PropertyInfo createdProperty = createdType.DeclaredProperties.First();

            Expression expression = Expression.Constant(Activator.CreateInstance(createdType.AsType()));

            Assert.Throws<ArgumentException>("property", () => Expression.Property(expression, createdProperty));
            Assert.Throws<ArgumentException>("property", () => Expression.Property(expression, createdProperty.Name));

            Assert.Throws<ArgumentException>("property", () => Expression.PropertyOrField(expression, createdProperty.Name));

            Assert.Throws<ArgumentException>("property", () => Expression.MakeMemberAccess(expression, createdProperty));
        }

        [Fact]
        public static void ToStringTest()
        {
            var e1 = Expression.Property(null, typeof(DateTime).GetProperty(nameof(DateTime.Now)));
            Assert.Equal("DateTime.Now", e1.ToString());

            var e2 = Expression.Property(Expression.Parameter(typeof(DateTime), "d"), typeof(DateTime).GetProperty(nameof(DateTime.Year)));
            Assert.Equal("d.Year", e2.ToString());
        }
    }
}
