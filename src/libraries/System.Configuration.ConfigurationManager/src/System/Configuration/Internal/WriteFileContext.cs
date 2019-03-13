// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;

// The CODEDOM check is here to support frameworks that may not have fully 
// incorporated all of corefx, but want to use System.Configuration.ConfigurationManager. 
// TempFileCollection was moved in corefx. 
#if CODEDOM
using System.CodeDom.Compiler;
#else
using System.IO.Internal;
#endif

namespace System.Configuration.Internal
{
    internal class WriteFileContext
    {
        private const int SavingTimeout = 10000;        // 10 seconds
        private const int SavingRetryInterval = 100;    // 100 milliseconds
        private readonly string _templateFilename;

        private TempFileCollection _tempFiles;

        internal WriteFileContext(string filename, string templateFilename)
        {
            string directoryname = UrlPath.GetDirectoryOrRootName(filename);

            _templateFilename = templateFilename;
            _tempFiles = new TempFileCollection(directoryname);
            try
            {
                TempNewFilename = _tempFiles.AddExtension("newcfg");
            }
            catch
            {
                ((IDisposable)_tempFiles).Dispose();
                _tempFiles = null;
                throw;
            }
        }

        internal string TempNewFilename { get; }

        // Cleanup the WriteFileContext object based on either success
        // or failure
        //
        // Note: The current algorithm guarantess
        //         1) The file we are saving to will always be present 
        //            on the file system (ie. there will be no window
        //            during saving in which there won't be a file there)
        //         2) It will always be available for reading from a 
        //            client and it will be complete and accurate.
        //
        // ... This means that writing is a bit more complicated, and may
        // have to be retried (because of reading lock), but I don't see 
        // anyway to get around this given 1 and 2.
        internal void Complete(string filename, bool success)
        {
            try
            {
                if (!success) return;

                if (File.Exists(filename))
                {
                    // Test that we can write to the file
                    ValidateWriteAccess(filename);

                    // Copy Attributes from original
                    DuplicateFileAttributes(filename, TempNewFilename);
                }
                else
                {
                    if (_templateFilename != null)
                    {
                        // Copy Acl from template file
                        DuplicateTemplateAttributes(_templateFilename, TempNewFilename);
                    }
                }

                ReplaceFile(TempNewFilename, filename);

                // Don't delete, since we just moved it.
                _tempFiles.KeepFiles = true;
            }
            finally
            {
                ((IDisposable)_tempFiles).Dispose();
                _tempFiles = null;
            }
        }

        // Copy all the files attributes that we care about from the source
        // file to the destination file
        private void DuplicateFileAttributes(string source, string destination)
        {
            DateTime creationTime;

            // Copy File Attributes, ie. Hidden, Readonly, etc.
            FileAttributes attributes = File.GetAttributes(source);
            File.SetAttributes(destination, attributes);

            // Copy Creation Time
            creationTime = File.GetCreationTimeUtc(source);
            File.SetCreationTimeUtc(destination, creationTime);

            // Copy ACL's
            DuplicateTemplateAttributes(source, destination);
        }

        // Copy over all the attributes you would want copied from a template file.
        // As of right now this is just acl's
        private void DuplicateTemplateAttributes(string source, string destination)
        {
            FileAttributes fileAttributes = File.GetAttributes(source);
            File.SetAttributes(destination, fileAttributes);
        }

        // Validate that we can write to the file.  This will enforce the ACL's
        // on the file.  Since we do our moving of files to replace, this is 
        // nice to ensure we are not by-passing some security permission
        // that someone set (although that could bypass this via move themselves)
        //
        // Note: 1) This is really just a nice to have, since with directory permissions
        //          they could do the same thing we are doing
        //
        //       2) We are depending on the current behavior that if the file is locked 
        //          and we can not open it, that we will get an UnauthorizedAccessException
        //          and not the IOException.
        private void ValidateWriteAccess(string filename)
        {
            FileStream fs = null;

            try
            {
                // Try to open file for write access
                fs = new FileStream(filename,
                    FileMode.Open,
                    FileAccess.Write,
                    FileShare.ReadWrite);
            }
            catch (IOException)
            {
                // Someone else was using the file.  Since we did not get
                // the unauthorizedAccessException we have access to the file
            }
            finally
            {
                fs?.Close();
            }
        }

        /// <summary>
        /// Replace one file with another, retrying if locked.
        /// </summary>
        private void ReplaceFile(string source, string target)
        {
            bool writeSucceeded;
            int duration = 0;

            writeSucceeded = AttemptMove(source, target);

            // The file may be open for read, if it is then 
            // lets try again because maybe they will finish
            // soon, and we will be able to replace
            while (!writeSucceeded &&
                (duration < SavingTimeout) &&
                File.Exists(target) &&
                !FileIsWriteLocked(target))
            {
                Thread.Sleep(SavingRetryInterval);
                duration += SavingRetryInterval;
                writeSucceeded = AttemptMove(source, target);
            }

            if (!writeSucceeded)
            {
                throw new ConfigurationErrorsException(SR.Format(SR.Config_write_failed, target));
            }
        }

        // Attempt to move a file from one location to another, overwriting if needed
        private bool AttemptMove(string source, string target)
        {
            try
            {
                if (File.Exists(target))
                {
                    File.Replace(source, target, null);
                }
                else
                {
                    File.Move(source, target);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool FileIsWriteLocked(string fileName)
        {
            Stream fileStream = null;

            if (!File.Exists(fileName))
            {
                // It can't be locked if it doesn't exist
                return false;
            }

            try
            {
                // Try to open for shared reading
                fileStream = new FileStream(fileName,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read | FileShare.Delete);

                // If we can open it for shared reading, it is not write locked
                return false;
            }
            catch
            {
                return true;
            }
            finally
            {
                fileStream?.Close();
            }
        }
    }
}
