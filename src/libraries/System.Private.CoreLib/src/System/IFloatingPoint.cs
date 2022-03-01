// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

#if !FEATURE_GENERIC_MATH
#error FEATURE_GENERIC_MATH is not defined
#endif

namespace System
{
    /// <summary>Defines a floating-point type.</summary>
    /// <typeparam name="TSelf">The type that implements the interface.</typeparam>
    [RequiresPreviewFeatures(Number.PreviewFeatureMessage, Url = Number.PreviewFeatureUrl)]
    public partial interface IFloatingPoint<TSelf>
        : ISignedNumber<TSelf>, IFloatingPoint<TSelf, TSelf>
        where TSelf : IFloatingPoint<TSelf>
    {
        // static abstract TSelf AcosPi(TSelf x);
        //
        // static abstract TSelf AsinPi(TSelf x);
        //
        // static abstract TSelf AtanPi(TSelf x);
        //
        // static abstract TSelf Atan2Pi(TSelf y, TSelf x);
        //
        // static abstract TSelf Compound(TSelf x, TSelf n);
        //
        // static abstract TSelf CosPi(TSelf x);
        //
        // static abstract TSelf ExpM1(TSelf x);
        //
        // static abstract TSelf Exp2(TSelf x);
        //
        // static abstract TSelf Exp2M1(TSelf x);
        //
        // static abstract TSelf Exp10(TSelf x);
        //
        // static abstract TSelf Exp10M1(TSelf x);
        //
        // static abstract TSelf Hypot(TSelf x, TSelf y);
        //
        // static abstract TSelf LogP1(TSelf x);
        //
        // static abstract TSelf Log2P1(TSelf x);
        //
        // static abstract TSelf Log10P1(TSelf x);
        //
        // static abstract TSelf MaxMagnitudeNumber(TSelf x, TSelf y);
        //
        // static abstract TSelf MaxNumber(TSelf x, TSelf y);
        //
        // static abstract TSelf MinMagnitudeNumber(TSelf x, TSelf y);
        //
        // static abstract TSelf MinNumber(TSelf x, TSelf y);
        //
        // static abstract TSelf Root(TSelf x, TSelf n);
        //
        // static abstract TSelf SinPi(TSelf x);
        //
        // static abstract TSelf TanPi(TSelf x);
    }

    /// <summary>Defines a floating-point type that is represented in a base-2 format.</summary>
    /// <typeparam name="TSelf">The type that implements the interface.</typeparam>
    [RequiresPreviewFeatures(Number.PreviewFeatureMessage, Url = Number.PreviewFeatureUrl)]
    public interface IBinaryFloatingPoint<TSelf>
        : IBinaryNumber<TSelf>,
          IFloatingPoint<TSelf>
        where TSelf : IBinaryFloatingPoint<TSelf>
    {
    }
}
