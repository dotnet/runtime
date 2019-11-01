// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Design.Tests
{
    public class ComponentChangingEventArgsTests
    {
        public static IEnumerable<object[]> Ctor_TestData()
        {
            yield return new object[] { "component", new ArrayConverter().GetProperties(new int[1])[0] };
            yield return new object[] { null, null };
        }

        [Theory]
        [MemberData(nameof(Ctor_TestData))]
        public void Ctor(object component, MemberDescriptor member)
        {
            var eventArgs = new ComponentChangingEventArgs(component, member);
            Assert.Same(component, eventArgs.Component);
            Assert.Same(member, eventArgs.Member);
        }
    }
}
