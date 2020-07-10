// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/******************************************************************************
 * This file is auto-generated from a template file by the GenerateTests.csx  *
 * script in tests\src\JIT\HardwareIntrinsics\X86\Shared. In order to make    *
 * changes, please update the corresponding template and run according to the *
 * directions listed in the file.                                             *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JIT.HardwareIntrinsics.X86
{
    public static partial class Program
    {
        static Program()
        {
            TestList = new Dictionary<string, Action>() {
                ["MinHorizontal"] = MinHorizontal
            };
        }

        private static void MinHorizontal()
        {
            var test = new SimpleUnaryOpTest__MinHorizontal();

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

    public sealed unsafe class SimpleUnaryOpTest__MinHorizontal
    {
        private static readonly int LargestVectorSize = 16;

        private static readonly int Op1ElementCount = Unsafe.SizeOf<Vector128<UInt16>>() / sizeof(UInt16);
        private static readonly int RetElementCount = Unsafe.SizeOf<Vector128<UInt16>>() / sizeof(UInt16);

        private static UInt16[] _data = new UInt16[Op1ElementCount];

        private static Vector128<UInt16> _clsVar;

        private Vector128<UInt16> _fld;

        private SimpleUnaryOpTest__DataTable<UInt16, UInt16> _dataTable;

        static SimpleUnaryOpTest__MinHorizontal()
        {
            var random = new Random();

            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = (ushort)(random.Next(0, ushort.MaxValue)); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<UInt16>, byte>(ref _clsVar), ref Unsafe.As<UInt16, byte>(ref _data[0]), (uint)Unsafe.SizeOf<Vector128<UInt16>>());
        }

        public SimpleUnaryOpTest__MinHorizontal()
        {
            Succeeded = true;

            var random = new Random();

            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = (ushort)(random.Next(0, ushort.MaxValue)); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<UInt16>, byte>(ref _fld), ref Unsafe.As<UInt16, byte>(ref _data[0]), (uint)Unsafe.SizeOf<Vector128<UInt16>>());

            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = (ushort)(random.Next(0, ushort.MaxValue)); }
            _dataTable = new SimpleUnaryOpTest__DataTable<UInt16, UInt16>(_data, new UInt16[RetElementCount], LargestVectorSize);
        }

        public bool IsSupported => Sse41.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            var result = Sse41.MinHorizontal(
                Unsafe.Read<Vector128<UInt16>>(_dataTable.inArrayPtr)
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_Load()
        {
            var result = Sse41.MinHorizontal(
                Sse2.LoadVector128((UInt16*)(_dataTable.inArrayPtr))
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_LoadAligned()
        {
            var result = Sse41.MinHorizontal(
                Sse2.LoadAlignedVector128((UInt16*)(_dataTable.inArrayPtr))
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            var result = typeof(Sse41).GetMethod(nameof(Sse41.MinHorizontal), new Type[] { typeof(Vector128<UInt16>) })
                                     .Invoke(null, new object[] {
                                        Unsafe.Read<Vector128<UInt16>>(_dataTable.inArrayPtr)
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<UInt16>)(result));
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_Load()
        {
            var result = typeof(Sse41).GetMethod(nameof(Sse41.MinHorizontal), new Type[] { typeof(Vector128<UInt16>) })
                                     .Invoke(null, new object[] {
                                        Sse2.LoadVector128((UInt16*)(_dataTable.inArrayPtr))
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<UInt16>)(result));
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_LoadAligned()
        {
            var result = typeof(Sse41).GetMethod(nameof(Sse41.MinHorizontal), new Type[] { typeof(Vector128<UInt16>) })
                                     .Invoke(null, new object[] {
                                        Sse2.LoadAlignedVector128((UInt16*)(_dataTable.inArrayPtr))
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<UInt16>)(result));
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario()
        {
            var result = Sse41.MinHorizontal(
                _clsVar
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_clsVar, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            var firstOp = Unsafe.Read<Vector128<UInt16>>(_dataTable.inArrayPtr);
            var result = Sse41.MinHorizontal(firstOp);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(firstOp, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_Load()
        {
            var firstOp = Sse2.LoadVector128((UInt16*)(_dataTable.inArrayPtr));
            var result = Sse41.MinHorizontal(firstOp);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(firstOp, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_LoadAligned()
        {
            var firstOp = Sse2.LoadAlignedVector128((UInt16*)(_dataTable.inArrayPtr));
            var result = Sse41.MinHorizontal(firstOp);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(firstOp, _dataTable.outArrayPtr);
        }

        public void RunLclFldScenario()
        {
            var test = new SimpleUnaryOpTest__MinHorizontal();
            var result = Sse41.MinHorizontal(test._fld);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld, _dataTable.outArrayPtr);
        }

        public void RunFldScenario()
        {
            var result = Sse41.MinHorizontal(_fld);

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

        private void ValidateResult(Vector128<UInt16> firstOp, void* result, [CallerMemberName] string method = "")
        {
            UInt16[] inArray = new UInt16[Op1ElementCount];
            UInt16[] outArray = new UInt16[RetElementCount];

            Unsafe.WriteUnaligned(ref Unsafe.As<UInt16, byte>(ref inArray[0]), firstOp);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt16, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector128<UInt16>>());

            ValidateResult(inArray, outArray, method);
        }

        private void ValidateResult(void* firstOp, void* result, [CallerMemberName] string method = "")
        {
            UInt16[] inArray = new UInt16[Op1ElementCount];
            UInt16[] outArray = new UInt16[RetElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt16, byte>(ref inArray[0]), ref Unsafe.AsRef<byte>(firstOp), (uint)Unsafe.SizeOf<Vector128<UInt16>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt16, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector128<UInt16>>());

            ValidateResult(inArray, outArray, method);
        }

        private void ValidateResult(UInt16[] firstOp, UInt16[] result, [CallerMemberName] string method = "")
        {
            var minValue = firstOp.Min();
            var minIndex = firstOp.ToList().IndexOf(minValue);
            
            if ((result[0] != minValue) || (result[1] != minIndex))
            {
                Succeeded = false;
            }
            else
            {
                for (var i = 2; i < RetElementCount; i++)
                {
                    if (result[i] != 0)
                    {
                        Succeeded = false;
                        break;
                    }
                }
            }

            if (!Succeeded)
            {
                Console.WriteLine($"{nameof(Sse41)}.{nameof(Sse41.MinHorizontal)}<UInt16>(Vector128<UInt16>): {method} failed:");
                Console.WriteLine($"  firstOp: ({string.Join(", ", firstOp)})");
                Console.WriteLine($"   result: ({string.Join(", ", result)})");
                Console.WriteLine();
            }
        }
    }
}
