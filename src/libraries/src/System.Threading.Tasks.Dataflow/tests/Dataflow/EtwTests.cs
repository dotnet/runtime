// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Threading.Tasks.Dataflow.Tests
{
    public class EtwTests
    {
        [Fact]
        public void TestEtw()
        {
            RemoteExecutor.Invoke(() =>
            {
                using (var listener = new TestEventListener(new Guid("16F53577-E41D-43D4-B47E-C17025BF4025"), EventLevel.Verbose))
                {
                    ActionBlock<int> ab = null;
                    BufferBlock<int> bb = null;
                    int remaining = 0;
                    CountdownEvent ce = new CountdownEvent(0);

                    // Check that block creation events fire
                    const int DataflowBlockCreatedId = 1;
                    remaining = 2;
                    listener.RunWithCallback(ev => {
                            Assert.Equal(expected: DataflowBlockCreatedId, actual: ev.EventId);
                            remaining--;
                        },
                        () => {
                            ab = new ActionBlock<int>(i => { });
                            bb = new BufferBlock<int>(); // trigger block creation event
                            Assert.Equal(expected: 0, actual: remaining);
                        });

                    // Check that linking events fire
                    const int BlockLinkedId = 4;
                    remaining = 1;
                    IDisposable link = null;
                    listener.RunWithCallback(ev => {
                            Assert.Equal(expected: BlockLinkedId, actual: ev.EventId);
                            remaining--;
                        },
                        () => {
                            link = bb.LinkTo(ab);
                            Assert.Equal(expected: 0, actual: remaining);
                        });

                    // Check that unlinking events fire
                    const int BlockUnlinkedId = 5;
                    remaining = 1;
                    listener.RunWithCallback(ev => {
                            Assert.Equal(expected: BlockUnlinkedId, actual: ev.EventId);
                            remaining--;
                        },
                        () => {
                            link.Dispose();
                            Assert.Equal(expected: 0, actual: remaining);
                        });

                    // Check that task launched events fire
                    const int TaskLaunchedId = 2;
                    ce.Reset(1);
                    listener.RunWithCallback(ev => {
                            Assert.Equal(expected: TaskLaunchedId, actual: ev.EventId);
                            ce.Signal();
                        },
                        () => {
                            ab.Post(42);
                            ce.Wait();
                            Assert.Equal(expected: 0, actual: ce.CurrentCount);
                        });

                    // Check that completion events fire
                    const int BlockCompletedId = 3;
                    ce.Reset(2);
                        listener.RunWithCallback(ev => {
                            Assert.Equal(expected: BlockCompletedId, actual: ev.EventId);
                            ce.Signal();
                        },
                        () => {
                            ab.Complete();
                            bb.Complete();
                            ce.Wait();
                            Assert.Equal(expected: 0, actual: ce.CurrentCount);
                        });
                }

                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

    }
}
