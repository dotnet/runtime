// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.IO
{
    internal abstract partial class FileSystem
    {
        private static readonly FileSystem s_current = new Win32FileSystem();
    }
}
