// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Design.Tests
{
    public class ComponentRenameEventArgsTests
    {
        public static IEnumerable<object[]> Ctor_TestData()
        {
            yield return new object[] { "component", "oldName", "newName" };
            yield return new object[] { null, null, null };
        }

        [Theory]
        [MemberData(nameof(Ctor_TestData))]
        public void Ctor(object component, string oldName, string newName)
        {
            var eventArgs = new ComponentRenameEventArgs(component, oldName, newName);
            Assert.Same(component, eventArgs.Component);
            Assert.Same(oldName, eventArgs.OldName);
            Assert.Same(newName, eventArgs.NewName);
        }
    }
}
