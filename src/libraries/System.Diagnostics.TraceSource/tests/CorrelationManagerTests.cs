// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Collections;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace System.Diagnostics.TraceSourceTests
{
    public class CorrelationManagerTests
    {
        [Fact]
        public void CorrelationManager_CheckStack()
        {
            Trace.CorrelationManager.StartLogicalOperation("one");
            Trace.CorrelationManager.StartLogicalOperation("two");

            // 2 operations in progress
            ValidateStack(Trace.CorrelationManager.LogicalOperationStack, "two", "one");

            Trace.CorrelationManager.StopLogicalOperation();
            // 1 operation in progress
            ValidateStack(Trace.CorrelationManager.LogicalOperationStack, "one");

            Trace.CorrelationManager.StopLogicalOperation();
            // 0 operation in progress
            ValidateStack(Trace.CorrelationManager.LogicalOperationStack, Array.Empty<object>());
        }

        [Fact]
        public void CorrelationManager_NullOperationId()
        {
            AssertExtensions.Throws<ArgumentNullException>("operationId", () => Trace.CorrelationManager.StartLogicalOperation(null));
        }

        [Fact]
        public void CorrelationManager_EmptyStack()
        {
            Assert.Throws<InvalidOperationException>(() => Trace.CorrelationManager.StopLogicalOperation());
        }

        [Fact]
        public void CorrelationManager_ActivityId()
        {
            Guid id = Guid.NewGuid();
            Trace.CorrelationManager.ActivityId = id;

            Assert.Equal(id, Trace.CorrelationManager.ActivityId);
        }

        [Fact]
        public async Task CorrelationManager_MutateStackPush()
        {
            Guid id = Guid.NewGuid();
            Trace.CorrelationManager.ActivityId = id;

            Trace.CorrelationManager.StartLogicalOperation(1);

            await Task.Yield();

            Trace.CorrelationManager.LogicalOperationStack.Push(2);

            ValidateStack(Trace.CorrelationManager.LogicalOperationStack, 2, 1);
        }

        [Fact]
        public async Task CorrelationManager_MutateStackPop()
        {
            Guid id = Guid.NewGuid();
            Trace.CorrelationManager.ActivityId = id;

            Trace.CorrelationManager.StartLogicalOperation(1);

            await Task.Yield();

            Assert.Equal(1,Trace.CorrelationManager.LogicalOperationStack.Pop());

            ValidateStack(Trace.CorrelationManager.LogicalOperationStack, Array.Empty<object>());
        }

        [Fact]
        public async Task CorrelationManager_ActivtyIdAcrossAwait()
        {
            Guid g = Guid.NewGuid();
            Trace.CorrelationManager.ActivityId = g;
            Assert.Equal(g, Trace.CorrelationManager.ActivityId);
            await Task.Yield();
            Assert.Equal(g, Trace.CorrelationManager.ActivityId);
            await Task.Delay(1);
            Assert.Equal(g, Trace.CorrelationManager.ActivityId);
        }

        [Fact]
        public async Task CorrelationManager_CorrelationAcrossAwait()
        {
            Guid g = Guid.NewGuid();
            Trace.CorrelationManager.ActivityId = g;


            Assert.Equal(g, Trace.CorrelationManager.ActivityId);
            Trace.CorrelationManager.StartLogicalOperation("one");

            // 1 operation in progress
            ValidateStack(Trace.CorrelationManager.LogicalOperationStack, "one");
            Trace.CorrelationManager.StartLogicalOperation("two");
            await Task.Yield();

            // 2 operations in progress
            ValidateStack(Trace.CorrelationManager.LogicalOperationStack, "two", "one");
            Trace.CorrelationManager.StopLogicalOperation();
            ValidateStack(Trace.CorrelationManager.LogicalOperationStack, "one");

            await Task.Delay(1);
            Trace.CorrelationManager.StopLogicalOperation();
            ValidateStack(Trace.CorrelationManager.LogicalOperationStack);
        }

        private static void ValidateStack(Stack input, params object[] expectedContents)
        {
            Assert.Equal(expectedContents.Length, input.Count);
            // Note: this reverts the stack 
            Stack currentStack = new Stack(input);

            // The expected values are passed in in the order they are supposed to be in the original stack
            // so we need to match them from the end of the array since the stack is also reversed
            for (int i = expectedContents.Length - 1; i >= 0; i--)
            {
                Assert.Equal(expectedContents[i], currentStack.Pop());
            }
        }
    }
}
