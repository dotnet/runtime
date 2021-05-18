// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System
{
    /// <summary>
    /// Represents a pseudo-random number generator, which is an algorithm that produces a sequence of numbers
    /// that meet certain statistical requirements for randomness.
    /// </summary>
    public partial class Random
    {
        /// <summary>The underlying generator implementation.</summary>
        /// <remarks>
        /// This is separated out so that different generators can be used based on how this Random instance is constructed.
        /// If it's built from a seed, then we may need to ensure backwards compatibility for folks expecting consistent sequences
        /// based on that seed.  If the instance is actually derived from Random, then we need to ensure the derived type's
        /// overrides are called anywhere they were being called previously.  But if the instance is the base type and is constructed
        /// with the default constructor, we have a lot of flexibility as to how to optimize the performance and quality of the generator.
        /// </remarks>
        private readonly ImplBase _impl;

        /// <summary>Initializes a new instance of the <see cref="Random"/> class using a default seed value.</summary>
        public Random() =>
            // With no seed specified, if this is the base type, we can implement this however we like.
            // If it's a derived type, for compat we respect the previous implementation, so that overrides
            // are called as they were previously.
            _impl = GetType() == typeof(Random) ? new XoshiroImpl() : new Net5CompatImpl(this);

        /// <summary>Initializes a new instance of the Random class, using the specified seed value.</summary>
        /// <param name="Seed">
        /// A number used to calculate a starting value for the pseudo-random number sequence. If a negative number
        /// is specified, the absolute value of the number is used.
        /// </param>
        public Random(int Seed) =>
            // With a custom seed, for compat we respect the previous implementation so that the same sequence
            // previously output continues to be output.
            _impl = new Net5CompatImpl(this, Seed);

        /// <summary>Constructor used by <see cref="ThreadSafeRandom"/>.</summary>
        /// <param name="isThreadSafeRandom">Must be true.</param>
        protected private Random(bool isThreadSafeRandom)
        {
            Debug.Assert(isThreadSafeRandom);
            _impl = null!; // base implementation isn't used at all
        }

        /// <summary>Provides a thread-safe <see cref="Random"/> instance that may be used concurrently from any thread.</summary>
        public static Random Shared { get; } = new ThreadSafeRandom();

        /// <summary>Returns a non-negative random integer.</summary>
        /// <returns>A 32-bit signed integer that is greater than or equal to 0 and less than <see cref="int.MaxValue"/>.</returns>
        public virtual int Next()
        {
            int result = _impl.Next();
            AssertInRange(result, 0, int.MaxValue);
            return result;
        }

        /// <summary>Returns a non-negative random integer that is less than the specified maximum.</summary>
        /// <param name="maxValue">The exclusive upper bound of the random number to be generated. <paramref name="maxValue"/> must be greater than or equal to 0.</param>
        /// <returns>
        /// A 32-bit signed integer that is greater than or equal to 0, and less than <paramref name="maxValue"/>; that is, the range of return values ordinarily
        /// includes 0 but not <paramref name="maxValue"/>. However, if <paramref name="maxValue"/> equals 0, <paramref name="maxValue"/> is returned.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than 0.</exception>
        public virtual int Next(int maxValue)
        {
            if (maxValue < 0)
            {
                ThrowMaxValueMustBeNonNegative();
            }

            int result = _impl.Next(maxValue);
            AssertInRange(result, 0, maxValue);
            return result;
        }

        /// <summary>Returns a random integer that is within a specified range.</summary>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number returned. <paramref name="maxValue"/> must be greater than or equal to <paramref name="minValue"/>.</param>
        /// <returns>
        /// A 32-bit signed integer greater than or equal to <paramref name="minValue"/> and less than <paramref name="maxValue"/>; that is, the range of return values includes <paramref name="minValue"/>
        /// but not <paramref name="maxValue"/>. If minValue equals <paramref name="maxValue"/>, <paramref name="minValue"/> is returned.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="minValue"/> is greater than <paramref name="maxValue"/>.</exception>
        public virtual int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
            {
                ThrowMinMaxValueSwapped();
            }

            int result = _impl.Next(minValue, maxValue);
            AssertInRange(result, minValue, maxValue);
            return result;
        }

        /// <summary>Returns a non-negative random integer.</summary>
        /// <returns>A 64-bit signed integer that is greater than or equal to 0 and less than <see cref="long.MaxValue"/>.</returns>
        public virtual long NextInt64()
        {
            long result = _impl.NextInt64();
            AssertInRange(result, 0, long.MaxValue);
            return result;
        }

        /// <summary>Returns a non-negative random integer that is less than the specified maximum.</summary>
        /// <param name="maxValue">The exclusive upper bound of the random number to be generated. <paramref name="maxValue"/> must be greater than or equal to 0.</param>
        /// <returns>
        /// A 64-bit signed integer that is greater than or equal to 0, and less than <paramref name="maxValue"/>; that is, the range of return values ordinarily
        /// includes 0 but not <paramref name="maxValue"/>. However, if <paramref name="maxValue"/> equals 0, <paramref name="maxValue"/> is returned.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than 0.</exception>
        public virtual long NextInt64(long maxValue)
        {
            if (maxValue < 0)
            {
                ThrowMaxValueMustBeNonNegative();
            }

            long result = _impl.NextInt64(maxValue);
            AssertInRange(result, 0, maxValue);
            return result;
        }

        /// <summary>Returns a random integer that is within a specified range.</summary>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number returned. <paramref name="maxValue"/> must be greater than or equal to <paramref name="minValue"/>.</param>
        /// <returns>
        /// A 64-bit signed integer greater than or equal to <paramref name="minValue"/> and less than <paramref name="maxValue"/>; that is, the range of return values includes <paramref name="minValue"/>
        /// but not <paramref name="maxValue"/>. If minValue equals <paramref name="maxValue"/>, <paramref name="minValue"/> is returned.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="minValue"/> is greater than <paramref name="maxValue"/>.</exception>
        public virtual long NextInt64(long minValue, long maxValue)
        {
            if (minValue > maxValue)
            {
                ThrowMinMaxValueSwapped();
            }

            long result = _impl.NextInt64(minValue, maxValue);
            AssertInRange(result, minValue, maxValue);
            return result;
        }

        /// <summary>Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.</summary>
        /// <returns>A single-precision floating point number that is greater than or equal to 0.0, and less than 1.0.</returns>
        public virtual float NextSingle()
        {
            float result = _impl.NextSingle();
            AssertInRange(result);
            return result;
        }

        /// <summary>Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.</summary>
        /// <returns>A double-precision floating point number that is greater than or equal to 0.0, and less than 1.0.</returns>
        public virtual double NextDouble()
        {
            double result = _impl.NextDouble();
            AssertInRange(result);
            return result;
        }

        /// <summary>Fills the elements of a specified array of bytes with random numbers.</summary>
        /// <param name="buffer">The array to be filled with random numbers.</param>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        public virtual void NextBytes(byte[] buffer)
        {
            if (buffer is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.buffer);
            }

            _impl.NextBytes(buffer);
        }

        /// <summary>Fills the elements of a specified span of bytes with random numbers.</summary>
        /// <param name="buffer">The array to be filled with random numbers.</param>
        public virtual void NextBytes(Span<byte> buffer) => _impl.NextBytes(buffer);

        /// <summary>Returns a random floating-point number between 0.0 and 1.0.</summary>
        /// <returns>A double-precision floating point number that is greater than or equal to 0.0, and less than 1.0.</returns>
        protected virtual double Sample()
        {
            double result = _impl.Sample();
            AssertInRange(result);
            return result;
        }

        private static void ThrowMaxValueMustBeNonNegative() =>
            throw new ArgumentOutOfRangeException("maxValue", SR.Format(SR.ArgumentOutOfRange_NeedNonNegNum, "maxValue"));

        private static void ThrowMinMaxValueSwapped() =>
            throw new ArgumentOutOfRangeException("minValue", SR.Format(SR.Argument_MinMaxValue, "minValue", "maxValue"));

        [Conditional("DEBUG")]
        private static void AssertInRange(long result, long minInclusive, long maxExclusive)
        {
            if (maxExclusive > minInclusive)
            {
                if (result < minInclusive || result >= maxExclusive)
                {
                    Debug.Fail($"Expected {minInclusive} <= {result} < {maxExclusive}");
                }
            }
            else
            {
                if (result != minInclusive)
                {
                    Debug.Fail($"Expected {minInclusive} == {result}");
                }
            }
        }

        [Conditional("DEBUG")]
        private static void AssertInRange(double result)
        {
            if (result < 0.0 || result >= 1.0)
            {
                // Avoid calling result.ToString() when the Assert condition is not met
                Debug.Fail($"Expected 0.0 <= {result} < 1.0");
            }
        }

        /// <summary>Random implementation that delegates all calls to a ThreadStatic Impl instance.</summary>
        private sealed class ThreadSafeRandom : Random
        {
            // We need Random.Shared to return an instance that is thread-safe, as it may be used from multiple threads.
            // It's also possible that one thread could retrieve Shared and pass it to another thread, so Shared can't
            // just access a ThreadStatic to return a Random instance stored there.  As such, we need the instance
            // returned from Shared itself to be thread-safe, which can be accomplished either by locking around all
            // accesses or by itself accessing a ThreadStatic on every access: we've chosen the latter, as it is more
            // scalable.  With that, we have two basic approaches:
            // 1. the instance returned can be a base Random instance constructed with an _impl that is a ThreadSafeImpl.
            // 2. the instance returned can be a special Random-derived instance, where _impl is ignored and the derived
            //    type overrides all methods on the base.
            // (1) is cleaner, as (2) requires duplicating a bit more code, but (2) enables all virtual dispatch to be
            // devirtualized and potentially inlined, so that Random.Shared.NextXx(...) ends up being faster.
            // This implements (2).

            [ThreadStatic]
            private static XoshiroImpl? t_random;

            public ThreadSafeRandom() : base(isThreadSafeRandom: true) { }

            private static XoshiroImpl LocalRandom => t_random ?? Create();

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static XoshiroImpl Create() => t_random = new();

            public override int Next()
            {
                int result = LocalRandom.Next();
                AssertInRange(result, 0, int.MaxValue);
                return result;
            }

            public override int Next(int maxValue)
            {
                if (maxValue < 0)
                {
                    ThrowMaxValueMustBeNonNegative();
                }

                int result = LocalRandom.Next(maxValue);
                AssertInRange(result, 0, maxValue);
                return result;
            }

            public override int Next(int minValue, int maxValue)
            {
                if (minValue > maxValue)
                {
                    ThrowMinMaxValueSwapped();
                }

                int result = LocalRandom.Next(minValue, maxValue);
                AssertInRange(result, minValue, maxValue);
                return result;
            }

            public override long NextInt64()
            {
                long result = LocalRandom.NextInt64();
                AssertInRange(result, 0, long.MaxValue);
                return result;
            }

            public override long NextInt64(long maxValue)
            {
                if (maxValue < 0)
                {
                    ThrowMaxValueMustBeNonNegative();
                }

                long result = LocalRandom.NextInt64(maxValue);
                AssertInRange(result, 0, maxValue);
                return result;
            }

            public override long NextInt64(long minValue, long maxValue)
            {
                if (minValue > maxValue)
                {
                    ThrowMinMaxValueSwapped();
                }

                long result = LocalRandom.NextInt64(minValue, maxValue);
                AssertInRange(result, minValue, maxValue);
                return result;
            }

            public override float NextSingle()
            {
                float result = LocalRandom.NextSingle();
                AssertInRange(result);
                return result;
            }

            public override double NextDouble()
            {
                double result = LocalRandom.NextDouble();
                AssertInRange(result);
                return result;
            }

            public override void NextBytes(byte[] buffer)
            {
                if (buffer is null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.buffer);
                }

                LocalRandom.NextBytes(buffer);
            }

            public override void NextBytes(Span<byte> buffer) => LocalRandom.NextBytes(buffer);

            protected override double Sample() => throw new NotSupportedException();
        }
    }
}
