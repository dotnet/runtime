// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.IO
{
    internal static partial class FileSystem
    {
        public static unsafe void CreateDirectory(string fullPath, byte[]? securityDescriptor = null)
        {
            const int preAllocationSize = 8;

            // We can save a bunch of work if the directory we want to create already exists.  This also
            // saves us in the case where sub paths are inaccessible (due to ERROR_ACCESS_DENIED) but the
            // final path is accessible and the directory already exists.  For example, consider trying
            // to create c:\Foo\Bar\Baz, where everything already exists but ACLS prevent access to c:\Foo
            // and c:\Foo\Bar. In that case, this code will think it needs to create c:\Foo, and c:\Foo\Bar
            // and fail to due so, causing an exception to be thrown.  This is not what we want.
            if (DirectoryExists(fullPath))
            {
                return;
            }

            // Preallocate list to improve performance
            // We use a constant because counting the `/` characters
            // would be too costly from a performance point of view, despite vectorization.
            List<string> stackDir = new(preAllocationSize);

            // Attempt to figure out which directories don't exist, and only
            // create the ones we need.  Note that FileExists may fail due
            // to Win32 ACL's preventing us from seeing a directory, and this
            // isn't thread safe.
            bool somePathExists = false;

            ReadOnlySpan<char> fullPathSpan = fullPath;
            int length = fullPathSpan.Length;

            // We need to trim the trailing slash or the code will try to create 2 directories of the same name.
            if (length >= 2 && PathInternal.EndsInDirectorySeparatorUnchecked(fullPathSpan))
            {
                length--;
            }

            int lengthRoot = PathInternal.GetRootLength(fullPathSpan);

            if (length > lengthRoot)
            {
                // Special case root (fullPath = X:\\)
                int i = length - 1;
                while (i >= lengthRoot && !somePathExists)
                {
                    string dir = fullPath[..(i + 1)];

                    // Create only the ones missing
                    if (!DirectoryExists(dir))
                    {
                        stackDir.Add(dir);
                    }
                    else
                    {
                        somePathExists = true;
                    }

                    while (i > lengthRoot && !PathInternal.IsDirectorySeparator(fullPath[i]))
                    {
                        i--;
                    }

                    i--;
                }
            }

            int count = stackDir.Count;
            bool r = true;
            int firstError = 0;
            string errorString = fullPath;

            fixed (byte* pSecurityDescriptor = securityDescriptor)
            {
                Interop.Kernel32.SECURITY_ATTRIBUTES secAttrs = new()
                {
                    nLength = (uint)sizeof(Interop.Kernel32.SECURITY_ATTRIBUTES),
                    lpSecurityDescriptor = (IntPtr)pSecurityDescriptor
                };

                while (stackDir.Count > 0)
                {
                    int removalIndex = stackDir.Count - 1;
                    string name = stackDir[removalIndex];
                    stackDir.RemoveAt(removalIndex);

                    r = Interop.Kernel32.CreateDirectory(name, ref secAttrs);
                    if (!r && firstError == 0)
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
                            (bool fileExists, bool dirExists) = PathExists(name, out currentError);

                            // If there's a file in this directory's place, or if we have ERROR_ACCESS_DENIED when checking if the directory already exists throw.
                            if (fileExists || !dirExists && currentError == Interop.Errors.ERROR_ACCESS_DENIED)
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
            if (count == 0 && !somePathExists)
            {
                string? root = Path.GetPathRoot(fullPath);
                if (!DirectoryExists(root))
                {
                    throw Win32Marshal.GetExceptionForWin32Error(Interop.Errors.ERROR_PATH_NOT_FOUND, root);
                }

                return;
            }

            // Only throw an exception if creating the exact directory we
            // wanted failed to work correctly.
            if (!r && firstError != 0)
            {
                throw Win32Marshal.GetExceptionForWin32Error(firstError, errorString);
            }
        }
    }
}
