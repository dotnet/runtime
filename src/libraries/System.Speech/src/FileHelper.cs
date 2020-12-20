// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Security;
using System.Security.Permissions;

namespace System.IO
{
    static internal class FileHelper
    {
        // The normal way to create and open a temp file fails when an impolite
        // process opens the new file before we can (DDVSO 519951, 638468).
        // The CLR doesn't provide a better way of doing it (DDVSO 641087), so
        // we roll our own.   This method can and should be replaced once the
        // CLR provides a better way.
        //
        // An "impolite process" is one that doesn't use the technique described
        // in https://blogs.msdn.microsoft.com/oldnewthing/20130415-00/?p=4663
        // "How to write a polite indexer/scanner".
        //
        // Implementation discussion:  The normal usage of a temporary file is:
        //      1. create a new file
        //      2. write data into the file
        //      3. use the data:  pass the file (by name) to external components/services
        //      4. delete the file
        // This forces a requirement for creating and opening the file atomically
        // in step 1 - any method that leaves a window between create and open is
        // vulnerable to an effective denial-of-service attack by another process
        // that opens the file in the window and thus blocks our access in step 2.
        // The other process need not be  malicious;  an anti-virus scanner or a
        // file-system search indexer can interfere in just this way.
        //
        // This requirement rules out the "obvious" pattern:
        //      string path = Path.GetTempFileName();
        //      FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        // There's a vulnerable window between the two lines.
        // Similarly, the requirement rules out any pattern that doesn't create the
        // stream using FileMode.CreateNew. In fact, CLR team says that atomicity is
        // only guaranteed using both FileMode.CreateNew and FileShare.None (or more
        // precisely lack of FileShare.Write), and even that is only true on Windows.
        // Thus even this method won't necessarily work on non-Windows platforms.
        // (This can't be fixed by WPF.)
        //
        // [Using FileMode.CreateNew also avoids a possible security vulnerability
        // in which a malicious process exploits the vulnerable window to create
        // a symbolic link from the path to a system file, causing us to truncate
        // or overwrite the system file.   On Windows this is not really a vulnerability
        // since symbolic links require elevation.]
        //
        // Another requirement is that the temp file is actually new - we don't want
        // to write into an existing file.  Path.GetTempFileName() took care of that
        // by guaranteeing to return a unique name, but no such guarantee exists for
        // other ways of generating a name.  Instead, we use Path.GetRandomFileName(),
        // which produces a name that is unique with high probability.  Fortunately
        // FileMode.CreateNew fails if the name is already in use;   if so, we simply
        // try again with a different random name.   If this fails too often, we
        // assume the failure is for a reason deeper than name collision and give up.
        //
        // We do not set the delete-on-close option, since the normal use closes the
        // file after step 2 so that it can be passed to another component or
        // service in step 3.
        //
        // As a courtesy, this method can create the temp file in a subfolder of
        // the temp folder, specified by the caller.   This makes it easier to know
        // where temp files came from, in case they don't get deleted correctly.
        // (Path.GetTempFileName() doesn't support such a courtesy.)
        // A general/public implementation should check that this argument
        // really denotes a subfolder (e.g. disallow subFolder="..\..\..\Windows"),
        // to avoid creating temp files in arbitrary places, a potential security
        // hole.  This internal implementation can trust its callers to be honest.
        //
        ///<summary>
        /// Atomically create a new zero-length temporary file and open it in a file stream
        ///</summary>
        /// <param name="filePath">Path to the temp file.  Caller needs this to delete the file when done</param>
        /// <param name="fileAccess">desired access to the temp file (defaults to Write)</param>
        /// <param name="fileOptions">desired options for the temp file (defaults to None)</param>
        /// <param name="extension">desired extension, or null (defaults to null)</param>
        /// <param name="subFolder">desired subfolder of temp folder, or null (defaults to "WPF")</param>
        /// <SecurityNote>
        ///     Critical - Calls into filesystem functions, returns local file path.
        /// </SecurityNote>
        [SecurityCritical]
        static internal FileStream CreateAndOpenTemporaryFile(
                    out string filePath,
                    FileAccess fileAccess = FileAccess.Write,
                    FileOptions fileOptions = FileOptions.None,
                    string extension = null,
                    string subFolder = "WPF")
        {
            const int MaxRetries = 5;
            int retries = MaxRetries;
            filePath = null;
            bool needAsserts = System.Security.SecurityManager.CurrentThreadRequiresSecurityContextCapture();

            string folderPath = Path.GetTempPath();

            if (!String.IsNullOrEmpty(subFolder))
            {
                string subFolderPath = Path.Combine(folderPath, subFolder);

                if (!Directory.Exists(subFolderPath))
                {
                    if (!needAsserts)
                    {
                        Directory.CreateDirectory(subFolderPath);
                    }
                    else
                    {
                        new FileIOPermission(FileIOPermissionAccess.Read | FileIOPermissionAccess.Write, folderPath).Assert();
                        Directory.CreateDirectory(subFolderPath);
                        FileIOPermission.RevertAssert();
                    }
                }

                folderPath = subFolderPath;
            }

            if (needAsserts)
            {
                new FileIOPermission(FileIOPermissionAccess.Read | FileIOPermissionAccess.Write, folderPath).Assert();
            }

            FileStream stream = null;
            while (stream == null)
            {
                // build a candidate path name for the temp file
                string path = Path.Combine(folderPath, Path.GetRandomFileName());
                if (!String.IsNullOrEmpty(extension))
                {
                    path = Path.ChangeExtension(path, extension);
                }

                // try creating and opening the file
                --retries;
                try
                {
                    const int DefaultBufferSize = 4096;    // so says FileStream doc
                    // mode must be CreateNew and share must be None, see discussion above
                    stream = new FileStream(path, FileMode.CreateNew, fileAccess, FileShare.None, DefaultBufferSize, fileOptions);

                    // success, report the path name to the caller
                    filePath = path;
                }
                catch (Exception e) when (retries > 0 && (e is IOException || e is UnauthorizedAccessException))
                {
                    // failure - perhaps because a file with the candidate path name
                    // already exists.  Try again with a different candidate.
                    // If the failure happens too often, let the exception bubble out.
                }
            }

            return stream;
        }

        // PreSharp uses message numbers that the C# compiler doesn't know about.
        // Disable the C# complaints, per the PreSharp documentation.
#pragma warning disable 1634, 1691
#pragma warning disable 56502 // disable PreSharp warning about empty catch blocks

        ///<summary>
        /// Delete a temporary file robustly.
        ///</summary>
        /// <param name="filePath">Path to the temp file.</param>
        /// <SecurityNote>
        ///     Critical - Calls into filesystem functions, asserts permission.
        /// </SecurityNote>
        [SecurityCritical]
        static internal void DeleteTemporaryFile(string filePath)
        {
            if (!String.IsNullOrEmpty(filePath))
            {
                bool needAsserts = System.Security.SecurityManager.CurrentThreadRequiresSecurityContextCapture();
                if (needAsserts)
                {
                    new FileIOPermission(FileIOPermissionAccess.Write, filePath).Assert();
                }

                try
                {
                    File.Delete(filePath);
                }
                catch (System.IO.IOException)
                {
                    // DDVSO 227517: We may not be able to delete the file if it's being used by some other process (e.g. Anti-virus check).
                    // There's nothing we can do in that case, so just eat the exception and leave the file behind
                }
            }
        }
#pragma warning restore 56502
    }
}
