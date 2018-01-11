// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class MemberBindTests
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

        public class Inner
        {
            public int Value { get; set; }
        }

        public class Outer
        {
            public Inner InnerProperty { get; set; } = new Inner();
            public Inner InnerField = new Inner();
            public Inner ReadonlyInnerProperty { get; } = new Inner();
            public Inner WriteonlyInnerProperty { set { } }
            public readonly Inner ReadonlyInnerField = new Inner();
            public static Inner StaticInnerProperty { get; set; } = new Inner();
            public static Inner StaticInnerField = new Inner();
            public static Inner StaticReadonlyInnerProperty { get; } = new Inner();
            public static readonly Inner StaticReadonlyInnerField = new Inner();
            public static Inner StaticWriteonlyInnerProperty { set { } }
        }

        [Fact]
        public void NullMethodOrMemberInfo()
        {
            AssertExtensions.Throws<ArgumentNullException>("member", () => Expression.MemberBind(default(MemberInfo)));
            AssertExtensions.Throws<ArgumentNullException>("member", () => Expression.MemberBind(default(MemberInfo), Enumerable.Empty<MemberBinding>()));
            AssertExtensions.Throws<ArgumentNullException>("propertyAccessor", () => Expression.MemberBind(default(MethodInfo)));
            AssertExtensions.Throws<ArgumentNullException>("propertyAccessor", () => Expression.MemberBind(default(MethodInfo), Enumerable.Empty<MemberBinding>()));
        }

        [Fact]
        public void NullBindings()
        {
            PropertyInfo mem = typeof(PropertyAndFields).GetProperty(nameof(PropertyAndFields.StringProperty));
            MethodInfo meth = mem.GetGetMethod();
            AssertExtensions.Throws<ArgumentNullException>("bindings", () => Expression.MemberBind(mem, default(MemberBinding[])));
            AssertExtensions.Throws<ArgumentNullException>("bindings", () => Expression.MemberBind(mem, default(IEnumerable<MemberBinding>)));
            AssertExtensions.Throws<ArgumentNullException>("bindings", () => Expression.MemberBind(meth, default(MemberBinding[])));
            AssertExtensions.Throws<ArgumentNullException>("bindings", () => Expression.MemberBind(meth, default(IEnumerable<MemberBinding>)));
        }

        [Fact]
        public void NullBindingInBindings()
        {
            PropertyInfo mem = typeof(PropertyAndFields).GetProperty(nameof(PropertyAndFields.StringProperty));
            MethodInfo meth = mem.GetGetMethod();
            AssertExtensions.Throws<ArgumentNullException>("bindings", () => Expression.MemberBind(mem, default(MemberBinding)));
            AssertExtensions.Throws<ArgumentNullException>("bindings", () => Expression.MemberBind(mem, Enumerable.Repeat<MemberBinding>(null, 1)));
            AssertExtensions.Throws<ArgumentNullException>("bindings", () => Expression.MemberBind(meth, default(MemberBinding)));
            AssertExtensions.Throws<ArgumentNullException>("bindings", () => Expression.MemberBind(meth, Enumerable.Repeat<MemberBinding>(null, 1)));
        }

        [Fact]
        public void BindMethodMustBeProperty()
        {
            MemberInfo toString = typeof(object).GetMember(nameof(ToString))[0];
            MethodInfo toStringMeth = typeof(object).GetMethod(nameof(ToString));
            AssertExtensions.Throws<ArgumentException>("member", () => Expression.MemberBind(toString));
            AssertExtensions.Throws<ArgumentException>("member", () => Expression.MemberBind(toString, Enumerable.Empty<MemberBinding>()));
            AssertExtensions.Throws<ArgumentException>("propertyAccessor", () => Expression.MemberBind(toStringMeth));
            AssertExtensions.Throws<ArgumentException>("propertyAccessor", () => Expression.MemberBind(toStringMeth, Enumerable.Empty<MemberBinding>()));
        }

        [Fact]
        public void MemberBindingMustBeMemberOfType()
        {
            MemberMemberBinding bind = Expression.MemberBind(
                typeof(Outer).GetProperty(nameof(Outer.InnerProperty)),
                Expression.Bind(typeof(Inner).GetProperty(nameof(Inner.Value)), Expression.Constant(3))
                );
            NewExpression newExp = Expression.New(typeof(PropertyAndFields));
            AssertExtensions.Throws<ArgumentException>("bindings[0]", () => Expression.MemberInit(newExp, bind));
        }

        [Fact]
        public void UpdateSameReturnsSame()
        {
            MemberAssignment bind = Expression.Bind(typeof(Inner).GetProperty(nameof(Inner.Value)), Expression.Constant(3));
            MemberMemberBinding memberBind = Expression.MemberBind(typeof(Outer).GetProperty(nameof(Outer.InnerProperty)), bind);
            Assert.Same(memberBind, memberBind.Update(Enumerable.Repeat(bind, 1)));
        }


        [Fact]
        public void UpdateDifferentReturnsDifferent()
        {
            MemberAssignment bind = Expression.Bind(typeof(Inner).GetProperty(nameof(Inner.Value)), Expression.Constant(3));
            MemberMemberBinding memberBind = Expression.MemberBind(typeof(Outer).GetProperty(nameof(Outer.InnerProperty)), bind);
            Assert.NotSame(memberBind, memberBind.Update(new[] {Expression.Bind(typeof(Inner).GetProperty(nameof(Inner.Value)), Expression.Constant(3))}));
            Assert.NotSame(memberBind, memberBind.Update(Enumerable.Empty<MemberBinding>()));
        }

        [Fact]
        public void UpdateNullThrows()
        {
            MemberAssignment bind = Expression.Bind(typeof(Inner).GetProperty(nameof(Inner.Value)), Expression.Constant(3));
            MemberMemberBinding memberBind = Expression.MemberBind(typeof(Outer).GetProperty(nameof(Outer.InnerProperty)), bind);
            AssertExtensions.Throws<ArgumentNullException>("bindings", () => memberBind.Update(null));
        }

        [Fact]
        public void UpdateDoesntRepeatEnumeration()
        {
            MemberAssignment bind = Expression.Bind(typeof(Inner).GetProperty(nameof(Inner.Value)), Expression.Constant(3));
            MemberMemberBinding memberBind = Expression.MemberBind(typeof(Outer).GetProperty(nameof(Outer.InnerProperty)), bind);
            Assert.NotSame(memberBind, memberBind.Update(new RunOnceEnumerable<MemberBinding>(new[] { Expression.Bind(typeof(Inner).GetProperty(nameof(Inner.Value)), Expression.Constant(3)) })));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void InnerProperty(bool useInterpreter)
        {
            Expression<Func<Outer>> exp = Expression.Lambda<Func<Outer>>(
                Expression.MemberInit(
                    Expression.New(typeof(Outer)),
                    Expression.MemberBind(
                        typeof(Outer).GetProperty(nameof(Outer.InnerProperty)),
                        Expression.Bind(typeof(Inner).GetProperty(nameof(Inner.Value)), Expression.Constant(3))
                        )
                    )
                );
            Func<Outer> func = exp.Compile(useInterpreter);
            Assert.Equal(3, func().InnerProperty.Value);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void InnerField(bool useInterpreter)
        {
            Expression<Func<Outer>> exp = Expression.Lambda<Func<Outer>>(
                Expression.MemberInit(
                    Expression.New(typeof(Outer)),
                    Expression.MemberBind(
                        typeof(Outer).GetField(nameof(Outer.InnerField)),
                        Expression.Bind(typeof(Inner).GetProperty(nameof(Inner.Value)), Expression.Constant(4))
                        )
                    )
                );
            Func<Outer> func = exp.Compile(useInterpreter);
            Assert.Equal(4, func().InnerField.Value);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void StaticInnerProperty(bool useInterpreter)
        {
            Expression<Func<Outer>> exp = Expression.Lambda<Func<Outer>>(
                Expression.MemberInit(
                    Expression.New(typeof(Outer)),
                    Expression.MemberBind(
                        typeof(Outer).GetProperty(nameof(Outer.StaticInnerProperty)),
                        Expression.Bind(typeof(Inner).GetProperty(nameof(Inner.Value)), Expression.Constant(5))
                        )
                    )
                );
            Assert.Throws<InvalidProgramException>(() => exp.Compile(useInterpreter));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void StaticInnerField(bool useInterpreter)
        {
            Expression<Func<Outer>> exp = Expression.Lambda<Func<Outer>>(
                Expression.MemberInit(
                    Expression.New(typeof(Outer)),
                    Expression.MemberBind(
                        typeof(Outer).GetField(nameof(Outer.StaticInnerField)),
                        Expression.Bind(typeof(Inner).GetProperty(nameof(Inner.Value)), Expression.Constant(6))
                        )
                    )
                );
            Assert.Throws<InvalidProgramException>(() => exp.Compile(useInterpreter));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void ReadonlyInnerProperty(bool useInterpreter)
        {
            Expression<Func<Outer>> exp = Expression.Lambda<Func<Outer>>(
                Expression.MemberInit(
                    Expression.New(typeof(Outer)),
                    Expression.MemberBind(
                        typeof(Outer).GetProperty(nameof(Outer.ReadonlyInnerProperty)),
                        Expression.Bind(typeof(Inner).GetProperty(nameof(Inner.Value)), Expression.Constant(7))
                        )
                    )
                );
            Func<Outer> func = exp.Compile(useInterpreter);
            Assert.Equal(7, func().ReadonlyInnerProperty.Value);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void ReadonlyInnerField(bool useInterpreter)
        {
            Expression<Func<Outer>> exp = Expression.Lambda<Func<Outer>>(
                Expression.MemberInit(
                    Expression.New(typeof(Outer)),
                    Expression.MemberBind(
                        typeof(Outer).GetField(nameof(Outer.ReadonlyInnerField)),
                        Expression.Bind(typeof(Inner).GetProperty(nameof(Inner.Value)), Expression.Constant(8))
                        )
                    )
                );
            Func<Outer> func = exp.Compile(useInterpreter);
            Assert.Equal(8, func().ReadonlyInnerField.Value);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void StaticReadonlyInnerProperty(bool useInterpreter)
        {
            Expression<Func<Outer>> exp = Expression.Lambda<Func<Outer>>(
                Expression.MemberInit(
                    Expression.New(typeof(Outer)),
                    Expression.MemberBind(
                        typeof(Outer).GetProperty(nameof(Outer.StaticReadonlyInnerProperty)),
                        Expression.Bind(typeof(Inner).GetProperty(nameof(Inner.Value)), Expression.Constant(5))
                        )
                    )
                );
            Assert.Throws<InvalidProgramException>(() => exp.Compile(useInterpreter));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void StaticReadonlyInnerField(bool useInterpreter)
        {
            Expression<Func<Outer>> exp = Expression.Lambda<Func<Outer>>(
                Expression.MemberInit(
                    Expression.New(typeof(Outer)),
                    Expression.MemberBind(
                        typeof(Outer).GetField(nameof(Outer.StaticReadonlyInnerField)),
                        Expression.Bind(typeof(Inner).GetProperty(nameof(Inner.Value)), Expression.Constant(6))
                        )
                    )
                );
            Assert.Throws<InvalidProgramException>(() => exp.Compile(useInterpreter));
        }

#if FEATURE_COMPILE
        [Fact]
        public void GlobalMethod()
        {
            ModuleBuilder module = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), AssemblyBuilderAccess.RunAndCollect).DefineDynamicModule("Module");
            MethodBuilder globalMethod = module.DefineGlobalMethod("GlobalMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(int), Type.EmptyTypes);
            globalMethod.GetILGenerator().Emit(OpCodes.Ret);
            module.CreateGlobalFunctions();
            MethodInfo globalMethodInfo = module.GetMethod(globalMethod.Name);
            AssertExtensions.Throws<ArgumentException>("propertyAccessor", () => Expression.MemberBind(globalMethodInfo));
        }
#endif

        public void WriteOnlyInnerProperty()
        {
            MemberAssignment bind = Expression.Bind(typeof(Inner).GetProperty(nameof(Inner.Value)), Expression.Constant(0));
            PropertyInfo property = typeof(Outer).GetProperty(nameof(Outer.WriteonlyInnerProperty));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.MemberBind(property, bind));
        }

        public void StaticWriteOnlyInnerProperty()
        {
            MemberAssignment bind = Expression.Bind(typeof(Inner).GetProperty(nameof(Inner.Value)), Expression.Constant(0));
            PropertyInfo property = typeof(Outer).GetProperty(nameof(Outer.StaticWriteonlyInnerProperty));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.MemberBind(property, bind));
        }
    }
}
