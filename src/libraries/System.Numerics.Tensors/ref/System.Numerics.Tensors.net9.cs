// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        public static void ConvertToIntegerNative<TFrom, TTo>(System.ReadOnlySpan<TFrom> source, System.Span<TTo> destination) where TFrom : System.Numerics.IFloatingPoint<TFrom> where TTo : System.Numerics.IBinaryInteger<TTo> { }
        public static void ConvertToInteger<TFrom, TTo>(System.ReadOnlySpan<TFrom> source, System.Span<TTo> destination) where TFrom : System.Numerics.IFloatingPoint<TFrom> where TTo : System.Numerics.IBinaryInteger<TTo> { }
    }
}
