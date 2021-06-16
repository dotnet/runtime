// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Since https://github.com/dotnet/runtime/issues/6775 was a performance issue,
// there's not really a correctness test for this.
// However, this is a very simple test that can be used to compare the code generated
// for a non-accelerated vector of 3 floats, a "raw" Vector3 and a Vector3
// wrapped in a struct.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Numerics;

namespace Test01
{
    public struct SimpleVector3
    {
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public SimpleVector3( float x, float y, float z )
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public static SimpleVector3 operator +( SimpleVector3 a, SimpleVector3 b )
            => new SimpleVector3( a.x + b.x, a.y + b.y, a.z + b.z );

        public float X
        {
            get { return x; }
            set { x = value; }
        }

        public float Y
        {
            get { return y; }
            set { y = value; }
        }

        public float Z
        {
            get { return z; }
            set { z = value; }
        }

        float x, y, z;
    }

    public struct WrappedVector3
    {
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public WrappedVector3( float x, float y, float z )
        {
            v = new System.Numerics.Vector3( x, y, z );
        }

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        WrappedVector3( System.Numerics.Vector3 v )
        {
            this.v = v;
        }

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public static WrappedVector3 operator +( WrappedVector3 a, WrappedVector3 b )
            => new WrappedVector3( a.v + b.v );

        public float X
        {
            get { return v.X; }
            set { v.X = value; }
        }

        public float Y
        {
            get { return v.Y; }
            set { v.Y = value; }
        }
                
        public float Z
        {
            get { return v.Z; }
            set { v.Z = value; }
        }

        Vector3 v;
    }

    public class Program
    {
        public const int DefaultSeed = 20010415;
        public static int Seed = Environment.GetEnvironmentVariable("CORECLR_SEED") switch
        {
            string seedStr when seedStr.Equals("random", StringComparison.OrdinalIgnoreCase) => new Random().Next(),
            string seedStr when int.TryParse(seedStr, out int envSeed) => envSeed,
            _ => DefaultSeed
        };

        static Random random = new Random(Seed);
        [MethodImpl( MethodImplOptions.NoInlining )]
        static SimpleVector3 RandomSimpleVector3()
            => new SimpleVector3( (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble() );
        [MethodImpl(MethodImplOptions.NoInlining)]
        static WrappedVector3 RandomWrappedVector3()
            => new WrappedVector3( (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble() );
        [MethodImpl(MethodImplOptions.NoInlining)]
        static Vector3 RandomVector3()
            => new Vector3( (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble() );

        public static float TestSimple()
        {
            var simpleA = RandomSimpleVector3();
            var simpleB = RandomSimpleVector3();
            var simpleC = simpleA + simpleB;
            Console.WriteLine("Simple Vector3: {0},{1},{2}", simpleC.X, simpleC.Y, simpleC.Z);
            return simpleC.X + simpleC.Y + simpleC.Z;
        }
        public static float TestWrapped()
        {
            var wrappedA = RandomWrappedVector3();
            var wrappedB = RandomWrappedVector3();
            var wrappedC = wrappedA + wrappedB;
            Console.WriteLine("Wrapped Vector3: {0},{1},{2}", wrappedC.X, wrappedC.Y, wrappedC.Z);
            return wrappedC.X + wrappedC.Y + wrappedC.Z;
        }
        public static float TestSIMD()
        {
            var a = RandomVector3();
            var b = RandomVector3();
            var c = a + b;
            Console.WriteLine("SIMD Vector3: {0},{1},{2}", c.X, c.Y, c.Z);
            return c.X + c.Y + c.Z;
        }
        public static int Main( string[] args )
        {
            int returnVal = 100;

            // First, test a simple (non-SIMD) Vector3 type
            float f = TestSimple();

            // Now a wrapped SIMD Vector3.
            if (TestWrapped() != f)
            {
                returnVal = -1;
            }

            // Now, SIMD Vector3
            if (TestSIMD() != f)
            {
                returnVal = -1;
            }

            return 100;
        }
    }
}

