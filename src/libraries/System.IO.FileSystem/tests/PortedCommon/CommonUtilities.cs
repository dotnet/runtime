// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**
This is meant to contain useful utilities for IO related work
**/

#define TRACE
#define DEBUG
using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

//machine information
public static class FileSystemDebugInfo
{
    public static string MachineInfo()
    {
        StringBuilder builder = new StringBuilder(string.Format("{0}/////////Machine Info///////////{0}", Environment.NewLine));
        builder.AppendLine(string.Format("CurrentDrive NTFS?: {0}", IsCurrentDriveNTFS()));
        builder.AppendLine(string.Format("////////////////////{0}", Environment.NewLine));

        return builder.ToString();
    }

    public static bool IsCurrentDriveNTFS()
    {
        return IOServices.IsDriveNTFS(IOServices.GetCurrentDrive());
    }

    public static bool IsPathAdminAccessOnly(string path, bool treatPathAsFilePath)
    {
        //we leave invalid paths as valid testcase scenarios and don't filter these
        //1) We check if the path is root on a system drive
        //2) @TODO WinDir?
        string systemDrive = Environment.GetEnvironmentVariable("SystemDrive");
        char systemDriveLetter = systemDrive.ToLower()[0];
        try
        {
            string dirName = Path.GetFullPath(path);
            if (treatPathAsFilePath)
                dirName = Path.GetDirectoryName(dirName);
            if ((new DirectoryInfo(dirName)).Parent == null)
            {
                if (Path.GetPathRoot(dirName).StartsWith(systemDriveLetter.ToString(), StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        catch (Exception) { }
        return false;
    }
}

/// <summary>
/// Due to the increasing number of context indexing services (mssearch.exe, etrust) operating in our test run machines, Directory operations like Delete and Move
/// are not guaranteed to work in first attempt. This utility class do these operations in a fail safe manner
/// Possible solutions
///  - Set FileAttributes.NotContentIndex on the directory. But there is a race between creating the directory and setting this property. Other than using ACL, can't see a good solution
///  - encourage labs to stop these services before a test run. This is under review by CLRLab but there are lots of other labs that do these too
///  - fail and retry attempt: which is what this class does
/// VSW 446086 and 473287 have more information on this.
/// </summary>
public static class FailSafeDirectoryOperations
{
    /// <summary>
    /// Deleting
    /// </summary>
    /// <param name="path"></param>
    /// <param name="recursive"></param>
    private const int MAX_ATTEMPT = 10;
    public static void DeleteDirectory(string path, bool recursive)
    {
        DeleteDirectoryInfo(new DirectoryInfo(path), recursive);
    }

    public static void DeleteDirectoryInfo(DirectoryInfo dirInfo, bool recursive)
    {
        int dirModificationAttemptCount;
        bool dirModificationOperationThrew;
        dirModificationAttemptCount = 0;
        do
        {
            dirModificationOperationThrew = false;
            try
            {
                if (dirInfo.Exists)
                    dirInfo.Delete(recursive);
            }
            catch (IOException)
            {
                Console.Write("|");
                dirModificationOperationThrew = true;
            }
            catch (UnauthorizedAccessException)
            {
                Console.Write("}");
                dirModificationOperationThrew = true;
            }
            if (dirModificationOperationThrew)
            {
                Task.Delay(5000).Wait();
            }
        } while (dirModificationOperationThrew && dirModificationAttemptCount++ < MAX_ATTEMPT);
        EnsureDirectoryNotExist(dirInfo.FullName);
        //We want to thrown if the operation failed
        if (Directory.Exists(dirInfo.FullName))
            throw new ArgumentException("Throwing from FailSafeDirectoryOperations.DeleteDirectoryInfo. Delete unsuccessful");
    }


    /// <summary>
    /// Moving
    /// </summary>
    /// <param name="sourceName"></param>
    /// <param name="destName"></param>
    public static void MoveDirectory(string sourceName, string destName)
    {
        MoveDirectoryInfo(new DirectoryInfo(sourceName), destName);
    }

    public static DirectoryInfo MoveDirectoryInfo(DirectoryInfo dirInfo, string dirToMove)
    {
        int dirModificationAttemptCount;
        bool dirModificationOperationThrew;
        dirModificationAttemptCount = 0;
        string originalDirName = dirInfo.FullName;
        do
        {
            dirModificationOperationThrew = false;
            try
            {
                dirInfo.MoveTo(dirToMove);
            }
            catch (IOException)
            {
                Console.Write(">");
                Task.Delay(5000).Wait();
                dirModificationOperationThrew = true;
            }
        } while (dirModificationOperationThrew && dirModificationAttemptCount++ < MAX_ATTEMPT);
        EnsureDirectoryNotExist(originalDirName);
        //We want to thrown if the operation failed
        if (Directory.Exists(originalDirName))
            throw new ArgumentException("Throwing from FailSafeDirectoryOperations.MoveDirectory. Move unsuccessful");
        return dirInfo;
    }

    /// <summary>
    /// It can take some time before the Directory.Exists will return false after a directory delete/Move
    /// </summary>
    /// <param name="path"></param>
    private static void EnsureDirectoryNotExist(string path)
    {
        int dirModificationAttemptCount;
        dirModificationAttemptCount = 0;
        while (Directory.Exists(path) && dirModificationAttemptCount++ < MAX_ATTEMPT)
        {
            // This is because something like antivirus software or
            // some disk indexing service has a handle to the directory.  The directory
            // won't be deleted until all of the handles are closed.
            Task.Delay(5000).Wait();
            Console.Write("<");
        }
    }
}



/// <summary>
/// This class is meant to create directory and files under it
/// </summary>
public class ManageFileSystem : IDisposable
{
    private const int DefaultDirectoryDepth = 3;
    private const int DefaultNumberofFiles = 100;
    private const int MaxNumberOfSubDirsPerDir = 2;
    //@TODO
    public const string DirPrefixName = "Laks_";

    private int _directoryLevel;
    private int _numberOfFiles;
    private string _startDir;

    private Random _random;

    private List<string> _listOfFiles;
    private List<string> _listOfAllDirs;
    private Dictionary<int, Dictionary<string, List<string>>> _allDirs;


    public ManageFileSystem()
    {
        Init(GetNonExistingDir(Directory.GetCurrentDirectory(), DirPrefixName), DefaultDirectoryDepth, DefaultNumberofFiles);
    }
    public ManageFileSystem(string startDirName)
        : this(startDirName, DefaultDirectoryDepth, DefaultNumberofFiles)
    {
    }
    public ManageFileSystem(string startDirName, int dirDepth, int numFiles)
    {
        Init(startDirName, dirDepth, numFiles);
    }

    public static string GetNonExistingDir(string parentDir, string prefix)
    {
        string tempPath;
        while (true)
        {
            tempPath = Path.Combine(parentDir, string.Format("{0}{1}", prefix, Path.GetFileNameWithoutExtension(Path.GetRandomFileName())));
            if (!Directory.Exists(tempPath) && !File.Exists(tempPath))
                break;
        }
        return tempPath;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // free other state (managed objects)
            // set interesting (large) fields to null
        }
        // free your own state (unmanaged objects)
        FailSafeDirectoryOperations.DeleteDirectory(_startDir, true);
    }

    ~ManageFileSystem()
    {
        Dispose(false);
    }

    private void Init(string startDirName, int dirDepth, int numFiles)
    {
        if (Directory.Exists(Path.GetFullPath(startDirName)))
            throw new ArgumentException(string.Format("ERROR: Directory exists : {0}", _startDir));
        _startDir = Path.GetFullPath(startDirName);
        _directoryLevel = dirDepth;
        _numberOfFiles = numFiles;
        _random = new Random(-55);
        CreateFileSystem();
    }

    /// <summary>
    /// This API creates a file system under m_startDir, m_DirectoryLevel deep with m_numberOfFiles
    /// We will store the created information on collections so as to get to them later
    /// </summary>
    private void CreateFileSystem()
    {
        _listOfFiles = new List<string>();
        _listOfAllDirs = new List<string>();
        Directory.CreateDirectory(_startDir);
        //we will not include this directory
        //        m_listOfAllDirs.Add(m_startDir);

        _allDirs = new Dictionary<int, Dictionary<string, List<string>>>();
        //        List<String> dirsForOneLevel = new List<String>();
        //        List<String> tempDirsForOneLevel;
        List<string> filesForThisDir;
        Dictionary<string, List<string>> dirsForOneLevel = new Dictionary<string, List<string>>();
        Dictionary<string, List<string>> tempDirsForOneLevel;
        dirsForOneLevel.Add(_startDir, new List<string>());
        _allDirs.Add(0, dirsForOneLevel);

        //First we create the directories
        for (int i = 0; i < (_directoryLevel - 1); i++)
        {
            dirsForOneLevel = _allDirs[i];
            int numOfDirForThisLevel = _random.Next((MaxNumberOfSubDirsPerDir + 1));
            int numOfDirPerDir = numOfDirForThisLevel / dirsForOneLevel.Count;
            //@TODO!! we should handle this situation in a better way
            if (numOfDirPerDir == 0)
                numOfDirPerDir = 1;
            //            Trace.Assert(numOfDirPerDir > 0, "Err_897324g! @TODO handle this scenario");
            tempDirsForOneLevel = new Dictionary<string, List<string>>();
            foreach (string dir in dirsForOneLevel.Keys)
            //                for (int j = 0; j < dirsForOneLevel.Count; j++)
            {
                for (int k = 0; k < numOfDirPerDir; k++)
                {
                    string dirName = GetNonExistingDir(dir, DirPrefixName);
                    Debug.Assert(!Directory.Exists(dirName), string.Format("ERR_93472g! Directory exists: {0}", dirName));
                    tempDirsForOneLevel.Add(dirName, new List<string>());
                    _listOfAllDirs.Add(dirName);
                    Directory.CreateDirectory(dirName);
                }
            }
            _allDirs.Add(i + 1, tempDirsForOneLevel);
        }
        //Then we add the files
        //@TODO!! random or fixed?
        int numberOfFilePerDirLevel = _numberOfFiles / _directoryLevel;
        byte[] bits;
        for (int i = 0; i < _directoryLevel; i++)
        {
            dirsForOneLevel = _allDirs[i];
            int numOfFilesForThisLevel = _random.Next(numberOfFilePerDirLevel + 1);
            int numOFilesPerDir = numOfFilesForThisLevel / dirsForOneLevel.Count;
            //UPDATE: 2/1/2005, we will add at least 1
            if (numOFilesPerDir == 0)
                numOFilesPerDir = 1;
            //            for (int j = 0; j < dirsForOneLevel.Count; j++)
            foreach (string dir in dirsForOneLevel.Keys)
            {
                filesForThisDir = dirsForOneLevel[dir];
                for (int k = 0; k < numOFilesPerDir; k++)
                {
                    string fileName = Path.Combine(dir, Path.GetFileName(Path.GetRandomFileName()));
                    bits = new byte[_random.Next(10)];
                    _random.NextBytes(bits);
                    File.WriteAllBytes(fileName, bits);
                    _listOfFiles.Add(fileName);
                    filesForThisDir.Add(fileName);
                }
            }
        }
    }

    public string StartDirectory
    {
        get { return _startDir; }
    }

    //some methods to help us
    public string[] GetDirectories(int level)
    {
        Dictionary<string, List<string>> dirsForThisLevel = null;
        if (_allDirs.TryGetValue(level, out dirsForThisLevel))
        {
            //        Dictionary<String, List<String>> dirsForThisLevel = m_allDirs[level];
            ICollection<string> keys = dirsForThisLevel.Keys;
            string[] values = new string[keys.Count];
            keys.CopyTo(values, 0);
            return values;
        }
        else
            return new string[0];
    }

    /// <summary>
    /// Note that this doesn't return the m_startDir
    /// </summary>
    /// <returns></returns>
    public string[] GetAllDirectories()
    {
        return _listOfAllDirs.ToArray();
    }


    public string[] GetFiles(string dirName, int level)
    {
        string dirFullName = Path.GetFullPath(dirName);
        Dictionary<string, List<string>> dirsForThisLevel = _allDirs[level];
        foreach (string dir in dirsForThisLevel.Keys)
        {
            if (dir.Equals(dirFullName, StringComparison.CurrentCultureIgnoreCase))
                return dirsForThisLevel[dir].ToArray();
        }
        return null;
    }

    public string[] GetAllFiles()
    {
        return _listOfFiles.ToArray();
    }
}


public class TestInfo
{
    static TestInfo()
    {
        CurrentDirectory = Directory.GetCurrentDirectory();
    }

    public static string CurrentDirectory { get; set; }
}
