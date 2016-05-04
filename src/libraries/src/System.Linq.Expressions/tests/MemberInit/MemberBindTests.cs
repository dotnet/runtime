// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
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

        [Fact]
        public void NullMethodOrMemberInfo()
        {
            Assert.Throws<ArgumentNullException>("member", () => Expression.MemberBind(default(MemberInfo)));
            Assert.Throws<ArgumentNullException>("member", () => Expression.MemberBind(default(MemberInfo), Enumerable.Empty<MemberBinding>()));
            Assert.Throws<ArgumentNullException>("propertyAccessor", () => Expression.MemberBind(default(MethodInfo)));
            Assert.Throws<ArgumentNullException>("propertyAccessor", () => Expression.MemberBind(default(MethodInfo), Enumerable.Empty<MemberBinding>()));
        }

        [Fact]
        public void NullBindings()
        {
            var mem = typeof(PropertyAndFields).GetProperty(nameof(PropertyAndFields.StringProperty));
            var meth = mem.GetGetMethod();
            Assert.Throws<ArgumentNullException>("bindings", () => Expression.MemberBind(mem, default(MemberBinding[])));
            Assert.Throws<ArgumentNullException>("bindings", () => Expression.MemberBind(mem, default(IEnumerable<MemberBinding>)));
            Assert.Throws<ArgumentNullException>("bindings", () => Expression.MemberBind(meth, default(MemberBinding[])));
            Assert.Throws<ArgumentNullException>("bindings", () => Expression.MemberBind(meth, default(IEnumerable<MemberBinding>)));
        }

        [Fact]
        public void NullBindingInBindings()
        {
            var mem = typeof(PropertyAndFields).GetProperty(nameof(PropertyAndFields.StringProperty));
            var meth = mem.GetGetMethod();
            Assert.Throws<ArgumentNullException>("bindings", () => Expression.MemberBind(mem, default(MemberBinding)));
            Assert.Throws<ArgumentNullException>("bindings", () => Expression.MemberBind(mem, Enumerable.Repeat<MemberBinding>(null, 1)));
            Assert.Throws<ArgumentNullException>("bindings", () => Expression.MemberBind(meth, default(MemberBinding)));
            Assert.Throws<ArgumentNullException>("bindings", () => Expression.MemberBind(meth, Enumerable.Repeat<MemberBinding>(null, 1)));
        }

        [Fact]
        public void BindMethodMustBeProperty()
        {
            MemberInfo toString = typeof(object).GetMember(nameof(ToString))[0];
            MethodInfo toStringMeth = typeof(object).GetMethod(nameof(ToString));
            Assert.Throws<ArgumentException>("member", () => Expression.MemberBind(toString));
            Assert.Throws<ArgumentException>("member", () => Expression.MemberBind(toString, Enumerable.Empty<MemberBinding>()));
            Assert.Throws<ArgumentException>("propertyAccessor", () => Expression.MemberBind(toStringMeth));
            Assert.Throws<ArgumentException>("propertyAccessor", () => Expression.MemberBind(toStringMeth, Enumerable.Empty<MemberBinding>()));
        }

        [Fact]
        public void MemberBindingMustBeMemberOfType()
        {
            
        }
    }
}
