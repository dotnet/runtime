// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;

namespace System.IO
{
    internal static partial class FileSystem
    {
        public static unsafe void CreateDirectory(string fullPath, byte[]? securityDescriptor = null)
        {
            // We can save a bunch of work if the directory we want to create already exists.  This also
            // saves us in the case where sub paths are inaccessible (due to ERROR_ACCESS_DENIED) but the
            // final path is accessible and the directory already exists.  For example, consider trying
            // to create c:\Foo\Bar\Baz, where everything already exists but ACLS prevent access to c:\Foo
            // and c:\Foo\Bar.  In that case, this code will think it needs to create c:\Foo, and c:\Foo\Bar
            // and fail to due so, causing an exception to be thrown.  This is not what we want.
            if (DirectoryExists(fullPath))
            {
                return;
            }

            List<string> stackDir = new List<string>();

            // Attempt to figure out which directories don't exist, and only
            // create the ones we need.  Note that FileExists may fail due
            // to Win32 ACL's preventing us from seeing a directory, and this
            // isn't threadsafe.

            bool somepathexists = false;

            ReadOnlySpan<char> fullPathSpan = fullPath;
            int length = fullPathSpan.Length;

            // We need to trim the trailing slash or the code will try to create 2 directories of the same name.
            if (length >= 2 && PathInternal.IsDirectorySeparator(fullPathSpan[^1]))
            {
                length--;
            }

            int lengthRoot = PathInternal.GetRootLength(fullPathSpan);

            using (DisableMediaInsertionPrompt.Create())
            {
                if (length > lengthRoot)
                {
                    // Special case root (fullpath = X:\\)
                    int i = length - 1;
                    while (i >= lengthRoot && !somepathexists)
                    {
                        ReadOnlySpan<char> dir = fullPathSpan[..(i + 1)];

                        // Neither GetFileAttributes or FindFirstFile like trailing separators
                        dir = PathInternal.TrimEndingDirectorySeparator(dir);

                        string dirString = new(dir);

                        if (!DirectoryExistsSlim(dirString, out _)) // Create only the ones missing
                        {
                            stackDir.Add(dirString);
                        }
                        else
                        {
                            somepathexists = true;
                        }

                        while (i > lengthRoot && !PathInternal.IsDirectorySeparator(fullPath[i]))
                        {
                            i--;
                        }

                        i--;
                    }
                }
            }

            int count = stackDir.Count;
            bool r = true;
            int firstError = 0;
            string errorString = fullPath;

            fixed (byte* pSecurityDescriptor = securityDescriptor)
            {
                Interop.Kernel32.SECURITY_ATTRIBUTES secAttrs = new Interop.Kernel32.SECURITY_ATTRIBUTES
                {
                    nLength = (uint)sizeof(Interop.Kernel32.SECURITY_ATTRIBUTES),
                    lpSecurityDescriptor = (IntPtr)pSecurityDescriptor
                };

                for (int i = count - 1; i >= 0; i--)
                {
                    string name = stackDir[i];

                    r = Interop.Kernel32.CreateDirectory(name, ref secAttrs);
                    if (!r && (firstError == 0))
                    {
                        int currentError = Marshal.GetLastWin32Error();
                        // While we tried to avoid creating directories that don't
                        // exist above, there are at least two cases that will
                        // cause us to see ERROR_ALREADY_EXISTS here.  FileExists
                        // can fail because we didn't have permission to the
                        // directory.  Secondly, another thread or process could
                        // create the directory between the time we check and the
                        // time we try using the directory.  Thirdly, it could
                        // fail because the target does exist, but is a file.
                        if (currentError != Interop.Errors.ERROR_ALREADY_EXISTS)
                        {
                            firstError = currentError;
                        }
                        else
                        {
                            // If there's a file in this directory's place, or if we have ERROR_ACCESS_DENIED when checking if the directory already exists throw.
                            if (FileExists(name) || (!DirectoryExists(name, out currentError) && currentError == Interop.Errors.ERROR_ACCESS_DENIED))
                            {
                                firstError = currentError;
                                errorString = name;
                            }
                        }
                    }
                }
            }

            // We need this check to mask OS differences
            // Handle CreateDirectory("X:\\") when X: doesn't exist. Similarly for n/w paths.
            if ((count == 0) && !somepathexists)
            {
                ReadOnlySpan<char> root = Path.GetPathRoot(fullPathSpan);
                string rootString = new(root);

                if (!DirectoryExists(rootString))
                {
                    throw Win32Marshal.GetExceptionForWin32Error(Interop.Errors.ERROR_PATH_NOT_FOUND, rootString);
                }

                return;
            }

            // Only throw an exception if creating the exact directory we
            // wanted failed to work correctly.
            if (!r && (firstError != 0))
            {
                throw Win32Marshal.GetExceptionForWin32Error(firstError, errorString);
            }
        }
    }
}
