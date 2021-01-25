// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.IO;

namespace Microsoft.Extensions.Configuration.Test
{
    public class DisposableFileSystem : IDisposable
    {
        private const int _retries = 100;
        private const int _msDelay = 100;

        public DisposableFileSystem()
        {
            RootPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            CreateFolder("");
            DirectoryInfo = new DirectoryInfo(RootPath);
        }

        public string RootPath { get; }

        public DirectoryInfo DirectoryInfo { get; }

        public DisposableFileSystem CreateFolder(string path, bool absolute = false)
        {
            var fullPath = absolute
                ? path
                : Path.Combine(RootPath, path);

            Directory.CreateDirectory(fullPath);

            WaitForFileSystem(
                () => Directory.Exists(fullPath),
                $"Directory.CreateDirectory(\"{fullPath}\") failed");

            return this;
        }

        public DisposableFileSystem WriteFile(string path, string text = "temp", bool absolute = false)
        {
            var fullPath = absolute
                ? path
                : Path.Combine(RootPath, path);

            File.WriteAllText(fullPath, text);

            WaitForFileSystem(
                () => File.ReadAllText(fullPath).Length == text.Length,
                $"File.WriteAllText(\"{fullPath}\", \"{text}\") failed");

            return this;
        }

        public DisposableFileSystem DeleteFile(string path, bool absolute = false)
        {
            var fullPath = absolute
                ? path
                : Path.Combine(RootPath, path);

            WaitForFileSystem(
                () => !File.Exists(fullPath),
                $"File.Delete(\"{fullPath}\") failed",
                () => File.Delete(fullPath));

            return this;
        }

        public DisposableFileSystem CreateFiles(params string[] fileRelativePaths)
        {
            foreach (var path in fileRelativePaths)
            {
                var fullPath = Path.Combine(RootPath, path);
                var dirName = Path.GetDirectoryName(fullPath);
                Directory.CreateDirectory(dirName);

                WaitForFileSystem(
                    () => Directory.Exists(dirName),
                    $"Directory.CreateDirectory(\"{dirName}\") failed");

                WriteFile(
                    fullPath,
                    string.Format("Automatically generated for testing on {0:yyyy}/{0:MM}/{0:dd} {0:hh}:{0:mm}:{0:ss}", DateTime.UtcNow));
            }

            return this;
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(RootPath, true);
            }
            catch
            {
                // Don't throw if this fails.
            }
        }

        private void WaitForFileSystem(
            Func<bool> test,
            string failureMessage,
            Action retry = null)
        {
            Exception failure = null;

            Func<bool> nonThrowingTest = () =>
            {
                try
                {
                    failure = null;
                    retry?.Invoke();
                    return test();
                }
                catch (Exception exception)
                {
                    failure = exception;
                    return false;
                }
            };

            var i = 0;
            while (!nonThrowingTest())
            {
                if (++i >= _retries)
                {
                    if (failure != null)
                    {
                        failureMessage += $" (Exception: {failure.Message})";
                    }
                    throw new Exception(failureMessage);
                }

                Thread.Sleep(_msDelay);
            }
        }
    }
}
