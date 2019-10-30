// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Design.Tests
{
    public class CommandIDTests
    {
        public static IEnumerable<object[]> Ctor_MenuGroup_CommandID_TestData()
        {
            yield return new object[] { Guid.Empty, -1 };
            yield return new object[] { Guid.NewGuid(), 10 };
        }

        [Theory]
        [MemberData(nameof(Ctor_MenuGroup_CommandID_TestData))]
        public void Ctor_Guid_ID(Guid guid, int ID)
        {
            var commandId = new CommandID(guid, ID);
            Assert.Equal(guid, commandId.Guid);
            Assert.Equal(ID, commandId.ID);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var commandId = new CommandID(Guid.NewGuid(), 10);

            yield return new object[] { commandId, commandId, true };
            yield return new object[] { commandId, new CommandID(commandId.Guid, 10), true };
            yield return new object[] { commandId, new CommandID(Guid.Empty, 10), false };
            yield return new object[] { commandId, new CommandID(commandId.Guid, 9), false };

            yield return new object[] { commandId, new object(), false };
            yield return new object[] { commandId, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(CommandID commandId, object other, bool expected)
        {
            Assert.Equal(expected, commandId.Equals(other));
            if (other is CommandID)
            {
                Assert.Equal(expected, commandId.GetHashCode().Equals(other.GetHashCode()));
            }
        }

        [Fact]
        public void ToString_Invoke_ReturnsExpected()
        {
            Guid guid = Guid.NewGuid();
            var commandId = new CommandID(guid, 10);
            Assert.Equal($"{guid} : 10", commandId.ToString());
        }
    }
}
