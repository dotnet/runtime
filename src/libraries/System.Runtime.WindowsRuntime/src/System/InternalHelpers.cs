// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        internal static unsafe void ZeroMemory(byte* src, long len)
        {
            while (len-- > 0)
                *(src + len) = 0;
        }
    }
}
