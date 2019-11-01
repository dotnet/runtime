// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Design.Tests
{
    public partial class ActiveDesignerEventArgsTests
    {
        public static IEnumerable<object[]> Ctor_OldDesigner_NewDesigner_TestData()
        {
            yield return new object[] { new TestDesignerHost(), new TestDesignerHost() };
            yield return new object[] { null, null };
        }

        [Theory]
        [MemberData(nameof(Ctor_OldDesigner_NewDesigner_TestData))]
        public void Ctor_NewObject(IDesignerHost oldDesigner, IDesignerHost newDesigner)
        {
            var eventArgs = new ActiveDesignerEventArgs(oldDesigner, newDesigner);
            Assert.Same(oldDesigner, eventArgs.OldDesigner);
            Assert.Same(newDesigner, eventArgs.NewDesigner);
        }
    }
}
