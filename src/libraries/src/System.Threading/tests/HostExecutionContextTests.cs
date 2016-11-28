// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Threading.Tests
{
    public static class HostExecutionContextTests
    {
        [Fact]
        public static void BasicTest()
        {
            var hec = new TestHostExecutionContext();
            Assert.Null(hec.State);

            var obj = new object();
            hec = new TestHostExecutionContext(obj);
            Assert.Same(obj, hec.State);

            obj = new object();
            hec.State = obj;
            Assert.Same(obj, hec.State);
            Assert.NotNull(hec.CreateCopy());

            hec.State = null;
            Assert.Null(hec.State);
            Assert.NotNull(hec.CreateCopy());

            Assert.False(hec.DisposeTrueCalled);
            hec.Dispose();
            Assert.True(hec.DisposeTrueCalled);

            // Dispose(bool) is public
            new HostExecutionContext().Dispose(true);
        }

        private class TestHostExecutionContext : HostExecutionContext
        {
            public bool DisposeTrueCalled { get; private set; }

            public TestHostExecutionContext()
            {
            }

            public TestHostExecutionContext(object state)
                : base(state)
            {
            }

            public new object State
            {
                get
                {
                    return base.State;
                }
                set
                {
                    base.State = value;
                }
            }

            public override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    DisposeTrueCalled = true;
                }
                base.Dispose(disposing);
            }
        }
    }
}
