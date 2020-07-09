// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class IndexExpressionTests
    {
        [Fact]
        public void UpdateSameTest()
        {
            SampleClassWithProperties instance = new SampleClassWithProperties { DefaultProperty = new List<int> { 100, 101 } };
            IndexExpression expr = instance.DefaultIndexExpression;

            IndexExpression exprUpdated = expr.Update(expr.Object, instance.DefaultArguments);

            // Has to be the same, because everything is the same.
            Assert.Same(expr, exprUpdated);

            // Invoke to check expression.
            IndexExpressionHelpers.AssertInvokeCorrect(100, expr);
            IndexExpressionHelpers.AssertInvokeCorrect(100, exprUpdated);
        }

        [Fact]
        public void UpdateDoesntRepeatEnumeration()
        {
            SampleClassWithProperties instance = new SampleClassWithProperties { DefaultProperty = new List<int> { 100, 101 } };
            IndexExpression expr = instance.DefaultIndexExpression;

            Assert.Same(expr, expr.Update(expr.Object, new RunOnceEnumerable<Expression>(instance.DefaultArguments)));
        }

        [Fact]
        public void UpdateDifferentObjectTest()
        {
            SampleClassWithProperties instance = new SampleClassWithProperties { DefaultProperty = new List<int> { 100, 101 } };
            IndexExpression expr = instance.DefaultIndexExpression;

            Assert.NotSame(expr, expr.Update(instance.DefaultPropertyExpression, instance.DefaultArguments));
        }

        [Fact]
        public void UpdateDifferentArgumentsTest()
        {
            SampleClassWithProperties instance = new SampleClassWithProperties { DefaultProperty = new List<int> { 100, 101 } };
            IndexExpression expr = instance.DefaultIndexExpression;

            Assert.NotSame(expr, expr.Update(expr.Object, new [] { Expression.Constant(0)}));
        }

        [Fact]
        public void UpdateTest()
        {
            SampleClassWithProperties instance = new SampleClassWithProperties
            {
                DefaultProperty = new List<int> { 100, 101 },
                AlternativeProperty = new List<int> { 200, 201 }
            };

            IndexExpression expr = instance.DefaultIndexExpression;
            MemberExpression newProperty = Expression.Property(Expression.Constant(instance),
                typeof(SampleClassWithProperties).GetProperty(nameof(instance.AlternativeProperty)));
            ConstantExpression[] newArguments = {Expression.Constant(1)};

            IndexExpression exprUpdated = expr.Update(newProperty, newArguments);

            // Replace Object and Arguments of IndexExpression.
            IndexExpressionHelpers.AssertEqual(
                exprUpdated,
                Expression.MakeIndex(newProperty, instance.DefaultIndexer, newArguments));

            // Invoke to check expression.
            IndexExpressionHelpers.AssertInvokeCorrect(100, expr);
            IndexExpressionHelpers.AssertInvokeCorrect(201, exprUpdated);
        }

        [Fact]
        public static void ToStringTest()
        {
            IndexExpression e1 = Expression.MakeIndex(Expression.Parameter(typeof(Vector1), "v"), typeof(Vector1).GetProperty("Item"), new[] { Expression.Parameter(typeof(int), "i") });
            Assert.Equal("v.Item[i]", e1.ToString());

            IndexExpression e2 = Expression.MakeIndex(Expression.Parameter(typeof(Vector2), "v"), typeof(Vector2).GetProperty("Item"), new[] { Expression.Parameter(typeof(int), "i"), Expression.Parameter(typeof(int), "j") });
            Assert.Equal("v.Item[i, j]", e2.ToString());

            IndexExpression e3 = Expression.ArrayAccess(Expression.Parameter(typeof(int[,]), "xs"), Expression.Parameter(typeof(int), "i"), Expression.Parameter(typeof(int), "j"));
            Assert.Equal("xs[i, j]", e3.ToString());
        }

#if FEATURE_COMPILE
        private static TypeBuilder GetTestTypeBuilder() =>
            AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("TestAssembly"), AssemblyBuilderAccess.RunAndCollect)
                .DefineDynamicModule("TestModule")
                .DefineType("TestType");

        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/14920", TestRuntimes.Mono)]
        public void NoAccessorIndexedProperty()
        {
            TypeBuilder typeBuild = GetTestTypeBuilder();

            typeBuild.DefineProperty("Item", PropertyAttributes.None, typeof(int), new[] { typeof(int) });

            TypeInfo info = typeBuild.CreateTypeInfo();
            Type type = info;
            PropertyInfo prop = info.DeclaredProperties.First();
            Expression instance = Expression.Default(type);
            AssertExtensions.Throws<ArgumentException>("indexer", () => Expression.Property(instance, prop, Expression.Constant(0)));
            AssertExtensions.Throws<ArgumentException>("propertyName", () => Expression.Property(instance, "Item", Expression.Constant(0)));
        }

        [Fact]
        public void ByRefIndexedProperty()
        {
            TypeBuilder typeBuild = GetTestTypeBuilder();
            FieldBuilder field = typeBuild.DefineField("_value", typeof(int), FieldAttributes.Private);

            PropertyBuilder property = typeBuild.DefineProperty(
                "Item", PropertyAttributes.None, typeof(int).MakeByRefType(), new[] {typeof(int)});

            MethodBuilder getter = typeBuild.DefineMethod(
                "get_Item",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig
                | MethodAttributes.PrivateScope,
                typeof(int).MakeByRefType(),
                new[] {typeof(int)});

            ILGenerator ilGen = getter.GetILGenerator();
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldflda, field);
            ilGen.Emit(OpCodes.Ret);

            property.SetGetMethod(getter);

            TypeInfo info = typeBuild.CreateTypeInfo();
            Type type = info;
            PropertyInfo prop = type.GetProperties()[0];
            Expression instance = Expression.Default(type);
            AssertExtensions.Throws<ArgumentException>("indexer", () => Expression.Property(instance, prop, Expression.Constant(0)));
            AssertExtensions.Throws<ArgumentException>("propertyName", () => Expression.Property(instance, "Item", Expression.Constant(0)));
        }

        [Fact]
        public void VoidIndexedProperty()
        {
            TypeBuilder typeBuild = GetTestTypeBuilder();

            PropertyBuilder property = typeBuild.DefineProperty(
                "Item", PropertyAttributes.None, typeof(void), new[] { typeof(int) });

            MethodBuilder getter = typeBuild.DefineMethod(
                "get_Item",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig
                | MethodAttributes.PrivateScope,
                typeof(void),
                new[] { typeof(int) });

            ILGenerator ilGen = getter.GetILGenerator();
            ilGen.Emit(OpCodes.Ret);

            property.SetGetMethod(getter);

            TypeInfo info = typeBuild.CreateTypeInfo();
            Type type = info;
            PropertyInfo prop = type.GetProperties()[0];
            Expression instance = Expression.Default(type);
            AssertExtensions.Throws<ArgumentException>("indexer", () => Expression.Property(instance, prop, Expression.Constant(0)));
            AssertExtensions.Throws<ArgumentException>("propertyName", () => Expression.Property(instance, "Item", Expression.Constant(0)));
        }

        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/14927", TestRuntimes.Mono)]
        public void IndexedPropertyGetReturnsWrongType()
        {
            TypeBuilder typeBuild = GetTestTypeBuilder();

            PropertyBuilder property = typeBuild.DefineProperty(
                "Item", PropertyAttributes.None, typeof(int), new[] { typeof(int) });

            MethodBuilder getter = typeBuild.DefineMethod(
                "get_Item",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig
                | MethodAttributes.PrivateScope,
                typeof(long),
                new[] { typeof(int) });

            ILGenerator ilGen = getter.GetILGenerator();
            ilGen.Emit(OpCodes.Ret);

            property.SetGetMethod(getter);

            TypeInfo info = typeBuild.CreateTypeInfo();
            Type type = info;
            PropertyInfo prop = type.GetProperties()[0];
            Expression instance = Expression.Default(type);
            AssertExtensions.Throws<ArgumentException>("indexer", () => Expression.Property(instance, prop, Expression.Constant(0)));
            AssertExtensions.Throws<ArgumentException>("propertyName", () => Expression.Property(instance, "Item", Expression.Constant(0)));
        }

        [Fact]
        public void IndexedPropertySetterNoParams()
        {
            TypeBuilder typeBuild = GetTestTypeBuilder();

            PropertyBuilder property = typeBuild.DefineProperty(
                "Item", PropertyAttributes.None, typeof(int), new[] { typeof(int) });

            MethodBuilder setter = typeBuild.DefineMethod(
                "set_Item",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig
                | MethodAttributes.PrivateScope,
                typeof(void),
                Type.EmptyTypes);

            ILGenerator ilGen = setter.GetILGenerator();
            ilGen.Emit(OpCodes.Ret);

            property.SetSetMethod(setter);

            TypeInfo info = typeBuild.CreateTypeInfo();
            Type type = info;
            PropertyInfo prop = type.GetProperties()[0];
            Expression instance = Expression.Default(type);
            AssertExtensions.Throws<ArgumentException>("indexer", () => Expression.Property(instance, prop, Expression.Constant(0)));
            AssertExtensions.Throws<ArgumentException>("propertyName", () => Expression.Property(instance, "Item", Expression.Constant(0)));
        }

        [Fact]
        public void IndexedPropertySetterByrefValueType()
        {
            TypeBuilder typeBuild = GetTestTypeBuilder();

            PropertyBuilder property = typeBuild.DefineProperty(
                "Item", PropertyAttributes.None, typeof(int), new[] { typeof(int) });

            MethodBuilder setter = typeBuild.DefineMethod(
                "set_Item",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig
                | MethodAttributes.PrivateScope,
                typeof(void),
                new [] {typeof(int), typeof(int).MakeByRefType()});

            ILGenerator ilGen = setter.GetILGenerator();
            ilGen.Emit(OpCodes.Ret);

            property.SetSetMethod(setter);

            TypeInfo info = typeBuild.CreateTypeInfo();
            Type type = info;
            PropertyInfo prop = type.GetProperties()[0];
            Expression instance = Expression.Default(type);
            AssertExtensions.Throws<ArgumentException>("indexer", () => Expression.Property(instance, prop, Expression.Constant(0)));
            AssertExtensions.Throws<ArgumentException>("propertyName", () => Expression.Property(instance, "Item", Expression.Constant(0)));
        }

        [Fact]
        public void IndexedPropertySetterNotReturnVoid()
        {
            TypeBuilder typeBuild = GetTestTypeBuilder();

            PropertyBuilder property = typeBuild.DefineProperty(
                "Item", PropertyAttributes.None, typeof(int), new[] { typeof(int) });

            MethodBuilder setter = typeBuild.DefineMethod(
                "set_Item",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig
                | MethodAttributes.PrivateScope,
                typeof(int),
                new[] { typeof(int), typeof(int) });

            ILGenerator ilGen = setter.GetILGenerator();
            ilGen.Emit(OpCodes.Ret);

            property.SetSetMethod(setter);

            TypeInfo info = typeBuild.CreateTypeInfo();
            Type type = info;
            PropertyInfo prop = type.GetProperties()[0];
            Expression instance = Expression.Default(type);
            AssertExtensions.Throws<ArgumentException>("indexer", () => Expression.Property(instance, prop, Expression.Constant(0)));
            AssertExtensions.Throws<ArgumentException>("propertyName", () => Expression.Property(instance, "Item", Expression.Constant(0)));
        }

        [Fact]
        public void IndexedPropertyGetterInstanceSetterStatic()
        {
            TypeBuilder typeBuild = GetTestTypeBuilder();

            PropertyBuilder property = typeBuild.DefineProperty(
                "Item", PropertyAttributes.None, typeof(int), new[] { typeof(int) });

            MethodBuilder getter = typeBuild.DefineMethod(
                "get_Item",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig
                | MethodAttributes.PrivateScope,
                typeof(int),
                new[] { typeof(int) });

            MethodBuilder setter = typeBuild.DefineMethod(
                "set_Item",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Static
                | MethodAttributes.PrivateScope,
                typeof(void),
                new[] { typeof(int), typeof(int) });

            ILGenerator ilGen = getter.GetILGenerator();
            ilGen.Emit(OpCodes.Ldc_I4_0);
            ilGen.Emit(OpCodes.Ret);

            ilGen = setter.GetILGenerator();
            ilGen.Emit(OpCodes.Ret);

            property.SetGetMethod(getter);
            property.SetSetMethod(setter);

            TypeInfo info = typeBuild.CreateTypeInfo();
            Type type = info;
            PropertyInfo prop = type.GetProperties()[0];
            Expression instance = Expression.Default(type);
            AssertExtensions.Throws<ArgumentException>("indexer", () => Expression.Property(instance, prop, Expression.Constant(0)));
            AssertExtensions.Throws<ArgumentException>("propertyName", () => Expression.Property(instance, "Item", Expression.Constant(0)));
        }

        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/14927", TestRuntimes.Mono)]
        public void IndexedPropertySetterValueTypeNotMatchPropertyType()
        {
            TypeBuilder typeBuild = GetTestTypeBuilder();

            PropertyBuilder property = typeBuild.DefineProperty(
                "Item", PropertyAttributes.None, typeof(int), new[] { typeof(int) });

            MethodBuilder setter = typeBuild.DefineMethod(
                "set_Item",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig
                | MethodAttributes.PrivateScope,
                typeof(void),
                new[] { typeof(int), typeof(long) });

            ILGenerator ilGen = setter.GetILGenerator();
            ilGen.Emit(OpCodes.Ret);

            property.SetSetMethod(setter);

            TypeInfo info = typeBuild.CreateTypeInfo();
            Type type = info;
            PropertyInfo prop = type.GetProperties()[0];
            Expression instance = Expression.Default(type);
            AssertExtensions.Throws<ArgumentException>("indexer", () => Expression.Property(instance, prop, Expression.Constant(0)));
            AssertExtensions.Throws<ArgumentException>("propertyName", () => Expression.Property(instance, "Item", Expression.Constant(0)));
        }

        [Fact]
        public void IndexedPropertyGetterSetterArgCountMismatch()
        {
            TypeBuilder typeBuild = GetTestTypeBuilder();

            PropertyBuilder property = typeBuild.DefineProperty(
                "Item", PropertyAttributes.None, typeof(int), new[] { typeof(int) });

            MethodBuilder getter = typeBuild.DefineMethod(
                "get_Item",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig
                | MethodAttributes.PrivateScope,
                typeof(int),
                new[] { typeof(int) });

            MethodBuilder setter = typeBuild.DefineMethod(
                "set_Item",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig
                | MethodAttributes.PrivateScope,
                typeof(void),
                new[] { typeof(int), typeof(int), typeof(int) });

            ILGenerator ilGen = getter.GetILGenerator();
            ilGen.Emit(OpCodes.Ldc_I4_0);
            ilGen.Emit(OpCodes.Ret);

            ilGen = setter.GetILGenerator();
            ilGen.Emit(OpCodes.Ret);

            property.SetGetMethod(getter);
            property.SetSetMethod(setter);

            TypeInfo info = typeBuild.CreateTypeInfo();
            Type type = info;
            PropertyInfo prop = type.GetProperties()[0];
            Expression instance = Expression.Default(type);
            AssertExtensions.Throws<ArgumentException>("indexer", () => Expression.Property(instance, prop, Expression.Constant(0)));
            AssertExtensions.Throws<ArgumentException>("propertyName", () => Expression.Property(instance, "Item", Expression.Constant(0)));
        }

        [Fact]
        public void IndexedPropertyGetterSetterArgumentTypeMismatch()
        {
            TypeBuilder typeBuild = GetTestTypeBuilder();

            PropertyBuilder property = typeBuild.DefineProperty(
                "Item", PropertyAttributes.None, typeof(int), new[] { typeof(int), typeof(int), typeof(int) });

            MethodBuilder getter = typeBuild.DefineMethod(
                "get_Item",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig
                | MethodAttributes.PrivateScope,
                typeof(int),
                new[] { typeof(int), typeof(int), typeof(int) });

            MethodBuilder setter = typeBuild.DefineMethod(
                "set_Item",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig
                | MethodAttributes.PrivateScope,
                typeof(void),
                new[] { typeof(int), typeof(int), typeof(long), typeof(int) });

            ILGenerator ilGen = getter.GetILGenerator();
            ilGen.Emit(OpCodes.Ldc_I4_0);
            ilGen.Emit(OpCodes.Ret);

            ilGen = setter.GetILGenerator();
            ilGen.Emit(OpCodes.Ret);

            property.SetGetMethod(getter);
            property.SetSetMethod(setter);

            TypeInfo info = typeBuild.CreateTypeInfo();
            Type type = info;
            PropertyInfo prop = type.GetProperties()[0];
            Expression instance = Expression.Default(type);
            AssertExtensions.Throws<ArgumentException>("indexer", () => Expression.Property(instance, prop, Expression.Constant(0), Expression.Constant(0), Expression.Constant(0)));
            AssertExtensions.Throws<ArgumentException>("propertyName", () => Expression.Property(instance, "Item", Expression.Constant(0), Expression.Constant(0), Expression.Constant(0)));
        }

        [Fact]
        public void IndexedPropertyVarArgs()
        {
            TypeBuilder typeBuild = GetTestTypeBuilder();

            PropertyBuilder property = typeBuild.DefineProperty(
                "Item", PropertyAttributes.None, typeof(int), new[] { typeof(int) });

            MethodBuilder getter = typeBuild.DefineMethod(
                "get_Item",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig
                | MethodAttributes.PrivateScope,
                CallingConventions.VarArgs,
                typeof(int),
                Type.EmptyTypes);

            ILGenerator ilGen = getter.GetILGenerator();
            ilGen.Emit(OpCodes.Ldc_I4_0);
            ilGen.Emit(OpCodes.Ret);

            property.SetGetMethod(getter);

            TypeInfo info = typeBuild.CreateTypeInfo();
            Type type = info;
            PropertyInfo prop = type.GetProperties()[0];
            Expression instance = Expression.Default(type);
            AssertExtensions.Throws<ArgumentException>("indexer", () => Expression.Property(instance, prop, Expression.Constant(0), Expression.Constant(0), Expression.Constant(0)));
            AssertExtensions.Throws<ArgumentException>("propertyName", () => Expression.Property(instance, "Item", Expression.Constant(0), Expression.Constant(0), Expression.Constant(0)));
        }

        [Fact]
        public void NullInstanceInstanceProperty()
        {
            PropertyInfo prop = typeof(Dictionary<int, int>).GetProperty("Item");
            ConstantExpression index = Expression.Constant(0);
            AssertExtensions.Throws<ArgumentException>("instance", () => Expression.Property(null, prop, index));
        }

        [Fact]
        public void InstanceToStaticProperty()
        {
            TypeBuilder typeBuild = GetTestTypeBuilder();

            PropertyBuilder property = typeBuild.DefineProperty(
                "Item", PropertyAttributes.None, typeof(int), new[] { typeof(int) });

            MethodBuilder getter = typeBuild.DefineMethod(
                "get_Item",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Static
                | MethodAttributes.PrivateScope,
                typeof(int),
                new[] { typeof(int) });

            ILGenerator ilGen = getter.GetILGenerator();
            ilGen.Emit(OpCodes.Ldc_I4_0);
            ilGen.Emit(OpCodes.Ret);

            property.SetGetMethod(getter);

            TypeInfo info = typeBuild.CreateTypeInfo();
            Type type = info;
            PropertyInfo prop = type.GetProperties()[0];
            Expression instance = Expression.Default(type);
            AssertExtensions.Throws<ArgumentException>("instance", () => Expression.Property(instance, prop, Expression.Constant(0)));
        }

        [Fact]
        public void ByRefIndexer()
        {
            TypeBuilder typeBuild = GetTestTypeBuilder();

            PropertyBuilder property = typeBuild.DefineProperty(
                "Item", PropertyAttributes.None, typeof(int), new[] { typeof(int).MakeByRefType() });

            MethodBuilder getter = typeBuild.DefineMethod(
                "get_Item",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig
                | MethodAttributes.PrivateScope,
                typeof(int),
                new[] { typeof(int).MakeByRefType() });

            ILGenerator ilGen = getter.GetILGenerator();
            ilGen.Emit(OpCodes.Ldc_I4_0);
            ilGen.Emit(OpCodes.Ret);

            property.SetGetMethod(getter);

            TypeInfo info = typeBuild.CreateTypeInfo();
            Type type = info;
            PropertyInfo prop = type.GetProperties()[0];
            Expression instance = Expression.Default(type);
            AssertExtensions.Throws<ArgumentException>("indexes[0]", () => Expression.Property(instance, prop, Expression.Constant(0)));
        }

// FEATURE_COMPILE
#endif

        [Fact]
        public void CallWithoutIndices()
        {
            PropertyInfo prop = typeof(Dictionary<int, int>).GetProperty("Item");
            DefaultExpression dict = Expression.Default(typeof(Dictionary<int, int>));
            AssertExtensions.Throws<ArgumentException>("indexer", () => Expression.Property(dict, prop, Array.Empty<Expression>()));
        }

        [Fact]
        public void CallWithExcessiveIndices()
        {
            PropertyInfo prop = typeof(Dictionary<int, int>).GetProperty("Item");
            DefaultExpression dict = Expression.Default(typeof(Dictionary<int, int>));
            ConstantExpression index = Expression.Constant(0);
            AssertExtensions.Throws<ArgumentException>("indexer", () => Expression.Property(dict, prop, index, index));
        }

        [Fact]
        public void CallWithUnassignableIndex()
        {
            PropertyInfo prop = typeof(Dictionary<int, int>).GetProperty("Item");
            DefaultExpression dict = Expression.Default(typeof(Dictionary<int, int>));
            ConstantExpression index = Expression.Constant(0L);
            AssertExtensions.Throws<ArgumentException>("arguments[0]", () => Expression.Property(dict, prop, index));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void CallWithLambdaIndex(bool useInterpreter)
        {
            // An exception to the rule against unassignable indices, lamdba expressions
            // can be automatically quoted.
            PropertyInfo prop = typeof(Dictionary<Expression<Func<int>>, int>).GetProperty("Item");
            Expression<Func<int>> index = () => 2;
            ConstantExpression dict = Expression.Constant(new Dictionary<Expression<Func<int>>, int>{{index, 9}});
            Func<int> f = Expression.Lambda<Func<int>>(Expression.Property(dict, prop, index)).Compile(useInterpreter);
            Assert.Equal(9, f());
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void CallWithIntAndLambdaIndex(bool useInterpreter)
        {
            PropertyInfo prop = typeof(IntAndExpressionIndexed).GetProperty("Item");
            ConstantExpression instance = Expression.Constant(new IntAndExpressionIndexed());
            Expression<Action> index = Expression.Lambda<Action>(Expression.Empty());
            ConstantExpression intIdx = Expression.Constant(0);
            Func<bool> f = Expression.Lambda<Func<bool>>(Expression.Property(instance, prop, intIdx, index)).Compile(useInterpreter);
            Assert.True(f());
        }

        [Fact]
        public void TryIndexedAccessNonIndexedProperty()
        {
            ConstantExpression instance = Expression.Constant("");
            PropertyInfo prop = typeof(string).GetProperty(nameof(string.Length));
            ConstantExpression index = Expression.Constant(0);
            AssertExtensions.Throws<ArgumentException>("indexer", () => Expression.Property(instance, prop, index));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void OverloadedIndexer(bool useInterpreter)
        {
            ConstantExpression instance = Expression.Constant(new OverloadedIndexers());
            ConstantExpression index = Expression.Constant("");
            Expression<Func<int>> exp = Expression.Lambda<Func<int>>(Expression.Property(instance, "Item", index));
            Func<int> f = exp.Compile(useInterpreter);
            Assert.Equal(2, f());
        }

        [Fact]
        public void OverloadedIndexerBothMatch()
        {
            ConstantExpression instance = Expression.Constant(new OverloadedIndexersBothMatchString());
            ConstantExpression index = Expression.Constant("");
            Assert.Throws<InvalidOperationException>(() => Expression.Property(instance, "Item", index));
        }

        [Fact]
        public void NoSuchPropertyExplicitlyNoIndices()
        {
            ConstantExpression instance = Expression.Constant("");
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Property(instance, "ThisDoesNotExist", Array.Empty<Expression>()));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Property(instance, "ThisDoesNotExist", null));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void NonIndexedPropertyExplicitlyNoIndices(bool useInterpreter)
        {
            ConstantExpression instance = Expression.Constant("123");
            IndexExpression prop = Expression.Property(instance, "Length", null);
            Expression<Func<int>> exp = Expression.Lambda<Func<int>>(prop);
            Func<int> func = exp.Compile(useInterpreter);
            Assert.Equal(3, func());
        }

        [Fact]
        public void FindNothingForNullArgument()
        {
            ConstantExpression instance = Expression.Constant("123");
            AssertExtensions.Throws<ArgumentException>("propertyName", () => Expression.Property(instance, "Length", new Expression[] {null}));
        }

        [Fact]
        public void NullArgument()
        {
            ConstantExpression instance = Expression.Constant(new Dictionary<int, int>());
            PropertyInfo prop = typeof(Dictionary<int, int>).GetProperty("Item");
            AssertExtensions.Throws<ArgumentException>("propertyName", () => Expression.Property(instance, "Item", new Expression[] {null}));
            AssertExtensions.Throws<ArgumentNullException>("arguments[0]", () => Expression.Property(instance, prop, new Expression[] {null}));
        }

        [Fact]
        public void UnreadableIndex()
        {
            ConstantExpression instance = Expression.Constant(new Dictionary<int, int>());
            PropertyInfo prop = typeof(Dictionary<int, int>).GetProperty("Item");
            MemberExpression index = Expression.Property(null, typeof(Unreadable<int>).GetProperty(nameof(Unreadable<int>.WriteOnly)));
            AssertExtensions.Throws<ArgumentException>("arguments[0]", () => Expression.Property(instance, "Item", index));
            AssertExtensions.Throws<ArgumentException>("arguments[0]", () => Expression.Property(instance, prop, index));
        }


        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConstrainedVirtualCall(bool useInterpreter)
        {
            // Virtual call via base declaration to valuetype.
            ConstantExpression instance = Expression.Constant(new InterfaceIndexableValueType());
            PropertyInfo prop = typeof(IIndexable).GetProperty("Item");
            IndexExpression index = Expression.Property(instance, prop, Expression.Constant(4));
            Expression<Func<int>> lambda = Expression.Lambda<Func<int>>(
                index
            );
            Func<int> func = lambda.Compile(useInterpreter);
            Assert.Equal(8, func());
        }


        private interface IIndexable
        {
            int this[int index] { get; }
        }

        private struct InterfaceIndexableValueType : IIndexable
        {
            public int this[int index] => index * 2;
        }

        private class IntAndExpressionIndexed
        {
            public bool this[int x, Expression<Action> y] => true;
        }

        private class OverloadedIndexers
        {
            public int this[int index] => 0;

            public int this[int x, int y] => 1;

            public int this[string index] => 2;
        }

        private class OverloadedIndexersBothMatchString
        {
            public int this[IComparable index] => 0;

            public int this[ICloneable index] => 1;
        }

        class Vector1
        {
            public int this[int x]
            {
                get { return 0; }
            }
        }

        class Vector2
        {
            public int this[int x, int y]
            {
                get { return 0; }
            }
        }
    }
}
