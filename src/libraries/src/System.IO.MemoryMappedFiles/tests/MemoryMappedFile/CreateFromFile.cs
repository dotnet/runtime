// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using Xunit;

[Collection("CreateFromFile")]
public class CreateFromFile : MMFTestBase
{
    private readonly static string s_uniquifier = Guid.NewGuid().ToString();
    private readonly static string s_fileNameTest1 = "CreateFromFile_test1_" + s_uniquifier + ".txt";
    private readonly static string s_fileNameTest2 = "CreateFromFile_test2_" + s_uniquifier + ".txt";
    private readonly static string s_fileNameTest3 = "CreateFromFile_test3_" + s_uniquifier + ".txt";
    private readonly static string s_fileNameNonexistent = "CreateFromFile_nonexistent_" + s_uniquifier + ".txt";

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
            if (File.Exists(s_fileNameTest1))
                File.Delete(s_fileNameTest1);

            if (File.Exists(s_fileNameTest2))
                File.Delete(s_fileNameTest2);
            String fileText = "Non-empty file for MMF testing.";
            File.WriteAllText(s_fileNameTest2, fileText);

            ////////////////////////////////////////////////////////////////////////
            // CreateFromFile(String)
            ////////////////////////////////////////////////////////////////////////

            // [] fileName

            // null fileName
            VerifyCreateFromFileException<ArgumentNullException>("Loc001", null);

            // existing file
            VerifyCreateFromFile("Loc002", s_fileNameTest2);

            // nonexistent file
            if (File.Exists(s_fileNameNonexistent))
                File.Delete(s_fileNameNonexistent);
            VerifyCreateFromFileException<FileNotFoundException>("Loc003", s_fileNameNonexistent);

            // FS open
            using (FileStream fs = new FileStream(s_fileNameTest2, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                VerifyCreateFromFileException<IOException>("Loc004a", s_fileNameTest2);
            }

            // same file - not allowed
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(s_fileNameTest2))
            {
                VerifyCreateFromFileException<IOException>("Loc004b", s_fileNameTest2);
            }

            ////////////////////////////////////////////////////////////////////////
            // CreateFromFile(String, FileMode)
            ////////////////////////////////////////////////////////////////////////

            if (File.Exists(s_fileNameTest1))
                File.Delete(s_fileNameTest1);
            if (File.Exists(s_fileNameTest2))
                File.Delete(s_fileNameTest2);
            File.WriteAllText(s_fileNameTest2, fileText);

            // [] fileName

            // null fileName
            VerifyCreateFromFileException<ArgumentNullException>("Loc101", null, FileMode.Open);

            // existing file - open
            VerifyCreateFromFile("Loc102a", s_fileNameTest2, FileMode.Open);
            VerifyCreateFromFile("Loc102b", s_fileNameTest2, FileMode.OpenOrCreate);
            // existing file - create
            // can't create new since it exists
            VerifyCreateFromFileException<IOException>("Loc102d", s_fileNameTest2, FileMode.CreateNew);
            // newly created file - exception with default capacity
            VerifyCreateFromFileException<ArgumentException>("Loc102c", s_fileNameTest2, FileMode.Create);
            VerifyCreateFromFileException<ArgumentException>("Loc102f", s_fileNameTest2, FileMode.Truncate);
            // append not allowed
            VerifyCreateFromFileException<ArgumentException>("Loc102e", s_fileNameTest2, FileMode.Append);

            // nonexistent file - error
            if (File.Exists(s_fileNameNonexistent))
                File.Delete(s_fileNameNonexistent);
            VerifyCreateFromFileException<FileNotFoundException>("Loc103a", s_fileNameNonexistent, FileMode.Open);
            // newly created file - exception with default capacity
            VerifyCreateFromFileException<ArgumentException>("Loc103b", s_fileNameTest1, FileMode.OpenOrCreate);
            VerifyCreateFromFileException<ArgumentException>("Loc103c", s_fileNameTest1, FileMode.CreateNew);
            VerifyCreateFromFileException<ArgumentException>("Loc103d", s_fileNameTest1, FileMode.Create);
            VerifyCreateFromFileException<ArgumentException>("Loc103e", s_fileNameTest2, FileMode.Truncate);
            // append not allowed
            VerifyCreateFromFileException<ArgumentException>("Loc103f", s_fileNameTest2, FileMode.Append);

            // empty file - exception with default capacity
            using (FileStream fs = new FileStream(s_fileNameTest1, FileMode.Create))
            {
            }
            VerifyCreateFromFileException<ArgumentException>("Loc104a", s_fileNameTest1, FileMode.Open);
            VerifyCreateFromFileException<ArgumentException>("Loc104b", s_fileNameTest1, FileMode.OpenOrCreate);
            VerifyCreateFromFileException<ArgumentException>("Loc104c", s_fileNameTest1, FileMode.Create);
            VerifyCreateFromFileException<ArgumentException>("Loc104d", s_fileNameTest1, FileMode.Truncate);
            // can't create new since it exists
            VerifyCreateFromFileException<IOException>("Loc104e", s_fileNameTest1, FileMode.CreateNew);
            // append not allowed
            VerifyCreateFromFileException<ArgumentException>("Loc104f", s_fileNameTest1, FileMode.Append);

            // FS open
            using (FileStream fs = new FileStream(s_fileNameTest2, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                VerifyCreateFromFileException<IOException>("Loc105a", s_fileNameTest2, FileMode.Open);
            }

            // same file - not allowed
            File.WriteAllText(s_fileNameTest2, fileText);
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(s_fileNameTest2))
            {
                VerifyCreateFromFileException<IOException>("Loc105b", s_fileNameTest2, FileMode.Open);
            }


            ////////////////////////////////////////////////////////////////////////
            // CreateFromFile(String, FileMode, String)
            ////////////////////////////////////////////////////////////////////////

            if (File.Exists(s_fileNameTest1))
                File.Delete(s_fileNameTest1);
            if (File.Exists(s_fileNameTest2))
                File.Delete(s_fileNameTest2);
            File.WriteAllText(s_fileNameTest2, fileText);
            File.WriteAllText(s_fileNameTest3, fileText + fileText);

            // [] fileName

            // null fileName
            VerifyCreateFromFileException<ArgumentNullException>("Loc201", null, FileMode.Open);

            // existing file - open
            VerifyCreateFromFile("Loc202a", s_fileNameTest2, FileMode.Open);
            VerifyCreateFromFile("Loc202b", s_fileNameTest2, FileMode.OpenOrCreate);
            // existing file - create
            // can't create new since it exists
            VerifyCreateFromFileException<IOException>("Loc202d", s_fileNameTest2, FileMode.CreateNew);
            // newly created file - exception with default capacity
            VerifyCreateFromFileException<ArgumentException>("Loc202c", s_fileNameTest2, FileMode.Create);
            VerifyCreateFromFileException<ArgumentException>("Loc202f", s_fileNameTest2, FileMode.Truncate);
            // append not allowed
            VerifyCreateFromFileException<ArgumentException>("Loc202e", s_fileNameTest2, FileMode.Append);

            // nonexistent file - error
            if (File.Exists(s_fileNameNonexistent))
                File.Delete(s_fileNameNonexistent);
            VerifyCreateFromFileException<FileNotFoundException>("Loc203a", s_fileNameNonexistent, FileMode.Open);
            // newly created file - exception with default capacity
            VerifyCreateFromFileException<ArgumentException>("Loc203b", s_fileNameTest1, FileMode.OpenOrCreate);
            VerifyCreateFromFileException<ArgumentException>("Loc203c", s_fileNameTest1, FileMode.CreateNew);
            VerifyCreateFromFileException<ArgumentException>("Loc203d", s_fileNameTest1, FileMode.Create);
            VerifyCreateFromFileException<ArgumentException>("Loc203e", s_fileNameTest2, FileMode.Truncate);
            // append not allowed
            VerifyCreateFromFileException<ArgumentException>("Loc203f", s_fileNameTest2, FileMode.Append);

            // empty file - exception with default capacity
            using (FileStream fs = new FileStream(s_fileNameTest1, FileMode.Create))
            {
            }
            VerifyCreateFromFileException<ArgumentException>("Loc204a", s_fileNameTest1, FileMode.Open);
            VerifyCreateFromFileException<ArgumentException>("Loc204b", s_fileNameTest1, FileMode.OpenOrCreate);
            VerifyCreateFromFileException<ArgumentException>("Loc204c", s_fileNameTest1, FileMode.Create);
            VerifyCreateFromFileException<ArgumentException>("Loc204d", s_fileNameTest1, FileMode.Truncate);
            // can't create new since it exists
            VerifyCreateFromFileException<IOException>("Loc204e", s_fileNameTest1, FileMode.CreateNew);
            // append not allowed
            VerifyCreateFromFileException<ArgumentException>("Loc204f", s_fileNameTest1, FileMode.Append);

            // FS open
            using (FileStream fs = new FileStream(s_fileNameTest2, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                VerifyCreateFromFileException<IOException>("Loc205a", s_fileNameTest2, FileMode.Open);
            }

            // same file - not allowed
            File.WriteAllText(s_fileNameTest2, fileText);
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(s_fileNameTest2))
            {
                VerifyCreateFromFileException<IOException>("Loc205b", s_fileNameTest2, FileMode.Open);
            }

            // [] mapName

            // mapname > 260 chars
            VerifyCreateFromFile("Loc211", s_fileNameTest2, FileMode.Open, "CreateFromFile2" + new String('a', 1000) + s_uniquifier);

            // null
            VerifyCreateFromFile("Loc212", s_fileNameTest2, FileMode.Open, null);

            // empty string disallowed
            VerifyCreateFromFileException<ArgumentException>("Loc213", s_fileNameTest2, FileMode.Open, String.Empty);

            // all whitespace
            VerifyCreateFromFile("Loc214", s_fileNameTest2, FileMode.Open, "\t \n\u00A0");

            // MMF with this mapname already exists
            if (Interop.IsWindows) // named maps not supported on Unix
            {
                using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(s_fileNameTest3, FileMode.Open, "map215" + s_uniquifier))
                {
                    VerifyCreateFromFileException<IOException>("Loc215", s_fileNameTest2, FileMode.Open, "map215" + s_uniquifier);
                }
            }

            // MMF with this mapname existed, but was closed
            VerifyCreateFromFile("Loc216", s_fileNameTest2, FileMode.Open, "map215" + s_uniquifier);

            // "global/" prefix
            VerifyCreateFromFile("Loc217", s_fileNameTest2, FileMode.Open, "global/CFF_0" + s_uniquifier);

            // "local/" prefix
            VerifyCreateFromFile("Loc218", s_fileNameTest2, FileMode.Open, "local/CFF_1" + s_uniquifier);


            ////////////////////////////////////////////////////////////////////////
            // CreateFromFile(String, FileMode, String, long)
            ////////////////////////////////////////////////////////////////////////

            // [] fileName

            // null fileName
            VerifyCreateFromFileException<ArgumentNullException>("Loc301", null, FileMode.Open, "CFF_mapname" + s_uniquifier, 0);

            // [] capacity

            // newly created file - exception with default capacity
            if (File.Exists(s_fileNameTest1))
                File.Delete(s_fileNameTest1);
            VerifyCreateFromFileException<ArgumentException>("Loc311", s_fileNameTest1, FileMode.CreateNew, "CFF_mapname211" + s_uniquifier, 0);

            // newly created file - valid with >0 capacity
            if (File.Exists(s_fileNameTest1))
                File.Delete(s_fileNameTest1);
            VerifyCreateFromFile("Loc312", s_fileNameTest1, FileMode.CreateNew, "CFF_mapname312" + s_uniquifier, 1);

            // existing file, default capacity
            VerifyCreateFromFile("Loc313", s_fileNameTest2, FileMode.Open, "CFF_mapname313" + s_uniquifier, 0);

            // existing file, capacity less than file size
            File.WriteAllText(s_fileNameTest2, fileText);
            VerifyCreateFromFileException<ArgumentOutOfRangeException>("Loc314", s_fileNameTest2, FileMode.Open, "CFF_mapname314" + s_uniquifier, 6);

            // existing file, capacity equal to file size
            File.WriteAllText(s_fileNameTest2, fileText);
            VerifyCreateFromFile("Loc315", s_fileNameTest2, FileMode.Open, "CFF_mapname315" + s_uniquifier, fileText.Length);

            // existing file, capacity greater than file size
            File.WriteAllText(s_fileNameTest2, fileText);
            VerifyCreateFromFile("Loc316", s_fileNameTest2, FileMode.Open, "CFF_mapname316" + s_uniquifier, 6000);

            // negative
            VerifyCreateFromFileException<ArgumentOutOfRangeException>("Loc317", s_fileNameTest2, FileMode.Open, "CFF_mapname317" + s_uniquifier, -1);

            // negative
            VerifyCreateFromFileException<ArgumentOutOfRangeException>("Loc318", s_fileNameTest2, FileMode.Open, "CFF_mapname318" + s_uniquifier, -4096);

            VerifyCreateFromFileException<IOException>("Loc319b", s_fileNameTest2, FileMode.Open, "CFF_mapname319" + s_uniquifier, Int64.MaxValue);  // valid but too large


            ////////////////////////////////////////////////////////////////////////
            // CreateFromFile(String, FileMode, String, long, MemoryMappedFileAccess)
            ////////////////////////////////////////////////////////////////////////

            // [] capacity

            // existing file, capacity less than file size, MemoryMappedFileAccess.Read
            File.WriteAllText(s_fileNameTest2, fileText);
            VerifyCreateFromFileException<ArgumentOutOfRangeException>("Loc414", s_fileNameTest2, FileMode.Open, "CFF_mapname414" + s_uniquifier, 6, MemoryMappedFileAccess.Read);

            // existing file, capacity equal to file size, MemoryMappedFileAccess.Read
            File.WriteAllText(s_fileNameTest2, fileText);
            VerifyCreateFromFile("Loc415", s_fileNameTest2, FileMode.Open, "CFF_mapname415" + s_uniquifier, fileText.Length, MemoryMappedFileAccess.Read);

            // existing file, capacity greater than file size, MemoryMappedFileAccess.Read
            File.WriteAllText(s_fileNameTest2, fileText);
            VerifyCreateFromFileException<ArgumentException>("Loc416", s_fileNameTest2, FileMode.Open, "CFF_mapname416" + s_uniquifier, 6000, MemoryMappedFileAccess.Read);

            ////////////////////////////////////////////////////////////////////////
            // CreateFromFile(FileStream, String, long, MemoryMappedFileAccess,
            //    MemoryMappedFileSecurity, HandleInheritability, bool)
            ////////////////////////////////////////////////////////////////////////

            if (File.Exists(s_fileNameTest1))
                File.Delete(s_fileNameTest1);

            File.WriteAllText(s_fileNameTest2, fileText);
            File.WriteAllText(s_fileNameTest3, fileText + fileText);

            // [] fileStream

            // null filestream
            VerifyCreateFromFileException<ArgumentNullException>("Loc401", null, "map401" + s_uniquifier, 4096, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);

            // existing file
            using (FileStream fs = new FileStream(s_fileNameTest2, FileMode.Open))
            {
                VerifyCreateFromFile("Loc402", fs, "map402" + s_uniquifier, 4096, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            if (Interop.IsWindows) // named maps not supported on Unix
            {
                // same FS
                using (FileStream fs = new FileStream(s_fileNameTest2, FileMode.Open))
                {
                    using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(fs, "map403a" + s_uniquifier, 8192, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false))
                    {
                        VerifyCreateFromFile("Loc403", fs, "map403" + s_uniquifier, 8192, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
                    }
                }
            }

            // closed FS
            using (FileStream fs = new FileStream(s_fileNameTest2, FileMode.Open))
            {
                fs.Dispose();
                VerifyCreateFromFileException<ObjectDisposedException>("Loc404", fs, "map404" + s_uniquifier, 4096, MemoryMappedFileAccess.Read, HandleInheritability.None, false);
            }

            // newly created file - exception with default capacity
            using (FileStream fs = new FileStream(s_fileNameTest1, FileMode.CreateNew))
            {
                VerifyCreateFromFileException<ArgumentException>("Loc405", fs, "map405" + s_uniquifier, 0, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // empty file - exception with default capacity
            using (FileStream fs = new FileStream(s_fileNameTest1, FileMode.Open))
            {
                VerifyCreateFromFileException<ArgumentException>("Loc406", fs, "map406" + s_uniquifier, 0, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // [] mapName

            // mapname > 260 chars
            File.WriteAllText(s_fileNameTest2, fileText);
            using (FileStream fs = new FileStream(s_fileNameTest2, FileMode.Open))
            {
                VerifyCreateFromFile("Loc411", fs, "CreateFromFile" + new String('a', 1000) + s_uniquifier, 4096, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // null
            File.WriteAllText(s_fileNameTest2, fileText);
            using (FileStream fs = new FileStream(s_fileNameTest2, FileMode.Open))
            {
                VerifyCreateFromFile("Loc412", fs, null, 4096, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // empty string disallowed
            using (FileStream fs = new FileStream(s_fileNameTest2, FileMode.Open))
            {
                VerifyCreateFromFileException<ArgumentException>("Loc413", fs, String.Empty, 4096, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // all whitespace
            File.WriteAllText(s_fileNameTest2, fileText);
            using (FileStream fs = new FileStream(s_fileNameTest2, FileMode.Open))
            {
                VerifyCreateFromFile("Loc414", fs, "\t \n\u00A0", 4096, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            if (Interop.IsWindows) // named maps not supported on Unix
            {
                // MMF with this mapname already exists
                using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(s_fileNameTest3, FileMode.Open, "map415" + s_uniquifier))
                {
                    using (FileStream fs2 = new FileStream(s_fileNameTest2, FileMode.Open))
                    {
                        VerifyCreateFromFileException<IOException>("Loc415", fs2, "map415" + s_uniquifier, 4096, MemoryMappedFileAccess.Read, HandleInheritability.None, false);
                    }
                }
            }

            // MMF with this mapname existed, but was closed
            using (FileStream fs = new FileStream(s_fileNameTest2, FileMode.Open))
            {
                VerifyCreateFromFile("Loc416", fs, "map415" + s_uniquifier, 4096, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // "global/" prefix
            using (FileStream fs = new FileStream(s_fileNameTest2, FileMode.Open))
            {
                VerifyCreateFromFile("Loc417", fs, "global/CFF_2" + s_uniquifier, 4096, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // "local/" prefix
            using (FileStream fs = new FileStream(s_fileNameTest2, FileMode.Open))
            {
                VerifyCreateFromFile("Loc418", fs, "local/CFF_3" + s_uniquifier, 4096, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // [] capacity

            // newly created file - exception with default capacity
            if (File.Exists(s_fileNameTest1))
                File.Delete(s_fileNameTest1);
            using (FileStream fs = new FileStream(s_fileNameTest1, FileMode.CreateNew))
            {
                VerifyCreateFromFileException<ArgumentException>("Loc421", fs, "CFF_mapname421" + s_uniquifier, 0, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // newly created file - valid with >0 capacity
            if (File.Exists(s_fileNameTest1))
                File.Delete(s_fileNameTest1);
            using (FileStream fs = new FileStream(s_fileNameTest1, FileMode.CreateNew))
            {
                VerifyCreateFromFile("Loc422", fs, "CFF_mapname422" + s_uniquifier, 1, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // existing file, default capacity
            using (FileStream fs = new FileStream(s_fileNameTest2, FileMode.Open))
            {
                VerifyCreateFromFile("Loc423", fs, "CFF_mapname423" + s_uniquifier, 0, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // existing file, capacity less than file size
            File.WriteAllText(s_fileNameTest2, fileText);
            using (FileStream fs = new FileStream(s_fileNameTest2, FileMode.Open))
            {
                VerifyCreateFromFileException<ArgumentOutOfRangeException>("Loc424", fs, "CFF_mapname424" + s_uniquifier, 6, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // existing file, capacity equal to file size
            File.WriteAllText(s_fileNameTest2, fileText);
            using (FileStream fs = new FileStream(s_fileNameTest2, FileMode.Open))
            {
                VerifyCreateFromFile("Loc425", fs, "CFF_mapname425" + s_uniquifier, fileText.Length, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // existing file, capacity greater than file size
            File.WriteAllText(s_fileNameTest2, fileText);
            using (FileStream fs = new FileStream(s_fileNameTest2, FileMode.Open))
            {
                VerifyCreateFromFile("Loc426", fs, "CFF_mapname426" + s_uniquifier, 6000, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // existing file, capacity greater than file size & access = Read only
            File.WriteAllText(s_fileNameTest2, fileText);
            using (FileStream fs = new FileStream(s_fileNameTest2, FileMode.Open))
            {
                VerifyCreateFromFileException<ArgumentException>("Loc426a", fs, "CFF_mapname426a" + s_uniquifier, 6000, MemoryMappedFileAccess.Read, HandleInheritability.None, false);
            }

            // negative
            using (FileStream fs = new FileStream(s_fileNameTest2, FileMode.Open))
            {
                VerifyCreateFromFileException<ArgumentOutOfRangeException>("Loc427", fs, "CFF_mapname427" + s_uniquifier, -1, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // negative
            using (FileStream fs = new FileStream(s_fileNameTest2, FileMode.Open))
            {
                VerifyCreateFromFileException<ArgumentOutOfRangeException>("Loc428", fs, "CFF_mapname428" + s_uniquifier, -4096, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // Int64.MaxValue - cannot exceed local address space
            using (FileStream fs = new FileStream(s_fileNameTest2, FileMode.Open))
            {
                VerifyCreateFromFileException<IOException>("Loc429", fs, "CFF_mapname429" + s_uniquifier, Int64.MaxValue, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);  // valid but too large
            }

            // [] access

            // Write is disallowed
            using (FileStream fs = new FileStream(s_fileNameTest2, FileMode.Open, FileAccess.ReadWrite))
            {
                VerifyCreateFromFileException<ArgumentException>("Loc430", fs, "CFF_mapname430" + s_uniquifier, 0, MemoryMappedFileAccess.Write, HandleInheritability.None, false);
            }

            // valid access (filestream is ReadWrite)
            MemoryMappedFileAccess[] accessList = new MemoryMappedFileAccess[] {
                MemoryMappedFileAccess.Read,
                MemoryMappedFileAccess.ReadWrite,
                MemoryMappedFileAccess.CopyOnWrite,
            };
            foreach (MemoryMappedFileAccess access in accessList)
            {
                using (FileStream fs = new FileStream(s_fileNameTest2, FileMode.Open, FileAccess.ReadWrite))
                {
                    VerifyCreateFromFile("Loc431_" + access, fs, "CFF_mapname431_" + access + s_uniquifier, 0, access, HandleInheritability.None, false);
                }
            }

            // invalid access (filestream is ReadWrite)
            accessList = new MemoryMappedFileAccess[] {
                MemoryMappedFileAccess.ReadExecute,
                MemoryMappedFileAccess.ReadWriteExecute,
            };
            foreach (MemoryMappedFileAccess access in accessList)
            {
                using (FileStream fs = new FileStream(s_fileNameTest2, FileMode.Open, FileAccess.ReadWrite))
                {
                    VerifyCreateFromFileException<UnauthorizedAccessException>("Loc432_" + access, fs, "CFF_mapname432_" + access + s_uniquifier, 0, access, HandleInheritability.None, false);
                }
            }

            // valid access (filestream is Read only)
            accessList = new MemoryMappedFileAccess[] {
                MemoryMappedFileAccess.Read,
                MemoryMappedFileAccess.CopyOnWrite,
            };
            foreach (MemoryMappedFileAccess access in accessList)
            {
                using (FileStream fs = new FileStream(s_fileNameTest2, FileMode.Open, FileAccess.Read))
                {
                    VerifyCreateFromFile("Loc433_" + access, fs, "CFF_mapname433_" + access + s_uniquifier, 0, access, HandleInheritability.None, false);
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
                using (FileStream fs = new FileStream(s_fileNameTest2, FileMode.Open, FileAccess.Read))
                {
                    VerifyCreateFromFileException<UnauthorizedAccessException>("Loc434_" + access, fs, "CFF_mapname434_" + access + s_uniquifier, 0, access, HandleInheritability.None, false);
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
                using (FileStream fs = new FileStream(s_fileNameTest2, FileMode.Open, FileAccess.Write))
                {
                    VerifyCreateFromFileException<UnauthorizedAccessException>("Loc435_" + access, fs, "CFF_mapname435_" + access + s_uniquifier, 0, access, HandleInheritability.None, false);
                }
            }

            // invalid enum value
            accessList = new MemoryMappedFileAccess[] {
                (MemoryMappedFileAccess)(-1),
                (MemoryMappedFileAccess)(6),
            };
            foreach (MemoryMappedFileAccess access in accessList)
            {
                using (FileStream fs = new FileStream(s_fileNameTest2, FileMode.Open, FileAccess.ReadWrite))
                {
                    VerifyCreateFromFileException<ArgumentOutOfRangeException>("Loc436_" + ((int)access), fs, "CFF_mapname436_" + ((int)access) + s_uniquifier, 0, access, HandleInheritability.None, false);
                }
            }

            // [] inheritability

            // None
            using (FileStream fs = new FileStream(s_fileNameTest2, FileMode.Open))
            {
                VerifyCreateFromFile("Loc461", fs, "CFF_mapname461" + s_uniquifier, 0, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // Inheritable
            using (FileStream fs = new FileStream(s_fileNameTest2, FileMode.Open))
            {
                VerifyCreateFromFile("Loc462", fs, "CFF_mapname462" + s_uniquifier, 0, MemoryMappedFileAccess.ReadWrite, HandleInheritability.Inheritable, false);
            }

            // invalid
            using (FileStream fs = new FileStream(s_fileNameTest2, FileMode.Open))
            {
                VerifyCreateFromFileException<ArgumentOutOfRangeException>("Loc463", fs, "CFF_mapname463" + s_uniquifier, 0, MemoryMappedFileAccess.ReadWrite, (HandleInheritability)(-1), false);
            }
            using (FileStream fs = new FileStream(s_fileNameTest2, FileMode.Open))
            {
                VerifyCreateFromFileException<ArgumentOutOfRangeException>("Loc464", fs, "CFF_mapname464" + s_uniquifier, 0, MemoryMappedFileAccess.ReadWrite, (HandleInheritability)(2), false);
            }

            // [] leaveOpen

            // false
            using (FileStream fs = new FileStream(s_fileNameTest2, FileMode.Open))
            {
                VerifyCreateFromFile("Loc471", fs, "CFF_mapname471" + s_uniquifier, 0, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            }

            // true
            using (FileStream fs = new FileStream(s_fileNameTest2, FileMode.Open))
            {
                VerifyCreateFromFile("Loc472", fs, "CFF_mapname472" + s_uniquifier, 0, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true);
            }

            /// END TEST CASES

           File.Delete(s_fileNameTest1);
           File.Delete(s_fileNameTest2);
           File.Delete(s_fileNameTest3);

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
        if (mapName != null && Interop.PlatformDetection.OperatingSystem != Interop.OperatingSystem.Windows)
        {
            return;
        }

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
        if (mapName != null && Interop.PlatformDetection.OperatingSystem != Interop.OperatingSystem.Windows)
        {
            return;
        }

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
        if (mapName != null && Interop.PlatformDetection.OperatingSystem != Interop.OperatingSystem.Windows)
        {
            return;
        }

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
        if (mapName != null && Interop.PlatformDetection.OperatingSystem != Interop.OperatingSystem.Windows)
        {
            return;
        }

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
        if (mapName != null && Interop.PlatformDetection.OperatingSystem != Interop.OperatingSystem.Windows)
        {
            return;
        }

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
        if (mapName != null && Interop.PlatformDetection.OperatingSystem != Interop.OperatingSystem.Windows)
        {
            return;
        }

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
        if (mapName != null && Interop.PlatformDetection.OperatingSystem != Interop.OperatingSystem.Windows)
        {
            return;
        }

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
        if (mapName != null && Interop.PlatformDetection.OperatingSystem != Interop.OperatingSystem.Windows)
        {
            return;
        }

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
