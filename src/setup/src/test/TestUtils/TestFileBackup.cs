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
                throw new Exception($"The backup directory already exists. Please make sure that all customizers are correctly disposed.");
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

                Directory.Delete(_backupPath, recursive: true);
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
