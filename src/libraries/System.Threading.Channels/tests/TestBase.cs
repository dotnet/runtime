// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable 0649 // unused fields there for future testing needs

namespace System.Threading.Channels.Tests
{
    public abstract class TestBase
    {
        public static IEnumerable<object[]> ThreeBools =>
            from b1 in new[] { false, true }
            from b2 in new[] { false, true }
            from b3 in new[] { false, true }
            select new object[] { b1, b2, b3 };

        protected void AssertSynchronouslyCanceled(Task task, CancellationToken token)
        {
            Assert.Equal(TaskStatus.Canceled, task.Status);
            OperationCanceledException oce = Assert.ThrowsAny<OperationCanceledException>(() => task.GetAwaiter().GetResult());
            if (PlatformDetection.IsNetCore)
            {
                // Earlier netstandard versions didn't have the APIs to always make this possible.
                Assert.Equal(token, oce.CancellationToken);
            }
        }

        protected void AssertSynchronousSuccess<T>(ValueTask<T> task) => Assert.True(task.IsCompletedSuccessfully);
        protected void AssertSynchronousSuccess(ValueTask task) => Assert.True(task.IsCompletedSuccessfully);
        protected void AssertSynchronousSuccess(Task task) => Assert.Equal(TaskStatus.RanToCompletion, task.Status);

        protected static object CreateSynchronouslyCanceledAsyncOperation(
            string typeName,
            CancellationToken cancellationToken,
            Type genericTypeArgument = null)
        {
            Type operationType = typeof(Channel<int>).Assembly.GetType(typeName, throwOnError: true);
            if (genericTypeArgument is not null)
            {
                operationType = operationType.MakeGenericType(genericTypeArgument);
            }

            MethodInfo trySetCanceled = operationType.GetMethod("TrySetCanceled", BindingFlags.Instance | BindingFlags.Public);
            var cancellationCallback = new Action<object, CancellationToken>((state, token) =>
            {
                Assert.True((bool)trySetCanceled.Invoke(state, new object[] { token }));
            });

            return Activator.CreateInstance(
                operationType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { true, cancellationToken, false, cancellationCallback },
                culture: null);
        }

        protected static object GetChannelParent<T>(Channel<T> channel) =>
            channel.Reader.GetType().GetField("_parent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(channel.Reader);

        protected static ValueTask<T> GetOperationValueTask<T>(object operation) =>
            (ValueTask<T>)operation.GetType().GetProperty("ValueTaskOfT", BindingFlags.Instance | BindingFlags.Public).GetValue(operation);

        protected static (object Next, object Previous) GetOperationLinks(object operation)
        {
            Type operationType = operation.GetType();
            return (
                operationType.GetProperty("Next", BindingFlags.Instance | BindingFlags.Public).GetValue(operation),
                operationType.GetProperty("Previous", BindingFlags.Instance | BindingFlags.Public).GetValue(operation));
        }

        protected static object GetOperationListHead(object parent, string fieldName) =>
            parent.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(parent);

        protected static void RemoveOperationFromList(object parent, string fieldName, object operation)
        {
            FieldInfo headField = parent.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Type utilitiesType = typeof(Channel<int>).Assembly.GetType("System.Threading.Channels.ChannelUtilities", throwOnError: true);
            MethodInfo remove = utilitiesType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                .Single(method => method.Name == "Remove")
                .MakeGenericMethod(operation.GetType());
            var arguments = new object[] { headField.GetValue(parent), operation };

            remove.Invoke(null, arguments);
            headField.SetValue(parent, arguments[0]);
        }

        protected static void SetOperationListHead(object parent, string fieldName, object operation)
        {
            Type operationType = operation.GetType();
            operationType.GetProperty("Next", BindingFlags.Instance | BindingFlags.Public).SetValue(operation, operation);
            operationType.GetProperty("Previous", BindingFlags.Instance | BindingFlags.Public).SetValue(operation, operation);
            parent.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(parent, operation);
        }

        protected static bool TryReserveOperation(object operation) =>
            (bool)operation.GetType().GetMethod("TryReserveCompletionIfCancelable", BindingFlags.Instance | BindingFlags.Public).Invoke(operation, null);

        protected void AssertSynchronousTrue(Task<bool> task)
        {
            AssertSynchronousSuccess(task);
            Assert.True(task.Result);
        }

        protected void AssertSynchronousTrue(ValueTask<bool> task)
        {
            AssertSynchronousSuccess(task);
            Assert.True(task.Result);
        }

        internal sealed class DelegateObserver<T> : IObserver<T>
        {
            public Action<T> OnNextDelegate = null;
            public Action<Exception> OnErrorDelegate = null;
            public Action OnCompletedDelegate = null;

            void IObserver<T>.OnNext(T value) => OnNextDelegate?.Invoke(value);

            void IObserver<T>.OnError(Exception error) => OnErrorDelegate?.Invoke(error);

            void IObserver<T>.OnCompleted() => OnCompletedDelegate?.Invoke();
        }
    }
}
