// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

[assembly:System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.MidpointRounding))]

namespace System
{
    public static partial class Math
    {
        public const double PI = 3.14159265358979323846;
        public const double E = 2.7182818284590452354;
    }
#if netcoreapp11
    public static partial class MathF
    {
        public const float PI = 3.14159265f;
        public const float E = 2.71828183f;
    }
#endif
}
