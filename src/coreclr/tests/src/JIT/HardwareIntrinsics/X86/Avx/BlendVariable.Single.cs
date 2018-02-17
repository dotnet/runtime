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
        private static void BlendVariableSingle()
        {
            var test = new SimpleTernaryOpTest__BlendVariableSingle();

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

    public sealed unsafe class SimpleTernaryOpTest__BlendVariableSingle
    {
        private const int VectorSize = 32;

        private const int Op1ElementCount = VectorSize / sizeof(Single);
        private const int Op2ElementCount = VectorSize / sizeof(Single);
        private const int Op3ElementCount = VectorSize / sizeof(Single);
        private const int RetElementCount = VectorSize / sizeof(Single);

        private static Single[] _data1 = new Single[Op1ElementCount];
        private static Single[] _data2 = new Single[Op2ElementCount];
        private static Single[] _data3 = new Single[Op3ElementCount];

        private static Vector256<Single> _clsVar1;
        private static Vector256<Single> _clsVar2;
        private static Vector256<Single> _clsVar3;

        private Vector256<Single> _fld1;
        private Vector256<Single> _fld2;
        private Vector256<Single> _fld3;

        private SimpleTernaryOpTest__DataTable<Single, Single, Single, Single> _dataTable;

        static SimpleTernaryOpTest__BlendVariableSingle()
        {
            var random = new Random();

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = (float)(random.NextDouble()); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<Single>, byte>(ref _clsVar1), ref Unsafe.As<Single, byte>(ref _data1[0]), VectorSize);
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = (float)(random.NextDouble()); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<Single>, byte>(ref _clsVar2), ref Unsafe.As<Single, byte>(ref _data2[0]), VectorSize);
            for (var i = 0; i < Op3ElementCount; i++) { _data3[i] = (float)(((i % 2) == 0) ? -0.0 : 1.0); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<Single>, byte>(ref _clsVar3), ref Unsafe.As<Single, byte>(ref _data3[0]), VectorSize);
        }

        public SimpleTernaryOpTest__BlendVariableSingle()
        {
            Succeeded = true;

            var random = new Random();

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = (float)(random.NextDouble()); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<Single>, byte>(ref _fld1), ref Unsafe.As<Single, byte>(ref _data1[0]), VectorSize);
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = (float)(random.NextDouble()); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<Single>, byte>(ref _fld2), ref Unsafe.As<Single, byte>(ref _data2[0]), VectorSize);
            for (var i = 0; i < Op3ElementCount; i++) { _data3[i] = (float)(((i % 2) == 0) ? -0.0 : 1.0); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<Single>, byte>(ref _fld3), ref Unsafe.As<Single, byte>(ref _data3[0]), VectorSize);

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = (float)(random.NextDouble()); }
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = (float)(random.NextDouble()); }
            for (var i = 0; i < Op3ElementCount; i++) { _data3[i] = (float)(((i % 2) == 0) ? -0.0 : 1.0); }
            _dataTable = new SimpleTernaryOpTest__DataTable<Single, Single, Single, Single>(_data1, _data2, _data3, new Single[RetElementCount], VectorSize);
        }

        public bool IsSupported => Avx.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            var result = Avx.BlendVariable(
                Unsafe.Read<Vector256<Single>>(_dataTable.inArray1Ptr),
                Unsafe.Read<Vector256<Single>>(_dataTable.inArray2Ptr),
                Unsafe.Read<Vector256<Single>>(_dataTable.inArray3Ptr)
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.inArray3Ptr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_Load()
        {
            var result = Avx.BlendVariable(
                Avx.LoadVector256((Single*)(_dataTable.inArray1Ptr)),
                Avx.LoadVector256((Single*)(_dataTable.inArray2Ptr)),
                Avx.LoadVector256((Single*)(_dataTable.inArray3Ptr))
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.inArray3Ptr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_LoadAligned()
        {
            var result = Avx.BlendVariable(
                Avx.LoadAlignedVector256((Single*)(_dataTable.inArray1Ptr)),
                Avx.LoadAlignedVector256((Single*)(_dataTable.inArray2Ptr)),
                Avx.LoadAlignedVector256((Single*)(_dataTable.inArray3Ptr))
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.inArray3Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            var result = typeof(Avx).GetMethod(nameof(Avx.BlendVariable), new Type[] { typeof(Vector256<Single>), typeof(Vector256<Single>), typeof(Vector256<Single>) })
                                     .Invoke(null, new object[] {
                                        Unsafe.Read<Vector256<Single>>(_dataTable.inArray1Ptr),
                                        Unsafe.Read<Vector256<Single>>(_dataTable.inArray2Ptr),
                                        Unsafe.Read<Vector256<Single>>(_dataTable.inArray3Ptr)
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector256<Single>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.inArray3Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_Load()
        {
            var result = typeof(Avx).GetMethod(nameof(Avx.BlendVariable), new Type[] { typeof(Vector256<Single>), typeof(Vector256<Single>), typeof(Vector256<Single>) })
                                     .Invoke(null, new object[] {
                                        Avx.LoadVector256((Single*)(_dataTable.inArray1Ptr)),
                                        Avx.LoadVector256((Single*)(_dataTable.inArray2Ptr)),
                                        Avx.LoadVector256((Single*)(_dataTable.inArray3Ptr))
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector256<Single>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.inArray3Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_LoadAligned()
        {
            var result = typeof(Avx).GetMethod(nameof(Avx.BlendVariable), new Type[] { typeof(Vector256<Single>), typeof(Vector256<Single>), typeof(Vector256<Single>) })
                                     .Invoke(null, new object[] {
                                        Avx.LoadAlignedVector256((Single*)(_dataTable.inArray1Ptr)),
                                        Avx.LoadAlignedVector256((Single*)(_dataTable.inArray2Ptr)),
                                        Avx.LoadAlignedVector256((Single*)(_dataTable.inArray3Ptr))
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector256<Single>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.inArray3Ptr, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario()
        {
            var result = Avx.BlendVariable(
                _clsVar1,
                _clsVar2,
                _clsVar3
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_clsVar1, _clsVar2, _clsVar3, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            var firstOp = Unsafe.Read<Vector256<Single>>(_dataTable.inArray1Ptr);
            var secondOp = Unsafe.Read<Vector256<Single>>(_dataTable.inArray2Ptr);
            var thirdOp = Unsafe.Read<Vector256<Single>>(_dataTable.inArray3Ptr);
            var result = Avx.BlendVariable(firstOp, secondOp, thirdOp);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(firstOp, secondOp, thirdOp, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_Load()
        {
            var firstOp = Avx.LoadVector256((Single*)(_dataTable.inArray1Ptr));
            var secondOp = Avx.LoadVector256((Single*)(_dataTable.inArray2Ptr));
            var thirdOp = Avx.LoadVector256((Single*)(_dataTable.inArray3Ptr));
            var result = Avx.BlendVariable(firstOp, secondOp, thirdOp);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(firstOp, secondOp, thirdOp, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_LoadAligned()
        {
            var firstOp = Avx.LoadAlignedVector256((Single*)(_dataTable.inArray1Ptr));
            var secondOp = Avx.LoadAlignedVector256((Single*)(_dataTable.inArray2Ptr));
            var thirdOp = Avx.LoadAlignedVector256((Single*)(_dataTable.inArray3Ptr));
            var result = Avx.BlendVariable(firstOp, secondOp, thirdOp);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(firstOp, secondOp, thirdOp, _dataTable.outArrayPtr);
        }

        public void RunLclFldScenario()
        {
            var test = new SimpleTernaryOpTest__BlendVariableSingle();
            var result = Avx.BlendVariable(test._fld1, test._fld2, test._fld3);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld1, test._fld2, test._fld3, _dataTable.outArrayPtr);
        }

        public void RunFldScenario()
        {
            var result = Avx.BlendVariable(_fld1, _fld2, _fld3);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_fld1, _fld2, _fld3, _dataTable.outArrayPtr);
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

        private void ValidateResult(Vector256<Single> firstOp, Vector256<Single> secondOp, Vector256<Single> thirdOp, void* result, [CallerMemberName] string method = "")
        {
            Single[] inArray1 = new Single[Op1ElementCount];
            Single[] inArray2 = new Single[Op2ElementCount];
            Single[] inArray3 = new Single[Op3ElementCount];
            Single[] outArray = new Single[RetElementCount];

            Unsafe.Write(Unsafe.AsPointer(ref inArray1[0]), firstOp);
            Unsafe.Write(Unsafe.AsPointer(ref inArray2[0]), secondOp);
            Unsafe.Write(Unsafe.AsPointer(ref inArray3[0]), thirdOp);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Single, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), VectorSize);

            ValidateResult(inArray1, inArray2, inArray3, outArray, method);
        }

        private void ValidateResult(void* firstOp, void* secondOp, void* thirdOp, void* result, [CallerMemberName] string method = "")
        {
            Single[] inArray1 = new Single[Op1ElementCount];
            Single[] inArray2 = new Single[Op2ElementCount];
            Single[] inArray3 = new Single[Op3ElementCount];
            Single[] outArray = new Single[RetElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Single, byte>(ref inArray1[0]), ref Unsafe.AsRef<byte>(firstOp), VectorSize);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Single, byte>(ref inArray2[0]), ref Unsafe.AsRef<byte>(secondOp), VectorSize);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Single, byte>(ref inArray3[0]), ref Unsafe.AsRef<byte>(thirdOp), VectorSize);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Single, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), VectorSize);

            ValidateResult(inArray1, inArray2, inArray3, outArray, method);
        }

        private void ValidateResult(Single[] firstOp, Single[] secondOp, Single[] thirdOp, Single[] result, [CallerMemberName] string method = "")
        {
            if (((BitConverter.SingleToInt32Bits(thirdOp[0]) >> 31) & 1) == 1 ? BitConverter.SingleToInt32Bits(secondOp[0]) != BitConverter.SingleToInt32Bits(result[0]) : BitConverter.SingleToInt32Bits(firstOp[0]) != BitConverter.SingleToInt32Bits(result[0]))
            {
                Succeeded = false;
            }
            else
            {
                for (var i = 1; i < RetElementCount; i++)
                {
                    if (((BitConverter.SingleToInt32Bits(thirdOp[i]) >> 31) & 1) == 1 ? BitConverter.SingleToInt32Bits(secondOp[i]) != BitConverter.SingleToInt32Bits(result[i]) : BitConverter.SingleToInt32Bits(firstOp[i]) != BitConverter.SingleToInt32Bits(result[i]))
                    {
                        Succeeded = false;
                        break;
                    }
                }
            }

            if (!Succeeded)
            {
                Console.WriteLine($"{nameof(Avx)}.{nameof(Avx.BlendVariable)}<Single>(Vector256<Single>, Vector256<Single>, Vector256<Single>): {method} failed:");
                Console.WriteLine($"   firstOp: ({string.Join(", ", firstOp)})");
                Console.WriteLine($"  secondOp: ({string.Join(", ", secondOp)})");
                Console.WriteLine($"   thirdOp: ({string.Join(", ", thirdOp)})");
                Console.WriteLine($"    result: ({string.Join(", ", result)})");
                Console.WriteLine();
            }
        }
    }
}
