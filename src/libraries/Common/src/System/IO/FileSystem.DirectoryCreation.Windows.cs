// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

            fixed (byte* pSecurityDescriptor = securityDescriptor)
            {
                Interop.Kernel32.SECURITY_ATTRIBUTES secAttrs = new Interop.Kernel32.SECURITY_ATTRIBUTES
                {
                    nLength = (uint)sizeof(Interop.Kernel32.SECURITY_ATTRIBUTES),
                    lpSecurityDescriptor = (IntPtr)pSecurityDescriptor
                };

                // Every call to CreateDirectory uses EnsureExtendedPrefix on the supplied path, which
                // creates a new string if the prefix was missing.  Since we're making at least one and
                // potentially multiple calls to CreateDirectory, we lift out that prefixing so that the
                // string that's created can be shared across all calls rather than having a temporary in each.
                fullPath = PathInternal.EnsureExtendedPrefix(fullPath);

                // We know the directory doesn't exist.  Before doing more work to parse the path into
                // subdirectories and checking each one for existence, try to just create the directory.
                // If it works, which is a common case when the parent directory already exists, we're done.
                // Otherwise, ignore the error and proceed with the full handling. This makes a fast path
                // faster and a slow path a little bit slower.
                if (Interop.Kernel32.CreateDirectory(fullPath, &secAttrs))
                {
                    return;
                }

                // Use enough stack space to hold three directory paths.  After that, ValueListBuilder
                // will grow with ArrayPool arrays.
                ThreeObjects scratch = default;
                var stackDir = new ValueListBuilder<string>(MemoryMarshal.CreateSpan(ref Unsafe.As<object, string>(ref scratch.Arg0!), 3));

                // Attempt to figure out which directories don't exist, and only
                // create the ones we need.  Note that FileExists may fail due
                // to Win32 ACL's preventing us from seeing a directory, and this
                // isn't threadsafe.

                bool somepathexists = false;
                int length = fullPath.Length;

                // We need to trim the trailing slash or the code will try to create 2 directories of the same name.
                if (length >= 2 && PathInternal.EndsInDirectorySeparator(fullPath.AsSpan()))
                {
                    length--;
                }

                int lengthRoot = PathInternal.GetRootLength(fullPath.AsSpan());

                if (length > lengthRoot)
                {
                    // Special case root (fullpath = X:\\)
                    int i = length - 1;
                    while (i >= lengthRoot && !somepathexists)
                    {
                        string dir = fullPath.Substring(0, i + 1);

                        if (!DirectoryExists(dir)) // Create only the ones missing
                        {
                            stackDir.Append(dir);
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

                int count = stackDir.Length;
                bool r = true;
                int firstError = 0;
                string errorString = fullPath;

                while (stackDir.Length > 0)
                {
                    ref string slot = ref stackDir[^1];
                    string name = slot;
                    slot = null!; // to avoid keeping these strings alive in pooled arrays
                    stackDir.Length--;

                    r = Interop.Kernel32.CreateDirectory(name, &secAttrs);
                    if (!r && (firstError == 0))
                    {
                        // While we tried to avoid creating directories that don't
                        // exist above, there are at least two cases that will
                        // cause us to see ERROR_ALREADY_EXISTS here.  FileExists
                        // can fail because we didn't have permission to the
                        // directory.  Secondly, another thread or process could
                        // create the directory between the time we check and the
                        // time we try using the directory.  Thirdly, it could
                        // fail because the target does exist, but is a file.
                        int currentError = Marshal.GetLastWin32Error();
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

                stackDir.Dispose();

                // We need this check to mask OS differences
                // Handle CreateDirectory("X:\\") when X: doesn't exist. Similarly for n/w paths.
                if ((count == 0) && !somepathexists)
                {
                    string? root = Path.GetPathRoot(fullPath);
                    if (root is null || !DirectoryExists(root))
                    {
                        throw Win32Marshal.GetExceptionForWin32Error(Interop.Errors.ERROR_PATH_NOT_FOUND, root);
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
}
