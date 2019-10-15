// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;

namespace System.IO.Pipes.Tests
{
    /// <summary>
    /// The class contains interop declarations and helpers methods for them.
    /// </summary>
    internal static partial class InteropTest
    {
        private static unsafe bool TryHandleGetImpersonationUserNameError(SafePipeHandle handle, int error, uint userNameMaxLength, char* userName, out string impersonationUserName)
        {
            // Uap does not allow loading libraries
            impersonationUserName = string.Empty;
            return false;
        }
    }
}
