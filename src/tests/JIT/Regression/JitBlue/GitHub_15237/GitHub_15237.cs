using System;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

namespace UnsafeTesting
{
    public class Program
    {
        [Fact]
        public static int TestEntryPoint()
        {
            float UnsafeAs = LengthSquaredUnsafeAs();
            Console.WriteLine($"Unsafe.As           : {UnsafeAs}");
            float UnsafeRead = LengthSquaredUnsafeRead();
            Console.WriteLine($"Unsafe.Read         : {UnsafeRead}");
            float UnsafeReadUnaligned = LengthSquaredUnsafeReadUnaligned();
            Console.WriteLine($"Unsafe.ReadUnaligned: {UnsafeReadUnaligned}");
            float NoVectors = LengthSquaredUnsafeReadUnaligned();
            Console.WriteLine($"No Vectors          : {NoVectors}");
            float ManualVectors = LengthSquaredUnsafeReadUnaligned();
            Console.WriteLine($"Manual Vectors      : {ManualVectors}");
            if ((Math.Abs(UnsafeAs - ManualVectors) > Single.Epsilon) ||
                (Math.Abs(UnsafeRead - ManualVectors) > Single.Epsilon) ||
                (Math.Abs(UnsafeReadUnaligned - ManualVectors) > Single.Epsilon) ||
                (Math.Abs(NoVectors - ManualVectors) > Single.Epsilon))
            {
                Console.WriteLine("FAIL");
                return -1;
            }
            else
            {
                Console.WriteLine("PASS");
                return 100;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float LengthSquaredUnsafeAs()
        {
            QuaternionStruct start = new QuaternionStruct(8.5f, 9.4f, 1.2f, 1f);

            return start.LengthSquaredUnsafeAs();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float LengthSquaredUnsafeRead()
        {
            QuaternionStruct start = new QuaternionStruct(8.5f, 9.4f, 1.2f, 1f);

            return start.LengthSquaredUnsafeRead();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float LengthSquaredUnsafeReadUnaligned()
        {
            QuaternionStruct start = new QuaternionStruct(8.5f, 9.4f, 1.2f, 1f);

            return start.LengthSquaredUnsafeReadUnaligned();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float LengthSquaredNoVectors()
        {
            QuaternionStruct start = new QuaternionStruct(8.5f, 9.4f, 1.2f, 1f);

            return start.LengthSquaredNoVectors();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float LengthSquaredManualVectors()
        {
            QuaternionStruct start = new QuaternionStruct(8.5f, 9.4f, 1.2f, 1f);

            return start.LengthSquaredManualVectors();
        }
    }

    public struct QuaternionStruct : IEquatable<QuaternionStruct>
    {
        public float X;
        public float Y;
        public float Z;
        public float W;

        public QuaternionStruct(float x, float y, float z, float w)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.W = w;
        }

        public float LengthSquaredManualVectors() => ToVector4(this).LengthSquared();

        public float LengthSquaredUnsafeAs()
        {
            Vector4 q = Unsafe.As<QuaternionStruct, Vector4>(ref this);

            return q.LengthSquared();
        }

        public unsafe float LengthSquaredUnsafeRead()
        {
            fixed (QuaternionStruct* p = &this)
            {
                Vector4 q = Unsafe.Read<Vector4>(p);

                return q.LengthSquared();
            }
        }

        public float LengthSquaredUnsafeReadUnaligned()
        {
            Vector4 q = Unsafe.ReadUnaligned<Vector4>(ref Unsafe.As<QuaternionStruct, byte>(ref this));

            return q.LengthSquared();
        }

        public float LengthSquaredNoVectors()
        {
            return X * X + Y * Y + Z * Z + W * W;
        }

        public bool Equals(QuaternionStruct other)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            CultureInfo ci = CultureInfo.CurrentCulture;

            return $"{{X:{X.ToString(ci)} Y:{Y.ToString(ci)} Z:{Z.ToString(ci)} W:{W.ToString(ci)}}}";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static QuaternionStruct FromVector4(Vector4 vector)
        {
            return new QuaternionStruct(vector.X, vector.Y, vector.Z, vector.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector4 ToVector4(QuaternionStruct quaternionStruct)
        {
            return new Vector4(quaternionStruct.X, quaternionStruct.Y, quaternionStruct.Z, quaternionStruct.W);
        }
    }
}
