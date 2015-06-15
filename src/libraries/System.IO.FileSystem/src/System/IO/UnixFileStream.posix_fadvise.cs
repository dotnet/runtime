// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.IO
{
    /// <summary>Provides an implementation of a file stream for Unix files.</summary>
    internal sealed partial class UnixFileStream : FileStreamBase
    {
        /// <summary>Performs additional configuration of the opened stream based on provided options.</summary>
        partial void PostOpenConfigureStreamFromOptions()
        {
            // Support additional options after the file has been opened.
            // These provide hints around how the file will be accessed.
            Interop.libc.Advice fadv =
                _options == FileOptions.RandomAccess ? Interop.libc.Advice.POSIX_FADV_RANDOM :
                _options == FileOptions.SequentialScan ? Interop.libc.Advice.POSIX_FADV_SEQUENTIAL :
                0;
            if (fadv != 0)
            {
                SysCall<Interop.libc.Advice, int>((fd, advice, _) => Interop.libc.posix_fadvise(fd, 0, 0, advice), fadv);
            }
        }
    }
}