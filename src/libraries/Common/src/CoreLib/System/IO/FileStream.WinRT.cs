// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace System.IO
{
    public partial class FileStream : Stream
    {
        private unsafe SafeFileHandle OpenHandle(FileMode mode, FileShare share, FileOptions options)
        {
            Interop.Kernel32.SECURITY_ATTRIBUTES secAttrs = GetSecAttrs(share);

            int access =
                ((_access & FileAccess.Read) == FileAccess.Read ? GENERIC_READ : 0) |
                ((_access & FileAccess.Write) == FileAccess.Write ? GENERIC_WRITE : 0);

            // Our Inheritable bit was stolen from Windows, but should be set in
            // the security attributes class.  Don't leave this bit set.
            share &= ~FileShare.Inheritable;

            // Must use a valid Win32 constant here...
            if (mode == FileMode.Append)
                mode = FileMode.OpenOrCreate;

            Interop.Kernel32.CREATEFILE2_EXTENDED_PARAMETERS parameters = new Interop.Kernel32.CREATEFILE2_EXTENDED_PARAMETERS();
            parameters.dwSize = (uint)sizeof(Interop.Kernel32.CREATEFILE2_EXTENDED_PARAMETERS);
            parameters.dwFileFlags = (uint)options;
            parameters.lpSecurityAttributes = &secAttrs;

            using (DisableMediaInsertionPrompt.Create())
            {
                return ValidateFileHandle(Interop.Kernel32.CreateFile2(
                    lpFileName: _path,
                    dwDesiredAccess: access,
                    dwShareMode: share,
                    dwCreationDisposition: mode,
                    pCreateExParams: ref parameters));
            }
        }
    }
}
