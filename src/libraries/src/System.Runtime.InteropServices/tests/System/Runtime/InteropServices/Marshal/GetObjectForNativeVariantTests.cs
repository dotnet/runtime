// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

#pragma warning disable 618

namespace System.Runtime.InteropServices.Tests
{
    [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Marshalling between VARIANT and Object is not supported in AppX")]
    public class GetObjectForNativeVariantTests
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct Record
        {
            public IntPtr _record;
            public IntPtr _recordInfo;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct UnionTypes
        {
            [FieldOffset(0)] internal sbyte _i1;
            [FieldOffset(0)] internal short _i2;
            [FieldOffset(0)] internal int _i4;
            [FieldOffset(0)] internal long _i8;
            [FieldOffset(0)] internal byte _ui1;
            [FieldOffset(0)] internal ushort _ui2;
            [FieldOffset(0)] internal uint _ui4;
            [FieldOffset(0)] internal ulong _ui8;
            [FieldOffset(0)] internal int _int;
            [FieldOffset(0)] internal uint _uint;
            [FieldOffset(0)] internal float _r4;
            [FieldOffset(0)] internal double _r8;
            [FieldOffset(0)] internal long _cy;
            [FieldOffset(0)] internal double _date;
            [FieldOffset(0)] internal IntPtr _bstr;
            [FieldOffset(0)] internal IntPtr _unknown;
            [FieldOffset(0)] internal IntPtr _dispatch;
            [FieldOffset(0)] internal int _error;
            [FieldOffset(0)] internal IntPtr _pvarVal;
            [FieldOffset(0)] internal IntPtr _byref;
            [FieldOffset(0)] internal Record _record;
            [FieldOffset(0)] internal IntPtr _parray;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TypeUnion
        {
            public ushort vt;
            public ushort wReserved1;
            public ushort wReserved2;
            public ushort wReserved3;
            public UnionTypes _unionTypes;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct Variant
        {
            [FieldOffset(0)] public TypeUnion m_Variant;
            [FieldOffset(0)] public decimal m_decimal;

            public override string ToString() => "0x" + m_Variant.vt.ToString("X");
        }

        // Taken from wtypes.h
        public const ushort VT_EMPTY = 0;
        public const ushort VT_NULL = 1;
        public const ushort VT_I2 = 2;
        public const ushort VT_I4 = 3;
        public const ushort VT_R4 = 4;
        public const ushort VT_R8 = 5;
        public const ushort VT_CY = 6;
        public const ushort VT_DATE = 7;
        public const ushort VT_BSTR = 8;
        public const ushort VT_DISPATCH = 9;
        public const ushort VT_ERROR = 10;
        public const ushort VT_BOOL = 11;
        public const ushort VT_VARIANT = 12;
        public const ushort VT_UNKNOWN = 13;
        public const ushort VT_DECIMAL = 14;
        public const ushort VT_I1 = 16;
        public const ushort VT_UI1 = 17;
        public const ushort VT_UI2 = 18;
        public const ushort VT_UI4 = 19;
        public const ushort VT_I8 = 20;
        public const ushort VT_UI8 = 21;
        public const ushort VT_INT = 22;
        public const ushort VT_UINT = 23;
        public const ushort VT_VOID = 24;
        public const ushort VT_HRESULT = 25;
        public const ushort VT_PTR = 26;
        public const ushort VT_SAFEARRAY = 27;
        public const ushort VT_CARRAY = 28;
        public const ushort VT_USERDEFINED = 29;
        public const ushort VT_LPSTR = 30;
        public const ushort VT_LPWSTR = 31;
        public const ushort VT_RECORD = 36;
        public const ushort VT_INT_PTR = 37;
        public const ushort VT_UINT_PTR = 38;
        public const ushort VT_FILETIME = 64;
        public const ushort VT_BLOB = 65;
        public const ushort VT_STREAM = 66;
        public const ushort VT_STORAGE = 67;
        public const ushort VT_STREAMED_OBJECT = 68;
        public const ushort VT_STORED_OBJECT = 69;
        public const ushort VT_BLOB_OBJECT = 70;
        public const ushort VT_CF = 71;
        public const ushort VT_CLSID = 72;
        public const ushort VT_VERSIONED_STREAM = 73;
        public const ushort VT_BSTR_BLOB = 0xfff;
        public const ushort VT_VECTOR = 0x1000;
        public const ushort VT_ARRAY = 0x2000;
        public const ushort VT_BYREF = 0x4000;
        public const ushort VT_RESERVED = 0x8000;
        public const ushort VT_ILLEGAL = 0xffff;
        public const ushort VT_ILLEGALMASKED = 0xfff;
        public const ushort VT_TYPEMASK = 0xfff;

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

            var obj = new object();
#if !netstandard // Marshal.GetIDispatchForObject is not in netstandard2.0
            if (!PlatformDetection.IsNetCore)
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
#endif

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

        [Theory]
        [MemberData(nameof(GetObjectForNativeVariant_PrimitivesByRef_TestData))]
        [MemberData(nameof(GetObjectForNativeVariant_TestData))]
        [PlatformSpecific(TestPlatforms.Windows)]
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

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetObjectForNativeVariant_ErrorMissing_ReturnsTypeMissing()
        {
            // This cannot be in the [MemberData] as XUnit uses reflection to invoke the test method
            // and Type.Missing is handled specially by the runtime.
            GetObjectForNativeVariant_Normal_ReturnsExpected(CreateVariant(VT_ERROR, new UnionTypes { _error = unchecked((int)0x80020004) }), Type.Missing);
        }

        public static IEnumerable<object[]> GetObjectForNativeVariant_Decimal_TestData()
        {
            // VT_DECIMAL => decimal.
            yield return new object[] { 10.5m };
            yield return new object[] { 10m };
            yield return new object[] { 0m };
            yield return new object[] { -10m };
            yield return new object[] { -10.5m };
        }

        [Theory]
        [MemberData(nameof(GetObjectForNativeVariant_Decimal_TestData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetObjectForNativeVariant_Decimal_ReturnsExpected(decimal d)
        {
            var variant = new Variant { m_decimal = d };
            variant.m_Variant.vt = VT_DECIMAL;
            Assert.Equal(d, GetObjectForNativeVariant(variant));
        }

        [Theory]
        [MemberData(nameof(GetObjectForNativeVariant_PrimitivesByRef_TestData))]
        [PlatformSpecific(TestPlatforms.Windows)]
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

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        [ActiveIssue(31480, TargetFrameworkMonikers.Netcoreapp)]
        public void GetObjectForNativeVariant_Record_ReturnsExpected()
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
                Assert.Equal(10, GetObjectForNativeVariant(variant));
                GetObjectForNativeVariant_NestedVariant_ReturnsExpected(variant, record);

                variant.m_Variant.vt |= VT_BYREF;
                Assert.Equal(10, GetObjectForNativeVariant(variant));
            }
            finally
            {
                Marshal.DestroyStructure<int>(pRecord);
                Marshal.FreeHGlobal(pRecord);
                Marshal.Release(pRecordInfo);
            }
        }

        [Theory]
        [MemberData(nameof(GetObjectForNativeVariant_PrimitivesByRef_TestData))]
        [PlatformSpecific(TestPlatforms.Windows)]
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

        [Theory]
        [MemberData(nameof(GetObjectForNativeVariant_Decimal_TestData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public unsafe void GetObjectForNativeVariant_DecimalByRef_Success(decimal d)
        {
            IntPtr ptr = new IntPtr(&d);
            Variant variant = CreateVariant(VT_DECIMAL | VT_BYREF, new UnionTypes { _pvarVal = ptr });
            Assert.Equal(d, GetObjectForNativeVariant(variant));
        }

        public static IEnumerable<object[]> GetObjectForNativeVariant_Array_TestData()
        {
            yield return new object[]
            {
                CreateVariant(VT_ARRAY, new UnionTypes { _parray = IntPtr.Zero }),
                null
            };
        }

        [Theory]
        [MemberData(nameof(GetObjectForNativeVariant_Array_TestData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetObjectForNativeVariant_Array_ReturnsExpected(Variant source, object expected)
        {
            Assert.Equal(expected, GetObjectForNativeVariant(source));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void GetObjectForNativeVariant_Unix_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetObjectForNativeVariant(IntPtr.Zero));
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetObjectForNativeVariant<int>(IntPtr.Zero));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetObjectForNativeVariant_ZeroPointer_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("pSrcNativeVariant", () => Marshal.GetObjectForNativeVariant(IntPtr.Zero));
            AssertExtensions.Throws<ArgumentNullException>("pSrcNativeVariant", () => Marshal.GetObjectForNativeVariant<int>(IntPtr.Zero));
        }

        [Theory]
        [InlineData(VT_I2 | VT_BYREF)]
        [InlineData(VT_UI2 | VT_BYREF)]
        [InlineData(VT_I4 | VT_BYREF)]
        [InlineData(VT_R4 | VT_BYREF)]
        [InlineData(VT_R8 | VT_BYREF)]
        [InlineData(VT_CY | VT_BYREF)]
        [InlineData(VT_DATE | VT_BYREF)]
        [InlineData(VT_BSTR | VT_BYREF)]
        [InlineData(VT_DISPATCH | VT_BYREF)]
        [InlineData(VT_ERROR | VT_BYREF)]
        [InlineData(VT_BOOL | VT_BYREF)]
        [InlineData(VT_VARIANT | VT_BYREF)]
        [InlineData(VT_UNKNOWN | VT_BYREF)]
        [InlineData(VT_I1 | VT_BYREF)]
        [InlineData(VT_UI1 | VT_BYREF)]
        [InlineData(VT_UI2 | VT_BYREF)]
        [InlineData(VT_UI4 | VT_BYREF)]
        [InlineData(VT_I8 | VT_BYREF)]
        [InlineData(VT_UI8 | VT_BYREF)]
        [InlineData(VT_INT | VT_BYREF)]
        [InlineData(VT_UINT | VT_BYREF)]
        [InlineData(VT_VOID | VT_BYREF)]
        [InlineData(VT_HRESULT | VT_BYREF)]
        [InlineData(VT_PTR | VT_BYREF)]
        [InlineData(VT_SAFEARRAY | VT_BYREF)]
        [InlineData(VT_CARRAY | VT_BYREF)]
        [InlineData(VT_USERDEFINED | VT_BYREF)]
        [InlineData(VT_LPSTR | VT_BYREF)]
        [InlineData(VT_LPWSTR | VT_BYREF)]
        [InlineData(VT_RECORD | VT_BYREF)]
        [InlineData(VT_INT_PTR | VT_BYREF)]
        [InlineData(VT_UINT_PTR | VT_BYREF)]
        [InlineData(VT_FILETIME | VT_BYREF)]
        [InlineData(VT_BLOB | VT_BYREF)]
        [InlineData(VT_STREAM | VT_BYREF)]
        [InlineData(VT_STORAGE | VT_BYREF)]
        [InlineData(VT_STREAMED_OBJECT | VT_BYREF)]
        [InlineData(VT_STORED_OBJECT | VT_BYREF)]
        [InlineData(VT_BLOB_OBJECT | VT_BYREF)]
        [InlineData(VT_CF | VT_BYREF)]
        [InlineData(VT_CLSID | VT_BYREF)]
        [InlineData(VT_VERSIONED_STREAM | VT_BYREF)]
        [InlineData(VT_BSTR_BLOB | VT_BYREF)]
        [InlineData(VT_VECTOR | VT_BYREF)]
        [InlineData(VT_ARRAY | VT_BYREF)]
        [InlineData(VT_RESERVED | VT_BYREF)]
        [InlineData(VT_ILLEGAL | VT_BYREF)]
        [InlineData(VT_ILLEGALMASKED | VT_BYREF)]
        [InlineData(VT_TYPEMASK| VT_BYREF)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetObjectForNativeVariant_ZeroByRefTypeNotEmptyOrNull_ThrowsArgumentException(ushort vt)
        {
            var variant = new Variant();
            variant.m_Variant.vt = vt;
            variant.m_Variant._unionTypes._byref = IntPtr.Zero;

            AssertExtensions.Throws<ArgumentException>(null, () => GetObjectForNativeVariant(variant));
        }

        [Theory]
        [InlineData(-657435.0)]
        [InlineData(2958466.0)]
        [InlineData(double.NegativeInfinity)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NaN)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetObjectForNativeVariant_InvalidDate_ThrowsArgumentException(double value)
        {
            Variant variant = CreateVariant(VT_DATE, new UnionTypes { _date = value });
            AssertExtensions.Throws<ArgumentException>(null, () => GetObjectForNativeVariant(variant));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetObjectForNativeVariant_NoDataForRecord_ThrowsArgumentException()
        {
            Variant variant = CreateVariant(VT_RECORD, new UnionTypes { _record = new Record { _recordInfo = IntPtr.Zero } });
            AssertExtensions.Throws<ArgumentException>(null, () => GetObjectForNativeVariant(variant));
        }

        public static IEnumerable<object[]> GetObjectForNativeVariant_NoSuchGuid_TestData()
        {
            yield return new object[] { typeof(string).GUID };
            yield return new object[] { Guid.Empty };
        }

        [Theory]
        [MemberData(nameof(GetObjectForNativeVariant_NoSuchGuid_TestData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetObjectForNativeVariant_NoSuchGuid_ThrowsArgumentException(Guid guid)
        {
            int record = 10;
            var recordInfo = new RecordInfo { Guid = guid };
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
                AssertExtensions.Throws<ArgumentException>(null, () => GetObjectForNativeVariant(variant));
            }
            finally
            {
                Marshal.DestroyStructure<int>(pRecord);
                Marshal.FreeHGlobal(pRecord);
                Marshal.Release(pRecordInfo);
            }
        }

        public static IEnumerable<object[]> GetObjectForNativeVariant_CantMap_ThrowsArgumentException()
        {
            yield return new object[] { CreateVariant(VT_VARIANT, new UnionTypes()) };
            yield return new object[] { CreateVariant(15, new UnionTypes()) };
            yield return new object[] { CreateVariant(VT_HRESULT, new UnionTypes()) };
            yield return new object[] { CreateVariant(VT_PTR, new UnionTypes()) };
            yield return new object[] { CreateVariant(VT_SAFEARRAY, new UnionTypes()) };
            yield return new object[] { CreateVariant(VT_CARRAY, new UnionTypes()) };
            yield return new object[] { CreateVariant(VT_USERDEFINED, new UnionTypes()) };
            yield return new object[] { CreateVariant(VT_LPSTR, new UnionTypes()) };
            yield return new object[] { CreateVariant(VT_LPWSTR, new UnionTypes()) };
            yield return new object[] { CreateVariant(32, new UnionTypes()) };
            yield return new object[] { CreateVariant(33, new UnionTypes()) };
            yield return new object[] { CreateVariant(34, new UnionTypes()) };
            yield return new object[] { CreateVariant(35, new UnionTypes()) };
            yield return new object[] { CreateVariant(VT_INT_PTR, new UnionTypes()) };
            yield return new object[] { CreateVariant(VT_UINT_PTR, new UnionTypes()) };
            yield return new object[] { CreateVariant(39, new UnionTypes()) };
            yield return new object[] { CreateVariant(63, new UnionTypes()) };
            yield return new object[] { CreateVariant(VT_FILETIME, new UnionTypes()) };
            yield return new object[] { CreateVariant(VT_BLOB, new UnionTypes()) };
            yield return new object[] { CreateVariant(VT_STREAM, new UnionTypes()) };
            yield return new object[] { CreateVariant(VT_STORAGE, new UnionTypes()) };
            yield return new object[] { CreateVariant(VT_STREAMED_OBJECT, new UnionTypes()) };
            yield return new object[] { CreateVariant(VT_STORED_OBJECT, new UnionTypes()) };
            yield return new object[] { CreateVariant(VT_BLOB_OBJECT, new UnionTypes()) };
            yield return new object[] { CreateVariant(VT_CF, new UnionTypes()) };
            yield return new object[] { CreateVariant(VT_CLSID, new UnionTypes()) };
            yield return new object[] { CreateVariant(VT_VERSIONED_STREAM, new UnionTypes()) };
            yield return new object[] { CreateVariant(74, new UnionTypes()) };
            yield return new object[] { CreateVariant(127, new UnionTypes()) };
        }

        [Theory]
        [MemberData(nameof(GetObjectForNativeVariant_CantMap_ThrowsArgumentException))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetObjectForNativeVariant_CantMap_ThrowsArgumentException(Variant variant)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => GetObjectForNativeVariant(variant));
        }

        [Theory]
        [MemberData(nameof(GetObjectForNativeVariant_CantMap_ThrowsArgumentException))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetObjectForNativeVariant_CantMapByRef_ThrowsArgumentException(Variant variant)
        {
            variant.m_Variant.vt |= VT_BYREF;
            AssertExtensions.Throws<ArgumentException>(null, () => GetObjectForNativeVariant(variant));
        }

        public static IEnumerable<object[]> GetObjectForNativeVariant_InvalidVarType_TestData()
        {
            yield return new object[] { 128 };
            yield return new object[] { 4094 };
            yield return new object[] { VT_BSTR_BLOB };
            yield return new object[] { VT_ILLEGALMASKED };
            yield return new object[] { VT_TYPEMASK };
            yield return new object[] { VT_VECTOR };
            yield return new object[] { 4097 };
            yield return new object[] { 8191 };
            yield return new object[] { 16383 };
            yield return new object[] { VT_RESERVED };
            yield return new object[] { 65534 };
            yield return new object[] { VT_ILLEGAL };
        }

        [Theory]
        [MemberData(nameof(GetObjectForNativeVariant_InvalidVarType_TestData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetObjectForNativeVariant_InvalidVarType_InvalidOleVariantTypeException(ushort vt)
        {
            Variant variant = CreateVariant(vt, new UnionTypes { _byref = (IntPtr)10 });
            Assert.Throws<InvalidOleVariantTypeException>(() => GetObjectForNativeVariant(variant));
        }

        [Theory]
        [MemberData(nameof(GetObjectForNativeVariant_InvalidVarType_TestData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetObjectForNativeVariant_InvalidVarTypeByRef_InvalidOleVariantTypeException(ushort vt)
        {
            Variant variant = CreateVariant((ushort)(vt | VT_BYREF), new UnionTypes { _byref = (IntPtr)10 });
            Assert.Throws<InvalidOleVariantTypeException>(() => GetObjectForNativeVariant(variant));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetObjectForNativeVariant_ByRefNestedVariant_InvalidOleVariantTypeException()
        {
            var source = new Variant();
            source.m_Variant.vt = VT_INT | VT_BYREF;
            source.m_Variant._unionTypes._byref = (IntPtr)10;

            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<Variant>());
            try
            {
                Marshal.StructureToPtr(source, ptr, fDeleteOld: false);

                var variant = new Variant();
                variant.m_Variant.vt = VT_VARIANT | VT_BYREF;
                variant.m_Variant._unionTypes._pvarVal = ptr;

                Assert.Throws<InvalidOleVariantTypeException>(() => GetObjectForNativeVariant(variant));
            }
            finally
            {
                Marshal.DestroyStructure<Variant>(ptr);
                Marshal.FreeHGlobal(ptr);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetObjectForNativeVariant_ArrayOfEmpty_ThrowsInvalidOleVariantTypeException()
        {
            Variant variant = CreateVariant(VT_ARRAY, new UnionTypes { _parray = (IntPtr)10 });
            Assert.Throws<InvalidOleVariantTypeException>(() => GetObjectForNativeVariant(variant));
        }

        private static object GetObjectForNativeVariant(Variant variant)
        {
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<Variant>());
            try
            {
                Marshal.StructureToPtr(variant, ptr, fDeleteOld: false);
                return Marshal.GetObjectForNativeVariant(ptr);
            }
            finally
            {
                Marshal.DestroyStructure<Variant>(ptr);
                Marshal.FreeHGlobal(ptr);
            }
        }

        private static Variant CreateVariant(ushort vt, UnionTypes union)
        {
            var variant = new Variant();
            variant.m_Variant.vt = vt;
            variant.m_Variant._unionTypes = union;
            return variant;
        }

        private static void DeleteVariant(Variant variant)
        {
            if (variant.m_Variant.vt == VT_BSTR)
            {
                Marshal.FreeBSTR(variant.m_Variant._unionTypes._bstr);
            }
            else if (variant.m_Variant.vt == VT_UNKNOWN && variant.m_Variant._unionTypes._unknown != IntPtr.Zero)
            {
                Marshal.Release(variant.m_Variant._unionTypes._unknown);
            }
            else if (variant.m_Variant.vt == VT_DISPATCH && variant.m_Variant._unionTypes._dispatch != IntPtr.Zero)
            {
                Marshal.Release(variant.m_Variant._unionTypes._dispatch);
            }
        }

        public class RecordInfo : IRecordInfo
        {
            public Guid Guid { get; set; }

            public void RecordInit([Out] IntPtr pvNew)
            {
                throw new NotImplementedException();
            }

            public void RecordClear([In] IntPtr pvExisting)
            {
                throw new NotImplementedException();
            }

            public void RecordCopy([In] IntPtr pvExisting, [Out] IntPtr pvNew)
            {
                throw new NotImplementedException();
            }

            public void GetGuid(out Guid pguid)
            {
                pguid = Guid;
            }

            public void GetName([MarshalAs(UnmanagedType.BStr)] out string pbstrName)
            {
                throw new NotImplementedException();
            }

            public void GetSize(out uint pcbSize)
            {
                throw new NotImplementedException();
            }

            public void GetTypeInfo([MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "System.Runtime.InteropServices.CustomMarshalers.TypeToTypeInfoMarshaler")] out Type ppTypeInfo)
            {
                throw new NotImplementedException();
            }

            public void GetField([In] IntPtr pvData, [In, MarshalAs(UnmanagedType.LPWStr)] string szFieldName, [MarshalAs(UnmanagedType.Struct)] out object pvarField)
            {
                throw new NotImplementedException();
            }

            public void GetFieldNoCopy([In] IntPtr pvData, [In, MarshalAs(UnmanagedType.LPWStr)] string szFieldName, [MarshalAs(UnmanagedType.Struct)] out object pvarField, out IntPtr ppvDataCArray)
            {
                throw new NotImplementedException();
            }

            public void PutField([In] uint wFlags, [In, Out] IntPtr pvData, [In, MarshalAs(UnmanagedType.LPWStr)] string szFieldName, [In, MarshalAs(UnmanagedType.Struct)] ref object pvarField)
            {
                throw new NotImplementedException();
            }

            public void PutFieldNoCopy([In] uint wFlags, [In, Out] IntPtr pvData, [In, MarshalAs(UnmanagedType.LPWStr)] string szFieldName, [In, MarshalAs(UnmanagedType.Struct)] ref object pvarField)
            {
                throw new NotImplementedException();
            }

            public void GetFieldNames([In, Out] ref uint pcNames, [MarshalAs(UnmanagedType.BStr)] out string rgBstrNames)
            {
                throw new NotImplementedException();
            }

            public int IsMatchingType([In, MarshalAs(UnmanagedType.Interface)] IRecordInfo pRecordInfo)
            {
                throw new NotImplementedException();
            }

            public IntPtr RecordCreate()
            {
                throw new NotImplementedException();
            }

            public void RecordCreateCopy([In] IntPtr pvSource, out IntPtr ppvDest)
            {
                throw new NotImplementedException();
            }

            public void RecordDestroy([In] IntPtr pvRecord)
            {
                throw new NotImplementedException();
            }
        }

        [Guid("0000002F-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComImport]
        public interface IRecordInfo
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            void RecordInit([Out] IntPtr pvNew);

            [MethodImpl(MethodImplOptions.InternalCall)]
            void RecordClear([In] IntPtr pvExisting);

            [MethodImpl(MethodImplOptions.InternalCall)]
            void RecordCopy([In] IntPtr pvExisting, [Out] IntPtr pvNew);

            [MethodImpl(MethodImplOptions.InternalCall)]
            void GetGuid(out Guid pguid);

            [MethodImpl(MethodImplOptions.InternalCall)]
            void GetName([MarshalAs(UnmanagedType.BStr)] out string pbstrName);

            [MethodImpl(MethodImplOptions.InternalCall)]
            void GetSize(out uint pcbSize);

            [MethodImpl(MethodImplOptions.InternalCall)]
            void GetTypeInfo([MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "System.Runtime.InteropServices.CustomMarshalers.TypeToTypeInfoMarshaler")] out Type ppTypeInfo);

            [MethodImpl(MethodImplOptions.InternalCall)]
            void GetField([In] IntPtr pvData, [MarshalAs(UnmanagedType.LPWStr)] [In] string szFieldName, [MarshalAs(UnmanagedType.Struct)] out object pvarField);

            [MethodImpl(MethodImplOptions.InternalCall)]
            void GetFieldNoCopy([In] IntPtr pvData, [MarshalAs(UnmanagedType.LPWStr)] [In] string szFieldName, [MarshalAs(UnmanagedType.Struct)] out object pvarField, out IntPtr ppvDataCArray);

            [MethodImpl(MethodImplOptions.InternalCall)]
            void PutField([In] uint wFlags, [In] [Out] IntPtr pvData, [MarshalAs(UnmanagedType.LPWStr)] [In] string szFieldName, [MarshalAs(UnmanagedType.Struct)] [In] ref object pvarField);

            [MethodImpl(MethodImplOptions.InternalCall)]
            void PutFieldNoCopy([In] uint wFlags, [In] [Out] IntPtr pvData, [MarshalAs(UnmanagedType.LPWStr)] [In] string szFieldName, [MarshalAs(UnmanagedType.Struct)] [In] ref object pvarField);

            [MethodImpl(MethodImplOptions.InternalCall)]
            void GetFieldNames([In] [Out] ref uint pcNames, [MarshalAs(UnmanagedType.BStr)] out string rgBstrNames);

            [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
            int IsMatchingType([MarshalAs(UnmanagedType.Interface)] [In] IRecordInfo pRecordInfo);

            [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
            IntPtr RecordCreate();

            [MethodImpl(MethodImplOptions.InternalCall)]
            void RecordCreateCopy([In] IntPtr pvSource, out IntPtr ppvDest);

            [MethodImpl(MethodImplOptions.InternalCall)]
            void RecordDestroy([In] IntPtr pvRecord);
        }
    }
}

#pragma warning restore 618
