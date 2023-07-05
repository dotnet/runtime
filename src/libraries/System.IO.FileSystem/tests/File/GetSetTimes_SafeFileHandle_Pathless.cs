// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.IO.Tests
{
    public sealed class File_GetSetTimes_SafeFileHandle_Pathless : File_GetSetTimes_SafeFileHandle
    {
        protected override SafeFileHandle OpenFileHandle(string path, FileAccess fileAccess)
        {
            SafeFileHandle originHandle = base.OpenFileHandle(path, fileAccess);

            // Create handle by ptr to force that `SafeFileHandle.Path` is `null`
            SafeFileHandle newHandle = new(originHandle.DangerousGetHandle(), true);
            originHandle.SetHandleAsInvalid();

            return newHandle;
        }
    }
}
