// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace System.Dynamic.Tests
{
    public class BindingRestrictionsTests
    {
        // Considers itself equal to all of its kind, hence distinguishing equality and reference equality in tests.
        private class Egalitarian : IEquatable<Egalitarian>
        {
            public bool Equals(Egalitarian other) => true;

            public override bool Equals(object obj) => obj is Egalitarian;

            public override int GetHashCode() => 1;
        }

        [Fact]
        public void EmptyAllowsAll()
        {
            Expression exp = BindingRestrictions.Empty.ToExpression();
            Assert.IsType<ConstantExpression>(exp);
            Assert.Equal(typeof(bool), exp.Type);
            Assert.Equal(true, ((ConstantExpression)exp).Value);

            // The above are implementation details that could reasonably change without error.
            // The below must still hold so that empty binding restrictions still allows everything.
            Assert.True(Expression.Lambda<Func<bool>>(exp).Compile()());
        }

        [Fact]
        public void MergeWithEmptyReturnsSame()
        {
            BindingRestrictions isTrueBool = BindingRestrictions.GetTypeRestriction(
                Expression.Constant(true), typeof(bool));
            Assert.Same(isTrueBool, isTrueBool.Merge(BindingRestrictions.Empty));
            Assert.Same(isTrueBool, BindingRestrictions.Empty.Merge(isTrueBool));
        }

        [Fact]
        public void MergeWithSelfReturnsNotSame()
        {
            BindingRestrictions isTrueBool = BindingRestrictions.GetTypeRestriction(
                Expression.Constant(true), typeof(bool));
            Assert.NotSame(isTrueBool, isTrueBool.Merge(isTrueBool));
        }

        [Fact]
        public void MergeWithSelfHasSameExpression()
        {
            Expression exp = Expression.Constant(true);
            BindingRestrictions allowAll = BindingRestrictions.GetExpressionRestriction(exp);
            BindingRestrictions doubled = allowAll.Merge(allowAll);
            Assert.Same(exp, doubled.ToExpression());
        }

        [Fact]
        public void MergeNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("restrictions", () => BindingRestrictions.Empty.Merge(null));
        }

        [Fact]
        public void ExpressionRestrictionFromNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("expression", () => BindingRestrictions.GetExpressionRestriction(null));
        }

        [Fact]
        public void ExpressionRestrictionFromNonBooleanExpression()
        {
            AssertExtensions.Throws<ArgumentException>(
                "expression", () => BindingRestrictions.GetExpressionRestriction(Expression.Empty()));
            AssertExtensions.Throws<ArgumentException>(
                "expression", () => BindingRestrictions.GetExpressionRestriction(Expression.Constant("")));
        }

        [Fact]
        public void InstanceRestrictionFromNull()
        {
            AssertExtensions.Throws<ArgumentNullException>(
                "expression", () => BindingRestrictions.GetInstanceRestriction(null, new object()));
        }

        [Fact]
        public void CombineRestrictionsFromNull()
        {
            Assert.Same(BindingRestrictions.Empty, BindingRestrictions.Combine(null));
        }

        [Fact]
        public void CombineRestrictionsFromEmpty()
        {
            Assert.Same(BindingRestrictions.Empty, BindingRestrictions.Combine(Array.Empty<DynamicMetaObject>()));
        }

        [Fact]
        public void CombineRestrictionsFromAllNull()
        {
            Assert.Same(BindingRestrictions.Empty, BindingRestrictions.Combine(new DynamicMetaObject[10]));
        }

        [Fact]
        public void CustomRestrictionsEqualIfExpressionSame()
        {
            Expression exp = Expression.Constant(false);
            BindingRestrictions x = BindingRestrictions.GetExpressionRestriction(exp);
            BindingRestrictions y = BindingRestrictions.GetExpressionRestriction(exp);
            Assert.Equal(x, y);
            Assert.Equal(x.GetHashCode(), y.GetHashCode());
        }

        [Fact]
        public void CustomRestrictionsNotEqualIfExpressionsNotSame()
        {
            BindingRestrictions x = BindingRestrictions.GetExpressionRestriction(Expression.Constant(false));
            BindingRestrictions y = BindingRestrictions.GetExpressionRestriction(Expression.Constant(false));
            Assert.NotEqual(x, y);
        }

        [Fact]
        public void CustomRestrictionsNotEqualNull()
        {
            BindingRestrictions br = BindingRestrictions.GetExpressionRestriction(Expression.Constant(false));
            Assert.False(br.Equals(null));
        }

        [Fact]
        public void MergeCombines()
        {
            foreach (bool x in new[] {false, true})
                foreach (bool y in new[] {false, true})
                {
                    BindingRestrictions bX = BindingRestrictions.GetExpressionRestriction(Expression.Constant(x));
                    BindingRestrictions bY = BindingRestrictions.GetExpressionRestriction(Expression.Constant(y));
                    BindingRestrictions merged = bX.Merge(bY);
                    Assert.Equal(x & y, Expression.Lambda<Func<bool>>(merged.ToExpression()).Compile()());
                }
        }

        [Fact]
        public void MergeCombinesDeeper()
        {
            foreach (bool w in new[] {false, true})
            {
                BindingRestrictions bW = BindingRestrictions.GetExpressionRestriction(Expression.Constant(w));
                foreach (bool x in new[] {false, true})
                {
                    BindingRestrictions bX = BindingRestrictions.GetExpressionRestriction(Expression.Constant(x));
                    foreach (bool y in new[] {false, true})
                    {
                        BindingRestrictions bY = BindingRestrictions.GetExpressionRestriction(Expression.Constant(y));
                        BindingRestrictions merged = bW.Merge(bX).Merge(bY);
                        Assert.Equal(w & x & y, Expression.Lambda<Func<bool>>(merged.ToExpression()).Compile()());
                        foreach (bool z in new[] {false, true})
                        {
                            BindingRestrictions bZ = BindingRestrictions.GetExpressionRestriction(
                                Expression.Constant(z));
                            merged = bW.Merge(bX).Merge(bY).Merge(bZ);
                            Assert.Equal(
                                w & x & y & z, Expression.Lambda<Func<bool>>(merged.ToExpression()).Compile()());
                        }
                    }
                }
            }
        }

        [Fact]
        public void InstanceRestrictionRequiresIdentity()
        {
            Egalitarian instance = new Egalitarian();
            Expression exp = Expression.Constant(instance);
            BindingRestrictions sameInstance = BindingRestrictions.GetInstanceRestriction(exp, instance);
            Assert.True(Expression.Lambda<Func<bool>>(sameInstance.ToExpression()).Compile()());
            BindingRestrictions diffInstance = BindingRestrictions.GetInstanceRestriction(exp, new Egalitarian());
            Assert.False(Expression.Lambda<Func<bool>>(diffInstance.ToExpression()).Compile()());
            BindingRestrictions noInstance = BindingRestrictions.GetInstanceRestriction(exp, null);
            Assert.False(Expression.Lambda<Func<bool>>(noInstance.ToExpression()).Compile()());
        }

        [Fact]
        public void InstanceRestrictionForNull()
        {
            Expression exp = Expression.Default(typeof(Egalitarian));
            BindingRestrictions hasNull = BindingRestrictions.GetInstanceRestriction(exp, null);
            Assert.True(Expression.Lambda<Func<bool>>(hasNull.ToExpression()).Compile()());
            BindingRestrictions hasInst = BindingRestrictions.GetInstanceRestriction(exp, new Egalitarian());
            Assert.False(Expression.Lambda<Func<bool>>(hasInst.ToExpression()).Compile()());
        }

        [Fact]
        public void InstanceRestrictionEqualsIfAllSame()
        {
            Expression exp = Expression.Default(typeof(Egalitarian));
            Egalitarian inst = new Egalitarian();
            BindingRestrictions x = BindingRestrictions.GetInstanceRestriction(exp, inst);
            BindingRestrictions y = BindingRestrictions.GetInstanceRestriction(exp, inst);
            Assert.Equal(x, y);
            Assert.Equal(x.GetHashCode(), y.GetHashCode());
            x = BindingRestrictions.GetInstanceRestriction(exp, null);
            y = BindingRestrictions.GetInstanceRestriction(exp, null);
            Assert.Equal(x, y);
            Assert.Equal(x.GetHashCode(), y.GetHashCode());
        }

        [Fact]
        public void InstanceRestrictionNotEqualIfDifferentInstance()
        {
            Expression exp = Expression.Default(typeof(Egalitarian));
            BindingRestrictions x = BindingRestrictions.GetInstanceRestriction(exp, new Egalitarian());
            BindingRestrictions y = BindingRestrictions.GetInstanceRestriction(exp, new Egalitarian());
            Assert.NotEqual(x, y);
        }

        [Fact]
        public void InstanceRestrictionNotEqualNull()
        {
            BindingRestrictions br = BindingRestrictions.GetInstanceRestriction(
                Expression.Default(typeof(Egalitarian)), null);
            Assert.False(br.Equals(null));
        }

        [Fact]
        public void InstanceRestrictionNotEqualIfDifferentExpression()
        {
            Egalitarian inst = new Egalitarian();
            BindingRestrictions x = BindingRestrictions.GetInstanceRestriction(
                Expression.Default(typeof(Egalitarian)), inst);
            BindingRestrictions y = BindingRestrictions.GetInstanceRestriction(
                Expression.Default(typeof(Egalitarian)), inst);
            Assert.NotEqual(x, y);
        }

        private static IEnumerable<object> SomeObjects()
        {
            yield return "";
            yield return 0;
            yield return new Uri("https://example.net/");
            yield return DateTime.MaxValue;
        }

        public static IEnumerable<object[]> ObjectsAsArguments => SomeObjects().Select(o => new[] {o});

        public static IEnumerable<object[]> ObjectsAndWrongTypes() => from obj in SomeObjects()
            from typeObj in SomeObjects()
            where obj.GetType() != typeObj.GetType()
            select new[] {obj, typeObj.GetType()};

        [Theory, MemberData(nameof(ObjectsAsArguments))]
        public void TypeRestrictionTrueForMatchType(object obj)
        {
            BindingRestrictions isType = BindingRestrictions.GetTypeRestriction(Expression.Constant(obj), obj.GetType());
            Assert.True(Expression.Lambda<Func<bool>>(isType.ToExpression()).Compile()());
        }

        [Theory, MemberData(nameof(ObjectsAndWrongTypes))]
        public void TypeRestrictionFalseForOtherType(object obj, Type type)
        {
            BindingRestrictions isType = BindingRestrictions.GetTypeRestriction(Expression.Constant(obj), type);
            Assert.False(Expression.Lambda<Func<bool>>(isType.ToExpression()).Compile()());
        }

        [Fact]
        public void TypeRestrictionEqualIfSameTypeAndExpression()
        {
            Expression exp = Expression.Default(typeof(Egalitarian));
            BindingRestrictions x = BindingRestrictions.GetTypeRestriction(exp, typeof(Egalitarian));
            BindingRestrictions y = BindingRestrictions.GetTypeRestriction(
                exp, typeof(Egalitarian).MakeArrayType().GetElementType());
            Assert.Equal(x, y);
            Assert.Equal(x.GetHashCode(), y.GetHashCode());
        }

        [Fact]
        public void TypeRestrictionNotEqualIfDifferentType()
        {
            Expression exp = Expression.Default(typeof(Egalitarian));
            BindingRestrictions x = BindingRestrictions.GetTypeRestriction(exp, typeof(Egalitarian));
            BindingRestrictions y = BindingRestrictions.GetTypeRestriction(exp, typeof(string));
            Assert.NotEqual(x, y);
        }

        [Fact]
        public void TypeRestrictionNotEqualIfDifferentExpression()
        {
            BindingRestrictions x = BindingRestrictions.GetTypeRestriction(
                Expression.Default(typeof(Egalitarian)), typeof(Egalitarian));
            BindingRestrictions y = BindingRestrictions.GetTypeRestriction(
                Expression.Default(typeof(Egalitarian)), typeof(Egalitarian));
            Assert.NotEqual(x, y);
        }

        [Fact]
        public void TypeRestrictionNotEqualNull()
        {
            BindingRestrictions br = BindingRestrictions.GetTypeRestriction(
                Expression.Default(typeof(Egalitarian)), typeof(Egalitarian));
            Assert.False(br.Equals(null));
        }
    }
}
