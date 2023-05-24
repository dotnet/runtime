// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO
{
    internal static partial class FileSystem
    {
#pragma warning disable IDE0060 // Remove unused parameter
         private static bool TryCloneFile(string sourceFullPath, string destFullPath, bool overwrite)
#pragma warning restore IDE0060 // Remove unused parameter
         {
            // No such functionality is available on unix OSes (other than OSX-like ones).
            return false;
         }
    }
}
