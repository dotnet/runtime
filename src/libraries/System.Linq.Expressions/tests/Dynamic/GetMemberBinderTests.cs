// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CSharp.RuntimeBinder;
using Xunit;

namespace System.Dynamic.Tests
{
    public class GetMemberBinderTests
    {
        private class MinimumOverrideGetMemberBinder : GetMemberBinder
        {
            public MinimumOverrideGetMemberBinder(string name, bool ignoreCase)
                : base(name, ignoreCase)
            {
            }

            public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
            {
                throw new NotSupportedException();
            }
        }

        private class TestBaseClass
        {
            public string Name => nameof(TestBaseClass);
        }

        private class TestDerivedClass : TestBaseClass
        {
            public new string Name => nameof(TestDerivedClass);
        }

        private static readonly string[] Names =
        {
            "arg", "ARG", "Arg", "Argument name that isn’t a valid C♯ name 👿🤢",
            "horrid name with" + (char)0xD800 + "a half surrogate", "new", "break"
        };

        public static IEnumerable<object[]> NamesAndBools() => Names.Select((n, i) => new object[] {n, i % 2 == 0});

        [Fact]
        public void InvokeInstanceProperty()
        {
            dynamic d = "1234";
            Assert.Equal(4, d.Length);
        }

        [Fact]
        public void InvokeGenericClassInstanceProperty()
        {
            dynamic d = new List<int> {1, 2, 3, 4};
            Assert.Equal(4, d.Count);
        }

        [Fact]
        public void InvokeCaseInsensitiveFails()
        {
            dynamic d = "1234";
            Assert.Throws<RuntimeBinderException>(() => d.LENGTH);
        }

        [Fact]
        public void MemberHiding()
        {
            dynamic d = new TestDerivedClass();
            Assert.Equal(nameof(TestDerivedClass), d.Name);
        }

        [Fact]
        public void NullName()
        {
            AssertExtensions.Throws<ArgumentNullException>("name", () => new MinimumOverrideGetMemberBinder(null, false));
            AssertExtensions.Throws<ArgumentNullException>("name", () => new MinimumOverrideGetMemberBinder(null, true));
        }

        [Theory, MemberData(nameof(NamesAndBools))]
        public void CTorArgumentsStored(string name, bool ignoreCase)
        {
            GetMemberBinder binder = new MinimumOverrideGetMemberBinder(name, ignoreCase);
            Assert.Equal(ignoreCase, binder.IgnoreCase);
            Assert.Equal(binder.Name, name);
        }

        [Theory, MemberData(nameof(NamesAndBools))]
        public void ReturnTypeObject(string name, bool ignoreCase) =>
            Assert.Equal(typeof(object), new MinimumOverrideGetMemberBinder(name, ignoreCase).ReturnType);
    }
}
