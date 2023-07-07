// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/******************************************************************************
 * This file is auto-generated from a template file by the GenerateTests.csx  *
 * script in tests\src\JIT\HardwareIntrinsics\X86\Shared. In order to make    *
 * changes, please update the corresponding template and run according to the *
 * directions listed in the file.                                             *
 ******************************************************************************/

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

namespace JIT.HardwareIntrinsics.X86._Avx512F.handwritten
{
    public static partial class Program
    {
        [Fact]
        public static void ConvertToVector256Int32WithTruncationDouble()
        {
            var test = new SimpleUnaryOpTest__ConvertToVector256Int32WithTruncationDouble();

            if (test.IsSupported)
            {
                // Validates basic functionality works, using Unsafe.Read
                test.RunBasicScenario_UnsafeRead();

                if (Avx512F.IsSupported)
                {
                    // Validates basic functionality works, using Load
                    test.RunBasicScenario_Load();

                    // Validates basic functionality works, using LoadAligned
                    test.RunBasicScenario_LoadAligned();
                }

                // Validates calling via reflection works, using Unsafe.Read
                test.RunReflectionScenario_UnsafeRead();

                if (Avx512F.IsSupported)
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

                if (Avx512F.IsSupported)
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

    public sealed unsafe class SimpleUnaryOpTest__ConvertToVector256Int32WithTruncationDouble
    {
        private static readonly int LargestVectorSize = 64;

        private static readonly int Op1ElementCount = Unsafe.SizeOf<Vector512<Double>>() / sizeof(Double);
        private static readonly int RetElementCount = Unsafe.SizeOf<Vector256<Int32>>() / sizeof(Int32);

        private static Double[] _data = new Double[Op1ElementCount];

        private static Vector512<Double> _clsVar;

        private Vector512<Double> _fld;

        private SimpleUnaryOpTest__DataTable<Int32, Double> _dataTable;

        static SimpleUnaryOpTest__ConvertToVector256Int32WithTruncationDouble()
        {
            var random = new Random();

            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = (double)(random.NextDouble()); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector512<Double>, byte>(ref _clsVar), ref Unsafe.As<Double, byte>(ref _data[0]), (uint)Unsafe.SizeOf<Vector512<Double>>());
        }

        public SimpleUnaryOpTest__ConvertToVector256Int32WithTruncationDouble()
        {
            Succeeded = true;

            var random = new Random();

            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = (double)(random.NextDouble()); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector512<Double>, byte>(ref _fld), ref Unsafe.As<Double, byte>(ref _data[0]), (uint)Unsafe.SizeOf<Vector512<Double>>());

            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = (double)(random.NextDouble()); }
            _dataTable = new SimpleUnaryOpTest__DataTable<Int32, Double>(_data, new Int32[RetElementCount], LargestVectorSize);
        }

        public bool IsSupported => Avx512F.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            var result = Avx512F.ConvertToVector256Int32WithTruncation(
                Unsafe.Read<Vector512<Double>>(_dataTable.inArrayPtr)
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_Load()
        {
            var result = Avx512F.ConvertToVector256Int32WithTruncation(
                Avx512F.LoadVector512((Double*)(_dataTable.inArrayPtr))
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_LoadAligned()
        {
            var result = Avx512F.ConvertToVector256Int32WithTruncation(
                Avx512F.LoadAlignedVector512((Double*)(_dataTable.inArrayPtr))
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            var result = typeof(Avx512F).GetMethod(nameof(Avx512F.ConvertToVector256Int32WithTruncation), new Type[] { typeof(Vector512<Double>) })
                                     .Invoke(null, new object[] {
                                        Unsafe.Read<Vector512<Double>>(_dataTable.inArrayPtr)
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector256<Int32>)(result));
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_Load()
        {
            var result = typeof(Avx512F).GetMethod(nameof(Avx512F.ConvertToVector256Int32WithTruncation), new Type[] { typeof(Vector512<Double>) })
                                     .Invoke(null, new object[] {
                                        Avx512F.LoadVector512((Double*)(_dataTable.inArrayPtr))
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector256<Int32>)(result));
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_LoadAligned()
        {
            var result = typeof(Avx512F).GetMethod(nameof(Avx512F.ConvertToVector256Int32WithTruncation), new Type[] { typeof(Vector512<Double>) })
                                     .Invoke(null, new object[] {
                                        Avx512F.LoadAlignedVector512((Double*)(_dataTable.inArrayPtr))
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector256<Int32>)(result));
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario()
        {
            var result = Avx512F.ConvertToVector256Int32WithTruncation(
                _clsVar
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_clsVar, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            var firstOp = Unsafe.Read<Vector512<Double>>(_dataTable.inArrayPtr);
            var result = Avx512F.ConvertToVector256Int32WithTruncation(firstOp);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(firstOp, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_Load()
        {
            var firstOp = Avx512F.LoadVector512((Double*)(_dataTable.inArrayPtr));
            var result = Avx512F.ConvertToVector256Int32WithTruncation(firstOp);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(firstOp, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_LoadAligned()
        {
            var firstOp = Avx512F.LoadAlignedVector512((Double*)(_dataTable.inArrayPtr));
            var result = Avx512F.ConvertToVector256Int32WithTruncation(firstOp);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(firstOp, _dataTable.outArrayPtr);
        }

        public void RunLclFldScenario()
        {
            var test = new SimpleUnaryOpTest__ConvertToVector256Int32WithTruncationDouble();
            var result = Avx512F.ConvertToVector256Int32WithTruncation(test._fld);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld, _dataTable.outArrayPtr);
        }

        public void RunFldScenario()
        {
            var result = Avx512F.ConvertToVector256Int32WithTruncation(_fld);

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

        private void ValidateResult(Vector512<Double> firstOp, void* result, [CallerMemberName] string method = "")
        {
            Double[] inArray = new Double[Op1ElementCount];
            Int32[] outArray = new Int32[RetElementCount];

            Unsafe.WriteUnaligned(ref Unsafe.As<Double, byte>(ref inArray[0]), firstOp);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Int32, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector256<Int32>>());

            ValidateResult(inArray, outArray, method);
        }

        private void ValidateResult(void* firstOp, void* result, [CallerMemberName] string method = "")
        {
            Double[] inArray = new Double[Op1ElementCount];
            Int32[] outArray = new Int32[RetElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Double, byte>(ref inArray[0]), ref Unsafe.AsRef<byte>(firstOp), (uint)Unsafe.SizeOf<Vector512<Double>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Int32, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector256<Int32>>());

            ValidateResult(inArray, outArray, method);
        }

        private void ValidateResult(Double[] firstOp, Int32[] result, [CallerMemberName] string method = "")
        {
            for (var i = 0; i < Op1ElementCount; i++)
            {
                if (result[i] != (Int32)(firstOp[i]))
                {
                    Succeeded = false;
                    break;
                }
            }

            for (var i = Op1ElementCount; i < RetElementCount; i++)
            {
                if (result[i] != 0)
                {
                    Succeeded = false;
                    break;
                }
            }

            if (!Succeeded)
            {
                Console.WriteLine($"{nameof(Avx512F)}.{nameof(Avx512F.ConvertToVector256Int32WithTruncation)}(Vector512<Double>): {method} failed:");
                Console.WriteLine($"  firstOp: ({string.Join(", ", firstOp)})");
                Console.WriteLine($"   result: ({string.Join(", ", result)})");
                Console.WriteLine();
            }
        }
    }
}
