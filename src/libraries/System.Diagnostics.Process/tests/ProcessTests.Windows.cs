// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics.Tests
{
    public partial class ProcessTests
    {
        private string WriteScriptFile(string directory, string name, int returnValue)
        {
            string filename = Path.Combine(directory, name);
            filename += ".bat";
            File.WriteAllText(filename, $"exit {returnValue}");
            return filename;
        }

        private static bool FileHandleIsValid(SafeFileHandle fileHandle)
        {
            return Interop.Kernel32.GetFileType(fileHandle) == Interop.Kernel32.FileTypes.FILE_TYPE_DISK;
        }
    }
}
