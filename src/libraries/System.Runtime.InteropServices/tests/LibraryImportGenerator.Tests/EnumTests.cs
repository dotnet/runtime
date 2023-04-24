// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;

using Xunit;

namespace LibraryImportGenerator.IntegrationTests
{
    partial class NativeExportsNE
    {
        public partial class IntEnum
        {
            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "subtract_return_int")]
            public static partial EnumTests.IntEnum Subtract_Return(EnumTests.IntEnum a, EnumTests.IntEnum b);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "subtract_out_int")]
            public static partial void Subtract_Out(EnumTests.IntEnum a, EnumTests.IntEnum b, out EnumTests.IntEnum c);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "subtract_ref_int")]
            public static partial void Subtract_Ref(EnumTests.IntEnum a, ref EnumTests.IntEnum b);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "subtract_ref_int")]
            public static partial void Subtract_In(EnumTests.IntEnum a, in EnumTests.IntEnum b);
        }

        public partial class ByteEnum
        {
            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "subtract_return_byte")]
            public static partial EnumTests.ByteEnum Subtract_Return(EnumTests.ByteEnum a, EnumTests.ByteEnum b);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "subtract_out_byte")]
            public static partial void Subtract_Out(EnumTests.ByteEnum a, EnumTests.ByteEnum b, out EnumTests.ByteEnum c);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "subtract_ref_byte")]
            public static partial void Subtract_Ref(EnumTests.ByteEnum a, ref EnumTests.ByteEnum b);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "subtract_ref_byte")]
            public static partial void Subtract_In(EnumTests.ByteEnum a, in EnumTests.ByteEnum b);
        }
    }

    public class EnumTests
    {
        internal enum IntEnum
        {
            Zero,
            One,
            Two,
            Three,
            Max = int.MaxValue
        }

        internal enum ByteEnum : byte
        {
            Zero,
            One,
            Two,
            Three,
            Max = byte.MaxValue
        }

        [Fact]
        public void EnumByValue()
        {
            {
                IntEnum ret = NativeExportsNE.IntEnum.Subtract_Return(IntEnum.Max, IntEnum.Zero);
                Assert.Equal(IntEnum.Max, ret);
            }
            {
                ByteEnum ret = NativeExportsNE.ByteEnum.Subtract_Return(ByteEnum.Max, ByteEnum.Zero);
                Assert.Equal(ByteEnum.Max, ret);
            }
        }

        [Fact]
        public void EnumByRef()
        {
            {
                IntEnum a = IntEnum.Three;
                IntEnum b = IntEnum.Two;
                IntEnum expected = IntEnum.One;

                IntEnum ret;
                NativeExportsNE.IntEnum.Subtract_Out(a, b, out ret);
                Assert.Equal(expected, ret);

                IntEnum refValue = b;
                NativeExportsNE.IntEnum.Subtract_In(a, in refValue);
                Assert.Equal(expected, refValue); // Value is updated even when passed with in keyword (matches built-in system)

                refValue = b;
                NativeExportsNE.IntEnum.Subtract_Ref(a, ref refValue);
                Assert.Equal(expected, refValue);
            }

            {
                ByteEnum a = ByteEnum.Three;
                ByteEnum b = ByteEnum.Two;
                ByteEnum expected = ByteEnum.One;

                ByteEnum ret;
                NativeExportsNE.ByteEnum.Subtract_Out(a, b, out ret);
                Assert.Equal(expected, ret);

                ByteEnum refValue = b;
                NativeExportsNE.ByteEnum.Subtract_In(a, in refValue);
                Assert.Equal(expected, refValue); // Value is updated even when passed with in keyword (matches built-in system)

                refValue = b;
                NativeExportsNE.ByteEnum.Subtract_Ref(a, ref refValue);
                Assert.Equal(expected, refValue);
            }
        }
    }
}
