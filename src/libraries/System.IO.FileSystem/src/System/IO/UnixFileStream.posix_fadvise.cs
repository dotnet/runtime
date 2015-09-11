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
            Interop.Sys.FileAdvice fadv =
                _options == FileOptions.RandomAccess ? Interop.Sys.FileAdvice.POSIX_FADV_RANDOM :
                _options == FileOptions.SequentialScan ? Interop.Sys.FileAdvice.POSIX_FADV_SEQUENTIAL :
                0;
            if (fadv != 0)
            {
                SysCall<Interop.Sys.FileAdvice, int>((fd, advice, _) => Interop.Sys.PosixFAdvise(fd, 0, 0, advice), fadv);
            }
        }
    }
}