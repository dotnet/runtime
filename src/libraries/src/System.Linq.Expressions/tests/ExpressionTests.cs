// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    // Tests for features of the Expression class, rather than any derived types,
    // including how it acts with custom derived types.
    //
    // Unfortunately there is slightly different internal behaviour depending on whether
    // a derived Expression uses the new constructor, uses the old constructor, or uses
    // the new constructor after at least one use of the old constructor has been made,
    // due to static state being affected. For this reason some tests have to be done
    // in a particular order, with those for the old constructor coming after most of
    // the tests, and those affected by this being repeated after that.
    [TestCaseOrderer("System.Linq.Expressions.Tests.TestOrderer", "System.Linq.Expressions.Tests")]
    public class ExpressionTests
    {
        private static readonly Expression MarkerExtension = Expression.Constant(0);

        private class IncompleteExpressionOverride : Expression
        {
            public class Visitor : ExpressionVisitor
            {
                protected override Expression VisitExtension(Expression node) => MarkerExtension;
            }

            public IncompleteExpressionOverride()
                : base()
            {
            }

            public Expression VisitChildren() => VisitChildren(new Visitor());
        }

        private class ClaimedReducibleOverride : IncompleteExpressionOverride
        {
            public override bool CanReduce => true;
        }

        private class ReducesToSame : ClaimedReducibleOverride
        {
            public override Expression Reduce() => this;
        }

        private class ReducesToNull : ClaimedReducibleOverride
        {
            public override Expression Reduce() => null;
        }

        private class ReducesToLongTyped : ClaimedReducibleOverride
        {
            private class ReducedToLongTyped : IncompleteExpressionOverride
            {
                public override Type Type => typeof(long);
            }

            public override Type Type => typeof(int);

            public override Expression Reduce() => new ReducedToLongTyped();
        }

        private class Reduces : ClaimedReducibleOverride
        {
            public override Type Type => typeof(int);

            public override Expression Reduce() => new Reduces();
        }

        private class ReducesFromStrangeNodeType : Expression
        {
            public override Type Type => typeof(int);

            public override ExpressionType NodeType => (ExpressionType)(-1);

            public override bool CanReduce => true;

            public override Expression Reduce() => Constant(3);
        }

        private class ObsoleteIncompleteExpressionOverride : Expression
        {
#pragma warning disable 0618 // Testing obsolete behaviour.
            public ObsoleteIncompleteExpressionOverride(ExpressionType nodeType, Type type)
                : base(nodeType, type)
            {
            }
#pragma warning restore 0618
        }

        private class IrreducibleWithTypeAndNodeType : Expression
        {
            public override Type Type => typeof(void);

            public override ExpressionType NodeType => ExpressionType.Extension;
        }

        private class IrreduceibleWithTypeAndStrangeNodeType : Expression
        {
            public override Type Type => typeof(void);

            public override ExpressionType NodeType => (ExpressionType)(-1);
        }

        private class ExtensionNoToString : Expression
        {
            public override ExpressionType NodeType => ExpressionType.Extension;
            public override Type Type => typeof(int);
            public override bool CanReduce => false;
        }

        private class ExtensionToString : Expression
        {
            public override ExpressionType NodeType => ExpressionType.Extension;
            public override Type Type => typeof(int);
            public override bool CanReduce => false;

            public override string ToString() => "bar";
        }

        public static IEnumerable<object[]> AllNodeTypesPlusSomeInvalid
        {
            get
            {
                foreach (ExpressionType et in Enum.GetValues(typeof(ExpressionType)))
                    yield return new object[] { et };
                yield return new object[] { (ExpressionType)(-1) };
                yield return new object[] { (ExpressionType)int.MaxValue };
                yield return new object[] { (ExpressionType)int.MinValue };
            }
        }

        public static IEnumerable<object[]> SomeTypes => new[] { typeof(int), typeof(void), typeof(object), typeof(DateTime), typeof(string), typeof(ExpressionTests), typeof(ExpressionType) }
    .Select(type => new object[] { type });

        [Fact]
        public void NodeTypeMustBeOverridden()
        {
            var exp = new IncompleteExpressionOverride();
            Assert.Throws<InvalidOperationException>(() => exp.NodeType);
        }

        [Theory, TestOrder(1), MemberData(nameof(AllNodeTypesPlusSomeInvalid))]
        public void NodeTypeFromConstructor(ExpressionType nodeType)
        {
            Assert.Equal(nodeType, new ObsoleteIncompleteExpressionOverride(nodeType, typeof(int)).NodeType);
        }

        [Fact, TestOrder(2)]
        public void NodeTypeMustBeOverriddenAfterObsoleteConstructorUsed()
        {
            var exp = new IncompleteExpressionOverride();
            Assert.Throws<InvalidOperationException>(() => exp.NodeType);
        }

        [Fact]
        public void TypeMustBeOverridden()
        {
            var exp = new IncompleteExpressionOverride();
            Assert.Throws<InvalidOperationException>(() => exp.Type);
        }

        [Theory, TestOrder(1), MemberData(nameof(SomeTypes))]
        public void TypeFromConstructor(Type type)
        {
            Assert.Equal(type, new ObsoleteIncompleteExpressionOverride(ExpressionType.Constant, type).Type);
        }

        [Fact, TestOrder(1)]
        public void TypeMayBeNonNullOnObsoleteConstructedExpression()
        {
            // This is probably undesirable, but throwing here would be a breaking change.
            // Impact must be considered before prohibiting this.
            Assert.Null(new ObsoleteIncompleteExpressionOverride(ExpressionType.Add, null).Type);
        }

        [Fact, TestOrder(2)]
        public void TypeMustBeOverriddenCheckCorrectAfterObsoleteConstructorUsed()
        {
            var exp = new IncompleteExpressionOverride();
            Assert.Throws<InvalidOperationException>(() => exp.Type);
        }

        [Fact]
        public void DefaultCannotReduce()
        {
            Assert.False(new IncompleteExpressionOverride().CanReduce);
        }

        [Fact]
        public void DefaultReducesToSame()
        {
            var exp = new IncompleteExpressionOverride();
            Assert.Same(exp, exp.Reduce());
        }

        [Fact]
        public void VisitChildrenThrowsAsNotReducible()
        {
            var exp = new IncompleteExpressionOverride();
            AssertExtensions.Throws<ArgumentException>(null, () => exp.VisitChildren());
        }

        [Fact]
        public void CanVisitChildrenIfReallyReduces()
        {
            var exp = new Reduces();
            Assert.NotSame(exp, exp.VisitChildren());
        }

        [Fact]
        public void VisitingCallsVisitExtension()
        {
            Assert.Same(MarkerExtension, new IncompleteExpressionOverride.Visitor().Visit(new IncompleteExpressionOverride()));
        }

        [Fact]
        public void ReduceAndCheckThrowsByDefault()
        {
            var exp = new IncompleteExpressionOverride();
            AssertExtensions.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public void ReduceExtensionsThrowsByDefault()
        {
            var exp = new IncompleteExpressionOverride();
            AssertExtensions.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public void IfClaimCanReduceMustReduce()
        {
            var exp = new ClaimedReducibleOverride();
            AssertExtensions.Throws<ArgumentException>(null, () => exp.Reduce());
        }

        [Fact]
        public void ReduceAndCheckThrowOnReduceToSame()
        {
            var exp = new ReducesToSame();
            AssertExtensions.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public void ReduceAndCheckThrowOnReduceToNull()
        {
            var exp = new ReducesToNull();
            AssertExtensions.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public void ReduceAndCheckThrowOnReducedTypeNotAssignable()
        {
            var exp = new ReducesToLongTyped();
            AssertExtensions.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

#pragma warning disable 0169, 0414 // Accessed through reflection.
        private static int TestField;
        private const int TestConstant = 0;
        private static readonly int TestInitOnlyField = 0;
#pragma warning restore 0169, 0414

        private static int Unreadable
        {
            set { }
        }

        private static int Unwritable => 0;

        private class UnreadableIndexableClass
        {
            public int this[int index]
            {
                set { }
            }
        }

        private class UnwritableIndexableClass
        {
            public int this[int index] => 0;
        }

        [Fact]
        public void ConfirmCanRead()
        {
            var readableExpressions = new Expression[]
            {
                Expression.Constant(0),
                Expression.Add(Expression.Constant(0), Expression.Constant(0)),
                Expression.Default(typeof(int)),
                Expression.Property(Expression.Constant(new List<int>()), "Count"),
                Expression.ArrayIndex(Expression.Constant(Array.Empty<int>()), Expression.Constant(0)),
                Expression.Field(null, typeof(ExpressionTests), "TestField"),
                Expression.Field(null, typeof(ExpressionTests), "TestConstant"),
                Expression.Field(null, typeof(ExpressionTests), "TestInitOnlyField")
            };
            Expression.Block(typeof(void), readableExpressions);
        }

        public static IEnumerable<Expression> UnreadableExpressions
        {
            get
            {
                yield return Expression.Property(null, typeof(ExpressionTests), "Unreadable");
                yield return Expression.Property(Expression.Constant(new UnreadableIndexableClass()), "Item", Expression.Constant(0));
            }
        }

        public static IEnumerable<object[]> UnreadableExpressionData => UnreadableExpressions.Concat(new Expression[1]).Select(exp => new object[] { exp });

        public static IEnumerable<Expression> WritableExpressions
        {
            get
            {
                yield return Expression.Property(null, typeof(ExpressionTests), "Unreadable");
                yield return Expression.Property(Expression.Constant(new UnreadableIndexableClass()), "Item", Expression.Constant(0));
                yield return Expression.Field(null, typeof(ExpressionTests), "TestField");
                yield return Expression.Parameter(typeof(int));
            }
        }

        public static IEnumerable<Expression> UnwritableExpressions
        {
            get
            {
                yield return Expression.Property(null, typeof(ExpressionTests), "Unwritable");
                yield return Expression.Property(Expression.Constant(new UnwritableIndexableClass()), "Item", Expression.Constant(0));
                yield return Expression.Field(null, typeof(ExpressionTests), "TestConstant");
                yield return Expression.Field(null, typeof(ExpressionTests), "TestInitOnlyField");
                yield return Expression.Call(Expression.Default(typeof(ExpressionTests)), "ConfirmCannotReadSequence", new Type[0]);
                yield return null;
            }
        }

        public static IEnumerable<object[]> UnwritableExpressionData => UnwritableExpressions.Select(exp => new object[] { exp });

        public static IEnumerable<object[]> WritableExpressionData => WritableExpressions.Select(exp => new object[] { exp });

        [Theory, MemberData(nameof(UnreadableExpressionData))]
        public void ConfirmCannotRead(Expression unreadableExpression)
        {
            if (unreadableExpression == null)
                AssertExtensions.Throws<ArgumentNullException>("expression", () => Expression.Increment(unreadableExpression));
            else
                AssertExtensions.Throws<ArgumentException>("expression", () => Expression.Increment(unreadableExpression));
        }

        [Fact]
        public void ConfirmCannotReadSequence()
        {
            AssertExtensions.Throws<ArgumentException>("expressions[0]", () => Expression.Block(typeof(void), UnreadableExpressions));
        }

        [Theory, MemberData(nameof(UnwritableExpressionData))]
        public void ConfirmCannotWrite(Expression unwritableExpression)
        {
            if (unwritableExpression == null)
                AssertExtensions.Throws<ArgumentNullException>("left", () => Expression.Assign(unwritableExpression, Expression.Constant(0)));
            else
                AssertExtensions.Throws<ArgumentException>("left", () => Expression.Assign(unwritableExpression, Expression.Constant(0)));
        }

        [Theory, MemberData(nameof(WritableExpressionData))]
        public void ConfirmCanWrite(Expression writableExpression)
        {
            Expression.Assign(writableExpression, Expression.Default(writableExpression.Type));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void CompileIrreduciebleExtension(bool useInterpreter)
        {
            Expression<Action> exp = Expression.Lambda<Action>(new IrreducibleWithTypeAndNodeType());
            AssertExtensions.Throws<ArgumentException>(null, () => exp.Compile(useInterpreter));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void CompileIrreduciebleStrangeNodeTypeExtension(bool useInterpreter)
        {
            Expression<Action> exp = Expression.Lambda<Action>(new IrreduceibleWithTypeAndStrangeNodeType());
            AssertExtensions.Throws<ArgumentException>(null, () => exp.Compile(useInterpreter));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void CompileReducibleStrangeNodeTypeExtension(bool useInterpreter)
        {
            Expression<Func<int>> exp = Expression.Lambda<Func<int>>(new ReducesFromStrangeNodeType());
            Assert.Equal(3, exp.Compile(useInterpreter)());
        }

        [Fact]
        public void ToStringTest()
        {
            var e1 = new ExtensionNoToString();
            Assert.Equal($"[{typeof(ExtensionNoToString).FullName}]", e1.ToString());

            BinaryExpression e2 = Expression.Add(Expression.Constant(1), new ExtensionToString());
            Assert.Equal($"(1 + bar)", e2.ToString());
        }
    }
}
