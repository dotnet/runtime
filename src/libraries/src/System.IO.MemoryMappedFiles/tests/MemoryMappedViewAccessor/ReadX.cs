// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Reflection;
using System.Collections.Generic;
using Xunit;

public class MMVA_ReadX : TestBase
{
    [Fact]
    public static void ReadXTestCases()
    {
        bool bResult = false;
        MMVA_ReadX test = new MMVA_ReadX();

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
        Type[] testedTypes = new Type[] { typeof(Boolean), typeof(Char), typeof(Byte), typeof(Int16),
                      typeof(Int32), typeof(Int64), typeof(SByte),
                      typeof(UInt16), typeof(UInt32), typeof(UInt64),
                      typeof(Single), typeof(Double), typeof(Decimal) };
        Type[] oneByteTypes = new Type[] { typeof(Boolean), typeof(Byte), typeof(SByte) };
        Type[] twoByteTypes = new Type[] { typeof(Char), typeof(Int16), typeof(UInt16) };
        Type[] fourByteTypes = new Type[] { typeof(Single), typeof(Int32), typeof(UInt32) };
        Type[] eightByteTypes = new Type[] { typeof(Double), typeof(Int64), typeof(UInt64) };
        Type[] sixteenByteTypes = new Type[] { typeof(Decimal) };

        Byte[] byteArray1 = new Byte[] { (byte)194, (byte)41, (byte)1, (byte)150, (byte)114, (byte)62, (byte)81, (byte)203, (byte)251, (byte)39, (byte)83, (byte)124, (byte)0, (byte)0, (byte)14, (byte)128 };
        Byte[] byteArray2 = new Byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 0, 0, 0, 0 };
        Byte[] byteArray3 = new Byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 };

        try
        {
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("MMVA_ReadX0", pageSize * 10))
            {
                // Default ViewAccessor size - whole MMF
                using (MemoryMappedViewAccessor view = mmf.CreateViewAccessor())
                {
                    // position = 0, default (zeros)
                    VerifyRead<Boolean>("Loc001a", view, 0, false);
                    VerifyRead<Byte>("Loc001b", view, 0, 0);
                    VerifyRead<SByte>("Loc001c", view, 0, 0);
                    VerifyRead<Char>("Loc001d", view, 0, (Char)0);
                    VerifyRead<Int16>("Loc001e", view, 0, 0);
                    VerifyRead<UInt16>("Loc001f", view, 0, 0);
                    VerifyRead<Int32>("Loc001g", view, 0, 0);
                    VerifyRead<UInt32>("Loc001h", view, 0, 0);
                    VerifyRead<Int64>("Loc001i", view, 0, 0);
                    VerifyRead<UInt64>("Loc001j", view, 0, 0);
                    VerifyRead<Single>("Loc001k", view, 0, 0f);
                    VerifyRead<Double>("Loc001l", view, 0, 0d);
                    VerifyRead<Decimal>("Loc001m", view, 0, 0m);

                    // position = 1
                    VerifyRead<Boolean>("Loc002a", view, 1, false);
                    VerifyRead<Byte>("Loc002b", view, 1, 0);
                    VerifyRead<SByte>("Loc002c", view, 1, 0);
                    VerifyRead<Char>("Loc002d", view, 1, (Char)0);
                    VerifyRead<Int16>("Loc002e", view, 1, 0);
                    VerifyRead<UInt16>("Loc002f", view, 1, 0);
                    VerifyRead<Int32>("Loc002g", view, 1, 0);
                    VerifyRead<UInt32>("Loc002h", view, 1, 0);
                    VerifyRead<Int64>("Loc002i", view, 1, 0);
                    VerifyRead<UInt64>("Loc002j", view, 1, 0);
                    VerifyRead<Single>("Loc002k", view, 1, 0f);
                    VerifyRead<Double>("Loc002l", view, 1, 0d);
                    VerifyRead<Decimal>("Loc002m", view, 1, 0m);

                    // position is last possible for type
                    VerifyRead<Boolean>("Loc003a", view, view.Capacity - 1, false);
                    VerifyRead<Byte>("Loc003b", view, view.Capacity - 1, 0);
                    VerifyRead<SByte>("Loc003c", view, view.Capacity - 1, 0);
                    VerifyRead<Char>("Loc003d", view, view.Capacity - 2, (Char)0);
                    VerifyRead<Int16>("Loc003e", view, view.Capacity - 2, 0);
                    VerifyRead<UInt16>("Loc003f", view, view.Capacity - 2, 0);
                    VerifyRead<Single>("Loc003k", view, view.Capacity - 4, 0f);
                    VerifyRead<Int32>("Loc003g", view, view.Capacity - 4, 0);
                    VerifyRead<UInt32>("Loc003h", view, view.Capacity - 4, 0);
                    VerifyRead<Double>("Loc003l", view, view.Capacity - 8, 0d);
                    VerifyRead<Int64>("Loc003i", view, view.Capacity - 8, 0);
                    VerifyRead<UInt64>("Loc003j", view, view.Capacity - 8, 0);
                    VerifyRead<Decimal>("Loc003m", view, view.Capacity - 16, 0m);

                    // Write some data
                    view.Write(0, (byte)1);
                    view.Write(1000, (byte)1);
                    view.Write(view.Capacity - 1, (byte)255);

                    WriteByteArray(view, 4703, byteArray1, 0, 16);
                    WriteByteArray(view, 12000 - 16, byteArray3, 0, 16);
                    WriteByteArray(view, 2000, byteArray3, 0, 16);

                    // position = 0
                    VerifyRead<Boolean>("Loc011a", view, 0, true);
                    VerifyRead<Byte>("Loc011b", view, 0, 1);
                    VerifyRead<SByte>("Loc11c", view, 0, 1);
                    VerifyRead<Char>("Loc011d", view, 0, (Char)1);
                    VerifyRead<Int16>("Loc011e", view, 0, 1);
                    VerifyRead<UInt16>("Loc011f", view, 0, 1);
                    VerifyRead<Int32>("Loc011g", view, 0, 1);
                    VerifyRead<UInt32>("Loc011h", view, 0, 1);
                    VerifyRead<Int64>("Loc011i", view, 0, 1);
                    VerifyRead<UInt64>("Loc011j", view, 0, 1);
                    VerifyRead<Single>("Loc011k", view, 0, Single.Epsilon);
                    VerifyRead<Double>("Loc011l", view, 0, Double.Epsilon);
                    VerifyRead<Decimal>("Loc011m", view, 0, 1m);

                    //Byte[] byteArray1 = new Byte[] { (byte)194, (byte)41, (byte)1, (byte)150, (byte)114, (byte)62, (byte)81, (byte)203, (byte)251, (byte)39, (byte)83, (byte)124, (byte)0, (byte)0, (byte)14, (byte)128  };
                    // position > 0
                    int pos = 4703;
                    VerifyRead<Boolean>("Loc012a1", view, pos, true);
                    VerifyRead<Boolean>("Loc012a2", view, pos + 2, true);
                    VerifyRead<Boolean>("Loc012a3", view, pos + 12, false);
                    VerifyRead<Byte>("Loc012b1", view, pos, 194);
                    VerifyRead<Byte>("Loc012b2", view, pos + 15, 128);
                    VerifyRead<Byte>("Loc012b3", view, pos + 5, 62);
                    VerifyRead<SByte>("Loc012c1", view, pos, -62);
                    VerifyRead<SByte>("Loc012c2", view, pos + 15, -128);
                    VerifyRead<SByte>("Loc012c3", view, pos + 5, 62);
                    VerifyRead<Char>("Loc012d1", view, pos, (Char)10690);
                    VerifyRead<Char>("Loc012d2", view, pos + 7, (Char)64459);
                    VerifyRead<Char>("Loc012d3", view, pos + 15, (Char)128);
                    VerifyRead<Int16>("Loc012e1", view, pos, 10690);
                    VerifyRead<Int16>("Loc012e2", view, pos + 7, -1077);
                    VerifyRead<Int16>("Loc012e3", view, pos + 5, 20798);
                    VerifyRead<UInt16>("Loc012f1", view, pos, 10690);
                    VerifyRead<UInt16>("Loc012f2", view, pos + 7, 64459);
                    VerifyRead<UInt16>("Loc012f3", view, pos + 5, 20798);
                    VerifyRead<Int32>("Loc012g1", view, pos + 4, BitConverter.ToInt32(byteArray1, 4));
                    VerifyRead<Int32>("Loc012g2", view, pos + 11, BitConverter.ToInt32(byteArray1, 11));
                    VerifyRead<Int32>("Loc012g3", view, pos + 15, 128);
                    VerifyRead<UInt32>("Loc012h1", view, pos + 4, BitConverter.ToUInt32(byteArray1, 4));
                    VerifyRead<UInt32>("Loc012h2", view, pos + 11, BitConverter.ToUInt32(byteArray1, 11));
                    VerifyRead<UInt32>("Loc012h3", view, pos + 15, 128);
                    VerifyRead<Int64>("Loc012i1", view, pos, BitConverter.ToInt64(byteArray1, 0));
                    VerifyRead<Int64>("Loc012i2", view, pos + 3, BitConverter.ToInt64(byteArray1, 3));
                    VerifyRead<Int64>("Loc012i3", view, pos + 15, 128);
                    VerifyRead<UInt64>("Loc012j1", view, pos, BitConverter.ToUInt64(byteArray1, 0));
                    VerifyRead<UInt64>("Loc012j2", view, pos + 3, BitConverter.ToUInt64(byteArray1, 3));
                    VerifyRead<UInt64>("Loc012j3", view, pos + 15, 128);
                    VerifyRead<Single>("Loc012k1", view, pos, BitConverter.ToSingle(byteArray1, 0));
                    VerifyRead<Single>("Loc012k2", view, pos + 5, BitConverter.ToSingle(byteArray1, 5));
                    VerifyRead<Double>("Loc012l1", view, pos, BitConverter.ToDouble(byteArray1, 0));
                    VerifyRead<Double>("Loc012l2", view, pos + 5, BitConverter.ToDouble(byteArray1, 5));
                    VerifyRead<Decimal>("Loc012m1", view, pos, new Decimal(new Int32[] { BitConverter.ToInt32(byteArray1, 0), BitConverter.ToInt32(byteArray1, 4), BitConverter.ToInt32(byteArray1, 8), BitConverter.ToInt32(byteArray1, 12) }));
                    VerifyRead<Decimal>("Loc012m2", view, pos + 4, new Decimal(new Int32[] { BitConverter.ToInt32(byteArray1, 4), BitConverter.ToInt32(byteArray1, 8), BitConverter.ToInt32(byteArray1, 12), 0 })); // ensure last 4 bytes are zero

                    // invalid values
                    VerifyRead<Boolean>("Loc013a", view, 2000, true);
                    VerifyReadException("Loc013d", typeof(Decimal), view, view.Capacity - 1, typeof(ArgumentException));

                    // position is last possible for type
                    VerifyRead<Boolean>("Loc014a", view, view.Capacity - 1, true);
                    VerifyRead<Byte>("Loc014b", view, view.Capacity - 1, 255);
                    VerifyRead<SByte>("Loc014c", view, view.Capacity - 1, -1);
                    VerifyRead<Char>("Loc014d", view, view.Capacity - 2, (Char)65280);
                    VerifyRead<Int16>("Loc014e", view, view.Capacity - 2, -256);
                    VerifyRead<UInt16>("Loc014f", view, view.Capacity - 2, 65280);
                    VerifyRead<Single>("Loc014k", view, view.Capacity - 4, BitConverter.ToSingle(new Byte[] { 0, 0, 0, 255 }, 0));
                    VerifyRead<Int32>("Loc014g", view, view.Capacity - 4, (int)255 << 24);
                    VerifyRead<UInt32>("Loc014h", view, view.Capacity - 4, (uint)255 << 24);
                    VerifyRead<Double>("Loc014l", view, view.Capacity - 8, BitConverter.ToDouble(new Byte[] { 0, 0, 0, 0, 0, 0, 0, 255 }, 0));
                    VerifyRead<Int64>("Loc014i", view, view.Capacity - 8, ((long)255 << 56));
                    VerifyRead<UInt64>("Loc014j", view, view.Capacity - 8, ((ulong)255 << 56));
                    view.Write(view.Capacity - 1, (byte)128);
                    view.Write(view.Capacity - 15, (byte)255);
                    VerifyRead<Decimal>("Loc014m", view, view.Capacity - 16, -65280m);
                    view.Write(view.Capacity - 1, (byte)255);

                    // Exceptions
                    foreach (Type type in testedTypes)
                    {
                        // position <0
                        VerifyReadException("Loc031", type, view, -1, typeof(ArgumentOutOfRangeException));
                        // position >= view.Capacity
                        VerifyReadException("Loc032", type, view, view.Capacity, typeof(ArgumentOutOfRangeException));
                        VerifyReadException("Loc033", type, view, view.Capacity + 1, typeof(ArgumentOutOfRangeException));
                    }

                    // position+sizeof(type) >= view.Capacity
                    foreach (Type type in twoByteTypes)
                        VerifyReadException("Loc034", type, view, view.Capacity - 1, typeof(ArgumentException));
                    foreach (Type type in fourByteTypes)
                        VerifyReadException("Loc035", type, view, view.Capacity - 3, typeof(ArgumentException));
                    foreach (Type type in eightByteTypes)
                        VerifyReadException("Loc036", type, view, view.Capacity - 7, typeof(ArgumentException));
                    foreach (Type type in sixteenByteTypes)
                        VerifyReadException("Loc037", type, view, view.Capacity - 15, typeof(ArgumentException));
                }

                // ViewAccessor starts at nonzero offset, spans remainder of MMF
                using (MemoryMappedViewAccessor view = mmf.CreateViewAccessor(1000, 0))
                {
                    // position = 0
                    VerifyRead<Boolean>("Loc111a", view, 0, true);
                    VerifyRead<Byte>("Loc111b", view, 0, 1);
                    VerifyRead<SByte>("Loc111c", view, 0, 1);
                    VerifyRead<Char>("Loc111d", view, 0, (Char)1);
                    VerifyRead<Int16>("Loc111e", view, 0, 1);
                    VerifyRead<UInt16>("Loc111f", view, 0, 1);
                    VerifyRead<Int32>("Loc111g", view, 0, 1);
                    VerifyRead<UInt32>("Loc111h", view, 0, 1);
                    VerifyRead<Int64>("Loc111i", view, 0, 1);
                    VerifyRead<UInt64>("Loc111j", view, 0, 1);
                    VerifyRead<Single>("Loc111k", view, 0, Single.Epsilon);
                    VerifyRead<Double>("Loc111l", view, 0, Double.Epsilon);
                    VerifyRead<Decimal>("Loc111m", view, 0, 1);

                    // position > 0
                    int pos = 3703;  // already written at 4703 in the previous accessor
                    VerifyRead<Boolean>("Loc112a1", view, pos, true);
                    VerifyRead<Boolean>("Loc112a2", view, pos + 2, true);
                    VerifyRead<Boolean>("Loc112a3", view, pos + 12, false);
                    VerifyRead<Byte>("Loc112b1", view, pos, 194);
                    VerifyRead<Byte>("Loc112b2", view, pos + 15, 128);
                    VerifyRead<Byte>("Loc112b3", view, pos + 5, 62);
                    VerifyRead<SByte>("Loc112c1", view, pos, -62);
                    VerifyRead<SByte>("Loc112c2", view, pos + 15, -128);
                    VerifyRead<SByte>("Loc112c3", view, pos + 5, 62);
                    VerifyRead<Char>("Loc112d1", view, pos, (Char)10690);
                    VerifyRead<Char>("Loc112d2", view, pos + 7, (Char)64459);
                    VerifyRead<Char>("Loc112d3", view, pos + 15, (Char)128);
                    VerifyRead<Int16>("Loc112e1", view, pos, 10690);
                    VerifyRead<Int16>("Loc112e2", view, pos + 7, -1077);
                    VerifyRead<Int16>("Loc112e3", view, pos + 5, 20798);
                    VerifyRead<UInt16>("Loc112f1", view, pos, 10690);
                    VerifyRead<UInt16>("Loc112f2", view, pos + 7, 64459);
                    VerifyRead<UInt16>("Loc112f3", view, pos + 5, 20798);
                    VerifyRead<Int32>("Loc112g1", view, pos + 4, BitConverter.ToInt32(byteArray1, 4));
                    VerifyRead<Int32>("Loc112g2", view, pos + 11, BitConverter.ToInt32(byteArray1, 11));
                    VerifyRead<Int32>("Loc112g3", view, pos + 15, 128);
                    VerifyRead<UInt32>("Loc112h1", view, pos + 4, BitConverter.ToUInt32(byteArray1, 4));
                    VerifyRead<UInt32>("Loc112h2", view, pos + 11, BitConverter.ToUInt32(byteArray1, 11));
                    VerifyRead<UInt32>("Loc112h3", view, pos + 15, 128);
                    VerifyRead<Int64>("Loc112i1", view, pos, BitConverter.ToInt64(byteArray1, 0));
                    VerifyRead<Int64>("Loc112i2", view, pos + 3, BitConverter.ToInt64(byteArray1, 3));
                    VerifyRead<Int64>("Loc112i3", view, pos + 15, 128);
                    VerifyRead<UInt64>("Loc112j1", view, pos, BitConverter.ToUInt64(byteArray1, 0));
                    VerifyRead<UInt64>("Loc112j2", view, pos + 3, BitConverter.ToUInt64(byteArray1, 3));
                    VerifyRead<UInt64>("Loc112j3", view, pos + 15, 128);
                    VerifyRead<Single>("Loc112k1", view, pos, BitConverter.ToSingle(byteArray1, 0));
                    VerifyRead<Single>("Loc112k2", view, pos + 5, BitConverter.ToSingle(byteArray1, 5));
                    VerifyRead<Double>("Loc112l1", view, pos, BitConverter.ToDouble(byteArray1, 0));
                    VerifyRead<Double>("Loc112l2", view, pos + 5, BitConverter.ToDouble(byteArray1, 5));
                    VerifyRead<Decimal>("Loc112m1", view, pos, new Decimal(new Int32[] { BitConverter.ToInt32(byteArray1, 0), BitConverter.ToInt32(byteArray1, 4), BitConverter.ToInt32(byteArray1, 8), BitConverter.ToInt32(byteArray1, 12) }));
                    VerifyRead<Decimal>("Loc112m2", view, pos + 4, new Decimal(new Int32[] { BitConverter.ToInt32(byteArray1, 4), BitConverter.ToInt32(byteArray1, 8), BitConverter.ToInt32(byteArray1, 12), 0 })); // ensure last 4 bytes are zero

                    // position is last possible for type
                    VerifyRead<Boolean>("Loc114a", view, view.Capacity - 1, true);
                    VerifyRead<Byte>("Loc114b", view, view.Capacity - 1, 255);
                    VerifyRead<SByte>("Loc114c", view, view.Capacity - 1, -1);
                    VerifyRead<Char>("Loc114d", view, view.Capacity - 2, (Char)65280);
                    VerifyRead<Int16>("Loc114e", view, view.Capacity - 2, -256);
                    VerifyRead<UInt16>("Loc114f", view, view.Capacity - 2, 65280);
                    VerifyRead<Single>("Loc114k", view, view.Capacity - 4, BitConverter.ToSingle(new Byte[] { 0, 0, 0, 255 }, 0));
                    VerifyRead<Int32>("Loc114g", view, view.Capacity - 4, (int)255 << 24);
                    VerifyRead<UInt32>("Loc114h", view, view.Capacity - 4, (uint)255 << 24);
                    VerifyRead<Double>("Loc114l", view, view.Capacity - 8, BitConverter.ToDouble(new Byte[] { 0, 0, 0, 0, 0, 0, 0, 255 }, 0));
                    VerifyRead<Int64>("Loc114i", view, view.Capacity - 8, ((long)255 << 56));
                    VerifyRead<UInt64>("Loc114j", view, view.Capacity - 8, ((ulong)255 << 56));
                    view.Write(view.Capacity - 1, (byte)128);
                    view.Write(view.Capacity - 15, (byte)255);
                    VerifyRead<Decimal>("Loc114m", view, view.Capacity - 16, -65280m);
                    view.Write(view.Capacity - 1, (byte)255);

                    // Exceptions
                    foreach (Type type in testedTypes)
                    {
                        // position <0
                        VerifyReadException("Loc131", type, view, -1, typeof(ArgumentOutOfRangeException));
                        // position >= view.Capacity
                        VerifyReadException("Loc132", type, view, view.Capacity, typeof(ArgumentOutOfRangeException));
                        VerifyReadException("Loc133", type, view, view.Capacity + 1, typeof(ArgumentOutOfRangeException));
                    }

                    // position+sizeof(type) >= view.Capacity
                    foreach (Type type in twoByteTypes)
                        VerifyReadException("Loc134", type, view, view.Capacity - 1, typeof(ArgumentException));
                    foreach (Type type in fourByteTypes)
                        VerifyReadException("Loc135", type, view, view.Capacity - 3, typeof(ArgumentException));
                    foreach (Type type in eightByteTypes)
                        VerifyReadException("Loc136", type, view, view.Capacity - 7, typeof(ArgumentException));
                    foreach (Type type in sixteenByteTypes)
                        VerifyReadException("Loc137", type, view, view.Capacity - 15, typeof(ArgumentException));
                }

                // ViewAccessor starts at nonzero offset, with size shorter than MMF
                using (MemoryMappedViewAccessor view = mmf.CreateViewAccessor(2000, 10000))
                {
                    // position is last possible for type
                    VerifyRead<Boolean>("Loc214a", view, view.Capacity - 1, true);
                    VerifyRead<Byte>("Loc214b", view, view.Capacity - 1, Byte.MaxValue);
                    VerifyRead<SByte>("Loc214c", view, view.Capacity - 1, -1);
                    VerifyRead<Char>("Loc214d", view, view.Capacity - 2, Char.MaxValue);
                    VerifyRead<Int16>("Loc214e", view, view.Capacity - 2, -1);
                    VerifyRead<UInt16>("Loc214f", view, view.Capacity - 2, UInt16.MaxValue);
                    VerifyRead<Single>("Loc214k", view, view.Capacity - 4, Single.NaN);
                    VerifyRead<Int32>("Loc214g", view, view.Capacity - 4, -1);
                    VerifyRead<UInt32>("Loc214h", view, view.Capacity - 4, UInt32.MaxValue);
                    VerifyRead<Double>("Loc214l", view, view.Capacity - 8, Double.NaN);
                    VerifyRead<Int64>("Loc214i", view, view.Capacity - 8, -1);
                    VerifyRead<UInt64>("Loc214j", view, view.Capacity - 8, UInt64.MaxValue);

                    WriteByteArray(view, view.Capacity - 16, byteArray2, 0, 16);

                    VerifyRead<Decimal>("Loc214m", view, view.Capacity - 16, Decimal.MaxValue);

                    // Exceptions
                    foreach (Type type in testedTypes)
                    {
                        // position <0
                        VerifyReadException("Loc231", type, view, -1, typeof(ArgumentOutOfRangeException));
                        // position >= view.Capacity
                        VerifyReadException("Loc232", type, view, view.Capacity, typeof(ArgumentOutOfRangeException));
                        VerifyReadException("Loc233", type, view, view.Capacity + 1, typeof(ArgumentOutOfRangeException));
                    }

                    // position+sizeof(type) >= view.Capacity
                    foreach (Type type in twoByteTypes)
                        VerifyReadException("Loc234", type, view, view.Capacity - 1, typeof(ArgumentException));
                    foreach (Type type in fourByteTypes)
                        VerifyReadException("Loc235", type, view, view.Capacity - 3, typeof(ArgumentException));
                    foreach (Type type in eightByteTypes)
                        VerifyReadException("Loc236", type, view, view.Capacity - 7, typeof(ArgumentException));
                    foreach (Type type in sixteenByteTypes)
                        VerifyReadException("Loc237", type, view, view.Capacity - 15, typeof(ArgumentException));
                }

                // Accessor does not support reading
                using (MemoryMappedViewAccessor view = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Write))
                {
                    foreach (Type t in testedTypes)
                    {
                        VerifyReadException("Loc401_" + t.ToString(), t, view, 0, typeof(NotSupportedException));
                    }
                }

                // Call after view has been disposed
                MemoryMappedViewAccessor view1 = mmf.CreateViewAccessor();
                view1.Dispose();
                foreach (Type t in testedTypes)
                {
                    VerifyReadException("Loc501_" + t.ToString(), t, view1, 0, typeof(ObjectDisposedException));
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

    private void WriteByteArray(MemoryMappedViewAccessor view, long position, byte[] array, int offset, int count)
    {
        for (int g = 0; g < count; g++)
        {
            view.Write(position + offset + g, array[g]);
        }
    }

    /// START HELPER FUNCTIONS
    public void VerifyRead<T>(String strLoc, MemoryMappedViewAccessor view, long position, T expectedValue)
    {
        iCountTestcases++;
        try
        {
            MethodInfo method = GetMethod(typeof(T));
            T ret = (T)method.Invoke(view, new Object[] { position });
            Eval<T>(expectedValue, ret, "ERROR, {0}_{1}:  Returned value was wrong.", strLoc, typeof(T).Name);
        }
        catch (TargetInvocationException ex)
        {
            Exception inner = ex.InnerException;
            iCountErrors++;
            Console.WriteLine("ERROR, {0}_{1}: Unexpected exception, {2}", strLoc, typeof(T).Name, inner);
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}_{1}: Unexpected exception, {2}", strLoc, typeof(T).Name, ex);
        }
    }

    public void VerifyReadException(String strLoc, Type dataType, MemoryMappedViewAccessor view, long position, Type exceptionType)
    {
        iCountTestcases++;
        try
        {
            MethodInfo method = GetMethod(dataType);
            method.Invoke(view, new Object[] { position });
            iCountErrors++;
            Console.WriteLine("ERROR, {0}_{1}: No exception thrown, expected {2}", strLoc, dataType.Name, exceptionType);
        }
        catch (TargetInvocationException ex)
        {
            Exception inner = ex.InnerException;
            if (inner.GetType() == exceptionType)
            {
                //Console.WriteLine("{0}_{1}: Expected, {2}: {3}", strLoc, dataType.Name, inner.GetType(), inner.Message);
            }
            else
                throw inner;
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Console.WriteLine("ERROR, {0}_{1}: Unexpected exception, {2}", strLoc, dataType.Name, ex);
        }
    }

    public MethodInfo GetMethod(Type dataType)
    {
        Type viewType = typeof(MemoryMappedViewAccessor);
        MethodInfo method = null;
        if (dataType == typeof(Boolean))
            method = viewType.GetMethod("ReadBoolean", new Type[] { typeof(long) });
        else if (dataType == typeof(Byte))
            method = viewType.GetMethod("ReadByte", new Type[] { typeof(long) });
        else if (dataType == typeof(Char))
            method = viewType.GetMethod("ReadChar", new Type[] { typeof(long) });
        else if (dataType == typeof(Decimal))
            method = viewType.GetMethod("ReadDecimal", new Type[] { typeof(long) });
        else if (dataType == typeof(Single))
            method = viewType.GetMethod("ReadSingle", new Type[] { typeof(long) });
        else if (dataType == typeof(Double))
            method = viewType.GetMethod("ReadDouble", new Type[] { typeof(long) });
        else if (dataType == typeof(Int16))
            method = viewType.GetMethod("ReadInt16", new Type[] { typeof(long) });
        else if (dataType == typeof(Int32))
            method = viewType.GetMethod("ReadInt32", new Type[] { typeof(long) });
        else if (dataType == typeof(Int64))
            method = viewType.GetMethod("ReadInt64", new Type[] { typeof(long) });
        else if (dataType == typeof(SByte))
            method = viewType.GetMethod("ReadSByte", new Type[] { typeof(long) });
        else if (dataType == typeof(UInt16))
            method = viewType.GetMethod("ReadUInt16", new Type[] { typeof(long) });
        else if (dataType == typeof(UInt32))
            method = viewType.GetMethod("ReadUInt32", new Type[] { typeof(long) });
        else if (dataType == typeof(UInt64))
            method = viewType.GetMethod("ReadUInt64", new Type[] { typeof(long) });
        else
            throw new Exception("Unknown type " + dataType.ToString() + "!");

        if (method == null)
            throw new Exception("No Read method found for type " + dataType.ToString() + "!");
        else
            return method;
    }
}
