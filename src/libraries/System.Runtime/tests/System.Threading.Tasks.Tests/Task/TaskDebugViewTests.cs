// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using Xunit;

namespace System.Threading.Tasks.Tests
{
    public static class TaskDebugViewTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsDebuggerTypeProxyAttributeSupported))]
        public static void TaskOfTResult_DebugProxy_CompletedSuccessfully_ReturnsResult()
        {
            Task<string> task = Task.FromResult("expected");

            object proxy = DebuggerAttributes.GetProxyObject(task);
            PropertyInfo resultProperty = proxy.GetType().GetProperty("Result");
            object result = resultProperty.GetValue(proxy);

            Assert.Equal("expected", result);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsDebuggerTypeProxyAttributeSupported))]
        public static void TaskOfTResult_DebugProxy_Faulted_ResultThrows()
        {
            var tcs = new TaskCompletionSource<string>();
            tcs.SetException(new InvalidOperationException("task faulted"));
            Task<string> task = tcs.Task;

            object proxy = DebuggerAttributes.GetProxyObject(task);
            PropertyInfo resultProperty = proxy.GetType().GetProperty("Result");

            TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => resultProperty.GetValue(proxy));
            AggregateException ae = Assert.IsType<AggregateException>(tie.InnerException);
            Assert.IsType<InvalidOperationException>(ae.InnerException);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsDebuggerTypeProxyAttributeSupported))]
        public static void TaskOfTResult_DebugProxy_Canceled_ResultThrows()
        {
            var tcs = new TaskCompletionSource<string>();
            tcs.SetCanceled();
            Task<string> task = tcs.Task;

            object proxy = DebuggerAttributes.GetProxyObject(task);
            PropertyInfo resultProperty = proxy.GetType().GetProperty("Result");

            TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => resultProperty.GetValue(proxy));
            Assert.IsType<AggregateException>(tie.InnerException);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsDebuggerTypeProxyAttributeSupported))]
        public static void TaskOfTResult_DebugProxy_Incomplete_ResultReturnsDefault()
        {
            var tcs = new TaskCompletionSource<string>();
            Task<string> task = tcs.Task;

            object proxy = DebuggerAttributes.GetProxyObject(task);
            PropertyInfo resultProperty = proxy.GetType().GetProperty("Result");
            object result = resultProperty.GetValue(proxy);

            Assert.Null(result);

            tcs.SetResult("done");
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsDebuggerTypeProxyAttributeSupported))]
        public static void TaskOfTResult_DebugProxy_Incomplete_ValueType_ResultReturnsDefault()
        {
            var tcs = new TaskCompletionSource<int>();
            Task<int> task = tcs.Task;

            object proxy = DebuggerAttributes.GetProxyObject(task);
            PropertyInfo resultProperty = proxy.GetType().GetProperty("Result");
            object result = resultProperty.GetValue(proxy);

            Assert.Equal(0, result);

            tcs.SetResult(42);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsDebuggerTypeProxyAttributeSupported))]
        public static void TaskOfTResult_DebugProxy_ShowsCorrectStatus()
        {
            var tcs = new TaskCompletionSource<string>();
            tcs.SetException(new InvalidOperationException());
            Task<string> task = tcs.Task;

            object proxy = DebuggerAttributes.GetProxyObject(task);

            PropertyInfo statusProperty = proxy.GetType().GetProperty("Status");
            Assert.Equal(TaskStatus.Faulted, statusProperty.GetValue(proxy));

            PropertyInfo exceptionProperty = proxy.GetType().GetProperty("Exception");
            Assert.NotNull(exceptionProperty.GetValue(proxy));
        }
    }
}
