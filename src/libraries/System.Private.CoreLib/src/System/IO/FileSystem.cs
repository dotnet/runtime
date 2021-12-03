// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace System.IO
{
    internal static partial class FileSystem
    {
        internal static void VerifyValidPath(string path, string argName)
        {
            if (path == null)
            {
                throw new ArgumentNullException(argName);
            }
            else if (path.Length == 0)
            {
                throw new ArgumentException(SR.Arg_PathEmpty, argName);
            }
            else if (path.Contains('\0'))
            {
                throw new ArgumentException(SR.Argument_InvalidPathChars, argName);
            }
        }

        internal static bool Copy(string sourcePath, string destinationPath, bool recursive,
            bool skipExistingFiles, CancellationToken cancellationToken)
        {
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            IEnumerable<string> directoryEnumeration;

            try
            {
                directoryEnumeration = Directory.EnumerateDirectories(sourcePath, "*", searchOption);
            }
            catch (IOException)
            {
                // Return false if Enumeration is not possible
                return false;
            }

            foreach (string enumeratedDirectory in directoryEnumeration)
            {
                string newDirectoryPath = enumeratedDirectory.Replace(sourcePath, destinationPath);
                Directory.CreateDirectory(newDirectoryPath);

                cancellationToken.ThrowIfCancellationRequested();
            }

            foreach (string enumeratedFile in Directory.GetFiles(sourcePath, "*.*", searchOption))
            {
                if (skipExistingFiles && File.Exists(enumeratedFile))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    continue;
                }

                try
                {
                    CopyFile(enumeratedFile, enumeratedFile.Replace(sourcePath, destinationPath), true);
                }
                catch (IOException ex)
                {
                    // Return false for read failures
                    return ex.HResult == Interop.Errors.ERROR_ACCESS_DENIED ? false :
                        // Otherwise rethrow
                        throw ex;
                }

                cancellationToken.ThrowIfCancellationRequested();
            }

            return false;
        }
    }
}
