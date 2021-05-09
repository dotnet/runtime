// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.VisualBasic.Tests
{
    public class FileSystemTests : System.IO.FileCleanupTestBase
    {
        protected override void Dispose(bool disposing)
        {
            try
            {
                FileSystem.FileClose(0); // close all files
            }
            catch (Exception)
            {
            }
            base.Dispose(disposing);
        }

        // On OSX, the temp directory /tmp/ is a symlink to /private/tmp, so setting the current
        // directory to a symlinked path will result in GetCurrentDirectory returning the absolute
        // path that followed the symlink.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotOSX))]
        public void ChDir()
        {
            var savedDirectory = System.IO.Directory.GetCurrentDirectory();
            FileSystem.ChDir(TestDirectory);
            Assert.Equal(TestDirectory, System.IO.Directory.GetCurrentDirectory());
            FileSystem.ChDir(savedDirectory);
            Assert.Equal(savedDirectory, System.IO.Directory.GetCurrentDirectory());
        }

        // Not tested:
        //   public static void ChDrive(char Drive){ throw null; }
        //   public static void ChDrive(string Drive){ throw null; }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49568", typeof(PlatformDetection), nameof(PlatformDetection.IsMacOsAppleSilicon))]
        public void CloseAllFiles()
        {
            var fileName1 = GetTestFilePath();
            var fileName2 = GetTestFilePath();

            RemoteExecutor.Invoke(
                (fileName1, fileName2) =>
                {
                    putStringNoClose(fileName1, "abc");
                    putStringNoClose(fileName2, "123");

                    // ProjectData.EndApp() should close all open files.
                    Microsoft.VisualBasic.CompilerServices.ProjectData.EndApp();

                    static void putStringNoClose(string fileName, string str)
                    {
                        int fileNumber = FileSystem.FreeFile();
                        FileSystem.FileOpen(fileNumber, fileName, OpenMode.Random);
                        FileSystem.FilePut(fileNumber, str);
                    }
                },
                fileName1,
                fileName2,
                new RemoteInvokeOptions() { ExpectedExitCode = 0 }).Dispose();

            // Verify all text was written to the files.
            Assert.Equal("abc", getString(fileName1));
            Assert.Equal("123", getString(fileName2));

            static string getString(string fileName)
            {
                int fileNumber = FileSystem.FreeFile();
                FileSystem.FileOpen(fileNumber, fileName, OpenMode.Random);
                string str = null;
                FileSystem.FileGet(fileNumber, ref str);
                FileSystem.FileClose(fileNumber);
                return str;
            }
        }

        // Can't get current directory on OSX before setting it.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotOSX))]
        public void CurDir()
        {
            Assert.Equal(FileSystem.CurDir(), System.IO.Directory.GetCurrentDirectory());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        public void CurDir_Drive()
        {
            var currentDirectory = System.IO.Directory.GetCurrentDirectory();
            int index = currentDirectory.IndexOf(System.IO.Path.VolumeSeparatorChar);
            if (index == 1)
            {
                Assert.Equal(currentDirectory, FileSystem.CurDir(currentDirectory[0]));
            }
        }

        [Fact]
        public void Dir()
        {
            var fileNames = Enumerable.Range(0, 3).Select(i => GetTestFileName(i)).ToArray();
            int n = fileNames.Length;

            for (int i = 0; i < n; i++)
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(TestDirectory, fileNames[i]), i.ToString());
            }

            // Get all files.
            string fileName = FileSystem.Dir(System.IO.Path.Combine(TestDirectory, "*"));
            var foundNames = new string[n];
            for (int i = 0; i < n; i++)
            {
                foundNames[i] = fileName;
                fileName = FileSystem.Dir();
            }
            Assert.Null(fileName);

            Array.Sort(fileNames);
            Array.Sort(foundNames);
            for (int i = 0; i < n; i++)
            {
                Assert.Equal(fileNames[i], foundNames[i]);
            }

            // Get single file.
            fileName = FileSystem.Dir(System.IO.Path.Combine(TestDirectory, fileNames[2]));
            Assert.Equal(fileName, fileNames[2]);
            fileName = FileSystem.Dir();
            Assert.Null(fileName);

            // Get missing file.
            fileName = FileSystem.Dir(GetTestFilePath(n));
            Assert.Equal("", fileName);
        }

        [Fact]
        public void Dir_Volume()
        {
            if (PlatformDetection.IsWindows)
            {
                _ = FileSystem.Dir(TestDirectory, FileAttribute.Volume);
            }
            else
            {
                AssertThrows<PlatformNotSupportedException>(() => FileSystem.Dir(TestDirectory, FileAttribute.Volume));
            }
        }

        [Fact]
        public void EOF()
        {
            int fileNumber = FileSystem.FreeFile();
            var fileName = GetTestFilePath();
            FileSystem.FileOpen(fileNumber, fileName, OpenMode.Output);
            FileSystem.Write(fileNumber, 'a');
            FileSystem.Write(fileNumber, 'b');
            FileSystem.Write(fileNumber, 'c');
            FileSystem.FileClose(fileNumber);

            FileSystem.FileOpen(fileNumber, fileName, OpenMode.Input);
            for (int i = 0; i < 3; i++)
            {
                Assert.False(FileSystem.EOF(fileNumber));
                char c = default;
                FileSystem.Input(fileNumber, ref c);
            }
            Assert.True(FileSystem.EOF(fileNumber));
            FileSystem.FileClose(fileNumber);
        }

        // Not tested:
        //   public static OpenMode FileAttr(int FileNumber){ throw null; }

        [Fact]
        public void FileClose()
        {
            int fileNumber = FileSystem.FreeFile();

            // Close before opening.
            FileSystem.FileClose(fileNumber);

            createAndOpenFile(fileNumber);
            FileSystem.FileClose(fileNumber);

            // Close a second time.
            FileSystem.FileClose(fileNumber);

            // Close all files.
            fileNumber = FileSystem.FreeFile();
            createAndOpenFile(fileNumber);
            createAndOpenFile(FileSystem.FreeFile());
            Assert.NotEqual(fileNumber, FileSystem.FreeFile());
            FileSystem.FileClose(0);
            Assert.Equal(fileNumber, FileSystem.FreeFile());

            void createAndOpenFile(int fileNumber)
            {
                var fileName = GetTestFilePath();
                System.IO.File.WriteAllText(fileName, "abc123");
                FileSystem.FileOpen(fileNumber, fileName, OpenMode.Input);
            }
        }

        [Fact]
        public void FileCopy()
        {
            // Copy to a new file.
            var sourceName = GetTestFilePath();
            System.IO.File.WriteAllText(sourceName, "abc");
            var destName = GetTestFilePath();
            FileSystem.FileCopy(sourceName, destName);
            Assert.Equal("abc", System.IO.File.ReadAllText(destName));

            // Copy over an existing file.
            sourceName = GetTestFilePath();
            System.IO.File.WriteAllText(sourceName, "def");
            destName = GetTestFilePath();
            System.IO.File.WriteAllText(destName, "123");
            FileSystem.FileCopy(sourceName, destName);
            Assert.Equal("def", System.IO.File.ReadAllText(destName));
        }

        // Not tested:
        //   public static System.DateTime FileDateTime(string PathName){ throw null; }

        [Fact]
        public void FileGet_FilePut()
        {
            int fileNumber = FileSystem.FreeFile();
            var fileName = GetTestFilePath();
            DateTime dateTime = new DateTime(2019, 1, 1);

            FileSystem.FileOpen(fileNumber, fileName, OpenMode.Random);
            FileSystem.FilePut(fileNumber, true);
            FileSystem.FilePut(fileNumber, (byte)1);
            FileSystem.FilePut(fileNumber, (char)2);
            FileSystem.FilePut(fileNumber, (decimal)3);
            FileSystem.FilePut(fileNumber, 4.0);
            FileSystem.FilePut(fileNumber, 5.0f);
            FileSystem.FilePut(fileNumber, 6);
            FileSystem.FilePut(fileNumber, 7L);
            FileSystem.FilePut(fileNumber, (short)8);
            FileSystem.FilePut(fileNumber, "ABC");
            FileSystem.FilePut(fileNumber, dateTime);
            FileSystem.FileClose(fileNumber);

            bool _bool = default;
            byte _byte = default;
            char _char = default;
            decimal _decimal = default;
            double _double = default;
            float _float = default;
            int _int = default;
            long _long = default;
            short _short = default;
            string _string = default;
            DateTime _dateTime = default;
            FileSystem.FileOpen(fileNumber, fileName, OpenMode.Random);
            FileSystem.FileGet(fileNumber, ref _bool);
            FileSystem.FileGet(fileNumber, ref _byte);
            FileSystem.FileGet(fileNumber, ref _char);
            FileSystem.FileGet(fileNumber, ref _decimal);
            FileSystem.FileGet(fileNumber, ref _double);
            FileSystem.FileGet(fileNumber, ref _float);
            FileSystem.FileGet(fileNumber, ref _int);
            FileSystem.FileGet(fileNumber, ref _long);
            FileSystem.FileGet(fileNumber, ref _short);
            FileSystem.FileGet(fileNumber, ref _string);
            FileSystem.FileGet(fileNumber, ref _dateTime);
            Assert.True(FileSystem.EOF(fileNumber));
            FileSystem.FileClose(fileNumber);

            Assert.True(_bool);
            Assert.Equal((byte)1, _byte);
            Assert.Equal((char)2, _char);
            Assert.Equal((decimal)3, _decimal);
            Assert.Equal(4.0, _double);
            Assert.Equal(5.0f, _float);
            Assert.Equal(6, _int);
            Assert.Equal(7L, _long);
            Assert.Equal((short)8, _short);
            Assert.Equal("ABC", _string);
            Assert.Equal(dateTime, _dateTime);
        }

        // Not tested:
        //   public static void FileGet(int FileNumber, ref System.Array Value, long RecordNumber = -1, bool ArrayIsDynamic = false, bool StringIsFixedLength = false) { }
        //   public static void FileGet(int FileNumber, ref System.ValueType Value, long RecordNumber = -1) { }
        //   public static void FilePut(int FileNumber, System.Array Value, long RecordNumber = -1, bool ArrayIsDynamic = false, bool StringIsFixedLength = false) { }
        //   public static void FilePut(int FileNumber, System.ValueType Value, long RecordNumber = -1) { }
        //   public static void FilePut(object FileNumber, object Value, object RecordNumber/* = -1*/) { }

        [Fact]
        public void FileGetObject_FilePutObject()
        {
            int fileNumber = FileSystem.FreeFile();
            var fileName = GetTestFilePath();
            DateTime dateTime = new DateTime(2019, 1, 1);

            FileSystem.FileOpen(fileNumber, fileName, OpenMode.Random);
            FileSystem.FilePutObject(fileNumber, true);
            FileSystem.FilePutObject(fileNumber, (byte)1);
            FileSystem.FilePutObject(fileNumber, (char)2);
            FileSystem.FilePutObject(fileNumber, (decimal)3);
            FileSystem.FilePutObject(fileNumber, 4.0);
            FileSystem.FilePutObject(fileNumber, 5.0f);
            FileSystem.FilePutObject(fileNumber, 6);
            FileSystem.FilePutObject(fileNumber, 7L);
            FileSystem.FilePutObject(fileNumber, (short)8);
            FileSystem.FilePutObject(fileNumber, "ABC");
            FileSystem.FilePutObject(fileNumber, dateTime);
            FileSystem.FileClose(fileNumber);

            object _bool = null;
            object _byte = null;
            object _char = null;
            object _decimal = null;
            object _double = null;
            object _float = null;
            object _int = null;
            object _long = null;
            object _short = null;
            object _string = null;
            object _dateTime = null;
            FileSystem.FileOpen(fileNumber, fileName, OpenMode.Random);
            FileSystem.FileGetObject(fileNumber, ref _bool);
            FileSystem.FileGetObject(fileNumber, ref _byte);
            FileSystem.FileGetObject(fileNumber, ref _char);
            FileSystem.FileGetObject(fileNumber, ref _decimal);
            FileSystem.FileGetObject(fileNumber, ref _double);
            FileSystem.FileGetObject(fileNumber, ref _float);
            FileSystem.FileGetObject(fileNumber, ref _int);
            FileSystem.FileGetObject(fileNumber, ref _long);
            FileSystem.FileGetObject(fileNumber, ref _short);
            FileSystem.FileGetObject(fileNumber, ref _string);
            FileSystem.FileGetObject(fileNumber, ref _dateTime);
            Assert.True(FileSystem.EOF(fileNumber));
            FileSystem.FileClose(fileNumber);

            Assert.Equal((object)true, _bool);
            Assert.Equal((byte)1, _byte);
            Assert.Equal((char)2, _char);
            Assert.Equal((decimal)3, _decimal);
            Assert.Equal(4.0, _double);
            Assert.Equal(5.0f, _float);
            Assert.Equal(6, _int);
            Assert.Equal(7L, _long);
            Assert.Equal((short)8, _short);
            Assert.Equal("ABC", _string);
            Assert.Equal(dateTime, _dateTime);
        }

        [Fact]
        public void FileLen()
        {
            int fileNumber = FileSystem.FreeFile();
            string fileName = GetTestFilePath();
            FileSystem.FileOpen(fileNumber, fileName, OpenMode.Append);
            FileSystem.FileClose(fileNumber);
            Assert.Equal(0, FileSystem.FileLen(fileName));

            fileNumber = FileSystem.FreeFile();
            fileName = GetTestFilePath();
            System.IO.File.WriteAllText(fileName, "abc123");
            Assert.Equal(6, FileSystem.FileLen(fileName));
        }

        [Fact]
        public void FileOpen()
        {
            // OpenMode.Append:
            int fileNumber = FileSystem.FreeFile();
            string fileName = GetTestFilePath();
            FileSystem.FileOpen(fileNumber, fileName, OpenMode.Append);
            FileSystem.FileClose(fileNumber);

            // OpenMode.Binary:
            fileNumber = FileSystem.FreeFile();
            fileName = GetTestFilePath();
            FileSystem.FileOpen(fileNumber, fileName, OpenMode.Binary);
            FileSystem.FileClose(fileNumber);

            // OpenMode.Input:
            fileNumber = FileSystem.FreeFile();
            fileName = GetTestFilePath();
            AssertThrows<System.IO.FileNotFoundException>(() => FileSystem.FileOpen(fileNumber, fileName, OpenMode.Input));
            System.IO.File.WriteAllText(fileName, "abc123");
            FileSystem.FileOpen(fileNumber, fileName, OpenMode.Input);
            FileSystem.FileClose(fileNumber);

            // OpenMode.Output:
            fileNumber = FileSystem.FreeFile();
            fileName = GetTestFilePath();
            FileSystem.FileOpen(fileNumber, fileName, OpenMode.Output);
            FileSystem.FileClose(fileNumber);

            // OpenMode.Random:
            fileNumber = FileSystem.FreeFile();
            fileName = GetTestFilePath();
            FileSystem.FileOpen(fileNumber, fileName, OpenMode.Random);
            FileSystem.FileClose(fileNumber);

            // Open a second time.
            fileNumber = FileSystem.FreeFile();
            fileName = GetTestFilePath();
            FileSystem.FileOpen(fileNumber, fileName, OpenMode.Append);
            AssertThrows<System.IO.IOException>(() => FileSystem.FileOpen(fileNumber, fileName, OpenMode.Append));
            FileSystem.FileClose(fileNumber);

            // Open an invalid fileNumber.
            AssertThrows<System.IO.IOException>(() => FileSystem.FileOpen(256, GetTestFilePath(), OpenMode.Append));
        }

        // Not tested:
        //   public static void FileWidth(int FileNumber, int RecordWidth) { }

        [Fact]
        public void FreeFile()
        {
            int fileNumber = FileSystem.FreeFile();
            Assert.Equal(fileNumber, FileSystem.FreeFile());
            FileSystem.FileOpen(fileNumber, GetTestFilePath(), OpenMode.Append);
            Assert.NotEqual(fileNumber, FileSystem.FreeFile());
        }

        // Not tested:
        //   public static FileAttribute GetAttr(string PathName) { throw null; }

        [Fact]
        public void Input_Write()
        {
            int fileNumber = FileSystem.FreeFile();
            var fileName = GetTestFilePath();
            DateTime dateTime = new DateTime(2019, 1, 1);

            FileSystem.FileOpen(fileNumber, fileName, OpenMode.Output);
            FileSystem.Write(fileNumber, true);
            FileSystem.Write(fileNumber, (byte)1, (char)2, (decimal)3, 4.0, 5.0f);
            FileSystem.Write(fileNumber, 6, 7L);
            FileSystem.Write(fileNumber, (short)8, "ABC", dateTime);
            FileSystem.FileClose(fileNumber);

            bool _bool = default;
            byte _byte = default;
            char _char = default;
            decimal _decimal = default;
            double _double = default;
            float _float = default;
            int _int = default;
            long _long = default;
            short _short = default;
            string _string = default;
            DateTime _dateTime = default;
            FileSystem.FileOpen(fileNumber, fileName, OpenMode.Input);
            FileSystem.Input(fileNumber, ref _bool);
            FileSystem.Input(fileNumber, ref _byte);
            FileSystem.Input(fileNumber, ref _char);
            FileSystem.Input(fileNumber, ref _decimal);
            FileSystem.Input(fileNumber, ref _double);
            FileSystem.Input(fileNumber, ref _float);
            FileSystem.Input(fileNumber, ref _int);
            FileSystem.Input(fileNumber, ref _long);
            FileSystem.Input(fileNumber, ref _short);
            FileSystem.Input(fileNumber, ref _string);
            FileSystem.Input(fileNumber, ref _dateTime);
            Assert.True(FileSystem.EOF(fileNumber));
            FileSystem.FileClose(fileNumber);

            Assert.True(_bool);
            Assert.Equal((byte)1, _byte);
            Assert.Equal((char)2, _char);
            Assert.Equal((decimal)3, _decimal);
            Assert.Equal(4.0, _double);
            Assert.Equal(5.0f, _float);
            Assert.Equal(6, _int);
            Assert.Equal(7L, _long);
            Assert.Equal((short)8, _short);
            Assert.Equal("ABC", _string);
            Assert.Equal(dateTime, _dateTime);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34362", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        public void Input_Object_Write()
        {
            int fileNumber = FileSystem.FreeFile();
            var fileName = GetTestFilePath();
            DateTime dateTime = new DateTime(2019, 1, 1);

            FileSystem.FileOpen(fileNumber, fileName, OpenMode.Output);
            FileSystem.Write(fileNumber, DBNull.Value);
            FileSystem.Write(fileNumber, true);
            FileSystem.Write(fileNumber, (byte)1, (char)2, (decimal)3, 4.0, 5.0f);
            FileSystem.Write(fileNumber, 6, 7L);
            FileSystem.Write(fileNumber, (short)8, "ABC", dateTime);
            FileSystem.FileClose(fileNumber);

            object _dbnull = null;
            object _bool = null;
            object _byte = null;
            object _char = null;
            object _decimal = null;
            object _double = null;
            object _float = null;
            object _int = null;
            object _long = null;
            object _short = null;
            object _string = null;
            object _dateTime = null;
            FileSystem.FileOpen(fileNumber, fileName, OpenMode.Input);
            try
            {
                FileSystem.Input(fileNumber, ref _dbnull);
                FileSystem.Input(fileNumber, ref _bool);
                FileSystem.Input(fileNumber, ref _byte);
                FileSystem.Input(fileNumber, ref _char);
                FileSystem.Input(fileNumber, ref _decimal);
                FileSystem.Input(fileNumber, ref _double);
                FileSystem.Input(fileNumber, ref _float);
                FileSystem.Input(fileNumber, ref _int);
                FileSystem.Input(fileNumber, ref _long);
                FileSystem.Input(fileNumber, ref _short);
                FileSystem.Input(fileNumber, ref _string);
                FileSystem.Input(fileNumber, ref _dateTime);
                Assert.True(FileSystem.EOF(fileNumber));

                Assert.Equal(DBNull.Value, _dbnull);
                Assert.Equal((object)true, _bool);
                Assert.Equal((short)1, _byte);
                Assert.Equal("\u0002", _char);
                Assert.Equal((short)3, _decimal);
                Assert.Equal((short)4, _double);
                Assert.Equal((short)5, _float);
                Assert.Equal((short)6, _int);
                Assert.Equal((short)7, _long);
                Assert.Equal((short)8, _short);
                Assert.Equal("ABC", _string);
                Assert.Equal(dateTime, _dateTime);
            }
            catch (PlatformNotSupportedException)
            {
                // Conversion.ParseInputField() is not supported on non-Windows platforms currently,
                // but this is also failing on some Windows platforms. Need to investigate.
            }
            FileSystem.FileClose(fileNumber);
        }

        // Not tested:
        //   public static string InputString(int FileNumber, int CharCount) { throw null; }

        [Fact]
        public void Kill()
        {
            var fileName = GetTestFilePath();
            System.IO.File.WriteAllText(fileName, "abc123");
            Assert.True(System.IO.File.Exists(fileName));
            FileSystem.Kill(fileName);
            Assert.False(System.IO.File.Exists(fileName));

            // Missing file.
            fileName = GetTestFilePath();
            AssertThrows<System.IO.FileNotFoundException>(() => FileSystem.Kill(fileName));
            Assert.False(System.IO.File.Exists(fileName));
        }

        // Not tested:
        //   public static string LineInput(int FileNumber) { throw null; }
        //   public static long Loc(int FileNumber) { throw null; }

        // Lock is supported on Windows only currently.
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Lock_Unlock()
        {
            int fileNumber = FileSystem.FreeFile();
            var fileName = GetTestFilePath();
            FileSystem.FileOpen(fileNumber, fileName, OpenMode.Output, Share: OpenShare.Shared);
            remoteWrite(fileName, "abc");
            try
            {
                FileSystem.Lock(fileNumber);
                remoteWrite(fileName, "123");
            }
            finally
            {
                FileSystem.Unlock(fileNumber);
            }
            remoteWrite(fileName, "456");
            FileSystem.FileClose(fileNumber);
            Assert.Equal("abc456", System.IO.File.ReadAllText(fileName));

            static void remoteWrite(string fileName, string text)
            {
                RemoteExecutor.Invoke(
                    (fileName, text) =>
                    {
                        using (var stream = System.IO.File.Open(fileName, System.IO.FileMode.Append, System.IO.FileAccess.Write, System.IO.FileShare.ReadWrite))
                        {
                            try
                            {
                                using (var writer = new System.IO.StreamWriter(stream))
                                {
                                    writer.Write(text);
                                }
                            }
                            catch (System.IO.IOException)
                            {
                            }
                        }
                    },
                    fileName,
                    text,
                    new RemoteInvokeOptions() { ExpectedExitCode = 0 }).Dispose();
            }
        }

        // Not tested:
        //   public static void Lock(int FileNumber, long Record) { }
        //   public static void Lock(int FileNumber, long FromRecord, long ToRecord) { }
        //   public static long LOF(int FileNumber) { throw null; }
        //   public static void Unlock(int FileNumber, long Record) { }
        //   public static void Unlock(int FileNumber, long FromRecord, long ToRecord) { }

        [Fact]
        public void MkDir_RmDir()
        {
            var dirName = GetTestFilePath();
            Assert.False(System.IO.Directory.Exists(dirName));

            FileSystem.MkDir(dirName);
            Assert.True(System.IO.Directory.Exists(dirName));

            // Create the directory a second time.
            AssertThrows<System.IO.IOException>(() => FileSystem.MkDir(dirName));

            FileSystem.RmDir(dirName);
            Assert.False(System.IO.Directory.Exists(dirName));

            // Remove the directory a second time.
            AssertThrows<System.IO.DirectoryNotFoundException>(() => FileSystem.RmDir(dirName));
        }

        // Not tested:
        //   public static void Print(int FileNumber, params object[] Output) { }
        //   public static void PrintLine(int FileNumber, params object[] Output) { }

        [Fact]
        public void Rename()
        {
            // Rename to an unused name.
            var sourceName = GetTestFilePath();
            System.IO.File.WriteAllText(sourceName, "abc");
            var destName = GetTestFilePath();
            if (PlatformDetection.IsWindows)
            {
                FileSystem.Rename(sourceName, destName);
                Assert.False(System.IO.File.Exists(sourceName));
                Assert.True(System.IO.File.Exists(destName));
                Assert.Equal("abc", System.IO.File.ReadAllText(destName));
            }
            else
            {
                AssertThrows<PlatformNotSupportedException>(() => FileSystem.Rename(sourceName, destName));
                Assert.True(System.IO.File.Exists(sourceName));
                Assert.False(System.IO.File.Exists(destName));
                Assert.Equal("abc", System.IO.File.ReadAllText(sourceName));
            }

            // Rename to an existing name.
            sourceName = GetTestFilePath();
            System.IO.File.WriteAllText(sourceName, "def");
            destName = GetTestFilePath();
            System.IO.File.WriteAllText(destName, "123");
            if (PlatformDetection.IsWindows)
            {
                AssertThrows<System.IO.IOException>(() => FileSystem.Rename(sourceName, destName));
            }
            else
            {
                AssertThrows<PlatformNotSupportedException>(() => FileSystem.Rename(sourceName, destName));
            }
            Assert.True(System.IO.File.Exists(sourceName));
            Assert.True(System.IO.File.Exists(destName));
            Assert.Equal("def", System.IO.File.ReadAllText(sourceName));
            Assert.Equal("123", System.IO.File.ReadAllText(destName));
        }

        // Not tested:
        //   public static void Reset() { }
        //   public static void Seek(int FileNumber, long Position) { }
        //   public static long Seek(int FileNumber) { throw null; }
        //   public static void SetAttr(string PathName, FileAttribute Attributes) { }
        //   public static SpcInfo SPC(short Count) { throw null; }
        //   public static TabInfo TAB() { throw null; }
        //   public static TabInfo TAB(short Column) { throw null; }
        //   public static void WriteLine(int FileNumber, params object[] Output) { }

        [Fact]
        public void Write_ArgumentException()
        {
            int fileNumber = FileSystem.FreeFile();
            var fileName = GetTestFilePath();
            FileSystem.FileOpen(fileNumber, fileName, OpenMode.Output);
            AssertThrows<ArgumentException>(() => FileSystem.Write(fileNumber, new object()));
            FileSystem.FileClose(fileNumber);
        }

        // We cannot use XUnit.Assert.Throws<T>() for lambdas that rely on AssemblyData (such as
        // file numbers) because AssemblyData instances are associated with the calling assembly, and
        // in RELEASE builds, the calling assembly of a lambda invoked from XUnit.Assert.Throws<T>()
        // is corelib rather than this assembly, so file numbers created outside the lambda will be invalid.
        private static void AssertThrows<TException>(Action action) where TException : Exception
        {
            TException ex = null;
            try
            {
                action();
            }
            catch (TException e)
            {
                ex = e;
            }
            if (ex == null)
            {
                throw new ThrowsException(typeof(TException));
            }

            if (ex.GetType() != typeof(TException))
            {
                throw new ThrowsException(typeof(TException), ex);
            }
        }
    }
}
