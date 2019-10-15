// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xunit;

namespace System.Dynamic.Tests
{
    public class CallInfoTests
    {
        [Fact]
        public void Ctor_NullNames_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("argNames", () => new CallInfo(0, default(IEnumerable<string>)));
            AssertExtensions.Throws<ArgumentNullException>("argNames", () => new CallInfo(0, default(string[])));
        }

        [Theory]
        [InlineData(-1, new string[0])]
        [InlineData(2, new string[] { "foo", "bar", "baz", "quux", "quuux" })]
        public void Ctor_CountLessThanArgNamesCount_ThrowsArgumentException(int argCount, string[] argNames)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new CallInfo(argCount, argNames));
        }

        [Fact]
        public void Ctor_NullItemInArgNames_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("argNames[1]", () => new CallInfo(3, "foo", null, "bar"));
            AssertExtensions.Throws<ArgumentNullException>(
                "argNames[0]", () => new CallInfo(3, Enumerable.Repeat(default(string), 2)));
        }

        [Theory]
        [InlineData(0, new string[0])]
        [InlineData(1, new string[0])]
        [InlineData(5, new string[] { "foo", "bar", "baz", "quux", "quuux" })]
        public void Ctor_Int_String(int argCount, string[] argNames)
        {
            var info1 = new CallInfo(argCount, argNames);
            Assert.Equal(argCount, info1.ArgumentCount);
            Assert.Equal(argNames, info1.ArgumentNames);

            var info2 = new CallInfo(argCount, (IEnumerable<string>)argNames);
            Assert.Equal(argCount, info2.ArgumentCount);
            Assert.Equal(argNames, info2.ArgumentNames);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            CallInfo basicCallInfo = new CallInfo(1, new string[0]);
            yield return new object[] { basicCallInfo, new CallInfo(1, new string[0]), true };
            yield return new object[] { basicCallInfo, basicCallInfo, true };
            yield return new object[] { basicCallInfo, new CallInfo(0, new string[0]), false };
            yield return new object[] { basicCallInfo, new CallInfo(1, new string[] { "foo" }), false };

            yield return new object[] { new CallInfo(2, new string[] { "foo", "bar" }), new CallInfo(2, new string[] { "foo", "bar" }), true};
            yield return new object[] { new CallInfo(2, new string[] { "foo", "bar" }), new CallInfo(2, new string[] { "foo", "baz" }), false };
            yield return new object[] { new CallInfo(2, new string[] { "foo", "bar" }), new CallInfo(3, new string[] { "foo", "bar" }), false };

            yield return new object[] { basicCallInfo, "CallInfo", false };
            yield return new object[] { basicCallInfo, new object(), false };
            yield return new object[] { basicCallInfo, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public static void Equals_GetHashCode_ReturnsExpected(CallInfo info, object obj, bool expected)
        {
            Assert.Equal(expected, info.Equals(obj));

            if (obj is CallInfo)
            {
                // Failure at this point is not definitely a bug,
                // but should be considered a concern unless it can be
                // convincingly ruled a fluke.
                Assert.Equal(expected, info.GetHashCode().Equals(obj.GetHashCode()));
            }
        }

        [Fact]
        public void Ctor_Enumerable_MakesReadOnlyCopy()
        {
            List<string> nameList = new List<string> {"foo", "bar"};
            ReadOnlyCollection<string> nameReadOnly = nameList.AsReadOnly();
            var info = new CallInfo(2, nameReadOnly);
            nameList[0] = "baz";
            nameList[1] = "qux";
            Assert.Equal(new[] {"foo", "bar"}, info.ArgumentNames);
        }

        [Fact]
        public void Ctor_Array_MakesReadOnlyCopy()
        {
            string[] nameArray = {"foo", "bar"};
            var nameReadOnly = new ReadOnlyCollection<string>(nameArray);
            var info = new CallInfo(2, nameReadOnly);
            nameArray[0] = "baz";
            nameArray[1] = "qux";
            Assert.Equal(new[] {"foo", "bar"}, info.ArgumentNames);
        }
    }
}
