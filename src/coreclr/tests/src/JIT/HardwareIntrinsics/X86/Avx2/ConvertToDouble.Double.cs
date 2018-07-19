// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

namespace JIT.HardwareIntrinsics.X86
{
    public static partial class Program
    {
        private static void ConvertToDoubleDouble()
        {
            var test = new SimdScalarUnaryOpTest__ConvertToDoubleDouble();

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

                // Validates passing the field of a local class works
                test.RunClassLclFldScenario();

                // Validates passing an instance member of a class works
                test.RunClassFldScenario();

                // Validates passing the field of a local struct works
                test.RunStructLclFldScenario();

                // Validates passing an instance member of a struct works
                test.RunStructFldScenario();
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

    public sealed unsafe class SimdScalarUnaryOpTest__ConvertToDoubleDouble
    {
        private struct TestStruct
        {
            public Vector256<Double> _fld;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();
                var random = new Random();

                for (var i = 0; i < Op1ElementCount; i++) { _data[i] = TestLibrary.Generator.GetDouble(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<Double>, byte>(ref testStruct._fld), ref Unsafe.As<Double, byte>(ref _data[0]), (uint)Unsafe.SizeOf<Vector256<Double>>());

                return testStruct;
            }

            public void RunStructFldScenario(SimdScalarUnaryOpTest__ConvertToDoubleDouble testClass)
            {
                var result = Avx2.ConvertToDouble(_fld);
                testClass.ValidateResult(_fld, result);
            }
        }

        private static readonly int LargestVectorSize = 32;

        private static readonly int Op1ElementCount = Unsafe.SizeOf<Vector256<Double>>() / sizeof(Double);

        private static Double[] _data = new Double[Op1ElementCount];

        private static Vector256<Double> _clsVar;

        private Vector256<Double> _fld;

        private SimdScalarUnaryOpTest__DataTable<Double> _dataTable;

        static SimdScalarUnaryOpTest__ConvertToDoubleDouble()
        {
            var random = new Random();

            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = TestLibrary.Generator.GetDouble(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<Double>, byte>(ref _clsVar), ref Unsafe.As<Double, byte>(ref _data[0]), (uint)Unsafe.SizeOf<Vector256<Double>>());
        }

        public SimdScalarUnaryOpTest__ConvertToDoubleDouble()
        {
            Succeeded = true;

            var random = new Random();

            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = TestLibrary.Generator.GetDouble(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<Double>, byte>(ref _fld), ref Unsafe.As<Double, byte>(ref _data[0]), (uint)Unsafe.SizeOf<Vector256<Double>>());

            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = TestLibrary.Generator.GetDouble(); }
            _dataTable = new SimdScalarUnaryOpTest__DataTable<Double>(_data, LargestVectorSize);
        }

        public bool IsSupported => Avx2.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            var result = Avx2.ConvertToDouble(
                Unsafe.Read<Vector256<Double>>(_dataTable.inArrayPtr)
            );

            ValidateResult(_dataTable.inArrayPtr, result);
        }

        public void RunBasicScenario_Load()
        {
            var result = Avx2.ConvertToDouble(
                Avx.LoadVector256((Double*)(_dataTable.inArrayPtr))
            );

            ValidateResult(_dataTable.inArrayPtr, result);
        }

        public void RunBasicScenario_LoadAligned()
        {
            var result = Avx2.ConvertToDouble(
                Avx.LoadAlignedVector256((Double*)(_dataTable.inArrayPtr))
            );

            ValidateResult(_dataTable.inArrayPtr, result);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            var result = typeof(Avx2).GetMethod(nameof(Avx2.ConvertToDouble), new Type[] { typeof(Vector256<Double>) })
                                     .Invoke(null, new object[] {
                                        Unsafe.Read<Vector256<Double>>(_dataTable.inArrayPtr)
                                     });

            ValidateResult(_dataTable.inArrayPtr, (Double)(result));
        }

        public void RunReflectionScenario_Load()
        {
            var result = typeof(Avx2).GetMethod(nameof(Avx2.ConvertToDouble), new Type[] { typeof(Vector256<Double>) })
                                     .Invoke(null, new object[] {
                                        Avx.LoadVector256((Double*)(_dataTable.inArrayPtr))
                                     });

            ValidateResult(_dataTable.inArrayPtr, (Double)(result));
        }

        public void RunReflectionScenario_LoadAligned()
        {
            var result = typeof(Avx2).GetMethod(nameof(Avx2.ConvertToDouble), new Type[] { typeof(Vector256<Double>) })
                                     .Invoke(null, new object[] {
                                        Avx.LoadAlignedVector256((Double*)(_dataTable.inArrayPtr))
                                     });

            ValidateResult(_dataTable.inArrayPtr, (Double)(result));
        }

        public void RunClsVarScenario()
        {
            var result = Avx2.ConvertToDouble(
                _clsVar
            );

            ValidateResult(_clsVar, result);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            var firstOp = Unsafe.Read<Vector256<Double>>(_dataTable.inArrayPtr);
            var result = Avx2.ConvertToDouble(firstOp);

            ValidateResult(firstOp, result);
        }

        public void RunLclVarScenario_Load()
        {
            var firstOp = Avx.LoadVector256((Double*)(_dataTable.inArrayPtr));
            var result = Avx2.ConvertToDouble(firstOp);

            ValidateResult(firstOp, result);
        }

        public void RunLclVarScenario_LoadAligned()
        {
            var firstOp = Avx.LoadAlignedVector256((Double*)(_dataTable.inArrayPtr));
            var result = Avx2.ConvertToDouble(firstOp);

            ValidateResult(firstOp, result);
        }

        public void RunClassLclFldScenario()
        {
            var test = new SimdScalarUnaryOpTest__ConvertToDoubleDouble();
            var result = Avx2.ConvertToDouble(test._fld);

            ValidateResult(test._fld, result);
        }

        public void RunClassFldScenario()
        {
            var result = Avx2.ConvertToDouble(_fld);

            ValidateResult(_fld, result);
        }

        public void RunStructLclFldScenario()
        {
            var test = TestStruct.Create();
            var result = Avx2.ConvertToDouble(test._fld);

            ValidateResult(test._fld, result);
        }

        public void RunStructFldScenario()
        {
            var test = TestStruct.Create();
            test.RunStructFldScenario(this);
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

        private void ValidateResult(Vector256<Double> firstOp, Double result, [CallerMemberName] string method = "")
        {
            Double[] inArray = new Double[Op1ElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Double, byte>(ref inArray[0]), firstOp);
            ValidateResult(inArray, result, method);
        }

        private void ValidateResult(void* firstOp, Double result, [CallerMemberName] string method = "")
        {
            Double[] inArray = new Double[Op1ElementCount];
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Double, byte>(ref inArray[0]), ref Unsafe.AsRef<byte>(firstOp), (uint)Unsafe.SizeOf<Vector256<Double>>());
            ValidateResult(inArray, result, method);
        }

        private void ValidateResult(Double[] firstOp, Double result, [CallerMemberName] string method = "")
        {
            if (BitConverter.DoubleToInt64Bits(firstOp[0]) != BitConverter.DoubleToInt64Bits(result))
            {
                Succeeded = false;
            }

            if (!Succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(Avx2)}.{nameof(Avx2.ConvertToDouble)}<Double>(Vector256<Double>): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"  firstOp: ({string.Join(", ", firstOp)})");
                TestLibrary.TestFramework.LogInformation($"   result: result");
                TestLibrary.TestFramework.LogInformation(string.Empty);
            }
        }
    }
}
