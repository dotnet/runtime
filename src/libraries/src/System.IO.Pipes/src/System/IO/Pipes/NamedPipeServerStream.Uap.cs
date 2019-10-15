// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.IO.Pipes
{
    /// <summary>
    /// Named pipe server
    /// </summary>
    public sealed partial class NamedPipeServerStream : PipeStream
    {
        // Depending on the Windows platform, we will try to reload a potentially missing DLL
        // and reattempt the retrieval of the impersonation username.
        private unsafe string HandleGetImpersonationUserNameError(int error, uint userNameMaxLength, char* userName)
        {
            // UAP does not allow calling LoadLibraryEx to try to load sspicli.dll like
            // in Win32, so we won't retry and will directly throw the passed error.
            throw WinIOError(error);
        }
    }
}
