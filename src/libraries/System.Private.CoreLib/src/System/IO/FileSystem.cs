// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if MS_IO_REDIST
using System;

namespace Microsoft.IO
#else
namespace System.IO
#endif
{
    internal static partial class FileSystem
    {
        internal static void VerifyValidPath(string path, string argName)
        {
            if (path == null)
            {
                throw new ArgumentNullException(argName);
            }
            else if (path.Length == 0)
            {
                throw new ArgumentException(SR.Arg_PathEmpty, argName);
            }
            else if (path.Contains('\0'))
            {
                throw new ArgumentException(SR.Argument_InvalidPathChars, argName);
            }
        }
    }
}
