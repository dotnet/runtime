// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.FileSystem.Tests;
using Xunit;

namespace EnumerableTests
{
    public class File_EnumerableAPIs : FileSystemTest
    {
        private static EnumerableUtils utils;

        [Fact]
        public void RunTests()
        {
            utils = new EnumerableUtils();

            utils.CreateTestDirs(TestDirectory);

            TestFileAPIs();

            TestFileExceptions();

            utils.DeleteTestDirs();

            Assert.True(utils.Passed);
        }

        // file tests
        private static void TestFileAPIs()
        {
            TestFileReadAllLinesFast(false);
            TestFileReadAllLinesFast(true);
            TestFileWriteAllLines(false, false);
            TestFileWriteAllLines(true, false);
            TestFileWriteAllLines(false, true);
            TestFileWriteAllLines(true, true);
            TestFileReadAllLinesIterator();
        }

        private static void TestFileReadAllLinesIterator()
        {
            IEnumerable<string> lines = File.ReadLines(Path.Combine(utils.testDir, "file1"));
            foreach (var line in lines)
            {
            }

            try
            {
                foreach (var line in lines)
                {
                }
            }
            catch (ObjectDisposedException)
            {
                // Currently the test is not hooked to report in case of failures. It is best if we rethrow an exception until we rewrite the harness
                Console.WriteLine("We should not get an ObjectDisposedException but got one ; Dev10 864262 regressed");
                throw;
            }

        }

        private static void TestFileWriteAllLines(bool useEncodingOverload, bool append)
        {
            String chkptFlag = "chkpt_wal_";
            int failCount = 0;

            String encodingPart = useEncodingOverload ? ", Encoding" : "";
            String writePart = append ? "Append" : "Write";
            String testname = String.Format("File.{0}AllLines(path{1})", writePart, encodingPart);


            String lineContent = "This is another line";
            String firstLineContent = "This is the first line";
            String[] contents = new String[10];
            for (int i = 0; i < contents.Length; i++)
            {
                contents[i] = lineContent;
            }

            String file1 = Path.Combine(utils.testDir, "file1.out");
            if (append)
            {
                File.WriteAllLines(file1, new String[] { firstLineContent });
            }


            if (useEncodingOverload)
            {
                if (append)
                {
                    File.AppendAllLines(file1, (IEnumerable<String>)contents, Encoding.UTF8);
                }
                else
                {

                    File.WriteAllLines(file1, (IEnumerable<String>)contents, Encoding.UTF8);
                }
            }
            else
            {
                if (append)
                {
                    File.AppendAllLines(file1, (IEnumerable<String>)contents);
                }
                else
                {
                    File.WriteAllLines(file1, (IEnumerable<String>)contents);
                }
            }


            int expectedCount = append ? 11 : 10;

            IEnumerable<String> fileContents = null;
            if (useEncodingOverload)
            {
                fileContents = File.ReadLines(file1, Encoding.UTF8);
            }
            else
            {
                fileContents = File.ReadLines(file1);
            }

            int count = 0;
            foreach (String line in fileContents)
            {
                if (append && count == 0)
                {
                    if (!firstLineContent.Equals(line))
                    {
                        failCount++;
                        Console.WriteLine(chkptFlag + "1: {0} FAILED at first line", testname);
                        Console.WriteLine("got unexpected line: " + line);
                    }
                    count++;
                    continue;
                }

                count++;
                if (!lineContent.Equals(line))
                {
                    failCount++;
                    Console.WriteLine(chkptFlag + "2: {0} FAILED", testname);
                    Console.WriteLine("got unexpected line: " + line);

                }
            }
            if (count != expectedCount)
            {
                failCount++;
                Console.WriteLine(chkptFlag + "3: {0} FAILED. Line count didn't equal expected", testname);
                Console.WriteLine("expected {0} but got {1}", expectedCount, count);
            }

            utils.PrintTestStatus(testname, "WriteAllLines", failCount);

            File.Delete(file1);

        }

        private static void TestFileReadAllLinesFast(bool useEncodingOverload)
        {
            String chkptFlag = "chkpt_ralf_";
            int failCount = 0;

            string testname = null;
            if (useEncodingOverload)
            {
                testname = "File.ReadLines(path, Encoding)";
            }
            else
            {
                testname = "File.ReadLines(path)";
            }
            String file1 = Path.Combine(utils.testDir, "file1.out");
            String lineContent = "This is a line of test file content";
            String[] contents = new String[10];
            for (int i = 0; i < contents.Length; i++)
            {
                contents[i] = lineContent;
            }

            if (useEncodingOverload)
            {
                File.WriteAllLines(file1, contents, Encoding.UTF8);
            }
            else
            {

                File.WriteAllLines(file1, contents);
            }

            IEnumerable<String> fileContents = null;
            if (useEncodingOverload)
            {
                fileContents = File.ReadLines(file1, Encoding.UTF8);
            }
            else
            {
                fileContents = File.ReadLines(file1);
            }

            int count = 0;
            foreach (String line in fileContents)
            {
                count++;
                if (!lineContent.Equals(line))
                {
                    failCount++;
                    Console.WriteLine(chkptFlag + "1: {0} FAILED", testname);
                    Console.WriteLine("got unexpected line: " + line);
                }
            }

            if (count != 10)
            {
                failCount++;
                Console.WriteLine(chkptFlag + "2: {0} FAILED. Line count didn't equal expected", testname);
                Console.WriteLine("expected {0} but got {1}", 10, count);
            }

            File.Delete(file1);

            // empty file
            file1 = Path.Combine(utils.testDir, "empty.out");
            FileStream fs = File.Create(file1);
            fs.Dispose();

            fileContents = null;
            if (useEncodingOverload)
            {
                fileContents = File.ReadLines(file1, Encoding.UTF8);
            }
            else
            {
                fileContents = File.ReadLines(file1);
            }

            foreach (String line in fileContents)
            {
                failCount++;
                Console.WriteLine(chkptFlag + "3: {0} FAILED", testname);
                Console.WriteLine("got unexpected line: " + line);
            }

            File.Delete(file1);

            utils.PrintTestStatus(testname, "ReadLines", failCount);

        }

        private static void TestFileExceptions()
        {
            // Create common file and dir names
            String nullFileName = null;
            String emptyFileName1 = "";
            String emptyFileName2 = " ";
            String longPath = Path.Combine(new String('a', IOInputs.MaxDirectory), "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", "test.txt");

            String notExistsFileName = null;
            if (Interop.IsWindows) // drive labels
            {
                String unusedDrive = EnumerableUtils.GetUnusedDrive();
                if (unusedDrive != null)
                {
                    notExistsFileName = Path.Combine(unusedDrive, Path.Combine("temp", "notExists", "temp.txt")); // just skip otherwise
                }
            }
            String[] lines = { "test line 1", "test line 2" };
            IEnumerable<String> contents = (IEnumerable<String>)lines;

            // Read exceptions
            TestReadFileExceptions(nullFileName, "null path", Encoding.UTF8, new ArgumentNullException());
            TestReadFileExceptions(emptyFileName1, "empty path 1", Encoding.UTF8, new ArgumentException());
            TestReadFileExceptions(emptyFileName2, "empty path 2", Encoding.UTF8, new ArgumentException());
            TestReadFileExceptions(longPath, "long path", Encoding.UTF8, new PathTooLongException());
            if (notExistsFileName != null)
            {
                TestReadFileExceptions(notExistsFileName, "not exists", Encoding.UTF8, new DirectoryNotFoundException());
            }
            TestReadFileExceptionsWithEncoding("temp.txt", "null encoding", null, new ArgumentNullException(), File.ReadLines, File.ReadLines, "File.ReadLines");

            // Write exceptions
            TestWriteFileExceptions(nullFileName, "null path", contents, Encoding.UTF8, new ArgumentNullException());
            TestWriteFileExceptions(emptyFileName1, "empty path 1", contents, Encoding.UTF8, new ArgumentException());
            TestWriteFileExceptions(emptyFileName2, "empty path 2", contents, Encoding.UTF8, new ArgumentException());
            TestWriteFileExceptions(longPath, "long path", contents, Encoding.UTF8, new PathTooLongException());
            if (notExistsFileName != null)
            {
                TestWriteFileExceptions(notExistsFileName, "not exists", contents, Encoding.UTF8, new DirectoryNotFoundException());
            }
            TestWriteFileExceptions("temp.txt", "null contents", null, Encoding.UTF8, new ArgumentNullException());

            TestWriteFileExceptionsWithEncoding("temp.txt", "null encoding", contents, null, new ArgumentNullException(), File.WriteAllLines, File.WriteAllLines, "File.WriteAllLines");
            TestWriteFileExceptionsWithEncoding("temp.txt", "null encoding", contents, null, new ArgumentNullException(), File.AppendAllLines, File.AppendAllLines, "File.AppendAllLines");

        }

        private static void TestReadFileExceptions(String fileName, String fileNameDescription, Encoding encoding, Exception expectedException)
        {
            TestReadFileExceptionsDefaultEncoding(fileName, fileNameDescription, expectedException, File.ReadLines, File.ReadLines, "File.ReadLines");
            TestReadFileExceptionsWithEncoding(fileName, fileNameDescription, encoding, expectedException, File.ReadLines, File.ReadLines, "File.ReadLines");
        }

        private static void TestReadFileExceptionsDefaultEncoding(String path, String pathDescription, Exception expectedException,
                                                                    EnumerableUtils.ReadFastDelegate1 readDelegate, EnumerableUtils.ReadFastDelegate2 readDelegate2, String methodName)
        {
            int failCount = 0;
            String chkptFlag = "chkpt_rfede_";

            try
            {
                IEnumerable<String> lines = readDelegate(path);
                Console.WriteLine(chkptFlag + "1: didn't throw");
                failCount++;
            }
            catch (Exception e)
            {
                if (e.GetType() != expectedException.GetType())
                {
                    failCount++;
                    Console.WriteLine(chkptFlag + "2: threw wrong exception");
                    Console.WriteLine(e);
                }
            }

            String testName = String.Format("TestReadFileExceptions({0})", pathDescription);
            utils.PrintTestStatus(testName, methodName, failCount);
        }

        private static void TestReadFileExceptionsWithEncoding(String path, String pathDescription, Encoding encoding, Exception expectedException,
                                                                    EnumerableUtils.ReadFastDelegate1 readDelegate, EnumerableUtils.ReadFastDelegate2 readDelegate2, String methodName)
        {
            int failCount = 0;
            String chkptFlag = "chkpt_rfewe_";

            try
            {
                IEnumerable<String> lines = readDelegate2(path, encoding);
                Console.WriteLine(chkptFlag + "1: didn't throw");
                failCount++;
            }
            catch (Exception e)
            {
                if (e.GetType() != expectedException.GetType())
                {
                    failCount++;
                    Console.WriteLine(chkptFlag + "2: threw wrong exception");
                    Console.WriteLine(e);
                }
            }

            String testName = String.Format("TestReadFileExceptions_Encoding({0})", pathDescription);
            utils.PrintTestStatus(testName, methodName, failCount);
        }

        private static void TestWriteFileExceptions(String fileName, String fileNameDescription, IEnumerable<String> contents, Encoding encoding, Exception expectedException)
        {
            TestWriteFileExceptionsDefaultEncoding(fileName, fileNameDescription, contents, expectedException, File.WriteAllLines, File.WriteAllLines, "File.WriteAllLines");
            TestWriteFileExceptionsWithEncoding(fileName, fileNameDescription, contents, encoding, expectedException, File.WriteAllLines, File.WriteAllLines, "File.WriteAllLines");
            TestWriteFileExceptionsDefaultEncoding(fileName, fileNameDescription, contents, expectedException, File.AppendAllLines, File.AppendAllLines, "File.AppendAllLines");
            TestWriteFileExceptionsWithEncoding(fileName, fileNameDescription, contents, encoding, expectedException, File.AppendAllLines, File.AppendAllLines, "File.AppendAllLines");
        }

        private static void TestWriteFileExceptionsDefaultEncoding(String path, String pathDescription, IEnumerable<String> contents, Exception expectedException,
                                                                    EnumerableUtils.WriteFastDelegate1 writeDelegate, EnumerableUtils.WriteFastDelegate2 writeDelegate2, String methodName)
        {
            int failCount = 0;
            String chkptFlag = "chkpt_wfede_";

            try
            {
                writeDelegate(path, contents);
                Console.WriteLine(chkptFlag + "1: didn't throw");
                failCount++;
            }
            catch (Exception e)
            {
                if (e.GetType() != expectedException.GetType())
                {
                    failCount++;
                    Console.WriteLine(chkptFlag + "2: threw wrong exception");
                    Console.WriteLine(e);
                }
            }

            String testName = String.Format("TestWriteFileExceptions({0})", pathDescription);
            utils.PrintTestStatus(testName, methodName, failCount);
        }

        private static void TestWriteFileExceptionsWithEncoding(String path, String pathDescription, IEnumerable<String> contents, Encoding encoding, Exception expectedException,
                                                                    EnumerableUtils.WriteFastDelegate1 writeDelegate, EnumerableUtils.WriteFastDelegate2 writeDelegate2, String methodName)
        {
            int failCount = 0;
            String chkptFlag = "chkpt_wfewe_";

            try
            {
                writeDelegate2(path, contents, encoding);
                Console.WriteLine(chkptFlag + "1: didn't throw");
                failCount++;
            }
            catch (Exception e)
            {
                if (e.GetType() != expectedException.GetType())
                {
                    failCount++;
                    Console.WriteLine(chkptFlag + "2: threw wrong exception");
                    Console.WriteLine(e);
                }
            }

            String testName = String.Format("TestWriteFileExceptions_Encoding({0})", pathDescription);
            utils.PrintTestStatus(testName, methodName, failCount);
        }

    }
}
