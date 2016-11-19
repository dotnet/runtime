// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Threading.Tests
{
    public static class HostExecutionContextManagerTests
    {
        [Fact]
        public static void BasicTest()
        {
            ThreadTestHelpers.RunTestInBackgroundThread(() =>
            {
                var hecm = new HostExecutionContextManager();
                Assert.Null(hecm.Capture());
                Assert.Throws<InvalidOperationException>(() => hecm.SetHostExecutionContext(null));

                var hec0 = new HostExecutionContext();
                var hec1 = new HostExecutionContext();
                object previousState0 = hecm.SetHostExecutionContext(hec0);
                ExecutionContext ec = ExecutionContext.Capture();
                object previousState1 = hecm.SetHostExecutionContext(hec1);

                Assert.Throws<InvalidOperationException>(() => hecm.Revert(null));
                Assert.Throws<InvalidOperationException>(() => hecm.Revert(new object()));
                Assert.Throws<InvalidOperationException>(() => hecm.Revert(previousState0));

                object otherThreadState = null;
                ThreadTestHelpers.RunTestInBackgroundThread(
                    () => otherThreadState = hecm.SetHostExecutionContext(new HostExecutionContext()));
                Assert.Throws<InvalidOperationException>(() => hecm.Revert(otherThreadState));

                ExecutionContext.Run(
                    ec,
                    state => Assert.Throws<InvalidOperationException>(() => hecm.Revert(previousState1)),
                    null);

                hecm.Revert(previousState1);
                Assert.Throws<InvalidOperationException>(() => hecm.Revert(previousState1));

                // Revert always reverts to a null host execution context when it's not hosted
                Assert.Throws<InvalidOperationException>(() => hecm.Revert(previousState0));
            });
        }
    }
}
