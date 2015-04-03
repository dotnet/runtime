// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Reflection;
using System.Collections.Generic;
using Xunit;

public class MMVA_WriteX : TestBase
{
    private Random _random;
    private uint _pageSize;

    [Fact]
    public static void WriteXTestCases()
    {
        bool bResult = false;
        MMVA_WriteX test = new MMVA_WriteX();

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
        _random = new Random(-55);
        _pageSize = SystemInfoHelpers.GetPageSize();

        Type[] testedTypes = new Type[] { typeof(Boolean), typeof(Char), typeof(Byte), typeof(Int16),
                      typeof(Int32), typeof(Int64), typeof(SByte),
                      typeof(UInt16), typeof(UInt32), typeof(UInt64),
                      typeof(Single), typeof(Double), typeof(Decimal) };
        Type[] oneByteTypes = new Type[] { typeof(Boolean), typeof(Byte), typeof(SByte) };
        Type[] twoByteTypes = new Type[] { typeof(Char), typeof(Int16), typeof(UInt16) };
        Type[] fourByteTypes = new Type[] { typeof(Single), typeof(Int32), typeof(UInt32) };
        Type[] eightByteTypes = new Type[] { typeof(Double), typeof(Int64), typeof(UInt64) };
        Type[] sixteenByteTypes = new Type[] { typeof(Decimal) };

        try
        {
            using (FileStream fs = new FileStream(Path.GetTempFileName(), FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 0x1000, FileOptions.DeleteOnClose))
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(fs, null, _pageSize * 10, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true))
            {
                // Default ViewAccessor size - whole MMF
                using (MemoryMappedViewAccessor view = mmf.CreateViewAccessor())
                {
                    // position = 0
                    VerifyWrite<Boolean>("Loc011a", view, 0, true);
                    VerifyWrite<Byte>("Loc011b", view, 0, 234);
                    VerifyWrite<SByte>("Loc11c", view, 0, 127);
                    VerifyWrite<Char>("Loc011d", view, 0, (Char)23974);
                    VerifyWrite<Int16>("Loc011e", view, 0, 12434);
                    VerifyWrite<UInt16>("Loc011f", view, 0, 24512);
                    VerifyWrite<Int32>("Loc011g", view, 0, 928734);
                    VerifyWrite<UInt32>("Loc011h", view, 0, 210991212);
                    VerifyWrite<Int64>("Loc011i", view, 0, -1029830128231);
                    VerifyWrite<UInt64>("Loc011j", view, 0, 10293891274);
                    VerifyWrite<Single>("Loc011k", view, 0, -0.12243098f);
                    VerifyWrite<Double>("Loc011l", view, 0, 213.1209382093d);
                    VerifyWrite<Decimal>("Loc011m", view, 0, 110293.1123m);

                    // position > 0
                    VerifyWrite<Boolean>("Loc012a1", view, RandPos(), true);
                    VerifyWrite<Boolean>("Loc012a2", view, RandPos(), false);
                    VerifyWrite<Byte>("Loc012b1", view, RandPos(), 194);
                    VerifyWrite<Byte>("Loc012b2", view, RandPos(), Byte.MinValue);
                    VerifyWrite<Byte>("Loc012b3", view, RandPos(), Byte.MaxValue);
                    VerifyWrite<SByte>("Loc012c1", view, RandPos(), -62);
                    VerifyWrite<SByte>("Loc012c2", view, RandPos(), SByte.MinValue);
                    VerifyWrite<SByte>("Loc012c3", view, RandPos(), SByte.MaxValue);
                    VerifyWrite<Char>("Loc012d1", view, RandPos(), (Char)10690);
                    VerifyWrite<Char>("Loc012d2", view, RandPos(), Char.MinValue);
                    VerifyWrite<Char>("Loc012d3", view, RandPos(), Char.MaxValue);
                    VerifyWrite<Int16>("Loc012e1", view, RandPos(), -1077);
                    VerifyWrite<Int16>("Loc012e2", view, RandPos(), Int16.MinValue);
                    VerifyWrite<Int16>("Loc012e3", view, RandPos(), Int16.MaxValue);
                    VerifyWrite<UInt16>("Loc012f1", view, RandPos(), 20798);
                    VerifyWrite<UInt16>("Loc012f2", view, RandPos(), UInt16.MinValue);
                    VerifyWrite<UInt16>("Loc012f3", view, RandPos(), UInt16.MaxValue);
                    VerifyWrite<Int32>("Loc012g1", view, RandPos(), 1212938472);
                    VerifyWrite<Int32>("Loc012g2", view, RandPos(), Int32.MinValue);
                    VerifyWrite<Int32>("Loc012g3", view, RandPos(), Int32.MaxValue);
                    VerifyWrite<UInt32>("Loc012h1", view, RandPos(), 127983047);
                    VerifyWrite<UInt32>("Loc012h2", view, RandPos(), UInt32.MinValue);
                    VerifyWrite<UInt32>("Loc012h3", view, RandPos(), UInt32.MaxValue);
                    VerifyWrite<Int64>("Loc012i1", view, RandPos(), -12039842392110123);
                    VerifyWrite<Int64>("Loc012i2", view, RandPos(), Int64.MinValue);
                    VerifyWrite<Int64>("Loc012i3", view, RandPos(), Int64.MaxValue);
                    VerifyWrite<UInt64>("Loc012j1", view, RandPos(), 938059802);
                    VerifyWrite<UInt64>("Loc012j2", view, RandPos(), UInt64.MinValue);
                    VerifyWrite<UInt64>("Loc012j3", view, RandPos(), UInt64.MaxValue);
                    VerifyWrite<Single>("Loc012k1", view, RandPos(), 0f);
                    VerifyWrite<Single>("Loc012k2", view, RandPos(), Single.MinValue);
                    VerifyWrite<Single>("Loc012k3", view, RandPos(), Single.MaxValue);
                    VerifyWrite<Single>("Loc012k4", view, RandPos(), Single.NegativeInfinity);
                    VerifyWrite<Single>("Loc012k5", view, RandPos(), Single.PositiveInfinity);
                    VerifyWrite<Single>("Loc012k6", view, RandPos(), Single.NaN);
                    VerifyWrite<Double>("Loc012l1", view, RandPos(), 0d);
                    VerifyWrite<Double>("Loc012l2", view, RandPos(), Double.MinValue);
                    VerifyWrite<Double>("Loc012l3", view, RandPos(), Double.MaxValue);
                    VerifyWrite<Double>("Loc012l4", view, RandPos(), Double.NegativeInfinity);
                    VerifyWrite<Double>("Loc012l5", view, RandPos(), Double.PositiveInfinity);
                    VerifyWrite<Double>("Loc012l6", view, RandPos(), Double.NaN);
                    VerifyWrite<Decimal>("Loc012m1", view, RandPos(), -1230912.12312m);
                    VerifyWrite<Decimal>("Loc012m2", view, RandPos(), Decimal.MinValue);
                    VerifyWrite<Decimal>("Loc012m3", view, RandPos(), Decimal.MaxValue);

                    // position is last possible for type
                    VerifyWrite<Boolean>("Loc014a", view, view.Capacity - 1, true);
                    VerifyWrite<Byte>("Loc014b", view, view.Capacity - 1, 255);
                    VerifyWrite<SByte>("Loc014c", view, view.Capacity - 1, -1);
                    VerifyWrite<Char>("Loc014d", view, view.Capacity - 2, (Char)65280);
                    VerifyWrite<Int16>("Loc014e", view, view.Capacity - 2, -256);
                    VerifyWrite<UInt16>("Loc014f", view, view.Capacity - 2, 65280);
                    VerifyWrite<Single>("Loc014k", view, view.Capacity - 4, -0.000001f);
                    VerifyWrite<Int32>("Loc014g", view, view.Capacity - 4, 123);
                    VerifyWrite<UInt32>("Loc014h", view, view.Capacity - 4, 19238);
                    VerifyWrite<Double>("Loc014l", view, view.Capacity - 8, -0.00000001d);
                    VerifyWrite<Int64>("Loc014i", view, view.Capacity - 8, 1029380);
                    VerifyWrite<UInt64>("Loc014j", view, view.Capacity - 8, 9235);
                    VerifyWrite<Decimal>("Loc014m", view, view.Capacity - 16, -65280m);

                    // Exceptions
                    foreach (Type type in testedTypes)
                    {
                        // position <0
                        VerifyWriteException("Loc031", type, view, -1, typeof(ArgumentOutOfRangeException));
                        // position >= view.Capacity
                        VerifyWriteException("Loc032", type, view, view.Capacity, typeof(ArgumentOutOfRangeException));
                        VerifyWriteException("Loc033", type, view, view.Capacity + 1, typeof(ArgumentOutOfRangeException));
                    }

                    // position+sizeof(type) >= view.Capacity
                    foreach (Type type in twoByteTypes)
                        VerifyWriteException("Loc034", type, view, view.Capacity - 1, typeof(ArgumentException));
                    foreach (Type type in fourByteTypes)
                        VerifyWriteException("Loc035", type, view, view.Capacity - 3, typeof(ArgumentException));
                    foreach (Type type in eightByteTypes)
                        VerifyWriteException("Loc036", type, view, view.Capacity - 7, typeof(ArgumentException));
                    foreach (Type type in sixteenByteTypes)
                        VerifyWriteException("Loc037", type, view, view.Capacity - 15, typeof(ArgumentException));

                    // ViewAccessor starts at nonzero offset, spans remainder of MMF
                    using (MemoryMappedViewAccessor view2 = mmf.CreateViewAccessor(1000, 0))
                    {
                        // position = 0
                        VerifyWrite<Boolean>("Loc111a", view2, 0, true);
                        VerifyWrite<Byte>("Loc111b", view2, 0, 1);
                        VerifyWrite<SByte>("Loc111c", view2, 0, 1);
                        VerifyWrite<Char>("Loc111d", view2, 0, (Char)1);
                        VerifyWrite<Int16>("Loc111e", view2, 0, 1);
                        VerifyWrite<UInt16>("Loc111f", view2, 0, 1);
                        VerifyWrite<Int32>("Loc111g", view2, 0, 1);
                        VerifyWrite<UInt32>("Loc111h", view2, 0, 1);
                        VerifyWrite<Int64>("Loc111i", view2, 0, 1);
                        VerifyWrite<UInt64>("Loc111j", view2, 0, 1);
                        VerifyWrite<Single>("Loc111k", view2, 0, Single.Epsilon);
                        VerifyWrite<Double>("Loc111l", view2, 0, Double.Epsilon);
                        VerifyWrite<Decimal>("Loc111m", view2, 0, 1.00001m);

                        // position = 0 of original view should be left untouched
                        VerifyRead<Decimal>("Loc111y", view, 0, 110293.1123m);

                        // Original view can read new values at offset 1000
                        VerifyRead<Decimal>("Loc111z", view, 1000, 1.00001m);

                        // Exceptions
                        foreach (Type type in testedTypes)
                        {
                            // position <0
                            VerifyWriteException("Loc131", type, view, -1, typeof(ArgumentOutOfRangeException));
                            // position >= view.Capacity
                            VerifyWriteException("Loc132", type, view, view.Capacity, typeof(ArgumentOutOfRangeException));
                            VerifyWriteException("Loc133", type, view, view.Capacity + 1, typeof(ArgumentOutOfRangeException));
                        }

                        // position+sizeof(type) >= view.Capacity
                        foreach (Type type in twoByteTypes)
                            VerifyWriteException("Loc134", type, view, view.Capacity - 1, typeof(ArgumentException));
                        foreach (Type type in fourByteTypes)
                            VerifyWriteException("Loc135", type, view, view.Capacity - 3, typeof(ArgumentException));
                        foreach (Type type in eightByteTypes)
                            VerifyWriteException("Loc136", type, view, view.Capacity - 7, typeof(ArgumentException));
                        foreach (Type type in sixteenByteTypes)
                            VerifyWriteException("Loc137", type, view, view.Capacity - 15, typeof(ArgumentException));
                    }
                }

                // ViewAccessor starts at nonzero offset, with size shorter than MMF
                using (MemoryMappedViewAccessor view = mmf.CreateViewAccessor(2000, 10000))
                {
                    // position is last possible for type
                    VerifyWrite<Boolean>("Loc214a", view, view.Capacity - 1, true);
                    VerifyWrite<Byte>("Loc214b", view, view.Capacity - 1, (byte)251);
                    VerifyWrite<SByte>("Loc214c", view, view.Capacity - 1, -42);
                    VerifyWrite<Char>("Loc214d", view, view.Capacity - 2, (Char)2132);
                    VerifyWrite<Int16>("Loc214e", view, view.Capacity - 2, -9187);
                    VerifyWrite<UInt16>("Loc214f", view, view.Capacity - 2, 42354);
                    VerifyWrite<Single>("Loc214k", view, view.Capacity - 4, 3409.12f);
                    VerifyWrite<Int32>("Loc214g", view, view.Capacity - 4, 792351320);
                    VerifyWrite<UInt32>("Loc214h", view, view.Capacity - 4, 2098312120);
                    VerifyWrite<Double>("Loc214l", view, view.Capacity - 8, -0.12398721342d);
                    VerifyWrite<Int64>("Loc214i", view, view.Capacity - 8, (long)98176239824);
                    VerifyWrite<UInt64>("Loc214j", view, view.Capacity - 8, (ulong)1029831212091029);
                    VerifyWrite<Decimal>("Loc214m", view, view.Capacity - 16, 0.216082743618029m);

                    // Exceptions
                    foreach (Type type in testedTypes)
                    {
                        // position <0
                        VerifyWriteException("Loc231", type, view, -1, typeof(ArgumentOutOfRangeException));
                        // position >= view.Capacity
                        VerifyWriteException("Loc232", type, view, view.Capacity, typeof(ArgumentOutOfRangeException));
                        VerifyWriteException("Loc233", type, view, view.Capacity + 1, typeof(ArgumentOutOfRangeException));
                    }

                    // position+sizeof(type) >= view.Capacity
                    foreach (Type type in twoByteTypes)
                        VerifyWriteException("Loc234", type, view, view.Capacity - 1, typeof(ArgumentException));
                    foreach (Type type in fourByteTypes)
                        VerifyWriteException("Loc235", type, view, view.Capacity - 3, typeof(ArgumentException));
                    foreach (Type type in eightByteTypes)
                        VerifyWriteException("Loc236", type, view, view.Capacity - 7, typeof(ArgumentException));
                    foreach (Type type in sixteenByteTypes)
                        VerifyWriteException("Loc237", type, view, view.Capacity - 15, typeof(ArgumentException));
                }

                // Accessor does not support reading
                using (MemoryMappedViewAccessor view = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read))
                {
                    foreach (Type t in testedTypes)
                    {
                        VerifyWriteException("Loc401_" + t.ToString(), t, view, 0, typeof(NotSupportedException));
                    }
                }

                // Call after view has been disposed
                MemoryMappedViewAccessor view1 = mmf.CreateViewAccessor();
                view1.Dispose();
                foreach (Type t in testedTypes)
                {
                    VerifyWriteException("Loc501_" + t.ToString(), t, view1, 0, typeof(ObjectDisposedException));
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
    public void VerifyWrite<T>(String strLoc, MemoryMappedViewAccessor view, long position, T value)
    {
        iCountTestcases++;
        try
        {
            // write the value
            MethodInfo writeMethod = GetWriteMethod(typeof(T));
            writeMethod.Invoke(view, new Object[] { position, value });

            // read the value and compare
            MethodInfo readMethod = GetReadMethod(typeof(T));
            T ret = (T)readMethod.Invoke(view, new Object[] { position });
            Eval<T>(value, ret, "ERROR, {0}_{1}:  Returned value was wrong.", strLoc, typeof(T).Name);

            // read the bytes and compare
            Byte[] expectedBytes = GetExpectedBytes(typeof(T), value);
            for (int i = 0; i < expectedBytes.Length; i++)
                Eval<Byte>(expectedBytes[i], view.ReadByte(position + i), "ERROR, {0}_{1}:  Byte at position {2} was wrong.", strLoc, typeof(T).Name, position + i);
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

    public void VerifyRead<T>(String strLoc, MemoryMappedViewAccessor view, long position, T expectedValue)
    {
        iCountTestcases++;
        try
        {
            MethodInfo readMethod = GetReadMethod(typeof(T));
            T ret = (T)readMethod.Invoke(view, new Object[] { position });
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

    public void VerifyWriteException(String strLoc, Type dataType, MemoryMappedViewAccessor view, long position, Type exceptionType)
    {
        iCountTestcases++;
        try
        {
            MethodInfo writeMethod = GetWriteMethod(dataType);
            writeMethod.Invoke(view, new Object[] { position, Convert.ChangeType(0, dataType) });
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

    public MethodInfo GetReadMethod(Type dataType)
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

    public MethodInfo GetWriteMethod(Type dataType)
    {
        Type viewType = typeof(MemoryMappedViewAccessor);
        MethodInfo method = null;
        if (dataType == typeof(Boolean))
            method = viewType.GetMethod("Write", new Type[] { typeof(long), typeof(Boolean) });
        else if (dataType == typeof(Byte))
            method = viewType.GetMethod("Write", new Type[] { typeof(long), typeof(Byte) });
        else if (dataType == typeof(Char))
            method = viewType.GetMethod("Write", new Type[] { typeof(long), typeof(Char) });
        else if (dataType == typeof(Decimal))
            method = viewType.GetMethod("Write", new Type[] { typeof(long), typeof(Decimal) });
        else if (dataType == typeof(Single))
            method = viewType.GetMethod("Write", new Type[] { typeof(long), typeof(Single) });
        else if (dataType == typeof(Double))
            method = viewType.GetMethod("Write", new Type[] { typeof(long), typeof(Double) });
        else if (dataType == typeof(Int16))
            method = viewType.GetMethod("Write", new Type[] { typeof(long), typeof(Int16) });
        else if (dataType == typeof(Int32))
            method = viewType.GetMethod("Write", new Type[] { typeof(long), typeof(Int32) });
        else if (dataType == typeof(Int64))
            method = viewType.GetMethod("Write", new Type[] { typeof(long), typeof(Int64) });
        else if (dataType == typeof(SByte))
            method = viewType.GetMethod("Write", new Type[] { typeof(long), typeof(SByte) });
        else if (dataType == typeof(UInt16))
            method = viewType.GetMethod("Write", new Type[] { typeof(long), typeof(UInt16) });
        else if (dataType == typeof(UInt32))
            method = viewType.GetMethod("Write", new Type[] { typeof(long), typeof(UInt32) });
        else if (dataType == typeof(UInt64))
            method = viewType.GetMethod("Write", new Type[] { typeof(long), typeof(UInt64) });
        else
            throw new Exception("Unknown type " + dataType.ToString() + "!");

        if (method == null)
            throw new Exception("No Write method found for type " + dataType.ToString() + "!");
        else
            return method;
    }

    public Byte[] GetExpectedBytes(Type dataType, Object value)
    {
        Byte[] expectedBytes = null;
        if (dataType == typeof(Boolean))
            expectedBytes = BitConverter.GetBytes((Boolean)value);
        else if (dataType == typeof(Byte))
            expectedBytes = new Byte[] { (Byte)value };
        else if (dataType == typeof(SByte))
            expectedBytes = new Byte[] { (Byte)((SByte)value) };
        else if (dataType == typeof(Char))
            expectedBytes = BitConverter.GetBytes((Char)value);
        else if (dataType == typeof(Int16))
            expectedBytes = BitConverter.GetBytes((Int16)value);
        else if (dataType == typeof(UInt16))
            expectedBytes = BitConverter.GetBytes((UInt16)value);
        else if (dataType == typeof(Int32))
            expectedBytes = BitConverter.GetBytes((Int32)value);
        else if (dataType == typeof(UInt32))
            expectedBytes = BitConverter.GetBytes((UInt32)value);
        else if (dataType == typeof(Int64))
            expectedBytes = BitConverter.GetBytes((Int64)value);
        else if (dataType == typeof(UInt64))
            expectedBytes = BitConverter.GetBytes((UInt64)value);
        else if (dataType == typeof(Single))
            expectedBytes = BitConverter.GetBytes((Single)value);
        else if (dataType == typeof(Double))
            expectedBytes = BitConverter.GetBytes((Double)value);
        else if (dataType == typeof(Decimal))
        {
            Int32[] expectedInts = Decimal.GetBits((Decimal)value);
            expectedBytes = new Byte[16];
            Array.Copy(BitConverter.GetBytes(expectedInts[0]), 0, expectedBytes, 0, 4);
            Array.Copy(BitConverter.GetBytes(expectedInts[1]), 0, expectedBytes, 4, 4);
            Array.Copy(BitConverter.GetBytes(expectedInts[2]), 0, expectedBytes, 8, 4);
            Array.Copy(BitConverter.GetBytes(expectedInts[3]), 0, expectedBytes, 12, 4);
        }
        else
            throw new Exception("Unknown type " + dataType.ToString() + "!");

        return expectedBytes;
    }

    int RandPos()
    {
        return _random.Next(0, (int)_pageSize * 10 - 100);
    }
}
