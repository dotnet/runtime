// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CSharp.RuntimeBinder;
using Xunit;

namespace System.Dynamic.Tests
{
    public class SetMemberBinderTests
    {
        private class MinimumOverrideSetMemberBinder : SetMemberBinder
        {
            public MinimumOverrideSetMemberBinder(string name, bool ignoreCase)
                : base(name, ignoreCase)
            {
            }

            public override DynamicMetaObject FallbackSetMember(
                DynamicMetaObject target, DynamicMetaObject value, DynamicMetaObject errorSuggestion)
            {
                throw new NotSupportedException();
            }
        }

        private class TestBaseClass
        {
            public string Name { get; set; }
        }

        private class TestDerivedClass : TestBaseClass
        {
            public new string Name { get; set; }
        }

        private static readonly string[] Names =
        {
            "arg", "ARG", "Arg", "Argument name that isn’t a valid C♯ name 👿🤢",
            "horrid name with" + (char)0xD800 + "a half surrogate", "new", "break"
        };

        private static IEnumerable<object[]> NamesAndBools() => Names.Select((n, i) => new object[] { n, i % 2 == 0 });

        [Fact]
        public void InvokeInstanceProperty()
        {
            StringBuilder sb = new StringBuilder();
            dynamic d = sb;
            d.Length = 4;
            Assert.Equal(4, sb.Length);
        }

        [Fact]
        public void InvokeCaseInsensitiveFails()
        {
            StringBuilder sb = new StringBuilder();
            dynamic d = sb;
            Assert.Throws<RuntimeBinderException>(() => d.LENGTH = 4);
        }

        [Fact]
        public void MemberHiding()
        {
            TestDerivedClass tdc = new TestDerivedClass();
            dynamic d = tdc;
            d.Name = "test";
            Assert.Equal("test", tdc.Name);
            Assert.Null(((TestBaseClass)tdc).Name);
        }

        [Fact]
        public void NullName()
        {
            AssertExtensions.Throws<ArgumentNullException>("name", () => new MinimumOverrideSetMemberBinder(null, false));
            AssertExtensions.Throws<ArgumentNullException>("name", () => new MinimumOverrideSetMemberBinder(null, true));
        }

        [Theory, MemberData(nameof(NamesAndBools))]
        public void CTorArgumentsStored(string name, bool ignoreCase)
        {
            SetMemberBinder binder = new MinimumOverrideSetMemberBinder(name, ignoreCase);
            Assert.Equal(ignoreCase, binder.IgnoreCase);
            Assert.Equal(binder.Name, name);
        }

        [Theory, MemberData(nameof(NamesAndBools))]
        public void ReturnTypeObject(string name, bool ignoreCase) =>
            Assert.Equal(typeof(object), new MinimumOverrideSetMemberBinder(name, ignoreCase).ReturnType);
    }
}
