// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using System.Threading;

namespace System.Runtime.WindowsRuntime.Internal
{
    internal static class __Error
    {
        internal static void StreamIsClosed()
        {
            throw new ObjectDisposedException(null, SR.ObjectDisposed_StreamClosed);
        }

        internal static void SeekNotSupported()
        {
            throw new NotSupportedException(SR.NotSupported_UnseekableStream);
        }

        internal static void ReadNotSupported()
        {
            throw new NotSupportedException(SR.NotSupported_UnreadableStream);
        }

        internal static void WriteNotSupported()
        {
            throw new NotSupportedException(SR.NotSupported_UnwritableStream);
        }

        internal static void SetErrorCode(this Exception ex, int code)
        {
            // Stub, until COM interop guys fix the exception logic
        }

        internal static void TryDeregister(this CancellationTokenRegistration ctr)
        {
            //nothing to do for projectN
        }
    }

    internal class Helpers
    {
        private static Task s_completedTask = null;

        internal static Task CompletedTask
        {
            get
            {
                var completedTask = s_completedTask;
                if (completedTask == null)
                {
                    var taskSource = new TaskCompletionSource<VoidValueTypeParameter>();
                    taskSource.SetResult(default(VoidValueTypeParameter));
                    s_completedTask = completedTask = taskSource.Task;
                }
                return completedTask;
            }
        }

        internal static Task<TYPE> TaskFromException<TYPE>(Exception e)
        {
            var taskSource = new TaskCompletionSource<TYPE>();
            taskSource.SetException(e);
            return taskSource.Task;
        }

        internal static Task<TYPE> TaskFromCancellation<TYPE>(CancellationToken cancellationToken)
        {
            var taskSource = new TaskCompletionSource<TYPE>();
            taskSource.SetCanceled();
            taskSource.Task.Wait(cancellationToken);
            return taskSource.Task;
        }

        internal unsafe static void ZeroMemory(byte* src, long len)
        {
            while (len-- > 0)
                *(src + len) = 0;
        }
    }
}
