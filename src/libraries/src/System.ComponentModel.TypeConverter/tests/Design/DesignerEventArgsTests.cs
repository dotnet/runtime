// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Design.Tests
{
    public class DesignerEventArgsTests
    {
        public static IEnumerable<object[]> Ctor_TestData()
        {
            yield return new object[] { new TestDesignerHost() };
            yield return new object[] { null };
        }

        [Theory]
        [MemberData(nameof(Ctor_TestData))]
        public void Ctor(IDesignerHost designer)
        {
            var eventArgs = new DesignerEventArgs(designer);
            Assert.Same(designer, eventArgs.Designer);
        }
    }
}
