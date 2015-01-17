// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using Xunit;

[Collection("CreateFromFile")]
public class CreateFromFile : MMFTestBase
{
    [Fact]
    public static void CreateFromFileTestCases()
    {
        bool bResult = false;
        CreateFromFile test = new CreateFromFile();

        try
        {
            bResult = test.RunTest();
        }
        catch (Exception exc_main)
        {
            bResult = false;
            Console.WriteLine("FAiL! Error in CreateFromFile! Uncaught Exception in main(), exc_main==" + exc_main.ToString());
        }

        Assert.True(bResult, "One or more test cases failed.");
    }

    public bool RunTest()
    {
        try
        {
            if (File.Exists("CreateFromFile_test1.txt"))
                File.Delete("CreateFromFile_test1.txt");

            if (File.Exists("CreateFromFile_test2.txt"))
                File.Delete("CreateFromFile_test2.txt");
            String fileText = "Non-empty file for MMF testing.";
            File.WriteAllText("CreateFromFile_test2.txt", fileText);

            ////////////////////////////////////////////////////////////////////////
            // CreateFromFile(String)
            ////////////////////////////////////////////////////////////////////////

            // [] fileName

            // null fileName
            VerifyCreateFromFileException<ArgumentNullException>("Loc001", null);

            // existing file
            VerifyCreateFromFile("Loc002", "CreateFromFile_test2.txt");

            // nonexistent file
            if (File.Exists("nonexistent.txt"))
                File.Delete("nonexistent.txt");
            VerifyCreateFromFileException<FileNotFoundException>("Loc003", "nonexistent.txt");

            // FS open
            using (FileStream fs = new FileStream("CreateFromFile_test2.txt", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                VerifyCreateFromFileException<IOException>("Loc004a", "CreateFromFile_test2.txt");
            }

            // same file - not allowed
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile("CreateFromFile_test2.txt"))
            {
                VerifyCreateFromFileException<IOException>("Loc004b", "CreateFromFile_test2.txt");
            }

            ////////////////////////////////////////////////////////////////////////
            // CreateFromFile(String, FileMode)
            ////////////////////////////////////////////////////////////////////////

            if (File.Exists("CreateFromFile_test1.txt"))
                File.Delete("CreateFromFile_test1.txt");
            if (File.Exists("CreateFromFile_test2.txt"))
                File.Delete("CreateFromFile_test2.txt");
            File.WriteAllText("CreateFromFile_test2.txt", fileText);

            // [] fileName

            // null fileName
            VerifyCreateFromFileException<ArgumentNullException>("Loc101", null, FileMode.Open);

            // existing file - open
            VerifyCreateFromFile("Loc102a", "CreateFromFile_test2.txt", FileMode.Open);
            VerifyCreateFromFile("Loc102b", "CreateFromFile_test2.txt", FileMode.OpenOrCreate);
            // existing file - create
            // can't create new since it exists
            VerifyCreateFromFileException<IOException>("Loc102d", "CreateFromFile_test2.txt", FileMode.CreateNew);
            // newly created file - exception with default capacity
            VerifyCreateFromFileException<ArgumentException>("Loc102c", "CreateFromFile_test2.txt", FileMode.Create);
            VerifyCreateFromFileException<ArgumentException>("Loc102f", "CreateFromFile_test2.txt", FileMode.Truncate);
            // append not allowed
            VerifyCreateFromFileException<ArgumentException>("Loc102e", "CreateFromFile_test2.txt", FileMode.Append);

            // nonexistent file - error
            if (File.Exists("nonexistent.txt"))
                File.Delete("nonexistent.txt");
            VerifyCreateFromFileException<FileNotFoundException>("Loc103a", "nonexistent.txt", FileMode.Open);
            // newly created file - exception with default capacity
            VerifyCreateFromFileException<ArgumentException>("Loc103b", "CreateFromFile_test1.txt", FileMode.OpenOrCreate);
            VerifyCreateFromFileException<ArgumentException>("Loc103c", "CreateFromFile_test1.txt", FileMode.CreateNew);
            VerifyCreateFromFileException<ArgumentException>("Loc103d", "CreateFromFile_test1.txt", FileMode.Create);
            VerifyCreateFromFileException<ArgumentException>("Loc103e", "CreateFromFile_test2.txt", FileMode.Truncate);
            // append not allowed
            VerifyCreateFromFileException<ArgumentException>("Loc103f", "CreateFromFile_test2.txt", FileMode.Append);

            // empty file - exception with default capacity
            using (FileStream fs = new FileStream("CreateFromFile_test1.txt", FileMode.Create))
            {
            }
            VerifyCreateFromFileException<ArgumentException>("Loc104a", "CreateFromFile_test1.txt", FileMode.Open);
            VerifyCreateFromFileException<ArgumentException>("Loc104b", "CreateFromFile_test1.txt", FileMode.OpenOrCreate);
            VerifyCreateFromFileException<ArgumentException>("Loc104c", "CreateFromFile_test1.txt", FileMode.Create);
            VerifyCreateFromFileException<ArgumentException>("Loc104d", "CreateFromFile_test1.txt", FileMode.Truncate);
            // can't create new since it exists
            VerifyCreateFromFileException<IOException>("Loc104e", "CreateFromFile_test1.txt", FileMode.CreateNew);
            // append not allowed
            VerifyCreateFromFileException<ArgumentException>("Loc104f", "CreateFromFile_test1.txt", FileMode.Append);

            // FS open
            using (FileStream fs = new FileStream("CreateFromFile_test2.txt", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                VerifyCreateFromFileException<IOException>("Loc105a", "CreateFromFile_test2.txt", FileMode.Open);
            }

            // same file - not allowed
            File.WriteAllText("CreateFromFile_test2.txt", fileText);
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile("CreateFromFile_test2.txt"))
            {
                VerifyCreateFromFileException<IOException>("Loc105b", "CreateFromFile_test2.txt", FileMode.Open);
            }


            ////////////////////////////////////////////////////////////////////////
            // CreateFromFile(String, FileMode, String)
            ////////////////////////////////////////////////////////////////////////

            if (File.Exists("CreateFromFile_test1.txt"))
                File.Delete("CreateFromFile_test1.txt");
            if (File.Exists("CreateFromFile_test2.txt"))
                File.Delete("CreateFromFile_test2.txt");
            File.WriteAllText("CreateFromFile_test2.txt", fileText);
            File.WriteAllText("test3.txt", fileText + fileText);

            // [] fileName

            // null fileName
            VerifyCreateFromFileException<ArgumentNullException>("Loc201", null, FileMode.Open);

            // existing file - open
            VerifyCreateFromFile("Loc202a", "CreateFromFile_test2.txt", FileMode.Open);
            VerifyCreateFromFile("Loc202b", "CreateFromFile_test2.txt", FileMode.OpenOrCreate);
            // existing file - create
            // can't create new since it exists
            VerifyCreateFromFileException<IOException>("Loc202d", "CreateFromFile_test2.txt", FileMode.CreateNew);
            // newly created file - exception with default capacity
            VerifyCreateFromFileException<ArgumentException>("Loc202c", "CreateFromFile_test2.txt", FileMode.Create);
            VerifyCreateFromFileException<ArgumentException>("Loc202f", "CreateFromFile_test2.txt", FileMode.Truncate);
            // append not allowed
            VerifyCreateFromFileException<ArgumentException>("Loc202e", "CreateFromFile_test2.txt", FileMode.Append);

            // nonexistent file - error
            if (File.Exists("nonexistent.txt"))
                File.Delete("nonexistent.txt");
            VerifyCreateFromFileException<FileNotFoundException>("Loc203a", "nonexistent.txt", FileMode.Open);
            // newly created file - exception with default capacity
            VerifyCreateFromFileException<ArgumentException>("Loc203b", "CreateFromFile_test1.txt", FileMode.OpenOrCreate);
            VerifyCreateFromFileException<ArgumentException>("Loc203c", "CreateFromFile_test1.txt", FileMode.CreateNew);
            VerifyCreateFromFileException<ArgumentException>("Loc203d", "CreateFromFile_test1.txt", FileMode.Create);
            VerifyCreateFromFileException<ArgumentException>("Loc203e", "CreateFromFile_test2.txt", FileMode.Truncate);
            // append not allowed
            VerifyCreateFromFileException<ArgumentException>("Loc203f", "CreateFromFile_test2.txt", FileMode.Append);

            // empty file - exception with default capacity
            using (FileStream fs = new FileStream("CreateFromFile_test1.txt", FileMode.Create))
            {
            }
            VerifyCreateFromFileException<ArgumentException>("Loc204a", "CreateFromFile_test1.txt", FileMode.Open);
            VerifyCreateFromFileException<ArgumentException>("Loc204b", "CreateFromFile_test1.txt", FileMode.OpenOrCreate);
            VerifyCreateFromFileException<ArgumentException>("Loc204c", "CreateFromFile_test1.txt", FileMode.Create);
            VerifyCreateFromFileException<ArgumentException>("Loc204d", "CreateFromFile_test1.txt", FileMode.Truncate);
            // can't create new since it exists
            VerifyCreateFromFileException<IOException>("Loc204e", "CreateFromFile_test1.txt", FileMode.CreateNew);
            // append not allowed
            VerifyCreateFromFileException<ArgumentException>("Loc204f", "CreateFromFile_test1.txt", FileMode.Append);

            // FS open
            using (FileStream fs = new FileStream("CreateFromFile_test2.txt", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                VerifyCreateFromFileException<IOException>("Loc205a", "CreateFromFile_test2.txt", FileMode.Open);
            }

            // same file - not allowed
            File.WriteAllText("CreateFromFile_test2.txt", fileText);
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile("CreateFromFile_test2.txt"))
            {
                VerifyCreateFromFileException<IOException>("Loc205b", "CreateFromFile_test2.txt", FileMode.Open);
            }

            // [] mapName

            // mapname > 260 chars
            VerifyCreateFromFile("Loc211", "CreateFromFile_test2.txt", FileMode.Open, "CreateFromFile2" + new String('a', 1000));

            // null
            VerifyCreateFromFile("Loc212", "CreateFromFile_test2.txt", FileMode.Open, null);

            // empty string disallowed
            VerifyCreateFromFileException<ArgumentException>("Loc213", "CreateFromFile_test2.txt", FileMode.Open, String.Empty);

            // all whitespace
            VerifyCreateFromFile("Loc214", "CreateFromFile_test2.txt", FileMode.Open, "\t \n\u00A0");

            // MMF with this mapname already exists
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile("test3.txt", FileMode.Open, "map215"))
            {
                VerifyCreateFromFileException<IOException>("Loc215", "CreateFromFile_test2.txt", FileMode.Open, "map215");
            }

            // MMF with this mapname existed, but was closed
            VerifyCreateFromFile("Loc216", "CreateFromFile_test2.txt", FileMode.Open, "map215");

            // "global/" prefix
            VerifyCreateFromFile("Loc217", "CreateFromFile_test2.txt", FileMode.Open, "global/mapname");

            // "local/" prefix
            VerifyCreateFromFile("Loc218", "CreateFromFile_test2.txt", FileMode.Open, "local/mapname");


            ////////////////////////////////////////////////////////////////////////
            // CreateFromFile(String, FileMode, String, long)
            ////////////////////////////////////////////////////////////////////////

            // [] fileName

            // null fileName
            VerifyCreateFromFileException<ArgumentNullException>("Loc301", null, FileMode.Open, "CFF_mapname", 0);

            // [] capacity

            // newly created file - exception with default capacity
            if (File.Exists("CreateFromFile_test1.txt"))
                File.Delete("CreateFromFile_test1.txt");
            VerifyCreateFromFileException<ArgumentException>("Loc311", "CreateFromFile_test1.txt", FileMode.CreateNew, "CFF_mapname211", 0);

            // newly created file - valid with >0 capacity
            if (File.Exists("CreateFromFile_test1.txt"))
                File.Delete("CreateFromFile_test1.txt");
            VerifyCreateFromFile("Loc312", "CreateFromFile_test1.txt", FileMode.CreateNew, "CFF_mapname312", 1);

            // existing file, default capacity
            VerifyCreateFromFile("Loc313", "CreateFromFile_test2.txt", FileMode.Open, "CFF_mapname313", 0);

            // existing file, capacity less than file size
            File.WriteAllText("CreateFromFile_test2.txt", fileText);
            VerifyCreateFromFileException<ArgumentOutOfRangeException>("Loc314", "CreateFromFile_test2.txt", FileMode.Open, "CFF_mapname314", 6);

            // existing file, capacity equal to file size
            File.WriteAllText("CreateFromFile_test2.txt", fileText);
            VerifyCreateFromFile("Loc315", "CreateFromFile_test2.txt", FileMode.Open, "CFF_mapname315", fileText.Length);

            // existing file, capacity greater than file size
            File.WriteAllText("CreateFromFile_test2.txt", fileText);
            VerifyCreateFromFile("Loc316", "CreateFromFile_test2.txt", FileMode.Open, "CFF_mapname316", 6000);

            // negative
            VerifyCreateFromFileException<ArgumentOutOfRangeException>("Loc317", "CreateFromFile_test2.txt", FileMode.Open, "CFF_mapname317", -1);

            // negative
            VerifyCreateFromFileException<ArgumentOutOfRangeException>("Loc318", "CreateFromFile_test2.txt", FileMode.Open, "CFF_mapname318", -4096);

            VerifyCreateFromFileException<IOException>("Loc319b", "CreateFromFile_test2.txt", FileMode.Open, "CFF_mapname319", Int64.MaxValue);  // valid but too large


            ////////////////////////////////////////////////////////////////////////
            // CreateFromFile(String, FileMode, String, long, MemoryMappedFileAccess)
            ////////////////////////////////////////////////////////////////////////

            // [] capacity

            // existing file, capacity less than file size, MemoryMappedFileAccess.Read
            File.WriteAllText("CreateFromFile_test2.txt", fileText);
            VerifyCreateFromFileException<ArgumentOutOfRangeException>("Loc414", "CreateFromFile_test2.txt", FileMode.Open, "CFF_mapname414", 6, MemoryMappedFileAccess.Read);

            // existing file, capacity equal to file size, MemoryMappedFileAccess.Read
            File.WriteAllText("CreateFromFile_test2.txt", fileText);
            VerifyCreateFromFile("Loc415", "CreateFromFile_test2.txt", FileMode.Open, "CFF_mapname415", fileText.Length, MemoryMappedFileAccess.Read);

            // existing file, capacity greater than file size, MemoryMappedFileAccess.Read
            File.WriteAllText("CreateFromFile_test2.txt", fileText);
            VerifyCreateFromFileException<ArgumentException>("Loc416", "CreateFromFile_test2.txt", FileMode.Open, "CFF_mapname416", 6000, MemoryMappedFileAccess.Read);

            ////////////////////////////////////////////////////////////////////////
            // CreateFromFile(FileStream, String, long, MemoryMappedFileAccess,
            //    MemoryMappedFileSecurity, HandleInheritability, bool)
            ////////////////////////////////////////////////////////////////////////

            if (File.Exists("CreateFromFile_test1.txt"))
                File.Delete("CreateFromFile_test1.txt");

            File.WriteAllText("CreateFromFile_test2.txt", fileText);
            File.WriteAllText("test3.txt", fileText + fileText);

            // [] fileStream

            // null filestream
            VerifyCreateFromFileException<ArgumentNullException>("Loc401", null, "map401", 4096, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);

            // existing file
            using (FileStream fs = new FileStream("CreateFromFile_test2.txt", FileMode.Open))
            {
                VerifyCreateFromFile("Loc402", fs, "map402", 4096, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // same FS
            using (FileStream fs = new FileStream("CreateFromFile_test2.txt", FileMode.Open))
            {
                using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(fs, "map403a", 8192, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false))
                {
                    VerifyCreateFromFile("Loc403", fs, "map403", 8192, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
                }
            }

            // closed FS
            using (FileStream fs = new FileStream("CreateFromFile_test2.txt", FileMode.Open))
            {
                fs.Dispose();
                VerifyCreateFromFileException<ObjectDisposedException>("Loc404", fs, "map404", 4096, MemoryMappedFileAccess.Read, HandleInheritability.None, false);
            }

            // newly created file - exception with default capacity
            using (FileStream fs = new FileStream("CreateFromFile_test1.txt", FileMode.CreateNew))
            {
                VerifyCreateFromFileException<ArgumentException>("Loc405", fs, "map405", 0, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // empty file - exception with default capacity
            using (FileStream fs = new FileStream("CreateFromFile_test1.txt", FileMode.Open))
            {
                VerifyCreateFromFileException<ArgumentException>("Loc406", fs, "map406", 0, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // [] mapName

            // mapname > 260 chars
            File.WriteAllText("CreateFromFile_test2.txt", fileText);
            using (FileStream fs = new FileStream("CreateFromFile_test2.txt", FileMode.Open))
            {
                VerifyCreateFromFile("Loc411", fs, "CreateFromFile" + new String('a', 1000), 4096, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // null
            File.WriteAllText("CreateFromFile_test2.txt", fileText);
            using (FileStream fs = new FileStream("CreateFromFile_test2.txt", FileMode.Open))
            {
                VerifyCreateFromFile("Loc412", fs, null, 4096, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // empty string disallowed
            using (FileStream fs = new FileStream("CreateFromFile_test2.txt", FileMode.Open))
            {
                VerifyCreateFromFileException<ArgumentException>("Loc413", fs, String.Empty, 4096, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // all whitespace
            File.WriteAllText("CreateFromFile_test2.txt", fileText);
            using (FileStream fs = new FileStream("CreateFromFile_test2.txt", FileMode.Open))
            {
                VerifyCreateFromFile("Loc414", fs, "\t \n\u00A0", 4096, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // MMF with this mapname already exists
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile("test3.txt", FileMode.Open, "map415"))
            {
                using (FileStream fs2 = new FileStream("CreateFromFile_test2.txt", FileMode.Open))
                {
                    VerifyCreateFromFileException<IOException>("Loc415", fs2, "map415", 4096, MemoryMappedFileAccess.Read, HandleInheritability.None, false);
                }
            }

            // MMF with this mapname existed, but was closed
            using (FileStream fs = new FileStream("CreateFromFile_test2.txt", FileMode.Open))
            {
                VerifyCreateFromFile("Loc416", fs, "map415", 4096, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // "global/" prefix
            using (FileStream fs = new FileStream("CreateFromFile_test2.txt", FileMode.Open))
            {
                VerifyCreateFromFile("Loc417", fs, "global/mapname", 4096, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // "local/" prefix
            using (FileStream fs = new FileStream("CreateFromFile_test2.txt", FileMode.Open))
            {
                VerifyCreateFromFile("Loc418", fs, "local/mapname", 4096, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // [] capacity

            // newly created file - exception with default capacity
            if (File.Exists("CreateFromFile_test1.txt"))
                File.Delete("CreateFromFile_test1.txt");
            using (FileStream fs = new FileStream("CreateFromFile_test1.txt", FileMode.CreateNew))
            {
                VerifyCreateFromFileException<ArgumentException>("Loc421", fs, "CFF_mapname421", 0, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // newly created file - valid with >0 capacity
            if (File.Exists("CreateFromFile_test1.txt"))
                File.Delete("CreateFromFile_test1.txt");
            using (FileStream fs = new FileStream("CreateFromFile_test1.txt", FileMode.CreateNew))
            {
                VerifyCreateFromFile("Loc422", fs, "CFF_mapname422", 1, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // existing file, default capacity
            using (FileStream fs = new FileStream("CreateFromFile_test2.txt", FileMode.Open))
            {
                VerifyCreateFromFile("Loc423", fs, "CFF_mapname423", 0, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // existing file, capacity less than file size
            File.WriteAllText("CreateFromFile_test2.txt", fileText);
            using (FileStream fs = new FileStream("CreateFromFile_test2.txt", FileMode.Open))
            {
                VerifyCreateFromFileException<ArgumentOutOfRangeException>("Loc424", fs, "CFF_mapname424", 6, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // existing file, capacity equal to file size
            File.WriteAllText("CreateFromFile_test2.txt", fileText);
            using (FileStream fs = new FileStream("CreateFromFile_test2.txt", FileMode.Open))
            {
                VerifyCreateFromFile("Loc425", fs, "CFF_mapname425", fileText.Length, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // existing file, capacity greater than file size
            File.WriteAllText("CreateFromFile_test2.txt", fileText);
            using (FileStream fs = new FileStream("CreateFromFile_test2.txt", FileMode.Open))
            {
                VerifyCreateFromFile("Loc426", fs, "CFF_mapname426", 6000, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // existing file, capacity greater than file size & access = Read only
            File.WriteAllText("CreateFromFile_test2.txt", fileText);
            using (FileStream fs = new FileStream("CreateFromFile_test2.txt", FileMode.Open))
            {
                VerifyCreateFromFileException<ArgumentException>("Loc426a", fs, "CFF_mapname426a", 6000, MemoryMappedFileAccess.Read, HandleInheritability.None, false);
            }

            // negative
            using (FileStream fs = new FileStream("CreateFromFile_test2.txt", FileMode.Open))
            {
                VerifyCreateFromFileException<ArgumentOutOfRangeException>("Loc427", fs, "CFF_mapname427", -1, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // negative
            using (FileStream fs = new FileStream("CreateFromFile_test2.txt", FileMode.Open))
            {
                VerifyCreateFromFileException<ArgumentOutOfRangeException>("Loc428", fs, "CFF_mapname428", -4096, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // Int64.MaxValue - cannot exceed local address space
            using (FileStream fs = new FileStream("CreateFromFile_test2.txt", FileMode.Open))
            {
                VerifyCreateFromFileException<IOException>("Loc429", fs, "CFF_mapname429", Int64.MaxValue, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);  // valid but too large
            }

            // [] access

            // Write is disallowed
            using (FileStream fs = new FileStream("CreateFromFile_test2.txt", FileMode.Open, FileAccess.ReadWrite))
            {
                VerifyCreateFromFileException<ArgumentException>("Loc430", fs, "CFF_mapname430", 0, MemoryMappedFileAccess.Write, HandleInheritability.None, false);
            }

            // valid access (filestream is ReadWrite)
            MemoryMappedFileAccess[] accessList = new MemoryMappedFileAccess[] {
                MemoryMappedFileAccess.Read,
                MemoryMappedFileAccess.ReadWrite,
                MemoryMappedFileAccess.CopyOnWrite,
            };
            foreach (MemoryMappedFileAccess access in accessList)
            {
                using (FileStream fs = new FileStream("CreateFromFile_test2.txt", FileMode.Open, FileAccess.ReadWrite))
                {
                    VerifyCreateFromFile("Loc431_" + access, fs, "CFF_mapname431_" + access, 0, access, HandleInheritability.None, false);
                }
            }

            // invalid access (filestream is ReadWrite)
            accessList = new MemoryMappedFileAccess[] {
                MemoryMappedFileAccess.ReadExecute,
                MemoryMappedFileAccess.ReadWriteExecute,
            };
            foreach (MemoryMappedFileAccess access in accessList)
            {
                using (FileStream fs = new FileStream("CreateFromFile_test2.txt", FileMode.Open, FileAccess.ReadWrite))
                {
                    VerifyCreateFromFileException<UnauthorizedAccessException>("Loc432_" + access, fs, "CFF_mapname432_" + access, 0, access, HandleInheritability.None, false);
                }
            }

            // valid access (filestream is Read only)
            accessList = new MemoryMappedFileAccess[] {
                MemoryMappedFileAccess.Read,
                MemoryMappedFileAccess.CopyOnWrite,
            };
            foreach (MemoryMappedFileAccess access in accessList)
            {
                using (FileStream fs = new FileStream("CreateFromFile_test2.txt", FileMode.Open, FileAccess.Read))
                {
                    VerifyCreateFromFile("Loc433_" + access, fs, "CFF_mapname433_" + access, 0, access, HandleInheritability.None, false);
                }
            }

            // invalid access (filestream was Read only)
            accessList = new MemoryMappedFileAccess[] {
                MemoryMappedFileAccess.ReadWrite,
                MemoryMappedFileAccess.ReadExecute,
                MemoryMappedFileAccess.ReadWriteExecute,
            };
            foreach (MemoryMappedFileAccess access in accessList)
            {
                using (FileStream fs = new FileStream("CreateFromFile_test2.txt", FileMode.Open, FileAccess.Read))
                {
                    VerifyCreateFromFileException<UnauthorizedAccessException>("Loc434_" + access, fs, "CFF_mapname434_" + access, 0, access, HandleInheritability.None, false);
                }
            }

            // invalid access (filestream is Write only)
            accessList = new MemoryMappedFileAccess[] {
                MemoryMappedFileAccess.Read,
                MemoryMappedFileAccess.ReadWrite,
                MemoryMappedFileAccess.CopyOnWrite,
                MemoryMappedFileAccess.ReadExecute,
                MemoryMappedFileAccess.ReadWriteExecute,
            };
            foreach (MemoryMappedFileAccess access in accessList)
            {
                using (FileStream fs = new FileStream("CreateFromFile_test2.txt", FileMode.Open, FileAccess.Write))
                {
                    VerifyCreateFromFileException<UnauthorizedAccessException>("Loc435_" + access, fs, "CFF_mapname435_" + access, 0, access, HandleInheritability.None, false);
                }
            }

            // invalid enum value
            accessList = new MemoryMappedFileAccess[] {
                (MemoryMappedFileAccess)(-1),
                (MemoryMappedFileAccess)(6),
            };
            foreach (MemoryMappedFileAccess access in accessList)
            {
                using (FileStream fs = new FileStream("CreateFromFile_test2.txt", FileMode.Open, FileAccess.ReadWrite))
                {
                    VerifyCreateFromFileException<ArgumentOutOfRangeException>("Loc436_" + ((int)access), fs, "CFF_mapname436_" + ((int)access), 0, access, HandleInheritability.None, false);
                }
            }

            // [] inheritability

            // None
            using (FileStream fs = new FileStream("CreateFromFile_test2.txt", FileMode.Open))
            {
                VerifyCreateFromFile("Loc461", fs, "CFF_mapname461", 0, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // Inheritable
            using (FileStream fs = new FileStream("CreateFromFile_test2.txt", FileMode.Open))
            {
                VerifyCreateFromFile("Loc462", fs, "CFF_mapname462", 0, MemoryMappedFileAccess.ReadWrite, HandleInheritability.Inheritable, false);
            }

            // invalid
            using (FileStream fs = new FileStream("CreateFromFile_test2.txt", FileMode.Open))
            {
                VerifyCreateFromFileException<ArgumentOutOfRangeException>("Loc463", fs, "CFF_mapname463", 0, MemoryMappedFileAccess.ReadWrite, (HandleInheritability)(-1), false);
            }
            using (FileStream fs = new FileStream("CreateFromFile_test2.txt", FileMode.Open))
            {
                VerifyCreateFromFileException<ArgumentOutOfRangeException>("Loc464", fs, "CFF_mapname464", 0, MemoryMappedFileAccess.ReadWrite, (HandleInheritability)(2), false);
            }

            // [] leaveOpen

            // false
            using (FileStream fs = new FileStream("CreateFromFile_test2.txt", FileMode.Open))
            {
                VerifyCreateFromFile("Loc471", fs, "CFF_mapname471", 0, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // true
            using (FileStream fs = new FileStream("CreateFromFile_test2.txt", FileMode.Open))
            {
                VerifyCreateFromFile("Loc472", fs, "CFF_mapname472", 0, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true);
            }

            /// END TEST CASES

            if (iCountErrors == 0)
            {
                return true;
            }
            else
            {
                Console.WriteLine("Fail: iCountErrors==" + iCountErrors);
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERR999: Unexpected exception in runTest, {0}", ex);
            return false;
        }
    }

    /// START HELPER FUNCTIONS
    public void VerifyCreateFromFile(String strLoc, String fileName)
    {
        iCountTestcases++;
        try
        {
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(fileName))
            {
                VerifyAccess(strLoc, mmf, MemoryMappedFileAccess.ReadWrite, 1);
                VerifyHandleInheritability(strLoc, mmf.SafeMemoryMappedFileHandle, HandleInheritability.None);
            }
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: Unexpected exception, {1}", strLoc, ex);
        }
    }

    public void VerifyCreateFromFileException<EXCTYPE>(String strLoc, String fileName) where EXCTYPE : Exception
    {
        iCountTestcases++;
        try
        {
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(fileName))
            {
                iCountErrors++;
                Console.WriteLine("ERROR, {0}: No exception thrown, expected {1}", strLoc, typeof(EXCTYPE));
            }
        }
        catch (EXCTYPE)
        {
            //Console.WriteLine("{0}: Expected, {1}: {2}", strLoc, ex.GetType(), ex.Message);
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: Unexpected exception, {1}", strLoc, ex);
        }
    }

    public void VerifyCreateFromFile(String strLoc, String fileName, FileMode fileMode)
    {
        iCountTestcases++;
        try
        {
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(fileName, fileMode))
            {
                VerifyAccess(strLoc, mmf, MemoryMappedFileAccess.ReadWrite, 1);
                VerifyHandleInheritability(strLoc, mmf.SafeMemoryMappedFileHandle, HandleInheritability.None);
            }
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: Unexpected exception, {1}", strLoc, ex);
        }
    }

    public void VerifyCreateFromFileException<EXCTYPE>(String strLoc, String fileName, FileMode fileMode) where EXCTYPE : Exception
    {
        iCountTestcases++;
        try
        {
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(fileName, fileMode))
            {
                iCountErrors++;
                Console.WriteLine("ERROR, {0}: No exception thrown, expected {1}", strLoc, typeof(EXCTYPE));
            }
        }
        catch (EXCTYPE)
        {
            //Console.WriteLine("{0}: Expected, {1}: {2}", strLoc, ex.GetType(), ex.Message);
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: Unexpected exception, {1}", strLoc, ex);
        }
    }

    public void VerifyCreateFromFile(String strLoc, String fileName, FileMode fileMode, String mapName)
    {
        iCountTestcases++;
        try
        {
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(fileName, fileMode, mapName))
            {
                VerifyAccess(strLoc, mmf, MemoryMappedFileAccess.ReadWrite, 1);
                VerifyHandleInheritability(strLoc, mmf.SafeMemoryMappedFileHandle, HandleInheritability.None);
            }
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: Unexpected exception, {1}", strLoc, ex);
        }
    }

    public void VerifyCreateFromFileException<EXCTYPE>(String strLoc, String fileName, FileMode fileMode, String mapName) where EXCTYPE : Exception
    {
        iCountTestcases++;
        try
        {
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(fileName, fileMode, mapName))
            {
                iCountErrors++;
                Console.WriteLine("ERROR, {0}: No exception thrown, expected {1}", strLoc, typeof(EXCTYPE));
            }
        }
        catch (EXCTYPE)
        {
            //Console.WriteLine("{0}: Expected, {1}: {2}", strLoc, ex.GetType(), ex.Message);
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: Unexpected exception, {1}", strLoc, ex);
        }
    }

    public void VerifyCreateFromFile(String strLoc, String fileName, FileMode fileMode, String mapName, long capacity)
    {
        iCountTestcases++;
        try
        {
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(fileName, fileMode, mapName, capacity))
            {
                VerifyAccess(strLoc, mmf, MemoryMappedFileAccess.ReadWrite, capacity);
                VerifyHandleInheritability(strLoc, mmf.SafeMemoryMappedFileHandle, HandleInheritability.None);
            }
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: Unexpected exception, {1}", strLoc, ex);
        }
    }

    public void VerifyCreateFromFileException<EXCTYPE>(String strLoc, String fileName, FileMode fileMode, String mapName, long capacity) where EXCTYPE : Exception
    {
        iCountTestcases++;
        try
        {
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(fileName, fileMode, mapName, capacity))
            {
                iCountErrors++;
                Console.WriteLine("ERROR, {0}: No exception thrown, expected {1}", strLoc, typeof(EXCTYPE));
            }
        }
        catch (EXCTYPE)
        {
            //Console.WriteLine("{0}: Expected, {1}: {2}", strLoc, ex.GetType(), ex.Message);
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: Unexpected exception, {1}", strLoc, ex);
        }
    }

    public void VerifyCreateFromFile(String strLoc, String fileName, FileMode fileMode, String mapName, long capacity, MemoryMappedFileAccess access)
    {
        iCountTestcases++;
        try
        {
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(fileName, fileMode, mapName, capacity, access))
            {
                VerifyAccess(strLoc, mmf, access, capacity);
                VerifyHandleInheritability(strLoc, mmf.SafeMemoryMappedFileHandle, HandleInheritability.None);
            }
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: Unexpected exception, {1}", strLoc, ex);
        }
    }

    public void VerifyCreateFromFileException<EXCTYPE>(String strLoc, String fileName, FileMode fileMode, String mapName, long capacity, MemoryMappedFileAccess access) where EXCTYPE : Exception
    {
        iCountTestcases++;
        try
        {
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(fileName, fileMode, mapName, capacity, access))
            {
                iCountErrors++;
                Console.WriteLine("ERROR, {0}: No exception thrown, expected {1}", strLoc, typeof(EXCTYPE));
            }
        }
        catch (EXCTYPE)
        {
            //Console.WriteLine("{0}: Expected, {1}: {2}", strLoc, ex.GetType(), ex.Message);
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: Unexpected exception, {1}", strLoc, ex);
        }
    }

    public void VerifyCreateFromFile(String strLoc, FileStream fileStream, String mapName, long capacity, MemoryMappedFileAccess access, HandleInheritability inheritability, bool leaveOpen)
    {
        iCountTestcases++;
        try
        {
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(fileStream, mapName, capacity, access, inheritability, leaveOpen))
            {
                VerifyAccess(strLoc, mmf, access, capacity);
                VerifyHandleInheritability(strLoc, mmf.SafeMemoryMappedFileHandle, inheritability);
            }
            VerifyLeaveOpen(strLoc, fileStream, leaveOpen);
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: Unexpected exception, {1}", strLoc, ex);
        }
    }

    public void VerifyCreateFromFileException<EXCTYPE>(String strLoc, FileStream fileStream, String mapName, long capacity, MemoryMappedFileAccess access, HandleInheritability inheritability, bool leaveOpen) where EXCTYPE : Exception
    {
        iCountTestcases++;
        try
        {
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(fileStream, mapName, capacity, access, inheritability, leaveOpen))
            {
                iCountErrors++;
                Console.WriteLine("ERROR, {0}: No exception thrown, expected {1}", strLoc, typeof(EXCTYPE));
            }
        }
        catch (EXCTYPE)
        {
            //Console.WriteLine("{0}: Expected, {1}: {2}", strLoc, ex.GetType(), ex.Message);
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: Unexpected exception, {1}", strLoc, ex);
        }
    }

    void VerifyLeaveOpen(String strLoc, FileStream fs, bool expectedOpen)
    {
        if (fs.CanSeek != expectedOpen)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: MemoryMappedFile did not respect LeaveOpen.  Expected FileStream open={1}, got open={2}", strLoc, expectedOpen, fs.CanSeek);
        }
    }
}
