// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

internal static partial class Interop
{
    /// <summary>Common Unix errno error codes.</summary>
    internal static partial class Errors
    {
        // These values were defined in:
        // include/asm-generic/errno-base.h
        // /usr/include/sys/errno.h

        internal const int EPERM = 1;
        internal const int ENOENT = 2;
        internal const int ESRCH = 3;
        internal const int EINTR = 4;
        internal const int EIO = 5;
        internal const int ENXIO = 6;
        internal const int E2BIG = 7;
        internal const int ENOEXEC = 8;
        internal const int EBADF = 9;
        internal const int ECHILD = 10;

        internal const int ENOMEM = 12;
        internal const int EACCES = 13;
        internal const int EFAULT = 14;
        internal const int ENOTBLK = 15;
        internal const int EBUSY = 16;
        internal const int EEXIST = 17;
        internal const int EXDEV = 18;
        internal const int ENODEV = 19;
        internal const int ENOTDIR = 20;
        internal const int EISDIR = 21;
        internal const int EINVAL = 22;
        internal const int ENFILE = 23;
        internal const int EMFILE = 24;
        internal const int ENOTTY = 25;
        internal const int ETXTBSY = 26;
        internal const int EFBIG = 27;
        internal const int ENOSPC = 28;
        internal const int ESPIPE = 29;
        internal const int EROFS = 30;
        internal const int EMLINK = 31;
        internal const int EPIPE = 32;
        internal const int EDOM = 33;
        internal const int ERANGE = 34;
    }
}
