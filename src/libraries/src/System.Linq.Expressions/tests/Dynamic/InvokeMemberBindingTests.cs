// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CSharp.RuntimeBinder;
using Xunit;

namespace System.Dynamic.Tests
{
    public class InvokeMemberBindingTests
    {
        private class MinimumOverrideInvokeMemberBinding : InvokeMemberBinder
        {
            public MinimumOverrideInvokeMemberBinding(string name, bool ignoreCase, CallInfo callInfo)
                : base(name, ignoreCase, callInfo)
            {
            }

            public override DynamicMetaObject FallbackInvokeMember(
                DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
            {
                throw new NotSupportedException();
            }

            public override DynamicMetaObject FallbackInvoke(
                DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
            {
                throw new NotSupportedException();
            }
        }

        private class TestBaseClass
        {
            public object Method(int x, int y) => new TestBaseClass();

            public string TellType<T>(T item) => typeof(T).Name;

            public bool TryParseInt(string value, out int result) => int.TryParse(value, out result);
        }

        private class TestDerivedClass : TestBaseClass
        {
            public new string Method(int x, int y) => "Hiding";
        }

        private static IEnumerable<object[]> ObjectArguments()
        {
            yield return new object[] {0};
            yield return new object[] {""};
            yield return new object[] {new Uri("http://example.net/")};
            yield return new[] {new object()};
        }

        [Theory, MemberData(nameof(ObjectArguments))]
        public void InvokeVirtualMethod(object value)
        {
            dynamic d = value;
            Assert.Equal(value.ToString(), d.ToString());
        }

        [Theory, MemberData(nameof(ObjectArguments))]
        public void InvokeNonVirtualMethod(object value)
        {
            dynamic d = value;
            Assert.Equal(value.GetType(), d.GetType());
        }

        [Theory, MemberData(nameof(ObjectArguments))]
        public void InvokeCaseInsensitiveFails(dynamic value)
        {
            Assert.Throws<RuntimeBinderException>(() => value.tostring());
        }

        // TODO: Use a case-insensitive binder so that the above actually works.
        // https://github.com/dotnet/corefx/issues/14012

        [Fact]
        public void MethodHiding()
        {
            dynamic d = new TestDerivedClass();
            Assert.Equal("Hiding", d.Method(1, 2));
        }

        [Fact]
        public void GenericMethod()
        {
            dynamic d = new TestDerivedClass();
            Assert.Equal(nameof(Int32), d.TellType(0));
            Assert.Equal(nameof(TestDerivedClass), d.TellType(d));
            // Explicit type selection.
            Assert.Equal(nameof(TestBaseClass), d.TellType<TestBaseClass>(d));
        }

        [Fact]
        public void ByRef()
        {
            dynamic d = new TestDerivedClass();
            int x;
            dynamic s = "21";
            Assert.True(d.TryParseInt(s, out x));
            Assert.Equal(21, x);
        }

        [Fact]
        public void GenericType()
        {
            dynamic d = new List<int>();
            dynamic x = 32;
            d.Add(x);
            d.Add(x);
            int tally = 0;
            foreach (int item in d)
            {
                tally += item;
            }

            Assert.Equal(64, tally);
        }

        [Fact]
        public void NoSuchMethod()
        {
            dynamic d = new object();
            Assert.Throws<RuntimeBinderException>(() => d.MagicallyFixAllTheBugs());
        }

        [Fact]
        public void NoArgumentMatch()
        {
            dynamic d = new TestDerivedClass();
            Assert.Throws<RuntimeBinderException>(() => d.Method());
        }

        [Fact]
        public void NotAMethod()
        {
            dynamic d = "A string";
            Assert.Throws<RuntimeBinderException>(() => d.Length());
        }

        [Fact]
        public void NullName()
        {
            CallInfo info = new CallInfo(0);
            Assert.Throws<ArgumentNullException>(
                "name", () => new MinimumOverrideInvokeMemberBinding(null, false, info));
        }

        [Fact]
        public void NullCallInfo()
        {
            Assert.Throws<ArgumentNullException>(
                "callInfo", () => new MinimumOverrideInvokeMemberBinding("Name", false, null));
        }

        [Fact]
        public void NameStored()
        {
            var binding = new MinimumOverrideInvokeMemberBinding("My test name", false, new CallInfo(0));
            Assert.Equal("My test name", binding.Name);
        }

        [Fact]
        public void TypeIsObject()
        {
            Assert.Equal(
                typeof(object), new MinimumOverrideInvokeMemberBinding("name", true, new CallInfo(0)).ReturnType);
        }

        [Fact]
        public void IgnoreCaseStored()
        {
            CallInfo info = new CallInfo(0);
            Assert.False(new MinimumOverrideInvokeMemberBinding("name", false, info).IgnoreCase);
            Assert.True(new MinimumOverrideInvokeMemberBinding("name", true, info).IgnoreCase);
        }

        [Fact]
        public void CallInfoStored()
        {
            CallInfo info = new CallInfo(0);
            Assert.Same(info, new MinimumOverrideInvokeMemberBinding("name", false, info).CallInfo);
        }
    }
}
