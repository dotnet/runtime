// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public interface ITestFileBackup
    {
        void Backup(string path);
    }

    public class TestFileBackup : ITestFileBackup, IDisposable
    {
        private readonly string _basePath;
        private readonly string _backupPath;

        public TestFileBackup(string basePath, string backupName = "test")
        {
            _basePath = Path.GetFullPath(basePath);
            _backupPath = Path.Combine(_basePath, $".{backupName}.backup");

            if (Directory.Exists(_backupPath))
            {
                string existingBackupStack = "";
                try
                {
                    existingBackupStack = File.ReadAllText(Path.Combine(_backupPath, "_stackTrace.txt"));
                }
                catch (Exception)
                {

                }

                throw new Exception(
                    $"The backup directory `{_backupPath}` already exists. Please make sure that all customizers are correctly disposed.\r\n" +
                    $"The existing backup directory was created with this stack {existingBackupStack}");
            }
        }

        public void Backup(string path)
        {
            path = Path.GetFullPath(path);
            if (!path.StartsWith(_basePath))
            {
                throw new Exception($"Trying to backup file {path} which is outside of the backup root {_basePath}.");
            }

            if (!Directory.Exists(_backupPath))
            {
                Directory.CreateDirectory(_backupPath);
                File.WriteAllText(Path.Combine(_backupPath, "_stackTrace.txt"), Environment.StackTrace);
            }

            string backupFile = Path.Combine(_backupPath, path.Substring(_basePath.Length + 1));
            string containingDirectory = Path.GetDirectoryName(backupFile);
            if (!Directory.Exists(containingDirectory))
            {
                Directory.CreateDirectory(containingDirectory);
            }

            if (!File.Exists(backupFile))
            {
                File.Copy(path, backupFile);
            }
        }

        public void Dispose()
        {
            if (Directory.Exists(_backupPath))
            {
                CopyOverDirectory(_backupPath, _basePath);

                // Directory.Delete sometimes fails with error that the directory is not empty.
                // This is a known problem where the actual Delete call is not 100% synchronous
                // the OS reports a success but the file/folder is not fully removed yet.
                // So implement a simple retry with a short timeout.
                IOException exception = null;
                for (int retryCount = 5; retryCount > 0; retryCount--)
                {
                    try
                    {
                        Directory.Delete(_backupPath, recursive: true);
                        if (!Directory.Exists(_backupPath))
                        {
                            return;
                        }
                    }
                    catch (IOException ex)
                    {
                        exception = ex;
                    }

                    System.Threading.Thread.Sleep(200);
                }
                
                throw new Exception(
                    $"Failed to delete the backup folder {_backupPath} even after retries.\r\n"
                    + (exception == null ? "" : exception.ToString()));
            }
        }

        private static void CopyOverDirectory(string source, string destination)
        {
            foreach (string directory in Directory.GetDirectories(source))
            {
                CopyOverDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
            }

            foreach (string file in Directory.GetFiles(source))
            {
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
            }
        }
    }
}
