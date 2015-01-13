// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security;

namespace System.IO
{
    // Only static data; no need to serialize
    internal static class __Error
    {
        internal static Exception GetStreamIsClosed()
        {
            return new ObjectDisposedException(null, Strings.ObjectDisposed_StreamIsClosed);
        }

        internal static Exception GetReadNotSupported()
        {
            return new NotSupportedException(Strings.NotSupported_UnreadableStream);
        }

        internal static Exception GetWriteNotSupported()
        {
            return new NotSupportedException(Strings.NotSupported_UnwritableStream);
        }
    }
}
