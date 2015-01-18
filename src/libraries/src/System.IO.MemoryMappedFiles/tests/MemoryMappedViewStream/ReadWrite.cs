// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Collections.Generic;
using Xunit;
using System.Runtime.InteropServices;

public class MMVS_ReadWrite : TestBase
{
    [Fact]
    public static void ReadWriteTestCases()
    {
        bool bResult = false;
        MMVS_ReadWrite test = new MMVS_ReadWrite();

        try
        {
            bResult = test.runTest();
        }
        catch (Exception exc_main)
        {
            bResult = false;
            Console.WriteLine("Fail! Error Err_9999zzz! Uncaught Exception in main(), exc_main==" + exc_main.ToString());
        }

        Assert.True(bResult, "One or more test cases failed.");
    }

    public bool runTest()
    {
        uint pageSize = SystemInfoHelpers.GetPageSize();

        Byte[] byteArray1 = new Byte[] { 194, 58, 217, 150, 114, 62, 81, 203, 251, 39, 83, 124, 0, 0, 14, 128 };
        Byte[] byteArray2 = new Byte[] { 0x35, 0x4C, 0x4A, 0x79, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 5, 0,
                                     1,
                                         0x4A, 0xCE, 0x96, 0x0C,
                                     0, 0, 0 };
        Byte[] byteArray3 = new Byte[] { 0x35, 0x4C, 0x4A, 0x79, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 5, 0 };
        Byte[] byteArray3b = new Byte[] { 0, 0, 0, 0, 0, 0, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        Byte[] byteArray4 = new Byte[] { 1 };
        Byte[] byteArray5 = new Byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0, 0, 0, 0, 1, 0xBB, 0x68, 0x01, 0x00, 0, 0, 0 };

        Decimal dec = 20349.12309m;
        DateTime date = DateTime.Now;

        try
        {
            if (File.Exists("file1.dat"))
                File.Delete("file1.dat");
            using (FileStream fileStream = new FileStream("file1.dat", FileMode.Create, FileAccess.ReadWrite))
            {
                using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(fileStream, "MMVS_ReadWrite0", pageSize * 10, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false))
                {
                    // Default ViewStream size - whole MMF
                    using (MemoryMappedViewStream view = mmf.CreateViewStream())
                    {
                        BinaryReader reader = new BinaryReader(view);
                        BinaryWriter writer = new BinaryWriter(view);

                        // nothing written - read zeros
                        VerifyRead("Loc001", view, new Byte[24], 0, 24, new Byte[24], 24);
                        VerifyReadBoolean("Loc002", reader, false);
                        VerifyReadUInt16("Loc003", reader, 0);
                        VerifyReadInt32("Loc004", reader, 0);
                        VerifyReadDouble("Loc005", reader, 0d);
                        VerifyReadDecimal("Loc006", reader, 0m);

                        // Write to ViewStream
                        view.Seek(0, SeekOrigin.Begin);
                        view.Write(byteArray1, 0, 16);
                        view.Seek(0, SeekOrigin.Begin);
                        VerifyRead("Loc011", view, new Byte[16], 0, 16, byteArray1, 16);
                        reader.BaseStream.Seek(0, SeekOrigin.Begin);
                        VerifyReadBoolean("Loc012", reader, true);
                        VerifyReadUInt16("Loc013", reader, 55610);
                        VerifyReadInt32("Loc014", reader, 1363047062);
                        VerifyReadDouble("Loc015", reader, BitConverter.ToDouble(byteArray1, 7));
                        reader.BaseStream.Seek(0, SeekOrigin.Begin);
                        VerifyReadDecimal("Loc016", reader, new Decimal(new Int32[] { BitConverter.ToInt32(byteArray1, 0), BitConverter.ToInt32(byteArray1, 4), BitConverter.ToInt32(byteArray1, 8), BitConverter.ToInt32(byteArray1, 12) }));

                        // Write to BinaryWriter
                        writer.BaseStream.Seek(2161, SeekOrigin.Begin);
                        writer.Write(dec);
                        writer.Write(true);
                        writer.Write(211209802);
                        view.Seek(2161, SeekOrigin.Begin);
                        VerifyRead("Loc021", view, new Byte[24], 0, 24, byteArray2, 24);
                        reader.BaseStream.Seek(2161, SeekOrigin.Begin);
                        VerifyReadDecimal("Loc022", reader, dec);
                        VerifyReadBoolean("Loc023", reader, true);
                        VerifyReadInt32("Loc024", reader, 211209802);

                        // Write to end of stream
                        view.Seek(-16, SeekOrigin.End);
                        view.Write(byteArray2, 0, 16); // now at end of stream
                        VerifyRead("Loc031", view, new Byte[16], 0, 16, new Byte[16], 0);
                        view.Seek(-16, SeekOrigin.End);
                        VerifyRead("Loc032", view, new Byte[16], 0, 16, byteArray3, 16);
                        view.Seek(-8, SeekOrigin.End);
                        VerifyRead("Loc033", view, new Byte[16], 0, 16, byteArray3b, 8);  // read partial array
                        // BinaryReader
                        reader.BaseStream.Seek(-16, SeekOrigin.End);
                        VerifyReadDecimal("Loc034", reader, dec); // now at end of stream
                        VerifyReadDecimalException<IOException>("Loc035", reader);
                        reader.BaseStream.Seek(-8, SeekOrigin.End);
                        VerifyRead("Loc036", reader, new Byte[16], 0, 16, byteArray3b, 8);  // read partial array

                        // Write to end of stream as calculated from viewstream capacity
                        view.Seek(view.Capacity - 1, SeekOrigin.Begin);
                        view.Write(byteArray2, 16, 1); // now at end of stream
                        VerifyRead("Loc041", view, new Byte[16], 0, 16, new Byte[16], 0);
                        view.Seek(view.Capacity - 1, SeekOrigin.Begin);
                        VerifyRead("Loc042", view, new Byte[1], 0, 1, byteArray4, 1);
                        reader.BaseStream.Seek(view.Capacity - 1, SeekOrigin.Begin);
                        VerifyReadBoolean("Loc043", reader, true); // now at end of stream
                        VerifyReadBooleanException<EndOfStreamException>("Loc044", reader);

                        // Write past end of stream
                        view.Seek(0, SeekOrigin.End);
                        VerifyWriteException<NotSupportedException>("Loc051", view, new Byte[16], 0, 16);
                        writer.Seek(0, SeekOrigin.End);
                        VerifyWriteException<NotSupportedException>("Loc052", writer, Byte.MaxValue);

                        // Seek past end
                        view.Seek(1, SeekOrigin.End);
                        VerifyRead("Loc061", view, new Byte[1], 0, 1, new Byte[1], 0);
                        view.Seek((int)(view.Capacity + 1), SeekOrigin.Begin);
                        VerifyRead("Loc062", view, new Byte[1], 0, 1, new Byte[1], 0);

                        // Seek before beginning
                        VerifySeekException<IOException>("Loc065", view, -1, SeekOrigin.Begin);
                        VerifySeekException<IOException>("Loc066", view, (int)(-view.Capacity - 1), SeekOrigin.End);
                        VerifySeekException<IOException>("Loc067", reader.BaseStream, -1, SeekOrigin.Begin);
                        VerifySeekException<IOException>("Loc068", reader.BaseStream, (int)(-view.Capacity - 1), SeekOrigin.End);

                        view.Flush();

                        // Verify file state
                        BinaryReader reader2 = new BinaryReader(fileStream);
                        reader2.BaseStream.Seek(0, SeekOrigin.Begin);
                        VerifyReadBoolean("Loc071", reader2, true);
                        VerifyReadUInt16("Loc072", reader2, 55610);
                        VerifyReadInt32("Loc073", reader2, 1363047062);
                        VerifyReadDouble("Loc074", reader2, BitConverter.ToDouble(byteArray1, 7));
                        reader2.BaseStream.Seek(0, SeekOrigin.Begin);
                        VerifyReadDecimal("Loc075", reader2, new Decimal(new Int32[] { BitConverter.ToInt32(byteArray1, 0), BitConverter.ToInt32(byteArray1, 4), BitConverter.ToInt32(byteArray1, 8), BitConverter.ToInt32(byteArray1, 12) }));
                        reader2.BaseStream.Seek(2161, SeekOrigin.Begin);
                        VerifyReadDecimal("Loc076", reader2, dec);
                        VerifyReadBoolean("Loc077", reader2, true);
                        VerifyReadInt32("Loc078", reader2, 211209802);
                        reader2.BaseStream.Seek(-1, SeekOrigin.End);
                        VerifyReadBoolean("Loc079", reader2, true);
                    }

                    // ViewStream starts at nonzero offset, spans remainder of MMF
                    using (MemoryMappedViewStream view = mmf.CreateViewStream(1000, 0))
                    {
                        BinaryReader reader = new BinaryReader(view);
                        BinaryWriter writer = new BinaryWriter(view);

                        // nothing written - read zeros
                        VerifyRead("Loc101", view, new Byte[24], 0, 24, new Byte[24], 24);
                        VerifyReadBoolean("Loc102", reader, false);
                        VerifyReadUInt16("Loc103", reader, 0);
                        VerifyReadInt32("Loc104", reader, 0);
                        VerifyReadDouble("Loc105", reader, 0d);
                        VerifyReadDecimal("Loc106", reader, 0m);

                        // Write to ViewStream
                        view.Seek(0, SeekOrigin.Begin);
                        view.Write(byteArray1, 0, 16);
                        view.Seek(0, SeekOrigin.Begin);
                        VerifyRead("Loc111", view, new Byte[16], 0, 16, byteArray1, 16);
                        reader.BaseStream.Seek(0, SeekOrigin.Begin);
                        VerifyReadBoolean("Loc112", reader, true);
                        VerifyReadUInt16("Loc113", reader, 55610);
                        VerifyReadInt32("Loc114", reader, 1363047062);
                        VerifyReadDouble("Loc115", reader, BitConverter.ToDouble(byteArray1, 7));
                        reader.BaseStream.Seek(0, SeekOrigin.Begin);
                        VerifyReadDecimal("Loc116", reader, new Decimal(new Int32[] { BitConverter.ToInt32(byteArray1, 0), BitConverter.ToInt32(byteArray1, 4), BitConverter.ToInt32(byteArray1, 8), BitConverter.ToInt32(byteArray1, 12) }));

                        // Read existing values
                        view.Seek(1161, SeekOrigin.Begin);
                        VerifyRead("Loc117", view, new Byte[24], 0, 24, byteArray2, 24);
                        reader.BaseStream.Seek(1161, SeekOrigin.Begin);
                        VerifyReadDecimal("Loc118", reader, dec);
                        VerifyReadBoolean("Loc119", reader, true);
                        VerifyReadInt32("Loc120", reader, 211209802);

                        // Write to BinaryWriter
                        writer.BaseStream.Seek(3000, SeekOrigin.Begin);
                        writer.Write(Decimal.MaxValue);
                        writer.Write(true);
                        writer.Write(92347);
                        view.Seek(3000, SeekOrigin.Begin);
                        VerifyRead("Loc121", view, new Byte[24], 0, 24, byteArray5, 24);
                        reader.BaseStream.Seek(3000, SeekOrigin.Begin);
                        VerifyReadDecimal("Loc122", reader, Decimal.MaxValue);
                        VerifyReadBoolean("Loc123", reader, true);
                        VerifyReadInt32("Loc124", reader, 92347);

                        // Write to end of stream
                        view.Seek(-16, SeekOrigin.End);
                        view.Write(byteArray2, 0, 16); // now at end of stream
                        VerifyRead("Loc131", view, new Byte[16], 0, 16, new Byte[16], 0);
                        view.Seek(-16, SeekOrigin.End);
                        VerifyRead("Loc132", view, new Byte[16], 0, 16, byteArray3, 16);
                        view.Seek(-8, SeekOrigin.End);
                        VerifyRead("Loc133", view, new Byte[16], 0, 16, byteArray3b, 8);  // read partial array
                        // BinaryReader
                        reader.BaseStream.Seek(-16, SeekOrigin.End);
                        VerifyReadDecimal("Loc134", reader, dec); // now at end of stream
                        VerifyReadDecimalException<IOException>("Loc135", reader);
                        reader.BaseStream.Seek(-8, SeekOrigin.End);
                        VerifyRead("Loc136", reader, new Byte[16], 0, 16, byteArray3b, 8);  // read partial array

                        // Write to end of stream as calculated from viewstream capacity
                        view.Seek(view.Capacity - 1, SeekOrigin.Begin);
                        view.Write(byteArray2, 16, 1); // now at end of stream
                        VerifyRead("Loc141", view, new Byte[16], 0, 16, new Byte[16], 0);
                        view.Seek(view.Capacity - 1, SeekOrigin.Begin);
                        VerifyRead("Loc142", view, new Byte[1], 0, 1, byteArray4, 1);
                        reader.BaseStream.Seek(view.Capacity - 1, SeekOrigin.Begin);
                        VerifyReadBoolean("Loc143", reader, true); // now at end of stream
                        VerifyReadBooleanException<IOException>("Loc144", reader);

                        // Write past end of stream
                        view.Seek(-8, SeekOrigin.End);
                        VerifyWriteException<NotSupportedException>("Loc151", view, new Byte[16], 0, 16);
                        writer.Seek(-8, SeekOrigin.End);
                        VerifyWriteException<NotSupportedException>("Loc152", writer, Decimal.MaxValue);

                        // Seek past end
                        view.Seek(1, SeekOrigin.End);
                        VerifyRead("Loc161", view, new Byte[1], 0, 1, new Byte[1], 0);
                        view.Seek((int)(view.Capacity + 1), SeekOrigin.Begin);
                        VerifyRead("Loc162", view, new Byte[1], 0, 1, new Byte[1], 0);

                        // Seek before beginning
                        VerifySeekException<IOException>("Loc165", view, -1, SeekOrigin.Begin);
                        VerifySeekException<IOException>("Loc166", view, (int)(-view.Capacity - 1), SeekOrigin.End);
                        VerifySeekException<IOException>("Loc167", reader.BaseStream, -1, SeekOrigin.Begin);
                        VerifySeekException<IOException>("Loc168", reader.BaseStream, (int)(-view.Capacity - 1), SeekOrigin.End);
                    }

                    // ViewStream starts at nonzero offset, with size shorter than MMF
                    using (MemoryMappedViewStream view = mmf.CreateViewStream(2000, 10000))
                    {
                        BinaryReader reader = new BinaryReader(view);
                        BinaryWriter writer = new BinaryWriter(view);

                        // nothing written - read zeros
                        VerifyRead("Loc201", view, new Byte[24], 0, 24, new Byte[24], 24);
                        VerifyReadBoolean("Loc202", reader, false);
                        VerifyReadUInt16("Loc203", reader, 0);
                        VerifyReadInt32("Loc204", reader, 0);
                        VerifyReadDouble("Loc205", reader, 0d);
                        VerifyReadDecimal("Loc206", reader, 0m);

                        // Write to ViewStream
                        view.Seek(0, SeekOrigin.Begin);
                        view.Write(byteArray1, 0, 16);
                        view.Seek(0, SeekOrigin.Begin);
                        VerifyRead("Loc211", view, new Byte[16], 0, 16, byteArray1, 16);
                        reader.BaseStream.Seek(0, SeekOrigin.Begin);
                        VerifyReadBoolean("Loc212", reader, true);
                        VerifyReadUInt16("Loc213", reader, 55610);
                        VerifyReadInt32("Loc214", reader, 1363047062);
                        VerifyReadDouble("Loc215", reader, BitConverter.ToDouble(byteArray1, 7));
                        reader.BaseStream.Seek(0, SeekOrigin.Begin);
                        VerifyReadDecimal("Loc216", reader, new Decimal(new Int32[] { BitConverter.ToInt32(byteArray1, 0), BitConverter.ToInt32(byteArray1, 4), BitConverter.ToInt32(byteArray1, 8), BitConverter.ToInt32(byteArray1, 12) }));

                        // Read existing values
                        view.Seek(161, SeekOrigin.Begin);
                        VerifyRead("Loc217", view, new Byte[24], 0, 24, byteArray2, 24);
                        reader.BaseStream.Seek(161, SeekOrigin.Begin);
                        VerifyReadDecimal("Loc218", reader, dec);
                        VerifyReadBoolean("Loc219", reader, true);
                        VerifyReadInt32("Loc220", reader, 211209802);

                        // Write to BinaryWriter
                        writer.BaseStream.Seek(3000, SeekOrigin.Begin);
                        writer.Write(Decimal.MaxValue);
                        writer.Write(true);
                        writer.Write(92347);
                        view.Seek(3000, SeekOrigin.Begin);
                        VerifyRead("Loc221", view, new Byte[24], 0, 24, byteArray5, 24);
                        reader.BaseStream.Seek(3000, SeekOrigin.Begin);
                        VerifyReadDecimal("Loc222", reader, Decimal.MaxValue);
                        VerifyReadBoolean("Loc223", reader, true);
                        VerifyReadInt32("Loc224", reader, 92347);

                        // Write to end of stream
                        view.Seek(-16, SeekOrigin.End);
                        view.Write(byteArray2, 0, 16); // now at end of stream
                        VerifyRead("Loc231", view, new Byte[16], 0, 16, new Byte[16], 0);
                        view.Seek(-16, SeekOrigin.End);
                        VerifyRead("Loc232", view, new Byte[16], 0, 16, byteArray3, 16);
                        view.Seek(-8, SeekOrigin.End);
                        VerifyRead("Loc233", view, new Byte[16], 0, 16, byteArray3b, 8);  // read partial array
                        // BinaryReader
                        reader.BaseStream.Seek(-16, SeekOrigin.End);
                        VerifyReadDecimal("Loc234", reader, dec); // now at end of stream
                        VerifyReadDecimalException<IOException>("Loc235", reader);
                        reader.BaseStream.Seek(-8, SeekOrigin.End);
                        VerifyRead("Loc236", reader, new Byte[16], 0, 16, byteArray3b, 8);  // read partial array

                        // Write to end of stream as calculated from viewstream capacity
                        view.Seek(view.Capacity - 1, SeekOrigin.Begin);
                        view.Write(byteArray2, 16, 1); // now at end of stream
                        VerifyRead("Loc241", view, new Byte[16], 0, 16, new Byte[16], 0);
                        view.Seek(view.Capacity - 1, SeekOrigin.Begin);
                        VerifyRead("Loc242", view, new Byte[1], 0, 1, byteArray4, 1);
                        reader.BaseStream.Seek(view.Capacity - 1, SeekOrigin.Begin);
                        VerifyReadBoolean("Loc243", reader, true); // now at end of stream
                        VerifyReadBooleanException<IOException>("Loc244", reader);

                        // Write past end of stream
                        view.Seek(-1, SeekOrigin.End);
                        VerifyWriteException<NotSupportedException>("Loc251", view, new Byte[16], 0, 2);
                        writer.Seek(-1, SeekOrigin.End);
                        VerifyWriteException<NotSupportedException>("Loc252", writer, Char.MaxValue);

                        // Seek past end
                        view.Seek(1, SeekOrigin.End);
                        VerifyRead("Loc261", view, new Byte[1], 0, 1, new Byte[1], 0);
                        view.Seek((int)(view.Capacity + 1), SeekOrigin.Begin);
                        VerifyRead("Loc262", view, new Byte[1], 0, 1, new Byte[1], 0);

                        // Seek before beginning
                        VerifySeekException<IOException>("Loc265", view, -1, SeekOrigin.Begin);
                        VerifySeekException<IOException>("Loc266", view, (int)(-view.Capacity - 1), SeekOrigin.End);
                        VerifySeekException<IOException>("Loc267", reader.BaseStream, -1, SeekOrigin.Begin);
                        VerifySeekException<IOException>("Loc268", reader.BaseStream, (int)(-view.Capacity - 1), SeekOrigin.End);
                    }
                }
            }

            // Use a pagefile backed MMF instead of file backed
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("MMVS_ReadWrite1", pageSize * 10))
            {
                using (MemoryMappedViewStream view = mmf.CreateViewStream())
                {
                    BinaryReader reader = new BinaryReader(view);
                    BinaryWriter writer = new BinaryWriter(view);

                    // nothing written - read zeros
                    VerifyRead("Loc401", view, new Byte[24], 0, 24, new Byte[24], 24);
                    VerifyReadBoolean("Loc402", reader, false);
                    VerifyReadUInt16("Loc403", reader, 0);
                    VerifyReadInt32("Loc404", reader, 0);
                    VerifyReadDouble("Loc405", reader, 0d);
                    VerifyReadDecimal("Loc406", reader, 0m);

                    // Write to ViewStream
                    view.Seek(0, SeekOrigin.Begin);
                    view.Write(byteArray1, 0, 16);
                    view.Seek(0, SeekOrigin.Begin);
                    VerifyRead("Loc411", view, new Byte[16], 0, 16, byteArray1, 16);
                    reader.BaseStream.Seek(0, SeekOrigin.Begin);
                    VerifyReadBoolean("Loc412", reader, true);
                    VerifyReadUInt16("Loc413", reader, 55610);
                    VerifyReadInt32("Loc414", reader, 1363047062);
                    VerifyReadDouble("Loc415", reader, BitConverter.ToDouble(byteArray1, 7));
                    reader.BaseStream.Seek(0, SeekOrigin.Begin);
                    VerifyReadDecimal("Loc416", reader, new Decimal(new Int32[] { BitConverter.ToInt32(byteArray1, 0), BitConverter.ToInt32(byteArray1, 4), BitConverter.ToInt32(byteArray1, 8), BitConverter.ToInt32(byteArray1, 12) }));

                    // Write to BinaryWriter
                    writer.BaseStream.Seek(2161, SeekOrigin.Begin);
                    writer.Write(dec);
                    writer.Write(true);
                    writer.Write(211209802);
                    view.Seek(2161, SeekOrigin.Begin);
                    VerifyRead("Loc421", view, new Byte[24], 0, 24, byteArray2, 24);
                    reader.BaseStream.Seek(2161, SeekOrigin.Begin);
                    VerifyReadDecimal("Loc422", reader, dec);
                    VerifyReadBoolean("Loc423", reader, true);
                    VerifyReadInt32("Loc424", reader, 211209802);

                    // Write to end of stream
                    view.Seek(-16, SeekOrigin.End);
                    view.Write(byteArray2, 0, 16); // now at end of stream
                    VerifyRead("Loc431", view, new Byte[16], 0, 16, new Byte[16], 0);
                    view.Seek(-16, SeekOrigin.End);
                    VerifyRead("Loc432", view, new Byte[16], 0, 16, byteArray3, 16);
                    view.Seek(-8, SeekOrigin.End);
                    VerifyRead("Loc433", view, new Byte[16], 0, 16, byteArray3b, 8);  // read partial array
                    // BinaryReader
                    reader.BaseStream.Seek(-16, SeekOrigin.End);
                    VerifyReadDecimal("Loc434", reader, dec); // now at end of stream
                    VerifyReadDecimalException<IOException>("Loc435", reader);
                    reader.BaseStream.Seek(-8, SeekOrigin.End);
                    VerifyRead("Loc436", reader, new Byte[16], 0, 16, byteArray3b, 8);  // read partial array

                    // Write to end of stream as calculated from viewstream capacity
                    view.Seek(view.Capacity - 1, SeekOrigin.Begin);
                    view.Write(byteArray2, 16, 1); // now at end of stream
                    VerifyRead("Loc441", view, new Byte[16], 0, 16, new Byte[16], 0);
                    view.Seek(view.Capacity - 1, SeekOrigin.Begin);
                    VerifyRead("Loc442", view, new Byte[1], 0, 1, byteArray4, 1);
                    reader.BaseStream.Seek(view.Capacity - 1, SeekOrigin.Begin);
                    VerifyReadBoolean("Loc443", reader, true); // now at end of stream
                    VerifyReadBooleanException<IOException>("Loc444", reader);

                    // Write past end of stream
                    view.Seek(0, SeekOrigin.End);
                    VerifyWriteException<NotSupportedException>("Loc451", view, new Byte[16], 0, 16);
                    writer.Seek(0, SeekOrigin.End);
                    VerifyWriteException<NotSupportedException>("Loc452", writer, Byte.MaxValue);

                    // Seek past end
                    view.Seek(1, SeekOrigin.End);
                    VerifyRead("Loc461", view, new Byte[1], 0, 1, new Byte[1], 0);
                    view.Seek((int)(view.Capacity + 1), SeekOrigin.Begin);
                    VerifyRead("Loc462", view, new Byte[1], 0, 1, new Byte[1], 0);

                    // Seek before beginning
                    VerifySeekException<IOException>("Loc465", view, -1, SeekOrigin.Begin);
                    VerifySeekException<IOException>("Loc466", view, (int)(-view.Capacity - 1), SeekOrigin.End);
                    VerifySeekException<IOException>("Loc467", reader.BaseStream, -1, SeekOrigin.Begin);
                    VerifySeekException<IOException>("Loc468", reader.BaseStream, (int)(-view.Capacity - 1), SeekOrigin.End);
                }

                using (MemoryMappedViewStream view = mmf.CreateViewStream(0, 10000, MemoryMappedFileAccess.CopyOnWrite))
                {
                    BinaryReader reader = new BinaryReader(view);
                    BinaryWriter writer = new BinaryWriter(view);

                    // Read existing values
                    view.Seek(2161, SeekOrigin.Begin);
                    VerifyRead("Loc501", view, new Byte[24], 0, 24, byteArray2, 24);
                    reader.BaseStream.Seek(2161, SeekOrigin.Begin);
                    VerifyReadDecimal("Loc502", reader, dec);
                    VerifyReadBoolean("Loc503", reader, true);
                    VerifyReadInt32("Loc504", reader, 211209802);

                    // Write to ViewStream
                    view.Seek(4000, SeekOrigin.Begin);
                    view.Write(byteArray1, 0, 16);
                    view.Seek(4000, SeekOrigin.Begin);
                    VerifyRead("Loc511", view, new Byte[16], 0, 16, byteArray1, 16);
                    reader.BaseStream.Seek(4000, SeekOrigin.Begin);
                    VerifyReadBoolean("Loc512", reader, true);
                    VerifyReadUInt16("Loc513", reader, 55610);
                    VerifyReadInt32("Loc514", reader, 1363047062);
                    VerifyReadDouble("Loc515", reader, BitConverter.ToDouble(byteArray1, 7));
                    reader.BaseStream.Seek(4000, SeekOrigin.Begin);
                    VerifyReadDecimal("Loc516", reader, new Decimal(new Int32[] { BitConverter.ToInt32(byteArray1, 0), BitConverter.ToInt32(byteArray1, 4), BitConverter.ToInt32(byteArray1, 8), BitConverter.ToInt32(byteArray1, 12) }));
                }

                using (MemoryMappedViewStream view = mmf.CreateViewStream(0, 10000, MemoryMappedFileAccess.Read))
                {
                    BinaryReader reader = new BinaryReader(view);

                    // Read existing values
                    view.Seek(2161, SeekOrigin.Begin);
                    VerifyRead("Loc601", view, new Byte[24], 0, 24, byteArray2, 24);
                    reader.BaseStream.Seek(2161, SeekOrigin.Begin);
                    VerifyReadDecimal("Loc602", reader, dec);
                    VerifyReadBoolean("Loc603", reader, true);
                    VerifyReadInt32("Loc604", reader, 211209802);

                    // Values from CopyOnWrite ViewStream were not preserved
                    view.Seek(4000, SeekOrigin.Begin);
                    VerifyRead("Loc611", view, new Byte[16], 0, 16, new Byte[16], 16);
                    reader.BaseStream.Seek(4000, SeekOrigin.Begin);
                    VerifyReadBoolean("Loc612", reader, false);
                    VerifyReadUInt16("Loc613", reader, 0);
                    VerifyReadInt32("Loc614", reader, 0);
                    VerifyReadDouble("Loc615", reader, 0d);
                    reader.BaseStream.Seek(4000, SeekOrigin.Begin);
                    VerifyReadDecimal("Loc616", reader, 0m);
                }
            }

            /// END TEST CASES

            if (iCountErrors == 0)
            {
                return true;
            }
            else
            {
                Console.WriteLine("Fail! iCountErrors==" + iCountErrors);
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
    public void VerifyRead(String strLoc, MemoryMappedViewStream view, Byte[] array, int offset, int count, Byte[] expected, int expectedReturn)
    {
        iCountTestcases++;
        try
        {
            int ret = view.Read(array, offset, count);
            Eval(expectedReturn, ret, "ERROR, {0}:  Return value was wrong.", strLoc);
            if (!Eval(array.Length, expected.Length, "TEST ERROR, {0}: expected array size was wrong.", strLoc))
                return;

            for (int i = 0; i < expected.Length; i++)
                if (!Eval(expected[i], array[i], "ERROR, {0}:  Array value at position {1} was wrong.", strLoc, i))
                {
                    Console.WriteLine("LOC_abc, Only reporting one error for this array.  Comment 'break' to see more");
                    break;
                }
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: Unexpected exception, {1}", strLoc, ex);
        }
    }

    public void VerifyRead(String strLoc, BinaryReader reader, Byte[] array, int offset, int count, Byte[] expected, int expectedReturn)
    {
        iCountTestcases++;
        try
        {
            int ret = reader.Read(array, offset, count);
            Eval(expectedReturn, ret, "ERROR, {0}:  Return value was wrong.", strLoc);
            if (!Eval(array.Length, expected.Length, "TEST ERROR, {0}: expected array size was wrong.", strLoc))
                return;

            for (int i = 0; i < expected.Length; i++)
                if (!Eval(expected[i], array[i], "ERROR, {0}:  Array value at position {1} was wrong.", strLoc, i))
                {
                    Console.WriteLine("LOC_def, Only reporting one error for this array.  Comment 'break' to see more");
                    break;
                }
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: Unexpected exception, {1}", strLoc, ex);
        }
    }

    public void VerifyReadBoolean(String strLoc, BinaryReader reader, Boolean expected)
    {
        iCountTestcases++;
        try
        {
            Boolean ret = reader.ReadBoolean();
            Eval(expected, ret, "ERROR, {0}:  Returned value was wrong.", strLoc);
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: Unexpected exception, {1}", strLoc, ex);
        }
    }

    public void VerifyReadUInt16(String strLoc, BinaryReader reader, UInt16 expected)
    {
        iCountTestcases++;
        try
        {
            UInt16 ret = reader.ReadUInt16();
            Eval(expected, ret, "ERROR, {0}:  Returned value was wrong.", strLoc);
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: Unexpected exception, {1}", strLoc, ex);
        }
    }

    public void VerifyReadInt32(String strLoc, BinaryReader reader, Int32 expected)
    {
        iCountTestcases++;
        try
        {
            Int32 ret = reader.ReadInt32();
            Eval(expected, ret, "ERROR, {0}:  Returned value was wrong.", strLoc);
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: Unexpected exception, {1}", strLoc, ex);
        }
    }

    public void VerifyReadDouble(String strLoc, BinaryReader reader, Double expected)
    {
        iCountTestcases++;
        try
        {
            Double ret = reader.ReadDouble();
            Eval(expected, ret, "ERROR, {0}:  Returned value was wrong.", strLoc);
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: Unexpected exception, {1}", strLoc, ex);
        }
    }

    public void VerifyReadDecimal(String strLoc, BinaryReader reader, Decimal expected)
    {
        iCountTestcases++;
        try
        {
            Decimal ret = reader.ReadDecimal();
            Eval(expected, ret, "ERROR, {0}:  Returned value was wrong.", strLoc);
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: Unexpected exception, {1}", strLoc, ex);
        }
    }

    public void VerifyReadException<EXCTYPE>(String strLoc, MemoryMappedViewStream view, Byte[] array, int offset, int count) where EXCTYPE : Exception
    {
        iCountTestcases++;
        try
        {
            view.Read(array, offset, count);
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: No exception thrown, expected {1}", strLoc, typeof(EXCTYPE));
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

    public void VerifyReadException<EXCTYPE>(String strLoc, BinaryReader reader, Byte[] array, int offset, int count) where EXCTYPE : Exception
    {
        iCountTestcases++;
        try
        {
            reader.Read(array, offset, count);
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: No exception thrown, expected {1}", strLoc, typeof(EXCTYPE));
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

    public void VerifyWriteException<EXCTYPE>(String strLoc, MemoryMappedViewStream view, Byte[] array, int offset, int count) where EXCTYPE : Exception
    {
        iCountTestcases++;
        try
        {
            view.Write(array, offset, count);
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: No exception thrown, expected {1}", strLoc, typeof(EXCTYPE));
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

    public void VerifyWriteException<EXCTYPE>(String strLoc, BinaryWriter writer, Byte value) where EXCTYPE : Exception
    {
        iCountTestcases++;
        try
        {
            writer.Write(value);
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: No exception thrown, expected {1}", strLoc, typeof(EXCTYPE));
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

    public void VerifyWriteException<EXCTYPE>(String strLoc, BinaryWriter writer, Char value) where EXCTYPE : Exception
    {
        iCountTestcases++;
        try
        {
            writer.Write(value);
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: No exception thrown, expected {1}", strLoc, typeof(EXCTYPE));
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

    public void VerifyWriteException<EXCTYPE>(String strLoc, BinaryWriter writer, Decimal value) where EXCTYPE : Exception
    {
        iCountTestcases++;
        try
        {
            writer.Write(value);
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: No exception thrown, expected {1}", strLoc, typeof(EXCTYPE));
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

    public void VerifyReadBooleanException<EXCTYPE>(String strLoc, BinaryReader reader) where EXCTYPE : Exception
    {
        iCountTestcases++;
        try
        {
            Boolean ret = reader.ReadBoolean();
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: No exception thrown, expected {1}, got {2}", strLoc, typeof(EXCTYPE), ret);
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

    public void VerifyReadDecimalException<EXCTYPE>(String strLoc, BinaryReader reader) where EXCTYPE : Exception
    {
        iCountTestcases++;
        try
        {
            Decimal ret = reader.ReadDecimal();
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: No exception thrown, expected {1}, got {2}", strLoc, typeof(EXCTYPE), ret);
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

    public void VerifySeekException<EXCTYPE>(String strLoc, MemoryMappedViewStream view, int offset, SeekOrigin origin) where EXCTYPE : Exception
    {
        iCountTestcases++;
        try
        {
            long ret = view.Seek(offset, origin);
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: No exception thrown, expected {1}, got {2}", strLoc, typeof(EXCTYPE), ret);
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

    public void VerifySeekException<EXCTYPE>(String strLoc, Stream stream, int offset, SeekOrigin origin) where EXCTYPE : Exception
    {
        iCountTestcases++;
        try
        {
            long ret = stream.Seek(offset, origin);
            iCountErrors++;
            Console.WriteLine("ERROR, {0}: No exception thrown, expected {1}, got {2}", strLoc, typeof(EXCTYPE), ret);
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
}
