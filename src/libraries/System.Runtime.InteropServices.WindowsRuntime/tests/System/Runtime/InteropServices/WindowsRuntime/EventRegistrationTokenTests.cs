// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Runtime.InteropServices.WindowsRuntime.Tests
{
    public class EventRegistrationTokenTests
    {
        public static IEnumerable<object[]> Equals_TestData()
        {
            var tokenTable = new EventRegistrationTokenTable<Delegate>();
            EventRegistrationToken token = tokenTable.AddEventHandler(new EventHandler(EventHandlerMethod1));
            EventRegistrationToken emptyToken = tokenTable.AddEventHandler(null);

            yield return new object[] { token, token, true };
            yield return new object[] { token, emptyToken, false };
            yield return new object[] { emptyToken, emptyToken, true };
            yield return new object[] { emptyToken, new EventRegistrationToken(), true };

            yield return new object[] { token, new object(), false };
            yield return new object[] { token, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(EventRegistrationToken token, object other, bool expected)
        {
            Assert.Equal(expected, token.Equals(other));
            if (other is EventRegistrationToken otherToken)
            {
                Assert.Equal(expected, token == otherToken);
                Assert.Equal(!expected, token != otherToken);
                Assert.Equal(expected, token.GetHashCode().Equals(other.GetHashCode()));
            }
        }

        private static void EventHandlerMethod1(object sender, EventArgs e) { }
        private static void EventHandlerMethod2(object sender, EventArgs e) { }
    }
}
