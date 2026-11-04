// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Enumeration;
using Internal.JitInterface;

namespace SuperPmi.Mcs;

internal static class VerbMerge
{
    private const int BufferSize = 0x10000000;

    // Merges the files matching 'pattern' into one output file. The pattern is one of:
    //      1. *.mc -- assumes the current directory.
    //      2. foo/bar/*.mc -- with a relative directory.
    //      3. /foo/bar/baz/*.mc -- with a full path.
    // Without a wildcard, the last component is a directory name, and all its files are merged.
    // 'recursive' also searches all child directories. 'dedup' removes duplicates while merging,
    // and 'stripCR' also removes CompileResults.
    public static int DoWork(string nameOfOutputFile, string pattern, bool recursive, bool dedup, bool stripCR)
    {
        Logger.LogInfo($"Merging files matching '{pattern}' into '{nameOfOutputFile}'");

        RemoveDup removeDups = dedup ? new RemoveDup(stripCR, legacyCompare: false) : null;

        string dir;
        string file;
        int lastSlash = pattern.LastIndexOf(Path.DirectorySeparatorChar);
        if (lastSlash < 0)
        {
            // A pattern with a wildcard in the current directory, or else a directory name.
            if (pattern.Contains('*'))
            {
                file = pattern;
                dir = ".";
            }
            else
            {
                file = "*";
                dir = pattern;
            }
        }
        else if (pattern.IndexOf('*', lastSlash) < 0)
        {
            file = "*";
            dir = lastSlash == pattern.Length - 1 ? pattern.Substring(0, lastSlash) : pattern;
        }
        else
        {
            dir = pattern.Substring(0, lastSlash);
            file = pattern.Substring(lastSlash + 1);
        }

        int result;
        long totalSize = 0;
        using (FileStream output = FileHelpers.CreateOutput(nameOfOutputFile))
        {
            if (output == null)
                return -1;

            var timer = Stopwatch.StartNew();
            byte[] buffer = new byte[BufferSize];
            result = AppendAllInDir(output, dir, file, buffer, recursive, removeDups, ref totalSize);
            timer.Stop();

            if (result == 0)
                Logger.LogInfo(FileHelpers.Rate(totalSize, timer));
        }

        if (result != 0)
        {
            // Don't leave a half-created file behind.
            try
            {
                File.Delete(nameOfOutputFile);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                Logger.LogError($"Failed to delete file after MCS /merge failed. GetLastError()={ErrorHandling.LastError(e)}");
            }
        }
        else if (totalSize == 0)
        {
            Logger.LogError("No files found to merge.");
            result = 1;
        }

        return result;
    }

    private static int AppendAllInDir(FileStream output, string dir, string file, byte[] buffer, bool recursive, RemoveDup removeDups, ref long totalSize)
    {
        if (!FilterDirectory(dir, file, wantDirectories: false, out List<string> files))
            return -1;

        foreach (string name in files)
        {
            string fileFullPath = dir + Path.DirectorySeparatorChar + name;

            long fileSize;
            try
            {
                fileSize = new FileInfo(fileFullPath).Length;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                Logger.LogError($"Failed to stat file '{fileFullPath}'. errno={ErrorHandling.LastError(e)}");
                return -1;
            }

            if (fileSize == 0)
            {
                Logger.LogInfo($"Skipping zero-length file '{fileFullPath}'");
            }
            else
            {
                int result = AppendFile(output, fileFullPath, removeDups, buffer);
                if (result != 0)
                    return result;
            }

            totalSize += fileSize;
        }

        if (recursive)
        {
            if (!FilterDirectory(dir, "*", wantDirectories: true, out List<string> subDirs))
                return -1;

            foreach (string name in subDirs)
            {
                int result = AppendAllInDir(output, dir + Path.DirectorySeparatorChar + name, file, buffer, recursive, removeDups, ref totalSize);
                if (result != 0)
                    return result;
            }
        }

        return 0;
    }

    private static int AppendFile(FileStream output, string fileFullPath, RemoveDup removeDups, byte[] buffer)
    {
        Logger.LogInfo($"Appending file '{fileFullPath}'");

        if (removeDups != null)
        {
            if (!removeDups.CopyAndRemoveDups(fileFullPath, output))
            {
                Logger.LogError("Failed to remove dups");
                return -1;
            }
            return 0;
        }

        try
        {
            using var input = new FileStream(fileFullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1, FileOptions.SequentialScan);
            int read;
            while ((read = input.Read(buffer)) > 0)
                output.Write(buffer, 0, read);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Logger.LogError($"Failed to open input file '{fileFullPath}'. GetLastError()={ErrorHandling.LastError(e)}");
            return -1;
        }
        return 0;
    }

    // Lists the files (or directories) in 'dir' matching 'searchPattern', sorted by name.
    // A directory that doesn't exist just has no matches.
    private static bool FilterDirectory(string dir, string searchPattern, bool wantDirectories, out List<string> names)
    {
        names = new List<string>();

        if (!Directory.Exists(dir))
            return true;

        bool ignoreCase = OperatingSystem.IsWindows();
        try
        {
            var options = new EnumerationOptions { AttributesToSkip = 0, IgnoreInaccessible = false, RecurseSubdirectories = false };
            foreach (string path in Directory.EnumerateFileSystemEntries(dir, "*", options))
            {
                string name = Path.GetFileName(path);
                if (!FileSystemName.MatchesSimpleExpression(searchPattern, name, ignoreCase))
                    continue;

                FileAttributes attributes = File.GetAttributes(path);
                bool isDirectory = (attributes & FileAttributes.Directory) != 0;
                if (wantDirectories)
                {
                    // Skip links (possible loops), and on Windows system and hidden directories.
                    const FileAttributes skipped = FileAttributes.ReparsePoint | FileAttributes.System | FileAttributes.Hidden;
                    if (!isDirectory || (attributes & (OperatingSystem.IsWindows() ? skipped : FileAttributes.ReparsePoint)) != 0)
                        continue;
                }
                else if (isDirectory)
                {
                    continue;
                }

                names.Add(name);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Logger.LogError($"Failed to read directory. errno={ErrorHandling.LastError(e)}");
            return false;
        }

        names.Sort(string.CompareOrdinal);
        return true;
    }
}

// Removes duplicate method contexts while copying. Method contexts are bucketed by IL size, then
// compared by method hash (ignoring the method name), or with MethodContext.Equal for the legacy mode.
internal sealed unsafe class RemoveDup
{
    private readonly bool m_stripCR;
    private readonly bool m_legacyCompare;
    private readonly Dictionary<uint, HashSet<string>> m_inFile = new();
    private readonly Dictionary<uint, List<MethodContext>> m_inFileLegacy = new();

    public RemoveDup(bool stripCR, bool legacyCompare)
    {
        m_stripCR = stripCR;
        m_legacyCompare = legacyCompare;
    }

    private bool unique(MethodContext mc)
    {
        CORINFO_METHOD_INFO info = mc.repCompileMethod(out uint flags, out _);
        string hash = mc.dumpMethodHashToBuffer(ignoreMethodName: true, &info, flags);

        if (!m_inFile.TryGetValue(info.ILCodeSize, out HashSet<string> ourRank))
            m_inFile.Add(info.ILCodeSize, ourRank = new HashSet<string>(StringComparer.Ordinal));

        return ourRank.Add(hash);
    }

    private bool uniqueLegacy(MethodContext mc)
    {
        CORINFO_METHOD_INFO info = mc.repCompileMethod(out _, out _);

        if (!m_inFileLegacy.TryGetValue(info.ILCodeSize, out List<MethodContext> ourRank))
            m_inFileLegacy.Add(info.ILCodeSize, ourRank = new List<MethodContext>());

        foreach (MethodContext scratch in ourRank)
        {
            if (mc.Equal(scratch))
                return false;
        }

        ourRank.Add(mc);
        return true;
    }

    public bool CopyAndRemoveDups(string nameOfInput, Stream output)
    {
        using var mci = new MethodContextIterator(progressReport: true);
        if (!mci.Initialize(nameOfInput))
            return false;

        int savedCount = 0;
        while (mci.MoveNext())
        {
            MethodContext mc = mci.CurrentTakeOwnership();
            if (m_stripCR)
                mc.cr = new CompileResult();

            if (m_legacyCompare ? uniqueLegacy(mc) : unique(mc))
            {
                mc.saveToFile(output);
                savedCount++;
            }
        }

        Logger.LogInfo($"Loaded {mci.MethodContextNumber}, Saved {savedCount}");

        return mci.Destroy();
    }
}
