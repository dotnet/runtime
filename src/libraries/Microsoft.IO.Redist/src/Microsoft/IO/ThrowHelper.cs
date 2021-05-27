// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.IO
{
    internal static class ThrowHelper
    {
        internal static void ThrowEndOfFileException()
        {
            throw new EndOfStreamException(SR.IO_EOF_ReadBeyondEOF);
        }
    }
}
