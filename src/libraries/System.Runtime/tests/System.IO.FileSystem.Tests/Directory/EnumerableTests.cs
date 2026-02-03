// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class EnumerableTests : FileSystemTest
    {
        [Fact]
        // we don't guarantee thread safety of enumerators in general, but on Windows we
        // currently are thread safe, and this test will help ensure that if we change that
        // it's a conscious decision. Discussed in https://github.com/dotnet/runtime/issues/24295.
        [PlatformSpecific(TestPlatforms.Windows)]
        public void FileEnumeratorIsThreadSafe()
        {
            string directory = Directory.CreateDirectory(GetTestFilePath()).FullName;
            for (int i = 0; i < 100; i++)
                File.Create(Path.Combine(directory, GetTestFileName())).Dispose();

            // We are really only trying to make sure we don't terminate the process.
            // Throwing IOException at this point to try and flush out other problems
            // like bad handles. Can narrow the throw if this isn't reliable.

            try
            {
                new ThreadSafeRepro().Execute(directory);
            }
            catch (Exception e) when (!(e is IOException))
            {
            }
        }

        [Fact]
        public void FileEnumeratorIsThreadSafe_ParallelForEach()
        {
            List<string> expected = [];
            string directory = Directory.CreateDirectory(GetTestFilePath()).FullName;
            for (int i = 0; i < 100; i++)
            {
                string file = Path.Join(directory, GetTestFileName());
                File.Create(file).Dispose();
                expected.Add(file);
            }

            for (int i = 0; i < 100; i++) // test multiple times to ensure thread safety.
            {
                ConcurrentBag<string> result = [];
                ParallelLoopResult parallelResult = Parallel.ForEach(Directory.EnumerateFiles(directory), f => result.Add(f));
                AssertExtensions.CollectionEqual(expected, result, StringComparer.Ordinal);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void CreateSubdirectory_RootDriveSubfolder_Windows()
        {
            // Get the root of the OS drive (e.g., "C:\")
            string rootDrive = Path.GetPathRoot(Environment.SystemDirectory);

            // Create a DirectoryInfo for the root drive
            DirectoryInfo rootDirectory = new DirectoryInfo(rootDrive);

            // Create a unique test folder name to avoid conflicts
            string testFolderName = GetTestFileName();

            try
            {
                // Create a subdirectory directly in the root
                DirectoryInfo subDirectory = rootDirectory.CreateSubdirectory(testFolderName);

                // Verify it was created
                Assert.True(subDirectory.Exists);
                Assert.Equal(Path.Combine(rootDrive, testFolderName), subDirectory.FullName);
            }
            finally
            {
                // Cleanup
                string testFolderPath = Path.Combine(rootDrive, testFolderName);
                if (Directory.Exists(testFolderPath))
                {
                    Directory.Delete(testFolderPath, recursive: true);
                }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Linux)]
        public void CreateSubdirectory_RootDriveSubfolder_ThrowsUnauthorizedAccessException_Linux()
        {
            string rootDrive = "/";

            DirectoryInfo rootDirectory = new DirectoryInfo(rootDrive);
            string testFolderName = $"TestFolder_{Guid.NewGuid():N}";

            // Expect permission failure when trying to create directly under "/", in test envs access to / is denied,
            //but at least we know directory craeting attempt was done
            Assert.Throws<UnauthorizedAccessException>(() =>
            {
                rootDirectory.CreateSubdirectory(testFolderName);
            });
        }

        [Fact]
        public void EnumerateDirectories_NonBreakingSpace()
        {
            DirectoryInfo rootDirectory = Directory.CreateDirectory(GetTestFilePath());
            DirectoryInfo subDirectory1 = rootDirectory.CreateSubdirectory("\u00A0");
            DirectoryInfo subDirectory2 = subDirectory1.CreateSubdirectory(GetTestFileName());

            FSAssert.EqualWhenOrdered(new string[] { subDirectory1.FullName, subDirectory2.FullName }, Directory.EnumerateDirectories(rootDirectory.FullName, string.Empty, SearchOption.AllDirectories));
        }

         [Fact]
         [PlatformSpecific(TestPlatforms.Windows)]
         public void EnumerateDirectories_TrailingDot()
         {
             string prefix = @"\\?\";
             string tempPath = GetTestFilePath();
             string fileName = "Test.txt";

             string[] dirPaths = {
                 Path.Join(prefix, tempPath, "Test"),
                 Path.Join(prefix, tempPath, "TestDot."),
                 Path.Join(prefix, tempPath, "TestDotDot..")
             };

             // Create directories and their files using "\\?\C:\" paths
             foreach (string dirPath in dirPaths)
             {
                 if (Directory.Exists(dirPath))
                 {
                     Directory.Delete(dirPath, recursive: true);
                 }

                 Directory.CreateDirectory(dirPath);

                 // Directory.Exists should work with directories containing trailing dots and prefixed with \\?\
                 Assert.True(Directory.Exists(dirPath));

                 string filePath = Path.Join(dirPath, fileName);
                 using FileStream fs = File.Create(filePath);

                 // File.Exists should work with directories containing trailing dots and prefixed with \\?\
                 Assert.True(File.Exists(filePath));
             }

             try
             {
                 // Enumerate directories and their files using "C:\" paths
                 DirectoryInfo sourceInfo = new DirectoryInfo(tempPath);
                 foreach (DirectoryInfo dirInfo in sourceInfo.EnumerateDirectories("*", SearchOption.AllDirectories))
                 {
                     // DirectoryInfo.Exists should work with or without \\?\ for folders with trailing dots
                     Assert.True(dirInfo.Exists);

                     if (dirInfo.FullName.EndsWith("."))
                     {
                         // Directory.Exists is not expected to work with directories containing trailing dots and not prefixed with \\?\
                         Assert.False(Directory.Exists(dirInfo.FullName));
                     }

                     foreach (FileInfo fileInfo in dirInfo.EnumerateFiles("*.*", SearchOption.TopDirectoryOnly))
                     {
                         // FileInfo.Exists should work with or without \\?\ for folders with trailing dots
                         Assert.True(fileInfo.Exists);

                         if (fileInfo.Directory.FullName.EndsWith("."))
                         {
                             // File.Exists is not expected to work with directories containing trailing dots and not prefixed with \\?\
                             Assert.False(File.Exists(fileInfo.FullName));
                         }
                     }
                 }
             }
             finally
             {
                 foreach (string dirPath in dirPaths)
                 {
                     Directory.Delete(dirPath, recursive: true);
                 }
             }
         }

        class ThreadSafeRepro
        {
            volatile IEnumerator<string> _enumerator;

            void Enumerate(IEnumerator<string> s)
            {
                while (s.MoveNext())
                { }
                s.Dispose();
            }

            public void Execute(string directory)
            {
                CancellationTokenSource source = new CancellationTokenSource();
                CancellationToken token = source.Token;

                void Work()
                {
                    do
                    {
                        IEnumerator<string> x = _enumerator;
                        if (x != null)
                            Enumerate(x);
                    } while (!token.IsCancellationRequested);
                }

                Task taskOne = Task.Run(action: Work);
                Task taskTwo = Task.Run(action: Work);

                try
                {
                    for (int i = 0; i < 1000; i++)
                    {
                        _enumerator = Directory.EnumerateFiles(directory).GetEnumerator();
                        Enumerate(_enumerator);
                    }
                }
                finally
                {
                    source.Cancel();
                    Task.WaitAll(taskOne, taskTwo);
                }
            }
        }
    }
}
