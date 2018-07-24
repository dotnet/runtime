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
        private static void MultiplySubtractNegatedScalarDouble()
        {
            var test = new SimpleTernaryOpTest__MultiplySubtractNegatedScalarDouble();

            if (test.IsSupported)
            {
                // Validates basic functionality works, using Unsafe.Read
                test.RunBasicScenario_UnsafeRead();

                if (Sse2.IsSupported)
                {
                    // Validates basic functionality works, using Load
                    test.RunBasicScenario_Load();

                    // Validates basic functionality works, using LoadAligned
                    test.RunBasicScenario_LoadAligned();
                }

                // Validates calling via reflection works, using Unsafe.Read
                test.RunReflectionScenario_UnsafeRead();

                if (Sse2.IsSupported)
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

                if (Sse2.IsSupported)
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

    public sealed unsafe class SimpleTernaryOpTest__MultiplySubtractNegatedScalarDouble
    {
        private struct TestStruct
        {
            public Vector128<Double> _fld1;
            public Vector128<Double> _fld2;
            public Vector128<Double> _fld3;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetDouble(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Double>, byte>(ref testStruct._fld1), ref Unsafe.As<Double, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector128<Double>>());
                for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetDouble(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Double>, byte>(ref testStruct._fld2), ref Unsafe.As<Double, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector128<Double>>());
                for (var i = 0; i < Op3ElementCount; i++) { _data3[i] = TestLibrary.Generator.GetDouble(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Double>, byte>(ref _clsVar3), ref Unsafe.As<Double, byte>(ref _data3[0]), (uint)Unsafe.SizeOf<Vector128<Double>>());

                return testStruct;
            }

            public void RunStructFldScenario(SimpleTernaryOpTest__MultiplySubtractNegatedScalarDouble testClass)
            {
                var result = Fma.MultiplySubtractNegatedScalar(_fld1, _fld2, _fld3);

                Unsafe.Write(testClass._dataTable.outArrayPtr, result);
                testClass.ValidateResult(_fld1, _fld2, _fld3, testClass._dataTable.outArrayPtr);
            }
        }

        private static readonly int LargestVectorSize = 16;

        private static readonly int Op1ElementCount = Unsafe.SizeOf<Vector128<Double>>() / sizeof(Double);
        private static readonly int Op2ElementCount = Unsafe.SizeOf<Vector128<Double>>() / sizeof(Double);
        private static readonly int Op3ElementCount = Unsafe.SizeOf<Vector128<Double>>() / sizeof(Double);
        private static readonly int RetElementCount = Unsafe.SizeOf<Vector128<Double>>() / sizeof(Double);

        private static Double[] _data1 = new Double[Op1ElementCount];
        private static Double[] _data2 = new Double[Op2ElementCount];
        private static Double[] _data3 = new Double[Op3ElementCount];

        private static Vector128<Double> _clsVar1;
        private static Vector128<Double> _clsVar2;
        private static Vector128<Double> _clsVar3;

        private Vector128<Double> _fld1;
        private Vector128<Double> _fld2;
        private Vector128<Double> _fld3;

        private SimpleTernaryOpTest__DataTable<Double, Double, Double, Double> _dataTable;

        static SimpleTernaryOpTest__MultiplySubtractNegatedScalarDouble()
        {
            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetDouble(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Double>, byte>(ref _clsVar1), ref Unsafe.As<Double, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector128<Double>>());
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetDouble(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Double>, byte>(ref _clsVar2), ref Unsafe.As<Double, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector128<Double>>());
            for (var i = 0; i < Op3ElementCount; i++) { _data3[i] = TestLibrary.Generator.GetDouble(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Double>, byte>(ref _clsVar3), ref Unsafe.As<Double, byte>(ref _data3[0]), (uint)Unsafe.SizeOf<Vector128<Double>>());
        }

        public SimpleTernaryOpTest__MultiplySubtractNegatedScalarDouble()
        {
            Succeeded = true;

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetDouble(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Double>, byte>(ref _fld1), ref Unsafe.As<Double, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector128<Double>>());
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetDouble(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Double>, byte>(ref _fld2), ref Unsafe.As<Double, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector128<Double>>());
            for (var i = 0; i < Op3ElementCount; i++) { _data3[i] = TestLibrary.Generator.GetDouble(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Double>, byte>(ref _fld3), ref Unsafe.As<Double, byte>(ref _data3[0]), (uint)Unsafe.SizeOf<Vector128<Double>>());

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetDouble(); }
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetDouble(); }
            for (var i = 0; i < Op3ElementCount; i++) { _data3[i] = TestLibrary.Generator.GetDouble(); }
            _dataTable = new SimpleTernaryOpTest__DataTable<Double, Double, Double, Double>(_data1, _data2, _data3, new Double[RetElementCount], LargestVectorSize);
        }

        public bool IsSupported => Fma.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            var result = Fma.MultiplySubtractNegatedScalar(
                Unsafe.Read<Vector128<Double>>(_dataTable.inArray1Ptr),
                Unsafe.Read<Vector128<Double>>(_dataTable.inArray2Ptr),
                Unsafe.Read<Vector128<Double>>(_dataTable.inArray3Ptr)
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.inArray3Ptr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_Load()
        {
            var result = Fma.MultiplySubtractNegatedScalar(
                Sse2.LoadVector128((Double*)(_dataTable.inArray1Ptr)),
                Sse2.LoadVector128((Double*)(_dataTable.inArray2Ptr)),
                Sse2.LoadVector128((Double*)(_dataTable.inArray3Ptr))
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.inArray3Ptr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_LoadAligned()
        {
            var result = Fma.MultiplySubtractNegatedScalar(
                Sse2.LoadAlignedVector128((Double*)(_dataTable.inArray1Ptr)),
                Sse2.LoadAlignedVector128((Double*)(_dataTable.inArray2Ptr)),
                Sse2.LoadAlignedVector128((Double*)(_dataTable.inArray3Ptr))
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.inArray3Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            var result = typeof(Fma).GetMethod(nameof(Fma.MultiplySubtractNegatedScalar), new Type[] { typeof(Vector128<Double>), typeof(Vector128<Double>), typeof(Vector128<Double>) })
                                     .Invoke(null, new object[] {
                                        Unsafe.Read<Vector128<Double>>(_dataTable.inArray1Ptr),
                                        Unsafe.Read<Vector128<Double>>(_dataTable.inArray2Ptr),
                                        Unsafe.Read<Vector128<Double>>(_dataTable.inArray3Ptr)
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<Double>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.inArray3Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_Load()
        {
            var result = typeof(Fma).GetMethod(nameof(Fma.MultiplySubtractNegatedScalar), new Type[] { typeof(Vector128<Double>), typeof(Vector128<Double>), typeof(Vector128<Double>) })
                                     .Invoke(null, new object[] {
                                        Sse2.LoadVector128((Double*)(_dataTable.inArray1Ptr)),
                                        Sse2.LoadVector128((Double*)(_dataTable.inArray2Ptr)),
                                        Sse2.LoadVector128((Double*)(_dataTable.inArray3Ptr))
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<Double>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.inArray3Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_LoadAligned()
        {
            var result = typeof(Fma).GetMethod(nameof(Fma.MultiplySubtractNegatedScalar), new Type[] { typeof(Vector128<Double>), typeof(Vector128<Double>), typeof(Vector128<Double>) })
                                     .Invoke(null, new object[] {
                                        Sse2.LoadAlignedVector128((Double*)(_dataTable.inArray1Ptr)),
                                        Sse2.LoadAlignedVector128((Double*)(_dataTable.inArray2Ptr)),
                                        Sse2.LoadAlignedVector128((Double*)(_dataTable.inArray3Ptr))
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<Double>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.inArray3Ptr, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario()
        {
            var result = Fma.MultiplySubtractNegatedScalar(
                _clsVar1,
                _clsVar2,
                _clsVar3
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_clsVar1, _clsVar2, _clsVar3, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            var firstOp = Unsafe.Read<Vector128<Double>>(_dataTable.inArray1Ptr);
            var secondOp = Unsafe.Read<Vector128<Double>>(_dataTable.inArray2Ptr);
            var thirdOp = Unsafe.Read<Vector128<Double>>(_dataTable.inArray3Ptr);
            var result = Fma.MultiplySubtractNegatedScalar(firstOp, secondOp, thirdOp);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(firstOp, secondOp, thirdOp, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_Load()
        {
            var firstOp = Sse2.LoadVector128((Double*)(_dataTable.inArray1Ptr));
            var secondOp = Sse2.LoadVector128((Double*)(_dataTable.inArray2Ptr));
            var thirdOp = Sse2.LoadVector128((Double*)(_dataTable.inArray3Ptr));
            var result = Fma.MultiplySubtractNegatedScalar(firstOp, secondOp, thirdOp);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(firstOp, secondOp, thirdOp, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_LoadAligned()
        {
            var firstOp = Sse2.LoadAlignedVector128((Double*)(_dataTable.inArray1Ptr));
            var secondOp = Sse2.LoadAlignedVector128((Double*)(_dataTable.inArray2Ptr));
            var thirdOp = Sse2.LoadAlignedVector128((Double*)(_dataTable.inArray3Ptr));
            var result = Fma.MultiplySubtractNegatedScalar(firstOp, secondOp, thirdOp);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(firstOp, secondOp, thirdOp, _dataTable.outArrayPtr);
        }

        public void RunClassLclFldScenario()
        {
            var test = new SimpleTernaryOpTest__MultiplySubtractNegatedScalarDouble();
            var result = Fma.MultiplySubtractNegatedScalar(test._fld1, test._fld2, test._fld3);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld1, test._fld2, test._fld3, _dataTable.outArrayPtr);
        }

        public void RunClassFldScenario()
        {
            var result = Fma.MultiplySubtractNegatedScalar(_fld1, _fld2, _fld3);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_fld1, _fld2, _fld3, _dataTable.outArrayPtr);
        }

        public void RunStructLclFldScenario()
        {
            var test = TestStruct.Create();
            var result = Fma.MultiplySubtractNegatedScalar(test._fld1, test._fld2, test._fld3);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld1, test._fld2, test._fld3, _dataTable.outArrayPtr);
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

        private void ValidateResult(Vector128<Double> firstOp, Vector128<Double> secondOp, Vector128<Double> thirdOp, void* result, [CallerMemberName] string method = "")
        {
            Double[] inArray1 = new Double[Op1ElementCount];
            Double[] inArray2 = new Double[Op2ElementCount];
            Double[] inArray3 = new Double[Op3ElementCount];
            Double[] outArray = new Double[RetElementCount];

            Unsafe.WriteUnaligned(ref Unsafe.As<Double, byte>(ref inArray1[0]), firstOp);
            Unsafe.WriteUnaligned(ref Unsafe.As<Double, byte>(ref inArray2[0]), secondOp);
            Unsafe.WriteUnaligned(ref Unsafe.As<Double, byte>(ref inArray3[0]), thirdOp);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Double, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector128<Double>>());

            ValidateResult(inArray1, inArray2, inArray3, outArray, method);
        }

        private void ValidateResult(void* firstOp, void* secondOp, void* thirdOp, void* result, [CallerMemberName] string method = "")
        {
            Double[] inArray1 = new Double[Op1ElementCount];
            Double[] inArray2 = new Double[Op2ElementCount];
            Double[] inArray3 = new Double[Op3ElementCount];
            Double[] outArray = new Double[RetElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Double, byte>(ref inArray1[0]), ref Unsafe.AsRef<byte>(firstOp), (uint)Unsafe.SizeOf<Vector128<Double>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Double, byte>(ref inArray2[0]), ref Unsafe.AsRef<byte>(secondOp), (uint)Unsafe.SizeOf<Vector128<Double>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Double, byte>(ref inArray3[0]), ref Unsafe.AsRef<byte>(thirdOp), (uint)Unsafe.SizeOf<Vector128<Double>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Double, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector128<Double>>());

            ValidateResult(inArray1, inArray2, inArray3, outArray, method);
        }

        private void ValidateResult(Double[] firstOp, Double[] secondOp, Double[] thirdOp, Double[] result, [CallerMemberName] string method = "")
        {
            if (BitConverter.DoubleToInt64Bits(Math.Round(-(firstOp[0] * secondOp[0]) - thirdOp[0], 9)) != BitConverter.DoubleToInt64Bits(Math.Round(result[0], 9)))
            {
                Succeeded = false;
            }
            else
            {
                for (var i = 1; i < RetElementCount; i++)
                {
                    if (BitConverter.DoubleToInt64Bits(firstOp[i]) != BitConverter.DoubleToInt64Bits(result[i]))
                    {
                        Succeeded = false;
                        break;
                    }
                }
            }

            if (!Succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(Fma)}.{nameof(Fma.MultiplySubtractNegatedScalar)}<Double>(Vector128<Double>, Vector128<Double>, Vector128<Double>): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   firstOp: ({string.Join(", ", firstOp)})");
                TestLibrary.TestFramework.LogInformation($"  secondOp: ({string.Join(", ", secondOp)})");
                TestLibrary.TestFramework.LogInformation($"   thirdOp: ({string.Join(", ", thirdOp)})");
                TestLibrary.TestFramework.LogInformation($"    result: ({string.Join(", ", result)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);
            }
        }
    }
}
