// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class GetObjectForNativeVariantTests
    {
        public static IEnumerable<object[]> GetObjectForNativeVariant_PrimitivesByRef_TestData()
        {
            // VT_NULL => null.
            yield return new object[]
            {
                CreateVariant(VT_NULL, new UnionTypes { _byref = IntPtr.Zero }),
                DBNull.Value
            };

            yield return new object[]
            {
                CreateVariant(VT_NULL, new UnionTypes { _byref = (IntPtr)10 }),
                DBNull.Value
            };

            // VT_I2 => short.
            yield return new object[]
            {
                CreateVariant(VT_I2, new UnionTypes { _i2 = 10 }),
                (short)10
            };

            yield return new object[]
            {
                CreateVariant(VT_I2, new UnionTypes { _i2 = 0 }),
                (short)0
            };

            yield return new object[]
            {
                CreateVariant(VT_I2, new UnionTypes { _i2 = -10 }),
                (short)(-10)
            };

            // VT_I4 => int.
            yield return new object[]
            {
                CreateVariant(VT_I4, new UnionTypes { _i4 = 10 }),
                10
            };

            yield return new object[]
            {
                CreateVariant(VT_I4, new UnionTypes { _i4 = 0 }),
                0
            };

            yield return new object[]
            {
                CreateVariant(VT_I4, new UnionTypes { _i4 = -10 }),
                -10
            };

            // VT_R4 => float.
            yield return new object[]
            {
                CreateVariant(VT_R4, new UnionTypes { _r4 = 10 }),
                (float)10
            };

            yield return new object[]
            {
                CreateVariant(VT_R4, new UnionTypes { _r4 = 0 }),
                (float)0
            };

            yield return new object[]
            {
                CreateVariant(VT_R4, new UnionTypes { _r4 = -10 }),
                (float)(-10)
            };

            yield return new object[]
            {
                CreateVariant(VT_R4, new UnionTypes { _r4 = float.PositiveInfinity }),
                float.PositiveInfinity
            };

            yield return new object[]
            {
                CreateVariant(VT_R4, new UnionTypes { _r4 = float.NegativeInfinity }),
                float.NegativeInfinity
            };

            yield return new object[]
            {
                CreateVariant(VT_R4, new UnionTypes { _r4 = float.NaN }),
                float.NaN
            };

            // VT_R8 => double.
            yield return new object[]
            {
                CreateVariant(VT_R8, new UnionTypes { _r8 = 10 }),
                (double)10
            };

            yield return new object[]
            {
                CreateVariant(VT_R8, new UnionTypes { _r8 = 0 }),
                (double)0
            };

            yield return new object[]
            {
                CreateVariant(VT_R8, new UnionTypes { _r8 = -10 }),
                (double)(-10)
            };

            yield return new object[]
            {
                CreateVariant(VT_R8, new UnionTypes { _r8 = double.PositiveInfinity }),
                double.PositiveInfinity
            };

            yield return new object[]
            {
                CreateVariant(VT_R8, new UnionTypes { _r8 = double.NegativeInfinity }),
                double.NegativeInfinity
            };

            yield return new object[]
            {
                CreateVariant(VT_R8, new UnionTypes { _r8 = double.NaN }),
                double.NaN
            };

            // VT_CY => decimal.
            yield return new object[]
            {
                CreateVariant(VT_CY, new UnionTypes { _cy = 200 }),
                0.02m
            };

            yield return new object[]
            {
                CreateVariant(VT_CY, new UnionTypes { _cy = 0 }),
                0m
            };

            yield return new object[]
            {
                CreateVariant(VT_CY, new UnionTypes { _cy = -200 }),
                -0.02m
            };

            // VT_DATE => DateTime.
            DateTime maxDate = DateTime.MaxValue;
            yield return new object[]
            {
                CreateVariant(VT_DATE, new UnionTypes { _date = maxDate.ToOADate() }),
                new DateTime(9999, 12, 31, 23, 59, 59, 999)
            };

            yield return new object[]
            {
                CreateVariant(VT_DATE, new UnionTypes { _date = 200 }),
                new DateTime(1900, 07, 18)
            };

            yield return new object[]
            {
                CreateVariant(VT_DATE, new UnionTypes { _date = 0.5 }),
                new DateTime(1899, 12, 30, 12, 0, 0)
            };

            yield return new object[]
            {
                CreateVariant(VT_DATE, new UnionTypes { _date = 0 }),
                new DateTime(1899, 12, 30)
            };

            yield return new object[]
            {
                CreateVariant(VT_DATE, new UnionTypes { _date = -0.5 }),
                new DateTime(1899, 12, 30, 12, 0, 0)
            };

            yield return new object[]
            {
                CreateVariant(VT_DATE, new UnionTypes { _date = -200 }),
                new DateTime(1899, 06, 13)
            };

            DateTime minDate = new DateTime(100, 01, 01, 23, 59, 59, 999);
            yield return new object[]
            {
                CreateVariant(VT_DATE, new UnionTypes { _date = minDate.ToOADate() }),
                minDate
            };

            // VT_BSTR => string.
            yield return new object[]
            {
                CreateVariant(VT_BSTR, new UnionTypes { _bstr = IntPtr.Zero }),
                null
            };

            IntPtr emptyString = Marshal.StringToBSTR("");
            yield return new object[]
            {
                CreateVariant(VT_BSTR, new UnionTypes { _bstr = emptyString }),
                ""
            };

            IntPtr oneLetterString = Marshal.StringToBSTR("a");
            yield return new object[]
            {
                CreateVariant(VT_BSTR, new UnionTypes { _bstr = oneLetterString }),
                "a"
            };

            IntPtr twoLetterString = Marshal.StringToBSTR("ab");
            yield return new object[]
            {
                CreateVariant(VT_BSTR, new UnionTypes { _bstr = twoLetterString }),
                "ab"
            };

            IntPtr embeddedNullString = Marshal.StringToBSTR("a\0c");
            yield return new object[]
            {
                CreateVariant(VT_BSTR, new UnionTypes { _bstr = embeddedNullString }),
                "a\0c"
            };

            // VT_DISPATCH => object.
            yield return new object[]
            {
                CreateVariant(VT_DISPATCH, new UnionTypes { _dispatch = IntPtr.Zero }),
                null
            };

            var obj = new Common.IDispatchComObject();
            if (PlatformDetection.IsWindows)
            {
                IntPtr dispatch = Marshal.GetIDispatchForObject(obj);
                yield return new object[]
                {
                    CreateVariant(VT_DISPATCH, new UnionTypes { _dispatch = dispatch }),
                    obj
                };
            }
            else
            {
                Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetIDispatchForObject(obj));
            }

            // VT_ERROR => int.
            yield return new object[]
            {
                CreateVariant(VT_ERROR, new UnionTypes { _error = int.MaxValue }),
                int.MaxValue
            };

            yield return new object[]
            {
                CreateVariant(VT_ERROR, new UnionTypes { _error = 0 }),
                0
            };

            yield return new object[]
            {
                CreateVariant(VT_ERROR, new UnionTypes { _error = int.MinValue }),
                int.MinValue
            };

            // VT_BOOL => bool.
            yield return new object[]
            {
                CreateVariant(VT_BOOL, new UnionTypes { _i1 = 1 }),
                true
            };

            yield return new object[]
            {
                CreateVariant(VT_BOOL, new UnionTypes { _i1 = 0 }),
                false
            };

            yield return new object[]
            {
                CreateVariant(VT_BOOL, new UnionTypes { _i1 = -1 }),
                true
            };

            // VT_UNKNOWN => object.
            yield return new object[]
            {
                CreateVariant(VT_UNKNOWN, new UnionTypes { _unknown = IntPtr.Zero }),
                null
            };

            IntPtr unknown = Marshal.GetIUnknownForObject(obj);
            yield return new object[]
            {
                CreateVariant(VT_UNKNOWN, new UnionTypes { _unknown = unknown }),
                obj
            };

            // VT_I1 => sbyte.
            yield return new object[]
            {
                CreateVariant(VT_I1, new UnionTypes { _i1 = 10 }),
                (sbyte)10
            };

            yield return new object[]
            {
                CreateVariant(VT_I1, new UnionTypes { _i1 = 0 }),
                (sbyte)0
            };

            yield return new object[]
            {
                CreateVariant(VT_I1, new UnionTypes { _i1 = -10 }),
                (sbyte)(-10)
            };

            // VT_UI1 => byte.
            yield return new object[]
            {
                CreateVariant(VT_UI1, new UnionTypes { _ui1 = 10 }),
                (byte)10
            };

            yield return new object[]
            {
                CreateVariant(VT_UI1, new UnionTypes { _ui1 = 0 }),
                (byte)0
            };

            // VT_UI2 => ushort.
            yield return new object[]
            {
                CreateVariant(VT_UI2, new UnionTypes { _ui2 = 10 }),
                (ushort)10
            };

            yield return new object[]
            {
                CreateVariant(VT_UI2, new UnionTypes { _ui2 = 0 }),
                (ushort)0
            };

            // VT_UI4 => uint.
            yield return new object[]
            {
                CreateVariant(VT_UI4, new UnionTypes { _ui4 = 10 }),
                (uint)10
            };

            yield return new object[]
            {
                CreateVariant(VT_UI4, new UnionTypes { _ui4 = 0 }),
                (uint)0
            };

            // VT_I8 => long.
            yield return new object[]
            {
                CreateVariant(VT_I8, new UnionTypes { _i8 = 10 }),
                (long)10
            };

            yield return new object[]
            {
                CreateVariant(VT_I8, new UnionTypes { _i8 = 0 }),
                (long)0
            };

            yield return new object[]
            {
                CreateVariant(VT_I8, new UnionTypes { _i8 = -10 }),
                (long)(-10)
            };

            // VT_UI8 => ulong.
            yield return new object[]
            {
                CreateVariant(VT_UI8, new UnionTypes { _ui8 = 10 }),
                (ulong)10
            };

            yield return new object[]
            {
                CreateVariant(VT_UI8, new UnionTypes { _ui8 = 0 }),
                (ulong)0
            };

            // VT_INT => int.
            yield return new object[]
            {
                CreateVariant(VT_INT, new UnionTypes { _int = 10 }),
                10
            };

            yield return new object[]
            {
                CreateVariant(VT_INT, new UnionTypes { _int = 0 }),
                0
            };

            yield return new object[]
            {
                CreateVariant(VT_INT, new UnionTypes { _int = -10 }),
                -10
            };

            // VT_UINT => uint.
            yield return new object[]
            {
                CreateVariant(VT_UINT, new UnionTypes { _uint = 10 }),
                (uint)10
            };

            yield return new object[]
            {
                CreateVariant(VT_UINT, new UnionTypes { _uint = 0 }),
                (uint)0
            };

            // VT_VOID => null.
            yield return new object[]
            {
                CreateVariant(VT_VOID, new UnionTypes()),
                null
            };
        }

        public static IEnumerable<object[]> GetObjectForNativeVariant_TestData()
        {
            // VT_EMPTY => null.
            yield return new object[]
            {
                CreateVariant(VT_EMPTY, new UnionTypes { _byref = IntPtr.Zero }),
                null
            };

            yield return new object[]
            {
                CreateVariant(VT_EMPTY, new UnionTypes { _byref = (IntPtr)10 }),
                null
            };

            // VT_EMPTY | VT_BYREF => zero.
            object expectedZero;
            if (IntPtr.Size == 8)
            {
                expectedZero = (ulong)0;
            }
            else
            {
                expectedZero = (uint)0;
            }
            yield return new object[]
            {
                CreateVariant(VT_EMPTY | VT_BYREF, new UnionTypes { _byref = IntPtr.Zero }),
                expectedZero
            };

            object expectedTen;
            if (IntPtr.Size == 8)
            {
                expectedTen = (ulong)10;
            }
            else
            {
                expectedTen = (uint)10;
            }
            yield return new object[]
            {
                CreateVariant(VT_EMPTY | VT_BYREF, new UnionTypes { _byref = (IntPtr)10 }),
                expectedTen
            };

            // VT_RECORD.
            yield return new object[]
            {
                CreateVariant(VT_RECORD, new UnionTypes { _record = new Record { _record = IntPtr.Zero, _recordInfo = (IntPtr)1 } }),
                null
            };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        [MemberData(nameof(GetObjectForNativeVariant_PrimitivesByRef_TestData))]
        [MemberData(nameof(GetObjectForNativeVariant_TestData))]
        public void GetObjectForNativeVariant_Normal_ReturnsExpected(Variant variant, object expected)
        {
            try
            {
                Assert.Equal(expected, GetObjectForNativeVariant(variant));
            }
            finally
            {
                DeleteVariant(variant);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void GetObjectForNativeVariant_ErrorMissing_ReturnsTypeMissing()
        {
            // This cannot be in the [MemberData] as XUnit uses reflection to invoke the test method
            // and Type.Missing is handled specially by the runtime.
            GetObjectForNativeVariant_Normal_ReturnsExpected(CreateVariant(VT_ERROR, new UnionTypes { _error = unchecked((int)0x80020004) }), Type.Missing);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        [MemberData(nameof(GetObjectForNativeVariant_PrimitivesByRef_TestData))]
        public void GetObjectForNativeVariant_NestedVariant_ReturnsExpected(Variant source, object expected)
        {
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<Variant>());
            try
            {
                Marshal.StructureToPtr(source, ptr, fDeleteOld: false);

                Variant variant = CreateVariant(VT_VARIANT | VT_BYREF, new UnionTypes { _pvarVal = ptr });
                Assert.Equal(expected, GetObjectForNativeVariant(variant));
            }
            finally
            {
                DeleteVariant(source);
                Marshal.DestroyStructure<Variant>(ptr);
                Marshal.FreeHGlobal(ptr);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void GetObjectForNativeVariant_Record_Throws()
        {
            int record = 10;
            var recordInfo = new RecordInfo { Guid = typeof(int).GUID };
            IntPtr pRecord = Marshal.AllocHGlobal(Marshal.SizeOf<int>());
            IntPtr pRecordInfo = Marshal.GetComInterfaceForObject<RecordInfo, IRecordInfo>(recordInfo);
            try
            {
                Marshal.StructureToPtr(record, pRecord, fDeleteOld: false);

                Variant variant = CreateVariant(VT_RECORD, new UnionTypes
                {
                    _record = new Record
                    {
                        _record = pRecord,
                        _recordInfo = pRecordInfo
                    }
                });

                Assert.Throws<ArgumentException>(() => GetObjectForNativeVariant(variant));
            }
            finally
            {
                Marshal.DestroyStructure<int>(pRecord);
                Marshal.FreeHGlobal(pRecord);
                Marshal.Release(pRecordInfo);
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        [MemberData(nameof(GetObjectForNativeVariant_PrimitivesByRef_TestData))]
        public unsafe void GetObjectForNativeVariant_ByRef_ReturnsExpected(Variant source, object value)
        {
            try
            {
                IntPtr ptr = new IntPtr(&source.m_Variant._unionTypes);

                var variant = new Variant();
                variant.m_Variant.vt = (ushort)(source.m_Variant.vt | VT_BYREF);
                variant.m_Variant._unionTypes._byref = ptr;

                Assert.Equal(value, GetObjectForNativeVariant(variant));
            }
            finally
            {
                DeleteVariant(source);
            }
        }
    }
}
