// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class Expression_Tests
    {
        [Fact]
        public static void NewMakeBinary()
        {
            int? i = 10;
            double? j = 23.89;
            ConstantExpression left = Expression.Constant(i, typeof(Nullable<int>));
            ConstantExpression right = Expression.Constant(j, typeof(Nullable<double>));
            Expression<Func<int, double?>> conversion = (int k) => (double?)i;
            BinaryExpression v = Expression.MakeBinary(ExpressionType.Coalesce, left, right, false, null, conversion);
            Assert.NotNull(v);
        }

#pragma warning disable xUnit1013 // needs to be public for reflection-based test
        public void Add(ref int i)
#pragma warning restore xUnit1013
        {
        }

        [Fact]
        public static void ElementInit()
        {
            MethodInfo mi1 = typeof(Expression_Tests).GetMethod("Add");
            ConstantExpression ce1 = Expression.Constant(4, typeof(int));

            AssertExtensions.Throws<ArgumentException>("addMethod", () => Expression.ElementInit(mi1, new Expression[] { ce1 }));
        }

        public class Atom
        {
            public static implicit operator bool (Atom atom) { return true; }
            public Atom this[Atom b0] { get { return null; } }
            public Atom this[Atom b0, Atom b1] { get { return null; } }
        }

        [Fact]
        public static void EqualityBetweenReferenceAndInterfacesSucceeds()
        {
            // able to build reference comparison between interfaces
            Expression.Equal(Expression.Constant(null, typeof(IEnumerable)), Expression.Constant(null, typeof(IComparer)));
            Expression.NotEqual(Expression.Constant(null, typeof(IEnumerable)), Expression.Constant(null, typeof(IComparer)));

            // able to build reference comparison between reference type and interfaces
            Expression.Equal(Expression.Constant(null, typeof(BaseClass)), Expression.Constant(null, typeof(IEnumerable)));
            Expression.Equal(Expression.Constant(null, typeof(IEnumerable)), Expression.Constant(null, typeof(BaseClass)));
            Expression.NotEqual(Expression.Constant(null, typeof(BaseClass)), Expression.Constant(null, typeof(IEnumerable)));
            Expression.NotEqual(Expression.Constant(null, typeof(IEnumerable)), Expression.Constant(null, typeof(BaseClass)));
        }

        [Fact]
        public static void EqualityBetweenStructAndIterfaceFails()
        {
            Expression expStruct = Expression.Constant(5);
            Expression expIface = Expression.Constant(null, typeof(IComparable));
            Assert.Throws<InvalidOperationException>(() => Expression.Equal(expStruct, expIface));
        }

        [Fact]
        public static void EqualityBetweenInheritedTypesSucceeds()
        {
            Expression.Equal(Expression.Constant(null, typeof(BaseClass)), Expression.Constant(null, typeof(DerivedClass)));
            Expression.Equal(Expression.Constant(null, typeof(DerivedClass)), Expression.Constant(null, typeof(BaseClass)));
            Expression.NotEqual(Expression.Constant(null, typeof(BaseClass)), Expression.Constant(null, typeof(DerivedClass)));
            Expression.NotEqual(Expression.Constant(null, typeof(DerivedClass)), Expression.Constant(null, typeof(BaseClass)));
        }

        [Fact]
        public static void Regress_ThisPropertyCanBeOverloaded()
        {
            Expression<Predicate<Atom>> d = atom => atom && atom[atom];
        }

        [Fact]
        public static void Arrays()
        {
            Expression<Func<int, int[]>> exp1 = i => new int[i];
            NewArrayExpression aex1 = exp1.Body as NewArrayExpression;
            Assert.NotNull(aex1);
            Assert.Equal(ExpressionType.NewArrayBounds, aex1.NodeType);

            Expression<Func<int[], int>> exp2 = (i) => i.Length;
            UnaryExpression uex2 = exp2.Body as UnaryExpression;
            Assert.NotNull(uex2);
            Assert.Equal(ExpressionType.ArrayLength, uex2.NodeType);
        }

        private void Method3<T, U, V>()
        {
        }

        private void Method4<T, U, V, W>()
        {
        }

        private void Method()
        {
        }

        [Fact]
        public static void CheckedExpressions()
        {
            Expression<Func<int, int, int>> exp = (a, b) => unchecked(a + b);
            BinaryExpression bex = exp.Body as BinaryExpression;
            Assert.NotNull(bex);
            Assert.Equal(ExpressionType.Add, bex.NodeType);

            exp = (a, b) => checked(a + b);
            bex = exp.Body as BinaryExpression;
            Assert.NotNull(bex);
            Assert.Equal(ExpressionType.AddChecked, bex.NodeType);

            exp = (a, b) => unchecked(a * b);
            bex = exp.Body as BinaryExpression;
            Assert.NotNull(bex);
            Assert.Equal(ExpressionType.Multiply, bex.NodeType);

            exp = (a, b) => checked(a * b);
            bex = exp.Body as BinaryExpression;
            Assert.NotNull(bex);
            Assert.Equal(ExpressionType.MultiplyChecked, bex.NodeType);

            Expression<Func<double, int>> exp2 = (a) => unchecked((int)a);
            UnaryExpression uex = exp2.Body as UnaryExpression;
            Assert.NotNull(uex);
            Assert.Equal(ExpressionType.Convert, uex.NodeType);

            exp2 = (a) => checked((int)a);
            uex = exp2.Body as UnaryExpression;
            Assert.NotNull(uex);
            Assert.Equal(ExpressionType.ConvertChecked, uex.NodeType);
        }

        protected virtual int Foo(int x)
        {
            return x;
        }

        [Fact]
        public static void VirtualCallExpressions()
        {
            Expression_Tests obj = new Expression_Tests();
            Expression<Func<int, int>> exp = x => obj.Foo(x);
            MethodCallExpression mc = exp.Body as MethodCallExpression;
            Assert.NotNull(mc);
            Assert.Equal(ExpressionType.Call, mc.NodeType);
        }


        [Fact]
        public static void ConstantNullWithValueTypeIsInvalid()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Constant(null, typeof(int)));
        }

        [Fact]
        public static void ConstantNullWithNullableValueType()
        {
            Expression.Constant(null, typeof(int?));
        }

        [Fact]
        public static void ConstantNullWithReferenceType()
        {
            Expression.Constant(null, typeof(Expression_Tests));
        }

        [Fact]
        public static void ConstantIntWithInterface()
        {
            Expression.Constant(10, typeof(IComparable));
        }

        [Fact]
        public static void ConstantIntWithNullableInt()
        {
            Expression.Constant(10, typeof(int?));
        }

        [Fact]
        public static void TestUserDefinedOperators()
        {
            TestUserDefinedMathOperators<U, U>();
            TestUserDefinedComparisonOperators<U, U>();
            TestUserDefinedBitwiseOperators<U, U>();
            TestUserDefinedMathOperators<U?, U?>();
            TestUserDefinedComparisonOperators<U?, U?>();
            TestUserDefinedBitwiseOperators<U?, U?>();

            TestUserDefinedComparisonOperators<B, B>();
            TestUserDefinedLogicalOperators<B, B>();
            TestUserDefinedComparisonOperators<B?, B?>();
            TestUserDefinedLogicalOperators<B?, B?>();

            TestUserDefinedMathOperators<M, N>();
            TestUserDefinedMathOperators<M?, N?>();
        }

        internal static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
        internal static Type GetNonNullableType(Type type)
        {
            if (IsNullableType(type))
                type = type.GetGenericArguments()[0];
            return type;
        }

        private static void TestUserDefinedMathOperators<X, Y>()
        {
            ParameterExpression x = Expression.Parameter(typeof(X), "x");
            ParameterExpression y = Expression.Parameter(typeof(Y), "y");
            Type nnX = GetNonNullableType(typeof(X));
            Type nnY = GetNonNullableType(typeof(Y));
            AssertIsOp(Expression.Add(x, y), "op_Addition");
            AssertIsOp(Expression.Add(x, y, null), "op_Addition");
            AssertIsOp(Expression.Add(x, y, nnX.GetMethod("op_Subtraction")), "op_Subtraction");
            AssertIsOp(Expression.AddChecked(x, y), "op_Addition");
            AssertIsOp(Expression.AddChecked(x, y, null), "op_Addition");
            AssertIsOp(Expression.AddChecked(x, y, nnX.GetMethod("op_Subtraction")), "op_Subtraction");
            AssertIsOp(Expression.Subtract(x, y), "op_Subtraction");
            AssertIsOp(Expression.Subtract(x, y, null), "op_Subtraction");
            AssertIsOp(Expression.Subtract(x, y, nnX.GetMethod("op_Addition")), "op_Addition");
            AssertIsOp(Expression.SubtractChecked(x, y), "op_Subtraction");
            AssertIsOp(Expression.SubtractChecked(x, y, null), "op_Subtraction");
            AssertIsOp(Expression.SubtractChecked(x, y, nnX.GetMethod("op_Addition")), "op_Addition");
            AssertIsOp(Expression.Multiply(x, y), "op_Multiply");
            AssertIsOp(Expression.Multiply(x, y, null), "op_Multiply");
            AssertIsOp(Expression.Multiply(x, y, nnY.GetMethod("op_Division")), "op_Division");
            AssertIsOp(Expression.MultiplyChecked(x, y), "op_Multiply");
            AssertIsOp(Expression.MultiplyChecked(x, y, null), "op_Multiply");
            AssertIsOp(Expression.MultiplyChecked(x, y, nnY.GetMethod("op_Division")), "op_Division");
            AssertIsOp(Expression.Divide(x, y), "op_Division");
            AssertIsOp(Expression.Divide(x, y, null), "op_Division");
            AssertIsOp(Expression.Divide(x, y, nnY.GetMethod("op_Multiply")), "op_Multiply");
            AssertIsOp(Expression.Negate(x), "op_UnaryNegation");
            AssertIsOp(Expression.Negate(x, null), "op_UnaryNegation");
            AssertIsOp(Expression.Negate(x, nnX.GetMethod("op_OnesComplement")), "op_OnesComplement");
            AssertIsOp(Expression.NegateChecked(x), "op_UnaryNegation");
            AssertIsOp(Expression.NegateChecked(x, null), "op_UnaryNegation");
            AssertIsOp(Expression.NegateChecked(x, nnX.GetMethod("op_OnesComplement")), "op_OnesComplement");
        }

        private static void TestUserDefinedComparisonOperators<X, Y>()
        {
            ParameterExpression x = Expression.Parameter(typeof(X), "x");
            ParameterExpression y = Expression.Parameter(typeof(Y), "y");
            Type nnX = GetNonNullableType(typeof(X));
            AssertIsOp(Expression.LessThan(x, y), "op_LessThan");
            AssertIsOp(Expression.LessThan(x, y, false, null), "op_LessThan");
            AssertIsOp(Expression.LessThan(x, y, true, null), "op_LessThan");
            AssertIsOp(Expression.LessThan(x, y, false, nnX.GetMethod("op_GreaterThan")), "op_GreaterThan");
            AssertIsOp(Expression.LessThan(x, y, true, nnX.GetMethod("op_GreaterThan")), "op_GreaterThan");
            AssertIsOp(Expression.LessThanOrEqual(x, y), "op_LessThanOrEqual");
            AssertIsOp(Expression.LessThanOrEqual(x, y, false, null), "op_LessThanOrEqual");
            AssertIsOp(Expression.LessThanOrEqual(x, y, true, null), "op_LessThanOrEqual");
            AssertIsOp(Expression.LessThanOrEqual(x, y, false, nnX.GetMethod("op_GreaterThan")), "op_GreaterThan");
            AssertIsOp(Expression.LessThanOrEqual(x, y, true, nnX.GetMethod("op_GreaterThan")), "op_GreaterThan");
            AssertIsOp(Expression.GreaterThan(x, y), "op_GreaterThan");
            AssertIsOp(Expression.GreaterThan(x, y, false, null), "op_GreaterThan");
            AssertIsOp(Expression.GreaterThan(x, y, true, null), "op_GreaterThan");
            AssertIsOp(Expression.GreaterThan(x, y, false, nnX.GetMethod("op_LessThan")), "op_LessThan");
            AssertIsOp(Expression.GreaterThan(x, y, true, nnX.GetMethod("op_LessThan")), "op_LessThan");
            AssertIsOp(Expression.GreaterThanOrEqual(x, y), "op_GreaterThanOrEqual");
            AssertIsOp(Expression.GreaterThanOrEqual(x, y, false, null), "op_GreaterThanOrEqual");
            AssertIsOp(Expression.GreaterThanOrEqual(x, y, true, null), "op_GreaterThanOrEqual");
            AssertIsOp(Expression.GreaterThanOrEqual(x, y, false, nnX.GetMethod("op_LessThan")), "op_LessThan");
            AssertIsOp(Expression.GreaterThanOrEqual(x, y, true, nnX.GetMethod("op_LessThan")), "op_LessThan");
            AssertIsOp(Expression.Equal(x, y), "op_Equality");
            AssertIsOp(Expression.Equal(x, y, false, null), "op_Equality");
            AssertIsOp(Expression.Equal(x, y, true, null), "op_Equality");
            AssertIsOp(Expression.Equal(x, y, false, nnX.GetMethod("op_Inequality")), "op_Inequality");
            AssertIsOp(Expression.Equal(x, y, true, nnX.GetMethod("op_Inequality")), "op_Inequality");
            AssertIsOp(Expression.NotEqual(x, y), "op_Inequality");
            AssertIsOp(Expression.NotEqual(x, y, false, null), "op_Inequality");
            AssertIsOp(Expression.NotEqual(x, y, true, null), "op_Inequality");
            AssertIsOp(Expression.NotEqual(x, y, false, nnX.GetMethod("op_Equality")), "op_Equality");
            AssertIsOp(Expression.NotEqual(x, y, true, nnX.GetMethod("op_Equality")), "op_Equality");
        }

        private static void TestUserDefinedBitwiseOperators<X, Y>()
        {
            ParameterExpression x = Expression.Parameter(typeof(X), "x");
            ParameterExpression y = Expression.Parameter(typeof(Y), "y");
            Type nnX = GetNonNullableType(typeof(X));
            AssertIsOp(Expression.And(x, y), "op_BitwiseAnd");
            AssertIsOp(Expression.And(x, y, null), "op_BitwiseAnd");
            AssertIsOp(Expression.And(x, y, nnX.GetMethod("op_BitwiseOr")), "op_BitwiseOr");
            AssertIsOp(Expression.Or(x, y), "op_BitwiseOr");
            AssertIsOp(Expression.Or(x, y, null), "op_BitwiseOr");
            AssertIsOp(Expression.Or(x, y, nnX.GetMethod("op_BitwiseAnd")), "op_BitwiseAnd");
            AssertIsOp(Expression.ExclusiveOr(x, y), "op_ExclusiveOr");
            AssertIsOp(Expression.ExclusiveOr(x, y, null), "op_ExclusiveOr");
            AssertIsOp(Expression.ExclusiveOr(x, y, nnX.GetMethod("op_BitwiseAnd")), "op_BitwiseAnd");
            AssertIsOp(Expression.Not(x), "op_OnesComplement");
            AssertIsOp(Expression.Not(x, null), "op_OnesComplement");
            AssertIsOp(Expression.Not(x, nnX.GetMethod("op_UnaryNegation")), "op_UnaryNegation");
        }

        private static void TestUserDefinedLogicalOperators<X, Y>()
        {
            ParameterExpression x = Expression.Parameter(typeof(X), "x");
            ParameterExpression y = Expression.Parameter(typeof(Y), "y");
            Type nnX = GetNonNullableType(typeof(X));
            AssertIsOp(Expression.And(x, y), "op_BitwiseAnd");
            AssertIsOp(Expression.And(x, y, null), "op_BitwiseAnd");
            AssertIsOp(Expression.And(x, y, nnX.GetMethod("op_BitwiseOr")), "op_BitwiseOr");
            AssertIsOp(Expression.Or(x, y), "op_BitwiseOr");
            AssertIsOp(Expression.Or(x, y, null), "op_BitwiseOr");
            AssertIsOp(Expression.Or(x, y, nnX.GetMethod("op_BitwiseAnd")), "op_BitwiseAnd");
            AssertIsOp(Expression.ExclusiveOr(x, y), "op_ExclusiveOr");
            AssertIsOp(Expression.ExclusiveOr(x, y, null), "op_ExclusiveOr");
            AssertIsOp(Expression.ExclusiveOr(x, y, nnX.GetMethod("op_BitwiseAnd")), "op_BitwiseAnd");
            AssertIsOp(Expression.Not(x), "op_LogicalNot");
            AssertIsOp(Expression.Not(x, null), "op_LogicalNot");
            AssertIsOp(Expression.Not(x, nnX.GetMethod("op_UnaryNegation")), "op_UnaryNegation");
        }

        private static void AssertIsOp(BinaryExpression b, string opName)
        {
            Assert.NotNull(b.Method);
            Assert.Equal(opName, b.Method.Name);
        }

        private static void AssertIsOp(UnaryExpression u, string opName)
        {
            Assert.NotNull(u.Method);
            Assert.Equal(opName, u.Method.Name);
        }


        [Fact]
        public static void TestUserDefinedCoercions()
        {
            TestUserDefinedCoercion<M, N>();
            TestUserDefinedCoercion<M, N?>();
            TestUserDefinedCoercion<M?, N>();
            TestUserDefinedCoercion<M?, N?>();
        }

        private static void TestUserDefinedCoercion<X, Y>()
        {
            ParameterExpression x = Expression.Parameter(typeof(X), "x");
            ParameterExpression y = Expression.Parameter(typeof(Y), "y");
            Type nnX = GetNonNullableType(typeof(X));
            Type nnY = GetNonNullableType(typeof(Y));

            AssertIsCoercion(Expression.Convert(x, typeof(Y)), "op_Implicit", typeof(Y));
            AssertIsCoercion(Expression.Convert(x, typeof(Y), null), "op_Implicit", typeof(Y));
            AssertIsCoercion(Expression.Convert(x, typeof(Y), nnX.GetMethod("Foo")), "Foo", typeof(Y));
            AssertIsCoercion(Expression.Convert(y, typeof(X)), "op_Explicit", typeof(X));
            AssertIsCoercion(Expression.Convert(y, typeof(X), null), "op_Explicit", typeof(X));
            AssertIsCoercion(Expression.Convert(y, typeof(X), nnY.GetMethod("Bar")), "Bar", typeof(X));
            AssertIsCoercion(Expression.ConvertChecked(x, typeof(Y)), "op_Implicit", typeof(Y));
            AssertIsCoercion(Expression.ConvertChecked(x, typeof(Y), null), "op_Implicit", typeof(Y));
            AssertIsCoercion(Expression.ConvertChecked(x, typeof(Y), nnX.GetMethod("Foo")), "Foo", typeof(Y));
            AssertIsCoercion(Expression.ConvertChecked(y, typeof(X)), "op_Explicit", typeof(X));
            AssertIsCoercion(Expression.ConvertChecked(y, typeof(X), null), "op_Explicit", typeof(X));
            AssertIsCoercion(Expression.ConvertChecked(y, typeof(X), nnY.GetMethod("Bar")), "Bar", typeof(X));
            AssertIsCoercion(Expression.Convert(x, typeof(Y)), "op_Implicit", typeof(Y));
            AssertIsCoercion(Expression.Convert(x, typeof(Y), null), "op_Implicit", typeof(Y));
            AssertIsCoercion(Expression.Convert(x, typeof(Y), nnX.GetMethod("Foo")), "Foo", typeof(Y));
            AssertIsCoercion(Expression.Convert(y, typeof(X)), "op_Explicit", typeof(X));
            AssertIsCoercion(Expression.Convert(y, typeof(X), null), "op_Explicit", typeof(X));
            AssertIsCoercion(Expression.Convert(y, typeof(X), nnY.GetMethod("Bar")), "Bar", typeof(X));
        }

        private static void AssertIsCoercion(UnaryExpression u, string opName, Type expected)
        {
            Debug.WriteLine("Convert: {0} -> {1}", u.Operand.Type, u.Type);
            Assert.NotNull(u.Method);
            Assert.Equal(opName, u.Method.Name);
            Assert.Equal(expected, u.Type);
        }

        [Fact]
        public static void TestGetFuncType()
        {
            // 1 type arg Func
            Type type = Expression.GetFuncType(new Type[] { typeof(int) });
            Assert.True(type.IsGenericType);
            Assert.Equal(1, type.GetGenericArguments().Length);
            Assert.Equal(typeof(int), type.GetGenericArguments()[0]);

            // 2 type arg Func
            type = Expression.GetFuncType(new Type[] { typeof(int), typeof(string) });
            Assert.True(type.IsGenericType);
            Assert.Equal(typeof(Func<,>), type.GetGenericTypeDefinition());
            Assert.Equal(typeof(int), type.GetGenericArguments()[0]);
            Assert.Equal(typeof(string), type.GetGenericArguments()[1]);

            // 3 type arg Func
            type = Expression.GetFuncType(new Type[] { typeof(string), typeof(int), typeof(decimal) });
            Assert.True(type.IsGenericType);
            Assert.Equal(typeof(Func<,,>), type.GetGenericTypeDefinition());
            Assert.Equal(typeof(string), type.GetGenericArguments()[0]);
            Assert.Equal(typeof(int), type.GetGenericArguments()[1]);
            Assert.Equal(typeof(decimal), type.GetGenericArguments()[2]);

            // 4 type arg Func
            type = Expression.GetFuncType(new Type[] { typeof(string), typeof(int), typeof(decimal), typeof(float) });
            Assert.True(type.IsGenericType);
            Assert.Equal(typeof(Func<,,,>), type.GetGenericTypeDefinition());
            Assert.Equal(typeof(string), type.GetGenericArguments()[0]);
            Assert.Equal(typeof(int), type.GetGenericArguments()[1]);
            Assert.Equal(typeof(decimal), type.GetGenericArguments()[2]);
            Assert.Equal(typeof(float), type.GetGenericArguments()[3]);

            // 5 type arg Func
            type = Expression.GetFuncType(new Type[] { typeof(NWindProxy.Customer), typeof(string), typeof(int), typeof(decimal), typeof(float) });
            Assert.True(type.IsGenericType);
            Assert.Equal(typeof(Func<,,,,>), type.GetGenericTypeDefinition());
            Assert.Equal(typeof(NWindProxy.Customer), type.GetGenericArguments()[0]);
            Assert.Equal(typeof(string), type.GetGenericArguments()[1]);
            Assert.Equal(typeof(int), type.GetGenericArguments()[2]);
            Assert.Equal(typeof(decimal), type.GetGenericArguments()[3]);
            Assert.Equal(typeof(float), type.GetGenericArguments()[4]);
        }

        [Fact]
        public static void TestGetFuncTypeWithNullFails()
        {
            AssertExtensions.Throws<ArgumentNullException>("typeArgs", () => Expression.GetFuncType(null));
        }

        [Fact]
        public static void TestGetFuncTypeWithTooManyArgsFails()
        {
            AssertExtensions.Throws<ArgumentException>("typeArgs", () => Expression.GetFuncType(new Type[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int) }));
        }

        [Fact]
        public static void TestPropertiesAndFieldsByName()
        {
            Expression p = Expression.Parameter(typeof(NWindProxy.Customer), "c");
            Assert.Equal("ContactName", Expression.PropertyOrField(p, "contactName").Member.Name);
            Assert.Equal("ContactName", Expression.Field(p, "CONTACTNAME").Member.Name);

            Expression t = Expression.Parameter(typeof(Expression_Tests), "t");
            Assert.Equal("IsFunky", Expression.PropertyOrField(t, "IsFunky").Member.Name);
            Assert.Equal("IsFunky", Expression.PropertyOrField(t, "isFunky").Member.Name);
            Assert.Equal("IsFunky", Expression.PropertyOrField(t, "isfunky").Member.Name);
            Assert.True(typeof(PropertyInfo).IsAssignableFrom(Expression.PropertyOrField(t, "isfunky").Member.GetType()));
            Assert.Equal("IsFunky", Expression.Property(t, "IsFunky").Member.Name);
            Assert.Equal("IsFunky", Expression.Property(t, "isFunky").Member.Name);
            Assert.Equal("IsFunky", Expression.Property(t, "ISFUNKY").Member.Name);
            Assert.True(typeof(PropertyInfo).IsAssignableFrom(Expression.Property(t, "isFunky").Member.GetType()));
            Assert.True(typeof(FieldInfo).IsAssignableFrom(Expression.Field(t, "_isFunky").Member.GetType()));
        }

        private bool IsFunky { get { return _isfunky; } }
        private bool _isfunky = true;

        [Fact]
        // tests calling instance methods by name (generic and non-generic)
        public static void TestCallInstanceMethodsByName()
        {
            Expression_Tests obj = new Expression_Tests();
            MethodCallExpression mc1 = Expression.Call(Expression.Constant(obj), "SomeMethod", null, Expression.Constant(5));
            Assert.Equal(typeof(int), mc1.Method.GetParameters()[0].ParameterType);
            MethodCallExpression mc2 = Expression.Call(Expression.Constant(obj), "Somemethod", null, Expression.Constant("Five"));
            Assert.Equal(typeof(string), mc2.Method.GetParameters()[0].ParameterType);
            MethodCallExpression mc3 = Expression.Call(Expression.Constant(obj), "someMethod", new Type[] { typeof(int), typeof(string) }, Expression.Constant(5), Expression.Constant("Five"));
            Assert.Equal(typeof(int), mc3.Method.GetParameters()[0].ParameterType);
            Assert.Equal(typeof(string), mc3.Method.GetParameters()[1].ParameterType);
        }

        private void SomeMethod(int someArg)
        {
        }

        private void SomeMethod(string someArg)
        {
        }

        private void SomeMethod<A, B>(A a, B b)
        {
        }

        [Fact]
        // this tests calling static methods by name (generic and non-generic)
        public static void TestCallStaticMethodsByName()
        {
            NWindProxy.Customer[] custs = new[] { new NWindProxy.Customer { CustomerID = "BUBBA", ContactName = "Bubba Gump" } };
            NWindProxy.Order[] orders = new[] { new NWindProxy.Order { CustomerID = "BUBBA" } };


            Expression query = custs.AsQueryable().Expression;
            Expression query2 = orders.AsQueryable().Expression;

            Expression<Func<NWindProxy.Customer, bool>> pred = c => c.CustomerID == "BUBBA";
            Expression<Func<NWindProxy.Customer, string>> selName = c => c.ContactName;
            Expression<Func<NWindProxy.Customer, string>> cId = c => c.CustomerID;
            Expression<Func<NWindProxy.Order, string>> ocId = o => o.CustomerID;
            Expression<Func<NWindProxy.Customer, IEnumerable<NWindProxy.Order>>> selOrders = c => c.Orders;
            Expression<Func<NWindProxy.Customer, NWindProxy.Customer, NWindProxy.Customer>> agg = (c1, c2) => c1;
            Expression<Func<NWindProxy.Customer, NWindProxy.Order, string>> joinPair = (c, o) => c.ContactName + o.CustomerID;
            Expression<Func<string, IEnumerable<NWindProxy.Customer>, string>> strCusts = (s, cs) => s + cs.Count();
            Expression<Func<string, IEnumerable<string>, string>> strStrs = (s, ss) => s + ss.Count();
            Expression<Func<NWindProxy.Customer, IEnumerable<NWindProxy.Order>, string>> custOrds = (c, os) => c.ContactName + os.Count();
            Expression<Func<string, NWindProxy.Customer, string>> agg2 = (s, c) => c.ContactName + s;

            Expression<Func<string, int>> aggsel = s => s.Length;

            Expression comparer = Expression.Constant(StringComparer.OrdinalIgnoreCase);

            Type[] taCust = new Type[] { typeof(NWindProxy.Customer) };
            Type[] taCustOrder = new Type[] { typeof(NWindProxy.Customer), typeof(NWindProxy.Order) };
            Type[] taCustOrderString = new Type[] { typeof(NWindProxy.Customer), typeof(NWindProxy.Order), typeof(string) };
            Type[] taCustString = new Type[] { typeof(NWindProxy.Customer), typeof(string) };
            Type[] taCustStringString = new Type[] { typeof(NWindProxy.Customer), typeof(string), typeof(string) };
            Type[] taCustStringInt = new Type[] { typeof(NWindProxy.Customer), typeof(string), typeof(int) };
            Type[] taCustOrderStringString = new Type[] { typeof(NWindProxy.Customer), typeof(NWindProxy.Order), typeof(string), typeof(string) };

            // test by calling all known Queryable methods

            CheckMethod("Aggregate", Expression.Call(typeof(Queryable), "Aggregate", taCust, query, agg));
            CheckMethod("Aggregate", Expression.Call(typeof(Queryable), "Aggregate", taCustString, query, Expression.Constant("Bubba"), agg2));
            CheckMethod("Aggregate", Expression.Call(typeof(Queryable), "Aggregate", taCustStringInt, query, Expression.Constant("Bubba"), agg2, aggsel));
            CheckMethod("All", Expression.Call(typeof(Queryable), "All", taCust, query, pred));
            CheckMethod("Any", Expression.Call(typeof(Queryable), "Any", taCust, query));
            CheckMethod("Any", Expression.Call(typeof(Queryable), "Any", taCust, query, pred));
            CheckMethod("Cast", Expression.Call(typeof(Queryable), "Cast", new Type[] { typeof(object) }, query));
            CheckMethod("Concat", Expression.Call(typeof(Queryable), "Concat", taCust, query, query));
            CheckMethod("Count", Expression.Call(typeof(Queryable), "Count", taCust, query));
            CheckMethod("Count", Expression.Call(typeof(Queryable), "Count", taCust, query, pred));
            CheckMethod("Distinct", Expression.Call(typeof(Queryable), "Distinct", taCust, query));
            CheckMethod("ElementAt", Expression.Call(typeof(Queryable), "ElementAt", taCust, query, Expression.Constant(1)));
            CheckMethod("ElementAtOrDefault", Expression.Call(typeof(Queryable), "ElementAtOrDefault", taCust, query, Expression.Constant(1)));
            CheckMethod("SequenceEqual", Expression.Call(typeof(Queryable), "SequenceEqual", taCust, query, query));
            CheckMethod("Except", Expression.Call(typeof(Queryable), "Except", taCust, query, query));
            CheckMethod("First", Expression.Call(typeof(Queryable), "First", taCust, query));
            CheckMethod("First", Expression.Call(typeof(Queryable), "First", taCust, query, pred));
            CheckMethod("FirstOrDefault", Expression.Call(typeof(Queryable), "FirstOrDefault", taCust, query));
            CheckMethod("FirstOrDefault", Expression.Call(typeof(Queryable), "FirstOrDefault", taCust, query, pred));
            CheckMethod("GroupBy", Expression.Call(typeof(Queryable), "GroupBy", taCustString, query, selName));
            CheckMethod("GroupBy", Expression.Call(typeof(Queryable), "GroupBy", taCustStringString, query, selName, selName));
            CheckMethod("GroupBy", Expression.Call(typeof(Queryable), "GroupBy", taCustString, query, selName, comparer));
            CheckMethod("GroupBy", Expression.Call(typeof(Queryable), "GroupBy", taCustStringString, query, selName, selName, comparer));
            CheckMethod("Intersect", Expression.Call(typeof(Queryable), "Intersect", taCust, query, query));
            CheckMethod("LongCount", Expression.Call(typeof(Queryable), "LongCount", taCust, query));
            CheckMethod("LongCount", Expression.Call(typeof(Queryable), "LongCount", taCust, query, pred));
            CheckMethod("Max", Expression.Call(typeof(Queryable), "Max", taCust, query));
            CheckMethod("Max", Expression.Call(typeof(Queryable), "Max", taCustString, query, selName));
            CheckMethod("Min", Expression.Call(typeof(Queryable), "Min", taCust, query));
            CheckMethod("Min", Expression.Call(typeof(Queryable), "Min", taCustString, query, selName));
            CheckMethod("OfType", Expression.Call(typeof(Queryable), "OfType", new Type[] { typeof(object) }, query));
            MethodCallExpression ordered = Expression.Call(typeof(Queryable), "OrderBy", taCustString, query, selName);
            CheckMethod("OrderBy", ordered);
            ordered = Expression.Call(typeof(Queryable), "OrderByDescending", taCustString, query, selName);
            CheckMethod("OrderByDescending", ordered);
            CheckMethod("Reverse", Expression.Call(typeof(Queryable), "Reverse", taCust, query));
            CheckMethod("Select", Expression.Call(typeof(Queryable), "Select", taCustString, query, selName));
            CheckMethod("SelectMany", Expression.Call(typeof(Queryable), "SelectMany", taCustOrder, query, selOrders));
            CheckMethod("SelectMany", Expression.Call(typeof(Queryable), "SelectMany", taCustOrderString, query, selOrders, joinPair));
            CheckMethod("Single", Expression.Call(typeof(Queryable), "Single", taCust, query));
            CheckMethod("Single", Expression.Call(typeof(Queryable), "Single", taCust, query, pred));
            CheckMethod("SingleOrDefault", Expression.Call(typeof(Queryable), "SingleOrDefault", taCust, query));
            CheckMethod("SingleOrDefault", Expression.Call(typeof(Queryable), "SingleOrDefault", taCust, query, pred));
            CheckMethod("Skip", Expression.Call(typeof(Queryable), "Skip", taCust, query, Expression.Constant(1)));
            CheckMethod("SkipWhile", Expression.Call(typeof(Queryable), "SkipWhile", taCust, query, pred));
            CheckMethod("Take", Expression.Call(typeof(Queryable), "Take", taCust, query, Expression.Constant(1)));
            CheckMethod("TakeWhile", Expression.Call(typeof(Queryable), "TakeWhile", taCust, query, pred));
            CheckMethod("ThenBy", Expression.Call(typeof(Queryable), "ThenBy", taCustString, ordered, selName));
            CheckMethod("ThenByDescending", Expression.Call(typeof(Queryable), "ThenByDescending", taCustString, ordered, selName));
            CheckMethod("Union", Expression.Call(typeof(Queryable), "Union", taCust, query, query));
            CheckMethod("Where", Expression.Call(typeof(Queryable), "Where", taCust, query, pred));
            CheckMethod("Join", Expression.Call(typeof(Queryable), "Join", taCustOrderStringString, query, query2, cId, ocId, joinPair));
            CheckMethod("Join", Expression.Call(typeof(Queryable), "Join", taCustOrderStringString, query, query2, cId, ocId, joinPair, comparer));
            CheckMethod("GroupJoin", Expression.Call(typeof(Queryable), "GroupJoin", taCustOrderStringString, query, query2, cId, ocId, custOrds));
            CheckMethod("GroupJoin", Expression.Call(typeof(Queryable), "GroupJoin", taCustOrderStringString, query, query2, cId, ocId, custOrds, comparer));
            CheckMethod("Zip", Expression.Call(typeof(Queryable), "Zip", taCustOrderString, query, query2, joinPair));

            ConstructAggregates<int>(0);
            ConstructAggregates<int?>(0);
            ConstructAggregates<long>(0);
            ConstructAggregates<long?>(0);
            ConstructAggregates<double>(0);
            ConstructAggregates<double?>(0);
            ConstructAggregates<decimal>(0);
            ConstructAggregates<decimal?>(0);
        }

        private static void ConstructAggregates<T>(T value)
        {
            ConstantExpression values = Expression.Constant(new T[] { }.AsQueryable());
            ConstantExpression custs = Expression.Constant(new NWindProxy.Customer[] { }.AsQueryable());
            Expression<Func<NWindProxy.Customer, T>> cvalue = c => value;
            Type[] taCust = new Type[] { typeof(NWindProxy.Customer) };
            CheckMethod("Sum", Expression.Call(typeof(Queryable), "Sum", null, values));
            CheckMethod("Sum", Expression.Call(typeof(Queryable), "Sum", taCust, custs, cvalue));
            CheckMethod("Average", Expression.Call(typeof(Queryable), "Average", null, values));
            CheckMethod("Average", Expression.Call(typeof(Queryable), "Average", taCust, custs, cvalue));
        }

        private static void CheckMethod(string name, Expression expr)
        {
            Assert.Equal(ExpressionType.Call, expr.NodeType);
            MethodCallExpression mc = expr as MethodCallExpression;
            Assert.Equal(name, mc.Method.Name);

            Assert.Equal(typeof(Queryable), mc.Method.DeclaringType);
        }
    }

    namespace NWindProxy
    {
        public class Customer
        {
            public string CustomerID;
            public string ContactName;
            public string CompanyName;
            public List<Order> Orders = new List<Order>();
        }
        public class Order
        {
            public string CustomerID;
        }
    }

    public class Compiler_Tests
    {
        public class AndAlso
        {
            public bool value;
            public AndAlso(bool value) { this.value = value; }
            public static AndAlso operator &(AndAlso a1, AndAlso a2) { return new AndAlso(a1.value && a2.value); }
            public static bool operator true(AndAlso a) { return a.value; }
            public static bool operator false(AndAlso a) { return !a.value; }
            public static AndAlso operator !(AndAlso a) { return new AndAlso(false); }
            public override string ToString() { return value.ToString(); }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void TestAndAlso(bool useInterpreter)
        {
            AndAlso a1 = new AndAlso(true);
            Func<AndAlso> f1 = () => a1 && !a1;
            AndAlso r1 = f1();

            Expression<Func<AndAlso>> e = () => a1 && !a1;
            AndAlso r2 = e.Compile(useInterpreter)();

            Assert.Equal(r2.value, r1.value);
        }

        public struct TC1
        {
            public string Name;
            public int data;

            public TC1(string name, int data)
            {
                Name = name;
                this.data = data;
            }
            public static TC1 operator &(TC1 t1, TC1 t2) { return new TC1("And", 01); }
            public static TC1 operator |(TC1 t1, TC1 t2) { return new TC1("Or", 02); }
            public static TC1 Meth1(TC1 t1, TC1 t2) { return new TC1(); }
            public static bool operator true(TC1 a) { return true; }
            public static bool operator false(TC1 a) { return false; }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void ObjectCallOnValueType(bool useInterpreter)
        {
            object st_local = new TC1();
            MethodInfo mi = typeof(object).GetMethod("ToString");
            Expression<Func<string>> lam = Expression.Lambda<Func<string>>(Expression.Call(Expression.Constant(st_local), mi, null), null);
            Func<string> f = lam.Compile(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void AndAlsoLift(bool useInterpreter)
        {
            TC1? tc1 = new TC1("lhs", 324589);
            TC1? tc2 = new TC1("rhs", 324589);

            ConstantExpression left = Expression.Constant(tc1, typeof(TC1?));
            ConstantExpression right = Expression.Constant(tc2, typeof(TC1?));
            ParameterExpression p0 = Expression.Parameter(typeof(TC1?), "tc1");
            ParameterExpression p1 = Expression.Parameter(typeof(TC1?), "tc2");

            BinaryExpression result = (BinaryExpression)Expression.AndAlso(left, right);
            Expression<Func<TC1?>> e1 = Expression.Lambda<Func<TC1?>>(
                Expression.Invoke(
                    Expression.Lambda<Func<TC1?, TC1?, TC1?>>((result), new ParameterExpression[] { p0, p1 }),
                    new Expression[] { left, right }),
               Enumerable.Empty<ParameterExpression>());

            Func<TC1?> f1 = e1.Compile(useInterpreter);
            Assert.NotNull(f1());
            Assert.Equal("And", f1().Value.Name);

            BinaryExpression resultOr = (BinaryExpression)Expression.OrElse(left, right);
            Expression<Func<TC1?>> e2 = Expression.Lambda<Func<TC1?>>(
                Expression.Invoke(
                    Expression.Lambda<Func<TC1?, TC1?, TC1?>>((resultOr), new ParameterExpression[] { p0, p1 }),
                    new Expression[] { left, right }),
               Enumerable.Empty<ParameterExpression>());

            Func<TC1?> f2 = e2.Compile(useInterpreter);
            Assert.NotNull(f2());
            Assert.Equal("lhs", f2().Value.Name);

            ConstantExpression constant = Expression.Constant(1.0, typeof(double));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Lambda<Func<double?>>(constant, null));
        }

        public static int GetBound()
        {
            return 1;
        }

        public int Bound
        {
            get
            {
                return 3;
            }
        }

        public struct Complex
        {
            public int x;
            public int y;

            public static Complex operator +(Complex c)
            {
                Complex temp = new Complex();
                temp.x = c.x + 1;
                temp.y = c.y + 1;
                return temp;
            }
            public Complex(int x, int y) { this.x = x; this.y = y; }
        }

        public class CustomerWriteBack
        {
            private Customer _cust;
            public string m_x = "ha ha ha";
            public string Func0(ref string x)
            {
                x = "Changed";
                return x;
            }
            public string X
            {
                get { return m_x; }
                set { m_x = value; }
            }
            public static int Funct1(ref int i)
            {
                return i = 5;
            }
            public static int Prop { get { return 7; } set { Assert.Equal(5, value); } }

            public Customer Cust
            {
                get
                {
                    if (_cust == null) _cust = new Customer(98007, "Sree");
                    return _cust;
                }
                set
                {
                    _cust = value;
                }
            }

            public Customer ComputeCust(ref Customer cust)
            {
                cust.zip = 90008;
                cust.name = "SreeCho";
                return cust;
            }
        }

        public class Customer
        {
            public int zip;
            public string name;
            public Customer(int zip, string name) { this.zip = zip; this.name = name; }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void Writeback(bool useInterpreter)
        {
            CustomerWriteBack a = new CustomerWriteBack();
            Type t = typeof(CustomerWriteBack);
            MethodInfo mi = t.GetMethod("Func0");
            MethodInfo pi = t.GetMethod("get_X");
            MethodInfo piCust = t.GetMethod("get_Cust");
            MethodInfo miCust = t.GetMethod("ComputeCust");

            Expression<Func<int>> e1 =
                    Expression.Lambda<Func<int>>(
                        Expression.Call(typeof(CustomerWriteBack).GetMethod("Funct1"), new[] { Expression.Property(null, typeof(CustomerWriteBack).GetProperty("Prop")) }),
                        null);
            Func<int> f1 = e1.Compile(useInterpreter);
            int result = f1();
            Assert.Equal(5, result);

            Expression<Func<string>> e = Expression.Lambda<Func<string>>(
                 Expression.Call(
                     Expression.Constant(a, typeof(CustomerWriteBack)),
                     mi,
                     new Expression[] { Expression.Property(Expression.Constant(a, typeof(CustomerWriteBack)), pi) }
                     ),
                 null);
            Func<string> f = e.Compile(useInterpreter);
            string r = f();
            Assert.Equal("Changed", a.m_x);

            Expression<Func<Customer>> e2 = Expression.Lambda<Func<Customer>>(
                 Expression.Call(
                     Expression.Constant(a, typeof(CustomerWriteBack)),
                     miCust,
                     new Expression[] { Expression.Property(Expression.Constant(a, typeof(CustomerWriteBack)), piCust) }
                     ),
                 null);
            Func<Customer> f2 = e2.Compile(useInterpreter);
            Customer r2 = f2();
            Assert.True(a.Cust.zip == 90008 && a.Cust.name == "SreeCho");
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void UnaryPlus(bool useInterpreter)
        {
            ConstantExpression ce = Expression.Constant((ushort)10);

            UnaryExpression result = Expression.UnaryPlus(ce);

            Assert.Throws<InvalidOperationException>(() =>
            {
                //unary Plus Operator
                byte val = 10;
                Expression<Func<byte>> e =
                    Expression.Lambda<Func<byte>>(
                        Expression.UnaryPlus(Expression.Constant(val, typeof(byte))),
                        Enumerable.Empty<ParameterExpression>());
            });

            //User-defined objects
            Complex comp = new Complex(10, 20);
            Expression<Func<Complex>> e1 =
                Expression.Lambda<Func<Complex>>(
                    Expression.UnaryPlus(Expression.Constant(comp, typeof(Complex))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Complex> f1 = e1.Compile(useInterpreter);
            Complex comp1 = f1();
            Assert.True((comp1.x == comp.x + 1 && comp1.y == comp.y + 1));

            Expression<Func<Complex, Complex>> testExpr = (x) => +x;
            Assert.Equal("x => +x", testExpr.ToString());
            Func<Complex, Complex> v = testExpr.Compile(useInterpreter);
        }

        private struct S
        {
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CompileRelationOveratorswithIsLiftToNullTrue(bool useInterpreter)
        {
            int? x = 10;
            int? y = 2;
            ParameterExpression p1 = Expression.Parameter(typeof(int?), "x");
            ParameterExpression p2 = Expression.Parameter(typeof(int?), "y");

            Expression<Func<int?, int?, bool?>> e = Expression.Lambda<Func<int?, int?, bool?>>(
                Expression.GreaterThan(p1, p2, true, null), new ParameterExpression[] { p1, p2 });
            Func<int?, int?, bool?> f = e.Compile(useInterpreter);
            bool? r = f(x, y);
            Assert.True(r.Value);

            Expression<Func<int?, int?, bool?>> e1 = Expression.Lambda<Func<int?, int?, bool?>>(
                Expression.LessThan(p1, p2, true, null), new ParameterExpression[] { p1, p2 });
            f = e1.Compile(useInterpreter);
            r = f(x, y);
            Assert.False(r.Value);

            Expression<Func<int?, int?, bool?>> e2 = Expression.Lambda<Func<int?, int?, bool?>>(
                Expression.GreaterThanOrEqual(p1, p2, true, null), new ParameterExpression[] { p1, p2 });
            f = e2.Compile(useInterpreter);
            r = f(x, y);
            Assert.True(r.Value);

            Expression<Func<int?, int?, bool?>> e3 = Expression.Lambda<Func<int?, int?, bool?>>(
                Expression.Equal(p1, p2, true, null), new ParameterExpression[] { p1, p2 });
            f = e3.Compile(useInterpreter);
            r = f(x, y);
            Assert.False(r.Value);

            Expression<Func<int?, int?, bool?>> e4 = Expression.Lambda<Func<int?, int?, bool?>>(
                Expression.LessThanOrEqual(p1, p2, true, null), new ParameterExpression[] { p1, p2 });
            f = e4.Compile(useInterpreter);
            r = f(x, y);
            Assert.False(r.Value);

            Expression<Func<int?, int?, bool?>> e5 = Expression.Lambda<Func<int?, int?, bool?>>(
                Expression.NotEqual(p1, p2, true, null), new ParameterExpression[] { p1, p2 });
            f = e5.Compile(useInterpreter);
            r = f(x, y);
            Assert.True(r.Value);

            int? n = 10;
            Expression<Func<bool?>> e6 = Expression.Lambda<Func<bool?>>(
                Expression.NotEqual(
                    Expression.Constant(n, typeof(int?)),
                    Expression.Convert(Expression.Constant(null, typeof(object)), typeof(int?)),
                    true,
                    null),
                null);
            Func<bool?> f6 = e6.Compile(useInterpreter);
            Assert.Null(f6());
        }

        private class TestClass : IEquatable<TestClass>
        {
            private int _val;
            public TestClass(string S, int Val)
            {
                this.S = S;
                _val = Val;
            }

            public string S;
            public int Val { get { return _val; } set { _val = value; } }

            public override bool Equals(object o)
            {
                return (o is TestClass) && Equals((TestClass)o);
            }
            public bool Equals(TestClass other)
            {
                return other.S == S;
            }
            public override int GetHashCode()
            {
                return S.GetHashCode();
            }
        }


        private class AnonHelperClass1
        {
            public Expression<Func<decimal>> mem1;
            public AnonHelperClass1(Expression<Func<decimal>> mem1) { this.mem1 = mem1; }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void NewExpressionwithMemberAssignInit(bool useInterpreter)
        {
            string s = "Bad Mojo";
            int val = 10;

            ConstructorInfo constructor = typeof(TestClass).GetConstructor(new Type[] { typeof(string), typeof(int) });
            MemberInfo[] members = new MemberInfo[] { typeof(TestClass).GetField("S"), typeof(TestClass).GetProperty("Val") };
            Expression[] expressions = new Expression[] { Expression.Constant(s, typeof(string)), Expression.Constant(val, typeof(int)) };

            Expression<Func<TestClass>> e = Expression.Lambda<Func<TestClass>>(
               Expression.New(constructor, expressions, members),
               Enumerable.Empty<ParameterExpression>());
            Func<TestClass> f = e.Compile(useInterpreter);
            Assert.True(object.Equals(f(), new TestClass(s, val)));

            List<MemberInfo> members1 = new List<MemberInfo>();
            members1.Add(typeof(TestClass).GetField("S"));
            members1.Add(typeof(TestClass).GetProperty("Val"));

            Expression<Func<TestClass>> e1 = Expression.Lambda<Func<TestClass>>(
               Expression.New(constructor, expressions, members1),
               Enumerable.Empty<ParameterExpression>());
            Func<TestClass> f1 = e1.Compile(useInterpreter);
            Assert.True(object.Equals(f1(), new TestClass(s, val)));
            MemberInfo mem1 = typeof(AnonHelperClass1).GetField("mem1");
            LambdaExpression ce1 = Expression.Lambda(Expression.Constant(45m, typeof(decimal)));
            ConstructorInfo constructor1 = typeof(AnonHelperClass1).GetConstructor(new Type[] { typeof(Expression<Func<decimal>>) });

            Expression[] arguments = new Expression[] { ce1 };
            MemberInfo[] members2 = new MemberInfo[] { mem1 };

            NewExpression result = Expression.New(constructor1, arguments, members2);
            Assert.NotNull(result);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void TypeAsNullableToObject(bool useInterpreter)
        {
            Expression<Func<object>> e = Expression.Lambda<Func<object>>(Expression.TypeAs(Expression.Constant(0, typeof(int?)), typeof(object)));
            Func<object> f = e.Compile(useInterpreter); // System.ArgumentException: Unhandled unary: TypeAs
            Assert.Equal(0, f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void TypesIsConstantValueType(bool useInterpreter)
        {
            Expression<Func<bool>> e = Expression.Lambda<Func<bool>>(Expression.TypeIs(Expression.Constant(5), typeof(object)));
            Func<bool> f = e.Compile(useInterpreter);
            Assert.True(f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void ConstantEmitsValidIL(bool useInterpreter)
        {
            Expression<Func<byte>> e = Expression.Lambda<Func<byte>>(Expression.Constant((byte)0), Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);
            Assert.Equal((byte)0, f());
        }

        public struct MyStruct : IComparable
        {
            int IComparable.CompareTo(object other)
            {
                return 0;
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void Casts(bool useInterpreter)
        {
            // System.ValueType to value type
            Assert.Equal(10, TestCast<System.ValueType, int>(10, useInterpreter));

            // System.ValueType to enum type
            Assert.Equal(ExpressionType.Add, TestCast<System.ValueType, ExpressionType>(ExpressionType.Add, useInterpreter));

            // System.Enum to enum type
            Assert.Equal(ExpressionType.Add, TestCast<System.Enum, ExpressionType>(ExpressionType.Add, useInterpreter));

            // System.ValueType to nullable value type
            Assert.Equal(10, TestCast<System.ValueType, int?>(10, useInterpreter));

            // System.ValueType to nullable enum type
            Assert.Equal(ExpressionType.Add, TestCast<System.ValueType, ExpressionType?>(ExpressionType.Add, useInterpreter));

            // System.Enum to nullable enum type
            Assert.Equal(ExpressionType.Add, TestCast<System.Enum, ExpressionType?>(ExpressionType.Add, useInterpreter));

            // Enum to System.Enum
            Assert.Equal(ExpressionType.Add, TestCast<ExpressionType, System.Enum>(ExpressionType.Add, useInterpreter));

            // Enum to System.ValueType
            Assert.Equal(ExpressionType.Add, TestCast<ExpressionType, System.ValueType>(ExpressionType.Add, useInterpreter));

            // nullable enum to System.Enum
            Assert.Equal(ExpressionType.Add, TestCast<ExpressionType?, System.Enum>(ExpressionType.Add, useInterpreter));

            // nullable enum to System.ValueType
            Assert.Equal(ExpressionType.Add, TestCast<ExpressionType?, System.ValueType>(ExpressionType.Add, useInterpreter));

            // nullable to object (box)
            Assert.Equal(10, TestCast<int?, object>(10, useInterpreter));

            // object to nullable (unbox)
            Assert.Equal(10, TestCast<object, int?>(10, useInterpreter));

            // nullable to interface (box + cast)
            TestCast<int?, IComparable>(10, useInterpreter);

            // interface to nullable (unbox)
            TestCast<IComparable, int?>(10, useInterpreter);

            // interface to interface
            TestCast<IComparable, IEquatable<int>>(10, useInterpreter);
            TestCast<IEquatable<int>, IComparable>(10, useInterpreter);

            // value type to object (box)
            Assert.Equal(10, TestCast<int, object>(10, useInterpreter));

            // object to value type (unbox)
            Assert.Equal(10, TestCast<object, int>(10, useInterpreter));

            // tests with user defined struct
            TestCast<ValueType, MyStruct>(new MyStruct(), useInterpreter);
            TestCast<MyStruct, ValueType>(new MyStruct(), useInterpreter);
            TestCast<object, MyStruct>(new MyStruct(), useInterpreter);
            TestCast<MyStruct, object>(new MyStruct(), useInterpreter);
            TestCast<IComparable, MyStruct>(new MyStruct(), useInterpreter);
            TestCast<MyStruct, IComparable>(new MyStruct(), useInterpreter);
            TestCast<ValueType, MyStruct?>(new MyStruct(), useInterpreter);
            TestCast<MyStruct?, ValueType>(new MyStruct(), useInterpreter);
            TestCast<object, MyStruct?>(new MyStruct(), useInterpreter);
            TestCast<MyStruct?, object>(new MyStruct(), useInterpreter);
            TestCast<IComparable, MyStruct?>(new MyStruct(), useInterpreter);
            TestCast<MyStruct?, IComparable>(new MyStruct(), useInterpreter);
        }

        private static S TestCast<T, S>(T value, bool useInterpreter)
        {
            Func<S> d = Expression.Lambda<Func<S>>(Expression.Convert(Expression.Constant(value, typeof(T)), typeof(S))).Compile(useInterpreter);
            return d();
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void Conversions(bool useInterpreter)
        {
            Assert.Equal((byte)10, TestConvert<byte, byte>(10, useInterpreter));
            Assert.Equal((byte)10, TestConvert<sbyte, byte>(10, useInterpreter));
            Assert.Equal((byte)10, TestConvert<short, byte>(10, useInterpreter));
            Assert.Equal((byte)10, TestConvert<ushort, byte>(10, useInterpreter));
            Assert.Equal((byte)10, TestConvert<int, byte>(10, useInterpreter));
            Assert.Equal((byte)10, TestConvert<uint, byte>(10, useInterpreter));
            Assert.Equal((byte)10, TestConvert<long, byte>(10, useInterpreter));
            Assert.Equal((byte)10, TestConvert<ulong, byte>(10, useInterpreter));
            Assert.Equal((byte)10, TestConvert<float, byte>(10.0f, useInterpreter));
            Assert.Equal((byte)10, TestConvert<float, byte>(10.0f, useInterpreter));
            Assert.Equal((byte)10, TestConvert<double, byte>(10.0, useInterpreter));
            Assert.Equal((byte)10, TestConvert<double, byte>(10.0, useInterpreter));
            Assert.Equal((byte)10, TestConvert<decimal, byte>(10m, useInterpreter));
            Assert.Equal((byte)10, TestConvert<decimal, byte>(10m, useInterpreter));

            Assert.Equal((short)10, TestConvert<byte, short>(10, useInterpreter));
            Assert.Equal((short)10, TestConvert<sbyte, short>(10, useInterpreter));
            Assert.Equal((short)10, TestConvert<short, short>(10, useInterpreter));
            Assert.Equal((short)10, TestConvert<ushort, short>(10, useInterpreter));
            Assert.Equal((short)10, TestConvert<int, short>(10, useInterpreter));
            Assert.Equal((short)10, TestConvert<uint, short>(10, useInterpreter));
            Assert.Equal((short)10, TestConvert<long, short>(10, useInterpreter));
            Assert.Equal((short)10, TestConvert<ulong, short>(10, useInterpreter));
            Assert.Equal((short)10, TestConvert<float, short>(10.0f, useInterpreter));
            Assert.Equal((short)10, TestConvert<double, short>(10.0, useInterpreter));
            Assert.Equal((short)10, TestConvert<decimal, short>(10m, useInterpreter));

            Assert.Equal((int)10, TestConvert<byte, int>(10, useInterpreter));
            Assert.Equal((int)10, TestConvert<sbyte, int>(10, useInterpreter));
            Assert.Equal((int)10, TestConvert<short, int>(10, useInterpreter));
            Assert.Equal((int)10, TestConvert<ushort, int>(10, useInterpreter));
            Assert.Equal((int)10, TestConvert<int, int>(10, useInterpreter));
            Assert.Equal((int)10, TestConvert<uint, int>(10, useInterpreter));
            Assert.Equal((int)10, TestConvert<long, int>(10, useInterpreter));
            Assert.Equal((int)10, TestConvert<ulong, int>(10, useInterpreter));
            Assert.Equal((int)10, TestConvert<float, int>(10.0f, useInterpreter));
            Assert.Equal((int)10, TestConvert<double, int>(10.0, useInterpreter));
            Assert.Equal((int)10, TestConvert<decimal, int>(10m, useInterpreter));

            Assert.Equal((long)10, TestConvert<byte, long>(10, useInterpreter));
            Assert.Equal((long)10, TestConvert<sbyte, long>(10, useInterpreter));
            Assert.Equal((long)10, TestConvert<short, long>(10, useInterpreter));
            Assert.Equal((long)10, TestConvert<ushort, long>(10, useInterpreter));
            Assert.Equal((long)10, TestConvert<int, long>(10, useInterpreter));
            Assert.Equal((long)10, TestConvert<uint, long>(10, useInterpreter));
            Assert.Equal((long)10, TestConvert<long, long>(10, useInterpreter));
            Assert.Equal((long)10, TestConvert<ulong, long>(10, useInterpreter));
            Assert.Equal((long)10, TestConvert<float, long>(10.0f, useInterpreter));
            Assert.Equal((long)10, TestConvert<double, long>(10.0, useInterpreter));
            Assert.Equal((long)10, TestConvert<decimal, long>(10m, useInterpreter));

            Assert.Equal((double)10, TestConvert<byte, double>(10, useInterpreter));
            Assert.Equal((double)10, TestConvert<sbyte, double>(10, useInterpreter));
            Assert.Equal((double)10, TestConvert<short, double>(10, useInterpreter));
            Assert.Equal((double)10, TestConvert<ushort, double>(10, useInterpreter));
            Assert.Equal((double)10, TestConvert<int, double>(10, useInterpreter));
            Assert.Equal((double)10, TestConvert<uint, double>(10, useInterpreter));
            Assert.Equal((double)10, TestConvert<long, double>(10, useInterpreter));
            Assert.Equal((double)10, TestConvert<ulong, double>(10, useInterpreter));
            Assert.Equal((double)10, TestConvert<float, double>(10.0f, useInterpreter));
            Assert.Equal((double)10, TestConvert<double, double>(10.0, useInterpreter));
            Assert.Equal((double)10, TestConvert<decimal, double>(10m, useInterpreter));

            Assert.Equal((decimal)10, TestConvert<byte, decimal>(10, useInterpreter));
            Assert.Equal((decimal)10, TestConvert<sbyte, decimal>(10, useInterpreter));
            Assert.Equal((decimal)10, TestConvert<short, decimal>(10, useInterpreter));
            Assert.Equal((decimal)10, TestConvert<ushort, decimal>(10, useInterpreter));
            Assert.Equal((decimal)10, TestConvert<int, decimal>(10, useInterpreter));
            Assert.Equal((decimal)10, TestConvert<uint, decimal>(10, useInterpreter));
            Assert.Equal((decimal)10, TestConvert<long, decimal>(10, useInterpreter));
            Assert.Equal((decimal)10, TestConvert<ulong, decimal>(10, useInterpreter));
            Assert.Equal((decimal)10, TestConvert<float, decimal>(10.0f, useInterpreter));
            Assert.Equal((decimal)10, TestConvert<double, decimal>(10.0, useInterpreter));
            Assert.Equal((decimal)10, TestConvert<decimal, decimal>(10m, useInterpreter));

            // nullable to non-nullable
            Assert.Equal((byte)10, TestConvert<byte?, byte>(10, useInterpreter));
            Assert.Equal((byte)10, TestConvert<sbyte?, byte>(10, useInterpreter));
            Assert.Equal((byte)10, TestConvert<short?, byte>(10, useInterpreter));
            Assert.Equal((byte)10, TestConvert<ushort?, byte>(10, useInterpreter));
            Assert.Equal((byte)10, TestConvert<int?, byte>(10, useInterpreter));
            Assert.Equal((byte)10, TestConvert<uint?, byte>(10, useInterpreter));
            Assert.Equal((byte)10, TestConvert<long?, byte>(10, useInterpreter));
            Assert.Equal((byte)10, TestConvert<ulong?, byte>(10, useInterpreter));
            Assert.Equal((byte)10, TestConvert<float?, byte>(10.0f, useInterpreter));
            Assert.Equal((byte)10, TestConvert<float?, byte>(10.0f, useInterpreter));
            Assert.Equal((byte)10, TestConvert<double?, byte>(10.0, useInterpreter));
            Assert.Equal((byte)10, TestConvert<double?, byte>(10.0, useInterpreter));
            Assert.Equal((byte)10, TestConvert<decimal?, byte>(10m, useInterpreter));
            Assert.Equal((byte)10, TestConvert<decimal?, byte>(10m, useInterpreter));

            Assert.Equal((short)10, TestConvert<byte?, short>(10, useInterpreter));
            Assert.Equal((short)10, TestConvert<sbyte?, short>(10, useInterpreter));
            Assert.Equal((short)10, TestConvert<short?, short>(10, useInterpreter));
            Assert.Equal((short)10, TestConvert<ushort?, short>(10, useInterpreter));
            Assert.Equal((short)10, TestConvert<int?, short>(10, useInterpreter));
            Assert.Equal((short)10, TestConvert<uint?, short>(10, useInterpreter));
            Assert.Equal((short)10, TestConvert<long?, short>(10, useInterpreter));
            Assert.Equal((short)10, TestConvert<ulong?, short>(10, useInterpreter));
            Assert.Equal((short)10, TestConvert<float?, short>(10.0f, useInterpreter));
            Assert.Equal((short)10, TestConvert<double?, short>(10.0, useInterpreter));
            Assert.Equal((short)10, TestConvert<decimal?, short>(10m, useInterpreter));

            Assert.Equal((int)10, TestConvert<byte?, int>(10, useInterpreter));
            Assert.Equal((int)10, TestConvert<sbyte?, int>(10, useInterpreter));
            Assert.Equal((int)10, TestConvert<short?, int>(10, useInterpreter));
            Assert.Equal((int)10, TestConvert<ushort?, int>(10, useInterpreter));
            Assert.Equal((int)10, TestConvert<int?, int>(10, useInterpreter));
            Assert.Equal((int)10, TestConvert<uint?, int>(10, useInterpreter));
            Assert.Equal((int)10, TestConvert<long?, int>(10, useInterpreter));
            Assert.Equal((int)10, TestConvert<ulong?, int>(10, useInterpreter));
            Assert.Equal((int)10, TestConvert<float?, int>(10.0f, useInterpreter));
            Assert.Equal((int)10, TestConvert<double?, int>(10.0, useInterpreter));
            Assert.Equal((int)10, TestConvert<decimal?, int>(10m, useInterpreter));

            Assert.Equal((long)10, TestConvert<byte?, long>(10, useInterpreter));
            Assert.Equal((long)10, TestConvert<sbyte?, long>(10, useInterpreter));
            Assert.Equal((long)10, TestConvert<short?, long>(10, useInterpreter));
            Assert.Equal((long)10, TestConvert<ushort?, long>(10, useInterpreter));
            Assert.Equal((long)10, TestConvert<int?, long>(10, useInterpreter));
            Assert.Equal((long)10, TestConvert<uint?, long>(10, useInterpreter));
            Assert.Equal((long)10, TestConvert<long?, long>(10, useInterpreter));
            Assert.Equal((long)10, TestConvert<ulong?, long>(10, useInterpreter));
            Assert.Equal((long)10, TestConvert<float?, long>(10.0f, useInterpreter));
            Assert.Equal((long)10, TestConvert<double?, long>(10.0, useInterpreter));
            Assert.Equal((long)10, TestConvert<decimal?, long>(10m, useInterpreter));

            Assert.Equal((double)10, TestConvert<byte?, double>(10, useInterpreter));
            Assert.Equal((double)10, TestConvert<sbyte?, double>(10, useInterpreter));
            Assert.Equal((double)10, TestConvert<short?, double>(10, useInterpreter));
            Assert.Equal((double)10, TestConvert<ushort?, double>(10, useInterpreter));
            Assert.Equal((double)10, TestConvert<int?, double>(10, useInterpreter));
            Assert.Equal((double)10, TestConvert<uint?, double>(10, useInterpreter));
            Assert.Equal((double)10, TestConvert<long?, double>(10, useInterpreter));
            Assert.Equal((double)10, TestConvert<ulong?, double>(10, useInterpreter));
            Assert.Equal((double)10, TestConvert<float?, double>(10.0f, useInterpreter));
            Assert.Equal((double)10, TestConvert<double?, double>(10.0, useInterpreter));
            Assert.Equal((double)10, TestConvert<decimal?, double>(10m, useInterpreter));

            Assert.Equal((decimal)10, TestConvert<byte?, decimal>(10, useInterpreter));
            Assert.Equal((decimal)10, TestConvert<sbyte?, decimal>(10, useInterpreter));
            Assert.Equal((decimal)10, TestConvert<short?, decimal>(10, useInterpreter));
            Assert.Equal((decimal)10, TestConvert<ushort?, decimal>(10, useInterpreter));
            Assert.Equal((decimal)10, TestConvert<int?, decimal>(10, useInterpreter));
            Assert.Equal((decimal)10, TestConvert<uint?, decimal>(10, useInterpreter));
            Assert.Equal((decimal)10, TestConvert<long?, decimal>(10, useInterpreter));
            Assert.Equal((decimal)10, TestConvert<ulong?, decimal>(10, useInterpreter));
            Assert.Equal((decimal)10, TestConvert<float?, decimal>(10.0f, useInterpreter));
            Assert.Equal((decimal)10, TestConvert<double?, decimal>(10.0, useInterpreter));
            Assert.Equal((decimal)10, TestConvert<decimal?, decimal>(10m, useInterpreter));

            // non-nullable to nullable
            Assert.Equal((byte?)10, TestConvert<byte, byte?>(10, useInterpreter));
            Assert.Equal((byte?)10, TestConvert<sbyte, byte?>(10, useInterpreter));
            Assert.Equal((byte?)10, TestConvert<short, byte?>(10, useInterpreter));
            Assert.Equal((byte?)10, TestConvert<ushort, byte?>(10, useInterpreter));
            Assert.Equal((byte?)10, TestConvert<int, byte?>(10, useInterpreter));
            Assert.Equal((byte?)10, TestConvert<uint, byte?>(10, useInterpreter));
            Assert.Equal((byte?)10, TestConvert<long, byte?>(10, useInterpreter));
            Assert.Equal((byte?)10, TestConvert<ulong, byte?>(10, useInterpreter));
            Assert.Equal((byte?)10, TestConvert<float, byte?>(10.0f, useInterpreter));
            Assert.Equal((byte?)10, TestConvert<float, byte?>(10.0f, useInterpreter));
            Assert.Equal((byte?)10, TestConvert<double, byte?>(10.0, useInterpreter));
            Assert.Equal((byte?)10, TestConvert<double, byte?>(10.0, useInterpreter));
            Assert.Equal((byte?)10, TestConvert<decimal, byte?>(10m, useInterpreter));
            Assert.Equal((byte?)10, TestConvert<decimal, byte?>(10m, useInterpreter));

            Assert.Equal((short?)10, TestConvert<byte, short?>(10, useInterpreter));
            Assert.Equal((short?)10, TestConvert<sbyte, short?>(10, useInterpreter));
            Assert.Equal((short?)10, TestConvert<short, short?>(10, useInterpreter));
            Assert.Equal((short?)10, TestConvert<ushort, short?>(10, useInterpreter));
            Assert.Equal((short?)10, TestConvert<int, short?>(10, useInterpreter));
            Assert.Equal((short?)10, TestConvert<uint, short?>(10, useInterpreter));
            Assert.Equal((short?)10, TestConvert<long, short?>(10, useInterpreter));
            Assert.Equal((short?)10, TestConvert<ulong, short?>(10, useInterpreter));
            Assert.Equal((short?)10, TestConvert<float, short?>(10.0f, useInterpreter));
            Assert.Equal((short?)10, TestConvert<double, short?>(10.0, useInterpreter));
            Assert.Equal((short?)10, TestConvert<decimal, short?>(10m, useInterpreter));

            Assert.Equal((int?)10, TestConvert<byte, int?>(10, useInterpreter));
            Assert.Equal((int?)10, TestConvert<sbyte, int?>(10, useInterpreter));
            Assert.Equal((int?)10, TestConvert<short, int?>(10, useInterpreter));
            Assert.Equal((int?)10, TestConvert<ushort, int?>(10, useInterpreter));
            Assert.Equal((int?)10, TestConvert<int, int?>(10, useInterpreter));
            Assert.Equal((int?)10, TestConvert<uint, int?>(10, useInterpreter));
            Assert.Equal((int?)10, TestConvert<long, int?>(10, useInterpreter));
            Assert.Equal((int?)10, TestConvert<ulong, int?>(10, useInterpreter));
            Assert.Equal((int?)10, TestConvert<float, int?>(10.0f, useInterpreter));
            Assert.Equal((int?)10, TestConvert<double, int?>(10.0, useInterpreter));
            Assert.Equal((int?)10, TestConvert<decimal, int?>(10m, useInterpreter));

            Assert.Equal((long?)10, TestConvert<byte, long?>(10, useInterpreter));
            Assert.Equal((long?)10, TestConvert<sbyte, long?>(10, useInterpreter));
            Assert.Equal((long?)10, TestConvert<short, long?>(10, useInterpreter));
            Assert.Equal((long?)10, TestConvert<ushort, long?>(10, useInterpreter));
            Assert.Equal((long?)10, TestConvert<int, long?>(10, useInterpreter));
            Assert.Equal((long?)10, TestConvert<uint, long?>(10, useInterpreter));
            Assert.Equal((long?)10, TestConvert<long, long?>(10, useInterpreter));
            Assert.Equal((long?)10, TestConvert<ulong, long?>(10, useInterpreter));
            Assert.Equal((long?)10, TestConvert<float, long?>(10.0f, useInterpreter));
            Assert.Equal((long?)10, TestConvert<double, long?>(10.0, useInterpreter));
            Assert.Equal((long?)10, TestConvert<decimal, long?>(10m, useInterpreter));

            Assert.Equal((double?)10, TestConvert<byte, double?>(10, useInterpreter));
            Assert.Equal((double?)10, TestConvert<sbyte, double?>(10, useInterpreter));
            Assert.Equal((double?)10, TestConvert<short, double?>(10, useInterpreter));
            Assert.Equal((double?)10, TestConvert<ushort, double?>(10, useInterpreter));
            Assert.Equal((double?)10, TestConvert<int, double?>(10, useInterpreter));
            Assert.Equal((double?)10, TestConvert<uint, double?>(10, useInterpreter));
            Assert.Equal((double?)10, TestConvert<long, double?>(10, useInterpreter));
            Assert.Equal((double?)10, TestConvert<ulong, double?>(10, useInterpreter));
            Assert.Equal((double?)10, TestConvert<float, double?>(10.0f, useInterpreter));
            Assert.Equal((double?)10, TestConvert<double, double?>(10.0, useInterpreter));
            Assert.Equal((double?)10, TestConvert<decimal, double?>(10m, useInterpreter));

            Assert.Equal((decimal?)10, TestConvert<byte, decimal?>(10, useInterpreter));
            Assert.Equal((decimal?)10, TestConvert<sbyte, decimal?>(10, useInterpreter));
            Assert.Equal((decimal?)10, TestConvert<short, decimal?>(10, useInterpreter));
            Assert.Equal((decimal?)10, TestConvert<ushort, decimal?>(10, useInterpreter));
            Assert.Equal((decimal?)10, TestConvert<int, decimal?>(10, useInterpreter));
            Assert.Equal((decimal?)10, TestConvert<uint, decimal?>(10, useInterpreter));
            Assert.Equal((decimal?)10, TestConvert<long, decimal?>(10, useInterpreter));
            Assert.Equal((decimal?)10, TestConvert<ulong, decimal?>(10, useInterpreter));
            Assert.Equal((decimal?)10, TestConvert<float, decimal?>(10.0f, useInterpreter));
            Assert.Equal((decimal?)10, TestConvert<double, decimal?>(10.0, useInterpreter));
            Assert.Equal((decimal?)10, TestConvert<decimal, decimal?>(10m, useInterpreter));

            // nullable to nullable
            Assert.Equal((byte?)10, TestConvert<byte?, byte?>(10, useInterpreter));
            Assert.Equal((byte?)10, TestConvert<sbyte?, byte?>(10, useInterpreter));
            Assert.Equal((byte?)10, TestConvert<short?, byte?>(10, useInterpreter));
            Assert.Equal((byte?)10, TestConvert<ushort?, byte?>(10, useInterpreter));
            Assert.Equal((byte?)10, TestConvert<int?, byte?>(10, useInterpreter));
            Assert.Equal((byte?)10, TestConvert<uint?, byte?>(10, useInterpreter));
            Assert.Equal((byte?)10, TestConvert<long?, byte?>(10, useInterpreter));
            Assert.Equal((byte?)10, TestConvert<ulong?, byte?>(10, useInterpreter));
            Assert.Equal((byte?)10, TestConvert<float?, byte?>(10.0f, useInterpreter));
            Assert.Equal((byte?)10, TestConvert<float?, byte?>(10.0f, useInterpreter));
            Assert.Equal((byte?)10, TestConvert<double?, byte?>(10.0, useInterpreter));
            Assert.Equal((byte?)10, TestConvert<double?, byte?>(10.0, useInterpreter));
            Assert.Equal((byte?)10, TestConvert<decimal?, byte?>(10m, useInterpreter));
            Assert.Equal((byte?)10, TestConvert<decimal?, byte?>(10m, useInterpreter));

            Assert.Equal((short?)10, TestConvert<byte?, short?>(10, useInterpreter));
            Assert.Equal((short?)10, TestConvert<sbyte?, short?>(10, useInterpreter));
            Assert.Equal((short?)10, TestConvert<short?, short?>(10, useInterpreter));
            Assert.Equal((short?)10, TestConvert<ushort?, short?>(10, useInterpreter));
            Assert.Equal((short?)10, TestConvert<int?, short?>(10, useInterpreter));
            Assert.Equal((short?)10, TestConvert<uint?, short?>(10, useInterpreter));
            Assert.Equal((short?)10, TestConvert<long?, short?>(10, useInterpreter));
            Assert.Equal((short?)10, TestConvert<ulong?, short?>(10, useInterpreter));
            Assert.Equal((short?)10, TestConvert<float?, short?>(10.0f, useInterpreter));
            Assert.Equal((short?)10, TestConvert<double?, short?>(10.0, useInterpreter));
            Assert.Equal((short?)10, TestConvert<decimal?, short?>(10m, useInterpreter));

            Assert.Equal((int?)10, TestConvert<byte?, int?>(10, useInterpreter));
            Assert.Equal((int?)10, TestConvert<sbyte?, int?>(10, useInterpreter));
            Assert.Equal((int?)10, TestConvert<short?, int?>(10, useInterpreter));
            Assert.Equal((int?)10, TestConvert<ushort?, int?>(10, useInterpreter));
            Assert.Equal((int?)10, TestConvert<int?, int?>(10, useInterpreter));
            Assert.Equal((int?)10, TestConvert<uint?, int?>(10, useInterpreter));
            Assert.Equal((int?)10, TestConvert<long?, int?>(10, useInterpreter));
            Assert.Equal((int?)10, TestConvert<ulong?, int?>(10, useInterpreter));
            Assert.Equal((int?)10, TestConvert<float?, int?>(10.0f, useInterpreter));
            Assert.Equal((int?)10, TestConvert<double?, int?>(10.0, useInterpreter));
            Assert.Equal((int?)10, TestConvert<decimal?, int?>(10m, useInterpreter));

            Assert.Equal((long?)10, TestConvert<byte?, long?>(10, useInterpreter));
            Assert.Equal((long?)10, TestConvert<sbyte?, long?>(10, useInterpreter));
            Assert.Equal((long?)10, TestConvert<short?, long?>(10, useInterpreter));
            Assert.Equal((long?)10, TestConvert<ushort?, long?>(10, useInterpreter));
            Assert.Equal((long?)10, TestConvert<int?, long?>(10, useInterpreter));
            Assert.Equal((long?)10, TestConvert<uint?, long?>(10, useInterpreter));
            Assert.Equal((long?)10, TestConvert<long?, long?>(10, useInterpreter));
            Assert.Equal((long?)10, TestConvert<ulong?, long?>(10, useInterpreter));
            Assert.Equal((long?)10, TestConvert<float?, long?>(10.0f, useInterpreter));
            Assert.Equal((long?)10, TestConvert<double?, long?>(10.0, useInterpreter));
            Assert.Equal((long?)10, TestConvert<decimal?, long?>(10m, useInterpreter));

            Assert.Equal((double?)10, TestConvert<byte?, double?>(10, useInterpreter));
            Assert.Equal((double?)10, TestConvert<sbyte?, double?>(10, useInterpreter));
            Assert.Equal((double?)10, TestConvert<short?, double?>(10, useInterpreter));
            Assert.Equal((double?)10, TestConvert<ushort?, double?>(10, useInterpreter));
            Assert.Equal((double?)10, TestConvert<int?, double?>(10, useInterpreter));
            Assert.Equal((double?)10, TestConvert<uint?, double?>(10, useInterpreter));
            Assert.Equal((double?)10, TestConvert<long?, double?>(10, useInterpreter));
            Assert.Equal((double?)10, TestConvert<ulong?, double?>(10, useInterpreter));
            Assert.Equal((double?)10, TestConvert<float?, double?>(10.0f, useInterpreter));
            Assert.Equal((double?)10, TestConvert<double?, double?>(10.0, useInterpreter));
            Assert.Equal((double?)10, TestConvert<decimal?, double?>(10m, useInterpreter));

            Assert.Equal((decimal?)10, TestConvert<byte?, decimal?>(10, useInterpreter));
            Assert.Equal((decimal?)10, TestConvert<sbyte?, decimal?>(10, useInterpreter));
            Assert.Equal((decimal?)10, TestConvert<short?, decimal?>(10, useInterpreter));
            Assert.Equal((decimal?)10, TestConvert<ushort?, decimal?>(10, useInterpreter));
            Assert.Equal((decimal?)10, TestConvert<int?, decimal?>(10, useInterpreter));
            Assert.Equal((decimal?)10, TestConvert<uint?, decimal?>(10, useInterpreter));
            Assert.Equal((decimal?)10, TestConvert<long?, decimal?>(10, useInterpreter));
            Assert.Equal((decimal?)10, TestConvert<ulong?, decimal?>(10, useInterpreter));
            Assert.Equal((decimal?)10, TestConvert<float?, decimal?>(10.0f, useInterpreter));
            Assert.Equal((decimal?)10, TestConvert<double?, decimal?>(10.0, useInterpreter));
            Assert.Equal((decimal?)10, TestConvert<decimal?, decimal?>(10m, useInterpreter));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void ConvertMinMax(bool useInterpreter)
        {
            unchecked
            {
                Assert.Equal((float)uint.MaxValue, TestConvert<uint, float>(uint.MaxValue, useInterpreter));
                Assert.Equal((double)uint.MaxValue, TestConvert<uint, double>(uint.MaxValue, useInterpreter));
                Assert.Equal((float?)uint.MaxValue, TestConvert<uint, float?>(uint.MaxValue, useInterpreter));
                Assert.Equal((double?)uint.MaxValue, TestConvert<uint, double?>(uint.MaxValue, useInterpreter));

                Assert.Equal((float)ulong.MaxValue, TestConvert<ulong, float>(ulong.MaxValue, useInterpreter));
                Assert.Equal((double)ulong.MaxValue, TestConvert<ulong, double>(ulong.MaxValue, useInterpreter));
                Assert.Equal((float?)ulong.MaxValue, TestConvert<ulong, float?>(ulong.MaxValue, useInterpreter));
                Assert.Equal((double?)ulong.MaxValue, TestConvert<ulong, double?>(ulong.MaxValue, useInterpreter));

                /*
                 * needs more thought about what should happen.. these have undefined runtime behavior.
                 * results depend on whether values are in registers or locals, debug or retail etc.
                 *
                float fmin = float.MinValue;
                float fmax = float.MaxValue;
                double dmin = double.MinValue;
                double dmax = double.MaxValue;

                Assert.AreEqual((uint)fmin, TestConvert<float, uint>(fmin, useInterpreter));
                Assert.AreEqual((ulong)fmax, TestConvert<float, ulong>(fmax, useInterpreter));
                Assert.AreEqual((uint?)fmin, TestConvert<float, uint?>(fmin, useInterpreter));
                Assert.AreEqual((ulong?)fmax, TestConvert<float, ulong?>(fmax, useInterpreter));

                Assert.AreEqual((uint)dmin, TestConvert<double, uint>(dmin, useInterpreter));
                Assert.AreEqual((ulong)dmax, TestConvert<double, ulong>(dmax, useInterpreter));
                Assert.AreEqual((uint?)dmin, TestConvert<double, uint?>(dmin, useInterpreter));
                Assert.AreEqual((ulong?)dmax, TestConvert<double, ulong?>(dmax, useInterpreter));
                 */

                Assert.Equal((float)(uint?)uint.MaxValue, TestConvert<uint?, float>(uint.MaxValue, useInterpreter));
                Assert.Equal((double)(uint?)uint.MaxValue, TestConvert<uint?, double>(uint.MaxValue, useInterpreter));
                Assert.Equal((float?)(uint?)uint.MaxValue, TestConvert<uint?, float?>(uint.MaxValue, useInterpreter));
                Assert.Equal((double?)(uint?)uint.MaxValue, TestConvert<uint?, double?>(uint.MaxValue, useInterpreter));

                Assert.Equal((float)(ulong?)ulong.MaxValue, TestConvert<ulong?, float>(ulong.MaxValue, useInterpreter));
                Assert.Equal((double)(ulong?)ulong.MaxValue, TestConvert<ulong?, double>(ulong.MaxValue, useInterpreter));
                Assert.Equal((float?)(ulong?)ulong.MaxValue, TestConvert<ulong?, float?>(ulong.MaxValue, useInterpreter));
                Assert.Equal((double?)(ulong?)ulong.MaxValue, TestConvert<ulong?, double?>(ulong.MaxValue, useInterpreter));
            }
        }

        private static S TestConvert<T, S>(T value, bool useInterpreter)
        {
            Func<S> d = Expression.Lambda<Func<S>>(Expression.Convert(Expression.Constant(value, typeof(T)), typeof(S))).Compile(useInterpreter);
            return d();
        }

        private static S TestConvertChecked<T, S>(T value, bool useInterpreter)
        {
            Func<S> d = Expression.Lambda<Func<S>>(Expression.ConvertChecked(Expression.Constant(value, typeof(T)), typeof(S))).Compile(useInterpreter);
            return d();
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void ConvertNullToInt(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() =>
            {
                Expression<Func<ValueType, int>> e = v => (int)v;
                Func<ValueType, int> f = e.Compile(useInterpreter);
                f(null);
            });
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void ShiftWithMismatchedNulls(bool useInterpreter)
        {
            Expression<Func<byte?, int, int?>> e = (byte? b, int i) => (byte?)(b << i);
            Func<byte?, int, int?> f = e.Compile(useInterpreter);
            Assert.Equal(20, f(5, 2));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CoalesceChars(bool useInterpreter)
        {
            ParameterExpression x = Expression.Parameter(typeof(char?), "x");
            ParameterExpression y = Expression.Parameter(typeof(char?), "y");
            Expression<Func<char?, char?, char?>> e =
                Expression.Lambda<Func<char?, char?, char?>>(
                    Expression.Coalesce(x, y),
                    new ParameterExpression[] { x, y });
            Func<char?, char?, char?> f = e.Compile(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void ConvertToChar(bool useInterpreter)
        {
            Func<char> f = Expression.Lambda<Func<Char>>(Expression.Convert(Expression.Constant((byte)65), typeof(char))).Compile(useInterpreter);
            Assert.Equal('A', f());

            Func<char> f2 = Expression.Lambda<Func<Char>>(Expression.Convert(Expression.Constant(65), typeof(char))).Compile(useInterpreter);
            Assert.Equal('A', f2());

            Func<char> f3 = Expression.Lambda<Func<Char>>(Expression.Convert(Expression.Constant(-1), typeof(char))).Compile(useInterpreter);
            char c3 = f3();
            Func<int> f4 = Expression.Lambda<Func<int>>(Expression.Convert(Expression.Constant(c3), typeof(int))).Compile(useInterpreter);
            Assert.Equal(ushort.MaxValue, f4());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void MixedTypeNullableOps(bool useInterpreter)
        {
            Expression<Func<decimal, int?, decimal?>> e = (d, i) => d + i;
            Func<decimal, int?, decimal?> f = e.Compile(useInterpreter);
            decimal? result = f(1.0m, 4);
            Debug.WriteLine(result);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void NullGuidConstant(bool useInterpreter)
        {
            Expression<Func<Guid?, bool>> f2 = g2 => g2 != null;
            Func<Guid?, bool> d2 = f2.Compile(useInterpreter);
            Assert.True(d2(Guid.NewGuid()));
            Assert.False(d2(null));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void AddNullConstants(bool useInterpreter)
        {
            Expression<Func<int?>> f = Expression.Lambda<Func<int?>>(
                Expression.Add(
                    Expression.Constant(null, typeof(int?)),
                    Expression.Constant(1, typeof(int?))
                    ));

            int? result = f.Compile(useInterpreter)();
            Assert.False(result.HasValue);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CallWithRefParam(bool useInterpreter)
        {
            Expression<Func<int, int>> f = x => x + MethodWithRefParam(ref x) + x;
            Func<int, int> d = f.Compile(useInterpreter);
            Assert.Equal(113, d(10));
        }

        public static int MethodWithRefParam(ref int x)
        {
            x = 3;
            return 100;
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CallWithOutParam(bool useInterpreter)
        {
            Expression<Func<int, int>> f = x => x + MethodWithOutParam(out x) + x;
            Func<int, int> d = f.Compile(useInterpreter);
            Assert.Equal(113, d(10));
        }

        public static int MethodWithOutParam(out int x)
        {
            x = 3;
            return 100;
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void NewArrayInvoke(bool useInterpreter)
        {
            Expression<Func<int, string[]>> linq1 = (a => new string[a]);
            InvocationExpression linq1a = Expression.Invoke(linq1, new Expression[] { Expression.Constant(3) });
            Expression<Func<string[]>> linq1b = Expression.Lambda<Func<string[]>>(linq1a, new ParameterExpression[] { });
            Func<string[]> f = linq1b.Compile(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void LiftedAddDateTimeTimeSpan(bool useInterpreter)
        {
            Expression<Func<DateTime?, TimeSpan, DateTime?>> f = (x, y) => x + y;
            Assert.Equal(ExpressionType.Add, f.Body.NodeType);
            Debug.WriteLine(f);
            Func<DateTime?, TimeSpan, DateTime?> d = f.Compile(useInterpreter);
            DateTime? dt = DateTime.Now;
            TimeSpan ts = new TimeSpan(3, 2, 1);
            DateTime? dt2 = dt + ts;
            Assert.Equal(dt2, d(dt, ts));
            Assert.Null(d(null, ts));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void LiftedAddDateTimeTimeSpan2(bool useInterpreter)
        {
            Expression<Func<DateTime?, TimeSpan?, DateTime?>> f = (x, y) => x + y;
            Assert.Equal(ExpressionType.Add, f.Body.NodeType);
            Debug.WriteLine(f);
            Func<DateTime?, TimeSpan?, DateTime?> d = f.Compile(useInterpreter);
            DateTime? dt = DateTime.Now;
            TimeSpan? ts = new TimeSpan(3, 2, 1);
            DateTime? dt2 = dt + ts;
            Assert.Equal(dt2, d(dt, ts));
            Assert.Null(d(null, ts));
            Assert.Null(d(dt, null));
            Assert.Null(d(null, null));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void LiftedSubDateTime(bool useInterpreter)
        {
            Expression<Func<DateTime?, DateTime?, TimeSpan?>> f = (x, y) => x - y;
            Assert.Equal(ExpressionType.Subtract, f.Body.NodeType);
            Debug.WriteLine(f);
            Func<DateTime?, DateTime?, TimeSpan?> d = f.Compile(useInterpreter);
            DateTime? dt1 = DateTime.Now;
            DateTime? dt2 = new DateTime(2006, 5, 1);
            TimeSpan? ts = dt1 - dt2;
            Assert.Equal(ts, d(dt1, dt2));
            Assert.Null(d(null, dt2));
            Assert.Null(d(dt1, null));
            Assert.Null(d(null, null));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void LiftedEqualDateTime(bool useInterpreter)
        {
            Expression<Func<DateTime?, DateTime?, bool>> f = (x, y) => x == y;
            Assert.Equal(ExpressionType.Equal, f.Body.NodeType);
            Debug.WriteLine(f);
            Func<DateTime?, DateTime?, bool> d = f.Compile(useInterpreter);
            DateTime? dt1 = DateTime.Now;
            DateTime? dt2 = new DateTime(2006, 5, 1);
            Assert.True(d(dt1, dt1));
            Assert.False(d(dt1, dt2));
            Assert.False(d(null, dt2));
            Assert.False(d(dt1, null));
            Assert.True(d(null, null));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void LiftedNotEqualDateTime(bool useInterpreter)
        {
            Expression<Func<DateTime?, DateTime?, bool>> f = (x, y) => x != y;
            Assert.Equal(ExpressionType.NotEqual, f.Body.NodeType);
            Debug.WriteLine(f);
            Func<DateTime?, DateTime?, bool> d = f.Compile(useInterpreter);
            DateTime? dt1 = DateTime.Now;
            DateTime? dt2 = new DateTime(2006, 5, 1);
            Assert.False(d(dt1, dt1));
            Assert.True(d(dt1, dt2));
            Assert.True(d(null, dt2));
            Assert.True(d(dt1, null));
            Assert.False(d(null, null));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void LiftedLessThanDateTime(bool useInterpreter)
        {
            Expression<Func<DateTime?, DateTime?, bool>> f = (x, y) => x < y;
            Assert.Equal(ExpressionType.LessThan, f.Body.NodeType);
            Debug.WriteLine(f);
            Func<DateTime?, DateTime?, bool> d = f.Compile(useInterpreter);
            DateTime? dt1 = DateTime.Now;
            DateTime? dt2 = new DateTime(2006, 5, 1);
            Assert.False(d(dt1, dt1));
            Assert.True(d(dt2, dt1));
            Assert.False(d(null, dt2));
            Assert.False(d(dt1, null));
            Assert.False(d(null, null));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void LessThanDateTime(bool useInterpreter)
        {
            Expression<Func<DateTime, DateTime, bool>> f = (x, y) => x < y;
            Assert.Equal(ExpressionType.LessThan, f.Body.NodeType);
            Debug.WriteLine(f);
            Func<DateTime, DateTime, bool> d = f.Compile(useInterpreter);
            DateTime dt1 = DateTime.Now;
            DateTime dt2 = new DateTime(2006, 5, 1);
            Assert.False(d(dt1, dt1));
            Assert.True(d(dt2, dt1));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void InvokeLambda(bool useInterpreter)
        {
            Expression<Func<int, int>> f = x => x + 1;
            InvocationExpression ie = Expression.Invoke(f, Expression.Constant(5));
            Expression<Func<int>> lambda = Expression.Lambda<Func<int>>(ie);
            Func<int> d = lambda.Compile(useInterpreter);
            Assert.Equal(6, d());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CallCompiledLambda(bool useInterpreter)
        {
            Expression<Func<int, int>> f = x => x + 1;
            Func<int, int> compiled = f.Compile(useInterpreter);
            Expression<Func<int>> lambda = () => compiled(5);
            Func<int> d = lambda.Compile(useInterpreter);
            Assert.Equal(6, d());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CallCompiledLambdaWithTypeMissing(bool useInterpreter)
        {
            Expression<Func<object, bool>> f = x => x == Type.Missing;
            Func<object, bool> compiled = f.Compile(useInterpreter);
            Expression<Func<object, bool>> lambda = x => compiled(x);
            Func<object, bool> d = lambda.Compile(useInterpreter);
            Assert.True(d(Type.Missing));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void InvokeQuotedLambda(bool useInterpreter)
        {
            Expression<Func<int, int>> f = x => x + 1;
            InvocationExpression ie = Expression.Invoke(Expression.Quote(f), Expression.Constant(5));
            Expression<Func<int>> lambda = Expression.Lambda<Func<int>>(ie);
            Func<int> d = lambda.Compile(useInterpreter);
            Assert.Equal(6, d());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void InvokeComputedDelegate(bool useInterpreter)
        {
            ParameterExpression x = Expression.Parameter(typeof(int), "x");
            ParameterExpression y = Expression.Parameter(typeof(int), "y");
            Expression call = Expression.Call(null, typeof(Compiler_Tests).GetMethod("ComputeDelegate", BindingFlags.Static | BindingFlags.Public), new Expression[] { y });
            InvocationExpression ie = Expression.Invoke(call, x);
            Expression<Func<int, int, int>> lambda = Expression.Lambda<Func<int, int, int>>(ie, x, y);

            Func<int, int, int> d = lambda.Compile(useInterpreter);
            Assert.Equal(14, d(5, 9));
            Assert.Equal(40, d(5, 8));
        }

        public static Func<int, int> ComputeDelegate(int y)
        {
            if ((y & 1) != 0)
                return x => x + y;
            else
                return x => x * y;
        }

        [Fact]
        public static void InvokeNonTypedLambdaFails()
        {
            Expression call = Expression.Call(null, typeof(Compiler_Tests).GetMethod("ComputeDynamicLambda", BindingFlags.Static | BindingFlags.Public), new Expression[] { });
            AssertExtensions.Throws<ArgumentException>("expression", () => Expression.Invoke(call, null));
        }

        public static LambdaExpression ComputeDynamicLambda()
        {
            return null;
        }

        [Fact]
        public static void InvokeNonTypedDelegateFails()
        {
            Expression call = Expression.Call(null, typeof(Compiler_Tests).GetMethod("ComputeDynamicDelegate", BindingFlags.Static | BindingFlags.Public), new Expression[] { });
            AssertExtensions.Throws<ArgumentException>("expression", () => Expression.Invoke(call, null));
        }

        public static Delegate ComputeDynamicDelegate()
        {
            return null;
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void NestedQuotedLambdas(bool useInterpreter)
        {
            Expression<Func<int, Expression<Func<int, int>>>> f = a => b => a + b;
            Func<int, Expression<Func<int, int>>> d = f.Compile(useInterpreter);
            Expression<Func<int, int>> f2 = d(3);
            Func<int, int> d2 = f2.Compile(useInterpreter);
            int v = d2(4);
            Assert.Equal(7, v);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void StaticMethodCall(bool useInterpreter)
        {
            Expression<Func<int, int, int>> f = (a, b) => Math.Max(a, b);
            Func<int, int, int> d = f.Compile(useInterpreter);
            Assert.Equal(4, d(3, 4));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CallOnCapturedInstance(bool useInterpreter)
        {
            Foo foo = new Foo();
            Expression<Func<int, int>> f = (a) => foo.Zip(a);
            Func<int, int> d = f.Compile(useInterpreter);
            Assert.Equal(225, d(15));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void VirtualCall(bool useInterpreter)
        {
            Foo bar = new Bar();
            Expression<Func<Foo, string>> f = foo => foo.Virt();
            Func<Foo, string> d = f.Compile(useInterpreter);
            Assert.Equal("Bar", d(bar));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void NestedLambda(bool useInterpreter)
        {
            Expression<Func<int, int>> f = (a) => M1(a, (b) => b * b);
            Func<int, int> d = f.Compile(useInterpreter);
            Assert.Equal(100, d(10));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void NestedLambdaWithOuterArg(bool useInterpreter)
        {
            Expression<Func<int, int>> f = (a) => M1(a + a, (b) => b * a);
            Func<int, int> d = f.Compile(useInterpreter);
            Assert.Equal(200, d(10));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void NestedExpressionLambda(bool useInterpreter)
        {
            Expression<Func<int, int>> f = (a) => M2(a, (b) => b * b);
            Func<int, int> d = f.Compile(useInterpreter);
            Assert.Equal(10, d(10));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void NestedExpressionLambdaWithOuterArg(bool useInterpreter)
        {
            Expression<Func<int, int>> f = (a) => M2(a, (b) => b * a);
            Func<int, int> d = f.Compile(useInterpreter);
            Assert.Equal(99, d(99));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void ArrayInitializedWithLiterals(bool useInterpreter)
        {
            Expression<Func<int[]>> f = () => new int[] { 1, 2, 3, 4, 5 };
            Func<int[]> d = f.Compile(useInterpreter);
            int[] v = d();
            Assert.Equal(5, v.Length);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void ArrayInitializedWithCapturedInstance(bool useInterpreter)
        {
            Foo foo = new Foo();
            Expression<Func<Foo[]>> f = () => new Foo[] { foo };
            Func<Foo[]> d = f.Compile(useInterpreter);
            Foo[] v = d();
            Assert.Equal(1, v.Length);
            Assert.Equal(foo, v[0]);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void NullableAddition(bool useInterpreter)
        {
            Expression<Func<double?, double?>> f = (v) => v + v;
            Func<double?, double?> d = f.Compile(useInterpreter);
            Assert.Equal(20.0, d(10.0));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void NullableComparedToLiteral(bool useInterpreter)
        {
            Expression<Func<int?, bool>> f = (v) => v > 10;
            Func<int?, bool> d = f.Compile(useInterpreter);
            Assert.True(d(12));
            Assert.False(d(5));
            Assert.True(d(int.MaxValue));
            Assert.False(d(int.MinValue));
            Assert.False(d(null));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void NullableModuloLiteral(bool useInterpreter)
        {
            Expression<Func<double?, double?>> f = (v) => v % 10;
            Func<double?, double?> d = f.Compile(useInterpreter);
            Assert.Equal(5.0, d(15.0));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void ArrayIndexer(bool useInterpreter)
        {
            Expression<Func<int[], int, int>> f = (v, i) => v[i];
            Func<int[], int, int> d = f.Compile(useInterpreter);
            int[] ints = new[] { 1, 2, 3 };
            Assert.Equal(3, d(ints, 2));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void ConvertToNullableDouble(bool useInterpreter)
        {
            Expression<Func<int?, double?>> f = (v) => (double?)v;
            Func<int?, double?> d = f.Compile(useInterpreter);
            Assert.Equal(10.0, d(10));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void UnboxToInt(bool useInterpreter)
        {
            Expression<Func<object, int>> f = (a) => (int)a;
            Func<object, int> d = f.Compile(useInterpreter);
            Assert.Equal(5, d(5));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void TypeIs(bool useInterpreter)
        {
            Expression<Func<Foo, bool>> f = x => x is Foo;
            Func<Foo, bool> d = f.Compile(useInterpreter);
            Assert.True(d(new Foo()));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void TypeAs(bool useInterpreter)
        {
            Expression<Func<Foo, Bar>> f = x => x as Bar;
            Func<Foo, Bar> d = f.Compile(useInterpreter);
            Assert.Null(d(new Foo()));
            Assert.NotNull(d(new Bar()));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void Coalesce(bool useInterpreter)
        {
            Expression<Func<int?, int>> f = x => x ?? 5;
            Func<int?, int> d = f.Compile(useInterpreter);
            Assert.Equal(5, d(null));
            Assert.Equal(2, d(2));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CoalesceRefTypes(bool useInterpreter)
        {
            Expression<Func<string, string>> f = x => x ?? "nil";
            Func<string, string> d = f.Compile(useInterpreter);
            Assert.Equal("nil", d(null));
            Assert.Equal("Not Nil", d("Not Nil"));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void MultiDimensionalArrayAccess(bool useInterpreter)
        {
            Expression<Func<int, int, int[,], int>> f = (x, y, a) => a[x, y];
            Func<int, int, int[,], int> d = f.Compile(useInterpreter);
            int[,] array = new int[2, 2] { { 0, 1 }, { 2, 3 } };
            Assert.Equal(3, d(1, 1, array));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void NewClassWithMemberInitializer(bool useInterpreter)
        {
            Expression<Func<int, ClassX>> f = v => new ClassX { A = v };
            Func<int, ClassX> d = f.Compile(useInterpreter);
            Assert.Equal(5, d(5).A);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void NewStructWithArgs(bool useInterpreter)
        {
            Expression<Func<int, StructZ>> f = v => new StructZ(v);
            Func<int, StructZ> d = f.Compile(useInterpreter);
            Assert.Equal(5, d(5).A);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void NewStructWithArgsAndMemberInitializer(bool useInterpreter)
        {
            Expression<Func<int, StructZ>> f = v => new StructZ(v) { A = v + 1 };
            Func<int, StructZ> d = f.Compile(useInterpreter);
            Assert.Equal(6, d(5).A);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void NewClassWithMemberInitializers(bool useInterpreter)
        {
            Expression<Func<int, ClassX>> f = v => new ClassX { A = v, B = v };
            Func<int, ClassX> d = f.Compile(useInterpreter);
            Assert.Equal(5, d(5).A);
            Assert.Equal(7, d(7).B);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void NewStructWithMemberInitializer(bool useInterpreter)
        {
            Expression<Func<int, StructX>> f = v => new StructX { A = v };
            Func<int, StructX> d = f.Compile(useInterpreter);
            Assert.Equal(5, d(5).A);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void NewStructWithMemberInitializers(bool useInterpreter)
        {
            Expression<Func<int, StructX>> f = v => new StructX { A = v, B = v };
            Func<int, StructX> d = f.Compile(useInterpreter);
            Assert.Equal(5, d(5).A);
            Assert.Equal(7, d(7).B);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void ListInitializer(bool useInterpreter)
        {
            Expression<Func<int, List<ClassY>>> f = x => new List<ClassY> { new ClassY { B = x } };
            Func<int, List<ClassY>> d = f.Compile(useInterpreter);
            List<ClassY> list = d(5);
            Assert.Equal(1, list.Count);
            Assert.Equal(5, list[0].B);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void ListInitializerLong(bool useInterpreter)
        {
            Expression<Func<int, List<ClassY>>> f = x => new List<ClassY> { new ClassY { B = x }, new ClassY { B = x + 1 }, new ClassY { B = x + 2 } };
            Func<int, List<ClassY>> d = f.Compile(useInterpreter);
            List<ClassY> list = d(5);
            Assert.Equal(3, list.Count);
            Assert.Equal(5, list[0].B);
            Assert.Equal(6, list[1].B);
            Assert.Equal(7, list[2].B);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void ListInitializerInferred(bool useInterpreter)
        {
            Expression<Func<int, List<ClassY>>> f = x => new List<ClassY> { new ClassY { B = x }, new ClassY { B = x + 1 }, new ClassY { B = x + 2 } };
            Func<int, List<ClassY>> d = f.Compile(useInterpreter);
            List<ClassY> list = d(5);
            Assert.Equal(3, list.Count);
            Assert.Equal(5, list[0].B);
            Assert.Equal(6, list[1].B);
            Assert.Equal(7, list[2].B);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void NewClassWithMemberListInitializer(bool useInterpreter)
        {
            Expression<Func<int, ClassX>> f =
                v => new ClassX { A = v, B = v + 1, Ys = { new ClassY { B = v + 2 } } };
            Func<int, ClassX> d = f.Compile(useInterpreter);
            ClassX x = d(5);
            Assert.Equal(5, x.A);
            Assert.Equal(6, x.B);
            Assert.Equal(1, x.Ys.Count);
            Assert.Equal(7, x.Ys[0].B);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void NewClassWithMemberListOfStructInitializer(bool useInterpreter)
        {
            Expression<Func<int, ClassX>> f =
                v => new ClassX { A = v, B = v + 1, SYs = { new StructY { B = v + 2 } } };
            Func<int, ClassX> d = f.Compile(useInterpreter);
            ClassX x = d(5);
            Assert.Equal(5, x.A);
            Assert.Equal(6, x.B);
            Assert.Equal(1, x.SYs.Count);
            Assert.Equal(7, x.SYs[0].B);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void NewClassWithMemberMemberInitializer(bool useInterpreter)
        {
            Expression<Func<int, ClassX>> f =
                v => new ClassX { A = v, B = v + 1, Y = { B = v + 2 } };
            Func<int, ClassX> d = f.Compile(useInterpreter);
            ClassX x = d(5);
            Assert.Equal(5, x.A);
            Assert.Equal(6, x.B);
            Assert.Equal(7, x.Y.B);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void NewStructWithMemberListInitializer(bool useInterpreter)
        {
            Expression<Func<int, StructX>> f =
                v => new StructX { A = v, B = v + 1, Ys = { new ClassY { B = v + 2 } } };
            Func<int, StructX> d = f.Compile(useInterpreter);
            StructX x = d(5);
            Assert.Equal(5, x.A);
            Assert.Equal(6, x.B);
            Assert.Equal(1, x.Ys.Count);
            Assert.Equal(7, x.Ys[0].B);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void NewStructWithStructMemberMemberInitializer(bool useInterpreter)
        {
            Expression<Func<int, StructX>> f =
                v => new StructX { A = v, B = v + 1, SY = new StructY { B = v + 2 } };
            Func<int, StructX> d = f.Compile(useInterpreter);
            StructX x = d(5);
            Assert.Equal(5, x.A);
            Assert.Equal(6, x.B);
            Assert.Equal(7, x.SY.B);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void StructStructMemberInitializationThroughPropertyThrowsException(bool useInterpreter)
        {
            Expression<Func<int, StructX>> f = GetExpressionTreeForMemberInitializationThroughProperty<StructX>();
            Assert.Throws<InvalidOperationException>(() => f.Compile(useInterpreter));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void ClassStructMemberInitializationThroughPropertyThrowsException(bool useInterpreter)
        {
            Expression<Func<int, ClassX>> f = GetExpressionTreeForMemberInitializationThroughProperty<ClassX>();
            Assert.Throws<InvalidOperationException>(() => f.Compile(useInterpreter));
        }

        private static Expression<Func<int, T>> GetExpressionTreeForMemberInitializationThroughProperty<T>()
        {
            // Generate the expression:
            //   v => new T { A = v, B = v + 1, SYP = { B = v + 2 } };
            ParameterExpression parameterV = Expression.Parameter(typeof(int), "v");
            return
                Expression.Lambda<Func<int, T>>(
                    // new T { A = v, B= v + 1, SYP = { B = v + 2 } }
                    Expression.MemberInit(
                        // new T
                        Expression.New(typeof(T).GetConstructor(new Type[0])),

                        // { A = v, B= v + 1, SYP = { B = v + 2 } };
                        new MemberBinding[] {
                            // A = v
                            Expression.Bind(typeof(T).GetField("A"), parameterV),

                            // B = v + 1
                            Expression.Bind(typeof(T).GetField("B"), Expression.Add(parameterV, Expression.Constant(1, typeof(int)))),

                            // SYP = { B = v + 2 }
                            Expression.MemberBind(
                                typeof(T).GetMethod("get_SYP"),
                                new MemberBinding[] {
                                    Expression.Bind(
                                        typeof(StructY).GetField("B"),
                                        Expression.Add(
                                            parameterV,
                                            Expression.Constant(2, typeof(int))
                                        )
                                    )
                                }
                            )
                        }
                    ),

                    // v =>
                    new ParameterExpression[] { parameterV }
                );
        }


        [Fact]
        public static void ShortCircuitAnd()
        {
            int[] values = new[] { 1, 2, 3, 4, 5 };

            IQueryable<int> q = from v in values.AsQueryable()
                    where v == 100 && BadJuju(v) == 10
                    select v;

            List<int> list = q.ToList();
            Assert.Equal(0, list.Count);
        }

        [Fact]
        public static void ShortCircuitOr()
        {
            int[] values = new[] { 1, 2, 3, 4, 5 };

            IQueryable<int> q = from v in values.AsQueryable()
                    where v != 100 || BadJuju(v) == 10
                    select v;

            List<int> list = q.ToList();
            Assert.Equal(values.Length, list.Count);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void UnaryOperators(bool useInterpreter)
        {
            // Not
            Assert.False(TestUnary<bool, bool>(ExpressionType.Not, true, useInterpreter));
            Assert.True(TestUnary<bool, bool>(ExpressionType.Not, false, useInterpreter));
            Assert.False((bool)TestUnary<bool?, bool?>(ExpressionType.Not, true, useInterpreter));
            Assert.True((bool)TestUnary<bool?, bool?>(ExpressionType.Not, false, useInterpreter));
            Assert.Null(TestUnary<bool?, bool?>(ExpressionType.Not, null, useInterpreter));
            Assert.Equal(~1, TestUnary<int, int>(ExpressionType.Not, 1, useInterpreter));
            Assert.Equal(~1, TestUnary<int?, int?>(ExpressionType.Not, 1, useInterpreter));
            Assert.Null(TestUnary<int?, int?>(ExpressionType.Not, null, useInterpreter));

            // Negate
            Assert.Equal(-1, TestUnary<int, int>(ExpressionType.Negate, 1, useInterpreter));
            Assert.Equal(-1, TestUnary<int?, int?>(ExpressionType.Negate, 1, useInterpreter));
            Assert.Null(TestUnary<int?, int?>(ExpressionType.Negate, null, useInterpreter));
            Assert.Equal(-1, TestUnary<int, int>(ExpressionType.NegateChecked, 1, useInterpreter));
            Assert.Equal(-1, TestUnary<int?, int?>(ExpressionType.NegateChecked, 1, useInterpreter));
            Assert.Null(TestUnary<int?, int?>(ExpressionType.NegateChecked, null, useInterpreter));

            Assert.Equal(-1, TestUnary<decimal, decimal>(ExpressionType.Negate, 1, useInterpreter));
            Assert.Equal(-1, TestUnary<decimal?, decimal?>(ExpressionType.Negate, 1, useInterpreter));
            Assert.Null(TestUnary<decimal?, decimal?>(ExpressionType.Negate, null, useInterpreter));
            Assert.Equal(-1, TestUnary<decimal, decimal>(ExpressionType.NegateChecked, 1, useInterpreter));
            Assert.Equal(-1, TestUnary<decimal?, decimal?>(ExpressionType.NegateChecked, 1, useInterpreter));
            Assert.Null(TestUnary<decimal?, decimal?>(ExpressionType.NegateChecked, null, useInterpreter));
        }

        private static R TestUnary<T, R>(Expression<Func<T, R>> f, T v, bool useInterpreter)
        {
            Func<T, R> d = f.Compile(useInterpreter);
            R rv = d(v);
            return rv;
        }

        private static R TestUnary<T, R>(ExpressionType op, T v, bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(T), "v");
            Expression<Func<T, R>> f = Expression.Lambda<Func<T, R>>(Expression.MakeUnary(op, p, null), p);
            return TestUnary(f, v, useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void ShiftULong(bool useInterpreter)
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                Expression<Func<ulong>> e =
                  Expression.Lambda<Func<ulong>>(
                    Expression.RightShift(
                        Expression.Constant((ulong)5, typeof(ulong)),
                        Expression.Constant((ulong)1, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
                Func<ulong> f = e.Compile(useInterpreter);
                f();
            });
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void MultiplyMinInt(bool useInterpreter)
        {
            Assert.Throws<OverflowException>(() =>
            {
                Func<long> f = Expression.Lambda<Func<long>>(
                  Expression.MultiplyChecked(
                    Expression.Constant((long)-1, typeof(long)),
                    Expression.Constant(long.MinValue, typeof(long))),
                    Enumerable.Empty<ParameterExpression>()
                    ).Compile(useInterpreter);
                f();
            });
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void MultiplyMinInt2(bool useInterpreter)
        {
            Assert.Throws<OverflowException>(() =>
            {
                Func<long> f = Expression.Lambda<Func<long>>(
                  Expression.MultiplyChecked(
                    Expression.Constant(long.MinValue, typeof(long)),
                    Expression.Constant((long)-1, typeof(long))),
                  Enumerable.Empty<ParameterExpression>()).Compile(useInterpreter);
                f();
            });
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void ConvertSignedToUnsigned(bool useInterpreter)
        {
            Func<ulong> f = Expression.Lambda<Func<ulong>>(Expression.Convert(Expression.Constant((sbyte)-1), typeof(ulong))).Compile(useInterpreter);
            Assert.Equal(ulong.MaxValue, f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void ConvertUnsignedToSigned(bool useInterpreter)
        {
            Func<sbyte> f = Expression.Lambda<Func<sbyte>>(Expression.Convert(Expression.Constant(UInt64.MaxValue), typeof(sbyte))).Compile(useInterpreter);
            Assert.Equal((sbyte)-1, f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedSignedToUnsigned(bool useInterpreter)
        {
            Func<ulong> f = Expression.Lambda<Func<ulong>>(Expression.ConvertChecked(Expression.Constant((sbyte)-1), typeof(ulong))).Compile(useInterpreter);
            Assert.Throws<OverflowException>(() => f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUnsignedToSigned(bool useInterpreter)
        {
            Func<sbyte> f = Expression.Lambda<Func<sbyte>>(Expression.ConvertChecked(Expression.Constant(UInt64.MaxValue), typeof(sbyte))).Compile(useInterpreter);
            Assert.Throws<OverflowException>(() => f());
        }

        static class System_Linq_Expressions_Expression_TDelegate__1
        {
            public static T Default<T>() { return default(T); }
            public static void UseSystem_Linq_Expressions_Expression_TDelegate__1(bool call) // call this passing false
            {
                if (call)
                {
                    Default<System.Linq.Expressions.Expression<System.Object>>().Compile();
                    Default<System.Linq.Expressions.Expression<System.Object>>().Update(
                Default<System.Linq.Expressions.Expression>(),
                Default<System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression>>());
                }
            }
        }

        [Fact]
        public static void ExprT_Update()
        {
            System_Linq_Expressions_Expression_TDelegate__1.UseSystem_Linq_Expressions_Expression_TDelegate__1(false);
        }

        public enum MyEnum
        {
            Value
        }

        public class EnumOutLambdaClass
        {
            public static void Bar(out MyEnum o)
            {
                o = MyEnum.Value;
            }

            public static void BarRef(ref MyEnum o)
            {
                o = MyEnum.Value;
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void UninitializedEnumOut(bool useInterpreter)
        {
            ParameterExpression x = Expression.Variable(typeof(MyEnum), "x");

            Expression<Action> expression = Expression.Lambda<Action>(
                            Expression.Block(
                            new[] { x },
                            Expression.Call(null, typeof(EnumOutLambdaClass).GetMethod("Bar"), x)));

            expression.Compile(useInterpreter)();
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void BinaryOperators(bool useInterpreter)
        {
            // AndAlso
            Assert.True(TestBinary<bool, bool>(ExpressionType.AndAlso, true, true, useInterpreter));
            Assert.False(TestBinary<bool, bool>(ExpressionType.AndAlso, false, true, useInterpreter));
            Assert.False(TestBinary<bool, bool>(ExpressionType.AndAlso, true, false, useInterpreter));
            Assert.False(TestBinary<bool, bool>(ExpressionType.AndAlso, false, false, useInterpreter));
            Assert.True((bool)TestBinary<bool?, bool?>(ExpressionType.AndAlso, true, true, useInterpreter));
            Assert.False((bool)TestBinary<bool?, bool?>(ExpressionType.AndAlso, true, false, useInterpreter));
            Assert.False((bool)TestBinary<bool?, bool?>(ExpressionType.AndAlso, false, true, useInterpreter));
            Assert.False((bool)TestBinary<bool?, bool?>(ExpressionType.AndAlso, false, false, useInterpreter));
            Assert.Null(TestBinary<bool?, bool?>(ExpressionType.AndAlso, true, null, useInterpreter));
            Assert.Null(TestBinary<bool?, bool?>(ExpressionType.AndAlso, null, true, useInterpreter));
            Assert.False((bool)TestBinary<bool?, bool?>(ExpressionType.AndAlso, false, null, useInterpreter));
            Assert.False((bool)TestBinary<bool?, bool?>(ExpressionType.AndAlso, null, false, useInterpreter));
            Assert.Null(TestBinary<bool?, bool?>(ExpressionType.AndAlso, null, null, useInterpreter));

            // OrElse
            Assert.True(TestBinary<bool, bool>(ExpressionType.OrElse, true, true, useInterpreter));
            Assert.True(TestBinary<bool, bool>(ExpressionType.OrElse, false, true, useInterpreter));
            Assert.True(TestBinary<bool, bool>(ExpressionType.OrElse, true, false, useInterpreter));
            Assert.False(TestBinary<bool, bool>(ExpressionType.OrElse, false, false, useInterpreter));
            Assert.True((bool)TestBinary<bool?, bool?>(ExpressionType.OrElse, true, true, useInterpreter));
            Assert.True((bool)TestBinary<bool?, bool?>(ExpressionType.OrElse, true, false, useInterpreter));
            Assert.True((bool)TestBinary<bool?, bool?>(ExpressionType.OrElse, false, true, useInterpreter));
            Assert.False((bool)TestBinary<bool?, bool?>(ExpressionType.OrElse, false, false, useInterpreter));
            Assert.True((bool)TestBinary<bool?, bool?>(ExpressionType.OrElse, true, null, useInterpreter));
            Assert.True((bool)TestBinary<bool?, bool?>(ExpressionType.OrElse, null, true, useInterpreter));
            Assert.Null(TestBinary<bool?, bool?>(ExpressionType.OrElse, false, null, useInterpreter));
            Assert.Null(TestBinary<bool?, bool?>(ExpressionType.OrElse, null, false, useInterpreter));
            Assert.Null(TestBinary<bool?, bool?>(ExpressionType.OrElse, null, null, useInterpreter));

            // And
            Assert.True(TestBinary<bool, bool>(ExpressionType.And, true, true, useInterpreter));
            Assert.False(TestBinary<bool, bool>(ExpressionType.And, false, true, useInterpreter));
            Assert.False(TestBinary<bool, bool>(ExpressionType.And, true, false, useInterpreter));
            Assert.False(TestBinary<bool, bool>(ExpressionType.And, false, false, useInterpreter));
            Assert.True((bool)TestBinary<bool?, bool?>(ExpressionType.And, true, true, useInterpreter));
            Assert.False((bool)TestBinary<bool?, bool?>(ExpressionType.And, true, false, useInterpreter));
            Assert.False((bool)TestBinary<bool?, bool?>(ExpressionType.And, false, true, useInterpreter));
            Assert.False((bool)TestBinary<bool?, bool?>(ExpressionType.And, false, false, useInterpreter));
            Assert.Null(TestBinary<bool?, bool?>(ExpressionType.And, true, null, useInterpreter));
            Assert.Null(TestBinary<bool?, bool?>(ExpressionType.And, null, true, useInterpreter));
            Assert.False((bool)TestBinary<bool?, bool?>(ExpressionType.And, false, null, useInterpreter));
            Assert.False((bool)TestBinary<bool?, bool?>(ExpressionType.And, null, false, useInterpreter));
            Assert.Null(TestBinary<bool?, bool?>(ExpressionType.And, null, null, useInterpreter));
            Assert.Equal(2, TestBinary<int, int>(ExpressionType.And, 2, 3, useInterpreter));
            Assert.Equal(2, TestBinary<int?, int?>(ExpressionType.And, 2, 3, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.And, null, 3, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.And, 2, null, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.And, null, null, useInterpreter));

            // Or
            Assert.True(TestBinary<bool, bool>(ExpressionType.Or, true, true, useInterpreter));
            Assert.True(TestBinary<bool, bool>(ExpressionType.Or, false, true, useInterpreter));
            Assert.True(TestBinary<bool, bool>(ExpressionType.Or, true, false, useInterpreter));
            Assert.False(TestBinary<bool, bool>(ExpressionType.Or, false, false, useInterpreter));
            Assert.True((bool)TestBinary<bool?, bool?>(ExpressionType.Or, true, true, useInterpreter));
            Assert.True((bool)TestBinary<bool?, bool?>(ExpressionType.Or, true, false, useInterpreter));
            Assert.True((bool)TestBinary<bool?, bool?>(ExpressionType.Or, false, true, useInterpreter));
            Assert.False((bool)TestBinary<bool?, bool?>(ExpressionType.Or, false, false, useInterpreter));
            Assert.True((bool)TestBinary<bool?, bool?>(ExpressionType.Or, true, null, useInterpreter));
            Assert.True((bool)TestBinary<bool?, bool?>(ExpressionType.Or, null, true, useInterpreter));
            Assert.Null(TestBinary<bool?, bool?>(ExpressionType.Or, false, null, useInterpreter));
            Assert.Null(TestBinary<bool?, bool?>(ExpressionType.Or, null, false, useInterpreter));
            Assert.Null(TestBinary<bool?, bool?>(ExpressionType.Or, null, null, useInterpreter));
            Assert.Equal(3, TestBinary<int, int>(ExpressionType.Or, 2, 1, useInterpreter));
            Assert.Equal(3, TestBinary<int?, int?>(ExpressionType.Or, 2, 1, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.Or, null, 1, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.Or, 2, null, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.Or, null, 1, useInterpreter));

            // ExclusiveOr
            Assert.False(TestBinary<bool, bool>(ExpressionType.ExclusiveOr, true, true, useInterpreter));
            Assert.True(TestBinary<bool, bool>(ExpressionType.ExclusiveOr, true, false, useInterpreter));
            Assert.True(TestBinary<bool, bool>(ExpressionType.ExclusiveOr, false, true, useInterpreter));
            Assert.False(TestBinary<bool, bool>(ExpressionType.ExclusiveOr, false, false, useInterpreter));
            Assert.False((bool)TestBinary<bool?, bool?>(ExpressionType.ExclusiveOr, true, true, useInterpreter));
            Assert.True((bool)TestBinary<bool?, bool?>(ExpressionType.ExclusiveOr, true, false, useInterpreter));
            Assert.True((bool)TestBinary<bool?, bool?>(ExpressionType.ExclusiveOr, false, true, useInterpreter));
            Assert.False((bool)TestBinary<bool?, bool?>(ExpressionType.ExclusiveOr, false, false, useInterpreter));
            Assert.Null(TestBinary<bool?, bool?>(ExpressionType.ExclusiveOr, true, null, useInterpreter));
            Assert.Null(TestBinary<bool?, bool?>(ExpressionType.ExclusiveOr, null, true, useInterpreter));
            Assert.Null(TestBinary<bool?, bool?>(ExpressionType.ExclusiveOr, false, null, useInterpreter));
            Assert.Null(TestBinary<bool?, bool?>(ExpressionType.ExclusiveOr, null, false, useInterpreter));
            Assert.Null(TestBinary<bool?, bool?>(ExpressionType.ExclusiveOr, null, null, useInterpreter));
            Assert.Equal(4, TestBinary<int, int>(ExpressionType.ExclusiveOr, 5, 1, useInterpreter));
            Assert.Equal(4, TestBinary<int?, int?>(ExpressionType.ExclusiveOr, 5, 1, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.ExclusiveOr, null, 1, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.ExclusiveOr, 5, null, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.ExclusiveOr, null, null, useInterpreter));

            // Equal
            Assert.False(TestBinary<int, bool>(ExpressionType.Equal, 1, 2, useInterpreter));
            Assert.True(TestBinary<int, bool>(ExpressionType.Equal, 1, 1, useInterpreter));
            Assert.False(TestBinary<int?, bool>(ExpressionType.Equal, 1, 2, useInterpreter));
            Assert.True(TestBinary<int?, bool>(ExpressionType.Equal, 1, 1, useInterpreter));
            Assert.False(TestBinary<int?, bool>(ExpressionType.Equal, null, 2, useInterpreter));
            Assert.False(TestBinary<int?, bool>(ExpressionType.Equal, 1, null, useInterpreter));
            Assert.True(TestBinary<int?, bool>(ExpressionType.Equal, null, null, useInterpreter));

            Assert.False(TestBinary<decimal, bool>(ExpressionType.Equal, 1, 2, useInterpreter));
            Assert.True(TestBinary<decimal, bool>(ExpressionType.Equal, 1, 1, useInterpreter));
            Assert.False(TestBinary<decimal?, bool>(ExpressionType.Equal, 1, 2, useInterpreter));
            Assert.True(TestBinary<decimal?, bool>(ExpressionType.Equal, 1, 1, useInterpreter));
            Assert.False(TestBinary<decimal?, bool>(ExpressionType.Equal, null, 2, useInterpreter));
            Assert.False(TestBinary<decimal?, bool>(ExpressionType.Equal, 1, null, useInterpreter));
            Assert.True(TestBinary<decimal?, bool>(ExpressionType.Equal, null, null, useInterpreter));

            // NotEqual
            Assert.True(TestBinary<int, bool>(ExpressionType.NotEqual, 1, 2, useInterpreter));
            Assert.False(TestBinary<int, bool>(ExpressionType.NotEqual, 1, 1, useInterpreter));
            Assert.True(TestBinary<int?, bool>(ExpressionType.NotEqual, 1, 2, useInterpreter));
            Assert.False(TestBinary<int?, bool>(ExpressionType.NotEqual, 1, 1, useInterpreter));
            Assert.True(TestBinary<int?, bool>(ExpressionType.NotEqual, null, 2, useInterpreter));
            Assert.True(TestBinary<int?, bool>(ExpressionType.NotEqual, 1, null, useInterpreter));
            Assert.False(TestBinary<int?, bool>(ExpressionType.NotEqual, null, null, useInterpreter));

            Assert.True(TestBinary<decimal, bool>(ExpressionType.NotEqual, 1, 2, useInterpreter));
            Assert.False(TestBinary<decimal, bool>(ExpressionType.NotEqual, 1, 1, useInterpreter));
            Assert.True(TestBinary<decimal?, bool>(ExpressionType.NotEqual, 1, 2, useInterpreter));
            Assert.False(TestBinary<decimal?, bool>(ExpressionType.NotEqual, 1, 1, useInterpreter));
            Assert.True(TestBinary<decimal?, bool>(ExpressionType.NotEqual, null, 2, useInterpreter));
            Assert.True(TestBinary<decimal?, bool>(ExpressionType.NotEqual, 1, null, useInterpreter));
            Assert.False(TestBinary<decimal?, bool>(ExpressionType.NotEqual, null, null, useInterpreter));

            // LessThan
            Assert.True(TestBinary<int, bool>(ExpressionType.LessThan, 1, 2, useInterpreter));
            Assert.False(TestBinary<int, bool>(ExpressionType.LessThan, 2, 1, useInterpreter));
            Assert.False(TestBinary<int, bool>(ExpressionType.LessThan, 2, 2, useInterpreter));
            Assert.True(TestBinary<int?, bool>(ExpressionType.LessThan, 1, 2, useInterpreter));
            Assert.False(TestBinary<int?, bool>(ExpressionType.LessThan, 2, 1, useInterpreter));
            Assert.False(TestBinary<int?, bool>(ExpressionType.LessThan, 2, 2, useInterpreter));
            Assert.False(TestBinary<int?, bool>(ExpressionType.LessThan, null, 2, useInterpreter));
            Assert.False(TestBinary<int?, bool>(ExpressionType.LessThan, 2, null, useInterpreter));
            Assert.False(TestBinary<int?, bool>(ExpressionType.LessThan, null, null, useInterpreter));

            Assert.True(TestBinary<decimal, bool>(ExpressionType.LessThan, 1, 2, useInterpreter));
            Assert.False(TestBinary<decimal, bool>(ExpressionType.LessThan, 2, 1, useInterpreter));
            Assert.False(TestBinary<decimal, bool>(ExpressionType.LessThan, 2, 2, useInterpreter));
            Assert.True(TestBinary<decimal?, bool>(ExpressionType.LessThan, 1, 2, useInterpreter));
            Assert.False(TestBinary<decimal?, bool>(ExpressionType.LessThan, 2, 1, useInterpreter));
            Assert.False(TestBinary<decimal?, bool>(ExpressionType.LessThan, 2, 2, useInterpreter));
            Assert.False(TestBinary<decimal?, bool>(ExpressionType.LessThan, null, 2, useInterpreter));
            Assert.False(TestBinary<decimal?, bool>(ExpressionType.LessThan, 2, null, useInterpreter));
            Assert.False(TestBinary<decimal?, bool>(ExpressionType.LessThan, null, null, useInterpreter));

            // LessThanOrEqual
            Assert.True(TestBinary<int, bool>(ExpressionType.LessThanOrEqual, 1, 2, useInterpreter));
            Assert.False(TestBinary<int, bool>(ExpressionType.LessThanOrEqual, 2, 1, useInterpreter));
            Assert.True(TestBinary<int, bool>(ExpressionType.LessThanOrEqual, 2, 2, useInterpreter));
            Assert.True(TestBinary<int?, bool>(ExpressionType.LessThanOrEqual, 1, 2, useInterpreter));
            Assert.False(TestBinary<int?, bool>(ExpressionType.LessThanOrEqual, 2, 1, useInterpreter));
            Assert.True(TestBinary<int?, bool>(ExpressionType.LessThanOrEqual, 2, 2, useInterpreter));
            Assert.False(TestBinary<int?, bool>(ExpressionType.LessThanOrEqual, null, 2, useInterpreter));
            Assert.False(TestBinary<int?, bool>(ExpressionType.LessThanOrEqual, 2, null, useInterpreter));
            Assert.False(TestBinary<int?, bool>(ExpressionType.LessThanOrEqual, null, null, useInterpreter));

            Assert.True(TestBinary<decimal, bool>(ExpressionType.LessThanOrEqual, 1, 2, useInterpreter));
            Assert.False(TestBinary<decimal, bool>(ExpressionType.LessThanOrEqual, 2, 1, useInterpreter));
            Assert.True(TestBinary<decimal, bool>(ExpressionType.LessThanOrEqual, 2, 2, useInterpreter));
            Assert.True(TestBinary<decimal?, bool>(ExpressionType.LessThanOrEqual, 1, 2, useInterpreter));
            Assert.False(TestBinary<decimal?, bool>(ExpressionType.LessThanOrEqual, 2, 1, useInterpreter));
            Assert.True(TestBinary<decimal?, bool>(ExpressionType.LessThanOrEqual, 2, 2, useInterpreter));
            Assert.False(TestBinary<decimal?, bool>(ExpressionType.LessThanOrEqual, null, 2, useInterpreter));
            Assert.False(TestBinary<decimal?, bool>(ExpressionType.LessThanOrEqual, 2, null, useInterpreter));
            Assert.False(TestBinary<decimal?, bool>(ExpressionType.LessThanOrEqual, null, null, useInterpreter));

            // GreaterThan
            Assert.False(TestBinary<int, bool>(ExpressionType.GreaterThan, 1, 2, useInterpreter));
            Assert.True(TestBinary<int, bool>(ExpressionType.GreaterThan, 2, 1, useInterpreter));
            Assert.False(TestBinary<int, bool>(ExpressionType.GreaterThan, 2, 2, useInterpreter));
            Assert.False(TestBinary<int?, bool>(ExpressionType.GreaterThan, 1, 2, useInterpreter));
            Assert.True(TestBinary<int?, bool>(ExpressionType.GreaterThan, 2, 1, useInterpreter));
            Assert.False(TestBinary<int?, bool>(ExpressionType.GreaterThan, 2, 2, useInterpreter));
            Assert.False(TestBinary<int?, bool>(ExpressionType.GreaterThan, null, 2, useInterpreter));
            Assert.False(TestBinary<int?, bool>(ExpressionType.GreaterThan, 2, null, useInterpreter));
            Assert.False(TestBinary<int?, bool>(ExpressionType.GreaterThan, null, null, useInterpreter));

            Assert.False(TestBinary<decimal, bool>(ExpressionType.GreaterThan, 1, 2, useInterpreter));
            Assert.True(TestBinary<decimal, bool>(ExpressionType.GreaterThan, 2, 1, useInterpreter));
            Assert.False(TestBinary<decimal, bool>(ExpressionType.GreaterThan, 2, 2, useInterpreter));
            Assert.False(TestBinary<decimal?, bool>(ExpressionType.GreaterThan, 1, 2, useInterpreter));
            Assert.True(TestBinary<decimal?, bool>(ExpressionType.GreaterThan, 2, 1, useInterpreter));
            Assert.False(TestBinary<decimal?, bool>(ExpressionType.GreaterThan, 2, 2, useInterpreter));
            Assert.False(TestBinary<decimal?, bool>(ExpressionType.GreaterThan, null, 2, useInterpreter));
            Assert.False(TestBinary<decimal?, bool>(ExpressionType.GreaterThan, 2, null, useInterpreter));
            Assert.False(TestBinary<decimal?, bool>(ExpressionType.GreaterThan, null, null, useInterpreter));

            // GreaterThanOrEqual
            Assert.False(TestBinary<int, bool>(ExpressionType.GreaterThanOrEqual, 1, 2, useInterpreter));
            Assert.True(TestBinary<int, bool>(ExpressionType.GreaterThanOrEqual, 2, 1, useInterpreter));
            Assert.True(TestBinary<int, bool>(ExpressionType.GreaterThanOrEqual, 2, 2, useInterpreter));
            Assert.False(TestBinary<int?, bool>(ExpressionType.GreaterThanOrEqual, 1, 2, useInterpreter));
            Assert.True(TestBinary<int?, bool>(ExpressionType.GreaterThanOrEqual, 2, 1, useInterpreter));
            Assert.True(TestBinary<int?, bool>(ExpressionType.GreaterThanOrEqual, 2, 2, useInterpreter));
            Assert.False(TestBinary<int?, bool>(ExpressionType.GreaterThanOrEqual, null, 2, useInterpreter));
            Assert.False(TestBinary<int?, bool>(ExpressionType.GreaterThanOrEqual, 2, null, useInterpreter));
            Assert.False(TestBinary<int?, bool>(ExpressionType.GreaterThanOrEqual, null, null, useInterpreter));

            Assert.False(TestBinary<decimal, bool>(ExpressionType.GreaterThanOrEqual, 1, 2, useInterpreter));
            Assert.True(TestBinary<decimal, bool>(ExpressionType.GreaterThanOrEqual, 2, 1, useInterpreter));
            Assert.True(TestBinary<decimal, bool>(ExpressionType.GreaterThanOrEqual, 2, 2, useInterpreter));
            Assert.False(TestBinary<decimal?, bool>(ExpressionType.GreaterThanOrEqual, 1, 2, useInterpreter));
            Assert.True(TestBinary<decimal?, bool>(ExpressionType.GreaterThanOrEqual, 2, 1, useInterpreter));
            Assert.True(TestBinary<decimal?, bool>(ExpressionType.GreaterThanOrEqual, 2, 2, useInterpreter));
            Assert.False(TestBinary<decimal?, bool>(ExpressionType.GreaterThanOrEqual, null, 2, useInterpreter));
            Assert.False(TestBinary<decimal?, bool>(ExpressionType.GreaterThanOrEqual, 2, null, useInterpreter));
            Assert.False(TestBinary<decimal?, bool>(ExpressionType.GreaterThanOrEqual, null, null, useInterpreter));

            // Add
            Assert.Equal(3, TestBinary<int, int>(ExpressionType.Add, 1, 2, useInterpreter));
            Assert.Equal(3, TestBinary<int?, int?>(ExpressionType.Add, 1, 2, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.Add, null, 2, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.Add, 1, null, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.Add, null, null, useInterpreter));

            Assert.Equal(3, TestBinary<decimal, decimal>(ExpressionType.Add, 1, 2, useInterpreter));
            Assert.Equal(3, TestBinary<decimal?, decimal?>(ExpressionType.Add, 1, 2, useInterpreter));
            Assert.Null(TestBinary<decimal?, decimal?>(ExpressionType.Add, null, 2, useInterpreter));
            Assert.Null(TestBinary<decimal?, decimal?>(ExpressionType.Add, 1, null, useInterpreter));
            Assert.Null(TestBinary<decimal?, decimal?>(ExpressionType.Add, null, null, useInterpreter));

            // AddChecked
            Assert.Equal(3, TestBinary<int, int>(ExpressionType.AddChecked, 1, 2, useInterpreter));
            Assert.Equal(3, TestBinary<int?, int?>(ExpressionType.AddChecked, 1, 2, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.AddChecked, null, 2, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.AddChecked, 1, null, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.AddChecked, null, null, useInterpreter));

            // Subtract
            Assert.Equal(1, TestBinary<int, int>(ExpressionType.Subtract, 2, 1, useInterpreter));
            Assert.Equal(1, TestBinary<int?, int?>(ExpressionType.Subtract, 2, 1, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.Subtract, null, 1, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.Subtract, 2, null, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.Subtract, null, null, useInterpreter));

            Assert.Equal(1, TestBinary<decimal, decimal>(ExpressionType.Subtract, 2, 1, useInterpreter));
            Assert.Equal(1, TestBinary<decimal?, decimal?>(ExpressionType.Subtract, 2, 1, useInterpreter));
            Assert.Null(TestBinary<decimal?, decimal?>(ExpressionType.Subtract, null, 1, useInterpreter));
            Assert.Null(TestBinary<decimal?, decimal?>(ExpressionType.Subtract, 2, null, useInterpreter));
            Assert.Null(TestBinary<decimal?, decimal?>(ExpressionType.Subtract, null, null, useInterpreter));

            // SubtractChecked
            Assert.Equal(1, TestBinary<int, int>(ExpressionType.SubtractChecked, 2, 1, useInterpreter));
            Assert.Equal(1, TestBinary<int?, int?>(ExpressionType.SubtractChecked, 2, 1, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.SubtractChecked, null, 1, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.SubtractChecked, 2, null, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.SubtractChecked, null, null, useInterpreter));

            // Multiply
            Assert.Equal(2, TestBinary<int, int>(ExpressionType.Multiply, 2, 1, useInterpreter));
            Assert.Equal(2, TestBinary<int?, int?>(ExpressionType.Multiply, 2, 1, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.Multiply, null, 1, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.Multiply, 2, null, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.Multiply, null, null, useInterpreter));

            Assert.Equal(2, TestBinary<decimal, decimal>(ExpressionType.Multiply, 2, 1, useInterpreter));
            Assert.Equal(2, TestBinary<decimal?, decimal?>(ExpressionType.Multiply, 2, 1, useInterpreter));
            Assert.Null(TestBinary<decimal?, decimal?>(ExpressionType.Multiply, null, 1, useInterpreter));
            Assert.Null(TestBinary<decimal?, decimal?>(ExpressionType.Multiply, 2, null, useInterpreter));
            Assert.Null(TestBinary<decimal?, decimal?>(ExpressionType.Multiply, null, null, useInterpreter));

            // MultiplyChecked
            Assert.Equal(2, TestBinary<int, int>(ExpressionType.MultiplyChecked, 2, 1, useInterpreter));
            Assert.Equal(2, TestBinary<int?, int?>(ExpressionType.MultiplyChecked, 2, 1, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.MultiplyChecked, null, 1, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.MultiplyChecked, 2, null, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.MultiplyChecked, null, null, useInterpreter));

            // Divide
            Assert.Equal(2, TestBinary<int, int>(ExpressionType.Divide, 5, 2, useInterpreter));
            Assert.Equal(2, TestBinary<int?, int?>(ExpressionType.Divide, 5, 2, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.Divide, null, 2, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.Divide, 5, null, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.Divide, null, null, useInterpreter));

            Assert.Equal(2.5m, TestBinary<decimal, decimal>(ExpressionType.Divide, 5, 2, useInterpreter));
            Assert.Equal(2.5m, TestBinary<decimal?, decimal?>(ExpressionType.Divide, 5, 2, useInterpreter));
            Assert.Null(TestBinary<decimal?, decimal?>(ExpressionType.Divide, null, 2, useInterpreter));
            Assert.Null(TestBinary<decimal?, decimal?>(ExpressionType.Divide, 5, null, useInterpreter));
            Assert.Null(TestBinary<decimal?, decimal?>(ExpressionType.Divide, null, null, useInterpreter));

            // Modulo
            Assert.Equal(3, TestBinary<int, int>(ExpressionType.Modulo, 7, 4, useInterpreter));
            Assert.Equal(3, TestBinary<int?, int?>(ExpressionType.Modulo, 7, 4, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.Modulo, null, 4, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.Modulo, 7, null, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.Modulo, null, null, useInterpreter));

            Assert.Equal(3, TestBinary<decimal, decimal>(ExpressionType.Modulo, 7, 4, useInterpreter));
            Assert.Equal(3, TestBinary<decimal?, decimal?>(ExpressionType.Modulo, 7, 4, useInterpreter));
            Assert.Null(TestBinary<decimal?, decimal?>(ExpressionType.Modulo, null, 4, useInterpreter));
            Assert.Null(TestBinary<decimal?, decimal?>(ExpressionType.Modulo, 7, null, useInterpreter));
            Assert.Null(TestBinary<decimal?, decimal?>(ExpressionType.Modulo, null, null, useInterpreter));

            // Power
            Assert.Equal(16, TestBinary<double, double>(ExpressionType.Power, 2, 4, useInterpreter));
            Assert.Equal(16, TestBinary<double?, double?>(ExpressionType.Power, 2, 4, useInterpreter));
            Assert.Null(TestBinary<double?, double?>(ExpressionType.Power, null, 4, useInterpreter));
            Assert.Null(TestBinary<double?, double?>(ExpressionType.Power, 2, null, useInterpreter));
            Assert.Null(TestBinary<double?, double?>(ExpressionType.Power, null, null, useInterpreter));

            // LeftShift
            Assert.Equal(10, TestBinary<int, int>(ExpressionType.LeftShift, 5, 1, useInterpreter));
            Assert.Equal(10, TestBinary<int?, int?>(ExpressionType.LeftShift, 5, 1, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.LeftShift, null, 1, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.LeftShift, 5, null, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.LeftShift, null, null, useInterpreter));

            // RightShift
            Assert.Equal(2, TestBinary<int, int>(ExpressionType.RightShift, 4, 1, useInterpreter));
            Assert.Equal(2, TestBinary<int?, int?>(ExpressionType.RightShift, 4, 1, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.RightShift, null, 1, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.RightShift, 4, null, useInterpreter));
            Assert.Null(TestBinary<int?, int?>(ExpressionType.RightShift, null, null, useInterpreter));
        }

        private static R TestBinary<T, R>(Expression<Func<T, T, R>> f, T v1, T v2, bool useInterpreter)
        {
            Func<T, T, R> d = f.Compile(useInterpreter);
            R rv = d(v1, v2);
            return rv;
        }

        private static R TestBinary<T, R>(ExpressionType op, T v1, T v2, bool useInterpreter)
        {
            ParameterExpression p1 = Expression.Parameter(typeof(T), "v1");
            ParameterExpression p2 = Expression.Parameter(typeof(T), "v2");
            Expression<Func<T, T, R>> f = Expression.Lambda<Func<T, T, R>>(Expression.MakeBinary(op, p1, p2), p1, p2);
            return TestBinary(f, v1, v2, useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void TestConvertToNullable(bool useInterpreter)
        {
            // Using an unchecked cast to ensure that a Convert expression is used (and not ConvertChecked)
            Expression<Func<int, int?>> f = x => unchecked((int?)x);
            Assert.Equal(ExpressionType.Convert, f.Body.NodeType);
            Func<int, int?> d = f.Compile(useInterpreter);
            Assert.Equal(2, d(2));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void TestNullableMethods(bool useInterpreter)
        {
            TestNullableCall(new ArraySegment<int>(), (v) => v.HasValue, (v) => v.HasValue, useInterpreter);
            TestNullableCall(5.1, (v) => v.GetHashCode(), (v) => v.GetHashCode(), useInterpreter);
            TestNullableCall(5L, (v) => v.ToString(), (v) => v.ToString(), useInterpreter);
            TestNullableCall(5, (v) => v.GetValueOrDefault(7), (v) => v.GetValueOrDefault(7), useInterpreter);
            TestNullableCall(42, (v) => v.Equals(42), (v) => v.Equals(42), useInterpreter);
            TestNullableCall(42, (v) => v.Equals(0), (v) => v.Equals(0), useInterpreter);
            TestNullableCall(5, (v) => v.GetValueOrDefault(), (v) => v.GetValueOrDefault(), useInterpreter);

            Expression<Func<int?, int>> f = x => x.Value;
            Func<int?, int> d = f.Compile(useInterpreter);
            Assert.Equal(2, d(2));
            Assert.Throws<InvalidOperationException>(() => d(null));
        }

        private static void TestNullableCall<T, U>(T arg, Func<T?, U> f, Expression<Func<T?, U>> e, bool useInterpreter)
            where T : struct
        {
            Func<T?, U> d = e.Compile(useInterpreter);
            Assert.Equal(f(arg), d(arg));
            Assert.Equal(f(null), d(null));
        }

        public static int BadJuju(int v)
        {
            throw new Exception("Bad Juju");
        }

        public static int M1(int v, Func<int, int> f)
        {
            return f(v);
        }

        public static int M2(int v, Expression<Func<int, int>> f)
        {
            return v;
        }

        public class Foo
        {
            public Foo()
            {
            }
            public int Zip(int y)
            {
                return y * y;
            }
            public virtual string Virt()
            {
                return "Foo";
            }
        }

        public class Bar : Foo
        {
            public Bar()
            {
            }
            public override string Virt()
            {
                return "Bar";
            }
        }

        public class ClassX
        {
            public int A;
            public int B;
            public int C;
            public ClassY Y = new ClassY();
            public ClassY YP
            {
                get { return this.Y; }
            }
            public List<ClassY> Ys = new List<ClassY>();
            public List<StructY> SYs = new List<StructY>();
            public StructY SY;
            public StructY SYP
            {
                get { return this.SY; }
            }
        }

        public class ClassY
        {
            public int B;
            public int PB
            {
                get { return this.B; }
                set { this.B = value; }
            }
        }

        public class StructX
        {
            public int A;
            public int B;
            public int C;
            public ClassY Y = new ClassY();
            public ClassY YP
            {
                get { return this.Y; }
            }
            public List<ClassY> Ys = new List<ClassY>();
            public StructY SY;
            public StructY SYP
            {
                get { return this.SY; }
            }
        }

        public struct StructY
        {
            public int B;
            public int PB
            {
                get { return this.B; }
                set { this.B = value; }
            }
        }

        public struct StructZ
        {
            public int A;
            public StructZ(int a)
            {
                this.A = a;
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void PropertyAccess(bool useInterpreter)
        {
            NWindProxy.Customer cust = new NWindProxy.Customer { CustomerID = "BUBBA", ContactName = "Bubba Gump" };
            ParameterExpression c = Expression.Parameter(typeof(NWindProxy.Customer), "c");
            ParameterExpression c2 = Expression.Parameter(typeof(NWindProxy.Customer), "c2");

            Assert.Equal(cust, Expression.Lambda(c, c).Compile(useInterpreter).DynamicInvoke(cust));
            Assert.Equal(cust.ContactName, Expression.Lambda(Expression.PropertyOrField(c, "ContactName"), c).Compile(useInterpreter).DynamicInvoke(cust));
            Assert.Equal(cust.Orders, Expression.Lambda(Expression.PropertyOrField(c, "Orders"), c).Compile(useInterpreter).DynamicInvoke(cust));
            Assert.Equal(cust.CustomerID, Expression.Lambda(Expression.PropertyOrField(c, "CustomerId"), c).Compile(useInterpreter).DynamicInvoke(cust));
            Assert.True((bool)Expression.Lambda(Expression.Equal(Expression.PropertyOrField(c, "CustomerId"), Expression.PropertyOrField(c, "CUSTOMERID")), c).Compile(useInterpreter).DynamicInvoke(cust));
            Assert.True((bool)
                Expression.Lambda(
                    Expression.And(
                        Expression.Equal(Expression.PropertyOrField(c, "CustomerId"), Expression.PropertyOrField(c2, "CustomerId")),
                        Expression.Equal(Expression.PropertyOrField(c, "ContactName"), Expression.PropertyOrField(c2, "ContactName"))
                        ),
                    c, c2)
                .Compile(useInterpreter).DynamicInvoke(cust, cust));
        }

        private static void ArimeticOperatorTests(Type type, object value, bool testUnSigned, bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(type, "x");
            if (testUnSigned)
                Expression.Lambda(Expression.Negate(p), p).Compile(useInterpreter).DynamicInvoke(new object[] { value });
            Expression.Lambda(Expression.Add(p, p), p).Compile(useInterpreter).DynamicInvoke(new object[] { value });
            Expression.Lambda(Expression.Subtract(p, p), p).Compile(useInterpreter).DynamicInvoke(new object[] { value });
            Expression.Lambda(Expression.Multiply(p, p), p).Compile(useInterpreter).DynamicInvoke(new object[] { value });
            Expression.Lambda(Expression.Divide(p, p), p).Compile(useInterpreter).DynamicInvoke(new object[] { value });
        }

        private static void RelationalOperatorTests(Type type, object value, bool testModulo, bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(type, "x");
            if (testModulo)
                Expression.Lambda(Expression.Modulo(p, p), p).Compile(useInterpreter).DynamicInvoke(new object[] { value });
            Expression.Lambda(Expression.Equal(p, p), p).Compile(useInterpreter).DynamicInvoke(new object[] { value });
            Expression.Lambda(Expression.NotEqual(p, p), p).Compile(useInterpreter).DynamicInvoke(new object[] { value });
            Expression.Lambda(Expression.LessThan(p, p), p).Compile(useInterpreter).DynamicInvoke(new object[] { value });
            Expression.Lambda(Expression.LessThanOrEqual(p, p), p).Compile(useInterpreter).DynamicInvoke(new object[] { value });
            Expression.Lambda(Expression.GreaterThan(p, p), p).Compile(useInterpreter).DynamicInvoke(new object[] { value });
            Expression.Lambda(Expression.GreaterThanOrEqual(p, p), p).Compile(useInterpreter).DynamicInvoke(new object[] { value });
        }

        private static void NumericOperatorTests(Type type, object value, bool testModulo, bool testUnSigned, bool useInterpreter)
        {
            ArimeticOperatorTests(type, value, testUnSigned, useInterpreter);
            RelationalOperatorTests(type, value, testModulo, useInterpreter);
        }

        private static void NumericOperatorTests(Type type, object value, bool useInterpreter)
        {
            NumericOperatorTests(type, value, true, true, useInterpreter);
        }

        private static void IntegerOperatorTests(Type type, object value, bool testModulo, bool testUnsigned, bool useInterpreter)
        {
            NumericOperatorTests(type, value, testModulo, testUnsigned, useInterpreter);
            LogicalOperatorTests(type, value, useInterpreter);
        }

        private static void LogicalOperatorTests(Type type, object value, bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(type, "x");
            Expression.Lambda(Expression.Not(p), p).Compile(useInterpreter).DynamicInvoke(new object[] { value });
            Expression.Lambda(Expression.Or(p, p), p).Compile(useInterpreter).DynamicInvoke(new object[] { value });
            Expression.Lambda(Expression.And(p, p), p).Compile(useInterpreter).DynamicInvoke(new object[] { value });
            Expression.Lambda(Expression.ExclusiveOr(p, p), p).Compile(useInterpreter).DynamicInvoke(new object[] { value });
        }

        private static void IntegerOperatorTests(Type type, object value, bool useInterpreter)
        {
            IntegerOperatorTests(type, value, true, true, useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void NumericOperators(bool useInterpreter)
        {
            RelationalOperatorTests(typeof(sbyte), (sbyte)1, false, useInterpreter);
            LogicalOperatorTests(typeof(sbyte), (sbyte)1, useInterpreter);
            RelationalOperatorTests(typeof(short), (short)1, false, useInterpreter);
            LogicalOperatorTests(typeof(sbyte), (sbyte)1, useInterpreter);
            IntegerOperatorTests(typeof(int), 1, useInterpreter);
            IntegerOperatorTests(typeof(long), (long)1, useInterpreter);
            RelationalOperatorTests(typeof(byte), (byte)1, false, useInterpreter);
            LogicalOperatorTests(typeof(byte), (byte)1, useInterpreter);
            RelationalOperatorTests(typeof(ushort), (ushort)1, false, useInterpreter);
            LogicalOperatorTests(typeof(ushort), (ushort)1, useInterpreter);
            IntegerOperatorTests(typeof(uint), (uint)1, true, false, useInterpreter);
            IntegerOperatorTests(typeof(ulong), (ulong)1, true, false, useInterpreter);
            NumericOperatorTests(typeof(float), (float)1, useInterpreter);
            NumericOperatorTests(typeof(double), (double)1, useInterpreter);
            NumericOperatorTests(typeof(decimal), (decimal)1, useInterpreter);

            RelationalOperatorTests(typeof(sbyte?), (sbyte?)1, false, useInterpreter);
            LogicalOperatorTests(typeof(sbyte?), (sbyte?)1, useInterpreter);
            RelationalOperatorTests(typeof(short?), (short?)1, false, useInterpreter);
            LogicalOperatorTests(typeof(short?), (short?)1, useInterpreter);
            IntegerOperatorTests(typeof(int?), (int?)1, useInterpreter);
            IntegerOperatorTests(typeof(long?), (long?)1, useInterpreter);
            RelationalOperatorTests(typeof(byte?), (byte?)1, false, useInterpreter);
            LogicalOperatorTests(typeof(byte?), (byte?)1, useInterpreter);
            RelationalOperatorTests(typeof(ushort?), (ushort?)1, false, useInterpreter);
            LogicalOperatorTests(typeof(ushort?), (ushort?)1, useInterpreter);
            IntegerOperatorTests(typeof(uint?), (uint?)1, true, false, useInterpreter);
            IntegerOperatorTests(typeof(ulong?), (ulong?)1, true, false, useInterpreter);

            NumericOperatorTests(typeof(float?), (float?)1, useInterpreter);
            NumericOperatorTests(typeof(double?), (double?)1, useInterpreter);
            NumericOperatorTests(typeof(decimal?), (decimal?)1, useInterpreter);
        }

        private static void TrueBooleanOperatorTests(Type type, object arg1, object arg2, bool useInterpreter)
        {
            ParameterExpression x = Expression.Parameter(type, "x");
            ParameterExpression y = Expression.Parameter(type, "y");
            Expression.Lambda(Expression.AndAlso(x, y), x, y).Compile(useInterpreter).DynamicInvoke(new object[] { arg1, arg2 });
            Expression.Lambda(Expression.OrElse(x, y), x, y).Compile(useInterpreter).DynamicInvoke(new object[] { arg1, arg2 });
            GeneralBooleanOperatorTests(type, arg1, arg2, useInterpreter);
        }

        private static void GeneralBooleanOperatorTests(Type type, object arg1, object arg2, bool useInterpreter)
        {
            ParameterExpression x = Expression.Parameter(type, "x");
            ParameterExpression y = Expression.Parameter(type, "y");
            Expression.Lambda(Expression.And(x, y), x, y).Compile(useInterpreter).DynamicInvoke(new object[] { arg1, arg2 });
            Expression.Lambda(Expression.Or(x, y), x, y).Compile(useInterpreter).DynamicInvoke(new object[] { arg1, arg2 });
            Expression.Lambda(Expression.Not(x), x).Compile(useInterpreter).DynamicInvoke(new object[] { arg1 });
            Expression.Lambda(Expression.Equal(x, y), x, y).Compile(useInterpreter).DynamicInvoke(new object[] { arg1, arg2 });
            Expression.Lambda(Expression.NotEqual(x, y), x, y).Compile(useInterpreter).DynamicInvoke(new object[] { arg1, arg2 });
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void BooleanOperators(bool useInterpreter)
        {
            TrueBooleanOperatorTests(typeof(bool), true, false, useInterpreter);
            TrueBooleanOperatorTests(typeof(bool?), true, false, useInterpreter);
        }
    }

    public struct U
    {
        public static U operator +(U x, U y) { throw new NotImplementedException(); }
        public static U operator -(U x, U y) { throw new NotImplementedException(); }
        public static U operator *(U x, U y) { throw new NotImplementedException(); }
        public static U operator /(U x, U y) { throw new NotImplementedException(); }
        public static U operator <(U x, U y) { throw new NotImplementedException(); }
        public static U operator <=(U x, U y) { throw new NotImplementedException(); }
        public static U operator >(U x, U y) { throw new NotImplementedException(); }
        public static U operator >=(U x, U y) { throw new NotImplementedException(); }
        public static U operator ==(U x, U y) { throw new NotImplementedException(); }
        public static U operator !=(U x, U y) { throw new NotImplementedException(); }
        public static U operator &(U x, U y) { throw new NotImplementedException(); }
        public static U operator |(U x, U y) { throw new NotImplementedException(); }
        public static U operator ^(U x, U y) { throw new NotImplementedException(); }
        public static U operator -(U x) { throw new NotImplementedException(); }
        public static U operator ~(U x) { throw new NotImplementedException(); }
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public struct B
    {
        public static B operator <(B x, B y) { throw new NotImplementedException(); }
        public static B operator <=(B x, B y) { throw new NotImplementedException(); }
        public static B operator >(B x, B y) { throw new NotImplementedException(); }
        public static B operator >=(B x, B y) { throw new NotImplementedException(); }
        public static B operator ==(B x, B y) { throw new NotImplementedException(); }
        public static B operator !=(B x, B y) { throw new NotImplementedException(); }
        public static B operator &(B x, B y) { throw new NotImplementedException(); }
        public static B operator |(B x, B y) { throw new NotImplementedException(); }
        public static B operator ^(B x, B y) { throw new NotImplementedException(); }
        public static B operator !(B x) { throw new NotImplementedException(); }
        public static B operator -(B x) { throw new NotImplementedException(); }
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public struct M
    {
        public static M operator +(M m, N n) { throw new NotImplementedException(); }
        public static M operator -(M m, N n) { throw new NotImplementedException(); }
        public static M operator -(M m) { throw new NotImplementedException(); }
        public static M operator ~(M m) { throw new NotImplementedException(); }
        public static explicit operator M(N n) { throw new NotImplementedException(); }
        public static N Foo(M m) { throw new NotImplementedException(); }
    }

    public struct N
    {
        public static M operator *(M m, N n) { throw new NotImplementedException(); }
        public static M operator /(M m, N n) { throw new NotImplementedException(); }
        public static N operator -(N n) { throw new NotImplementedException(); }
        public static implicit operator N(M m) { throw new NotImplementedException(); }
        public static M Bar(N n) { throw new NotImplementedException(); }
    }

    public class DerivedClass : BaseClass
    {
    }

    internal class AssertionException : Exception
    {
        public AssertionException(string msg)
            : base(msg)
        {
        }
    }
}
