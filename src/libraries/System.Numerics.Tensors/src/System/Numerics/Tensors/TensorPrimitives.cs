// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics.Tensors
{
    /// <summary>Performs primitive tensor operations over spans of memory.</summary>
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise result of: <c>pow(e, <paramref name="x" />)</c>.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>This method effectively does <c><paramref name="destination" />[i] = <see cref="MathF" />.Exp(<paramref name="x" />[i])</c>.</remarks>
        public static void Exp(ReadOnlySpan<float> x, Span<float> destination)
        {
            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            for (int i = 0; i < x.Length; i++)
            {
                destination[i] = MathF.Exp(x[i]);
            }
        }

        /// <summary>Computes the element-wise result of: <c>ln(<paramref name="x" />)</c>.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>This method effectively does <c><paramref name="destination" />[i] = <see cref="MathF" />.Log(<paramref name="x" />[i])</c>.</remarks>
        public static void Log(ReadOnlySpan<float> x, Span<float> destination)
        {
            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            for (int i = 0; i < x.Length; i++)
            {
                destination[i] = MathF.Log(x[i]);
            }
        }

        /// <summary>Computes the element-wise result of: <c>cosh(<paramref name="x" />)</c>.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>This method effectively does <c><paramref name="destination" />[i] = <see cref="MathF" />.Cosh(<paramref name="x" />[i])</c>.</remarks>
        public static void Cosh(ReadOnlySpan<float> x, Span<float> destination)
        {
            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            for (int i = 0; i < x.Length; i++)
            {
                destination[i] = MathF.Cosh(x[i]);
            }
        }

        /// <summary>Computes the element-wise result of: <c>sinh(<paramref name="x" />)</c>.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>This method effectively does <c><paramref name="destination" />[i] = <see cref="MathF" />.Sinh(<paramref name="x" />[i])</c>.</remarks>
        public static void Sinh(ReadOnlySpan<float> x, Span<float> destination)
        {
            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            for (int i = 0; i < x.Length; i++)
            {
                destination[i] = MathF.Sinh(x[i]);
            }
        }

        /// <summary>Computes the element-wise result of: <c>tanh(<paramref name="x" />)</c>.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>This method effectively does <c><paramref name="destination" />[i] = <see cref="MathF" />.Tanh(<paramref name="x" />[i])</c>.</remarks>
        public static void Tanh(ReadOnlySpan<float> x, Span<float> destination)
        {
            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            for (int i = 0; i < x.Length; i++)
            {
                destination[i] = MathF.Tanh(x[i]);
            }
        }
    }
}
