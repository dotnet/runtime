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
        public const double MaxValue = 1.7976931348623157E+308;
        public const double MinValue = -1.7976931348623157E+308;

        // Note Epsilon should be a double whose hex representation is 0x1
        // on little endian machines.
        public const double Epsilon = 4.9406564584124654E-324;
        public const double NaN = 0.0 / 0.0;
        public const double NegativeInfinity = -1.0 / 0.0;
        public const double PositiveInfinity = 1.0 / 0.0;
    }
    public partial struct Single
    {
        public const float Epsilon = 1.4E-45f;
        public const float MaxValue = 3.40282346638528859E+38f;
        public const float MinValue = -3.40282346638528859E+38f;
        public const float NaN = 0.0f / 0.0f;
        public const float NegativeInfinity = -1.0f / 0.0f;
        public const float PositiveInfinity = 1.0f / 0.0f;
    }
    public ref partial struct TypedReference
    {
        // We need to add this into the manual ref assembly to preserve it because the
        // implementation doesn't have any reference field, hence GenApi will not emit it.
        private object _dummy;
        // Placing the value type field in the manual ref as well to avoid the error CS0282: There is no defined ordering between fields in multiple declarations of partial struct 'TypedReference'.
        private int _dummyPrimitive;
    }
}
