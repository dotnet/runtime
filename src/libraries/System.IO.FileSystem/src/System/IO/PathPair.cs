// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.IO
{
    internal struct PathPair
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
