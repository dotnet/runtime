// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Note: If you add a new ctor overloads you need to update ParameterInfo.RawDefaultValue

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false)]
    public sealed class DecimalConstantAttribute : Attribute
    {
        private Decimal _dec;

        [CLSCompliant(false)]
        public DecimalConstantAttribute(
            byte scale,
            byte sign,
            uint hi,
            uint mid,
            uint low
        )
        {
            _dec = new Decimal((int)low, (int)mid, (int)hi, (sign != 0), scale);
        }

        public DecimalConstantAttribute(
            byte scale,
            byte sign,
            int hi,
            int mid,
            int low
        )
        {
            _dec = new Decimal(low, mid, hi, (sign != 0), scale);
        }

        public Decimal Value => _dec;
    }
}
