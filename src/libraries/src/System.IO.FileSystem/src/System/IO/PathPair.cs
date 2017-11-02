// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    internal readonly struct PathPair
    {
        internal readonly string UserPath;
        internal readonly string FullPath;

        internal PathPair(string userPath, string fullPath)
        {
            UserPath = userPath;
            FullPath = fullPath;
        }
    }
}
