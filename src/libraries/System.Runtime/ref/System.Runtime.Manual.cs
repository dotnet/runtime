// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System
{
    public partial struct Double
    {
        public const double MinValue = -1.7976931348623157E+308;
        public const double MaxValue = 1.7976931348623157E+308;

        // Note Epsilon should be a double whose hex representation is 0x1
        // on little endian machines.
        public const double Epsilon = 4.9406564584124654E-324;
        public const double NegativeInfinity = (double)-1.0 / (double)(0.0);
        public const double PositiveInfinity = (double)1.0 / (double)(0.0);
        public const double NaN = (double)0.0 / (double)0.0;
    }
    public partial struct Single
    {
        public const float MinValue = (float)-3.40282346638528859e+38;
        public const float Epsilon = (float)1.4e-45;
        public const float MaxValue = (float)3.40282346638528859e+38;
        public const float PositiveInfinity = (float)1.0 / (float)0.0;
        public const float NegativeInfinity = (float)-1.0 / (float)0.0;
        public const float NaN = (float)0.0 / (float)0.0;
    }
    public ref partial struct TypedReference
    {
        // We need to add this into the manual ref assembly to preserve it because the
        // implementation doesn't have any reference field, hence GenApi will not emit it.
        private object _dummy;
        private int _dummyPrimitive;
    }
}
