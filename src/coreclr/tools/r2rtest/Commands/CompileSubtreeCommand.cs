// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace R2RTest
{
    class CompileSubtreeCommand
    {
        public static int CompileSubtree(BuildOptions options)
        {
            if (options.InputDirectory == null)
            {
                Console.WriteLine("--input-directory is a required argument.");
                return 1;
            }

            if (options.CoreRootDirectory == null)
            {
                Console.Error.WriteLine("--core-root-directory (--cr) is a required argument.");
                return 1;
            }

            if (options.OutputDirectory == null)
            {
                options.OutputDirectory = options.InputDirectory;
            }

            if (options.OutputDirectory.IsParentOf(options.InputDirectory))
            {
                Console.WriteLine("Error: Input and output folders must be distinct, and the output directory (which gets deleted) better not be a parent of the input directory.");
                return 1;
            }

            IEnumerable<CompilerRunner> runners = options.CompilerRunners(isFramework: false);

            if (!options.Exe)
            {
                PathExtensions.DeleteOutputFolders(options.OutputDirectory.FullName, options.CoreRootDirectory.FullName, runners, recursive: true);
            }

            string[] directories = LocateSubtree(options.InputDirectory.FullName, options.CoreRootDirectory.FullName).ToArray();

            ConcurrentBag<BuildFolder> folders = new ConcurrentBag<BuildFolder>();
            int relativePathOffset = options.InputDirectory.FullName.Length;
            if (relativePathOffset > 0 && options.InputDirectory.FullName[relativePathOffset - 1] != Path.DirectorySeparatorChar)
            {
                relativePathOffset++;
            }

            int folderCount = 0;
            int compilationCount = 0;
            int executionCount = 0;
            Parallel.ForEach(directories, (string directory) =>
            {
                string outputDirectoryPerFolder = options.OutputDirectory.FullName;
                if (directory.Length > relativePathOffset)
                {
                    outputDirectoryPerFolder = Path.Combine(outputDirectoryPerFolder, directory.Substring(relativePathOffset));
                }
                try
                {
                    BuildFolder folder = BuildFolder.FromDirectory(directory.ToString(), runners, outputDirectoryPerFolder, options);
                    if (folder != null)
                    {
                        folders.Add(folder);
                        Interlocked.Add(ref compilationCount, folder.Compilations.Count);
                        Interlocked.Add(ref executionCount, folder.Executions.Count);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Error scanning folder {0}: {1}", directory, ex.Message);
                }
                int currentCount = Interlocked.Increment(ref folderCount);
                if (currentCount % 100 == 0)
                {
                    StringBuilder lineReport = new StringBuilder();
                    lineReport.Append($@"Found {folders.Count} folders to build ");
                    lineReport.Append($@"({compilationCount} compilations, ");
                    if (!options.NoExe)
                    {
                        lineReport.Append($@"{executionCount} executions, ");
                    }
                    lineReport.Append($@"{currentCount} / {directories.Length} folders scanned)");
                    Console.WriteLine(lineReport.ToString());
                }
            });
            Console.Write($@"Found {folders.Count} folders to build ({compilationCount} compilations, ");
            if (!options.NoExe)
            {
                Console.Write($@"{executionCount} executions, ");
            }
            Console.WriteLine($@"{directories.Length} folders scanned)");

            BuildFolderSet folderSet = new BuildFolderSet(folders, runners, options);
            bool success = folderSet.Build();
            folderSet.WriteLogs();

            if (!options.NoCleanup)
            {
                PathExtensions.DeleteOutputFolders(options.OutputDirectory.FullName, options.CoreRootDirectory.FullName, runners, recursive: true);
            }

            return success ? 0 : 1;
        }

        private static string[] LocateSubtree(string folder, string coreRootFolder)
        {
            ConcurrentBag<string> directories = new ConcurrentBag<string>();
            // TODO: this is somewhat hacky - should we introduce a new option -bt (artifacts/tests/OS.arch.config) we'd use
            // to derive the location of Core_Root and testhost?
            string testHostFolder = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(coreRootFolder)), "testhost");
            LocateSubtreeAsync(folder, coreRootFolder, testHostFolder, directories).Wait();
            return directories.ToArray();
        }

        private static async Task LocateSubtreeAsync(string folder, string coreRootFolder, string testHostFolder, ConcurrentBag<string> directories)
        {
            if (!Path.GetExtension(folder).Equals(".out", StringComparison.OrdinalIgnoreCase) &&
                !folder.Equals(coreRootFolder, StringComparison.OrdinalIgnoreCase) &&
                !folder.Equals(testHostFolder, StringComparison.OrdinalIgnoreCase))
            {
                directories.Add(folder);
                List<Task> subfolderTasks = new List<Task>();
                foreach (string subdir in Directory.EnumerateDirectories(folder))
                {
                    subfolderTasks.Add(Task.Run(() => LocateSubtreeAsync(subdir, coreRootFolder, testHostFolder, directories)));
                }
                await Task.WhenAll(subfolderTasks);
            }
        }
    }
}
