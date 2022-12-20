// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using R2RTest;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// A set of helper to manipulate paths into a canonicalized form to ensure user-provided paths
/// match those in the ETW log.
/// </summary>
static class PathExtensions
{
    /// <summary>
    /// Millisecond timeout for file / directory deletion.
    /// </summary>
    const int DeletionTimeoutMilliseconds = 10000;

    /// <summary>
    /// Back-off for repeated checks for directory deletion. According to my local experience [trylek],
    /// when the directory is opened in the file explorer, the propagation typically takes 2 seconds.
    /// </summary>
    const int DirectoryDeletionBackoffMilliseconds = 500;

    internal static string AppendOSExeSuffix(this string path) => (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? path + ".exe" : path);

    internal static string AppendOSDllSuffix(this string path) => path +
        (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".dll" : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? ".dylib" : ".so");

    internal static string ToAbsolutePath(this string argValue) => Path.GetFullPath(argValue);

    internal static string ToAbsoluteDirectoryPath(this string argValue) => argValue.ToAbsolutePath().StripTrailingDirectorySeparators();

    internal static string StripTrailingDirectorySeparators(this string str)
    {
        if (String.IsNullOrWhiteSpace(str))
        {
            return str;
        }

        while (str.Length > 0 && str[str.Length - 1] == Path.DirectorySeparatorChar)
        {
            str = str.Remove(str.Length - 1);
        }

        return str;
    }

    internal static string ConcatenatePaths(this IEnumerable<string> paths)
    {
        return string.Join(Path.PathSeparator, paths);
    }

    // TODO: this assumes we're running tests from the root
    internal static string DotNetAppPath => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet" : "Tools/dotnetcli/dotnet";

    internal static void RecreateDirectory(this string path)
    {
        if (Directory.Exists(path))
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            Task<bool> deleteSubtreeTask = path.DeleteSubtree();
            deleteSubtreeTask.Wait();
            if (deleteSubtreeTask.Result)
            {
                Console.WriteLine("Deleted {0} in {1} msecs", path, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                throw new Exception($"Error: Could not delete output folder {path}");
            }
        }

        Directory.CreateDirectory(path);
    }

    internal static bool IsParentOf(this DirectoryInfo outputPath, DirectoryInfo inputPath)
    {
        DirectoryInfo parentInfo = inputPath.Parent;
        while (parentInfo != null)
        {
            if (parentInfo == outputPath)
                return true;

            parentInfo = parentInfo.Parent;
        }

        return false;
    }

    public static string FindFile(this string fileName, IEnumerable<string> paths)
    {
        foreach (string path in paths)
        {
            string fileOnPath = Path.Combine(path, fileName);
            if (File.Exists(fileOnPath))
            {
                return fileOnPath;
            }
        }
        return null;
    }

    /// <summary>
    /// Parallel deletion of multiple disjunct subtrees.
    /// </summary>
    /// <param name="path">List of directories to delete</param>
    /// <returns>Task returning true on success, false on failure</returns>
    public static bool DeleteSubtrees(this string[] paths)
    {
        return DeleteSubtreesAsync(paths).Result;
    }

    private static async Task<bool> DeleteSubtreesAsync(this string[] paths)
    {
        bool succeeded = true;

        var tasks = new List<Task<bool>>();
        foreach (string path in paths)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    // Non-existent folders are harmless w.r.t. deletion
                    Console.WriteLine("Skipping non-existent folder: '{0}'", path);
                }
                else
                {
                    Console.WriteLine("Deleting '{0}'", path);
                    tasks.Add(path.DeleteSubtree());
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error deleting '{0}': {1}", path, ex.Message);
                succeeded = false;
            }
        }

        await Task<bool>.WhenAll(tasks);

        foreach (var task in tasks)
        {
            if (!task.Result)
            {
                succeeded = false;
                break;
            }
        }
        return succeeded;
    }

    private static async Task<bool> DeleteSubtree(this string folder)
    {
        Task<bool>[] subtasks = new []
        {
            DeleteSubtreesAsync(Directory.GetDirectories(folder)),
            DeleteFiles(Directory.GetFiles(folder))
        };

        await Task<bool>.WhenAll(subtasks);
        bool succeeded = subtasks.All(subtask => subtask.Result);

        if (succeeded)
        {
            Stopwatch folderDeletion = new Stopwatch();
            folderDeletion.Start();
            while (Directory.Exists(folder))
            {
                try
                {
                    Directory.Delete(folder, recursive: false);
                }
                catch (DirectoryNotFoundException)
                {
                    // Directory not found is OK (the directory might have been deleted during the back-off delay).
                }
                catch (Exception)
                {
                    Console.WriteLine("Folder deletion failure, maybe transient ({0} msecs): '{1}'", folderDeletion.ElapsedMilliseconds, folder);
                }

                if (!Directory.Exists(folder))
                {
                    break;
                }

                if (folderDeletion.ElapsedMilliseconds > DeletionTimeoutMilliseconds)
                {
                    Console.Error.WriteLine("Timed out trying to delete directory '{0}'", folder);
                    succeeded = false;
                    break;
                }

                Thread.Sleep(DirectoryDeletionBackoffMilliseconds);
            }
        }

        return succeeded;
    }

    private static async Task<bool> DeleteFiles(string[] files)
    {
        Task<bool>[] tasks = new Task<bool>[files.Length];
        for (int i = 0; i < files.Length; i++)
        {
            int temp = i;
            tasks[i] = Task<bool>.Run(() => files[temp].DeleteFile());
        }
        await Task<bool>.WhenAll(tasks);
        return tasks.All(task => task.Result);
    }

    private static bool DeleteFile(this string file)
    {
        try
        {
            File.Delete(file);
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"{file}: {ex.Message}");
            return false;
        }
    }

    public static string[] LocateOutputFolders(string folder, string coreRootFolder, IEnumerable<CompilerRunner> runners, bool recursive)
    {
        ConcurrentBag<string> directories = new ConcurrentBag<string>();
        LocateOutputFoldersAsync(folder, coreRootFolder, runners, recursive, directories).Wait();
        return directories.ToArray();
    }

    private static async Task LocateOutputFoldersAsync(string folder, string coreRootFolder, IEnumerable<CompilerRunner> runners, bool recursive, ConcurrentBag<string> directories)
    {
        if (coreRootFolder == null || !StringComparer.OrdinalIgnoreCase.Equals(folder, coreRootFolder))
        {
            List<Task> subfolderTasks = new List<Task>();
            foreach (string dir in Directory.EnumerateDirectories(folder))
            {
                if (Path.GetExtension(dir).Equals(".out", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (CompilerRunner runner in runners)
                    {
                        if (runner.GetOutputPath(folder) == dir)
                        {
                            directories.Add(dir);
                        }
                    }
                }
                else if (recursive)
                {
                    subfolderTasks.Add(Task.Run(() => LocateOutputFoldersAsync(dir, coreRootFolder, runners, recursive, directories)));
                }
            }
            await Task.WhenAll(subfolderTasks);
        }
    }

    public static bool DeleteOutputFolders(string folder, string coreRootFolder, IEnumerable<CompilerRunner> runners, bool recursive)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        Console.WriteLine("Locating output {0} {1}", (recursive ? "subtree" : "folder"), folder);
        string[] outputFolders = LocateOutputFolders(folder, coreRootFolder, runners, recursive);
        Console.WriteLine("Deleting {0} output folders", outputFolders.Length);

        if (DeleteSubtrees(outputFolders))
        {
            Console.WriteLine("Successfully deleted {0} output folders in {1} msecs", outputFolders.Length, stopwatch.ElapsedMilliseconds);
            return true;
        }
        else
        {
            Console.Error.WriteLine("Failed deleting {0} output folders in {1} msecs", outputFolders.Length, stopwatch.ElapsedMilliseconds);
            return false;
        }
    }

    public static StringComparer OSPathCaseComparer => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
}
