// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Xunit;

namespace System.Compostition.Tests
{
    public class ExportTests
    {
        [Fact]
        public void Ctor_Value_Action()
        {
            int exportActionCalled = 0;
            var export = new Export<int>(1, () => exportActionCalled++);
            Assert.Equal(1, export.Value);
            Assert.Equal(0, exportActionCalled);
        }

        [Fact]
        public void Dispose_MultipleTimes_InvokesDisposeAction()
        {
            int exportActionCalled = 0;
            Export<int> export = new Export<int>(1, () => exportActionCalled++);
            Assert.Equal(0, exportActionCalled);

            export.Dispose();
            Assert.Equal(1, exportActionCalled);

            export.Dispose();
            Assert.Equal(2, exportActionCalled);
        }

        [Fact]
        public void Dispose_NullAction_Nop()
        {
            Export<int> export = new Export<int>(1, null);
            export.Dispose();
            export.Dispose();
        }
    }
}
