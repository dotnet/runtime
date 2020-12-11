// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace R2RTest
{
    internal static class Linux
    {
        [Flags]
        private enum Permissions : byte
        {
            Read = 1,
            Write = 2,
            Execute = 4,

            ReadExecute = Read | Execute,

            ReadWriteExecute = Read | Write | Execute,
        }

        private enum PermissionGroupShift : int
        {
            Owner = 6,
            Group = 3,
            Other = 0,
        }

        [DllImport("libc", SetLastError = true)]
        private static extern int chmod(string path, int flags);

        public static void MakeExecutable(string path)
        {
            int errno = chmod(path,
                ((byte)Permissions.ReadWriteExecute << (int)PermissionGroupShift.Owner) |
                ((byte)Permissions.ReadExecute << (int)PermissionGroupShift.Group) |
                ((byte)Permissions.ReadExecute << (int)PermissionGroupShift.Other));

            if (errno != 0)
            {
                throw new Exception($@"Failed to set permissions on {path}: error code {errno}");
            }
        }
    }

}
