// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

[StructLayout(LayoutKind.Sequential, Pack = 8, Size = 8)]
struct MyVector64<T> where T : struct { }

[StructLayout(LayoutKind.Sequential, Pack = 16, Size = 16)]
struct MyVector128<T> where T : struct { }

[StructLayout(LayoutKind.Sequential, Pack = 32, Size = 32)]
struct MyVector256<T> where T : struct { }

interface ITestStructure
{
    int Size { get; }
    int OffsetOfByte { get; }
    int OffsetOfValue { get; }
}

struct DefaultLayoutDefaultPacking<T> : ITestStructure
{
    public byte _byte;
    public T _value;

    public int Size => Unsafe.SizeOf<DefaultLayoutDefaultPacking<T>>();
    public int OffsetOfByte => Program.OffsetOf(ref this, ref _byte);
    public int OffsetOfValue => Program.OffsetOf(ref this, ref _value);
}

[StructLayout(LayoutKind.Sequential)]
struct SequentialLayoutDefaultPacking<T> : ITestStructure
{
    public byte _byte;
    public T _value;

    public int Size => Unsafe.SizeOf<SequentialLayoutDefaultPacking<T>>();
    public int OffsetOfByte => Program.OffsetOf(ref this, ref _byte);
    public int OffsetOfValue => Program.OffsetOf(ref this, ref _value);
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct SequentialLayoutMinPacking<T> : ITestStructure
{
    public byte _byte;
    public T _value;

    public int Size => Unsafe.SizeOf<SequentialLayoutMinPacking<T>>();
    public int OffsetOfByte => Program.OffsetOf(ref this, ref _byte);
    public int OffsetOfValue => Program.OffsetOf(ref this, ref _value);
}

[StructLayout(LayoutKind.Sequential, Pack = 128)]
struct SequentialLayoutMaxPacking<T> : ITestStructure
{
    public byte _byte;
    public T _value;

    public int Size => Unsafe.SizeOf<SequentialLayoutMaxPacking<T>>();
    public int OffsetOfByte => Program.OffsetOf(ref this, ref _byte);
    public int OffsetOfValue => Program.OffsetOf(ref this, ref _value);
}

[StructLayout(LayoutKind.Auto)]
struct AutoLayoutDefaultPacking<T> : ITestStructure
{
    public byte _byte;
    public T _value;

    public int Size => Unsafe.SizeOf<AutoLayoutDefaultPacking<T>>();
    public int OffsetOfByte => Program.OffsetOf(ref this, ref _byte);
    public int OffsetOfValue => Program.OffsetOf(ref this, ref _value);
}

[StructLayout(LayoutKind.Auto, Pack = 1)]
struct AutoLayoutMinPacking<T> : ITestStructure
{
    public byte _byte;
    public T _value;

    public int Size => Unsafe.SizeOf<AutoLayoutMinPacking<T>>();
    public int OffsetOfByte => Program.OffsetOf(ref this, ref _byte);
    public int OffsetOfValue => Program.OffsetOf(ref this, ref _value);
}

[StructLayout(LayoutKind.Auto, Pack = 128)]
struct AutoLayoutMaxPacking<T> : ITestStructure
{
    public byte _byte;
    public T _value;

    public int Size => Unsafe.SizeOf<AutoLayoutMaxPacking<T>>();
    public int OffsetOfByte => Program.OffsetOf(ref this, ref _byte);
    public int OffsetOfValue => Program.OffsetOf(ref this, ref _value);
}

unsafe class Program
{
    const int Pass = 100;
    const int Fail = 0;

    static int Main(string[] args)
    {
        bool succeeded = true;

        // Test fundamental data types
        succeeded &= TestBoolean();
        succeeded &= TestByte();
        succeeded &= TestChar();
        succeeded &= TestDouble();
        succeeded &= TestInt16();
        succeeded &= TestInt32();
        succeeded &= TestInt64();
        succeeded &= TestIntPtr();
        succeeded &= TestSByte();
        succeeded &= TestSingle();
        succeeded &= TestUInt16();
        succeeded &= TestUInt32();
        succeeded &= TestUInt64();
        succeeded &= TestUIntPtr();
        succeeded &= TestVector64();
        succeeded &= TestVector128();
        succeeded &= TestVector256();

        // Test custom data types with explicit size/packing 
        succeeded &= TestMyVector64();
        succeeded &= TestMyVector128();
        succeeded &= TestMyVector256();

        return succeeded ? Pass : Fail;
    }

    static bool TestBoolean()
    {
        bool succeeded = true;

        succeeded &= Test<DefaultLayoutDefaultPacking<bool>>(
            expectedSize: 2,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        succeeded &= Test<SequentialLayoutDefaultPacking<bool>>(
            expectedSize: 2,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        succeeded &= Test<SequentialLayoutMinPacking<bool>>(
            expectedSize: 2,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        succeeded &= Test<SequentialLayoutMaxPacking<bool>>(
            expectedSize: 2,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        succeeded &= Test<AutoLayoutDefaultPacking<bool>>(
            expectedSize: 2,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        succeeded &= Test<AutoLayoutMinPacking<bool>>(
            expectedSize: 2,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        succeeded &= Test<AutoLayoutMaxPacking<bool>>(
            expectedSize: 2,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        return succeeded;
    }

    static bool TestByte()
    {
        bool succeeded = true;

        succeeded &= Test<DefaultLayoutDefaultPacking<byte>>(
            expectedSize: 2,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        succeeded &= Test<SequentialLayoutDefaultPacking<byte>>(
            expectedSize: 2,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        succeeded &= Test<SequentialLayoutMinPacking<byte>>(
            expectedSize: 2,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        succeeded &= Test<SequentialLayoutMaxPacking<byte>>(
            expectedSize: 2,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        succeeded &= Test<AutoLayoutDefaultPacking<byte>>(
            expectedSize: 2,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        succeeded &= Test<AutoLayoutMinPacking<byte>>(
            expectedSize: 2,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        succeeded &= Test<AutoLayoutMaxPacking<byte>>(
            expectedSize: 2,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        return succeeded;
    }

    static bool TestChar()
    {
        bool succeeded = true;

        succeeded &= Test<DefaultLayoutDefaultPacking<char>>(
            expectedSize: 4,
            expectedOffsetByte: 0,
            expectedOffsetValue: 2
        );

        succeeded &= Test<SequentialLayoutDefaultPacking<char>>(
            expectedSize: 4,
            expectedOffsetByte: 0,
            expectedOffsetValue: 2
        );

        succeeded &= Test<SequentialLayoutMinPacking<char>>(
            expectedSize: 3,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        succeeded &= Test<SequentialLayoutMaxPacking<char>>(
            expectedSize: 4,
            expectedOffsetByte: 0,
            expectedOffsetValue: 2
        );

        succeeded &= Test<AutoLayoutDefaultPacking<char>>(
            expectedSize: 4,
            expectedOffsetByte: 2,
            expectedOffsetValue: 0
        );

        succeeded &= Test<AutoLayoutMinPacking<char>>(
            expectedSize: 4,
            expectedOffsetByte: 2,
            expectedOffsetValue: 0
        );

        succeeded &= Test<AutoLayoutMaxPacking<char>>(
            expectedSize: 4,
            expectedOffsetByte: 2,
            expectedOffsetValue: 0
        );

        return succeeded;
    }

    static bool TestDouble()
    {
        bool succeeded = true;

        if (OperatingSystem.IsWindows() || (RuntimeInformation.ProcessArchitecture != Architecture.X86))
        {
            succeeded &= Test<DefaultLayoutDefaultPacking<double>>(
                expectedSize: 16,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );

            succeeded &= Test<SequentialLayoutDefaultPacking<double>>(
                expectedSize: 16,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );

            succeeded &= Test<SequentialLayoutMinPacking<double>>(
                expectedSize: 9,
                expectedOffsetByte: 0,
                expectedOffsetValue: 1
            );

            succeeded &= Test<SequentialLayoutMaxPacking<double>>(
                expectedSize: 16,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );

            if (RuntimeInformation.ProcessArchitecture != Architecture.X86)
            {
                succeeded &= Test<AutoLayoutDefaultPacking<double>>(
                    expectedSize: 16,
                    expectedOffsetByte: 8,
                    expectedOffsetValue: 0
                );

                succeeded &= Test<AutoLayoutMinPacking<double>>(
                    expectedSize: 16,
                    expectedOffsetByte: 8,
                    expectedOffsetValue: 0
                );

                succeeded &= Test<AutoLayoutMaxPacking<double>>(
                    expectedSize: 16,
                    expectedOffsetByte: 8,
                    expectedOffsetValue: 0
                );
            }
            else
            {
                succeeded &= Test<AutoLayoutDefaultPacking<double>>(
                    expectedSize: 12,
                    expectedOffsetByte: 8,
                    expectedOffsetValue: 0
                );

                succeeded &= Test<AutoLayoutMinPacking<double>>(
                    expectedSize: 12,
                    expectedOffsetByte: 8,
                    expectedOffsetValue: 0
                );

                succeeded &= Test<AutoLayoutMaxPacking<double>>(
                    expectedSize: 12,
                    expectedOffsetByte: 8,
                    expectedOffsetValue: 0
                );
            }
        }
        else
        {
            // The System V ABI for i386 defines this type as having 4-byte alignment

            succeeded &= Test<DefaultLayoutDefaultPacking<double>>(
                expectedSize: 12,
                expectedOffsetByte: 0,
                expectedOffsetValue: 4
            );

            succeeded &= Test<SequentialLayoutDefaultPacking<double>>(
                expectedSize: 12,
                expectedOffsetByte: 0,
                expectedOffsetValue: 4
            );

            succeeded &= Test<SequentialLayoutMinPacking<double>>(
                expectedSize: 9,
                expectedOffsetByte: 0,
                expectedOffsetValue: 1
            );

            succeeded &= Test<SequentialLayoutMaxPacking<double>>(
                expectedSize: 12,
                expectedOffsetByte: 0,
                expectedOffsetValue: 4
            );

            succeeded &= Test<AutoLayoutDefaultPacking<double>>(
                expectedSize: 12,
                expectedOffsetByte: 8,
                expectedOffsetValue: 0
            );

            succeeded &= Test<AutoLayoutMinPacking<double>>(
                expectedSize: 12,
                expectedOffsetByte: 8,
                expectedOffsetValue: 0
            );

            succeeded &= Test<AutoLayoutMaxPacking<double>>(
                expectedSize: 12,
                expectedOffsetByte: 8,
                expectedOffsetValue: 0
            );
        }

        return succeeded;
    }

    static bool TestInt16()
    {
        bool succeeded = true;

        succeeded &= Test<DefaultLayoutDefaultPacking<short>>(
            expectedSize: 4,
            expectedOffsetByte: 0,
            expectedOffsetValue: 2
        );

        succeeded &= Test<SequentialLayoutDefaultPacking<short>>(
            expectedSize: 4,
            expectedOffsetByte: 0,
            expectedOffsetValue: 2
        );

        succeeded &= Test<SequentialLayoutMinPacking<short>>(
            expectedSize: 3,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        succeeded &= Test<SequentialLayoutMaxPacking<short>>(
            expectedSize: 4,
            expectedOffsetByte: 0,
            expectedOffsetValue: 2
        );

        succeeded &= Test<AutoLayoutDefaultPacking<short>>(
            expectedSize: 4,
            expectedOffsetByte: 2,
            expectedOffsetValue: 0
        );

        succeeded &= Test<AutoLayoutMinPacking<short>>(
            expectedSize: 4,
            expectedOffsetByte: 2,
            expectedOffsetValue: 0
        );

        succeeded &= Test<AutoLayoutMaxPacking<short>>(
            expectedSize: 4,
            expectedOffsetByte: 2,
            expectedOffsetValue: 0
        );

        return succeeded;
    }

    static bool TestInt32()
    {
        bool succeeded = true;

        succeeded &= Test<DefaultLayoutDefaultPacking<int>>(
            expectedSize: 8,
            expectedOffsetByte: 0,
            expectedOffsetValue: 4
        );

        succeeded &= Test<SequentialLayoutDefaultPacking<int>>(
            expectedSize: 8,
            expectedOffsetByte: 0,
            expectedOffsetValue: 4
        );

        succeeded &= Test<SequentialLayoutMinPacking<int>>(
            expectedSize: 5,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        succeeded &= Test<SequentialLayoutMaxPacking<int>>(
            expectedSize: 8,
            expectedOffsetByte: 0,
            expectedOffsetValue: 4
        );

        succeeded &= Test<AutoLayoutDefaultPacking<int>>(
            expectedSize: 8,
            expectedOffsetByte: 4,
            expectedOffsetValue: 0
        );

        succeeded &= Test<AutoLayoutMinPacking<int>>(
            expectedSize: 8,
            expectedOffsetByte: 4,
            expectedOffsetValue: 0
        );

        succeeded &= Test<AutoLayoutMaxPacking<int>>(
            expectedSize: 8,
            expectedOffsetByte: 4,
            expectedOffsetValue: 0
        );

        return succeeded;
    }

    static bool TestInt64()
    {
        bool succeeded = true;

        if (OperatingSystem.IsWindows() || (RuntimeInformation.ProcessArchitecture != Architecture.X86))
        {
            succeeded &= Test<DefaultLayoutDefaultPacking<long>>(
                expectedSize: 16,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );

            succeeded &= Test<SequentialLayoutDefaultPacking<long>>(
                expectedSize: 16,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );

            succeeded &= Test<SequentialLayoutMinPacking<long>>(
                expectedSize: 9,
                expectedOffsetByte: 0,
                expectedOffsetValue: 1
            );

            succeeded &= Test<SequentialLayoutMaxPacking<long>>(
                expectedSize: 16,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );

            if (RuntimeInformation.ProcessArchitecture != Architecture.X86)
            {
                succeeded &= Test<AutoLayoutDefaultPacking<long>>(
                    expectedSize: 16,
                    expectedOffsetByte: 8,
                    expectedOffsetValue: 0
                );

                succeeded &= Test<AutoLayoutMinPacking<long>>(
                    expectedSize: 16,
                    expectedOffsetByte: 8,
                    expectedOffsetValue: 0
                );

                succeeded &= Test<AutoLayoutMaxPacking<long>>(
                    expectedSize: 16,
                    expectedOffsetByte: 8,
                    expectedOffsetValue: 0
                );
            }
            else
            {
                succeeded &= Test<AutoLayoutDefaultPacking<long>>(
                    expectedSize: 12,
                    expectedOffsetByte: 8,
                    expectedOffsetValue: 0
                );

                succeeded &= Test<AutoLayoutMinPacking<long>>(
                    expectedSize: 12,
                    expectedOffsetByte: 8,
                    expectedOffsetValue: 0
                );

                succeeded &= Test<AutoLayoutMaxPacking<long>>(
                    expectedSize: 12,
                    expectedOffsetByte: 8,
                    expectedOffsetValue: 0
                );
            }
        }
        else
        {
            // The System V ABI for i386 defines this type as having 4-byte alignment

            succeeded &= Test<DefaultLayoutDefaultPacking<long>>(
                expectedSize: 12,
                expectedOffsetByte: 0,
                expectedOffsetValue: 4
            );

            succeeded &= Test<SequentialLayoutDefaultPacking<long>>(
                expectedSize: 12,
                expectedOffsetByte: 0,
                expectedOffsetValue: 4
            );

            succeeded &= Test<SequentialLayoutMinPacking<long>>(
                expectedSize: 9,
                expectedOffsetByte: 0,
                expectedOffsetValue: 1
            );

            succeeded &= Test<SequentialLayoutMaxPacking<long>>(
                expectedSize: 12,
                expectedOffsetByte: 0,
                expectedOffsetValue: 4
            );

            succeeded &= Test<AutoLayoutDefaultPacking<long>>(
                expectedSize: 12,
                expectedOffsetByte: 8,
                expectedOffsetValue: 0
            );

            succeeded &= Test<AutoLayoutMinPacking<long>>(
                expectedSize: 12,
                expectedOffsetByte: 8,
                expectedOffsetValue: 0
            );

            succeeded &= Test<AutoLayoutMaxPacking<long>>(
                expectedSize: 12,
                expectedOffsetByte: 8,
                expectedOffsetValue: 0
            );
        }

        return succeeded;
    }

    static bool TestIntPtr()
    {
        bool succeeded = true;

        if (Environment.Is64BitProcess)
        {
            succeeded &= Test<DefaultLayoutDefaultPacking<IntPtr>>(
                expectedSize: 16,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );

            succeeded &= Test<SequentialLayoutDefaultPacking<IntPtr>>(
                expectedSize: 16,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );

            succeeded &= Test<SequentialLayoutMinPacking<IntPtr>>(
                expectedSize: 9,
                expectedOffsetByte: 0,
                expectedOffsetValue: 1
            );

            succeeded &= Test<SequentialLayoutMaxPacking<IntPtr>>(
                expectedSize: 16,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );

            succeeded &= Test<AutoLayoutDefaultPacking<IntPtr>>(
                expectedSize: 16,
                expectedOffsetByte: 8,
                expectedOffsetValue: 0
            );

            succeeded &= Test<AutoLayoutMinPacking<IntPtr>>(
                expectedSize: 16,
                expectedOffsetByte: 8,
                expectedOffsetValue: 0
            );

            succeeded &= Test<AutoLayoutMaxPacking<IntPtr>>(
                expectedSize: 16,
                expectedOffsetByte: 8,
                expectedOffsetValue: 0
            );
        }
        else
        {
            succeeded &= Test<DefaultLayoutDefaultPacking<IntPtr>>(
                expectedSize: 8,
                expectedOffsetByte: 0,
                expectedOffsetValue: 4
            );

            succeeded &= Test<SequentialLayoutDefaultPacking<IntPtr>>(
                expectedSize: 8,
                expectedOffsetByte: 0,
                expectedOffsetValue: 4
            );

            succeeded &= Test<SequentialLayoutMinPacking<IntPtr>>(
                expectedSize: 5,
                expectedOffsetByte: 0,
                expectedOffsetValue: 1
            );

            succeeded &= Test<SequentialLayoutMaxPacking<IntPtr>>(
                expectedSize: 8,
                expectedOffsetByte: 0,
                expectedOffsetValue: 4
            );

            succeeded &= Test<AutoLayoutDefaultPacking<IntPtr>>(
                expectedSize: 8,
                expectedOffsetByte: 4,
                expectedOffsetValue: 0
            );

            succeeded &= Test<AutoLayoutMinPacking<IntPtr>>(
                expectedSize: 8,
                expectedOffsetByte: 4,
                expectedOffsetValue: 0
            );

            succeeded &= Test<AutoLayoutMaxPacking<IntPtr>>(
                expectedSize: 8,
                expectedOffsetByte: 4,
                expectedOffsetValue: 0
            );
        }

        return succeeded;
    }

    static bool TestSByte()
    {
        bool succeeded = true;

        succeeded &= Test<DefaultLayoutDefaultPacking<sbyte>>(
            expectedSize: 2,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        succeeded &= Test<SequentialLayoutDefaultPacking<sbyte>>(
            expectedSize: 2,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        succeeded &= Test<SequentialLayoutMinPacking<sbyte>>(
            expectedSize: 2,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        succeeded &= Test<SequentialLayoutMaxPacking<sbyte>>(
            expectedSize: 2,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        succeeded &= Test<AutoLayoutDefaultPacking<sbyte>>(
            expectedSize: 2,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        succeeded &= Test<AutoLayoutMinPacking<sbyte>>(
            expectedSize: 2,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        succeeded &= Test<AutoLayoutMaxPacking<sbyte>>(
            expectedSize: 2,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        return succeeded;
    }

    static bool TestSingle()
    {
        bool succeeded = true;

        succeeded &= Test<DefaultLayoutDefaultPacking<float>>(
            expectedSize: 8,
            expectedOffsetByte: 0,
            expectedOffsetValue: 4
        );

        succeeded &= Test<SequentialLayoutDefaultPacking<float>>(
            expectedSize: 8,
            expectedOffsetByte: 0,
            expectedOffsetValue: 4
        );

        succeeded &= Test<SequentialLayoutMinPacking<float>>(
            expectedSize: 5,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        succeeded &= Test<SequentialLayoutMaxPacking<float>>(
            expectedSize: 8,
            expectedOffsetByte: 0,
            expectedOffsetValue: 4
        );

        succeeded &= Test<AutoLayoutDefaultPacking<float>>(
            expectedSize: 8,
            expectedOffsetByte: 4,
            expectedOffsetValue: 0
        );

        succeeded &= Test<AutoLayoutMinPacking<float>>(
            expectedSize: 8,
            expectedOffsetByte: 4,
            expectedOffsetValue: 0
        );

        succeeded &= Test<AutoLayoutMaxPacking<float>>(
            expectedSize: 8,
            expectedOffsetByte: 4,
            expectedOffsetValue: 0
        );

        return succeeded;
    }

    static bool TestUInt16()
    {
        bool succeeded = true;

        succeeded &= Test<DefaultLayoutDefaultPacking<ushort>>(
            expectedSize: 4,
            expectedOffsetByte: 0,
            expectedOffsetValue: 2
        );

        succeeded &= Test<SequentialLayoutDefaultPacking<ushort>>(
            expectedSize: 4,
            expectedOffsetByte: 0,
            expectedOffsetValue: 2
        );

        succeeded &= Test<SequentialLayoutMinPacking<ushort>>(
            expectedSize: 3,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        succeeded &= Test<SequentialLayoutMaxPacking<ushort>>(
            expectedSize: 4,
            expectedOffsetByte: 0,
            expectedOffsetValue: 2
        );

        succeeded &= Test<AutoLayoutDefaultPacking<ushort>>(
            expectedSize: 4,
            expectedOffsetByte: 2,
            expectedOffsetValue: 0
        );

        succeeded &= Test<AutoLayoutMinPacking<ushort>>(
            expectedSize: 4,
            expectedOffsetByte: 2,
            expectedOffsetValue: 0
        );

        succeeded &= Test<AutoLayoutMaxPacking<ushort>>(
            expectedSize: 4,
            expectedOffsetByte: 2,
            expectedOffsetValue: 0
        );

        return succeeded;
    }

    static bool TestUInt32()
    {
        bool succeeded = true;

        succeeded &= Test<DefaultLayoutDefaultPacking<uint>>(
            expectedSize: 8,
            expectedOffsetByte: 0,
            expectedOffsetValue: 4
        );

        succeeded &= Test<SequentialLayoutDefaultPacking<uint>>(
            expectedSize: 8,
            expectedOffsetByte: 0,
            expectedOffsetValue: 4
        );

        succeeded &= Test<SequentialLayoutMinPacking<uint>>(
            expectedSize: 5,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        succeeded &= Test<SequentialLayoutMaxPacking<uint>>(
            expectedSize: 8,
            expectedOffsetByte: 0,
            expectedOffsetValue: 4
        );

        succeeded &= Test<AutoLayoutDefaultPacking<uint>>(
            expectedSize: 8,
            expectedOffsetByte: 4,
            expectedOffsetValue: 0
        );

        succeeded &= Test<AutoLayoutMinPacking<uint>>(
            expectedSize: 8,
            expectedOffsetByte: 4,
            expectedOffsetValue: 0
        );

        succeeded &= Test<AutoLayoutMaxPacking<uint>>(
            expectedSize: 8,
            expectedOffsetByte: 4,
            expectedOffsetValue: 0
        );

        return succeeded;
    }

    static bool TestUInt64()
    {
        bool succeeded = true;

        if (OperatingSystem.IsWindows() || (RuntimeInformation.ProcessArchitecture != Architecture.X86))
        {
            succeeded &= Test<DefaultLayoutDefaultPacking<ulong>>(
                expectedSize: 16,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );

            succeeded &= Test<SequentialLayoutDefaultPacking<ulong>>(
                expectedSize: 16,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );

            succeeded &= Test<SequentialLayoutMinPacking<ulong>>(
                expectedSize: 9,
                expectedOffsetByte: 0,
                expectedOffsetValue: 1
            );

            succeeded &= Test<SequentialLayoutMaxPacking<ulong>>(
                expectedSize: 16,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );

            if (RuntimeInformation.ProcessArchitecture != Architecture.X86)
            {
                succeeded &= Test<AutoLayoutDefaultPacking<ulong>>(
                    expectedSize: 16,
                    expectedOffsetByte: 8,
                    expectedOffsetValue: 0
                );

                succeeded &= Test<AutoLayoutMinPacking<ulong>>(
                    expectedSize: 16,
                    expectedOffsetByte: 8,
                    expectedOffsetValue: 0
                );

                succeeded &= Test<AutoLayoutMaxPacking<ulong>>(
                    expectedSize: 16,
                    expectedOffsetByte: 8,
                    expectedOffsetValue: 0
                );
            }
            else
            {
                succeeded &= Test<AutoLayoutDefaultPacking<ulong>>(
                    expectedSize: 12,
                    expectedOffsetByte: 8,
                    expectedOffsetValue: 0
                );

                succeeded &= Test<AutoLayoutMinPacking<ulong>>(
                    expectedSize: 12,
                    expectedOffsetByte: 8,
                    expectedOffsetValue: 0
                );

                succeeded &= Test<AutoLayoutMaxPacking<ulong>>(
                    expectedSize: 12,
                    expectedOffsetByte: 8,
                    expectedOffsetValue: 0
                );
            }
        }
        else
        {
            // The System V ABI for i386 defines this type as having 4-byte alignment

            succeeded &= Test<DefaultLayoutDefaultPacking<ulong>>(
                expectedSize: 12,
                expectedOffsetByte: 0,
                expectedOffsetValue: 4
            );

            succeeded &= Test<SequentialLayoutDefaultPacking<ulong>>(
                expectedSize: 12,
                expectedOffsetByte: 0,
                expectedOffsetValue: 4
            );

            succeeded &= Test<SequentialLayoutMinPacking<ulong>>(
                expectedSize: 9,
                expectedOffsetByte: 0,
                expectedOffsetValue: 1
            );

            succeeded &= Test<SequentialLayoutMaxPacking<ulong>>(
                expectedSize: 12,
                expectedOffsetByte: 0,
                expectedOffsetValue: 4
            );

            succeeded &= Test<AutoLayoutDefaultPacking<ulong>>(
                expectedSize: 12,
                expectedOffsetByte: 8,
                expectedOffsetValue: 0
            );

            succeeded &= Test<AutoLayoutMinPacking<ulong>>(
                expectedSize: 12,
                expectedOffsetByte: 8,
                expectedOffsetValue: 0
            );

            succeeded &= Test<AutoLayoutMaxPacking<ulong>>(
                expectedSize: 12,
                expectedOffsetByte: 8,
                expectedOffsetValue: 0
            );
        }

        return succeeded;
    }

    static bool TestUIntPtr()
    {
        bool succeeded = true;

        if (Environment.Is64BitProcess)
        {
            succeeded &= Test<DefaultLayoutDefaultPacking<UIntPtr>>(
                expectedSize: 16,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );

            succeeded &= Test<SequentialLayoutDefaultPacking<UIntPtr>>(
                expectedSize: 16,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );

            succeeded &= Test<SequentialLayoutMinPacking<UIntPtr>>(
                expectedSize: 9,
                expectedOffsetByte: 0,
                expectedOffsetValue: 1
            );

            succeeded &= Test<SequentialLayoutMaxPacking<UIntPtr>>(
                expectedSize: 16,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );

            succeeded &= Test<AutoLayoutDefaultPacking<UIntPtr>>(
                expectedSize: 16,
                expectedOffsetByte: 8,
                expectedOffsetValue: 0
            );

            succeeded &= Test<AutoLayoutMinPacking<UIntPtr>>(
                expectedSize: 16,
                expectedOffsetByte: 8,
                expectedOffsetValue: 0
            );

            succeeded &= Test<AutoLayoutMaxPacking<UIntPtr>>(
                expectedSize: 16,
                expectedOffsetByte: 8,
                expectedOffsetValue: 0
            );
        }
        else
        {
            succeeded &= Test<DefaultLayoutDefaultPacking<UIntPtr>>(
                expectedSize: 8,
                expectedOffsetByte: 0,
                expectedOffsetValue: 4
            );

            succeeded &= Test<SequentialLayoutDefaultPacking<UIntPtr>>(
                expectedSize: 8,
                expectedOffsetByte: 0,
                expectedOffsetValue: 4
            );

            succeeded &= Test<SequentialLayoutMinPacking<UIntPtr>>(
                expectedSize: 5,
                expectedOffsetByte: 0,
                expectedOffsetValue: 1
            );

            succeeded &= Test<SequentialLayoutMaxPacking<UIntPtr>>(
                expectedSize: 8,
                expectedOffsetByte: 0,
                expectedOffsetValue: 4
            );

            succeeded &= Test<AutoLayoutDefaultPacking<UIntPtr>>(
                expectedSize: 8,
                expectedOffsetByte: 4,
                expectedOffsetValue: 0
            );

            succeeded &= Test<AutoLayoutMinPacking<UIntPtr>>(
                expectedSize: 8,
                expectedOffsetByte: 4,
                expectedOffsetValue: 0
            );

            succeeded &= Test<AutoLayoutMaxPacking<UIntPtr>>(
                expectedSize: 8,
                expectedOffsetByte: 4,
                expectedOffsetValue: 0
            );
        }

        return succeeded;
    }

    static bool TestVector64()
    {
        bool succeeded = true;

        succeeded &= Test<DefaultLayoutDefaultPacking<Vector64<byte>>>(
            expectedSize: 16,
            expectedOffsetByte: 0,
            expectedOffsetValue: 8
        );
        
        succeeded &= Test<SequentialLayoutDefaultPacking<Vector64<byte>>>(
            expectedSize: 16,
            expectedOffsetByte: 0,
            expectedOffsetValue: 8
        );
        
        succeeded &= Test<SequentialLayoutMinPacking<Vector64<byte>>>(
            expectedSize: 9,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );
        
        succeeded &= Test<SequentialLayoutMaxPacking<Vector64<byte>>>(
            expectedSize: 16,
            expectedOffsetByte: 0,
            expectedOffsetValue: 8
        );
        
        if (RuntimeInformation.ProcessArchitecture != Architecture.X86)
        {
            succeeded &= Test<AutoLayoutDefaultPacking<Vector64<byte>>>(
                expectedSize: 16,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );
        
            succeeded &= Test<AutoLayoutMinPacking<Vector64<byte>>>(
                expectedSize: 16,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );
        
            succeeded &= Test<AutoLayoutMaxPacking<Vector64<byte>>>(
                expectedSize: 16,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );
        }
        else
        {
            succeeded &= Test<AutoLayoutDefaultPacking<Vector64<byte>>>(
                expectedSize: 12,
                expectedOffsetByte: 0,
                expectedOffsetValue: 4
            );
        
            succeeded &= Test<AutoLayoutMinPacking<Vector64<byte>>>(
                expectedSize: 12,
                expectedOffsetByte: 0,
                expectedOffsetValue: 4
            );
        
            succeeded &= Test<AutoLayoutMaxPacking<Vector64<byte>>>(
                expectedSize: 12,
                expectedOffsetByte: 0,
                expectedOffsetValue: 4
            );
        }

        return succeeded;
    }

    static bool TestVector128()
    {
        bool succeeded = true;

        if (RuntimeInformation.ProcessArchitecture == Architecture.Arm)
        {
            // The Procedure Call Standard for ARM defines this type as having 8-byte alignment

            succeeded &= Test<DefaultLayoutDefaultPacking<Vector128<byte>>>(
                expectedSize: 24,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );

            succeeded &= Test<SequentialLayoutDefaultPacking<Vector128<byte>>>(
                expectedSize: 24,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );

            succeeded &= Test<SequentialLayoutMaxPacking<Vector128<byte>>>(
                expectedSize: 24,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );
        }
        else
        {
            succeeded &= Test<DefaultLayoutDefaultPacking<Vector128<byte>>>(
                expectedSize: 32,
                expectedOffsetByte: 0,
                expectedOffsetValue: 16
            );

            succeeded &= Test<SequentialLayoutDefaultPacking<Vector128<byte>>>(
                expectedSize: 32,
                expectedOffsetByte: 0,
                expectedOffsetValue: 16
            );

            succeeded &= Test<SequentialLayoutMaxPacking<Vector128<byte>>>(
                expectedSize: 32,
                expectedOffsetByte: 0,
                expectedOffsetValue: 16
            );
        }

        succeeded &= Test<SequentialLayoutMinPacking<Vector128<byte>>>(
            expectedSize: 17,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        if (RuntimeInformation.ProcessArchitecture != Architecture.X86)
        {
            succeeded &= Test<AutoLayoutDefaultPacking<Vector128<byte>>>(
                expectedSize: 24,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );

            succeeded &= Test<AutoLayoutMinPacking<Vector128<byte>>>(
                expectedSize: 24,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );

            succeeded &= Test<AutoLayoutMaxPacking<Vector128<byte>>>(
                expectedSize: 24,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );
        }
        else
        {
            succeeded &= Test<AutoLayoutDefaultPacking<Vector128<byte>>>(
                expectedSize: 20,
                expectedOffsetByte: 0,
                expectedOffsetValue: 4
            );

            succeeded &= Test<AutoLayoutMinPacking<Vector128<byte>>>(
                expectedSize: 20,
                expectedOffsetByte: 0,
                expectedOffsetValue: 4
            );

            succeeded &= Test<AutoLayoutMaxPacking<Vector128<byte>>>(
                expectedSize: 20,
                expectedOffsetByte: 0,
                expectedOffsetValue: 4
            );
        }

        return succeeded;
    }

    static bool TestVector256()
    {
        bool succeeded = true;

        if (RuntimeInformation.ProcessArchitecture == Architecture.Arm)
        {
            // The Procedure Call Standard for ARM defines this type as having 8-byte alignment

            succeeded &= Test<DefaultLayoutDefaultPacking<Vector256<byte>>>(
                expectedSize: 40,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );

            succeeded &= Test<SequentialLayoutDefaultPacking<Vector256<byte>>>(
                expectedSize: 40,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );

            succeeded &= Test<SequentialLayoutMaxPacking<Vector256<byte>>>(
                expectedSize: 40,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );
        }
        else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
        {
            // The Procedure Call Standard for ARM64 defines this type as having 16-byte alignment

            succeeded &= Test<DefaultLayoutDefaultPacking<Vector256<byte>>>(
                expectedSize: 48,
                expectedOffsetByte: 0,
                expectedOffsetValue: 16
            );

            succeeded &= Test<SequentialLayoutDefaultPacking<Vector256<byte>>>(
                expectedSize: 48,
                expectedOffsetByte: 0,
                expectedOffsetValue: 16
            );

            succeeded &= Test<SequentialLayoutMaxPacking<Vector256<byte>>>(
                expectedSize: 48,
                expectedOffsetByte: 0,
                expectedOffsetValue: 16
            );
        }
        else
        {
            succeeded &= Test<DefaultLayoutDefaultPacking<Vector256<byte>>>(
                expectedSize: 64,
                expectedOffsetByte: 0,
                expectedOffsetValue: 32
            );

            succeeded &= Test<SequentialLayoutDefaultPacking<Vector256<byte>>>(
                expectedSize: 64,
                expectedOffsetByte: 0,
                expectedOffsetValue: 32
            );

            succeeded &= Test<SequentialLayoutMaxPacking<Vector256<byte>>>(
                expectedSize: 64,
                expectedOffsetByte: 0,
                expectedOffsetValue: 32
            );
        }

        succeeded &= Test<SequentialLayoutMinPacking<Vector256<byte>>>(
            expectedSize: 33,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        if (RuntimeInformation.ProcessArchitecture != Architecture.X86)
        {
            succeeded &= Test<AutoLayoutDefaultPacking<Vector256<byte>>>(
                expectedSize: 40,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );

            succeeded &= Test<AutoLayoutMinPacking<Vector256<byte>>>(
                expectedSize: 40,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );

            succeeded &= Test<AutoLayoutMaxPacking<Vector256<byte>>>(
                expectedSize: 40,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );
        }
        else
        {
            succeeded &= Test<AutoLayoutDefaultPacking<Vector256<byte>>>(
                expectedSize: 36,
                expectedOffsetByte: 0,
                expectedOffsetValue: 4
            );

            succeeded &= Test<AutoLayoutMinPacking<Vector256<byte>>>(
                expectedSize: 36,
                expectedOffsetByte: 0,
                expectedOffsetValue: 4
            );

            succeeded &= Test<AutoLayoutMaxPacking<Vector256<byte>>>(
                expectedSize: 36,
                expectedOffsetByte: 0,
                expectedOffsetValue: 4
            );
        }

        return succeeded;
    }

    static bool TestMyVector64()
    {
        bool succeeded = true;

        succeeded &= Test<DefaultLayoutDefaultPacking<MyVector64<byte>>>(
            expectedSize: 9,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );
        
        succeeded &= Test<SequentialLayoutDefaultPacking<MyVector64<byte>>>(
            expectedSize: 9,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );
        
        succeeded &= Test<SequentialLayoutMinPacking<MyVector64<byte>>>(
            expectedSize: 9,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );
        
        succeeded &= Test<SequentialLayoutMaxPacking<MyVector64<byte>>>(
            expectedSize: 9,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );
        
        if (Environment.Is64BitProcess)
        {
            succeeded &= Test<AutoLayoutDefaultPacking<MyVector64<byte>>>(
                expectedSize: 16,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );
        
            succeeded &= Test<AutoLayoutMinPacking<MyVector64<byte>>>(
                expectedSize: 16,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );
        
            succeeded &= Test<AutoLayoutMaxPacking<MyVector64<byte>>>(
                expectedSize: 16,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );
        }
        else
        {
            succeeded &= Test<AutoLayoutDefaultPacking<MyVector64<byte>>>(
                expectedSize: 12,
                expectedOffsetByte: 0,
                expectedOffsetValue: 4
            );
        
            succeeded &= Test<AutoLayoutMinPacking<MyVector64<byte>>>(
                expectedSize: 12,
                expectedOffsetByte: 0,
                expectedOffsetValue: 4
            );
        
            succeeded &= Test<AutoLayoutMaxPacking<MyVector64<byte>>>(
                expectedSize: 12,
                expectedOffsetByte: 0,
                expectedOffsetValue: 4
            );
        }

        return succeeded;
    }

    static bool TestMyVector128()
    {
        bool succeeded = true;

        succeeded &= Test<DefaultLayoutDefaultPacking<MyVector128<byte>>>(
            expectedSize: 17,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        succeeded &= Test<SequentialLayoutDefaultPacking<MyVector128<byte>>>(
            expectedSize: 17,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        succeeded &= Test<SequentialLayoutMinPacking<MyVector128<byte>>>(
            expectedSize: 17,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        succeeded &= Test<SequentialLayoutMaxPacking<MyVector128<byte>>>(
            expectedSize: 17,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        if (Environment.Is64BitProcess)
        {
            succeeded &= Test<AutoLayoutDefaultPacking<MyVector128<byte>>>(
                expectedSize: 24,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );

            succeeded &= Test<AutoLayoutMinPacking<MyVector128<byte>>>(
                expectedSize: 24,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );

            succeeded &= Test<AutoLayoutMaxPacking<MyVector128<byte>>>(
                expectedSize: 24,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );
        }
        else
        {
            succeeded &= Test<AutoLayoutDefaultPacking<MyVector128<byte>>>(
                expectedSize: 20,
                expectedOffsetByte: 0,
                expectedOffsetValue: 4
            );

            succeeded &= Test<AutoLayoutMinPacking<MyVector128<byte>>>(
                expectedSize: 20,
                expectedOffsetByte: 0,
                expectedOffsetValue: 4
            );

            succeeded &= Test<AutoLayoutMaxPacking<MyVector128<byte>>>(
                expectedSize: 20,
                expectedOffsetByte: 0,
                expectedOffsetValue: 4
            );
        }

        return succeeded;
    }

    static bool TestMyVector256()
    {
        bool succeeded = true;

        succeeded &= Test<DefaultLayoutDefaultPacking<MyVector256<byte>>>(
            expectedSize: 33,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        succeeded &= Test<SequentialLayoutDefaultPacking<MyVector256<byte>>>(
            expectedSize: 33,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        succeeded &= Test<SequentialLayoutMinPacking<MyVector256<byte>>>(
            expectedSize: 33,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        succeeded &= Test<SequentialLayoutMaxPacking<MyVector256<byte>>>(
            expectedSize: 33,
            expectedOffsetByte: 0,
            expectedOffsetValue: 1
        );

        if (Environment.Is64BitProcess)
        {
            succeeded &= Test<AutoLayoutDefaultPacking<MyVector256<byte>>>(
                expectedSize: 40,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );

            succeeded &= Test<AutoLayoutMinPacking<MyVector256<byte>>>(
                expectedSize: 40,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );

            succeeded &= Test<AutoLayoutMaxPacking<MyVector256<byte>>>(
                expectedSize: 40,
                expectedOffsetByte: 0,
                expectedOffsetValue: 8
            );
        }
        else
        {
            succeeded &= Test<AutoLayoutDefaultPacking<MyVector256<byte>>>(
                expectedSize: 36,
                expectedOffsetByte: 0,
                expectedOffsetValue: 4
            );

            succeeded &= Test<AutoLayoutMinPacking<MyVector256<byte>>>(
                expectedSize: 36,
                expectedOffsetByte: 0,
                expectedOffsetValue: 4
            );

            succeeded &= Test<AutoLayoutMaxPacking<MyVector256<byte>>>(
                expectedSize: 36,
                expectedOffsetByte: 0,
                expectedOffsetValue: 4
            );
        }

        return succeeded;
    }

    static bool Test<T>(int expectedSize, int expectedOffsetByte, int expectedOffsetValue) where T : ITestStructure
    {
        bool succeeded = true;
        var testStructure = default(T);

        int size = testStructure.Size;
        if (size != expectedSize)
        {
            Console.WriteLine($"Unexpected Size for {testStructure.GetType()}.");
            Console.WriteLine($"     Expected: {expectedSize}; Actual: {size}");
            succeeded = false;
        }

        int offsetByte = testStructure.OffsetOfByte;
        if (offsetByte != expectedOffsetByte)
        {
            Console.WriteLine($"Unexpected Offset for {testStructure.GetType()}.Byte.");
            Console.WriteLine($"     Expected: {expectedOffsetByte}; Actual: {offsetByte}");
            succeeded = false;
        }

        int offsetValue = testStructure.OffsetOfValue;
        if (offsetValue != expectedOffsetValue)
        {
            Console.WriteLine($"Unexpected Offset for {testStructure.GetType()}.Value.");
            Console.WriteLine($"     Expected: {expectedOffsetValue}; Actual: {offsetValue}");
            succeeded = false;
        }

        if (!succeeded)
        {
            Console.WriteLine();
        }

        return succeeded;
    }

    internal static int OffsetOf<T, U>(ref T origin, ref U target)
    {
        return Unsafe.ByteOffset(ref origin, ref Unsafe.As<U, T>(ref target)).ToInt32();
    }
}
