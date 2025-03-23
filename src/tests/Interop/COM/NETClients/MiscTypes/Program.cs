// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
namespace NetClient
{
    using System;
    using System.Drawing;
    using System.Reflection;
    using System.Runtime.InteropServices;

    using TestLibrary;
    using Xunit;
    using Server.Contract;
    using Server.Contract.Servers;

    struct Struct {}

    enum LongBasedEnum : long {}

    class SomeSafeHandle : SafeHandle
    {
        public SomeSafeHandle() : base((nint)1, true) {}
        public override bool IsInvalid => true;
        protected override bool ReleaseHandle() => true;
    }

    class SomeCriticalHandle : CriticalHandle
    {
        public SomeCriticalHandle() : base((nint)1) {}
        public override bool IsInvalid => true;
        protected override bool ReleaseHandle() => true;
    }

    class CustomConvertible : IConvertible
    {
        public const long Value = 0x1234567890ABCDEF;
        public TypeCode GetTypeCode() => TypeCode.Int64;
        public bool ToBoolean(IFormatProvider? provider) => throw new NotImplementedException();
        public char ToChar(IFormatProvider? provider) => throw new NotImplementedException();
        public sbyte ToSByte(IFormatProvider? provider) => throw new NotImplementedException();
        public byte ToByte(IFormatProvider? provider) => throw new NotImplementedException();
        public short ToInt16(IFormatProvider? provider) => throw new NotImplementedException();
        public ushort ToUInt16(IFormatProvider? provider) => throw new NotImplementedException();
        public int ToInt32(IFormatProvider? provider) => throw new NotImplementedException();
        public uint ToUInt32(IFormatProvider? provider) => throw new NotImplementedException();
        public long ToInt64(IFormatProvider? provider) => Value;
        public ulong ToUInt64(IFormatProvider? provider) => throw new NotImplementedException();
        public float ToSingle(IFormatProvider? provider) => throw new NotImplementedException();
        public double ToDouble(IFormatProvider? provider) => throw new NotImplementedException();
        public decimal ToDecimal(IFormatProvider? provider) => throw new NotImplementedException();
        public DateTime ToDateTime(IFormatProvider? provider) => throw new NotImplementedException();
        public string ToString(IFormatProvider? provider) => throw new NotImplementedException();
        public object ToType(Type conversionType, IFormatProvider? provider) => throw new NotImplementedException();
    }

    public unsafe class Program
    {
        [Fact]
        public static int TestEntryPoint()
        {
            // RegFree COM is not supported on Windows Nano
            if (TestLibrary.Utilities.IsWindowsNanoServer)
            {
                return 100;
            }

            try
            {
                ValidationTests();
                ValidateNegativeTests();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Test object interop failure: {e}");
                return 101;
            }

            return 100;
        }

        private class InterfaceImpl : Server.Contract.IInterface2
        {
        }

        private static void ValidationTests()
        {
            Console.WriteLine($"Running {nameof(ValidationTests)} ...");

            var miscTypeTesting = (Server.Contract.Servers.MiscTypesTesting)new Server.Contract.Servers.MiscTypesTestingClass();

            Console.WriteLine("-- Primitives <=> VARIANT...");
            {
                object expected = null;
                Assert.Equal(expected, miscTypeTesting.Marshal_Variant(expected));
            }
            {
                var expected = DBNull.Value;
                Assert.Equal(expected, miscTypeTesting.Marshal_Variant(expected));
            }
            {
                var expected = (sbyte)0x0f;
                Assert.Equal(expected, miscTypeTesting.Marshal_Variant(expected));
            }
            {
                var expected = (short)0x07ff;
                Assert.Equal(expected, miscTypeTesting.Marshal_Variant(expected));
            }
            {
                var expected = (int)0x07ffffff;
                Assert.Equal(expected, miscTypeTesting.Marshal_Variant(expected));
            }
            {
                var expected = (long)0x07ffffffffffffff;
                Assert.Equal(expected, miscTypeTesting.Marshal_Variant(expected));
            }
            {
                var expected = 123.456f;
                Assert.Equal(expected, miscTypeTesting.Marshal_Variant(expected));
            }
            {
                var expected = 123.456;
                Assert.Equal(expected, miscTypeTesting.Marshal_Variant(expected));
            }
            {
                var expected = true;
                Assert.Equal(expected, miscTypeTesting.Marshal_Variant(expected));
            }
            {
                var expected = false;
                Assert.Equal(expected, miscTypeTesting.Marshal_Variant(expected));
            }
            {
                var expected = 123.456m;
                Assert.Equal(expected, miscTypeTesting.Marshal_Variant(expected));
            }
            {
                var expected = unchecked((nint)0x07ffffffffffffff);
                Assert.Equal((int)expected, miscTypeTesting.Marshal_Variant(expected));
            }
            {
                var expected = (LongBasedEnum)0x07ffffffffffffff;
                Assert.Equal((long)expected, miscTypeTesting.Marshal_Variant(expected));
            }
            {
                var expected = new DateTime(9999, 12, 31);
                Assert.Equal(expected, miscTypeTesting.Marshal_Variant(expected));
            }

            Console.WriteLine("-- BSTR <=> VARIANT...");
            {
                var expected = "The quick Fox jumped over the lazy Dog.";
                Assert.Equal(expected, miscTypeTesting.Marshal_Variant(expected));
            }

            Console.WriteLine("-- Special types <=> VARIANT...");
            {
                var expected = Type.Missing;
                Assert.Equal(expected, miscTypeTesting.Marshal_Variant(expected));
            }
            {
                var expected = Color.Red;
                Assert.Equal((uint)ColorTranslator.ToOle(expected), miscTypeTesting.Marshal_Variant(expected));
            }

            Console.WriteLine("-- Wrappers <=> VARIANT...");
#pragma warning disable 0618 // CurrencyWrapper is obsolete
            {
                var expected = 123.456m;
                Assert.Equal(expected, miscTypeTesting.Marshal_Variant(new CurrencyWrapper(expected)));
            }
#pragma warning restore 0618
            {
                var expected = "The quick Fox jumped over the lazy Dog.";
                Assert.Equal(expected, miscTypeTesting.Marshal_Variant(new BStrWrapper(expected)));
            }
            {
                var expected = unchecked((int)0x80004005);
                Assert.Equal(expected, miscTypeTesting.Marshal_Variant(new ErrorWrapper(expected)));
            }
            {
                var expected = unchecked((int)0x80020004); // DISP_E_PARAMNOTFOUND
                Assert.Equal(Type.Missing, miscTypeTesting.Marshal_Variant(new ErrorWrapper(expected)));
            }
            {
                var expected = miscTypeTesting;
                Assert.Equal(expected, miscTypeTesting.Marshal_Variant(new UnknownWrapper(expected)));
            }

            Console.WriteLine("-- Arrays <=> VARIANT...");
            {
                var expected = new int[] { 1, 2, 3 };
                Assert.Equal(expected, miscTypeTesting.Marshal_Variant(expected));
            }
            {
                var expected = new string[] { "quick", "brown", "fox" };
                Assert.Equal(expected, miscTypeTesting.Marshal_Variant(expected));
            }

            Console.WriteLine("-- IUnknown <=> VARIANT...");
            {
                var expected = miscTypeTesting;
                Assert.Equal(expected, miscTypeTesting.Marshal_Variant(expected));
            }

            Console.WriteLine("-- IConvertible <=> VARIANT...");
            {
                Assert.Equal(CustomConvertible.Value, miscTypeTesting.Marshal_Variant(new CustomConvertible()));
            }

            Console.WriteLine("-- System.Guid <=> VARIANT...");
            {
                var expected = new Guid("{8EFAD956-B33D-46CB-90F4-45F55BA68A96}");
                Assert.Equal(expected, miscTypeTesting.Marshal_Variant(expected));
            }

            Console.WriteLine("-- Interfaces...");
            {
                var interfaceMaybe = miscTypeTesting.Marshal_Interface(new InterfaceImpl());
                Assert.True(interfaceMaybe is Server.Contract.IInterface1);
                Assert.True(interfaceMaybe is Server.Contract.IInterface2);
            }
        }

        private static void ValidateNegativeTests()
        {
            Console.WriteLine($"Running {nameof(ValidateNegativeTests)} ...");

            var miscTypeTesting = (Server.Contract.Servers.MiscTypesTesting)new Server.Contract.Servers.MiscTypesTestingClass();

            Console.WriteLine("-- DispatchWrapper with non-IDispatch object <=> VARIANT...");
            {
                Assert.Throws<InvalidCastException>(() => miscTypeTesting.Marshal_Variant(new DispatchWrapper(miscTypeTesting)));
            }
            Console.WriteLine("-- Unmappable types <=> VARIANT...");
            {
                Assert.Throws<ArgumentException>(() => miscTypeTesting.Marshal_Variant(TimeSpan.FromSeconds(1)));
            }
            {
                Assert.Throws<ArgumentException>(() => miscTypeTesting.Marshal_Variant(new SomeSafeHandle()));
            }
            {
                Assert.Throws<ArgumentException>(() => miscTypeTesting.Marshal_Variant(new SomeCriticalHandle()));
            }
            {
                Assert.Throws<ArgumentException>(() => miscTypeTesting.Marshal_Variant(new VariantWrapper(null)));
            }

            Console.WriteLine("-- User defined ValueType <=> VARIANT...");
            {
                Assert.Throws<NotSupportedException>(() => miscTypeTesting.Marshal_Variant(new Struct()));
            }
        }
    }
}
