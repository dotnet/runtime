// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class ExpressionDebuggerTypeProxyTests
    {
        private class Inner
        {
            public int Value { get; set; }
        }

        private class Outer
        {
            public Inner InnerProperty { get; set; } = new Inner();
        }

        private static readonly PropertyInfo DebugViewProperty = typeof(Expression).GetProperty(
            "DebugView", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        // Available via Arguments and Expressions properties
        private static readonly string[] ExcludedPropertyNames = {"ArgumentCount", "ExpressionCount"};

        private Type GetDebugViewType(Type type)
        {
            var att =
                (DebuggerTypeProxyAttribute)
                    type.GetCustomAttributes().Single(at => at.TypeId.Equals(typeof(DebuggerTypeProxyAttribute)));
            string proxyName = att.ProxyTypeName;
            proxyName = proxyName.Substring(0, proxyName.IndexOf(','));
            return type.GetTypeInfo().Assembly.GetType(proxyName);
        }

        private void AssertIsReadOnly<T>(ICollection<T> collection)
        {
            Assert.True(collection.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => collection.Clear());
            Assert.Throws<NotSupportedException>(() => collection.Add(default(T)));
            Assert.Throws<NotSupportedException>(() => collection.Remove(default(T)));
        }

        [Theory]
        [MemberData(nameof(BinaryExpressionProxy))]
        [MemberData(nameof(BlockExpressionProxy))]
        [MemberData(nameof(CatchBlockProxy))]
        [MemberData(nameof(ConditionalExpressionProxy))]
        [MemberData(nameof(ConstantExpressionProxy))]
        [MemberData(nameof(DebugInfoExpressionProxy))]
        [MemberData(nameof(DefaultExpressionProxy))]
        [MemberData(nameof(GotoExpressionProxy))]
        [MemberData(nameof(IndexExpressionProxy))]
        [MemberData(nameof(InvocationExpressionProxy))]
        [MemberData(nameof(LabelExpressionProxy))]
        [MemberData(nameof(LambdaExpressionProxy))]
        [MemberData(nameof(ListInitExpressionProxy))]
        [MemberData(nameof(LoopExpressionProxy))]
        [MemberData(nameof(MemberExpressionProxy))]
        [MemberData(nameof(MemberInitExpressionProxy))]
        [MemberData(nameof(MethodCallExpressionProxy))]
        [MemberData(nameof(NewArrayExpressionProxy))]
        [MemberData(nameof(NewExpressionProxy))]
        [MemberData(nameof(ParameterExpressionProxy))]
        [MemberData(nameof(RuntimeVariablesExpressionProxy))]
        [MemberData(nameof(SwitchCaseExpressionProxy))]
        [MemberData(nameof(SwitchExpressionProxy))]
        [MemberData(nameof(TryExpressionProxy))]
        [MemberData(nameof(TypeBinaryExpressionProxy))]
        [MemberData(nameof(UnaryExpressionProxy))]
        public void VerifyDebugView(object obj)
        {
            Type type = obj.GetType();
            Type viewType = GetDebugViewType(type);
            object view = viewType.GetConstructors().Single().Invoke(new[] {obj});
            IEnumerable<PropertyInfo> properties =
                type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                    .Where(pr => !ExcludedPropertyNames.Contains(pr.Name) && pr.CanRead);
            if (obj is Expression)
            {
                properties = properties.Append(DebugViewProperty);
            }
            foreach (var property in properties)
            {
                string name = property.Name;
                PropertyInfo proxyProperty = viewType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                Assert.True(proxyProperty != null, $"Could not find property {name} in proxy for {type}");
                Assert.True(proxyProperty.CanRead);
                Assert.False(proxyProperty.CanWrite);
                object value;
                try
                {
                    value = property.GetValue(obj);
                }
                catch (Exception ex)
                {
                    Exception proxyEx = Assert.ThrowsAny<Exception>(() => proxyProperty.GetValue(view));
                    Assert.IsType(ex.GetType(), proxyEx);
                    continue;
                }

                object proxyValue = proxyProperty.GetValue(view);
                Assert.Equal(value, proxyValue);
                if (!(proxyValue is string) && proxyValue is IEnumerable)
                {
                    var exCol = proxyValue as ICollection<Expression>;
                    if (exCol != null)
                    {
                        AssertIsReadOnly(exCol);
                        continue;
                    }

                    var parCol = proxyValue as ICollection<ParameterExpression>;
                    if (parCol != null)
                    {
                        AssertIsReadOnly(parCol);
                        continue;
                    }

                    var elCol = proxyValue as ICollection<ElementInit>;
                    if (elCol != null)
                    {
                        AssertIsReadOnly(elCol);
                        continue;
                    }

                    var mbCol = proxyValue as ICollection<MemberBinding>;
                    if (mbCol != null)
                    {
                        AssertIsReadOnly(mbCol);
                        continue;
                    }

                    var miCol = proxyValue as ICollection<MemberInfo>;
                    if (miCol != null)
                    {
                        AssertIsReadOnly(miCol);
                        continue;
                    }

                    var scCol = proxyValue as ICollection<SwitchCase>;
                    if (scCol != null)
                    {
                        AssertIsReadOnly(scCol);
                        continue;
                    }

                    AssertIsReadOnly((ICollection<CatchBlock>)proxyValue);
                }
            }
        }

        [Theory, MemberData(nameof(OnePerType))]
        public void ThrowOnNullToCtor(object sourceObject)
        {
            Type type = sourceObject.GetType();
            Type viewType = GetDebugViewType(type);
            ConstructorInfo ctor = viewType.GetConstructors().Single();
            TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => ctor.Invoke(new object[] { null }));
            ArgumentNullException ane = (ArgumentNullException)tie.InnerException;
            if (!PlatformDetection.IsNetNative) // The .NET Native toolchain optimizes away exception ParamNames
            {
                Assert.Equal(ctor.GetParameters()[0].Name, ane.ParamName);
            }
        }

        private static IEnumerable<object[]> OnePerType()
        {
            HashSet<Type> seenTypes = new HashSet<Type>();
            foreach (var candidate in
                BinaryExpressionProxy()
                    .Concat(BlockExpressionProxy())
                    .Concat(CatchBlockProxy())
                    .Concat(ConditionalExpressionProxy())
                    .Concat(ConstantExpressionProxy())
                    .Concat(DebugInfoExpressionProxy())
                    .Concat(DefaultExpressionProxy())
                    .Concat(GotoExpressionProxy())
                    .Concat(IndexExpressionProxy())
                    .Concat(InvocationExpressionProxy())
                    .Concat(LabelExpressionProxy())
                    .Concat(LambdaExpressionProxy())
                    .Concat(ListInitExpressionProxy())
                    .Concat(LoopExpressionProxy())
                    .Concat(MemberExpressionProxy())
                    .Concat(MemberInitExpressionProxy())
                    .Concat(MethodCallExpressionProxy())
                    .Concat(NewArrayExpressionProxy())
                    .Concat(NewExpressionProxy())
                    .Concat(ParameterExpressionProxy())
                    .Concat(RuntimeVariablesExpressionProxy())
                    .Concat(SwitchCaseExpressionProxy())
                    .Concat(SwitchExpressionProxy())
                    .Concat(TryExpressionProxy())
                    .Concat(TypeBinaryExpressionProxy())
                    .Concat(UnaryExpressionProxy()))
            {
                if (seenTypes.Add(candidate[0].GetType()))
                    yield return candidate;
            }
        }

        private static IEnumerable<object[]> BinaryExpressionProxy()
        {
            yield return new object[] {Expression.Assign(Expression.Variable(typeof(int)), Expression.Constant(-1))};
            yield return new object[] {Expression.Equal(Expression.Constant(false), Expression.Constant(true))};
            yield return new object[] {Expression.AddAssign(Expression.Parameter(typeof(int)), Expression.Constant(2))};
            yield return new object[] {Expression.Assign(Expression.Parameter(typeof(int)), Expression.Constant(2))};
        }

        private static IEnumerable<object[]> BlockExpressionProxy()
        {
            for (int paramCount = 0; paramCount != 4; ++paramCount)
            {
                List<ParameterExpression> parameters = Enumerable.Range(0, paramCount).Select(x => Expression.Parameter(typeof(int))).ToList();
                for (int count = 0; count != 7; ++count)
                {
                    yield return
                        new object[]
                        {Expression.Block(parameters, Enumerable.Range(0, count).Select(x => Expression.Constant(x)))};
                }
            }
        }

        private static IEnumerable<object[]> CatchBlockProxy()
        {
            yield return new object[] {Expression.Catch(typeof(InvalidFilterCriteriaException), Expression.Empty())};
            yield return
                new object[]
                {Expression.Catch(typeof(InvalidCastException), Expression.Constant(3), Expression.Constant(false))};
            yield return
                new object[]
                {Expression.Catch(Expression.Variable(typeof(InvalidFilterCriteriaException)), Expression.Empty())};
            yield return
                new object[]
                {
                    Expression.Catch(
                        Expression.Variable(typeof(InvalidCastException)), Expression.Constant(3),
                        Expression.Constant(false))
                };
        }

        private static IEnumerable<object[]> ConditionalExpressionProxy()
        {
            yield return new object[] {Expression.IfThen(Expression.Constant(false), Expression.Constant(1))};
            yield return
                new object[]
                {Expression.IfThenElse(Expression.Constant(false), Expression.Constant(2), Expression.Constant(3))};
            yield return
                new object[]
                {Expression.Condition(Expression.Constant(true), Expression.Constant(4), Expression.Constant(5))};
            yield return
                new object[]
                {
                    Expression.Condition(
                        Expression.Constant(true), Expression.Constant(""), Expression.Constant(""), typeof(object))
                };
        }

        private static IEnumerable<object[]> ConstantExpressionProxy()
        {
            yield return new object[] {Expression.Constant(DateTime.UtcNow)};
            yield return new object[] {Expression.Constant(null)};
            yield return new object[] {Expression.Constant(null, typeof(int?))};
        }

        private static IEnumerable<object[]> DebugInfoExpressionProxy()
        {
            yield return new object[] {Expression.ClearDebugInfo(Expression.SymbolDocument(""))};
            yield return new object[] {Expression.DebugInfo(Expression.SymbolDocument(""), 1, 2, 3, 4)};
        }

        private static IEnumerable<object[]> DefaultExpressionProxy()
        {
            yield return new object[] {Expression.Empty()};
            yield return new object[] {Expression.Default(typeof(int))};
        }

        private static IEnumerable<object[]> GotoExpressionProxy()
        {
            yield return new object[] {Expression.Continue(Expression.Label(typeof(void)))};
            yield return new object[] {Expression.Break(Expression.Label(typeof(void)), typeof(void))};
            yield return
                new object[]
                {Expression.Return(Expression.Label(typeof(object)), Expression.Constant(""), typeof(object))};
        }

        private static IEnumerable<object[]> IndexExpressionProxy()
        {
            yield return
                new object[] {Expression.ArrayAccess(Expression.Constant(new[] {1, 2, 3}), Expression.Constant(2))};
            yield return
                new object[]
                {
                    Expression.Property(
                        Expression.Default(typeof(Dictionary<string, int>)), "Item", Expression.Constant("key"))
                };
        }

        private static IEnumerable<object[]> InvocationExpressionProxy()
        {
            Func<int, int, int> addFunc = (x, y) => x + y;
            yield return
                new object[]
                {Expression.Invoke(Expression.Constant(addFunc), Expression.Constant(4), Expression.Constant(5))};

            for (int i = 0; i != 7; ++i)
            {
                ParameterExpression[] parameters = Enumerable.Range(0, i).Select(_ => Expression.Parameter(typeof(int))).ToArray();
                Expression[] arguments = Enumerable.Range(0, i).Select(x => (Expression)Expression.Constant(x)).ToArray();
                yield return
                    new object[] {Expression.Invoke(Expression.Lambda(Expression.Empty(), parameters), arguments)};
                yield return
                    new object[] {Expression.Invoke(Expression.Lambda(Expression.Constant(0), parameters), arguments)};
            }
        }

        private static IEnumerable<object[]> LabelExpressionProxy()
        {
            yield return new object[] {Expression.Label(Expression.Label(typeof(void)))};
            yield return new object[] {Expression.Label(Expression.Label(typeof(string)), Expression.Constant("!"))};
        }

        private static IEnumerable<object[]> LambdaExpressionProxy()
        {
            Expression<Func<int, int, int>> add = (x, y) => x + y;
            yield return new object[] {add};
            yield return new object[] {Expression.Lambda(Expression.Empty())};
        }

        private static IEnumerable<object[]> ListInitExpressionProxy()
        {
            Expression<Func<List<int>>> exp = () => new List<int> {1, 2, 3};
            yield return new object[] {exp.Body};
        }

        private static IEnumerable<object[]> LoopExpressionProxy()
        {
            yield return new object[] {Expression.Loop(Expression.Empty())};
            yield return new object[] {Expression.Loop(Expression.Empty(), Expression.Label(typeof(void)))};
            yield return
                new object[]
                {Expression.Loop(Expression.Empty(), Expression.Label(typeof(void)), Expression.Label(typeof(void)))};
        }

        private static IEnumerable<object[]> MemberExpressionProxy()
        {
            yield return new object[] {Expression.Field(null, typeof(ExpressionDebuggerTypeProxyTests), nameof(DebugViewProperty))};
            yield return new object[] {Expression.Property(Expression.Constant(""), "Length")};
        }

        private static IEnumerable<object[]> MemberInitExpressionProxy()
        {
            yield return
                new object[]
                {
                    Expression.MemberInit(
                        Expression.New(typeof(Outer)),
                        Expression.MemberBind(
                            typeof(Outer).GetProperty(nameof(Outer.InnerProperty)),
                            Expression.Bind(typeof(Inner).GetProperty(nameof(Inner.Value)), Expression.Constant(3))))
                };
        }

        private static IEnumerable<object[]> MethodCallExpressionProxy()
        {
            yield return new object[] {Expression.Call(Expression.Constant(1), "ToString", new Type[0])};
            Expression<Func<bool>> exp = () => 1.Equals(2);
            yield return new object[] {exp.Body};
        }

        private static IEnumerable<object[]> NewArrayExpressionProxy()
        {
            yield return
                new object[] {Expression.NewArrayBounds(typeof(int), Expression.Constant(2), Expression.Constant(2))};
            yield return
                new object[]
                {Expression.NewArrayInit(typeof(string), Expression.Constant("A"), Expression.Constant("B"))};
        }

        private static IEnumerable<object[]> NewExpressionProxy()
        {
            yield return new object[] {Expression.New(typeof(object))};
            yield return
                new object[]
                {
                    Expression.New(
                        typeof(string).GetConstructors().First(c => c.GetParameters().Length == 2),
                        Expression.Constant('x'), Expression.Constant(3))
                };
        }

        private static IEnumerable<object[]> ParameterExpressionProxy()
        {
            yield return new object[] {Expression.Variable(typeof(int))};
            yield return new object[] {Expression.Parameter(typeof(Expression))};
            yield return new object[] {Expression.Parameter(typeof(int).MakeByRefType())};
        }

        private static IEnumerable<object[]> RuntimeVariablesExpressionProxy()
        {
            yield return new object[] {Expression.RuntimeVariables(Expression.Variable(typeof(int)))};
            yield return new object[] {Expression.RuntimeVariables()};
        }

        private static IEnumerable<object[]> SwitchCaseExpressionProxy()
        {
            yield return new object[] {Expression.SwitchCase(Expression.Empty(), Expression.Constant(0))};
        }

        private static IEnumerable<object[]> SwitchExpressionProxy()
        {
            yield return new object[] {Expression.Switch(Expression.Constant(2))};
            yield return
                new object[]
                {
                    Expression.Switch(
                        Expression.Constant(2), Expression.Constant("!"),
                        Expression.SwitchCase(Expression.Constant("X"), Expression.Constant(1)))
                };
        }

        private static IEnumerable<object[]> TryExpressionProxy()
        {
            yield return new object[] {Expression.TryFault(Expression.Empty(), Expression.Empty())};
            yield return new object[] {Expression.TryFinally(Expression.Empty(), Expression.Empty())};
            yield return
                new object[]
                {
                    Expression.TryCatch(
                        Expression.Constant(1), Expression.Catch(typeof(Exception), Expression.Constant(2)))
                };
            yield return
                new object[]
                {
                    Expression.TryCatchFinally(
                        Expression.Constant(1), Expression.Empty(),
                        Expression.Catch(typeof(Exception), Expression.Constant(2)))
                };
        }

        private static IEnumerable<object[]> TypeBinaryExpressionProxy()
        {
            yield return new object[] {Expression.TypeIs(Expression.Constant(2), typeof(string))};
            yield return new object[] {Expression.TypeAs(Expression.Constant("", typeof(object)), typeof(string))};
        }

        private static IEnumerable<object[]> UnaryExpressionProxy()
        {
            yield return new object[] {Expression.Not(Expression.Constant(true))};
            yield return new object[] {Expression.Increment(Expression.Variable(typeof(int)))};
            yield return new object[] {Expression.OnesComplement(Expression.Constant(23))};
        }
    }
}
