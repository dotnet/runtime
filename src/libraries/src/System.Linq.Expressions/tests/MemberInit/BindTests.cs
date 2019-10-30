// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
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
            public string ReadonlyStringProperty => "";
            public static string StaticStringProperty { get; set; }
            public static string StaticStringField;
            public static string StaticReadonlyStringProperty => "";
            public static readonly string StaticReadonlyStringField = "";
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

        private class GenericType<T>
        {
            public int AlwaysInt32 { get; set; }
        }

        [Fact]
        public void NullPropertyAccessor()
        {
            AssertExtensions.Throws<ArgumentNullException>("propertyAccessor", () => Expression.Bind(default(MethodInfo), Expression.Constant(0)));
        }

        [Fact]
        public void NullMember()
        {
            AssertExtensions.Throws<ArgumentNullException>("member", () => Expression.Bind(default(MemberInfo), Expression.Constant(0)));
        }

        [Fact]
        public void NullExpression()
        {
            MemberInfo member = typeof(PropertyAndFields).GetMember(nameof(PropertyAndFields.StringProperty))[0];
            PropertyInfo property = typeof(PropertyAndFields).GetProperty(nameof(PropertyAndFields.StringProperty));
            AssertExtensions.Throws<ArgumentNullException>("expression", () => Expression.Bind(member, null));
            AssertExtensions.Throws<ArgumentNullException>("expression", () => Expression.Bind(property, null));
        }

        [Fact]
        public void ReadOnlyMember()
        {
            AssertExtensions.Throws<ArgumentException>("member", () => Expression.Bind(typeof(string).GetProperty(nameof(string.Length)), Expression.Constant(0)));
            AssertExtensions.Throws<ArgumentException>("member", () => Expression.Bind(typeof(string).GetMember(nameof(string.Length))[0], Expression.Constant(0)));
        }

        [Fact]
        public void WriteOnlyExpression()
        {
            MemberInfo member = typeof(PropertyAndFields).GetMember(nameof(PropertyAndFields.StringProperty))[0];
            PropertyInfo property = typeof(PropertyAndFields).GetProperty(nameof(PropertyAndFields.StringProperty));
            Expression expression = Expression.Property(Expression.Constant(new Unreadable<string>()), typeof(Unreadable<string>), nameof(Unreadable<string>.WriteOnly));
            AssertExtensions.Throws<ArgumentException>("expression", () => Expression.Bind(member, expression));
            AssertExtensions.Throws<ArgumentException>("expression", () => Expression.Bind(property, expression));
        }

        [Fact]
        public void MethodForMember()
        {
            MemberInfo member = typeof(PropertyAndFields).GetMember(nameof(object.ToString))[0];
            MethodInfo method = typeof(PropertyAndFields).GetMethod(nameof(object.ToString));
            AssertExtensions.Throws<ArgumentException>("member", () => Expression.Bind(member, Expression.Constant("")));
            AssertExtensions.Throws<ArgumentException>("propertyAccessor", () => Expression.Bind(method, Expression.Constant("")));
        }

        [Fact]
        public void ExpressionTypeNotAssignable()
        {
            MemberInfo member = typeof(PropertyAndFields).GetMember(nameof(PropertyAndFields.StringProperty))[0];
            PropertyInfo property = typeof(PropertyAndFields).GetProperty(nameof(PropertyAndFields.StringProperty));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Bind(member, Expression.Constant(0)));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Bind(property, Expression.Constant(0)));
        }

        [Fact]
        public void OpenGenericTypesMember()
        {
            MemberInfo member = typeof(Unreadable<>).GetMember("WriteOnly")[0];
            PropertyInfo property = typeof(Unreadable<>).GetProperty("WriteOnly");
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Bind(member, Expression.Constant(0)));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Bind(property, Expression.Constant(0)));
        }

        [Fact]
        public void OpenGenericTypesNonGenericMember()
        {
            MemberInfo member = typeof(GenericType<>).GetMember(nameof(GenericType<int>.AlwaysInt32))[0];
            PropertyInfo property = typeof(GenericType<>).GetProperty(nameof(GenericType<int>.AlwaysInt32));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Bind(member, Expression.Constant(0)));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Bind(property, Expression.Constant(0)));
        }

        [Fact]
        public void MustBeMemberOfType()
        {
            NewExpression newExp = Expression.New(typeof(UriBuilder));
            MemberAssignment bind = Expression.Bind(
                typeof(PropertyAndFields).GetProperty(nameof(PropertyAndFields.StringProperty)),
                Expression.Constant("value")
                );
            AssertExtensions.Throws<ArgumentException>("bindings[0]", () => Expression.MemberInit(newExp, bind));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void MemberAssignmentFromMember(bool useInterpreter)
        {
            PropertyAndFields result = Expression.Lambda<Func<PropertyAndFields>>(
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

        [Theory, ClassData(typeof(CompilationTypes))]
        public void ConstantField(bool useInterpreter)
        {
            MemberInfo member = typeof(PropertyAndFields).GetMember(nameof(PropertyAndFields.ConstantString))[0];
            Expression<Func<PropertyAndFields>> attemptAssignToConstant = Expression.Lambda<Func<PropertyAndFields>>(
                Expression.MemberInit(
                    Expression.New(typeof(PropertyAndFields)),
                    Expression.Bind(member, Expression.Constant(""))
                    )
                );

            Assert.Throws<NotSupportedException>(() => attemptAssignToConstant.Compile(useInterpreter));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void ReadonlyField(bool useInterpreter)
        {
            MemberInfo member = typeof(PropertyAndFields).GetMember(nameof(PropertyAndFields.ReadonlyStringField))[0];
            Expression<Func<PropertyAndFields>> assignToReadonly = Expression.Lambda<Func<PropertyAndFields>>(
                Expression.MemberInit(
                    Expression.New(typeof(PropertyAndFields)),
                    Expression.Bind(member, Expression.Constant("ABC"))
                    )
                );
            Func<PropertyAndFields> func = assignToReadonly.Compile(useInterpreter);
            Assert.Equal("ABC", func().ReadonlyStringField);
        }

        [Fact]
        public void ReadonlyProperty()
        {
            MemberInfo member = typeof(PropertyAndFields).GetMember(nameof(PropertyAndFields.ReadonlyStringProperty))[0];
            PropertyInfo property = typeof(PropertyAndFields).GetProperty(nameof(PropertyAndFields.ReadonlyStringProperty));
            AssertExtensions.Throws<ArgumentException>("member", () => Expression.Bind(member, Expression.Constant("")));
            AssertExtensions.Throws<ArgumentException>("member", () => Expression.Bind(property, Expression.Constant("")));
        }

        [Fact]
        public void StaticReadonlyProperty()
        {
            MemberInfo member = typeof(PropertyAndFields).GetMember(nameof(PropertyAndFields.StaticReadonlyStringProperty))[0];
            PropertyInfo property = typeof(PropertyAndFields).GetProperty(nameof(PropertyAndFields.StaticReadonlyStringProperty));
            AssertExtensions.Throws<ArgumentException>("member", () => Expression.Bind(member, Expression.Constant("")));
            AssertExtensions.Throws<ArgumentException>("member", () => Expression.Bind(property, Expression.Constant("")));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void StaticField(bool useInterpreter)
        {
            MemberInfo member = typeof(PropertyAndFields).GetMember(nameof(PropertyAndFields.StaticStringField))[0];
            Expression<Func<PropertyAndFields>> assignToReadonly = Expression.Lambda<Func<PropertyAndFields>>(
                Expression.MemberInit(
                    Expression.New(typeof(PropertyAndFields)),
                    Expression.Bind(member, Expression.Constant("ABC"))
                    )
                );
            Func<PropertyAndFields> func = assignToReadonly.Compile(useInterpreter);
            PropertyAndFields.StaticStringField = "123";
            func();
            Assert.Equal("ABC", PropertyAndFields.StaticStringField);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void StaticProperty(bool useInterpreter)
        {
            MemberInfo member = typeof(PropertyAndFields).GetMember(nameof(PropertyAndFields.StaticStringProperty))[0];
            Expression<Func<PropertyAndFields>> assignToStaticProperty = Expression.Lambda<Func<PropertyAndFields>>(
                Expression.MemberInit(
                    Expression.New(typeof(PropertyAndFields)),
                    Expression.Bind(member, Expression.Constant("ABC"))
                    )
                );
            Assert.Throws<InvalidProgramException>(() => assignToStaticProperty.Compile(useInterpreter));
        }

        [Fact]
        public void UpdateDifferentReturnsDifferent()
        {
            MemberAssignment bind = Expression.Bind(typeof(PropertyAndFields).GetProperty("StringProperty"), Expression.Constant("Hello Property"));
            Assert.NotSame(bind, Expression.Default(typeof(string)));
        }

        [Fact]
        public void UpdateSameReturnsSame()
        {
            MemberAssignment bind = Expression.Bind(typeof(PropertyAndFields).GetProperty("StringProperty"), Expression.Constant("Hello Property"));
            Assert.Same(bind, bind.Update(bind.Expression));
        }

        [Fact]
        public void MemberBindingTypeAssignment()
        {
            MemberAssignment bind = Expression.Bind(typeof(PropertyAndFields).GetProperty("StringProperty"), Expression.Constant("Hello Property"));
            Assert.Equal(MemberBindingType.Assignment, bind.BindingType);
        }

        private class BogusBinding : MemberBinding
        {
            public BogusBinding(MemberBindingType type, MemberInfo member)
#pragma warning disable 618
                : base(type, member)
#pragma warning restore 618
            {
            }

            public override string ToString() => ""; // Called internal to test framework and default would throw.
        }

        public static IEnumerable<object[]> BogusBindings()
        {
            MemberInfo member = typeof(PropertyAndFields).GetMember(nameof(PropertyAndFields.StaticReadonlyStringField))[0];
            foreach (MemberBindingType type in new[] {MemberBindingType.Assignment, MemberBindingType.ListBinding, MemberBindingType.MemberBinding, (MemberBindingType)(-1)})
            {
                yield return new object[] {new BogusBinding(type, member)};
            }
        }

        [Theory, MemberData(nameof(BogusBindings))]
        public void BogusBindingType(MemberBinding binding)
        {
            AssertExtensions.Throws<ArgumentException>("bindings[0]", () => Expression.MemberInit(Expression.New(typeof(PropertyAndFields)), binding));
        }

#if FEATURE_COMPILE
        [Fact]
        public void GlobalMethod()
        {
            ModuleBuilder module = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), AssemblyBuilderAccess.RunAndCollect).DefineDynamicModule("Module");
            MethodBuilder globalMethod = module.DefineGlobalMethod("GlobalMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new [] {typeof(int)});
            globalMethod.GetILGenerator().Emit(OpCodes.Ret);
            module.CreateGlobalFunctions();
            MethodInfo globalMethodInfo = module.GetMethod(globalMethod.Name);
            AssertExtensions.Throws<ArgumentException>("propertyAccessor", () => Expression.Bind(globalMethodInfo, Expression.Constant(2)));
        }

        [Fact]
        public void GlobalField()
        {
            ModuleBuilder module = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), AssemblyBuilderAccess.RunAndCollect).DefineDynamicModule("Module");
            FieldBuilder fieldBuilder = module.DefineInitializedData("GlobalField", new byte[4], FieldAttributes.Public);
            module.CreateGlobalFunctions();
            FieldInfo globalField = module.GetField(fieldBuilder.Name);
            AssertExtensions.Throws<ArgumentException>("member", () => Expression.Bind(globalField, Expression.Default(globalField.FieldType)));
        }
#endif
    }
}
