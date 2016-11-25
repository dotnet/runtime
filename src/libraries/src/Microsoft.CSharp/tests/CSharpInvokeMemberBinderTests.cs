// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.CSharp.RuntimeBinder.Tests
{
    public class CSharpInvokeMemberBinderTests
    {
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
            yield return new object[] { 0 };
            yield return new object[] { "" };
            yield return new object[] { new Uri("http://example.net/") };
            yield return new[] { new object() };
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
        // Cannot be done directly with C# calls.

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
            foreach (var item in d)
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
    }
}
