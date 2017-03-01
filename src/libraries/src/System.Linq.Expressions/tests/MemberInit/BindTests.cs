// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            NewExpression newExp = Expression.New(typeof(UriBuilder));
            MemberAssignment bind = Expression.Bind(
                typeof(PropertyAndFields).GetProperty(nameof(PropertyAndFields.StringProperty)),
                Expression.Constant("value")
                );
            Assert.Throws<ArgumentException>("bindings[0]", () => Expression.MemberInit(newExp, bind));
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
            Assert.Throws<ArgumentException>("member", () => Expression.Bind(member, Expression.Constant("")));
            Assert.Throws<ArgumentException>("member", () => Expression.Bind(property, Expression.Constant("")));
        }

        [Fact]
        public void StaticReadonlyProperty()
        {
            MemberInfo member = typeof(PropertyAndFields).GetMember(nameof(PropertyAndFields.StaticReadonlyStringProperty))[0];
            PropertyInfo property = typeof(PropertyAndFields).GetProperty(nameof(PropertyAndFields.StaticReadonlyStringProperty));
            Assert.Throws<ArgumentException>("member", () => Expression.Bind(member, Expression.Constant("")));
            Assert.Throws<ArgumentException>("member", () => Expression.Bind(property, Expression.Constant("")));
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
        public void StaticReadonlyField(bool useInterpreter)
        {
            MemberInfo member = typeof(PropertyAndFields).GetMember(nameof(PropertyAndFields.StaticReadonlyStringField))[0];
            Expression<Func<PropertyAndFields>> assignToReadonly = Expression.Lambda<Func<PropertyAndFields>>(
                Expression.MemberInit(
                    Expression.New(typeof(PropertyAndFields)),
                    Expression.Bind(member, Expression.Constant("ABC" + useInterpreter))
                    )
                );
            Func<PropertyAndFields> func = assignToReadonly.Compile(useInterpreter);
            func();
            Assert.Equal("ABC" + useInterpreter, PropertyAndFields.StaticReadonlyStringField);
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

        [Fact]
        public void GlobalMethod()
        {
            ModuleBuilder module = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), AssemblyBuilderAccess.Run).DefineDynamicModule("Module");
            MethodBuilder globalMethod = module.DefineGlobalMethod("GlobalMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new [] {typeof(int)});
            globalMethod.GetILGenerator().Emit(OpCodes.Ret);
            module.CreateGlobalFunctions();
            MethodInfo globalMethodInfo = module.GetMethod(globalMethod.Name);
            Assert.Throws<ArgumentException>("propertyAccessor", () => Expression.Bind(globalMethodInfo, Expression.Constant(2)));
        }

        [Fact]
        public void GlobalField()
        {
            ModuleBuilder module = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), AssemblyBuilderAccess.Run).DefineDynamicModule("Module");
            FieldBuilder fieldBuilder = module.DefineInitializedData("GlobalField", new byte[4], FieldAttributes.Public);
            module.CreateGlobalFunctions();
            FieldInfo globalField = module.GetField(fieldBuilder.Name);
            Assert.Throws<ArgumentException>("member", () => Expression.Bind(globalField, Expression.Default(globalField.FieldType)));
        }
    }
}
