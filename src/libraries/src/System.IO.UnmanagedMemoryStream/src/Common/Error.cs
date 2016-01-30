// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security;

namespace System.IO
{
    // Only static data; no need to serialize
    internal static class Error
    {
        internal static Exception GetStreamIsClosed()
        {
            return new ObjectDisposedException(null, SR.ObjectDisposed_StreamIsClosed);
        }

        internal static Exception GetReadNotSupported()
        {
            return new NotSupportedException(SR.NotSupported_UnreadableStream);
        }

        internal static Exception GetWriteNotSupported()
        {
            return new NotSupportedException(SR.NotSupported_UnwritableStream);
        }
    }
}
