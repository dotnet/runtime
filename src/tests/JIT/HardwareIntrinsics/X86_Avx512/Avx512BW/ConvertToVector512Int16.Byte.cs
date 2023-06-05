// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/******************************************************************************
 * This file is auto-generated from a template file by the GenerateTests.csx  *
 * script in tests\src\JIT\HardwareIntrinsics\X86\Shared. In order to make    *
 * changes, please update the corresponding template and run according to the *
 * directions listed in the file.                                             *
 ******************************************************************************/

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

namespace JIT.HardwareIntrinsics.X86._Avx512BW.handwritten
{
    public static partial class Program
    {
        [Fact]
        public static void ConvertToVector512Int16Byte()
        {
            var test = new SimpleUnaryOpTest__ConvertToVector512Int16Byte();

            if (test.IsSupported)
            {
                // Validates basic functionality works, using Unsafe.Read
                test.RunBasicScenario_UnsafeRead();

                if (Avx.IsSupported)
                {
                    // Validates basic functionality works, using Load
                    test.RunBasicScenario_Load();

                    // Validates basic functionality works, using LoadAligned
                    test.RunBasicScenario_LoadAligned();
                }

                // Validates calling via reflection works, using Unsafe.Read
                test.RunReflectionScenario_UnsafeRead();

                if (Avx.IsSupported)
                {
                    // Validates calling via reflection works, using Load
                    test.RunReflectionScenario_Load();

                    // Validates calling via reflection works, using LoadAligned
                    test.RunReflectionScenario_LoadAligned();
                }

                // Validates passing a static member works
                test.RunClsVarScenario();

                // Validates passing a local works, using Unsafe.Read
                test.RunLclVarScenario_UnsafeRead();

                if (Avx.IsSupported)
                {
                    // Validates passing a local works, using Load
                    test.RunLclVarScenario_Load();

                    // Validates passing a local works, using LoadAligned
                    test.RunLclVarScenario_LoadAligned();
                }

                // Validates passing the field of a local works
                test.RunLclFldScenario();

                // Validates passing an instance member works
                test.RunFldScenario();
            }
            else
            {
                // Validates we throw on unsupported hardware
                test.RunUnsupportedScenario();
            }

            if (!test.Succeeded)
            {
                throw new Exception("One or more scenarios did not complete as expected.");
            }
        }
    }

    public sealed unsafe class SimpleUnaryOpTest__ConvertToVector512Int16Byte
    {
        private const int VectorSize = 64;

        private const int Op1ElementCount = 32 / sizeof(Byte);
        private const int RetElementCount = VectorSize / sizeof(Int16);

        private static Byte[] _data = new Byte[Op1ElementCount];

        private static Vector256<Byte> _clsVar;

        private Vector256<Byte> _fld;

        private SimpleUnaryOpTest__DataTable<UInt16, Byte> _dataTable;

        static SimpleUnaryOpTest__ConvertToVector512Int16Byte()
        {
            var random = new Random();

            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = (byte)(random.Next(0, byte.MaxValue)); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<Byte>, byte>(ref _clsVar), ref Unsafe.As<Byte, byte>(ref _data[0]), 16);
        }

        public SimpleUnaryOpTest__ConvertToVector512Int16Byte()
        {
            Succeeded = true;

            var random = new Random();

            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = (byte)(random.Next(0, byte.MaxValue)); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<Byte>, byte>(ref _fld), ref Unsafe.As<Byte, byte>(ref _data[0]), 16);

            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = (byte)(random.Next(0, byte.MaxValue)); }
            _dataTable = new SimpleUnaryOpTest__DataTable<UInt16, Byte>(_data, new UInt16[RetElementCount], VectorSize);
        }

        public bool IsSupported => Avx512BW.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            var result = Avx512BW.ConvertToVector512Int16(
                Unsafe.Read<Vector256<Byte>>(_dataTable.inArrayPtr)
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_Load()
        {
            var result = Avx512BW.ConvertToVector512Int16(
                Avx.LoadVector256((Byte*)(_dataTable.inArrayPtr))
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_LoadAligned()
        {
            var result = Avx512BW.ConvertToVector512Int16(
                Avx.LoadAlignedVector256((Byte*)(_dataTable.inArrayPtr))
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            var result = typeof(Avx512BW).GetMethod(nameof(Avx512BW.ConvertToVector512Int16), new Type[] { typeof(Vector256<Byte>) })
                                     .Invoke(null, new object[] {
                                        Unsafe.Read<Vector256<Byte>>(_dataTable.inArrayPtr)
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector512<Int16>)(result));
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_Load()
        {
            var result = typeof(Avx512BW).GetMethod(nameof(Avx512BW.ConvertToVector512Int16), new Type[] { typeof(Vector256<Byte>) })
                                     .Invoke(null, new object[] {
                                        Avx.LoadVector256((Byte*)(_dataTable.inArrayPtr))
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector512<Int16>)(result));
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_LoadAligned()
        {
            var result = typeof(Avx512BW).GetMethod(nameof(Avx512BW.ConvertToVector512Int16), new Type[] { typeof(Vector256<Byte>) })
                                     .Invoke(null, new object[] {
                                        Avx.LoadAlignedVector256((Byte*)(_dataTable.inArrayPtr))
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector512<Int16>)(result));
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario()
        {
            var result = Avx512BW.ConvertToVector512Int16(
                _clsVar
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_clsVar, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            var firstOp = Unsafe.Read<Vector256<Byte>>(_dataTable.inArrayPtr);
            var result = Avx512BW.ConvertToVector512Int16(firstOp);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(firstOp, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_Load()
        {
            var firstOp = Avx.LoadVector256((Byte*)(_dataTable.inArrayPtr));
            var result = Avx512BW.ConvertToVector512Int16(firstOp);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(firstOp, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_LoadAligned()
        {
            var firstOp = Avx.LoadAlignedVector256((Byte*)(_dataTable.inArrayPtr));
            var result = Avx512BW.ConvertToVector512Int16(firstOp);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(firstOp, _dataTable.outArrayPtr);
        }

        public void RunLclFldScenario()
        {
            var test = new SimpleUnaryOpTest__ConvertToVector512Int16Byte();
            var result = Avx512BW.ConvertToVector512Int16(test._fld);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld, _dataTable.outArrayPtr);
        }

        public void RunFldScenario()
        {
            var result = Avx512BW.ConvertToVector512Int16(_fld);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_fld, _dataTable.outArrayPtr);
        }

        public void RunUnsupportedScenario()
        {
            Succeeded = false;

            try
            {
                RunBasicScenario_UnsafeRead();
            }
            catch (PlatformNotSupportedException)
            {
                Succeeded = true;
            }
        }

        private void ValidateResult(Vector256<Byte> firstOp, void* result, [CallerMemberName] string method = "")
        {
            Byte[] inArray = new Byte[Op1ElementCount];
            UInt16[] outArray = new UInt16[RetElementCount];

            Unsafe.WriteUnaligned(ref Unsafe.As<Byte, byte>(ref inArray[0]), firstOp);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt16, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), VectorSize);

            ValidateResult(inArray, outArray, method);
        }

        private void ValidateResult(void* firstOp, void* result, [CallerMemberName] string method = "")
        {
            Byte[] inArray = new Byte[Op1ElementCount];
            UInt16[] outArray = new UInt16[RetElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Byte, byte>(ref inArray[0]), ref Unsafe.AsRef<byte>(firstOp), 32);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt16, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), VectorSize);

            ValidateResult(inArray, outArray, method);
        }

        private void ValidateResult(Byte[] firstOp, UInt16[] result, [CallerMemberName] string method = "")
        {
            if (result[0] != (short)firstOp[0])
            {
                Succeeded = false;
            }
            else
            {
                for (var i = 1; i < RetElementCount; i++)
                {
                    if (result[i] != (short)firstOp[i])
                    {
                        Succeeded = false;
                        break;
                    }
                }
            }

            if (!Succeeded)
            {
                Console.WriteLine($"{nameof(Avx512BW)}.{nameof(Avx512BW.ConvertToVector512Int16)}<UInt16>(Vector256<Byte>): {method} failed:");
                Console.WriteLine($"  firstOp: ({string.Join(", ", firstOp)})");
                Console.WriteLine($"   result: ({string.Join(", ", result)})");
                Console.WriteLine();
            }
        }
    }
}
