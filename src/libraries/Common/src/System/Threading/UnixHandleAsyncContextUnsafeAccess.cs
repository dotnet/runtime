// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Threading
{
    internal static class UnixHandleAsyncContextUnsafeAccess
    {
        private const string AsyncContextTypeName = "System.Threading.UnixHandleAsyncContext, System.Private.CoreLib";
        private const string OperationTypeName = "System.Threading.UnixHandleAsyncContext+Operation, System.Private.CoreLib";
        private const string DelegateOperationTypeName = "System.Threading.UnixHandleAsyncContext+DelegateOperation, System.Private.CoreLib";

        public enum AsyncResult
        {
            Pending = 0,
            Completed = 1,
            Aborted = 2,
        }

        public enum SyncResult
        {
            Completed = 1,
            Aborted = 2,
            TimedOut = 4,
        }

        public enum OnCompletedResult
        {
            Completed = 1,
            Aborted = 2,
            Canceled = 3,
        }

        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        [return: UnsafeAccessorType(AsyncContextTypeName)]
        private static extern object CreateAsyncContext(SafeHandle handle);

        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        [return: UnsafeAccessorType(DelegateOperationTypeName)]
        private static extern object CreateDelegateOperation(Func<SafeHandle, bool> tryComplete, Action<int> onCompleted);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "IsReadReady")]
        private static extern bool IsReadReady(
            [UnsafeAccessorType(AsyncContextTypeName)] object context,
            out int observedSequenceNumber);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "IsWriteReady")]
        private static extern bool IsWriteReady(
            [UnsafeAccessorType(AsyncContextTypeName)] object context,
            out int observedSequenceNumber);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "StartAsyncReadAsInt")]
        private static extern int StartAsyncRead(
            [UnsafeAccessorType(AsyncContextTypeName)] object context,
            [UnsafeAccessorType(OperationTypeName)] object operation,
            int observedSequenceNumber,
            CancellationToken cancellationToken);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "StartAsyncWriteAsInt")]
        private static extern int StartAsyncWrite(
            [UnsafeAccessorType(AsyncContextTypeName)] object context,
            [UnsafeAccessorType(OperationTypeName)] object operation,
            int observedSequenceNumber,
            CancellationToken cancellationToken);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ReadAsInt")]
        private static extern int ReadSync(
            [UnsafeAccessorType(AsyncContextTypeName)] object context,
            [UnsafeAccessorType(OperationTypeName)] object operation,
            int observedSequenceNumber,
            int timeout);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "WriteAsInt")]
        private static extern int WriteSync(
            [UnsafeAccessorType(AsyncContextTypeName)] object context,
            [UnsafeAccessorType(OperationTypeName)] object operation,
            int observedSequenceNumber,
            int timeout);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "AbortAndDispose")]
        private static extern bool AbortAndDispose(
            [UnsafeAccessorType(AsyncContextTypeName)] object context);

        public sealed class AsyncContext
        {
            private readonly object _context;

            public AsyncContext(SafeHandle handle)
            {
                _context = CreateAsyncContext(handle);
            }

            public bool IsReadReady(out int observedSequenceNumber)
                => UnixHandleAsyncContextUnsafeAccess.IsReadReady(_context, out observedSequenceNumber);

            public bool IsWriteReady(out int observedSequenceNumber)
                => UnixHandleAsyncContextUnsafeAccess.IsWriteReady(_context, out observedSequenceNumber);

            public AsyncResult StartAsyncRead(Operation operation, int observedSequenceNumber, CancellationToken cancellationToken)
                => (AsyncResult)UnixHandleAsyncContextUnsafeAccess.StartAsyncRead(_context, operation.Instance, observedSequenceNumber, cancellationToken);

            public AsyncResult StartAsyncWrite(Operation operation, int observedSequenceNumber, CancellationToken cancellationToken)
                => (AsyncResult)UnixHandleAsyncContextUnsafeAccess.StartAsyncWrite(_context, operation.Instance, observedSequenceNumber, cancellationToken);

            public SyncResult Read(Operation operation, int observedSequenceNumber, int timeout)
                => (SyncResult)ReadSync(_context, operation.Instance, observedSequenceNumber, timeout);

            public SyncResult Write(Operation operation, int observedSequenceNumber, int timeout)
                => (SyncResult)WriteSync(_context, operation.Instance, observedSequenceNumber, timeout);

            public bool AbortAndDispose()
                => UnixHandleAsyncContextUnsafeAccess.AbortAndDispose(_context);

            public static Operation CreateOperation(Func<SafeHandle, bool> tryComplete, Action<OnCompletedResult> onCompleted)
                => new Operation(tryComplete, result => onCompleted((OnCompletedResult)result));

            public readonly struct Operation
            {
                public object Instance { get; }

                internal Operation(Func<SafeHandle, bool> tryComplete, Action<int> onCompleted)
                {
                    Instance = CreateDelegateOperation(tryComplete, onCompleted);
                }
            }
        }
    }
}
