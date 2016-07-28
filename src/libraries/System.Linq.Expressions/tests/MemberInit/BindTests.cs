// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class BindTests
    {
        private class PropertyAndFields
        {
#pragma warning disable 649 // Assigned through expressions.
            public string StringProperty { get; set; }
            public string StringField;
            public readonly string ReadonlyStringField;
            public string ReadonlyStringProperty { get { return ""; } }
            public static string StaticStringProperty { get; set; }
            public static string StaticStringField;
            public const string ConstantString = "Constant";
#pragma warning restore 649
        }

        private class Unreadable<T>
        {
            public T WriteOnly
            {
                set { }
            }
        }

        [Fact]
        public void NullPropertyAccessor()
        {
            Assert.Throws<ArgumentNullException>("propertyAccessor", () => Expression.Bind(default(MethodInfo), Expression.Constant(0)));
        }

        [Fact]
        public void NullMember()
        {
            Assert.Throws<ArgumentNullException>("member", () => Expression.Bind(default(MemberInfo), Expression.Constant(0)));
        }

        [Fact]
        public void NullExpression()
        {
            MemberInfo member = typeof(PropertyAndFields).GetMember(nameof(PropertyAndFields.StringProperty))[0];
            PropertyInfo property = typeof(PropertyAndFields).GetProperty(nameof(PropertyAndFields.StringProperty));
            Assert.Throws<ArgumentNullException>("expression", () => Expression.Bind(member, null));
            Assert.Throws<ArgumentNullException>("expression", () => Expression.Bind(property, null));
        }

        [Fact]
        public void ReadOnlyMember()
        {
            Assert.Throws<ArgumentException>("member", () => Expression.Bind(typeof(string).GetProperty(nameof(string.Length)), Expression.Constant(0)));
            Assert.Throws<ArgumentException>("member", () => Expression.Bind(typeof(string).GetMember(nameof(string.Length))[0], Expression.Constant(0)));
        }

        [Fact]
        public void WriteOnlyExpression()
        {
            MemberInfo member = typeof(PropertyAndFields).GetMember(nameof(PropertyAndFields.StringProperty))[0];
            PropertyInfo property = typeof(PropertyAndFields).GetProperty(nameof(PropertyAndFields.StringProperty));
            Expression expression = Expression.Property(Expression.Constant(new Unreadable<string>()), typeof(Unreadable<string>), nameof(Unreadable<string>.WriteOnly));
            Assert.Throws<ArgumentException>("expression", () => Expression.Bind(member, expression));
            Assert.Throws<ArgumentException>("expression", () => Expression.Bind(property, expression));
        }

        [Fact]
        public void MethodForMember()
        {
            MemberInfo member = typeof(PropertyAndFields).GetMember(nameof(object.ToString))[0];
            MethodInfo method = typeof(PropertyAndFields).GetMethod(nameof(object.ToString));
            Assert.Throws<ArgumentException>("member", () => Expression.Bind(member, Expression.Constant("")));
            Assert.Throws<ArgumentException>("propertyAccessor", () => Expression.Bind(method, Expression.Constant("")));
        }

        [Fact]
        public void ExpressionTypeNotAssignable()
        {
            MemberInfo member = typeof(PropertyAndFields).GetMember(nameof(PropertyAndFields.StringProperty))[0];
            PropertyInfo property = typeof(PropertyAndFields).GetProperty(nameof(PropertyAndFields.StringProperty));
            Assert.Throws<ArgumentException>(null, () => Expression.Bind(member, Expression.Constant(0)));
            Assert.Throws<ArgumentException>(null, () => Expression.Bind(property, Expression.Constant(0)));
        }

        [Fact]
        public void OpenGenericTypesMember()
        {
            MemberInfo member = typeof(Unreadable<>).GetMember("WriteOnly")[0];
            PropertyInfo property = typeof(Unreadable<>).GetProperty("WriteOnly");
            Assert.Throws<ArgumentException>(null, () => Expression.Bind(member, Expression.Constant(0)));
            Assert.Throws<ArgumentException>(null, () => Expression.Bind(property, Expression.Constant(0)));
        }

        [Fact]
        public void MustBeMemberOfType()
        {
            var newExp = Expression.New(typeof(UriBuilder));
            var bind = Expression.Bind(
                typeof(PropertyAndFields).GetProperty(nameof(PropertyAndFields.StringProperty)),
                Expression.Constant("value")
                );
            Assert.Throws<ArgumentException>("bindings[0]", () => Expression.MemberInit(newExp, bind));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void MemberAssignmentFromMember(bool useInterpreter)
        {
            PropertyAndFields result  = Expression.Lambda<Func<PropertyAndFields>>(
                Expression.MemberInit(
                    Expression.New(typeof(PropertyAndFields)),
                    Expression.Bind(
                        typeof(PropertyAndFields).GetMember("StringProperty")[0],
                        Expression.Constant("Hello Property")
                        ),
                    Expression.Bind(
                        typeof(PropertyAndFields).GetMember("StringField")[0],
                        Expression.Constant("Hello Field")
                        )
                )
            ).Compile(useInterpreter)();

            Assert.Equal("Hello Property", result.StringProperty);
            Assert.Equal("Hello Field", result.StringField);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void MemberAssignmentFromMethodInfo(bool useInterpreter)
        {
            PropertyAndFields result = Expression.Lambda<Func<PropertyAndFields>>(
                Expression.MemberInit(
                    Expression.New(typeof(PropertyAndFields)),
                    Expression.Bind(
                        typeof(PropertyAndFields).GetProperty("StringProperty"),
                        Expression.Constant("Hello Property")
                        )
                )
            ).Compile(useInterpreter)();

            Assert.Equal("Hello Property", result.StringProperty);
        }

        [Fact]
        [ActiveIssue(5963)]
        public void ConstantField()
        {
            MemberInfo member = typeof(PropertyAndFields).GetMember("Constant")[0];
            Assert.Throws<ArgumentException>("member", () => Expression.Bind(member, Expression.Constant("")));
        }

        [Fact]
        [ActiveIssue(5963)]
        public void ReadonlyField()
        {
            MemberInfo member = typeof(PropertyAndFields).GetMember("ReadonlyStringField")[0];
            Assert.Throws<ArgumentException>("member", () => Expression.Bind(member, Expression.Constant("")));
        }

        [Fact]
        public void ReadonlyProperty()
        {
            MemberInfo member = typeof(PropertyAndFields).GetMember("ReadonlyStringProperty")[0];
            PropertyInfo property = typeof(PropertyAndFields).GetProperty("ReadonlyStringProperty");
            Assert.Throws<ArgumentException>("member", () => Expression.Bind(member, Expression.Constant("")));
            Assert.Throws<ArgumentException>("member", () => Expression.Bind(property, Expression.Constant("")));
        }

        [Fact]
        [ActiveIssue(5963)]
        public void StaticField()
        {
            MemberInfo member = typeof(PropertyAndFields).GetMember("StaticStringField")[0];
            Assert.Throws<ArgumentException>("member", () => Expression.Bind(member, Expression.Constant("")));
        }

        [Fact]
        [ActiveIssue(5963)]
        public void StaticProperty()
        {
            MemberInfo member = typeof(PropertyAndFields).GetMember("StaticStringProperty")[0];
            PropertyInfo property = typeof(PropertyAndFields).GetProperty("StaticStringProperty");
            Assert.Throws<ArgumentException>("member", () => Expression.Bind(member, Expression.Constant("")));
            Assert.Throws<ArgumentException>("member", () => Expression.Bind(property, Expression.Constant("")));
        }

        [Fact]
        public void UpdateDifferentReturnsDifferent()
        {
            var bind = Expression.Bind(typeof(PropertyAndFields).GetProperty("StringProperty"), Expression.Constant("Hello Property"));
            Assert.NotSame(bind, Expression.Default(typeof(string)));
        }

        [Fact]
        public void UpdateSameReturnsSame()
        {
            var bind = Expression.Bind(typeof(PropertyAndFields).GetProperty("StringProperty"), Expression.Constant("Hello Property"));
            Assert.Same(bind, bind.Update(bind.Expression));
        }

        [Fact]
        public void MemberBindingTypeAssignment()
        {
            var bind = Expression.Bind(typeof(PropertyAndFields).GetProperty("StringProperty"), Expression.Constant("Hello Property"));
            Assert.Equal(MemberBindingType.Assignment, bind.BindingType);
        }
    }
}
