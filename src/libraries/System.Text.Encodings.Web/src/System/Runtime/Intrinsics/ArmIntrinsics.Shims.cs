namespace System.Runtime.Intrinsics.Arm
{
    internal abstract class ArmBase
    {
        public abstract class Arm64
        {
            public const bool IsSupported = false;
            public static int LeadingZeroCount(ulong value) => throw new PlatformNotSupportedException();
            public static uint ReverseElementBits(ulong value) => throw new PlatformNotSupportedException();
        }
        public const bool IsSupported = false;
        public static int LeadingZeroCount(uint value) => throw new PlatformNotSupportedException();
        public static uint ReverseElementBits(uint value) => throw new PlatformNotSupportedException();
    }

    internal abstract class AdvSimd : ArmBase
    {
        public new abstract class Arm64 : ArmBase.Arm64
        {
            public static Vector64<byte> AddAcross(Vector64<byte> value) => throw new PlatformNotSupportedException();

            public static Vector64<byte> ZipHigh(Vector64<byte> left, Vector64<byte> right) { throw new PlatformNotSupportedException(); }

            public static Vector64<short> ZipHigh(Vector64<short> left, Vector64<short> right) { throw new PlatformNotSupportedException(); }

            public static Vector64<int> ZipHigh(Vector64<int> left, Vector64<int> right) { throw new PlatformNotSupportedException(); }

            public static Vector64<sbyte> ZipHigh(Vector64<sbyte> left, Vector64<sbyte> right) { throw new PlatformNotSupportedException(); }

            public static Vector64<float> ZipHigh(Vector64<float> left, Vector64<float> right) { throw new PlatformNotSupportedException(); }

            public static Vector64<ushort> ZipHigh(Vector64<ushort> left, Vector64<ushort> right) { throw new PlatformNotSupportedException(); }

            public static Vector64<uint> ZipHigh(Vector64<uint> left, Vector64<uint> right) { throw new PlatformNotSupportedException(); }

            public static Vector128<byte> ZipHigh(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }

            public static Vector128<double> ZipHigh(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

            public static Vector128<short> ZipHigh(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }

            public static Vector128<int> ZipHigh(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }

            public static Vector128<long> ZipHigh(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }

            public static Vector128<sbyte> ZipHigh(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }

            public static Vector128<float> ZipHigh(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

            public static Vector128<ushort> ZipHigh(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }

            public static Vector128<uint> ZipHigh(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }

            public static Vector128<ulong> ZipHigh(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }

            public static Vector64<byte> ZipLow(Vector64<byte> left, Vector64<byte> right) { throw new PlatformNotSupportedException(); }

            public static Vector64<short> ZipLow(Vector64<short> left, Vector64<short> right) { throw new PlatformNotSupportedException(); }

            public static Vector64<int> ZipLow(Vector64<int> left, Vector64<int> right) { throw new PlatformNotSupportedException(); }

            public static Vector64<sbyte> ZipLow(Vector64<sbyte> left, Vector64<sbyte> right) { throw new PlatformNotSupportedException(); }

            public static Vector64<float> ZipLow(Vector64<float> left, Vector64<float> right) { throw new PlatformNotSupportedException(); }

            public static Vector64<ushort> ZipLow(Vector64<ushort> left, Vector64<ushort> right) { throw new PlatformNotSupportedException(); }

            public static Vector64<uint> ZipLow(Vector64<uint> left, Vector64<uint> right) { throw new PlatformNotSupportedException(); }

            public static Vector128<byte> ZipLow(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }

            public static Vector128<double> ZipLow(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

            public static Vector128<short> ZipLow(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }

            public static Vector128<int> ZipLow(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }

            public static Vector128<long> ZipLow(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }

            public static Vector128<sbyte> ZipLow(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }

            public static Vector128<float> ZipLow(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

            public static Vector128<ushort> ZipLow(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }

            public static Vector128<uint> ZipLow(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }

            public static Vector128<ulong> ZipLow(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }

            public static Vector64<byte> MaxAcross(Vector128<byte> value) { throw new PlatformNotSupportedException(); }
        }

        public static Vector64<byte> PopCount(Vector64<byte> value) => throw new PlatformNotSupportedException();

        public static Vector128<double> DuplicateToVector128(double value) { throw new PlatformNotSupportedException(); }

        public static Vector128<long> DuplicateToVector128(long value) { throw new PlatformNotSupportedException(); }

        public static Vector128<ulong> DuplicateToVector128(ulong value) { throw new PlatformNotSupportedException(); }

        public static unsafe Vector128<byte> LoadVector128(byte* address) { throw new PlatformNotSupportedException(); }
    }
}
