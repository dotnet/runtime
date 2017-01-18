// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Text;

internal partial class Interop
{
    internal partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, EntryPoint = "GetVolumeInformationW", CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
        internal static extern bool GetVolumeInformation(string drive, [Out]StringBuilder volumeName, int volumeNameBufLen, out int volSerialNumber, out int maxFileNameLen, out int fileSystemFlags, [Out]StringBuilder fileSystemName, int fileSystemNameBufLen);
    }
}
