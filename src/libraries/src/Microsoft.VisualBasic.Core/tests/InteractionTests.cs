// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.VisualBasic.Tests
{
    public class InteractionTests
    {
        [Theory]
        [MemberData(nameof(CallByName_TestData))]
        public void CallByName(object instance, string methodName, CallType useCallType, object[] args, Func<object, object> getResult, object expected)
        {
            Assert.Equal(getResult is null ? expected : null, Interaction.CallByName(instance, methodName, useCallType, args));
            if (getResult != null)
            {
                Assert.Equal(expected, getResult(instance));
            }
        }

        [Theory]
        [MemberData(nameof(CallByName_ArgumentException_TestData))]
        public void CallByName_ArgumentException(object instance, string methodName, CallType useCallType, object[] args)
        {
            Assert.Throws<ArgumentException>(() => Interaction.CallByName(instance, methodName, useCallType, args));
        }

        [Theory]
        [MemberData(nameof(CallByName_MissingMemberException_TestData))]
        public void CallByName_MissingMemberException(object instance, string methodName, CallType useCallType, object[] args)
        {
            Assert.Throws<MissingMemberException>(() => Interaction.CallByName(instance, methodName, useCallType, args));
        }

        private static IEnumerable<object[]> CallByName_TestData()
        {
            yield return new object[] { new Class(), "Method", CallType.Method, new object[] { 1, 2 }, null, 3 };
            yield return new object[] { new Class(), "Method", CallType.Get, new object[] { 2, 3 }, null, 5 };
            yield return new object[] { new Class(), "P", CallType.Get, new object[0], null, 0 };
            yield return new object[] { new Class(), "Item", CallType.Get, new object[] { 2 }, null, 2 };
            yield return new object[] { new Class(), "P", CallType.Set, new object[] { 3 }, new Func<object, object>(obj => ((Class)obj).Value), 3 };
            yield return new object[] { new Class(), "Item", CallType.Let, new object[] { 4, 5 }, new Func<object, object>(obj => ((Class)obj).Value), 9 };
        }

        private static IEnumerable<object[]> CallByName_ArgumentException_TestData()
        {
            yield return new object[] { null, null, default(CallType), new object[0] };
            yield return new object[] { new Class(), "Method", default(CallType), new object[] { 1, 2 } };
            yield return new object[] { new Class(), "Method", (CallType)int.MaxValue, new object[] { 1, 2 } };
        }

        private static IEnumerable<object[]> CallByName_MissingMemberException_TestData()
        {
            yield return new object[] { new Class(), "Method", CallType.Method, new object[0] };
            yield return new object[] { new Class(), "Q", CallType.Get, new object[0] };
        }

        private sealed class Class
        {
            public int Value;
            public int Method(int x, int y) => x + y;
            public int P
            {
                get { return Value; }
                set { Value = value; }
            }
            public object this[object index]
            {
                get { return Value + (int)index; }
                set { Value = (int)value + (int)index; }
            }
        }
    }
}
