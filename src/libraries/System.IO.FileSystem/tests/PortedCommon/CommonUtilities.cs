// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    public static String MachineInfo()
    {
        StringBuilder builder = new StringBuilder(String.Format("{0}/////////Machine Info///////////{0}", Environment.NewLine));
        builder.AppendLine(String.Format("CurrentDrive NTFS?: {0}", IsCurrentDriveNTFS()));
        builder.AppendLine(String.Format("////////////////////{0}", Environment.NewLine));

        return builder.ToString();
    }

    public static bool IsCurrentDriveNTFS()
    {
        return IOServices.IsDriveNTFS(IOServices.GetCurrentDrive());
    }

    public static bool IsPathAdminAccessOnly(String path, bool treatPathAsFilePath)
    {
        //we leave invalid paths as valid testcase scenarios and don't filter these
        //1) We check if the path is root on a system drive
        //2) @TODO WinDir?
        String systemDrive = Environment.GetEnvironmentVariable("SystemDrive");
        Char systemDriveLetter = systemDrive.ToLower()[0];
        try
        {
            String dirName = Path.GetFullPath(path);
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
    public static void DeleteDirectory(String path, bool recursive)
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
    public static void MoveDirectory(String sourceName, String destName)
    {
        MoveDirectoryInfo(new DirectoryInfo(sourceName), destName);
    }

    public static DirectoryInfo MoveDirectoryInfo(DirectoryInfo dirInfo, String dirToMove)
    {
        int dirModificationAttemptCount;
        bool dirModificationOperationThrew;
        dirModificationAttemptCount = 0;
        String originalDirName = dirInfo.FullName;
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
    private static void EnsureDirectoryNotExist(String path)
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
    public const String DirPrefixName = "Laks_";

    private int _directoryLevel;
    private int _numberOfFiles;
    private string _startDir;

    private Random _random;

    private List<String> _listOfFiles;
    private List<String> _listOfAllDirs;
    private Dictionary<int, Dictionary<String, List<String>>> _allDirs;


    public ManageFileSystem()
    {
        Init(GetNonExistingDir(Directory.GetCurrentDirectory(), DirPrefixName), DefaultDirectoryDepth, DefaultNumberofFiles);
    }
    public ManageFileSystem(String startDirName)
        : this(startDirName, DefaultDirectoryDepth, DefaultNumberofFiles)
    {
    }
    public ManageFileSystem(String startDirName, int dirDepth, int numFiles)
    {
        Init(startDirName, dirDepth, numFiles);
    }

    public static String GetNonExistingDir(String parentDir, String prefix)
    {
        String tempPath;
        while (true)
        {
            tempPath = Path.Combine(parentDir, String.Format("{0}{1}", prefix, Path.GetFileNameWithoutExtension(Path.GetRandomFileName())));
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

    private void Init(String startDirName, int dirDepth, int numFiles)
    {
        if (Directory.Exists(Path.GetFullPath(startDirName)))
            throw new ArgumentException(String.Format("ERROR: Directory exists : {0}", _startDir));
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
        _listOfFiles = new List<String>();
        _listOfAllDirs = new List<String>();
        Directory.CreateDirectory(_startDir);
        //we will not include this directory
        //        m_listOfAllDirs.Add(m_startDir);

        String currentWorkingDir = _startDir;
        String parentDir = _startDir;

        _allDirs = new Dictionary<int, Dictionary<String, List<String>>>();
        //        List<String> dirsForOneLevel = new List<String>();
        //        List<String> tempDirsForOneLevel;
        List<String> filesForThisDir;
        Dictionary<String, List<String>> dirsForOneLevel = new Dictionary<String, List<String>>();
        Dictionary<String, List<String>> tempDirsForOneLevel;
        dirsForOneLevel.Add(_startDir, new List<String>());
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
            tempDirsForOneLevel = new Dictionary<String, List<String>>();
            foreach (String dir in dirsForOneLevel.Keys)
            //                for (int j = 0; j < dirsForOneLevel.Count; j++)
            {
                for (int k = 0; k < numOfDirPerDir; k++)
                {
                    String dirName = GetNonExistingDir(dir, DirPrefixName);
                    Debug.Assert(!Directory.Exists(dirName), String.Format("ERR_93472g! Directory exists: {0}", dirName));
                    tempDirsForOneLevel.Add(dirName, new List<String>());
                    _listOfAllDirs.Add(dirName);
                    Directory.CreateDirectory(dirName);
                }
            }
            _allDirs.Add(i + 1, tempDirsForOneLevel);
        }
        //Then we add the files 
        //@TODO!! random or fixed?
        int numberOfFilePerDirLevel = _numberOfFiles / _directoryLevel;
        Byte[] bits;
        for (int i = 0; i < _directoryLevel; i++)
        {
            dirsForOneLevel = _allDirs[i];
            int numOfFilesForThisLevel = _random.Next(numberOfFilePerDirLevel + 1);
            int numOFilesPerDir = numOfFilesForThisLevel / dirsForOneLevel.Count;
            //UPDATE: 2/1/2005, we will add at least 1
            if (numOFilesPerDir == 0)
                numOFilesPerDir = 1;
            //            for (int j = 0; j < dirsForOneLevel.Count; j++)
            foreach (String dir in dirsForOneLevel.Keys)
            {
                filesForThisDir = dirsForOneLevel[dir];
                for (int k = 0; k < numOFilesPerDir; k++)
                {
                    String fileName = Path.Combine(dir, Path.GetFileName(Path.GetRandomFileName()));
                    bits = new Byte[_random.Next(10)];
                    _random.NextBytes(bits);
                    File.WriteAllBytes(fileName, bits);
                    _listOfFiles.Add(fileName);
                    filesForThisDir.Add(fileName);
                }
            }
        }
    }

    public String StartDirectory
    {
        get { return _startDir; }
    }

    //some methods to help us
    public String[] GetDirectories(int level)
    {
        Dictionary<String, List<String>> dirsForThisLevel = null;
        if (_allDirs.TryGetValue(level, out dirsForThisLevel))
        {
            //        Dictionary<String, List<String>> dirsForThisLevel = m_allDirs[level];
            ICollection<String> keys = dirsForThisLevel.Keys;
            String[] values = new String[keys.Count];
            keys.CopyTo(values, 0);
            return values;
        }
        else
            return new String[0];
    }

    /// <summary>
    /// Note that this doesn't return the m_startDir
    /// </summary>
    /// <returns></returns>
    public String[] GetAllDirectories()
    {
        return _listOfAllDirs.ToArray();
    }


    public String[] GetFiles(String dirName, int level)
    {
        String dirFullName = Path.GetFullPath(dirName);
        Dictionary<String, List<String>> dirsForThisLevel = _allDirs[level];
        foreach (String dir in dirsForThisLevel.Keys)
        {
            if (dir.Equals(dirFullName, StringComparison.CurrentCultureIgnoreCase))
                return dirsForThisLevel[dir].ToArray();
        }
        return null;
    }

    public String[] GetAllFiles()
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

    private delegate void ExceptionCode();

    private void DeleteFile(String fileName)
    {
        if (File.Exists(fileName))
            File.Delete(fileName);
    }


    //Checks for error
    private static bool Eval(bool expression, String msg, params Object[] values)
    {
        return Eval(expression, String.Format(msg, values));
    }

    private static bool Eval<T>(T actual, T expected, String errorMsg)
    {
        bool retValue = expected == null ? actual == null : expected.Equals(actual);

        if (!retValue)
            Eval(retValue, errorMsg +
            " Expected:" + (null == expected ? "<null>" : expected.ToString()) +
            " Actual:" + (null == actual ? "<null>" : actual.ToString()));

        return retValue;
    }

    private static bool Eval(bool expression, String msg)
    {
        if (!expression)
        {
            Console.WriteLine(msg);
        }
        return expression;
    }

    //Checks for a particular type of exception
    private static void CheckException<E>(ExceptionCode test, string error)
    {
        CheckException<E>(test, error, null);
    }

    //Checks for a particular type of exception and an Exception msg
    private static void CheckException<E>(ExceptionCode test, string error, String msgExpected)
    {
        bool exception = false;
        try
        {
            test();
            error = String.Format("{0} Exception NOT thrown ", error);
        }
        catch (Exception e)
        {
            if (e.GetType() == typeof(E))
            {
                exception = true;
                if (System.Globalization.CultureInfo.CurrentUICulture.Name == "en-US" && msgExpected != null && e.Message != msgExpected)
                {
                    exception = false;
                    error = String.Format("{0} Message Different: <{1}>", error, e.Message);
                }
            }
            else
                error = String.Format("{0} Exception type: {1}", error, e.GetType().Name);
        }
        Eval(exception, error);
    }
}
